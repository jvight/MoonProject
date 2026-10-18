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

    # 6. Flank `ILC · SSR-3` stencil lettering on the rear half of the orange beltline + `Unit_Number` frame pad
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
        for dz in (-0.095, 0.095):
            mb.add_bevel_box(
                center_u=(sx * 0.441, stripe_y, UNIT_NUMBER_PIVOT_U[2] + dz),
                size_u=(0.004, 0.084, 0.006),
                mat="R1_GreyMetal",
                bevel=0.0,
                segments=0,
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


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build SSR3_Chassis FBX and preview renders.")
    parser.add_argument(
        "--chassis-fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover/SSR3_Chassis.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    chassis_root = build_ssr3_chassis()
    tris = r1kit.count_triangles(chassis_root)
    print(f"[SSR3_Chassis] Triangles: {tris}")
    r1kit.export_fbx(chassis_root, args.chassis_fbx_out)

    if not args.skip_renders:
        concept_img = Path("D:/Project/MoonProject-wt/agy-elders/Logs/concepts/ssr3_direction_a_boxy_cart.png")
        r1kit.render_structure_suite(
            root=chassis_root,
            slug="ssr3_chassis",
            out_dir=args.renders_dir,
            target_height=0.75,
            footprint_radius=0.85,
            pose_rest_fn=lambda _r: None,
            pose_active_fn=lambda _r: None,
            cam_side_x=-1.0,
            concept_path=concept_img if concept_img.exists() else None,
            use_active_for_compare=False,
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
