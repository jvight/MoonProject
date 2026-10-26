#!/usr/bin/env python3
"""
Encodes the look-max camera move (Logs/look-max/move-<stamp>/<profile>/frame_NNNN.jpg, written by
MoonProject.Art.PlayModeTests.LookMaxSession.Move_IsRendered_FrameByFrame) to one MP4 per profile and a side-by-side
comparison, with the ffmpeg that ships with the imageio-ffmpeg package.

Usage:
  python tools/art/look_max_video.py [MOVE_FOLDER] [--fps 24]     # default: the newest Logs/look-max/move-* folder
"""
import argparse
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg

CRF = 16
PRESET = "slow"


def encode(ffmpeg: str, frames: Path, fps: int, out: Path) -> None:
    subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-framerate", str(fps), "-i", str(frames / "frame_%04d.jpg"),
                    "-c:v", "libx264", "-preset", PRESET, "-crf", str(CRF), "-pix_fmt", "yuv420p", str(out)],
                   check=True)


def stack(ffmpeg: str, top: Path, bottom: Path, out: Path) -> None:
    subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(top), "-i", str(bottom), "-filter_complex",
                    "[0:v][1:v]vstack=inputs=2,scale=iw/2:-2", "-c:v", "libx264", "-preset", PRESET, "-crf", str(CRF),
                    "-pix_fmt", "yuv420p", str(out)], check=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("folder", nargs="?", type=Path)
    parser.add_argument("--fps", type=int, default=24)
    args = parser.parse_args()
    folder = args.folder
    if folder is None:
        moves = sorted(Path("Logs/look-max").glob("move-*"))
        if not moves:
            print("no Logs/look-max/move-* folder", file=sys.stderr)
            return 1
        folder = moves[-1]

    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    videos = {}
    for profile in sorted(p for p in folder.iterdir() if p.is_dir()):
        out = folder / f"{folder.name}_{profile.name}.mp4"
        encode(ffmpeg, profile, args.fps, out)
        videos[profile.name] = out
        print(out)

    if "shipped" in videos and "max" in videos:
        out = folder / f"{folder.name}_shipped_over_max.mp4"
        stack(ffmpeg, videos["shipped"], videos["max"], out)
        print(out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
