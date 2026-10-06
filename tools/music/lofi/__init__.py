"""
lofi - the radio station's composer, arranger and mix engineer (tools/music).

Theory and composition (theory, voicing, harmony, groove, melody, bassline, comping, composer) turn a TrackSpec
into a Score; instruments, mixer and master turn the Score into audio on top of the shared DSP core
`tools/audio/synth`, which this package puts on sys.path (the core's documented import convention).
"""
import sys
from pathlib import Path

_SYNTH_ROOT = str(Path(__file__).resolve().parents[2] / "audio")
if _SYNTH_ROOT not in sys.path:
    sys.path.insert(0, _SYNTH_ROOT)
