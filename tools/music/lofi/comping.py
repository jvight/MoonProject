"""
Keys comping and pad sustain.

Keys play voiced chords in lazy rhythmic cells (held, Charleston, stabs, lazy two-hit), rolled bottom-to-top by a
few milliseconds like a relaxed hand, with the top voice slightly louder. Sometimes the next chord is pushed in an
eighth early (the lofi "anticipation"). Stabs mode plays short, bouncing off-beat cells instead of the lazy ones.
Arpeggio mode breaks chords into soft eighth notes for intros and outros; rock mode is the lullaby's arpeggio,
leaning on every beat and easing off between, like rocking a cradle.
Pads hold each chord and tie voices that stay on the same pitch, so only moving voices re-attack.
"""
from .groove import STEPS_PER_BAR, swung_beat
from .score import NoteEvent

_FULL_BAR_CELLS = (
    ((0, 15),),
    ((0, 5), (6, 9)),
    ((0, 7), (10, 5)),
    ((0, 3), (6, 3), (10, 5)),
    ((0, 11), (12, 3)),
)
_HALF_BAR_CELLS = (
    ((0, 7),),
    ((0, 3), (4, 3)),
    ((0, 5), (6, 2)),
)
_STAB_FULL_BAR_CELLS = (
    ((0, 2), (6, 2), (10, 2), (14, 2)),
    ((0, 3), (6, 2), (8, 2), (14, 2)),
    ((2, 2), (6, 2), (10, 2), (14, 2)),
    ((0, 2), (3, 2), (8, 3), (12, 2)),
)
_STAB_HALF_BAR_CELLS = (
    ((0, 2), (6, 2)),
    ((2, 2), (6, 2)),
    ((0, 3), (4, 2)),
)
_ARP_ORDER = (0, 1, 2, 3, 2, 1, 3, 1)
ROCK_ACCENTS = (1.2, 0.7)
PUSH_BEATS = 0.5


def _beat_of(step, swing):
    bar, inner = divmod(step, STEPS_PER_BAR)
    return 4.0 * bar + swung_beat(inner, swing)


def _rolled(beat, length, voicing, velocity, rng):
    roll_s = float(rng.uniform(0.004, 0.026))
    base_offset = 0.003 + 0.004 * float(rng.standard_normal())
    notes = []
    for index, pitch in enumerate(voicing):
        top = index == len(voicing) - 1
        level = velocity * (1.08 if top else 0.94) * (1.0 + 0.04 * float(rng.standard_normal()))
        offset = base_offset + roll_s * index / max(1, len(voicing) - 1)
        notes.append(NoteEvent(beat, length, pitch, min(1.0, max(0.1, level)), offset))
    return notes


def comp_hits(chords, bar_beats, swing, rng, push_probability=0.25, stabs=False):
    """
    Rhythm of the keys over a list of ChordEvents. Returns [(beat, length_beats, chord_event)] where a pushed hit
    sounds an eighth before its chord and replaces that chord's downbeat. `stabs` picks the bouncing cells.
    """
    full_cells, half_cells = (_STAB_FULL_BAR_CELLS, _STAB_HALF_BAR_CELLS) if stabs else (_FULL_BAR_CELLS,
                                                                                         _HALF_BAR_CELLS)
    hits = []
    for event in chords:
        cells = full_cells if event.length >= bar_beats else half_cells
        cell = cells[int(rng.integers(len(cells)))]
        for step, length in cell:
            start = event.beat + _beat_of(step, swing)
            end = event.beat + _beat_of(step + length, swing) if step + length < STEPS_PER_BAR else event.end
            hits.append([start, end - start, event])
    for index in range(1, len(hits)):
        start, _, event = hits[index]
        previous = hits[index - 1]
        changes = previous[2] is not event and abs(start - event.beat) < 1e-9
        if changes and previous[0] < start - 1.0 and rng.random() < push_probability:
            push = start - PUSH_BEATS
            previous[1] = min(previous[1], push - previous[0] - 0.05)
            hits[index][1] += start - push
            hits[index][0] = push
    return [tuple(h) for h in hits]


def keys_part(chords, mode, bar_beats, swing, rng, velocity=0.62):
    """Keys notes for a run of chords in `mode` ("comp", "stabs", "arp" or "rock")."""
    notes = []
    if mode in ("comp", "stabs"):
        for beat, length, event in comp_hits(chords, bar_beats, swing, rng, stabs=mode == "stabs"):
            notes.extend(_rolled(beat, length, event.keys_voicing, velocity * float(rng.uniform(0.9, 1.05)), rng))
    elif mode in ("arp", "rock"):
        for event in chords:
            steps = round(event.length * 4)
            for index, step in enumerate(range(0, steps, 2)):
                voice = event.keys_voicing[_ARP_ORDER[index % len(_ARP_ORDER)] % len(event.keys_voicing)]
                beat = event.beat + _beat_of(step, swing)
                level = velocity * 0.8 * (1.0 + 0.06 * float(rng.standard_normal()))
                if mode == "rock":
                    level *= ROCK_ACCENTS[index % len(ROCK_ACCENTS)]
                notes.append(NoteEvent(beat, min(1.5, event.end - beat), voice, min(1.0, level),
                                       0.004 * float(rng.standard_normal())))
    return notes


def pad_part(chords, velocity=0.5):
    """Sustained pad notes; a pitch held by consecutive chords is tied instead of re-attacked."""
    notes = []
    open_notes = {}
    for event in chords:
        current = set(event.pad_voicing)
        for pitch in list(open_notes):
            if pitch not in current:
                start = open_notes.pop(pitch)
                notes.append(NoteEvent(start, event.beat - start, pitch, velocity))
        for pitch in event.pad_voicing:
            open_notes.setdefault(pitch, event.beat)
    if chords:
        end = chords[-1].end
        for pitch, start in sorted(open_notes.items()):
            notes.append(NoteEvent(start, end - start, pitch, velocity))
    return sorted(notes, key=lambda n: (n.beat, n.pitch))
