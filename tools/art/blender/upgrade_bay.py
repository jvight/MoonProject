"""Build script for the R1 UpgradeBay structure (`Assets/_Project/Art/R1/Models/Structures/UpgradeBay.fbx`)."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Sequence

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

BLENDER_DIR = Path(__file__).resolve().parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402

CONCEPT_IMAGE_PATH = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\s_upgrade_bay.png")
ROVER_FBX_PATH = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")

# Key dimensions in r1kit Unity-space metres:
#   +X_u is viewer-LEFT when looking at the front (+Z_u); -X_u is viewer-RIGHT; +Y_u is up; +Z_u is front.
# Sized so 07 v3 (RoverModel.fbx: 1.48 m W x 1.59 m L x 1.76 m H) fits inside with concept proportions.
UPGRADE_BAY_DIMS = {
    "footprint_width": 3.20,
    "portal_height": 2.82,
    "col_center_x": 1.26,
    "col_width_x": 0.40,
    "col_depth_z": 0.62,
    "plinth_width_x": 0.68,
    "plinth_depth_z": 1.44,
    "plinth_height_y": 0.11,
    "collar_width_x": 0.50,
    "collar_depth_z": 0.78,
    "collar_height_y": 0.10,
    "rail_rest_x": 0.61,
    "rail_length_z": 2.32,
    "bridge_bot_y": 2.42,
    "bridge_top_y": 2.68,
    "bridge_half_span_x": 0.86,
    "bridge_half_depth_z": 0.21,
    "hoist_pivot_y": 2.44,
    "hoist_claw_offset_x": 0.08,
    "hoist_claw_offset_y": -0.46,
    "arm_l_pivot_x": 1.06,
    "arm_l_pivot_y": 1.18,
    "arm_l_pivot_z": 0.06,
    "arm_r_pivot_x": -1.06,
    "arm_r_pivot_y": 1.18,
    "arm_r_pivot_z": -0.04,
    "flood_x": 0.68,
    "flood_y": 2.31,
    "flood_z": 0.04,
    "status_bezel_y": 1.56,
    "status_lamp_1_y": 1.41,
    "status_lamp_step_y": 0.15,
    "stripe_bot_y": 1.30,
    "stripe_top_y": 1.52,
    "patch_center_y": 0.76,
    "patch_width": 0.26,
    "patch_height": 0.36,
    "patch_thick": 0.024,
    "socket_power_x": -1.26,
    "socket_power_y": 0.08,
    "socket_power_z": 0.74,
}


def _rounded_rect_ring_2d(
    width_u: float,
    depth_v: float,
    corner_r: float,
    corner_segments: int = 2,
) -> list[tuple[float, float]]:
    """
    Generate a 2D closed rounded-rectangle loop in (u, v) coordinates centered at (0, 0).
    Returns `4 * (corner_segments + 1)` points in counter-clockwise order.
    """
    hu = width_u * 0.5
    hv = depth_v * 0.5
    rc = max(0.004, min(corner_r, hu * 0.85, hv * 0.85))
    pts: list[tuple[float, float]] = []
    corners = (
        (hu - rc, -hv + rc, -math.pi * 0.5),
        (hu - rc, hv - rc, 0.0),
        (-hu + rc, hv - rc, math.pi * 0.5),
        (-hu + rc, -hv + rc, math.pi),
    )
    for cu, cv, a_start in corners:
        for i in range(corner_segments + 1):
            ang = a_start + (math.pi * 0.5) * (i / corner_segments)
            pts.append((cu + rc * math.cos(ang), cv + rc * math.sin(ang)))
    return pts


def _add_swept_rounded_rect(
    mb: r1kit.MeshBuilder,
    side: float,
    stations: Sequence[tuple[float, float, float, float, float, float, float]],
    mat: str,
    corner_segments: int = 2,
) -> list[bmesh.types.BMFace]:
    """
    Sweep a rounded-rectangle cross-section along a 2D curve in the X-Y plane.
    Each station is (cx_pos, cy, cz, width_u, depth_z, corner_r, theta_deg) where cx_pos > 0:
      - theta_deg = 0: vertical shaft (+Y tangent, u > 0 is outer +side*X, u < 0 is inner -side*X)
      - theta_deg = 90: horizontal top elbow (-side*X tangent, u > 0 is top +Y, u < 0 is bottom -Y)
    Closes both ends with all-quad bridge caps (zero n-gons, zero boundary edges).
    """
    rings: list[list[bmesh.types.BMVert]] = []
    for cx, cy, cz, wu, dz, cr, theta_deg in stations:
        th = math.radians(theta_deg)
        cos_t = math.cos(th)
        sin_t = math.sin(th)
        prof = _rounded_rect_ring_2d(wu, dz, cr, corner_segments=corner_segments)
        ring: list[bmesh.types.BMVert] = []
        for u_pt, v_pt in prof:
            xu = side * (cx + u_pt * cos_t)
            yu = cy + (u_pt * sin_t)
            zu = cz + v_pt
            ring.append(mb.bm.verts.new(mb._to_local_b((xu, yu, zu))))
        rings.append(ring)

    m_pts = len(rings[0])
    faces: list[bmesh.types.BMFace] = []
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for i in range(m_pts):
            j = (i + 1) % m_pts
            if side > 0.0:
                faces.append(mb.bm.faces.new([r0[i], r0[j], r1[j], r1[i]]))
            else:
                faces.append(mb.bm.faces.new([r0[j], r0[i], r1[i], r1[j]]))

    # All-quad bridge caps on start and end rings
    half_m = m_pts // 2
    r_start = rings[0]
    r_end = rings[-1]
    for k in range(half_m - 1):
        v_s = [r_start[k + 1], r_start[k], r_start[m_pts - 1 - k], r_start[m_pts - 2 - k]]
        v_e = [r_end[k], r_end[k + 1], r_end[m_pts - 2 - k], r_end[m_pts - 1 - k]]
        if side < 0.0:
            v_s.reverse()
            v_e.reverse()
        faces.append(mb.bm.faces.new(v_s))
        faces.append(mb.bm.faces.new(v_e))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Heavy bolted column base plinths, stepped ankle collars, front hydraulic feed pipes, coiling ground cable, and Socket_Power_1."""
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
            bevel=0.018,
            segments=1,
            top_scale_xz=(0.96, 0.98),
        )
        # Stepped dark-charcoal column ankle collar (y = 0.11 .. 0.21 m)
        mb.add_bevel_box(
            center_u=(sx, plinth_h + collar_h * 0.5, 0.0),
            size_u=(collar_w, collar_h, collar_d),
            mat="R1_GreyMetal",
            bevel=0.016,
            segments=1,
            top_scale_xz=(0.94, 0.95),
        )
        # Perimeter hex anchor bolts on each base plinth
        bolts: list[tuple[float, float, float]] = []
        for bx_off in (-plinth_w * 0.36, plinth_w * 0.36):
            for bz_off in (-0.58, -0.42, 0.42, 0.58):
                bolts.append((sx + bx_off, plinth_h + 0.006, bz_off))
        mb.add_bolts(
            bolts,
            radius=0.016,
            height=0.012,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

        # Curved front steel hydraulic feed pipe from plinth toe into lower column front face (matching s_upgrade_bay.png)
        pipe_pts = [
            (sx, plinth_h * 0.85, 0.52),
            (sx, plinth_h + 0.10, 0.51),
            (sx, plinth_h + 0.24, 0.46),
            (sx, plinth_h + 0.35, 0.38),
            (sx, plinth_h + 0.39, 0.29),
        ]
        mb.add_tube_path(pipe_pts, radius=0.030, mat="R1_BareSteel", segments=8, cap_ends=True)
        # Flanged pipe collars at base plinth and lower column entry
        mb.add_cylinder(
            center_u=(sx, plinth_h + 0.010, 0.52),
            radius=0.050,
            length=0.024,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sx, plinth_h + 0.39, 0.33),
            radius=0.050,
            length=0.026,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )

        # Lateral slide-way sleeper blocks supporting the wheel guide rails (between plinth at 0.92 and rail outer edge at 0.81)
        for sz_way in (-0.58, 0.50):
            way_x = side * 0.855
            mb.add_bevel_box(
                center_u=(way_x, 0.022, sz_way),
                size_u=(0.14, 0.044, 0.095),
                mat="R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )

    # Heavy power cable gland receptacle on the front of the right (-X) plinth toe + coiling ground cable (matching s_upgrade_bay.png)
    px = d["socket_power_x"]
    py = d["socket_power_y"]
    pz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(px, py, pz - 0.022),
        radius=0.052,
        length=0.058,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=8,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(px, py, pz + 0.006),
        radius=0.034,
        length=0.032,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        bevel_segments=1,
    )

    # Braided dark rubber cable coiling away across the ground from the right pillar base to the outer edge of the right guide rail (-0.81 m)
    coil_pts = [
        (px, py, pz + 0.015),
        (px + 0.04, 0.040, pz + 0.14),
        (-1.14, 0.024, 1.00),
        (-1.02, 0.024, 1.12),
        (-0.90, 0.024, 1.14),
        (-0.82, 0.024, 1.06),
        (-0.79, 0.024, 0.92),
        (-0.80, 0.024, 0.78),
        (-0.82, 0.026, 0.66),
        (-0.83, 0.036, 0.56),
    ]
    mb.add_tube_path(coil_pts, radius=0.024, mat="R1_Rubber", segments=8, cap_ends=True)
    # Cable junction block where the coiled cable feeds into the right guide rail outer curb
    mb.add_bevel_box(
        center_u=(-0.83, 0.038, 0.55),
        size_u=(0.068, 0.052, 0.088),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
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
    """Stage 2: Inner column service slots, external conduit pipes, overhead Warren truss beam, carriage, and hanging dome lamp shades."""
    d = UPGRADE_BAY_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    col_x = d["col_center_x"]
    col_w = d["col_width_x"]
    col_d = d["col_depth_z"]
    inner_x = col_x - col_w * 0.5  # 1.06 m

    # 1. Recessed vertical arm-slot channels on the inner faces (x = +-1.06 m, y = 0.26 .. 1.88 m)
    slot_y_bot = 0.26
    slot_y_top = 1.88
    slot_cy = (slot_y_bot + slot_y_top) * 0.5
    slot_h = slot_y_top - slot_y_bot
    for side in (-1.0, 1.0):
        sx_inner = side * inner_x
        # Bevelled dark-charcoal slot surround frame protruding slightly from inner column face
        mb.add_bevel_box(
            center_u=(sx_inner + side * 0.012, slot_cy, 0.03),
            size_u=(0.058, slot_h, 0.25),
            mat="R1_GreyMetal",
            bevel=0.009,
            segments=1,
        )
        # Dark recessed shadow channel
        mb.add_bevel_box(
            center_u=(sx_inner + side * 0.022, slot_cy, 0.03),
            size_u=(0.050, slot_h - 0.08, 0.16),
            mat="R1_DarkGlass",
            bevel=0.0,
            segments=0,
        )
        # Vertical brass lead-screw inside the slot
        mb.add_cylinder(
            center_u=(sx_inner + side * 0.008, slot_cy, 0.03),
            radius=0.018,
            length=slot_h - 0.10,
            mat="R1_Copper",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )

    # 2. External vertical conduit manifold on the outer viewer-right (-X) column face (matching s_upgrade_bay.png)
    outer_rx = -(col_x + col_w * 0.5)  # -1.46 m
    for z_pipe, mat_pipe, rad_p in (
        (-0.02, "R1_GreyMetal", 0.028),
        (0.12, "R1_GreyMetal", 0.028),
        (0.23, "R1_Copper", 0.022),
    ):
        pipe_path = [
            (outer_rx + 0.05, 0.18, z_pipe),
            (outer_rx - 0.046, 0.28, z_pipe),
            (outer_rx - 0.046, 1.72, z_pipe),
            (outer_rx + 0.05, 1.82, z_pipe),
        ]
        mb.add_tube_path(pipe_path, radius=rad_p, mat=mat_pipe, segments=6, cap_ends=True)
    # 4 horizontal pipe-clamp brackets along the outer right column
    for clamp_y in (0.48, 0.88, 1.28, 1.64):
        mb.add_bevel_box(
            center_u=(outer_rx - 0.028, clamp_y, 0.10),
            size_u=(0.058, 0.042, 0.36),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )

    # 3. Overhead horizontal Warren truss bridge (x = -0.86 .. +0.86 m, y = 2.24 .. 2.50 m) joining the pillar elbows
    bx = d["bridge_half_span_x"]
    by0 = d["bridge_bot_y"]
    by1 = d["bridge_top_y"]
    bz = d["bridge_half_depth_z"]

    # Bolted dark-charcoal end-flange collars where the truss bridge emerges from the curved pillar elbows
    for side in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(side * (bx - 0.01), (by0 + by1) * 0.5, 0.0),
            size_u=(0.056, (by1 - by0) + 0.12, bz * 2.0 + 0.10),
            mat="R1_GreyMetal",
            bevel=0.010,
            segments=1,
        )
        end_bolts: list[tuple[float, float, float]] = []
        for by_b in (by0 - 0.01, (by0 + by1) * 0.5, by1 + 0.01):
            for bz_b in (-bz, bz):
                end_bolts.append((side * (bx - 0.040), by_b, bz_b))
        mb.add_bolts(end_bolts, radius=0.012, height=0.010, axis_u="X", mat="R1_BareSteel", bevel_segments=0)

    # 4 longitudinal main chords of the bridge truss
    for cy_chord in (by0, by1):
        for cz_chord in (-bz, bz):
            mb.add_strut(
                p0_u=(-bx, cy_chord, cz_chord),
                p1_u=(bx, cy_chord, cz_chord),
                width=0.046,
                thickness=0.046,
                mat="R1_BareSteel",
                bevel=0.004,
            )

    # 5 bays of crisp Warren V-diagonals on front (+Z) and back (-Z) faces + transverse ties
    n_bays = 5
    span_x = bx * 2.0
    bay_w = span_x / n_bays
    for i in range(n_bays + 1):
        x_curr = -bx + i * bay_w
        mb.add_strut((x_curr, by1, -bz), (x_curr, by1, bz), width=0.030, thickness=0.030, mat="R1_GreyMetal", bevel=0.0)
        mb.add_strut((x_curr, by0, -bz), (x_curr, by0, bz), width=0.030, thickness=0.030, mat="R1_GreyMetal", bevel=0.0)
        if i < n_bays:
            x_next = -bx + (i + 1) * bay_w
            x_mid = 0.5 * (x_curr + x_next)
            for cz_face in (-bz, bz):
                mb.add_strut(
                    (x_curr, by0, cz_face),
                    (x_mid, by1, cz_face),
                    width=0.030,
                    thickness=0.030,
                    mat="R1_BareSteel",
                    bevel=0.0,
                )
                mb.add_strut(
                    (x_mid, by1, cz_face),
                    (x_next, by0, cz_face),
                    width=0.030,
                    thickness=0.030,
                    mat="R1_BareSteel",
                    bevel=0.0,
                )

    # 4. Two hanging dome work lamp shades under the left and right ends of the truss beam (matching s_upgrade_bay.png)
    fx = d["flood_x"]
    fy = d["flood_y"]
    fz = d["flood_z"]
    for side in (-1.0, 1.0):
        tilt_z = side * 22.0
        # Mounting bracket collar on bottom of truss beam
        mb.add_bevel_box(
            center_u=(side * fx, by0 - 0.028, fz),
            size_u=(0.10, 0.054, 0.10),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )
        # Upper brass neck ring of the dome lamp
        mb.add_cylinder(
            center_u=(side * fx - side * 0.012, fy + 0.054, fz),
            radius=0.062,
            radius_top=0.046,
            length=0.042,
            mat="R1_Copper",
            axis_u="Y",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
            rot_deg_u=(0.0, 0.0, tilt_z),
        )
        # Deep flared brass dome shade (R1_Copper)
        mb.add_cylinder(
            center_u=(side * fx - side * 0.026, fy + 0.010, fz),
            radius=0.128,
            radius_top=0.062,
            length=0.080,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
            bevel=0.006,
            bevel_segments=1,
            rot_deg_u=(0.0, 0.0, tilt_z),
        )

    # 5. Central overhead hoist carriage housing on the truss beam (matching s_upgrade_bay.png)
    hy = d["hoist_pivot_y"]
    mb.add_bevel_box(
        center_u=(0.0, hy + 0.06, 0.0),
        size_u=(0.30, 0.19, 0.42),
        mat="R1_GreyMetal",
        bevel=0.014,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, hy - 0.01, 0.0),
        size_u=(0.25, 0.065, 0.36),
        mat="R1_Copper",
        bevel=0.010,
        segments=1,
    )

    # 6. Vertical 3-lamp status column bezel housing on the front (+Z) face of the viewer-right (-X) pillar
    # Placed in Stage_2 (which already uses R1_Copper and R1_DarkGlass) to minimize draw calls
    rx = -col_x
    front_z = col_d * 0.5
    by_bezel = d["status_bezel_y"]
    mb.add_bevel_box(
        center_u=(rx, by_bezel, front_z + 0.014),
        size_u=(0.17, 0.48, 0.034),
        mat="R1_Copper",
        bevel=0.012,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(rx, by_bezel, front_z + 0.020),
        size_u=(0.132, 0.43, 0.028),
        mat="R1_DarkGlass",
        bevel=0.004,
        segments=1,
    )
    for idx in range(3):
        ly = d["status_lamp_1_y"] + idx * d["status_lamp_step_y"]
        mb.add_cylinder(
            center_u=(rx, ly, front_z + 0.028),
            radius=0.050,
            length=0.014,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.003,
            bevel_segments=1,
        )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Smooth swept rounded-rectangle cream pillars with curved top elbows, orange stripe, and green hatches."""
    d = UPGRADE_BAY_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    col_x = d["col_center_x"]
    col_w = d["col_width_x"]
    col_d = d["col_depth_z"]

    # Build each pillar as ONE continuous swept rounded-rectangle column that bends in a big elbow at the top into the truss beam
    elbow_piv_x = 0.90
    elbow_piv_y = 2.29
    elbow_r = 0.26
    for side in (-1.0, 1.0):
        stations: list[tuple[float, float, float, float, float, float, float]] = [
            # Base bevel & straight vertical column shaft (y = 0.21 .. 1.88 m)
            (col_x, 0.210, 0.0, col_w - 0.028, col_d - 0.028, 0.066, 0.0),
            (col_x, 0.228, 0.0, col_w, col_d, 0.076, 0.0),
            (col_x, 1.880, 0.0, col_w, col_d, 0.076, 0.0),
            # Subtle horizontal panel seam groove at y = 1.89 m
            (col_x, 1.888, 0.0, col_w - 0.012, col_d - 0.012, 0.070, 2.0),
            (col_x - 0.002, 1.896, 0.0, col_w, col_d, 0.076, 5.0),
            # Gentle inward shoulder curve (y = 1.90 .. 2.28 m, x = 1.26 -> 1.16 m)
            (1.225, 2.030, 0.0, col_w - 0.004, col_d - 0.010, 0.074, 12.0),
            (1.182, 2.170, 0.0, col_w - 0.008, col_d - 0.020, 0.072, 8.0),
            (1.162, 2.276, 0.0, col_w - 0.010, col_d - 0.028, 0.072, 2.0),
            # Subtle upper horizontal panel seam groove at y = 2.29 m
            (1.160, 2.285, 0.0, col_w - 0.022, col_d - 0.040, 0.068, 3.0),
            (1.160, 2.294, 0.0, col_w - 0.010, col_d - 0.028, 0.072, 5.0),
        ]
        # Big smooth 90-degree rounded top elbow curving into the horizontal truss bridge
        for deg in (22.0, 45.0, 68.0, 90.0):
            rad = math.radians(deg)
            cx_e = elbow_piv_x + elbow_r * math.cos(rad)
            cy_e = elbow_piv_y + elbow_r * math.sin(rad)
            stations.append((cx_e, cy_e, 0.0, col_w - 0.010, col_d - 0.032, 0.072, deg))
        # Short horizontal stub meeting the truss end-flange at x = 0.86 m
        stations.append((0.865, elbow_piv_y + elbow_r, 0.0, col_w - 0.012, col_d - 0.034, 0.072, 90.0))
        stations.append((0.848, elbow_piv_y + elbow_r, 0.0, col_w - 0.036, col_d - 0.056, 0.062, 90.0))

        _add_swept_rounded_rect(mb, side=side, stations=stations, mat="R1_CreamEnamel", corner_segments=2)

        # Swept rounded-rectangle warm-orange stripe band (R1_OrangeStripe) hugging the column
        sy0 = d["stripe_bot_y"]
        sy1 = d["stripe_top_y"]
        stripe_stations = [
            (col_x, sy0, 0.0, col_w + 0.002, col_d + 0.002, 0.076, 0.0),
            (col_x, sy0 + 0.008, 0.0, col_w + 0.010, col_d + 0.010, 0.078, 0.0),
            (col_x, sy1 - 0.008, 0.0, col_w + 0.010, col_d + 0.010, 0.078, 0.0),
            (col_x, sy1, 0.0, col_w + 0.002, col_d + 0.002, 0.076, 0.0),
        ]
        _add_swept_rounded_rect(mb, side=side, stations=stripe_stations, mat="R1_OrangeStripe", corner_segments=2)

        # Panel rivets above and below the shoulder & elbow seam lines on the front (+Z) face (matching s_upgrade_bay.png)
        sx = side * col_x
        front_z = col_d * 0.5
        seam_rivets: list[tuple[float, float, float]] = []
        for bx_off in (-0.105, 0.105):
            seam_rivets.append((sx + bx_off, 1.855, front_z + 0.002))
            seam_rivets.append((sx - side * 0.006 + bx_off, 1.925, front_z - 0.002))
            seam_rivets.append((side * 1.162 + bx_off, 2.255, front_z - 0.012))
            seam_rivets.append((side * 1.155 + bx_off, 2.325, front_z - 0.014))
        mb.add_bolts(seam_rivets, radius=0.012, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

        # Riveted sage-green salvaged maintenance hatch (R1_SagePatch) on the lower front (+Z) face of both columns
        py = d["patch_center_y"]
        pw = d["patch_width"]
        ph = d["patch_height"]
        pt = d["patch_thick"]
        mb.add_bevel_box(
            center_u=(sx, py, front_z + pt * 0.4),
            size_u=(pw, ph, pt),
            mat="R1_SagePatch",
            bevel=0.010,
            segments=1,
        )
        patch_rivets: list[tuple[float, float, float]] = []
        for rx_off in (-pw * 0.36, pw * 0.36):
            for ry_off in (-ph * 0.38, ph * 0.38):
                patch_rivets.append((sx + rx_off, py + ry_off, front_z + pt + 0.002))
        mb.add_bolts(patch_rivets, radius=0.012, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

    # Riveted sage-green plate on the front (+Z) face of the overhead hoist carriage (matching s_upgrade_bay.png)
    hy = d["hoist_pivot_y"]
    mb.add_bevel_box(
        center_u=(0.0, hy + 0.075, 0.212),
        size_u=(0.25, 0.115, 0.022),
        mat="R1_SagePatch",
        bevel=0.006,
        segments=1,
    )
    mb.add_bolts(
        [(-0.092, hy + 0.075, 0.224), (0.092, hy + 0.075, 0.224)],
        radius=0.011,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    return mb.build_object(parent=root)


def _build_wrench_arm_left(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the viewer-left (+X) articulated brass arm (`WrenchArm_L`) reaching toward 07's head/cheek with a C-wrench claw."""
    d = UPGRADE_BAY_DIMS
    px, py, pz = d["arm_l_pivot_x"], d["arm_l_pivot_y"], d["arm_l_pivot_z"]
    mb = r1kit.MeshBuilder("WrenchArm_L", pivot_u=(px, py, pz))

    # 1. Vertical slot carriage plate & triangular brass shoulder gusset at (1.06, 1.18, 0.06)
    mb.add_bevel_box(
        center_u=(px + 0.01, py - 0.02, pz),
        size_u=(0.055, 0.26, 0.14),
        mat="R1_Copper",
        bevel=0.008,
        segments=1,
    )
    mb.add_strut(
        p0_u=(px, py - 0.10, pz),
        p1_u=(px - 0.10, py + 0.01, pz),
        width=0.058,
        thickness=0.095,
        mat="R1_Copper",
        bevel=0.007,
    )
    sh_u = (px - 0.09, py + 0.01, pz)
    mb.add_cylinder(
        center_u=sh_u,
        radius=0.052,
        length=0.11,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=sh_u,
        radius=0.024,
        length=0.13,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 2. Upper articulated brass boom reaching up-and-inward from shoulder (0.97, 1.19, 0.06) to high elbow (0.78, 1.58, 0.10)
    elbow_u = (0.78, 1.58, pz + 0.04)
    mb.add_strut(
        p0_u=sh_u,
        p1_u=elbow_u,
        width=0.060,
        thickness=0.066,
        mat="R1_Copper",
        bevel=0.008,
    )
    for t in (0.34, 0.68):
        cx_c = sh_u[0] + (elbow_u[0] - sh_u[0]) * t
        cy_c = sh_u[1] + (elbow_u[1] - sh_u[1]) * t
        cz_c = sh_u[2] + (elbow_u[2] - sh_u[2]) * t
        mb.add_bevel_box(
            center_u=(cx_c, cy_c, cz_c),
            size_u=(0.072, 0.072, 0.078),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
            rot_deg_u=(0.0, 0.0, -26.0),
        )

    # 3. High cylindrical brass elbow joint drum at (0.78, 1.58, 0.10)
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.050,
        length=0.105,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.024,
        length=0.125,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 4. Articulated brass forearm angling down-and-inward with rest pose jaw tip at x = +0.60 m (1.20 m clear gap)
    wrist_u = (0.68, 1.18, pz + 0.08)
    mb.add_strut(
        p0_u=elbow_u,
        p1_u=wrist_u,
        width=0.048,
        thickness=0.054,
        mat="R1_Copper",
        bevel=0.007,
    )
    for t in (0.36, 0.72):
        cx_f = elbow_u[0] + (wrist_u[0] - elbow_u[0]) * t
        cy_f = elbow_u[1] + (wrist_u[1] - elbow_u[1]) * t
        cz_f = elbow_u[2] + (wrist_u[2] - elbow_u[2]) * t
        mb.add_bevel_box(
            center_u=(cx_f, cy_f, cz_f),
            size_u=(0.060, 0.060, 0.066),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
            rot_deg_u=(0.0, 0.0, 14.0),
        )

    # 5. Wrist rotary collar + open C-shaped spanner wrench jaws at the tip (innermost tip at X = +0.600 m)
    mb.add_cylinder(
        center_u=wrist_u,
        radius=0.036,
        length=0.072,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.005,
        bevel_segments=1,
    )
    mb.add_strut(
        p0_u=wrist_u,
        p1_u=(0.62, 1.11, wrist_u[2] + 0.02),
        width=0.024,
        thickness=0.034,
        mat="R1_BareSteel",
        bevel=0.003,
    )
    mb.add_strut(
        p0_u=(0.62, 1.11, wrist_u[2] + 0.02),
        p1_u=(0.61, 1.04, wrist_u[2] + 0.02),
        width=0.020,
        thickness=0.030,
        mat="R1_BareSteel",
        bevel=0.003,
    )
    mb.add_strut(
        p0_u=wrist_u,
        p1_u=(0.70, 1.08, wrist_u[2] - 0.02),
        width=0.024,
        thickness=0.034,
        mat="R1_BareSteel",
        bevel=0.003,
    )
    mb.add_strut(
        p0_u=(0.70, 1.08, wrist_u[2] - 0.02),
        p1_u=(0.65, 1.03, wrist_u[2] - 0.02),
        width=0.020,
        thickness=0.030,
        mat="R1_BareSteel",
        bevel=0.003,
    )

    return mb.build_object(parent=parent)


def _build_wrench_arm_right(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the viewer-right (-X) articulated brass arm (`WrenchArm_R`) with rest pose tip at X = -0.60 m."""
    d = UPGRADE_BAY_DIMS
    px, py, pz = d["arm_r_pivot_x"], d["arm_r_pivot_y"], d["arm_r_pivot_z"]
    mb = r1kit.MeshBuilder("WrenchArm_R", pivot_u=(px, py, pz))

    # 1. Vertical slot carriage plate & triangular brass shoulder gusset at (-1.06, 1.18, -0.04)
    mb.add_bevel_box(
        center_u=(px - 0.01, py - 0.02, pz),
        size_u=(0.055, 0.26, 0.14),
        mat="R1_Copper",
        bevel=0.008,
        segments=1,
    )
    mb.add_strut(
        p0_u=(px, py - 0.10, pz),
        p1_u=(px + 0.10, py + 0.01, pz),
        width=0.058,
        thickness=0.095,
        mat="R1_Copper",
        bevel=0.007,
    )
    sh_u = (px + 0.09, py + 0.01, pz)
    mb.add_cylinder(
        center_u=sh_u,
        radius=0.052,
        length=0.11,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=sh_u,
        radius=0.024,
        length=0.13,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 2. Upper articulated brass boom reaching up-and-inward from (-0.97, 1.19, -0.04) to high elbow (-0.78, 1.58, -0.06)
    elbow_u = (-0.78, 1.58, pz - 0.02)
    mb.add_strut(
        p0_u=sh_u,
        p1_u=elbow_u,
        width=0.060,
        thickness=0.066,
        mat="R1_Copper",
        bevel=0.008,
    )
    for t in (0.34, 0.68):
        cx_c = sh_u[0] + (elbow_u[0] - sh_u[0]) * t
        cy_c = sh_u[1] + (elbow_u[1] - sh_u[1]) * t
        cz_c = sh_u[2] + (elbow_u[2] - sh_u[2]) * t
        mb.add_bevel_box(
            center_u=(cx_c, cy_c, cz_c),
            size_u=(0.072, 0.072, 0.078),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
            rot_deg_u=(0.0, 0.0, 26.0),
        )

    # 3. High cylindrical brass elbow joint drum at (-0.78, 1.58, -0.06)
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.050,
        length=0.105,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=elbow_u,
        radius=0.024,
        length=0.125,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 4. Forearm reaching down-and-inward with rest pose tip at x = -0.60 m (1.20 m clear gap)
    wrist_u = (-0.69, 1.24, pz - 0.04)
    mb.add_strut(
        p0_u=elbow_u,
        p1_u=wrist_u,
        width=0.048,
        thickness=0.054,
        mat="R1_Copper",
        bevel=0.007,
    )
    for t in (0.36, 0.72):
        cx_f = elbow_u[0] + (wrist_u[0] - elbow_u[0]) * t
        cy_f = elbow_u[1] + (wrist_u[1] - elbow_u[1]) * t
        cz_f = elbow_u[2] + (wrist_u[2] - elbow_u[2]) * t
        mb.add_bevel_box(
            center_u=(cx_f, cy_f, cz_f),
            size_u=(0.060, 0.060, 0.066),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
            rot_deg_u=(0.0, 0.0, -15.0),
        )

    # 5. Wrist rotary collar + welding/fitting torch nozzle tip (innermost tip at X = -0.600 m)
    mb.add_cylinder(
        center_u=wrist_u,
        radius=0.036,
        length=0.072,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.005,
        bevel_segments=1,
    )
    mb.add_strut(
        p0_u=wrist_u,
        p1_u=(-0.64, 1.17, wrist_u[2]),
        width=0.032,
        thickness=0.032,
        mat="R1_Copper",
        bevel=0.004,
    )
    mb.add_strut(
        p0_u=(-0.64, 1.17, wrist_u[2]),
        p1_u=(-0.609, 1.11, wrist_u[2]),
        width=0.018,
        thickness=0.018,
        mat="R1_BareSteel",
        bevel=0.002,
    )

    return mb.build_object(parent=parent)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """
    Stage 4: Ground wheel guide rails (`GuideRail_L/R`), articulated brass wrench arms (`WrenchArm_L/R`),
    overhead vertical lead-screw + claw (`Hoist` + `HoistClaw_L/R`), and lamp lenses (`FloodLamp_L/R`, `StatusLamp_1..3`).
    """
    d = UPGRADE_BAY_DIMS

    # Stage_4 hub mesh: rear wheel chock stops on the ground rails
    mb_stage4 = r1kit.MeshBuilder("Stage_4", pivot_u=(0.0, 0.0, 0.0))
    for side in (-1.0, 1.0):
        mb_stage4.add_bevel_box(
            center_u=(side * d["rail_rest_x"], 0.024, -1.04),
            size_u=(0.30, 0.048, 0.12),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )
    stage_4_obj = mb_stage4.build_object(parent=root)

    # 1. GuideRail_L (+X) and GuideRail_R (-X) centered at ±0.61 m with 0.32 m inner trough (0.40 m outer width)
    rail_len = d["rail_length_z"]
    for side, name in ((1.0, "GuideRail_L"), (-1.0, "GuideRail_R")):
        rx = side * d["rail_rest_x"]
        mb_rail = r1kit.MeshBuilder(name, pivot_u=(rx, 0.05, 0.0))
        # Dark-charcoal lower sleeper plate (0.40 m wide)
        mb_rail.add_bevel_box(
            center_u=(rx, 0.018, 0.0),
            size_u=(0.40, 0.036, rail_len),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )
        # Raised inner & outer wheel guide curb flanges (0.04 m wide at rx ± 0.18 m -> 0.32 m clear inner trough)
        for curbx in (-0.180, 0.180):
            mb_rail.add_bevel_box(
                center_u=(rx + curbx, 0.052, 0.0),
                size_u=(0.040, 0.038, rail_len - 0.04),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
            )
        # Brushed steel top wear strip along the inner guide flange
        mb_rail.add_bevel_box(
            center_u=(rx - side * 0.180, 0.074, 0.0),
            size_u=(0.028, 0.010, rail_len - 0.10),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
        )
        # Flared entry/exit lead-in ramps at both ends
        for end_s in (-1.0, 1.0):
            mb_rail.add_bevel_box(
                center_u=(rx, 0.016, end_s * (rail_len * 0.5 + 0.05)),
                size_u=(0.41, 0.032, 0.11),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
            )
        mb_rail.build_object(parent=stage_4_obj)

    # 2. Concept-sized articulated brass arms (`WrenchArm_L` and `WrenchArm_R`)
    _build_wrench_arm_left(stage_4_obj)
    _build_wrench_arm_right(stage_4_obj)

    # 3. Overhead vertical lead-screw actuator (`Hoist`) + hinged brass claw (`HoistClaw_L`, `HoistClaw_R`)
    hy = d["hoist_pivot_y"]
    mb_hoist = r1kit.MeshBuilder("Hoist", pivot_u=(0.0, hy, 0.0))
    # Upper brass guide collar under the carriage
    mb_hoist.add_bevel_box(
        center_u=(0.0, hy - 0.024, 0.0),
        size_u=(0.15, 0.036, 0.15),
        mat="R1_Copper",
        bevel=0.006,
        segments=1,
    )
    # Central bare-steel lead-screw shaft hanging vertically from y = 2.23 down to y = 1.87
    screw_top_y = hy - 0.03
    screw_bot_y = hy - 0.39
    screw_len = screw_top_y - screw_bot_y
    screw_cy = (screw_top_y + screw_bot_y) * 0.5
    mb_hoist.add_cylinder(
        center_u=(0.0, screw_cy, 0.0),
        radius=0.022,
        length=screw_len,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )
    # Prominent helical-look brass/steel lead-screw thread collars + twin parallel guide rods (matching s_upgrade_bay.png)
    n_turns = 10
    for idx in range(n_turns):
        ty = screw_bot_y + 0.024 + idx * ((screw_len - 0.048) / max(1, n_turns - 1))
        mb_hoist.add_cylinder(
            center_u=(0.0, ty, 0.0),
            radius=0.038,
            length=0.016,
            mat="R1_Copper" if (idx % 2 == 0) else "R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    for gx in (-0.048, 0.048):
        mb_hoist.add_cylinder(
            center_u=(gx, screw_cy, 0.0),
            radius=0.010,
            length=screw_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )

    # Lower brass spreader clevis block at bottom of the lead-screw
    clevis_cy = hy + d["hoist_claw_offset_y"] + 0.032
    mb_hoist.add_bevel_box(
        center_u=(0.0, clevis_cy, 0.0),
        size_u=(0.20, 0.072, 0.13),
        mat="R1_Copper",
        bevel=0.010,
        segments=1,
    )
    for side in (-1.0, 1.0):
        px_c = side * d["hoist_claw_offset_x"]
        py_c = hy + d["hoist_claw_offset_y"]
        mb_hoist.add_cylinder(
            center_u=(px_c, py_c, 0.0),
            radius=0.020,
            length=0.145,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    hoist_obj = mb_hoist.build_object(parent=stage_4_obj)

    # Hinged curved brass gripper jaws (`HoistClaw_L`, `HoistClaw_R`) beneath the clevis block
    claw_y = hy + d["hoist_claw_offset_y"]
    for side, name in ((1.0, "HoistClaw_L"), (-1.0, "HoistClaw_R")):
        cx = side * d["hoist_claw_offset_x"]
        mb_claw = r1kit.MeshBuilder(name, pivot_u=(cx, claw_y, 0.0))
        mb_claw.add_cylinder(
            center_u=(cx, claw_y, 0.0),
            radius=0.030,
            length=0.095,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )
        elbow_j = (side * 0.145, claw_y - 0.095, 0.0)
        mb_claw.add_strut(
            p0_u=(cx, claw_y, 0.0),
            p1_u=elbow_j,
            width=0.038,
            thickness=0.074,
            mat="R1_Copper",
            bevel=0.005,
        )
        mb_claw.add_cylinder(
            center_u=elbow_j,
            radius=0.022,
            length=0.082,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        tip_j = (side * 0.055, claw_y - 0.21, 0.0)
        mb_claw.add_strut(
            p0_u=elbow_j,
            p1_u=tip_j,
            width=0.032,
            thickness=0.064,
            mat="R1_Copper",
            bevel=0.005,
        )
        mb_claw.add_bevel_box(
            center_u=(side * 0.040, claw_y - 0.215, 0.0),
            size_u=(0.038, 0.026, 0.068),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
            rot_deg_u=(0.0, 0.0, side * 18.0),
        )
        mb_claw.build_object(parent=hoist_obj)

    # 4. Hanging dome work lamp lenses (`FloodLamp_L`, `FloodLamp_R`) facing downward under the truss beam
    fx = d["flood_x"]
    fy = d["flood_y"]
    fz = d["flood_z"]
    for side, name in ((1.0, "FloodLamp_L"), (-1.0, "FloodLamp_R")):
        lx = side * fx
        tilt_z = side * 22.0
        mb_flood = r1kit.MeshBuilder(name, pivot_u=(lx, fy, fz))
        # Prominent glowing warm-amber convex bulb/lens (`R1_LampAmber`) protruding from the bottom of the dome shade
        mb_flood.add_cylinder(
            center_u=(lx - side * 0.038, fy - 0.024, fz),
            radius=0.106,
            radius_top=0.116,
            length=0.032,
            mat="R1_LampAmber",
            axis_u="Y",
            segments=10,
            bevel=0.008,
            bevel_segments=1,
            rot_deg_u=(0.0, 0.0, tilt_z),
        )
        mb_flood.build_object(parent=stage_4_obj)

    # 5. Three stacked circular traffic-style status lamps (`StatusLamp_1..3`) on the viewer-right (-X) column front
    rx = -d["col_center_x"]
    front_z = d["col_depth_z"] * 0.5
    lamp_z = front_z + 0.034
    for idx in range(3):
        ly = d["status_lamp_1_y"] + idx * d["status_lamp_step_y"]
        lname = f"StatusLamp_{idx + 1}"
        mb_lamp = r1kit.MeshBuilder(lname, pivot_u=(rx, ly, lamp_z))
        mb_lamp.add_cylinder(
            center_u=(rx, ly, lamp_z + 0.004),
            radius=0.038,
            length=0.016,
            mat="R1_LampAmber",
            axis_u="Z",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )
        mb_lamp.build_object(parent=stage_4_obj)

    return stage_4_obj


def pose_upgrade_bay_rest(root: bpy.types.Object) -> None:
    """Set UpgradeBay moving parts to rest pose (zero rotation, exported FBX pose)."""
    d = UPGRADE_BAY_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "GuideRail_L":
            obj.location = r1kit.u_pos(d["rail_rest_x"], 0.05, 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "GuideRail_R":
            obj.location = r1kit.u_pos(-d["rail_rest_x"], 0.05, 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Hoist":
            obj.location = r1kit.u_pos(0.0, d["hoist_pivot_y"], 0.0)
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name in ("HoistClaw_L", "HoistClaw_R", "WrenchArm_L", "WrenchArm_R"):
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_upgrade_bay_active(root: bpy.types.Object) -> None:
    """Set UpgradeBay moving parts to active fitting pose (rails closed +0.04 m, hoist lowered, claws & arms articulated)."""
    d = UPGRADE_BAY_DIMS
    pose_upgrade_bay_rest(root)
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "GuideRail_L":
            obj.location = r1kit.u_pos(d["rail_rest_x"] - 0.04, 0.05, 0.0)
        elif obj.name == "GuideRail_R":
            obj.location = r1kit.u_pos(-d["rail_rest_x"] + 0.04, 0.05, 0.0)
        elif obj.name == "Hoist":
            obj.location = r1kit.u_pos(0.0, d["hoist_pivot_y"] - 0.08, 0.0)
        elif obj.name == "HoistClaw_L":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, -10.0)
        elif obj.name == "HoistClaw_R":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, 10.0)
        elif obj.name == "WrenchArm_L":
            obj.rotation_euler = r1kit.u_rot(0.0, 4.0, 4.0)
        elif obj.name == "WrenchArm_R":
            obj.rotation_euler = r1kit.u_rot(0.0, -4.0, -4.0)


def _verify_rover_v3_clearance(root: bpy.types.Object) -> None:
    """
    Verify the static portal frame (`Stage_1`, `Stage_2`, `Stage_3`) clears the 07 v3 drive-through envelope
    (`1.48 m` W x `1.59 m` L x `1.76 m` H) above wheel level (`y in [0.10, 1.80]`, `|x| < 0.75`, `|z| < 0.80`).
    """
    pose_upgrade_bay_rest(root)
    bpy.context.view_layer.update()
    static_names = {"Stage_1", "Stage_2", "Stage_3"}
    violations: list[str] = []
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.name not in static_names or obj.data is None:
            continue
        mw = obj.matrix_world
        for v in obj.data.vertices:
            wb = mw @ v.co
            xu, yu, zu = wb.x, wb.z, wb.y
            if abs(xu) < 0.75 and 0.10 < yu < 1.80 and abs(zu) < 0.80:
                violations.append(f"{obj.name} at Unity ({xu:.3f}, {yu:.3f}, {zu:.3f})")
                break
    if violations:
        raise RuntimeError(f"UpgradeBay portal frame violates 07 v3 (1.48x1.59x1.76 m) clearance: {violations}")


def _setup_lunar_night_world(world: bpy.types.World) -> None:
    """Configure a deep lunar night sky with subtle procedural star dots matching s_upgrade_bay.png."""
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Strength"].default_value = 1.0

    tex_coord = nt.nodes.new("ShaderNodeTexCoord")
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (140.0, 140.0, 140.0)

    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.feature = "F1"
    vor.inputs["Scale"].default_value = 1.0
    vor.inputs["Randomness"].default_value = 1.0

    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (0.82, 0.88, 1.0, 1.0)
    ramp.color_ramp.elements[1].position = 0.042
    ramp.color_ramp.elements[1].color = (0.005, 0.009, 0.022, 1.0)

    nt.links.new(tex_coord.outputs["Generated"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], vor.inputs["Vector"])
    nt.links.new(vor.outputs["Distance"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])


def _build_lunar_ground_material() -> bpy.types.Material:
    """Create a cratered/pebbled lunar regolith material matching the dusty ground in s_upgrade_bay.png."""
    mat = bpy.data.materials.new(name="_CompareLunarGround")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.94
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.08

    tex_coord = nt.nodes.new("ShaderNodeTexCoord")
    noise_fine = nt.nodes.new("ShaderNodeTexNoise")
    noise_fine.inputs["Scale"].default_value = 16.0
    noise_fine.inputs["Detail"].default_value = 8.0
    noise_fine.inputs["Roughness"].default_value = 0.72

    ramp_col = nt.nodes.new("ShaderNodeValToRGB")
    ramp_col.color_ramp.elements[0].position = 0.22
    ramp_col.color_ramp.elements[0].color = (0.08, 0.075, 0.070, 1.0)
    ramp_col.color_ramp.elements[1].position = 0.78
    ramp_col.color_ramp.elements[1].color = (0.28, 0.26, 0.23, 1.0)

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    bump.inputs["Distance"].default_value = 0.04

    nt.links.new(tex_coord.outputs["Object"], noise_fine.inputs["Vector"])
    nt.links.new(noise_fine.outputs["Fac"], ramp_col.inputs["Fac"])
    nt.links.new(ramp_col.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(noise_fine.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def render_upgrade_bay_scale_and_compare(root: bpy.types.Object, out_dir: Path, r2_dir: Path) -> None:
    """Render the UpgradeBay scale shot and side-by-side concept comparison (`compare_upgrade_bay.png`) with the real 07 v3 model."""
    out_dir = Path(out_dir).resolve()
    r2_dir = Path(r2_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    r2_dir.mkdir(parents=True, exist_ok=True)

    r1kit.ensure_materials(use_vertex_color_shading=True)
    # Boost R1_LampAmber emission for the night concept render so the dome lights and 3-lamp column glow warmly
    lamp_mat = bpy.data.materials.get("R1_LampAmber")
    if lamp_mat is not None and lamp_mat.node_tree is not None:
        for node in lamp_mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED" and "Emission Strength" in node.inputs:
                node.inputs["Emission Strength"].default_value = 9.0

    pose_upgrade_bay_active(root)
    bpy.context.view_layer.update()

    cleanup_objs: list[bpy.types.Object] = []
    cleanup_meshes: list[bpy.types.Mesh] = []
    cleanup_lights: list[bpy.types.Light] = []
    cleanup_cams: list[bpy.types.Camera] = []
    cleanup_mats: list[bpy.types.Material] = []

    # 1. Import the real 07 v3 model (`RoverModel.fbx`) inside the UpgradeBay facing front (+Z_u = +Y_b)
    rover_fbx = Path(ROVER_FBX_PATH).resolve()
    if rover_fbx.exists():
        before_objs = set(bpy.data.objects)
        before_meshes = set(bpy.data.meshes)
        before_mats = set(bpy.data.materials)
        bpy.ops.import_scene.fbx(filepath=str(rover_fbx), use_anim=False)
        rover_objs = [o for o in bpy.data.objects if o not in before_objs]
        rover_meshes = [m for m in bpy.data.meshes if m not in before_meshes]
        rover_mats = [m for m in bpy.data.materials if m not in before_mats]
        cleanup_objs.extend(rover_objs)
        cleanup_meshes.extend(rover_meshes)
        cleanup_mats.extend(rover_mats)

        rover_dir = str(BLENDER_DIR / "rover")
        if rover_dir not in sys.path:
            sys.path.insert(0, rover_dir)
        try:
            from rover import rover_parts  # type: ignore
        except Exception:
            rover_parts = None

        for obj in rover_objs:
            obj.animation_data_clear()
            if obj.parent is None:
                # RoverModel.fbx imports upright on its 4 wheels with Screen, CutterTurret, and DigScoop facing +Y_b (+Z_u) at 0 rotation.
                obj.rotation_mode = "XYZ"
                obj.rotation_euler = (0.0, 0.0, 0.0)
                obj.location = r1kit.u_pos(0.0, 0.0, -0.06)
            if obj.type == "MESH":
                me = obj.data
                if "Col" in me.color_attributes and "Color" not in me.color_attributes:
                    me.color_attributes["Col"].name = "Color"
                if me.color_attributes:
                    me.color_attributes.active_color = me.color_attributes[0]
                for idx, mat in enumerate(me.materials):
                    if mat is None:
                        continue
                    base_name = mat.name.split(".")[0]
                    if base_name == "R1_Screen" and rover_parts is not None:
                        rover_parts.configure_procedural_crt_screen_shader(
                            mat, {"roughness": 0.22, "emission_strength": 3.5}
                        )
                    else:
                        canon = bpy.data.materials.get(base_name)
                        if canon is not None:
                            me.materials[idx] = canon

    bpy.context.view_layer.update()

    # 2. Build cratered lunar regolith ground with undulating distant ridge at -Y_b (behind the bay) matching s_upgrade_bay.png
    bm_g = bmesh.new()
    grid_n = 48
    grid_ext = 18.0
    verts_grid: list[list[bmesh.types.BMVert]] = []
    for iy in range(grid_n + 1):
        row: list[bmesh.types.BMVert] = []
        v_frac = iy / grid_n
        y_b = 7.0 - v_frac * 25.0  # from +7.0 (front) back to -18.0 (distant horizon behind the bay)
        for ix in range(grid_n + 1):
            u_frac = ix / grid_n
            x_b = -grid_ext + u_frac * (2.0 * grid_ext)
            dist_pad = math.hypot(x_b, y_b)
            ridge = 0.0
            if y_b < -2.2:
                ridge = (
                    0.58 * math.sin((-y_b - 2.2) * 0.26)
                    + 0.16 * math.cos(x_b * 0.48)
                    + 0.08 * math.sin(x_b * 1.3 - y_b * 0.9)
                )
            undul = 0.025 * math.sin(x_b * 1.9) * math.cos(y_b * 1.7) * min(1.0, max(0.0, (dist_pad - 2.0) / 2.2))
            z_b = max(-0.02, ridge + undul) if dist_pad > 2.0 else 0.0
            row.append(bm_g.verts.new((x_b, y_b, z_b)))
        verts_grid.append(row)
    for iy in range(grid_n):
        for ix in range(grid_n):
            f = bm_g.faces.new(
                [verts_grid[iy][ix], verts_grid[iy][ix + 1], verts_grid[iy + 1][ix + 1], verts_grid[iy + 1][ix]]
            )
            f.smooth = True
    bmesh.ops.recalc_face_normals(bm_g, faces=bm_g.faces)

    ground_mesh = bpy.data.meshes.new("_CompareLunarGroundMesh")
    bm_g.to_mesh(ground_mesh)
    bm_g.free()
    ground_mat = _build_lunar_ground_material()
    ground_mesh.materials.append(ground_mat)
    ground_obj = bpy.data.objects.new("_CompareLunarGround", ground_mesh)
    bpy.context.collection.objects.link(ground_obj)
    cleanup_objs.append(ground_obj)
    cleanup_meshes.append(ground_mesh)
    cleanup_mats.append(ground_mat)

    # 3. Night sky & warm key lighting matching s_upgrade_bay.png (front-left sun + warm under-beam work lamps)
    scene = bpy.context.scene
    old_world = scene.world
    night_world = bpy.data.worlds.new("_CompareNightWorld")
    _setup_lunar_night_world(night_world)
    scene.world = night_world

    def _add_light_u(
        name: str,
        ltype: str,
        loc_u: tuple[float, float, float],
        target_u: tuple[float, float, float],
        energy: float,
        color: tuple[float, float, float],
        size: float = 0.5,
        spot_deg: float = 75.0,
    ) -> None:
        ld = bpy.data.lights.new(name=name, type=ltype)
        ld.energy = energy
        ld.color = color
        if ltype == "SUN":
            ld.angle = math.radians(size)
        elif ltype == "AREA":
            ld.size = size
        elif ltype == "SPOT":
            ld.spot_size = math.radians(spot_deg)
            ld.spot_blend = 0.55
            ld.shadow_soft_size = size
        elif ltype == "POINT":
            ld.shadow_soft_size = size
        lo = bpy.data.objects.new(name=name, object_data=ld)
        bpy.context.collection.objects.link(lo)
        loc_b = r1kit.u_pos(*loc_u)
        tgt_b = r1kit.u_pos(*target_u)
        lo.location = loc_b
        direction = (tgt_b - loc_b).normalized()
        lo.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler("XYZ")
        cleanup_objs.append(lo)
        cleanup_lights.append(ld)

    # Warm directional key light from front-left-above (+X_u, +Y_u, +Z_u), casting crisp shadow to the right (-X_u)
    _add_light_u("_CompareSunKey", "SUN", (4.6, 4.4, 5.2), (0.0, 1.0, 0.0), energy=3.5, color=(1.0, 0.90, 0.74), size=4.0)
    # Soft cool night sky fill from front-right (-X_u) so dark charcoal and recessed details stay readable
    _add_light_u("_CompareSkyFill", "SUN", (-4.0, 3.0, 3.5), (0.0, 1.0, 0.0), energy=0.60, color=(0.58, 0.70, 0.96), size=15.0)
    # Warm amber spot + point lamps positioned just below the rim of the two under-beam dome lights (`FloodLamp_L/R`)
    for side in (-1.0, 1.0):
        lx_u = side * (UPGRADE_BAY_DIMS["flood_x"] - 0.05)
        ly_u = UPGRADE_BAY_DIMS["flood_y"] - 0.20
        lz_u = UPGRADE_BAY_DIMS["flood_z"]
        _add_light_u(
            f"_CompareFloodSpot_{int(side)}",
            "SPOT",
            (lx_u, ly_u, lz_u),
            (side * 0.15, 0.10, 0.12),
            energy=165.0,
            color=(1.0, 0.68, 0.24),
            size=0.16,
            spot_deg=85.0,
        )
        _add_light_u(
            f"_CompareFloodGlow_{int(side)}",
            "POINT",
            (lx_u, ly_u - 0.02, lz_u + 0.04),
            (lx_u, 0.0, lz_u),
            energy=28.0,
            color=(1.0, 0.72, 0.28),
            size=0.12,
        )

    # 4. Perspective camera matching s_upgrade_bay.png (1376 x 768, 35 mm focal length, front-left +X_u/+Z_u 3/4 view)
    cam_data = bpy.data.cameras.new(name="_CompareCam")
    cam_data.sensor_width = 36.0
    cam_obj = bpy.data.objects.new(name="_CompareCam", object_data=cam_data)
    bpy.context.collection.objects.link(cam_obj)
    cleanup_objs.append(cam_obj)
    cleanup_cams.append(cam_data)

    old_cam = scene.camera
    scene.camera = cam_obj
    r1kit._aim_camera_at(
        cam_obj,
        cam_pos_u=(3.52, 2.16, 5.28),
        target_u=(-0.08, 1.18, 0.02),
        lens_mm=35.0,
    )

    render_alone_path = r2_dir / "upgrade_bay_concept_view.png"
    r1kit._render_to_file(render_alone_path, res_x=1376, res_y=768, freestyle=True)

    # Also save `upgrade_bay_scale_07.png` from this view
    r1kit._render_to_file(out_dir / "upgrade_bay_scale_07.png", res_x=1120, res_y=840, freestyle=True)

    # Compose side-by-side comparison image (`1376x768` concept left + `1376x768` render right)
    if CONCEPT_IMAGE_PATH.exists():
        for dst in (r2_dir / "compare_upgrade_bay.png", out_dir / "compare_upgrade_bay.png"):
            r1kit.compose_side_by_side(CONCEPT_IMAGE_PATH, render_alone_path, dst, target_height=768)

    # Restore studio state and remove temporary comparison objects
    scene.camera = old_cam
    scene.world = old_world
    bpy.data.worlds.remove(night_world, do_unlink=True)
    for obj in cleanup_objs:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)
    for me in cleanup_meshes:
        if me.name in bpy.data.meshes:
            bpy.data.meshes.remove(me, do_unlink=True)
    for ld in cleanup_lights:
        if ld.name in bpy.data.lights:
            bpy.data.lights.remove(ld, do_unlink=True)
    for cd in cleanup_cams:
        if cd.name in bpy.data.cameras:
            bpy.data.cameras.remove(cd, do_unlink=True)
    for mat in cleanup_mats:
        if mat.name in bpy.data.materials:
            bpy.data.materials.remove(mat, do_unlink=True)

    pose_upgrade_bay_rest(root)
    bpy.context.view_layer.update()
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
    parser.add_argument(
        "--r2-dir",
        type=Path,
        default=Path("Logs/r2"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_upgrade_bay()
    tris = r1kit.count_triangles(root)
    print(f"[UpgradeBay] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        render_upgrade_bay_scale_and_compare(root, args.renders_dir, args.r2_dir)
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
