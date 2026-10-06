"""Every sound cue of Lofi Lunar as a deterministic recipe on the shared ``synth`` core.

A recipe returns one signal (mono ``(n,)`` or stereo ``(n, 2)``) per variant; ``build_sfx.py`` handles DC
removal, edge fades, loudness normalisation per category, the safety limiter and file output. All tonal
material is in D major pentatonic (D E F# A B). Loops are periodic by construction: whole-cycle frequencies
(``loop_freq``), LFO rates of k / loop length, events placed with wrap-around, and every filter - including the
pink/brown colouring of white noise - run through ``synth.loop.periodic``.
"""
import math
import re
from dataclasses import dataclass
from pathlib import Path

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


RELIC_CONTENT = Path(__file__).resolve().parents[2] / "Assets" / "_Project" / "Data" / "Content" / "Relics"
RELIC_LADDER_NOTES = 10
_RELIC_FIELD = re.compile(r"^  _(id|answerNote): *(.*?)\s*$", re.MULTILINE)


def read_relic_notes(folder: Path = RELIC_CONTENT) -> tuple:
    """``((relic_id, ladder_index), ...)`` sorted by id, read from Gameplay's relic content assets
    (``Relic_<id>.asset``: ``_id`` and ``_answerNote``, an index into the D major pentatonic ladder from D5).
    Gameplay owns the notes; re-rendering after a content change keeps every relic singing its own note."""
    notes = []
    for asset in sorted(folder.glob("Relic_*.asset")):
        fields = dict(_RELIC_FIELD.findall(asset.read_text(encoding="utf-8")))
        if "id" not in fields or "answerNote" not in fields:
            raise ValueError(f"{asset}: missing _id or _answerNote (RelicDefinition layout changed?)")
        index = int(fields["answerNote"])
        if not 0 <= index < RELIC_LADDER_NOTES:
            raise ValueError(f"{asset}: _answerNote {index} outside the ladder 0..{RELIC_LADDER_NOTES - 1}")
        notes.append((fields["id"], index))
    if not notes:
        raise ValueError(f"no relic content assets in {folder}")
    return tuple(sorted(notes))


RELIC_NOTES = read_relic_notes()


def relic_answer(variant, _gen):
    """A relic's reply on its own note (D major pentatonic ladder from D5): a slow-attack shimmering bell with a
    softer octave answering 110 ms later, tremolo, detuned twins and air. Same voice for every relic, so they are
    told apart by their note."""
    freq = pentatonic(5, RELIC_NOTES[variant][1])
    root = instruments.soft_bell(freq, 2.9, decay=1.9, attack=0.03)
    octave = instruments.soft_bell(2.0 * freq, 2.9, decay=1.5, attack=0.03)
    mix = root.copy()
    place(mix, octave, samples(0.11), 0.45)
    mix = filters.lowpass(effects.tremolo(mix, 6.5, 0.22), 6000.0)
    return _mono_reverb(mix, room=0.7, damping=0.6, wet=0.32, dry=1.0)


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


SCRAP_CHIME_NOTES = 8
CHIME_BRIGHT_LIMIT_HZ = 1000.0


def scrap_chime(variant, _gen):
    """Glassy chime on degree ``variant`` of D major pentatonic from D5 (D5 E5 F#5 A5 B5 D6 E6 F#6): consecutive
    pickups climb. Above ~1 kHz the bell partials are tapered so the top notes stay as soft as the low ones."""
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
    return _mono_reverb(dry, room=0.5, damping=0.55, wet=0.2, dry=1.0)


def relic_placed(_variant, gen):
    """A relic settles on the museum shelf: a soft felt 'tock' on D3 and a quiet A4 -> D5 kalimba resolve."""
    n = samples(1.8)
    tock = osc.sine(n, osc.glide(n, 1.25 * note_freq("D3"), note_freq("D3"), time_constant=0.015))
    tock *= envelope.ar(n, 0.002, 0.16)
    felt = filters.lowpass(filters.lowpass(noise.white(n, gen), 900.0), 900.0) * envelope.ar(n, 0.001, 0.035)
    mix = tock + 0.5 * felt
    place(mix, instruments.kalimba(note_freq("A4"), 1.6, gen, decay=1.1), samples(0.05), 0.35)
    place(mix, instruments.kalimba(note_freq("D5"), 1.6, gen, decay=1.4), samples(0.16), 0.45)
    return _mono_reverb(filters.lowpass(mix, 5000.0), room=0.45, damping=0.6, wet=0.2, dry=1.0)


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


def tether_snap(_variant, gen):
    """The tether lets go on its own (anti-frustration snap): a softer, longer, falling sigh - a band-passed
    breath sweeping down with a slow A4 -> D4 fall. Never a crack or a twang."""
    n = samples(0.9)
    centre = osc.glide(n, 1800.0, 380.0)
    sigh = filters.swept(noise.pink(n, gen), "bandpass", centre, q=1.6)
    breath = filters.lowpass(noise.white(n, gen), 900.0)
    env = envelope.segments(n, [(0.0, 0.0), (0.09, 1.0), (0.35, 0.6), (0.9, 0.0)], shape="smooth")
    hint = osc.sine(n, osc.glide(n, note_freq("A4"), note_freq("D4"), time_constant=0.22))
    hint *= envelope.ar(n, 0.03, 0.5)
    return filters.lowpass((2.6 * sigh + 0.1 * breath) * env + 0.04 * hint, 4500.0)


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

def _rolled(notes, gap: float, duration: float, gen, decay: float, brightness: float, gains=None) -> np.ndarray:
    """Felt-piano notes rolled upward by ``gap`` seconds each (a soft, unhurried chord)."""
    n = samples(duration)
    mix = np.zeros(n)
    for k, note in enumerate(notes):
        tone = instruments.felt_piano(note_freq(note), duration, gen, decay=decay, brightness=brightness)
        place(mix, tone, samples(gap * k), gains[k] if gains else 1.0)
    return mix


def ui_menu_open(_variant, gen):
    """The pause menu opens: a soft felt-piano D4 A4 D5 rolled upward, like settling into the cabin."""
    mix = _rolled(("D4", "A4", "D5"), 0.045, 1.1, gen, decay=1.3, brightness=0.45, gains=(0.8, 0.7, 0.6))
    return _mono_reverb(filters.lowpass(mix, 4500.0), room=0.4, damping=0.6, wet=0.14, dry=1.0)


def ui_menu_close(_variant, gen):
    """The menu closes: the same felt piano, A4 then D4, a little softer and shorter."""
    mix = _rolled(("A4", "D4"), 0.06, 0.85, gen, decay=0.9, brightness=0.4, gains=(0.65, 0.8))
    return _mono_reverb(filters.lowpass(mix, 4500.0), room=0.4, damping=0.6, wet=0.12, dry=1.0)


UI_FOCUS_NOTES = ("A4", "B4", "D5")


def ui_focus(variant, gen):
    """Focus moves: a tiny soft wooden tick (three pentatonic pitches, picked without repeats)."""
    tick = instruments.wood_tick(note_freq(UI_FOCUS_NOTES[variant]), 0.12, gen, decay=0.045)
    return filters.lowpass(tick, 4000.0)


UI_SLIDER_NOTES = ("D5", "E5")


def ui_slider(variant, gen):
    """A slider step: an even smaller muted kalimba tap."""
    tap = instruments.kalimba(note_freq(UI_SLIDER_NOTES[variant]), 0.16, gen, decay=0.07, tine=0.03, warmth=0.0)
    return filters.lowpass(tap, 3500.0)


def ui_prompt(_variant, gen):
    """A context prompt appears: a soft E5 grace note into A5 on the felt piano, barely there."""
    mix = _rolled(("E5", "A5"), 0.035, 0.7, gen, decay=0.6, brightness=0.35, gains=(0.45, 0.8))
    return _mono_reverb(filters.lowpass(mix, 4000.0), room=0.35, damping=0.6, wet=0.12, dry=1.0)


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


UI_CARD_PHRASE = (
    # time (s), note, gain
    (0.00, "A5", 0.8),
    (0.19, "F#5", 0.65),
    (0.38, "A5", 0.7),
    (0.57, "D6", 0.85),
    (0.57, "D5", 0.35),
)


def ui_card(_variant, _gen):
    """A memory card appears: a delicate music-box phrase A5 F#5 A5 resolving to D6 over a low D5 tine,
    stereo, in a small room."""
    n = samples(2.6)
    dry = np.zeros((n, 2))
    mono = np.zeros(n)
    for when, note, gain in UI_CARD_PHRASE:
        tone = instruments.music_box(note_freq(note), 1.9, decay=1.3 if note.endswith("6") else 0.9)
        position = 0.25 if note == "D6" else (-0.2 if note == "D5" else -0.05)
        place(dry, pan(tone, position), samples(when), gain)
        place(mono, tone, samples(when), gain)
    wet = effects.reverb(mono, room=0.55, damping=0.55, wet=0.28, dry=0.0, width=0.9)
    return dry + wet


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
    Cue("relic_answer", "oneshot_3d", relic_answer, variants=len(RELIC_NOTES), volume=(0.8, 0.8), fade_out=0.2,
        variant_labels=tuple(relic_id for relic_id, _ in RELIC_NOTES), milestone="M2", tonal=True,
        notes="One variant per relic (label = relic id) on its answer note from Data/Content/Relics: shimmering "
              "soft bell + octave, tremolo 6.5 Hz."),
    Cue("recovery_lift", "loop_3d", recovery_lift, loop=True, file_stem="recovery_lift_loop", volume=(0.6, 0.6),
        milestone="M2", tonal=True, hf_cutoff=6000.0, hf_max_db=-40.0,
        notes="Servo whir D3/A3 + air while 07 is lifted to safety; content < 3.2 kHz for the pitch glide."),
    Cue("recovery_settle", "oneshot_3d", recovery_settle, volume=(0.7, 0.7), fade_out=0.15, milestone="M2",
        notes="Air release + A3 -> D3 servo settle when the recovery lift sets 07 down."),
    Cue("scrap_chime", "oneshot_3d", scrap_chime, variants=SCRAP_CHIME_NOTES, volume=(0.7, 0.7), fade_out=0.1,
        variant_labels=("D5", "E5", "Fs5", "A5", "B5", "D6", "E6", "Fs6"), milestone="M2", tonal=True,
        notes="Glassy chime per pentatonic degree D5..F#6, ascending; the runtime picks the clip from the combo "
              "step (climbs, then weaves over the top notes)."),
    Cue("tether_attach", "oneshot_3d", tether_attach, volume=(0.7, 0.7), fade_out=0.15, milestone="M2",
        tonal=True, notes="Karplus-Strong felt pluck D4 + A2 body."),
    Cue("tether_hum", "loop_3d", tether_hum, loop=True, file_stem="tether_hum_loop", volume=(0.5, 0.5),
        milestone="M2", tonal=True, notes="D3/A3/D4 hum, vibrato 4.5 Hz, wobble 0.75 Hz, 4 s seamless."),
    Cue("tether_release", "oneshot_3d", tether_release, volume=(0.6, 0.6), pitch=(0.95, 1.05), fade_out=0.06,
        milestone="M2", notes="Breathy band-passed noise sweep 450->2200 Hz + falling A4->D4 hint."),
    Cue("tether_snap", "oneshot_3d", tether_snap, volume=(0.45, 0.45), pitch=(0.97, 1.03), fade_out=0.1,
        milestone="M2", notes="Softer sighing release when the tether snaps itself: falling breath 1800->380 Hz."),
    Cue("relic_placed", "oneshot_3d", relic_placed, volume=(0.7, 0.7), fade_out=0.2, milestone="M2", tonal=True,
        notes="Relic placed on the museum shelf: soft D3 felt tock + A4 -> D5 kalimba resolve."),
    Cue("excavation_rumble", "loop_3d", excavation_rumble, loop=True, file_stem="excavation_rumble_loop",
        volume=(0.6, 0.6), milestone="M2", notes="Low brown-noise rumble + grit + D2/A2 drone, 6 s seamless."),
    Cue("surfacing_sparkle", "oneshot_3d", surfacing_sparkle, volume=(0.8, 0.8), fade_out=0.25, milestone="M2",
        tonal=True, notes="Rising glassy run D5..D6 over an upward whoosh."),
    Cue("ui_menu_open", "ui_2d", ui_menu_open, volume=(0.6, 0.6), fade_out=0.15, tonal=True,
        notes="UiCue MenuOpen: felt piano D4 A4 D5 rolled upward."),
    Cue("ui_menu_close", "ui_2d", ui_menu_close, volume=(0.5, 0.5), fade_out=0.12, tonal=True,
        notes="UiCue MenuClose: felt piano A4 -> D4."),
    Cue("ui_focus", "ui_2d", ui_focus, variants=len(UI_FOCUS_NOTES), variant_labels=("A4", "B4", "D5"),
        volume=(0.26, 0.3), pitch=(0.985, 1.015), fade_out=0.02, tonal=True,
        notes="UiCue FocusMove: tiny wooden tick; the runtime rate-limits and softens rapid repeats."),
    Cue("ui_slider", "ui_2d", ui_slider, variants=len(UI_SLIDER_NOTES), variant_labels=("D5", "E5"),
        volume=(0.22, 0.26), pitch=(0.985, 1.015), fade_out=0.02, tonal=True,
        notes="UiCue SliderStep: muted kalimba tap; rate-limited and softened under repeats."),
    Cue("ui_prompt", "ui_2d", ui_prompt, volume=(0.4, 0.4), fade_out=0.1, tonal=True,
        notes="UiCue PromptShown: barely-there E5 -> A5 felt-piano grace note."),
    Cue("ui_hold_fill", "ui_2d", ui_hold_fill, volume=(0.45, 0.45), fade_out=0.05,
        notes="UiCue HoldFill: D5/A5 swell rising over the 0.6 s hold (UI TowerPanelSettings.HoldSeconds); "
              "stopped on release/complete."),
    Cue("ui_hold_complete", "ui_2d", ui_hold_complete, volume=(0.55, 0.55), fade_out=0.2, tonal=True,
        notes="UiCue HoldComplete: soft resolved D major felt-piano chord + music-box D6."),
    Cue("ui_card", "ui_2d", ui_card, volume=(0.5, 0.5), fade_out=0.25, tonal=True,
        notes="UiCue CardShown: music-box phrase A5 F#5 A5 -> D6 over D5."),
    Cue("ui_confirm", "ui_2d", ui_confirm, volume=(0.7, 0.7), fade_out=0.08, tonal=True, notes="Rising D5 -> A5."),
    Cue("ui_back", "ui_2d", ui_back, volume=(0.65, 0.65), fade_out=0.08, tonal=True, notes="Falling A4 -> D4."),
    Cue("upgrade_arpeggio", "stinger_2d", upgrade_arpeggio, volume=(0.8, 0.8), fade_out=0.3, milestone="M2",
        tonal=True, notes="Soft kalimba D4 A4 D5 F#5 A5, stereo, over a quiet D/A pad."),
)
