"""
Three procedural title-screen themes (Variants A, B, C) for 07's CRT title screen (docs/design/title-screen.md).

All three are in pure D major pentatonic (D E F# A B), 60-72 BPM, 2-3 minute seamless loops with no drums and no lofi
beat, combining a warm orbital pad, felt piano and celesta through soft tape-saturated 'Old Radio' (C2 'Radio bench')
warmth with at most a whisper of tape air (never static crackle), mastered to -20.0 LUFS.
"""
from .spec import TITLE_TARGET_LUFS, Mood, SectionPlan, TrackSpec

DEFAULT_TITLE_THEME_ID = "title_a_turning_moon"

TITLE_DESCRIPTIONS_VI = {
    "title_a_turning_moon": (
        "Phương án A (Khuyên dùng — sát mẫu nhất): Đệm pad ấm áp trôi chậm theo quỹ đạo mặt trăng, dương cầm nỉ "
        "(felt piano) rải hợp âm dịu dàng đối đáp cùng tiếng chuông celesta trong trẻo qua màu băng từ radio cổ "
        "ở mức -20 LUFS — tĩnh lặng, ấm áp và gợi chút kỳ diệu."
    ),
    "title_b_earthlight_cradle": (
        "Phương án B (Giàu giai điệu hơn): Giai điệu celesta, sáo và dương cầm nỉ ngân nga chủ đề ngũ cung D trưởng "
        "rõ nét, giàu chất tự sự hơn trên nền hợp âm rải mềm mại và đệm pad ấm áp."
    ),
    "title_c_quiet_horizon": (
        "Phương án C (Tối giản / Ambient): Nhịp 60 BPM chậm rãi, hợp âm pad ngân dài mênh mông như đường chân trời "
        "mặt trăng, điểm xuyết những nốt celesta và theremin xa xăm giữa khoảng lặng rộng mở."
    ),
}

TITLE_THEMES = (
    TrackSpec(
        number=10, slug="title_a_turning_moon", title="Little Turning Moon", seed=5101, bpm=66.0, swing=0.52,
        key="D major", family="I-vi-ii-V-pent", drums="pulse", ending="loop", title_theme=True,
        target_lufs=TITLE_TARGET_LUFS, keys_voice="felt",
        mood=Mood(warmth_hz=9200.0, dust_db=-48.0, wow_cents=6.5, space=0.8, pump_db=0.0, substitution=0.25,
                  texture="old_radio", orbit_bars=4),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.45, 0.72)),
            SectionPlan("A", ("A1", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.45, theme="a", opening=(0.72, 0.78)),
            SectionPlan("B", ("B1", "B2"), drums="off", keys="rock", bass="hold", pad=True, lead="felt",
                        density=0.4, theme="b", opening=(0.78, 0.75)),
            SectionPlan("A'", ("A3", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.5, theme="a", opening=(0.75, 0.8)),
            SectionPlan("B'", ("B1", "B2"), drums="off", keys="rock", bass="hold", pad=True, lead="felt",
                        density=0.35, theme="b", opening=(0.8, 0.7)),
            SectionPlan("Outro", ("O",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.7, 0.45)),
        ),
        notes=("Variant A (closest to brief, default pick): 66 BPM pure D major pentatonic lullaby for 07's CRT "
               "title screen. A warm low-passed pad drifts in a slow four-bar lunar orbit over a held root pedal; "
               "felt piano plays cradle-like broken chords while celesta and high felt piano trade sparse, tender "
               "phrases through C2 'Radio bench' tape warmth at -20 LUFS with zero drums and no static."),
    ),
    TrackSpec(
        number=11, slug="title_b_earthlight_cradle", title="Earthlight Cradle", seed=5202, bpm=70.0, swing=0.53,
        key="D major", family="I-iii-vi-V-pent", drums="pulse", ending="loop", title_theme=True,
        target_lufs=TITLE_TARGET_LUFS, keys_voice="felt", themes=(("a", "jingle"),),
        mood=Mood(warmth_hz=9600.0, dust_db=-48.0, wow_cents=6.0, space=0.75, pump_db=0.0, substitution=0.3,
                  texture="old_radio", orbit_bars=4),
        form=(
            SectionPlan("Intro", ("A1",), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.35, theme="a", opening=(0.5, 0.78)),
            SectionPlan("A", ("A1", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.65, theme="a", opening=(0.78, 0.85)),
            SectionPlan("B", ("B1", "B2"), drums="off", keys="arp", bass="hold", pad=True, lead="flute",
                        density=0.55, theme="b", opening=(0.85, 0.8)),
            SectionPlan("A'", ("A3", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.7, theme="a", opening=(0.8, 0.85)),
            SectionPlan("B'", ("B1", "B2"), drums="off", keys="arp", bass="hold", pad=True, lead="felt",
                        density=0.5, theme="b", opening=(0.85, 0.75)),
            SectionPlan("Outro", ("O",), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.3, theme="a", opening=(0.75, 0.5)),
        ),
        notes=("Variant B (more melodic): 70 BPM pure D major pentatonic theme. Celesta opens by singing a gentle "
               "variation of Lumen Station's D-E-A pentatonic motif over felt-piano broken chords and a warm pad, "
               "answered in the B sections by soft breathy flute and high felt piano, mastered through Old Radio "
               "tape warmth to -20 LUFS with no drums."),
    ),
    TrackSpec(
        number=12, slug="title_c_quiet_horizon", title="Quiet Horizon", seed=5303, bpm=60.0, swing=0.51,
        key="D major", family="I-vi-pent-drone", drums="pulse", ending="loop", title_theme=True,
        target_lufs=TITLE_TARGET_LUFS, keys_voice="felt",
        mood=Mood(warmth_hz=8200.0, dust_db=-50.0, wow_cents=7.5, space=0.92, pump_db=0.0, substitution=0.2,
                  texture="old_radio", orbit_bars=6),
        form=(
            SectionPlan("Horizon", ("A1",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.4, 0.65)),
            SectionPlan("A", ("A1", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="theremin",
                        density=0.25, theme="a", opening=(0.65, 0.7)),
            SectionPlan("B", ("B1", "B2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.3, theme="b", opening=(0.7, 0.68)),
            SectionPlan("Drift", ("A3",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.68, 0.65)),
            SectionPlan("A'", ("A1", "A2"), drums="off", keys="rock", bass="hold", pad=True, lead="celesta",
                        density=0.28, theme="a", opening=(0.65, 0.6)),
            SectionPlan("Outro", ("O",), drums="off", keys="rock", bass="hold", pad=True, opening=(0.6, 0.4)),
        ),
        notes=("Variant C (more minimal/ambient): 60 BPM spacious lunar horizon bed in pure D major pentatonic. "
               "Two-bar harmonic breaths on a wide six-bar orbiting pad and deep pedal bass, hushed felt piano, and "
               "sparse, distant theremin and celesta phrases separated by long silences, mastered at -20 LUFS."),
    ),
)
