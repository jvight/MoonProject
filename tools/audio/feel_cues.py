"""0.5.2 "Satisfying" cue recipes (docs/design/feel-pass.md, polish-pass.md section 1.1): the cut's beats, the dock's
lock and the first hour's silent events (07's voice is sampled: sampled/voice.py). Every pitch is D major
pentatonic; peaks stay at mezzo-piano and each beat has at most three layers (feel-pass R10). In vacuum, sound is
what 07's chassis feels (R11): low, close and muffled. Imported by cues.py, which lists the cues.
"""
import numpy as np

from synth import envelope, filters, instruments, noise, osc
from synth.core import loop_freq, note_freq, place, samples
from synth.loop import periodic

# ------------------------------------------------------------------------------------------------------- the cut

def _glass(freq: float, seconds: float) -> np.ndarray:
    return instruments.glass_chime(freq, seconds, decay=0.5) * 0.5


def _chassis_thump(seconds: float, hz: float = 60.0) -> np.ndarray:
    """A felt low thump through 07's body: a short sine drop, low-passed hard."""
    n = samples(seconds)
    body = osc.sine(n, osc.glide(n, hz * 1.4, hz, time_constant=0.02)) * envelope.ar(n, 0.004, seconds * 0.8)
    return filters.lowpass(body, 180.0)


CUT_SIZZLE_LOOP_S = 3.0


def cut_sizzle(_variant, gen):
    """The beam heard through 07's body while it cuts: a low-passed, close sizzle with a slow shimmer. Seamless."""
    n = samples(CUT_SIZZLE_LOOP_S)
    hiss = periodic(noise.white(n, gen), lambda x: filters.lowpass(filters.highpass(noise.pink_filter(x), 300.0),
                                                                   1400.0))
    hiss = hiss / max(float(np.std(hiss)), 1e-9)
    shimmer = envelope.lfo(n, 6.0 / CUT_SIZZLE_LOOP_S, 0.2, 0.8)
    return 0.12 * hiss * shimmer


def cut_crack(_variant, gen):
    """The piece breaking free: a soft high B pluck and a felt thud through the wreck and the chassis."""
    n = samples(1.2)
    pluck = instruments.kalimba(note_freq("B5"), 1.0, gen, decay=0.6)
    mix = np.zeros(n)
    place(mix, 0.5 * pluck, 0)
    place(mix, 0.9 * _chassis_thump(0.18, 52.0), 0)
    return filters.lowpass(mix, 5500.0)


def strap_tuk(_variant, gen):
    """A strap cinching a bundle (the cut's fold, a relic stowed in the cradle): a short muted 'tuk' on E."""
    n = samples(0.35)
    tone = osc.sine(n, osc.glide(n, note_freq("E4") * 1.08, note_freq("E4"), time_constant=0.01))
    tone *= envelope.ar(n, 0.002, 0.08)
    slap = filters.bandpass(noise.white(n, gen), 900.0, 1.4) * envelope.ar(n, 0.001, 0.03)
    return filters.lowpass(0.6 * tone + 0.12 * slap, 2500.0)


# ------------------------------------------------------------------------------------------------------- the dock

def _tick(gen, freq: float, seconds: float = 0.12) -> np.ndarray:
    return instruments.wood_tick(freq, seconds, gen, decay=0.04)


def dock_walls_rise(_variant, gen):
    """The dock's four low walls rising, staggered 60 ms: soft mechanical ticks on D-A-D-A over a short servo push."""
    n = samples(1.0)
    mix = np.zeros(n)
    for index, note in enumerate(("D4", "A4", "D5", "A4")):
        place(mix, 0.45 * _tick(gen, note_freq(note)), samples(0.06 * index + 0.38))
    push_n = samples(0.5)
    push = filters.bandpass(noise.pink_filter(noise.white(push_n, gen)), 380.0, 1.8)
    push = push / max(float(np.std(push)), 1e-9) * envelope.segments(
        push_n, [(0.0, 0.0), (0.1, 1.0), (0.4, 0.6), (0.5, 0.0)], shape="smooth")
    place(mix, 0.05 * push, 0)
    return filters.lowpass(mix, 4000.0)


def dock_walls_settle(_variant, gen):
    """Full: the walls lowering, staggered (600 ms), and settling with two soft ticks on A and D."""
    n = samples(1.2)
    mix = np.zeros(n)
    sink_n = samples(0.6)
    sink = filters.bandpass(noise.pink_filter(noise.white(sink_n, gen)), 300.0, 1.8)
    sink = sink / max(float(np.std(sink)), 1e-9) * envelope.segments(
        sink_n, [(0.0, 0.0), (0.15, 0.7), (0.6, 0.0)], shape="smooth")
    place(mix, 0.04 * sink, 0)
    place(mix, 0.35 * _tick(gen, note_freq("A3")), samples(0.62))
    place(mix, 0.3 * _tick(gen, note_freq("D3")), samples(0.72))
    return filters.lowpass(mix, 3500.0)


def dock_walls_drop(_variant, gen):
    """Driving off early: the walls drop at once, a quick soft fall and one low tick."""
    n = samples(0.5)
    fall = filters.bandpass(noise.pink_filter(noise.white(n, gen)), 260.0, 1.5)
    fall = fall / max(float(np.std(fall)), 1e-9) * envelope.segments(
        n, [(0.0, 0.0), (0.04, 0.6), (0.25, 0.0)], shape="smooth")
    mix = 0.04 * fall
    place(mix, 0.3 * _tick(gen, note_freq("D3")), samples(0.24))
    return filters.lowpass(mix, 3000.0)


# ------------------------------------------------------------------------------------- the first hour's silences


BOOST_LOOP_S = 2.0


def boost_whine(_variant, _gen):
    """The capacitor drums boosting: a soft rising-feeling whine on A with a slow shimmer. Seamless."""
    n = samples(BOOST_LOOP_S)
    a = loop_freq(note_freq("A4"), n)
    tone = osc.additive(n, a, [(1, 1.0), (2, 0.25), (4, 0.08)]) * envelope.lfo(n, 4.0 / BOOST_LOOP_S, 0.15, 0.85)
    return periodic(0.12 * tone, lambda x: filters.lowpass(x, 2500.0))


def boost_release(_variant, gen):
    """The drums emptying: a short 'fff' of air-less release (felt, not heard) and a tick on D."""
    n = samples(0.6)
    fff = filters.bandpass(noise.white(n, gen), 1500.0, 0.8) * envelope.ar(n, 0.01, 0.25)
    mix = 0.06 * fff
    place(mix, 0.3 * _tick(gen, note_freq("D5"), 0.1), samples(0.3))
    return filters.lowpass(mix, 4000.0)


def save_tick(_variant, gen):
    """The game saved: a tiny soft tick on D."""
    return filters.lowpass(0.25 * _tick(gen, note_freq("D6"), 0.15), 5000.0)


COUNT_TICK_NOTES = ("D5", "E5", "F#5", "A5", "B5", "D6")


def count_tick(variant, gen):
    """The stock counter rolling one unit (feel-pass CountTick): a soft rising pentatonic tick."""
    return filters.lowpass(0.25 * _tick(gen, note_freq(COUNT_TICK_NOTES[variant]), 0.15), 5000.0)


# ----------------------------------------------------------------------------------------- picking a part up (E)

def pickup_lift(_variant, gen):
    """07's arm lifting a part: a soft, short servo whirr rising a fourth (A2 to D3), close and muffled."""
    n = samples(0.7)
    rise = osc.glide(n, note_freq("A2"), note_freq("D3"), time_constant=0.2)
    motor = osc.additive(n, rise, [(1, 1.0), (2, 0.35), (3, 0.1)])
    whirr = filters.bandpass(noise.pink_filter(noise.white(n, gen)), 450.0, 2.0)
    whirr = whirr / max(float(np.std(whirr)), 1e-9)
    shape = envelope.segments(n, [(0.0, 0.0), (0.08, 1.0), (0.5, 0.8), (0.7, 0.0)], shape="smooth")
    return filters.lowpass((0.07 * motor + 0.035 * whirr) * shape, 2500.0)


def pickup_clunk(_variant, gen):
    """The part seating on 07's back: a solid little clunk on D, felt through the chassis."""
    n = samples(0.45)
    mix = 0.7 * np.concatenate((_chassis_thump(0.12, 85.0), np.zeros(n - samples(0.12))))
    knock = osc.sine(n, note_freq("D4")) * envelope.ar(n, 0.002, 0.07)
    tick = filters.bandpass(noise.white(n, gen), 1800.0, 1.5) * envelope.ar(n, 0.001, 0.02)
    return filters.lowpass(mix + 0.3 * knock + 0.08 * tick, 4000.0)


# ----------------------------------------------------------------------------- M3-26: the charge run and the tiers

CUTTER_TIER_LABELS = ("tier0", "tier1", "tier2", "tier3")
SEGMENT_NOTES = ("D5", "E5", "F#5", "A5", "B5")


def cut_ignite_tier(variant, gen):
    """Ignition per cutter tier (M3-26 section 5): the zing on D6 gets brighter and hotter with each tier (more
    upper partials and a touch more sheen), and the chassis thump a little deeper."""
    n = samples(0.6)
    bright = 1.0 + 0.4 * variant
    d6 = osc.glide(n, note_freq("D6") * 1.03, note_freq("D6"), time_constant=0.03)
    zing = osc.sine(n, d6) + 0.12 * variant * osc.sine(n, 2.0 * d6) + 0.05 * variant * osc.sine(n, 3.0 * d6)
    zing *= envelope.ar(n, 0.002, 0.3 + 0.05 * variant)
    sheen = filters.bandpass(noise.white(n, gen), 3000.0 + 600.0 * variant, 2.0) * envelope.ar(n, 0.001, 0.08)
    mix = 0.3 * zing + 0.04 * bright * sheen
    place(mix, 0.4 * _chassis_thump(0.12, 62.0 - 4.0 * variant), 0)
    return filters.lowpass(mix, 6000.0 + 1200.0 * variant)


CUT_UNDERTONE_LOOP_S = 4.0


def cut_undertone(_variant, _gen):
    """The beam's deeper hum for the higher tiers: a warm D2 with its fifth, breathing slowly. Seamless; the
    runtime sets its level by tier (silent at tier 0)."""
    n = samples(CUT_UNDERTONE_LOOP_S)
    tone = osc.additive(n, loop_freq(note_freq("D2"), n), [(1, 1.0), (2, 0.35)])
    tone += 0.3 * osc.sine(n, loop_freq(note_freq("A2"), n))
    tone *= envelope.lfo(n, 2.0 / CUT_UNDERTONE_LOOP_S, 0.15, 0.85)
    return periodic(0.15 * tone, lambda x: filters.lowpass(x, 400.0))


def segment_slide(variant, gen):
    """The beam moving to a piece's next cut line: a soft tick and a short upward slide onto the next pentatonic
    step, so a big plate reads as several small victories."""
    n = samples(0.6)
    target = note_freq(SEGMENT_NOTES[variant])
    slide = osc.sine(n, osc.glide(n, target * 0.94, target, time_constant=0.04)) * envelope.ar(n, 0.005, 0.3)
    mix = 0.3 * slide
    place(mix, 0.2 * _tick(gen, target, 0.1), 0)
    return filters.lowpass(mix, 5000.0)


# ------------------------------------------------------------------------------------------ M3-25: cargo and storage

def cargo_tok(_variant, gen):
    """The cargo hold is full: a soft 'tok' on its lid, a wooden knock on A3, felt more than heard."""
    n = samples(0.3)
    knock = _tick(gen, note_freq("A3"), 0.3)
    thump = np.concatenate((_chassis_thump(0.08, 110.0), np.zeros(n - samples(0.08))))
    return filters.lowpass(0.45 * knock + 0.4 * thump, 3000.0)


# ------------------------------------------------------------------------- 0.5.5 B: 07's hold boots

HOLD_SLOT_NOTES = ("D5", "E5", "F#5", "A5", "B5", "D6", "E6", "F#6")


def hold_boot(_variant, gen):
    """The first Tab press: 07's hold powering on in the dark, a soft rising D3 hum and a faint relay click."""
    n = samples(1.2)
    rise = osc.glide(n, note_freq("D3") * 0.94, note_freq("D3"), time_constant=0.25)
    hum = osc.additive(n, rise, [(1, 1.0), (2, 0.3)])
    hum *= envelope.segments(n, [(0.0, 0.0), (0.35, 0.8), (1.0, 0.6), (1.2, 0.0)], shape="smooth")
    mix = 0.15 * filters.lowpass(hum, 900.0)
    place(mix, 0.2 * _tick(gen, note_freq("A4"), 0.1), samples(0.02))
    return mix


def hold_slot(variant, gen):
    """A hold slot's outline lighting (60 ms apart, left to right): a soft tick on the next pentatonic step."""
    return filters.lowpass(0.25 * _tick(gen, note_freq(HOLD_SLOT_NOTES[variant]), 0.15), 5000.0)


def item_slide(_variant, gen):
    """A starter item sliding into its slot: a short felt slide and a seated tick on D."""
    n = samples(0.35)
    slide = filters.bandpass(noise.pink_filter(noise.white(n, gen)), 900.0, 1.4)
    slide = slide / max(float(np.std(slide)), 1e-9) * envelope.segments(
        n, [(0.0, 0.0), (0.05, 1.0), (0.2, 0.3), (0.25, 0.0)], shape="smooth")
    mix = 0.04 * slide
    place(mix, 0.25 * _tick(gen, note_freq("D5"), 0.1), samples(0.22))
    return filters.lowpass(mix, 4000.0)


# ------------------------------------------------------------------------- 0.5.5 C: seals and containers

CONTAINER_LABELS = ("cabinet", "pod", "crate", "case", "box")
CONTAINER_NOTES = {"cabinet": "A3", "pod": "D3", "crate": "E3", "case": "B3", "box": "F#3"}


def seal_cut(_variant, gen):
    """The cut finishing a container's seal: the beam's last bite, a soft pop as the seal lets go and a small ping
    in the metal on A5, all felt through the chassis (no air)."""
    n = samples(0.8)
    mix = np.zeros(n)
    bite = filters.bandpass(noise.white(samples(0.12), gen), 2200.0, 1.5) * envelope.ar(samples(0.12), 0.002, 0.05)
    place(mix, 0.1 * bite, 0)
    place(mix, 0.5 * _chassis_thump(0.1, 90.0), samples(0.08))
    ping = instruments.glass_chime(note_freq("A5"), 0.6, decay=0.4) * 0.3
    place(mix, ping, samples(0.1))
    return filters.lowpass(mix, 5000.0)


def container_open(variant, gen):
    """A container opening and its find sliding out: a low hinge groan on the container's note, a latch knock and a
    short slide. The note differs by kind (cabinet, pod, crate, case, box), all D major pentatonic."""
    note = note_freq(CONTAINER_NOTES[CONTAINER_LABELS[variant]])
    n = samples(1.1)
    hinge = osc.sine(n, osc.glide(n, note * 0.97, note, time_constant=0.2))
    hinge *= envelope.segments(n, [(0.0, 0.0), (0.1, 1.0), (0.5, 0.4), (0.7, 0.0)], shape="smooth")
    mix = 0.12 * filters.lowpass(hinge, 800.0)
    place(mix, 0.3 * _tick(gen, note * 2.0, 0.15), 0)
    slide_n = samples(0.3)
    slide = filters.bandpass(noise.pink_filter(noise.white(slide_n, gen)), 700.0, 1.4)
    slide = slide / max(float(np.std(slide)), 1e-9) * envelope.segments(
        slide_n, [(0.0, 0.0), (0.08, 1.0), (0.3, 0.0)], shape="smooth")
    place(mix, 0.03 * slide, samples(0.6))
    return filters.lowpass(mix, 4000.0)


# ---------------------------------------------------------- 0.5.5 E: the cut's charge-up has weight (section 5.5)

def charge_brace(_variant, gen):
    """07 bracing before a fresh charge: a short servo 'chunk' and the wheel-lock clack, through the chassis."""
    n = samples(0.6)
    servo_n = samples(0.25)
    servo = osc.additive(servo_n, osc.glide(servo_n, note_freq("A2"), note_freq("D3"), time_constant=0.06),
                         [(1, 1.0), (2, 0.4), (3, 0.15)])
    servo *= envelope.segments(servo_n, [(0.0, 0.0), (0.03, 1.0), (0.2, 0.7), (0.25, 0.0)], shape="smooth")
    mix = np.zeros(n)
    place(mix, 0.12 * servo, 0)
    place(mix, 0.6 * _chassis_thump(0.1, 75.0), samples(0.22))
    place(mix, 0.3 * _tick(gen, note_freq("D4"), 0.15), samples(0.4))
    return filters.lowpass(mix, 3500.0)


CHARGE_WHINE_LOOP_S = 2.0


def charge_whine(_variant, _gen):
    """The capacitor whine on D5: a soft sine with a filtered-saw edge, steady and seamless; the runtime glides its
    pitch up the stages (D -> A -> D') and back down on a drain."""
    n = samples(CHARGE_WHINE_LOOP_S)
    d5 = loop_freq(note_freq("D5"), n)
    edge = periodic(osc.saw(n, d5), lambda x: filters.lowpass(x, 1800.0))
    tone = (osc.sine(n, d5) + 0.25 * edge) * envelope.lfo(n, 6.0 / CHARGE_WHINE_LOOP_S, 0.06, 0.94)
    return periodic(0.12 * tone, lambda x: filters.lowpass(x, 3000.0))


CHARGE_HUM_LOOP_S = 3.0


def charge_hum(_variant, _gen):
    """The low hum on D under the charge, swelling with the stages (the runtime sets its level). Seamless."""
    n = samples(CHARGE_HUM_LOOP_S)
    tone = osc.additive(n, loop_freq(note_freq("D2"), n), [(1, 1.0), (2, 0.45), (3, 0.12)])
    return periodic(0.2 * tone, lambda x: filters.lowpass(x, 500.0))


def charge_detent(_variant, gen):
    """A charge stage locking (the owner's "Cells" charge-up): a capacitor cell's glass sleeve sliding forward (a
    short, smooth glassy slide), then the discrete detent click, small and mechanical."""
    n = samples(0.2)
    slide_n = samples(0.07)
    slide = filters.bandpass(noise.white(slide_n, gen), 2600.0, 3.0)
    slide *= envelope.segments(slide_n, [(0.0, 0.0), (0.03, 1.0), (0.07, 0.0)], shape="smooth")
    mix = np.zeros(n)
    place(mix, 0.06 * slide, 0)
    at = samples(0.06)
    click_n = n - at
    click = filters.bandpass(noise.white(click_n, gen), 1600.0, 2.0) * envelope.ar(click_n, 0.0008, 0.015)
    body = osc.sine(click_n, note_freq("A3")) * envelope.ar(click_n, 0.001, 0.03)
    place(mix, 0.3 * click + 0.25 * body, at)
    return filters.lowpass(filters.lowpass(mix, 4000.0), 4000.0)


def charge_lock(_variant, gen):
    """The full lock: a firm two-part 'clunk-click' felt through the chassis before the held breath."""
    n = samples(0.4)
    mix = 0.8 * np.concatenate((_chassis_thump(0.12, 80.0), np.zeros(n - samples(0.12))))
    place(mix, 0.3 * osc.sine(samples(0.12), note_freq("D3")) * envelope.ar(samples(0.12), 0.001, 0.05), 0)
    click_n = samples(0.1)
    click = filters.bandpass(noise.white(click_n, gen), 1800.0, 2.0) * envelope.ar(click_n, 0.0008, 0.02)
    place(mix, 0.35 * click, samples(0.07))
    return filters.lowpass(mix, 4000.0)


# ------------------------------------------------------------------------------------- 0.5.5 D: scanning (section 1)

SCAN_SWEEP_LOOP_S = 2.0
SCAN_STEP_NOTES = ("D5", "E5", "F#5", "A5")


def scan_sweep(_variant, gen):
    """The lens's fan of light sweeping a fragment: a soft airy tone on D5 with a slow shimmer, quiet and calm. The
    runtime steps its pitch up the pentatonic at each 25% (D -> E -> F# -> A). Seamless."""
    n = samples(SCAN_SWEEP_LOOP_S)
    d5 = loop_freq(note_freq("D5"), n)
    tone = osc.sine(n, d5) + 0.2 * osc.sine(n, 2.0 * d5)
    air = periodic(noise.white(n, gen), lambda x: filters.bandpass(noise.pink_filter(x), 2200.0, 1.2))
    air = air / max(float(np.std(air)), 1e-9)
    tone *= envelope.lfo(n, 4.0 / SCAN_SWEEP_LOOP_S, 0.15, 0.85)
    return periodic(0.08 * tone + 0.01 * air, lambda x: filters.lowpass(x, 3500.0))


def scan_step(variant, _gen):
    """A quarter of a scan drawn: a small glass note on the next step (D E F# A)."""
    return filters.lowpass(0.4 * _glass(note_freq(SCAN_STEP_NOTES[variant]), 0.5), 5500.0)


def scan_liftoff(_variant, _gen):
    """The dissolve: the line drawing unthreads and streams into 07's lens. A soft rising shimmer (D5 -> D6 glide
    under glass notes A5, D6) ending in a bright, short lens flare chime on F#6."""
    n = samples(1.3)
    glide = osc.sine(n, osc.glide(n, note_freq("D5"), note_freq("D6"), time_constant=0.25))
    glide *= envelope.segments(n, [(0.0, 0.0), (0.3, 0.6), (0.85, 0.8), (1.0, 0.0)], shape="smooth")
    mix = 0.12 * glide
    place(mix, 0.3 * _glass(note_freq("A5"), 0.6), samples(0.25))
    place(mix, 0.32 * _glass(note_freq("D6"), 0.6), samples(0.5))
    place(mix, 0.35 * _glass(note_freq("F#6"), 0.5), samples(0.8))
    return filters.lowpass(mix, 6500.0)


# --------------------------------------------------------------------- 0.5.5 D: a blueprint's sheet complete


def sheet_complete(_variant, gen):
    """A blueprint's last fragment scanned, the sheet complete: a soft, warm rising D major arpeggio (D4 F#4 A4 D5)
    on a felt piano, settling on a quiet bell D5. No glass, so it never sounds like the scan's lift-off."""
    n = samples(2.6)
    mix = np.zeros(n)
    for index, note in enumerate(("D4", "F#4", "A4", "D5")):
        place(mix, (0.3 + 0.04 * index) * instruments.felt_piano(note_freq(note), 2.0, gen, decay=1.4),
              samples(0.14 * index))
    place(mix, 0.12 * instruments.soft_bell(note_freq("D5"), 2.0, decay=1.6, attack=0.05), samples(0.56))
    return filters.lowpass(mix, 4500.0)


# ------------------------------------------------------------------- 0.5.5: the 07 tab's drag and drop (ui)

def part_pick(_variant, gen):
    """Picking a part up in the 07 tab: a light lift, a tick rising E5 -> A5, soft and tactile."""
    n = samples(0.25)
    mix = np.zeros(n)
    place(mix, 0.2 * _tick(gen, note_freq("E5"), 0.12), 0)
    place(mix, 0.22 * _tick(gen, note_freq("A5"), 0.15), samples(0.05))
    return filters.lowpass(mix, 4500.0)


def part_drop(_variant, gen):
    """Dropping a part on a valid socket: a seated tick on D5 over a small felt thud, settling."""
    n = samples(0.35)
    mix = 0.25 * np.concatenate((_chassis_thump(0.08, 110.0), np.zeros(n - samples(0.08))))
    place(mix, 0.25 * _tick(gen, note_freq("D5"), 0.2), samples(0.01))
    return filters.lowpass(mix, 4000.0)


def part_refuse(_variant, gen):
    """A drop the socket refuses: two soft, muted ticks falling B4 -> F#4, gentle, never a buzzer."""
    n = samples(0.3)
    mix = np.zeros(n)
    place(mix, 0.18 * _tick(gen, note_freq("B4"), 0.1), 0)
    place(mix, 0.18 * _tick(gen, note_freq("F#4"), 0.15), samples(0.09))
    return filters.lowpass(mix, 2500.0)


