"""07's voice (docs/design/07-identity.md): the five-tone status annunciator (D acknowledge, E working, F# query,
A done, low B fault) delivered inside a mechanical gesture. A real motor whine is varispeeded along a contour that
lands on the tone (a query rises onto it, done falls onto it), a creaking metal squeal rides on the same tone, a relay
clicks at the onset, the small speaker cone rattles and the old tape wavers. A fault is the motor straining, then
choking. Warm, soft and slow: an old service machine, not a cartoon.
"""
from dataclasses import dataclass

import numpy as np

from synth import effects, filters
from synth.core import note_freq, samples

from . import dsp

MOTOR = "motor_whine"
SQUEAL = "metal_squeal"


@dataclass(frozen=True)
class Syllable:
    """One voiced syllable: when it starts, how long, the tone, the contour (semitones over its own time) and its
    loudness relative to the gesture."""
    at: float
    length: float
    note: str
    points: tuple
    gain: float = 1.0
    attack: float = 0.014
    release: float = 0.06


def _syllable(lib, gen, s: Syllable, squeal: float, rattle: float) -> np.ndarray:
    n = samples(s.length)
    semis = dsp.contour(n, s.points)
    motor_source = lib(MOTOR)
    offset = float(gen.uniform(0.0, len(motor_source) / 48000.0 - s.length * 2.5 - 0.1))
    motor = dsp.varispeed(motor_source, dsp.ratio_for(s.note, lib.f0(MOTOR), semis), n, offset)
    # The whine through a small, boxy speaker: the fundamental carries the tone, the upper harmonics only colour it
    # (the motor's own 4th harmonic would otherwise sing two octaves above the note).
    fundamental = note_freq(s.note)
    motor = filters.butter(filters.highpass(motor, fundamental * 0.6), "lowpass", fundamental * 1.7, order=4)
    motor = filters.peaking(motor, fundamental, 4.0, q=1.2)
    motor = dsp.speaker_rattle(dsp.peak_normalize(motor), float(gen.uniform(31.0, 39.0)), rattle)
    creak_source = lib(SQUEAL)
    creak = dsp.varispeed(creak_source, dsp.ratio_for(s.note, lib.f0(SQUEAL), semis), n,
                          float(gen.uniform(0.0, 0.6)))
    creak = filters.butter(filters.highpass(creak, fundamental * 0.7), "lowpass", fundamental * 2.2, order=2)
    body = dsp.peak_normalize(motor) + squeal * dsp.peak_normalize(creak)
    env = dsp.shape(n, [(0.0, 0.0), (s.attack, 1.0), (max(s.attack, s.length - s.release), 0.85),
                        (s.length, 0.0)])
    return s.gain * body * env


def gesture(lib, gen, syllables, length: float, click: str = "relay_click", click_gain: float = 0.22,
            squeal: float = 0.22, rattle: float = 0.28, wow: float = 9.0) -> np.ndarray:
    """Syllables laid out in time, a relay click at the first onset, then tape warble and the cabinet's colour."""
    out = np.zeros(samples(length))
    for s in syllables:
        dsp.place(out, _syllable(lib, gen, s, squeal, rattle), s.at)
    if click:
        tick = filters.lowpass(filters.highpass(dsp.peak_normalize(lib(click)), 400.0), 4200.0)
        dsp.place(out, tick, max(0.0, syllables[0].at - 0.012), click_gain)
    out = effects.wow_flutter(out, wow_cents=wow, wow_rate=0.7, flutter_cents=wow * 0.4, flutter_rate=7.0,
                              drift_cents=wow * 0.5, gen=gen)
    return filters.lowpass(out, 6000.0)


def acknowledge(lib, gen, variant: int) -> np.ndarray:
    """D: a short level 'mm' that lifts onto D and settles a hair below."""
    if variant == 0:
        return gesture(lib, gen, [Syllable(0.02, 0.24, "D5", ((0.0, -2.0), (0.05, 0.0), (0.17, 0.0), (0.24, -0.4)))],
                       0.34)
    if variant == 1:
        return gesture(lib, gen, [Syllable(0.02, 0.12, "D5", ((0.0, -1.5), (0.04, 0.0), (0.12, -0.2))),
                                  Syllable(0.17, 0.15, "D5", ((0.0, -0.6), (0.05, 0.2), (0.15, -0.3)), 0.85)], 0.40)
    return gesture(lib, gen, [Syllable(0.02, 0.3, "D4", ((0.0, -1.0), (0.06, 0.0), (0.3, -0.5)))], 0.4,
                   click="latch_thunk", rattle=0.4)


def working(lib, gen, variant: int) -> np.ndarray:
    """E: busy, a held tone that wobbles around E like a motor under load."""
    if variant == 0:
        return gesture(lib, gen, [Syllable(0.02, 0.48, "E5", ((0.0, -1.0), (0.06, 0.0), (0.15, 0.25), (0.24, -0.3),
                                                               (0.33, 0.2), (0.48, 0.0)))], 0.56)
    if variant == 1:
        return gesture(lib, gen, [Syllable(0.02, 0.16, "E5", ((0.0, -0.8), (0.05, 0.0), (0.16, 0.3))),
                                  Syllable(0.22, 0.2, "E5", ((0.0, 0.3), (0.08, -0.2), (0.2, 0.1)), 0.9)], 0.48)
    return gesture(lib, gen, [Syllable(0.02, 0.24, "E5", ((0.0, -0.5), (0.24, 0.3))),
                              Syllable(0.28, 0.18, "E5", ((0.0, 0.2), (0.18, -0.2)), 0.8),
                              Syllable(0.50, 0.22, "E5", ((0.0, -0.2), (0.22, 0.4)), 0.75)], 0.8,
                   click="count_tick", click_gain=0.12, rattle=0.34)


def query(lib, gen, variant: int) -> np.ndarray:
    """F#: rises onto F# like a question, with a small overshoot."""
    if variant == 0:
        return gesture(lib, gen, [Syllable(0.02, 0.42, "F#5", ((0.0, -4.0), (0.1, -3.2), (0.32, 0.4),
                                                                (0.42, 0.0)))], 0.5)
    if variant == 1:
        return gesture(lib, gen, [Syllable(0.02, 0.12, "D5", ((0.0, -0.5), (0.05, 0.0), (0.12, -0.3)), 0.8),
                                  Syllable(0.18, 0.3, "F#5", ((0.0, -2.0), (0.22, 0.5), (0.3, 0.3)))], 0.54)
    return gesture(lib, gen, [Syllable(0.02, 0.56, "F#5", ((0.0, -9.0), (0.22, -4.0), (0.44, 0.0),
                                                            (0.56, 1.0)), attack=0.03)], 0.66, squeal=0.3)


def done(lib, gen, variant: int) -> np.ndarray:
    """A: falls onto A and settles, the task finished."""
    if variant == 0:
        return gesture(lib, gen, [Syllable(0.02, 0.5, "A5", ((0.0, 2.0), (0.08, 1.8), (0.3, 0.0), (0.5, -0.2)))],
                       0.6)
    if variant == 1:
        return gesture(lib, gen, [Syllable(0.02, 0.14, "B5", ((0.0, -0.5), (0.04, 0.0), (0.14, -0.2))),
                                  Syllable(0.2, 0.34, "A5", ((0.0, 0.8), (0.12, 0.0), (0.34, -0.3)))], 0.62)
    return gesture(lib, gen, [Syllable(0.02, 0.74, "A5", ((0.0, 5.0), (0.08, 4.5), (0.32, 0.0), (0.74, -0.4)),
                                       release=0.2)], 0.86, squeal=0.16, wow=12.0)


def fault(lib, gen, variant: int) -> np.ndarray:
    """Low B: the motor strains up against a load, then chokes and sags; a latch drops."""
    n_len = (0.9, 0.62, 1.25)[variant]
    out = np.zeros(samples(n_len))
    if variant == 2:
        strain = [Syllable(0.02, 0.4, "B4", ((0.0, -2.5), (0.3, 0.0), (0.4, -0.4)), attack=0.03),
                  Syllable(0.48, 0.7, "B4", ((0.0, -2.0), (0.3, 0.0), (0.45, -0.3), (0.7, -6.0)), attack=0.03)]
    else:
        tail = n_len - 0.06
        strain = [Syllable(0.02, tail, "B4", ((0.0, -2.5), (tail * 0.35, 0.0), (tail * 0.55, -0.3),
                                               (tail, -6.0)), attack=0.03)]
    voice = gesture(lib, gen, strain, n_len, click="", rattle=0.45, wow=14.0)
    voice *= dsp.stutter(len(voice), gen, n_len * 0.45, 16.0, 0.75)
    dsp.place(out, voice, 0.0)
    grind = filters.lowpass(dsp.varispeed(lib("crane_motor"), 0.9, samples(n_len), float(gen.uniform(8.0, 20.0))),
                            900.0)
    dsp.place(out, 0.35 * dsp.peak_normalize(grind) * dsp.shape(len(grind), [(0.0, 0.0), (0.08, 1.0),
                                                                            (n_len * 0.8, 0.6), (n_len, 0.0)]), 0.0)
    dsp.place(out, 0.6 * dsp.peak_normalize(filters.lowpass(lib("latch_thunk"), 2500.0)), n_len * 0.72)
    return out


GESTURES = {
    "voice_ack": acknowledge,
    "voice_working": working,
    "voice_query": query,
    "voice_done": done,
    "voice_fault": fault,
}
