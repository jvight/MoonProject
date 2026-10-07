"""
Round bass lines that lock to the kick: each chord starts on its bass note, later kicks inside the chord get the
root, fifth or octave, a diatonic approach note sometimes leads into the next chord, and short steps between
neighbouring notes occasionally slide. Roots stay between G1 and B2, picked nearest to the previous note.
The "walk" mode is the jam's walking bass instead: a note on every beat, chord tones on beats 1 and 3, scale steps
between them, the last beat stepping into the next chord's bass, and now and then a swung sixteenth pickup.
"""
from .groove import swung_beat
from .score import NoteEvent

ROOT_LOW = 31
ROOT_HIGH = 47
TOP = 50
_GAP_BEATS = 0.12
WALK_PICKUP_PROBABILITY = 0.15
WALK_PICKUP_STEP = 3
_WALK_STEP_WEIGHT = {1: 1.0, 2: 1.0, 3: 0.7, 4: 0.5, 5: 0.3}


def nearest(pc, previous, low=ROOT_LOW, high=ROOT_HIGH):
    options = [p for p in range(low, high + 1) if p % 12 == pc]
    return min(options, key=lambda p: (abs(p - previous), p))


def _approach_pc(target_pc, current_pitch, key):
    scale = key.scale
    index = scale.index(target_pc)
    below, above = scale[(index - 1) % 7], scale[(index + 1) % 7]
    return above if current_pitch % 12 > target_pc else below


def _walk_pitch(previous, target, pcs, rng):
    """A walking step from `previous` towards `target` on one of `pcs`, never repeating the note."""
    options = [p for p in range(ROOT_LOW - 3, TOP + 1) if p % 12 in pcs and 0 < abs(p - previous) <= 5]
    weights = []
    for pitch in options:
        toward = 1.4 if abs(target - pitch) < abs(target - previous) else 1.0
        weights.append(_WALK_STEP_WEIGHT[abs(pitch - previous)] * toward)
    total = sum(weights)
    return options[int(rng.choice(len(options), p=[w / total for w in weights]))]


def _walk_bar(bar_start, slots, swing, key, next_bass_pc, previous, rng):
    notes = []
    slot_start = 0.0
    for slot_index, slot in enumerate(slots):
        chord = slot.chord
        following = slots[slot_index + 1].chord.bass if slot_index + 1 < len(slots) else next_bass_pc
        root = nearest(chord.bass, previous)
        target = nearest(following, root) if following is not None else root
        beats = int(round(slot.beats))
        pitches = [root]
        for beat in range(1, beats):
            if beat == beats - 1 and following is not None and following != chord.bass:
                approach = _approach_pc(following, pitches[-1], key)
                pitches.append(nearest(approach, target, ROOT_LOW - 3, TOP))
            else:
                strong = beat % 2 == 0
                pcs = chord.tones if strong or chord.borrowed else key.pcs
                pitches.append(_walk_pitch(pitches[-1], target, pcs, rng))
        for beat, pitch in enumerate(pitches):
            start = slot_start + beat
            velocity = (0.9 if beat == 0 else 0.74) * (1.0 + 0.05 * float(rng.standard_normal()))
            offset = 0.004 * float(rng.standard_normal())
            pickup = beat < len(pitches) - 1 and rng.random() < WALK_PICKUP_PROBABILITY
            length = (swung_beat(WALK_PICKUP_STEP, swing) if pickup else 1.0) - _GAP_BEATS
            notes.append(NoteEvent(bar_start + start, length, pitch, min(1.0, velocity), offset))
            if pickup:
                pickup_start = start + swung_beat(WALK_PICKUP_STEP, swing)
                notes.append(NoteEvent(bar_start + pickup_start, start + 1.0 - pickup_start - _GAP_BEATS / 2.0,
                                       pitch, min(1.0, 0.5 * velocity), offset))
        previous = root
        slot_start += slot.beats
    return notes, previous


def bar_notes(bar_start, slots, kick_steps, mode, swing, key, next_bass_pc, previous, rng):
    """Bass notes for one bar. `slots` are the bar's ChordSlots; returns (notes, last pitch)."""
    if mode == "walk":
        return _walk_bar(bar_start, slots, swing, key, next_bass_pc, previous, rng)
    notes = []
    slot_start = 0.0
    for slot_index, slot in enumerate(slots):
        slot_end = slot_start + slot.beats
        chord = slot.chord
        pitch = nearest(chord.bass, previous)
        starts = [slot_start]
        if mode == "groove":
            for step in kick_steps:
                beat = swung_beat(step, swing)
                if slot_start + 0.4 < beat < slot_end - 0.2:
                    starts.append(beat)
        is_last_slot = slot_index == len(slots) - 1
        approach = (mode == "groove" and is_last_slot and next_bass_pc is not None and next_bass_pc != chord.bass
                    and starts[-1] <= slot_end - 1.0 and rng.random() < 0.35)
        if approach:
            starts.append(slot_end - 0.5)
        pitches = []
        for index in range(len(starts)):
            if index == 0:
                pitches.append(pitch)
            elif approach and index == len(starts) - 1:
                target = nearest(next_bass_pc, pitches[-1])
                pitches.append(nearest(_approach_pc(next_bass_pc, pitches[-1], key), target, ROOT_LOW - 3, TOP))
            else:
                roll = rng.random()
                if roll < 0.2:
                    pitches.append(nearest(chord.fifth, pitch, ROOT_LOW, TOP))
                elif roll < 0.3 and pitch + 12 <= TOP:
                    pitches.append(pitch + 12)
                else:
                    pitches.append(pitch)
        for index, start in enumerate(starts):
            end = starts[index + 1] - _GAP_BEATS if index + 1 < len(starts) else slot_end - _GAP_BEATS
            velocity = (0.92 if index == 0 else 0.78) * (1.0 + 0.05 * float(rng.standard_normal()))
            offset = 0.002 * float(rng.standard_normal())
            notes.append(NoteEvent(bar_start + start, max(0.2, end - start), pitches[index], min(1.0, velocity),
                                   offset))
        previous = pitches[0]
        slot_start = slot_end
    return notes, previous


def add_slides(notes, rng, probability=0.22):
    """Mark close, small steps as slides (the synth glides into them)."""
    result = []
    for index, note in enumerate(notes):
        if index > 0:
            before = notes[index - 1]
            close = note.beat - before.end < 0.3 and 0 < abs(note.pitch - before.pitch) <= 5
            if close and rng.random() < probability:
                note = NoteEvent(note.beat, note.length, note.pitch, note.velocity, note.offset_s, True)
        result.append(note)
    return result


def kick_steps_for(pattern):
    """Kick steps of a bar pattern; a bar without drums keeps the bass on a 1 / 3-and feel."""
    steps = sorted({step for step, voice, _ in pattern if voice == "kick"})
    return steps or [0, 10]
