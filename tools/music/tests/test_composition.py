"""Whole-score checks on every track and tape. Run: python -m unittest discover -s tools/music/tests -t tools/music"""
import unittest
from itertools import pairwise

from lofi.bassline import ROOT_LOW, TOP
from lofi.composer import compose
from lofi.melody import LEADS
from lofi.theory import D_MAJOR
from lofi.theory_report import melody_report, out_of_key_notes
from lofi.tracks import CATALOGUE, PLAYLIST, track_by_id
from lofi.voicing import KEYS_STYLE, PAD_STYLE

MIN_SECONDS = 120.0
MAX_SECONDS = 180.0


class CompositionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scores = [compose(spec) for spec in CATALOGUE]

    def test_track_lengths_fit_the_radio(self):
        for score in self.scores:
            self.assertTrue(MIN_SECONDS <= score.duration_s <= MAX_SECONDS, f"{score.spec.id} {score.duration_s}")

    def test_playlist_is_distinct(self):
        ids = [s.id for s in PLAYLIST]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertEqual(len({s.family for s in PLAYLIST}), len(PLAYLIST))
        self.assertEqual(len({(s.bpm, s.swing) for s in PLAYLIST}), len(PLAYLIST))
        self.assertEqual(track_by_id("03").slug, "dust_on_the_dial")
        self.assertEqual(track_by_id("slow_orbit").id, "slow_orbit")
        self.assertEqual(len({s.id for s in CATALOGUE}), len(CATALOGUE))

    def test_no_note_leaves_the_key_except_borrowed_chords(self):
        for score in self.scores:
            self.assertEqual(out_of_key_notes(score), [], score.spec.id)

    def test_borrowed_chords_resolve_to_d(self):
        for score in self.scores:
            for event, following in pairwise(score.chords):
                if event.chord.borrowed:
                    self.assertEqual(following.chord.root, D_MAJOR.tonic, f"{score.spec.id} at {event.beat}")

    def test_melodies_follow_the_rules(self):
        for score in self.scores:
            report = melody_report(score)
            self.assertGreater(report["notes"], 20, score.spec.id)
            self.assertEqual(report["non_pentatonic"], [], score.spec.id)
            self.assertEqual(report["strong_off_chord"], [], score.spec.id)
            self.assertEqual(report["clashes"], [], score.spec.id)

    def test_parts_stay_in_their_registers(self):
        for score in self.scores:
            for note in score.parts["bass"]:
                self.assertTrue(ROOT_LOW - 3 <= note.pitch <= TOP, note)
            for note in score.parts["keys"]:
                self.assertTrue(KEYS_STYLE.low <= note.pitch <= KEYS_STYLE.high, note)
            for note in score.parts["pad"]:
                self.assertTrue(PAD_STYLE.low <= note.pitch <= PAD_STYLE.high, note)
            for name, notes in score.parts.items():
                if name.startswith("lead."):
                    voice = LEADS[name.split(".", 1)[1]]
                    self.assertTrue(all(voice.low <= n.pitch <= voice.high for n in notes), name)

    def test_composition_is_deterministic(self):
        for spec, score in zip(CATALOGUE, self.scores):
            again = compose(spec)
            self.assertEqual(again.parts, score.parts, spec.id)
            self.assertEqual(again.drums, score.drums, spec.id)
            self.assertEqual(again.chords, score.chords, spec.id)

    def test_repeated_sections_are_not_literal_copies(self):
        for score in self.scores:
            by_phrase = {}
            for section in score.sections:
                start, end = section.start_beat, section.start_beat + 4.0 * section.bars
                drums = tuple((round(h.beat - start, 3), h.voice) for h in score.drums if start <= h.beat < end)
                keys = tuple((round(n.beat - start, 3), n.pitch) for n in score.parts["keys"] if start <= n.beat < end)
                signature = (drums, keys)
                self.assertNotIn(signature, by_phrase.values(), f"{score.spec.id} {section.name}")
                by_phrase[section.name] = signature

    def test_every_section_change_is_bar_aligned(self):
        for score in self.scores:
            for section in score.sections:
                self.assertEqual(section.start_beat % 4.0, 0.0)
            self.assertEqual(sum(s.bars for s in score.sections) * 4.0, score.spec.music_beats)


if __name__ == "__main__":
    unittest.main()
