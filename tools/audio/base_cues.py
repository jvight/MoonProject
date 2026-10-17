"""Opening v2 base recipes (docs/design/opening-v2.md B8, B10): a structure taking power for the first time and the
first nightfall's lamps coming on one by one. Physical and soft, in the R1 'Radio bench' language of the build cues
(relays, filaments, transformer windings through the warm bench chain), never a square or saw beep. Every pitch is
D major pentatonic and peaks stay at mezzo-piano. Imported by cues.py, which lists the cues.
"""
import numpy as np

from synth import envelope, filters, instruments, osc
from synth.core import note_freq, place, samples

from r1 import bench

# The nightfall's lamps, nearest to the base first: each one a pentatonic step higher.
BASE_LAMP_NOTES = ("D4", "E4", "F#4", "A4", "B4", "D5", "E5", "F#5")


def _transformer_swell(seconds: float, start: str, end: str, swell_s: float) -> np.ndarray:
    """A transformer's windings taking load: a low hum gliding up from ``start`` to ``end`` under a slow swell."""
    n = samples(seconds)
    pitch = osc.glide(n, note_freq(start), note_freq(end), time_constant=swell_s * 0.35)
    hum = osc.additive(n, pitch, [(1, 1.0), (2, 0.42), (3, 0.16), (4, 0.06)])
    env = envelope.segments(n, [(0.0, 0.0), (swell_s, 1.0), (swell_s + 0.5, 0.8), (seconds, 0.0)], shape="smooth")
    return filters.lowpass(hum * env, 900.0)


def structure_power_on(_variant, gen):
    """Power reaching a structure: the contactor's soft relay thunk, the transformer swelling from D2 up onto A2,
    and its lamp blooming on a warm D5 with a breath of A4 (at most three layers, a slow 1 s swell)."""
    n = samples(3.0)
    mix = np.zeros(n)
    place(mix, 0.6 * bench.relay_armature(note_freq("D3"), 0.3, gen, double=True, decay_s=0.16), 0)
    place(mix, 0.45 * bench.speaker_cone_thump(note_freq("D2"), 0.4, gen, decay_s=0.3), samples(0.01))
    place(mix, 0.5 * _transformer_swell(2.6, "D2", "A2", 1.0), samples(0.05))
    lamp = (0.55 * bench.warm_chime_voice(note_freq("D5"), 1.8, gen, decay_s=1.3, attack_s=0.02)
            + 0.3 * instruments.soft_pad(note_freq("A4"), 1.8, attack=0.3, release_t60=1.2))
    place(mix, lamp, samples(1.0))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.004, drive_db=2.4, lp_hz=2800.0, room=0.4, wet=0.18,
                                   flutter_cents=0.0)


def base_lamp_on(variant, gen):
    """One base lamp coming on at nightfall: a muted filament switch, then the bulb's warm glow blooming on its
    pentatonic step (a soft felt chime over a slow pad), so a row of them climbs gently into the dark."""
    hz = note_freq(BASE_LAMP_NOTES[variant])
    n = samples(1.8)
    mix = np.zeros(n)
    place(mix, 0.5 * bench.bakelite_toggle(hz, 0.16, gen, direction="up", decay_s=0.1, muted=True), 0)
    place(mix, 0.5 * bench.warm_chime_voice(hz, 1.5, gen, decay_s=1.1, attack_s=0.015), samples(0.04))
    place(mix, 0.22 * instruments.soft_pad(hz / 2.0, 1.6, attack=0.25, release_t60=0.9), samples(0.04))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2700.0, room=0.42, wet=0.2,
                                   flutter_cents=0.0)


def title_crt_off(_variant, gen):
    """The title's CRT switching off (docs/design/opening-cutscene-research.md Q1/Q3): a soft tube "tock" (the
    power switch through a bakelite case), the picture's charge collapsing as a faint falling breath, and a short
    warm ring on D4 that the wake's D-A-E ghost chord continues. No whine, no beep."""
    n = samples(1.4)
    mix = np.zeros(n)
    place(mix, 0.7 * bench.bakelite_toggle(note_freq("D4"), 0.2, gen, direction="down", decay_s=0.12, muted=True), 0)
    place(mix, 0.5 * bench.speaker_cone_thump(note_freq("D3"), 0.35, gen, decay_s=0.22), samples(0.005))
    collapse_n = samples(0.6)
    pitch = osc.glide(collapse_n, note_freq("A5"), note_freq("D4"), time_constant=0.12)
    breath = osc.additive(collapse_n, pitch, [(1, 1.0), (2, 0.2)]) * envelope.ar(collapse_n, 0.004, 0.45)
    place(mix, 0.08 * filters.lowpass(breath, 2200.0), samples(0.01))
    place(mix, 0.3 * bench.warm_chime_voice(note_freq("D4"), 1.2, gen, decay_s=0.9, attack_s=0.01), samples(0.03))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2600.0, room=0.35, wet=0.14,
                                   flutter_cents=0.0)
