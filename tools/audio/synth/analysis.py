"""Objective measurements (we cannot listen, so we measure).

Levels: sample peak, true peak (4x oversampled), RMS, DC offset. Loudness: ITU-R BS.1770-4 K-weighted
integrated LUFS with the -70 LUFS absolute and -10 LU relative gates, and the loudest sliding window
(momentary-style, configurable length). Spectrum: centroid and the share of energy above a cutoff. Loops:
continuity across the seam.
"""
import math

import numpy as np
from scipy.signal import resample_poly, welch

from .core import SAMPLE_RATE, gain_to_db
from .filters import k_weight

_BLOCK_S = 0.4
_HOP_S = 0.1
_ABS_GATE_LUFS = -70.0
_REL_GATE_LU = -10.0


def _as_2d(x: np.ndarray) -> np.ndarray:
    return x[:, None] if x.ndim == 1 else x


def peak_db(x: np.ndarray) -> float:
    return gain_to_db(float(np.max(np.abs(x))))


def true_peak_db(x: np.ndarray) -> float:
    """Peak of a 4x oversampled copy (BS.1770 true-peak approximation)."""
    x2 = _as_2d(x)
    over = max(float(np.max(np.abs(resample_poly(x2[:, c], 4, 1)))) for c in range(x2.shape[1]))
    return gain_to_db(max(over, float(np.max(np.abs(x2)))))


def rms_db(x: np.ndarray) -> float:
    return gain_to_db(math.sqrt(float(np.mean(np.square(x)))))


def dc_offset(x: np.ndarray) -> float:
    """Largest absolute channel mean."""
    return float(np.max(np.abs(_as_2d(x).mean(axis=0))))


def _weighted_power(x: np.ndarray) -> np.ndarray:
    """Per-sample K-weighted power summed over channels (L/R weights 1.0)."""
    return np.square(k_weight(_as_2d(x))).sum(axis=1)


def _window_means(power: np.ndarray, window: int, hop: int) -> np.ndarray:
    if power.shape[0] <= window:
        return np.array([power.mean()])
    prefix = np.concatenate(([0.0], np.cumsum(power)))
    starts = np.arange(0, power.shape[0] - window + 1, hop)
    return (prefix[starts + window] - prefix[starts]) / window


def _to_lufs(mean_square) -> float:
    return -0.691 + 10.0 * math.log10(mean_square) if mean_square > 0.0 else -200.0


def loudness_integrated(x: np.ndarray) -> float:
    """BS.1770-4 integrated loudness (LUFS): 400 ms blocks, 75 % overlap, absolute and relative gating."""
    blocks = _window_means(_weighted_power(x), int(_BLOCK_S * SAMPLE_RATE), int(_HOP_S * SAMPLE_RATE))
    with np.errstate(divide="ignore"):
        block_lufs = -0.691 + 10.0 * np.log10(blocks)
    above_abs = blocks[block_lufs > _ABS_GATE_LUFS]
    if above_abs.size == 0:
        return -200.0
    relative_gate = _to_lufs(above_abs.mean()) + _REL_GATE_LU
    gated = blocks[(block_lufs > _ABS_GATE_LUFS) & (block_lufs > relative_gate)]
    return _to_lufs(gated.mean())


def loudness_momentary_max(x: np.ndarray, window: float = 0.4) -> float:
    """Loudest K-weighted sliding window (hop = window / 4) in LUFS. 0.4 s is BS.1770 'momentary'; short
    one-shots are better compared with ~0.2 s."""
    w = max(1, int(window * SAMPLE_RATE))
    return _to_lufs(float(_window_means(_weighted_power(x), w, max(1, w // 4)).max()))


def spectral_stats(x: np.ndarray, hf_cutoff: float = 8000.0) -> tuple:
    """``(centroid_hz, hf_db)``: magnitude-weighted spectral centroid (Welch, Hann 4096) of the mono mix and
    the energy above ``hf_cutoff`` relative to the total, in dB. DC bin excluded."""
    mono = x.mean(axis=1) if x.ndim == 2 else x
    freqs, psd = welch(mono, fs=SAMPLE_RATE, window="hann", nperseg=min(4096, mono.shape[0]))
    freqs, psd = freqs[1:], psd[1:]
    mags = np.sqrt(psd)
    centroid = float((freqs * mags).sum() / mags.sum()) if mags.sum() > 0 else 0.0
    total = float(psd.sum())
    hf = float(psd[freqs >= hf_cutoff].sum())
    return centroid, (10.0 * math.log10(hf / total) if hf > 0.0 and total > 0.0 else -200.0)


def seam_stats(x: np.ndarray) -> tuple:
    """``(ratio, level_jump_db)`` of a loop. ``ratio`` = second-difference error across the seam divided by the
    99.9th percentile of the same error inside the file (<= 1: the seam is no rougher than the material);
    ``level_jump_db`` = RMS difference between the last and the first 50 ms. Worst channel."""
    x2 = _as_2d(x)
    w = int(0.05 * SAMPLE_RATE)
    worst_ratio, worst_jump = 0.0, 0.0
    for c in range(x2.shape[1]):
        ch = x2[:, c]
        seam = max(abs(ch[0] - 2.0 * ch[-1] + ch[-2]), abs(ch[1] - 2.0 * ch[0] + ch[-1]))
        inner = np.abs(ch[2:] - 2.0 * ch[1:-1] + ch[:-2])
        ref = float(np.quantile(inner, 0.999))
        ratio = seam / ref if ref > 0.0 else (0.0 if seam == 0.0 else math.inf)
        head = math.sqrt(float(np.mean(np.square(ch[:w]))))
        tail = math.sqrt(float(np.mean(np.square(ch[-w:]))))
        jump = abs(gain_to_db(head, -120.0) - gain_to_db(tail, -120.0))
        worst_ratio, worst_jump = max(worst_ratio, ratio), max(worst_jump, jump)
    return worst_ratio, worst_jump


def analyze(x: np.ndarray, loop: bool = False, hf_cutoff: float = 8000.0, momentary_window: float = 0.2) -> dict:
    """Every metric for one signal (see the module docstring)."""
    x2 = _as_2d(x)
    centroid, hf_db = spectral_stats(x, hf_cutoff)
    result = {
        "duration": x2.shape[0] / SAMPLE_RATE,
        "channels": x2.shape[1],
        "peak_db": peak_db(x),
        "true_peak_db": true_peak_db(x),
        "rms_db": rms_db(x),
        "lufs_integrated": loudness_integrated(x),
        "lufs_momentary_max": loudness_momentary_max(x, momentary_window),
        "dc": dc_offset(x),
        "edge_db": gain_to_db(float(max(np.max(np.abs(x2[0])), np.max(np.abs(x2[-1]))))),
        "centroid_hz": centroid,
        "hf_db": hf_db,
        "hf_cutoff": hf_cutoff,
        "loop": loop,
    }
    if loop:
        result["seam_ratio"], result["seam_jump_db"] = seam_stats(x)
    return result
