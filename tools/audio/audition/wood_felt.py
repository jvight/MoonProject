"""Direction B ("Wood & Felt"): warm acoustic sound design for UI cues and 07's voice.

Built from muted rosewood marimba bars struck with soft wool-yarn mallets, thumb-pad box kalimbas with cedar cavity
resonances, hollow maple/walnut wood taps, tactile cotton-paper and wool-felt slides for UI movements, and 07's
chirps voiced on a breathy wooden ocarina (vessel flute) with air chiff and a warm woody body.
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono
from synth.pluck import karplus_strong


def _acoustic_room(x: np.ndarray, room: float = 0.34, wet: float = 0.15) -> np.ndarray:
    """Warm, felt-damped wooden chamber reflection."""
    return to_mono(effects.reverb(x, room=room, damping=0.74, wet=wet, dry=1.0, predelay=0.008))


def _wood_body_filter(x: np.ndarray) -> np.ndarray:
    """Applies coupled hollow cedar/maple soundbox formants (250 Hz, 540 Hz, 1180 Hz)."""
    b1 = filters.bandpass(x, 250.0, 2.4)
    b2 = filters.bandpass(x, 540.0, 2.8)
    b3 = filters.bandpass(x, 1180.0, 3.0)
    return 0.68 * x + 0.32 * b1 + 0.24 * b2 + 0.14 * b3


def paper_cloth(duration: float, gen, kind: str = "paper") -> np.ndarray:
    """Tactile friction of thick cotton rag paper ('paper') or brushed wool felt ('cloth')."""
    n = samples(duration)
    raw = noise.pink(n, gen)
    if kind == "paper":
        # Heavy cotton paper: mid-band rustle with subtle grain micro-texture, rolled off above 3.6 kHz.
        band = filters.bandpass(raw, 1350.0, 1.3) + 0.45 * filters.bandpass(raw, 720.0, 1.6)
        flutter = envelope.lfo(n, 14.0, 0.22, 0.78, phase=float(gen.uniform(0.0, 1.0)))
        shaped = filters.lowpass(band * flutter, 3400.0)
    else:
        # Brushed wool felt: darker, velvety friction centred at 480..950 Hz.
        band = filters.bandpass(raw, 520.0, 1.4) + 0.55 * filters.bandpass(raw, 940.0, 1.5)
        shaped = filters.lowpass(band, 2400.0)

    env = envelope.segments(
        n,
        [(0.0, 0.0), (min(0.025, duration * 0.3), 1.0),
         (max(min(0.025, duration * 0.3) + 0.005, duration * 0.72), 0.65),
         (duration, 0.0)],
        shape="smooth",
    )
    sig = shaped * env
    std = max(float(np.std(sig)), 1e-9)
    return filters.highpass(sig / std * 0.12, 85.0)


def wood_tap(freq: float, duration: float, gen, decay: float = 0.09,
             muted: bool = False) -> np.ndarray:
    """Hollow maple/walnut woodblock or wooden token tap excited by a cushioned skin/felt pad."""
    n = samples(duration)
    # Cushioned skin/felt contact burst (no sharp digital click).
    contact = filters.lowpass(filters.highpass(noise.pink(n, gen), 80.0), 1400.0 if not muted else 900.0)
    contact *= envelope.ar(n, 0.0022, 0.012)

    # Coupled wood-cavity modes (1.0, 1.47, 2.12) + low table thump at 130 Hz.
    t60 = decay if not muted else min(decay, 0.045)
    q_base = 18.0 if not muted else 9.0
    cavity = np.zeros(n)
    for ratio, q_scale, dec_scale, weight in (
        (1.00, 1.0, 1.00, 1.00),
        (1.47, 0.85, 0.65, 0.52),
        (2.12, 0.70, 0.42, 0.24 if not muted else 0.08),
    ):
        m_hz = min(freq * ratio, 3600.0)
        mode = filters.bandpass(contact, m_hz, q_base * q_scale) * envelope.ar(n, 0.002, t60 * dec_scale)
        cavity += weight * mode

    # Gentle pitch settle on the fundamental wood mode (starts 3% higher on impact and relaxes over 8 ms).
    f_glide = osc.glide(n, freq * 1.03, freq, time_constant=0.007)
    wood_core = osc.sine(n, f_glide) * envelope.ar(n, 0.0025, t60)
    table_thud = filters.bandpass(contact, 135.0, 2.2) * envelope.ar(n, 0.0025, 0.038)

    mix = _wood_body_filter(1.4 * cavity + 0.38 * wood_core + 0.55 * table_thud)
    return filters.lowpass(filters.highpass(mix, 65.0), 3400.0 if not muted else 2200.0)


def marimba_bar(freq: float, duration: float, gen, decay: float = 0.58,
                muted: bool = False) -> np.ndarray:
    """Rosewood marimba bar struck with a soft wool-yarn mallet over a cedar resonator tube."""
    n = samples(duration)
    t60 = decay if not muted else min(decay, 0.16)

    # Soft wool-yarn mallet thump (6 ms cushioned attack).
    yarn = filters.lowpass(filters.highpass(noise.pink(n, gen), 70.0), 1100.0)
    yarn *= envelope.ar(n, 0.0045, 0.022)
    resonator_puff = filters.bandpass(yarn, freq, 8.0) * envelope.ar(n, 0.004, t60 * 0.7)

    # Arch-cut rosewood bar: fundamental (1.0x) and double-octave second mode (3.98x, fast decay).
    f0 = osc.glide(n, freq * 1.008, freq, time_constant=0.009)
    fundamental = osc.sine(n, f0) * envelope.ar(n, 0.0045, t60)
    warm_second = osc.sine(n, freq * 2.0, phase=0.18) * envelope.ar(n, 0.0045, t60 * 0.48)
    arch_mode = np.zeros(n)
    if freq * 3.98 < 4200.0 and not muted:
        arch_mode = osc.sine(n, freq * 3.98, phase=0.37) * envelope.ar(n, 0.003, t60 * 0.20)

    bar = 0.85 * fundamental + 0.16 * warm_second + 0.11 * arch_mode + 0.65 * resonator_puff + 0.22 * yarn
    return filters.lowpass(_wood_body_filter(bar), 3800.0 if not muted else 2400.0)


def felt_kalimba(freq: float, duration: float, gen, decay: float = 0.68) -> np.ndarray:
    """Box kalimba plucked with the soft pad of the thumb, coupled to a hollow gourd/wood soundbox."""
    n = samples(duration)
    # Warm low-brightness waveguide tine.
    tine = karplus_strong(
        n, freq, gen,
        decay=0.994,
        brightness=0.25,
        pick_softness=1350.0,
    ) * envelope.ar(n, 0.004, decay)

    # Fundamental gourd reinforcement + soft thumb-pad wood knock.
    gourd = osc.sine(n, freq) * envelope.ar(n, 0.005, decay * 0.95)
    thumb = wood_tap(min(freq * 0.5, 320.0), min(duration, 0.18), gen, decay=0.05)
    mix = 0.55 * tine + 0.45 * gourd
    place(mix, 0.25 * thumb, 0)
    return filters.lowpass(_wood_body_filter(mix), 3800.0)


def ocarina_phrase(syllables: tuple, gen, tremolo_depth: float = 0.0,
                   vibrato_cents: float = 9.0) -> np.ndarray:
    """Breathy clay/boxwood ocarina (vessel flute) phrase with edge-tone chiff and woody cavity resonance.

    ``syllables`` is a sequence of ``(onset_s, note, duration_s, bend_semitones, gain)``.
    """
    last_onset, _, last_dur, _, _ = syllables[-1]
    total_s = last_onset + last_dur + 0.24
    n = samples(total_s)
    mix = np.zeros(n)

    for idx, (onset, note, length, bend, gain) in enumerate(syllables):
        s_len = samples(length + 0.08)
        hz = note_freq(note)
        t = np.arange(s_len) / SAMPLE_RATE

        # Gentle breath-pressure pitch rise on attack (~10 cents over 20 ms) + optional tail bend.
        attack_settle = 1.0 - 0.006 * np.exp(-t / 0.018)
        f_curve = np.full(s_len, hz, dtype=np.float64) * attack_settle
        if abs(bend) > 1e-4:
            u = np.clip((t - length * 0.35) / max(length * 0.65, 0.02), 0.0, 1.0) ** 1.35
            f_curve *= 2.0 ** (bend * u / 12.0)

        # Delayed organic breath vibrato (fades in smoothly after 65 ms).
        vib_env = np.clip((t - 0.04) / 0.08, 0.0, 1.0)
        f_curve *= 2.0 ** (vibrato_cents * vib_env * np.sin(2.0 * math.pi * 5.2 * t) / 1200.0)

        # Vessel flute standing wave: predominantly fundamental + soft 2nd harmonic + trace 3rd harmonic.
        vessel = (
            0.82 * osc.sine(s_len, f_curve)
            + 0.14 * osc.sine(s_len, f_curve * 2.0, phase=0.21)
            + 0.03 * osc.sine(s_len, f_curve * 3.0, phase=0.47)
        )

        # Fipple edge-tone breath turbulence tracking the pitch (jet-drive coupling).
        air_raw = noise.pink(s_len, gen)
        jet_noise = (
            0.70 * filters.swept(air_raw, "bandpass", np.clip(f_curve, 160.0, 2400.0), q=3.2)
            + 0.30 * filters.swept(air_raw, "bandpass", np.clip(f_curve * 1.5, 240.0, 3000.0), q=2.4)
        )
        jet_std = max(float(np.std(jet_noise)), 1e-9)
        jet_noise = jet_noise / jet_std * (0.20 + 0.08 * vessel)

        # Soft onset chiff (air catching the labium lip over the first 22 ms).
        chiff = filters.bandpass(air_raw, min(hz * 1.8, 2200.0), 2.2) * envelope.ar(s_len, 0.004, 0.022)
        chiff = chiff / max(float(np.std(chiff)), 1e-9) * 0.08

        # Soft breath envelope (18 ms rounded attack, gentle release).
        breath_env = envelope.segments(
            s_len,
            [(0.0, 0.0), (min(0.020, length * 0.28), 1.0),
             (max(min(0.020, length * 0.28) + 0.005, length * 0.78), 0.82),
             (length + 0.08, 0.0)],
            shape="smooth",
        )
        note_sig = (vessel + jet_noise + chiff) * breath_env
        if tremolo_depth > 0.0:
            note_sig = effects.tremolo(note_sig, rate=6.8, depth=tremolo_depth)

        place(mix, gain * _wood_body_filter(note_sig), samples(onset))
        if idx == 0:
            # Tiny wooden finger-hole / pad tap at phrase start.
            place(mix, 0.16 * wood_tap(hz * 0.5, 0.09, gen, decay=0.04, muted=True), samples(onset))

    return filters.lowpass(filters.highpass(mix, 95.0), 3800.0)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: whisper-quiet cotton-paper slip + a muted maple tap on A4."""
    n = samples(0.28)
    mix = np.zeros(n)
    place(mix, 0.52 * paper_cloth(0.11, gen, kind="paper"), 0)
    place(mix, 0.78 * wood_tap(note_freq("A4"), 0.20, gen, decay=0.075), samples(0.014))
    place(mix, 0.42 * marimba_bar(note_freq("A4"), 0.22, gen, decay=0.14, muted=True), samples(0.016))
    return _acoustic_room(filters.lowpass(mix, 3600.0), room=0.22, wet=0.10)


def place_card(gen) -> np.ndarray:
    """Place card shows: heavy card unfolding on felt + rosewood marimba/kalimba phrase (A4 F#4 A4 -> D5 over D3)."""
    n = samples(1.65)
    mix = np.zeros(n)
    place(mix, 0.35 * paper_cloth(0.22, gen, kind="paper"), 0)
    place(mix, 0.28 * paper_cloth(0.26, gen, kind="cloth"), samples(0.04))

    phrase = (
        (0.03, "A4", 0.72, 0.55),
        (0.19, "F#4", 0.64, 0.52),
        (0.35, "A4", 0.68, 0.55),
        (0.52, "D5", 0.84, 0.82),
    )
    for when, note, gain, dec in phrase:
        hz = note_freq(note)
        voice = 0.58 * marimba_bar(hz, 0.95, gen, decay=dec) + 0.42 * felt_kalimba(hz, 0.95, gen, decay=dec * 1.1)
        place(mix, gain * voice, samples(when))

    # Warm D3 marimba bass cushion under the resolving D5.
    place(mix, 0.52 * marimba_bar(note_freq("D3"), 1.05, gen, decay=0.85), samples(0.52))
    return _acoustic_room(filters.lowpass(mix, 3900.0), room=0.38, wet=0.18)


def tab_open(gen) -> np.ndarray:
    """Tab opens: wool-felt tray sliding open + warm rising rosewood marimba D4 -> A4."""
    n = samples(0.66)
    mix = np.zeros(n)
    place(mix, 0.38 * paper_cloth(0.20, gen, kind="cloth"), 0)
    place(mix, 0.76 * marimba_bar(note_freq("D4"), 0.50, gen, decay=0.36), samples(0.015))
    place(mix, 0.88 * marimba_bar(note_freq("A4"), 0.54, gen, decay=0.42), samples(0.085))
    place(mix, 0.35 * felt_kalimba(note_freq("A4"), 0.50, gen, decay=0.38), samples(0.088))
    return _acoustic_room(filters.lowpass(mix, 3600.0), room=0.30, wet=0.14)


def tab_close(gen) -> np.ndarray:
    """Tab closes: felt tray sliding shut + settling marimba A4 -> D4 + soft wooden rest on D3."""
    n = samples(0.62)
    mix = np.zeros(n)
    place(mix, 0.35 * paper_cloth(0.18, gen, kind="cloth"), 0)
    place(mix, 0.80 * marimba_bar(note_freq("A4"), 0.46, gen, decay=0.32), samples(0.010))
    place(mix, 0.86 * marimba_bar(note_freq("D4"), 0.50, gen, decay=0.38), samples(0.082))
    place(mix, 0.42 * wood_tap(note_freq("D3"), 0.24, gen, decay=0.08, muted=True), samples(0.090))
    return _acoustic_room(filters.lowpass(mix, 3400.0), room=0.28, wet=0.13)


def item_pick(gen) -> np.ndarray:
    """Item picked: light cloth lift + hollow wooden token knock rising E4 -> A4."""
    n = samples(0.32)
    mix = np.zeros(n)
    place(mix, 0.36 * paper_cloth(0.10, gen, kind="cloth"), 0)
    place(mix, 0.72 * wood_tap(note_freq("E4"), 0.20, gen, decay=0.08), samples(0.005))
    place(mix, 0.84 * wood_tap(note_freq("A4"), 0.22, gen, decay=0.095), samples(0.058))
    place(mix, 0.38 * felt_kalimba(note_freq("A4"), 0.24, gen, decay=0.16), samples(0.060))
    return _acoustic_room(filters.lowpass(mix, 3600.0), room=0.24, wet=0.11)


def item_drop(gen) -> np.ndarray:
    """Item dropped: cushioned wool-felt thud + seated rosewood knock on D4."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.38 * paper_cloth(0.10, gen, kind="cloth"), 0)
    place(mix, 0.68 * wood_tap(note_freq("D3"), 0.20, gen, decay=0.07, muted=True), 0)
    place(mix, 0.86 * wood_tap(note_freq("D4"), 0.28, gen, decay=0.11), samples(0.012))
    place(mix, 0.50 * marimba_bar(note_freq("D4"), 0.30, gen, decay=0.20, muted=True), samples(0.014))
    return _acoustic_room(filters.lowpass(mix, 3400.0), room=0.25, wet=0.12)


def item_refuse(gen) -> np.ndarray:
    """Refused: two palm-muted hollow wood taps falling B4 -> F#4 with a short felt brush."""
    n = samples(0.35)
    mix = np.zeros(n)
    place(mix, 0.32 * paper_cloth(0.12, gen, kind="cloth"), 0)
    place(mix, 0.78 * wood_tap(note_freq("B4"), 0.16, gen, decay=0.055, muted=True), samples(0.004))
    place(mix, 0.82 * wood_tap(note_freq("F#4"), 0.20, gen, decay=0.065, muted=True), samples(0.094))
    return _acoustic_room(filters.lowpass(mix, 2600.0), room=0.20, wet=0.09)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: delicate paper rustle + thumb-pad kalimba grace note E4 -> A4."""
    n = samples(0.60)
    mix = np.zeros(n)
    place(mix, 0.30 * paper_cloth(0.12, gen, kind="paper"), 0)
    place(mix, 0.58 * felt_kalimba(note_freq("E4"), 0.46, gen, decay=0.34), samples(0.006))
    place(mix, 0.84 * felt_kalimba(note_freq("A4"), 0.52, gen, decay=0.42), samples(0.044))
    return _acoustic_room(filters.lowpass(mix, 3600.0), room=0.30, wet=0.14)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

WOOD_FELT_CHIRPS = {
    "curious": (
        (0.00, "D4", 0.14, 0.0, 0.72),
        (0.16, "F#4", 0.14, 0.0, 0.82),
        (0.32, "A4", 0.24, 2.0, 0.95),
    ),
    "affirm": (
        (0.00, "A4", 0.12, 0.0, 0.80),
        (0.13, "D5", 0.20, 0.0, 0.95),
    ),
    "proud": (
        (0.00, "D4", 0.13, 0.0, 0.70),
        (0.15, "E4", 0.13, 0.0, 0.76),
        (0.30, "F#4", 0.13, 0.0, 0.82),
        (0.45, "A4", 0.15, 0.0, 0.88),
        (0.62, "D5", 0.30, 0.0, 1.00),
    ),
    "worried": (
        (0.00, "B4", 0.20, 0.0, 0.88),
        (0.21, "F#4", 0.30, -1.0, 0.78),
    ),
    "relieved": (
        (0.00, "F#4", 0.19, 0.0, 0.80),
        (0.21, "D4", 0.23, 0.0, 0.75),
        (0.46, "A4", 0.30, 0.0, 0.95),
    ),
}


def chirp_curious(gen) -> np.ndarray:
    phrase = ocarina_phrase(WOOD_FELT_CHIRPS["curious"], gen, vibrato_cents=10.0)
    return _acoustic_room(phrase, room=0.34, wet=0.16)


def chirp_affirm(gen) -> np.ndarray:
    phrase = ocarina_phrase(WOOD_FELT_CHIRPS["affirm"], gen, vibrato_cents=7.0)
    return _acoustic_room(phrase, room=0.32, wet=0.14)


def chirp_proud(gen) -> np.ndarray:
    phrase = ocarina_phrase(WOOD_FELT_CHIRPS["proud"], gen, vibrato_cents=9.0)
    # Warm thumb-kalimba D4 resonance blooming under the final D5 note.
    place(phrase, 0.38 * felt_kalimba(note_freq("D4"), 0.45, gen, decay=0.36), samples(0.62))
    return _acoustic_room(filters.lowpass(phrase, 3800.0), room=0.36, wet=0.17)


def chirp_worried(gen) -> np.ndarray:
    phrase = ocarina_phrase(WOOD_FELT_CHIRPS["worried"], gen, tremolo_depth=0.22, vibrato_cents=18.0)
    return _acoustic_room(phrase, room=0.32, wet=0.15)


def chirp_relieved(gen) -> np.ndarray:
    phrase = ocarina_phrase(WOOD_FELT_CHIRPS["relieved"], gen, vibrato_cents=8.0)
    place(phrase, 0.32 * marimba_bar(note_freq("D3"), 0.48, gen, decay=0.38), samples(0.46))
    return _acoustic_room(filters.lowpass(phrase, 3600.0), room=0.35, wet=0.16)


def scan_complete(gen) -> np.ndarray:
    """A scan completes: brushed-felt sweep across wooden bars into a warm marimba + ocarina D major resolve."""
    n = samples(1.38)
    mix = np.zeros(n)
    place(mix, 0.42 * paper_cloth(0.55, gen, kind="cloth"), 0)

    # Rising rosewood marimba + kalimba steps (A4 -> D5 -> F#5) with a warm ocarina breath on D5.
    steps = (
        (0.14, "A4", 0.70, 0.55),
        (0.38, "D5", 0.84, 0.65),
        (0.64, "F#5", 0.90, 0.60),
    )
    for when, note, gain, dec in steps:
        hz = note_freq(note)
        bar = 0.55 * marimba_bar(hz, 0.65, gen, decay=dec) + 0.45 * felt_kalimba(hz, 0.65, gen, decay=dec)
        place(mix, gain * bar, samples(when))

    breath = ocarina_phrase(((0.0, "D5", 0.42, 0.0, 0.52),), gen, vibrato_cents=8.0)
    place(mix, breath, samples(0.38))
    place(mix, 0.45 * marimba_bar(note_freq("D4"), 0.72, gen, decay=0.58), samples(0.38))
    return _acoustic_room(filters.lowpass(mix, 4000.0), room=0.38, wet=0.18)
