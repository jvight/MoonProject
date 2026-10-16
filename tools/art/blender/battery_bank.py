"""Procedural Blender 5.1 generator for the R1 `BatteryBank` structure (`tools/art/blender/battery_bank.py`).

Hierarchy contract (DESIGN.md §3.3 / TASK.md §1):
  BatteryBank (root empty at 0,0,0)
   +-- Stage_1           # Base tray, 4 corner feet, 4 cell-cradle saddles, partition fins, copper busbar
   +-- Stage_2           # Rounded cream-enamel vault cabinet, mid-belt orange stripe, sage corner patch, side junction box
   |    +-- Socket_Power_1
   |    +-- Socket_Power_2
   +-- Stage_3           # Overhead manifold + 4 cylindrical dark-charcoal cells (brass caps, sage patch on #3)
   +-- Stage_4           # Moving parts + emissive lamps
        +-- SafetyBar    # Front U-shaped lock bar (pivots -90 deg forward/down around X at lower hinge)
        +-- Plunger_1    # Top brass contact plunger cell 1 (slides +0.06m in Y)
        +-- Plunger_2
        +-- Plunger_3
        +-- Plunger_4
        +-- RotaryDial   # Side rotary charge selector dial (rotates +90 deg around X)
        +-- SightGlass_1 # Emissive amber pill sight-glass cell 1 (R1_LampAmber)
        +-- SightGlass_2
        +-- SightGlass_3
        +-- SightGlass_4
        +-- RoofBeacon   # Emissive amber dome beacon on cabinet roof (R1_LampAmber)
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

CELL_XS: tuple[float, ...] = (0.39, 0.13, -0.13, -0.39)  # Cells 1..4 from viewer-left (+X) to viewer-right (-X)
CELL_Z: float = 0.25
SAFETY_PIVOT_U: tuple[float, float, float] = (0.0, 0.24, 0.43)
DIAL_PIVOT_U: tuple[float, float, float] = (-0.68, 0.96, -0.02)
BEACON_PIVOT_U: tuple[float, float, float] = (0.0, 1.46, 0.02)
PLUNGER_BASE_Y: float = 1.15
PLUNGER_RISE_Y: float = 0.06
SIGHT_GLASS_Y: float = 0.64
SIGHT_GLASS_Z: float = 0.372


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Base tray, 4 corner feet, 4 contoured cell-cradle saddles, partition fins, copper busbar."""
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # --- 4 corner block feet under the vault ---
    foot_corners = [
        (-0.56, -0.34),
        (0.56, -0.34),
        (-0.56, 0.34),
        (0.56, 0.34),
    ]
    for fx, fz in foot_corners:
        sx = -1.0 if fx < 0.0 else 1.0
        mb.add_bevel_box(
            center_u=(fx, 0.020, fz),
            size_u=(0.18, 0.040, 0.20),
            mat="R1_Rubber",
            bevel=0.0,
            segments=0,
        )
        mb.add_bevel_box(
            center_u=(fx, 0.075, fz),
            size_u=(0.16, 0.090, 0.18),
            mat="R1_GreyMetal",
            bevel=0.012,
            segments=1,
        )
        mb.add_bolts(
            [(fx + sx * 0.055, 0.045, fz)],
            radius=0.013,
            height=0.012,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # --- Lower dark-metal chassis tray & recessed alcove floor ---
    mb.add_bevel_box(
        center_u=(0.0, 0.14, 0.0),
        size_u=(1.26, 0.08, 0.80),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.24, 0.18),
        size_u=(1.10, 0.05, 0.44),
        mat="R1_BareSteel",
        bevel=0.008,
        segments=1,
    )

    # --- 4 contoured semicircular lower cell-cradle saddles & 5 partition fins ---
    for cx in CELL_XS:
        mb.add_bevel_box(
            center_u=(cx, 0.285, CELL_Z),
            size_u=(0.21, 0.055, 0.24),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
        # Semicircular front cradle collar lip around each cell base
        mb.add_torus(
            center_u=(cx, 0.325, CELL_Z),
            major_radius=0.116,
            minor_radius=0.018,
            mat="R1_BareSteel",
            axis_u="Y",
            major_segments=6,
            minor_segments=4,
            arc_deg=180.0,
            start_deg=180.0,
        )

    # 5 vertical steel partition fins separating and flanking the 4 cell bays
    for fx in (-0.51, -0.26, 0.0, 0.26, 0.51):
        mb.add_bevel_box(
            center_u=(fx, 0.34, CELL_Z + 0.02),
            size_u=(0.028, 0.14, 0.26),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
        )

    # --- Heavy horizontal copper busbar across the lower cell-cradle base ---
    mb.add_bevel_box(
        center_u=(0.0, 0.28, CELL_Z + 0.12),
        size_u=(1.04, 0.028, 0.032),
        mat="R1_Copper",
        bevel=0.0,
        segments=0,
    )
    for cx in CELL_XS:
        mb.add_cylinder(
            center_u=(cx, 0.28, CELL_Z + 0.12),
            radius=0.020,
            length=0.048,
            mat="R1_Brass",
            axis_u="Y",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )

    # --- Lower front hinge sockets for SafetyBar (pivot at Y=0.24, Z=0.43) ---
    for sx in (-1.0, 1.0):
        hx = sx * 0.49
        mb.add_cylinder(
            center_u=(hx, SAFETY_PIVOT_U[1], SAFETY_PIVOT_U[2] - 0.01),
            radius=0.032,
            length=0.055,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Rounded cream-enamel vault shell, mid-belt orange stripe, sage corner patch, side junction box & sockets."""
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    # --- Unified rounded-edge cream-enamel submarine vault enclosure ---
    # Rear insulated vault wall (z = -0.44 .. +0.08, y = 0.15 .. 1.46)
    mb.add_bevel_box(
        center_u=(0.0, 0.805, -0.18),
        size_u=(1.36, 1.31, 0.52),
        mat="R1_CreamEnamel",
        bevel=0.075,
        segments=2,
    )
    # Left (+X) and Right (-X) rounded side cheek walls extending to z = +0.43
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.60, 0.805, 0.0),
            size_u=(0.16, 1.31, 0.86),
            mat="R1_CreamEnamel",
            bevel=0.055,
            segments=2,
        )
    # Top vault ceiling hood extending to z = +0.43
    mb.add_bevel_box(
        center_u=(0.0, 1.39, 0.0),
        size_u=(1.36, 0.14, 0.86),
        mat="R1_CreamEnamel",
        bevel=0.055,
        segments=2,
    )
    # Bottom vault threshold lip extending to z = +0.43
    mb.add_bevel_box(
        center_u=(0.0, 0.20, 0.0),
        size_u=(1.36, 0.10, 0.86),
        mat="R1_CreamEnamel",
        bevel=0.040,
        segments=1,
    )
    # Front submarine airlock portal ring framing the recessed cell bay (z_front = 0.435)
    mb.add_rounded_portal_ring(
        w_in=1.08,
        y_bot_in=0.25,
        y_top_in=1.32,
        r_bot_in=0.06,
        r_top_in=0.07,
        w_out=1.35,
        y_bot_out=0.16,
        y_top_out=1.45,
        r_bot_out=0.09,
        r_top_out=0.11,
        thickness=0.06,
        mat="R1_CreamEnamel",
        z_front=0.435,
        corner_segments=4,
    )
    # Inner dark-metal gasket frame lining the recessed cell bay opening
    mb.add_rounded_portal_ring(
        w_in=1.05,
        y_bot_in=0.26,
        y_top_in=1.31,
        r_bot_in=0.05,
        r_top_in=0.06,
        w_out=1.10,
        y_bot_out=0.24,
        y_top_out=1.33,
        r_bot_out=0.06,
        r_top_out=0.07,
        thickness=0.05,
        mat="R1_GreyMetal",
        z_front=0.415,
        corner_segments=3,
    )

    # Dark interior alcove back wall & ceiling liner behind the 4 cells (z = 0.08)
    mb.add_bevel_box(
        center_u=(0.0, 0.78, 0.08),
        size_u=(1.08, 1.08, 0.04),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    # Rear external cooling ribs on back wall (z = -0.45)
    for rx in (-0.38, 0.0, 0.38):
        mb.add_bevel_box(
            center_u=(rx, 0.78, -0.45),
            size_u=(0.10, 0.84, 0.04),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )

    # --- Continuous warm-orange beltline stripe (`R1_OrangeStripe`) at mid-height (Y = 0.64) ---
    for sx in (-1.0, 1.0):
        # Side wall orange stripe
        mb.add_bevel_box(
            center_u=(sx * 0.662, 0.64, 0.0),
            size_u=(0.045, 0.11, 0.84),
            mat="R1_OrangeStripe",
            bevel=0.0,
            segments=0,
        )
        # Front jamb wrap of the orange stripe
        mb.add_bevel_box(
            center_u=(sx * 0.605, 0.64, 0.412),
            size_u=(0.13, 0.11, 0.052),
            mat="R1_OrangeStripe",
            bevel=0.0,
            segments=0,
        )
    # Rear wall orange stripe
    mb.add_bevel_box(
        center_u=(0.0, 0.64, -0.425),
        size_u=(1.32, 0.11, 0.045),
        mat="R1_OrangeStripe",
        bevel=0.0,
        segments=0,
    )

    # --- Muted sage-green field-repair patch (`R1_SagePatch`) wrapping lower front-right (-X) corner ---
    mb.add_bevel_box(
        center_u=(-0.60, 0.38, 0.412),
        size_u=(0.14, 0.28, 0.052),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(-0.668, 0.38, 0.22),
        size_u=(0.042, 0.28, 0.40),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )
    # Perimeter hex rivets on sage repair patch + right side seam
    mb.add_bolt_row((-0.55, 0.27, 0.44), (-0.55, 0.49, 0.44), count=3, radius=0.009, height=0.008, axis_u="Z", bevel_segments=0)
    mb.add_bolt_row((-0.69, 0.27, 0.05), (-0.69, 0.49, 0.05), count=3, radius=0.009, height=0.008, axis_u="X", bevel_segments=0)
    mb.add_bolt_row((-0.69, 0.27, 0.38), (-0.69, 0.49, 0.38), count=3, radius=0.009, height=0.008, axis_u="X", bevel_segments=0)
    mb.add_bolt_row((-0.69, 0.76, 0.38), (-0.69, 1.28, 0.38), count=4, radius=0.009, height=0.008, axis_u="X", bevel_segments=0)

    # --- External barrel hinges on viewer-left (+X) front jamb ---
    for hy in (0.48, 1.02):
        mb.add_bevel_box(
            center_u=(0.64, hy, 0.415),
            size_u=(0.06, 0.13, 0.032),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )
        mb.add_cylinder(
            center_u=(0.67, hy, 0.422),
            radius=0.020,
            length=0.14,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

    # --- Viewer-right (-X) side rotary selector dial bezel at DIAL_PIVOT_U ---
    dx, dy, dz = DIAL_PIVOT_U
    mb.add_bevel_box(
        center_u=(dx - 0.005, dy, dz),
        size_u=(0.032, 0.23, 0.23),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(dx - 0.015, dy, dz),
        radius=0.088,
        length=0.024,
        mat="R1_BareSteel",
        axis_u="X",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    for sy in (-0.085, 0.085):
        for sz in (-0.085, 0.085):
            mb.add_bolts([(dx - 0.022, dy + sy, dz + sz)], radius=0.009, height=0.008, axis_u="X", mat="R1_BareSteel", bevel_segments=0)

    # --- Viewer-right (-X) side power gland plate, twin coaxial sockets, and braided cable ---
    mb.add_bevel_box(
        center_u=(-0.685, 0.96, -0.27),
        size_u=(0.036, 0.24, 0.22),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    for sy in (1.01, 0.90):
        mb.add_cylinder(
            center_u=(-0.71, sy, -0.27),
            radius=0.038,
            length=0.030,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        mb.add_cylinder(
            center_u=(-0.728, sy, -0.27),
            radius=0.024,
            length=0.020,
            mat="R1_Copper",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    # Heavy braided cable curving down from upper-right gland along the side of the cabinet
    mb.add_tube_path(
        points_u=[
            (-0.72, 0.90, -0.27),
            (-0.75, 0.78, -0.25),
            (-0.74, 0.48, -0.20),
            (-0.71, 0.18, -0.15),
            (-0.64, 0.12, -0.12),
        ],
        radius=0.022,
        mat="R1_Rubber",
        segments=7,
        cap_ends=True,
    )

    # --- Roof beacon base collar at top of cabinet (Y = 1.46) ---
    bx, by, bz = BEACON_PIVOT_U
    mb.add_cylinder(
        center_u=(bx, by + 0.005, bz),
        radius=0.145,
        length=0.028,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(bx, by + 0.022, bz),
        radius=0.122,
        length=0.020,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.0,
        bevel_segments=0,
    )
    # 4 roof corner bolts matching s_battery_bank.png
    for sx in (-0.42, 0.42):
        for sz in (-0.28, 0.28):
            mb.add_bolts([(sx, 1.465, sz)], radius=0.012, height=0.010, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    stage_2_obj = mb.build_object(parent=root)

    # Power socket empties pointing outward (-X)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_2_obj,
        pivot_u=(-0.74, 1.01, -0.27),
        outward_dir_u=(-1.0, 0.0, 0.0),
    )
    r1kit.create_empty(
        name="Socket_Power_2",
        parent=stage_2_obj,
        pivot_u=(-0.74, 0.90, -0.27),
        outward_dir_u=(-1.0, 0.0, 0.0),
    )
    return stage_2_obj


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Overhead charging manifold + 4 vertical cylindrical dark-charcoal storage cells."""
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # --- Overhead charging manifold bar inside the upper alcove ceiling (y = 1.29) ---
    mb.add_bevel_box(
        center_u=(0.0, 1.29, CELL_Z),
        size_u=(1.04, 0.045, 0.26),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    for cx in CELL_XS:
        mb.add_cylinder(
            center_u=(cx, 1.26, CELL_Z),
            radius=0.052,
            length=0.040,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

    # --- 4 vertical cylindrical battery cells ---
    for idx, cx in enumerate(CELL_XS, start=1):
        # Stepped brass bottom cap (y = 0.30 .. 0.38)
        mb.add_cylinder(
            center_u=(cx, 0.34, CELL_Z),
            radius=0.114,
            length=0.075,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.0,
            bevel_segments=0,
        )

        # Main cylindrical dark-charcoal cell body (y = 0.38 .. 0.96)
        mb.add_cylinder(
            center_u=(cx, 0.67, CELL_Z),
            radius=0.112,
            length=0.58,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=12,
            bevel=0.0,
            bevel_segments=0,
        )

        # Upper and lower moulded dark-steel bands around cell cylinder
        for by in (0.43, 0.89):
            mb.add_cylinder(
                center_u=(cx, by, CELL_Z),
                radius=0.118,
                length=0.036,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=12,
                bevel=0.0,
                bevel_segments=0,
            )

        # Cell 3 (X = -0.13, second from viewer-right) wears a riveted sage-green field-repair band!
        if idx == 3:
            mb.add_cylinder(
                center_u=(cx, 0.66, CELL_Z),
                radius=0.118,
                length=0.20,
                mat="R1_SagePatch",
                axis_u="Y",
                segments=12,
                bevel=0.0,
                bevel_segments=0,
            )
            for ry in (0.59, 0.73):
                for sx in (-1.0, 1.0):
                    mb.add_bolts(
                        [(cx + sx * 0.095, ry, CELL_Z + 0.075)],
                        radius=0.008,
                        height=0.008,
                        axis_u="Z",
                        mat="R1_BareSteel",
                        bevel_segments=0,
                    )

        # Thick flat-topped cylindrical brass top cap (y = 0.96 .. 1.04) matching s_battery_bank.png
        mb.add_cylinder(
            center_u=(cx, 1.00, CELL_Z),
            radius=0.115,
            length=0.075,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.006,
            bevel_segments=1,
        )

        # Front pill-shaped sight-glass bezel frame (houses SightGlass_1..4 in Stage_4)
        bz_z = CELL_Z + 0.110
        mb.add_bevel_box(
            center_u=(cx, SIGHT_GLASS_Y, bz_z),
            size_u=(0.076, 0.38, 0.028),
            mat="R1_Brass",
            bevel=0.008,
            segments=1,
        )

    return mb.build_object(parent=root)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: SafetyBar, Plunger_1..4, RotaryDial, SightGlass_1..4, and RoofBeacon."""
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. SafetyBar (U-shaped tubular steel bar hinged at SAFETY_PIVOT_U = (0.0, 0.24, 0.43))
    py, pz = SAFETY_PIVOT_U[1], SAFETY_PIVOT_U[2]
    sb_mb = r1kit.MeshBuilder("SafetyBar", pivot_u=SAFETY_PIVOT_U)
    # Continuous U-shaped tubular lock bar standing up to Y = 0.48 in rest pose, swinging forward/down in active pose
    sb_mb.add_tube_path(
        points_u=[
            (-0.49, py, pz),
            (-0.49, py + 0.22, pz + 0.05),
            (-0.44, py + 0.25, pz + 0.06),
            (0.44, py + 0.25, pz + 0.06),
            (0.49, py + 0.22, pz + 0.05),
            (0.49, py, pz),
        ],
        radius=0.022,
        mat="R1_BareSteel",
        segments=8,
        cap_ends=True,
    )
    # Orange & dark-metal grip sleeves on the U-bar
    for sx in (-0.28, 0.28):
        sb_mb.add_cylinder(
            center_u=(sx, py + 0.25, pz + 0.06),
            radius=0.027,
            length=0.14,
            mat="R1_OrangeStripe",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    sb_mb.build_object(parent=stage_4)

    # 2. Plunger_1..4 (vertical brass/copper contact plungers between cell tops Y=1.04 and ceiling Y=1.28)
    for idx, cx in enumerate(CELL_XS, start=1):
        p_pivot = (cx, PLUNGER_BASE_Y, CELL_Z)
        p_mb = r1kit.MeshBuilder(f"Plunger_{idx}", pivot_u=p_pivot)
        # Lower wide brass contact flange resting on cell top cap
        p_mb.add_cylinder(
            center_u=(cx, PLUNGER_BASE_Y - 0.095, CELL_Z),
            radius=0.058,
            length=0.028,
            mat="R1_Brass",
            axis_u="Y",
            segments=10,
            bevel=0.0,
            bevel_segments=0,
        )
        # Vertical brass plunger shaft inside the alcove
        p_mb.add_cylinder(
            center_u=(cx, PLUNGER_BASE_Y, CELL_Z),
            radius=0.026,
            length=0.20,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        # Copper spring collar ring at mid-shaft
        p_mb.add_cylinder(
            center_u=(cx, PLUNGER_BASE_Y + 0.04, CELL_Z),
            radius=0.040,
            length=0.028,
            mat="R1_Copper",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        p_mb.build_object(parent=stage_4)

    # 3. RotaryDial (rotates +90 deg around X at DIAL_PIVOT_U)
    dx, dy, dz = DIAL_PIVOT_U
    d_mb = r1kit.MeshBuilder("RotaryDial", pivot_u=DIAL_PIVOT_U)
    d_mb.add_cylinder(
        center_u=(dx - 0.025, dy, dz),
        radius=0.068,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    d_mb.add_bevel_box(
        center_u=(dx - 0.042, dy + 0.010, dz),
        size_u=(0.026, 0.135, 0.036),
        mat="R1_CreamEnamel",
        bevel=0.005,
        segments=1,
    )
    d_mb.add_bevel_box(
        center_u=(dx - 0.046, dy + 0.055, dz),
        size_u=(0.028, 0.032, 0.030),
        mat="R1_OrangeStripe",
        bevel=0.0,
        segments=0,
    )
    d_mb.build_object(parent=stage_4)

    # 4. SightGlass_1..4 (continuous glowing vertical pill bar R1_LampAmber on each cell)
    for idx, cx in enumerate(CELL_XS, start=1):
        sg_pivot = (cx, SIGHT_GLASS_Y, SIGHT_GLASS_Z)
        sg_mb = r1kit.MeshBuilder(f"SightGlass_{idx}", pivot_u=sg_pivot)
        sg_mb.add_bevel_box(
            center_u=(cx, SIGHT_GLASS_Y, SIGHT_GLASS_Z),
            size_u=(0.048, 0.33, 0.022),
            mat="R1_LampAmber",
            bevel=0.008,
            segments=1,
        )
        sg_mb.build_object(parent=stage_4)

    # 5. RoofBeacon (emissive R1_LampAmber dome lamp on cabinet roof reaching Y = 1.60 m)
    bx, by, bz = BEACON_PIVOT_U
    rb_mb = r1kit.MeshBuilder("RoofBeacon", pivot_u=BEACON_PIVOT_U)
    rb_mb.add_cylinder(
        center_u=(bx, by + 0.075, bz),
        radius=0.105,
        radius_top=0.068,
        length=0.125,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.022,
        bevel_segments=2,
    )
    rb_mb.build_object(parent=stage_4)

    return stage_4


def pose_battery_bank_rest(root: bpy.types.Object) -> None:
    """Set BatteryBank moving parts to their rest pose (bar up/locked, plungers seated, dial at 0 deg)."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "SafetyBar":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name.startswith("Plunger_"):
            idx = int(obj.name.split("_")[1]) - 1
            obj.location = r1kit.u_pos(CELL_XS[idx], PLUNGER_BASE_Y, CELL_Z)
        elif obj.name == "RotaryDial":
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_battery_bank_active(root: bpy.types.Object) -> None:
    """Set BatteryBank moving parts to their active pose (bar swung -90 deg forward/down, plungers +0.06m up, dial +90 deg)."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "SafetyBar":
            # In Blender (Y=fwd, Z=up), -82 deg around X swings the upright U-bar forward (+Y) and down (-Z)
            obj.rotation_euler = (math.radians(-82.0), 0.0, 0.0)
        elif obj.name.startswith("Plunger_"):
            idx = int(obj.name.split("_")[1]) - 1
            obj.location = r1kit.u_pos(CELL_XS[idx], PLUNGER_BASE_Y + PLUNGER_RISE_Y, CELL_Z)
        elif obj.name == "RotaryDial":
            obj.rotation_euler = (math.radians(90.0), 0.0, 0.0)


def build_battery_bank() -> bpy.types.Object:
    """Build the complete BatteryBank hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("BatteryBank", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_battery_bank_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.82)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build BatteryBank FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/BatteryBank.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_battery_bank()
    tris = r1kit.count_triangles(root)
    print(f"[BatteryBank] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        concept_img = Path("D:/Project/MoonProject-wt/agy-concept/Logs/base/s_battery_bank.png")
        r1kit.render_structure_suite(
            root=root,
            slug="battery_bank",
            out_dir=args.renders_dir,
            target_height=1.60,
            footprint_radius=0.82,
            pose_rest_fn=pose_battery_bank_rest,
            pose_active_fn=pose_battery_bank_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
            cam_side_x=-1.0,
            concept_path=concept_img,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
