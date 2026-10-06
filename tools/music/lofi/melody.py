"""
Sparse, melancholic lead lines built from call-and-response phrases.

Vocabulary: D major pentatonic only (D E F# A B), so every lead note also fits the game's sonar and chimes.
Rules, checked again by tests and the theory report:
  * strong notes (beats 1 and 3, anything a quarter or longer, every phrase ending) land on a chord tone or an
    in-key tension of the chord sounding under them;
  * notes an eighth or longer never sit a semitone from a pitch class the keys or the pad voice, in any chord
    they sound over (no m2/m9 rubs);
  * over a borrowed chord every note follows the strong rule (the borrowed tone makes the scale's neighbours rub).
A two-bar call is answered by the same rhythm moved along the pentatonic ladder to fit the next chords; whole
two-bar slots are left empty on purpose - silence is part of the tune.
"""
import math
from dataclasses import dataclass

from .comping import PUSH_BEATS
from .groove import STEPS_PER_BAR, swung_beat
from .score import NoteEvent

FRAME_STEPS = 2 * STEPS_PER_BAR
# Two-bar rhythm cells: (start, length) in 16th steps of the frame, from sparse sighs to busier lines.
RHYTHMS = (
    ((0, 6), (6, 2), (8, 8)),
    ((2, 2), (4, 2), (6, 2), (8, 4), (12, 4), (16, 10)),
    ((0, 3), (3, 3), (6, 2), (8, 8), (24, 6)),
    ((10, 2), (12, 2), (14, 4), (18, 2), (20, 8)),
    ((0, 2), (2, 2), (4, 8), (16, 2), (18, 2), (20, 6)),
    ((0, 12), (12, 4), (16, 12)),
    ((0, 3), (3, 3), (6, 2), (8, 3), (11, 3), (14, 2), (16, 12)),
    ((2, 4), (6, 4), (10, 6), (18, 2), (20, 10)),
    ((4, 8), (12, 16)),
    ((0, 2), (2, 2), (4, 2), (6, 2), (8, 8), (18, 4), (22, 8)),
    ((0, 4), (4, 2), (6, 2), (8, 4), (12, 4), (16, 8)),
    ((2, 2), (4, 4), (8, 2), (10, 6), (18, 2), (20, 2), (22, 6)),
    ((0, 2), (3, 3), (6, 4), (10, 2), (12, 4), (16, 4), (20, 8)),
    ((6, 2), (8, 6), (14, 2), (16, 8), (26, 2), (28, 4)),
)
_STEP_WEIGHT = {0: 0.45, 1: 1.0, 2: 0.55, 3: 0.2, 4: 0.08}


@dataclass(frozen=True)
class LeadVoice:
    """Tessitura of a lead instrument (MIDI, inclusive)."""
    name: str
    low: int
    high: int


LEADS = {v.name: v for v in (
    LeadVoice("kalimba", 66, 86),
    LeadVoice("musicbox", 74, 90),
    LeadVoice("flute", 66, 83),
)}


@dataclass(frozen=True)
class Motif:
    """A call remembered for later sections: its rhythm and its pentatonic-ladder degrees."""
    rhythm: tuple
    degrees: tuple


def is_strong(step, length, last):
    return step % 8 == 0 or length >= 4 or last


def clash_pcs(chord_event):
    """Pitch classes a semitone away from anything the keys or pad voice, or from the chord's guide tones."""
    sounding = {p % 12 for p in chord_event.keys_voicing + chord_event.pad_voicing}
    sounding |= chord_event.chord.guide_tones
    return {(pc + d) % 12 for pc in sounding for d in (-1, 1)}


def allowed_pcs(onset_event, sounding_events, key, strong, length_steps):
    """
    Pitch classes a lead may use for a note that starts over `onset_event` and sounds over `sounding_events`
    (every chord it overlaps, including one pushed in early by the keys) - see the module rules.
    """
    pcs = set(key.pentatonic)
    if strong or onset_event.chord.borrowed:
        pcs &= onset_event.chord.melodic_pcs(key)
    for event in sounding_events:
        if event.chord.borrowed:
            pcs &= event.chord.melodic_pcs(key)
        if length_steps >= 2 or strong:
            pcs -= clash_pcs(event)
    return pcs


class _Frame:
    """A two-bar frame of a section: converts 16th steps to beats and finds the harmony under each note."""

    def __init__(self, start_beat, swing, timeline):
        self.start = start_beat
        self.swing = swing
        self.timeline = timeline

    def beat(self, step):
        bar, inner = divmod(step, STEPS_PER_BAR)
        return self.start + 4.0 * bar + swung_beat(inner, self.swing)

    def chord(self, step):
        return self.timeline.at(self.beat(step))

    def sounding(self, step, length):
        """Chord events heard under a note: overlapping it, or pushed in up to half a beat before their time."""
        start = self.beat(step)
        end = self.beat(step + length) if step + length < FRAME_STEPS else self.start + 8.0
        return [c for c in self.timeline.chords if c.beat < end + PUSH_BEATS and c.end > start]


def _ladder(key, voice):
    return [p for p in range(voice.low, voice.high + 1) if p % 12 in key.pentatonic]


def _pick(options, previous, target, ladder, rng, history):
    weights = []
    for pitch in options:
        weight = math.exp(-((pitch - target) / 5.0) ** 2)
        if previous is not None:
            weight *= _STEP_WEIGHT.get(abs(ladder.index(pitch) - ladder.index(previous)), 0.0)
        if len(history) >= 2 and history[-1] == history[-2] == pitch:
            weight = 0.0
        weights.append(weight)
    total = sum(weights)
    if total <= 0.0:
        reference = target if previous is None else previous
        return min(options, key=lambda p: (abs(p - reference), p))
    return options[int(rng.choice(len(options), p=[w / total for w in weights]))]


def _options(frame, step, length, last, key, ladder):
    event = frame.chord(step)
    strong = is_strong(step, length, last)
    pcs = allowed_pcs(event, frame.sounding(step, length), key, strong, length)
    if last:
        pcs = (pcs & event.chord.tones) or pcs
    options = [p for p in ladder if p % 12 in pcs]
    if not options:
        raise ValueError(f"no pentatonic note fits {event.chord.symbol} voiced {event.keys_voicing}")
    return options


def compose_call(rhythm, frame, key, voice, rng, anchor):
    """Invent a two-bar call over the frame's harmony; returns [(step, length, pitch)]."""
    ladder = _ladder(key, voice)
    arch = float(rng.uniform(3.0, 7.0))
    notes = []
    previous = None
    history = []
    for index, (step, length) in enumerate(rhythm):
        last = index == len(rhythm) - 1
        progress = index / max(1, len(rhythm) - 1)
        target = anchor + arch * math.sin(math.pi * progress) - (2.5 if last else 0.0)
        options = _options(frame, step, length, last, key, ladder)
        if last and previous is not None:
            lower = [p for p in options if p <= previous]
            options = lower or options
        pitch = _pick(options, previous, target, ladder, rng, history)
        notes.append((step, length, pitch))
        history.append(pitch)
        previous = pitch
    return notes


def _mutate_rhythm(rhythm, rng):
    rhythm = list(rhythm)
    if len(rhythm) > 2 and rng.random() < 0.3:
        drop = int(rng.integers(1, len(rhythm) - 1))
        step, length = rhythm[drop - 1]
        rhythm[drop - 1] = (step, length + rhythm[drop][1])
        del rhythm[drop]
    if rng.random() < 0.4:
        step, length = rhythm[-1]
        rhythm[-1] = (step, max(2, min(FRAME_STEPS - step, length + int(rng.choice([-2, 2, 4])))))
    return tuple(rhythm)


def answer(motif, frame, key, voice, rng, mutate=True):
    """Re-sing a motif over new harmony: same rhythm (lightly mutated), moved along the ladder, then fixed."""
    ladder = _ladder(key, voice)
    rhythm = _mutate_rhythm(motif.rhythm, rng) if mutate else motif.rhythm
    degrees = list(motif.degrees[:len(rhythm)])
    while len(degrees) < len(rhythm):
        degrees.append(degrees[-1])
    misses = {}
    for shift in (-2, -1, 0, 1, 2):
        misses[shift] = 0
        for index, (step, length) in enumerate(rhythm):
            degree = degrees[index] + shift
            options = _options(frame, step, length, index == len(rhythm) - 1, key, ladder)
            if not 0 <= degree < len(ladder) or ladder[degree] not in options:
                misses[shift] += 1
    fewest = min(misses.values())
    shifts = [s for s in misses if misses[s] <= fewest + (1 if mutate else 0)]
    shift = shifts[int(rng.integers(len(shifts)))]
    ornament = int(rng.integers(len(rhythm))) if mutate and rng.random() < 0.4 else None
    notes = []
    for index, (step, length) in enumerate(rhythm):
        last = index == len(rhythm) - 1
        degree = min(len(ladder) - 1, max(0, degrees[index] + shift))
        if index == ornament and not last:
            degree = min(len(ladder) - 1, max(0, degree + int(rng.choice([-1, 1]))))
        options = _options(frame, step, length, last, key, ladder)
        pitch = ladder[degree]
        if pitch not in options:
            pitch = min(options, key=lambda p: (abs(p - pitch), -p))
        notes.append((step, length, pitch))
    return notes


def remember(notes, key, voice):
    ladder = _ladder(key, voice)
    return Motif(tuple((s, l) for s, l, _ in notes), tuple(ladder.index(p) for _, _, p in notes))


def _plan(bars, density):
    frames = bars // 2
    if frames <= 2:
        return ["call", "rest"][:frames]
    if density >= 0.66:
        pattern = ["call", "answer", "again", "answer"]
    elif density >= 0.33:
        pattern = ["call", "answer", "rest", "answer"]
    else:
        pattern = ["call", "rest", "answer", "rest"]
    return [pattern[i % len(pattern)] for i in range(frames)]


def _events(notes, frame, rng, velocity_scale):
    events = []
    count = len(notes)
    for index, (step, length, pitch) in enumerate(notes):
        start = frame.beat(step)
        end = frame.beat(step + length) if step + length < FRAME_STEPS else frame.start + 8.0
        shape = 0.62 + 0.12 * math.sin(math.pi * index / max(1, count - 1))
        accent = 0.06 if is_strong(step, length, index == count - 1) else 0.0
        velocity = min(1.0, max(0.2, (shape + accent + 0.04 * float(rng.standard_normal())) * velocity_scale))
        offset = 0.004 + 0.006 * float(rng.standard_normal())
        events.append(NoteEvent(start, end - start, pitch, velocity, offset))
    return events


def section_melody(start_beat, bars, swing, timeline, key, voice, density, rng, motif=None):
    """
    Melody for one section. With `motif`, the section opens by re-singing it (a reprise); otherwise it invents a
    new call. Returns (NoteEvents, the motif this section is built on).
    """
    events = []
    anchor = voice.low + 0.42 * (voice.high - voice.low)
    eligible = [r for r in RHYTHMS if len(r) <= 3 + 6 * density]
    for index, role in enumerate(_plan(bars, density)):
        frame = _Frame(start_beat + 8.0 * index, swing, timeline)
        if role == "rest":
            continue
        if role == "call" and motif is None:
            rhythm = eligible[int(rng.integers(len(eligible)))]
            notes = compose_call(rhythm, frame, key, voice, rng, anchor)
            motif = remember(notes, key, voice)
        else:
            notes = answer(motif, frame, key, voice, rng, mutate=role != "call")
        events.extend(_events(notes, frame, rng, 1.0 if role != "answer" else 0.94))
    return events, motif
