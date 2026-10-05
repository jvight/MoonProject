"""
Track specifications: one frozen description per radio track drives every decision deterministically.
A spec names its key, progression family, drummer, form (a list of section plans) and mood.
"""
from dataclasses import dataclass, field

from .harmony import BEATS_PER_BAR

DRUM_LEVELS = ("off", "sparse", "light", "full")
KEYS_MODES = ("comp", "arp", "off")
BASS_MODES = ("off", "hold", "groove")
ENDINGS = ("fade", "tapestop")


@dataclass(frozen=True)
class SectionPlan:
    """
    One section of the form. `phrases` are family phrase ids (four bars each). `drum_bars` limits the drums to a
    bar range inside the section. `theme` groups sections that share a melodic motif (the first one invents it,
    later ones re-sing it). `opening` is the master low-pass openness (0 = muffled, 1 = fully open) at the
    section's start and end.
    """
    name: str
    phrases: tuple
    drums: str = "full"
    keys: str = "comp"
    bass: str = "groove"
    pad: bool = False
    lead: str | None = None
    density: float = 0.5
    theme: str = "a"
    opening: tuple = (1.0, 1.0)
    drum_bars: tuple | None = None

    @property
    def bars(self):
        return 4 * len(self.phrases)

    def drums_at(self, bar):
        if self.drum_bars is not None and not self.drum_bars[0] <= bar < self.drum_bars[1]:
            return "off"
        return self.drums

    def validate(self):
        if self.drums not in DRUM_LEVELS or self.keys not in KEYS_MODES or self.bass not in BASS_MODES:
            raise ValueError(f"section {self.name}: invalid arrangement {self.drums}/{self.keys}/{self.bass}")


@dataclass(frozen=True)
class Mood:
    """Production colour. warmth_hz: master low-pass when fully open; dust_db: vinyl level vs. the mix;
    wow_cents: tape wow depth; space: reverb send 0..1; pump_db: sidechain duck; substitution: chord colour
    substitution rate."""
    warmth_hz: float = 12000.0
    dust_db: float = -34.0
    wow_cents: float = 7.0
    space: float = 0.5
    pump_db: float = 3.0
    substitution: float = 0.3


@dataclass(frozen=True)
class TrackSpec:
    """Everything that defines one radio track."""
    number: int
    slug: str
    title: str
    seed: int
    bpm: float
    swing: float
    key: str
    family: str
    drums: str
    form: tuple
    mood: Mood = field(default_factory=Mood)
    ending: str = "fade"

    @property
    def id(self):
        return f"{self.number:02d}_{self.slug}"

    @property
    def bars(self):
        return sum(section.bars for section in self.form)

    @property
    def music_beats(self):
        return self.bars * BEATS_PER_BAR

    def validate(self):
        if not 68.0 <= self.bpm <= 84.0:
            raise ValueError(f"{self.id}: {self.bpm} BPM is outside the station's 68-84 range")
        if not 0.5 <= self.swing <= 0.66:
            raise ValueError(f"{self.id}: swing {self.swing} outside 0.5..0.66")
        if self.ending not in ENDINGS:
            raise ValueError(f"{self.id}: unknown ending {self.ending!r}")
        for section in self.form:
            section.validate()
