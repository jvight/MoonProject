"""Procedural Blender 5.1 generator for the R1 `Smelter` structure (`tools/art/blender/smelter.py`).

Hierarchy contract (DESIGN.md §3.5 / TASK.md §2):
  Smelter (root empty at 0,0,0)
   +-- Stage_1           # Heavy dark base ring, 4 leveling jack pads, left hopper column, right conveyor support legs, power sockets
   |    +-- Socket_Power_1
   |    +-- Socket_Power_2
   +-- Stage_2           # Dark charcoal lower furnace pot, 5 copper induction coil rings, front brass porthole & crucible silhouette
   |    +-- CrucibleGlow # Recessed molten-amber furnace interior glow disc behind front porthole
   +-- Stage_3           # Upper domed cream-enamel shoulder with orange beltline, louvered chimney stack + conical cap, discharge hood & roller conveyor
   +-- Stage_4           # Articulated hopper gate, 4 radial chimney damper flaps, HoodBeacon, and cast ingots
        +-- HopperDoor   # Hinged ore feed chute gate + basalt ore chunks on the left (+X) hopper
        +-- DamperFlap_1 # North (+Z) radial exhaust damper flap at top of chimney
        +-- DamperFlap_2 # East (+X) radial exhaust damper flap at top of chimney
        +-- DamperFlap_3 # South (-Z) radial exhaust damper flap at top of chimney
        +-- DamperFlap_4 # West (-X) radial exhaust damper flap at top of chimney
        +-- HoodBeacon   # Amber strobe lamp atop the right (-X) ingot discharge hood
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


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Heavy base ring, 4 leveling jack pads, left hopper column leg, right conveyor legs, rear power sockets."""
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # --- Heavy dark-charcoal lower bowl base & pedestal ring ---
    mb.add_cylinder(
        center_u=(0.0, 0.08, 0.0),
        radius=0.72,
        radius_top=0.80,
        length=0.16,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.21, 0.0),
        radius=0.80,
        radius_top=0.86,
        length=0.12,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.012,
        bevel_segments=1,
    )

    # --- 4 low-profile base pads under the furnace pot ---
    for ang_deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(ang_deg)
        fx = math.sin(rad) * 0.68
        fz = math.cos(rad) * 0.68
        mb.add_bevel_box(
            center_u=(fx, 0.025, fz),
            size_u=(0.18, 0.05, 0.18),
            mat="R1_Rubber",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

    # --- Left (+X) freestanding vertical cylindrical support post under the big ore feed hopper ---
    hop_col_x, hop_col_z = 1.08, 0.26
    # Wide circular ground foot pad
    mb.add_cylinder(
        center_u=(hop_col_x, 0.024, hop_col_z),
        radius=0.150,
        radius_top=0.134,
        length=0.048,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(hop_col_x, 0.060, hop_col_z),
        radius=0.094,
        length=0.032,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Lower stout outer post tube
    mb.add_cylinder(
        center_u=(hop_col_x, 0.22, hop_col_z),
        radius=0.070,
        length=0.30,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Mid-post machined telescoping collar ring
    mb.add_cylinder(
        center_u=(hop_col_x, 0.37, hop_col_z),
        radius=0.088,
        length=0.048,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Upper inner support column
    mb.add_cylinder(
        center_u=(hop_col_x, 0.49, hop_col_z),
        radius=0.056,
        length=0.22,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Top clevis fork & horizontal hinge pin supporting the Hopper at (1.08, 0.65, 0.26)
    mb.add_bevel_box(
        center_u=(hop_col_x, 0.59, hop_col_z),
        size_u=(0.14, 0.048, 0.16),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, 26.0, 0.0),
    )
    for s_off in (-0.056, 0.056):
        rad_h = math.radians(26.0)
        ox = -math.sin(rad_h) * s_off
        oz = math.cos(rad_h) * s_off
        mb.add_bevel_box(
            center_u=(hop_col_x + ox, 0.64, hop_col_z + oz),
            size_u=(0.11, 0.095, 0.030),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, 26.0, 0.0),
        )
    mb.add_cylinder(
        center_u=(hop_col_x, 0.65, hop_col_z),
        radius=0.024,
        length=0.16,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(0.0, 26.0, 0.0),
    )

    # --- Right (-X) outboard conveyor support legs (supporting the outer end of the forward-right angled roller bed) ---
    conv_yaw_deg = 12.0
    conv_yaw_rad = math.radians(conv_yaw_deg)
    leg_base_x, leg_base_z = -1.44, 0.36
    perp_x = -math.sin(conv_yaw_rad)
    perp_z = math.cos(conv_yaw_rad)
    for s_lat in (-0.15, 0.15):
        lx = leg_base_x + perp_x * s_lat
        lz = leg_base_z + perp_z * s_lat
        mb.add_bevel_box(
            center_u=(lx, 0.02, lz),
            size_u=(0.09, 0.04, 0.09),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, conv_yaw_deg, 0.0),
        )
        mb.add_bevel_box(
            center_u=(lx, 0.15, lz),
            size_u=(0.052, 0.25, 0.052),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, conv_yaw_deg, 0.0),
        )
    # Cross tie bar between the two outer conveyor legs
    mb.add_bevel_box(
        center_u=(leg_base_x, 0.07, leg_base_z),
        size_u=(0.045, 0.045, 0.34),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, conv_yaw_deg, 0.0),
    )

    # --- Rear twin power cable sockets (`Socket_Power_1`, `Socket_Power_2`) at Z = -0.88 ---
    mb.add_bevel_box(
        center_u=(0.0, 0.42, -0.84),
        size_u=(0.46, 0.24, 0.12),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    for sx in (-0.13, 0.13):
        mb.add_cylinder(
            center_u=(sx, 0.42, -0.91),
            radius=0.048,
            length=0.04,
            mat="R1_Brass",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_cylinder(
            center_u=(sx, 0.42, -0.935),
            radius=0.026,
            length=0.02,
            mat="R1_Copper",
            axis_u="Z",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )

    stage_1_obj = mb.build_object(parent=root)

    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(-0.13, 0.42, -0.95),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    r1kit.create_empty(
        name="Socket_Power_2",
        parent=stage_1_obj,
        pivot_u=(0.13, 0.42, -0.95),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Dark-charcoal cylindrical furnace belly, 5 copper induction coil rings, front brass porthole + crucible icon."""
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    # --- Main dark-charcoal cylindrical furnace belly (`y = 0.26 .. 1.18m`, radius `0.86m`) ---
    mb.add_cylinder(
        center_u=(0.0, 0.72, 0.0),
        radius=0.86,
        length=0.92,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=18,
        bevel=0.015,
        bevel_segments=1,
    )

    # --- 5 tightly stacked bright copper induction coil rings around the lower belly (`y = 0.27 .. 0.43m`) ---
    for r_idx in range(5):
        ry = 0.27 + r_idx * 0.038
        mb.add_torus(
            center_u=(0.0, ry, 0.0),
            major_radius=0.875,
            minor_radius=0.019,
            mat="R1_Copper",
            axis_u="Y",
            major_segments=16,
            minor_segments=5,
        )

    # Dark recess collar above and below the copper coil bank
    for cy in (0.24, 0.455):
        mb.add_cylinder(
            center_u=(0.0, cy, 0.0),
            radius=0.872,
            length=0.024,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=16,
            bevel=0.0,
            bevel_segments=0,
        )

    # --- Front circular brass inspection porthole (`Z = +0.85, Y = 0.78`) ---
    port_y, port_z = 0.78, 0.85
    # Heavy outer dark mounting boss
    mb.add_cylinder(
        center_u=(0.0, port_y, port_z - 0.02),
        radius=0.31,
        length=0.06,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    # Bolted brass porthole bezel disc
    mb.add_cylinder(
        center_u=(0.0, port_y, port_z + 0.005),
        radius=0.285,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.008,
        bevel_segments=1,
    )
    # Recessed copper/dark inner bezel step around the glowing window
    mb.add_cylinder(
        center_u=(0.0, port_y, port_z + 0.012),
        radius=0.222,
        length=0.026,
        mat="R1_Copper",
        axis_u="Z",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    # 8 bolts around the brass porthole bezel
    port_bolts = []
    for b_idx in range(8):
        ang = math.radians(b_idx * 45.0 + 22.5)
        port_bolts.append((math.sin(ang) * 0.252, port_y + math.cos(ang) * 0.252, port_z + 0.026))
    mb.add_bolts(
        port_bolts,
        radius=0.011,
        height=0.009,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )
    # Heavy 2-leaf top hinge block above the porthole straddling the Y=1.18 seam (matching concept)
    mb.add_bevel_box(
        center_u=(0.0, port_y + 0.34, port_z + 0.01),
        size_u=(0.18, 0.16, 0.065),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, port_y + 0.34, port_z + 0.042),
        radius=0.022,
        length=0.20,
        mat="R1_BareSteel",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_bolts(
        [
            (-0.055, port_y + 0.39, port_z + 0.042),
            (0.055, port_y + 0.39, port_z + 0.042),
            (-0.055, port_y + 0.29, port_z + 0.042),
            (0.055, port_y + 0.29, port_z + 0.042),
        ],
        radius=0.010,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # Dark tilted crucible silhouette (`R1_Rubber` deep matte charcoal) with overhead hanger rod & pouring spout inside the porthole window
    mb.add_bevel_box(
        center_u=(0.01, port_y + 0.10, port_z + 0.035),
        size_u=(0.026, 0.14, 0.020),
        mat="R1_Rubber",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, 0.0, -6.0),
    )
    mb.add_cylinder(
        center_u=(0.0, port_y - 0.02, port_z + 0.036),
        radius=0.082,
        radius_top=0.106,
        length=0.03,
        mat="R1_Rubber",
        axis_u="Z",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(0.0, 0.0, -10.0),
    )
    # Crucible side trunnion handles / pouring lip
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.115, port_y + 0.025, port_z + 0.036),
            size_u=(0.058, 0.024, 0.022),
            mat="R1_Rubber",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, 0.0, sx * 20.0),
        )

    # --- Twin vertical braided power/cooling cables descending along the front-right belly (`X = -0.38, Z = +0.76`) ---
    for cx_off in (-0.36, -0.41):
        mb.add_tube_path(
            points_u=[
                (cx_off, 1.15, 0.77),
                (cx_off, 0.80, 0.80),
                (cx_off, 0.45, 0.80),
                (cx_off, 0.14, 0.75),
            ],
            radius=0.018,
            mat="R1_Rubber",
            segments=6,
            cap_ends=True,
        )
    # Cable clamp brackets on the belly
    for cy_clamp in (0.72, 1.12):
        mb.add_bevel_box(
            center_u=(-0.385, cy_clamp, 0.79),
            size_u=(0.11, 0.04, 0.05),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, -26.0, 0.0),
        )

    stage_2_obj = mb.build_object(parent=root)

    # --- Child: CrucibleGlow (recessed molten-amber furnace interior glow disc behind the crucible silhouette) ---
    cg_mb = r1kit.MeshBuilder("CrucibleGlow", pivot_u=(0.0, port_y, port_z + 0.005))
    cg_mb.add_cylinder(
        center_u=(0.0, port_y, port_z + 0.020),
        radius=0.200,
        length=0.022,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    cg_mb.build_object(parent=stage_2_obj)

    return stage_2_obj


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Domed cream-enamel upper shoulder + orange beltline, louvered chimney + conical cap, left hopper throat, right discharge hood & conveyor."""
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # --- Upper domed warm cream-enamel shoulder (`y = 1.18 .. 1.66m`) ---
    # 1. Lower vertical cream skirt ring with horizontal orange beltline stripe (`y = 1.18 .. 1.40m`)
    mb.add_cylinder(
        center_u=(0.0, 1.29, 0.0),
        radius=0.87,
        radius_top=0.85,
        length=0.22,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=18,
        bevel=0.010,
        bevel_segments=1,
    )
    # Warm-orange horizontal beltline stripe around the cream shoulder (`y = 1.25 .. 1.34m`)
    mb.add_cylinder(
        center_u=(0.0, 1.295, 0.0),
        radius=0.876,
        radius_top=0.862,
        length=0.088,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=18,
        bevel=0.0,
        bevel_segments=0,
    )
    # 2. Rounded cream-enamel dome crown tapering to the chimney collar (`y = 1.40 .. 1.66m`)
    mb.add_cylinder(
        center_u=(0.0, 1.49, 0.0),
        radius=0.85,
        radius_top=0.72,
        length=0.18,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=18,
        bevel=0.014,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 1.62, 0.0),
        radius=0.72,
        radius_top=0.52,
        length=0.10,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=18,
        bevel=0.012,
        bevel_segments=1,
    )
    # Paired rivet bolts along vertical panel seams on the cream shoulder (matching concept)
    seam_bolts = []
    for seam_deg in (-65.0, -22.0, 22.0, 65.0):
        for off_deg in (-3.2, 3.2):
            rad = math.radians(seam_deg + off_deg)
            for by, br in ((1.22, 0.874), (1.58, 0.725)):
                seam_bolts.append((math.sin(rad) * br, by, math.cos(rad) * br))
    mb.add_bolts(
        seam_bolts,
        radius=0.011,
        height=0.009,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # --- Top louvered chimney stack (`y = 1.66 .. 2.11m`) + stepped overhanging conical cap (`y = 2.10 .. 2.38m`) ---
    # Chimney wide base collar ring
    mb.add_cylinder(
        center_u=(0.0, 1.69, 0.0),
        radius=0.47,
        radius_top=0.43,
        length=0.07,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 1.74, 0.0),
        radius=0.39,
        radius_top=0.36,
        length=0.05,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    # Recessed dark chimney core
    mb.add_cylinder(
        center_u=(0.0, 1.93, 0.0),
        radius=0.29,
        radius_top=0.21,
        length=0.38,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=14,
        bevel=0.0,
        bevel_segments=0,
    )
    # 6 closely stacked horizontal louver frustum rings on the chimney cone
    for l_idx in range(6):
        ly = 1.765 + l_idx * 0.056
        lr = 0.35 - l_idx * 0.015
        mb.add_cylinder(
            center_u=(0.0, ly, 0.0),
            radius=lr,
            radius_top=lr - 0.022,
            length=0.032,
            mat="R1_BareSteel" if l_idx % 2 == 0 else "R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.0,
            bevel_segments=0,
        )
    # 4 tapered vertical frame ribs flanking the front and rear louver windows (at ±24° and ±156°, leaving front center clear)
    for ang_deg in (-24.0, 24.0, -156.0, 156.0):
        rad = math.radians(ang_deg)
        sx, cz = math.sin(rad), math.cos(rad)
        mb.add_bevel_box(
            center_u=(sx * 0.305, 1.92, cz * 0.305),
            size_u=(0.036, 0.39, 0.040),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
            rot_deg_u=(-14.5, ang_deg, 0.0),
        )
        # Base bolster block at the bottom of each rib
        mb.add_bevel_box(
            center_u=(sx * 0.375, 1.735, cz * 0.375),
            size_u=(0.044, 0.065, 0.048),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
    # Stepped overhanging conical rain/slag cap at the peak (`y = 2.10 .. 2.38m`)
    mb.add_cylinder(
        center_u=(0.0, 2.115, 0.0),
        radius=0.43,
        radius_top=0.405,
        length=0.034,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 2.142, 0.0),
        radius=0.385,
        radius_top=0.355,
        length=0.030,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=16,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(0.0, 2.265, 0.0),
        radius=0.365,
        radius_top=0.016,
        length=0.23,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.005,
        bevel_segments=1,
    )

    # --- Left (+X) side ore feed hopper mounting throat (`X = +0.84, Y = 0.80, Z = +0.20`, yawed toward camera) ---
    mb.add_bevel_box(
        center_u=(0.82, 0.80, 0.20),
        size_u=(0.24, 0.50, 0.44),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
        rot_deg_u=(0.0, -26.0, 0.0),
    )
    # Short horizontal coupling rib flush against the belly between hopper throat and front porthole bezel (matching concept)
    mb.add_bevel_box(
        center_u=(0.74, 0.78, 0.48),
        size_u=(0.14, 0.028, 0.042),
        mat="R1_BareSteel",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, -32.0, 0.0),
    )

    # --- Right (-X) ingot discharge hood & angled roller conveyor chute (yawed +12° forward-right) ---
    conv_yaw_deg = 12.0
    conv_yaw_rad = math.radians(conv_yaw_deg)
    dir_x = -math.cos(conv_yaw_rad)
    dir_z = math.sin(conv_yaw_rad)
    perp_x = -math.sin(conv_yaw_rad)
    perp_z = math.cos(conv_yaw_rad)

    hood_cx, hood_cy, hood_cz = -0.84, 0.74, 0.22
    # 1. Brushed steel arched discharge hood (`X = -0.84, Y = 0.74, Z = +0.22`, rotated +12° around Y)
    mb.add_bevel_box(
        center_u=(hood_cx, hood_cy, hood_cz),
        size_u=(0.30, 0.56, 0.46),
        mat="R1_BareSteel",
        bevel=0.028,
        segments=1,
        rot_deg_u=(0.0, conv_yaw_deg, 0.0),
    )
    # Glowing amber interior throat recessed inside the discharge hood opening
    mb.add_bevel_box(
        center_u=(hood_cx + dir_x * 0.07, 0.70, hood_cz + dir_z * 0.07),
        size_u=(0.18, 0.42, 0.35),
        mat="R1_LampAmber",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, conv_yaw_deg, 0.0),
    )
    # Beacon mounting collar on top of the discharge hood (`X = -0.86, Y = 1.04, Z = +0.23`)
    mb.add_cylinder(
        center_u=(-0.86, 1.04, 0.23),
        radius=0.072,
        length=0.04,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )

    # 2. Angled roller conveyor bed sloping from `(-0.84, 0.52, 0.22)` down to `(-1.54, 0.27, 0.37)`
    p_start = (-0.84, 0.52, 0.22)
    p_end = (-1.54, 0.27, 0.37)
    for s_lat in (-0.175, 0.175):
        mb.add_strut(
            p0_u=(p_start[0] + perp_x * s_lat, p_start[1], p_start[2] + perp_z * s_lat),
            p1_u=(p_end[0] + perp_x * s_lat, p_end[1], p_end[2] + perp_z * s_lat),
            width=0.038,
            thickness=0.068,
            mat="R1_BareSteel",
            bevel=0.0,
        )
    # Dark bed floor & 5 steel conveyor rollers
    mb.add_strut(
        p0_u=(p_start[0], p_start[1] - 0.015, p_start[2]),
        p1_u=(p_end[0], p_end[1] - 0.015, p_end[2]),
        width=0.32,
        thickness=0.026,
        mat="R1_GreyMetal",
        bevel=0.0,
    )
    for r_idx in range(5):
        t = (r_idx + 0.35) / 4.7
        rx = p_start[0] * (1.0 - t) + p_end[0] * t
        ry = p_start[1] * (1.0 - t) + p_end[1] * t
        rz = p_start[2] * (1.0 - t) + p_end[2] * t
        mb.add_cylinder(
            center_u=(rx, ry, rz),
            radius=0.026,
            length=0.33,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
            rot_deg_u=(0.0, conv_yaw_deg, 0.0),
        )

    return mb.build_object(parent=root)


def _rot_pt_around_pivot_y(
    pt: tuple[float, float, float],
    piv: tuple[float, float, float],
    yaw_deg: float,
) -> tuple[float, float, float]:
    """Rotate a Unity point (x, y, z) around a vertical axis through `piv` by `yaw_deg` (where +yaw swings +X toward +Z)."""
    rad = math.radians(yaw_deg)
    c, s = math.cos(rad), math.sin(rad)
    dx = pt[0] - piv[0]
    dz = pt[2] - piv[2]
    return (
        piv[0] + dx * c - dz * s,
        pt[1],
        piv[2] + dx * s + dz * c,
    )


def _add_prism_polygon_xy_yawed(
    mb: r1kit.MeshBuilder,
    pts_xy: list[tuple[float, float]],
    z_min: float,
    z_max: float,
    piv_u: tuple[float, float, float],
    yaw_deg: float,
    mat: str,
) -> None:
    """Create a closed manifold prism from a 2D polygon in local (X, Y) extruded from z_min to z_max, yawed around `piv_u`."""
    import bmesh

    back_verts = [
        mb.bm.verts.new(mb._to_local_b(_rot_pt_around_pivot_y((x, y, z_min), piv_u, yaw_deg)))
        for x, y in pts_xy
    ]
    front_verts = [
        mb.bm.verts.new(mb._to_local_b(_rot_pt_around_pivot_y((x, y, z_max), piv_u, yaw_deg)))
        for x, y in pts_xy
    ]
    faces = [
        mb.bm.faces.new(list(reversed(back_verts))),
        mb.bm.faces.new(front_verts),
    ]
    n = len(pts_xy)
    for i in range(n):
        j = (i + 1) % n
        faces.append(mb.bm.faces.new([back_verts[i], back_verts[j], front_verts[j], front_verts[i]]))
    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)


def _build_hopper_node(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the concept-sized feed Hopper node (`Hopper`) perched on the left (+X) support post."""
    hop_piv = (1.08, 0.65, 0.26)
    hop_yaw = 26.0  # Swings the outer-left (+X) scoop mouth forward toward +Z so camera sees both cheek wall and open dark trough
    mb = r1kit.MeshBuilder("Hopper", pivot_u=hop_piv)

    z_center = 0.26
    half_w = 0.27  # Total width 0.54m in local Z
    z_back = z_center - half_w
    z_front = z_center + half_w

    # 1. Core 5-sided hopper body with a deep recessed mouth floor (B_in -> C_in)
    core_poly = [
        (1.03, 0.58),  # A: bottom hinge vertex above post
        (1.48, 0.85),  # B_in: recessed floor outer-left
        (1.16, 1.03),  # C_in: recessed floor inner-top
        (0.90, 1.03),  # D: upper furnace throat junction
        (0.90, 0.60),  # E: lower furnace throat junction
    ]
    _add_prism_polygon_xy_yawed(
        mb,
        pts_xy=core_poly,
        z_min=z_back + 0.036,
        z_max=z_front - 0.036,
        piv_u=hop_piv,
        yaw_deg=hop_yaw,
        mat="R1_GreyMetal",
    )

    # 2. Rear (-Z) and Front (+Z) 5-sided cheek walls framing the open funnel mouth
    rear_cheek_poly = [
        (1.02, 0.57),  # A: bottom hinge vertex
        (1.58, 1.04),  # B_back: outer left scoop lip
        (1.17, 1.28),  # C_back: top crest of the open mouth
        (0.89, 1.10),  # D: upper furnace junction
        (0.89, 0.59),  # E: lower furnace junction
    ]
    front_cheek_poly = [
        (1.02, 0.57),  # A: bottom hinge vertex
        (1.56, 0.90),  # B_front: outer left scoop lip (lower than rear so dark mouth floor + rocks face camera)
        (1.17, 1.13),  # C_front: top crest of front cheek wall
        (0.89, 1.06),  # D: upper furnace junction
        (0.89, 0.59),  # E: lower furnace junction
    ]
    _add_prism_polygon_xy_yawed(
        mb,
        pts_xy=rear_cheek_poly,
        z_min=z_back,
        z_max=z_back + 0.038,
        piv_u=hop_piv,
        yaw_deg=hop_yaw,
        mat="R1_GreyMetal",
    )
    _add_prism_polygon_xy_yawed(
        mb,
        pts_xy=front_cheek_poly,
        z_min=z_front - 0.038,
        z_max=z_front,
        piv_u=hop_piv,
        yaw_deg=hop_yaw,
        mat="R1_GreyMetal",
    )

    # 3. Deep dark shadow cavity liner (`R1_DarkGlass`) spanning the entire open hopper mouth (`B -> C`) so the trough reads dark & hollow
    dark_mouth_poly = [
        (1.53, 0.88),
        (1.56, 1.01),
        (1.17, 1.25),
        (1.15, 1.10),
    ]
    _add_prism_polygon_xy_yawed(
        mb,
        pts_xy=dark_mouth_poly,
        z_min=z_back + 0.036,
        z_max=z_front - 0.036,
        piv_u=hop_piv,
        yaw_deg=hop_yaw,
        mat="R1_DarkGlass",
    )

    # 4. Crisp brushed-steel (`R1_BareSteel`) structural rim flange framing all 4 sides of the open funnel mouth (`B -> C`)
    mb.add_strut(
        p0_u=_rot_pt_around_pivot_y((1.58, 1.042, z_back + 0.020), hop_piv, hop_yaw),
        p1_u=_rot_pt_around_pivot_y((1.17, 1.282, z_back + 0.020), hop_piv, hop_yaw),
        width=0.048,
        thickness=0.038,
        mat="R1_BareSteel",
        bevel=0.0,
    )
    mb.add_strut(
        p0_u=_rot_pt_around_pivot_y((1.56, 0.902, z_front - 0.020), hop_piv, hop_yaw),
        p1_u=_rot_pt_around_pivot_y((1.17, 1.132, z_front - 0.020), hop_piv, hop_yaw),
        width=0.048,
        thickness=0.038,
        mat="R1_BareSteel",
        bevel=0.0,
    )
    # Outer-left (B) and Inner-top (C) transverse mouth rim bars spanning across Z
    mb.add_strut(
        p0_u=_rot_pt_around_pivot_y((1.57, 1.040, z_back + 0.015), hop_piv, hop_yaw),
        p1_u=_rot_pt_around_pivot_y((1.55, 0.900, z_front - 0.015), hop_piv, hop_yaw),
        width=0.050,
        thickness=0.042,
        mat="R1_BareSteel",
        bevel=0.0,
    )
    mb.add_strut(
        p0_u=_rot_pt_around_pivot_y((1.17, 1.280, z_back + 0.015), hop_piv, hop_yaw),
        p1_u=_rot_pt_around_pivot_y((1.17, 1.130, z_front - 0.015), hop_piv, hop_yaw),
        width=0.050,
        thickness=0.042,
        mat="R1_BareSteel",
        bevel=0.0,
    )
    # Upper hood slope plate from C down to furnace wall D=(0.89, 1.08)
    mb.add_strut(
        p0_u=_rot_pt_around_pivot_y((1.17, 1.20, z_center), hop_piv, hop_yaw),
        p1_u=_rot_pt_around_pivot_y((0.89, 1.08, z_center), hop_piv, hop_yaw),
        width=half_w * 2.0 - 0.02,
        thickness=0.038,
        mat="R1_BareSteel",
        bevel=0.0,
    )

    # 5. Rugged notched scoop bumper / dump stop at outer lip B=(1.60, 0.94) (matching concept)
    mb.add_bevel_box(
        center_u=_rot_pt_around_pivot_y((1.60, 0.94, z_center), hop_piv, hop_yaw),
        size_u=(0.072, 0.054, 0.40),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, -hop_yaw, -38.0),
    )
    # Lower hinge knuckle lug at A=(1.08, 0.65, 0.26) mating with the support post clevis
    mb.add_bevel_box(
        center_u=hop_piv,
        size_u=(0.12, 0.09, 0.076),
        mat="R1_BareSteel",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, -hop_yaw, 0.0),
    )

    # 6. Heap of chunky faceted brushed-steel ore rocks nestled in the center of the dark hopper mouth (matching s_smelter.png)
    rock_specs = [
        ((1.38, 1.05, z_center + 0.02), (0.14, 0.13, 0.14), (18.0, 24.0, -22.0), "R1_BareSteel"),
        ((1.27, 1.14, z_center - 0.02), (0.15, 0.14, 0.14), (-14.0, -28.0, 16.0), "R1_BareSteel"),
        ((1.33, 1.10, z_center - 0.08), (0.13, 0.12, 0.13), (10.0, 42.0, -15.0), "R1_BareSteel"),
        ((1.22, 1.18, z_center + 0.03), (0.13, 0.12, 0.13), (-20.0, 15.0, -8.0), "R1_BareSteel"),
    ]
    rock_cuts = [
        ((0.7, 0.6, 0.3), 0.74),
        ((-0.6, 0.7, -0.3), 0.76),
    ]
    for r_pos, r_size, r_rot, r_mat in rock_specs:
        mb.add_fractured_rock(
            center_u=_rot_pt_around_pivot_y(r_pos, hop_piv, hop_yaw),
            size_u=r_size,
            cuts_u=rock_cuts,
            mat=r_mat,
            bevel=0.0,
            bevel_segments=0,
            rot_deg_u=r_rot,
        )

    return mb.build_object(parent=parent)


def _build_damper_fin(
    parent: bpy.types.Object,
    name: str,
    pivot_u: tuple[float, float, float],
    azimuth_deg: float,
    flare_deg: float,
) -> bpy.types.Object:
    """Build a sculpted radial exhaust cowl fin (`DamperFlap_k`) flared UP and OUTWARD at concept angle around the chimney."""
    px, py, pz = pivot_u
    mb = r1kit.MeshBuilder(name, pivot_u=pivot_u)

    az_rad = math.radians(azimuth_deg)
    rx = math.sin(az_rad)
    rz = math.cos(az_rad)
    fl_rad = math.radians(flare_deg)

    # Bottom hinge bracket at pivot_u
    mb.add_bevel_box(
        center_u=(px, py + 0.015, pz),
        size_u=(0.10, 0.044, 0.050),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, azimuth_deg, 0.0),
    )

    # Midpoint of the flared fin blade (length 0.36m extending UP and OUTWARD at flare_deg from vertical)
    blade_len = 0.36
    mid_dist = blade_len * 0.50
    bx = px + rx * (mid_dist * math.sin(fl_rad))
    by = py + 0.02 + mid_dist * math.cos(fl_rad)
    bz = pz + rz * (mid_dist * math.sin(fl_rad))

    # Note: in MeshBuilder.add_bevel_box, rot_deg_u[0] = -flare_deg tilts the top (+Y_u / +Z_b) OUTWARD along (rx, rz)!
    mb.add_bevel_box(
        center_u=(bx, by, bz),
        size_u=(0.195, blade_len, 0.034),
        mat="R1_BareSteel",
        bevel=0.008,
        segments=1,
        rot_deg_u=(-flare_deg, azimuth_deg, 0.0),
        top_scale_xz=(1.22, 0.88),
    )
    # Recessed dark inner panel on the inward/upward-facing side of the flared fin
    in_off = -0.007
    ix = bx + rx * (in_off * math.cos(fl_rad))
    iy = by - in_off * math.sin(fl_rad)
    iz = bz + rz * (in_off * math.cos(fl_rad))
    mb.add_bevel_box(
        center_u=(ix, iy, iz),
        size_u=(0.142, blade_len * 0.78, 0.024),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(-flare_deg, azimuth_deg, 0.0),
        top_scale_xz=(1.18, 0.90),
    )

    return mb.build_object(parent=parent)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: Big feed Hopper + HopperDoor throat gate, 4 diagonal cowl fins DamperFlap_1..4, HoodBeacon, and cast ingots."""
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # --- 1. Big Feed Hopper (`Hopper` node on its own post at the left +X side for tip animation) ---
    _build_hopper_node(stage_4)

    # --- 2. HopperDoor (inner furnace throat chute gate kept at existing node name & pivot) ---
    hop_door_piv = (0.94, 0.62, 0.16)
    hd_mb = r1kit.MeshBuilder("HopperDoor", pivot_u=hop_door_piv)
    hd_mb.add_bevel_box(
        center_u=(0.94, 0.80, 0.20),
        size_u=(0.06, 0.42, 0.36),
        mat="R1_BareSteel",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, -26.0, 0.0),
    )
    hd_mb.build_object(parent=stage_4)

    # --- 3. DamperFlap_1..4 (4 radial exhaust cowl fins at diagonal quadrants flared UP and OUTWARD like s_smelter.png) ---
    fin_specs = [
        ("DamperFlap_1", (0.30, 1.74, 0.22), 54.0, 45.0),     # Front-Left (+X / +Z)
        ("DamperFlap_2", (0.33, 1.74, -0.16), 116.0, 50.0),   # Rear-Left (+X / -Z)
        ("DamperFlap_3", (-0.33, 1.74, -0.16), -116.0, 50.0), # Rear-Right (-X / -Z)
        ("DamperFlap_4", (-0.30, 1.74, 0.22), -54.0, 45.0),   # Front-Right (-X / +Z)
    ]
    for f_name, f_piv, az_deg, fl_deg in fin_specs:
        _build_damper_fin(
            parent=stage_4,
            name=f_name,
            pivot_u=f_piv,
            azimuth_deg=az_deg,
            flare_deg=fl_deg,
        )

    # --- 4. HoodBeacon (glowing amber strobe dome atop the right ingot discharge hood + cast ingots on conveyor) ---
    hb_piv = (-0.86, 1.06, 0.23)
    hb_mb = r1kit.MeshBuilder("HoodBeacon", pivot_u=hb_piv)
    hb_mb.add_cylinder(
        center_u=(-0.86, 1.13, 0.23),
        radius=0.058,
        radius_top=0.046,
        length=0.13,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.016,
        bevel_segments=1,
    )
    # Freshly cast trapezoidal ingots sliding down the angled roller conveyor (`p_start = (-0.84, 0.52, 0.22)` -> `p_end = (-1.54, 0.27, 0.37)`):
    # 1. Upper ingot emerging from the hood (`t = 0.32`): silver brushed steel (`R1_BareSteel`)
    hb_mb.add_ingot(
        center_u=(-1.06, 0.49, 0.27),
        size_u=(0.21, 0.088, 0.24),
        mat="R1_BareSteel",
        bevel=0.008,
        rot_deg_u=(0.0, 12.0, -19.2),
    )
    # 2. Lower ingot further down the rollers (`t = 0.76`): warm copper-amber ingot (`R1_Copper`)
    hb_mb.add_ingot(
        center_u=(-1.37, 0.38, 0.33),
        size_u=(0.21, 0.088, 0.24),
        mat="R1_Copper",
        bevel=0.008,
        rot_deg_u=(0.0, 12.0, -19.2),
    )
    hb_mb.build_object(parent=stage_4)

    return stage_4


def pose_smelter_rest(root: bpy.types.Object) -> None:
    """Set Smelter moving parts to their rest pose (unrotated transforms for FBX export)."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    for name in ("Hopper", "HopperDoor", "DamperFlap_1", "DamperFlap_2", "DamperFlap_3", "DamperFlap_4"):
        if name in objs:
            objs[name].rotation_euler = (0.0, 0.0, 0.0)


def pose_smelter_active(root: bpy.types.Object) -> None:
    """Set Smelter moving parts to their active smelting pose."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "Hopper" in objs:
        objs["Hopper"].rotation_euler = (0.0, math.radians(-2.0), 0.0)
    if "HopperDoor" in objs:
        objs["HopperDoor"].rotation_euler = (0.0, math.radians(8.0), 0.0)
    for idx in range(1, 5):
        name = f"DamperFlap_{idx}"
        if name in objs:
            objs[name].rotation_euler = (0.0, 0.0, 0.0)


def _render_smelter_concept_compare(
    root: bpy.types.Object,
    out_dir: Path,
    concept_path: Path,
) -> None:
    """Render Smelter from the concept camera angle at night with a warm key light and molten-amber practical lights."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    pose_smelter_active(root)
    bpy.context.view_layer.update()

    temp_objs: list[bpy.types.Object] = []
    temp_mats: list[bpy.types.Material] = []

    # 1. Night sky world background (deep lunar night sky, warm-neutral bounce so cream never looks blue-grey)
    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("R1_SmelterNightWorld")
    scene.world.use_nodes = True
    w_nt = scene.world.node_tree
    w_nt.nodes.clear()
    w_out = w_nt.nodes.new("ShaderNodeOutputWorld")
    w_bg_cam = w_nt.nodes.new("ShaderNodeBackground")
    w_bg_cam.inputs["Color"].default_value = r1kit._srgb_to_linear("#070A14")
    w_bg_cam.inputs["Strength"].default_value = 1.0
    w_bg_amb = w_nt.nodes.new("ShaderNodeBackground")
    w_bg_amb.inputs["Color"].default_value = r1kit._srgb_to_linear("#E8D8BE")
    w_bg_amb.inputs["Strength"].default_value = 0.28
    w_lp = w_nt.nodes.new("ShaderNodeLightPath")
    w_mix = w_nt.nodes.new("ShaderNodeMixShader")
    w_nt.links.new(w_lp.outputs["Is Camera Ray"], w_mix.inputs["Fac"])
    w_nt.links.new(w_bg_amb.outputs["Background"], w_mix.inputs[1])
    w_nt.links.new(w_bg_cam.outputs["Background"], w_mix.inputs[2])
    w_nt.links.new(w_mix.outputs["Shader"], w_out.inputs["Surface"])

    # 2. Wide lunar regolith ground surface extending to the horizon
    ground_mb = r1kit.MeshBuilder("_SmelterLunarGround", pivot_u=(0.0, -0.08, 0.0))
    ground_mb.add_cylinder(
        center_u=(0.0, -0.08, -4.0),
        radius=20.0,
        length=0.16,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=64,
        bevel=0.02,
        bevel_segments=1,
    )
    ground_obj = ground_mb.build_object()
    ground_mat = bpy.data.materials.new("_SmelterGroundMat")
    ground_mat.use_nodes = True
    g_bsdf = ground_mat.node_tree.nodes["Principled BSDF"]
    g_bsdf.inputs["Base Color"].default_value = r1kit._srgb_to_linear("#6E6D6A")
    g_bsdf.inputs["Roughness"].default_value = 0.92
    g_bsdf.inputs["Metallic"].default_value = 0.02
    ground_obj.data.materials.clear()
    ground_obj.data.materials.append(ground_mat)
    temp_objs.append(ground_obj)
    temp_mats.append(ground_mat)

    # IMPORTANT: Call ensure_materials(use_vertex_color_shading=True) AFTER ground_mb.build_object()
    # because build_object() resets materials to use_vertex_color_shading=False!
    r1kit.ensure_materials(use_vertex_color_shading=True)
    lamp_mat = bpy.data.materials.get("R1_LampAmber")
    if lamp_mat and lamp_mat.use_nodes:
        for node in lamp_mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                node.inputs["Base Color"].default_value = r1kit._srgb_to_linear("#FF8A18")
                if "Emission Color" in node.inputs:
                    node.inputs["Emission Color"].default_value = r1kit._srgb_to_linear("#FF8414")
                if "Emission Strength" in node.inputs:
                    node.inputs["Emission Strength"].default_value = 1.65

    def _add_sun(name: str, from_u: tuple[float, float, float], tgt_u: tuple[float, float, float], hex_col: str, energy: float, angle_deg: float) -> None:
        l_data = bpy.data.lights.new(name=name, type="SUN")
        l_data.energy = energy
        l_data.color = r1kit._srgb_to_linear(hex_col)[:3]
        l_data.angle = math.radians(angle_deg)
        l_obj = bpy.data.objects.new(name, l_data)
        bpy.context.collection.objects.link(l_obj)
        direction = (r1kit.u_pos(*tgt_u) - r1kit.u_pos(*from_u)).normalized()
        l_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler("XYZ")
        temp_objs.append(l_obj)

    def _add_point(name: str, pos_u: tuple[float, float, float], hex_col: str, energy: float, radius: float) -> None:
        l_data = bpy.data.lights.new(name=name, type="POINT")
        l_data.energy = energy
        l_data.color = r1kit._srgb_to_linear(hex_col)[:3]
        l_data.shadow_soft_size = radius
        l_obj = bpy.data.objects.new(name, l_data)
        bpy.context.collection.objects.link(l_obj)
        l_obj.location = r1kit.u_pos(*pos_u)
        temp_objs.append(l_obj)

    # 3. Warm night key light from front-left-top + soft warm-cream fill (never cold blue!)
    _add_sun("_SmelterWarmKey", (3.8, 4.2, 4.6), (0.0, 1.0, 0.0), "#FFEAD0", 3.5, 5.0)
    _add_sun("_SmelterWarmFill", (-4.0, 2.5, 3.8), (0.0, 1.0, 0.0), "#F6E4C8", 1.2, 22.0)
    _add_sun("_SmelterWarmRim", (-1.5, 4.2, -3.8), (0.0, 1.2, 0.0), "#FFE2B8", 1.1, 10.0)

    # 4. Warm molten-amber practical lights at discharge hood, beacon, and glowing ingot on conveyor
    _add_point("_HoodThroatGlow", (-0.96, 0.72, 0.34), "#FFA028", 35.0, 0.16)
    _add_point("_BeaconGlow", (-0.86, 1.18, 0.32), "#FFB030", 20.0, 0.12)
    _add_point("_IngotGroundPool", (-1.36, 0.42, 0.42), "#FF9824", 28.0, 0.18)

    # 5. Camera matched to s_smelter.png angle & full-structure framing
    cam_data = bpy.data.cameras.new("_SmelterConceptCam")
    cam_obj = bpy.data.objects.new("_SmelterConceptCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    temp_objs.append(cam_obj)

    r1kit._aim_camera_at(
        cam_obj,
        cam_pos_u=(-0.56, 1.54, 5.25),
        target_u=(-0.04, 1.08, 0.0),
        lens_mm=37.0,
    )

    concept_render_path = out_dir / "_smelter_concept_view.png"
    r1kit._render_to_file(concept_render_path, res_x=1376, res_y=768, freestyle=True)

    if Path(concept_path).exists():
        r1kit.compose_side_by_side(
            left_path=Path(concept_path),
            right_path=concept_render_path,
            out_path=out_dir / "compare_smelter.png",
            target_height=768,
        )
    concept_render_path.unlink(missing_ok=True)

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    for tmat in temp_mats:
        if tmat.name in bpy.data.materials:
            bpy.data.materials.remove(tmat)

    pose_smelter_rest(root)
    bpy.context.view_layer.update()
    r1kit.ensure_materials(use_vertex_color_shading=False)


def build_smelter() -> bpy.types.Object:
    """Build the complete Smelter hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("Smelter", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_smelter_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=1.45)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build Smelter FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/Smelter.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_smelter()
    tris = r1kit.count_triangles(root)
    print(f"[Smelter] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        concept_img = Path("D:/Project/MoonProject-wt/agy-concept/Logs/base/s_smelter.png")
        _render_smelter_concept_compare(
            root=root,
            out_dir=args.renders_dir,
            concept_path=concept_img,
        )
        r1kit.render_structure_suite(
            root=root,
            slug="smelter",
            out_dir=args.renders_dir,
            target_height=2.36,
            footprint_radius=1.45,
            pose_rest_fn=pose_smelter_rest,
            pose_active_fn=pose_smelter_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
            cam_side_x=-1.0,
            concept_path=None,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
