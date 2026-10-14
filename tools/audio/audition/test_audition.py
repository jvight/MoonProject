"""Unit tests for the AUDIO-AUDITION synthesis directions (A, B, C) and comparison export."""
import ast
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
import soundfile as sf

AUDIO_TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(AUDIO_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(AUDIO_TOOLS_DIR))

import analyze_audio  # noqa: E402
from audition import (  # noqa: E402
    AUDITION_CUES,
    DIRECTIONS,
    render_all,
    workshop,
)
from audition.render_audition import MAX_FILE_BYTES, MAX_TOTAL_BYTES  # noqa: E402
from synth.core import SAMPLE_RATE, rng  # noqa: E402


class TestAuditionDirections(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls._tmp = tempfile.TemporaryDirectory(prefix="lunar_audition_test_")
        cls.out_dir = Path(cls._tmp.name)
        cls.summary = render_all(cls.out_dir)

    @classmethod
    def tearDownClass(cls):
        cls._tmp.cleanup()

    def test_cue_count_and_metadata(self):
        self.assertEqual(len(AUDITION_CUES), 14)
        ui_cues = [c for c in AUDITION_CUES if c.group == "UI"]
        rover_cues = [c for c in AUDITION_CUES if c.group == "07"]
        self.assertEqual(len(ui_cues), 8)
        self.assertEqual(len(rover_cues), 6)
        self.assertEqual(set(DIRECTIONS.keys()), {"A", "B", "C", "current"})

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
        for direction in ("A", "B", "C"):
            s1 = ac.renders[direction](rng(f"det_{direction}", 0))
            s2 = ac.renders[direction](rng(f"det_{direction}", 0))
            np.testing.assert_array_equal(s1, s2)

    def test_wav_format_size_and_quality_gates(self):
        self.assertLess(self.summary["total_bytes"], MAX_TOTAL_BYTES)
        for direction in ("A", "B", "C", "current"):
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

                if direction in ("A", "B", "C"):
                    self.assertTrue(m["in_key"], f"{direction}/{ac.id}.wav pitch {m['pitch']} not in D pentatonic")
                    self.assertLessEqual(
                        abs(m["pitch_cents"]),
                        analyze_audio.MAX_KEY_ERROR_CENTS,
                        f"{direction}/{ac.id}.wav pitch error {m['pitch_cents']:.1f}c > 20c",
                    )

    def test_directions_are_timbrally_distinct(self):
        centroids_a = [self.summary["metrics"]["A"][c.id]["centroid_hz"] for c in AUDITION_CUES]
        centroids_b = [self.summary["metrics"]["B"][c.id]["centroid_hz"] for c in AUDITION_CUES]
        centroids_c = [self.summary["metrics"]["C"][c.id]["centroid_hz"] for c in AUDITION_CUES]

        # Workshop (metal/mechanical) is brighter than Wood & Felt, which is brighter than band-limited Old Radio.
        self.assertGreater(float(np.mean(centroids_a)), float(np.mean(centroids_b)) + 150.0)
        self.assertGreater(float(np.mean(centroids_b)), float(np.mean(centroids_c)) + 80.0)

        # Verify waveforms for the same cue differ across directions.
        for ac in AUDITION_CUES:
            a_sig, _ = sf.read(str(self.out_dir / "A" / f"{ac.id}.wav"))
            b_sig, _ = sf.read(str(self.out_dir / "B" / f"{ac.id}.wav"))
            c_sig, _ = sf.read(str(self.out_dir / "C" / f"{ac.id}.wav"))
            n = min(len(a_sig), len(b_sig), len(c_sig))
            self.assertFalse(np.allclose(a_sig[:n], b_sig[:n]))
            self.assertFalse(np.allclose(b_sig[:n], c_sig[:n]))
            self.assertFalse(np.allclose(a_sig[:n], c_sig[:n]))

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
            for key in ("A", "B", "C", "current"):
                self.assertIsInstance(entry[key], str)
                self.assertGreater(len(entry[key].strip()), 15)
                self.assertNotIn("\n", entry[key].strip())
                rel_path = entry["files"][key]
                self.assertTrue((self.out_dir / rel_path).exists())


if __name__ == "__main__":
    unittest.main()
