"""Generate Deliverable 3 R1 story prop:
- Assets/_Project/Art/R1/Models/Props/BuildApronMarkings.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_apron_markings.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

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


def map_top_planar_uv_rect(
    obj: bpy.types.Object,
    x_min: float,
    x_max: float,
    z_u_min: float,
    z_u_max: float,
    u_range: tuple[float, float],
    v_range: tuple[float, float],
) -> None:
    """Map upward-facing (+Y Unity / +Z Blender) polygons inside (x_min..x_max, z_u_min..z_u_max) to a UV sub-rect."""
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    world_mat = obj.matrix_world
    span_x = max(1e-5, x_max - x_min)
    span_z = max(1e-5, z_u_max - z_u_min)
    u0, u1 = u_range
    v0, v1 = v_range

    for poly in mesh.polygons:
        c_w = world_mat @ poly.center
        if not (x_min - 0.05 <= c_w.x <= x_max + 0.05 and z_u_min - 0.05 <= c_w.y <= z_u_max + 0.05):
            continue
        n_world = (world_mat.to_3x3() @ poly.normal).normalized()
        if n_world.z > 0.55:
            for loop_idx in poly.loop_indices:
                v_idx = mesh.loops[loop_idx].vertex_index
                co_w = world_mat @ mesh.vertices[v_idx].co
                u_norm = max(0.0, min(1.0, (co_w.x - x_min) / span_x))
                v_norm = max(0.0, min(1.0, (co_w.y - z_u_min) / span_z))
                uv_layer[loop_idx].uv = (u0 + u_norm * (u1 - u0), v0 + v_norm * (v1 - v0))
        else:
            for loop_idx in poly.loop_indices:
                uv_layer[loop_idx].uv = (u0 + 0.02 * (u1 - u0), v0 + 0.02 * (v1 - v0))


def build_apron_markings() -> bpy.types.Object:
    """Build B5 unloading apron floor markings (~4.40 W x 4.80 L x 0.024 H m)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("BuildApronMarkings", pivot_u=(0.0, 0.0, 0.0))

    # 1. ApronFrame: worn painted safety stripes, outer boundary bars, rover wheel tracks, cable trench guide
    frame = r1kit.MeshBuilder("ApronFrame", pivot_u=(0.0, 0.0, 0.0))

    # Outer dashed boundary rails (4.40 m W x 4.80 m L) in weathered CreamEnamel & GreyMetal
    for sx in (-1.0, 1.0):
        x_rail = sx * 2.14
        for seg_idx in range(5):
            z_seg = -1.76 + seg_idx * 0.88
            # Skip one segment gap for worn/broken paint realism
            mat_name = "R1_GreyMetal" if (seg_idx == 2 and sx > 0) else "R1_CreamEnamel"
            frame.add_bevel_box((x_rail, 0.006, z_seg), (0.12, 0.012, 0.64), mat=mat_name, bevel=0.002, segments=1)

    # Front (-Z = -2.26 m) and Rear (+Z = +2.26 m) diagonal/alternating safety hazard stripe thresholds
    num_stripes = 11
    for sz, z_bar in ((-1.0, -2.26), (1.0, 2.26)):
        # Underlying dark threshold backing plate
        frame.add_bevel_box((0.0, 0.004, z_bar), (4.40, 0.008, 0.28), mat="R1_GreyMetal", bevel=0.002, segments=1)
        for i in range(num_stripes):
            if sz > 0 and i in (5,):
                # Leave center rear gap for the cable link channel
                continue
            x_s = -1.90 + i * 0.38
            mat_s = "R1_OrangeStripe" if (i % 2 == 0) else "R1_CreamEnamel"
            frame.add_bevel_box(
                (x_s, 0.009, z_bar),
                (0.22, 0.010, 0.22),
                mat=mat_s,
                bevel=0.002,
                segments=1,
                rot_deg_u=(0.0, 22.0, 0.0),
            )

    # Inner Rover 07 wheel-track alignment bars (1.48 m X span x 2.02 m Z length) inside the ChargingPad bay
    for sx in (-1.0, 1.0):
        wx = sx * 0.74
        for z_tick in (-0.82, 0.0, 0.82):
            frame.add_bevel_box((wx, 0.005, z_tick), (0.14, 0.010, 0.42), mat="R1_GreyMetal", bevel=0.002, segments=1)
        # Front/back wheel stop chocks markings
        frame.add_bevel_box((wx, 0.007, -1.12), (0.24, 0.012, 0.08), mat="R1_OrangeStripe", bevel=0.002, segments=1)
        frame.add_bevel_box((wx, 0.007, 1.12), (0.24, 0.012, 0.08), mat="R1_OrangeStripe", bevel=0.002, segments=1)

    # Rear (+Z) cable link guide channel leading from the ChargingPad bay edge (Z=1.68) to Z=2.40
    for sx in (-1.0, 1.0):
        frame.add_bevel_box((sx * 0.22, 0.007, 2.04), (0.07, 0.014, 0.68), mat="R1_OrangeStripe", bevel=0.002, segments=1)
    frame.add_bevel_box((0.0, 0.005, 2.04), (0.34, 0.010, 0.66), mat="R1_Rubber", bevel=0.002, segments=1)

    frame.build_object(parent=root)

    # 2. StencilCodes: faded 1980s stencil code plates on the apron floor (mapped to R1_Signage atlas)
    stencils = r1kit.MeshBuilder("StencilCodes", pivot_u=(0.0, 0.007, -1.85))
    # Front-left stencil code plaque ("SEC-04 / HABITAT & LAB")
    stencils.add_bevel_box((-1.15, 0.007, -1.85), (0.96, 0.014, 0.42), mat="R1_Signage", bevel=0.002, segments=1)
    # Front-right rover bay pictogram plaque ("PICT_ROVER")
    stencils.add_bevel_box((1.25, 0.007, -1.85), (0.48, 0.014, 0.42), mat="R1_Signage", bevel=0.002, segments=1)
    # Rear-left stencil plaque ("EVAC 1988")
    stencils.add_bevel_box((-1.25, 0.007, 1.85), (0.62, 0.014, 0.36), mat="R1_Signage", bevel=0.002, segments=1)
    stencils_obj = stencils.build_object(parent=root)

    # 3. BayGlowOutline: 3.30 m x 3.30 m ChargingPad snap bay outline that glows faintly when build mode is open
    glow = r1kit.MeshBuilder("BayGlowOutline", pivot_u=(0.0, 0.010, 0.0))
    bay_half = 1.65  # 3.30 m x 3.30 m bay outline matching the ~3.2 x 3.2 m ChargingPad
    arm_len = 0.78
    strip_w = 0.095
    strip_h = 0.014
    y_g = 0.011

    # 4 heavy L-bracket corners of the ChargingPad bay
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            cx_arm = sx * (bay_half - arm_len * 0.5)
            cz_corner = sz * bay_half
            glow.add_bevel_box((cx_arm, y_g, cz_corner), (arm_len, strip_h, strip_w), mat="R1_LampAmber", bevel=0.002, segments=1)

            cx_corner = sx * bay_half
            cz_arm = sz * (bay_half - arm_len * 0.5 + strip_w * 0.5)
            glow.add_bevel_box(
                (cx_corner, y_g, cz_arm),
                (strip_w, strip_h, arm_len - strip_w),
                mat="R1_LampAmber",
                bevel=0.002,
                segments=1,
            )

    # Mid-edge snap ticks on all 4 sides of the 3.30 m bay
    for sx in (-1.0, 1.0):
        glow.add_bevel_box((sx * bay_half, y_g, 0.0), (strip_w, strip_h, 0.56), mat="R1_LampAmber", bevel=0.002, segments=1)
    for sz in (-1.0, 1.0):
        glow.add_bevel_box((0.0, y_g, sz * bay_half), (0.56, strip_h, strip_w), mat="R1_LampAmber", bevel=0.002, segments=1)

    # Center pad alignment crosshair ring & ticks at (0, 0, 0)
    glow.add_bevel_box((0.0, y_g, 0.0), (0.48, strip_h, 0.065), mat="R1_LampAmber", bevel=0.002, segments=1)
    glow.add_bevel_box((0.0, y_g, 0.0), (0.065, strip_h, 0.48), mat="R1_LampAmber", bevel=0.002, segments=1)
    glow.build_object(parent=root)

    # Anchor empties for World placement & build-mode snapping
    r1kit.create_empty("PadSnap_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("RoverApproach_Anchor", parent=root, pivot_u=(0.0, 0.0, -2.26))
    r1kit.create_empty("CableChannel_Anchor", parent=root, pivot_u=(0.0, 0.01, 2.38))

    r1kit.apply_triplanar_uvs(root)
    # Map the 3 stencil plaques onto R1_Signage UV cells
    map_top_planar_uv_rect(
        stencils_obj,
        x_min=-1.63,
        x_max=-0.67,
        z_u_min=-2.06,
        z_u_max=-1.64,
        u_range=(0.02, 0.48),
        v_range=(0.52, 0.73),  # SEC04_HABITAT
    )
    map_top_planar_uv_rect(
        stencils_obj,
        x_min=1.01,
        x_max=1.49,
        z_u_min=-2.06,
        z_u_max=-1.64,
        u_range=(0.02, 0.23),
        v_range=(0.02, 0.23),  # PICT_ROVER
    )
    map_top_planar_uv_rect(
        stencils_obj,
        x_min=-1.56,
        x_max=-0.94,
        z_u_min=1.67,
        z_u_max=2.03,
        u_range=(0.76, 0.98),
        v_range=(0.76, 0.98),  # EVAC_1988
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=2.40)

    # Boost dust on the painted apron markings so they look worn, faded, and half-buried in lunar dust
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or "Col" not in obj.data.color_attributes:
            continue
        attr = obj.data.color_attributes["Col"]
        world_mat = obj.matrix_world
        for loop in obj.data.loops:
            co_w = world_mat @ obj.data.vertices[loop.vertex_index].co
            r, g, b, a = attr.data[loop.index].color
            # Vary wear and regolith dust across the floor markings
            radial = math.hypot(co_w.x, co_w.y) / 2.6
            dust_add = 0.28 + 0.18 * math.sin(co_w.x * 2.7 + co_w.y * 1.9) + 0.14 * min(1.0, radial)
            wear_add = 0.22 + 0.15 * math.cos(co_w.x * 3.5 - co_w.y * 2.3)
            attr.data[loop.index].color = (
                max(0.28, min(1.0, r * 0.88)),
                max(0.0, min(1.0, g + wear_add)),
                max(0.0, min(1.0, b + dust_add)),
                a,
            )

    fbx_path = PROPS_DIR / "BuildApronMarkings.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_apron_markings()


if __name__ == "__main__":
    main()
