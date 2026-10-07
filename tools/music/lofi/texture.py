"""
Record-and-tape texture and the generic helpers the shared core does not provide yet:
  vinyl         crackle (short grains with heavy-tailed sizes), rare soft pops and a thin hiss, stereo.
  cassette      tape hiss: soft, band-limited, slowly breathing, two partly correlated channels.
  airlock       cassette hiss plus the room tone of a small metal room (air handling and a faint hum on D and A).
  airlock_room  what a single mic taped to the airlock hears: a few close metal reflections and a low-mid bloom.
  tape_stop     the tape (or turntable) slowing to a halt: a variable-speed read of the signal.
  mono_below    removes stereo difference below a frequency so the low end is mono (club/phone safe).
  narrow        folds the sides in (stereo width 0..1).
  orbit         a slow equal-power drift left and right.
  wrap_loop     folds a one-cycle render's pre-roll and tail into the cycle: the steady state of a looping tape.
  steady        runs a process on a loop as if the loop had been playing forever (dynamics without a seam).
  bed           the texture named by a track's mood, `n` samples long (seamless when `loop`).
"""
import math

import numpy as np
from synth import envelope, filters, instruments, loop, noise, osc
from synth.core import SAMPLE_RATE, midi_to_freq, samples

LOOP_SEAM_FADE_S = 1.0
STEADY_WARMUP_S = 2.0
STEADY_LOOKAHEAD_S = 0.05
AIRLOCK_REFLECTIONS = ((0.0071, 0.55, 0), (0.0113, 0.45, 1), (0.0167, 0.38, 0), (0.0239, 0.3, 1), (0.031, 0.22, 0),
                       (0.037, 0.18, 1))
AIRLOCK_REFLECTION_LEVEL = 0.35
AIRLOCK_BLOOM = (230.0, 2.5, 1.2)


def _vinyl_channel(n, gen, crackle_rate):
    crackle = instruments.scatter(
        n, gen, crackle_rate,
        lambda: instruments.grain(gen, float(gen.uniform(0.0003, 0.0014)), 0.00005, 0.0007),
        gain_fn=lambda: min(1.0, 0.12 * float(gen.pareto(2.4)) + 0.02), wrap=False)
    crackle = filters.butter(crackle, "bandpass", (700.0, 6500.0), order=2)
    pops = instruments.scatter(
        n, gen, 0.3, lambda: instruments.grain(gen, 0.006, 0.0004, 0.004),
        gain_fn=lambda: float(gen.uniform(0.25, 0.6)), wrap=False)
    pops = filters.lowpass(pops, 1500.0)
    hiss = filters.butter(noise.white(n, gen), "bandpass", (1500.0, 9000.0), order=1)
    return crackle + 0.5 * pops + 0.05 * hiss


def vinyl(n, gen, crackle_rate=14.0):
    """Stereo record surface noise: two partly correlated channels."""
    a = _vinyl_channel(n, gen, crackle_rate)
    b = _vinyl_channel(n, gen, crackle_rate)
    return np.stack([0.8 * a + 0.2 * b, 0.2 * a + 0.8 * b], axis=1)


def tape_stop(x, start_s, duration_s):
    """
    Slow the transport from full speed to a halt over `duration_s` seconds starting at `start_s`; pitch and
    tempo sink together. The output keeps the input length and is silent after the stop.
    """
    n = x.shape[0]
    start = samples(start_s)
    span = min(samples(duration_s), n - start)
    speed = np.ones(n)
    u = np.arange(span) / span
    speed[start:start + span] = (1.0 - u) ** 1.7
    speed[start + span:] = 0.0
    position = np.concatenate(([0.0], np.cumsum(speed[:-1])))
    index = np.arange(n, dtype=np.float64)
    out = np.stack([np.interp(position, index, x[:, c]) for c in range(x.shape[1])], axis=1)
    fade = np.ones(n)
    tail = samples(0.35 * duration_s)
    fade[start + span - tail:start + span] = envelope.curve(tail, "smooth")[::-1]
    fade[start + span:] = 0.0
    return out * fade[:, None]


def mono_below(x, freq=150.0):
    """Mid/side: high-pass the side signal so everything below `freq` is mono."""
    mid = 0.5 * (x[:, 0] + x[:, 1])
    side = filters.butter(0.5 * (x[:, 0] - x[:, 1]), "highpass", freq, order=4)
    return np.stack([mid + side, mid - side], axis=1)


def _hiss_channel(n, gen):
    hiss = filters.butter(filters.butter(noise.white(n, gen), "highpass", 1800.0, order=1), "lowpass", 7000.0,
                          order=2)
    breath = filters.onepole_lp(noise.white(n, gen), 0.4)
    breath /= float(np.max(np.abs(breath))) or 1.0
    return hiss * (1.0 + 0.12 * breath)


def cassette(n, gen):
    """Stereo tape hiss that breathes slowly, two partly correlated channels."""
    a = _hiss_channel(n, gen)
    b = _hiss_channel(n, gen)
    return np.stack([0.75 * a + 0.25 * b, 0.25 * a + 0.75 * b], axis=1)


def airlock(n, gen):
    """Cassette hiss over the room tone of a small metal room: low air handling and a faint hum on D2 and A2."""
    air = filters.butter(noise.brown(n, gen), "lowpass", 220.0, order=2)
    air /= float(np.std(air)) or 1.0
    hum = osc.sine(n, midi_to_freq(38)) + 0.5 * osc.sine(n, midi_to_freq(45), phase=0.3)
    hiss = cassette(n, gen)
    hiss /= float(np.std(hiss)) or 1.0
    room = 1.6 * air + 0.25 * hum
    return hiss + np.stack([room, room], axis=1)


def airlock_room(x):
    """The band as heard by one mic taped to the airlock: close metal reflections and a low-mid bloom."""
    out = np.array(x, copy=True)
    for delay_s, gain, channel in AIRLOCK_REFLECTIONS:
        delay = samples(delay_s)
        source = x[:, 0] + x[:, 1]
        reflected = np.zeros(x.shape[0])
        reflected[delay:] = source[:-delay]
        out[:, channel] += AIRLOCK_REFLECTION_LEVEL * gain * filters.lowpass(reflected, 4500.0)
    freq, gain_db, q = AIRLOCK_BLOOM
    return filters.peaking(out, freq, gain_db, q)


def narrow(x, width):
    """Scale the side signal by `width` (1 = unchanged, 0 = mono)."""
    mid = 0.5 * (x[:, 0] + x[:, 1])
    side = 0.5 * (x[:, 0] - x[:, 1]) * width
    return np.stack([mid + side, mid - side], axis=1)


def orbit(x, rate_hz, depth):
    """Equal-power drift of a stereo signal: the image swings left and right `rate_hz` times a second."""
    angle = 0.25 * math.pi * (1.0 + depth * np.sin(2.0 * math.pi * rate_hz * np.arange(x.shape[0]) / SAMPLE_RATE))
    return np.stack([x[:, 0] * math.sqrt(2.0) * np.cos(angle), x[:, 1] * math.sqrt(2.0) * np.sin(angle)], axis=1)


def wrap_loop(x, start, length):
    """
    The steady state of a looping tape from one cycle's render: `x` holds the cycle from sample `start` for
    `length` samples, the pre-roll before it (early onsets of the first notes) and the decaying tail after it
    (reverb, echoes, ringing notes). The pre-roll is folded onto the cycle's end and the tail onto its start, as a
    tape going round would overlap them, so the last sample flows into the first like any other neighbours.
    """
    tail = x[start + length:]
    if start > length or tail.shape[0] > length:
        raise ValueError("the pre-roll and the tail must each be shorter than the loop")
    out = np.array(x[start:start + length], copy=True)
    out[length - start:] += x[:start]
    out[:tail.shape[0]] += tail
    return out


def steady(x, process, warmup_s=STEADY_WARMUP_S, lookahead_s=STEADY_LOOKAHEAD_S):
    """
    `process(signal) -> signal` applied to the loop `x` in its steady state: the loop's own end is prepended as
    warm-up and its start appended for look-ahead, then both are cut away. For processes whose memory is shorter
    than `warmup_s` (compressors, limiters) the result is seamless.
    """
    n = x.shape[0]
    warm, ahead = min(n, samples(warmup_s)), min(n, samples(lookahead_s))
    extended = np.concatenate((x[n - warm:], x, x[:ahead]))
    return process(extended)[warm:warm + n]


def bed(texture, n, gen, seamless):
    """The surface-noise bed named by `texture`, `n` samples; `seamless` cross-fades it into a loop."""
    make = {"vinyl": vinyl, "cassette": cassette, "airlock": airlock}[texture]
    if not seamless:
        return make(n, gen)
    fade = samples(LOOP_SEAM_FADE_S)
    return loop.crossfade_loop(make(n + fade, gen), n, fade, equal_power=True)
