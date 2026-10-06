"""
Round bass lines that lock to the kick: each chord starts on its bass note, later kicks inside the chord get the
root, fifth or octave, a diatonic approach note sometimes leads into the next chord, and short steps between
neighbouring notes occasionally slide. Roots stay between G1 and B2, picked nearest to the previous note.
"""
from .groove import swung_beat
from .score import NoteEvent

ROOT_LOW = 31
ROOT_HIGH = 47
TOP = 50
_GAP_BEATS = 0.12


def nearest(pc, previous, low=ROOT_LOW, high=ROOT_HIGH):
    options = [p for p in range(low, high + 1) if p % 12 == pc]
    return min(options, key=lambda p: (abs(p - previous), p))


def _approach_pc(target_pc, current_pitch, key):
    scale = key.scale
    index = scale.index(target_pc)
    below, above = scale[(index - 1) % 7], scale[(index + 1) % 7]
    return above if current_pitch % 12 > target_pc else below


def bar_notes(bar_start, slots, kick_steps, mode, swing, key, next_bass_pc, previous, rng):
    """Bass notes for one bar. `slots` are the bar's ChordSlots; returns (notes, last pitch)."""
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
