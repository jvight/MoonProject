"""
Score -> stereo master.

Stems: Rhodes (stereo tremolo + chorus), pad (low-passed, chorused wide), bass (mono, saturated, round), leads
(panned, dotted-eighth tape echo) and drums (round-robin kit, per-voice pans, dusty bus).
Mix: every stem is gain-staged to a loudness target (so all tracks balance alike), the kick ducks keys, pad, bass
and leads through a score-driven pump envelope, and a shared reverb sends from keys, pad, leads and snare.
Master: section low-pass automation (also the 11-14 kHz warmth roll-off), tape wow/flutter and saturation, mono
below 150 Hz, vinyl bed, ending (fade or tape stop), glue compression, loudness normalisation and a true-peak
limiter.
"""
import math

import numpy as np
from scipy.signal import oaconvolve
from synth import analysis, effects, envelope, filters
from synth.core import db_to_gain, pan, place, samples

from .instruments import DrumKit, bass_line, flute_note, kalimba_note, musicbox_note, pad_note, rhodes_note
from .texture import mono_below, tape_stop, vinyl

PRE_ROLL_S = 0.5
END_PAD_S = 0.5
TARGET_LUFS = -16.0
CEILING_DBTP = -1.5
STEM_LUFS = {"drums": -19.0, "bass": -21.0, "keys": -20.5, "pad": -28.0, "lead": -21.0}
PUMP_DEPTH = {"keys": 1.0, "pad": 1.0, "bass": 0.5, "lead": 0.3}
REVERB_SEND = {"keys": 0.22, "pad": 0.3, "lead": 0.45, "drums": 0.06, "bass": 0.0}
DRUM_PANS = {"kick": 0.0, "snare": -0.08, "rim": -0.18, "hat": 0.22, "ohat": 0.22, "shaker": -0.3}
LEAD_PANS = {"kalimba": 0.25, "musicbox": 0.3, "flute": -0.2}
FADE_S = 6.0
FADE_OVERHANG_S = 0.3
FADE_FLOOR_DB = -50.0
TAPE_STOP_S = 1.7
TAPE_DRIVE = 1.25
TAPE_LEVEL_LUFS = -20.0
GLUE_LEVEL_LUFS = -18.0
GLUE_THRESHOLD_DB = -24.0


class Renderer:
    """
    Renders one composed Score; `rng(*keys)` supplies seeded generators (synth.rng bound to the track).
    `limit_s` renders only the first seconds (previews): later events are skipped, not just cut.
    """

    def __init__(self, score, rng, limit_s=None):
        self.score = score
        self.spec = score.spec
        self.rng = rng
        self.preview = limit_s is not None and limit_s < score.duration_s
        length = min(score.duration_s, limit_s) if self.preview else score.duration_s
        self.n = samples(PRE_ROLL_S + length + END_PAD_S)

    def at(self, beat, offset_s=0.0):
        return samples(PRE_ROLL_S + self.score.seconds(beat) + offset_s)

    def _sound(self, voice, note, gen):
        if voice == "keys":
            return rhodes_note(note.pitch, note.velocity, self.score.seconds(note.length), gen)
        if voice == "pad":
            return pad_note(note.pitch, note.velocity, self.score.seconds(note.length), gen)
        if voice == "flute":
            return flute_note(note.pitch, note.velocity, self.score.seconds(note.length), gen)
        if voice == "kalimba":
            return kalimba_note(note.pitch, note.velocity, gen)
        return musicbox_note(note.pitch, note.velocity, gen)

    def _notes(self, notes, voice, gen):
        out = np.zeros(self.n)
        for note in notes:
            start = self.at(note.beat, note.offset_s)
            if start < self.n:
                place(out, self._sound(voice, note, gen), start)
        return out

    def keys(self):
        dry = self._notes(self.score.parts["keys"], "keys", self.rng("keys-notes"))
        rate = self.spec.bpm / 60.0 * 3.0
        stereo = np.stack([effects.tremolo(dry, rate, 0.4), effects.tremolo(dry, rate, 0.4, phase=0.5)], axis=1)
        stereo = effects.chorus(stereo, voices=2, rate=0.55, depth=0.0018, delay=0.011, mix=0.4, spread=0.9)
        return filters.highpass(stereo, 110.0)

    def pad(self):
        dry = self._notes(self.score.parts["pad"], "pad", self.rng("pad-notes"))
        dry = filters.butter(dry, "lowpass", 2600.0, order=2)
        wide = effects.chorus(dry, voices=3, rate=0.23, depth=0.004, delay=0.016, mix=0.6, spread=1.0)
        return filters.butter(wide, "highpass", 150.0, order=2)

    def bass(self):
        line = bass_line(self.score.parts["bass"], PRE_ROLL_S, self.n, self.score.seconds)
        line = filters.butter(effects.saturate(line, 1.6), "lowpass", 760.0, order=2)
        return np.stack([line, line], axis=1) * math.sqrt(0.5)

    def lead(self, name):
        voice = name.split(".", 1)[1]
        dry = filters.highpass(self._notes(self.score.parts[name], voice, self.rng("lead", voice)), 220.0)
        stereo = pan(dry, LEAD_PANS[voice])
        dotted_eighth = 0.75 * 60.0 / self.spec.bpm
        return effects.echo(stereo, dotted_eighth, feedback=0.32, damping_hz=2400.0, mix=0.28)

    def drums(self):
        kit = DrumKit(self.spec.drums, self.rng("kit"))
        voices = {}
        for hit in self.score.drums:
            buffer = voices.setdefault(hit.voice, np.zeros(self.n))
            sound = kit.hit(hit.voice, hit.velocity)
            place(buffer, sound, self.at(hit.beat, hit.offset_s), hit.velocity ** 1.2)
        bus = np.zeros((self.n, 2))
        for voice, buffer in sorted(voices.items()):
            if voice != "kick":
                buffer = filters.butter(buffer, "highpass", 120.0, order=2)
            bus += pan(buffer, DRUM_PANS[voice])
        return filters.butter(effects.saturate(bus * 2.0, 1.2) / 2.0, "lowpass", 11000.0, order=2)

    def pump(self):
        """Duck envelope (dB, positive = quieter) from the score's kick hits."""
        impulses = np.zeros(self.n)
        for hit in self.score.drums:
            position = self.at(hit.beat, hit.offset_s)
            if hit.voice == "kick" and position < self.n:
                impulses[position] += hit.velocity
        kernel_n = samples(0.6)
        kernel = envelope.ar(kernel_n, 0.008, 0.45)
        shape = np.minimum(1.0, oaconvolve(impulses, kernel)[:self.n])
        return self.spec.mood.pump_db * shape

    def cutoff_curve(self):
        points = sorted(self.score.cutoff)
        times = np.array([self.at(beat) for beat, _ in points], dtype=np.float64)
        logs = np.log(np.array([hz for _, hz in points]))
        return np.exp(np.interp(np.arange(self.n), times, logs))

    def stems(self):
        stems = {"keys": self.keys(), "bass": self.bass(), "drums": self.drums()}
        if "pad" in self.score.parts:
            stems["pad"] = self.pad()
        for name in self.score.parts:
            if name.startswith("lead."):
                stems[name] = self.lead(name)
        return stems


def _family(stem_name):
    return stem_name.split(".", 1)[0]


def _to_lufs(x, target):
    return x * db_to_gain(target - analysis.loudness_integrated(x))


def mix(renderer, stems):
    """Gain-stage, pump and reverb the stems into one stereo music bus."""
    space = renderer.spec.mood.space
    duck = renderer.pump()
    bus = np.zeros((renderer.n, 2))
    send = np.zeros((renderer.n, 2))
    for name, stem in sorted(stems.items()):
        family = _family(name)
        staged = _to_lufs(stem, STEM_LUFS[family])
        if family in PUMP_DEPTH:
            staged = staged * db_to_gain(-PUMP_DEPTH[family] * duck)[:, None]
        bus += staged
        send += REVERB_SEND[family] * space * staged
    wet = effects.reverb(send, room=0.84, damping=0.55, wet=1.0, dry=0.0, width=1.0, predelay=0.025)
    wet = filters.butter(filters.butter(wet, "highpass", 220.0, order=2), "lowpass", 7000.0, order=2)
    return bus + wet


def master(renderer, music):
    """Master chain; returns the final stereo signal (length may shrink for a tape-stop ending)."""
    spec = renderer.spec
    music = filters.swept(music, "lowpass", renderer.cutoff_curve(), q=0.6, block=64)
    music = effects.wow_flutter(music, wow_cents=spec.mood.wow_cents, wow_rate=0.42, flutter_cents=1.5,
                                flutter_rate=6.3, drift_cents=2.5, gen=renderer.rng("tape"))
    music = _to_lufs(music, TAPE_LEVEL_LUFS)
    music = effects.saturate(music, TAPE_DRIVE) * (math.tanh(TAPE_DRIVE) / TAPE_DRIVE)
    music = mono_below(filters.butter(music, "highpass", 25.0, order=2))
    music_lufs = analysis.loudness_integrated(music)
    bed = vinyl(renderer.n, renderer.rng("vinyl"))
    out = music + _to_lufs(bed, music_lufs + spec.mood.dust_db)
    music_end_s = PRE_ROLL_S + renderer.score.seconds(spec.music_beats)
    if renderer.preview:
        out = envelope.fade_out(out, 1.0)
    elif spec.ending == "tapestop":
        stop_s = music_end_s - renderer.score.seconds(2.0)
        out = tape_stop(out, stop_s, TAPE_STOP_S)[:samples(stop_s + TAPE_STOP_S + 0.3)]
    else:
        end = samples(music_end_s + FADE_OVERHANG_S)
        out = fader_out(out[:end], FADE_S + FADE_OVERHANG_S)
    out = envelope.fade_in(out, 0.08)
    out = _to_lufs(out, GLUE_LEVEL_LUFS)
    out = effects.compressor(out, threshold_db=GLUE_THRESHOLD_DB, ratio=1.6, attack=0.03, release=0.3, knee_db=8.0,
                             rms_time=0.05)
    return normalise(out)


def fader_out(x, duration_s, floor_db=FADE_FLOOR_DB):
    """Mixing-desk fade: the gain falls linearly in dB to `floor_db`, then a short ramp closes to exact zero."""
    count = min(x.shape[0], samples(duration_s))
    gains = db_to_gain(np.linspace(0.0, floor_db, count))
    out = np.array(x, copy=True)
    out[x.shape[0] - count:] *= gains[:, None]
    return envelope.fade_out(out, 0.03)


def normalise(x, target=TARGET_LUFS, ceiling=CEILING_DBTP, passes=4, tolerance_lu=0.05):
    """Loudness to `target` LUFS through a true-peak limiter, re-aiming the input gain until it lands."""
    gain_db = target - analysis.loudness_integrated(x)
    for _ in range(passes):
        limited = effects.limiter(x * db_to_gain(gain_db), ceiling_db=ceiling, lookahead=0.005, release=0.12)
        error = target - analysis.loudness_integrated(limited)
        if abs(error) <= tolerance_lu:
            break
        gain_db += error
    return limited


def render(score, rng, limit_s=None):
    """Score -> (stereo master, stems before gain staging)."""
    renderer = Renderer(score, rng, limit_s)
    stems = renderer.stems()
    return master(renderer, mix(renderer, stems)), stems
