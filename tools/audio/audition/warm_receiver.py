"""Refinement C1 ("Warm receiver"): warmer, fuller, gentler evolution of Direction C ("Old Radio").

Refines round-1 C ("Old Radio") per owner feedback:
- Much less AM/tape hiss (~14 dB quieter than C) and gentler triode saturation (~1.6 dB drive vs. C's ~4.2 dB).
- Fuller low-mid body: opens the receiver high-pass down to ~118 Hz, adds a warm 220 Hz console low-shelf and
  wooden cabinet chest resonances (165 Hz, 310 Hz, 720 Hz), and reinforces tones with cathode-coupled sub-octave body.
- Gentle top roll-off: smooth 6 dB/oct one-pole low-pass + soft second-stage roll-off instead of a steep cutoff.
- Slower, softer raised-cosine attacks (18..35 ms) and longer, unhurried tails in a cozy small room so nothing ever
  sounds thin, abrupt, or "cheap".
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono


def _warm_receiver_chain(x: np.ndarray, gen, hiss_level: float = 0.0045, drive_db: float = 1.6,
                         lp_hz: float = 2200.0, hp_hz: float = 118.0,
                         room: float = 0.42, wet: float = 0.22) -> np.ndarray:
    """Passes a signal through a clean, warm console receiver: low-mid chest down to ~118 Hz, gentle top roll-off,
    minimal velvet tube floor, mild triode saturation, and a cozy small room."""
    n = x.shape[0]
    dur = n / SAMPLE_RATE

    # 1. Whisper-quiet velvet tube-floor breath + subtle D2/D3 heater warmth (much quieter than C).
    hiss_raw = noise.pink(n, gen)
    hiss_band = filters.bandpass(hiss_raw, 680.0, 1.0) + 0.55 * filters.bandpass(hiss_raw, 320.0, 1.3)
    hiss_std = max(float(np.std(hiss_band)), 1e-9)
    drift_lfo = envelope.lfo(n, 3.2, 0.12, 0.88, phase=float(gen.uniform(0.0, 1.0)))
    filament = 0.30 * osc.sine(n, note_freq("D2")) + 0.18 * osc.sine(n, note_freq("D3"))
    bed_env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.045, dur * 0.25), 1.0),
         (max(min(0.045, dur * 0.25) + 0.010, dur * 0.80), 0.68),
         (dur, 0.0)],
        shape="smooth",
    )
    bed = (hiss_band / hiss_std * hiss_level * drift_lfo + 0.0035 * filament) * bed_env

    # 2. Mild asymmetric triode saturation (subtle 2nd-harmonic glow without fuzz or crunch).
    biased = x + bed + 0.016 * bed_env
    drive = 10.0 ** (drive_db / 20.0)
    saturated = filters.dc_block(effects.saturate(biased, drive=drive), 20.0)

    # 3. Full wooden console cabinet body (low-mid chest at 165 Hz & 310 Hz + open paper cone at 720 Hz).
    warm_shelf = filters.lowshelf(saturated, 220.0, gain_db=3.2, q=0.65)
    cab = (
        0.66 * warm_shelf
        + 0.26 * filters.bandpass(warm_shelf, 165.0, 1.8)
        + 0.30 * filters.bandpass(warm_shelf, 310.0, 1.9)
        + 0.18 * filters.bandpass(warm_shelf, 720.0, 2.1)
    )

    # 4. Very gentle, slow oscillator/tape drift.
    if n > 64:
        cab = effects.wow_flutter(cab, wow_cents=2.0, wow_rate=0.38,
                                  flutter_cents=0.6, flutter_rate=5.1)

    # 5. Open low-mid passband down to ~118 Hz + gentle 6 dB/oct top roll-off + soft HF guard.
    opened = filters.highpass(cab, hp_hz, q=0.65)
    rolled = filters.lowpass(filters.onepole_lp(opened, lp_hz), min(lp_hz * 1.48, 3400.0), q=0.58)

    # 6. Cozy small wooden room reflection with a warm damped tail.
    rev = to_mono(effects.reverb(rolled, room=room, damping=0.76, wet=wet, dry=1.0, predelay=0.011))
    return filters.highpass(filters.onepole_lp(rev, lp_hz * 1.15), hp_hz * 0.9)


def console_switch(duration: float, gen, coil_hz: float = 293.66, double: bool = False,
                   muted: bool = False) -> np.ndarray:
    """Cushioned mercury-wetted / felt-damped console switch: soft low-mid thud + warm inductor settle."""
    n = samples(duration)
    out = np.zeros(n)

    events = [(0.004, 1.0, 1.0)]
    if double:
        events.append((0.026, 0.62, 0.90))

    for onset_s, gain, scale in events:
        start = samples(onset_s)
        rem = n - start
        if rem <= 32:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0045, 0.018)
        # Warm low-mid console switch resonances (no high bakelite snap).
        c1 = filters.bandpass(burst, 440.0 * scale, 3.0) * envelope.ar(rem, 0.004, 0.026)
        c2 = filters.bandpass(burst, 820.0 * scale, 2.8) * envelope.ar(rem, 0.0035, 0.018)
        # Deep wooden chassis cushion (~132 Hz).
        thump = osc.sine(rem, osc.glide(rem, 162.0, 122.0, time_constant=0.014)) * envelope.ar(rem, 0.0045, 0.048)
        # Warm inductor ring at coil_hz and its sub-octave.
        coil_t60 = 0.065 if not muted else 0.032
        coil = (
            0.75 * osc.sine(rem, coil_hz * scale)
            + 0.35 * osc.sine(rem, max(coil_hz * 0.5 * scale, 115.0))
        ) * envelope.ar(rem, 0.005, coil_t60)
        seg = gain * (0.65 * c1 + 0.32 * c2 + 0.62 * thump + 0.45 * coil)
        place(out, envelope.fade_out(seg, min(0.015, rem / SAMPLE_RATE * 0.25)), start)

    return filters.onepole_lp(filters.highpass(out, 95.0), 1750.0 if not muted else 1300.0)


def warm_valve_tone(freq: float, duration: float, gen, attack_s: float = 0.026,
                    decay_s: float = 0.52, warble_cents: float = 3.5,
                    sag_semitones: float = 0.0, sub_gain: float = 0.24) -> np.ndarray:
    """Full-bodied vacuum-tube tone with a slow raised-cosine attack, sub-octave cathode warmth, and long tail."""
    n = samples(duration)
    t = np.arange(n) / SAMPLE_RATE

    # Subtle 5-cent capacitor settle over 20 ms (smooth, never pitch-bendy).
    rc_settle = 1.0 - 0.003 * np.exp(-t / 0.020)
    f_curve = np.full(n, freq, dtype=np.float64) * rc_settle
    if abs(sag_semitones) > 1e-4:
        u = np.clip((t - duration * 0.30) / max(duration * 0.70, 0.02), 0.0, 1.0) ** 1.4
        f_curve *= 2.0 ** (sag_semitones * u / 12.0)

    if warble_cents > 0.0:
        f_curve *= 2.0 ** (warble_cents * np.sin(2.0 * math.pi * 4.8 * t + 0.25) / 1200.0)

    # Fundamental + warm sub-octave low-mid body + soft 2nd & trace 3rd tube harmonics.
    sub_hz = f_curve * 0.5
    sub_voice = sub_gain * osc.sine(n, sub_hz, phase=0.08) if freq * 0.5 >= 110.0 else np.zeros(n)
    wave = (
        0.82 * osc.sine(n, f_curve)
        + sub_voice
        + 0.20 * osc.sine(n, f_curve * 2.0, phase=0.14)
        + 0.04 * osc.sine(n, f_curve * 3.0, phase=0.38)
    )
    env = envelope.ar(n, attack_s, decay_s, shape="smooth")
    toned = filters.onepole_lp(wave * env, min(freq * 3.2, 2300.0))
    return envelope.fade_out(toned, min(0.035, duration * 0.20))


def warm_valve_chirp_phrase(syllables: tuple, gen, warble_cents: float = 7.5,
                            sag_filter: bool = False) -> np.ndarray:
    """07's voice through the Warm Receiver: slower, creamier legato tube phrasing with rich low-mid chest."""
    last_onset, _, last_dur, _, _ = syllables[-1]
    total_s = last_onset + last_dur + 0.32
    n = samples(total_s)

    freq_track = np.full(n, note_freq(syllables[0][1]), dtype=np.float64)
    gate = np.zeros(n, dtype=np.float64)
    prev_hz = note_freq(syllables[0][1])

    for onset, note, length, bend, gain in syllables:
        start = samples(onset)
        count = min(samples(length + 0.09), n - start)
        if count <= 16:
            continue
        hz = note_freq(note)
        glide_n = min(count, samples(0.070))
        seg = np.full(count, hz, dtype=np.float64)
        seg[:glide_n] = osc.glide(glide_n, prev_hz, hz, time_constant=0.022)
        if abs(bend) > 1e-4:
            tail = min(count, samples(min(length, 0.16)))
            seg[-tail:] *= 2.0 ** (np.linspace(0.0, bend, tail) / 12.0)
        freq_track[start:start + count] = seg
        freq_track[start + count:] = seg[-1]
        prev_hz = float(seg[-1])

        # Slower, softer raised-cosine syllable envelope (26 ms attack, long gentle release).
        att_s = min(0.028, length * 0.32)
        hold_s = max(att_s + 0.008, length * 0.78)
        s_env = envelope.segments(
            count,
            [(0.0, 0.0), (att_s, gain), (hold_s, gain * 0.86), (count / SAMPLE_RATE, 0.0)],
            shape="smooth",
        )
        gate[start:start + count] = np.maximum(gate[start:start + count], s_env)

    t = np.arange(n) / SAMPLE_RATE
    warble = (
        warble_cents * np.sin(2.0 * math.pi * 4.9 * t)
        + 0.30 * warble_cents * np.sin(2.0 * math.pi * 1.6 * t + 0.7)
    )
    freq_mod = freq_track * (2.0 ** (warble / 1200.0))

    # Richer sub-octave cathode coupling (0.24) + warm fundamental + gentle 2nd harmonic.
    voice = (
        0.80 * osc.sine(n, freq_mod)
        + 0.24 * osc.sine(n, np.maximum(freq_mod * 0.5, 118.0), phase=0.09)
        + 0.20 * osc.sine(n, freq_mod * 2.0, phase=0.19)
        + 0.04 * osc.sine(n, freq_mod * 3.0, phase=0.47)
    )
    # Warm low-mid receiver resonance tracking the fundamental and sub-octave chest.
    warm_body = (
        0.65 * filters.swept(voice, "bandpass", np.clip(freq_mod * 0.95, 130.0, 1800.0), q=2.2)
        + 0.35 * filters.swept(voice, "bandpass", np.clip(freq_mod * 0.5, 120.0, 900.0), q=1.8)
    )
    shaped = (0.68 * voice + 0.45 * warm_body) * gate

    if sag_filter:
        cutoff = np.linspace(1950.0, 1150.0, n)
        shaped = filters.swept(shaped, "lowpass", cutoff, q=0.707)

    # Whisper-soft cushioned switch at start and end.
    place(shaped, 0.12 * console_switch(0.11, gen, coil_hz=note_freq(syllables[0][1]), double=False, muted=True), 0)
    end_pos = min(n - samples(0.10), samples(last_onset + last_dur + 0.02))
    if end_pos > 0:
        place(shaped, 0.08 * console_switch(0.09, gen, coil_hz=note_freq(syllables[-1][1]) * 0.5,
                                            double=False, muted=True), end_pos)

    return _warm_receiver_chain(shaped, gen, hiss_level=0.005, drive_db=1.8, lp_hz=2150.0, room=0.45, wet=0.24)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: cushioned mercury-switch pulse + warm low-mid A4/A3 receiver tone with a soft tail."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.38 * console_switch(0.18, gen, coil_hz=note_freq("A4"), double=False, muted=True), 0)
    place(mix, 0.78 * warm_valve_tone(note_freq("A4"), 0.32, gen, attack_s=0.018, decay_s=0.20,
                                      warble_cents=2.5, sub_gain=0.28), samples(0.012))
    return _warm_receiver_chain(mix, gen, hiss_level=0.0035, drive_db=1.4, lp_hz=2050.0, room=0.38, wet=0.20)


def place_card(gen) -> np.ndarray:
    """Place card shows: soft console switch + unhurried 4-note warm tube chime (A4 F#4 A4 -> D5 over D3/A3)."""
    n = samples(1.82)
    mix = np.zeros(n)
    place(mix, 0.22 * console_switch(0.20, gen, coil_hz=note_freq("D4"), double=True, muted=True), 0)

    phrase = (
        (0.04, "A4", 0.68, 0.68),
        (0.21, "F#4", 0.62, 0.64),
        (0.38, "A4", 0.66, 0.68),
        (0.56, "D5", 0.84, 0.98),
    )
    for when, note, gain, dec in phrase:
        tone = warm_valve_tone(note_freq(note), 1.12, gen, attack_s=0.026, decay_s=dec,
                               warble_cents=4.5, sub_gain=0.26)
        place(mix, gain * tone, samples(when))

    # Deep D3 + A3 console receiver foundation under the resolving D5.
    place(mix, 0.56 * warm_valve_tone(note_freq("D3"), 1.18, gen, attack_s=0.035, decay_s=0.95,
                                      warble_cents=3.0, sub_gain=0.0), samples(0.54))
    place(mix, 0.32 * warm_valve_tone(note_freq("A3"), 1.10, gen, attack_s=0.032, decay_s=0.85,
                                      warble_cents=3.0, sub_gain=0.0), samples(0.56))
    return _warm_receiver_chain(mix, gen, hiss_level=0.0045, drive_db=1.7, lp_hz=2200.0, room=0.48, wet=0.25)


def tab_open(gen) -> np.ndarray:
    """Tab opens: cushioned console switch + slow-blooming rising receiver tones D4 -> A4 over D3 warmth."""
    n = samples(0.80)
    mix = np.zeros(n)
    place(mix, 0.30 * console_switch(0.20, gen, coil_hz=note_freq("D4"), double=True, muted=True), 0)
    place(mix, 0.74 * warm_valve_tone(note_freq("D4"), 0.62, gen, attack_s=0.024, decay_s=0.44,
                                      warble_cents=3.5, sub_gain=0.30), samples(0.020))
    place(mix, 0.88 * warm_valve_tone(note_freq("A4"), 0.66, gen, attack_s=0.026, decay_s=0.50,
                                      warble_cents=4.0, sub_gain=0.28), samples(0.105))
    return _warm_receiver_chain(mix, gen, hiss_level=0.004, drive_db=1.6, lp_hz=2150.0, room=0.44, wet=0.22)


def tab_close(gen) -> np.ndarray:
    """Tab closes: gentle falling receiver tones A4 -> D4 settling into a warm D3 tube rest in a small room."""
    n = samples(0.76)
    mix = np.zeros(n)
    place(mix, 0.78 * warm_valve_tone(note_freq("A4"), 0.56, gen, attack_s=0.024, decay_s=0.40,
                                      warble_cents=3.5, sub_gain=0.25), samples(0.010))
    place(mix, 0.86 * warm_valve_tone(note_freq("D4"), 0.62, gen, attack_s=0.026, decay_s=0.46,
                                      warble_cents=3.5, sag_semitones=-0.18, sub_gain=0.34), samples(0.095))
    place(mix, 0.28 * console_switch(0.20, gen, coil_hz=note_freq("D4"), double=False, muted=True), samples(0.110))
    return _warm_receiver_chain(mix, gen, hiss_level=0.004, drive_db=1.5, lp_hz=2000.0, room=0.44, wet=0.22)


def item_pick(gen) -> np.ndarray:
    """Item picked: soft console contact + rounded rising receiver tones E4 -> A4 with low-mid body."""
    n = samples(0.42)
    mix = np.zeros(n)
    place(mix, 0.30 * console_switch(0.14, gen, coil_hz=note_freq("E4"), double=False, muted=True), 0)
    place(mix, 0.72 * warm_valve_tone(note_freq("E4"), 0.26, gen, attack_s=0.018, decay_s=0.17,
                                      warble_cents=3.0, sub_gain=0.26), samples(0.008))
    place(mix, 0.86 * warm_valve_tone(note_freq("A4"), 0.32, gen, attack_s=0.020, decay_s=0.22,
                                      warble_cents=3.0, sub_gain=0.28), samples(0.068))
    return _warm_receiver_chain(mix, gen, hiss_level=0.0035, drive_db=1.5, lp_hz=2100.0, room=0.38, wet=0.19)


def item_drop(gen) -> np.ndarray:
    """Item dropped: deep cushioned transformer/console settle + grounded D4 & D3 warm tube bloom."""
    n = samples(0.46)
    mix = np.zeros(n)
    place(mix, 0.42 * console_switch(0.22, gen, coil_hz=note_freq("D4"), double=True, muted=True), 0)
    place(mix, 0.86 * warm_valve_tone(note_freq("D4"), 0.38, gen, attack_s=0.020, decay_s=0.26,
                                      warble_cents=3.0, sub_gain=0.32), samples(0.016))
    place(mix, 0.54 * warm_valve_tone(note_freq("D3"), 0.38, gen, attack_s=0.022, decay_s=0.26,
                                      warble_cents=2.0, sub_gain=0.0), samples(0.016))
    return _warm_receiver_chain(mix, gen, hiss_level=0.0035, drive_db=1.6, lp_hz=1950.0, room=0.40, wet=0.20)


def item_refuse(gen) -> np.ndarray:
    """Refused: two soft, dark-filtered receiver tones falling B4 -> F#4 with cushioned attacks."""
    n = samples(0.45)
    mix = np.zeros(n)
    place(mix, 0.32 * console_switch(0.16, gen, coil_hz=note_freq("B4"), double=False, muted=True), 0)
    place(mix, 0.72 * warm_valve_tone(note_freq("B4"), 0.22, gen, attack_s=0.018, decay_s=0.14,
                                      warble_cents=2.5, sub_gain=0.26), samples(0.008))
    place(mix, 0.34 * console_switch(0.18, gen, coil_hz=note_freq("F#4"), double=False, muted=True), samples(0.108))
    place(mix, 0.86 * warm_valve_tone(note_freq("F#4"), 0.30, gen, attack_s=0.020, decay_s=0.20,
                                      warble_cents=2.5, sag_semitones=-0.06, sub_gain=0.30), samples(0.114))
    return _warm_receiver_chain(mix, gen, hiss_level=0.0035, drive_db=1.5, lp_hz=1750.0, room=0.38, wet=0.19)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: whisper-soft switch + slow-blooming E4 -> A4 warm console tone in a small room."""
    n = samples(0.74)
    mix = np.zeros(n)
    place(mix, 0.22 * console_switch(0.14, gen, coil_hz=note_freq("E4"), double=False, muted=True), 0)
    place(mix, 0.62 * warm_valve_tone(note_freq("E4"), 0.56, gen, attack_s=0.024, decay_s=0.38,
                                      warble_cents=3.5, sub_gain=0.26), samples(0.012))
    place(mix, 0.86 * warm_valve_tone(note_freq("A4"), 0.64, gen, attack_s=0.028, decay_s=0.48,
                                      warble_cents=4.0, sub_gain=0.28), samples(0.060))
    return _warm_receiver_chain(mix, gen, hiss_level=0.004, drive_db=1.5, lp_hz=2150.0, room=0.44, wet=0.22)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

WARM_RECEIVER_CHIRPS = {
    "curious": (
        (0.00, "D4", 0.16, 0.0, 0.74),
        (0.18, "F#4", 0.16, 0.0, 0.84),
        (0.36, "A4", 0.28, 2.0, 0.96),
    ),
    "affirm": (
        (0.00, "A4", 0.14, 0.0, 0.82),
        (0.15, "D5", 0.24, 0.0, 0.96),
    ),
    "proud": (
        (0.00, "D4", 0.14, 0.0, 0.72),
        (0.16, "E4", 0.14, 0.0, 0.78),
        (0.32, "F#4", 0.14, 0.0, 0.84),
        (0.48, "A4", 0.16, 0.0, 0.90),
        (0.66, "D5", 0.34, 0.0, 1.00),
    ),
    "worried": (
        (0.00, "B4", 0.22, 0.0, 0.88),
        (0.24, "F#4", 0.34, -0.8, 0.78),
    ),
    "relieved": (
        (0.00, "F#4", 0.21, 0.0, 0.80),
        (0.23, "D4", 0.25, 0.0, 0.76),
        (0.50, "A4", 0.34, 0.0, 0.96),
    ),
}


def chirp_curious(gen) -> np.ndarray:
    return warm_valve_chirp_phrase(WARM_RECEIVER_CHIRPS["curious"], gen, warble_cents=8.0)


def chirp_affirm(gen) -> np.ndarray:
    return warm_valve_chirp_phrase(WARM_RECEIVER_CHIRPS["affirm"], gen, warble_cents=6.5)


def chirp_proud(gen) -> np.ndarray:
    return warm_valve_chirp_phrase(WARM_RECEIVER_CHIRPS["proud"], gen, warble_cents=7.5)


def chirp_worried(gen) -> np.ndarray:
    return warm_valve_chirp_phrase(WARM_RECEIVER_CHIRPS["worried"], gen, warble_cents=15.0, sag_filter=True)


def chirp_relieved(gen) -> np.ndarray:
    return warm_valve_chirp_phrase(WARM_RECEIVER_CHIRPS["relieved"], gen, warble_cents=7.0)


def scan_complete(gen) -> np.ndarray:
    """A scan completes: gentle low-mid tuning swell + slow-blooming 3-note warm receiver chord (A4 -> D5 -> F#5)."""
    n = samples(1.56)
    mix = np.zeros(n)

    # Soft cushioned console switch + warm low-mid tuning air swell (much quieter & darker than C).
    place(mix, 0.22 * console_switch(0.18, gen, coil_hz=note_freq("D4"), double=True, muted=True), 0)
    sweep_n = samples(0.72)
    centre = osc.glide(sweep_n, 260.0, 960.0, time_constant=0.26)
    dial_swish = filters.swept(noise.pink(sweep_n, gen), "bandpass", centre, q=2.8)
    dial_swish = dial_swish / max(float(np.std(dial_swish)), 1e-9) * 0.045
    dial_swish *= envelope.segments(sweep_n, [(0.0, 0.0), (0.18, 1.0), (0.54, 0.50), (0.72, 0.0)], shape="smooth")
    place(mix, dial_swish, 0)

    # Warm carrier glide (D3/D4 -> A3/A4) locking into three slow-attack receiver tones (A4 -> D5 -> F#5).
    carrier_f = osc.glide(sweep_n, note_freq("D4"), note_freq("A4"), time_constant=0.24)
    carrier = 0.70 * osc.sine(sweep_n, carrier_f) + 0.30 * osc.sine(sweep_n, carrier_f * 0.5)
    carrier *= envelope.segments(sweep_n, [(0.0, 0.0), (0.14, 0.32), (0.54, 0.40), (0.72, 0.0)], shape="smooth")
    place(mix, carrier, 0)

    steps = (
        (0.18, "A4", 0.70, 0.72),
        (0.44, "D5", 0.86, 0.84),
        (0.70, "F#5", 0.88, 0.76),
    )
    for when, note, gain, dec in steps:
        tone = warm_valve_tone(note_freq(note), 0.82, gen, attack_s=0.028, decay_s=dec,
                               warble_cents=5.0, sub_gain=0.28)
        place(mix, gain * tone, samples(when))

    place(mix, 0.52 * warm_valve_tone(note_freq("D3"), 0.92, gen, attack_s=0.035, decay_s=0.76,
                                      warble_cents=3.5, sub_gain=0.0), samples(0.44))
    place(mix, 0.44 * warm_valve_tone(note_freq("D4"), 0.90, gen, attack_s=0.030, decay_s=0.74,
                                      warble_cents=4.0, sub_gain=0.25), samples(0.44))
    return _warm_receiver_chain(mix, gen, hiss_level=0.005, drive_db=1.8, lp_hz=2250.0, room=0.48, wet=0.25)
