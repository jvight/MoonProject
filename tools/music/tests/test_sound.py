"""Instruments, texture, mix helpers and measurements. Run: python -m unittest discover -s tools/music/tests -t tools/music"""
import unittest

import numpy as np
from lofi import analysis, instruments, mixer, texture
from lofi.composer import compose
from lofi.groove import VOICES
from lofi.mixer import CEILING_DBTP, Renderer, fader_out, normalise, render
from lofi.score import NoteEvent
from lofi.spec import Mood, SectionPlan, TrackSpec
from lofi.theory import D_MAJOR
from lofi.tracks import PLAYLIST
from synth import effects, filters
from synth.analysis import loudness_integrated, seam_stats, true_peak_db
from synth.core import SAMPLE_RATE, rng


def _noise(seconds, seed):
    return np.random.default_rng(seed).standard_normal((int(seconds * SAMPLE_RATE), 2)) * 0.1


class InstrumentTests(unittest.TestCase):
    def _assert_clean_note(self, note, name):
        self.assertTrue(np.all(np.isfinite(note)), name)
        self.assertEqual(note[0], 0.0, f"{name} must start from silence")
        self.assertLess(abs(note[-1]), 1e-3, f"{name} must end in silence")
        self.assertLess(np.max(np.abs(note)), 2.0, name)

    def test_pitched_notes_start_and_end_silent(self):
        gen = rng("test", "notes")
        for pitch in (52, 64, 76):
            self._assert_clean_note(instruments.rhodes_note(pitch, 0.8, 1.0, gen), f"rhodes {pitch}")
            self._assert_clean_note(instruments.pad_note(pitch + 5, 0.5, 1.5, gen), f"pad {pitch}")
        for pitch in (66, 78, 86):
            self._assert_clean_note(instruments.kalimba_note(pitch, 0.8, gen), f"kalimba {pitch}")
            self._assert_clean_note(instruments.flute_note(pitch, 0.8, 0.8, gen), f"flute {pitch}")
        for pitch in (74, 82, 90):
            self._assert_clean_note(instruments.musicbox_note(pitch, 0.8, gen), f"musicbox {pitch}")
        for pitch in (52, 65, 76, 88):
            self._assert_clean_note(instruments.vibes_note(pitch, 0.8, 0.6, gen), f"vibes {pitch}")
            self._assert_clean_note(instruments.felt_note(pitch, 0.8, 0.9, gen), f"felt {pitch}")

    def test_leads_have_no_harsh_top(self):
        gen = rng("test", "top")
        for note in (instruments.kalimba_note(86, 1.0, gen), instruments.musicbox_note(90, 1.0, gen),
                     instruments.flute_note(83, 1.0, 1.0, gen), instruments.rhodes_note(76, 1.0, 1.0, gen),
                     instruments.vibes_note(88, 1.0, 1.0, gen), instruments.felt_note(88, 1.0, 1.0, gen)):
            spectrum = np.abs(np.fft.rfft(note)) ** 2
            freqs = np.fft.rfftfreq(note.shape[0], 1.0 / SAMPLE_RATE)
            share = spectrum[freqs >= 8000.0].sum() / spectrum.sum()
            self.assertLess(10.0 * np.log10(share + 1e-30), -30.0)

    def test_drum_kit_round_robin_varies_and_ends_silent(self):
        for style in instruments.KIT_PARAMS:
            kit = instruments.DrumKit(style, rng("test", "kit", style), variants=2)
            for voice in VOICES:
                first, second = kit.hit(voice, 0.9), kit.hit(voice, 0.9)
                self.assertFalse(np.array_equal(first, second), f"{style} {voice}")
                self.assertEqual(first[-1], 0.0)

    def test_theremin_glides_between_notes_and_rests_in_silence(self):
        notes = [NoteEvent(1.0, 1.0, 74, 0.7), NoteEvent(2.1, 1.5, 78, 0.8), NoteEvent(8.0, 1.0, 69, 0.6)]
        n = int(8.0 * SAMPLE_RATE)
        line = instruments.theremin_line(notes, 0.0, n, lambda beat: 0.75 * beat)
        self.assertTrue(np.all(np.isfinite(line)))
        self.assertEqual(line[0], 0.0)
        clicks = filters.butter(line, "highpass", 8000.0, order=4)
        self.assertLess(np.max(np.abs(clicks)), 1e-3, "no steps (clicks) in the line")
        gap = line[int(4.6 * SAMPLE_RATE):int(5.9 * SAMPLE_RATE)]
        self.assertLess(np.max(np.abs(gap)), 1e-4, "a long rest falls silent")
        legato = line[int(1.4 * SAMPLE_RATE):int(1.7 * SAMPLE_RATE)]
        self.assertGreater(np.max(np.abs(legato)), 0.1, "close notes share one breath")

    def test_bass_slides_glide_without_reattack(self):
        score = compose(PLAYLIST[0])
        notes = score.parts["bass"]
        line = instruments.bass_line(notes, 0.0, int(20 * SAMPLE_RATE), score.seconds)
        self.assertTrue(np.all(np.isfinite(line)))
        self.assertLess(np.max(np.abs(np.diff(line))), 0.05, "no steps (clicks) in the bass line")


class TextureAndMixTests(unittest.TestCase):
    def test_mono_below_makes_the_low_end_mono(self):
        lows = filters.butter(_noise(4.0, 1), "lowpass", 60.0, order=4)
        highs = filters.butter(_noise(4.0, 9), "highpass", 2000.0, order=4)
        x = lows / np.std(lows) + highs / np.std(highs)
        self.assertLess(abs(analysis.correlation(x, below_hz=150.0)), 0.2)
        mono = texture.mono_below(x)
        self.assertGreater(analysis.correlation(mono, below_hz=150.0), 0.95)
        self.assertLess(analysis.correlation(mono), 0.5)

    def test_tape_stop_keeps_the_start_and_ends_silent(self):
        x = _noise(4.0, 2)
        stopped = texture.tape_stop(x, 1.0, 1.5)
        cut = int(1.0 * SAMPLE_RATE)
        np.testing.assert_allclose(stopped[:cut], x[:cut])
        self.assertTrue(np.all(stopped[int(2.5 * SAMPLE_RATE):] == 0.0))

    def test_fader_out_reaches_exact_zero(self):
        x = _noise(8.0, 3)
        faded = fader_out(x, 6.0)
        np.testing.assert_allclose(faded[:SAMPLE_RATE], x[:SAMPLE_RATE])
        self.assertTrue(np.all(faded[-1] == 0.0))

    def test_wrap_loop_is_the_steady_state_of_a_repeating_cycle(self):
        n, pre, tail = 4800, 300, 3000
        impulses = np.zeros(pre + n + tail)
        impulses[[pre - 50, pre + 700, pre + n - 120]] = 1.0
        x = np.stack([filters.onepole_lp(impulses, 40.0)] * 2, axis=1)
        looped = texture.wrap_loop(x, pre, n)
        cycle = impulses[pre:pre + n] + np.pad(impulses[:pre], (n - pre, 0))
        forever = filters.onepole_lp(np.tile(cycle, 6), 40.0)
        np.testing.assert_allclose(looped[:, 0], forever[4 * n:5 * n], atol=1e-3)

    def test_steady_runs_dynamics_as_if_the_loop_never_ended(self):
        t = np.arange(int(4.0 * SAMPLE_RATE)) / SAMPLE_RATE
        tone = 0.5 * np.sin(2.0 * np.pi * 220.0 * t) * (0.6 + 0.4 * np.sin(2.0 * np.pi * 0.5 * t))
        x = np.stack([tone, tone], axis=1)

        def squash(y):
            return effects.compressor(y, threshold_db=-20.0, ratio=3.0, attack=0.02, release=0.3)

        forever = squash(np.concatenate([x, x, x]))[x.shape[0]:2 * x.shape[0]]
        np.testing.assert_allclose(texture.steady(x, squash), forever, atol=1e-6)

    def test_beds_loop_seamlessly(self):
        n = int(3.0 * SAMPLE_RATE)
        for name in ("vinyl", "cassette", "airlock"):
            bed = texture.bed(name, n, rng("test", "bed", name), True)
            self.assertEqual(bed.shape, (n, 2))
            ratio, _ = seam_stats(bed)
            self.assertLessEqual(ratio, 1.0, name)

    def test_narrow_folds_the_sides_and_airlock_room_keeps_the_signal(self):
        x = _noise(1.0, 12)
        mono = texture.narrow(x, 0.0)
        np.testing.assert_allclose(mono[:, 0], mono[:, 1])
        room = texture.airlock_room(x)
        self.assertEqual(room.shape, x.shape)
        self.assertTrue(np.all(np.isfinite(room)))

    def test_a_loop_renders_exactly_one_seamless_cycle(self):
        spec = TrackSpec(number=99, slug="test_loop", title="Test Loop", seed=99, bpm=84.0, swing=0.6,
                         key="D major", family="I-IV-iii-vi", drums="bounce", ending="loop", tape=True,
                         mood=Mood(texture="cassette"),
                         form=(SectionPlan("A", ("O",), lead="vibes", density=0.6, opening=(0.8, 0.8)),))
        score = compose(spec)
        audio, _ = render(score, lambda *keys: rng(spec.seed, *keys))
        self.assertEqual(audio.shape[0], Renderer(score, rng).loop_n)
        self.assertAlmostEqual(loudness_integrated(audio), mixer.TARGET_LUFS, delta=0.1)
        ratio, _ = seam_stats(audio)
        self.assertLessEqual(ratio, 1.0)

    def test_vinyl_is_stereo_and_partly_correlated(self):
        bed = texture.vinyl(int(5 * SAMPLE_RATE), rng("test", "vinyl"))
        self.assertEqual(bed.shape[1], 2)
        self.assertTrue(0.2 < analysis.correlation(bed) < 0.9)

    def test_normalise_hits_loudness_under_the_ceiling(self):
        x = _noise(6.0, 4) * np.linspace(0.2, 1.0, int(6.0 * SAMPLE_RATE))[:, None]
        out = normalise(x)
        self.assertAlmostEqual(loudness_integrated(out), mixer.TARGET_LUFS, delta=0.1)
        self.assertLessEqual(true_peak_db(out), CEILING_DBTP + 0.05)

    def test_render_is_deterministic(self):
        spec = PLAYLIST[1]
        score = compose(spec)
        first, _ = render(score, lambda *keys: rng(spec.seed, *keys), limit_s=6.0)
        second, _ = render(score, lambda *keys: rng(spec.seed, *keys), limit_s=6.0)
        np.testing.assert_array_equal(first, second)
        self.assertEqual(first.shape, (Renderer(score, rng, 6.0).n, 2))


class AnalysisTests(unittest.TestCase):
    def test_silent_gaps_are_found_inside_but_not_at_the_edges(self):
        x = _noise(6.0, 5)
        x[int(2.0 * SAMPLE_RATE):int(2.5 * SAMPLE_RATE)] = 0.0
        x[-int(0.8 * SAMPLE_RATE):] = 0.0
        gaps = analysis.silent_gaps(x)
        self.assertEqual(len(gaps), 1)
        self.assertAlmostEqual(gaps[0][0], 2.0, delta=0.05)
        self.assertAlmostEqual(gaps[0][1], 0.5, delta=0.1)

    def test_band_balance_sums_to_one(self):
        balance = analysis.band_balance(_noise(3.0, 6))
        self.assertAlmostEqual(sum(balance.values()), 1.0, places=6)
        self.assertGreater(balance["high"], balance["low"])

    def test_correlation_extremes(self):
        mono = _noise(2.0, 7)
        mono[:, 1] = mono[:, 0]
        self.assertAlmostEqual(analysis.correlation(mono), 1.0)
        inverted = mono.copy()
        inverted[:, 1] = -inverted[:, 0]
        self.assertAlmostEqual(analysis.correlation(inverted), -1.0)

    def test_chroma_finds_the_key(self):
        t = np.arange(4 * SAMPLE_RATE) / SAMPLE_RATE
        triad = sum(np.sin(2.0 * np.pi * f * t) for f in (293.66, 369.99, 440.0))
        x = np.stack([triad, triad], axis=1) * 0.2
        self.assertAlmostEqual(analysis.chroma(x)[9], 1.0 / 3.0, delta=0.03)
        self.assertLess(analysis.chroma_out_of_key(x, D_MAJOR.pcs), 0.02)
        self.assertGreater(analysis.chroma_out_of_key(_noise(4.0, 10), D_MAJOR.pcs), 0.3)

    def test_beat_clarity_locks_only_to_the_true_tempo(self):
        x = _noise(20.0, 11) * 0.01
        beat = 60.0 / 80.0
        for start in np.arange(0.25, 19.5, beat):
            x[int(start * SAMPLE_RATE):int(start * SAMPLE_RATE) + 200] += 0.5
        locked, phase = analysis.beat_clarity(x, 80.0, 0.25)
        self.assertGreater(locked, 3.0)
        self.assertLess(min(phase, 1.0 - phase), 0.1)
        self.assertLess(analysis.beat_clarity(x, 83.0, 0.25)[0], 1.5)

    def test_clipped_samples(self):
        x = _noise(1.0, 8)
        x[10, 0] = 1.0
        self.assertEqual(analysis.clipped_samples(x), 1)


if __name__ == "__main__":
    unittest.main()
