#!/usr/bin/env python3
"""Render every Lofi Lunar sound cue deterministically with the shared ``synth`` core.

Writes 48 kHz 16-bit WAVs into Assets/_Project/Audio/{SFX, SFX/2D, Ambience} and the cue manifest
tools/audio/sfx_manifest.json (read by the Unity AudioLibrary builder). Every file is finished the same way:
DC removal, click-free edges (one-shots) or exact periodicity (loops), loudness normalisation to its category
target (BS.1770 K-weighted), and a soft safety limiter that only engages above -1.2 dBFS (reported as a failure:
recipes are expected to have enough headroom).

Usage:
  python tools/audio/build_sfx.py                 # render everything, write the manifest, print the analysis
  python tools/audio/build_sfx.py --only ui_click # render a subset (other manifest entries are kept)
  python tools/audio/build_sfx.py --check         # re-render into a temp dir: byte-identical + quality gates
"""
import argparse
import hashlib
import json
import os
import shutil
import sys
import tempfile
import time
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))

import analyze_audio  # noqa: E402
from cues import CATEGORIES, CUES  # noqa: E402
from synth.analysis import loudness_integrated, loudness_momentary_max  # noqa: E402
from synth.core import db_to_gain, map_channels, rng  # noqa: E402
from synth.effects import soft_limit  # noqa: E402
from synth.envelope import fade_in, fade_out  # noqa: E402
from synth.filters import dc_block  # noqa: E402
from synth.io import write_wav  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
AUDIO_ROOT_ASSET = "Assets/_Project/Audio"
MANIFEST = REPO / "tools" / "audio" / "sfx_manifest.json"
MANIFEST_VERSION = 1
PEAK_CEILING_DB = -1.2
ONESHOT_FADE_IN_S = 0.0005
DC_BLOCK_HZ = 15.0

_CUES_BY_ID = {c.id: c for c in CUES}


def _remove_dc_keep_edges(x: np.ndarray) -> np.ndarray:
    """Subtracts the exact mean as a Hann-shaped bump: zero DC while both edges stay exactly 0."""
    window = np.hanning(x.shape[0])
    return x - x.mean() * window / window.mean()


def finish(cue, signal: np.ndarray) -> tuple:
    """DC removal, edges, loudness normalisation and safety limiting -> ``(signal, gain_db, limited)``."""
    category = CATEGORIES[cue.category]
    x = np.asarray(signal, dtype=np.float64)
    if cue.loop:
        # The exact mean keeps the loop periodic (a high-pass would add a transient at the seam).
        x = x - x.mean(axis=0)
    else:
        x = map_channels(x, lambda c: _remove_dc_keep_edges(
            fade_out(fade_in(dc_block(c, DC_BLOCK_HZ), ONESHOT_FADE_IN_S), cue.fade_out)))
    if category.loudness_mode == "momentary":
        measured = loudness_momentary_max(x, analyze_audio.MOMENTARY_WINDOW_S)
    else:
        measured = loudness_integrated(x)
    gain_db = category.target_lufs - measured
    x = x * db_to_gain(gain_db)
    limited = float(np.max(np.abs(x))) > db_to_gain(PEAK_CEILING_DB)
    if limited:
        x = soft_limit(x, PEAK_CEILING_DB)
    return x, gain_db, limited


def render_cue(cue_id: str, out_root: str) -> list:
    """Renders all variants of one cue into ``out_root`` (worker-process entry point)."""
    cue = _CUES_BY_ID[cue_id]
    results = []
    for variant, rel in enumerate(cue.files()):
        started = time.perf_counter()
        signal, gain_db, limited = finish(cue, cue.render(variant, rng(cue.id, variant)))
        path = Path(out_root) / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        write_wav(path, signal)
        results.append({
            "rel": rel,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "gain_db": gain_db,
            "limited": limited,
            "seconds": time.perf_counter() - started,
        })
    return results


def manifest_entry(cue, file_results) -> dict:
    category = CATEGORIES[cue.category]
    return {
        "id": cue.id,
        "category": cue.category,
        "milestone": cue.milestone,
        "bus": category.bus,
        "spatial": category.spatial,
        "loop": cue.loop,
        "files": [f"{AUDIO_ROOT_ASSET}/{r['rel']}" for r in file_results],
        "sha256": [r["sha256"] for r in file_results],
        "volumeMin": cue.volume[0],
        "volumeMax": cue.volume[1],
        "pitchMin": cue.pitch[0],
        "pitchMax": cue.pitch[1],
        "targetLufs": category.target_lufs,
        "loudnessMode": category.loudness_mode,
        "hfCutoffHz": cue.hf_cutoff,
        "hfMaxDb": cue.hf_max_db,
        "tonal": cue.tonal,
        "notes": cue.notes,
    }


def manifest_text(entries) -> str:
    data = {
        "version": MANIFEST_VERSION,
        "generator": "tools/audio/build_sfx.py",
        "sampleRate": 48000,
        "audioRoot": AUDIO_ROOT_ASSET,
        "cues": entries,
    }
    return json.dumps(data, indent=2, ensure_ascii=False) + "\n"


def _pool(jobs: int, fn, args_list) -> list:
    if jobs <= 1:
        return [fn(*a) for a in args_list]
    with ProcessPoolExecutor(max_workers=jobs) as pool:
        futures = [pool.submit(fn, *a) for a in args_list]
        return [f.result() for f in futures]


def quality_report(cue_ids, results, out_root: Path, jobs: int) -> int:
    owners = [(_CUES_BY_ID[cid], r) for cid in cue_ids for r in results[cid]]
    metrics = _pool(jobs, analyze_audio.analyze_file,
                    [(out_root / r["rel"], cue.loop, cue.hf_cutoff, cue.tonal) for cue, r in owners])
    print(analyze_audio.HEADER)
    failed = 0
    for (cue, r), m in zip(owners, metrics):
        category = CATEGORIES[cue.category]
        fails = analyze_audio.evaluate(m, cue.hf_max_db, category.target_lufs, category.loudness_mode)
        if r["limited"]:
            fails.append("safety limiter engaged (crest factor too high for the loudness target)")
        failed += bool(fails)
        print(analyze_audio.format_row(m, fails))
    return failed


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="re-render to a temp dir and verify; write nothing")
    ap.add_argument("--only", nargs="+", metavar="CUE_ID", help="render only these cues")
    ap.add_argument("--jobs", type=int, default=min(8, os.cpu_count() or 1))
    ap.add_argument("--keep", action="store_true", help="--check: keep the temp render folder")
    args = ap.parse_args()

    unknown = [c for c in (args.only or []) if c not in _CUES_BY_ID]
    if unknown:
        print(f"unknown cue id(s): {', '.join(unknown)}")
        return 2
    cue_ids = args.only or [c.id for c in CUES]
    audio_root = REPO / AUDIO_ROOT_ASSET
    out_root = Path(tempfile.mkdtemp(prefix="lunar_sfx_check_")) if args.check else audio_root

    started = time.perf_counter()
    rendered = _pool(args.jobs, render_cue, [(cid, str(out_root)) for cid in cue_ids])
    results = dict(zip(cue_ids, rendered))
    print(f"rendered {sum(len(v) for v in rendered)} file(s) from {len(cue_ids)} cue(s) "
          f"in {time.perf_counter() - started:.1f} s -> {out_root}")
    for cid in cue_ids:
        for r in results[cid]:
            print(f"  {r['rel']:<42} gain {r['gain_db']:+6.1f} dB  {r['seconds']:5.1f} s"
                  + ("  LIMITED" if r["limited"] else ""))

    problems = quality_report(cue_ids, results, out_root, args.jobs)

    previous = {}
    if MANIFEST.exists():
        previous = {c["id"]: c for c in json.loads(MANIFEST.read_text(encoding="utf-8"))["cues"]}
    entries = []
    for cue in CUES:
        if cue.id in results:
            entries.append(manifest_entry(cue, results[cue.id]))
        elif cue.id in previous:
            entries.append(previous[cue.id])
        else:
            print(f"manifest: cue '{cue.id}' has never been rendered (run without --only)")
            problems += 1
    text = manifest_text(entries)

    if args.check:
        for cid in cue_ids:
            for r in results[cid]:
                committed = audio_root / r["rel"]
                if not committed.exists():
                    print(f"check: {committed.relative_to(REPO)} is missing (run build_sfx.py)")
                    problems += 1
                elif hashlib.sha256(committed.read_bytes()).hexdigest() != r["sha256"]:
                    print(f"check: {r['rel']} differs from the fresh render (stale file or non-determinism)")
                    problems += 1
        if not MANIFEST.exists() or MANIFEST.read_text(encoding="utf-8") != text:
            print("check: sfx_manifest.json differs from the fresh render (stale manifest)")
            problems += 1
        if args.keep:
            print(f"check: renders kept in {out_root}")
        else:
            shutil.rmtree(out_root, ignore_errors=True)
        print("CHECK: " + ("PASS" if problems == 0 else f"FAIL ({problems} problem(s))"))
    else:
        MANIFEST.write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {MANIFEST.relative_to(REPO)}")
        print("BUILD: " + ("OK" if problems == 0 else f"FAIL ({problems} problem(s))"))
    return 0 if problems == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
