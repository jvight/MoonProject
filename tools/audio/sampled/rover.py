"""07's own small switches and gaps from recorded sources. The lamps' switch when the player turns them on or off
(L / L3, RoverLightsSwitched), at the headlamp. On: a small relay ticking closed and a faint breath of the filament's
hum on D3 as the lamps ease up. Off: a softer, lower tick and nothing after. Close and quiet; no tone of its own.

The cutter's sputter (CutterSputtered, the first cut tried without an igniter): a spark gap that will not catch, two
dry clicks of the contact and a short fizzle dying away. Dry and physical, no tone. Every other refused cut
(CutterRefused): a soft, low fault tick, the turret's latch held shut with a breath of dry buzz.

07's tow winch (opening v3 slice 3.2): the cable spooling out as the hook attaches (the drum's ratchet and its gear
motor easing down), the cable's strain while towing (a seamless creak loop of slowed metal over a low tension hum, the
game scaling it with the load) and the release clunk with the slack cable's slap. Physical; never a beep."""
import numpy as np

from synth import filters
from synth.core import samples

from . import dsp, parts

SWITCH_S = 0.45
TOW_RELEASE_LABELS = ("release", "snap")
STRAIN_LOOP_S = 4.0
LOOP_SEAM_S = 0.35
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


def cutter_refuse(lib, gen, _variant: int) -> np.ndarray:
    """A cut the cutter refused: a soft, low fault tick, the turret's latch thudding shut slowed and muffled, with a
    breath of dry contactor buzz under it."""
    n = samples(0.35)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1100.0, 60.0, 0.7), 0.0, 0.6)
    buzz_n = samples(0.12)
    buzz = dsp.varispeed(lib("arc_buzz"), 0.5, buzz_n, float(gen.uniform(0.0, 4.0)))
    buzz = dsp.peak_normalize(filters.butter(filters.highpass(buzz, 150.0), "lowpass", 900.0, order=2))
    dsp.place(out, buzz * dsp.shape(buzz_n, [(0.0, 0.0), (0.01, 1.0), (0.12, 0.0)]), 0.01, 0.2)
    return out


def tow_spool(lib, gen, _variant: int) -> np.ndarray:
    """The tow hook attaching: the winch drum's ratchet paying out cable as its gear motor eases down onto D."""
    n = samples(1.2)
    out = np.zeros(n)
    ratchet = dsp.varispeed(lib("winder"), 0.75, samples(1.0), 0.0)
    ratchet = dsp.peak_normalize(filters.butter(filters.highpass(ratchet, 250.0), "lowpass", 2200.0, order=4))
    dsp.place(out, ratchet * dsp.shape(len(ratchet), [(0.0, 0.0), (0.05, 1.0), (0.7, 0.7), (1.0, 0.0)]), 0.05, 0.4)
    gear_n = samples(1.0)
    gear = parts.gear(lib, gen, gear_n, "A5") * dsp.shape(gear_n, [(0.0, 0.0), (0.1, 1.0), (1.0, 0.0)])
    dsp.place(out, gear, 0.0, 0.3)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1800.0, 80.0, 0.9), 0.0, 0.4)
    return out


def tow_strain_loop(lib, gen, _variant: int) -> np.ndarray:
    """The cable under load: a slow creak of strained metal over a low tension hum on D, seamless over 4 s (the game
    raises its level and pitch with the load)."""
    n = samples(STRAIN_LOOP_S + LOOP_SEAM_S)
    creak = dsp.fit(dsp.varispeed(lib("metal_squeal"), 0.3, n, float(gen.uniform(0.0, 0.3))), n)
    creak = dsp.peak_normalize(filters.butter(filters.highpass(creak, 120.0), "lowpass", 1500.0, order=2))
    hum = parts.hum(lib, n, 0.5)
    raw = 0.6 * creak + 0.4 * hum
    seam = samples(LOOP_SEAM_S)
    body = raw[:samples(STRAIN_LOOP_S)].copy()
    fade = np.sin(np.linspace(0.0, np.pi / 2.0, seam))
    body[:seam] = body[:seam] * fade + raw[samples(STRAIN_LOOP_S):samples(STRAIN_LOOP_S) + seam] * fade[::-1]
    return body


def tow_release(lib, gen, variant: int) -> np.ndarray:
    """The tow let go (variant 1, release: the hook's latch clunking open and the slack cable slapping the ground;
    variant 2, snap: the cable parting under load, a hard metal whip, the latch kicked open and a longer, rougher
    slap as the end recoils)."""
    n = samples(0.7 if variant == 0 else 0.9)
    out = np.zeros(n)
    if variant == 1:
        dsp.place(out, parts.clip(lib, "metal_tap", 3200.0, 300.0, 1.1), 0.0, 0.35)
        dsp.place(out, parts.clip(lib, "pedal_clunk", 1400.0, 60.0, 1.05), 0.01, 0.35)
        whip = parts.clip(lib, "scrape_grit", 2800.0, 300.0)[:samples(0.55)]
        dsp.place(out, whip * dsp.shape(len(whip), [(0.0, 1.0), (0.15, 0.7), (0.55, 0.0)]), 0.05, 0.45)
        slap = parts.clip(lib, "grit_roll", 2500.0, 200.0)[:samples(0.45)]
        dsp.place(out, slap * dsp.shape(len(slap), [(0.0, 1.0), (0.45, 0.0)]), 0.3, 0.4)
        return out

    dsp.place(out, parts.clip(lib, "pedal_clunk", 1100.0, 60.0, 0.9), 0.0, 0.35)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1700.0, 90.0, 1.05), 0.02, 0.2)
    slap = parts.clip(lib, "grit_roll", 2500.0, 200.0)[:samples(0.4)]
    dsp.place(out, slap * dsp.shape(len(slap), [(0.0, 1.0), (0.1, 0.8), (0.4, 0.0)]), 0.1, 0.45)
    return out


# Cue id -> (recipe, variants). Every variant ships: on and off, the one sputter and the refusal tick.
SOUNDS = {
    "rover_lights_switch": (lights_switch, len(LABELS)),
    "cutter_sputter": (cutter_sputter, 1),
    "cutter_refuse": (cutter_refuse, 1),
    "tow_spool": (tow_spool, 1),
    "tow_strain_loop": (tow_strain_loop, 1),
    "tow_release": (tow_release, len(TOW_RELEASE_LABELS)),
}
