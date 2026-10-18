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


BUILDERS: Dict[str, Callable[[], bpy.types.Object]] = {
    "mission_flag": build_mission_flag,
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
