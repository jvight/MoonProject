"""Conventions, musical tuning, seeded randomness and buffer helpers.

Conventions used by every module of :mod:`synth`:

* One fixed sample rate, :data:`SAMPLE_RATE` = 48000 Hz.
* Signals are ``numpy.float64`` arrays with a nominal range of -1..1. Mono is shape ``(n,)``; stereo is shape
  ``(n, 2)`` (frames x channels, the soundfile layout).
* Times are seconds, frequencies Hz, gains linear unless a name ends in ``_db``.
* Randomness only comes from :func:`rng`, so a render is identical on every run.
"""
import hashlib
import math

import numpy as np

SAMPLE_RATE = 48000
TWO_PI = 2.0 * math.pi

#: The game's key for SFX (docs/VISION.md): D major pentatonic. Music is in D major / B minor.
PENTATONIC = ("D", "E", "F#", "A", "B")

_PITCH_CLASSES = {
    "C": 0, "C#": 1, "DB": 1, "D": 2, "D#": 3, "EB": 3, "E": 4, "F": 5, "F#": 6, "GB": 6,
    "G": 7, "G#": 8, "AB": 8, "A": 9, "A#": 10, "BB": 10, "B": 11,
}


def samples(seconds: float) -> int:
    """Seconds -> whole sample count."""
    return round(seconds * SAMPLE_RATE)


def seconds(n: int) -> float:
    return n / SAMPLE_RATE


def time_axis(n: int) -> np.ndarray:
    """Sample times in seconds, ``[0, 1/sr, 2/sr, ...]``."""
    return np.arange(n, dtype=np.float64) / SAMPLE_RATE


def db_to_gain(db):
    return 10.0 ** (np.asarray(db, dtype=np.float64) / 20.0) if np.ndim(db) else 10.0 ** (db / 20.0)


def gain_to_db(gain, floor: float = -200.0):
    """Linear gain -> dB, with ``floor`` for zero/negative input."""
    if np.ndim(gain):
        g = np.asarray(gain, dtype=np.float64)
        return np.where(g > 0.0, 20.0 * np.log10(np.maximum(g, 1e-300)), floor)
    return 20.0 * math.log10(gain) if gain > 0.0 else floor


def rng(*keys) -> np.random.Generator:
    """Deterministic generator keyed by strings/ints (e.g. ``rng("scrap_chime", 3)``). Uses sha256 of the keys,
    never Python's salted ``hash()``."""
    digest = hashlib.sha256(":".join(str(k) for k in keys).encode("utf-8")).digest()
    return np.random.Generator(np.random.PCG64(int.from_bytes(digest[:16], "little")))


# ----------------------------------------------------------------------------------------------- tuning

def note_to_midi(name: str) -> int:
    """``"A4"`` -> 69, ``"F#3"`` -> 54, ``"Bb2"`` -> 46 (scientific pitch notation)."""
    text = name.strip().upper()
    split = len(text)
    while split > 0 and (text[split - 1].isdigit() or text[split - 1] == "-"):
        split -= 1
    pitch, octave = text[:split], text[split:]
    if pitch not in _PITCH_CLASSES or not octave:
        raise ValueError(f"unknown note '{name}'")
    return 12 * (int(octave) + 1) + _PITCH_CLASSES[pitch]


def midi_to_freq(midi, a4: float = 440.0):
    """Equal temperament; accepts fractional MIDI numbers (cents) and arrays."""
    return a4 * 2.0 ** ((np.asarray(midi, dtype=np.float64) - 69.0) / 12.0) if np.ndim(midi) \
        else a4 * 2.0 ** ((midi - 69.0) / 12.0)


def note_freq(name: str, a4: float = 440.0) -> float:
    """``note_freq("D5")`` -> 587.33 Hz."""
    return midi_to_freq(note_to_midi(name), a4)


def pentatonic(octave: int, degree: int) -> float:
    """Degree ``degree`` (0 = D) of D major pentatonic starting in ``octave``; degrees >= 5 climb octaves."""
    octave += degree // len(PENTATONIC)
    return note_freq(f"{PENTATONIC[degree % len(PENTATONIC)]}{octave}")


def loop_freq(freq: float, loop_samples: int) -> float:
    """Nearest frequency completing a whole number of cycles in ``loop_samples`` (exactly periodic loops)."""
    cycles = max(1, round(freq * loop_samples / SAMPLE_RATE))
    return cycles * SAMPLE_RATE / loop_samples


# ----------------------------------------------------------------------------------------------- buffers

def silence(n: int, stereo: bool = False) -> np.ndarray:
    return np.zeros((n, 2) if stereo else n, dtype=np.float64)


def is_stereo(x: np.ndarray) -> bool:
    return x.ndim == 2


def channels(x: np.ndarray) -> list:
    """List of 1-D channel arrays (views)."""
    return [x[:, c] for c in range(x.shape[1])] if x.ndim == 2 else [x]


def map_channels(x: np.ndarray, fn) -> np.ndarray:
    """Applies ``fn(1-D array) -> 1-D array`` per channel, keeping the mono/stereo shape."""
    if x.ndim == 1:
        return fn(x)
    return np.stack([fn(x[:, c]) for c in range(x.shape[1])], axis=1)


def to_mono(x: np.ndarray) -> np.ndarray:
    return x.mean(axis=1) if x.ndim == 2 else x


def to_stereo(x: np.ndarray) -> np.ndarray:
    return x if x.ndim == 2 else np.stack([x, x], axis=1)


def pan(x: np.ndarray, position: float) -> np.ndarray:
    """Equal-power pan of a mono signal, ``position`` -1 (left) .. 1 (right) -> stereo."""
    angle = (position + 1.0) * math.pi / 4.0
    return np.stack([x * math.cos(angle), x * math.sin(angle)], axis=1)


def place(dst: np.ndarray, src: np.ndarray, offset: int, gain: float = 1.0, wrap: bool = False) -> np.ndarray:
    """Adds ``src * gain`` into ``dst`` at sample ``offset`` (in place; returns ``dst``). Mono into stereo is
    duplicated to both channels. With ``wrap`` the overflow continues at index 0 (events on seamless loops);
    otherwise it is truncated (negative offsets are clipped too)."""
    if dst.ndim == 2 and src.ndim == 1:
        src = src[:, None]
    n = dst.shape[0]
    if wrap:
        pos, start = offset % n, 0
        while start < src.shape[0]:
            count = min(src.shape[0] - start, n - pos)
            dst[pos:pos + count] += gain * src[start:start + count]
            start += count
            pos = 0
        return dst
    s0, d0 = max(0, -offset), max(0, offset)
    count = min(src.shape[0] - s0, n - d0)
    if count > 0:
        dst[d0:d0 + count] += gain * src[s0:s0 + count]
    return dst


def mix(*signals) -> np.ndarray:
    """Sum of signals of possibly different lengths (zero-padded); any stereo input makes the result stereo."""
    n = max(s.shape[0] for s in signals)
    stereo = any(s.ndim == 2 for s in signals)
    out = silence(n, stereo)
    for s in signals:
        place(out, s, 0)
    return out


def normalize_peak(x: np.ndarray, peak_db: float = -1.0) -> np.ndarray:
    """Scales so the absolute sample peak sits at ``peak_db`` dBFS (silence is returned unchanged)."""
    peak = float(np.max(np.abs(x))) if x.size else 0.0
    return x * (db_to_gain(peak_db) / peak) if peak > 0.0 else x
