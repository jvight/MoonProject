"""
tools/look_report.py metrics on synthetic frames. Run: python -m unittest discover -s tools/tests -t tools
"""
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

import look_report

WIDTH, HEIGHT = 200, 100


def save(folder, name, rgb):
    path = Path(folder) / f"{name}.png"
    Image.fromarray((np.clip(rgb, 0, 1) * 255).round().astype(np.uint8)).save(path)
    return path


def grey(value):
    return np.full((HEIGHT, WIDTH, 3), value, dtype=np.float64)


class MetricTests(unittest.TestCase):
    def test_a_flat_frame_has_no_spread_and_full_flatness(self):
        with tempfile.TemporaryDirectory() as tmp:
            m, _, _ = look_report.measure(save(tmp, "flat", grey(0.3)))
        self.assertAlmostEqual(m.spread, 0.0, places=2)
        self.assertAlmostEqual(m.flatness, 1.0)
        self.assertFalse(m.passes)

    def test_a_ramp_spreads_and_only_the_lower_45_percent_counts_for_flatness(self):
        ramp = np.repeat(np.linspace(0.0, 1.0, WIDTH)[None, :, None], HEIGHT, axis=0).repeat(3, axis=2)
        frame = ramp.copy()
        frame[:int(HEIGHT * 0.55), :, :] = 0.5
        with tempfile.TemporaryDirectory() as tmp:
            m, _, _ = look_report.measure(save(tmp, "ramp", frame))
        self.assertGreater(m.spread, 0.40)
        self.assertLess(m.flatness, 0.15)
        self.assertTrue(m.passes)

    def test_the_brightest_percent_is_classified_and_located(self):
        frame = grey(0.2)
        frame[5:10, 150:190] = (1.0, 0.7, 0.3)
        with tempfile.TemporaryDirectory() as tmp:
            m, rgb, mask = look_report.measure(save(tmp, "lamp", frame))
        self.assertGreater(m.top["warm"], 0.99)
        self.assertGreater(m.top_centre[0], 0.7)
        self.assertLess(m.top_centre[1], 0.15)
        self.assertEqual(mask.shape, rgb.shape[:2])

    def test_categories_of_typical_colours(self):
        colours = np.array([[(1.0, 0.75, 0.35), (0.95, 0.9, 0.82), (0.55, 0.6, 0.95), (0.7, 0.7, 0.72),
                             (0.46, 0.45, 0.7), (1.0, 0.2, 0.25)]])
        kinds = [look_report.CATEGORIES[k] for k in look_report.classify(colours)[0]]
        self.assertEqual(kinds, ["warm", "cream", "cool", "neutral", "cool", "warm"])


class ReportAndDiffTests(unittest.TestCase):
    def test_report_writes_markdown_and_masks_and_diff_pairs_frames(self):
        with tempfile.TemporaryDirectory() as tmp:
            before, after = Path(tmp, "before"), Path(tmp, "after")
            before.mkdir()
            after.mkdir()
            save(before, "owner", grey(0.3))
            save(after, "owner", np.random.default_rng(1).random((HEIGHT, WIDTH, 3)))
            save(after, "title", grey(0.1))
            look_report.report(after)
            self.assertTrue((after / "report" / "look.md").exists())
            self.assertTrue((after / "report" / "owner-top1.png").exists())
            look_report.diff(before, after)
            text = (after / "report" / "diff.md").read_text(encoding="utf-8")
            self.assertIn("| owner | 0.00 |", text)
            self.assertIn("| title | missing | present |", text)


if __name__ == "__main__":
    unittest.main()
