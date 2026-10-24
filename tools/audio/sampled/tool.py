"""07's tools from recorded sources, in place of the 0.5.5 sine tones that read as beeps in the opening's mix pass.

The scan lens: while a fragment is being read, the lens's small fan motor turning on D5 under a faint breath of air
(the runtime steps its pitch D E F# A each quarter); each quarter drawn, a light tap on the lens collar and a short
ring of struck metal on that step, soft enough to sit inside the sweep instead of on top of it.

The cutter's charge, heavy rather than weightless: 07 braces on a real servo and a padded clunk, each stage seats a
capacitor cell with a dense latch, the full lock rings the chassis steel on D2; the whine is a real small motor held
on D5 (the runtime glides it D -> A -> D' up the stages) over the mains hum carried down to D2. The cut: a deeper
hum-and-arc body for the higher tiers, the beam's servo sliding up a step to each next cut line, and the piece
breaking free with a snap and a low ring.
"""
import numpy as np

from synth import filters
from synth.core import note_freq, samples

from . import dsp, parts

SCAN_SWEEP_LOOP_S = 2.0
SCAN_STEP_NOTES = ("D5", "E5", "F#5", "A5")
CHARGE_WHINE_LOOP_S = 2.0
CHARGE_HUM_LOOP_S = 3.0
LOOP_SEAM_S = 0.35
CUT_UNDERTONE_LOOP_S = 4.0
SEGMENT_NOTES = ("D5", "E5", "F#5", "A5", "B5")


def scan_sweep(lib, gen, _variant: int) -> np.ndarray:
    """The lens's fan sweeping a fragment: its small motor turning on D5 with a slow sway and a faint breath of air,
    quiet and calm. Seamless over 2 s."""
    n = samples(SCAN_SWEEP_LOOP_S + LOOP_SEAM_S)
    f = note_freq("D5")
    motor = parts.band(parts.motor(lib, n, "D5"), f * 0.7, f * 1.8)
    air = dsp.varispeed(lib("radio_sweep"), 0.7, n, float(gen.uniform(0.0, 6.0)))
    air = parts.band(air, 600.0, 2400.0)
    sway = 0.85 + 0.15 * np.sin(2.0 * np.pi * (2.0 / SCAN_SWEEP_LOOP_S) * np.arange(n) / (n - samples(LOOP_SEAM_S)))
    return dsp.seamless(0.8 * motor * sway + 0.12 * air, samples(SCAN_SWEEP_LOOP_S), samples(LOOP_SEAM_S))


def scan_step(lib, _gen, variant: int) -> np.ndarray:
    """A quarter of a scan drawn: a light tap on the lens collar and a short ring of struck metal on the next step
    (variant k: D5 E5 F#5 A5), kept in tune."""
    note = SCAN_STEP_NOTES[variant]
    tap = parts.clip(lib, "metal_tap", 2600.0, 300.0, 1.1)

    def build(cents):
        out = np.zeros(samples(0.7))
        dsp.place(out, tap, 0.0, 0.25)
        dsp.place(out, parts.ring(lib, note, 0.65, cents), 0.005, 0.6)
        return out

    return parts.in_tune(build, note)


def segment_slide(lib, gen, variant: int) -> np.ndarray:
    """The beam moving to a piece's next cut line: the turret's latch letting go and its small servo sliding up onto
    the next step (variant k: D5 E5 F#5 A5 B5), so a big plate reads as several small victories."""
    note = SEGMENT_NOTES[variant]
    n = samples(0.6)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 2400.0, 150.0, 1.15), 0.0, 0.4)
    slide_n = samples(0.5)
    slide = parts.servo(lib, gen, slide_n, note, [(0.0, -2.0), (0.12, 0.0), (0.5, 0.0)])
    dsp.place(out, slide * dsp.shape(slide_n, [(0.0, 0.0), (0.03, 1.0), (0.2, 0.6), (0.5, 0.0)]), 0.02, 0.5)
    return out


def charge_whine(lib, _gen, _variant: int) -> np.ndarray:
    """The capacitor whine on D5: a real small motor held steady through a narrow speaker, with a slow breath in its
    level. Seamless over 2 s."""
    n = samples(CHARGE_WHINE_LOOP_S + LOOP_SEAM_S)
    f = note_freq("D5")
    whine = parts.band(parts.motor(lib, n, "D5"), f * 0.6, f * 2.2)
    t = np.arange(n) / (n - samples(LOOP_SEAM_S))
    breath = 0.9 + 0.1 * np.sin(2.0 * np.pi * 3.0 * t)
    return dsp.seamless(whine * breath, samples(CHARGE_WHINE_LOOP_S), samples(LOOP_SEAM_S))


def charge_hum(lib, gen, _variant: int) -> np.ndarray:
    """The low hum on D2 under the charge (the runtime sets its level by stage): the mains hum carried down, warm and
    round. Seamless over 3 s."""
    n = samples(CHARGE_HUM_LOOP_S + LOOP_SEAM_S)
    hum = parts.hum(lib, n, 0.5, float(gen.uniform(1.0, 6.0)))
    hum = dsp.peak_normalize(filters.butter(hum, "lowpass", 500.0, order=4))
    return dsp.seamless(hum, samples(CHARGE_HUM_LOOP_S), samples(LOOP_SEAM_S))


def charge_brace(lib, gen, _variant: int) -> np.ndarray:
    """07 bracing before a fresh charge: a heavy servo driving the clamps down, a padded clunk through the chassis as
    the wheels lock, and the lock's latch."""
    n = samples(0.75)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "servo_thump", 2200.0, 60.0, 0.85), 0.0, 0.5)
    dsp.place(out, parts.clip(lib, "pedal_clunk", 1200.0, 50.0, 0.8), 0.2, 0.6)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1800.0, 70.0, 0.85), 0.26, 0.35)
    return out


def charge_detent(lib, gen, _variant: int) -> np.ndarray:
    """A charge stage locking: a capacitor cell seated by hand into its rack, a dense latch with a small tick."""
    n = samples(0.3)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "latch_thunk", 2200.0, 120.0, 1.05), 0.0, 0.8)
    dsp.place(out, parts.clip(lib, "count_tick", 2600.0, 500.0, 0.9), 0.015, 0.2)
    return out


def charge_lock(lib, gen, _variant: int) -> np.ndarray:
    """The full lock: a heavy, padded clunk-latch and the chassis steel ringing low on D2, the held breath before
    the cut."""
    n = samples(1.0)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "door_clunk", 1300.0, 55.0, 0.75), 0.0, 0.55)
    ring = parts.clip(lib, "metal_thud", 650.0, 62.0, 1.012)[:samples(0.9)]
    dsp.place(out, ring * dsp.shape(len(ring), [(0.0, 1.0), (0.9, 0.0)]), 0.0, 0.45)
    dsp.place(out, parts.clip(lib, "latch_thunk", 1900.0, 80.0, 0.8), 0.06, 0.35)
    return out


def cut_undertone(lib, gen, _variant: int) -> np.ndarray:
    """The beam's deeper body for the higher tiers: the mains hum carried to D2 under a slowed arc's low growl. The
    runtime sets its level by tier (silent at tier 0). Seamless over 4 s."""
    n = samples(CUT_UNDERTONE_LOOP_S + LOOP_SEAM_S)
    hum = parts.hum(lib, n, 0.5, float(gen.uniform(1.0, 6.0)))
    arc = parts.band(dsp.varispeed(lib("arc_buzz"), 0.4, n, float(gen.uniform(0.0, 2.0))), 60.0, 600.0)
    return dsp.seamless(0.8 * hum + 0.3 * arc, samples(CUT_UNDERTONE_LOOP_S), samples(LOOP_SEAM_S))


def cut_crack(lib, gen, _variant: int) -> np.ndarray:
    """The piece breaking free: the last metal letting go with a dry snap, its weight dropping into the chassis
    with a low ring, a little grit after."""
    n = samples(1.2)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "metal_tap", 2600.0, 200.0, 0.8), 0.0, 0.45)
    dsp.place(out, parts.clip(lib, "case_down", 2000.0, 55.0, 0.8), 0.05, 0.6)
    thud = parts.clip(lib, "metal_thud", 700.0, 62.0, 1.012)[:samples(1.0)]
    dsp.place(out, thud * dsp.shape(len(thud), [(0.0, 1.0), (1.0, 0.0)]), 0.05, 0.45)
    grit = parts.clip(lib, "grit_roll", 3000.0, 200.0)[:samples(0.4)]
    dsp.place(out, grit * dsp.shape(len(grit), [(0.0, 1.0), (0.4, 0.0)]), 0.1, 0.1)
    return out


# Cue id -> (recipe, variants). Every variant ships: one step per quarter of a scan.
SOUNDS = {
    "scan_sweep": (scan_sweep, 1),
    "scan_step": (scan_step, len(SCAN_STEP_NOTES)),
    "charge_whine": (charge_whine, 1),
    "charge_hum": (charge_hum, 1),
    "charge_brace": (charge_brace, 1),
    "charge_detent": (charge_detent, 1),
    "charge_lock": (charge_lock, 1),
    "cut_undertone": (cut_undertone, 1),
    "cut_crack": (cut_crack, 1),
    "segment_slide": (segment_slide, len(SEGMENT_NOTES)),
}
