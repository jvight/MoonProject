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
from synth.fm import operator, two_op
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


# --------------------------------------------------------------------------------------------------- friends: Tilly

def _blip(duration: float, start: str, end: str, glide: float, gen, amp: float = 1.0, index: float = 0.7,
          vibrato_cents: float = 0.0, vibrato_rate: float = 9.0, formant=(900.0, 1700.0)) -> np.ndarray:
    """One bird-like blip: a soft 2-operator FM tone that glides from ``start`` into ``end`` (time constant
    ``glide`` seconds) and holds it, with a gently moving formant ('oo' -> 'ee') and an optional flutter."""
    n = samples(duration)
    freq = osc.glide(n, note_freq(start), note_freq(end), time_constant=glide)
    if vibrato_cents:
        freq = freq * 2.0 ** (vibrato_cents * np.sin(2.0 * math.pi * vibrato_rate * np.arange(n) / SAMPLE_RATE)
                              / 1200.0)
    tone = two_op(n, freq, 2.0, index * envelope.exp_decay(n, max(duration, 0.05) * 1.5))
    shaped = filters.swept(tone, "bandpass", osc.glide(n, formant[0], formant[1]), q=2.0)
    env = envelope.segments(n, [(0.0, 0.0), (0.008, 1.0), (0.65 * duration, 0.75), (duration, 0.0)],
                            shape="smooth")
    return amp * (0.6 * tone + 0.8 * shaped) * env


def _phrase(parts, gen, tail: float = 0.25) -> np.ndarray:
    """Places blips ``[(time_s, blip_array), ...]`` in a buffer with a short tail."""
    length = max(t + b.shape[0] / SAMPLE_RATE for t, b in parts) + tail
    out = np.zeros(samples(length))
    for t, b in parts:
        place(out, b, samples(t))
    return out


def _chirp_finish(x: np.ndarray, cutoff: float = 5000.0, room: float = 0.35, wet: float = 0.12) -> np.ndarray:
    return _mono_reverb(filters.lowpass(filters.highpass(x, 300.0), cutoff), room=room, damping=0.6, wet=wet,
                        dry=1.0)


TILLY_CURIOUS = (("D5", "A5"), ("E5", "B5"), ("F#5", "D6"))


def tilly_curious(variant, gen):
    """Curious 'hm?': a short blip, then a slower slide up a fourth/fifth that holds like a question."""
    low, high = TILLY_CURIOUS[variant]
    return _chirp_finish(_phrase([
        (0.0, _blip(0.07, high, low, 0.008, gen, amp=0.6)),
        (0.12, _blip(0.26, low, high, 0.022, gen, vibrato_cents=8.0)),
    ], gen))


TILLY_HAPPY = (("A5", "B5", "D6", "E6"), ("D6", "B5", "D6", "E6"), ("F#5", "A5", "B5", "D6"))


def tilly_happy(variant, gen):
    """Happy bouncing trill climbing the pentatonic."""
    notes = TILLY_HAPPY[variant]
    parts = []
    for k, note in enumerate(notes):
        last = k == len(notes) - 1
        prev = notes[k - 1] if k else note
        parts.append((0.075 * k, _blip(0.16 if last else 0.06, prev, note, 0.008, gen, amp=0.7 + 0.08 * k,
                                       vibrato_cents=18.0 if last else 0.0)))
    return _chirp_finish(_phrase(parts, gen))


TILLY_SLEEPY = (("A5", "F#5"), ("D5", "A4"))


def tilly_sleepy(variant, gen):
    """Sleepy coo on the perch: a slow falling note with a breath, then a smaller, lower echo."""
    high, low = TILLY_SLEEPY[variant]
    coo = _blip(0.5, high, low, 0.12, gen, amp=0.8, index=0.4, vibrato_cents=10.0, vibrato_rate=4.0,
                formant=(1400.0, 800.0))
    echo = _blip(0.3, low, low, 0.02, gen, amp=0.35, index=0.3, formant=(1000.0, 700.0))
    n = samples(0.5)
    breath = filters.bandpass(noise.pink(n, gen), 900.0, 0.8) * envelope.segments(
        n, [(0.0, 0.0), (0.15, 1.0), (0.5, 0.0)], shape="smooth")
    breath = 0.25 * breath / max(float(np.std(breath)), 1e-9) * 0.1
    phrase = _phrase([(0.0, coo + breath), (0.55, echo)], gen, tail=0.3)
    return _chirp_finish(phrase, cutoff=3500.0, wet=0.16)


TILLY_GREETING = (("D5", "A5", "B5", "D6"), ("E5", "B5", "A5", "D6"), ("A4", "D5", "F#5", "A5"))


def tilly_greeting(variant, gen):
    """'Hello!' when she flies out to meet 07: a rising swoop, then a quick little trill landing high."""
    a, b, c, d = TILLY_GREETING[variant]
    return _chirp_finish(_phrase([
        (0.0, _blip(0.17, a, b, 0.025, gen, amp=0.9)),
        (0.2, _blip(0.06, b, c, 0.008, gen, amp=0.6)),
        (0.27, _blip(0.06, c, b, 0.008, gen, amp=0.6)),
        (0.34, _blip(0.24, b, d, 0.012, gen, amp=0.85, vibrato_cents=10.0)),
    ], gen))


TILLY_EXCITED = (("D5", "F#5", "A5", "D6", "E6", "D6"), ("A5", "B5", "D6", "B5", "D6", "E6"),
                 ("F#5", "A5", "B5", "D6", "F#6", "E6"))


def tilly_excited(variant, gen):
    """Excited (a relic is home): a fast fluttering arpeggio that skips upward and lands with a flutter."""
    notes = TILLY_EXCITED[variant]
    parts = []
    for k, note in enumerate(notes):
        last = k == len(notes) - 1
        prev = notes[k - 1] if k else note
        parts.append((0.055 * k, _blip(0.22 if last else 0.05, prev, note, 0.006, gen, amp=0.65 + 0.05 * k,
                                       vibrato_cents=14.0 if last else 0.0, vibrato_rate=11.0)))
    return _chirp_finish(_phrase(parts, gen))


TILLY_BROKEN = (("A5", "D5"), ("B5", "E5"), ("D6", "A5"))


def tilly_broken(variant, gen):
    """A broken, hopeful answer from the crater floor: a curious chirp that stutters, sags an octave mid-way and
    tries again, faint and muffled under the dust, with a whisper of crackle."""
    high, low = TILLY_BROKEN[variant]
    first = _blip(0.18, low, high, 0.02, gen, amp=0.8)
    sag = _blip(0.12, high, low, 0.015, gen, amp=0.4, index=0.3)
    retry = _blip(0.26, low, high, 0.025, gen, amp=0.7, vibrato_cents=10.0)
    phrase = _phrase([(0.0, first), (0.2, sag), (0.36, retry)], gen, tail=0.3)
    n = phrase.shape[0]
    gate = np.ones(n)
    t = samples(0.03)
    while t < n:
        if gen.random() < 0.35:
            width = samples(gen.uniform(0.012, 0.03))
            ramp = samples(0.002)
            dip = np.concatenate((np.linspace(1.0, 0.1, ramp), np.full(max(0, width - 2 * ramp), 0.1),
                                  np.linspace(0.1, 1.0, ramp)))
            gate[t:t + dip.shape[0]] *= dip[:max(0, n - t)]
        t += samples(gen.uniform(0.03, 0.07))
    crackle = _grains(n, gen, 14.0, (0.0006, 0.0015), (0.0002, 0.0002), (0.0005, 0.001), 0.04, 0.6)
    crackle = filters.lowpass(filters.bandpass(crackle, 1500.0, 0.9), 3000.0)
    return _chirp_finish(phrase * gate + crackle, cutoff=3000.0, wet=0.2)


TILLY_FOUND = (("A5", "D6"), ("B5", "E6"), ("F#5", "A5"))


def tilly_found(variant, gen):
    """'Found it!' as Tilly hovers over something: a quick hop up that lands bright with a little flutter, and a
    tiny echo of the landing note."""
    low, high = TILLY_FOUND[variant]
    return _chirp_finish(_phrase([
        (0.0, _blip(0.06, low, low, 0.006, gen, amp=0.6)),
        (0.08, _blip(0.2, low, high, 0.012, gen, amp=0.9, vibrato_cents=10.0, vibrato_rate=10.0)),
        (0.32, _blip(0.07, high, high, 0.006, gen, amp=0.4)),
    ], gen))


def friend_spot_ping(_variant, gen):
    """The soft ping marking what a friend spotted: a round A5 bell with its octave shimmering a moment later,
    gentler and higher than 07's kalimba sonar."""
    n = samples(1.8)
    bell = instruments.soft_bell(note_freq("A5"), 1.8, decay=1.3, attack=0.012)
    place(bell, instruments.soft_bell(note_freq("A6"), 1.8, decay=0.9, attack=0.012)[:n - samples(0.07)],
          samples(0.07), 0.25)
    bell = filters.lowpass(effects.tremolo(bell, 6.0, 0.18), 5500.0)
    return _mono_reverb(bell, room=0.55, damping=0.6, wet=0.25, dry=1.0)


TILLY_ROTOR_LOOP_S = 4.0


def tilly_rotor(_variant, gen):
    """Tiny four-rotor hum: blade-pass tones around A3 that beat slowly against each other, a soft whirr of air
    pulsing at the blade rate. Whole cycles in the loop; nothing above ~3.5 kHz so it pitches up cleanly."""
    n = samples(TILLY_ROTOR_LOOP_S)
    step = SAMPLE_RATE / n
    base = loop_freq(note_freq("A3"), n)
    buzz = np.zeros(n)
    for offset, amp, phase in ((0.0, 1.0, 0.0), (step, 0.8, 0.31), (2.0 * step, 0.7, 0.57), (-step, 0.75, 0.83)):
        buzz += amp * osc.additive(n, base + offset, [(1, 1.0), (2, 0.45), (3, 0.2), (4, 0.08), (6, 0.03)],
                                   phase=phase)
    whirr = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 950.0, 0.9))
    whirr *= envelope.lfo(n, base, 0.45, 0.55)
    mix = 0.22 * buzz + 1.6 * whirr / max(float(np.std(whirr)), 1e-9) * 0.1
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(filters.lowpass(x, 3500.0), 3500.0), 90.0))


STITCH_LOOP_S = 4.0
STITCH_NOTES = ("D6", "E6", "F#6", "A6")


def friend_stitch(_variant, gen):
    """Repair 'stitching': a warm D/A shimmer, a soft sewing-machine pulse on D3, and tiny needle-like pentatonic
    ticks and thread pulls scattered over it, in a little room. Seamless; the runtime lifts it over the repair."""
    n = samples(STITCH_LOOP_S)
    step = SAMPLE_RATE / n
    pad = np.zeros(n)
    for note, amp in (("D4", 1.0), ("A4", 0.7), ("D5", 0.4)):
        f = loop_freq(note_freq(note), n)
        pad += amp * (osc.sine(n, f) + 0.6 * osc.sine(n, f + step, phase=0.4))
    pad *= envelope.lfo(n, 12.0 / STITCH_LOOP_S, 0.15, 0.85)
    pulse = osc.additive(n, loop_freq(note_freq("D3"), n), [(1, 1.0), (2, 0.3)])
    pulse *= envelope.lfo(n, 24.0 / STITCH_LOOP_S, 0.5, 0.5) ** 2
    ticks = np.zeros(n)
    for pos in noise.event_times(n, gen, 13.0):
        note = STITCH_NOTES[int(gen.integers(0, len(STITCH_NOTES)))]
        tick = instruments.partial(samples(0.05), note_freq(note), gen.uniform(0.4, 1.0), 0.0015, 0.04)
        place(ticks, tick, int(pos), wrap=True)
    pulls = _grains(n, gen, 5.0, (0.02, 0.04), (0.006, 0.01), (0.015, 0.03), 0.3, 0.4)
    pulls = periodic(pulls, lambda x: filters.lowpass(filters.bandpass(x, 1800.0, 1.4), 4000.0))
    dry = 0.22 * pad + 0.18 * pulse + 0.5 * ticks + 0.5 * pulls
    wet = periodic(dry, lambda x: to_mono(effects.reverb(x, room=0.55, damping=0.6, wet=0.35, dry=1.0)),
                   warmup=samples(3.0))
    return periodic(wet, lambda x: filters.lowpass(x, 6000.0))


def friend_boot(_variant, gen):
    """The repaired friend boots: three soft electric flickers (the eye), a little power-up glide on D3, then a
    music-box startup jingle D5 F#5 A5 D6."""
    n = samples(2.4)
    mix = np.zeros(n)
    for when, gain in ((0.0, 0.5), (0.11, 0.35), (0.18, 0.6)):
        flick = instruments.partial(samples(0.03), note_freq("A6"), gain, 0.001, 0.02)
        click = filters.bandpass(noise.white(samples(0.004), gen), 2000.0, 1.0) * 0.1
        place(mix, flick, samples(when))
        place(mix, click, samples(when))
    rise_n = samples(0.5)
    rise = osc.sine(rise_n, osc.glide(rise_n, note_freq("D2"), note_freq("D3")))
    rise *= envelope.segments(rise_n, [(0.0, 0.0), (0.3, 1.0), (0.5, 0.0)], shape="smooth")
    place(mix, rise, samples(0.12), 0.35)
    for k, note in enumerate(("D5", "F#5", "A5", "D6")):
        last = k == 3
        place(mix, instruments.music_box(note_freq(note), 1.6 if last else 0.9, decay=1.3 if last else 0.7),
              samples(0.48 + 0.12 * k), 0.7 if last else 0.55)
    return _mono_reverb(filters.lowpass(mix, 5500.0), room=0.45, damping=0.6, wet=0.2, dry=1.0)


FRIEND_PART_STEPS = ("D5", "F#5", "A5", "B5")


def _amber_bar(freq: float, duration: float) -> np.ndarray:
    """Warm vibraphone-like bar (the 'amber' of friend parts, unlike the glassy scrap chime): fundamental,
    the bar's 4x mode dying fast, and a slow motor tremolo."""
    n = samples(duration)
    bar = (instruments.partial(n, freq, 1.0, 0.002, 1.6)
           + instruments.partial(n, 4.0 * freq, 0.22, 0.0015, 0.25)
           + instruments.partial(n, 2.0 * freq, 0.06, 0.002, 0.6))
    return effects.tremolo(bar, 5.2, 0.3)


def friend_part(variant, _gen):
    """A friend part collected: a warm amber bar climbing D5 F#5 A5 B5 per part, and on the last part a resolved
    A5 -> D6 figure over a soft D5."""
    if variant < len(FRIEND_PART_STEPS):
        mix = _amber_bar(note_freq(FRIEND_PART_STEPS[variant]), 1.6)
        mix += 0.25 * _amber_bar(note_freq("D4"), 1.6)
    else:
        mix = np.zeros(samples(2.2))
        place(mix, _amber_bar(note_freq("D5"), 2.0), 0, 0.35)
        place(mix, _amber_bar(note_freq("A5"), 2.0), 0, 0.8)
        place(mix, _amber_bar(note_freq("D6"), 2.0), samples(0.14), 0.9)
    return _mono_reverb(filters.lowpass(mix, 6000.0), room=0.5, damping=0.55, wet=0.22, dry=1.0)


# --------------------------------------------------------------------------------------------------- hover-jump

JUMP_CHARGE_LOOP_S = 4.0


def jump_charge(_variant, gen):
    """The Hover-Jump charging: a soft electric hum on D4 (the runtime steps it up D E F# A B as the charge grows)
    with a gentle tremolo and a quiet spring-creak texture underneath. Seamless; < 3.5 kHz so the climb stays clean."""
    n = samples(JUMP_CHARGE_LOOP_S)
    step = SAMPLE_RATE / n
    f = loop_freq(note_freq("D4"), n)
    hum = osc.additive(n, f, [(1, 1.0), (2, 0.28), (3, 0.1), (4, 0.04)])
    hum += 0.45 * osc.additive(n, f + step, [(1, 1.0), (2, 0.2)], phase=0.27)
    hum *= envelope.lfo(n, 24.0 / JUMP_CHARGE_LOOP_S, 0.12, 0.88)
    excitation = np.zeros(n)
    for pos in noise.event_times(n, gen, 26.0, jitter=0.6):
        excitation[int(pos)] += gen.uniform(0.3, 1.0)
    creak = periodic(excitation, lambda x: 0.6 * filters.bandpass(x, 310.0, 8.0) + 0.35 * filters.bandpass(
        x, 520.0, 7.0), warmup=samples(0.5))
    creak *= 1.0 / max(float(np.std(creak)), 1e-9) * 0.05
    return periodic(0.3 * hum + creak, lambda x: filters.highpass(filters.lowpass(x, 3200.0), 70.0))


JUMP_LEAP_SIZES = ("hop", "leap")


def jump_leap(variant, gen):
    """The leap: a soft rising 'boing' (a wobbling D4/A3 spring tone settling into tune) over a whoosh of air;
    the hop is small and short, the leap longer and fuller."""
    big = variant == 1
    duration = 1.1 if big else 0.5
    n = samples(duration)
    base = note_freq("A3" if big else "D4")
    t = np.arange(n) / SAMPLE_RATE
    wobble = 1.0 + 0.05 * np.exp(-t / (0.16 if big else 0.08)) * np.sin(2.0 * math.pi * 11.0 * t)
    rise = osc.glide(n, 0.82 * base, base, time_constant=0.05)
    boing = osc.additive(n, rise * wobble, [(1, 1.0), (2, 0.18), (3, 0.05)])
    boing *= envelope.ar(n, 0.006, 0.6 if big else 0.28)
    sweep_n = samples(0.9 if big else 0.35)
    whoosh = filters.swept(noise.pink(sweep_n, gen), "bandpass", osc.glide(sweep_n, 280.0, 1300.0), q=1.1)
    whoosh *= envelope.segments(sweep_n, [(0.0, 0.0), (0.2 if big else 0.08, 1.0),
                                          (0.9 if big else 0.35, 0.0)], shape="smooth")
    whoosh = whoosh / max(float(np.std(whoosh)), 1e-9) * 0.08
    mix = 0.55 * boing
    place(mix, whoosh, 0, 1.0 if big else 0.6)
    return filters.lowpass(mix, 4500.0)


def coil_twang(variant, gen):
    """The coils springing out on the leap: a soft plucked spring on A3/D4 with the spring's falling dispersive
    'tw-' chirp, felt-soft."""
    note = ("A3", "D4")[variant]
    n = samples(0.8)
    string = karplus_strong(n, note_freq(note), gen, decay=0.993, brightness=0.3, pick_softness=1800.0)
    chirp_n = samples(0.09)
    chirp = osc.sine(chirp_n, osc.glide(chirp_n, 1300.0, 320.0)) * envelope.ar(chirp_n, 0.002, 0.07)
    mix = string.copy()
    place(mix, chirp, 0, 0.12)
    return filters.lowpass(mix, 4000.0)


AIR_WIND_LOOP_S = 6.0


def air_wind(_variant, gen):
    """Air rushing past during a leap: two soft band-passed noise layers (no whistle) breathing slowly.
    Seamless; the runtime swells it with air time and speed."""
    n = samples(AIR_WIND_LOOP_S)
    low = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 520.0, 0.6),
                   warmup=samples(1.0))
    high = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 1400.0, 0.9),
                    warmup=samples(1.0))
    low *= envelope.lfo(n, 2.0 / AIR_WIND_LOOP_S, 0.2, 0.8)
    high *= envelope.lfo(n, 3.0 / AIR_WIND_LOOP_S, 0.3, 0.7, phase=0.25)
    mix = low / max(float(np.std(low)), 1e-9) + 0.5 * high / max(float(np.std(high)), 1e-9)
    return periodic(mix, lambda x: filters.lowpass(filters.lowpass(x, 2800.0), 2800.0))


JUMP_LAND_SETTINGS = (
    # thud start/end Hz, cushion T60, sproing note
    (80.0, 52.0, 0.35, "D3"),
    (72.0, 48.0, 0.42, "E3"),
)


def jump_land(variant, gen):
    """A cushioned landing after a leap: the air cushion's soft 'pfff', a muted low thud, the springs taking the
    weight with a little sproing, and dust settling. Much gentler than the hard landing thump."""
    f_start, f_end, cushion_t60, spring_note = JUMP_LAND_SETTINGS[variant]
    n = samples(1.4)
    puff = filters.highpass(filters.lowpass(filters.lowpass(noise.pink(n, gen), 900.0), 900.0), 150.0)
    puff *= envelope.ar(n, 0.015, cushion_t60)
    puff = puff / max(float(np.std(puff)), 1e-9) * 0.11
    thud = osc.sine(n, osc.glide(n, 1.4 * f_start, f_end, time_constant=0.05)) * envelope.ar(n, 0.025, 0.25)
    t = np.arange(n) / SAMPLE_RATE
    spring_f = note_freq(spring_note) * (1.0 + 0.03 * np.exp(-t / 0.12) * np.sin(2.0 * math.pi * 9.0 * t))
    spring = osc.sine(n, spring_f) * envelope.ar(n, 0.02, 0.35)
    dust = filters.lowpass(filters.highpass(noise.pink(n, gen), 1400.0), 4200.0) * envelope.ar(n, 0.06, 0.9)
    dust = dust / max(float(np.std(dust)), 1e-9) * 0.004
    return puff + 0.16 * thud + 0.15 * spring + dust


def coil_pop(_variant, gen):
    """The coils popping in under 07 after the purchase: a small mechanical clunk, then a springy sproing on D4."""
    n = samples(1.0)
    clunk = filters.bandpass(noise.white(n, gen), 260.0, 1.4) * envelope.ar(n, 0.002, 0.06)
    clunk = clunk / max(float(np.std(clunk[:samples(0.05)])), 1e-9) * 0.08
    clunk += 0.25 * osc.sine(n, osc.glide(n, 150.0, 110.0, time_constant=0.03)) * envelope.ar(n, 0.004, 0.09)
    t = np.arange(n) / SAMPLE_RATE
    sproing_f = osc.glide(n, note_freq("A3"), note_freq("D4"), time_constant=0.04) * (
        1.0 + 0.06 * np.exp(-t / 0.18) * np.sin(2.0 * math.pi * 10.0 * t))
    sproing = osc.additive(n, sproing_f, [(1, 1.0), (2, 0.15)]) * envelope.ar(n, 0.01, 0.55)
    mix = clunk.copy()
    place(mix, 0.6 * sproing[:n - samples(0.06)], samples(0.06))
    return filters.lowpass(mix, 4000.0)


# Gameplay fires the bench's spark particle bursts at these times (s) after the purchase; the crackles match them.
WORKBENCH_SPARK_BURSTS = (0.0, 0.16, 0.32)


def workbench_upgrade(_variant, gen):
    """Buying an upgrade at Kenji's workbench: three soft spark crackles in time with the bench's spark bursts and a
    toolbox rattle, then a resolved cadence (felt piano A4 + E5 settling into D major with a music-box D6), distinct
    from the tower's arpeggio."""
    n = samples(3.2)
    sparks = np.zeros(n)
    for burst, gain in zip(WORKBENCH_SPARK_BURSTS, (1.0, 0.8, 0.65)):
        burst_n = samples(0.14)
        crackle = _grains(burst_n, gen, 70.0, (0.002, 0.006), (0.0003, 0.0006), (0.002, 0.005), 0.25, 0.6)
        crackle *= envelope.segments(burst_n, [(0.0, 0.0), (0.008, 1.0), (0.14, 0.0)], shape="smooth")
        place(sparks, crackle, samples(burst), gain)
    sparks = filters.lowpass(filters.bandpass(sparks, 3000.0, 0.9), 5000.0)
    rattle = np.zeros(n)
    for k in range(7):
        freq = gen.uniform(900.0, 1500.0)
        clink_n = samples(0.12)
        clink = (instruments.partial(clink_n, freq, 1.0, 0.0008, 0.06)
                 + instruments.partial(clink_n, 1.53 * freq, 0.5, 0.0008, 0.04)
                 + instruments.partial(clink_n, 2.31 * freq, 0.2, 0.0008, 0.025))
        place(rattle, clink, samples(0.05 + 0.045 * k + gen.uniform(0.0, 0.02)), gen.uniform(0.4, 0.8))
    rattle = filters.lowpass(rattle, 4500.0)
    chord = np.zeros(n)
    place(chord, _rolled(("A4", "E5"), 0.03, 1.0, gen, decay=0.8, brightness=0.45), samples(0.38), 0.6)
    place(chord, _rolled(("D4", "F#4", "A4", "D5"), 0.02, 2.4, gen, decay=1.6, brightness=0.45,
                         gains=(0.7, 0.55, 0.55, 0.6)), samples(0.72))
    place(chord, instruments.music_box(note_freq("D6"), 2.0, decay=1.3), samples(0.78), 0.2)
    mono = 0.6 * sparks + 0.5 * rattle + chord
    wet = effects.reverb(mono, room=0.5, damping=0.6, wet=0.22, dry=0.0, width=0.85)
    dry = np.stack([0.6 * sparks + 0.5 * rattle * 0.8 + chord, 0.6 * sparks * 0.8 + 0.5 * rattle + chord], axis=1)
    return dry + wet


# ---------------------------------------------------------------------------------------------- Bell, radio, canyon

def _old_radio(x: np.ndarray, drive: float = 1.6) -> np.ndarray:
    """An old receiver's voice: band-limited 280 Hz - 2.6 kHz, gently saturated, slightly boxy."""
    band = filters.lowpass(filters.lowpass(filters.highpass(x, 280.0), 2600.0), 2600.0)
    return effects.saturate(filters.peaking(band, 900.0, 3.0, 1.2), drive) / drive


def _radio_blip(note: str, duration: float, gen, amp: float = 1.0) -> np.ndarray:
    """A short warbling tone as an old radio would sound it (AM-filtered, a touch of flutter)."""
    n = samples(duration)
    freq = osc.vibrato(n, note_freq(note), 7.0, 12.0)
    tone = osc.additive(n, freq, [(1, 1.0), (2, 0.25), (3, 0.1)]) * envelope.ar(n, 0.006, duration * 0.9)
    return amp * tone


def _static(n: int, gen, centre: float = 1400.0, q: float = 0.8) -> np.ndarray:
    hiss = filters.bandpass(noise.white(n, gen), centre, q)
    crackle = _grains(n, gen, 30.0, (0.0005, 0.0015), (0.0002, 0.0002), (0.0004, 0.001), 0.5, 0.7)
    return hiss / max(float(np.std(hiss)), 1e-9) * 0.3 + filters.bandpass(crackle, 1800.0, 0.9)


def bell_broken(variant, gen):
    """Bell, dormant, answering a ping: a dying set catching for a moment - faint crackle, the needle drifting
    past a station, a fragment of a note that fades away."""
    n = samples(1.2)
    static = _static(n, gen) * envelope.segments(n, [(0.0, 0.0), (0.06, 0.8), (0.5, 0.5), (1.2, 0.0)],
                                                  shape="smooth")
    note = ("A4", "D5")[variant]
    fragment = np.zeros(n)
    place(fragment, _radio_blip(note, 0.45, gen, 0.9), samples(0.28))
    fragment *= envelope.segments(n, [(0.0, 1.0), (0.55, 1.0), (0.75, 0.25), (1.2, 0.0)], shape="smooth")
    return _old_radio(0.5 * static + fragment)


def bell_excited(variant, gen):
    """Bell's happy crackle when a relic comes home: a bright static burst, then two quick warbling blips up
    the scale, like a station cheering through the speaker."""
    n = samples(0.95)
    burst = _static(n, gen, 1800.0, 0.9) * envelope.segments(n, [(0.0, 0.0), (0.01, 1.0), (0.18, 0.0)],
                                                            shape="smooth")
    first, second = (("D5", "A5"), ("E5", "B5"))[variant]
    mix = 0.45 * burst
    place(mix, _radio_blip(first, 0.12, gen, 0.8), samples(0.16))
    place(mix, _radio_blip(second, 0.3, gen, 1.0), samples(0.3))
    return _old_radio(mix, 1.4)


def bell_tune(variant, gen):
    """Bell's station-switch crackle: the detent's tick, a swish of static as the needle moves, a resonant
    whistle-free sweep between stations."""
    n = samples(0.5)
    tick = filters.bandpass(noise.white(n, gen), 2000.0, 1.2) * envelope.ar(n, 0.0005, 0.008)
    sweep = filters.swept(noise.white(n, gen), "bandpass",
                          osc.glide(n, (700.0, 1100.0)[variant], (1500.0, 650.0)[variant]), q=3.0)
    sweep *= envelope.segments(n, [(0.0, 0.0), (0.05, 1.0), (0.3, 0.6), (0.5, 0.0)], shape="smooth")
    return _old_radio(0.3 * tick / max(float(np.max(np.abs(tick))), 1e-9) + sweep / max(float(np.std(sweep)),
                                                                                         1e-9) * 0.15)


BELL_STEP_PITCH_HZ = (520.0, 610.0, 470.0)


def bell_step(variant, gen):
    """A light tripod-foot tap: a tiny rubber-tipped click with a hollow little knock from the cabinet."""
    n = samples(0.12)
    click = filters.bandpass(noise.white(n, gen), 2400.0, 1.4) * envelope.ar(n, 0.0004, 0.006)
    click = click / max(float(np.max(np.abs(click))), 1e-9) * 0.25
    knock = osc.sine(n, osc.glide(n, 1.2 * BELL_STEP_PITCH_HZ[variant], BELL_STEP_PITCH_HZ[variant],
                                  time_constant=0.004)) * envelope.ar(n, 0.0008, 0.035)
    return filters.lowpass(click + 0.5 * knock, 4500.0)


BELL_DOZE_LOOP_S = 6.0


def bell_doze(_variant, gen):
    """Bell dozing: the dial lamp's ember hum on D2/D3, a faint warm A3 glow tone and a valve's rare soft tick.
    Seamless and very soft."""
    n = samples(BELL_DOZE_LOOP_S)
    step = SAMPLE_RATE / n
    d2 = loop_freq(note_freq("D2"), n)
    hum = osc.additive(n, d2, [(1, 0.6), (2, 1.0), (3, 0.3), (4, 0.15)]) + 0.4 * osc.sine(n, 2.0 * d2 + step,
                                                                                         phase=0.3)
    glow = osc.sine(n, loop_freq(note_freq("A3"), n), amp=0.18)
    breath = envelope.lfo(n, 2.0 / BELL_DOZE_LOOP_S, 0.2, 0.8)
    ticks = _grains(n, gen, 1.5, (0.001, 0.003), (0.0003, 0.0003), (0.001, 0.002), 0.25, 0.4)
    ticks = periodic(ticks, lambda x: filters.lowpass(filters.bandpass(x, 1500.0, 1.0), 3000.0))
    mix = (0.3 * hum + glow) * breath + ticks
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(x, 2200.0), 50.0))


def bell_wake(_variant, gen):
    """Bell wakes: the hum warms up from nothing, a little crackle, and a sleepy two-note hello E5 -> A5."""
    n = samples(1.4)
    hum = osc.additive(n, osc.glide(n, note_freq("D2") * 0.7, note_freq("D2")), [(1, 0.6), (2, 1.0), (3, 0.3)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.4, 1.0), (1.4, 0.0)], shape="smooth")
    crackle = _static(n, gen, 1500.0, 1.0) * envelope.segments(n, [(0.0, 0.0), (0.2, 0.6), (0.5, 0.0)],
                                                                shape="smooth")
    mix = 0.25 * hum + 0.3 * crackle
    place(mix, _radio_blip("E5", 0.14, gen, 0.6), samples(0.55))
    place(mix, _radio_blip("A5", 0.4, gen, 0.75), samples(0.72))
    return _old_radio(mix, 1.3)


def _whistle(notes, durations, gen, glide: float = 0.018, vibrato_cents: float = 0.0, vibrato_rate: float = 5.5,
             start_cents: float = 0.0, amp: float = 1.0) -> np.ndarray:
    """A heterodyne whistle as an old receiver makes it between stations: one continuous tone that slides from
    note to note (portamento ``glide`` seconds) instead of Tilly's separate bird-like blips. ``start_cents``
    makes it tune in from off-pitch, like a squeak settling onto a station."""
    total = float(sum(durations))
    n = samples(total)
    cents = np.zeros(n)
    t0 = 0
    target = np.zeros(n)
    for note, duration in zip(notes, durations):
        t1 = min(n, t0 + samples(duration))
        target[t0:t1] = 1200.0 * math.log2(note_freq(note) / note_freq(notes[0]))
        t0 = t1
    target[t0:] = target[t0 - 1] if t0 else 0.0
    alpha = 1.0 - math.exp(-1.0 / (glide * SAMPLE_RATE))
    value = start_cents
    for i in range(n):
        value += (target[i] - value) * alpha
        cents[i] = value
    t = np.arange(n) / SAMPLE_RATE
    if vibrato_cents:
        cents = cents + vibrato_cents * np.sin(2.0 * math.pi * vibrato_rate * t) * np.clip(t / 0.15, 0.0, 1.0)
    freq = note_freq(notes[0]) * 2.0 ** (cents / 1200.0)
    tone = osc.additive(n, freq, [(1, 1.0), (2, 0.18), (3, 0.08)])
    gates = np.zeros(n)
    t0 = 0
    for k, duration in enumerate(durations):
        seg = samples(duration)
        last = k == len(durations) - 1
        env = envelope.segments(seg, [(0.0, 0.35 if k else 0.0), (0.012, 1.0), (0.7 * duration, 0.85),
                                      (duration, 0.0 if last else 0.45)], shape="smooth")
        gates[t0:t0 + seg] = env[:max(0, min(seg, n - t0))]
        t0 += seg
    return amp * tone * gates


def _fading_station(x: np.ndarray, gen, depth: float = 0.25, rate: float = 3.0) -> np.ndarray:
    """Slow AM 'fading' of a distant station riding on the signal."""
    n = x.shape[0]
    phase = gen.uniform(0.0, 2.0 * math.pi)
    t = np.arange(n) / SAMPLE_RATE
    return x * (1.0 - depth * 0.5 * (1.0 + np.sin(2.0 * math.pi * rate * t + phase)))


def _bell_voice(phrase: np.ndarray, gen, crackle: float = 0.12, drive: float = 1.4) -> np.ndarray:
    """Bell's speaker: a little static riding under the whistle, then the old receiver's band and warmth."""
    bed = _static(phrase.shape[0], gen, 1500.0, 0.9)
    env = envelope.segments(phrase.shape[0], [(0.0, 0.0), (0.02, 1.0),
                                               (phrase.shape[0] / SAMPLE_RATE, 0.3)], shape="smooth")
    return _old_radio(phrase + crackle * bed * env, drive)


BELL_CURIOUS = (("A4", "D5"), ("D5", "E5"), ("E5", "A5"))


def bell_curious(variant, gen):
    """'Hm?': a tuning squeak settling onto a note, then the whistle sliding up like a question, fading as a
    distant station does."""
    low, high = BELL_CURIOUS[variant]
    phrase = _whistle((low, high), (0.16, 0.34), gen, glide=0.025, vibrato_cents=12.0, start_cents=-450.0)
    return _bell_voice(_fading_station(_pad(phrase, 0.12), gen), gen)


BELL_HAPPY = (("D5", "E5", "F#5", "A5"), ("A4", "D5", "E5", "A5"), ("F#5", "A5", "B5", "D6"))


def bell_happy(variant, gen):
    """Happy at home: the dial whistles up a little pentatonic run, gliding note to note, landing with a wobble."""
    notes = BELL_HAPPY[variant]
    phrase = _whistle(notes, (0.09, 0.09, 0.09, 0.3), gen, glide=0.012, vibrato_cents=10.0, start_cents=-200.0)
    return _bell_voice(_pad(phrase, 0.15), gen, crackle=0.1)


BELL_SLEEPY = (("A4", "F#4"), ("D5", "B4"))


def bell_sleepy(variant, gen):
    """Sleepy: a slow whistle sagging down like a tape winding down, under a soft hiss."""
    high, low = BELL_SLEEPY[variant]
    phrase = _whistle((high, low), (0.3, 0.7), gen, glide=0.06, vibrato_cents=12.0, vibrato_rate=3.5, amp=0.8)
    n = phrase.shape[0]
    hiss = filters.bandpass(noise.pink(n, gen), 1200.0, 0.7)
    hiss = hiss / max(float(np.std(hiss)), 1e-9) * 0.05 * envelope.segments(
        n, [(0.0, 0.0), (0.2, 1.0), (n / SAMPLE_RATE, 0.0)], shape="smooth")
    return _bell_voice(_pad(phrase + hiss, 0.2), gen, crackle=0.06, drive=1.2)


BELL_GREETING = (("D5", "F#5", "A5", "D6"), ("A4", "D5", "F#5", "A5"))


def bell_greeting(variant, gen):
    """'Hello!': a quick upward tuning squeak, then a bright station-ident whistle that holds its last note."""
    notes = BELL_GREETING[variant]
    phrase = _whistle(notes, (0.12, 0.1, 0.1, 0.42), gen, glide=0.014, vibrato_cents=14.0, start_cents=-700.0)
    return _bell_voice(_pad(phrase, 0.18), gen, crackle=0.14)


BELL_FOUND = (("A5", "D6"), ("E5", "A5"))


def bell_found(variant, gen):
    """'Got it!': a squeak sweeping down and locking onto a clean tone, then hopping up a fourth."""
    lock, hop = BELL_FOUND[variant]
    phrase = _whistle((lock, hop), (0.24, 0.3), gen, glide=0.02, vibrato_cents=8.0, start_cents=800.0)
    return _bell_voice(_pad(phrase, 0.15), gen, crackle=0.1)


def _pad(x: np.ndarray, tail: float) -> np.ndarray:
    return np.concatenate((x, np.zeros(samples(tail))))


BELL_WALK_LOOP_S = 4.0
BELL_WALK_TICKS = 24


def bell_rotor(_variant, gen):
    """Bell's walking effort (her 'rotor' loop): the soft clockwork of her four camera legs - an escapement's
    tick and tock, a tiny spring whirr and a faint cabinet creak. Seamless; nothing above ~3.5 kHz so it speeds up
    cleanly with her waddle."""
    n = samples(BELL_WALK_LOOP_S)
    ticks = np.zeros(n)
    tick_n = samples(0.05)
    for k in range(BELL_WALK_TICKS):
        freq = 1900.0 if k % 2 == 0 else 1350.0
        body = filters.bandpass(noise.white(tick_n, gen), freq, 2.0) * envelope.ar(tick_n, 0.0008, 0.012)
        body = body / max(float(np.max(np.abs(body))), 1e-9)
        knock = osc.sine(tick_n, 0.32 * freq) * envelope.ar(tick_n, 0.001, 0.02)
        place(ticks, 0.5 * body + 0.35 * knock, k * n // BELL_WALK_TICKS, 1.0 if k % 2 == 0 else 0.8, wrap=True)
    whirr = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 700.0, 1.2))
    whirr = whirr / max(float(np.std(whirr)), 1e-9) * 0.05 * envelope.lfo(n, 6.0 / BELL_WALK_LOOP_S, 0.2, 0.8)
    creak = periodic(noise.white(n, gen), lambda x: filters.swept(
        x, "bandpass", 560.0 + 80.0 * np.sin(2.0 * math.pi * 2.0 * np.arange(x.shape[0]) / n), q=8.0))
    creak = creak / max(float(np.std(creak)), 1e-9) * 0.04 * envelope.lfo(n, 2.0 / BELL_WALK_LOOP_S, 0.5, 0.5) ** 3
    mix = 0.5 * ticks + whirr + creak
    return periodic(mix, lambda x: filters.highpass(filters.lowpass(filters.lowpass(x, 3500.0), 3500.0), 120.0))


def bell_tape_slot(_variant, gen):
    """07's beam slides Bell's tape into her slot: a plastic slide, the clack of it seating and the little
    capstan motor catching."""
    n = samples(0.75)
    slide_n = samples(0.2)
    slide = filters.bandpass(noise.pink(slide_n, gen), 900.0, 1.0) * envelope.segments(
        slide_n, [(0.0, 0.0), (0.05, 1.0), (0.18, 0.0)], shape="smooth")
    clack_n = samples(0.08)
    clack = filters.bandpass(noise.white(clack_n, gen), 1700.0, 1.6) * envelope.ar(clack_n, 0.0006, 0.02)
    seat = osc.sine(clack_n, osc.glide(clack_n, 260.0, 190.0, time_constant=0.01)) * envelope.ar(clack_n, 0.001,
                                                                                                  0.03)
    motor_n = samples(0.45)
    motor = osc.additive(motor_n, osc.glide(motor_n, 70.0, note_freq("D3"), time_constant=0.12),
                         [(1, 0.5), (2, 1.0), (3, 0.4), (5, 0.15)])
    motor *= envelope.segments(motor_n, [(0.0, 0.0), (0.1, 1.0), (0.3, 0.6), (0.45, 0.0)], shape="smooth")
    mix = np.zeros(n)
    place(mix, slide / max(float(np.std(slide)), 1e-9) * 0.06, 0)
    place(mix, clack / max(float(np.max(np.abs(clack))), 1e-9) * 0.3 + 0.25 * seat, samples(0.17))
    place(mix, 0.06 * motor, samples(0.24))
    return filters.lowpass(filters.lowpass(mix, 4500.0), 4500.0)


BELL_SWEEP_STATIONS = ((0.25, "A4"), (0.48, "D5"), (0.72, "F#5"))


def bell_needle_sweep(_variant, gen):
    """Bell waking from her repair: the dial lamp's hum warms up and the needle sweeps up the band past three
    faint stations before it settles."""
    n = samples(1.6)
    sweep_n = samples(1.3)
    sweep = filters.swept(noise.white(sweep_n, gen), "bandpass", osc.glide(sweep_n, 450.0, 2200.0), q=2.5)
    sweep = sweep / max(float(np.std(sweep)), 1e-9) * 0.12 * envelope.segments(
        sweep_n, [(0.0, 0.0), (0.15, 1.0), (1.0, 0.7), (1.3, 0.0)], shape="smooth")
    hum = osc.additive(n, osc.glide(n, note_freq("D2") * 0.8, note_freq("D2"), time_constant=0.3),
                       [(1, 0.6), (2, 1.0), (3, 0.3)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.4, 1.0), (1.2, 0.8), (1.6, 0.0)], shape="smooth")
    mix = 0.08 * hum
    place(mix, sweep, samples(0.05))
    for when, note in BELL_SWEEP_STATIONS:
        place(mix, _radio_blip(note, 0.16, gen, 0.35), samples(when))
    return _old_radio(mix, 1.3)


def radio_dial_click(_variant, gen):
    """One detent of Bell's chunky dial: a bakelite catch and land with a small wooden knock."""
    n = samples(0.16)
    catch = filters.bandpass(noise.white(n, gen), 1600.0, 1.2) * envelope.ar(n, 0.0012, 0.01)
    land = np.zeros(n)
    place(land, filters.bandpass(noise.white(n, gen), 1300.0, 1.2)[:n - samples(0.018)] * envelope.ar(
        n - samples(0.018), 0.0004, 0.01), samples(0.018))
    knock = osc.sine(n, note_freq("A3")) * envelope.ar(n, 0.002, 0.05)
    mix = (0.2 * catch / max(float(np.max(np.abs(catch))), 1e-9)
           + 0.25 * land / max(float(np.max(np.abs(land))), 1e-9))
    return filters.lowpass(filters.lowpass(mix + 0.4 * knock, 4000.0), 4000.0)


def cassette_pickup(_variant, gen):
    """Collecting a tape: a plastic clack, a little spin of the reels speeding up, and a soft D6 tick of the
    counter - small and pleased."""
    n = samples(1.0)
    clack = filters.bandpass(noise.white(n, gen), 1600.0, 1.4) * envelope.ar(n, 0.0005, 0.02)
    clack = clack / max(float(np.max(np.abs(clack))), 1e-9) * 0.35
    spin_n = samples(0.55)
    rate = osc.glide(spin_n, 9.0, 26.0)
    spin_am = 0.5 + 0.5 * np.sin(2.0 * math.pi * np.cumsum(rate) / SAMPLE_RATE)
    whirr = filters.bandpass(noise.pink(spin_n, gen), 1100.0, 1.6) * spin_am
    whirr = whirr / max(float(np.std(whirr)), 1e-9) * 0.07 * envelope.segments(
        spin_n, [(0.0, 0.0), (0.08, 1.0), (0.4, 0.8), (0.55, 0.0)], shape="smooth")
    motor = osc.sine(spin_n, osc.glide(spin_n, note_freq("A3"), note_freq("D4")), amp=0.08) * envelope.segments(
        spin_n, [(0.0, 0.0), (0.1, 1.0), (0.55, 0.0)], shape="smooth")
    mix = clack.copy()
    place(mix, whirr + motor, samples(0.04))
    place(mix, instruments.music_box(note_freq("D6"), 0.4, decay=0.35), samples(0.58), 0.18)
    return filters.lowpass(mix, 5000.0)


def crew_log_found(_variant, gen):
    """Opening Ro's tin cache: a soft tin 'tink', the hinge's little creak and a whisper of paper."""
    n = samples(1.3)
    tink = (instruments.partial(n, 1150.0, 1.0, 0.001, 0.25) + instruments.partial(n, 1150.0 * 2.71, 0.25, 0.001,
                                                                                    0.08))
    excitation = np.zeros(n)
    t = samples(0.18)
    while t < samples(0.5):
        excitation[t] += gen.uniform(0.4, 1.0)
        t += samples(gen.uniform(0.008, 0.016))
    creak = filters.bandpass(excitation, 720.0, 8.0) + 0.5 * filters.bandpass(excitation, 1150.0, 6.0)
    creak = creak / max(float(np.max(np.abs(creak))), 1e-9) * 0.25
    rustle = _grains(n, gen, 60.0, (0.004, 0.012), (0.001, 0.002), (0.003, 0.008), 0.2, 0.5)
    rustle = filters.lowpass(filters.bandpass(rustle, 2600.0, 0.8), 4500.0) * envelope.segments(
        n, [(0.0, 0.0), (0.45, 0.0), (0.6, 1.0), (1.2, 0.0)], shape="smooth")
    return filters.lowpass(0.3 * tink + creak + 0.8 * rustle, 5000.0)


def bell_signal_pick(_variant, gen):
    """Bell's signal standing up at the pillar: a warm, very soft A4/D5/F#5 shimmer swelling and fading, far
    away."""
    pad = sum(instruments.soft_pad(note_freq(note), 4.0, 1.0, 1.8, hold=0.6, detune_cents=7.0)
              for note in ("A4", "D5", "F#5"))
    pad = filters.lowpass(effects.tremolo(pad, 5.0, 0.25), 3500.0)
    return _mono_reverb(pad, room=0.8, damping=0.6, wet=0.45, dry=1.0)


def bell_signal_found(_variant, gen):
    """Bell's signal found: a resolved two-note chime, A5 then D6, soft and round."""
    n = samples(2.2)
    mix = instruments.soft_bell(note_freq("A5"), 2.2, decay=1.4, attack=0.01)
    place(mix, instruments.soft_bell(note_freq("D6"), 2.2, decay=1.6, attack=0.01)[:n - samples(0.2)],
          samples(0.2), 0.9)
    return _mono_reverb(filters.lowpass(mix, 5500.0), room=0.6, damping=0.6, wet=0.28, dry=1.0)


CANYON_LOOP_S = 16.0
CANYON_WHISPER_BANDS = (
    # centre Hz, Q, LFO cycles per loop, phase
    (420.0, 2.2, 1.0, 0.0),
    (680.0, 2.4, 2.0, 0.3),
    (1050.0, 2.6, 3.0, 0.55),
    (1550.0, 2.8, 2.0, 0.8),
)


def canyon_whisper(_variant, gen):
    """Whispering Canyon's bed: breath-like air through narrow stone, its 'vowel' moving as four soft bands
    swell in turn (fixed filters, periodic swells: seamless), with a high, faint air layer. Stereo, very soft."""
    n = samples(CANYON_LOOP_S)
    sides = []
    for side in range(2):
        mix = np.zeros(n)
        for centre, q, cycles, phase in CANYON_WHISPER_BANDS:
            band = periodic(noise.white(n, gen), lambda x, c=centre, qq=q, sd=side: filters.bandpass(
                noise.pink_filter(x), c * (1.0 + 0.03 * sd), qq), warmup=samples(1.0))
            swell = envelope.lfo(n, cycles / CANYON_LOOP_S, 0.5, 0.5, phase=phase + 0.11 * side) ** 2
            mix += band / max(float(np.std(band)), 1e-9) * swell
        air = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 2600.0, 1.0),
                       warmup=samples(1.0))
        mix += 0.15 * air / max(float(np.std(air)), 1e-9)
        sides.append(periodic(mix, lambda x: filters.lowpass(filters.lowpass(x, 3200.0), 3200.0)))
    return np.stack(sides, axis=1)


def canyon_trough(_variant, gen):
    """The chasm trough's deeper, darker bed: low brown rumble breathing slowly, a hollow 180 Hz resonance and a
    faint D2 undertone. Stereo, seamless."""
    n = samples(CANYON_LOOP_S)
    d2 = loop_freq(note_freq("D2"), n)
    sides = []
    for side in range(2):
        rumble = periodic(noise.white(n, gen), lambda x: filters.lowpass(filters.lowpass(
            noise.brown_filter(x, 0.996), 320.0), 320.0), warmup=samples(1.0))
        hollow = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 180.0, 1.6),
                          warmup=samples(1.0))
        breath = envelope.lfo(n, 2.0 / CANYON_LOOP_S, 0.25, 0.75, phase=0.3 * side)
        mix = (rumble / max(float(np.std(rumble)), 1e-9) + 0.6 * hollow / max(float(np.std(hollow)), 1e-9)) * breath
        mix += 0.25 * osc.sine(n, d2, phase=0.2 * side)
        sides.append(periodic(mix, lambda x: filters.highpass(x, 30.0)))
    return np.stack(sides, axis=1)


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


# --------------------------------------------------------------------------------------------------- relays (M3-06)

def relay_mast_creak(_variant, gen):
    """An old relay mast straightening: a long, low metal groan rising as it comes upright (stick-slip pulses in a
    resonant body), a last little scrape and a soft clunk as it locks."""
    n = samples(2.3)
    groan_n = samples(1.7)
    rate = osc.glide(groan_n, 9.0, 22.0, time_constant=0.6)
    phase = np.cumsum(rate) / SAMPLE_RATE
    pulses = np.maximum(0.0, np.sin(2.0 * math.pi * phase)) ** 3
    friction = filters.bandpass(noise.white(groan_n, gen), 900.0, 1.2) * pulses
    body = filters.swept(friction, "bandpass", osc.glide(groan_n, 160.0, 260.0, time_constant=0.7), q=3.0)
    body = effects.saturate(body / max(float(np.std(body)), 1e-9), 0.6)
    body *= envelope.segments(groan_n, [(0.0, 0.0), (0.25, 1.0), (1.4, 0.8), (1.7, 0.0)], shape="smooth")
    scrape_n = samples(0.25)
    scrape = filters.bandpass(noise.pink(scrape_n, gen), 1400.0, 1.0) * envelope.segments(
        scrape_n, [(0.0, 0.0), (0.05, 1.0), (0.25, 0.0)], shape="smooth")
    clunk_n = samples(0.5)
    clunk = osc.sine(clunk_n, osc.glide(clunk_n, 120.0, 90.0, time_constant=0.03)) * envelope.ar(clunk_n, 0.003,
                                                                                                   0.18)
    clunk += 0.4 * filters.bandpass(noise.white(clunk_n, gen), 500.0, 1.5) * envelope.ar(clunk_n, 0.001, 0.03)
    mix = np.zeros(n)
    place(mix, 0.2 * body, 0)
    place(mix, 0.05 * scrape / max(float(np.std(scrape)), 1e-9), samples(1.55))
    place(mix, 0.3 * clunk, samples(1.75))
    return filters.lowpass(filters.highpass(mix, 50.0), 4500.0)


def relay_lamp_warm(_variant, gen):
    """The mast's lamp warming up: a filament's tiny catch, then a soft hum rising from D3 into a warm glow with its
    fifth and octave blooming over it."""
    n = samples(2.6)
    d3 = note_freq("D3")
    hum = osc.additive(n, osc.glide(n, 0.92 * d3, d3, time_constant=0.25), [(1, 1.0), (2, 0.4), (3, 0.15)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.5, 0.7), (1.6, 1.0), (2.6, 0.0)], shape="smooth")
    glow = (instruments.soft_pad(note_freq("A4"), 2.4, 0.7, 1.2, hold=0.4)
            + 0.7 * instruments.soft_pad(note_freq("D5"), 2.4, 0.9, 1.2, hold=0.3))
    catch_n = samples(0.1)
    catch = filters.bandpass(noise.white(catch_n, gen), 3000.0, 2.0) * envelope.ar(catch_n, 0.0005, 0.02)
    mix = 0.25 * hum
    place(mix, 0.12 * glow, samples(0.2))
    place(mix, 0.04 * catch / max(float(np.max(np.abs(catch))), 1e-9), 0)
    return filters.lowpass(mix, 5000.0)


RELAY_LINK_NOTES = (("A4", 0.0), ("D5", 0.2), ("F#5", 0.4))


def relay_link(_variant, gen):
    """The link: a short three-note answer as an old radio would sound it - A4, D5, then F#5 left ringing - with a
    breath of static, as if home had heard the new mast and replied."""
    n = samples(1.9)
    mix = np.zeros(n)
    for note, when in RELAY_LINK_NOTES:
        last = note == RELAY_LINK_NOTES[-1][0]
        place(mix, _radio_blip(note, 0.9 if last else 0.18, gen, 1.0 if last else 0.75), samples(when))
        place(mix, 0.5 * instruments.soft_bell(note_freq(note), 1.4 if last else 0.5, decay=1.0 if last else 0.4),
              samples(when))
    hiss = _static(n, gen, 1400.0, 0.8) * envelope.segments(n, [(0.0, 0.0), (0.1, 1.0), (1.0, 0.2), (1.9, 0.0)],
                                                            shape="smooth")
    return _old_radio(mix + 0.12 * hiss, 1.2)


def radio_hop_out(_variant, gen):
    """A radio-hop leaving: static swells up while a resonant band sweeps down and the sound filters out to a thin,
    soft hiss as the screen fades (the radio's own static holds the dark until the hop lands)."""
    n = samples(1.1)
    centre = osc.glide(n, 1800.0, 380.0, time_constant=0.35)
    env = envelope.segments(n, [(0.0, 0.0), (0.3, 1.0), (0.8, 0.6), (1.1, 0.0)], shape="smooth")
    sides = []
    for side in range(2):
        hiss = filters.lowpass(filters.highpass(noise.white(n, gen), 250.0), 2600.0)
        sweep = filters.swept(noise.white(n, gen), "bandpass", centre * (1.0 + 0.03 * side), q=4.0)
        sides.append(filters.lowpass(filters.lowpass((0.4 * hiss + 1.2 * sweep) * env, 3200.0), 3200.0))
    return np.stack(sides, axis=1)


def radio_hop_in(_variant, gen):
    """A radio-hop landing: the static's band sweeps up and locks onto the station - a soft D5 carrier emerges and
    the hiss settles away as the view eases back in."""
    n = samples(1.3)
    centre = osc.glide(n, 380.0, 1600.0, time_constant=0.3)
    hiss_env = envelope.segments(n, [(0.0, 0.6), (0.4, 1.0), (1.0, 0.15), (1.3, 0.0)], shape="smooth")
    carrier = osc.additive(n, note_freq("D5"), [(1, 1.0), (2, 0.15)]) * envelope.segments(
        n, [(0.0, 0.0), (0.45, 0.0), (0.75, 1.0), (1.3, 0.0)], shape="smooth")
    sides = []
    for side in range(2):
        sweep = filters.swept(noise.white(n, gen), "bandpass", centre * (1.0 + 0.03 * side), q=4.0)
        hiss = filters.lowpass(filters.highpass(noise.white(n, gen), 250.0), 2600.0)
        mix = (1.0 * sweep + 0.3 * hiss) * hiss_env + 0.5 * carrier
        sides.append(filters.lowpass(filters.lowpass(mix, 3200.0), 3200.0))
    return np.stack(sides, axis=1)


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


# Site names after their anchor's "site." prefix; the runtime maps SiteAnswered.SiteId to these labels.
SITE_ANSWER_NOTES = (("depot", "D4"), ("garage", "E4"), ("drill", "F#4"), ("kestrel", "A4"), ("lander", "B4"))
SITE_ANSWER_LABELS = tuple(label for site, _ in SITE_ANSWER_NOTES for label in (site, f"{site}_relic"))


def site_answer(variant, gen):
    """A salvage site answering 07's ping on its own note: a soft, hollow hull resonance swelling up (detuned twins,
    a slow beat) and settling. A site still holding a crew relic answers warmer: its octave and a D5 glow rise
    with it."""
    note = SITE_ANSWER_NOTES[variant // 2][1]
    holds_relic = variant % 2 == 1
    freq = note_freq(note)
    n = samples(3.0)
    hull = (instruments.partial(n, freq, 1.0, 0.12, 2.2)
            + instruments.partial(n, freq, 0.6, 0.12, 2.0, detune_cents=6.0)
            + instruments.partial(n, 2.0 * freq, 0.18, 0.1, 1.0)
            + instruments.partial(n, 3.0 * freq, 0.05, 0.1, 0.5))
    knock = filters.lowpass(noise.white(n, gen), 600.0) * envelope.ar(n, 0.002, 0.04)
    mix = 0.6 * hull + 0.05 * knock / max(float(np.max(np.abs(knock))), 1e-9)
    if holds_relic:
        mix += 0.22 * instruments.soft_bell(2.0 * freq, 3.0, decay=1.6, attack=0.15)
        mix += 0.12 * instruments.soft_pad(note_freq("D5"), 3.0, 0.6, 1.4, hold=0.4)
    return _mono_reverb(filters.lowpass(mix, 4500.0), room=0.7, damping=0.6, wet=0.3, dry=1.0)


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
    Cue("salvage_chime", "oneshot_3d", salvage_chime, variants=SALVAGE_CHIME_NOTES, volume=(0.7, 0.7),
        fade_out=0.1, variant_labels=("D5", "E5", "Fs5", "A5", "B5", "D6", "E6", "Fs6"), milestone="M3",
        tonal=True, notes="Salvage folding in: felt fold + glassy chime per pentatonic degree D5..F#6; the salvage "
                          "melody picks the clip from the site's combo step (climbs, then weaves over the top)."),
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
    Cue("tilly_broken", "oneshot_3d", tilly_broken, variants=len(TILLY_BROKEN), volume=(0.45, 0.5), fade_out=0.1,
        milestone="M3", tonal=True,
        notes="Tilly dormant answering a ping: faint, glitchy, hopeful chirp (stutters, sags, retries)."),
    Cue("tilly_curious", "oneshot_3d", tilly_curious, variants=len(TILLY_CURIOUS), volume=(0.5, 0.55),
        fade_out=0.08, milestone="M3", tonal=True, notes="Tilly curious 'hm?': short blip + rising question slide."),
    Cue("tilly_happy", "oneshot_3d", tilly_happy, variants=len(TILLY_HAPPY), volume=(0.55, 0.6), fade_out=0.08,
        milestone="M3", tonal=True, notes="Tilly happy: bouncing pentatonic trill."),
    Cue("tilly_sleepy", "oneshot_3d", tilly_sleepy, variants=len(TILLY_SLEEPY), volume=(0.35, 0.4),
        fade_out=0.12, milestone="M3", tonal=True, notes="Tilly napping on her perch: slow falling coo + echo."),
    Cue("tilly_greeting", "oneshot_3d", tilly_greeting, variants=len(TILLY_GREETING), volume=(0.6, 0.65),
        fade_out=0.08, milestone="M3", tonal=True,
        notes="Tilly flying out to meet 07: rising swoop + trill landing high."),
    Cue("tilly_excited", "oneshot_3d", tilly_excited, variants=len(TILLY_EXCITED), volume=(0.6, 0.65),
        fade_out=0.08, milestone="M3", tonal=True, notes="Tilly when a relic comes home: fluttering arpeggio."),
    Cue("tilly_found", "oneshot_3d", tilly_found, variants=len(TILLY_FOUND), volume=(0.55, 0.6), fade_out=0.08,
        milestone="M3", tonal=True, notes="Tilly spotting something: 'found it!' hop up with a bright landing."),
    Cue("friend_spot_ping", "oneshot_3d", friend_spot_ping, volume=(0.5, 0.5), fade_out=0.2, milestone="M3",
        tonal=True, notes="Soft A5 bell + A6 shimmer marking what a friend spotted (relic, part, scrap)."),
    Cue("tilly_rotor", "loop_3d", tilly_rotor, loop=True, file_stem="tilly_rotor_loop", volume=(0.35, 0.35),
        hf_cutoff=6000.0, hf_max_db=-40.0, milestone="M3", tonal=True,
        notes="Tilly's tiny four-rotor hum around A3, 4 s seamless; < 3.5 kHz so speed can pitch it up."),
    Cue("friend_stitch", "loop_3d", friend_stitch, loop=True, file_stem="friend_stitch_loop", volume=(0.6, 0.6),
        milestone="M3", tonal=True,
        notes="Repair stitching: D/A shimmer + D3 sewing pulse + pentatonic needle ticks, 4 s seamless."),
    Cue("friend_boot", "oneshot_3d", friend_boot, volume=(0.7, 0.7), fade_out=0.2, milestone="M3", tonal=True,
        notes="Repaired friend boots: eye flickers, power-up glide, music-box jingle D5 F#5 A5 D6."),
    Cue("friend_part", "oneshot_3d", friend_part, variants=len(FRIEND_PART_STEPS) + 1, volume=(0.65, 0.65),
        variant_labels=("step1", "step2", "step3", "step4", "complete"), fade_out=0.2, milestone="M3",
        tonal=True, notes="Friend part collected: amber vibraphone bar climbing D5 F#5 A5 B5; 'complete' on the last "
                          "part resolves A5 -> D6."),
    Cue("jump_charge", "loop_3d", jump_charge, loop=True, file_stem="jump_charge_loop", volume=(0.45, 0.45),
        hf_cutoff=6000.0, hf_max_db=-40.0, milestone="M3", tonal=True,
        notes="Hover-Jump charge hum on D4 + spring creak; runtime steps it D E F# A B with the charge."),
    Cue("jump_leap", "oneshot_3d", jump_leap, variants=len(JUMP_LEAP_SIZES), variant_labels=JUMP_LEAP_SIZES,
        volume=(0.6, 0.6), fade_out=0.1, milestone="M3",
        notes="Leap: soft rising boing + whoosh; 'hop' for taps, 'leap' for strong charges (volume by strength)."),
    Cue("coil_twang", "oneshot_3d", coil_twang, variants=2, variant_labels=("A3", "D4"), volume=(0.4, 0.45),
        fade_out=0.1, milestone="M3", tonal=True, notes="The coils springing out on a leap: soft plucked spring."),
    Cue("air_wind", "loop_3d", air_wind, loop=True, file_stem="air_wind_loop", volume=(0.4, 0.4), milestone="M3",
        notes="Soft rushing air while airborne, 6 s seamless; swells with air time and speed."),
    Cue("jump_land", "oneshot_3d", jump_land, variants=len(JUMP_LAND_SETTINGS), volume=(0.75, 0.85),
        pitch=(0.97, 1.03), fade_out=0.1, milestone="M3",
        notes="Cushioned landing after a leap: air puff + muted thud + spring + dust (gentler than the thump)."),
    Cue("coil_pop", "oneshot_3d", coil_pop, volume=(0.6, 0.6), fade_out=0.1, milestone="M3",
        notes="The Hover-Jump coils popping in after the purchase: clunk + sproing."),
    Cue("workbench_upgrade", "stinger_2d", workbench_upgrade, volume=(0.75, 0.75), fade_out=0.3, milestone="M3",
        tonal=True, notes="Workbench purchase: spark crackles at 0/0.16/0.32 s (matching the bench's bursts) + "
                          "toolbox rattle + resolved A -> D major cadence."),
    Cue("bell_broken", "oneshot_3d", bell_broken, variants=2, volume=(0.45, 0.5), fade_out=0.15, milestone="M3",
        notes="Bell dormant answering a ping: a dying set catching a station for a moment."),
    Cue("bell_excited", "oneshot_3d", bell_excited, variants=2, volume=(0.55, 0.6), fade_out=0.1, milestone="M3",
        tonal=True, notes="Bell's happy crackle on a new relic: static burst + two warbling blips up the scale."),
    Cue("bell_tune", "oneshot_3d", bell_tune, variants=2, volume=(0.45, 0.5), fade_out=0.06, milestone="M3",
        notes="Bell's station-switch crackle: detent tick + a static swish as the needle moves."),
    Cue("bell_step", "tick_3d", bell_step, variants=len(BELL_STEP_PITCH_HZ), volume=(0.45, 0.55),
        pitch=(0.96, 1.04), fade_out=0.02, milestone="M3", notes="Bell's light tripod-foot taps while she walks."),
    Cue("bell_doze", "loop_3d", bell_doze, loop=True, file_stem="bell_doze_loop", volume=(0.3, 0.3), milestone="M3",
        tonal=True, notes="Bell dozing: dial ember hum on D2/D3 + faint A3 glow + rare valve tick, 6 s seamless."),
    Cue("bell_wake", "oneshot_3d", bell_wake, volume=(0.5, 0.5), fade_out=0.12, milestone="M3", tonal=True,
        notes="Bell waking: the hum warms up, a crackle, a sleepy E5 -> A5 hello."),
    Cue("bell_curious", "oneshot_3d", bell_curious, variants=len(BELL_CURIOUS), volume=(0.45, 0.5), fade_out=0.08,
        milestone="M3", tonal=True,
        notes="Bell 'hm?': a tuning squeak settling onto a note, then the whistle sliding up like a question."),
    Cue("bell_happy", "oneshot_3d", bell_happy, variants=len(BELL_HAPPY), volume=(0.5, 0.55), fade_out=0.08,
        milestone="M3", tonal=True, notes="Bell happy at home: a gliding dial-whistle run up the pentatonic."),
    Cue("bell_sleepy", "oneshot_3d", bell_sleepy, variants=len(BELL_SLEEPY), volume=(0.35, 0.4), fade_out=0.15,
        milestone="M3", tonal=True, notes="Bell sleepy: a slow whistle sagging down like a tape winding down."),
    Cue("bell_greeting", "oneshot_3d", bell_greeting, variants=len(BELL_GREETING), volume=(0.55, 0.6),
        fade_out=0.1, milestone="M3", tonal=True,
        notes="Bell 'hello!': upward tuning squeak + a station-ident whistle (her jingle replaces it at home)."),
    Cue("bell_found", "oneshot_3d", bell_found, variants=len(BELL_FOUND), volume=(0.5, 0.55), fade_out=0.08,
        milestone="M3", tonal=True, notes="Bell 'got it!': a squeak locking onto a clean tone, then up a fourth."),
    Cue("bell_rotor", "loop_3d", bell_rotor, loop=True, file_stem="bell_rotor_loop", volume=(0.3, 0.3),
        hf_cutoff=6000.0, hf_max_db=-40.0, milestone="M3",
        notes="Bell's walking effort: soft clockwork tick-tock of her legs + spring whirr + creak, 4 s seamless."),
    Cue("bell_tape_slot", "oneshot_3d", bell_tape_slot, volume=(0.6, 0.6), fade_out=0.08, milestone="M3",
        notes="BellCued.TapeSlotted: the tape slides into her slot, clacks home and the capstan motor catches."),
    Cue("bell_needle_sweep", "oneshot_3d", bell_needle_sweep, volume=(0.6, 0.6), fade_out=0.15, milestone="M3",
        notes="BellCued.NeedleSwept: lamp hum warms, the needle sweeps up the band past three faint stations."),
    Cue("radio_dial_click", "tick_2d", radio_dial_click, volume=(0.7, 0.7), fade_out=0.02, milestone="M3",
        notes="One detent of Bell's radio dial (2D, at the dial)."),
    Cue("cassette_pickup", "oneshot_3d", cassette_pickup, volume=(0.7, 0.7), fade_out=0.1, milestone="M3",
        notes="Collecting a cassette: plastic clack + reels spinning up + a soft D6 counter tick."),
    Cue("crew_log_found", "oneshot_3d", crew_log_found, volume=(0.6, 0.6), fade_out=0.1, milestone="M3",
        notes="Opening a crew log cache: tin tink + hinge creak + paper rustle."),
    Cue("bell_signal_pick", "oneshot_3d", bell_signal_pick, volume=(0.35, 0.35), fade_out=0.4, milestone="M3",
        tonal=True, notes="Bell's signal pillar appearing: warm, very soft A4/D5/F#5 shimmer (played distant)."),
    Cue("bell_signal_found", "oneshot_3d", bell_signal_found, volume=(0.55, 0.55), fade_out=0.25, milestone="M3",
        tonal=True, notes="Bell's signal found: resolved A5 -> D6 chime."),
    Cue("canyon_whisper", "ambience_2d", canyon_whisper, loop=True, file_stem="canyon_whisper_loop",
        volume=(0.8, 0.8), milestone="M3",
        notes="Whispering Canyon bed: breathy air with a slowly moving 'vowel', 16 s seamless stereo."),
    Cue("canyon_trough", "ambience_2d", canyon_trough, loop=True, file_stem="canyon_trough_loop", volume=(0.7, 0.7),
        milestone="M3", notes="The chasm trough's deeper, darker bed: low rumble + hollow 180 Hz + D2, 16 s seamless."),
    Cue("room_tone", "ambience_2d", room_tone, loop=True, file_stem="room_tone_loop", volume=(0.8, 0.8),
        milestone="M3", notes="Solitude room tone: wide, very soft dark air + faint D2/A2, 24 s seamless stereo."),
    Cue("rover_lamp_hum", "loop_3d", rover_lamp_hum, loop=True, file_stem="rover_lamp_hum_loop", volume=(0.5, 0.5),
        milestone="M3", tonal=True, notes="07's lamp: tiny warm hum on D3 + filament buzz, 4 s seamless."),
    Cue("rover_servo", "loop_3d", rover_servo, loop=True, file_stem="rover_servo_loop", volume=(0.5, 0.5),
        milestone="M3", notes="07's servos (steering, neck): soft gear whirr + faint whine, 2 s seamless."),
    Cue("rover_metal_tick", "tick_3d", rover_metal_tick, variants=len(METAL_TICK_NOTES), volume=(0.4, 0.55),
        pitch=(0.98, 1.02), fade_out=0.03, milestone="M3", tonal=True,
        notes="07's metal ticking as it cools after a drive: dry click + a tiny pentatonic ring."),
    Cue("relay_mast_creak", "oneshot_3d", relay_mast_creak, volume=(0.6, 0.6), fade_out=0.1, milestone="M3",
        notes="A relay mast straightening: low metal groan rising, a scrape and a soft locking clunk."),
    Cue("relay_lamp_warm", "oneshot_3d", relay_lamp_warm, volume=(0.55, 0.55), fade_out=0.2, milestone="M3",
        tonal=True, notes="A mast's lamp warming: filament catch, hum rising onto D3, A4/D5 glow."),
    Cue("relay_link", "oneshot_3d", relay_link, volume=(0.6, 0.6), fade_out=0.2, milestone="M3", tonal=True,
        notes="The link answer from home's direction: old-radio A4 D5 F#5 with a breath of static."),
    Cue("radio_hop_out", "radio_fx_2d", radio_hop_out, volume=(0.8, 0.8), fade_out=0.05, milestone="M3",
        notes="Radio-hop leaving: static swells, band sweeps down and filters out as the screen fades."),
    Cue("radio_hop_in", "radio_fx_2d", radio_hop_in, volume=(0.8, 0.8), fade_out=0.1, milestone="M3",
        notes="Radio-hop landing: band sweeps up and locks onto a soft D5 carrier as the view eases in."),
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
    Cue("site_answer", "oneshot_3d", site_answer, variants=len(SITE_ANSWER_LABELS), variant_labels=SITE_ANSWER_LABELS,
        volume=(0.8, 0.8), fade_out=0.25, milestone="M3", tonal=True,
        notes="A site answering a ping: hull resonance on its note (depot D4, garage E4, drill F#4, kestrel A4, "
              "lander B4); '_relic' answers warmer with its octave and a D5 glow."),
    Cue("upgrade_arpeggio", "stinger_2d", upgrade_arpeggio, volume=(0.8, 0.8), fade_out=0.3, milestone="M2",
        tonal=True, notes="Soft kalimba D4 A4 D5 F#5 A5, stereo, over a quiet D/A pad."),
)
