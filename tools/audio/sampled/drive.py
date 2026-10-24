"""07 driving, from recorded sources in place of the additive synth hum that read as electronic, not physical.

The drive motor: the mains hum carried to D2 for the motor's body, a real small motor slowed far down for its gear
whir, and the crane's gear motor faintly under both for weight, all under 2.5 kHz so the runtime's pitch 0.5..2 (by
speed and throttle) stays clean. The boost: the capacitor drums spinning up, a real small motor held on A4.
"""
import numpy as np

from synth import filters
from synth.core import note_freq, samples

from . import dsp, parts

HUM_LOOP_S = 4.0
BOOST_LOOP_S = 2.0
LOOP_SEAM_S = 0.35


def rover_hum(lib, gen, _variant: int) -> np.ndarray:
    """07's drive motor: a warm mains hum on D2, a slowed motor's gear whir on D4 and the crane's gear for weight,
    with a slow sway in its level. Seamless over 4 s."""
    n = samples(HUM_LOOP_S + LOOP_SEAM_S)
    body = parts.hum(lib, n, 0.5, float(gen.uniform(1.0, 6.0)))
    f = note_freq("D4")
    whir = parts.band(parts.motor(lib, n, "D4"), f * 0.5, f * 2.5)
    weight = parts.band(parts.gear(lib, gen, n, "D4"), 60.0, 700.0)
    t = np.arange(n) / (n - samples(LOOP_SEAM_S))
    sway = 0.92 + 0.08 * np.sin(2.0 * np.pi * 2.0 * t)
    mix = (0.75 * body + 0.3 * whir + 0.25 * weight) * sway
    mix = filters.butter(mix, "lowpass", 2500.0, order=4)
    return dsp.seamless(mix, samples(HUM_LOOP_S), samples(LOOP_SEAM_S))


def boost_whine(lib, gen, _variant: int) -> np.ndarray:
    """The capacitor drums boosting: a real small motor spinning on A4 through a narrow speaker, breathing slowly.
    Seamless over 2 s."""
    n = samples(BOOST_LOOP_S + LOOP_SEAM_S)
    f = note_freq("A4")
    whine = parts.band(parts.motor(lib, n, "A4"), f * 0.6, f * 2.2)
    t = np.arange(n) / (n - samples(LOOP_SEAM_S))
    breath = 0.85 + 0.15 * np.sin(2.0 * np.pi * 2.0 * t)
    return dsp.seamless(whine * breath, samples(BOOST_LOOP_S), samples(LOOP_SEAM_S))


# Cue id -> (recipe, variants).
SOUNDS = {
    "rover_hum": (rover_hum, 1),
    "boost_whine": (boost_whine, 1),
}
