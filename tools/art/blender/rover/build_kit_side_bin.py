"""
Headless-Blender builder for 07 v3's R1 Kit_SideBin upgrade (`Assets/_Project/Art/R1/Models/Rover/Kit_SideBin.fbx`).

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/build_kit_side_bin.py
"""
from __future__ import annotations

import argparse
from pathlib import Path
import sys

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))
sys.dont_write_bytecode = True

from kit_parts import build_and_export_kit  # noqa: E402


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export Kit_SideBin.fbx.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/Kit_SideBin.fbx"),
    )
    args = parser.parse_args(argv)
    build_and_export_kit("Kit_SideBin", str(args.fbx_out))


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
