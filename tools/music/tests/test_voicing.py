"""Voicings and voice leading. Run: python -m unittest discover -s tools/music/tests -t tools/music"""
import unittest
from itertools import pairwise

import numpy as np
from lofi.harmony import FAMILIES, parse_phrase
from lofi.theory import B_MINOR, D_MAJOR, parse_chord
from lofi.voicing import KEYS_STYLE, PAD_STYLE, candidates, lead_voices, motion

MAX_MEAN_VOICE_MOTION = 2.5
MAX_SINGLE_VOICE_MOTION = 7


def _family_chords():
    for family in FAMILIES.values():
        key = D_MAJOR if family.mode == "major" else B_MINOR
        chords = []
        for phrase_id in ("A1", "A2", "B1", "B2", "A3", "O"):
            chords.extend(c for bar in parse_phrase(family.phrases[phrase_id], key) for c in bar)
        yield family, key, chords


class VoicingTests(unittest.TestCase):
    def test_every_candidate_keeps_guide_tones_and_stays_in_the_chord(self):
        for family, key, chords in _family_chords():
            for chord in chords:
                allowed = chord.tones | chord.available_tensions(key)
                for style in (KEYS_STYLE, PAD_STYLE):
                    for voicing, _ in candidates(chord, key, style):
                        pcs = {p % 12 for p in voicing}
                        self.assertTrue(chord.guide_tones <= pcs, f"{chord.symbol} {voicing}")
                        self.assertTrue(pcs <= allowed, f"{chord.symbol} {voicing}")
                        self.assertEqual(len(voicing), style.voices)
                        self.assertTrue(style.low <= voicing[0] and voicing[-1] <= style.high)
                        self.assertLessEqual(voicing[-1] - voicing[0], style.max_span)

    def test_low_voices_are_not_muddy(self):
        for _, key, chords in _family_chords():
            for chord in chords:
                for voicing, _ in candidates(chord, key, KEYS_STYLE):
                    low, next_up = voicing[0], voicing[1]
                    if low < 48:
                        self.assertGreaterEqual(next_up - low, 4, voicing)
                    self.assertFalse(next_up - low == 1 and low < 60, voicing)

    def test_voice_leading_moves_little(self):
        for family, key, chords in _family_chords():
            for style in (KEYS_STYLE, PAD_STYLE):
                voicings = lead_voices(chords, key, style, np.random.default_rng(30369))
                for before, after in pairwise(voicings):
                    self.assertLessEqual(motion(before, after) / style.voices, MAX_MEAN_VOICE_MOTION,
                                         f"{family.name} {before} -> {after}")
                    self.assertLessEqual(max(abs(a - b) for a, b in zip(before, after)), MAX_SINGLE_VOICE_MOTION)

    def test_voice_leading_is_deterministic(self):
        chords = [parse_chord(t, D_MAJOR) for t in ("ii:m9", "V:13", "I:maj9", "vi:m9")]
        first = lead_voices(chords, D_MAJOR, KEYS_STYLE, np.random.default_rng(33074))
        second = lead_voices(chords, D_MAJOR, KEYS_STYLE, np.random.default_rng(33074))
        self.assertEqual(first, second)

    def test_rootless_keys_prefer_leaving_the_root_to_the_bass(self):
        chord = parse_chord("ii:m9", D_MAJOR)
        best_voicing, _ = min(candidates(chord, D_MAJOR, KEYS_STYLE), key=lambda item: item[1])
        self.assertNotIn(chord.root, {p % 12 for p in best_voicing})


if __name__ == "__main__":
    unittest.main()
