"""Keys, chords and progression families. Run: python -m unittest discover -s tools/music/tests -t tools/music"""
import unittest

import numpy as np
from lofi.harmony import FAMILIES, parse_phrase, phrase_bars, substitute
from lofi.theory import B_MINOR, D_MAJOR, QUALITIES, Chord, key_named, note_name, parse_chord

D, E, F_SHARP, G, A, B, C_SHARP, B_FLAT = 2, 4, 6, 7, 9, 11, 1, 10


class KeyTests(unittest.TestCase):
    def test_d_major_and_b_minor_share_their_notes(self):
        self.assertEqual(D_MAJOR.pcs, {D, E, F_SHARP, G, A, B, C_SHARP})
        self.assertEqual(B_MINOR.pcs, D_MAJOR.pcs)
        self.assertEqual(B_MINOR.scale[0], B)

    def test_pentatonic_is_the_game_scale(self):
        self.assertEqual(D_MAJOR.pentatonic, {D, E, F_SHARP, A, B})
        self.assertEqual(B_MINOR.pentatonic, D_MAJOR.pentatonic)

    def test_numerals_follow_the_mode(self):
        self.assertEqual(D_MAJOR.degree_root("vi"), B)
        self.assertEqual(B_MINOR.degree_root("III"), D)
        self.assertEqual(B_MINOR.degree_root("VII"), A)

    def test_unknown_key_is_refused(self):
        with self.assertRaises(ValueError):
            key_named("C major")

    def test_note_names(self):
        self.assertEqual(note_name(62), "D4")
        self.assertEqual(note_name(70), "Bb4")
        self.assertEqual(note_name(33), "A1")


class ChordTests(unittest.TestCase):
    def test_ii9_in_d_major(self):
        chord = parse_chord("ii:m9", D_MAJOR)
        self.assertEqual(chord.symbol, "Em9")
        self.assertEqual(chord.tones, {E, G, B, D, F_SHARP})
        self.assertEqual(chord.guide_tones, {G, D, F_SHARP})

    def test_slash_chord_and_six_nine(self):
        chord = parse_chord("I:6/9/iii", D_MAJOR)
        self.assertEqual(chord.symbol, "D6/9/F#")
        self.assertEqual(chord.bass, F_SHARP)
        self.assertEqual(chord.tones, {D, F_SHARP, A, B, E})

    def test_fixed_chords_keep_their_colour_and_draw_nothing(self):
        chord = parse_chord("I:6/9=", D_MAJOR)
        self.assertTrue(chord.fixed)
        self.assertEqual(chord.symbol, "D6/9")
        gen = np.random.default_rng(1618)
        for _ in range(16):
            self.assertIs(substitute(chord, D_MAJOR, gen, 1.0), chord)
        self.assertEqual(gen.random(), np.random.default_rng(1618).random())

    def test_borrowed_flag_must_match_the_notes(self):
        borrowed = parse_chord("iv:m6!", D_MAJOR)
        self.assertEqual(borrowed.out_of_key(D_MAJOR), {B_FLAT})
        with self.assertRaises(ValueError):
            parse_chord("iv:m6", D_MAJOR)
        with self.assertRaises(ValueError):
            parse_chord("IV:maj9!", D_MAJOR)
        with self.assertRaises(ValueError):
            parse_chord("I:maj9#11", D_MAJOR)

    def test_tensions_are_filtered_by_the_key(self):
        f_sharp_minor = parse_chord("iii:m7", D_MAJOR)
        self.assertEqual(f_sharp_minor.available_tensions(D_MAJOR), {B})
        dominant = parse_chord("V:7sus4", D_MAJOR)
        self.assertEqual(dominant.available_tensions(D_MAJOR), {B, F_SHARP})

    def test_every_quality_has_its_guide_tones_inside_it(self):
        for quality in QUALITIES.values():
            self.assertTrue(set(quality.guide) <= set(quality.intervals), quality.suffix)
            self.assertEqual(quality.intervals[0], 0)


class FamilyTests(unittest.TestCase):
    def _key_for(self, family):
        return D_MAJOR if family.mode == "major" else B_MINOR

    def test_every_phrase_parses_and_fills_four_bars(self):
        for family in FAMILIES.values():
            key = self._key_for(family)
            for phrase_id, text in family.phrases.items():
                bars = parse_phrase(text, key)
                self.assertEqual(len(bars), 4, f"{family.name} {phrase_id}")

    def test_only_flagged_chords_leave_the_key(self):
        for family in FAMILIES.values():
            key = self._key_for(family)
            for text in family.phrases.values():
                for bar in parse_phrase(text, key):
                    for chord in bar:
                        self.assertEqual(bool(chord.out_of_key(key)), chord.borrowed, chord.symbol)

    def test_the_only_borrowed_chord_is_the_minor_iv_of_one_family(self):
        borrowed_families = set()
        for family in FAMILIES.values():
            key = self._key_for(family)
            for text in family.phrases.values():
                for bar in parse_phrase(text, key):
                    for chord in bar:
                        if chord.borrowed:
                            borrowed_families.add(family.name)
                            self.assertEqual(chord.symbol, "Gm6")
        self.assertEqual(borrowed_families, {"IV-iv-I"})

    def test_substitutions_keep_function_and_key(self):
        gen = np.random.default_rng(79156)
        for family in FAMILIES.values():
            key = self._key_for(family)
            for text in family.phrases.values():
                for bar in parse_phrase(text, key):
                    for chord in bar:
                        for _ in range(8):
                            swapped = substitute(chord, key, gen, 1.0)
                            self.assertEqual((swapped.root, swapped.bass), (chord.root, chord.bass))
                            self.assertEqual(bool(swapped.out_of_key(key)), chord.borrowed)

    def test_phrase_bars_are_deterministic_and_four_beats(self):
        family = FAMILIES["ii-V-I"]
        first = phrase_bars(family, D_MAJOR, ("A1", "A2"), np.random.default_rng(33074), 0.5)
        second = phrase_bars(family, D_MAJOR, ("A1", "A2"), np.random.default_rng(33074), 0.5)
        self.assertEqual(first, second)
        for bar in first:
            self.assertAlmostEqual(sum(slot.beats for slot in bar), 4.0)

    def test_family_mode_must_match_the_key(self):
        with self.assertRaises(ValueError):
            phrase_bars(FAMILIES["ii-V-I"], B_MINOR, ("A1",), np.random.default_rng(76518), 0.0)

    def test_chord_symbol_without_bass(self):
        self.assertEqual(Chord(A, QUALITIES["13sus4"], A).symbol, "A13sus4")


if __name__ == "__main__":
    unittest.main()
