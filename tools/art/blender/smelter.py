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
        segments=20,
        bevel=0.015,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.21, 0.0),
        radius=0.80,
        radius_top=0.86,
        length=0.12,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=20,
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

    # --- Left (+X) vertical cylindrical support column under the ore feed hopper ---
    hop_col_x, hop_col_z = 0.96, 0.16
    mb.add_cylinder(
        center_u=(hop_col_x, 0.025, hop_col_z),
        radius=0.11,
        length=0.05,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.008,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(hop_col_x, 0.34, hop_col_z),
        radius=0.055,
        length=0.58,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(hop_col_x, 0.56, hop_col_z),
        radius=0.072,
        length=0.08,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
    )

    # --- Right (-X) outboard conveyor support legs (supporting the outer end of the angled roller bed) ---
    for sz in (-0.15, 0.15):
        # Outer vertical leg at X = -1.46
        mb.add_bevel_box(
            center_u=(-1.46, 0.02, sz),
            size_u=(0.09, 0.04, 0.09),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        mb.add_bevel_box(
            center_u=(-1.46, 0.16, sz),
            size_u=(0.052, 0.26, 0.052),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )
    # Cross tie bar between the two outer conveyor legs
    mb.add_bevel_box(
        center_u=(-1.46, 0.08, 0.0),
        size_u=(0.045, 0.045, 0.34),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )

    # --- Rear twin power cable sockets (`Socket_Power_1`, `Socket_Power_2`) at Z = -0.88 ---
    mb.add_bevel_box(
        center_u=(0.0, 0.42, -0.84),
        size_u=(0.46, 0.24, 0.12),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    for sx in (-0.13, 0.13):
        mb.add_cylinder(
            center_u=(sx, 0.42, -0.91),
            radius=0.048,
            length=0.04,
            mat="R1_Brass",
            axis_u="Z",
            segments=10,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_cylinder(
            center_u=(sx, 0.42, -0.935),
            radius=0.026,
            length=0.02,
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
        segments=20,
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
            major_segments=20,
            minor_segments=6,
        )

    # Dark recess collar above and below the copper coil bank
    for cy in (0.24, 0.455):
        mb.add_cylinder(
            center_u=(0.0, cy, 0.0),
            radius=0.872,
            length=0.024,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=20,
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
        segments=20,
        bevel=0.010,
        bevel_segments=1,
    )
    # Bolted brass porthole bezel disc
    mb.add_cylinder(
        center_u=(0.0, port_y, port_z + 0.005),
        radius=0.285,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=20,
        bevel=0.008,
        bevel_segments=1,
    )
    # 8 bolts around the brass porthole bezel
    port_bolts = []
    for b_idx in range(8):
        ang = math.radians(b_idx * 45.0 + 22.5)
        port_bolts.append((math.sin(ang) * 0.245, port_y + math.cos(ang) * 0.245, port_z + 0.028))
    mb.add_bolts(
        port_bolts,
        radius=0.012,
        height=0.010,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )
    # Heavy top hinge block above the porthole (matching concept)
    mb.add_bevel_box(
        center_u=(0.0, port_y + 0.30, port_z + 0.01),
        size_u=(0.15, 0.13, 0.065),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, port_y + 0.33, port_z + 0.04),
        radius=0.020,
        length=0.17,
        mat="R1_BareSteel",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Dark tilted crucible silhouette with pouring spout & side trunnion handles inside the porthole window
    mb.add_cylinder(
        center_u=(0.0, port_y - 0.02, port_z + 0.036),
        radius=0.075,
        radius_top=0.095,
        length=0.03,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=12,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(0.0, 0.0, -10.0),
    )
    # Crucible side trunnion handles
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.105, port_y + 0.02, port_z + 0.036),
            size_u=(0.05, 0.022, 0.022),
            mat="R1_GreyMetal",
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
        radius=0.205,
        length=0.022,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=20,
        bevel=0.0,
        bevel_segments=0,
    )
    cg_mb.build_object(parent=stage_2_obj)

    return stage_2_obj


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Domed cream-enamel upper shoulder + orange beltline, louvered chimney + conical cap, left hopper box, right discharge hood & conveyor."""
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
        segments=20,
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
        segments=20,
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
        segments=20,
        bevel=0.016,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 1.62, 0.0),
        radius=0.72,
        radius_top=0.52,
        length=0.10,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=20,
        bevel=0.014,
        bevel_segments=1,
    )
    # Rivet bolts along panel seams on the cream shoulder
    seam_bolts = []
    for ang_deg in (25.0, 85.0, 145.0, 205.0, 265.0, 325.0):
        rad = math.radians(ang_deg)
        for by, br in ((1.21, 0.875), (1.37, 0.860), (1.53, 0.795)):
            seam_bolts.append((math.sin(rad) * br, by, math.cos(rad) * br))
    mb.add_bolts(
        seam_bolts,
        radius=0.011,
        height=0.009,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # --- Top louvered chimney stack (`y = 1.66 .. 2.12m`) + conical peak cap (`y = 2.12 .. 2.36m`) ---
    # Chimney base collar ring
    mb.add_cylinder(
        center_u=(0.0, 1.70, 0.0),
        radius=0.44,
        radius_top=0.38,
        length=0.08,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        bevel_segments=1,
    )
    # Recessed dark/amber chimney core
    mb.add_cylinder(
        center_u=(0.0, 1.92, 0.0),
        radius=0.28,
        radius_top=0.22,
        length=0.40,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=14,
        bevel=0.0,
        bevel_segments=0,
    )
    # 5 closely stacked horizontal louver rings on the chimney cone
    for l_idx in range(5):
        ly = 1.77 + l_idx * 0.068
        lr = 0.33 - l_idx * 0.016
        mb.add_cylinder(
            center_u=(0.0, ly, 0.0),
            radius=lr,
            radius_top=lr * 0.90,
            length=0.036,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=16,
            bevel=0.0,
            bevel_segments=0,
        )
    # 4 vertical rib struts along the chimney louver cone
    for ang_deg in (0.0, 90.0, 180.0, 270.0):
        rad = math.radians(ang_deg)
        mb.add_strut(
            p0_u=(math.sin(rad) * 0.33, 1.72, math.cos(rad) * 0.33),
            p1_u=(math.sin(rad) * 0.25, 2.12, math.cos(rad) * 0.25),
            width=0.036,
            thickness=0.036,
            mat="R1_GreyMetal",
            bevel=0.0,
        )
    # Conical rain/slag cap at the peak (`y = 2.12 .. 2.36m`)
    mb.add_cylinder(
        center_u=(0.0, 2.24, 0.0),
        radius=0.38,
        radius_top=0.02,
        length=0.24,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=20,
        bevel=0.006,
        bevel_segments=1,
    )

    # --- Left (+X) side ore feed hopper mounting throat (`X = +0.78 .. +0.96, Y = 0.54 .. 0.96, Z = +0.16`) ---
    mb.add_bevel_box(
        center_u=(0.86, 0.74, 0.16),
        size_u=(0.24, 0.42, 0.46),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )

    # --- Right (-X) ingot discharge hood & angled roller conveyor chute ---
    # 1. Brushed steel arched discharge hood (`X = -0.88, Y = 0.74, Z = +0.14`)
    mb.add_bevel_box(
        center_u=(-0.88, 0.74, 0.14),
        size_u=(0.28, 0.56, 0.48),
        mat="R1_BareSteel",
        bevel=0.028,
        segments=1,
    )
    # Glowing amber interior throat inside the discharge hood
    mb.add_bevel_box(
        center_u=(-0.94, 0.70, 0.14),
        size_u=(0.18, 0.42, 0.36),
        mat="R1_LampAmber",
        bevel=0.0,
        segments=0,
    )
    # Beacon mounting collar on top of the discharge hood (`X = -0.90, Y = 1.04, Z = +0.14`)
    mb.add_cylinder(
        center_u=(-0.90, 1.04, 0.14),
        radius=0.072,
        length=0.04,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.005,
        bevel_segments=1,
    )

    # 2. Angled roller conveyor bed sloping from `(-0.86, 0.52, 0.14)` down to `(-1.54, 0.28, 0.14)`
    conv_cx, conv_cy, conv_cz = -1.20, 0.40, 0.14
    conv_tilt_z = -19.5  # Slopes down toward -X
    for sz in (-0.18, 0.18):
        mb.add_bevel_box(
            center_u=(conv_cx, conv_cy, conv_cz + sz),
            size_u=(0.76, 0.065, 0.038),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
            rot_deg_u=(0.0, 0.0, conv_tilt_z),
        )
    # Dark bed floor & 5 steel conveyor rollers
    mb.add_bevel_box(
        center_u=(conv_cx, conv_cy - 0.015, conv_cz),
        size_u=(0.72, 0.028, 0.32),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, 0.0, conv_tilt_z),
    )
    for r_idx in range(5):
        t = r_idx / 4.0
        rx = -0.90 * (1.0 - t) + (-1.50) * t
        ry = 0.51 * (1.0 - t) + 0.29 * t
        mb.add_cylinder(
            center_u=(rx, ry, conv_cz),
            radius=0.026,
            length=0.33,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=10,
            bevel=0.0,
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: HopperDoor + ore chunks, DamperFlap_1..4 around chimney base, HoodBeacon, and cast ingots."""
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # --- 1. HopperDoor (angled trapezoidal ore chute on +X side with basalt ore chunks inside) ---
    hop_piv = (0.94, 0.62, 0.16)
    hd_mb = r1kit.MeshBuilder("HopperDoor", pivot_u=hop_piv)
    # Sloped chute trough walls
    hd_mb.add_bevel_box(
        center_u=(1.10, 0.80, 0.16),
        size_u=(0.40, 0.04, 0.44),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
        rot_deg_u=(0.0, 0.0, 28.0),
    )
    for sz in (-0.21, 0.21):
        hd_mb.add_bevel_box(
            center_u=(1.06, 0.90, 0.16 + sz),
            size_u=(0.38, 0.24, 0.036),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
            rot_deg_u=(0.0, 0.0, 24.0),
        )
    # Chunky raw basalt/steel ore rocks sitting inside the hopper chute
    for ox, oy, oz, osize, orot in (
        (1.02, 0.92, 0.10, (0.11, 0.09, 0.12), (14.0, 22.0, -18.0)),
        (1.10, 0.96, 0.22, (0.12, 0.10, 0.11), (-12.0, -18.0, 15.0)),
        (0.98, 0.98, 0.20, (0.09, 0.08, 0.10), (8.0, 35.0, -10.0)),
    ):
        hd_mb.add_bevel_box(
            center_u=(ox, oy, oz),
            size_u=osize,
            mat="R1_BareSteel",
            bevel=0.018,
            segments=1,
            rot_deg_u=orot,
        )
    hd_mb.build_object(parent=stage_4)

    # --- 2. DamperFlap_1..4 (4 radial exhaust damper petals around the base of the chimney louver stack) ---
    flap_specs = [
        ("DamperFlap_1", (0.0, 1.74, 0.36), (-28.0, 0.0, 0.0)),    # +Z (North)
        ("DamperFlap_2", (0.36, 1.74, 0.0), (0.0, 0.0, -28.0)),    # +X (East)
        ("DamperFlap_3", (0.0, 1.74, -0.36), (28.0, 0.0, 0.0)),    # -Z (South)
        ("DamperFlap_4", (-0.36, 1.74, 0.0), (0.0, 0.0, 28.0)),    # -X (West)
    ]
    for f_name, f_piv, f_rot in flap_specs:
        df_mb = r1kit.MeshBuilder(f_name, pivot_u=f_piv)
        # Offset outward/upward from pivot so the petal stands proudly around the chimney base
        dx = 0.06 if f_piv[0] > 0.1 else (-0.06 if f_piv[0] < -0.1 else 0.0)
        dz = 0.06 if f_piv[2] > 0.1 else (-0.06 if f_piv[2] < -0.1 else 0.0)
        sx_w = 0.18 if abs(dx) < 0.01 else 0.032
        sz_w = 0.18 if abs(dz) < 0.01 else 0.032
        df_mb.add_bevel_box(
            center_u=(f_piv[0] + dx, f_piv[1] + 0.11, f_piv[2] + dz),
            size_u=(sx_w, 0.24, sz_w),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
            rot_deg_u=f_rot,
        )
        df_mb.build_object(parent=stage_4)

    # --- 3. HoodBeacon (glowing amber strobe dome atop the right ingot discharge hood + cast ingots on conveyor) ---
    hb_piv = (-0.90, 1.06, 0.14)
    hb_mb = r1kit.MeshBuilder("HoodBeacon", pivot_u=hb_piv)
    hb_mb.add_cylinder(
        center_u=(-0.90, 1.13, 0.14),
        radius=0.058,
        radius_top=0.046,
        length=0.13,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=14,
        bevel=0.018,
        bevel_segments=1,
    )
    # Freshly cast trapezoidal ingots sliding down the roller conveyor:
    # Upper ingot: silver brushed steel (`R1_BareSteel`)
    hb_mb.add_ingot(
        center_u=(-1.06, 0.51, 0.14),
        size_u=(0.22, 0.085, 0.14),
        mat="R1_BareSteel",
        bevel=0.008,
        rot_deg_u=(0.0, 0.0, -19.5),
    )
    # Lower ingot: glowing warm amber/copper ingot (`R1_LampAmber`)
    hb_mb.add_ingot(
        center_u=(-1.34, 0.41, 0.14),
        size_u=(0.22, 0.085, 0.14),
        mat="R1_LampAmber",
        bevel=0.008,
        rot_deg_u=(0.0, 0.0, -19.5),
    )
    hb_mb.build_object(parent=stage_4)

    return stage_4


def pose_smelter_rest(root: bpy.types.Object) -> None:
    """Set Smelter moving parts to their closed/stowed rest pose."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "HopperDoor" in objs:
        objs["HopperDoor"].rotation_euler = (0.0, 0.0, 0.0)
    for idx in range(1, 5):
        name = f"DamperFlap_{idx}"
        if name in objs:
            objs[name].rotation_euler = (0.0, 0.0, 0.0)


def pose_smelter_active(root: bpy.types.Object) -> None:
    """Set Smelter moving parts to their active smelting pose (hopper tilted open, chimney damper flaps flared)."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "HopperDoor" in objs:
        objs["HopperDoor"].rotation_euler = (0.0, math.radians(14.0), 0.0)
    flap_eulers = {
        "DamperFlap_1": (math.radians(18.0), 0.0, 0.0),
        "DamperFlap_2": (0.0, math.radians(18.0), 0.0),
        "DamperFlap_3": (math.radians(-18.0), 0.0, 0.0),
        "DamperFlap_4": (0.0, math.radians(-18.0), 0.0),
    }
    for name, rot in flap_eulers.items():
        if name in objs:
            objs[name].rotation_euler = rot


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
            concept_path=concept_img,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
