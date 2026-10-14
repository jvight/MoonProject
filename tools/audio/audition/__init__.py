"""Audition synthesis directions (A "Workshop", B "Wood & Felt", C "Old Radio") for UI cues and 07's chirps."""
from . import old_radio, wood_felt, workshop
from .render_audition import (
    AUDITION_CUES,
    DIRECTIONS,
    AuditionCue,
    finish_audition_signal,
    render_all,
    render_current_cue,
)

__all__ = [
    "AUDITION_CUES",
    "AuditionCue",
    "DIRECTIONS",
    "finish_audition_signal",
    "old_radio",
    "render_all",
    "render_current_cue",
    "wood_felt",
    "workshop",
]
