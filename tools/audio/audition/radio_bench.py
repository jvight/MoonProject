"""Refinement C2 ("Radio bench"): C's vintage radio colour on top of tactile physical hardware sources for UI,
and 07's chirps as a rounder, formant-voiced little radio creature (still wordless).

Refines round-1 C ("Old Radio") per owner feedback:
- UI cues are grounded in physical bench mechanisms heard through the radio receiver chain:
  * ``knob_detent``: rotary selector shaft friction + spring-ball indexing click into a notched brass plate + tuned
    variable-capacitor plate ring.
  * ``bakelite_switch``: spring-loaded toggle pre-travel rub + hollow moulded-bakelite snap + contact bounce + busbar
    inductance ring.
  * ``relay_clack``: enclosed electromagnetic armature pull-in and yoke seating clack + phosphor-bronze leaf ring.
  * ``speaker_cone_thump``: direct DC pulse / capacitor discharge flexing the paper speaker cone and spider suspension.
- 07's chirps use a rounded glottal-like triode source passed through a dual swept vocal-formant filter bank (F1 throat
  roundness + F2 mouth cooing contour) so 07 sounds like a warm, wordless little creature inside the radio.
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono
from synth.pluck import karplus_strong


def _radio_bench_chain(x: np.ndarray, gen, hiss_level: float = 0.011, drive_db: float = 3.2,
                       lp_hz: float = 2950.0, hp_hz: float = 135.0,
                       room: float = 0.28, wet: float = 0.13) -> np.ndarray:
    """Applies C's vintage triode saturation, bakelite & paper-cone speaker cabinet colour, subtle tape flutter,
    and close bench-room reflection."""
    n = x.shape[0]
    dur = n / SAMPLE_RATE

    # 1. Envelope-gated receiver floor + subtle D2/D3 transformer hum.
    hiss_raw = noise.pink(n, gen)
    hiss_band = filters.bandpass(hiss_raw, 980.0, 1.2) + 0.45 * filters.bandpass(hiss_raw, 480.0, 1.4)
    hiss_std = max(float(np.std(hiss_band)), 1e-9)
    flutter_lfo = envelope.lfo(n, 4.2, 0.18, 0.82, phase=float(gen.uniform(0.0, 1.0)))
    filament = 0.25 * osc.sine(n, note_freq("D2")) + 0.14 * osc.sine(n, note_freq("D3"))
    bed_env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.020, dur * 0.2), 1.0),
         (max(min(0.020, dur * 0.2) + 0.005, dur * 0.78), 0.70),
         (dur, 0.0)],
        shape="smooth",
    )
    bed = (hiss_band / hiss_std * hiss_level * flutter_lfo + 0.005 * filament) * bed_env

    # 2. Asymmetric triode saturation for warm even harmonics.
    biased = x + bed + 0.036 * bed_env
    drive = 10.0 ** (drive_db / 20.0)
    saturated = filters.dc_block(effects.saturate(biased, drive=drive), 22.0)

    # 3. Bench speaker & bakelite cabinet resonances (240 Hz cone chest, 440 Hz box, 1280 Hz phenolic baffle).
    cab = (
        0.58 * saturated
        + 0.20 * filters.bandpass(saturated, 240.0, 2.0)
        + 0.26 * filters.bandpass(saturated, 440.0, 2.2)
        + 0.32 * filters.bandpass(saturated, 1280.0, 2.2)
    )

    # 4. Subtle flutter (high rate only so short UI detents stay pitch-exact).
    if n > 64:
        cab = effects.wow_flutter(cab, wow_cents=1.2, wow_rate=1.8,
                                  flutter_cents=0.9, flutter_rate=6.2)

    # 5. Receiver passband + close radio-bench room reflection.
    band = filters.highpass(filters.lowpass(filters.lowpass(cab, lp_hz), lp_hz), hp_hz)
    return to_mono(effects.reverb(band, room=room, damping=0.72, wet=wet, dry=1.0, predelay=0.006))


# ----------------------------------------------------------------------------------- Physical Bench Mechanisms

def knob_detent(freq: float, duration: float, gen, decay_s: float = 0.14,
                muted: bool = False) -> np.ndarray:
    """Weighted knurled rotary selector knob stepping across a leaf-spring detent and exciting a variable-capacitor
    plate ring tuned to ``freq``."""
    n = samples(duration)
    out = np.zeros(n)

    # 1. Pre-rotation shaft bushing rub (first 9 ms).
    rub_n = min(n, samples(0.010))
    rub = filters.bandpass(noise.pink(rub_n, gen), 880.0, 1.6)
    rub *= envelope.segments(rub_n, [(0.0, 0.0), (0.003, 1.0), (0.010, 0.0)], shape="smooth")
    place(out, 0.35 * rub, 0)

    # 2. Detent ball seating into notched brass index plate (t = 3 ms).
    start = samples(0.003)
    rem = n - start
    if rem > 16:
        imp = noise.pink(rem, gen) * envelope.ar(rem, 0.0015, 0.011)
        # Brass detent plate & knurled phenolic knob shell modes.
        detent_modes = (
            0.95 * filters.bandpass(imp, 680.0, 3.8) * envelope.ar(rem, 0.0014, 0.022)
            + 1.10 * filters.bandpass(imp, 1340.0, 4.2) * envelope.ar(rem, 0.0012, 0.016)
            + 0.75 * filters.bandpass(imp, 2120.0, 4.0) * envelope.ar(rem, 0.0010, 0.011)
        )
        # Variable-capacitor air-vane waveguide + modal ring tuned to freq.
        vane_wg = karplus_strong(
            rem, freq, gen,
            decay=0.992 if not muted else 0.976,
            brightness=0.38 if not muted else 0.22,
            pick_softness=2100.0,
        ) * envelope.ar(rem, 0.0022, decay_s if not muted else min(decay_s, 0.08))
        vane_core = (
            0.78 * osc.sine(rem, freq)
            + 0.34 * osc.sine(rem, freq * 2.0, phase=0.14)
            + 0.14 * osc.sine(rem, freq * 3.0, phase=0.38)
        ) * envelope.ar(rem, 0.0028, decay_s)
        panel_thud = osc.sine(rem, osc.glide(rem, 185.0, 132.0, time_constant=0.008)) * envelope.ar(rem, 0.002, 0.028)
        seg = 1.15 * detent_modes + 0.65 * vane_wg + 0.72 * vane_core + 0.40 * panel_thud
        place(out, envelope.fade_out(seg, min(0.018, rem / SAMPLE_RATE * 0.25)), start)

    return filters.lowpass(filters.highpass(out, 95.0), 2950.0 if not muted else 2050.0)


def bakelite_switch(freq: float, duration: float, gen, direction: str = "up",
                    decay_s: float = 0.18, muted: bool = False) -> np.ndarray:
    """Heavy spring-loaded moulded-bakelite toggle switch throw: spring travel + hollow phenolic snap + contact bounce
    + tuned busbar/coil ring."""
    n = samples(duration)
    out = np.zeros(n)

    # 1. Spring lever pre-travel friction (pitch-swept 13 ms rub).
    travel_n = min(n, samples(0.013))
    f_start, f_end = (520.0, 1120.0) if direction == "up" else (1120.0, 520.0)
    rub = filters.swept(noise.pink(travel_n, gen), "bandpass", osc.glide(travel_n, f_start, f_end), q=2.6)
    rub *= envelope.segments(travel_n, [(0.0, 0.0), (0.004, 1.0), (0.013, 0.0)], shape="smooth")
    place(out, 0.38 * rub, 0)

    # 2. Over-center cam snap (t = 7 ms) + contact bounce (t = 23 ms).
    snaps = [(0.007, 1.0, 1.0), (0.023, 0.60, 0.90)]
    for onset_s, gain, scale in snaps:
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0015, 0.012)
        # Hollow moulded-bakelite case cavity modes (440 Hz, 1020 Hz, 1820 Hz).
        b1 = filters.bandpass(burst, 440.0 * scale, 3.2) * envelope.ar(rem, 0.0016, 0.026)
        b2 = filters.bandpass(burst, 1020.0 * scale, 3.8) * envelope.ar(rem, 0.0014, 0.019)
        b3 = filters.bandpass(burst, 1820.0 * scale, 3.6) * envelope.ar(rem, 0.0012, 0.013)
        chassis = osc.sine(rem, osc.glide(rem, 195.0, 130.0, time_constant=0.009)) * envelope.ar(rem, 0.002, 0.034)
        place(out, gain * (0.95 * b1 + 1.05 * b2 + 0.72 * b3 + 0.48 * chassis), start)

    # 3. Tuned busbar / tank-coil ring excited by the contact closure.
    ring_start = samples(0.008)
    ring_rem = n - ring_start
    if ring_rem > 32:
        t60 = decay_s if not muted else min(decay_s, 0.090)
        wg = karplus_strong(
            ring_rem, freq, gen,
            decay=0.993 if not muted else 0.975,
            brightness=0.35 if not muted else 0.20,
            pick_softness=1900.0,
        ) * envelope.ar(ring_rem, 0.0028, t60)
        inductor = (
            0.78 * osc.sine(ring_rem, freq)
            + 0.32 * osc.sine(ring_rem, freq * 2.0, phase=0.17)
            + 0.12 * osc.sine(ring_rem, freq * 3.0, phase=0.41)
        ) * envelope.ar(ring_rem, 0.0035, t60)
        seg = 0.58 * wg + 0.76 * inductor
        place(out, envelope.fade_out(seg, min(0.020, ring_rem / SAMPLE_RATE * 0.25)), ring_start)

    return filters.lowpass(filters.highpass(out, 95.0), 2850.0 if not muted else 1950.0)


def relay_clack(freq: float, duration: float, gen, double: bool = False,
                decay_s: float = 0.13, muted: bool = False) -> np.ndarray:
    """Enclosed electromagnetic armature relay: yoke pull-in thud + metal can clack + phosphor-bronze leaf ring."""
    n = samples(duration)
    out = np.zeros(n)

    events = [(0.002, 1.0, 1.0)]
    if double:
        events.append((0.019, 0.66, 0.89))

    for onset_s, gain, scale in events:
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0015, 0.011)
        # Relay yoke thud (260 Hz), metal can clack (780 Hz), and bronze contact leaf (1560 Hz).
        yoke = filters.bandpass(burst, 260.0 * scale, 2.8) * envelope.ar(rem, 0.0018, 0.026)
        can = filters.bandpass(burst, 780.0 * scale, 3.8) * envelope.ar(rem, 0.0015, 0.019)
        leaf = filters.bandpass(burst, 1560.0 * scale, 4.2) * envelope.ar(rem, 0.0012, 0.014)
        # Tuned contact-spring ring at freq.
        t60 = decay_s if not muted else min(decay_s, 0.070)
        spring = (
            0.78 * osc.sine(rem, freq * scale)
            + 0.30 * osc.sine(rem, freq * 2.0 * scale, phase=0.22)
            + 0.10 * osc.sine(rem, freq * 3.0 * scale, phase=0.45)
        ) * envelope.ar(rem, 0.0028, t60)
        seg = gain * (0.70 * yoke + 1.05 * can + 0.82 * leaf + 0.68 * spring)
        place(out, envelope.fade_out(seg, min(0.016, rem / SAMPLE_RATE * 0.25)), start)

    return filters.lowpass(filters.highpass(out, 95.0), 2850.0 if not muted else 1950.0)


def speaker_cone_thump(freq: float, duration: float, gen, decay_s: float = 0.20,
                       sag_semitones: float = 0.0) -> np.ndarray:
    """Paper speaker cone flexing under a DC step / capacitor discharge, coupled with a voice-coil inductive ring
    tuned to ``freq``."""
    n = samples(duration)
    t = np.arange(n) / SAMPLE_RATE

    # 1. Low mass-spring paper-cone excursion (180 -> 126 Hz) + spider suspension rebound (245 Hz).
    cone_f = osc.glide(n, 180.0, 126.0, time_constant=0.014)
    cone_excursion = osc.sine(n, cone_f) * envelope.ar(n, 0.003, 0.052)
    rebound = osc.sine(n, 245.0, phase=0.25) * envelope.ar(n, 0.006, 0.040)

    # 2. Paper cone surround flex & voice-coil rub texture (540 Hz & 1180 Hz filtered pink-noise puff).
    paper_raw = noise.pink(n, gen)
    paper_puff = (
        0.70 * filters.bandpass(paper_raw, 540.0, 2.2)
        + 0.45 * filters.bandpass(paper_raw, 1180.0, 2.6)
    ) * envelope.ar(n, 0.0025, 0.022)

    # 3. Tuned voice-coil & output-transformer tank ring at exact freq.
    f_curve = np.full(n, freq, dtype=np.float64)
    if abs(sag_semitones) > 1e-4:
        u = np.clip((t - duration * 0.30) / max(duration * 0.70, 0.02), 0.0, 1.0) ** 1.35
        f_curve *= 2.0 ** (sag_semitones * u / 12.0)

    coil_ring = (
        0.78 * osc.sine(n, f_curve)
        + 0.34 * osc.sine(n, f_curve * 2.0, phase=0.15)
        + 0.14 * osc.sine(n, f_curve * 3.0, phase=0.39)
    ) * envelope.ar(n, 0.005, decay_s)

    mix = 0.68 * cone_excursion + 0.28 * rebound + 0.42 * paper_puff + 0.82 * coil_ring
    return envelope.fade_out(filters.lowpass(filters.highpass(mix, 95.0), 2850.0), min(0.025, duration * 0.22))


# ----------------------------------------------------------------------------------- 07 Vocal-Formant Valve Voice

def vocal_valve_chirp_phrase(syllables: tuple, gen, warble_cents: float = 9.5,
                             sag_filter: bool = False) -> np.ndarray:
    """07's voice in C2: C's warm valve oscillator made rounder and more vocal (formant-like, a wordless little
    creature cooing through a two-formant F1/F2 vocal filter bank, framed by physical PTT relay clacks).

    Each syllable tuple is ``(onset_s, note, duration_s, bend_semitones, gain, f1_pair, f2_pair)`` where
    ``f1_pair = (f1_start_hz, f1_peak_hz)`` and ``f2_pair = (f2_start_hz, f2_peak_hz)`` shape the wordless vowel
    contour (e.g. rounded 'oo/mm' -> open 'oh/ah' -> 'oo').
    """
    last_onset, _, last_dur, _, _, _, _ = syllables[-1]
    total_s = last_onset + last_dur + 0.24
    n = samples(total_s)

    freq_track = np.full(n, note_freq(syllables[0][1]), dtype=np.float64)
    f1_track = np.full(n, float(syllables[0][5][0]), dtype=np.float64)
    f2_track = np.full(n, float(syllables[0][6][0]), dtype=np.float64)
    gate = np.zeros(n, dtype=np.float64)
    prev_hz = note_freq(syllables[0][1])

    for onset, note, length, bend, gain, (f1_a, f1_b), (f2_a, f2_b) in syllables:
        start = samples(onset)
        count = min(samples(length + 0.055), n - start)
        if count <= 12:
            continue
        hz = note_freq(note)
        glide_n = min(count, samples(0.048))
        seg = np.full(count, hz, dtype=np.float64)
        seg[:glide_n] = osc.glide(glide_n, prev_hz, hz, time_constant=0.014)
        if abs(bend) > 1e-4:
            tail = min(count, samples(min(length, 0.14)))
            seg[-tail:] *= 2.0 ** (np.linspace(0.0, bend, tail) / 12.0)
        freq_track[start:start + count] = seg
        freq_track[start + count:] = seg[-1]
        prev_hz = float(seg[-1])

        # Smooth vowel-formant contour across the syllable (opens from f_a to f_b and rounds back gently).
        dur_seg = count / SAMPLE_RATE
        f1_env = envelope.segments(
            count,
            [(0.0, f1_a), (dur_seg * 0.42, f1_b), (dur_seg, f1_a * 0.65 + f1_b * 0.35)],
            shape="smooth",
        )
        f2_env = envelope.segments(
            count,
            [(0.0, f2_a), (dur_seg * 0.45, f2_b), (dur_seg, f2_a * 0.55 + f2_b * 0.45)],
            shape="smooth",
        )
        f1_track[start:start + count] = f1_env
        f1_track[start + count:] = f1_env[-1]
        f2_track[start:start + count] = f2_env
        f2_track[start + count:] = f2_env[-1]

        # Rounded creature-breath syllable envelope.
        att_s = min(0.018, length * 0.26)
        hold_s = max(att_s + 0.005, length * 0.76)
        s_env = envelope.segments(
            count,
            [(0.0, 0.0), (att_s, gain), (hold_s, gain * 0.85), (dur_seg, 0.0)],
            shape="smooth",
        )
        gate[start:start + count] = np.maximum(gate[start:start + count], s_env)

    t = np.arange(n) / SAMPLE_RATE
    # Organic creature micro-vibrato (5.5 Hz + 1.9 Hz gentle expression).
    warble = (
        warble_cents * np.sin(2.0 * math.pi * 5.5 * t)
        + 0.35 * warble_cents * np.sin(2.0 * math.pi * 1.9 * t + 0.8)
    )
    freq_mod = freq_track * (2.0 ** (warble / 1200.0))

    # Rounded glottal-like valve wave: fundamental + rich harmonics (2x..5x) that feed the vocal formants.
    fundamental_body = 0.80 * osc.sine(n, freq_mod)
    glottal_rich = (
        fundamental_body
        + 0.48 * osc.sine(n, freq_mod * 2.0, phase=0.18)
        + 0.32 * osc.sine(n, freq_mod * 3.0, phase=0.44)
        + 0.20 * osc.sine(n, freq_mod * 4.0, phase=0.71)
        + 0.12 * osc.sine(n, freq_mod * 5.0, phase=0.29)
    )
    glottal_rounded = filters.dc_block(effects.saturate(glottal_rich + 0.10, drive=1.65), 25.0)
    velvet_breath = filters.bandpass(noise.pink(n, gen), 1150.0, 1.5) * (0.055 + 0.045 * fundamental_body)
    excitation = glottal_rounded + velvet_breath

    # Dual swept vocal formants (F1 throat roundness, F2 cooing vowel cavity) + 1950 Hz creature nasal formant.
    formant_f1 = filters.swept(excitation, "bandpass", np.clip(f1_track, 280.0, 860.0), q=3.2)
    formant_f2 = filters.swept(excitation, "bandpass", np.clip(f2_track, 850.0, 2150.0), q=3.8)
    formant_f3 = filters.bandpass(excitation, 1950.0, 3.4)

    # Blend direct fundamental body with strong vocal formant resonators.
    voiced = (0.44 * fundamental_body + 0.68 * formant_f1 + 0.78 * formant_f2 + 0.26 * formant_f3) * gate

    if sag_filter:
        cutoff = np.linspace(2350.0, 1250.0, n)
        voiced = filters.swept(voiced, "lowpass", cutoff, q=0.75)

    # Physical PTT relay clack + subtle speaker-cone thump at phrase start and release clack at end.
    first_hz = note_freq(syllables[0][1])
    place(voiced, 0.30 * relay_clack(first_hz, 0.10, gen, double=False, decay_s=0.06), 0)
    place(voiced, 0.20 * speaker_cone_thump(first_hz, 0.12, gen, decay_s=0.07), 0)
    end_pos = min(n - samples(0.09), samples(last_onset + last_dur))
    if end_pos > 0:
        place(voiced, 0.20 * relay_clack(note_freq(syllables[-1][1]) * 0.5, 0.08, gen,
                                         double=False, decay_s=0.05, muted=True), end_pos)

    return _radio_bench_chain(voiced, gen, hiss_level=0.011, drive_db=3.2, lp_hz=2900.0)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: physical rotary knob detent + soft relay clack + speaker-cone pulse on A4."""
    n = samples(0.30)
    mix = np.zeros(n)
    place(mix, 0.85 * knob_detent(note_freq("A4"), 0.24, gen, decay_s=0.13), 0)
    place(mix, 0.55 * relay_clack(note_freq("A4"), 0.18, gen, double=False, decay_s=0.10), samples(0.004))
    place(mix, 0.50 * speaker_cone_thump(note_freq("A4"), 0.24, gen, decay_s=0.12), samples(0.008))
    return _radio_bench_chain(mix, gen, hiss_level=0.009, drive_db=3.0, lp_hz=2850.0)


def place_card(gen) -> np.ndarray:
    """Place card shows: 4-step rotary band-selector knob detents + relay clack + bench speaker chime
    (A4 F#4 A4 -> D5 over D3)."""
    n = samples(1.65)
    mix = np.zeros(n)
    place(mix, 0.52 * bakelite_switch(note_freq("D4"), 0.22, gen, direction="up", decay_s=0.14), 0)

    phrase = (
        (0.03, "A4", 0.72, 0.46),
        (0.19, "F#4", 0.64, 0.42),
        (0.35, "A4", 0.70, 0.46),
        (0.52, "D5", 0.88, 0.72),
    )
    for when, note, gain, dec in phrase:
        hz = note_freq(note)
        det = knob_detent(hz, 0.30, gen, decay_s=0.16)
        cone = speaker_cone_thump(hz, 0.90, gen, decay_s=dec)
        place(mix, gain * 0.52 * det, samples(when))
        place(mix, gain * 0.72 * cone, samples(when))

    # Grounded D3 speaker-cone & transformer resonance under the resolving D5.
    place(mix, 0.42 * speaker_cone_thump(note_freq("D3"), 0.96, gen, decay_s=0.70), samples(0.52))
    place(mix, 0.38 * relay_clack(note_freq("D4"), 0.20, gen, double=True, decay_s=0.13), samples(0.51))
    return _radio_bench_chain(mix, gen, hiss_level=0.011, drive_db=3.3, lp_hz=2900.0)


def tab_open(gen) -> np.ndarray:
    """Tab opens: heavy bakelite toggle switch thrown ON + speaker-cone thump + relay clack rising D4 -> A4."""
    n = samples(0.68)
    mix = np.zeros(n)
    place(mix, 0.78 * bakelite_switch(note_freq("D4"), 0.28, gen, direction="up", decay_s=0.20), 0)
    place(mix, 0.68 * speaker_cone_thump(note_freq("D4"), 0.46, gen, decay_s=0.26), samples(0.016))
    place(mix, 0.62 * relay_clack(note_freq("A4"), 0.22, gen, double=True, decay_s=0.15), samples(0.086))
    place(mix, 0.84 * speaker_cone_thump(note_freq("A4"), 0.52, gen, decay_s=0.34), samples(0.092))
    return _radio_bench_chain(mix, gen, hiss_level=0.011, drive_db=3.2, lp_hz=2850.0)


def tab_close(gen) -> np.ndarray:
    """Tab closes: bakelite switch thrown OFF + relay release clack + speaker-cone thump settling A4 -> D4."""
    n = samples(0.64)
    mix = np.zeros(n)
    place(mix, 0.68 * knob_detent(note_freq("A4"), 0.24, gen, decay_s=0.16), 0)
    place(mix, 0.72 * speaker_cone_thump(note_freq("A4"), 0.42, gen, decay_s=0.24), samples(0.008))
    place(mix, 0.78 * bakelite_switch(note_freq("D4"), 0.28, gen, direction="down", decay_s=0.20, muted=True),
          samples(0.080))
    place(mix, 0.82 * speaker_cone_thump(note_freq("D4"), 0.46, gen, decay_s=0.30, sag_semitones=-0.20),
          samples(0.086))
    place(mix, 0.52 * relay_clack(note_freq("D4"), 0.20, gen, double=True, decay_s=0.12, muted=True), samples(0.094))
    return _radio_bench_chain(mix, gen, hiss_level=0.010, drive_db=3.1, lp_hz=2750.0)


def item_pick(gen) -> np.ndarray:
    """Item picked: rotary knob detent click + relay clack lifting E4 -> A4 with a crisp speaker-cone pulse."""
    n = samples(0.34)
    mix = np.zeros(n)
    place(mix, 0.75 * knob_detent(note_freq("E4"), 0.20, gen, decay_s=0.11), 0)
    place(mix, 0.62 * speaker_cone_thump(note_freq("E4"), 0.20, gen, decay_s=0.11), samples(0.006))
    place(mix, 0.68 * relay_clack(note_freq("A4"), 0.20, gen, double=False, decay_s=0.12), samples(0.056))
    place(mix, 0.82 * speaker_cone_thump(note_freq("A4"), 0.24, gen, decay_s=0.14), samples(0.060))
    return _radio_bench_chain(mix, gen, hiss_level=0.009, drive_db=3.0, lp_hz=2850.0)


def item_drop(gen) -> np.ndarray:
    """Item dropped: heavy speaker-cone thump + seated bakelite switch & double relay clack locking onto D4/D3."""
    n = samples(0.38)
    mix = np.zeros(n)
    place(mix, 0.78 * bakelite_switch(note_freq("D4"), 0.24, gen, direction="down", decay_s=0.15), 0)
    place(mix, 0.68 * relay_clack(note_freq("D4"), 0.22, gen, double=True, decay_s=0.13), samples(0.006))
    place(mix, 0.84 * speaker_cone_thump(note_freq("D4"), 0.30, gen, decay_s=0.17), samples(0.012))
    place(mix, 0.40 * speaker_cone_thump(note_freq("D3"), 0.28, gen, decay_s=0.15), samples(0.014))
    return _radio_bench_chain(mix, gen, hiss_level=0.009, drive_db=3.2, lp_hz=2700.0)


def item_refuse(gen) -> np.ndarray:
    """Refused: two blocked bakelite-switch & relay clacks falling B4 -> F#4 with a damped speaker-cone thud."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.72 * relay_clack(note_freq("B4"), 0.16, gen, double=False, decay_s=0.08, muted=True), 0)
    place(mix, 0.68 * speaker_cone_thump(note_freq("B4"), 0.16, gen, decay_s=0.08), samples(0.005))
    place(mix, 0.78 * bakelite_switch(note_freq("F#4"), 0.20, gen, direction="down", decay_s=0.09, muted=True),
          samples(0.092))
    place(mix, 0.82 * speaker_cone_thump(note_freq("F#4"), 0.22, gen, decay_s=0.10, sag_semitones=-0.06),
          samples(0.098))
    return _radio_bench_chain(mix, gen, hiss_level=0.010, drive_db=3.2, lp_hz=2350.0)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: soft relay clack + rotary detent tick into a warm E4 -> A4 bench indicator chime."""
    n = samples(0.62)
    mix = np.zeros(n)
    place(mix, 0.62 * relay_clack(note_freq("E4"), 0.18, gen, double=False, decay_s=0.11), 0)
    place(mix, 0.56 * speaker_cone_thump(note_freq("E4"), 0.40, gen, decay_s=0.24), samples(0.008))
    place(mix, 0.66 * knob_detent(note_freq("A4"), 0.26, gen, decay_s=0.17), samples(0.044))
    place(mix, 0.82 * speaker_cone_thump(note_freq("A4"), 0.48, gen, decay_s=0.32), samples(0.048))
    return _radio_bench_chain(mix, gen, hiss_level=0.010, drive_db=3.0, lp_hz=2850.0)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

# Each entry: (onset_s, note, duration_s, bend_semitones, gain, (f1_start, f1_peak), (f2_start, f2_peak))
RADIO_BENCH_CHIRPS = {
    "curious": (
        (0.00, "D4", 0.14, 0.0, 0.74, (360.0, 520.0), (1020.0, 1420.0)),
        (0.16, "F#4", 0.14, 0.0, 0.84, (420.0, 610.0), (1180.0, 1620.0)),
        (0.32, "A4", 0.24, 2.0, 0.96, (460.0, 690.0), (1280.0, 1880.0)),
    ),
    "affirm": (
        (0.00, "A4", 0.12, 0.0, 0.82, (390.0, 560.0), (1120.0, 1520.0)),
        (0.13, "D5", 0.20, 0.0, 0.96, (490.0, 680.0), (1360.0, 1820.0)),
    ),
    "proud": (
        (0.00, "D4", 0.13, 0.0, 0.72, (360.0, 500.0), (1020.0, 1340.0)),
        (0.15, "E4", 0.13, 0.0, 0.78, (400.0, 550.0), (1120.0, 1460.0)),
        (0.30, "F#4", 0.13, 0.0, 0.84, (440.0, 600.0), (1220.0, 1580.0)),
        (0.45, "A4", 0.15, 0.0, 0.90, (480.0, 650.0), (1340.0, 1720.0)),
        (0.62, "D5", 0.30, 0.0, 1.00, (520.0, 710.0), (1440.0, 1900.0)),
    ),
    "worried": (
        (0.00, "B4", 0.20, 0.0, 0.88, (560.0, 410.0), (1520.0, 1080.0)),
        (0.21, "F#4", 0.30, -1.0, 0.78, (430.0, 320.0), (1080.0, 840.0)),
    ),
    "relieved": (
        (0.00, "F#4", 0.19, 0.0, 0.80, (580.0, 460.0), (1540.0, 1220.0)),
        (0.21, "D4", 0.23, 0.0, 0.76, (450.0, 360.0), (1160.0, 960.0)),
        (0.46, "A4", 0.30, 0.0, 0.96, (420.0, 580.0), (1120.0, 1520.0)),
    ),
}


def chirp_curious(gen) -> np.ndarray:
    return vocal_valve_chirp_phrase(RADIO_BENCH_CHIRPS["curious"], gen, warble_cents=10.5)


def chirp_affirm(gen) -> np.ndarray:
    return vocal_valve_chirp_phrase(RADIO_BENCH_CHIRPS["affirm"], gen, warble_cents=8.5)


def chirp_proud(gen) -> np.ndarray:
    return vocal_valve_chirp_phrase(RADIO_BENCH_CHIRPS["proud"], gen, warble_cents=9.5)


def chirp_worried(gen) -> np.ndarray:
    return vocal_valve_chirp_phrase(RADIO_BENCH_CHIRPS["worried"], gen, warble_cents=18.0, sag_filter=True)


def chirp_relieved(gen) -> np.ndarray:
    return vocal_valve_chirp_phrase(RADIO_BENCH_CHIRPS["relieved"], gen, warble_cents=9.0)


def scan_complete(gen) -> np.ndarray:
    """A scan completes: stepping rotary knob detents across the dial + relay clack + speaker-cone thump locking onto
    07's formant-voiced valve chord (A4 -> D5 -> F#5)."""
    n = samples(1.38)
    mix = np.zeros(n)

    # Physical rotary tuning knob stepping through 3 detents + bakelite lock switch.
    for onset_s, note_name, gain in ((0.00, "D4", 0.52), (0.14, "E4", 0.44), (0.28, "F#4", 0.44)):
        place(mix, gain * knob_detent(note_freq(note_name), 0.18, gen, decay_s=0.10), samples(onset_s))
    place(mix, 0.48 * relay_clack(note_freq("A4"), 0.18, gen, double=True, decay_s=0.12), samples(0.38))

    # Formant-filtered heterodyne murmur gliding D4 -> A4 as the dial turns.
    sweep_n = samples(0.62)
    glide_f = osc.glide(sweep_n, note_freq("D4"), note_freq("A4"), time_constant=0.20)
    carrier = 0.70 * osc.sine(sweep_n, glide_f) + 0.30 * osc.sine(sweep_n, glide_f * 2.0)
    carrier_formant = filters.swept(carrier, "bandpass", np.linspace(520.0, 1420.0, sweep_n), q=3.2)
    carrier_env = envelope.segments(sweep_n, [(0.0, 0.0), (0.10, 0.34), (0.48, 0.42), (0.62, 0.0)], shape="smooth")
    place(mix, (0.50 * carrier + 0.50 * carrier_formant) * carrier_env, 0)

    # Three bench speaker-cone + formant-voiced valve notes (A4 -> D5 -> F#5) locking in over D4.
    steps = (
        (0.16, "A4", 0.70, 0.52),
        (0.40, "D5", 0.84, 0.62),
        (0.66, "F#5", 0.90, 0.56),
    )
    for when, note, gain, dec in steps:
        hz = note_freq(note)
        cone = speaker_cone_thump(hz, 0.66, gen, decay_s=dec)
        place(mix, gain * cone, samples(when))

    # Add 07's warm vocal-formant murmur on D5/F#5 and grounded D4 speaker body.
    murmur = vocal_valve_chirp_phrase(
        ((0.0, "D5", 0.24, 0.0, 0.46, (480.0, 640.0), (1320.0, 1720.0)),
         (0.26, "F#5", 0.30, 0.0, 0.52, (520.0, 680.0), (1440.0, 1860.0))),
        gen, warble_cents=8.0,
    )
    place(mix, 0.48 * murmur, samples(0.40))
    place(mix, 0.40 * speaker_cone_thump(note_freq("D4"), 0.72, gen, decay_s=0.55), samples(0.40))
    return _radio_bench_chain(mix, gen, hiss_level=0.011, drive_db=3.3, lp_hz=2950.0)
