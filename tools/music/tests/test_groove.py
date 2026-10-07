"""Swing grid, drum patterns and humanisation. Run: python -m unittest discover -s tools/music/tests -t tools/music"""
import unittest

import numpy as np
from lofi.groove import STEPS_PER_BAR, STYLES, bar_pattern, humanise, swung_beat


class SwingTests(unittest.TestCase):
    def test_straight_grid(self):
        for step in range(STEPS_PER_BAR):
            self.assertAlmostEqual(swung_beat(step, 0.5), step / 4.0)

    def test_swing_delays_only_the_second_sixteenth_of_each_pair(self):
        swing = 0.6
        for beat in range(4):
            self.assertAlmostEqual(swung_beat(4 * beat, swing), beat)
            self.assertAlmostEqual(swung_beat(4 * beat + 1, swing), beat + 0.5 * swing)
            self.assertAlmostEqual(swung_beat(4 * beat + 2, swing), beat + 0.5)
            self.assertAlmostEqual(swung_beat(4 * beat + 3, swing), beat + 0.5 + 0.5 * swing)

    def test_grid_stays_ordered_for_every_station_swing(self):
        for swing in (0.5, 0.55, 0.58, 0.62, 0.66):
            positions = [swung_beat(s, swing) for s in range(STEPS_PER_BAR)]
            self.assertEqual(positions, sorted(positions))
            self.assertLess(positions[-1], 4.0)


class PatternTests(unittest.TestCase):
    def test_full_bars_keep_the_backbeat(self):
        gen = np.random.default_rng(2974)
        for style in STYLES.values():
            for _ in range(20):
                pattern = bar_pattern(style, "full", None, gen)
                snares = {s for s, v, level in pattern if v == "snare" and level > 0.5}
                self.assertEqual(snares, set(style.snares[0]), style.name)
                self.assertIn((0, "kick", 1.0), pattern)

    def test_extra_layers_join_light_and_full_bars_only(self):
        gen = np.random.default_rng(4711)
        snaps = {s for s, v, _ in bar_pattern(STYLES["bounce"], "full", None, gen) if v == "snap"}
        self.assertEqual(snaps, {4, 12})
        swishes = {s for s, v, _ in bar_pattern(STYLES["jam"], "light", None, gen) if v == "swish"}
        self.assertEqual(swishes, {0, 4, 8, 12})
        sparse = bar_pattern(STYLES["jam"], "sparse", None, gen)
        self.assertFalse([h for h in sparse if h[1] == "swish"])

    def test_a_looser_drummer_spreads_wider(self):
        def spread(style_name):
            style = STYLES[style_name]
            gen = np.random.default_rng(2718)
            offsets = []
            for bar in range(64):
                hits = humanise(4.0 * bar, bar_pattern(style, "light", None, gen), style, 0.6, gen)
                offsets.extend(h.offset_s for h in hits if h.voice == "kick")
            return float(np.std(offsets))
        self.assertGreater(spread("jam"), 1.5 * spread("laidback"))

    def test_drop_fill_empties_the_second_half(self):
        pattern = bar_pattern(STYLES["boombap"], "full", "drop", np.random.default_rng(78539))
        late = [(s, v) for s, v, _ in pattern if s >= 8]
        self.assertEqual(late, [(15, "rim")])

    def test_patterns_are_deterministic(self):
        first = bar_pattern(STYLES["laidback"], "full", "roll", np.random.default_rng(33074))
        second = bar_pattern(STYLES["laidback"], "full", "roll", np.random.default_rng(33074))
        self.assertEqual(first, second)

    def test_humanise_keeps_hits_near_the_swung_grid(self):
        style = STYLES["laidback"]
        gen = np.random.default_rng(157)
        pattern = bar_pattern(style, "full", None, gen)
        hits = humanise(8.0, pattern, style, 0.58, gen)
        self.assertEqual(len(hits), len(pattern))
        for hit in hits:
            self.assertLess(abs(hit.offset_s), 0.045)
            self.assertTrue(0.05 <= hit.velocity <= 1.0)
            grid = [8.0 + swung_beat(s, 0.58) for s in range(STEPS_PER_BAR)]
            self.assertTrue(any(abs(hit.beat - g) < 1e-9 for g in grid))

    def test_snare_sits_behind_the_beat(self):
        style = STYLES["brushy"]
        gen = np.random.default_rng(38498)
        offsets = []
        for bar in range(64):
            hits = humanise(4.0 * bar, bar_pattern(style, "full", None, gen), style, 0.6, gen)
            offsets.extend(h.offset_s for h in hits if h.voice == "snare" and h.velocity > 0.5)
        mean_ms = 1000.0 * sum(offsets) / len(offsets)
        self.assertAlmostEqual(mean_ms, style.snare_lag_ms, delta=2.0)


if __name__ == "__main__":
    unittest.main()
