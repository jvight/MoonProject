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
  vibes_note    vibraphone bar: fundamental, the bar's 4x mode dying fast, a faint 10x mode where it stays soft and
                a yarn-mallet thump; the motor tremolo is applied on the stem so every bar shares one rotor.
  felt_note     felt-hammer piano (the core's felt_piano) held for its gate, then softly damped.
  theremin_line one continuous sine voice for a whole lead part: notes that follow each other closely glide, the
                level breathes between notes and the vibrato blooms late in every note, like a gentle theremin.
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
VIBES_RING_S = 0.6
VIBES_DAMP_S = 0.12
FELT_RELEASE_S = 0.6
SOFT_PARTIAL_CEILING_HZ = 6000.0
THEREMIN_GLIDE_S = 0.12
THEREMIN_BREATH = 0.3
THEREMIN_ATTACK_S = 0.14
THEREMIN_RELEASE_S = 0.45
THEREMIN_VIBRATO_HZ = 4.6
THEREMIN_VIBRATO_CENTS = 10.0
THEREMIN_VIBRATO_DELAY_S = 0.25
THEREMIN_VIBRATO_BLOOM_S = 0.4


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


def vibes_note(pitch, velocity, gate_s, gen):
    """Vibraphone bar struck with a soft yarn mallet, ringing for `gate_s` plus a short ring-out, then damped."""
    n = samples(gate_s + VIBES_RING_S)
    freq = _detuned(midi_to_freq(pitch), float(gen.normal(0.0, 1.0)))
    sustain_t60 = 3.2 * 2.0 ** (-(pitch - 72) / 24.0)
    brightness = 0.35 + 0.65 * velocity
    tone = (instruments.partial(n, freq, 1.0, 0.002, sustain_t60)
            + instruments.partial(n, 4.0 * freq, 0.16 * brightness, 0.0015, 0.32))
    if 10.0 * freq < SOFT_PARTIAL_CEILING_HZ:
        tone += instruments.partial(n, 10.0 * freq, 0.025 * brightness, 0.001, 0.06)
    mallet_n = samples(0.006)
    mallet = filters.lowpass(noise.white(mallet_n, gen), 1600.0) * envelope.ar(mallet_n, 0.0005, 0.005)
    tone[:mallet_n] += 0.03 * velocity * mallet
    gate_n = min(n, samples(gate_s + VIBES_RING_S / 2.0))
    t = np.arange(n - gate_n) / SAMPLE_RATE
    tone[gate_n:] *= np.exp(-t / VIBES_DAMP_S)
    return envelope.fade_out(tone * velocity * 0.75, 0.02)


def felt_note(pitch, velocity, gate_s, gen):
    """Felt piano held by the damper for `gate_s` seconds; soft hits are darker, the dampers fall gently."""
    n = samples(gate_s + FELT_RELEASE_S)
    decay = 3.2 * 2.0 ** (-(pitch - 60) / 24.0)
    note = instruments.felt_piano(midi_to_freq(pitch), n / SAMPLE_RATE, gen, decay=decay,
                                  brightness=0.3 + 0.3 * velocity, attack=0.005)
    gate_n = min(n, samples(gate_s))
    t = np.arange(n - gate_n) / SAMPLE_RATE
    note[gate_n:] *= np.exp(-t / (FELT_RELEASE_S / 4.0))
    return envelope.fade_out(note * (0.25 + 0.75 * velocity ** 1.3), 0.03)


def theremin_line(notes, start_s, n, seconds_of):
    """
    Render a lead part as one continuous voice into `n` samples (`seconds_of(beat)` maps beats to seconds,
    `start_s` is the pre-roll). Notes whose gap is shorter than the release share one breath: the pitch glides
    into each note over THEREMIN_GLIDE_S and the level dips to THEREMIN_BREATH between them. Each note's vibrato
    starts late and blooms slowly.
    """
    spans = []
    for note in notes:
        start = min(n, samples(start_s + seconds_of(note.beat) + note.offset_s))
        stop = min(n, samples(start_s + seconds_of(note.end) + note.offset_s))
        spans.append((start, max(start + 1, stop), midi_to_freq(note.pitch), note.velocity))
    release_n = samples(THEREMIN_RELEASE_S)
    groups = []
    for span in spans:
        if groups and span[0] - groups[-1][-1][1] < release_n:
            groups[-1].append(span)
        else:
            groups.append([span])
    freq = np.full(n, spans[0][2] if spans else 220.0)
    level = np.zeros(n)
    bloom = np.zeros(n)
    gate = np.zeros(n)
    glide_n = samples(THEREMIN_GLIDE_S)
    delay_n = samples(THEREMIN_VIBRATO_DELAY_S)
    bloom_n = samples(THEREMIN_VIBRATO_BLOOM_S)
    for group in groups:
        for index, (start, stop, target, velocity) in enumerate(group):
            following = group[index + 1][0] if index + 1 < len(group) else min(n, stop + release_n)
            freq[start:following] = target
            if index > 0:
                count = min(glide_n, following - start)
                freq[start:start + count] = osc.glide(count, group[index - 1][2], target)
            level[start:stop] = velocity
            level[stop:following] = velocity * (THEREMIN_BREATH if index + 1 < len(group) else 1.0)
            ramp = np.clip(np.arange(max(0, stop - start - delay_n)) / bloom_n, 0.0, 1.0)
            bloom[start + delay_n:stop] = ramp * ramp * (3.0 - 2.0 * ramp)
        first, last = group[0][0], group[-1][1]
        body = envelope.adsr(last - first + release_n, THEREMIN_ATTACK_S, 0.0, 1.0, THEREMIN_RELEASE_S / 3.0,
                             gate=(last - first) / SAMPLE_RATE, shape="exp")
        end = min(n, first + body.shape[0])
        gate[first:end] = body[:end - first]
    level = filters.onepole_lp(level, 10.0) * gate
    bloom = filters.onepole_lp(bloom, 6.0)
    cents = THEREMIN_VIBRATO_CENTS * bloom * np.sin(2.0 * math.pi * THEREMIN_VIBRATO_HZ * np.arange(n) / SAMPLE_RATE)
    sung = freq * 2.0 ** (cents / 1200.0)
    tone = osc.sine(n, sung) + 0.14 * osc.sine(n, 2.0 * sung, phase=0.25) + 0.035 * osc.sine(n, 3.0 * sung)
    return tone * level * 0.6


class DrumKit:
    """
    One drummer's sounds: `variants` round-robin takes in three velocity layers per voice, so repeated hits are
    never identical samples and soft hits are also darker.
    """
    LAYERS = (0.4, 0.75)

    def __init__(self, style, gen, variants=4):
        params = KIT_PARAMS[style]
        self._takes = {}
        for voice in _DRUMS:
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
    "bounce": {"kick_top": 130.0, "kick_t60": 0.45, "click": 0.05, "snare_tone": 0.9, "snare_t60": 0.18,
               "snare_attack": 0.001, "snare_lp": 6200.0},
    "jam": {"kick_top": 105.0, "kick_t60": 0.55, "click": 0.02, "snare_tone": 0.2, "snare_t60": 0.36,
            "snare_attack": 0.011, "snare_lp": 3900.0},
    "pulse": {"kick_top": 95.0, "kick_t60": 0.8, "click": 0.01, "snare_tone": 0.15, "snare_t60": 0.4,
              "snare_attack": 0.014, "snare_lp": 3400.0},
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


def _snap(params, hardness, gen):
    """Finger snap: a short, rounded click with a little skin tone under it."""
    n = samples(0.12)
    click = filters.bandpass(noise.white(n, gen), 2300.0, 1.3) * envelope.ar(n, 0.0004, 0.045)
    skin = osc.sine(n, 1180.0) * envelope.ar(n, 0.0005, 0.02)
    return filters.lowpass(click + 0.25 * skin, 5200.0 + 800.0 * hardness)


def _swish(params, hardness, gen):
    """A brush stirring across the snare head: soft band-limited noise that swells in and fades."""
    n = samples(0.5)
    sweep = filters.butter(noise.white(n, gen), "bandpass", (1400.0, 6000.0), order=2)
    return filters.lowpass(sweep, 5500.0) * envelope.ar(n, 0.16, 0.3) * (0.5 + 0.5 * hardness)


def _thump(params, hardness, gen):
    """Someone bumping the airlock hatch: a deep muffled thud, the hatch ringing faintly on D3 and A3."""
    n = samples(1.2)
    t = np.arange(n) / SAMPLE_RATE
    body = osc.sine(n, 52.0 + 40.0 * np.exp(-t / 0.05)) * envelope.ar(n, 0.003, 0.35)
    knock_n = samples(0.03)
    knock = filters.bandpass(noise.white(knock_n, gen), 260.0, 1.1) * envelope.ar(knock_n, 0.001, 0.02)
    body[:knock_n] += 1.2 * knock
    ring = (instruments.partial(n, midi_to_freq(50), 0.1, 0.004, 0.7)
            + instruments.partial(n, midi_to_freq(57), 0.05, 0.004, 0.5))
    return filters.lowpass(body + ring, 900.0) * (0.6 + 0.4 * hardness)


# Insertion order is the kit's rendering order: new voices go last so earlier voices keep their seeded takes.
_DRUMS = {"kick": _kick, "snare": _snare, "rim": _rim, "hat": _hat, "ohat": _ohat, "shaker": _shaker,
          "snap": _snap, "swish": _swish, "thump": _thump}
