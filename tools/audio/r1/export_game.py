#!/usr/bin/env python3
"""Deterministically render the picked variant of every R1 cue into the game's audio folders
and register all 32 cues in ``tools/audio/sfx_manifest.json``.

Outputs:
  - 29 SFX cues     -> ``Assets/_Project/Audio/SFX/R1/<cue_id>.wav``
  - 3 ambience beds -> ``Assets/_Project/Audio/Ambience/R1/<cue_id>.wav``
  - Manifest update -> ``tools/audio/sfx_manifest.json``

Usage:
  python tools/audio/r1/export_game.py           # render all 32 picked R1 WAVs and update sfx_manifest.json
  python tools/audio/r1/export_game.py --check   # verify exported WAVs and sfx_manifest.json match without writing
"""
import argparse
import hashlib
import io
import math
import sys
from pathlib import Path

AUDIO_TOOLS_DIR = Path(__file__).resolve().parents[1]
REPO_ROOT = AUDIO_TOOLS_DIR.parents[1]
if str(AUDIO_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(AUDIO_TOOLS_DIR))

import numpy as np  # noqa: E402
import soundfile as sf  # noqa: E402
from analyze_audio import MAX_KEY_ERROR_CENTS, dominant_pitch, evaluate  # noqa: E402
from cues import CUES  # noqa: E402
from synth import analysis, core  # noqa: E402
from synth.io import quantize  # noqa: E402

from r1.build_audition import condition_and_normalize  # noqa: E402
from r1.catalog import CATALOG, CueSpec  # noqa: E402

AUDIO_ASSET_REL = "Assets/_Project/Audio"
SFX_R1_REL = f"{AUDIO_ASSET_REL}/SFX/R1"
AMBIENCE_R1_REL = f"{AUDIO_ASSET_REL}/Ambience/R1"
SFX_R1_DIR = REPO_ROOT / SFX_R1_REL
AMBIENCE_R1_DIR = REPO_ROOT / AMBIENCE_R1_REL
MANIFEST_PATH = AUDIO_TOOLS_DIR / "sfx_manifest.json"
HF_CUTOFF_HZ = 8000.0
HF_MAX_DB = -30.0

SPATIAL_GROUPS = frozenset({"face", "building", "machines"})


def cue_rel_path(spec: CueSpec) -> str:
    """Project-relative WAV path for a picked R1 cue."""
    folder = AMBIENCE_R1_REL if spec.group_id == "ambience" else SFX_R1_REL
    return f"{folder}/{spec.cue_id}.wav"


def cue_abs_path(spec: CueSpec) -> Path:
    """Absolute filesystem path for a picked R1 cue."""
    return REPO_ROOT / cue_rel_path(spec)


def cue_bus(spec: CueSpec) -> str:
    """Runtime AudioBus name for a picked R1 cue."""
    return "Ambience" if spec.group_id == "ambience" else "Sfx"


def cue_spatial(spec: CueSpec) -> bool:
    """Whether a picked R1 cue is played in 3D space (True) or flat 2D (False)."""
    return spec.group_id in SPATIAL_GROUPS


def render_picked(spec: CueSpec) -> np.ndarray:
    """Render and loudness-condition the picked variant ('A' or 'B') of ``spec``."""
    variant_idx = 0 if spec.pick == "A" else 1
    gen = core.rng("r1", spec.cue_id, spec.pick)
    raw = spec.fn(variant_idx, gen)
    return condition_and_normalize(raw, spec)


def build_r1_manifest_entry(spec: CueSpec, sha256_hex: str, tonal: bool) -> dict:
    """Construct the ``sfx_manifest.json`` entry for an R1 cue."""
    return {
        "id": spec.cue_id,
        "category": spec.category,
        "milestone": "R1",
        "bus": cue_bus(spec),
        "spatial": cue_spatial(spec),
        "loop": spec.loop,
        "files": [cue_rel_path(spec)],
        "variantLabels": [spec.cue_id],
        "sha256": [sha256_hex],
        "volumeMin": spec.volume,
        "volumeMax": spec.volume,
        "pitchMin": 1.0,
        "pitchMax": 1.0,
        "targetLufs": spec.target_lufs,
        "loudnessMode": "integrated" if spec.loop else "momentary",
        "hfCutoffHz": HF_CUTOFF_HZ,
        "hfMaxDb": HF_MAX_DB,
        "tonal": tonal,
        "notes": spec.pick_reason,
    }


def _file_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def base_manifest_entries() -> list[dict]:
    """Manifest entries for the non-R1 cues in ``cues.py``, from the WAVs on disk (the same entries build_sfx writes)."""
    from build_sfx import manifest_entry
    entries: list[dict] = []
    for cue in CUES:
        files = [{"rel": rel, "sha256": _file_sha256(REPO_ROOT / AUDIO_ASSET_REL / rel)} for rel in cue.files()]
        entries.append(manifest_entry(cue, files))
    return entries


AUDIO_LIBRARY_PATH = REPO_ROOT / "Assets" / "_Project" / "Data" / "Audio" / "AudioLibrary.asset"
BUS_ENUM = {"Master": 0, "Music": 1, "Sfx": 2, "Ambience": 3}


def _deterministic_guid(rel_path: str) -> str:
    return hashlib.md5(f"moonproject:r1:{rel_path}".encode("utf-8")).hexdigest()


def _folder_meta_text(rel_path: str) -> str:
    guid = _deterministic_guid(rel_path)
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def _wav_meta_text(spec: CueSpec) -> str:
    rel = cue_rel_path(spec)
    guid = _deterministic_guid(rel)
    is_ambience = spec.group_id == "ambience"
    load_type = 1 if is_ambience else 0
    quality = "0.6" if is_ambience else "0.8"
    # The importer (ImportRuleSet) rewrites the meta on every import, so the file written here is what it would
    # write: SFX outside a /2D/ folder is forced mono (the R1 UI cues sit in SFX/R1/ too), an SFX loop is PCM so
    # Vorbis padding cannot break its seam, ambience keeps its channels.
    force_mono = 0 if is_ambience else 1
    compression = 0 if not is_ambience and Path(rel).stem.endswith("_loop") else 1
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "AudioImporter:\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 8\n"
        "  defaultSettings:\n"
        "    serializedVersion: 2\n"
        f"    loadType: {load_type}\n"
        "    sampleRateSetting: 0\n"
        "    sampleRateOverride: 44100\n"
        f"    compressionFormat: {compression}\n"
        f"    quality: {quality}\n"
        "    conversionMode: 0\n"
        "    preloadAudioData: 1\n"
        "  platformSettingOverrides: {}\n"
        f"  forceToMono: {force_mono}\n"
        "  normalize: 1\n"
        "  loadInBackground: 0\n"
        "  ambisonic: 0\n"
        "  3D: 1\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def _read_meta_guid(asset_rel: str) -> str:
    meta_path = REPO_ROOT / f"{asset_rel}.meta"
    for line in meta_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid:"):
            return line.split(":", 1)[1].strip()
    raise ValueError(f"No guid in {meta_path}")


def _build_audio_library_yaml(all_cues: list[dict]) -> str:
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: 50de3fa9890fec145a1ee5677b9e5010, type: 3}",
        "  m_Name: AudioLibrary",
        "  m_EditorClassIdentifier: ",
        "  _cues:",
    ]

    for entry in all_cues:
        lines.append(f"  - _id: {entry['id']}")
        lines.append("    _clips:")
        for f_rel in entry["files"]:
            guid = _read_meta_guid(f_rel)
            lines.append(f"    - {{fileID: 8300000, guid: {guid}, type: 3}}")
        lines.append("    _variantLabels:")
        for label in entry["variantLabels"]:
            lines.append(f"    - {label}")
        lines.append(f"    _bus: {BUS_ENUM[entry['bus']]}")
        lines.append(f"    _spatial: {1 if entry['spatial'] else 0}")
        lines.append(f"    _loop: {1 if entry['loop'] else 0}")
        lines.append(f"    _volumeMin: {entry['volumeMin']:g}")
        lines.append(f"    _volumeMax: {entry['volumeMax']:g}")
        lines.append(f"    _pitchMin: {entry['pitchMin']:g}")
        lines.append(f"    _pitchMax: {entry['pitchMax']:g}")

    return "\n".join(lines) + "\n"


def export_r1(check_only: bool = False) -> tuple[list[dict], list[dict], list[str]]:
    """Render (or check) all 32 picked R1 cues and update (or check) ``sfx_manifest.json`` and ``AudioLibrary.asset``.

    Returns ``(r1_entries, metrics_rows, errors)``.
    """
    if not check_only:
        SFX_R1_DIR.mkdir(parents=True, exist_ok=True)
        AMBIENCE_R1_DIR.mkdir(parents=True, exist_ok=True)

    r1_entries: list[dict] = []
    metrics_rows: list[dict] = []
    errors: list[str] = []

    for folder_rel, folder_dir in ((SFX_R1_REL, SFX_R1_DIR), (AMBIENCE_R1_REL, AMBIENCE_R1_DIR)):
        folder_meta = folder_dir.parent / f"{folder_dir.name}.meta"
        expected_folder_meta = _folder_meta_text(folder_rel)
        if check_only:
            if not folder_meta.exists() or folder_meta.read_text(encoding="utf-8") != expected_folder_meta:
                errors.append(f"missing or stale folder meta: {folder_rel}.meta")
        else:
            folder_meta.write_text(expected_folder_meta, encoding="utf-8")

    for spec in CATALOG:
        signal = render_picked(spec)
        pcm = quantize(signal, 16)
        buf = io.BytesIO()
        sf.write(buf, pcm, core.SAMPLE_RATE, subtype="PCM_16", format="WAV")
        raw_wav = buf.getvalue()
        digest = hashlib.sha256(raw_wav).hexdigest()
        buf.seek(0)
        quantized, _ = sf.read(buf, dtype="float64", always_2d=False)

        m = analysis.analyze(quantized, loop=spec.loop, hf_cutoff=HF_CUTOFF_HZ, momentary_window=0.2)
        pitch_name, pitch_cents, in_key = dominant_pitch(quantized)
        tonal = bool(in_key and abs(pitch_cents) <= MAX_KEY_ERROR_CENTS)
        m["file"] = f"{spec.cue_id}.wav"
        m["tonal"] = tonal
        m["pitch"] = pitch_name
        m["pitch_cents"] = pitch_cents
        m["in_key"] = in_key

        loudness_mode = "integrated" if spec.loop else "momentary"
        gate_failures = evaluate(m, hf_max_db=HF_MAX_DB, target_lufs=spec.target_lufs, loudness_mode=loudness_mode)
        if gate_failures:
            errors.append(f"{spec.cue_id}: quality gate failure: {'; '.join(gate_failures)}")

        measured_lufs = float(m["lufs_integrated"] if spec.loop else m["lufs_momentary_max"])
        effective_db = measured_lufs + 20.0 * math.log10(spec.volume)

        target_path = cue_abs_path(spec)
        meta_path = REPO_ROOT / f"{cue_rel_path(spec)}.meta"
        expected_meta = _wav_meta_text(spec)
        if check_only:
            if not target_path.exists():
                errors.append(f"missing WAV: {cue_rel_path(spec)}")
            elif _file_sha256(target_path) != digest:
                errors.append(f"out-of-date WAV: {cue_rel_path(spec)}")
            if not meta_path.exists() or meta_path.read_text(encoding="utf-8") != expected_meta:
                errors.append(f"missing or stale WAV meta: {cue_rel_path(spec)}.meta")
        else:
            target_path.write_bytes(raw_wav)
            meta_path.write_text(expected_meta, encoding="utf-8")

        entry = build_r1_manifest_entry(spec, digest, tonal=tonal)
        r1_entries.append(entry)
        metrics_rows.append({
            "cue_id": spec.cue_id,
            "group_id": spec.group_id,
            "category": spec.category,
            "pick": spec.pick,
            "loop": spec.loop,
            "spatial": cue_spatial(spec),
            "bus": cue_bus(spec),
            "duration_s": float(m["duration"]),
            "target_lufs": spec.target_lufs,
            "measured_lufs": measured_lufs,
            "volume": spec.volume,
            "effective_db": effective_db,
            "peak_db": float(m["peak_db"]),
            "true_peak_db": float(m["true_peak_db"]),
            "hf_db": float(m["hf_db"]),
            "pitch": pitch_name,
            "pitch_cents": float(pitch_cents),
            "sha256": digest,
            "path": cue_rel_path(spec),
        })

    from build_sfx import manifest_text
    all_cues = base_manifest_entries() + r1_entries
    serialized = manifest_text(all_cues)

    if check_only:
        if not MANIFEST_PATH.exists():
            errors.append(f"missing manifest: {MANIFEST_PATH}")
        elif MANIFEST_PATH.read_text(encoding="utf-8") != serialized:
            errors.append("tools/audio/sfx_manifest.json is out of date; run tools/audio/r1/export_game.py")
    else:
        MANIFEST_PATH.write_text(serialized, encoding="utf-8")

    if not errors or not check_only:
        library_yaml = _build_audio_library_yaml(all_cues)
        if check_only:
            if not AUDIO_LIBRARY_PATH.exists() or AUDIO_LIBRARY_PATH.read_text(encoding="utf-8") != library_yaml:
                errors.append("Assets/_Project/Data/Audio/AudioLibrary.asset is out of date")
        else:
            AUDIO_LIBRARY_PATH.write_text(library_yaml, encoding="utf-8")

    return r1_entries, metrics_rows, errors


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="verify exported WAVs and sfx_manifest.json without writing")
    args = ap.parse_args()

    r1_entries, rows, errors = export_r1(check_only=args.check)
    if errors:
        for err in errors:
            print(f"FAIL: {err}", file=sys.stderr)
        return 1

    action = "Checked" if args.check else "Exported"
    print(f"{action} {len(r1_entries)} R1 cues ({sum(not r['loop'] for r in rows)} one-shots, "
          f"{sum(r['loop'] for r in rows)} loops).")
    print(f"{'cue_id':<28} {'var':>3} {'mode':>4} {'LUFS':>6} {'vol':>5} {'eff dB':>7}  path")
    for r in rows:
        mode = "loop" if r["loop"] else "1sht"
        print(f"{r['cue_id']:<28} {r['pick']:>3} {mode:>4} {r['measured_lufs']:6.1f} "
              f"{r['volume']:5.2f} {r['effective_db']:7.1f}  {r['path']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
