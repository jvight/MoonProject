#!/usr/bin/env python3
"""Render the AUDIO-AUDITION comparison sets (A "Workshop", B "Wood & Felt", C "Old Radio", plus "current").

Writes 48 kHz, 16-bit, mono WAV files into ``Logs/audition/<A|B|C|current>/<cue>.wav`` and the descriptive manifest
``Logs/audition/index.json``. Also verifies that every rendered WAV passes the project's audio quality gates and
size limits (< 200 KB per file, < 8 MB total).

Usage:
  python tools/audio/audition/render_audition.py
  python tools/audio/audition/render_audition.py --out-dir Logs/audition
"""
import argparse
import json
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
from audition import old_radio, wood_felt, workshop  # noqa: E402
from cues import CATEGORIES, CUES  # noqa: E402
from synth.analysis import loudness_momentary_max, peak_db, true_peak_db  # noqa: E402
from synth.core import SAMPLE_RATE, db_to_gain, rng, samples, to_mono  # noqa: E402
from synth.effects import soft_limit  # noqa: E402
from synth.envelope import fade_in, fade_out  # noqa: E402
from synth.filters import dc_block  # noqa: E402
from synth.io import write_wav  # noqa: E402

DEFAULT_OUT_DIR = REPO_ROOT / "Logs" / "audition"
MAX_FILE_BYTES = 200 * 1024
MAX_TOTAL_BYTES = 8 * 1024 * 1024
MAX_CURRENT_DURATION_S = 1.85
PEAK_TARGET_CEILING_DB = -1.35
TRUE_PEAK_TARGET_CEILING_DB = -0.65

_EXISTING_CUES = {c.id: c for c in CUES}

DIRECTIONS = {
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
    "C": {
        "name": "Old Radio",
        "summary": (
            "The crew's world: warm tape/valve receiver colour (band-limited ~190 Hz..2.85 kHz, asymmetric triode "
            "saturation, faint envelope-gated AM/tape hiss, subtle capstan wow & flutter), soft magnetic reed-relay "
            "clicks for UI switches, and 07's chirps as a warm heterodyne valve-radio voice warbling in D major "
            "pentatonic."
        ),
    },
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
        },
        descriptions={
            "current": "Existing page_tick: bandpass white-noise burst + 2-sine wood_tick at A5 (880 Hz).",
            "A": "Felt-damped micro-escapement pawl click nudging a small spun brass washer on A4.",
            "B": "Whisper-quiet cotton-paper slip ending in a muted maple wood tap and marimba touch on A4.",
            "C": "Soft magnetic reed-relay contact tick with a warm band-limited A4 capacitor pip over faint tape hiss.",
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
        },
        descriptions={
            "current": "Existing ui_card: 3-sine music_box phrase A5 F#5 A5 -> D6 over D5 (high octave, no pluck transient).",
            "A": "Brass nameplate sliding on felt + 4-note plucked phosphor-bronze comb phrase (A4 F#4 A4 -> D5 over D4).",
            "B": "Heavy cotton card unfolding on wool felt + rosewood marimba & thumb-kalimba phrase (A4 F#4 A4 -> D5 over D3).",
            "C": "Rotary relay step + vintage 4-note vacuum-tube station-ident chime (A4 F#4 A4 -> D5 over D3) with tape flutter.",
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
        },
        descriptions={
            "current": "Existing ui_confirm: pure 3-harmonic additive synth two-note beep rising D5 -> A5.",
            "A": "Spring-latch catch releasing + rising felt-damped servo whirr (D3 -> A3) + hollow aluminium tubes D4 -> A4.",
            "B": "Wool-felt tray sliding open with two warm rising rosewood marimba & kalimba notes (D4 -> A4).",
            "C": "Comms console toggle switch ON (double reed relay) + warm rising triode valve tones (D4 -> A4).",
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
        },
        descriptions={
            "current": "Existing ui_back: pure 3-harmonic additive synth two-note beep falling A4 -> D4.",
            "A": "Felt-damped servo easing down (A3 -> D3) + aluminium tubes A4 -> D4 + seated spring-latch click.",
            "B": "Wool-felt tray sliding shut + settling rosewood marimba notes (A4 -> D4) + soft wooden rest on D3.",
            "C": "Falling valve tones (A4 -> D4) sagging gently as the capacitor discharges + soft reed-relay release.",
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
        },
        descriptions={
            "current": "Existing part_pick: two high 40 ms 2-sine wood_ticks rising E5 -> A5.",
            "A": "Spring-clip release tick + rising two-step spun brass washer ring (E4 -> A4).",
            "B": "Light wool-cloth lift + hollow maple token knock rising E4 -> A4 with a thumb-kalimba touch.",
            "C": "Bakelite patch-plug click + short rising two-tone valve blip (E4 -> A4) through the radio passband.",
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
        },
        descriptions={
            "current": "Existing part_drop: low sine drop + 2-sine wood_tick at D5 (587 Hz).",
            "A": "Padded socket clunk + seated brass washer ring on D4 + locking detent click over an A3 tube body.",
            "B": "Cushioned wool-felt thud + seated hollow rosewood knock and muted marimba bar on D4.",
            "C": "Patch-bay plug seating with a warm transformer thump + grounded D4/D3 valve tone settle.",
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
        },
        descriptions={
            "current": "Existing part_refuse: two 2-sine wood_ticks falling B4 -> F#4.",
            "A": "Two felt-choked, muted bracket taps falling B4 -> F#4 (damped metal modes that cannot ring).",
            "B": "Two palm-muted hollow walnut taps falling B4 -> F#4 with a short wool-felt brush.",
            "C": "Two muted reed-relay drops falling B4 -> F#4 through a darkened receiver filter.",
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
        },
        descriptions={
            "current": "Existing ui_prompt: high E5 -> A5 felt_piano grace note.",
            "A": "Small mechanical indicator flag flip + soft E4 -> A4 hollow aluminium tube chime.",
            "B": "Delicate cotton-paper rustle + thumb-pad box kalimba grace note rising E4 -> A4.",
            "C": "Soft reed-relay contact + warm E4 -> A4 heterodyne valve whistle.",
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
        },
        descriptions={
            "current": "Existing chirp_curious: 0.7*sine + 0.18*square gliding D5 -> F#5 -> A5 (+2 st bend) with 6.5 Hz LFO.",
            "A": "Tiny felt-damped servo governor breathing under 3 plucked music-box comb & brass tines (D4 -> F#4 -> A4).",
            "B": "Soft breathy wooden ocarina 3-note question (D4 -> F#4 -> A4) with gentle air chiff and upward inflection.",
            "C": "Little heterodyne valve-radio voice warbling D4 -> F#4 -> A4 through the warm receiver passband.",
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
        },
        descriptions={
            "current": "Existing chirp_affirm: 0.7*sine + 0.18*square two-note beep A5 -> D6.",
            "A": "Crisp 2-syllable mechanical nod: servo breath + plucked comb & brass washer A4 -> D5.",
            "B": "Gentle 2-note breathy ocarina nod (A4 -> D5) with a soft wooden finger-pad tap.",
            "C": "Keyed 2-note valve-radio 'roger' (A4 -> D5) framed by soft reed-relay clicks.",
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
        },
        descriptions={
            "current": "Existing chirp_proud: 0.7*sine + 0.18*square 5-note climb D5 E5 F#5 A5 D6.",
            "A": "5-note climbing music-box comb & servo flourish (D4 E4 F#4 A4 D5) with sympathetic D4 aluminium tube ring.",
            "B": "Warm 5-note wooden ocarina melody (D4 E4 F#4 A4 D5) blooming into a thumb-kalimba D4 resonance.",
            "C": "5-note vintage station-ident valve fanfare (D4 E4 F#4 A4 D5) glowing through warm saturated tubes.",
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
        },
        descriptions={
            "current": "Existing chirp_worried: 0.7*sine + 0.18*square falling B5 -> F#5 with 35-cent 6.5 Hz vibrato.",
            "A": "Drooping 2-note mechanism (B4 -> F#4) with fluttering governor tremolo and winding-down servo whirr.",
            "B": "Sighing 2-note wooden ocarina (B4 -> F#4) with extra breath turbulence and gentle tremor.",
            "C": "Sagging-filament valve voice (B4 -> F#4) where tube warble deepens and the receiver passband darkens.",
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
        },
        descriptions={
            "current": "Existing chirp_relieved: 0.7*sine + 0.18*square 3-note contour F#5 -> D5 -> A5.",
            "A": "Settling 3-note servo & resonant metal sigh (F#4 -> D4 -> A4) coming to a steady rest.",
            "B": "Warm 3-note ocarina exhale (F#4 -> D4 -> A4) settling over a low D3 rosewood marimba cushion.",
            "C": "3-note valve voice (F#4 -> D4 -> A4) stabilizing from a gentle warble into a warm steady carrier.",
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
        },
        descriptions={
            "current": "Existing scan_liftoff: 1.0 s pure sine sweep D5 -> D6 under high glass_chime notes A5 D6 F#6.",
            "A": "Mechanical lens-iris blades clicking shut + rising servo governor into a warm A4 -> D5 -> F#5 tube/brass chime.",
            "B": "Brushed wool-felt sweep across hollow wooden bars blooming into an A4 -> D5 -> F#5 marimba, kalimba & ocarina resolve.",
            "C": "AM tuning-dial band sweep + heterodyne glide locking onto a warm 3-note valve chord (A4 -> D5 -> F#5).",
        },
    ),
)


def finish_audition_signal(signal: np.ndarray, target_lufs: float, fade_out_s: float) -> np.ndarray:
    """Finishes a mono audition signal: DC removal, click-free edges, exact momentary LUFS normalization, and
    transparent crest-factor control so sample peak <= -1.2 dBFS and true peak <= -0.5 dBTP."""
    x = np.asarray(to_mono(signal), dtype=np.float64)
    x = build_sfx._remove_dc_keep_edges(
        fade_out(fade_in(dc_block(x, build_sfx.DC_BLOCK_HZ), build_sfx.ONESHOT_FADE_IN_S), fade_out_s)
    )

    for _ in range(5):
        measured = loudness_momentary_max(x, analyze_audio.MOMENTARY_WINDOW_S)
        x = x * db_to_gain(target_lufs - measured)
        cur_peak = peak_db(x)
        cur_tp = true_peak_db(x)
        if cur_peak <= PEAK_TARGET_CEILING_DB and cur_tp <= TRUE_PEAK_TARGET_CEILING_DB:
            break
        # Gently round the highest transient peaks and re-enforce clean zero edges before re-normalizing.
        excess = max(cur_peak - PEAK_TARGET_CEILING_DB, cur_tp - TRUE_PEAK_TARGET_CEILING_DB, 0.0)
        ceiling = max(-3.5, PEAK_TARGET_CEILING_DB - 0.35 * excess)
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


def render_all(out_dir: Path = DEFAULT_OUT_DIR) -> dict:
    """Renders all 4 sets (``A``, ``B``, ``C``, ``current``) into ``out_dir`` and writes ``index.json``."""
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    metrics_by_dir = {"A": {}, "B": {}, "C": {}, "current": {}}
    total_bytes = 0

    for ac in AUDITION_CUES:
        for direction in ("A", "B", "C", "current"):
            folder = out_dir / direction
            folder.mkdir(parents=True, exist_ok=True)
            wav_path = folder / f"{ac.id}.wav"

            if direction == "current":
                sig = render_current_cue(ac)
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
            if size_bytes >= MAX_FILE_BYTES:
                m["failures"].append(f"file size {size_bytes} B >= {MAX_FILE_BYTES} B")
            metrics_by_dir[direction][ac.id] = m

    index_payload = {
        "task": "AUDIO-AUDITION",
        "sample_rate_hz": SAMPLE_RATE,
        "bits_per_sample": 16,
        "channels": 1,
        "scale": "D major pentatonic (D E F# A B)",
        "directions": DIRECTIONS,
        "cues": [
            {
                "cue": ac.id,
                "group": ac.group,
                "moment": ac.moment,
                "current_cue_id": ac.current_cue_id,
                "category": ac.category,
                "target_lufs": ac.target_lufs,
                "files": {
                    "A": f"A/{ac.id}.wav",
                    "B": f"B/{ac.id}.wav",
                    "C": f"C/{ac.id}.wav",
                    "current": f"current/{ac.id}.wav",
                },
                "A": ac.descriptions["A"],
                "B": ac.descriptions["B"],
                "C": ac.descriptions["C"],
                "current": ac.descriptions["current"],
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
    ap.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Output folder (default: Logs/audition)")
    args = ap.parse_args()

    summary = render_all(args.out_dir)
    failed = 0
    for direction in ("current", "A", "B", "C"):
        print(f"\n=== Direction: {direction} ({DIRECTIONS[direction]['name']}) ===")
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
