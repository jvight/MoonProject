"""Unit tests for the AUDIO-AUDITION Round 2 Old Radio refinements (C, C1, C2, C3) and Round 1 modules."""
import ast
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy.signal import welch

AUDIO_TOOLS_DIR = Path(__file__).resolve().parents[1]
REPO_ROOT = AUDIO_TOOLS_DIR.parents[1]
if str(AUDIO_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(AUDIO_TOOLS_DIR))

import analyze_audio  # noqa: E402
from audition import (  # noqa: E402
    AUDITION_CUES,
    DIRECTIONS,
    ROUND1_DIRECTIONS,
    render_all,
    workshop,
)
from audition.render_audition import (  # noqa: E402
    MAX_AUDITION_PEAK_DB,
    MAX_FILE_BYTES,
    MAX_TOTAL_BYTES,
    ROUND1_OUT_DIR,
)
from synth.core import SAMPLE_RATE, rng  # noqa: E402


class TestAuditionDirections(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls._tmp = tempfile.TemporaryDirectory(prefix="lunar_audition2_test_")
        cls.out_dir = Path(cls._tmp.name)
        cls.summary = render_all(cls.out_dir, round_num=2)

    @classmethod
    def tearDownClass(cls):
        cls._tmp.cleanup()

    def test_cue_count_and_metadata(self):
        self.assertEqual(len(AUDITION_CUES), 14)
        ui_cues = [c for c in AUDITION_CUES if c.group == "UI"]
        rover_cues = [c for c in AUDITION_CUES if c.group == "07"]
        self.assertEqual(len(ui_cues), 8)
        self.assertEqual(len(rover_cues), 6)
        self.assertEqual(set(DIRECTIONS.keys()), {"C", "C1", "C2", "C3"})
        self.assertEqual(set(ROUND1_DIRECTIONS.keys()), {"A", "B", "C", "current"})

    def test_workshop_uses_no_pure_oscillators(self):
        source = Path(workshop.__file__).read_text(encoding="utf-8")
        tree = ast.parse(source)
        forbidden = {"sine", "square", "saw", "triangle", "additive"}
        for node in ast.walk(tree):
            if isinstance(node, ast.Call):
                if isinstance(node.func, ast.Attribute):
                    self.assertNotIn(
                        node.func.attr,
                        forbidden,
                        f"Direction A (workshop.py) must not call pure oscillator '{node.func.attr}'",
                    )
                elif isinstance(node.func, ast.Name):
                    self.assertNotIn(
                        node.func.id,
                        forbidden,
                        f"Direction A (workshop.py) must not call pure oscillator '{node.func.id}'",
                    )

    def test_determinism(self):
        ac = AUDITION_CUES[0]
        for direction in ("C", "C1", "C2", "C3"):
            s1 = ac.renders[direction](rng(f"det_{direction}", 0))
            s2 = ac.renders[direction](rng(f"det_{direction}", 0))
            np.testing.assert_array_equal(s1, s2)

    def test_c_matches_round1_reference(self):
        if not (ROUND1_OUT_DIR / "C").exists():
            self.skipTest("Round 1 Logs/audition/C not present")
        for ac in AUDITION_CUES:
            r1 = (ROUND1_OUT_DIR / "C" / f"{ac.id}.wav").read_bytes()
            r2 = (self.out_dir / "C" / f"{ac.id}.wav").read_bytes()
            self.assertEqual(r1, r2, f"C/{ac.id}.wav differs from Round 1 C reference")

    def test_wav_format_size_peak_and_quality_gates(self):
        self.assertLess(self.summary["total_bytes"], MAX_TOTAL_BYTES)
        for direction in ("C", "C1", "C2", "C3"):
            for ac in AUDITION_CUES:
                wav_path = self.out_dir / direction / f"{ac.id}.wav"
                self.assertTrue(wav_path.exists(), f"Missing {wav_path}")
                size_bytes = wav_path.stat().st_size
                self.assertLess(size_bytes, MAX_FILE_BYTES, f"{wav_path} exceeds 200 KB ({size_bytes} B)")
                self.assertLess(size_bytes, 200_000, f"{wav_path} exceeds 200,000 B ({size_bytes} B)")

                info = sf.info(str(wav_path))
                self.assertEqual(info.samplerate, SAMPLE_RATE)
                self.assertEqual(info.channels, 1)
                self.assertEqual(info.subtype, "PCM_16")

                m = self.summary["metrics"][direction][ac.id]
                self.assertEqual(m["failures"], [], f"{direction}/{ac.id}.wav failed gates: {m['failures']}")
                self.assertLessEqual(
                    m["peak_db"],
                    MAX_AUDITION_PEAK_DB,
                    f"{direction}/{ac.id}.wav peak {m['peak_db']:.2f} dBFS > {MAX_AUDITION_PEAK_DB}",
                )
                self.assertLessEqual(
                    m["true_peak_db"],
                    MAX_AUDITION_PEAK_DB,
                    f"{direction}/{ac.id}.wav true peak {m['true_peak_db']:.2f} dBTP > {MAX_AUDITION_PEAK_DB}",
                )
                self.assertTrue(m["in_key"], f"{direction}/{ac.id}.wav pitch {m['pitch']} not in D pentatonic")
                self.assertLessEqual(
                    abs(m["pitch_cents"]),
                    analyze_audio.MAX_KEY_ERROR_CENTS,
                    f"{direction}/{ac.id}.wav pitch error {m['pitch_cents']:.1f}c > 20c",
                )

                # Verify no clicks/pops: zero boundary samples and smooth sample-to-sample derivative.
                sig, _ = sf.read(str(wav_path))
                self.assertEqual(float(sig[0]), 0.0)
                self.assertEqual(float(sig[-1]), 0.0)
                max_step = float(np.max(np.abs(np.diff(sig))))
                self.assertLess(max_step, 0.15, f"{direction}/{ac.id}.wav has steep step jump {max_step:.3f}")

    def test_variants_are_clearly_distinct(self):
        centroids = {
            d: [self.summary["metrics"][d][c.id]["centroid_hz"] for c in AUDITION_CUES]
            for d in ("C", "C1", "C2", "C3")
        }
        chirp_centroids = {
            d: [self.summary["metrics"][d][c.id]["centroid_hz"] for c in AUDITION_CUES if c.group == "07"]
            for d in ("C", "C1", "C2", "C3")
        }

        # C1 ("Warm receiver") is significantly warmer/darker with a fuller low-mid body and longer tails than C.
        self.assertLess(float(np.mean(centroids["C1"])), float(np.mean(centroids["C"])) - 150.0)
        for ac in AUDITION_CUES:
            self.assertGreater(
                self.summary["metrics"]["C1"][ac.id]["duration"],
                self.summary["metrics"]["C"][ac.id]["duration"],
                f"C1/{ac.id}.wav should have a longer gentle room tail than C",
            )

        # C2 ("Radio bench") vocal-formant chirps have higher formant presence than C and C1.
        self.assertGreater(float(np.mean(chirp_centroids["C2"])), float(np.mean(chirp_centroids["C"])) + 100.0)

        # C3 ("Lullaby radio") celesta/music-box phrases have the highest harmonic overtone centroid.
        self.assertGreater(float(np.mean(centroids["C3"])), float(np.mean(centroids["C2"])) + 80.0)
        self.assertGreater(float(np.mean(centroids["C3"])), float(np.mean(centroids["C"])) + 100.0)

        # Measure low-mid (100..350 Hz) energy share to verify C1's fuller low-mid body.
        low_mid_shares = {d: [] for d in ("C", "C1", "C2", "C3")}
        for d in ("C", "C1", "C2", "C3"):
            for ac in AUDITION_CUES:
                sig, _ = sf.read(str(self.out_dir / d / f"{ac.id}.wav"))
                f, psd = welch(sig, fs=SAMPLE_RATE, nperseg=min(4096, len(sig)))
                low_mid_shares[d].append(float(psd[(f >= 100.0) & (f < 350.0)].sum() / psd[1:].sum()))
        self.assertGreater(float(np.mean(low_mid_shares["C1"])), float(np.mean(low_mid_shares["C"])) + 0.12)

        # Verify pairwise waveform distinctness for every cue across all 4 variants.
        variants = ("C", "C1", "C2", "C3")
        for ac in AUDITION_CUES:
            sigs = {d: sf.read(str(self.out_dir / d / f"{ac.id}.wav"))[0] for d in variants}
            for i, d1 in enumerate(variants):
                for d2 in variants[i + 1:]:
                    n = min(len(sigs[d1]), len(sigs[d2]))
                    self.assertFalse(
                        np.allclose(sigs[d1][:n], sigs[d2][:n]),
                        f"{d1}/{ac.id}.wav and {d2}/{ac.id}.wav are not distinct",
                    )

    def test_index_json_structure(self):
        index_path = self.out_dir / "index.json"
        self.assertTrue(index_path.exists())
        data = json.loads(index_path.read_text(encoding="utf-8"))
        self.assertEqual(data["sample_rate_hz"], SAMPLE_RATE)
        self.assertEqual(data["bits_per_sample"], 16)
        self.assertEqual(data["channels"], 1)
        self.assertEqual(len(data["cues"]), 14)

        for entry in data["cues"]:
            self.assertIn("cue", entry)
            for key in ("C", "C1", "C2", "C3"):
                self.assertIsInstance(entry[key], str)
                self.assertGreater(len(entry[key].strip()), 15)
                self.assertNotIn("\n", entry[key].strip())
                rel_path = entry["files"][key]
                self.assertTrue((self.out_dir / rel_path).exists())


if __name__ == "__main__":
    unittest.main()
