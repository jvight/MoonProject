"""Seeded noise. Generators take a ``numpy.random.Generator`` from :func:`synth.core.rng`.

The coloured noises are also exposed as filters (``pink_filter``, ``brown_filter``) so a loop can colour
periodic white noise with :func:`synth.loop.periodic` - generating coloured noise directly restarts the
colouring filter at sample 0, which leaves a (filtered) step at a loop seam.
"""
import math

import numpy as np
from scipy.signal import lfilter

from .core import SAMPLE_RATE

# Paul Kellet's refined pink filter: six one-pole sections plus a direct and a one-sample-delayed term.
_PINK_POLES = ((0.99886, 0.0555179), (0.99332, 0.0750759), (0.96900, 0.1538520), (0.86650, 0.3104856),
               (0.55000, 0.5329522), (-0.7616, -0.0168980))
_PINK_SCALE = 0.11


def white(n: int, gen: np.random.Generator) -> np.ndarray:
    """Uniform white noise in -1..1 (RMS ~0.577)."""
    return gen.uniform(-1.0, 1.0, n)


def pink_filter(x: np.ndarray) -> np.ndarray:
    """-3 dB/octave colouring of white noise ``x`` (Kellet). Uniform white in -> RMS ~0.1..0.2 out."""
    out = 0.5362 * x
    out[1:] += 0.115926 * x[:-1]
    for pole, gain in _PINK_POLES:
        out += lfilter([gain], [1.0, -pole], x)
    return _PINK_SCALE * out


def pink(n: int, gen: np.random.Generator) -> np.ndarray:
    return pink_filter(white(n, gen))


def brown_filter(x: np.ndarray, leak: float = 0.997) -> np.ndarray:
    """-6 dB/octave (above the leak corner) leaky integration, scaled to RMS ~0.25 for uniform white input."""
    gain = 0.25 / math.sqrt((1.0 - leak) / ((1.0 + leak) * 3.0))
    return gain * lfilter([1.0 - leak], [1.0, -leak], x)


def brown(n: int, gen: np.random.Generator, leak: float = 0.997) -> np.ndarray:
    return brown_filter(white(n, gen), leak)


def event_times(n: int, gen: np.random.Generator, rate: float, jitter: float = 1.0) -> np.ndarray:
    """Sorted sample positions (< n) of an event train with mean ``rate`` events/s. ``jitter`` 0 = regular
    spacing, 1 = Poisson (exponential gaps)."""
    mean_gap = SAMPLE_RATE / rate
    batch = int(n / mean_gap * 1.5) + 16
    times = np.array([gen.uniform(0.0, mean_gap)])
    while times[-1] < n:
        gaps = (1.0 - jitter) * mean_gap + jitter * gen.exponential(mean_gap, batch)
        times = np.concatenate((times, times[-1] + np.cumsum(gaps)))
    return times[times < n].astype(np.int64)
