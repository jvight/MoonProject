"""Generate Deliverable 5 R1 story props:
- Assets/_Project/Art/R1/Models/Props/DeadAntennaMast.fbx
- Assets/_Project/Art/R1/Models/Props/SignalPillar.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_dead_antenna_and_signal.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import antenna_mast  # noqa: E402
import r1kit  # noqa: E402


PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"


def _build_bent_weathered_dish(az_obj: bpy.types.Object) -> bpy.types.Object:
    """Build a weathered, drooped/bent parabolic dish with a snapped feed-horn strut for DeadAntennaMast."""
    d = antenna_mast.ANTENNA_MAST_DIMS
    ax = d["dish_az_x"]
    py = d["dish_piv_y"]
    az = d["dish_az_z"]

    dish_mb = r1kit.MeshBuilder("Dish", pivot_u=(ax, py, az))
    r_max = d["dish_diameter"] * 0.5  # 0.475 m
    depth_z = d["dish_depth"]         # 0.18 m
    apex_z = az + 0.045

    # Helper that rotates dish coordinates around (ax, py, az) into a drooped, storm/impact-bent posture
    # (+28 deg downward droop around X, -14 deg yaw twist around Y, +11 deg roll around Z)
    pitch_rad = math.radians(28.0)
    yaw_rad = math.radians(-14.0)
    roll_rad = math.radians(11.0)
    cp, sp = math.cos(pitch_rad), math.sin(pitch_rad)
    cy, sy = math.cos(yaw_rad), math.sin(yaw_rad)
    cr, sr = math.cos(roll_rad), math.sin(roll_rad)

    def _transform_bent_u(pt_u: Tuple[float, float, float], warp: float = 0.0) -> Tuple[float, float, float]:
        dx = pt_u[0] - ax
        dy = pt_u[1] - py
        dz = pt_u[2] - apex_z + 0.045
        # Roll in XY
        x1 = dx * cr - dy * sr
        y1 = dx * sr + dy * cr
        z1 = dz + warp
        # Pitch downward around X (tilts +Z forward/down toward -Y)
        x2 = x1
        y2 = y1 * cp - z1 * sp
        z2 = y1 * sp + z1 * cp
        # Slight yaw around Y
        x3 = x2 * cy + z2 * sy
        y3 = y2
        z3 = -x2 * sy + z2 * cy
        return (ax + x3, py + y3, az + z3)

    # Rear mounting hub & weathered steel boss
    dish_mb.add_cylinder(
        center_u=_transform_bent_u((ax, py, apex_z - 0.015)),
        radius=0.085,
        length=0.042,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.005,
        rot_deg_u=(28.0, -14.0, 11.0),
    )
    dish_mb.add_cylinder(
        center_u=_transform_bent_u((ax, py, apex_z + 0.012)),
        radius=0.042,
        length=0.028,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(28.0, -14.0, 11.0),
    )

    n_ribs = 12
    ring_radii = (r_max * 0.45, r_max * 0.74, r_max)
    ring_mats = ("R1_BareSteel", "R1_BareSteel", "R1_GreyMetal")
    ring_thick = (0.012, 0.014, 0.022)

    def _dish_pt(r: float, theta_rad: float, rib_idx: int) -> Tuple[float, float, float]:
        u = r / r_max
        z_off = depth_z * (u * u)
        # Dent/warp the upper-left sector of the dish rim (ribs 2, 3, 4) so the dish silhouette is visibly bent
        warp_z = 0.0
        r_eff = r
        if rib_idx in (2, 3, 4) and u > 0.55:
            dent_strength = 0.095 if rib_idx == 3 else 0.055
            warp_z = -dent_strength * ((u - 0.45) / 0.55)
            r_eff = r * (0.90 if rib_idx == 3 else 0.94)
        raw_u = (
            ax + r_eff * math.cos(theta_rad),
            py + r_eff * math.sin(theta_rad),
            apex_z + z_off,
        )
        return _transform_bent_u(raw_u, warp=warp_z)

    # 12 radial ribs
    for i in range(n_ribs):
        th = (2.0 * math.pi * i) / n_ribs
        p0 = _dish_pt(0.072, th, i)
        p1 = _dish_pt(ring_radii[1], th, i)
        p2 = _dish_pt(r_max, th, i)
        dish_mb.add_strut(p0, p1, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)
        dish_mb.add_strut(p1, p2, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)

    # 3 concentric 12-sided polygon hoops
    for r_ring, m_ring, t_ring in zip(ring_radii, ring_mats, ring_thick):
        for i in range(n_ribs):
            i_next = (i + 1) % n_ribs
            th0 = (2.0 * math.pi * i) / n_ribs
            th1 = (2.0 * math.pi * i_next) / n_ribs
            dish_mb.add_strut(
                _dish_pt(r_ring, th0, i),
                _dish_pt(r_ring, th1, i_next),
                t_ring,
                t_ring,
                m_ring,
                bevel=0.0,
            )

    # Off-kilter feed horn (one of the 3 tripod struts is snapped and bent aside)
    focal_u = _transform_bent_u((ax - 0.03, py - 0.04, apex_z + 0.31))
    for idx_f, (angle_deg, rib_i) in enumerate(((90.0, 3), (210.0, 7), (330.0, 11))):
        th_f = math.radians(angle_deg)
        p_base = _dish_pt(ring_radii[1], th_f, rib_i)
        if idx_f == 0:
            # Snapped upper strut hanging loose
            snapped_tip = _transform_bent_u((ax + 0.08, py + 0.06, apex_z + 0.21))
            dish_mb.add_strut(p_base, snapped_tip, 0.012, 0.012, "R1_BareSteel", bevel=0.0)
        else:
            dish_mb.add_strut(p_base, focal_u, 0.012, 0.012, "R1_BareSteel", bevel=0.0)

    dish_mb.add_cylinder(
        center_u=focal_u,
        radius=0.044,
        radius_top=0.028,
        length=0.056,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        rot_deg_u=(34.0, -18.0, 15.0),
    )

    return dish_mb.build_object(parent=az_obj)


def build_dead_antenna_mast() -> bpy.types.Object:
    """Build DeadAntennaMast (relay.1 silhouette on the west rim: weathered, bent dish, zero lights)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)

    root = r1kit.create_empty("DeadAntennaMast", pivot_u=(0.0, 0.0, 0.0))
    antenna_mast._build_stage_1(root)
    antenna_mast._build_stage_2(root)
    antenna_mast._build_stage_3(root)
    antenna_mast._build_stage_4(root)

    # Replace the pristine Dish with our bent, drooped, weathered Dish
    az_obj = bpy.data.objects.get("DishAzimuth")
    old_dish = bpy.data.objects.get("Dish")
    if old_dish is not None:
        old_mesh = old_dish.data
        bpy.data.objects.remove(old_dish, do_unlink=True)
        if old_mesh is not None and old_mesh.users == 0:
            bpy.data.meshes.remove(old_mesh)
    if az_obj is not None:
        _build_bent_weathered_dish(az_obj)

    # Replace ALL emissive materials (R1_LampAmber, R1_Screen) with cold dark R1_DarkGlass / R1_GreyMetal
    mats = r1kit.ensure_materials(use_vertex_color_shading=False)
    dark_glass_mat = mats["R1_DarkGlass"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        for slot_idx, mat in enumerate(mesh.materials):
            if mat is not None and mat.name in ("R1_LampAmber", "R1_Screen"):
                mesh.materials[slot_idx] = dark_glass_mat

        # Deduplicate material slots if R1_DarkGlass now appears twice on a mesh
        slot_names = [m.name if m else "" for m in mesh.materials]
        if len(set(slot_names)) < len(slot_names):
            canonical_order = []
            remap = {}
            for old_i, m_name in enumerate(slot_names):
                if m_name not in canonical_order:
                    canonical_order.append(m_name)
                remap[old_i] = canonical_order.index(m_name)
            for poly in mesh.polygons:
                poly.material_index = remap.get(poly.material_index, 0)
            mesh.materials.clear()
            for m_name in canonical_order:
                mesh.materials.append(mats[m_name])

    antenna_mast.pose_antenna_mast_rest(root)
    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(
        root,
        footprint_radius=antenna_mast.ANTENNA_MAST_DIMS["footprint_width"] * 0.55,
    )

    # Boost dust and cavity darkening so DeadAntennaMast reads as cold, weathered, and long-dead
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or "Col" not in obj.data.color_attributes:
            continue
        attr = obj.data.color_attributes["Col"]
        for item in attr.data:
            r, g, b, a = item.color
            item.color = (
                max(0.18, min(1.0, r * 0.84)),
                max(0.0, min(1.0, g * 1.18 + 0.06)),
                max(0.0, min(1.0, b + 0.24)),
                a,
            )

    fbx_path = PROPS_DIR / "DeadAntennaMast.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_segmented_vertical_cylinder(
    bm: bmesh.types.BMesh,
    radius: float,
    height_y: float,
    radial_segs: int,
    height_segs: int,
    mat_idx: int,
) -> None:
    """Add a closed, watertight manifold vertical cylinder with height subdivisions along Blender +Z (Unity +Y)."""
    rings: List[List[bmesh.types.BMVert]] = []
    for h_i in range(height_segs + 1):
        z_bl = (height_y * h_i) / height_segs
        ring: List[bmesh.types.BMVert] = []
        for s_i in range(radial_segs):
            ang = (2.0 * math.pi * s_i) / radial_segs
            x_bl = radius * math.cos(ang)
            y_bl = radius * math.sin(ang)
            ring.append(bm.verts.new((x_bl, y_bl, z_bl)))
        rings.append(ring)

    faces: List[bmesh.types.BMFace] = []
    for h_i in range(height_segs):
        r0 = rings[h_i]
        r1 = rings[h_i + 1]
        for s_i in range(radial_segs):
            s_next = (s_i + 1) % radial_segs
            f = bm.faces.new((r0[s_i], r0[s_next], r1[s_next], r1[s_i]))
            f.material_index = mat_idx
            f.smooth = True
            faces.append(f)

    # Bottom and top end-caps so the mesh is 100% closed manifold (boundary_edges == 0)
    f_bot = bm.faces.new(list(reversed(rings[0])))
    f_bot.material_index = mat_idx
    f_bot.smooth = False
    faces.append(f_bot)

    f_top = bm.faces.new(rings[-1])
    f_top.material_index = mat_idx
    f_top.smooth = False
    faces.append(f_top)

    bmesh.ops.recalc_face_normals(bm, faces=faces)


def build_signal_pillar() -> bpy.types.Object:
    """Build B11 'pillar of light' stylised vacuum signal beam (~0.88 W x 0.88 L x 60.0 H m)."""
    r1kit.reset_scene()
    mats = r1kit.ensure_materials(use_vertex_color_shading=False)

    root = r1kit.create_empty("SignalPillar", pivot_u=(0.0, 0.0, 0.0))

    pillar_height = 60.0
    r_core = 0.14
    r_halo = 0.44

    bm = bmesh.new()
    # Two concentric closed manifold cylinders: inner core (r=0.14 m) and outer soft envelope (r=0.44 m)
    _add_segmented_vertical_cylinder(bm, radius=r_core, height_y=pillar_height, radial_segs=16, height_segs=16, mat_idx=0)
    _add_segmented_vertical_cylinder(bm, radius=r_halo, height_y=pillar_height, radial_segs=20, height_segs=16, mat_idx=0)

    me = bpy.data.meshes.new("PillarBeam")
    bm.to_mesh(me)
    bm.free()

    # Use R1_LampAmber slot in FBX (replaced in Unity prefab by R1_SignalPillar.mat with MoonProject/R1/SignalPillar shader)
    me.materials.append(mats["R1_LampAmber"])

    beam_obj = bpy.data.objects.new("PillarBeam", me)
    bpy.context.collection.objects.link(beam_obj)
    beam_obj.parent = root
    beam_obj.location = (0.0, 0.0, 0.0)

    r1kit.apply_weighted_normals(beam_obj, use_modifier=True)

    # Set clean cylindrical UVs (U = angle around cylinder, V = normalized height 0..1)
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv_layer = me.uv_layers.active.data
    for poly in me.polygons:
        for loop_idx in poly.loop_indices:
            v_co = me.vertices[me.loops[loop_idx].vertex_index].co
            ang01 = (math.atan2(v_co.y, v_co.x) / (2.0 * math.pi)) % 1.0
            h01 = max(0.0, min(1.0, v_co.z / pillar_height))
            uv_layer[loop_idx].uv = (ang01, h01)

    # Bake custom vertex colors encoding vertical height fade in R, core-vs-halo weight in G, and soft top fade in B
    if "Col" in me.color_attributes:
        me.color_attributes.remove(me.color_attributes["Col"])
    col_attr = me.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    for poly in me.polygons:
        is_cap = abs(poly.normal.z) > 0.8
        for loop_idx in poly.loop_indices:
            v_co = me.vertices[me.loops[loop_idx].vertex_index].co
            rad = math.hypot(v_co.x, v_co.y)
            h01 = max(0.0, min(1.0, v_co.z / pillar_height))
            # Soft fade at the top and zero on end-caps
            vert_fade = 0.0 if is_cap else ((1.0 - h01) ** 1.65)
            is_inner = 1.0 if rad < 0.25 else 0.42
            col_attr.data[loop_idx].color = (
                max(0.02, min(1.0, vert_fade)),
                is_inner,
                h01,
                1.0,
            )

    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Top_Anchor", parent=root, pivot_u=(0.0, pillar_height, 0.0))

    fbx_path = PROPS_DIR / "SignalPillar.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_dead_antenna_mast()
    build_signal_pillar()


if __name__ == "__main__":
    main()
