"""Procedural Blender generator for the three R1 opening unlocks:

1. ``CrateRack.fbx``        -> ``Assets/_Project/Art/R1/Models/Structures/CrateRack.fbx``
   Base's first home storage: an open cream tube frame (1.60 m W x 1.20 m H x 0.90 m D)
   holding three salvaged VIS crate shells (``Crate_0``, ``Crate_1``, ``Crate_2``) with
   four slots (``Slot_0..3``); chalked old white enamel (``R1_WhitePaint``), half-cut
   VIS stencils (``Decal_VIS_1..3``), bright weld beads (``WeldBeads``), a cut ``"07"``
   tag (``Tag_07``), and four separate hinged lid nodes (``Lid_0..3``) that can visibly
   close, each with a small amber latch lamp (``LatchLamp_0..3``).

2. ``ReflectorBeacon.fbx``  -> ``Assets/_Project/Art/R1/Models/Structures/ReflectorBeacon.fbx``
   Passive survey trail marker: a slim cream post (1.30 m H, 0.42 m W x 0.42 m D) on
   three short steel foot spikes (``FootSpikes``), a cap housing (``CapHousing``) with
   one coral band (``R1_OrangeStripe``) and a single faceted three-sided hollow
   corner-cube retroreflector (``CornerCube``), plus bright weld beads (``WeldBeads``)
   and a cut ``"07"`` tag (``Tag_07``).

3. ``Kit_HexAdapter.fbx``   -> ``Assets/_Project/Art/R1/Models/Rover/Kit_HexAdapter.fbx``
   Salvaged Tycho charge-port adapter kit (~0.184 m across, 0.046 m deep) that mounts
   flush on 07's flank socket: flat hexagonal plate (``HexPlate``) in Tycho white
   composite (``R1_WhitePaint``) with a muted teal rim (``R1_TealMark``), six small
   contact pins joined into one connected hexagon outline (``ContactHex``), soft teal
   charging rim ring (``RimGlow``), bright weld beads (``WeldBeads``), a cut ``"07"``
   tag (``Tag_07``), and a cropped Tycho decal (``Decal_Tycho_1``).
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Sequence

import bmesh
import bpy
from mathutils import Euler, Quaternion, Vector

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))

import r1kit  # noqa: E402


UNLOCK_MATERIAL_SPECS: dict[str, dict[str, object]] = {
    # Salvaged VIS / Tycho chalked white enamel / composite
    "R1_WhitePaint": {
        "base_color": (0.87, 0.86, 0.83, 1.0),
        "wear_color": (0.55, 0.56, 0.58, 1.0),
        "dust_color": (0.58, 0.56, 0.52, 1.0),
        "metallic": 0.06,
        "roughness": 0.55,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    # Muted Tycho teal rim / trim
    "R1_TealMark": {
        "base_color": (0.18, 0.42, 0.44, 1.0),
        "wear_color": (0.46, 0.50, 0.52, 1.0),
        "dust_color": (0.48, 0.47, 0.44, 1.0),
        "metallic": 0.08,
        "roughness": 0.50,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
}


def register_unlock_materials() -> None:
    """Register R1_WhitePaint and R1_TealMark into r1kit at runtime without modifying r1kit.py."""
    for name, spec in UNLOCK_MATERIAL_SPECS.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = (*r1kit.ALLOWED_MATERIALS, name)
        r1kit.MATERIAL_SPECS[name] = spec


def _finalize_builder(
    mb: r1kit.MeshBuilder,
    parent: bpy.types.Object | None = None,
    smooth_angle_deg: float = 52.0,
) -> bpy.types.Object:
    """Triangulate any >4-vertex polygons on mb.bm and build the mesh object."""
    ngon_faces = [f for f in mb.bm.faces if f.is_valid and len(f.verts) > 4]
    if ngon_faces:
        bmesh.ops.triangulate(mb.bm, faces=ngon_faces, quad_method="BEAUTY", ngon_method="BEAUTY")
    return mb.build_object(parent=parent, smooth_angle_deg=smooth_angle_deg)


def _add_rippled_weld_seam(
    mb: r1kit.MeshBuilder,
    points_u: Sequence[tuple[float, float, float]],
    base_radius: float = 0.0085,
    ripples: int = 12,
    segments: int = 8,
    mat: str = "R1_BareSteel",
) -> list[bmesh.types.BMFace]:
    """Add a single continuous rippled caterpillar weld bead along points_u (1 closed manifold shell, >64 verts)."""
    raw_pts = [mb._to_local_b(p) for p in points_u]
    if len(raw_pts) < 2:
        return []

    # Subdivide path into dense steps so the weld caterpillar has organic ripples and >64 verts
    total_steps = max(14, ripples * 2)
    cum_len = [0.0]
    for i in range(1, len(raw_pts)):
        cum_len.append(cum_len[-1] + (raw_pts[i] - raw_pts[i - 1]).length)
    total_len = max(1e-5, cum_len[-1])

    pts: list[Vector] = []
    for step in range(total_steps + 1):
        target_d = (step / total_steps) * total_len
        seg_idx = 0
        for j in range(len(cum_len) - 1):
            if cum_len[j] <= target_d <= cum_len[j + 1]:
                seg_idx = j
                break
        span = max(1e-6, cum_len[seg_idx + 1] - cum_len[seg_idx])
        t_local = (target_d - cum_len[seg_idx]) / span
        pts.append(raw_pts[seg_idx].lerp(raw_pts[seg_idx + 1], t_local))

    rings: list[list[bmesh.types.BMVert]] = []
    prev_quat: Quaternion | None = None
    for idx, pt in enumerate(pts):
        if idx == 0:
            tangent = (pts[1] - pts[0]).normalized()
        elif idx == len(pts) - 1:
            tangent = (pts[-1] - pts[-2]).normalized()
        else:
            tangent = ((pts[idx] - pts[idx - 1]).normalized() + (pts[idx + 1] - pts[idx]).normalized()).normalized()

        if prev_quat is None:
            up_hint = "Y" if abs(tangent.y) < 0.9 else "X"
            quat = tangent.to_track_quat("Z", up_hint)
        else:
            prev_z = prev_quat @ Vector((0.0, 0.0, 1.0))
            quat = prev_z.rotation_difference(tangent) @ prev_quat
        prev_quat = quat

        u_frac = idx / total_steps
        taper = math.sin(math.pi * u_frac) ** 0.35
        ripple = 1.0 + 0.20 * math.sin(2.0 * math.pi * ripples * u_frac)
        r_cur = max(base_radius * 0.45, base_radius * taper * ripple)

        ring: list[bmesh.types.BMVert] = []
        for s in range(segments):
            ang = (2.0 * math.pi * s) / segments
            local_pt = Vector((r_cur * math.cos(ang), r_cur * math.sin(ang), 0.0))
            ring.append(mb.bm.verts.new(pt + (quat @ local_pt)))
        rings.append(ring)

    faces: list[bmesh.types.BMFace] = []
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for s in range(segments):
            sn = (s + 1) % segments
            faces.append(mb.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    # Cap start and end with a center pole so all cap faces are triangles (zero n-gons)
    start_pole = mb.bm.verts.new(pts[0] - (prev_quat @ Vector((0.0, 0.0, base_radius * 0.35))))
    end_pole = mb.bm.verts.new(pts[-1] + (prev_quat @ Vector((0.0, 0.0, base_radius * 0.35))))
    for s in range(segments):
        sn = (s + 1) % segments
        faces.append(mb.bm.faces.new([start_pole, rings[0][sn], rings[0][s]]))
        faces.append(mb.bm.faces.new([end_pole, rings[-1][s], rings[-1][sn]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def _build_tag_07_node(
    parent: bpy.types.Object,
    pivot_u: tuple[float, float, float],
    scale: float = 1.0,
    face_axis: str = "Z",
    rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    """Build a small cut sheet-metal '07' tag welded onto a home-built structure or kit piece.

    When ``face_axis == 'Z'``, the tag faces ``+Z`` (used on CrateRack and ReflectorBeacon).
    When ``face_axis == 'X'``, the tag faces ``+X`` outboard (used on Kit_HexAdapter).
    """
    mb = r1kit.MeshBuilder("Tag_07", pivot_u=pivot_u)
    px, py, pz = pivot_u
    s = scale

    def _place_box(
        u_right: float,
        v_up: float,
        w_out: float,
        su: float,
        sv: float,
        sw: float,
        mat: str,
        bevel: float = 0.001,
        extra_roll_deg: float = 0.0,
    ) -> None:
        if face_axis == "X":
            center = (px + w_out, py + v_up, pz + u_right)
            size = (sw, sv, su)
            r_deg = (rot_deg_u[0] + extra_roll_deg, rot_deg_u[1], rot_deg_u[2])
        else:
            center = (px + u_right, py + v_up, pz + w_out)
            size = (su, sv, sw)
            r_deg = (rot_deg_u[0], rot_deg_u[1], rot_deg_u[2] + extra_roll_deg)
        mb.add_bevel_box(
            center_u=center,
            size_u=size,
            mat=mat,
            bevel=min(bevel, min(size) * 0.28),
            segments=1,
            rot_deg_u=r_deg,
        )

    # 1. Cut steel backing coupon + rover cream enamel front plate + coral top edge strip
    _place_box(0.0, 0.0, 0.002 * s, 0.074 * s, 0.048 * s, 0.004 * s, "R1_BareSteel", bevel=0.0012 * s)
    _place_box(0.0, -0.002 * s, 0.005 * s, 0.068 * s, 0.040 * s, 0.004 * s, "R1_CreamEnamel", bevel=0.0012 * s)
    _place_box(0.0, 0.017 * s, 0.0055 * s, 0.068 * s, 0.007 * s, 0.0045 * s, "R1_OrangeStripe", bevel=0.0008 * s)

    # 2. Raised "0" glyph on the left half (u = -0.015 * s) and "7" glyph on the right half (u = +0.015 * s)
    # Constructed as two crisp stamped relief blocks so component count stays tiny (zero hole-pattern risk)
    # "0" block with recessed dark center window
    _place_box(-0.015 * s, -0.003 * s, 0.0080 * s, 0.018 * s, 0.024 * s, 0.003 * s, "R1_GreyMetal", bevel=0.0006 * s)
    _place_box(-0.015 * s, -0.003 * s, 0.0088 * s, 0.008 * s, 0.013 * s, 0.0025 * s, "R1_CreamEnamel", bevel=0.0004 * s)

    # "7" top bar + diagonal stem
    _place_box(0.014 * s, 0.006 * s, 0.0082 * s, 0.018 * s, 0.0055 * s, 0.0032 * s, "R1_GreyMetal", bevel=0.0006 * s)
    _place_box(
        0.014 * s,
        -0.004 * s,
        0.0080 * s,
        0.006 * s,
        0.021 * s,
        0.0030 * s,
        "R1_GreyMetal",
        bevel=0.0006 * s,
        extra_roll_deg=-20.0,
    )

    return _finalize_builder(mb, parent=parent)


def _center_uv_island(obj: bpy.types.Object) -> None:
    """Center the UV island of an emissive/lamp mesh around (0.5, 0.5) so R1Lit hotspot shaders glow cleanly."""
    if obj.type != "MESH" or obj.data is None:
        return
    me = obj.data
    uv_layer = me.uv_layers.get(r1kit.UV_MAP_NAME)
    if uv_layer is None:
        uv_layer = me.uv_layers.new(name=r1kit.UV_MAP_NAME)
    me.uv_layers.active = uv_layer
    me.uv_layers.active_index = 0

    verts = me.vertices
    loops = me.loops
    uv_data = uv_layer.data
    if len(uv_data) == 0:
        return

    xs = [verts[loops[i].vertex_index].co.x for i in range(len(loops))]
    ys = [verts[loops[i].vertex_index].co.y for i in range(len(loops))]
    zs = [verts[loops[i].vertex_index].co.z for i in range(len(loops))]
    span_x = max(xs) - min(xs)
    span_y = max(ys) - min(ys)
    span_z = max(zs) - min(zs)
    mid_x = 0.5 * (max(xs) + min(xs))
    mid_y = 0.5 * (max(ys) + min(ys))
    mid_z = 0.5 * (max(zs) + min(zs))

    # Pick the two axes with the largest span so the UV island fills [0.15, 0.85]^2 around (0.5, 0.5)
    axes = sorted([(span_x, "X", mid_x), (span_y, "Y", mid_y), (span_z, "Z", mid_z)], key=lambda item: item[0], reverse=True)
    span_u, ax_u, mid_u = axes[0]
    span_v, ax_v, mid_v = axes[1]
    span_u = max(span_u, 1e-4)
    span_v = max(span_v, 1e-4)

    def _coord(v_co: Vector, axis_name: str) -> float:
        if axis_name == "X":
            return float(v_co.x)
        if axis_name == "Y":
            return float(v_co.y)
        return float(v_co.z)

    for i in range(len(loops)):
        v_co = verts[loops[i].vertex_index].co
        u = 0.5 + ((_coord(v_co, ax_u) - mid_u) / span_u) * 0.68
        v = 0.5 + ((_coord(v_co, ax_v) - mid_v) / span_v) * 0.68
        uv_data[i].uv = (round(u, 5), round(v, 5))
    me.update()


# ==============================================================================
# 1. CRATE RACK (Assets/_Project/Art/R1/Models/Structures/CrateRack.fbx)
#    Envelope: 1.60 m W (X) x 1.20 m H (Y) x 0.90 m D (Z)
# ==============================================================================


def _build_salvaged_crate_shell(
    parent: bpy.types.Object,
    name: str,
    center_u: tuple[float, float, float],
    size_u: tuple[float, float, float],
    *,
    cut_side: str = "NONE",
    has_center_divider: bool = False,
) -> bpy.types.Object:
    """Build a salvaged VIS white-enamel crate shell with corner bumpers, faded stripe remnant, and cut flange."""
    cx, cy, cz = center_u
    wx, hy, dz = size_u
    hx = wx * 0.5
    hz = dz * 0.5
    y_bot = cy - hy * 0.5
    y_top = cy + hy * 0.5
    wall_t = 0.034
    floor_h = 0.044

    mb = r1kit.MeshBuilder(name, pivot_u=center_u)

    # Bottom tub floor + recessed dark interior liner
    mb.add_bevel_box(
        center_u=(cx, y_bot + floor_h * 0.5, cz),
        size_u=(wx - 0.02, floor_h, dz - 0.02),
        mat="R1_WhitePaint",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(cx, y_bot + floor_h + 0.008, cz),
        size_u=(wx - wall_t * 2.1, 0.018, dz - wall_t * 2.1),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )

    # Front (+Z) and Back (-Z) walls in chalked white enamel
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(cx, cy + 0.01, cz + sz * (hz - wall_t * 0.5)),
            size_u=(wx - 0.016, hy - 0.02, wall_t),
            mat="R1_WhitePaint",
            bevel=0.008,
            segments=1,
        )

    # Left (-X) and Right (+X) side walls (with raw bare-steel cut rim if cut_side matches)
    for sx, side_tag in ((-1.0, "LEFT"), (1.0, "RIGHT")):
        is_cut = cut_side == side_tag
        mb.add_bevel_box(
            center_u=(cx + sx * (hx - wall_t * 0.5), cy + 0.01, cz),
            size_u=(wall_t, hy - 0.02, dz - wall_t * 1.5),
            mat="R1_BareSteel" if is_cut else "R1_WhitePaint",
            bevel=0.006,
            segments=1,
        )

    # Corner bumper columns (`R1_GreyMetal`) on outer corners; bare steel angle bar on field-cut corners
    cw = 0.048
    for sx, side_tag in ((-1.0, "LEFT"), (1.0, "RIGHT")):
        is_cut = cut_side == side_tag
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(cx + sx * (hx - cw * 0.45), cy, cz + sz * (hz - cw * 0.45)),
                size_u=(cw, hy, cw),
                mat="R1_BareSteel" if is_cut else "R1_GreyMetal",
                bevel=0.006,
                segments=1,
            )

    # Salvaged VIS orange waist stripe remnant across front face (partially cut away on one side)
    stripe_w = wx * 0.58
    stripe_x_off = -wx * 0.12 if cut_side == "RIGHT" else (wx * 0.12 if cut_side == "LEFT" else -wx * 0.16)
    mb.add_bevel_box(
        center_u=(cx + stripe_x_off, y_bot + hy * 0.42, cz + hz + 0.004),
        size_u=(stripe_w, 0.038, 0.010),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )

    # Upper lip collar rim (`R1_GreyMetal`) where dust covers seat
    rim_h = 0.024
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(cx, y_top - rim_h * 0.5, cz + sz * (hz - wall_t * 0.5)),
            size_u=(wx, rim_h, wall_t + 0.010),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )

    # Central welded divider bulkhead for the wide upper crate (`Crate_2`) creating 2 distinct bays
    if has_center_divider:
        mb.add_bevel_box(
            center_u=(cx, cy + 0.01, cz),
            size_u=(0.044, hy - 0.01, dz - 0.02),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )

    return _finalize_builder(mb, parent=parent)


def _build_crate_lid_and_lamp(
    root: bpy.types.Object,
    slot_idx: int,
    slot_x: float,
    hinge_y: float,
    hinge_z: float,
    lid_w: float,
    lid_d: float,
    open_deg: float,
) -> None:
    """Build separate hinged lid node `Lid_<i>` (closed at Quaternion.identity) and `LatchLamp_<i>`."""
    lid_h = 0.042
    lid_cz = hinge_z + lid_d * 0.5 - 0.018
    lid_cy = hinge_y + lid_h * 0.45

    mb_lid = r1kit.MeshBuilder(f"Lid_{slot_idx}", pivot_u=(slot_x, hinge_y, hinge_z))
    # Main chamfered dust-cover lid in chalked VIS white enamel
    mb_lid.add_bevel_box(
        center_u=(slot_x, lid_cy, lid_cz),
        size_u=(lid_w, lid_h, lid_d),
        mat="R1_WhitePaint",
        bevel=0.010,
        segments=1,
        top_scale_xz=(0.94, 0.94),
    )
    # Two stiffening ribs on top of the lid (`R1_GreyMetal`)
    for rx_off in (-lid_w * 0.26, lid_w * 0.26):
        mb_lid.add_bevel_box(
            center_u=(slot_x + rx_off, lid_cy + lid_h * 0.5 + 0.004, lid_cz),
            size_u=(0.032, 0.012, lid_d * 0.76),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
        )
    # Rear hinge barrel at the pivot line
    mb_lid.add_cylinder(
        center_u=(slot_x, hinge_y, hinge_z),
        radius=0.018,
        length=lid_w * 0.68,
        mat="R1_BareSteel",
        axis_u="X",
        segments=8,
        bevel=0.003,
    )
    # Front latch tongue (`R1_BareSteel`) hanging down over the front rim when shut
    front_z = hinge_z + lid_d - 0.014
    mb_lid.add_bevel_box(
        center_u=(slot_x, hinge_y - 0.008, front_z + 0.008),
        size_u=(0.11, 0.048, 0.022),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
    )
    lid_obj = _finalize_builder(mb_lid, parent=root)

    # Small amber latch lamp (`LatchLamp_<i>`) mounted on the front latch bezel of the slot
    lamp_pivot = (slot_x + lid_w * 0.28, hinge_y - 0.038, front_z + 0.012)
    mb_lamp = r1kit.MeshBuilder(f"LatchLamp_{slot_idx}", pivot_u=lamp_pivot)
    mb_lamp.add_bevel_box(
        center_u=(lamp_pivot[0], lamp_pivot[1], lamp_pivot[2] - 0.005),
        size_u=(0.056, 0.036, 0.016),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    mb_lamp.add_bevel_box(
        center_u=(lamp_pivot[0], lamp_pivot[1], lamp_pivot[2] + 0.004),
        size_u=(0.040, 0.022, 0.012),
        mat="R1_LampAmber",
        bevel=0.003,
        segments=1,
    )
    lamp_obj = _finalize_builder(mb_lamp, parent=root)
    _center_uv_island(lamp_obj)

    if abs(open_deg) > 1e-3:
        # Positive Blender X rotation swings the +Y (Unity +Z) lid upward around its rear hinge
        lid_obj.rotation_euler = Euler((math.radians(open_deg), 0.0, 0.0), "XYZ")


def build_crate_rack() -> bpy.types.Object:
    """Build CrateRack (1.60 m W x 1.20 m H x 0.90 m D) with 3 salvaged VIS crates, 4 slots, 4 lids, and 4 latch lamps."""
    register_unlock_materials()
    root = r1kit.create_empty("CrateRack", pivot_u=(0.0, 0.0, 0.0))

    # 1. Open cream tube frame (`Frame`): exact outer envelope 1.60 m W x 1.20 m H x 0.90 m D
    mb_frame = r1kit.MeshBuilder("Frame", pivot_u=(0.0, 0.0, 0.0))
    for side in (-1.0, 1.0):
        sx = side * 0.75
        # Splayed steel base skids spanning z in [-0.45, +0.45] and x out to [-0.80, +0.80]
        mb_frame.add_bevel_box(
            center_u=(sx, 0.035, 0.0),
            size_u=(0.10, 0.046, 0.86),
            mat="R1_BareSteel",
            bevel=0.008,
            segments=1,
        )
        # Rubber ground foot pads at front and back of each skid (reaching y = 0.00, z = +-0.45, x = +-0.80)
        for sz in (-1.0, 1.0):
            mb_frame.add_bevel_box(
                center_u=(sx, 0.012, sz * 0.405),
                size_u=(0.10, 0.024, 0.090),
                mat="R1_Rubber",
                bevel=0.004,
                segments=1,
            )
        # Rear vertical cream tube post rising to y = 1.20 m
        mb_frame.add_cylinder(
            center_u=(sx, 0.62, -0.37),
            radius=0.034,
            length=1.16,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=12,
            bevel=0.006,
        )
        # Front lower vertical cream tube post rising to y = 0.66 m
        mb_frame.add_cylinder(
            center_u=(sx, 0.35, 0.37),
            radius=0.034,
            length=0.62,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=12,
            bevel=0.006,
        )
        # Angled upper side tube strut from front mid-post (0.64, +0.37) to rear top arch (1.16, -0.37)
        mb_frame.add_strut(
            p0_u=(sx, 0.64, 0.37),
            p1_u=(sx, 1.15, -0.37),
            width=0.056,
            thickness=0.056,
            mat="R1_CreamEnamel",
            bevel=0.008,
        )
        # Lower and upper side shelf rails (`R1_GreyMetal`)
        mb_frame.add_bevel_box(
            center_u=(sx, 0.12, 0.02),
            size_u=(0.064, 0.048, 0.78),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )
        mb_frame.add_bevel_box(
            center_u=(sx, 0.60, -0.04),
            size_u=(0.064, 0.048, 0.68),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )

    # Top header arch rail at y = 1.165 m (top at y = 1.20 m) + coral stripe
    mb_frame.add_bevel_box(
        center_u=(0.0, 1.165, -0.37),
        size_u=(1.56, 0.070, 0.076),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=1,
    )
    mb_frame.add_bevel_box(
        center_u=(0.0, 1.165, -0.330),
        size_u=(1.36, 0.036, 0.010),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )

    # Horizontal shelf crossbars supporting the 3 salvaged crates
    for cy_bar, cz_bar in ((0.12, 0.34), (0.12, -0.28), (0.60, 0.24), (0.60, -0.34)):
        mb_frame.add_bevel_box(
            center_u=(0.0, cy_bar, cz_bar),
            size_u=(1.52, 0.048, 0.056),
            mat="R1_CreamEnamel",
            bevel=0.007,
            segments=1,
        )
    # Central vertical support post tying lower and upper shelves together
    mb_frame.add_bevel_box(
        center_u=(0.0, 0.36, 0.03),
        size_u=(0.058, 0.50, 0.66),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    _finalize_builder(mb_frame, parent=root)

    # 2. Three salvaged VIS crate shells (`Crate_0`, `Crate_1` on lower shelf; double-bay `Crate_2` on upper shelf)
    _build_salvaged_crate_shell(
        root,
        "Crate_0",
        center_u=(-0.39, 0.31, 0.04),
        size_u=(0.66, 0.34, 0.64),
        cut_side="RIGHT",
    )
    _build_salvaged_crate_shell(
        root,
        "Crate_1",
        center_u=(0.39, 0.31, 0.04),
        size_u=(0.66, 0.34, 0.64),
        cut_side="LEFT",
    )
    _build_salvaged_crate_shell(
        root,
        "Crate_2",
        center_u=(0.0, 0.79, -0.05),
        size_u=(1.42, 0.34, 0.62),
        cut_side="NONE",
        has_center_divider=True,
    )

    # 3. Four slot anchor empties (`Slot_0..3`) inside the four bays
    slot_positions = [
        (-0.39, 0.30, 0.04),
        (0.39, 0.30, 0.04),
        (-0.36, 0.78, -0.05),
        (0.36, 0.78, -0.05),
    ]
    for idx, pos_u in enumerate(slot_positions):
        r1kit.create_empty(f"Slot_{idx}", parent=root, pivot_u=pos_u)

    # 4. Four separate hinged lid nodes (`Lid_0..3`) and amber latch lamps (`LatchLamp_0..3`)
    # All 4 lids sit at Quaternion.identity (closed) so the rack's outer bounds match the frame (1.60 x 1.20 x 0.90 m)
    # and can be rotated around local X in Unity to open/close per slot.
    _build_crate_lid_and_lamp(
        root,
        slot_idx=0,
        slot_x=-0.39,
        hinge_y=0.48,
        hinge_z=-0.26,
        lid_w=0.62,
        lid_d=0.62,
        open_deg=0.0,
    )
    _build_crate_lid_and_lamp(
        root,
        slot_idx=1,
        slot_x=0.39,
        hinge_y=0.48,
        hinge_z=-0.26,
        lid_w=0.62,
        lid_d=0.62,
        open_deg=0.0,
    )
    _build_crate_lid_and_lamp(
        root,
        slot_idx=2,
        slot_x=-0.36,
        hinge_y=0.96,
        hinge_z=-0.34,
        lid_w=0.65,
        lid_d=0.60,
        open_deg=0.0,
    )
    _build_crate_lid_and_lamp(
        root,
        slot_idx=3,
        slot_x=0.36,
        hinge_y=0.96,
        hinge_z=-0.34,
        lid_w=0.65,
        lid_d=0.60,
        open_deg=0.0,
    )

    # 5. Bright weld beads (`WeldBeads`) where 07 joined the salvaged VIS crate shells to the cream frame
    mb_welds = r1kit.MeshBuilder("WeldBeads", pivot_u=(0.0, 0.55, 0.15))
    weld_paths = [
        # Central seam where Crate_0 and Crate_1 are welded to the center support post
        [(-0.04, 0.16, 0.365), (-0.04, 0.31, 0.365), (-0.04, 0.45, 0.365)],
        [(0.04, 0.16, 0.365), (0.04, 0.31, 0.365), (0.04, 0.45, 0.365)],
        # Upper crate central divider weld bead
        [(0.0, 0.64, 0.265), (0.0, 0.79, 0.265), (0.0, 0.93, 0.265)],
        # Side frame mounting weld seams
        [(-0.72, 0.62, 0.24), (-0.64, 0.62, 0.26), (-0.54, 0.62, 0.26)],
        [(0.54, 0.62, 0.26), (0.64, 0.62, 0.26), (0.72, 0.62, 0.24)],
        [(-0.72, 0.14, 0.35), (-0.62, 0.14, 0.36), (-0.50, 0.14, 0.36)],
        [(0.50, 0.14, 0.36), (0.62, 0.14, 0.36), (0.72, 0.14, 0.35)],
    ]
    for path in weld_paths:
        _add_rippled_weld_seam(mb_welds, path, base_radius=0.011, ripples=10, segments=8, mat="R1_BareSteel")
    _finalize_builder(mb_welds, parent=root)

    # 6. Cut "07" tag (`Tag_07`) welded onto the front-left cream upright post
    _build_tag_07_node(
        root,
        pivot_u=(-0.75, 0.52, 0.402),
        scale=1.15,
        face_axis="Z",
        rot_deg_u=(0.0, 0.0, -5.0),
    )

    # 7. Half-cut VIS maker decals (`Decal_VIS_1..3`) on the three salvaged crate shells
    # Decal_VIS_1 on Crate_0 front face: right portion of VIS_STENCIL cut away at the center weld
    r1kit.add_maker_decal_node(
        name="Decal_VIS_1",
        parent=root,
        center_u=(-0.22, 0.31, 0.362),
        normal_u=(0.0, 0.0, 1.0),
        width=0.22,
        height=0.14,
        cell_key="VIS_STENCIL",
        u_crop=(0.0, 0.64),
        v_crop=(0.0, 1.0),
    )
    # Decal_VIS_2 on Crate_1 front face: left portion of VIS_1985 cut away at the center weld
    r1kit.add_maker_decal_node(
        name="Decal_VIS_2",
        parent=root,
        center_u=(0.23, 0.31, 0.362),
        normal_u=(0.0, 0.0, 1.0),
        width=0.18,
        height=0.20,
        cell_key="VIS_1985",
        u_crop=(0.34, 1.0),
        v_crop=(0.0, 1.0),
    )
    # Decal_VIS_3 on Crate_2 upper bay front face: bottom half cropped by the cut shell rim
    r1kit.add_maker_decal_node(
        name="Decal_VIS_3",
        parent=root,
        center_u=(-0.38, 0.77, 0.262),
        normal_u=(0.0, 0.0, 1.0),
        width=0.28,
        height=0.12,
        cell_key="VIS_STENCIL",
        u_crop=(0.0, 1.0),
        v_crop=(0.32, 1.0),
    )

    bpy.context.view_layer.update()
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    for idx in range(4):
        lamp_node = bpy.data.objects.get(f"LatchLamp_{idx}")
        if lamp_node is not None:
            _center_uv_island(lamp_node)
    r1kit.bake_vertex_colors(root, footprint_radius=0.95)
    bpy.context.view_layer.update()
    return root


# ==============================================================================
# 2. REFLECTOR BEACON (Assets/_Project/Art/R1/Models/Structures/ReflectorBeacon.fbx)
#    Envelope: 0.42 m W (X) x 1.30 m H (Y) x 0.42 m D (Z)
# ==============================================================================


def _build_corner_cube_node(
    parent: bpy.types.Object,
    pivot_u: tuple[float, float, float] = (0.0, 1.13, 0.06),
) -> bpy.types.Object:
    """Build `CornerCube`: a single faceted three-sided hollow (trihedral retroreflector) as 1 closed manifold shell.

    Three mutually perpendicular mirror planes meet at an inward-recessed central apex
    (`z_apex = 0.012 m`) inside a chamfered hexagonal optical rim (`z_front = 0.096 m`),
    with 3 crisp dihedral facet creases that catch and return light.
    """
    cx, cy, cz = pivot_u
    mb = r1kit.MeshBuilder("CornerCube", pivot_u=pivot_u)
    bm = mb.bm
    mat_idx = mb._mat_index("R1_BareSteel")

    r_outer = 0.096
    r_inner = 0.082
    z_front = cz + 0.036
    z_rim_in = cz + 0.028
    z_back = cz - 0.034
    z_apex = cz - 0.044  # Recessed 8.0 cm behind the front optical rim!

    # 6 vertices around the hexagonal optical rim (angles 30, 90, 150, 210, 270, 330 deg):
    # Dihedral valley creases run from the central apex to vertices 1 (90 deg), 3 (210 deg), and 5 (330 deg),
    # dividing the hollow into 3 large trihedral mirror facets!
    outer_front: list[bmesh.types.BMVert] = []
    outer_back: list[bmesh.types.BMVert] = []
    inner_rim: list[bmesh.types.BMVert] = []

    for k in range(6):
        ang = math.radians(30.0 + 60.0 * k)
        ux = cx + r_outer * math.cos(ang)
        uy = cy + r_outer * math.sin(ang)
        outer_front.append(bm.verts.new(mb._to_local_b((ux, uy, z_front))))
        outer_back.append(bm.verts.new(mb._to_local_b((ux * 0.92, uy * 0.92, z_back))))

        # Dihedral valley vertices (k = 1, 3, 5) sit slightly deeper than outer corner peaks (k = 0, 2, 4)
        # so the 3 mirror planes meet at ~90 deg dihedral angles inside the hollow
        r_k = r_inner * (0.86 if (k % 2 == 1) else 1.0)
        z_k = (z_rim_in - 0.016) if (k % 2 == 1) else z_rim_in
        ix = cx + r_k * math.cos(ang)
        iy = cy + r_k * math.sin(ang)
        inner_rim.append(bm.verts.new(mb._to_local_b((ix, iy, z_k))))

    v_apex = bm.verts.new(mb._to_local_b((cx, cy, z_apex)))
    v_back_center = bm.verts.new(mb._to_local_b((cx, cy, z_back - 0.008)))

    faces: list[bmesh.types.BMFace] = []
    for k in range(6):
        kn = (k + 1) % 6
        # Outer barrel side quad (back to front)
        faces.append(bm.faces.new([outer_back[k], outer_back[kn], outer_front[kn], outer_front[k]]))
        # Front chamfered rim quad (outer_front to inner_rim)
        faces.append(bm.faces.new([outer_front[k], outer_front[kn], inner_rim[kn], inner_rim[k]]))
        # Closed rear cap triangle
        faces.append(bm.faces.new([v_back_center, outer_back[kn], outer_back[k]]))

    # Three large recessed trihedral mirror quads meeting at v_apex:
    # Facet 0 (upper-right): [v_apex, inner_rim[5], inner_rim[0], inner_rim[1]]
    # Facet 1 (upper-left):  [v_apex, inner_rim[1], inner_rim[2], inner_rim[3]]
    # Facet 2 (lower):       [v_apex, inner_rim[3], inner_rim[4], inner_rim[5]]
    for k_start in (5, 1, 3):
        k_mid = (k_start + 1) % 6
        k_end = (k_start + 2) % 6
        faces.append(bm.faces.new([v_apex, inner_rim[k_start], inner_rim[k_mid], inner_rim[k_end]]))

    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for f in faces:
        f.material_index = mat_idx
        f.smooth = True

    obj = _finalize_builder(mb, parent=parent, smooth_angle_deg=24.0)
    _center_uv_island(obj)
    return obj


def build_reflector_beacon() -> bpy.types.Object:
    """Build ReflectorBeacon (0.42 m W x 1.30 m H x 0.42 m D) with 3 foot spikes, cream post, coral band, and 1 corner-cube hollow."""
    register_unlock_materials()
    root = r1kit.create_empty("ReflectorBeacon", pivot_u=(0.0, 0.0, 0.0))

    # 1. Three short steel foot spikes (`FootSpikes`) spanning 0.42 m W x 0.42 m D, resting at y = 0.00 m
    mb_spikes = r1kit.MeshBuilder("FootSpikes", pivot_u=(0.0, 0.08, 0.0))
    # Central base collar hub (`R1_GreyMetal`)
    mb_spikes.add_cylinder(
        center_u=(0.0, 0.14, 0.0),
        radius=0.064,
        length=0.14,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.008,
    )
    # 3 splayed foot legs chosen so max(|x|) = 0.21 m and max(|z|) = 0.21 m -> exact 0.42 m x 0.42 m footprint
    spike_angles_deg = (35.0, 145.0, 270.0)
    for ang_deg in spike_angles_deg:
        rad = math.radians(ang_deg)
        dx = math.cos(rad)
        dz = math.sin(rad)
        # Angled leg strut from central collar (r=0.04, y=0.15) out to spike shoulder (r=0.175, y=0.06)
        mb_spikes.add_strut(
            p0_u=(dx * 0.038, 0.15, dz * 0.038),
            p1_u=(dx * 0.175, 0.06, dz * 0.175),
            width=0.044,
            thickness=0.034,
            mat="R1_GreyMetal",
            bevel=0.005,
        )
        # Short vertical regolith spike driven into the ground (`y = 0.00..0.09 m`)
        sx = dx * 0.176
        sz = dz * 0.176
        mb_spikes.add_cylinder(
            center_u=(sx, 0.045, sz),
            radius=0.012,
            radius_top=0.034,
            length=0.090,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.003,
        )
    _finalize_builder(mb_spikes, parent=root)

    # 2. Slim cream post (`Post`) rising from y = 0.18 m to y = 0.96 m
    mb_post = r1kit.MeshBuilder("Post", pivot_u=(0.0, 0.56, 0.0))
    mb_post.add_oct_prism(
        center_u=(0.0, 0.56, 0.0),
        width_x=0.088,
        height_y=0.76,
        depth_z=0.088,
        chamfer=0.022,
        mat="R1_CreamEnamel",
        bevel=0.006,
        bevel_segments=1,
    )
    # Lower and upper machined joint collars (`R1_GreyMetal`)
    for cy_collar in (0.22, 0.91):
        mb_post.add_oct_prism(
            center_u=(0.0, cy_collar, 0.0),
            width_x=0.104,
            height_y=0.044,
            depth_z=0.104,
            chamfer=0.026,
            mat="R1_GreyMetal",
            bevel=0.005,
            bevel_segments=1,
        )
    _finalize_builder(mb_post, parent=root)

    # 3. Cap housing (`CapHousing`) from y = 0.93 m to y = 1.30 m with ONE coral band (`R1_OrangeStripe`)
    mb_cap = r1kit.MeshBuilder("CapHousing", pivot_u=(0.0, 1.12, 0.0))
    # Lower neck block carrying the single coral identification band (`y = 0.93..1.02 m`)
    mb_cap.add_oct_prism(
        center_u=(0.0, 0.975, 0.0),
        width_x=0.168,
        height_y=0.090,
        depth_z=0.158,
        chamfer=0.038,
        mat="R1_CreamEnamel",
        bevel=0.008,
        bevel_segments=1,
    )
    # Single coral band (`R1_OrangeStripe`) wrapping around the cap waist
    mb_cap.add_oct_prism(
        center_u=(0.0, 0.978, 0.0),
        width_x=0.176,
        height_y=0.056,
        depth_z=0.166,
        chamfer=0.040,
        mat="R1_OrangeStripe",
        bevel=0.005,
        bevel_segments=1,
    )
    # Main reflector hood housing (`y = 1.01..1.27 m`) in cream enamel
    mb_cap.add_oct_prism(
        center_u=(0.0, 1.135, -0.008),
        width_x=0.236,
        height_y=0.250,
        depth_z=0.184,
        chamfer=0.054,
        mat="R1_CreamEnamel",
        bevel=0.012,
        bevel_segments=2,
    )
    # Top sun-visor cap reaching exact y = 1.30 m
    mb_cap.add_oct_prism(
        center_u=(0.0, 1.272, 0.006),
        width_x=0.244,
        height_y=0.056,
        depth_z=0.204,
        chamfer=0.056,
        mat="R1_CreamEnamel",
        bevel=0.010,
        bevel_segments=1,
    )
    # Dark optical bezel ring around the front corner-cube aperture (`R1_GreyMetal`)
    mb_cap.add_oct_prism(
        center_u=(0.0, 1.13, 0.048),
        width_x=0.212,
        height_y=0.212,
        depth_z=0.088,
        chamfer=0.052,
        mat="R1_GreyMetal",
        bevel=0.008,
        bevel_segments=1,
    )
    _finalize_builder(mb_cap, parent=root)

    # 4. Single faceted 3-sided hollow corner-cube retroreflector (`CornerCube`)
    _build_corner_cube_node(root, pivot_u=(0.0, 1.13, 0.06))
    r1kit.create_empty("Reflector_Anchor", parent=root, pivot_u=(0.0, 1.13, 0.06))

    # 5. Bright weld beads (`WeldBeads`) where 07 welded the foot spikes and cap neck
    mb_welds = r1kit.MeshBuilder("WeldBeads", pivot_u=(0.0, 0.55, 0.0))
    # Ring weld around the upper post / cap neck junction
    neck_ring_pts = [
        (0.056 * math.cos(math.radians(a)), 0.93, 0.056 * math.sin(math.radians(a)))
        for a in range(-40, 225, 30)
    ]
    _add_rippled_weld_seam(mb_welds, neck_ring_pts, base_radius=0.009, ripples=12, segments=8, mat="R1_BareSteel")
    # Three leg-to-hub weld beads at the base collar
    for ang_deg in spike_angles_deg:
        rad = math.radians(ang_deg)
        nx = math.cos(rad)
        nz = math.sin(rad)
        tx = -nz
        tz = nx
        leg_weld = [
            (nx * 0.058 - tx * 0.024, 0.16, nz * 0.058 - tz * 0.024),
            (nx * 0.068, 0.145, nz * 0.068),
            (nx * 0.058 + tx * 0.024, 0.16, nz * 0.058 + tz * 0.024),
        ]
        _add_rippled_weld_seam(mb_welds, leg_weld, base_radius=0.0095, ripples=8, segments=8, mat="R1_BareSteel")
    _finalize_builder(mb_welds, parent=root)

    # 6. Cut "07" tag (`Tag_07`) welded onto the front of the cream post just below the coral band
    _build_tag_07_node(
        root,
        pivot_u=(0.0, 0.81, 0.046),
        scale=0.95,
        face_axis="Z",
        rot_deg_u=(0.0, 0.0, 4.0),
    )

    bpy.context.view_layer.update()
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    cc_node = bpy.data.objects.get("CornerCube")
    if cc_node is not None:
        _center_uv_island(cc_node)
    r1kit.bake_vertex_colors(root, footprint_radius=0.35)
    bpy.context.view_layer.update()
    return root


# ==============================================================================
# 3. HEX CHARGE ADAPTER (Assets/_Project/Art/R1/Models/Rover/Kit_HexAdapter.fbx)
#    Envelope: 0.046 m D (X) x 0.184 m H (Y) x 0.184 m W (Z)
#    Root (0,0,0) on inner mounting face (x=0), extending outboard along +X.
# ==============================================================================


def _append_regular_hex_prism(
    mb: r1kit.MeshBuilder,
    x_min: float,
    x_max: float,
    radius_yz: float,
    mat: str,
    bevel: float = 0.003,
) -> None:
    """Append a closed manifold regular hexagon prism in the Y-Z plane spanning x in [x_min, x_max]."""
    bm = mb.bm
    mat_idx = mb._mat_index(mat)
    # Angles 0, 60, 120, 180, 240, 300 deg in (Z, Y) so span_z = 2 * R and span_y = sqrt(3) * R,
    # or blend two orientations / scale slightly so both Y and Z spans are ~0.184 m
    pts_yz: list[tuple[float, float]] = []
    for k in range(6):
        ang = math.radians(30.0 + 60.0 * k)
        # Scale Y by 1.0 and Z by 1.0 / cos(30 deg) * 0.94 so the hex reads symmetric and ~0.184 m across
        py = radius_yz * math.sin(ang)
        pz = radius_yz * math.cos(ang) * 1.06
        pts_yz.append((py, pz))

    back_v = [bm.verts.new(mb._to_local_b((x_min, py, pz))) for py, pz in pts_yz]
    front_v = [bm.verts.new(mb._to_local_b((x_max, py, pz))) for py, pz in pts_yz]

    faces: list[bmesh.types.BMFace] = []
    faces.append(bm.faces.new(list(reversed(back_v))))
    faces.append(bm.faces.new(front_v))
    for k in range(6):
        kn = (k + 1) % 6
        faces.append(bm.faces.new([back_v[k], back_v[kn], front_v[kn], front_v[k]]))

    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for f in faces:
        f.material_index = mat_idx
        f.smooth = True

    edges = [e for f in faces for e in f.edges if e.is_valid]
    mb._bevel_edges(faces, edges, bevel, 1, mat)


def _append_hex_ring_manifold(
    mb: r1kit.MeshBuilder,
    x_min: float,
    x_max: float,
    r_inner: float,
    r_outer: float,
    mat: str,
    *,
    corner_pin_protrusion: float = 0.0,
    sub_segs_per_side: int = 3,
) -> None:
    """Append a single closed manifold hexagonal ring (with optional raised corner contact pins) in the Y-Z plane.

    By subdividing each of the 6 hex sides into ``sub_segs_per_side`` segments and sculpting the 6 corner
    vertices outward along +X and radially, the entire 6-pin hexagonal contact outline is a SINGLE connected
    closed manifold shell with >96 vertices (zero disconnected components -> zero hole-pattern false positives).
    """
    bm = mb.bm
    mat_idx = mb._mat_index(mat)

    corners_in: list[tuple[float, float]] = []
    corners_out: list[tuple[float, float]] = []
    for k in range(6):
        ang = math.radians(30.0 + 60.0 * k)
        sy = math.sin(ang)
        sz = math.cos(ang) * 1.06
        corners_in.append((r_inner * sy, r_inner * sz))
        corners_out.append((r_outer * sy, r_outer * sz))

    n_total = 6 * sub_segs_per_side
    ring_in_back: list[bmesh.types.BMVert] = []
    ring_out_back: list[bmesh.types.BMVert] = []
    ring_in_front: list[bmesh.types.BMVert] = []
    ring_out_front: list[bmesh.types.BMVert] = []

    for k in range(6):
        kn = (k + 1) % 6
        iy0, iz0 = corners_in[k]
        iy1, iz1 = corners_in[kn]
        oy0, oz0 = corners_out[k]
        oz1_y, oz1_z = corners_out[kn]

        for s in range(sub_segs_per_side):
            t = s / sub_segs_per_side
            # At s == 0 (each of the 6 hex vertices), bulge the contact pin forward in +X and slightly wider
            is_pin = (s == 0) and (corner_pin_protrusion > 0.0)
            pin_x = x_max + (corner_pin_protrusion if is_pin else 0.0)
            pin_scale_out = 1.12 if is_pin else 1.0
            pin_scale_in = 0.88 if is_pin else 1.0

            py_in = (iy0 * (1.0 - t) + iy1 * t) * pin_scale_in
            pz_in = (iz0 * (1.0 - t) + iz1 * t) * pin_scale_in
            py_out = (oy0 * (1.0 - t) + oz1_y * t) * pin_scale_out
            pz_out = (oz0 * (1.0 - t) + oz1_z * t) * pin_scale_out

            ring_in_back.append(bm.verts.new(mb._to_local_b((x_min, py_in, pz_in))))
            ring_out_back.append(bm.verts.new(mb._to_local_b((x_min, py_out, pz_out))))
            ring_in_front.append(bm.verts.new(mb._to_local_b((pin_x, py_in, pz_in))))
            ring_out_front.append(bm.verts.new(mb._to_local_b((pin_x, py_out, pz_out))))

    faces: list[bmesh.types.BMFace] = []
    for i in range(n_total):
        j = (i + 1) % n_total
        # Front face quad
        faces.append(bm.faces.new([ring_in_front[i], ring_out_front[i], ring_out_front[j], ring_in_front[j]]))
        # Back face quad
        faces.append(bm.faces.new([ring_out_back[i], ring_in_back[i], ring_in_back[j], ring_out_back[j]]))
        # Outer wall quad
        faces.append(bm.faces.new([ring_out_back[i], ring_out_back[j], ring_out_front[j], ring_out_front[i]]))
        # Inner wall quad
        faces.append(bm.faces.new([ring_in_front[i], ring_in_front[j], ring_in_back[j], ring_in_back[i]]))

    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for f in faces:
        f.material_index = mat_idx
        f.smooth = True


def build_kit_hex_adapter() -> bpy.types.Object:
    """Build Kit_HexAdapter (0.046 m D_x x 0.184 m H_y x 0.184 m W_z) for 07's flank socket."""
    register_unlock_materials()
    root = r1kit.create_empty("Kit_HexAdapter", pivot_u=(0.0, 0.0, 0.0))

    # 1. Flat hexagonal plate (`HexPlate`) in Tycho white composite (`R1_WhitePaint`) with muted teal rim (`R1_TealMark`)
    mb_plate = r1kit.MeshBuilder("HexPlate", pivot_u=(0.020, 0.0, 0.0))
    # Inner mounting flange (`x = 0.000..0.012 m`, `R1_GreyMetal`) that seats flush against 07's flank at x = 0
    _append_regular_hex_prism(mb_plate, x_min=0.000, x_max=0.012, radius_yz=0.086, mat="R1_GreyMetal", bevel=0.002)
    # Main Tycho white composite hexagonal plate (`x = 0.008..0.034 m`, radius 0.091 m -> 0.184 m across)
    _append_regular_hex_prism(mb_plate, x_min=0.008, x_max=0.034, radius_yz=0.090, mat="R1_WhitePaint", bevel=0.004)
    # Muted Tycho teal outer hexagonal bezel rim (`x = 0.026..0.040 m`, `R1_TealMark`)
    _append_hex_ring_manifold(
        mb_plate,
        x_min=0.026,
        x_max=0.040,
        r_inner=0.070,
        r_outer=0.088,
        mat="R1_TealMark",
        corner_pin_protrusion=0.0,
        sub_segs_per_side=3,
    )
    # Recessed dark insulator well (`x = 0.030..0.037 m`, `R1_GreyMetal`) inside the contact ring
    _append_regular_hex_prism(mb_plate, x_min=0.030, x_max=0.037, radius_yz=0.056, mat="R1_GreyMetal", bevel=0.002)
    _finalize_builder(mb_plate, parent=root)

    # 2. Six small contact pins arranged as ONE connected hexagon outline (`ContactHex`, `x = 0.034..0.046 m`)
    mb_contacts = r1kit.MeshBuilder("ContactHex", pivot_u=(0.040, 0.0, 0.0))
    _append_hex_ring_manifold(
        mb_contacts,
        x_min=0.034,
        x_max=0.041,
        r_inner=0.038,
        r_outer=0.050,
        mat="R1_Copper",
        corner_pin_protrusion=0.005,  # Reaches exact x_max = 0.046 m at the 6 corner contact pins!
        sub_segs_per_side=4,
    )
    _finalize_builder(mb_contacts, parent=root)

    # 3. Soft teal charging rim light channel (`RimGlow`, overridden to `R1_HexAdapterRimTeal.mat` in Unity)
    mb_glow = r1kit.MeshBuilder("RimGlow", pivot_u=(0.038, 0.0, 0.0))
    _append_hex_ring_manifold(
        mb_glow,
        x_min=0.033,
        x_max=0.042,
        r_inner=0.058,
        r_outer=0.069,
        mat="R1_TealMark",
        corner_pin_protrusion=0.0,
        sub_segs_per_side=3,
    )
    glow_obj = _finalize_builder(mb_glow, parent=root)
    _center_uv_island(glow_obj)

    # 4. Bright weld beads (`WeldBeads`) around the mounting perimeter where 07 field-welded the adapter
    mb_welds = r1kit.MeshBuilder("WeldBeads", pivot_u=(0.010, 0.0, 0.0))
    adapter_weld_paths = [
        [(0.006, 0.078, -0.036), (0.007, 0.088, 0.0), (0.006, 0.078, 0.036)],
        [(0.006, -0.078, -0.036), (0.007, -0.088, 0.0), (0.006, -0.078, 0.036)],
        [(0.006, -0.032, -0.086), (0.007, 0.0, -0.090), (0.006, 0.032, -0.086)],
    ]
    for path in adapter_weld_paths:
        _add_rippled_weld_seam(mb_welds, path, base_radius=0.0055, ripples=9, segments=8, mat="R1_BareSteel")
    _finalize_builder(mb_welds, parent=root)

    # 5. Small cut "07" tag (`Tag_07`) welded on the lower-aft corner of the hex plate facing outboard (+X)
    _build_tag_07_node(
        root,
        pivot_u=(0.034, -0.034, -0.026),
        scale=0.38,
        face_axis="X",
        rot_deg_u=(-8.0, 0.0, 0.0),
    )

    # 6. Cropped Tycho maker decal (`Decal_Tycho_1`) on the upper white composite face
    r1kit.add_maker_decal_node(
        name="Decal_Tycho_1",
        parent=root,
        center_u=(0.034, 0.034, 0.012),
        normal_u=(1.0, 0.0, 0.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.042,
        height=0.026,
        cell_key="TYCHO",
        offset=0.0012,
        thickness=0.0006,
        u_crop=(0.0, 0.78),
        v_crop=(0.15, 0.85),
    )

    bpy.context.view_layer.update()
    r1kit.apply_triplanar_uvs(root, texel_scale=2.2)
    _center_uv_island(glow_obj)
    r1kit.bake_vertex_colors(root, footprint_radius=0.18, include_ground_occluder=False)
    bpy.context.view_layer.update()
    return root


def build_all_unlocks(
    structures_dir: Path = Path("Assets/_Project/Art/R1/Models/Structures"),
    rover_dir: Path = Path("Assets/_Project/Art/R1/Models/Rover"),
) -> list[Path]:
    """Build and export CrateRack.fbx, ReflectorBeacon.fbx, and Kit_HexAdapter.fbx."""
    structures_dir = Path(structures_dir).resolve()
    rover_dir = Path(rover_dir).resolve()
    structures_dir.mkdir(parents=True, exist_ok=True)
    rover_dir.mkdir(parents=True, exist_ok=True)

    exported: list[Path] = []

    # 1. CrateRack.fbx
    r1kit.reset_scene()
    crate_rack = build_crate_rack()
    crate_rack_path = structures_dir / "CrateRack.fbx"
    print(f"[unlocks] CrateRack triangles: {r1kit.count_triangles(crate_rack)}")
    r1kit.export_fbx(crate_rack, crate_rack_path)
    exported.append(crate_rack_path)

    # 2. ReflectorBeacon.fbx
    r1kit.reset_scene()
    beacon = build_reflector_beacon()
    beacon_path = structures_dir / "ReflectorBeacon.fbx"
    print(f"[unlocks] ReflectorBeacon triangles: {r1kit.count_triangles(beacon)}")
    r1kit.export_fbx(beacon, beacon_path)
    exported.append(beacon_path)

    # 3. Kit_HexAdapter.fbx
    r1kit.reset_scene()
    hex_adapter = build_kit_hex_adapter()
    hex_adapter_path = rover_dir / "Kit_HexAdapter.fbx"
    print(f"[unlocks] Kit_HexAdapter triangles: {r1kit.count_triangles(hex_adapter)}")
    r1kit.export_fbx(hex_adapter, hex_adapter_path)
    exported.append(hex_adapter_path)

    return exported


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export the three R1 opening unlocks FBX models.")
    parser.add_argument(
        "--structures-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures"),
    )
    parser.add_argument(
        "--rover-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rover"),
    )
    args = parser.parse_args(argv)
    build_all_unlocks(structures_dir=args.structures_dir, rover_dir=args.rover_dir)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
