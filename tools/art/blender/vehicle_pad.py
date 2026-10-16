"""Build script for the R1 VehiclePad structure (`Assets/_Project/Art/R1/Models/Structures/VehiclePad.fbx`)."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Sequence

import bmesh
import bpy
from mathutils import Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

CONCEPT_IMAGE_PATH = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\s_vehicle_pad.png")
ROVER_FBX_PATH = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")

# Key dimensions in r1kit Unity-space metres:
#   +X_u is viewer-LEFT when looking at the front (+Z_u); -X_u is viewer-RIGHT; +Y_u is up; +Z_u is front.
VEHICLE_PAD_DIMS: dict[str, float] = {
    "footprint_width": 3.40,
    "footprint_depth": 2.96,
    "total_height": 2.28,
    "deck_height": 0.22,
    "curb_top_y": 0.26,
    "subframe_h": 0.12,
    # 8-sided octagonal pad outer & inner dimensions
    "oct_outer_x": 1.56,
    "oct_outer_z": 1.36,
    "oct_corner_x": 0.78,
    "oct_corner_z": 0.58,
    "oct_inner_x": 1.28,
    "oct_inner_z": 1.08,
    "oct_inner_cx": 0.64,
    "oct_inner_cz": 0.44,
    # Corner cantilever crane post (back-right -X_u, -Z_u corner, matching s_vehicle_pad.png)
    "crane_x": -1.14,
    "crane_z": -0.92,
    "crane_post_w": 0.36,
    "crane_post_d": 0.36,
    "crane_shoulder_y": 1.94,
    "stripe_center_y": 1.44,
    "stripe_height": 0.16,
    "patch_center_y": 0.98,
    # Protruding lower weld control console & Socket_Power_1 on crane post front/inner face
    "console_x": -1.14,
    "console_y": 0.54,
    "console_z": -0.66,
    "socket_power_x": -1.36,
    "socket_power_y": 0.16,
    "socket_power_z": -0.56,
    # Rotating CraneBoom, HoistHook (pulley + 4-cable lifting spreader frame), and BoomLamp
    "boom_tip_x": 0.06,
    "boom_tip_y": 2.16,
    "boom_tip_z": 0.02,
    "hook_pivot_y": 2.08,
    "hook_drop_y": 0.12,
    "boom_lamp_x": 0.18,
    "boom_lamp_y": 1.96,
    "boom_lamp_z": 0.12,
    # Hinged front (+Z_u) tread-plate ramp (`RampLip`) sloping down to the ground
    "ramp_pivot_y": 0.20,
    "ramp_pivot_z": 1.34,
    "ramp_width_x": 1.44,
    "ramp_length_z": 0.56,
    "ramp_drop_deg": 4.0,
    # 4 corner bollard lamps mounted on top of the continuous cream octagonal curb
    "bollard_1_x": 1.42,
    "bollard_1_z": 0.48,
    "bollard_2_x": -1.42,
    "bollard_2_z": 0.48,
    "bollard_3_x": 0.70,
    "bollard_3_z": -1.22,
    "bollard_4_x": -0.70,
    "bollard_4_z": 1.22,
    "bollard_lamp_y": 0.42,
}


def _octagon_vertices_xz(rx: float, rz: float, cx: float, cz: float) -> list[tuple[float, float]]:
    """
    Return the 8 vertices (x_u, z_u) of a symmetric octagon in counter-clockwise order starting at front-left:
      0: (+cx, +rz)  front-left
      1: (+rx, +cz)  left-front
      2: (+rx, -cz)  left-back
      3: (+cx, -rz)  back-left
      4: (-cx, -rz)  back-right
      5: (-rx, -cz)  right-back
      6: (-rx, +cz)  right-front
      7: (-cx, +rz)  front-right
    """
    return [
        (cx, rz),
        (rx, cz),
        (rx, -cz),
        (cx, -rz),
        (-cx, -rz),
        (-rx, -cz),
        (-rx, cz),
        (-cx, rz),
    ]


def _add_octagonal_prism(
    mb: r1kit.MeshBuilder,
    verts_xz: Sequence[tuple[float, float]],
    y_bot: float,
    y_top: float,
    mat: str,
    bevel: float = 0.012,
    top_inset: float = 0.0,
) -> list[bmesh.types.BMFace]:
    """
    Build a watertight all-quad octagonal slab/plate from an 8-vertex (x_u, z_u) loop.
    Caps top and bottom with 3 clean quads each (0-1-6-7, 1-2-5-6, 2-3-4-5) so there are zero n-gons and zero boundary edges.
    """
    n = len(verts_xz)
    bot_ring: list[bmesh.types.BMVert] = []
    top_ring: list[bmesh.types.BMVert] = []
    for x, z in verts_xz:
        bot_ring.append(mb.bm.verts.new(mb._to_local_b((x, y_bot, z))))
        scale = 1.0 - top_inset
        top_ring.append(mb.bm.verts.new(mb._to_local_b((x * scale, y_top, z * scale))))

    faces: list[bmesh.types.BMFace] = []
    for i in range(n):
        j = (i + 1) % n
        faces.append(mb.bm.faces.new([bot_ring[i], bot_ring[j], top_ring[j], top_ring[i]]))

    # 3 all-quad strips across the 8-vertex top and bottom caps
    for k in range(3):
        i0, i1, i2, i3 = k, k + 1, 6 - k, 7 - k
        faces.append(mb.bm.faces.new([top_ring[i0], top_ring[i1], top_ring[i2], top_ring[i3]]))
        faces.append(mb.bm.faces.new([bot_ring[i3], bot_ring[i2], bot_ring[i1], bot_ring[i0]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    edges = r1kit._unique(e for f in faces for e in f.edges if e.is_valid)
    mb._assign_faces(faces, mat)
    if bevel > 0.0:
        faces, _ = mb._bevel_edges(faces, edges, bevel, 1, mat)
    return faces


def _add_continuous_octagonal_curb(
    mb: r1kit.MeshBuilder,
    outer_xz: Sequence[tuple[float, float]],
    inner_xz: Sequence[tuple[float, float]],
    y_bot: float,
    y_top: float,
    mat: str,
) -> list[bmesh.types.BMFace]:
    """
    Build ONE continuous, closed, bevelled 8-sided octagonal cream curb around the perimeter of the pad.
    Uses a 8-vertex bevelled cross-section ring at each of the 8 octagon vertices, connected in a closed loop
    with zero gaps and zero boundary edges.
    """
    # Cross-section profile stations (t_outer in [0..1], y_u) around the curb rim
    h = y_top - y_bot
    prof = [
        (0.98, y_bot),
        (1.00, y_bot + 0.018),
        (0.99, y_top - 0.032),
        (0.90, y_top - 0.008),
        (0.80, y_top),
        (0.18, y_top),
        (0.06, y_top - 0.010),
        (0.00, y_bot),
    ]
    n_corners = len(outer_xz)
    m_prof = len(prof)
    rings: list[list[bmesh.types.BMVert]] = []
    for (ox, oz), (ix, iz) in zip(outer_xz, inner_xz):
        ring: list[bmesh.types.BMVert] = []
        for t, y_p in prof:
            px = ix + (ox - ix) * t
            pz = iz + (oz - iz) * t
            ring.append(mb.bm.verts.new(mb._to_local_b((px, y_p, pz))))
        rings.append(ring)

    faces: list[bmesh.types.BMFace] = []
    for c_idx in range(n_corners):
        r0 = rings[c_idx]
        r1 = rings[(c_idx + 1) % n_corners]
        for p_idx in range(m_prof):
            q_idx = (p_idx + 1) % m_prof
            faces.append(mb.bm.faces.new([r0[p_idx], r1[p_idx], r1[q_idx], r0[q_idx]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def _add_trapezoid_panel(
    mb: r1kit.MeshBuilder,
    p0_xz: tuple[float, float],
    p1_xz: tuple[float, float],
    q1_xz: tuple[float, float],
    q0_xz: tuple[float, float],
    y_bot: float,
    y_top: float,
    mat: str,
    bevel: float = 0.005,
) -> list[bmesh.types.BMFace]:
    """Build a watertight 6-face quad trapezoidal deck plate from 4 (x_u, z_u) corners."""
    corners = [p0_xz, p1_xz, q1_xz, q0_xz]
    bot_v = [mb.bm.verts.new(mb._to_local_b((x, y_bot, z))) for x, z in corners]
    top_v = [mb.bm.verts.new(mb._to_local_b((x, y_top, z))) for x, z in corners]
    faces: list[bmesh.types.BMFace] = [
        mb.bm.faces.new([top_v[0], top_v[1], top_v[2], top_v[3]]),
        mb.bm.faces.new([bot_v[3], bot_v[2], bot_v[1], bot_v[0]]),
    ]
    for i in range(4):
        j = (i + 1) % 4
        faces.append(mb.bm.faces.new([bot_v[i], bot_v[j], top_v[j], top_v[i]]))
    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    edges = r1kit._unique(e for f in faces for e in f.edges if e.is_valid)
    mb._assign_faces(faces, mat)
    if bevel > 0.0:
        faces, _ = mb._bevel_edges(faces, edges, bevel, 1, mat)
    return faces


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Heavy octagonal under-deck chassis slab, 10 perimeter ribbed ground-anchor brackets, crane foundation, and Socket_Power_1."""
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    sh = d["subframe_h"]
    # 1. True octagonal dark-charcoal under-deck foundation slab (y = 0.00 .. 0.12 m)
    base_oct = _octagon_vertices_xz(
        d["oct_outer_x"] - 0.02,
        d["oct_outer_z"] - 0.02,
        d["oct_corner_x"] - 0.01,
        d["oct_corner_z"] - 0.01,
    )
    _add_octagonal_prism(mb, base_oct, y_bot=0.0, y_top=sh, mat="R1_GreyMetal", bevel=0.014, top_inset=0.01)

    # 2. Ribbed dark-charcoal ground-anchor outrigger brackets around the 8 sides of the octagonal pad (matching s_vehicle_pad.png)
    bolts: list[tuple[float, float, float]] = []
    outriggers = [
        # Left (+X) and right (-X) edges (pairs of brackets like s_vehicle_pad.png)
        (1.60, 0.30, 0.22, 0.20, 0.0),
        (1.60, -0.30, 0.22, 0.20, 0.0),
        (-1.60, 0.30, 0.22, 0.20, 0.0),
        (-1.60, -0.30, 0.22, 0.20, 0.0),
        # Back (-Z) edge
        (0.42, -1.40, 0.20, 0.22, 0.0),
        (-0.42, -1.40, 0.20, 0.22, 0.0),
        # 4 diagonal 45-degree octagon chamfer edges
        (1.22, 1.02, 0.22, 0.22, -45.0),
        (-1.22, 1.02, 0.22, 0.22, 45.0),
        (1.22, -1.02, 0.22, 0.22, 45.0),
        (-1.22, -1.02, 0.22, 0.22, -45.0),
    ]
    for ox, oz, ow, od, oyaw in outriggers:
        mb.add_bevel_box(
            center_u=(ox, 0.044, oz),
            size_u=(ow, 0.088, od),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
            rot_deg_u=(0.0, oyaw, 0.0),
            top_scale_xz=(0.88, 0.88),
        )
        bolts.append((ox, 0.092, oz))

    # 3. Stepped corner crane foundation pedestal on the back-right (-X, -Z) octagon corner
    cx = d["crane_x"]
    cz = d["crane_z"]
    mb.add_bevel_box(
        center_u=(cx, 0.295, cz),
        size_u=(0.48, 0.09, 0.48),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
        top_scale_xz=(0.90, 0.90),
    )
    for bx_off in (-0.18, 0.18):
        for bz_off in (-0.18, 0.18):
            bolts.append((cx + bx_off, 0.344, cz + bz_off))

    mb.add_bolts(bolts, radius=0.015, height=0.012, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    # 4. Lower power cable gland receptacle + trailing ground cables at the crane post base (matching s_vehicle_pad.png)
    px = d["socket_power_x"]
    py = d["socket_power_y"]
    pz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(px + 0.030, py, pz),
        radius=0.050,
        length=0.054,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=8,
        bevel=0.005,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(px + 0.004, py, pz),
        radius=0.032,
        length=0.034,
        mat="R1_BareSteel",
        axis_u="X",
        segments=8,
        bevel=0.004,
        bevel_segments=1,
    )

    stage_1_obj = mb.build_object(parent=root)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(px, py, pz),
        outward_dir_u=(-1.0, 0.0, 0.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """
    Stage 2: Dark octagonal assembly deck with 8 trapezoidal non-slip tread-plate panels + central octagonal plate
    (zero glossy white strips), protruding weld control console with trailing cables, multi-loop coiled hose, and bollard pedestals.
    """
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    sh = d["subframe_h"]   # 0.12 m
    dh = d["deck_height"]  # 0.22 m

    # 1. Recessed dark-charcoal octagonal sub-deck bed (y = 0.12 .. 0.21 m)
    inner_oct = _octagon_vertices_xz(
        d["oct_inner_x"] + 0.02,
        d["oct_inner_z"] + 0.02,
        d["oct_inner_cx"] + 0.01,
        d["oct_inner_cz"] + 0.01,
    )
    _add_octagonal_prism(mb, inner_oct, y_bot=sh, y_top=dh - 0.010, mat="R1_GreyMetal", bevel=0.008)

    # 2. Central octagonal dark non-slip plate (`R1_Rubber` with `R1_GreyMetal` collar & subtle `R1_Copper` center cap)
    center_outer = _octagon_vertices_xz(0.46, 0.40, 0.23, 0.17)
    center_inner = _octagon_vertices_xz(0.40, 0.34, 0.20, 0.14)
    _add_octagonal_prism(mb, center_outer, y_bot=dh - 0.010, y_top=dh + 0.004, mat="R1_GreyMetal", bevel=0.004)
    _add_octagonal_prism(mb, center_inner, y_bot=dh - 0.004, y_top=dh + 0.010, mat="R1_Rubber", bevel=0.004)
    mb.add_cylinder(
        center_u=(0.0, dh + 0.010, 0.0),
        radius=0.11,
        length=0.010,
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 3. 8 trapezoidal dark non-slip tread-plate panels (`R1_Rubber` with raised `R1_GreyMetal` tread cleats)
    # arranged radially around the central octagon inside the cream octagonal curb (matching s_vehicle_pad.png)
    panel_outer = _octagon_vertices_xz(
        d["oct_inner_x"] - 0.04,
        d["oct_inner_z"] - 0.04,
        d["oct_inner_cx"] - 0.02,
        d["oct_inner_cz"] - 0.02,
    )
    panel_inner = _octagon_vertices_xz(0.52, 0.45, 0.26, 0.19)
    for i in range(8):
        j = (i + 1) % 8
        # Slightly shrink each panel along its edge to leave crisp radial seam channels between the 8 trapezoids
        ox0, oz0 = panel_outer[i]
        ox1, oz1 = panel_outer[j]
        ix0, iz0 = panel_inner[i]
        ix1, iz1 = panel_inner[j]
        p0 = (ox0 * 0.94 + ox1 * 0.06, oz0 * 0.94 + oz1 * 0.06)
        p1 = (ox0 * 0.06 + ox1 * 0.94, oz0 * 0.06 + oz1 * 0.94)
        q1 = (ix0 * 0.08 + ix1 * 0.92, iz0 * 0.08 + iz1 * 0.92)
        q0 = (ix0 * 0.92 + ix1 * 0.08, iz0 * 0.92 + iz1 * 0.08)
        _add_trapezoid_panel(mb, p0, p1, q1, q0, y_bot=dh - 0.010, y_top=dh + 0.008, mat="R1_Rubber", bevel=0.005)

        # Raised dark-charcoal diamond/bar tread ribs on each trapezoidal panel (`R1_GreyMetal`)
        edge_dx = ox1 - ox0
        edge_dz = oz1 - oz0
        yaw_deg = math.degrees(math.atan2(edge_dx, edge_dz)) + 90.0
        for r_t in (0.30, 0.56, 0.80):
            mx = 0.5 * ((1.0 - r_t) * (ix0 + ix1) + r_t * (ox0 + ox1))
            mz = 0.5 * ((1.0 - r_t) * (iz0 + iz1) + r_t * (oz0 + oz1))
            rib_len = math.hypot(ox1 - ox0, oz1 - oz0) * (0.35 + 0.42 * r_t)
            mb.add_bevel_box(
                center_u=(mx, dh + 0.011, mz),
                size_u=(rib_len, 0.008, 0.036),
                mat="R1_GreyMetal",
                bevel=0.0,
                segments=0,
                rot_deg_u=(0.0, yaw_deg, 0.0),
            )

    # 4. Protruding lower crane/weld control console on the front/inner face of the crane post (matching s_vehicle_pad.png)
    con_x = d["console_x"]
    con_y = d["console_y"]
    con_z = d["console_z"]
    mb.add_bevel_box(
        center_u=(con_x, con_y, con_z),
        size_u=(0.26, 0.28, 0.18),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    # Recessed dark-glass monitor screen + copper button on the console face
    mb.add_bevel_box(
        center_u=(con_x, con_y + 0.03, con_z + 0.084),
        size_u=(0.17, 0.13, 0.024),
        mat="R1_DarkGlass",
        bevel=0.0,
        segments=0,
        rot_deg_u=(-10.0, 0.0, 0.0),
    )
    mb.add_cylinder(
        center_u=(con_x - 0.04, con_y - 0.06, con_z + 0.090),
        radius=0.020,
        length=0.020,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Twin curved cables/conduits descending from the bottom of the control console onto the curb and trailing off to the right (matching s_vehicle_pad.png)
    for x_off, mat_p in ((-0.04, "R1_Rubber"), (0.04, "R1_Copper")):
        mb.add_tube_path(
            [
                (con_x + x_off, con_y - 0.13, con_z + 0.02),
                (con_x + x_off - 0.06, 0.28, con_z + 0.10),
                (d["socket_power_x"] - 0.04, 0.14, d["socket_power_z"] + 0.06),
                (d["socket_power_x"] - 0.28, 0.028, d["socket_power_z"] + 0.26),
                (d["socket_power_x"] - 0.68, 0.024, d["socket_power_z"] + 0.56),
            ],
            radius=0.020,
            mat=mat_p,
            segments=6,
            cap_ends=True,
        )

    # 5. Multi-loop open coiled dark-rubber welding hose (`R1_Rubber`) hanging on a brass hook on the inner-front (+X, +Z) flank of the crane post
    cx = d["crane_x"]
    cz = d["crane_z"]
    mb.add_bevel_box(
        center_u=(cx + 0.16, 1.50, cz + 0.10),
        size_u=(0.06, 0.07, 0.08),
        mat="R1_Copper",
        bevel=0.006,
        segments=1,
    )
    for loop_idx, off_x in enumerate((0.18, 0.21, 0.24)):
        loop_pts: list[tuple[float, float, float]] = []
        rx_l = 0.095 + 0.008 * loop_idx
        ry_l = 0.155 + 0.012 * loop_idx
        for step in range(9):
            ang = math.radians(step * 45.0)
            lx = cx + off_x + rx_l * 0.65 * math.sin(ang)
            ly = 1.36 + ry_l * math.cos(ang)
            lz = cz + 0.12 + rx_l * math.sin(ang)
            loop_pts.append((lx, ly, lz))
        mb.add_tube_path(
            loop_pts,
            radius=0.015,
            mat="R1_Rubber",
            segments=6,
            cap_ends=True,
        )

    # 6. 4 corner bollard lamp bases, top caps, and protective vertical cage bars mounted on top of the cream octagonal curb
    ct = d["curb_top_y"]  # 0.26 m
    bollard_xz = [
        (d["bollard_1_x"], d["bollard_1_z"]),
        (d["bollard_2_x"], d["bollard_2_z"]),
        (d["bollard_3_x"], d["bollard_3_z"]),
        (d["bollard_4_x"], d["bollard_4_z"]),
    ]
    for px_b, pz_b in bollard_xz:
        # Dark-charcoal stepped base collar on top of the cream curb (y = 0.26 .. 0.35 m)
        mb.add_cylinder(
            center_u=(px_b, ct + 0.045, pz_b),
            radius=0.076,
            radius_top=0.060,
            length=0.090,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.005,
            bevel_segments=1,
        )
        # Domed dark-charcoal + brass top cap above the amber lantern (y = 0.49 .. 0.53 m)
        mb.add_cylinder(
            center_u=(px_b, ct + 0.245, pz_b),
            radius=0.062,
            radius_top=0.042,
            length=0.036,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.005,
            bevel_segments=1,
        )
        # 3 vertical protective guard ribs around the bollard lens
        for ang_deg in (0.0, 120.0, 240.0):
            rad_a = math.radians(ang_deg)
            mb.add_cylinder(
                center_u=(px_b + 0.056 * math.cos(rad_a), ct + 0.16, pz_b + 0.056 * math.sin(rad_a)),
                radius=0.006,
                length=0.15,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=6,
                bevel=0.0,
                bevel_segments=0,
            )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Continuous bevelled cream-enamel octagonal curb with bolts, tall corner crane column, orange stripe, and sage patches."""
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # 1. ONE continuous bevelled cream-enamel octagonal curb around the entire pad (y = 0.08 .. 0.26 m)
    outer_oct = _octagon_vertices_xz(d["oct_outer_x"], d["oct_outer_z"], d["oct_corner_x"], d["oct_corner_z"])
    inner_oct = _octagon_vertices_xz(d["oct_inner_x"], d["oct_inner_z"], d["oct_inner_cx"], d["oct_inner_cz"])
    _add_continuous_octagonal_curb(
        mb,
        outer_xz=outer_oct,
        inner_xz=inner_oct,
        y_bot=0.08,
        y_top=d["curb_top_y"],
        mat="R1_CreamEnamel",
    )

    # Top perimeter hex anchor bolts along all 8 sides of the continuous cream octagonal curb
    ct = d["curb_top_y"]
    rim_bolts: list[tuple[float, float, float]] = []
    for i in range(8):
        j = (i + 1) % 8
        mx0 = 0.5 * (outer_oct[i][0] + inner_oct[i][0])
        mz0 = 0.5 * (outer_oct[i][1] + inner_oct[i][1])
        mx1 = 0.5 * (outer_oct[j][0] + inner_oct[j][0])
        mz1 = 0.5 * (outer_oct[j][1] + inner_oct[j][1])
        for frac in (0.18, 0.50, 0.82):
            bx = mx0 + (mx1 - mx0) * frac
            bz = mz0 + (mz1 - mz0) * frac
            rim_bolts.append((bx, ct + 0.004, bz))
    mb.add_bolts(rim_bolts, radius=0.013, height=0.010, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    # 2. Corner cantilever crane column at (-1.14, y = 0.34 .. 1.94 m, -0.92)
    cx = d["crane_x"]
    cz = d["crane_z"]
    pw = d["crane_post_w"]
    pd = d["crane_post_d"]
    y0 = 0.34
    y_mid = 1.62
    y_top = d["crane_shoulder_y"]

    # Lower main cream-enamel column shaft (y = 0.34 .. 1.62 m)
    mb.add_bevel_box(
        center_u=(cx, 0.5 * (y0 + y_mid), cz),
        size_u=(pw, y_mid - y0, pd),
        mat="R1_CreamEnamel",
        bevel=0.028,
        segments=2,
        top_scale_xz=(0.95, 0.95),
    )
    # Upper stepped cream-enamel crane shoulder (y = 1.62 .. 1.94 m)
    mb.add_bevel_box(
        center_u=(cx, 0.5 * (y_mid + y_top), cz),
        size_u=(pw + 0.016, y_top - y_mid, pd + 0.016),
        mat="R1_CreamEnamel",
        bevel=0.024,
        segments=2,
        top_scale_xz=(0.86, 0.86),
    )

    # 3. Continuous horizontal warm-orange beltline stripe (#FF8A5B) wrapping the crane column at y = 1.44 m
    mb.add_bevel_box(
        center_u=(cx, d["stripe_center_y"], cz),
        size_u=(pw + 0.012, d["stripe_height"], pd + 0.012),
        mat="R1_OrangeStripe",
        bevel=0.008,
        segments=1,
    )

    # 4. Riveted sage-green salvaged maintenance panel (R1_SagePatch) on the front (+Z) face of the crane post
    front_cz = cz + pd * 0.5
    py = d["patch_center_y"]
    mb.add_bevel_box(
        center_u=(cx, py, front_cz + 0.010),
        size_u=(0.24, 0.34, 0.022),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )
    patch_rivets = [
        (cx + rx_o, py + ry_o, front_cz + 0.022)
        for rx_o in (-0.088, 0.088)
        for ry_o in (-0.13, 0.13)
    ]
    mb.add_bolts(patch_rivets, radius=0.011, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

    # Sage-green front panel on the lower crane control console
    mb.add_bevel_box(
        center_u=(d["console_x"], d["console_y"] - 0.01, d["console_z"] + 0.088),
        size_u=(0.22, 0.22, 0.014),
        mat="R1_SagePatch",
        bevel=0.005,
        segments=1,
    )

    return mb.build_object(parent=root)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """
    Stage 4: Tapered cream jib (`CraneBoom`) -> (`HoistHook` with pulley block + 4-cable lifting spreader frame,
    `BoomLamp` large hanging dome lamp), hinged front tread-plate ramp (`RampLip`), and 4 corner `BollardLamp_1..4`.
    """
    d = VEHICLE_PAD_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. CraneBoom: rotating slewing turret + tapered cream cantilevered jib + hydraulic luffing cylinder + top tension cable + large dome lamp shade
    cx = d["crane_x"]
    cy = d["crane_shoulder_y"]
    cz = d["crane_z"]
    tx = d["boom_tip_x"]
    ty = d["boom_tip_y"]
    tz = d["boom_tip_z"]

    boom_mb = r1kit.MeshBuilder("CraneBoom", pivot_u=(cx, cy, cz))
    # Slewing ring turntable & vertical cream-enamel turret cap reaching y = 2.26 m
    boom_mb.add_cylinder(
        center_u=(cx, cy + 0.024, cz),
        radius=0.16,
        length=0.048,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.005,
        bevel_segments=1,
    )
    boom_mb.add_bevel_box(
        center_u=(cx, cy + 0.16, cz),
        size_u=(0.30, 0.28, 0.30),
        mat="R1_CreamEnamel",
        bevel=0.022,
        segments=2,
        top_scale_xz=(0.84, 0.84),
    )
    # Circular side bearing boss on the crane turret head (matching s_vehicle_pad.png)
    boom_mb.add_cylinder(
        center_u=(cx, cy + 0.15, cz + 0.15),
        radius=0.058,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        bevel_segments=1,
    )

    # Continuous tapered cream-enamel jib boom from column shoulder to tip (matching s_vehicle_pad.png)
    jib_p0 = (cx + 0.04, cy + 0.13, cz + 0.03)
    jib_p1 = (cx * 0.52 + tx * 0.48, cy + 0.16, cz * 0.52 + tz * 0.48)
    jib_p2 = (tx, ty, tz)
    boom_mb.add_strut(
        p0_u=jib_p0,
        p1_u=jib_p1,
        width=0.17,
        thickness=0.22,
        mat="R1_CreamEnamel",
        bevel=0.016,
    )
    boom_mb.add_strut(
        p0_u=jib_p1,
        p1_u=jib_p2,
        width=0.15,
        thickness=0.17,
        mat="R1_CreamEnamel",
        bevel=0.014,
    )
    # Cream-enamel boom nose housing + dark sheave pin at (tx, ty - 0.01, tz)
    boom_mb.add_bevel_box(
        center_u=(tx, ty, tz),
        size_u=(0.16, 0.15, 0.16),
        mat="R1_CreamEnamel",
        bevel=0.016,
        segments=1,
        rot_deg_u=(0.0, -38.0, 0.0),
    )
    boom_mb.add_cylinder(
        center_u=(tx + 0.04, ty, tz + 0.06),
        radius=0.034,
        length=0.04,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=8,
        bevel=0.003,
        bevel_segments=1,
    )

    # Lower diagonal hydraulic luffing cylinder (brass barrel + bare-steel piston rod) bracing the underside of the jib
    hyd_p0 = (cx + 0.10, cy - 0.16, cz + 0.08)
    hyd_mid = (cx * 0.60 + tx * 0.40, cy * 0.60 + (ty - 0.06) * 0.40 - 0.09, cz * 0.60 + tz * 0.40)
    hyd_p1 = (cx * 0.34 + tx * 0.66, cy * 0.34 + (ty - 0.06) * 0.66 - 0.04, cz * 0.34 + tz * 0.66)
    boom_mb.add_pipe(hyd_p0, hyd_mid, radius=0.038, mat="R1_Copper", segments=8)
    boom_mb.add_pipe(hyd_mid, hyd_p1, radius=0.022, mat="R1_BareSteel", segments=8)

    # Upper tension guy-cable rod from kingpost peak (y = 2.28 m) to outer boom tip
    boom_mb.add_pipe(
        p0_u=(cx, d["total_height"] - 0.01, cz),
        p1_u=(tx, ty + 0.06, tz),
        radius=0.014,
        mat="R1_BareSteel",
        segments=6,
    )

    # Gooseneck bracket + large flared brass dome work lamp shade at the very tip of the jib (`BoomLamp` position, matching s_vehicle_pad.png)
    blx = d["boom_lamp_x"]
    bly = d["boom_lamp_y"]
    blz = d["boom_lamp_z"]
    boom_mb.add_tube_path(
        [
            (tx + 0.02, ty + 0.02, tz + 0.02),
            (blx - 0.03, bly + 0.14, blz - 0.03),
            (blx, bly + 0.08, blz),
        ],
        radius=0.020,
        mat="R1_GreyMetal",
        segments=6,
        cap_ends=True,
    )
    boom_mb.add_cylinder(
        center_u=(blx, bly + 0.072, blz),
        radius=0.064,
        radius_top=0.044,
        length=0.044,
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.004,
        bevel_segments=1,
    )
    boom_mb.add_cylinder(
        center_u=(blx, bly + 0.022, blz),
        radius=0.168,
        radius_top=0.068,
        length=0.096,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.008,
        bevel_segments=1,
    )
    boom_obj = boom_mb.build_object(parent=stage_4)

    # 2. HoistHook (child of CraneBoom): hoist cable + pulley block + 4-cable lifting spreader frame with 4 chassis clamps
    hy_piv = d["hook_pivot_y"]
    hook_mb = r1kit.MeshBuilder("HoistHook", pivot_u=(tx, hy_piv, tz))
    # Twin vertical steel hoist cables descending from jib nose (y = 2.14 m) to pulley sheave block (y = 1.68 m)
    cable_top_y = 2.14
    sheave_y = 1.68
    cable_cy = 0.5 * (cable_top_y + sheave_y)
    cable_len = cable_top_y - sheave_y
    for cx_off in (-0.018, 0.018):
        hook_mb.add_cylinder(
            center_u=(tx + cx_off, cable_cy, tz),
            radius=0.011,
            length=cable_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )
    # Heavy pulley sheave block & brass wheel at y = 1.66 m
    hook_mb.add_bevel_box(
        center_u=(tx, sheave_y, tz),
        size_u=(0.12, 0.14, 0.10),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    hook_mb.add_cylinder(
        center_u=(tx, sheave_y, tz),
        radius=0.054,
        length=0.112,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        bevel_segments=1,
    )

    # 4 splayed steel sling cables from bottom of pulley block (y = 1.60 m) to the 4 corners of the rectangular lifting spreader frame (y = 1.34 m)
    frame_y = 1.34
    frame_hx = 0.38
    frame_hz = 0.25
    sling_top = (tx, sheave_y - 0.06, tz)
    for sx_f in (-1.0, 1.0):
        for sz_f in (-1.0, 1.0):
            c_pt = (tx + sx_f * frame_hx, frame_y + 0.02, tz + sz_f * frame_hz)
            hook_mb.add_pipe(
                p0_u=sling_top,
                p1_u=c_pt,
                radius=0.011,
                mat="R1_BareSteel",
                segments=6,
            )
            # Brass corner clevis block + downward chassis-lifting clamp jaw at each of the 4 spreader corners
            hook_mb.add_bevel_box(
                center_u=(c_pt[0], frame_y, c_pt[2]),
                size_u=(0.072, 0.064, 0.072),
                mat="R1_Copper",
                bevel=0.006,
                segments=1,
            )
            hook_mb.add_bevel_box(
                center_u=(c_pt[0], frame_y - 0.065, c_pt[2]),
                size_u=(0.036, 0.085, 0.044),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
            )

    # Rectangular lifting spreader frame bars (2 longitudinal side beams, 2 end crossbeams, and 1 central crossbeam)
    for sz_f in (-1.0, 1.0):
        hook_mb.add_strut(
            p0_u=(tx - frame_hx, frame_y, tz + sz_f * frame_hz),
            p1_u=(tx + frame_hx, frame_y, tz + sz_f * frame_hz),
            width=0.042,
            thickness=0.046,
            mat="R1_GreyMetal",
            bevel=0.004,
        )
    for sx_f in (-frame_hx, 0.0, frame_hx):
        hook_mb.add_strut(
            p0_u=(tx + sx_f, frame_y, tz - frame_hz),
            p1_u=(tx + sx_f, frame_y, tz + frame_hz),
            width=0.040,
            thickness=0.044,
            mat="R1_GreyMetal",
            bevel=0.004,
        )
    hook_mb.build_object(parent=boom_obj)

    # 3. BoomLamp (child of CraneBoom, only R1_LampAmber): bright warm-amber convex bulb protruding under the dome shade
    bl_mb = r1kit.MeshBuilder("BoomLamp", pivot_u=(blx, bly, blz))
    bl_mb.add_cylinder(
        center_u=(blx, bly - 0.010, blz),
        radius=0.114,
        radius_top=0.124,
        length=0.032,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=10,
        bevel=0.008,
        bevel_segments=1,
    )
    bl_mb.build_object(parent=boom_obj)

    # 4. RampLip (child of Stage_4): hinged front (+Z_u) tread-plate ramp sloping down from the octagonal curb to the ground
    ry_p = d["ramp_pivot_y"]
    rz_p = d["ramp_pivot_z"]
    rw_x = d["ramp_width_x"]
    rl_z = d["ramp_length_z"]
    ramp_pitch = 19.0
    ramp_mb = r1kit.MeshBuilder("RampLip", pivot_u=(0.0, ry_p, rz_p))

    # Twin heavy hinge barrels at the top of the ramp (matching s_vehicle_pad.png)
    for hx_k in (-0.48, 0.48):
        ramp_mb.add_cylinder(
            center_u=(hx_k, ry_p + 0.02, rz_p),
            radius=0.032,
            length=0.22,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )

    # Main sloped dark-charcoal ramp bed (`R1_GreyMetal`) + dark non-slip rubber tread insert (`R1_Rubber`) reaching ground level
    ramp_cy = ry_p - 0.088
    ramp_cz = rz_p + rl_z * 0.48
    ramp_mb.add_bevel_box(
        center_u=(0.0, ramp_cy, ramp_cz),
        size_u=(rw_x, 0.056, rl_z),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
        rot_deg_u=(ramp_pitch, 0.0, 0.0),
    )
    ramp_mb.add_bevel_box(
        center_u=(0.0, ramp_cy + 0.010, ramp_cz + 0.004),
        size_u=(rw_x - 0.16, 0.048, rl_z - 0.06),
        mat="R1_Rubber",
        bevel=0.004,
        segments=1,
        rot_deg_u=(ramp_pitch, 0.0, 0.0),
    )
    # Raised horizontal dark-metal traction ribs across the sloped ramp bed
    for t_rib in (0.20, 0.40, 0.60, 0.80):
        z_r = rz_p + rl_z * t_rib
        y_r = ry_p + 0.018 - (rl_z * t_rib) * math.tan(math.radians(ramp_pitch))
        ramp_mb.add_bevel_box(
            center_u=(0.0, y_r, z_r),
            size_u=(rw_x - 0.22, 0.012, 0.028),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
            rot_deg_u=(ramp_pitch, 0.0, 0.0),
        )
    # Bevelled cream-enamel side cheek curbs on left and right edges of the ramp (matching s_vehicle_pad.png)
    for sx_end in (-1.0, 1.0):
        ramp_mb.add_bevel_box(
            center_u=(sx_end * (rw_x * 0.5 + 0.03), ramp_cy + 0.016, ramp_cz - 0.01),
            size_u=(0.095, 0.078, rl_z),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
            rot_deg_u=(ramp_pitch, 0.0, 0.0),
        )
    ramp_mb.build_object(parent=stage_4)

    # 5. BollardLamp_1..4 (children of Stage_4, only R1_LampAmber): 4 glowing amber lanterns at the octagon corners
    by_b = d["bollard_lamp_y"]
    bollard_coords = [
        ("BollardLamp_1", d["bollard_1_x"], by_b, d["bollard_1_z"]),
        ("BollardLamp_2", d["bollard_2_x"], by_b, d["bollard_2_z"]),
        ("BollardLamp_3", d["bollard_3_x"], by_b, d["bollard_3_z"]),
        ("BollardLamp_4", d["bollard_4_x"], by_b, d["bollard_4_z"]),
    ]
    for b_name, bx_i, by_i, bz_i in bollard_coords:
        b_mb = r1kit.MeshBuilder(b_name, pivot_u=(bx_i, by_i, bz_i))
        b_mb.add_cylinder(
            center_u=(bx_i, by_i, bz_i),
            radius=0.050,
            radius_top=0.044,
            length=0.14,
            mat="R1_LampAmber",
            axis_u="Y",
            segments=10,
            bevel=0.005,
            bevel_segments=1,
        )
        b_mb.build_object(parent=stage_4)

    return stage_4


def pose_vehicle_pad_rest(root: bpy.types.Object) -> None:
    """Set VehiclePad moving parts to rest pose (CraneBoom at 0°, HoistHook raised, RampLip at 0°)."""
    d = VEHICLE_PAD_DIMS
    rel_x = d["boom_tip_x"] - d["crane_x"]
    rel_y = d["hook_pivot_y"] - d["crane_shoulder_y"]
    rel_z = d["boom_tip_z"] - d["crane_z"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name in ("CraneBoom", "RampLip"):
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "HoistHook":
            obj.location = r1kit.u_pos(rel_x, rel_y, rel_z)
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_vehicle_pad_active(root: bpy.types.Object) -> None:
    """Set VehiclePad moving parts to active assembly pose (CraneBoom slewed slightly, HoistHook lowered, RampLip grounded)."""
    d = VEHICLE_PAD_DIMS
    rel_x = d["boom_tip_x"] - d["crane_x"]
    rel_y = d["hook_pivot_y"] - d["crane_shoulder_y"] - d["hook_drop_y"]
    rel_z = d["boom_tip_z"] - d["crane_z"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "CraneBoom":
            obj.rotation_euler = r1kit.u_rot(0.0, -4.0, 0.0)
        elif obj.name == "HoistHook":
            obj.location = r1kit.u_pos(rel_x, rel_y, rel_z)
        elif obj.name == "RampLip":
            obj.rotation_euler = r1kit.u_rot(d["ramp_drop_deg"], 0.0, 0.0)


def _build_suspended_chassis_standin(pos_u: tuple[float, float, float], yaw_deg_u: float = 38.0) -> bpy.types.Object:
    """
    Build the 6-wheeled open-frame vehicle chassis suspended beneath the VehiclePad lifting spreader frame
    for the concept comparison render (`s_vehicle_pad.png`), without exporting it into `VehiclePad.fbx`.
    """
    rx, ry, rz = pos_u
    mb = r1kit.MeshBuilder("_SuspendedVehicleChassis", pivot_u=pos_u)

    # 6 chunky treaded rubber tires (3 per side) + dark-metal suspension arms
    wheel_r = 0.22
    wheel_w = 0.19
    wheel_x = 0.54
    for sx in (-1.0, 1.0):
        for sz in (-0.52, 0.0, 0.52):
            mb.add_cylinder(
                center_u=(rx + sx * wheel_x, ry + wheel_r, rz + sz),
                radius=wheel_r,
                length=wheel_w,
                mat="R1_Rubber",
                axis_u="X",
                segments=12,
                bevel=0.020,
                bevel_segments=1,
            )
            mb.add_cylinder(
                center_u=(rx + sx * (wheel_x + 0.012), ry + wheel_r, rz + sz),
                radius=wheel_r * 0.52,
                length=wheel_w + 0.016,
                mat="R1_CreamEnamel",
                axis_u="X",
                segments=10,
                bevel=0.008,
                bevel_segments=1,
            )
            mb.add_cylinder(
                center_u=(rx + sx * (wheel_x + 0.020), ry + wheel_r, rz + sz),
                radius=wheel_r * 0.22,
                length=wheel_w + 0.028,
                mat="R1_GreyMetal",
                axis_u="X",
                segments=8,
                bevel=0.005,
                bevel_segments=1,
            )
            mb.add_strut(
                p0_u=(rx + sx * 0.34, ry + 0.38, rz + sz),
                p1_u=(rx + sx * (wheel_x - 0.05), ry + wheel_r, rz + sz),
                width=0.044,
                thickness=0.044,
                mat="R1_GreyMetal",
                bevel=0.004,
            )

    # Open-frame chassis: dark-charcoal lower floor pan + cream-enamel open side truss rails (with rectangular side cutouts like s_vehicle_pad.png)
    mb.add_bevel_box(
        center_u=(rx, ry + 0.38, rz),
        size_u=(0.76, 0.08, 1.38),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    # Lower and upper cream side rails + vertical pillars forming the open side windows
    for sx in (-1.0, 1.0):
        rail_x = rx + sx * 0.42
        mb.add_bevel_box(
            center_u=(rail_x, ry + 0.43, rz),
            size_u=(0.08, 0.055, 1.44),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(rail_x, ry + 0.56, rz),
            size_u=(0.075, 0.048, 1.42),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        for z_post in (-0.66, -0.22, 0.22, 0.66):
            mb.add_bevel_box(
                center_u=(rail_x, ry + 0.495, rz + z_post),
                size_u=(0.072, 0.09, 0.08),
                mat="R1_CreamEnamel",
                bevel=0.006,
                segments=1,
            )
    # Front and rear cream bumper crossbeams + internal dark-metal cross ribs
    for z_end in (-0.68, 0.68):
        mb.add_bevel_box(
            center_u=(rx, ry + 0.49, rz + z_end),
            size_u=(0.90, 0.14, 0.09),
            mat="R1_CreamEnamel",
            bevel=0.010,
            segments=1,
        )
    for z_rib in (-0.34, 0.0, 0.34):
        mb.add_bevel_box(
            center_u=(rx, ry + 0.44, rz + z_rib),
            size_u=(0.76, 0.05, 0.06),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )

    obj = mb.build_object()
    obj.rotation_euler = (0.0, 0.0, math.radians(-yaw_deg_u))
    r1kit._init_solid_vcol(obj, (0.92, 0.08, 0.0, 1.0))
    return obj


def _setup_lunar_night_world(world: bpy.types.World) -> None:
    """Configure a deep lunar night sky with subtle procedural star dots matching s_vehicle_pad.png."""
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
    """Create a cratered/pebbled lunar regolith material matching the dusty ground in s_vehicle_pad.png."""
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


def render_vehicle_pad_compare(root: bpy.types.Object, out_dir: Path, r2_dir: Path) -> None:
    """Render side-by-side concept comparison (`Logs/r2/compare_vehicle_pad.png`) against `s_vehicle_pad.png`."""
    d = VEHICLE_PAD_DIMS
    out_dir = Path(out_dir).resolve()
    r2_dir = Path(r2_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    r2_dir.mkdir(parents=True, exist_ok=True)

    r1kit.ensure_materials(use_vertex_color_shading=True)
    lamp_mat = bpy.data.materials.get("R1_LampAmber")
    if lamp_mat is not None and lamp_mat.node_tree is not None:
        for node in lamp_mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED" and "Emission Strength" in node.inputs:
                node.inputs["Emission Strength"].default_value = 9.0

    pose_vehicle_pad_active(root)
    bpy.context.view_layer.update()

    cleanup_objs: list[bpy.types.Object] = []
    cleanup_meshes: list[bpy.types.Mesh] = []
    cleanup_lights: list[bpy.types.Light] = []
    cleanup_cams: list[bpy.types.Camera] = []
    cleanup_mats: list[bpy.types.Material] = []

    # 1. Import the real 07 v3 model (`RoverModel.fbx`) on the foreground-left (+X_u = 1.82, +Z_u = 1.35) facing the pad curb
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

        rover_dir = str(THIS_DIR / "rover")
        if rover_dir not in sys.path:
            sys.path.insert(0, rover_dir)
        try:
            from rover import rover_parts  # type: ignore
        except Exception:
            rover_parts = None

        for obj in rover_objs:
            obj.animation_data_clear()
            if obj.parent is None:
                # RoverModel.fbx imports upright on its 4 wheels with Screen, CutterTurret, and DigScoop facing +Y_b (+Z_u);
                # rotating +74 deg around world Z turns 07's front (+Y_b) toward (-X_b, +Y_b) (front-right toward the pad curb).
                obj.rotation_mode = "XYZ"
                obj.rotation_euler = (0.0, 0.0, math.radians(74.0))
                obj.location = r1kit.u_pos(1.82, 0.0, 1.35)
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

    # 2. Build the 6-wheeled open-frame vehicle chassis suspended beneath the lifting spreader frame (`HoistHook`)
    chassis_obj = _build_suspended_chassis_standin(pos_u=(0.06, 0.44, 0.02), yaw_deg_u=16.0)
    cleanup_objs.append(chassis_obj)
    if chassis_obj.data is not None:
        cleanup_meshes.append(chassis_obj.data)

    # 3. Build cratered lunar regolith ground with undulating distant ridge behind the pad matching s_vehicle_pad.png
    bm_g = bmesh.new()
    grid_n = 48
    grid_ext = 20.0
    verts_grid: list[list[bmesh.types.BMVert]] = []
    for iy in range(grid_n + 1):
        row: list[bmesh.types.BMVert] = []
        v_frac = iy / grid_n
        y_b = 9.0 - v_frac * 27.0
        for ix in range(grid_n + 1):
            u_frac = ix / grid_n
            x_b = -grid_ext + u_frac * (2.0 * grid_ext)
            dist_pad = math.hypot(x_b, y_b)
            ridge = 0.0
            if y_b < -2.4:
                ridge = (
                    0.54 * math.sin((-y_b - 2.4) * 0.24)
                    + 0.16 * math.cos(x_b * 0.46)
                    + 0.08 * math.sin(x_b * 1.2 - y_b * 0.85)
                )
            undul = 0.025 * math.sin(x_b * 1.8) * math.cos(y_b * 1.6) * min(1.0, max(0.0, (dist_pad - 2.2) / 2.2))
            z_b = max(-0.02, ridge + undul) if dist_pad > 2.2 else 0.0
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

    # 4. Night sky & warm key lighting matching s_vehicle_pad.png
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

    # Warm directional key light from front-left-above (+X_u, +Y_u, +Z_u)
    _add_light_u("_CompareSunKey", "SUN", (3.8, 4.6, 5.4), (0.0, 0.8, 0.0), energy=3.5, color=(1.0, 0.90, 0.74), size=4.0)
    # Soft cool night sky fill
    _add_light_u("_CompareSkyFill", "SUN", (-4.4, 3.2, 3.6), (0.0, 0.8, 0.0), energy=0.65, color=(0.58, 0.70, 0.96), size=15.0)
    # Warm amber spotlight + glow positioned just below the rim of the hanging boom dome lamp (`BoomLamp`)
    _add_light_u(
        "_CompareBoomSpot",
        "SPOT",
        (d["boom_lamp_x"], d["boom_lamp_y"] - 0.22, d["boom_lamp_z"]),
        (0.06, 0.35, 0.02),
        energy=195.0,
        color=(1.0, 0.70, 0.26),
        size=0.18,
        spot_deg=86.0,
    )
    _add_light_u(
        "_CompareBoomGlow",
        "POINT",
        (d["boom_lamp_x"], d["boom_lamp_y"] - 0.24, d["boom_lamp_z"]),
        (0.0, 0.0, 0.0),
        energy=32.0,
        color=(1.0, 0.72, 0.28),
        size=0.14,
    )
    # Warm welding spark point light between 07 and the front-left octagonal curb
    _add_light_u(
        "_CompareWeldSpark",
        "POINT",
        (1.05, 0.44, 1.38),
        (0.85, 0.26, 1.25),
        energy=22.0,
        color=(1.0, 0.76, 0.32),
        size=0.06,
    )

    # 5. Perspective camera matching s_vehicle_pad.png (1376 x 768, 35 mm focal length)
    # Placed at (-X_u, +Y_u, +Z_u) so:
    #   - RampLip (+Z_u) is at the foreground bottom-left
    #   - 07 (+X_u, +Z_u) is at the foreground left facing the pad
    #   - Crane column (-X_u, -Z_u) is on the right with its jib reaching left across the frame in full profile!
    cam_data = bpy.data.cameras.new("_CompareCam")
    cam_data.sensor_width = 36.0
    cam_obj = bpy.data.objects.new("_CompareCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    cleanup_objs.append(cam_obj)
    cleanup_cams.append(cam_data)

    old_cam = scene.camera
    scene.camera = cam_obj
    r1kit._aim_camera_at(
        cam_obj,
        cam_pos_u=(-2.42, 2.46, 5.78),
        target_u=(0.28, 0.80, 0.30),
        lens_mm=35.0,
    )

    render_alone_path = r2_dir / "vehicle_pad_concept_view.png"
    r1kit._render_to_file(render_alone_path, res_x=1376, res_y=768, freestyle=True)

    if CONCEPT_IMAGE_PATH.exists():
        for dst in (r2_dir / "compare_vehicle_pad.png", out_dir / "compare_vehicle_pad.png"):
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

    pose_vehicle_pad_rest(root)
    bpy.context.view_layer.update()
    r1kit.ensure_materials(use_vertex_color_shading=False)


def build_vehicle_pad() -> bpy.types.Object:
    """Build the complete VehiclePad hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("VehiclePad", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_vehicle_pad_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=VEHICLE_PAD_DIMS["footprint_width"] * 0.56)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build VehiclePad FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/VehiclePad.fbx"),
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
    root = build_vehicle_pad()
    tris = r1kit.count_triangles(root)
    print(f"[VehiclePad] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        render_vehicle_pad_compare(root, args.renders_dir, args.r2_dir)
        r1kit.render_structure_suite(
            root=root,
            slug="vehicle_pad",
            out_dir=args.renders_dir,
            target_height=VEHICLE_PAD_DIMS["total_height"],
            footprint_radius=VEHICLE_PAD_DIMS["footprint_width"] * 0.56,
            pose_rest_fn=pose_vehicle_pad_rest,
            pose_active_fn=pose_vehicle_pad_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
