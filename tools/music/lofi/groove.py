"""
Drum grooves: a 16-step grid per bar with 16th-note swing, seeded pattern mutation, phrase-end fills and per-voice
humanisation (lazy snare, loose hats, tight kick).

Swing: inside every beat the 16ths come in pairs; the second 16th of a pair lands at `swing` of the pair's length
instead of half-way (0.5 = straight, 0.58 = a gentle lofi lilt). Eighth notes stay on the grid.
"""
from dataclasses import dataclass

STEPS_PER_BAR = 16
STEPS_PER_BEAT = 4
VOICES = ("kick", "snare", "rim", "hat", "ohat", "shaker", "snap", "swish", "thump")


def swung_beat(step, swing):
    """Position in beats (from the bar start) of 16th-step `step` (0..15) under 16th swing `swing`."""
    beat, sub = divmod(step, STEPS_PER_BEAT)
    pair, second = divmod(sub, 2)
    return beat + 0.5 * pair + 0.5 * swing * second


@dataclass(frozen=True)
class DrumHit:
    """A drum hit at `beat` (absolute, on the swung grid) shifted by `offset_s` seconds of human feel."""
    beat: float
    offset_s: float
    voice: str
    velocity: float


@dataclass(frozen=True)
class DrumStyle:
    """
    Pattern vocabulary of one track's drummer. `extras` are (voice, steps, velocity) layers added to every light or
    full bar (finger snaps on the backbeat, a brush stirring on the beats); `looseness` scales the humanisation
    spread (1 = the station's tight-but-human feel, higher = a looser jam); `fills` are the phrase-end fills the
    drummer chooses from.
    """
    name: str
    kicks: tuple
    snares: tuple
    hat_velocity: tuple
    odd_hat_rate: float
    ghost_steps: tuple
    snare_lag_ms: float
    shaker_in_full: bool
    extras: tuple = ()
    looseness: float = 1.0
    fills: tuple = ("roll", "drop", "skip")


STYLES = {s.name: s for s in (
    DrumStyle("boombap", kicks=((0, 10), (0, 7, 10), (0, 10, 13), (0, 3, 10)), snares=((4, 12),),
              hat_velocity=(0.5, 0.24, 0.72, 0.28), odd_hat_rate=0.35,
              ghost_steps=(7, 9, 15), snare_lag_ms=10.0, shaker_in_full=False),
    DrumStyle("laidback", kicks=((0, 9), (0, 6, 9), (0, 10), (0, 9, 14)), snares=((4, 12),),
              hat_velocity=(0.5, 0.22, 0.66, 0.28), odd_hat_rate=1.0,
              ghost_steps=(3, 7, 15), snare_lag_ms=16.0, shaker_in_full=True),
    DrumStyle("halftime", kicks=((0, 7), (0, 10, 11), (0, 3, 7)), snares=((8,),),
              hat_velocity=(0.55, 0.22, 0.7, 0.26), odd_hat_rate=0.5,
              ghost_steps=(5, 13, 15), snare_lag_ms=14.0, shaker_in_full=True),
    DrumStyle("brushy", kicks=((0, 10), (0, 7, 10)), snares=((4, 12),),
              hat_velocity=(0.0, 0.0, 0.55, 0.0), odd_hat_rate=0.0,
              ghost_steps=(3, 7, 11, 15), snare_lag_ms=18.0, shaker_in_full=True),
    DrumStyle("bounce", kicks=((0, 7, 10), (0, 6, 10), (0, 10, 14), (0, 3, 7, 10)), snares=((4, 12),),
              hat_velocity=(0.55, 0.2, 0.75, 0.3), odd_hat_rate=0.5,
              ghost_steps=(7, 11, 15), snare_lag_ms=6.0, shaker_in_full=True,
              extras=(("snap", (4, 12), 0.62),)),
    DrumStyle("jam", kicks=((0, 10), (0, 7, 10), (0, 9), (0, 10, 15)), snares=((4, 12),),
              hat_velocity=(0.0, 0.0, 0.0, 0.0), odd_hat_rate=0.0,
              ghost_steps=(2, 7, 10, 15), snare_lag_ms=22.0, shaker_in_full=False,
              extras=(("swish", (0, 4, 8, 12), 0.4),), looseness=1.8),
    DrumStyle("pulse", kicks=((0,), (0, 10)), snares=((8,),),
              hat_velocity=(0.0, 0.0, 0.0, 0.0), odd_hat_rate=0.0,
              ghost_steps=(), snare_lag_ms=20.0, shaker_in_full=False,
              extras=(("swish", (0, 8), 0.4),), looseness=1.4, fills=("roll", "drop")),
)}

_FEEL_MS = {"kick": (0.0, 3.0), "snare": (None, 4.0), "rim": (None, 4.0), "hat": (2.0, 5.0),
            "ohat": (2.0, 5.0), "shaker": (4.0, 6.0), "snap": (None, 3.0), "swish": (-12.0, 8.0)}


def style_named(name):
    if name not in STYLES:
        raise ValueError(f"unknown drum style {name!r}; known: {sorted(STYLES)}")
    return STYLES[name]


def bar_pattern(style, intensity, fill, rng):
    """
    One bar as [(step, voice, velocity)]. Intensity: "sparse" (rim + shaker), "light" (kick, rim, hats) or
    "full" (kick, snare, ghosts, hats, open hat). `fill` is None, "roll", "drop" or "skip".
    """
    hits = []
    kick = style.kicks[int(rng.integers(len(style.kicks)))]
    snare = style.snares[int(rng.integers(len(style.snares)))]
    if intensity == "sparse":
        hits.append((0, "kick", 0.55))
        hits.extend((s, "rim", 0.45) for s in snare)
        hits.extend((s, "shaker", (0.32, 0.16, 0.4, 0.18)[s % 4]) for s in range(STEPS_PER_BAR))
    else:
        hits.extend((s, "kick", 1.0 if s == 0 else 0.78) for s in kick)
        backbeat = "snare" if intensity == "full" else "rim"
        hits.extend((s, backbeat, 0.9 if backbeat == "snare" else 0.6) for s in snare)
        for step in range(STEPS_PER_BAR):
            velocity = style.hat_velocity[step % 4]
            plays = rng.random() < style.odd_hat_rate if step % 2 else rng.random() > 0.06
            if velocity > 0.0 and plays:
                hits.append((step, "hat", velocity))
        if intensity == "full":
            hits.extend((s, "snare", 0.18 + 0.08 * rng.random()) for s in style.ghost_steps if rng.random() < 0.35)
            if rng.random() < 0.3:
                hits = [h for h in hits if not (h[0] == 14 and h[1] == "hat")] + [(14, "ohat", 0.5)]
            if style.shaker_in_full:
                hits.extend((s, "shaker", (0.26, 0.12, 0.32, 0.14)[s % 4]) for s in range(STEPS_PER_BAR))
        for voice, steps, velocity in style.extras:
            hits.extend((s, voice, velocity) for s in steps)
    return _apply_fill(hits, fill, intensity)


def _apply_fill(hits, fill, intensity):
    if fill == "drop":
        return [h for h in hits if h[0] < 8] + [(15, "rim", 0.4)]
    if fill == "roll" and intensity != "sparse":
        kept = [h for h in hits if not (h[0] >= 13 and h[1] in ("snare", "rim"))]
        voice = "snare" if intensity == "full" else "rim"
        return kept + [(13, voice, 0.22), (14, voice, 0.3), (15, voice, 0.4)]
    if fill == "skip" and intensity != "sparse":
        kept = [h for h in hits if not (h[0] == 12 and h[1] in ("snare", "rim")) and not h[0] > 12]
        return kept + [(11, "kick", 0.7), (12, "ohat", 0.45), (14, "kick", 0.6)]
    return hits


def humanise(bar_start_beat, pattern, style, swing, rng):
    """Place a bar pattern on the swung grid with per-voice timing feel and velocity variation."""
    hits = []
    for step, voice, velocity in sorted(pattern, key=lambda h: (h[0], VOICES.index(h[1]))):
        mean_ms, sigma_ms = _FEEL_MS[voice]
        mean = style.snare_lag_ms if mean_ms is None else mean_ms
        offset = (mean + sigma_ms * style.looseness * float(rng.standard_normal())) / 1000.0
        level = min(1.0, max(0.05, velocity * (1.0 + 0.08 * float(rng.standard_normal()))))
        hits.append(DrumHit(bar_start_beat + swung_beat(step, swing), offset, voice, level))
    return hits
