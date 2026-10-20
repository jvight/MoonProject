"""Which sampled recipe and variant each game cue plays: one line per cue, so a variant is swapped here and the cue
rebuilt (``python tools/audio/build_sfx.py --library <folder> --only <cue>``). The pick is recorded in the manifest.

The picks favour one coherent family with the softest attacks: two-syllable gestures where 07 talks back
(affirm, happy, curious), a single sung tone where it settles (proud, relieved, greeting), the low weary D for tired,
and the strain-and-choke kept for worried alone so a fault never sounds like anything else.
"""
import math
import os

import numpy as np

from synth.core import note_freq

from . import base, boot, build, call, dsp, face, rover, sky, ui, voice

LIBRARY_ENV = "MOONPROJECT_SOUND_LIBRARY"

# 07's chirp moods (ChirpMood order) -> (annunciator tone, variant).
VOICE = {
    "chirp_affirm": ("voice_ack", 1),
    "chirp_happy": ("voice_done", 1),
    "chirp_proud": ("voice_done", 0),
    "chirp_curious": ("voice_query", 1),
    "chirp_worried": ("voice_fault", 0),
    "chirp_tired": ("voice_ack", 2),
    "chirp_relieved": ("voice_done", 2),
    "chirp_greeting": ("voice_query", 0),
    "chirp_ask": ("voice_ask", 0),
}

# Build cues -> variant of its recipe in build.SOUNDS.
BUILD = {
    "build_place": 0,
    "build_snap": 0,
    "build_weld_loop": 0,
    "build_structure_complete": 0,
    "structure_power_on": 0,
}

# Cues whose every variant ships: the New Game self-test's motions (sampled/boot.py: the wheels, the sparks and the
# flickers each have their own), the base's lamps at dusk (sampled/base.py: one per lamp in the lighting order),
# 07's lamp switch (sampled/rover.py: on and off), 07's face booting (sampled/face.py: the owner's picks, with every
# stroke, text tick and load step) and the call (sampled/call.py: its two loops and each note of the name's climb).
_EVERY_VARIANT = {**boot.SOUNDS, **base.SOUNDS, **rover.SOUNDS, **face.SOUNDS, **call.SOUNDS}

# The self-test cutter's fault on its result: the short strain-and-choke, so it is 07's fault voice but quicker than
# worried's.
BOOT_FAULT = ("voice_fault", 1)

# The sky's moments -> variant of its recipe in sky.SOUNDS.
SKY = {
    "sky_diamond_ring": 0,
    "sky_dusk_swell": 0,
}

# The databank on 07's face -> variant of its recipe in ui.SOUNDS: the light relay, the tube settling on D, and the
# typewriter's single ratchet notch (warmer than the counter tick).
UI = {
    "ui_relay_click": 0,
    "ui_tube_thunk": 0,
    "ui_tape_tick": 1,
    "ui_next_line": 0,
}

# 07's eye and antenna read each syllable's pitch as 0..1 between these (log).
EYE_LOW_HZ = note_freq("D4")
EYE_HIGH_HZ = note_freq("E6")

_LIBRARY = []


def library_folder() -> str:
    """The sound library folder from the environment, or empty when it is not on this machine."""
    return os.environ.get(LIBRARY_ENV, "")


def _library() -> dsp.Library:
    if not _LIBRARY:
        folder = library_folder()
        if not folder:
            raise RuntimeError(f"sampled cues need the sound library: pass --library or set {LIBRARY_ENV}")
        _LIBRARY.append(dsp.Library(folder))
    return _LIBRARY[0]


def label(cue_id: str) -> str:
    """The recorded pick, e.g. ``voice_ack v2``."""
    if cue_id in VOICE:
        tone, variant = VOICE[cue_id]
        return f"{tone} v{variant + 1}"
    if cue_id == "boot_fault":
        return f"{BOOT_FAULT[0]} v{BOOT_FAULT[1] + 1}"
    if cue_id in _EVERY_VARIANT:
        return cue_id
    if cue_id in SKY:
        return f"{cue_id} v{SKY[cue_id] + 1}"
    if cue_id in UI:
        return f"{cue_id} v{UI[cue_id] + 1}"
    return f"{cue_id} v{BUILD[cue_id] + 1}"


def renderer(cue_id: str):
    """A ``render(variant, gen)`` for cues.py that plays the pick (a sampled cue has one variant)."""
    if cue_id in VOICE:
        tone, pick = VOICE[cue_id]

        def render(_variant, gen):
            return voice.render(_library(), gen, tone, pick)

        render.__doc__ = f"07's voice, {cue_id}: {voice.GESTURES[tone][pick].description}"
        return render
    if cue_id == "boot_fault":
        tone, pick = BOOT_FAULT

        def render(_variant, gen):
            return voice.render(_library(), gen, tone, pick)

        render.__doc__ = f"The self-test's fault result: {voice.GESTURES[tone][pick].description}"
        return render
    if cue_id in _EVERY_VARIANT:
        step, _ = _EVERY_VARIANT[cue_id]

        def render(variant, gen):
            return step(_library(), gen, variant)

        render.__doc__ = step.__doc__
        return render
    if cue_id in SKY:
        fn, _ = sky.SOUNDS[cue_id]
        pick = SKY[cue_id]
    elif cue_id in UI:
        fn, _ = ui.SOUNDS[cue_id]
        pick = UI[cue_id]
    else:
        fn, _ = build.SOUNDS[cue_id]
        pick = BUILD[cue_id]

    def render(_variant, gen):
        return fn(_library(), gen, pick)

    render.__doc__ = fn.__doc__
    return render


def eye_pitch(note: str) -> float:
    """0 .. 1 where a syllable's tone sits between D4 and E6 (log), for 07's eye and antenna."""
    return float(np.clip(math.log(note_freq(note) / EYE_LOW_HZ) / math.log(EYE_HIGH_HZ / EYE_LOW_HZ), 0.0, 1.0))


def chirp_bank_data() -> dict:
    """The syllable data the game reads (tools/audio/chirp_syllables.json), from the picked gestures."""
    moods = []
    for cue_id, (tone, pick) in VOICE.items():
        syllables = voice.syllable_data(tone, pick)
        moods.append({"mood": cue_id.removeprefix("chirp_"), "cue": cue_id, "variants": [{
            "onsets": [s[0] for s in syllables],
            "pitches": [round(eye_pitch(s[1]), 4) for s in syllables],
            "lengths": [s[2] for s in syllables],
        }]})
    return {"version": 1, "moods": moods}
