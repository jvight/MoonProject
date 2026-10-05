"""Karplus-Strong plucked string (soft, low-passed pick; allpass fine tuning; exact pitch)."""
import math

import numpy as np
from scipy.signal import lfilter

from .core import SAMPLE_RATE, samples
from .filters import onepole_lp
from .noise import white


def karplus_strong(n: int, freq: float, gen: np.random.Generator, decay: float = 0.996, brightness: float = 0.5,
                   pick_softness: float = 2500.0, pick_length: float | None = None) -> np.ndarray:
    """Plucked string at ``freq`` Hz.

    ``decay``: loop gain per period (closer to 1 = longer ring). ``brightness`` 0..1 morphs the loop filter from
    the classic two-point average (dark) to none (bright). ``pick_softness``: low-pass (Hz) of the noise burst,
    lower = rounder, felt-like. ``pick_length``: burst length in seconds (default: one period).

    The whole loop (delay, averaging filter, tuning allpass) is one rational transfer function, so it runs in
    a single ``lfilter`` call:  H(z) = A_den(z) / (A_den(z) - g z^-N F(z) A_num(z)).
    """
    period = SAMPLE_RATE / freq
    avg = 0.5 * (1.0 - brightness)
    loop_delay = period - avg
    delay = math.floor(loop_delay - 0.1)
    frac = loop_delay - delay
    c = (1.0 - frac) / (1.0 + frac)
    if delay < 2:
        raise ValueError(f"frequency {freq} Hz too high for Karplus-Strong at {SAMPLE_RATE} Hz")
    burst_n = samples(pick_length) if pick_length else delay
    burst = onepole_lp(onepole_lp(white(burst_n, gen), pick_softness), pick_softness)
    burst -= burst.mean()
    excitation = np.zeros(n)
    excitation[:min(n, burst_n)] = burst[:n]
    # F(z) = (1 - avg) + avg z^-1 ; allpass A(z) = (c + z^-1) / (1 + c z^-1)
    loop_terms = ((1.0 - avg) * c, (1.0 - avg) + avg * c, avg)
    den = np.zeros(delay + 3)
    den[0], den[1] = 1.0, c
    for k, term in enumerate(loop_terms):
        den[delay + k] -= decay * term
    return lfilter([1.0, c], den, excitation)
