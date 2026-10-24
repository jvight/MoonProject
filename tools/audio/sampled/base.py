"""The base's machines from recorded sources.

The lamps: at dusk each lamp warms on as BaseLamps eases it up (pad, array, bank, mast, 0.9 s apart), a relay pulling
its contactor closed with a warm clunk, the cold filament's soft tick a breath later, and the lamp's hum easing in on
its step of the pentatonic ladder, so the base climbs gently into the dark. Physical and close; no chime.

The charging pad's lock: as the clamp arms seat against 07's flanks (PadLockClosed), the rams settling and a padded,
heavy-but-soft clunk; as they let go (PadLockOpened), a short release hiss and a lighter clunk.

The socket rack (RackChanged): a bay filling as its part is fitted, the two jaws easing shut into a solid clunk-latch
and the bay lamp's low warm hum swelling on D; a bay's outline first appearing, a quiet relay click and a faint
pentatonic tone of bowed metal.
"""
import numpy as np

from synth import filters
from synth.core import note_freq, samples

from . import dsp, parts

LAMP_S = 1.2
DOCK_HUM_LOOP_S = 4.0
LOOP_SEAM_S = 0.35
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


def mast_clunk(lib, gen, _variant: int) -> np.ndarray:
    """A segment of the antenna mast telescoping up and locking home: a heavy, padded clunk with the steel's low ring
    on D2 and the collar's latch (the game pitches it up a step per segment)."""
    n = samples(0.9)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "door_clunk", 1300.0, 60.0, 0.8), 0.0, 0.5)
    ring = parts.clip(lib, "metal_thud", 650.0, 62.0, 1.012)[:samples(0.8)]
    dsp.place(out, ring * dsp.shape(len(ring), [(0.0, 1.0), (0.8, 0.0)]), 0.0, 0.4)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1800.0, 80.0, 0.9), 0.05, 0.3)
    return out


def mast_settle(lib, gen, _variant: int) -> np.ndarray:
    """The raised mast settling: its gear easing down onto D and a long, soft ring of the whole steel column."""
    n = samples(1.6)
    out = np.zeros(n)
    gear_n = samples(0.5)
    gear = parts.gear(lib, gen, gear_n, "D5") * dsp.shape(gear_n, [(0.0, 0.0), (0.08, 1.0), (0.5, 0.0)])
    dsp.place(out, gear, 0.0, 0.3)
    ring = parts.clip(lib, "metal_thud", 600.0, 62.0, 1.012)[:samples(1.4)]
    dsp.place(out, ring * dsp.shape(len(ring), [(0.0, 0.0), (0.05, 1.0), (1.4, 0.0)]), 0.1, 0.45)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1500.0, 70.0, 0.75), 0.12, 0.25)
    return out


def rack_fill(lib, gen, _variant: int) -> np.ndarray:
    """A rack bay filling: the two jaws easing shut on their gear, a solid clunk-latch as they close on the part,
    then the bay lamp's low warm hum swelling on D3."""
    n = samples(1.9)
    out = np.zeros(n)
    jaw_n = samples(0.4)
    jaw = parts.gear(lib, gen, jaw_n, "A4") * dsp.shape(jaw_n, [(0.0, 0.0), (0.06, 1.0), (0.3, 0.7), (0.4, 0.0)])
    dsp.place(out, jaw, 0.0, 0.3)
    dsp.place(out, parts.clip(lib, "pedal_clunk", 1300.0, 60.0, 0.95), 0.36, 0.45)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1900.0, 80.0, 0.9), 0.39, 0.35)
    hum_n = samples(1.4)
    hum = parts.hum(lib, hum_n, 1.0, float(gen.uniform(1.0, 6.0)))
    dsp.place(out, hum * dsp.shape(hum_n, [(0.0, 0.0), (0.45, 1.0), (0.8, 0.8), (1.4, 0.0)]), 0.45, 0.4)
    return out


def rack_outline(lib, gen, _variant: int) -> np.ndarray:
    """A bay's outline first appearing: a quiet relay click and a faint bowed-metal tone on A5, there and gone."""
    n = samples(0.8)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "relay_click", 2400.0, 200.0, 0.95), 0.0, 0.35)
    tone_n = samples(0.7)
    f = note_freq("A5")
    rate = dsp.ratio_for("A5", lib.f0("bowed_metal"), np.zeros(tone_n))
    tone = dsp.varispeed(lib("bowed_metal"), rate, tone_n, float(gen.uniform(0.0, 1.0)))
    tone = dsp.peak_normalize(filters.butter(filters.highpass(tone, f * 0.6), "lowpass", f * 2.4, order=4))
    dsp.place(out, tone * dsp.shape(tone_n, [(0.0, 0.0), (0.08, 1.0), (0.25, 0.6), (0.7, 0.0)]), 0.03, 0.3)
    return out


def dock_charge_hum(lib, gen, _variant: int) -> np.ndarray:
    """The pad charging 07: its transformer's warm hum on D3 and its octave, breathing slowly like a lamp keeping
    watch. Seamless over 4 s."""
    n = samples(DOCK_HUM_LOOP_S + LOOP_SEAM_S)
    hum = parts.hum(lib, n, 1.0, float(gen.uniform(1.0, 6.0)))
    t = np.arange(n) / (n - samples(LOOP_SEAM_S))
    breath = 0.75 + 0.25 * np.sin(2.0 * np.pi * 2.0 * t)
    return dsp.seamless(hum * breath, samples(DOCK_HUM_LOOP_S), samples(LOOP_SEAM_S))


# Cue id -> (recipe, variants). Every variant ships: one per lamp in the lighting order, one each for the lock.
SOUNDS = {
    "base_lamp_on": (lamp_on, len(LAMP_STEPS)),
    "pad_lock_close": (pad_lock_close, 1),
    "pad_lock_open": (pad_lock_open, 1),
    "mast_clunk": (mast_clunk, 1),
    "mast_settle": (mast_settle, 1),
    "rack_fill": (rack_fill, 1),
    "rack_outline": (rack_outline, 1),
    "dock_charge_hum": (dock_charge_hum, 1),
}
