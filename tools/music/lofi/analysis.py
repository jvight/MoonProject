"""
Measurements of a rendered track (we cannot listen, so we measure). Loudness, true peak and spectral centroid come
from the shared core (synth.analysis); this module adds the music-specific checks: crest factor, energy balance
across low / mid / high bands, stereo correlation (whole band and below 150 Hz), clipped samples, silent gaps,
how much tonal energy falls outside the key (chroma) and how firmly the onsets lock to the declared tempo.
"""
import math

import numpy as np
from scipy.signal import stft, welch
from synth import analysis, filters
from synth.core import SAMPLE_RATE

BANDS = (("low", 20.0, 250.0), ("mid", 250.0, 4000.0), ("high", 4000.0, 20000.0))
MONO_BELOW_HZ = 150.0


def band_balance(x):
    """Share of spectral power in each band of BANDS (mono mix, Welch PSD), summing to 1 over 20 Hz..20 kHz."""
    mono = x.mean(axis=1)
    freqs, psd = welch(mono, fs=SAMPLE_RATE, window="hann", nperseg=8192)
    total = float(psd[(freqs >= BANDS[0][1]) & (freqs < BANDS[-1][2])].sum())
    return {name: float(psd[(freqs >= lo) & (freqs < hi)].sum()) / total for name, lo, hi in BANDS}


def correlation(x, below_hz=None):
    """Pearson correlation of left and right (1 = mono, 0 = unrelated, < 0 = phase trouble)."""
    if below_hz is not None:
        x = filters.butter(x, "lowpass", below_hz, order=4)
    left, right = x[:, 0] - x[:, 0].mean(), x[:, 1] - x[:, 1].mean()
    denominator = math.sqrt(float(np.dot(left, left)) * float(np.dot(right, right)))
    return float(np.dot(left, right)) / denominator if denominator > 0.0 else 1.0


def clipped_samples(x, threshold=0.999):
    return int(np.count_nonzero(np.abs(x) >= threshold))


def silent_gaps(x, window_s=0.05, floor_db=-60.0, head_s=0.5, tail_s=1.0):
    """
    [(start_s, length_s)] of runs of `window_s` windows quieter than `floor_db` RMS, ignoring the first `head_s`
    and the last `tail_s` seconds (where a track may begin and end in silence).
    """
    window = int(window_s * SAMPLE_RATE)
    count = x.shape[0] // window
    power = np.square(x[:count * window]).mean(axis=1).reshape(count, window).mean(axis=1)
    quiet = 10.0 * np.log10(np.maximum(power, 1e-20)) < floor_db
    quiet[:int(head_s / window_s)] = False
    quiet[count - int(tail_s / window_s):] = False
    gaps = []
    start = None
    for index, flag in enumerate(quiet.tolist() + [False]):
        if flag and start is None:
            start = index
        elif not flag and start is not None:
            gaps.append((start * window_s, (index - start) * window_s))
            start = None
    return gaps


def chroma(x, low_hz=110.0, high_hz=4000.0):
    """Share of spectral power per pitch class (index 0 = C) between `low_hz` and `high_hz`, mono mix."""
    freqs, psd = welch(x.mean(axis=1), fs=SAMPLE_RATE, window="hann", nperseg=32768)
    band = (freqs >= low_hz) & (freqs <= high_hz)
    classes = np.round(69.0 + 12.0 * np.log2(freqs[band] / 440.0)).astype(int) % 12
    energy = np.bincount(classes, weights=psd[band], minlength=12)
    return energy / energy.sum()


def chroma_out_of_key(x, key_pcs):
    """Share of tonal power on pitch classes outside `key_pcs` (~0.42 for noise, a few % for music in key)."""
    shares = chroma(x)
    return float(sum(shares[pc] for pc in range(12) if pc not in key_pcs))


def beat_clarity(x, bpm, offset_s, bins=16):
    """
    Spectral-flux onsets folded onto the beat period: the loudest phase bin over the mean bin. ~1 when onsets
    do not follow `bpm` (a 3 % tempo error already folds to ~1.03); clearly above 1 when the groove locks to it.
    Returns (ratio, phase of the loudest bin in beats).
    """
    hop = 240
    _, _, spectrum = stft(x.mean(axis=1), fs=SAMPLE_RATE, nperseg=1024, noverlap=1024 - hop, boundary=None)
    log_mag = np.log1p(100.0 * np.abs(spectrum))
    flux = np.maximum(0.0, np.diff(log_mag, axis=1)).sum(axis=0)
    times = (np.arange(flux.shape[0]) + 1) * hop / SAMPLE_RATE + 512 / SAMPLE_RATE
    phase_bins = (((times - offset_s) * bpm / 60.0) % 1.0 * bins).astype(int) % bins
    profile = np.bincount(phase_bins, weights=flux, minlength=bins) / np.bincount(phase_bins, minlength=bins)
    return float(profile.max() / profile.mean()), float(np.argmax(profile)) / bins


def measure(x, key_pcs, bpm, offset_s):
    """Every metric the quality gates and the per-track report use (`offset_s`: time of beat 1)."""
    centroid, hf_db = analysis.spectral_stats(x, 8000.0)
    peak = analysis.peak_db(x)
    rms = analysis.rms_db(x)
    return {
        "duration_s": x.shape[0] / SAMPLE_RATE,
        "loudness_lufs": analysis.loudness_integrated(x),
        "true_peak_dbtp": analysis.true_peak_db(x),
        "sample_peak_db": peak,
        "rms_db": rms,
        "crest_db": peak - rms,
        "centroid_hz": centroid,
        "above_8k_db": hf_db,
        "bands": band_balance(x),
        "correlation": correlation(x),
        "correlation_below_150hz": correlation(x, MONO_BELOW_HZ),
        "clipped_samples": clipped_samples(x),
        "silent_gaps": silent_gaps(x),
        "dc": analysis.dc_offset(x),
        "chroma_out_of_key": chroma_out_of_key(x, key_pcs),
        "beat_clarity": beat_clarity(x, bpm, offset_s)[0],
    }
