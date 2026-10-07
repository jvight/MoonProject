"""
The radio station's playlist. Each spec gives a track its own personality: tempo, swing, drummer, progression
family, lead voices and production colour. Track ids are "NN_slug" and name the rendered files. The catalogue adds
Ro's cassette tapes (lofi.tapes), which the radio only plays once they are collected.
"""
from .spec import Mood, SectionPlan, TrackSpec
from .tapes import TAPES

PLAYLIST = (
    TrackSpec(
        number=1, slug="earthrise_static", title="Earthrise Static", seed=1701, bpm=76.0, swing=0.58,
        key="D major", family="I-vi-ii-V", drums="boombap", ending="fade",
        mood=Mood(warmth_hz=12500.0, dust_db=-34.0, wow_cents=7.0, space=0.5, pump_db=3.0, substitution=0.3),
        form=(
            SectionPlan("Intro", ("A1",), drums="sparse", bass="off", drum_bars=(2, 4), opening=(0.25, 0.75)),
            SectionPlan("A", ("A1", "A2"), lead="kalimba", density=0.45, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="flute", density=0.5, theme="b"),
            SectionPlan("A'", ("A3", "A2"), pad=True, lead="kalimba", density=0.6, theme="a"),
            SectionPlan("Break", ("B1",), drums="sparse", keys="arp", bass="hold", pad=True,
                        opening=(0.78, 0.72)),
            SectionPlan("B'", ("B1", "B2"), pad=True, lead="flute", density=0.4, theme="b"),
            SectionPlan("Outro", ("O",), drums="light", bass="hold", pad=True, lead="kalimba", density=0.2,
                        drum_bars=(0, 2), opening=(0.9, 0.2)),
        ),
    ),
    TrackSpec(
        number=2, slug="sevens_lullaby", title="Seven's Lullaby", seed=707, bpm=70.0, swing=0.6,
        key="D major", family="IV-iv-I", drums="brushy", ending="tapestop",
        mood=Mood(warmth_hz=11000.0, dust_db=-31.0, wow_cents=10.0, space=0.65, pump_db=2.5, substitution=0.25),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", keys="arp", bass="off", lead="musicbox", density=0.25,
                        opening=(0.3, 0.75)),
            SectionPlan("A", ("A1", "A2"), drums="light", lead="musicbox", density=0.4, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="kalimba", density=0.45, theme="b"),
            SectionPlan("A'", ("A3", "A2"), pad=True, lead="musicbox", density=0.55, theme="a"),
            SectionPlan("B'", ("B1", "B2"), drums="light", pad=True, lead="kalimba", density=0.35, theme="b"),
            SectionPlan("Outro", ("O",), drums="off", keys="arp", bass="hold", pad=True, lead="musicbox",
                        density=0.2, opening=(0.85, 0.25)),
        ),
    ),
    TrackSpec(
        number=3, slug="dust_on_the_dial", title="Dust on the Dial", seed=4242, bpm=82.0, swing=0.56,
        key="D major", family="IV-iii-ii-I", drums="boombap", ending="tapestop",
        mood=Mood(warmth_hz=13000.0, dust_db=-32.0, wow_cents=8.0, space=0.4, pump_db=3.5, substitution=0.35),
        form=(
            SectionPlan("Intro", ("A1",), drums="light", bass="off", drum_bars=(2, 4), opening=(0.2, 0.8)),
            SectionPlan("A", ("A1", "A2"), lead="kalimba", density=0.5, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="flute", density=0.55, theme="b"),
            SectionPlan("A'", ("A3", "A2"), pad=True, lead="kalimba", density=0.65, theme="a"),
            SectionPlan("Break", ("A1",), drums="sparse", keys="arp", bass="hold", pad=True,
                        opening=(0.72, 0.72)),
            SectionPlan("A''", ("A1", "A3"), lead="flute", density=0.45, theme="a"),
            SectionPlan("Outro", ("O",), drums="light", bass="hold", pad=True, drum_bars=(0, 2),
                        opening=(0.9, 0.2)),
        ),
    ),
    TrackSpec(
        number=4, slug="long_way_home", title="Long Way Home", seed=1969, bpm=74.0, swing=0.6,
        key="B minor", family="i-iv-VI-v", drums="laidback", ending="fade",
        mood=Mood(warmth_hz=12000.0, dust_db=-35.0, wow_cents=6.0, space=0.6, pump_db=3.0, substitution=0.3),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", bass="off", pad=True, opening=(0.3, 0.75)),
            SectionPlan("A", ("A1", "A2"), pad=True, lead="flute", density=0.45, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="kalimba", density=0.5, theme="b"),
            SectionPlan("A'", ("A3", "A2"), pad=True, lead="flute", density=0.6, theme="a"),
            SectionPlan("Break", ("B1",), drums="sparse", keys="arp", bass="hold", pad=True,
                        opening=(0.75, 0.7)),
            SectionPlan("B'", ("B1", "B2"), lead="kalimba", density=0.45, theme="b"),
            SectionPlan("Outro", ("O",), drums="off", bass="hold", pad=True, lead="flute", density=0.2,
                        opening=(0.85, 0.2)),
        ),
    ),
    TrackSpec(
        number=5, slug="low_orbit_tea", title="Low Orbit Tea", seed=3141, bpm=80.0, swing=0.62,
        key="D major", family="ii-V-I", drums="laidback", ending="fade",
        mood=Mood(warmth_hz=13500.0, dust_db=-36.0, wow_cents=6.0, space=0.45, pump_db=3.0, substitution=0.35),
        form=(
            SectionPlan("Intro", ("A1",), drums="sparse", bass="off", drum_bars=(1, 4), opening=(0.25, 0.8)),
            SectionPlan("A", ("A1", "A2"), lead="kalimba", density=0.55, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="musicbox", density=0.45, theme="b"),
            SectionPlan("A'", ("A3", "A2"), lead="kalimba", density=0.65, theme="a"),
            SectionPlan("B'", ("B1", "B2"), drums="light", pad=True, lead="musicbox", density=0.4, theme="b"),
            SectionPlan("A''", ("A1", "A2"), pad=True, lead="kalimba", density=0.5, theme="a"),
            SectionPlan("Outro", ("O",), drums="light", bass="hold", pad=True, drum_bars=(0, 2),
                        opening=(0.9, 0.2)),
        ),
    ),
    TrackSpec(
        number=6, slug="signal_through_craters", title="Signal Through Craters", seed=2026, bpm=68.0, swing=0.57,
        key="B minor", family="i-VI-III-VII", drums="halftime", ending="fade",
        mood=Mood(warmth_hz=11500.0, dust_db=-33.0, wow_cents=9.0, space=0.75, pump_db=4.0, substitution=0.25),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", keys="arp", bass="off", pad=True, lead="musicbox",
                        density=0.2, opening=(0.25, 0.7)),
            SectionPlan("A", ("A1", "A2"), lead="flute", density=0.4, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="musicbox", density=0.35, theme="b"),
            SectionPlan("Break", ("A1",), drums="sparse", keys="arp", bass="hold", pad=True,
                        opening=(0.75, 0.7)),
            SectionPlan("A'", ("A3", "A2"), pad=True, lead="flute", density=0.55, theme="a"),
            SectionPlan("Outro", ("O",), drums="light", bass="hold", pad=True, lead="musicbox", density=0.2,
                        theme="b", drum_bars=(0, 2), opening=(0.9, 0.2)),
        ),
    ),
)


CATALOGUE = PLAYLIST + TAPES


def track_by_id(track_id):
    """A playlist track or a tape by id, slug or catalogue number ("3", "03", "dust_on_the_dial", "slow_orbit")."""
    for spec in CATALOGUE:
        if track_id in (spec.id, spec.slug, str(spec.number), f"{spec.number:02d}"):
            return spec
    raise ValueError(f"unknown track {track_id!r}; known: {[s.id for s in CATALOGUE]}")
