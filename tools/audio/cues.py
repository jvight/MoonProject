"""Every sound cue of Lunarion as a deterministic recipe on the shared ``synth`` core.

A recipe returns one signal (mono ``(n,)`` or stereo ``(n, 2)``) per variant; ``build_sfx.py`` handles DC
removal, edge fades, loudness normalisation per category, the safety limiter and file output. All tonal
material is in D major pentatonic (D E F# A B). Loops are periodic by construction: whole-cycle frequencies
(``loop_freq``), LFO rates of k / loop length, events placed with wrap-around, and every filter - including the
pink/brown colouring of white noise - run through ``synth.loop.periodic``.
"""
import math
from dataclasses import dataclass

import numpy as np
from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, loop_freq, note_freq, pan, pentatonic, place, samples, to_mono
from synth.fm import two_op
from synth.loop import periodic

from sampled import base as sampled_base
from sampled import boot as sampled_boot
from sampled import rover as sampled_rover
from sampled import picks
import feel_cues as feel


@dataclass(frozen=True)
class Category:
    """Mixing family: output folder, loudness target/measurement and the runtime bus that plays it."""
    name: str
    folder: str
    target_lufs: float
    loudness_mode: str  # "momentary": loudest 200 ms window (one-shots); "integrated": BS.1770 gated (loops)
    bus: str
    spatial: bool


CATEGORIES = {
    c.name: c for c in (
        Category("oneshot_3d", "SFX", -18.0, "momentary", "Sfx", True),
        Category("loop_3d", "SFX", -22.0, "integrated", "Sfx", True),
        Category("ui_2d", "SFX/2D", -22.0, "momentary", "Sfx", False),
        Category("stinger_2d", "SFX/2D", -20.0, "momentary", "Sfx", False),
        Category("radio_2d", "SFX/2D", -24.0, "integrated", "Music", False),
        Category("radio_fx_2d", "SFX/2D", -24.0, "momentary", "Music", False),
        Category("ambience_2d", "Ambience", -30.0, "integrated", "Ambience", False),
        # Tiny transients (taps, detents) read loud for their energy: a lower target keeps their peaks soft.
        Category("tick_3d", "SFX", -26.0, "momentary", "Sfx", True),
        Category("tick_2d", "SFX/2D", -28.0, "momentary", "Sfx", False),
    )
}


@dataclass(frozen=True)
class Cue:
    id: str
    category: str
    render: object  # callable(variant: int, gen: numpy.random.Generator) -> signal
    notes: str
    loop: bool = False
    variants: int = 1
    file_stem: str | None = None
    volume: tuple = (1.0, 1.0)
    pitch: tuple = (1.0, 1.0)
    hf_cutoff: float = 8000.0
    hf_max_db: float = -30.0
    fade_out: float = 0.02
    milestone: str = "M1"
    variant_labels: tuple = ()
    tonal: bool = False  # the dominant pitch must be D major pentatonic (checked by analyze_audio)
    sampled: bool = False  # rendered from the external sound library (sampled/picks.py); needs --library

    def labels(self) -> list:
        """One label per variant: the given labels, else 01, 02... (a single variant is labelled with the id)."""
        if self.variants == 1:
            return [self.id]
        return list(self.variant_labels or tuple(f"{i + 1:02d}" for i in range(self.variants)))

    def files(self) -> list:
        """Paths relative to Assets/_Project/Audio."""
        stem = self.file_stem or self.id
        folder = CATEGORIES[self.category].folder
        if self.variants == 1:
            return [f"{folder}/{stem}.wav"]
        return [f"{folder}/{stem}_{label}.wav" for label in self.labels()]


def _mono_reverb(x: np.ndarray, **kwargs) -> np.ndarray:
    return to_mono(effects.reverb(x, **kwargs))


def _lognormal(gen, sigma: float) -> float:
    return math.exp(gen.normal(0.0, sigma))


def _grains(n: int, gen, rate: float, length, attack, t60, gain: float, sigma: float) -> np.ndarray:
    """Noise grains scattered over a loop (wrapping at the seam) with log-normal gains."""
    return instruments.scatter(
        n, gen, rate,
        lambda: instruments.grain(gen, gen.uniform(*length), gen.uniform(*attack), gen.uniform(*t60)),
        lambda: gain * _lognormal(gen, sigma))


# --------------------------------------------------------------------------------------------------- rover

HUM_LOOP_S = 4.0


def rover_hum(_variant, gen):
    """Warm electric hum on D2 with a rotation-synchronous whir. Every component completes whole cycles in the
    loop and nothing sits above ~3 kHz, so AudioSource.pitch 0.5..2 stays clean."""
    n = samples(HUM_LOOP_S)
    f0 = loop_freq(note_freq("D2"), n)
    beat = f0 + SAMPLE_RATE / n
    harmonics = [(1, 1.0), (2, 0.85), (3, 0.42), (4, 0.33), (5, 0.16), (6, 0.15), (7, 0.07), (8, 0.07),
                 (9, 0.035), (10, 0.03), (12, 0.02), (14, 0.012), (16, 0.008)]
    core = osc.additive(n, f0, harmonics)
    twin = osc.additive(n, beat, [(1, 0.35), (2, 0.25), (3, 0.08)], phase=0.31)
    whine = osc.additive(n, f0, [(12, 0.035), (16, 0.012)], phase=0.13)
    whine *= envelope.lfo(n, 12.0 / HUM_LOOP_S, 0.5, 0.5)
    whir = periodic(noise.white(n, gen), lambda s: filters.bandpass(s, 1200.0, 1.2))
    whir *= 0.06 * envelope.lfo(n, 2.0 * f0, 0.5, 0.5)
    mix = (core + twin + whine + whir) * envelope.lfo(n, 2.0 / HUM_LOOP_S, 0.06, 1.0)
    return periodic(mix, lambda s: filters.highpass(filters.lowpass(filters.lowpass(s, 3000.0), 3000.0), 35.0))


CRUNCH_LOOP_S = 4.0


def dust_crunch(_variant, gen):
    """Soft granular crunch of regolith under the wheels: many tiny crisp grains, fewer chunky ones and a low
    rolling bed."""
    n = samples(CRUNCH_LOOP_S)
    fine = _grains(n, gen, 95.0, (0.003, 0.009), (0.0004, 0.0009), (0.003, 0.008), 0.35, 0.55)
    coarse = _grains(n, gen, 24.0, (0.012, 0.03), (0.001, 0.002), (0.01, 0.025), 0.5, 0.5)
    fine = periodic(fine, lambda s: filters.lowpass(filters.lowpass(filters.bandpass(s, 2400.0, 0.8), 4500.0),
                                                    4500.0))
    coarse = periodic(coarse, lambda s: filters.lowpass(filters.bandpass(s, 700.0, 1.0), 3000.0))
    bed = periodic(noise.white(n, gen), lambda s: filters.highpass(
        filters.lowpass(filters.lowpass(noise.brown_filter(s, 0.995), 220.0), 220.0), 40.0))
    return fine + 0.8 * coarse + 0.5 * bed


CREAK_SETTINGS = (
    # resonator scale, stick-slip start/end interval (s), active duration (s), spring note
    (0.92, 0.0075, 0.016, 0.42, "F#3"),
    (1.00, 0.0065, 0.014, 0.36, "E3"),
    (1.10, 0.0085, 0.018, 0.48, "D3"),
)


def suspension_creak(variant, gen):
    """Soft rubbery creak: a stick-slip impulse train exciting low resonances (nothing above ~1.2 kHz, so it
    never squeals) over a quiet, slightly wobbling spring tone."""
    scale_r, start_gap, end_gap, duration, spring_note = CREAK_SETTINGS[variant]
    n = samples(duration + 0.25)
    active = samples(duration)
    excitation = np.zeros(n)
    t = samples(0.004)
    while t < active:
        u = t / active
        level = math.sin(math.pi * min(1.0, u * 1.6)) ** 1.5 if u < 0.625 else (1.0 - u) / 0.375
        amp = max(0.0, level) * gen.uniform(0.55, 1.0)
        excitation[t] += amp
        excitation[t + 1] += 0.6 * amp
        t += max(1, samples((start_gap + (end_gap - start_gap) * u) * gen.uniform(0.8, 1.2)))
    body = sum(weight * filters.bandpass(excitation, freq * scale_r, q)
               for freq, q, weight in ((380.0, 9.0, 1.0), (640.0, 7.0, 0.55), (1050.0, 6.0, 0.25)))
    spring = osc.sine(n, osc.vibrato(n, note_freq(spring_note), 9.0, 20.0)) * envelope.ar(n, 0.012, 0.32)
    return filters.highpass(filters.lowpass(filters.lowpass(body + 0.08 * spring, 2400.0), 2400.0), 90.0)


THUMP_SETTINGS = (
    # glide start/end Hz, whump T60, dust T60, dust level (relative)
    (98.0, 47.0, 0.45, 1.0, 0.30),
    (90.0, 44.0, 0.52, 1.2, 0.26),
    (104.0, 50.0, 0.40, 0.9, 0.34),
)


def landing_thump(variant, gen):
    """Soft whump (descending sine + low body noise + a little mid thud) with a slow dust hiss settling."""
    f_start, f_end, whump_t60, dust_t60, dust_level = THUMP_SETTINGS[variant]
    n = samples(1.5)
    whump = osc.sine(n, osc.glide(n, f_start, f_end, time_constant=0.06)) * envelope.ar(n, 0.005, whump_t60)
    body = filters.lowpass(filters.lowpass(noise.brown(n, gen), 180.0), 180.0) * envelope.ar(n, 0.004, 0.25)
    thud = filters.bandpass(noise.white(n, gen), 330.0, 0.9) * envelope.ar(n, 0.003, 0.12)
    dust = filters.lowpass(filters.lowpass(filters.highpass(noise.pink(n, gen), 1500.0), 4800.0), 4800.0)
    dust = dust / max(float(np.std(dust)), 1e-12) * envelope.ar(n, 0.07, dust_t60)
    return whump + 0.9 * body + 0.22 * thud + 0.05 * dust_level * dust


# --------------------------------------------------------------------------------------------------- radio

STATIC_LOOP_S = 6.0


def radio_static(_variant, gen):
    """Warm AM/vinyl static: band-limited hiss with a slow flutter, a resonant 'AM band' shimmer, soft vinyl
    crackle and a little low rumble. Stereo, independent noise per side."""
    n = samples(STATIC_LOOP_S)
    sides = []
    for side in range(2):
        hiss = periodic(noise.white(n, gen), lambda s: filters.lowpass(
            filters.lowpass(filters.highpass(s, 250.0), 2600.0), 3200.0))
        band = periodic(noise.white(n, gen), lambda s: filters.bandpass(s, 1000.0, 2.5))
        flutter = (envelope.lfo(n, 3.0 / STATIC_LOOP_S, 0.2, 0.7, phase=0.16 * side)
                   + envelope.lfo(n, 7.0 / STATIC_LOOP_S, 0.1, 0.0, phase=0.32 * side))
        crackle = _grains(n, gen, 9.0, (0.0006, 0.002), (0.0002, 0.0002), (0.0005, 0.0015), 0.6, 0.7)
        crackle = periodic(crackle, lambda s: filters.lowpass(
            filters.lowpass(filters.bandpass(s, 1500.0, 0.9), 3500.0), 3500.0))
        rumble = periodic(noise.white(n, gen), lambda s: filters.highpass(
            filters.lowpass(noise.brown_filter(s, 0.996), 150.0), 30.0))
        mix = 0.5 * hiss * flutter + 0.45 * band * flutter + 1.6 * crackle + 0.25 * rumble
        sides.append(periodic(mix, lambda s: filters.lowpass(filters.lowpass(s, 3600.0), 3600.0)))
    return np.stack(sides, axis=1)


def radio_tune(_variant, gen):
    """Turning the dial between two stations: static swells in while a soft resonant band sweeps up and back
    down, then settles away. Plays under the crossfade between radio tracks."""
    n = samples(1.8)
    centre = np.interp(np.arange(n), [0, samples(0.6), samples(1.1), n - 1], [500.0, 1700.0, 900.0, 700.0])
    env = envelope.segments(n, [(0.0, 0.0), (0.35, 1.0), (1.25, 0.8), (1.8, 0.0)], shape="smooth")
    sides = []
    for side in range(2):
        hiss = filters.lowpass(filters.highpass(noise.white(n, gen), 250.0), 3000.0)
        sweep = filters.swept(noise.white(n, gen), "bandpass", centre * (1.0 + 0.03 * side), q=4.0)
        sides.append(filters.lowpass(filters.lowpass((0.35 * hiss + 1.3 * sweep) * env, 3200.0), 3200.0))
    return np.stack(sides, axis=1)


# ---------------------------------------------------------------------------------------------- recovery & salvage


RECOVERY_LOOP_S = 4.0


def recovery_lift(_variant, gen):
    """07 being lifted to safety: a soft servo whir on D3/A3 (triangle-like partials, gentle 6 Hz motor flutter)
    with a breath of air under it. Seamless 4 s loop; the runtime glides its pitch up a whole tone (D to E) over the
    lift."""
    n = samples(RECOVERY_LOOP_S)
    whir = np.zeros(n)
    for note, amp in (("D3", 1.0), ("A3", 0.5), ("D4", 0.22)):
        f = loop_freq(note_freq(note), n)
        whir += amp * osc.additive(n, f, [(1, 1.0), (3, 0.11), (5, 0.04)], phase=0.17 * amp)
    whir *= envelope.lfo(n, 24.0 / RECOVERY_LOOP_S, 0.12, 0.88)
    air = periodic(noise.white(n, gen), lambda s: filters.bandpass(noise.pink_filter(s), 1400.0, 0.8))
    air *= envelope.lfo(n, 2.0 / RECOVERY_LOOP_S, 0.25, 0.75)
    mix = 0.6 * whir + 1.4 * air
    return periodic(mix, lambda s: filters.highpass(filters.lowpass(s, 3200.0), 60.0))


def recovery_settle(_variant, gen):
    """The lift sets 07 down: a soft air release falling away and a gentle A3 -> D3 servo settle."""
    n = samples(1.4)
    sigh = filters.swept(noise.pink(n, gen), "bandpass", osc.glide(n, 1500.0, 300.0), q=1.2)
    sigh *= envelope.segments(n, [(0.0, 0.0), (0.05, 1.0), (0.5, 0.35), (1.4, 0.0)], shape="smooth")
    servo = osc.additive(n, osc.glide(n, note_freq("A3"), note_freq("D3"), time_constant=0.12),
                         [(1, 1.0), (2, 0.2), (3, 0.08)])
    servo *= envelope.ar(n, 0.02, 0.7)
    body = osc.sine(n, note_freq("D2")) * envelope.ar(n, 0.01, 0.25)
    return filters.lowpass(1.8 * sigh + 0.35 * servo + 0.3 * body, 4000.0)


SALVAGE_CHIME_NOTES = 8
CHIME_BRIGHT_LIMIT_HZ = 1000.0


def salvage_chime(variant, gen):
    """A piece folding into 07's cargo: a soft felt fold and a glassy chime on degree ``variant`` of D major
    pentatonic from D5 (D5 E5 F#5 A5 B5 D6 E6 F#6), so a chain of pieces at one site climbs. Above ~1 kHz the bell
    partials are tapered so the top notes stay as soft as the low ones."""
    freq = pentatonic(5, variant)
    n = samples(1.4)
    if freq <= CHIME_BRIGHT_LIMIT_HZ:
        dry = instruments.glass_chime(freq, 1.4, decay=1.15)
    else:
        taper = CHIME_BRIGHT_LIMIT_HZ / freq
        dry = (instruments.partial(n, freq, 1.0, 0.0012, 1.15)
               + instruments.partial(n, freq, 0.25, 0.0012, 0.92, detune_cents=2.0)
               + instruments.partial(n, 2.76 * freq, 0.20 * taper, 0.0012, 0.32)
               + instruments.partial(n, 5.40 * freq, 0.06 * taper * taper, 0.0012, 0.1))
    fold = filters.lowpass(filters.lowpass(noise.white(n, gen), 700.0), 700.0) * envelope.ar(n, 0.003, 0.05)
    fold = fold / max(float(np.max(np.abs(fold))), 1e-9) * 0.08
    return _mono_reverb(dry + fold, room=0.5, damping=0.55, wet=0.2, dry=1.0)


# --------------------------------------------------------------------------------------------------- UI

def _rolled(notes, gap: float, duration: float, gen, decay: float, brightness: float, gains=None) -> np.ndarray:
    """Felt-piano notes rolled upward by ``gap`` seconds each (a soft, unhurried chord)."""
    n = samples(duration)
    mix = np.zeros(n)
    for k, note in enumerate(notes):
        tone = instruments.felt_piano(note_freq(note), duration, gen, decay=decay, brightness=brightness)
        place(mix, tone, samples(gap * k), gains[k] if gains else 1.0)
    return mix


UI_HOLD_RISE_S = 0.6


def ui_hold_fill(_variant, gen):
    """Holding to buy: a gentle swell (D5 + A5 shimmer with a breath of air) rising over the UI's 0.6 s hold
    and gliding the last few cents into tune exactly as the ring fills. The runtime stops it on release."""
    n = samples(UI_HOLD_RISE_S + 0.3)
    rise = samples(UI_HOLD_RISE_S)
    curve = np.minimum(1.0, np.arange(n) / rise)
    env = curve ** 2.2 * envelope.segments(n, [(0.0, 1.0), (UI_HOLD_RISE_S, 1.0), (UI_HOLD_RISE_S + 0.3, 0.0)],
                                           shape="smooth")
    cents = -25.0 * (1.0 - curve)
    tone = np.zeros(n)
    for note, amp in (("D5", 1.0), ("A5", 0.55), ("D6", 0.18)):
        tone += amp * osc.sine(n, note_freq(note) * 2.0 ** (cents / 1200.0))
    air = filters.swept(noise.pink(n, gen), "bandpass", osc.glide(n, 700.0, 2400.0), q=1.2)
    mix = (tone + 0.9 * air / max(float(np.std(air)), 1e-9) * 0.05) * env
    return filters.lowpass(effects.tremolo(mix, 7.0, 0.12), 5000.0)


def ui_hold_complete(_variant, gen):
    """The ring fills: a soft resolved D major chord (felt piano D4 F#4 A4 D5, rolled quickly) with a faint
    music-box D6 on top, in a small warm room. The upgrade's own arpeggio rides over it."""
    n = samples(1.8)
    mix = _rolled(("D4", "F#4", "A4", "D5"), 0.018, 1.8, gen, decay=1.5, brightness=0.45,
                  gains=(0.75, 0.6, 0.6, 0.55))
    place(mix, instruments.music_box(note_freq("D6"), 1.5, decay=1.0), samples(0.06), 0.18)
    wet = effects.reverb(filters.lowpass(mix, 4500.0)[:n], room=0.5, damping=0.6, wet=0.2, dry=0.0, width=0.8)
    return filters.lowpass(mix, 4500.0)[:, None] * np.array([[1.0, 1.0]]) + wet


# --------------------------------------------------------------------------------------------------- solitude (M3-10)

ROOM_TONE_LOOP_S = 24.0


def room_tone(_variant, gen):
    """The sound of the space around 07 far from home: a wide, very soft, dark air (decorrelated low noise in each
    ear, breathing very slowly) with a faint D2/A2 resonance far under it. Nothing much above ~1.2 kHz, so it never
    tires the ear the way hiss does. 24 s seamless stereo."""
    n = samples(ROOM_TONE_LOOP_S)
    loop_hz = 1.0 / ROOM_TONE_LOOP_S
    step = SAMPLE_RATE / n
    d2 = loop_freq(note_freq("D2"), n)
    a2 = loop_freq(note_freq("A2"), n)
    sides = []
    for side in range(2):
        air = periodic(noise.white(n, gen), lambda x: filters.highpass(filters.lowpass(filters.lowpass(
            noise.pink_filter(x), 420.0), 420.0), 40.0), warmup=samples(2.0))
        air = air / max(float(np.std(air)), 1e-9)
        sheen = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 850.0, 0.7),
                         warmup=samples(2.0))
        sheen = sheen / max(float(np.std(sheen)), 1e-9) * envelope.lfo(n, loop_hz, 0.5, 0.5, phase=0.5 * side)
        breath = (envelope.lfo(n, 2.0 * loop_hz, 0.12, 0.88, phase=0.31 * side)
                  + envelope.lfo(n, 3.0 * loop_hz, 0.05, 0.0, phase=0.6 + 0.2 * side))
        resonance = (osc.sine(n, d2 + side * step, phase=0.1 + 0.3 * side)
                     + 0.6 * osc.sine(n, a2 - side * step, phase=0.4 + 0.2 * side))
        resonance *= envelope.lfo(n, 2.0 * loop_hz, 0.4, 0.6, phase=0.15 + 0.5 * side)
        mix = (air + 0.1 * sheen) * breath + 0.12 * resonance
        sides.append(periodic(mix, lambda x: filters.lowpass(filters.lowpass(x, 1200.0), 1200.0)))
    return np.stack(sides, axis=1)


LAMP_HUM_LOOP_S = 4.0


def rover_lamp_hum(_variant, gen):
    """07's lamp: a tiny warm electrical hum on D3 with its octave and fifth, a whisper of filament buzz and a
    slow shimmer as the current breathes. Seamless, very soft; heard mostly when everything else is quiet."""
    n = samples(LAMP_HUM_LOOP_S)
    step = SAMPLE_RATE / n
    d3 = loop_freq(note_freq("D3"), n)
    hum = osc.additive(n, d3, [(1, 1.0), (2, 0.45), (3, 0.18), (4, 0.06)])
    hum += 0.3 * osc.sine(n, 2.0 * d3 + step, phase=0.25)
    shimmer = envelope.lfo(n, 3.0 / LAMP_HUM_LOOP_S, 0.15, 0.85)
    buzz = periodic(noise.white(n, gen), lambda x: filters.bandpass(x, 2.0 * d3 * 4.0, 6.0), warmup=samples(0.5))
    buzz = buzz / max(float(np.std(buzz)), 1e-9) * 0.015 * envelope.lfo(n, 2.0 * d3, 0.5, 0.5) ** 4
    mix = 0.3 * hum * shimmer + buzz
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 2500.0), 80.0))


SERVO_LOOP_S = 2.0


def rover_servo(_variant, gen):
    """A small servo working: a soft gear whirr (narrow noise bands with a faint whine riding on them) and the
    flutter of the gear teeth. Seamless; the runtime fades it with how fast the servo moves and lifts its pitch."""
    n = samples(SERVO_LOOP_S)
    loop_hz = 1.0 / SERVO_LOOP_S
    whirr = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 650.0, 2.5),
                     warmup=samples(0.5))
    whirr = whirr / max(float(np.std(whirr)), 1e-9)
    teeth = envelope.lfo(n, round(38.0 / loop_hz) * loop_hz, 0.25, 0.75)
    whine = osc.additive(n, loop_freq(1240.0, n), [(1, 1.0), (2, 0.2)])
    whine *= envelope.lfo(n, 3.0 * loop_hz, 0.3, 0.7)
    body = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 240.0, 1.5),
                    warmup=samples(0.5))
    body = body / max(float(np.std(body)), 1e-9)
    mix = 0.12 * whirr * teeth + 0.015 * whine + 0.05 * body
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 3500.0), 120.0))


METAL_TICK_NOTES = ("A5", "D6", "E6", "F#6", "B5")


def rover_metal_tick(variant, gen):
    """07's metal cooling after a drive: one tiny tick of a panel settling - a dry click and a faint, very short
    ring on a pentatonic note."""
    n = samples(0.2)
    freq = note_freq(METAL_TICK_NOTES[variant])
    click = filters.bandpass(noise.white(n, gen), 3200.0, 1.5) * envelope.ar(n, 0.0003, 0.004)
    click = click / max(float(np.max(np.abs(click))), 1e-9) * 0.2
    ring = (instruments.partial(n, freq, 1.0, 0.0006, 0.09)
            + instruments.partial(n, 2.76 * freq, 0.2, 0.0006, 0.03))
    return filters.lowpass(click + 0.35 * ring, 6000.0)


# --------------------------------------------------------------------------------------------------- salvage (M3-13)

SALVAGE_CUT_LOOP_S = 3.0


def _cut_sparks(n: int, gen, rate: float, centre: float) -> np.ndarray:
    """Soft spark ticks scattered over a loop (wrapping), band-limited so they never bite."""
    sparks = _grains(n, gen, rate, (0.0008, 0.003), (0.0002, 0.0004), (0.002, 0.006), 0.4, 0.7)
    return periodic(sparks, lambda x: filters.lowpass(filters.bandpass(x, centre, 0.9), 5000.0))


def salvage_cut_metal(_variant, gen):
    """07's beam cutting a metal plate: a low, soft grind that breathes with the beam, with sparks pattering over it.
    Gritty but never harsh; seamless."""
    n = samples(SALVAGE_CUT_LOOP_S)
    grind = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.brown_filter(x, 0.995), 220.0, 1.2))
    grind = grind / max(float(np.std(grind)), 1e-9) * envelope.lfo(n, 9.0 / SALVAGE_CUT_LOOP_S, 0.25, 0.75)
    body = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 650.0, 2.0))
    body = body / max(float(np.std(body)), 1e-9)
    sparks = _cut_sparks(n, gen, 26.0, 3000.0)
    mix = 0.5 * grind + 0.15 * body + 1.4 * sparks
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 5000.0), 60.0))


def salvage_cut_wiring(_variant, gen):
    """The beam working loose a cable bundle: dry crackle and the odd snap of a strand over a light, buzzy grind."""
    n = samples(SALVAGE_CUT_LOOP_S)
    crackle = _grains(n, gen, 90.0, (0.0004, 0.0015), (0.0001, 0.0002), (0.0005, 0.0015), 0.4, 0.4)
    crackle = periodic(crackle, lambda x: filters.lowpass(filters.bandpass(x, 2200.0, 0.8), 5000.0))
    snaps = _grains(n, gen, 6.0, (0.004, 0.008), (0.0002, 0.0003), (0.006, 0.012), 0.35, 0.2)
    snaps = periodic(snaps, lambda x: filters.lowpass(filters.bandpass(x, 1500.0, 1.2), 4500.0))
    buzz = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 420.0, 2.5))
    buzz = buzz / max(float(np.std(buzz)), 1e-9) * envelope.lfo(n, 12.0 / SALVAGE_CUT_LOOP_S, 0.3, 0.7)
    mix = 1.2 * crackle + 0.8 * snaps + 0.15 * buzz
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 5000.0), 90.0))


def salvage_cut_optics(_variant, gen):
    """The beam easing free a solar cell or lens: a glassy shimmer of tiny high ticks over a soft, airy hiss."""
    n = samples(SALVAGE_CUT_LOOP_S)
    glints = _grains(n, gen, 30.0, (0.001, 0.003), (0.0003, 0.0006), (0.01, 0.03), 0.35, 0.6)
    glints = periodic(glints, lambda x: filters.lowpass(filters.bandpass(x, 3400.0, 3.0), 5500.0))
    air = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 1600.0, 0.8))
    air = air / max(float(np.std(air)), 1e-9) * envelope.lfo(n, 6.0 / SALVAGE_CUT_LOOP_S, 0.3, 0.7)
    low = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.brown_filter(x, 0.995), 300.0, 1.2))
    low = low / max(float(np.std(low)), 1e-9)
    mix = 1.5 * glints + 0.08 * air + 0.12 * low
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 5500.0), 80.0))


CUT_TONE_LOOP_S = 2.0


def salvage_cut_tone(_variant, gen):
    """The beam's singing edge: a soft D4 whine (a touch of its octave and fifth, a slow shimmer) that the runtime
    steps up the pentatonic as the cut goes on. Seamless."""
    n = samples(CUT_TONE_LOOP_S)
    step = SAMPLE_RATE / n
    d4 = loop_freq(note_freq("D4"), n)
    tone = osc.additive(n, d4, [(1, 1.0), (2, 0.3), (3, 0.12)]) + 0.35 * osc.sine(n, d4 + step, phase=0.3)
    tone *= envelope.lfo(n, 4.0 / CUT_TONE_LOOP_S, 0.15, 0.85)
    return periodic(0.3 * tone, lambda x: filters.lowpass(x, 3000.0))


SALVAGE_BREAK_KINDS = ("metal", "wiring", "optics")


def salvage_break(variant, gen):
    """The piece breaking loose, by material: metal a soft muffled crack and a hollow clunk; wiring a snap with a
    little spring of the cable; optics a glassy crack with a tiny tinkle. Satisfying, never sharp."""
    n = samples(1.0)
    kind = SALVAGE_BREAK_KINDS[variant]
    crack = filters.bandpass(noise.white(n, gen), 1800.0, 1.0) * envelope.ar(n, 0.0008, 0.03)
    crack = crack / max(float(np.max(np.abs(crack))), 1e-9)
    mix = np.zeros(n)
    if kind == "metal":
        clunk = osc.sine(n, osc.glide(n, 180.0, 120.0, time_constant=0.04)) * envelope.ar(n, 0.003, 0.25)
        ring = instruments.partial(n, note_freq("A2"), 0.4, 0.004, 0.5) + instruments.partial(
            n, 2.0 * note_freq("A2"), 0.15, 0.004, 0.3)
        mix = 0.25 * filters.lowpass(crack, 2500.0) + 0.5 * clunk + 0.25 * ring
    elif kind == "wiring":
        t = np.arange(n) / SAMPLE_RATE
        spring_f = note_freq("D4") * (1.0 + 0.05 * np.exp(-t / 0.15) * np.sin(2.0 * math.pi * 11.0 * t))
        spring = osc.additive(n, spring_f, [(1, 1.0), (2, 0.2)]) * envelope.ar(n, 0.006, 0.35)
        snap = effects.saturate(filters.lowpass(crack, 3000.0), 1.5)
        mix = 0.3 * snap + 0.2 * spring
    else:
        tinkle = np.zeros(n)
        for k, note in enumerate(("B5", "D6", "F#6")):
            place(tinkle, instruments.glass_chime(note_freq(note), 0.7, decay=0.35), samples(0.03 + 0.05 * k),
                  0.5 - 0.1 * k)
        mix = 0.3 * filters.highpass(crack, 900.0) + 0.25 * tinkle
    return filters.lowpass(mix, 6000.0)


# ------------------------------------------------------------------------------------------- built for 07 (M3-14)


def stargaze_swell(_variant, gen):
    """07 looks up at the stars: a soft, low, airy swell. A D2/A2/D3 pad breathes in under a slowly opening band of
    air, holds a moment with a faint A3 on top, and lets go over the wind."""
    n = samples(6.0)
    mix = np.zeros((n, 2))
    for note, gain, side in (("D2", 0.5, 0.0), ("A2", 0.32, -0.2), ("D3", 0.22, 0.2), ("A3", 0.06, 0.1)):
        place(mix, pan(gain * instruments.soft_pad(note_freq(note), 6.0, 2.2, 2.4, hold=0.9), side), 0, 0.25)
    shape = envelope.segments(n, [(0.0, 0.0), (2.4, 1.0), (3.4, 0.7), (6.0, 0.0)], shape="smooth")
    for side in (-0.35, 0.35):
        air = filters.swept(noise.pink(n, gen), "bandpass", osc.glide(n, 320.0, 900.0, time_constant=1.6), q=0.9)
        air = filters.lowpass(air / max(float(np.std(air)), 1e-9), 2500.0) * shape
        place(mix, pan(0.035 * air, side), 0)
    return mix


DOCK_HUM_LOOP_S = 4.0


def dock_charge_hum(_variant, gen):
    """Charging: a very soft, warm hum on D3 with its octave, breathing slowly, like a lamp keeping watch."""
    n = samples(DOCK_HUM_LOOP_S)
    step = SAMPLE_RATE / n
    d3 = loop_freq(note_freq("D3"), n)
    hum = osc.additive(n, d3, [(1, 1.0), (2, 0.5), (3, 0.12)]) + 0.4 * osc.sine(n, 2.0 * d3 + step, phase=0.4)
    hum *= envelope.lfo(n, 2.0 / DOCK_HUM_LOOP_S, 0.25, 0.75)
    return periodic(0.3 * hum, lambda x: filters.highpass(filters.lowpass(x, 2000.0), 80.0))


DOCK_FULL_CHORD = (("D4", 0.0, 0.8), ("F#4", 0.07, 0.55), ("A4", 0.14, 0.6), ("D5", 0.24, 0.7))


def dock_full(_variant, gen):
    """Charged (first-hour v2 section 5): the full resolved chord D F# A D', rolled softly from the bottom like a hand
    on a felt piano, over a warm D3 pad that lets go slowly. The rising cell notes end here."""
    n = samples(4.0)
    mix = np.zeros(n)
    for note, delay, amp in DOCK_FULL_CHORD:
        bell = instruments.soft_bell(note_freq(note), 3.2, decay=2.0, attack=0.05)
        place(mix, amp * bell, samples(delay))
    pad = instruments.soft_pad(note_freq("D3"), 3.4, 0.5, 2.0, hold=0.3)
    place(mix, 0.12 * pad, 0)
    return _mono_reverb(filters.lowpass(mix, 4800.0), room=0.6, damping=0.6, wet=0.25, dry=1.0)


# --------------------------------------------------------------------------------------------------- the battery

BATTERY_CELL_NOTES = ("D5", "E5", "F#5", "A5", "B5", "D6", "E6", "F#6")


def battery_cell(variant, _gen):
    """A cell filling at a charger (M3-20): a small warm chime on the next note of D E F# A B, then up an octave,
    short and soft so a whole charge is a gentle rising run; also the lamps lighting one by one as 07 wakes."""
    freq = note_freq(BATTERY_CELL_NOTES[variant])
    n = samples(1.3)
    chime = (instruments.partial(n, freq, 1.0, 0.004, 0.9)
             + instruments.partial(n, freq, 0.35, 0.004, 0.8, detune_cents=4.0)
             + instruments.partial(n, 2.0 * freq, 0.1, 0.003, 0.35)
             + instruments.partial(n, 3.0 * freq, 0.03, 0.002, 0.18))
    return _mono_reverb(filters.lowpass(chime, 5200.0), room=0.4, damping=0.6, wet=0.2, dry=1.0)


def battery_low(_variant, _gen):
    """The Low warning (30%): one soft falling two-note, A4 then F#4, a round slow-attack bell - a gentle
    'getting low', never an alarm."""
    mix = np.zeros(samples(2.2))
    place(mix, instruments.soft_bell(note_freq("A4"), 1.4, decay=1.0, attack=0.04), 0, 0.8)
    place(mix, instruments.soft_bell(note_freq("F#4"), 1.8, decay=1.3, attack=0.05), samples(0.42), 0.75)
    return _mono_reverb(filters.lowpass(mix, 3800.0), room=0.55, damping=0.65, wet=0.24, dry=1.0)


def battery_sigh(_variant, gen):
    """The battery empties (0%): a soft power-down sigh - a warm D4 hum gliding down to A2 as it fades, under a
    breath of air - then 07 crawls on its sealed reserve."""
    n = samples(2.0)
    glide = osc.glide(n, note_freq("D4"), note_freq("A2"), time_constant=0.55)
    hum = osc.additive(n, glide, [(1, 1.0), (2, 0.3), (3, 0.08)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.06, 1.0), (0.7, 0.55), (1.9, 0.0)], shape="smooth")
    breath = filters.bandpass(noise.pink(n, gen), 700.0, 0.7)
    breath *= envelope.segments(n, [(0.0, 0.0), (0.12, 1.0), (1.2, 0.0)], shape="smooth")
    breath = breath / max(float(np.std(breath)), 1e-9) * 0.03
    return _mono_reverb(filters.lowpass(0.4 * hum + breath, 2400.0), room=0.5, damping=0.7, wet=0.2, dry=1.0)


def lamp_tick(_variant, gen):
    """A flank lamp waking in the opening: a tiny relay tick and a breath of warm filament on D6, very quiet."""
    n = samples(0.18)
    tick = filters.bandpass(noise.white(n, gen), 2600.0, 1.8) * envelope.ar(n, 0.0004, 0.006)
    glow = osc.sine(n, note_freq("D6")) * envelope.ar(n, 0.004, 0.09)
    return filters.lowpass(0.22 * tick / max(float(np.max(np.abs(tick))), 1e-9) + 0.12 * glow, 5000.0)


# ---- 07's wake through its own eye (docs/design/cutscenes.md section 1) ----

ASLEEP_STATIC_LOOP_S = 8.0


def asleep_static(_variant, gen):
    """The radio asleep and out of tune, on the title and through 07's opening: soft hiss in an old set's narrow
    band breathing slowly, a faint heterodyne whistle on A5 that wanders a few cents, and the odd crackle pop.
    Seamless and quiet; the opening lets the ghost chord surface out of it."""
    n = samples(ASLEEP_STATIC_LOOP_S)
    loop_hz = 1.0 / ASLEEP_STATIC_LOOP_S
    hiss = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 1300.0, 0.6),
                    warmup=samples(0.5))
    hiss = hiss / max(float(np.std(hiss)), 1e-9) * envelope.lfo(n, 2.0 * loop_hz, 0.2, 0.8)
    t = np.arange(n) / SAMPLE_RATE
    wander = loop_freq(note_freq("A5"), n) * (1.0 + 0.003 * np.sin(2.0 * np.pi * loop_hz * t))
    whistle = osc.sine(n, wander) * envelope.lfo(n, loop_hz, 0.5, 0.5, phase=0.25) ** 2
    pops = np.zeros(n)
    for start in noise.event_times(n, gen, 1.5):
        length = samples(0.006)
        grain = filters.bandpass(noise.white(length, gen), 2400.0, 1.2) * envelope.ar(length, 0.0004, 0.004)
        place(pops, grain * gen.uniform(0.3, 1.0), int(start), wrap=True)
    mix = 0.25 * hiss + 0.012 * whistle + 0.08 * pops
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 4200.0), 260.0))


CARRIER_LOOP_S = 8.0


def radio_carrier(_variant, gen):
    """The Earth signal's carrier once tower level 1 clears the static (M3-24): no music and no words, just a steady
    soft tone on D4 with its fifth A4 far beneath, breathing very slowly, over a thin trace of the old band's hiss.
    The sound of something still transmitting. 8 s seamless."""
    n = samples(CARRIER_LOOP_S)
    loop_hz = 1.0 / CARRIER_LOOP_S
    tone = (osc.sine(n, loop_freq(note_freq("D4"), n))
            + 0.18 * osc.sine(n, loop_freq(note_freq("A4"), n))
            + 0.08 * osc.sine(n, loop_freq(note_freq("D5"), n)))
    tone *= envelope.lfo(n, loop_hz, 0.12, 0.88) * envelope.lfo(n, 3.0 * loop_hz, 0.05, 0.95, phase=0.3)
    hiss = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 1300.0, 0.6),
                    warmup=samples(0.5))
    hiss = hiss / max(float(np.std(hiss)), 1e-9)
    return periodic(0.05 * tone + 0.03 * hiss, lambda x: filters.highpass(filters.lowpass(x, 3600.0), 200.0))


GHOST_CHORD_NOTES = ("D3", "A3", "E4")
GHOST_CHORD_BAR_S = 3.0
GHOST_CHORD_BAND_HZ = 1600.0


def _rhodes(freq, duration):
    """A soft electric-piano tone: 2-op FM with a decaying bell index, plus a faint high tine."""
    n = samples(duration)
    body = two_op(n, freq, 1.0, 1.6 * envelope.exp_decay(n, 1.2), amp=envelope.exp_decay(n, 3.2))
    tine = two_op(n, freq, 14.0, 0.4 * envelope.exp_decay(n, 0.15), amp=envelope.exp_decay(n, 0.4))
    return (body + 0.05 * tine) * envelope.ar(n, 0.004, duration * 4.0)


def ghost_chord(_variant, gen):
    """A ghost of the music inside the static as 07 wakes (O1): a soft Rhodes chord D-A-E heard faintly through an old
    radio band with tape wow, surfacing out of nothing and sinking back."""
    duration = GHOST_CHORD_BAR_S + 0.6
    n = samples(duration)
    chord = np.zeros(n)
    for i, note in enumerate(GHOST_CHORD_NOTES):
        place(chord, _rhodes(note_freq(note), duration), samples(0.03 * i))
    radio = filters.lowpass(filters.highpass(chord, 320.0), GHOST_CHORD_BAND_HZ)
    radio = effects.wow_flutter(radio, wow_cents=9.0, wow_rate=0.5, flutter_cents=3.0, drift_cents=4.0, gen=gen)
    surface = envelope.segments(n, [(0.0, 0.0), (0.45 * duration, 1.0), (0.6 * duration, 0.85), (duration, 0.0)],
                                shape="smooth")
    hiss = filters.bandpass(noise.pink(n, gen), 1300.0, 0.6)
    hiss = hiss / max(float(np.std(hiss)), 1e-9)
    return filters.butter(0.4 * radio * surface + 0.02 * hiss * surface, "lowpass", 3000.0, order=4)


def wake_power_thump(_variant, gen):
    """07's systems starting inside the black (O1): a felted low thump, a breath of air and a soft D2 hum easing
    up under it, as if from inside the chassis."""
    n = samples(1.6)
    thump = osc.sine(n, osc.glide(n, 95.0, 52.0, time_constant=0.05)) * envelope.ar(n, 0.006, 0.35)
    felt = filters.lowpass(noise.pink(n, gen), 420.0)
    felt = felt / max(float(np.std(felt)), 1e-9) * envelope.ar(n, 0.003, 0.12)
    hum = osc.additive(n, note_freq("D2"), [(1, 1.0), (2, 0.35), (3, 0.1)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.15, 0.0), (0.7, 1.0), (1.6, 0.0)], shape="smooth")
    return filters.lowpass(0.5 * thump + 0.05 * felt + 0.12 * hum, 1400.0)


LID_TICK_NOTES = ("A2", "D3", "D5")
LID_TICK_LABELS = ("a2", "d3", "d5")


def lid_tick(variant, gen):
    """One felted servo tick of 07's lid: a soft click with a short ring on its note (A2 as the picture opens, D3 as
    the eyes open, D5 for the short wake's lid click)."""
    freq = note_freq(LID_TICK_NOTES[variant])
    n = samples(0.5)
    ring = osc.additive(n, freq, [(1, 1.0), (2, 0.4), (3, 0.12)])
    ring *= envelope.ar(n, 0.002, 0.22 if freq < 200.0 else 0.14)
    click_n = samples(0.006)
    click = filters.bandpass(noise.white(click_n, gen), 1800.0, 0.9) * envelope.ar(click_n, 0.0004, 0.004)
    out = 0.35 * filters.lowpass(ring, 2600.0)
    out[:click_n] += 0.08 * click
    return out


def neck_servo(_variant, gen):
    """07's old neck turning its head (O6-O9): a soft servo whirr whose faint whine lifts from D5 to E5."""
    n = samples(1.6)
    whine = osc.additive(n, osc.glide(n, note_freq("D5"), note_freq("E5"), time_constant=0.5), [(1, 1.0), (2, 0.2)])
    whirr = filters.bandpass(noise.pink(n, gen), 650.0, 2.5)
    whirr = whirr / max(float(np.std(whirr)), 1e-9)
    shape = envelope.segments(n, [(0.0, 0.0), (0.3, 1.0), (1.1, 0.8), (1.6, 0.0)], shape="smooth")
    return filters.highpass(filters.lowpass(0.02 * whine + 0.1 * whirr, 3200.0), 120.0) * shape


CUES = (
    # 0.5.2 "Satisfying" (docs/design/feel-pass.md, polish-pass.md 1.1): recipes in feel_cues.py.
    Cue("cut_sizzle", "loop_3d", feel.cut_sizzle, loop=True, file_stem="cut_sizzle_loop", volume=(0.5, 0.5),
        milestone="M3", notes="The beam through 07's body: a low-passed sizzle, level by cut progress. 3 s seamless."),
    Cue("cut_crack", "oneshot_3d", feel.cut_crack, volume=(0.7, 0.7), milestone="M3",
        notes="The piece breaks free: a soft high B pluck plus a felt thud."),
    Cue("strap_tuk", "tick_3d", feel.strap_tuk, volume=(0.7, 0.7), milestone="M3",
        notes="A strap cinching: the bundle's fold, a relic stowed in the cradle. A muted tuk on E."),
    Cue("dock_walls_rise", "oneshot_3d", feel.dock_walls_rise, volume=(0.6, 0.6), milestone="M3",
        notes="The dock's walls rising, staggered: soft ticks D A D A over a short servo push."),
    Cue("dock_clamp", "oneshot_3d", feel.dock_clamp, volume=(0.65, 0.65), milestone="M3",
        notes="The clamp closing on 07: a felt clunk and a 100 ms connection crackle."),
    Cue("dock_walls_settle", "tick_3d", feel.dock_walls_settle, volume=(0.55, 0.55), milestone="M3",
        notes="Full: the walls lowering and settling, two soft ticks on A and D."),
    Cue("dock_walls_drop", "tick_3d", feel.dock_walls_drop, volume=(0.5, 0.5), milestone="M3",
        notes="Driving off early: the walls drop at once, a quick soft fall."),
    Cue("chirp_affirm", "oneshot_3d", picks.renderer("chirp_affirm"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, affirm: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_happy", "oneshot_3d", picks.renderer("chirp_happy"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, happy: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_proud", "oneshot_3d", picks.renderer("chirp_proud"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, proud: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_curious", "oneshot_3d", picks.renderer("chirp_curious"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, curious: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_worried", "oneshot_3d", picks.renderer("chirp_worried"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, worried: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_tired", "oneshot_3d", picks.renderer("chirp_tired"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, tired: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_relieved", "oneshot_3d", picks.renderer("chirp_relieved"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, relieved: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("chirp_greeting", "oneshot_3d", picks.renderer("chirp_greeting"), volume=(0.55, 0.55), milestone="M3",
        sampled=True, notes="07's voice, greeting: an annunciator gesture from a real motor whine (sampled/picks.py); "
                            "syllables in chirp_syllables.json."),
    Cue("part_pick", "tick_2d", feel.part_pick, volume=(0.45, 0.45), milestone="M3",
        notes="0.5.5: picking a part up in the 07 tab: a light tick rising E5 -> A5."),
    Cue("part_drop", "tick_2d", feel.part_drop, volume=(0.45, 0.45), milestone="M3",
        notes="0.5.5: a part dropped on a valid socket: a seated tick on D5 over a felt thud."),
    Cue("part_refuse", "tick_2d", feel.part_refuse, volume=(0.4, 0.4), milestone="M3",
        notes="0.5.5: a drop refused: two soft muted ticks falling B4 -> F#4."),
    Cue("ui_relay_click", "tick_2d", picks.renderer("ui_relay_click"), volume=(0.45, 0.45), milestone="M3",
        sampled=True, notes="The databank's page starting to flip: a small relay pulling in, its contact settling."),
    Cue("ui_tube_thunk", "tick_2d", picks.renderer("ui_tube_thunk"), volume=(0.45, 0.45), milestone="M3",
        sampled=True, notes="The databank's picture tube settling on a channel: a soft latch thunk, the steel's ring "
                            "on D2 and a breath of the tube's hum on D3."),
    Cue("ui_tape_tick", "tick_2d", picks.renderer("ui_tape_tick"), volume=(0.4, 0.4), milestone="M3",
        sampled=True, notes="The databank's list cursor moving one line: a typewriter carriage's single ratchet "
                            "notch, like a tape head stepping."),
    Cue("sheet_complete", "stinger_2d", feel.sheet_complete, volume=(0.45, 0.45), milestone="M3", tonal=True,
        notes="0.5.5 D: a blueprint's sheet complete: a soft rising D major arpeggio on felt piano."),
    Cue("scan_sweep", "loop_3d", feel.scan_sweep, loop=True, file_stem="scan_sweep_loop", volume=(0.5, 0.5),
        milestone="M3", notes="0.5.5 D: the lens's fan sweeping a fragment; pitch steps D E F# A per 25%. 2 s loop."),
    Cue("scan_step", "oneshot_3d", feel.scan_step, variants=len(feel.SCAN_STEP_NOTES), volume=(0.5, 0.5),
        milestone="M3", tonal=True, notes="0.5.5 D: a quarter of the scan drawn: a glass note D E F# A."),
    Cue("scan_liftoff", "oneshot_3d", feel.scan_liftoff, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 D: the dissolve into the lens: a rising shimmer and a flare chime on F#6."),
    Cue("charge_brace", "oneshot_3d", feel.charge_brace, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 E: 07 bracing before a fresh charge: servo chunk and wheel-lock clack."),
    Cue("charge_whine", "loop_3d", feel.charge_whine, loop=True, file_stem="charge_whine_loop", volume=(0.5, 0.5),
        milestone="M3", notes="0.5.5 E: the capacitor whine on D5; pitch glides D -> A -> D' at runtime. 2 s seamless."),
    Cue("charge_hum", "loop_3d", feel.charge_hum, loop=True, file_stem="charge_hum_loop", volume=(0.55, 0.55),
        milestone="M3", notes="0.5.5 E: the low hum on D under the charge, level by stage. 3 s seamless."),
    Cue("charge_detent", "tick_3d", feel.charge_detent, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 E: a charge stage locking: a cell sleeve slides, then the detent click."),
    Cue("charge_lock", "oneshot_3d", feel.charge_lock, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 E: the full lock: a firm clunk-click before the held breath."),
    Cue("hold_boot", "ui_2d", feel.hold_boot, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 B: the first Tab press, 07's hold powering on in the dark."),
    Cue("hold_slot", "tick_2d", feel.hold_slot, variants=len(feel.HOLD_SLOT_NOTES), volume=(0.45, 0.45),
        milestone="M3", notes="0.5.5 B: a hold slot lighting, a rising pentatonic tick (D5 .. F#6)."),
    Cue("item_slide", "tick_2d", feel.item_slide, volume=(0.45, 0.45), milestone="M3",
        notes="0.5.5 B: a starter item sliding into its slot."),
    Cue("seal_cut", "oneshot_3d", feel.seal_cut, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 C: a container's seal cut through: a pop felt through the chassis and a ping on A5."),
    Cue("container_open", "oneshot_3d", feel.container_open, variants=len(feel.CONTAINER_LABELS),
        variant_labels=feel.CONTAINER_LABELS, volume=(0.6, 0.6), milestone="M3",
        notes="0.5.5 C: a container opening and its find sliding out; the hinge note by kind."),
    Cue("cut_ignite_tier", "oneshot_3d", feel.cut_ignite_tier, variants=len(feel.CUTTER_TIER_LABELS),
        variant_labels=feel.CUTTER_TIER_LABELS, volume=(0.65, 0.65), milestone="M3",
        notes="Ignition per cutter tier: the D6 zing brighter and hotter each tier, the thump a touch deeper."),
    Cue("cut_undertone", "loop_3d", feel.cut_undertone, loop=True, file_stem="cut_undertone_loop", volume=(0.6, 0.6),
        milestone="M3", notes="The beam's deeper hum for higher tiers: D2 + A2, level by tier. 4 s seamless."),
    Cue("segment_slide", "oneshot_3d", feel.segment_slide, variants=len(feel.SEGMENT_NOTES), volume=(0.5, 0.5),
        milestone="M3", notes="The beam moving to the next cut line: a tick and a slide onto the next step."),
    Cue("cargo_tok", "tick_3d", feel.cargo_tok, volume=(0.7, 0.7), milestone="M3",
        notes="The cargo hold full (M3-25): a soft tok on the lid."),
    Cue("pickup_lift", "oneshot_3d", feel.pickup_lift, volume=(0.6, 0.6), milestone="M3",
        notes="07 lifting a part (Collect Act): a soft servo whirr rising A2 -> D3."),
    Cue("pickup_clunk", "tick_3d", feel.pickup_clunk, volume=(0.8, 0.8), milestone="M3",
        notes="The part seating on 07's back (Collect Impact): a solid little clunk on D."),
    Cue("sky_diamond_ring", "stinger_2d", picks.renderer("sky_diamond_ring"), volume=(0.74, 0.74), milestone="M3",
        sampled=True, notes="Opening v2 B6: the Sun breaking out behind Earth on the first charge: bowed metal "
                            "rising onto D, its octave opening like light, a breath of creak."),
    Cue("build_place", "oneshot_3d", picks.renderer("build_place"), volume=(0.68, 0.68), milestone="M3", sampled=True,
        notes="A frame set down on regolith: an instrument case dropped, a steel ring tuned to D2, the feet latching."),
    Cue("build_snap", "tick_3d", picks.renderer("build_snap"), volume=(0.9, 0.9), milestone="M3", sampled=True,
        notes="The ghost aligning: a latch button and a vintage click, a ball detent dropping into its seat."),
    Cue("build_weld_loop", "loop_3d", picks.renderer("build_weld_loop"), loop=True, volume=(0.72, 0.72),
        milestone="M3", sampled=True, notes="The weld: a real arc buzz slowed and warmed, sparse crackle, a low hum on "
                                            "D. 4 s seamless."),
    Cue("build_structure_complete", "oneshot_3d", picks.renderer("build_structure_complete"), volume=(0.71, 0.71),
        milestone="M3", sampled=True, notes="The last weld cools: a case latch, a relay and 07's done tone onto A."),
    Cue("structure_power_on", "oneshot_3d", picks.renderer("structure_power_on"), volume=(0.6, 0.6), milestone="M3",
        sampled=True, notes="Opening v2 B8: power reaching a structure: a radio's power latch, a mains hum swelling "
                            "from D onto A, its lamp switching on."),
    Cue("boot_lens", "oneshot_3d", picks.renderer("boot_lens"), variants=sampled_boot.SOUNDS["boot_lens"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, the iris ratchets and the focus motor hunts onto A (on Started)."),
    Cue("boot_lamp_flicker", "tick_3d", picks.renderer("boot_lamp_flicker"), variants=sampled_boot.SOUNDS["boot_lamp_flicker"][1],
        volume=(0.9, 0.9), milestone="M3", sampled=True,
        notes="New Game self-test, a lamp flicker: relay, spark, a bark of hum (Pulse 0..2)."),
    Cue("boot_lamp_ramp", "oneshot_3d", picks.renderer("boot_lamp_ramp"), variants=sampled_boot.SOUNDS["boot_lamp_ramp"][1],
        volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="New Game self-test, the lamp switch and the circuit ramping onto D (Pulse 3)."),
    Cue("boot_head_sweep", "oneshot_3d", picks.renderer("boot_head_sweep"), variants=sampled_boot.SOUNDS["boot_head_sweep"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, the head servo sweeping out and back, grit (Pulse 0)."),
    Cue("boot_head_nod", "oneshot_3d", picks.renderer("boot_head_nod"), variants=sampled_boot.SOUNDS["boot_head_nod"][1],
        volume=(0.5, 0.5), milestone="M3", sampled=True,
        notes="New Game self-test, the nod and a latch seating (Pulse 1)."),
    Cue("boot_wheel", "oneshot_3d", picks.renderer("boot_wheel"), variants=sampled_boot.SOUNDS["boot_wheel"][1],
        volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="New Game self-test, one wheel twitch: a gear grunt and a clunk, each wheel its own (Pulse 0..3)."),
    Cue("boot_motor_shudder", "oneshot_3d", picks.renderer("boot_motor_shudder"), variants=sampled_boot.SOUNDS["boot_motor_shudder"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, the drive motor shuddering up (Pulse 0)."),
    Cue("boot_motor_creep", "oneshot_3d", picks.renderer("boot_motor_creep"), variants=sampled_boot.SOUNDS["boot_motor_creep"][1],
        volume=(0.4, 0.4), milestone="M3", sampled=True,
        notes="New Game self-test, the creep forward: wheels grinding regolith (Pulse 1)."),
    Cue("boot_cutter_spark", "oneshot_3d", picks.renderer("boot_cutter_spark"), variants=sampled_boot.SOUNDS["boot_cutter_spark"][1],
        volume=(0.5, 0.5), milestone="M3", sampled=True,
        notes="New Game self-test, an ignition attempt that fizzles, weaker each time (Pulse 0..2)."),
    Cue("boot_wing_jerk", "oneshot_3d", picks.renderer("boot_wing_jerk"), variants=sampled_boot.SOUNDS["boot_wing_jerk"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, the wing straining and jerking half open (Pulse 0)."),
    Cue("boot_wing_stall", "oneshot_3d", picks.renderer("boot_wing_stall"), variants=sampled_boot.SOUNDS["boot_wing_stall"][1],
        volume=(0.5, 0.5), milestone="M3", sampled=True,
        notes="New Game self-test, the wing grinding against the jam (Pulse 1)."),
    Cue("boot_wing_fold", "oneshot_3d", picks.renderer("boot_wing_fold"), variants=sampled_boot.SOUNDS["boot_wing_fold"][1],
        volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="New Game self-test, the wing folding back, a latch (Pulse 2)."),
    Cue("boot_antenna_rise", "oneshot_3d", picks.renderer("boot_antenna_rise"), variants=sampled_boot.SOUNDS["boot_antenna_rise"][1],
        volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="New Game self-test, the mast running up its sprung tape and latching (Pulse 0)."),
    Cue("boot_antenna_signal", "oneshot_3d", picks.renderer("boot_antenna_signal"), variants=sampled_boot.SOUNDS["boot_antenna_signal"][1],
        volume=(0.45, 0.45), milestone="M3", sampled=True,
        notes="New Game self-test, orbit hiss swelling in, receive only (Pulse 1)."),
    Cue("boot_battery", "oneshot_3d", picks.renderer("boot_battery"), variants=sampled_boot.SOUNDS["boot_battery"][1],
        volume=(0.45, 0.45), milestone="M3", sampled=True,
        notes="New Game self-test, the charge whine rising as the cells fill (on Started)."),
    Cue("boot_assembly", "oneshot_3d", picks.renderer("boot_assembly"), variants=sampled_boot.SOUNDS["boot_assembly"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, a latch and a short rising query: no blueprints (on Started)."),
    Cue("boot_begin_turn", "oneshot_3d", picks.renderer("boot_begin_turn"), variants=sampled_boot.SOUNDS["boot_begin_turn"][1],
        volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="New Game self-test, the head turning to the beacon (Pulse 0)."),
    Cue("boot_begin_shiver", "oneshot_3d", picks.renderer("boot_begin_shiver"), variants=sampled_boot.SOUNDS["boot_begin_shiver"][1],
        volume=(0.45, 0.45), milestone="M3", sampled=True,
        notes="New Game self-test, the dust shiver (Pulse 1)."),
    Cue("boot_begin_lamps", "oneshot_3d", picks.renderer("boot_begin_lamps"), variants=sampled_boot.SOUNDS["boot_begin_lamps"][1],
        volume=(0.6, 0.6), milestone="M3", sampled=True,
        notes="New Game self-test, the headlamps and a D F# A ready chord (Pulse 2)."),
    Cue("boot_fault", "oneshot_3d", picks.renderer("boot_fault"), volume=(0.55, 0.55), milestone="M3", sampled=True,
        notes="The self-test's fault result (the cutter): 07's short strain-and-choke on low B."),
    Cue("base_lamp_on", "oneshot_3d", picks.renderer("base_lamp_on"), variants=sampled_base.SOUNDS["base_lamp_on"][1],
        volume=(0.5, 0.5), milestone="M3", sampled=True,
        notes="A base lamp warming on at dusk (BaseLampLit): a relay's warm clunk, the filament's soft tick and the "
              "lamp's hum easing in on its step, D3 E3 F#3 A3 in the lighting order."),
    Cue("boost_whine", "loop_3d", feel.boost_whine, loop=True, file_stem="boost_whine_loop", volume=(0.45, 0.45),
        milestone="M3", notes="The capacitor drums boosting: a soft whine on A. 2 s seamless."),
    Cue("boost_release", "tick_3d", feel.boost_release, volume=(0.6, 0.6), milestone="M3",
        notes="The drums emptying: a felt fff and a tick on D."),
    Cue("page_tick", "tick_2d", feel.page_tick, volume=(0.5, 0.5), milestone="M3",
        notes="A new step's line: a breath-quiet page-turn tick."),
    Cue("save_tick", "tick_2d", feel.save_tick, volume=(0.4, 0.4), milestone="M3",
        notes="The game saved: a tiny soft tick on D, once per burst."),
    Cue("count_tick", "tick_2d", feel.count_tick, variants=len(feel.COUNT_TICK_NOTES), volume=(0.45, 0.45),
        milestone="M3", notes="The stock counter rolling one unit: a rising pentatonic tick."),
    Cue("asleep_static", "radio_2d", asleep_static, loop=True, file_stem="asleep_static_loop", volume=(0.6, 0.6),
        milestone="M3", notes="The radio asleep, out of tune (title, 07's opening): narrow-band hiss, a faint "
                              "wandering A5 whistle, odd crackle pops. 8 s seamless."),
    Cue("ghost_chord", "radio_fx_2d", ghost_chord, volume=(0.6, 0.6), fade_out=0.4, milestone="M3", tonal=True,
        notes="A ghost Rhodes chord D-A-E surfacing faintly through the static as 07 wakes (O1)."),
    Cue("wake_power_thump", "stinger_2d", wake_power_thump, volume=(0.5, 0.5), fade_out=0.3, milestone="M3",
        notes="O1: 07's systems thump on inside the black, a soft D2 hum easing up under it."),
    Cue("lid_tick", "tick_2d", lid_tick, variants=len(LID_TICK_NOTES), variant_labels=LID_TICK_LABELS,
        volume=(0.6, 0.6), fade_out=0.05, milestone="M3", tonal=True,
        notes="Felted servo tick of 07's lid: A2 (picture opens), D3 (eyes open), D5 (the short wake)."),
    Cue("neck_servo", "tick_2d", neck_servo, volume=(0.5, 0.5), fade_out=0.2, milestone="M3",
        notes="O6-O9: 07's old neck turning, a soft whirr with a faint whine lifting D5 to E5."),
    Cue("rover_hum", "loop_3d", rover_hum, loop=True, file_stem="rover_hum_loop", volume=(0.55, 0.55),
        hf_cutoff=6000.0, hf_max_db=-45.0,
        tonal=True, notes="D2 electric hum, 4 s seamless; content < 3 kHz so AudioSource.pitch 0.5..2 stays clean."),
    Cue("dust_crunch", "loop_3d", dust_crunch, loop=True, file_stem="dust_crunch_loop", volume=(0.5, 0.5),
        notes="Granular regolith crunch under wheels, 4 s seamless; volume follows speed x grounded."),
    Cue("suspension_creak", "oneshot_3d", suspension_creak, variants=3, volume=(0.35, 0.5),
        pitch=(0.94, 1.06), fade_out=0.05,
        notes="Soft rubbery stick-slip creak + quiet spring tone; no resonance above ~1.2 kHz (never squeaky)."),
    Cue("landing_thump", "oneshot_3d", landing_thump, variants=3, volume=(0.85, 1.0), pitch=(0.95, 1.05),
        fade_out=0.08, notes="Soft whump (98->47 Hz glide) + body + slow dust hiss; runtime scales by impact."),
    Cue("radio_carrier", "radio_2d", radio_carrier, loop=True, file_stem="radio_carrier_loop", tonal=True,
        milestone="M3", notes="The Earth signal's carrier after tower L1 (M3-24): a steady soft D4 tone with a faint "
                              "A4, breathing slowly over a trace of hiss. 8 s seamless; no music."),
    Cue("radio_static", "radio_2d", radio_static, loop=True, file_stem="radio_static_loop",
        notes="Warm AM/vinyl static, 6 s seamless stereo; volume rises as radio clarity falls."),
    Cue("radio_tune", "radio_fx_2d", radio_tune, fade_out=0.1,
        notes="Dial-tuning swish (static swell + sweeping resonant band) under the crossfade between tracks."),
    Cue("recovery_lift", "loop_3d", recovery_lift, loop=True, file_stem="recovery_lift_loop", volume=(0.6, 0.6),
        milestone="M2", tonal=True, hf_cutoff=6000.0, hf_max_db=-40.0,
        notes="Servo whir D3/A3 + air while 07 is lifted to safety; content < 3.2 kHz for the pitch glide."),
    Cue("recovery_settle", "oneshot_3d", recovery_settle, volume=(0.7, 0.7), fade_out=0.15, milestone="M2",
        notes="Air release + A3 -> D3 servo settle when the recovery lift sets 07 down."),
    Cue("salvage_chime", "oneshot_3d", salvage_chime, variants=SALVAGE_CHIME_NOTES, volume=(0.7, 0.7),
        fade_out=0.1, variant_labels=("D5", "E5", "Fs5", "A5", "B5", "D6", "E6", "Fs6"), milestone="M3",
        tonal=True, notes="Salvage folding in: felt fold + glassy chime per pentatonic degree D5..F#6; the salvage "
                          "melody picks the clip from the site's combo step (climbs, then weaves over the top)."),
    Cue("ui_hold_fill", "ui_2d", ui_hold_fill, volume=(0.45, 0.45), fade_out=0.05,
        notes="UiCue HoldFill: D5/A5 swell rising over the 0.6 s hold (UI TowerPanelSettings.HoldSeconds); "
              "stopped on release/complete."),
    Cue("ui_hold_complete", "ui_2d", ui_hold_complete, volume=(0.55, 0.55), fade_out=0.2, tonal=True,
        notes="UiCue HoldComplete: soft resolved D major felt-piano chord + music-box D6."),
    Cue("room_tone", "ambience_2d", room_tone, loop=True, file_stem="room_tone_loop", volume=(0.8, 0.8),
        milestone="M3", notes="Solitude room tone: wide, very soft dark air + faint D2/A2, 24 s seamless stereo."),
    Cue("rover_lamp_hum", "loop_3d", rover_lamp_hum, loop=True, file_stem="rover_lamp_hum_loop", volume=(0.5, 0.5),
        milestone="M3", tonal=True, notes="07's lamp: tiny warm hum on D3 + filament buzz, 4 s seamless."),
    Cue("rover_servo", "loop_3d", rover_servo, loop=True, file_stem="rover_servo_loop", volume=(0.5, 0.5),
        milestone="M3", notes="07's servos (steering, neck): soft gear whirr + faint whine, 2 s seamless."),
    Cue("rover_metal_tick", "tick_3d", rover_metal_tick, variants=len(METAL_TICK_NOTES), volume=(0.4, 0.55),
        pitch=(0.98, 1.02), fade_out=0.03, milestone="M3", tonal=True,
        notes="07's metal ticking as it cools after a drive: dry click + a tiny pentatonic ring."),
    Cue("salvage_cut_metal", "loop_3d", salvage_cut_metal, loop=True, file_stem="salvage_cut_metal_loop",
        volume=(0.5, 0.5), milestone="M3", notes="Cutting metal: low soft grind breathing with the beam + sparks."),
    Cue("salvage_cut_wiring", "loop_3d", salvage_cut_wiring, loop=True, file_stem="salvage_cut_wiring_loop",
        volume=(0.5, 0.5), milestone="M3", notes="Working wiring loose: dry crackle, the odd strand snap, light buzz."),
    Cue("salvage_cut_optics", "loop_3d", salvage_cut_optics, loop=True, file_stem="salvage_cut_optics_loop",
        volume=(0.5, 0.5), milestone="M3", notes="Easing optics free: glassy shimmer of tiny ticks over soft air."),
    Cue("salvage_cut_tone", "loop_3d", salvage_cut_tone, loop=True, file_stem="salvage_cut_tone_loop",
        volume=(0.4, 0.4), milestone="M3", tonal=True, hf_cutoff=6000.0, hf_max_db=-40.0,
        notes="The beam's singing edge on D4; the runtime steps it up the pentatonic as the cut goes on."),
    Cue("salvage_break", "oneshot_3d", salvage_break, variants=len(SALVAGE_BREAK_KINDS),
        variant_labels=SALVAGE_BREAK_KINDS, volume=(0.65, 0.65), fade_out=0.1, milestone="M3",
        notes="A piece breaking loose: metal crack + clunk, wiring snap + spring, optics glassy crack + tinkle."),
    Cue("stargaze_swell", "stinger_2d", stargaze_swell, volume=(0.5, 0.5), fade_out=0.6, milestone="M3",
        tonal=True, notes="07 stargazing: a soft low D2/A2/D3 pad with a faint A3, swelling in under airy noise."),
    Cue("dock_charge_hum", "loop_3d", dock_charge_hum, loop=True, file_stem="dock_charge_hum_loop",
        volume=(0.35, 0.35), milestone="M3", tonal=True,
        notes="Charging: a very soft warm hum on D3 + octave, breathing slowly."),
    Cue("dock_full", "oneshot_3d", dock_full, volume=(0.5, 0.5), fade_out=0.2, milestone="M3", tonal=True,
        notes="Charged: soft bell on D5 with A5 blooming above it."),
    Cue("battery_cell", "oneshot_3d", battery_cell, variants=len(BATTERY_CELL_NOTES),
        variant_labels=tuple(note.replace("#", "s") for note in BATTERY_CELL_NOTES), volume=(0.45, 0.45),
        fade_out=0.2, milestone="M3", tonal=True,
        notes="A cell filling (and a flank lamp waking): a small warm chime, D5 E5 F#5 A5 B5 then up an octave."),
    Cue("battery_low", "oneshot_3d", battery_low, volume=(0.5, 0.5), fade_out=0.25, milestone="M3", tonal=True,
        notes="The Low warning (30%): one soft falling two-note A4 -> F#4, round bells, never an alarm."),
    Cue("battery_sigh", "oneshot_3d", battery_sigh, volume=(0.5, 0.5), fade_out=0.25, milestone="M3",
        notes="The battery empties: a soft power-down sigh, D4 gliding to A2 under a breath of air."),
    Cue("rover_lights_switch", "tick_3d", picks.renderer("rover_lights_switch"),
        variants=sampled_rover.SOUNDS["rover_lights_switch"][1], variant_labels=sampled_rover.LABELS,
        volume=(0.5, 0.5), milestone="M3", sampled=True,
        notes="07's lamps switched by the player (RoverLightsSwitched), at the headlamp: on = a relay tick, its "
              "contact and a faint filament breath on D3; off = a softer, darker tick."),
    Cue("lamp_tick", "tick_3d", lamp_tick, volume=(0.45, 0.45), fade_out=0.03, milestone="M3",
        notes="A flank lamp lighting as 07 wakes: a tiny relay tick with a breath of filament on D6."),
)
