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

    # 4 radial hold-down tape tabs + regolith dust clumps around the skirt perimeter
    for i in range(4):
        ang = math.radians(45.0 + 90.0 * i)
        tx = sx_center + 0.255 * math.cos(ang)
        tz = sz_center + 0.255 * math.sin(ang)
        shroud_mb.add_bevel_box(
            center_u=(tx, 0.024, tz),
            size_u=(0.065, 0.018, 0.036),
            mat="R1_OrangeStripe" if (i % 2 == 0) else "R1_GreyMetal",
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

    # 2. SeismometerUnit: upper cylindrical sensor head peeking out of the gold-foil shroud
    seis_mb = r1kit.MeshBuilder("SeismometerUnit", pivot_u=(sx_center, 0.24, sz_center))
    # Main machined sensor cylinder (y = 0.22..0.325 m)
    seis_mb.add_cylinder(
        center_u=(sx_center, 0.272, sz_center),
        radius=0.118,
        length=0.105,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=16,
        bevel=0.005,
    )
    # Upper radiator bezel ring
    seis_mb.add_cylinder(
        center_u=(sx_center, 0.328, sz_center),
        radius=0.124,
        length=0.022,
        mat="R1_GreyMetal",
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
    # Solar alignment shadow gnomon pin + calibration scale plate on top
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
        mat="R1_OrangeStripe",
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
        mat="R1_GreyMetal",
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
    # Orange cable identification band & regolith dust mound over the middle of the cable
    cable_mb.add_cylinder(
        center_u=(-0.06, 0.017, 0.065),
        radius=0.019,
        length=0.028,
        mat="R1_OrangeStripe",
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

    # 4. CentralBox: squat central electronics & telemetry box
    box_mb = r1kit.MeshBuilder("CentralBox", pivot_u=(bx_center, 0.0, bz_center))
    # Lower dark grey skid base frame + 4 footpads sunk slightly in regolith (y = -0.012..0.045 m)
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.022, bz_center),
        size_u=(0.44, 0.048, 0.36),
        mat="R1_GreyMetal",
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

    # Squat chamfered CreamEnamel electronics housing (y = 0.045..0.285 m)
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.165, bz_center),
        size_u=(0.40, 0.24, 0.32),
        mat="R1_CreamEnamel",
        bevel=0.012,
        segments=2,
        top_scale_xz=(0.92, 0.90),
    )
    # Warm-orange identification stripe band around upper perimeter
    box_mb.add_bevel_box(
        center_u=(bx_center, 0.225, bz_center),
        size_u=(0.392, 0.038, 0.312),
        mat="R1_OrangeStripe",
        bevel=0.004,
        segments=1,
    )
    # Gold-foil thermal blanket patch on rear/top half of central box + radiator louver deck
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
            mat="R1_GreyMetal",
            bevel=0.0015,
            segments=1,
        )
    # Front instrument bezel & toggle switch guard
    box_mb.add_bevel_box(
        center_u=(bx_center - 0.05, 0.145, bz_center - 0.158),
        size_u=(0.20, 0.11, 0.022),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    box_mb.add_cylinder(
        center_u=(bx_center - 0.09, 0.145, bz_center - 0.172),
        radius=0.014,
        length=0.018,
        mat="R1_Brass",
        axis_u="Z",
        segments=10,
        bevel=0.002,
    )
    box_mb.add_cylinder(
        center_u=(bx_center - 0.01, 0.145, bz_center - 0.172),
        radius=0.018,
        length=0.014,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=10,
        bevel=0.002,
    )
    # Short telemetry whip stub on rear-right corner (y = 0.28..0.43 m)
    box_mb.add_cylinder(
        center_u=(bx_center + 0.13, 0.305, bz_center + 0.09),
        radius=0.022,
        length=0.042,
        mat="R1_GreyMetal",
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

    # 5. DeadStatusLamp: prominent dead status lamp turret on front-left top deck of CentralBox (R1_DarkGlass, no R1_LampAmber)
    lamp_pos = (bx_center - 0.10, 0.295, bz_center - 0.08)
    lamp_mb = r1kit.MeshBuilder("DeadStatusLamp", pivot_u=lamp_pos)
    # Armored cage base & brass ring
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1], lamp_pos[2]),
        radius=0.034,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.003,
    )
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1] + 0.015, lamp_pos[2]),
        radius=0.029,
        length=0.012,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
    )
    # Unlit dark glass dome lens
    lamp_mb.add_cylinder(
        center_u=(lamp_pos[0], lamp_pos[1] + 0.032, lamp_pos[2]),
        radius=0.024,
        radius_top=0.015,
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


BUILDERS: Dict[str, Callable[[], bpy.types.Object]] = {
    "mission_flag": build_mission_flag,
    "seismic_station": build_seismic_station,
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

