"""Build R1 Charging Pad Blueprint Fragments (`PadFragment_CoilPlate.fbx`, `PadFragment_GuideRail.fbx`).

Deliverable 2 (`Logs/TASK.md`):
- `PadFragment_CoilPlate`: a round induction coil plate (~0.50 m diameter, 0.12 m tall,
  exposed concentric copper windings, ceramic comb insulators, brass busbar terminals).
- `PadFragment_GuideRail`: a short rover guide rail section (~0.90 m long, 0.28 m wide,
  0.18 m tall) with the charging pad's yellow-black hazard edge and brushed steel wheel rub bar.
- Both models have a clean, simple silhouette so the scan raster and corner brackets wrap
  the object's own bounds cleanly.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def build_coil_plate() -> bpy.types.Object:
    """Build `PadFragment_CoilPlate` (0.50 m x 0.50 m x 0.12 m round induction coil plate)."""
    mb = r1kit.MeshBuilder("PadFragment_CoilPlate", pivot_u=(0.0, 0.0, 0.0))

    # 1. Outer circular induction tray base (`R = 0.250 m` -> exact 0.50 m diameter across X and Z)
    # Rubber isolation base ring (y = 0.000 .. 0.018)
    mb.add_cylinder(
        center_u=(0.0, 0.009, 0.0),
        radius=0.250,
        length=0.018,
        mat="R1_Rubber",
        axis_u="Y",
        segments=32,
        bevel=0.003,
        bevel_segments=1,
    )
    # Heavy machined grey-metal induction housing pan (y = 0.016 .. 0.052)
    mb.add_cylinder(
        center_u=(0.0, 0.034, 0.0),
        radius=0.246,
        length=0.036,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=32,
        bevel=0.004,
        bevel_segments=2,
    )
    # Outer raised containment rim ring (`R_out = 0.244`, `R_in = 0.214`, y = 0.050 .. 0.074)
    mb.add_ring(
        center_u=(0.0, 0.062, 0.0),
        r_outer=0.244,
        r_inner=0.214,
        height=0.024,
        segs=32,
        axis="y",
        mat="R1_GreyMetal",
    )

    # 4 cardinal alignment indexing tabs (`R1_OrangeStripe`) + 4 diagonal steel hold-down lugs
    for ang_deg in (0.0, 90.0, 180.0, 270.0):
        rad = math.radians(ang_deg)
        tx = math.cos(rad) * 0.228
        tz = math.sin(rad) * 0.228
        mb.add_bevel_box(
            center_u=(tx, 0.064, tz),
            size_u=(0.034, 0.024, 0.056),
            mat="R1_OrangeStripe",
            bevel=0.0025,
            segments=1,
            rot_deg_u=(0.0, -ang_deg, 0.0),
        )
    for ang_deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(ang_deg)
        lx = math.cos(rad) * 0.226
        lz = math.sin(rad) * 0.226
        mb.add_bevel_box(
            center_u=(lx, 0.062, lz),
            size_u=(0.036, 0.026, 0.044),
            mat="R1_BareSteel",
            bevel=0.0025,
            segments=1,
            rot_deg_u=(0.0, -ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=(lx, 0.078, lz),
            radius=0.008,
            length=0.008,
            mat="R1_Brass",
            axis_u="Y",
            segments=6,
            bevel=0.001,
            bevel_segments=1,
        )

    # 2. Dark ferrite magnetic backing disc + gold-foil thermal shield bed under the copper coils
    mb.add_cylinder(
        center_u=(0.0, 0.054, 0.0),
        radius=0.210,
        length=0.008,
        mat="R1_Rubber",
        axis_u="Y",
        segments=32,
        bevel=0.0015,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.060, 0.0),
        radius=0.198,
        length=0.006,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=32,
        bevel=0.001,
        bevel_segments=1,
    )

    # 3. Five concentric heavy copper induction coil windings (`R1_Copper`, y = 0.062 .. 0.098)
    coil_radii = [
        (0.052, 0.034),
        (0.088, 0.070),
        (0.124, 0.106),
        (0.160, 0.142),
        (0.196, 0.176),
    ]
    for r_out, r_in in coil_radii:
        mb.add_ring(
            center_u=(0.0, 0.080, 0.0),
            r_outer=r_out,
            r_inner=r_in,
            height=0.034,
            segs=32,
            axis="y",
            mat="R1_Copper",
        )
        # Upper copper turn crown ring for rounded multi-strand visual richness
        mb.add_ring(
            center_u=(0.0, 0.099, 0.0),
            r_outer=r_out - 0.002,
            r_inner=r_in + 0.002,
            height=0.006,
            segs=32,
            axis="y",
            mat="R1_Copper",
        )

    # 4. Six radial ceramic comb retainers (`R1_CreamEnamel`) clamping the copper windings
    for i in range(6):
        ang_deg = i * 60.0 + 30.0
        rad = math.radians(ang_deg)
        cx = math.cos(rad) * 0.118
        cz = math.sin(rad) * 0.118
        mb.add_bevel_box(
            center_u=(cx, 0.086, cz),
            size_u=(0.156, 0.038, 0.022),
            mat="R1_CreamEnamel",
            bevel=0.003,
            segments=1,
            rot_deg_u=(0.0, -ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=(cx, 0.107, cz),
            radius=0.006,
            length=0.006,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.001,
            bevel_segments=1,
        )

    # 5. Central flux concentrator hub & high-current brass busbar terminal block reaching y = 0.120 m
    mb.add_cylinder(
        center_u=(0.0, 0.082, 0.0),
        radius=0.030,
        length=0.044,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=20,
        bevel=0.003,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.108, 0.0),
        radius=0.020,
        length=0.012,
        mat="R1_Brass",
        axis_u="Y",
        segments=6,
        bevel=0.002,
        bevel_segments=1,
    )

    # Twin ceramic standoff terminals + copper busbars at +Z side reaching exact height 0.120 m
    mb.add_bevel_box(
        center_u=(0.0, 0.084, 0.145),
        size_u=(0.088, 0.036, 0.046),
        mat="R1_CreamEnamel",
        bevel=0.003,
        segments=1,
    )
    for sx in (-0.024, 0.024):
        # Heavy copper busbar strip from winding to terminal post
        mb.add_bevel_box(
            center_u=(sx, 0.104, 0.095),
            size_u=(0.016, 0.008, 0.096),
            mat="R1_Copper",
            bevel=0.0015,
            segments=1,
        )
        # Brass terminal post topping out at y = 0.120 m
        mb.add_cylinder(
            center_u=(sx, 0.111, 0.145),
            radius=0.011,
            length=0.018,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
            bevel_segments=1,
        )

    # Stencilled part-number plate on -Z side of the coil assembly
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.0, 0.103, -0.145),
        width=0.092,
        height=0.042,
        cell_name="SEC_04",
        rot_deg_u=(-90.0, 0.0, 0.0),
        thickness=0.006,
        frame_mat="R1_GreyMetal",
        rivets=True,
    )

    root_obj = ruins_common.to_ruin_object(mb, "PadFragment_CoilPlate", parent=None, origin_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Scan_1", parent=root_obj, pivot_u=(0.0, 0.060, 0.0))
    return root_obj


def build_guide_rail() -> bpy.types.Object:
    """Build `PadFragment_GuideRail` (0.28 m W x 0.90 m L x 0.18 m H rover guide rail section)."""
    mb = r1kit.MeshBuilder("PadFragment_GuideRail", pivot_u=(0.0, 0.0, 0.0))

    # Exact outer envelope: X in [-0.140, +0.140] (0.28 m), Z in [-0.450, +0.450] (0.90 m), Y in [0.000, 0.180] (0.18 m)
    # 1. Heavy structural base flange plate (0.280 m W x 0.032 m H x 0.900 m L)
    mb.add_bevel_box(
        center_u=(0.0, 0.016, 0.0),
        size_u=(0.280, 0.032, 0.900),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=2,
    )
    # Rubber deck seal pad under the base flange
    mb.add_bevel_box(
        center_u=(0.0, 0.004, 0.0),
        size_u=(0.264, 0.008, 0.884),
        mat="R1_Rubber",
        bevel=0.002,
        segments=1,
    )

    # 2. Raised guide curb beam (y = 0.030 .. 0.164, X centered slightly toward +X so -X has wheel rub face)
    mb.add_bevel_box(
        center_u=(0.012, 0.096, 0.0),
        size_u=(0.184, 0.132, 0.876),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=2,
    )
    # Cream-enamel structural cheek panel along outer (+X) side of the rail
    mb.add_bevel_box(
        center_u=(0.098, 0.094, 0.0),
        size_u=(0.024, 0.096, 0.820),
        mat="R1_CreamEnamel",
        bevel=0.004,
        segments=1,
    )

    # 3. Yellow-Black Hazard Edge (`Logs/TASK.md`: "with the pad's yellow-black edge")
    # Alternating high-contrast yellow-gold (`R1_Brass`), safety amber (`R1_OrangeStripe`),
    # and matte-black (`R1_Rubber`) hazard chevron blocks along the top crown (reaching exact y = 0.180 m)
    # and along the inner upper edge of the guide rail!
    n_stripes = 11
    stripe_span_z = 0.858
    stripe_len = stripe_span_z / n_stripes
    for i in range(n_stripes):
        sz = -stripe_span_z * 0.5 + (i + 0.5) * stripe_len
        if i % 2 == 0:
            top_mat = "R1_Brass"
            edge_mat = "R1_OrangeStripe"
        else:
            top_mat = "R1_Rubber"
            edge_mat = "R1_Rubber"

        # Top crown hazard block (y = 0.158 .. 0.180 -> exact 0.180 m top height)
        mb.add_bevel_box(
            center_u=(0.008, 0.169, sz),
            size_u=(0.156, 0.022, stripe_len * 0.96),
            mat=top_mat,
            bevel=0.0025,
            segments=1,
        )
        # Angled inner-upper hazard edge bumper (-X rover side, y = 0.126 .. 0.168)
        mb.add_bevel_box(
            center_u=(-0.076, 0.146, sz),
            size_u=(0.032, 0.038, stripe_len * 0.96),
            mat=edge_mat,
            bevel=0.0025,
            segments=1,
        )

    # Two stencilled yellow-black `HAZARD` plates embedded in the outer cheek for extra graphic punch
    for hz in (-0.22, 0.22):
        ruins_common.add_signage_plate(
            mb,
            center_u=(0.111, 0.096, hz),
            width=0.220,
            height=0.064,
            cell_name="HAZARD",
            rot_deg_u=(0.0, -90.0, 0.0),
            thickness=0.006,
            frame_mat="R1_BareSteel",
            rivets=True,
        )

    # 4. Inner rover wheel rub rail (`R1_BareSteel`) + lower rubber shock bumper (`R1_Rubber`) on -X face
    mb.add_bevel_box(
        center_u=(-0.088, 0.092, 0.0),
        size_u=(0.024, 0.048, 0.864),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(-0.092, 0.050, 0.0),
        size_u=(0.022, 0.028, 0.856),
        mat="R1_Rubber",
        bevel=0.003,
        segments=1,
    )

    # 5. Four heavy vertical gusset brackets & deck anchor bolts along the base flanges
    for gz in (-0.34, -0.11, 0.11, 0.34):
        # Outer (+X) gusset rib
        mb.add_bevel_box(
            center_u=(0.116, 0.064, gz),
            size_u=(0.036, 0.068, 0.026),
            mat="R1_BareSteel",
            bevel=0.0025,
            segments=1,
        )
        # Deck anchor bolts on both -X and +X base flanges
        for bx in (-0.116, 0.122):
            mb.add_cylinder(
                center_u=(bx, 0.036, gz),
                radius=0.011,
                length=0.010,
                mat="R1_Brass",
                axis_u="Y",
                segments=6,
                bevel=0.0015,
                bevel_segments=1,
            )

    # End-cap alignment dowel pins at -Z and +Z ends (kept within [-0.45, +0.45] Z envelope)
    for sz in (-1.0, 1.0):
        mb.add_cylinder(
            center_u=(0.012, 0.096, sz * 0.438),
            radius=0.022,
            length=0.020,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=12,
            bevel=0.002,
            bevel_segments=1,
        )

    root_obj = ruins_common.to_ruin_object(mb, "PadFragment_GuideRail", parent=None, origin_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Scan_1", parent=root_obj, pivot_u=(0.0, 0.090, 0.0))
    return root_obj


def export_pad_fragments(
    out_dir: Path = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props",
) -> list[Path]:
    """Build, UV-unwrap, bake vertex colours, and export both pad blueprint fragment FBXs."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    exported: list[Path] = []

    # 1. PadFragment_CoilPlate
    ruins_common.reset_ruins_scene()
    coil_root = build_coil_plate()
    r1kit.apply_triplanar_uvs(coil_root, texel_scale=0.85)
    ruins_common.apply_signage_uvs(coil_root)
    ruins_common.bake_ruins_vertex_colors(coil_root, footprint_radius=0.35)
    coil_path = out_dir / "PadFragment_CoilPlate.fbx"
    r1kit.export_fbx(coil_root, coil_path)
    exported.append(coil_path)
    print(f"[pad_fragments] Exported {coil_path}")

    # 2. PadFragment_GuideRail
    ruins_common.reset_ruins_scene()
    rail_root = build_guide_rail()
    r1kit.apply_triplanar_uvs(rail_root, texel_scale=0.85)
    ruins_common.apply_signage_uvs(rail_root)
    ruins_common.bake_ruins_vertex_colors(rail_root, footprint_radius=0.50)
    rail_path = out_dir / "PadFragment_GuideRail.fbx"
    r1kit.export_fbx(rail_root, rail_path)
    exported.append(rail_path)
    print(f"[pad_fragments] Exported {rail_path}")

    return exported


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(
        description="Build and export PadFragment_CoilPlate.fbx and PadFragment_GuideRail.fbx."
    )
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props",
    )
    args = parser.parse_args(argv)
    export_pad_fragments(out_dir=args.out_dir)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
