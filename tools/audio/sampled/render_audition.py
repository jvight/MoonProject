#!/usr/bin/env python3
"""Render the sampled sound set for an A/B listening pass, before anything is wired into the game.

Writes, into the output folder, one OGG per variant (``<cue>_v<k>.ogg``), the current synthesized version of the same
cue where one exists (``<cue>_current.ogg``), both levelled to the same loudness so only the sound differs, and
``index.json`` (every cue: its group, description, variants and current file) for the listening page.

Usage:
  python tools/audio/sampled/render_audition.py --library <Sonniss GDC 2026 folder> --out <folder>
"""
import argparse
import json
import sys
from pathlib import Path

AUDIO_TOOLS = Path(__file__).resolve().parents[1]
REPO = AUDIO_TOOLS.parents[1]
sys.path.insert(0, str(AUDIO_TOOLS))

from synth import io  # noqa: E402
from synth.core import rng  # noqa: E402

from sampled import boot, build, dsp, voice  # noqa: E402

SFX = REPO / "Assets/_Project/Audio/SFX"
VOICE_LUFS = -22.0
BOOT_LUFS = -22.0
DUST_LUFS = -28.0

# The synthesized cue a sampled sound would replace, where one is still in the game (path under
# Assets/_Project/Audio/SFX). The voice and the build set already ship sampled (sampled/picks.py).
CURRENT = {
    "boot_carrier": "2D/radio_carrier_loop.wav", "boot_crt": "2D/wake_power_thump.wav",
    "boot_lens": "2D/lens_whine.wav", "boot_lamps": "lamp_tick.wav", "boot_head": "2D/neck_servo.wav",
    "boot_cutter_fault": "cut_ignite_tier_tier0.wav", "boot_battery": "battery_cell_D5.wav",
}

TARGETS = {
    "build_place": -19.0, "build_snap": -21.5, "build_weld_loop": -21.5, "build_structure_complete": -18.5,
    "structure_power_on": -22.0,
}


def _entries():
    """(cue, group, description, render(lib, gen, variant), variants, target LUFS, loop)."""
    for cue, variants in voice.GESTURES.items():
        yield (cue, "voice", " / ".join(g.description for g in variants),
               (lambda lib, gen, v, name=cue: voice.render(lib, gen, name, v)), len(variants), VOICE_LUFS, False)
    for cue, fn in boot.STEPS.items():
        yield cue, "boot", (fn.__doc__ or "").strip(), (lambda lib, gen, v, f=fn: f(lib, gen)), 1, BOOT_LUFS, False
    yield ("boot_dust_slide", "boot", (boot.dust_slide.__doc__ or "").strip(), boot.dust_slide, 3, DUST_LUFS, False)
    for cue, (fn, count) in build.SOUNDS.items():
        yield cue, "build", (fn.__doc__ or "").strip(), fn, count, TARGETS[cue], cue == "build_weld_loop"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--library", required=True, help="the extracted Sonniss GDC 2026 folder (read only)")
    parser.add_argument("--out", required=True, help="where the OGGs and index.json go")
    args = parser.parse_args()
    lib = dsp.Library(args.library)
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    index = []
    for cue, group, description, render, variants, target, loop in _entries():
        files = []
        for variant in range(variants):
            signal = dsp.finish(render(lib, rng("sampled", cue, variant), variant), target, loop=loop)
            name = f"{cue}_v{variant + 1}.ogg"
            io.write_ogg(out / name, signal)
            files.append(name)
        current = None
        if cue in CURRENT:
            old, _ = io.read_audio(SFX / CURRENT[cue])
            old = old.mean(axis=1) if old.ndim == 2 else old
            current = f"{cue}_current.ogg"
            io.write_ogg(out / current, dsp.finish(old, target, loop=loop))
        index.append({"cue": cue, "group": group, "description": " ".join(description.split()), "variants": files,
                      "current": current, "loop": loop, "targetLufs": target})
        print(f"{cue:28} {variants} variant(s){'  + current' if current else ''}", flush=True)
    (out / "index.json").write_text(json.dumps({"cues": index}, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {sum(len(e['variants']) + (1 if e['current'] else 0) for e in index)} files to {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
