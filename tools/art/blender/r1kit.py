"""Shared Blender 5.1 R1 modelling, vertex-colour baking, FBX export, and preview rendering kit."""

from __future__ import annotations

import datetime
import hashlib
import math
from pathlib import Path
from typing import Callable, Iterable, Sequence

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Quaternion, Vector

ALLOWED_MATERIALS: tuple[str, ...] = (
    "R1_CreamEnamel",
    "R1_OrangeStripe",
    "R1_GreyMetal",
    "R1_BareSteel",
    "R1_Rubber",
    "R1_GoldFoil",
    "R1_DarkGlass",
    "R1_Copper",
    "R1_SagePatch",
    "R1_LampAmber",
    "R1_SolarCell",
    "R1_Basalt",
    "R1_Brass",
)


def _srgb_to_linear(hex_str: str) -> tuple[float, float, float, float]:
    h = hex_str.lstrip("#")
    vals = [int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)]
    lin = [
        c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
        for c in vals
    ]
    return (lin[0], lin[1], lin[2], 1.0)


MATERIAL_SPECS: dict[str, dict[str, object]] = {
    "R1_CreamEnamel": {
        "base_color": _srgb_to_linear("#F2E3C6"),
        "wear_color": _srgb_to_linear("#43454E"),
        "dust_color": _srgb_to_linear("#8D8B86"),
        "metallic": 0.04,
        "roughness": 0.34,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_OrangeStripe": {
        "base_color": _srgb_to_linear("#FF8A5B"),
        "wear_color": _srgb_to_linear("#D9C7A8"),
        "dust_color": _srgb_to_linear("#8D8B86"),
        "metallic": 0.04,
        "roughness": 0.38,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_GreyMetal": {
        "base_color": _srgb_to_linear("#3E4048"),
        "wear_color": _srgb_to_linear("#7C8089"),
        "dust_color": _srgb_to_linear("#85868B"),
        "metallic": 0.68,
        "roughness": 0.44,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_BareSteel": {
        "base_color": _srgb_to_linear("#8D9199"),
        "wear_color": _srgb_to_linear("#C4C8D0"),
        "dust_color": _srgb_to_linear("#787A80"),
        "metallic": 0.86,
        "roughness": 0.30,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_Rubber": {
        "base_color": _srgb_to_linear("#23252B"),
        "wear_color": _srgb_to_linear("#484B54"),
        "dust_color": _srgb_to_linear("#828388"),
        "metallic": 0.02,
        "roughness": 0.84,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_GoldFoil": {
        "base_color": _srgb_to_linear("#E8A735"),
        "wear_color": _srgb_to_linear("#FFE08A"),
        "dust_color": _srgb_to_linear("#8C8370"),
        "metallic": 0.94,
        "roughness": 0.24,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_DarkGlass": {
        "base_color": _srgb_to_linear("#171922"),
        "wear_color": _srgb_to_linear("#3A3E4C"),
        "dust_color": _srgb_to_linear("#6E7078"),
        "metallic": 0.12,
        "roughness": 0.14,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_Copper": {
        "base_color": _srgb_to_linear("#DA8E48"),
        "wear_color": _srgb_to_linear("#F6BA78"),
        "dust_color": _srgb_to_linear("#847B72"),
        "metallic": 0.88,
        "roughness": 0.27,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_SagePatch": {
        "base_color": _srgb_to_linear("#78A582"),
        "wear_color": _srgb_to_linear("#454952"),
        "dust_color": _srgb_to_linear("#888A86"),
        "metallic": 0.12,
        "roughness": 0.42,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_LampAmber": {
        "base_color": _srgb_to_linear("#FFB547"),
        "wear_color": _srgb_to_linear("#FFD484"),
        "dust_color": _srgb_to_linear("#FFB547"),
        "metallic": 0.0,
        "roughness": 0.18,
        "emission": _srgb_to_linear("#FFB547"),
        "emission_strength": 4.2,
    },
    "R1_SolarCell": {
        "base_color": _srgb_to_linear("#2D225A"),
        "wear_color": _srgb_to_linear("#5B4C96"),
        "dust_color": _srgb_to_linear("#747284"),
        "metallic": 0.30,
        "roughness": 0.18,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_Basalt": {
        "base_color": _srgb_to_linear("#6B6E75"),
        "wear_color": _srgb_to_linear("#A3A8B2"),
        "dust_color": _srgb_to_linear("#757A89"),
        "metallic": 0.03,
        "roughness": 0.80,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_Brass": {
        "base_color": _srgb_to_linear("#DBB052"),
        "wear_color": _srgb_to_linear("#F6D686"),
        "dust_color": _srgb_to_linear("#8A806C"),
        "metallic": 0.90,
        "roughness": 0.25,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
}

VCOL_ATTRIBUTE_NAME = "Color"
FIXED_EXPORT_TIME = datetime.datetime(2026, 10, 15, 0, 0, 0)


def u_pos(x_right: float, y_up: float, z_fwd: float) -> Vector:
    """Convert Unity position (x_right, y_up, z_fwd) to Blender position (x_right, z_fwd, y_up)."""
    return Vector((float(x_right), float(z_fwd), float(y_up)))


def u_vec(dx_right: float, dy_up: float, dz_fwd: float) -> Vector:
    """Convert Unity direction vector (dx_right, dy_up, dz_fwd) to Blender vector."""
    return Vector((float(dx_right), float(dz_fwd), float(dy_up)))


def u_rot(rx_deg: float, ry_deg: float, rz_deg: float) -> Euler:
    """Convert Unity left-handed Euler angles (rx_deg, ry_deg, rz_deg) to Blender right-handed Euler."""
    return Euler(
        (
            math.radians(-float(rx_deg)),
            math.radians(-float(rz_deg)),
            math.radians(-float(ry_deg)),
        ),
        "XYZ",
    )


def reset_scene() -> bpy.types.Scene:
    """Reset Blender to a clean empty scene in metric units (metres)."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = 30
    scene.render.fps_base = 1.0
    return scene


def ensure_materials(use_vertex_color_shading: bool = False) -> dict[str, bpy.types.Material]:
    """Create or update the 11 allowed R1 materials."""
    mats: dict[str, bpy.types.Material] = {}
    for name in ALLOWED_MATERIALS:
        spec = MATERIAL_SPECS[name]
        mat = bpy.data.materials.get(name)
        if mat is None:
            mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()

        out_node = nt.nodes.new("ShaderNodeOutputMaterial")
        out_node.location = (400, 0)

        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        bsdf.location = (100, 0)
        bsdf.inputs["Base Color"].default_value = spec["base_color"]
        bsdf.inputs["Metallic"].default_value = float(spec["metallic"])
        bsdf.inputs["Roughness"].default_value = float(spec["roughness"])
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = spec["emission"]
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = float(spec["emission_strength"])

        if use_vertex_color_shading and float(spec["emission_strength"]) == 0.0:
            vcol = nt.nodes.new("ShaderNodeVertexColor")
            vcol.layer_name = VCOL_ATTRIBUTE_NAME
            vcol.location = (-700, 0)

            sep = nt.nodes.new("ShaderNodeSeparateColor")
            sep.location = (-500, 0)
            nt.links.new(vcol.outputs["Color"], sep.inputs["Color"])

            # Mix base color with edge wear (G)
            wear_mix = nt.nodes.new("ShaderNodeMix")
            wear_mix.data_type = "RGBA"
            wear_mix.location = (-280, 120)
            wear_mix.inputs["A"].default_value = spec["base_color"]
            wear_mix.inputs["B"].default_value = spec["wear_color"]

            wear_mul = nt.nodes.new("ShaderNodeMath")
            wear_mul.operation = "MULTIPLY"
            wear_mul.inputs[1].default_value = 0.45
            wear_mul.location = (-440, 120)
            nt.links.new(sep.outputs["Green"], wear_mul.inputs[0])
            nt.links.new(wear_mul.outputs["Value"], wear_mix.inputs["Factor"])

            # Mix with cavity dust (B)
            dust_mix = nt.nodes.new("ShaderNodeMix")
            dust_mix.data_type = "RGBA"
            dust_mix.location = (-120, 120)
            dust_mix.inputs["B"].default_value = spec["dust_color"]
            nt.links.new(wear_mix.outputs["Result"], dust_mix.inputs["A"])

            dust_mul = nt.nodes.new("ShaderNodeMath")
            dust_mul.operation = "MULTIPLY"
            dust_mul.inputs[1].default_value = 0.38
            dust_mul.location = (-280, -40)
            nt.links.new(sep.outputs["Blue"], dust_mul.inputs[0])
            nt.links.new(dust_mul.outputs["Value"], dust_mix.inputs["Factor"])

            # Multiply by AO (R remapped to 0.35..1.0)
            ao_map = nt.nodes.new("ShaderNodeMapRange")
            ao_map.location = (-280, -200)
            ao_map.inputs["From Min"].default_value = 0.0
            ao_map.inputs["From Max"].default_value = 1.0
            ao_map.inputs["To Min"].default_value = 0.32
            ao_map.inputs["To Max"].default_value = 1.0
            nt.links.new(sep.outputs["Red"], ao_map.inputs["Value"])

            ao_mul = nt.nodes.new("ShaderNodeMix")
            ao_mul.data_type = "RGBA"
            ao_mul.blend_type = "MULTIPLY"
            ao_mul.inputs["Factor"].default_value = 1.0
            ao_mul.location = (40, 120)
            nt.links.new(dust_mix.outputs["Result"], ao_mul.inputs["A"])
            nt.links.new(ao_map.outputs["Result"], ao_mul.inputs["B"])
            nt.links.new(ao_mul.outputs["Result"], bsdf.inputs["Base Color"])

        nt.links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])
        mats[name] = mat
    return mats


def create_empty(
    name: str,
    parent: bpy.types.Object | None = None,
    pivot_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    outward_dir_u: tuple[float, float, float] | None = None,
) -> bpy.types.Object:
    """Create an Empty object at Unity position pivot_u, optionally oriented so Unity +Z points along outward_dir_u."""
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.12
    bpy.context.collection.objects.link(obj)
    if parent is not None:
        bpy.context.view_layer.update()
        obj.parent = parent
        obj.location = u_pos(*pivot_u) - parent.matrix_world.translation
    else:
        obj.location = u_pos(*pivot_u)

    if outward_dir_u is not None:
        dir_b = u_vec(*outward_dir_u).normalized()
        up_axis = "Z" if abs(dir_b.z) < 0.98 else "X"
        obj.rotation_euler = dir_b.to_track_quat("Y", up_axis).to_euler("XYZ")
    return obj


def _unique(seq: Iterable) -> list:
    """Return unique elements in deterministic insertion order (independent of pointer hash)."""
    return list(dict.fromkeys(seq))


class MeshBuilder:
    """Procedural bmesh builder for a single named R1 mesh node in Unity-space coordinates."""

    def __init__(self, name: str, pivot_u: tuple[float, float, float] = (0.0, 0.0, 0.0)) -> None:
        self.name = name
        self.pivot_u = pivot_u
        self.pivot_b = u_pos(*pivot_u)
        self.bm = bmesh.new()
        self._mat_to_idx: dict[str, int] = {}
        self._idx_to_mat: list[str] = []

    def _mat_index(self, mat_name: str) -> int:
        if mat_name not in ALLOWED_MATERIALS:
            raise ValueError(f"Material '{mat_name}' is not in ALLOWED_MATERIALS")
        if mat_name not in self._mat_to_idx:
            idx = len(self._idx_to_mat)
            self._mat_to_idx[mat_name] = idx
            self._idx_to_mat.append(mat_name)
        return self._mat_to_idx[mat_name]

    def _to_local_b(self, pos_u: tuple[float, float, float]) -> Vector:
        return u_pos(*pos_u) - self.pivot_b

    def _assign_faces(self, faces: Iterable[bmesh.types.BMFace], mat_name: str) -> None:
        idx = self._mat_index(mat_name)
        for f in faces:
            if f.is_valid:
                f.material_index = idx
                f.smooth = True

    def _bevel_edges(
        self,
        faces: Iterable[bmesh.types.BMFace],
        edges: list[bmesh.types.BMEdge],
        offset: float,
        segments: int,
        mat: str,
    ) -> tuple[list[bmesh.types.BMFace], list[bmesh.types.BMVert]]:
        valid_edges = _unique(e for e in edges if e.is_valid)
        if offset > 0.0 and segments > 0 and valid_edges:
            b_res = bmesh.ops.bevel(
                self.bm,
                geom=valid_edges,
                offset=offset,
                offset_type="OFFSET",
                segments=segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )
            combined = list(faces) + [
                g for g in b_res.get("faces", []) if isinstance(g, bmesh.types.BMFace)
            ]
        else:
            combined = list(faces)
        valid_faces = _unique(f for f in combined if f.is_valid)
        valid_verts = _unique(v for f in valid_faces for v in f.verts if v.is_valid)
        if valid_verts:
            bmesh.ops.remove_doubles(self.bm, verts=valid_verts, dist=1e-5)
        valid_faces = _unique(f for f in valid_faces if f.is_valid)
        self._assign_faces(valid_faces, mat)
        valid_verts = _unique(v for f in valid_faces for v in f.verts if v.is_valid)
        return valid_faces, valid_verts

    def add_bevel_box(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float],
        mat: str,
        bevel: float = 0.015,
        segments: int = 2,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        top_scale_xz: tuple[float, float] = (1.0, 1.0),
    ) -> list[bmesh.types.BMFace]:
        """Add a bevelled box (or tapered frustum) in Unity coordinates."""
        wx, hy, dz = size_u
        res = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = res["verts"]
        for v in verts:
            v.co.x *= wx
            v.co.y *= dz
            v.co.z *= hy
            if v.co.z > 0.0:
                v.co.x *= top_scale_xz[0]
                v.co.y *= top_scale_xz[1]

        geom_faces = _unique(f for v in verts for f in v.link_faces if f.is_valid)
        geom_edges = _unique(e for f in geom_faces for e in f.edges if e.is_valid)
        self._assign_faces(geom_faces, mat)
        all_faces, all_verts = self._bevel_edges(geom_faces, geom_edges, bevel, segments, mat)

        rot_b = Euler(
            (
                math.radians(rot_deg_u[0]),
                math.radians(rot_deg_u[2]),
                math.radians(-rot_deg_u[1]),
            ),
            "XYZ",
        ).to_matrix()
        bmesh.ops.rotate(self.bm, verts=all_verts, cent=(0.0, 0.0, 0.0), matrix=rot_b)
        bmesh.ops.translate(self.bm, verts=all_verts, vec=self._to_local_b(center_u))
        return all_faces

    def add_cylinder(
        self,
        center_u: tuple[float, float, float],
        radius: float,
        length: float,
        mat: str,
        axis_u: str = "Y",
        segments: int = 16,
        bevel: float = 0.01,
        bevel_segments: int = 1,
        radius_top: float | None = None,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ) -> list[bmesh.types.BMFace]:
        """Add a bevelled cylinder or frustum aligned with Unity axis X, Y (up), or Z (forward)."""
        r_top = radius if radius_top is None else radius_top
        res = bmesh.ops.create_cone(
            self.bm,
            cap_ends=True,
            cap_tris=False,
            segments=segments,
            radius1=radius,
            radius2=r_top,
            depth=length,
        )
        verts = res["verts"]
        geom_faces = _unique(f for v in verts for f in v.link_faces if f.is_valid)
        self._assign_faces(geom_faces, mat)

        cap_edges = _unique(
            e
            for f in geom_faces
            if f.is_valid and abs(f.normal.z) > 0.85
            for e in f.edges
            if e.is_valid
        )
        geom_faces, verts = self._bevel_edges(geom_faces, cap_edges, bevel, bevel_segments, mat)

        if axis_u == "X":
            align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
        elif axis_u == "Z":
            align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
        else:
            align_mat = Matrix.Identity(3)

        rot_b = Euler(
            (
                math.radians(rot_deg_u[0]),
                math.radians(rot_deg_u[2]),
                math.radians(-rot_deg_u[1]),
            ),
            "XYZ",
        ).to_matrix()
        bmesh.ops.rotate(self.bm, verts=verts, cent=(0.0, 0.0, 0.0), matrix=rot_b @ align_mat)
        bmesh.ops.translate(self.bm, verts=verts, vec=self._to_local_b(center_u))
        return geom_faces

    def add_oct_prism(
        self,
        center_u: tuple[float, float, float],
        width_x: float,
        height_y: float,
        depth_z: float,
        chamfer: float,
        mat: str,
        bevel: float = 0.015,
        bevel_segments: int = 2,
        top_inset: float = 0.0,
    ) -> list[bmesh.types.BMFace]:
        """Add an octagonal prism (chamfered rectangle in X-Z) with bevelled top and bottom rims."""
        hx = width_x * 0.5
        hz = depth_z * 0.5
        hy = height_y * 0.5
        c = min(chamfer, hx * 0.85, hz * 0.85)

        pts_2d = [
            (-hx + c, -hz),
            (hx - c, -hz),
            (hx, -hz + c),
            (hx, hz - c),
            (hx - c, hz),
            (-hx + c, hz),
            (-hx, hz - c),
            (-hx, -hz + c),
        ]
        bot_verts = [self.bm.verts.new((x, z, -hy)) for x, z in pts_2d]
        top_verts = []
        for x, z in pts_2d:
            length_xz = math.hypot(x, z)
            scale = max(0.1, (length_xz - top_inset) / length_xz) if length_xz > 1e-5 else 1.0
            top_verts.append(self.bm.verts.new((x * scale, z * scale, hy)))

        faces: list[bmesh.types.BMFace] = []
        faces.append(self.bm.faces.new(list(reversed(bot_verts))))
        faces.append(self.bm.faces.new(top_verts))
        n = len(pts_2d)
        for i in range(n):
            j = (i + 1) % n
            faces.append(
                self.bm.faces.new([bot_verts[i], bot_verts[j], top_verts[j], top_verts[i]])
            )
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        edges = _unique(e for f in faces for e in f.edges if e.is_valid)
        faces, verts = self._bevel_edges(faces, edges, bevel, bevel_segments, mat)
        bmesh.ops.translate(self.bm, verts=verts, vec=self._to_local_b(center_u))
        return faces

    def add_arch_housing(
        self,
        base_center_u: tuple[float, float, float],
        width_x: float,
        height_y: float,
        depth_z: float,
        arch_radius: float,
        mat: str,
        bevel: float = 0.02,
        arch_segments: int = 10,
        arch_axis_u: str = "Z",
    ) -> list[bmesh.types.BMFace]:
        """Add a rounded-top bollard/cabinet shell whose top arches across Z (front-to-back) or X (left-to-right)."""
        hx = width_x * 0.5
        hz = depth_z * 0.5
        half_span = hz if arch_axis_u == "Z" else hx
        r = min(arch_radius, half_span * 0.92)
        straight_h = max(0.05, height_y - r)

        profile_2d: list[tuple[float, float]] = [
            (-half_span, 0.0),
            (half_span, 0.0),
            (half_span, straight_h),
        ]
        for i in range(1, arch_segments):
            theta = (math.pi * 0.5) * (i / arch_segments)
            ps = (half_span - r) + r * math.cos(theta)
            py = straight_h + r * math.sin(theta)
            profile_2d.append((ps, py))
        profile_2d.append((half_span - r, height_y))
        profile_2d.append((-half_span + r, height_y))
        for i in range(1, arch_segments):
            theta = (math.pi * 0.5) * (i / arch_segments)
            ps = (-half_span + r) - r * math.sin(theta)
            py = straight_h + r * math.cos(theta)
            profile_2d.append((ps, py))
        profile_2d.append((-half_span, straight_h))

        if arch_axis_u == "Z":
            side_neg = [self.bm.verts.new((-hx, ps, py)) for ps, py in profile_2d]
            side_pos = [self.bm.verts.new((hx, ps, py)) for ps, py in profile_2d]
        else:
            side_neg = [self.bm.verts.new((ps, -hz, py)) for ps, py in profile_2d]
            side_pos = [self.bm.verts.new((ps, hz, py)) for ps, py in profile_2d]

        faces: list[bmesh.types.BMFace] = []
        faces.append(self.bm.faces.new(list(reversed(side_neg))))
        faces.append(self.bm.faces.new(side_pos))
        n = len(profile_2d)
        for i in range(n):
            j = (i + 1) % n
            faces.append(
                self.bm.faces.new([side_neg[i], side_neg[j], side_pos[j], side_pos[i]])
            )
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        cap_edges = _unique(list(faces[0].edges) + list(faces[1].edges))
        faces, verts = self._bevel_edges(faces, cap_edges, bevel, 2, mat)
        bmesh.ops.translate(self.bm, verts=verts, vec=self._to_local_b(base_center_u))
        return faces

    def add_strut(
        self,
        p0_u: tuple[float, float, float],
        p1_u: tuple[float, float, float],
        width: float,
        thickness: float,
        mat: str,
        bevel: float = 0.005,
    ) -> list[bmesh.types.BMFace]:
        """Add a bevelled rectangular strut connecting Unity points p0_u and p1_u."""
        v0 = self._to_local_b(p0_u)
        v1 = self._to_local_b(p1_u)
        delta = v1 - v0
        length = delta.length
        if length < 1e-4:
            return []

        res = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = res["verts"]
        for v in verts:
            v.co.x *= width
            v.co.y *= thickness
            v.co.z *= length

        faces = _unique(f for v in verts for f in v.link_faces if f.is_valid)
        edges = _unique(e for f in faces for e in f.edges if e.is_valid)
        self._assign_faces(faces, mat)
        faces, verts = self._bevel_edges(
            faces,
            edges,
            min(bevel, min(width, thickness) * 0.3),
            1,
            mat,
        )

        direction = delta.normalized()
        up_axis = "Y" if abs(direction.y) < 0.95 else "X"
        rot_mat = direction.to_track_quat("Z", up_axis).to_matrix()
        mid = (v0 + v1) * 0.5
        bmesh.ops.rotate(self.bm, verts=verts, cent=(0.0, 0.0, 0.0), matrix=rot_mat)
        bmesh.ops.translate(self.bm, verts=verts, vec=mid)
        return faces

    def add_tube_path(
        self,
        points_u: Sequence[tuple[float, float, float]],
        radius: float,
        mat: str,
        segments: int = 12,
        cap_ends: bool = True,
    ) -> list[bmesh.types.BMFace]:
        """Sweep a circular tube along a 3D path of Unity coordinates."""
        pts = [self._to_local_b(p) for p in points_u]
        if len(pts) < 2:
            return []

        rings: list[list[bmesh.types.BMVert]] = []
        prev_quat: Quaternion | None = None
        for idx, pt in enumerate(pts):
            if idx == 0:
                tangent = (pts[1] - pts[0]).normalized()
            elif idx == len(pts) - 1:
                tangent = (pts[-1] - pts[-2]).normalized()
            else:
                t1 = (pts[idx] - pts[idx - 1]).normalized()
                t2 = (pts[idx + 1] - pts[idx]).normalized()
                tangent = (t1 + t2).normalized()

            if prev_quat is None:
                up_hint = "Y" if abs(tangent.y) < 0.9 else "X"
                quat = tangent.to_track_quat("Z", up_hint)
            else:
                prev_z = prev_quat @ Vector((0.0, 0.0, 1.0))
                rot_diff = prev_z.rotation_difference(tangent)
                quat = rot_diff @ prev_quat
            prev_quat = quat

            ring: list[bmesh.types.BMVert] = []
            for s in range(segments):
                ang = (2.0 * math.pi * s) / segments
                local_pt = Vector((radius * math.cos(ang), radius * math.sin(ang), 0.0))
                world_pt = pt + (quat @ local_pt)
                ring.append(self.bm.verts.new(world_pt))
            rings.append(ring)

        faces: list[bmesh.types.BMFace] = []
        for r_idx in range(len(rings) - 1):
            r0 = rings[r_idx]
            r1 = rings[r_idx + 1]
            for s in range(segments):
                sn = (s + 1) % segments
                faces.append(self.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

        if cap_ends:
            faces.append(self.bm.faces.new(list(reversed(rings[0]))))
            faces.append(self.bm.faces.new(rings[-1]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)
        return faces

    def add_pipe(
        self,
        p0_u: tuple[float, float, float],
        p1_u: tuple[float, float, float],
        radius: float,
        mat: str,
        segments: int = 10,
    ) -> list[bmesh.types.BMFace]:
        """Add a straight cylindrical pipe connecting Unity points p0_u and p1_u."""
        return self.add_tube_path([p0_u, p1_u], radius=radius, mat=mat, segments=segments, cap_ends=True)

    def add_bolts(
        self,
        positions_u: Sequence[tuple[float, float, float]],
        radius: float = 0.016,
        height: float = 0.012,
        axis_u: str = "Y",
        mat: str = "R1_BareSteel",
        bevel_segments: int = 1,
    ) -> None:
        """Add 6-sided hex bolts at the given Unity positions."""
        for pos in positions_u:
            self.add_cylinder(
                center_u=pos,
                radius=radius,
                length=height,
                mat=mat,
                axis_u=axis_u,
                segments=6,
                bevel=radius * 0.22 if bevel_segments > 0 else 0.0,
                bevel_segments=bevel_segments,
            )

    def add_curved_patch(
        self,
        center_y: float,
        height_y: float,
        cyl_radius: float,
        thickness: float,
        angle_center_deg: float,
        angle_span_deg: float,
        mat: str,
        arc_segments: int = 8,
        bevel: float = 0.006,
    ) -> list[bmesh.types.BMFace]:
        """Add a cylindrical-shell patch (or stripe band) around a vertical cylinder centered at (0, *, 0)."""
        hy = height_y * 0.5
        r_in = cyl_radius
        r_out = cyl_radius + thickness
        a0 = math.radians(angle_center_deg - angle_span_deg * 0.5)
        a1 = math.radians(angle_center_deg + angle_span_deg * 0.5)

        bot_in: list[bmesh.types.BMVert] = []
        bot_out: list[bmesh.types.BMVert] = []
        top_in: list[bmesh.types.BMVert] = []
        top_out: list[bmesh.types.BMVert] = []

        for i in range(arc_segments + 1):
            t = i / arc_segments
            ang = a0 + (a1 - a0) * t
            sx = math.sin(ang)
            cz = math.cos(ang)
            bot_in.append(self.bm.verts.new(self._to_local_b((r_in * sx, center_y - hy, r_in * cz))))
            bot_out.append(self.bm.verts.new(self._to_local_b((r_out * sx, center_y - hy, r_out * cz))))
            top_in.append(self.bm.verts.new(self._to_local_b((r_in * sx, center_y + hy, r_in * cz))))
            top_out.append(self.bm.verts.new(self._to_local_b((r_out * sx, center_y + hy, r_out * cz))))

        faces: list[bmesh.types.BMFace] = []
        for i in range(arc_segments):
            j = i + 1
            faces.append(self.bm.faces.new([bot_out[i], bot_out[j], top_out[j], top_out[i]]))
            faces.append(self.bm.faces.new([bot_in[j], bot_in[i], top_in[i], top_in[j]]))
            faces.append(self.bm.faces.new([top_in[i], top_out[i], top_out[j], top_in[j]]))
            faces.append(self.bm.faces.new([bot_out[i], bot_in[i], bot_in[j], bot_out[j]]))

        faces.append(self.bm.faces.new([bot_in[0], bot_out[0], top_out[0], top_in[0]]))
        faces.append(self.bm.faces.new([bot_out[-1], bot_in[-1], top_in[-1], top_out[-1]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        if bevel > 0.0:
            outer_edges = _unique(
                e
                for f in faces
                for e in f.edges
                if e.is_manifold and e.calc_face_angle(0.0) > math.radians(60.0)
            )
            if outer_edges:
                faces, _ = self._bevel_edges(
                    faces,
                    outer_edges,
                    min(bevel, thickness * 0.45),
                    1,
                    mat,
                )
        return faces

    def add_conical_panel(
        self,
        y_bot: float,
        y_top: float,
        r_bot: float,
        r_top: float,
        thickness: float,
        angle_center_deg: float,
        angle_span_deg: float,
        mat: str,
        arc_segments: int = 8,
        bevel: float = 0.006,
    ) -> list[bmesh.types.BMFace]:
        """Add a curved conical-frustum armor panel or window frame around the vertical Y axis."""
        a0 = math.radians(angle_center_deg - angle_span_deg * 0.5)
        a1 = math.radians(angle_center_deg + angle_span_deg * 0.5)

        bot_in: list[bmesh.types.BMVert] = []
        bot_out: list[bmesh.types.BMVert] = []
        top_in: list[bmesh.types.BMVert] = []
        top_out: list[bmesh.types.BMVert] = []

        for i in range(arc_segments + 1):
            t = i / arc_segments
            ang = a0 + (a1 - a0) * t
            sx = math.sin(ang)
            cz = math.cos(ang)
            bot_in.append(self.bm.verts.new(self._to_local_b((r_bot * sx, y_bot, r_bot * cz))))
            bot_out.append(
                self.bm.verts.new(self._to_local_b(((r_bot + thickness) * sx, y_bot, (r_bot + thickness) * cz)))
            )
            top_in.append(self.bm.verts.new(self._to_local_b((r_top * sx, y_top, r_top * cz))))
            top_out.append(
                self.bm.verts.new(self._to_local_b(((r_top + thickness) * sx, y_top, (r_top + thickness) * cz)))
            )

        faces: list[bmesh.types.BMFace] = []
        for i in range(arc_segments):
            j = i + 1
            faces.append(self.bm.faces.new([bot_out[i], bot_out[j], top_out[j], top_out[i]]))
            faces.append(self.bm.faces.new([bot_in[j], bot_in[i], top_in[i], top_in[j]]))
            faces.append(self.bm.faces.new([top_in[i], top_out[i], top_out[j], top_in[j]]))
            faces.append(self.bm.faces.new([bot_out[i], bot_in[i], bot_in[j], bot_out[j]]))

        faces.append(self.bm.faces.new([bot_in[0], bot_out[0], top_out[0], top_in[0]]))
        faces.append(self.bm.faces.new([bot_out[-1], bot_in[-1], top_in[-1], top_out[-1]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        if bevel > 0.0:
            outer_edges = _unique(
                e
                for f in faces
                for e in f.edges
                if e.is_manifold and e.calc_face_angle(0.0) > math.radians(55.0)
            )
            if outer_edges:
                faces, _ = self._bevel_edges(
                    faces,
                    outer_edges,
                    min(bevel, thickness * 0.42),
                    1,
                    mat,
                )
        return faces

    def add_rounded_panel(
        self,
        width_x: float,
        y_bot: float,
        y_top: float,
        r_bot: float,
        r_top: float,
        thickness: float,
        mat: str,
        z_front: float = 0.0,
        cyl_radius: float | None = None,
        yaw_deg: float = 0.0,
        corner_segments: int = 8,
        cols: int = 10,
        bevel: float = 0.010,
    ) -> list[bmesh.types.BMFace]:
        """Add a flat or cylindrical-shell panel with rounded top/bottom corners and clean all-quad rims."""
        _ = bevel
        hw = width_x * 0.5
        h_total = y_top - y_bot
        rb = max(0.0, min(r_bot, hw - 0.04, h_total * 0.46))
        rt = max(0.0, min(r_top, hw - 0.04, h_total * 0.46))

        def _build_grid(z_off: float) -> list[list[bmesh.types.BMVert]]:
            rows_yh: list[tuple[float, float]] = []
            if rb > 1e-4:
                for i in range(corner_segments + 1):
                    ang = (math.pi * 0.5) * (1.0 - i / corner_segments)
                    ry = (y_bot + rb) - rb * math.sin(ang)
                    rx = (hw - rb) + rb * math.cos(ang)
                    rows_yh.append((ry, rx))
            else:
                rows_yh.append((y_bot, hw))

            if rt > 1e-4:
                for i in range(corner_segments + 1):
                    ang = (math.pi * 0.5) * (i / corner_segments)
                    ry = (y_top - rt) + rt * math.sin(ang)
                    rx = (hw - rt) + rt * math.cos(ang)
                    rows_yh.append((ry, rx))
            else:
                rows_yh.append((y_top, hw))

            grid: list[list[bmesh.types.BMVert]] = []
            yaw_rad = math.radians(yaw_deg)
            for ry, rx in rows_yh:
                row_verts: list[bmesh.types.BMVert] = []
                for c in range(cols + 1):
                    u = -1.0 + 2.0 * (c / cols)
                    sx = u * rx
                    if cyl_radius is None:
                        pt = (sx, ry, z_front - thickness + z_off)
                    else:
                        theta = yaw_rad + (sx / cyl_radius)
                        rad = cyl_radius + z_off
                        pt = (rad * math.sin(theta), ry, rad * math.cos(theta))
                    row_verts.append(self.bm.verts.new(self._to_local_b(pt)))
                grid.append(row_verts)
            return grid

        g_in = _build_grid(0.0)
        g_out = _build_grid(thickness)

        n_rows = len(g_out)
        faces: list[bmesh.types.BMFace] = []

        for r in range(n_rows - 1):
            for c in range(cols):
                faces.append(self.bm.faces.new([g_out[r][c], g_out[r][c + 1], g_out[r + 1][c + 1], g_out[r + 1][c]]))
                faces.append(self.bm.faces.new([g_in[r][c + 1], g_in[r][c], g_in[r + 1][c], g_in[r + 1][c + 1]]))

        def _perim(g: list[list[bmesh.types.BMVert]]) -> list[bmesh.types.BMVert]:
            p: list[bmesh.types.BMVert] = []
            for c in range(cols):
                p.append(g[0][c])
            for r in range(n_rows - 1):
                p.append(g[r][cols])
            for c in range(cols, 0, -1):
                p.append(g[n_rows - 1][c])
            for r in range(n_rows - 1, 0, -1):
                p.append(g[r][0])
            return p

        ra = _perim(g_in)
        rb_ring = _perim(g_out)
        m_len = len(ra)
        for i in range(m_len):
            j = (i + 1) % m_len
            faces.append(self.bm.faces.new([ra[i], ra[j], rb_ring[j], rb_ring[i]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)
        return faces

    def add_rounded_portal_ring(
        self,
        w_in: float,
        y_bot_in: float,
        y_top_in: float,
        r_bot_in: float,
        r_top_in: float,
        w_out: float,
        y_bot_out: float,
        y_top_out: float,
        r_bot_out: float,
        r_top_out: float,
        thickness: float,
        mat: str,
        z_front: float = 0.0,
        cyl_radius: float | None = None,
        yaw_deg: float = 0.0,
        corner_segments: int = 10,
        sub_rings: int = 1,
        bevel: float = 0.016,
    ) -> list[bmesh.types.BMFace]:
        """Add a closed all-quad portal frame or cylindrical surround panel between two rounded-corner contours."""
        _ = (sub_rings, bevel)
        horiz_segs = 6

        def _contour(width: float, yb: float, yt: float, rb: float, rt: float) -> list[tuple[float, float]]:
            hw = width * 0.5
            h_tot = yt - yb
            rb_c = max(0.004, min(rb, hw - 0.01, h_tot * 0.48))
            rt_c = max(0.004, min(rt, hw - 0.01, h_tot * 0.48))
            pts: list[tuple[float, float]] = []
            # Bottom-right corner (-pi/2 .. 0)
            for i in range(corner_segments + 1):
                ang = -math.pi * 0.5 + (math.pi * 0.5) * (i / corner_segments)
                pts.append(((hw - rb_c) + rb_c * math.cos(ang), (yb + rb_c) + rb_c * math.sin(ang)))
            # Top-right corner (0 .. pi/2)
            for i in range(corner_segments + 1):
                ang = (math.pi * 0.5) * (i / corner_segments)
                pts.append(((hw - rt_c) + rt_c * math.cos(ang), (yt - rt_c) + rt_c * math.sin(ang)))
            # Top horizontal span (hw - rt_c .. -hw + rt_c) so cylindrical panels follow the cylinder arc
            for s in range(1, horiz_segs):
                u = s / horiz_segs
                pts.append(((hw - rt_c) * (1.0 - u) + (-hw + rt_c) * u, yt))
            # Top-left corner (pi/2 .. pi)
            for i in range(corner_segments + 1):
                ang = math.pi * 0.5 + (math.pi * 0.5) * (i / corner_segments)
                pts.append(((-hw + rt_c) + rt_c * math.cos(ang), (yt - rt_c) + rt_c * math.sin(ang)))
            # Bottom-left corner (pi .. 3*pi/2)
            for i in range(corner_segments + 1):
                ang = math.pi + (math.pi * 0.5) * (i / corner_segments)
                pts.append(((-hw + rb_c) + rb_c * math.cos(ang), (yb + rb_c) + rb_c * math.sin(ang)))
            # Bottom horizontal span (-hw + rb_c .. hw - rb_c) so cylindrical panels follow the cylinder arc
            for s in range(1, horiz_segs):
                u = s / horiz_segs
                pts.append(((-hw + rb_c) * (1.0 - u) + (hw - rb_c) * u, yb))
            return pts

        c_in = _contour(w_in, y_bot_in, y_top_in, r_bot_in, r_top_in)
        c_out = _contour(w_out, y_bot_out, y_top_out, r_bot_out, r_top_out)
        m_pts = len(c_in)
        yaw_rad = math.radians(yaw_deg)

        def _make_ring(t_lerp: float, z_off: float) -> list[bmesh.types.BMVert]:
            ring_verts: list[bmesh.types.BMVert] = []
            for k in range(m_pts):
                sx = c_in[k][0] * (1.0 - t_lerp) + c_out[k][0] * t_lerp
                sy = c_in[k][1] * (1.0 - t_lerp) + c_out[k][1] * t_lerp
                if cyl_radius is None:
                    pt = (sx, sy, z_front - thickness + z_off)
                else:
                    theta = yaw_rad + (sx / cyl_radius)
                    rad = cyl_radius + z_off
                    pt = (rad * math.sin(theta), sy, rad * math.cos(theta))
                ring_verts.append(self.bm.verts.new(self._to_local_b(pt)))
            return ring_verts

        profile_specs: list[tuple[float, float]] = [
            (0.0, thickness),
            (1.0, thickness),
            (1.0, 0.0),
            (0.0, 0.0),
        ]

        loops = [_make_ring(t_val, z_val) for t_val, z_val in profile_specs]
        faces: list[bmesh.types.BMFace] = []
        n_loops = len(loops)
        for l_idx in range(n_loops):
            la = loops[l_idx]
            lb = loops[(l_idx + 1) % n_loops]
            for i in range(m_pts):
                j = (i + 1) % m_pts
                faces.append(self.bm.faces.new([la[i], lb[i], lb[j], la[j]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)
        return faces


    def add_bolt_row(
        self,
        start_u: tuple[float, float, float],
        end_u: tuple[float, float, float],
        count: int,
        radius: float = 0.016,
        height: float = 0.012,
        axis_u: str = "Z",
        mat: str = "R1_BareSteel",
        bevel_segments: int = 1,
    ) -> None:
        """Add a linear row of evenly spaced 6-sided hex bolts between start_u and end_u."""
        if count <= 0:
            return
        if count == 1:
            mid = tuple((start_u[k] + end_u[k]) * 0.5 for k in range(3))
            self.add_bolts([mid], radius=radius, height=height, axis_u=axis_u, mat=mat, bevel_segments=bevel_segments)
            return
        pts: list[tuple[float, float, float]] = []
        for i in range(count):
            t = i / (count - 1)
            pts.append(tuple(start_u[k] * (1.0 - t) + end_u[k] * t for k in range(3)))
        self.add_bolts(pts, radius=radius, height=height, axis_u=axis_u, mat=mat, bevel_segments=bevel_segments)

    def add_hinge(
        self,
        center_u: tuple[float, float, float],
        axis_u: str = "X",
        length: float = 0.22,
        knuckle_radius: float = 0.028,
        leaf_width: float = 0.085,
        leaf_thickness: float = 0.018,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        mat_body: str = "R1_GreyMetal",
        mat_pin: str = "R1_BareSteel",
    ) -> None:
        """Add a heavy industrial 3-knuckle barrel hinge with mounting leaf plate and steel hinge pin."""
        cx, cy, cz = center_u
        seg_len = length * 0.28
        offsets = (-length * 0.34, 0.0, length * 0.34)
        for idx, off in enumerate(offsets):
            if axis_u == "X":
                k_pos = (cx + off, cy, cz)
            elif axis_u == "Y":
                k_pos = (cx, cy + off, cz)
            else:
                k_pos = (cx, cy, cz + off)
            self.add_cylinder(
                center_u=k_pos,
                radius=knuckle_radius * (1.04 if idx == 1 else 1.0),
                length=seg_len,
                mat=mat_body,
                axis_u=axis_u,
                segments=10,
                bevel=knuckle_radius * 0.16,
                bevel_segments=1,
                rot_deg_u=rot_deg_u,
            )
        # Central steel hinge pin extending slightly past the outer knuckles
        self.add_cylinder(
            center_u=center_u,
            radius=knuckle_radius * 0.56,
            length=length * 1.08,
            mat=mat_pin,
            axis_u=axis_u,
            segments=8,
            bevel=knuckle_radius * 0.10,
            bevel_segments=1,
            rot_deg_u=rot_deg_u,
        )
        # Mounting leaf strap behind the knuckles
        if axis_u == "X":
            leaf_size = (length * 0.92, leaf_width, leaf_thickness)
        elif axis_u == "Y":
            leaf_size = (leaf_width, length * 0.92, leaf_thickness)
        else:
            leaf_size = (leaf_width, leaf_thickness, length * 0.92)
        self.add_bevel_box(
            center_u=center_u,
            size_u=leaf_size,
            mat=mat_body,
            bevel=min(leaf_thickness * 0.3, 0.005),
            segments=1,
            rot_deg_u=rot_deg_u,
        )

    def add_vent_grille(
        self,
        center_u: tuple[float, float, float],
        width: float,
        height: float,
        depth: float = 0.032,
        slat_count: int = 5,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        frame_mat: str = "R1_GreyMetal",
        back_mat: str = "R1_DarkGlass",
    ) -> None:
        """Add a bevelled rectangular louver vent grille with dark recessed backing and angled slats."""
        # Outer bevelled frame housing
        self.add_bevel_box(
            center_u=center_u,
            size_u=(width, height, depth),
            mat=frame_mat,
            bevel=0.008,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
        # Dark recessed shadow backing
        self.add_bevel_box(
            center_u=center_u,
            size_u=(max(0.02, width - 0.044), max(0.02, height - 0.044), depth + 0.006),
            mat=back_mat,
            bevel=0.003,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
        if slat_count <= 0:
            return
        usable_h = max(0.04, height - 0.06)
        step_y = usable_h / slat_count
        slat_h = step_y * 0.52
        yaw_rad = math.radians(rot_deg_u[1])
        fwd_x = math.sin(yaw_rad) * (depth * 0.36)
        fwd_z = math.cos(yaw_rad) * (depth * 0.36)
        for i in range(slat_count):
            local_y = -usable_h * 0.5 + step_y * (i + 0.5)
            self.add_bevel_box(
                center_u=(center_u[0] + fwd_x, center_u[1] + local_y, center_u[2] + fwd_z),
                size_u=(max(0.02, width - 0.056), slat_h, depth * 0.48),
                mat=frame_mat,
                bevel=0.003,
                segments=1,
                rot_deg_u=(rot_deg_u[0] - 22.0, rot_deg_u[1], rot_deg_u[2]),
            )

    def add_thruster_quad(
        self,
        center_u: tuple[float, float, float],
        outward_yaw_deg: float,
        pod_size: float = 0.24,
        nozzle_length: float = 0.14,
        nozzle_radius: float = 0.052,
        pod_mat: str = "R1_GreyMetal",
        nozzle_mat: str = "R1_BareSteel",
    ) -> None:
        """Add a spacecraft 4-nozzle RCS attitude-control thruster pod oriented at outward_yaw_deg around Y."""
        cx, cy, cz = center_u
        yaw_rad = math.radians(outward_yaw_deg)
        out_x = math.sin(yaw_rad)
        out_z = math.cos(yaw_rad)
        tan_x = math.cos(yaw_rad)
        tan_z = -math.sin(yaw_rad)

        # Base mounting collar & sculpted RCS manifold block
        self.add_bevel_box(
            center_u=(cx - out_x * 0.03, cy, cz - out_z * 0.03),
            size_u=(pod_size * 0.92, pod_size * 0.92, pod_size * 0.65),
            mat="R1_Copper",
            bevel=0.008,
            segments=1,
            rot_deg_u=(0.0, outward_yaw_deg, 0.0),
        )
        self.add_bevel_box(
            center_u=(cx + out_x * 0.02, cy, cz + out_z * 0.02),
            size_u=(pod_size, pod_size, pod_size * 0.72),
            mat=pod_mat,
            bevel=0.014,
            segments=2,
            rot_deg_u=(0.0, outward_yaw_deg, 0.0),
            top_scale_xz=(0.86, 0.86),
        )

        # 4 conical De Laval thruster bells: Up (+Y), Down (-Y), Left-Tangential (+T), Right-Tangential (-T)
        reach = pod_size * 0.50 + nozzle_length * 0.42
        for sign_y in (-1.0, 1.0):
            ny = cy + sign_y * reach
            r_bot = nozzle_radius if sign_y < 0.0 else nozzle_radius * 0.48
            r_top = nozzle_radius * 0.48 if sign_y < 0.0 else nozzle_radius
            self.add_cylinder(
                center_u=(cx + out_x * 0.03, ny, cz + out_z * 0.03),
                radius=r_bot,
                radius_top=r_top,
                length=nozzle_length,
                mat=nozzle_mat,
                axis_u="Y",
                segments=10,
                bevel=0.003,
                bevel_segments=1,
            )
        for sign_t in (-1.0, 1.0):
            nx = cx + out_x * 0.03 + sign_t * tan_x * reach
            nz = cz + out_z * 0.03 + sign_t * tan_z * reach
            r_bot = nozzle_radius if sign_t < 0.0 else nozzle_radius * 0.48
            r_top = nozzle_radius * 0.48 if sign_t < 0.0 else nozzle_radius
            self.add_cylinder(
                center_u=(nx, cy, nz),
                radius=r_bot,
                radius_top=r_top,
                length=nozzle_length,
                mat=nozzle_mat,
                axis_u="X",
                segments=10,
                bevel=0.003,
                bevel_segments=1,
                rot_deg_u=(0.0, outward_yaw_deg, 0.0),
            )

    def add_foil_wrap(
        self,
        p0_u: tuple[float, float, float],
        p1_u: tuple[float, float, float],
        radius: float,
        rings: int = 5,
        segments: int = 10,
        foil_mat: str = "R1_GoldFoil",
        band_mat: str = "R1_Rubber",
    ) -> list[bmesh.types.BMFace]:
        """Add a crinkled multi-layer gold Kapton foil sleeve between p0_u and p1_u with end retaining bands."""
        v0 = self._to_local_b(p0_u)
        v1 = self._to_local_b(p1_u)
        delta = v1 - v0
        length = delta.length
        if length < 1e-4:
            return []

        direction = delta.normalized()
        up_axis = "Y" if abs(direction.y) < 0.92 else "X"
        quat = direction.to_track_quat("Z", up_axis)

        ring_verts: list[list[bmesh.types.BMVert]] = []
        total_rings = max(3, rings + 1)
        for r_idx in range(total_rings):
            t = r_idx / (total_rings - 1)
            center_pt = v0.lerp(v1, t)
            is_end = r_idx == 0 or r_idx == total_rings - 1
            ring_scale = 0.94 if is_end else (1.035 if r_idx % 2 == 1 else 0.985)
            twist = 0.0 if is_end else ((math.pi / segments) * 0.16 * (1.0 if r_idx % 2 == 1 else -1.0))
            ring: list[bmesh.types.BMVert] = []
            for s in range(segments):
                ang = (2.0 * math.pi * s) / segments + twist
                crinkle = 1.0 if is_end else (1.0 + 0.028 * math.sin(s * 2.0 + r_idx * 1.7))
                rad = radius * ring_scale * crinkle
                local_pt = Vector((rad * math.cos(ang), rad * math.sin(ang), 0.0))
                ring.append(self.bm.verts.new(center_pt + (quat @ local_pt)))
            ring_verts.append(ring)

        faces: list[bmesh.types.BMFace] = []
        for r_idx in range(len(ring_verts) - 1):
            r0 = ring_verts[r_idx]
            r1 = ring_verts[r_idx + 1]
            for s in range(segments):
                sn = (s + 1) % segments
                faces.append(self.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))
        faces.append(self.bm.faces.new(list(reversed(ring_verts[0]))))
        faces.append(self.bm.faces.new(ring_verts[-1]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, foil_mat)
        cap_edges = _unique(list(faces[-2].edges) + list(faces[-1].edges))
        faces, _ = self._bevel_edges(faces, cap_edges, min(0.005, radius * 0.06), 1, foil_mat)

        # Add dark retaining collar bands at both ends of the foil wrap
        band_len = min(0.048, length * 0.14)
        for t_band in (0.04, 0.96):
            pt_a = tuple(p0_u[k] * (1.0 - (t_band - 0.03)) + p1_u[k] * (t_band - 0.03) for k in range(3))
            pt_b = tuple(p0_u[k] * (1.0 - (t_band + 0.03)) + p1_u[k] * (t_band + 0.03) for k in range(3))
            self.add_strut(
                p0_u=pt_a,
                p1_u=pt_b,
                width=radius * 2.02,
                thickness=radius * 2.02,
                mat=band_mat,
                bevel=min(0.006, band_len * 0.25),
            )
        return faces

    def add_fractured_rock(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float],
        cuts_u: Sequence[tuple[tuple[float, float, float], float]],
        mat: str = "R1_Basalt",
        bevel: float = 0.032,
        bevel_segments: int = 1,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ) -> list[bmesh.types.BMFace]:
        """Add a closed, bevelled fractured basalt block/slab sliced by half-space planes in normalized [-1, 1]^3 space."""
        wx, hy, dz = size_u
        res = bmesh.ops.create_cube(self.bm, size=2.0)
        shell_verts = list(res["verts"])
        for v in shell_verts:
            if v.co.z > 0.0:
                v.co.x = v.co.x * 0.62 + 0.10
                v.co.y = v.co.y * 0.64 - 0.08
                v.co.z += 0.18 * v.co.x - 0.14 * v.co.y
            else:
                v.co.x = v.co.x * 0.96 - 0.04
                v.co.y = v.co.y * 0.95 + 0.04
        shell_faces = _unique(f for v in shell_verts for f in v.link_faces if f.is_valid)
        shell_edges = _unique(e for f in shell_faces for e in f.edges if e.is_valid)

        for norm_u, dist_frac in cuts_u:
            n_b = u_vec(*norm_u)
            if n_b.length < 1e-6:
                continue
            n_b.normalize()
            p_co = n_b * float(dist_frac)
            geom = [v for v in shell_verts if v.is_valid] + [e for e in shell_edges if e.is_valid] + [
                f for f in shell_faces if f.is_valid
            ]
            if not geom:
                break
            b_res = bmesh.ops.bisect_plane(
                self.bm,
                geom=geom,
                dist=1e-5,
                plane_co=p_co,
                plane_no=n_b,
                clear_outer=True,
                clear_inner=False,
            )
            cut_edges = [
                g for g in b_res.get("geom_cut", []) if isinstance(g, bmesh.types.BMEdge) and g.is_valid
            ]
            if len(cut_edges) >= 3:
                fill_res = bmesh.ops.edgeloop_fill(self.bm, edges=cut_edges)
                new_faces = [f for f in fill_res.get("faces", []) if isinstance(f, bmesh.types.BMFace) and f.is_valid]
            else:
                new_faces = []

            surviving_verts = [
                g for g in b_res.get("geom", []) if isinstance(g, bmesh.types.BMVert) and g.is_valid
            ]
            shell_faces = _unique(
                [f for v in surviving_verts for f in v.link_faces if f.is_valid] + new_faces
            )
            shell_verts = _unique(v for f in shell_faces for v in f.verts if v.is_valid)
            shell_edges = _unique(e for f in shell_faces for e in f.edges if e.is_valid)

        if shell_verts:
            bmesh.ops.remove_doubles(self.bm, verts=shell_verts, dist=0.18)
            shell_faces = _unique(f for f in shell_faces if f.is_valid and len(f.verts) >= 3)
            shell_verts = _unique(v for f in shell_faces for v in f.verts if v.is_valid)
            shell_edges = _unique(e for f in shell_faces for e in f.edges if e.is_valid)

        # Normalize bounding box to [-0.5, 0.5]^3 and scale to (wx, dz, hy)
        if shell_verts:
            xs = [v.co.x for v in shell_verts]
            ys = [v.co.y for v in shell_verts]
            zs = [v.co.z for v in shell_verts]
            min_x, max_x = min(xs), max(xs)
            min_y, max_y = min(ys), max(ys)
            min_z, max_z = min(zs), max(zs)
            span_x = max(1e-4, max_x - min_x)
            span_y = max(1e-4, max_y - min_y)
            span_z = max(1e-4, max_z - min_z)
            cx0 = 0.5 * (min_x + max_x)
            cy0 = 0.5 * (min_y + max_y)
            cz0 = 0.5 * (min_z + max_z)
            for v in shell_verts:
                v.co.x = ((v.co.x - cx0) / span_x) * wx
                v.co.y = ((v.co.y - cy0) / span_y) * dz
                v.co.z = ((v.co.z - cz0) / span_z) * hy

        bmesh.ops.recalc_face_normals(self.bm, faces=shell_faces)
        self._assign_faces(shell_faces, mat)
        all_faces, all_verts = self._bevel_edges(shell_faces, shell_edges, bevel, bevel_segments, mat)

        rot_b = Euler(
            (
                math.radians(rot_deg_u[0]),
                math.radians(rot_deg_u[2]),
                math.radians(-rot_deg_u[1]),
            ),
            "XYZ",
        ).to_matrix()
        bmesh.ops.rotate(self.bm, verts=all_verts, cent=(0.0, 0.0, 0.0), matrix=rot_b)
        bmesh.ops.translate(self.bm, verts=all_verts, vec=self._to_local_b(center_u))
        return all_faces

    def add_weathered_rock(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float],
        seed: int,
        lon_segs: int = 16,
        lat_rings: int = 8,
        fractures_u: Sequence[tuple[tuple[float, float, float], float, float]] = (),
        pits_u: Sequence[tuple[tuple[float, float, float], float, float]] = (),
        crevices_u: Sequence[tuple[tuple[float, float, float], float, float, float]] = (),
        skirt_flare: float = 0.16,
        burial_frac: float = 0.16,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        mat: str = "R1_Basalt",
    ) -> list[bmesh.types.BMFace]:
        """Add a closed, manifold weathered lunar basalt/breccia lump with soft fracture faces, vesicle pits, crevices, buried base, and regolith skirt."""
        wx, hy, dz = size_u
        rng = np.random.default_rng(seed)

        # Deterministic 3D harmonic wave vectors for organic breccia lump deformation
        n_waves = 9
        wave_dirs = rng.normal(0.0, 1.0, size=(n_waves, 3)).astype(np.float64)
        wave_dirs /= np.linalg.norm(wave_dirs, axis=1, keepdims=True) + 1e-8
        wave_freqs = np.array([1.25, 1.45, 1.75, 2.15, 2.55, 3.05, 3.60, 4.20, 4.85], dtype=np.float64)
        wave_amps = np.array([0.16, 0.13, 0.11, 0.085, 0.065, 0.050, 0.038, 0.028, 0.022], dtype=np.float64)
        wave_phases = rng.uniform(0.0, 2.0 * math.pi, size=n_waves)

        def _sculpt_dir(dx: float, dy: float, dz_u: float) -> tuple[float, float, float]:
            # 1. Broad asymmetric breccia lobes & weathered humps
            disp = 1.0
            for w_idx in range(n_waves):
                kx, ky, kz = wave_dirs[w_idx]
                dot_k = dx * kx + dy * ky + dz_u * kz
                disp += float(wave_amps[w_idx]) * math.sin(
                    dot_k * float(wave_freqs[w_idx]) * math.pi + float(wave_phases[w_idx])
                )

            # Broaden lower mass and gently dome the crown so rock sits with heavy gravity
            lower_broaden = 1.0 + 0.16 * max(0.0, 0.30 - dy)
            px = dx * disp * lower_broaden
            py = dy * disp
            pz = dz_u * disp * lower_broaden

            # Flatten underside smoothly for buried footing stability
            if py < -0.58:
                py = -0.58 + (py + 0.58) * 0.32

            # 2. Softened conchoidal fracture planes with rounded/chipped rims (never sharp polyhedra)
            for f_idx, (norm_u, plane_dist, strength) in enumerate(fractures_u):
                nx, ny, nz = norm_u
                n_len = math.sqrt(nx * nx + ny * ny + nz * nz)
                if n_len < 1e-6:
                    continue
                nx, ny, nz = nx / n_len, ny / n_len, nz / n_len
                proj = px * nx + py * ny + pz * nz
                # Tangential coordinates for subtle fracture-face undulation & chipped rim
                tx = px - proj * nx
                ty = py - proj * ny
                tz = pz - proj * nz
                t_len = math.sqrt(tx * tx + ty * ty + tz * tz)
                chip_wave = 0.035 * math.sin(t_len * 6.5 + f_idx * 2.1 + seed * 0.3)
                eff_dist = plane_dist + chip_wave
                if proj > eff_dist - 0.08:
                    # Smooth soft-knee compression above the fracture plane
                    delta = proj - eff_dist
                    soft_excess = 0.5 * (delta + math.sqrt(delta * delta + 0.09 * 0.09))
                    # Subtle conchoidal ripple across the cleaved face
                    conchoid = 0.024 * math.cos(t_len * 5.2 + f_idx * 1.7)
                    # Slight weathered rim shoulder right at the fracture margin
                    rim_shoulder = 0.020 * math.exp(-((delta / 0.14) ** 2))
                    push = -soft_excess * strength + conchoid * min(1.0, soft_excess * 4.0) + rim_shoulder
                    px += nx * push
                    py += ny * push
                    pz += nz * push

            # 3. Macro spall / vesicle pits (concave indentations with subtle raised rims)
            r_cur = math.sqrt(px * px + py * py + pz * pz)
            if r_cur > 1e-6:
                ux, uy, uz = px / r_cur, py / r_cur, pz / r_cur
                pit_scale = 1.0
                for p_dir_u, ang_rad, p_depth in pits_u:
                    pdx, pdy, pdz = p_dir_u
                    p_len = math.sqrt(pdx * pdx + pdy * pdy + pdz * pdz)
                    if p_len < 1e-6 or ang_rad <= 1e-4:
                        continue
                    pdx, pdy, pdz = pdx / p_len, pdy / p_len, pdz / p_len
                    cos_a = max(-1.0, min(1.0, ux * pdx + uy * pdy + uz * pdz))
                    ang = math.acos(cos_a)
                    u_ang = ang / ang_rad
                    if u_ang < 1.35:
                        bowl = (max(0.0, 1.0 - u_ang * u_ang) ** 1.55) * p_depth
                        rim = math.exp(-(((u_ang - 1.03) / 0.20) ** 2)) * p_depth * 0.24
                        pit_scale = min(pit_scale, 1.0 - bowl + rim)

                # 4. Weathered fracture crevices / split seams
                crev_scale = 1.0
                for c_norm_u, c_off, c_width, c_depth in crevices_u:
                    cnx, cny, cnz = c_norm_u
                    c_len = math.sqrt(cnx * cnx + cny * cny + cnz * cnz)
                    if c_len < 1e-6 or c_width <= 1e-4:
                        continue
                    cnx, cny, cnz = cnx / c_len, cny / c_len, cnz / c_len
                    dist_c = abs((ux * cnx + uy * cny + uz * cnz) - c_off)
                    if dist_c < c_width and uy > -0.40:
                        tc = 1.0 - (dist_c / c_width)
                        up_fade = max(0.0, min(1.0, (uy + 0.40) / 0.45))
                        groove = (tc * tc) * (3.0 - 2.0 * tc) * c_depth * up_fade
                        crev_scale = min(crev_scale, 1.0 - groove)

                total_scale = max(0.42, pit_scale * crev_scale)
                px *= total_scale
                py *= total_scale
                pz *= total_scale

            return (px, py, pz)

        # Build raw Unity coordinates on the closed sphere topology
        raw_pts: list[list[float]] = []
        raw_pts.append(list(_sculpt_dir(0.0, 1.0, 0.0)))  # index 0: top pole
        ring_indices: list[list[int]] = []
        for r in range(1, lat_rings):
            v_frac = r / lat_rings
            # Slightly denser latitude spacing near the lower ground-skirt transition
            theta = math.pi * (0.88 * v_frac + 0.12 * (v_frac * v_frac))
            sin_t = math.sin(theta)
            cos_t = math.cos(theta)
            twist = (math.pi / lon_segs) * 0.32 * (r % 2)
            ring: list[int] = []
            for s in range(lon_segs):
                phi = (2.0 * math.pi * s) / lon_segs + twist
                dx = sin_t * math.cos(phi)
                dy = cos_t
                dz_u = sin_t * math.sin(phi)
                ring.append(len(raw_pts))
                raw_pts.append(list(_sculpt_dir(dx, dy, dz_u)))
            ring_indices.append(ring)
        bot_idx = len(raw_pts)
        raw_pts.append(list(_sculpt_dir(0.0, -1.0, 0.0)))  # bottom pole

        # Normalize to target (wx, hy, dz) with buried base and flared regolith skirt
        xs = [p[0] for p in raw_pts]
        ys = [p[1] for p in raw_pts]
        zs = [p[2] for p in raw_pts]
        min_x, max_x = min(xs), max(xs)
        min_y, max_y = min(ys), max(ys)
        min_z, max_z = min(zs), max(zs)
        span_x = max(1e-4, max_x - min_x)
        span_y = max(1e-4, max_y - min_y)
        span_z = max(1e-4, max_z - min_z)
        cx0 = 0.5 * (min_x + max_x)
        cz0 = 0.5 * (min_z + max_z)

        burial_y = hy * burial_frac
        for p in raw_pts:
            nx_u = ((p[0] - cx0) / span_x) * wx
            ny_01 = (p[1] - min_y) / span_y
            nz_u = ((p[2] - cz0) / span_z) * dz

            # Flared regolith skirt around ground contact (ny_01 in [0.05, 0.40])
            if 0.04 < ny_01 < 0.42 and skirt_flare > 0.0:
                t_sk = (ny_01 - 0.04) / 0.38
                skirt_bell = math.sin(math.pi * t_sk) ** 1.35
                az = math.atan2(nz_u, nx_u)
                az_mod = 0.84 + 0.32 * (0.5 + 0.5 * math.sin(3.0 * az + seed * 0.7))
                flare = 1.0 + skirt_flare * skirt_bell * az_mod
                nx_u *= flare
                nz_u *= flare
                # Slightly settle the outer skirt rim toward the ground plane
                ny_01 -= 0.025 * skirt_bell * skirt_flare

            p[0] = nx_u
            p[1] = ny_01 * hy - burial_y
            p[2] = nz_u

        # Create BMVerts in local Blender coordinates
        bm_verts = [self.bm.verts.new(u_pos(p[0], p[1], p[2])) for p in raw_pts]
        faces: list[bmesh.types.BMFace] = []

        # Top cap
        first_ring = ring_indices[0]
        for s in range(lon_segs):
            sn = (s + 1) % lon_segs
            faces.append(self.bm.faces.new([bm_verts[0], bm_verts[first_ring[s]], bm_verts[first_ring[sn]]]))

        # Middle quad bands
        for r_idx in range(len(ring_indices) - 1):
            r0 = ring_indices[r_idx]
            r1 = ring_indices[r_idx + 1]
            for s in range(lon_segs):
                sn = (s + 1) % lon_segs
                faces.append(
                    self.bm.faces.new([bm_verts[r0[s]], bm_verts[r1[s]], bm_verts[r1[sn]], bm_verts[r0[sn]]])
                )

        # Bottom cap
        last_ring = ring_indices[-1]
        for s in range(lon_segs):
            sn = (s + 1) % lon_segs
            faces.append(self.bm.faces.new([bm_verts[bot_idx], bm_verts[last_ring[sn]], bm_verts[last_ring[s]]]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        rot_b = Euler(
            (
                math.radians(rot_deg_u[0]),
                math.radians(rot_deg_u[2]),
                math.radians(-rot_deg_u[1]),
            ),
            "XYZ",
        ).to_matrix()
        bmesh.ops.rotate(self.bm, verts=bm_verts, cent=(0.0, 0.0, 0.0), matrix=rot_b)
        bmesh.ops.translate(self.bm, verts=bm_verts, vec=self._to_local_b(center_u))
        return faces

    def add_torus(
        self,
        center_u: tuple[float, float, float],
        major_radius: float,
        minor_radius: float,
        mat: str,
        axis_u: str = "Y",
        major_segments: int = 24,
        minor_segments: int = 10,
        arc_deg: float = 360.0,
        start_deg: float = 0.0,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ) -> list[bmesh.types.BMFace]:
        """Add a closed manifold torus ring or capped partial torus arc in Unity coordinates."""
        is_full = abs(arc_deg) >= 359.99
        n_rings = major_segments if is_full else (major_segments + 1)
        start_rad = math.radians(start_deg)
        span_rad = math.radians(arc_deg)

        rings: list[list[bmesh.types.BMVert]] = []
        all_verts: list[bmesh.types.BMVert] = []
        for r_idx in range(n_rings):
            t = r_idx / major_segments
            theta = start_rad + span_rad * t
            cos_t = math.cos(theta)
            sin_t = math.sin(theta)
            ring: list[bmesh.types.BMVert] = []
            for s_idx in range(minor_segments):
                phi = (2.0 * math.pi * s_idx) / minor_segments
                r_cur = major_radius + minor_radius * math.cos(phi)
                z_cur = minor_radius * math.sin(phi)
                v = self.bm.verts.new((r_cur * cos_t, r_cur * sin_t, z_cur))
                ring.append(v)
                all_verts.append(v)
            rings.append(ring)

        faces: list[bmesh.types.BMFace] = []
        ring_pairs = major_segments
        for r_idx in range(ring_pairs):
            r0 = rings[r_idx]
            r1 = rings[(r_idx + 1) % len(rings)]
            for s_idx in range(minor_segments):
                sn = (s_idx + 1) % minor_segments
                faces.append(self.bm.faces.new([r0[s_idx], r1[s_idx], r1[sn], r0[sn]]))

        if not is_full:
            faces.append(self.bm.faces.new(list(reversed(rings[0]))))
            faces.append(self.bm.faces.new(rings[-1]))

        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign_faces(faces, mat)

        if axis_u == "X":
            align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
        elif axis_u == "Z":
            align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
        else:
            align_mat = Matrix.Identity(3)

        rot_b = Euler(
            (
                math.radians(rot_deg_u[0]),
                math.radians(rot_deg_u[2]),
                math.radians(-rot_deg_u[1]),
            ),
            "XYZ",
        ).to_matrix()
        bmesh.ops.rotate(self.bm, verts=all_verts, cent=(0.0, 0.0, 0.0), matrix=rot_b @ align_mat)
        bmesh.ops.translate(self.bm, verts=all_verts, vec=self._to_local_b(center_u))
        return faces

    def add_ingot(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float] = (0.14, 0.072, 0.28),
        mat: str = "R1_BareSteel",
        bevel: float = 0.008,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ) -> list[bmesh.types.BMFace]:
        """Add a bevelled trapezoidal ingot bar or lunar glass block."""
        return self.add_bevel_box(
            center_u=center_u,
            size_u=size_u,
            mat=mat,
            bevel=bevel,
            segments=1,
            rot_deg_u=rot_deg_u,
            top_scale_xz=(0.76, 0.86),
        )

    def add_supply_crate(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float] = (0.38, 0.28, 0.34),
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        body_mat: str = "R1_CreamEnamel",
        stripe_mat: str = "R1_OrangeStripe",
        hardware_mat: str = "R1_GreyMetal",
    ) -> None:
        """Add a salvaged crew supply crate with lower tub, upper lid overhang, orange strap bands, and front latch."""
        cx, cy, cz = center_u
        wx, hy, dz = size_u
        yaw_rad = math.radians(rot_deg_u[1])
        pitch_rad = math.radians(rot_deg_u[0])

        def _local_to_world(lx: float, ly: float, lz: float) -> tuple[float, float, float]:
            # Apply pitch (X) then yaw (Y)
            py = ly * math.cos(pitch_rad) - lz * math.sin(pitch_rad)
            pz = ly * math.sin(pitch_rad) + lz * math.cos(pitch_rad)
            rx = lx * math.cos(yaw_rad) + pz * math.sin(yaw_rad)
            rz = -lx * math.sin(yaw_rad) + pz * math.cos(yaw_rad)
            return (cx + rx, cy + py, cz + rz)

        # Lower crate tub
        tub_h = hy * 0.66
        self.add_bevel_box(
            center_u=_local_to_world(0.0, -hy * 0.5 + tub_h * 0.5, 0.0),
            size_u=(wx * 0.96, tub_h, dz * 0.96),
            mat=body_mat,
            bevel=0.016,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
        # Upper crate lid
        lid_h = hy * 0.34
        self.add_bevel_box(
            center_u=_local_to_world(0.0, hy * 0.5 - lid_h * 0.5 + 0.004, 0.0),
            size_u=(wx, lid_h, dz),
            mat=body_mat,
            bevel=0.018,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
        # Central warm-orange strap band wrapping over front/top/back
        self.add_bevel_box(
            center_u=_local_to_world(0.0, 0.0, 0.0),
            size_u=(wx * 0.30, hy + 0.010, dz + 0.010),
            mat=stripe_mat,
            bevel=0.006,
            segments=1,
            rot_deg_u=rot_deg_u,
        )
        # Front metallic latch clasp
        self.add_bevel_box(
            center_u=_local_to_world(0.0, hy * 0.12, dz * 0.5 + 0.008),
            size_u=(wx * 0.22, hy * 0.26, 0.022),
            mat=hardware_mat,
            bevel=0.004,
            segments=1,
            rot_deg_u=rot_deg_u,
        )

    def add_wire_spool(
        self,
        center_u: tuple[float, float, float],
        flange_radius: float = 0.115,
        width: float = 0.125,
        axis_u: str = "X",
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
        coil_mat: str = "R1_Copper",
        flange_mat: str = "R1_GreyMetal",
        hub_mat: str = "R1_BareSteel",
    ) -> None:
        """Add an industrial cable/wire spool with flanged rims, wound copper core, and steel hub."""
        cx, cy, cz = center_u
        # Wound copper wire drum core
        self.add_cylinder(
            center_u=center_u,
            radius=flange_radius * 0.78,
            length=width * 0.80,
            mat=coil_mat,
            axis_u=axis_u,
            segments=12,
            bevel=0.005,
            bevel_segments=1,
            rot_deg_u=rot_deg_u,
        )
        # End flanges
        flange_t = max(0.012, width * 0.11)
        half_off = (width - flange_t) * 0.5
        yaw_rad = math.radians(rot_deg_u[1])
        for sign in (-1.0, 1.0):
            if axis_u == "X":
                fx = cx + sign * half_off * math.cos(yaw_rad)
                fy = cy
                fz = cz - sign * half_off * math.sin(yaw_rad)
            elif axis_u == "Y":
                fx, fy, fz = cx, cy + sign * half_off, cz
            else:
                fx = cx + sign * half_off * math.sin(yaw_rad)
                fy = cy
                fz = cz + sign * half_off * math.cos(yaw_rad)
            self.add_cylinder(
                center_u=(fx, fy, fz),
                radius=flange_radius,
                length=flange_t,
                mat=flange_mat,
                axis_u=axis_u,
                segments=12,
                bevel=0.004,
                bevel_segments=1,
                rot_deg_u=rot_deg_u,
            )
        # Central steel spindle hub
        self.add_cylinder(
            center_u=center_u,
            radius=flange_radius * 0.26,
            length=width + 0.016,
            mat=hub_mat,
            axis_u=axis_u,
            segments=8,
            bevel=0.003,
            bevel_segments=1,
            rot_deg_u=rot_deg_u,
        )

    def build_object(
        self,
        parent: bpy.types.Object | None = None,
        smooth_angle_deg: float = 52.0,
    ) -> bpy.types.Object:
        """Finalize bmesh into a Blender Mesh Object with canonical vertex/face/loop order and smooth bevel normals."""
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])

        # Remap material indices so obj.data.materials is in canonical ALLOWED_MATERIALS order
        used_canonical = [m for m in ALLOWED_MATERIALS if m in self._mat_to_idx]
        old_to_new = {
            self._mat_to_idx[m]: new_idx for new_idx, m in enumerate(used_canonical)
        }

        # Canonicalize vertex order by 3D position so C pointer hashes in bmesh.ops.bevel never affect indices
        sorted_verts = sorted(
            self.bm.verts,
            key=lambda v: (round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5)),
        )
        vert_to_idx = {v: idx for idx, v in enumerate(sorted_verts)}
        canonical_coords = [
            (round(v.co.x, 6), round(v.co.y, 6), round(v.co.z, 6))
            for v in sorted_verts
        ]

        # Canonicalize each face's starting loop vertex (preserving winding) and sort faces
        face_records: list[tuple[tuple[int, ...], int]] = []
        for f in self.bm.faces:
            if not f.is_valid or len(f.verts) < 3:
                continue
            v_ids = [vert_to_idx[v] for v in f.verts]
            if len(set(v_ids)) < 3:
                continue
            min_pos = v_ids.index(min(v_ids))
            rot_ids = tuple(v_ids[min_pos:] + v_ids[:min_pos])
            mat_idx = old_to_new.get(f.material_index, 0)
            face_records.append((rot_ids, mat_idx))
        face_records.sort(key=lambda item: (item[0], item[1]))

        self.bm.free()

        # Rebuild clean bmesh in canonical order
        cbm = bmesh.new()
        c_verts = [cbm.verts.new(co) for co in canonical_coords]
        cbm.verts.ensure_lookup_table()
        seen_faces: set[tuple[int, ...]] = set()
        for rot_ids, mat_idx in face_records:
            key = tuple(sorted(rot_ids))
            if key in seen_faces:
                continue
            seen_faces.add(key)
            try:
                cf = cbm.faces.new([c_verts[i] for i in rot_ids])
                cf.material_index = mat_idx
                cf.smooth = True
            except ValueError:
                continue

        cbm.faces.index_update()
        cbm.normal_update()

        # Guarantee every closed shell has positive signed volume (outward normals)
        seen_shell_faces: set[int] = set()
        for face in cbm.faces:
            if face.index in seen_shell_faces:
                continue
            stack = [face]
            shell: list[bmesh.types.BMFace] = []
            seen_shell_faces.add(face.index)
            while stack:
                cur = stack.pop()
                shell.append(cur)
                for edge in cur.edges:
                    for other in edge.link_faces:
                        if other.index not in seen_shell_faces:
                            seen_shell_faces.add(other.index)
                            stack.append(other)
            if all(len(e.link_faces) == 2 for sf in shell for e in sf.edges):
                vol = sum(sf.calc_center_median().dot(sf.normal) * sf.calc_area() for sf in shell) / 3.0
                if vol < 0.0:
                    bmesh.ops.reverse_faces(cbm, faces=shell)
        cbm.normal_update()

        cbm.edges.index_update()
        for e in cbm.edges:
            e.index = min(e.verts[0].index, e.verts[1].index) * (len(c_verts) + 1) + max(
                e.verts[0].index, e.verts[1].index
            )
        cbm.edges.sort()
        cbm.verts.index_update()
        cbm.edges.index_update()
        cbm.faces.index_update()

        sharp_thresh = math.radians(smooth_angle_deg)
        for e in cbm.edges:
            if e.is_manifold and e.calc_face_angle(0.0) > sharp_thresh:
                e.smooth = False
            else:
                e.smooth = True

        me = bpy.data.meshes.new(self.name)
        cbm.to_mesh(me)
        cbm.free()

        mats = ensure_materials(use_vertex_color_shading=False)
        for m_name in used_canonical:
            me.materials.append(mats[m_name])

        obj = bpy.data.objects.new(self.name, me)
        bpy.context.collection.objects.link(obj)
        if parent is not None:
            obj.parent = parent
            parent_world = parent.matrix_world.translation
            obj.location = self.pivot_b - parent_world
        else:
            obj.location = self.pivot_b

        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        wn = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
        wn.keep_sharp = True
        wn.weight = 50
        bpy.ops.object.modifier_apply(modifier=wn.name)
        return obj


def apply_triplanar_uvs(obj: bpy.types.Object, texel_scale: float = 0.85) -> None:
    """Assign deterministic dominant-axis box-projected UVs (`UVMap`) to all mesh objects under `obj`."""
    for mesh_obj in collect_hierarchy(obj):
        if mesh_obj.type != "MESH" or mesh_obj.data is None:
            continue
        me = mesh_obj.data
        uv_layer = me.uv_layers.get("UVMap")
        if uv_layer is None:
            uv_layer = me.uv_layers.new(name="UVMap")
        me.uv_layers.active = uv_layer
        me.uv_layers.active_index = 0

        verts = me.vertices
        loops = me.loops
        uv_data = uv_layer.data
        for poly in me.polygons:
            n = poly.normal
            ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
            for loop_idx in poly.loop_indices:
                v_co = verts[loops[loop_idx].vertex_index].co
                if az >= ax and az >= ay:
                    u = v_co.x * texel_scale + 0.5
                    v = v_co.y * texel_scale + 0.5
                elif ax >= ay:
                    u = v_co.y * texel_scale + 0.5
                    v = v_co.z * texel_scale + 0.5
                else:
                    u = v_co.x * texel_scale + 0.5
                    v = v_co.z * texel_scale + 0.5
                uv_data[loop_idx].uv = (round(u, 5), round(v, 5))
        me.update()


def collect_hierarchy(root: bpy.types.Object) -> list[bpy.types.Object]:
    """Collect root and all descendants in deterministic depth-first sorted order."""
    result = [root]
    for child in sorted(root.children, key=lambda o: o.name):
        result.extend(collect_hierarchy(child))
    return result


def count_triangles(root: bpy.types.Object) -> int:
    """Return total triangle count across all mesh objects under root."""
    total = 0
    for obj in collect_hierarchy(root):
        if obj.type == "MESH" and obj.data is not None:
            obj.data.calc_loop_triangles()
            total += len(obj.data.loop_triangles)
    return total


def bake_vertex_colors(
    root: bpy.types.Object,
    footprint_radius: float = 2.0,
    bevel_wear_width: float = 0.022,
) -> None:
    """Bake Cycles AO into R, convex edge wear into G, crease/cavity dust into B, and 1.0 into A."""
    mesh_objs = [o for o in collect_hierarchy(root) if o.type == "MESH" and len(o.data.polygons) > 0]
    if not mesh_objs:
        return

    # Find lowest world Z so the ground AO plane sits just below all geometry
    min_world_z = min(
        (obj.matrix_world @ v.co).z
        for obj in mesh_objs
        for v in obj.data.vertices
    )

    # Temporary ground plane so base footings receive realistic ground-contact AO
    bpy.ops.mesh.primitive_circle_add(
        vertices=32,
        radius=footprint_radius * 1.6,
        fill_type="NGON",
        location=(0.0, 0.0, min_world_z - 0.004),
    )
    ground_obj = bpy.context.active_object
    ground_obj.name = "_TempGroundAO"

    for obj in mesh_objs + [ground_obj]:
        me = obj.data
        while len(me.color_attributes) > 0:
            me.color_attributes.remove(me.color_attributes[0])
        ca = me.color_attributes.new(
            name=VCOL_ATTRIBUTE_NAME,
            type="FLOAT_COLOR",
            domain="CORNER",
        )
        me.color_attributes.active_color = ca
        me.color_attributes.render_color_index = 0

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 48
    scene.cycles.seed = 0
    scene.cycles.use_animated_seed = False

    if scene.world is None:
        scene.world = bpy.data.worlds.new("R1_BakeWorld")
    scene.world.light_settings.distance = 0.65

    scene.render.bake.target = "VERTEX_COLORS"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_objs[0]
    bpy.ops.object.bake(type="AO")

    # Delete temporary ground object and its mesh
    ground_mesh = ground_obj.data
    bpy.data.objects.remove(ground_obj, do_unlink=True)
    bpy.data.meshes.remove(ground_mesh)

    border_thresh = 0.036 if bevel_wear_width <= 0.022 else bevel_wear_width * 1.6
    wear_full = bevel_wear_width
    wear_fade = 0.030 if bevel_wear_width <= 0.022 else bevel_wear_width * 1.36

    # Compute curvature (G = convex wear, B = crease/cavity where dust settles) per mesh loop
    for obj in mesh_objs:
        me = obj.data
        num_loops = len(me.loops)
        rgba = np.ones(num_loops * 4, dtype=np.float32)
        ca = me.color_attributes[VCOL_ATTRIBUTE_NAME]
        ca.data.foreach_get("color", rgba)
        rgba = rgba.reshape(-1, 4)
        ao_loop = np.clip(rgba[:, 0].copy(), 0.0, 1.0)

        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bm.edges.ensure_lookup_table()
        bm.faces.ensure_lookup_table()
        bm.normal_update()

        n_verts = len(bm.verts)
        v_convex = np.zeros(n_verts, dtype=np.float32)
        v_concave = np.zeros(n_verts, dtype=np.float32)

        face_widths = np.array(
            [
                min((e.calc_length() for e in f.edges), default=0.1)
                for f in bm.faces
            ],
            dtype=np.float32,
        )

        for e in bm.edges:
            if not e.is_manifold:
                continue
            signed_ang = e.calc_face_angle_signed()
            f_ws = [float(face_widths[f.index]) for f in e.link_faces]
            min_fw = min(f_ws) if f_ws else 0.1
            max_fw = max(f_ws) if f_ws else 0.1

            # Distinguish true bevel strips and sharp corners from smooth cylindrical/tubular segmentation
            is_sharp_corner = abs(signed_ang) >= 0.95
            is_bevel_border = min_fw < border_thresh and (max_fw > 1.30 * min_fw or min_fw < 0.014)
            if not (is_sharp_corner or is_bevel_border):
                continue

            bevel_boost = 1.85 if min_fw < border_thresh else 1.0

            if signed_ang > 0.04:
                val = min(1.0, (signed_ang / 0.85) * bevel_boost)
                v0, v1 = e.verts[0].index, e.verts[1].index
                if val > v_convex[v0]:
                    v_convex[v0] = val
                if val > v_convex[v1]:
                    v_convex[v1] = val
            elif signed_ang < -0.04:
                val = min(1.0, (-signed_ang / 0.80) * bevel_boost)
                v0, v1 = e.verts[0].index, e.verts[1].index
                if val > v_concave[v0]:
                    v_concave[v0] = val
                if val > v_concave[v1]:
                    v_concave[v1] = val

        v_convex_smooth = v_convex.copy()
        v_concave_smooth = v_concave.copy()
        nbr_dist = 0.04 if bevel_wear_width <= 0.022 else max(0.04, bevel_wear_width * 1.8)
        for v in bm.verts:
            vi = v.index
            nbrs = [e.other_vert(v).index for e in v.link_edges if e.calc_length() < nbr_dist]
            if nbrs:
                v_convex_smooth[vi] = max(
                    v_convex[vi],
                    0.80 * float(np.mean(v_convex[nbrs])),
                )
                v_concave_smooth[vi] = max(
                    v_concave[vi],
                    0.80 * float(np.mean(v_concave[nbrs])),
                )

        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        loop_idx = 0
        for f in bm.faces:
            f_norm_w = (rot_mat @ f.normal).normalized()
            up_weight = float(np.clip(f_norm_w.z * 0.45 + 0.65, 0.25, 1.0))
            min_edge_len = float(face_widths[f.index])
            # Confine G (convex edge wear) to bevel strips, ridges, bolts, and narrow chamfers
            if min_edge_len <= wear_full:
                face_wear_mask = 1.0
            elif min_edge_len < wear_fade:
                face_wear_mask = (wear_fade - min_edge_len) / (wear_fade - wear_full)
            else:
                face_wear_mask = 0.0

            face_ao_vals = [
                float(ao_loop[loop_idx + k]) for k in range(len(f.loops))
            ]
            face_ao_floor = (
                float(np.percentile(face_ao_vals, 72)) * 0.94
                if min_edge_len >= wear_fade and max(face_ao_vals) > 0.72
                else 0.0
            )
            cavity_face_scale = 0.22 if min_edge_len >= wear_fade else 0.95

            for l in f.loops:
                vi = l.vert.index
                w_pos = world_mat @ l.vert.co
                ao_val = max(float(ao_loop[loop_idx]), face_ao_floor)

                # R: Ambient Occlusion (1 = open)
                r_chan = float(np.clip(0.08 + 0.92 * (ao_val ** 1.15), 0.0, 1.0))

                # G: Convex edge wear (0 = none on flat panels, bright on convex bevels/ridges)
                g_chan = float(
                    np.clip(
                        v_convex_smooth[vi] * face_wear_mask * (0.25 + 0.75 * r_chan),
                        0.0,
                        1.0,
                    )
                )

                # B: Crease / cavity where dust settles (0 = none on open flat panels)
                ground_factor = float(np.clip(1.0 - (w_pos.z / 0.32), 0.0, 1.0)) * 0.22
                ao_cavity = max(0.0, (0.72 - r_chan) / 0.72) * cavity_face_scale
                raw_cavity = max(float(v_concave_smooth[vi]), ao_cavity) * up_weight + ground_factor * (1.0 - 0.6 * r_chan)
                b_chan = float(np.clip(raw_cavity * (1.0 - 0.80 * g_chan), 0.0, 1.0))
                if b_chan < 0.03:
                    b_chan = 0.0
                if g_chan < 0.03:
                    g_chan = 0.0

                rgba[loop_idx, 0] = r_chan
                rgba[loop_idx, 1] = g_chan
                rgba[loop_idx, 2] = b_chan
                rgba[loop_idx, 3] = 1.0
                loop_idx += 1

        bm.free()
        ca.data.foreach_set("color", rgba.reshape(-1))
        me.update()


def bake_rock_vertex_colors(
    root: bpy.types.Object,
    footprint_radius: float = 1.0,
    skirt_height_frac: float = 0.24,
) -> None:
    """Bake rock-specific vertex colours: R = strong crevice/pit/overhang AO, G = subtle chipped fracture-rim wear, B = top dust blanket + basal regolith skirt."""
    mesh_objs = [o for o in collect_hierarchy(root) if o.type == "MESH" and len(o.data.polygons) > 0]
    if not mesh_objs:
        return

    # Temporary ground disc at z = -0.015 (just below ground contact line z = 0) so lower flanks receive realistic ground AO
    bpy.ops.mesh.primitive_circle_add(
        vertices=32,
        radius=footprint_radius * 1.75,
        fill_type="NGON",
        location=(0.0, 0.0, -0.015),
    )
    ground_obj = bpy.context.active_object
    ground_obj.name = "_TempRockGroundAO"

    for obj in mesh_objs + [ground_obj]:
        me = obj.data
        while len(me.color_attributes) > 0:
            me.color_attributes.remove(me.color_attributes[0])
        ca = me.color_attributes.new(
            name=VCOL_ATTRIBUTE_NAME,
            type="FLOAT_COLOR",
            domain="CORNER",
        )
        me.color_attributes.active_color = ca
        me.color_attributes.render_color_index = 0

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 48
    scene.cycles.seed = 0
    scene.cycles.use_animated_seed = False

    if scene.world is None:
        scene.world = bpy.data.worlds.new("R1_BakeWorld")
    scene.world.light_settings.distance = 0.55

    scene.render.bake.target = "VERTEX_COLORS"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_objs[0]
    bpy.ops.object.bake(type="AO")

    ground_mesh = ground_obj.data
    bpy.data.objects.remove(ground_obj, do_unlink=True)
    bpy.data.meshes.remove(ground_mesh)

    for obj in mesh_objs:
        me = obj.data
        num_loops = len(me.loops)
        rgba = np.ones(num_loops * 4, dtype=np.float32)
        ca = me.color_attributes[VCOL_ATTRIBUTE_NAME]
        ca.data.foreach_get("color", rgba)
        rgba = rgba.reshape(-1, 4)
        ao_loop = np.clip(rgba[:, 0].copy(), 0.0, 1.0)

        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bm.edges.ensure_lookup_table()
        bm.faces.ensure_lookup_table()
        bm.normal_update()

        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        w_coords = [world_mat @ v.co for v in bm.verts]
        zs = [p.z for p in w_coords]
        min_z = min(zs) if zs else -0.1
        max_z = max(zs) if zs else 0.5
        above_span = max(0.05, max_z)
        skirt_top_z = above_span * skirt_height_frac

        # Centroid for measuring local inward pit/crevice indentation relative to 1-ring neighbors
        cx = float(np.mean([p.x for p in w_coords]))
        cy = float(np.mean([p.y for p in w_coords]))
        cz = float(np.mean([p.z for p in w_coords]))
        center_w = Vector((cx, cy, cz))
        vert_radii = np.array([(p - center_w).length for p in w_coords], dtype=np.float32)
        avg_radius = max(0.05, float(np.mean(vert_radii)))

        n_verts = len(bm.verts)
        v_convex = np.zeros(n_verts, dtype=np.float32)
        v_concave = np.zeros(n_verts, dtype=np.float32)
        v_inward = np.zeros(n_verts, dtype=np.float32)

        for e in bm.edges:
            if not e.is_manifold:
                continue
            signed_ang = e.calc_face_angle_signed()
            v0, v1 = e.verts[0].index, e.verts[1].index
            if signed_ang > 0.14:
                val = min(1.0, (signed_ang - 0.14) / 0.72)
                if val > v_convex[v0]:
                    v_convex[v0] = val
                if val > v_convex[v1]:
                    v_convex[v1] = val
            elif signed_ang < -0.025:
                val = min(1.0, (-signed_ang - 0.025) / 0.38)
                if val > v_concave[v0]:
                    v_concave[v0] = val
                if val > v_concave[v1]:
                    v_concave[v1] = val

        for v in bm.verts:
            vi = v.index
            nbrs = [e.other_vert(v).index for e in v.link_edges]
            if nbrs:
                mean_nbr_r = float(np.mean(vert_radii[nbrs]))
                inward_diff = (mean_nbr_r - float(vert_radii[vi])) / avg_radius
                if inward_diff > 0.004:
                    v_inward[vi] = min(1.0, inward_diff * 11.5)

        v_convex_smooth = v_convex.copy()
        v_concave_smooth = np.maximum(v_concave, v_inward)
        for v in bm.verts:
            vi = v.index
            nbrs = [e.other_vert(v).index for e in v.link_edges]
            if nbrs:
                v_convex_smooth[vi] = max(v_convex[vi], 0.65 * float(np.mean(v_convex[nbrs])))
                v_concave_smooth[vi] = max(
                    v_concave_smooth[vi],
                    0.72 * float(np.mean(v_concave_smooth[nbrs])),
                )

        loop_idx = 0
        for f in bm.faces:
            f_norm_w = (rot_mat @ f.normal).normalized()
            for l in f.loops:
                vi = l.vert.index
                w_pos = w_coords[vi]
                v_norm_w = (rot_mat @ l.vert.normal).normalized()
                # Blend face and vertex normal so smooth lobes and crisp fracture steps both respond
                n_w = (f_norm_w * 0.45 + v_norm_w * 0.55).normalized()

                ao_val = float(ao_loop[loop_idx])
                h_above = float(w_pos.z)
                h_01 = float(np.clip((h_above - min_z) / max(0.05, max_z - min_z), 0.0, 1.0))

                # 1. Basal regolith skirt factor around ground contact (z in [-0.04, skirt_top_z])
                if h_above <= skirt_top_z and n_w.z > -0.25:
                    skirt_t = float(np.clip(1.0 - (max(0.0, h_above) / max(0.02, skirt_top_z)), 0.0, 1.0))
                    skirt_factor = skirt_t * skirt_t * (3.0 - 2.0 * skirt_t)
                else:
                    skirt_factor = 0.0

                # 2. Crevice & vesicle pit occlusion
                crevice_occ = float(np.clip(v_concave_smooth[vi] * 0.88, 0.0, 0.86))
                overhang_occ = float(np.clip((-n_w.z - 0.08) * 0.68, 0.0, 0.56))

                raw_ao = (0.14 + 0.86 * (ao_val ** 1.15)) * (1.0 - crevice_occ) * (1.0 - overhang_occ)
                # Keep the basal regolith skirt bright enough to blend seamlessly into the ground regolith
                r_chan = float(np.clip(max(raw_ao, skirt_factor * 0.78 * (1.0 - 0.5 * crevice_occ)), 0.08, 1.0))

                # 3. Intermittent chipped fracture-rim wear (G) only on upper/side fracture ridges, never on skirt or pits
                chip_hash = 0.5 + 0.5 * math.sin(w_pos.x * 11.3 + w_pos.y * 9.7 + w_pos.z * 13.1)
                if h_01 > 0.34 and skirt_factor < 0.25 and crevice_occ < 0.22:
                    g_chan = float(
                        np.clip(
                            v_convex_smooth[vi] * (0.35 + 0.55 * chip_hash) * r_chan * 0.68,
                            0.0,
                            0.58,
                        )
                    )
                else:
                    g_chan = 0.0

                # 4. Dust blanket on top crown + basal regolith skirt + upward-facing shallow pockets (B)
                top_up = float(np.clip((n_w.z - 0.18) / 0.62, 0.0, 1.0))
                top_height = float(np.clip((h_01 - 0.38) / 0.38, 0.0, 1.0))
                dust_mod = 0.82 + 0.18 * math.cos(w_pos.x * 6.4 - w_pos.y * 5.8 + w_pos.z * 4.2)
                top_dust = (top_up ** 1.15) * top_height * dust_mod * (1.0 - 0.72 * crevice_occ)
                pocket_dust = (
                    float(np.clip(v_concave_smooth[vi] * 0.62, 0.0, 0.58))
                    * float(np.clip((n_w.z - 0.38) / 0.45, 0.0, 1.0))
                )
                skirt_dust = skirt_factor * (0.86 + 0.14 * dust_mod) * (1.0 - 0.45 * crevice_occ)

                b_chan = float(np.clip(max(top_dust, skirt_dust, pocket_dust) * (1.0 - 0.65 * g_chan), 0.0, 1.0))
                # Ensure deep side/overhanging crevices have zero dust so they stay dark in sunlight
                if n_w.z < 0.12 and skirt_factor < 0.15:
                    b_chan *= max(0.0, (n_w.z + 0.05) / 0.17) * (1.0 - crevice_occ)

                if b_chan < 0.03:
                    b_chan = 0.0
                if g_chan < 0.03:
                    g_chan = 0.0

                rgba[loop_idx, 0] = r_chan
                rgba[loop_idx, 1] = g_chan
                rgba[loop_idx, 2] = b_chan
                rgba[loop_idx, 3] = 1.0
                loop_idx += 1

        bm.free()
        ca.data.foreach_set("color", rgba.reshape(-1))
        me.update()


def _install_deterministic_fbx_hooks() -> None:
    """Ensure Blender's binary FBX exporter uses deterministic UUIDs and a fixed creation timestamp."""
    from io_scene_fbx import export_fbx_bin, fbx_utils

    fbx_utils._keys_to_uuids.clear()
    fbx_utils._uuids_to_keys.clear()

    def _deterministic_key_to_uuid(uuids: dict[int, object], key: object) -> fbx_utils.UUID:
        if isinstance(key, int) and 0 <= key < 2**63:
            uuid = key
        else:
            digest = hashlib.blake2b(str(key).encode("utf-8"), digest_size=8).digest()
            uuid = int.from_bytes(digest, "little") & ((1 << 62) - 1)
        if uuid > int(1e9):
            t_uuid = uuid % int(1e9)
            if t_uuid not in uuids:
                uuid = t_uuid
        if uuid in uuids:
            inc = 1 if uuid < 2**62 else -1
            while uuid in uuids:
                uuid += inc
        return fbx_utils.UUID(uuid)

    fbx_utils._key_to_uuid = _deterministic_key_to_uuid

    orig_header = getattr(export_fbx_bin, "_orig_fbx_header_elements", export_fbx_bin.fbx_header_elements)
    export_fbx_bin._orig_fbx_header_elements = orig_header

    def _fixed_header_elements(root_elem: object, scene_data: object, time: object = None) -> None:
        orig_header(root_elem, scene_data, time=FIXED_EXPORT_TIME)

    export_fbx_bin.fbx_header_elements = _fixed_header_elements


def export_fbx(root: bpy.types.Object, filepath: Path) -> None:
    """Export the structure hierarchy rooted at `root` deterministically to FBX for Unity."""
    filepath = Path(filepath).resolve()
    filepath.parent.mkdir(parents=True, exist_ok=True)

    ensure_materials(use_vertex_color_shading=False)
    _install_deterministic_fbx_hooks()

    baked_leaf_rots: list[tuple[bpy.types.Object, Euler, Matrix]] = []
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collect_hierarchy(root):
        obj.hide_viewport = False
        obj.hide_render = False
        if obj.type == "MESH" and not obj.children and obj.data is not None and math.cos(obj.rotation_euler.z) < 0.0:
            saved_rot = obj.rotation_euler.copy()
            rot_mat = saved_rot.to_matrix().to_4x4()
            obj.data.transform(rot_mat)
            obj.data.update()
            obj.rotation_euler = (0.0, 0.0, 0.0)
            baked_leaf_rots.append((obj, saved_rot, rot_mat.inverted()))
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        check_existing=False,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_space_transform=True,
        bake_space_transform=False,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        use_mesh_modifiers_render=True,
        mesh_smooth_type="OFF",
        colors_type="SRGB",
        prioritize_active_color=True,
        use_subsurf=False,
        use_mesh_edges=False,
        use_tspace=False,
        use_triangles=False,
        use_custom_props=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
        axis_forward="-Z",
        axis_up="Y",
    )

    for obj, saved_rot, inv_mat in baked_leaf_rots:
        obj.data.transform(inv_mat)
        obj.data.update()
        obj.rotation_euler = saved_rot
    bpy.context.view_layer.update()


def _setup_preview_lighting_and_plinth(footprint_radius: float) -> list[bpy.types.Object]:
    """Create lunar regolith plinth, low sun light, cool earthshine fill, and dark lunar sky for renders."""
    temp_objs: list[bpy.types.Object] = []

    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("R1_PreviewWorld")
    scene.world.use_nodes = True
    w_nt = scene.world.node_tree
    w_nt.nodes.clear()
    w_out = w_nt.nodes.new("ShaderNodeOutputWorld")
    w_bg = w_nt.nodes.new("ShaderNodeBackground")
    w_bg.inputs["Color"].default_value = _srgb_to_linear("#12172E")
    w_bg.inputs["Strength"].default_value = 0.85
    w_nt.links.new(w_bg.outputs["Background"], w_out.inputs["Surface"])

    # Lunar regolith plinth disc
    mb = MeshBuilder("_PreviewPlinth", pivot_u=(0.0, -0.06, 0.0))
    mb.add_cylinder(
        center_u=(0.0, -0.06, 0.0),
        radius=footprint_radius * 1.38,
        length=0.12,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=48,
        bevel=0.02,
        bevel_segments=2,
    )
    plinth_obj = mb.build_object()
    plinth_mat = bpy.data.materials.new("_PreviewPlinthMat")
    plinth_mat.use_nodes = True
    p_bsdf = plinth_mat.node_tree.nodes["Principled BSDF"]
    p_bsdf.inputs["Base Color"].default_value = _srgb_to_linear("#6E7077")
    p_bsdf.inputs["Roughness"].default_value = 0.88
    p_bsdf.inputs["Metallic"].default_value = 0.02
    plinth_obj.data.materials.clear()
    plinth_obj.data.materials.append(plinth_mat)
    temp_objs.append(plinth_obj)

    # Key sun light (warm low polar sun)
    sun_data = bpy.data.lights.new(name="_KeySun", type="SUN")
    sun_data.energy = 3.8
    sun_data.color = _srgb_to_linear("#FFF2DC")[:3]
    sun_data.angle = math.radians(4.0)
    sun_obj = bpy.data.objects.new("_KeySun", sun_data)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = Euler((math.radians(48.0), math.radians(-12.0), math.radians(36.0)), "XYZ")
    temp_objs.append(sun_obj)

    # Cool earthshine fill light
    fill_data = bpy.data.lights.new(name="_EarthFill", type="SUN")
    fill_data.energy = 1.15
    fill_data.color = _srgb_to_linear("#78A4E8")[:3]
    fill_data.angle = math.radians(25.0)
    fill_obj = bpy.data.objects.new("_EarthFill", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = Euler((math.radians(60.0), math.radians(15.0), math.radians(-135.0)), "XYZ")
    temp_objs.append(fill_obj)

    # Rim light
    rim_data = bpy.data.lights.new(name="_RimLight", type="SUN")
    rim_data.energy = 1.4
    rim_data.color = _srgb_to_linear("#FFE3B8")[:3]
    rim_data.angle = math.radians(8.0)
    rim_obj = bpy.data.objects.new("_RimLight", rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = Euler((math.radians(70.0), 0.0, math.radians(165.0)), "XYZ")
    temp_objs.append(rim_obj)

    return temp_objs


def _aim_camera_at(
    cam_obj: bpy.types.Object,
    cam_pos_u: tuple[float, float, float],
    target_u: tuple[float, float, float],
    lens_mm: float = 45.0,
    ortho_scale: float | None = None,
) -> None:
    pos_b = u_pos(*cam_pos_u)
    tgt_b = u_pos(*target_u)
    cam_obj.location = pos_b
    direction = (tgt_b - pos_b).normalized()
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler("XYZ")
    cam_data = cam_obj.data
    if ortho_scale is not None:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho_scale
    else:
        cam_data.type = "PERSP"
        cam_data.lens = lens_mm


def _render_to_file(filepath: Path, res_x: int = 960, res_y: int = 720, freestyle: bool = True) -> None:
    filepath = Path(filepath).resolve()
    filepath.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.use_freestyle = freestyle
    if freestyle and scene.view_layers:
        vl = scene.view_layers[0]
        vl.use_freestyle = True
        fs = vl.freestyle_settings
        if not fs.linesets:
            ls = fs.linesets.new("R1_Outline")
        else:
            ls = fs.linesets[0]
        if ls.linestyle is None:
            ls.linestyle = bpy.data.linestyles.get("R1_LineStyle") or bpy.data.linestyles.new("R1_LineStyle")
        ls.select_silhouette = True
        ls.select_border = True
        ls.select_crease = True
        fs.crease_angle = math.radians(132.0)
        ls.linestyle.color = (0.09, 0.10, 0.13)
        ls.linestyle.thickness = 1.35
    scene.render.filepath = str(filepath)
    bpy.ops.render.render(write_still=True)


def render_vertex_color_grid(
    root: bpy.types.Object,
    out_path: Path,
    res_x: int = 640,
    res_y: int = 480,
) -> Path:
    """Render a 2x2 vertex-colour inspection sheet (R=AO, G=Wear, B=Cavity, RGB) using the active scene camera."""
    out_path = Path(out_path).resolve()
    out_dir = out_path.parent
    out_dir.mkdir(parents=True, exist_ok=True)

    mesh_objs = [o for o in collect_hierarchy(root) if o.type == "MESH"]
    saved_mats: dict[bpy.types.Object, list[bpy.types.Material]] = {}
    for obj in mesh_objs:
        saved_mats[obj] = list(obj.data.materials)

    vcol_mat = bpy.data.materials.new("_VColInspectMat")
    vcol_mat.use_nodes = True
    v_nt = vcol_mat.node_tree
    v_nt.nodes.clear()
    v_out = v_nt.nodes.new("ShaderNodeOutputMaterial")
    v_emit = v_nt.nodes.new("ShaderNodeEmission")
    v_attr = v_nt.nodes.new("ShaderNodeVertexColor")
    v_attr.layer_name = VCOL_ATTRIBUTE_NAME
    v_sep = v_nt.nodes.new("ShaderNodeSeparateColor")
    v_comb = v_nt.nodes.new("ShaderNodeCombineColor")
    v_nt.links.new(v_attr.outputs["Color"], v_sep.inputs["Color"])
    v_nt.links.new(v_emit.outputs["Emission"], v_out.inputs["Surface"])

    for obj in mesh_objs:
        obj.data.materials.clear()
        obj.data.materials.append(vcol_mat)

    prev_vt = bpy.context.scene.view_settings.view_transform
    bpy.context.scene.view_settings.view_transform = "Standard"
    channel_imgs: list[np.ndarray] = []
    for ch_mode in ("RGB", "Red", "Green", "Blue"):
        for link in list(v_nt.links):
            if link.to_node in (v_emit, v_comb):
                v_nt.links.remove(link)
        if ch_mode == "RGB":
            v_nt.links.new(v_attr.outputs["Color"], v_emit.inputs["Color"])
        else:
            v_nt.links.new(v_sep.outputs[ch_mode], v_comb.inputs["Red"])
            v_nt.links.new(v_sep.outputs[ch_mode], v_comb.inputs["Green"])
            v_nt.links.new(v_sep.outputs[ch_mode], v_comb.inputs["Blue"])
            v_nt.links.new(v_comb.outputs["Color"], v_emit.inputs["Color"])

        tmp_path = out_dir / f"_tmp_vcol_{ch_mode}.png"
        _render_to_file(tmp_path, res_x=res_x, res_y=res_y, freestyle=False)
        img = bpy.data.images.load(str(tmp_path))
        w, h = img.size
        buf = np.zeros(w * h * 4, dtype=np.float32)
        img.pixels.foreach_get(buf)
        bpy.data.images.remove(img)
        tmp_path.unlink(missing_ok=True)
        channel_imgs.append(buf.reshape(h, w, 4))
    bpy.context.scene.view_settings.view_transform = prev_vt

    h, w, _ = channel_imgs[0].shape
    grid = np.zeros((h * 2, w * 2, 4), dtype=np.float32)
    grid[h : 2 * h, 0:w, :] = channel_imgs[1]       # Top-left: R (AO)
    grid[h : 2 * h, w : 2 * w, :] = channel_imgs[2] # Top-right: G (Edge wear)
    grid[0:h, 0:w, :] = channel_imgs[3]             # Bottom-left: B (Crease/cavity)
    grid[0:h, w : 2 * w, :] = channel_imgs[0]       # Bottom-right: RGB combined

    comp_img = bpy.data.images.new("_CompVCol", width=w * 2, height=h * 2, alpha=True)
    comp_img.pixels.foreach_set(grid.reshape(-1))
    comp_img.filepath_raw = str(out_path)
    comp_img.file_format = "PNG"
    comp_img.save()
    bpy.data.images.remove(comp_img)

    for obj, mats_list in saved_mats.items():
        obj.data.materials.clear()
        for m in mats_list:
            obj.data.materials.append(m)
    if vcol_mat.name in bpy.data.materials:
        bpy.data.materials.remove(vcol_mat)
    return out_path


def render_structure_suite(
    root: bpy.types.Object,
    slug: str,
    out_dir: Path,
    target_height: float,
    footprint_radius: float,
    pose_rest_fn: Callable[[bpy.types.Object], None],
    pose_active_fn: Callable[[bpy.types.Object], None],
    pose_rest_name: str = "pose_rest",
    pose_active_name: str = "pose_active",
    cam_side_x: float = 1.0,
    concept_path: Path | None = None,
    use_active_for_compare: bool = True,
) -> list[Path]:
    """Render all required views, poses, build stages, wireframe, R/G/B vertex-colour sheet, and optional concept comparison."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    rendered_paths: list[Path] = []

    ensure_materials(use_vertex_color_shading=True)
    temp_objs = _setup_preview_lighting_and_plinth(footprint_radius)

    cam_data = bpy.data.cameras.new("_PreviewCam")
    cam_obj = bpy.data.objects.new("_PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.append(cam_obj)

    mid_y = target_height * 0.48
    dist = max(target_height * 2.28, footprint_radius * 2.95)
    sx = -1.0 if cam_side_x < 0.0 else 1.0

    # Ensure all stages visible and in rest/default display pose
    stages = sorted(
        [c for c in root.children if c.name.startswith("Stage_")],
        key=lambda o: o.name,
    )
    for obj in collect_hierarchy(root):
        obj.hide_render = False
        obj.hide_viewport = False

    pose_rest_fn(root)
    bpy.context.view_layer.update()

    # 1. Front, Side, Three-Quarter, Top views
    views = {
        "front": ((0.0, mid_y + 0.25, dist * 1.05), (0.0, mid_y, 0.0), 48.0, None),
        "side": ((sx * dist * 1.05, mid_y + 0.25, 0.0), (0.0, mid_y, 0.0), 48.0, None),
        "three_quarter": ((sx * dist * 0.76, mid_y + dist * 0.34, dist * 0.78), (0.0, mid_y, 0.0), 48.0, None),
        "top": ((0.0, dist * 1.45, 0.08), (0.0, 0.0, 0.0), 50.0, max(footprint_radius * 2.55, target_height * 1.5)),
    }
    for v_name, (c_pos, c_tgt, lens, ortho) in views.items():
        _aim_camera_at(cam_obj, c_pos, c_tgt, lens_mm=lens, ortho_scale=ortho)
        p = out_dir / f"{slug}_{v_name}.png"
        _render_to_file(p)
        rendered_paths.append(p)

    # 2. Moving parts in both end positions (from a clear hero 3/4 angle)
    _aim_camera_at(
        cam_obj,
        (sx * dist * 0.74, mid_y + dist * 0.30, dist * 0.76),
        (0.0, mid_y, 0.0),
        lens_mm=48.0,
    )
    pose_rest_fn(root)
    bpy.context.view_layer.update()
    p_rest = out_dir / f"{slug}_{pose_rest_name}.png"
    _render_to_file(p_rest)
    rendered_paths.append(p_rest)

    pose_active_fn(root)
    bpy.context.view_layer.update()
    p_act = out_dir / f"{slug}_{pose_active_name}.png"
    _render_to_file(p_act)
    rendered_paths.append(p_act)

    # Restore rest pose
    pose_rest_fn(root)
    bpy.context.view_layer.update()

    # 3. Build stages (stage 1 only, stages 1-2, ...)
    for max_idx in range(len(stages)):
        for s_idx, stg in enumerate(stages):
            hide = s_idx > max_idx
            for sub in collect_hierarchy(stg):
                sub.hide_render = hide
        stage_label = f"stage_1" if max_idx == 0 else f"stage_1_{max_idx + 1}"
        p_stg = out_dir / f"{slug}_{stage_label}.png"
        _render_to_file(p_stg)
        rendered_paths.append(p_stg)

    for obj in collect_hierarchy(root):
        obj.hide_render = False

    # 4. Wireframe view (clay material + wireframe modifier on all meshes)
    wire_mat = bpy.data.materials.new("_WireOverlayMat")
    wire_mat.use_nodes = True
    w_bsdf = wire_mat.node_tree.nodes["Principled BSDF"]
    w_bsdf.inputs["Base Color"].default_value = _srgb_to_linear("#161A24")
    w_bsdf.inputs["Roughness"].default_value = 0.9

    clay_mat = bpy.data.materials.new("_ClayMat")
    clay_mat.use_nodes = True
    c_bsdf = clay_mat.node_tree.nodes["Principled BSDF"]
    c_bsdf.inputs["Base Color"].default_value = _srgb_to_linear("#D8D3C8")
    c_bsdf.inputs["Roughness"].default_value = 0.55

    saved_mats: dict[bpy.types.Object, list[bpy.types.Material]] = {}
    mesh_objs = [o for o in collect_hierarchy(root) if o.type == "MESH"]
    for obj in mesh_objs:
        saved_mats[obj] = list(obj.data.materials)
        obj.data.materials.clear()
        obj.data.materials.append(clay_mat)
        obj.data.materials.append(wire_mat)
        for poly in obj.data.polygons:
            poly.material_index = 0
        wm = obj.modifiers.new("_PreviewWire", "WIREFRAME")
        wm.thickness = 0.0045
        wm.use_replace = False
        wm.material_offset = 1

    p_wire = out_dir / f"{slug}_wireframe.png"
    _render_to_file(p_wire, freestyle=False)
    rendered_paths.append(p_wire)

    for obj in mesh_objs:
        if "_PreviewWire" in obj.modifiers:
            obj.modifiers.remove(obj.modifiers["_PreviewWire"])

    for obj, mats_list in saved_mats.items():
        obj.data.materials.clear()
        for m in mats_list:
            obj.data.materials.append(m)

    # 5. Vertex-colour view showing R (AO), G (Edge Wear), B (Cavity/Dust), and RGB
    p_vcol = render_vertex_color_grid(root, out_dir / f"{slug}_vertex_color.png")
    rendered_paths.append(p_vcol)

    # 6. Optional side-by-side comparison with concept (`Logs/renders/compare_<slug>.png`)
    if concept_path is not None and Path(concept_path).exists():
        rhs_path = p_act if use_active_for_compare else (out_dir / f"{slug}_three_quarter.png")
        p_comp = compose_side_by_side(
            left_path=Path(concept_path),
            right_path=rhs_path,
            out_path=out_dir / f"compare_{slug}.png",
            target_height=768,
        )
        rendered_paths.append(p_comp)

    # Cleanup temporary objects
    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    ground_mat = bpy.data.materials.get("_PreviewGroundMat")
    for m_tmp in (wire_mat, clay_mat, ground_mat):
        if m_tmp is not None and m_tmp.name in bpy.data.materials:
            bpy.data.materials.remove(m_tmp)

    ensure_materials(use_vertex_color_shading=False)
    return rendered_paths


def _init_solid_vcol(obj: bpy.types.Object, rgba: tuple[float, float, float, float] = (1.0, 0.0, 0.0, 1.0)) -> None:
    """Initialize a uniform FLOAT_COLOR CORNER attribute on a helper mesh."""
    me = obj.data
    while len(me.color_attributes) > 0:
        me.color_attributes.remove(me.color_attributes[0])
    ca = me.color_attributes.new(
        name=VCOL_ATTRIBUTE_NAME,
        type="FLOAT_COLOR",
        domain="CORNER",
    )
    me.color_attributes.active_color = ca
    me.color_attributes.render_color_index = 0
    arr = np.tile(np.array(rgba, dtype=np.float32), len(me.loops))
    ca.data.foreach_set("color", arr)


def build_rover_07_standin(
    pos_u: tuple[float, float, float],
    rover_width: float = 1.52,
    rover_height: float = 1.55,
    rover_length: float = 2.04,
    yaw_deg_u: float = 0.0,
) -> bpy.types.Object:
    """Build a 1.52 x 1.55 m scale reference stand-in for Rover 07 with R1 belt stripe, sage patch, and CRT visor."""
    rx, ry, rz = pos_u
    mb = MeshBuilder("_Rover07StandIn", pivot_u=pos_u)

    # 4 wheels spanning the exact 1.52 m width
    wheel_r = 0.29
    wheel_w = 0.24
    wheel_x = rover_width * 0.5 - wheel_w * 0.5
    for sx in (-1.0, 1.0):
        for sz in (-0.62, 0.62):
            mb.add_cylinder(
                center_u=(rx + sx * wheel_x, ry + wheel_r, rz + sz),
                radius=wheel_r,
                length=wheel_w,
                mat="R1_Rubber",
                axis_u="X",
                segments=14,
                bevel=0.025,
                bevel_segments=1,
            )
            mb.add_cylinder(
                center_u=(rx + sx * (wheel_x + sx * 0.01), ry + wheel_r, rz + sz),
                radius=wheel_r * 0.52,
                length=wheel_w + 0.01,
                mat="R1_GreyMetal",
                axis_u="X",
                segments=10,
                bevel=0.008,
                bevel_segments=1,
            )

    # Rounded cream chassis body (y = 0.34 .. 0.98 m)
    body_w = rover_width - 0.22
    body_h = 0.62
    body_cy = ry + 0.65
    mb.add_bevel_box(
        center_u=(rx, body_cy, rz),
        size_u=(body_w, body_h, rover_length * 0.92),
        mat="R1_CreamEnamel",
        bevel=0.11,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(rx, body_cy - 0.02, rz),
        size_u=(body_w + 0.016, 0.09, rover_length * 0.92 + 0.016),
        mat="R1_OrangeStripe",
        bevel=0.006,
        segments=1,
    )
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(rx + sx * (body_w * 0.5 + 0.008), body_cy - 0.02, rz - 0.10),
            size_u=(0.02, 0.30, 0.52),
            mat="R1_SagePatch",
            bevel=0.006,
            segments=1,
        )

    # CRT monitor head reaching up to exact rover_height (1.55 m)
    head_h = rover_height - 0.96
    head_cy = ry + 0.96 + head_h * 0.5
    head_z = rz + 0.26
    mb.add_bevel_box(
        center_u=(rx, head_cy, head_z),
        size_u=(0.76, head_h, 0.68),
        mat="R1_CreamEnamel",
        bevel=0.08,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(rx, head_cy, head_z + 0.34),
        size_u=(0.56, head_h * 0.68, 0.04),
        mat="R1_DarkGlass",
        bevel=0.01,
        segments=1,
    )
    for eye_x in (-0.12, 0.12):
        mb.add_bevel_box(
            center_u=(rx + eye_x, head_cy + 0.02, head_z + 0.355),
            size_u=(0.085, 0.11, 0.02),
            mat="R1_LampAmber",
            bevel=0.005,
            segments=1,
        )
    rover_box = mb.build_object()
    rover_box.rotation_euler = (0.0, 0.0, math.radians(-yaw_deg_u))
    _init_solid_vcol(rover_box, (0.94, 0.06, 0.0, 1.0))
    return rover_box


def compose_side_by_side(
    left_path: Path,
    right_path: Path,
    out_path: Path,
    target_height: int = 768,
    divider_px: int = 8,
) -> Path:
    """Compose two PNG images side-by-side at matching height `target_height` using Blender image buffers."""
    left_path = Path(left_path).resolve()
    right_path = Path(right_path).resolve()
    out_path = Path(out_path).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)

    def _load_resized(p: Path) -> np.ndarray:
        img = bpy.data.images.load(str(p))
        orig_w, orig_h = img.size
        new_w = max(1, int(round(orig_w * (target_height / orig_h))))
        img.scale(new_w, target_height)
        buf = np.zeros(new_w * target_height * 4, dtype=np.float32)
        img.pixels.foreach_get(buf)
        bpy.data.images.remove(img)
        return buf.reshape(target_height, new_w, 4)

    arr_l = _load_resized(left_path)
    arr_r = _load_resized(right_path)
    h, wl, _ = arr_l.shape
    _, wr, _ = arr_r.shape
    total_w = wl + divider_px + wr

    canvas = np.zeros((h, total_w, 4), dtype=np.float32)
    canvas[:, :, 0:3] = 0.04
    canvas[:, :, 3] = 1.0
    canvas[:, 0:wl, :] = arr_l
    canvas[:, wl + divider_px : total_w, :] = arr_r

    comp = bpy.data.images.new("_SideBySide", width=total_w, height=h, alpha=True)
    comp.pixels.foreach_set(canvas.reshape(-1))
    comp.filepath_raw = str(out_path)
    comp.file_format = "PNG"
    comp.save()
    bpy.data.images.remove(comp)
    return out_path


def compose_before_after_grid(
    before_paths: Sequence[Path],
    after_paths: Sequence[Path],
    out_path: Path,
    cell_height: int = 540,
    divider_px: int = 8,
) -> Path:
    """Compose a 2-row (top=before, bottom=after) x N-column comparison sheet using Blender image buffers."""
    out_path = Path(out_path).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)

    def _load_cell(p: Path) -> np.ndarray:
        img = bpy.data.images.load(str(Path(p).resolve()))
        orig_w, orig_h = img.size
        new_w = max(1, int(round(orig_w * (cell_height / orig_h))))
        img.scale(new_w, cell_height)
        buf = np.zeros(new_w * cell_height * 4, dtype=np.float32)
        img.pixels.foreach_get(buf)
        bpy.data.images.remove(img)
        return buf.reshape(cell_height, new_w, 4)

    top_cells = [_load_cell(p) for p in before_paths]
    bot_cells = [_load_cell(p) for p in after_paths]
    col_widths = [max(t.shape[1], b.shape[1]) for t, b in zip(top_cells, bot_cells)]
    total_w = sum(col_widths) + divider_px * max(0, len(col_widths) - 1)
    total_h = cell_height * 2 + divider_px

    canvas = np.zeros((total_h, total_w, 4), dtype=np.float32)
    canvas[:, :, 0:3] = 0.04
    canvas[:, :, 3] = 1.0

    x_off = 0
    for c_w, top_arr, bot_arr in zip(col_widths, top_cells, bot_cells):
        tw = top_arr.shape[1]
        bw = bot_arr.shape[1]
        # In Blender image buffer, row 0 is bottom of image, so top row is y = cell_height + divider_px .. total_h
        canvas[cell_height + divider_px : total_h, x_off : x_off + tw, :] = top_arr
        canvas[0:cell_height, x_off : x_off + bw, :] = bot_arr
        x_off += c_w + divider_px

    comp = bpy.data.images.new("_BeforeAfterGrid", width=total_w, height=total_h, alpha=True)
    comp.pixels.foreach_set(canvas.reshape(-1))
    comp.filepath_raw = str(out_path)
    comp.file_format = "PNG"
    comp.save()
    bpy.data.images.remove(comp)
    return out_path


def render_family_lineup(
    roots_and_offsets: Sequence[tuple[bpy.types.Object, float] | tuple[bpy.types.Object, float, float, float]],
    out_path: Path,
    rover_height: float = 1.46,
    rover_width: float = 1.52,
    rover_length: float = 2.04,
) -> Path:
    """Render all structures side-by-side (or on a two-tier family shelf) with a 1.46 m stand-in for 07."""
    ensure_materials(use_vertex_color_shading=True)
    multi_tier = any(len(item) > 2 for item in roots_and_offsets)

    for item in roots_and_offsets:
        if len(item) == 2:
            r_obj, x_off = item
            r_obj.location = u_pos(x_off, 0.0, 0.0)
        else:
            r_obj, x_off, y_off, z_off = item  # type: ignore[misc]
            r_obj.location = u_pos(x_off, y_off, z_off)

    plinth_mb = MeshBuilder("_FamilyShelf", pivot_u=(0.0, -0.08, 0.0))
    rover_objs: list[bpy.types.Object] = []

    if multi_tier:
        top_y = max(float(item[2]) for item in roots_and_offsets if len(item) > 2)  # type: ignore[index]
        rover_top = build_rover_07_standin(
            pos_u=(5.40, top_y, 0.0),
            rover_width=rover_width,
            rover_height=rover_height,
            rover_length=rover_length,
            yaw_deg_u=-24.0,
        )
        rover_bot = build_rover_07_standin(
            pos_u=(4.60, 0.0, 0.0),
            rover_width=rover_width,
            rover_height=rover_height,
            rover_length=rover_length,
            yaw_deg_u=-24.0,
        )
        rover_objs.extend([rover_top, rover_bot])
        plinth_mb.add_bevel_box(
            center_u=(-0.10, top_y - 0.08, 0.0),
            size_u=(13.6, 0.16, 3.6),
            mat="R1_GreyMetal",
            bevel=0.03,
            segments=1,
        )
        plinth_mb.add_bevel_box(
            center_u=(0.30, -0.08, 0.0),
            size_u=(12.2, 0.16, 3.6),
            mat="R1_GreyMetal",
            bevel=0.03,
            segments=1,
        )
        cam_pos = (-0.85, top_y * 0.5 + 2.35, 13.8)
        cam_tgt = (-0.10, top_y * 0.5 + 0.72, 0.0)
        lens_mm = 36.0
        res_x, res_y = 1920, 1080
    else:
        rover_box = build_rover_07_standin(
            pos_u=(3.55, 0.0, 0.0),
            rover_width=rover_width,
            rover_height=rover_height,
            rover_length=rover_length,
        )
        rover_objs.append(rover_box)
        plinth_mb.add_bevel_box(
            center_u=(-0.15, -0.08, 0.0),
            size_u=(11.2, 0.16, 4.4),
            mat="R1_GreyMetal",
            bevel=0.03,
            segments=1,
        )
        cam_pos = (-0.10, 2.85, 11.8)
        cam_tgt = (-0.15, 0.88, 0.0)
        lens_mm = 35.0
        res_x, res_y = 1600, 900

    shelf_obj = plinth_mb.build_object()
    _init_solid_vcol(shelf_obj, (0.85, 0.0, 0.15, 1.0))

    temp_objs = _setup_preview_lighting_and_plinth(footprint_radius=0.1)
    # Hide the small circular plinth since we built the rectangular family shelf
    temp_objs[0].hide_render = True

    cam_data = bpy.data.cameras.new("_FamilyCam")
    cam_obj = bpy.data.objects.new("_FamilyCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.extend([*rover_objs, shelf_obj, cam_obj])

    _aim_camera_at(
        cam_obj,
        cam_pos_u=cam_pos,
        target_u=cam_tgt,
        lens_mm=lens_mm,
    )
    _render_to_file(out_path, res_x=res_x, res_y=res_y, freestyle=True)

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    ensure_materials(use_vertex_color_shading=False)
    return Path(out_path).resolve()
