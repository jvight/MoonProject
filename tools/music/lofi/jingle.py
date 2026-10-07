"""
Bell's station jingle: the call sign of Lumen Station's radio, in D major pentatonic.

The call is three notes, D5 E5 A5: up a step, then a leap to the open fifth (Bell standing up). The answer is four,
B5 A5 F#5 D5: one step higher, then home down the D major chord (the homecoming greeting). "Lumen After Dark,
Vol. 1" opens with the same seven notes as its show flourish (QUOTES["jingle"]), so the jingle Bell plays is the one
Ro's show started with.

Two renders for the audio box (render_jingle): `bell_jingle_short` (the call: Bell stands up after her repair) and
`bell_jingle_full` (call and answer: her homecoming greeting and the dial stingers). Bell sounds like an old radio
cabinet: a vibraphone bar over a soft Rhodes, a little reverb for the soft tail, wow and flutter, a soft
speaker-shaped band-pass and a faint carrier hiss. Rendered mono at the core's 48 kHz, then resampled to the
jingle's delivery rate.
"""
from dataclasses import dataclass

import numpy as np
from scipy.signal import resample_poly
from synth import analysis, effects, envelope, filters, noise
from synth.core import SAMPLE_RATE, db_to_gain, note_to_midi, place, samples

from . import analysis as music_analysis
from .instruments import rhodes_note, vibes_note
from .theory import D_MAJOR, note_name

CALL = ("D5", "E5", "A5")
ANSWER = ("B5", "A5", "F#5", "D5")
# (start, length) in sixteenth steps from the start of each phrase.
CALL_RHYTHM = ((0, 2), (2, 2), (4, 8))
ANSWER_RHYTHM = ((0, 2), (2, 2), (4, 2), (6, 10))
CALL_VELOCITY = (0.7, 0.74, 0.86)
ANSWER_VELOCITY = (0.78, 0.72, 0.74, 0.88)
# In the two-bar show quote the answer starts on the downbeat of the second bar.
QUOTE_ANSWER_STEP = 16

DELIVERY_RATE = 44100
STEP_S = 0.09
ANSWER_START_S = 0.9
LOUDNESS_LUFS = -18.0
MOMENTARY_WINDOW_S = 0.4
CEILING_DB = -1.5
UNDER_GAIN_DB = -11.0
STATIC_DB = -44.0
BAND_HZ = (260.0, 4200.0)
TREMOLO_HZ = 5.2
TREMOLO_DEPTH = 0.22
FADE_OUT_S = 0.45
# Where the melody's fundamentals live (D5..B5, a semitone of margin): the pitch detector looks only here.
MELODY_BAND_HZ = (550.0, 1050.0)


def _quote_notes():
    """The jingle as (step, length, MIDI pitch) inside a two-bar frame."""
    call = [(step, length, note_to_midi(name)) for (step, length), name in zip(CALL_RHYTHM, CALL)]
    answer = [(QUOTE_ANSWER_STEP + step, length, note_to_midi(name))
              for (step, length), name in zip(ANSWER_RHYTHM, ANSWER)]
    return tuple(call + answer)


QUOTES = {"jingle": _quote_notes()}


@dataclass(frozen=True)
class JingleSpec:
    """One jingle render: the call alone or call and answer, its length, its seed and a one-line description."""
    id: str
    answer: bool
    duration_s: float
    seed: int
    notes: str

    @property
    def melody(self):
        """[(onset seconds, note name, ring seconds, velocity)] in playing order."""
        events = []
        phrases = [(0.0, CALL, CALL_RHYTHM, CALL_VELOCITY)]
        if self.answer:
            phrases.append((ANSWER_START_S, ANSWER, ANSWER_RHYTHM, ANSWER_VELOCITY))
        for start, names, rhythm, velocities in phrases:
            for (step, length), name, velocity in zip(rhythm, names, velocities):
                events.append((start + step * STEP_S, name, length * STEP_S, velocity))
        return events


# Under the call's last note an open fifth, under the answer's last note the warm D major home chord.
CALL_BLOOM = ("D4", "A4")
HOME_BLOOM = ("D3", "A3", "F#4")

JINGLES = (
    JingleSpec("bell_jingle_short", answer=False, duration_s=1.9, seed=3107,
               notes="The call D5 E5 A5: Bell stands up after her repair."),
    JingleSpec("bell_jingle_full", answer=True, duration_s=2.95, seed=3108,
               notes="Call D5 E5 A5 and answer B5 A5 F#5 D5: Bell's homecoming greeting and the dial stingers."),
)


def jingle_by_id(jingle_id):
    for spec in JINGLES:
        if spec.id == jingle_id:
            return spec
    raise ValueError(f"unknown jingle {jingle_id!r}; known: {[j.id for j in JINGLES]}")


def _bloom(names, onset_s, ring_s, gen, out):
    for index, name in enumerate(names):
        note = rhodes_note(note_to_midi(name), 0.5, ring_s, gen)
        place(out, note, samples(onset_s + 0.012 * index), db_to_gain(UNDER_GAIN_DB))


def render_master(spec, rng):
    """The jingle at the core's 48 kHz, mono, at its loudness target; `rng(*keys)` gives seeded generators."""
    n = samples(spec.duration_s)
    bars = np.zeros(n)
    under = np.zeros(n)
    melody = spec.melody
    gen = rng("vibes")
    for index, (onset, name, ring, velocity) in enumerate(melody):
        last = index == len(melody) - 1
        gate = spec.duration_s - onset if last else ring
        place(bars, vibes_note(note_to_midi(name), velocity, gate, gen), samples(onset))
    rhodes = rng("rhodes")
    call_end = melody[len(CALL) - 1]
    _bloom(CALL_BLOOM, call_end[0], (ANSWER_START_S if spec.answer else spec.duration_s) - call_end[0], rhodes,
           under)
    if spec.answer:
        home = melody[-1]
        _bloom(HOME_BLOOM, home[0], spec.duration_s - home[0], rhodes, under)
    dry = effects.tremolo(bars, TREMOLO_HZ, TREMOLO_DEPTH) + under
    wet = effects.reverb(dry, room=0.38, damping=0.55, wet=0.2, dry=1.0, width=0.0)[:, 0]
    wobbly = effects.wow_flutter(wet, wow_cents=6.0, wow_rate=1.1, flutter_cents=2.5, flutter_rate=6.8,
                                 drift_cents=2.0, gen=rng("tape"))
    radio = filters.butter(wobbly, "bandpass", BAND_HZ, order=2)
    radio = filters.peaking(radio, 1100.0, 2.0, 0.9)
    radio = effects.saturate(radio * 1.5, 1.3) / 1.5
    carrier = filters.butter(noise.white(n, rng("static")), "bandpass", (1200.0, 5000.0), order=2)
    carrier *= envelope.segments(n, [(0.0, 0.0), (0.08, 1.0), (spec.duration_s - FADE_OUT_S, 0.6),
                                     (spec.duration_s, 0.0)], shape="linear")
    carrier_gain = db_to_gain(STATIC_DB) * float(np.sqrt(np.mean(radio ** 2))) / float(np.sqrt(np.mean(carrier ** 2)))
    out = envelope.fade_out(envelope.fade_in(radio + carrier_gain * carrier, 0.002), FADE_OUT_S)
    gain_db = LOUDNESS_LUFS - analysis.loudness_momentary_max(out, MOMENTARY_WINDOW_S)
    return effects.limiter(out * db_to_gain(gain_db), ceiling_db=CEILING_DB, lookahead=0.005, release=0.1)


def to_delivery_rate(x):
    """48 kHz -> the jingles' 44.1 kHz delivery rate (polyphase, deterministic), edges kept at exact silence."""
    resampled = resample_poly(x, DELIVERY_RATE // 300, SAMPLE_RATE // 300)
    return envelope.fade_out(envelope.fade_in(resampled, 0.001), 0.005)


def measure(x, rate, spec):
    """
    Measurements of a decoded jingle (mono `x` at `rate`) and the notes heard at each onset: loudness on the core's
    48 kHz grid, true peak, edges, high-frequency share and how much tonal energy leaves D major pentatonic.
    """
    core = music_analysis.to_core_rate(x, rate)
    stereo = np.stack([core, core], axis=1)
    melody = []
    for onset, name, _, _ in spec.melody:
        midi = music_analysis.onset_note(x, rate, onset, *MELODY_BAND_HZ)
        nearest = round(midi)
        melody.append({"note": name, "onset_s": onset, "detected": note_name(nearest),
                       "cents": 100.0 * (midi - nearest)})
    measured = {
        "duration_s": x.shape[0] / rate,
        "sample_rate": int(rate),
        "channels": 1 if x.ndim == 1 else x.shape[1],
        "loudness_momentary_lufs": analysis.loudness_momentary_max(core, MOMENTARY_WINDOW_S),
        "loudness_lufs": analysis.loudness_integrated(core),
        "true_peak_dbtp": analysis.true_peak_db(x),
        "edge_max": float(max(abs(x[0]), abs(x[-1]))),
        "above_8k_db": analysis.spectral_stats(core, 8000.0)[1],
        "chroma_out_of_pentatonic": music_analysis.chroma_out_of_key(stereo, D_MAJOR.pentatonic),
    }
    return measured, melody
