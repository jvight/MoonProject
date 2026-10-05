"""Every sound cue of Lofi Lunar as a deterministic recipe on the shared ``synth`` core.

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
from synth.fm import operator
from synth.loop import periodic
from synth.pluck import karplus_strong


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

    def files(self) -> list:
        """Paths relative to Assets/_Project/Audio."""
        stem = self.file_stem or self.id
        folder = CATEGORIES[self.category].folder
        if self.variants == 1:
            return [f"{folder}/{stem}.wav"]
        labels = self.variant_labels or tuple(f"{i + 1:02d}" for i in range(self.variants))
        return [f"{folder}/{stem}_{label}.wav" for label in labels]


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


# --------------------------------------------------------------------------------------------------- world

AMBIENCE_LOOP_S = 72.0
AMBIENCE_TONES = (
    # time (s), note, amplitude, pan, attack (s)
    (2.0, "A3", 0.8, -0.35, 2.5),
    (9.5, "D4", 0.7, 0.30, 2.0),
    (17.0, "F#4", 0.55, -0.10, 3.0),
    (25.5, "B3", 0.75, 0.40, 2.5),
    (33.0, "E4", 0.5, -0.45, 2.0),
    (40.5, "A4", 0.55, 0.15, 3.0),
    (48.0, "D4", 0.7, -0.25, 2.5),
    (55.0, "B4", 0.4, 0.45, 3.0),
    (62.5, "F#4", 0.55, -0.05, 2.5),
    (68.0, "D5", 0.3, 0.25, 3.0),
)


def ambience_bed(_variant, gen):
    """Very soft lunar hush (no wind: a dark, slowly breathing noise floor) with sparse, distant pentatonic
    tones swimming in a long reverb. Every modulation period divides the 72 s loop."""
    n = samples(AMBIENCE_LOOP_S)
    loop_hz = 1.0 / AMBIENCE_LOOP_S
    hush = np.zeros((n, 2))
    for side in range(2):
        floor = periodic(noise.white(n, gen), lambda s: filters.highpass(
            filters.lowpass(filters.lowpass(noise.pink_filter(s), 520.0), 520.0), 60.0), warmup=samples(2.0))
        air = periodic(noise.white(n, gen), lambda s: filters.bandpass(noise.pink_filter(s), 1300.0, 0.7),
                       warmup=samples(2.0))
        breath = (envelope.lfo(n, 2.0 * loop_hz, 0.15, 0.75, phase=0.2 * side)
                  + envelope.lfo(n, 3.0 * loop_hz, 0.10, 0.0, phase=0.07 + 0.16 * side))
        swell = envelope.lfo(n, 4.0 * loop_hz, 0.5, 0.5, phase=0.33 * side)
        hush[:, side] = (floor + 0.18 * air * swell) * breath
    dry = np.zeros((n, 2))
    mono = np.zeros(n)
    for when, note, amp, position, attack in AMBIENCE_TONES:
        tone = instruments.soft_pad(note_freq(note), attack + 9.0, attack, 5.5, hold=0.6, detune_cents=5.0)
        tone = filters.lowpass(tone, 2200.0)
        start = samples(when + gen.uniform(-0.4, 0.4))
        place(dry, pan(tone, position), start, 0.35 * amp, wrap=True)
        place(mono, tone, start, amp, wrap=True)
    wet = periodic(mono, lambda s: effects.reverb(s, room=0.88, damping=0.6, wet=1.0, dry=0.0, predelay=0.045),
                   warmup=samples(12.0))
    return hush + 0.06 * (dry + wet)


# --------------------------------------------------------------------------------------------------- sonar & relics

def sonar_ping(_variant, gen):
    """Kalimba D5 with a small, soft room."""
    dry = instruments.kalimba(note_freq("D5"), 1.9, gen, decay=1.5)
    return _mono_reverb(dry, room=0.45, damping=0.55, wet=0.22, dry=1.0)


def relic_answer(_variant, _gen):
    """The relic's reply: a higher, shimmering A5 + D6 pair (slow attack, tremolo, detuned twins, more air)."""
    a5 = instruments.soft_bell(note_freq("A5"), 2.9, decay=1.9, attack=0.03)
    d6 = instruments.soft_bell(note_freq("D6"), 2.9, decay=1.7, attack=0.03)
    mix = a5.copy()
    place(mix, d6, samples(0.11), 0.75)
    mix = filters.lowpass(effects.tremolo(mix, 6.5, 0.22), 6000.0)
    return _mono_reverb(mix, room=0.7, damping=0.6, wet=0.32, dry=1.0)


def scrap_chime(variant, _gen):
    """Glassy chime on degree ``variant`` of D major pentatonic in octave 5: consecutive pickups climb."""
    dry = instruments.glass_chime(pentatonic(5, variant), 1.4, decay=1.15)
    return _mono_reverb(dry, room=0.5, damping=0.55, wet=0.2, dry=1.0)


EXCAVATION_LOOP_S = 6.0


def excavation_rumble(_variant, gen):
    """Low, soft earth rumble with a little sliding grit and a quiet D2/A2 drone that beats slowly."""
    n = samples(EXCAVATION_LOOP_S)
    rumble = periodic(noise.white(n, gen), lambda s: filters.highpass(
        filters.lowpass(filters.lowpass(noise.brown_filter(s, 0.996), 140.0), 140.0), 28.0))
    grit = _grains(n, gen, 35.0, (0.01, 0.03), (0.002, 0.002), (0.008, 0.02), 0.4, 0.5)
    grit = periodic(grit, lambda s: filters.lowpass(filters.bandpass(s, 450.0, 1.2), 1800.0))
    step = SAMPLE_RATE / n
    d2, a2 = loop_freq(note_freq("D2"), n), loop_freq(note_freq("A2"), n)
    drone = (osc.sine(n, d2, amp=0.25) + osc.sine(n, d2 + step, 0.4, 0.12)
             + osc.sine(n, a2, 0.2, 0.15) + osc.sine(n, a2 + step, 0.7, 0.07))
    swell = (envelope.lfo(n, 3.0 / EXCAVATION_LOOP_S, 0.2, 1.0)
             + envelope.lfo(n, 8.0 / EXCAVATION_LOOP_S, 0.1, 0.0, phase=0.16))
    return (1.6 * rumble + 0.8 * grit + drone) * swell


def surfacing_sparkle(_variant, gen):
    """A relic surfaces: a quick rising run of glassy notes (D5 to D6) over a soft upward whoosh."""
    n = samples(3.0)
    mix = np.zeros(n)
    for k in range(6):
        last = k == 5
        note = instruments.glass_chime(pentatonic(5, k), 2.4 if last else 1.4, decay=1.6 if last else 0.9,
                                       attack=0.004)
        place(mix, note, samples(0.075 * k), 0.55 + 0.08 * k)
    sweep_n = samples(2.2)
    whoosh = filters.swept(noise.pink(sweep_n, gen), "bandpass", osc.glide(sweep_n, 300.0, 1500.0), q=1.4)
    whoosh *= envelope.segments(sweep_n, [(0.0, 0.0), (0.45, 1.0), (2.2, 0.0)], shape="smooth")
    place(mix, whoosh, 0, 1.2)
    mix = filters.lowpass(effects.tremolo(mix, 7.0, 0.15), 7000.0)
    return _mono_reverb(mix, room=0.75, damping=0.6, wet=0.32, dry=1.0)


# --------------------------------------------------------------------------------------------------- tether

def tether_attach(_variant, gen):
    """Soft felt pluck on D4 with a little A2 body and a faint D5 glint."""
    n = samples(1.3)
    string = karplus_strong(n, note_freq("D4"), gen, decay=0.9965, brightness=0.35, pick_softness=2000.0)
    body = osc.sine(n, note_freq("A2")) * envelope.ar(n, 0.002, 0.12)
    glint = osc.sine(n, note_freq("D5")) * envelope.ar(n, 0.006, 0.55)
    return filters.lowpass(string + 0.25 * body + 0.06 * glint, 5000.0)


TETHER_LOOP_S = 4.0


def tether_hum(_variant, gen):
    """Warm energy hum on D3 + A3 + D4 with a gentle vibrato (4.5 Hz) and a slow wobble (0.75 Hz); faint fizz."""
    n = samples(TETHER_LOOP_S)
    vib_rate = 18.0 / TETHER_LOOP_S
    vibrato = osc.sine(n, vib_rate)
    tone = np.zeros(n)
    for note, amp in (("D3", 1.0), ("A3", 0.55), ("D4", 0.32)):
        f = loop_freq(note_freq(note), n)
        beta = 0.004 * f / vib_rate
        tone += amp * operator(n, f, modulation=vibrato, index=beta)
        tone += 0.04 * amp * operator(n, 3.0 * f, modulation=vibrato, index=3.0 * beta, phase=0.2)
    wobble = envelope.lfo(n, 3.0 / TETHER_LOOP_S, 0.09, 0.91)
    fizz = periodic(noise.white(n, gen), lambda s: filters.bandpass(s, 1800.0, 3.0))
    fizz *= envelope.lfo(n, 24.0 / TETHER_LOOP_S, 0.5, 0.5)
    mix = tone * wobble + 0.035 * fizz
    return periodic(mix, lambda s: filters.lowpass(to_mono(effects.chorus(
        s, voices=1, rate=2.0 / TETHER_LOOP_S, depth=0.0015, delay=0.008, mix=0.35, spread=0.0)), 2600.0))


def tether_release(_variant, gen):
    """Breathy 'fwip': a rising band-passed noise sweep with a soft falling A4 -> D4 tonal hint."""
    n = samples(0.55)
    rise, settle = samples(0.14), samples(0.3)
    idx = np.arange(n)
    centre = np.where(idx < rise, 450.0 * (2200.0 / 450.0) ** (idx / rise),
                      2200.0 - 700.0 * np.minimum(1.0, (idx - rise) / settle))
    sweep = filters.swept(noise.pink(n, gen), "bandpass", centre, q=2.2)
    breath = filters.lowpass(noise.white(n, gen), 1200.0)
    env = envelope.segments(n, [(0.0, 0.0), (0.06, 1.0), (0.18, 0.75), (0.55, 0.0)], shape="smooth")
    hint = osc.sine(n, osc.glide(n, note_freq("A4"), note_freq("D4"), time_constant=0.08))
    hint *= envelope.ar(n, 0.01, 0.25)
    return filters.lowpass((3.0 * sweep + 0.12 * breath) * env + 0.05 * hint, 6000.0)


# --------------------------------------------------------------------------------------------------- UI

def ui_click(_variant, gen):
    """Tiny soft tick: a very short A5 + A4 blip with a whisper of noise."""
    n = samples(0.09)
    tone = osc.sine(n, note_freq("A5")) * envelope.ar(n, 0.0008, 0.035)
    low = osc.sine(n, note_freq("A4")) * envelope.ar(n, 0.001, 0.025)
    tick = filters.bandpass(noise.white(n, gen), 2500.0, 1.0) * envelope.ar(n, 0.0003, 0.006)
    return filters.lowpass(tone + 0.4 * low + 0.15 * tick, 6000.0)


def _ui_two_notes(first: str, second: str, gap: float, duration: float, amps=(0.8, 1.0)) -> np.ndarray:
    n = samples(duration)
    mix = np.zeros(n)
    for k, note in enumerate((first, second)):
        tone = osc.additive(n, note_freq(note), [(1.0, 1.0), (2.0, 0.15), (3.0, 0.05)]) * envelope.ar(n, 0.003, 0.35)
        place(mix, tone, samples(gap * k), amps[k])
    return _mono_reverb(filters.lowpass(mix, 5000.0), room=0.3, damping=0.6, wet=0.12, dry=1.0)


def ui_confirm(_variant, _gen):
    """Rising D5 -> A5."""
    return _ui_two_notes("D5", "A5", 0.08, 0.55)


def ui_back(_variant, _gen):
    """Falling A4 -> D4, a little softer."""
    return _ui_two_notes("A4", "D4", 0.07, 0.5, amps=(0.9, 0.8))


def upgrade_arpeggio(_variant, gen):
    """Soft kalimba arpeggio D4 A4 D5 F#5 A5 rising left to right over a quiet D/A pad, in a warm stereo room."""
    n = samples(3.4)
    dry = np.zeros((n, 2))
    mono = np.zeros(n)
    for k, note in enumerate(("D4", "A4", "D5", "F#5", "A5")):
        last = k == 4
        tone = instruments.kalimba(note_freq(note), 2.6 if last else 1.6, gen, decay=1.8 if last else 1.2)
        gain = 0.75 + 0.05 * k
        place(dry, pan(tone, -0.3 + 0.15 * k), samples(0.11 * k), gain)
        place(mono, tone, samples(0.11 * k), gain)
    pad = (instruments.soft_pad(note_freq("D4"), 3.4, 0.35, 1.6, hold=0.5)
           + instruments.soft_pad(note_freq("A4"), 3.4, 0.35, 1.6, hold=0.5))
    wet = effects.reverb(mono, room=0.65, damping=0.55, wet=0.3, dry=0.0, width=0.9)
    return dry + wet + 0.12 * filters.lowpass(pad, 2000.0)[:, None]


# --------------------------------------------------------------------------------------------------- registry

CUES = (
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
    Cue("radio_static", "radio_2d", radio_static, loop=True, file_stem="radio_static_loop",
        notes="Warm AM/vinyl static, 6 s seamless stereo; volume rises as radio clarity falls."),
    Cue("radio_tune", "radio_fx_2d", radio_tune, fade_out=0.1,
        notes="Dial-tuning swish (static swell + sweeping resonant band) under the crossfade between tracks."),
    Cue("ambience_bed", "ambience_2d", ambience_bed, loop=True, file_stem="ambience_bed_loop", volume=(0.8, 0.8),
        notes="72 s seamless stereo lunar hush with 10 sparse distant pentatonic tones in a long reverb."),
    Cue("sonar_ping", "oneshot_3d", sonar_ping, volume=(0.8, 0.8), fade_out=0.15, milestone="M2",
        tonal=True, notes="Kalimba D5 (tine partial 6.27x, felt-pick transient)."),
    Cue("relic_answer", "oneshot_3d", relic_answer, volume=(0.8, 0.8), fade_out=0.2, milestone="M2",
        tonal=True, notes="A5 + D6 shimmering soft bells, tremolo 6.5 Hz, detuned twins."),
    Cue("scrap_chime", "oneshot_3d", scrap_chime, variants=5, volume=(0.7, 0.7), fade_out=0.1,
        variant_labels=("D5", "E5", "Fs5", "A5", "B5"), milestone="M2", tonal=True,
        notes="Glassy chime per pentatonic degree; clip index = combo step (pitch 2.0 for the next octave)."),
    Cue("tether_attach", "oneshot_3d", tether_attach, volume=(0.7, 0.7), fade_out=0.15, milestone="M2",
        tonal=True, notes="Karplus-Strong felt pluck D4 + A2 body."),
    Cue("tether_hum", "loop_3d", tether_hum, loop=True, file_stem="tether_hum_loop", volume=(0.5, 0.5),
        milestone="M2", tonal=True, notes="D3/A3/D4 hum, vibrato 4.5 Hz, wobble 0.75 Hz, 4 s seamless."),
    Cue("tether_release", "oneshot_3d", tether_release, volume=(0.6, 0.6), pitch=(0.95, 1.05), fade_out=0.06,
        milestone="M2", notes="Breathy band-passed noise sweep 450->2200 Hz + falling A4->D4 hint."),
    Cue("excavation_rumble", "loop_3d", excavation_rumble, loop=True, file_stem="excavation_rumble_loop",
        volume=(0.6, 0.6), milestone="M2", notes="Low brown-noise rumble + grit + D2/A2 drone, 6 s seamless."),
    Cue("surfacing_sparkle", "oneshot_3d", surfacing_sparkle, volume=(0.8, 0.8), fade_out=0.25, milestone="M2",
        tonal=True, notes="Rising glassy run D5..D6 over an upward whoosh."),
    Cue("ui_click", "ui_2d", ui_click, volume=(0.6, 0.6), fade_out=0.01, tonal=True, notes="Soft A5 tick."),
    Cue("ui_confirm", "ui_2d", ui_confirm, volume=(0.7, 0.7), fade_out=0.08, tonal=True, notes="Rising D5 -> A5."),
    Cue("ui_back", "ui_2d", ui_back, volume=(0.65, 0.65), fade_out=0.08, tonal=True, notes="Falling A4 -> D4."),
    Cue("upgrade_arpeggio", "stinger_2d", upgrade_arpeggio, volume=(0.8, 0.8), fade_out=0.3, milestone="M2",
        tonal=True, notes="Soft kalimba D4 A4 D5 F#5 A5, stereo, over a quiet D/A pad."),
)
