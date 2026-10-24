"""The call (docs/design/opening-v3-purpose.md section 2): Lumen's call sign keyed by the Ark IV sweep beside dark
Earth, three short and one long, a faint far tone in D major pentatonic heard from minute 1. Seamless loops; the game
breaks it up on reserve, adds the static while it is only Locked, and swells it as 07 faces the light. Answered, relay.1
keys the same sign back a fourth below. As the sender's name decodes, one soft note per character climbs the
pentatonic and lands on D when ARK IV is whole. 07's transmission leaves the mast on a contactor and the carrier
swelling away over the rim; relay.1's answer is a far clunk and one swell of the reply. Never a beep: every tone
is bowed metal carried onto its note, band-limited like a far radio, glassy and far."""
import numpy as np

from synth import filters
from synth.core import note_freq, samples

from . import dsp, parts

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
PULSE_S = 3.2
# One note per decoded character, climbing; the last always lands on D.
DECODE_NOTES = ("E4", "F#4", "A4", "B4", "D5")


def _tone(lib, gen, n: int, note: str) -> np.ndarray:
    """The bowed metal held on ``note``, band-limited around it like a far radio, peak-normalised."""
    rate = dsp.ratio_for(note, lib.f0("bowed_metal"), np.zeros(n))
    x = dsp.varispeed(lib("bowed_metal"), rate, n, float(gen.uniform(0.0, 1.0)))
    f = note_freq(note)
    return dsp.peak_normalize(filters.butter(filters.highpass(x, f * 0.6), "lowpass", f * 2.4, order=4))


def _sign(lib, gen, note: str) -> np.ndarray:
    """The call sign once on ``note``: three short and one long, each with a soft attack and release."""
    n = samples(SIGN_S)
    tone = _tone(lib, gen, n, note)
    gate = np.zeros(n)
    for start, length in SIGN:
        seg_n = samples(length)
        edge = min(0.03, length * 0.3)
        env = dsp.shape(seg_n, [(0.0, 0.0), (edge, 1.0), (length - edge, 1.0), (length, 0.0)])
        dsp.place(gate, env, start)
    return tone * gate


def carrier(lib, gen, _variant: int) -> np.ndarray:
    """The call: the sign once every 4 s on A5, far and faint, silent at the seam so it loops cleanly."""
    out = np.zeros(samples(CARRIER_LOOP_S))
    dsp.place(out, _sign(lib, gen, CALL_NOTE), LEAD_S)
    return out


def answered(lib, gen, _variant: int) -> np.ndarray:
    """The call answered: the sign on A5, then relay.1 keying it back on E5, a little further and softer."""
    out = np.zeros(samples(ANSWERED_LOOP_S))
    dsp.place(out, _sign(lib, gen, CALL_NOTE), LEAD_S)
    dsp.place(out, filters.lowpass(_sign(lib, gen, REPLY_NOTE), 2400.0), REPLY_AT_S, 0.7)
    return out


def decoded(lib, gen, variant: int) -> np.ndarray:
    """A character of the sender's name decoded: one soft, single resolving note, climbing E4 F#4 A4 B4 and landing
    on D5 as the name completes (variant k: the k-th step of the climb)."""
    n = samples(1.2)
    tone = _tone(lib, gen, n, DECODE_NOTES[variant])
    return tone * dsp.shape(n, [(0.0, 0.0), (0.12, 1.0), (0.45, 0.6), (1.2, 0.0)])


def network_pulse(lib, gen, _variant: int) -> np.ndarray:
    """07's transmission leaving the mast (NetworkPulseLaunched): the mast's contactor closing with a padded clunk,
    then the old network's carrier swelling up from D4 onto A5 and thinning out as the pulse runs away over the rim."""
    n = samples(PULSE_S)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "relay_click", 2400.0, 150.0, 0.8), 0.0, 0.4)
    dsp.place(out, parts.clip(lib, "pedal_clunk", 1200.0, 60.0, 0.9), 0.01, 0.45)
    swell_n = samples(PULSE_S - 0.05)
    semis = dsp.contour(swell_n, [(0.0, -19.0), (0.6, 0.0), (PULSE_S - 0.05, 0.0)])
    rate = dsp.ratio_for(CALL_NOTE, lib.f0("bowed_metal"), semis)
    swell = dsp.varispeed(lib("bowed_metal"), rate, swell_n, 1.0)
    f = note_freq(CALL_NOTE)
    swell = dsp.peak_normalize(filters.butter(filters.highpass(swell, 220.0), "lowpass", f * 2.4, order=4))
    fade = dsp.shape(swell_n, [(0.0, 0.0), (0.25, 0.8), (0.7, 1.0), (PULSE_S - 0.05, 0.0)])
    dsp.place(out, swell * fade, 0.05, 0.6)
    return out


def relay_ack(lib, gen, _variant: int) -> np.ndarray:
    """Relay.1 answering (RelayAcked), heard across the basin: its contactor's far, dull clunk and its lamp's relay,
    then a single soft swell of the reply on E5."""
    n = samples(1.6)
    out = np.zeros(n)
    dsp.place(out, parts.clip(lib, "door_clunk", 900.0, 70.0, 0.85), 0.0, 0.4)
    dsp.place(out, parts.clip(lib, "relay_click", 1800.0, 200.0, 0.8), 0.08, 0.25)
    tone_n = samples(1.3)
    tone = _tone(lib, gen, tone_n, REPLY_NOTE)
    dsp.place(out, tone * dsp.shape(tone_n, [(0.0, 0.0), (0.15, 1.0), (0.5, 0.6), (1.3, 0.0)]), 0.25, 0.4)
    return out


# Cue id -> (recipe, variants). Every variant ships: the two loops, and one note per step of the name's climb.
SOUNDS = {
    "call_carrier": (carrier, 1),
    "call_answered": (answered, 1),
    "call_decoded": (decoded, len(DECODE_NOTES)),
    "network_pulse": (network_pulse, 1),
    "relay_ack": (relay_ack, 1),
}
