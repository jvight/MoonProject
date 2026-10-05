"""Unit tests for the shared DSP core. Run: python -m unittest discover -s tools/audio/tests -v"""
import hashlib
import math
import sys
import tempfile
import threading
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from synth import analysis, core, effects, envelope, filters, fm, io, loop, noise, osc, pluck  # noqa: E402
from synth.core import SAMPLE_RATE  # noqa: E402


def _alias_db(signal: np.ndarray, fundamental_bin: int, odd_only: bool) -> float:
    """Energy outside the true harmonic bins, relative to the total (bin-aligned, rectangular window)."""
    spectrum = np.abs(np.fft.rfft(signal)) ** 2
    total = spectrum[1:].sum()
    harmonics = np.arange(fundamental_bin, spectrum.shape[0], fundamental_bin)
    if odd_only:
        harmonics = harmonics[(harmonics // fundamental_bin) % 2 == 1]
    return 10.0 * math.log10((total - spectrum[harmonics].sum()) / total)


def _tone_level_db(x: np.ndarray, freq: float) -> float:
    return analysis.rms_db(x[SAMPLE_RATE // 10:]) - analysis.rms_db(osc.sine(x.shape[0], freq)[SAMPLE_RATE // 10:])


class OscillatorTests(unittest.TestCase):
    N = 4096

    def _freq(self, k: int) -> float:
        return SAMPLE_RATE * k / self.N

    def test_band_limited_waveforms_alias_far_less_than_naive(self):
        k = 257
        f = self._freq(k)
        t = np.mod(np.arange(self.N) * f / SAMPLE_RATE, 1.0)
        naive_saw = 2.0 * t - 1.0
        naive_tri = np.where(t < 0.5, 4.0 * t - 1.0, 3.0 - 4.0 * t)
        self.assertLess(_alias_db(osc.saw(self.N, f), k, False), _alias_db(naive_saw, k, False) - 12.0)
        self.assertLess(_alias_db(osc.triangle(self.N, f), k, True), _alias_db(naive_tri, k, True) - 10.0)
        self.assertLess(_alias_db(osc.square(self.N, f), k, True), -25.0)

    def test_sine_matches_closed_form_for_constant_and_array_frequency(self):
        n = 48000
        ref = np.sin(2.0 * math.pi * 440.0 * np.arange(n) / SAMPLE_RATE)
        np.testing.assert_allclose(osc.sine(n, 440.0), ref, atol=1e-12)
        np.testing.assert_allclose(osc.sine(n, np.full(n, 440.0)), ref, atol=1e-7)

    def test_additive_drops_partials_above_guard(self):
        out = osc.additive(1024, 15000.0, [(1.0, 1.0), (2.0, 1.0)])
        np.testing.assert_allclose(out, osc.sine(1024, 15000.0), atol=1e-12)


class NoiseAndRandomTests(unittest.TestCase):
    def test_rng_is_deterministic_and_key_sensitive(self):
        a = noise.white(1000, core.rng("cue", 1))
        b = noise.white(1000, core.rng("cue", 1))
        c = noise.white(1000, core.rng("cue", 2))
        np.testing.assert_array_equal(a, b)
        self.assertFalse(np.array_equal(a, c))

    def test_pink_slope_is_about_minus_three_db_per_octave(self):
        x = noise.pink(SAMPLE_RATE * 8, core.rng("pink"))
        low = analysis.rms_db(filters.bandpass(x, 500.0, 2.0))
        high = analysis.rms_db(filters.bandpass(x, 4000.0, 2.0))
        # Constant-Q bands: white noise would rise +9 dB over three octaves, pink stays ~flat.
        self.assertAlmostEqual(high - low, 0.0, delta=1.5)

    def test_event_times_rate(self):
        times = noise.event_times(SAMPLE_RATE * 20, core.rng("ev"), 50.0)
        self.assertTrue(np.all(np.diff(times) >= 0))
        self.assertAlmostEqual(times.shape[0] / 20.0, 50.0, delta=5.0)


class EnvelopeTests(unittest.TestCase):
    def test_fades_hit_exact_zero_at_edges(self):
        x = np.ones((4800, 2))
        out = envelope.fade_out(envelope.fade_in(x, 0.01), 0.01)
        self.assertEqual(out[0, 0], 0.0)
        self.assertEqual(out[-1, 1], 0.0)

    def test_ar_decays_sixty_db_in_t60(self):
        env = envelope.ar(SAMPLE_RATE * 2, 0.0, 1.0)
        self.assertAlmostEqual(20.0 * math.log10(env[SAMPLE_RATE]), -60.0, delta=0.1)

    def test_adsr_linear_and_exp_reach_sustain(self):
        for shape in ("linear", "exp"):
            env = envelope.adsr(SAMPLE_RATE * 2, 0.01, 0.1, 0.5, 0.3, gate=1.5, shape=shape)
            self.assertAlmostEqual(env[SAMPLE_RATE], 0.5, delta=0.01, msg=shape)
            self.assertEqual(env[0], 0.0)


class FilterTests(unittest.TestCase):
    def test_lowpass_attenuates_two_octaves_up_by_about_24_db(self):
        x = osc.sine(SAMPLE_RATE, 4000.0)
        self.assertAlmostEqual(_tone_level_db(filters.lowpass(x, 1000.0), 4000.0), -24.5, delta=1.0)

    def test_bandpass_peak_is_unity(self):
        x = osc.sine(SAMPLE_RATE, 1000.0)
        self.assertAlmostEqual(_tone_level_db(filters.bandpass(x, 1000.0, 2.0), 1000.0), 0.0, delta=0.1)

    def test_shelves_reach_their_gain(self):
        low = osc.sine(SAMPLE_RATE, 50.0)
        high = osc.sine(SAMPLE_RATE, 15000.0)
        self.assertAlmostEqual(_tone_level_db(filters.lowshelf(low, 400.0, 6.0), 50.0), 6.0, delta=0.3)
        self.assertAlmostEqual(_tone_level_db(filters.highshelf(high, 3000.0, -6.0), 15000.0), -6.0, delta=0.3)

    def test_swept_matches_static_filter_for_constant_curve_and_supports_stereo(self):
        x = noise.white(9600, core.rng("sw"))
        np.testing.assert_allclose(filters.swept(x, "lowpass", np.full(9600, 900.0)),
                                   filters.lowpass(x, 900.0), atol=1e-12)
        stereo = np.stack([x, -x], axis=1)
        out = filters.swept(stereo, "bandpass", lambda i: 300.0 + i * 0.1, q=2.0)
        np.testing.assert_allclose(out[:, 0], -out[:, 1], atol=1e-12)


class LoudnessTests(unittest.TestCase):
    def test_bs1770_calibration_997hz(self):
        # BS.1770: a 0 dBFS 997 Hz sine reads -3.01 LUFS, so -20 dBFS reads -23.01.
        x = osc.sine(SAMPLE_RATE * 3, 997.0, amp=0.1)
        self.assertAlmostEqual(analysis.loudness_integrated(x), -23.01, delta=0.05)

    def test_gating_ignores_silence(self):
        tone = osc.sine(SAMPLE_RATE * 3, 997.0, amp=0.1)
        padded = np.concatenate((tone, np.zeros(SAMPLE_RATE * 6)))
        # Ungated, 6 s of silence would pull the reading down ~4.8 dB; the gates drop the silent blocks and keep
        # only the few blocks that straddle the tone's end (BS.1770 behaviour, ~0.2 dB).
        self.assertAlmostEqual(analysis.loudness_integrated(padded), analysis.loudness_integrated(tone), delta=0.3)

    def test_true_peak_sees_inter_sample_peaks(self):
        x = osc.sine(4800, SAMPLE_RATE / 4.0, phase=0.125)
        self.assertGreater(analysis.true_peak_db(x), analysis.peak_db(x) + 2.0)


class PluckAndFmTests(unittest.TestCase):
    def test_karplus_strong_is_in_tune(self):
        for note in ("D3", "D4", "A5"):
            f = core.note_freq(note)
            x = pluck.karplus_strong(SAMPLE_RATE * 2, f, core.rng("ks"), decay=0.999)
            seg = x[4800:4800 + 65536] * np.hanning(65536)
            spectrum = np.abs(np.fft.rfft(seg, 8 * 65536))
            freqs = np.fft.rfftfreq(8 * 65536, 1.0 / SAMPLE_RATE)
            band = (freqs > f * 0.97) & (freqs < f * 1.03)
            measured = freqs[band][np.argmax(spectrum[band])]
            self.assertLess(abs(1200.0 * math.log2(measured / f)), 1.0, msg=note)

    def test_feedback_operator_matches_reference_loop(self):
        n = 2000
        theta = 2.0 * math.pi * 330.0 * np.arange(n) / SAMPLE_RATE
        ref = np.empty(n)
        p1 = p2 = 0.0
        for i in range(n):
            ref[i] = math.sin(theta[i] + 0.25 * (p1 + p2))
            p2, p1 = p1, ref[i]
        np.testing.assert_allclose(fm.operator(n, 330.0, feedback=0.5), ref, atol=1e-9)

    def test_two_op_with_zero_index_is_a_sine(self):
        np.testing.assert_allclose(fm.two_op(1000, 440.0, 2.0, 0.0), osc.sine(1000, 440.0), atol=1e-12)


class EffectsTests(unittest.TestCase):
    def test_reverb_is_stereo_finite_and_decays(self):
        impulse = np.zeros(SAMPLE_RATE // 10)
        impulse[0] = 1.0
        out = effects.reverb(impulse, room=0.6, wet=1.0, dry=0.0, tail=4.0)
        self.assertEqual(out.shape[1], 2)
        self.assertTrue(np.all(np.isfinite(out)))
        early = analysis.rms_db(out[:SAMPLE_RATE // 2])
        late = analysis.rms_db(out[-SAMPLE_RATE // 2:])
        self.assertLess(late - early, -50.0)

    def test_limiter_respects_ceiling(self):
        x = 1.6 * osc.sine(SAMPLE_RATE, 3000.0) * envelope.lfo(SAMPLE_RATE, 2.0, 0.5, 0.5)
        out = effects.limiter(x, ceiling_db=-1.0)
        self.assertLessEqual(analysis.peak_db(out), -1.0 + 1e-9)
        self.assertLessEqual(analysis.true_peak_db(out), -0.5)

    def test_compressor_reduces_level_above_threshold(self):
        x = osc.sine(SAMPLE_RATE * 2, 200.0, amp=0.7)
        out = effects.compressor(x, threshold_db=-20.0, ratio=4.0, attack=0.005, release=0.05, knee_db=0.0)
        before = analysis.rms_db(x[SAMPLE_RATE:])
        after = analysis.rms_db(out[SAMPLE_RATE:])
        # ~-6 dB RMS level is 14 dB over: expect ~10.5 dB of reduction.
        self.assertAlmostEqual(before - after, 10.5, delta=1.5)

    def test_chorus_and_wow_flutter_shapes(self):
        x = osc.sine(SAMPLE_RATE, 440.0)
        self.assertEqual(effects.chorus(x).shape, (SAMPLE_RATE, 2))
        self.assertEqual(effects.wow_flutter(np.stack([x, x], 1)).shape, (SAMPLE_RATE, 2))
        self.assertTrue(np.all(np.isfinite(effects.echo(x, 0.25, tail=1.0))))

    def test_soft_limit_never_exceeds_ceiling(self):
        x = np.linspace(-3.0, 3.0, 10001)
        out = effects.soft_limit(x, -1.0)
        self.assertLessEqual(float(np.max(np.abs(out))), core.db_to_gain(-1.0))
        np.testing.assert_array_equal(out[np.abs(x) < 0.5], x[np.abs(x) < 0.5])


class LoopTests(unittest.TestCase):
    def test_periodic_filtering_makes_noise_loop_seamless(self):
        x = noise.white(SAMPLE_RATE * 2, core.rng("loop"))
        y = loop.periodic(x, lambda s: filters.lowpass(noise.brown_filter(s), 300.0))
        ratio, _ = analysis.seam_stats(y)
        self.assertLessEqual(ratio, 1.0)
        naive = filters.lowpass(noise.brown_filter(x), 300.0)
        self.assertGreater(analysis.seam_stats(naive)[0], ratio)

    def test_crossfade_loop_keeps_seam_continuous(self):
        x = filters.lowpass(noise.white(SAMPLE_RATE * 3, core.rng("xf")), 500.0)
        y = loop.crossfade_loop(x, SAMPLE_RATE * 2, SAMPLE_RATE // 4)
        self.assertEqual(y.shape[0], SAMPLE_RATE * 2)
        self.assertAlmostEqual(y[0], x[SAMPLE_RATE * 2], places=12)


class IoTests(unittest.TestCase):
    def test_wav_is_byte_deterministic_and_round_trips(self):
        x = np.stack([osc.sine(4800, 440.0, amp=0.5), osc.sine(4800, 660.0, amp=0.5)], axis=1)
        with tempfile.TemporaryDirectory() as tmp:
            a, b = Path(tmp) / "a.wav", Path(tmp) / "b.wav"
            io.write_wav(a, x)
            io.write_wav(b, x)
            self.assertEqual(hashlib.sha256(a.read_bytes()).digest(), hashlib.sha256(b.read_bytes()).digest())
            back, rate = io.read_audio(a)
            self.assertEqual(rate, SAMPLE_RATE)
            self.assertLess(float(np.max(np.abs(back - x))), 2.0 / 32767.0)

    def test_long_stereo_ogg_writes_from_a_worker_thread(self):
        # Regression: one-shot encoding of long buffers overflowed worker-thread stacks (process exit 127).
        x = np.stack([osc.sine(SAMPLE_RATE * 30, 440.0, amp=0.3), osc.sine(SAMPLE_RATE * 30, 660.0, amp=0.3)], 1)
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "long.ogg"
            worker = threading.Thread(target=io.write_ogg, args=(path, x))
            worker.start()
            worker.join()
            back, rate = io.read_audio(path)
            self.assertEqual(rate, SAMPLE_RATE)
            self.assertEqual(back.shape, x.shape)

    def test_ogg_decoded_digest_is_stable(self):
        x = osc.sine(SAMPLE_RATE, 440.0, amp=0.3)
        with tempfile.TemporaryDirectory() as tmp:
            a, b = Path(tmp) / "a.ogg", Path(tmp) / "b.ogg"
            io.write_ogg(a, x)
            io.write_ogg(b, x)
            self.assertEqual(io.audio_digest(a), io.audio_digest(b))


if __name__ == "__main__":
    unittest.main()
