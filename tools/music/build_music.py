#!/usr/bin/env python3
"""
Render the Lofi Lunar radio station, Ro's cassette tapes and Bell's station jingle.

  radio    every TrackSpec of tools/music/lofi/tracks.py PLAYLIST: a 48 kHz stereo Ogg Vorbis file in
           Assets/_Project/Audio/Music/Radio and the manifest tools/music/playlist.json (read by the audio box's
           RadioPlaylist builder).
  tapes    every TrackSpec of tools/music/lofi/tapes.py TAPES: a seamless 48 kHz stereo Ogg Vorbis loop in
           Assets/_Project/Audio/Music/Tapes/<cassette id>.ogg and the manifest tools/music/tapes.json (the playlist
           schema plus "tape", the cassette id). Tapes never enter playlist.json: the radio plays a tape only once
           07 has collected it, so the audio box adds them from tapes.json.
  jingles  Bell's station jingle (tools/music/lofi/jingle.py): 44.1 kHz 16-bit mono WAV files in
           Assets/_Project/Audio/Music/Jingles and the manifest tools/music/jingles.json (notes and their onsets).

Every track and jingle also gets an analysis report in tools/music/reports, and the listening report
tools/music/reports/tapes.md compares the whole catalogue.

Usage:
  python tools/music/build_music.py                 # everything (in parallel), manifests, reports
  python tools/music/build_music.py --track 03      # one radio track or tape (other manifest entries are kept)
  python tools/music/build_music.py --jingles       # only the jingles
  python tools/music/build_music.py --preview 30    # first 30 s of the selected tracks into Logs/music (ignored)
  python tools/music/build_music.py --check         # re-render everything into a temp dir: the decoded audio, the
                                                    # jingle WAV bytes and the three manifests must equal the
                                                    # committed ones, and every gate must pass

Every number in the reports is measured on the decoded files, i.e. on what the game ships. OGG bytes are not
deterministic (libsndfile picks a random stream serial), so OGG files are compared through synth.io.audio_digest, and
an unchanged render keeps the existing file untouched; WAV files are byte-identical. Exit code: 0 = every gate passed.
"""
import argparse
import hashlib
import json
import os
import sys
import tempfile
import time
from concurrent.futures import ProcessPoolExecutor
from dataclasses import dataclass
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(TOOLS / "music"), str(TOOLS / "audio")]

import synth  # noqa: E402
from lofi import analysis, jingle, listening, theory_report  # noqa: E402
from lofi.composer import compose  # noqa: E402
from lofi.mixer import PRE_ROLL_S, TARGET_LUFS, render  # noqa: E402
from lofi.tapes import TAPES  # noqa: E402
from lofi.theory import D_MAJOR, key_named  # noqa: E402
from lofi.tracks import CATALOGUE, PLAYLIST, track_by_id  # noqa: E402
from synth.core import note_to_midi  # noqa: E402

REPO = TOOLS.parent
REPORTS = REPO / "tools" / "music" / "reports"
LISTENING_REPORT = REPORTS / "tapes.md"
PREVIEWS = REPO / "Logs" / "music"
JINGLE_ASSET_DIR = "Assets/_Project/Audio/Music/Jingles"
JINGLE_MANIFEST = REPO / "tools" / "music" / "jingles.json"
MANIFEST_VERSION = 1
OGG_QUALITY = 0.6
DEFAULT_JOBS = 4

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
MAX_TEMPO_ERROR = 0.005
MAX_SEAM_RATIO = 1.0
MAX_SEAM_STEP_LU = 3.0
JINGLE_DURATION_S = (1.5, 3.0)
MAX_JINGLE_EDGE = 1e-3
MAX_JINGLE_OUT_OF_PENTATONIC = 0.1


@dataclass(frozen=True)
class Collection:
    """A set of tracks that ships to one folder with one manifest; `tape` entries also name their cassette."""
    name: str
    specs: tuple
    asset_dir: str
    manifest: Path
    tape: bool


RADIO = Collection("radio", PLAYLIST, "Assets/_Project/Audio/Music/Radio", REPO / "tools" / "music" / "playlist.json",
                   False)
TAPE_DECK = Collection("tapes", TAPES, "Assets/_Project/Audio/Music/Tapes", REPO / "tools" / "music" / "tapes.json",
                       True)
COLLECTIONS = (RADIO, TAPE_DECK)


def collection_of(spec):
    return TAPE_DECK if spec.tape else RADIO


def _round(value):
    if isinstance(value, float):
        return round(value, 4)
    if isinstance(value, dict):
        return {k: _round(v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return [_round(v) for v in value]
    return value


def gates(spec, measured, theory, listened):
    """{gate: (value, passed)} for one track."""
    tempo_error = abs(listened["tempo_detected_bpm"] - spec.bpm) / spec.bpm
    result = {
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
        "tempo_error": (tempo_error, tempo_error <= MAX_TEMPO_ERROR),
        "out_of_key_notes": (theory["out_of_key_notes"], theory["out_of_key_notes"] == 0),
        "lead_non_pentatonic": (theory["lead_non_pentatonic"], theory["lead_non_pentatonic"] == 0),
        "lead_strong_off_chord": (theory["lead_strong_off_chord"], theory["lead_strong_off_chord"] == 0),
        "lead_clashes": (theory["lead_clashes"], theory["lead_clashes"] == 0),
    }
    if "seam" in listened:
        seam = listened["seam"]
        result["seam_ratio"] = (seam["ratio"], seam["ratio"] <= MAX_SEAM_RATIO)
        result["seam_loudness_step_lu"] = (seam["loudness_step_lu"], seam["loudness_step_lu"] <= MAX_SEAM_STEP_LU)
    return result


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
    loop = spec.ending == "loop" and not preview_s
    measured = analysis.measure(decoded, key_named(spec.key).pcs, spec.bpm, 0.0 if loop else PRE_ROLL_S)
    theory = theory_report.summary(score)
    listened = {} if preview_s else listening.listen(decoded, loop)
    return {
        "spec": spec,
        "digest": digest,
        "measured": measured,
        "theory": theory,
        "listening": listened,
        "gates": {} if preview_s else gates(spec, measured, theory, listened),
        "seconds": time.time() - started,
    }


def jingle_gates(spec, measured, melody):
    expected = [name for _, name, _, _ in spec.melody]
    heard = [note["detected"] for note in melody]
    pentatonic = all(note_to_midi(name) % 12 in D_MAJOR.pentatonic for name in heard)
    low, high = JINGLE_DURATION_S
    return {
        "duration_s": (measured["duration_s"], low <= measured["duration_s"] <= high),
        "sample_rate": (measured["sample_rate"], measured["sample_rate"] == jingle.DELIVERY_RATE),
        "channels": (measured["channels"], measured["channels"] == 1),
        "true_peak_dbtp": (measured["true_peak_dbtp"], measured["true_peak_dbtp"] <= MAX_TRUE_PEAK_DBTP),
        "loudness_momentary_lufs": (measured["loudness_momentary_lufs"],
                                    abs(measured["loudness_momentary_lufs"] - jingle.LOUDNESS_LUFS)
                                    <= LOUDNESS_TOLERANCE_LU),
        "edge_max": (measured["edge_max"], measured["edge_max"] <= MAX_JINGLE_EDGE),
        "above_8k_db": (measured["above_8k_db"], measured["above_8k_db"] <= MAX_ABOVE_8K_DB),
        "chroma_out_of_pentatonic": (measured["chroma_out_of_pentatonic"],
                                     measured["chroma_out_of_pentatonic"] <= MAX_JINGLE_OUT_OF_PENTATONIC),
        "notes_heard": (" ".join(heard), heard == expected),
        "notes_pentatonic": (pentatonic, pentatonic),
    }


def build_jingle(jingle_id, out_dir):
    """Render, write and measure one jingle (runs in a worker process)."""
    started = time.time()
    spec = jingle.jingle_by_id(jingle_id)
    master = jingle.render_master(spec, lambda *keys: synth.rng(spec.seed, *keys))
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{spec.id}.wav"
    synth.io.write_wav(path, jingle.to_delivery_rate(master), bits=16, sample_rate=jingle.DELIVERY_RATE)
    decoded, rate = synth.io.read_audio(path)
    measured, melody = jingle.measure(decoded, rate, spec)
    return {
        "spec": spec,
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "measured": measured,
        "melody": melody,
        "gates": jingle_gates(spec, measured, melody),
        "seconds": time.time() - started,
    }


def _report(result):
    spec = result["spec"]
    report = {
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
        "gates": {name: {"value": value, "pass": bool(ok)} for name, (value, ok) in result["gates"].items()},
    }
    if spec.tape:
        report["tape"] = spec.id
        report["notes"] = spec.notes
    report["instruments"] = listening.instruments(spec)
    report["listening"] = result["listening"]
    return _round(report)


def _jingle_report(result):
    spec = result["spec"]
    return _round({
        "id": spec.id,
        "notes": spec.notes,
        "sha256": result["sha256"],
        "measured": result["measured"],
        "melody": result["melody"],
        "gates": {name: {"value": value, "pass": bool(ok)} for name, (value, ok) in result["gates"].items()},
    })


def _entry(result):
    spec = result["spec"]
    measured = result["measured"]
    entry = {"id": spec.id}
    if spec.tape:
        entry["tape"] = spec.id
    entry.update({
        "file": f"{collection_of(spec).asset_dir}/{spec.id}.ogg",
        "title": spec.title,
        "bpm": spec.bpm,
        "key": spec.key,
        "duration": measured["duration_s"],
        "loudnessLufs": measured["loudness_lufs"],
        "truePeakDbtp": measured["true_peak_dbtp"],
        "pcmSha256": result["digest"],
    })
    return _round(entry)


def _jingle_entry(result):
    spec = result["spec"]
    melody = spec.melody
    return _round({
        "id": spec.id,
        "file": f"{JINGLE_ASSET_DIR}/{spec.id}.wav",
        "notes": [name for _, name, _, _ in melody],
        "onsets": [onset for onset, _, _, _ in melody],
        "duration": result["measured"]["duration_s"],
        "loudnessLufsMomentary": result["measured"]["loudness_momentary_lufs"],
        "truePeakDbtp": result["measured"]["true_peak_dbtp"],
        "sha256": result["sha256"],
    })


def _load(path, key):
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {"version": MANIFEST_VERSION, key: []}


def _json_text(data):
    return json.dumps(data, indent=2, ensure_ascii=False) + "\n"


def _write_text(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def _manifest_text(collection, results, existing):
    entries = {t["id"]: t for t in existing["tracks"]}
    for r in results:
        if r["spec"] in collection.specs:
            entries[r["spec"].id] = _entry(r)
    known = [s.id for s in collection.specs]
    return _json_text({"version": MANIFEST_VERSION, "tracks": [entries[i] for i in known if i in entries]})


def _jingle_manifest_text(results, existing):
    entries = {j["id"]: j for j in existing["jingles"]}
    for r in results:
        entries[r["spec"].id] = _jingle_entry(r)
    known = [j.id for j in jingle.JINGLES]
    return _json_text({"version": MANIFEST_VERSION, "sampleRate": jingle.DELIVERY_RATE,
                       "jingles": [entries[i] for i in known if i in entries]})


def _failed(result):
    return [name for name, (_, ok) in result["gates"].items() if not ok]


def _print_tracks(results):
    print(f"{'track':28} {'bpm':>5} {'found':>6} {'key':8} {'family':13} {'dur s':>6} {'LUFS':>6} {'dBTP':>6} "
          f"{'crest':>5} {'low/mid/high %':>15} {'corr':>5} {'<150':>5} {'off-key':>7} {'beat':>5} {'seam':>5} "
          f"{'notes':>5} {'gates':>6} {'time':>5}")
    for r in results:
        m, spec, heard = r["measured"], r["spec"], r["listening"]
        bands = "/".join(f"{100 * m['bands'][b]:.1f}" for b in ("low", "mid", "high"))
        found = f"{heard['tempo_detected_bpm']:6.2f}" if heard else f"{'-':>6}"
        seam = f"{heard['seam']['ratio']:5.2f}" if "seam" in heard else f"{'-':>5}"
        failed = _failed(r)
        print(f"{spec.id:28} {spec.bpm:5.0f} {found} {spec.key:8} {spec.family:13} {m['duration_s']:6.1f} "
              f"{m['loudness_lufs']:6.2f} {m['true_peak_dbtp']:6.2f} {m['crest_db']:5.1f} {bands:>15} "
              f"{m['correlation']:5.2f} {m['correlation_below_150hz']:5.2f} {100 * m['chroma_out_of_key']:6.1f}% "
              f"{m['beat_clarity']:5.2f} {seam} {r['theory']['lead_notes']:5d} "
              f"{'ok' if not failed else 'FAIL':>6} {r['seconds']:5.0f}")
        for name in failed:
            print(f"    gate failed: {name} = {r['gates'][name][0]}")


def _print_jingles(results):
    for r in results:
        m = r["measured"]
        heard = " ".join(f"{n['detected']}({n['cents']:+.0f}c)" for n in r["melody"])
        failed = _failed(r)
        print(f"{r['spec'].id:20} {m['duration_s']:5.2f} s {m['sample_rate']} Hz  loudest 400 ms "
              f"{m['loudness_momentary_lufs']:6.2f} LUFS  TP {m['true_peak_dbtp']:6.2f}  off-pentatonic "
              f"{100 * m['chroma_out_of_pentatonic']:4.1f}%  heard {heard}  {'ok' if not failed else 'FAIL'}")
        for name in failed:
            print(f"    gate failed: {name} = {r['gates'][name][0]}")


def _run(track_ids, jingle_ids, track_dir, jingle_dir, preview_s, jobs):
    """Render in parallel; `track_dir(spec)` and `jingle_dir` say where files go. Returns (tracks, jingles)."""
    with ProcessPoolExecutor(max_workers=max(1, jobs)) as pool:
        tracks = [pool.submit(build_one, track_id, str(track_dir(track_by_id(track_id))), preview_s)
                  for track_id in track_ids]
        jingles = [pool.submit(build_jingle, jingle_id, str(jingle_dir)) for jingle_id in jingle_ids]
        return [f.result() for f in tracks], [f.result() for f in jingles]


def _spread_ok(results):
    loudness = [r["measured"]["loudness_lufs"] for r in results]
    spread = max(loudness) - min(loudness)
    print(f"catalogue loudness spread: {spread:.2f} LU (limit {PLAYLIST_SPREAD_LU})")
    return spread <= PLAYLIST_SPREAD_LU


def _all_passed(track_results, jingle_results):
    return all(not _failed(r) for r in track_results + jingle_results)


def _check(track_ids, jingle_ids, jobs):
    with tempfile.TemporaryDirectory() as temp:
        temp = Path(temp)
        track_results, jingle_results = _run(track_ids, jingle_ids, lambda spec: temp / collection_of(spec).name,
                                             temp / "jingles", None, jobs)
    _print_tracks(track_results)
    _print_jingles(jingle_results)
    ok = _all_passed(track_results, jingle_results)
    for r in track_results:
        spec = r["spec"]
        collection = collection_of(spec)
        committed = REPO / collection.asset_dir / f"{spec.id}.ogg"
        entry = {t["id"]: t for t in _load(collection.manifest, "tracks")["tracks"]}.get(spec.id)
        same_file = committed.exists() and synth.io.audio_digest(committed) == r["digest"]
        same_entry = entry == _entry(r)
        print(f"{spec.id}: committed audio {'matches' if same_file else 'DIFFERS'}, "
              f"{collection.manifest.name} entry {'matches' if same_entry else 'DIFFERS'}")
        ok = ok and same_file and same_entry
    jingle_entries = {j["id"]: j for j in _load(JINGLE_MANIFEST, "jingles")["jingles"]}
    for r in jingle_results:
        committed = REPO / JINGLE_ASSET_DIR / f"{r['spec'].id}.wav"
        same_file = committed.exists() and hashlib.sha256(committed.read_bytes()).hexdigest() == r["sha256"]
        same_entry = jingle_entries.get(r["spec"].id) == _jingle_entry(r)
        print(f"{r['spec'].id}: committed WAV {'matches' if same_file else 'DIFFERS'} byte for byte, "
              f"jingles.json entry {'matches' if same_entry else 'DIFFERS'}")
        ok = ok and same_file and same_entry
    if len(track_ids) == len(CATALOGUE) and len(jingle_ids) == len(jingle.JINGLES):
        ok = _spread_ok(track_results) and ok
        empty = {"version": MANIFEST_VERSION, "tracks": [], "jingles": []}
        fresh = {collection.manifest: _manifest_text(collection, track_results, empty) for collection in COLLECTIONS}
        fresh[JINGLE_MANIFEST] = _jingle_manifest_text(jingle_results, empty)
        for path, text in fresh.items():
            committed = path.read_text(encoding="utf-8").replace("\r\n", "\n") if path.exists() else ""
            same = committed == text
            print(f"{path.name}: {'unchanged' if same else 'DIFFERS from a fresh render'}")
            ok = ok and same
    print("RESULT: PASS" if ok else "RESULT: FAIL")
    return 0 if ok else 1


def _write_outputs(track_results, jingle_results):
    for collection in COLLECTIONS:
        if any(r["spec"] in collection.specs for r in track_results):
            text = _manifest_text(collection, track_results, _load(collection.manifest, "tracks"))
            _write_text(collection.manifest, text)
    if jingle_results:
        _write_text(JINGLE_MANIFEST, _jingle_manifest_text(jingle_results, _load(JINGLE_MANIFEST, "jingles")))
    for r in track_results:
        _write_text(REPORTS / f"{r['spec'].id}.json", _json_text(_report(r)))
    for r in jingle_results:
        _write_text(REPORTS / f"{r['spec'].id}.json", _json_text(_jingle_report(r)))
    track_reports = []
    for spec in CATALOGUE:
        path = REPORTS / f"{spec.id}.json"
        report = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}
        if "listening" not in report:
            print(f"listening report not written: {path.name} is missing or predates it (run a full build)")
            return
        track_reports.append(report)
    jingle_reports = [json.loads((REPORTS / f"{j.id}.json").read_text(encoding="utf-8")) for j in jingle.JINGLES
                      if (REPORTS / f"{j.id}.json").exists()]
    _write_text(LISTENING_REPORT, listening.markdown(track_reports, {s.id for s in TAPES}, jingle_reports))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--track", help="track or tape id, slug or catalogue number (default: everything)")
    parser.add_argument("--jingles", action="store_true", help="render only the jingles")
    parser.add_argument("--preview", type=float, metavar="SECONDS", help="render only the first SECONDS")
    parser.add_argument("--check", action="store_true", help="verify determinism, manifests and quality gates")
    parser.add_argument("--jobs", type=int, default=DEFAULT_JOBS, help="parallel renders (each needs ~2 GB)")
    args = parser.parse_args()
    if args.jingles:
        track_ids, jingle_ids = [], [j.id for j in jingle.JINGLES]
    elif args.track:
        track_ids, jingle_ids = [track_by_id(args.track).id], []
    else:
        track_ids, jingle_ids = [s.id for s in CATALOGUE], [j.id for j in jingle.JINGLES]

    if args.preview:
        results, _ = _run(track_ids, [], lambda spec: PREVIEWS, PREVIEWS, args.preview, args.jobs)
        _print_tracks(results)
        print(f"previews written to {PREVIEWS}")
        return 0

    if args.check:
        return _check(track_ids, jingle_ids, args.jobs)

    track_results, jingle_results = _run(track_ids, jingle_ids, lambda spec: REPO / collection_of(spec).asset_dir,
                                         REPO / JINGLE_ASSET_DIR, None, args.jobs)
    _print_tracks(track_results)
    _print_jingles(jingle_results)
    _write_outputs(track_results, jingle_results)
    ok = _all_passed(track_results, jingle_results)
    if len(track_ids) == len(CATALOGUE):
        ok = _spread_ok(track_results) and ok
    print("RESULT: PASS" if ok else "RESULT: FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
