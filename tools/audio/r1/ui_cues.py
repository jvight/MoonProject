"""The CRT UI cues (Group 5).

- ui_panel_open: panel open
- ui_panel_close: panel close
- ui_cursor_move: cursor move
- ui_confirm: confirm
- ui_cancel: cancel
- ui_toast: toast notification

Each cue renders Variant A (0 = closer to C2 'Radio bench') and Variant B (1 = a bolder electro-mechanical/acoustic idea).
All UI cues are mixed as the quietest family in the R1 set, and all tonal elements sit in D major pentatonic (D E F# A B).
"""
import numpy as np

from synth import effects, envelope, filters, instruments, noise, osc
from synth.core import SAMPLE_RATE, note_freq, place, samples

from . import bench


def ui_panel_open(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI panel open: gentle drawer/card slide + warm rising two-step latch."""
    if variant == 0:
        # Variant A (C2 Radio bench): bakelite toggle release + cotton card slide + warm A4 -> D5 chime through cone.
        n = samples(0.28)
        mix = np.zeros(n)
        place(mix, 0.42 * bench.bakelite_toggle(note_freq("D4"), 0.14, gen, direction="up", decay_s=0.08, muted=True), 0)
        place(mix, 0.35 * bench.paper_slide(0.15, gen, f_start=680.0, f_end=1280.0, tone_freq=note_freq("A4")), samples(0.012))
        place(mix, 0.62 * bench.warm_chime_voice(note_freq("A4"), 0.16, gen, decay_s=0.11, attack_s=0.005), samples(0.022))
        place(mix, 0.78 * bench.warm_chime_voice(note_freq("D5"), 0.20, gen, decay_s=0.14, attack_s=0.005), samples(0.080))
        place(mix, 0.32 * bench.felt_latch(note_freq("D5"), 0.06, gen, decay_s=0.04), samples(0.155))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2650.0, wet=0.08)

    # Variant B (Bolder idea): hinged brass instrument cover release + 3 micro-detent ticks + felt-piano F#4 -> B4.
    n = samples(0.30)
    mix = np.zeros(n)
    place(mix, 0.38 * bench.relay_armature(note_freq("F#4"), 0.10, gen, decay_s=0.06, muted=True), 0)
    for idx, t0 in enumerate((0.022, 0.048, 0.074)):
        place(
            mix,
            0.25 * bench.rotary_detent(note_freq("A4"), 0.06, gen, decay_s=0.04, muted=True),
            samples(t0),
        )
    place(mix, 0.65 * instruments.felt_piano(note_freq("F#4"), 0.18, gen, decay=0.14, brightness=0.35), samples(0.026))
    place(mix, 0.82 * instruments.felt_piano(note_freq("B4"), 0.21, gen, decay=0.16, brightness=0.38), samples(0.088))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.0025, drive_db=1.8, lp_hz=2700.0, wet=0.10)


def ui_panel_close(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI panel close: card slide home + felt-damped latch + warm falling settle."""
    if variant == 0:
        # Variant A (C2 Radio bench): cotton card slide-in + D5 -> A4 warm settle + felt-damped bakelite latch.
        n = samples(0.26)
        mix = np.zeros(n)
        place(mix, 0.34 * bench.paper_slide(0.13, gen, f_start=1240.0, f_end=660.0, tone_freq=note_freq("D4")), 0)
        place(mix, 0.64 * bench.warm_chime_voice(note_freq("D5"), 0.14, gen, decay_s=0.09, attack_s=0.005), samples(0.015))
        place(mix, 0.78 * bench.warm_chime_voice(note_freq("A4"), 0.17, gen, decay_s=0.12, attack_s=0.005), samples(0.072))
        place(mix, 0.45 * bench.felt_latch(note_freq("A4"), 0.08, gen, decay_s=0.05), samples(0.102))
        place(mix, 0.36 * bench.bakelite_toggle(note_freq("A3"), 0.11, gen, direction="down", decay_s=0.07, muted=True), samples(0.110))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2550.0, wet=0.07)

    # Variant B (Bolder idea): instrument drawer sliding onto leather bumper + kalimba B4 -> F#4 wood settle.
    n = samples(0.28)
    mix = np.zeros(n)
    place(mix, 0.34 * bench.paper_slide(0.12, gen, f_start=1100.0, f_end=620.0), 0)
    place(mix, 0.66 * instruments.kalimba(note_freq("B4"), 0.15, gen, decay=0.11), samples(0.018))
    place(mix, 0.80 * instruments.kalimba(note_freq("F#4"), 0.18, gen, decay=0.13), samples(0.078))
    place(mix, 0.48 * bench.speaker_cone_thump(note_freq("D3"), 0.12, gen, decay_s=0.08, cone_weight=0.55), samples(0.105))
    place(mix, 0.42 * bench.felt_latch(note_freq("F#4"), 0.07, gen, decay_s=0.05), samples(0.110))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.0025, drive_db=1.8, lp_hz=2500.0, wet=0.08)


def ui_cursor_move(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI cursor move: tiny, non-fatiguing mechanical detent tick."""
    if variant == 0:
        # Variant A (C2 Radio bench): single delicate wafer-switch rotary detent tick + warm A4 wooden body.
        n = samples(0.075)
        mix = np.zeros(n)
        place(mix, 0.65 * bench.rotary_detent(note_freq("A4"), 0.068, gen, decay_s=0.038, muted=True), samples(0.002))
        place(mix, 0.55 * bench.felt_latch(note_freq("A4"), 0.060, gen, decay_s=0.035), samples(0.003))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.002, drive_db=1.6, lp_hz=2400.0, wet=0.04, flutter_cents=0.0)

    # Variant B (Bolder idea): micro-escapement leaf-spring tick + rosewood key-cap tap at D5.
    n = samples(0.075)
    mix = np.zeros(n)
    place(mix, 0.72 * instruments.wood_tick(note_freq("D5"), 0.062, gen, decay=0.032), samples(0.002))
    place(mix, 0.48 * bench.felt_latch(note_freq("D5"), 0.058, gen, decay_s=0.032), samples(0.002))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.002, drive_db=1.5, lp_hz=2450.0, wet=0.04, flutter_cents=0.0)


def ui_confirm(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI confirm: tactile button engage + warm two-note affirmative chime."""
    if variant == 0:
        # Variant A (C2 Radio bench): bakelite push-button engage + warm D4 -> A4 affirmative tube chime.
        n = samples(0.32)
        mix = np.zeros(n)
        place(mix, 0.45 * bench.bakelite_toggle(note_freq("D4"), 0.13, gen, direction="up", decay_s=0.08, muted=True), 0)
        place(mix, 0.68 * bench.warm_chime_voice(note_freq("D4"), 0.18, gen, decay_s=0.12, attack_s=0.004), samples(0.015))
        place(mix, 0.85 * bench.warm_chime_voice(note_freq("A4"), 0.22, gen, decay_s=0.16, attack_s=0.004), samples(0.075))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.1, lp_hz=2650.0, wet=0.09)

    # Variant B (Bolder idea): solenoid punch-latch + dual celesta/soft-bell strike (F#4 -> D5).
    n = samples(0.34)
    mix = np.zeros(n)
    place(mix, 0.40 * bench.relay_armature(note_freq("F#4"), 0.11, gen, decay_s=0.06, muted=True), 0)
    place(mix, 0.35 * bench.felt_latch(note_freq("F#4"), 0.07, gen, decay_s=0.04), samples(0.008))
    place(mix, 0.68 * instruments.soft_bell(note_freq("F#4"), 0.20, decay=0.14, attack=0.004), samples(0.018))
    place(mix, 0.84 * instruments.soft_bell(note_freq("D5"), 0.24, decay=0.18, attack=0.004), samples(0.082))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.0025, drive_db=1.8, lp_hz=2700.0, wet=0.11)


def ui_cancel(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI cancel: spring-return release toggle + warm descending settle (never a harsh buzzer)."""
    if variant == 0:
        # Variant A (C2 Radio bench): spring-return bakelite release + warm descending A4 -> D4 murmur.
        n = samples(0.30)
        mix = np.zeros(n)
        place(mix, 0.44 * bench.bakelite_toggle(note_freq("A3"), 0.13, gen, direction="down", decay_s=0.08, muted=True), 0)
        place(mix, 0.66 * bench.warm_chime_voice(note_freq("A4"), 0.15, gen, decay_s=0.10, attack_s=0.004), samples(0.015))
        place(mix, 0.82 * bench.warm_chime_voice(note_freq("D4"), 0.20, gen, decay_s=0.14, attack_s=0.005), samples(0.078))
        place(mix, 0.38 * bench.felt_latch(note_freq("D4"), 0.08, gen, decay_s=0.05), samples(0.090))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.1, lp_hz=2500.0, wet=0.08)

    # Variant B (Bolder idea): damped escapement drop + muted wooden kalimba step-down (F#4 -> D4) onto leather stop.
    n = samples(0.30)
    mix = np.zeros(n)
    place(mix, 0.38 * bench.rotary_detent(note_freq("F#4"), 0.09, gen, decay_s=0.05, muted=True), 0)
    place(mix, 0.68 * instruments.kalimba(note_freq("F#4"), 0.16, gen, decay=0.11), samples(0.015))
    place(mix, 0.84 * instruments.kalimba(note_freq("D4"), 0.20, gen, decay=0.14), samples(0.080))
    place(mix, 0.42 * bench.speaker_cone_thump(note_freq("D3"), 0.12, gen, decay_s=0.08, cone_weight=0.50), samples(0.085))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.0025, drive_db=1.8, lp_hz=2450.0, wet=0.09)


def ui_toast(variant: int, gen: np.random.Generator) -> np.ndarray:
    """UI toast notification: gentle pneumatic message-card arrival + warm bell ping."""
    if variant == 0:
        # Variant A (C2 Radio bench): pneumatic message-tube puff + relay tick + warm B4 -> F#5 valve ping.
        n = samples(0.36)
        mix = np.zeros(n)
        place(mix, 0.32 * bench.paper_slide(0.10, gen, f_start=780.0, f_end=1380.0), 0)
        place(mix, 0.36 * bench.relay_armature(note_freq("B4"), 0.10, gen, decay_s=0.06, muted=True), samples(0.018))
        place(mix, 0.66 * bench.warm_chime_voice(note_freq("B4"), 0.18, gen, decay_s=0.12, attack_s=0.005), samples(0.028))
        place(mix, 0.82 * bench.warm_chime_voice(note_freq("F#5"), 0.24, gen, decay_s=0.17, attack_s=0.005), samples(0.088))
        return bench.radio_bench_chain(mix, gen, hiss_level=0.003, drive_db=2.0, lp_hz=2750.0, wet=0.10)

    # Variant B (Bolder idea): vintage desk-telegraph marginal bell tap (A4/D5 on felt) preceded by card feed whisper.
    n = samples(0.38)
    mix = np.zeros(n)
    place(mix, 0.30 * bench.paper_slide(0.09, gen, f_start=860.0, f_end=1460.0), 0)
    place(mix, 0.38 * bench.felt_latch(note_freq("A4"), 0.07, gen, decay_s=0.04), samples(0.020))
    place(mix, 0.78 * instruments.soft_bell(note_freq("A4"), 0.30, decay=0.22, attack=0.005), samples(0.026))
    place(mix, 0.45 * instruments.glass_chime(note_freq("D5"), 0.28, decay=0.18), samples(0.032))
    return bench.radio_bench_chain(mix, gen, hiss_level=0.0025, drive_db=1.8, lp_hz=2800.0, wet=0.12)


UI_CUES = {
    "ui_panel_open": ui_panel_open,
    "ui_panel_close": ui_panel_close,
    "ui_cursor_move": ui_cursor_move,
    "ui_confirm": ui_confirm,
    "ui_cancel": ui_cancel,
    "ui_toast": ui_toast,
}
