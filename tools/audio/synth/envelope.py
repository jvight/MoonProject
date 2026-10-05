"""Envelopes, fades and LFOs (times in seconds; each envelope returns ``n`` gains).

Every attack starts from exactly zero and every fade-out ends at exactly zero: even the softest cue may not
click.
"""
import math

import numpy as np

from .core import SAMPLE_RATE, TWO_PI, samples

_LN_1000 = math.log(1000.0)


def decay_coefficient(t60: float) -> float:
    """Per-sample multiplier that decays by 60 dB in ``t60`` seconds."""
    return math.exp(-_LN_1000 / (max(t60, 1e-6) * SAMPLE_RATE))


def exp_decay(n: int, t60: float) -> np.ndarray:
    """``1 -> 0.001`` over ``t60`` seconds (and onwards), exponential."""
    return np.exp(-_LN_1000 * np.arange(n) / (max(t60, 1e-6) * SAMPLE_RATE))


def curve(count: int, shape: str = "sine") -> np.ndarray:
    """Rising curve of ``count`` samples from exactly 0 towards 1: ``linear``, ``sine`` (fast start) or
    ``smooth`` (raised cosine, zero slope at both ends)."""
    u = np.arange(count, dtype=np.float64) / max(count, 1)
    if shape == "linear":
        return u
    if shape == "sine":
        return np.sin(0.5 * math.pi * u)
    if shape == "smooth":
        return 0.5 - 0.5 * np.cos(math.pi * u)
    raise ValueError(f"unknown curve shape '{shape}'")


def ar(n: int, attack: float, t60: float, shape: str = "smooth") -> np.ndarray:
    """Percussive envelope: curved attack to 1.0, then exponential decay of 60 dB per ``t60``."""
    a_n = min(n, samples(attack))
    return np.concatenate((curve(a_n, shape), exp_decay(n - a_n, t60)))


def adsr(n: int, attack: float, decay: float, sustain: float, release: float, gate: float | None = None,
         shape: str = "exp") -> np.ndarray:
    """ADSR with note-off at ``gate`` seconds (default: the release ends exactly at ``n``).

    ``shape="linear"``: straight segments (``decay``/``release`` are segment lengths).
    ``shape="exp"``: smooth attack, exponential decay towards ``sustain`` and exponential release, where
    ``decay``/``release`` are T60 times (analog-style).
    """
    a_n = min(n, samples(attack))
    r_n = samples(release)
    gate_n = min(n, samples(gate) if gate is not None else max(a_n, n - r_n))
    env = np.zeros(n, dtype=np.float64)
    if shape == "linear":
        d_n = samples(decay)
        held = np.interp(np.arange(gate_n), [0, a_n, a_n + d_n], [0.0, 1.0, sustain])
        env[:gate_n] = held
        level = held[-1] if gate_n else 0.0
        env[gate_n:] = level * np.clip(1.0 - np.arange(n - gate_n) / max(r_n, 1), 0.0, 1.0)
        return env
    if shape != "exp":
        raise ValueError(f"unknown ADSR shape '{shape}'")
    attack_n = min(a_n, gate_n)
    env[:attack_n] = curve(a_n, "smooth")[:attack_n]
    if gate_n > a_n:
        k = np.arange(gate_n - a_n)
        env[a_n:gate_n] = sustain + (1.0 - sustain) * np.exp(-_LN_1000 * k / (max(decay, 1e-6) * SAMPLE_RATE))
    level = env[gate_n - 1] if gate_n else 0.0
    env[gate_n:] = level * exp_decay(n - gate_n, release)
    return env


def segments(n: int, points, shape: str = "linear") -> np.ndarray:
    """Breakpoints ``[(time_s, level), ...]`` with increasing times; the last level is held. ``shape="smooth"``
    eases each segment with a raised cosine."""
    times = np.array([samples(t) for t, _ in points], dtype=np.float64)
    levels = np.array([lvl for _, lvl in points], dtype=np.float64)
    idx = np.arange(n, dtype=np.float64)
    if shape == "linear":
        return np.interp(idx, times, levels)
    if shape != "smooth":
        raise ValueError(f"unknown segment shape '{shape}'")
    seg = np.clip(np.searchsorted(times, idx, side="right") - 1, 0, len(times) - 1)
    nxt = np.minimum(seg + 1, len(times) - 1)
    span = np.maximum(times[nxt] - times[seg], 1.0)
    u = 0.5 - 0.5 * np.cos(math.pi * np.clip((idx - times[seg]) / span, 0.0, 1.0))
    return levels[seg] + (levels[nxt] - levels[seg]) * u


def fade_in(x: np.ndarray, duration: float, shape: str = "sine") -> np.ndarray:
    """Copy of ``x`` whose first sample is exactly 0, rising over ``duration`` (mono or stereo)."""
    out = np.array(x, dtype=np.float64, copy=True)
    count = min(out.shape[0], samples(duration))
    gains = curve(count, shape)
    out[:count] *= gains[:, None] if out.ndim == 2 else gains
    return out


def fade_out(x: np.ndarray, duration: float, shape: str = "sine") -> np.ndarray:
    """Copy of ``x`` whose last sample is exactly 0, falling over ``duration`` (mono or stereo)."""
    out = np.array(x, dtype=np.float64, copy=True)
    count = min(out.shape[0], samples(duration))
    if count <= 0:
        return out
    gains = curve(count, shape)[::-1]
    out[out.shape[0] - count:] *= gains[:, None] if out.ndim == 2 else gains
    return out


def lfo(n: int, rate: float, depth: float = 1.0, offset: float = 0.0, phase: float = 0.0) -> np.ndarray:
    """``offset + depth * sin(2 pi (rate t + phase))``. For loops use ``rate = k / loop_seconds``."""
    return offset + depth * np.sin(TWO_PI * (phase + rate * np.arange(n) / SAMPLE_RATE))
