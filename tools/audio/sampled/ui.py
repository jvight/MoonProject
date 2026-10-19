"""The databank on 07's face from recorded sources: a relay closing as a page starts to flip, a picture tube's channel
settling with a soft thunk and a breath of its hum on D, and a tape head stepping one line down the list. Small,
close and warm: switch gear and latches with their highs taken off, nothing brighter than a desk lamp's click."""
import numpy as np

from synth.core import samples

from . import dsp, parts


def relay_click(lib, gen, variant: int) -> np.ndarray:
    """A page starting to flip: a small relay pulling in, its contact settling a breath later (variant 1: a heavier
    relay, slowed, with the vintage switch's body under it)."""
    n = samples(0.22)
    out = np.zeros(n)
    rate = (1.0, 0.82)[variant]
    dsp.place(out, parts.clip(lib, "relay_click", 3400.0, 180.0, rate), 0.0, 0.8)
    dsp.place(out, parts.clip(lib, "count_tick", 2600.0, 400.0, 1.15), 0.028 + 0.004 * variant, 0.18)
    if variant == 1:
        dsp.place(out, parts.clip(lib, "vintage_click", 2200.0, 150.0, 0.9), 0.004, 0.3)
    return out


def tube_thunk(lib, gen, variant: int) -> np.ndarray:
    """The picture tube settling on a channel: a soft latch thunk with the steel's low ring on D2 and a short breath of
    the tube's hum on D3 easing out (variant 1: the hum swells in a hair later and rises onto A as it settles)."""
    n = samples(0.6)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1800.0, 80.0, 0.85), 0.0, 0.7)
    # The steel's ring: a partial at 72.5 Hz lifted onto D2, its out-of-key 43 Hz fundamental filtered away.
    ring = parts.clip(lib, "metal_thud", 600.0, 62.0, 1.012)[:samples(0.45)]
    dsp.place(out, ring * dsp.shape(len(ring), [(0.0, 1.0), (0.45, 0.0)]), 0.0, 0.3)
    hum_n = samples(0.5)
    ratio = (1.0, np.interp(np.arange(hum_n), [0, hum_n - 1], [1.0, 1.4983]))[variant]
    hum = parts.hum(lib, hum_n, ratio, float(gen.uniform(1.0, 6.0)))
    swell = dsp.shape(hum_n, [(0.0, 0.0), (0.04 + 0.03 * variant, 1.0), (0.5, 0.0)])
    dsp.place(out, hum * swell, 0.02, 0.22)
    return out


def tape_tick(lib, gen, variant: int) -> np.ndarray:
    """The list cursor moving one line: a tape head's step, a tiny counter tick (variant 1: the typewriter
    carriage's ratchet, one notch)."""
    n = samples(0.12)
    out = np.zeros(n)
    if variant == 0:
        dsp.place(out, parts.clip(lib, "count_tick", 2400.0, 350.0, 1.05), 0.0, 0.7)
    else:
        notch = parts.clip(lib, "typewriter_carriage", 2400.0, 300.0)[:samples(0.09)]
        dsp.place(out, notch * dsp.shape(len(notch), [(0.0, 1.0), (0.09, 0.0)]), 0.0, 0.7)
    return out


# Cue id -> (recipe, variants).
SOUNDS = {
    "ui_relay_click": (relay_click, 2),
    "ui_tube_thunk": (tube_thunk, 2),
    "ui_tape_tick": (tape_tick, 2),
}
