"""Unit tests for the R1 Audio Audition Set (`tools/audio/r1/` and `Logs/audition/`).

Run:
    python -m unittest discover -s tools/audio/tests -v
"""

from __future__ import annotations

import json
from pathlib import Path
import sys
import tempfile
import unittest

import numpy as np

_REPO_ROOT = Path(__file__).resolve().parents[3]
_AUDIO_ROOT = _REPO_ROOT / "tools" / "audio"
for _p in (str(_REPO_ROOT), str(_AUDIO_ROOT)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from synth import core, io as synth_io  # noqa: E402
from tools.audio.r1.build_audition import (  # noqa: E402
    DEFAULT_OUT_DIR,
    build_all,
    condition_and_normalize,
)
from tools.audio.r1.catalog import (  # noqa: E402
    CATALOG,
    CATALOG_BY_ID,
    GROUPS,
    GUIDE_MAX_EFFECTIVE_DB,
    GUIDE_MAX_TARGET_LUFS,
)


EXPECTED_CUES_BY_GROUP: dict[str, tuple[str, ...]] = {
    "face": (
        "face_boot",
        "face_blink",
        "face_happy",
        "face_worried",
        "face_asleep",
        "face_startled",
        "face_proud",
    ),
    "sky": (
        "sky_diamond_ring",
        "sky_dawn_sweep",
        "sky_nightfall_settle",
    ),
    "building": (
        "build_ghost",
        "build_snap",
        "build_invalid",
        "build_place",
        "build_weld_loop",
        "build_stage_complete",
        "build_structure_complete",
        "build_deconstruct",
    ),
    "machines": (
        "machine_solar_track",
        "machine_wings_unfold",
        "machine_wings_fold",
        "machine_pad_contacts",
        "machine_transformer_loop",
        "machine_pylon_switch",
        "machine_cable_drums",
        "machine_lander_ramp",
        "machine_cargo_hatch",
        "machine_crate_lid",
    ),
    "ui": (
        "ui_panel_open",
        "ui_panel_close",
        "ui_cursor_move",
        "ui_confirm",
        "ui_cancel",
        "ui_toast",
        "ui_type_card",
        "ui_type_terminal",
    ),
    "ambience": (
        "ambience_earthlit_night",
        "ambience_sunlit_day",
        "ambience_eclipse",
    ),
}


class R1AuditionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        manifest_path = DEFAULT_OUT_DIR / "manifest.json"
        if not manifest_path.exists():
            build_all(out_dir=DEFAULT_OUT_DIR, verbose=False)
        cls.manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

    def test_catalog_covers_all_required_groups_and_cues(self) -> None:
        group_ids = [gid for gid, _, _ in GROUPS]
        self.assertEqual(group_ids, list(EXPECTED_CUES_BY_GROUP.keys()))

        total_expected = sum(len(v) for v in EXPECTED_CUES_BY_GROUP.values())
        self.assertEqual(len(CATALOG), total_expected)

        for gid, expected_ids in EXPECTED_CUES_BY_GROUP.items():
            actual_ids = tuple(s.cue_id for s in CATALOG if s.group_id == gid)
            self.assertEqual(actual_ids, expected_ids, msg=f"Group {gid} mismatch")

    def test_vietnamese_descriptions_and_picks_present(self) -> None:
        for spec in CATALOG:
            self.assertTrue(spec.desc_a_vi.strip(), msg=f"{spec.cue_id} missing desc_a_vi")
            self.assertTrue(spec.desc_b_vi.strip(), msg=f"{spec.cue_id} missing desc_b_vi")
            self.assertNotIn("\n", spec.desc_a_vi, msg=f"{spec.cue_id} desc_a_vi must be one line")
            self.assertNotIn("\n", spec.desc_b_vi, msg=f"{spec.cue_id} desc_b_vi must be one line")
            self.assertIn(spec.pick, ("A", "B"), msg=f"{spec.cue_id} invalid pick")
            self.assertTrue(spec.pick_reason.strip(), msg=f"{spec.cue_id} missing pick_reason")

    def test_loudness_targets_respect_guide_and_ui_is_quietest(self) -> None:
        ui_specs = [s for s in CATALOG if s.group_id == "ui"]
        non_ui_specs = [s for s in CATALOG if s.group_id != "ui"]

        for spec in CATALOG:
            self.assertLessEqual(
                spec.target_lufs,
                GUIDE_MAX_TARGET_LUFS,
                msg=f"{spec.cue_id} target_lufs {spec.target_lufs} > {GUIDE_MAX_TARGET_LUFS}",
            )
            self.assertLessEqual(
                spec.effective_db,
                GUIDE_MAX_EFFECTIVE_DB,
                msg=f"{spec.cue_id} effective_db {spec.effective_db:.2f} > {GUIDE_MAX_EFFECTIVE_DB}",
            )

        max_ui_target = max(s.target_lufs for s in ui_specs)
        min_non_ui_target = min(s.target_lufs for s in non_ui_specs)
        self.assertLess(
            max_ui_target,
            min_non_ui_target,
            msg=f"UI target LUFS ({max_ui_target}) must be strictly quieter than non-UI ({min_non_ui_target})",
        )

        max_ui_eff = max(s.effective_db for s in ui_specs)
        min_non_ui_eff = min(s.effective_db for s in non_ui_specs)
        self.assertLess(
            max_ui_eff,
            min_non_ui_eff,
            msg=f"UI effective dB ({max_ui_eff:.2f}) must be strictly quieter than non-UI ({min_non_ui_eff:.2f})",
        )

    def test_seeded_determinism_and_variant_distinctness(self) -> None:
        sample_ids = (
            "face_boot",
            "sky_diamond_ring",
            "build_place",
            "build_weld_loop",
            "machine_transformer_loop",
            "ui_cursor_move",
            "ui_type_card",
        )
        with tempfile.TemporaryDirectory() as tmp:
            tmp_dir = Path(tmp)
            for cid in sample_ids:
                spec = CATALOG_BY_ID[cid]
                a1 = condition_and_normalize(spec.fn(0, core.rng("r1", cid, "A")), spec)
                a2 = condition_and_normalize(spec.fn(0, core.rng("r1", cid, "A")), spec)
                b1 = condition_and_normalize(spec.fn(1, core.rng("r1", cid, "B")), spec)

                np.testing.assert_array_equal(a1, a2, err_msg=f"{cid} Variant A is not bit-identical")
                self.assertFalse(
                    np.array_equal(a1, b1),
                    msg=f"{cid} Variant A and B should be distinct",
                )

                wav1 = tmp_dir / f"{cid}_1.wav"
                wav2 = tmp_dir / f"{cid}_2.wav"
                wavb = tmp_dir / f"{cid}_b.wav"
                synth_io.write_wav(wav1, a1)
                synth_io.write_wav(wav2, a2)
                synth_io.write_wav(wavb, b1)
                self.assertEqual(wav1.read_bytes(), wav2.read_bytes(), msg=f"{cid} WAV bytes differ")
                self.assertEqual(synth_io.audio_digest(wav1), synth_io.audio_digest(wav2))
                self.assertNotEqual(synth_io.audio_digest(wav1), synth_io.audio_digest(wavb))

    def test_rendered_audition_files_and_quality_gates(self) -> None:
        self.assertEqual(self.manifest["failures"], [])
        self.assertEqual(self.manifest["cue_count"], len(CATALOG))
        self.assertLess(self.manifest["max_ui_lufs"], self.manifest["min_non_ui_lufs"])
        self.assertLess(self.manifest["max_ui_effective_db"], self.manifest["min_non_ui_effective_db"])

        for entry in self.manifest["cues"]:
            cid = entry["cue_id"]
            wav_a = DEFAULT_OUT_DIR / "A" / f"{cid}.wav"
            wav_b = DEFAULT_OUT_DIR / "B" / f"{cid}.wav"
            wav_root = DEFAULT_OUT_DIR / f"{cid}.wav"
            self.assertTrue(wav_a.exists(), msg=f"Missing {wav_a}")
            self.assertTrue(wav_b.exists(), msg=f"Missing {wav_b}")
            self.assertTrue(wav_root.exists(), msg=f"Missing {wav_root}")

            for var in ("A", "B"):
                rep = entry[var]
                self.assertTrue(rep["ok"], msg=f"{cid} [{var}] failed gates: {rep['issues']}")
                self.assertTrue(rep["in_key"], msg=f"{cid} [{var}] pitch {rep['pitch']} not in D major pentatonic")
                self.assertLessEqual(abs(rep["pitch_cents"]), 20.0, msg=f"{cid} [{var}] pitch cents > 20")

            if cid == "sky_dawn_sweep":
                self.assertAlmostEqual(entry["A"]["duration_s"], 6.0, delta=0.5)
                self.assertAlmostEqual(entry["B"]["duration_s"], 6.0, delta=0.5)
            elif entry["group_id"] == "ambience":
                self.assertAlmostEqual(entry["A"]["duration_s"], 60.0, delta=0.05)
                self.assertAlmostEqual(entry["B"]["duration_s"], 60.0, delta=0.05)

    def test_index_html_contains_all_cues_players_and_picks(self) -> None:
        import html

        index_path = DEFAULT_OUT_DIR / "index.html"
        self.assertTrue(index_path.exists())
        content = index_path.read_text(encoding="utf-8")

        for spec in CATALOG:
            self.assertIn(spec.cue_id, content)
            self.assertIn(f'src="A/{spec.cue_id}.wav"', content)
            self.assertIn(f'src="B/{spec.cue_id}.wav"', content)
            self.assertIn(html.escape(spec.desc_a_vi), content)
            self.assertIn(html.escape(spec.desc_b_vi), content)
            self.assertIn(html.escape(spec.pick_reason), content)


if __name__ == "__main__":
    unittest.main()
