"""Shared helpers for R1 Lumen Station ruins models (headless Blender).

Note: Does NOT modify tools/art/blender/r1kit.py on disk. Dynamically registers
R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime, provides deterministic
UV mapping onto the 512x512 R1_Signage atlas, enhances vertex-color weathering
(top-surface dust settling + edge wear), and renders Night and Dawn 3/4 views.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402

RUINS_FBX_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Ruins"
RENDERS_DIR = REPO_ROOT / "Logs" / "renders"
SIGNAGE_ALBEDO_PATH = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Textures" / "T_R1_Signage_Albedo.png"

# UV rects (u0, v0, u1, v1) in OpenGL/Unity UV space (V=0 bottom, V=1 top)
# matching tools/art/r1/generate_signage_atlas.py
SIGNAGE_UV_CELLS: Dict[str, Tuple[float, float, float, float]] = {
    # Row 3 (Top row, V = 0.75..1.00)
    "LOGO": (0.020, 0.760, 0.230, 0.990),
    "LUMEN": (0.260, 0.760, 0.740, 0.990),
    "EVAC_1988": (0.765, 0.765, 0.985, 0.985),
    # Row 2 (Upper-middle row, V = 0.50..0.75)
    "SEC04_HABITAT": (0.020, 0.515, 0.480, 0.735),
    "SEC_04": (0.510, 0.520, 0.740, 0.730),
    "HABITAT_LAB": (0.760, 0.520, 0.985, 0.730),
    # Row 1 (Lower-middle row, V = 0.25..0.50)
    "WORKSHOP": (0.020, 0.265, 0.480, 0.485),
    "HAZARD": (0.510, 0.265, 0.990, 0.485),
    # Row 0 (Bottom row, V = 0.00..0.25)
    "PICT_ROVER": (0.020, 0.020, 0.230, 0.230),
    "PICT_RADIO": (0.270, 0.020, 0.480, 0.230),
    "PICT_BOTANY": (0.520, 0.020, 0.730, 0.230),
    "PICT_AIRLOCK": (0.770, 0.020, 0.980, 0.230),
}

# Per-object registered signage plates: obj_name -> list of (center_bl, u_dir_bl, v_dir_bl, width, height, cell_name)
_SIGNAGE_PLATES_BY_MB: Dict[int, List[Tuple[Vector, Vector, Vector, float, float, str]]] = {}
_SIGNAGE_PLATES_BY_OBJ: Dict[str, List[Tuple[Vector, Vector, Vector, float, float, str]]] = {}
_PATCHED_R1KIT = False


def _install_r1kit_ruins_extensions() -> None:
    """Install runtime helpers and material specs onto r1kit in memory without modifying r1kit.py on disk."""
    global _PATCHED_R1KIT

    extra_specs = {
        "R1_Screen": {
            "base_color": r1kit._srgb_to_linear("#0F241C"),
            "wear_color": r1kit._srgb_to_linear("#264438"),
            "dust_color": r1kit._srgb_to_linear("#5A6E64"),
            "metallic": 0.05,
            "roughness": 0.08,
            "emission": r1kit._srgb_to_linear("#33EB9E"),
            "emission_strength": 2.6,
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

    if _PATCHED_R1KIT:
        return
    _PATCHED_R1KIT = True

    # Convenience aliases on r1kit module
    if not hasattr(r1kit, "ensure_r1_materials"):
        r1kit.ensure_r1_materials = r1kit.ensure_materials
    if not hasattr(r1kit, "create_root"):
        r1kit.create_root = lambda name: r1kit.create_empty(name, pivot_u=(0.0, 0.0, 0.0))
    if not hasattr(r1kit, "add_empty"):
        r1kit.add_empty = lambda name, pos_u, parent=None: r1kit.create_empty(name, parent=parent, pivot_u=pos_u)

    orig_init = r1kit.MeshBuilder.__init__

    def _mb_init(self, name: str = "_Mesh", pivot_u: Tuple[float, float, float] = (0.0, 0.0, 0.0)) -> None:
        orig_init(self, name=name, pivot_u=pivot_u)

    r1kit.MeshBuilder.__init__ = _mb_init

    orig_bevel_box = r1kit.MeshBuilder.add_bevel_box

    def _mb_add_bevel_box(
        self,
        center_u: Tuple[float, float, float],
        size_u: Tuple[float, float, float],
        mat: str = "R1_GreyMetal",
        bevel: float = 0.015,
        segments: int = 2,
        rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
        top_scale_xz: Tuple[float, float] = (1.0, 1.0),
        segs: Optional[int] = None,
    ):
        if segs is not None:
            segments = segs
        return orig_bevel_box(
            self,
            center_u=center_u,
            size_u=size_u,
            mat=mat,
            bevel=bevel,
            segments=segments,
            rot_deg_u=rot_deg_u,
            top_scale_xz=top_scale_xz,
        )

    r1kit.MeshBuilder.add_bevel_box = _mb_add_bevel_box

    orig_cylinder = r1kit.MeshBuilder.add_cylinder

    def _mb_add_cylinder(
        self,
        center_u: Tuple[float, float, float],
        radius: float,
        length: Optional[float] = None,
        mat: str = "R1_GreyMetal",
        axis_u: str = "Y",
        segments: int = 16,
        bevel: float = 0.01,
        bevel_segments: int = 1,
        radius_top: Optional[float] = None,
        rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
        height: Optional[float] = None,
        segs: Optional[int] = None,
        axis: Optional[str] = None,
    ):
        if height is not None:
            length = height
        if length is None:
            length = 0.1
        if segs is not None:
            segments = segs
        if axis is not None:
            axis_u = axis.upper()
        return orig_cylinder(
            self,
            center_u=center_u,
            radius=radius,
            length=length,
            mat=mat,
            axis_u=axis_u,
            segments=segments,
            bevel=bevel,
            bevel_segments=bevel_segments,
            radius_top=radius_top,
            rot_deg_u=rot_deg_u,
        )

    r1kit.MeshBuilder.add_cylinder = _mb_add_cylinder

    def _mb_add_ring(
        self,
        center_u: Tuple[float, float, float],
        r_outer: float,
        r_inner: float,
        height: float,
        segs: int = 16,
        axis: str = "y",
        mat: str = "R1_BareSteel",
    ):
        """Add a closed, manifold quad annulus ring around Unity axis ('x', 'y', or 'z')."""
        axis_u = axis.upper()
        hz = height * 0.5
        ro = max(r_outer, r_inner + 0.004)
        ri = max(0.002, min(r_inner, ro - 0.004))

        bot_in = []
        bot_out = []
        top_in = []
        top_out = []
        for i in range(segs):
            ang = (2.0 * math.pi * i) / segs
            ca = math.cos(ang)
            sa = math.sin(ang)
            bot_in.append(self.bm.verts.new((ri * ca, ri * sa, -hz)))
            bot_out.append(self.bm.verts.new((ro * ca, ro * sa, -hz)))
            top_in.append(self.bm.verts.new((ri * ca, ri * sa, hz)))
            top_out.append(self.bm.verts.new((ro * ca, ro * sa, hz)))

        faces = []
        for i in range(segs):
            j = (i + 1) % segs
            faces.append(self.bm.faces.new([bot_out[i], bot_out[j], top_out[j], top_out[i]]))
            faces.append(self.bm.faces.new([bot_in[j], bot_in[i], top_in[i], top_in[j]]))
            faces.append(self.bm.faces.new([top_in[i], top_out[i], top_out[j], top_in[j]]))
            faces.append(self.bm.faces.new([bot_out[i], bot_in[i], bot_in[j], bot_out[j]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        all_verts = bot_in + bot_out + top_in + top_out
        if axis_u == "X":
            align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
        elif axis_u == "Z":
            align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
        else:
            align_mat = Matrix.Identity(3)

        bmesh.ops.rotate(self.bm, verts=all_verts, cent=(0.0, 0.0, 0.0), matrix=align_mat)
        bmesh.ops.translate(self.bm, verts=all_verts, vec=self._to_local_b(center_u))
        return faces

    r1kit.MeshBuilder.add_ring = _mb_add_ring

    def _mb_add_detail_bolt(
        self,
        center_u: Tuple[float, float, float],
        radius: float = 0.016,
        height: float = 0.012,
        axis: str = "y",
        mat: str = "R1_BareSteel",
    ) -> None:
        self.add_bolts([center_u], radius=radius, height=height, axis_u=axis.upper(), mat=mat)

    r1kit.MeshBuilder.add_Detail_Bolt = _mb_add_detail_bolt


def register_ruins_materials() -> None:
    """Register R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime without editing r1kit.py."""
    _install_r1kit_ruins_extensions()


def reset_ruins_scene() -> None:
    """Reset the Blender scene and register all 15 R1 materials including R1_Signage."""
    _SIGNAGE_PLATES_BY_MB.clear()
    _SIGNAGE_PLATES_BY_OBJ.clear()
    _install_r1kit_ruins_extensions()
    r1kit.reset_scene()


def add_signage_plate(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    width: float,
    height: float,
    cell_name: str,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    thickness: float = 0.012,
    frame_mat: Optional[str] = "R1_GreyMetal",
    rivets: bool = True,
) -> None:
    """Add a stencilled signage plate (with optional backing frame + corner rivets) and record its UV cell."""
    if cell_name not in SIGNAGE_UV_CELLS:
        raise ValueError(f"Unknown signage cell '{cell_name}'. Valid: {sorted(SIGNAGE_UV_CELLS.keys())}")

    if frame_mat is not None:
        mb.add_bevel_box(
            center_u,
            (width + 0.036, height + 0.036, thickness * 0.65),
            bevel=0.004,
            mat=frame_mat,
            rot_deg_u=rot_deg_u,
            segs=1,
        )

    # Match MeshBuilder.add_bevel_box rotation convention:
    # rot_b = Euler((rad(rx), rad(rz), rad(-ry)), "XYZ") in Blender space
    # In Unity coordinates, local +Z is (0, 1, 0) in Blender and local +Y is (0, 0, 1) in Blender.
    rot_b = Euler(
        (
            math.radians(rot_deg_u[0]),
            math.radians(rot_deg_u[2]),
            math.radians(-rot_deg_u[1]),
        ),
        "XYZ",
    ).to_matrix()

    n_bl = (rot_b @ Vector((0.0, 1.0, 0.0))).normalized()
    v_bl = (rot_b @ Vector((0.0, 0.0, 1.0))).normalized()
    u_bl = v_bl.cross(n_bl).normalized()

    # Convert n_bl, v_bl, u_bl back to Unity space (x_bl, z_bl, y_bl) for offset placement
    front_u = Vector((n_bl.x, n_bl.z, n_bl.y))
    up_u = Vector((v_bl.x, v_bl.z, v_bl.y))
    right_u = Vector((u_bl.x, u_bl.z, u_bl.y))

    plate_center_u = Vector(center_u) + front_u * (thickness * 0.28)
    mb.add_bevel_box(
        (plate_center_u.x, plate_center_u.y, plate_center_u.z),
        (width, height, thickness * 0.85),
        bevel=0.002,
        mat="R1_Signage",
        rot_deg_u=rot_deg_u,
        segs=1,
    )

    if rivets and width >= 0.28 and height >= 0.14:
        dx = width * 0.5 - 0.022
        dy = height * 0.5 - 0.022
        for sx in (-1.0, 1.0):
            for sy in (-1.0, 1.0):
                rc = plate_center_u + right_u * (sx * dx) + up_u * (sy * dy) + front_u * (thickness * 0.45)
                mb.add_Detail_Bolt(
                    (rc.x, rc.y, rc.z),
                    radius=0.009,
                    height=0.006,
                    axis="z",
                    mat="R1_BareSteel",
                )

    c_bl = r1kit.u_pos(plate_center_u.x, plate_center_u.y, plate_center_u.z)
    _SIGNAGE_PLATES_BY_MB.setdefault(id(mb), []).append((c_bl, u_bl, v_bl, width, height, cell_name))


def to_ruin_object(
    mb: r1kit.MeshBuilder,
    name: str,
    parent: Optional[bpy.types.Object] = None,
    origin_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    shade_smooth: bool = True,
) -> bpy.types.Object:
    """Convert a MeshBuilder to a Blender object and transfer any registered signage plate records."""
    _ = shade_smooth
    mb.name = name
    mb.pivot_u = origin_u
    mb.pivot_b = r1kit.u_pos(*origin_u)
    plates = _SIGNAGE_PLATES_BY_MB.pop(id(mb), None)
    obj = mb.build_object(parent=parent)
    if plates:
        o_vec = r1kit.u_pos(*origin_u)
        shifted = [(c_bl - o_vec, u_bl, v_bl, w, h, cell) for (c_bl, u_bl, v_bl, w, h, cell) in plates]
        _SIGNAGE_PLATES_BY_OBJ[obj.name] = shifted
    return obj


def apply_signage_uvs(root: bpy.types.Object) -> None:
    """Map all R1_Signage faces under `root` onto their registered SIGNAGE_UV_CELLS rectangles."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        mat_names = [m.name if m else "" for m in mesh.materials]
        if "R1_Signage" not in mat_names:
            continue
        signage_mat_idx = mat_names.index("R1_Signage")
        plates = _SIGNAGE_PLATES_BY_OBJ.get(obj.name, [])
        if not plates:
            continue

        uv_layer = mesh.uv_layers.get("UVMap")
        if uv_layer is None:
            uv_layer = mesh.uv_layers.new(name="UVMap")
        uv_data = uv_layer.data

        for poly in mesh.polygons:
            if poly.material_index != signage_mat_idx:
                continue
            p_center = poly.center
            best_plate = min(plates, key=lambda pl: (p_center - pl[0]).length_squared)
            c_bl, u_bl, v_bl, width, height, cell_name = best_plate
            u0, v0, u1, v1 = SIGNAGE_UV_CELLS[cell_name]

            for li in poly.loop_indices:
                vi = mesh.loops[li].vertex_index
                co = mesh.vertices[vi].co
                rel = co - c_bl
                s = (rel.dot(u_bl) / max(1e-5, width)) + 0.5
                t = (rel.dot(v_bl) / max(1e-5, height)) + 0.5
                s = max(0.02, min(0.98, s))
                t = max(0.02, min(0.98, t))
                u = u0 + s * (u1 - u0)
                v = v0 + t * (v1 - v0)
                uv_data[li].uv = (round(u, 5), round(v, 5))
        mesh.update()


def bake_ruins_vertex_colors(root: bpy.types.Object, footprint_radius: float = 3.5) -> None:
    """Bake R1 vertex colors (R=AO, G=EdgeWear, B=RegolithDust, A=1) with heavier top dust & edge wear."""
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)

    objs = [o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and o.data is not None]
    z_min, z_max = 0.0, 2.5
    for obj in objs:
        mw = obj.matrix_world
        for v in obj.data.vertices:
            wz = (mw @ v.co).z
            if wz > z_max:
                z_max = wz
    z_span = max(0.5, z_max - z_min)

    for obj in objs:
        mesh = obj.data
        col_attr = mesh.color_attributes.get(r1kit.VCOL_ATTRIBUTE_NAME)
        if col_attr is None:
            continue

        mw = obj.matrix_world
        rot_mw = mw.to_3x3()

        bm = bmesh.new()
        bm.from_mesh(mesh)
        bm.verts.ensure_lookup_table()
        bm.edges.ensure_lookup_table()

        vert_top_dust = np.zeros(len(bm.verts), dtype=np.float32)
        vert_wear_boost = np.zeros(len(bm.verts), dtype=np.float32)

        for v in bm.verts:
            wpos = mw @ v.co
            wnorm = (rot_mw @ v.normal).normalized()
            up_factor = max(0.0, wnorm.z)
            height_01 = max(0.0, min(1.0, (wpos.z - z_min) / z_span))
            top_dust = (up_factor ** 1.35) * (0.58 + 0.24 * (1.0 - height_01))
            vert_top_dust[v.index] = top_dust

            convex_sum = 0.0
            cnt = 0
            for e in v.link_edges:
                if e.is_manifold:
                    ang = e.calc_face_angle_signed(0.0)
                    if ang > 0.06:
                        convex_sum += min(1.0, ang / 1.05)
                    cnt += 1
            wear = (convex_sum / max(1, cnt)) * 1.35
            if up_factor > 0.25:
                wear += 0.06
            vert_wear_boost[v.index] = max(0.0, min(1.0, wear))

        bm.free()

        num_loops = len(mesh.loops)
        rgba = np.ones(num_loops * 4, dtype=np.float32)
        col_attr.data.foreach_get("color", rgba)
        rgba = rgba.reshape(-1, 4)

        for poly in mesh.polygons:
            poly_wnorm = (rot_mw @ poly.normal).normalized()
            face_up_dust = (max(0.0, poly_wnorm.z) ** 1.4) * 0.52
            for li in poly.loop_indices:
                vi = mesh.loops[li].vertex_index
                r, g, b, _ = rgba[li]
                new_g = min(1.0, max(float(g), float(vert_wear_boost[vi])))
                new_b = min(1.0, max(float(b), float(vert_top_dust[vi]), float(face_up_dust)))
                rgba[li, 0] = max(0.0, min(1.0, float(r)))
                rgba[li, 1] = new_g
                rgba[li, 2] = new_b
                rgba[li, 3] = 1.0

        col_attr.data.foreach_set("color", rgba.reshape(-1))
        mesh.update()


def attach_signage_texture_for_render(use_vertex_color_shading: bool = True) -> None:
    """Configure R1 materials with vertex-color shading (fixing Blender 5.1 RGBA Mix sockets) and attach T_R1_Signage_Albedo.png."""
    r1kit.ensure_materials(use_vertex_color_shading=False)
    for name in r1kit.ALLOWED_MATERIALS:
        spec = r1kit.MATERIAL_SPECS[name]
        mat = bpy.data.materials.get(name)
        if mat is None or not mat.use_nodes:
            continue
        nt = mat.node_tree
        bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is None:
            continue

        if use_vertex_color_shading and float(spec["emission_strength"]) == 0.0:
            vcol = nt.nodes.new("ShaderNodeVertexColor")
            vcol.layer_name = r1kit.VCOL_ATTRIBUTE_NAME
            vcol.location = (-700, 0)

            sep = nt.nodes.new("ShaderNodeSeparateColor")
            sep.location = (-500, 0)
            nt.links.new(vcol.outputs["Color"], sep.inputs["Color"])

            # Mix base color with edge wear (G) using Blender 5.1 RGBA sockets (inputs[6]=A, inputs[7]=B, outputs[2]=Result)
            wear_mix = nt.nodes.new("ShaderNodeMix")
            wear_mix.data_type = "RGBA"
            wear_mix.location = (-280, 120)
            wear_mix.inputs[6].default_value = spec["base_color"]
            wear_mix.inputs[7].default_value = spec["wear_color"]

            wear_mul = nt.nodes.new("ShaderNodeMath")
            wear_mul.operation = "MULTIPLY"
            wear_mul.inputs[1].default_value = 0.28
            wear_mul.location = (-440, 120)
            nt.links.new(sep.outputs["Green"], wear_mul.inputs[0])
            nt.links.new(wear_mul.outputs["Value"], wear_mix.inputs[0])

            # Mix with cavity/top dust (B)
            dust_mix = nt.nodes.new("ShaderNodeMix")
            dust_mix.data_type = "RGBA"
            dust_mix.location = (-120, 120)
            dust_mix.inputs[7].default_value = spec["dust_color"]
            nt.links.new(wear_mix.outputs[2], dust_mix.inputs[6])

            dust_mul = nt.nodes.new("ShaderNodeMath")
            dust_mul.operation = "MULTIPLY"
            dust_mul.inputs[1].default_value = 0.26
            dust_mul.location = (-280, -40)
            nt.links.new(sep.outputs["Blue"], dust_mul.inputs[0])
            nt.links.new(dust_mul.outputs["Value"], dust_mix.inputs[0])

            # Multiply by AO (R remapped to 0.52..1.0)
            ao_map = nt.nodes.new("ShaderNodeMapRange")
            ao_map.location = (-280, -200)
            ao_map.inputs["From Min"].default_value = 0.0
            ao_map.inputs["From Max"].default_value = 1.0
            ao_map.inputs["To Min"].default_value = 0.52
            ao_map.inputs["To Max"].default_value = 1.0
            nt.links.new(sep.outputs["Red"], ao_map.inputs["Value"])

            ao_comb = nt.nodes.new("ShaderNodeCombineColor")
            ao_comb.location = (-120, -200)
            nt.links.new(ao_map.outputs["Result"], ao_comb.inputs["Red"])
            nt.links.new(ao_map.outputs["Result"], ao_comb.inputs["Green"])
            nt.links.new(ao_map.outputs["Result"], ao_comb.inputs["Blue"])

            ao_mul = nt.nodes.new("ShaderNodeMix")
            ao_mul.data_type = "RGBA"
            ao_mul.blend_type = "MULTIPLY"
            ao_mul.inputs[0].default_value = 1.0
            ao_mul.location = (40, 120)
            nt.links.new(dust_mix.outputs[2], ao_mul.inputs[6])
            nt.links.new(ao_comb.outputs["Color"], ao_mul.inputs[7])
            nt.links.new(ao_mul.outputs[2], bsdf.inputs["Base Color"])

            if name == "R1_Signage" and SIGNAGE_ALBEDO_PATH.exists():
                tex_node = nt.nodes.new("ShaderNodeTexImage")
                tex_node.location = (-680, 320)
                tex_node.image = bpy.data.images.load(str(SIGNAGE_ALBEDO_PATH), check_existing=True)
                nt.links.new(tex_node.outputs["Color"], wear_mix.inputs[6])


def _configure_fast_cycles(scene: bpy.types.Scene, res_x: int, res_y: int) -> None:
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = False
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "OPTIX"
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        scene.cycles.device = "GPU"
    except Exception:
        pass
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.film_transparent = False
    scene.render.use_freestyle = True
    if scene.view_layers:
        vl = scene.view_layers[0]
        vl.use_freestyle = True
        fs = vl.freestyle_settings
        ls = fs.linesets[0] if fs.linesets else fs.linesets.new("R1_RuinsOutline")
        if ls.linestyle is None:
            ls.linestyle = bpy.data.linestyles.get("R1_RuinsLine") or bpy.data.linestyles.new("R1_RuinsLine")
        ls.select_silhouette = True
        ls.select_border = True
        ls.select_crease = True
        fs.crease_angle = math.radians(134.0)
        ls.linestyle.color = (0.08, 0.09, 0.12)
        ls.linestyle.thickness = 1.25
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"



def _frame_camera_on_root(
    cam_obj: bpy.types.Object,
    root: bpy.types.Object,
    cam_angle_deg: float,
    cam_elev_deg: float,
    distance_scale: float = 1.0,
) -> Tuple[Vector, float, float]:
    bbox_min = [1e9, 1e9, 1e9]
    bbox_max = [-1e9, -1e9, -1e9]
    for o in r1kit.collect_hierarchy(root):
        if o.type == "MESH":
            for v in o.bound_box:
                wv = o.matrix_world @ Vector(v)
                for i in range(3):
                    bbox_min[i] = min(bbox_min[i], wv[i])
                    bbox_max[i] = max(bbox_max[i], wv[i])

    center = Vector([(bbox_min[i] + bbox_max[i]) * 0.5 for i in range(3)])
    extents = [bbox_max[i] - bbox_min[i] for i in range(3)]
    max_dim = max(extents + [1.0])
    dist = max_dim * 1.58 * distance_scale

    az = math.radians(cam_angle_deg)
    el = math.radians(cam_elev_deg)
    cam_pos = Vector(
        (
            center.x + dist * math.cos(el) * math.sin(az),
            center.y - dist * math.cos(el) * math.cos(az),
            center.z + dist * math.sin(el),
        )
    )
    cam_obj.location = cam_pos
    direction = center - cam_pos
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return center, max_dim, bbox_min[2]


def _find_emissive_positions(root: bpy.types.Object) -> List[Tuple[Vector, str]]:
    """Find world-space centers of faces using R1_LampAmber or R1_Screen to place warm local night lights."""
    results: List[Tuple[Vector, str]] = []
    for o in r1kit.collect_hierarchy(root):
        if o.type != "MESH" or o.data is None:
            continue
        mesh = o.data
        mats = [m.name if m else "" for m in mesh.materials]
        for target_mat in ("R1_LampAmber", "R1_Screen"):
            if target_mat not in mats:
                continue
            midx = mats.index(target_mat)
            acc = Vector((0.0, 0.0, 0.0))
            cnt = 0
            for poly in mesh.polygons:
                if poly.material_index == midx:
                    acc += o.matrix_world @ poly.center
                    cnt += 1
            if cnt > 0:
                results.append((acc / cnt, target_mat))
    return results


def render_ruin_day_night(
    root: bpy.types.Object,
    slug: str,
    cam_angle_deg: float = 36.0,
    cam_elev_deg: float = 22.0,
    res: Tuple[int, int] = (1024, 768),
) -> Tuple[Path, Path]:
    """Render both `<slug>_dawn.png` and `<slug>_night.png` three-quarter previews into `Logs/renders/`."""
    RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    attach_signage_texture_for_render(use_vertex_color_shading=True)

    scene = bpy.context.scene
    _configure_fast_cycles(scene, res[0], res[1])

    cam_data = bpy.data.cameras.new("RuinPreviewCam")
    cam_data.lens = 48.0
    cam_obj = bpy.data.objects.new("RuinPreviewCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    center, max_dim, min_z = _frame_camera_on_root(cam_obj, root, cam_angle_deg, cam_elev_deg)

    # Lunar regolith ground plane
    bpy.ops.mesh.primitive_plane_add(size=max_dim * 9.0, location=(center.x, center.y, min(0.0, min_z)))
    ground = bpy.context.active_object
    ground.name = "_RuinPreviewGround"
    gmat = bpy.data.materials.new("_RuinGroundMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.20, 0.21, 0.24, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.92
    ground.data.materials.append(gmat)

    world = bpy.data.worlds.new("_RuinPreviewWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")

    sun_data = bpy.data.lights.new("SunKey", type="SUN")
    sun_obj = bpy.data.objects.new("SunKey", sun_data)
    scene.collection.objects.link(sun_obj)

    fill_data = bpy.data.lights.new("SkyFill", type="SUN")
    fill_obj = bpy.data.objects.new("SkyFill", fill_data)
    scene.collection.objects.link(fill_obj)

    rim_data = bpy.data.lights.new("RimLight", type="SUN")
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    scene.collection.objects.link(rim_obj)

    # 1. DAWN RENDER (low grazing warm polar dawn sun, long crisp shadows, slate-blue sky fill)
    if bg:
        bg.inputs["Color"].default_value = (0.14, 0.16, 0.22, 1.0)
        bg.inputs["Strength"].default_value = 0.65

    sun_data.energy = 4.8
    sun_data.color = (1.0, 0.78, 0.56)
    sun_data.angle = math.radians(1.2)
    sun_obj.rotation_euler = (math.radians(78.0), math.radians(8.0), math.radians(-42.0))

    fill_data.energy = 1.15
    fill_data.color = (0.62, 0.74, 0.95)
    fill_data.angle = math.radians(25.0)
    fill_obj.rotation_euler = (math.radians(48.0), 0.0, math.radians(135.0))

    rim_data.energy = 1.4
    rim_data.color = (0.95, 0.86, 0.76)
    rim_obj.rotation_euler = (math.radians(72.0), 0.0, math.radians(160.0))

    dawn_path = RENDERS_DIR / f"{slug}_dawn.png"
    scene.render.filepath = str(dawn_path)
    bpy.ops.render.render(write_still=True)

    # 2. NIGHT RENDER (deep lunar indigo sky, cool steel-blue earthshine, warm local beacon/lamp pools)
    if bg:
        bg.inputs["Color"].default_value = (0.022, 0.034, 0.062, 1.0)
        bg.inputs["Strength"].default_value = 0.45

    gbsdf.inputs["Base Color"].default_value = (0.13, 0.15, 0.19, 1.0)

    sun_data.energy = 1.35
    sun_data.color = (0.52, 0.68, 0.96)
    sun_data.angle = math.radians(4.0)
    sun_obj.rotation_euler = (math.radians(58.0), 0.0, math.radians(-35.0))

    fill_data.energy = 0.65
    fill_data.color = (0.38, 0.52, 0.82)
    fill_obj.rotation_euler = (math.radians(42.0), 0.0, math.radians(125.0))

    rim_data.energy = 1.10
    rim_data.color = (0.58, 0.74, 1.0)
    rim_obj.rotation_euler = (math.radians(66.0), 0.0, math.radians(155.0))

    point_lights: List[bpy.types.Object] = []
    emissives = _find_emissive_positions(root)
    if not emissives:
        emissives = [(center + Vector((max_dim * 0.25, -max_dim * 0.25, max_dim * 0.25)), "R1_LampAmber")]

    for idx, (pos, mat_type) in enumerate(emissives):
        pdata = bpy.data.lights.new(f"_NightPt_{idx}", type="POINT")
        if mat_type == "R1_LampAmber":
            pdata.color = (1.0, 0.68, 0.26)
            pdata.energy = max(45.0, max_dim * 18.0)
            pdata.shadow_soft_size = 0.35
        else:
            pdata.color = (0.35, 0.95, 0.68)
            pdata.energy = max(25.0, max_dim * 10.0)
            pdata.shadow_soft_size = 0.25
        pobj = bpy.data.objects.new(f"_NightPt_{idx}", pdata)
        cam_dir = (cam_obj.location - pos).normalized()
        pobj.location = pos + cam_dir * 0.35 + Vector((0.0, 0.0, 0.12))
        scene.collection.objects.link(pobj)
        point_lights.append(pobj)

    night_path = RENDERS_DIR / f"{slug}_night.png"
    scene.render.filepath = str(night_path)
    bpy.ops.render.render(write_still=True)

    for obj in [cam_obj, ground, sun_obj, fill_obj, rim_obj] + point_lights:
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.cameras.remove(cam_data)
    bpy.data.lights.remove(sun_data)
    bpy.data.lights.remove(fill_data)
    bpy.data.lights.remove(rim_data)
    bpy.data.materials.remove(gmat)
    bpy.data.worlds.remove(world)
    r1kit.ensure_materials(use_vertex_color_shading=False)

    return dawn_path, night_path


def finalize_ruin_and_export(
    root: bpy.types.Object,
    slug: str,
    fbx_name: str,
    footprint_radius: float = 3.5,
    cam_angle_deg: float = 36.0,
    cam_elev_deg: float = 22.0,
    res: Tuple[int, int] = (1024, 768),
    skip_renders: bool = False,
) -> Tuple[Path, Path, Path]:
    """Apply triplanar UVs, override R1_Signage UVs, bake ruins vertex colors, export FBX, and render Night/Dawn."""
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    apply_signage_uvs(root)
    bake_ruins_vertex_colors(root, footprint_radius=footprint_radius)

    fbx_path = RUINS_FBX_DIR / fbx_name
    r1kit.export_fbx(root, fbx_path)

    dawn_path = RENDERS_DIR / f"{slug}_dawn.png"
    night_path = RENDERS_DIR / f"{slug}_night.png"
    if not skip_renders:
        dawn_path, night_path = render_ruin_day_night(
            root,
            slug=slug,
            cam_angle_deg=cam_angle_deg,
            cam_elev_deg=cam_elev_deg,
            res=res,
        )
    return fbx_path, dawn_path, night_path
