"""07's CRT face cues (Group 1).

07 has no voice; its sounds come from its CRT screen and electro-mechanical body:
- face_boot: tube warm-up hum and horizontal scanline opening into eyes
- face_blink: a soft relay tick
- feeling stingers: face_happy, face_worried, face_asleep (slow purr), face_startled, face_proud

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder electro-mechanical idea).
Never synth beeps; all tonal resonances sit in D major pentatonic.
"""
import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples
from synth.pluck import karplus_strong

from . import bench


def face_boot(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT face boot: tube warm-up hum (0..0.45 s horizontal line) and vertical eye-open bloom (0.45..1.25 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): bakelite power switch + D2/A2 heater transformer hum + deflection yoke
        # speaker-cone thump and plucked phosphor-screen coil ring opening D3 -> A3 -> D4.
        n = samples(1.28)
        mix = np.zeros(n)
        place(mix, 0.72 * bench.bakelite_toggle(note_freq("D3"), 0.24, gen, direction="up", decay_s=0.16), 0)

        # Heater filament & deflection coil hum swelling during the horizontal scanline stage (0..0.45 s).
        hum_n = samples(1.18)
        hum = (
            0.78 * osc.sine(hum_n, note_freq("D3"))
            + 0.45 * osc.sine(hum_n, note_freq("A2"))
            + 0.32 * osc.sine(hum_n, note_freq("D2"))
            + 0.22 * osc.sine(hum_n, note_freq("A3"), phase=0.2)
        )
        hum *= envelope.segments(
            hum_n,
            [(0.0, 0.0), (0.12, 0.55), (0.45, 0.95), (0.95, 0.50), (1.18, 0.0)],
            shape="smooth",
        )
        place(mix, 0.52 * hum, samples(0.03))

        # Horizontal line ignition tick at 0.14 s, then vertical eye-open relay + cone bloom at 0.45 s.
        place(mix, 0.42 * bench.relay_armature(note_freq("A3"), 0.16, gen, decay_s=0.10), samples(0.14))
        place(mix, 0.68 * bench.speaker_cone_thump(note_freq("D4"), 0.58, gen, decay_s=0.38), samples(0.45))
        place(mix, 0.54 * bench.warm_chime_voice(note_freq("A4"), 0.68, gen, decay_s=0.45), samples(0.54))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.006, drive_db=2.8, lp_hz=2700.0)

    # Variant B (Bolder idea): CRT degauss coil thrum through the steel shadow mask + capstan flywheel spin-up
    # + dual reed-relay contacts settling into a warm felt-damped D4/F#4/A4 chassis chord.
    n = samples(1.36)
    mix = np.zeros(n)
    degauss_n = samples(0.62)
    degauss = (
        0.85 * osc.sine(degauss_n, note_freq("D2"))
        + 0.65 * osc.sine(degauss_n, note_freq("D3"), phase=0.15)
        + 0.35 * osc.sine(degauss_n, note_freq("A3"), phase=0.33)
    ) * envelope.ar(degauss_n, 0.015, 0.48)
    mask_buzz = filters.bandpass(noise.pink(degauss_n, gen), note_freq("D4"), 3.2) * envelope.ar(degauss_n, 0.008, 0.18)
    place(mix, 0.65 * (degauss + 0.35 * mask_buzz), 0)

    # Capstan flywheel spin-up rub into vertical eye opening at 0.44 s.
    spin = bench.servo_worm_gear(0.46, gen, note_freq("A2"), note_freq("D3"), tooth_rate_hz=18.0)
    place(mix, 0.42 * spin, samples(0.06))
    for idx, (note, gain) in enumerate((("D4", 0.78), ("F#4", 0.58), ("A4", 0.64))):
        onset = 0.44 + 0.055 * idx
        place(mix, 0.38 * bench.felt_latch(note_freq(note), 0.16, gen, decay_s=0.09), samples(onset))
        place(mix, gain * instruments.felt_piano(note_freq(note), 0.78, gen, decay=0.55, brightness=0.38),
              samples(onset + 0.008))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.4, lp_hz=2600.0, wet=0.15)


def face_blink(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT face blink: a soft relay tick as the amber eye closes and reopens (~0.18 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): soft enclosed reed-relay close & reopen tick (D4 -> A4) with paper-cone cushion.
        n = samples(0.20)
        mix = np.zeros(n)
        place(mix, 0.72 * bench.relay_armature(note_freq("D4"), 0.10, gen, decay_s=0.055, muted=True), 0)
        place(mix, 0.48 * bench.speaker_cone_thump(note_freq("D4"), 0.11, gen, decay_s=0.06, cone_weight=0.45),
              samples(0.003))
        place(mix, 0.58 * bench.relay_armature(note_freq("A4"), 0.10, gen, decay_s=0.060, muted=True), samples(0.074))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.2, lp_hz=2400.0, wet=0.08,
                                       flutter_cents=0.0)

    # Variant B (Bolder idea): camera leaf-shutter felt eyelash tap ('tuk-tik' on A3 -> D4, warm wood/felt cavity).
    n = samples(0.19)
    mix = np.zeros(n)
    place(mix, 0.76 * bench.felt_latch(note_freq("A3"), 0.09, gen, decay_s=0.048, wood_body=0.68), 0)
    place(mix, 0.82 * bench.felt_latch(note_freq("D4"), 0.10, gen, decay_s=0.055, wood_body=0.72), samples(0.068))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.002, drive_db=2.0, lp_hz=2200.0, wet=0.07,
                                   flutter_cents=0.0)


def face_happy(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT feeling stinger — happy: short, warm electro-mechanical three-step lift in D major pentatonic."""
    if variant == 0:
        # Variant A (C2 Radio bench): stepping relay armatures + speaker-cone & bronze-spring bounce (F#4 -> A4 -> D5).
        n = samples(0.62)
        mix = np.zeros(n)
        steps = ((0.00, "F#4", 0.68, 0.18), (0.095, "A4", 0.78, 0.20), (0.195, "D5", 0.92, 0.32))
        for onset, note, gain, dec in steps:
            hz = note_freq(note)
            place(mix, 0.55 * gain * bench.relay_armature(hz, 0.16, gen, decay_s=0.09), samples(onset))
            place(mix, gain * bench.speaker_cone_thump(hz, 0.36, gen, decay_s=dec), samples(onset + 0.005))
        place(mix, 0.42 * bench.speaker_cone_thump(note_freq("D4"), 0.38, gen, decay_s=0.28), samples(0.195))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2750.0)

    # Variant B (Bolder idea): playful two-hop governor servo purr + chassis-mounted thumb-kalimba tines (A4 -> B4 -> D5).
    n = samples(0.66)
    mix = np.zeros(n)
    purr = bench.servo_worm_gear(0.32, gen, note_freq("D3"), note_freq("A3"), tooth_rate_hz=26.0)
    place(mix, 0.38 * purr, 0)
    for onset, note, gain, dec in ((0.00, "A4", 0.68, 0.24), (0.085, "B4", 0.74, 0.26), (0.175, "D5", 0.94, 0.38)):
        hz = note_freq(note)
        tine = instruments.kalimba(hz, 0.42, gen, decay=dec, tine=0.04, warmth=0.12)
        place(mix, gain * tine, samples(onset))
        place(mix, 0.32 * gain * bench.felt_latch(hz, 0.12, gen, decay_s=0.06), samples(onset))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2650.0, wet=0.14)


def face_worried(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT feeling stinger — worried: soft drooping relay/coil settle (B4 -> F#4 / A3 -> E3)."""
    if variant == 0:
        # Variant A (C2 Radio bench): two muted relay armatures dropping B4 -> F#4 with a sagging speaker-cone sigh.
        n = samples(0.64)
        mix = np.zeros(n)
        place(mix, 0.65 * bench.relay_armature(note_freq("B4"), 0.18, gen, decay_s=0.11, muted=True), 0)
        place(mix, 0.74 * bench.speaker_cone_thump(note_freq("B4"), 0.26, gen, decay_s=0.16), samples(0.006))
        place(mix, 0.58 * bench.bakelite_toggle(note_freq("F#4"), 0.24, gen, direction="down", decay_s=0.14, muted=True),
              samples(0.16))
        place(mix, 0.88 * bench.speaker_cone_thump(note_freq("F#4"), 0.42, gen, decay_s=0.28, sag_semitones=-0.12),
              samples(0.168))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2200.0)

    # Variant B (Bolder idea): brow-servo worm-gear easing downward (A3 -> E3) with slipping clutch detent ticks
    # and a closing low-pass filter like a drooping lid.
    n = samples(0.68)
    mix = np.zeros(n)
    servo = bench.servo_worm_gear(0.52, gen, note_freq("A3"), note_freq("E3"), tooth_rate_hz=17.0)
    place(mix, 0.55 * servo, 0)
    place(mix, 0.68 * bench.rotary_detent(note_freq("A3"), 0.22, gen, decay_s=0.14, muted=True), 0)
    place(mix, 0.82 * bench.rotary_detent(note_freq("E3"), 0.36, gen, decay_s=0.24, muted=True), samples(0.18))
    place(mix, 0.52 * bench.speaker_cone_thump(note_freq("E3"), 0.42, gen, decay_s=0.28), samples(0.185))
    swept = filters.swept(mix, "lowpass", np.linspace(2300.0, 1200.0, n), q=0.75)
    return bench.radio_bench_chain(swept, gen, hiss_level=0.004, drive_db=2.4, lp_hz=2050.0)


def face_asleep(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT feeling stinger — asleep: a slow, warm electro-mechanical purr as 07's screen rests."""
    if variant == 0:
        # Variant A (C2 Radio bench): soft falling bakelite switch (A3 -> D3) into a slow 1.8 Hz cathode-heater
        # & speaker-cone purr on D2/A2/D3.
        n = samples(1.45)
        mix = np.zeros(n)
        place(mix, 0.48 * bench.bakelite_toggle(note_freq("A3"), 0.22, gen, direction="down", decay_s=0.12, muted=True),
              0)
        place(mix, 0.55 * bench.speaker_cone_thump(note_freq("D3"), 0.35, gen, decay_s=0.22), samples(0.09))

        purr_n = samples(1.32)
        carrier = (
            0.82 * osc.sine(purr_n, note_freq("D3"))
            + 0.65 * osc.sine(purr_n, note_freq("A2"))
            + 0.48 * osc.sine(purr_n, note_freq("D2"))
        )
        purr_grain = filters.bandpass(noise.pink(purr_n, gen), 320.0, 2.0)
        purr_grain /= max(float(np.std(purr_grain)), 1e-9)
        pulse = envelope.lfo(purr_n, 16.0, 0.28, 0.72) * envelope.lfo(purr_n, 1.5, 0.35, 0.65)
        env = envelope.segments(
            purr_n,
            [(0.0, 0.0), (0.22, 0.90), (0.92, 0.75), (1.32, 0.0)],
            shape="smooth",
        )
        place(mix, (0.68 * carrier + 0.12 * purr_grain) * pulse * env, samples(0.10))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.5, lp_hz=1750.0, hp_hz=55.0)

    # Variant B (Bolder idea): cooling vacuum-tube glass ticks + slow clockwork flywheel governor purr on D3/F#3/A2.
    n = samples(1.50)
    mix = np.zeros(n)
    for onset, note, gain in ((0.04, "A4", 0.32), (0.34, "F#4", 0.26), (0.78, "D4", 0.22)):
        place(mix, gain * bench.felt_latch(note_freq(note), 0.10, gen, decay_s=0.05), samples(onset))

    purr_n = samples(1.42)
    chord = (
        0.85 * osc.sine(purr_n, note_freq("D3"))
        + 0.50 * osc.sine(purr_n, note_freq("A2"))
        + 0.38 * osc.sine(purr_n, note_freq("F#3"), phase=0.25)
    )
    escapement = envelope.lfo(purr_n, 13.5, 0.32, 0.68)
    breath = envelope.segments(
        purr_n,
        [(0.0, 0.0), (0.35, 0.95), (0.75, 0.55), (1.05, 0.80), (1.42, 0.0)],
        shape="smooth",
    )
    place(mix, 0.75 * chord * escapement * breath, samples(0.04))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.2, lp_hz=1850.0, hp_hz=55.0, wet=0.16)


def face_startled(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT feeling stinger — startled: brisk double-relay & spring reflex (never harsh or loud)."""
    if variant == 0:
        # Variant A (C2 Radio bench): quick double armature clack + upward speaker-cone deflection jump (D4 -> A4).
        n = samples(0.46)
        mix = np.zeros(n)
        place(mix, 0.78 * bench.relay_armature(note_freq("D4"), 0.14, gen, double=True, decay_s=0.08), 0)
        place(mix, 0.68 * bench.speaker_cone_thump(note_freq("D4"), 0.18, gen, decay_s=0.10), samples(0.004))
        place(mix, 0.85 * bench.rotary_detent(note_freq("A4"), 0.26, gen, decay_s=0.16), samples(0.058))
        place(mix, 0.76 * bench.speaker_cone_thump(note_freq("A4"), 0.30, gen, decay_s=0.18), samples(0.062))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.7, lp_hz=2650.0)

    # Variant B (Bolder idea): camera-iris solenoid reflex snap + plucked bronze coil spring jump (E4 -> B4 -> A4).
    n = samples(0.48)
    mix = np.zeros(n)
    place(mix, 0.72 * bench.felt_latch(note_freq("E4"), 0.12, gen, decay_s=0.06), 0)
    spring_n = samples(0.36)
    spring = karplus_strong(spring_n, note_freq("B4"), gen, decay=0.992, brightness=0.36, pick_softness=2100.0)
    spring *= envelope.ar(spring_n, 0.002, 0.16)
    place(mix, 0.62 * spring, samples(0.032))
    place(mix, 0.84 * bench.speaker_cone_thump(note_freq("A4"), 0.32, gen, decay_s=0.20), samples(0.085))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.4, lp_hz=2600.0)


def face_proud(variant: int, gen: np.random.Generator) -> np.ndarray:
    """CRT feeling stinger — proud: warm 4-step electro-mechanical climb (D4 -> F#4 -> A4 -> D5)."""
    if variant == 0:
        # Variant A (C2 Radio bench): 4-step rotary selector & relay clacks + warm speaker-cone chime over D3.
        n = samples(0.92)
        mix = np.zeros(n)
        steps = (
            (0.00, "D4", 0.68, 0.20),
            (0.11, "F#4", 0.74, 0.22),
            (0.22, "A4", 0.82, 0.26),
            (0.35, "D5", 0.95, 0.44),
        )
        for onset, note, gain, dec in steps:
            hz = note_freq(note)
            place(mix, 0.52 * gain * bench.rotary_detent(hz, 0.18, gen, decay_s=0.11), samples(onset))
            place(mix, gain * bench.speaker_cone_thump(hz, 0.48, gen, decay_s=dec), samples(onset + 0.006))
        place(mix, 0.45 * bench.speaker_cone_thump(note_freq("D3"), 0.52, gen, decay_s=0.42), samples(0.35))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.006, drive_db=2.8, lp_hz=2800.0)

    # Variant B (Bolder idea): gentle neck-lift servo rise + 3-voice spun-brass & felt-piano chord (D4 -> A4 -> D5).
    n = samples(0.96)
    mix = np.zeros(n)
    lift = bench.servo_worm_gear(0.38, gen, note_freq("A2"), note_freq("D3"), tooth_rate_hz=20.0)
    place(mix, 0.36 * lift, 0)
    for onset, note, gain, dec in ((0.02, "D4", 0.72, 0.35), (0.14, "A4", 0.82, 0.42), (0.28, "D5", 0.96, 0.56)):
        hz = note_freq(note)
        place(mix, 0.35 * bench.relay_armature(hz, 0.14, gen, decay_s=0.08), samples(onset))
        place(mix, gain * bench.warm_chime_voice(hz, 0.62, gen, decay_s=dec), samples(onset + 0.005))
    place(mix, 0.42 * instruments.felt_piano(note_freq("F#4"), 0.60, gen, decay=0.45, brightness=0.38),
          samples(0.29))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.4, lp_hz=2750.0, wet=0.16)
