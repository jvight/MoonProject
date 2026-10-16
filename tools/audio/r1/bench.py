"""Shared C2 'Radio bench' signal chain and electro-mechanical physical synthesis primitives.

Every physical mechanism (relays, bakelite switches, rotary detents, speaker cones, felt latches,
paper slides, hydraulic actuators, worm-gear servos, steel frames, CRT deflection/degauss coils) is
built from filtered noise bursts, Karplus-Strong waveguides, and damped modal resonators tuned to
D major pentatonic (D E F# A B), then coloured through a warm vintage radio-bench chain (triode
saturation, paper-cone & bakelite cabinet formants, subtle tape hiss and capstan flutter).
"""
import math

import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, note_freq, pan, place, samples, to_mono
from synth.pluck import karplus_strong


def radio_bench_chain(
    x: np.ndarray,
    gen: np.random.Generator,
    hiss_level: float = 0.006,
    drive_db: float = 2.6,
    lp_hz: float = 2850.0,
    hp_hz: float = 85.0,
    room: float = 0.28,
    wet: float = 0.12,
    flutter_cents: float = 0.8,
) -> np.ndarray:
    """Applies C2 'Radio bench' colour: asymmetric valve warmth, paper speaker cone & bakelite baffle
    resonances, envelope-gated tape hiss, gentle capstan flutter, and close bench-room reflection."""
    mono = to_mono(x)
    n = mono.shape[0]
    dur = n / SAMPLE_RATE

    # 1. Envelope-gated tape/receiver hiss + faint D2/A2 filament warmth.
    hiss_raw = noise.pink(n, gen)
    hiss_band = filters.bandpass(hiss_raw, 920.0, 1.3) + 0.45 * filters.bandpass(hiss_raw, 440.0, 1.5)
    hiss_std = max(float(np.std(hiss_band)), 1e-9)
    flutter_lfo = envelope.lfo(n, 3.8, 0.15, 0.85, phase=float(gen.uniform(0.0, 1.0)))
    filament = 0.25 * osc.sine(n, note_freq("D2")) + 0.12 * osc.sine(n, note_freq("A2"))
    att = min(0.018, dur * 0.18)
    rel_start = max(att + 0.004, dur * 0.78)
    bed_env = envelope.segments(
        n,
        [(0.0, 0.0), (att, 1.0), (rel_start, 0.65), (dur, 0.0)],
        shape="smooth",
    )
    bed = (hiss_band / hiss_std * hiss_level * flutter_lfo + 0.003 * filament) * bed_env

    # 2. Asymmetric triode valve saturation (warm 2nd-harmonic roundness).
    biased = mono + bed + 0.028 * bed_env
    drive = 10.0 ** (drive_db / 20.0)
    saturated = filters.dc_block(effects.saturate(biased, drive=drive), 20.0)

    # 3. Paper speaker cone & bakelite cabinet formants (tuned to D3/A3/D4/F#5 neighbourhood).
    cab = (
        0.64 * saturated
        + 0.22 * filters.bandpass(saturated, note_freq("D3"), 2.2)
        + 0.24 * filters.bandpass(saturated, note_freq("A3"), 2.4)
        + 0.20 * filters.bandpass(saturated, note_freq("D5") * 2.0, 2.2)
    )

    # 4. Subtle tape capstan flutter (gentle enough to keep D-pentatonic pitches within +/- 5 cents).
    if n > samples(0.45) and flutter_cents > 0.0:
        cab = effects.wow_flutter(
            cab,
            wow_cents=min(flutter_cents, 0.30),
            wow_rate=1.6,
            flutter_cents=min(flutter_cents * 0.6, 0.20),
            flutter_rate=5.8,
        )

    # 5. Warm passband + close bench room.
    band = filters.highpass(filters.lowpass(filters.lowpass(cab, lp_hz), lp_hz), hp_hz)
    if wet <= 0.0:
        return band
    return to_mono(effects.reverb(band, room=room, damping=0.72, wet=wet, dry=1.0, predelay=0.005))


def relay_armature(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    double: bool = False,
    decay_s: float = 0.12,
    muted: bool = False,
) -> np.ndarray:
    """Enclosed electromagnetic relay: yoke pull-in thud + bakelite/steel can clack + phosphor-bronze
    contact spring tuned to ``freq``."""
    n = samples(duration)
    out = np.zeros(n)
    events = [(0.002, 1.0, 1.0)]
    if double:
        events.append((0.021, 0.62, 0.91))

    for onset_s, gain, scale in events:
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0012, 0.010)
        yoke = filters.bandpass(burst, 240.0 * scale, 2.6) * envelope.ar(rem, 0.0015, 0.024)
        can = filters.bandpass(burst, 740.0 * scale, 3.4) * envelope.ar(rem, 0.0012, 0.017)
        leaf = filters.bandpass(burst, 1480.0 * scale, 3.8) * envelope.ar(rem, 0.0010, 0.012)
        t60 = decay_s if not muted else min(decay_s, 0.065)
        wg = karplus_strong(
            rem,
            max(80.0, min(freq, 2400.0)),
            gen,
            decay=0.991 if not muted else 0.974,
            brightness=0.32 if not muted else 0.18,
            pick_softness=1800.0,
        ) * envelope.ar(rem, 0.002, t60)
        spring = (
            0.85 * osc.sine(rem, freq)
            + 0.26 * osc.sine(rem, freq * 2.0, phase=0.21)
        ) * envelope.ar(rem, 0.0025, t60)
        seg = gain * (0.55 * yoke + 0.80 * can + 0.55 * leaf + 0.48 * wg + 0.85 * spring)
        place(out, envelope.fade_out(seg, min(0.015, rem / SAMPLE_RATE * 0.25)), start)

    cutoff = 2800.0 if not muted else 1900.0
    return filters.lowpass(filters.highpass(out, 80.0), cutoff)


def bakelite_toggle(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    direction: str = "up",
    decay_s: float = 0.16,
    muted: bool = False,
) -> np.ndarray:
    """Spring-loaded moulded-bakelite toggle switch throw: lever pre-travel friction + hollow phenolic
    cam snap + contact bounce + tuned tank-coil ring at ``freq``."""
    n = samples(duration)
    out = np.zeros(n)

    travel_n = min(n, samples(0.012))
    f_start, f_end = (480.0, 1040.0) if direction == "up" else (1040.0, 480.0)
    rub = filters.swept(noise.pink(travel_n, gen), "bandpass", osc.glide(travel_n, f_start, f_end), q=2.4)
    rub *= envelope.segments(travel_n, [(0.0, 0.0), (0.004, 1.0), (0.012, 0.0)], shape="smooth")
    place(out, 0.30 * rub, 0)

    for onset_s, gain, scale in ((0.006, 1.0, 1.0), (0.022, 0.55, 0.90)):
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0014, 0.011)
        b1 = filters.bandpass(burst, 420.0 * scale, 3.0) * envelope.ar(rem, 0.0015, 0.025)
        b2 = filters.bandpass(burst, 960.0 * scale, 3.5) * envelope.ar(rem, 0.0012, 0.018)
        b3 = filters.bandpass(burst, 1680.0 * scale, 3.4) * envelope.ar(rem, 0.0010, 0.012)
        chassis = (
            0.72 * osc.sine(rem, note_freq("D3")) + 0.35 * osc.sine(rem, note_freq("A2"))
        ) * envelope.ar(rem, 0.002, 0.032)
        place(out, gain * (0.75 * b1 + 0.80 * b2 + 0.50 * b3 + 0.42 * chassis), start)

    ring_start = samples(0.007)
    ring_rem = n - ring_start
    if ring_rem > 32:
        t60 = decay_s if not muted else min(decay_s, 0.085)
        wg = karplus_strong(
            ring_rem,
            max(80.0, min(freq, 2400.0)),
            gen,
            decay=0.992 if not muted else 0.975,
            brightness=0.33 if not muted else 0.18,
            pick_softness=1850.0,
        ) * envelope.ar(ring_rem, 0.0025, t60)
        coil = (
            0.86 * osc.sine(ring_rem, freq)
            + 0.28 * osc.sine(ring_rem, freq * 2.0, phase=0.17)
        ) * envelope.ar(ring_rem, 0.003, t60)
        seg = 0.46 * wg + 0.86 * coil
        place(out, envelope.fade_out(seg, min(0.018, ring_rem / SAMPLE_RATE * 0.25)), ring_start)

    cutoff = 2750.0 if not muted else 1850.0
    return filters.lowpass(filters.highpass(out, 80.0), cutoff)


def rotary_detent(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    decay_s: float = 0.12,
    muted: bool = False,
) -> np.ndarray:
    """Weighted knurled rotary selector knob stepping across a brass leaf-spring detent and exciting
    a variable-capacitor air-vane ring at ``freq``."""
    n = samples(duration)
    out = np.zeros(n)

    rub_n = min(n, samples(0.009))
    rub = filters.bandpass(noise.pink(rub_n, gen), 820.0, 1.5)
    rub *= envelope.segments(rub_n, [(0.0, 0.0), (0.003, 1.0), (0.009, 0.0)], shape="smooth")
    place(out, 0.26 * rub, 0)

    start = samples(0.003)
    rem = n - start
    if rem > 16:
        imp = noise.pink(rem, gen) * envelope.ar(rem, 0.0012, 0.010)
        detent_modes = (
            0.75 * filters.bandpass(imp, 640.0, 3.6) * envelope.ar(rem, 0.0012, 0.020)
            + 0.80 * filters.bandpass(imp, 1280.0, 4.0) * envelope.ar(rem, 0.0010, 0.015)
            + 0.48 * filters.bandpass(imp, 1980.0, 3.8) * envelope.ar(rem, 0.0009, 0.010)
        )
        t60 = decay_s if not muted else min(decay_s, 0.070)
        vane_wg = karplus_strong(
            rem,
            max(80.0, min(freq, 2400.0)),
            gen,
            decay=0.992 if not muted else 0.975,
            brightness=0.35 if not muted else 0.20,
            pick_softness=2000.0,
        ) * envelope.ar(rem, 0.002, t60)
        vane_core = (
            0.86 * osc.sine(rem, freq)
            + 0.28 * osc.sine(rem, freq * 2.0, phase=0.14)
        ) * envelope.ar(rem, 0.0025, t60)
        thud = (
            0.70 * osc.sine(rem, note_freq("D3")) + 0.35 * osc.sine(rem, note_freq("A2"))
        ) * envelope.ar(rem, 0.002, 0.025)
        seg = 0.80 * detent_modes + 0.48 * vane_wg + 0.88 * vane_core + 0.32 * thud
        place(out, envelope.fade_out(seg, min(0.016, rem / SAMPLE_RATE * 0.25)), start)

    cutoff = 2850.0 if not muted else 1950.0
    return filters.lowpass(filters.highpass(out, 85.0), cutoff)


def speaker_cone_thump(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    decay_s: float = 0.20,
    sag_semitones: float = 0.0,
    cone_weight: float = 0.65,
) -> np.ndarray:
    """Paper speaker cone flexing under a DC step / capacitor discharge, coupled with a voice-coil
    inductive ring tuned to ``freq``."""
    n = samples(duration)
    t = np.arange(n) / SAMPLE_RATE

    cone_excursion = (
        0.72 * osc.sine(n, note_freq("D3"))
        + 0.36 * osc.sine(n, note_freq("A2"))
    ) * envelope.ar(n, 0.003, 0.052)
    rebound = osc.sine(n, note_freq("A3"), phase=0.25) * envelope.ar(n, 0.006, 0.042)

    paper_raw = noise.pink(n, gen)
    paper_puff = (
        0.65 * filters.bandpass(paper_raw, 520.0, 2.0)
        + 0.40 * filters.bandpass(paper_raw, 1120.0, 2.4)
    ) * envelope.ar(n, 0.0025, 0.022)

    f_curve = np.full(n, freq, dtype=np.float64)
    if abs(sag_semitones) > 1e-4:
        u = np.clip((t - duration * 0.45) / max(duration * 0.55, 0.02), 0.0, 1.0) ** 1.6
        f_curve *= 2.0 ** (np.clip(sag_semitones, -0.08, 0.08) * u / 12.0)

    coil_ring = (
        0.88 * osc.sine(n, f_curve)
        + 0.28 * osc.sine(n, f_curve * 2.0, phase=0.15)
    ) * envelope.ar(n, 0.0045, decay_s)

    mix = cone_weight * cone_excursion + 0.24 * rebound + 0.32 * paper_puff + 0.94 * coil_ring
    return envelope.fade_out(filters.lowpass(filters.highpass(mix, 70.0), 2800.0), min(0.025, duration * 0.22))


def felt_latch(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    decay_s: float = 0.07,
    wood_body: float = 0.55,
) -> np.ndarray:
    """Wool-felt cushioned mechanical latch / keybed tap with a warm maple/bakelite resonance at ``freq``."""
    n = samples(duration)
    brush_n = min(n, samples(0.007))
    brush = filters.lowpass(filters.bandpass(noise.pink(brush_n, gen), 950.0, 1.2), 2100.0)
    brush *= envelope.ar(brush_n, 0.001, 0.005)

    body = (
        0.90 * osc.sine(n, freq)
        + 0.26 * osc.sine(n, freq * 2.0, phase=0.12)
    ) * envelope.ar(n, 0.0018, decay_s)

    thud = (
        0.72 * osc.sine(n, note_freq("D3")) + 0.32 * osc.sine(n, note_freq("A2"))
    ) * envelope.ar(n, 0.0015, 0.022)
    out = wood_body * body + 0.28 * thud
    out[:brush_n] += 0.34 * brush
    return envelope.fade_out(filters.lowpass(filters.highpass(out, 75.0), 2500.0), min(0.015, duration * 0.25))


def paper_slide(
    duration: float,
    gen: np.random.Generator,
    f_start: float = 680.0,
    f_end: float = 1320.0,
    tone_freq: float | None = None,
) -> np.ndarray:
    """Cotton paper card sliding across a felt platen or brass guide slot."""
    n = samples(duration)
    raw = noise.pink(n, gen)
    grain_mod = 0.72 + 0.28 * np.sin(2.0 * math.pi * 28.0 * np.arange(n) / SAMPLE_RATE)
    swept_band = filters.swept(raw * grain_mod, "bandpass", osc.glide(n, f_start, f_end), q=2.0)
    velvet = filters.lowpass(filters.highpass(raw, 280.0), 1800.0)
    env = envelope.segments(
        n,
        [(0.0, 0.0), (duration * 0.22, 1.0), (duration * 0.76, 0.72), (duration, 0.0)],
        shape="smooth",
    )
    out = (0.75 * swept_band + 0.25 * velvet) * env
    if tone_freq is not None:
        guide_ring = (
            0.84 * osc.sine(n, tone_freq) + 0.22 * osc.sine(n, tone_freq * 2.0)
        ) * env
        out = 0.50 * out + 0.50 * guide_ring
    return filters.lowpass(filters.highpass(out, 110.0), 2800.0)


def hydraulic_cylinder(
    duration: float,
    gen: np.random.Generator,
    f_start: float,
    f_end: float,
    fluid_start_hz: float = 480.0,
    fluid_end_hz: float = 920.0,
) -> np.ndarray:
    """Pressurized hydraulic actuator stroke: fluid flow through a brass check valve + piston seal rub
    + damped housing hum morphing ``f_start -> f_end``."""
    n = samples(duration)
    raw = noise.pink(n, gen)
    fluid = filters.swept(raw, "bandpass", osc.glide(n, fluid_start_hz, fluid_end_hz), q=2.4)
    fluid /= max(float(np.std(fluid)), 1e-9)

    morph = envelope.curve(n, "smooth")
    voice_start = 0.84 * osc.sine(n, f_start) + 0.30 * osc.sine(n, f_start * 2.0, phase=0.19)
    voice_end = 0.84 * osc.sine(n, f_end) + 0.30 * osc.sine(n, f_end * 2.0, phase=0.31)
    housing = (1.0 - morph) * voice_start + morph * voice_end

    pulse = envelope.lfo(n, 16.0, 0.14, 0.86)
    env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.08, duration * 0.15), 1.0), (duration * 0.82, 0.80), (duration, 0.0)],
        shape="smooth",
    )
    out = (0.14 * fluid + 0.72 * housing * pulse) * env
    return filters.lowpass(filters.highpass(out, 65.0), 2400.0)


def servo_worm_gear(
    duration: float,
    gen: np.random.Generator,
    f_start: float,
    f_end: float,
    tooth_rate_hz: float = 22.0,
) -> np.ndarray:
    """Low-RPM brushed DC servo and worm-gear train heard through a cast-aluminium housing."""
    n = samples(duration)
    morph = envelope.curve(n, "smooth")
    w_start = 0.86 * osc.sine(n, f_start) + 0.28 * osc.sine(n, f_start * 2.0, phase=0.13)
    w_end = 0.86 * osc.sine(n, f_end) + 0.28 * osc.sine(n, f_end * 2.0, phase=0.27)
    winding = (1.0 - morph) * w_start + morph * w_end

    tooth_mod = envelope.lfo(n, tooth_rate_hz, 0.18, 0.82)
    brush = filters.swept(
        noise.pink(n, gen), "bandpass", osc.glide(n, max(250.0, f_start * 2.5), max(250.0, f_end * 2.5)), q=2.0
    )
    brush /= max(float(np.std(brush)), 1e-9)
    env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.06, duration * 0.14), 1.0), (duration * 0.84, 0.78), (duration, 0.0)],
        shape="smooth",
    )
    out = (0.76 * winding * tooth_mod + 0.08 * brush * tooth_mod) * env
    return filters.lowpass(filters.highpass(out, 70.0), 2200.0)


def steel_frame_impact(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    regolith: float = 0.35,
    decay_s: float = 0.42,
) -> np.ndarray:
    """Heavy structural steel / cast-aluminium frame drop onto lunar regolith with bolted truss modal ring."""
    n = samples(duration)
    thump = (
        0.75 * osc.sine(n, note_freq("D2"))
        + 0.45 * osc.sine(n, note_freq("A2"))
    ) * envelope.ar(n, 0.003, 0.22)
    sub_body = filters.lowpass(noise.brown(n, gen), 140.0) * envelope.ar(n, 0.004, 0.18)

    wg = karplus_strong(
        n,
        max(70.0, min(freq, 1800.0)),
        gen,
        decay=0.994,
        brightness=0.30,
        pick_softness=1500.0,
    ) * envelope.ar(n, 0.003, decay_s)
    truss_modes = (
        0.88 * osc.sine(n, freq) * envelope.ar(n, 0.003, decay_s)
        + 0.32 * osc.sine(n, freq * 2.0, phase=0.22) * envelope.ar(n, 0.0025, decay_s * 0.65)
    )
    crunch_n = min(n, samples(0.28))
    crunch = filters.lowpass(filters.bandpass(noise.pink(crunch_n, gen), 620.0, 1.1), 2200.0)
    crunch *= envelope.ar(crunch_n, 0.005, 0.16)

    out = 0.62 * thump + 0.28 * sub_body + 0.44 * wg + 0.82 * truss_modes
    out[:crunch_n] += regolith * crunch
    return envelope.fade_out(filters.lowpass(filters.highpass(out, 42.0), 2800.0), min(0.04, duration * 0.2))


def warm_chime_voice(
    freq: float,
    duration: float,
    gen: np.random.Generator,
    decay_s: float = 0.85,
    attack_s: float = 0.004,
) -> np.ndarray:
    """Warm electro-mechanical chime (plucked bronze reed + felt-struck bell resonance on ``freq``)."""
    n = samples(duration)
    wg = karplus_strong(
        n,
        max(80.0, min(freq, 2400.0)),
        gen,
        decay=0.995,
        brightness=0.34,
        pick_softness=1900.0,
    ) * envelope.ar(n, attack_s, decay_s * 0.85)
    bell = (
        0.86 * osc.sine(n, freq) * envelope.ar(n, attack_s, decay_s)
        + 0.28 * osc.sine(n, freq * 2.0, phase=0.16) * envelope.ar(n, attack_s, decay_s * 0.55)
        + 0.09 * osc.sine(n, freq * 3.0, phase=0.39) * envelope.ar(n, attack_s, decay_s * 0.32)
    )
    felt_n = min(n, samples(0.010))
    felt = filters.lowpass(noise.pink(felt_n, gen), 900.0) * envelope.ar(felt_n, 0.001, 0.008)
    out = 0.42 * wg + 0.78 * bell
    out[:felt_n] += 0.08 * felt
    return envelope.fade_out(filters.lowpass(out, 3200.0), min(0.04, duration * 0.18))
