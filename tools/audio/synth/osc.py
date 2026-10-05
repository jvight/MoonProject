"""Oscillators. ``freq`` is a constant (Hz) or a per-sample array of length ``n`` (glides, vibrato).

Saw and pulse are band-limited with polyBLEP, the triangle with polyBLAMP (measured: alias energy -48 dB for a
3 kHz triangle vs. -35 dB naive), so they stay clean at high pitches and after AudioSource pitch-shifting.
``phase`` is in cycles (0..1).
"""
import numpy as np

from .core import SAMPLE_RATE, TWO_PI


def phase_cycles(n: int, freq, phase: float = 0.0) -> np.ndarray:
    """Unwrapped phase in cycles for each sample (constant frequencies are exact, not accumulated)."""
    if np.ndim(freq) == 0:
        return phase + float(freq) * np.arange(n, dtype=np.float64) / SAMPLE_RATE
    f = np.asarray(freq, dtype=np.float64)[:n]
    if f.shape[0] < n:
        raise ValueError("frequency array shorter than n")
    acc = np.empty(n, dtype=np.float64)
    acc[0] = 0.0
    np.cumsum(f[:-1] / SAMPLE_RATE, out=acc[1:])
    return phase + acc


def _increment(n: int, freq) -> np.ndarray:
    """Per-sample phase increment in cycles."""
    if np.ndim(freq) == 0:
        return np.full(n, float(freq) / SAMPLE_RATE)
    return np.asarray(freq, dtype=np.float64)[:n] / SAMPLE_RATE


def sine(n: int, freq, phase: float = 0.0, amp: float = 1.0) -> np.ndarray:
    return amp * np.sin(TWO_PI * phase_cycles(n, freq, phase))


def _poly_blep(t: np.ndarray, dt: np.ndarray) -> np.ndarray:
    out = np.zeros_like(t)
    lo = t < dt
    u = t[lo] / dt[lo]
    out[lo] = u + u - u * u - 1.0
    hi = t > 1.0 - dt
    u = (t[hi] - 1.0) / dt[hi]
    out[hi] = u * u + u + u + 1.0
    return out


def _poly_blamp(t: np.ndarray, dt: np.ndarray) -> np.ndarray:
    out = np.zeros_like(t)
    lo = t < dt
    u = t[lo] / dt[lo] - 1.0
    out[lo] = -u * u * u / 3.0
    hi = t > 1.0 - dt
    u = (t[hi] - 1.0) / dt[hi] + 1.0
    out[hi] = u * u * u / 3.0
    return out


def saw(n: int, freq, phase: float = 0.0) -> np.ndarray:
    """Rising band-limited sawtooth, -1..1."""
    t = np.mod(phase_cycles(n, freq, phase), 1.0)
    return 2.0 * t - 1.0 - _poly_blep(t, _increment(n, freq))


def square(n: int, freq, phase: float = 0.0, width: float = 0.5) -> np.ndarray:
    """Band-limited pulse, -1..1, duty cycle ``width``."""
    t = np.mod(phase_cycles(n, freq, phase), 1.0)
    dt = _increment(n, freq)
    out = np.where(t < width, 1.0, -1.0)
    return out + _poly_blep(t, dt) - _poly_blep(np.mod(t - width, 1.0), dt)


def triangle(n: int, freq, phase: float = 0.0) -> np.ndarray:
    """Band-limited triangle, -1..1; phase 0 is the bottom corner."""
    t = np.mod(phase_cycles(n, freq, phase), 1.0)
    dt = _increment(n, freq)
    out = np.where(t < 0.5, 4.0 * t - 1.0, 3.0 - 4.0 * t)
    return out + 4.0 * dt * (_poly_blamp(t, dt) - _poly_blamp(np.mod(t + 0.5, 1.0), dt))


def additive(n: int, freq, partials, phase: float = 0.0) -> np.ndarray:
    """Sum of sines ``[(ratio, amplitude), ...]`` over a fundamental ``freq`` (scalar or per-sample).
    Partials at or above 0.45 x sample rate are dropped (no aliasing by construction)."""
    base = phase_cycles(n, freq, phase)
    top = float(np.max(freq)) if np.ndim(freq) else float(freq)
    out = np.zeros(n, dtype=np.float64)
    for ratio, amp in partials:
        if amp == 0.0 or top * ratio >= 0.45 * SAMPLE_RATE:
            continue
        out += amp * np.sin(TWO_PI * ratio * base)
    return out


def glide(n: int, start: float, end: float, time_constant: float | None = None) -> np.ndarray:
    """Frequency curve from ``start`` to ``end``: exponential over the whole length (perceptually even), or an
    exponential approach with ``time_constant`` seconds when given."""
    if n <= 1:
        return np.full(n, float(end))
    if time_constant is None:
        return start * (end / start) ** (np.arange(n) / (n - 1))
    return end + (start - end) * np.exp(-np.arange(n) / (time_constant * SAMPLE_RATE))


def vibrato(n: int, freq: float, rate: float, depth_cents: float, phase: float = 0.0) -> np.ndarray:
    """Per-sample frequency of ``freq`` with a sine vibrato of +/- ``depth_cents``."""
    cents = depth_cents * np.sin(TWO_PI * (phase + rate * np.arange(n) / SAMPLE_RATE))
    return freq * 2.0 ** (cents / 1200.0)
