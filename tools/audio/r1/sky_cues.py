"""The sky cues (Group 2).

- sky_diamond_ring: the eclipse-release 'diamond ring' bead (a delicate shimmer)
- sky_dawn_sweep: the dawn sweep (a slow warm swell, ~6 s)
- sky_nightfall_settle: the nightfall settle

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder idea).
All tonal material is in D major pentatonic (D E F# A B).
"""
import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, note_freq, pan, place, samples

from . import bench


def sky_diamond_ring(variant: int, gen: np.random.Generator) -> np.ndarray:
    """Eclipse-release 'diamond ring' bead: a delicate D-pentatonic shimmer as the first ray of sun breaks."""
    if variant == 0:
        # Variant A (C2 Radio bench): faint telemetry relay tick + plucked bronze-wire & soft-bell harmonic shimmer
        # (A4, D5, F#5, A5) through a warm tube-radio passband with slow tape wow.
        n = samples(2.85)
        mix = np.zeros(n)
        place(mix, 0.28 * bench.relay_armature(note_freq("A4"), 0.14, gen, decay_s=0.08, muted=True), 0)

        notes = ((0.03, "A4", 0.65, 1.4), (0.14, "D5", 0.92, 1.8), (0.27, "F#5", 0.58, 1.5), (0.42, "A5", 0.45, 1.3))
        for onset, note, gain, dec in notes:
            voice = bench.warm_chime_voice(note_freq(note), 2.2, gen, decay_s=dec, attack_s=0.012)
            place(mix, gain * voice, samples(onset))

        shimmer = effects.tremolo(mix, rate=5.2, depth=0.18)
        return bench.radio_bench_chain(shimmer, gen, hiss_level=0.005, drive_db=2.2, lp_hz=3100.0, room=0.58, wet=0.24)

    # Variant B (Bolder idea): electrostatic dust-whisper transient blooming into a warm bowed-glass & reed-organ
    # halo on D4 + A4 + D5 + F#5 with gentle stereo-summed chorus shimmer.
    n = samples(3.0)
    mix = np.zeros(n)
    whisper_n = samples(1.2)
    whisper = filters.swept(noise.pink(whisper_n, gen), "bandpass", osc.glide(whisper_n, 900.0, 2200.0), q=2.8)
    whisper /= max(float(np.std(whisper)), 1e-9)
    whisper *= envelope.segments(whisper_n, [(0.0, 0.0), (0.25, 0.12), (1.2, 0.0)], shape="smooth")
    place(mix, whisper, 0)

    for onset, note, gain in ((0.02, "D4", 0.52), (0.08, "A4", 0.68), (0.16, "D5", 0.95), (0.28, "F#5", 0.54)):
        bell = instruments.soft_bell(note_freq(note), 2.5, decay=1.8, attack=0.035, detune_cents=3.5)
        place(mix, gain * bell, samples(onset))

    pad = instruments.soft_pad(note_freq("D4"), 2.6, attack=0.35, release_t60=1.6, hold=0.3, detune_cents=3.0)
    place(mix, 0.35 * pad, samples(0.05))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=1.9, lp_hz=3200.0, room=0.65, wet=0.28)


def sky_dawn_sweep(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The dawn sweep: a slow, warm ~6 s swell as low polar sunlight sweeps across the crater floor."""
    n = samples(6.0)
    if variant == 0:
        # Variant A (C2 Radio bench): 6-second warm radio-tower transformer & valve-pad swell (D3, A3, D4, F#4, A4)
        # opening its low-pass filter from 420 Hz to 2400 Hz, with two soft relay ticks as solar panels wake.
        mix = np.zeros(n)
        for note, gain, att in (("D3", 0.72, 1.4), ("A3", 0.68, 1.6), ("D4", 0.95, 1.8), ("F#4", 0.54, 2.1),
                                ("A4", 0.48, 2.4)):
            pad = instruments.soft_pad(note_freq(note), 5.8, attack=att, release_t60=2.4, hold=0.8, detune_cents=3.5)
            place(mix, gain * pad, 0)

        # Distant solar-panel wake relay ticks at 1.2 s and 2.6 s.
        place(mix, 0.22 * bench.relay_armature(note_freq("A3"), 0.18, gen, decay_s=0.11, muted=True), samples(1.20))
        place(mix, 0.26 * bench.relay_armature(note_freq("D4"), 0.20, gen, decay_s=0.13, muted=True), samples(2.60))

        cutoff = envelope.segments(
            n,
            [(0.0, 420.0), (2.6, 2350.0), (4.4, 2100.0), (6.0, 680.0)],
            shape="smooth",
        )
        swept = filters.swept(mix, "lowpass", cutoff, q=0.85)
        return bench.radio_bench_chain(swept, gen, hiss_level=0.005, drive_db=2.4, lp_hz=2600.0, room=0.62, wet=0.24)

    # Variant B (Bolder idea): deep horizon terminator wave (D2/A2/D3 sub-pad + swept dust-glow air) over which
    # five gentle felt-piano & kalimba notes (D4 -> E4 -> F#4 -> A4 -> D5) bloom across 6 seconds.
    mix = np.zeros(n)
    sub = (
        0.65 * instruments.soft_pad(note_freq("D3"), 5.8, attack=1.5, release_t60=2.5, hold=0.9, detune_cents=3.0)
        + 0.45 * instruments.soft_pad(note_freq("A2"), 5.8, attack=1.6, release_t60=2.5, hold=0.9, detune_cents=2.5)
    )
    place(mix, sub, 0)

    air = filters.swept(
        noise.pink(n, gen),
        "bandpass",
        envelope.segments(n, [(0.0, 320.0), (2.8, 1250.0), (6.0, 420.0)], shape="smooth"),
        q=2.0,
    )
    air /= max(float(np.std(air)), 1e-9)
    air *= envelope.segments(n, [(0.0, 0.0), (2.2, 0.06), (4.5, 0.04), (6.0, 0.0)], shape="smooth")
    place(mix, air, 0)

    phrase = ((0.45, "D4", 0.65), (1.15, "E4", 0.58), (1.85, "F#4", 0.68), (2.60, "A4", 0.76), (3.40, "D5", 0.90))
    for onset, note, gain in phrase:
        piano = instruments.felt_piano(note_freq(note), 2.4, gen, decay=1.6, brightness=0.42)
        place(mix, gain * piano, samples(onset))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.1, lp_hz=2700.0, room=0.68, wet=0.28)


def sky_nightfall_settle(variant: int, gen: np.random.Generator) -> np.ndarray:
    """The nightfall settle: solar relays drop open and a warm chord settles into earthlit quiet (~3.5 s)."""
    if variant == 0:
        # Variant A (C2 Radio bench): solar contactor relay releasing + descending tube-receiver sigh
        # (A4 -> F#4 -> D4 over D3 speaker cone warmth) as the passband darkens.
        n = samples(3.5)
        mix = np.zeros(n)
        place(mix, 0.42 * bench.bakelite_toggle(note_freq("A3"), 0.24, gen, direction="down", decay_s=0.14, muted=True),
              0)
        notes = ((0.06, "A4", 0.62, 1.1), (0.42, "F#4", 0.68, 1.3), (0.82, "D4", 0.92, 1.9))
        for onset, note, gain, dec in notes:
            place(mix, gain * bench.speaker_cone_thump(note_freq(note), 2.2, gen, decay_s=dec), samples(onset))

        pad = instruments.soft_pad(note_freq("D3"), 3.2, attack=0.45, release_t60=1.8, hold=0.3, detune_cents=3.0)
        place(mix, 0.52 * pad, samples(0.15))
        swept = filters.swept(mix, "lowpass", np.linspace(2400.0, 950.0, n), q=0.75)
        return bench.radio_bench_chain(swept, gen, hiss_level=0.004, drive_db=2.3, lp_hz=2100.0, room=0.55, wet=0.22)

    # Variant B (Bolder idea): quiet thermal-contraction metal ticks + slow felt-piano nocturnal cadence
    # (B3 -> A3 -> F#3 -> D3) dissolving into wide lunar stillness.
    n = samples(3.6)
    mix = np.zeros(n)
    place(mix, 0.28 * bench.felt_latch(note_freq("A4"), 0.12, gen, decay_s=0.06), samples(0.08))
    place(mix, 0.24 * bench.felt_latch(note_freq("D4"), 0.12, gen, decay_s=0.06), samples(0.62))

    cadence = ((0.02, "B3", 0.55), (0.38, "A3", 0.62), (0.76, "F#3", 0.68), (1.18, "D3", 0.95))
    for onset, note, gain in cadence:
        piano = instruments.felt_piano(note_freq(note), 2.2, gen, decay=1.5, brightness=0.35)
        place(mix, gain * piano, samples(onset))

    pad = instruments.soft_pad(note_freq("D3"), 2.4, attack=0.4, release_t60=1.5, hold=0.2, detune_cents=3.0)
    place(mix, 0.42 * pad, samples(1.10))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2000.0, room=0.65, wet=0.26)
