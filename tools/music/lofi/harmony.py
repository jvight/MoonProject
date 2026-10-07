"""
Progression families: four-bar phrases written in Roman numerals for one key centre, plus seeded colour
substitutions that keep each chord's function (maj9 <-> 6/9, m9 <-> m11, 13 <-> 13sus4 ...) so repeated phrases are
never literally identical. A bar holding two chords splits its four beats evenly.
"""
from dataclasses import dataclass

from .theory import QUALITIES, Chord, parse_chord

BEATS_PER_BAR = 4

_SUBSTITUTES = {
    "maj7": ("maj9", "6/9"),
    "maj9": ("maj7", "6/9"),
    "6/9": ("maj9",),
    "maj9#11": ("maj9",),
    "m7": ("m9", "m11"),
    "m9": ("m11", "m7"),
    "m11": ("m9",),
    "13": ("13sus4", "9sus4"),
    "13sus4": ("9sus4", "13"),
    "9sus4": ("13sus4",),
    "7sus4": ("9sus4",),
}


@dataclass(frozen=True)
class ChordSlot:
    """One chord held for `beats` beats."""
    chord: Chord
    beats: float


@dataclass(frozen=True)
class Family:
    """A progression family: named four-bar phrases ("bar | bar | bar | bar") for one mode."""
    name: str
    mode: str
    phrases: dict


FAMILIES = {f.name: f for f in (
    Family("I-vi-ii-V", "major", {
        "A1": "I:maj9 | vi:m9 | ii:m9 | V:13sus4",
        "A2": "I:maj9 | vi:m9 | ii:m9 | V:13sus4 V:13",
        "A3": "I:6/9 | vi:m11 | ii:m9 | V:9sus4",
        "B1": "IV:maj9 | iii:m7 | vi:m9 | V:13sus4",
        "B2": "IV:maj9 | iii:m7 | ii:m9 | V:9sus4 V:13",
        "O": "I:maj9 | vi:m9 | IV:maj9 | I:6/9",
    }),
    Family("IV-iv-I", "major", {
        "A1": "I:maj9 | IV:maj9 | iv:m6! | I:maj9/iii",
        "A2": "ii:m9 | V:13sus4 | I:6/9 | vi:m9",
        "A3": "I:maj7 | IV:maj9#11 | iv:m6! | I:6/9/iii",
        "B1": "vi:m9 | iii:m7 | IV:maj9 | V:13sus4",
        "B2": "vi:m9 | iii:m7 | ii:m9 | iv:m6!",
        "O": "I:maj9 | IV:maj9 | iv:m6! | I:6/9",
    }),
    Family("IV-iii-ii-I", "major", {
        "A1": "IV:maj9 | iii:m7 | ii:m9 | I:maj9",
        "A2": "IV:maj9 | iii:m7 | ii:m9 V:13sus4 | I:6/9",
        "A3": "IV:maj9#11 | iii:m7 | ii:m11 | I:maj9",
        "B1": "vi:m9 | V:13sus4 | IV:maj9 | iii:m7",
        "B2": "ii:m9 | iii:m7 | IV:maj9 | V:13sus4 V:13",
        "O": "IV:maj9 | iii:m7 | ii:m9 | I:maj9",
    }),
    Family("i-iv-VI-v", "minor", {
        "A1": "i:m9 | iv:m9 | VI:maj9 | v:m7",
        "A2": "i:m9 | iv:m9 | VI:maj9#11 | v:7sus4",
        "A3": "i:m11 | iv:m9 | VI:maj7 | v:m7 VII:13sus4",
        "B1": "III:maj9 | VII:6/9 | VI:maj9 | iv:m9",
        "B2": "III:maj9 | VII:13sus4 | VI:maj9 | v:7sus4",
        "O": "i:m9 | VI:maj9 | iv:m9 | i:m11",
    }),
    Family("ii-V-I", "major", {
        "A1": "ii:m9 | V:13 | I:maj9 | vi:m9",
        "A2": "ii:m9 | V:13sus4 V:13 | I:maj9 | I:6/9",
        "A3": "ii:m11 | V:9sus4 | I:6/9 | vi:m11",
        "B1": "IV:maj9 | V:13sus4 | iii:m7 | vi:m9",
        "B2": "ii:m9 | iii:m7 | IV:maj9 | V:13sus4 V:13",
        "O": "ii:m9 | V:13sus4 | I:maj9 | I:6/9",
    }),
    Family("i-VI-III-VII", "minor", {
        "A1": "i:m9 | VI:maj7 | III:maj9 | VII:6/9",
        "A2": "i:m9 | VI:maj9 | III:maj9 | VII:13sus4",
        "A3": "i:m11 | VI:maj9#11 | III:6/9 | VII:13sus4 VII:13",
        "B1": "iv:m9 | v:m7 | VI:maj9 | VII:6/9",
        "B2": "iv:m9 | III:maj9 | VI:maj9#11 | v:7sus4",
        "O": "i:m9 | VI:maj9 | III:maj9 | i:m9",
    }),
    # Tape families. Each outro ends on V so the tape loops straight back into its first chord.
    Family("I-IV-iii-vi", "major", {
        "F": "I:6/9= | I:6/9= | ii:m9 | V:13sus4",
        "A1": "I:maj9 | IV:maj9 | iii:m7 vi:m9 | ii:m9 V:13",
        "A2": "I:maj9 | IV:maj9 | iii:m7 vi:m9 | ii:m9 V:13sus4",
        "A3": "I:6/9 | IV:maj9#11 | iii:m7 vi:m11 | ii:m11 V:13",
        "B1": "IV:maj9 | V:13sus4 | iii:m7 | vi:m9",
        "B2": "ii:m9 | iii:m7 | IV:maj9 | V:13sus4 V:13",
        "O": "I:maj9 | IV:maj9 | ii:m9 | V:13sus4",
    }),
    Family("I-IV/I", "major", {
        "A1": "I:maj9 | IV:maj9/I | I:maj9 | IV:maj9/I",
        "A2": "I:maj9 | IV:maj9/I | iii:m7 | vi:m9",
        "A3": "I:6/9 | IV:maj9#11/I | I:maj9 | ii:m9 V:13sus4",
        "B1": "ii:m9 | iii:m7 | IV:maj9 | V:13sus4",
        "B2": "ii:m11 | iii:m7 | IV:maj9 | V:13sus4 V:13",
        "O": "I:maj9 | IV:maj9/I | ii:m9 | V:13sus4",
    }),
    Family("I-vi-IV#11", "major", {
        "A1": "I:maj9 | I:maj9 | vi:m9 | vi:m9",
        "A2": "IV:maj9#11 | IV:maj9#11 | I:maj9/iii | V:13sus4",
        "A3": "I:6/9 | I:maj9 | vi:m11 | vi:m9",
        "B1": "ii:m9 | ii:m9 | IV:maj9 | IV:maj9",
        "B2": "vi:m11 | iii:m7 | IV:maj9#11 | V:13sus4",
        "O": "I:maj9 | vi:m9 | IV:maj9#11 | V:13sus4",
    }),
)}


def family_named(name):
    if name not in FAMILIES:
        raise ValueError(f"unknown progression family {name!r}; known: {sorted(FAMILIES)}")
    return FAMILIES[name]


def parse_phrase(text, key):
    """"I:maj9 | ii:m9 V:13" -> [[Chord], [Chord, Chord]] (one list per bar)."""
    return [[parse_chord(token, key) for token in bar.split()] for bar in text.split("|")]


def substitute(chord, key, rng, rate):
    """
    With probability `rate`, swap the chord's colour for a sibling quality that stays in (or keeps) its key.
    A fixed chord is returned before any draw, so marking one never reshuffles the choices around it.
    """
    if chord.fixed:
        return chord
    options = _SUBSTITUTES.get(chord.quality.suffix, ())
    if not options or rng.random() >= rate:
        return chord
    choice = QUALITIES[options[int(rng.integers(len(options)))]]
    swapped = Chord(chord.root, choice, chord.bass, chord.borrowed)
    return swapped if bool(swapped.out_of_key(key)) == chord.borrowed else chord


def phrase_bars(family, key, phrase_ids, rng, substitution_rate):
    """Bars (lists of ChordSlot) for consecutive phrases, with seeded colour substitutions."""
    if family.mode != key.mode:
        raise ValueError(f"family {family.name} is written for {family.mode}, track key is {key.name}")
    bars = []
    for phrase_id in phrase_ids:
        for bar in parse_phrase(family.phrases[phrase_id], key):
            beats = BEATS_PER_BAR / len(bar)
            bars.append([ChordSlot(substitute(c, key, rng, substitution_rate), beats) for c in bar])
    return bars
