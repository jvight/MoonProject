"""Refinement C3 ("Lullaby radio"): short pentatonic music-box and celesta phrases heard through the old radio,
with very few noise elements — the calmest and most musical of the three refinements.

Refines round-1 C ("Old Radio") per owner feedback:
- Replaces electronic oscillator pips and switch noise with a warm **celesta** (felt-struck resonator plates over
  wooden cavities with a +2.4c detuned shimmer twin and bell-like 2x/3x/4x overtones) blended with a **plucked
  vintage music-box comb** (soft waveguide tines and crystalline pentatonic upper-octave grace notes).
- Very few noise elements: no switch clatter, no static bursts, and a near-silent tube floor (~22 dB quieter than C).
- Every UI cue and 07 chirp is a short, unhurried D-major-pentatonic lullaby phrase or chordal grace motif heard
  through warm vacuum tubes, wooden cabinet formants, gentle tape wow, and a calm chamber reverb.
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono
from synth.pluck import karplus_strong


def _lullaby_radio_chain(x: np.ndarray, gen, hiss_level: float = 0.0012, drive_db: float = 1.8,
                         lp_hz: float = 3450.0, hp_hz: float = 145.0,
                         room: float = 0.48, wet: float = 0.22) -> np.ndarray:
    """Passes a musical celesta/music-box signal through a quiet, warm vintage tube receiver and wooden cabinet."""
    n = x.shape[0]
    dur = n / SAMPLE_RATE

    # 1. Near-silent tube bias floor (virtually no hiss, just a trace of D2/D3 filament warmth).
    hiss_raw = noise.pink(n, gen)
    hiss_band = filters.bandpass(hiss_raw, 680.0, 1.2)
    hiss_std = max(float(np.std(hiss_band)), 1e-9)
    filament = 0.25 * osc.sine(n, note_freq("D2")) + 0.14 * osc.sine(n, note_freq("D3"))
    bed_env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.035, dur * 0.25), 1.0),
         (max(min(0.035, dur * 0.25) + 0.010, dur * 0.80), 0.65),
         (dur, 0.0)],
        shape="smooth",
    )
    bed = (hiss_band / hiss_std * hiss_level + 0.0020 * filament) * bed_env

    # 2. Gentle triode saturation for warm 2nd-order harmonic bloom.
    biased = x + bed + 0.016 * bed_env
    drive = 10.0 ** (drive_db / 20.0)
    saturated = filters.dc_block(effects.saturate(biased, drive=drive), 20.0)

    # 3. Warm wooden tabletop radio & music-box soundboard formants (380 Hz box, 960 Hz soundboard, 1850 Hz bell air).
    cab = (
        0.58 * saturated
        + 0.16 * filters.bandpass(saturated, 380.0, 2.0)
        + 0.26 * filters.bandpass(saturated, 960.0, 2.2)
        + 0.30 * filters.bandpass(saturated, 1850.0, 2.4)
    )

    # 4. Dreamy lullaby tape cradle (gentle wow, minimal flutter).
    if n > 64:
        cab = effects.wow_flutter(cab, wow_cents=2.4, wow_rate=0.55,
                                  flutter_cents=0.6, flutter_rate=5.4)

    # 5. Smooth old-radio passband (preserves gentle celesta bell harmonics up to ~3.4 kHz without harshness).
    band = filters.highpass(filters.lowpass(filters.lowpass(cab, lp_hz, q=0.65), lp_hz, q=0.65), hp_hz)
    return to_mono(effects.reverb(band, room=room, damping=0.72, wet=wet, dry=1.0, predelay=0.012))


def celesta_note(freq: float, duration: float, gen, attack_s: float = 0.010,
                 decay_s: float = 0.64, shimmer_cents: float = 2.4,
                 muted: bool = False) -> np.ndarray:
    """Felt-struck celesta bar over a wooden resonator cavity: warm fundamental, detuned shimmer twin, octave box
    resonance, and bell-like 3x/4x celesta overtones (zero scratchy noise)."""
    n = samples(duration)
    t60 = decay_s if not muted else min(decay_s, 0.18)

    # Fundamental + detuned celesta twin for gentle beating shimmer.
    twin_hz = freq * (2.0 ** (shimmer_cents / 1200.0))
    p1 = 0.78 * osc.sine(n, freq) * envelope.ar(n, attack_s, t60, shape="smooth")
    p1_twin = 0.24 * osc.sine(n, twin_hz, phase=0.19) * envelope.ar(n, attack_s * 1.1, t60 * 0.90, shape="smooth")

    # Wooden resonator box 2nd harmonic (octave) — strong celesta bell warmth.
    p2 = (0.48 if not muted else 0.26) * osc.sine(n, freq * 2.0, phase=0.12) * envelope.ar(
        n, attack_s * 0.9, t60 * 0.72, shape="smooth"
    )

    # Signature celesta steel-plate bell overtones (3.0x twelfth and 4.0x double-octave).
    p3 = np.zeros(n)
    if freq * 3.0 < 3600.0:
        amp3 = 0.28 if not muted else 0.12
        p3 = amp3 * osc.sine(n, freq * 3.0, phase=0.33) * envelope.ar(n, attack_s * 0.8, t60 * 0.48, shape="smooth")
    p4 = np.zeros(n)
    if freq * 4.0 < 3800.0 and not muted:
        p4 = 0.18 * osc.sine(n, freq * 4.0, phase=0.51) * envelope.ar(n, attack_s * 0.7, t60 * 0.34, shape="smooth")

    sig = p1 + p1_twin + p2 + p3 + p4
    return envelope.fade_out(sig, min(0.030, duration * 0.22))


def music_box_tine(freq: float, duration: float, gen, attack_s: float = 0.008,
                   decay_s: float = 0.58, muted: bool = False) -> np.ndarray:
    """Gentle clockwork music-box comb tine on a wooden soundbox: clean, low-noise waveguide pluck blended with
    pure damped tine modes."""
    n = samples(duration)
    t60 = decay_s if not muted else min(decay_s, 0.15)

    # Soft waveguide tine (clean pluck without scratchy noise burst).
    wg = karplus_strong(
        n, freq, gen,
        decay=0.996 if not muted else 0.982,
        brightness=0.36 if not muted else 0.20,
        pick_softness=1850.0,
    ) * envelope.ar(n, attack_s, t60, shape="smooth")
    wg_fund = filters.bandpass(wg, freq, 14.0)

    # Pure music-box fundamental + strong octave + bell chime overtones (3.0x and 4.0x).
    pure_fund = 0.72 * osc.sine(n, freq) * envelope.ar(n, attack_s, t60, shape="smooth")
    pure_oct = (0.44 if not muted else 0.24) * osc.sine(n, freq * 2.0, phase=0.21) * envelope.ar(
        n, attack_s * 0.85, t60 * 0.68, shape="smooth"
    )
    tine_twelfth = np.zeros(n)
    if freq * 3.0 < 3500.0:
        tine_twelfth = (0.24 if not muted else 0.10) * osc.sine(n, freq * 3.0, phase=0.41) * envelope.ar(
            n, attack_s * 0.75, t60 * 0.42, shape="smooth"
        )
    tine_chime = np.zeros(n)
    if freq * 4.0 < 3700.0 and not muted:
        tine_chime = 0.15 * osc.sine(n, freq * 4.0, phase=0.58) * envelope.ar(
            n, attack_s * 0.65, t60 * 0.28, shape="smooth"
        )

    mix = 0.36 * wg + 0.42 * wg_fund + pure_fund + pure_oct + tine_twelfth + tine_chime
    return envelope.fade_out(filters.lowpass(mix, 3550.0 if not muted else 2450.0), min(0.028, duration * 0.22))


def lullaby_voice(freq: float, duration: float, gen, attack_s: float = 0.009,
                  decay_s: float = 0.62, celesta_weight: float = 0.56,
                  muted: bool = False) -> np.ndarray:
    """Combined celesta & music-box lullaby voice with a shimmering octave music-box overtone."""
    cel = celesta_note(freq, duration, gen, attack_s=attack_s, decay_s=decay_s, muted=muted)
    mb = music_box_tine(freq, duration, gen, attack_s=max(0.006, attack_s * 0.85), decay_s=decay_s * 0.92, muted=muted)
    voice = celesta_weight * cel + (1.0 - celesta_weight) * mb
    # Add a delicate upper-octave music-box sympathetic tine (2.0 * freq) for authentic celesta/music-box sparkle.
    if freq * 2.0 <= 2400.0 and not muted:
        oct_tine = music_box_tine(freq * 2.0, duration, gen, attack_s=attack_s * 1.1, decay_s=decay_s * 0.58)
        voice = voice + 0.34 * oct_tine
    return voice


def lullaby_chirp_phrase(syllables: tuple, gen, harmony_notes: tuple = (),
                         sag_filter: bool = False, warble_cents: float = 4.0) -> np.ndarray:
    """07's lullaby-radio voice: a clockwork music-box & celesta melody accompanied by a soft, breath-free vacuum-tube
    carrier halo in D major pentatonic."""
    last_onset, _, last_dur, _, _ = syllables[-1]
    total_s = last_onset + last_dur + 0.30
    n = samples(total_s)
    mix = np.zeros(n)

    # 1. Soft sustained tube-celesta carrier halo gliding gently underneath the music-box notes.
    freq_track = np.full(n, note_freq(syllables[0][1]), dtype=np.float64)
    halo_gate = np.zeros(n, dtype=np.float64)
    prev_hz = note_freq(syllables[0][1])

    for onset, note, length, bend, gain in syllables:
        start = samples(onset)
        hz = note_freq(note)

        # Articulated celesta + music-box note for each syllable.
        note_dur = min(total_s - onset, length + 0.24)
        chime = lullaby_voice(hz, note_dur, gen, attack_s=0.014, decay_s=length * 1.65, celesta_weight=0.58)
        place(mix, 0.84 * gain * chime, start)

        # If the syllable has an upward bend (e.g. curious), add a delicate music-box grace tine on the next pentatonic step.
        if bend > 0.5:
            grace_hz = hz * (2.0 ** (2.0 / 12.0))
            grace_start = min(n - 64, start + samples(length * 0.46))
            grace_dur = max(0.12, (n - grace_start) / SAMPLE_RATE)
            grace = music_box_tine(grace_hz, grace_dur, gen, attack_s=0.012, decay_s=length * 1.05)
            place(mix, 0.34 * gain * grace, grace_start)

        # Update the soft tube carrier halo track.
        count = min(samples(length + 0.08), n - start)
        if count > 16:
            glide_n = min(count, samples(0.055))
            seg = np.full(count, hz, dtype=np.float64)
            seg[:glide_n] = osc.glide(glide_n, prev_hz, hz, time_constant=0.018)
            if bend < -0.2:
                tail = min(count, samples(min(length, 0.16)))
                seg[-tail:] *= 2.0 ** (np.linspace(0.0, bend * 0.40, tail) / 12.0)
            freq_track[start:start + count] = seg
            freq_track[start + count:] = seg[-1]
            prev_hz = float(seg[-1])

            s_env = envelope.segments(
                count,
                [(0.0, 0.0), (min(0.022, length * 0.3), gain),
                 (max(min(0.022, length * 0.3) + 0.005, length * 0.78), gain * 0.82),
                 (count / SAMPLE_RATE, 0.0)],
                shape="smooth",
            )
            halo_gate[start:start + count] = np.maximum(halo_gate[start:start + count], s_env)

    t = np.arange(n) / SAMPLE_RATE
    f_halo = freq_track * (2.0 ** (warble_cents * np.sin(2.0 * math.pi * 4.6 * t) / 1200.0))
    halo = (
        0.68 * osc.sine(n, f_halo)
        + 0.32 * osc.sine(n, f_halo * 2.0, phase=0.16)
    ) * halo_gate
    mix += 0.30 * halo

    # 2. Optional soft harmony notes (celesta accompaniment).
    for h_onset, h_note, h_dur, h_gain in harmony_notes:
        h_sig = celesta_note(note_freq(h_note), min(total_s - h_onset, h_dur), gen,
                             attack_s=0.014, decay_s=h_dur * 0.82)
        place(mix, h_gain * h_sig, samples(h_onset))

    if sag_filter:
        cutoff = np.linspace(2650.0, 1550.0, n)
        mix = filters.swept(mix, "lowpass", cutoff, q=0.707)

    return _lullaby_radio_chain(mix, gen, hiss_level=0.0012, drive_db=1.8, lp_hz=3400.0, room=0.48, wet=0.23)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: delicate music-box tine & celesta touch on A4 with a faint D5 fifth shimmer (no click)."""
    n = samples(0.32)
    mix = np.zeros(n)
    place(mix, 0.22 * celesta_note(note_freq("D5"), 0.24, gen, attack_s=0.008, decay_s=0.14), 0)
    place(mix, 0.88 * lullaby_voice(note_freq("A4"), 0.29, gen, attack_s=0.008, decay_s=0.19, celesta_weight=0.52),
          samples(0.010))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.6, lp_hz=3350.0, room=0.40, wet=0.18)


def place_card(gen) -> np.ndarray:
    """Place card shows: clockwork music-box & celesta lullaby phrase (A4 F#4 A4 -> D5) harmonized over D4 & F#4."""
    n = samples(1.72)
    mix = np.zeros(n)

    phrase = (
        (0.02, "A4", 0.72, 0.64),
        (0.19, "F#4", 0.64, 0.60),
        (0.36, "A4", 0.70, 0.64),
        (0.54, "D5", 0.88, 0.96),
    )
    for when, note, gain, dec in phrase:
        voice = lullaby_voice(note_freq(note), 1.08, gen, attack_s=0.010, decay_s=dec, celesta_weight=0.56)
        place(mix, gain * voice, samples(when))

    # Gentle celesta accompaniment chord (D4 + A4 + F#5 shimmer) blooming under the melody.
    place(mix, 0.30 * celesta_note(note_freq("D4"), 1.05, gen, attack_s=0.018, decay_s=0.72), samples(0.02))
    place(mix, 0.34 * celesta_note(note_freq("A4"), 1.08, gen, attack_s=0.020, decay_s=0.82), samples(0.54))
    place(mix, 0.26 * music_box_tine(note_freq("F#5"), 0.96, gen, attack_s=0.014, decay_s=0.68), samples(0.56))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0012, drive_db=1.8, lp_hz=3450.0, room=0.50, wet=0.24)


def tab_open(gen) -> np.ndarray:
    """Tab opens: 3-note rising music-box & celesta grace arpeggio (D4 -> F#4 -> A4) through the warm radio."""
    n = samples(0.70)
    mix = np.zeros(n)
    place(mix, 0.62 * lullaby_voice(note_freq("D4"), 0.56, gen, attack_s=0.009, decay_s=0.38, celesta_weight=0.55),
          samples(0.008))
    place(mix, 0.56 * lullaby_voice(note_freq("F#4"), 0.52, gen, attack_s=0.009, decay_s=0.36, celesta_weight=0.55),
          samples(0.056))
    place(mix, 0.90 * lullaby_voice(note_freq("A4"), 0.58, gen, attack_s=0.010, decay_s=0.44, celesta_weight=0.58),
          samples(0.106))
    place(mix, 0.26 * music_box_tine(note_freq("D5"), 0.50, gen, attack_s=0.012, decay_s=0.36), samples(0.112))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.7, lp_hz=3400.0, room=0.45, wet=0.21)


def tab_close(gen) -> np.ndarray:
    """Tab closes: 3-note descending celesta & music-box cadence (A4 -> F#4 -> D4) resting on a soft A4/D4 chord."""
    n = samples(0.68)
    mix = np.zeros(n)
    place(mix, 0.68 * lullaby_voice(note_freq("A4"), 0.50, gen, attack_s=0.009, decay_s=0.34, celesta_weight=0.56),
          samples(0.006))
    place(mix, 0.54 * lullaby_voice(note_freq("F#4"), 0.48, gen, attack_s=0.009, decay_s=0.32, celesta_weight=0.56),
          samples(0.052))
    place(mix, 0.88 * lullaby_voice(note_freq("D4"), 0.56, gen, attack_s=0.010, decay_s=0.42, celesta_weight=0.60),
          samples(0.100))
    place(mix, 0.28 * celesta_note(note_freq("A4"), 0.50, gen, attack_s=0.014, decay_s=0.36), samples(0.104))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.7, lp_hz=3300.0, room=0.45, wet=0.21)


def item_pick(gen) -> np.ndarray:
    """Item picked: delicate two-note music-box & celesta lift (E4 -> A4) with zero switch noise."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.68 * lullaby_voice(note_freq("E4"), 0.24, gen, attack_s=0.008, decay_s=0.16, celesta_weight=0.50),
          samples(0.005))
    place(mix, 0.88 * lullaby_voice(note_freq("A4"), 0.28, gen, attack_s=0.008, decay_s=0.20, celesta_weight=0.54),
          samples(0.060))
    place(mix, 0.24 * music_box_tine(note_freq("E5"), 0.22, gen, attack_s=0.009, decay_s=0.15), samples(0.064))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.6, lp_hz=3400.0, room=0.40, wet=0.19)


def item_drop(gen) -> np.ndarray:
    """Item dropped: gentle two-note celesta & music-box settle (A4 -> D4 with D5 harmonic chime)."""
    n = samples(0.40)
    mix = np.zeros(n)
    place(mix, 0.54 * lullaby_voice(note_freq("A4"), 0.24, gen, attack_s=0.008, decay_s=0.15, celesta_weight=0.54),
          samples(0.004))
    place(mix, 0.88 * lullaby_voice(note_freq("D4"), 0.34, gen, attack_s=0.009, decay_s=0.24, celesta_weight=0.58),
          samples(0.036))
    place(mix, 0.30 * celesta_note(note_freq("D5"), 0.30, gen, attack_s=0.012, decay_s=0.20), samples(0.038))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.7, lp_hz=3300.0, room=0.42, wet=0.20)


def item_refuse(gen) -> np.ndarray:
    """Refused: soft, felt-muted celesta & music-box two-note sigh falling B4 -> F#4 (calm, never scolding)."""
    n = samples(0.38)
    mix = np.zeros(n)
    place(mix, 0.74 * lullaby_voice(note_freq("B4"), 0.20, gen, attack_s=0.010, decay_s=0.14,
                                    celesta_weight=0.60, muted=False), samples(0.005))
    place(mix, 0.88 * lullaby_voice(note_freq("F#4"), 0.26, gen, attack_s=0.010, decay_s=0.18,
                                    celesta_weight=0.62, muted=False), samples(0.096))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.6, lp_hz=3000.0, room=0.38, wet=0.18)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: crystalline yet warm 3-note music-box & celesta grace motif (E4 -> F#4 -> A4)."""
    n = samples(0.66)
    mix = np.zeros(n)
    place(mix, 0.54 * lullaby_voice(note_freq("E4"), 0.50, gen, attack_s=0.009, decay_s=0.34, celesta_weight=0.52),
          samples(0.006))
    place(mix, 0.48 * lullaby_voice(note_freq("F#4"), 0.48, gen, attack_s=0.009, decay_s=0.32, celesta_weight=0.52),
          samples(0.038))
    place(mix, 0.88 * lullaby_voice(note_freq("A4"), 0.56, gen, attack_s=0.010, decay_s=0.44, celesta_weight=0.56),
          samples(0.074))
    place(mix, 0.26 * music_box_tine(note_freq("E5"), 0.48, gen, attack_s=0.011, decay_s=0.34), samples(0.078))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0010, drive_db=1.7, lp_hz=3400.0, room=0.46, wet=0.22)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

LULLABY_CHIRPS = {
    "curious": (
        (0.00, "D4", 0.15, 0.0, 0.74),
        (0.17, "F#4", 0.15, 0.0, 0.84),
        (0.34, "A4", 0.26, 2.0, 0.96),
    ),
    "affirm": (
        (0.00, "A4", 0.13, 0.0, 0.82),
        (0.14, "D5", 0.22, 0.0, 0.96),
    ),
    "proud": (
        (0.00, "D4", 0.13, 0.0, 0.72),
        (0.15, "E4", 0.13, 0.0, 0.78),
        (0.30, "F#4", 0.13, 0.0, 0.84),
        (0.45, "A4", 0.15, 0.0, 0.90),
        (0.62, "D5", 0.32, 0.0, 1.00),
    ),
    "worried": (
        (0.00, "B4", 0.21, 0.0, 0.88),
        (0.22, "F#4", 0.32, -0.5, 0.78),
    ),
    "relieved": (
        (0.00, "F#4", 0.20, 0.0, 0.80),
        (0.22, "D4", 0.24, 0.0, 0.76),
        (0.48, "A4", 0.32, 0.0, 0.96),
    ),
}


def chirp_curious(gen) -> np.ndarray:
    return lullaby_chirp_phrase(
        LULLABY_CHIRPS["curious"], gen,
        harmony_notes=((0.34, "D5", 0.42, 0.28), (0.34, "F#4", 0.42, 0.24)),
        warble_cents=4.0,
    )


def chirp_affirm(gen) -> np.ndarray:
    return lullaby_chirp_phrase(
        LULLABY_CHIRPS["affirm"], gen,
        harmony_notes=((0.14, "F#4", 0.38, 0.28), (0.14, "A4", 0.40, 0.28)),
        warble_cents=3.5,
    )


def chirp_proud(gen) -> np.ndarray:
    return lullaby_chirp_phrase(
        LULLABY_CHIRPS["proud"], gen,
        harmony_notes=((0.30, "D4", 0.55, 0.26), (0.62, "A4", 0.52, 0.34), (0.62, "F#5", 0.48, 0.26)),
        warble_cents=4.0,
    )


def chirp_worried(gen) -> np.ndarray:
    return lullaby_chirp_phrase(
        LULLABY_CHIRPS["worried"], gen,
        harmony_notes=((0.22, "D4", 0.45, 0.28), (0.22, "B4", 0.40, 0.22)),
        sag_filter=True,
        warble_cents=8.0,
    )


def chirp_relieved(gen) -> np.ndarray:
    return lullaby_chirp_phrase(
        LULLABY_CHIRPS["relieved"], gen,
        harmony_notes=((0.48, "D4", 0.52, 0.32), (0.48, "F#4", 0.50, 0.28)),
        warble_cents=3.8,
    )


def scan_complete(gen) -> np.ndarray:
    """A scan completes: delicate ascending pentatonic music-box glissando (D4 E4 F#4 A4) blossoming into a glowing
    celesta & music-box lullaby chord (A4 -> D5 -> F#5 over D4) with almost no noise."""
    n = samples(1.46)
    mix = np.zeros(n)

    # Soft clockwork music-box glissando replacing the noisy AM dial static sweep.
    gliss = (
        (0.01, "D4", 0.32, 0.36),
        (0.08, "E4", 0.34, 0.36),
        (0.15, "F#4", 0.38, 0.40),
    )
    for when, note, gain, dec in gliss:
        tine = music_box_tine(note_freq(note), 0.55, gen, attack_s=0.008, decay_s=dec)
        place(mix, gain * tine, samples(when))

    # Main 3-note celesta & music-box lullaby resolve (A4 -> D5 -> F#5).
    steps = (
        (0.22, "A4", 0.72, 0.66),
        (0.45, "D5", 0.86, 0.76),
        (0.70, "F#5", 0.90, 0.70),
    )
    for when, note, gain, dec in steps:
        voice = lullaby_voice(note_freq(note), 0.75, gen, attack_s=0.010, decay_s=dec, celesta_weight=0.58)
        place(mix, gain * voice, samples(when))

    # Warm D4 + A4 celesta cradle under the resolve.
    place(mix, 0.36 * celesta_note(note_freq("D4"), 0.82, gen, attack_s=0.018, decay_s=0.68), samples(0.45))
    place(mix, 0.30 * celesta_note(note_freq("A4"), 0.80, gen, attack_s=0.020, decay_s=0.68), samples(0.45))
    return _lullaby_radio_chain(mix, gen, hiss_level=0.0012, drive_db=1.8, lp_hz=3500.0, room=0.50, wet=0.24)
