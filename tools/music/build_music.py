#!/usr/bin/env python3
"""
Render the Lofi Lunar radio station: every TrackSpec in tools/music/lofi/tracks.py becomes a 48 kHz stereo Ogg
Vorbis file in Assets/_Project/Audio/Music/Radio, plus the playlist manifest tools/music/playlist.json (read by
the audio box's RadioPlaylist builder) and one analysis report per track in tools/music/reports.

Usage:
  python tools/music/build_music.py                 # render every track (in parallel), manifest, reports
  python tools/music/build_music.py --track 03      # render one track (other manifest entries are kept)
  python tools/music/build_music.py --preview 30    # first 30 s of the selected tracks into Logs/music (ignored)
  python tools/music/build_music.py --check         # re-render into a temp dir: the decoded audio must equal the
                                                    # committed files and the manifest, and every gate must pass

Every number in the reports is measured on the decoded OGG, i.e. on what the game ships. OGG bytes are not
deterministic (libsndfile picks a random stream serial), so files are compared through synth.io.audio_digest, and
an unchanged render keeps the existing file untouched. Exit code: 0 = every quality gate passed.
"""
import argparse
import json
import os
import sys
import tempfile
import time
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(TOOLS / "music"), str(TOOLS / "audio")]

import synth  # noqa: E402
from lofi import analysis, theory_report  # noqa: E402
from lofi.composer import compose  # noqa: E402
from lofi.mixer import PRE_ROLL_S, TARGET_LUFS, render  # noqa: E402
from lofi.theory import key_named  # noqa: E402
from lofi.tracks import PLAYLIST, track_by_id  # noqa: E402

REPO = TOOLS.parent
RADIO_ASSET_DIR = "Assets/_Project/Audio/Music/Radio"
MANIFEST = REPO / "tools" / "music" / "playlist.json"
REPORTS = REPO / "tools" / "music" / "reports"
PREVIEWS = REPO / "Logs" / "music"
MANIFEST_VERSION = 1
OGG_QUALITY = 0.6

LOUDNESS_TOLERANCE_LU = 0.5
PLAYLIST_SPREAD_LU = 1.0
MAX_TRUE_PEAK_DBTP = -1.0
MIN_DURATION_S = 120.0
MAX_DURATION_S = 185.0
MIN_BASS_CORRELATION = 0.98
MIN_CORRELATION = 0.5
MAX_HIGH_BAND_SHARE = 0.05
MAX_ABOVE_8K_DB = -25.0
MAX_CHROMA_OUT_OF_KEY = 0.12
MIN_BEAT_CLARITY = 1.5


def _round(value):
    if isinstance(value, float):
        return round(value, 4)
    if isinstance(value, dict):
        return {k: _round(v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return [_round(v) for v in value]
    return value


def gates(measured, theory):
    """{gate: (value, passed)} for one track."""
    return {
        "loudness_lufs": (measured["loudness_lufs"],
                          abs(measured["loudness_lufs"] - TARGET_LUFS) <= LOUDNESS_TOLERANCE_LU),
        "true_peak_dbtp": (measured["true_peak_dbtp"], measured["true_peak_dbtp"] <= MAX_TRUE_PEAK_DBTP),
        "duration_s": (measured["duration_s"], MIN_DURATION_S <= measured["duration_s"] <= MAX_DURATION_S),
        "clipped_samples": (measured["clipped_samples"], measured["clipped_samples"] == 0),
        "silent_gaps": (len(measured["silent_gaps"]), not measured["silent_gaps"]),
        "correlation_below_150hz": (measured["correlation_below_150hz"],
                                    measured["correlation_below_150hz"] >= MIN_BASS_CORRELATION),
        "correlation": (measured["correlation"], measured["correlation"] >= MIN_CORRELATION),
        "high_band_share": (measured["bands"]["high"], measured["bands"]["high"] <= MAX_HIGH_BAND_SHARE),
        "above_8k_db": (measured["above_8k_db"], measured["above_8k_db"] <= MAX_ABOVE_8K_DB),
        "chroma_out_of_key": (measured["chroma_out_of_key"], measured["chroma_out_of_key"] <= MAX_CHROMA_OUT_OF_KEY),
        "beat_clarity": (measured["beat_clarity"], measured["beat_clarity"] >= MIN_BEAT_CLARITY),
        "out_of_key_notes": (theory["out_of_key_notes"], theory["out_of_key_notes"] == 0),
        "lead_non_pentatonic": (theory["lead_non_pentatonic"], theory["lead_non_pentatonic"] == 0),
        "lead_strong_off_chord": (theory["lead_strong_off_chord"], theory["lead_strong_off_chord"] == 0),
        "lead_clashes": (theory["lead_clashes"], theory["lead_clashes"] == 0),
    }


def build_one(track_id, out_dir, preview_s):
    """Compose, render, encode and measure one track (runs in a worker process)."""
    started = time.time()
    spec = track_by_id(track_id)
    score = compose(spec)
    audio, _ = render(score, lambda *keys: synth.rng(spec.seed, *keys), preview_s)
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    final = out_dir / (f"{spec.id}_preview.ogg" if preview_s else f"{spec.id}.ogg")
    fresh = final.with_name(final.stem + ".tmp.ogg")
    synth.io.write_ogg(fresh, audio, quality=OGG_QUALITY)
    digest = synth.io.audio_digest(fresh)
    if final.exists() and synth.io.audio_digest(final) == digest:
        fresh.unlink()
    else:
        os.replace(fresh, final)
    decoded, _ = synth.io.read_audio(final)
    measured = analysis.measure(decoded, key_named(spec.key).pcs, spec.bpm, PRE_ROLL_S)
    theory = theory_report.summary(score)
    return {
        "spec": spec,
        "digest": digest,
        "measured": measured,
        "theory": theory,
        "gates": {} if preview_s else gates(measured, theory),
        "seconds": time.time() - started,
    }


def _report(result):
    spec = result["spec"]
    return _round({
        "id": spec.id,
        "title": spec.title,
        "bpm": spec.bpm,
        "swing": spec.swing,
        "key": spec.key,
        "family": spec.family,
        "drums": spec.drums,
        "ending": spec.ending,
        "form": [f"{s.name} x{s.bars}" for s in spec.form],
        "pcm_sha256": result["digest"],
        "measured": result["measured"],
        "theory": result["theory"],
        "gates": {name: {"value": value, "pass": ok} for name, (value, ok) in result["gates"].items()},
    })


def _entry(result):
    spec = result["spec"]
    measured = result["measured"]
    return _round({
        "id": spec.id,
        "file": f"{RADIO_ASSET_DIR}/{spec.id}.ogg",
        "title": spec.title,
        "bpm": spec.bpm,
        "key": spec.key,
        "duration": measured["duration_s"],
        "loudnessLufs": measured["loudness_lufs"],
        "truePeakDbtp": measured["true_peak_dbtp"],
        "pcmSha256": result["digest"],
    })


def _load_manifest():
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {"version": MANIFEST_VERSION, "tracks": []}


def _write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def _print_table(results):
    print(f"{'track':28} {'bpm':>5} {'key':8} {'family':13} {'dur s':>6} {'LUFS':>6} {'dBTP':>6} "
          f"{'crest':>5} {'low/mid/high %':>15} {'corr':>5} {'<150':>5} {'off-key':>7} {'beat':>5} {'notes':>5} "
          f"{'gates':>6} {'time':>5}")
    for r in results:
        m, spec = r["measured"], r["spec"]
        bands = "/".join(f"{100 * m['bands'][b]:.1f}" for b in ("low", "mid", "high"))
        failed = [name for name, (_, ok) in r["gates"].items() if not ok]
        print(f"{spec.id:28} {spec.bpm:5.0f} {spec.key:8} {spec.family:13} {m['duration_s']:6.1f} "
              f"{m['loudness_lufs']:6.2f} {m['true_peak_dbtp']:6.2f} {m['crest_db']:5.1f} {bands:>15} "
              f"{m['correlation']:5.2f} {m['correlation_below_150hz']:5.2f} {100 * m['chroma_out_of_key']:6.1f}% "
              f"{m['beat_clarity']:5.2f} {r['theory']['lead_notes']:5d} "
              f"{'ok' if not failed else 'FAIL':>6} {r['seconds']:5.0f}")
        for name in failed:
            print(f"    gate failed: {name} = {r['gates'][name][0]}")


def _run(track_ids, out_dir, preview_s):
    workers = min(len(track_ids), os.cpu_count() or 1)
    with ProcessPoolExecutor(max_workers=workers) as pool:
        futures = [pool.submit(build_one, track_id, str(out_dir), preview_s) for track_id in track_ids]
        return [f.result() for f in futures]


def _spread_ok(results):
    loudness = [r["measured"]["loudness_lufs"] for r in results]
    spread = max(loudness) - min(loudness)
    print(f"playlist loudness spread: {spread:.2f} LU (limit {PLAYLIST_SPREAD_LU})")
    return spread <= PLAYLIST_SPREAD_LU


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--track", help="track id, slug or number (default: every track)")
    parser.add_argument("--preview", type=float, metavar="SECONDS", help="render only the first SECONDS")
    parser.add_argument("--check", action="store_true", help="verify determinism and quality gates")
    args = parser.parse_args()
    specs = [track_by_id(args.track)] if args.track else list(PLAYLIST)
    track_ids = [s.id for s in specs]

    if args.preview:
        results = _run(track_ids, PREVIEWS, args.preview)
        _print_table(results)
        print(f"previews written to {PREVIEWS}")
        return 0

    if args.check:
        manifest = {t["id"]: t for t in _load_manifest()["tracks"]}
        with tempfile.TemporaryDirectory() as temp:
            results = _run(track_ids, temp, None)
        _print_table(results)
        ok = all(passed for r in results for _, passed in r["gates"].values())
        for r in results:
            committed = REPO / RADIO_ASSET_DIR / f"{r['spec'].id}.ogg"
            entry = manifest.get(r["spec"].id)
            same_file = committed.exists() and synth.io.audio_digest(committed) == r["digest"]
            same_manifest = entry is not None and entry["pcmSha256"] == r["digest"]
            print(f"{r['spec'].id}: committed audio {'matches' if same_file else 'DIFFERS'}, "
                  f"manifest {'matches' if same_manifest else 'DIFFERS'}")
            ok = ok and same_file and same_manifest
        if not args.track:
            ok = _spread_ok(results) and ok
        print("RESULT: PASS" if ok else "RESULT: FAIL")
        return 0 if ok else 1

    results = _run(track_ids, REPO / RADIO_ASSET_DIR, None)
    _print_table(results)
    manifest = _load_manifest()
    entries = {t["id"]: t for t in manifest["tracks"]}
    for r in results:
        entries[r["spec"].id] = _entry(r)
        _write_json(REPORTS / f"{r['spec'].id}.json", _report(r))
    known = [s.id for s in PLAYLIST]
    _write_json(MANIFEST, {"version": MANIFEST_VERSION, "tracks": [entries[i] for i in known if i in entries]})
    ok = all(passed for r in results for _, passed in r["gates"].values())
    if not args.track:
        ok = _spread_ok(results) and ok
    print("RESULT: PASS" if ok else "RESULT: FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
