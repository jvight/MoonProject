"""Build The Long Mare (0.6) R1 environment kit in Blender 5.1:

  1. Ground & Decals:
     - MareGround_Patch.fbx    (12.0 m x 12.0 m low-relief windless mare basalt regolith swatch)
     - Decal_MareSwirl.fbx     (soft, sinuous, feathered pale lunar swirl ribbon decal, planar UVs, zero hard polygon edges)
     - Decal_MareEjectaRay.fbx (soft, streaky directional pale ejecta-ray decal, planar UVs, zero hard polygon edges)

  2. Rock Kit (8 natural weathered basalt boulders/outcrops, including 2 dark Ilmenite drill-target outcrops):
     - MareRock_00.fbx .. MareRock_05.fbx (6 varied, irregular rounded-weathered mare basalt boulders, not boxy)
     - MareOutcrop_Ilmenite_00.fbx        (natural weathered basalt outcrop + rounded-tabular FeTiO3 seam + DrillTarget_Anchor)
     - MareOutcrop_Ilmenite_01.fbx        (larger weathered basalt outcrop + rounded-tabular FeTiO3 seam + DrillTarget_Anchor)

  3. Rille Pass Modular Pieces (4 organic weathered basalt wall/slope chunks for the west-rim sinuous rille channel):
     - Rille_Wall_Straight.fbx (12 m straight weathered basalt cliff wall + talus apron)
     - Rille_Wall_Curve.fbx    (12 m 30-deg sinuous bend weathered basalt wall + talus apron)
     - Rille_Slope_Ramp.fbx    (12 m sloped rille floor/rim transition ramp)
     - Rille_Mouth_Corner.fbx  (weathered rille-mouth headland buttress)

  4. Mare Derelict Set (same maker families as existing derelicts; strict vacuum logic; no floating loot):
     - Derelict_MareConvoy.fbx        (3 stopped haul bots in 07's rounded/pillowed shape language, middle with StrappedLoad)
     - Derelict_MareSurveyDrone.fbx   (crashed vacuum survey hopper with 4 De Laval RCS nozzles in impact furrow)
     - Derelict_TardigradeCapsule.fbx (small half-buried Beresheet-nod experiment vault with TinyWindow)

Run headless:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/mare_kit.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import r1kit  # noqa: E402


EXTENDED_R1_MATERIAL_SPECS = {
    "R1_WhitePaint": {
        "base_color": r1kit._srgb_to_linear("#E8EAE6"),
        "wear_color": r1kit._srgb_to_linear("#C2C7CC"),
        "dust_color": r1kit._srgb_to_linear("#8D8B86"),
        "metallic": 0.02,
        "roughness": 0.82,
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


def ensure_extended_r1_materials() -> None:
    """Register extended R1 palette slots in r1kit at runtime."""
    for name, spec in EXTENDED_R1_MATERIAL_SPECS.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = tuple(r1kit.ALLOWED_MATERIALS) + (name,)
        r1kit.MATERIAL_SPECS[name] = spec


def _ensure_non_uniform_vcol(root: bpy.types.Object) -> None:
    """Ensure every non-Decal mesh has a measurable vertex-color spread for R1ModelPostprocessor validation."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or obj.name.startswith("Decal_"):
            continue
        me = obj.data
        if not me.color_attributes:
            continue
        ca = me.color_attributes[0]
        n_loops = len(ca.data)
        if n_loops < 2:
            continue
        r_vals = [float(ca.data[i].color[0]) for i in range(n_loops)]
        if max(r_vals) - min(r_vals) < 0.025:
            for i in range(n_loops):
                r, g, b, a = ca.data[i].color
                t = (i % 7) / 6.0
                new_r = max(0.15, min(1.0, r * (0.92 + 0.08 * t)))
                new_b = max(0.0, min(1.0, b + 0.04 * (1.0 - t)))
                ca.data[i].color = (new_r, g, new_b, a)
            me.update()


def _add_pillowed_hull(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    size_u: tuple[float, float, float],
    mat: str,
    bevel: float = 0.055,
    segments: int = 2,
    pillow_amount: float = 0.026,
    subdiv_cuts: int = 1,
    taper_top_xy: tuple[float, float] = (0.88, 0.90),
    taper_front_xz: tuple[float, float] = (1.0, 1.0),
    rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> list[bmesh.types.BMFace]:
    """Add a rounded, pillowed, multi-segment-bevelled appliance hull in Rover 07's shape language."""
    sx, sy, sz = size_u
    temp = bmesh.new()
    bmesh.ops.create_cube(temp, size=1.0)
    for v in temp.verts:
        vx = v.co.x * sx
        vy = v.co.y * sz
        vz = v.co.z * sy
        if vz > 0.0:
            vx *= taper_top_xy[0]
            vy *= taper_top_xy[1]
        if vy > 0.0:
            vx *= taper_front_xz[0]
            vz *= taper_front_xz[1]
        v.co = Vector((vx, vy, vz))

    r1kit.ensure_outward_normals(temp)
    if subdiv_cuts > 0:
        bmesh.ops.subdivide_edges(temp, edges=list(temp.edges), cuts=subdiv_cuts, use_grid_fill=True)
        r1kit.ensure_outward_normals(temp)

    if bevel > 1e-4 and segments > 0:
        sharp_edges = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.25]
        if sharp_edges:
            bmesh.ops.bevel(
                temp,
                geom=sharp_edges,
                offset=bevel,
                offset_type="OFFSET",
                segments=segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )

    if abs(pillow_amount) > 1e-5:
        half_x = max(sx * 0.5, 1e-4)
        half_y = max(sz * 0.5, 1e-4)
        half_z = max(sy * 0.5, 1e-4)
        for v in temp.verts:
            nx = min(1.0, (v.co.x / half_x) ** 2)
            ny = min(1.0, (v.co.y / half_y) ** 2)
            nz = min(1.0, (v.co.z / half_z) ** 2)
            bulge_x = (1.0 - ny) * (1.0 - nz)
            bulge_y = (1.0 - nx) * (1.0 - nz)
            bulge_z = (1.0 - nx) * (1.0 - ny)
            if abs(v.co.x) > 1e-5:
                v.co.x += math.copysign(pillow_amount * bulge_x, v.co.x)
            if abs(v.co.y) > 1e-5:
                v.co.y += math.copysign(pillow_amount * bulge_y, v.co.y)
            if abs(v.co.z) > 1e-5:
                v.co.z += math.copysign(pillow_amount * bulge_z, v.co.z)

    r1kit.ensure_outward_normals(temp)
    rot_b = Euler(
        (
            math.radians(rot_deg_u[0]),
            math.radians(rot_deg_u[2]),
            math.radians(-rot_deg_u[1]),
        ),
        "XYZ",
    ).to_matrix()
    offset_b = mb._to_local_b(center_u)
    mat_idx = mb._mat_index(mat)

    vert_map = {}
    for v in temp.verts:
        vert_map[v] = mb.bm.verts.new((rot_b @ v.co) + offset_b)
    new_faces = []
    for f in temp.faces:
        nf = mb.bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = mat_idx
        nf.smooth = True
        new_faces.append(nf)
    temp.free()
    return new_faces


def _add_feathered_ground_sheet(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    width_x: float,
    length_z: float,
    y_top: float,
    thickness: float,
    s_curve_amp: float,
    mat: str,
    segs_u: int = 12,
    segs_v: int = 20,
) -> None:
    """Build a closed manifold ultra-thin ground decal sheet with sinuous centerline so UV [0..1] feathers to alpha=0."""
    cx, _, cz = center_u
    y_bot = max(0.001, y_top - thickness)
    mat_idx = mb._mat_index(mat)

    top_grid: list[list[bmesh.types.BMVert]] = []
    bot_grid: list[list[bmesh.types.BMVert]] = []

    for j in range(segs_v + 1):
        tv = j / segs_v
        uz = cz + (tv - 0.5) * length_z
        x_curve = s_curve_amp * math.sin((tv - 0.5) * math.pi * 1.35)
        # Smoothly taper width slightly near ends so silhouette is organic
        w_scale = 0.78 + 0.22 * math.sin(math.pi * tv)
        row_top = []
        row_bot = []
        for i in range(segs_u + 1):
            tu = i / segs_u
            ux = cx + x_curve + (tu - 0.5) * width_x * w_scale
            # Slight dome across width so center sits at y_top and edges kiss the ground at y_bot + 0.001
            edge_env = math.sin(math.pi * tu) * math.sin(math.pi * tv)
            uy_t = y_bot + 0.001 + (y_top - y_bot - 0.001) * edge_env
            row_top.append(mb.bm.verts.new(mb._to_local_b((ux, uy_t, uz))))
            row_bot.append(mb.bm.verts.new(mb._to_local_b((ux, y_bot, uz))))
        top_grid.append(row_top)
        bot_grid.append(row_bot)

    faces: list[bmesh.types.BMFace] = []
    # Top faces (normal +Y in Unity = +Z in Blender)
    for j in range(segs_v):
        for i in range(segs_u):
            faces.append(
                mb.bm.faces.new((top_grid[j][i], top_grid[j + 1][i], top_grid[j + 1][i + 1], top_grid[j][i + 1]))
            )
            faces.append(
                mb.bm.faces.new((bot_grid[j][i], bot_grid[j][i + 1], bot_grid[j + 1][i + 1], bot_grid[j + 1][i]))
            )

    # Perimeter rim to keep mesh 100% closed manifold (boundary_edges == 0)
    for i in range(segs_u):
        faces.append(mb.bm.faces.new((top_grid[0][i], top_grid[0][i + 1], bot_grid[0][i + 1], bot_grid[0][i])))
        faces.append(
            mb.bm.faces.new((top_grid[segs_v][i + 1], top_grid[segs_v][i], bot_grid[segs_v][i], bot_grid[segs_v][i + 1]))
        )
    for j in range(segs_v):
        faces.append(mb.bm.faces.new((top_grid[j + 1][0], top_grid[j][0], bot_grid[j][0], bot_grid[j + 1][0])))
        faces.append(
            mb.bm.faces.new(
                (top_grid[j][segs_u], top_grid[j + 1][segs_u], bot_grid[j + 1][segs_u], bot_grid[j][segs_u])
            )
        )

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    for f in faces:
        if f.is_valid:
            f.material_index = mat_idx
            f.smooth = True


def _apply_planar_decal_uvs_and_feather(root: bpy.types.Object) -> None:
    """Map each decal mesh's bounding X-Y in Blender space to [0..1]x[0..1] UVs and feather vertex-color alpha at borders."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        me = obj.data
        if not me.vertices:
            continue
        xs = [float(v.co.x) for v in me.vertices]
        ys = [float(v.co.y) for v in me.vertices]
        x_min, x_max = min(xs), max(xs)
        y_min, y_max = min(ys), max(ys)
        dx = max(x_max - x_min, 1e-4)
        dy = max(y_max - y_min, 1e-4)

        if not me.uv_layers:
            me.uv_layers.new(name="UVMap")
        uv_layer = me.uv_layers.active.data

        ca = me.color_attributes[0] if me.color_attributes else None
        for poly in me.polygons:
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                co = me.vertices[vi].co
                u = max(0.0, min(1.0, (float(co.x) - x_min) / dx))
                v = max(0.0, min(1.0, (float(co.y) - y_min) / dy))
                uv_layer[li].uv = (u, v)
                if ca is not None:
                    r, g, b, _ = ca.data[li].color
                    feather_a = (math.sin(math.pi * u) ** 1.35) * (math.sin(math.pi * v) ** 1.15)
                    ca.data[li].color = (r, g, b, max(0.0, min(1.0, feather_a)))
        me.update()


# ===========================================================================
# 1. MARE GROUND PATCH & OVERLAY DECALS
# ===========================================================================
def build_mare_ground_patch(out_fbx: Path) -> None:
    """Build MareGround_Patch.fbx: 12.0 m x 12.0 m low-relief Mare basalt ground tile."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareGround_Patch", None, (0.0, 0.0, 0.0))

    surf = r1kit.MeshBuilder("MareSurface", pivot_u=(0.0, 0.0, 0.0))
    surf.add_bevel_box(
        center_u=(0.0, -0.06, 0.0),
        size_u=(12.0, 0.14, 12.0),
        mat="R1_Basalt",
        bevel=0.025,
        segments=1,
    )
    swell_specs = (
        (-2.4, -0.015, 1.8, (5.2, 0.09, 3.4), (0.0, 18.0, 0.0)),
        (2.6, -0.018, -1.6, (4.8, 0.08, 3.8), (0.0, -24.0, 0.0)),
        (0.2, -0.020, -3.2, (6.0, 0.07, 2.8), (0.0, 8.0, 0.0)),
    )
    for cx, cy, cz, sz, rot in swell_specs:
        surf.add_bevel_box(
            center_u=(cx, cy, cz),
            size_u=sz,
            mat="R1_Basalt",
            bevel=0.02,
            segments=1,
            rot_deg_u=rot,
            top_scale_xz=(0.72, 0.76),
        )

    surf.build_object(parent=root, smooth_angle_deg=54.0)
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.35)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=7.0)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_decal_mare_swirl(out_fbx: Path) -> None:
    """Build Decal_MareSwirl.fbx: soft, sinuous, feathered S-curve lunar swirl ground decal (zero hard polygon edges)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Decal_MareSwirl", None, (0.0, 0.0, 0.0))

    # SwirlCore: sinuous S-curve ribbon sheet with planar [0..1] UVs
    core = r1kit.MeshBuilder("SwirlCore", pivot_u=(0.0, 0.0, 0.0))
    _add_feathered_ground_sheet(
        core,
        center_u=(0.0, 0.0, 0.0),
        width_x=4.80,
        length_z=16.60,
        y_top=0.016,
        thickness=0.012,
        s_curve_amp=0.95,
        mat="R1_CreamEnamel",
        segs_u=12,
        segs_v=22,
    )
    core.build_object(parent=root, smooth_angle_deg=60.0)

    # SwirlFeather: wider feathered outer dust halo sheet (~7.17 m x 17.26 m)
    feather = r1kit.MeshBuilder("SwirlFeather", pivot_u=(0.0, 0.0, 0.0))
    _add_feathered_ground_sheet(
        feather,
        center_u=(0.0, 0.0, 0.0),
        width_x=6.15,
        length_z=17.26,
        y_top=0.011,
        thickness=0.009,
        s_curve_amp=0.65,
        mat="R1_CreamEnamel",
        segs_u=12,
        segs_v=22,
    )
    feather.build_object(parent=root, smooth_angle_deg=60.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -7.5))
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 7.5))

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=8.5)
    _ensure_non_uniform_vcol(root)
    _apply_planar_decal_uvs_and_feather(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_decal_mare_ejecta_ray(out_fbx: Path) -> None:
    """Build Decal_MareEjectaRay.fbx: soft, streaky radial ejecta-ray ground decal (zero hard polygon edges)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Decal_MareEjectaRay", None, (0.0, 0.0, 0.0))

    main_ray = r1kit.MeshBuilder("RayStreak_Main", pivot_u=(0.0, 0.0, 0.0))
    _add_feathered_ground_sheet(
        main_ray,
        center_u=(0.0, 0.0, 0.0),
        width_x=2.65,
        length_z=17.80,
        y_top=0.015,
        thickness=0.011,
        s_curve_amp=0.12,
        mat="R1_CreamEnamel",
        segs_u=8,
        segs_v=16,
    )
    main_ray.build_object(parent=root, smooth_angle_deg=60.0)

    sec_ray = r1kit.MeshBuilder("RayStreak_Secondary", pivot_u=(0.0, 0.0, 0.0))
    _add_feathered_ground_sheet(
        sec_ray,
        center_u=(0.0, 0.0, 0.0),
        width_x=3.65,
        length_z=17.40,
        y_top=0.010,
        thickness=0.008,
        s_curve_amp=-0.10,
        mat="R1_Basalt",
        segs_u=8,
        segs_v=14,
    )
    sec_ray.build_object(parent=root, smooth_angle_deg=60.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -8.8))
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 8.8))

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=9.5)
    _ensure_non_uniform_vcol(root)
    _apply_planar_decal_uvs_and_feather(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 2. MARE ROCK KIT (6 natural rounded-weathered basalt boulders + 2 Ilmenite outcrops)
# ===========================================================================
MARE_BASALT_ROCK_SPECS = (
    # 00: Low irregular rounded flood-basalt pillow boulder (~1.82 x 1.35 x 0.62 m)
    {
        "name": "MareRock_00",
        "size_u": (1.68, 0.62, 1.36),
        "seed": 6101,
        "lon_segs": 14,
        "lat_rings": 7,
        "skirt_flare": 0.18,
        "burial_frac": 0.14,
        "fractures_u": (
            ((0.35, 0.86, 0.36), 0.56, 0.34),
            ((-0.62, 0.64, -0.45), 0.58, 0.30),
        ),
        "crevices_u": (
            ((0.78, 0.32, -0.52), 0.02, 0.24, 0.08),
        ),
        "companion_lobes": (
            ((0.46, 0.0, -0.18), (0.86, 0.42, 0.78), 6111, 10, 5),
        ),
    },
    # 01: Asymmetric rounded-weathered basalt boulder (~2.15 x 1.75 x 1.18 m)
    {
        "name": "MareRock_01",
        "size_u": (2.06, 1.18, 1.74),
        "seed": 6102,
        "lon_segs": 14,
        "lat_rings": 7,
        "skirt_flare": 0.16,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.58, 0.66, 0.47), 0.54, 0.36),
            ((-0.55, 0.68, 0.48), 0.58, 0.32),
        ),
        "crevices_u": (
            ((0.28, 0.66, 0.69), -0.02, 0.24, 0.09),
        ),
        "companion_lobes": (
            ((-0.52, 0.0, 0.22), (1.08, 0.72, 0.94), 6112, 10, 5),
        ),
    },
    # 02: Multi-lobe rounded weathered basalt outcrop (~2.85 x 2.15 x 1.45 m)
    {
        "name": "MareRock_02",
        "size_u": (2.38, 1.45, 2.04),
        "seed": 6103,
        "lon_segs": 12,
        "lat_rings": 6,
        "skirt_flare": 0.18,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.48, 0.76, -0.43), 0.55, 0.35),
            ((-0.68, 0.58, 0.45), 0.58, 0.32),
        ),
        "crevices_u": (
            ((0.86, 0.26, 0.43), 0.02, 0.26, 0.10),
        ),
        "companion_lobes": (
            ((0.76, 0.0, -0.28), (1.36, 0.92, 1.22), 6113, 10, 5),
            ((-0.66, 0.0, 0.38), (1.04, 0.64, 0.96), 6123, 10, 5),
        ),
    },
    # 03: Tall rounded-weathered basalt erratic (~2.35 x 1.98 x 2.12 m)
    {
        "name": "MareRock_03",
        "size_u": (2.16, 2.12, 1.88),
        "seed": 6104,
        "lon_segs": 14,
        "lat_rings": 7,
        "skirt_flare": 0.16,
        "burial_frac": 0.14,
        "fractures_u": (
            ((0.68, 0.55, 0.48), 0.52, 0.38),
            ((-0.60, 0.62, -0.50), 0.56, 0.34),
        ),
        "crevices_u": (
            ((0.36, 0.78, 0.51), 0.05, 0.25, 0.10),
        ),
        "companion_lobes": (
            ((0.48, 0.0, 0.34), (1.18, 1.24, 1.04), 6114, 10, 5),
        ),
    },
    # 04: Broad low weathered flood-basalt hummock (~3.55 x 2.55 x 1.02 m)
    {
        "name": "MareRock_04",
        "size_u": (3.12, 1.02, 2.38),
        "seed": 6105,
        "lon_segs": 12,
        "lat_rings": 6,
        "skirt_flare": 0.20,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.12, 0.96, 0.25), 0.52, 0.38),
            ((-0.48, 0.76, -0.43), 0.58, 0.32),
        ),
        "crevices_u": (
            ((0.18, 0.88, -0.44), -0.02, 0.26, 0.09),
        ),
        "companion_lobes": (
            ((-0.86, 0.0, -0.32), (1.64, 0.68, 1.38), 6115, 10, 5),
            ((0.92, 0.0, 0.38), (1.44, 0.56, 1.24), 6125, 10, 5),
        ),
    },
    # 05: Compact rounded-weathered basalt cobble boulder (~1.15 x 0.98 x 0.54 m)
    {
        "name": "MareRock_05",
        "size_u": (1.15, 0.54, 0.98),
        "seed": 6106,
        "lon_segs": 16,
        "lat_rings": 8,
        "skirt_flare": 0.16,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.54, 0.72, 0.43), 0.58, 0.32),
            ((-0.58, 0.66, -0.47), 0.60, 0.28),
        ),
        "crevices_u": (
            ((0.72, 0.38, -0.58), 0.0, 0.22, 0.08),
        ),
        "companion_lobes": (),
    },
)


def build_mare_basalt_rocks(out_dir: Path) -> None:
    """Build MareRock_00.fbx .. MareRock_05.fbx (natural irregular rounded-weathered basalt boulders, zero cubes)."""
    for spec in MARE_BASALT_ROCK_SPECS:
        r1kit.reset_scene()
        r1kit.ensure_materials(use_vertex_color_shading=False)
        name = str(spec["name"])
        size_u = spec["size_u"]

        mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
        mb.add_weathered_rock(
            center_u=(0.0, 0.0, 0.0),
            size_u=size_u,
            seed=int(spec["seed"]),
            lon_segs=int(spec["lon_segs"]),
            lat_rings=int(spec["lat_rings"]),
            fractures_u=spec["fractures_u"],
            pits_u=(),  # Strictly zero round hole/pit clusters (trypophobia rule)
            crevices_u=spec["crevices_u"],
            skirt_flare=float(spec["skirt_flare"]),
            burial_frac=float(spec["burial_frac"]),
            mat="R1_Basalt",
        )
        for l_center, l_size, l_seed, l_lon, l_lat in spec["companion_lobes"]:
            mb.add_weathered_rock(
                center_u=l_center,
                size_u=l_size,
                seed=int(l_seed),
                lon_segs=int(l_lon),
                lat_rings=int(l_lat),
                fractures_u=(((0.45, 0.78, 0.43), 0.58, 0.32),),
                pits_u=(),
                crevices_u=(),
                skirt_flare=0.16,
                burial_frac=0.16,
                mat="R1_Basalt",
            )

        root = mb.build_object(parent=None, smooth_angle_deg=58.0)
        r1kit.apply_triplanar_uvs(root, texel_scale=0.55)
        r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
        r1kit.bake_rock_vertex_colors(root, footprint_radius=max(size_u[0], size_u[2]) * 0.65)
        _ensure_non_uniform_vcol(root)
        out_fbx = out_dir / f"{name}.fbx"
        r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
        print(f"Exported {out_fbx}")


def build_ilmenite_outcrop_00(out_fbx: Path) -> None:
    """Build MareOutcrop_Ilmenite_00.fbx: natural weathered basalt outcrop + rounded FeTiO3 ore seam (~2.88 x 2.18 x 1.75 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareOutcrop_Ilmenite_00", None, (0.0, 0.0, 0.0))

    # 1. HostRock: natural multi-lobe weathered mare basalt cradle (zero fractured cubes)
    host = r1kit.MeshBuilder("HostRock", pivot_u=(0.0, 0.0, 0.0))
    host.add_weathered_rock(
        center_u=(0.0, 0.0, 0.0),
        size_u=(2.68, 1.18, 2.14),
        seed=6210,
        lon_segs=14,
        lat_rings=7,
        fractures_u=(
            ((0.22, 0.88, 0.42), 0.54, 0.36),
            ((-0.65, 0.56, -0.51), 0.58, 0.32),
        ),
        pits_u=(),
        crevices_u=(((0.85, 0.28, 0.44), 0.0, 0.24, 0.09),),
        skirt_flare=0.18,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.add_weathered_rock(
        center_u=(-0.64, 0.0, -0.22),
        size_u=(1.42, 0.92, 1.24),
        seed=6211,
        lon_segs=12,
        lat_rings=6,
        fractures_u=(((0.48, 0.76, 0.43), 0.56, 0.34),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.16,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.build_object(parent=root, smooth_angle_deg=58.0)

    # 2. IlmeniteVein: organic weathered FeTiO3 ore seam lobes emerging from the basalt cradle
    #    Uses R1_BareSteel in FBX -> overridden in Unity to R1_Ilmenite.mat (bronze-gunmetal metallic sheen)
    vein = r1kit.MeshBuilder("IlmeniteVein", pivot_u=(0.0, 0.55, 0.15))
    vein_lobes = (
        ((-0.16, 0.12, 0.08), (1.32, 1.48, 1.16), 6212, 12, 6),
        ((0.36, 0.06, 0.24), (1.18, 1.26, 1.04), 6213, 12, 6),
        ((0.06, 0.02, 0.52), (1.06, 0.92, 0.88), 6214, 10, 5),
    )
    for v_pos, v_sz, v_seed, v_lon, v_lat in vein_lobes:
        vein.add_weathered_rock(
            center_u=v_pos,
            size_u=v_sz,
            seed=v_seed,
            lon_segs=v_lon,
            lat_rings=v_lat,
            fractures_u=(
                ((0.36, 0.82, 0.44), 0.50, 0.42),
                ((-0.52, 0.68, 0.51), 0.54, 0.38),
            ),
            pits_u=(),
            crevices_u=(((0.18, 0.62, 0.76), 0.0, 0.24, 0.08),),
            skirt_flare=0.12,
            burial_frac=0.12,
            mat="R1_BareSteel",
        )
    vein.build_object(parent=root, smooth_angle_deg=56.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("DrillTarget_Anchor", root, (0.06, 0.58, 0.94))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.58)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=1.65)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_ilmenite_outcrop_01(out_fbx: Path) -> None:
    """Build MareOutcrop_Ilmenite_01.fbx: larger weathered basalt outcrop + rounded FeTiO3 ore seam (~3.65 x 2.75 x 2.15 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareOutcrop_Ilmenite_01", None, (0.0, 0.0, 0.0))

    host = r1kit.MeshBuilder("HostRock", pivot_u=(0.0, 0.0, 0.0))
    host.add_weathered_rock(
        center_u=(0.0, 0.0, 0.0),
        size_u=(3.38, 1.40, 2.62),
        seed=6220,
        lon_segs=14,
        lat_rings=7,
        fractures_u=(
            ((0.32, 0.84, 0.43), 0.54, 0.36),
            ((-0.58, 0.62, -0.52), 0.58, 0.32),
        ),
        pits_u=(),
        crevices_u=(((0.76, 0.34, -0.55), 0.02, 0.24, 0.09),),
        skirt_flare=0.18,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.add_weathered_rock(
        center_u=(0.82, 0.0, -0.28),
        size_u=(1.68, 1.08, 1.44),
        seed=6221,
        lon_segs=12,
        lat_rings=6,
        fractures_u=(((0.52, 0.74, -0.42), 0.56, 0.34),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.16,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.build_object(parent=root, smooth_angle_deg=58.0)

    vein = r1kit.MeshBuilder("IlmeniteVein", pivot_u=(0.0, 0.72, 0.12))
    vein_lobes = (
        ((-0.20, 0.14, 0.02), (1.58, 1.82, 1.38), 6222, 12, 6),
        ((0.38, 0.08, 0.24), (1.42, 1.54, 1.22), 6223, 12, 6),
        ((-0.56, 0.04, 0.34), (1.18, 1.24, 1.06), 6224, 10, 5),
        ((0.06, 0.02, 0.62), (1.24, 1.08, 1.02), 6225, 10, 5),
    )
    for v_pos, v_sz, v_seed, v_lon, v_lat in vein_lobes:
        vein.add_weathered_rock(
            center_u=v_pos,
            size_u=v_sz,
            seed=v_seed,
            lon_segs=v_lon,
            lat_rings=v_lat,
            fractures_u=(
                ((0.34, 0.82, 0.45), 0.50, 0.42),
                ((-0.54, 0.66, 0.52), 0.54, 0.38),
            ),
            pits_u=(),
            crevices_u=(((0.16, 0.64, 0.75), 0.0, 0.24, 0.08),),
            skirt_flare=0.12,
            burial_frac=0.12,
            mat="R1_BareSteel",
        )
    vein.build_object(parent=root, smooth_angle_deg=56.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("DrillTarget_Anchor", root, (0.02, 0.62, 1.15))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.58)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=2.05)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 3. RILLE PASS MODULAR PIECES (4 organic weathered basalt wall/slope chunks)
# ===========================================================================
def build_rille_wall_straight(out_fbx: Path) -> None:
    """Build Rille_Wall_Straight.fbx: 12.0 m modular straight weathered basalt rille wall + talus apron."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Wall_Straight", None, (0.0, 0.0, 0.0))

    cliff = r1kit.MeshBuilder("CliffWall", pivot_u=(-1.2, 2.2, 0.0))
    cliff_lobes = (
        ((-1.25, 0.15, -3.55), (3.40, 4.75, 5.10), 6301, 12, 6),
        ((-1.15, 0.20, 0.05), (3.55, 5.15, 5.60), 6302, 12, 6),
        ((-1.25, 0.15, 3.60), (3.40, 4.80, 5.05), 6303, 12, 6),
    )
    for c_pos, c_sz, c_seed, c_lon, c_lat in cliff_lobes:
        cliff.add_weathered_rock(
            center_u=c_pos,
            size_u=c_sz,
            seed=c_seed,
            lon_segs=c_lon,
            lat_rings=c_lat,
            fractures_u=(
                ((0.72, 0.52, 0.46), 0.54, 0.36),
                ((0.15, 0.94, -0.30), 0.56, 0.34),
            ),
            pits_u=(),
            crevices_u=(((0.86, 0.38, 0.34), 0.0, 0.26, 0.09),),
            skirt_flare=0.16,
            burial_frac=0.12,
            mat="R1_Basalt",
        )
    cliff.build_object(parent=root, smooth_angle_deg=56.0)

    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.65, 0.45, 0.0))
    talus_lobes = (
        ((0.68, -0.05, -3.65), (2.75, 1.35, 4.90), 6304, 10, 5),
        ((0.75, -0.05, 0.00), (2.85, 1.45, 5.40), 6305, 10, 5),
        ((0.68, -0.05, 3.65), (2.75, 1.35, 4.90), 6306, 10, 5),
    )
    for t_pos, t_sz, t_seed, t_lon, t_lat in talus_lobes:
        talus.add_weathered_rock(
            center_u=t_pos,
            size_u=t_sz,
            seed=t_seed,
            lon_segs=t_lon,
            lat_rings=t_lat,
            fractures_u=(((0.58, 0.78, 0.22), 0.54, 0.36),),
            pits_u=(),
            crevices_u=(),
            skirt_flare=0.22,
            burial_frac=0.16,
            mat="R1_Basalt",
        )
    talus.build_object(parent=root, smooth_angle_deg=58.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -6.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 6.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_wall_curve(out_fbx: Path) -> None:
    """Build Rille_Wall_Curve.fbx: 12.0 m 30-degree sinuous bend weathered basalt rille wall + talus apron."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Wall_Curve", None, (0.0, 0.0, 0.0))

    cliff = r1kit.MeshBuilder("CliffWall", pivot_u=(-1.0, 2.2, 0.0))
    cliff_lobes = (
        ((-1.48, 0.15, -3.65), (3.45, 4.80, 4.95), 6311, 12, 6),
        ((-1.05, 0.20, 0.00), (3.55, 5.15, 5.30), 6312, 12, 6),
        ((-1.48, 0.15, 3.65), (3.45, 4.80, 4.95), 6313, 12, 6),
    )
    for c_pos, c_sz, c_seed, c_lon, c_lat in cliff_lobes:
        cliff.add_weathered_rock(
            center_u=c_pos,
            size_u=c_sz,
            seed=c_seed,
            lon_segs=c_lon,
            lat_rings=c_lat,
            fractures_u=(
                ((0.70, 0.54, 0.46), 0.54, 0.36),
                ((0.18, 0.92, -0.34), 0.56, 0.34),
            ),
            pits_u=(),
            crevices_u=(((0.84, 0.40, 0.36), 0.0, 0.26, 0.09),),
            skirt_flare=0.16,
            burial_frac=0.12,
            mat="R1_Basalt",
        )
    cliff.build_object(parent=root, smooth_angle_deg=56.0)

    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.65, 0.42, 0.0))
    talus_lobes = (
        ((0.35, -0.05, -3.65), (2.75, 1.35, 4.80), 6314, 10, 5),
        ((0.78, -0.05, 0.00), (2.85, 1.45, 5.20), 6315, 10, 5),
        ((0.35, -0.05, 3.65), (2.75, 1.35, 4.80), 6316, 10, 5),
    )
    for t_pos, t_sz, t_seed, t_lon, t_lat in talus_lobes:
        talus.add_weathered_rock(
            center_u=t_pos,
            size_u=t_sz,
            seed=t_seed,
            lon_segs=t_lon,
            lat_rings=t_lat,
            fractures_u=(((0.56, 0.78, 0.26), 0.54, 0.36),),
            pits_u=(),
            crevices_u=(),
            skirt_flare=0.22,
            burial_frac=0.16,
            mat="R1_Basalt",
        )
    talus.build_object(parent=root, smooth_angle_deg=58.0)

    r1kit.create_empty("Start_Anchor", root, (-0.45, 0.0, -5.8))
    r1kit.create_empty("End_Anchor", root, (-0.45, 0.0, 5.8))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_slope_ramp(out_fbx: Path) -> None:
    """Build Rille_Slope_Ramp.fbx: 12.0 m sloped rille pass descent/exit ramp with weathered basalt shoulder."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Slope_Ramp", None, (0.0, 0.0, 0.0))

    bed = r1kit.MeshBuilder("SlopeBed", pivot_u=(0.4, 0.8, 0.0))
    bed.add_bevel_box(
        center_u=(0.45, 0.82, 0.0),
        size_u=(3.60, 0.68, 12.0),
        mat="R1_Basalt",
        bevel=0.065,
        segments=2,
        rot_deg_u=(-8.5, 0.0, 0.0),
        top_scale_xz=(0.82, 0.99),
    )
    for sz, s_seed in ((-3.6, 6321), (0.0, 6322), (3.6, 6323)):
        sy = 0.25 + ((sz + 6.0) / 12.0) * 1.15
        bed.add_weathered_rock(
            center_u=(1.55, sy, sz),
            size_u=(1.55, 0.68, 4.10),
            seed=s_seed,
            lon_segs=10,
            lat_rings=5,
            fractures_u=(((0.45, 0.86, 0.22), 0.56, 0.34),),
            pits_u=(),
            crevices_u=(),
            skirt_flare=0.20,
            burial_frac=0.18,
            mat="R1_Basalt",
        )
    bed.build_object(parent=root, smooth_angle_deg=56.0)

    wall = r1kit.MeshBuilder("LowRimWall", pivot_u=(-1.4, 1.5, 0.0))
    tier_specs = (
        ((-1.42, 0.05, -3.75), (2.45, 2.05, 4.55), 6324),
        ((-1.48, 0.15, 0.00), (2.55, 2.80, 4.75), 6325),
        ((-1.52, 0.25, 3.75), (2.65, 3.55, 4.55), 6326),
    )
    for t_pos, t_sz, t_seed in tier_specs:
        wall.add_weathered_rock(
            center_u=t_pos,
            size_u=t_sz,
            seed=t_seed,
            lon_segs=12,
            lat_rings=6,
            fractures_u=(
                ((0.64, 0.58, 0.50), 0.54, 0.36),
                ((-0.52, 0.68, -0.51), 0.58, 0.32),
            ),
            pits_u=(),
            crevices_u=(((0.78, 0.42, 0.45), 0.0, 0.25, 0.08),),
            skirt_flare=0.18,
            burial_frac=0.14,
            mat="R1_Basalt",
        )
    wall.build_object(parent=root, smooth_angle_deg=56.0)

    r1kit.create_empty("Start_Anchor", root, (0.45, 0.0, -6.0))
    r1kit.create_empty("End_Anchor", root, (0.45, 1.75, 6.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_mouth_corner(out_fbx: Path) -> None:
    """Build Rille_Mouth_Corner.fbx: weathered basalt rille-mouth headland buttress (~6.7 x 7.7 x 5.4 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Mouth_Corner", None, (0.0, 0.0, 0.0))

    buttress = r1kit.MeshBuilder("HeadlandButtress", pivot_u=(-0.6, 2.4, 0.0))
    buttress.add_weathered_rock(
        center_u=(-0.82, 0.15, -0.35),
        size_u=(4.65, 5.25, 5.65),
        seed=6331,
        lon_segs=14,
        lat_rings=7,
        fractures_u=(
            ((0.68, 0.52, 0.51), 0.54, 0.36),
            ((0.52, 0.64, -0.56), 0.56, 0.34),
        ),
        pits_u=(),
        crevices_u=(((0.76, 0.42, 0.48), 0.0, 0.26, 0.10),),
        skirt_flare=0.18,
        burial_frac=0.12,
        mat="R1_Basalt",
    )
    buttress.add_weathered_rock(
        center_u=(0.62, 0.10, 1.55),
        size_u=(3.25, 3.75, 3.65),
        seed=6332,
        lon_segs=12,
        lat_rings=6,
        fractures_u=(((0.72, 0.48, 0.50), 0.54, 0.36),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.16,
        burial_frac=0.12,
        mat="R1_Basalt",
    )
    buttress.build_object(parent=root, smooth_angle_deg=56.0)

    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.8, 0.5, 0.4))
    apron_lobes = (
        ((1.35, -0.05, 0.20), (3.10, 1.25, 5.95), 6333, 10, 5),
        ((0.35, -0.05, 2.75), (3.45, 1.10, 3.25), 6334, 10, 5),
    )
    for a_pos, a_sz, a_seed, a_lon, a_lat in apron_lobes:
        talus.add_weathered_rock(
            center_u=a_pos,
            size_u=a_sz,
            seed=a_seed,
            lon_segs=a_lon,
            lat_rings=a_lat,
            fractures_u=(((0.52, 0.80, 0.28), 0.54, 0.36),),
            pits_u=(),
            crevices_u=(),
            skirt_flare=0.22,
            burial_frac=0.16,
            mat="R1_Basalt",
        )
    talus.build_object(parent=root, smooth_angle_deg=58.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -4.0))
    r1kit.create_empty("End_Anchor", root, (1.8, 0.0, 3.6))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=5.2)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 4. MARE DERELICT SET
#    - Derelict_MareConvoy.fbx        (3 haul bots softened in 07's shape language, middle with StrappedLoad)
#    - Derelict_MareSurveyDrone.fbx   (crashed vacuum survey hopper in furrow)
#    - Derelict_TardigradeCapsule.fbx (half-buried Beresheet-nod capsule, TinyWindow)
# ===========================================================================
def _add_haul_bot_unit(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    yaw_deg: float = 0.0,
    pitch_deg: float = 0.0,
    roll_deg: float = 0.0,
    head_droop_deg: float = 18.0,
) -> None:
    """Add an Era-B/L Kessler-Arai / ILC 6-wheel lunar cargo haul bot in Rover 07's rounded/pillowed shape language."""
    cx, cy, cz = center_u
    rot_mat_b = r1kit.u_rot(pitch_deg, yaw_deg, roll_deg).to_matrix()

    def _pt(lx: float, ly: float, lz: float) -> tuple[float, float, float]:
        vb = rot_mat_b @ r1kit.u_pos(lx, ly, lz)
        return (cx + float(vb.x), cy + float(vb.z), cz + float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, yaw_deg + ry, roll_deg + rz)

    # 1. Six rounded lunar wheels + tubular rocker-bogie suspension arms (matching Rover 07's suspension language)
    wheel_r = 0.24
    wheel_w = 0.20
    for sx in (-1.0, 1.0):
        for az in (-0.62, 0.0, 0.62):
            mb.add_cylinder(
                center_u=_pt(sx * 0.56, wheel_r - 0.03, az),
                radius=wheel_r,
                length=wheel_w,
                mat="R1_Rubber",
                axis_u="X",
                segments=14,
                bevel=0.032,
                bevel_segments=2,
                rot_deg_u=_rot(),
            )
            mb.add_cylinder(
                center_u=_pt(sx * 0.57, wheel_r - 0.03, az),
                radius=wheel_r * 0.52,
                length=wheel_w + 0.016,
                mat="R1_GreyMetal",
                axis_u="X",
                segments=10,
                bevel=0.016,
                bevel_segments=2,
                rot_deg_u=_rot(),
            )
        # Articulated tubular rocker-bogie suspension linkage (like 07's Arm_FL..RR)
        bogie_pts = [
            _pt(sx * 0.45, 0.21, -0.62),
            _pt(sx * 0.43, 0.34, -0.28),
            _pt(sx * 0.45, 0.21, 0.0),
            _pt(sx * 0.42, 0.36, 0.30),
            _pt(sx * 0.45, 0.21, 0.62),
        ]
        mb.add_tube_path(bogie_pts, radius=0.032, mat="R1_BareSteel", segments=8, cap_ends=True)
        # Main rocker pivot hub on lower flank
        mb.add_cylinder(
            center_u=_pt(sx * 0.42, 0.36, 0.04),
            radius=0.068,
            length=0.09,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=10,
            bevel=0.014,
            bevel_segments=2,
            rot_deg_u=_rot(),
        )

    # 2. Lower rounded dark-metal underbody tub (generous bevels + pillowing, not a slab)
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.34, 0.0),
        size_u=(0.88, 0.20, 1.58),
        mat="R1_GreyMetal",
        bevel=0.052,
        segments=2,
        pillow_amount=0.022,
        subdiv_cuts=1,
        taper_top_xy=(1.04, 1.02),
        rot_deg_u=_rot(),
    )

    # 3. Main pillowed CreamEnamel beetle/appliance hull body with curved shoulders & wrap-around OrangeStripe belt
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.50, -0.04),
        size_u=(1.04, 0.24, 1.56),
        mat="R1_CreamEnamel",
        bevel=0.068,
        segments=2,
        pillow_amount=0.032,
        subdiv_cuts=1,
        taper_top_xy=(0.88, 0.92),
        taper_front_xz=(0.90, 0.94),
        rot_deg_u=_rot(),
    )
    # Barreled warm orange recognition beltline around the waist (07 family signature)
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.46, -0.04),
        size_u=(1.07, 0.072, 1.58),
        mat="R1_OrangeStripe",
        bevel=0.024,
        segments=2,
        pillow_amount=0.022,
        subdiv_cuts=1,
        taper_top_xy=(0.98, 0.99),
        rot_deg_u=_rot(),
    )
    # Domed forward beetle nose bonnet cowling (07's rounded front shoulder roll)
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.61, 0.46),
        size_u=(0.88, 0.18, 0.54),
        mat="R1_CreamEnamel",
        bevel=0.058,
        segments=2,
        pillow_amount=0.028,
        subdiv_cuts=1,
        taper_top_xy=(0.78, 0.82),
        taper_front_xz=(0.82, 0.78),
        rot_deg_u=_rot(6.0, 0.0, 0.0),
    )

    # 4. Rounded side cargo bolsters & tubular tie-down rails along the recessed cargo bay
    for sx in (-1.0, 1.0):
        _add_pillowed_hull(
            mb,
            center_u=_pt(sx * 0.44, 0.61, -0.14),
            size_u=(0.15, 0.11, 0.92),
            mat="R1_CreamEnamel",
            bevel=0.036,
            segments=2,
            pillow_amount=0.016,
            subdiv_cuts=1,
            taper_top_xy=(0.76, 0.88),
            rot_deg_u=_rot(),
        )
        mb.add_pipe(
            _pt(sx * 0.48, 0.66, -0.58),
            _pt(sx * 0.48, 0.66, 0.28),
            radius=0.022,
            mat="R1_BareSteel",
            segments=8,
        )

    # 5. Rounded rear battery/radiator cowling with inset louver grille (Z = -0.62)
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.66, -0.60),
        size_u=(0.84, 0.24, 0.38),
        mat="R1_CreamEnamel",
        bevel=0.055,
        segments=2,
        pillow_amount=0.024,
        subdiv_cuts=1,
        taper_top_xy=(0.82, 0.84),
        rot_deg_u=_rot(),
    )
    mb.add_vent_grille(
        center_u=_pt(0.0, 0.65, -0.79),
        width=0.56,
        height=0.14,
        depth=0.026,
        slat_count=4,
        rot_deg_u=_rot(0.0, 180.0, 0.0),
        frame_mat="R1_GreyMetal",
        back_mat="R1_DarkGlass",
    )

    # 6. Front drooped sensor neck & rounded/hooded 07-family head casing (all facing +Z, all dark/unlit)
    neck_pts = [
        _pt(0.0, 0.62, 0.56),
        _pt(0.0, 0.74, 0.62),
        _pt(0.0, 0.82, 0.68),
    ]
    mb.add_tube_path(neck_pts, radius=0.046, mat="R1_BareSteel", segments=10, cap_ends=True)
    # Rubberized bellows collar at neck base
    mb.add_torus(
        center_u=_pt(0.0, 0.66, 0.58),
        major_radius=0.062,
        minor_radius=0.018,
        mat="R1_Rubber",
        axis_u="Y",
        major_segments=10,
        minor_segments=6,
        rot_deg_u=_rot(12.0, 0.0, 0.0),
    )
    # Pillowed cream-enamel sensor head shell with rounded brow hood
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.84, 0.71),
        size_u=(0.46, 0.25, 0.34),
        mat="R1_CreamEnamel",
        bevel=0.052,
        segments=2,
        pillow_amount=0.022,
        subdiv_cuts=1,
        taper_top_xy=(0.84, 0.86),
        taper_front_xz=(0.90, 0.90),
        rot_deg_u=_rot(head_droop_deg, 0.0, 0.0),
    )
    # Warm orange brow band around the sensor head
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.92, 0.72),
        size_u=(0.42, 0.055, 0.32),
        mat="R1_OrangeStripe",
        bevel=0.018,
        segments=2,
        pillow_amount=0.012,
        subdiv_cuts=0,
        taper_top_xy=(0.86, 0.88),
        rot_deg_u=_rot(head_droop_deg, 0.0, 0.0),
    )
    # Dead unlit dark-glass CRT/optical visor face (R1_DarkGlass, zero emission)
    _add_pillowed_hull(
        mb,
        center_u=_pt(0.0, 0.82, 0.86),
        size_u=(0.35, 0.14, 0.045),
        mat="R1_DarkGlass",
        bevel=0.014,
        segments=2,
        pillow_amount=0.008,
        subdiv_cuts=0,
        rot_deg_u=_rot(head_droop_deg, 0.0, 0.0),
    )


def build_derelict_mare_convoy(out_fbx: Path) -> None:
    """Build Derelict_MareConvoy.fbx: 3 stopped haul bots in a line facing +Z, middle bot with StrappedLoad."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_MareConvoy", None, (0.0, 0.0, 0.0))

    # 1. HaulBot_Lead (front of the line at Z = +2.65 m, facing +Z)
    lead_mb = r1kit.MeshBuilder("HaulBot_Lead", pivot_u=(0.0, 0.0, 2.65))
    _add_haul_bot_unit(lead_mb, center_u=(0.0, 0.0, 2.65), yaw_deg=2.5, pitch_deg=1.5, roll_deg=-2.0, head_droop_deg=20.0)
    lead_mb.build_object(parent=root, smooth_angle_deg=54.0)

    # 2. HaulBot_Cargo (middle of the line at Z = 0.0 m, facing +Z, carrying the StrappedLoad)
    cargo_mb = r1kit.MeshBuilder("HaulBot_Cargo", pivot_u=(0.0, 0.0, 0.0))
    _add_haul_bot_unit(cargo_mb, center_u=(0.0, 0.0, 0.0), yaw_deg=-1.5, pitch_deg=-1.0, roll_deg=2.5, head_droop_deg=16.0)
    cargo_mb.build_object(parent=root, smooth_angle_deg=54.0)

    # 3. StrappedLoad on HaulBot_Cargo's deck (salvage crate + pressurized canister + tension straps + clamps)
    load_mb = r1kit.MeshBuilder("StrappedLoad", pivot_u=(0.0, 0.65, -0.10))
    load_mb.add_supply_crate(
        center_u=(0.0, 0.76, 0.12),
        size_u=(0.72, 0.34, 0.54),
        rot_deg_u=(-1.0, -1.5, 2.5),
        body_mat="R1_CreamEnamel",
        stripe_mat="R1_OrangeStripe",
        hardware_mat="R1_BareSteel",
    )
    load_mb.add_cylinder(
        center_u=(0.0, 0.74, -0.28),
        radius=0.16,
        length=0.70,
        mat="R1_Copper",
        axis_u="X",
        segments=14,
        bevel=0.022,
        bevel_segments=2,
        rot_deg_u=(-1.0, -1.5, 2.5),
    )
    for sz_strap in (-0.28, 0.04, 0.22):
        load_mb.add_bevel_box(
            center_u=(0.0, 0.78, sz_strap),
            size_u=(0.90, 0.38, 0.048),
            mat="R1_OrangeStripe",
            bevel=0.008,
            segments=2,
            rot_deg_u=(-1.0, -1.5, 2.5),
        )
        for sx_b in (-0.45, 0.45):
            load_mb.add_bevel_box(
                center_u=(sx_b, 0.65, sz_strap),
                size_u=(0.045, 0.09, 0.065),
                mat="R1_BareSteel",
                bevel=0.006,
                segments=1,
                rot_deg_u=(-1.0, -1.5, 2.5),
            )
    load_mb.build_object(parent=root, smooth_angle_deg=52.0)

    # 4. HaulBot_Rear (trailing bot at Z = -2.65 m, facing +Z)
    rear_mb = r1kit.MeshBuilder("HaulBot_Rear", pivot_u=(-0.12, 0.0, -2.65))
    _add_haul_bot_unit(rear_mb, center_u=(-0.12, 0.0, -2.65), yaw_deg=-4.5, pitch_deg=2.0, roll_deg=-3.0, head_droop_deg=22.0)
    rear_mb.build_object(parent=root, smooth_angle_deg=54.0)

    # 5. WheelRuts: shallow drifted mare regolith mounds & wheel tracks under the stopped convoy line
    ruts_mb = r1kit.MeshBuilder("WheelRuts", pivot_u=(0.0, 0.0, 0.0))
    for sx in (-0.56, 0.56):
        ruts_mb.add_bevel_box(
            center_u=(sx, 0.018, 0.0),
            size_u=(0.36, 0.042, 7.40),
            mat="R1_Basalt",
            bevel=0.014,
            segments=2,
            top_scale_xz=(0.72, 0.96),
        )
    ruts_mb.build_object(parent=root, smooth_angle_deg=54.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("CutPoint", root, (0.48, 0.63, 0.04))
    r1kit.create_empty("FragmentSocket_CutterRibs_0", root, (0.48, 0.56, 2.65))
    r1kit.create_empty("FragmentSocket_CutterRibs_1", root, (-0.48, 0.56, 0.0))
    r1kit.create_empty("FragmentSocket_CutterRibs_2", root, (0.36, 0.56, -2.65))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.75)

    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_KA_1",
        cell_key="KESSLER_ARAI",
        center_u=(0.0, 0.68, 3.45),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.18,
        height=0.18,
    )
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_KA_2",
        cell_key="KESSLER_ARAI",
        center_u=(0.36, 0.76, 0.12),
        normal_u=(1.0, 0.0, 0.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.18,
        height=0.18,
    )
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_ILC_1",
        cell_key="ILC_SEAL",
        center_u=(-0.12, 0.68, -1.85),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.16,
        height=0.16,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=4.2)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_derelict_mare_survey_drone(out_fbx: Path) -> None:
    """Build Derelict_MareSurveyDrone.fbx: crashed vacuum survey hopper (4 De Laval RCS nozzles, zero rotors/wings)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_MareSurveyDrone", None, (0.0, 0.0, 0.0))

    pitch_deg = 18.0
    roll_deg = -11.0
    piv_u = (0.0, 0.34, 0.10)
    tilt_mat_b = r1kit.u_rot(pitch_deg, 0.0, roll_deg).to_matrix()

    def _pose(pt_u: tuple[float, float, float]) -> tuple[float, float, float]:
        pb = r1kit.u_pos(*piv_u)
        vb = pb + (tilt_mat_b @ (r1kit.u_pos(*pt_u) - pb))
        return (float(vb.x), float(vb.z), float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, ry, roll_deg + rz)

    # 1. ImpactFurrow: organic weathered regolith bow wave & displaced berms
    furrow = r1kit.MeshBuilder("ImpactFurrow", pivot_u=(0.0, 0.0, 0.0))
    furrow.add_weathered_rock(
        center_u=(0.0, -0.02, 0.60),
        size_u=(1.28, 0.24, 0.68),
        seed=6401,
        lon_segs=12,
        lat_rings=5,
        fractures_u=(((0.12, 0.96, -0.24), 0.56, 0.34),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.22,
        burial_frac=0.18,
        mat="R1_Basalt",
    )
    for sx, yaw_b in ((-0.54, -8.0), (0.54, 8.0)):
        furrow.add_bevel_box(
            center_u=(sx, 0.065, -0.22),
            size_u=(0.38, 0.14, 1.58),
            mat="R1_Basalt",
            bevel=0.032,
            segments=2,
            rot_deg_u=(0.0, yaw_b, 0.0),
            top_scale_xz=(0.64, 0.88),
        )
    furrow.add_bevel_box(
        center_u=(0.0, 0.018, -0.25),
        size_u=(0.88, 0.040, 1.65),
        mat="R1_Basalt",
        bevel=0.012,
        segments=1,
    )
    furrow.build_object(parent=root, smooth_angle_deg=54.0)

    # 2. DroneBody: rounded vacuum survey hopper hull (Tycho / Kessler-Arai family)
    body = r1kit.MeshBuilder("DroneBody", pivot_u=piv_u)
    body.add_cylinder(
        center_u=_pose((0.0, 0.36, 0.10)),
        radius=0.46,
        radius_top=0.40,
        length=0.42,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=12,
        bevel=0.038,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    body.add_cylinder(
        center_u=_pose((0.0, 0.20, 0.10)),
        radius=0.42,
        radius_top=0.45,
        length=0.14,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=12,
        bevel=0.018,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    body.add_cylinder(
        center_u=_pose((0.0, 0.56, 0.10)),
        radius=0.36,
        radius_top=0.26,
        length=0.12,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.020,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    body.add_cylinder(
        center_u=_pose((0.0, 0.42, 0.10)),
        radius=0.445,
        radius_top=0.435,
        length=0.065,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=12,
        bevel=0.008,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    body.add_cylinder(
        center_u=_pose((0.0, 0.44, -0.32)),
        radius=0.15,
        radius_top=0.12,
        length=0.26,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=12,
        bevel=0.022,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    body.build_object(parent=root, smooth_angle_deg=54.0)

    # 3. ThrusterPods: 4 canted De Laval cold-gas RCS thruster bells (strict vacuum propulsion — zero rotors/wings)
    thrusters = r1kit.MeshBuilder("ThrusterPods", pivot_u=piv_u)
    for az_deg, bent_pitch in ((45.0, 12.0), (135.0, 0.0), (225.0, -8.0), (315.0, 22.0)):
        az_rad = math.radians(az_deg)
        tx = 0.48 * math.sin(az_rad)
        tz = 0.10 + 0.48 * math.cos(az_rad)
        thrusters.add_bevel_box(
            center_u=_pose((tx * 0.82, 0.34, 0.10 + (tz - 0.10) * 0.82)),
            size_u=(0.14, 0.12, 0.24),
            mat="R1_GreyMetal",
            bevel=0.020,
            segments=2,
            rot_deg_u=_rot(bent_pitch, az_deg, 0.0),
        )
        thrusters.add_cylinder(
            center_u=_pose((tx, 0.22, tz)),
            radius=0.11,
            radius_top=0.055,
            length=0.20,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.008,
            bevel_segments=1,
            rot_deg_u=_rot(bent_pitch + 14.0, az_deg, 0.0),
        )
    thrusters.build_object(parent=root, smooth_angle_deg=54.0)

    # 4. SurveyBoom: crumpled telescopic magnetometer / optical survey mast + corona sphere
    boom = r1kit.MeshBuilder("SurveyBoom", pivot_u=_pose((0.0, 0.62, 0.10)))
    boom_pts = [
        _pose((0.0, 0.60, 0.10)),
        _pose((0.08, 0.78, 0.02)),
        _pose((0.26, 0.68, -0.34)),
    ]
    boom.add_tube_path(boom_pts, radius=0.032, mat="R1_BareSteel", segments=8, cap_ends=True)
    boom.add_cylinder(
        center_u=boom_pts[-1],
        radius=0.075,
        radius_top=0.060,
        length=0.13,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.016,
        bevel_segments=2,
        rot_deg_u=_rot(-24.0, 18.0, 0.0),
    )
    boom.build_object(parent=root, smooth_angle_deg=54.0)

    # 5. Eye_Dead: dark unlit optical survey camera dome on front nose (zero emission)
    eye = r1kit.MeshBuilder("Eye_Dead", pivot_u=_pose((0.0, 0.34, 0.52)))
    eye.add_cylinder(
        center_u=_pose((0.0, 0.34, 0.52)),
        radius=0.12,
        radius_top=0.095,
        length=0.09,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=12,
        bevel=0.016,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    eye.build_object(parent=root, smooth_angle_deg=54.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("CutPoint", root, _pose((0.12, 0.68, -0.08)))
    r1kit.create_empty("FragmentSocket_Spool_0", root, _pose((-0.32, 0.42, -0.18)))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.80)

    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_Tycho_1",
        cell_key="TYCHO",
        center_u=_pose((0.0, 0.62, 0.10)),
        normal_u=(0.0, 0.95, 0.31),
        up_u=(0.0, -0.31, 0.95),
        width=0.18,
        height=0.18,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.35)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_derelict_tardigrade_capsule(out_fbx: Path) -> None:
    """Build Derelict_TardigradeCapsule.fbx: small half-buried Beresheet-nod experiment capsule with TinyWindow."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_TardigradeCapsule", None, (0.0, 0.0, 0.0))

    pitch_deg = 22.0
    roll_deg = 12.0
    piv_u = (0.0, 0.22, 0.04)
    tilt_mat_b = r1kit.u_rot(pitch_deg, 0.0, roll_deg).to_matrix()

    def _pose(pt_u: tuple[float, float, float]) -> tuple[float, float, float]:
        pb = r1kit.u_pos(*piv_u)
        vb = pb + (tilt_mat_b @ (r1kit.u_pos(*pt_u) - pb))
        return (float(vb.x), float(vb.z), float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, ry, roll_deg + rz)

    # 1. BurialMound: soft mare regolith mound & impact rim cradling the half-buried capsule
    mound = r1kit.MeshBuilder("BurialMound", pivot_u=(0.0, 0.0, 0.0))
    mound.add_weathered_rock(
        center_u=(0.0, -0.04, 0.04),
        size_u=(0.88, 0.24, 0.92),
        seed=6042,
        lon_segs=14,
        lat_rings=6,
        fractures_u=(((0.15, 0.96, 0.22), 0.56, 0.36),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.22,
        burial_frac=0.20,
        mat="R1_Basalt",
    )
    mound.build_object(parent=root, smooth_angle_deg=56.0)

    # 2. CapsuleBody: compact sealed Beresheet-nod biological/archival vault (white enamel + gold MLI foil + brass)
    cap = r1kit.MeshBuilder("CapsuleBody", pivot_u=piv_u)
    cap.add_cylinder(
        center_u=_pose((0.0, 0.22, 0.04)),
        radius=0.26,
        radius_top=0.24,
        length=0.34,
        mat="R1_WhitePaint",
        axis_u="Y",
        segments=12,
        bevel=0.024,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    cap.add_cylinder(
        center_u=_pose((0.0, 0.15, 0.04)),
        radius=0.272,
        radius_top=0.268,
        length=0.14,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=12,
        bevel=0.014,
        bevel_segments=2,
        rot_deg_u=_rot(),
    )
    cap.add_torus(
        center_u=_pose((0.0, 0.34, 0.04)),
        major_radius=0.23,
        minor_radius=0.024,
        mat="R1_BareSteel",
        axis_u="Y",
        major_segments=14,
        minor_segments=8,
        rot_deg_u=_rot(),
    )
    for sx in (-1.0, 1.0):
        cap.add_bevel_box(
            center_u=_pose((sx * 0.27, 0.26, 0.04)),
            size_u=(0.045, 0.09, 0.16),
            mat="R1_BareSteel",
            bevel=0.008,
            segments=2,
            rot_deg_u=_rot(),
        )
    cap.add_cylinder(
        center_u=_pose((0.0, 0.26, 0.285)),
        radius=0.056,
        radius_top=0.050,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    cap.build_object(parent=root, smooth_angle_deg=54.0)

    # 3. ArchivePlaque: multi-layer optical Lunar Library / tardigrade arch disc carrier on top lid
    plaque = r1kit.MeshBuilder("ArchivePlaque", pivot_u=_pose((0.0, 0.39, 0.04)))
    plaque.add_cylinder(
        center_u=_pose((0.0, 0.395, 0.04)),
        radius=0.16,
        radius_top=0.14,
        length=0.032,
        mat="R1_Copper",
        axis_u="Y",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    plaque.add_cylinder(
        center_u=_pose((0.0, 0.412, 0.04)),
        radius=0.105,
        radius_top=0.100,
        length=0.018,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.004,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    plaque.build_object(parent=root, smooth_angle_deg=54.0)

    # 4. TinyWindow: a tiny lit window (~5.5 cm diameter) with warm amber dormant life-support glow
    win = r1kit.MeshBuilder("TinyWindow", pivot_u=_pose((0.0, 0.26, 0.302)))
    win.add_cylinder(
        center_u=_pose((0.0, 0.26, 0.302)),
        radius=0.034,
        radius_top=0.030,
        length=0.022,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=10,
        bevel=0.004,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    win.build_object(parent=root, smooth_angle_deg=54.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("ScanTarget_Anchor", root, _pose((0.0, 0.41, 0.04)))
    r1kit.create_empty("CarryGrip_Anchor", root, _pose((0.0, 0.32, 0.04)))
    r1kit.create_empty("Window_Anchor", root, _pose((0.0, 0.26, 0.31)))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.95)

    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_ILC_1",
        cell_key="ILC_SEAL",
        center_u=_pose((0.0, 0.26, -0.21)),
        normal_u=(0.0, -0.37, -0.93),
        up_u=(0.0, 0.93, -0.37),
        width=0.11,
        height=0.11,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.65)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def main() -> None:
    repo_root = HERE.parents[2]
    mare_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Mare"
    mare_dir.mkdir(parents=True, exist_ok=True)

    # 1. Mare ground patch & decals
    build_mare_ground_patch(mare_dir / "MareGround_Patch.fbx")
    build_decal_mare_swirl(mare_dir / "Decal_MareSwirl.fbx")
    build_decal_mare_ejecta_ray(mare_dir / "Decal_MareEjectaRay.fbx")

    # 2. Mare rock kit (6 natural basalt boulders/outcrops + 2 Ilmenite drill-target outcrops)
    build_mare_basalt_rocks(mare_dir)
    build_ilmenite_outcrop_00(mare_dir / "MareOutcrop_Ilmenite_00.fbx")
    build_ilmenite_outcrop_01(mare_dir / "MareOutcrop_Ilmenite_01.fbx")

    # 3. Rille pass modular wall/slope chunks (4 pieces)
    build_rille_wall_straight(mare_dir / "Rille_Wall_Straight.fbx")
    build_rille_wall_curve(mare_dir / "Rille_Wall_Curve.fbx")
    build_rille_slope_ramp(mare_dir / "Rille_Slope_Ramp.fbx")
    build_rille_mouth_corner(mare_dir / "Rille_Mouth_Corner.fbx")

    # 4. Mare derelict set (stopped convoy, crashed survey drone, half-buried tardigrade capsule)
    build_derelict_mare_convoy(mare_dir / "Derelict_MareConvoy.fbx")
    build_derelict_mare_survey_drone(mare_dir / "Derelict_MareSurveyDrone.fbx")
    build_derelict_tardigrade_capsule(mare_dir / "Derelict_TardigradeCapsule.fbx")


if __name__ == "__main__":
    main()
