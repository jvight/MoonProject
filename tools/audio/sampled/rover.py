"""07's own small switches and gaps from recorded sources. The lamps' switch when the player turns them on or off
(L / L3, RoverLightsSwitched), at the headlamp. On: a small relay ticking closed and a faint breath of the filament's
hum on D3 as the lamps ease up. Off: a softer, lower tick and nothing after. Close and quiet; no tone of its own.

The cutter's sputter (CutterSputtered, the first cut tried without an igniter): a spark gap that will not catch, two
dry clicks of the contact and a short fizzle dying away. Dry and physical, no tone."""
import numpy as np

from synth import filters
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


def cutter_sputter(lib, gen, _variant: int) -> np.ndarray:
    """The cutter tried without its igniter: click, click, a fizzle. Two dry contact clicks, each with a tiny spark
    snap that does not catch, then a short crackle dying away."""
    n = samples(0.7)
    out = np.zeros(n)
    snap_src = lib("arc_spark")
    for i, at in enumerate((0.0, 0.14)):
        dsp.place(out, parts.clip(lib, "relay_click", 3200.0, 250.0, 1.05 - 0.08 * i), at, 0.45)
        start = samples(0.04 + 0.05 * i)
        snap = snap_src[start:start + samples(0.03)]
        snap = filters.butter(filters.bandpass(snap, 2600.0, 0.7), "lowpass", 3800.0, order=4)
        dsp.place(out, dsp.peak_normalize(snap) * dsp.shape(len(snap), [(0.0, 1.0), (len(snap) / 48000.0, 0.0)]),
                  at + 0.008, 0.25)
    fizz_n = samples(0.4)
    fizz = parts.clip(lib, "crackle", 3200.0, 700.0)[:fizz_n]
    dsp.place(out, fizz * dsp.shape(len(fizz), [(0.0, 0.0), (0.02, 1.0), (len(fizz) / 48000.0, 0.0)]), 0.24, 0.3)
    return out


# Cue id -> (recipe, variants). Every variant ships: on and off, and the one sputter.
SOUNDS = {
    "rover_lights_switch": (lights_switch, len(LABELS)),
    "cutter_sputter": (cutter_sputter, 1),
}
