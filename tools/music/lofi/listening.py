"""
The listening report: what each track should sound like, checked by measurement because we cannot listen.

`listen` measures the musical intent of one decoded track (blind tempo, scale, loudness over time, spectral balance
per octave, stereo width of the music above the bass, and the seam of a loop); `markdown` turns every report into
tools/music/reports/tapes.md, a per-tape description plus a side-by-side table of the whole catalogue so the tapes'
differences from each other and from the radio's six tracks are visible at a glance.
"""
import math

import numpy as np
from synth import filters

from . import analysis
from .groove import style_named

WIDTH_ABOVE_HZ = 300.0


def width_db(x, above_hz=WIDTH_ABOVE_HZ):
    """Side-to-mid power ratio (dB) above `above_hz`: how wide the music is, ignoring the mono bass."""
    high = filters.butter(x, "highpass", above_hz, order=4)
    mid = 0.5 * (high[:, 0] + high[:, 1])
    side = 0.5 * (high[:, 0] - high[:, 1])
    return 10.0 * math.log10(max(float(np.mean(side ** 2)), 1e-30) / max(float(np.mean(mid ** 2)), 1e-30))


def listen(x, loop):
    """Measurements of musical intent for one decoded track (`loop`: also measure its seam)."""
    bpm, clarity = analysis.estimate_tempo(x)
    report = {
        "tempo_detected_bpm": bpm,
        "tempo_detected_clarity": clarity,
        "scale": analysis.detect_scale(x),
        "loudness": analysis.loudness_profile(x),
        "octaves_db": analysis.octave_balance(x),
        "width_db": width_db(x),
    }
    if loop:
        report["seam"] = analysis.seam(x)
    return report


def instruments(spec):
    """What plays on a track, from its spec: keys, leads, drummer, bass modes and the surface under the music."""
    leads = []
    for plan in spec.form:
        if plan.lead is not None and plan.lead not in leads:
            leads.append(plan.lead)
    bass = []
    for plan in spec.form:
        if plan.bass != "off" and plan.bass not in bass:
            bass.append(plan.bass)
    keys = []
    for plan in spec.form:
        if plan.keys != "off" and plan.keys not in keys:
            keys.append(plan.keys)
    extras = [voice for voice, _, _ in style_named(spec.drums).extras]
    return {
        "keys": f"{spec.keys_voice} ({', '.join(keys)})",
        "leads": leads,
        "drums": spec.drums + (f" (+ {', '.join(extras)})" if extras else ""),
        "bass": bass,
        "texture": spec.mood.texture,
        "events": [voice for _, voice, _ in spec.events],
    }


def _row(report):
    listened = report["listening"]
    measured = report["measured"]
    form = " ".join(part.split(" x")[0] for part in report["form"])
    return (f"| {report['title']} | {report['bpm']:g} / {listened['tempo_detected_bpm']:.1f} | {report['swing']:g} "
            f"| {report['family']} | {report['drums']} | {', '.join(report['instruments']['leads'])} "
            f"| {report['instruments']['keys']} | {report['instruments']['texture']} | {measured['duration_s']:.1f} "
            f"| {measured['loudness_lufs']:.2f} | {measured['true_peak_dbtp']:.2f} "
            f"| {listened['loudness']['range_lu']:.1f} | {measured['centroid_hz']:.0f} "
            f"| {measured['above_8k_db']:.1f} | {listened['width_db']:.1f} | {report['ending']} | {form} |")


def _tape_section(report):
    listened = report["listening"]
    measured = report["measured"]
    seam = listened["seam"]
    octaves = " ".join(f"{band}: {value:.0f}" for band, value in listened["octaves_db"].items())
    curve = " ".join(f"{value:.1f}" for value in listened["loudness"]["every_10s_lufs"])
    instruments_used = report["instruments"]
    lines = [
        f"### {report['title']} (`{report['id']}`)",
        "",
        report["notes"],
        "",
        f"- Tempo: {report['bpm']:g} BPM declared, {listened['tempo_detected_bpm']:.2f} BPM found blind "
        f"(onset fold clarity {listened['tempo_detected_clarity']:.2f}); swing {report['swing']:g}.",
        f"- Harmony: {report['key']}, family {report['family']}; tonal energy {100 * listened['scale']['share']:.1f} % "
        f"inside the {listened['scale']['scale']} collection (D major / B minor), "
        f"{100 * measured['chroma_out_of_key']:.1f} % outside the key.",
        f"- Form ({measured['duration_s']:.1f} s, one seamless loop): {', '.join(report['form'])}.",
        f"- Instruments: keys {instruments_used['keys']}; leads {', '.join(instruments_used['leads'])}; drums "
        f"{instruments_used['drums']}; bass {', '.join(instruments_used['bass'])}; bed {instruments_used['texture']}"
        + (f"; one-off {', '.join(instruments_used['events'])}" if instruments_used["events"] else "") + ".",
        f"- Loudness: {measured['loudness_lufs']:.2f} LUFS integrated, "
        f"true peak {measured['true_peak_dbtp']:.2f} dBTP, short-term range {listened['loudness']['range_lu']:.1f} LU "
        f"({listened['loudness']['min_lufs']:.1f} to {listened['loudness']['max_lufs']:.1f} LUFS); "
        f"every 10 s: {curve}.",
        f"- Spectrum: centroid {measured['centroid_hz']:.0f} Hz, energy above 8 kHz {measured['above_8k_db']:.1f} dB; "
        f"octave bands (dB re loudest) {octaves}.",
        f"- Stereo: correlation {measured['correlation']:.2f} "
        f"(below 150 Hz {measured['correlation_below_150hz']:.3f}), "
        f"side/mid above {WIDTH_ABOVE_HZ:.0f} Hz {listened['width_db']:.1f} dB.",
        f"- Loop seam: prediction error {seam['ratio']:.2f} x the file's own 99.9th percentile (<= 1 is seamless), "
        f"short-term loudness step {seam['loudness_step_lu']:.2f} LU between the last and first 3 s.",
        "",
    ]
    return lines


def _jingle_section(report):
    notes = ", ".join(f"{n['note']} at {n['onset_s']:.2f} s (heard {n['detected']}, {n['cents']:+.0f} c)"
                      for n in report["melody"])
    measured = report["measured"]
    return [
        f"- `{report['id']}`: {report['notes']} {measured['duration_s']:.2f} s, "
        f"{measured['sample_rate']} Hz mono, loudest 400 ms {measured['loudness_momentary_lufs']:.1f} LUFS, "
        f"true peak {measured['true_peak_dbtp']:.2f} dBTP, "
        f"{100 * measured['chroma_out_of_pentatonic']:.1f} % of its tonal energy outside D major pentatonic. "
        f"Notes: {notes}.",
    ]


def markdown(track_reports, tape_ids, jingle_reports):
    """The listening report for the tapes and jingles, with the whole catalogue side by side."""
    lines = [
        "# Tapes and jingles: listening report",
        "",
        "Generated by `python tools/music/build_music.py` from the reports next to this file; every number is "
        "measured on the decoded files the game ships. Nobody can listen inside the build, so each musical intent "
        "is checked by a measurement instead (tempo found blind from the onsets, scale from the chroma, loudness "
        "over time, spectral balance, stereo width, loop seam, jingle notes pitch-detected).",
        "",
        "## The catalogue side by side",
        "",
        "| Track | BPM (declared / found) | Swing | Family | Drums | Leads | Keys | Bed | Length s | LUFS | dBTP "
        "| LRA LU | Centroid Hz | >8 kHz dB | Width dB | Ending | Form |",
        "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|",
    ]
    lines.extend(_row(report) for report in track_reports)
    lines.extend(["", "## The tapes", ""])
    for report in track_reports:
        if report["id"] in tape_ids:
            lines.extend(_tape_section(report))
    lines.extend(["## Bell's station jingle", ""])
    for report in jingle_reports:
        lines.extend(_jingle_section(report))
    lines.append("")
    return "\n".join(lines)
