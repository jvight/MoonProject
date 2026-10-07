"""
Ro's cassette tapes (docs/STORY.md "Cassettes"): one track per collectable tape, played by the radio only once 07
owns the tape. Tapes live in the same world as the station (D major family, lofi, calm) but each has its own tempo,
groove, instruments and form, and every tape loops seamlessly because the Tape Deck plays one tape on repeat.
A tape's id is its cassette id, which names its file and its tapes.json entry.
"""
from .spec import Mood, SectionPlan, TrackSpec

TAPES = (
    TrackSpec(
        number=7, slug="after_dark_1", title="Lumen After Dark, Vol. 1", seed=1107, bpm=84.0, swing=0.64,
        key="D major", family="I-IV-iii-vi", drums="bounce", ending="loop", tape=True, themes=(("a", "jingle"),),
        mood=Mood(warmth_hz=14000.0, dust_db=-37.0, wow_cents=5.5, space=0.4, pump_db=3.5, substitution=0.3,
                  texture="cassette", presence_db=2.5),
        form=(
            SectionPlan("On Air", ("F",), drums="light", bass="hold", pad=True, lead="vibes", quote="jingle",
                        drum_bars=(2, 4), opening=(0.6, 0.9)),
            SectionPlan("A", ("A1", "A2"), keys="stabs", lead="vibes", density=0.6, theme="a"),
            SectionPlan("B", ("B1", "B2"), pad=True, lead="vibes", density=0.5, theme="b"),
            SectionPlan("A'", ("A3", "A2"), keys="stabs", pad=True, lead="vibes", density=0.7, theme="a"),
            SectionPlan("Break", ("B1",), drums="sparse", keys="arp", bass="hold", pad=True, opening=(0.8, 0.75)),
            SectionPlan("B'", ("B1", "B2"), keys="stabs", pad=True, lead="vibes", density=0.45, theme="b"),
            SectionPlan("A''", ("A1",), keys="stabs", lead="vibes", density=0.5, theme="a"),
            SectionPlan("Outro", ("O",), drums="light", bass="hold", pad=True, drum_bars=(0, 3),
                        opening=(0.9, 0.6)),
        ),
        notes=("Ro's night show opener: the brightest and bounciest tape. It opens On Air with Bell's station jingle "
               "on vibraphone over a held D6/9, and the A theme is that jingle re-sung over the changes. Finger "
               "snaps on 2 and 4, the swingiest hats and shaker of the catalogue, off-beat Rhodes stabs and a "
               "bouncing bass; faster chord changes (two per bar at phrase ends), cassette hiss instead of vinyl, "
               "and a presence lift that gives it the brightest top end of any track."),
    ),
    TrackSpec(
        number=8, slug="dust_and_honey", title="Dust & Honey", seed=2308, bpm=72.0, swing=0.62,
        key="D major", family="I-IV/I", drums="jam", ending="loop", tape=True,
        events=((89.5, "thump", 0.85),),
        mood=Mood(warmth_hz=10500.0, dust_db=-35.0, wow_cents=8.0, space=0.3, pump_db=1.5, substitution=0.35,
                  texture="airlock", width=0.45),
        form=(
            SectionPlan("Count-in", ("A1",), drums="light", bass="walk", drum_bars=(1, 4), opening=(0.5, 0.8)),
            SectionPlan("A", ("A1", "A2"), drums="light", bass="walk", lead="rhodes", density=0.5, theme="a"),
            SectionPlan("B", ("B1", "B2"), bass="walk", pad=True, lead="rhodes", density=0.5, theme="b"),
            SectionPlan("Airlock", ("A1",), drums="light", keys="arp", bass="hold", pad=True, drum_bars=(0, 2),
                        opening=(0.8, 0.7)),
            SectionPlan("A'", ("A3", "A2"), bass="walk", lead="rhodes", density=0.65, theme="a"),
            SectionPlan("B'", ("B1", "B2"), drums="light", bass="walk", pad=True, lead="rhodes", density=0.4,
                        theme="b"),
            SectionPlan("Outro", ("O",), drums="light", bass="walk", opening=(0.8, 0.5)),
        ),
        notes=("A loose jam recorded with one mic taped to the airlock: honeyed Rhodes melody (auto-panned) over "
               "Rhodes comping that vamps between D and G-over-D, a walking bass, brushes stirring on every beat "
               "with lazy brush slaps and a drummer who sits well behind the beat. The mix is narrow and boxy "
               "(close metal reflections, a low-mid bloom, room tone with a faint hum on D and A under the tape "
               "hiss). In the Airlock break the band thins out and someone (Kenji) bumps the hatch: one soft, "
               "muffled thump on the and of 2."),
    ),
    TrackSpec(
        number=9, slug="slow_orbit", title="Slow Orbit", seed=4409, bpm=64.0, swing=0.54,
        key="D major", family="I-vi-IV#11", drums="pulse", ending="loop", tape=True, keys_voice="felt",
        mood=Mood(warmth_hz=10000.0, dust_db=-37.0, wow_cents=9.0, space=0.85, pump_db=1.0, substitution=0.2,
                  texture="cassette", orbit_bars=4),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", keys="rock", bass="off", pad=True, opening=(0.35, 0.7)),
            SectionPlan("A", ("A1", "A2"), drums="light", keys="rock", bass="hold", lead="theremin", density=0.35,
                        theme="a"),
            SectionPlan("B", ("B1", "B2"), drums="light", keys="rock", bass="hold", pad=True, lead="felt",
                        density=0.4, theme="b"),
            SectionPlan("A'", ("A3", "A2"), drums="light", keys="rock", bass="hold", lead="theremin", density=0.45,
                        theme="a"),
            SectionPlan("B'", ("B1", "B2"), drums="off", keys="rock", bass="hold", pad=True, lead="felt",
                        density=0.3, theme="b"),
            SectionPlan("Outro", ("O",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.7, 0.35)),
        ),
        notes=("The slowest tape, a lullaby: felt-piano broken chords instead of Rhodes, long chords that hold for "
               "two bars, a Lydian G (maj9#11) for the floating feel, and a wide pad that drifts left and right "
               "once every four bars like something in orbit. The A theme is sung by a soft theremin-like voice "
               "that glides between notes; the B theme by a high felt piano. The only drums are a heartbeat kick, "
               "a rim on 3 and a brush stir, and only in the middle of the tape."),
    ),
)
