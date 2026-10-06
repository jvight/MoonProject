"""
Theory checks on a composed Score (no audio): chord chart, out-of-key notes, melody rule violations and
melody-against-accompaniment clashes. Used by the unit tests and printed by build_music.py.
"""
from .comping import PUSH_BEATS
from .harmony import BEATS_PER_BAR
from .melody import is_strong
from .theory import key_named

_EPS = 1e-6


def chord_chart(score):
    """{section name: ["| Dmaj9 | Bm9 | Em9 A13sus4 |" ...]} - one line per four bars."""
    chart = []
    for section in score.sections:
        bars = []
        for bar in range(section.bars):
            start = section.start_beat + bar * BEATS_PER_BAR
            symbols = [c.chord.symbol for c in score.chords if start - _EPS <= c.beat < start + BEATS_PER_BAR - _EPS]
            bars.append(" ".join(symbols))
        lines = ["| " + " | ".join(bars[i:i + 4]) + " |" for i in range(0, len(bars), 4)]
        chart.append((section.name, lines))
    return chart


def _borrowed_pcs_near(score, beat, length):
    pcs = set()
    for event in score.chords:
        if event.chord.borrowed and event.beat < beat + length + PUSH_BEATS and event.end > beat - _EPS:
            pcs |= event.chord.tones
    return pcs


def out_of_key_notes(score):
    """Notes outside the key that no borrowed chord (sounding or pushed in) explains: [(part, NoteEvent)]."""
    key = key_named(score.spec.key)
    found = []
    for part, notes in score.parts.items():
        for note in notes:
            pc = note.pitch % 12
            if pc not in key.pcs and pc not in _borrowed_pcs_near(score, note.beat, note.length):
                found.append((part, note))
    return found


def _lead_parts(score):
    return {name: notes for name, notes in score.parts.items() if name.startswith("lead.")}


def _bar_steps(score, beat):
    """(16th step inside its two-bar frame rounded to the grid, length in steps) for rule checks."""
    swing = score.spec.swing
    within = beat % (2 * BEATS_PER_BAR)
    beat_index, frac = divmod(within, 1.0)
    pair, rest = divmod(frac, 0.5)
    second = 1 if rest > 0.25 * swing else 0
    return int(beat_index) * 4 + int(pair) * 2 + second


def melody_report(score):
    """Counts of melody rule breaks and clashes, plus the offending notes for diagnosis."""
    key = key_named(score.spec.key)
    timeline = score.timeline
    accompaniment = score.parts.get("keys", []) + score.parts.get("pad", [])
    report = {"notes": 0, "non_pentatonic": [], "strong_off_chord": [], "clashes": []}
    for part, notes in _lead_parts(score).items():
        for index, note in enumerate(notes):
            report["notes"] += 1
            pc = note.pitch % 12
            if pc not in key.pentatonic:
                report["non_pentatonic"].append((part, note))
            following = notes[index + 1] if index + 1 < len(notes) else None
            phrase_end = following is None or following.beat - note.end > 1.0
            length_steps = round(note.length * 4)
            strong = is_strong(_bar_steps(score, note.beat), length_steps, phrase_end)
            chord = timeline.at(note.beat).chord
            if (strong or chord.borrowed) and pc not in chord.melodic_pcs(key):
                report["strong_off_chord"].append((part, note, chord.symbol))
            if length_steps < 2 and not strong:
                continue
            for other in accompaniment:
                overlap = other.beat < note.end - _EPS and other.end > note.beat + _EPS
                if overlap and abs(note.pitch - other.pitch) in (1, 13):
                    report["clashes"].append((part, note, other.pitch))
    return report


def summary(score):
    """Plain-data theory summary for the analysis report."""
    melody = melody_report(score)
    return {
        "chord_chart": {name: lines for name, lines in chord_chart(score)},
        "out_of_key_notes": len(out_of_key_notes(score)),
        "borrowed_chords": sorted({c.chord.symbol for c in score.chords if c.chord.borrowed}),
        "lead_notes": melody["notes"],
        "lead_non_pentatonic": len(melody["non_pentatonic"]),
        "lead_strong_off_chord": len(melody["strong_off_chord"]),
        "lead_clashes": len(melody["clashes"]),
        "parts": {name: len(notes) for name, notes in score.parts.items()},
        "drum_hits": len(score.drums),
    }
