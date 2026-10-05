"""
The station's instrument voices, built on the shared DSP core (tools/audio/synth). Each note function returns a
mono float64 array that starts from silence and ends at silence; nothing has a hard attack or a whistling top.

  rhodes_note   two FM stacks: a 1:1 body pair whose index jumps with velocity and decays (the bark of a hard hit)
                over a steady warm index, and a 14:1 tine pair that rings only for the first tens of milliseconds
                (the bell of the tine, thinned out for high notes so it never reaches past ~6 kHz); two-stage
                decay, felt-damper release.
  pad_note      three detuned band-limited saws under a slow swell (the pad bus low-passes them).
  bass_line     one continuous monophonic oscillator for the whole track (sine + soft octave), so slides are
                real glides and legato notes never re-attack.
  kalimba_note  the core kalimba tine.
  musicbox_note a tuned comb tooth: fundamental, faint detuned octave, a short high partial, a tiny mechanism tick.
  flute_note    soft FM flute: low index, delayed vibrato, breath noise around the second harmonic.
  DrumKit       round-robin, velocity-layered one-shots per drummer style.
"""
import math

import numpy as np
from synth import envelope, filters, fm, instruments, noise, osc
from synth.core import SAMPLE_RATE, midi_to_freq, samples

RHODES_RELEASE_S = 0.35
PAD_ATTACK_S = 0.9
PAD_RELEASE_S = 1.6
LEAD_RING_S = 1.8
TINE_RATIO = 14.0
TINE_CEILING_HZ = 6000.0
KICK_BOTTOM_HZ = 50.0


def _detuned(freq, cents):
    return freq * 2.0 ** (cents / 1200.0)


def rhodes_note(pitch, velocity, gate_s, gen):
    """Electric piano note held for `gate_s` seconds, then damped."""
    freq = _detuned(midi_to_freq(pitch), float(gen.normal(0.0, 1.5)))
    n = samples(gate_s + RHODES_RELEASE_S)
    t = np.arange(n) / SAMPLE_RATE
    register = min(1.0, max(0.25, (84 - pitch) / 30.0))
    bark = (0.6 + 2.2 * velocity ** 2) * register * np.exp(-t / 0.25) + 0.7
    body = fm.two_op(n, freq, 1.0, bark)
    tine_reach = min(1.0, TINE_CEILING_HZ / (TINE_RATIO * freq)) ** 2
    tine_index = 1.2 * velocity * tine_reach * np.exp(-t / 0.03)
    tine = fm.two_op(n, freq, TINE_RATIO, tine_index) * np.exp(-t / 0.8)
    sustain_t = 2.8 * 2.0 ** (-(pitch - 60) / 24.0)
    amp = 0.35 * np.exp(-t / 0.18) + 0.65 * np.exp(-t / sustain_t)
    attack = envelope.curve(min(n, samples(0.003)), "smooth")
    amp[:attack.shape[0]] *= attack
    gate_n = min(n, samples(gate_s))
    amp[gate_n:] *= np.exp(-(t[gate_n:] - t[gate_n]) / 0.07)
    note = (0.85 * body + 0.4 * tine) * amp * (0.3 + 0.7 * velocity ** 1.4)
    return envelope.fade_out(note, 0.02)


def pad_note(pitch, velocity, gate_s, gen):
    """Pad voice held for `gate_s` seconds: three detuned saws, slow swell, long release."""
    n = samples(gate_s + PAD_RELEASE_S)
    freq = midi_to_freq(pitch)
    voices = sum(osc.saw(n, _detuned(freq, cents), phase=float(gen.random())) for cents in (-9.0, 0.0, 8.0))
    env = envelope.adsr(n, PAD_ATTACK_S, 1.2, 0.85, 0.8 * PAD_RELEASE_S, gate=gate_s, shape="exp")
    return envelope.fade_out(voices * env * (velocity / 3.0), 0.05)


def _legato_groups(notes):
    """Split a line into groups joined by slides: a sliding note continues its predecessor's gate."""
    groups = []
    for note in notes:
        if note.glide and groups:
            groups[-1].append(note)
        else:
            groups.append([note])
    return groups


def bass_line(notes, start_s, n, seconds_of, glide_s=0.07):
    """
    Render a monophonic bass line (NoteEvents) into `n` samples. `seconds_of(beat)` maps beats to seconds;
    `start_s` shifts the whole line (pre-roll). Notes joined by slides share one envelope and glide in pitch over
    `glide_s`, so they never re-attack.
    """
    def at(beat, offset):
        return min(n, samples(start_s + seconds_of(beat) + offset))

    freq = np.full(n, midi_to_freq(notes[0].pitch) if notes else 55.0)
    amp = np.zeros(n)
    starts = [at(note.beat, note.offset_s) for note in notes] + [n]
    for index, note in enumerate(notes):
        start, stop, target = starts[index], starts[index + 1], midi_to_freq(note.pitch)
        freq[start:stop] = target
        if note.glide and index > 0:
            glide_n = min(stop - start, samples(glide_s))
            freq[start:start + glide_n] = osc.glide(glide_n, midi_to_freq(notes[index - 1].pitch), target)
    release_n = samples(0.06)
    for group in _legato_groups(notes):
        start = at(group[0].beat, group[0].offset_s)
        stop = at(group[-1].end, group[-1].offset_s)
        gate = stop - start
        peak = 0.55 + 0.45 * group[0].velocity
        body = envelope.adsr(gate + release_n, 0.006, 0.4, 0.8, 0.05, gate=gate / SAMPLE_RATE, shape="exp") * peak
        end = min(n, start + body.shape[0])
        amp[start:end] = np.maximum(amp[start:end], body[:end - start])
    tone = osc.sine(n, freq) + 0.22 * osc.sine(n, 2.0 * freq, phase=0.25)
    return tone * amp


def kalimba_note(pitch, velocity, gen):
    return instruments.kalimba(midi_to_freq(pitch), LEAD_RING_S, gen, decay=1.5, tine=0.07, warmth=0.15) * velocity


def musicbox_note(pitch, velocity, gen):
    """Music-box tooth: soft and short so the high register never stings."""
    n = samples(LEAD_RING_S)
    freq = midi_to_freq(pitch)
    tone = (instruments.partial(n, freq, 1.0, 0.002, 1.3)
            + instruments.partial(n, 2.0 * freq, 0.12, 0.002, 0.5, detune_cents=4.0)
            + instruments.partial(n, 4.2 * freq, 0.035, 0.001, 0.12))
    tick_n = samples(0.003)
    tick = filters.bandpass(noise.white(tick_n, gen), 2500.0, 1.2) * envelope.ar(tick_n, 0.0002, 0.002)
    tone[:tick_n] += 0.02 * tick
    return tone * velocity * 0.8


def flute_note(pitch, velocity, gate_s, gen):
    """Breathy FM flute held for `gate_s` seconds."""
    release = 0.18
    n = samples(gate_s + release)
    base = midi_to_freq(pitch)
    t = np.arange(n) / SAMPLE_RATE
    depth = 11.0 * np.clip((t - 0.25) / 0.4, 0.0, 1.0)
    freq = base * 2.0 ** (depth * np.sin(2.0 * math.pi * (5.1 * t + float(gen.random()))) / 1200.0)
    env = envelope.adsr(n, 0.09, 0.5, 0.82, release / 3.0, gate=gate_s, shape="exp")
    index = 0.45 + 0.25 * np.exp(-t / 0.08)
    tone = fm.two_op(n, freq, 1.0, index)
    breath = filters.bandpass(noise.white(n, gen), 2.0 * base, 1.6) * 0.18
    return envelope.fade_out((tone + breath) * env * velocity * 0.7, 0.02)


class DrumKit:
    """
    One drummer's sounds: `variants` round-robin takes in three velocity layers per voice, so repeated hits are
    never identical samples and soft hits are also darker.
    """
    LAYERS = (0.4, 0.75)

    def __init__(self, style, gen, variants=4):
        params = KIT_PARAMS[style]
        self._takes = {}
        for voice in ("kick", "snare", "rim", "hat", "ohat", "shaker"):
            for layer, hardness in enumerate((0.35, 0.65, 1.0)):
                self._takes[(voice, layer)] = [envelope.fade_out(_DRUMS[voice](params, hardness, gen), 0.01)
                                               for _ in range(variants)]
        self._counter = {}

    def hit(self, voice, velocity):
        layer = 0 if velocity < self.LAYERS[0] else 1 if velocity < self.LAYERS[1] else 2
        takes = self._takes[(voice, layer)]
        count = self._counter.get(voice, 0)
        self._counter[voice] = count + 1
        return takes[count % len(takes)]


KIT_PARAMS = {
    "boombap": {"kick_top": 135.0, "kick_t60": 0.5, "click": 0.06, "snare_tone": 1.0, "snare_t60": 0.2,
                "snare_attack": 0.001, "snare_lp": 6000.0},
    "laidback": {"kick_top": 120.0, "kick_t60": 0.55, "click": 0.04, "snare_tone": 0.8, "snare_t60": 0.26,
                 "snare_attack": 0.002, "snare_lp": 5200.0},
    "halftime": {"kick_top": 110.0, "kick_t60": 0.7, "click": 0.035, "snare_tone": 0.9, "snare_t60": 0.34,
                 "snare_attack": 0.002, "snare_lp": 4800.0},
    "brushy": {"kick_top": 115.0, "kick_t60": 0.5, "click": 0.03, "snare_tone": 0.25, "snare_t60": 0.32,
               "snare_attack": 0.009, "snare_lp": 4200.0},
}


def _kick(params, hardness, gen):
    n = samples(0.6)
    t = np.arange(n) / SAMPLE_RATE
    top = params["kick_top"] * (0.9 + 0.1 * hardness) * float(gen.uniform(0.98, 1.02))
    freq = KICK_BOTTOM_HZ + (top - KICK_BOTTOM_HZ) * np.exp(-t / 0.032)
    body = osc.sine(n, freq) * envelope.ar(n, 0.0015, params["kick_t60"])
    click_n = samples(0.006)
    click = filters.lowpass(noise.white(click_n, gen), 2200.0) * envelope.ar(click_n, 0.0003, 0.005)
    body[:click_n] += params["click"] * hardness * click
    return filters.lowpass(np.tanh(1.3 * body) / math.tanh(1.3), 3500.0)


def _snare(params, hardness, gen):
    n = samples(0.5)
    t = np.arange(n) / SAMPLE_RATE
    tone = (0.5 * osc.sine(n, 188.0 * (1.0 + 0.04 * np.exp(-t / 0.02))) * envelope.ar(n, 0.001, 0.13)
            + 0.22 * osc.sine(n, 334.0) * envelope.ar(n, 0.001, 0.08))
    hiss = filters.butter(noise.white(n, gen), "bandpass", (800.0, 5000.0), order=2)
    hiss *= envelope.ar(n, params["snare_attack"], params["snare_t60"] * (0.8 + 0.2 * hardness))
    out = params["snare_tone"] * tone + 0.75 * hiss
    return filters.lowpass(out, params["snare_lp"] * (0.7 + 0.3 * hardness))


def _rim(params, hardness, gen):
    n = samples(0.15)
    wood = (0.6 * osc.sine(n, 470.0) * envelope.ar(n, 0.0005, 0.05)
            + 0.3 * osc.sine(n, 1650.0) * envelope.ar(n, 0.0005, 0.03))
    tick = filters.bandpass(noise.white(n, gen), 2400.0, 1.5) * envelope.ar(n, 0.0003, 0.012)
    return filters.lowpass(wood + 0.4 * tick, 4500.0 + 2000.0 * hardness)


def _metal(n, gen):
    ratios = (1.0, 1.4831, 1.8003, 2.5462, 2.6302, 3.8967)
    base = float(gen.uniform(200.0, 210.0))
    return sum(osc.square(n, base * r, phase=float(gen.random())) for r in ratios) / len(ratios)


def _hat(params, hardness, gen, t60=0.06):
    n = samples(max(0.12, 1.4 * t60))
    mix = 0.75 * noise.white(n, gen) + 0.35 * _metal(n, gen)
    shaped = filters.butter(filters.butter(mix, "highpass", 5000.0, order=2), "lowpass", 9500.0, order=2)
    return shaped * envelope.ar(n, 0.0008, t60 * (0.85 + 0.15 * hardness)) * (0.6 + 0.4 * hardness)


def _ohat(params, hardness, gen):
    return _hat(params, hardness, gen, t60=0.38)


def _shaker(params, hardness, gen):
    n = samples(0.12)
    grains = filters.butter(noise.white(n, gen), "bandpass", (4500.0, 9000.0), order=2)
    return grains * envelope.ar(n, 0.007, 0.07) * (0.6 + 0.4 * hardness)


_DRUMS = {"kick": _kick, "snare": _snare, "rim": _rim, "hat": _hat, "ohat": _ohat, "shaker": _shaker}
