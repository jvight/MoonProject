"""Procedural Blender 5.1 generator for the R1 `StorageRack` structure (`tools/art/blender/storage_rack.py`).

Hierarchy contract (DESIGN.md §3.7 / TASK.md §4):
  StorageRack (root empty at 0,0,0)
   +-- Stage_1           # 4 dark-charcoal vertical bents, 8 bolted feet, front lower bumper rail, rear power socket
   |    +-- Socket_Power_1
   +-- Stage_2           # 2-tier x 3-bay shelf grid beams, rounded-corner cubby frames, warm interior liners
   +-- Stage_3           # Full-height rounded cream-enamel side end-plates, double orange stripe, riveted sage corner gussets
   +-- Stage_4           # 6 counterbalanced tilt-bin cradles with heterogeneous cargo + 6 amber occupancy pip lamps
        +-- Cradle_1..6  # Counterbalanced tilt-bin trays packed with ingots, amber blocks, wire spools & supply crates
        +-- PipLamp_1..6 # 6 glowing amber occupancy indicator lamps above each bay (R1_LampAmber)
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

BAY_XS: tuple[float, ...] = (0.54, 0.0, -0.54)  # Left (+X), Center (0), Right (-X)
BENT_XS: tuple[float, ...] = (0.81, 0.27, -0.27, -0.81)


def _cradle_pivot_u(idx: int) -> tuple[float, float, float]:
    """Return Unity hinge pivot for Cradle_{idx} (idx in 1..6)."""
    col = (idx - 1) % 3
    tier = (idx - 1) // 3
    bx = BAY_XS[col]
    if tier == 0:
        return (bx, 0.22, 0.36)
    return (bx, 0.90, 0.36)


def _pip_pivot_u(idx: int) -> tuple[float, float, float]:
    """Return Unity pivot for PipLamp_{idx} (idx in 1..6)."""
    col = (idx - 1) % 3
    tier = (idx - 1) // 3
    bx = BAY_XS[col]
    if tier == 0:
        return (bx, 0.83, 0.40)
    return (bx, 1.53, 0.40)


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: 4 dark-charcoal vertical bents, 8 bolted feet, lower bumper rail, rear power socket."""
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # --- 4 dark-charcoal vertical frame bents & 8 mounting feet ---
    for bx in BENT_XS:
        for sz in (-0.36, 0.36):
            # Foot pad at ground Y = 0..0.12
            mb.add_bevel_box(
                center_u=(bx, 0.06, sz),
                size_u=(0.085, 0.12, 0.10),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )
            # Vertical column post from Y = 0.12 .. 1.62
            mb.add_bevel_box(
                center_u=(bx, 0.87, sz),
                size_u=(0.068, 1.50, 0.075),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )
        # Front-to-back tie beams at bottom (Y=0.16), middle (Y=0.83), and top (Y=1.54)
        for ty in (0.16, 0.83, 1.54):
            mb.add_bevel_box(
                center_u=(bx, ty, 0.0),
                size_u=(0.058, 0.068, 0.76),
                mat="R1_GreyMetal",
                bevel=0.0,
                segments=0,
            )

    # --- Front lower rubber nudge bumper rail across all 3 bays (`Y = 0.13, Z = +0.40`) ---
    mb.add_bevel_box(
        center_u=(0.0, 0.13, 0.40),
        size_u=(1.68, 0.055, 0.055),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )

    # --- Rear low-voltage power gland box & Socket_Power_1 ---
    mb.add_bevel_box(
        center_u=(-0.54, 0.22, -0.42),
        size_u=(0.18, 0.15, 0.07),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.54, 0.22, -0.465),
        radius=0.034,
        length=0.025,
        mat="R1_Brass",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(-0.54, 0.22, -0.482),
        radius=0.018,
        length=0.016,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    stage_1_obj = mb.build_object(parent=root)

    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(-0.54, 0.22, -0.49),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: 2-tier x 3-bay shelf grid beams, rounded-corner cubby frames, warm interior liners."""
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    # --- Horizontal front & rear shelf beams at bottom (Y=0.16), middle (Y=0.83), and top header (Y=1.54) ---
    for sy, sh in ((0.16, 0.068), (0.83, 0.088), (1.54, 0.095)):
        # Front horizontal header/shelf beam
        mb.add_bevel_box(
            center_u=(0.0, sy, 0.37),
            size_u=(1.68, sh, 0.068),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        # Rear horizontal beam
        mb.add_bevel_box(
            center_u=(0.0, sy, -0.37),
            size_u=(1.68, sh, 0.055),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        # Twin bolt heads on each vertical column intersection on the front beam
        for bx in BENT_XS:
            mb.add_bolts(
                [(bx - 0.015, sy, 0.408), (bx + 0.015, sy, 0.408)],
                radius=0.008,
                height=0.006,
                axis_u="Z",
                mat="R1_BareSteel",
                bevel_segments=0,
            )

    # --- 6 warm-lit interior cubby boxes & rounded-corner front cubby bezels ---
    tier_ys = (0.48, 1.17)
    for ty in tier_ys:
        # Full-width shelf floor plate under the 3 bays of this tier
        mb.add_bevel_box(
            center_u=(0.0, ty - 0.27, 0.0),
            size_u=(1.60, 0.032, 0.70),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        for bx in BAY_XS:
            # Rounded-corner brushed-steel front cubby surround bezel (`Z = +0.36`)
            # Built from 4 frame bars around the 0.46 x 0.52 bay opening
            for s_top in (-1.0, 1.0):
                mb.add_bevel_box(
                    center_u=(bx, ty + s_top * 0.265, 0.365),
                    size_u=(0.46, 0.034, 0.045),
                    mat="R1_BareSteel",
                    bevel=0.0,
                    segments=0,
                )
            for s_side in (-1.0, 1.0):
                mb.add_bevel_box(
                    center_u=(bx + s_side * 0.215, ty, 0.365),
                    size_u=(0.034, 0.50, 0.045),
                    mat="R1_BareSteel",
                    bevel=0.0,
                    segments=0,
                )
            # Warm brass/cream interior back & side liners inside each cubby so the bay reads warm-lit like the concept
            mb.add_bevel_box(
                center_u=(bx, ty, -0.32),
                size_u=(0.44, 0.50, 0.024),
                mat="R1_Brass",
                bevel=0.0,
                segments=0,
            )
            for s_side in (-1.0, 1.0):
                mb.add_bevel_box(
                    center_u=(bx + s_side * 0.21, ty, 0.0),
                    size_u=(0.022, 0.50, 0.64),
                    mat="R1_CreamEnamel",
                    bevel=0.0,
                    segments=0,
                )
            # Warm overhead glow strip inside the top of each cubby ceiling
            mb.add_bevel_box(
                center_u=(bx, ty + 0.24, 0.10),
                size_u=(0.34, 0.018, 0.14),
                mat="R1_LampAmber",
                bevel=0.0,
                segments=0,
            )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Full-height rounded cream-enamel side end-plates, double orange stripe, riveted sage corner gussets."""
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # --- Left (+X) and Right (-X) full-height rounded cream-enamel side end-plates (`X = ±0.855`) ---
    for sx in (-1.0, 1.0):
        ex = sx * 0.855
        # Main vertical rounded-corner cream-enamel end-plate (`Y = 0.10 .. 1.60, Z = -0.40 .. +0.40`)
        mb.add_bevel_box(
            center_u=(ex, 0.85, 0.0),
            size_u=(0.056, 1.50, 0.80),
            mat="R1_CreamEnamel",
            bevel=0.024,
            segments=1,
        )

        # Double horizontal warm-orange beltline stripes (`R1_OrangeStripe`) across the middle of each side end-plate
        for oy in (0.79, 0.88):
            mb.add_bevel_box(
                center_u=(ex + sx * 0.004, oy, 0.0),
                size_u=(0.058, 0.052, 0.806),
                mat="R1_OrangeStripe",
                bevel=0.0,
                segments=0,
            )

        # Riveted muted sage-green triangular/chamfered corner gussets (`R1_SagePatch`) at the corners of each side end-plate
        # Top-front corner gusset (`Y = 1.44, Z = +0.26`), bottom-front (`Y = 0.24, Z = +0.26`), bottom-rear (`Y = 0.24, Z = -0.26`)
        for gy, gz in ((1.44, 0.26), (0.24, 0.26), (0.24, -0.26)):
            mb.add_bevel_box(
                center_u=(ex + sx * 0.008, gy, gz),
                size_u=(0.054, 0.24, 0.24),
                mat="R1_SagePatch",
                bevel=0.008,
                segments=1,
            )
            mb.add_bolts(
                [
                    (ex + sx * 0.036, gy - 0.07, gz - 0.07),
                    (ex + sx * 0.036, gy - 0.07, gz + 0.07),
                    (ex + sx * 0.036, gy + 0.07, gz + (0.07 if gz > 0 else -0.07)),
                ],
                radius=0.009,
                height=0.007,
                axis_u="X",
                mat="R1_BareSteel",
                bevel_segments=0,
            )

        # Edge rivets along the vertical borders of the cream side end-plate
        mb.add_bolts(
            [
                (ex + sx * 0.030, 0.52, 0.34),
                (ex + sx * 0.030, 1.14, 0.34),
                (ex + sx * 0.030, 0.52, -0.34),
                (ex + sx * 0.030, 1.14, -0.34),
                (ex + sx * 0.030, 1.48, -0.34),
            ],
            radius=0.009,
            height=0.007,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _populate_cradle_cargo(mb: r1kit.MeshBuilder, idx: int, px: float, py: float, pz: float) -> None:
    """Pack exact R1 cargo matching s_storage_rack.png into Cradle_{idx}."""
    cy = py + 0.03
    cz = pz - 0.22
    if idx == 1:
        # Bay 1 (Bottom-Left, +X): Deep brushed-steel tilt-bin tub holding a cream & orange supply crate
        # Deep bin side & back walls
        mb.add_bevel_box(
            center_u=(px, cy + 0.14, cz - 0.14),
            size_u=(0.38, 0.26, 0.026),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
        )
        for sx in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(px + sx * 0.18, cy + 0.14, cz),
                size_u=(0.026, 0.26, 0.30),
                mat="R1_BareSteel",
                bevel=0.005,
                segments=1,
            )
            # Pantograph scissor linkage bar on each side of the tilt-bin
            mb.add_bevel_box(
                center_u=(px + sx * 0.20, cy + 0.12, cz + 0.08),
                size_u=(0.018, 0.24, 0.036),
                mat="R1_BareSteel",
                bevel=0.0,
                segments=0,
                rot_deg_u=(28.0, 0.0, 0.0),
            )
        # Cream & orange supply crate inside the bin
        mb.add_supply_crate(
            center_u=(px, cy + 0.16, cz + 0.02),
            size_u=(0.31, 0.22, 0.24),
            rot_deg_u=(0.0, 0.0, 0.0),
            body_mat="R1_CreamEnamel",
            stripe_mat="R1_OrangeStripe",
            hardware_mat="R1_GreyMetal",
        )
    elif idx == 2:
        # Bay 2 (Bottom-Center, 0): Two wound copper wire spools (one upright on left, one horizontal on right)
        mb.add_wire_spool(
            center_u=(px - 0.09, cy + 0.11, cz + 0.02),
            flange_radius=0.105,
            width=0.11,
            axis_u="X",
            rot_deg_u=(0.0, 18.0, 0.0),
            coil_mat="R1_Copper",
            flange_mat="R1_BareSteel",
            hub_mat="R1_Brass",
        )
        mb.add_wire_spool(
            center_u=(px + 0.09, cy + 0.08, cz + 0.02),
            flange_radius=0.095,
            width=0.13,
            axis_u="Y",
            rot_deg_u=(0.0, 0.0, 0.0),
            coil_mat="R1_Copper",
            flange_mat="R1_BareSteel",
            hub_mat="R1_Brass",
        )
    elif idx == 3:
        # Bay 3 (Bottom-Right, -X): Cream & orange crew supply crate sitting squarely in the bay
        mb.add_supply_crate(
            center_u=(px, cy + 0.13, cz + 0.02),
            size_u=(0.34, 0.24, 0.26),
            rot_deg_u=(0.0, -4.0, 0.0),
            body_mat="R1_CreamEnamel",
            stripe_mat="R1_OrangeStripe",
            hardware_mat="R1_GreyMetal",
        )
    elif idx == 4:
        # Bay 4 (Top-Left, +X): Pyramid stack of 6 silver brushed-steel trapezoidal ingots (3 + 2 + 1)
        for ix in (-0.11, 0.0, 0.11):
            mb.add_ingot((px + ix, cy + 0.035, cz + 0.02), (0.098, 0.058, 0.22), "R1_BareSteel", bevel=0.005)
        for ix in (-0.058, 0.058):
            mb.add_ingot((px + ix, cy + 0.092, cz + 0.02), (0.098, 0.058, 0.22), "R1_BareSteel", bevel=0.005)
        mb.add_ingot((px, cy + 0.148, cz + 0.02), (0.098, 0.058, 0.22), "R1_BareSteel", bevel=0.005)
    elif idx == 5:
        # Bay 5 (Top-Center, 0): Pyramid stack of 6 warm amber/brass blocks (3 + 2 + 1)
        for ix in (-0.11, 0.0, 0.11):
            mb.add_ingot((px + ix, cy + 0.035, cz + 0.02), (0.098, 0.058, 0.22), "R1_Brass", bevel=0.005)
        for ix in (-0.058, 0.058):
            mb.add_ingot((px + ix, cy + 0.092, cz + 0.02), (0.098, 0.058, 0.22), "R1_LampAmber", bevel=0.005)
        mb.add_ingot((px, cy + 0.148, cz + 0.02), (0.098, 0.058, 0.22), "R1_LampAmber", bevel=0.005)
    else:
        # Bay 6 (Top-Right, -X): Stacked steel bars & copper plate stacks (left & right columns + top cross bar)
        for sx in (-0.085, 0.085):
            mb.add_bevel_box((px + sx, cy + 0.03, cz + 0.02), (0.13, 0.045, 0.22), "R1_BareSteel", bevel=0.004, segments=1)
            mb.add_bevel_box((px + sx, cy + 0.085, cz + 0.02), (0.13, 0.060, 0.22), "R1_Copper", bevel=0.0, segments=0)
            mb.add_bevel_box((px + sx, cy + 0.14, cz + 0.02), (0.13, 0.045, 0.22), "R1_BareSteel", bevel=0.004, segments=1)
        mb.add_bevel_box((px, cy + 0.19, cz + 0.02), (0.26, 0.048, 0.18), "R1_BareSteel", bevel=0.004, segments=1)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: Cradle_1..6 counterbalanced tilt-bin trays with cargo + PipLamp_1..6 occupancy lamps."""
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. Cradle_1..6 (counterbalanced tilt-bin trays with front tubular grab bars)
    for idx in range(1, 7):
        px, py, pz = _cradle_pivot_u(idx)
        c_mb = r1kit.MeshBuilder(f"Cradle_{idx}", pivot_u=(px, py, pz))

        # Tilt-bin bottom tray plate (`Z = pz - 0.36 .. pz`)
        c_mb.add_bevel_box(
            center_u=(px, py + 0.015, pz - 0.19),
            size_u=(0.40, 0.028, 0.34),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        # Front brushed-steel tubular retaining grab bar across the bottom of the bay opening (matching s_storage_rack.png)
        c_mb.add_cylinder(
            center_u=(px, py + 0.045, pz + 0.01),
            radius=0.012,
            length=0.38,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        for sx in (-1.0, 1.0):
            c_mb.add_bevel_box(
                center_u=(px + sx * 0.18, py + 0.03, pz - 0.01),
                size_u=(0.024, 0.048, 0.05),
                mat="R1_BareSteel",
                bevel=0.0,
                segments=0,
            )

        # Pack exact R1 cargo inside this tilt-bin
        _populate_cradle_cargo(c_mb, idx, px, py, pz)
        c_mb.build_object(parent=stage_4)

    # 2. PipLamp_1..6 (glowing amber occupancy indicator lamps above each bay)
    for idx in range(1, 7):
        lx, ly, lz = _pip_pivot_u(idx)
        p_mb = r1kit.MeshBuilder(f"PipLamp_{idx}", pivot_u=(lx, ly, lz))
        p_mb.add_bevel_box(
            center_u=(lx, ly, lz + 0.005),
            size_u=(0.13, 0.036, 0.022),
            mat="R1_LampAmber",
            bevel=0.004,
            segments=1,
        )
        p_mb.build_object(parent=stage_4)

    return stage_4


def pose_storage_rack_rest(root: bpy.types.Object) -> None:
    """Set StorageRack cradles to their upright stowed rest pose."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name.startswith("Cradle_"):
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_storage_rack_active(root: bpy.types.Object) -> None:
    """Set StorageRack cradles to active dispensing pose (Cradle_1 tilted forward -42 deg toward Rover 07, others -6 deg)."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "Cradle_1":
            obj.rotation_euler = (math.radians(-42.0), 0.0, 0.0)
        elif obj.name.startswith("Cradle_"):
            obj.rotation_euler = (math.radians(-6.0), 0.0, 0.0)


def build_storage_rack() -> bpy.types.Object:
    """Build the complete StorageRack hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("StorageRack", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_storage_rack_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.95)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build StorageRack FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/StorageRack.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_storage_rack()
    tris = r1kit.count_triangles(root)
    print(f"[StorageRack] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        concept_img = Path("D:/Project/MoonProject-wt/agy-concept/Logs/base/s_storage_rack.png")
        r1kit.render_structure_suite(
            root=root,
            slug="storage_rack",
            out_dir=args.renders_dir,
            target_height=1.62,
            footprint_radius=0.95,
            pose_rest_fn=pose_storage_rack_rest,
            pose_active_fn=pose_storage_rack_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
            cam_side_x=-1.0,
            concept_path=concept_img,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
