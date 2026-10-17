"""Build script for the 10 R1 weathered lunar basalt/breccia rock models (`Assets/_Project/Art/R1/Models/Rocks/Rock_00..09.fbx`).

- `Rock_00..Rock_04`: 5 varied irregular rounded pebbles/cobbles (<= 180 triangles each).
- `Rock_05..Rock_09`: 5 varied weathered lunar boulders with soft rounded forms, a few fracture planes,
  vesicle/spall pits, fracture crevices, dust-blanketed tops, and a basal regolith skirt (<= 300 triangles each).
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402


ROCK_SPECS: tuple[dict[str, object], ...] = (
    # Pebble 0: weathered rounded basalt cobble with shallow crown spall pit (0.38x -> ~0.37 m W)
    {
        "name": "Rock_00",
        "footprint_radius": 0.2128,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3420, 0.2128, 0.2964),
                "seed": 101,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.11,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.52, 0.74, 0.42), 0.54, 0.48),
                    ((-0.64, 0.62, -0.45), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.25, 0.88, 0.40), 0.34, 0.12),
                    ((-0.72, 0.45, 0.52), 0.28, 0.10),
                    ((0.40, 0.38, -0.83), 0.30, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Pebble 1: asymmetric sub-rounded breccia lump with oblique cleavage face (0.38x -> ~0.39 m W)
    {
        "name": "Rock_01",
        "footprint_radius": 0.2204,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3572, 0.2356, 0.2812),
                "seed": 203,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.68, 0.64, 0.35), 0.50, 0.52),
                    ((-0.42, 0.78, -0.46), 0.56, 0.44),
                    ((0.18, 0.55, -0.81), 0.60, 0.38),
                ],
                "pits_u": [
                    ((0.58, 0.65, 0.48), 0.32, 0.13),
                    ((-0.55, 0.62, -0.56), 0.30, 0.11),
                    ((-0.20, 0.92, 0.32), 0.26, 0.09),
                ],
                "crevices_u": [
                    ((0.82, 0.18, -0.54), 0.05, 0.18, 0.09),
                ],
            },
        ],
    },
    # Pebble 2: low-domed weathered regolith stone with stepped rim (0.38x -> ~0.41 m W)
    {
        "name": "Rock_02",
        "footprint_radius": 0.2356,
        "skirt_height_frac": 0.28,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3724, 0.1748, 0.3192),
                "seed": 307,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.13,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.22, 0.92, 0.32), 0.54, 0.46),
                    ((-0.68, 0.54, 0.49), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.12, 0.94, -0.32), 0.36, 0.13),
                    ((0.76, 0.48, 0.43), 0.28, 0.10),
                    ((-0.65, 0.52, -0.55), 0.30, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Pebble 3: squat potato-shaped lunar clast with weathered split seam (0.42x -> ~0.40 m W)
    {
        "name": "Rock_03",
        "footprint_radius": 0.2352,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3612, 0.2436, 0.3360),
                "seed": 409,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                    ((0.62, 0.58, -0.52), 0.56, 0.44),
                ],
                "pits_u": [
                    ((-0.45, 0.78, 0.42), 0.32, 0.12),
                    ((0.64, 0.55, -0.53), 0.30, 0.12),
                    ((0.15, 0.85, 0.50), 0.26, 0.09),
                ],
                "crevices_u": [
                    ((0.65, 0.20, 0.73), -0.04, 0.20, 0.11),
                ],
            },
        ],
    },
    # Pebble 4: pear-shaped impact-gardened basalt lump with spall pits (0.45x -> ~0.44 m W)
    {
        "name": "Rock_04",
        "footprint_radius": 0.2565,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.4140, 0.2340, 0.3240),
                "seed": 503,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.46, 0.78, -0.42), 0.52, 0.50),
                    ((-0.72, 0.52, 0.46), 0.58, 0.40),
                ],
                "pits_u": [
                    ((0.38, 0.82, -0.42), 0.34, 0.13),
                    ((-0.68, 0.56, 0.47), 0.28, 0.11),
                    ((-0.25, 0.68, -0.68), 0.30, 0.10),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Medium Boulder 0 (Rock_05): broad weathered lunar breccia boulder (0.75x -> ~1.15 m W)
    {
        "name": "Rock_05",
        "footprint_radius": 0.6750,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (1.0200, 0.6600, 0.9000),
                "seed": 607,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.58, 0.70, 0.41), 0.50, 0.52),
                    ((-0.62, 0.64, -0.45), 0.54, 0.46),
                    ((0.24, 0.62, -0.74), 0.58, 0.40),
                ],
                "pits_u": [
                    ((0.48, 0.74, 0.46), 0.32, 0.15),
                    ((-0.66, 0.58, 0.47), 0.28, 0.13),
                    ((0.28, 0.64, -0.71), 0.34, 0.15),
                    ((-0.42, 0.82, -0.38), 0.26, 0.11),
                    ((0.82, 0.38, -0.42), 0.25, 0.12),
                ],
                "crevices_u": [
                    ((0.76, 0.18, -0.62), 0.06, 0.19, 0.13),
                ],
            },
        ],
    },
    # Medium Boulder 1 (Rock_06): asymmetric humped basalt erratic with companion spall cobble (0.80x -> ~1.22 m W)
    {
        "name": "Rock_06",
        "footprint_radius": 0.7360,
        "skirt_height_frac": 0.24,
        "shells": [
            {
                "center_u": (-0.0480, 0.0, 0.0160),
                "size_u": (1.0080, 0.8160, 0.9120),
                "seed": 709,
                "lon_segs": 16,
                "lat_rings": 8,
                "skirt_flare": 0.17,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.64, 0.66, 0.39), 0.48, 0.54),
                    ((-0.56, 0.72, -0.40), 0.54, 0.46),
                    ((-0.78, 0.42, 0.46), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.56, 0.68, 0.47), 0.32, 0.15),
                    ((-0.52, 0.74, -0.42), 0.30, 0.13),
                    ((0.22, 0.58, -0.78), 0.28, 0.13),
                    ((-0.76, 0.48, 0.43), 0.26, 0.12),
                ],
                "crevices_u": [
                    ((-0.68, 0.22, 0.70), -0.05, 0.18, 0.12),
                ],
            },
            {
                "center_u": (0.3360, 0.0, -0.1440),
                "size_u": (0.5280, 0.3520, 0.4800),
                "seed": 719,
                "lon_segs": 10,
                "lat_rings": 4,
                "skirt_flare": 0.14,
                "burial_frac": 0.22,
                "rot_deg_u": (4.0, 24.0, -5.0),
                "fractures_u": [
                    ((0.62, 0.70, 0.35), 0.52, 0.48),
                ],
                "pits_u": [
                    ((0.45, 0.78, 0.42), 0.34, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Medium Boulder 2 (Rock_07): split-crown lunar breccia boulder (0.80x -> ~1.24 m W)
    {
        "name": "Rock_07",
        "footprint_radius": 0.7680,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (1.1360, 0.6560, 0.9920),
                "seed": 811,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.32, 0.86, 0.39), 0.52, 0.48),
                    ((-0.68, 0.58, 0.44), 0.54, 0.46),
                    ((0.64, 0.56, -0.52), 0.56, 0.44),
                ],
                "pits_u": [
                    ((0.52, 0.72, 0.45), 0.30, 0.14),
                    ((-0.62, 0.64, 0.45), 0.32, 0.15),
                    ((0.58, 0.58, -0.57), 0.30, 0.14),
                    ((-0.48, 0.62, -0.62), 0.28, 0.13),
                    ((0.08, 0.92, -0.38), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.88, 0.14, 0.45), 0.02, 0.22, 0.16),
                    ((-0.42, 0.24, 0.87), -0.08, 0.16, 0.11),
                ],
            },
        ],
    },
    # Large Boulder 0 (Rock_08): slanted crater-ejecta boulder with companion talus lump (1.65x -> ~2.43 m W)
    {
        "name": "Rock_08",
        "footprint_radius": 1.5510,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0825, 0.0, -0.0495),
                "size_u": (2.2110, 1.4850, 1.9470),
                "seed": 919,
                "lon_segs": 16,
                "lat_rings": 8,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (3.0, -14.0, -4.0),
                "fractures_u": [
                    ((-0.62, 0.68, 0.39), 0.48, 0.54),
                    ((0.58, 0.66, -0.47), 0.52, 0.48),
                    ((0.76, 0.44, 0.47), 0.58, 0.42),
                ],
                "pits_u": [
                    ((-0.54, 0.72, 0.43), 0.34, 0.15),
                    ((0.52, 0.68, -0.51), 0.30, 0.14),
                    ((0.72, 0.52, 0.45), 0.28, 0.13),
                    ((-0.36, 0.58, -0.73), 0.30, 0.13),
                ],
                "crevices_u": [
                    ((0.62, 0.20, 0.76), 0.04, 0.19, 0.13),
                ],
            },
            {
                "center_u": (-0.6600, 0.0, 0.3630),
                "size_u": (1.1550, 0.6930, 1.0230),
                "seed": 929,
                "lon_segs": 10,
                "lat_rings": 4,
                "skirt_flare": 0.14,
                "burial_frac": 0.22,
                "rot_deg_u": (-4.0, -26.0, 5.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((-0.42, 0.80, 0.42), 0.34, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Large Boulder 1 (Rock_09): massive hummocky lunar boulder (1.95x -> ~2.95 m W)
    {
        "name": "Rock_09",
        "footprint_radius": 1.8720,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (2.6910, 1.8330, 2.4960),
                "seed": 1031,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((-0.54, 0.74, -0.40), 0.50, 0.50),
                    ((0.66, 0.60, 0.45), 0.54, 0.46),
                    ((-0.28, 0.64, 0.71), 0.58, 0.42),
                ],
                "pits_u": [
                    ((-0.48, 0.76, -0.43), 0.34, 0.15),
                    ((0.60, 0.64, 0.48), 0.32, 0.14),
                    ((-0.26, 0.66, 0.70), 0.30, 0.14),
                    ((0.42, 0.58, -0.69), 0.28, 0.13),
                    ((0.15, 0.92, 0.36), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.54, 0.22, -0.81), -0.05, 0.18, 0.13),
                ],
            },
        ],
    },
)


def build_rock(spec: dict[str, object]) -> bpy.types.Object:
    """Build a single R1 weathered lunar basalt/breccia rock mesh object with UVs and baked vertex colours."""
    name = str(spec["name"])
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    for shell in spec["shells"]:
        mb.add_weathered_rock(
            center_u=shell["center_u"],
            size_u=shell["size_u"],
            seed=int(shell["seed"]),
            lon_segs=int(shell["lon_segs"]),
            lat_rings=int(shell["lat_rings"]),
            fractures_u=shell["fractures_u"],
            pits_u=shell["pits_u"],
            crevices_u=shell["crevices_u"],
            skirt_flare=float(shell["skirt_flare"]),
            burial_frac=float(shell["burial_frac"]),
            rot_deg_u=shell["rot_deg_u"],
            mat="R1_Basalt",
        )
    obj = mb.build_object(smooth_angle_deg=44.0)
    r1kit.apply_triplanar_uvs(obj, texel_scale=0.85)
    bpy.context.view_layer.update()
    r1kit.bake_rock_vertex_colors(
        obj,
        footprint_radius=float(spec["footprint_radius"]),
        skirt_height_frac=float(spec["skirt_height_frac"]),
    )
    return obj


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build Rock_00..Rock_09 FBX models.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rocks"),
    )
    args = parser.parse_args(argv)

    args.out_dir.mkdir(parents=True, exist_ok=True)
    for spec in ROCK_SPECS:
        r1kit.reset_scene()
        rock_obj = build_rock(spec)
        tris = r1kit.count_triangles(rock_obj)
        verts = len(rock_obj.data.vertices)
        out_path = args.out_dir / f"{spec['name']}.fbx"
        r1kit.export_fbx(rock_obj, out_path)
        print(f"[{spec['name']}] Exported {out_path} ({verts} verts, {tris} triangles)")


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
