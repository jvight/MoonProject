"""07's power-on self-test (POST), heard from 07: one sound per motion of the New Game wake's checks (Core
BootCheckStep, Lens to Begin). A check's sound plays when it starts (BootCheckStarted) or, for a check made of several
motions, one per BootCheckPulse (Rover's indices: the lamps' flickers and ramp, the four wheels, the cutter's sparks,
the head's sweep and nod...). Real machine sources only: relays, a motor whine for every servo, the crane's gear motor,
an arc for the cutter's failed ignitions, a sprung tape for the antenna, grit and rattles for 36 years of regolith
sliding off. The tones that speak (the assembly's query, the ready chord) are 07's voice; the cutter's fault is
voiced on its result; the cells' ticks are the battery's own (BatteryAudio on FlankLampLit). Each sound is the size of
its motion in Rover's check timing (PostMotions, main 2026-10-18).
"""
import numpy as np

from synth import effects, filters
from synth.core import samples

from . import dsp, parts, voice

WHEEL_NOTES = ("D6", "B5", "E6", "A5")
WHEEL_RATES = (1.0, 0.9, 1.08, 0.84)
SPARK_STRENGTHS = (1.0, 0.8, 0.55)
FLICKER_RATES = (1.0, 1.06, 0.94)


def lens(lib, gen, _variant: int) -> np.ndarray:
    """The iris (the whole 1 s check): a ratcheting slide, the focus motor hunting around A and settling, a soft click
    when it locks."""
    n = samples(1.0)
    out = np.zeros(n)
    slide_n = samples(0.5)
    slide = parts.clip(lib, "tape_spring", 5000.0, 200.0, 0.8)[:slide_n]
    dsp.place(out, slide * dsp.shape(slide_n, [(0.0, 0.0), (0.04, 1.0), (0.5, 0.0)]), 0.0, 0.45)
    hunt = parts.servo(lib, gen, n, "A4", ((0.0, -3.0), (0.25, 1.0), (0.46, -0.6), (0.66, 0.2), (0.83, 0.0)), 0.5)
    out += hunt * dsp.shape(n, [(0.0, 0.0), (0.05, 1.0), (0.79, 0.8), (1.0, 0.0)])
    dsp.place(out, parts.clip(lib, "count_tick", 4000.0, 300.0), 0.82, 0.5)
    return out


def lamp_flicker(lib, gen, variant: int) -> np.ndarray:
    """One of the lamps' three uneven flickers: a relay, a spark, a bark of hum."""
    n = samples(0.2)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "relay_click", 4500.0, 300.0, FLICKER_RATES[variant]), 0.0, 0.5)
    spark = filters.butter(filters.bandpass(lib("arc_spark")[samples(0.04):samples(0.1)], 3000.0, 0.8), "lowpass",
                           4000.0, order=4)
    dsp.place(out, dsp.peak_normalize(spark), 0.005, 0.12)
    bark_n = samples(0.08)
    bark = parts.hum(lib, bark_n, 1.0, float(gen.uniform(2.0, 15.0))) * dsp.shape(bark_n, [
        (0.0, 0.0), (0.01, 1.0), (0.08, 0.0)])
    dsp.place(out, bark, 0.004, 0.5)
    return out


def lamp_ramp(lib, gen, _variant: int) -> np.ndarray:
    """After the gap: the lamp switch, then the lamp circuit ramping up onto D."""
    n = samples(1.5)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "lamp_switch", 4500.0), 0.0, 0.55)
    ratio = 2.0 ** (dsp.contour(n, [(0.0, -3.0), (0.6, 0.0), (1.5, 0.0)]) / 12.0)
    out += 0.8 * parts.hum(lib, n, ratio) * dsp.shape(n, [(0.0, 0.0), (1.1, 1.0), (1.5, 0.0)])
    return out


def head_sweep(lib, gen, _variant: int) -> np.ndarray:
    """The head's servo sweeping out and back with the gear motor under it, grit sliding off."""
    n = samples(1.12)
    sweep = parts.servo(lib, gen, n, "D4", ((0.0, -3.0), (0.54, 2.0), (0.99, -1.0), (1.12, -1.0)), 0.55)
    sweep += 0.45 * parts.gear(lib, gen, n, "D6")
    out = sweep * dsp.shape(n, [(0.0, 0.0), (0.07, 1.0), (0.99, 0.9), (1.12, 0.0)])
    grit = parts.clip(lib, "scrape_grit", 4500.0, 300.0, 0.957)[:samples(0.9)]
    dsp.place(out, grit * dsp.shape(len(grit), [(0.0, 0.0), (0.1, 1.0), (len(grit) / 48000.0, 0.0)]), 0.18, 0.15)
    return out


def head_nod(lib, gen, _variant: int) -> np.ndarray:
    """The nod: the servo dips and comes back up, a latch seats."""
    n = samples(0.6)
    out = parts.servo(lib, gen, n, "D4", ((0.0, 0.0), (0.2, -2.0), (0.4, 0.0)), 0.5) * \
        dsp.shape(n, [(0.0, 0.0), (0.05, 1.0), (0.4, 0.6), (0.6, 0.0)])
    dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0), 0.36, 0.6)
    return out


def wheel(lib, gen, variant: int) -> np.ndarray:
    """One wheel's twitch: a short grunt of gear motor and a clunk, each wheel a little different (one mismatched)."""
    n = samples(0.4)
    out = np.zeros(n)
    grunt_n = samples(0.14)
    grunt = parts.gear(lib, gen, grunt_n, WHEEL_NOTES[variant]) * dsp.shape(grunt_n, [(0.0, 0.0), (0.02, 1.0),
                                                                                      (0.14, 0.0)])
    dsp.place(out, grunt, 0.0, 0.5)
    dsp.place(out, parts.clip(lib, "pedal_clunk", 3000.0, 80.0, WHEEL_RATES[variant]), 0.06, 0.7)
    return out


def motor_shudder(lib, gen, _variant: int) -> np.ndarray:
    """The drive motor spinning up through a shudder and settling."""
    n = samples(1.12)
    rate = dsp.contour(n, [(0.0, 0.45), (0.62, 1.0), (1.12, 1.0)])
    gear = dsp.varispeed(lib("crane_motor"), rate * 1.227, n, float(gen.uniform(0.0, 20.0)))
    gear = dsp.peak_normalize(filters.lowpass(gear, 1500.0))
    depth = dsp.contour(n, [(0.0, 0.7), (0.75, 0.15), (1.12, 0.1)])
    shudder = 1.0 - depth * (0.5 + 0.5 * np.sin(2.0 * np.pi * 9.0 * np.arange(n) / 48000.0))
    whine = parts.servo(lib, gen, n, "D3", ((0.0, -12.0), (0.62, 0.0), (1.12, 0.0)), 0.35)
    return (gear * shudder + whine) * dsp.shape(n, [(0.0, 0.0), (0.1, 0.7), (0.56, 1.0), (0.87, 0.8), (1.12, 0.0)])


def motor_creep(lib, gen, _variant: int) -> np.ndarray:
    """07 creeping forward a hand's width: its wheels grinding a little regolith."""
    n = samples(0.72)
    grit = dsp.fit(parts.clip(lib, "grit_roll", 3500.0, 200.0), n)
    grind = parts.gear(lib, gen, n, "A5")
    return (grit + 0.3 * grind) * dsp.shape(n, [(0.0, 0.0), (0.15, 1.0), (0.5, 0.7), (0.72, 0.0)])


def cutter_spark(lib, gen, variant: int) -> np.ndarray:
    """One ignition attempt: a relay, a whine rising, a spark that fizzles out, shorter each time (the game plays each
    attempt softer than the last)."""
    strength = SPARK_STRENGTHS[variant]
    n = samples(0.75)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "relay_click", 4500.0, 300.0), 0.0, 0.45)
    rise_n = samples(0.3)
    dsp.place(out, parts.servo(lib, gen, rise_n, "A5", ((0.0, -7.0), (0.3, 0.0)), 0.3)
              * dsp.shape(rise_n, [(0.0, 0.0), (0.05, 1.0), (0.3, 0.6)]), 0.02)
    spark = lib("arc_spark")[samples(0.04):samples(0.04 + 0.22 * strength)]
    spark = filters.butter(filters.bandpass(spark, 3000.0, 0.6), "lowpass", 4000.0, order=4)
    dsp.place(out, dsp.peak_normalize(spark) * dsp.shape(len(spark), [(0.0, 1.0), (len(spark) / 48000.0, 0.0)]),
              0.3, 0.32)
    fizz_n = samples(0.25 * strength)
    fizz = parts.clip(lib, "crackle", 3500.0, 800.0)[:fizz_n]
    dsp.place(out, fizz * dsp.shape(fizz_n, [(0.0, 1.0), (fizz_n / 48000.0, 0.0)]), 0.45, 0.15)
    return out


def wing_jerk(lib, gen, _variant: int) -> np.ndarray:
    """The solar wing's servo straining, then jerking half open with a clunk."""
    n = samples(0.45)
    out = (parts.servo(lib, gen, n, "D4", ((0.0, -3.0), (0.36, 1.0)), 0.55) + 0.4 * parts.gear(lib, gen, n, "B5")) * \
        dsp.shape(n, [(0.0, 0.0), (0.05, 1.0), (0.38, 1.0), (0.45, 0.0)])
    dsp.place(out, parts.clip(lib, "door_clunk", 3000.0, 70.0)[:samples(0.1)], 0.35, 0.8)
    return out


def wing_stall(lib, gen, _variant: int) -> np.ndarray:
    """The wing stuck against the jam: the servo grinding, a dry metal squeal, stuttering."""
    n = samples(0.8)
    stall = parts.servo(lib, gen, n, "D4", ((0.0, 1.0), (0.8, 1.3)), 0.4)
    stall += 0.3 * dsp.peak_normalize(filters.butter(dsp.varispeed(lib("metal_squeal"), 0.62, n, 0.3), "lowpass",
                                                     2400.0, order=2))
    stall *= dsp.stutter(n, gen, 0.0, 14.0, 0.7)
    return stall * dsp.shape(n, [(0.0, 1.0), (0.65, 0.8), (0.8, 0.0)])


def wing_fold(lib, gen, _variant: int) -> np.ndarray:
    """The wing folding back: the servo running down, the latch seating."""
    n = samples(0.8)
    out = parts.servo(lib, gen, n, "D4", ((0.0, 1.0), (0.7, -5.0)), 0.5) * \
        dsp.shape(n, [(0.0, 0.0), (0.06, 1.0), (0.7, 0.6), (0.8, 0.0)])
    dsp.place(out, parts.clip(lib, "latch_thunk", 2500.0), 0.7, 0.4)
    return out


def antenna_rise(lib, gen, _variant: int) -> np.ndarray:
    """The mast running up on its sprung tape and latching at the top."""
    n = samples(1.2)
    out = np.zeros(n)
    run_n = samples(1.08)
    # The sprung tape clacks as it runs out: its teeth are held under the run so the mast's travel carries.
    run = dsp.peak_normalize(effects.limiter(parts.clip(lib, "tape_spring", 5000.0, 150.0, 0.7)[:run_n],
                                             ceiling_db=-22.0))
    dsp.place(out, run * dsp.shape(run_n, [(0.0, 0.0), (0.05, 1.0), (0.97, 0.8), (1.08, 0.0)]), 0.0, 0.8)
    dsp.place(out, parts.clip(lib, "carabiner_screw", 4500.0, 300.0)[:samples(0.4)], 0.5, 0.15)
    dsp.place(out, parts.clip(lib, "latch_thunk", 3000.0), 1.08, 0.3)
    return out


def antenna_signal(lib, gen, _variant: int) -> np.ndarray:
    """The receiver opening onto orbit's hiss (receive only): it swells in under the rise and holds."""
    n = samples(1.8)
    hiss = dsp.varispeed(lib("radio_sweep"), 1.0, n, float(gen.uniform(0.0, 25.0)))
    # The old AM sweep crackles: its pops are held under the hiss so orbit's static is heard, not its spikes.
    hiss = effects.limiter(dsp.peak_normalize(filters.bandpass(hiss, 1600.0, 0.6)), ceiling_db=-26.0)
    hiss = effects.wow_flutter(dsp.peak_normalize(hiss), wow_cents=18.0, wow_rate=0.6, flutter_cents=6.0, gen=gen)
    return hiss * dsp.shape(n, [(0.0, 0.0), (1.1, 0.5), (1.5, 0.6), (1.8, 0.0)])


def battery(lib, gen, _variant: int) -> np.ndarray:
    """The cells filling to half: the charge whine rising under the cell lamps (their ticks are the battery's own)."""
    n = samples(1.2)
    whine = parts.servo(lib, gen, n, "D5", ((0.0, -5.0), (0.77, 0.0), (1.2, 0.0)), 1.0)
    return whine * dsp.shape(n, [(0.0, 0.0), (0.1, 0.6), (0.77, 1.0), (1.2, 0.0)])


def assembly(lib, gen, _variant: int) -> np.ndarray:
    """The assembly system finds no blueprints: a latch settles and 07 asks on F#, a short rising query."""
    n = samples(0.7)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "case_latch", 3500.0), 0.0, 0.6)
    dsp.place(out, voice.render(lib, gen, "voice_query", 0), 0.13, 0.8)
    return out


def begin_turn(lib, gen, _variant: int) -> np.ndarray:
    """The head turning to the lander's beacon: the servo and the gear motor under it."""
    n = samples(2.0)
    turn = (parts.servo(lib, gen, n, "D4", ((0.0, -2.0), (1.4, 1.0), (2.0, 0.0)), 0.5)
            + 0.4 * parts.gear(lib, gen, n, "A5"))
    return turn * dsp.shape(n, [(0.0, 0.0), (0.15, 1.0), (1.7, 0.8), (2.0, 0.0)])


def begin_shiver(lib, gen, _variant: int) -> np.ndarray:
    """07 shivering off 36 years of dust: a rattle of panels and springs, grit pouring off the metal."""
    n = samples(1.2)
    shiver = (0.6 * dsp.fit(parts.clip(lib, "metal_rattle", 6000.0, 300.0), n)
              + 0.5 * dsp.fit(parts.clip(lib, "spring_clatter", 5000.0, 400.0), n)
              + 0.6 * dsp.fit(parts.clip(lib, "grit_roll", 4000.0, 200.0), n)
              + 0.4 * dsp.fit(parts.clip(lib, "dust_wipe", 4000.0, 300.0), n))
    return effects.tremolo(shiver, 13.0, 0.5) * dsp.shape(n, [(0.0, 0.0), (0.1, 1.0), (0.8, 0.6), (1.2, 0.0)])


def begin_lamps(lib, gen, _variant: int) -> np.ndarray:
    """The headlamps on, and a mechanical ready chord (D, F#, A) settles: the recovery begins."""
    n = samples(1.4)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "lamp_switch", 4500.0), 0.0, 0.55)
    glow_n = samples(1.0)
    dsp.place(out, parts.hum(lib, glow_n, 1.0) * dsp.shape(glow_n, [(0.0, 0.0), (0.3, 1.0), (1.0, 0.0)]), 0.01, 0.35)
    chord = voice.gesture(lib, gen, [
        voice.Syllable(0.0, 1.0, "D5", ((0.0, -1.0), (0.1, 0.0), (1.0, -0.2)), 0.8, attack=0.03, release=0.35),
        voice.Syllable(0.07, 0.93, "F#5", ((0.0, -1.0), (0.1, 0.0), (0.93, -0.2)), 0.65, attack=0.03, release=0.35),
        voice.Syllable(0.14, 0.86, "A5", ((0.0, -1.0), (0.1, 0.0), (0.86, -0.2)), 0.6, attack=0.03, release=0.35),
    ], 1.05, click="relay_click")
    dsp.place(out, chord, 0.3, 0.9)
    return out


# Cue id -> (recipe, variants).
SOUNDS = {
    "boot_lens": (lens, 1),
    "boot_lamp_flicker": (lamp_flicker, len(FLICKER_RATES)),
    "boot_lamp_ramp": (lamp_ramp, 1),
    "boot_head_sweep": (head_sweep, 1),
    "boot_head_nod": (head_nod, 1),
    "boot_wheel": (wheel, len(WHEEL_NOTES)),
    "boot_motor_shudder": (motor_shudder, 1),
    "boot_motor_creep": (motor_creep, 1),
    "boot_cutter_spark": (cutter_spark, len(SPARK_STRENGTHS)),
    "boot_wing_jerk": (wing_jerk, 1),
    "boot_wing_stall": (wing_stall, 1),
    "boot_wing_fold": (wing_fold, 1),
    "boot_antenna_rise": (antenna_rise, 1),
    "boot_antenna_signal": (antenna_signal, 1),
    "boot_battery": (battery, 1),
    "boot_assembly": (assembly, 1),
    "boot_begin_turn": (begin_turn, 1),
    "boot_begin_shiver": (begin_shiver, 1),
    "boot_begin_lamps": (begin_lamps, 1),
}
