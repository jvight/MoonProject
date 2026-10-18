"""Building cues (Group 3).

- build_ghost: the ghost appearing (a CRT projector hum)
- build_invalid: the invalid spot (a low muted thunk)
- build_stage_complete: a stage complete (a clunk plus a small pentatonic chime)
- build_deconstruct: the deconstruct

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder idea).
All tonal material is in D major pentatonic (D E F# A B).
"""
import numpy as np

from synth import envelope, instruments, osc
from synth.core import note_freq, place, samples

from . import bench


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
