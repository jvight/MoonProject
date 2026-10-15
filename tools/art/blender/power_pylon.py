"""Build script for the R1 PowerPylon structure (`Assets/_Project/Art/R1/Models/Structures/PowerPylon.fbx`)."""

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

# Single table of dimensions taken from DESIGN.md (sections 1.2, 2, and 3.4)
POWER_PYLON_DIMS: dict[str, float] = {
    # Overall footprint and heights (DESIGN.md §2 & §3.4)
    "footprint_width": 0.85,
    "footprint_depth": 0.85,
    "waist_height": 0.82,
    "total_height": 1.95,
    # Stage 1: Tripod outrigger legs & central inner core
    "footpad_radial_dist": 0.34,
    "footpad_radius": 0.068,
    "footpad_height": 0.024,
    "leg_boss_radius": 0.25,
    "leg_boss_y": 0.26,
    "core_pipe_radius": 0.075,
    "core_pipe_top_y": 0.80,
    # Stage 2: Cream-enamel cylindrical junction drum (y = 0.16..0.82 m)
    "drum_bottom_y": 0.16,
    "drum_top_y": 0.82,
    "drum_radius": 0.235,
    "stripe_center_y": 0.56,
    "stripe_height": 0.072,
    "patch_center_y": 0.53,
    "patch_height": 0.22,
    "patch_angle_deg": -22.0,
    "patch_span_deg": 42.0,
    "socket_y": 0.30,
    "socket_radial_dist": 0.285,
    # Stage 3: Tapered open-lattice A-frame post & gooseneck lantern hood
    "mast_base_y": 0.84,
    "mast_top_y": 1.62,
    "mast_bottom_half_w": 0.115,
    "mast_top_half_w": 0.058,
    "mast_chord_thick": 0.022,
    "insulator_arm_y": 1.52,
    "insulator_x": 0.135,
    "switch_box_y": 1.24,
    "gooseneck_peak_y": 1.95,
    "lantern_hood_y": 1.71,
    "lantern_hood_z": 0.30,
    "lantern_hood_radius": 0.135,
    # Stage 4: Moving parts (CableDrum_1..3, KnifeSwitch, Yoke, PathLamp)
    "cable_drum_y": 0.36,
    "cable_drum_radial_dist": 0.295,
    "cable_drum_flange_r": 0.082,
    "cable_drum_width": 0.075,
    "knife_switch_rest_deg": -28.0,
    "knife_switch_delta_deg": 45.0,
    "yoke_pivot_y": 1.62,
    "yoke_active_yaw_deg": 60.0,
    "cable_drum_active_spin_deg": 110.0,
}

# Tripod legs at 0 deg (front +Z), 120 deg, 240 deg; sockets & cable drums interleaved at 60, 180, 300 deg
LEG_ANGLES_DEG: tuple[float, float, float] = (20.0, 140.0, 260.0)
SOCKET_ANGLES_DEG: tuple[float, float, float] = (-20.0, 100.0, 220.0)
DRUM_ANGLES_DEG: tuple[float, float, float] = (75.0, 195.0, 315.0)


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Three splayed dark-charcoal tripod outrigger legs with bolted circular footpads and central pipe core."""
    d = POWER_PYLON_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # Central inner structural pipe core inside the drum (y = 0.20..0.80 m) so the underside of the drum stays clean
    core_bot_y = 0.20
    core_top_y = d["core_pipe_top_y"]
    mb.add_cylinder(
        center_u=(0.0, (core_bot_y + core_top_y) * 0.5, 0.0),
        radius=d["core_pipe_radius"],
        length=core_top_y - core_bot_y,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    r_foot = d["footpad_radial_dist"]
    r_pad = d["footpad_radius"]
    h_pad = d["footpad_height"]
    r_boss = d["leg_boss_radius"]
    y_boss = d["leg_boss_y"]

    for ang_deg in LEG_ANGLES_DEG:
        ang = math.radians(ang_deg)
        sx = math.sin(ang)
        cz = math.cos(ang)

        fx = r_foot * sx
        fz = r_foot * cz

        # Circular dark-charcoal footpad disc
        mb.add_cylinder(
            center_u=(fx, h_pad * 0.5, fz),
            radius=r_pad,
            length=h_pad,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )

        # 3 anchor bolts around each footpad
        bolts: list[tuple[float, float, float]] = []
        for b_idx in range(3):
            b_ang = ang + math.radians(b_idx * 120.0 + 60.0)
            bolts.append(
                (
                    fx + (r_pad * 0.65) * math.sin(b_ang),
                    h_pad + 0.005,
                    fz + (r_pad * 0.65) * math.cos(b_ang),
                )
            )
        mb.add_bolts(
            bolts,
            radius=0.010,
            height=0.008,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

        # Vertical ankle socket + splayed leg strut + upper mounting boss on the drum side (matching s_power_pylon.png)
        mb.add_cylinder(
            center_u=(fx, h_pad + 0.045, fz),
            radius=0.022,
            length=0.09,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_strut(
            p0_u=(fx, h_pad + 0.08, fz),
            p1_u=(r_boss * sx, y_boss, r_boss * cz),
            width=0.036,
            thickness=0.036,
            mat="R1_GreyMetal",
            bevel=0.004,
        )
        # Spider tie-bar connecting leg boss to central inner pipe core for Stage_1 structural integrity
        mb.add_strut(
            p0_u=(r_boss * sx, y_boss, r_boss * cz),
            p1_u=(0.0, y_boss, 0.0),
            width=0.032,
            thickness=0.032,
            mat="R1_GreyMetal",
            bevel=0.0,
        )
        mb.add_bevel_box(
            center_u=((r_boss - 0.015) * sx, y_boss, (r_boss - 0.015) * cz),
            size_u=(0.068, 0.11, 0.060),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_bolts(
            [((r_boss + 0.018) * sx, y_boss + 0.025, (r_boss + 0.018) * cz)],
            radius=0.011,
            height=0.010,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Cream-enamel junction drum, warm-orange band, sage-green salvage patch, and 3 brass power sockets."""
    d = POWER_PYLON_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    y_bot = d["drum_bottom_y"]
    y_top = d["drum_top_y"]
    drum_h = y_top - y_bot
    drum_cy = (y_bot + y_top) * 0.5
    drum_r = d["drum_radius"]

    # Main cylindrical cream-enamel junction drum with generously bevelled top/bottom shoulders
    mb.add_cylinder(
        center_u=(0.0, drum_cy, 0.0),
        radius=drum_r,
        length=drum_h,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=14,
        bevel=0.042,
        bevel_segments=1,
    )

    # Upper seam collar ring and perimeter bolts on top shoulder of drum (matching s_power_pylon.png)
    mb.add_cylinder(
        center_u=(0.0, y_top - 0.055, 0.0),
        radius=drum_r + 0.005,
        length=0.016,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=14,
        bevel=0.0,
        bevel_segments=0,
    )
    top_bolts: list[tuple[float, float, float]] = []
    for i in range(6):
        b_ang = math.radians(i * 60.0 + 15.0)
        top_bolts.append(
            (
                (drum_r - 0.025) * math.sin(b_ang),
                y_top + 0.004,
                (drum_r - 0.025) * math.cos(b_ang),
            )
        )
    mb.add_bolts(
        top_bolts,
        radius=0.010,
        height=0.008,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # Single continuous warm-orange band (#FF8A5B) wrapping around the cream drum
    mb.add_cylinder(
        center_u=(0.0, d["stripe_center_y"], 0.0),
        radius=drum_r + 0.006,
        length=d["stripe_height"],
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=14,
        bevel=0.0,
        bevel_segments=0,
    )

    # Curved sage-green salvage patch plate (#7F9584) riveted to the drum (matching s_power_pylon.png)
    patch_cy = d["patch_center_y"]
    patch_h = d["patch_height"]
    patch_ang = d["patch_angle_deg"]
    patch_span = d["patch_span_deg"]
    mb.add_curved_patch(
        center_y=patch_cy,
        height_y=patch_h,
        cyl_radius=drum_r + 0.004,
        thickness=0.016,
        angle_center_deg=patch_ang,
        angle_span_deg=patch_span,
        mat="R1_SagePatch",
        arc_segments=4,
        bevel=0.004,
    )

    # 4 corner rivets on the curved sage-green salvage patch
    r_rivet = drum_r + 0.022
    for dy_sign in (-1.0, 1.0):
        for da_sign in (-1.0, 1.0):
            r_ang = math.radians(patch_ang + da_sign * (patch_span * 0.38))
            ry = patch_cy + dy_sign * (patch_h * 0.38)
            mb.add_cylinder(
                center_u=(r_rivet * math.sin(r_ang), ry, r_rivet * math.cos(r_ang)),
                radius=0.010,
                length=0.010,
                mat="R1_BareSteel",
                axis_u="Z",
                segments=6,
                bevel=0.0,
                bevel_segments=0,
                rot_deg_u=(0.0, math.degrees(r_ang), 0.0),
            )

    # 3 heavy brass/copper power plug receptacles spaced at 120 deg around the lower drum
    sock_y = d["socket_y"]
    for ang_deg in SOCKET_ANGLES_DEG:
        ang = math.radians(ang_deg)
        sx = math.sin(ang)
        cz = math.cos(ang)

        # Grey mounting flange collar
        mb.add_cylinder(
            center_u=((drum_r + 0.010) * sx, sock_y, (drum_r + 0.010) * cz),
            radius=0.068,
            length=0.036,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        # Heavy brass/copper plug receptacle barrel
        mb.add_cylinder(
            center_u=((drum_r + 0.036) * sx, sock_y, (drum_r + 0.036) * cz),
            radius=0.048,
            radius_top=0.044,
            length=0.048,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

    stage_2_obj = mb.build_object(parent=root)

    # Create Socket_Power_1, Socket_Power_2, Socket_Power_3 empties (+Z pointing outward)
    sock_r = d["socket_radial_dist"]
    for idx, ang_deg in enumerate(SOCKET_ANGLES_DEG, start=1):
        ang = math.radians(ang_deg)
        sx = math.sin(ang)
        cz = math.cos(ang)
        r1kit.create_empty(
            name=f"Socket_Power_{idx}",
            parent=stage_2_obj,
            pivot_u=(sock_r * sx, sock_y, sock_r * cz),
            outward_dir_u=(sx, 0.0, cz),
        )

    return stage_2_obj


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Tapered open-lattice A-frame post, twin cream ceramic insulators, switch box, and curved gooseneck copper lantern hood."""
    d = POWER_PYLON_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    y0 = d["mast_base_y"]
    y1 = d["mast_top_y"]
    hw0 = d["mast_bottom_half_w"]
    hw1 = d["mast_top_half_w"]
    chord_t = d["mast_chord_thick"]

    # Base pedestal flange on top of the cream drum
    mb.add_cylinder(
        center_u=(0.0, y0 - 0.010, 0.0),
        radius=0.175,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_bevel_box(
        center_u=(0.0, y0 + 0.012, 0.0),
        size_u=(hw0 * 2.25, 0.026, hw0 * 2.25),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        top_scale_xz=(0.94, 0.94),
    )

    # 4 tapered corner angle-iron chords of the open-lattice post
    corners = [(-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0)]
    bot_pts = [(sx * hw0, y0 + 0.02, sz * hw0) for sx, sz in corners]
    top_pts = [(sx * hw1, y1, sz * hw1) for sx, sz in corners]

    for b_pt, t_pt in zip(bot_pts, top_pts):
        mb.add_strut(b_pt, t_pt, width=chord_t, thickness=chord_t, mat="R1_GreyMetal", bevel=0.0)

    # X-cross diagonal lattice bracing across 3 vertical bays on all 4 faces (matching s_power_pylon.png)
    n_bays = 3
    for bay in range(n_bays):
        t_low = bay / n_bays
        t_high = (bay + 1) / n_bays
        for side_idx in range(4):
            next_idx = (side_idx + 1) % 4
            p00 = tuple(bot_pts[side_idx][k] * (1.0 - t_low) + top_pts[side_idx][k] * t_low for k in range(3))
            p10 = tuple(bot_pts[next_idx][k] * (1.0 - t_low) + top_pts[next_idx][k] * t_low for k in range(3))
            p01 = tuple(bot_pts[side_idx][k] * (1.0 - t_high) + top_pts[side_idx][k] * t_high for k in range(3))
            p11 = tuple(bot_pts[next_idx][k] * (1.0 - t_high) + top_pts[next_idx][k] * t_high for k in range(3))

            # Horizontal tie + X-cross diagonals
            mb.add_strut(p01, p11, width=0.014, thickness=0.014, mat="R1_GreyMetal", bevel=0.0)
            mb.add_strut(p00, p11, width=0.012, thickness=0.012, mat="R1_GreyMetal", bevel=0.0)
            mb.add_strut(p10, p01, width=0.012, thickness=0.012, mat="R1_GreyMetal", bevel=0.0)

    # Top cap plate at mast crown (y = 1.62 m)
    mb.add_bevel_box(
        center_u=(0.0, y1 + 0.012, 0.0),
        size_u=(hw1 * 2.5, 0.028, hw1 * 2.5),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        top_scale_xz=(0.94, 0.94),
    )

    # Upper cross-arm bar and two cream-enamel ribbed ceramic insulator stacks (matching s_power_pylon.png)
    ins_y = d["insulator_arm_y"]
    ins_x = d["insulator_x"]
    mb.add_bevel_box(
        center_u=(0.0, ins_y + 0.055, 0.0),
        size_u=(ins_x * 2.25, 0.024, 0.036),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    for side_x in (-1.0, 1.0):
        ix = side_x * ins_x
        # Steel core rod of insulator stack
        mb.add_cylinder(
            center_u=(ix, ins_y, 0.0),
            radius=0.012,
            length=0.11,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )
        # 3 ribbed cream-enamel ceramic insulator skirts
        for disc_idx in (-1, 0, 1):
            mb.add_cylinder(
                center_u=(ix, ins_y + disc_idx * 0.030, 0.0),
                radius=0.034,
                radius_top=0.025,
                length=0.018,
                mat="R1_CreamEnamel",
                axis_u="Y",
                segments=8,
                bevel=0.0,
                bevel_segments=0,
            )

    # Side rotary disconnect switch box housing on viewer-left (+X) of the lattice mast
    sw_y = d["switch_box_y"]
    sw_x = 0.095
    mb.add_bevel_box(
        center_u=(sw_x, sw_y, 0.0),
        size_u=(0.048, 0.14, 0.095),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    mb.add_bevel_box(
        center_u=(sw_x + 0.022, sw_y, 0.0),
        size_u=(0.014, 0.115, 0.072),
        mat="R1_Copper",
        bevel=0.0,
        segments=0,
    )

    # Curved gooseneck lamp pipe arching from mast top (1.62 m) up to 1.95 m and down to lantern hood at (0, 1.71, 0.30)
    hood_y = d["lantern_hood_y"]
    hood_z = d["lantern_hood_z"]
    peak_y = d["gooseneck_peak_y"] - 0.020  # tube radius 0.020 reaches 1.95 m total height

    gooseneck_pts: list[tuple[float, float, float]] = [
        (0.0, y1 + 0.02, 0.0),
        (0.0, y1 + 0.16, 0.0),
    ]
    # Smooth 180-degree arch over the top
    arch_center_z = hood_z * 0.5
    arch_r_z = hood_z * 0.5
    arch_base_y = y1 + 0.16
    arch_r_y = peak_y - arch_base_y
    n_arch = 7
    for i in range(1, n_arch + 1):
        theta = math.pi * (i / n_arch)
        gz = arch_center_z - arch_r_z * math.cos(theta)
        gy = arch_base_y + arch_r_y * math.sin(theta)
        gooseneck_pts.append((0.0, gy, gz))
    gooseneck_pts.append((0.0, hood_y + 0.045, hood_z))

    mb.add_tube_path(
        points_u=gooseneck_pts,
        radius=0.020,
        mat="R1_GreyMetal",
        segments=8,
        cap_ends=True,
    )

    # Flared copper/brass dome lantern shade (R1_Copper) at the end of the gooseneck arm (matching s_power_pylon.png)
    hood_r = d["lantern_hood_radius"]
    # Upper brass cap neck
    mb.add_cylinder(
        center_u=(0.0, hood_y + 0.042, hood_z),
        radius=0.044,
        length=0.032,
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    # Main flared dome shade
    mb.add_cylinder(
        center_u=(0.0, hood_y + 0.010, hood_z),
        radius=hood_r,
        radius_top=0.048,
        length=0.048,
        mat="R1_Copper",
        axis_u="Y",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
    )
    # Thin wire guard rim around the bulb under the copper shade
    mb.add_cylinder(
        center_u=(0.0, hood_y - 0.016, hood_z),
        radius=hood_r * 0.82,
        length=0.008,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    return mb.build_object(parent=root)


def _build_cable_drum(
    idx: int,
    ang_deg: float,
    parent: bpy.types.Object,
) -> bpy.types.Object:
    """Build one spring-loaded cable guide drum (`CableDrum_1..3`) with pivot at its axle center."""
    d = POWER_PYLON_DIMS
    ang = math.radians(ang_deg)
    sx = math.sin(ang)
    cz = math.cos(ang)

    r_dist = d["cable_drum_radial_dist"]
    cy = d["cable_drum_y"]
    cx = r_dist * sx
    cz_pos = r_dist * cz

    # Build in local space around (cx, cy, cz_pos) before applying object yaw (-ang_deg)
    mb = r1kit.MeshBuilder(f"CableDrum_{idx}", pivot_u=(cx, cy, cz_pos))

    flange_r = d["cable_drum_flange_r"]
    w = d["cable_drum_width"]

    # Coiled dark cable core on the spool (local X is the spin axle)
    mb.add_cylinder(
        center_u=(cx, cy, cz_pos),
        radius=flange_r * 0.72,
        length=w * 0.78,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Left and right circular spool flanges + raised radial spoke bar so rotation is clearly visible
    for side in (-1.0, 1.0):
        fx = cx + side * (w * 0.44)
        mb.add_cylinder(
            center_u=(fx, cy, cz_pos),
            radius=flange_r,
            length=0.012,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_bevel_box(
            center_u=(fx + side * 0.005, cy, cz_pos),
            size_u=(0.008, flange_r * 1.75, 0.022),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )

    # Central steel axle hub and copper fairlead guide bracket
    mb.add_cylinder(
        center_u=(cx, cy, cz_pos),
        radius=0.020,
        length=w + 0.026,
        mat="R1_BareSteel",
        axis_u="X",
        segments=6,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_bevel_box(
        center_u=(cx, cy - 0.015, cz_pos + 0.055),
        size_u=(0.042, 0.048, 0.028),
        mat="R1_Copper",
        bevel=0.0,
        segments=0,
    )

    obj = mb.build_object(parent=parent)
    obj.rotation_euler = (0.0, 0.0, math.radians(-ang_deg))
    return obj


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: `CableDrum_1..3`, `KnifeSwitch`, `Yoke`, and `PathLamp` (`R1_LampAmber`)."""
    d = POWER_PYLON_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. CableDrum_1, CableDrum_2, CableDrum_3 at 120 deg spacing around the junction drum
    for idx, ang_deg in enumerate(DRUM_ANGLES_DEG, start=1):
        _build_cable_drum(idx, ang_deg, stage_4)

    # 2. KnifeSwitch: manual rotary disconnect lever on the upper mast switch box (+X viewer-left side)
    # Build collinear vertically from sw_pivot, then tilt outward via object rotation_euler in pose_rest / pose_active
    sw_pivot = (0.125, d["switch_box_y"] - 0.020, 0.0)
    ks_mb = r1kit.MeshBuilder("KnifeSwitch", pivot_u=sw_pivot)
    # Pivot hinge barrel
    ks_mb.add_cylinder(
        center_u=sw_pivot,
        radius=0.018,
        length=0.042,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=6,
        bevel=0.0,
        bevel_segments=0,
    )
    # Copper switch blade arm rising from sw_pivot
    ks_mb.add_bevel_box(
        center_u=(sw_pivot[0], sw_pivot[1] + 0.065, sw_pivot[2]),
        size_u=(0.018, 0.130, 0.024),
        mat="R1_Copper",
        bevel=0.004,
        segments=1,
    )
    # Insulated warm-orange grip handle connected cleanly to the tip of the copper switch blade
    ks_mb.add_bevel_box(
        center_u=(sw_pivot[0], sw_pivot[1] + 0.155, sw_pivot[2]),
        size_u=(0.028, 0.072, 0.032),
        mat="R1_OrangeStripe",
        bevel=0.005,
        segments=1,
    )
    ks_mb.build_object(parent=stage_4)

    # 3. Yoke: top insulator cable-guide yoke at mast crown (y = 1.62 m), yaws +/-180 deg around Y
    yoke_y = d["yoke_pivot_y"]
    yoke_mb = r1kit.MeshBuilder("Yoke", pivot_u=(0.0, yoke_y, 0.0))
    # Crinkled gold Kapton thermal boot wrapping the mast-to-gooseneck swivel joint
    yoke_mb.add_cylinder(
        center_u=(0.0, yoke_y + 0.042, 0.0),
        radius=0.036,
        length=0.044,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    # Swiveling dark-metal cable-guide clevis arm & brass pulley sheave
    yoke_mb.add_bevel_box(
        center_u=(0.0, yoke_y + 0.062, -0.052),
        size_u=(0.056, 0.036, 0.095),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    yoke_mb.add_cylinder(
        center_u=(0.0, yoke_y + 0.062, -0.095),
        radius=0.038,
        length=0.024,
        mat="R1_Copper",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    yoke_mb.build_object(parent=stage_4)

    # 4. PathLamp: separate mesh node using ONLY R1_LampAmber
    hood_y = d["lantern_hood_y"]
    hood_z = d["lantern_hood_z"]
    lamp_mb = r1kit.MeshBuilder("PathLamp", pivot_u=(0.0, hood_y - 0.015, hood_z))
    # Warm-amber bulb globe inside the copper gooseneck lantern shade (matching s_power_pylon.png)
    lamp_mb.add_cylinder(
        center_u=(0.0, hood_y - 0.012, hood_z),
        radius=0.044,
        radius_top=0.030,
        length=0.046,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    # Warm-amber status glow ring at the base of the lattice mast (visible in s_power_pylon.png)
    lamp_mb.add_cylinder(
        center_u=(0.0, d["mast_base_y"] - 0.010, 0.0),
        radius=0.180,
        length=0.012,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Warm-amber indicator rings around the 3 power plug receptacles
    drum_r = d["drum_radius"]
    sock_y = d["socket_y"]
    for ang_deg in SOCKET_ANGLES_DEG:
        ang = math.radians(ang_deg)
        sx = math.sin(ang)
        cz = math.cos(ang)
        lamp_mb.add_cylinder(
            center_u=((drum_r + 0.024) * sx, sock_y, (drum_r + 0.024) * cz),
            radius=0.056,
            length=0.012,
            mat="R1_LampAmber",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
    lamp_mb.build_object(parent=stage_4)

    return stage_4


def pose_power_pylon_rest(root: bpy.types.Object) -> None:
    """Set PowerPylon to its rest pose (KnifeSwitch open at +28 deg outward, Yoke at 0 deg, CableDrums at 0 deg)."""
    d = POWER_PYLON_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "KnifeSwitch":
            # In Blender (+X right, +Y forward, +Z up), +Y rotation tilts +Z toward +X (viewer-left)
            obj.rotation_euler = (0.0, math.radians(abs(d["knife_switch_rest_deg"])), 0.0)
        elif obj.name == "Yoke":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name.startswith("CableDrum_"):
            idx = int(obj.name.split("_")[-1]) - 1
            ang_deg = DRUM_ANGLES_DEG[idx]
            obj.rotation_euler = (0.0, 0.0, math.radians(-ang_deg))


def pose_power_pylon_active(root: bpy.types.Object) -> None:
    """Set PowerPylon to its active end pose (KnifeSwitch rotated +45 deg, Yoke yawed 60 deg, CableDrums rotated)."""
    d = POWER_PYLON_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "KnifeSwitch":
            obj.rotation_euler = (
                0.0,
                math.radians(abs(d["knife_switch_rest_deg"]) + d["knife_switch_delta_deg"]),
                0.0,
            )
        elif obj.name == "Yoke":
            obj.rotation_euler = (0.0, 0.0, math.radians(-d["yoke_active_yaw_deg"]))
        elif obj.name.startswith("CableDrum_"):
            idx = int(obj.name.split("_")[-1]) - 1
            ang_deg = DRUM_ANGLES_DEG[idx]
            obj.rotation_euler = (
                math.radians(d["cable_drum_active_spin_deg"]),
                0.0,
                math.radians(-ang_deg),
            )


def build_power_pylon() -> bpy.types.Object:
    """Build the complete PowerPylon hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("PowerPylon", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_power_pylon_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=POWER_PYLON_DIMS["footprint_width"] * 0.65)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build PowerPylon FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/PowerPylon.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_power_pylon()
    tris = r1kit.count_triangles(root)
    print(f"[PowerPylon] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        r1kit.render_structure_suite(
            root=root,
            slug="power_pylon",
            out_dir=args.renders_dir,
            target_height=POWER_PYLON_DIMS["total_height"],
            footprint_radius=POWER_PYLON_DIMS["footprint_width"] * 0.65,
            pose_rest_fn=pose_power_pylon_rest,
            pose_active_fn=pose_power_pylon_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
