"""07's power-on self-test (POST): one sound per check of the New Game wake (Core BootCheckStep, Lens to Begin), each
the size of the check's motion. Real machine sources only: relays, a motor whine for every servo, the crane's gear
motor, an arc for the cutter's failed ignition, a sprung tape for the antenna, grit and rattles for 36 years of
regolith sliding off. The tones that speak (the assembly's query, the ready chord) are 07's voice; the cutter's fault
is voiced on its result. The black, the lid and the lens hunt before the checks keep the wake's own cues
(WakeAudio), so Carrier, Resume and Agenda have no sound here.

Each recipe is laid out on its design length (``DESIGN``) and stretched onto Rover's step length (``LENGTHS``):
onsets and motions scale, the clicks and clunks themselves keep their size.
"""
import numpy as np

from synth import effects, filters
from synth.core import samples

from . import dsp, parts, voice

# Rover's check lengths in seconds (PostSettings, main 2026-10-18).
LENGTHS = {
    "boot_lens": 1.0, "boot_lamps": 2.4, "boot_head": 1.8, "boot_wheels": 1.8, "boot_motor": 1.6,
    "boot_cutter": 2.4, "boot_wing_jam": 2.0, "boot_antenna": 1.8, "boot_battery": 1.2, "boot_assembly": 0.7,
    "boot_begin": 4.0,
}

# The length each recipe was laid out on.
DESIGN = {
    "boot_lens": 1.2, "boot_lamps": 2.4, "boot_head": 2.0, "boot_wheels": 2.0, "boot_motor": 1.8,
    "boot_cutter": 1.9, "boot_wing_jam": 2.2, "boot_antenna": 2.0, "boot_battery": 1.4, "boot_assembly": 0.8,
    "boot_begin": 4.5,
}


class _Timeline:
    """Design seconds onto the step's real seconds."""

    def __init__(self, cue: str):
        self.k = LENGTHS[cue] / DESIGN[cue]
        self.n = samples(LENGTHS[cue])

    def __call__(self, seconds: float) -> float:
        return seconds * self.k

    def points(self, points) -> list:
        return [(self(t), v) for t, v in points]


def lens(lib, gen) -> np.ndarray:
    """The iris: a ratcheting slide, the focus motor hunting around A and settling, a soft click when it locks."""
    t = _Timeline("boot_lens")
    out = np.zeros(t.n)
    slide_n = samples(t(0.6))
    slide = parts.clip(lib, "tape_spring", 5000.0, 200.0, 0.8)[:slide_n]
    dsp.place(out, slide * dsp.shape(slide_n, t.points([(0.0, 0.0), (0.05, 1.0), (0.6, 0.0)])), 0.0, 0.45)
    out += parts.servo(lib, gen, t.n, "A4", tuple(t.points(((0.0, -3.0), (0.3, 1.0), (0.55, -0.6), (0.8, 0.2),
                                                             (1.0, 0.0)))), 0.5) * \
        dsp.shape(t.n, t.points([(0.0, 0.0), (0.06, 1.0), (0.95, 0.8), (1.2, 0.0)]))
    dsp.place(out, parts.clip(lib, "count_tick", 4000.0, 300.0), t(0.98), 0.5)
    return out


def lamps(lib, gen) -> np.ndarray:
    """Three uneven flickers (relay, spark, a bark of hum), a dark gap, then the lamp circuit ramping up onto D."""
    t = _Timeline("boot_lamps")
    out = np.zeros(t.n)
    for at, gain in ((0.0, 0.8), (0.17, 0.55), (0.42, 0.7)):
        dsp.place(out, parts.clip(lib, "relay_click", 4500.0, 300.0), t(at), 0.5 * gain)
        spark = filters.bandpass(lib("arc_spark")[samples(0.04):samples(0.1)], 3000.0, 0.8)
        dsp.place(out, dsp.peak_normalize(spark), t(at) + 0.005, 0.12 * gain)
        bark_n = samples(0.08)
        bark = parts.hum(lib, bark_n, 1.0, float(gen.uniform(2.0, 15.0))) * dsp.shape(bark_n, [
            (0.0, 0.0), (0.01, 1.0), (0.08, 0.0)])
        dsp.place(out, bark, t(at) + 0.004, 0.5 * gain)
    dsp.place(out, parts.clip(lib, "lamp_switch", 4500.0), t(0.9), 0.55)
    ramp_n = samples(t(1.5))
    ratio = 2.0 ** (dsp.contour(ramp_n, t.points([(0.0, -3.0), (0.6, 0.0), (1.5, 0.0)])) / 12.0)
    dsp.place(out, parts.hum(lib, ramp_n, ratio) * dsp.shape(ramp_n, t.points([(0.0, 0.0), (1.1, 1.0), (1.5, 0.0)])),
              t(0.9), 0.8)
    return out


def head(lib, gen) -> np.ndarray:
    """The head's servo sweeps out and back with the gear motor under it, grit sliding off, then a nod."""
    t = _Timeline("boot_head")
    out = np.zeros(t.n)
    sweep_n = samples(t(1.25))
    sweep = parts.servo(lib, gen, sweep_n, "D4", tuple(t.points(((0.0, -3.0), (0.6, 2.0), (1.1, -1.0),
                                                                 (1.25, -1.0)))), 0.55)
    sweep += 0.45 * parts.gear(lib, gen, sweep_n, "D6")
    dsp.place(out, sweep * dsp.shape(sweep_n, t.points([(0.0, 0.0), (0.08, 1.0), (1.1, 0.9), (1.25, 0.0)])), 0.0)
    grit = parts.clip(lib, "scrape_grit", 4500.0, 300.0, 0.957)
    dsp.place(out, grit * dsp.shape(len(grit), [(0.0, 0.0), (0.1, 1.0), (len(grit) / 48000.0, 0.0)]), t(0.2), 0.15)
    nod_n = samples(t(0.4))
    dsp.place(out, parts.servo(lib, gen, nod_n, "D4", tuple(t.points(((0.0, 0.0), (0.2, -2.0), (0.4, 0.0)))), 0.5)
              * dsp.shape(nod_n, t.points([(0.0, 0.0), (0.05, 1.0), (0.4, 0.0)])), t(1.3))
    dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0), t(1.68), 0.6)
    return out


def wheels(lib, gen) -> np.ndarray:
    """Four wheels twitch in turn: a short grunt of gear motor and a clunk each, each wheel a little different."""
    t = _Timeline("boot_wheels")
    out = np.zeros(t.n)
    for at, rate, note in ((0.1, 1.0, "D6"), (0.55, 0.9, "B5"), (1.0, 1.08, "E6"), (1.45, 0.84, "A5")):
        grunt_n = samples(0.14)
        grunt = parts.gear(lib, gen, grunt_n, note) * dsp.shape(grunt_n, [(0.0, 0.0), (0.02, 1.0), (0.14, 0.0)])
        dsp.place(out, grunt, t(at), 0.5)
        dsp.place(out, parts.clip(lib, "pedal_clunk", 3000.0, 80.0, rate), t(at) + 0.06, 0.7)
    return out


def motor(lib, gen) -> np.ndarray:
    """The drive motor spins up through a shudder (07 creeps forward a hand's width) and settles."""
    t = _Timeline("boot_motor")
    rate = dsp.contour(t.n, t.points([(0.0, 0.45), (1.0, 1.0), (1.8, 1.0)]))
    gear = dsp.varispeed(lib("crane_motor"), rate * 1.227, t.n, float(gen.uniform(0.0, 20.0)))
    gear = dsp.peak_normalize(filters.lowpass(gear, 1500.0))
    depth = dsp.contour(t.n, t.points([(0.0, 0.7), (1.2, 0.15), (1.8, 0.1)]))
    shudder = 1.0 - depth * (0.5 + 0.5 * np.sin(2.0 * np.pi * 9.0 * np.arange(t.n) / 48000.0))
    whine = parts.servo(lib, gen, t.n, "D3", tuple(t.points(((0.0, -12.0), (1.0, 0.0), (1.8, 0.0)))), 0.35)
    out = (gear * shudder + whine) * dsp.shape(t.n, t.points([(0.0, 0.0), (0.15, 0.7), (0.9, 1.0), (1.4, 0.8),
                                                             (1.8, 0.0)]))
    grit = parts.clip(lib, "grit_roll", 3500.0, 200.0)[:samples(0.8)]
    dsp.place(out, grit * dsp.shape(len(grit), [(0.0, 0.0), (0.2, 1.0), (0.8, 0.0)]), t(0.7), 0.12)
    return out


def cutter(lib, gen) -> np.ndarray:
    """The cutter tries to ignite three times (a relay, a whine rising, a spark that fizzles, weaker each time) and
    dies; the fault itself is voiced when the result comes in."""
    t = _Timeline("boot_cutter")
    out = np.zeros(t.n)
    for at, strength in ((0.1, 1.0), (0.75, 0.75), (1.3, 0.5)):
        dsp.place(out, parts.clip(lib, "relay_click", 4500.0, 300.0), t(at), 0.45)
        rise_n = samples(0.3)
        dsp.place(out, parts.servo(lib, gen, rise_n, "A5", ((0.0, -7.0), (0.3, 0.0)), 0.3 * strength)
                  * dsp.shape(rise_n, [(0.0, 0.0), (0.05, 1.0), (0.3, 0.6)]), t(at) + 0.02)
        spark = lib("arc_spark")[samples(0.04):samples(0.04 + 0.22 * strength)]
        spark = filters.butter(filters.bandpass(spark, 3000.0, 0.6), "lowpass", 4000.0, order=4)
        dsp.place(out, dsp.peak_normalize(spark) * dsp.shape(len(spark), [(0.0, 1.0), (len(spark) / 48000.0, 0.0)]),
                  t(at) + 0.3, 0.32 * strength)
        fizz = parts.clip(lib, "crackle", 3500.0, 800.0)[:samples(0.25)]
        dsp.place(out, fizz * dsp.shape(len(fizz), [(0.0, 1.0), (0.25, 0.0)]), t(at) + 0.45, 0.15 * strength)
    return out


def wing_jam(lib, gen) -> np.ndarray:
    """The solar wing's servo strains, jerks half open with a clunk, stalls grinding against the jam, folds back."""
    t = _Timeline("boot_wing_jam")
    out = np.zeros(t.n)
    strain_n = samples(t(0.65))
    strain = (parts.servo(lib, gen, strain_n, "D4", tuple(t.points(((0.0, -3.0), (0.6, 1.0)))), 0.55)
              + 0.4 * parts.gear(lib, gen, strain_n, "B5"))
    dsp.place(out, strain * dsp.shape(strain_n, t.points([(0.0, 0.0), (0.06, 1.0), (0.65, 1.0)])), 0.0)
    dsp.place(out, parts.clip(lib, "door_clunk", 3000.0, 70.0), t(0.62), 0.8)
    stall_n = samples(t(0.6))
    stall = parts.servo(lib, gen, stall_n, "D4", tuple(t.points(((0.0, 1.0), (0.6, 1.3)))), 0.4)
    stall += 0.3 * dsp.peak_normalize(filters.butter(dsp.varispeed(lib("metal_squeal"), 0.62, stall_n, 0.3),
                                                     "lowpass", 2400.0, order=2))
    stall *= dsp.stutter(stall_n, gen, 0.0, 14.0, 0.7)
    dsp.place(out, stall * dsp.shape(stall_n, t.points([(0.0, 1.0), (0.5, 0.8), (0.6, 0.0)])), t(0.7))
    back_n = samples(t(0.6))
    dsp.place(out, parts.servo(lib, gen, back_n, "D4", tuple(t.points(((0.0, 1.0), (0.6, -5.0)))), 0.5)
              * dsp.shape(back_n, t.points([(0.0, 0.0), (0.06, 1.0), (0.6, 0.0)])), t(1.32))
    dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0), t(1.95), 0.7)
    return out


def antenna(lib, gen) -> np.ndarray:
    """The mast runs up on its sprung tape, latches, and the receiver opens onto orbit's hiss (receive only)."""
    t = _Timeline("boot_antenna")
    out = np.zeros(t.n)
    run_n = samples(t(1.0))
    # The sprung tape clacks as it runs out: its teeth are held under the run so the mast's travel carries.
    run = dsp.peak_normalize(effects.limiter(parts.clip(lib, "tape_spring", 5000.0, 150.0, 0.7)[:run_n],
                                             ceiling_db=-22.0))
    dsp.place(out, run * dsp.shape(run_n, t.points([(0.0, 0.0), (0.05, 1.0), (0.9, 0.8), (1.0, 0.0)])), 0.0, 0.8)
    dsp.place(out, parts.clip(lib, "carabiner_screw", 4500.0, 300.0)[:samples(0.4)], t(0.55), 0.15)
    dsp.place(out, parts.clip(lib, "latch_thunk", 3000.0), t(1.0), 0.3)
    hiss_n = samples(t(0.95))
    hiss = dsp.varispeed(lib("radio_sweep"), 1.0, hiss_n, float(gen.uniform(0.0, 25.0)))
    # The old AM sweep crackles: its pops are held under the hiss so orbit's static is heard, not its spikes.
    hiss = effects.limiter(dsp.peak_normalize(filters.bandpass(hiss, 1600.0, 0.6)), ceiling_db=-26.0)
    hiss = effects.wow_flutter(dsp.peak_normalize(hiss), wow_cents=18.0, wow_rate=0.6, flutter_cents=6.0, gen=gen)
    dsp.place(out, hiss * dsp.shape(hiss_n, t.points([(0.0, 0.0), (0.35, 0.6), (0.95, 0.4)])), t(1.05), 1.3)
    return out


def battery(lib, gen) -> np.ndarray:
    """The cell relays close one by one, each tick a pentatonic step higher, the charge whine rising under them,
    settling at half."""
    t = _Timeline("boot_battery")
    out = np.zeros(t.n)
    # The tick rings near 430 Hz: 1.023 puts it on A4, then each relay a D-pentatonic step up (A B D E).
    for at, step in ((0.08, 0.0), (0.32, 2.0), (0.56, 5.0), (0.8, 7.0)):
        dsp.place(out, parts.clip(lib, "relay_click", 3500.0, 300.0), t(at), 0.2)
        tick = parts.clip(lib, "count_tick", 3000.0, 300.0, 1.023 * 2.0 ** (step / 12.0))
        dsp.place(out, tick, t(at) + 0.01, 0.18)
    whine = parts.servo(lib, gen, t.n, "D5", tuple(t.points(((0.0, -5.0), (0.9, 0.0), (1.4, 0.0)))), 0.4)
    dsp.place(out, whine * dsp.shape(t.n, t.points([(0.0, 0.0), (0.1, 0.6), (0.9, 1.0), (1.4, 0.0)])), 0.0)
    return out


def assembly(lib, gen) -> np.ndarray:
    """The assembly system finds no blueprints: a latch settles and 07 asks on F#, a short rising query."""
    t = _Timeline("boot_assembly")
    out = np.zeros(t.n)
    dsp.place(out, parts.clip(lib, "case_latch", 3500.0), 0.0, 0.6)
    dsp.place(out, voice.render(lib, gen, "voice_query", 0), t(0.15), 0.8)
    return out


def begin(lib, gen) -> np.ndarray:
    """Recovery begins: the head turns to the lander's beacon, 07 shivers off 36 years of dust, the headlamps come on,
    and a mechanical ready chord (D, F#, A) settles."""
    t = _Timeline("boot_begin")
    out = np.zeros(t.n)
    turn_n = samples(t(1.3))
    turn = (parts.servo(lib, gen, turn_n, "D4", tuple(t.points(((0.0, -2.0), (0.9, 1.0), (1.3, 0.0)))), 0.5)
            + 0.4 * parts.gear(lib, gen, turn_n, "A5"))
    dsp.place(out, turn * dsp.shape(turn_n, t.points([(0.0, 0.0), (0.1, 1.0), (1.1, 0.8), (1.3, 0.0)])), 0.0)
    shiver_n = samples(1.2)
    shiver = (0.6 * dsp.fit(parts.clip(lib, "metal_rattle", 6000.0, 300.0), shiver_n)
              + 0.5 * dsp.fit(parts.clip(lib, "spring_clatter", 5000.0, 400.0), shiver_n)
              + 0.6 * dsp.fit(parts.clip(lib, "grit_roll", 4000.0, 200.0), shiver_n)
              + 0.4 * dsp.fit(parts.clip(lib, "dust_wipe", 4000.0, 300.0), shiver_n))
    shiver = effects.tremolo(shiver, 13.0, 0.5) * dsp.shape(shiver_n, [(0.0, 0.0), (0.1, 1.0), (0.8, 0.6), (1.2, 0.0)])
    dsp.place(out, shiver, t(1.35), 0.55)
    dsp.place(out, parts.clip(lib, "lamp_switch", 4500.0), t(2.65), 0.55)
    glow_n = samples(1.0)
    dsp.place(out, parts.hum(lib, glow_n, 1.0) * dsp.shape(glow_n, [(0.0, 0.0), (0.3, 1.0), (1.0, 0.0)]), t(2.66),
              0.35)
    chord = voice.gesture(lib, gen, [
        voice.Syllable(0.0, 1.0, "D5", ((0.0, -1.0), (0.1, 0.0), (1.0, -0.2)), 0.8, attack=0.03, release=0.35),
        voice.Syllable(0.07, 0.93, "F#5", ((0.0, -1.0), (0.1, 0.0), (0.93, -0.2)), 0.65, attack=0.03, release=0.35),
        voice.Syllable(0.14, 0.86, "A5", ((0.0, -1.0), (0.1, 0.0), (0.86, -0.2)), 0.6, attack=0.03, release=0.35),
    ], 1.05, click="relay_click")
    dsp.place(out, chord, LENGTHS["boot_begin"] - 1.08, 0.9)
    return out


STEPS = {
    "boot_lens": lens, "boot_lamps": lamps, "boot_head": head, "boot_wheels": wheels, "boot_motor": motor,
    "boot_cutter": cutter, "boot_wing_jam": wing_jam, "boot_antenna": antenna, "boot_battery": battery,
    "boot_assembly": assembly, "boot_begin": begin,
}
