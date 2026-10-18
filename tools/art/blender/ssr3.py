"""Procedural Blender 5.1 generator for the R1 SSR-3 elder rover family (`tools/art/blender/ssr3.py`).

Deliverable 1:
  - `SSR3_Chassis.fbx` (`Assets/_Project/Art/R1/Models/Rover/SSR3_Chassis.fbx`)
    Shared older 1978 ILC utility cart chassis (~1.40 m L x 0.95 m W x 0.75 m H to deck):
      * Brake-bent R1_CreamEnamel sheet-metal body with chamfered upper shoulders, panel seams, and rivets
      * Continuous R1_OrangeStripe beltline with `ILC · SSR-3` flank stencil and `Unit_Number` decal slot
      * 4 small wheels (`Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`) in 07's 8-bolt deep-dish chevron language
      * Single off-center front headlamp (`Headlamp` + `HeadlampGlass` in dark `R1_DarkGlass`) and dark `CELL 0%` strip
      * No CRT face, no cartoon eyes, no hands, no active emissive materials
      * `Topper_Socket` empty on the recessed dorsal deck
      * Sun-bleached, dust-drifted vertex-colour weathering (`Col`: R=AO, G=edge wear, B=drifted regolith dust)
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Sequence

import bpy
import numpy as np

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

# ---------------------------------------------------------------------------
# SSR-3 Chassis Envelope & Rig Constants (Unity coordinates: +X right, +Y up, +Z forward)
# Target envelope: ~1.40 m L (Z) x 0.95 m W (X) x 0.75 m H (Y) to deck
# ---------------------------------------------------------------------------
CHASSIS_WIDTH_X: float = 0.95
CHASSIS_LENGTH_Z: float = 1.40
CHASSIS_DECK_HEIGHT_Y: float = 0.75

WHEEL_RADIUS: float = 0.215
WHEEL_WIDTH: float = 0.19
WHEEL_TRACK_X: float = 0.38  # Outer tyre face at +/- (0.38 + 0.095) = +/- 0.475 m -> 0.95 m total width
WHEEL_BASE_Z: float = 0.44   # Front/rear axles at z = +/- 0.44 m

TOPPER_SOCKET_PIVOT_U: tuple[float, float, float] = (0.0, 0.69, 0.0)
UNIT_NUMBER_PIVOT_U: tuple[float, float, float] = (0.0, 0.505, 0.26)
# Single front headlamp on viewer-left of front nose (+X = +0.255 m in Unity coords)
HEADLAMP_PIVOT_U: tuple[float, float, float] = (0.255, 0.505, 0.672)


# ---------------------------------------------------------------------------
# Minimalist 3D Stencil Glyph Helper (clean relief bars in Unity coords)
# Each glyph is defined in normalized [0..1] width x [0..1] height as rectangles:
# (x_center, y_center, width, height)
# ---------------------------------------------------------------------------
GLYPH_RECTS: dict[str, Sequence[tuple[float, float, float, float]]] = {
    "I": (
        (0.50, 0.50, 0.26, 1.00),
        (0.50, 0.90, 0.76, 0.20),
        (0.50, 0.10, 0.76, 0.20),
    ),
    "L": (
        (0.22, 0.50, 0.24, 1.00),
        (0.54, 0.10, 0.84, 0.20),
    ),
    "C": (
        (0.20, 0.50, 0.24, 0.84),
        (0.54, 0.90, 0.76, 0.20),
        (0.54, 0.10, 0.76, 0.20),
    ),
    "E": (
        (0.20, 0.50, 0.24, 1.00),
        (0.54, 0.90, 0.78, 0.20),
        (0.48, 0.50, 0.66, 0.18),
        (0.54, 0.10, 0.78, 0.20),
    ),
    "S": (
        (0.50, 0.90, 0.82, 0.20),
        (0.20, 0.70, 0.24, 0.44),
        (0.50, 0.50, 0.82, 0.18),
        (0.80, 0.30, 0.24, 0.44),
        (0.50, 0.10, 0.82, 0.20),
    ),
    "R": (
        (0.20, 0.50, 0.24, 1.00),
        (0.52, 0.90, 0.76, 0.20),
        (0.80, 0.72, 0.24, 0.42),
        (0.52, 0.52, 0.76, 0.18),
        (0.74, 0.26, 0.24, 0.52),
    ),
    "-": (
        (0.50, 0.50, 0.72, 0.20),
    ),
    "·": (
        (0.50, 0.50, 0.30, 0.24),
    ),
    "0": (
        (0.18, 0.50, 0.22, 0.88),
        (0.82, 0.50, 0.22, 0.88),
        (0.50, 0.90, 0.76, 0.20),
        (0.50, 0.10, 0.76, 0.20),
    ),
    "3": (
        (0.50, 0.90, 0.82, 0.20),
        (0.82, 0.50, 0.22, 0.88),
        (0.54, 0.50, 0.68, 0.18),
        (0.50, 0.10, 0.82, 0.20),
    ),
    "4": (
        (0.18, 0.68, 0.22, 0.52),
        (0.50, 0.44, 0.82, 0.18),
        (0.74, 0.50, 0.22, 0.88),
    ),
    "5": (
        (0.50, 0.90, 0.78, 0.20),
        (0.18, 0.70, 0.22, 0.48),
        (0.50, 0.50, 0.78, 0.18),
        (0.82, 0.30, 0.22, 0.48),
        (0.50, 0.10, 0.78, 0.20),
    ),
    "6": (
        (0.18, 0.50, 0.22, 0.88),
        (0.50, 0.90, 0.76, 0.20),
        (0.50, 0.50, 0.76, 0.18),
        (0.82, 0.30, 0.22, 0.48),
        (0.50, 0.10, 0.76, 0.20),
    ),
    "%": (
        (0.22, 0.80, 0.28, 0.26),
        (0.26, 0.20, 0.22, 0.24),
        (0.50, 0.50, 0.22, 0.24),
        (0.74, 0.80, 0.22, 0.24),
        (0.78, 0.20, 0.28, 0.26),
    ),
}


def _add_stencil_text_flank(
    mb: r1kit.MeshBuilder,
    text: str,
    side_x: float,
    center_y: float,
    center_z: float,
    char_w: float,
    char_h: float,
    relief_x: float,
    mat: str,
) -> None:
    """Append 3D stencil characters along a left (-X) or right (+X) flank panel."""
    sx = 1.0 if side_x >= 0.0 else -1.0
    dir_z = 1.0 if sx > 0.0 else -1.0
    step = char_w * 1.28
    chars = list(text)
    total_span = step * max(0, len(chars) - 1)
    start_z = center_z - dir_z * (total_span * 0.5)

    for idx, ch in enumerate(chars):
        rects = GLYPH_RECTS.get(ch)
        if not rects:
            continue
        cz_char = start_z + dir_z * (idx * step)
        for rx, ry, rw, rh in rects:
            local_dz = (rx - 0.5) * char_w * dir_z
            local_dy = (ry - 0.5) * char_h
            mb.add_bevel_box(
                center_u=(side_x, center_y + local_dy, cz_char + local_dz),
                size_u=(relief_x, rh * char_h, rw * char_w),
                mat=mat,
                bevel=0.0,
                segments=0,
            )


def _add_stencil_text_front(
    mb: r1kit.MeshBuilder,
    text: str,
    center_x: float,
    center_y: float,
    front_z: float,
    char_w: float,
    char_h: float,
    relief_z: float,
    mat: str,
) -> None:
    """Append 3D stencil characters across a front (+Z) panel, reading left-to-right when viewed from +Z."""
    step = char_w * 1.26
    chars = list(text)
    total_span = step * max(0, len(chars) - 1)
    start_x = center_x + total_span * 0.5

    for idx, ch in enumerate(chars):
        rects = GLYPH_RECTS.get(ch)
        if not rects:
            continue
        cx_char = start_x - idx * step
        for rx, ry, rw, rh in rects:
            local_dx = -(rx - 0.5) * char_w
            local_dy = (ry - 0.5) * char_h
            mb.add_bevel_box(
                center_u=(cx_char + local_dx, center_y + local_dy, front_z),
                size_u=(rw * char_w, rh * char_h, relief_z),
                mat=mat,
                bevel=0.0,
                segments=0,
            )


# ---------------------------------------------------------------------------
# Wheel Builder (07's 8-bolt deep-dish + 3D chevron lug language, scaled for SSR-3)
# ---------------------------------------------------------------------------
def _build_ssr3_wheel(
    root: bpy.types.Object,
    name: str,
    pivot_u: tuple[float, float, float],
    dish_mat: str = "R1_CreamEnamel",
) -> bpy.types.Object:
    """Build a small SSR-3 off-road wheel (R = 0.215 m) with recessed cream/sage 8-bolt hub dish."""
    import bmesh

    px, py, pz = pivot_u
    sx = 1.0 if px >= 0.0 else -1.0
    mb = r1kit.MeshBuilder(name, pivot_u=pivot_u)

    r_outer = WHEEL_RADIUS          # 0.215 m
    lug_h = 0.009
    r_carcass = r_outer - lug_h     # 0.206 m
    r_rim = 0.125
    tyre_core_w = 0.145

    # 1. Rubber tyre carcass & plump torus sidewalls so the central wheel hub recess is open
    mb.add_cylinder(
        center_u=(px, py, pz),
        radius=r_carcass,
        length=tyre_core_w,
        mat="R1_Rubber",
        axis_u="X",
        segments=24,
        bevel=0.012,
        bevel_segments=1,
    )
    for side_s in (-1.0, 1.0):
        mb.add_torus(
            center_u=(px + side_s * 0.052, py, pz),
            major_radius=0.160,
            minor_radius=0.043,
            mat="R1_Rubber",
            axis_u="X",
            major_segments=24,
            minor_segments=8,
        )

    # 2. 14 paired (28 total) cylindrical-hugging 3D chevron tread lug blocks around the tyre crown
    num_lugs = 14
    lug_arc_half = (2.0 * math.pi / num_lugs) * 0.27
    chev_dth = 0.13
    x_inner = 0.008
    x_outer = 0.082
    r_root = r_carcass - 0.005

    all_lug_faces = []
    for i in range(num_lugs):
        base_theta = (2.0 * math.pi * i) / num_lugs
        for pair_s in (-1.0, 1.0):
            theta_c = base_theta + (math.pi / num_lugs if pair_s < 0.0 else 0.0)
            pts_cyl = (
                # Bottom ring sunk into tyre carcass
                (px + pair_s * x_inner, theta_c - lug_arc_half + 0.02, r_root),
                (px + pair_s * x_inner, theta_c + lug_arc_half + 0.02, r_root),
                (px + pair_s * x_outer, theta_c + lug_arc_half - chev_dth, r_root * 0.96),
                (px + pair_s * x_outer, theta_c - lug_arc_half - chev_dth, r_root * 0.96),
                # Top crown ring at r_outer
                (px + pair_s * (x_inner + 0.004), theta_c - lug_arc_half * 0.78 + 0.02, r_outer),
                (px + pair_s * (x_inner + 0.004), theta_c + lug_arc_half * 0.78 + 0.02, r_outer),
                (px + pair_s * (x_outer - 0.005), theta_c + lug_arc_half * 0.78 - chev_dth, r_outer - 0.002),
                (px + pair_s * (x_outer - 0.005), theta_c - lug_arc_half * 0.78 - chev_dth, r_outer - 0.002),
            )
            l_verts = [
                mb.bm.verts.new(
                    mb._to_local_b((ux, py + rad * math.sin(th), pz + rad * math.cos(th)))
                )
                for ux, th, rad in pts_cyl
            ]
            quads = (
                (3, 2, 1, 0),
                (4, 5, 6, 7),
                (0, 1, 5, 4),
                (1, 2, 6, 5),
                (2, 3, 7, 6),
                (3, 0, 4, 7),
            )
            for a, b, c, d in quads:
                all_lug_faces.append(mb.bm.faces.new((l_verts[a], l_verts[b], l_verts[c], l_verts[d])))

    bmesh.ops.recalc_face_normals(mb.bm, faces=all_lug_faces)
    mb._assign_faces(all_lug_faces, "R1_Rubber")

    # 3. Exposed deep-dish metal wheel hub (protruding past the inner cylinder cap at |dx| = 0.0725..0.092 m)
    mb.add_cylinder(
        center_u=(px + sx * 0.070, py, pz),
        radius=r_rim * 0.92,
        length=0.022,
        mat=dish_mat,
        axis_u="X",
        segments=20,
        bevel=0.004,
        bevel_segments=1,
    )
    mb.add_torus(
        center_u=(px + sx * 0.082, py, pz),
        major_radius=r_rim * 0.94,
        minor_radius=0.014,
        mat="R1_GreyMetal",
        axis_u="X",
        major_segments=20,
        minor_segments=6,
    )
    mb.add_torus(
        center_u=(px + sx * 0.080, py, pz),
        major_radius=r_rim * 0.68,
        minor_radius=0.015,
        mat=dish_mat,
        axis_u="X",
        major_segments=18,
        minor_segments=6,
    )
    mb.add_cylinder(
        center_u=(px + sx * 0.078, py, pz),
        radius=r_rim * 0.34,
        length=0.022,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=14,
        bevel=0.003,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(px + sx * 0.085, py, pz),
        radius=r_rim * 0.22,
        length=0.024,
        mat="R1_BareSteel",
        axis_u="X",
        segments=12,
        bevel=0.004,
        bevel_segments=1,
    )
    # Circle of 8 hex lug bolts clearly visible on the hub dish face
    bolt_x = px + sx * 0.083
    bolt_pts = [
        (
            bolt_x,
            py + r_rim * 0.48 * math.sin(2.0 * math.pi * b_i / 8),
            pz + r_rim * 0.48 * math.cos(2.0 * math.pi * b_i / 8),
        )
        for b_i in range(8)
    ]
    mb.add_bolts(
        bolt_pts,
        radius=0.008,
        height=0.012,
        axis_u="X",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # Inboard axle hub backing
    mb.add_cylinder(
        center_u=(px - sx * 0.068, py, pz),
        radius=r_rim * 0.85,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=14,
        bevel=0.004,
        bevel_segments=1,
    )

    return mb.build_object(parent=root)


# ---------------------------------------------------------------------------
# Headlamp Builder (Single off-center front headlamp — dark R1_DarkGlass)
# ---------------------------------------------------------------------------
def _build_ssr3_headlamp(root: bpy.types.Object) -> bpy.types.Object:
    """Build the single front headlamp (`Headlamp` + `HeadlampGlass` using dark `R1_DarkGlass`)."""
    hx, hy, hz = HEADLAMP_PIVOT_U
    hl_mb = r1kit.MeshBuilder("Headlamp", pivot_u=HEADLAMP_PIVOT_U)

    # Dark metal mounting collar recessed into the front orange stripe
    hl_mb.add_cylinder(
        center_u=(hx, hy, hz - 0.004),
        radius=0.068,
        length=0.022,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=20,
        bevel=0.004,
        bevel_segments=1,
    )
    # Turned R1_Brass outer bezel ring sized cleanly to the orange stripe height
    hl_mb.add_cylinder(
        center_u=(hx, hy, hz + 0.008),
        radius=0.062,
        radius_top=0.056,
        length=0.026,
        mat="R1_Brass",
        axis_u="Z",
        segments=20,
        bevel=0.005,
        bevel_segments=1,
    )
    hl_mb.add_torus(
        center_u=(hx, hy, hz + 0.019),
        major_radius=0.050,
        minor_radius=0.008,
        mat="R1_Brass",
        axis_u="Z",
        major_segments=20,
        minor_segments=6,
    )
    # Inner dark reflector cup collar
    hl_mb.add_cylinder(
        center_u=(hx, hy, hz + 0.012),
        radius=0.044,
        length=0.016,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=16,
        bevel=0.003,
        bevel_segments=1,
    )
    hl_obj = hl_mb.build_object(parent=root)

    # Child glass dome node — strictly dark R1_DarkGlass (no R1_LampAmber)
    glass_piv = (hx, hy, hz + 0.018)
    gl_mb = r1kit.MeshBuilder("HeadlampGlass", pivot_u=glass_piv)
    gl_mb.add_cylinder(
        center_u=(hx, hy, hz + 0.020),
        radius=0.043,
        radius_top=0.028,
        length=0.014,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=18,
        bevel=0.006,
        bevel_segments=2,
    )
    gl_mb.build_object(parent=hl_obj)
    return hl_obj


# ---------------------------------------------------------------------------
# Chassis Body Builder (Brake-bent 1978 boxy cart body + deck + suspension)
# ---------------------------------------------------------------------------
def _build_ssr3_body(root: bpy.types.Object) -> bpy.types.Object:
    """Build the brake-bent SSR-3 cart body (`Body`) with orange stripe, stencils, suspension, and dorsal deck."""
    mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.0, 0.0))

    # 1. Central lower belly skid pan & subframe between front and rear axles
    mb.add_bevel_box(
        center_u=(0.0, 0.255, 0.0),
        size_u=(0.64, 0.11, 1.18),
        mat="R1_GreyMetal",
        bevel=0.014,
        segments=1,
    )
    # Central lower rocker skirt between the front and rear wheel arches (z in [-0.14, +0.14])
    mb.add_bevel_box(
        center_u=(0.0, 0.305, 0.0),
        size_u=(0.86, 0.13, 0.28),
        mat="R1_CreamEnamel",
        bevel=0.012,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.245, 0.0),
        size_u=(0.87, 0.036, 0.27),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    # Side tie-down / tow loops & 3 lower ventilation slots on the central rocker skirt
    for sx in (-1.0, 1.0):
        mb.add_vent_grille(
            center_u=(sx * 0.428, 0.315, 0.0),
            width=0.16,
            height=0.054,
            depth=0.018,
            slat_count=3,
            rot_deg_u=(0.0, 90.0 * sx, 0.0),
            frame_mat="R1_GreyMetal",
            back_mat="R1_DarkGlass",
        )
        # Heavy forged tie-down U-loop beneath the central rocker sill
        mb.add_torus(
            center_u=(sx * 0.432, 0.232, 0.0),
            major_radius=0.026,
            minor_radius=0.0065,
            mat="R1_BareSteel",
            axis_u="Z",
            major_segments=12,
            minor_segments=6,
        )

    # 2. Main brake-bent sheet-metal hull tub (`R1_CreamEnamel`)
    # Lower-mid hull above wheel arches (y = 0.38 .. 0.61 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.495, 0.0),
        size_u=(0.87, 0.23, 1.33),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=2,
    )
    # Upper chamfered brake-bent shoulder hull (y = 0.59 .. 0.725 m, tapered inward to 0.80 x 1.24 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.658, 0.0),
        size_u=(0.87, 0.135, 1.33),
        mat="R1_CreamEnamel",
        bevel=0.016,
        segments=2,
        top_scale_xz=(0.91, 0.93),
    )
    # Lower angled nose glacis & rear valence chamfers
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, 0.355, sz * 0.595),
            size_u=(0.84, 0.095, 0.14),
            mat="R1_CreamEnamel",
            bevel=0.012,
            segments=1,
            rot_deg_u=(sz * 22.0, 0.0, 0.0),
        )

    # 3. Recessed dorsal payload deck & raised perimeter coaming rim reaching exact Y = 0.750 m
    coam_y = 0.730
    coam_h = 0.040  # Top at 0.730 + 0.020 = 0.750 m
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.375, coam_y, 0.0),
            size_u=(0.048, coam_h, 1.22),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(sx * 0.348, coam_y - 0.004, 0.0),
            size_u=(0.016, coam_h - 0.006, 1.14),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
        )
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, coam_y, sz * 0.585),
            size_u=(0.798, coam_h, 0.050),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(0.0, coam_y - 0.004, sz * 0.556),
            size_u=(0.71, coam_h - 0.006, 0.016),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
        )
    # Recessed dorsal payload well floor (y = 0.69 m) with structural ribs & umbilical socket
    mb.add_bevel_box(
        center_u=(0.0, 0.682, 0.0),
        size_u=(0.70, 0.024, 1.12),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    for rz in (-0.42, -0.21, 0.0, 0.21, 0.42):
        mb.add_bevel_box(
            center_u=(0.0, 0.694, rz),
            size_u=(0.66, 0.012, 0.032),
            mat="R1_BareSteel",
            bevel=0.003,
            segments=1,
        )
    # Central dorsal umbilical connector ring & 4 corner topper alignment lugs
    mb.add_cylinder(
        center_u=(0.0, 0.698, 0.08),
        radius=0.056,
        length=0.022,
        mat="R1_Brass",
        axis_u="Y",
        segments=14,
        bevel=0.004,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.706, 0.08),
        radius=0.036,
        length=0.016,
        mat="R1_Copper",
        axis_u="Y",
        segments=12,
        bevel=0.003,
        bevel_segments=1,
    )
    for sx in (-0.30, 0.30):
        for sz in (-0.48, 0.48):
            mb.add_bevel_box(
                center_u=(sx, 0.704, sz),
                size_u=(0.052, 0.024, 0.052),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
            )

    # 4. Continuous warm coral-orange belt stripe (`R1_OrangeStripe`) wrapping front, flanks, and rear
    stripe_y = 0.505
    stripe_h = 0.112
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.436, stripe_y, 0.0),
            size_u=(0.012, stripe_h, 1.336),
            mat="R1_OrangeStripe",
            bevel=0.004,
            segments=1,
        )
        for dy in (-stripe_h * 0.52, stripe_h * 0.52):
            mb.add_bevel_box(
                center_u=(sx * 0.435, stripe_y + dy, 0.0),
                size_u=(0.009, 0.008, 1.334),
                mat="R1_GreyMetal",
                bevel=0.002,
                segments=1,
            )
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, stripe_y, sz * 0.666),
            size_u=(0.876, stripe_h, 0.012),
            mat="R1_OrangeStripe",
            bevel=0.004,
            segments=1,
        )

    # 5. Recessed front `CELL 0%` status strip beside the headlamp on the front orange band (dark, unlit)
    status_x = -0.075
    status_y = 0.505
    status_z = 0.672
    mb.add_bevel_box(
        center_u=(status_x, status_y, status_z),
        size_u=(0.36, 0.076, 0.018),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(status_x, status_y, status_z + 0.005),
        size_u=(0.33, 0.052, 0.012),
        mat="R1_DarkGlass",
        bevel=0.003,
        segments=1,
    )
    _add_stencil_text_front(
        mb,
        text="CELL 0%",
        center_x=status_x,
        center_y=status_y,
        front_z=status_z + 0.010,
        char_w=0.025,
        char_h=0.032,
        relief_z=0.004,
        mat="R1_Copper",
    )

    # 6. Flank `ILC · SSR-3` stencil lettering on the rear half of the orange beltline
    for sx in (-1.0, 1.0):
        _add_stencil_text_flank(
            mb,
            text="ILC · SSR-3",
            side_x=sx * 0.442,
            center_y=stripe_y,
            center_z=-0.20,
            char_w=0.036,
            char_h=0.052,
            relief_x=0.004,
            mat="R1_GreyMetal",
        )

    # 7. Brake-bent vertical panel seam channels, corner bolts/rivets, and right-flank R1_SagePatch repair plate
    for sx in (-1.0, 1.0):
        for seam_z in (-0.48, 0.04, 0.48):
            mb.add_bevel_box(
                center_u=(sx * 0.434, 0.56, seam_z),
                size_u=(0.008, 0.30, 0.009),
                mat="R1_GreyMetal",
                bevel=0.002,
                segments=1,
            )
        bolt_pts = [
            (sx * 0.436, 0.615, bz)
            for bz in (-0.58, -0.36, -0.14, 0.14, 0.36, 0.58)
        ]
        mb.add_bolts(
            bolt_pts,
            radius=0.007,
            height=0.008,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )
    # Front and rear vertical corner seam bolts (above and below the orange beltline, matching concept)
    for sz in (-1.0, 1.0):
        for sx in (-1.0, 1.0):
            mb.add_bolts(
                [
                    (sx * 0.39, 0.60, sz * 0.664),
                    (sx * 0.39, 0.41, sz * 0.664),
                ],
                radius=0.007,
                height=0.008,
                axis_u="Z",
                mat="R1_BareSteel",
                bevel_segments=0,
            )

    # Riveted sage-green service patch (`R1_SagePatch`) on the right-rear lower flank
    mb.add_bevel_box(
        center_u=(0.436, 0.425, -0.22),
        size_u=(0.012, 0.095, 0.22),
        mat="R1_SagePatch",
        bevel=0.004,
        segments=1,
    )
    mb.add_bolts(
        [
            (0.441, 0.460, -0.31),
            (0.441, 0.460, -0.13),
            (0.441, 0.390, -0.31),
            (0.441, 0.390, -0.13),
        ],
        radius=0.006,
        height=0.006,
        axis_u="X",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # 8. Trapezoidal wheel-arch flare lips & suspension struts/coil-over shocks at all 4 wheels
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            wz = sz * WHEEL_BASE_Z
            # Upper wheel-arch eyebrow lip (`R1_GreyMetal`)
            mb.add_bevel_box(
                center_u=(sx * 0.415, 0.435, wz),
                size_u=(0.075, 0.028, 0.36),
                mat="R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )
            # Angled front/rear trapezoidal fender skirts
            for end_s in (-1.0, 1.0):
                mb.add_bevel_box(
                    center_u=(sx * 0.415, 0.365, wz + end_s * 0.205),
                    size_u=(0.072, 0.14, 0.028),
                    mat="R1_GreyMetal",
                    bevel=0.005,
                    segments=1,
                    rot_deg_u=(end_s * 28.0, 0.0, 0.0),
                )
            # Recessed dark wheel-well backing liner
            mb.add_bevel_box(
                center_u=(sx * 0.315, 0.325, wz),
                size_u=(0.028, 0.20, 0.36),
                mat="R1_GreyMetal",
                bevel=0.004,
                segments=1,
            )
            # Suspension lower wishbone strut & vertical coil-over damper connecting hull to wheel axle
            mb.add_strut(
                p0_u=(sx * 0.26, 0.27, wz - sz * 0.06),
                p1_u=(sx * WHEEL_TRACK_X, WHEEL_RADIUS, wz),
                width=0.044,
                thickness=0.036,
                mat="R1_GreyMetal",
                bevel=0.005,
            )
            mb.add_cylinder(
                center_u=(sx * 0.325, 0.325, wz),
                radius=0.022,
                length=0.17,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=10,
                bevel=0.003,
                bevel_segments=1,
            )
            mb.add_cylinder(
                center_u=(sx * 0.325, 0.335, wz),
                radius=0.032,
                length=0.095,
                mat="R1_Copper",
                axis_u="Y",
                segments=10,
                bevel=0.004,
                bevel_segments=1,
            )

    # 9. Heavy front & rear dark-charcoal bumper bars (`z = +/- 0.675 m`, outer faces at `z = +/- 0.700 m`)
    for sz in (-1.0, 1.0):
        bz = sz * 0.674
        mb.add_bevel_box(
            center_u=(0.0, 0.305, bz),
            size_u=(0.88, 0.068, 0.052),
            mat="R1_GreyMetal",
            bevel=0.010,
            segments=1,
        )
        for sx in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.415, 0.305, bz + sz * 0.003),
                size_u=(0.075, 0.074, 0.056),
                mat="R1_Rubber",
                bevel=0.008,
                segments=1,
            )
            mb.add_bevel_box(
                center_u=(sx * 0.23, 0.305, bz + sz * 0.016),
                size_u=(0.038, 0.052, 0.032),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
            )
        mb.add_bolt_row(
            start_u=(-0.32, 0.305, bz + sz * 0.024),
            end_u=(0.32, 0.305, bz + sz * 0.024),
            count=6,
            radius=0.008,
            height=0.010,
            axis_u="Z",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # Rear unlit tail-lamp housings (dark R1_DarkGlass lenses in R1_Brass bezels)
    for sx in (-0.28, 0.28):
        mb.add_bevel_box(
            center_u=(sx, 0.505, -0.672),
            size_u=(0.092, 0.048, 0.018),
            mat="R1_Brass",
            bevel=0.004,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(sx, 0.505, -0.676),
            size_u=(0.074, 0.032, 0.014),
            mat="R1_DarkGlass",
            bevel=0.003,
            segments=1,
        )

    return mb.build_object(parent=root)


# ---------------------------------------------------------------------------
# Sun-Bleached & Dust-Drifted Weathering Vertex-Colour Post-Pass
# ---------------------------------------------------------------------------
def _apply_ssr3_decades_at_rest_weathering(root: bpy.types.Object, footprint_radius: float = 0.85) -> None:
    """Bake Cycles AO + enhance sun-bleached upper edge wear (G) and heavy drifted regolith dust (B)."""
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius, bevel_wear_width=0.024)

    mesh_objs = [o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and len(o.data.polygons) > 0]
    for obj in mesh_objs:
        me = obj.data
        ca = me.color_attributes.get(r1kit.VCOL_ATTRIBUTE_NAME)
        if ca is None:
            continue

        num_loops = len(me.loops)
        rgba = np.ones(num_loops * 4, dtype=np.float32)
        ca.data.foreach_get("color", rgba)
        rgba = rgba.reshape(-1, 4)

        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        loop_idx = 0
        for poly in me.polygons:
            wn = (rot_mat @ poly.normal).normalized()
            # In Blender space: +Z is up, +Y is forward
            up_dot = float(wn.z)
            for l_i in poly.loop_indices:
                v_idx = me.loops[l_i].vertex_index
                wp = world_mat @ me.vertices[v_idx].co
                h_z = float(wp.z)

                r_ao = float(rgba[loop_idx, 0])
                g_wear = float(rgba[loop_idx, 1])
                b_dust = float(rgba[loop_idx, 2])

                # Sun-bleached upper chamfers & coaming edges (boost G on upper surfaces and bevels)
                sun_bleach = max(0.0, min(1.0, (h_z - 0.45) / 0.30)) * max(0.0, up_dot + 0.25)
                g_out = float(np.clip(max(g_wear * 1.18, sun_bleach * 0.28 * r_ao), 0.0, 1.0))

                # Heavy dust-drifted regolith (decades at rest):
                # 1. Basal dust drift from ground up to z = 0.34 m (wheels, arches, bumpers, lower skirt)
                basal_drift = max(0.0, min(1.0, (0.34 - h_z) / 0.34)) * 0.72
                # 2. Settled lunar dust blanket on all upward-facing horizontal surfaces (deck, bumpers, hood)
                top_blanket = max(0.0, (up_dot - 0.35) / 0.65) * 0.64
                b_out = float(np.clip(max(b_dust * 1.25, basal_drift, top_blanket), 0.0, 1.0))

                rgba[loop_idx, 0] = r_ao
                rgba[loop_idx, 1] = g_out
                rgba[loop_idx, 2] = b_out
                rgba[loop_idx, 3] = 1.0
                loop_idx += 1

        ca.data.foreach_set("color", rgba.reshape(-1))
        me.update()


# ---------------------------------------------------------------------------
# Complete SSR3_Chassis Assembly
# ---------------------------------------------------------------------------
def build_ssr3_chassis() -> bpy.types.Object:
    """Build the complete `SSR3_Chassis` hierarchy in the active Blender scene."""
    root = r1kit.create_empty("SSR3_Chassis", pivot_u=(0.0, 0.0, 0.0))

    _build_ssr3_body(root)
    _build_ssr3_headlamp(root)

    # 4 small wheels in 07's wheel/hub language (cream enamel deep-dish hubs with 8 lug bolts)
    _build_ssr3_wheel(root, "Wheel_FL", (-WHEEL_TRACK_X, WHEEL_RADIUS, WHEEL_BASE_Z), dish_mat="R1_CreamEnamel")
    _build_ssr3_wheel(root, "Wheel_FR", (WHEEL_TRACK_X, WHEEL_RADIUS, WHEEL_BASE_Z), dish_mat="R1_CreamEnamel")
    _build_ssr3_wheel(root, "Wheel_RL", (-WHEEL_TRACK_X, WHEEL_RADIUS, -WHEEL_BASE_Z), dish_mat="R1_SagePatch")
    _build_ssr3_wheel(root, "Wheel_RR", (WHEEL_TRACK_X, WHEEL_RADIUS, -WHEEL_BASE_Z), dish_mat="R1_CreamEnamel")

    # Required sockets / slots on the chassis
    r1kit.create_empty("Topper_Socket", parent=root, pivot_u=TOPPER_SOCKET_PIVOT_U)
    r1kit.create_empty("Unit_Number", parent=root, pivot_u=UNIT_NUMBER_PIVOT_U)

    bpy.context.view_layer.update()
    _apply_ssr3_decades_at_rest_weathering(root, footprint_radius=0.88)
    return root


# ---------------------------------------------------------------------------
# Deliverable 2: SSR3_Topper_06 ("Relay Keeper" — 6 Battery Cells + 6 Dark Glass Tubes + Rear Cable Reel)
# ---------------------------------------------------------------------------
def _build_topper_06_battery_rack(root: bpy.types.Object) -> bpy.types.Object:
    """Build the 6-cell battery rack (`BatteryRack`) with heavy `R1_Copper` busbars and `R1_BareSteel` straps."""
    mb = r1kit.MeshBuilder("BatteryRack", pivot_u=(0.0, 0.0, 0.20))

    # 1. Base steel mounting tray sitting in the recessed dorsal deck well
    mb.add_bevel_box(
        center_u=(0.0, 0.016, 0.21),
        size_u=(0.61, 0.032, 0.56),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
    )
    mb.add_bolts(
        [
            (sx * 0.285, 0.032, cz)
            for sx in (-1.0, 1.0)
            for cz in (-0.03, 0.21, 0.45)
        ],
        radius=0.008,
        height=0.010,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # 2. 6 Rectangular Industrial Battery Cells (2 columns x 3 rows = 6 cells)
    cell_xs = (-0.15, 0.15)
    cell_zs = (0.39, 0.21, 0.03)
    for cx in cell_xs:
        sx = 1.0 if cx >= 0.0 else -1.0
        for cz in cell_zs:
            # Main cell housing (`R1_BareSteel` light industrial steel-grey casing)
            mb.add_bevel_box(
                center_u=(cx, 0.105, cz),
                size_u=(0.27, 0.16, 0.154),
                mat="R1_BareSteel",
                bevel=0.008,
                segments=1,
            )
            # Top recessed dark terminal cover plate (`R1_GreyMetal`)
            mb.add_bevel_box(
                center_u=(cx, 0.188, cz),
                size_u=(0.248, 0.014, 0.136),
                mat="R1_GreyMetal",
                bevel=0.004,
                segments=1,
            )
            # Outer flank vertical stiffening ribs (`R1_GreyMetal`)
            for dz in (-0.042, 0.042):
                mb.add_bevel_box(
                    center_u=(cx + sx * 0.136, 0.105, cz + dz),
                    size_u=(0.008, 0.135, 0.016),
                    mat="R1_GreyMetal",
                    bevel=0.002,
                    segments=1,
                )

    # Perimeter steel angle-iron hold-down strap frame wrapping the 6-cell block
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.292, 0.155, 0.21),
            size_u=(0.014, 0.024, 0.54),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
        )
    for sz_pos in (-0.055, 0.475):
        mb.add_bevel_box(
            center_u=(0.0, 0.155, sz_pos),
            size_u=(0.59, 0.024, 0.014),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
        )

    # 3. Heavy `R1_Copper` Busbars & Tie-Down Straps across the front and top of the 6 cells
    for cx in cell_xs:
        # Front vertical copper busbar strap rising up the front of the battery pack
        mb.add_bevel_box(
            center_u=(cx, 0.105, 0.478),
            size_u=(0.048, 0.174, 0.020),
            mat="R1_Copper",
            bevel=0.005,
            segments=1,
        )
        # Top arched copper elbow block over the front edge
        mb.add_bevel_box(
            center_u=(cx, 0.194, 0.468),
            size_u=(0.050, 0.024, 0.036),
            mat="R1_Copper",
            bevel=0.005,
            segments=1,
        )
        # Longitudinal top copper busbar running across all 3 cell rows
        mb.add_bevel_box(
            center_u=(cx, 0.198, 0.21),
            size_u=(0.044, 0.016, 0.51),
            mat="R1_Copper",
            bevel=0.004,
            segments=1,
        )
        # Brass terminal post clamps at each cell row
        for cz in cell_zs:
            mb.add_cylinder(
                center_u=(cx, 0.207, cz),
                radius=0.018,
                length=0.015,
                mat="R1_Brass",
                axis_u="Y",
                segments=12,
                bevel=0.002,
                bevel_segments=1,
            )

    # Transverse copper inter-cell jumper bars & rear power feed busbar into the cable reel
    for cz in cell_zs:
        mb.add_bevel_box(
            center_u=(0.0, 0.204, cz),
            size_u=(0.34, 0.014, 0.030),
            mat="R1_Copper",
            bevel=0.003,
            segments=1,
        )
    mb.add_bevel_box(
        center_u=(0.0, 0.145, -0.085),
        size_u=(0.048, 0.018, 0.095),
        mat="R1_Copper",
        bevel=0.003,
        segments=1,
    )

    return mb.build_object(parent=root)


def _build_topper_06_cell_tubes(root: bpy.types.Object) -> bpy.types.Object:
    """Build the 6 dark glass cell status tubes (`CellTubes`) in `R1_DarkGlass` & `R1_Brass` (zero `R1_LampAmber`)."""
    mb = r1kit.MeshBuilder("CellTubes", pivot_u=(0.0, 0.195, 0.22))

    # 6 dark glass status tubes mounted atop the 6 battery cells (staggered slightly so all 6 domes read in 3/4 view)
    tube_coords = (
        (-0.088, 0.40),
        (0.088, 0.37),
        (-0.088, 0.22),
        (0.088, 0.19),
        (-0.088, 0.04),
        (0.088, 0.01),
    )
    for tx, tz in tube_coords:
        # Turned R1_Brass base collar ring
        mb.add_cylinder(
            center_u=(tx, 0.206, tz),
            radius=0.036,
            length=0.024,
            mat="R1_Brass",
            axis_u="Y",
            segments=16,
            bevel=0.003,
            bevel_segments=1,
        )
        mb.add_torus(
            center_u=(tx, 0.218, tz),
            major_radius=0.034,
            minor_radius=0.005,
            mat="R1_Brass",
            axis_u="Y",
            major_segments=16,
            minor_segments=6,
        )
        # Internal unlit copper electrode post at base of tube
        mb.add_cylinder(
            center_u=(tx, 0.232, tz),
            radius=0.012,
            length=0.024,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
            bevel=0.002,
            bevel_segments=1,
        )
        # Tall cylindrical R1_DarkGlass vacuum status tube envelope
        mb.add_cylinder(
            center_u=(tx, 0.272, tz),
            radius=0.031,
            length=0.096,
            mat="R1_DarkGlass",
            axis_u="Y",
            segments=16,
            bevel=0.004,
            bevel_segments=1,
        )
        # Domed R1_DarkGlass top cap
        mb.add_cylinder(
            center_u=(tx, 0.326, tz),
            radius=0.031,
            radius_top=0.016,
            length=0.022,
            mat="R1_DarkGlass",
            axis_u="Y",
            segments=16,
            bevel=0.008,
            bevel_segments=2,
        )
        # Top R1_Brass finial cap button
        mb.add_cylinder(
            center_u=(tx, 0.340, tz),
            radius=0.012,
            length=0.008,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.002,
            bevel_segments=1,
        )

    return mb.build_object(parent=root)


def _build_topper_06_cable_reel(root: bpy.types.Object) -> bpy.types.Object:
    """Build the rear flanged cable reel (`CableReel`) in `R1_SagePatch`/`R1_GreyMetal` with wound `R1_Rubber` cable."""
    mb = r1kit.MeshBuilder("CableReel", pivot_u=(0.0, 0.195, -0.31))
    reel_y = 0.195
    reel_z = -0.31

    # 1. Base mounting skid plate on the rear dorsal deck (`R1_GreyMetal`)
    mb.add_bevel_box(
        center_u=(0.0, 0.016, reel_z),
        size_u=(0.58, 0.032, 0.35),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )

    # 2. Twin A-frame trunnion pedestals (`R1_SagePatch`) & axle journals (`R1_GreyMetal` / `R1_BareSteel`)
    for sx in (-1.0, 1.0):
        px = sx * 0.245
        for leg_z in (reel_z - 0.115, reel_z + 0.115):
            mb.add_strut(
                p0_u=(px, 0.024, leg_z),
                p1_u=(px, reel_y, reel_z),
                width=0.034,
                thickness=0.042,
                mat="R1_SagePatch",
                bevel=0.004,
            )
        mb.add_bevel_box(
            center_u=(px, 0.105, reel_z),
            size_u=(0.024, 0.145, 0.12),
            mat="R1_SagePatch",
            bevel=0.004,
            segments=1,
        )
        mb.add_cylinder(
            center_u=(sx * 0.252, reel_y, reel_z),
            radius=0.042,
            length=0.038,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=16,
            bevel=0.004,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sx * 0.266, reel_y, reel_z),
            radius=0.024,
            length=0.024,
            mat="R1_BareSteel",
            axis_u="X",
            segments=12,
            bevel=0.003,
            bevel_segments=1,
        )

    # 3. Left & Right Circular Reel Flanges (`R1_SagePatch` disc + `R1_GreyMetal` rolled rim + 6 hub bolts)
    for sx in (-1.0, 1.0):
        fx = sx * 0.202
        mb.add_cylinder(
            center_u=(fx, reel_y, reel_z),
            radius=0.182,
            length=0.018,
            mat="R1_SagePatch",
            axis_u="X",
            segments=24,
            bevel=0.004,
            bevel_segments=1,
        )
        mb.add_torus(
            center_u=(fx, reel_y, reel_z),
            major_radius=0.180,
            minor_radius=0.011,
            mat="R1_GreyMetal",
            axis_u="X",
            major_segments=24,
            minor_segments=6,
        )
        mb.add_cylinder(
            center_u=(fx + sx * 0.005, reel_y, reel_z),
            radius=0.068,
            length=0.022,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=16,
            bevel=0.003,
            bevel_segments=1,
        )
        bolt_pts = [
            (
                fx + sx * 0.014,
                reel_y + 0.115 * math.sin(2.0 * math.pi * b_i / 6),
                reel_z + 0.115 * math.cos(2.0 * math.pi * b_i / 6),
            )
            for b_i in range(6)
        ]
        mb.add_bolts(
            bolt_pts,
            radius=0.008,
            height=0.010,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # 4. Wound `R1_Rubber` Cable Drum + 9 distinct wound rubber cable coils + rear brass coupler plug
    mb.add_cylinder(
        center_u=(0.0, reel_y, reel_z),
        radius=0.132,
        length=0.38,
        mat="R1_Rubber",
        axis_u="X",
        segments=20,
        bevel=0.006,
        bevel_segments=1,
    )
    for coil_i in range(9):
        cx = -0.168 + coil_i * 0.042
        mb.add_torus(
            center_u=(cx, reel_y, reel_z),
            major_radius=0.130,
            minor_radius=0.021,
            mat="R1_Rubber",
            axis_u="X",
            major_segments=20,
            minor_segments=8,
        )

    # Rear fairlead roller guide & stowed brass umbilical connector plug
    mb.add_bevel_box(
        center_u=(0.0, 0.055, -0.465),
        size_u=(0.14, 0.048, 0.042),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.055, -0.485),
        radius=0.024,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=14,
        bevel=0.003,
        bevel_segments=1,
    )

    return mb.build_object(parent=root)


def build_ssr3_topper_06() -> bpy.types.Object:
    """Build the complete `SSR3_Topper_06` ("Relay Keeper") hierarchy in the active Blender scene."""
    root = r1kit.create_empty("SSR3_Topper_06", pivot_u=(0.0, 0.0, 0.0))

    _build_topper_06_battery_rack(root)
    _build_topper_06_cell_tubes(root)
    _build_topper_06_cable_reel(root)

    bpy.context.view_layer.update()
    _apply_ssr3_decades_at_rest_weathering(root, footprint_radius=0.62)
    return root


def _build_unit_06_stencil(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the `Unit_Number_06` relief stencil (`06`) on both flanks at `UNIT_NUMBER_PIVOT_U`."""
    mb = r1kit.MeshBuilder("Unit_Number_06", pivot_u=UNIT_NUMBER_PIVOT_U)
    for sx in (-1.0, 1.0):
        _add_stencil_text_flank(
            mb,
            text="06",
            side_x=sx * 0.442,
            center_y=UNIT_NUMBER_PIVOT_U[1],
            center_z=UNIT_NUMBER_PIVOT_U[2],
            char_w=0.042,
            char_h=0.068,
            relief_x=0.005,
            mat="R1_GreyMetal",
        )
    return mb.build_object(parent=parent)


def build_elder06_combined() -> bpy.types.Object:
    """Build the combined Elder 06 assembly (`SSR3_Chassis` + `SSR3_Topper_06` + `06` stencil) for preview rendering."""
    chassis_root = build_ssr3_chassis()
    topper_socket = next((o for o in chassis_root.children if o.name == "Topper_Socket"), chassis_root)
    unit_num_socket = next((o for o in chassis_root.children if o.name == "Unit_Number"), chassis_root)

    topper_root = build_ssr3_topper_06()
    topper_root.parent = topper_socket
    topper_root.location = (0.0, 0.0, 0.0)

    _build_unit_06_stencil(unit_num_socket)
    bpy.context.view_layer.update()
    _apply_ssr3_decades_at_rest_weathering(chassis_root, footprint_radius=0.88)
    return chassis_root


# ---------------------------------------------------------------------------
# Shared Helical Coil Helper (local to MeshBuilder pivot)
# ---------------------------------------------------------------------------
def _add_helical_coil_u(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    coil_radius: float,
    wire_radius: float,
    length: float,
    turns: float,
    axis_u: str,
    mat: str,
    segs_per_turn: int = 16,
    wire_segs: int = 8,
    rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> list:
    """Append a closed 3D helical coil in MeshBuilder local space."""
    local_center_u = (
        center_u[0] - mb.pivot_u[0],
        center_u[1] - mb.pivot_u[1],
        center_u[2] - mb.pivot_u[2],
    )
    rot_rad = (
        math.radians(rot_deg_u[0]),
        math.radians(rot_deg_u[1]),
        math.radians(rot_deg_u[2]),
    )
    return r1kit.append_helical_coil(
        mb.bm,
        coil_radius=coil_radius,
        wire_radius=wire_radius,
        length=length,
        turns=turns,
        axis=axis_u,
        center_xyz=local_center_u,
        segs_per_turn=segs_per_turn,
        wire_segs=wire_segs,
        mat_idx=mb._mat_index(mat),
        rot_euler=rot_rad,
    )


# ---------------------------------------------------------------------------
# Deliverable 1 (agy-ssr3b): SSR3_Topper_05 ("Dish Tender" — Tall Copper Coil
# Mast on a Turntable with Two Small Horn Feeds, Folded Half-Down, Sun-Bleached)
# ---------------------------------------------------------------------------
TOPPER_05_HINGE_U: tuple[float, float, float] = (0.0, 0.155, -0.02)
TOPPER_05_FOLD_DEG: float = 50.0  # Folded half-down forward (+Z) over the dorsal deck


def _rotate_bm_around_hinge_x(mb: r1kit.MeshBuilder, hinge_u: tuple[float, float, float], fold_deg: float) -> None:
    """Rotate all current vertices in `mb.bm` forward (+Z in Unity) around `hinge_u` by `fold_deg` degrees."""
    import bmesh
    from mathutils import Matrix

    rot_mat = Matrix.Rotation(-math.radians(fold_deg), 3, "X")
    bmesh.ops.rotate(
        mb.bm,
        verts=list(mb.bm.verts),
        cent=mb._to_local_b(hinge_u),
        matrix=rot_mat,
    )


def _build_topper_05_turntable(root: bpy.types.Object) -> bpy.types.Object:
    """Build the rotary azimuth turntable (`Turntable`) with degree ring, hinge trunnions, and folding jack strut."""
    mb = r1kit.MeshBuilder("Turntable", pivot_u=(0.0, 0.06, 0.04))
    hx, hy, hz = TOPPER_05_HINGE_U

    # 1. Heavy steel deck baseplate bolted into the dorsal payload well
    mb.add_bevel_box(
        center_u=(0.0, 0.016, 0.08),
        size_u=(0.58, 0.032, 0.56),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
    )
    mb.add_bolts(
        [
            (sx * 0.26, 0.032, cz)
            for sx in (-1.0, 1.0)
            for cz in (-0.16, 0.08, 0.32)
        ],
        radius=0.008,
        height=0.010,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # 2. Stationary azimuth bearing ring (`R1_GreyMetal`) + turned `R1_Brass` azimuth degree ring
    mb.add_cylinder(
        center_u=(0.0, 0.052, hz),
        radius=0.225,
        length=0.046,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=24,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_torus(
        center_u=(0.0, 0.068, hz),
        major_radius=0.226,
        minor_radius=0.012,
        mat="R1_Brass",
        axis_u="Y",
        major_segments=24,
        minor_segments=8,
    )
    # 4 radial azimuth lock clamp dogs around the turntable perimeter
    for ang_deg in (45.0, 135.0, 225.0, 315.0):
        ang = math.radians(ang_deg)
        mb.add_bevel_box(
            center_u=(0.232 * math.sin(ang), 0.052, hz + 0.232 * math.cos(ang)),
            size_u=(0.042, 0.038, 0.042),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

    # 3. Rotating upper turntable platter (`R1_CreamEnamel` with 8 perimeter bolts)
    mb.add_cylinder(
        center_u=(0.0, 0.092, hz),
        radius=0.195,
        length=0.036,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=24,
        bevel=0.006,
        bevel_segments=1,
    )
    mb.add_bolts(
        [
            (0.158 * math.sin(2.0 * math.pi * i / 8), 0.110, hz + 0.158 * math.cos(2.0 * math.pi * i / 8))
            for i in range(8)
        ],
        radius=0.007,
        height=0.008,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )

    # 4. Twin mast elevation trunnion cheek brackets (`R1_GreyMetal`) & horizontal hinge axle (`R1_BareSteel` / `R1_Brass`)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.076, 0.145, hz),
            size_u=(0.028, 0.095, 0.125),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
            top_scale_xz=(0.90, 0.72),
        )
        mb.add_cylinder(
            center_u=(sx * 0.092, hy, hz),
            radius=0.034,
            length=0.022,
            mat="R1_Brass",
            axis_u="X",
            segments=16,
            bevel=0.003,
            bevel_segments=1,
        )
    mb.add_cylinder(
        center_u=(hx, hy, hz),
        radius=0.020,
        length=0.22,
        mat="R1_BareSteel",
        axis_u="X",
        segments=14,
        bevel=0.003,
        bevel_segments=1,
    )

    # 5. Forward elevation screw-jack actuator & cushioned rest cradle holding the mast folded half-down
    fold_rad = math.radians(TOPPER_05_FOLD_DEG)
    attach_s = 0.21
    attach_y = hy + attach_s * math.cos(fold_rad)
    attach_z = hz + attach_s * math.sin(fold_rad)
    clevis_u = (0.0, 0.055, 0.29)

    mb.add_bevel_box(
        center_u=(0.0, 0.048, 0.29),
        size_u=(0.11, 0.042, 0.085),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    mb.add_strut(
        p0_u=clevis_u,
        p1_u=(0.0, attach_y - 0.02, attach_z),
        width=0.036,
        thickness=0.036,
        mat="R1_BareSteel",
        bevel=0.005,
    )
    mb.add_strut(
        p0_u=(0.0, 0.065, 0.275),
        p1_u=(0.0, 0.175, 0.21),
        width=0.048,
        thickness=0.048,
        mat="R1_Brass",
        bevel=0.006,
    )
    # Forward rubber-padded travel rest pedestal at z = 0.31 m
    mb.add_bevel_box(
        center_u=(0.0, 0.058, 0.32),
        size_u=(0.16, 0.065, 0.054),
        mat="R1_SagePatch",
        bevel=0.005,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.095, 0.32),
        size_u=(0.14, 0.022, 0.048),
        mat="R1_Rubber",
        bevel=0.004,
        segments=1,
    )

    return mb.build_object(parent=root)


def _build_topper_05_rf_cabinet(root: bpy.types.Object) -> bpy.types.Object:
    """Build the rear finned S-band RF transceiver cabinet (`RfCabinet`) with copper waveguide lines."""
    mb = r1kit.MeshBuilder("RfCabinet", pivot_u=(0.0, 0.16, -0.34))
    cab_z = -0.34

    # 1. Base mounting plinth in the rear dorsal payload well
    mb.add_bevel_box(
        center_u=(0.0, 0.015, cab_z),
        size_u=(0.56, 0.030, 0.32),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )

    # 2. Lower & upper stacked instrument housings (Direction C stacked transceiver boxes + Lineup 05 finned unit)
    mb.add_bevel_box(
        center_u=(0.0, 0.088, cab_z),
        size_u=(0.52, 0.122, 0.28),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.085, cab_z),
        size_u=(0.526, 0.034, 0.286),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )
    # Shadow seam collar between lower and upper RF decks
    mb.add_bevel_box(
        center_u=(0.0, 0.155, cab_z),
        size_u=(0.49, 0.018, 0.255),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    # Upper S-band amplifier deck
    mb.add_bevel_box(
        center_u=(0.0, 0.220, cab_z),
        size_u=(0.48, 0.118, 0.25),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=2,
    )
    # Left-flank riveted R1_SagePatch access panel & right-flank dark glass inspection meter
    mb.add_bevel_box(
        center_u=(-0.262, 0.088, cab_z),
        size_u=(0.010, 0.082, 0.16),
        mat="R1_SagePatch",
        bevel=0.003,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.242, 0.220, cab_z + 0.05),
        size_u=(0.012, 0.052, 0.084),
        mat="R1_DarkGlass",
        bevel=0.003,
        segments=1,
    )
    for sx in (-1.0, 1.0):
        mb.add_vent_grille(
            center_u=(sx * 0.240, 0.220, cab_z - 0.045),
            width=0.095,
            height=0.062,
            depth=0.016,
            slat_count=3,
            rot_deg_u=(0.0, 90.0 * sx, 0.0),
            frame_mat="R1_GreyMetal",
            back_mat="R1_DarkGlass",
        )

    # 3. Top & rear vertical radiative cooling heatsink fins (`R1_BareSteel`) matching Lineup 05
    mb.add_bevel_box(
        center_u=(0.0, 0.285, cab_z - 0.01),
        size_u=(0.42, 0.022, 0.21),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    for fin_i in range(9):
        fx = -0.176 + fin_i * 0.044
        mb.add_bevel_box(
            center_u=(fx, 0.312, cab_z - 0.01),
            size_u=(0.012, 0.046, 0.195),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(fx, 0.175, cab_z - 0.146),
            size_u=(0.012, 0.195, 0.032),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
        )

    # 4. Twin curved `R1_Copper` coaxial waveguide feed pipes connecting `RfCabinet` into the turntable & mast
    for sx in (-1.0, 1.0):
        px = sx * 0.115
        mb.add_cylinder(
            center_u=(px, 0.225, cab_z + 0.132),
            radius=0.024,
            length=0.024,
            mat="R1_Brass",
            axis_u="Z",
            segments=12,
            bevel=0.003,
            bevel_segments=1,
        )
        mb.add_tube_path(
            points_u=[
                (px, 0.225, cab_z + 0.140),
                (px, 0.232, -0.14),
                (sx * 0.088, 0.205, -0.07),
                (sx * 0.068, 0.165, -0.02),
            ],
            radius=0.015,
            mat="R1_Copper",
            segments=12,
            cap_ends=True,
        )

    return mb.build_object(parent=root)


def _build_topper_05_coil_mast(root: bpy.types.Object) -> bpy.types.Object:
    """Build the tall S-band copper helical coil mast (`CoilMast`), folded half-down (`50°`) over the deck."""
    hx, hy, hz = TOPPER_05_HINGE_U
    mb = r1kit.MeshBuilder("CoilMast", pivot_u=TOPPER_05_HINGE_U)

    # Construct the mast upright along +Y above hinge (hx, hy, hz), then tilt by TOPPER_05_FOLD_DEG
    # 1. Lower mast hinge knuckle & main structural steel column (y = hy .. hy + 0.37)
    mb.add_cylinder(
        center_u=(hx, hy, hz),
        radius=0.048,
        length=0.13,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=16,
        bevel=0.005,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(hx, hy + 0.185, hz),
        radius=0.042,
        length=0.37,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=18,
        bevel=0.004,
        bevel_segments=1,
    )
    # Turned R1_Brass mast clamping collars along the lower column
    for cy_off in (0.09, 0.20, 0.34):
        mb.add_cylinder(
            center_u=(hx, hy + cy_off, hz),
            radius=0.052,
            length=0.026,
            mat="R1_Brass",
            axis_u="Y",
            segments=18,
            bevel=0.004,
            bevel_segments=1,
        )

    # Twin R1_Copper coaxial riser pipes running up the flanks of the lower mast column
    for sx in (-1.0, 1.0):
        mb.add_tube_path(
            points_u=[
                (sx * 0.066, hy + 0.01, hz),
                (sx * 0.066, hy + 0.18, hz),
                (sx * 0.066, hy + 0.35, hz),
            ],
            radius=0.013,
            mat="R1_Copper",
            segments=10,
            cap_ends=True,
        )

    # 2. Helical S-Band Transmit Antenna Base Ground-Plane Disc & Insulator Collar (at y = hy + 0.375)
    coil_base_y = hy + 0.375
    mb.add_cylinder(
        center_u=(hx, coil_base_y, hz),
        radius=0.132,
        length=0.022,
        mat="R1_Brass",
        axis_u="Y",
        segments=24,
        bevel=0.004,
        bevel_segments=1,
    )
    mb.add_torus(
        center_u=(hx, coil_base_y, hz),
        major_radius=0.130,
        minor_radius=0.009,
        mat="R1_GreyMetal",
        axis_u="Y",
        major_segments=24,
        minor_segments=6,
    )
    mb.add_bolts(
        [
            (
                hx + 0.102 * math.sin(2.0 * math.pi * i / 6),
                coil_base_y + 0.012,
                hz + 0.102 * math.cos(2.0 * math.pi * i / 6),
            )
            for i in range(6)
        ],
        radius=0.007,
        height=0.008,
        axis_u="Y",
        mat="R1_BareSteel",
        bevel_segments=0,
    )
    # Dark glass high-voltage RF feed-through insulator collar
    mb.add_cylinder(
        center_u=(hx, coil_base_y + 0.024, hz),
        radius=0.056,
        length=0.032,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=16,
        bevel=0.004,
        bevel_segments=1,
    )

    # 3. Central dielectric/steel support spine & 3 perimeter spacer rods through the copper helix
    coil_len = 0.35
    coil_center_y = coil_base_y + 0.022 + coil_len * 0.5
    coil_top_y = coil_base_y + 0.022 + coil_len

    mb.add_cylinder(
        center_u=(hx, coil_center_y, hz),
        radius=0.025,
        length=coil_len + 0.04,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=14,
        bevel=0.003,
        bevel_segments=1,
    )
    for rod_i in range(3):
        ang = 2.0 * math.pi * rod_i / 3.0
        mb.add_cylinder(
            center_u=(hx + 0.095 * math.sin(ang), coil_center_y, hz + 0.095 * math.cos(ang)),
            radius=0.0075,
            length=coil_len + 0.016,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
            bevel_segments=1,
        )

    # 4. Tall 8.5-Turn Spun `R1_Copper` Helical Transmit Coil
    _add_helical_coil_u(
        mb,
        center_u=(hx, coil_center_y, hz),
        coil_radius=0.106,
        wire_radius=0.0145,
        length=coil_len - 0.018,
        turns=8.5,
        axis_u="Y",
        mat="R1_Copper",
        segs_per_turn=18,
        wire_segs=8,
    )

    # 5. Upper R1_Brass spider ring & unlit R1_DarkGlass / R1_Brass finial cap
    mb.add_torus(
        center_u=(hx, coil_top_y, hz),
        major_radius=0.096,
        minor_radius=0.011,
        mat="R1_Brass",
        axis_u="Y",
        major_segments=20,
        minor_segments=6,
    )
    mb.add_cylinder(
        center_u=(hx, coil_top_y, hz),
        radius=0.042,
        length=0.022,
        mat="R1_Brass",
        axis_u="Y",
        segments=16,
        bevel=0.004,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(hx, coil_top_y + 0.024, hz),
        radius=0.026,
        radius_top=0.014,
        length=0.032,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=14,
        bevel=0.004,
        bevel_segments=1,
    )

    # Tilt the entire CoilMast forward (+Z) by TOPPER_05_FOLD_DEG (50 degrees, folded half-down)
    _rotate_bm_around_hinge_x(mb, TOPPER_05_HINGE_U, TOPPER_05_FOLD_DEG)
    return mb.build_object(parent=root)


def _build_topper_05_horn_feeds(root: bpy.types.Object) -> bpy.types.Object:
    """Build the two small flared brass microwave horn feeds (`HornFeeds`) mounted halfway up the folded mast."""
    hx, hy, hz = TOPPER_05_HINGE_U
    fold_rad = math.radians(TOPPER_05_FOLD_DEG)
    arm_s = 0.275
    piv_y = round(hy + arm_s * math.cos(fold_rad), 3)
    piv_z = round(hz + arm_s * math.sin(fold_rad), 3)

    mb = r1kit.MeshBuilder("HornFeeds", pivot_u=(0.0, piv_y, piv_z))
    arm_y = hy + arm_s

    # 1. Central waveguide splitter block clamped around the mast at s = 0.275 m
    mb.add_bevel_box(
        center_u=(hx, arm_y, hz),
        size_u=(0.145, 0.092, 0.115),
        mat="R1_Brass",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(hx, arm_y, hz + 0.058),
        size_u=(0.105, 0.062, 0.016),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )

    # 2. Left (-X) and Right (+X) Waveguide Arms + Flared Pyramidal Microwave Horn Feeds (Direction C)
    for sx in (-1.0, 1.0):
        # Horizontal cylindrical waveguide neck
        mb.add_cylinder(
            center_u=(sx * 0.115, arm_y, hz),
            radius=0.028,
            length=0.11,
            mat="R1_Brass",
            axis_u="X",
            segments=16,
            bevel=0.003,
            bevel_segments=1,
        )
        # Diagonal lower support gusset strut under each waveguide arm
        mb.add_strut(
            p0_u=(sx * 0.048, arm_y - 0.065, hz),
            p1_u=(sx * 0.165, arm_y - 0.015, hz),
            width=0.022,
            thickness=0.026,
            mat="R1_Brass",
            bevel=0.003,
        )
        # Inner throat flange ring
        mb.add_bevel_box(
            center_u=(sx * 0.168, arm_y, hz),
            size_u=(0.018, 0.082, 0.096),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )
        # Flared pyramidal microwave feed horn body (`R1_Brass`) opening outward along +/-X
        mb.add_bevel_box(
            center_u=(sx * 0.245, arm_y, hz),
            size_u=(0.074, 0.148, 0.088),
            mat="R1_Brass",
            bevel=0.007,
            segments=1,
            rot_deg_u=(0.0, 0.0, 90.0 * sx),
            top_scale_xz=(1.88, 1.82),
        )
        # Outer rolled rectangular horn aperture rim (`R1_Brass`)
        mb.add_bevel_box(
            center_u=(sx * 0.322, arm_y, hz),
            size_u=(0.020, 0.144, 0.168),
            mat="R1_Brass",
            bevel=0.005,
            segments=1,
        )
        # Recessed dark waveguide throat liner (`R1_DarkGlass`) inside the horn mouth
        mb.add_bevel_box(
            center_u=(sx * 0.326, arm_y, hz),
            size_u=(0.014, 0.108, 0.132),
            mat="R1_DarkGlass",
            bevel=0.003,
            segments=1,
        )
        # Internal copper probe pin inside the dark waveguide throat
        mb.add_cylinder(
            center_u=(sx * 0.328, arm_y, hz),
            radius=0.012,
            length=0.016,
            mat="R1_Copper",
            axis_u="X",
            segments=10,
            bevel=0.002,
            bevel_segments=1,
        )
        # Curved R1_Copper coaxial feed elbow into the underside of the horn throat
        mb.add_tube_path(
            points_u=[
                (sx * 0.066, arm_y - 0.075, hz),
                (sx * 0.125, arm_y - 0.068, hz),
                (sx * 0.155, arm_y - 0.032, hz),
            ],
            radius=0.012,
            mat="R1_Copper",
            segments=10,
            cap_ends=True,
        )

    # Tilt HornFeeds by the exact same hinge & fold angle as CoilMast
    _rotate_bm_around_hinge_x(mb, TOPPER_05_HINGE_U, TOPPER_05_FOLD_DEG)
    return mb.build_object(parent=root)


def _apply_ssr3_sun_bleached_weathering(root: bpy.types.Object, footprint_radius: float = 0.68) -> None:
    """Apply extra sun-bleached upper-surface edge wear (G) and settled lunar regolith dust (B) for Elder 05."""
    _apply_ssr3_decades_at_rest_weathering(root, footprint_radius=footprint_radius)

    mesh_objs = [o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and len(o.data.polygons) > 0]
    for obj in mesh_objs:
        me = obj.data
        ca = me.color_attributes.get(r1kit.VCOL_ATTRIBUTE_NAME)
        if ca is None:
            continue

        num_loops = len(me.loops)
        rgba = np.ones(num_loops * 4, dtype=np.float32)
        ca.data.foreach_get("color", rgba)
        rgba = rgba.reshape(-1, 4)

        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        loop_idx = 0
        for poly in me.polygons:
            wn = (rot_mat @ poly.normal).normalized()
            up_dot = float(wn.z)
            for l_i in poly.loop_indices:
                v_idx = me.loops[l_i].vertex_index
                wp = world_mat @ me.vertices[v_idx].co
                h_z = float(wp.z)

                r_ao = float(rgba[loop_idx, 0])
                g_wear = float(rgba[loop_idx, 1])
                b_dust = float(rgba[loop_idx, 2])

                # Strong sun-bleaching on the folded mast, horn feeds, and upper RF cabinet
                bleach_h = max(0.0, min(1.0, (h_z - 0.12) / 0.45))
                bleach_up = max(0.0, up_dot + 0.35)
                g_out = float(np.clip(max(g_wear * 1.28, bleach_h * bleach_up * 0.54 * r_ao), 0.0, 1.0))
                b_out = float(np.clip(max(b_dust, max(0.0, up_dot - 0.25) * 0.68), 0.0, 1.0))

                rgba[loop_idx, 0] = r_ao
                rgba[loop_idx, 1] = g_out
                rgba[loop_idx, 2] = b_out
                rgba[loop_idx, 3] = 1.0
                loop_idx += 1

        ca.data.foreach_set("color", rgba.reshape(-1))
        me.update()


def build_ssr3_topper_05() -> bpy.types.Object:
    """Build the complete `SSR3_Topper_05` ("Dish Tender") hierarchy in the active Blender scene."""
    root = r1kit.create_empty("SSR3_Topper_05", pivot_u=(0.0, 0.0, 0.0))

    _build_topper_05_turntable(root)
    _build_topper_05_rf_cabinet(root)
    _build_topper_05_coil_mast(root)
    _build_topper_05_horn_feeds(root)

    bpy.context.view_layer.update()
    _apply_ssr3_sun_bleached_weathering(root, footprint_radius=0.68)
    return root


def _build_unit_05_stencil(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the `Unit_Number_05` relief stencil (`05`) on both flanks at `UNIT_NUMBER_PIVOT_U`."""
    mb = r1kit.MeshBuilder("Unit_Number_05", pivot_u=UNIT_NUMBER_PIVOT_U)
    for sx in (-1.0, 1.0):
        _add_stencil_text_flank(
            mb,
            text="05",
            side_x=sx * 0.442,
            center_y=UNIT_NUMBER_PIVOT_U[1],
            center_z=UNIT_NUMBER_PIVOT_U[2],
            char_w=0.042,
            char_h=0.068,
            relief_x=0.005,
            mat="R1_GreyMetal",
        )
    return mb.build_object(parent=parent)


def build_elder05_combined() -> bpy.types.Object:
    """Build the combined Elder 05 assembly (`SSR3_Chassis` + `SSR3_Topper_05` + `05` stencil) for preview rendering."""
    chassis_root = build_ssr3_chassis()
    topper_socket = next((o for o in chassis_root.children if o.name == "Topper_Socket"), chassis_root)
    unit_num_socket = next((o for o in chassis_root.children if o.name == "Unit_Number"), chassis_root)

    topper_root = build_ssr3_topper_05()
    topper_root.parent = topper_socket
    topper_root.location = (0.0, 0.0, 0.0)

    _build_unit_05_stencil(unit_num_socket)
    bpy.context.view_layer.update()
    _apply_ssr3_sun_bleached_weathering(chassis_root, footprint_radius=0.88)
    return chassis_root


# ---------------------------------------------------------------------------
# Deliverable 2 (agy-ssr3b): SSR3_Topper_04 ("Terrace Lift & Jumper" —
# Pressurized Gas Bottles with Copper Lines, 4 De Laval Cold-Gas Thrusters,
# and a Raised Long-Travel Suspension Kit)
# ---------------------------------------------------------------------------
TOPPER_04_SUSPENSION_LIFT_Y: float = 0.14  # Hull lift above standard wheel stance (0.14 m)


def _add_pressurized_gas_bottle(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    radius: float,
    body_len: float,
    dome_len: float,
    mat: str = "R1_Brass",
    strap_mat: str = "R1_GreyMetal",
    strap_z_offsets: Sequence[float] = (-0.18, 0.18),
) -> None:
    """Append a horizontal Z-aligned cylindrical gas bottle with rounded dome ends and steel clamp bands."""
    cx, cy, cz = center_u
    mb.add_cylinder(
        center_u=(cx, cy, cz),
        radius=radius,
        length=body_len,
        mat=mat,
        axis_u="Z",
        segments=24,
        bevel=min(radius * 0.34, 0.038),
        bevel_segments=2,
    )
    for sz in (-1.0, 1.0):
        dome_cz = cz + sz * (body_len * 0.5 + dome_len * 0.32)
        r_bot = radius * 0.76 if sz > 0.0 else radius * 0.36
        r_top = radius * 0.36 if sz > 0.0 else radius * 0.76
        mb.add_cylinder(
            center_u=(cx, cy, dome_cz),
            radius=r_bot,
            radius_top=r_top,
            length=dome_len,
            mat=mat,
            axis_u="Z",
            segments=20,
            bevel=dome_len * 0.28,
            bevel_segments=2,
        )
        # End neck boss (`R1_BareSteel`)
        neck_cz = cz + sz * (body_len * 0.5 + dome_len * 0.85)
        mb.add_cylinder(
            center_u=(cx, cy, neck_cz),
            radius=radius * 0.24,
            length=0.026,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=12,
            bevel=0.003,
            bevel_segments=1,
        )

    for z_off in strap_z_offsets:
        mb.add_cylinder(
            center_u=(cx, cy, cz + z_off),
            radius=radius + 0.006,
            length=0.030,
            mat=strap_mat,
            axis_u="Z",
            segments=24,
            bevel=0.003,
            bevel_segments=1,
        )


def _build_topper_04_gas_bottles(root: bpy.types.Object) -> bpy.types.Object:
    """Build the twin dorsal O2 gas bottles + twin side outrigger accumulator tanks (`GasBottles`)."""
    mb = r1kit.MeshBuilder("GasBottles", pivot_u=(0.0, 0.15, 0.0))

    # 1. Dorsal steel tank cradle frame in the payload well
    mb.add_bevel_box(
        center_u=(0.0, 0.016, 0.0),
        size_u=(0.62, 0.032, 0.92),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    for sz_cradle in (-0.24, 0.24):
        mb.add_bevel_box(
            center_u=(0.0, 0.056, sz_cradle),
            size_u=(0.58, 0.054, 0.048),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
        )
    # Central dorsal R1_SagePatch pressure regulator & service manifold housing
    mb.add_bevel_box(
        center_u=(0.0, 0.075, 0.0),
        size_u=(0.12, 0.085, 0.42),
        mat="R1_SagePatch",
        bevel=0.006,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.122, 0.0),
        size_u=(0.095, 0.016, 0.16),
        mat="R1_DarkGlass",
        bevel=0.003,
        segments=1,
    )

    # 2. Twin large dorsal brushed-brass O2 tanks (`R1_Brass`, x = +/- 0.165 m, y = 0.168 m)
    for sx in (-1.0, 1.0):
        _add_pressurized_gas_bottle(
            mb,
            center_u=(sx * 0.165, 0.168, 0.0),
            radius=0.114,
            body_len=0.72,
            dome_len=0.068,
            mat="R1_Brass",
            strap_mat="R1_GreyMetal",
            strap_z_offsets=(-0.24, 0.24),
        )
        # Saddle clamp bolts on the outer edges of the dorsal tank bands
        mb.add_bolts(
            [
                (sx * 0.285, 0.115, -0.24),
                (sx * 0.285, 0.115, 0.24),
            ],
            radius=0.008,
            height=0.012,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # 3. Twin side outrigger accumulator bottles (`R1_Brass`, x = +/- 0.448 m, y = 0.025 m)
    for sx in (-1.0, 1.0):
        # Outrigger steel saddle beams cantilevering over the dorsal coaming rim
        for sz_beam in (-0.20, 0.20):
            mb.add_bevel_box(
                center_u=(sx * 0.355, 0.068, sz_beam),
                size_u=(0.19, 0.032, 0.044),
                mat="R1_GreyMetal",
                bevel=0.004,
                segments=1,
            )
            mb.add_bevel_box(
                center_u=(sx * 0.435, 0.035, sz_beam),
                size_u=(0.048, 0.068, 0.044),
                mat="R1_GreyMetal",
                bevel=0.004,
                segments=1,
            )
        _add_pressurized_gas_bottle(
            mb,
            center_u=(sx * 0.452, 0.028, 0.0),
            radius=0.076,
            body_len=0.52,
            dome_len=0.052,
            mat="R1_Brass",
            strap_mat="R1_GreyMetal",
            strap_z_offsets=(-0.20, 0.20),
        )
        mb.add_bolts(
            [
                (sx * 0.532, 0.028, -0.20),
                (sx * 0.532, 0.028, 0.20),
            ],
            radius=0.007,
            height=0.010,
            axis_u="X",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_topper_04_copper_lines(root: bpy.types.Object) -> bpy.types.Object:
    """Build the high-pressure `R1_Copper` gas manifold lines and brass union valves (`CopperLines`)."""
    mb = r1kit.MeshBuilder("CopperLines", pivot_u=(0.0, 0.12, 0.0))

    # Front and rear cross-manifold header blocks (`R1_Copper` + `R1_Brass`)
    for sz in (-1.0, 1.0):
        hz = sz * 0.435
        mb.add_bevel_box(
            center_u=(0.0, 0.058, hz),
            size_u=(0.38, 0.036, 0.042),
            mat="R1_Copper",
            bevel=0.005,
            segments=1,
        )
        for sx in (-1.0, 1.0):
            # Valve block on each dorsal bottle dome
            mb.add_cylinder(
                center_u=(sx * 0.165, 0.168, sz * 0.425),
                radius=0.024,
                length=0.032,
                mat="R1_Brass",
                axis_u="Z",
                segments=12,
                bevel=0.003,
                bevel_segments=1,
            )
            # High-pressure arched copper pipe from dorsal bottle dome down to front/rear deck header
            mb.add_tube_path(
                points_u=[
                    (sx * 0.165, 0.168, sz * 0.432),
                    (sx * 0.215, 0.155, sz * 0.485),
                    (sx * 0.225, 0.088, sz * 0.475),
                    (sx * 0.165, 0.062, sz * 0.435),
                ],
                radius=0.014,
                mat="R1_Copper",
                segments=10,
                cap_ends=True,
            )
            # Copper feed loop from dorsal bottle out over the coaming into the side accumulator bottle dome
            mb.add_tube_path(
                points_u=[
                    (sx * 0.165, 0.168, sz * 0.430),
                    (sx * 0.295, 0.162, sz * 0.410),
                    (sx * 0.415, 0.105, sz * 0.365),
                    (sx * 0.452, 0.028, sz * 0.305),
                ],
                radius=0.013,
                mat="R1_Copper",
                segments=10,
                cap_ends=True,
            )
            # Outer side manifold pipe running along the side accumulator bottle and feeding the De Laval thrusters
            mb.add_tube_path(
                points_u=[
                    (sx * 0.452, 0.028, sz * 0.305),
                    (sx * 0.505, -0.025, sz * 0.335),
                    (sx * 0.485, -0.068, sz * 0.240),
                    (sx * 0.458, -0.072, sz * 0.225),
                ],
                radius=0.012,
                mat="R1_Copper",
                segments=10,
                cap_ends=True,
            )

    # Longitudinal side copper balance rail along each outrigger bottle
    for sx in (-1.0, 1.0):
        mb.add_tube_path(
            points_u=[
                (sx * 0.518, -0.022, -0.28),
                (sx * 0.525, -0.022, 0.0),
                (sx * 0.518, -0.022, 0.28),
            ],
            radius=0.011,
            mat="R1_Copper",
            segments=10,
            cap_ends=True,
        )

    return mb.build_object(parent=root)


def _build_topper_04_jump_thrusters(root: bpy.types.Object) -> bpy.types.Object:
    """Build the 4 canted conical brass De Laval Cold-Gas Jump Thruster nozzles (`JumpThrusters`)."""
    mb = r1kit.MeshBuilder("JumpThrusters", pivot_u=(0.0, -0.14, 0.0))

    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            tx = sx * 0.456
            tz = sz * 0.225
            cant_deg = 8.0 * sx

            # 1. Upper solenoid thruster valve & mounting bracket under the side accumulator bottle
            mb.add_bevel_box(
                center_u=(tx - sx * 0.018, -0.052, tz),
                size_u=(0.068, 0.044, 0.068),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
            )
            mb.add_cylinder(
                center_u=(tx, -0.082, tz),
                radius=0.038,
                length=0.034,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=16,
                bevel=0.004,
                bevel_segments=1,
                rot_deg_u=(0.0, 0.0, cant_deg),
            )

            # 2. Canted conical R1_Brass De Laval thruster bell (radius_top=0.030 throat -> radius=0.072 exit mouth)
            bell_x = tx + sx * 0.010
            bell_y = -0.155
            mb.add_cylinder(
                center_u=(bell_x, bell_y, tz),
                radius=0.072,
                radius_top=0.030,
                length=0.135,
                mat="R1_Brass",
                axis_u="Y",
                segments=20,
                bevel=0.004,
                bevel_segments=1,
                rot_deg_u=(0.0, 0.0, cant_deg),
            )
            # Mid-bell stiffening band & bottom exit lip torus
            mb.add_torus(
                center_u=(bell_x + sx * 0.004, bell_y - 0.020, tz),
                major_radius=0.056,
                minor_radius=0.006,
                mat="R1_Brass",
                axis_u="Y",
                major_segments=20,
                minor_segments=6,
                rot_deg_u=(0.0, 0.0, cant_deg),
            )
            mb.add_torus(
                center_u=(bell_x + sx * 0.009, bell_y - 0.064, tz),
                major_radius=0.070,
                minor_radius=0.007,
                mat="R1_Brass",
                axis_u="Y",
                major_segments=20,
                minor_segments=6,
                rot_deg_u=(0.0, 0.0, cant_deg),
            )
            # Dark interior nozzle throat shadow plug (`R1_DarkGlass`) at bottom of bell
            mb.add_cylinder(
                center_u=(bell_x + sx * 0.008, bell_y - 0.062, tz),
                radius=0.058,
                length=0.012,
                mat="R1_DarkGlass",
                axis_u="Y",
                segments=18,
                bevel=0.002,
                bevel_segments=1,
                rot_deg_u=(0.0, 0.0, cant_deg),
            )

    return mb.build_object(parent=root)


def _build_topper_04_suspension_kit(root: bpy.types.Object) -> bpy.types.Object:
    """Build the raised long-travel coil-spring suspension kit & drop-belly cradle (`SuspensionKit`)."""
    mb = r1kit.MeshBuilder("SuspensionKit", pivot_u=(0.0, -0.48, 0.0))

    # In Elder04, the hull (and Topper_Socket) is lifted by TOPPER_04_SUSPENSION_LIFT_Y (+0.14 m),
    # so the 4 wheel axle centers sit at y_local = (WHEEL_RADIUS - (0.69 + 0.14)) = -0.615 m.
    axle_y = WHEEL_RADIUS - (TOPPER_SOCKET_PIVOT_U[1] + TOPPER_04_SUSPENSION_LIFT_Y)  # -0.615 m

    # 1. Tapered central drop-belly undercarriage cradle (matching Elder 04 in ssr3_direction_a_boxy_cart.png)
    mb.add_bevel_box(
        center_u=(0.0, -0.465, 0.0),
        size_u=(0.48, 0.155, 1.08),
        mat="R1_CreamEnamel",
        bevel=0.012,
        segments=1,
        top_scale_xz=(1.12, 1.08),
    )
    mb.add_bevel_box(
        center_u=(0.0, -0.542, 0.0),
        size_u=(0.46, 0.028, 1.02),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    # Front and rear angled belly skid glacis plates with center vertical seam & corner bolts
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, -0.465, sz * 0.535),
            size_u=(0.44, 0.145, 0.042),
            mat="R1_CreamEnamel",
            bevel=0.006,
            segments=1,
            rot_deg_u=(-sz * 18.0, 0.0, 0.0),
        )
        mb.add_bevel_box(
            center_u=(0.0, -0.465, sz * 0.556),
            size_u=(0.012, 0.135, 0.012),
            mat="R1_GreyMetal",
            bevel=0.002,
            segments=1,
            rot_deg_u=(-sz * 18.0, 0.0, 0.0),
        )
        mb.add_bolts(
            [
                (sx * 0.18, -0.42, sz * 0.552)
                for sx in (-1.0, 1.0)
            ]
            + [
                (sx * 0.18, -0.51, sz * 0.535)
                for sx in (-1.0, 1.0)
            ],
            radius=0.007,
            height=0.008,
            axis_u="Z",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    # 2. 4 Long-Travel Coil-Over Shock Struts + Diagonal Wishbone Control Arms at FL, FR, RL, RR
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            wz = sz * WHEEL_BASE_Z
            top_mount_u = (sx * 0.285, -0.345, wz)
            knuckle_u = (sx * 0.335, axle_y + 0.012, wz)
            mid_shock_u = (
                0.5 * (top_mount_u[0] + knuckle_u[0]),
                0.5 * (top_mount_u[1] + knuckle_u[1]),
                wz,
            )
            shock_cant_deg = 11.0 * sx

            # Upper spring-tower shroud enclosing the chassis upper strut root
            mb.add_bevel_box(
                center_u=(sx * 0.305, -0.335, wz),
                size_u=(0.095, 0.095, 0.14),
                mat="R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )
            # Central telescoping steel damper shaft connecting upper tower to lower wheel knuckle
            mb.add_strut(
                p0_u=top_mount_u,
                p1_u=knuckle_u,
                width=0.032,
                thickness=0.032,
                mat="R1_BareSteel",
                bevel=0.004,
            )
            # Upper & lower R1_Brass coil-spring perch collars
            for perch_t in (0.14, 0.86):
                py_p = top_mount_u[1] * (1.0 - perch_t) + knuckle_u[1] * perch_t
                px_p = top_mount_u[0] * (1.0 - perch_t) + knuckle_u[0] * perch_t
                mb.add_cylinder(
                    center_u=(px_p, py_p, wz),
                    radius=0.044,
                    length=0.020,
                    mat="R1_Brass",
                    axis_u="Y",
                    segments=16,
                    bevel=0.003,
                    bevel_segments=1,
                    rot_deg_u=(0.0, 0.0, shock_cant_deg),
                )
            # Long-travel 7.5-turn exposed helical coil spring (`R1_GreyMetal`)
            _add_helical_coil_u(
                mb,
                center_u=mid_shock_u,
                coil_radius=0.038,
                wire_radius=0.0085,
                length=0.205,
                turns=7.5,
                axis_u="Y",
                mat="R1_GreyMetal",
                segs_per_turn=14,
                wire_segs=7,
                rot_deg_u=(0.0, 0.0, shock_cant_deg),
            )

            # Twin diagonal lower A-arm wishbone struts from the drop-belly cradle to the wheel hub knuckle
            for arm_dz in (-0.065, 0.065):
                mb.add_strut(
                    p0_u=(sx * 0.205, -0.505, wz + arm_dz),
                    p1_u=(sx * 0.335, axle_y, wz + arm_dz * 0.25),
                    width=0.034,
                    thickness=0.028,
                    mat="R1_GreyMetal",
                    bevel=0.004,
                )
            # Upper radius link arm stabilizing the lifted knuckle
            mb.add_strut(
                p0_u=(sx * 0.225, -0.415, wz - sz * 0.08),
                p1_u=(sx * 0.330, axle_y + 0.045, wz),
                width=0.028,
                thickness=0.024,
                mat="R1_BareSteel",
                bevel=0.003,
            )

            # Heavy forged lower wheel-hub carrier knuckle at the lifted axle position
            mb.add_bevel_box(
                center_u=(sx * 0.340, axle_y + 0.008, wz),
                size_u=(0.062, 0.076, 0.084),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
            )
            mb.add_cylinder(
                center_u=(sx * 0.355, axle_y, wz),
                radius=0.034,
                length=0.052,
                mat="R1_BareSteel",
                axis_u="X",
                segments=14,
                bevel=0.004,
                bevel_segments=1,
            )

    return mb.build_object(parent=root)


def build_ssr3_topper_04() -> bpy.types.Object:
    """Build the complete `SSR3_Topper_04` ("Terrace Lift & Jumper") hierarchy in the active Blender scene."""
    root = r1kit.create_empty("SSR3_Topper_04", pivot_u=(0.0, 0.0, 0.0))

    _build_topper_04_gas_bottles(root)
    _build_topper_04_copper_lines(root)
    _build_topper_04_jump_thrusters(root)
    _build_topper_04_suspension_kit(root)

    bpy.context.view_layer.update()
    _apply_ssr3_decades_at_rest_weathering(root, footprint_radius=0.68)
    return root


def _build_unit_04_stencil(parent: bpy.types.Object) -> bpy.types.Object:
    """Build the `Unit_Number_04` relief stencil (`04`) on both flanks at `UNIT_NUMBER_PIVOT_U`."""
    mb = r1kit.MeshBuilder("Unit_Number_04", pivot_u=UNIT_NUMBER_PIVOT_U)
    for sx in (-1.0, 1.0):
        _add_stencil_text_flank(
            mb,
            text="04",
            side_x=sx * 0.442,
            center_y=UNIT_NUMBER_PIVOT_U[1],
            center_z=UNIT_NUMBER_PIVOT_U[2],
            char_w=0.042,
            char_h=0.068,
            relief_x=0.005,
            mat="R1_GreyMetal",
        )
    return mb.build_object(parent=parent)


def build_elder04_combined() -> bpy.types.Object:
    """Build the combined Elder 04 assembly (`SSR3_Chassis` + `SSR3_Topper_04` + `04` stencil + lifted hull)."""
    chassis_root = build_ssr3_chassis()
    topper_socket = next((o for o in chassis_root.children if o.name == "Topper_Socket"), chassis_root)
    unit_num_socket = next((o for o in chassis_root.children if o.name == "Unit_Number"), chassis_root)

    topper_root = build_ssr3_topper_04()
    topper_root.parent = topper_socket
    topper_root.location = (0.0, 0.0, 0.0)

    _build_unit_04_stencil(unit_num_socket)

    # Apply the long-travel suspension lift (+0.14 m Z in Blender space) to the hull while keeping wheels on ground
    for child in chassis_root.children:
        if not child.name.startswith("Wheel_"):
            child.location.z += TOPPER_04_SUSPENSION_LIFT_Y

    bpy.context.view_layer.update()
    _apply_ssr3_decades_at_rest_weathering(chassis_root, footprint_radius=0.88)
    return chassis_root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build SSR3_Chassis and SSR3_Topper_06/05/04 FBXs and preview renders.")
    parser.add_argument(
        "--chassis-fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/SSR3_Chassis.fbx"),
    )
    parser.add_argument(
        "--topper06-fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/SSR3_Topper_06.fbx"),
    )
    parser.add_argument(
        "--topper05-fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/SSR3_Topper_05.fbx"),
    )
    parser.add_argument(
        "--topper04-fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/SSR3_Topper_04.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument(
        "--only",
        choices=("all", "04", "05", "06"),
        default="all",
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    concept_a_img = Path("D:/Project/MoonProject-wt/agy-elders/Logs/concepts/ssr3_direction_a_boxy_cart.png")
    concept_c_img = Path("D:/Project/MoonProject-wt/agy-elders/Logs/concepts/ssr3_direction_c_mast_rover.png")

    if args.only in ("all", "06"):
        # 1. Build & export SSR3_Chassis.fbx
        r1kit.reset_scene()
        chassis_root = build_ssr3_chassis()
        chassis_tris = r1kit.count_triangles(chassis_root)
        print(f"[SSR3_Chassis] Triangles: {chassis_tris}")
        r1kit.export_fbx(chassis_root, args.chassis_fbx_out)

        # 2. Build & export SSR3_Topper_06.fbx
        r1kit.reset_scene()
        topper06_root = build_ssr3_topper_06()
        topper06_tris = r1kit.count_triangles(topper06_root)
        print(f"[SSR3_Topper_06] Triangles: {topper06_tris}")
        r1kit.export_fbx(topper06_root, args.topper06_fbx_out)

        if not args.skip_renders:
            r1kit.render_structure_suite(
                root=topper06_root,
                slug="ssr3_topper_06",
                out_dir=args.renders_dir,
                target_height=0.39,
                footprint_radius=0.62,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_a_img if concept_a_img.exists() else None,
                use_active_for_compare=False,
            )

            r1kit.reset_scene()
            elder06_root = build_elder06_combined()
            r1kit.render_structure_suite(
                root=elder06_root,
                slug="elder06",
                out_dir=args.renders_dir,
                target_height=1.08,
                footprint_radius=0.88,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_a_img if concept_a_img.exists() else None,
                use_active_for_compare=False,
            )

    if args.only in ("all", "05"):
        # 3. Build & export SSR3_Topper_05.fbx ("Dish Tender")
        r1kit.reset_scene()
        topper05_root = build_ssr3_topper_05()
        topper05_tris = r1kit.count_triangles(topper05_root)
        print(f"[SSR3_Topper_05] Triangles: {topper05_tris}")
        r1kit.export_fbx(topper05_root, args.topper05_fbx_out)

        if not args.skip_renders:
            r1kit.render_structure_suite(
                root=topper05_root,
                slug="ssr3_topper_05",
                out_dir=args.renders_dir,
                target_height=0.72,
                footprint_radius=0.68,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_c_img if concept_c_img.exists() else None,
                use_active_for_compare=False,
            )

            r1kit.reset_scene()
            elder05_root = build_elder05_combined()
            r1kit.render_structure_suite(
                root=elder05_root,
                slug="elder05",
                out_dir=args.renders_dir,
                target_height=1.38,
                footprint_radius=0.88,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_c_img if concept_c_img.exists() else None,
                use_active_for_compare=False,
            )

    if args.only in ("all", "04"):
        # 4. Build & export SSR3_Topper_04.fbx ("Terrace Lift & Jumper")
        r1kit.reset_scene()
        topper04_root = build_ssr3_topper_04()
        topper04_tris = r1kit.count_triangles(topper04_root)
        print(f"[SSR3_Topper_04] Triangles: {topper04_tris}")
        r1kit.export_fbx(topper04_root, args.topper04_fbx_out)

        if not args.skip_renders:
            r1kit.render_structure_suite(
                root=topper04_root,
                slug="ssr3_topper_04",
                out_dir=args.renders_dir,
                target_height=0.92,
                footprint_radius=0.72,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_a_img if concept_a_img.exists() else None,
                use_active_for_compare=False,
            )

            r1kit.reset_scene()
            elder04_root = build_elder04_combined()
            r1kit.render_structure_suite(
                root=elder04_root,
                slug="elder04",
                out_dir=args.renders_dir,
                target_height=1.12,
                footprint_radius=0.92,
                pose_rest_fn=lambda _r: None,
                pose_active_fn=lambda _r: None,
                cam_side_x=-1.0,
                concept_path=concept_a_img if concept_a_img.exists() else None,
                use_active_for_compare=False,
            )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)



