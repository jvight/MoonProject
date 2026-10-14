"""Direction A ("Workshop"): physical, mechanical sound design for UI cues and 07's voice.

Every sound is built from physical excitations (felt-damped impacts, spring-latch double-catches, paper/felt friction,
and commutator-modulated noise) driving waveguide strings/tines (``karplus_strong``) and multi-modal resonant filter
banks (``filters.bandpass`` / ``filters.swept``) in a close workshop room. No pure sine, square, saw, or triangle
audio oscillators are used anywhere in this module.
"""
import math

import numpy as np

from synth import effects, envelope, filters, noise
from synth.core import SAMPLE_RATE, note_freq, place, samples, to_mono
from synth.pluck import karplus_strong

# D major pentatonic ladder across the warm workshop octaves (D3 .. A5).
PENTA_WORKSHOP = ("D3", "E3", "F#3", "A3", "B3", "D4", "E4", "F#4", "A4", "B4", "D5", "E5", "F#5", "A5")


def _workshop_room(x: np.ndarray, room: float = 0.30, wet: float = 0.14) -> np.ndarray:
    """Close, warm wooden-and-metal workshop room reflection."""
    return to_mono(effects.reverb(x, room=room, damping=0.68, wet=wet, dry=1.0, predelay=0.006))


def _commutator_pulse(n: int, rate_hz: float | np.ndarray, duty: float = 0.45) -> np.ndarray:
    """Soft mechanical gear/commutator modulation envelope (0.35 .. 1.0) derived from a rounded phase wrap."""
    if np.ndim(rate_hz) == 0:
        phase = (np.arange(n, dtype=np.float64) * (float(rate_hz) / SAMPLE_RATE)) % 1.0
    else:
        phase = np.cumsum(np.asarray(rate_hz, dtype=np.float64) / SAMPLE_RATE) % 1.0
    window = np.exp(-((phase - duty) / 0.22) ** 2)
    return 0.45 + 0.55 * window


def _felt_mallet_exciter(n: int, gen, attack_s: float = 0.003, decay_s: float = 0.018,
                         cutoff_hz: float = 1800.0) -> np.ndarray:
    """Cushioned felt-or-leather strike burst: shaped pink noise with a rounded transient."""
    raw = noise.pink(n, gen)
    shaped = filters.lowpass(filters.highpass(raw, 90.0), cutoff_hz)
    env = envelope.ar(n, attack_s, decay_s)
    return effects.saturate(shaped * env, drive=1.6)


def latch_click(duration: float, gen, base_hz: float = 440.0, double: bool = True,
                muted: bool = False) -> np.ndarray:
    """Physical spring-latch mechanism: pre-slip friction + pawl catch + housing modal ring."""
    n = samples(duration)
    out = np.zeros(n)

    # 1. Pre-slip felt/metal friction grain (first 12 ms).
    slip_n = min(n, samples(0.014))
    slip = filters.bandpass(noise.pink(slip_n, gen), 1150.0, 1.4)
    slip *= envelope.segments(slip_n, [(0.0, 0.0), (0.004, 1.0), (0.014, 0.0)], shape="smooth")
    place(out, 0.22 * slip, 0)

    # 2. Primary pawl impact and optional secondary seating catch.
    catches = [(0.003, 1.0, 1.0)]
    if double:
        catches.append((0.021, 0.68, 0.92))

    for onset_s, gain, pitch_scale in catches:
        start = samples(onset_s)
        rem = n - start
        if rem <= 16:
            continue
        imp = _felt_mallet_exciter(rem, gen, attack_s=0.0018, decay_s=0.011,
                                   cutoff_hz=1500.0 if muted else 2400.0)
        # Modal housing resonances (inexact mechanical ratios of a stamped bracket).
        modes = (
            (base_hz * pitch_scale, 14.0 if not muted else 7.0, 0.055 if not muted else 0.025, 1.0),
            (base_hz * pitch_scale * 1.58, 12.0 if not muted else 6.0, 0.038 if not muted else 0.018, 0.65),
            (base_hz * pitch_scale * 2.41, 10.0 if not muted else 5.0, 0.024 if not muted else 0.012, 0.35),
        )
        ring = np.zeros(rem)
        for m_hz, q, t60, weight in modes:
            if m_hz < 5200.0:
                mode_sig = filters.bandpass(imp, m_hz, q) * envelope.ar(rem, 0.0015, t60)
                ring += weight * mode_sig
        # Low padded chassis thump through the mount (~135 Hz).
        thud = filters.bandpass(imp, 145.0, 2.2) * envelope.ar(rem, 0.002, 0.035)
        place(out, gain * (ring + 0.55 * thud), start)

    return filters.lowpass(filters.highpass(out, 65.0), 4200.0 if muted else 5400.0)


def servo_whirr(duration: float, freq_start: float, freq_end: float, gen,
                attack_s: float = 0.025, release_s: float = 0.045) -> np.ndarray:
    """Felt-damped miniature gearmotor whirr: commutator-modulated noise through swept housing formants."""
    n = samples(duration)
    u = np.linspace(0.0, 1.0, n)
    s_curve = u * u * (3.0 - 2.0 * u)
    f_track = freq_start * (freq_end / freq_start) ** s_curve

    bed = noise.pink(n, gen)
    mod = _commutator_pulse(n, f_track, duty=0.48)
    excited = bed * mod

    # Broad felt-damped housing resonances tracking the gear rate and its 2nd/3rd harmonics.
    f1 = np.clip(f_track, 90.0, 2200.0)
    f2 = np.clip(f_track * 2.0, 160.0, 3200.0)
    f3 = np.clip(f_track * 3.0, 240.0, 3800.0)
    mode1 = filters.swept(excited, "bandpass", f1, q=3.0)
    mode2 = filters.swept(excited, "bandpass", f2, q=2.4)
    mode3 = filters.swept(excited, "bandpass", f3, q=2.0)

    # Soft felt-lined bearing friction around 620 Hz.
    bearing = filters.bandpass(bed, 620.0, 1.6)

    env = envelope.segments(
        n,
        [(0.0, 0.0), (min(attack_s, duration * 0.35), 1.0),
         (max(min(attack_s, duration * 0.35) + 0.005, duration - release_s), 0.78),
         (duration, 0.0)],
        shape="smooth",
    )
    raw = (0.9 * mode1 + 0.55 * mode2 + 0.25 * mode3 + 0.30 * bearing) * env
    std = max(float(np.std(raw)), 1e-9)
    return filters.lowpass(filters.highpass(raw / std * 0.042, 75.0), 3200.0)


def brass_washer(freq: float, duration: float, gen, decay: float = 0.55,
                 muted: bool = False) -> np.ndarray:
    """Small spun-brass washer / ring tuned to ``freq``: waveguide pluck coupled to circular-ring modes."""
    n = samples(duration)
    ks_decay = 0.995 if not muted else 0.978
    core = karplus_strong(
        n, freq, gen,
        decay=ks_decay,
        brightness=0.38 if not muted else 0.20,
        pick_softness=2200.0 if not muted else 1100.0,
    )
    core *= envelope.ar(n, 0.0025, decay if not muted else min(decay, 0.12))
    # Reinforce fundamental ring mode of the spun brass washer.
    fundamental_mode = filters.bandpass(core, freq, 14.0 if not muted else 7.0)

    # Circular brass washer modal resonances excited by a soft strike burst.
    strike = _felt_mallet_exciter(n, gen, attack_s=0.002, decay_s=0.014,
                                  cutoff_hz=2600.0 if not muted else 1300.0)
    modal = np.zeros(n)
    for ratio, q, t60_scale, amp in (
        (1.0, 32.0, 1.0, 1.35),
        (2.0, 22.0, 0.50, 0.25),
        (2.92, 18.0, 0.28, 0.07 if not muted else 0.03),
    ):
        m_hz = freq * ratio
        if m_hz < 5600.0:
            m = filters.bandpass(strike, m_hz, q if not muted else q * 0.45)
            m *= envelope.ar(n, 0.002, (decay if not muted else 0.09) * t60_scale)
            modal += amp * m

    bench = filters.bandpass(strike, 260.0, 2.5) * envelope.ar(n, 0.002, 0.045)
    mix = 0.60 * core + 1.15 * fundamental_mode + 1.25 * modal + 0.28 * bench
    return filters.lowpass(filters.highpass(mix, 80.0), 5800.0 if not muted else 2800.0)


def aluminium_tube(freq: float, duration: float, gen, decay: float = 0.85) -> np.ndarray:
    """Hollow aluminium chime tube struck with a felt hammer: warm fundamental + open-pipe air column."""
    n = samples(duration)
    hammer = _felt_mallet_exciter(n, gen, attack_s=0.004, decay_s=0.022, cutoff_hz=1900.0)

    tube_wg = karplus_strong(
        n, freq, gen,
        decay=0.9965,
        brightness=0.30,
        pick_softness=1600.0,
    ) * envelope.ar(n, 0.0045, decay)
    tube_fund = filters.bandpass(tube_wg, freq, 16.0)

    modal = np.zeros(n)
    for ratio, q, t60_scale, amp in (
        (0.5, 8.0, 0.16, 0.18),
        (1.0, 36.0, 1.0, 1.15),
        (2.0, 24.0, 0.60, 0.28),
        (2.76, 18.0, 0.32, 0.09),
    ):
        m_hz = freq * ratio
        if 75.0 <= m_hz <= 5200.0:
            m = filters.bandpass(hammer, m_hz, q) * envelope.ar(n, 0.004, decay * t60_scale)
            modal += amp * m

    mix = 0.60 * tube_wg + 1.10 * tube_fund + 1.30 * modal
    return filters.lowpass(filters.highpass(mix, 70.0), 5000.0)


def music_box_comb(freq: float, duration: float, gen, decay: float = 0.72,
                   bend_semitones: float = 0.0) -> np.ndarray:
    """Plucked phosphor-bronze music-box tine mounted on a brass bridge, with optional grace-tine inflection."""
    n = samples(duration)
    pin = _felt_mallet_exciter(n, gen, attack_s=0.0018, decay_s=0.010, cutoff_hz=2400.0)
    bridge = filters.bandpass(pin, 380.0, 3.2) * envelope.ar(n, 0.002, 0.04)

    wg = karplus_strong(
        n, freq, gen,
        decay=0.996,
        brightness=0.36,
        pick_softness=2100.0,
    ) * envelope.ar(n, 0.003, decay)

    body1 = filters.bandpass(wg + 0.35 * pin, freq, 18.0)
    body2 = filters.bandpass(wg + 0.20 * pin, min(freq * 2.0, 5200.0), 12.0)
    tine = 0.62 * wg + 0.95 * body1 + 0.22 * body2

    # Mechanical comb grace-tine inflection on the adjacent D-major-pentatonic tine when bend_semitones != 0.
    if abs(bend_semitones) > 1e-4:
        grace_ratio = 2.0 ** (2.0 / 12.0) if bend_semitones > 0 else 2.0 ** (-2.0 / 12.0)
        grace_hz = freq * grace_ratio
        grace_start = min(n - 32, samples(duration * 0.42))
        if grace_start > 0 and n - grace_start > 64:
            grace_wg = karplus_strong(n - grace_start, grace_hz, gen, decay=0.994,
                                      brightness=0.32, pick_softness=1800.0)
            grace_wg *= envelope.ar(n - grace_start, 0.004, decay * 0.55)
            place(tine, 0.32 * grace_wg, grace_start)

    return filters.lowpass(filters.highpass(tine + 0.24 * bridge, 85.0), 5400.0)


# --------------------------------------------------------------------------------------------------- UI Cues (8)

def ticker_line(gen) -> np.ndarray:
    """Ticker line arrives: felt-damped micro-escapement pawl click + a tiny brass washer ring on A4."""
    n = samples(0.28)
    mix = np.zeros(n)
    click = latch_click(0.16, gen, base_hz=note_freq("A4"), double=False)
    washer = brass_washer(note_freq("A4"), 0.24, gen, decay=0.16)
    place(mix, 0.55 * click, 0)
    place(mix, 0.72 * washer, samples(0.008))
    return _workshop_room(filters.lowpass(mix, 5000.0), room=0.22, wet=0.10)


def place_card(gen) -> np.ndarray:
    """Place card shows: brass nameplate sliding on felt + 4-note plucked comb phrase (A4 F#4 A4 -> D5 over D4)."""
    n = samples(1.65)
    mix = np.zeros(n)
    # Soft brass-on-felt rail slide at the start.
    slide_n = samples(0.16)
    slide = filters.bandpass(noise.pink(slide_n, gen), 920.0, 1.5)
    slide *= envelope.segments(slide_n, [(0.0, 0.0), (0.03, 1.0), (0.12, 0.45), (0.16, 0.0)], shape="smooth")
    place(mix, 0.14 * slide, 0)

    phrase = (
        (0.02, "A4", 0.72, 0.65),
        (0.18, "F#4", 0.62, 0.60),
        (0.34, "A4", 0.68, 0.65),
        (0.50, "D5", 0.85, 0.90),
        (0.50, "D4", 0.48, 0.95),
    )
    for when, note, gain, dec in phrase:
        tone = music_box_comb(note_freq(note), 1.05, gen, decay=dec)
        place(mix, gain * tone, samples(when))

    return _workshop_room(filters.lowpass(mix, 5200.0), room=0.36, wet=0.18)


def tab_open(gen) -> np.ndarray:
    """Tab opens: spring-latch catch releasing + rising felt-damped servo (D3->A3) + aluminium tubes D4 -> A4."""
    n = samples(0.68)
    mix = np.zeros(n)
    place(mix, 0.55 * latch_click(0.18, gen, base_hz=note_freq("D4"), double=True), 0)
    place(mix, 0.48 * servo_whirr(0.26, note_freq("D3"), note_freq("A3"), gen), samples(0.015))
    place(mix, 0.72 * aluminium_tube(note_freq("D4"), 0.52, gen, decay=0.38), samples(0.025))
    place(mix, 0.85 * aluminium_tube(note_freq("A4"), 0.56, gen, decay=0.44), samples(0.105))
    return _workshop_room(filters.lowpass(mix, 4800.0), room=0.28, wet=0.14)


def tab_close(gen) -> np.ndarray:
    """Tab closes: felt-damped servo easing down (A3->D3) + aluminium tubes A4 -> D4 + seated latch click."""
    n = samples(0.64)
    mix = np.zeros(n)
    place(mix, 0.45 * servo_whirr(0.24, note_freq("A3"), note_freq("D3"), gen), 0)
    place(mix, 0.78 * aluminium_tube(note_freq("A4"), 0.48, gen, decay=0.34), samples(0.01))
    place(mix, 0.82 * aluminium_tube(note_freq("D4"), 0.52, gen, decay=0.40), samples(0.09))
    place(mix, 0.58 * latch_click(0.20, gen, base_hz=note_freq("D4"), double=True), samples(0.10))
    return _workshop_room(filters.lowpass(mix, 4600.0), room=0.26, wet=0.13)


def item_pick(gen) -> np.ndarray:
    """Item picked: spring-clip release tick + rising two-step brass washer ping E4 -> A4."""
    n = samples(0.34)
    mix = np.zeros(n)
    place(mix, 0.45 * latch_click(0.14, gen, base_hz=note_freq("E4"), double=False), 0)
    place(mix, 0.68 * brass_washer(note_freq("E4"), 0.24, gen, decay=0.16), samples(0.005))
    place(mix, 0.82 * brass_washer(note_freq("A4"), 0.26, gen, decay=0.20), samples(0.062))
    return _workshop_room(filters.lowpass(mix, 5200.0), room=0.24, wet=0.11)


def item_drop(gen) -> np.ndarray:
    """Item dropped: padded socket clunk + seated brass washer ring on D4 + locking detent."""
    n = samples(0.38)
    mix = np.zeros(n)
    place(mix, 0.65 * latch_click(0.22, gen, base_hz=note_freq("D4"), double=True), 0)
    place(mix, 0.85 * brass_washer(note_freq("D4"), 0.32, gen, decay=0.24), samples(0.012))
    place(mix, 0.45 * aluminium_tube(note_freq("A3"), 0.28, gen, decay=0.18), samples(0.018))
    return _workshop_room(filters.lowpass(mix, 4600.0), room=0.25, wet=0.12)


def item_refuse(gen) -> np.ndarray:
    """Refused: two felt-choked, muted bracket taps falling B4 -> F#4 (damped metal that cannot ring)."""
    n = samples(0.36)
    mix = np.zeros(n)
    place(mix, 0.65 * latch_click(0.15, gen, base_hz=note_freq("B4"), double=False, muted=True), 0)
    place(mix, 0.72 * brass_washer(note_freq("B4"), 0.16, gen, decay=0.08, muted=True), samples(0.004))
    place(mix, 0.68 * latch_click(0.18, gen, base_hz=note_freq("F#4"), double=False, muted=True), samples(0.095))
    place(mix, 0.78 * brass_washer(note_freq("F#4"), 0.20, gen, decay=0.09, muted=True), samples(0.099))
    return _workshop_room(filters.lowpass(mix, 3400.0), room=0.20, wet=0.09)


def prompt_chip(gen) -> np.ndarray:
    """Prompt chip: small mechanical indicator flag flip + soft E4 -> A4 aluminium tube chime."""
    n = samples(0.62)
    mix = np.zeros(n)
    place(mix, 0.38 * latch_click(0.14, gen, base_hz=note_freq("E4"), double=False), 0)
    place(mix, 0.58 * aluminium_tube(note_freq("E4"), 0.48, gen, decay=0.34), samples(0.008))
    place(mix, 0.82 * aluminium_tube(note_freq("A4"), 0.54, gen, decay=0.42), samples(0.048))
    return _workshop_room(filters.lowpass(mix, 4800.0), room=0.30, wet=0.14)


# --------------------------------------------------------------------------------------------------- 07 Cues (6)

# Warm workshop-octave contours (one octave below the piercing D5..E6 chirps, staying strictly in D major pentatonic).
WORKSHOP_CHIRPS = {
    "curious": (
        # (onset_s, note, duration_s, bend_semitones, gain)
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
        (0.62, "D5", 0.32, 0.0, 1.00),
    ),
    "worried": (
        (0.00, "B4", 0.20, 0.0, 0.88),
        (0.21, "F#4", 0.30, -1.0, 0.78),
    ),
    "relieved": (
        (0.00, "F#4", 0.19, 0.0, 0.80),
        (0.21, "D4", 0.23, 0.0, 0.75),
        (0.46, "A4", 0.32, 0.0, 0.95),
    ),
}


def _render_workshop_chirp(mood: str, gen) -> np.ndarray:
    """07's mechanical voice: a tiny felt-damped servo governor breathing under plucked resonant metal tines."""
    contour = WORKSHOP_CHIRPS[mood]
    last_onset, _, last_dur, _, _ = contour[-1]
    total_s = last_onset + last_dur + 0.26
    n = samples(total_s)
    mix = np.zeros(n)

    # Continuous servo-governor "breath" spanning the phrase.
    f_start = note_freq(contour[0][1]) * 0.5
    f_end = note_freq(contour[-1][1]) * 0.5
    servo_dur = last_onset + last_dur + 0.08
    whirr = servo_whirr(servo_dur, f_start, f_end, gen, attack_s=0.02, release_s=0.06)
    place(mix, 0.42 * whirr, 0)

    for idx, (onset, note, length, bend, gain) in enumerate(contour):
        hz = note_freq(note)
        # Resonant comb tine + brass washer body for each syllable.
        tine = music_box_comb(hz, length + 0.22, gen, decay=length * 1.45, bend_semitones=bend)
        washer = brass_washer(hz, length + 0.22, gen, decay=length * 1.15)
        syllable = 0.65 * tine + 0.45 * washer

        # Worried mood adds a mechanical governor flutter via tremolo.
        if mood == "worried":
            syllable = effects.tremolo(syllable, rate=8.5, depth=0.28)

        place(mix, gain * syllable, samples(onset))
        if idx == 0:
            place(mix, 0.28 * latch_click(0.10, gen, base_hz=hz, double=False), samples(onset))

    # Proud adds a warm D4 sympathetic tube resonance on the final note.
    if mood == "proud":
        place(mix, 0.45 * aluminium_tube(note_freq("D4"), 0.50, gen, decay=0.38), samples(contour[-1][0]))

    return _workshop_room(filters.lowpass(mix, 5200.0), room=0.32, wet=0.15)


def chirp_curious(gen) -> np.ndarray:
    return _render_workshop_chirp("curious", gen)


def chirp_affirm(gen) -> np.ndarray:
    return _render_workshop_chirp("affirm", gen)


def chirp_proud(gen) -> np.ndarray:
    return _render_workshop_chirp("proud", gen)


def chirp_worried(gen) -> np.ndarray:
    return _render_workshop_chirp("worried", gen)


def chirp_relieved(gen) -> np.ndarray:
    return _render_workshop_chirp("relieved", gen)


def scan_complete(gen) -> np.ndarray:
    """A scan completes: lens iris leaves sliding shut + rising servo governor + A4 -> D5 -> F#5 brass/tube chime."""
    n = samples(1.38)
    mix = np.zeros(n)

    # Mechanical lens-iris blades sliding and clicking home.
    place(mix, 0.42 * latch_click(0.16, gen, base_hz=note_freq("A4"), double=True), 0)
    place(mix, 0.48 * servo_whirr(0.75, note_freq("D3"), note_freq("A3"), gen, attack_s=0.05, release_s=0.15),
          samples(0.02))

    # Three-step resonant metal resolve (A4 -> D5 -> F#5) with warm D4 tube body.
    steps = (
        (0.18, "A4", 0.72, 0.65),
        (0.42, "D5", 0.84, 0.72),
        (0.68, "F#5", 0.92, 0.62),
    )
    for when, note, gain, dec in steps:
        hz = note_freq(note)
        chime = 0.55 * aluminium_tube(hz, 0.68, gen, decay=dec) + 0.45 * brass_washer(hz, 0.68, gen, decay=dec * 0.85)
        place(mix, gain * chime, samples(when))

    place(mix, 0.42 * aluminium_tube(note_freq("D4"), 0.75, gen, decay=0.60), samples(0.42))
    return _workshop_room(filters.lowpass(mix, 5600.0), room=0.38, wet=0.18)
