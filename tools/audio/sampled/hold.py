"""07's hold and its counters from recorded sources, in place of the synth wood ticks that read as beeps.

Each hold slot's outline lighting (left to right, 60 ms apart) and each unit the stock counter rolls: the counting
machine's small mechanism ticking over, with a tiny ring of struck metal on the next pentatonic step, so a run climbs
like a mechanism, not a tone generator. Every tick is kept in tune and kept under the mix.
"""
import numpy as np

from synth.core import samples

from . import dsp, parts

TICK_S = 0.15
HOLD_SLOT_NOTES = ("D5", "E5", "F#5", "A5", "B5", "D6", "E6", "F#6")
COUNT_TICK_NOTES = ("D5", "E5", "F#5", "A5", "B5", "D6")


def _tick(lib, note: str) -> np.ndarray:
    """The counting machine's tick with a short ring on ``note``."""
    click = parts.clip(lib, "count_tick", 3000.0, 1000.0, 1.0)

    def build(cents):
        out = np.zeros(samples(TICK_S))
        dsp.place(out, click, 0.0, 0.5)
        dsp.place(out, parts.ring(lib, note, TICK_S - 0.004, cents), 0.002, 0.45)
        return out

    return parts.in_tune(build, note)


def hold_slot(lib, _gen, variant: int) -> np.ndarray:
    """A hold slot's outline lighting: the mechanism ticking over and a tiny ring on its step (variant k: D5 E5 F#5
    A5 B5 D6 E6 F#6)."""
    return _tick(lib, HOLD_SLOT_NOTES[variant])


def count_tick(lib, _gen, variant: int) -> np.ndarray:
    """The stock counter rolling one unit: the mechanism ticking over and a tiny ring on the next step (variant k:
    D5 E5 F#5 A5 B5 D6)."""
    return _tick(lib, COUNT_TICK_NOTES[variant])


# Cue id -> (recipe, variants). Every variant ships: one per step of the climb.
SOUNDS = {
    "hold_slot": (hold_slot, len(HOLD_SLOT_NOTES)),
    "count_tick": (count_tick, len(COUNT_TICK_NOTES)),
}
