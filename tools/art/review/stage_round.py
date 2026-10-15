#!/usr/bin/env python3
"""
Stages one review round of a 3D model for the owner's review page (tools/art/review/index.html), so publishing it
is one Artifact call from any session.

    python tools/art/review/stage_round.py --model rover --name "07" --round 3 \
        --fbx Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx \
        --concept D:/.../turnaround.png --capture D:/.../renders --changed "Wider wheelbase, deeper panel seams"

What it does:
  1. FBX -> GLB in headless Blender (blender_fbx_to_glb.py: no vertex colours, slots kept, transforms applied), or
     takes a ready --glb;
  2. rewrites the GLB's materials to the game's R1 palette by slot name (r1_palette.py; a material outside the R1
     slots stops the round) and stores it as base64 text (model.glb.txt), since artifacts do not serve .glb;
  3. shrinks concepts and captures to JPEG (longest side MAX_SIDE px);
  4. merges the round into the shared manifest (MoonProject-wt/.review3d/manifest.json, one per machine, so every
     session and worktree sees every round) and writes the staging folder: index.html, manifest.json and only this
     round's files, plus publish.json listing them.
A round without --concept reuses the previous round's concepts. Re-staging the same round replaces it. Several
rounds or models can be staged into one folder before one publish; delete the folder after publishing.

Then publish the staging folder (the default is <worktree>/Logs/review3d/stage; it must be inside the publishing
session's working directory or scratchpad, so pass --out there) with one Artifact call, exactly as the script
prints it: file_path = <out>/index.html, root = <out>, files = publish.json's list, url = the page's URL (kept in
MoonProject-wt/.review3d/artifact_url.txt). Files left out of a publish stay on the page, so older rounds keep
working.
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import base64
import datetime
import json
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

import r1_palette

HERE = Path(__file__).resolve().parent
BLENDER = Path("D:/Blender/blender.exe")
BLENDER_SCRIPT = HERE / "blender_fbx_to_glb.py"
PAGE = HERE / "index.html"
MAX_SIDE = 1600
JPEG_QUALITY = 84
MATTE = (24, 26, 31)
IMAGE_SUFFIXES = (".png", ".jpg", ".jpeg", ".webp")
CONVERT_TIMEOUT = 600
STATS_TAG = "REVIEW_GLB "
# Artifacts do not serve .glb, so the model goes as base64 text and the page turns it back into a GLB blob.
MODEL_TEXT = "model.glb.txt"


def git_root(start):
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"], cwd=start,
                         capture_output=True, text=True)
    if res.returncode != 0:
        raise SystemExit(f"not inside a git worktree: {res.stderr.strip()}")
    return Path(res.stdout.strip())


def shared_dir(root):
    """MoonProject-wt/.review3d next to the main checkout, shared by every worktree of the repository."""
    common = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--path-format=absolute",
                             "--git-common-dir"], cwd=root, capture_output=True, text=True, check=True)
    return Path(common.stdout.strip()).resolve().parent.parent / "MoonProject-wt" / ".review3d"


def convert(fbx, glb):
    """FBX -> GLB in headless Blender; returns the model's stats line as a dict."""
    res = subprocess.run([str(BLENDER), "-b", "--factory-startup", "--python", str(BLENDER_SCRIPT), "--", str(fbx),
                          str(glb)], capture_output=True, text=True, encoding="utf-8", errors="replace",
                         timeout=CONVERT_TIMEOUT)
    lines = [line for line in res.stdout.splitlines() if line.startswith(STATS_TAG)]
    if res.returncode != 0 or not lines or not glb.exists():
        raise SystemExit(f"Blender could not convert {fbx}:\n{res.stdout[-2000:]}{res.stderr[-2000:]}")
    return json.loads(lines[-1][len(STATS_TAG):])


def image_sources(paths):
    """Image files from <paths>, a folder contributing its images in name order."""
    found = []
    for path in map(Path, paths or []):
        if path.is_dir():
            found += sorted(p for p in path.iterdir() if p.suffix.lower() in IMAGE_SUFFIXES)
        elif path.suffix.lower() in IMAGE_SUFFIXES and path.exists():
            found.append(path)
        else:
            raise SystemExit(f"not an image or folder: {path}")
    return found


def to_jpeg(source, target):
    with Image.open(source) as image:
        image.load()
        if image.mode in ("RGBA", "LA", "P"):
            rgba = image.convert("RGBA")
            image = Image.new("RGB", rgba.size, MATTE)
            image.paste(rgba, mask=rgba.split()[3])
        else:
            image = image.convert("RGB")
        image.thumbnail((MAX_SIDE, MAX_SIDE))
        target.parent.mkdir(parents=True, exist_ok=True)
        image.save(target, "JPEG", quality=JPEG_QUALITY, optimize=True)


def staged_files(out):
    """Every file staged in <out> for the next publish except the page itself and publish.json."""
    return sorted(p.relative_to(out).as_posix() for p in out.rglob("*")
                  if p.is_file() and p.name not in ("index.html", "publish.json"))


def load_manifest(path):
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {"models": []}


def merge_round(manifest, model_id, name, entry):
    """Puts <entry> into <manifest> as round entry['round'] of <model_id>, replacing a re-staged round; a round
    without concepts inherits the latest earlier round's."""
    model = next((m for m in manifest["models"] if m["id"] == model_id), None)
    if model is None:
        if not name:
            raise SystemExit(f"model '{model_id}' is new: give it a display --name")
        model = {"id": model_id, "name": name, "rounds": []}
        manifest["models"].append(model)
    elif name:
        model["name"] = name
    if not entry["concepts"]:
        earlier = [r for r in model["rounds"] if r["round"] < entry["round"] and r["concepts"]]
        entry["concepts"] = max(earlier, key=lambda r: r["round"])["concepts"] if earlier else []
    model["rounds"] = sorted([r for r in model["rounds"] if r["round"] != entry["round"]] + [entry],
                             key=lambda r: r["round"])
    return manifest


def stage(args):
    root = git_root(Path.cwd())
    shared = shared_dir(root)
    out = Path(args.out).resolve() if args.out else root / "Logs" / "review3d" / "stage"
    folder = f"models/{args.model}/r{args.round}"
    if (out / folder).exists():
        shutil.rmtree(out / folder)
    (out / folder).mkdir(parents=True)
    glb = out / folder / "model.glb"
    if args.fbx:
        raw = out / folder / "raw.glb"
        stats = convert(Path(args.fbx), raw)
        r1_palette.apply_palette(raw, glb, r1_palette.load_palette(root))
        raw.unlink()
    else:
        stats = {}
        r1_palette.apply_palette(Path(args.glb), glb, r1_palette.load_palette(root))
    model_text = out / folder / MODEL_TEXT
    model_text.write_text(base64.b64encode(glb.read_bytes()).decode("ascii"), encoding="ascii")
    glb.unlink()
    concepts, captures = [], []
    for kind, sources, listed in (("concept", args.concept, concepts), ("capture", args.capture, captures)):
        for index, source in enumerate(image_sources(sources), 1):
            path = f"{folder}/{kind}-{index:02d}-{source.stem}.jpg"
            to_jpeg(source, out / path)
            listed.append({"src": path, "label": source.stem.replace("_", " ")})
    entry = {"round": args.round, "glb": f"{folder}/{MODEL_TEXT}", "concepts": concepts, "captures": captures,
             "changed": args.changed, "staged": datetime.date.today().isoformat(), "stats": stats}
    shared.mkdir(parents=True, exist_ok=True)
    manifest = merge_round(load_manifest(shared / "manifest.json"), args.model, args.name, entry)
    text = json.dumps(manifest, ensure_ascii=False, indent=1)
    (shared / "manifest.json").write_text(text, encoding="utf-8")
    (out / "manifest.json").write_text(text, encoding="utf-8")
    shutil.copyfile(PAGE, out / "index.html")
    files = staged_files(out)
    (out / "publish.json").write_text(json.dumps({"root": str(out), "files": files}, indent=1), encoding="utf-8")
    url_file = shared / "artifact_url.txt"
    url = url_file.read_text(encoding="utf-8").strip() if url_file.exists() else "(first publish: omit url)"
    print(f"staged {args.model} round {args.round} in {out}"
          + (f" ({stats['triangles']} triangles, {stats['meshes']} meshes)" if stats else ""))
    print("publish with one Artifact call:")
    print(json.dumps({"action": "publish", "file_path": str(out / "index.html"), "root": str(out),
                      "files": files, "url": url}, indent=1))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True, help="model id, e.g. rover, charging_pad (letters, digits, _ -)")
    ap.add_argument("--name", help="display name on the page (required the first time a model is staged)")
    ap.add_argument("--round", required=True, type=int, help="review round number (1, 2, ...)")
    source = ap.add_mutually_exclusive_group(required=True)
    source.add_argument("--fbx", help="FBX to convert in headless Blender")
    source.add_argument("--glb", help="an already converted GLB (vertex colours already stripped)")
    ap.add_argument("--concept", action="append", help="approved concept image or folder (repeatable)")
    ap.add_argument("--capture", action="append", help="Unity or Blender capture image or folder (repeatable)")
    ap.add_argument("--changed", default="", help="one line: what changed in this round")
    ap.add_argument("--out", help="staging folder (default <worktree>/Logs/review3d/stage)")
    args = ap.parse_args()
    if not all(ch.isalnum() or ch in "_-" for ch in args.model) or args.round < 1:
        raise SystemExit("--model takes letters, digits, '_' and '-'; --round is 1 or more")
    stage(args)


if __name__ == "__main__":
    sys.exit(main())
