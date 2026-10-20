"""The sky's moments from recorded sources: the eclipse's diamond ring (docs/design/opening-v2.md B6), the Sun breaking
out behind Earth on 07's first charge. Not a synth pad: bowed and scraped metal carried slowly up onto D, its
harmonics opening like light, with a breath of the old creak at the start. Very soft attack, mezzo-piano at most.
The first dusk (opening v3 R6, OpeningDuskBegan): a soft low swell on D as the Sun sinks to last light, a scraped
drone under bowed metal, easing in and away over eight seconds; no stinger."""
import numpy as np

from synth import effects, filters
from synth.core import samples

from . import dsp

RING_S = 5.0
DUSK_S = 8.0


def _glow(lib, gen, n: int, name: str, note: str, points, lp: float) -> np.ndarray:
    """A tonal metal source on ``note`` along ``points`` (semitones), band-limited and peak-normalised."""
    rate = dsp.ratio_for(note, lib.f0(name), dsp.contour(n, points))
    x = dsp.varispeed(lib(name), rate, n, float(gen.uniform(0.0, 1.0)))
    return dsp.peak_normalize(filters.butter(filters.highpass(x, 50.0), "lowpass", lp, order=2))


def _breath(lib, gen, n: int) -> np.ndarray:
    """The creak's breath: the dry-ice metal squeal slowed far down to a soft, airy groan."""
    x = dsp.varispeed(lib("metal_squeal"), 0.35, n, float(gen.uniform(0.0, 0.5)))
    return dsp.peak_normalize(filters.butter(filters.highpass(x, 200.0), "lowpass", 1800.0, order=2))


def diamond_ring(lib, gen, variant: int) -> np.ndarray:
    """The Sun breaking out behind Earth: a breath of creak, then a long glow rising onto D and opening upward
    (variant 1: the bowed metal and its octave; variant 2: a low scraped drone under it, a soft fifth, a high
    harmonic on top)."""
    n = samples(RING_S)
    swell = dsp.shape(n, [(0.0, 0.0), (1.6, 0.55), (2.8, 1.0), (3.6, 0.85), (RING_S, 0.0)])
    rise = ((0.0, -5.0), (2.6, 0.0), (RING_S, 0.0))
    if variant == 0:
        body = _glow(lib, gen, n, "bowed_metal", "D3", rise, 1600.0)
        octave = _glow(lib, gen, n, "bowed_metal", "D4", rise, 2600.0)
        bloom = dsp.shape(n, [(0.0, 0.0), (2.2, 0.0), (3.2, 1.0), (RING_S, 0.0)])
        out = (body + 0.45 * octave * bloom) * swell
    else:
        floor = _glow(lib, gen, n, "metal_drone_low", "D2", ((0.0, -2.0), (2.6, 0.0), (RING_S, 0.0)), 900.0)
        body = _glow(lib, gen, n, "bowed_metal", "D3", rise, 2000.0)
        fifth = _glow(lib, gen, n, "bowed_metal", "A3", rise, 2000.0)
        light = _glow(lib, gen, n, "bowed_metal", "D5", ((0.0, -7.0), (2.8, 0.0), (RING_S, 0.0)), 3200.0)
        bloom = dsp.shape(n, [(0.0, 0.0), (2.4, 0.0), (3.3, 1.0), (RING_S, 0.0)])
        out = (0.6 * floor + body + 0.35 * fifth + 0.25 * light * bloom) * swell
    breath_n = samples(2.0)
    breath = _breath(lib, gen, breath_n) * dsp.shape(breath_n, [(0.0, 0.0), (0.8, 1.0), (2.0, 0.0)])
    dsp.place(out, breath, 0.0, 0.25)
    return effects.reverb(out, room=0.8, damping=0.55, wet=0.22)


def dusk_swell(lib, gen, _variant: int) -> np.ndarray:
    """The first dusk: a low scraped drone on D2 and the bowed metal on D3 settling a breath downward, swelling in
    slowly and easing away, the light going long."""
    n = samples(DUSK_S)
    swell = dsp.shape(n, [(0.0, 0.0), (3.0, 1.0), (4.5, 0.85), (DUSK_S, 0.0)])
    floor = _glow(lib, gen, n, "metal_drone_low", "D2", ((0.0, 0.0), (DUSK_S, 0.0)), 700.0)
    body = _glow(lib, gen, n, "bowed_metal", "D3", ((0.0, 0.5), (4.0, 0.0), (DUSK_S, -0.3)), 1400.0)
    out = (0.7 * floor + 0.5 * body) * swell
    return effects.reverb(out, room=0.85, damping=0.6, wet=0.25)


# Cue id -> (recipe, variants).
SOUNDS = {
    "sky_diamond_ring": (diamond_ring, 2),
    "sky_dusk_swell": (dusk_swell, 1),
}
