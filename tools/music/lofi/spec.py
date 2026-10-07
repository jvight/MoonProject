"""
Track specifications: one frozen description per radio track or cassette tape drives every decision
deterministically. A spec names its key, progression family, drummer, form (a list of section plans) and mood.
"""
from dataclasses import dataclass, field

from .harmony import BEATS_PER_BAR

DRUM_LEVELS = ("off", "sparse", "light", "full")
KEYS_MODES = ("comp", "stabs", "arp", "rock", "off")
BASS_MODES = ("off", "hold", "groove", "walk")
ENDINGS = ("fade", "tapestop", "loop")
KEYS_VOICES = ("rhodes", "felt")
TEXTURES = ("vinyl", "cassette", "airlock")
STATION_BPM = (68.0, 84.0)
TAPE_BPM = (60.0, 90.0)


@dataclass(frozen=True)
class SectionPlan:
    """
    One section of the form. `phrases` are family phrase ids (four bars each). `drum_bars` limits the drums to a
    bar range inside the section. `theme` groups sections that share a melodic motif (the first one invents it,
    later ones re-sing it). `opening` is the master low-pass openness (0 = muffled, 1 = fully open) at the
    section's start and end. `quote` names a fixed phrase (lofi.jingle.QUOTES) the lead plays verbatim over the
    section's first two bars instead of a composed melody; the rest of the section rests.
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
    quote: str | None = None

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
        if self.quote is not None and self.lead is None:
            raise ValueError(f"section {self.name}: quote {self.quote!r} needs a lead voice to play it")


@dataclass(frozen=True)
class Mood:
    """Production colour. warmth_hz: master low-pass when fully open; dust_db: surface-noise bed level vs. the mix;
    wow_cents: tape wow depth; space: reverb send 0..1; pump_db: sidechain duck; substitution: chord colour
    substitution rate; texture: the bed under the music ("vinyl" record surface, "cassette" tape hiss, "airlock"
    tape hiss plus the hum of a small metal room and its early reflections); width: stereo width of the music
    (1 = as mixed, lower folds the sides in like a single room mic); orbit_bars: period of the pad's slow
    left-right drift in bars (0 = still); presence_db: a high-shelf lift of the top in the master (0 = none) for a
    crisper, live-on-air sound."""
    warmth_hz: float = 12000.0
    dust_db: float = -34.0
    wow_cents: float = 7.0
    space: float = 0.5
    pump_db: float = 3.0
    substitution: float = 0.3
    texture: str = "vinyl"
    width: float = 1.0
    orbit_bars: int = 0
    presence_db: float = 0.0


@dataclass(frozen=True)
class TrackSpec:
    """
    Everything that defines one radio track or cassette tape. A tape (`tape=True`) is named by its cassette id
    (the slug) instead of "NN_slug"; `number` is then only its catalogue number for the command line.
    `keys_voice` picks the comping instrument; `themes` seeds melodic themes from quotes instead of inventing them
    ((theme, quote name) pairs); `events` are one-off kit hits ((beat, voice, velocity)), e.g. a thump on the
    airlock door; `notes` is the listening description printed in the reports.
    """
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
    tape: bool = False
    keys_voice: str = "rhodes"
    themes: tuple = ()
    events: tuple = ()
    notes: str = ""

    @property
    def id(self):
        return self.slug if self.tape else f"{self.number:02d}_{self.slug}"

    @property
    def bars(self):
        return sum(section.bars for section in self.form)

    @property
    def music_beats(self):
        return self.bars * BEATS_PER_BAR

    def validate(self):
        low, high = TAPE_BPM if self.tape else STATION_BPM
        if not low <= self.bpm <= high:
            raise ValueError(f"{self.id}: {self.bpm} BPM is outside the {low:.0f}-{high:.0f} range")
        if not 0.5 <= self.swing <= 0.66:
            raise ValueError(f"{self.id}: swing {self.swing} outside 0.5..0.66")
        if self.ending not in ENDINGS:
            raise ValueError(f"{self.id}: unknown ending {self.ending!r}")
        if self.keys_voice not in KEYS_VOICES or self.mood.texture not in TEXTURES:
            raise ValueError(f"{self.id}: unknown keys voice {self.keys_voice!r} or texture {self.mood.texture!r}")
        if not 0.0 <= self.mood.width <= 1.0:
            raise ValueError(f"{self.id}: width {self.mood.width} outside 0..1")
        if self.mood.orbit_bars and self.bars % self.mood.orbit_bars:
            raise ValueError(f"{self.id}: the pad orbit ({self.mood.orbit_bars} bars) must divide {self.bars} bars")
        if self.ending == "loop" and self.form[-1].opening[1] != self.form[0].opening[0]:
            raise ValueError(f"{self.id}: a loop must end as open as it starts (no jump in brightness at the seam)")
        for beat, _, _ in self.events:
            if not 0.0 <= beat < self.music_beats:
                raise ValueError(f"{self.id}: event at beat {beat} is outside the music")
        for section in self.form:
            section.validate()
