"""Generate the 6 R1 Basin Life story props (`Assets/_Project/Art/R1/Models/Props/`):
1. Prop_MissionFlag.fbx
2. Prop_SeismicStation.fbx
3. Prop_Retroreflector.fbx
4. Prop_SurveyTripod.fbx
5. Prop_SampleRack.fbx
6. Decal_BootprintTrail.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_basin_life_props.py -- --prop mission_flag
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Callable, Dict, List, Sequence, Tuple

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"


def ensure_extended_r1_materials() -> None:
    """Register R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime."""
    extra_specs = {
        "R1_Screen": {
            "base_color": r1kit._srgb_to_linear("#1A140E"),
            "wear_color": r1kit._srgb_to_linear("#34281C"),
            "dust_color": r1kit._srgb_to_linear("#5E564C"),
            "metallic": 0.05,
            "roughness": 0.12,
            "emission": r1kit._srgb_to_linear("#FFB24A"),
            "emission_strength": 1.0,
        },
        "R1_Brass": {
            "base_color": r1kit._srgb_to_linear("#DBB052"),
            "wear_color": r1kit._srgb_to_linear("#F7D98C"),
            "dust_color": r1kit._srgb_to_linear("#8A8270"),
            "metallic": 0.92,
            "roughness": 0.24,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_Signage": {
            "base_color": r1kit._srgb_to_linear("#EDE3CE"),
            "wear_color": r1kit._srgb_to_linear("#464952"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.04,
            "roughness": 0.36,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_WhitePaint": {
            "base_color": r1kit._srgb_to_linear("#E8EAE6"),
            "wear_color": r1kit._srgb_to_linear("#C2C7CC"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.02,
            "roughness": 0.82,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_TealMark": {
            "base_color": r1kit._srgb_to_linear("#2F6E73"),
            "wear_color": r1kit._srgb_to_linear("#A8BDC2"),
            "dust_color": r1kit._srgb_to_linear("#788086"),
            "metallic": 0.04,
            "roughness": 0.65,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_SilverFoil": {
            "base_color": r1kit._srgb_to_linear("#C9CED2"),
            "wear_color": r1kit._srgb_to_linear("#EBF0F5"),
            "dust_color": r1kit._srgb_to_linear("#8A8E94"),
            "metallic": 0.85,
            "roughness": 0.45,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
    }
    allowed = list(r1kit.ALLOWED_MATERIALS)
    for name, spec in extra_specs.items():
        if name not in allowed:
            allowed.append(name)
        r1kit.MATERIAL_SPECS[name] = spec
    r1kit.ALLOWED_MATERIALS = tuple(allowed)


def _rotate_u(
    pt_u: Tuple[float, float, float],
    rot_deg_u: Tuple[float, float, float],
    origin_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> Tuple[float, float, float]:
    """Rotate a Unity-space point (x, y, z) around origin_u using the same Euler convention as r1kit in_place primitives."""
    ox, oy, oz = origin_u
    dx = pt_u[0] - ox
    dy = pt_u[1] - oy
    dz = pt_u[2] - oz
    # Convert delta to Blender vector (dx, dz, dy)
    vb = Vector((dx, dz, dy))
    rot_b = Euler(
        (
            math.radians(rot_deg_u[0]),
            math.radians(rot_deg_u[2]),
            math.radians(-rot_deg_u[1]),
        ),
        "XYZ",
    ).to_matrix()
    rb = rot_b @ vb
    return (ox + rb.x, oy + rb.z, oz + rb.y)


def _add_pleated_flag_slab(
    mb: r1kit.MeshBuilder,
    x_cols: Sequence[float],
    z_pleats: Sequence[float],
    y_bot: float,
    y_top: float,
    half_thick: float,
    mat: str,
    rot_deg_u: Tuple[float, float, float],
    origin_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Build a closed manifold rigid vacuum-flag sheet with vertical storage-fold pleats (no flapping wave)."""
    bm = mb.bm
    mat_idx = mb._mat_index(mat)
    n_cols = len(x_cols)
    n_rows = 7

    front_grid: List[List[bmesh.types.BMVert]] = []
    back_grid: List[List[bmesh.types.BMVert]] = []

    for r_i in range(n_rows):
        ty = r_i / (n_rows - 1)
        y_cur = y_bot + (y_top - y_bot) * ty
        # Slight gravitational/thermal sag at the fly end in vacuum (~8 mm max), zero wind wave
        f_row: List[bmesh.types.BMVert] = []
        b_row: List[bmesh.types.BMVert] = []
        for c_i in range(n_cols):
            tx = c_i / (n_cols - 1)
            x_cur = x_cols[c_i]
            # Vertical storage crease amplitude softens slightly near the top rod hem
            pleat_scale = 0.35 + 0.65 * math.sin(ty * math.pi * 0.85)
            z_mid = z_pleats[c_i] * pleat_scale
            y_sag = -0.008 * (tx ** 1.6) * (1.0 - 0.35 * ty)

            pf_u = _rotate_u((x_cur, y_cur + y_sag, z_mid + half_thick), rot_deg_u, origin_u)
            pb_u = _rotate_u((x_cur, y_cur + y_sag, z_mid - half_thick), rot_deg_u, origin_u)
            f_row.append(bm.verts.new(mb._to_local_b(pf_u)))
            b_row.append(bm.verts.new(mb._to_local_b(pb_u)))
        front_grid.append(f_row)
        back_grid.append(b_row)

    faces: List[bmesh.types.BMFace] = []
    for r_i in range(n_rows - 1):
        for c_i in range(n_cols - 1):
            # Front (+Z)
            faces.append(
                bm.faces.new(
                    [
                        front_grid[r_i][c_i],
                        front_grid[r_i][c_i + 1],
                        front_grid[r_i + 1][c_i + 1],
                        front_grid[r_i + 1][c_i],
                    ]
                )
            )
            # Back (-Z)
            faces.append(
                bm.faces.new(
                    [
                        back_grid[r_i][c_i + 1],
                        back_grid[r_i][c_i],
                        back_grid[r_i + 1][c_i],
                        back_grid[r_i + 1][c_i + 1],
                    ]
                )
            )

    # Bottom and top border quads
    for c_i in range(n_cols - 1):
        faces.append(
            bm.faces.new(
                [
                    back_grid[0][c_i],
                    back_grid[0][c_i + 1],
                    front_grid[0][c_i + 1],
                    front_grid[0][c_i],
                ]
            )
        )
        faces.append(
            bm.faces.new(
                [
                    front_grid[-1][c_i],
                    front_grid[-1][c_i + 1],
                    back_grid[-1][c_i + 1],
                    back_grid[-1][c_i],
                ]
            )
        )

    # Left (hoist) and right (fly) border quads
    for r_i in range(n_rows - 1):
        faces.append(
            bm.faces.new(
                [
                    back_grid[r_i][0],
                    front_grid[r_i][0],
                    front_grid[r_i + 1][0],
                    back_grid[r_i + 1][0],
                ]
            )
        )
        faces.append(
            bm.faces.new(
                [
                    front_grid[r_i][-1],
                    back_grid[r_i][-1],
                    back_grid[r_i + 1][-1],
                    front_grid[r_i + 1][-1],
                ]
            )
        )

    r1kit.ensure_outward_normals(bm, per_shell=True)
    for f in faces:
        if f.is_valid:
            f.material_index = mat_idx
            f.smooth = True


def build_mission_flag() -> bpy.types.Object:
    """Build Deliverable 1: Prop_MissionFlag (~0.98 W x 0.28 D x 1.56 H m).

    Sun-bleached flag (almost white, faint stripes) hanging from a horizontal top rod on a slightly
    tilted pole, base sunk in regolith (Apollo-style vacuum flag, no flapping cloth).
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_MissionFlag", pivot_u=(0.0, 0.0, 0.0))

    # Slight Apollo-style pole tilt (tilted ~6.0 deg toward +X and +2.5 deg toward +Z)
    pole_rot = (2.5, 0.0, -6.0)
    pole_origin = (0.0, 0.0, 0.0)

    def tp(x: float, y: float, z: float) -> Tuple[float, float, float]:
        return _rotate_u((x, y, z), pole_rot, pole_origin)

    # 1. FlagPole: regolith-sunk drive tube, compacted dust mound, telescoping pole, horizontal top rod
    pole_mb = r1kit.MeshBuilder("FlagPole", pivot_u=(0.0, 0.0, 0.0))

    # Compacted regolith mound around the sunk pole base (sunk -0.035 m below grade)
    pole_mb.add_cylinder(
        center_u=(0.0, 0.002, 0.0),
        radius=0.115,
        radius_top=0.058,
        length=0.068,
        mat="R1_Basalt",
        axis_u="Y",
        segments=14,
        bevel=0.008,
        bevel_segments=1,
    )
    # Disturbed regolith clods around base collar
    for cx, cz, rad, h in (
        (0.075, 0.035, 0.032, 0.024),
        (-0.068, 0.048, 0.028, 0.022),
        (0.022, -0.078, 0.035, 0.025),
    ):
        pole_mb.add_cylinder(
            center_u=(cx, 0.004, cz),
            radius=rad,
            radius_top=rad * 0.68,
            length=h,
            mat="R1_Basalt",
            axis_u="Y",
            segments=8,
            bevel=0.004,
            bevel_segments=1,
        )

    # Lower drive stake tube (sunk into regolith from y=-0.035 to y=0.70)
    pole_mb.add_cylinder(
        center_u=tp(0.0, 0.332, 0.0),
        radius=0.017,
        length=0.735,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    # Faded orange insertion-depth indicator band near bottom of drive tube
    pole_mb.add_cylinder(
        center_u=tp(0.0, 0.145, 0.0),
        radius=0.0185,
        length=0.036,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    # Hammered drive-tube top anvil collar + bayonet sleeve at y=0.70 m
    pole_mb.add_cylinder(
        center_u=tp(0.0, 0.70, 0.0),
        radius=0.024,
        length=0.052,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.003,
        rot_deg_u=pole_rot,
    )
    pole_mb.add_cylinder(
        center_u=tp(0.0, 0.735, 0.0),
        radius=0.020,
        length=0.028,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )

    # Upper telescoping pole section (y=0.74 to y=1.49 m)
    pole_mb.add_cylinder(
        center_u=tp(0.0, 1.115, 0.0),
        radius=0.0135,
        length=0.75,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    # Lower flag tie-down ring at y=0.93 m
    pole_mb.add_cylinder(
        center_u=tp(0.0, 0.93, 0.0),
        radius=0.018,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )

    # Top hinge bracket & finial cap at y=1.48..1.525 m
    pole_mb.add_bevel_box(
        center_u=tp(0.012, 1.482, 0.0),
        size_u=(0.052, 0.046, 0.036),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
        rot_deg_u=pole_rot,
    )
    pole_mb.add_cylinder(
        center_u=tp(0.0, 1.516, 0.0),
        radius=0.016,
        radius_top=0.010,
        length=0.028,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )

    # Horizontal telescoping top crossbar extending along +X (0.02 m to 0.86 m at y=1.482 m)
    # Outer sleeve (0.02..0.48 m)
    pole_mb.add_cylinder(
        center_u=tp(0.25, 1.482, 0.0),
        radius=0.0105,
        length=0.46,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    # Telescoping inner rod (0.48..0.86 m) — slightly jammed 3 cm short like Apollo 11
    pole_mb.add_cylinder(
        center_u=tp(0.67, 1.482, 0.0),
        radius=0.008,
        length=0.38,
        mat="R1_BareSteel",
        axis_u="X",
        segments=10,
        bevel=0.0015,
        rot_deg_u=pole_rot,
    )
    # Crossbar center telescoping latch ferrule and fly-end retention cap
    pole_mb.add_cylinder(
        center_u=tp(0.48, 1.482, 0.0),
        radius=0.0125,
        length=0.024,
        mat="R1_Brass",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    pole_mb.add_cylinder(
        center_u=tp(0.862, 1.482, 0.0),
        radius=0.011,
        length=0.020,
        mat="R1_Brass",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    # Diagonal deployment lock strut under hinge
    pole_mb.add_strut(
        p0_u=tp(0.008, 1.365, 0.0),
        p1_u=tp(0.135, 1.480, 0.0),
        width=0.012,
        thickness=0.012,
        mat="R1_BareSteel",
        bevel=0.002,
    )
    pole_mb.build_object(parent=root)

    # 2. FlagSheet: sun-bleached flag (almost white R1_CreamEnamel + faint R1_Signage stripes)
    # hanging straight down from the horizontal top rod, rigid vacuum hang with vertical storage pleats
    flag_mb = r1kit.MeshBuilder("FlagSheet", pivot_u=tp(0.44, 1.20, 0.0))

    # Vertical accordion storage pleats along X (because the horizontal rod jammed slightly on deployment)
    x_cols = [0.032, 0.135, 0.245, 0.355, 0.470, 0.585, 0.695, 0.785, 0.858]
    z_pleats = [0.000, 0.014, -0.012, 0.015, -0.014, 0.016, -0.015, 0.012, -0.005]

    _add_pleated_flag_slab(
        flag_mb,
        x_cols=x_cols,
        z_pleats=z_pleats,
        y_bot=0.955,
        y_top=1.465,
        half_thick=0.0045,
        mat="R1_CreamEnamel",
        rot_deg_u=pole_rot,
        origin_u=pole_origin,
    )

    # Top horizontal rod fabric sleeve tube + hoist vertical hem sleeve
    flag_mb.add_cylinder(
        center_u=tp(0.445, 1.480, 0.0),
        radius=0.014,
        length=0.815,
        mat="R1_CreamEnamel",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )
    flag_mb.add_cylinder(
        center_u=tp(0.018, 1.210, 0.0),
        radius=0.0165,
        length=0.515,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=10,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )

    # Faint sun-bleached horizontal stripes across both faces of the flag (5 stripes in R1_Signage)
    # Upper 3 stripes start after the canton (x = 0.365..0.848 m); lower 2 stripes span full width (x = 0.048..0.848 m)
    stripe_specs = [
        (1.425, 0.365, 0.848, 0.030),
        (1.335, 0.365, 0.848, 0.030),
        (1.245, 0.365, 0.848, 0.030),
        (1.135, 0.048, 0.848, 0.032),
        (1.015, 0.048, 0.848, 0.032),
    ]
    for y_s, x0, x1, h_s in stripe_specs:
        # Subdivide each stripe along the pleated columns so it hugs the pleated flag sheet cleanly
        for c_i in range(len(x_cols) - 1):
            xa = max(x0, x_cols[c_i])
            xb = min(x1, x_cols[c_i + 1])
            if xb - xa < 0.012:
                continue
            # Interpolate pleat Z at endpoints
            ta = (xa - x_cols[c_i]) / (x_cols[c_i + 1] - x_cols[c_i])
            tb = (xb - x_cols[c_i]) / (x_cols[c_i + 1] - x_cols[c_i])
            ty = (y_s - 0.955) / (1.465 - 0.955)
            pleat_scale = 0.35 + 0.65 * math.sin(ty * math.pi * 0.85)
            za = (z_pleats[c_i] + (z_pleats[c_i + 1] - z_pleats[c_i]) * ta) * pleat_scale
            zb = (z_pleats[c_i] + (z_pleats[c_i + 1] - z_pleats[c_i]) * tb) * pleat_scale
            tx_mid = 0.5 * (xa + xb) / 0.858
            y_sag = -0.008 * (tx_mid ** 1.6) * (1.0 - 0.35 * ty)
            flag_mb.add_strut(
                p0_u=tp(xa, y_s + y_sag, za),
                p1_u=tp(xb, y_s + y_sag, zb),
                width=h_s,
                thickness=0.0125,
                mat="R1_Signage",
                bevel=0.0015,
            )

    # Bleached canton border frame & faint Lumen lunar-orb emblem on upper hoist corner (x=0.055..0.345, y=1.22..1.44)
    canton_x = 0.195
    canton_y = 1.330
    flag_mb.add_bevel_box(
        center_u=tp(canton_x, canton_y, 0.001),
        size_u=(0.275, 0.205, 0.013),
        mat="R1_Signage",
        bevel=0.002,
        segments=1,
        rot_deg_u=pole_rot,
    )
    # Inner bleached field of canton + raised bleached lunar emblem disc
    flag_mb.add_bevel_box(
        center_u=tp(canton_x, canton_y, 0.001),
        size_u=(0.245, 0.175, 0.0145),
        mat="R1_CreamEnamel",
        bevel=0.002,
        segments=1,
        rot_deg_u=pole_rot,
    )
    flag_mb.add_cylinder(
        center_u=tp(canton_x, canton_y, 0.001),
        radius=0.052,
        length=0.016,
        mat="R1_Signage",
        axis_u="Z",
        segments=14,
        bevel=0.002,
        rot_deg_u=pole_rot,
    )

    flag_obj = flag_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Flag_Anchor", parent=root, pivot_u=tp(0.44, 1.20, 0.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.55)

    # Boost sun-bleached vertex color (high R=AO and high G=convex wear -> chalky almost-white bleached fabric,
    # with subtle B=dust variation on the faint stripes so they read softly without harsh contrast)
    if flag_obj.data is not None and "Col" in flag_obj.data.color_attributes:
        attr = flag_obj.data.color_attributes["Col"]
        for poly in flag_obj.data.polygons:
            is_stripe = poly.material_index > 0
            for loop_idx in poly.loop_indices:
                r, g, b, a = attr.data[loop_idx].color
                if is_stripe:
                    # Faint sun-bleached stripe: slightly dustier warm grey-cream
                    attr.data[loop_idx].color = (
                        max(0.86, min(1.0, r * 0.96)),
                        max(0.58, min(1.0, g * 0.75 + 0.35)),
                        max(0.32, min(1.0, b * 0.65 + 0.28)),
                        a,
                    )
                else:
                    # Almost-white sun-bleached flag cloth
                    attr.data[loop_idx].color = (
                        max(0.94, min(1.0, r * 0.98 + 0.08)),
                        max(0.88, min(1.0, g * 0.5 + 0.72)),
                        max(0.08, min(0.28, b * 0.35)),
                        a,
                    )

    fbx_path = PROPS_DIR / "Prop_MissionFlag.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_crinkled_foil_shroud(
    mb: r1kit.MeshBuilder,
    center_xz: Tuple[float, float],
    mat: str = "R1_GoldFoil",
) -> None:
    """Build a closed manifold crinkled Apollo-ALSEP circular thermal shroud in R1_GoldFoil."""
    bm = mb.bm
    mat_idx = mb._mat_index(mat)
    cx, cz = center_xz
    n_seg = 24

    # Profile from bottom center -> outer skirt -> stepped mid-skirt -> turret wall -> top inner rim -> top center
    # (radius, y_u, crinkle_r_amp, crinkle_y_amp)
    profile = [
        (0.0, -0.004, 0.0, 0.0),
        (0.285, 0.003, 0.014, 0.005),
        (0.288, 0.022, 0.016, 0.009),
        (0.225, 0.048, 0.015, 0.011),
        (0.162, 0.068, 0.012, 0.008),
        (0.150, 0.155, 0.010, 0.007),
        (0.142, 0.235, 0.008, 0.005),
        (0.0, 0.235, 0.0, 0.0),
    ]

    rings: List[List[bmesh.types.BMVert]] = []
    for r_idx, (rad, y_u, amp_r, amp_y) in enumerate(profile):
        if rad <= 1e-5:
            rings.append([bm.verts.new(mb._to_local_b((cx, y_u, cz)))])
        else:
            ring_verts: List[bmesh.types.BMVert] = []
            for s in range(n_seg):
                th = (2.0 * math.pi * s) / n_seg
                # Multi-harmonic deterministic Kapton crinkle facets
                cr_r = amp_r * (
                    0.55 * math.sin(7.0 * th + 1.3 * r_idx)
                    + 0.45 * math.cos(11.0 * th - 2.1 * r_idx)
                )
                cr_y = amp_y * (
                    0.60 * math.cos(9.0 * th + 0.9 * r_idx)
                    + 0.40 * math.sin(5.0 * th - 1.7 * r_idx)
                )
                r_eff = max(0.02, rad + cr_r)
                y_eff = y_u + cr_y
                px = cx + r_eff * math.cos(th)
                pz = cz + r_eff * math.sin(th)
                ring_verts.append(bm.verts.new(mb._to_local_b((px, y_eff, pz))))
            rings.append(ring_verts)

    faces: List[bmesh.types.BMFace] = []
    for k in range(len(rings) - 1):
        r0 = rings[k]
        r1 = rings[k + 1]
        if len(r0) == 1:
            pole = r0[0]
            for s in range(n_seg):
                sn = (s + 1) % n_seg
                faces.append(bm.faces.new([pole, r1[sn], r1[s]]))
        elif len(r1) == 1:
            pole = r1[0]
            for s in range(n_seg):
                sn = (s + 1) % n_seg
                faces.append(bm.faces.new([r0[s], r0[sn], pole]))
        else:
            for s in range(n_seg):
                sn = (s + 1) % n_seg
                faces.append(bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    r1kit.ensure_outward_normals(bm, per_shell=True)
    for f in faces:
        if f.is_valid:
            f.material_index = mat_idx
            # Keep crinkled Kapton foil flat-faceted so it catches sharp specular glints in grazing light
            f.smooth = False


def build_seismic_station() -> bpy.types.Object:
    """Build Deliverable 2: Prop_SeismicStation (~1.22 W x 0.60 D x 0.44 H m).

    Small Apollo-ALSEP-style seismometer under a crinkled foil shroud (R1_GoldFoil),
    a cable to a squat central box with a dead status lamp.
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_SeismicStation", pivot_u=(0.0, 0.0, 0.0))

    sx_center = -0.34
    sz_center = 0.0
    bx_center = 0.36
    bz_center = 0.0

    # 1. FoilShroud: crinkled R1_GoldFoil thermal blanket skirt & turret + ballast hold-down tabs
    shroud_mb = r1kit.MeshBuilder("FoilShroud", pivot_u=(sx_center, 0.0, sz_center))
    _add_crinkled_foil_shroud(shroud_mb, (sx_center, sz_center), mat="R1_GoldFoil")

    # 4 radial hold-down tabs (BareSteel) + regolith dust clumps around the skirt perimeter
    for i in range(4):
        ang = math.radians(45.0 + 90.0 * i)
        tx = sx_center + 0.255 * math.cos(ang)
        tz = sz_center + 0.255 * math.sin(ang)
        shroud_mb.add_bevel_box(
            center_u=(tx, 0.024, tz),
            size_u=(0.065, 0.018, 0.036),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=(0.0, -math.degrees(ang), 0.0),
        )
        rx = sx_center + 0.295 * math.cos(ang + 0.35)
        rz = sz_center + 0.295 * math.sin(ang + 0.35)
        shroud_mb.add_cylinder(
            center_u=(rx, 0.008, rz),
            radius=0.038,
            radius_top=0.022,
            length=0.024,
            mat="R1_Basalt",
            axis_u="Y",
            segments=8,
            bevel=0.003,
        )
    shroud_mb.build_object(parent=root)

    # 2. SeismometerUnit: upper cylindrical sensor head peeking out of the gold-foil shroud (SilverFoil + BareSteel, no cream/orange)
    seis_mb = r1kit.MeshBuilder("SeismometerUnit", pivot_u=(sx_center, 0.24, sz_center))
    # Main aluminised-film wrapped sensor cylinder (y = 0.22..0.325 m)
    seis_mb.add_cylinder(
        center_u=(sx_center, 0.272, sz_center),
        radius=0.118,
        length=0.105,
        mat="R1_SilverFoil",
        axis_u="Y",
        segments=16,
        bevel=0.005,
    )
    # Upper machined bare-aluminium radiator bezel ring
    seis_mb.add_cylinder(
        center_u=(sx_center, 0.328, sz_center),
        radius=0.124,
        length=0.022,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=16,
        bevel=0.003,
    )
    # Bullseye spirit-level bezel (R1_Brass) + bubble sight glass (R1_DarkGlass)
    seis_mb.add_cylinder(
        center_u=(sx_center - 0.032, 0.342, sz_center + 0.028),
        radius=0.028,
        length=0.016,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
    )
    seis_mb.add_cylinder(
        center_u=(sx_center - 0.032, 0.346, sz_center + 0.028),
        radius=0.020,
        length=0.012,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
    )
    # Solar alignment shadow gnomon pin + bare-aluminium calibration scale plate on top
    seis_mb.add_cylinder(
        center_u=(sx_center + 0.035, 0.365, sz_center - 0.020),
        radius=0.006,
        length=0.062,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.001,
    )
    seis_mb.add_bevel_box(
        center_u=(sx_center + 0.035, 0.341, sz_center - 0.020),
        size_u=(0.064, 0.010, 0.048),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
    )
    # Folded EVA deployment handle hoop
    seis_mb.add_tube_path(
        points_u=[
            (sx_center - 0.11, 0.315, sz_center),
            (sx_center - 0.13, 0.375, sz_center),
            (sx_center, 0.392, sz_center),
            (sx_center + 0.13, 0.375, sz_center),
            (sx_center + 0.11, 0.315, sz_center),
        ],
        radius=0.007,
        mat="R1_BareSteel",
        segments=8,
        cap_ends=True,
    )
    seis_mb.build_object(parent=root)

    # 3. UmbilicalCable: S-curve cable from seismometer shroud (-0.16, 0.025, 0.0) to central box (+0.16, 0.055, 0.0)
    cable_mb = r1kit.MeshBuilder("UmbilicalCable", pivot_u=(0.0, 0.02, 0.0))
    cable_pts = [
        (-0.20, 0.042, 0.01),
        (-0.14, 0.020, 0.035),
        (-0.06, 0.016, 0.065),
        (0.02, 0.016, 0.015),
        (0.09, 0.018, -0.045),
        (0.15, 0.036, -0.020),
        (0.19, 0.068, 0.00),
    ]
    cable_mb.add_tube_path(
        points_u=cable_pts,
        radius=0.016,
        mat="R1_Rubber",
        segments=10,
        cap_ends=True,
    )
    # Strain-relief gland at seismometer exit & central box entry
    cable_mb.add_cylinder(
        center_u=(-0.185, 0.036, 0.018),
        radius=0.022,
        length=0.048,
        mat="R1_BareSteel",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=(0.0, -18.0, -15.0),
    )
    cable_mb.add_cylinder(
        center_u=(0.175, 0.058, -0.006),
        radius=0.024,
        length=0.048,
        mat="R1_Brass",
        axis_u="X",
        segments=10,
        bevel=0.002,
        rot_deg_u=(0.0, -12.0, 24.0),
    )
    # Bare-metal cable crimp ferrule & regolith dust mound over the middle of the cable
    cable_mb.add_cylinder(
        center_u=(-0.06, 0.017, 0.065),
        radius=0.019,
        length=0.028,
        mat="R1_BareSteel",
        axis_u="X",
        segments=10,
        bevel=0.002,
    )
    cable_mb.add_bevel_box(
        center_u=(0.02, 0.008, 0.015),
        size_u=(0.11, 0.022, 0.085),
        mat="R1_Basalt",
        bevel=0.005,
        segments=1,
        top_scale_xz=(0.68, 0.68),
    )
    cable_mb.build_object(parent=root)

    # 4. CentralBox: 1971 Era-S electronics box (WhitePaint top, SilverFoil reflector skirt, BareSteel legs & handles, no orange)
    #    plus a small 1980s Lumen junction box (CreamEnamel + one OrangeStripe band) bolted to its -Z side.
    box_mb = r1kit.MeshBuilder("CentralBox", pivot_u=(bx_center, 0.0, bz_center))
    # Lower bare-aluminium skid frame + 4 short leg struts + 4 footpads sunk slightly in regolith
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.022, bz_center),
        size_u=(0.44, 0.048, 0.36),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
    )
    for fx in (-0.19, 0.19):
        for fz in (-0.15, 0.15):
            box_mb.add_cylinder(
                center_u=(bx_center + fx, -0.002, bz_center + fz),
                radius=0.036,
                length=0.018,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=10,
                bevel=0.002,
            )
            box_mb.add_cylinder(
                center_u=(bx_center + fx * 0.92, 0.032, bz_center + fz * 0.90),
                radius=0.014,
                length=0.048,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=8,
                bevel=0.002,
            )

    # Crinkled SilverFoil reflector skirt around the lower/mid body of the 1971 box (y = 0.045..0.205 m)
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.125, bz_center),
        size_u=(0.40, 0.16, 0.32),
        mat="R1_SilverFoil",
        bevel=0.010,
        segments=2,
        top_scale_xz=(0.95, 0.94),
    )
    # Chalky cool-white (R1_WhitePaint) upper thermal-control deck of the 1971 box (y = 0.200..0.285 m)
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.242, bz_center),
        size_u=(0.384, 0.086, 0.304),
        mat="R1_WhitePaint",
        bevel=0.008,
        segments=2,
        top_scale_xz=(0.96, 0.95),
    )
    # Bare-aluminium EVA carry handles on the -X and +X upper sides of the 1971 box
    for sx in (-1.0, 1.0):
        hx = bx_center + sx * 0.195
        box_mb.add_tube_path(
            points_u=[
                (hx, 0.225, bz_center - 0.07),
                (hx + sx * 0.025, 0.245, bz_center - 0.06),
                (hx + sx * 0.025, 0.245, bz_center + 0.06),
                (hx, 0.225, bz_center + 0.07),
            ],
            radius=0.0065,
            mat="R1_BareSteel",
            segments=8,
            cap_ends=True,
        )

    # Gold-foil thermal blanket patch on rear/top half of central box + bare-metal radiator louver deck
    box_mb.add_bevel_box(
        center_u=(bx_center + 0.04, 0.286, bz_center + 0.04),
        size_u=(0.24, 0.018, 0.16),
        mat="R1_GoldFoil",
        bevel=0.003,
        segments=1,
    )
    for lx in (-0.06, 0.0, 0.06, 0.12):
        box_mb.add_bevel_box(
            center_u=(bx_center + lx, 0.296, bz_center + 0.04),
            size_u=(0.024, 0.008, 0.13),
            mat="R1_BareSteel",
            bevel=0.0015,
            segments=1,
        )
    # Front-right 1971 instrument bezel & toggle switch guard (shifted +X so front-left holds the Lumen junction box)
    box_mb.add_bevel_box(
        center_u=(bx_center + 0.06, 0.145, bz_center - 0.158),
        size_u=(0.16, 0.10, 0.022),
        mat="R1_BareSteel",
        bevel=0.003,
        segments=1,
    )
    box_mb.add_cylinder(
        center_u=(bx_center + 0.02, 0.145, bz_center - 0.172),
        radius=0.014,
        length=0.018,
        mat="R1_Brass",
        axis_u="Z",
        segments=10,
        bevel=0.002,
    )
    box_mb.add_cylinder(
        center_u=(bx_center + 0.09, 0.145, bz_center - 0.172),
        radius=0.018,
        length=0.014,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=10,
        bevel=0.002,
    )

    # Small 1980s Lumen junction box (CreamEnamel with one coral OrangeStripe band) bolted to CentralBox's front-left (-Z) side
    jbox_x = bx_center - 0.10
    jbox_y = 0.170
    jbox_z = bz_center - 0.192
    # BareSteel mounting flange plate bolted to CentralBox's side wall
    box_mb.add_bevel_box(
        center_u=(jbox_x, jbox_y, bz_center - 0.160),
        size_u=(0.135, 0.125, 0.016),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
    )
    for b_dx in (-0.055, 0.055):
        for b_dy in (-0.048, 0.048):
            box_mb.add_cylinder(
                center_u=(jbox_x + b_dx, jbox_y + b_dy, bz_center - 0.170),
                radius=0.005,
                length=0.010,
                mat="R1_BareSteel",
                axis_u="Z",
                segments=6,
                bevel=0.001,
            )
    # Warm-cream enameled Lumen retrofit junction box body (y = 0.115..0.225 m)
    box_mb.add_bevel_box(
        center_u=(jbox_x, jbox_y, jbox_z),
        size_u=(0.115, 0.110, 0.068),
        mat="R1_CreamEnamel",
        bevel=0.006,
        segments=1,
    )
    # Single coral-orange (R1_OrangeStripe) identification band around the Lumen junction box
    box_mb.add_bevel_box(
        center_u=(jbox_x, jbox_y + 0.018, jbox_z),
        size_u=(0.119, 0.022, 0.072),
        mat="R1_OrangeStripe",
        bevel=0.002,
        segments=1,
    )

    # Short telemetry whip stub on rear-right corner (y = 0.28..0.43 m)
    box_mb.add_cylinder(
        center_u=(bx_center + 0.13, 0.305, bz_center + 0.09),
        radius=0.022,
        length=0.042,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    box_mb.add_cylinder(
        center_u=(bx_center + 0.13, 0.375, bz_center + 0.09),
        radius=0.007,
        length=0.11,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0015,
    )
    # Copper grounding strap on +X side
    box_mb.add_strut(
        p0_u=(bx_center + 0.20, 0.065, bz_center),
        p1_u=(bx_center + 0.245, 0.002, bz_center),
        width=0.024,
        thickness=0.008,
        mat="R1_Copper",
        bevel=0.0015,
    )
    box_mb.build_object(parent=root)

    # 5. DeadStatusLamp: dead status lamp mounted on top of the side-bolted Lumen junction box (R1_DarkGlass, no R1_LampAmber)
    lamp_pos = (jbox_x, 0.232, jbox_z)
    lamp_mb = r1kit.MeshBuilder("DeadStatusLamp", pivot_u=lamp_pos)
    # Armored cage base & brass ring
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1], lamp_pos[2]),
        radius=0.032,
        length=0.022,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.003,
    )
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1] + 0.015, lamp_pos[2]),
        radius=0.027,
        length=0.012,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
    )
    # Unlit dark glass dome lens
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1] + 0.032, lamp_pos[2]),
        radius=0.022,
        radius_top=0.014,
        length=0.028,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=12,
        bevel=0.004,
    )
    lamp_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Seismometer_Anchor", parent=root, pivot_u=(sx_center, 0.0, sz_center))
    r1kit.create_empty("CentralBox_Anchor", parent=root, pivot_u=(bx_center, 0.0, bz_center))
    r1kit.create_empty("StatusLamp_Dead", parent=root, pivot_u=(lamp_pos[0], lamp_pos[1] + 0.032, lamp_pos[2]))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.65)

    fbx_path = PROPS_DIR / "Prop_SeismicStation.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_corner_cube_prism_array_3x3(
    mb: r1kit.MeshBuilder,
    panel_origin_u: Tuple[float, float, float],
    panel_rot_u: Tuple[float, float, float],
    grid_span: float = 0.416,
    slab_back_z: float = 0.010,
    front_z: float = -0.018,
) -> List[Tuple[int, int, str]]:
    """Build a 100% closed manifold 3x3 array of large corner-cube optical prisms with broad glassy facets."""
    bm = mb.bm
    glass_idx = mb._mat_index("R1_DarkGlass")
    steel_idx = mb._mat_index("R1_BareSteel")

    n_cells = 3
    half = grid_span * 0.5
    step = grid_span / n_cells

    front_rim: List[List[bmesh.types.BMVert]] = []
    back_rim: List[List[bmesh.types.BMVert]] = []

    for r in range(n_cells + 1):
        y_loc = -half + r * step
        f_row: List[bmesh.types.BMVert] = []
        b_row: List[bmesh.types.BMVert] = []
        for c in range(n_cells + 1):
            x_loc = -half + c * step
            pf_u = _rotate_u(
                (panel_origin_u[0] + x_loc, panel_origin_u[1] + y_loc, panel_origin_u[2] + front_z),
                panel_rot_u,
                panel_origin_u,
            )
            pb_u = _rotate_u(
                (panel_origin_u[0] + x_loc, panel_origin_u[1] + y_loc, panel_origin_u[2] + slab_back_z),
                panel_rot_u,
                panel_origin_u,
            )
            f_row.append(bm.verts.new(mb._to_local_b(pf_u)))
            b_row.append(bm.verts.new(mb._to_local_b(pb_u)))
        front_rim.append(f_row)
        back_rim.append(b_row)

    # Per-cell asymmetric optical table offsets (ox, oy) and corner cant heights (dz00, dz10, dz11, dz01)
    # so the 9 large prisms have varied facet angles and non-repeating light reflections.
    cell_variations = [
        # Row 0 (bottom)
        (-0.004, -0.003, -0.0065, -0.0085, -0.0105, -0.0075, True),
        (0.003, -0.004, -0.0080, -0.0065, -0.0095, -0.0110, False),
        (-0.003, 0.004, -0.0095, -0.0115, -0.0075, -0.0065, True),
        # Row 1 (middle)
        (0.004, 0.002, -0.0070, -0.0105, -0.0085, -0.0065, False),
        (-0.002, -0.003, -0.0115, -0.0080, -0.0070, -0.0105, True),
        (0.003, -0.002, -0.0065, -0.0095, -0.0115, -0.0080, False),
        # Row 2 (top)
        (-0.003, 0.003, -0.0090, -0.0070, -0.0105, -0.0080, False),
        (0.002, 0.004, -0.0075, -0.0110, -0.0090, -0.0065, True),
        (-0.004, -0.002, -0.0110, -0.0085, -0.0070, -0.0100, False),
    ]

    poly_meta: List[Tuple[int, int, str]] = []
    thalf = step * 0.285

    for r in range(n_cells):
        for c in range(n_cells):
            v00 = front_rim[r][c]
            v10 = front_rim[r][c + 1]
            v11 = front_rim[r + 1][c + 1]
            v01 = front_rim[r + 1][c]

            idx = r * n_cells + c
            ox, oy, dz00, dz10, dz11, dz01, flip_diag = cell_variations[idx]
            cx_loc = -half + (c + 0.5) * step + ox
            cy_loc = -half + (r + 0.5) * step + oy

            table_pts = [
                (cx_loc - thalf, cy_loc - thalf, front_z + dz00),
                (cx_loc + thalf, cy_loc - thalf, front_z + dz10),
                (cx_loc + thalf, cy_loc + thalf, front_z + dz11),
                (cx_loc - thalf, cy_loc + thalf, front_z + dz01),
            ]
            t_verts: List[bmesh.types.BMVert] = []
            for tx, ty, tz in table_pts:
                pt_u = _rotate_u(
                    (panel_origin_u[0] + tx, panel_origin_u[1] + ty, panel_origin_u[2] + tz),
                    panel_rot_u,
                    panel_origin_u,
                )
                t_verts.append(bm.verts.new(mb._to_local_b(pt_u)))
            t00, t10, t11, t01 = t_verts

            # 4 wide chamfered corner-cube bevel facets + 2 large canted central optical table facets
            f_bot = bm.faces.new([v00, t00, t10, v10])
            f_right = bm.faces.new([v10, t10, t11, v11])
            f_top = bm.faces.new([v11, t11, t01, v01])
            f_left = bm.faces.new([v01, t01, t00, v00])
            if flip_diag:
                f_tab1 = bm.faces.new([t00, t11, t10])
                f_tab2 = bm.faces.new([t00, t01, t11])
            else:
                f_tab1 = bm.faces.new([t00, t01, t10])
                f_tab2 = bm.faces.new([t10, t01, t11])

            front_facets = [
                (f_bot, "bot"),
                (f_right, "right"),
                (f_top, "top"),
                (f_left, "left"),
                (f_tab1, "tab1"),
                (f_tab2, "tab2"),
            ]
            for pf, fkind in front_facets:
                pf.material_index = glass_idx
                pf.smooth = False
                poly_meta.append((r, c, fkind))

            # Back face quad (+Z)
            fb = bm.faces.new([back_rim[r][c], back_rim[r + 1][c], back_rim[r + 1][c + 1], back_rim[r][c + 1]])
            fb.material_index = steel_idx
            fb.smooth = False
            poly_meta.append((r, c, "back"))

    # Outer perimeter walls closing front_rim to back_rim
    for idx in range(n_cells):
        f1 = bm.faces.new([front_rim[0][idx], front_rim[0][idx + 1], back_rim[0][idx + 1], back_rim[0][idx]])
        f2 = bm.faces.new(
            [back_rim[n_cells][idx], back_rim[n_cells][idx + 1], front_rim[n_cells][idx + 1], front_rim[n_cells][idx]]
        )
        f3 = bm.faces.new([back_rim[idx][0], back_rim[idx + 1][0], front_rim[idx + 1][0], front_rim[idx][0]])
        f4 = bm.faces.new(
            [front_rim[idx][n_cells], front_rim[idx + 1][n_cells], back_rim[idx + 1][n_cells], back_rim[idx][n_cells]]
        )
        for fw in (f1, f2, f3, f4):
            fw.material_index = steel_idx
            fw.smooth = False
            poly_meta.append((0, 0, "wall"))

    r1kit.ensure_outward_normals(bm, per_shell=True)
    return poly_meta


def build_retroreflector() -> bpy.types.Object:
    """Build Deliverable 3: Prop_Retroreflector (~0.66 W x 0.62 D x 0.56 H m).

    Tilted panel of 3x3 large corner-cube optical prisms with visible glassy facets and varied wear/dust
    on a bare-aluminium stand with GoldFoil on the rear face only.
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_Retroreflector", pivot_u=(0.0, 0.0, 0.0))

    panel_piv = (0.0, 0.30, 0.0)
    # Tilted 38 deg toward -Z / +Y so the 3x3 prism face aims toward Earth & catches low grazing sun
    panel_rot = (-38.0, 0.0, 0.0)

    def rp(x: float, y: float, z: float) -> Tuple[float, float, float]:
        return _rotate_u((panel_piv[0] + x, panel_piv[1] + y, panel_piv[2] + z), panel_rot, panel_piv)

    # 1. SupportStand: tubular bare-aluminium A-frame legs, circular footpads in regolith, elevation trunnions, spirit level
    stand_mb = r1kit.MeshBuilder("SupportStand", pivot_u=(0.0, 0.0, 0.0))

    # 4 splayed tubular legs from the side trunnions (-0.27 / +0.27, 0.30, 0.0) down to circular footpads
    foot_coords = [
        (-0.29, -0.23),
        (0.29, -0.23),
        (-0.29, 0.23),
        (0.29, 0.23),
    ]
    for fx, fz in foot_coords:
        # Circular footpad plate + central ball-joint boss
        stand_mb.add_cylinder(
            center_u=(fx, 0.008, fz),
            radius=0.044,
            radius_top=0.036,
            length=0.016,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.002,
        )
        stand_mb.add_cylinder(
            center_u=(fx, 0.022, fz),
            radius=0.016,
            length=0.018,
            mat="R1_Brass",
            axis_u="Y",
            segments=10,
            bevel=0.002,
        )
        # Regolith dust ring partially burying each footpad
        stand_mb.add_cylinder(
            center_u=(fx, 0.003, fz),
            radius=0.056,
            radius_top=0.040,
            length=0.014,
            mat="R1_Basalt",
            axis_u="Y",
            segments=10,
            bevel=0.003,
        )
        # Inclined leg strut up to the side trunnion hub
        sx_hub = -0.265 if fx < 0 else 0.265
        stand_mb.add_pipe(
            p0_u=(fx, 0.020, fz),
            p1_u=(sx_hub, 0.295, 0.0),
            radius=0.012,
            mat="R1_BareSteel",
            segments=10,
        )

    # Lower horizontal spreader bars (left, right, and rear/front cross-braces — all R1_BareSteel, no cream/orange)
    for sx in (-1.0, 1.0):
        stand_mb.add_pipe(
            p0_u=(sx * 0.28, 0.11, -0.155),
            p1_u=(sx * 0.28, 0.11, 0.155),
            radius=0.010,
            mat="R1_BareSteel",
            segments=8,
        )
    stand_mb.add_pipe(
        p0_u=(-0.28, 0.11, 0.145),
        p1_u=(0.28, 0.11, 0.145),
        radius=0.010,
        mat="R1_BareSteel",
        segments=8,
    )
    stand_mb.add_pipe(
        p0_u=(-0.28, 0.11, -0.145),
        p1_u=(0.28, 0.11, -0.145),
        radius=0.010,
        mat="R1_BareSteel",
        segments=8,
    )

    # Side elevation trunnion hubs & Brass vernier locking knobs at x = +/-0.275, y = 0.30, z = 0.0
    for sx in (-1.0, 1.0):
        stand_mb.add_cylinder(
            center_u=(sx * 0.268, 0.30, 0.0),
            radius=0.036,
            length=0.032,
            mat="R1_BareSteel",
            axis_u="X",
            segments=14,
            bevel=0.003,
        )
        stand_mb.add_cylinder(
            center_u=(sx * 0.292, 0.30, 0.0),
            radius=0.024,
            length=0.022,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.002,
        )

    # Leveling bullseye spirit bubble & sun-compass plate on the right lower spreader bar
    stand_mb.add_bevel_box(
        center_u=(0.20, 0.122, -0.145),
        size_u=(0.075, 0.018, 0.056),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
    )
    stand_mb.add_cylinder(
        center_u=(0.20, 0.134, -0.145),
        radius=0.018,
        length=0.010,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.0015,
    )
    stand_mb.add_cylinder(
        center_u=(0.20, 0.138, -0.145),
        radius=0.012,
        length=0.008,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=10,
        bevel=0.001,
    )
    stand_mb.build_object(parent=root)

    # 2. PanelHousing: machined bare-aluminium (R1_BareSteel) optical array frame, GoldFoil thermal backing on rear face only
    housing_mb = r1kit.MeshBuilder("PanelHousing", pivot_u=panel_piv)

    # Rear machined bare-aluminium backing plate behind the prism array (z_local = +0.010 to +0.038 m)
    housing_mb.add_bevel_box(
        center_u=rp(0.0, 0.0, 0.024),
        size_u=(0.49, 0.49, 0.028),
        mat="R1_BareSteel",
        bevel=0.006,
        segments=1,
        rot_deg_u=panel_rot,
    )
    # Gold-foil multi-layer thermal blanket covering the rear face of the panel (z_local = +0.042 m)
    housing_mb.add_bevel_box(
        center_u=rp(0.0, 0.0, 0.042),
        size_u=(0.43, 0.43, 0.014),
        mat="R1_GoldFoil",
        bevel=0.004,
        segments=1,
        rot_deg_u=panel_rot,
    )

    # 4 perimeter bezel rails & stray-light sunshade hood framing the 0.42x0.42 m 3x3 prism cavity
    # Left & Right rails
    for sx in (-1.0, 1.0):
        housing_mb.add_bevel_box(
            center_u=rp(sx * 0.230, 0.0, -0.012),
            size_u=(0.040, 0.50, 0.056),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
            rot_deg_u=panel_rot,
        )
        # Inner anti-glare baffle lip
        housing_mb.add_bevel_box(
            center_u=rp(sx * 0.214, 0.0, -0.026),
            size_u=(0.014, 0.43, 0.034),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=panel_rot,
        )
    # Top & Bottom rails
    for sy in (-1.0, 1.0):
        housing_mb.add_bevel_box(
            center_u=rp(0.0, sy * 0.230, -0.012),
            size_u=(0.42, 0.040, 0.056),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
            rot_deg_u=panel_rot,
        )
        housing_mb.add_bevel_box(
            center_u=rp(0.0, sy * 0.214, -0.026),
            size_u=(0.43, 0.014, 0.034),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=panel_rot,
        )

    # Machined bare-aluminium 3x3 divider lattice separating the 9 large corner-cube optical prisms
    div_offset = 0.416 / 6.0  # +/- 0.0693 m
    for d_coord in (-div_offset, div_offset):
        housing_mb.add_bevel_box(
            center_u=rp(d_coord, 0.0, -0.017),
            size_u=(0.013, 0.414, 0.012),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=panel_rot,
        )
        housing_mb.add_bevel_box(
            center_u=rp(0.0, d_coord, -0.017),
            size_u=(0.414, 0.013, 0.012),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=panel_rot,
        )
    # 4 machined retainer clamp bosses at the 3x3 divider intersections
    for dx in (-div_offset, div_offset):
        for dy in (-div_offset, div_offset):
            housing_mb.add_bevel_box(
                center_u=rp(dx, dy, -0.019),
                size_u=(0.024, 0.024, 0.014),
                mat="R1_BareSteel",
                bevel=0.002,
                segments=1,
                rot_deg_u=panel_rot,
            )

    # Settled regolith dust drifts in the lower-left and lower-center bezel trough for asymmetric Era-S weathering
    housing_mb.add_bevel_box(
        center_u=rp(-0.142, -0.188, -0.024),
        size_u=(0.115, 0.036, 0.014),
        mat="R1_Basalt",
        bevel=0.004,
        segments=1,
        top_scale_xz=(0.72, 0.65),
        rot_deg_u=panel_rot,
    )
    housing_mb.add_bevel_box(
        center_u=rp(0.025, -0.194, -0.023),
        size_u=(0.082, 0.026, 0.012),
        mat="R1_Basalt",
        bevel=0.003,
        segments=1,
        top_scale_xz=(0.70, 0.65),
        rot_deg_u=panel_rot,
    )

    # Upper solar-glare sunhood visor along the top (+Y_local) edge
    housing_mb.add_bevel_box(
        center_u=rp(0.0, 0.244, -0.052),
        size_u=(0.49, 0.016, 0.048),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
        rot_deg_u=panel_rot,
    )
    # Machined corner brackets & top alignment fiducial bar (all R1_BareSteel, no orange)
    housing_mb.add_bevel_box(
        center_u=rp(0.0, 0.236, -0.041),
        size_u=(0.18, 0.024, 0.012),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
        rot_deg_u=panel_rot,
    )
    for sx in (-1.0, 1.0):
        housing_mb.add_bevel_box(
            center_u=rp(sx * 0.232, -0.18, -0.038),
            size_u=(0.032, 0.065, 0.012),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=panel_rot,
        )

    # Top EVA gloved-hand carry handle bar
    housing_mb.add_tube_path(
        points_u=[
            rp(-0.09, 0.245, 0.01),
            rp(-0.09, 0.295, 0.01),
            rp(0.09, 0.295, 0.01),
            rp(0.09, 0.245, 0.01),
        ],
        radius=0.0085,
        mat="R1_BareSteel",
        segments=8,
        cap_ends=True,
    )
    housing_mb.build_object(parent=root)

    # 3. PrismArray: 3x3 = 9 large corner-cube optical prisms with broad glassy facets seated inside the frame
    prism_mb = r1kit.MeshBuilder("PrismArray", pivot_u=panel_piv)
    poly_meta = _add_corner_cube_prism_array_3x3(
        prism_mb,
        panel_origin_u=panel_piv,
        panel_rot_u=panel_rot,
        grid_span=0.416,
        slab_back_z=0.010,
        front_z=-0.018,
    )
    prism_obj = prism_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Array_Anchor", parent=root, pivot_u=rp(0.0, 0.0, -0.022))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.42)

    # Assign varied per-cell and per-facet vertex colors (R=AO, G=glassy facet glint/wear, B=settled regolith dust)
    # across the 3x3 large corner-cube prisms so every prism shows distinct glassy facets and organic wear/dust.
    cell_weathering = {
        (0, 0): (0.48, 0.62, 0.94),  # bottom-left: heavy settled regolith dust drift
        (0, 1): (0.64, 0.74, 0.68),  # bottom-mid: moderate dust & worn lower bevel
        (0, 2): (0.78, 0.85, 0.42),  # bottom-right: light dust, crisp corner glint
        (1, 0): (0.68, 0.78, 0.56),  # mid-left: weathered/crazed outer prism
        (1, 1): (0.94, 0.95, 0.08),  # center: clean high-glint optical prism
        (1, 2): (0.90, 0.86, 0.12),  # mid-right: deep glassy blue with bright table facet
        (2, 0): (0.76, 0.88, 0.30),  # top-left: sun-crazed upper corner prism
        (2, 1): (0.92, 0.80, 0.10),  # top-mid: dark glass with mid-tone bevel catch
        (2, 2): (0.96, 0.98, 0.04),  # top-right: brightest optical glint under sunhood
    }
    facet_offsets = {
        "tab1": (0.04, 0.14, -0.12),   # bright primary optical table facet
        "tab2": (-0.02, -0.14, 0.04),  # deeper contrasting optical table facet
        "bot": (0.02, 0.10, 0.12),     # lower bevel catching low sun + settled dust
        "left": (0.00, 0.04, 0.05),    # mid-tone left bevel facet
        "right": (-0.04, -0.22, 0.00), # deep dark-blue right bevel facet
        "top": (-0.06, -0.26, -0.06),  # shaded upper bevel under sunhood
    }
    if prism_obj.data is not None and r1kit.VCOL_ATTRIBUTE_NAME in prism_obj.data.color_attributes:
        attr = prism_obj.data.color_attributes[r1kit.VCOL_ATTRIBUTE_NAME]
        for poly_idx, poly in enumerate(prism_obj.data.polygons):
            if poly_idx >= len(poly_meta):
                break
            r_cell, c_cell, fkind = poly_meta[poly_idx]
            if fkind not in facet_offsets:
                continue
            base_r, base_g, base_b = cell_weathering.get((r_cell, c_cell), (0.85, 0.82, 0.15))
            dr, dg, db = facet_offsets[fkind]
            # Alternate facet highlight emphasis across cells so adjacent prisms never look identical
            if (r_cell + c_cell) % 2 == 1 and fkind in ("tab1", "tab2"):
                dg = -dg * 0.85
            col_r = max(0.32, min(1.0, base_r + dr))
            col_g = max(0.38, min(1.0, base_g + dg))
            col_b = max(0.0, min(1.0, base_b + db))
            for loop_idx in poly.loop_indices:
                attr.data[loop_idx].color = (col_r, col_g, col_b, 1.0)

    fbx_path = PROPS_DIR / "Prop_Retroreflector.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_intact_survey_leg(
    mb: r1kit.MeshBuilder,
    p_hub: Tuple[float, float, float],
    p_mid: Tuple[float, float, float],
    p_foot: Tuple[float, float, float],
    side_vec: Tuple[float, float, float],
) -> None:
    """Add a dual-rail telescoping survey tripod leg from p_hub through p_mid to p_foot."""
    sx, sy, sz = side_vec
    # Dual upper wooden/enamel survey leg rails (p_hub -> p_mid)
    for sign in (-1.0, 1.0):
        p0 = (p_hub[0] + sign * sx, p_hub[1] + sign * sy, p_hub[2] + sign * sz)
        p1 = (p_mid[0] + sign * sx * 0.85, p_mid[1] + sign * sy * 0.85, p_mid[2] + sign * sz * 0.85)
        mb.add_strut(p0, p1, width=0.022, thickness=0.018, mat="R1_CreamEnamel", bevel=0.002)

    # Orange safety marking band near upper third of leg
    p_band0 = (
        p_hub[0] * 0.65 + p_mid[0] * 0.35,
        p_hub[1] * 0.65 + p_mid[1] * 0.35,
        p_hub[2] * 0.65 + p_mid[2] * 0.35,
    )
    p_band1 = (
        p_hub[0] * 0.48 + p_mid[0] * 0.52,
        p_hub[1] * 0.48 + p_mid[1] * 0.52,
        p_hub[2] * 0.48 + p_mid[2] * 0.52,
    )
    mb.add_strut(p_band0, p_band1, width=0.056, thickness=0.024, mat="R1_OrangeStripe", bevel=0.002)

    # Mid-span telescoping clamp collar (R1_GreyMetal + R1_Brass cam lever)
    p_clamp_a = (
        p_mid[0] * 0.92 + p_hub[0] * 0.08,
        p_mid[1] * 0.92 + p_hub[1] * 0.08,
        p_mid[2] * 0.92 + p_hub[2] * 0.08,
    )
    p_clamp_b = (
        p_mid[0] * 0.92 + p_foot[0] * 0.08,
        p_mid[1] * 0.92 + p_foot[1] * 0.08,
        p_mid[2] * 0.92 + p_foot[2] * 0.08,
    )
    mb.add_strut(p_clamp_a, p_clamp_b, width=0.058, thickness=0.028, mat="R1_GreyMetal", bevel=0.003)
    mb.add_cylinder(
        center_u=(p_mid[0], p_mid[1] + 0.016, p_mid[2]),
        radius=0.015,
        length=0.020,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )

    # Lower telescoping tubular extension (p_mid -> p_foot)
    mb.add_pipe(p_mid, p_foot, radius=0.013, mat="R1_BareSteel", segments=10)

    # Pointed steel foot shoe & stepping spur at p_foot
    p_tip = (
        p_foot[0] + (p_foot[0] - p_mid[0]) * 0.14,
        max(0.006, p_foot[1] - 0.004),
        p_foot[2] + (p_foot[2] - p_mid[2]) * 0.14,
    )
    mb.add_strut(p_foot, p_tip, width=0.028, thickness=0.022, mat="R1_GreyMetal", bevel=0.002)


def build_survey_tripod() -> bpy.types.Object:
    """Build Deliverable 4: Prop_SurveyTripod (~1.32 W x 0.68 D x 0.38 H m).

    Fallen survey tripod with a theodolite head, one leg buckled.
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_SurveyTripod", pivot_u=(0.0, 0.0, 0.0))

    hub_pos = (-0.32, 0.155, 0.02)
    head_piv = (-0.47, 0.165, 0.02)
    knee_piv = (0.12, 0.235, -0.17)

    # 1. TripodFrame: tribrach crown hub + 2 intact splayed telescoping legs + regolith dust mounds
    frame_mb = r1kit.MeshBuilder("TripodFrame", pivot_u=(0.0, 0.0, 0.0))

    # Fallen tripod crown plate & hinge ears at hub_pos (pitched ~78 deg onto its side toward -X)
    crown_rot = (8.0, 0.0, 76.0)
    frame_mb.add_cylinder(
        center_u=hub_pos,
        radius=0.088,
        length=0.036,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=14,
        bevel=0.004,
        rot_deg_u=crown_rot,
    )
    frame_mb.add_cylinder(
        center_u=(-0.355, 0.160, 0.02),
        radius=0.072,
        length=0.028,
        mat="R1_Brass",
        axis_u="Y",
        segments=14,
        bevel=0.003,
        rot_deg_u=crown_rot,
    )

    # Intact Leg 1 (rear-bottom leg resting directly along the regolith, z > 0)
    _add_intact_survey_leg(
        frame_mb,
        p_hub=(-0.29, 0.125, 0.075),
        p_mid=(0.16, 0.052, 0.195),
        p_foot=(0.58, 0.018, 0.275),
        side_vec=(0.0, 0.016, 0.012),
    )
    # Regolith dust mound partially burying Leg 1's foot and mid-joint
    frame_mb.add_cylinder(
        center_u=(0.59, 0.006, 0.275),
        radius=0.068,
        radius_top=0.042,
        length=0.022,
        mat="R1_Basalt",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    # Intact Leg 2 (lower-center leg touching the dust, z ~ 0.02)
    _add_intact_survey_leg(
        frame_mb,
        p_hub=(-0.29, 0.095, -0.005),
        p_mid=(0.15, 0.036, 0.010),
        p_foot=(0.61, 0.016, 0.025),
        side_vec=(0.0, 0.0, 0.018),
    )
    frame_mb.add_cylinder(
        center_u=(0.62, 0.005, 0.025),
        radius=0.062,
        radius_top=0.038,
        length=0.020,
        mat="R1_Basalt",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    # Folding spreader bar linking the 2 intact legs near hub
    frame_mb.add_pipe(
        p0_u=(-0.04, 0.082, 0.14),
        p1_u=(-0.04, 0.062, 0.005),
        radius=0.008,
        mat="R1_BareSteel",
        segments=8,
    )
    frame_mb.build_object(parent=root)

    # 2. BuckledLeg: third survey leg visibly buckled upward/forward at a sharp dogleg knee (knee_piv)
    buckled_mb = r1kit.MeshBuilder("BuckledLeg", pivot_u=knee_piv)
    p_bhub = (-0.29, 0.195, -0.040)
    p_bknee = knee_piv
    p_bfoot = (0.49, 0.020, -0.255)

    # Upper segment (p_bhub -> p_bknee): dual rails splayed/twisted open at the buckled knee
    buckled_mb.add_strut(
        (p_bhub[0], p_bhub[1] + 0.015, p_bhub[2] - 0.010),
        (p_bknee[0] - 0.015, p_bknee[1] + 0.018, p_bknee[2] - 0.012),
        width=0.022,
        thickness=0.018,
        mat="R1_CreamEnamel",
        bevel=0.002,
    )
    buckled_mb.add_strut(
        (p_bhub[0], p_bhub[1] - 0.015, p_bhub[2] + 0.010),
        (p_bknee[0] - 0.010, p_bknee[1] - 0.014, p_bknee[2] + 0.016),
        width=0.022,
        thickness=0.018,
        mat="R1_CreamEnamel",
        bevel=0.002,
    )
    # Orange stripe band on upper buckled leg
    buckled_mb.add_strut(
        (-0.14, 0.210, -0.088),
        (-0.04, 0.222, -0.120),
        width=0.054,
        thickness=0.024,
        mat="R1_OrangeStripe",
        bevel=0.002,
    )

    # Torn/popped telescoping clamp housing at the buckled knee joint + bent BareSteel inner tube
    buckled_mb.add_bevel_box(
        center_u=p_bknee,
        size_u=(0.068, 0.046, 0.044),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
        rot_deg_u=(18.0, -22.0, -14.0),
    )
    buckled_mb.add_cylinder(
        center_u=(p_bknee[0] + 0.012, p_bknee[1] + 0.026, p_bknee[2] - 0.008),
        radius=0.016,
        length=0.024,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.002,
        rot_deg_u=(25.0, -15.0, -32.0),
    )
    # Kinked bare-steel inner telescoping rod bending sharply down from knee_piv to p_bfoot
    buckled_mb.add_tube_path(
        points_u=[
            (p_bknee[0] - 0.02, p_bknee[1] + 0.004, p_bknee[2]),
            (p_bknee[0] + 0.035, p_bknee[1] - 0.012, p_bknee[2] - 0.018),
            (0.28, 0.135, -0.215),
            p_bfoot,
        ],
        radius=0.0135,
        mat="R1_BareSteel",
        segments=10,
        cap_ends=True,
    )
    # Snapped spreader strut stub hanging from the buckled knee
    buckled_mb.add_pipe(
        p0_u=(p_bknee[0] - 0.03, p_bknee[1] - 0.015, p_bknee[2] + 0.01),
        p1_u=(0.04, 0.115, -0.085),
        radius=0.0075,
        mat="R1_BareSteel",
        segments=8,
    )
    # Pointed foot shoe at p_bfoot + regolith impact clod
    buckled_mb.add_bevel_box(
        center_u=(p_bfoot[0] + 0.022, p_bfoot[1], p_bfoot[2] - 0.010),
        size_u=(0.052, 0.026, 0.030),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
        rot_deg_u=(0.0, 18.0, -28.0),
    )
    buckled_mb.add_cylinder(
        center_u=(p_bfoot[0] + 0.018, 0.006, p_bfoot[2] - 0.008),
        radius=0.054,
        radius_top=0.035,
        length=0.018,
        mat="R1_Basalt",
        axis_u="Y",
        segments=10,
        bevel=0.003,
    )
    buckled_mb.build_object(parent=root)

    # 3. TheodoliteHead: fallen optical survey theodolite head attached to the tribrach at head_piv
    head_mb = r1kit.MeshBuilder("TheodoliteHead", pivot_u=head_piv)
    head_rot = (12.0, -10.0, 74.0)

    def hp(x: float, y: float, z: float) -> Tuple[float, float, float]:
        return _rotate_u((head_piv[0] + x, head_piv[1] + y, head_piv[2] + z), head_rot, head_piv)

    # Tribrach stage & 3 brass leveling thumbscrews
    head_mb.add_cylinder(
        center_u=hp(0.0, -0.075, 0.0),
        radius=0.070,
        length=0.032,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=14,
        bevel=0.003,
        rot_deg_u=head_rot,
    )
    for ang_deg in (0.0, 120.0, 240.0):
        ar = math.radians(ang_deg)
        head_mb.add_cylinder(
            center_u=hp(0.058 * math.cos(ar), -0.095, 0.058 * math.sin(ar)),
            radius=0.014,
            length=0.028,
            mat="R1_Brass",
            axis_u="Y",
            segments=10,
            bevel=0.002,
            rot_deg_u=head_rot,
        )

    # Azimuth vernier scale drum & U-shaped alidade standards (R1_CreamEnamel + R1_OrangeStripe)
    head_mb.add_cylinder(
        center_u=hp(0.0, -0.042, 0.0),
        radius=0.064,
        length=0.034,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=14,
        bevel=0.004,
        rot_deg_u=head_rot,
    )
    # Left & Right alidade uprights supporting the telescope trunnion axis
    for sz in (-0.054, 0.054):
        head_mb.add_bevel_box(
            center_u=hp(0.0, 0.025, sz),
            size_u=(0.068, 0.125, 0.028),
            mat="R1_CreamEnamel",
            bevel=0.004,
            segments=1,
            rot_deg_u=head_rot,
        )
    # Elevation vernier circle drum on +Z upright & GoldFoil thermal cap on top bridge
    head_mb.add_cylinder(
        center_u=hp(0.0, 0.045, 0.074),
        radius=0.044,
        length=0.018,
        mat="R1_Brass",
        axis_u="Z",
        segments=14,
        bevel=0.002,
        rot_deg_u=head_rot,
    )
    head_mb.add_bevel_box(
        center_u=hp(0.0, 0.092, 0.0),
        size_u=(0.054, 0.020, 0.135),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
        rot_deg_u=head_rot,
    )
    head_mb.add_bevel_box(
        center_u=hp(0.0, 0.106, 0.0),
        size_u=(0.044, 0.012, 0.095),
        mat="R1_GoldFoil",
        bevel=0.002,
        segments=1,
        rot_deg_u=head_rot,
    )

    # Optical telescope barrel (tilted inside the yoke) + Brass objective ring + DarkGlass lens
    head_mb.add_cylinder(
        center_u=hp(0.0, 0.045, 0.0),
        radius=0.032,
        length=0.165,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=14,
        bevel=0.003,
        rot_deg_u=head_rot,
    )
    head_mb.add_cylinder(
        center_u=hp(-0.088, 0.045, 0.0),
        radius=0.036,
        length=0.022,
        mat="R1_Brass",
        axis_u="X",
        segments=14,
        bevel=0.002,
        rot_deg_u=head_rot,
    )
    head_mb.add_cylinder(
        center_u=hp(-0.096, 0.045, 0.0),
        radius=0.028,
        length=0.012,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=14,
        bevel=0.002,
        rot_deg_u=head_rot,
    )
    # Eyepiece micrometer barrel at +X end of telescope
    head_mb.add_cylinder(
        center_u=hp(0.092, 0.045, 0.0),
        radius=0.020,
        length=0.028,
        mat="R1_BareSteel",
        axis_u="X",
        segments=12,
        bevel=0.002,
        rot_deg_u=head_rot,
    )
    head_mb.add_cylinder(
        center_u=hp(0.104, 0.045, 0.0),
        radius=0.014,
        length=0.010,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=10,
        bevel=0.0015,
        rot_deg_u=head_rot,
    )
    # Regolith dust cushion beneath the fallen theodolite head
    head_mb.add_bevel_box(
        center_u=(-0.50, 0.012, 0.02),
        size_u=(0.18, 0.028, 0.16),
        mat="R1_Basalt",
        bevel=0.006,
        segments=1,
        top_scale_xz=(0.72, 0.72),
    )
    head_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Head_Anchor", parent=root, pivot_u=head_piv)
    r1kit.create_empty("BuckledKnee_Anchor", parent=root, pivot_u=knee_piv)

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.68)

    fbx_path = PROPS_DIR / "Prop_SurveyTripod.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_sample_core_tube(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    axis_u: str = "Y",
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Add a 0.36 m Era-S lunar geology core sample tube (BareSteel sleeve, Rubber grip collar, Brass cap, DarkGlass sight)."""
    cx, cy, cz = center_u

    def tp(dx: float, dy: float, dz: float) -> Tuple[float, float, float]:
        return _rotate_u((cx + dx, cy + dy, cz + dz), rot_deg_u, center_u)

    if axis_u == "Y":
        # Main vertical bare-aluminium core tube body (length 0.31 m, radius 0.025 m)
        mb.add_cylinder(
            center_u=center_u,
            radius=0.025,
            length=0.31,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        # Upper black rubber grip collar
        mb.add_cylinder(
            center_u=tp(0.0, 0.095, 0.0),
            radius=0.0265,
            length=0.036,
            mat="R1_Rubber",
            axis_u="Y",
            segments=12,
            bevel=0.002,
            rot_deg_u=rot_deg_u,
        )
        # Top Brass twist-lock cap & bottom BareSteel cutting shoe
        mb.add_cylinder(
            center_u=tp(0.0, 0.165, 0.0),
            radius=0.0275,
            length=0.028,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        mb.add_cylinder(
            center_u=tp(0.0, -0.165, 0.0),
            radius=0.0265,
            length=0.028,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        # Narrow DarkGlass sight window strip on -Z face
        mb.add_bevel_box(
            center_u=tp(0.0, -0.02, -0.024),
            size_u=(0.014, 0.12, 0.006),
            mat="R1_DarkGlass",
            bevel=0.001,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
    else:
        # Horizontal bare-aluminium core tube along X (fallen in dust)
        mb.add_cylinder(
            center_u=center_u,
            radius=0.025,
            length=0.31,
            mat="R1_BareSteel",
            axis_u="X",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        mb.add_cylinder(
            center_u=tp(0.095, 0.0, 0.0),
            radius=0.0265,
            length=0.036,
            mat="R1_Rubber",
            axis_u="X",
            segments=12,
            bevel=0.002,
            rot_deg_u=rot_deg_u,
        )
        mb.add_cylinder(
            center_u=tp(0.165, 0.0, 0.0),
            radius=0.0275,
            length=0.028,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        mb.add_cylinder(
            center_u=tp(-0.165, 0.0, 0.0),
            radius=0.0265,
            length=0.028,
            mat="R1_BareSteel",
            axis_u="X",
            segments=12,
            bevel=0.003,
            rot_deg_u=rot_deg_u,
        )
        mb.add_bevel_box(
            center_u=tp(-0.02, 0.024, 0.0),
            size_u=(0.12, 0.006, 0.014),
            mat="R1_DarkGlass",
            bevel=0.001,
            segments=1,
            rot_deg_u=rot_deg_u,
        )


def build_sample_rack() -> bpy.types.Object:
    """Build Deliverable 5: Prop_SampleRack (~0.96 W x 0.72 D x 0.94 H m).

    Hand-tool rack with a rake, tongs, scoop, and 3 core tubes, one tube fallen in the dust.
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_SampleRack", pivot_u=(0.0, 0.0, 0.0))

    # 1. RackFrame: portable tubular bare-aluminium A-frame geology carrier with tool bar & 3-slot core tube holster
    rack_mb = r1kit.MeshBuilder("RackFrame", pivot_u=(0.0, 0.0, 0.0))

    # 4 splayed tubular A-frame legs from top bar (x = +/-0.42, y = 0.78, z = 0.02) to footpads (z = -0.22 / +0.26)
    for sx in (-1.0, 1.0):
        x_leg = sx * 0.42
        for fz, tz in ((-0.21, 0.0), (0.25, 0.04)):
            # Circular footpad + regolith dust collar
            rack_mb.add_cylinder(
                center_u=(x_leg, 0.008, fz),
                radius=0.038,
                radius_top=0.030,
                length=0.016,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=10,
                bevel=0.002,
            )
            rack_mb.add_cylinder(
                center_u=(x_leg, 0.003, fz),
                radius=0.048,
                radius_top=0.034,
                length=0.012,
                mat="R1_Basalt",
                axis_u="Y",
                segments=10,
                bevel=0.002,
            )
            rack_mb.add_pipe(
                p0_u=(x_leg, 0.016, fz),
                p1_u=(x_leg, 0.78, tz),
                radius=0.012,
                mat="R1_BareSteel",
                segments=10,
            )
        # Side A-frame horizontal spreader bar at y = 0.24 m
        rack_mb.add_pipe(
            p0_u=(x_leg, 0.24, -0.15),
            p1_u=(x_leg, 0.24, 0.19),
            radius=0.010,
            mat="R1_BareSteel",
            segments=8,
        )

    # Lower horizontal crossbars at y = 0.24 m
    rack_mb.add_pipe((-0.42, 0.24, -0.14), (0.42, 0.24, -0.14), radius=0.011, mat="R1_BareSteel", segments=8)
    rack_mb.add_pipe((-0.42, 0.24, 0.18), (0.42, 0.24, 0.18), radius=0.011, mat="R1_BareSteel", segments=8)

    # Upper horizontal tool-carrier header bar at y = 0.78 m (all R1_BareSteel + Brass tool clips, no cream/orange)
    rack_mb.add_bevel_box(
        center_u=(0.0, 0.78, 0.02),
        size_u=(0.88, 0.056, 0.064),
        mat="R1_BareSteel",
        bevel=0.006,
        segments=1,
    )
    rack_mb.add_bevel_box(
        center_u=(0.0, 0.78, -0.012),
        size_u=(0.84, 0.022, 0.012),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
    )
    # 3 spring-loaded Brass tool-latch jaws on front (-Z) of header bar for Rake (-0.28), Tongs (-0.06), Scoop (+0.14)
    for tx in (-0.28, -0.06, 0.14):
        rack_mb.add_bevel_box(
            center_u=(tx, 0.78, -0.036),
            size_u=(0.046, 0.036, 0.044),
            mat="R1_Brass",
            bevel=0.003,
            segments=1,
        )

    # Top central EVA gloved-hand transport hoop (y = 0.80..0.91 m)
    rack_mb.add_tube_path(
        points_u=[
            (-0.14, 0.80, 0.02),
            (-0.12, 0.90, 0.02),
            (0.12, 0.90, 0.02),
            (0.14, 0.80, 0.02),
        ],
        radius=0.010,
        mat="R1_BareSteel",
        segments=8,
        cap_ends=True,
    )

    # Right-side 3-slot Core Tube Holster Tray (x = 0.18..0.40, y = 0.22..0.46, z = 0.08, all R1_BareSteel / Brass)
    rack_mb.add_bevel_box(
        center_u=(0.295, 0.235, 0.08),
        size_u=(0.24, 0.032, 0.11),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
    )
    rack_mb.add_bevel_box(
        center_u=(0.295, 0.45, 0.08),
        size_u=(0.24, 0.028, 0.11),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
    )
    for slot_x in (0.22, 0.30, 0.37):
        rack_mb.add_cylinder(
            center_u=(slot_x, 0.466, 0.08),
            radius=0.031,
            length=0.016,
            mat="R1_Brass" if slot_x > 0.34 else "R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.002,
        )
    rack_mb.build_object(parent=root)

    # 2. ToolRake: Apollo lunar sampling rake hanging in left clip (BareSteel shaft/head + Rubber black grip)
    rake_piv = (-0.28, 0.48, -0.055)
    rake_mb = r1kit.MeshBuilder("ToolRake", pivot_u=rake_piv)
    # Long vertical shaft (y = 0.16..0.89 m) + top Rubber T-handle + Rubber grip sleeve
    rake_mb.add_cylinder(
        center_u=(-0.28, 0.525, -0.055),
        radius=0.009,
        length=0.73,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0015,
    )
    rake_mb.add_cylinder(
        center_u=(-0.28, 0.68, -0.055),
        radius=0.012,
        length=0.11,
        mat="R1_Rubber",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    rake_mb.add_cylinder(
        center_u=(-0.28, 0.895, -0.055),
        radius=0.011,
        length=0.11,
        mat="R1_Rubber",
        axis_u="X",
        segments=10,
        bevel=0.002,
    )
    # Rake head yoke & 7 curved sampling comb tines at bottom (y = 0.08..0.18 m)
    rake_mb.add_bevel_box(
        center_u=(-0.28, 0.165, -0.055),
        size_u=(0.17, 0.024, 0.028),
        mat="R1_BareSteel",
        bevel=0.003,
        segments=1,
    )
    rake_mb.add_cylinder(
        center_u=(-0.28, 0.185, -0.055),
        radius=0.014,
        length=0.022,
        mat="R1_Brass",
        axis_u="Z",
        segments=10,
        bevel=0.002,
    )
    for t_i in range(7):
        tx = -0.28 + (t_i - 3) * 0.024
        rake_mb.add_strut(
            p0_u=(tx, 0.158, -0.055),
            p1_u=(tx, 0.082, -0.095),
            width=0.006,
            thickness=0.008,
            mat="R1_BareSteel",
            bevel=0.001,
        )
    rake_mb.build_object(parent=root)

    # 3. ToolTongs: long-handled rock-sampling tongs hanging in center-left clip (BareSteel shaft/jaws + Rubber black grip)
    tongs_piv = (-0.06, 0.48, -0.055)
    tongs_mb = r1kit.MeshBuilder("ToolTongs", pivot_u=tongs_piv)
    tongs_mb.add_cylinder(
        center_u=(-0.06, 0.53, -0.055),
        radius=0.0085,
        length=0.68,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0015,
    )
    # Upper black rubber squeeze-trigger grip assembly (y = 0.72..0.88 m)
    tongs_mb.add_bevel_box(
        center_u=(-0.06, 0.875, -0.055),
        size_u=(0.095, 0.024, 0.026),
        mat="R1_Rubber",
        bevel=0.003,
        segments=1,
    )
    tongs_mb.add_bevel_box(
        center_u=(-0.06, 0.815, -0.055),
        size_u=(0.078, 0.018, 0.022),
        mat="R1_Rubber",
        bevel=0.002,
        segments=1,
    )
    # Bottom Brass jaw linkage hub & 4 splayed rock-grasping claw fingers (y = 0.09..0.20 m)
    tongs_mb.add_cylinder(
        center_u=(-0.06, 0.19, -0.055),
        radius=0.016,
        length=0.032,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    for ang_deg in (0.0, 90.0, 180.0, 270.0):
        ar = math.radians(ang_deg + 45.0)
        cx_jaw = -0.06 + 0.036 * math.cos(ar)
        cz_jaw = -0.055 + 0.036 * math.sin(ar)
        tx_jaw = -0.06 + 0.018 * math.cos(ar)
        tz_jaw = -0.055 + 0.018 * math.sin(ar)
        tongs_mb.add_tube_path(
            points_u=[
                (-0.06, 0.185, -0.055),
                (cx_jaw, 0.135, cz_jaw),
                (tx_jaw, 0.092, tz_jaw),
            ],
            radius=0.0045,
            mat="R1_BareSteel",
            segments=6,
            cap_ends=True,
        )
    tongs_mb.build_object(parent=root)

    # 4. ToolScoop: adjustable lunar trenching scoop hanging in center-right clip (BareSteel shaft/head + Rubber black grip)
    scoop_piv = (0.14, 0.48, -0.055)
    scoop_mb = r1kit.MeshBuilder("ToolScoop", pivot_u=scoop_piv)
    scoop_mb.add_cylinder(
        center_u=(0.14, 0.535, -0.055),
        radius=0.0095,
        length=0.67,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.0015,
    )
    scoop_mb.add_cylinder(
        center_u=(0.14, 0.69, -0.055),
        radius=0.013,
        length=0.12,
        mat="R1_Rubber",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    scoop_mb.add_cylinder(
        center_u=(0.14, 0.875, -0.055),
        radius=0.014,
        length=0.042,
        mat="R1_Rubber",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    # Adjustable Brass angle-indexing swivel at y = 0.20 m + flared box-pan scoop head (y = 0.08..0.19 m)
    scoop_mb.add_cylinder(
        center_u=(0.14, 0.195, -0.055),
        radius=0.018,
        length=0.028,
        mat="R1_Brass",
        axis_u="X",
        segments=10,
        bevel=0.002,
    )
    scoop_mb.add_bevel_box(
        center_u=(0.14, 0.135, -0.075),
        size_u=(0.092, 0.105, 0.064),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
        rot_deg_u=(22.0, 0.0, 0.0),
        top_scale_xz=(0.82, 0.75),
    )
    scoop_mb.add_bevel_box(
        center_u=(0.14, 0.086, -0.095),
        size_u=(0.094, 0.018, 0.052),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
        rot_deg_u=(22.0, 0.0, 0.0),
    )
    scoop_mb.build_object(parent=root)

    # 5. CoreTubesRacked: 2 upright core sample tubes seated in the holster tray (x = 0.22 and x = 0.30, z = 0.08)
    racked_piv = (0.26, 0.38, 0.08)
    racked_mb = r1kit.MeshBuilder("CoreTubesRacked", pivot_u=racked_piv)
    _add_sample_core_tube(racked_mb, center_u=(0.22, 0.38, 0.08), axis_u="Y", rot_deg_u=(0.0, 0.0, 0.0))
    _add_sample_core_tube(racked_mb, center_u=(0.30, 0.38, 0.08), axis_u="Y", rot_deg_u=(2.0, 15.0, -1.5))
    racked_mb.build_object(parent=root)

    # 6. CoreTubeFallen: 3rd core sample tube fallen in the dust in front of the empty 3rd holster slot
    fallen_piv = (0.24, 0.028, -0.24)
    fallen_mb = r1kit.MeshBuilder("CoreTubeFallen", pivot_u=fallen_piv)
    _add_sample_core_tube(
        fallen_mb,
        center_u=fallen_piv,
        axis_u="X",
        rot_deg_u=(0.0, 28.0, -2.5),
    )
    # Regolith dust bed partially burying the fallen core tube
    fallen_mb.add_bevel_box(
        center_u=(fallen_piv[0], 0.007, fallen_piv[2]),
        size_u=(0.30, 0.018, 0.075),
        mat="R1_Basalt",
        bevel=0.005,
        segments=1,
        rot_deg_u=(0.0, 28.0, 0.0),
        top_scale_xz=(0.82, 0.68),
    )
    fallen_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Tools_Anchor", parent=root, pivot_u=(-0.06, 0.78, -0.035))
    r1kit.create_empty("FallenTube_Anchor", parent=root, pivot_u=fallen_piv)

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.55)

    fbx_path = PROPS_DIR / "Prop_SampleRack.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_decal_bootprint_trail() -> bpy.types.Object:
    """Build Deliverable 6: Decal_BootprintTrail, two crossing EVA bootprint trails pressed into the regolith (tileable).

    The prints are not modelled: `TrailSheet` is the closed box that MoonProject/R1/GroundImprint projects them
    through onto whatever ground already lies inside it, so the decal shows no slab, rim or quad edge of its own. The
    box reaches below the origin for dips in the ground and only a little above it. The prefab has no collider: an
    imprint is part of the ground, never something 07's wheels can catch on.
    """
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    width_x = 0.95
    length_z = 3.00
    reach_below = 0.10
    reach_above = 0.02

    root = r1kit.create_empty("Decal_BootprintTrail", pivot_u=(0.0, 0.0, 0.0))

    trail_mb = r1kit.MeshBuilder("TrailSheet", pivot_u=(0.0, 0.0, 0.0))
    trail_mb.add_bevel_box(
        center_u=(0.0, 0.5 * (reach_above - reach_below), 0.0),
        size_u=(width_x, reach_above + reach_below, length_z),
        mat="R1_Signage",
        bevel=0.002,
        segments=1,
    )
    trail_mb.build_object(parent=root)

    # Tileable path anchors at Z = -1.50 m and Z = +1.50 m (3.00 m stride) + Center_Anchor
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, 0.0, -0.5 * length_z))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.5 * length_z))
    r1kit.create_empty("Center_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.55)

    fbx_path = PROPS_DIR / "Decal_BootprintTrail.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


BUILDERS: Dict[str, Callable[[], bpy.types.Object]] = {
    "mission_flag": build_mission_flag,
    "seismic_station": build_seismic_station,
    "retroreflector": build_retroreflector,
    "survey_tripod": build_survey_tripod,
    "sample_rack": build_sample_rack,
    "decal_bootprint_trail": build_decal_bootprint_trail,
}


def _parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument(
        "--prop",
        choices=[*BUILDERS.keys(), "all"],
        default="all",
        help="Which Basin Life prop to build (default: all)",
    )
    return ap.parse_args(argv)




def main() -> None:
    args = _parse_args()
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    if args.prop == "all":
        for fn in BUILDERS.values():
            fn()
    else:
        BUILDERS[args.prop]()


if __name__ == "__main__":
    main()


