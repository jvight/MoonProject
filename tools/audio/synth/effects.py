"""Effects: reverb, chorus, wow/flutter, echo, tremolo, saturation and dynamics (compressor, limiters).

Feedback delay lines (reverb combs/allpasses, echo) are processed in blocks no longer than their delay: a block
then depends only on earlier blocks, so each block is a few numpy operations (plus one short ``lfilter`` for
damping) instead of a per-sample Python loop.
"""
import math

import numpy as np
from scipy.ndimage import minimum_filter1d, uniform_filter1d
from scipy.signal import lfilter, resample_poly

from .core import SAMPLE_RATE, TWO_PI, db_to_gain, samples, to_mono

# Freeverb tuning (Jezar, public domain), defined at 44.1 kHz and rescaled to the project rate.
_COMB_TUNING_44K = (1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617)
_ALLPASS_TUNING_44K = (556, 441, 341, 225)
_STEREO_SPREAD_44K = 23
_FIXED_GAIN = 0.015
_ALLPASS_FEEDBACK = 0.5


def _scaled(delays, spread: int) -> list:
    return [round((d + spread) * SAMPLE_RATE / 44100.0) for d in delays]


def _comb(x: np.ndarray, delay: int, feedback: float, damp: float) -> np.ndarray:
    """Freeverb comb: o[n] = s[n - D]; s[n] = x[n] + feedback * f[n]; f = one-pole low-pass (damp) of o."""
    n = x.shape[0]
    s = np.zeros(n + delay)
    state = np.zeros(1)
    b, a = [1.0 - damp], [1.0, -damp]
    for k in range(0, n, delay):
        count = min(delay, n - k)
        filtered, state = lfilter(b, a, s[k:k + count], zi=state)
        s[k + delay:k + delay + count] = x[k:k + count] + feedback * filtered
    return s[:n]


def _allpass(x: np.ndarray, delay: int, feedback: float = _ALLPASS_FEEDBACK) -> np.ndarray:
    """Freeverb allpass: v[n] = x[n] + g v[n - D]; y[n] = v[n - D] - x[n]."""
    n = x.shape[0]
    v = np.zeros(n + delay)
    for k in range(0, n, delay):
        count = min(delay, n - k)
        v[k + delay:k + delay + count] = x[k:k + count] + feedback * v[k:k + count]
    return v[:n] - x


def reverb(x: np.ndarray, room: float = 0.7, damping: float = 0.5, wet: float = 0.3, dry: float = 1.0,
           width: float = 1.0, predelay: float = 0.0, tail: float = 0.0) -> np.ndarray:
    """Freeverb (8 damped combs + 4 allpasses per side). Mono or stereo in, stereo ``(n, 2)`` out.

    ``room`` 0..1 -> comb feedback 0.70..0.98 (decay length); ``damping`` 0..1 -> how fast highs die in the
    tail; ``wet``/``dry`` linear gains; ``width`` 0 (mono) .. 1 (wide); ``predelay``/``tail`` in seconds
    (``tail`` appends silence so the decay is not cut off).
    """
    pad = samples(tail)
    src = np.concatenate((x, np.zeros((pad,) + x.shape[1:]))) if pad else np.asarray(x, dtype=np.float64)
    n = src.shape[0]
    feedback = 0.7 + 0.28 * min(max(room, 0.0), 1.0)
    damp = 0.4 * min(max(damping, 0.0), 1.0)
    fed = np.zeros(n)
    pre = samples(predelay)
    mono = to_mono(src) * (2.0 if src.ndim == 2 else 1.0)
    fed[pre:] = _FIXED_GAIN * mono[:n - pre]
    sides = []
    for spread in (0, _STEREO_SPREAD_44K):
        acc = np.zeros(n)
        for d in _scaled(_COMB_TUNING_44K, spread):
            acc += _comb(fed, d, feedback, damp)
        for d in _scaled(_ALLPASS_TUNING_44K, spread):
            acc = _allpass(acc, d)
        sides.append(acc)
    wet1 = wet * (width / 2.0 + 0.5)
    wet2 = wet * ((1.0 - width) / 2.0)
    left = wet1 * sides[0] + wet2 * sides[1]
    right = wet1 * sides[1] + wet2 * sides[0]
    out = np.stack([left, right], axis=1)
    out += dry * (src if src.ndim == 2 else src[:, None])
    return out


def _read_delayed(x: np.ndarray, delay_samples: np.ndarray) -> np.ndarray:
    """x[n - delay[n]] with linear interpolation (zeros before the start)."""
    idx = np.arange(x.shape[0], dtype=np.float64)
    return np.interp(idx - delay_samples, idx, x, left=0.0, right=0.0)


def chorus(x: np.ndarray, voices: int = 2, rate: float = 0.8, depth: float = 0.003, delay: float = 0.012,
           mix: float = 0.5, spread: float = 1.0, phase: float = 0.0) -> np.ndarray:
    """Multi-voice chorus -> stereo. Each voice reads ``x`` through a delay of ``delay +/- depth`` seconds
    modulated at ``rate`` Hz (voices evenly phase-offset and panned across +/- ``spread``). Stereo input is
    chorused per channel. For loops use ``rate = k / loop_seconds``."""
    if x.ndim == 2:
        left = chorus(x[:, 0], voices, rate, depth, delay, mix, spread, phase)[:, 0]
        right = chorus(x[:, 1], voices, rate, depth, delay, mix, spread, phase + 0.5 / max(voices, 1))[:, 1]
        return np.stack([left, right], axis=1)
    n = x.shape[0]
    t = np.arange(n) / SAMPLE_RATE
    out = np.zeros((n, 2))
    out += ((1.0 - mix) * x)[:, None]
    for v in range(voices):
        voice_phase = phase + v / voices
        d = (delay + depth * np.sin(TWO_PI * (rate * t + voice_phase))) * SAMPLE_RATE
        wet = _read_delayed(x, d) * (mix / voices)
        pos = 0.0 if voices == 1 else spread * (2.0 * v / (voices - 1) - 1.0)
        angle = (pos + 1.0) * math.pi / 4.0
        out[:, 0] += wet * math.cos(angle) * math.sqrt(2.0)
        out[:, 1] += wet * math.sin(angle) * math.sqrt(2.0)
    return out


def wow_flutter(x: np.ndarray, wow_cents: float = 6.0, wow_rate: float = 0.55, flutter_cents: float = 2.0,
                flutter_rate: float = 6.5, drift_cents: float = 0.0, gen: np.random.Generator | None = None,
                phase: float = 0.0) -> np.ndarray:
    """Tape-style pitch wobble via a modulated delay: slow ``wow`` and fast ``flutter`` sine components of
    +/- the given cents, plus optional random ``drift`` (smoothed noise, needs ``gen``). Mono or stereo
    (both channels share the same transport, as on a real tape)."""
    n = x.shape[0]
    t = np.arange(n) / SAMPLE_RATE
    cents = (wow_cents * np.sin(TWO_PI * (wow_rate * t + phase))
             + flutter_cents * np.sin(TWO_PI * (flutter_rate * t + 0.37 + phase)))
    if drift_cents and gen is not None:
        noise = lfilter([1.0 - 0.9995], [1.0, -0.9995], gen.standard_normal(n))
        peak = float(np.max(np.abs(noise))) or 1.0
        cents = cents + drift_cents * noise / peak
    ratio = 2.0 ** (cents / 1200.0) - 1.0
    # Pitch deviation is the derivative of the delay: delay(t) = -integral(ratio dt); keep it positive.
    delay = -np.cumsum(ratio) + 1.0
    delay -= delay.min()
    delay += 2.0
    if x.ndim == 2:
        return np.stack([_read_delayed(x[:, c], delay) for c in range(x.shape[1])], axis=1)
    return _read_delayed(x, delay)


def tremolo(x: np.ndarray, rate: float, depth: float, phase: float = 0.0) -> np.ndarray:
    """Amplitude modulation between ``1 - depth`` and 1."""
    gain = 1.0 - 0.5 * depth + 0.5 * depth * np.sin(TWO_PI * (phase + rate * np.arange(x.shape[0]) / SAMPLE_RATE))
    return x * (gain[:, None] if x.ndim == 2 else gain)


def echo(x: np.ndarray, time: float, feedback: float = 0.35, damping_hz: float = 3500.0, mix: float = 0.3,
         tail: float = 0.0) -> np.ndarray:
    """Feedback delay (mono or per-channel stereo) with a one-pole low-pass in the loop (repeats darken)."""
    if x.ndim == 2:
        return np.stack([echo(x[:, c], time, feedback, damping_hz, mix, tail) for c in range(2)], axis=1)
    pad = samples(tail)
    src = np.concatenate((x, np.zeros(pad))) if pad else x
    n = src.shape[0]
    d = max(1, samples(time))
    a = math.exp(-TWO_PI * damping_hz / SAMPLE_RATE)
    s = np.zeros(n + d)
    state = np.zeros(1)
    for k in range(0, n, d):
        count = min(d, n - k)
        damped, state = lfilter([1.0 - a], [1.0, -a], s[k:k + count], zi=state)
        s[k + d:k + d + count] = src[k:k + count] + feedback * damped
    return src + mix * s[:n]


def saturate(x: np.ndarray, drive: float = 2.0) -> np.ndarray:
    """Symmetric tanh saturation normalised so +/-1 stays +/-1 (adds odd harmonics, rounds peaks)."""
    return np.tanh(drive * x) / math.tanh(drive)


def soft_limit(x: np.ndarray, ceiling_db: float = -1.0, knee: float = 0.8) -> np.ndarray:
    """Static soft clipper: transparent below ``knee * ceiling``, then a tanh bend that never exceeds the
    ceiling. Value and slope are continuous at the knee. Memoryless (safe on loops)."""
    ceiling = db_to_gain(ceiling_db)
    t = knee * ceiling
    span = ceiling - t
    mag = np.abs(x)
    bent = t + span * np.tanh((mag - t) / span)
    return np.where(mag <= t, x, np.sign(x) * bent)


def _detector(x: np.ndarray, true_peak: bool) -> np.ndarray:
    """Per-sample absolute peak across channels; ``true_peak`` takes the max of a 4x oversampled signal."""
    mono_abs = np.abs(x).max(axis=1) if x.ndim == 2 else np.abs(x)
    if not true_peak:
        return mono_abs
    chans = [x[:, c] for c in range(x.shape[1])] if x.ndim == 2 else [x]
    over = np.max([np.abs(resample_poly(c, 4, 1)).reshape(-1, 4).max(axis=1) for c in chans], axis=0)
    return np.maximum(mono_abs, over[:mono_abs.shape[0]])


def _release_smooth(gain: np.ndarray, release: float, hop: int) -> np.ndarray:
    """Lets ``gain`` fall instantly but rise with a ``release`` time constant (control-rate loop)."""
    blocks = -(-gain.shape[0] // hop)
    padded = np.concatenate((gain, np.ones(blocks * hop - gain.shape[0])))
    block_min = padded.reshape(blocks, hop).min(axis=1)
    alpha = 1.0 - math.exp(-hop / (max(release, 1e-4) * SAMPLE_RATE))
    smooth = np.empty(blocks)
    level = 1.0
    for i, target in enumerate(block_min.tolist()):
        level = target if target < level else level + (target - level) * alpha
        smooth[i] = level
    centres = np.arange(blocks) * hop + 0.5 * hop
    return np.minimum(gain, np.interp(np.arange(gain.shape[0]), centres, smooth))


def limiter(x: np.ndarray, ceiling_db: float = -1.0, lookahead: float = 0.005, release: float = 0.08,
            true_peak: bool = True) -> np.ndarray:
    """Lookahead brickwall limiter. The gain ramps down smoothly over ``lookahead`` seconds before each peak (a
    forward minimum filter followed by a moving average of the same length guarantees the ramp reaches the
    required gain at the peak) and recovers with ``release``. ``true_peak`` detects inter-sample peaks on a
    4x oversampled copy. Output sample peaks never exceed the ceiling."""
    ceiling = db_to_gain(ceiling_db)
    level = _detector(x, true_peak)
    need = np.minimum(1.0, ceiling / np.maximum(level, 1e-12))
    size = max(1, samples(lookahead))
    ahead = minimum_filter1d(need, size=size, origin=-(size // 2), mode="nearest")
    ramp = uniform_filter1d(ahead, size=size, origin=(size - 1) // 2, mode="nearest")
    gain = _release_smooth(ramp, release, 16)
    out = x * (gain[:, None] if x.ndim == 2 else gain)
    return np.clip(out, -ceiling, ceiling)


def compressor(x: np.ndarray, threshold_db: float = -18.0, ratio: float = 3.0, attack: float = 0.01,
               release: float = 0.15, knee_db: float = 6.0, makeup_db: float = 0.0, rms_time: float = 0.01,
               sidechain: np.ndarray | None = None) -> np.ndarray:
    """Feed-forward soft-knee compressor (stereo-linked). The detector is the RMS level (dBFS, one-pole power
    average over ``rms_time``) of ``sidechain`` (default: the input), read every 1 ms; the gain reduction is
    smoothed with separate attack/release times."""
    hop = max(1, samples(0.001))
    det_src = x if sidechain is None else sidechain
    power = (det_src ** 2).mean(axis=1) if det_src.ndim == 2 else det_src ** 2
    k = math.exp(-1.0 / (max(rms_time, 1e-4) * SAMPLE_RATE))
    power = lfilter([1.0 - k], [1.0, -k], power)
    blocks = -(-power.shape[0] // hop)
    padded = np.concatenate((power, np.full(blocks * hop - power.shape[0], power[-1])))
    level_db = 10.0 * np.log10(np.maximum(padded.reshape(blocks, hop).mean(axis=1), 1e-12))
    over = level_db - threshold_db
    slope = 1.0 / ratio - 1.0
    half = 0.5 * knee_db
    reduction = np.where(over <= -half, 0.0,
                         np.where(over >= half, slope * over,
                                  slope * (over + half) ** 2 / (2.0 * max(knee_db, 1e-9))))
    a_att = 1.0 - math.exp(-hop / (max(attack, 1e-4) * SAMPLE_RATE))
    a_rel = 1.0 - math.exp(-hop / (max(release, 1e-4) * SAMPLE_RATE))
    smooth = np.empty(blocks)
    state = 0.0
    for i, target in enumerate(reduction.tolist()):
        state += (target - state) * (a_att if target < state else a_rel)
        smooth[i] = state
    centres = np.arange(blocks) * hop + 0.5 * hop
    gain_db = np.interp(np.arange(x.shape[0]), centres, smooth) + makeup_db
    gain = 10.0 ** (gain_db / 20.0)
    return x * (gain[:, None] if x.ndim == 2 else gain)
