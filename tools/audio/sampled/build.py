"""The build sounds from recorded sources: a frame set down on regolith and locked, a detent snap, the weld's arc as a
seamless loop, a structure finishing (07's done tone over its latch), and a structure taking power (a contactor, a
transformer swelling from D up onto A, its lamp switching on)."""
import numpy as np

from synth import filters
from synth.core import samples

from . import dsp, parts, voice

WELD_LOOP_S = 4.0
LOOP_SEAM_S = 0.35


def place(lib, gen, variant: int) -> np.ndarray:
    """A frame dropped onto regolith, the low ring of its steel, then the feet locking."""
    n = samples(1.1)
    out = np.zeros(n)
    rate = (1.0, 0.85)[variant]
    dsp.place(out, parts.clip(lib, "case_down", 3000.0, 60.0, rate), 0.0, 0.8)
    # The steel's ring: a partial at 72.5 Hz lifted onto D2, its out-of-key 43 Hz fundamental filtered away.
    thud = parts.clip(lib, "metal_thud", 700.0, 62.0, 1.012)[:samples(0.9)]
    dsp.place(out, thud * dsp.shape(len(thud), [(0.0, 1.0), (0.9, 0.0)]), 0.0, 0.55)
    dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0), 0.2, 0.55)
    if variant == 1:
        dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0, 60.0, 0.9), 0.33, 0.4)
    grit = parts.clip(lib, "grit_roll", 3500.0, 200.0)[:samples(0.5)]
    dsp.place(out, grit * dsp.shape(len(grit), [(0.0, 1.0), (0.5, 0.0)]), 0.03, 0.12)
    return out


def snap(lib, gen, variant: int) -> np.ndarray:
    """The ghost aligning: a ball detent dropping into its seat."""
    n = samples(0.4)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 2600.0, 150.0, (1.1, 1.25)[variant]), 0.0, 0.8)
    dsp.place(out, parts.clip(lib, ("vintage_click", "count_tick")[variant], 2800.0, 400.0), 0.02, 0.25)
    return out


def weld_loop(lib, gen, variant: int) -> np.ndarray:
    """The weld: a warm arc buzz with sparse crackle over a low hum on D, seamless over 4 s."""
    n = samples(WELD_LOOP_S + LOOP_SEAM_S)
    arc = dsp.varispeed(lib("arc_buzz"), (0.6, 0.5)[variant], n, float(gen.uniform(0.0, 2.0)))
    arc = dsp.peak_normalize(filters.lowpass(filters.highpass(arc, 300.0), 3200.0))
    crackle = dsp.fit(dsp.varispeed(lib("crackle"), 0.8, n), n)
    crackle = dsp.peak_normalize(filters.lowpass(filters.highpass(crackle, 900.0), 3600.0))
    hum = parts.hum(lib, n, 0.5)
    raw = 0.7 * arc + 0.25 * crackle + 0.35 * hum
    seam = samples(LOOP_SEAM_S)
    body = raw[:samples(WELD_LOOP_S)].copy()
    fade = np.sin(np.linspace(0.0, np.pi / 2.0, seam))
    body[:seam] = body[:seam] * fade + raw[samples(WELD_LOOP_S):samples(WELD_LOOP_S) + seam] * fade[::-1]
    return body


def structure_complete(lib, gen, variant: int) -> np.ndarray:
    """The last weld cools: the frame's latch, a relay closing, and 07's done tone falling onto A."""
    n = samples(1.3)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "case_latch", 3500.0), 0.0, 0.6)
    dsp.place(out, parts.clip(lib, "relay_click", 4500.0, 300.0), 0.12, 0.4)
    dsp.place(out, voice.render(lib, gen, "voice_done", variant), 0.2, 0.85)
    return out


def power_on(lib, gen, variant: int) -> np.ndarray:
    """Power reaching a structure: the contactor, the transformer swelling from D onto A, its lamp switching on."""
    n = samples(2.6)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "radio_power", 4000.0), 0.0, 0.7)
    dsp.place(out, parts.clip(lib, "door_clunk", 1800.0, 50.0, 1.4)[:samples(0.4)], 0.0, 0.3)
    swell_n = samples(2.4)
    ratio = 2.0 ** (dsp.contour(swell_n, [(0.0, -12.0), (1.0, (7.0, 0.0)[variant]), (2.4, (7.0, 0.0)[variant])])
                    / 12.0)
    swell = parts.hum(lib, swell_n, ratio) * dsp.shape(swell_n, [(0.0, 0.0), (0.9, 1.0), (1.6, 0.8), (2.4, 0.0)])
    dsp.place(out, swell, 0.05, 0.8)
    dsp.place(out, parts.clip(lib, "lamp_switch", 4500.0), 1.0, 0.5)
    return out


SOUNDS = {
    "build_place": (place, 2),
    "build_snap": (snap, 2),
    "build_weld_loop": (weld_loop, 2),
    "build_structure_complete": (structure_complete, 3),
    "structure_power_on": (power_on, 2),
}
