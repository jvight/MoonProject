"""
Record-and-tape texture and the two generic helpers the shared core does not provide yet:
  vinyl       crackle (short grains with heavy-tailed sizes), rare soft pops and a thin hiss, stereo.
  tape_stop   the tape (or turntable) slowing to a halt: a variable-speed read of the signal.
  mono_below  removes stereo difference below a frequency so the low end is mono (club/phone safe).
"""
import numpy as np
from synth import envelope, filters, instruments, noise
from synth.core import samples


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
