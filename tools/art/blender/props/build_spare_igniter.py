"""Build R1 standalone story prop `Prop_SpareIgniter.fbx` (`Assets/_Project/Art/R1/Models/Props/Prop_SpareIgniter.fbx`).

Matches `Cutter_Igniter` on Rover 07's cutter turret (`tools/art/blender/rover/rover_parts.py:2260`)
scaled to ~0.25 m length at 07's scale so the player recognises it when fitted:
- 3-ribbed white ceramic high-voltage insulator body (`R1_WhitePaint`)
- Turned brass base coupling, 6-sided bevelled hex wrench collar, and stepped brass electrode tip (`R1_Brass`)
- Small stencilled specification tag (`R1_Signage`)
- Lying on its side along X with pivot (0, 0, 0) at its base centre (`min_y = 0`, `center_x = 0`, `center_z = 0`).

Run headless:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_spare_igniter.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"

# Dimensions in Unity coordinates (lying on its side along X, resting on Y = 0)
TOTAL_LENGTH_X = 0.2500
AXIS_Y = 0.0450
HEX_FLAT_HALF_Y = 0.0450
HEX_CIRCUMRADIUS_Z = HEX_FLAT_HALF_Y / math.cos(math.radians(30.0))  # ~0.05196 m (width ~0.1039 m)

# Non-emissive stencil cell on T_R1_Signage_Albedo.png ("WORKSHOP" stencil block)
WORKSHOP_UV_CELL = (0.020, 0.265, 0.480, 0.485)


def ensure_extended_r1_materials() -> None:
    """Register R1_Brass, R1_Signage, and R1_WhitePaint into r1kit at runtime."""
    extra_specs = {
        "R1_Brass": {
            "base_color": r1kit._srgb_to_linear("#DBB052"),
            "wear_color": r1kit._srgb_to_linear("#F7D98C"),
            "dust_color": r1kit._srgb_to_linear("#8A8270"),
            "metallic": 0.92,
            "roughness": 0.22,
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
            "roughness": 0.78,
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


def _map_signage_uvs(obj: bpy.types.Object) -> None:
    """Map all R1_Signage faces onto the non-emissive stencil cell of T_R1_Signage_Albedo.png."""
    mesh = obj.data
    mat_names = [m.name if m else "" for m in mesh.materials]
    if "R1_Signage" not in mat_names:
        return
    sig_idx = mat_names.index("R1_Signage")
    if not mesh.uv_layers:
        mesh.uv_layers.new(name=r1kit.UV_MAP_NAME)
    uv_layer = mesh.uv_layers.active.data

    u0, v0, u1, v1 = WORKSHOP_UV_CELL
    # Tag spans x in [-0.078, -0.046], z in [-0.021, +0.021]
    x_min, x_max = -0.078, -0.046
    z_min, z_max = -0.021, 0.021
    span_x = max(1e-5, x_max - x_min)
    span_z = max(1e-5, z_max - z_min)

    for poly in mesh.polygons:
        if poly.material_index != sig_idx:
            continue
        for li in poly.loop_indices:
            v_co = mesh.vertices[mesh.loops[li].vertex_index].co
            # Blender coords: x = Unity X, y = Unity Z, z = Unity Y
            tx = max(0.0, min(1.0, (v_co.x - x_min) / span_x))
            tz = max(0.0, min(1.0, (v_co.y - z_min) / span_z))
            uv_layer[li].uv = (u0 + tx * (u1 - u0), v0 + tz * (v1 - v0))


def _enhance_brass_glint_vertex_colors(obj: bpy.types.Object) -> None:
    """Ensure clean AO and strong edge-glint curvature on R1_Brass and R1_WhitePaint so the cartridge reads crisply."""
    mesh = obj.data
    mat_names = [m.name if m else "" for m in mesh.materials]
    brass_idx = mat_names.index("R1_Brass") if "R1_Brass" in mat_names else -1
    white_idx = mat_names.index("R1_WhitePaint") if "R1_WhitePaint" in mat_names else -1

    for attr_name in (r1kit.VCOL_ATTRIBUTE_NAME, r1kit.COLOR_ATTR_NAME):
        attr = mesh.color_attributes.get(attr_name)
        if attr is None:
            continue
        for poly in mesh.polygons:
            is_brass = poly.material_index == brass_idx
            is_white = poly.material_index == white_idx
            for li in poly.loop_indices:
                c = list(attr.data[li].color)
                if is_brass:
                    c[0] = max(c[0], 0.86)  # bright open AO on machined brass
                    c[1] = max(c[1], 0.58)  # strong convex edge-wear highlight for warm brass glint
                    c[2] = min(c[2], 0.08)  # minimal cavity dust inside sealed crate
                elif is_white:
                    c[0] = max(c[0], 0.82)  # clean ceramic sheds with subtle valley AO
                    c[2] = min(c[2], 0.14)
                attr.data[li].color = (c[0], c[1], c[2], 1.0)


def build_spare_igniter() -> bpy.types.Object:
    """Build `Prop_SpareIgniter` at origin with pivot (0, 0, 0) at its base centre, lying on its side along X."""
    mb = r1kit.MeshBuilder("Prop_SpareIgniter", pivot_u=(0.0, 0.0, 0.0))
    brass_idx = mb._mat_index("R1_Brass")
    white_idx = mb._mat_index("R1_WhitePaint")

    # 1. Turned lathe cartridge body along X in [-0.1250, +0.1250], matching Cutter_Igniter (rover_parts.py:2262)
    #    scaled to 0.2500 m length with soft chamfered shoulders (`auto_chamfer=True`).
    #    Segments 0..4: R1_Brass base shaft & flange collar (x = -0.1250 .. -0.0365)
    #    Segments 5..15: R1_WhitePaint 3-shed high-voltage ceramic insulator (x = -0.0365 .. +0.0705)
    #    Segments 16..22: R1_Brass stepped HV electrode shroud & rounded spark tip (x = +0.0705 .. +0.1250)
    ign_profile = [
        (0.0000, -0.1250),
        (0.0310, -0.1250),
        (0.0368, -0.1205),
        (0.0368, -0.0810),
        (0.0425, -0.0775),
        (0.0425, -0.0395),
        (0.0318, -0.0365),
        # Shed 1
        (0.0416, -0.0280),
        (0.0416, -0.0110),
        (0.0308, -0.0020),
        # Shed 2
        (0.0416, 0.0070),
        (0.0416, 0.0250),
        (0.0308, 0.0340),
        # Shed 3
        (0.0406, 0.0430),
        (0.0406, 0.0610),
        (0.0298, 0.0685),
        # Stepped brass electrode barrel & tip
        (0.0254, 0.0715),
        (0.0254, 0.0945),
        (0.0160, 0.0980),
        (0.0160, 0.1185),
        (0.0112, 0.1230),
        (0.0055, 0.1246),
        (0.0000, 0.1250),
    ]
    ring_mats = (
        [brass_idx] * 6
        + [white_idx] * 9
        + [brass_idx] * 7
    )
    r1kit.append_lathe_profile(
        mb.bm,
        ign_profile,
        axis="X",
        center_xyz=(0.0, AXIS_Y, 0.0),
        segments=24,
        mat_idx=white_idx,
        ring_mat_indices=ring_mats,
        auto_chamfer=True,
    )

    # 2. 6-sided bevelled brass hex wrench collar (matching Cutter_Igniter's 6-sided collar at rover_parts.py:2295)
    #    Oriented with flat top (y = +0.0900) and flat bottom (y = 0.0000) so the cartridge rests stably on Y = 0.
    hex_poly_zy = [
        (HEX_CIRCUMRADIUS_Z, 0.0),
        (0.5 * HEX_CIRCUMRADIUS_Z, HEX_FLAT_HALF_Y),
        (-0.5 * HEX_CIRCUMRADIUS_Z, HEX_FLAT_HALF_Y),
        (-HEX_CIRCUMRADIUS_Z, 0.0),
        (-0.5 * HEX_CIRCUMRADIUS_Z, -HEX_FLAT_HALF_Y),
        (0.5 * HEX_CIRCUMRADIUS_Z, -HEX_FLAT_HALF_Y),
    ]
    r1kit.append_prism_extrusion(
        mb.bm,
        hex_poly_zy,
        depth=0.0340,
        axis="X",
        center_xyz=(-0.0620, AXIS_Y, 0.0),
        bevel_width=0.0036,
        bevel_segments=2,
        mat_idx=brass_idx,
    )

    # Subtle rear brass seating flat under the base shaft (x = -0.1040, y = 0.0000 .. 0.0120) so both ends rest level on Y = 0
    mb.add_bevel_box(
        center_u=(-0.1040, 0.0060, 0.0),
        size_u=(0.0220, 0.0120, 0.0320),
        mat="R1_Brass",
        bevel=0.0028,
        segments=2,
    )

    # 3. Small stencilled specification tag (`R1_Signage`) flush on the upper hex collar & front/back upper flats
    #    Top stencil plate (kept within y <= 0.0900 so total height stays cleanly at 0.0900 m)
    mb.add_bevel_box(
        center_u=(-0.0620, 0.0884, 0.0),
        size_u=(0.0260, 0.0032, 0.0380),
        mat="R1_Signage",
        bevel=0.0008,
        segments=1,
    )
    # Angled side stencil tags on the ±Z upper-flank hex flats (readable from side/3-quarter chase views)
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(-0.0620, AXIS_Y + 0.0220, sz * 0.0380),
            size_u=(0.0250, 0.0280, 0.0032),
            mat="R1_Signage",
            bevel=0.0008,
            segments=1,
            rot_deg_u=(sz * 30.0, 0.0, 0.0),
        )

    root = mb.build_object(parent=None, smooth_angle_deg=54.0)
    bpy.context.view_layer.update()

    # Exact bounds check & recentering guarantee:
    # Ensure min_y == 0.0, center_x == 0.0, center_z == 0.0 in Unity space (min_z == 0.0, center_x == 0.0, center_y == 0.0 in Blender)
    mesh = root.data
    xs = [v.co.x for v in mesh.vertices]
    ys = [v.co.y for v in mesh.vertices]
    zs = [v.co.z for v in mesh.vertices]
    cx_b = 0.5 * (min(xs) + max(xs))
    cy_b = 0.5 * (min(ys) + max(ys))
    min_z_b = min(zs)
    if abs(cx_b) > 1e-6 or abs(cy_b) > 1e-6 or abs(min_z_b) > 1e-6:
        for v in mesh.vertices:
            v.co.x -= cx_b
            v.co.y -= cy_b
            v.co.z -= min_z_b
        mesh.update()

    r1kit.apply_triplanar_uvs(root, texel_scale=1.2)
    _map_signage_uvs(root)
    r1kit.bake_vertex_colors(root, footprint_radius=0.14)
    _enhance_brass_glint_vertex_colors(root)
    return root


def main() -> None:
    ensure_extended_r1_materials()
    r1kit.reset_scene()
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    root = build_spare_igniter()
    out_path = PROPS_DIR / "Prop_SpareIgniter.fbx"
    print(f"[{root.name}] Triangles: {r1kit.count_triangles(root)}")
    r1kit.export_fbx(root, out_path)
    print(f"[spare_igniter] Exported {out_path}")


if __name__ == "__main__":
    main()
