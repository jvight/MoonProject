"""07's tools from recorded sources, in place of the 0.5.5 sine tones that read as beeps in the opening's mix pass.

The scan lens: while a fragment is being read, the lens's small fan motor turning on D5 under a faint breath of air
(the runtime steps its pitch D E F# A each quarter); each quarter drawn, a light tap on the lens collar and a short
ring of struck metal on that step, soft enough to sit inside the sweep instead of on top of it.

The cutter's charge: the capacitor's whine is a real small motor held on D5 through a narrow speaker (the runtime
glides it D -> A -> D' up the stages), and the low hum under it is the mains hum carried down to D2.
"""
import numpy as np

from analyze_audio import dominant_pitch
from synth import filters
from synth.core import note_freq, samples

from . import dsp, parts

SCAN_SWEEP_LOOP_S = 2.0
SCAN_STEP_NOTES = ("D5", "E5", "F#5", "A5")
CHARGE_WHINE_LOOP_S = 2.0
CHARGE_HUM_LOOP_S = 3.0
LOOP_SEAM_S = 0.35
# Where the sources hold steady: the bow sits closest to its pitch from here (it drifts flat later, which the step
# measures and takes out), and the motor's level stays within 3 dB from here on (its first seconds swell and dip).
RING_OFFSET_S = 1.0
MOTOR_OFFSET_S = 5.0


def _band(x: np.ndarray, low: float, high: float) -> np.ndarray:
    return dsp.peak_normalize(filters.butter(filters.highpass(x, low), "lowpass", high, order=4))


def _motor(lib, n: int) -> np.ndarray:
    """The small motor's steady stretch, held on D5."""
    return dsp.varispeed(lib("motor_whine"), dsp.ratio_for("D5", lib.f0("motor_whine"), np.zeros(n)), n,
                         MOTOR_OFFSET_S)


def scan_sweep(lib, gen, _variant: int) -> np.ndarray:
    """The lens's fan sweeping a fragment: its small motor turning on D5 with a slow sway and a faint breath of air,
    quiet and calm. Seamless over 2 s."""
    n = samples(SCAN_SWEEP_LOOP_S + LOOP_SEAM_S)
    f = note_freq("D5")
    motor = _band(_motor(lib, n), f * 0.7, f * 1.8)
    air = dsp.varispeed(lib("radio_sweep"), 0.7, n, float(gen.uniform(0.0, 6.0)))
    air = _band(air, 600.0, 2400.0)
    sway = 0.85 + 0.15 * np.sin(2.0 * np.pi * (2.0 / SCAN_SWEEP_LOOP_S) * np.arange(n) / (n - samples(LOOP_SEAM_S)))
    return dsp.seamless(0.8 * motor * sway + 0.12 * air, samples(SCAN_SWEEP_LOOP_S), samples(LOOP_SEAM_S))


def scan_step(lib, _gen, variant: int) -> np.ndarray:
    """A quarter of a scan drawn: a light tap on the lens collar and a short ring of struck metal on the next step
    (variant k: D5 E5 F#5 A5). A higher note reads further into the bow, where it drifts flat, so the step is measured
    once and struck again with that drift taken out."""
    note = SCAN_STEP_NOTES[variant]
    step = _step(lib, note, 0.0)
    _, error, _ = dominant_pitch(step)
    return _step(lib, note, -error) if abs(error) > 1.0 else step


def _step(lib, note: str, cents: float) -> np.ndarray:
    f = note_freq(note)
    out = np.zeros(samples(0.7))
    dsp.place(out, parts.clip(lib, "metal_tap", 2600.0, 300.0, 1.1), 0.0, 0.25)
    ring_n = samples(0.65)
    rate = dsp.ratio_for(note, lib.f0("bowed_metal"), np.full(ring_n, cents / 100.0))
    ring = _band(dsp.varispeed(lib("bowed_metal"), rate, ring_n, RING_OFFSET_S), f * 0.7, f * 1.6)
    dsp.place(out, ring * dsp.shape(ring_n, [(0.0, 0.0), (0.012, 1.0), (0.15, 0.45), (0.65, 0.0)]), 0.005, 0.6)
    return out


def charge_whine(lib, _gen, _variant: int) -> np.ndarray:
    """The capacitor whine on D5: a real small motor held steady through a narrow speaker, with a slow breath in its
    level. Seamless over 2 s."""
    n = samples(CHARGE_WHINE_LOOP_S + LOOP_SEAM_S)
    f = note_freq("D5")
    whine = _band(_motor(lib, n), f * 0.6, f * 2.2)
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


# Cue id -> (recipe, variants). Every variant ships: one step per quarter of a scan.
SOUNDS = {
    "scan_sweep": (scan_sweep, 1),
    "scan_step": (scan_step, len(SCAN_STEP_NOTES)),
    "charge_whine": (charge_whine, 1),
    "charge_hum": (charge_hum, 1),
}
