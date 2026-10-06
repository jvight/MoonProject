"""
Jazz voicings with minimal-motion voice leading.

A voicing is an ascending tuple of MIDI pitches. Candidates are built from every pitch-class subset that keeps the
chord's guide tones, stacked in close position and in drop-2 / drop-2-4 shapes, filtered by register, span and
low-interval limits. The voice leader then picks, chord after chord, the candidate with the least total motion
(sorted pairing is the optimal L1 matching for equal voice counts), preferring a smooth top line, the register
centre and the chord's named colours.
"""
from dataclasses import dataclass
from itertools import combinations, pairwise

# Lowest pitch at which an interval of N semitones between the two lowest voices stays clear (low-interval limits).
_LOW_INTERVAL_FLOOR = ((1, 53), (2, 51), (3, 48), (4, 46), (5, 44), (6, 43))
_EXTENSIONS = (2, 5, 6, 9)


@dataclass(frozen=True)
class VoicingStyle:
    """Register and shape rules for one comping instrument."""
    voices: int
    low: int
    high: int
    max_span: int
    centre: int
    shapes: tuple
    rootless: bool


KEYS_STYLE = VoicingStyle(voices=4, low=52, high=76, max_span=16, centre=63, shapes=("close", "drop2"),
                          rootless=True)
PAD_STYLE = VoicingStyle(voices=4, low=55, high=84, max_span=22, centre=69, shapes=("drop2", "drop24"),
                         rootless=False)


def _interval_ok(lower, upper):
    gap = upper - lower
    for interval, floor in _LOW_INTERVAL_FLOOR:
        if gap == interval and lower < floor:
            return False
    return not (gap == 1 and lower < 60)


def _colour_cost(chord, subset, style):
    cost = 0.7 * len(subset - chord.tones)
    if chord.root in subset and style.rootless:
        cost += 1.5
    for interval in chord.quality.intervals:
        if interval in _EXTENSIONS and (chord.root + interval) % 12 not in subset:
            cost += 0.8
    if chord.fifth in subset:
        cost += 0.2
    return cost


def pitch_class_sets(chord, key, style):
    """Every subset of chord tones + in-key tensions that holds all guide tones, with its colour cost."""
    guide = chord.guide_tones
    pool = sorted(chord.tones | chord.available_tensions(key))
    if len(guide) > style.voices:
        raise ValueError(f"{chord.symbol} needs {len(guide)} guide tones but the style has {style.voices} voices")
    sets = []
    for combo in combinations(pool, style.voices):
        subset = frozenset(combo)
        if guide <= subset:
            sets.append((subset, _colour_cost(chord, subset, style)))
    return sets


def _close_stacks(order, low, high):
    """Close-position stacks of the given pitch-class order whose lowest note lies in [low - 12, high]."""
    stacks = []
    for start in range(low - 12, high + 1):
        if start % 12 != order[0]:
            continue
        notes = [start]
        for pc in order[1:]:
            nxt = notes[-1] + 1
            while nxt % 12 != pc:
                nxt += 1
            notes.append(nxt)
        stacks.append(tuple(notes))
    return stacks


def _shape(close, shape):
    notes = list(close)
    if shape == "drop2" or shape == "drop24":
        notes[-2] -= 12
    if shape == "drop24":
        notes[-4] -= 12
    return tuple(sorted(notes))


def _playable(voicing, style):
    if voicing[0] < style.low or voicing[-1] > style.high:
        return False
    if voicing[-1] - voicing[0] > style.max_span:
        return False
    if len(set(voicing)) != len(voicing):
        return False
    return all(_interval_ok(a, b) for a, b in pairwise(voicing))


def _semitone_cost(voicing):
    return 0.6 * sum(1 for a, b in pairwise(voicing) if b - a == 1)


def candidates(chord, key, style):
    """All playable voicings of `chord` with their static (colour + shape) cost, sorted for determinism."""
    found = {}
    for subset, colour in pitch_class_sets(chord, key, style):
        pcs = sorted(subset)
        for rotation in range(len(pcs)):
            order = pcs[rotation:] + pcs[:rotation]
            for close in _close_stacks(order, style.low, style.high):
                for shape in style.shapes:
                    voicing = _shape(close, shape)
                    if _playable(voicing, style):
                        cost = colour + _semitone_cost(voicing)
                        if voicing not in found or found[voicing] > cost:
                            found[voicing] = cost
    if not found:
        raise ValueError(f"no playable voicing for {chord.symbol} in style {style}")
    return sorted(found.items())


def motion(previous, voicing):
    """Total semitone motion between two equal-size voicings (sorted pairing = optimal L1 matching)."""
    return sum(abs(a - b) for a, b in zip(previous, voicing))


def lead_voices(chords, key, style, rng, slack=0.6, previous=None):
    """
    Voice-lead a chord sequence. Returns one voicing per chord. `slack` lets the leader pick at random among
    candidates whose cost is within `slack` of the best, so repeated progressions get fresh but still smooth voicings.
    """
    result = []
    for chord in chords:
        options = candidates(chord, key, style)
        scored = []
        for voicing, static in options:
            register = 0.6 * abs(sum(voicing) / len(voicing) - style.centre)
            if previous is None:
                cost = static + register
            else:
                cost = static + register + motion(previous, voicing) + 0.5 * abs(voicing[-1] - previous[-1])
            scored.append((cost, voicing))
        best = min(cost for cost, _ in scored)
        near = [voicing for cost, voicing in scored if cost <= best + slack]
        previous = near[int(rng.integers(len(near)))]
        result.append(previous)
    return result
