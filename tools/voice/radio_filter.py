"""The old-tape and radio treatments that turn a dry TTS take into one of Lunarion's recordings.

Everything 07 hears is a recording (GDD v3 section 13): Ro's letters are cassette tape (warm, rounded, a soft hiss
and a motor hum on D), the automated station is a narrow broadcast band (a steady carrier on D, faint static).
Both stay warm and calm: the top end is shelved down rather than boosted, saturation only rounds peaks, and the
hum and carrier sit on D so they never fight the D major score. Deterministic: noise comes from ``synth.core.rng``
keyed by the line.
"""
import math
from dataclasses import dataclass

import numpy as np
from scipy.signal import resample

from synth.analysis import loudness_integrated
from synth.core import SAMPLE_RATE, db_to_gain, note_freq, rng, time_axis
from synth.effects import compressor, reverb, saturate, soft_limit, wow_flutter
from synth.envelope import fade_in, fade_out
from synth.filters import butter, highshelf, lowshelf, peaking
from synth.noise import pink, white

PEAK_CEILING_DB = -1.2
SPEECH_DRIVE_PEAK_DB = -6.0


@dataclass(frozen=True)
class Treatment:
    """One filter preset. Levels are dBFS after the speech is normalised to ``speech_lufs``."""
    name: str
    speech_lufs: float
    highpass_hz: float
    lowpass_hz: float
    band_order: int
    warmth_db: float
    warmth_hz: float
    soften_db: float
    soften_hz: float
    presence_db: float
    presence_hz: float
    drive: float
    wow_cents: float
    flutter_cents: float
    room_wet: float
    bed_db: float
    bed_low_hz: float
    bed_high_hz: float
    bed_pink: bool
    tone_db: float
    tone_notes: tuple
    tone_wobble_hz: float
    lead_s: float
    tail_s: float
    compress: bool


TAPE = Treatment(
    name="tape", speech_lufs=-20.0, highpass_hz=90.0, lowpass_hz=7200.0, band_order=4,
    warmth_db=2.5, warmth_hz=260.0, soften_db=-4.0, soften_hz=4200.0, presence_db=0.0, presence_hz=1500.0,
    drive=1.5, wow_cents=4.0, flutter_cents=1.2, room_wet=0.07,
    bed_db=-54.0, bed_low_hz=120.0, bed_high_hz=6000.0, bed_pink=True,
    tone_db=-60.0, tone_notes=("D2", "D3"), tone_wobble_hz=0.0, lead_s=0.5, tail_s=0.8, compress=False)

RADIO = Treatment(
    name="radio", speech_lufs=-21.0, highpass_hz=280.0, lowpass_hz=3600.0, band_order=2,
    warmth_db=1.5, warmth_hz=420.0, soften_db=-3.0, soften_hz=3000.0, presence_db=1.5, presence_hz=1300.0,
    drive=1.3, wow_cents=0.0, flutter_cents=0.0, room_wet=0.0,
    bed_db=-52.0, bed_low_hz=500.0, bed_high_hz=4500.0, bed_pink=False,
    tone_db=-57.0, tone_notes=("D3", "A3"), tone_wobble_hz=0.25, lead_s=0.6, tail_s=0.6, compress=True)

TREATMENTS = {t.name: t for t in (TAPE, RADIO)}


def to_project_rate(take: np.ndarray, take_rate: int, semitones: float) -> np.ndarray:
    """Resamples a take to 48 kHz, played ``semitones`` lower or higher (tape-speed style: pace follows pitch)."""
    speed = 2.0 ** (semitones / 12.0)
    length = int(round(take.shape[0] * SAMPLE_RATE / (take_rate * speed)))
    return resample(np.asarray(take, dtype=np.float64), length)


def _trim(x: np.ndarray, threshold_db: float = -50.0) -> np.ndarray:
    """Drops the take's own leading and trailing near-silence (the treatment adds its own lead and tail)."""
    loud = np.flatnonzero(np.abs(x) > db_to_gain(threshold_db))
    return x[loud[0]:loud[-1] + 1] if loud.size else x


def _shape_speech(x: np.ndarray, t: Treatment) -> np.ndarray:
    y = butter(x, "highpass", t.highpass_hz, order=2)
    y = lowshelf(y, t.warmth_hz, t.warmth_db)
    if t.presence_db:
        y = peaking(y, t.presence_hz, t.presence_db, q=0.8)
    y = highshelf(y, t.soften_hz, t.soften_db)
    peak = float(np.max(np.abs(y))) or 1.0
    y = saturate(y * db_to_gain(SPEECH_DRIVE_PEAK_DB) / peak, t.drive)
    if t.compress:
        y = compressor(y, threshold_db=-24.0, ratio=2.5, attack=0.01, release=0.2)
    y = butter(y, "lowpass", t.lowpass_hz, order=t.band_order)
    if t.wow_cents or t.flutter_cents:
        y = wow_flutter(y, wow_cents=t.wow_cents, wow_rate=0.5, flutter_cents=t.flutter_cents, flutter_rate=5.5)
    if t.room_wet:
        y = reverb(y, room=0.3, damping=0.7, wet=t.room_wet, dry=1.0, width=0.0, tail=0.3).mean(axis=1)
    return y


def _bed(n: int, t: Treatment, key: str) -> np.ndarray:
    """The hiss or static under the voice plus a steady hum/carrier on D, both very low."""
    gen = rng("voice", key, t.name, "bed")
    noise = pink(n, gen) if t.bed_pink else white(n, gen)
    noise = butter(noise, "bandpass", (t.bed_low_hz, t.bed_high_hz), order=2)
    noise *= db_to_gain(t.bed_db) / (math.sqrt(float(np.mean(noise ** 2))) or 1.0)
    time = time_axis(n)
    tone = np.zeros(n)
    for index, note in enumerate(t.tone_notes):
        tone += np.sin(2.0 * math.pi * note_freq(note) * time) / (index + 1)
    if t.tone_wobble_hz:
        tone *= 1.0 - 0.3 * (0.5 + 0.5 * np.sin(2.0 * math.pi * t.tone_wobble_hz * time))
    tone *= db_to_gain(t.tone_db) / (math.sqrt(float(np.mean(tone ** 2))) or 1.0)
    return noise + tone


def treat(speech: np.ndarray, t: Treatment, key: str) -> tuple:
    """Dry 48 kHz speech -> ``(clip, speech_start_seconds, speech_seconds)``; mono float64, edges at zero."""
    dry = _trim(speech)
    voiced = _shape_speech(dry, t)
    voiced *= db_to_gain(t.speech_lufs - loudness_integrated(voiced))
    lead = int(round(t.lead_s * SAMPLE_RATE))
    tail = int(round(t.tail_s * SAMPLE_RATE))
    clip = np.zeros(lead + voiced.shape[0] + tail)
    clip[lead:lead + voiced.shape[0]] += voiced
    clip += _bed(clip.shape[0], t, key)
    clip = fade_in(clip, t.lead_s * 0.6)
    clip = fade_out(clip, t.tail_s * 0.7)
    clip = soft_limit(clip, PEAK_CEILING_DB)
    clip[0] = clip[-1] = 0.0
    return clip, lead / SAMPLE_RATE, voiced.shape[0] / SAMPLE_RATE
