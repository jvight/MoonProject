"""Building cues (Group 3).

- build_ghost: the ghost appearing (a CRT projector hum)
- build_snap: the snap (a soft magnetic clack)
- build_invalid: the invalid spot (a low muted thunk)
- build_place: the placement (a frame drop, a light metal settle)
- build_weld_loop: the weld loop (a gentle crackle, not harsh, loopable 4 s)
- build_stage_complete: a stage complete (a clunk plus a small pentatonic chime)
- build_structure_complete: the structure completed (the lamp lights: a warm relay plus a chime)
- build_deconstruct: the deconstruct

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder idea).
All tonal material is in D major pentatonic (D E F# A B).
"""
import math

import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, loop_freq, note_freq, place, samples
from synth.loop import periodic

from . import bench

WELD_LOOP_S = 4.0


def build_ghost(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The ghost appearing: a CRT projector hum as the structure wireframe materializes."""
    if variant == 0:
        # Variant A (C2 Radio bench): rotary projector switch click + warm CRT deflection-coil hum swelling
        # on D3/A3/D4 with a 5 Hz magnetic scan flutter.
        n = samples(0.88)
        mix = np.zeros(n)
        place(mix, 0.58 * bench.rotary_detent(note_freq("D4"), 0.18, gen, decay_s=0.11), 0)

        hum_n = samples(0.82)
        hum = (
            0.84 * osc.sine(hum_n, note_freq("D3"))
            + 0.56 * osc.sine(hum_n, note_freq("A3"), phase=0.15)
            + 0.34 * osc.sine(hum_n, note_freq("D4"), phase=0.31)
        )
        flutter = envelope.lfo(hum_n, 5.5, 0.16, 0.84)
        env = envelope.segments(hum_n, [(0.0, 0.0), (0.14, 0.92), (0.58, 0.72), (0.82, 0.0)], shape="smooth")
        place(mix, 0.72 * hum * flutter * env, samples(0.03))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.006, drive_db=2.6, lp_hz=2400.0)

    # Variant B (Bolder idea): physical optical lens-barrel slide + electrostatic wireframe bloom (D3 + F#4 + A4).
    n = samples(0.94)
    mix = np.zeros(n)
    slide = bench.paper_slide(0.28, gen, f_start=520.0, f_end=1180.0, tone_freq=note_freq("A3"))
    place(mix, 0.45 * slide, 0)
    place(mix, 0.38 * bench.felt_latch(note_freq("D4"), 0.14, gen, decay_s=0.08), samples(0.06))

    bloom_n = samples(0.84)
    wireframe = (
        0.78 * osc.sine(bloom_n, note_freq("D4"))
        + 0.48 * osc.sine(bloom_n, note_freq("A3"))
        + 0.35 * osc.sine(bloom_n, note_freq("F#4"), phase=0.22)
    )
    env = envelope.segments(bloom_n, [(0.0, 0.0), (0.18, 0.95), (0.56, 0.68), (0.84, 0.0)], shape="smooth")
    place(mix, 0.75 * wireframe * env, samples(0.06))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.2, lp_hz=2550.0, wet=0.18)


def build_snap(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The snap: a soft magnetic clack when the build ghost aligns to a grid or socket."""
    if variant == 0:
        # Variant A (C2 Radio bench): magnetic reed-latch pull-in + damped steel pin seating on A3 -> D4.
        n = samples(0.26)
        mix = np.zeros(n)
        place(mix, 0.62 * bench.relay_armature(note_freq("A3"), 0.12, gen, decay_s=0.07), 0)
        place(mix, 0.88 * bench.rotary_detent(note_freq("D4"), 0.20, gen, decay_s=0.12), samples(0.028))
        place(mix, 0.64 * bench.speaker_cone_thump(note_freq("D4"), 0.20, gen, decay_s=0.12), samples(0.032))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.5, lp_hz=2600.0, flutter_cents=0.0)

    # Variant B (Bolder idea): heavy neodymium coupling 'thock-clack' with rubber gasket cushion on D3/D4.
    n = samples(0.28)
    mix = np.zeros(n)
    place(mix, 0.72 * bench.felt_latch(note_freq("D3"), 0.14, gen, decay_s=0.08, wood_body=0.75), 0)
    place(mix, 0.85 * bench.bakelite_toggle(note_freq("D4"), 0.22, gen, direction="up", decay_s=0.13),
          samples(0.024))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.3, lp_hz=2450.0, flutter_cents=0.0)


def build_invalid(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The invalid spot: a low muted thunk (never a harsh buzzer)."""
    if variant == 0:
        # Variant A (C2 Radio bench): muted bakelite switch blocked against a rubber stop + damped low speaker-cone
        # thud falling B3 -> F#3.
        n = samples(0.34)
        mix = np.zeros(n)
        place(mix, 0.68 * bench.relay_armature(note_freq("B3"), 0.14, gen, decay_s=0.07, muted=True), 0)
        place(mix, 0.65 * bench.speaker_cone_thump(note_freq("B3"), 0.16, gen, decay_s=0.08), samples(0.004))
        place(mix, 0.78 * bench.bakelite_toggle(note_freq("F#3"), 0.20, gen, direction="down", decay_s=0.09, muted=True),
              samples(0.082))
        place(mix, 0.86 * bench.speaker_cone_thump(note_freq("F#3"), 0.22, gen, decay_s=0.11), samples(0.086))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.6, lp_hz=1750.0, hp_hz=65.0,
                                       flutter_cents=0.0)

    # Variant B (Bolder idea): hollow rubber-mallet thud against a damped aluminium strut (A3 -> D3).
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.72 * bench.felt_latch(note_freq("A3"), 0.15, gen, decay_s=0.07, wood_body=0.70), 0)
    place(mix, 0.85 * bench.felt_latch(note_freq("D3"), 0.25, gen, decay_s=0.14, wood_body=0.85), samples(0.080))
    place(mix, 0.72 * bench.speaker_cone_thump(note_freq("D3"), 0.25, gen, decay_s=0.16, cone_weight=0.70),
          samples(0.080))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.2, lp_hz=1600.0, hp_hz=55.0,
                                   flutter_cents=0.0)


def build_place(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The placement: a frame drop onto regolith, then a light metal settle."""
    if variant == 0:
        # Variant A (C2 Radio bench): heavy steel base-frame drop on regolith (D3) + two staggered light aluminium
        # strut settles (A3, D4) through the warm bench speaker.
        n = samples(0.96)
        mix = np.zeros(n)
        place(mix, 0.88 * bench.steel_frame_impact(note_freq("D3"), 0.65, gen, regolith=0.42, decay_s=0.36), 0)
        place(mix, 0.56 * bench.relay_armature(note_freq("A3"), 0.24, gen, decay_s=0.15), samples(0.14))
        place(mix, 0.64 * bench.rotary_detent(note_freq("D4"), 0.32, gen, decay_s=0.20), samples(0.26))
        place(mix, 0.52 * bench.speaker_cone_thump(note_freq("D4"), 0.42, gen, decay_s=0.26), samples(0.265))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.8, lp_hz=2650.0)

    # Variant B (Bolder idea): three staggered tripod landing-foot drops + spring anchor latches clicking home
    # into a warm D3/F#3/A3 tubular truss resonance.
    n = samples(1.02)
    mix = np.zeros(n)
    for onset, note, gain in ((0.00, "D3", 0.78), (0.075, "F#3", 0.68), (0.155, "A3", 0.85)):
        hz = note_freq(note)
        place(mix, gain * bench.steel_frame_impact(hz, 0.52, gen, regolith=0.35, decay_s=0.30), samples(onset))
    place(mix, 0.58 * bench.bakelite_toggle(note_freq("D4"), 0.28, gen, direction="up", decay_s=0.18), samples(0.32))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.5, lp_hz=2550.0, wet=0.15)


def _weld_grains(
    n: int,
    gen: np.random.Generator,
    rate: float,
    dur_range: tuple,
    gain: float,
) -> np.ndarray:
    """Periodic scatter of soft, rounded arc-crackle grains (no harsh top)."""
    return instruments.scatter(
        n,
        gen,
        rate,
        lambda: instruments.grain(gen, gen.uniform(*dur_range), 0.0012, gen.uniform(0.003, 0.010)),
        lambda: gain * math.exp(gen.normal(0.0, 0.42)),
        wrap=True,
    )


def build_weld_loop(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The weld loop: a gentle crackle, not harsh, seamlessly loopable 4.0 s."""
    n = samples(WELD_LOOP_S)
    d2 = loop_freq(note_freq("D2"), n)
    a2 = loop_freq(note_freq("A2"), n)
    d3 = loop_freq(note_freq("D3"), n)
    a3 = loop_freq(note_freq("A3"), n)

    if variant == 0:
        # Variant A (C2 Radio bench): warm D2/A2/D3 welding-transformer hum through a vintage speaker cone
        # + soft carbon-arc crackle grains filtered between 350 Hz and 2.1 kHz (like vinyl/hearth crackle).
        hum = (
            0.85 * osc.sine(n, d3)
            + 0.65 * osc.sine(n, a2, phase=0.18)
            + 0.55 * osc.sine(n, d2, phase=0.37)
            + 0.28 * osc.sine(n, a3, phase=0.51)
        ) * envelope.lfo(n, 2.0 / WELD_LOOP_S, 0.14, 0.86)

        fine_crackle = _weld_grains(n, gen, rate=34.0, dur_range=(0.004, 0.011), gain=0.26)
        warm_pops = _weld_grains(n, gen, rate=11.0, dur_range=(0.010, 0.024), gain=0.34)
        sizzle = noise.white(n, gen) * envelope.lfo(n, 5.0 / WELD_LOOP_S, 0.22, 0.78)

        def _process_a(s: np.ndarray) -> np.ndarray:
            shaped = filters.highpass(filters.lowpass(filters.lowpass(s, 2150.0), 2150.0), 55.0)
            return filters.dc_block(effects.saturate(shaped, drive=1.45), 18.0)

        crackle_band = periodic(
            fine_crackle,
            lambda s: filters.bandpass(s, 1180.0, 1.3),
            warmup=samples(0.5),
        )
        pop_band = periodic(
            warm_pops,
            lambda s: filters.bandpass(s, 540.0, 1.5),
            warmup=samples(0.5),
        )
        hiss_band = periodic(
            sizzle,
            lambda s: filters.bandpass(noise.pink_filter(s), 780.0, 1.4),
            warmup=samples(0.5),
        )
        raw = 0.72 * hum + 0.52 * crackle_band + 0.58 * pop_band + 0.18 * hiss_band
        return periodic(raw, _process_a, warmup=samples(1.0))

    # Variant B (Bolder idea): vacuum-chassis induction stitcher loop — deep pulsing D2/D3/A3 magnetic induction
    # purr felt through 07's wheels, with muffled solder-bubble pops and warm filament hum.
    f_sharp3 = loop_freq(note_freq("F#3"), n)
    induction = (
        0.90 * osc.sine(n, d3)
        + 0.70 * osc.sine(n, d2, phase=0.12)
        + 0.48 * osc.sine(n, a2, phase=0.29)
        + 0.32 * osc.sine(n, f_sharp3, phase=0.44)
    )
    pulse = envelope.lfo(n, 8.0 / WELD_LOOP_S, 0.22, 0.78) * envelope.lfo(n, 2.0 / WELD_LOOP_S, 0.12, 0.88)
    bubbles = _weld_grains(n, gen, rate=18.0, dur_range=(0.008, 0.022), gain=0.32)
    bubble_band = periodic(
        bubbles,
        lambda s: filters.bandpass(s, float(d3 * 2.0), 2.2) + 0.6 * filters.bandpass(s, float(a3 * 2.0), 2.0),
        warmup=samples(0.5),
    )
    bed_noise = periodic(
        noise.white(n, gen),
        lambda s: filters.bandpass(noise.brown_filter(s, 0.995), 280.0, 1.4),
        warmup=samples(0.5),
    )
    raw = 0.78 * induction * pulse + 0.50 * bubble_band + 0.22 * bed_noise
    return periodic(
        raw,
        lambda s: filters.highpass(filters.lowpass(filters.lowpass(s, 1850.0), 1850.0), 48.0),
        warmup=samples(1.0),
    )


def build_stage_complete(variant: int, gen: np.random.Generator) -> np.ndarray:
    """A stage complete: a mechanical clunk plus a small D-major-pentatonic chime."""
    if variant == 0:
        # Variant A (C2 Radio bench): steel structural latch clunk (D3) + relay armature click + warm two-note
        # bench chime (A4 -> D5) through the speaker cone.
        n = samples(1.18)
        mix = np.zeros(n)
        place(mix, 0.72 * bench.steel_frame_impact(note_freq("D3"), 0.42, gen, regolith=0.15, decay_s=0.24), 0)
        place(mix, 0.58 * bench.relay_armature(note_freq("A4"), 0.18, gen, decay_s=0.11), samples(0.08))
        place(mix, 0.76 * bench.warm_chime_voice(note_freq("A4"), 0.68, gen, decay_s=0.45), samples(0.09))
        place(mix, 0.92 * bench.warm_chime_voice(note_freq("D5"), 0.86, gen, decay_s=0.62), samples(0.22))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2850.0)

    # Variant B (Bolder idea): crisp torque-wrench release click + seated plate thud + 3-note kalimba/brass
    # chime (F#4 -> A4 -> D5).
    n = samples(1.24)
    mix = np.zeros(n)
    place(mix, 0.75 * bench.bakelite_toggle(note_freq("D3"), 0.26, gen, direction="up", decay_s=0.16), 0)
    place(mix, 0.55 * bench.speaker_cone_thump(note_freq("D3"), 0.34, gen, decay_s=0.20), samples(0.012))
    for onset, note, gain, dec in ((0.09, "F#4", 0.64, 0.42), (0.19, "A4", 0.74, 0.48), (0.30, "D5", 0.94, 0.68)):
        hz = note_freq(note)
        tine = instruments.kalimba(hz, 0.82, gen, decay=dec, tine=0.05, warmth=0.10)
        place(mix, gain * tine, samples(onset))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2800.0, wet=0.16)


def build_structure_complete(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The structure completed: the amber lamp lights (a warm relay plus a resolving pentatonic chime)."""
    if variant == 0:
        # Variant A (C2 Radio bench): heavy contactor relay clack-thump + warm filament surge bloom (D3) as the
        # amber lamp lights, blossoming into a 4-note Radio Bench chord (D4 -> F#4 -> A4 -> D5).
        n = samples(2.15)
        mix = np.zeros(n)
        place(mix, 0.78 * bench.bakelite_toggle(note_freq("D3"), 0.28, gen, direction="up", decay_s=0.18), 0)
        place(mix, 0.65 * bench.relay_armature(note_freq("A3"), 0.22, gen, double=True, decay_s=0.14), samples(0.04))

        filament = instruments.soft_pad(note_freq("D3"), 1.9, attack=0.22, release_t60=1.2, hold=0.3, detune_cents=3.0)
        place(mix, 0.48 * filament, samples(0.05))

        notes = ((0.10, "D4", 0.72, 0.95), (0.21, "F#4", 0.68, 0.95), (0.32, "A4", 0.78, 1.05),
                 (0.45, "D5", 0.95, 1.35))
        for onset, note, gain, dec in notes:
            voice = bench.warm_chime_voice(note_freq(note), 1.55, gen, decay_s=dec)
            place(mix, gain * voice, samples(onset))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2900.0, room=0.45, wet=0.20)

    # Variant B (Bolder idea): double knife-relay latch + warm capacitor swell into an unhurried felt-piano &
    # soft-bell resolution (D3 + A3 + F#4 + A4 + D5).
    n = samples(2.25)
    mix = np.zeros(n)
    place(mix, 0.68 * bench.relay_armature(note_freq("D3"), 0.22, gen, double=True, decay_s=0.14), 0)
    place(mix, 0.62 * bench.speaker_cone_thump(note_freq("D3"), 0.55, gen, decay_s=0.38), samples(0.02))

    chord = ((0.08, "D4", 0.74), (0.17, "F#4", 0.66), (0.27, "A4", 0.76), (0.39, "D5", 0.96))
    for onset, note, gain in chord:
        hz = note_freq(note)
        piano = instruments.felt_piano(hz, 1.75, gen, decay=1.25, brightness=0.44)
        bell = instruments.soft_bell(hz, 1.75, decay=1.35, attack=0.025)
        place(mix, gain * (0.65 * piano + 0.35 * bell), samples(onset))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.2, lp_hz=2850.0, room=0.52, wet=0.24)


def build_deconstruct(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The deconstruct: unlatching relay, reverse servo wind-down, and parts folding neatly into 07's hold."""
    if variant == 0:
        # Variant A (C2 Radio bench): unlatching relay clack + reverse servo wind-down (A3 -> D3) + three
        # descending metallic part-stowing ticks (A4 -> F#4 -> D4) with speaker-cone settle.
        n = samples(1.12)
        mix = np.zeros(n)
        place(mix, 0.68 * bench.bakelite_toggle(note_freq("A3"), 0.22, gen, direction="down", decay_s=0.13), 0)
        servo = bench.servo_worm_gear(0.58, gen, note_freq("A3"), note_freq("D3"), tooth_rate_hz=19.0)
        place(mix, 0.52 * servo, samples(0.05))

        for onset, note, gain in ((0.18, "A4", 0.64), (0.34, "F#4", 0.70), (0.52, "D4", 0.86)):
            hz = note_freq(note)
            place(mix, gain * bench.rotary_detent(hz, 0.22, gen, decay_s=0.13, muted=True), samples(onset))
            place(mix, 0.65 * gain * bench.speaker_cone_thump(hz, 0.28, gen, decay_s=0.16), samples(onset + 0.006))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2550.0)

    # Variant B (Bolder idea): pneumatic pressure release sigh + spring clamps releasing in sequence
    # (B4 -> A4 -> F#4 -> D4) + cushioned canvas-and-steel fold into the cargo bay.
    n = samples(1.16)
    mix = np.zeros(n)
    sigh = bench.hydraulic_cylinder(0.48, gen, note_freq("A3"), note_freq("D3"), fluid_start_hz=1100.0,
                                    fluid_end_hz=380.0)
    place(mix, 0.54 * sigh, 0)
    for onset, note, gain in ((0.06, "B4", 0.52), (0.20, "A4", 0.60), (0.36, "F#4", 0.68), (0.54, "D4", 0.88)):
        place(mix, gain * bench.felt_latch(note_freq(note), 0.24, gen, decay_s=0.12, wood_body=0.72), samples(onset))
    place(mix, 0.62 * bench.steel_frame_impact(note_freq("D3"), 0.45, gen, regolith=0.15, decay_s=0.22),
          samples(0.56))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2450.0, wet=0.14)
