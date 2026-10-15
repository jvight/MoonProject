"""Master build entry script (`tools/art/blender/build_all.py`) for the first three R1 structures."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import cargo_lander  # noqa: E402
import charging_pad  # noqa: E402
import power_pylon  # noqa: E402
import r1kit  # noqa: E402
import solar_array  # noqa: E402


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(
        description="Build ChargingPad, SolarArray, PowerPylon, and CargoLander FBX models and preview renders."
    )
    parser.add_argument(
        "--models-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures"),
    )
    parser.add_argument(
        "--lander-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Lander/CargoLander.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    models_dir = args.models_dir
    renders_dir = args.renders_dir
    skip_flag = ["--skip-renders"] if args.skip_renders else []

    # 1. Build & export ChargingPad (+ 12 diagnostic renders)
    charging_pad.main(
        [
            "--fbx-out",
            str(models_dir / "ChargingPad.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 2. Build & export SolarArray (+ 12 diagnostic renders)
    solar_array.main(
        [
            "--fbx-out",
            str(models_dir / "SolarArray.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 3. Build & export PowerPylon (+ 12 diagnostic renders)
    power_pylon.main(
        [
            "--fbx-out",
            str(models_dir / "PowerPylon.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 4. Build & export CargoLander (+ diagnostic and showcase renders)
    cargo_lander.main(
        [
            "--fbx-out",
            str(args.lander_out),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 5. Build all three structures together in a fresh scene and render Logs/renders/family.png alongside 07's 1.46 m stand-in box
    if not args.skip_renders:
        r1kit.reset_scene()
        cp_root = charging_pad.build_charging_pad()
        sa_root = solar_array.build_solar_array()
        pp_root = power_pylon.build_power_pylon()

        # In Blender (+X is viewer-left, -X is viewer-right):
        # +3.55 m = 07 (1.46 m), +0.85 m = ChargingPad (1.55 m), -1.95 m = SolarArray (1.85 m), -3.95 m = PowerPylon (1.95 m)
        r1kit.render_family_lineup(
            roots_and_offsets=[
                (cp_root, 0.85),
                (sa_root, -1.95),
                (pp_root, -3.95),
            ],
            out_path=renders_dir / "family.png",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
