"""Filters: one-pole, RBJ biquads (static or swept), Butterworth, DC blocker and BS.1770 K-weighting.

All filters are causal and start from a zero state. Inputs may be mono ``(n,)`` or stereo ``(n, 2)``; stereo is
filtered per channel. For seamless loops wrap them in :func:`synth.loop.periodic`.
"""
import math

import numpy as np
from scipy.signal import butter as _butter
from scipy.signal import lfilter, sosfilt

from .core import SAMPLE_RATE, TWO_PI

BIQUAD_KINDS = ("lowpass", "highpass", "bandpass", "notch", "allpass", "peaking", "lowshelf", "highshelf")


def _apply(x: np.ndarray, b, a) -> np.ndarray:
    return lfilter(b, a, x, axis=0)


def coefficients(kind: str, freq: float, q: float = 0.7071, gain_db: float = 0.0) -> tuple:
    """RBJ Audio-EQ-Cookbook biquad ``(b, a)`` (normalised, ``a[0] == 1``). ``bandpass`` has 0 dB peak gain;
    ``gain_db`` is used by ``peaking`` and the shelves (whose slope is set through ``q``)."""
    freq = min(max(float(freq), 1.0), 0.49 * SAMPLE_RATE)
    w0 = TWO_PI * freq / SAMPLE_RATE
    cw, sw = math.cos(w0), math.sin(w0)
    alpha = sw / (2.0 * q)
    big_a = 10.0 ** (gain_db / 40.0)
    if kind == "lowpass":
        b, a = [(1 - cw) / 2, 1 - cw, (1 - cw) / 2], [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "highpass":
        b, a = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2], [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "bandpass":
        b, a = [alpha, 0.0, -alpha], [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "notch":
        b, a = [1.0, -2 * cw, 1.0], [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "allpass":
        b, a = [1 - alpha, -2 * cw, 1 + alpha], [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "peaking":
        b = [1 + alpha * big_a, -2 * cw, 1 - alpha * big_a]
        a = [1 + alpha / big_a, -2 * cw, 1 - alpha / big_a]
    elif kind in ("lowshelf", "highshelf"):
        s = 1.0 if kind == "lowshelf" else -1.0
        sq = 2.0 * math.sqrt(big_a) * alpha
        b = [big_a * ((big_a + 1) - s * (big_a - 1) * cw + sq),
             s * 2 * big_a * ((big_a - 1) - s * (big_a + 1) * cw),
             big_a * ((big_a + 1) - s * (big_a - 1) * cw - sq)]
        a = [(big_a + 1) + s * (big_a - 1) * cw + sq,
             -s * 2 * ((big_a - 1) + s * (big_a + 1) * cw),
             (big_a + 1) + s * (big_a - 1) * cw - sq]
    else:
        raise ValueError(f"unknown biquad kind '{kind}' (expected one of {BIQUAD_KINDS})")
    a0 = a[0]
    return np.array(b) / a0, np.array(a) / a0


def biquad(x: np.ndarray, kind: str, freq: float, q: float = 0.7071, gain_db: float = 0.0) -> np.ndarray:
    return _apply(x, *coefficients(kind, freq, q, gain_db))


def lowpass(x: np.ndarray, freq: float, q: float = 0.7071) -> np.ndarray:
    return biquad(x, "lowpass", freq, q)


def highpass(x: np.ndarray, freq: float, q: float = 0.7071) -> np.ndarray:
    return biquad(x, "highpass", freq, q)


def bandpass(x: np.ndarray, freq: float, q: float = 1.0) -> np.ndarray:
    """Constant 0 dB peak-gain band-pass."""
    return biquad(x, "bandpass", freq, q)


def notch(x: np.ndarray, freq: float, q: float = 1.0) -> np.ndarray:
    return biquad(x, "notch", freq, q)


def peaking(x: np.ndarray, freq: float, gain_db: float, q: float = 1.0) -> np.ndarray:
    return biquad(x, "peaking", freq, q, gain_db)


def lowshelf(x: np.ndarray, freq: float, gain_db: float, q: float = 0.7071) -> np.ndarray:
    return biquad(x, "lowshelf", freq, q, gain_db)


def highshelf(x: np.ndarray, freq: float, gain_db: float, q: float = 0.7071) -> np.ndarray:
    return biquad(x, "highshelf", freq, q, gain_db)


def butter(x: np.ndarray, kind: str, freq, order: int = 4) -> np.ndarray:
    """Butterworth ``lowpass``/``highpass`` (``freq`` Hz) or ``bandpass``/``bandstop`` (``freq`` = (lo, hi)),
    as second-order sections."""
    sos = _butter(order, freq, btype=kind, fs=SAMPLE_RATE, output="sos")
    return sosfilt(sos, x, axis=0)


def onepole_lp(x: np.ndarray, cutoff: float) -> np.ndarray:
    """6 dB/octave low-pass."""
    a = math.exp(-TWO_PI * cutoff / SAMPLE_RATE)
    return _apply(x, [1.0 - a], [1.0, -a])


def onepole_hp(x: np.ndarray, cutoff: float) -> np.ndarray:
    """6 dB/octave high-pass (input minus its one-pole low-pass)."""
    return x - onepole_lp(x, cutoff)


def dc_block(x: np.ndarray, cutoff: float = 15.0) -> np.ndarray:
    """First-order DC blocker y[n] = x[n] - x[n-1] + R y[n-1]."""
    r = math.exp(-TWO_PI * cutoff / SAMPLE_RATE)
    return _apply(x, [1.0, -1.0], [1.0, -r])


def swept(x: np.ndarray, kind: str, freq, q: float = 0.7071, gain_db: float = 0.0, block: int = 32) -> np.ndarray:
    """Biquad whose frequency follows ``freq`` (per-sample array, or callable(sample_index) -> Hz). Coefficients
    are refreshed every ``block`` samples while the transposed-direct-form state carries over, so sweeps are
    smooth. Mono or stereo (both channels follow the same curve)."""
    n = x.shape[0]
    out = np.empty(x.shape, dtype=np.float64)
    get = freq if callable(freq) else (lambda i: freq[i])
    state = np.zeros((2,) + x.shape[1:])
    for start in range(0, n, block):
        b, a = coefficients(kind, get(start), q, gain_db)
        out[start:start + block], state = lfilter(b, a, x[start:start + block], axis=0, zi=state)
    return out


# ITU-R BS.1770-4 K-weighting at 48 kHz: high-shelf pre-filter followed by the RLB high-pass.
_K_WEIGHTING = (
    ([1.53512485958697, -2.69169618940638, 1.19839281085285], [1.0, -1.69065929318241, 0.73248077421585]),
    ([1.0, -2.0, 1.0], [1.0, -1.99004745483398, 0.99007225036621]),
)


def k_weight(x: np.ndarray) -> np.ndarray:
    """BS.1770 K-weighting (defined for 48 kHz, the project's fixed rate)."""
    for b, a in _K_WEIGHTING:
        x = _apply(x, b, a)
    return x
