"""Unit tests for the voice pipeline (no TTS engine needed). Run: python -m unittest discover -s tools/voice/tests -v"""
import json
import struct
import sys
import unittest
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "audio"))
sys.path.insert(0, str(HERE.parent))

import build_voice  # noqa: E402
import onnx_seed  # noqa: E402
import radio_filter  # noqa: E402
from synth.analysis import loudness_integrated, true_peak_db  # noqa: E402
from synth.core import SAMPLE_RATE  # noqa: E402

REPO = HERE.parents[2]


def _field(number: int, payload: bytes) -> bytes:
    return onnx_seed._bytes_field(number, payload)


def _node(op: bytes, attributes=()) -> bytes:
    body = _field(4, op)
    for name in attributes:
        body += _field(5, _field(1, name))
    return body


def _model(*nodes) -> bytes:
    graph = b"".join(_field(1, n) for n in nodes) + _field(2, b"graph-name")
    return _field(1, b"\x07") + _field(7, graph) + _field(8, b"opset")


def _speech(seconds: float = 2.0) -> np.ndarray:
    """A voice-like test signal: a 140 Hz buzz with syllable-rate amplitude, padded with silence."""
    t = np.arange(int(seconds * SAMPLE_RATE)) / SAMPLE_RATE
    buzz = sum(np.sin(2 * np.pi * 140.0 * k * t) / k for k in range(1, 12))
    syllables = 0.5 + 0.5 * np.sin(2 * np.pi * 4.0 * t)
    pad = np.zeros(SAMPLE_RATE // 4)
    return np.concatenate((pad, 0.2 * buzz * syllables, pad))


class OnnxSeedTests(unittest.TestCase):
    def test_seeds_every_random_node_once_and_keeps_the_rest(self):
        model = _model(_node(b"Conv"), _node(b"RandomNormalLike", [b"dtype"]), _node(b"RandomNormalLike"))
        self.assertEqual([("RandomNormalLike", False)] * 2, onnx_seed.random_ops(model))

        seeded = onnx_seed.seed_model(model, 7.0)
        self.assertEqual([("RandomNormalLike", True)] * 2, onnx_seed.random_ops(seeded))
        self.assertEqual(seeded, onnx_seed.seed_model(seeded, 7.0), "an already seeded node is left alone")
        self.assertIn(struct.pack("<f", 7.0), seeded)
        fields = [f for f, *_ in onnx_seed.fields(seeded)]
        self.assertEqual([1, 7, 8], fields, "the model's other fields survive in order")

    def test_different_seeds_give_different_models(self):
        model = _model(_node(b"RandomNormalLike"))
        self.assertNotEqual(onnx_seed.seed_model(model, 1.0), onnx_seed.seed_model(model, 2.0))


class TreatmentTests(unittest.TestCase):
    def test_each_treatment_is_deterministic_loud_enough_and_never_hot(self):
        for treatment in radio_filter.TREATMENTS.values():
            with self.subTest(treatment.name):
                first, start, length = radio_filter.treat(_speech(), treatment, "test.line")
                second, _, _ = radio_filter.treat(_speech(), treatment, "test.line")
                self.assertTrue(np.array_equal(first, second))
                self.assertEqual(0.0, first[0])
                self.assertEqual(0.0, first[-1])
                self.assertLessEqual(true_peak_db(first), build_voice.TRUE_PEAK_LIMIT_DB)
                self.assertAlmostEqual(treatment.speech_lufs, loudness_integrated(first), delta=1.5)
                self.assertAlmostEqual(treatment.lead_s, start, places=3)
                self.assertGreater(length, 1.5)

    def test_the_radio_band_is_narrow_and_the_tape_keeps_its_top_soft(self):
        for treatment, ceiling_hz in ((radio_filter.RADIO, 5000.0), (radio_filter.TAPE, 9000.0)):
            with self.subTest(treatment.name):
                clip, _, _ = radio_filter.treat(_speech(), treatment, "test.line")
                spectrum = np.abs(np.fft.rfft(clip)) ** 2
                freqs = np.fft.rfftfreq(clip.shape[0], 1.0 / SAMPLE_RATE)
                above = spectrum[freqs > ceiling_hz].sum() / spectrum.sum()
                self.assertLess(above, 1e-3, f"little energy above {ceiling_hz} Hz: warm, never harsh")

    def test_resampling_follows_tape_speed(self):
        take = np.zeros(22050)
        self.assertEqual(SAMPLE_RATE, radio_filter.to_project_rate(take, 22050, 0.0).shape[0])
        lower = radio_filter.to_project_rate(take, 22050, -12.0).shape[0]
        self.assertEqual(2 * SAMPLE_RATE, lower, "an octave down plays twice as long")


class DataTests(unittest.TestCase):
    def setUp(self):
        self.lines = build_voice.load_json(build_voice.LINES)
        self.voices = build_voice.load_json(build_voice.VOICES)

    def test_lines_and_voices_validate(self):
        self.assertEqual([], build_voice.validate(self.lines, self.voices))

    def test_every_voiced_line_has_its_subtitle_in_both_languages(self):
        for language in ("en", "vi"):
            path = REPO / "Assets" / "_Project" / "Data" / "Localization" / f"{language}.json"
            keys = {e["key"] for e in json.loads(path.read_text(encoding="utf-8"))["entries"]}
            for line in self.lines["lines"]:
                self.assertIn(line["key"], keys, f"{language}: no subtitle for {line['key']}")

    def test_the_manifest_lists_every_line_with_its_clip(self):
        manifest = build_voice.load_json(build_voice.MANIFEST)
        self.assertEqual(build_voice.MANIFEST_VERSION, manifest["version"])
        self.assertEqual([l["key"] for l in self.lines["lines"]], [e["key"] for e in manifest["lines"]])
        for entry in manifest["lines"]:
            self.assertEqual(build_voice.clip_file(entry["key"]), entry["file"])
            self.assertTrue((REPO / entry["file"]).is_file(), entry["file"])

    def test_licences_allow_a_commercial_release_and_credits_are_kept(self):
        allowed = ("Public domain", "CC BY 4.0", "Apache-2.0")
        self.assertTrue(self.voices["engine"]["license"].startswith(allowed))
        for name, model in self.voices["models"].items():
            self.assertTrue(model["license"].startswith(allowed), name)
            if model["license"].startswith("CC BY"):
                self.assertTrue(model["credit"], f"{name} needs its attribution line")
                self.assertIn(model["credit"], build_voice.load_json(build_voice.MANIFEST)["credits"])


if __name__ == "__main__":
    unittest.main()
