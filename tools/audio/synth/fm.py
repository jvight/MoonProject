"""FM (phase modulation) operators, DX7 style: an operator's output is added to another operator's phase."""
import math

import numpy as np

from .core import TWO_PI
from .osc import phase_cycles

_FEEDBACK_TOLERANCE = 1e-10
_FEEDBACK_MAX_ITERATIONS = 400
_FEEDBACK_ITERATIVE_LIMIT = 0.95


def _self_feedback(theta: np.ndarray, beta: float) -> np.ndarray:
    """Solves y[n] = sin(theta[n] + beta * (y[n-1] + y[n-2]) / 2) (DX7's two-sample averaged feedback).

    For beta < 0.95 a vectorised fixed-point iteration converges geometrically (each pass shrinks the error
    by <= beta); stronger feedback falls back to a per-sample loop."""
    if beta < _FEEDBACK_ITERATIVE_LIMIT:
        y = np.sin(theta)
        for _ in range(_FEEDBACK_MAX_ITERATIONS):
            fb = np.zeros_like(y)
            fb[1:] += y[:-1]
            fb[2:] += y[:-2]
            nxt = np.sin(theta + 0.5 * beta * fb)
            done = float(np.max(np.abs(nxt - y))) < _FEEDBACK_TOLERANCE
            y = nxt
            if done:
                break
        return y
    y = np.empty_like(theta)
    p1 = p2 = 0.0
    for i, th in enumerate(theta.tolist()):
        v = math.sin(th + 0.5 * beta * (p1 + p2))
        y[i] = v
        p2, p1 = p1, v
    return y


def operator(n: int, freq, modulation: np.ndarray | None = None, index=1.0, amp: np.ndarray | None = None,
             feedback: float = 0.0, phase: float = 0.0) -> np.ndarray:
    """Sine operator.

    ``freq``: Hz, scalar or per-sample. ``modulation``: a signal (usually another operator's output) added to the
    phase after scaling by ``index`` (scalar or per-sample, radians per unit). ``amp``: optional output
    envelope. ``feedback``: self-modulation amount (0 = pure sine, ~1 = bright saw-like).
    A whole-cycle frequency modulated by a periodic modulator stays exactly periodic (loop-safe).
    """
    theta = TWO_PI * phase_cycles(n, freq, phase)
    if modulation is not None:
        theta = theta + np.asarray(index) * modulation[:n]
    y = _self_feedback(theta, float(feedback)) if feedback else np.sin(theta)
    return y if amp is None else y * amp[:n]


def two_op(n: int, carrier, ratio: float, index, amp: np.ndarray | None = None, modulator_feedback: float = 0.0,
           phase: float = 0.0) -> np.ndarray:
    """Classic 2-operator FM: a modulator at ``carrier * ratio`` drives the carrier with ``index`` (scalar or
    per-sample envelope, radians). ``carrier`` may be a scalar or per-sample frequency."""
    mod_freq = np.asarray(carrier, dtype=np.float64) * ratio if np.ndim(carrier) else carrier * ratio
    modulator = operator(n, mod_freq, feedback=modulator_feedback, phase=phase)
    return operator(n, carrier, modulation=modulator, index=index, amp=amp, phase=phase)
