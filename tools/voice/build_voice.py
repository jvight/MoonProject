#!/usr/bin/env python3
"""Render Lunarion's voiced radio lines: text -> offline TTS -> tape/radio treatment -> one clip per line.

Reads tools/voice/lines.json (the lines, keyed by the localization key the ticker shows as the subtitle) and
tools/voice/voices.json (the engine, the models with their licences and checksums, and each voice's settings).
Writes 48 kHz 16-bit mono WAVs to Assets/_Project/Audio/Music/Voice/ (imported streamed, like music) and
tools/voice/voice_manifest.json (read by the Unity VoiceLibrary builder). Voices are English only (owner,
2026-10-12); Vietnamese players hear the recording with Vietnamese subtitles.

The TTS engine lives outside the repo (never installed system-wide): ``--tts-home`` or ``MOON_TTS_HOME`` (default
``D:/Tools/sherpa-onnx``) holds ``venv/`` (sherpa-onnx), ``models/<dir>/`` (the extracted archives) and ``cache/``
(seeded model copies, see onnx_seed.py). Rendering is deterministic: seeded model noise, a fresh engine per line,
seeded beds.

Usage:
  python tools/voice/build_voice.py                    # render every line, write the manifest
  python tools/voice/build_voice.py --only letter.mast_1
  python tools/voice/build_voice.py --check            # re-render into a temp dir: byte-identical + gates
"""
import argparse
import hashlib
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "audio"))
sys.path.insert(0, str(HERE))

import onnx_seed  # noqa: E402
import radio_filter  # noqa: E402
from synth.analysis import loudness_integrated, true_peak_db  # noqa: E402
from synth.core import SAMPLE_RATE  # noqa: E402
from synth.io import write_wav  # noqa: E402

REPO = HERE.parents[1]
LINES = HERE / "lines.json"
VOICES = HERE / "voices.json"
MANIFEST = HERE / "voice_manifest.json"
WORKER = HERE / "tts_worker.py"
OUTPUT_ASSET_DIR = "Assets/_Project/Audio/Music/Voice"
MANIFEST_VERSION = 1
DEFAULT_TTS_HOME = "D:/Tools/sherpa-onnx"
TRUE_PEAK_LIMIT_DB = -1.0
LOUDNESS_TOLERANCE_LU = 1.5


def load_json(path: Path) -> dict:
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def clip_file(key: str) -> str:
    return f"{OUTPUT_ASSET_DIR}/{key.replace('.', '_')}.wav"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def validate(lines: dict, voices: dict) -> list:
    problems = []
    keys = set()
    for line in lines["lines"]:
        if line["key"] in keys:
            problems.append(f"duplicate line key {line['key']}")
        keys.add(line["key"])
        if line["voice"] not in voices["voices"]:
            problems.append(f"{line['key']}: unknown voice {line['voice']}")
        if not line["speak"].strip():
            problems.append(f"{line['key']}: nothing to speak")
    for name, voice in voices["voices"].items():
        if voice["model"] not in voices["models"]:
            problems.append(f"voice {name}: unknown model {voice['model']}")
        if voice["filter"] not in radio_filter.TREATMENTS:
            problems.append(f"voice {name}: unknown filter {voice['filter']}")
    return problems


def seeded_model(home: Path, model: dict, seed: int) -> Path:
    """A copy of the model with seeded noise, cached by the source checksum and the seed."""
    source = home / "models" / model["dir"] / model["onnx"]
    if not source.is_file():
        raise SystemExit(f"missing TTS model {source}: extract {model['archive']} ({model['source']}) there")
    actual = sha256_file(source)
    if actual != model["onnxSha256"]:
        raise SystemExit(f"{source}: sha256 {actual} does not match voices.json {model['onnxSha256']}")
    target = home / "cache" / f"{source.stem}-{actual[:12]}-seed{seed}.onnx"
    if not target.is_file():
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(onnx_seed.seed_model(source.read_bytes(), float(seed)))
    return target


def synthesise(home: Path, voices: dict, lines: list, scratch: Path) -> dict:
    """Runs the worker in the TTS venv; returns ``key -> (samples, sample_rate)``."""
    python = home / "venv" / ("Scripts/python.exe" if os.name == "nt" else "bin/python")
    if not python.is_file():
        raise SystemExit(f"missing TTS venv {python} (setup: tools/voice/README.md)")
    jobs = []
    for index, line in enumerate(lines):
        voice = voices["voices"][line["voice"]]
        model = voices["models"][voice["model"]]
        folder = home / "models" / model["dir"]
        jobs.append({
            "key": line["key"],
            "model": str(seeded_model(home, model, voice["seed"])),
            "tokens": str(folder / "tokens.txt"),
            "dataDir": str(folder / "espeak-ng-data"),
            "speaker": voice["speaker"],
            "noiseScale": voice["noiseScale"],
            "noiseW": voice["noiseW"],
            "lengthScale": voice["lengthScale"],
            "text": line["speak"],
            "out": str(scratch / f"take_{index}.f32"),
        })
    job_file = scratch / "jobs.json"
    job_file.write_text(json.dumps(jobs), encoding="utf-8")
    result = subprocess.run([str(python), "-I", str(WORKER), str(job_file)], capture_output=True, text=True,
                            cwd=str(scratch))
    if result.returncode != 0:
        raise SystemExit(f"TTS worker failed:\n{result.stderr}")
    rates = [json.loads(row) for row in result.stdout.splitlines() if row.strip()]
    takes = {}
    for job, rate in zip(jobs, rates):
        takes[job["key"]] = (np.fromfile(job["out"], dtype="<f4").astype(np.float64), rate["sampleRate"])
    return takes


def render(lines: list, voices: dict, home: Path, out_root: Path) -> list:
    with tempfile.TemporaryDirectory(prefix="moon_voice_") as scratch:
        takes = synthesise(home, voices, lines, Path(scratch))
    entries = []
    for line in lines:
        voice = voices["voices"][line["voice"]]
        treatment = radio_filter.TREATMENTS[voice["filter"]]
        take, rate = takes[line["key"]]
        speech = radio_filter.to_project_rate(take, rate, voice["pitchSemitones"])
        clip, start, length = radio_filter.treat(speech, treatment, line["key"])
        path = out_root / clip_file(line["key"])
        path.parent.mkdir(parents=True, exist_ok=True)
        write_wav(path, clip)
        entries.append({
            "key": line["key"],
            "voice": line["voice"],
            "file": clip_file(line["key"]),
            "seconds": round(clip.shape[0] / SAMPLE_RATE, 4),
            "speechStart": round(start, 4),
            "speechSeconds": round(length, 4),
            "loudnessLufs": round(loudness_integrated(clip), 2),
            "truePeakDbtp": round(true_peak_db(clip), 2),
            "targetLufs": treatment.speech_lufs,
            "sha256": sha256_file(path),
        })
    return entries


def gates(entries: list) -> list:
    problems = []
    for entry in entries:
        if entry["truePeakDbtp"] > TRUE_PEAK_LIMIT_DB:
            problems.append(f"{entry['key']}: true peak {entry['truePeakDbtp']} dBTP > {TRUE_PEAK_LIMIT_DB}")
        if abs(entry["loudnessLufs"] - entry["targetLufs"]) > LOUDNESS_TOLERANCE_LU:
            problems.append(f"{entry['key']}: {entry['loudnessLufs']} LUFS, target {entry['targetLufs']}")
    return problems


def manifest(entries: list, voices: dict) -> dict:
    credits = sorted({m["credit"] for m in voices["models"].values() if m["credit"]})
    return {"version": MANIFEST_VERSION, "sampleRate": SAMPLE_RATE, "language": "en", "credits": credits,
            "lines": entries}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--only", action="append", default=[], help="render only these line keys")
    parser.add_argument("--check", action="store_true", help="re-render into a temp dir and compare")
    parser.add_argument("--tts-home", default=os.environ.get("MOON_TTS_HOME", DEFAULT_TTS_HOME))
    args = parser.parse_args()

    lines = load_json(LINES)
    voices = load_json(VOICES)
    problems = validate(lines, voices)
    if problems:
        print("\n".join(problems))
        return 1
    chosen = [l for l in lines["lines"] if not args.only or l["key"] in args.only]
    unknown = set(args.only) - {l["key"] for l in chosen}
    if unknown:
        print(f"unknown line keys: {sorted(unknown)}")
        return 1
    home = Path(args.tts_home)

    if args.check:
        with tempfile.TemporaryDirectory(prefix="moon_voice_check_") as temp:
            entries = render(chosen, voices, home, Path(temp))
        committed = {e["key"]: e for e in load_json(MANIFEST)["lines"]} if MANIFEST.is_file() else {}
        problems = gates(entries)
        for entry in entries:
            old = committed.get(entry["key"])
            if old is None or old["sha256"] != entry["sha256"]:
                problems.append(f"{entry['key']}: render differs from the committed manifest")
            elif sha256_file(REPO / entry["file"]) != entry["sha256"]:
                problems.append(f"{entry['key']}: {entry['file']} differs from its manifest entry")
        print("\n".join(problems) if problems else f"check OK: {len(entries)} clips byte-identical")
        return 1 if problems else 0

    entries = render(chosen, voices, home, REPO)
    if args.only and MANIFEST.is_file():
        merged = {e["key"]: e for e in load_json(MANIFEST)["lines"]}
        merged.update({e["key"]: e for e in entries})
        entries = [merged[l["key"]] for l in lines["lines"] if l["key"] in merged]
    MANIFEST.write_text(json.dumps(manifest(entries, voices), indent=2) + "\n", encoding="utf-8")
    for entry in entries:
        print(f"{entry['key']:24} {entry['voice']:8} {entry['seconds']:6.2f}s {entry['loudnessLufs']:6.1f} LUFS "
              f"{entry['truePeakDbtp']:6.1f} dBTP")
    problems = gates(entries)
    print("\n".join(problems) if problems else "gates OK")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
