"""The base's lamps from recorded sources: at dusk each lamp warms on as BaseLamps eases it up (pad, array, bank, mast,
0.9 s apart), a relay pulling its contactor closed with a warm clunk, the cold filament's soft tick a breath later,
and the lamp's hum easing in on its step of the pentatonic ladder, so the base climbs gently into the dark. Physical
and close; no chime."""
import numpy as np

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


# Cue id -> (recipe, variants). Every variant ships: one per lamp in the lighting order.
SOUNDS = {
    "base_lamp_on": (lamp_on, len(LAMP_STEPS)),
}
