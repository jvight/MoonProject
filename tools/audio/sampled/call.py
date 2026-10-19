"""The call (docs/design/opening-v3-purpose.md section 2): Lumen's call sign keyed by the Ark IV sweep beside dark
Earth, three short and one long, a faint far tone in D major pentatonic heard from minute 1. Seamless loops; the game
breaks it up on reserve, adds the static while it is only Locked, and swells it as 07 faces the light. Answered, relay.1
keys the same sign back a fourth below. Never a beep: the tone is recorded metal or a motor carried onto its note,
band-limited like a far radio (variant 1: bowed metal, glassy; variant 2: a small motor's whine through a narrow
speaker)."""
import numpy as np

from synth import filters
from synth.core import note_freq, samples

from . import dsp

CARRIER_LOOP_S = 4.0
ANSWERED_LOOP_S = 6.0
CALL_NOTE = "A5"
REPLY_NOTE = "E5"
SHORT_S = 0.16
LONG_S = 0.7
GAP_S = 0.14
# Three short and one long, from the start of the sign (seconds).
SIGN = ((0.0, SHORT_S), (SHORT_S + GAP_S, SHORT_S), (2.0 * (SHORT_S + GAP_S), SHORT_S),
        (3.0 * (SHORT_S + GAP_S), LONG_S))
SIGN_S = 3.0 * (SHORT_S + GAP_S) + LONG_S
LEAD_S = 0.2
REPLY_AT_S = 2.8


def _tone(lib, gen, n: int, note: str, variant: int) -> np.ndarray:
    """A sustained, band-limited tone on ``note`` from a recorded source, peak-normalised."""
    if variant == 0:
        rate = dsp.ratio_for(note, lib.f0("bowed_metal"), np.zeros(n))
        x = dsp.varispeed(lib("bowed_metal"), rate, n, float(gen.uniform(0.0, 1.0)))
    else:
        rate = dsp.ratio_for(note, lib.f0("motor_whine"), np.zeros(n))
        x = dsp.varispeed(lib("motor_whine"), rate, n, float(gen.uniform(0.0, 4.0)))
    f = note_freq(note)
    return dsp.peak_normalize(filters.butter(filters.highpass(x, f * 0.6), "lowpass", f * 2.4, order=4))


def _sign(lib, gen, note: str, variant: int) -> np.ndarray:
    """The call sign once on ``note``: three short and one long, each with a soft attack and release."""
    n = samples(SIGN_S)
    tone = _tone(lib, gen, n, note, variant)
    gate = np.zeros(n)
    for start, length in SIGN:
        seg_n = samples(length)
        edge = min(0.03, length * 0.3)
        env = dsp.shape(seg_n, [(0.0, 0.0), (edge, 1.0), (length - edge, 1.0), (length, 0.0)])
        dsp.place(gate, env, start)
    return tone * gate


def carrier(lib, gen, variant: int) -> np.ndarray:
    """The call: the sign once every 4 s on A5, far and faint, silent at the seam so it loops cleanly."""
    out = np.zeros(samples(CARRIER_LOOP_S))
    dsp.place(out, _sign(lib, gen, CALL_NOTE, variant), LEAD_S)
    return out


def answered(lib, gen, variant: int) -> np.ndarray:
    """The call answered: the sign on A5, then relay.1 keying it back on E5, a little further and softer."""
    out = np.zeros(samples(ANSWERED_LOOP_S))
    dsp.place(out, _sign(lib, gen, CALL_NOTE, variant), LEAD_S)
    reply = _sign(lib, gen, REPLY_NOTE, variant)
    dsp.place(out, filters.lowpass(reply, 2400.0), REPLY_AT_S, 0.7)
    return out


# Cue id -> (recipe, variants): the tone colour's A and B for the owner's audition; picks.CALL ships one.
SOUNDS = {
    "call_carrier": (carrier, 2),
    "call_answered": (answered, 2),
}
