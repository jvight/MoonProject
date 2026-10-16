"""Procedural Blender 5.1 generator for the R1 `Fabricator` structure (`tools/art/blender/fabricator.py`).

Hierarchy contract (DESIGN.md §3.6 / TASK.md §3):
  Fabricator (root empty at 0,0,0)
   +-- Stage_1           # Pillar base pads, central circular platen pedestal, twin front cable boxes, rear power sockets
   |    +-- Socket_Power_1
   |    +-- Socket_Power_2
   +-- Stage_2           # Wide open-arch gantry frame: sage-green side pillars, chamfered cream-enamel top hood with orange shoulder stripes, interior feedstock bin
   +-- Stage_3           # Overhead brass gantry rail, amber ceiling strip lights, central rotary build platen, front-left slide-out delivery tray
   |    +-- BuildPlaten  # Circular brushed-brass rotary build table on sage-green collar
   |    +-- OutputSlide  # Front-left slide-out delivery tray carrying a fabricated cream/copper/brass motor cartridge
   +-- Stage_4           # Translating overhead gantry bridge, twin articulated brass tool arms, and front 6-segment amber progress ring
        +-- GantryBridge # Translating brass gantry carriage bridge with twin bolted carriage blocks & festoon cables
        |    +-- ToolArm_L # Left (+X) multi-jointed brass print/weld arm
        |    +-- ToolArm_R # Right (-X) multi-jointed brass print/laser arm
        +-- ProgressRing # Circular 6-segment amber progress indicator ring on the right-hand front pillar
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
    """Stage 1: Side pillar base pads, central cylindrical platen pedestal, front cable boxes & draped cables, rear power sockets."""
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # --- Left (+X) and Right (-X) pillar base pads (`X = ±0.92, Z = -0.50 .. +0.50`) ---
    for sx in (-1.0, 1.0):
        px = sx * 0.92
        mb.add_bevel_box(
            center_u=(px, 0.025, 0.0),
            size_u=(0.34, 0.05, 1.04),
            mat="R1_GreyMetal",
            bevel=0.010,
            segments=1,
        )

    # --- Central cylindrical rotary platen base pedestal (`radius = 0.42m, Y = 0.0 .. 0.32m`) ---
    mb.add_cylinder(
        center_u=(0.0, 0.16, 0.0),
        radius=0.43,
        radius_top=0.41,
        length=0.32,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=24,
        bevel=0.012,
        bevel_segments=1,
    )
    # Front twin calibration rib brackets on the pedestal base (matching concept)
    for bx in (-0.045, 0.045):
        mb.add_bevel_box(
            center_u=(bx, 0.13, 0.42),
            size_u=(0.028, 0.24, 0.06),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )
    # Horizontal tie conduits linking central pedestal to left/right pillars
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.58, 0.16, -0.06),
            size_u=(0.44, 0.14, 0.24),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )

    # --- Twin front pillar lower cable gland boxes (`X = ±0.92, Y = 0.34, Z = +0.51`) & twin braided cables ---
    for sx in (-1.0, 1.0):
        gx = sx * 0.92
        mb.add_bevel_box(
            center_u=(gx, 0.35, 0.51),
            size_u=(0.14, 0.13, 0.06),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        for cx_off in (-0.028, 0.028):
            mb.add_tube_path(
                points_u=[
                    (gx + cx_off, 0.31, 0.525),
                    (gx + cx_off, 0.18, 0.54),
                    (gx + cx_off, 0.02, 0.53),
                ],
                radius=0.015,
                mat="R1_Rubber",
                segments=6,
                cap_ends=True,
            )

    # --- Rear twin power cable sockets (`Socket_Power_1`, `Socket_Power_2`) on right-hand (-X) pillar rear ---
    mb.add_bevel_box(
        center_u=(-0.92, 0.38, -0.53),
        size_u=(0.24, 0.34, 0.08),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    for idx, sy in enumerate((0.45, 0.31), start=1):
        _ = idx
        mb.add_cylinder(
            center_u=(-0.92, sy, -0.58),
            radius=0.040,
            length=0.03,
            mat="R1_Brass",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_cylinder(
            center_u=(-0.92, sy, -0.60),
            radius=0.022,
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
        pivot_u=(-0.92, 0.45, -0.61),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    r1kit.create_empty(
        name="Socket_Power_2",
        parent=stage_1_obj,
        pivot_u=(-0.92, 0.31, -0.61),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Wide open-arch gantry frame: sage-green side pillars, chamfered cream-enamel top hood with orange shoulder stripes, interior feedstock bin."""
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    # --- 1. Left (+X) and Right (-X) muted sage-green lower side pillars (`X = ±0.92, Y = 0.05 .. 1.22, Z = -0.50 .. +0.50`) ---
    for sx in (-1.0, 1.0):
        px = sx * 0.92
        mb.add_bevel_box(
            center_u=(px, 0.635, 0.0),
            size_u=(0.30, 1.17, 1.00),
            mat="R1_SagePatch",
            bevel=0.028,
            segments=1,
        )
        # Outer side recessed riveted access panel + 2 heavy barrel hinges along the Y=1.22 hood seam
        panel_x = px + sx * 0.148
        mb.add_bevel_box(
            center_u=(panel_x, 0.60, 0.0),
            size_u=(0.022, 0.88, 0.72),
            mat="R1_SagePatch",
            bevel=0.008,
            segments=1,
        )
        # Rivet rows along the outer side access panel
        panel_bolts = []
        for by in (0.22, 0.41, 0.60, 0.79, 0.98):
            panel_bolts.append((panel_x + sx * 0.010, by, -0.31))
            panel_bolts.append((panel_x + sx * 0.010, by, 0.31))
        for bz in (-0.15, 0.0, 0.15):
            panel_bolts.append((panel_x + sx * 0.010, 0.98, bz))
            panel_bolts.append((panel_x + sx * 0.010, 0.22, bz))
        mb.add_bolts(
            panel_bolts,
            radius=0.009,
            height=0.007,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )
        # 2 heavy vertical barrel hinges across the Y=1.22 parting seam on the outer wall
        for hz in (-0.24, 0.24):
            mb.add_bevel_box(
                center_u=(panel_x + sx * 0.012, 1.22, hz),
                size_u=(0.032, 0.15, 0.075),
                mat="R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )
            mb.add_cylinder(
                center_u=(panel_x + sx * 0.022, 1.22, hz),
                radius=0.016,
                length=0.09,
                mat="R1_BareSteel",
                axis_u="Z",
                segments=8,
                bevel=0.0,
                bevel_segments=0,
            )

    # Dark horizontal parting seam band at Y = 1.22 m on both pillars
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.92, 1.22, 0.0),
            size_u=(0.308, 0.018, 1.008),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )

    # --- 2. Upper chamfered warm cream-enamel gantry hood (`Y = 1.22 .. 1.84m`) ---
    # Left and right vertical hood haunches (`Y = 1.22 .. 1.50m`)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.92, 1.36, 0.0),
            size_u=(0.30, 0.28, 1.00),
            mat="R1_CreamEnamel",
            bevel=0.024,
            segments=1,
        )
        # Horizontal orange shoulder stripe wrapping flush around the cream hood haunch (`Y = 1.40 .. 1.48m`)
        mb.add_bevel_box(
            center_u=(sx * 0.92, 1.44, 0.0),
            size_u=(0.306, 0.082, 1.006),
            mat="R1_OrangeStripe",
            bevel=0.0,
            segments=0,
        )

    # Chamfered top cream-enamel arch bridge spanning `X = -1.06 .. +1.06, Y = 1.50 .. 1.84`
    mb.add_bevel_box(
        center_u=(0.0, 1.67, 0.0),
        size_u=(2.12, 0.34, 1.00),
        mat="R1_CreamEnamel",
        bevel=0.045,
        segments=1,
        top_scale_xz=(0.82, 0.98),
    )
    # Recessed dark inner ceiling soffit inside the arch (`X = -0.76 .. +0.76, Y = 1.54`)
    mb.add_bevel_box(
        center_u=(0.0, 1.52, 0.0),
        size_u=(1.54, 0.05, 0.88),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    # Central panel seam groove on top of the cream arch at X = 0.0
    mb.add_bevel_box(
        center_u=(0.0, 1.68, 0.0),
        size_u=(0.016, 0.35, 1.008),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    # Front and rear bolt fasteners on the cream hood corners & sage pillar tops
    for sz in (-0.505, 0.505):
        mb.add_bolts(
            [
                (-0.98, 1.15, sz),
                (-0.84, 1.15, sz),
                (-0.98, 1.30, sz),
                (-0.84, 1.30, sz),
                (0.84, 1.15, sz),
                (0.98, 1.15, sz),
                (0.84, 1.30, sz),
                (0.98, 1.30, sz),
            ],
            radius=0.010,
            height=0.008,
            axis_u="Z",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # --- 3. Interior left-wall (+X) sage-green feedstock bin with ingots & glass beakers (`X = +0.58, Y = 0.62, Z = -0.04`) ---
    # Sloped open-top bin box mounted against the inner face of the left (+X) pillar
    mb.add_bevel_box(
        center_u=(0.58, 0.60, -0.04),
        size_u=(0.36, 0.36, 0.64),
        mat="R1_SagePatch",
        bevel=0.012,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.56, 0.78, -0.04),
        size_u=(0.32, 0.06, 0.60),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    # 3 trapezoidal brushed-steel ingots + 2 dark glass cylinder beakers protruding from the feedstock bin
    mb.add_ingot(
        center_u=(0.56, 0.85, 0.14),
        size_u=(0.11, 0.14, 0.08),
        mat="R1_BareSteel",
        bevel=0.005,
        rot_deg_u=(0.0, 8.0, -18.0),
    )
    mb.add_ingot(
        center_u=(0.60, 0.86, 0.02),
        size_u=(0.11, 0.14, 0.08),
        mat="R1_BareSteel",
        bevel=0.005,
        rot_deg_u=(0.0, -6.0, -14.0),
    )
    mb.add_cylinder(
        center_u=(0.55, 0.86, -0.16),
        radius=0.052,
        length=0.14,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )

    # --- 4. Right-hand front pillar (`X = -0.92, Y = 0.80, Z = +0.505`) circular brass bezel for ProgressRing ---
    mb.add_cylinder(
        center_u=(-0.92, 0.80, 0.505),
        radius=0.135,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=18,
        bevel=0.008,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.92, 0.80, 0.518),
        radius=0.105,
        length=0.020,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=18,
        bevel=0.0,
        bevel_segments=0,
    )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Overhead brass gantry rail, amber ceiling strip lights, central rotary build platen, front-left slide-out delivery tray."""
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # --- Fixed horizontal overhead brass gantry rail spanning the inner bay (`X = -0.78 .. +0.78, Y = 1.40, Z = 0.02`) ---
    mb.add_bevel_box(
        center_u=(0.0, 1.40, 0.02),
        size_u=(1.56, 0.075, 0.09),
        mat="R1_Brass",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 1.40, 0.068),
        size_u=(1.54, 0.028, 0.018),
        mat="R1_BareSteel",
        bevel=0.0,
        segments=0,
    )
    # Vertical guide rails inside both side pillars
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.76, 0.96, 0.02),
            size_u=(0.036, 0.88, 0.055),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )

    # --- Warm amber overhead work-bay strip lights under the arch ceiling (`Y = 1.50, Z = +0.18`) ---
    for lx in (-0.28, 0.28):
        mb.add_bevel_box(
            center_u=(lx, 1.50, 0.18),
            size_u=(0.46, 0.036, 0.055),
            mat="R1_LampAmber",
            bevel=0.006,
            segments=1,
        )

    stage_3_obj = mb.build_object(parent=root)

    # --- Child 1: BuildPlaten (circular brushed-brass rotary build table on sage-green drum collar) ---
    bp_mb = r1kit.MeshBuilder("BuildPlaten", pivot_u=(0.0, 0.42, 0.0))
    # Sage-green cylindrical drum collar (`y = 0.31 .. 0.47m`, radius `0.48m`)
    bp_mb.add_cylinder(
        center_u=(0.0, 0.39, 0.0),
        radius=0.48,
        length=0.16,
        mat="R1_SagePatch",
        axis_u="Y",
        segments=28,
        bevel=0.010,
        bevel_segments=1,
    )
    # Wide flat brushed-brass rotary build platen disc (`y = 0.47 .. 0.54m`, radius `0.48m`)
    bp_mb.add_cylinder(
        center_u=(0.0, 0.505, 0.0),
        radius=0.485,
        length=0.07,
        mat="R1_Brass",
        axis_u="Y",
        segments=28,
        bevel=0.012,
        bevel_segments=1,
    )
    # Inner concentric brass calibration ring on platen top
    bp_mb.add_cylinder(
        center_u=(0.0, 0.542, 0.0),
        radius=0.32,
        length=0.008,
        mat="R1_Brass",
        axis_u="Y",
        segments=24,
        bevel=0.0,
        bevel_segments=0,
    )
    bp_mb.build_object(parent=stage_3_obj)

    # --- Child 2: OutputSlide (front-left slide-out delivery tray extending toward +X/+Z with fabricated motor core) ---
    slide_piv = (0.38, 0.38, 0.48)
    os_mb = r1kit.MeshBuilder("OutputSlide", pivot_u=slide_piv)
    slide_yaw = 24.0  # Angled toward front-left (+X / +Z) matching s_fabricator.png
    # Twin telescopic steel slide rails under the tray
    os_mb.add_bevel_box(
        center_u=(0.34, 0.35, 0.44),
        size_u=(0.34, 0.042, 0.52),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    # Brushed-steel flat delivery tray bed
    os_mb.add_bevel_box(
        center_u=(0.44, 0.39, 0.58),
        size_u=(0.42, 0.042, 0.58),
        mat="R1_BareSteel",
        bevel=0.006,
        segments=1,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    # Muted sage-green front bumper lip on the outer edge of the slide tray
    os_mb.add_bevel_box(
        center_u=(0.56, 0.395, 0.84),
        size_u=(0.44, 0.062, 0.055),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    # Newly fabricated horizontal cylindrical motor/solenoid cartridge resting on the slide tray
    part_cx, part_cy, part_cz = 0.45, 0.49, 0.60
    os_mb.add_cylinder(
        center_u=(part_cx, part_cy, part_cz),
        radius=0.082,
        length=0.28,
        mat="R1_CreamEnamel",
        axis_u="X",
        segments=14,
        bevel=0.012,
        bevel_segments=1,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    os_mb.add_cylinder(
        center_u=(part_cx, part_cy, part_cz),
        radius=0.086,
        length=0.11,
        mat="R1_Copper",
        axis_u="X",
        segments=14,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    os_mb.add_cylinder(
        center_u=(part_cx, part_cy, part_cz),
        radius=0.048,
        length=0.33,
        mat="R1_Brass",
        axis_u="X",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
        rot_deg_u=(0.0, slide_yaw, 0.0),
    )
    os_mb.build_object(parent=stage_3_obj)

    return stage_3_obj


def _build_tool_arm(
    parent: bpy.types.Object,
    name: str,
    pivot_u: tuple[float, float, float],
    side_sign: float,
) -> bpy.types.Object:
    """Build an articulated multi-jointed brass print/weld tool arm suspended under GantryBridge."""
    px, py, pz = pivot_u
    mb = r1kit.MeshBuilder(name, pivot_u=pivot_u)

    # Upper vertical brass turret mount (`y = py - 0.14 .. py`)
    mb.add_cylinder(
        center_u=(px, py - 0.06, pz),
        radius=0.048,
        length=0.12,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.006,
        bevel_segments=1,
    )
    # Shoulder knuckle joint
    p_shoulder = (px, py - 0.13, pz)
    mb.add_bevel_box(
        center_u=p_shoulder,
        size_u=(0.075, 0.075, 0.075),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    # Curved/angled upper arm link sweeping outward slightly then down to elbow
    p_elbow = (px + side_sign * 0.05, py - 0.30, pz + 0.03)
    mb.add_strut(
        p0_u=p_shoulder,
        p1_u=p_elbow,
        width=0.048,
        thickness=0.048,
        mat="R1_Brass",
        bevel=0.006,
    )
    mb.add_cylinder(
        center_u=p_elbow,
        radius=0.038,
        length=0.075,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Forearm link angling inward toward the center of the platen (`X = 0`)
    p_wrist = (px - side_sign * 0.14, py - 0.38, pz + 0.04)
    mb.add_strut(
        p0_u=p_elbow,
        p1_u=p_wrist,
        width=0.040,
        thickness=0.040,
        mat="R1_Brass",
        bevel=0.005,
    )
    # Precision nozzle / laser stylus pointing inward & down toward platen center
    p_tip = (px - side_sign * 0.25, py - 0.50, pz + 0.04)
    mb.add_strut(
        p0_u=p_wrist,
        p1_u=p_tip,
        width=0.022,
        thickness=0.022,
        mat="R1_BareSteel",
        bevel=0.0,
    )
    mb.add_cylinder(
        center_u=p_tip,
        radius=0.012,
        length=0.024,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    return mb.build_object(parent=parent)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: GantryBridge with ToolArm_L & ToolArm_R, plus ProgressRing on the right-hand front pillar."""
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # --- 1. GantryBridge (twin bolted brass carriage blocks on the overhead rail + draped festoon cables) ---
    gb_piv = (0.0, 0.0, 0.0)
    gb_mb = r1kit.MeshBuilder("GantryBridge", pivot_u=gb_piv)
    for sx in (-1.0, 1.0):
        cx = sx * 0.32
        # Heavy rectangular brass carriage block with 4 corner bolts (matching s_fabricator.png)
        gb_mb.add_bevel_box(
            center_u=(cx, 1.40, 0.04),
            size_u=(0.16, 0.15, 0.12),
            mat="R1_Brass",
            bevel=0.010,
            segments=1,
        )
        gb_mb.add_bolts(
            [
                (cx - 0.05, 1.44, 0.105),
                (cx + 0.05, 1.44, 0.105),
                (cx - 0.05, 1.36, 0.105),
                (cx + 0.05, 1.36, 0.105),
            ],
            radius=0.009,
            height=0.007,
            axis_u="Z",
            mat="R1_BareSteel",
            bevel_segments=0,
        )
        # Draped braided festoon cable looping from inner pillar wall (`X = sx * 0.75`) to the carriage (`X = cx`)
        gb_mb.add_tube_path(
            points_u=[
                (sx * 0.74, 1.32, -0.02),
                (sx * 0.56, 1.15, -0.01),
                (sx * 0.42, 1.20, 0.01),
                (cx + sx * 0.05, 1.34, 0.02),
            ],
            radius=0.016,
            mat="R1_Rubber",
            segments=6,
            cap_ends=True,
        )

    gb_obj = gb_mb.build_object(parent=stage_4)

    # Children of GantryBridge: ToolArm_L (+X) and ToolArm_R (-X)
    _build_tool_arm(gb_obj, "ToolArm_L", pivot_u=(0.32, 1.33, 0.04), side_sign=1.0)
    _build_tool_arm(gb_obj, "ToolArm_R", pivot_u=(-0.32, 1.33, 0.04), side_sign=-1.0)

    # --- 2. ProgressRing (circular 6-segment glowing amber progress indicator on the right-hand front pillar) ---
    pr_piv = (-0.92, 0.80, 0.525)
    pr_mb = r1kit.MeshBuilder("ProgressRing", pivot_u=pr_piv)
    for seg_i in range(6):
        ang_deg = seg_i * 60.0 + 30.0
        rad = math.radians(ang_deg)
        sx = -0.92 + math.sin(rad) * 0.072
        sy = 0.80 + math.cos(rad) * 0.072
        pr_mb.add_bevel_box(
            center_u=(sx, sy, 0.528),
            size_u=(0.052, 0.024, 0.016),
            mat="R1_LampAmber",
            bevel=0.003,
            segments=1,
            rot_deg_u=(0.0, 0.0, -ang_deg),
        )
    pr_mb.build_object(parent=stage_4)

    return stage_4


def pose_fabricator_rest(root: bpy.types.Object) -> None:
    """Set Fabricator moving parts to their stowed rest pose."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "GantryBridge" in objs:
        objs["GantryBridge"].location = r1kit.u_pos(0.0, 0.0, 0.0)
    if "ToolArm_L" in objs:
        objs["ToolArm_L"].rotation_euler = (0.0, 0.0, 0.0)
    if "ToolArm_R" in objs:
        objs["ToolArm_R"].rotation_euler = (0.0, 0.0, 0.0)
    if "BuildPlaten" in objs:
        objs["BuildPlaten"].rotation_euler = (0.0, 0.0, 0.0)
    if "OutputSlide" in objs:
        objs["OutputSlide"].location = r1kit.u_pos(0.38, 0.38, 0.48)


def pose_fabricator_active(root: bpy.types.Object) -> None:
    """Set Fabricator moving parts to their active fabricating & part-delivery pose."""
    objs = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "GantryBridge" in objs:
        objs["GantryBridge"].location = r1kit.u_pos(0.0, 0.0, 0.04)
    if "ToolArm_L" in objs:
        objs["ToolArm_L"].rotation_euler = (0.0, math.radians(-8.0), 0.0)
    if "ToolArm_R" in objs:
        objs["ToolArm_R"].rotation_euler = (0.0, math.radians(8.0), 0.0)
    if "BuildPlaten" in objs:
        objs["BuildPlaten"].rotation_euler = (0.0, 0.0, math.radians(25.0))
    if "OutputSlide" in objs:
        objs["OutputSlide"].location = r1kit.u_pos(0.44, 0.38, 0.56)


def build_fabricator() -> bpy.types.Object:
    """Build the complete Fabricator hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("Fabricator", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_fabricator_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=1.22)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build Fabricator FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/Fabricator.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_fabricator()
    tris = r1kit.count_triangles(root)
    print(f"[Fabricator] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        concept_img = Path("D:/Project/MoonProject-wt/agy-concept/Logs/base/s_fabricator.png")
        r1kit.render_structure_suite(
            root=root,
            slug="fabricator",
            out_dir=args.renders_dir,
            target_height=1.84,
            footprint_radius=1.22,
            pose_rest_fn=pose_fabricator_rest,
            pose_active_fn=pose_fabricator_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
            cam_side_x=-1.0,
            concept_path=concept_img,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
