"""Direction C ("Old Radio"): warm tape/valve radio sound design for UI cues and 07's voice.

Evokes the crew's world (Bell's receiver, Kenji's tape deck, and warm vacuum-tube comms): band-limited passband
(~190 Hz .. 2.9 kHz), asymmetric triode saturation, faint envelope-gated AM/tape hiss, subtle tape wow & flutter,
soft magnetic reed-relay clicks for UI switches, and 07's chirps as a warm heterodyne valve-radio voice warbling in
D major pentatonic.
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono


def _valve_radio_chain(x: np.ndarray, gen, hiss_level: float = 0.022, drive_db: float = 4.2,
                       lp_hz: float = 2850.0, hp_hz: float = 185.0,
                       flutter_depth: float = 0.00055) -> np.ndarray:
    """Passes a signal through a vintage triode stage, bakelite speaker cabinet, tape flutter, and faint AM hiss."""
    n = x.shape[0]
    dur = n / SAMPLE_RATE

    # 1. Faint envelope-shaped AM/tape hiss + D2 filament warmth inside the cue window.
    hiss_raw = noise.pink(n, gen)
    hiss_band = filters.bandpass(hiss_raw, 1050.0, 1.1) + 0.45 * filters.bandpass(hiss_raw, 520.0, 1.5)
    hiss_std = max(float(np.std(hiss_band)), 1e-9)
    flutter_lfo = envelope.lfo(n, 4.5, 0.20, 0.80, phase=float(gen.uniform(0.0, 1.0)))
    filament = 0.25 * osc.sine(n, note_freq("D2")) + 0.12 * osc.sine(n, note_freq("D3"))
    bed_env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.022, dur * 0.2), 1.0),
         (max(min(0.022, dur * 0.2) + 0.005, dur * 0.78), 0.72),
         (dur, 0.0)],
        shape="smooth",
    )
    bed = (hiss_band / hiss_std * hiss_level * flutter_lfo + 0.008 * filament) * bed_env

    # 2. Asymmetric triode saturation (small DC bias produces warm 2nd-order even harmonics).
    biased = x + bed + 0.055 * bed_env
    drive = 10.0 ** (drive_db / 20.0)
    saturated = filters.dc_block(effects.saturate(biased, drive=drive), 25.0)

    # 3. Bakelite speaker cabinet formants (warm box resonance at 390 Hz and 1080 Hz).
    cab = (
        0.64 * saturated
        + 0.28 * filters.bandpass(saturated, 390.0, 2.2)
        + 0.22 * filters.bandpass(saturated, 1080.0, 2.5)
    )

    # 4. Gentle tape capstan wow & flutter.
    if flutter_depth > 0.0 and n > 64:
        cab = effects.wow_flutter(cab, wow_cents=3.5, wow_rate=0.45,
                                  flutter_cents=1.5, flutter_rate=6.2)

    # 5. Band-limited receiver passband + tight radio-room reflex.
    band = filters.highpass(filters.lowpass(filters.lowpass(cab, lp_hz), lp_hz), hp_hz)
    return to_mono(effects.reverb(band, room=0.24, damping=0.78, wet=0.10, dry=1.0, predelay=0.005))


def relay_click(duration: float, gen, coil_hz: float = 440.0, double: bool = False,
                muted: bool = False) -> np.ndarray:
    """Soft magnetic reed-relay / bakelite toggle switch: armature pull-in + contact closure + transformer thud."""
    n = samples(duration)
    out = np.zeros(n)

    events = [(0.002, 1.0, 1.0)]
    if double:
        events.append((0.019, 0.65, 0.88))

    for onset_s, gain, scale in events:
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        burst = noise.pink(rem, gen) * envelope.ar(rem, 0.0018, 0.010)
        # Warm bakelite switch resonances at 880 Hz and 1650 Hz.
        c1 = filters.bandpass(burst, 880.0 * scale, 4.0) * envelope.ar(rem, 0.0015, 0.016)
        c2 = filters.bandpass(burst, 1620.0 * scale, 3.5) * envelope.ar(rem, 0.0012, 0.011)
        # Transformer / chassis thump around 155 Hz.
        thump = osc.sine(rem, osc.glide(rem, 195.0, 135.0, time_constant=0.008)) * envelope.ar(rem, 0.002, 0.032)
        # Damped reed/coil ring at coil_hz.
        coil_t60 = 0.038 if not muted else 0.020
        coil = osc.sine(rem, coil_hz * scale) * envelope.ar(rem, 0.002, coil_t60)
        place(out, gain * (0.95 * c1 + 0.55 * c2 + 0.48 * thump + 0.36 * coil), start)

    return filters.lowpass(filters.highpass(out, 95.0), 2600.0 if not muted else 1900.0)


def valve_tone(freq: float, duration: float, gen, attack_s: float = 0.012,
               decay_s: float = 0.38, warble_cents: float = 6.0,
               sag_semitones: float = 0.0) -> np.ndarray:
    """Warm Wien-bridge / heterodyne vacuum-tube tone with RC capacitor pitch settle and gentle warble."""
    n = samples(duration)
    t = np.arange(n) / SAMPLE_RATE

    # RC capacitor charge-up settle on attack (starts ~14 cents flat and locks in over 14 ms).
    rc_settle = 1.0 - 0.008 * np.exp(-t / 0.014)
    f_curve = np.full(n, freq, dtype=np.float64) * rc_settle
    if abs(sag_semitones) > 1e-4:
        u = np.clip((t - duration * 0.25) / max(duration * 0.75, 0.02), 0.0, 1.0) ** 1.3
        f_curve *= 2.0 ** (sag_semitones * u / 12.0)

    # Gentle tube oscillator warble.
    if warble_cents > 0.0:
        f_curve *= 2.0 ** (warble_cents * np.sin(2.0 * math.pi * 5.6 * t + 0.3) / 1200.0)

    # Tube harmonics: strong fundamental, warm 2nd harmonic, soft 3rd harmonic.
    wave = (
        0.78 * osc.sine(n, f_curve)
        + 0.24 * osc.sine(n, f_curve * 2.0, phase=0.15)
        + 0.07 * osc.sine(n, f_curve * 3.0, phase=0.42)
    )
    env = envelope.ar(n, attack_s, decay_s)
    return wave * env


def valve_chirp_phrase(syllables: tuple, gen, warble_cents: float = 11.0,
                       sag_filter: bool = False) -> np.ndarray:
    """07's valve-radio voice: continuous RC-gliding heterodyne oscillator keyed through soft reed relays."""
    last_onset, _, last_dur, _, _ = syllables[-1]
    total_s = last_onset + last_dur + 0.22
    n = samples(total_s)

    # Continuous frequency trajectory with warm RC portamento between syllables.
    freq_track = np.full(n, note_freq(syllables[0][1]), dtype=np.float64)
    gate = np.zeros(n, dtype=np.float64)
    prev_hz = note_freq(syllables[0][1])

    for onset, note, length, bend, gain in syllables:
        start = samples(onset)
        count = min(samples(length + 0.04), n - start)
        if count <= 8:
            continue
        hz = note_freq(note)
        glide_n = min(count, samples(0.055))
        seg = np.full(count, hz, dtype=np.float64)
        seg[:glide_n] = osc.glide(glide_n, prev_hz, hz, time_constant=0.016)
        if abs(bend) > 1e-4:
            tail = min(count, samples(min(length, 0.14)))
            seg[-tail:] *= 2.0 ** (np.linspace(0.0, bend, tail) / 12.0)
        freq_track[start:start + count] = seg
        freq_track[start + count:] = seg[-1]
        prev_hz = float(seg[-1])

        # Smooth tube-keying envelope per syllable (14 ms attack, rounded release).
        s_env = envelope.segments(
            count,
            [(0.0, 0.0), (min(0.015, length * 0.25), gain),
             (max(min(0.015, length * 0.25) + 0.004, length * 0.76), gain * 0.84),
             (count / SAMPLE_RATE, 0.0)],
            shape="smooth",
        )
        gate[start:start + count] = np.maximum(gate[start:start + count], s_env)

    t = np.arange(n) / SAMPLE_RATE
    # Dual-rate organic tube warble (5.6 Hz main warble + 1.8 Hz slow drift).
    warble = (
        warble_cents * np.sin(2.0 * math.pi * 5.6 * t)
        + 0.35 * warble_cents * np.sin(2.0 * math.pi * 1.8 * t + 0.9)
    )
    freq_mod = freq_track * (2.0 ** (warble / 1200.0))

    # Heterodyne valve voice: fundamental + warm 2nd/3rd tube harmonics + sub-octave D-tuned hum coupling.
    voice = (
        0.76 * osc.sine(n, freq_mod)
        + 0.26 * osc.sine(n, freq_mod * 2.0, phase=0.19)
        + 0.08 * osc.sine(n, freq_mod * 3.0, phase=0.51)
        + 0.10 * osc.sine(n, freq_mod * 0.5, phase=0.11)
    )
    # Narrow heterodyne IF bandpass filter tracking the voice for warm radio-receiver vocal formant.
    if_band = filters.swept(voice + 0.18 * noise.pink(n, gen), "bandpass",
                            np.clip(freq_mod * 1.15, 220.0, 2400.0), q=3.2)
    shaped = (0.62 * voice + 0.55 * if_band) * gate

    if sag_filter:
        # Low-battery filament sag: passband darkens progressively from 2400 Hz down to 1350 Hz.
        cutoff = np.linspace(2400.0, 1350.0, n)
        shaped = filters.swept(shaped, "lowpass", cutoff, q=0.8)

    # Soft relay key-on click at start and faint key-off release at end.
    place(shaped, 0.22 * relay_click(0.09, gen, coil_hz=note_freq(syllables[0][1]), double=False), 0)
    end_pos = min(n - samples(0.09), samples(last_onset + last_dur))
    if end_pos > 0:
        place(shaped, 0.14 * relay_click(0.08, gen, coil_hz=note_freq(syllables[-1][1]) * 0.5,
                                         double=False, muted=True), end_pos)

    return _valve_radio_chain(shaped, gen, hiss_level=0.025, drive_db=4.8, lp_hz=2750.0)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: soft reed-relay contact tick + a warm band-limited A4 capacitor pip."""
    n = samples(0.28)
    mix = np.zeros(n)
    place(mix, 0.65 * relay_click(0.16, gen, coil_hz=note_freq("A4"), double=False), 0)
    place(mix, 0.72 * valve_tone(note_freq("A4"), 0.22, gen, attack_s=0.006, decay_s=0.12, warble_cents=4.0),
          samples(0.010))
    return _valve_radio_chain(mix, gen, hiss_level=0.016, drive_db=3.8, lp_hz=2650.0)


def place_card(gen) -> np.ndarray:
    """Place card shows: rotary relay step + vintage 4-note tube station-ident chime (A4 F#4 A4 -> D5 over D3)."""
    n = samples(1.65)
    mix = np.zeros(n)
    place(mix, 0.35 * relay_click(0.18, gen, coil_hz=note_freq("D4"), double=True), 0)

    phrase = (
        (0.03, "A4", 0.70, 0.52),
        (0.19, "F#4", 0.62, 0.48),
        (0.35, "A4", 0.68, 0.52),
        (0.52, "D5", 0.85, 0.80),
    )
    for when, note, gain, dec in phrase:
        tone = valve_tone(note_freq(note), 0.92, gen, attack_s=0.010, decay_s=dec, warble_cents=7.5)
        place(mix, gain * tone, samples(when))

    # Warm D3 tube drone under the resolving note.
    place(mix, 0.46 * valve_tone(note_freq("D3"), 0.98, gen, attack_s=0.020, decay_s=0.78, warble_cents=5.0),
          samples(0.52))
    return _valve_radio_chain(mix, gen, hiss_level=0.024, drive_db=4.5, lp_hz=2800.0)


def tab_open(gen) -> np.ndarray:
    """Tab opens: comms toggle switch ON (double reed relay) + warm rising valve tones D4 -> A4."""
    n = samples(0.66)
    mix = np.zeros(n)
    place(mix, 0.52 * relay_click(0.18, gen, coil_hz=note_freq("D4"), double=True), 0)
    place(mix, 0.75 * valve_tone(note_freq("D4"), 0.48, gen, attack_s=0.010, decay_s=0.32, warble_cents=6.0),
          samples(0.018))
    place(mix, 0.88 * valve_tone(note_freq("A4"), 0.52, gen, attack_s=0.010, decay_s=0.38, warble_cents=6.5),
          samples(0.092))
    return _valve_radio_chain(mix, gen, hiss_level=0.020, drive_db=4.2, lp_hz=2750.0)


def tab_close(gen) -> np.ndarray:
    """Tab closes: falling valve tones A4 -> D4 sagging as the capacitor discharges + soft relay release."""
    n = samples(0.62)
    mix = np.zeros(n)
    place(mix, 0.80 * valve_tone(note_freq("A4"), 0.44, gen, attack_s=0.010, decay_s=0.30, warble_cents=6.0),
          samples(0.008))
    place(mix, 0.85 * valve_tone(note_freq("D4"), 0.48, gen, attack_s=0.010, decay_s=0.34,
                                 warble_cents=6.0, sag_semitones=-0.35), samples(0.082))
    place(mix, 0.48 * relay_click(0.18, gen, coil_hz=note_freq("D4"), double=True, muted=True), samples(0.095))
    return _valve_radio_chain(mix, gen, hiss_level=0.018, drive_db=4.0, lp_hz=2600.0)


def item_pick(gen) -> np.ndarray:
    """Item picked: bakelite patch-plug click + short rising two-tone valve blip E4 -> A4."""
    n = samples(0.32)
    mix = np.zeros(n)
    place(mix, 0.52 * relay_click(0.12, gen, coil_hz=note_freq("E4"), double=False), 0)
    place(mix, 0.72 * valve_tone(note_freq("E4"), 0.18, gen, attack_s=0.006, decay_s=0.11, warble_cents=5.0),
          samples(0.006))
    place(mix, 0.84 * valve_tone(note_freq("A4"), 0.22, gen, attack_s=0.006, decay_s=0.14, warble_cents=5.0),
          samples(0.058))
    return _valve_radio_chain(mix, gen, hiss_level=0.016, drive_db=4.0, lp_hz=2700.0)


def item_drop(gen) -> np.ndarray:
    """Item dropped: patch-bay plug seating with a warm transformer thump + grounded D4 tube tone."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.68 * relay_click(0.20, gen, coil_hz=note_freq("D4"), double=True), 0)
    place(mix, 0.85 * valve_tone(note_freq("D4"), 0.28, gen, attack_s=0.008, decay_s=0.18, warble_cents=4.5),
          samples(0.014))
    place(mix, 0.42 * valve_tone(note_freq("D3"), 0.26, gen, attack_s=0.008, decay_s=0.15, warble_cents=3.0),
          samples(0.014))
    return _valve_radio_chain(mix, gen, hiss_level=0.016, drive_db=4.2, lp_hz=2550.0)


def item_refuse(gen) -> np.ndarray:
    """Refused: two muted reed-relay drops falling B4 -> F#4 through a dark receiver filter."""
    n = samples(0.35)
    mix = np.zeros(n)
    place(mix, 0.55 * relay_click(0.14, gen, coil_hz=note_freq("B4"), double=False, muted=True), 0)
    place(mix, 0.74 * valve_tone(note_freq("B4"), 0.14, gen, attack_s=0.007, decay_s=0.08, warble_cents=3.5),
          samples(0.005))
    place(mix, 0.58 * relay_click(0.16, gen, coil_hz=note_freq("F#4"), double=False, muted=True), samples(0.094))
    place(mix, 0.84 * valve_tone(note_freq("F#4"), 0.18, gen, attack_s=0.007, decay_s=0.11,
                                 warble_cents=3.5, sag_semitones=-0.08), samples(0.098))
    return _valve_radio_chain(mix, gen, hiss_level=0.018, drive_db=4.2, lp_hz=2100.0)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: soft relay contact + warm E4 -> A4 heterodyne carrier whistle."""
    n = samples(0.60)
    mix = np.zeros(n)
    place(mix, 0.38 * relay_click(0.12, gen, coil_hz=note_freq("E4"), double=False), 0)
    place(mix, 0.60 * valve_tone(note_freq("E4"), 0.42, gen, attack_s=0.010, decay_s=0.28, warble_cents=6.0),
          samples(0.008))
    place(mix, 0.84 * valve_tone(note_freq("A4"), 0.48, gen, attack_s=0.012, decay_s=0.35, warble_cents=6.5),
          samples(0.046))
    return _valve_radio_chain(mix, gen, hiss_level=0.020, drive_db=4.0, lp_hz=2750.0)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

OLD_RADIO_CHIRPS = {
    "curious": (
        (0.00, "D4", 0.14, 0.0, 0.74),
        (0.16, "F#4", 0.14, 0.0, 0.84),
        (0.32, "A4", 0.24, 2.0, 0.96),
    ),
    "affirm": (
        (0.00, "A4", 0.12, 0.0, 0.82),
        (0.13, "D5", 0.20, 0.0, 0.96),
    ),
    "proud": (
        (0.00, "D4", 0.13, 0.0, 0.72),
        (0.15, "E4", 0.13, 0.0, 0.78),
        (0.30, "F#4", 0.13, 0.0, 0.84),
        (0.45, "A4", 0.15, 0.0, 0.90),
        (0.62, "D5", 0.30, 0.0, 1.00),
    ),
    "worried": (
        (0.00, "B4", 0.20, 0.0, 0.88),
        (0.21, "F#4", 0.30, -1.0, 0.78),
    ),
    "relieved": (
        (0.00, "F#4", 0.19, 0.0, 0.80),
        (0.21, "D4", 0.23, 0.0, 0.76),
        (0.46, "A4", 0.30, 0.0, 0.96),
    ),
}


def chirp_curious(gen) -> np.ndarray:
    return valve_chirp_phrase(OLD_RADIO_CHIRPS["curious"], gen, warble_cents=12.0)


def chirp_affirm(gen) -> np.ndarray:
    return valve_chirp_phrase(OLD_RADIO_CHIRPS["affirm"], gen, warble_cents=9.0)


def chirp_proud(gen) -> np.ndarray:
    return valve_chirp_phrase(OLD_RADIO_CHIRPS["proud"], gen, warble_cents=10.5)


def chirp_worried(gen) -> np.ndarray:
    return valve_chirp_phrase(OLD_RADIO_CHIRPS["worried"], gen, warble_cents=24.0, sag_filter=True)


def chirp_relieved(gen) -> np.ndarray:
    return valve_chirp_phrase(OLD_RADIO_CHIRPS["relieved"], gen, warble_cents=10.0)


def scan_complete(gen) -> np.ndarray:
    """A scan completes: AM dial-tuning sweep locking onto a warm three-note valve chord (A4 -> D5 -> F#5)."""
    n = samples(1.38)
    mix = np.zeros(n)

    # Dial tuning band-sweep over the first 0.55 s + soft relay catch.
    place(mix, 0.38 * relay_click(0.15, gen, coil_hz=note_freq("D4"), double=True), 0)
    sweep_n = samples(0.65)
    centre = osc.glide(sweep_n, 420.0, 1480.0, time_constant=0.22)
    dial_swish = filters.swept(noise.pink(sweep_n, gen), "bandpass", centre, q=3.5)
    dial_swish = dial_swish / max(float(np.std(dial_swish)), 1e-9) * 0.12
    dial_swish *= envelope.segments(sweep_n, [(0.0, 0.0), (0.12, 1.0), (0.48, 0.55), (0.65, 0.0)], shape="smooth")
    place(mix, dial_swish, 0)

    # Warm heterodyne carrier glide (D4 -> A4) locking into three valve tones (A4 -> D5 -> F#5).
    carrier = osc.sine(sweep_n, osc.glide(sweep_n, note_freq("D4"), note_freq("A4"), time_constant=0.20))
    carrier *= envelope.segments(sweep_n, [(0.0, 0.0), (0.10, 0.35), (0.50, 0.45), (0.65, 0.0)], shape="smooth")
    place(mix, carrier, 0)

    steps = (
        (0.16, "A4", 0.70, 0.55),
        (0.40, "D5", 0.84, 0.65),
        (0.66, "F#5", 0.90, 0.58),
    )
    for when, note, gain, dec in steps:
        tone = valve_tone(note_freq(note), 0.66, gen, attack_s=0.012, decay_s=dec, warble_cents=8.0)
        place(mix, gain * tone, samples(when))

    place(mix, 0.42 * valve_tone(note_freq("D4"), 0.72, gen, attack_s=0.015, decay_s=0.58, warble_cents=5.0),
          samples(0.40))
    return _valve_radio_chain(mix, gen, hiss_level=0.025, drive_db=4.6, lp_hz=2850.0)
