"""Build script for the R1 UpgradeBay structure (`Assets/_Project/Art/R1/Models/Structures/UpgradeBay.fbx`)."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

# Single table of dimensions taken from DESIGN.md (§1.2, §2, §3.8) and TASK.md (07 v3: 1.48 x 1.59 x 1.76 m)
UPGRADE_BAY_DIMS: dict[str, float] = {
    # Overall footprint and height (DESIGN.md §2 & §3.8)
    "footprint_width": 2.40,
    "footprint_depth": 2.20,
    "portal_height": 2.15,
    "clear_width": 1.80,
    # 07 v3 clearance box (TASK.md: 1.48 m wide, 1.59 m tall, 1.76 m long)
    "rover_v3_width": 1.48,
    "rover_v3_height": 1.59,
    "rover_v3_length": 1.76,
    # Column & base plinth geometry
    "col_center_x": 1.05,
    "col_width_x": 0.30,
    "col_depth_z": 0.68,
    "col_base_y": 0.22,
    "plinth_width_x": 0.46,
    "plinth_depth_z": 1.44,
    "plinth_height_y": 0.11,
    "collar_width_x": 0.36,
    "collar_depth_z": 0.78,
    "collar_height_y": 0.11,
    # Signature warm-orange beltline stripe & sage-green salvage plates
    "stripe_center_y": 1.18,
    "stripe_height": 0.14,
    "patch_center_y": 0.66,
    "patch_width": 0.22,
    "patch_height": 0.36,
    "patch_thick": 0.022,
    # Overhead truss bridge (y = 1.88 .. 2.13 m)
    "bridge_bot_y": 1.90,
    "bridge_top_y": 2.12,
    "bridge_half_span_x": 0.88,
    "bridge_half_depth_z": 0.20,
    # Floor wheel guide rails (clear 1.48 m width in rest pose; close +0.05 m inward in active pose)
    "rail_rest_x": 0.81,
    "rail_slide_inward": 0.05,
    "rail_width": 0.082,
    "rail_height": 0.072,
    "rail_length": 2.12,
    # Overhead hoist & claw (bottom at y = 1.63 m > 1.59 m in rest pose; lowers -0.22 m in active pose)
    "hoist_pivot_y": 1.92,
    "hoist_drop_y": 0.22,
    "claw_pivot_x": 0.068,
    "claw_pivot_y": 1.76,
    "claw_close_deg": 20.0,
    # Side articulated wrench arms (pivoted at x = +-0.90 m, y = 1.15 m, z = 0.04 m)
    "arm_pivot_x": 0.90,
    "arm_pivot_y": 1.15,
    "arm_pivot_z": 0.04,
    "arm_active_swing_deg": 54.0,
    # Floodlamps & right-pillar 3-pip status bezel
    "flood_x": 0.70,
    "flood_y": 1.83,
    "flood_z": 0.04,
    "status_bezel_y": 1.18,
    "status_lamp_1_y": 1.05,
    "status_lamp_step_y": 0.13,
    # Power socket on lower right (-X) plinth
    "socket_power_x": -1.05,
    "socket_power_y": 0.14,
    "socket_power_z": 0.76,
}


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Heavy bolted column base plinths, stepped ankle collars, front hydraulic feed pipes, and Socket_Power_1."""
    d = UPGRADE_BAY_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    col_x = d["col_center_x"]
    plinth_w = d["plinth_width_x"]
    plinth_d = d["plinth_depth_z"]
    plinth_h = d["plinth_height_y"]
    collar_w = d["collar_width_x"]
    collar_d = d["collar_depth_z"]
    collar_h = d["collar_height_y"]

    for side in (-1.0, 1.0):
        sx = side * col_x
        # Heavy dark-charcoal ground plinth plate with bevelled edges
        mb.add_bevel_box(
            center_u=(sx, plinth_h * 0.5, 0.0),
            size_u=(plinth_w, plinth_h, plinth_d),
            mat="R1_GreyMetal",
            bevel=0.016,
            segments=1,
            top_scale_xz=(0.96, 0.98),
        )
        # Stepped dark-charcoal column ankle collar (y = 0.11 .. 0.22 m)
        mb.add_bevel_box(
            center_u=(sx, plinth_h + collar_h * 0.5, 0.0),
            size_u=(collar_w, collar_h, collar_d),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=1,
            top_scale_xz=(0.94, 0.95),
        )
        # Perimeter hex anchor bolts on each base plinth
        bolts: list[tuple[float, float, float]] = []
        for bx_off in (-plinth_w * 0.38, plinth_w * 0.38):
            for bz_off in (-0.60, -0.44, 0.44, 0.60):
                bolts.append((sx + bx_off, plinth_h + 0.006, bz_off))
        mb.add_bolts(
            bolts,
            radius=0.015,
            height=0.012,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

        # Structural rib gussets between plinth and ankle collar on front (+Z) and back (-Z)
        for sz in (-1.0, 1.0):
            for rx_off in (-0.11, 0.11):
                mb.add_strut(
                    p0_u=(sx + rx_off, plinth_h * 0.8, sz * 0.52),
                    p1_u=(sx + rx_off, plinth_h + collar_h * 0.85, sz * 0.36),
                    width=0.028,
                    thickness=0.028,
                    mat="R1_GreyMetal",
                    bevel=0.0,
                )

        # Curved front steel hydraulic feed pipe from plinth toe into lower column front face (matching s_upgrade_bay.png)
        pipe_pts = [
            (sx, plinth_h * 0.9, 0.52),
            (sx, plinth_h + 0.08, 0.50),
            (sx, plinth_h + 0.20, 0.45),
            (sx, plinth_h + 0.30, 0.37),
            (sx, plinth_h + 0.33, 0.30),
        ]
        mb.add_tube_path(pipe_pts, radius=0.024, mat="R1_BareSteel", segments=8, cap_ends=True)
        # Flanged pipe collars at base and column entry
        mb.add_cylinder(
            center_u=(sx, plinth_h + 0.008, 0.52),
            radius=0.042,
            length=0.022,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_cylinder(
            center_u=(sx, plinth_h + 0.33, 0.34),
            radius=0.042,
            length=0.024,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

        # Lateral slide-way sleeper blocks supporting the wheel guide rails at z = -0.58, +0.58
        for sz_way in (-0.58, 0.58):
            way_x = side * ((col_x - plinth_w * 0.5 + d["rail_rest_x"]) * 0.5)
            mb.add_bevel_box(
                center_u=(way_x, 0.022, sz_way),
                size_u=(0.18, 0.044, 0.095),
                mat="R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )
            mb.add_bevel_box(
                center_u=(side * d["rail_rest_x"], 0.042, sz_way),
                size_u=(0.12, 0.014, 0.068),
                mat="R1_BareSteel",
                bevel=0.0,
                segments=0,
            )

    # Heavy power cable gland receptacle on the front-right (-X) plinth toe (matching s_upgrade_bay.png)
    px = d["socket_power_x"]
    py = d["socket_power_y"]
    pz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(px, py, pz - 0.045),
        radius=0.056,
        length=0.065,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.006,
    )
    mb.add_cylinder(
        center_u=(px, py, pz - 0.012),
        radius=0.038,
        length=0.042,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.005,
    )

    stage_1_obj = mb.build_object(parent=root)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(px, py, pz),
        outward_dir_u=(0.0, 0.0, 1.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Inner column service slots, vertical lead-screws, external hydraulic pipe manifold, and overhead Warren truss bridge."""
    d = UPGRADE_BAY_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    col_x = d["col_center_x"]
    col_w = d["col_width_x"]
    inner_x = col_x - col_w * 0.5  # 0.90 m

    # 1. Recessed vertical arm-slot channels on the inner faces (x = +-0.90 m, y = 0.26 .. 1.62 m)
    slot_y_bot = 0.26
    slot_y_top = 1.62
    slot_cy = (slot_y_bot + slot_y_top) * 0.5
    slot_h = slot_y_top - slot_y_bot
    for side in (-1.0, 1.0):
        sx_inner = side * inner_x
        # Bevelled dark-charcoal slot surround frame
        mb.add_bevel_box(
            center_u=(sx_inner + side * 0.018, slot_cy, 0.04),
            size_u=(0.048, slot_h, 0.24),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        # Recessed shadow channel backing
        mb.add_bevel_box(
            center_u=(sx_inner + side * 0.026, slot_cy, 0.04),
            size_u=(0.042, slot_h - 0.08, 0.16),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        # Twin vertical steel guide rods + central brass lead-screw inside the slot
        for z_rod in (-0.01, 0.09):
            mb.add_cylinder(
                center_u=(sx_inner + side * 0.016, slot_cy, z_rod),
                radius=0.012,
                length=slot_h - 0.10,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=8,
                bevel=0.0,
                bevel_segments=0,
            )
        mb.add_cylinder(
            center_u=(sx_inner + side * 0.018, slot_cy, 0.04),
            radius=0.015,
            length=slot_h - 0.10,
            mat="R1_Copper",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        # Slot mounting bolts at top and bottom of the channel frame
        mb.add_bolts(
            [
                (sx_inner - side * 0.004, slot_y_bot + 0.03, -0.04),
                (sx_inner - side * 0.004, slot_y_bot + 0.03, 0.12),
                (sx_inner - side * 0.004, slot_y_top - 0.03, -0.04),
                (sx_inner - side * 0.004, slot_y_top - 0.03, 0.12),
            ],
            radius=0.012,
            height=0.010,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # 2. External vertical hydraulic pipe manifold on the outer viewer-right (-X) column face (matching s_upgrade_bay.png)
    outer_rx = -(col_x + col_w * 0.5)  # -1.20 m
    for z_pipe, mat_pipe in ((-0.16, "R1_GreyMetal"), (-0.02, "R1_GreyMetal"), (0.12, "R1_Copper")):
        pipe_path = [
            (outer_rx + 0.04, 0.20, z_pipe),
            (outer_rx - 0.035, 0.28, z_pipe),
            (outer_rx - 0.035, 1.44, z_pipe),
            (outer_rx + 0.04, 1.50, z_pipe),
        ]
        mb.add_tube_path(pipe_path, radius=0.019, mat=mat_pipe, segments=6, cap_ends=True)
    # 4 horizontal pipe-clamp brackets along the outer right column
    for clamp_y in (0.42, 0.74, 1.06, 1.36):
        mb.add_bevel_box(
            center_u=(outer_rx - 0.024, clamp_y, -0.02),
            size_u=(0.044, 0.036, 0.36),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        mb.add_bolts(
            [
                (outer_rx - 0.046, clamp_y, -0.09),
                (outer_rx - 0.046, clamp_y, 0.05),
            ],
            radius=0.011,
            height=0.008,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # Secondary hydraulic balance line on the outer viewer-left (+X) column back flank
    outer_lx = col_x + col_w * 0.5  # +1.20 m
    for z_pipe in (-0.12, 0.04):
        mb.add_tube_path(
            [
                (outer_lx - 0.04, 0.20, z_pipe),
                (outer_lx + 0.03, 0.28, z_pipe),
                (outer_lx + 0.03, 1.42, z_pipe),
                (outer_lx - 0.04, 1.48, z_pipe),
            ],
            radius=0.017,
            mat="R1_GreyMetal",
            segments=6,
            cap_ends=True,
        )
    for clamp_y in (0.52, 1.16):
        mb.add_bevel_box(
            center_u=(outer_lx + 0.02, clamp_y, -0.04),
            size_u=(0.040, 0.034, 0.24),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )

    # 3. Overhead horizontal Warren truss bridge (x = -0.88 .. +0.88 m, y = 1.90 .. 2.12 m)
    bx = d["bridge_half_span_x"]
    by0 = d["bridge_bot_y"]
    by1 = d["bridge_top_y"]
    bz = d["bridge_half_depth_z"]

    # Bolted end-flange mounting plates where the truss bridge bolts to the upper column shoulders
    for side in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(side * (bx - 0.015), (by0 + by1) * 0.5, 0.0),
            size_u=(0.042, (by1 - by0) + 0.09, bz * 2.0 + 0.08),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        end_bolts: list[tuple[float, float, float]] = []
        for by_b in (by0 - 0.01, (by0 + by1) * 0.5, by1 + 0.01):
            for bz_b in (-bz, bz):
                end_bolts.append((side * (bx - 0.038), by_b, bz_b))
        mb.add_bolts(end_bolts, radius=0.012, height=0.010, axis_u="X", mat="R1_BareSteel", bevel_segments=0)

    # 4 longitudinal main chords of the bridge truss
    for cy_chord in (by0, by1):
        for cz_chord in (-bz, bz):
            mb.add_strut(
                p0_u=(-bx, cy_chord, cz_chord),
                p1_u=(bx, cy_chord, cz_chord),
                width=0.042,
                thickness=0.042,
                mat="R1_GreyMetal",
                bevel=0.0,
            )

    # 6 bays of Warren zig-zag diagonal web struts on front (+Z) and back (-Z) faces + transverse ties
    n_bays = 6
    span_x = bx * 2.0
    bay_w = span_x / n_bays
    for i in range(n_bays + 1):
        x_curr = -bx + i * bay_w
        # Top and bottom transverse ties connecting front and back chords
        mb.add_strut((x_curr, by1, -bz), (x_curr, by1, bz), width=0.026, thickness=0.026, mat="R1_GreyMetal", bevel=0.0)
        mb.add_strut((x_curr, by0, -bz), (x_curr, by0, bz), width=0.026, thickness=0.026, mat="R1_GreyMetal", bevel=0.0)
        if i < n_bays:
            x_next = -bx + (i + 1) * bay_w
            x_mid = 0.5 * (x_curr + x_next)
            for cz_face in (-bz, bz):
                mb.add_strut(
                    (x_curr, by0, cz_face),
                    (x_mid, by1, cz_face),
                    width=0.024,
                    thickness=0.024,
                    mat="R1_GreyMetal",
                    bevel=0.0,
                )
                mb.add_strut(
                    (x_mid, by1, cz_face),
                    (x_next, by0, cz_face),
                    width=0.024,
                    thickness=0.024,
                    mat="R1_GreyMetal",
                    bevel=0.0,
                )

    # Brushed-steel hoist trolley track rails along the bottom chords
    for cz_rail in (-bz * 0.72, bz * 0.72):
        mb.add_bevel_box(
            center_u=(0.0, by0 - 0.024, cz_rail),
            size_u=(span_x - 0.08, 0.020, 0.028),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )

    # 4. Twin downward-angled brass/copper floodlamp shades under the left and right ends of the bridge
    fx = d["flood_x"]
    fy = d["flood_y"]
    fz = d["flood_z"]
    for side in (-1.0, 1.0):
        tilt_z = side * 24.0
        # Mounting stem bracket from bridge bottom chord
        mb.add_bevel_box(
            center_u=(side * fx, by0 - 0.032, fz),
            size_u=(0.065, 0.048, 0.075),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        # Flared copper/brass floodlamp reflector hood
        mb.add_cylinder(
            center_u=(side * fx, fy, fz),
            radius=0.088,
            radius_top=0.046,
            length=0.068,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
            bevel=0.005,
            rot_deg_u=(0.0, 0.0, tilt_z),
        )

    # 5. Stationary central hoist trolley housing bolted under the bridge (matching s_upgrade_bay.png)
    hy = d["hoist_pivot_y"]
    mb.add_bevel_box(
        center_u=(0.0, hy + 0.02, 0.0),
        size_u=(0.25, 0.14, 0.34),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    mb.add_bolts(
        [(-0.075, hy + 0.045, 0.174), (0.075, hy + 0.045, 0.174)],
        radius=0.010,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Cream-enamel portal columns with curved upper shoulders, orange beltline stripe, sage salvage plates, and 3-lamp bezel."""
    d = UPGRADE_BAY_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    col_x = d["col_center_x"]
    col_w = d["col_width_x"]
    col_d = d["col_depth_z"]
    y_base = d["col_base_y"]

    for side in (-1.0, 1.0):
        sx = side * col_x
        # Lower main vertical column shell (y = 0.22 .. 1.50 m)
        low_h = 1.50 - y_base
        mb.add_bevel_box(
            center_u=(sx, y_base + low_h * 0.5, 0.0),
            size_u=(col_w, low_h, col_d),
            mat="R1_CreamEnamel",
            bevel=0.028,
            segments=2,
        )

        # Mid-upper tapered shoulder block (y = 1.50 .. 1.84 m) leaning slightly inward toward the bridge
        mid_h = 0.34
        mb.add_bevel_box(
            center_u=(sx - side * 0.025, 1.50 + mid_h * 0.5, 0.0),
            size_u=(col_w + 0.05, mid_h, col_d - 0.02),
            mat="R1_CreamEnamel",
            bevel=0.024,
            segments=1,
            top_scale_xz=(0.94, 0.94),
        )

        # Upper arched portal crown housing (y = 1.82 .. 2.15 m) curving smoothly into the bridge (matching s_upgrade_bay.png)
        crown_h = d["portal_height"] - 1.82
        mb.add_arch_housing(
            base_center_u=(sx - side * 0.055, 1.82, 0.0),
            width_x=col_w + 0.11,
            height_y=crown_h,
            depth_z=col_d - 0.05,
            arch_radius=0.14,
            mat="R1_CreamEnamel",
            bevel=0.022,
            arch_segments=5,
            arch_axis_u="X",
        )

        # Horizontal panel seam trim bands at y = 1.50 m and y = 1.83 m with corner hex rivets
        for seam_y, seam_x, seam_w, seam_d in (
            (1.50, sx - side * 0.01, col_w + 0.028, col_d + 0.008),
            (1.83, sx - side * 0.04, col_w + 0.088, col_d - 0.032),
        ):
            mb.add_bevel_box(
                center_u=(seam_x, seam_y, 0.0),
                size_u=(seam_w, 0.014, seam_d),
                mat="R1_GreyMetal",
                bevel=0.0,
                segments=0,
            )
            front_z_seam = seam_d * 0.5 + 0.004
            for bx_off in (-seam_w * 0.34, seam_w * 0.34):
                for by_off in (-0.04, 0.04):
                    mb.add_bolts(
                        [(seam_x + bx_off, seam_y + by_off, front_z_seam)],
                        radius=0.011,
                        height=0.008,
                        axis_u="Z",
                        mat="R1_BareSteel",
                        bevel_segments=0,
                    )

        # Continuous horizontal warm-orange beltline stripe (#FF8A5B) wrapping each column at y = 1.18 m
        mb.add_bevel_box(
            center_u=(sx, d["stripe_center_y"], 0.0),
            size_u=(col_w + 0.012, d["stripe_height"], col_d + 0.012),
            mat="R1_OrangeStripe",
            bevel=0.006,
            segments=1,
        )

        # Riveted sage-green salvaged maintenance panel (R1_SagePatch) on the lower front (+Z) face of both columns
        front_z = col_d * 0.5
        py = d["patch_center_y"]
        pw = d["patch_width"]
        ph = d["patch_height"]
        pt = d["patch_thick"]
        mb.add_bevel_box(
            center_u=(sx, py, front_z + pt * 0.4),
            size_u=(pw, ph, pt),
            mat="R1_SagePatch",
            bevel=0.008,
            segments=1,
        )
        patch_rivets: list[tuple[float, float, float]] = []
        for rx_off in (-pw * 0.36, pw * 0.36):
            for ry_off in (-ph * 0.38, ph * 0.38):
                patch_rivets.append((sx + rx_off, py + ry_off, front_z + pt + 0.002))
        mb.add_bolts(patch_rivets, radius=0.012, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

        # Rear (-Z) ventilation louver grille on each column
        mb.add_vent_grille(
            center_u=(sx, 0.88, -front_z - 0.006),
            width=0.18,
            height=0.26,
            depth=0.024,
            slat_count=2,
            rot_deg_u=(0.0, 180.0, 0.0),
            frame_mat="R1_GreyMetal",
            back_mat="R1_DarkGlass",
        )

    # Riveted sage-green salvaged plate on the front of the central hoist trolley housing
    hy = d["hoist_pivot_y"]
    mb.add_bevel_box(
        center_u=(0.0, hy + 0.035, 0.172),
        size_u=(0.21, 0.076, 0.018),
        mat="R1_SagePatch",
        bevel=0.005,
        segments=1,
    )

    # Vertical 3-lamp status bezel on the front (+Z) face of the viewer-right (-X) column (matching s_upgrade_bay.png)
    rx = -col_x
    front_z = col_d * 0.5
    by_bezel = d["status_bezel_y"]
    mb.add_bevel_box(
        center_u=(rx, by_bezel, front_z + 0.012),
        size_u=(0.15, 0.44, 0.028),
        mat="R1_GreyMetal",
        bevel=0.009,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(rx, by_bezel, front_z + 0.018),
        size_u=(0.118, 0.40, 0.022),
        mat="R1_DarkGlass",
        bevel=0.0,
        segments=0,
    )
    # Bezel rings around each of the 3 status lamp positions
    for idx in range(3):
        ly = d["status_lamp_1_y"] + idx * d["status_lamp_step_y"]
        mb.add_cylinder(
            center_u=(rx, ly, front_z + 0.026),
            radius=0.044,
            length=0.012,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_wrench_arm(name: str, side: float, parent: bpy.types.Object) -> bpy.types.Object:
    """Build one articulated copper/brass & steel wrench arm (`WrenchArm_L` for +1, `WrenchArm_R` for -1)."""
    d = UPGRADE_BAY_DIMS
    px = side * d["arm_pivot_x"]
    py = d["arm_pivot_y"]
    pz = d["arm_pivot_z"]

    mb = r1kit.MeshBuilder(name, pivot_u=(px, py, pz))

    # Build in stowed/clear posture (reaching slightly inward to |x| = 0.77 m > 0.74 m and upward/forward)
    # so the rest pose clears the full 1.48 x 1.59 x 1.76 m box, and rotating inward in pose_active reaches 07's hull!
    # 1. Shoulder clevis mount & rotary barrel at (px, py, pz)
    mb.add_bevel_box(
        center_u=(px + side * 0.01, py, pz),
        size_u=(0.068, 0.14, 0.11),
        mat="R1_Copper",
        bevel=0.007,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(px - side * 0.015, py, pz),
        radius=0.038,
        length=0.095,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
    )
    mb.add_cylinder(
        center_u=(px - side * 0.015, py, pz),
        radius=0.020,
        length=0.115,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 2. Upper arm boom reaching from shoulder (|x|=0.89, y=1.15) to elbow at (|x|=0.80, y=1.36, z=0.06)
    elbow_u = (side * 0.80, py + 0.21, pz + 0.02)
    mb.add_strut(
        p0_u=(px - side * 0.015, py, pz),
        p1_u=elbow_u,
        width=0.044,
        thickness=0.048,
        mat="R1_Copper",
        bevel=0.005,
    )
    # Hydraulic damper cylinder under upper arm
    mb.add_pipe(
        p0_u=(px - side * 0.01, py - 0.055, pz),
        p1_u=(side * 0.83, py + 0.11, pz + 0.01),
        radius=0.018,
        mat="R1_Copper",
        segments=8,
    )
    mb.add_pipe(
        p0_u=(side * 0.83, py + 0.11, pz + 0.01),
        p1_u=(elbow_u[0], elbow_u[1] - 0.02, elbow_u[2]),
        radius=0.011,
        mat="R1_BareSteel",
        segments=8,
    )

    # 3. Elbow drum joint with steel hinge pin
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.036,
        length=0.078,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
    )
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.018,
        length=0.094,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 4. Forearm tube reaching from elbow to wrist at (|x|=0.785, y=1.06, z=0.14) — stays outside |x|=0.745 m in rest pose!
    wrist_u = (side * 0.785, py - 0.09, pz + 0.10)
    mb.add_pipe(
        p0_u=elbow_u,
        p1_u=wrist_u,
        radius=0.022,
        mat="R1_Copper",
        segments=8,
    )
    # Forearm collar ring
    mid_fore = tuple(elbow_u[k] * 0.45 + wrist_u[k] * 0.55 for k in range(3))
    mb.add_cylinder(
        center_u=mid_fore,
        radius=0.030,
        length=0.036,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 5. Wrist rotary bolt-driver & C-jaw wrench/welder head at wrist_u
    mb.add_cylinder(
        center_u=wrist_u,
        radius=0.028,
        length=0.056,
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    for jaw_z in (-0.028, 0.028):
        mb.add_bevel_box(
            center_u=(wrist_u[0] - side * 0.012, wrist_u[1] - 0.048, wrist_u[2] + jaw_z),
            size_u=(0.022, 0.058, 0.018),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )

    return mb.build_object(parent=parent)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: Moving parts (`GuideRail_L/R`, `Hoist` -> `HoistClaw_L/R`, `WrenchArm_L/R`) and lamps (`FloodLamp_L/R`, `StatusLamp_1..3`)."""
    d = UPGRADE_BAY_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. GuideRail_L (+X) and GuideRail_R (-X)
    rail_x = d["rail_rest_x"]
    rail_w = d["rail_width"]
    rail_h = d["rail_height"]
    rail_l = d["rail_length"]
    for side, r_name in ((1.0, "GuideRail_L"), (-1.0, "GuideRail_R")):
        rx = side * rail_x
        r_mb = r1kit.MeshBuilder(r_name, pivot_u=(rx, rail_h * 0.5, 0.0))
        # Bottom flange of I-beam guide rail
        r_mb.add_bevel_box(
            center_u=(rx, 0.016, 0.0),
            size_u=(rail_w, 0.024, rail_l),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )
        # Vertical web + upper guide lip
        r_mb.add_bevel_box(
            center_u=(rx, rail_h * 0.55, 0.0),
            size_u=(rail_w * 0.52, rail_h * 0.78, rail_l - 0.04),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        r_mb.add_bevel_box(
            center_u=(rx, rail_h - 0.012, 0.0),
            size_u=(rail_w * 0.88, 0.022, rail_l - 0.02),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )
        # Inner brushed-steel tyre rub strip
        r_mb.add_bevel_box(
            center_u=(rx - side * (rail_w * 0.42), rail_h * 0.62, 0.0),
            size_u=(0.014, 0.028, rail_l - 0.14),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )
        # Chock bumpers on top of rail at z = +-0.58 m
        for sz_chock in (-0.58, 0.58):
            r_mb.add_bevel_box(
                center_u=(rx, rail_h + 0.010, sz_chock),
                size_u=(rail_w * 0.82, 0.020, 0.14),
                mat="R1_GreyMetal",
                bevel=0.0,
                segments=0,
            )
        r_mb.build_object(parent=stage_4)

    # 2. Telescoping overhead Hoist plunger + vertical lead-screw + HoistClaw_L / HoistClaw_R
    hy = d["hoist_pivot_y"]
    hoist_mb = r1kit.MeshBuilder("Hoist", pivot_u=(0.0, hy, 0.0))
    # Twin vertical guide rods + central threaded lead-screw extending up to y = 2.06 m inside the trolley housing
    rod_top_y = hy + 0.14
    rod_bot_y = 1.77
    rod_cy = 0.5 * (rod_top_y + rod_bot_y)
    rod_len = rod_top_y - rod_bot_y
    for rx_rod in (-0.046, 0.046):
        hoist_mb.add_cylinder(
            center_u=(rx_rod, rod_cy, 0.0),
            radius=0.012,
            length=rod_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    hoist_mb.add_cylinder(
        center_u=(0.0, rod_cy, 0.0),
        radius=0.018,
        length=rod_len,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    # Machined lead-screw thread rings along the telescoping shaft
    for t_idx in range(4):
        ty = 1.80 + t_idx * 0.045
        hoist_mb.add_cylinder(
            center_u=(0.0, ty, 0.0),
            radius=0.024,
            length=0.010,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    # Lower clevis wrist block above the claw fingers (y = 1.74 .. 1.80 m)
    hoist_mb.add_bevel_box(
        center_u=(0.0, 1.77, 0.0),
        size_u=(0.17, 0.068, 0.11),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    hoist_mb.add_bolts(
        [(-0.052, 1.77, 0.058), (0.052, 1.77, 0.058)],
        radius=0.011,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )
    hoist_obj = hoist_mb.build_object(parent=stage_4)

    # HoistClaw_L (+X) and HoistClaw_R (-X): curved pincer fingers with lowest point at y = 1.628 m (> 1.59 m!)
    cx_piv = d["claw_pivot_x"]
    cy_piv = d["claw_pivot_y"]
    for side, c_name in ((1.0, "HoistClaw_L"), (-1.0, "HoistClaw_R")):
        px_c = side * cx_piv
        c_mb = r1kit.MeshBuilder(c_name, pivot_u=(px_c, cy_piv, 0.0))
        # Upper knuckle
        c_mb.add_cylinder(
            center_u=(px_c, cy_piv, 0.0),
            radius=0.022,
            length=0.072,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        # Upper outward jaw link (y = 1.76 -> 1.69)
        c_mb.add_strut(
            p0_u=(px_c, cy_piv, 0.0),
            p1_u=(side * 0.125, 1.69, 0.0),
            width=0.028,
            thickness=0.052,
            mat="R1_BareSteel",
            bevel=0.004,
        )
        # Lower inward pincer tip (y = 1.69 -> 1.628 m, strictly > 1.59 m in rest pose!)
        c_mb.add_strut(
            p0_u=(side * 0.125, 1.69, 0.0),
            p1_u=(side * 0.075, 1.628, 0.0),
            width=0.024,
            thickness=0.046,
            mat="R1_BareSteel",
            bevel=0.004,
        )
        c_mb.build_object(parent=hoist_obj)

    # 3. Side articulated wrench arms: WrenchArm_L (+X) and WrenchArm_R (-X)
    _build_wrench_arm("WrenchArm_L", side=1.0, parent=stage_4)
    _build_wrench_arm("WrenchArm_R", side=-1.0, parent=stage_4)

    # 4. Lamp meshes using ONLY R1_LampAmber: FloodLamp_L, FloodLamp_R, StatusLamp_1..3
    fx = d["flood_x"]
    fy = d["flood_y"]
    fz = d["flood_z"]
    for side, f_name in ((1.0, "FloodLamp_L"), (-1.0, "FloodLamp_R")):
        tilt_z = side * 24.0
        f_mb = r1kit.MeshBuilder(f_name, pivot_u=(side * fx, fy - 0.02, fz))
        f_mb.add_cylinder(
            center_u=(side * fx - side * 0.008, fy - 0.022, fz),
            radius=0.068,
            radius_top=0.052,
            length=0.022,
            mat="R1_LampAmber",
            axis_u="Y",
            segments=10,
            bevel=0.004,
            rot_deg_u=(0.0, 0.0, tilt_z),
        )
        f_mb.build_object(parent=stage_4)

    rx = -d["col_center_x"]
    front_z = d["col_depth_z"] * 0.5 + 0.028
    for idx in range(3):
        s_name = f"StatusLamp_{idx + 1}"
        ly = d["status_lamp_1_y"] + idx * d["status_lamp_step_y"]
        s_mb = r1kit.MeshBuilder(s_name, pivot_u=(rx, ly, front_z))
        s_mb.add_cylinder(
            center_u=(rx, ly, front_z),
            radius=0.032,
            length=0.016,
            mat="R1_LampAmber",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        s_mb.build_object(parent=stage_4)

    return stage_4


def pose_upgrade_bay_rest(root: bpy.types.Object) -> None:
    """Set UpgradeBay moving parts to rest pose (rails open, hoist raised above 1.59 m, wrench arms retracted)."""
    d = UPGRADE_BAY_DIMS
    rail_x = d["rail_rest_x"]
    rail_h = d["rail_height"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "GuideRail_L":
            obj.location = r1kit.u_pos(rail_x, rail_h * 0.5, 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "GuideRail_R":
            obj.location = r1kit.u_pos(-rail_x, rail_h * 0.5, 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Hoist":
            obj.location = r1kit.u_pos(0.0, d["hoist_pivot_y"], 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name in ("HoistClaw_L", "HoistClaw_R", "WrenchArm_L", "WrenchArm_R"):
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_upgrade_bay_active(root: bpy.types.Object) -> None:
    """Set UpgradeBay moving parts to active fitting pose (rails closed +0.05 m, hoist lowered -0.18 m, claws & arms swung inward)."""
    d = UPGRADE_BAY_DIMS
    rail_x = d["rail_rest_x"] - d["rail_slide_inward"]
    rail_h = d["rail_height"]
    swing = d["arm_active_swing_deg"]
    claw_deg = d["claw_close_deg"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "GuideRail_L":
            obj.location = r1kit.u_pos(rail_x, rail_h * 0.5, 0.0)
        elif obj.name == "GuideRail_R":
            obj.location = r1kit.u_pos(-rail_x, rail_h * 0.5, 0.0)
        elif obj.name == "Hoist":
            obj.location = r1kit.u_pos(0.0, d["hoist_pivot_y"] - 0.16, 0.0)
        elif obj.name == "HoistClaw_L":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, -claw_deg)
        elif obj.name == "HoistClaw_R":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, claw_deg)
        elif obj.name == "WrenchArm_L":
            obj.rotation_euler = r1kit.u_rot(0.0, 14.0, swing)
        elif obj.name == "WrenchArm_R":
            obj.rotation_euler = r1kit.u_rot(0.0, -14.0, -swing)


def _verify_rover_v3_clearance(root: bpy.types.Object) -> None:
    """Verify that in rest pose no vertex of UpgradeBay intersects 07 v3's 1.48 x 1.59 x 1.76 m bounding box."""
    d = UPGRADE_BAY_DIMS
    hx = d["rover_v3_width"] * 0.5   # 0.74 m
    hy = d["rover_v3_height"]         # 1.59 m
    hz = d["rover_v3_length"] * 0.5  # 0.88 m

    pose_upgrade_bay_rest(root)
    bpy.context.view_layer.update()

    violations: list[str] = []
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        wm = obj.matrix_world
        for v in obj.data.vertices:
            wp = wm @ v.co
            # Blender world space: x = x_u, y = z_u, z = y_u
            xu, yu, zu = wp.x, wp.z, wp.y
            if abs(xu) < hx - 1e-3 and 0.05 < yu < hy - 1e-3 and abs(zu) < hz - 1e-3:
                violations.append(f"{obj.name} at Unity ({xu:.3f}, {yu:.3f}, {zu:.3f})")
                break
    if violations:
        raise RuntimeError(f"UpgradeBay violates 07 v3 (1.48x1.59x1.76 m) clearance: {violations}")


def render_upgrade_bay_scale_and_compare(root: bpy.types.Object, out_dir: Path) -> None:
    """Render the UpgradeBay scale shot with a 1.48 x 1.59 x 1.76 m box for 07 inside and side-by-side concept comparison."""
    d = UPGRADE_BAY_DIMS
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    r1kit.ensure_materials(use_vertex_color_shading=True)
    temp_objs = r1kit._setup_preview_lighting_and_plinth(d["footprint_width"] * 0.62)

    cam_data = bpy.data.cameras.new("_ScaleCam")
    cam_obj = bpy.data.objects.new("_ScaleCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.append(cam_obj)

    # Build a 1.48 x 1.59 x 1.76 m calibration box + 07 stand-in inside the bay at (0, 0, 0)
    pose_upgrade_bay_rest(root)
    rover_obj = r1kit.build_rover_07_standin(
        pos_u=(0.0, 0.0, 0.0),
        rover_width=d["rover_v3_width"],
        rover_height=d["rover_v3_height"],
        rover_length=d["rover_v3_length"],
    )
    # Add exact 1.48 x 1.59 x 1.76 m wireframe bounding-box cage around 07 so clearance is visually explicit
    cage_mb = r1kit.MeshBuilder("_RoverV3ClearanceBox", pivot_u=(0.0, 0.0, 0.0))
    hx = d["rover_v3_width"] * 0.5
    hy = d["rover_v3_height"]
    hz = d["rover_v3_length"] * 0.5
    corners_bot = [(-hx, 0.01, -hz), (hx, 0.01, -hz), (hx, 0.01, hz), (-hx, 0.01, hz)]
    corners_top = [(-hx, hy, -hz), (hx, hy, -hz), (hx, hy, hz), (-hx, hy, hz)]
    for i in range(4):
        j = (i + 1) % 4
        cage_mb.add_strut(corners_bot[i], corners_bot[j], 0.012, 0.012, "R1_LampAmber", bevel=0.0)
        cage_mb.add_strut(corners_top[i], corners_top[j], 0.012, 0.012, "R1_LampAmber", bevel=0.0)
        cage_mb.add_strut(corners_bot[i], corners_top[i], 0.012, 0.012, "R1_LampAmber", bevel=0.0)
    cage_obj = cage_mb.build_object()
    r1kit._init_solid_vcol(cage_obj, (1.0, 0.0, 0.0, 1.0))
    temp_objs.extend([rover_obj, cage_obj])

    bpy.context.view_layer.update()
    mid_y = d["portal_height"] * 0.48
    dist = d["portal_height"] * 2.25
    r1kit._aim_camera_at(
        cam_obj,
        (dist * 0.36, mid_y + dist * 0.22, dist * 0.96),
        (0.0, mid_y * 0.92, 0.0),
        lens_mm=46.0,
    )
    p_scale = out_dir / "upgrade_bay_scale_07.png"
    r1kit._render_to_file(p_scale, res_x=1120, res_y=840)

    # Also render a hero active shot matching s_upgrade_bay.png (with compact v1/v2 rover stand-in so the arms and hoist frame clearly)
    cage_obj.hide_render = True
    rover_obj.scale = (0.76, 0.76, 0.72)
    pose_upgrade_bay_active(root)
    bpy.context.view_layer.update()
    p_hero = out_dir / "_tmp_upgrade_bay_concept_match.png"
    r1kit._render_to_file(p_hero, res_x=1120, res_y=840)

    concept_path = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\s_upgrade_bay.png")
    if concept_path.exists():
        r1kit.compose_side_by_side(
            concept_path,
            p_hero,
            out_dir / "compare_upgrade_bay.png",
            target_height=768,
        )
    p_hero.unlink(missing_ok=True)

    pose_upgrade_bay_rest(root)
    bpy.context.view_layer.update()

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    plinth_mat = bpy.data.materials.get("_PreviewPlinthMat")
    if plinth_mat is not None:
        bpy.data.materials.remove(plinth_mat)
    r1kit.ensure_materials(use_vertex_color_shading=False)


def build_upgrade_bay() -> bpy.types.Object:
    """Build the complete UpgradeBay hierarchy in the current Blender scene, verify clearance, and bake vertex colours."""
    root = r1kit.create_empty("UpgradeBay", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    _verify_rover_v3_clearance(root)
    pose_upgrade_bay_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=UPGRADE_BAY_DIMS["footprint_width"] * 0.58)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build UpgradeBay FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/UpgradeBay.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_upgrade_bay()
    tris = r1kit.count_triangles(root)
    print(f"[UpgradeBay] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        render_upgrade_bay_scale_and_compare(root, args.renders_dir)
        r1kit.render_structure_suite(
            root=root,
            slug="upgrade_bay",
            out_dir=args.renders_dir,
            target_height=UPGRADE_BAY_DIMS["portal_height"],
            footprint_radius=UPGRADE_BAY_DIMS["footprint_width"] * 0.55,
            pose_rest_fn=pose_upgrade_bay_rest,
            pose_active_fn=pose_upgrade_bay_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
