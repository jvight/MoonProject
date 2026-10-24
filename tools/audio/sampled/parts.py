"""Shared parts of the sampled recipes: the mains hum carried onto D, a small servo (the motor whine on a note), a
small motor held steady on a note, struck metal ringing on a note (and a check that keeps it in tune), the crane's gear
motor and a filtered clip of any source."""
import numpy as np

from analyze_audio import dominant_pitch
from synth import filters
from synth.core import note_freq, note_to_midi, samples

from . import dsp

D3_HZ = 146.83
# The small motor's steady stretch: its level stays within 3 dB from here on (its first seconds swell and dip).
MOTOR_OFFSET_S = 5.0
# Where the bow sits closest to its pitch (it drifts flat later, which in_tune measures and takes out).
RING_OFFSET_S = 1.0


def hum(lib, n: int, ratio, offset: float = 2.0) -> np.ndarray:
    """The mains hum (100 Hz, a strong 200 Hz) carried to D at ``ratio`` x 1.464 (D3 and its octave)."""
    x = dsp.varispeed(lib("fridge_hum"), np.asarray(ratio) * (D3_HZ / lib.f0("fridge_hum")), n, offset)
    return dsp.peak_normalize(filters.lowpass(filters.highpass(x, 70.0), 1400.0))


def servo(lib, gen, n: int, note: str, points, gain: float = 1.0) -> np.ndarray:
    """A small servo: the motor whine on ``note`` along ``points`` (semitones), through a narrow speaker."""
    semis = dsp.contour(n, points)
    whine = dsp.varispeed(lib("motor_whine"), dsp.ratio_for(note, lib.f0("motor_whine"), semis), n,
                          float(gen.uniform(0.0, 4.0)))
    f = note_freq(note)
    whine = filters.butter(filters.highpass(whine, f * 0.6), "lowpass", f * 2.2, order=4)
    return gain * dsp.peak_normalize(whine)


def band(x: np.ndarray, low: float, high: float) -> np.ndarray:
    """``x`` band-limited to ``low``..``high`` Hz (a steep top), peak-normalised."""
    return dsp.peak_normalize(filters.butter(filters.highpass(x, low), "lowpass", high, order=4))


def motor(lib, n: int, note: str) -> np.ndarray:
    """The small motor's steady stretch held on ``note`` (raw, not yet band-limited)."""
    return dsp.varispeed(lib("motor_whine"), dsp.ratio_for(note, lib.f0("motor_whine"), np.zeros(n)), n,
                         MOTOR_OFFSET_S)


def ring(lib, note: str, seconds: float, cents: float = 0.0) -> np.ndarray:
    """Struck metal ringing on ``note`` (``cents`` sharp of it) for ``seconds``: the bow carried onto the note, a
    quick strike and a decay, band-limited around the note. A high note reads the bow fast, so the ring stops where
    the bow does."""
    f = note_freq(note)
    n = samples(seconds)
    rate = float(dsp.ratio_for(note, lib.f0("bowed_metal"), np.asarray(cents / 100.0)))
    left = (len(lib("bowed_metal")) - samples(RING_OFFSET_S)) / rate
    length = min(seconds, 0.95 * left / samples(1.0))
    x = band(dsp.varispeed(lib("bowed_metal"), rate, n, RING_OFFSET_S), f * 0.7, f * 1.6)
    return x * dsp.shape(n, [(0.0, 0.0), (0.012, 1.0), (0.23 * length, 0.45), (length, 0.0), (seconds, 0.0)])


def in_tune(build, note: str):
    """``build(cents)`` struck once, measured against ``note``, and struck again with its error taken out, so a
    recipe's dominant pitch lands on its note however the bow drifts under it (measured from the note itself, so a
    ring that starts nearer a neighbour is still pulled onto its own note)."""
    first = build(0.0)
    heard, cents, _ = dominant_pitch(first)
    error = 100.0 * (note_to_midi(heard) - note_to_midi(note)) + cents
    return build(-error) if abs(error) > 1.0 else first


def gear(lib, gen, n: int, note: str = "A5") -> np.ndarray:
    """The crane's gear motor (a real low motor gliding 90-160 Hz), its own 791 Hz whine tuned onto ``note``."""
    x = dsp.varispeed(lib("crane_motor"), note_freq(note) / lib.f0("crane_motor"), n, float(gen.uniform(0.0, 20.0)))
    return dsp.peak_normalize(filters.lowpass(filters.highpass(x, 60.0), 1500.0))


def clip(lib, name: str, lp: float = 6000.0, hp: float = 60.0, rate: float = 1.0) -> np.ndarray:
    """Source ``name`` band-limited to ``hp``..``lp`` Hz, varispeeded by ``rate``, peak-normalised."""
    x = lib(name) if rate == 1.0 else dsp.varispeed(lib(name), rate, int(len(lib(name)) / rate))
    return dsp.peak_normalize(filters.butter(filters.highpass(x, hp), "lowpass", lp, order=4))
