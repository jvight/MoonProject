#!/usr/bin/env python3
"""Measure rendered audio files and print one row per file, with the SFX quality gates.

Columns: duration, channels, sample peak and true peak (dBFS), RMS (dBFS), K-weighted loudness (BS.1770-4 gated
integrated, and the loudest 200 ms window), DC offset, edge level (click risk: one-shots must start and end at
silence), loop seam (prediction-error ratio vs. the file's own 99.9th percentile / level jump across the seam),
spectral centroid, and the energy share above the cue's high-frequency guard (default 8 kHz).

Usage:
  python tools/audio/analyze_audio.py                      # every file in tools/audio/sfx_manifest.json
  python tools/audio/analyze_audio.py path/to/a.wav dir/   # arbitrary files (loop = file name contains 'loop')
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numpy as np  # noqa: E402
from synth.analysis import analyze  # noqa: E402
from synth.core import PENTATONIC, SAMPLE_RATE, note_to_midi, to_mono  # noqa: E402
from synth.io import read_audio  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
MANIFEST = REPO / "tools" / "audio" / "sfx_manifest.json"

MAX_PEAK_DB = -1.0
MAX_TRUE_PEAK_DB = -0.5
MAX_ABS_DC = 2e-4
MAX_EDGE_DB = -60.0
MAX_SEAM_RATIO = 1.0
MAX_SEAM_JUMP_DB = 3.0
LOUDNESS_TOLERANCE_DB = 0.5
MOMENTARY_WINDOW_S = 0.2
MAX_KEY_ERROR_CENTS = 20.0
_NOTE_NAMES = ("C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B")
_KEY_PITCH_CLASSES = {note_to_midi(f"{p}4") % 12 for p in PENTATONIC}


def dominant_pitch(x: np.ndarray) -> tuple:
    """``(note_name, cents_error, in_key)`` of the strongest spectral peak (60 Hz..5 kHz, zero-padded FFT with
    parabolic interpolation) compared with the nearest equal-tempered note and D major pentatonic."""
    mono = to_mono(x)
    size = 1 << 20
    spectrum = np.abs(np.fft.rfft(mono * np.hanning(mono.shape[0]), size))
    freqs = np.fft.rfftfreq(size, 1.0 / SAMPLE_RATE)
    band = np.nonzero((freqs >= 60.0) & (freqs <= 5000.0))[0]
    k = band[np.argmax(spectrum[band])]
    a, b, c = np.log(spectrum[k - 1:k + 2] + 1e-30)
    curvature = a - 2.0 * b + c
    offset = 0.5 * (a - c) / curvature if curvature != 0.0 else 0.0
    freq = (k + offset) * SAMPLE_RATE / size
    midi = 69.0 + 12.0 * np.log2(freq / 440.0)
    nearest = round(midi)
    name = f"{_NOTE_NAMES[nearest % 12]}{nearest // 12 - 1}"
    return name, 100.0 * (midi - nearest), nearest % 12 in _KEY_PITCH_CLASSES


def analyze_file(path: Path, loop: bool, hf_cutoff: float = 8000.0, tonal: bool = False) -> dict:
    data, rate = read_audio(path)
    if rate != SAMPLE_RATE:
        raise ValueError(f"{path}: expected {SAMPLE_RATE} Hz, got {rate}")
    result = analyze(data, loop=loop, hf_cutoff=hf_cutoff, momentary_window=MOMENTARY_WINDOW_S)
    result["file"] = path.name
    result["tonal"] = tonal
    if tonal:
        result["pitch"], result["pitch_cents"], result["in_key"] = dominant_pitch(data)
    return result


def evaluate(m: dict, hf_max_db: float = -30.0, target_lufs: float | None = None,
             loudness_mode: str | None = None) -> list:
    """Failed gates for one file (empty list = pass)."""
    failures = []
    if m["tonal"] and (not m["in_key"] or abs(m["pitch_cents"]) > MAX_KEY_ERROR_CENTS):
        failures.append(f"dominant pitch {m['pitch']} {m['pitch_cents']:+.0f} cents is not D major pentatonic")
    if m["peak_db"] > MAX_PEAK_DB:
        failures.append(f"peak {m['peak_db']:.2f} dBFS > {MAX_PEAK_DB}")
    if m["true_peak_db"] > MAX_TRUE_PEAK_DB:
        failures.append(f"true peak {m['true_peak_db']:.2f} dBTP > {MAX_TRUE_PEAK_DB}")
    if m["dc"] > MAX_ABS_DC:
        failures.append(f"DC {m['dc']:.2e} > {MAX_ABS_DC:.0e}")
    if m["loop"]:
        if m["seam_ratio"] > MAX_SEAM_RATIO:
            failures.append(f"seam ratio {m['seam_ratio']:.2f} > {MAX_SEAM_RATIO}")
        if m["seam_jump_db"] > MAX_SEAM_JUMP_DB:
            failures.append(f"seam level jump {m['seam_jump_db']:.2f} dB > {MAX_SEAM_JUMP_DB}")
    elif m["edge_db"] > MAX_EDGE_DB:
        failures.append(f"edge {m['edge_db']:.1f} dBFS > {MAX_EDGE_DB} (click risk)")
    if m["hf_db"] > hf_max_db:
        failures.append(f"energy above {m['hf_cutoff']:.0f} Hz {m['hf_db']:.1f} dB > {hf_max_db}")
    if target_lufs is not None:
        key = "lufs_momentary_max" if loudness_mode == "momentary" else "lufs_integrated"
        if abs(m[key] - target_lufs) > LOUDNESS_TOLERANCE_DB:
            failures.append(f"loudness {m[key]:.2f} LUFS != target {target_lufs} +/- {LOUDNESS_TOLERANCE_DB}")
    return failures


def _db(value: float) -> str:
    return "  -inf" if value <= -199.0 else f"{value:6.1f}"


HEADER = (f"{'file':<30} {'ch':>2} {'dur s':>6} {'peak':>6} {'TP':>6} {'rms':>6} {'LUFS-I':>6} {'LUFS-M':>6} "
          f"{'DC':>8} {'edge':>6} {'seam':>11} {'centroid':>8} {'HF dB':>11} {'pitch':>9}  verdict")


def format_row(m: dict, failures: list) -> str:
    seam = f"{m['seam_ratio']:4.2f}/{m['seam_jump_db']:3.1f}dB" if m["loop"] else "one-shot"
    hf = f"{_db(m['hf_db'])}@{m['hf_cutoff'] / 1000:.0f}k"
    pitch = f"{m['pitch']}{m['pitch_cents']:+.0f}c" if m["tonal"] else "-"
    verdict = "ok" if not failures else "FAIL: " + "; ".join(failures)
    return (f"{m['file']:<30} {m['channels']:>2} {m['duration']:6.2f} {_db(m['peak_db'])} {_db(m['true_peak_db'])} "
            f"{_db(m['rms_db'])} {_db(m['lufs_integrated'])} {_db(m['lufs_momentary_max'])} {m['dc']:8.1e} "
            f"{_db(m['edge_db'])} {seam:>11} {m['centroid_hz']:6.0f}Hz {hf:>11} {pitch:>9}  {verdict}")


def manifest_rows(manifest_path: Path = MANIFEST):
    """Yields ``(path, cue_entry)`` for every file listed in the manifest."""
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    for cue in data["cues"]:
        for asset_path in cue["files"]:
            yield REPO / asset_path, cue


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("paths", nargs="*", help="WAV files or folders (default: every file in the manifest)")
    ap.add_argument("--hf-cutoff", type=float, default=8000.0)
    args = ap.parse_args()
    print(HEADER)
    failed = 0
    if args.paths:
        files = []
        for p in map(Path, args.paths):
            files += sorted(p.rglob("*.wav")) if p.is_dir() else [p]
        for f in files:
            m = analyze_file(f, loop="loop" in f.stem, hf_cutoff=args.hf_cutoff)
            fails = evaluate(m)
            failed += bool(fails)
            print(format_row(m, fails))
    else:
        for path, cue in manifest_rows():
            m = analyze_file(path, loop=cue["loop"], hf_cutoff=cue["hfCutoffHz"], tonal=cue["tonal"])
            fails = evaluate(m, cue["hfMaxDb"], cue["targetLufs"], cue["loudnessMode"])
            failed += bool(fails)
            print(format_row(m, fails))
    print(f"{failed} file(s) failed the quality gates" if failed else "all files pass the quality gates")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
