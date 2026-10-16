"""Unit tests for the R1 game export script (tools/audio/r1/export_game.py)."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import sys
import unittest

_REPO_ROOT = Path(__file__).resolve().parents[3]
_AUDIO_ROOT = _REPO_ROOT / "tools" / "audio"
for _p in (str(_REPO_ROOT), str(_AUDIO_ROOT)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from r1.catalog import CATALOG, GUIDE_MAX_EFFECTIVE_DB, GUIDE_MAX_TARGET_LUFS  # noqa: E402
from r1.export_game import (  # noqa: E402
    AMBIENCE_R1_DIR,
    MANIFEST_PATH,
    SFX_R1_DIR,
    cue_abs_path,
    cue_bus,
    cue_rel_path,
    cue_spatial,
    export_r1,
)

REMOVED_CUE_IDS = frozenset({
    "ambience_bed",
    "ui_menu_open",
    "ui_menu_close",
    "ui_focus",
    "ui_slider",
    "ui_prompt",
    "ui_card",
    "ui_back",
})

REMOVED_WAV_PATHS = (
    "Assets/_Project/Audio/Ambience/ambience_bed_loop.wav",
    "Assets/_Project/Audio/SFX/2D/ui_menu_open.wav",
    "Assets/_Project/Audio/SFX/2D/ui_menu_close.wav",
    "Assets/_Project/Audio/SFX/2D/ui_focus_A4.wav",
    "Assets/_Project/Audio/SFX/2D/ui_focus_B4.wav",
    "Assets/_Project/Audio/SFX/2D/ui_focus_D5.wav",
    "Assets/_Project/Audio/SFX/2D/ui_slider_D5.wav",
    "Assets/_Project/Audio/SFX/2D/ui_slider_E5.wav",
    "Assets/_Project/Audio/SFX/2D/ui_prompt.wav",
    "Assets/_Project/Audio/SFX/2D/ui_card.wav",
    "Assets/_Project/Audio/SFX/2D/ui_confirm.wav",
    "Assets/_Project/Audio/SFX/2D/ui_back.wav",
)


class R1ExportGameTest(unittest.TestCase):
    """Verify deterministic R1 WAV export, manifest registration, loudness hierarchy, and cleanup."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.r1_entries, cls.rows, cls.errors = export_r1(check_only=True)
        cls.manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
        cls.manifest_by_id = {c["id"]: c for c in cls.manifest["cues"]}

    def test_check_passes_with_zero_errors(self) -> None:
        self.assertEqual(self.errors, [], f"export_r1(--check) reported errors: {self.errors}")
        self.assertEqual(len(self.r1_entries), 39)
        self.assertEqual(len(self.rows), 39)

    def test_exported_wav_locations_and_hashes(self) -> None:
        sfx_wavs = sorted(p.name for p in SFX_R1_DIR.glob("*.wav"))
        amb_wavs = sorted(p.name for p in AMBIENCE_R1_DIR.glob("*.wav"))
        self.assertEqual(len(sfx_wavs), 36, "Expected 36 SFX WAVs in Assets/_Project/Audio/SFX/R1/")
        self.assertEqual(len(amb_wavs), 3, "Expected 3 Ambience WAVs in Assets/_Project/Audio/Ambience/R1/")

        for spec in CATALOG:
            path = cue_abs_path(spec)
            self.assertTrue(path.is_file(), f"Missing exported WAV for {spec.cue_id}: {path}")
            entry = self.manifest_by_id.get(spec.cue_id)
            self.assertIsNotNone(entry, f"{spec.cue_id} missing from sfx_manifest.json")
            self.assertEqual(entry["files"], [cue_rel_path(spec)])
            disk_sha = hashlib.sha256(path.read_bytes()).hexdigest()
            self.assertEqual(entry["sha256"], [disk_sha], f"SHA-256 mismatch for {spec.cue_id}")

    def test_manifest_registration_matches_catalog_specs(self) -> None:
        for spec in CATALOG:
            entry = self.manifest_by_id[spec.cue_id]
            self.assertEqual(entry["milestone"], "R1")
            self.assertEqual(entry["category"], spec.category)
            self.assertEqual(entry["loop"], spec.loop)
            self.assertEqual(entry["bus"], cue_bus(spec))
            self.assertEqual(entry["spatial"], cue_spatial(spec))
            self.assertAlmostEqual(entry["volumeMin"], spec.volume, places=6)
            self.assertAlmostEqual(entry["volumeMax"], spec.volume, places=6)
            self.assertAlmostEqual(entry["targetLufs"], spec.target_lufs, places=6)

    def test_loudness_hierarchy_and_guide_ceiling(self) -> None:
        by_group: dict[str, list[float]] = {}
        for row in self.rows:
            self.assertLessEqual(
                row["target_lufs"],
                GUIDE_MAX_TARGET_LUFS,
                f"{row['cue_id']} target_lufs {row['target_lufs']} exceeds {GUIDE_MAX_TARGET_LUFS}",
            )
            self.assertLessEqual(
                row["effective_db"],
                GUIDE_MAX_EFFECTIVE_DB + 0.35,
                f"{row['cue_id']} effective_db {row['effective_db']:.2f} exceeds guide ceiling",
            )
            by_group.setdefault(row["group_id"], []).append(row["effective_db"])

        ui_mean = sum(by_group["ui"]) / len(by_group["ui"])
        for group_id, vals in by_group.items():
            if group_id == "ui":
                continue
            other_mean = sum(vals) / len(vals)
            self.assertLess(
                ui_mean,
                other_mean,
                f"UI family ({ui_mean:.2f} dB) must be quieter than {group_id} ({other_mean:.2f} dB)",
            )

    def test_replaced_cues_and_wavs_removed(self) -> None:
        library_text = (_REPO_ROOT / "Assets" / "_Project" / "Data" / "Audio" / "AudioLibrary.asset").read_text(
            encoding="utf-8"
        )
        for removed_id in REMOVED_CUE_IDS:
            self.assertNotIn(removed_id, self.manifest_by_id, f"Replaced cue {removed_id} still in sfx_manifest.json")
            self.assertNotIn(f"_id: {removed_id}", library_text, f"Replaced cue {removed_id} still in AudioLibrary.asset")
        for spec in CATALOG:
            self.assertIn(f"_id: {spec.cue_id}", library_text, f"R1 cue {spec.cue_id} missing from AudioLibrary.asset")
            self.assertTrue((cue_abs_path(spec).with_suffix(".wav.meta")).is_file())
        for rel in REMOVED_WAV_PATHS:
            self.assertFalse((_REPO_ROOT / rel).exists(), f"Replaced WAV {rel} still exists on disk")
            self.assertFalse((_REPO_ROOT / f"{rel}.meta").exists(), f"Replaced meta {rel}.meta still exists on disk")


if __name__ == "__main__":
    unittest.main()
