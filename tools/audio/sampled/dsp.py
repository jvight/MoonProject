"""Sample processing for the sampled recipes: loading slices of the library, varispeed along a pitch contour,
envelopes, placement and the shared finishing chain."""
import math
from pathlib import Path

import librosa
import numpy as np

from synth import analysis, effects, envelope, filters
from synth.core import SAMPLE_RATE, note_freq, samples

from .sources import SOURCES

EDGE_FADE_S = 0.003
HEADROOM_DB = -1.2


class Library:
    """Reads the source slices from the library folder (resampled to 48 kHz mono, DC removed, edges faded)."""

    def __init__(self, root):
        self._root = Path(root)
        if not self._root.is_dir():
            raise FileNotFoundError(f"sound library folder not found: {self._root}")
        self._cache = {}

    def __call__(self, name: str) -> np.ndarray:
        if name not in self._cache:
            source = SOURCES[name]
            path = self._root / source.path
            if not path.is_file():
                raise FileNotFoundError(f"source '{name}' missing from the library: {path}")
            y, _ = librosa.load(str(path), sr=SAMPLE_RATE, mono=True, offset=source.start, duration=source.length)
            y = filters.dc_block(np.asarray(y, dtype=np.float64))
            self._cache[name] = envelope.fade_out(envelope.fade_in(y, EDGE_FADE_S), EDGE_FADE_S)
        return self._cache[name]

    def f0(self, name: str) -> float:
        return SOURCES[name].f0


def contour(n: int, points, shape: str = "smooth") -> np.ndarray:
    """Semitones over time from breakpoints ``[(seconds, semitones), ...]``."""
    return envelope.segments(n, points, shape=shape)


def ratio_for(note: str, f0: float, semitones: np.ndarray) -> np.ndarray:
    """Playback-rate curve that puts a source of fundamental ``f0`` on ``note`` offset by ``semitones``."""
    return (note_freq(note) / f0) * 2.0 ** (semitones / 12.0)


def varispeed(x: np.ndarray, ratio, n: int, offset: float = 0.0) -> np.ndarray:
    """Plays ``x`` from ``offset`` seconds at a per-sample rate ``ratio`` (tape varispeed: pitch and texture move
    together), ``n`` samples long; stops in silence at the end of the source."""
    rate = np.broadcast_to(np.asarray(ratio, dtype=np.float64), (n,))
    pos = samples(offset) + np.concatenate(([0.0], np.cumsum(rate[:-1])))
    out = np.interp(pos, np.arange(len(x)), x, right=0.0)
    out[pos >= len(x) - 1] = 0.0
    return out


def shape(n: int, points, curve: str = "smooth") -> np.ndarray:
    """Gain breakpoints ``[(seconds, gain), ...]``."""
    return envelope.segments(n, points, shape=curve)


def place(dst: np.ndarray, src: np.ndarray, at: float, gain: float = 1.0) -> None:
    start = samples(at)
    if start >= len(dst):
        return
    end = min(len(dst), start + len(src))
    dst[start:end] += gain * src[:end - start]


def fit(x: np.ndarray, n: int) -> np.ndarray:
    """``x`` cut or zero-padded to ``n`` samples."""
    return x[:n] if len(x) >= n else np.concatenate((x, np.zeros(n - len(x))))


def speaker_rattle(x: np.ndarray, rate: float, depth: float, drive: float = 1.6) -> np.ndarray:
    """A small speaker cone buzzing against its frame: a fast amplitude flutter and a soft overdrive."""
    return effects.saturate(effects.tremolo(x, rate, depth), drive) / max(1.0, drive * 0.6)


def stutter(n: int, gen: np.random.Generator, start: float, rate: float, depth: float) -> np.ndarray:
    """An irregular choke from ``start``: a motor losing grip, gaps at about ``rate`` per second."""
    gain = np.ones(n)
    t = start
    while t < n / SAMPLE_RATE:
        gap = float(gen.uniform(0.35, 1.0)) / rate
        hole = samples(gap * float(gen.uniform(0.25, 0.55)))
        i = samples(t)
        gain[i:i + hole] *= 1.0 - depth
        t += gap
    return filters.onepole_lp(gain, 180.0)


def peak_normalize(x: np.ndarray, peak_db: float = -6.0) -> np.ndarray:
    peak = float(np.max(np.abs(x))) if x.size else 0.0
    return x if peak <= 0.0 else x * (10.0 ** (peak_db / 20.0) / peak)


def finish(x: np.ndarray, target_lufs: float, loop: bool = False, room: float = 0.0) -> np.ndarray:
    """DC removal, a warm high cut, an optional small room, click-free edges, loudness to ``target_lufs`` (loudest
    400 ms for one-shots, integrated for loops) and a safety limiter."""
    y = filters.dc_block(np.asarray(x, dtype=np.float64))
    y = filters.highpass(y, 45.0)
    if room > 0.0:
        y = effects.reverb(y, room=room, damping=0.6, wet=0.12)
    if not loop:
        y = envelope.fade_out(envelope.fade_in(y, 0.0015), min(0.03, len(y) / SAMPLE_RATE * 0.1))
    level = analysis.loudness_integrated(y) if loop else analysis.loudness_momentary_max(y)
    if math.isfinite(level):
        y = y * 10.0 ** ((target_lufs - level) / 20.0)
    if float(np.max(np.abs(y))) > 10.0 ** (HEADROOM_DB / 20.0):
        y = effects.limiter(y, ceiling_db=HEADROOM_DB - 0.2, release=0.03)
    return y
