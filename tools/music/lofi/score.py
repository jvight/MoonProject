"""
The score: everything the composer decides, in musical time (beats), before any sound is made.
Seconds are derived only at render time (`seconds = beat * 60 / bpm + offset_s`).
"""
from bisect import bisect_right
from dataclasses import dataclass, field


@dataclass(frozen=True)
class NoteEvent:
    """A pitched note. `glide` asks a monophonic voice to slide into this pitch from the previous one."""
    beat: float
    length: float
    pitch: int
    velocity: float
    offset_s: float = 0.0
    glide: bool = False

    @property
    def end(self):
        return self.beat + self.length


@dataclass(frozen=True)
class ChordEvent:
    """A chord as heard: when, how long, which chord, and the keys / pad voicings that sound it."""
    beat: float
    length: float
    chord: object
    keys_voicing: tuple
    pad_voicing: tuple

    @property
    def end(self):
        return self.beat + self.length


@dataclass(frozen=True)
class SectionSpan:
    """Where a planned section landed in the song."""
    name: str
    start_beat: float
    bars: int
    plan: object


class HarmonyTimeline:
    """Chord lookup by beat."""

    def __init__(self, chords):
        self.chords = list(chords)
        self._starts = [c.beat for c in self.chords]

    def at(self, beat):
        index = bisect_right(self._starts, beat + 1e-9) - 1
        if index < 0:
            raise ValueError(f"no chord sounds at beat {beat}")
        return self.chords[index]


@dataclass
class Score:
    """A composed track. `parts` maps part name ("keys", "pad", "bass", "lead.kalimba", ...) to NoteEvents."""
    spec: object
    sections: list
    chords: list
    parts: dict = field(default_factory=dict)
    drums: list = field(default_factory=list)
    cutoff: list = field(default_factory=list)
    end_beat: float = 0.0

    def seconds(self, beat):
        return beat * 60.0 / self.spec.bpm

    @property
    def duration_s(self):
        return self.seconds(self.end_beat)

    @property
    def timeline(self):
        return HarmonyTimeline(self.chords)
