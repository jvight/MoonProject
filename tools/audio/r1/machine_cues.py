"""Machine cues (Group 4).

- machine_solar_track: solar array tracking (a slow servo whir)
- machine_wings_unfold: wings unfold (hydraulic)
- machine_wings_fold: wings fold (hydraulic)
- machine_pad_contacts: charging-pad contacts rising (clunk-hiss)
- machine_transformer_loop: the transformer purr loop (seamless 4 s loop)
- machine_pylon_switch: the pylon knife switch (a big clack)
- machine_cable_drums: the cable drums
- machine_lander_ramp: the lander ramp (hydraulic plus a hollow thud)
- machine_cargo_hatch: the cargo hatch
- machine_crate_lid: the crate lid

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder idea).
All tonal material is in D major pentatonic (D E F# A B).
"""
import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, loop_freq, note_freq, place, samples
from synth.loop import periodic
from synth.pluck import karplus_strong

from . import bench

TRANSFORMER_LOOP_S = 4.0


def machine_solar_track(variant: int, gen: np.random.Generator) -> np.ndarray:
    """Solar array tracking: a slow servo whir (~1.25 s, matching Azimuth/Elevation easeDuration 1.10 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): slow low-RPM worm-gear azimuth servo whir on D3 -> E3 -> D3 with soft
        # stepper-relay pulses and a gentle detent settle.
        n = samples(1.26)
        mix = np.zeros(n)
        whir = bench.servo_worm_gear(1.12, gen, note_freq("D3"), note_freq("E3"), tooth_rate_hz=16.0)
        place(mix, 0.85 * whir, 0)
        place(mix, 0.35 * bench.relay_armature(note_freq("D3"), 0.14, gen, decay_s=0.08, muted=True), 0)
        place(mix, 0.45 * bench.rotary_detent(note_freq("A3"), 0.22, gen, decay_s=0.12, muted=True), samples(1.02))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.4, lp_hz=2100.0)

    # Variant B (Bolder idea): clockwork sun-tracker escapement & gimbal bearing (soft ticking gear train at 12 Hz
    # over a warm D3/A3/F#3 housing resonance).
    n = samples(1.28)
    mix = np.zeros(n)
    body_n = samples(1.16)
    housing = (
        0.82 * osc.sine(body_n, note_freq("D3"))
        + 0.48 * osc.sine(body_n, note_freq("A3"), phase=0.2)
        + 0.30 * osc.sine(body_n, note_freq("F#3"), phase=0.4)
    )
    housing *= envelope.segments(body_n, [(0.0, 0.0), (0.15, 0.85), (0.95, 0.75), (1.16, 0.0)], shape="smooth")
    place(mix, 0.65 * housing, 0)

    for step in range(9):
        onset = 0.06 + 0.11 * step
        note = "A3" if step % 2 == 0 else "D4"
        gain = 0.28 * math.sin(math.pi * (step + 1) / 10.0)
        place(mix, gain * bench.felt_latch(note_freq(note), 0.08, gen, decay_s=0.04), samples(onset))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.1, lp_hz=2050.0, wet=0.14)


import math  # noqa: E402


def machine_wings_unfold(variant: int, gen: np.random.Generator) -> np.ndarray:
    """Wings unfold (hydraulic): solenoid release, rising hydraulic stroke, and dual-wing lock clack (~1.30 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): solenoid valve click + rising hydraulic cylinder sweep (D3 -> A3)
        # + dual-wing lock clack on D4.
        n = samples(1.32)
        mix = np.zeros(n)
        place(mix, 0.56 * bench.relay_armature(note_freq("D3"), 0.16, gen, decay_s=0.10), 0)
        cyl = bench.hydraulic_cylinder(1.08, gen, note_freq("D3"), note_freq("A3"), fluid_start_hz=420.0,
                                       fluid_end_hz=920.0)
        place(mix, 0.82 * cyl, samples(0.04))
        place(mix, 0.74 * bench.bakelite_toggle(note_freq("D4"), 0.24, gen, direction="up", decay_s=0.15),
              samples(1.04))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2450.0)

    # Variant B (Bolder idea): two-stage pantograph scissor-hinge deployment with oiled brass slides (D3 -> F#3 -> A3)
    # and twin spring-detent latches clicking open.
    n = samples(1.36)
    mix = np.zeros(n)
    stroke1 = bench.hydraulic_cylinder(0.55, gen, note_freq("D3"), note_freq("F#3"), fluid_start_hz=380.0,
                                       fluid_end_hz=680.0)
    stroke2 = bench.hydraulic_cylinder(0.58, gen, note_freq("F#3"), note_freq("A3"), fluid_start_hz=580.0,
                                       fluid_end_hz=960.0)
    place(mix, 0.72 * stroke1, 0)
    place(mix, 0.45 * bench.rotary_detent(note_freq("F#3"), 0.16, gen, decay_s=0.10), samples(0.50))
    place(mix, 0.78 * stroke2, samples(0.52))
    place(mix, 0.68 * bench.rotary_detent(note_freq("A3"), 0.22, gen, decay_s=0.14), samples(1.04))
    place(mix, 0.62 * bench.rotary_detent(note_freq("D4"), 0.24, gen, decay_s=0.15), samples(1.09))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2400.0, wet=0.15)


def machine_wings_fold(variant: int, gen: np.random.Generator) -> np.ndarray:
    """Wings fold (hydraulic): pressure-relief valve sigh, descending hydraulic stroke, and cushioned stow thud."""
    if variant == 0:
        # Variant A (C2 Radio bench): pressure-relief valve sigh + descending hydraulic return stroke (A3 -> D3)
        # + cushioned rubber-bumper stow thud on D3.
        n = samples(1.26)
        mix = np.zeros(n)
        place(mix, 0.52 * bench.relay_armature(note_freq("A3"), 0.16, gen, decay_s=0.09, muted=True), 0)
        cyl = bench.hydraulic_cylinder(1.02, gen, note_freq("A3"), note_freq("D3"), fluid_start_hz=880.0,
                                       fluid_end_hz=360.0)
        place(mix, 0.80 * cyl, samples(0.03))
        place(mix, 0.78 * bench.speaker_cone_thump(note_freq("D3"), 0.28, gen, decay_s=0.18, cone_weight=0.80),
              samples(0.98))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.5, lp_hz=2250.0)

    # Variant B (Bolder idea): latch release tick + smooth gravity-assisted linkage fold (A3 -> E3 -> D3)
    # settling onto wool-felt stowage bumpers with a quiet double 'tuk'.
    n = samples(1.28)
    mix = np.zeros(n)
    place(mix, 0.48 * bench.bakelite_toggle(note_freq("A3"), 0.18, gen, direction="down", decay_s=0.10, muted=True), 0)
    servo = bench.servo_worm_gear(0.96, gen, note_freq("A3"), note_freq("D3"), tooth_rate_hz=15.0)
    place(mix, 0.74 * servo, samples(0.05))
    place(mix, 0.72 * bench.felt_latch(note_freq("E3"), 0.18, gen, decay_s=0.10, wood_body=0.75), samples(0.94))
    place(mix, 0.85 * bench.felt_latch(note_freq("D3"), 0.22, gen, decay_s=0.13, wood_body=0.80), samples(1.02))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.2, lp_hz=2150.0, wet=0.14)


def machine_pad_contacts(variant: int, gen: np.random.Generator) -> np.ndarray:
    """Charging-pad contacts rising (clunk-hiss): heavy clunk, pneumatic lift hiss, and contact shoe lock (~0.92 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): heavy magnetic release clunk (D3) + pneumatic lift hiss (0.55 s)
        # + copper contact shoes seating with a warm A3 -> D4 inductive hum.
        n = samples(0.94)
        mix = np.zeros(n)
        place(mix, 0.82 * bench.steel_frame_impact(note_freq("D3"), 0.35, gen, regolith=0.10, decay_s=0.20), 0)
        hiss = bench.hydraulic_cylinder(0.56, gen, note_freq("D3"), note_freq("A3"), fluid_start_hz=620.0,
                                        fluid_end_hz=1240.0)
        place(mix, 0.68 * hiss, samples(0.06))
        place(mix, 0.75 * bench.relay_armature(note_freq("D4"), 0.24, gen, double=True, decay_s=0.16), samples(0.58))
        place(mix, 0.65 * bench.speaker_cone_thump(note_freq("D4"), 0.32, gen, decay_s=0.22), samples(0.59))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.7, lp_hz=2650.0)

    # Variant B (Bolder idea): pressurized bellows hiss lifting first, then a staggered two-shoe heavy brass
    # clunk-lock (D3 + A3) and soft transformer link hum.
    n = samples(0.96)
    mix = np.zeros(n)
    bellows = bench.hydraulic_cylinder(0.52, gen, note_freq("A2"), note_freq("D3"), fluid_start_hz=540.0,
                                       fluid_end_hz=1080.0)
    place(mix, 0.72 * bellows, 0)
    place(mix, 0.80 * bench.bakelite_toggle(note_freq("D3"), 0.26, gen, direction="up", decay_s=0.16), samples(0.46))
    place(mix, 0.85 * bench.bakelite_toggle(note_freq("A3"), 0.30, gen, direction="up", decay_s=0.20), samples(0.54))
    hum_n = samples(0.36)
    link_hum = (0.75 * osc.sine(hum_n, note_freq("D3")) + 0.45 * osc.sine(hum_n, note_freq("A3"))) * envelope.ar(
        hum_n, 0.02, 0.28
    )
    place(mix, 0.55 * link_hum, samples(0.56))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.5, lp_hz=2550.0, wet=0.14)


def machine_transformer_loop(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The transformer purr loop: seamless 4.0 s loop of warm 1980s lamination-stack transformer hum."""
    n = samples(TRANSFORMER_LOOP_S)
    d2 = loop_freq(note_freq("D2"), n)
    a2 = loop_freq(note_freq("A2"), n)
    d3 = loop_freq(note_freq("D3"), n)
    a3 = loop_freq(note_freq("A3"), n)

    if variant == 0:
        # Variant A (C2 Radio bench): warm lamination-stack transformer purr on D2/A2/D3 through a bakelite
        # cabinet, breathing with a slow 0.5 Hz load swell and faint cooling-louver air whisper.
        core = (
            0.92 * osc.sine(n, d3)
            + 0.78 * osc.sine(n, d2, phase=0.11)
            + 0.62 * osc.sine(n, a2, phase=0.27)
            + 0.34 * osc.sine(n, a3, phase=0.43)
            + 0.14 * osc.sine(n, 2.0 * d3, phase=0.61)
        )
        swell = envelope.lfo(n, 2.0 / TRANSFORMER_LOOP_S, 0.12, 0.88)
        purr_mod = envelope.lfo(n, 16.0 / TRANSFORMER_LOOP_S, 0.08, 0.92)
        air = periodic(
            noise.white(n, gen),
            lambda s: filters.bandpass(noise.pink_filter(s), 420.0, 1.8),
            warmup=samples(0.5),
        )
        raw = 0.85 * core * swell * purr_mod + 0.08 * air
        return periodic(
            raw,
            lambda s: filters.dc_block(
                effects.saturate(filters.highpass(filters.lowpass(filters.lowpass(s, 1600.0), 1600.0), 45.0),
                                 drive=1.4),
                18.0,
            ),
            warmup=samples(1.0),
        )

    # Variant B (Bolder idea): deeper oil-immersed polyphase inductor purr (D2 + A2 + D3 + F#3) with a 3-cycle
    # magnetic beat and soft rhythmic regulator ticks buried in the warm bed.
    f_sharp3 = loop_freq(note_freq("F#3"), n)
    step_hz = SAMPLE_RATE / n
    polyphase = (
        0.88 * osc.sine(n, d3)
        + 0.22 * osc.sine(n, d3 + step_hz, phase=0.35)
        + 0.74 * osc.sine(n, d2, phase=0.14)
        + 0.54 * osc.sine(n, a2, phase=0.28)
        + 0.36 * osc.sine(n, f_sharp3, phase=0.52)
    )
    beat = envelope.lfo(n, 3.0 / TRANSFORMER_LOOP_S, 0.14, 0.86)
    ticks = np.zeros(n)
    for k in range(8):
        pos = int(k * n / 8)
        tick_seg = bench.felt_latch(note_freq("D3"), 0.08, gen, decay_s=0.04, wood_body=0.5)
        place(ticks, 0.08 * tick_seg, pos, wrap=True)
    raw = 0.88 * polyphase * beat + ticks
    return periodic(
        raw,
        lambda s: filters.highpass(filters.lowpass(filters.lowpass(s, 1450.0), 1450.0), 42.0),
        warmup=samples(1.0),
    )


def machine_pylon_switch(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The pylon knife switch: a big mechanical clack + busbar/cable ring (~0.80 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): heavy copper knife-blade lever swing + big two-jaw porcelain-and-steel clack
        # on D3/A3 followed by a short 120 ms inductive contact hum.
        n = samples(0.84)
        mix = np.zeros(n)
        swing = bench.paper_slide(0.14, gen, f_start=380.0, f_end=920.0)
        place(mix, 0.42 * swing, 0)
        place(mix, 0.92 * bench.bakelite_toggle(note_freq("D3"), 0.42, gen, direction="up", decay_s=0.26),
              samples(0.08))
        place(mix, 0.78 * bench.relay_armature(note_freq("A3"), 0.34, gen, double=True, decay_s=0.22), samples(0.095))
        place(mix, 0.84 * bench.speaker_cone_thump(note_freq("D3"), 0.48, gen, decay_s=0.32, cone_weight=0.85),
              samples(0.10))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=3.0, lp_hz=2650.0)

    # Variant B (Bolder idea): dual-pole breaker throw ('ka-CHUNK' on D3/A3) exciting a taut copper cable-drum
    # sympathetic waveguide ring (D3).
    n = samples(0.88)
    mix = np.zeros(n)
    place(mix, 0.65 * bench.rotary_detent(note_freq("A3"), 0.18, gen, decay_s=0.10), 0)
    place(mix, 0.95 * bench.steel_frame_impact(note_freq("D3"), 0.58, gen, regolith=0.12, decay_s=0.36), samples(0.09))
    cable_n = samples(0.65)
    cable = karplus_strong(cable_n, note_freq("D3"), gen, decay=0.996, brightness=0.32, pick_softness=1600.0)
    cable *= envelope.ar(cable_n, 0.004, 0.45)
    place(mix, 0.62 * cable, samples(0.10))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.7, lp_hz=2550.0, wet=0.16)


def machine_cable_drums(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The cable drums: heavy steel spools spinning with ratchet pawls and armoured-cable rub (~1.12 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): greased roller-bearing drum spin + accelerating/decelerating pawl-ratchet
        # click train on D3/A3 + coiled cable rub.
        n = samples(1.14)
        mix = np.zeros(n)
        roll = bench.servo_worm_gear(0.98, gen, note_freq("D3"), note_freq("A2"), tooth_rate_hz=12.0)
        place(mix, 0.72 * roll, 0)

        pawls = (0.04, 0.13, 0.21, 0.29, 0.38, 0.49, 0.62, 0.78, 0.94)
        for idx, onset in enumerate(pawls):
            note = "D4" if idx % 2 == 0 else "A3"
            gain = 0.58 * math.sin(math.pi * (idx + 1) / (len(pawls) + 1))
            place(mix, gain * bench.rotary_detent(note_freq(note), 0.14, gen, decay_s=0.08, muted=True),
                  samples(onset))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2400.0)

    # Variant B (Bolder idea): three-drum planetary winch roll (warm D2/A2/D3 rumble) with braided copper cable
    # layering and a final ratchet dog locking home on D3.
    n = samples(1.18)
    mix = np.zeros(n)
    winch = bench.hydraulic_cylinder(0.95, gen, note_freq("A2"), note_freq("D3"), fluid_start_hz=320.0,
                                     fluid_end_hz=640.0)
    place(mix, 0.78 * winch, 0)
    for idx, onset in enumerate((0.12, 0.30, 0.50, 0.72)):
        place(mix, 0.42 * bench.felt_latch(note_freq("A3"), 0.14, gen, decay_s=0.07), samples(onset))
    place(mix, 0.85 * bench.bakelite_toggle(note_freq("D3"), 0.28, gen, direction="down", decay_s=0.18),
          samples(0.88))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.4, lp_hz=2300.0, wet=0.15)


def machine_lander_ramp(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The lander ramp: hydraulic lowering plus a deep hollow thud as the ramp hits the regolith (~1.50 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): bay latch release clack + 1.0 s dual hydraulic lowering groan/hiss (A3 -> D3)
        # + deep hollow ribbed-steel ramp thud in lunar dust.
        n = samples(1.52)
        mix = np.zeros(n)
        place(mix, 0.65 * bench.bakelite_toggle(note_freq("A3"), 0.22, gen, direction="down", decay_s=0.14), 0)
        hyd = bench.hydraulic_cylinder(1.02, gen, note_freq("A3"), note_freq("D3"), fluid_start_hz=760.0,
                                       fluid_end_hz=340.0)
        place(mix, 0.76 * hyd, samples(0.05))
        place(mix, 0.96 * bench.steel_frame_impact(note_freq("D3"), 0.52, gen, regolith=0.48, decay_s=0.35),
              samples(1.02))
        place(mix, 0.72 * bench.speaker_cone_thump(note_freq("D2"), 0.45, gen, decay_s=0.30, cone_weight=0.90),
              samples(1.03))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.8, lp_hz=2400.0, hp_hz=50.0)

    # Variant B (Bolder idea): dog-clamp unbolting + hydraulic bleed with slow chain-roller clicks over 1.0 s,
    # culminating in a booming hollow lander-belly plate thump (D2/A2/D3 modal ring).
    n = samples(1.56)
    mix = np.zeros(n)
    place(mix, 0.62 * bench.relay_armature(note_freq("A3"), 0.18, gen, double=True, decay_s=0.11), 0)
    hyd = bench.servo_worm_gear(0.98, gen, note_freq("A3"), note_freq("D3"), tooth_rate_hz=11.0)
    place(mix, 0.68 * hyd, samples(0.06))
    for onset in (0.18, 0.36, 0.55, 0.75, 0.92):
        place(mix, 0.32 * bench.felt_latch(note_freq("D3"), 0.10, gen, decay_s=0.05), samples(onset))
    place(mix, 0.95 * bench.steel_frame_impact(note_freq("A2"), 0.55, gen, regolith=0.52, decay_s=0.38),
          samples(1.02))
    place(mix, 0.75 * bench.steel_frame_impact(note_freq("D3"), 0.50, gen, regolith=0.30, decay_s=0.34),
          samples(1.04))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.6, lp_hz=2300.0, hp_hz=48.0, wet=0.18)


def machine_cargo_hatch(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The cargo hatch: rotary latch pop, gas-strut lift hiss, and top-stop mechanical click (~1.10 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): rotary latch pop (D3) + pneumatic gas-strut lift hiss (D3 -> A3)
        # + top-stop click on D4.
        n = samples(1.12)
        mix = np.zeros(n)
        place(mix, 0.68 * bench.bakelite_toggle(note_freq("D3"), 0.22, gen, direction="up", decay_s=0.14), 0)
        strut = bench.hydraulic_cylinder(0.76, gen, note_freq("D3"), note_freq("A3"), fluid_start_hz=480.0,
                                         fluid_end_hz=1020.0)
        place(mix, 0.74 * strut, samples(0.06))
        place(mix, 0.76 * bench.relay_armature(note_freq("D4"), 0.24, gen, decay_s=0.15), samples(0.80))
        place(mix, 0.62 * bench.speaker_cone_thump(note_freq("D4"), 0.28, gen, decay_s=0.18), samples(0.81))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2600.0)

    # Variant B (Bolder idea): push-button latch clack + torsion-spring hinge glide with hollow cream-enamel
    # shell resonance (A3 -> E4) and felt-cushioned open detent.
    n = samples(1.14)
    mix = np.zeros(n)
    place(mix, 0.64 * bench.rotary_detent(note_freq("A3"), 0.20, gen, decay_s=0.12), 0)
    hinge = bench.servo_worm_gear(0.74, gen, note_freq("A3"), note_freq("E4"), tooth_rate_hz=18.0)
    place(mix, 0.68 * hinge, samples(0.05))
    place(mix, 0.82 * bench.felt_latch(note_freq("E4"), 0.26, gen, decay_s=0.15, wood_body=0.75), samples(0.78))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2550.0, wet=0.15)


def machine_crate_lid(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The crate lid: twin butterfly-clasp snaps and ribbed-aluminium lid swinging open to its stop (~1.05 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): twin butterfly-clasp snaps (staggered 60 ms on E4, B4) + ribbed-aluminium
        # lid hinge swing + hollow box rest thud on D3/D4.
        n = samples(1.06)
        mix = np.zeros(n)
        place(mix, 0.68 * bench.relay_armature(note_freq("E4"), 0.16, gen, decay_s=0.10), 0)
        place(mix, 0.72 * bench.relay_armature(note_freq("B4"), 0.16, gen, decay_s=0.10), samples(0.062))
        swing = bench.paper_slide(0.45, gen, f_start=420.0, f_end=780.0, tone_freq=note_freq("A3"))
        place(mix, 0.48 * swing, samples(0.14))
        place(mix, 0.85 * bench.speaker_cone_thump(note_freq("D4"), 0.38, gen, decay_s=0.24, cone_weight=0.78),
              samples(0.60))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.005, drive_db=2.6, lp_hz=2650.0)

    # Variant B (Bolder idea): unbuckling two heavy steel latches + brief vacuum-gasket sigh + padded lid resting
    # back against its stop with a warm wooden/phenolic knock on A3 -> D4.
    n = samples(1.08)
    mix = np.zeros(n)
    place(mix, 0.66 * bench.bakelite_toggle(note_freq("E4"), 0.18, gen, direction="up", decay_s=0.11), 0)
    place(mix, 0.70 * bench.bakelite_toggle(note_freq("A4"), 0.18, gen, direction="up", decay_s=0.11), samples(0.07))
    gasket = bench.hydraulic_cylinder(0.36, gen, note_freq("A3"), note_freq("D3"), fluid_start_hz=920.0,
                                      fluid_end_hz=440.0)
    place(mix, 0.46 * gasket, samples(0.16))
    place(mix, 0.88 * bench.felt_latch(note_freq("D4"), 0.36, gen, decay_s=0.20, wood_body=0.82), samples(0.58))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2500.0, wet=0.14)
