"""Build script for the 6 R1 fractured basalt rock and slab models (`Assets/_Project/Art/R1/Models/Rocks/Rock_00..05.fbx`)."""

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
    # Pebble 0: low-profile cleaved basalt cobble
    {
        "name": "Rock_00",
        "bevel_wear_width": 0.045,
        "footprint_radius": 0.55,
        "shells": [
            {
                "center_u": (0.0, 0.16, 0.0),
                "size_u": (0.88, 0.42, 0.76),
                "bevel": 0.032,
                "bevel_segments": 1,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "cuts_u": [
                    ((0.55, 0.78, 0.30), 0.56),
                    ((-0.62, 0.72, 0.31), 0.58),
                    ((0.28, 0.75, -0.60), 0.54),
                    ((-0.45, 0.62, -0.64), 0.60),
                ],
            },
        ],
    },
    # Pebble 1: angular wedge-shaped basalt fragment
    {
        "name": "Rock_01",
        "bevel_wear_width": 0.045,
        "footprint_radius": 0.58,
        "shells": [
            {
                "center_u": (0.0, 0.20, 0.0),
                "size_u": (0.92, 0.50, 0.72),
                "bevel": 0.030,
                "bevel_segments": 1,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "cuts_u": [
                    ((0.68, 0.68, 0.27), 0.52),
                    ((-0.48, 0.82, 0.31), 0.55),
                    ((0.18, 0.64, -0.75), 0.54),
                    ((-0.72, 0.42, -0.55), 0.60),
                ],
            },
        ],
    },
    # Pebble 2: flat layered basalt slab stone
    {
        "name": "Rock_02",
        "bevel_wear_width": 0.042,
        "footprint_radius": 0.62,
        "shells": [
            {
                "center_u": (0.0, 0.13, 0.0),
                "size_u": (0.96, 0.34, 0.80),
                "bevel": 0.026,
                "bevel_segments": 1,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "cuts_u": [
                    ((0.18, 0.96, 0.21), 0.64),
                    ((-0.65, 0.52, 0.55), 0.62),
                    ((0.72, 0.48, 0.50), 0.60),
                    ((-0.58, 0.46, -0.67), 0.64),
                ],
            },
        ],
    },
    # Boulder 0: broad weathered lunar basalt boulder with stepped fracture planes and basal shoulder
    {
        "name": "Rock_03",
        "bevel_wear_width": 0.068,
        "footprint_radius": 0.88,
        "shells": [
            {
                "center_u": (-0.06, 0.34, 0.02),
                "size_u": (1.28, 0.84, 1.16),
                "bevel": 0.046,
                "bevel_segments": 1,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "cuts_u": [
                    ((0.52, 0.78, 0.34), 0.54),
                    ((-0.60, 0.72, 0.35), 0.56),
                    ((0.32, 0.74, -0.59), 0.55),
                    ((-0.42, 0.68, -0.60), 0.58),
                    ((0.84, 0.28, 0.46), 0.64),
                    ((-0.80, 0.30, -0.52), 0.66),
                    ((0.20, 0.95, -0.24), 0.62),
                    ((0.15, -0.94, 0.30), 0.78),
                ],
            },
            {
                "center_u": (0.34, 0.15, -0.14),
                "size_u": (0.72, 0.44, 0.68),
                "bevel": 0.034,
                "bevel_segments": 1,
                "rot_deg_u": (4.0, 22.0, -5.0),
                "cuts_u": [
                    ((0.62, 0.72, 0.31), 0.56),
                    ((-0.48, 0.80, -0.36), 0.58),
                    ((0.35, 0.65, -0.67), 0.60),
                    ((0.78, 0.24, 0.58), 0.66),
                ],
            },
        ],
    },
    # Boulder 1: tall angular basalt erratic monolith with cleaved crown facets and low talus shelf
    {
        "name": "Rock_04",
        "bevel_wear_width": 0.068,
        "footprint_radius": 0.85,
        "shells": [
            {
                "center_u": (0.04, 0.43, 0.0),
                "size_u": (1.18, 1.04, 1.08),
                "bevel": 0.044,
                "bevel_segments": 1,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "cuts_u": [
                    ((0.64, 0.68, 0.36), 0.52),
                    ((-0.58, 0.74, 0.34), 0.54),
                    ((0.28, 0.72, -0.63), 0.54),
                    ((-0.46, 0.66, -0.59), 0.58),
                    ((0.86, 0.32, -0.40), 0.64),
                    ((-0.82, 0.35, 0.45), 0.64),
                    ((-0.18, 0.94, 0.28), 0.62),
                    ((-0.12, -0.95, -0.28), 0.78),
                ],
            },
            {
                "center_u": (-0.32, 0.16, 0.18),
                "size_u": (0.68, 0.46, 0.64),
                "bevel": 0.032,
                "bevel_segments": 1,
                "rot_deg_u": (-5.0, -28.0, 6.0),
                "cuts_u": [
                    ((-0.58, 0.74, 0.34), 0.56),
                    ((0.46, 0.78, 0.42), 0.58),
                    ((-0.42, 0.64, -0.64), 0.60),
                    ((-0.82, 0.25, -0.51), 0.66),
                ],
            },
        ],
    },
    # Boulder 2: wide split-shelf basalt outcrop with slanted tabular main block and companion ledge
    {
        "name": "Rock_05",
        "bevel_wear_width": 0.068,
        "footprint_radius": 0.94,
        "shells": [
            {
                "center_u": (-0.08, 0.29, -0.04),
                "size_u": (1.36, 0.74, 1.18),
                "bevel": 0.044,
                "bevel_segments": 1,
                "rot_deg_u": (3.0, 8.0, -4.0),
                "cuts_u": [
                    ((0.24, 0.92, 0.31), 0.58),
                    ((-0.64, 0.64, 0.42), 0.56),
                    ((0.68, 0.58, 0.45), 0.56),
                    ((0.52, 0.62, -0.59), 0.56),
                    ((-0.56, 0.60, -0.57), 0.58),
                    ((-0.86, 0.22, -0.46), 0.66),
                    ((0.84, 0.24, -0.48), 0.66),
                    ((0.0, -0.96, 0.28), 0.78),
                ],
            },
            {
                "center_u": (0.36, 0.14, 0.22),
                "size_u": (0.82, 0.40, 0.72),
                "bevel": 0.032,
                "bevel_segments": 1,
                "rot_deg_u": (-4.0, -18.0, 5.0),
                "cuts_u": [
                    ((0.32, 0.90, 0.29), 0.60),
                    ((0.68, 0.58, 0.45), 0.58),
                    ((-0.54, 0.68, -0.49), 0.60),
                    ((0.76, 0.30, -0.57), 0.64),
                ],
            },
        ],
    },
)


def build_rock(spec: dict[str, object]) -> bpy.types.Object:
    """Build a single R1 fractured basalt rock mesh object with UVs and baked vertex colours."""
    name = str(spec["name"])
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    for shell in spec["shells"]:
        mb.add_fractured_rock(
            center_u=shell["center_u"],
            size_u=shell["size_u"],
            cuts_u=shell["cuts_u"],
            mat="R1_Basalt",
            bevel=float(shell["bevel"]),
            bevel_segments=int(shell["bevel_segments"]),
            rot_deg_u=shell["rot_deg_u"],
        )
    obj = mb.build_object(smooth_angle_deg=56.0)
    r1kit.apply_triplanar_uvs(obj, texel_scale=0.85)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(
        obj,
        footprint_radius=float(spec["footprint_radius"]),
        bevel_wear_width=float(spec["bevel_wear_width"]),
    )
    return obj


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build Rock_00..Rock_05 FBX models.")
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
