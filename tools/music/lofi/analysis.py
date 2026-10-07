"""
Measurements of a rendered track (we cannot listen, so we measure). Loudness, true peak and spectral centroid come
from the shared core (synth.analysis); this module adds the music-specific checks: crest factor, energy balance
across low / mid / high bands, stereo correlation (whole band and below 150 Hz), clipped samples, silent gaps,
how much tonal energy falls outside the key (chroma) and how firmly the onsets lock to the declared tempo.

The listening report adds checks of musical intent: the tempo found blind from the onsets, the scale found from the
chroma (the diatonic collection holding the most tonal energy), short-term loudness over
time, the spectral balance per octave, the seam of a looping tape, and the notes of a jingle (pitch-detected from the
energy each onset adds). Tonal-hierarchy key profiles are not used: these pentatonic-heavy, extended-chord mixes
fool them (they hear A major in tracks whose G# energy is nil).
"""
import math

import numpy as np
from scipy.signal import resample_poly, stft, welch
from synth import analysis, filters
from synth.core import SAMPLE_RATE

BANDS = (("low", 20.0, 250.0), ("mid", 250.0, 4000.0), ("high", 4000.0, 20000.0))
MONO_BELOW_HZ = 150.0
TEMPO_SEARCH_BPM = (55.0, 100.0)
TEMPO_STEP_BPM = 0.05
SHORT_TERM_S = 3.0
SHORT_TERM_HOP_S = 1.0
LOUDNESS_GATE_LUFS = -70.0
OCTAVE_CENTRES_HZ = (31.5, 63.0, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0)
SEAM_WINDOW_S = 3.0
NOTE_NAMES = ("C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B")
_MAJOR_STEPS = (0, 2, 4, 5, 7, 9, 11)


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
    return _fold(*onset_flux(x), bpm, offset_s, bins)


def onset_flux(x):
    """(times in seconds, spectral flux) of the mono mix: positive log-magnitude change per 5 ms hop."""
    hop = 240
    _, _, spectrum = stft(x.mean(axis=1), fs=SAMPLE_RATE, nperseg=1024, noverlap=1024 - hop, boundary=None)
    log_mag = np.log1p(100.0 * np.abs(spectrum))
    flux = np.maximum(0.0, np.diff(log_mag, axis=1)).sum(axis=0)
    times = (np.arange(flux.shape[0]) + 1) * hop / SAMPLE_RATE + 512 / SAMPLE_RATE
    return times, flux


def _fold(times, flux, bpm, offset_s, bins):
    phase_bins = (((times - offset_s) * bpm / 60.0) % 1.0 * bins).astype(int) % bins
    profile = np.bincount(phase_bins, weights=flux, minlength=bins) / np.bincount(phase_bins, minlength=bins)
    return float(profile.max() / profile.mean()), float(np.argmax(profile)) / bins


def estimate_tempo(x, search=TEMPO_SEARCH_BPM, step=TEMPO_STEP_BPM, bins=16):
    """
    The tempo found blind: the BPM in `search` whose beat period folds the onsets most sharply (the beat_clarity
    ratio, maximised). The range spans the catalogue an octave wide, so halves and doubles of a tempo fall outside.
    Returns (bpm, clarity at that bpm).
    """
    times, flux = onset_flux(x)
    candidates = np.arange(search[0], search[1] + 0.5 * step, step)
    clarity = [_fold(times, flux, float(bpm), 0.0, bins)[0] for bpm in candidates]
    best = int(np.argmax(clarity))
    return float(candidates[best]), float(clarity[best])


def detect_scale(x):
    """The diatonic collection (named by its major key) holding the most tonal energy, its share and the runner-up."""
    shares = chroma(x)
    fits = sorted(((float(sum(shares[(tonic + step) % 12] for step in _MAJOR_STEPS)), NOTE_NAMES[tonic])
                   for tonic in range(12)), reverse=True)
    return {"scale": f"{fits[0][1]} major", "share": fits[0][0], "runner_up": f"{fits[1][1]} major",
            "runner_up_share": fits[1][0]}


def short_term_loudness(x, window_s=SHORT_TERM_S, hop_s=SHORT_TERM_HOP_S):
    """BS.1770 short-term loudness (LUFS) of `window_s` windows every `hop_s` seconds."""
    window, hop = int(window_s * SAMPLE_RATE), int(hop_s * SAMPLE_RATE)
    weighted = np.square(filters.k_weight(x)).sum(axis=1)
    prefix = np.concatenate(([0.0], np.cumsum(weighted)))
    starts = np.arange(0, max(1, weighted.shape[0] - window + 1), hop)
    means = (prefix[np.minimum(starts + window, weighted.shape[0])] - prefix[starts]) / window
    return -0.691 + 10.0 * np.log10(np.maximum(means, 1e-20))


def loudness_profile(x):
    """Short-term loudness summary: range (95th - 10th percentile, gated), extremes and a 10-second curve."""
    curve = short_term_loudness(x)
    gated = curve[curve > LOUDNESS_GATE_LUFS]
    return {
        "range_lu": float(np.percentile(gated, 95) - np.percentile(gated, 10)),
        "min_lufs": float(gated.min()),
        "max_lufs": float(gated.max()),
        "every_10s_lufs": [float(v) for v in curve[::10]],
    }


def octave_balance(x):
    """Power per octave band (centres OCTAVE_CENTRES_HZ) in dB relative to the loudest band, mono mix."""
    freqs, psd = welch(x.mean(axis=1), fs=SAMPLE_RATE, window="hann", nperseg=8192)
    powers = []
    for centre in OCTAVE_CENTRES_HZ:
        band = (freqs >= centre / math.sqrt(2.0)) & (freqs < centre * math.sqrt(2.0))
        powers.append(float(psd[band].sum()))
    loudest = max(powers)
    return {f"{centre:g}": 10.0 * math.log10(max(p, 1e-30) / loudest) for centre, p in zip(OCTAVE_CENTRES_HZ, powers)}


def seam(x):
    """
    How a loop sounds where it wraps: the core's seam statistics (second-difference error across the seam over the
    file's own 99.9th percentile, and the level jump between the last and first 50 ms) plus the short-term
    loudness difference between the last and the first SEAM_WINDOW_S seconds.
    """
    ratio, jump_db = analysis.seam_stats(x)
    window = int(SEAM_WINDOW_S * SAMPLE_RATE)
    head = analysis.loudness_integrated(x[:window])
    tail = analysis.loudness_integrated(x[-window:])
    return {"ratio": ratio, "jump_db": jump_db, "loudness_step_lu": abs(head - tail)}


def to_core_rate(x, rate):
    """Resample a decoded file to the core's rate (the K-weighting and chroma are defined at 48 kHz)."""
    if rate == SAMPLE_RATE:
        return x
    divisor = math.gcd(int(rate), SAMPLE_RATE)
    return resample_poly(x, SAMPLE_RATE // divisor, int(rate) // divisor, axis=0)


def onset_note(x, rate, onset_s, low_hz, high_hz, before_s=0.08, after_s=(0.02, 0.12)):
    """
    MIDI pitch (fractional) of the note struck at `onset_s`: the strongest peak between `low_hz` and `high_hz` in
    the magnitude the onset adds (spectrum just after minus spectrum just before), so earlier notes still ringing
    do not count.
    """
    mono = x if x.ndim == 1 else x.mean(axis=1)
    size = 1 << 17

    def spectrum(start_s, stop_s):
        segment = mono[max(0, int(start_s * rate)):int(stop_s * rate)]
        return np.abs(np.fft.rfft(segment * np.hanning(segment.shape[0]), size)) / max(1, segment.shape[0])

    before = spectrum(onset_s - before_s, onset_s) if onset_s >= before_s else 0.0
    added = np.maximum(0.0, spectrum(onset_s + after_s[0], onset_s + after_s[1]) - before)
    freqs = np.fft.rfftfreq(size, 1.0 / rate)
    band = np.nonzero((freqs >= low_hz) & (freqs <= high_hz))[0]
    k = band[np.argmax(added[band])]
    a, b, c = np.log(added[k - 1:k + 2] + 1e-30)
    curvature = a - 2.0 * b + c
    offset = 0.5 * (a - c) / curvature if curvature != 0.0 else 0.0
    return 69.0 + 12.0 * math.log2((k + offset) * rate / size / 440.0)


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
