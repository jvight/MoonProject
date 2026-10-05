"""Small, gentle instruments built from the primitives: kalimba, glass chime, soft bell, soft pad, noise grain.

All start from silence with a smooth attack and keep their spectrum soft (no partial above ~7 kHz for notes up
to D6), as the game's "nothing harsh" rule requires. Each returns a mono array of ``duration`` seconds.
"""
import numpy as np

from . import envelope, filters, noise, osc
from .core import SAMPLE_RATE, place, samples


def partial(n: int, freq: float, amp: float, attack: float, t60: float, detune_cents: float = 0.0) -> np.ndarray:
    """One enveloped sine partial (silent if it would exceed 0.45 x sample rate)."""
    f = freq * 2.0 ** (detune_cents / 1200.0)
    if f >= 0.45 * SAMPLE_RATE or amp == 0.0:
        return np.zeros(n)
    return amp * osc.sine(n, f) * envelope.ar(n, attack, t60)


def kalimba(freq: float, duration: float, gen: np.random.Generator, decay: float = 1.6, tine: float = 0.10,
            warmth: float = 0.05, attack: float = 0.0015) -> np.ndarray:
    """Kalimba-like tine: fundamental with a tiny downward pitch settle, the tine's inharmonic second mode
    (6.27 x, short), a faint octave and a soft felt-pick noise transient."""
    n = samples(duration)
    settle = freq * (1.0 + 0.004 * np.exp(-np.arange(n) / (0.02 * SAMPLE_RATE)))
    out = osc.sine(n, settle) * envelope.ar(n, attack, decay)
    out += partial(n, 2.0 * freq, warmth, attack, decay * 0.35)
    out += partial(n, 6.27 * freq, tine, attack * 0.5, 0.09)
    click_n = min(n, samples(0.004))
    click = filters.bandpass(noise.white(click_n, gen), 1800.0, 0.8) * envelope.ar(click_n, 0.0003, 0.003)
    out[:click_n] += 0.04 * click
    return out


def glass_chime(freq: float, duration: float, decay: float = 1.2, attack: float = 0.0012,
                shimmer_cents: float = 2.0) -> np.ndarray:
    """Glassy chime: bell-like inharmonic partials (2.76 x, 5.40 x) dying fast over a pure fundamental, plus a
    slightly detuned twin that beats slowly (shimmer)."""
    n = samples(duration)
    return (partial(n, freq, 1.0, attack, decay)
            + partial(n, freq, 0.25, attack, decay * 0.8, detune_cents=shimmer_cents)
            + partial(n, 2.76 * freq, 0.20, attack, decay * 0.28)
            + partial(n, 5.40 * freq, 0.06, attack, decay * 0.09))


def soft_bell(freq: float, duration: float, decay: float = 2.0, attack: float = 0.025,
              detune_cents: float = 5.0) -> np.ndarray:
    """Round slow-attack bell: harmonic partials 1, 2, 3 plus a detuned fundamental twin (gentle beat)."""
    n = samples(duration)
    return (partial(n, freq, 1.0, attack, decay)
            + partial(n, freq, 0.5, attack, decay * 0.9, detune_cents=detune_cents)
            + partial(n, 2.0 * freq, 0.12, attack, decay * 0.4)
            + partial(n, 3.0 * freq, 0.03, attack, decay * 0.2))


def soft_pad(freq: float, duration: float, attack: float, release_t60: float, hold: float = 0.0,
             detune_cents: float = 4.0, harmonics=((1.0, 1.0), (2.0, 0.18), (3.0, 0.05))) -> np.ndarray:
    """Warm sustained tone: two slightly detuned additive voices under a smooth swell, ``hold`` seconds at full
    level, then an exponential release (T60). The tail is faded to exactly zero."""
    n = samples(duration)
    a_n, h_n = min(n, samples(attack)), samples(hold)
    env = np.concatenate((envelope.curve(a_n, "smooth"), np.ones(min(h_n, n - a_n)),
                          envelope.exp_decay(max(0, n - a_n - h_n), release_t60)))
    up = 2.0 ** (detune_cents / 2400.0)
    voices = osc.additive(n, freq * up, harmonics) + osc.additive(n, freq / up, harmonics, phase=0.37)
    return envelope.fade_out(0.5 * voices * env, min(0.5, duration * 0.1))


def grain(gen: np.random.Generator, length: float, attack: float, t60: float) -> np.ndarray:
    """One noise grain: white noise under a short smooth-attack exponential envelope."""
    n = max(2, samples(length))
    return noise.white(n, gen) * envelope.ar(n, attack, t60)


def scatter(n: int, gen: np.random.Generator, rate: float, make_grain, gain_fn=None, wrap: bool = True,
            jitter: float = 1.0) -> np.ndarray:
    """Sums ``make_grain()`` events at Poisson-like times (mean ``rate``/s) into ``n`` samples. ``wrap`` places
    the overflow at the start (seamless loops). ``gain_fn()`` draws a per-event gain."""
    out = np.zeros(n)
    for pos in noise.event_times(n, gen, rate, jitter):
        place(out, make_grain(), int(pos), gain_fn() if gain_fn else 1.0, wrap=wrap)
    return out
