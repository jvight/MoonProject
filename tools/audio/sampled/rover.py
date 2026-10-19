"""07's own small switches from recorded sources: the lamps' switch when the player turns them on or off (L / L3,
RoverLightsSwitched), at the headlamp. On: a small relay ticking closed and a faint breath of the filament's hum on
D3 as the lamps ease up. Off: a softer, lower tick and nothing after. Close and quiet; no tone of its own."""
import numpy as np

from synth.core import samples

from . import dsp, parts

SWITCH_S = 0.45
LABELS = ("on", "off")


def lights_switch(lib, gen, variant: int) -> np.ndarray:
    """07's lamps switching (variant 1, on: a relay tick, its contact, a faint filament breath on D3; variant 2,
    off: a softer tick, slowed and darker)."""
    n = samples(SWITCH_S)
    out = np.zeros(n)
    if variant == 0:
        dsp.place(out, parts.clip(lib, "relay_click", 2600.0, 200.0, 0.95), 0.0, 0.5)
        dsp.place(out, parts.clip(lib, "count_tick", 2600.0, 600.0, 1.1), 0.022, 0.12)
        breath_n = samples(SWITCH_S - 0.03)
        hum = parts.hum(lib, breath_n, 1.0, float(gen.uniform(1.0, 6.0)))
        glow = dsp.shape(breath_n, [(0.0, 0.0), (0.12, 1.0), (SWITCH_S - 0.03, 0.0)])
        dsp.place(out, hum * glow, 0.03, 0.25)
    else:
        dsp.place(out, parts.clip(lib, "relay_click", 1800.0, 150.0, 0.8), 0.0, 0.35)
    return out


# Cue id -> (recipe, variants). Every variant ships: on and off.
SOUNDS = {
    "rover_lights_switch": (lights_switch, len(LABELS)),
}
