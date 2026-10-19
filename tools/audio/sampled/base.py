"""The base's machines from recorded sources.

The lamps: at dusk each lamp warms on as BaseLamps eases it up (pad, array, bank, mast, 0.9 s apart), a relay pulling
its contactor closed with a warm clunk, the cold filament's soft tick a breath later, and the lamp's hum easing in on
its step of the pentatonic ladder, so the base climbs gently into the dark. Physical and close; no chime.

The charging pad's lock: as the clamp arms seat against 07's flanks (PadLockClosed), the rams settling and a padded,
heavy-but-soft clunk; as they let go (PadLockOpened), a short release hiss and a lighter clunk.
"""
import numpy as np

from synth import filters
from synth.core import samples

from . import dsp, parts

LAMP_S = 1.2
# The hum's step for each lamp in the lighting order, as ratios of D3: D3, E3, F#3, A3 (D major pentatonic).
LAMP_STEPS = (1.0, 2.0 ** (2.0 / 12.0), 2.0 ** (4.0 / 12.0), 2.0 ** (7.0 / 12.0))


def lamp_on(lib, gen, variant: int) -> np.ndarray:
    """A base lamp coming on: a relay's warm clunk, the filament's soft tick, the lamp's hum easing in on its step
    (variant k: the k-th lamp in the lighting order, D3 E3 F#3 A3)."""
    n = samples(LAMP_S)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1500.0, 70.0, 0.8), 0.0, 0.3)
    dsp.place(out, parts.clip(lib, "relay_click", 2400.0, 150.0, 0.85), 0.006, 0.12)
    dsp.place(out, parts.clip(lib, "count_tick", 2800.0, 700.0, 1.2), 0.07, 0.07)
    hum_n = samples(LAMP_S - 0.05)
    hum = parts.hum(lib, hum_n, LAMP_STEPS[variant], float(gen.uniform(1.0, 6.0)))
    glow = dsp.shape(hum_n, [(0.0, 0.0), (0.35, 1.0), (0.6, 0.8), (LAMP_S - 0.05, 0.0)])
    dsp.place(out, hum * glow, 0.05, 0.5)
    return out


def pad_lock_close(lib, gen, _variant: int) -> np.ndarray:
    """The pad's clamp arms seating on 07: the rams' gear settling down onto D, a padded heavy clunk and the latch."""
    n = samples(0.8)
    out = np.zeros(n)
    ram_n = samples(0.35)
    ram = parts.gear(lib, gen, ram_n, "D5") * dsp.shape(ram_n, [(0.0, 0.6), (0.1, 1.0), (0.35, 0.0)])
    dsp.place(out, ram, 0.0, 0.35)
    dsp.place(out, parts.clip(lib, "pedal_clunk", 1100.0, 60.0, 0.85), 0.12, 0.45)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1700.0, 70.0, 0.8), 0.15, 0.3)
    return out


def pad_lock_open(lib, gen, _variant: int) -> np.ndarray:
    """The pad letting 07 go: a short release hiss as the rams vent, then a lighter clunk as the arms swing clear."""
    n = samples(0.7)
    out = np.zeros(n)
    hiss_n = samples(0.5)
    hiss = dsp.varispeed(lib("radio_sweep"), 0.8, hiss_n, float(gen.uniform(0.0, 6.0)))
    hiss = dsp.peak_normalize(filters.butter(filters.highpass(hiss, 900.0), "lowpass", 3000.0, order=2))
    dsp.place(out, hiss * dsp.shape(hiss_n, [(0.0, 0.0), (0.04, 1.0), (0.2, 0.7), (0.5, 0.0)]), 0.0, 0.5)
    ram_n = samples(0.35)
    ram = parts.gear(lib, gen, ram_n, "A5") * dsp.shape(ram_n, [(0.0, 0.0), (0.08, 1.0), (0.35, 0.0)])
    dsp.place(out, ram, 0.1, 0.3)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1700.0, 90.0, 1.1), 0.3, 0.2)
    return out


# Cue id -> (recipe, variants). Every variant ships: one per lamp in the lighting order, one each for the lock.
SOUNDS = {
    "base_lamp_on": (lamp_on, len(LAMP_STEPS)),
    "pad_lock_close": (pad_lock_close, 1),
    "pad_lock_open": (pad_lock_open, 1),
}
