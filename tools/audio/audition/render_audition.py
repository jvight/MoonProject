#!/usr/bin/env python3
"""Render the AUDIO-AUDITION Round 2 comparison sets (C "Old Radio" reference + C1 "Warm receiver",
C2 "Radio bench", and C3 "Lullaby radio") into ``Logs/audition2/<C|C1|C2|C3>/<cue>.wav`` and
``Logs/audition2/index.json``.

Also supports ``--round 1`` to render the Round 1 sets (``A``, ``B``, ``C``, ``current``).
Verifies that every rendered WAV passes the project's audio quality gates, peak <= -3.0 dBFS, and size limits
(< 200 KB per file, < 8 MB total).

Usage:
  python tools/audio/audition/render_audition.py
  python tools/audio/audition/render_audition.py --out-dir Logs/audition2
"""
import argparse
import json
import shutil
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np

AUDIO_TOOLS_DIR = Path(__file__).resolve().parents[1]
REPO_ROOT = AUDIO_TOOLS_DIR.parents[1]
if str(AUDIO_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(AUDIO_TOOLS_DIR))

import analyze_audio  # noqa: E402
import build_sfx  # noqa: E402
from audition import lullaby_radio, old_radio, radio_bench, warm_receiver, wood_felt, workshop  # noqa: E402
from cues import CATEGORIES, CUES  # noqa: E402
from synth.analysis import loudness_momentary_max, peak_db, true_peak_db  # noqa: E402
from synth.core import SAMPLE_RATE, db_to_gain, rng, samples, to_mono  # noqa: E402
from synth.effects import soft_limit  # noqa: E402
from synth.envelope import fade_in, fade_out  # noqa: E402
from synth.filters import dc_block  # noqa: E402
from synth.io import write_wav  # noqa: E402

ROUND1_OUT_DIR = REPO_ROOT / "Logs" / "audition"
DEFAULT_OUT_DIR = REPO_ROOT / "Logs" / "audition2"
MAX_FILE_BYTES = 200 * 1024
MAX_TOTAL_BYTES = 8 * 1024 * 1024
MAX_CURRENT_DURATION_S = 1.85
MAX_AUDITION_PEAK_DB = -3.0
PEAK_TARGET_CEILING_DB = -3.20
TRUE_PEAK_TARGET_CEILING_DB = -3.05

_EXISTING_CUES = {c.id: c for c in CUES}

DIRECTIONS = {
    "C": {
        "name": "Old Radio (Round 1 Reference)",
        "summary": (
            "Round-1 C reference: warm tape/valve receiver colour (band-limited ~185 Hz..2.85 kHz, asymmetric triode "
            "saturation, envelope-gated AM/tape hiss, subtle capstan wow & flutter), soft magnetic reed-relay clicks "
            "for UI switches, and 07's chirps as a heterodyne valve-radio voice warbling in D major pentatonic."
        ),
    },
    "C1": {
        "name": "Warm receiver",
        "summary": (
            "C with much less hiss (~14 dB quieter) and gentler triode saturation, a fuller low-mid body (high-pass "
            "opened down to ~118 Hz with 220 Hz low-shelf and wooden console chest formants at 165/310 Hz plus "
            "cathode sub-octave warmth), smooth 6 dB/oct top roll-off, slower raised-cosine attacks (18..35 ms), "
            "and longer gentle tails in a cozy small room."
        ),
    },
    "C2": {
        "name": "Radio bench",
        "summary": (
            "C's vintage radio colour on top of physical bench mechanisms for UI (knurled rotary knob detents, "
            "spring-loaded bakelite toggle switch throws, enclosed armature relay clacks, and paper speaker-cone "
            "thumps); 07's chirps use a rounded glottal-like valve wave shaped by a dual swept vocal-formant filter "
            "bank (F1 throat + F2 cooing vowel contour) for a wordless little radio creature."
        ),
    },
    "C3": {
        "name": "Lullaby radio",
        "summary": (
            "The calmest and most musical of the three: short D-major-pentatonic celesta and clockwork music-box "
            "phrases and chordal grace motifs heard through a warm, near-noiseless vintage tube radio (~22 dB less "
            "hiss than C, zero switch clatter) with gentle tape wow and a calm chamber reverb."
        ),
    },
}

ROUND1_DIRECTIONS = {
    "A": {
        "name": "Workshop",
        "summary": (
            "Physical, mechanical: soft spring-latch clicks, felt-damped miniature servo whirrs, and small resonant "
            "metal (spun brass washers, hollow aluminium tubes, plucked phosphor-bronze comb tines) tuned to D major "
            "pentatonic in a close workshop room; 07's chirps are a breathing music-box/servo mechanism with zero "
            "pure oscillators."
        ),
    },
    "B": {
        "name": "Wood & Felt",
        "summary": (
            "Warm acoustic: muted rosewood marimba bars struck with soft wool-yarn mallets, thumb-pad box kalimba "
            "with cedar gourd resonance, hollow maple wood taps, and cotton-paper/wool-felt friction for UI moves; "
            "07's chirps are soft breathy ocarina (vessel flute) phrases with air chiff and a woody body."
        ),
    },
    "C": DIRECTIONS["C"],
    "current": {
        "name": "Current (0.5.5 Baseline)",
        "summary": (
            "Current game audio exported from tools/audio/{cues,feel_cues}.py for side-by-side comparison."
        ),
    },
}


@dataclass(frozen=True)
class AuditionCue:
    id: str
    group: str
    moment: str
    current_cue_id: str
    category: str
    fade_out_s: float
    tonal: bool
    renders: dict
    descriptions: dict

    @property
    def target_lufs(self) -> float:
        return CATEGORIES[self.category].target_lufs


AUDITION_CUES = (
    AuditionCue(
        id="ticker_line",
        group="UI",
        moment="UI: ticker line arrives",
        current_cue_id="page_tick",
        category="tick_2d",
        fade_out_s=0.025,
        tonal=True,
        renders={
            "A": workshop.ticker_line,
            "B": wood_felt.ticker_line,
            "C": old_radio.ticker_line,
            "C1": warm_receiver.ticker_line,
            "C2": radio_bench.ticker_line,
            "C3": lullaby_radio.ticker_line,
        },
        descriptions={
            "current": "Existing page_tick: bandpass white-noise burst + 2-sine wood_tick at A5 (880 Hz).",
            "A": "Felt-damped micro-escapement pawl click nudging a small spun brass washer on A4.",
            "B": "Whisper-quiet cotton-paper slip ending in a muted maple wood tap and marimba touch on A4.",
            "C": "Soft magnetic reed-relay contact tick with a warm band-limited A4 capacitor pip over faint tape hiss.",
            "C1": "Cushioned mercury-switch pulse into a slow-attack A4/A3 warm console tone with a gentle small-room tail.",
            "C2": "Physical rotary knob detent + soft relay clack nudging a damped A4 tuning-plate ring and speaker-cone pulse.",
            "C3": "Delicate music-box tine & celesta touch on A4 with a faint D5 fifth shimmer through a quiet tube radio.",
        },
    ),
    AuditionCue(
        id="place_card",
        group="UI",
        moment="UI: place card shows",
        current_cue_id="ui_card",
        category="ui_2d",
        fade_out_s=0.14,
        tonal=True,
        renders={
            "A": workshop.place_card,
            "B": wood_felt.place_card,
            "C": old_radio.place_card,
            "C1": warm_receiver.place_card,
            "C2": radio_bench.place_card,
            "C3": lullaby_radio.place_card,
        },
        descriptions={
            "current": "Existing ui_card: 3-sine music_box phrase A5 F#5 A5 -> D6 over D5 (high octave, no pluck transient).",
            "A": "Brass nameplate sliding on felt + 4-note plucked phosphor-bronze comb phrase (A4 F#4 A4 -> D5 over D4).",
            "B": "Heavy cotton card unfolding on wool felt + rosewood marimba & thumb-kalimba phrase (A4 F#4 A4 -> D5 over D3).",
            "C": "Rotary relay step + vintage 4-note vacuum-tube station-ident chime (A4 F#4 A4 -> D5 over D3) with tape flutter.",
            "C1": "Unhurried 4-note warm console receiver chime (A4 F#4 A4 -> D5) over a deep D3/A3 low-mid chest bloom in a small room.",
            "C2": "4-step rotary band-selector knob detents + bakelite switch + speaker-cone thumps driving A4 F#4 A4 -> D5 over D3.",
            "C3": "Clockwork music-box & celesta lullaby phrase (A4 F#4 A4 -> D5) harmonized over a warm D4/A4/F#5 celesta radio chord.",
        },
    ),
    AuditionCue(
        id="tab_open",
        group="UI",
        moment="UI: Tab opens",
        current_cue_id="ui_confirm",
        category="ui_2d",
        fade_out_s=0.07,
        tonal=True,
        renders={
            "A": workshop.tab_open,
            "B": wood_felt.tab_open,
            "C": old_radio.tab_open,
            "C1": warm_receiver.tab_open,
            "C2": radio_bench.tab_open,
            "C3": lullaby_radio.tab_open,
        },
        descriptions={
            "current": "Existing ui_confirm: pure 3-harmonic additive synth two-note beep rising D5 -> A5.",
            "A": "Spring-latch catch releasing + rising felt-damped servo whirr (D3 -> A3) + hollow aluminium tubes D4 -> A4.",
            "B": "Wool-felt tray sliding open with two warm rising rosewood marimba & kalimba notes (D4 -> A4).",
            "C": "Comms console toggle switch ON (double reed relay) + warm rising triode valve tones (D4 -> A4).",
            "C1": "Cushioned console switch + slow-blooming rising warm receiver tones (D4 -> A4) with D3/A3 sub-octave body.",
            "C2": "Heavy bakelite toggle switch thrown ON + outward speaker-cone thump + double relay clack rising D4 -> A4.",
            "C3": "Gentle 3-note rising music-box & celesta grace arpeggio (D4 -> F#4 -> A4 with D5 tine shimmer) through a quiet tube radio.",
        },
    ),
    AuditionCue(
        id="tab_close",
        group="UI",
        moment="UI: Tab closes",
        current_cue_id="ui_back",
        category="ui_2d",
        fade_out_s=0.07,
        tonal=True,
        renders={
            "A": workshop.tab_close,
            "B": wood_felt.tab_close,
            "C": old_radio.tab_close,
            "C1": warm_receiver.tab_close,
            "C2": radio_bench.tab_close,
            "C3": lullaby_radio.tab_close,
        },
        descriptions={
            "current": "Existing ui_back: pure 3-harmonic additive synth two-note beep falling A4 -> D4.",
            "A": "Felt-damped servo easing down (A3 -> D3) + aluminium tubes A4 -> D4 + seated spring-latch click.",
            "B": "Wool-felt tray sliding shut + settling rosewood marimba notes (A4 -> D4) + soft wooden rest on D3.",
            "C": "Falling valve tones (A4 -> D4) sagging gently as the capacitor discharges + soft reed-relay release.",
            "C1": "Soft-attack falling warm receiver tones (A4 -> D4) settling into a deep D3 console chest tail in a small room.",
            "C2": "Rotary detent + bakelite toggle switch thrown OFF + relay release clack & speaker-cone thump settling A4 -> D4.",
            "C3": "3-note descending celesta & music-box lullaby cadence (A4 -> F#4 -> D4) coming to rest on a warm D4/A4 fifth cradle.",
        },
    ),
    AuditionCue(
        id="item_pick",
        group="UI",
        moment="UI: item picked",
        current_cue_id="part_pick",
        category="tick_2d",
        fade_out_s=0.03,
        tonal=True,
        renders={
            "A": workshop.item_pick,
            "B": wood_felt.item_pick,
            "C": old_radio.item_pick,
            "C1": warm_receiver.item_pick,
            "C2": radio_bench.item_pick,
            "C3": lullaby_radio.item_pick,
        },
        descriptions={
            "current": "Existing part_pick: two high 40 ms 2-sine wood_ticks rising E5 -> A5.",
            "A": "Spring-clip release tick + rising two-step spun brass washer ring (E4 -> A4).",
            "B": "Light wool-cloth lift + hollow maple token knock rising E4 -> A4 with a thumb-kalimba touch.",
            "C": "Bakelite patch-plug click + short rising two-tone valve blip (E4 -> A4) through the radio passband.",
            "C1": "Soft console contact + rounded, slow-attack rising receiver tones (E4 -> A4) with warm E3/A3 low-mid body.",
            "C2": "Knurled rotary knob detent click + relay clack + crisp speaker-cone pulse rising E4 -> A4.",
            "C3": "Delicate two-note music-box tine & celesta lift (E4 -> A4 with E5 shimmer) heard through the warm radio with zero switch click.",
        },
    ),
    AuditionCue(
        id="item_drop",
        group="UI",
        moment="UI: item dropped",
        current_cue_id="part_drop",
        category="tick_2d",
        fade_out_s=0.035,
        tonal=True,
        renders={
            "A": workshop.item_drop,
            "B": wood_felt.item_drop,
            "C": old_radio.item_drop,
            "C1": warm_receiver.item_drop,
            "C2": radio_bench.item_drop,
            "C3": lullaby_radio.item_drop,
        },
        descriptions={
            "current": "Existing part_drop: low sine drop + 2-sine wood_tick at D5 (587 Hz).",
            "A": "Padded socket clunk + seated brass washer ring on D4 + locking detent click over an A3 tube body.",
            "B": "Cushioned wool-felt thud + seated hollow rosewood knock and muted marimba bar on D4.",
            "C": "Patch-bay plug seating with a warm transformer thump + grounded D4/D3 valve tone settle.",
            "C1": "Deep cushioned console settle + full-bodied D4 & D3 warm receiver bloom (~120 Hz foundation) in a small room.",
            "C2": "Seated bakelite switch throw + double relay clack + heavy paper speaker-cone thump locking onto D4/D3.",
            "C3": "Gentle two-note celesta & music-box cadence (A4 -> D4 with D5 harmonic chime) settling softly into place.",
        },
    ),
    AuditionCue(
        id="item_refuse",
        group="UI",
        moment="UI: refused",
        current_cue_id="part_refuse",
        category="tick_2d",
        fade_out_s=0.035,
        tonal=True,
        renders={
            "A": workshop.item_refuse,
            "B": wood_felt.item_refuse,
            "C": old_radio.item_refuse,
            "C1": warm_receiver.item_refuse,
            "C2": radio_bench.item_refuse,
            "C3": lullaby_radio.item_refuse,
        },
        descriptions={
            "current": "Existing part_refuse: two 2-sine wood_ticks falling B4 -> F#4.",
            "A": "Two felt-choked, muted bracket taps falling B4 -> F#4 (damped metal modes that cannot ring).",
            "B": "Two palm-muted hollow walnut taps falling B4 -> F#4 with a short wool-felt brush.",
            "C": "Two muted reed-relay drops falling B4 -> F#4 through a darkened receiver filter.",
            "C1": "Two soft-attack, dark-filtered warm receiver tones falling B4 -> F#4 with gentle low-mid body.",
            "C2": "Two blocked bakelite-switch & relay clacks hitting a mechanical stop (B4 -> F#4) with a muted cone thud.",
            "C3": "Soft, felt-muted celesta & music-box two-note sigh falling B4 -> F#4 (calm, musical, and never scolding).",
        },
    ),
    AuditionCue(
        id="prompt_chip",
        group="UI",
        moment="UI: prompt chip",
        current_cue_id="ui_prompt",
        category="ui_2d",
        fade_out_s=0.06,
        tonal=True,
        renders={
            "A": workshop.prompt_chip,
            "B": wood_felt.prompt_chip,
            "C": old_radio.prompt_chip,
            "C1": warm_receiver.prompt_chip,
            "C2": radio_bench.prompt_chip,
            "C3": lullaby_radio.prompt_chip,
        },
        descriptions={
            "current": "Existing ui_prompt: high E5 -> A5 felt_piano grace note.",
            "A": "Small mechanical indicator flag flip + soft E4 -> A4 hollow aluminium tube chime.",
            "B": "Delicate cotton-paper rustle + thumb-pad box kalimba grace note rising E4 -> A4.",
            "C": "Soft reed-relay contact + warm E4 -> A4 heterodyne valve whistle.",
            "C1": "Whisper-soft switch + slow-blooming E4 -> A4 warm console tone with rich low-mid chest and room tail.",
            "C2": "Soft relay clack + rotary knob detent tick triggering a warm E4 -> A4 speaker-cone & bench-coil chime.",
            "C3": "Crystalline yet warm 3-note music-box & celesta grace motif (E4 -> F#4 -> A4) through the old radio.",
        },
    ),
    AuditionCue(
        id="chirp_curious",
        group="07",
        moment="07: content / Curious",
        current_cue_id="chirp_curious",
        category="oneshot_3d",
        fade_out_s=0.06,
        tonal=True,
        renders={
            "A": workshop.chirp_curious,
            "B": wood_felt.chirp_curious,
            "C": old_radio.chirp_curious,
            "C1": warm_receiver.chirp_curious,
            "C2": radio_bench.chirp_curious,
            "C3": lullaby_radio.chirp_curious,
        },
        descriptions={
            "current": "Existing chirp_curious: 0.7*sine + 0.18*square gliding D5 -> F#5 -> A5 (+2 st bend) with 6.5 Hz LFO.",
            "A": "Tiny felt-damped servo governor breathing under 3 plucked music-box comb & brass tines (D4 -> F#4 -> A4).",
            "B": "Soft breathy wooden ocarina 3-note question (D4 -> F#4 -> A4) with gentle air chiff and upward inflection.",
            "C": "Little heterodyne valve-radio voice warbling D4 -> F#4 -> A4 through the warm receiver passband.",
            "C1": "Creamy, slow-attack legato valve voice (D4 -> F#4 -> A4) with full low-mid chest (~120 Hz) in a small room.",
            "C2": "Rounder, formant-voiced radio creature cooing D4 -> F#4 -> A4 (swept F1/F2 vowel contour) framed by PTT relay clacks.",
            "C3": "3-note clockwork music-box & celesta phrase (D4 -> F#4 -> A4 with B4 grace tine) over a soft F#4/D5 celesta halo.",
        },
    ),
    AuditionCue(
        id="chirp_affirm",
        group="07",
        moment="07: Affirm",
        current_cue_id="chirp_affirm",
        category="oneshot_3d",
        fade_out_s=0.05,
        tonal=True,
        renders={
            "A": workshop.chirp_affirm,
            "B": wood_felt.chirp_affirm,
            "C": old_radio.chirp_affirm,
            "C1": warm_receiver.chirp_affirm,
            "C2": radio_bench.chirp_affirm,
            "C3": lullaby_radio.chirp_affirm,
        },
        descriptions={
            "current": "Existing chirp_affirm: 0.7*sine + 0.18*square two-note beep A5 -> D6.",
            "A": "Crisp 2-syllable mechanical nod: servo breath + plucked comb & brass washer A4 -> D5.",
            "B": "Gentle 2-note breathy ocarina nod (A4 -> D5) with a soft wooden finger-pad tap.",
            "C": "Keyed 2-note valve-radio 'roger' (A4 -> D5) framed by soft reed-relay clicks.",
            "C1": "Soft-attack 2-note warm receiver nod (A4 -> D5) with cathode sub-octave body and a gentle room tail.",
            "C2": "Wordless 2-syllable vocal-formant valve nod ('mhm -> ooh', A4 -> D5) with physical relay clack and cone thump.",
            "C3": "Warm 2-note celesta & music-box chime nod (A4 -> D5) harmonized with a soft F#4/A4 celesta chord.",
        },
    ),
    AuditionCue(
        id="chirp_proud",
        group="07",
        moment="07: Proud",
        current_cue_id="chirp_proud",
        category="oneshot_3d",
        fade_out_s=0.08,
        tonal=True,
        renders={
            "A": workshop.chirp_proud,
            "B": wood_felt.chirp_proud,
            "C": old_radio.chirp_proud,
            "C1": warm_receiver.chirp_proud,
            "C2": radio_bench.chirp_proud,
            "C3": lullaby_radio.chirp_proud,
        },
        descriptions={
            "current": "Existing chirp_proud: 0.7*sine + 0.18*square 5-note climb D5 E5 F#5 A5 D6.",
            "A": "5-note climbing music-box comb & servo flourish (D4 E4 F#4 A4 D5) with sympathetic D4 aluminium tube ring.",
            "B": "Warm 5-note wooden ocarina melody (D4 E4 F#4 A4 D5) blooming into a thumb-kalimba D4 resonance.",
            "C": "5-note vintage station-ident valve fanfare (D4 E4 F#4 A4 D5) glowing through warm saturated tubes.",
            "C1": "Unhurried 5-note legato warm receiver melody (D4 E4 F#4 A4 D5) with rich low-mid console chest and room bloom.",
            "C2": "Happy 5-syllable formant-voiced creature warble (D4 E4 F#4 A4 D5) opening to a warm vowel glow with relay clacks.",
            "C3": "5-note music-box & celesta lullaby melody (D4 E4 F#4 A4 D5) blossoming into a warm D4+A4+F#5 celesta radio chord.",
        },
    ),
    AuditionCue(
        id="chirp_worried",
        group="07",
        moment="07: low battery (worried)",
        current_cue_id="chirp_worried",
        category="oneshot_3d",
        fade_out_s=0.06,
        tonal=True,
        renders={
            "A": workshop.chirp_worried,
            "B": wood_felt.chirp_worried,
            "C": old_radio.chirp_worried,
            "C1": warm_receiver.chirp_worried,
            "C2": radio_bench.chirp_worried,
            "C3": lullaby_radio.chirp_worried,
        },
        descriptions={
            "current": "Existing chirp_worried: 0.7*sine + 0.18*square falling B5 -> F#5 with 35-cent 6.5 Hz vibrato.",
            "A": "Drooping 2-note mechanism (B4 -> F#4) with fluttering governor tremolo and winding-down servo whirr.",
            "B": "Sighing 2-note wooden ocarina (B4 -> F#4) with extra breath turbulence and gentle tremor.",
            "C": "Sagging-filament valve voice (B4 -> F#4) where tube warble deepens and the receiver passband darkens.",
            "C1": "Softly drooping warm receiver sigh (B4 -> F#4) with gentle low-pass darkening and deep low-mid console warmth.",
            "C2": "Drooping closed-vowel creature murmur (B4 -> F#4, F1/F2 closing down) with gentle vocal tremor and relay release.",
            "C3": "Tender, slow 2-note music-box & celesta lullaby sigh (B4 -> F#4 over soft D4/B4) with gentle tape wow.",
        },
    ),
    AuditionCue(
        id="chirp_relieved",
        group="07",
        moment="07: full battery (relieved)",
        current_cue_id="chirp_relieved",
        category="oneshot_3d",
        fade_out_s=0.07,
        tonal=True,
        renders={
            "A": workshop.chirp_relieved,
            "B": wood_felt.chirp_relieved,
            "C": old_radio.chirp_relieved,
            "C1": warm_receiver.chirp_relieved,
            "C2": radio_bench.chirp_relieved,
            "C3": lullaby_radio.chirp_relieved,
        },
        descriptions={
            "current": "Existing chirp_relieved: 0.7*sine + 0.18*square 3-note contour F#5 -> D5 -> A5.",
            "A": "Settling 3-note servo & resonant metal sigh (F#4 -> D4 -> A4) coming to a steady rest.",
            "B": "Warm 3-note ocarina exhale (F#4 -> D4 -> A4) settling over a low D3 rosewood marimba cushion.",
            "C": "3-note valve voice (F#4 -> D4 -> A4) stabilizing from a gentle warble into a warm steady carrier.",
            "C1": "Peaceful 3-note warm receiver exhale (F#4 -> D4 -> A4) settling into a long, steady low-mid room glow.",
            "C2": "Warm 3-syllable vocal-formant sigh ('ahh -> ooh -> mmm', F#4 -> D4 -> A4) settling over a soft cone thump.",
            "C3": "Serene 3-note celesta & music-box lullaby resolution (F#4 -> D4 -> A4) resting over a warm D4+F#4 celesta pad.",
        },
    ),
    AuditionCue(
        id="scan_complete",
        group="07",
        moment="07: a scan completes",
        current_cue_id="scan_liftoff",
        category="oneshot_3d",
        fade_out_s=0.10,
        tonal=True,
        renders={
            "A": workshop.scan_complete,
            "B": wood_felt.scan_complete,
            "C": old_radio.scan_complete,
            "C1": warm_receiver.scan_complete,
            "C2": radio_bench.scan_complete,
            "C3": lullaby_radio.scan_complete,
        },
        descriptions={
            "current": "Existing scan_liftoff: 1.0 s pure sine sweep D5 -> D6 under high glass_chime notes A5 D6 F#6.",
            "A": "Mechanical lens-iris blades clicking shut + rising servo governor into a warm A4 -> D5 -> F#5 tube/brass chime.",
            "B": "Brushed wool-felt sweep across hollow wooden bars blooming into an A4 -> D5 -> F#5 marimba, kalimba & ocarina resolve.",
            "C": "AM tuning-dial band sweep + heterodyne glide locking onto a warm 3-note valve chord (A4 -> D5 -> F#5).",
            "C1": "Gentle low-mid tuning air swell + slow-blooming 3-note warm receiver chord (A4 -> D5 -> F#5 over D3/D4) in a small room.",
            "C2": "Stepping rotary knob detents across the dial + relay clack + speaker-cone thumps locking onto 07's formant-voiced chord.",
            "C3": "Delicate ascending pentatonic music-box glissando (D4 E4 F#4) blossoming into a glowing A4 -> D5 -> F#5 celesta chord.",
        },
    ),
)


def finish_audition_signal(signal: np.ndarray, target_lufs: float, fade_out_s: float) -> np.ndarray:
    """Finishes a mono audition signal: DC removal, click-free edges, exact momentary LUFS normalization, and
    transparent crest-factor control so sample peak <= -3.0 dBFS and true peak <= -3.0 dBTP."""
    x = np.asarray(to_mono(signal), dtype=np.float64)
    x = build_sfx._remove_dc_keep_edges(
        fade_out(fade_in(dc_block(x, build_sfx.DC_BLOCK_HZ), build_sfx.ONESHOT_FADE_IN_S), fade_out_s)
    )

    for _ in range(6):
        measured = loudness_momentary_max(x, analyze_audio.MOMENTARY_WINDOW_S)
        x = x * db_to_gain(target_lufs - measured)
        cur_peak = peak_db(x)
        cur_tp = true_peak_db(x)
        if cur_peak <= PEAK_TARGET_CEILING_DB and cur_tp <= TRUE_PEAK_TARGET_CEILING_DB:
            break
        excess = max(cur_peak - PEAK_TARGET_CEILING_DB, cur_tp - TRUE_PEAK_TARGET_CEILING_DB, 0.0)
        ceiling = max(-5.0, PEAK_TARGET_CEILING_DB - 0.45 * excess)
        x = soft_limit(x, ceiling)
        x = build_sfx._remove_dc_keep_edges(fade_out(fade_in(x, build_sfx.ONESHOT_FADE_IN_S), fade_out_s))

    return x


def render_current_cue(audition_cue: AuditionCue) -> np.ndarray:
    """Exports the current game version of ``audition_cue`` from ``cues.py`` / ``build_sfx.py`` in mono."""
    cue = _EXISTING_CUES[audition_cue.current_cue_id]
    raw = cue.render(0, rng(cue.id, 0))
    mono = to_mono(raw)
    max_samples = samples(MAX_CURRENT_DURATION_S)
    if mono.shape[0] > max_samples:
        mono = mono[:max_samples]
    finished, _, _ = build_sfx.finish(cue, mono)
    return finished


def render_all(out_dir: Path = DEFAULT_OUT_DIR, round_num: int = 2) -> dict:
    """Renders all 4 sets for the selected round (Round 2: ``C, C1, C2, C3``; Round 1: ``A, B, C, current``)
    into ``out_dir`` and writes ``index.json``."""
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    active_dirs = ("C", "C1", "C2", "C3") if round_num == 2 else ("A", "B", "C", "current")
    dir_meta = DIRECTIONS if round_num == 2 else ROUND1_DIRECTIONS
    metrics_by_dir = {d: {} for d in active_dirs}
    total_bytes = 0

    for ac in AUDITION_CUES:
        for direction in active_dirs:
            folder = out_dir / direction
            folder.mkdir(parents=True, exist_ok=True)
            wav_path = folder / f"{ac.id}.wav"

            round1_c_src = ROUND1_OUT_DIR / "C" / f"{ac.id}.wav"
            if round_num == 2 and direction == "C" and round1_c_src.exists():
                shutil.copyfile(round1_c_src, wav_path)
            elif direction == "current":
                sig = render_current_cue(ac)
                write_wav(wav_path, sig, bits=16, sample_rate=SAMPLE_RATE)
            else:
                gen = rng(f"audition_{direction}_{ac.id}", 0)
                raw = ac.renders[direction](gen)
                sig = finish_audition_signal(raw, ac.target_lufs, ac.fade_out_s)
                write_wav(wav_path, sig, bits=16, sample_rate=SAMPLE_RATE)

            size_bytes = wav_path.stat().st_size
            total_bytes += size_bytes

            enforce_tonal = _EXISTING_CUES[ac.current_cue_id].tonal if direction == "current" else ac.tonal
            m = analyze_audio.analyze_file(wav_path, loop=False, hf_cutoff=8000.0, tonal=enforce_tonal)
            if not enforce_tonal:
                data, _ = analyze_audio.read_audio(wav_path)
                m["pitch"], m["pitch_cents"], m["in_key"] = analyze_audio.dominant_pitch(data)
            m["size_bytes"] = size_bytes
            m["target_lufs"] = ac.target_lufs
            m["failures"] = analyze_audio.evaluate(
                m, hf_max_db=-30.0, target_lufs=ac.target_lufs, loudness_mode="momentary"
            )
            if round_num == 2 and m["peak_db"] > MAX_AUDITION_PEAK_DB:
                m["failures"].append(f"peak {m['peak_db']:.2f} dBFS > {MAX_AUDITION_PEAK_DB}")
            if size_bytes >= MAX_FILE_BYTES:
                m["failures"].append(f"file size {size_bytes} B >= {MAX_FILE_BYTES} B")
            metrics_by_dir[direction][ac.id] = m

    index_payload = {
        "task": "AUDIO-AUDITION",
        "round": round_num,
        "sample_rate_hz": SAMPLE_RATE,
        "bits_per_sample": 16,
        "channels": 1,
        "scale": "D major pentatonic (D E F# A B)",
        "directions": dir_meta,
        "cues": [
            {
                "cue": ac.id,
                "group": ac.group,
                "moment": ac.moment,
                "current_cue_id": ac.current_cue_id,
                "category": ac.category,
                "target_lufs": ac.target_lufs,
                "files": {d: f"{d}/{ac.id}.wav" for d in active_dirs},
                **{d: ac.descriptions[d] for d in active_dirs},
            }
            for ac in AUDITION_CUES
        ],
    }
    index_path = out_dir / "index.json"
    index_path.write_text(json.dumps(index_payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    return {
        "out_dir": str(out_dir),
        "index_path": str(index_path),
        "total_bytes": total_bytes,
        "metrics": metrics_by_dir,
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Output folder (default: Logs/audition2)")
    ap.add_argument("--round", type=int, choices=(1, 2), default=2, help="Audition round to render (default: 2)")
    args = ap.parse_args()

    summary = render_all(args.out_dir, round_num=args.round)
    active_dirs = ("C", "C1", "C2", "C3") if args.round == 2 else ("current", "A", "B", "C")
    dir_meta = DIRECTIONS if args.round == 2 else ROUND1_DIRECTIONS

    failed = 0
    for direction in active_dirs:
        print(f"\n=== Variant: {direction} ({dir_meta[direction]['name']}) ===")
        print(analyze_audio.HEADER)
        for ac in AUDITION_CUES:
            m = summary["metrics"][direction][ac.id]
            fails = m["failures"]
            failed += bool(fails)
            print(analyze_audio.format_row(m, fails))

    total_kb = summary["total_bytes"] / 1024.0
    print(f"\nTotal size across all 56 WAV files: {total_kb:.1f} KB ({summary['total_bytes']} bytes)")
    if summary["total_bytes"] >= MAX_TOTAL_BYTES:
        print(f"FAIL: total size {summary['total_bytes']} B >= {MAX_TOTAL_BYTES} B")
        failed += 1
    print("AUDITION BUILD: " + ("PASS" if failed == 0 else f"FAIL ({failed} problem(s))"))
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
