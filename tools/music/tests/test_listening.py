"""
The listening analysis on signals with known answers.
Run: python -m unittest discover -s tools/music/tests -t tools/music
"""
import unittest

import numpy as np
from lofi import analysis
from synth.core import SAMPLE_RATE, midi_to_freq


def _clicks(bpm, seconds, seed):
    x = np.random.default_rng(seed).standard_normal((int(seconds * SAMPLE_RATE), 2)) * 0.01
    for start in np.arange(0.1, seconds - 0.1, 60.0 / bpm):
        x[int(start * SAMPLE_RATE):int(start * SAMPLE_RATE) + 200] += 0.5
    return x


def _tones(midis, seconds, amplitude=0.2):
    t = np.arange(int(seconds * SAMPLE_RATE)) / SAMPLE_RATE
    mono = sum(amplitude * np.sin(2.0 * np.pi * midi_to_freq(m) * t) for m in midis)
    return np.stack([mono, mono], axis=1)


class ListeningTests(unittest.TestCase):
    def test_tempo_is_found_blind(self):
        for bpm in (64.0, 72.0, 84.0):
            found, clarity = analysis.estimate_tempo(_clicks(bpm, 20.0, int(bpm)))
            self.assertAlmostEqual(found, bpm, delta=0.1)
            self.assertGreater(clarity, 2.0)

    def test_scale_is_the_collection_holding_the_energy(self):
        d_major = _tones((62, 64, 66, 67, 69, 71, 73), 3.0)
        self.assertEqual(analysis.detect_scale(d_major)["scale"], "D major")
        a_major = _tones((69, 71, 73, 74, 76, 78, 80), 3.0)
        self.assertEqual(analysis.detect_scale(a_major)["scale"], "A major")

    def test_onset_note_ignores_notes_still_ringing(self):
        rate = 44100
        t = np.arange(int(1.0 * rate)) / rate
        held = 0.5 * np.sin(2.0 * np.pi * midi_to_freq(74) * t)
        struck = np.where(t >= 0.4, 0.3 * np.sin(2.0 * np.pi * midi_to_freq(76) * t), 0.0)
        midi = analysis.onset_note(held + struck, rate, 0.4, 550.0, 1050.0)
        self.assertAlmostEqual(midi, 76.0, delta=0.1)

    def test_seam_of_a_periodic_signal_is_clean_and_of_a_cut_one_is_not(self):
        period = 200
        t = np.arange(period * 400) / SAMPLE_RATE
        loop = np.stack([0.3 * np.sin(2.0 * np.pi * SAMPLE_RATE / period * t)] * 2, axis=1)
        self.assertLessEqual(analysis.seam(loop)["ratio"], 1.0)
        cut = loop[:-period // 3]
        self.assertGreater(analysis.seam(cut)["ratio"], 1.0)

    def test_loudness_profile_and_octaves(self):
        quiet, loud = _tones((69,), 6.0, 0.05), _tones((69,), 6.0, 0.5)
        profile = analysis.loudness_profile(np.concatenate([quiet, loud]))
        self.assertAlmostEqual(profile["max_lufs"] - profile["min_lufs"], 20.0, delta=1.0)
        octaves = analysis.octave_balance(loud)
        self.assertEqual(max(octaves, key=octaves.get), "500")


if __name__ == "__main__":
    unittest.main()
