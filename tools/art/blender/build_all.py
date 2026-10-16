"""Master build entry script (`tools/art/blender/build_all.py`) for all ten R1 structures, CargoLander, and Rock_00..09."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import antenna_mast  # noqa: E402
import battery_bank  # noqa: E402
import cargo_lander  # noqa: E402
import charging_pad  # noqa: E402
import fabricator  # noqa: E402
import power_pylon  # noqa: E402
import r1kit  # noqa: E402
import rocks  # noqa: E402
import smelter  # noqa: E402
import solar_array  # noqa: E402
import storage_rack  # noqa: E402
import upgrade_bay  # noqa: E402
import vehicle_pad  # noqa: E402


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(
        description="Build all ten R1 structures, CargoLander, and Rock_00..09 FBX models and preview renders."
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
        "--rocks-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rocks"),
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

    # 1. Build & export ChargingPad (+ diagnostic renders)
    charging_pad.main(
        [
            "--fbx-out",
            str(models_dir / "ChargingPad.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 2. Build & export SolarArray (+ diagnostic renders)
    solar_array.main(
        [
            "--fbx-out",
            str(models_dir / "SolarArray.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 3. Build & export PowerPylon (+ diagnostic renders)
    power_pylon.main(
        [
            "--fbx-out",
            str(models_dir / "PowerPylon.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 4. Build & export BatteryBank (+ diagnostic renders)
    battery_bank.main(
        [
            "--fbx-out",
            str(models_dir / "BatteryBank.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 5. Build & export Smelter (+ diagnostic renders)
    smelter.main(
        [
            "--fbx-out",
            str(models_dir / "Smelter.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 6. Build & export Fabricator (+ diagnostic renders)
    fabricator.main(
        [
            "--fbx-out",
            str(models_dir / "Fabricator.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 7. Build & export StorageRack (+ diagnostic renders)
    storage_rack.main(
        [
            "--fbx-out",
            str(models_dir / "StorageRack.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 8. Build & export UpgradeBay (+ diagnostic, scale_07, and compare renders)
    upgrade_bay.main(
        [
            "--fbx-out",
            str(models_dir / "UpgradeBay.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 9. Build & export AntennaMast (+ diagnostic and compare renders)
    antenna_mast.main(
        [
            "--fbx-out",
            str(models_dir / "AntennaMast.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 10. Build & export VehiclePad (+ diagnostic and compare renders)
    vehicle_pad.main(
        [
            "--fbx-out",
            str(models_dir / "VehiclePad.fbx"),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 11. Build & export CargoLander (+ diagnostic and showcase renders)
    cargo_lander.main(
        [
            "--fbx-out",
            str(args.lander_out),
            "--renders-dir",
            str(renders_dir),
            *skip_flag,
        ]
    )

    # 12. Build & export Rock_00..Rock_09
    rocks.main(
        [
            "--out-dir",
            str(args.rocks_dir),
        ]
    )

    # 13. Build all ten structures together in a fresh scene and render Logs/renders/family.png alongside 07's 1.46 m stand-in
    if not args.skip_renders:
        r1kit.reset_scene()
        cp_root = charging_pad.build_charging_pad()
        sa_root = solar_array.build_solar_array()
        pp_root = power_pylon.build_power_pylon()
        bb_root = battery_bank.build_battery_bank()
        sm_root = smelter.build_smelter()
        fb_root = fabricator.build_fabricator()
        sr_root = storage_rack.build_storage_rack()
        ub_root = upgrade_bay.build_upgrade_bay()
        am_root = antenna_mast.build_antenna_mast()
        vp_root = vehicle_pad.build_vehicle_pad()

        top_y = 3.20
        r1kit.render_family_lineup(
            roots_and_offsets=[
                # Top tier: Power, Comms & Infrastructure
                (cp_root, 4.40, top_y, 0.0),
                (sa_root, 1.50, top_y, 0.0),
                (bb_root, -1.30, top_y, 0.0),
                (pp_root, -3.70, top_y, 0.0),
                (am_root, -6.40, top_y, 0.0),
                # Bottom tier: Production, Logistics & Bay/Pad
                (sm_root, 4.40, 0.0, 0.0),
                (fb_root, 1.40, 0.0, 0.0),
                (sr_root, -1.30, 0.0, 0.0),
                (ub_root, -4.40, 0.0, 0.0),
                (vp_root, -8.00, 0.0, 0.0),
            ],
            out_path=renders_dir / "family.png",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
