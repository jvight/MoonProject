"""Opening v2 base recipes (docs/design/opening-v2.md B10): the first nightfall's lamps coming on one by one (the
power-on is sampled: sampled/build.py). Physical and soft, in the R1 'Radio bench' language of the build cues
(relays, filaments, transformer windings through the warm bench chain), never a square or saw beep. Every pitch is
D major pentatonic and peaks stay at mezzo-piano. Imported by cues.py, which lists the cues.
"""
import numpy as np

from synth import instruments
from synth.core import note_freq, place, samples

from r1 import bench

# The nightfall's lamps, nearest to the base first: each one a pentatonic step higher.
BASE_LAMP_NOTES = ("D4", "E4", "F#4", "A4", "B4", "D5", "E5", "F#5")


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
