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


def _build_continuous_arch_hood(mb: r1kit.MeshBuilder) -> None:
    """Build ONE continuous rounded cream-enamel arch from left pillar (+X) over to right pillar (-X) with integrated orange bands."""
    hz = 0.50
    b_z = 0.026
    b_s = 0.090

    # 8-point bevelled cross-section loop in (s, z_offset) around [0, 1] x [-hz, +hz]
    cs_template: list[tuple[float, float]] = [
        (0.0, -hz + b_z),      # 0: inner wall back
        (0.0, hz - b_z),       # 1: inner wall front
        (b_s, hz),             # 2: front-inner bevel end
        (1.0 - b_s, hz),       # 3: front-outer bevel start
        (1.0, hz - b_z),       # 4: outer wall front
        (1.0, -hz + b_z),      # 5: outer wall back
        (1.0 - b_s, -hz),      # 6: back-outer bevel start
        (b_s, -hz),            # 7: back-inner bevel end
    ]
    # Faces 2..6 wrap across front face -> front-outer bevel -> outer wall -> back-outer bevel -> back face
    stripe_face_indices = {2, 3, 4, 5, 6}

    # Build station list (x_in, y_in, x_out, y_out, groove_inset, is_stripe_after) from right (-X) to left (+X)
    # Base at Y = 1.22, short vertical skirt to Y = 1.30, sweeping 0.54m outer radius corner (center_x = ±0.53, center_y = 1.30,
    # R_out = 0.54 -> top Y = 1.84; R_in_x = 0.24, R_in_y = 0.32 -> inner ceiling Y = 1.62)
    corner_angles_deg = [
        0.0,
        8.0,
        15.0,   # Orange stripe start
        24.0,
        33.0,   # Orange stripe end
        46.0,
        60.0,
        75.0,
        90.0,
    ]

    stations: list[tuple[float, float, float, float, float, bool]] = []

    # 1. Right (-X) vertical base skirt above pillar seam (Y = 1.22 -> 1.30)
    stations.append((-0.77, 1.22, -1.07, 1.22, 0.0, False))

    # 2. Right (-X) rounded arch shoulder (theta = 0 -> 90 deg)
    for idx, deg in enumerate(corner_angles_deg):
        rad = math.radians(deg)
        c_t = math.cos(rad)
        s_t = math.sin(rad)
        xi = -0.53 - 0.24 * c_t
        yi = 1.30 + 0.32 * s_t
        xo = -0.53 - 0.54 * c_t
        yo = 1.30 + 0.54 * s_t
        is_stripe = 15.0 <= deg < 33.0 and idx < len(corner_angles_deg) - 1
        stations.append((xi, yi, xo, yo, 0.0, is_stripe))

    # 3. Central horizontal span across the top (X = -0.53 .. +0.53, with subtle panel seams at X = ±0.44)
    top_x_stops = [
        (-0.445, 0.0),
        (-0.440, 0.0035),  # Subtle left-of-center panel seam groove
        (-0.435, 0.0),
        (0.0, 0.0),
        (0.435, 0.0),
        (0.440, 0.0035),   # Subtle right-of-center panel seam groove
        (0.445, 0.0),
    ]
    for tx, g_in in top_x_stops:
        stations.append((tx, 1.62, tx, 1.84, g_in, False))

    # 4. Left (+X) rounded arch shoulder (theta = 90 -> 0 deg)
    rev_angles = list(reversed(corner_angles_deg))
    for idx, deg in enumerate(rev_angles):
        rad = math.radians(deg)
        c_t = math.cos(rad)
        s_t = math.sin(rad)
        xi = 0.53 + 0.24 * c_t
        yi = 1.30 + 0.32 * s_t
        xo = 0.53 + 0.54 * c_t
        yo = 1.30 + 0.54 * s_t
        next_deg = rev_angles[idx + 1] if idx + 1 < len(rev_angles) else -1.0
        is_stripe = 15.0 <= next_deg < 33.0 and 15.0 < deg <= 33.0
        stations.append((xi, yi, xo, yo, 0.0, is_stripe))

    # 5. Left (+X) vertical base skirt down to pillar seam (Y = 1.30 -> 1.22)
    stations.append((0.77, 1.22, 1.07, 1.22, 0.0, False))

    rings: list[list] = []
    for xi, yi, xo, yo, g_in, _ in stations:
        dx = xo - xi
        dy = yo - yi
        ring_verts = []
        for s_val, z_val in cs_template:
            # Apply subtle panel seam inset on outer/front/back surfaces when g_in > 0
            s_eff = s_val - g_in * s_val * 2.5
            z_eff = z_val * (1.0 - g_in * 1.4)
            px = xi + dx * s_eff
            py = yi + dy * s_eff
            ring_verts.append(mb.bm.verts.new(mb._to_local_b((px, py, z_eff))))
        rings.append(ring_verts)

    m_cs = len(cs_template)
    cream_faces = []
    orange_faces = []
    for k in range(len(rings) - 1):
        r0 = rings[k]
        r1 = rings[k + 1]
        is_stripe_seg = stations[k][5]
        for m in range(m_cs):
            mn = (m + 1) % m_cs
            f = mb.bm.faces.new([r0[m], r0[mn], r1[mn], r1[m]])
            if is_stripe_seg and m in stripe_face_indices:
                orange_faces.append(f)
            else:
                cream_faces.append(f)

    # Cap both bottom ends (k = 0 and k = -1) with all-quad fan strips (8-gon -> 3 quads)
    r_start = rings[0]
    for q in range(3):
        v0 = r_start[q]
        v1 = r_start[q + 1]
        v2 = r_start[m_cs - 2 - q]
        v3 = r_start[m_cs - 1 - q]
        cream_faces.append(mb.bm.faces.new([v0, v3, v2, v1]))

    r_end = rings[-1]
    for q in range(3):
        v0 = r_end[q]
        v1 = r_end[q + 1]
        v2 = r_end[m_cs - 2 - q]
        v3 = r_end[m_cs - 1 - q]
        cream_faces.append(mb.bm.faces.new([v0, v1, v2, v3]))

    mb._assign_faces(cream_faces, "R1_CreamEnamel")
    mb._assign_faces(orange_faces, "R1_OrangeStripe")


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Wide open-arch gantry frame: sage-green side pillars, ONE continuous rounded cream-enamel arch with orange bands, interior feedstock bin."""
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    # --- 1. Left (+X) and Right (-X) muted sage-green lower side pillars (`X = ±0.92, Y = 0.05 .. 1.22, Z = -0.50 .. +0.50`) ---
    for sx in (-1.0, 1.0):
        px = sx * 0.92
        mb.add_bevel_box(
            center_u=(px, 0.635, 0.0),
            size_u=(0.30, 1.17, 1.00),
            mat="R1_SagePatch",
            bevel=0.026,
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
        for hz in (-0.26, 0.26):
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

    # --- 2. ONE continuous rounded warm cream-enamel gantry arch (`Y = 1.22 .. 1.84m`) with integrated orange bands ---
    _build_continuous_arch_hood(mb)

    # Front and rear bolt fasteners on the cream hood corners & sage pillar tops
    for sz in (-0.504, 0.504):
        mb.add_bolts(
            [
                (-0.98, 1.15, sz),
                (-0.84, 1.15, sz),
                (-0.98, 1.29, sz),
                (-0.84, 1.29, sz),
                (0.84, 1.15, sz),
                (0.98, 1.15, sz),
                (0.84, 1.29, sz),
                (0.98, 1.29, sz),
            ],
            radius=0.011,
            height=0.009,
            axis_u="Z",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # --- 3. Interior left-wall (+X) sage-green feedstock bin with ingots & glass beakers (`X = +0.58, Y = 0.62, Z = -0.04`) ---
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
    # Brushed-steel ingots + dark glass cylinder beakers protruding from the feedstock bin
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

    # --- Fixed horizontal overhead brass gantry rail spanning the inner bay (`X = -0.78 .. +0.78, Y = 1.38, Z = 0.02`) ---
    mb.add_bevel_box(
        center_u=(0.0, 1.38, 0.02),
        size_u=(1.56, 0.075, 0.09),
        mat="R1_Brass",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 1.38, 0.068),
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

    # --- Warm amber overhead work-bay strip lights under the arch ceiling (`Y = 1.56 .. 1.615, Z = +0.26`) ---
    # Ceiling mounting spine and brass end/center clasps under the arch soffit
    mb.add_bevel_box(
        center_u=(0.0, 1.608, 0.26),
        size_u=(0.98, 0.020, 0.072),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    for bx in (-0.47, 0.0, 0.47):
        mb.add_bevel_box(
            center_u=(bx, 1.592, 0.26),
            size_u=(0.036, 0.042, 0.082),
            mat="R1_Brass",
            bevel=0.005,
            segments=1,
        )
    # Twin prominent glowing warm-amber tubular strip lamps (`R1_LampAmber`) lighting the work table
    for lx in (-0.235, 0.235):
        mb.add_bevel_box(
            center_u=(lx, 1.584, 0.26),
            size_u=(0.43, 0.034, 0.064),
            mat="R1_LampAmber",
            bevel=0.008,
            segments=1,
        )
        mb.add_cylinder(
            center_u=(lx, 1.568, 0.265),
            radius=0.022,
            length=0.42,
            mat="R1_LampAmber",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
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
        segments=24,
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
        segments=24,
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
            center_u=(cx, 1.38, 0.04),
            size_u=(0.16, 0.15, 0.12),
            mat="R1_Brass",
            bevel=0.010,
            segments=1,
        )
        gb_mb.add_bolts(
            [
                (cx - 0.05, 1.42, 0.105),
                (cx + 0.05, 1.42, 0.105),
                (cx - 0.05, 1.34, 0.105),
                (cx + 0.05, 1.34, 0.105),
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
        start_deg = seg_i * 60.0 + 7.0
        pr_mb.add_torus(
            center_u=(-0.92, 0.80, 0.528),
            major_radius=0.072,
            minor_radius=0.014,
            mat="R1_LampAmber",
            axis_u="Z",
            major_segments=4,
            minor_segments=6,
            arc_deg=46.0,
            start_deg=start_deg,
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


def _render_fabricator_concept_compare(
    root: bpy.types.Object,
    out_dir: Path,
    concept_path: Path,
) -> None:
    """Render Fabricator from the concept camera angle at night with a warm key light and warm strip-lamp work lighting."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    pose_fabricator_active(root)
    bpy.context.view_layer.update()

    temp_objs: list[bpy.types.Object] = []
    temp_mats: list[bpy.types.Material] = []

    # 1. Night sky world background (deep lunar night sky, warm-neutral bounce so cream never looks blue-grey)
    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("R1_ConceptNightWorld")
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
    ground_mb = r1kit.MeshBuilder("_ConceptLunarGround", pivot_u=(0.0, -0.08, 0.0))
    ground_mb.add_cylinder(
        center_u=(0.0, -0.08, -1.0),
        radius=12.0,
        length=0.16,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=64,
        bevel=0.02,
        bevel_segments=1,
    )
    ground_obj = ground_mb.build_object()
    ground_mat = bpy.data.materials.new("_ConceptGroundMat")
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
    r1kit.ensure_materials(use_vertex_color_shading=True)

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

    # 3. Warm night key light from front-left-top + warm-cream fill on right pillar (never cold blue!)
    _add_sun("_ConceptWarmKey", (3.6, 3.8, 4.4), (0.0, 0.8, 0.0), "#FFEAD0", 3.6, 5.0)
    _add_sun("_ConceptWarmFill", (-4.2, 2.4, 3.6), (0.0, 0.8, 0.0), "#F6E4C8", 1.35, 22.0)
    _add_sun("_ConceptWarmRim", (-1.8, 4.0, -3.8), (0.0, 1.0, 0.0), "#FFE2B8", 1.0, 10.0)

    # 4. Warm amber practical strip lights under the arch illuminating the work table and inner arch walls
    for lx in (-0.24, 0.24):
        _add_point(f"_StripGlow_{lx:+.2f}", (lx, 1.48, 0.24), "#FFA830", 42.0, 0.16)
        _add_point(f"_TablePool_{lx:+.2f}", (lx * 0.7, 0.86, 0.14), "#FFB240", 24.0, 0.22)
    _add_point("_RingGlow", (-0.92, 0.80, 0.62), "#FFB038", 8.0, 0.10)

    # 5. Camera matched to s_fabricator.png angle & full-structure framing
    cam_data = bpy.data.cameras.new("_ConceptCam")
    cam_obj = bpy.data.objects.new("_ConceptCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    temp_objs.append(cam_obj)

    r1kit._aim_camera_at(
        cam_obj,
        cam_pos_u=(-2.72, 1.52, 4.40),
        target_u=(0.08, 0.86, 0.08),
        lens_mm=38.0,
    )

    concept_render_path = out_dir / "_fabricator_concept_view.png"
    r1kit._render_to_file(concept_render_path, res_x=1376, res_y=768, freestyle=True)

    if Path(concept_path).exists():
        r1kit.compose_side_by_side(
            left_path=Path(concept_path),
            right_path=concept_render_path,
            out_path=out_dir / "compare_fabricator.png",
            target_height=768,
        )
    concept_render_path.unlink(missing_ok=True)

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    for tmat in temp_mats:
        if tmat.name in bpy.data.materials:
            bpy.data.materials.remove(tmat)

    pose_fabricator_rest(root)
    bpy.context.view_layer.update()
    r1kit.ensure_materials(use_vertex_color_shading=False)


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
        _render_fabricator_concept_compare(
            root=root,
            out_dir=args.renders_dir,
            concept_path=concept_img,
        )
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
            concept_path=None,
            use_active_for_compare=True,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
