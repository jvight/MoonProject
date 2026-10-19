"""Build and export the 6 Kestrel-3 wreck debris pieces (0.8 m to 4 m) for R1.

Deliverable 2 (Logs/TASK.md):
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_HullPanel.fbx  (cut seam #1: bolted plate band + scorch marks)
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_StrutFrame.fbx (cut seam #2: bolted plate band + scorch marks)
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_Tank.fbx
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_EngineBell.fbx
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_CableTray.fbx
  - Assets/_Project/Art/R1/Models/Wreck/Kestrel_LandingFoot.fbx

Every piece has:
  - Half-buried base skirt with a flat underside below Z=0 (Unity Y<0) so lunar terrain swallows it cleanly
  - Child `CutPoint` empty above ground (Z>0) for cutter/salvage gameplay
  - R1 material slots and baked vertex colours (`Col`: R=AO, G=wear, B=dust)
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
RUINS_DIR = THIS_DIR / "ruins"
if str(RUINS_DIR) not in sys.path:
    sys.path.insert(0, str(RUINS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402
import ruins_common as rc  # noqa: E402


def _add_oriented_box(
    bm: bmesh.types.BMesh,
    size: tuple[float, float, float],
    center: tuple[float, float, float],
    rot_deg: tuple[float, float, float],
    mat_idx: int,
    bevel: float = 0.005,
) -> None:
    """Create a closed bevelled box rotated by Euler XYZ degrees and translated to center."""
    sx, sy, sz = size
    geom = bmesh.ops.create_cube(bm, size=1.0)
    verts = geom["verts"]
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=verts)

    if bevel > 0.0:
        vset = set(verts)
        edges = [e for e in bm.edges if e.verts[0] in vset and e.verts[1] in vset]
        res = bmesh.ops.bevel(
            bm,
            geom=edges,
            offset=min(bevel, min(sx, sy, sz) * 0.22),
            offset_type="OFFSET",
            segments=1,
            profile=0.5,
            affect="EDGES",
        )
        all_verts = [v for v in res.get("verts", []) if v.is_valid]
    else:
        all_verts = verts

    rot_mat = Euler(
        (math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])),
        "XYZ",
    ).to_matrix()
    bmesh.ops.rotate(bm, matrix=rot_mat, verts=all_verts, cent=(0.0, 0.0, 0.0))
    bmesh.ops.translate(bm, vec=center, verts=all_verts)

    vset = set(all_verts)
    for f in bm.faces:
        if all(v in vset for v in f.verts):
            f.material_index = mat_idx
            f.smooth = False


def _add_oriented_frustum(
    bm: bmesh.types.BMesh,
    r_bottom: float,
    r_top: float,
    length: float,
    segments: int,
    center: tuple[float, float, float],
    rot_deg: tuple[float, float, float],
    mat_idx: int,
    cap_mat_idx: int | None = None,
) -> None:
    """Create a closed manifold cylinder/cone frustum oriented by rot_deg and placed at center."""
    half_l = length * 0.5
    ring_b = []
    ring_t = []
    for i in range(segments):
        ang = 2.0 * math.pi * i / segments
        ca, sa = math.cos(ang), math.sin(ang)
        ring_b.append(bm.verts.new((r_bottom * ca, r_bottom * sa, -half_l)))
        ring_t.append(bm.verts.new((r_top * ca, r_top * sa, half_l)))

    v_bot = bm.verts.new((0.0, 0.0, -half_l))
    v_top = bm.verts.new((0.0, 0.0, half_l))
    all_verts = ring_b + ring_t + [v_bot, v_top]

    c_idx = mat_idx if cap_mat_idx is None else cap_mat_idx
    for i in range(segments):
        inext = (i + 1) % segments
        f_side = bm.faces.new((ring_b[i], ring_b[inext], ring_t[inext], ring_t[i]))
        f_side.material_index = mat_idx
        f_side.smooth = segments >= 12

        f_b = bm.faces.new((v_bot, ring_b[inext], ring_b[i]))
        f_b.material_index = c_idx
        f_b.smooth = False

        f_t = bm.faces.new((v_top, ring_t[i], ring_t[inext]))
        f_t.material_index = c_idx
        f_t.smooth = False

    rot_mat = Euler(
        (math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])),
        "XYZ",
    ).to_matrix()
    bmesh.ops.rotate(bm, matrix=rot_mat, verts=all_verts, cent=(0.0, 0.0, 0.0))
    bmesh.ops.translate(bm, vec=center, verts=all_verts)


def _add_buried_skirt_plinth(
    bm: bmesh.types.BMesh,
    top_w: float,
    top_d: float,
    bot_w: float,
    bot_d: float,
    z_bot: float,
    z_top: float,
    mat_idx: int,
    cx: float = 0.0,
    cy: float = 0.0,
) -> None:
    """Create an 8-sided flared burial skirt with a flat underside below Z=0 so terrain swallows it."""
    oct_angles = [i * (math.pi / 4.0) + (math.pi / 8.0) for i in range(8)]
    cos_corr = 1.0 / math.cos(math.pi / 8.0)
    ring_b = []
    ring_t = []
    for a in oct_angles:
        ca, sa = math.cos(a) * cos_corr, math.sin(a) * cos_corr
        ring_b.append(bm.verts.new((cx + (bot_w * 0.5) * ca, cy + (bot_d * 0.5) * sa, z_bot)))
        ring_t.append(bm.verts.new((cx + (top_w * 0.5) * ca, cy + (top_d * 0.5) * sa, z_top)))

    v_bot = bm.verts.new((cx, cy, z_bot))
    v_top = bm.verts.new((cx, cy, z_top))
    for i in range(8):
        inext = (i + 1) % 8
        f_s = bm.faces.new((ring_b[i], ring_b[inext], ring_t[inext], ring_t[i]))
        f_s.material_index = mat_idx
        f_s.smooth = False
        f_b = bm.faces.new((v_bot, ring_b[inext], ring_b[i]))
        f_b.material_index = mat_idx
        f_b.smooth = False
        f_t = bm.faces.new((v_top, ring_t[i], ring_t[inext]))
        f_t.material_index = mat_idx
        f_t.smooth = False


def _add_bolted_cut_seam_band(
    bm: bmesh.types.BMesh,
    center: tuple[float, float, float],
    rot_deg: tuple[float, float, float],
    span_w: float,
    band_thick: float,
    band_depth: float,
    mat_map: dict[str, int],
    bolt_count: int = 6,
) -> None:
    """Build a prominent bolted splice plate band flanked by dark scorch marks and orange cut-line tabs."""
    m_grey = mat_map["R1_GreyMetal"]
    m_bare = mat_map["R1_BareSteel"]
    m_rubber = mat_map["R1_Rubber"]

    rot_mat = Euler(
        (math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])),
        "XYZ",
    ).to_matrix()
    ctr = Vector(center)

    def place_local_box(
        lsize: tuple[float, float, float],
        lpos: tuple[float, float, float],
        m_idx: int,
        bevel: float = 0.004,
    ) -> None:
        wpos = ctr + (rot_mat @ Vector(lpos))
        _add_oriented_box(bm, lsize, (wpos.x, wpos.y, wpos.z), rot_deg, m_idx, bevel=bevel)

    # 1. Wide dark thermal scorch/char collar (R1_Rubber) flanking both sides of the cut seam
    place_local_box(
        (span_w * 1.04, band_depth * 2.15, band_thick * 0.85),
        (0.0, 0.0, 0.0),
        m_rubber,
        bevel=0.005,
    )
    # Irregular outer soot/scorch blotches along the cut line
    for side_y in (-band_depth * 0.90, band_depth * 0.90):
        for frac in (-0.34, 0.0, 0.34):
            if side_y < 0.0 and abs(frac) < 0.05:
                continue
            place_local_box(
                (span_w * 0.20, band_depth * 0.55, band_thick * 0.54),
                (span_w * frac, side_y, 0.0),
                m_rubber,
                bevel=0.004,
            )

    # 2. Twin heavy bolted splice plates (R1_GreyMetal) with a narrow central kerf/cut channel
    plate_y_off = band_depth * 0.34
    for sy in (-1.0, 1.0):
        place_local_box(
            (span_w, band_depth * 0.52, band_thick * 1.15),
            (0.0, sy * plate_y_off, 0.0),
            m_grey,
            bevel=0.005,
        )

    # 3. Exposed bright bare-steel shear bridge bars across the cut seam (what the cutter slices)
    for frac in (-0.36, -0.12, 0.12, 0.36):
        place_local_box(
            (0.055, band_depth * 1.22, band_thick * 1.24),
            (span_w * frac, 0.0, 0.0),
            m_bare,
            bevel=0.003,
        )

    # 4. Teal Kestrel-3 cutter-target brackets at both ends of the seam ("cutter goes here" read)
    m_teal = mat_map["R1_TealMark"]
    for sx in (-1.0, 1.0):
        place_local_box(
            (0.11, band_depth * 1.38, band_thick * 1.30),
            (sx * (span_w * 0.46), 0.0, 0.0),
            m_teal,
            bevel=0.004,
        )

    # 5. Row of heavy hex bolt heads along both splice plates
    for i in range(bolt_count):
        t = -0.38 + 0.76 * (i / max(1, bolt_count - 1))
        bx = span_w * t
        for sy in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                place_local_box(
                    (0.036, 0.036, 0.028),
                    (bx, sy * plate_y_off, sz * (band_thick * 0.62)),
                    m_bare,
                    bevel=0.002,
                )


def _register_kestrel_era_materials() -> None:
    """Register R1_WhitePaint and R1_TealMark into r1kit at runtime without modifying r1kit.py on disk."""
    extra_specs = {
        "R1_WhitePaint": {
            "base_color": r1kit._srgb_to_linear("#ECEAE4"),
            "wear_color": r1kit._srgb_to_linear("#565A63"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.04,
            "roughness": 0.38,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_TealMark": {
            "base_color": r1kit._srgb_to_linear("#3E7A82"),
            "wear_color": r1kit._srgb_to_linear("#9BA8AC"),
            "dust_color": r1kit._srgb_to_linear("#85888A"),
            "metallic": 0.06,
            "roughness": 0.40,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
    }
    for name, spec in extra_specs.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = tuple(r1kit.ALLOWED_MATERIALS) + (name,)
        r1kit.MATERIAL_SPECS[name] = spec


def _finalize_debris_object(
    name: str,
    bm: bmesh.types.BMesh,
    slot_names: list[str],
    target_size: tuple[float, float, float],
    target_min_z: float,
    cut_pivot: tuple[float, float, float],
    signage_cell: str | None = None,
) -> bpy.types.Object:
    """Normalize bounds, build Blender mesh, assign R1 materials/UVs/vertex colors, and attach CutPoint."""
    rc.register_ruins_materials()
    _register_kestrel_era_materials()
    canonical_mats = r1kit.ensure_materials(use_vertex_color_shading=False)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    xs = [v.co.x for v in bm.verts]
    ys = [v.co.y for v in bm.verts]
    zs = [v.co.z for v in bm.verts]
    cur_dx = max(xs) - min(xs)
    cur_dy = max(ys) - min(ys)
    cur_dz = max(zs) - min(zs)
    cx = 0.5 * (max(xs) + min(xs))
    cy = 0.5 * (max(ys) + min(ys))
    min_z = min(zs)

    bmesh.ops.translate(bm, vec=(-cx, -cy, -min_z), verts=bm.verts)
    bmesh.ops.scale(
        bm,
        vec=(target_size[0] / cur_dx, target_size[1] / cur_dy, target_size[2] / cur_dz),
        verts=bm.verts,
    )
    bmesh.ops.translate(bm, vec=(0.0, 0.0, target_min_z), verts=bm.verts)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()

    for s in slot_names:
        mesh.materials.append(canonical_mats[s])

    root = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(root)

    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)

    if signage_cell is not None and "R1_Signage" in slot_names:
        sig_idx = slot_names.index("R1_Signage")
        u0, v0, u1, v1 = rc.SIGNAGE_UV_CELLS.get(
            signage_cell, next(iter(rc.SIGNAGE_UV_CELLS.values()))
        )
        uv_layer = mesh.uv_layers.get("UVMap") or mesh.uv_layers.new(name="UVMap")
        uv_data = uv_layer.data
        sig_verts = [
            mesh.vertices[vi].co
            for poly in mesh.polygons
            if poly.material_index == sig_idx
            for vi in poly.vertices
        ]
        if sig_verts:
            s_min_x = min(v.x for v in sig_verts)
            s_max_x = max(v.x for v in sig_verts)
            s_min_y = min(v.y for v in sig_verts)
            s_max_y = max(v.y for v in sig_verts)
            span_x = max(1e-4, s_max_x - s_min_x)
            span_y = max(1e-4, s_max_y - s_min_y)
            for poly in mesh.polygons:
                if poly.material_index != sig_idx:
                    continue
                for li in poly.loop_indices:
                    co = mesh.vertices[mesh.loops[li].vertex_index].co
                    sx = max(0.02, min(0.98, (co.x - s_min_x) / span_x))
                    sy = max(0.02, min(0.98, (co.y - s_min_y) / span_y))
                    uv_data[li].uv = (round(u0 + sx * (u1 - u0), 5), round(v0 + sy * (v1 - v0), 5))
            mesh.update()

    rc.bake_ruins_vertex_colors(root, footprint_radius=max(target_size[0], target_size[1]) * 0.80)

    cut_empty = bpy.data.objects.new("CutPoint", None)
    cut_empty.empty_display_type = "PLAIN_AXES"
    cut_empty.empty_display_size = 0.12
    bpy.context.scene.collection.objects.link(cut_empty)
    cut_empty.parent = root
    cut_empty.location = cut_pivot
    return root


def build_kestrel_hull_panel() -> bpy.types.Object:
    """Piece 1: Curved Kestrel-3 hull panel with bolted cut seam band + scorch marks (2.80 x 1.85 x 1.35 m)."""
    slot_names = [
        "R1_WhitePaint",
        "R1_GreyMetal",
        "R1_Basalt",
        "R1_BareSteel",
        "R1_TealMark",
        "R1_GoldFoil",
        "R1_Rubber",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried base skirt / regolith-embedded hull cradle (sits in [-0.22, 0.02] m)
    _add_buried_skirt_plinth(bm, 2.15, 1.32, 2.38, 1.48, -0.22, 0.02, m["R1_Basalt"])

    # Curved segmented aeroshell hull panel (5 arched hull strakes tilted upward)
    strake_angles = [-26.0, -13.0, 0.0, 13.0, 26.0]
    for ang in strake_angles:
        rad = math.radians(ang)
        sx = math.sin(rad) * 1.05
        sz = 0.34 + math.cos(rad) * 0.30
        tilt_y = -ang * 0.85
        # Outer white thermal hull skin
        _add_oriented_box(
            bm,
            (0.58, 1.52, 0.085),
            (sx, -0.04, sz),
            (16.0, tilt_y, 3.0),
            m["R1_WhitePaint"],
            bevel=0.008,
        )
        # Inner gold MLI backing blanket pad
        _add_oriented_box(
            bm,
            (0.48, 1.32, 0.045),
            (sx * 0.96, -0.02, sz - 0.055),
            (16.0, tilt_y, 3.0),
            m["R1_GoldFoil"],
            bevel=0.004,
        )
        # Longitudinal Z-frames / stringer ribs on underside
        _add_oriented_box(
            bm,
            (0.09, 1.58, 0.14),
            (sx, -0.04, sz - 0.08),
            (16.0, tilt_y, 3.0),
            m["R1_Basalt"],
            bevel=0.005,
        )

    # Transverse bulkhead frame arcs at front and rear edges
    for y_off in (-0.74, 0.62):
        _add_oriented_box(
            bm,
            (2.42, 0.11, 0.17),
            (0.0, y_off, 0.48),
            (16.0, 0.0, 3.0),
            m["R1_GreyMetal"],
            bevel=0.006,
        )

    # Teal Kestrel-3 livery stripe across upper hull strakes
    _add_oriented_box(
        bm,
        (2.15, 0.22, 0.095),
        (0.0, 0.42, 0.78),
        (16.0, 0.0, 3.0),
        m["R1_TealMark"],
        bevel=0.004,
    )

    # Prominent bolted cut seam band + scorch marks wrapping proud over the hull panel crest!
    _add_bolted_cut_seam_band(
        bm,
        center=(0.0, 0.04, 0.73),
        rot_deg=(16.0, 0.0, 3.0),
        span_w=2.34,
        band_thick=0.16,
        band_depth=0.22,
        mat_map=m,
        bolt_count=7,
    )

    root = _finalize_debris_object(
        name="Kestrel_HullPanel",
        bm=bm,
        slot_names=slot_names,
        target_size=(2.80, 1.85, 1.35),
        target_min_z=-0.22,
        cut_pivot=(0.0, -0.05, 0.72),
    )

    # Attach ~0.4 m Tycho Heavy Industries maker decal (Decal_Tycho_1) on the central white hull strake,
    # positioned in the open bay so its plasma-cut / scorched upper-right corner kisses the cut-seam collar.
    white_idx = m["R1_WhitePaint"]
    central_polys = [
        p
        for p in root.data.polygons
        if p.material_index == white_idx and abs(p.center.x) < 0.14 and p.normal.z > 0.85 and p.area > 0.30
    ]
    top_poly = max(central_polys, key=lambda p: p.area)
    n_bl = Vector(top_poly.normal).normalized()
    p0_bl = Vector(top_poly.center)
    decal_x_bl = -0.02
    decal_y_bl = -0.44
    decal_z_bl = p0_bl.z - (n_bl.x * (decal_x_bl - p0_bl.x) + n_bl.y * (decal_y_bl - p0_bl.y)) / n_bl.z
    p_bl = Vector((decal_x_bl, decal_y_bl, decal_z_bl))
    up_bl = (Vector((0.0, 1.0, 0.0)) - n_bl * n_bl.y).normalized()

    r1kit.add_maker_decal_node(
        "Decal_Tycho_1",
        root,
        cell_key="TYCHO",
        center_u=(p_bl.x, p_bl.z, p_bl.y),
        normal_u=(n_bl.x, n_bl.z, n_bl.y),
        up_u=(up_bl.x, up_bl.z, up_bl.y),
        width=0.40,
        height=0.40,
        offset=0.002,
    )
    return root


def build_kestrel_strut_frame() -> bpy.types.Object:
    """Piece 2: Bent airframe strut frame with bolted cut seam band + scorch marks (2.45 x 1.60 x 1.25 m)."""
    slot_names = [
        "R1_Basalt",
        "R1_GreyMetal",
        "R1_BareSteel",
        "R1_WhitePaint",
        "R1_TealMark",
        "R1_Copper",
        "R1_Brass",
        "R1_Rubber",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried lower footing skirt
    _add_buried_skirt_plinth(bm, 2.05, 1.26, 2.38, 1.52, -0.20, 0.06, m["R1_Basalt"])

    # Two heavy bent longitudinal I-beam longerons
    for side_y in (-0.48, 0.48):
        _add_oriented_box(
            bm,
            (1.22, 0.16, 0.18),
            (-0.48, side_y, 0.34),
            (0.0, -24.0, 4.0 * (-1 if side_y < 0 else 1)),
            m["R1_Basalt"],
            bevel=0.006,
        )
        _add_oriented_box(
            bm,
            (1.26, 0.16, 0.18),
            (0.46, side_y * 0.92, 0.66),
            (4.0, 14.0, -5.0 * (-1 if side_y < 0 else 1)),
            m["R1_Basalt"],
            bevel=0.006,
        )

    # Diagonal cross-bracing struts
    _add_oriented_box(
        bm,
        (1.45, 0.11, 0.11),
        (-0.22, 0.0, 0.42),
        (12.0, -18.0, 34.0),
        m["R1_GreyMetal"],
        bevel=0.005,
    )
    _add_oriented_box(
        bm,
        (1.45, 0.11, 0.11),
        (0.24, 0.0, 0.58),
        (-12.0, 16.0, -34.0),
        m["R1_GreyMetal"],
        bevel=0.005,
    )

    # Transverse bulkhead web frames at left, center, and right
    for x_off, z_off, ry in ((-0.84, 0.22, -22.0), (0.02, 0.58, -4.0), (0.86, 0.52, 16.0)):
        _add_oriented_box(
            bm,
            (0.16, 1.18, 0.32),
            (x_off, 0.0, z_off),
            (0.0, ry, 0.0),
            m["R1_GreyMetal"],
            bevel=0.006,
        )

    # Remnant torn white hull skin flange riveted to one side of the truss
    _add_oriented_box(
        bm,
        (1.55, 0.46, 0.07),
        (-0.18, -0.52, 0.46),
        (28.0, -10.0, 4.0),
        m["R1_WhitePaint"],
        bevel=0.005,
    )

    # Severed hydraulic / pneumatic lines running along the truss
    _add_oriented_frustum(
        bm, 0.038, 0.038, 1.75, 10, (-0.05, 0.32, 0.48), (0.0, 78.0, 6.0), m["R1_Copper"]
    )
    _add_oriented_frustum(
        bm, 0.052, 0.052, 0.36, 10, (0.45, 0.32, 0.56), (0.0, 78.0, 6.0), m["R1_Brass"]
    )

    # Prominent bolted cut seam splice band + scorch marks across the central truss apex!
    _add_bolted_cut_seam_band(
        bm,
        center=(0.02, 0.0, 0.68),
        rot_deg=(0.0, -4.0, 90.0),
        span_w=1.24,
        band_thick=0.22,
        band_depth=0.30,
        mat_map=m,
        bolt_count=6,
    )

    return _finalize_debris_object(
        name="Kestrel_StrutFrame",
        bm=bm,
        slot_names=slot_names,
        target_size=(2.45, 1.60, 1.25),
        target_min_z=-0.20,
        cut_pivot=(0.02, 0.0, 0.74),
    )


def build_kestrel_tank() -> bpy.types.Object:
    """Piece 3: Half-buried Kestrel-3 propellant pressure tank with gold MLI wrap (1.95 x 1.35 x 1.15 m)."""
    slot_names = [
        "R1_WhitePaint",
        "R1_GoldFoil",
        "R1_Basalt",
        "R1_GreyMetal",
        "R1_TealMark",
        "R1_Brass",
        "R1_Copper",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried tank cradle & regolith skirt
    _add_buried_skirt_plinth(bm, 1.62, 1.08, 1.88, 1.30, -0.20, 0.10, m["R1_Basalt"])

    # Main cylindrical propellant vessel with domed ends (tilted at 9 deg pitch as if half-sunk)
    _add_oriented_frustum(
        bm, 0.48, 0.48, 1.28, 16, (-0.04, 0.0, 0.42), (0.0, 81.0, 4.0), m["R1_WhitePaint"]
    )
    # Domed end caps (stepped frustums)
    _add_oriented_frustum(
        bm, 0.48, 0.28, 0.24, 16, (-0.78, -0.05, 0.30), (0.0, 81.0, 4.0), m["R1_WhitePaint"]
    )
    _add_oriented_frustum(
        bm, 0.28, 0.48, 0.24, 16, (0.70, 0.05, 0.54), (0.0, 81.0, 4.0), m["R1_WhitePaint"]
    )

    # Gold foil MLI thermal blanket wrap sleeve around center section
    _add_oriented_frustum(
        bm, 0.505, 0.505, 0.72, 16, (-0.06, 0.0, 0.42), (0.0, 81.0, 4.0), m["R1_GoldFoil"]
    )

    # Twin heavy dark-steel girth clamp rings
    for x_off, z_off in ((-0.46, 0.35), (0.36, 0.48)):
        _add_oriented_frustum(
            bm, 0.53, 0.53, 0.11, 16, (x_off, 0.0, z_off), (0.0, 81.0, 4.0), m["R1_Basalt"]
        )

    # Teal Kestrel-3 livery stripe band near forward dome
    _add_oriented_frustum(
        bm, 0.515, 0.515, 0.14, 16, (0.52, 0.03, 0.51), (0.0, 81.0, 4.0), m["R1_TealMark"]
    )

    # Upper propellant feed manifold box & brass/copper valves
    _add_oriented_box(
        bm,
        (0.42, 0.34, 0.16),
        (0.18, 0.0, 0.86),
        (0.0, -9.0, 4.0),
        m["R1_GreyMetal"],
        bevel=0.006,
    )
    for vy in (-0.09, 0.09):
        _add_oriented_frustum(
            bm, 0.055, 0.045, 0.18, 12, (0.18, vy, 0.96), (0.0, -9.0, 0.0), m["R1_Brass"]
        )
    _add_oriented_frustum(
        bm, 0.04, 0.04, 0.54, 10, (-0.12, 0.24, 0.82), (14.0, 76.0, 0.0), m["R1_Copper"]
    )

    # Tycho identification badge plate on upper flank
    _add_oriented_box(
        bm,
        (0.38, 0.18, 0.03),
        (-0.22, -0.38, 0.72),
        (38.0, -8.0, 4.0),
        m["R1_TealMark"],
        bevel=0.003,
    )

    return _finalize_debris_object(
        name="Kestrel_Tank",
        bm=bm,
        slot_names=slot_names,
        target_size=(1.95, 1.35, 1.15),
        target_min_z=-0.20,
        cut_pivot=(0.18, 0.0, 0.88),
    )


def build_kestrel_engine_bell() -> bpy.types.Object:
    """Piece 4: Torn De Laval main thruster nozzle bell with cooling manifold (1.65 x 1.55 x 1.30 m)."""
    slot_names = [
        "R1_Basalt",
        "R1_GreyMetal",
        "R1_BareSteel",
        "R1_Copper",
        "R1_Brass",
        "R1_Rubber",
        "R1_TealMark",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried base skirt where the lower lip of the nozzle rests in regolith
    _add_buried_skirt_plinth(bm, 1.36, 1.28, 1.60, 1.50, -0.18, 0.08, m["R1_Basalt"])

    # Stepped parabolic nozzle cone tilted at 32 deg so mouth opens obliquely
    tilt = (28.0, -14.0, 10.0)
    _add_oriented_frustum(
        bm, 0.68, 0.52, 0.42, 16, (0.06, -0.08, 0.34), tilt, m["R1_Basalt"], cap_mat_idx=m["R1_Rubber"]
    )
    _add_oriented_frustum(
        bm, 0.52, 0.34, 0.38, 16, (-0.02, 0.06, 0.62), tilt, m["R1_Basalt"]
    )
    _add_oriented_frustum(
        bm, 0.30, 0.36, 0.32, 16, (-0.10, 0.20, 0.88), tilt, m["R1_GreyMetal"]
    )

    # Copper regenerative cooling manifold bands around nozzle exterior
    for r_band, pos in (
        (0.61, (0.02, -0.01, 0.48)),
        (0.45, (-0.05, 0.11, 0.72)),
    ):
        _add_oriented_frustum(bm, r_band, r_band, 0.09, 16, pos, tilt, m["R1_Copper"])

    # Charred soot collar at nozzle rim
    _add_oriented_frustum(
        bm, 0.70, 0.67, 0.10, 16, (0.10, -0.16, 0.18), tilt, m["R1_Rubber"]
    )

    # Upper sheared gimbal mount ring & actuator brackets
    _add_oriented_box(
        bm,
        (0.56, 0.56, 0.16),
        (-0.14, 0.28, 1.00),
        tilt,
        m["R1_BareSteel"],
        bevel=0.006,
    )
    _add_oriented_box(
        bm,
        (0.68, 0.18, 0.14),
        (-0.14, 0.28, 0.96),
        tilt,
        m["R1_TealMark"],
        bevel=0.005,
    )

    # Side turbopump exhaust stub & brass feed line
    _add_oriented_frustum(
        bm, 0.14, 0.11, 0.48, 12, (0.38, 0.18, 0.66), (18.0, 12.0, 0.0), m["R1_Brass"]
    )

    return _finalize_debris_object(
        name="Kestrel_EngineBell",
        bm=bm,
        slot_names=slot_names,
        target_size=(1.65, 1.55, 1.30),
        target_min_z=-0.18,
        cut_pivot=(-0.12, 0.24, 0.95),
    )


def build_kestrel_cable_tray() -> bpy.types.Object:
    """Piece 5: Twisted umbilical cable tray & conduit bundle (1.45 x 0.92 x 0.84 m)."""
    slot_names = [
        "R1_GreyMetal",
        "R1_Basalt",
        "R1_BareSteel",
        "R1_Rubber",
        "R1_Copper",
        "R1_Brass",
        "R1_TealMark",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried base skirt
    _add_buried_skirt_plinth(bm, 1.18, 0.72, 1.38, 0.88, -0.15, 0.06, m["R1_Basalt"])

    # Twisted / arched steel cable ladder rails (two angled segments)
    for side_y in (-0.28, 0.28):
        _add_oriented_box(
            bm,
            (0.82, 0.08, 0.12),
            (-0.26, side_y, 0.26),
            (6.0, -22.0, 5.0),
            m["R1_GreyMetal"],
            bevel=0.004,
        )
        _add_oriented_box(
            bm,
            (0.78, 0.08, 0.12),
            (0.28, side_y * 0.92, 0.46),
            (-8.0, 14.0, -6.0),
            m["R1_GreyMetal"],
            bevel=0.004,
        )

    # Cross rungs along the ladder tray
    for x_off, z_off, ry in (
        (-0.52, 0.16, -22.0),
        (-0.26, 0.26, -22.0),
        (0.0, 0.38, -4.0),
        (0.26, 0.46, 14.0),
        (0.52, 0.40, 14.0),
    ):
        _add_oriented_box(
            bm,
            (0.09, 0.64, 0.08),
            (x_off, 0.0, z_off),
            (0.0, ry, 0.0),
            m["R1_Basalt"],
            bevel=0.003,
        )

    # Heavy rubber-sheathed umbilical trunks & exposed copper bus conduits
    for y_off, mat_name, rad in (
        (-0.14, "R1_Rubber", 0.065),
        (0.0, "R1_Copper", 0.055),
        (0.14, "R1_Rubber", 0.065),
    ):
        _add_oriented_frustum(
            bm, rad, rad, 1.24, 10, (0.0, y_off, 0.36), (4.0, 74.0, 4.0), m[mat_name]
        )

    # Upper torn junction box & brass multi-pin umbilical head
    _add_oriented_box(
        bm,
        (0.36, 0.48, 0.24),
        (0.38, 0.0, 0.56),
        (-6.0, 14.0, -4.0),
        m["R1_BareSteel"],
        bevel=0.005,
    )
    _add_oriented_box(
        bm,
        (0.42, 0.14, 0.28),
        (0.02, 0.0, 0.44),
        (0.0, -4.0, 0.0),
        m["R1_TealMark"],
        bevel=0.004,
    )
    _add_oriented_frustum(
        bm, 0.11, 0.09, 0.22, 12, (0.54, 0.0, 0.58), (0.0, 104.0, 0.0), m["R1_Brass"]
    )

    return _finalize_debris_object(
        name="Kestrel_CableTray",
        bm=bm,
        slot_names=slot_names,
        target_size=(1.45, 0.92, 0.84),
        target_min_z=-0.15,
        cut_pivot=(0.02, 0.0, 0.52),
    )


def build_kestrel_landing_foot() -> bpy.types.Object:
    """Piece 6: Sheared Kestrel-3 landing-leg footpad & damper strut (1.18 x 1.12 x 0.92 m)."""
    slot_names = [
        "R1_Basalt",
        "R1_GreyMetal",
        "R1_BareSteel",
        "R1_WhitePaint",
        "R1_GoldFoil",
        "R1_TealMark",
        "R1_Rubber",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried base skirt beneath the lunar saucer footpad
    _add_buried_skirt_plinth(bm, 0.96, 0.92, 1.14, 1.08, -0.16, 0.05, m["R1_Basalt"])

    # Saucer-shaped crushable footpad dish (tilted slightly at 8 deg)
    _add_oriented_frustum(
        bm, 0.52, 0.42, 0.16, 16, (0.0, 0.0, 0.10), (6.0, -5.0, 0.0), m["R1_Basalt"]
    )
    _add_oriented_frustum(
        bm, 0.40, 0.24, 0.12, 16, (0.0, 0.0, 0.22), (6.0, -5.0, 0.0), m["R1_GreyMetal"]
    )

    # Gold foil thermal boot around the ball-joint socket
    _add_oriented_frustum(
        bm, 0.22, 0.16, 0.16, 12, (0.01, -0.01, 0.32), (10.0, -14.0, 0.0), m["R1_GoldFoil"]
    )

    # Main oleo-pneumatic shock strut (tilted at 22 deg)
    strut_rot = (14.0, -20.0, 8.0)
    _add_oriented_frustum(
        bm, 0.12, 0.12, 0.44, 12, (-0.06, -0.05, 0.48), strut_rot, m["R1_WhitePaint"]
    )
    _add_oriented_frustum(
        bm, 0.085, 0.085, 0.24, 12, (-0.12, -0.10, 0.66), strut_rot, m["R1_BareSteel"]
    )
    _add_oriented_frustum(
        bm, 0.135, 0.135, 0.10, 12, (-0.09, -0.07, 0.56), strut_rot, m["R1_Rubber"]
    )

    # Twin A-frame drag brace links with teal livery stripes
    for sy in (-1.0, 1.0):
        _add_oriented_box(
            bm,
            (0.52, 0.09, 0.09),
            (0.14, sy * 0.22, 0.38),
            (sy * 18.0, 32.0, sy * -14.0),
            m["R1_TealMark"],
            bevel=0.004,
        )

    # Sheared upper trunnion collar (cut point)
    _add_oriented_box(
        bm,
        (0.28, 0.30, 0.12),
        (-0.15, -0.12, 0.72),
        strut_rot,
        m["R1_GreyMetal"],
        bevel=0.005,
    )

    return _finalize_debris_object(
        name="Kestrel_LandingFoot",
        bm=bm,
        slot_names=slot_names,
        target_size=(1.18, 1.12, 0.92),
        target_min_z=-0.16,
        cut_pivot=(-0.14, -0.10, 0.70),
    )


def build_kestrel_antenna_boom() -> bpy.types.Object:
    """Piece 7: Broken Kestrel-3 solar-boom & high-gain antenna fragment (1.85 x 1.25 x 1.05 m)."""
    slot_names = [
        "R1_Basalt",
        "R1_GreyMetal",
        "R1_BareSteel",
        "R1_WhitePaint",
        "R1_TealMark",
        "R1_SolarCell",
        "R1_Copper",
        "R1_Brass",
        "R1_Rubber",
    ]
    m = {name: i for i, name in enumerate(slot_names)}
    bm = bmesh.new()

    # Half-buried base skirt beneath the torn root hinge & lower solar wing corner
    _add_buried_skirt_plinth(bm, 1.52, 0.98, 1.78, 1.18, -0.16, 0.05, m["R1_Basalt"])

    # Torn root deployment hinge housing & scorched fracture collar at (-0.62, -0.10, 0.22)
    root_rot = (8.0, -24.0, 10.0)
    _add_oriented_box(
        bm,
        (0.34, 0.32, 0.24),
        (-0.62, -0.10, 0.22),
        root_rot,
        m["R1_GreyMetal"],
        bevel=0.006,
    )
    _add_oriented_box(
        bm,
        (0.18, 0.36, 0.28),
        (-0.74, -0.12, 0.18),
        root_rot,
        m["R1_Rubber"],
        bevel=0.005,
    )
    # Teal Kestrel-3 livery band on root boom collar
    _add_oriented_box(
        bm,
        (0.16, 0.34, 0.26),
        (-0.48, -0.08, 0.28),
        root_rot,
        m["R1_TealMark"],
        bevel=0.005,
    )

    # Primary tubular white-painted deployment boom spar rising from root hinge to elbow
    boom_rot = (6.0, 64.0, 12.0)
    _add_oriented_frustum(
        bm, 0.085, 0.075, 1.18, 12, (-0.08, -0.02, 0.48), boom_rot, m["R1_WhitePaint"]
    )
    # Parallel secondary bare-steel truss longerons & cross-braces along the boom
    for sy in (-0.13, 0.13):
        _add_oriented_frustum(
            bm, 0.032, 0.032, 1.12, 10, (-0.08, sy, 0.44), boom_rot, m["R1_BareSteel"]
        )
    for frac, z_brace in ((-0.38, 0.34), (-0.06, 0.49), (0.26, 0.64)):
        _add_oriented_box(
            bm,
            (0.06, 0.30, 0.12),
            (frac, -0.02, z_brace),
            (6.0, -26.0, 12.0),
            m["R1_GreyMetal"],
            bevel=0.004,
        )

    # Copper power / telemetry harness conduit running along the boom to the cut point
    _add_oriented_frustum(
        bm, 0.034, 0.034, 1.08, 10, (-0.04, 0.08, 0.54), boom_rot, m["R1_Copper"]
    )

    # Sheared elbow actuator collar at CutPoint (0.12, 0.04, 0.78)
    _add_oriented_box(
        bm,
        (0.26, 0.26, 0.18),
        (0.12, 0.04, 0.76),
        (4.0, -22.0, 8.0),
        m["R1_BareSteel"],
        bevel=0.005,
    )
    _add_oriented_box(
        bm,
        (0.12, 0.28, 0.20),
        (0.22, 0.05, 0.80),
        (4.0, -22.0, 8.0),
        m["R1_TealMark"],
        bevel=0.004,
    )

    # Broken solar-wing paddle cantilevered off the -Y flank of the boom (tilted into the regolith)
    wing_rot = (26.0, -14.0, 8.0)
    wing_rot_mat = Euler(
        (math.radians(wing_rot[0]), math.radians(wing_rot[1]), math.radians(wing_rot[2])),
        "XYZ",
    ).to_matrix()
    wing_ctr = Vector((-0.14, -0.34, 0.34))

    def place_wing_box(
        lsize: tuple[float, float, float],
        lpos: tuple[float, float, float],
        m_idx: int,
        bevel: float = 0.004,
    ) -> None:
        wpos = wing_ctr + (wing_rot_mat @ Vector(lpos))
        _add_oriented_box(bm, lsize, (wpos.x, wpos.y, wpos.z), wing_rot, m_idx, bevel=bevel)

    # Aluminum honeycomb backing plate & white perimeter frame
    place_wing_box((0.96, 0.56, 0.046), (0.0, 0.0, 0.0), m["R1_WhitePaint"], bevel=0.005)
    place_wing_box((0.88, 0.48, 0.048), (0.0, 0.0, 0.006), m["R1_GreyMetal"], bevel=0.004)
    # Teal Tycho trim strip along the upper edge of the solar wing frame
    place_wing_box((0.88, 0.048, 0.052), (0.0, 0.22, 0.010), m["R1_TealMark"], bevel=0.003)
    # Two deep-violet photovoltaic cell bays separated by a bare-steel busbar rib
    for sx_cell in (-0.22, 0.22):
        place_wing_box((0.38, 0.40, 0.040), (sx_cell, -0.01, 0.022), m["R1_SolarCell"], bevel=0.003)
    place_wing_box((0.048, 0.42, 0.044), (0.0, -0.01, 0.024), m["R1_BareSteel"], bevel=0.003)

    # Fractured outer solar-wing corner bent at a sharp angle with charred edge
    bent_wing_rot = (18.0, 22.0, -14.0)
    _add_oriented_box(
        bm,
        (0.36, 0.44, 0.045),
        (-0.62, -0.38, 0.24),
        bent_wing_rot,
        m["R1_SolarCell"],
        bevel=0.004,
    )

    # Cantilevered high-gain dish & brass feed horn at the upper +X end of the boom,
    # angled so its white dish reflector face, teal rim collar, and brass/copper feed horn are visible
    dish_rot = (28.0, 22.0, -10.0)
    dish_rot_mat = Euler(
        (math.radians(dish_rot[0]), math.radians(dish_rot[1]), math.radians(dish_rot[2])),
        "XYZ",
    ).to_matrix()
    dish_ctr = Vector((0.56, 0.14, 0.56))

    def place_dish_frustum(
        r_b: float,
        r_t: float,
        length: float,
        segs: int,
        lpos: tuple[float, float, float],
        m_idx: int,
        cap_m_idx: int,
    ) -> None:
        wpos = dish_ctr + (dish_rot_mat @ Vector(lpos))
        _add_oriented_frustum(
            bm, r_b, r_t, length, segs, (wpos.x, wpos.y, wpos.z), dish_rot, m_idx, cap_mat_idx=cap_m_idx
        )

    place_dish_frustum(0.22, 0.43, 0.12, 16, (0.0, 0.0, 0.0), m["R1_WhitePaint"], m["R1_WhitePaint"])
    place_dish_frustum(0.43, 0.46, 0.048, 16, (0.0, 0.0, 0.068), m["R1_TealMark"], m["R1_WhitePaint"])
    place_dish_frustum(0.095, 0.080, 0.040, 12, (0.0, 0.0, 0.100), m["R1_GreyMetal"], m["R1_GreyMetal"])
    place_dish_frustum(0.062, 0.042, 0.26, 12, (0.0, 0.0, 0.220), m["R1_Brass"], m["R1_Copper"])

    return _finalize_debris_object(
        name="Kestrel_AntennaBoom",
        bm=bm,
        slot_names=slot_names,
        target_size=(1.85, 1.25, 1.05),
        target_min_z=-0.16,
        cut_pivot=(0.12, 0.04, 0.78),
    )


DEBRIS_BUILDERS = (
    ("Kestrel_HullPanel", build_kestrel_hull_panel),
    ("Kestrel_StrutFrame", build_kestrel_strut_frame),
    ("Kestrel_Tank", build_kestrel_tank),
    ("Kestrel_EngineBell", build_kestrel_engine_bell),
    ("Kestrel_CableTray", build_kestrel_cable_tray),
    ("Kestrel_LandingFoot", build_kestrel_landing_foot),
    ("Kestrel_AntennaBoom", build_kestrel_antenna_boom),
)


def render_kestrel_debris_with_rover(renders_dir: Path) -> None:
    """Render all 6 Kestrel-3 wreck debris pieces arranged around Rover 07 under Day C & Night A."""
    _ = renders_dir
    rc.reset_ruins_scene()

    scene_group = bpy.data.objects.new("_KestrelScaleScene", None)
    bpy.context.scene.collection.objects.link(scene_group)

    placements = (
        (build_kestrel_hull_panel, (1.40, -0.35, 0.0), 10.0),
        (build_kestrel_strut_frame, (1.35, 1.95, 0.0), -18.0),
        (build_kestrel_tank, (-1.65, 2.05, 0.0), 15.0),
        (build_kestrel_engine_bell, (3.65, 0.85, 0.0), -25.0),
        (build_kestrel_cable_tray, (-0.15, -1.75, 0.0), 8.0),
        (build_kestrel_landing_foot, (3.25, -1.55, 0.0), 22.0),
    )

    for builder_fn, loc, yaw_deg in placements:
        r = builder_fn()
        if r.data is not None:
            for v in r.data.vertices:
                if v.co.z < 0.0:
                    v.co.z = 0.0
            r.data.update()
        r.parent = scene_group
        r.location = Vector(loc)
        r.rotation_euler = Euler((0.0, 0.0, math.radians(yaw_deg)), "XYZ")

    rover_fbx = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")
    if rover_fbx.exists():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(rover_fbx.resolve()))
        rover_objs = [o for o in bpy.data.objects if o not in before]
        for o in rover_objs:
            if o.type == "MESH" and o.data is not None:
                for slot in o.material_slots:
                    if slot.material is not None:
                        base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                        if base_m is not None:
                            slot.material = base_m
            if o.parent is None:
                o.parent = scene_group
                o.location = Vector((-1.75, -0.55, 0.0))
                o.rotation_euler = Euler((0.0, 0.0, math.radians(24.0)), "XYZ")

    rc.render_ruin_day_night(
        scene_group,
        slug="kestrel_debris_scale",
        cam_angle_deg=26.0,
        cam_elev_deg=24.0,
        res=(1024, 768),
    )


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export the 6 Kestrel-3 wreck debris FBX models.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Wreck"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    args.out_dir.mkdir(parents=True, exist_ok=True)
    for name, builder_fn in DEBRIS_BUILDERS:
        rc.reset_ruins_scene()
        root = builder_fn()
        fbx_path = args.out_dir / f"{name}.fbx"
        r1kit.export_fbx(root, fbx_path)
        print(f"[kestrel_debris] Exported {fbx_path.name}")

    if not args.skip_renders:
        render_kestrel_debris_with_rover(args.renders_dir)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
