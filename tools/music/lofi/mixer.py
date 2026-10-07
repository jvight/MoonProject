"""
Score -> stereo master.

Stems: keys (Rhodes with stereo tremolo + chorus, or felt piano with a gentle chorus), pad (low-passed, chorused
wide, optionally drifting in a slow orbit), bass (mono, saturated, round), leads (panned, dotted-eighth tape echo;
vibes through a shared motor tremolo, a Rhodes lead auto-panned, the theremin as one continuous voice) and drums
(round-robin kit, per-voice pans, dusty bus).
Mix: every stem is gain-staged to a loudness target (so all tracks balance alike), the kick ducks keys, pad, bass
and leads through a score-driven pump envelope, and a shared reverb sends from keys, pad, leads and snare.
Master: the airlock room (one tape), section low-pass automation (also the 11-14 kHz warmth roll-off), presence, tape
wow/flutter and saturation, mono below 150 Hz, stereo width, surface-noise bed (vinyl, cassette hiss or airlock room
tone), ending (fade, tape stop, or a seamless loop for the tapes), glue compression, loudness normalisation and a
true-peak limiter. A loop folds its pre-roll and tail into the cycle before the bed is laid and runs the dynamics in
their steady state, so the last sample of the file flows into its first.
"""
import math

import numpy as np
from scipy.signal import oaconvolve
from synth import analysis, effects, envelope, filters
from synth.core import db_to_gain, pan, place, samples

from .harmony import BEATS_PER_BAR
from .instruments import (DrumKit, bass_line, felt_note, flute_note, kalimba_note, musicbox_note, pad_note,
                          rhodes_note, theremin_line, vibes_note)
from .texture import airlock_room, bed, mono_below, narrow, orbit, steady, tape_stop, wrap_loop
from .voicing import KEYS_STYLE

PRE_ROLL_S = 0.5
END_PAD_S = 0.5
TARGET_LUFS = -16.0
CEILING_DBTP = -1.5
STEM_LUFS = {"drums": -19.0, "bass": -21.0, "keys": -20.5, "pad": -28.0, "lead": -21.0}
PUMP_DEPTH = {"keys": 1.0, "pad": 1.0, "bass": 0.5, "lead": 0.3}
REVERB_SEND = {"keys": 0.22, "pad": 0.3, "lead": 0.45, "drums": 0.06, "bass": 0.0}
DRUM_PANS = {"kick": 0.0, "snare": -0.08, "rim": -0.18, "hat": 0.22, "ohat": 0.22, "shaker": -0.3, "snap": 0.15,
             "swish": -0.12, "thump": -0.45}
UNFILTERED_DRUMS = ("kick", "thump")
LEAD_PANS = {"kalimba": 0.25, "musicbox": 0.3, "flute": -0.2, "vibes": 0.2, "rhodes": -0.1, "theremin": -0.3,
             "felt": 0.35}
CROSS_ECHO_LEADS = ("theremin", "felt")
LEAD_ECHO_MIX = 0.28
VIBES_MOTOR_PER_BEAT = 2.0
VIBES_MOTOR_DEPTH = 0.25
RHODES_LEAD_PAN_PER_BEAT = 0.5
RHODES_LEAD_PAN_DEPTH = 0.5
PAD_ORBIT_DEPTH = 0.6
FELT_SPREAD = 0.9
PRESENCE_HZ = 3000.0
LOOP_TAIL_S = 6.0
LOOP_TAIL_FADE_S = 0.5
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
        self.looping = self.spec.ending == "loop" and not self.preview
        length = min(score.duration_s, limit_s) if self.preview else score.duration_s
        self.n = samples(PRE_ROLL_S + length + END_PAD_S)
        self.loop_n = samples(score.seconds(self.spec.music_beats))
        if self.looping:
            self.n = samples(PRE_ROLL_S) + self.loop_n + samples(LOOP_TAIL_S)

    def at(self, beat, offset_s=0.0):
        return samples(PRE_ROLL_S + self.score.seconds(beat) + offset_s)

    def _sound(self, voice, note, gen):
        gate_s = self.score.seconds(note.length)
        if voice == "keys":
            voice = self.spec.keys_voice
        if voice == "rhodes":
            return rhodes_note(note.pitch, note.velocity, gate_s, gen)
        if voice == "felt":
            return felt_note(note.pitch, note.velocity, gate_s, gen)
        if voice == "pad":
            return pad_note(note.pitch, note.velocity, gate_s, gen)
        if voice == "flute":
            return flute_note(note.pitch, note.velocity, gate_s, gen)
        if voice == "vibes":
            return vibes_note(note.pitch, note.velocity, gate_s, gen)
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

    def _spread_notes(self, notes, voice, gen):
        """Notes panned by pitch across the keys register, low left to high right, like sitting at a piano."""
        out = np.zeros((self.n, 2))
        span = KEYS_STYLE.high - KEYS_STYLE.low
        for note in notes:
            start = self.at(note.beat, note.offset_s)
            if start < self.n:
                position = FELT_SPREAD * (2.0 * (note.pitch - KEYS_STYLE.low) / span - 1.0)
                place(out, pan(self._sound(voice, note, gen), position), start)
        return out

    def keys(self):
        if self.spec.keys_voice == "felt":
            spread = self._spread_notes(self.score.parts["keys"], "keys", self.rng("keys-notes"))
            stereo = effects.chorus(spread, voices=2, rate=0.31, depth=0.0012, delay=0.013, mix=0.3, spread=0.8)
            return filters.highpass(stereo, 90.0)
        dry = self._notes(self.score.parts["keys"], "keys", self.rng("keys-notes"))
        rate = self.spec.bpm / 60.0 * 3.0
        stereo = np.stack([effects.tremolo(dry, rate, 0.4), effects.tremolo(dry, rate, 0.4, phase=0.5)], axis=1)
        stereo = effects.chorus(stereo, voices=2, rate=0.55, depth=0.0018, delay=0.011, mix=0.4, spread=0.9)
        return filters.highpass(stereo, 110.0)

    def pad(self):
        dry = self._notes(self.score.parts["pad"], "pad", self.rng("pad-notes"))
        dry = filters.butter(dry, "lowpass", 2600.0, order=2)
        wide = effects.chorus(dry, voices=3, rate=0.23, depth=0.004, delay=0.016, mix=0.6, spread=1.0)
        wide = filters.butter(wide, "highpass", 150.0, order=2)
        if self.spec.mood.orbit_bars:
            rate = self.spec.bpm / 60.0 / (BEATS_PER_BAR * self.spec.mood.orbit_bars)
            wide = orbit(wide, rate, PAD_ORBIT_DEPTH)
        return wide

    def bass(self):
        line = bass_line(self.score.parts["bass"], PRE_ROLL_S, self.n, self.score.seconds)
        line = filters.butter(effects.saturate(line, 1.6), "lowpass", 760.0, order=2)
        return np.stack([line, line], axis=1) * math.sqrt(0.5)

    def lead(self, name):
        voice = name.split(".", 1)[1]
        notes = self.score.parts[name]
        beat_hz = self.spec.bpm / 60.0
        if voice == "theremin":
            dry = theremin_line(notes, PRE_ROLL_S, self.n, self.score.seconds)
        else:
            dry = self._notes(notes, voice, self.rng("lead", voice))
        dry = filters.highpass(dry, 220.0)
        if voice == "vibes":
            dry = effects.tremolo(dry, beat_hz * VIBES_MOTOR_PER_BEAT, VIBES_MOTOR_DEPTH)
        stereo = pan(dry, LEAD_PANS[voice])
        if voice == "rhodes":
            stereo = orbit(stereo, beat_hz * RHODES_LEAD_PAN_PER_BEAT, RHODES_LEAD_PAN_DEPTH)
        dotted_eighth = 0.75 * 60.0 / self.spec.bpm
        if voice in CROSS_ECHO_LEADS:
            repeats = effects.echo(dry, dotted_eighth, feedback=0.32, damping_hz=2400.0, mix=1.0) - dry
            return stereo + LEAD_ECHO_MIX * pan(repeats, -LEAD_PANS[voice])
        return effects.echo(stereo, dotted_eighth, feedback=0.32, damping_hz=2400.0, mix=LEAD_ECHO_MIX)

    def drums(self):
        kit = DrumKit(self.spec.drums, self.rng("kit"))
        voices = {}
        for hit in self.score.drums:
            buffer = voices.setdefault(hit.voice, np.zeros(self.n))
            sound = kit.hit(hit.voice, hit.velocity)
            place(buffer, sound, self.at(hit.beat, hit.offset_s), hit.velocity ** 1.2)
        bus = np.zeros((self.n, 2))
        for voice, buffer in sorted(voices.items()):
            if voice not in UNFILTERED_DRUMS:
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


def _glue(x):
    return effects.compressor(x, threshold_db=GLUE_THRESHOLD_DB, ratio=1.6, attack=0.03, release=0.3, knee_db=8.0,
                              rms_time=0.05)


def master(renderer, music):
    """Master chain; returns the final stereo signal (length may shrink for a tape-stop ending; a loop is exactly
    one cycle long)."""
    spec = renderer.spec
    mood = spec.mood
    if mood.texture == "airlock":
        music = airlock_room(music)
    music = filters.swept(music, "lowpass", renderer.cutoff_curve(), q=0.6, block=64)
    if mood.presence_db:
        music = filters.highshelf(music, PRESENCE_HZ, mood.presence_db)
    music = effects.wow_flutter(music, wow_cents=mood.wow_cents, wow_rate=0.42, flutter_cents=1.5,
                                flutter_rate=6.3, drift_cents=2.5, gen=renderer.rng("tape"))
    music = _to_lufs(music, TAPE_LEVEL_LUFS)
    music = effects.saturate(music, TAPE_DRIVE) * (math.tanh(TAPE_DRIVE) / TAPE_DRIVE)
    music = mono_below(filters.butter(music, "highpass", 25.0, order=2))
    if mood.width < 1.0:
        music = narrow(music, mood.width)
    if renderer.looping:
        music = wrap_loop(envelope.fade_out(music, LOOP_TAIL_FADE_S), samples(PRE_ROLL_S), renderer.loop_n)
    music_lufs = analysis.loudness_integrated(music)
    surface = bed(mood.texture, music.shape[0], renderer.rng(mood.texture), renderer.looping)
    out = music + _to_lufs(surface, music_lufs + mood.dust_db)
    music_end_s = PRE_ROLL_S + renderer.score.seconds(spec.music_beats)
    if renderer.preview:
        out = envelope.fade_out(out, 1.0)
    elif spec.ending == "tapestop":
        stop_s = music_end_s - renderer.score.seconds(2.0)
        out = tape_stop(out, stop_s, TAPE_STOP_S)[:samples(stop_s + TAPE_STOP_S + 0.3)]
    elif spec.ending == "fade":
        end = samples(music_end_s + FADE_OVERHANG_S)
        out = fader_out(out[:end], FADE_S + FADE_OVERHANG_S)
    if not renderer.looping:
        out = envelope.fade_in(out, 0.08)
    out = _to_lufs(out, GLUE_LEVEL_LUFS)
    out = steady(out, _glue) if renderer.looping else _glue(out)
    return normalise(out, loop=renderer.looping)


def fader_out(x, duration_s, floor_db=FADE_FLOOR_DB):
    """Mixing-desk fade: the gain falls linearly in dB to `floor_db`, then a short ramp closes to exact zero."""
    count = min(x.shape[0], samples(duration_s))
    gains = db_to_gain(np.linspace(0.0, floor_db, count))
    out = np.array(x, copy=True)
    out[x.shape[0] - count:] *= gains[:, None]
    return envelope.fade_out(out, 0.03)


def normalise(x, target=TARGET_LUFS, ceiling=CEILING_DBTP, passes=4, tolerance_lu=0.05, loop=False):
    """
    Loudness to `target` LUFS through a true-peak limiter, re-aiming the input gain until it lands. A `loop` is
    limited in its steady state, so the limiter's gain flows across the seam.
    """
    def limit(y):
        return effects.limiter(y, ceiling_db=ceiling, lookahead=0.005, release=0.12)

    gain_db = target - analysis.loudness_integrated(x)
    for _ in range(passes):
        scaled = x * db_to_gain(gain_db)
        limited = steady(scaled, limit) if loop else limit(scaled)
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
