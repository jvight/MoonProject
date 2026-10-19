#!/usr/bin/env python3
"""Render every variant of the sampled sound set for a listening pass (to choose or change a pick in picks.py).

Writes, into the output folder, one OGG per variant (``<cue>_v<k>.ogg``), all levelled to their group's loudness so
only the sound differs, and ``index.json`` (every cue: its group, description and variants) for a listening page.

Usage:
  python tools/audio/sampled/render_audition.py --library <Sonniss GDC 2026 folder> --out <folder>
"""
import argparse
import json
import sys
from pathlib import Path

AUDIO_TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(AUDIO_TOOLS))

from synth import io  # noqa: E402
from synth.core import rng  # noqa: E402

from sampled import base, boot, build, call, dsp, face, rover, sky, ui, voice  # noqa: E402

VOICE_LUFS = -22.0
BOOT_LUFS = -22.0
SKY_LUFS = -20.0
UI_LUFS = -28.0
BASE_LUFS = -24.0
ROVER_LUFS = -26.0
FACE_LUFS = {"face_text_tick": -28.0, "face_line_done": -26.0, "face_load_step": -26.0}
FACE_DEFAULT_LUFS = -22.0
CALL_LUFS = -30.0
TARGETS = {
    "build_place": -19.0, "build_snap": -21.5, "build_weld_loop": -21.5, "build_structure_complete": -18.5,
    "structure_power_on": -22.0,
}


def _entries():
    """(cue, group, description, render(lib, gen, variant), variants, target LUFS, loop)."""
    for cue, variants in voice.GESTURES.items():
        yield (cue, "voice", " / ".join(g.description for g in variants),
               (lambda lib, gen, v, name=cue: voice.render(lib, gen, name, v)), len(variants), VOICE_LUFS, False)
    for cue, (fn, count) in boot.SOUNDS.items():
        yield cue, "boot", (fn.__doc__ or "").strip(), fn, count, BOOT_LUFS, False
    for cue, (fn, count) in sky.SOUNDS.items():
        yield cue, "sky", (fn.__doc__ or "").strip(), fn, count, SKY_LUFS, False
    for cue, (fn, count) in base.SOUNDS.items():
        yield cue, "base", (fn.__doc__ or "").strip(), fn, count, BASE_LUFS, False
    for cue, (fn, count) in rover.SOUNDS.items():
        yield cue, "rover", (fn.__doc__ or "").strip(), fn, count, ROVER_LUFS, False
    for cue, (fn, count) in call.SOUNDS.items():
        yield cue, "call", (fn.__doc__ or "").strip(), fn, count, CALL_LUFS, cue != "call_decoded"
    for cue, (fn, count) in face.SOUNDS.items():
        yield cue, "face", (fn.__doc__ or "").strip(), fn, count, FACE_LUFS.get(cue, FACE_DEFAULT_LUFS), False
    for cue, (fn, count) in ui.SOUNDS.items():
        yield cue, "ui", (fn.__doc__ or "").strip(), fn, count, UI_LUFS, False
    for cue, (fn, count) in build.SOUNDS.items():
        yield cue, "build", (fn.__doc__ or "").strip(), fn, count, TARGETS[cue], cue == "build_weld_loop"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--library", required=True, help="the extracted Sonniss GDC 2026 folder (read only)")
    parser.add_argument("--out", required=True, help="where the OGGs and index.json go")
    parser.add_argument("--group", help="only this group (voice, boot, sky, build, base, rover, face, call, ui)")
    args = parser.parse_args()
    lib = dsp.Library(args.library)
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    index = []
    for cue, group, description, render, variants, target, loop in _entries():
        if args.group and group != args.group:
            continue
        files = []
        for variant in range(variants):
            signal = dsp.finish(render(lib, rng("sampled", cue, variant), variant), target, loop=loop)
            name = f"{cue}_v{variant + 1}.ogg"
            io.write_ogg(out / name, signal)
            files.append(name)
        index.append({"cue": cue, "group": group, "description": " ".join(description.split()), "variants": files,
                      "loop": loop, "targetLufs": target})
        print(f"{cue:28} {variants} variant(s)", flush=True)
    (out / "index.json").write_text(json.dumps({"cues": index}, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {sum(len(e['variants']) for e in index)} files to {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
