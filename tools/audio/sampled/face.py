"""07's face booting, heard from 07, from recorded sources: the picture tube powering on (a degauss thunk and the
high-voltage whine rising and settling), the maker's mark traced stroke by stroke (a faint beam hum per stroke on the
D major pentatonic, a gentle resolve when the mark is whole), the readout typing (a counting machine's soft, dry tick,
varied and quiet so a 7 s run never tires), each line's heavier tick, and the load steps (small ratchet steps, the
last one resolving onto D). Never a beep: every tone is a recorded metal or motor carried onto its note."""
import numpy as np

from synth import effects, filters
from synth.core import samples, to_mono

from . import dsp, parts

STROKE_NOTES = ("D4", "E4", "F#4", "A4", "B4", "D5")
TEXT_TICKS = 4
LOAD_STEPS = 5


def _glow(lib, gen, n: int, note: str, points, lp: float) -> np.ndarray:
    """The bowed metal carried onto ``note`` along ``points`` (semitones): a soft tonal hum, band-limited."""
    rate = dsp.ratio_for(note, lib.f0("bowed_metal"), dsp.contour(n, points))
    x = dsp.varispeed(lib("bowed_metal"), rate, n, float(gen.uniform(0.0, 1.0)))
    return dsp.peak_normalize(filters.butter(filters.highpass(x, 80.0), "lowpass", lp, order=2))


def power_on(lib, gen, _variant: int) -> np.ndarray:
    """The picture tube coming on: the degauss coil's thunk with the steel's ring on D2, then the high-voltage whine
    rising onto D6 and settling soft."""
    n = samples(1.6)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1600.0, 60.0, 0.75), 0.0, 0.5)
    ring = parts.clip(lib, "metal_thud", 600.0, 62.0, 1.012)[:samples(0.6)]
    dsp.place(out, ring * dsp.shape(len(ring), [(0.0, 1.0), (0.6, 0.0)]), 0.0, 0.3)
    whine_n = samples(1.4)
    whine = parts.servo(lib, gen, whine_n, "D6", ((0.0, -7.0), (0.6, 0.5), (1.0, 0.0)), 1.0)
    dsp.place(out, whine * dsp.shape(whine_n, [(0.0, 0.0), (0.3, 1.0), (0.8, 0.5), (1.4, 0.0)]), 0.12, 0.1)
    return out


def logo_stroke(lib, gen, variant: int) -> np.ndarray:
    """One stroke of the maker's mark traced by the beam: a faint hum swelling and fading on its step of the
    pentatonic (variant k: stroke k, D4 E4 F#4 A4 B4 D5, climbing as the mark completes)."""
    n = samples(0.45)
    glow = _glow(lib, gen, n, STROKE_NOTES[variant], ((0.0, -0.5), (0.15, 0.0), (0.45, 0.0)), 2200.0)
    return glow * dsp.shape(n, [(0.0, 0.0), (0.08, 1.0), (0.25, 0.6), (0.45, 0.0)])


def logo_resolve(lib, gen, _variant: int) -> np.ndarray:
    """The mark whole: a gentle resolve on D, the bowed metal and its fifth easing in and out."""
    n = samples(1.5)
    swell = dsp.shape(n, [(0.0, 0.0), (0.35, 1.0), (0.8, 0.8), (1.5, 0.0)])
    body = _glow(lib, gen, n, "D4", ((0.0, -0.3), (0.3, 0.0), (1.5, 0.0)), 2000.0)
    fifth = _glow(lib, gen, n, "A4", ((0.0, -0.3), (0.3, 0.0), (1.5, 0.0)), 2400.0)
    return to_mono(effects.reverb((body + 0.5 * fifth) * swell, room=0.5, damping=0.6, wet=0.15))


def text_tick(lib, gen, variant: int) -> np.ndarray:
    """One typed character pair: the counting machine's soft dry tick, each variant a touch different in pitch and
    brightness (chosen at random, so a long readout never repeats itself)."""
    n = samples(0.08)
    out = np.zeros(n)
    rate = (0.92, 1.0, 1.08, 1.16)[variant]
    dsp.place(out, parts.clip(lib, "count_tick", 2200.0 + 150.0 * variant, 450.0, rate), 0.0, 0.7)
    return out


def line_done(lib, gen, _variant: int) -> np.ndarray:
    """A readout line complete: a slightly heavier tick, the counter settling with a relay's contact."""
    n = samples(0.25)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "count_tick", 1900.0, 250.0, 0.85), 0.0, 0.6)
    dsp.place(out, parts.clip(lib, "relay_click", 1800.0, 150.0, 0.8), 0.012, 0.3)
    return out


def load_step(lib, gen, variant: int) -> np.ndarray:
    """A load step: a small ratchet step, a touch higher each time (variants 1..4), and the fifth resolving: the
    latch and a breath of hum on D (variant 5)."""
    n = samples(0.6 if variant == LOAD_STEPS - 1 else 0.2)
    out = np.zeros(n)
    if variant < LOAD_STEPS - 1:
        rate = 0.9 + 0.06 * variant
        notch = parts.clip(lib, "typewriter_carriage", 2000.0, 250.0, rate)[:samples(0.12)]
        dsp.place(out, notch * dsp.shape(len(notch), [(0.0, 1.0), (0.12, 0.0)]), 0.0, 0.6)
        dsp.place(out, parts.clip(lib, "count_tick", 1800.0, 300.0, rate), 0.03, 0.25)
    else:
        dsp.place(out, parts.clip(lib, "latch_thunk", 1800.0, 80.0, 0.9), 0.0, 0.5)
        hum_n = samples(0.55)
        hum = parts.hum(lib, hum_n, 1.0, float(gen.uniform(1.0, 6.0)))
        dsp.place(out, hum * dsp.shape(hum_n, [(0.0, 0.0), (0.1, 1.0), (0.55, 0.0)]), 0.03, 0.35)
    return out


# Cue id -> (recipe, variants). Every variant ships: one per stroke, per text tick (random) and per load step.
SOUNDS = {
    "face_power_on": (power_on, 1),
    "face_logo_stroke": (logo_stroke, len(STROKE_NOTES)),
    "face_logo_resolve": (logo_resolve, 1),
    "face_text_tick": (text_tick, TEXT_TICKS),
    "face_line_done": (line_done, 1),
    "face_load_step": (load_step, LOAD_STEPS),
}
