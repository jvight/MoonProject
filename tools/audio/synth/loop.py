"""Seamless-loop helpers.

* :func:`periodic` runs a time-invariant process (filters, reverb, chorus with LFO rates of k / loop length) as
  if the loop repeated forever, so the last sample flows into the first like any other pair of neighbours.
  Combine it with periodic sources (``core.loop_freq`` frequencies, events placed with
  ``core.place(..., wrap=True)``, white noise coloured inside the process) for mathematically seamless loops.
* :func:`crossfade_loop` turns any longer render into a loop by cross-fading the material after the loop end
  into the loop start - the classic technique for material that is not periodic by construction.
"""
import math

import numpy as np


def periodic(x: np.ndarray, process, warmup: int | None = None) -> np.ndarray:
    """Applies ``process(array) -> array`` to loop ``x`` in its steady state.

    ``warmup`` samples of the loop's own past (default one whole loop; repeated when longer than the loop) are
    prepended and discarded afterwards; it must exceed the process's memory (e.g. a reverb tail). Processes
    with internal LFOs stay aligned only with whole-loop warmups (the default) and rates of k / loop length.
    The output may be mono or stereo whatever the input."""
    n = x.shape[0]
    warm = n if warmup is None else int(warmup)
    repeats = -(-warm // n) if warm else 0
    history = np.concatenate([x] * repeats)[repeats * n - warm:] if warm else x[:0]
    return process(np.concatenate((history, x)))[warm:]


def crossfade_loop(x: np.ndarray, loop_length: int, fade_length: int, equal_power: bool = True) -> np.ndarray:
    """``loop_length``-sample loop from ``x`` (at least ``loop_length + fade_length`` long): the start fades in
    while the material just after the loop end fades out over it, so ``out[-1] -> out[0]`` is the original
    continuous ``x[loop_length - 1] -> x[loop_length]``. Equal-power fades suit uncorrelated material (noise),
    equal-gain (``equal_power=False``) suits correlated (tonal) material."""
    if x.shape[0] < loop_length + fade_length:
        raise ValueError("source too short for the requested loop and cross-fade")
    u = np.arange(fade_length) / fade_length
    if equal_power:
        g_in, g_out = np.sin(0.5 * math.pi * u), np.cos(0.5 * math.pi * u)
    else:
        g_in, g_out = u, 1.0 - u
    if x.ndim == 2:
        g_in, g_out = g_in[:, None], g_out[:, None]
    out = np.array(x[:loop_length], dtype=np.float64, copy=True)
    out[:fade_length] = x[:fade_length] * g_in + x[loop_length:loop_length + fade_length] * g_out
    return out


def seam_error(x: np.ndarray) -> float:
    """Second-difference (linear prediction) error across the seam, max over channels."""
    return float(np.max(np.abs(x[0] - 2.0 * x[-1] + x[-2])))
