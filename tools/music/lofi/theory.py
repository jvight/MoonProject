"""
Pitch, key and chord vocabulary of the radio station.

Every track lives in D major or its relative B minor (the same seven pitch classes), so the game's sonar pings and
pickup chimes - D major pentatonic D E F# A B - always harmonise with whatever is playing. Chords carry jazz
extensions; a chord that deliberately leaves the key (the borrowed minor iv) must say so with `borrowed=True`.
"""
from dataclasses import dataclass

NOTE_NAMES = ("C", "C#", "D", "Eb", "E", "F", "F#", "G", "G#", "A", "Bb", "B")
_NUMERALS = ("i", "ii", "iii", "iv", "v", "vi", "vii")
_MAJOR_STEPS = (0, 2, 4, 5, 7, 9, 11)
_MINOR_STEPS = (0, 2, 3, 5, 7, 8, 10)


def note_name(midi):
    """MIDI number -> scientific pitch name, e.g. 62 -> "D4"."""
    return f"{NOTE_NAMES[midi % 12]}{midi // 12 - 1}"


@dataclass(frozen=True)
class Key:
    """A key centre: tonic pitch class plus mode ("major" or "minor", natural minor)."""
    tonic: int
    mode: str

    @property
    def name(self):
        return f"{NOTE_NAMES[self.tonic]} {self.mode}"

    @property
    def scale(self):
        steps = _MAJOR_STEPS if self.mode == "major" else _MINOR_STEPS
        return tuple((self.tonic + s) % 12 for s in steps)

    @property
    def pcs(self):
        return frozenset(self.scale)

    @property
    def pentatonic(self):
        """Major pentatonic of the (relative) major: degrees 1 2 3 5 6. Same set for D major and B minor."""
        major_tonic = self.tonic if self.mode == "major" else (self.tonic + 3) % 12
        return frozenset((major_tonic + s) % 12 for s in (0, 2, 4, 7, 9))

    def degree_root(self, numeral):
        """Roman numeral (case-insensitive, 1..7) -> root pitch class within this key."""
        return self.scale[_NUMERALS.index(numeral.lower())]


D_MAJOR = Key(2, "major")
B_MINOR = Key(11, "minor")
KEYS = {D_MAJOR.name: D_MAJOR, B_MINOR.name: B_MINOR}


def key_named(name):
    if name not in KEYS:
        raise ValueError(f"unknown key {name!r}; the radio only plays in {sorted(KEYS)}")
    return KEYS[name]


@dataclass(frozen=True)
class Quality:
    """
    A chord quality. Intervals are semitones above the root (mod 12), root first.
    `guide` tones must appear in every voicing (they carry the chord's identity); `tensions` are extra colours a
    melody may land on, used only when they belong to the key.
    """
    suffix: str
    intervals: tuple
    guide: tuple
    tensions: tuple


QUALITIES = {q.suffix: q for q in (
    Quality("maj7", (0, 4, 7, 11), (4, 11), (2, 9, 6)),
    Quality("maj9", (0, 4, 7, 11, 2), (4, 11, 2), (9, 6)),
    Quality("maj9#11", (0, 4, 7, 11, 2, 6), (4, 11, 6), (9,)),
    Quality("6/9", (0, 4, 7, 9, 2), (4, 9, 2), (6,)),
    Quality("m7", (0, 3, 7, 10), (3, 10), (2, 5)),
    Quality("m9", (0, 3, 7, 10, 2), (3, 10, 2), (5,)),
    Quality("m11", (0, 3, 7, 10, 2, 5), (3, 10, 5), ()),
    Quality("m6", (0, 3, 7, 9), (3, 9), (2,)),
    Quality("13", (0, 4, 7, 10, 2, 9), (4, 10, 9), ()),
    Quality("7sus4", (0, 5, 7, 10), (5, 10), (2, 9)),
    Quality("9sus4", (0, 5, 7, 10, 2), (5, 10, 2), (9,)),
    Quality("13sus4", (0, 5, 7, 10, 2, 9), (5, 10, 9), ()),
)}


@dataclass(frozen=True)
class Chord:
    """A chord: root pitch class, quality, optional different bass pitch class, and the borrowed flag."""
    root: int
    quality: Quality
    bass: int
    borrowed: bool = False

    @property
    def symbol(self):
        text = NOTE_NAMES[self.root] + self.quality.suffix
        if self.bass != self.root:
            text += "/" + NOTE_NAMES[self.bass]
        return text

    @property
    def tones(self):
        return frozenset((self.root + i) % 12 for i in self.quality.intervals)

    @property
    def guide_tones(self):
        return frozenset((self.root + i) % 12 for i in self.quality.guide)

    @property
    def fifth(self):
        return (self.root + 7) % 12

    def available_tensions(self, key):
        return frozenset((self.root + t) % 12 for t in self.quality.tensions if (self.root + t) % 12 in key.pcs)

    def melodic_pcs(self, key):
        """Pitch classes a melody may land on over this chord: chord tones plus in-key tensions."""
        return self.tones | self.available_tensions(key)

    def out_of_key(self, key):
        return self.tones - key.pcs


def parse_chord(token, key):
    """
    Parse "numeral:quality[/bass_numeral][!]" relative to `key`, e.g. "ii:m9", "I:maj9/iii", "iv:m6!".
    "!" marks a borrowed chord; the parser refuses chords that leave the key without it (and vice versa).
    """
    borrowed = token.endswith("!")
    body = token.rstrip("!")
    numeral, _, suffix = body.partition(":")
    bass_numeral = ""
    head, _, tail = suffix.rpartition("/")
    if head and tail.lower() in _NUMERALS:
        suffix, bass_numeral = head, tail
    if suffix not in QUALITIES:
        raise ValueError(f"unknown chord quality {suffix!r} in {token!r}")
    root = key.degree_root(numeral)
    bass = key.degree_root(bass_numeral) if bass_numeral else root
    chord = Chord(root, QUALITIES[suffix], bass, borrowed)
    if bool(chord.out_of_key(key)) != borrowed:
        raise ValueError(f"{token!r} -> {chord.symbol}: borrowed flag does not match its notes in {key.name}")
    return chord
