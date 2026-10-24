"""Audition candidates for the CRT UI cues (R1's "Radio bench", the owner's pick, "not quite happy yet"): the same
gestures played on real parts instead of synthesised toggles and chimes. Each cue has two variants: v1 keeps the
shipped gesture closest (a real switch or latch, then the same notes), v2 is quieter and more mechanical (the part
does most of the talking, one soft ring under it). Every ring is struck metal kept in tune on D major pentatonic,
band-limited like a small speaker on a radio bench.

Not wired into the game: render_audition.py --group ui_bench writes these next to the shipped R1 files
(``<cue>_current.ogg``) for the owner to choose by ear; the pick then replaces the R1 cue.
"""
import numpy as np

from synth import filters
from synth.core import samples

from . import dsp, parts

# The shipped R1 file each candidate is heard against (repo-relative).
SHIPPED = "Assets/_Project/Audio/SFX/R1/{cue}.wav"
# The radio bench's small speaker: nothing under or over this band.
BENCH_LOW_HZ = 150.0
BENCH_HIGH_HZ = 2800.0
ONSET_LEAD_S = 0.004


def _part(lib, name: str, seconds: float, rate: float = 1.0) -> np.ndarray:
    """``seconds`` of source ``name`` from just before its first strike, eased out at the end, peak-normalised."""
    x = parts.clip(lib, name, BENCH_HIGH_HZ, BENCH_LOW_HZ, rate)
    peak = float(np.max(np.abs(x)))
    start = max(0, int(np.argmax(np.abs(x) > 0.3 * peak)) - samples(ONSET_LEAD_S))
    n = samples(seconds)
    part = dsp.fit(x[start:], n)
    return part * dsp.shape(n, [(0.0, 1.0), (0.7 * seconds, 1.0), (seconds, 0.0)])


def _ring(lib, note: str, seconds: float) -> np.ndarray:
    """A short ring of struck metal on ``note``, kept in tune."""
    return parts.in_tune(lambda cents: parts.ring(lib, note, seconds, cents), note)


def _bench(x: np.ndarray) -> np.ndarray:
    """Through the bench's small speaker."""
    return filters.butter(filters.highpass(x, BENCH_LOW_HZ), "lowpass", BENCH_HIGH_HZ, order=4)


def _gesture(seconds: float, layers) -> np.ndarray:
    """``layers`` of ``(sound, at seconds, gain)`` laid into ``seconds``, through the bench speaker."""
    out = np.zeros(samples(seconds))
    for sound, at, gain in layers:
        dsp.place(out, sound, at, gain)
    return _bench(out)


def cursor_move(lib, gen, variant: int) -> np.ndarray:
    """The cursor moving: v1, one detent of the radio's mode wheel; v2, the fan dial's lighter click with a breath of
    A5 under it."""
    if variant == 0:
        return _gesture(0.075, [(_part(lib, "radio_wheel", 0.07), 0.0, 0.8)])
    return _gesture(0.09, [(_part(lib, "fan_dial", 0.07, 1.1), 0.0, 0.7), (_ring(lib, "A5", 0.08), 0.004, 0.15)])


def confirm(lib, gen, variant: int) -> np.ndarray:
    """Confirming: v1, the radio's power button pressed home and metal ringing D4 then A4; v2, a vintage double click
    with a single soft ring on D5."""
    if variant == 0:
        return _gesture(0.34, [(_part(lib, "radio_power", 0.12), 0.0, 0.7), (_ring(lib, "D4", 0.2), 0.015, 0.35),
                               (_ring(lib, "A4", 0.24), 0.075, 0.4)])
    return _gesture(0.3, [(_part(lib, "vintage_click", 0.15), 0.0, 0.8), (_ring(lib, "D5", 0.25), 0.03, 0.25)])


def cancel(lib, gen, variant: int) -> np.ndarray:
    """Backing out: v1, the lamp switch springing back and metal settling A4 then D4; v2, a light latch letting go
    and one low ring on D4."""
    if variant == 0:
        return _gesture(0.32, [(_part(lib, "lamp_switch", 0.14), 0.0, 0.7), (_ring(lib, "A4", 0.18), 0.015, 0.3),
                               (_ring(lib, "D4", 0.22), 0.078, 0.35)])
    return _gesture(0.3, [(_part(lib, "latch_thunk", 0.15, 1.2), 0.0, 0.7), (_ring(lib, "D4", 0.26), 0.02, 0.25)])


def toast(lib, gen, variant: int) -> np.ndarray:
    """A notice arriving: v1, a small relay and metal ringing B4 then F#5; v2, the tape measure's spring catching and
    one ring on A4."""
    if variant == 0:
        return _gesture(0.38, [(_part(lib, "relay_click", 0.1), 0.0, 0.6), (_ring(lib, "B4", 0.2), 0.028, 0.3),
                               (_ring(lib, "F#5", 0.26), 0.088, 0.35)])
    return _gesture(0.36, [(_part(lib, "tape_spring", 0.12), 0.0, 0.5), (_ring(lib, "A4", 0.3), 0.03, 0.3)])


def panel_open(lib, gen, variant: int) -> np.ndarray:
    """A panel opening: v1, an instrument case's latch lifting and metal rising A4 then D5; v2, the typewriter
    carriage sliding a notch and one ring on D5."""
    if variant == 0:
        return _gesture(0.3, [(_part(lib, "case_latch", 0.16, 1.1), 0.0, 0.7), (_ring(lib, "A4", 0.16), 0.022, 0.3),
                              (_ring(lib, "D5", 0.2), 0.08, 0.35)])
    return _gesture(0.3, [(_part(lib, "typewriter_carriage", 0.16), 0.0, 0.6), (_ring(lib, "D5", 0.22), 0.05, 0.25)])


def panel_close(lib, gen, variant: int) -> np.ndarray:
    """A panel closing: v1, the case's latch shutting, slowed, and metal falling D5 then A4; v2, a padded clunk and
    one low ring on D4."""
    if variant == 0:
        return _gesture(0.3, [(_part(lib, "case_latch", 0.16, 0.9), 0.0, 0.7), (_ring(lib, "D5", 0.16), 0.02, 0.3),
                              (_ring(lib, "A4", 0.2), 0.08, 0.35)])
    return _gesture(0.3, [(_part(lib, "pedal_clunk", 0.16, 1.1), 0.0, 0.6), (_ring(lib, "D4", 0.24), 0.02, 0.25)])


# Shipped R1 cue id -> (candidate recipe, variants).
SOUNDS = {
    "ui_cursor_move": (cursor_move, 2),
    "ui_confirm": (confirm, 2),
    "ui_cancel": (cancel, 2),
    "ui_toast": (toast, 2),
    "ui_panel_open": (panel_open, 2),
    "ui_panel_close": (panel_close, 2),
}
