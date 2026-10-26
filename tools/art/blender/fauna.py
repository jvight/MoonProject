"""Generate the four 0.6 Machine Fauna R1 models (`Assets/_Project/Art/R1/Models/Fauna/`):
1. PanelMite.fbx     — Palm-sized 6-legged solar-panel dust-wiper crawler (Lumen family, soft domed carapace).
2. Sweeper.fbx       — Slow wheeled dust sweeper with wide front rotary brush head (~1.2x 07's length, Lumen family, soft capsule/arched hull).
3. SurveyDrone.fbx   — Small hexagonal cold-gas thruster drone with 4 hull-corner De Laval nozzles (Tycho / Meridian family, no rotor booms).
4. Trencher.fbx      — Slow tracked crawler with front trenching cutter wheel, stadium tracks with rounded ends, and rear cable spool (bigger than 07, Lumen family).

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/fauna.py
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import bmesh
import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
REPO_ROOT = THIS_DIR.parents[2]

if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

FAUNA_MODELS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Fauna"
RENDERS_DIR = REPO_ROOT / "Logs" / "renders" / "fauna"


def ensure_extended_r1_materials() -> None:
    """Register R1_WhitePaint, R1_TealMark, and R1_SilverFoil into r1kit at runtime."""
    extra_specs = {
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


# ==============================================================================
# Soft-language geometry helpers (Rover 07 pill / capsule / stadium vocabulary)
# ==============================================================================


def _add_pill(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    radius: float,
    length: float,
    mat: str,
    axis_u: str = "Z",
    segments: int = 16,
    bevel_ratio: float = 0.88,
    bevel_segments: int = 4,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Add a capsule/pill volume: a cylinder whose deep multi-segment bevel rounds both ends into smooth domes."""
    max_bevel = min(radius * min(bevel_ratio, 0.78), length * 0.42)
    mb.add_cylinder(
        center_u=center_u,
        radius=radius,
        length=length,
        mat=mat,
        axis_u=axis_u,
        segments=segments,
        bevel=max_bevel,
        bevel_segments=bevel_segments,
        rot_deg_u=rot_deg_u,
    )


def _add_soft_pod(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    size_u: Tuple[float, float, float],
    mat: str,
    roundness: float = 0.40,
    segments: int = 4,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Add a soft rounded pod in 07's shape language: a box with generous multi-segment bevels eliminating slab corners."""
    bevel = min(size_u[0], size_u[1], size_u[2]) * roundness
    mb.add_bevel_box(
        center_u=center_u,
        size_u=size_u,
        mat=mat,
        bevel=bevel,
        segments=segments,
        rot_deg_u=rot_deg_u,
    )


def _add_curved_shell_arc(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    radius: float,
    thickness: float,
    span_x: float,
    angle_start_deg: float,
    angle_end_deg: float,
    mat: str,
    arc_segments: int = 12,
    bevel: float = 0.008,
) -> List[bmesh.types.BMFace]:
    """Add a closed cylindrical-arc hood/fender along the X axis (curving in the Y-Z plane around center_u).

    Angle 0 deg = +Z (forward), +90 deg = +Y (up), 180 deg = -Z (rear), -90 deg = -Y (down).
    """
    cx, cy, cz = center_u
    hx = span_x * 0.5
    r_in = radius
    r_out = radius + thickness
    a0 = math.radians(angle_start_deg)
    a1 = math.radians(angle_end_deg)

    neg_in: List[bmesh.types.BMVert] = []
    neg_out: List[bmesh.types.BMVert] = []
    pos_in: List[bmesh.types.BMVert] = []
    pos_out: List[bmesh.types.BMVert] = []

    for i in range(arc_segments + 1):
        t = i / arc_segments
        ang = a0 + (a1 - a0) * t
        sy = math.sin(ang)
        cz_dir = math.cos(ang)
        neg_in.append(mb.bm.verts.new(mb._to_local_b((cx - hx, cy + r_in * sy, cz + r_in * cz_dir))))
        neg_out.append(mb.bm.verts.new(mb._to_local_b((cx - hx, cy + r_out * sy, cz + r_out * cz_dir))))
        pos_in.append(mb.bm.verts.new(mb._to_local_b((cx + hx, cy + r_in * sy, cz + r_in * cz_dir))))
        pos_out.append(mb.bm.verts.new(mb._to_local_b((cx + hx, cy + r_out * sy, cz + r_out * cz_dir))))

    faces: List[bmesh.types.BMFace] = []
    for i in range(arc_segments):
        j = i + 1
        faces.append(mb.bm.faces.new([neg_out[i], pos_out[i], pos_out[j], neg_out[j]]))
        faces.append(mb.bm.faces.new([pos_in[i], neg_in[i], neg_in[j], pos_in[j]]))
        faces.append(mb.bm.faces.new([neg_in[i], neg_out[i], neg_out[j], neg_in[j]]))
        faces.append(mb.bm.faces.new([pos_out[i], pos_in[i], pos_in[j], pos_out[j]]))

    faces.append(mb.bm.faces.new([neg_in[0], pos_in[0], pos_out[0], neg_out[0]]))
    faces.append(mb.bm.faces.new([neg_out[-1], pos_out[-1], pos_in[-1], neg_in[-1]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)

    if bevel > 0.0:
        outer_edges = r1kit._unique(
            e
            for f in faces
            for e in f.edges
            if e.is_manifold and e.calc_face_angle(0.0) > math.radians(55.0)
        )
        if outer_edges:
            faces, _ = mb._bevel_edges(faces, outer_edges, min(bevel, thickness * 0.42), 1, mat)
    return faces


def _add_stadium_track_belt(
    mb: r1kit.MeshBuilder,
    center_x: float,
    y_bot: float,
    y_top: float,
    z_rear: float,
    z_front: float,
    width_x: float,
    belt_thickness: float,
    mat_belt: str,
    cleat_count: int = 24,
    end_segments: int = 12,
) -> None:
    """Build a true stadium/oval continuous track belt in the Y-Z plane with full semi-circular rounded ends."""
    r_outer = 0.5 * (y_top - y_bot)
    r_inner = max(0.06, r_outer - belt_thickness)
    y_mid = 0.5 * (y_bot + y_top)
    z_c_front = z_front - r_outer
    z_c_rear = z_rear + r_outer
    hx = width_x * 0.5

    # Build stadium loop stations (y_out, z_out, y_in, z_in, normal_y, normal_z)
    stations: List[Tuple[float, float, float, float, float, float]] = []

    # Front semi-circle: bottom (-90 deg) -> front (0 deg) -> top (+90 deg)
    for i in range(end_segments + 1):
        ang = -math.pi * 0.5 + math.pi * (i / end_segments)
        ny = math.sin(ang)
        nz = math.cos(ang)
        stations.append((
            y_mid + r_outer * ny,
            z_c_front + r_outer * nz,
            y_mid + r_inner * ny,
            z_c_front + r_inner * nz,
            ny,
            nz,
        ))

    # Rear semi-circle: top (+90 deg) -> rear (180 deg) -> bottom (+270 deg)
    for i in range(end_segments + 1):
        ang = math.pi * 0.5 + math.pi * (i / end_segments)
        ny = math.sin(ang)
        nz = math.cos(ang)
        stations.append((
            y_mid + r_outer * ny,
            z_c_rear + r_outer * nz,
            y_mid + r_inner * ny,
            z_c_rear + r_inner * nz,
            ny,
            nz,
        ))

    n_st = len(stations)
    neg_out: List[bmesh.types.BMVert] = []
    pos_out: List[bmesh.types.BMVert] = []
    neg_in: List[bmesh.types.BMVert] = []
    pos_in: List[bmesh.types.BMVert] = []

    for yo, zo, yi, zi, _, _ in stations:
        neg_out.append(mb.bm.verts.new(mb._to_local_b((center_x - hx, yo, zo))))
        pos_out.append(mb.bm.verts.new(mb._to_local_b((center_x + hx, yo, zo))))
        neg_in.append(mb.bm.verts.new(mb._to_local_b((center_x - hx, yi, zi))))
        pos_in.append(mb.bm.verts.new(mb._to_local_b((center_x + hx, yi, zi))))

    faces: List[bmesh.types.BMFace] = []
    for i in range(n_st):
        j = (i + 1) % n_st
        faces.append(mb.bm.faces.new([neg_out[i], pos_out[i], pos_out[j], neg_out[j]]))
        faces.append(mb.bm.faces.new([pos_in[i], neg_in[i], neg_in[j], pos_in[j]]))
        faces.append(mb.bm.faces.new([neg_in[i], neg_out[i], neg_out[j], neg_in[j]]))
        faces.append(mb.bm.faces.new([pos_out[i], pos_in[i], pos_in[j], pos_out[j]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat_belt)

    outer_edges = r1kit._unique(
        e for f in faces for e in f.edges if e.is_manifold and e.calc_face_angle(0.0) > math.radians(55.0)
    )
    if outer_edges:
        mb._bevel_edges(faces, outer_edges, min(0.018, belt_thickness * 0.35), 2, mat_belt)

    # Distribute rounded grouser cleats evenly around the entire stadium perimeter (including rounded ends)
    straight_len = max(0.01, z_c_front - z_c_rear)
    arc_len = math.pi * (r_outer - 0.015)
    total_perim = 2.0 * straight_len + 2.0 * arc_len
    cleat_r = r_outer - 0.012

    for c_idx in range(cleat_count):
        s = (c_idx / cleat_count) * total_perim
        if s < straight_len:
            # Bottom run (rear -> front)
            u = s / straight_len
            cz = z_c_rear + u * straight_len
            cy = y_mid - cleat_r
            pitch_deg = 0.0
        elif s < straight_len + arc_len:
            # Front semi-circle (-90 deg -> +90 deg)
            u = (s - straight_len) / arc_len
            ang = -math.pi * 0.5 + math.pi * u
            cz = z_c_front + cleat_r * math.cos(ang)
            cy = y_mid + cleat_r * math.sin(ang)
            pitch_deg = math.degrees(ang) - 90.0
        elif s < 2.0 * straight_len + arc_len:
            # Top run (front -> rear)
            u = (s - straight_len - arc_len) / straight_len
            cz = z_c_front - u * straight_len
            cy = y_mid + cleat_r
            pitch_deg = 180.0
        else:
            # Rear semi-circle (+90 deg -> +270 deg)
            u = (s - 2.0 * straight_len - arc_len) / arc_len
            ang = math.pi * 0.5 + math.pi * u
            cz = z_c_rear + cleat_r * math.cos(ang)
            cy = y_mid + cleat_r * math.sin(ang)
            pitch_deg = math.degrees(ang) - 90.0

        mb.add_bevel_box(
            center_u=(center_x, cy, cz),
            size_u=(width_x + 0.016, 0.032, 0.048),
            mat=mat_belt,
            bevel=0.011,
            segments=2,
            rot_deg_u=(pitch_deg, 0.0, 0.0),
        )


def _add_bristle_star_rings(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    span_x: float,
    r_inner: float,
    r_valley: float,
    r_tip: float,
    ring_thickness: float,
    num_rings: int,
    tufts_per_ring: int,
    mats: Sequence[str],
) -> None:
    """Build a series of star-notched radial bristle tuft rings along the X axis so the roller reads unmistakably as a brush."""
    _, cy, cz = center_u
    n_pts = tufts_per_ring * 2
    hx = ring_thickness * 0.5

    for r_idx in range(num_rings):
        tx = -0.5 * span_x + (span_x * r_idx / max(1, num_rings - 1))
        twist_rad = (math.pi / tufts_per_ring) * (r_idx % 2)
        mat_name = mats[r_idx % len(mats)]
        tip_r = r_tip if (r_idx % 2 == 0) else (r_tip - 0.008)

        neg_in: List[bmesh.types.BMVert] = []
        pos_in: List[bmesh.types.BMVert] = []
        neg_out: List[bmesh.types.BMVert] = []
        pos_out: List[bmesh.types.BMVert] = []

        for k in range(n_pts):
            ang = (2.0 * math.pi * k / n_pts) + twist_rad
            sy = math.sin(ang)
            cz_dir = math.cos(ang)
            r_out = tip_r if (k % 2 == 0) else r_valley
            neg_in.append(mb.bm.verts.new(mb._to_local_b((tx - hx, cy + r_inner * sy, cz + r_inner * cz_dir))))
            pos_in.append(mb.bm.verts.new(mb._to_local_b((tx + hx, cy + r_inner * sy, cz + r_inner * cz_dir))))
            neg_out.append(mb.bm.verts.new(mb._to_local_b((tx - hx, cy + r_out * sy, cz + r_out * cz_dir))))
            pos_out.append(mb.bm.verts.new(mb._to_local_b((tx + hx, cy + r_out * sy, cz + r_out * cz_dir))))

        faces: List[bmesh.types.BMFace] = []
        for k in range(n_pts):
            kn = (k + 1) % n_pts
            faces.append(mb.bm.faces.new([neg_out[k], pos_out[k], pos_out[kn], neg_out[kn]]))
            faces.append(mb.bm.faces.new([pos_in[k], neg_in[k], neg_in[kn], pos_in[kn]]))
            faces.append(mb.bm.faces.new([neg_in[k], neg_out[k], neg_out[kn], neg_in[kn]]))
            faces.append(mb.bm.faces.new([pos_out[k], pos_in[k], pos_in[kn], pos_out[kn]]))

        bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
        mb._assign_faces(faces, mat_name)


def _center_lamp_uvs_radially(root_obj: bpy.types.Object) -> None:
    """Map the `Lamp` child mesh UVs radially around (0.5, 0.5) so only the tiny center hits the shader's hot core."""
    for obj in r1kit.collect_hierarchy(root_obj):
        if obj.type != "MESH" or obj.data is None or obj.name != "Lamp":
            continue
        me = obj.data
        uv_layer = me.uv_layers.get(r1kit.UV_MAP_NAME)
        if uv_layer is None:
            continue
        verts = me.vertices
        if not verts:
            continue
        xs = [v.co.x for v in verts]
        ys = [v.co.y for v in verts]
        zs = [v.co.z for v in verts]
        cx = 0.5 * (min(xs) + max(xs))
        cy = 0.5 * (min(ys) + max(ys))
        cz = 0.5 * (min(zs) + max(zs))
        # In Blender coordinates (X=right, Y=-Z_unity, Z=Y_unity), the lamp faces forward (-Y in Blender)
        # Project onto the X-Z plane (front lens disc) so center (cx, cz) maps to UV (0.5, 0.5)
        rx_max = max(1e-4, 0.5 * (max(xs) - min(xs)))
        rz_max = max(1e-4, 0.5 * (max(zs) - min(zs)))
        loops = me.loops
        uv_data = uv_layer.data
        for poly in me.polygons:
            for loop_idx in poly.loop_indices:
                v_co = verts[loops[loop_idx].vertex_index].co
                dx = (v_co.x - cx) / rx_max
                dz = (v_co.z - cz) / rz_max
                u = 0.5 + dx * 0.44
                v = 0.5 + dz * 0.44
                uv_data[loop_idx].uv = (u, v)


def _boost_top_dust_cap(root_obj: bpy.types.Object, top_boost: float = 0.38) -> None:
    """Enhance upward-facing vertex colour B (cavity/top dust) so every fauna shell reads as dust-capped on the Moon."""
    for obj in r1kit.collect_hierarchy(root_obj):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        if "Col" not in mesh.color_attributes:
            continue
        attr = mesh.color_attributes["Col"]
        rot_mat = obj.matrix_world.to_3x3().normalized()
        for poly in mesh.polygons:
            world_n = (rot_mat @ poly.normal).normalized()
            # In Blender Z-up, world_n.z > 0.30 means upward-facing top surface
            up_factor = max(0.0, (world_n.z - 0.30) / 0.70)
            if up_factor <= 0.0:
                continue
            add_dust = top_boost * (up_factor ** 1.35)
            for loop_idx in poly.loop_indices:
                r, g, b, a = attr.data[loop_idx].color
                attr.data[loop_idx].color = (
                    r,
                    g,
                    min(1.0, max(b, b + add_dust)),
                    a,
                )


def _finalize_and_export(
    root: bpy.types.Object,
    fbx_name: str,
    footprint_radius: float,
    render_previews: bool = False,
    preview_slug: str = "",
) -> Path:
    """Apply UVs, center Lamp hot-core UVs, prune unused materials, bake dust-capped vertex colors, and export FBX."""
    r1kit.separate_coplanar_faces(r1kit.collect_hierarchy(root))
    r1kit.apply_triplanar_uvs(root)
    _center_lamp_uvs_radially(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)
    _boost_top_dust_cap(root, top_boost=0.36)

    FAUNA_MODELS_DIR.mkdir(parents=True, exist_ok=True)
    fbx_path = FAUNA_MODELS_DIR / fbx_name
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")

    if render_previews and preview_slug:
        RENDERS_DIR.mkdir(parents=True, exist_ok=True)
        r1kit.ensure_materials(use_vertex_color_shading=True)
        temp_objs = r1kit._setup_preview_lighting_and_plinth(footprint_radius)
        cam_data = bpy.data.cameras.new("_FaunaCam")
        cam_obj = bpy.data.objects.new("_FaunaCam", cam_data)
        bpy.context.collection.objects.link(cam_obj)
        bpy.context.scene.camera = cam_obj
        temp_objs.append(cam_obj)
        dist = max(0.85, footprint_radius * 2.55)
        mid_y = max(0.08, footprint_radius * 0.42)
        for v_name, c_pos in (
            ("front3q", (dist * 0.72, mid_y + dist * 0.38, dist * 0.78)),
            ("side", (dist * 1.02, mid_y + dist * 0.24, 0.08)),
            ("rear3q", (-dist * 0.72, mid_y + dist * 0.38, -dist * 0.78)),
        ):
            r1kit._aim_camera_at(cam_obj, c_pos, (0.0, mid_y, 0.0), lens_mm=48.0)
            r1kit._render_to_file(RENDERS_DIR / f"{preview_slug}_{v_name}.png", res_x=800, res_y=600, freestyle=True)
    return fbx_path


# ==============================================================================
# 1. PANEL MITE (`PanelMite.fbx`)
#    Palm-sized 6-legged solar-panel dust-wiper crawler (Lumen family).
#    Footprint: Width 0.40 m (X in [-0.20, 0.20]), Length 0.44 m (Z in [-0.22, 0.22]),
#               Height 0.21 m (Y in [0.00, 0.21]).
#    Hierarchy:
#      PanelMite (EMPTY)
#      +-- Body (MESH)
#      |   +-- Lamp (MESH)
#      |   +-- Wiper (MESH)
#      +-- Leg_L0..R2 (MESH x6)
# ==============================================================================


def build_panel_mite(render_previews: bool = False) -> Path:
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("PanelMite", pivot_u=(0.0, 0.0, 0.0))

    # --- Body ---
    mb_body = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.08, 0.0))

    # Soft rounded underbelly chassis pan
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.066, -0.005),
        size_u=(0.21, 0.056, 0.31),
        mat="R1_GreyMetal",
        roundness=0.44,
        segments=3,
    )

    # Soft beetle-like domed carapace shell (CreamEnamel) with generous multi-segment bevels
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.112, -0.006),
        size_u=(0.256, 0.098, 0.365),
        mat="R1_CreamEnamel",
        roundness=0.45,
        segments=4,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.136, -0.012),
        size_u=(0.214, 0.082, 0.305),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    # Soft capsule dorsal dome so the carapace reads like a smooth pebble/beetle
    _add_pill(
        mb_body,
        center_u=(0.0, 0.114, -0.006),
        radius=0.072,
        length=0.376,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.90,
        bevel_segments=4,
    )

    # Continuous soft rounded coral-orange beltline band
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.094, -0.005),
        size_u=(0.266, 0.028, 0.374),
        mat="R1_OrangeStripe",
        roundness=0.46,
        segments=3,
    )

    # Flush dorsal micro-solar cell patch with rounded grey bezel
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.180, -0.028),
        size_u=(0.156, 0.022, 0.195),
        mat="R1_GreyMetal",
        roundness=0.42,
        segments=3,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.186, -0.028),
        size_u=(0.132, 0.016, 0.172),
        mat="R1_SolarCell",
        roundness=0.38,
        segments=2,
    )

    # Soft rear sage maintenance cover & gold-foil thermal vent pocket + copper charge loops
    _add_pill(
        mb_body,
        center_u=(0.0, 0.146, -0.155),
        radius=0.024,
        length=0.115,
        mat="R1_SagePatch",
        axis_u="X",
        segments=12,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.108, -0.188),
        size_u=(0.105, 0.032, 0.032),
        mat="R1_GoldFoil",
        roundness=0.40,
        segments=2,
    )
    for sx in (-1.0, 1.0):
        _add_pill(
            mb_body,
            center_u=(sx * 0.055, 0.098, -0.202),
            radius=0.012,
            length=0.036,
            mat="R1_Copper",
            axis_u="Z",
            segments=10,
            bevel_ratio=0.75,
            bevel_segments=2,
        )

    # Rounded front optical visor & brass lamp bezel hood
    _add_pill(
        mb_body,
        center_u=(0.0, 0.106, 0.175),
        radius=0.024,
        length=0.132,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=12,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    _add_pill(
        mb_body,
        center_u=(0.0, 0.140, 0.168),
        radius=0.028,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.65,
        bevel_segments=2,
    )
    # Top micro telemetry stub (reaches Y = 0.21 m)
    _add_pill(
        mb_body,
        center_u=(-0.058, 0.194, -0.115),
        radius=0.008,
        length=0.032,
        mat="R1_Brass",
        axis_u="Y",
        segments=8,
        bevel_ratio=0.75,
        bevel_segments=2,
    )

    # 6 rounded leg socket boots
    leg_z_positions = (0.10, 0.0, -0.10)
    for sx in (-1.0, 1.0):
        for lz in leg_z_positions:
            _add_pill(
                mb_body,
                center_u=(sx * 0.110, 0.068, lz),
                radius=0.020,
                length=0.036,
                mat="R1_Rubber",
                axis_u="X",
                segments=10,
                bevel_ratio=0.75,
                bevel_segments=2,
            )

    body_obj = mb_body.build_object(parent=root)

    # --- Lamp (child of PanelMite, single warm R1_LampAmber emitter) ---
    mb_lamp = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 0.14, 0.18))
    _add_pill(
        mb_lamp,
        center_u=(0.0, 0.14, 0.182),
        radius=0.020,
        length=0.018,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.80,
        bevel_segments=2,
    )
    mb_lamp.build_object(parent=root)

    # --- Wiper (child of PanelMite, front electrostatic squeegee bar reaching Z = +0.22 m) ---
    mb_wiper = r1kit.MeshBuilder("Wiper", pivot_u=(0.0, 0.025, 0.18))
    for sx in (-1.0, 1.0):
        _add_pill(
            mb_wiper,
            center_u=(sx * 0.062, 0.038, 0.185),
            radius=0.010,
            length=0.056,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel_ratio=0.75,
            bevel_segments=2,
            rot_deg_u=(20.0, 0.0, 0.0),
        )
    _add_pill(
        mb_wiper,
        center_u=(0.0, 0.028, 0.205),
        radius=0.013,
        length=0.256,
        mat="R1_OrangeStripe",
        axis_u="X",
        segments=12,
        bevel_ratio=0.80,
        bevel_segments=2,
    )
    _add_soft_pod(
        mb_wiper,
        center_u=(0.0, 0.013, 0.208),
        size_u=(0.252, 0.022, 0.024),
        mat="R1_Rubber",
        roundness=0.42,
        segments=2,
    )
    mb_wiper.build_object(parent=root)

    # --- 6 Articulated Legs (Leg_L0..L2, Leg_R0..R2) ---
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        for idx, lz in enumerate(leg_z_positions):
            leg_name = f"Leg_{side_label}{idx}"
            pivot = (sx * 0.11, 0.065, lz)
            mb_leg = r1kit.MeshBuilder(leg_name, pivot_u=pivot)

            # Soft rounded coxa joint
            _add_pill(
                mb_leg,
                center_u=(sx * 0.125, 0.065, lz),
                radius=0.016,
                length=0.032,
                mat="R1_Brass",
                axis_u="Z",
                segments=10,
                bevel_ratio=0.80,
                bevel_segments=2,
            )
            # Soft capsule femur & tibia struts
            _add_pill(
                mb_leg,
                center_u=(sx * 0.155, 0.058, lz),
                radius=0.013,
                length=0.062,
                mat="R1_CreamEnamel",
                axis_u="X",
                segments=10,
                bevel_ratio=0.82,
                bevel_segments=2,
                rot_deg_u=(0.0, 0.0, -sx * 16.0),
            )
            _add_pill(
                mb_leg,
                center_u=(sx * 0.178, 0.032, lz),
                radius=0.011,
                length=0.054,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=10,
                bevel_ratio=0.78,
                bevel_segments=2,
                rot_deg_u=(0.0, 0.0, sx * 14.0),
            )
            # Rounded magnetic rubber footpad touching Y = 0.00 m, reaching X = +-0.20 m
            _add_pill(
                mb_leg,
                center_u=(sx * 0.182, 0.010, lz),
                radius=0.018,
                length=0.020,
                mat="R1_Rubber",
                axis_u="Y",
                segments=12,
                bevel_ratio=0.45,
                bevel_segments=2,
            )
            mb_leg.build_object(parent=root)

    return _finalize_and_export(
        root,
        "PanelMite.fbx",
        footprint_radius=0.26,
        render_previews=render_previews,
        preview_slug="panel_mite",
    )


# ==============================================================================
# 2. SWEEPER (`Sweeper.fbx`)
#    Slow wheeled dust sweeper with wide front rotary brush head (~1.2x 07's length).
#    Shaped in 07's soft language: capsule/arched hull, plump rounded rear dust hopper,
#    rounded sensor cab, open-front curved brush shroud, and dense visible radial bristles.
#    Footprint: Width 1.88 m (X in [-0.94, 0.94]), Length 2.42 m (Z in [-1.26, 1.16]),
#               Height 1.30 m (Y in [0.00, 1.30]).
#    Hierarchy:
#      Sweeper (EMPTY)
#      +-- Body (MESH)
#      |   +-- Lamp (MESH)
#      |   +-- BrushArm (MESH)
#      |       +-- Brush (MESH)
#      +-- Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR (MESH x4)
# ==============================================================================


def build_sweeper(render_previews: bool = False) -> Path:
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Sweeper", pivot_u=(0.0, 0.0, 0.0))

    # --- Body ---
    mb_body = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.48, -0.05))

    # 1. Soft rounded underbelly chassis tub & tubular suspension axles (R1_GreyMetal)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.34, -0.14),
        size_u=(0.94, 0.22, 1.56),
        mat="R1_GreyMetal",
        roundness=0.45,
        segments=4,
    )
    for wz in (0.36, -0.58):
        _add_pill(
            mb_body,
            center_u=(0.0, 0.26, wz),
            radius=0.058,
            length=1.18,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=12,
            bevel_ratio=0.60,
            bevel_segments=2,
        )

    # 2. Soft rounded forward body shell & plump capsule-domed rear dust hopper (R1_CreamEnamel)
    # Forward hull nose pod (smoothly rounded capsule nose tapering into the hopper)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.56, 0.14),
        size_u=(1.04, 0.42, 0.98),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    _add_pill(
        mb_body,
        center_u=(0.0, 0.54, 0.46),
        radius=0.18,
        length=0.92,
        mat="R1_CreamEnamel",
        axis_u="X",
        segments=14,
        bevel_ratio=0.88,
        bevel_segments=3,
    )

    # Plump, rounded rear vacuum/dust-collection hopper (smooth domed pod + capsule rear dome)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.64, -0.54),
        size_u=(1.12, 0.56, 1.24),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.78, -0.54),
        size_u=(0.92, 0.44, 1.08),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    # Rounded capsule tail dome reaching Z = -1.26 m (zero flat box transom)
    _add_pill(
        mb_body,
        center_u=(0.0, 0.66, -0.60),
        radius=0.32,
        length=1.32,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.90,
        bevel_segments=4,
    )

    # 3. Continuous soft rounded coral-orange beltline wrap (R1_OrangeStripe)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.49, -0.22),
        size_u=(1.15, 0.11, 1.84),
        mat="R1_OrangeStripe",
        roundness=0.46,
        segments=4,
    )
    _add_pill(
        mb_body,
        center_u=(0.0, 0.49, -0.64),
        radius=0.062,
        length=1.24,
        mat="R1_OrangeStripe",
        axis_u="Z",
        segments=12,
        bevel_ratio=0.88,
        bevel_segments=3,
    )

    # 4. Curved cylindrical wheel-arch fenders (no flat slab fenders)
    for sx in (-1.0, 1.0):
        for wz in (0.36, -0.58):
            _add_curved_shell_arc(
                mb_body,
                center_u=(sx * 0.68, 0.26, wz),
                radius=0.29,
                thickness=0.036,
                span_x=0.28,
                angle_start_deg=25.0,
                angle_end_deg=155.0,
                mat="R1_CreamEnamel",
                arc_segments=10,
                bevel=0.012,
            )

    # 5. Soft rounded sensor cab & forward pill lamp pod (07's soft head/pod language)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 1.02, 0.06),
        size_u=(0.56, 0.34, 0.54),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    _add_pill(
        mb_body,
        center_u=(0.0, 1.08, 0.06),
        radius=0.16,
        length=0.48,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.88,
        bevel_segments=4,
    )
    # Recessed dark-glass optical navigation band under cab brow
    _add_pill(
        mb_body,
        center_u=(0.0, 1.00, 0.30),
        radius=0.058,
        length=0.42,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=12,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    # Pill lamp housing on cab crown + machined brass bezel around Lamp at (0.0, 1.15, 0.28)
    _add_pill(
        mb_body,
        center_u=(0.0, 1.15, 0.14),
        radius=0.092,
        length=0.30,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    mb_body.add_cylinder(
        center_u=(0.0, 1.15, 0.268),
        radius=0.078,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.012,
        bevel_segments=2,
    )
    # Top telemetry whip & brass cap reaching Y = 1.30 m
    _add_pill(
        mb_body,
        center_u=(-0.16, 1.24, -0.04),
        radius=0.018,
        length=0.12,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel_ratio=0.80,
        bevel_segments=2,
    )

    # 6. Soft capsule dust-cyclone canisters on flanks (R1_SagePatch) + gold-foil thermal blanket & copper coils
    for sx in (-1.0, 1.0):
        _add_pill(
            mb_body,
            center_u=(sx * 0.54, 0.68, -0.52),
            radius=0.095,
            length=0.64,
            mat="R1_SagePatch",
            axis_u="Z",
            segments=14,
            bevel_ratio=0.88,
            bevel_segments=3,
        )
        _add_pill(
            mb_body,
            center_u=(sx * 0.52, 0.78, -0.52),
            radius=0.022,
            length=0.46,
            mat="R1_Copper",
            axis_u="Z",
            segments=10,
            bevel_ratio=0.75,
            bevel_segments=2,
        )

    # Flexible corrugated rubber suction hose curving from lower forward hull into the brush hood
    _add_pill(
        mb_body,
        center_u=(0.0, 0.35, 0.64),
        radius=0.075,
        length=0.34,
        mat="R1_Rubber",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.75,
        bevel_segments=3,
        rot_deg_u=(18.0, 0.0, 0.0),
    )
    # Gold-foil thermal blanket tucked into the rear hopper chute recess
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.44, -1.16),
        size_u=(0.56, 0.20, 0.14),
        mat="R1_GoldFoil",
        roundness=0.42,
        segments=3,
    )

    body_obj = mb_body.build_object(parent=root)

    # --- Lamp (child of Sweeper, single warm R1_LampAmber emitter) ---
    mb_lamp = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 1.15, 0.28))
    _add_pill(
        mb_lamp,
        center_u=(0.0, 1.15, 0.282),
        radius=0.062,
        length=0.026,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.80,
        bevel_segments=2,
    )
    mb_lamp.build_object(parent=root)

    # --- BrushArm (child of Sweeper, curved open-front cylindrical dust hood + tubular swing arms) ---
    mb_arm = r1kit.MeshBuilder("BrushArm", pivot_u=(0.0, 0.36, 0.62))

    # Left & right rounded capsule swing arms from hull hinge (0, 0.36, 0.62) to brush bearings (+-0.88, 0.24, 0.90)
    for sx in (-1.0, 1.0):
        _add_pill(
            mb_arm,
            center_u=(sx * 0.44, 0.35, 0.64),
            radius=0.044,
            length=0.38,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=12,
            bevel_ratio=0.80,
            bevel_segments=2,
        )
        _add_pill(
            mb_arm,
            center_u=(sx * 0.72, 0.31, 0.76),
            radius=0.042,
            length=0.38,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=12,
            bevel_ratio=0.82,
            bevel_segments=3,
            rot_deg_u=(22.0, sx * 26.0, 0.0),
        )
        # Soft capsule bearing end-plates at the outer ends of the brush
        _add_pill(
            mb_arm,
            center_u=(sx * 0.87, 0.25, 0.88),
            radius=0.085,
            length=0.055,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=14,
            bevel_ratio=0.75,
            bevel_segments=2,
        )
        mb_arm.add_cylinder(
            center_u=(sx * 0.90, 0.24, 0.90),
            radius=0.048,
            length=0.036,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.010,
            bevel_segments=2,
        )

    # Curved open-front cylindrical dust deflector shield wrapping ONLY the rear-upper arc (105..215 deg)
    # of the brush roller (leaving the entire front and top-front of the wide bristle brush exposed!)
    _add_curved_shell_arc(
        mb_arm,
        center_u=(0.0, 0.24, 0.90),
        radius=0.252,
        thickness=0.032,
        span_x=1.74,
        angle_start_deg=98.0,
        angle_end_deg=212.0,
        mat="R1_CreamEnamel",
        arc_segments=12,
        bevel=0.010,
    )
    # Coral-orange curved leading trim lip along the top edge of the curved brush hood
    _add_curved_shell_arc(
        mb_arm,
        center_u=(0.0, 0.24, 0.90),
        radius=0.256,
        thickness=0.036,
        span_x=1.76,
        angle_start_deg=86.0,
        angle_end_deg=108.0,
        mat="R1_OrangeStripe",
        arc_segments=6,
        bevel=0.010,
    )

    arm_obj = mb_arm.build_object(parent=root)

    # --- Brush (child of BrushArm, wide soft rotary brush with dense visible radial bristles) ---
    # Width 1.88 m (X in [-0.94, 0.94]), outer bristle diameter 0.48 m (Y in [0.00, 0.48], Z in [0.66, 1.14])
    mb_brush = r1kit.MeshBuilder("Brush", pivot_u=(0.0, 0.24, 0.90))

    # Slim central steel drive mandrel so bristles have deep radial visibility (r = 0.058 -> 0.240 m)
    mb_brush.add_cylinder(
        center_u=(0.0, 0.24, 0.90),
        radius=0.058,
        length=1.88,
        mat="R1_BareSteel",
        axis_u="X",
        segments=14,
        bevel=0.012,
        bevel_segments=2,
    )
    for sx in (-1.0, 1.0):
        mb_brush.add_cylinder(
            center_u=(sx * 0.84, 0.24, 0.90),
            radius=0.105,
            length=0.045,
            mat="R1_OrangeStripe",
            axis_u="X",
            segments=14,
            bevel=0.014,
            bevel_segments=2,
        )

    # 18 staggered star-notched radial bristle tuft rings (324 individual pointed bristle tufts!)
    # Alternating dark carbon-elastomer bristle tufts (R1_Rubber) and bronze/copper anti-static tufts (R1_Copper, R1_Brass)
    # reaching r_tip = 0.240 m (touching Y = 0.00 m ground and Z = +1.14 m forward)
    _add_bristle_star_rings(
        mb_brush,
        center_u=(0.0, 0.24, 0.90),
        span_x=1.60,
        r_inner=0.062,
        r_valley=0.115,
        r_tip=0.240,
        ring_thickness=0.028,
        num_rings=18,
        tufts_per_ring=18,
        mats=("R1_Rubber", "R1_Copper", "R1_Rubber", "R1_Brass"),
    )

    mb_brush.build_object(parent=arm_obj)

    # --- 4 Soft Balloon Wheels (Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR) ---
    wheel_specs = (
        ("Wheel_FL", (-0.70, 0.26, 0.36), -1.0),
        ("Wheel_FR", (0.70, 0.26, 0.36), 1.0),
        ("Wheel_RL", (-0.70, 0.26, -0.58), -1.0),
        ("Wheel_RR", (0.70, 0.26, -0.58), 1.0),
    )
    for w_name, (wx, wy, wz), sx in wheel_specs:
        mb_w = r1kit.MeshBuilder(w_name, pivot_u=(wx, wy, wz))
        # Soft pillowy balloon tyre (deeply bevelled cylinder reaching Y = 0.00 m)
        _add_pill(
            mb_w,
            center_u=(wx, wy, wz),
            radius=0.26,
            length=0.21,
            mat="R1_Rubber",
            axis_u="X",
            segments=20,
            bevel_ratio=0.36,
            bevel_segments=3,
        )
        # Soft domed CreamEnamel dish hubcap + coral ring + brass cap
        _add_pill(
            mb_w,
            center_u=(wx + sx * 0.02, wy, wz),
            radius=0.155,
            length=0.19,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=16,
            bevel_ratio=0.45,
            bevel_segments=3,
        )
        mb_w.add_cylinder(
            center_u=(wx + sx * 0.095, wy, wz),
            radius=0.105,
            length=0.032,
            mat="R1_OrangeStripe",
            axis_u="X",
            segments=14,
            bevel=0.010,
            bevel_segments=2,
        )
        _add_pill(
            mb_w,
            center_u=(wx + sx * 0.105, wy, wz),
            radius=0.052,
            length=0.042,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel_ratio=0.75,
            bevel_segments=2,
        )
        mb_w.build_object(parent=root)

    return _finalize_and_export(
        root,
        "Sweeper.fbx",
        footprint_radius=1.25,
        render_previews=render_previews,
        preview_slug="sweeper",
    )


# ==============================================================================
# 3. SURVEY FLOCK DRONE (`SurveyDrone.fbx`)
#    Small hexagonal cold-gas thruster drone (Tycho / Meridian family).
#    Vacuum logic: NO rotor booms or quadcopter arms; 4 De Laval cold-gas rocket
#    bell nozzles tucked directly under integrated corner gas manifolds on the
#    soft-bevelled hexagonal hull, fed by the top gold-foil spherical gas tank.
#    Footprint: Width 0.52 m (X in [-0.26, 0.26]), Length 0.52 m (Z in [-0.26, 0.26]),
#               Height 0.42 m (Y in [0.00, 0.42]).
#    Hierarchy:
#      SurveyDrone (EMPTY)
#      +-- Body (MESH)
#          +-- Lamp (MESH)
#          +-- Scanner (MESH)
#          +-- Nozzle_FL, Nozzle_FR, Nozzle_RL, Nozzle_RR (MESH x4)
# ==============================================================================


def build_survey_drone(render_previews: bool = False) -> Path:
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("SurveyDrone", pivot_u=(0.0, 0.0, 0.0))

    # --- Body ---
    mb_body = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.22, 0.0))

    # 1. Soft-bevelled hexagonal spacecraft bus (R1_WhitePaint) with smooth rounded shoulders
    mb_body.add_cylinder(
        center_u=(0.0, 0.215, 0.0),
        radius=0.238,
        length=0.185,
        mat="R1_WhitePaint",
        axis_u="Y",
        segments=6,
        bevel=0.062,
        bevel_segments=4,
    )
    mb_body.add_cylinder(
        center_u=(0.0, 0.132, 0.0),
        radius=0.185,
        length=0.036,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=6,
        bevel=0.014,
        bevel_segments=2,
    )

    # 2. Continuous Tycho teal stripe ring around the hexagonal equator (R1_TealMark)
    mb_body.add_cylinder(
        center_u=(0.0, 0.215, 0.0),
        radius=0.242,
        length=0.036,
        mat="R1_TealMark",
        axis_u="Y",
        segments=6,
        bevel=0.012,
        bevel_segments=2,
    )

    # 3. Prominent spherical/domed GoldFoil cold-gas propellant tank on top (Y up to 0.42 m)
    _add_pill(
        mb_body,
        center_u=(0.0, 0.335, 0.0),
        radius=0.132,
        length=0.145,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=18,
        bevel_ratio=0.90,
        bevel_segments=4,
    )
    # Brass pressure regulator cap at tank apex (reaches Y = 0.42 m)
    _add_pill(
        mb_body,
        center_u=(0.0, 0.406, 0.0),
        radius=0.038,
        length=0.028,
        mat="R1_Brass",
        axis_u="Y",
        segments=14,
        bevel_ratio=0.75,
        bevel_segments=2,
    )
    # Copper high-pressure cold-gas manifold ring encircling the base of the top GoldFoil sphere
    mb_body.add_torus(
        center_u=(0.0, 0.285, 0.0),
        major_radius=0.142,
        minor_radius=0.016,
        mat="R1_Copper",
        axis_u="Y",
        major_segments=18,
        minor_segments=8,
    )

    # 4. Compact hull-corner cold-gas manifold pods (tucked directly against the hex body at +-0.185, 0.185, +-0.185;
    #    NO quadcopter arms or rotor booms!) + vertical copper feed risers from the manifold ring
    nozzle_corners = (
        ("Nozzle_FL", -1.0, 1.0),
        ("Nozzle_FR", 1.0, 1.0),
        ("Nozzle_RL", -1.0, -1.0),
        ("Nozzle_RR", 1.0, -1.0),
    )
    for _, sx, sz in nozzle_corners:
        nx = sx * 0.185
        nz = sz * 0.185
        yaw_deg = math.degrees(math.atan2(nx, nz))
        # Compact rounded shoulder fairing blended directly into the hull corner
        _add_soft_pod(
            mb_body,
            center_u=(sx * 0.165, 0.205, sz * 0.165),
            size_u=(0.095, 0.078, 0.115),
            mat="R1_WhitePaint",
            roundness=0.45,
            segments=3,
            rot_deg_u=(0.0, yaw_deg, 0.0),
        )
        # Vertical Copper high-pressure cold-gas feed riser into corner pod
        _add_pill(
            mb_body,
            center_u=(sx * 0.155, 0.245, sz * 0.155),
            radius=0.015,
            length=0.075,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
            bevel_ratio=0.75,
            bevel_segments=2,
        )
        # SilverFoil thermal boot around nozzle gimbal socket
        mb_body.add_cylinder(
            center_u=(nx, 0.168, nz),
            radius=0.036,
            length=0.024,
            mat="R1_SilverFoil",
            axis_u="Y",
            segments=12,
            bevel=0.008,
            bevel_segments=2,
        )

    # 5. Front brass lamp hood & rear SilverFoil radiator louvre
    _add_pill(
        mb_body,
        center_u=(0.0, 0.215, 0.232),
        radius=0.042,
        length=0.032,
        mat="R1_Brass",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.70,
        bevel_segments=2,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.215, -0.228),
        size_u=(0.115, 0.056, 0.028),
        mat="R1_SilverFoil",
        roundness=0.42,
        segments=2,
    )

    # 6. Twin curved tubular landing skids under the hull reaching Y = 0.00 m
    for sx in (-1.0, 1.0):
        for sz in (-0.09, 0.09):
            _add_pill(
                mb_body,
                center_u=(sx * 0.145, 0.075, sz),
                radius=0.012,
                length=0.145,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=8,
                bevel_ratio=0.70,
                bevel_segments=2,
                rot_deg_u=(0.0, 0.0, -sx * 24.0),
            )
        _add_pill(
            mb_body,
            center_u=(sx * 0.180, 0.012, 0.0),
            radius=0.012,
            length=0.30,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=10,
            bevel_ratio=0.80,
            bevel_segments=2,
        )
        for sz_end in (-0.13, 0.13):
            _add_pill(
                mb_body,
                center_u=(sx * 0.180, 0.012, sz_end),
                radius=0.014,
                length=0.034,
                mat="R1_Rubber",
                axis_u="Z",
                segments=10,
                bevel_ratio=0.75,
                bevel_segments=2,
            )

    body_obj = mb_body.build_object(parent=root)

    # --- Lamp (child of Body, front cyan status eye; R1_LampAmber in FBX -> overridden to R1_FaunaLampCyan in Unity) ---
    mb_lamp = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 0.215, 0.245))
    _add_pill(
        mb_lamp,
        center_u=(0.0, 0.215, 0.246),
        radius=0.030,
        length=0.020,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=14,
        bevel_ratio=0.80,
        bevel_segments=2,
    )
    mb_lamp.build_object(parent=body_obj)

    # --- Scanner (child of Body, underbelly rotating optical/laser survey turret) ---
    mb_scan = r1kit.MeshBuilder("Scanner", pivot_u=(0.0, 0.09, 0.0))
    _add_pill(
        mb_scan,
        center_u=(0.0, 0.095, 0.0),
        radius=0.072,
        length=0.068,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=14,
        bevel_ratio=0.45,
        bevel_segments=2,
    )
    _add_pill(
        mb_scan,
        center_u=(0.0, 0.076, 0.056),
        radius=0.034,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel_ratio=0.65,
        bevel_segments=2,
        rot_deg_u=(24.0, 0.0, 0.0),
    )
    _add_pill(
        mb_scan,
        center_u=(0.0, 0.070, 0.068),
        radius=0.026,
        length=0.020,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=12,
        bevel_ratio=0.75,
        bevel_segments=2,
        rot_deg_u=(24.0, 0.0, 0.0),
    )
    mb_scan.build_object(parent=body_obj)

    # --- 4 De Laval Cold-Gas Thruster Bell Nozzles (Nozzle_FL, Nozzle_FR, Nozzle_RL, Nozzle_RR) ---
    # Tucked directly under the 4 hull corner manifolds at (+-0.185, 0.16, +-0.185) so the silhouette
    # reads as a vacuum spacecraft hopper on rocket thrusters, never a quadcopter with rotor arms.
    for n_name, sx, sz in nozzle_corners:
        nx = sx * 0.185
        nz = sz * 0.185
        mb_noz = r1kit.MeshBuilder(n_name, pivot_u=(nx, 0.16, nz))

        # Brass spherical gimbal ball joint at pivot
        _add_pill(
            mb_noz,
            center_u=(nx, 0.158, nz),
            radius=0.026,
            length=0.032,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel_ratio=0.85,
            bevel_segments=2,
        )
        # High-pressure valve throat
        mb_noz.add_cylinder(
            center_u=(nx, 0.132, nz),
            radius=0.020,
            length=0.030,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=12,
            bevel=0.005,
            bevel_segments=1,
        )
        # Prominent flared De Laval rocket expansion bell (BareSteel with Copper regenerative cooling band)
        mb_noz.add_cylinder(
            center_u=(nx, 0.082, nz),
            radius=0.058,
            radius_top=0.019,
            length=0.082,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=14,
            bevel=0.006,
            bevel_segments=2,
        )
        mb_noz.add_cylinder(
            center_u=(nx, 0.095, nz),
            radius=0.044,
            length=0.018,
            mat="R1_Copper",
            axis_u="Y",
            segments=14,
            bevel=0.005,
            bevel_segments=1,
        )
        mb_noz.add_cylinder(
            center_u=(nx, 0.044, nz),
            radius=0.060,
            length=0.012,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.004,
            bevel_segments=1,
        )
        mb_noz.build_object(parent=body_obj)

    return _finalize_and_export(
        root,
        "SurveyDrone.fbx",
        footprint_radius=0.32,
        render_previews=render_previews,
        preview_slug="survey_drone",
    )


# ==============================================================================
# 4. TRENCHER (`Trencher.fbx`)
#    Slow tracked crawler with front trenching cutter wheel and rear cable spool (bigger than 07).
#    Shaped in 07's soft language: rounded capsule/arched engineering hull, soft domed offset cab,
#    stadium tracks with full semi-circular rounded ends, exposed toothed trenching wheel,
#    and prominent spoked rear cable spool drum with wound rubber/copper cable coils.
#    Footprint: Width 1.96 m (X in [-0.98, 0.98]), Length 3.27 m (Z in [-1.65, 1.62]),
#               Height 1.84 m (Y in [0.00, 1.84]).
#    Hierarchy:
#      Trencher (EMPTY)
#      +-- Body (MESH)
#      |   +-- Lamp (MESH)
#      |   +-- TrenchBoom (MESH)
#      |   |   +-- TrenchWheel (MESH)
#      |   +-- CableSpool (MESH)
#      +-- Track_L, Track_R (MESH x2)
#      +-- Roller_FL, Roller_FR, Roller_RL, Roller_RR (MESH x4)
# ==============================================================================


def build_trencher(render_previews: bool = False) -> Path:
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Trencher", pivot_u=(0.0, 0.0, 0.0))

    # --- Body ---
    mb_body = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.62, 0.0))

    # 1. Soft rounded underbelly chassis cradle & heavy tubular cross-axles (R1_GreyMetal)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.44, -0.06),
        size_u=(0.94, 0.30, 1.56),
        mat="R1_GreyMetal",
        roundness=0.45,
        segments=4,
    )
    for rz in (0.58, -0.66):
        _add_pill(
            mb_body,
            center_u=(0.0, 0.30, rz),
            radius=0.072,
            length=1.22,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=12,
            bevel_ratio=0.65,
            bevel_segments=2,
        )

    # 2. Soft capsule/domed main hull shell (R1_CreamEnamel) — zero stacked rectangular slabs!
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.80, -0.06),
        size_u=(1.08, 0.62, 1.58),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.96, -0.06),
        size_u=(0.88, 0.44, 1.38),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    # Rounded capsule nose and tail domes on the main body
    _add_pill(
        mb_body,
        center_u=(0.0, 0.82, -0.06),
        radius=0.32,
        length=1.66,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.90,
        bevel_segments=4,
    )

    # 3. Continuous soft rounded coral-orange beltline wrap (R1_OrangeStripe)
    _add_soft_pod(
        mb_body,
        center_u=(0.0, 0.68, -0.06),
        size_u=(1.12, 0.12, 1.62),
        mat="R1_OrangeStripe",
        roundness=0.46,
        segments=4,
    )

    # 4. Soft rounded/domed offset operator/guidance cab on forward-left deck (X = -0.30, Z = +0.24)
    _add_soft_pod(
        mb_body,
        center_u=(-0.30, 1.34, 0.24),
        size_u=(0.64, 0.58, 0.72),
        mat="R1_CreamEnamel",
        roundness=0.46,
        segments=4,
    )
    _add_pill(
        mb_body,
        center_u=(-0.30, 1.44, 0.24),
        radius=0.22,
        length=0.64,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.90,
        bevel_segments=4,
    )
    # Coral collar band around the rounded cab base
    _add_soft_pod(
        mb_body,
        center_u=(-0.30, 1.15, 0.24),
        size_u=(0.58, 0.09, 0.62),
        mat="R1_OrangeStripe",
        roundness=0.48,
        segments=4,
    )
    # Curved dark-glass front & side optical windows on the rounded cab
    _add_pill(
        mb_body,
        center_u=(-0.30, 1.36, 0.56),
        radius=0.095,
        length=0.44,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=14,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    _add_pill(
        mb_body,
        center_u=(-0.58, 1.36, 0.24),
        radius=0.085,
        length=0.38,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=12,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    # Forward pill headlamp pod on the cab brow (housing Lamp at (-0.34, 1.54, 0.66))
    _add_pill(
        mb_body,
        center_u=(-0.34, 1.54, 0.50),
        radius=0.098,
        length=0.32,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.85,
        bevel_segments=3,
    )
    mb_body.add_cylinder(
        center_u=(-0.34, 1.54, 0.645),
        radius=0.086,
        length=0.038,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.012,
        bevel_segments=2,
    )
    # Top survey antenna mast reaching Y = 1.84 m
    _add_pill(
        mb_body,
        center_u=(-0.38, 1.73, 0.14),
        radius=0.022,
        length=0.22,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel_ratio=0.80,
        bevel_segments=2,
    )

    # 5. Soft starboard auxiliary power pack (SagePatch capsule + GoldFoil thermal blanket bay + Copper heat pipes)
    _add_pill(
        mb_body,
        center_u=(0.30, 1.24, 0.08),
        radius=0.20,
        length=0.84,
        mat="R1_SagePatch",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.88,
        bevel_segments=4,
    )
    _add_soft_pod(
        mb_body,
        center_u=(0.30, 1.18, -0.44),
        size_u=(0.36, 0.24, 0.36),
        mat="R1_GoldFoil",
        roundness=0.44,
        segments=3,
    )
    for cz in (-0.18, 0.18):
        _add_pill(
            mb_body,
            center_u=(0.48, 1.16, cz),
            radius=0.028,
            length=0.28,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
            bevel_ratio=0.75,
            bevel_segments=2,
        )

    # 6. Curved tubular A-frame rear spool gantry (supporting CableSpool at (0.0, 0.96, -1.04) while leaving it open)
    for sx in (-1.0, 1.0):
        _add_pill(
            mb_body,
            center_u=(sx * 0.44, 0.88, -0.82),
            radius=0.056,
            length=0.64,
            mat="R1_CreamEnamel",
            axis_u="Z",
            segments=12,
            bevel_ratio=0.82,
            bevel_segments=3,
            rot_deg_u=(-22.0, 0.0, 0.0),
        )
        _add_pill(
            mb_body,
            center_u=(sx * 0.44, 0.78, -0.84),
            radius=0.046,
            length=0.46,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=12,
            bevel_ratio=0.80,
            bevel_segments=2,
            rot_deg_u=(-28.0, 0.0, 0.0),
        )
        mb_body.add_cylinder(
            center_u=(sx * 0.44, 0.96, -1.04),
            radius=0.095,
            length=0.068,
            mat="R1_Brass",
            axis_u="X",
            segments=14,
            bevel=0.016,
            bevel_segments=2,
        )

    body_obj = mb_body.build_object(parent=root)

    # --- Lamp (child of Trencher, single warm R1_LampAmber emitter on cab brow) ---
    mb_lamp = r1kit.MeshBuilder("Lamp", pivot_u=(-0.34, 1.54, 0.66))
    _add_pill(
        mb_lamp,
        center_u=(-0.34, 1.54, 0.662),
        radius=0.068,
        length=0.028,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel_ratio=0.80,
        bevel_segments=2,
    )
    mb_lamp.build_object(parent=root)

    # --- TrenchBoom (child of Trencher, sleek tubular hydraulic boom arms + small curved crescent spoil shield) ---
    mb_boom = r1kit.MeshBuilder("TrenchBoom", pivot_u=(0.0, 0.68, 0.62))

    for sx in (-1.0, 1.0):
        # Sleek capsule boom arms sloping down from (sx*0.24, 0.68, 0.62) to wheel hub (sx*0.22, 0.56, 1.08)
        _add_pill(
            mb_boom,
            center_u=(sx * 0.23, 0.62, 0.85),
            radius=0.056,
            length=0.60,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=12,
            bevel_ratio=0.82,
            bevel_segments=3,
            rot_deg_u=(14.0, 0.0, 0.0),
        )
        # Hydraulic lift cylinders above boom arms
        _add_pill(
            mb_boom,
            center_u=(sx * 0.21, 0.82, 0.78),
            radius=0.036,
            length=0.38,
            mat="R1_OrangeStripe",
            axis_u="Z",
            segments=10,
            bevel_ratio=0.75,
            bevel_segments=2,
            rot_deg_u=(20.0, 0.0, 0.0),
        )
        _add_pill(
            mb_boom,
            center_u=(sx * 0.21, 0.74, 0.94),
            radius=0.022,
            length=0.30,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=10,
            bevel_ratio=0.70,
            bevel_segments=2,
            rot_deg_u=(20.0, 0.0, 0.0),
        )
        # Rounded planetary axle hub plates at (sx*0.22, 0.56, 1.08)
        _add_pill(
            mb_boom,
            center_u=(sx * 0.22, 0.56, 1.08),
            radius=0.115,
            length=0.056,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=14,
            bevel_ratio=0.75,
            bevel_segments=2,
        )

    # Compact curved crescent spoil fender wrapping ONLY the rear-upper 65 deg (110..175 deg) behind the wheel
    # so the giant toothed TrenchWheel is wide open across its top, front, and bottom!
    _add_curved_shell_arc(
        mb_boom,
        center_u=(0.0, 0.56, 1.08),
        radius=0.555,
        thickness=0.038,
        span_x=0.38,
        angle_start_deg=108.0,
        angle_end_deg=174.0,
        mat="R1_CreamEnamel",
        arc_segments=10,
        bevel=0.012,
    )
    _add_curved_shell_arc(
        mb_boom,
        center_u=(0.0, 0.56, 1.08),
        radius=0.560,
        thickness=0.042,
        span_x=0.40,
        angle_start_deg=96.0,
        angle_end_deg=114.0,
        mat="R1_OrangeStripe",
        arc_segments=5,
        bevel=0.010,
    )

    boom_obj = mb_boom.build_object(parent=root)

    # --- TrenchWheel (child of TrenchBoom, large 1.08 m diameter spoked/bucketed rock-cutting trencher wheel) ---
    # Reaches Z = +1.62 m (1.08 + 0.54), Y in [0.02, 1.10]
    mb_wheel = r1kit.MeshBuilder("TrenchWheel", pivot_u=(0.0, 0.56, 1.08))

    # Outer cutter wheel drum ring (GreyMetal) + inner Coral-Orange structural web + Brass planetary hub
    mb_wheel.add_torus(
        center_u=(0.0, 0.56, 1.08),
        major_radius=0.39,
        minor_radius=0.075,
        mat="R1_GreyMetal",
        axis_u="X",
        major_segments=24,
        minor_segments=12,
    )
    mb_wheel.add_cylinder(
        center_u=(0.0, 0.56, 1.08),
        radius=0.33,
        length=0.11,
        mat="R1_OrangeStripe",
        axis_u="X",
        segments=20,
        bevel=0.022,
        bevel_segments=2,
    )
    # 6 bold radial spokes across the wheel web
    for sp_idx in range(6):
        sp_ang = sp_idx * 60.0
        mb_wheel.add_bevel_box(
            center_u=(0.0, 0.56, 1.08),
            size_u=(0.14, 0.068, 0.72),
            mat="R1_GreyMetal",
            bevel=0.018,
            segments=2,
            rot_deg_u=(sp_ang, 0.0, 0.0),
        )
    _add_pill(
        mb_wheel,
        center_u=(0.0, 0.56, 1.08),
        radius=0.145,
        length=0.28,
        mat="R1_Brass",
        axis_u="X",
        segments=16,
        bevel_ratio=0.65,
        bevel_segments=2,
    )

    # 14 prominent excavator buckets & angled BareSteel rock-cutting teeth around the wheel perimeter (r up to 0.54 m)
    num_teeth = 14
    for t_idx in range(num_teeth):
        ang_deg = t_idx * (360.0 / num_teeth)
        ang_rad = math.radians(ang_deg)
        sy = math.sin(ang_rad)
        cz_dir = math.cos(ang_rad)

        # Rounded excavator scoop bucket (r = 0.44 m)
        by = 0.56 + 0.445 * sy
        bz = 1.08 + 0.445 * cz_dir
        _add_soft_pod(
            mb_wheel,
            center_u=(0.0, by, bz),
            size_u=(0.24, 0.088, 0.105),
            mat="R1_GreyMetal",
            roundness=0.38,
            segments=2,
            rot_deg_u=(-ang_deg + 18.0, 0.0, 0.0),
        )
        # Dual splayed BareSteel carbide rock-cutter teeth projecting to r = 0.54 m (Z = +1.62 m)
        ty = 0.56 + 0.505 * sy
        tz = 1.08 + 0.505 * cz_dir
        for sx in (-1.0, 1.0):
            mb_wheel.add_bevel_box(
                center_u=(sx * 0.072, ty, tz),
                size_u=(0.044, 0.046, 0.095),
                mat="R1_BareSteel",
                bevel=0.010,
                segments=2,
                rot_deg_u=(-ang_deg + 24.0, sx * 10.0, 0.0),
            )

    mb_wheel.build_object(parent=boom_obj)

    # --- CableSpool (child of Body, large 1.22 m diameter spoked rear cable drum with wound cable coils) ---
    # Centered at (0.0, 0.96, -1.04), reaching Z = -1.65 m (-1.04 - 0.61)
    mb_spool = r1kit.MeshBuilder("CableSpool", pivot_u=(0.0, 0.96, -1.04))

    # Central steel axle & drum core
    mb_spool.add_cylinder(
        center_u=(0.0, 0.96, -1.04),
        radius=0.22,
        length=0.72,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=16,
        bevel=0.018,
        bevel_segments=2,
    )

    # Large spoked circular reel flanges (radius = 0.61 m -> Z reaches -1.65 m, Y reaches 1.57 m)
    for sx in (-1.0, 1.0):
        fx = sx * 0.36
        # Outer rim torus (CreamEnamel) + Coral-Orange rim band + 6 radial flange ribs
        mb_spool.add_torus(
            center_u=(fx, 0.96, -1.04),
            major_radius=0.555,
            minor_radius=0.055,
            mat="R1_CreamEnamel",
            axis_u="X",
            major_segments=24,
            minor_segments=10,
        )
        mb_spool.add_cylinder(
            center_u=(fx, 0.96, -1.04),
            radius=0.52,
            length=0.048,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=24,
            bevel=0.016,
            bevel_segments=2,
        )
        mb_spool.add_cylinder(
            center_u=(fx + sx * 0.018, 0.96, -1.04),
            radius=0.45,
            length=0.032,
            mat="R1_OrangeStripe",
            axis_u="X",
            segments=24,
            bevel=0.010,
            bevel_segments=2,
        )
        for r_idx in range(6):
            mb_spool.add_bevel_box(
                center_u=(fx + sx * 0.024, 0.96, -1.04),
                size_u=(0.036, 0.068, 1.06),
                mat="R1_CreamEnamel",
                bevel=0.012,
                segments=2,
                rot_deg_u=(r_idx * 30.0, 0.0, 0.0),
            )
        _add_pill(
            mb_spool,
            center_u=(fx + sx * 0.038, 0.96, -1.04),
            radius=0.135,
            length=0.055,
            mat="R1_Brass",
            axis_u="X",
            segments=14,
            bevel_ratio=0.70,
            bevel_segments=2,
        )

    # 9 thick, distinct toroidal coils of wound trunk cable (R1_Rubber with R1_Copper splice collars)
    num_coils = 9
    for c_idx in range(num_coils):
        cx = -0.29 + (0.58 * c_idx / (num_coils - 1))
        coil_mat = "R1_Copper" if c_idx in (2, 6) else "R1_Rubber"
        mb_spool.add_torus(
            center_u=(cx, 0.96, -1.04),
            major_radius=0.39,
            minor_radius=0.044,
            mat=coil_mat,
            axis_u="X",
            major_segments=20,
            minor_segments=10,
        )

    mb_spool.build_object(parent=root)

    # --- Stadium Tracks with Full Semi-Circular Rounded Ends (Track_L, Track_R) ---
    # Outer X reaches +-0.98 m (width 1.96 m), Y in [0.00, 0.64], Z in [-0.98, +0.90]
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        tx = sx * 0.72
        mb_trk = r1kit.MeshBuilder(f"Track_{side_label}", pivot_u=(tx, 0.34, -0.04))

        # True stadium/oval track belt with full semi-circular rounded ends and 24 perimeter grouser cleats
        _add_stadium_track_belt(
            mb_trk,
            center_x=tx,
            y_bot=0.00,
            y_top=0.64,
            z_rear=-0.98,
            z_front=0.90,
            width_x=0.50,
            belt_thickness=0.085,
            mat_belt="R1_Rubber",
            cleat_count=24,
            end_segments=12,
        )

        # Soft pill-shaped bogie beam inside the stadium track loop (zero rectangular slab projections!)
        _add_pill(
            mb_trk,
            center_u=(tx, 0.32, -0.04),
            radius=0.165,
            length=1.46,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=16,
            bevel_ratio=0.90,
            bevel_segments=4,
        )
        # Soft capsule outer fairing plate (CreamEnamel + Coral-Orange pill stripe)
        _add_pill(
            mb_trk,
            center_u=(tx + sx * 0.14, 0.32, -0.04),
            radius=0.125,
            length=1.22,
            mat="R1_CreamEnamel",
            axis_u="Z",
            segments=14,
            bevel_ratio=0.90,
            bevel_segments=4,
        )
        _add_pill(
            mb_trk,
            center_u=(tx + sx * 0.21, 0.32, -0.04),
            radius=0.052,
            length=0.96,
            mat="R1_OrangeStripe",
            axis_u="Z",
            segments=12,
            bevel_ratio=0.88,
            bevel_segments=3,
        )

        mb_trk.build_object(parent=root)

    # --- 4 Track Drive Sprockets / Idler Rollers (Roller_FL, Roller_FR, Roller_RL, Roller_RR) ---
    roller_specs = (
        ("Roller_FL", (-0.72, 0.30, 0.58), -1.0),
        ("Roller_FR", (0.72, 0.30, 0.58), 1.0),
        ("Roller_RL", (-0.72, 0.30, -0.66), -1.0),
        ("Roller_RR", (0.72, 0.30, -0.66), 1.0),
    )
    for r_name, (rx, ry, rz), sx in roller_specs:
        mb_rol = r1kit.MeshBuilder(r_name, pivot_u=(rx, ry, rz))
        _add_pill(
            mb_rol,
            center_u=(rx, ry, rz),
            radius=0.22,
            length=0.44,
            mat="R1_BareSteel",
            axis_u="X",
            segments=16,
            bevel_ratio=0.35,
            bevel_segments=2,
        )
        _add_pill(
            mb_rol,
            center_u=(rx + sx * 0.22, ry, rz),
            radius=0.14,
            length=0.055,
            mat="R1_Brass",
            axis_u="X",
            segments=14,
            bevel_ratio=0.70,
            bevel_segments=2,
        )
        for sp_i in range(3):
            mb_rol.add_bevel_box(
                center_u=(rx + sx * 0.245, ry, rz),
                size_u=(0.028, 0.045, 0.30),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
                rot_deg_u=(sp_i * 60.0, 0.0, 0.0),
            )
        mb_rol.build_object(parent=root)

    return _finalize_and_export(
        root,
        "Trencher.fbx",
        footprint_radius=1.65,
        render_previews=render_previews,
        preview_slug="trencher",
    )


def build_all_fauna(render_previews: bool = False) -> List[Path]:
    return [
        build_panel_mite(render_previews=render_previews),
        build_sweeper(render_previews=render_previews),
        build_survey_drone(render_previews=render_previews),
        build_trencher(render_previews=render_previews),
    ]


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Build 0.6 Machine Fauna R1 FBX models.")
    parser.add_argument(
        "--render-previews",
        action="store_true",
        help="Render turntable preview PNGs to Logs/renders/fauna/.",
    )
    args = parser.parse_args(argv)
    build_all_fauna(render_previews=args.render_previews)
    return 0


if __name__ == "__main__":
    user_argv: List[str] = []
    if "--" in sys.argv:
        user_argv = sys.argv[sys.argv.index("--") + 1 :]
    raise SystemExit(main(user_argv))
