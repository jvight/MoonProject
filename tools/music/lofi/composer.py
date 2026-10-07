"""
TrackSpec -> Score. Harmony first (section phrases with colour substitutions), then voicings voice-led through the
whole song, then drums, bass locked to the kick, keys, pad and the lead melody, and finally the master low-pass
automation. Every random choice draws from `synth.rng(seed, <purpose>, <index>)`, so a spec always produces the
same score and changing one part never reshuffles another.

A looping tape (ending "loop") is composed as a circle: the last bar's bass approaches the first chord and the last
section's fill sets up the first section, exactly as if the form came round again.
"""
import synth

from .bassline import add_slides, bar_notes, kick_steps_for
from .comping import keys_part, pad_part
from .groove import DrumHit, bar_pattern, humanise, style_named
from .harmony import BEATS_PER_BAR, family_named, phrase_bars
from .jingle import QUOTES
from .melody import LEADS, quote_events, remember, section_melody
from .score import ChordEvent, Score, SectionSpan
from .theory import key_named
from .voicing import KEYS_STYLE, PAD_STYLE, lead_voices

MIN_CUTOFF_HZ = 320.0
TAIL_BEATS = 4.0


def cutoff_hz(opening, warmth_hz):
    """Openness 0..1 -> master low-pass frequency, exponential between MIN_CUTOFF_HZ and the track's warmth."""
    return MIN_CUTOFF_HZ * (warmth_hz / MIN_CUTOFF_HZ) ** opening


def _fill_for(plan, bar, next_plan, style, rng):
    last = bar == plan.bars - 1
    if last and next_plan is not None and next_plan.drums_at(0) == "off":
        return "drop"
    if last:
        return style.fills[int(rng.integers(len(style.fills)))]
    if bar % 4 == 3 and rng.random() < 0.3:
        return "roll"
    return None


def _harmony(spec, key):
    family = family_named(spec.family)
    bars = []
    for index, plan in enumerate(spec.form):
        bars.extend(phrase_bars(family, key, plan.phrases, synth.rng(spec.seed, "harmony", index),
                                spec.mood.substitution))
    slots = []
    beat = 0.0
    for bar in bars:
        for slot in bar:
            slots.append((beat, slot))
            beat += slot.beats
    chords = [s.chord for _, s in slots]
    keys_voicings = lead_voices(chords, key, KEYS_STYLE, synth.rng(spec.seed, "keys-voicing"))
    pad_voicings = lead_voices(chords, key, PAD_STYLE, synth.rng(spec.seed, "pad-voicing"))
    events = [ChordEvent(b, s.beats, s.chord, kv, pv)
              for (b, s), kv, pv in zip(slots, keys_voicings, pad_voicings)]
    return bars, events


def _lead(spec, plan, start, score, key, motifs, rng):
    """The section's lead notes: a verbatim quote, or a melody on its theme (seeded by a quote when the spec says
    so, otherwise invented by the theme's first section)."""
    voice = LEADS[plan.lead]
    if plan.quote is not None:
        return quote_events(start, spec.swing, score.timeline, QUOTES[plan.quote], rng)
    seeds = dict(spec.themes)
    if plan.theme not in motifs and plan.theme in seeds:
        motifs[plan.theme] = remember(QUOTES[seeds[plan.theme]], key, voice)
    notes, motif = section_melody(start, plan.bars, spec.swing, score.timeline, key, voice, plan.density, rng,
                                  motifs.get(plan.theme))
    motifs.setdefault(plan.theme, motif)
    return notes


def compose(spec):
    spec.validate()
    key = key_named(spec.key)
    style = style_named(spec.drums)
    bars, chord_events = _harmony(spec, key)
    score = Score(spec=spec, sections=[], chords=chord_events)
    score.end_beat = spec.music_beats + TAIL_BEATS
    parts = {"keys": [], "pad": [], "bass": []}
    motifs = {}
    bar_index = 0
    bass_previous = 38
    loop = spec.ending == "loop"
    for section_index, plan in enumerate(spec.form):
        start = bar_index * BEATS_PER_BAR
        end = start + plan.bars * BEATS_PER_BAR
        score.sections.append(SectionSpan(plan.name, start, plan.bars, plan))
        next_plan = spec.form[section_index + 1] if section_index + 1 < len(spec.form) else None
        if next_plan is None and loop:
            next_plan = spec.form[0]
        drum_rng = synth.rng(spec.seed, "drums", section_index)
        bass_rng = synth.rng(spec.seed, "bass", section_index)
        bass_notes = []
        for bar in range(plan.bars):
            bar_start = (bar_index + bar) * BEATS_PER_BAR
            level = plan.drums_at(bar)
            pattern = []
            if level != "off":
                pattern = bar_pattern(style, level, _fill_for(plan, bar, next_plan, style, drum_rng), drum_rng)
                score.drums.extend(humanise(bar_start, pattern, style, spec.swing, drum_rng))
            if plan.bass != "off":
                following = bars[bar_index + bar + 1][0].chord.bass if bar_index + bar + 1 < len(bars) else None
                if following is None and loop:
                    following = bars[0][0].chord.bass
                notes, bass_previous = bar_notes(bar_start, bars[bar_index + bar], kick_steps_for(pattern),
                                                 plan.bass, spec.swing, key, following, bass_previous, bass_rng)
                bass_notes.extend(notes)
        parts["bass"].extend(add_slides(bass_notes, bass_rng))
        section_chords = [c for c in chord_events if start <= c.beat < end]
        if plan.keys != "off":
            parts["keys"].extend(keys_part(section_chords, plan.keys, BEATS_PER_BAR, spec.swing,
                                           synth.rng(spec.seed, "keys", section_index)))
        if plan.pad:
            parts["pad"].extend(pad_part(section_chords))
        if plan.lead is not None:
            notes = _lead(spec, plan, start, score, key, motifs, synth.rng(spec.seed, "lead", section_index))
            parts.setdefault(f"lead.{plan.lead}", []).extend(notes)
        score.cutoff.append((start, cutoff_hz(plan.opening[0], spec.mood.warmth_hz)))
        score.cutoff.append((end, cutoff_hz(plan.opening[1], spec.mood.warmth_hz)))
        bar_index += plan.bars
    score.drums.extend(DrumHit(beat, 0.0, voice, velocity) for beat, voice, velocity in spec.events)
    score.parts = {name: notes for name, notes in parts.items() if notes}
    return score
