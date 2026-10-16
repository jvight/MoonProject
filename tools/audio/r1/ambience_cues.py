"""Loopable 60-second ambience beds (Group 6).

- ambience_earthlit_night: earthlit night (60 s seamless loop)
- ambience_sunlit_day: sunlit day (60 s seamless loop)
- ambience_eclipse: eclipse (60 s seamless loop)

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder electro-mechanical/acoustic idea).
Synthesized with loop-periodic D-pentatonic harmonics (`loop_freq`), cross-faded filtered noise beds,
and wrap-placed (`place(..., wrap=True)`) pre-reverbed voices so the 60.0 s loop seam is mathematically seamless.
"""
import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, loop_freq, note_freq, place, samples, to_mono
from synth.loop import crossfade_loop, periodic

from . import bench

BED_DURATION_S = 60.0


def _seamless_noise_bed(
    n: int,
    gen: np.random.Generator,
    low_hz: float,
    high_hz: float,
    gain: float,
    lfo_cycles: int = 3,
    lfo_depth: float = 0.20,
    brown: bool = False,
) -> np.ndarray:
    """Create a 60 s seamlessly looping filtered noise floor with slow periodic breathing."""
    fade_n = samples(2.0)
    raw_n = n + fade_n
    raw = noise.brown(raw_n, gen) if brown else noise.pink(raw_n, gen)
    shaped = filters.highpass(filters.lowpass(filters.lowpass(raw, high_hz), high_hz), low_hz)
    looped = crossfade_loop(shaped, n, fade_n, equal_power=True)
    mod = envelope.lfo(n, float(lfo_cycles) / BED_DURATION_S, lfo_depth, 1.0 - lfo_depth)
    out = looped * mod * gain
    return out - float(np.mean(out))


def _periodic_bench_drone(
    n: int,
    notes_gains: list[tuple[str, float]],
    lfo_cycles: int = 2,
    lfo_depth: float = 0.16,
    drive: float = 1.25,
    lp_hz: float = 1400.0,
) -> np.ndarray:
    """Create a 60 s phase-continuous warm D-pentatonic valve/reed drone."""
    out = np.zeros(n, dtype=np.float64)
    for idx, (note_name, amp) in enumerate(notes_gains):
        f0 = loop_freq(note_freq(note_name), n)
        f2 = loop_freq(note_freq(note_name) * 2.0, n)
        mod = envelope.lfo(
            n,
            float(lfo_cycles + idx) / BED_DURATION_S,
            lfo_depth,
            1.0 - lfo_depth,
            phase=0.17 * idx,
        )
        voice = 0.84 * osc.sine(n, f0) + 0.16 * osc.sine(n, f2, phase=0.23)
        out += amp * mod * voice

    def _proc(s: np.ndarray) -> np.ndarray:
        sat = effects.saturate(s, drive=drive)
        return filters.highpass(filters.lowpass(sat, lp_hz), 45.0)

    processed = periodic(out, _proc, warmup=samples(1.0))
    return processed - float(np.mean(processed))


def _reverbed_chime(
    note_name: str,
    dur_s: float,
    gen: np.random.Generator,
    gain: float,
    attack_s: float = 0.16,
    decay_s: float = 1.6,
    room: float = 0.62,
    wet: float = 0.34,
) -> np.ndarray:
    """Render a warm C2 bench chime with its own self-contained reverb tail and clean fades."""
    dry = bench.warm_chime_voice(note_freq(note_name), dur_s, gen, decay_s=decay_s, attack_s=attack_s)
    padded = np.concatenate([dry, np.zeros(samples(1.8))])
    rev = to_mono(effects.reverb(padded, room=room, damping=0.72, wet=wet, dry=1.0 - 0.3 * wet, predelay=0.012))
    rev = filters.highpass(filters.lowpass(rev, 2800.0), 90.0)
    rev = envelope.fade_in(envelope.fade_out(rev, 0.25), 0.025)
    return (rev - float(np.mean(rev))) * gain


def _reverbed_acoustic_voice(
    kind: str,
    note_name: str,
    dur_s: float,
    gen: np.random.Generator,
    gain: float,
    decay_s: float = 1.4,
    room: float = 0.65,
    wet: float = 0.38,
) -> np.ndarray:
    """Render an acoustic kalimba / felt_piano / glass_chime / soft_bell note with self-contained reverb."""
    hz = note_freq(note_name)
    if kind == "kalimba":
        dry = instruments.kalimba(hz, dur_s, gen, decay=decay_s)
    elif kind == "glass":
        dry = instruments.glass_chime(hz, dur_s, decay=decay_s)
        dry = envelope.fade_in(dry, 0.18)
    elif kind == "bell":
        dry = instruments.soft_bell(hz, dur_s, decay=decay_s)
        dry = envelope.fade_in(dry, 0.22)
    else:
        dry = instruments.felt_piano(hz, dur_s, gen, decay=decay_s, brightness=0.36)
    padded = np.concatenate([dry, np.zeros(samples(1.8))])
    rev = to_mono(effects.reverb(padded, room=room, damping=0.74, wet=wet, dry=1.0 - 0.3 * wet, predelay=0.015))
    rev = filters.highpass(filters.lowpass(rev, 2900.0), 85.0)
    rev = envelope.fade_in(envelope.fade_out(rev, 0.25), 0.020)
    return (rev - float(np.mean(rev))) * gain


def ambience_earthlit_night(variant: int, gen: np.random.Generator) -> np.ndarray:
    """60 s seamless loop: lonely-but-peaceful Earthlit night."""
    n = samples(BED_DURATION_S)
    if variant == 0:
        # Variant A (C2 Radio bench): warm D2/A2/D3 receiver filament hum + gentle tape hiss breathing
        # + sparse D-pentatonic tube chimes and distant bench relay ticks under blue Earthlight.
        mix = _periodic_bench_drone(
            n,
            [("D2", 0.22), ("A2", 0.16), ("D3", 0.18), ("A3", 0.10), ("F#3", 0.07)],
            lfo_cycles=3,
            lfo_depth=0.18,
            drive=1.24,
            lp_hz=1350.0,
        )
        mix += _seamless_noise_bed(n, gen, low_hz=160.0, high_hz=2300.0, gain=0.018, lfo_cycles=4, lfo_depth=0.20)

        night_notes = (
            (2.4, "A4", 2.6, 0.16),
            (6.8, "D5", 3.0, 0.14),
            (13.0, "F#4", 2.8, 0.15),
            (18.5, "E4", 2.5, 0.13),
            (23.6, "A4", 3.0, 0.15),
            (29.0, "B4", 2.6, 0.13),
            (33.4, "F#4", 3.0, 0.15),
            (39.6, "D4", 3.2, 0.16),
            (45.2, "A4", 2.8, 0.15),
            (50.6, "F#4", 2.8, 0.14),
            (55.4, "D5", 3.0, 0.13),
        )
        for onset_s, note_name, dur_s, gain in night_notes:
            ev = _reverbed_chime(note_name, dur_s, gen, gain=gain, attack_s=0.18, decay_s=1.5)
            place(mix, ev, samples(onset_s), wrap=True)

        for t_rel in (11.2, 27.4, 43.6, 58.0):
            tick = bench.relay_armature(note_freq("A4"), 0.12, gen, decay_s=0.07, muted=True)
            tick = envelope.fade_in(envelope.fade_out(tick, 0.015), 0.002) * 0.055
            place(mix, tick, samples(t_rel), wrap=True)

        return mix - float(np.mean(mix))

    # Variant B (Bolder idea): acoustic reed-harmonium drone + bowed glass/felt-piano harmonics + hull cooling ticks.
    mix = _periodic_bench_drone(
        n,
        [("D2", 0.20), ("A2", 0.17), ("F#3", 0.11), ("D3", 0.17), ("B3", 0.06)],
        lfo_cycles=2,
        lfo_depth=0.22,
        drive=1.18,
        lp_hz=1250.0,
    )
    mix += _seamless_noise_bed(n, gen, low_hz=120.0, high_hz=1850.0, gain=0.015, lfo_cycles=3, lfo_depth=0.22, brown=True)

    bowed_notes = (
        (3.0, "glass", "D4", 3.0, 0.16),
        (8.2, "felt", "A4", 3.2, 0.15),
        (14.5, "glass", "B4", 2.8, 0.13),
        (19.0, "felt", "F#4", 3.2, 0.15),
        (25.5, "glass", "D5", 3.0, 0.14),
        (31.0, "felt", "A4", 3.0, 0.15),
        (36.8, "glass", "E4", 2.8, 0.14),
        (42.0, "felt", "F#4", 3.2, 0.15),
        (48.0, "glass", "B4", 2.8, 0.13),
        (53.5, "felt", "A4", 3.2, 0.15),
    )
    for onset_s, kind, note_name, dur_s, gain in bowed_notes:
        ev = _reverbed_acoustic_voice(kind, note_name, dur_s, gen, gain=gain, decay_s=1.4, wet=0.40)
        place(mix, ev, samples(onset_s), wrap=True)

    for t_tick in (6.0, 16.8, 28.5, 39.2, 51.0):
        det = bench.rotary_detent(note_freq("A4"), 0.09, gen, decay_s=0.05, muted=True)
        det = envelope.fade_in(envelope.fade_out(det, 0.012), 0.002) * 0.050
        place(mix, det, samples(t_tick), wrap=True)

    return mix - float(np.mean(mix))


def ambience_sunlit_day(variant: int, gen: np.random.Generator) -> np.ndarray:
    """60 s seamless loop: warm, unhurried sunlit lunar day."""
    n = samples(BED_DURATION_S)
    if variant == 0:
        # Variant A (C2 Radio bench): golden solar-bus hum (D2/A2/D3/F#3/A3) + warm tube-organ phrases
        # + gentle solar-tracker stepping relays.
        mix = _periodic_bench_drone(
            n,
            [("D2", 0.18), ("A2", 0.16), ("D3", 0.19), ("F#3", 0.13), ("A3", 0.11), ("E3", 0.07)],
            lfo_cycles=4,
            lfo_depth=0.15,
            drive=1.28,
            lp_hz=1550.0,
        )
        mix += _seamless_noise_bed(n, gen, low_hz=200.0, high_hz=2600.0, gain=0.018, lfo_cycles=5, lfo_depth=0.16)

        day_phrases = (
            (1.8, "D4", 2.4, 0.15),
            (3.2, "F#4", 2.6, 0.15),
            (5.0, "A4", 2.8, 0.16),
            (11.0, "E4", 2.4, 0.14),
            (12.8, "A4", 2.6, 0.15),
            (14.8, "B4", 2.8, 0.14),
            (21.0, "F#4", 2.6, 0.15),
            (23.0, "A4", 2.6, 0.15),
            (25.2, "D5", 3.0, 0.15),
            (31.5, "B4", 2.4, 0.14),
            (33.5, "A4", 2.6, 0.15),
            (35.8, "F#4", 2.8, 0.15),
            (42.0, "D4", 2.4, 0.15),
            (44.0, "E4", 2.4, 0.14),
            (46.2, "A4", 2.8, 0.16),
            (52.0, "F#4", 2.6, 0.15),
            (54.2, "D5", 2.8, 0.14),
            (56.5, "A4", 2.8, 0.15),
        )
        for onset_s, note_name, dur_s, gain in day_phrases:
            ev = _reverbed_chime(note_name, dur_s, gen, gain=gain, attack_s=0.14, decay_s=1.3, wet=0.32)
            place(mix, ev, samples(onset_s), wrap=True)

        for t_step in (8.5, 19.2, 29.8, 40.4, 50.8):
            latch = bench.felt_latch(note_freq("D4"), 0.08, gen, decay_s=0.05)
            latch = envelope.fade_in(envelope.fade_out(latch, 0.012), 0.002) * 0.065
            place(mix, latch, samples(t_step), wrap=True)

        return mix - float(np.mean(mix))

    # Variant B (Bolder idea): sunlit workshop kalimba & felt-piano motifs over a purring flywheel harmonic bed.
    mix = _periodic_bench_drone(
        n,
        [("D2", 0.17), ("A2", 0.16), ("D3", 0.18), ("A3", 0.13), ("B3", 0.08), ("F#4", 0.05)],
        lfo_cycles=5,
        lfo_depth=0.14,
        drive=1.20,
        lp_hz=1600.0,
    )
    mix += _seamless_noise_bed(n, gen, low_hz=220.0, high_hz=2700.0, gain=0.016, lfo_cycles=4, lfo_depth=0.16)

    pluck_events = (
        (2.0, "kalimba", "D4", 2.0, 0.17),
        (3.4, "kalimba", "A4", 2.2, 0.17),
        (4.8, "felt", "B4", 2.2, 0.15),
        (6.4, "felt", "F#4", 2.5, 0.16),
        (12.5, "kalimba", "E4", 2.0, 0.16),
        (14.0, "kalimba", "F#4", 2.2, 0.16),
        (15.6, "felt", "A4", 2.4, 0.17),
        (17.2, "felt", "D5", 2.6, 0.16),
        (23.5, "kalimba", "B4", 2.0, 0.15),
        (25.0, "kalimba", "A4", 2.2, 0.16),
        (26.6, "felt", "F#4", 2.4, 0.16),
        (28.4, "felt", "D4", 2.6, 0.17),
        (34.5, "kalimba", "A4", 2.0, 0.17),
        (36.0, "kalimba", "B4", 2.2, 0.15),
        (37.6, "felt", "D5", 2.4, 0.16),
        (39.4, "felt", "A4", 2.6, 0.16),
        (45.5, "kalimba", "F#4", 2.0, 0.16),
        (47.0, "kalimba", "E4", 2.2, 0.15),
        (48.6, "felt", "D4", 2.6, 0.17),
        (54.0, "kalimba", "A4", 2.2, 0.16),
        (55.8, "felt", "F#4", 2.4, 0.16),
        (57.5, "felt", "D5", 2.4, 0.15),
    )
    for onset_s, kind, note_name, dur_s, gain in pluck_events:
        ev = _reverbed_acoustic_voice(kind, note_name, dur_s, gen, gain=gain, decay_s=1.1, wet=0.34)
        place(mix, ev, samples(onset_s), wrap=True)

    return mix - float(np.mean(mix))


def ambience_eclipse(variant: int, gen: np.random.Generator) -> np.ndarray:
    """60 s seamless loop: hushed, deep lunar eclipse."""
    n = samples(BED_DURATION_S)
    if variant == 0:
        # Variant A (C2 Radio bench): deep sub-octave filament resonance + low-passed tape hush
        # + sparse crystalline high tube harmonics and quiet battery-bus relay changeovers.
        mix = _periodic_bench_drone(
            n,
            [("D2", 0.24), ("A2", 0.18), ("D3", 0.16), ("A3", 0.08), ("E3", 0.06)],
            lfo_cycles=2,
            lfo_depth=0.24,
            drive=1.20,
            lp_hz=1150.0,
        )
        mix += _seamless_noise_bed(n, gen, low_hz=90.0, high_hz=1450.0, gain=0.015, lfo_cycles=2, lfo_depth=0.25, brown=True)

        eclipse_notes = (
            (4.0, "A4", 3.2, 0.14),
            (10.5, "D5", 3.5, 0.13),
            (18.0, "F#4", 3.4, 0.14),
            (25.5, "E4", 3.2, 0.13),
            (32.5, "A5", 3.4, 0.11),
            (39.0, "D5", 3.4, 0.13),
            (46.5, "B4", 3.2, 0.12),
            (53.5, "A4", 3.4, 0.14),
        )
        for onset_s, note_name, dur_s, gain in eclipse_notes:
            ev = _reverbed_chime(note_name, dur_s, gen, gain=gain, attack_s=0.26, decay_s=1.7, room=0.68, wet=0.42)
            place(mix, ev, samples(onset_s), wrap=True)

        for t_rel in (15.0, 36.0, 57.2):
            rel = bench.relay_armature(note_freq("D4"), 0.12, gen, decay_s=0.07, muted=True)
            rel = envelope.fade_in(envelope.fade_out(rel, 0.015), 0.002) * 0.050
            place(mix, rel, samples(t_rel), wrap=True)

        return mix - float(np.mean(mix))

    # Variant B (Bolder idea): bowed singing-bowl / soft-bell & glass resonances suspended in the penumbra.
    mix = _periodic_bench_drone(
        n,
        [("D2", 0.22), ("A2", 0.18), ("D3", 0.17), ("F#3", 0.09), ("A3", 0.08)],
        lfo_cycles=2,
        lfo_depth=0.24,
        drive=1.16,
        lp_hz=1100.0,
    )
    mix += _seamless_noise_bed(n, gen, low_hz=85.0, high_hz=1350.0, gain=0.014, lfo_cycles=3, lfo_depth=0.24, brown=True)

    bowl_events = (
        (3.5, "bell", "D4", 3.4, 0.15),
        (11.0, "glass", "A4", 3.6, 0.14),
        (18.5, "bell", "F#4", 3.4, 0.14),
        (26.0, "glass", "B4", 3.2, 0.13),
        (33.5, "bell", "D5", 3.6, 0.13),
        (41.0, "glass", "A4", 3.4, 0.14),
        (48.5, "bell", "E4", 3.2, 0.13),
        (55.0, "glass", "F#4", 3.4, 0.14),
    )
    for onset_s, kind, note_name, dur_s, gain in bowl_events:
        ev = _reverbed_acoustic_voice(kind, note_name, dur_s, gen, gain=gain, decay_s=1.6, room=0.70, wet=0.44)
        place(mix, ev, samples(onset_s), wrap=True)

    return mix - float(np.mean(mix))


AMBIENCE_CUES = {
    "ambience_earthlit_night": ambience_earthlit_night,
    "ambience_sunlit_day": ambience_sunlit_day,
    "ambience_eclipse": ambience_eclipse,
}
