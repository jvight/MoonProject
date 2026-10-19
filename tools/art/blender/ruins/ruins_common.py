"""Shared helpers for R1 Lumen Station ruins models (headless Blender).

Note: Does NOT modify tools/art/blender/r1kit.py on disk. Dynamically registers
R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime, provides deterministic
UV mapping onto the 512x512 R1_Signage atlas and AirlockPanel R1_Screen display,
high-segment smooth domed-cylinder / accordion-bellows / curved-band builders,
vertex-color weathering (top-surface dust + edge wear + dent cavity shading),
and Night/Dawn renders with a dark lunar sky, cool sky fill, and warm grazing key.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Callable, Dict, List, Optional, Sequence, Tuple

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]
R1_TOOLS_DIR = REPO_ROOT / "tools" / "art" / "r1"

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
if str(R1_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(R1_TOOLS_DIR))

import r1kit  # noqa: E402

RUINS_FBX_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Ruins"
RENDERS_DIR = REPO_ROOT / "Logs" / "renders"
SIGNAGE_ALBEDO_PATH = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Textures" / "T_R1_Signage_Albedo.png"
SIGNAGE_EMISSION_PATH = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Textures" / "T_R1_Signage_Emission.png"

# UV rects (u0, v0, u1, v1) in OpenGL/Unity UV space (V=0 bottom, V=1 top)
# matching tools/art/r1/generate_signage_atlas.py
SIGNAGE_UV_CELLS: Dict[str, Tuple[float, float, float, float]] = {
    # Row 3 (Top row, V = 0.75..1.00)
    "LOGO": (0.020, 0.760, 0.230, 0.990),
    "LUMEN": (0.260, 0.760, 0.740, 0.990),
    "EVAC_1988": (0.765, 0.765, 0.985, 0.985),
    # Row 2 (Upper-middle row, V = 0.50..0.75)
    "SEC04_HABITAT": (0.020, 0.515, 0.480, 0.735),
    "AIRLOCK_STATUS": (0.512, 0.515, 0.988, 0.735),
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
_SCREEN_PLATES_BY_MB: Dict[int, List[Tuple[Vector, Vector, Vector, float, float]]] = {}
_SCREEN_PLATES_BY_OBJ: Dict[str, List[Tuple[Vector, Vector, Vector, float, float]]] = {}
_PATCHED_R1KIT = False


def _install_r1kit_ruins_extensions() -> None:
    """Install runtime helpers and material specs onto r1kit in memory without modifying r1kit.py on disk."""
    global _PATCHED_R1KIT

    extra_specs = {
        "R1_Screen": {
            "base_color": r1kit._srgb_to_linear("#1A140E"),
            "wear_color": r1kit._srgb_to_linear("#34281C"),
            "dust_color": r1kit._srgb_to_linear("#5E564C"),
            "metallic": 0.05,
            "roughness": 0.12,
            "emission": r1kit._srgb_to_linear("#FFB24A"),
            "emission_strength": 2.4,
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
        segs: int = 32,
        axis: str = "y",
        mat: str = "R1_BareSteel",
        rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
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
    _SCREEN_PLATES_BY_MB.clear()
    _SCREEN_PLATES_BY_OBJ.clear()
    _install_r1kit_ruins_extensions()
    r1kit.reset_scene()


def _rot_matrix_b(rot_deg_u: Tuple[float, float, float]) -> Matrix:
    return Euler(
        (
            math.radians(rot_deg_u[0]),
            math.radians(rot_deg_u[2]),
            math.radians(-rot_deg_u[1]),
        ),
        "XYZ",
    ).to_matrix()


def add_domed_cylinder(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    radius: float,
    total_length: float,
    dome_depth: float = 0.42,
    mat: str = "R1_CreamEnamel",
    axis: str = "x",
    segs: int = 36,
    barrel_rings: int = 10,
    cap_rings: int = 6,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    clip_min_y_u: Optional[float] = 0.02,
    dent_fn: Optional[Callable[[float, float, float], Tuple[float, float, float]]] = None,
) -> list[bmesh.types.BMFace]:
    """Add a closed, manifold, smooth cylinder (36+ segments) with ellipsoidal domed end caps."""
    axis_u = axis.upper()
    half_len = total_length * 0.5
    d_depth = min(dome_depth, half_len * 0.45)
    half_barrel = max(0.05, half_len - d_depth)

    # Build profile pairs (z_pos, r_val) from -half_len to +half_len
    profile: List[Tuple[float, float]] = []
    # Bottom dome (from near-pole to barrel start)
    for k in range(cap_rings, 0, -1):
        theta = (math.pi * 0.5) * (k / (cap_rings + 0.35))
        z_p = -half_barrel - d_depth * math.sin(theta)
        r_p = radius * math.cos(theta)
        profile.append((z_p, r_p))

    # Barrel rings
    for b in range(barrel_rings + 1):
        t = b / max(1, barrel_rings)
        z_p = -half_barrel + t * (2.0 * half_barrel)
        profile.append((z_p, radius))

    # Top dome (from barrel end to near-pole)
    for k in range(1, cap_rings + 1):
        theta = (math.pi * 0.5) * (k / (cap_rings + 0.35))
        z_p = half_barrel + d_depth * math.sin(theta)
        r_p = radius * math.cos(theta)
        profile.append((z_p, r_p))

    if axis_u == "X":
        align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
    elif axis_u == "Z":
        align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
    else:
        align_mat = Matrix.Identity(3)

    rot_b = _rot_matrix_b(rot_deg_u) @ align_mat
    c_bl = mb._to_local_b(center_u)

    rings: List[List[bmesh.types.BMVert]] = []
    for z_p, r_p in profile:
        ring: List[bmesh.types.BMVert] = []
        for s in range(segs):
            ang = (2.0 * math.pi * s) / segs
            lx = r_p * math.cos(ang)
            ly = r_p * math.sin(ang)
            lz = z_p
            rel_bl = rot_b @ Vector((lx, ly, lz))
            if dent_fn is not None:
                dux, duy, duz = dent_fn(rel_bl.x, rel_bl.z, rel_bl.y)
                rel_bl.x += dux
                rel_bl.z += duy
                rel_bl.y += duz
            v_bl = c_bl + rel_bl
            if clip_min_y_u is not None:
                min_z_bl = clip_min_y_u - mb.pivot_b.z
                if v_bl.z < min_z_bl:
                    v_bl.z = min_z_bl
            ring.append(mb.bm.verts.new(v_bl))
        rings.append(ring)

    faces: List[bmesh.types.BMFace] = []
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for s in range(segs):
            sn = (s + 1) % segs
            faces.append(mb.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    faces.append(mb.bm.faces.new(list(reversed(rings[0]))))
    faces.append(mb.bm.faces.new(rings[-1]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def add_accordion_bellows(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    length: float,
    r_valley: float = 1.30,
    r_peak: float = 1.44,
    num_pleats: int = 5,
    mat: str = "R1_Rubber",
    axis: str = "x",
    segs: int = 36,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    clip_min_y_u: Optional[float] = 0.02,
) -> list[bmesh.types.BMFace]:
    """Add a closed, solid, non-see-through rubber accordion bellows with soft rounded pleats."""
    axis_u = axis.upper()
    half_len = length * 0.5
    r_mid = 0.5 * (r_valley + r_peak) + 0.25 * (r_peak - r_valley)

    profile: List[Tuple[float, float]] = [
        (-half_len, r_valley * 0.88),
        (-half_len + 0.02, r_valley),
    ]
    pleat_span = (length - 0.04) / max(1, num_pleats)
    for p in range(num_pleats):
        z0 = -half_len + 0.02 + p * pleat_span
        profile.extend(
            [
                (z0 + pleat_span * 0.14, r_mid),
                (z0 + pleat_span * 0.32, r_peak),
                (z0 + pleat_span * 0.68, r_peak),
                (z0 + pleat_span * 0.86, r_mid),
                (z0 + pleat_span * 1.00, r_valley),
            ]
        )
    profile.append((half_len, r_valley * 0.88))

    if axis_u == "X":
        align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
    elif axis_u == "Z":
        align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
    else:
        align_mat = Matrix.Identity(3)

    rot_b = _rot_matrix_b(rot_deg_u) @ align_mat
    c_bl = mb._to_local_b(center_u)

    rings: List[List[bmesh.types.BMVert]] = []
    for z_p, r_p in profile:
        ring: List[bmesh.types.BMVert] = []
        for s in range(segs):
            ang = (2.0 * math.pi * s) / segs
            lx = r_p * math.cos(ang)
            ly = r_p * math.sin(ang)
            v_bl = c_bl + (rot_b @ Vector((lx, ly, z_p)))
            if clip_min_y_u is not None:
                min_z_bl = clip_min_y_u - mb.pivot_b.z
                if v_bl.z < min_z_bl:
                    v_bl.z = min_z_bl
            ring.append(mb.bm.verts.new(v_bl))
        rings.append(ring)

    faces: List[bmesh.types.BMFace] = []
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for s in range(segs):
            sn = (s + 1) % segs
            faces.append(mb.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    faces.append(mb.bm.faces.new(list(reversed(rings[0]))))
    faces.append(mb.bm.faces.new(rings[-1]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def add_curved_cyl_band(
    mb: r1kit.MeshBuilder,
    cyl_center_u: Tuple[float, float, float],
    radius: float,
    thickness: float,
    t0: float,
    t1: float,
    ang0_deg: float,
    ang1_deg: float,
    mat: str,
    axis: str = "x",
    segs: int = 36,
    axial_segs: int = 1,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    clip_min_y_u: Optional[float] = 0.02,
    dent_fn: Optional[Callable[[float, float, float], Tuple[float, float, float]]] = None,
) -> list[bmesh.types.BMFace]:
    """Add a closed curved band or full collar on a cylinder using the exact angular grid `segs`."""
    axis_u = axis.upper()
    if axis_u == "X":
        align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
    elif axis_u == "Z":
        align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
    else:
        align_mat = Matrix.Identity(3)

    rot_b = _rot_matrix_b(rot_deg_u) @ align_mat
    c_bl = mb._to_local_b(cyl_center_u)

    r_in = radius - 0.012
    r_out = radius + thickness
    full_circle = (ang1_deg - ang0_deg) >= 359.5

    def make_vert(r_val: float, ang_rad: float, z_val: float) -> bmesh.types.BMVert:
        lx = r_val * math.cos(ang_rad)
        ly = r_val * math.sin(ang_rad)
        lz = z_val
        rel_bl = rot_b @ Vector((lx, ly, lz))
        if dent_fn is not None:
            dux, duy, duz = dent_fn(rel_bl.x, rel_bl.z, rel_bl.y)
            rel_bl.x += dux
            rel_bl.z += duy
            rel_bl.y += duz
        v_bl = c_bl + rel_bl
        if clip_min_y_u is not None:
            min_z_bl = clip_min_y_u - mb.pivot_b.z
            if v_bl.z < min_z_bl:
                v_bl.z = min_z_bl
        return mb.bm.verts.new(v_bl)

    z_steps = [t0 + (t1 - t0) * (k / max(1, axial_segs)) for k in range(axial_segs + 1)]
    faces: List[bmesh.types.BMFace] = []

    if full_circle:
        in_rings: List[List[bmesh.types.BMVert]] = []
        out_rings: List[List[bmesh.types.BMVert]] = []
        for zv in z_steps:
            ir: List[bmesh.types.BMVert] = []
            orr: List[bmesh.types.BMVert] = []
            for s in range(segs):
                ang = (2.0 * math.pi * s) / segs
                ir.append(make_vert(r_in, ang, zv))
                orr.append(make_vert(r_out, ang, zv))
            in_rings.append(ir)
            out_rings.append(orr)

        for k in range(axial_segs):
            for s in range(segs):
                sn = (s + 1) % segs
                faces.append(mb.bm.faces.new([out_rings[k][s], out_rings[k][sn], out_rings[k + 1][sn], out_rings[k + 1][s]]))
                faces.append(mb.bm.faces.new([in_rings[k][sn], in_rings[k][s], in_rings[k + 1][s], in_rings[k + 1][sn]]))
        for s in range(segs):
            sn = (s + 1) % segs
            faces.append(mb.bm.faces.new([out_rings[0][s], in_rings[0][s], in_rings[0][sn], out_rings[0][sn]]))
            faces.append(mb.bm.faces.new([in_rings[-1][s], out_rings[-1][s], out_rings[-1][sn], in_rings[-1][sn]]))
    else:
        # Snap angular subdivisions to match the cylinder's segment density
        span_deg = max(4.0, ang1_deg - ang0_deg)
        arc_segs = max(2, int(round(span_deg / (360.0 / segs))))
        angles = [math.radians(ang0_deg + span_deg * (i / arc_segs)) for i in range(arc_segs + 1)]

        in_grid: List[List[bmesh.types.BMVert]] = []
        out_grid: List[List[bmesh.types.BMVert]] = []
        for zv in z_steps:
            ir = [make_vert(r_in, a, zv) for a in angles]
            orr = [make_vert(r_out, a, zv) for a in angles]
            in_grid.append(ir)
            out_grid.append(orr)

        for k in range(axial_segs):
            for i in range(arc_segs):
                j = i + 1
                faces.append(mb.bm.faces.new([out_grid[k][i], out_grid[k][j], out_grid[k + 1][j], out_grid[k + 1][i]]))
                faces.append(mb.bm.faces.new([in_grid[k][j], in_grid[k][i], in_grid[k + 1][i], in_grid[k + 1][j]]))
            # Left and right angular end-caps
            faces.append(mb.bm.faces.new([in_grid[k][0], out_grid[k][0], out_grid[k + 1][0], in_grid[k + 1][0]]))
            faces.append(mb.bm.faces.new([out_grid[k][-1], in_grid[k][-1], in_grid[k + 1][-1], out_grid[k + 1][-1]]))

        for i in range(arc_segs):
            j = i + 1
            faces.append(mb.bm.faces.new([out_grid[0][i], in_grid[0][i], in_grid[0][j], out_grid[0][j]]))
            faces.append(mb.bm.faces.new([in_grid[-1][i], out_grid[-1][i], out_grid[-1][j], in_grid[-1][j]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)
    return faces


def add_extruded_profile_z(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    pts_xy: Sequence[Tuple[float, float]],
    depth_z: float,
    mat: str,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel: float = 0.008,
) -> list[bmesh.types.BMFace]:
    """Extrude a 2D polygon `(x, y)` in Unity X-Y plane across `[-depth_z/2, +depth_z/2]` along Unity Z."""
    hz = depth_z * 0.5
    rot_b = _rot_matrix_b(rot_deg_u)
    c_bl = mb._to_local_b(center_u)

    front_verts = [mb.bm.verts.new((x, -hz, y)) for x, y in pts_xy]
    back_verts = [mb.bm.verts.new((x, hz, y)) for x, y in pts_xy]

    faces: List[bmesh.types.BMFace] = []
    faces.append(mb.bm.faces.new(list(reversed(front_verts))))
    faces.append(mb.bm.faces.new(back_verts))
    n = len(pts_xy)
    for i in range(n):
        j = (i + 1) % n
        faces.append(mb.bm.faces.new([front_verts[i], front_verts[j], back_verts[j], back_verts[i]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)

    if bevel > 0.0:
        cap_edges = r1kit._unique(list(faces[0].edges) + list(faces[1].edges))
        faces, all_verts = mb._bevel_edges(faces, cap_edges, bevel, 1, mat)
    else:
        all_verts = front_verts + back_verts

    bmesh.ops.rotate(mb.bm, verts=all_verts, cent=(0.0, 0.0, 0.0), matrix=rot_b)
    bmesh.ops.translate(mb.bm, verts=all_verts, vec=c_bl)
    return faces


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

    rot_b = _rot_matrix_b(rot_deg_u)
    n_bl = (rot_b @ Vector((0.0, 1.0, 0.0))).normalized()
    v_bl = (rot_b @ Vector((0.0, 0.0, 1.0))).normalized()
    u_bl = v_bl.cross(n_bl).normalized()

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


def register_screen_uv_plate(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    width: float,
    height: float,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Register an R1_Screen display face so its UVs map cleanly onto [0..1] x [0..1]."""
    rot_b = _rot_matrix_b(rot_deg_u)
    n_bl = (rot_b @ Vector((0.0, 1.0, 0.0))).normalized()
    v_bl = (rot_b @ Vector((0.0, 0.0, 1.0))).normalized()
    u_bl = v_bl.cross(n_bl).normalized()
    c_bl = r1kit.u_pos(*center_u)
    _SCREEN_PLATES_BY_MB.setdefault(id(mb), []).append((c_bl, u_bl, v_bl, width, height))


def to_ruin_object(
    mb: r1kit.MeshBuilder,
    name: str,
    parent: Optional[bpy.types.Object] = None,
    origin_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    shade_smooth: bool = True,
) -> bpy.types.Object:
    """Convert a MeshBuilder to a Blender object and transfer any registered signage/screen plate records."""
    _ = shade_smooth
    mb.name = name
    mb.pivot_u = origin_u
    mb.pivot_b = r1kit.u_pos(*origin_u)
    plates = _SIGNAGE_PLATES_BY_MB.pop(id(mb), None)
    screens = _SCREEN_PLATES_BY_MB.pop(id(mb), None)
    obj = mb.build_object(parent=parent)
    o_vec = r1kit.u_pos(*origin_u)
    if plates:
        _SIGNAGE_PLATES_BY_OBJ[obj.name] = [
            (c_bl - o_vec, u_bl, v_bl, w, h, cell) for (c_bl, u_bl, v_bl, w, h, cell) in plates
        ]
    if screens:
        _SCREEN_PLATES_BY_OBJ[obj.name] = [
            (c_bl - o_vec, u_bl, v_bl, w, h) for (c_bl, u_bl, v_bl, w, h) in screens
        ]
    return obj


def apply_signage_uvs(root: bpy.types.Object) -> None:
    """Map R1_Signage faces onto SIGNAGE_UV_CELLS and registered R1_Screen faces onto [0..1] x [0..1]."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        mat_names = [m.name if m else "" for m in mesh.materials]

        uv_layer = mesh.uv_layers.get("UVMap")
        if uv_layer is None:
            uv_layer = mesh.uv_layers.new(name="UVMap")
        uv_data = uv_layer.data

        if "R1_Signage" in mat_names:
            signage_mat_idx = mat_names.index("R1_Signage")
            plates = _SIGNAGE_PLATES_BY_OBJ.get(obj.name, [])
            if plates:
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

        if "R1_Screen" in mat_names:
            screen_mat_idx = mat_names.index("R1_Screen")
            screens = _SCREEN_PLATES_BY_OBJ.get(obj.name, [])
            for poly in mesh.polygons:
                if poly.material_index != screen_mat_idx:
                    continue
                if screens:
                    p_center = poly.center
                    c_bl, u_bl, v_bl, width, height = min(
                        screens, key=lambda sc: (p_center - sc[0]).length_squared
                    )
                    for li in poly.loop_indices:
                        vi = mesh.loops[li].vertex_index
                        co = mesh.vertices[vi].co
                        rel = co - c_bl
                        s = max(0.01, min(0.99, (rel.dot(u_bl) / max(1e-5, width)) + 0.5))
                        t = max(0.01, min(0.99, (rel.dot(v_bl) / max(1e-5, height)) + 0.5))
                        uv_data[li].uv = (round(s, 5), round(t, 5))
                else:
                    # Non-airlock small screen faces map to a clean glowing corner region
                    for li in poly.loop_indices:
                        uv_data[li].uv = (0.04, 0.50)

        mesh.update()


def convex_box_points_u(
    center_u: Tuple[float, float, float],
    size_u: Tuple[float, float, float],
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    top_scale_xz: Tuple[float, float] = (1.0, 1.0),
    clip_min_y_u: Optional[float] = None,
) -> List[Tuple[float, float, float]]:
    """Return 8 corner points in Unity coordinates for an oriented/tapered convex box (6 quad faces, 12 tris)."""
    cx, cy, cz = center_u
    hx, hy, hz = size_u[0] * 0.5, size_u[1] * 0.5, size_u[2] * 0.5
    rot_b = _rot_matrix_b(rot_deg_u)
    pts: List[Tuple[float, float, float]] = []
    for sy in (-1.0, 1.0):
        tx = top_scale_xz[0] if sy > 0.0 else 1.0
        tz = top_scale_xz[1] if sy > 0.0 else 1.0
        for sx in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                local_b = Vector((sx * hx * tx, sz * hz * tz, sy * hy))
                rb = rot_b @ local_b
                ux = cx + rb.x
                uy = cy + rb.z
                uz = cz + rb.y
                if clip_min_y_u is not None and uy < clip_min_y_u:
                    uy = clip_min_y_u
                pts.append((ux, uy, uz))
    return pts


def convex_prism_points_u(
    center_u: Tuple[float, float, float],
    radius_a: float,
    radius_b: float,
    length: float,
    axis: str = "x",
    segs: int = 8,
    rot_deg_u: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    clip_min_y_u: Optional[float] = None,
    scale_pos_end: Tuple[float, float] = (1.0, 1.0),
) -> List[Tuple[float, float, float]]:
    """Return 2*segs points in Unity coordinates for a convex cylinder/frustum prism (segs=8 -> 10 faces, 28 tris)."""
    axis_u = axis.upper()
    if axis_u == "X":
        align_mat = Matrix.Rotation(math.pi / 2.0, 3, "Y")
    elif axis_u == "Z":
        align_mat = Matrix.Rotation(-math.pi / 2.0, 3, "X")
    else:
        align_mat = Matrix.Identity(3)

    rot_b = _rot_matrix_b(rot_deg_u) @ align_mat
    cx, cy, cz = center_u
    half_len = length * 0.5
    pts: List[Tuple[float, float, float]] = []
    for end_sign in (-1.0, 1.0):
        sa = scale_pos_end[0] if end_sign > 0.0 else 1.0
        sb = scale_pos_end[1] if end_sign > 0.0 else 1.0
        lz = end_sign * half_len
        for s in range(segs):
            ang = (2.0 * math.pi * s) / segs + (math.pi / segs)
            lx = radius_a * sa * math.cos(ang)
            ly = radius_b * sb * math.sin(ang)
            rb = rot_b @ Vector((lx, ly, lz))
            ux = cx + rb.x
            uy = cy + rb.z
            uz = cz + rb.y
            if clip_min_y_u is not None and uy < clip_min_y_u:
                uy = clip_min_y_u
            pts.append((ux, uy, uz))
    return pts


def convex_strut_points_u(
    p0_u: Tuple[float, float, float],
    p1_u: Tuple[float, float, float],
    half_w: float,
    half_d: float,
    clip_min_y_u: Optional[float] = None,
) -> List[Tuple[float, float, float]]:
    """Return 8 corner points in Unity coordinates for an oriented rectangular beam connecting p0_u and p1_u."""
    v0 = Vector(p0_u)
    v1 = Vector(p1_u)
    spine = (v1 - v0).normalized()
    ref = Vector((0.0, 0.0, 1.0)) if abs(spine.z) < 0.9 else Vector((1.0, 0.0, 0.0))
    right = ref.cross(spine).normalized()
    up = spine.cross(right).normalized()
    pts: List[Tuple[float, float, float]] = []
    for end_v in (v0, v1):
        for sr in (-1.0, 1.0):
            for su in (-1.0, 1.0):
                p = end_v + right * (sr * half_w) + up * (su * half_d)
                uy = max(clip_min_y_u, p.y) if clip_min_y_u is not None else p.y
                pts.append((p.x, uy, p.z))
    return pts


def add_convex_proxy(
    root: bpy.types.Object,
    name: str,
    points_u: Sequence[Tuple[float, float, float]],
    mat: str = "R1_GreyMetal",
) -> bpy.types.Object:
    """Build a closed, outward-wound convex hull collision proxy (`UCX_<Part>_<n>`) with <= 32 faces."""
    if not name.startswith("UCX_"):
        raise ValueError(f"Collision proxy name '{name}' must start with 'UCX_'.")
    if len(points_u) < 4:
        raise ValueError(f"Collision proxy '{name}' requires at least 4 non-coplanar points.")

    bm = bmesh.new()
    for ux, uy, uz in points_u:
        bm.verts.new(r1kit.u_pos(ux, uy, uz))
    bm.verts.ensure_lookup_table()

    res = bmesh.ops.convex_hull(bm, input=list(bm.verts))
    hull_geom = set(res.get("geom", []))
    unused = [v for v in list(bm.verts) if v not in hull_geom]
    if unused:
        bmesh.ops.delete(bm, geom=unused, context="VERTS")

    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.002)
    bmesh.ops.dissolve_limit(
        bm,
        angle_limit=math.radians(2.5),
        use_dissolve_boundaries=False,
        verts=list(bm.verts),
        edges=list(bm.edges),
    )
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    r1kit.ensure_outward_normals(bm)

    for f in bm.faces:
        f.material_index = 0
        f.smooth = False

    poly_count = len(bm.faces)
    tri_count = sum(max(1, len(f.verts) - 2) for f in bm.faces)
    if poly_count > 32 or tri_count > 32:
        bm.free()
        raise ValueError(
            f"Collision proxy '{name}' exceeds <= 32 faces contract: {poly_count} polygons, {tri_count} triangles."
        )

    non_manifold = [e for e in bm.edges if not e.is_manifold]
    concave = [e for e in bm.edges if e.is_manifold and e.calc_face_angle_signed(0.0) < -1e-3]
    if non_manifold or concave:
        bm.free()
        raise ValueError(
            f"Collision proxy '{name}' is not a valid closed convex hull "
            f"(non_manifold={len(non_manifold)}, concave={len(concave)})."
        )

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()

    mat_dict = r1kit.ensure_materials(use_vertex_color_shading=False)
    mesh.materials.append(mat_dict[mat])
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    obj.parent = root
    r1kit.apply_triplanar_uvs(obj, texel_scale=0.85)
    r1kit._init_solid_vcol(obj, (1.0, 0.0, 0.0, 1.0))
    return obj


def bake_ruins_vertex_colors(root: bpy.types.Object, footprint_radius: float = 3.5) -> None:
    """Bake R1 vertex colors (R=AO, G=EdgeWear, B=RegolithDust, A=1) on render meshes while isolating UCX_* proxies."""
    ucx_objs = [
        o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and o.name.startswith("UCX_")
    ]
    for ucx in ucx_objs:
        ucx.parent = None
        ucx.hide_render = True
        ucx.hide_viewport = True

    try:
        r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)

        objs = [
            o
            for o in r1kit.collect_hierarchy(root)
            if o.type == "MESH" and o.data is not None and not o.name.startswith("UCX_") and not o.name.startswith("Decal_")
        ]
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
            vert_cavity_ao = np.ones(len(bm.verts), dtype=np.float32)

            for v in bm.verts:
                wpos = mw @ v.co
                wnorm = (rot_mw @ v.normal).normalized()
                up_factor = max(0.0, wnorm.z)
                height_01 = max(0.0, min(1.0, (wpos.z - z_min) / z_span))
                top_dust = (up_factor ** 1.35) * (0.56 + 0.24 * (1.0 - height_01))
                vert_top_dust[v.index] = top_dust

                convex_sum = 0.0
                concave_sum = 0.0
                cnt = 0
                for e in v.link_edges:
                    if e.is_manifold:
                        ang = e.calc_face_angle_signed(0.0)
                        if ang > 0.08:
                            convex_sum += min(1.0, ang / 1.05)
                        elif ang < -0.05:
                            concave_sum += min(1.0, (-ang) / 0.85)
                        cnt += 1
                wear = (convex_sum / max(1, cnt)) * 1.25
                if up_factor > 0.25:
                    wear += 0.05
                vert_wear_boost[v.index] = max(0.0, min(1.0, wear))
                if concave_sum > 0.0:
                    vert_cavity_ao[v.index] = max(0.35, 1.0 - (concave_sum / max(1, cnt)) * 0.85)
                    vert_top_dust[v.index] = min(1.0, vert_top_dust[v.index] + (concave_sum / max(1, cnt)) * 0.45)

            bm.free()

            num_loops = len(mesh.loops)
            rgba = np.ones(num_loops * 4, dtype=np.float32)
            col_attr.data.foreach_get("color", rgba)
            rgba = rgba.reshape(-1, 4)

            is_airlock_panel = obj.name == "AirlockPanel"
            mat_names = [m.name if m else "" for m in mesh.materials]
            signage_mat_idx = mat_names.index("R1_Signage") if "R1_Signage" in mat_names else -1

            for poly in mesh.polygons:
                if is_airlock_panel and poly.material_index == signage_mat_idx:
                    for li in poly.loop_indices:
                        rgba[li, 0] = 1.0
                        rgba[li, 1] = 0.0
                        rgba[li, 2] = 0.0
                        rgba[li, 3] = 1.0
                    continue
                poly_wnorm = (rot_mw @ poly.normal).normalized()
                face_up_dust = (max(0.0, poly_wnorm.z) ** 1.4) * 0.50
                for li in poly.loop_indices:
                    vi = mesh.loops[li].vertex_index
                    r, g, b, _ = rgba[li]
                    new_r = max(0.0, min(1.0, float(r) * float(vert_cavity_ao[vi])))
                    new_g = min(1.0, max(float(g), float(vert_wear_boost[vi])))
                    new_b = min(1.0, max(float(b), float(vert_top_dust[vi]), float(face_up_dust)))
                    rgba[li, 0] = new_r
                    rgba[li, 1] = new_g
                    rgba[li, 2] = new_b
                    rgba[li, 3] = 1.0

            col_attr.data.foreach_set("color", rgba.reshape(-1))
            mesh.update()
    finally:
        for ucx in ucx_objs:
            ucx.parent = root
            ucx.hide_render = False
            ucx.hide_viewport = False
            r1kit.apply_triplanar_uvs(ucx, texel_scale=0.85)
            r1kit._init_solid_vcol(ucx, (1.0, 0.0, 0.0, 1.0))


def _draw_np_seg(
    canvas: np.ndarray,
    x0: float,
    y0: float,
    x1: float,
    y1: float,
    half_w: float,
    color: Tuple[float, float, float],
) -> None:
    h, w, _ = canvas.shape
    ix0 = max(0, int(min(x0, x1) - half_w - 1))
    ix1 = min(w - 1, int(max(x0, x1) + half_w + 1))
    iy0 = max(0, int(min(y0, y1) - half_w - 1))
    iy1 = min(h - 1, int(max(y0, y1) + half_w + 1))
    if ix1 < ix0 or iy1 < iy0:
        return
    yy, xx = np.mgrid[iy0 : iy1 + 1, ix0 : ix1 + 1]
    dx = x1 - x0
    dy = y1 - y0
    l2 = dx * dx + dy * dy
    if l2 < 1e-6:
        dist2 = (xx - x0) ** 2 + (yy - y0) ** 2
    else:
        t = np.clip(((xx - x0) * dx + (yy - y0) * dy) / l2, 0.0, 1.0)
        px = x0 + t * dx
        py = y0 + t * dy
        dist2 = (xx - px) ** 2 + (yy - py) ** 2
    mask = dist2 <= (half_w * half_w)
    sub = canvas[iy0 : iy1 + 1, ix0 : ix1 + 1]
    sub[mask] = color


def _get_or_create_airlock_screen_image() -> bpy.types.Image:
    """Create a procedural image for `R1_Screen` displaying `SEALED · 1988 · STANDBY` in warm amber using numpy."""
    img_name = "T_R1_AirlockStandby_Screen"
    existing = bpy.data.images.get(img_name)
    if existing is not None:
        return existing

    glyphs: Dict[str, List[Tuple[float, float, float, float]]] = {
        "A": [(0.08, 0.96, 0.48, 0.04), (0.92, 0.96, 0.52, 0.04), (0.24, 0.64, 0.76, 0.64)],
        "B": [
            (0.12, 0.04, 0.12, 0.96),
            (0.12, 0.08, 0.68, 0.08),
            (0.68, 0.08, 0.84, 0.22),
            (0.84, 0.22, 0.84, 0.36),
            (0.84, 0.36, 0.68, 0.48),
            (0.12, 0.48, 0.68, 0.48),
            (0.68, 0.48, 0.88, 0.62),
            (0.88, 0.62, 0.88, 0.80),
            (0.88, 0.80, 0.68, 0.92),
            (0.12, 0.92, 0.68, 0.92),
        ],
        "D": [
            (0.12, 0.04, 0.12, 0.96),
            (0.12, 0.08, 0.62, 0.08),
            (0.62, 0.08, 0.86, 0.28),
            (0.86, 0.28, 0.86, 0.72),
            (0.86, 0.72, 0.62, 0.92),
            (0.12, 0.92, 0.62, 0.92),
        ],
        "E": [
            (0.14, 0.04, 0.14, 0.96),
            (0.14, 0.08, 0.88, 0.08),
            (0.14, 0.50, 0.74, 0.50),
            (0.14, 0.92, 0.88, 0.92),
        ],
        "L": [(0.14, 0.04, 0.14, 0.96), (0.14, 0.92, 0.88, 0.92)],
        "N": [(0.12, 0.96, 0.12, 0.04), (0.12, 0.08, 0.88, 0.92), (0.88, 0.96, 0.88, 0.04)],
        "S": [
            (0.84, 0.20, 0.68, 0.08),
            (0.68, 0.08, 0.28, 0.08),
            (0.28, 0.08, 0.14, 0.22),
            (0.14, 0.22, 0.14, 0.38),
            (0.14, 0.38, 0.30, 0.48),
            (0.30, 0.48, 0.70, 0.52),
            (0.70, 0.52, 0.86, 0.64),
            (0.86, 0.64, 0.86, 0.80),
            (0.86, 0.80, 0.68, 0.92),
            (0.68, 0.92, 0.28, 0.92),
            (0.28, 0.92, 0.12, 0.78),
        ],
        "T": [(0.08, 0.08, 0.92, 0.08), (0.50, 0.08, 0.50, 0.96)],
        "Y": [(0.10, 0.06, 0.50, 0.52), (0.90, 0.06, 0.50, 0.52), (0.50, 0.52, 0.50, 0.96)],
        "·": [(0.42, 0.50, 0.58, 0.50)],
        "1": [(0.26, 0.22, 0.52, 0.08), (0.52, 0.08, 0.52, 0.92), (0.24, 0.92, 0.78, 0.92)],
        "8": [
            (0.30, 0.08, 0.70, 0.08),
            (0.70, 0.08, 0.84, 0.22),
            (0.84, 0.22, 0.84, 0.38),
            (0.84, 0.38, 0.70, 0.48),
            (0.30, 0.48, 0.70, 0.48),
            (0.30, 0.48, 0.16, 0.38),
            (0.16, 0.38, 0.16, 0.22),
            (0.16, 0.22, 0.30, 0.08),
            (0.70, 0.48, 0.88, 0.62),
            (0.88, 0.62, 0.88, 0.78),
            (0.88, 0.78, 0.70, 0.92),
            (0.70, 0.92, 0.30, 0.92),
            (0.30, 0.92, 0.12, 0.78),
            (0.12, 0.78, 0.12, 0.62),
            (0.12, 0.62, 0.30, 0.48),
        ],
        "9": [
            (0.30, 0.08, 0.70, 0.08),
            (0.70, 0.08, 0.86, 0.24),
            (0.86, 0.24, 0.86, 0.74),
            (0.86, 0.74, 0.68, 0.92),
            (0.68, 0.92, 0.28, 0.92),
            (0.86, 0.50, 0.30, 0.50),
            (0.30, 0.50, 0.14, 0.36),
            (0.14, 0.36, 0.14, 0.24),
            (0.14, 0.24, 0.30, 0.08),
        ],
    }

    w, h = 640, 192
    bg_dark = (0.086, 0.067, 0.051)
    amber_bright = (1.0, 0.73, 0.30)
    amber_dim = (0.72, 0.46, 0.16)

    canvas = np.zeros((h, w, 3), dtype=np.float32)
    canvas[:, :] = bg_dark

    # Outer and inner amber border frames
    for pad, col, hw in ((16.0, amber_dim, 2.0), (24.0, amber_bright, 1.6)):
        _draw_np_seg(canvas, pad, pad, w - pad, pad, hw, col)
        _draw_np_seg(canvas, w - pad, pad, w - pad, h - pad, hw, col)
        _draw_np_seg(canvas, w - pad, h - pad, pad, h - pad, hw, col)
        _draw_np_seg(canvas, pad, h - pad, pad, pad, hw, col)

    text = "SEALED · 1988 · STANDBY"
    char_w = 18.5
    char_h = 56.0
    spacing = 5.5
    total_w = len(text) * char_w + (len(text) - 1) * spacing
    x_start = (w - total_w) * 0.5
    y_top = (h - char_h) * 0.5

    for idx, ch in enumerate(text):
        gx = x_start + idx * (char_w + spacing)
        for sx0, sy0, sx1, sy1 in glyphs.get(ch, []):
            _draw_np_seg(
                canvas,
                gx + sx0 * char_w,
                y_top + sy0 * char_h,
                gx + sx1 * char_w,
                y_top + sy1 * char_h,
                2.7,
                amber_bright,
            )

    arr_flipped = np.flipud(canvas).copy()
    rgba = np.concatenate([arr_flipped, np.ones((h, w, 1), dtype=np.float32)], axis=-1)

    bl_img = bpy.data.images.new(img_name, width=w, height=h, alpha=True)
    bl_img.pixels.foreach_set(rgba.reshape(-1))
    bl_img.pack()
    return bl_img


def attach_signage_texture_for_render(use_vertex_color_shading: bool = True) -> None:
    """Configure R1 materials with vertex-color shading, T_R1_Signage_Albedo.png, and AirlockPanel screen display."""
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

        if name == "R1_Screen":
            screen_tex = nt.nodes.new("ShaderNodeTexImage")
            screen_tex.location = (-420, 120)
            screen_tex.image = _get_or_create_airlock_screen_image()
            screen_tex.extension = "EXTEND"
            nt.links.new(screen_tex.outputs["Color"], bsdf.inputs["Base Color"])
            if "Emission Color" in bsdf.inputs:
                nt.links.new(screen_tex.outputs["Color"], bsdf.inputs["Emission Color"])
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 2.6
            continue

        if use_vertex_color_shading and float(spec["emission_strength"]) == 0.0:
            vcol = nt.nodes.new("ShaderNodeVertexColor")
            vcol.layer_name = r1kit.VCOL_ATTRIBUTE_NAME
            vcol.location = (-700, 0)

            sep = nt.nodes.new("ShaderNodeSeparateColor")
            sep.location = (-500, 0)
            nt.links.new(vcol.outputs["Color"], sep.inputs["Color"])

            wear_mix = nt.nodes.new("ShaderNodeMix")
            wear_mix.data_type = "RGBA"
            wear_mix.location = (-280, 120)
            wear_mix.inputs[6].default_value = spec["base_color"]
            wear_mix.inputs[7].default_value = spec["wear_color"]

            wear_mul = nt.nodes.new("ShaderNodeMath")
            wear_mul.operation = "MULTIPLY"
            wear_mul.inputs[1].default_value = 0.24
            wear_mul.location = (-440, 120)
            nt.links.new(sep.outputs["Green"], wear_mul.inputs[0])
            nt.links.new(wear_mul.outputs["Value"], wear_mix.inputs[0])

            dust_mix = nt.nodes.new("ShaderNodeMix")
            dust_mix.data_type = "RGBA"
            dust_mix.location = (-120, 120)
            dust_mix.inputs[7].default_value = spec["dust_color"]
            nt.links.new(wear_mix.outputs[2], dust_mix.inputs[6])

            dust_mul = nt.nodes.new("ShaderNodeMath")
            dust_mul.operation = "MULTIPLY"
            dust_mul.inputs[1].default_value = 0.24
            dust_mul.location = (-280, -40)
            nt.links.new(sep.outputs["Blue"], dust_mul.inputs[0])
            nt.links.new(dust_mul.outputs["Value"], dust_mix.inputs[0])

            ao_map = nt.nodes.new("ShaderNodeMapRange")
            ao_map.location = (-280, -200)
            ao_map.inputs["From Min"].default_value = 0.0
            ao_map.inputs["From Max"].default_value = 1.0
            ao_map.inputs["To Min"].default_value = 0.54
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
                if SIGNAGE_EMISSION_PATH.exists():
                    em_node = nt.nodes.new("ShaderNodeTexImage")
                    em_node.location = (-680, -320)
                    em_node.image = bpy.data.images.load(str(SIGNAGE_EMISSION_PATH), check_existing=True)
                    if "Emission Color" in bsdf.inputs:
                        nt.links.new(em_node.outputs["Color"], bsdf.inputs["Emission Color"])
                    if "Emission Strength" in bsdf.inputs:
                        bsdf.inputs["Emission Strength"].default_value = 2.4


def _configure_fast_cycles(scene: bpy.types.Scene, res_x: int, res_y: int) -> None:
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    try:
        scene.cycles.use_denoising = True
    except Exception:
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
        fs.crease_angle = math.radians(118.0)
        ls.linestyle.color = (0.06, 0.07, 0.10)
        ls.linestyle.thickness = 1.15
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"


def setup_lunar_world_shader(
    world: bpy.types.World,
    sky_cam_rgba: Tuple[float, float, float, float],
    sky_fill_rgba: Tuple[float, float, float, float],
    fill_strength: float = 0.85,
) -> None:
    """Configure a dark space sky for camera rays while providing a cool blue sky-fill bounce for shadows."""
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()

    out_node = nt.nodes.new("ShaderNodeOutputWorld")
    out_node.location = (420, 0)

    mix_sh = nt.nodes.new("ShaderNodeMixShader")
    mix_sh.location = (200, 0)

    lp = nt.nodes.new("ShaderNodeLightPath")
    lp.location = (-60, 220)

    bg_fill = nt.nodes.new("ShaderNodeBackground")
    bg_fill.location = (-60, 40)
    bg_fill.inputs["Color"].default_value = sky_fill_rgba
    bg_fill.inputs["Strength"].default_value = fill_strength

    bg_cam = nt.nodes.new("ShaderNodeBackground")
    bg_cam.location = (-60, -120)
    bg_cam.inputs["Color"].default_value = sky_cam_rgba
    bg_cam.inputs["Strength"].default_value = 1.0

    nt.links.new(lp.outputs["Is Camera Ray"], mix_sh.inputs["Fac"])
    nt.links.new(bg_fill.outputs["Background"], mix_sh.inputs[1])
    nt.links.new(bg_cam.outputs["Background"], mix_sh.inputs[2])
    nt.links.new(mix_sh.outputs["Shader"], out_node.inputs["Surface"])


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
    dist = max_dim * 1.48 * distance_scale

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
    """Find world-space centers of faces using R1_LampAmber or R1_Screen to place warm local lights."""
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
    cam_angle_deg: float = 32.0,
    cam_elev_deg: float = 18.0,
    res: Tuple[int, int] = (1024, 768),
) -> Tuple[Path, Path]:
    """Render both `<slug>_dawn.png` and `<slug>_night.png` three-quarter previews into `Logs/renders/`."""
    RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    attach_signage_texture_for_render(use_vertex_color_shading=True)

    ucx_objs = [
        o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and o.name.startswith("UCX_")
    ]
    for ucx in ucx_objs:
        ucx.hide_render = True

    scene = bpy.context.scene
    _configure_fast_cycles(scene, res[0], res[1])

    cam_data = bpy.data.cameras.new("RuinPreviewCam")
    cam_data.lens = 42.0
    cam_obj = bpy.data.objects.new("RuinPreviewCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    center, max_dim, min_z = _frame_camera_on_root(cam_obj, root, cam_angle_deg, cam_elev_deg)

    # Lunar regolith ground plane with subtle distant rim rise so horizon reads as lunar terrain, not studio floor
    bpy.ops.mesh.primitive_grid_add(
        x_subdivisions=48,
        y_subdivisions=48,
        size=max_dim * 8.5,
        location=(center.x, center.y, min(0.0, min_z)),
    )
    ground = bpy.context.active_object
    ground.name = "_RuinPreviewGround"
    for v in ground.data.vertices:
        wx = v.co.x
        wy = v.co.y
        r_dist = math.hypot(wx, wy)
        if wy > max_dim * 1.1:
            back_t = (wy - max_dim * 1.1) / (max_dim * 3.2)
            v.co.z += (max(0.0, back_t) ** 1.65) * (max_dim * 0.42) + 0.08 * math.sin(wx * 0.6)
        elif r_dist > max_dim * 1.3:
            v.co.z += 0.04 * math.sin(wx * 0.8) * math.cos(wy * 0.8)
    for poly in ground.data.polygons:
        poly.use_smooth = True
    ground.data.update()

    gmat = bpy.data.materials.new("_RuinGroundMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.16, 0.17, 0.20, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.94
    ground.data.materials.append(gmat)

    world = bpy.data.worlds.new("_RuinPreviewWorld")
    scene.world = world

    sun_data = bpy.data.lights.new("SunKey", type="SUN")
    sun_obj = bpy.data.objects.new("SunKey", sun_data)
    scene.collection.objects.link(sun_obj)

    fill_data = bpy.data.lights.new("SkyFill", type="SUN")
    fill_obj = bpy.data.objects.new("SkyFill", fill_data)
    scene.collection.objects.link(fill_obj)

    rim_data = bpy.data.lights.new("RimLight", type="SUN")
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    scene.collection.objects.link(rim_obj)

    # Local warm point lights for emissives (active in both dawn and night, stronger at night)
    point_lights: List[bpy.types.Object] = []
    emissives = _find_emissive_positions(root)
    if not emissives:
        emissives = [(center + Vector((max_dim * 0.25, -max_dim * 0.25, max_dim * 0.25)), "R1_LampAmber")]

    for idx, (pos, mat_type) in enumerate(emissives):
        pdata = bpy.data.lights.new(f"_PreviewPt_{idx}", type="POINT")
        if mat_type == "R1_LampAmber":
            pdata.color = (1.0, 0.66, 0.24)
            pdata.energy = 22.0
            pdata.shadow_soft_size = 0.28
            pt_offset = Vector((0.0, -0.06, -0.22))
        else:
            pdata.color = (1.0, 0.72, 0.32)
            pdata.energy = 10.0
            pdata.shadow_soft_size = 0.22
            cam_dir = (cam_obj.location - pos).normalized()
            pt_offset = cam_dir * 0.25 + Vector((0.0, 0.0, -0.05))
        pobj = bpy.data.objects.new(f"_PreviewPt_{idx}", pdata)
        pobj.location = pos + pt_offset
        scene.collection.objects.link(pobj)
        point_lights.append(pobj)

    # 1. DAWN RENDER (dark lunar sky, low warm golden key light, cool blue sky fill)
    setup_lunar_world_shader(
        world,
        sky_cam_rgba=(0.0035, 0.0055, 0.0150, 1.0),
        sky_fill_rgba=(0.10, 0.14, 0.26, 1.0),
        fill_strength=0.75,
    )

    sun_data.energy = 5.2
    sun_data.color = (1.0, 0.75, 0.48)
    sun_data.angle = math.radians(1.1)
    sun_obj.rotation_euler = (math.radians(79.0), math.radians(6.0), math.radians(-48.0))

    fill_data.energy = 1.10
    fill_data.color = (0.55, 0.70, 0.96)
    fill_data.angle = math.radians(28.0)
    fill_obj.rotation_euler = (math.radians(46.0), 0.0, math.radians(132.0))

    rim_data.energy = 1.25
    rim_data.color = (0.98, 0.84, 0.66)
    rim_obj.rotation_euler = (math.radians(74.0), 0.0, math.radians(158.0))

    dawn_path = RENDERS_DIR / f"{slug}_dawn.png"
    scene.render.filepath = str(dawn_path)
    bpy.ops.render.render(write_still=True)

    # 2. NIGHT RENDER (deep space sky, cool earthshine key/fill, warm amber local lamp pools)
    setup_lunar_world_shader(
        world,
        sky_cam_rgba=(0.0018, 0.0030, 0.0090, 1.0),
        sky_fill_rgba=(0.05, 0.08, 0.18, 1.0),
        fill_strength=0.55,
    )
    gbsdf.inputs["Base Color"].default_value = (0.12, 0.14, 0.18, 1.0)

    sun_data.energy = 1.35
    sun_data.color = (0.50, 0.66, 0.96)
    sun_data.angle = math.radians(3.5)
    sun_obj.rotation_euler = (math.radians(60.0), 0.0, math.radians(-38.0))

    fill_data.energy = 0.65
    fill_data.color = (0.38, 0.52, 0.84)
    fill_obj.rotation_euler = (math.radians(42.0), 0.0, math.radians(125.0))

    rim_data.energy = 1.05
    rim_data.color = (0.56, 0.72, 1.0)
    rim_obj.rotation_euler = (math.radians(68.0), 0.0, math.radians(155.0))

    for pobj in point_lights:
        pobj.data.energy *= 1.65

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
    for ucx in ucx_objs:
        ucx.hide_render = False
    r1kit.ensure_materials(use_vertex_color_shading=False)

    return dawn_path, night_path


def render_ruin_collision_wireframe(
    root: bpy.types.Object,
    slug: str,
    cam_angle_deg: float = 32.0,
    cam_elev_deg: float = 18.0,
    res: Tuple[int, int] = (1024, 768),
) -> Path:
    """Render `<slug>_collision_wireframe.png` showing the UCX_* convex collision proxies in wireframe over the ruin."""
    RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    attach_signage_texture_for_render(use_vertex_color_shading=True)

    ucx_objs = [
        o for o in r1kit.collect_hierarchy(root) if o.type == "MESH" and o.name.startswith("UCX_")
    ]
    for ucx in ucx_objs:
        ucx.hide_render = True

    wire_palette = [
        (0.10, 1.00, 0.35, 1.0),  # Neon lime green
        (0.00, 0.90, 1.00, 1.0),  # Bright cyan
        (1.00, 0.72, 0.05, 1.0),  # Warm amber gold
        (1.00, 0.30, 0.80, 1.0),  # Bright magenta
        (0.45, 1.00, 0.15, 1.0),  # Chartreuse
        (0.25, 0.65, 1.00, 1.0),  # Electric sky blue
        (1.00, 0.42, 0.20, 1.0),  # Coral orange
        (0.75, 0.40, 1.00, 1.0),  # Violet
    ]

    scene = bpy.context.scene
    _configure_fast_cycles(scene, res[0], res[1])
    scene.cycles.samples = 24
    scene.render.use_freestyle = False

    temp_wire_objs: List[bpy.types.Object] = []
    temp_wire_meshes: List[bpy.types.Mesh] = []
    temp_wire_mats: List[bpy.types.Material] = []

    for idx, ucx in enumerate(ucx_objs):
        col = wire_palette[idx % len(wire_palette)]
        wmat = bpy.data.materials.new(f"_UCXWireMat_{idx}")
        wmat.use_nodes = True
        wbsdf = next(n for n in wmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        wbsdf.inputs["Base Color"].default_value = col
        wbsdf.inputs["Roughness"].default_value = 0.25
        if "Emission Color" in wbsdf.inputs:
            wbsdf.inputs["Emission Color"].default_value = col
        if "Emission Strength" in wbsdf.inputs:
            wbsdf.inputs["Emission Strength"].default_value = 3.2
        temp_wire_mats.append(wmat)

        wmesh = ucx.data.copy()
        if len(wmesh.vertices) > 0:
            cent = sum((v.co.copy() for v in wmesh.vertices), Vector((0.0, 0.0, 0.0))) / len(wmesh.vertices)
            for v in wmesh.vertices:
                delta = v.co - cent
                if delta.length > 1e-4:
                    v.co = v.co + delta.normalized() * 0.075
            wmesh.update()
        wmesh.materials.clear()
        wmesh.materials.append(wmat)
        temp_wire_meshes.append(wmesh)

        wobj = bpy.data.objects.new(f"_Wire_{ucx.name}", wmesh)
        wobj.matrix_world = ucx.matrix_world.copy()
        scene.collection.objects.link(wobj)
        mod = wobj.modifiers.new("UCXWire", type="WIREFRAME")
        mod.thickness = 0.030
        mod.use_replace = True
        mod.use_even_offset = True
        mod.use_boundary = True
        mod.offset = 0.0
        temp_wire_objs.append(wobj)

    cam_data = bpy.data.cameras.new("RuinWireCam")
    cam_data.lens = 40.0
    cam_obj = bpy.data.objects.new("RuinWireCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    center, max_dim, min_z = _frame_camera_on_root(cam_obj, root, cam_angle_deg, cam_elev_deg, distance_scale=1.04)

    bpy.ops.mesh.primitive_grid_add(
        x_subdivisions=24,
        y_subdivisions=24,
        size=max_dim * 6.0,
        location=(center.x, center.y, min(0.0, min_z)),
    )
    ground = bpy.context.active_object
    ground.name = "_RuinWireGround"
    gmat = bpy.data.materials.new("_RuinWireGroundMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.14, 0.15, 0.18, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.94
    ground.data.materials.append(gmat)

    world = bpy.data.worlds.new("_RuinWireWorld")
    scene.world = world
    setup_lunar_world_shader(
        world,
        sky_cam_rgba=(0.006, 0.009, 0.022, 1.0),
        sky_fill_rgba=(0.14, 0.18, 0.30, 1.0),
        fill_strength=0.95,
    )

    sun_data = bpy.data.lights.new("WireSunKey", type="SUN")
    sun_data.energy = 4.6
    sun_data.color = (1.0, 0.86, 0.68)
    sun_data.angle = math.radians(2.0)
    sun_obj = bpy.data.objects.new("WireSunKey", sun_data)
    sun_obj.rotation_euler = (math.radians(65.0), math.radians(6.0), math.radians(-42.0))
    scene.collection.objects.link(sun_obj)

    fill_data = bpy.data.lights.new("WireSkyFill", type="SUN")
    fill_data.energy = 1.6
    fill_data.color = (0.62, 0.76, 0.98)
    fill_data.angle = math.radians(28.0)
    fill_obj = bpy.data.objects.new("WireSkyFill", fill_data)
    fill_obj.rotation_euler = (math.radians(46.0), 0.0, math.radians(132.0))
    scene.collection.objects.link(fill_obj)

    wire_path = RENDERS_DIR / f"{slug}_collision_wireframe.png"
    scene.render.filepath = str(wire_path)
    bpy.ops.render.render(write_still=True)

    for obj in [cam_obj, ground, sun_obj, fill_obj] + temp_wire_objs:
        bpy.data.objects.remove(obj, do_unlink=True)
    for wmesh in temp_wire_meshes:
        bpy.data.meshes.remove(wmesh)
    for wmat in temp_wire_mats:
        bpy.data.materials.remove(wmat)
    bpy.data.cameras.remove(cam_data)
    bpy.data.lights.remove(sun_data)
    bpy.data.lights.remove(fill_data)
    bpy.data.materials.remove(gmat)
    bpy.data.worlds.remove(world)
    for ucx in ucx_objs:
        ucx.hide_render = False
    r1kit.ensure_materials(use_vertex_color_shading=False)
    return wire_path


def finalize_ruin_and_export(
    root: bpy.types.Object,
    slug: str,
    fbx_name: str,
    footprint_radius: float = 3.5,
    cam_angle_deg: float = 32.0,
    cam_elev_deg: float = 18.0,
    res: Tuple[int, int] = (1024, 768),
    skip_renders: bool = False,
) -> Tuple[Path, Path, Path]:
    """Apply triplanar UVs, override R1_Signage/R1_Screen UVs, bake ruins vertex colors, export FBX, and render."""
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    apply_signage_uvs(root)
    bake_ruins_vertex_colors(root, footprint_radius=footprint_radius)

    fbx_path = RUINS_FBX_DIR / fbx_name
    r1kit.export_fbx(root, fbx_path)

    has_ucx = any(
        o.type == "MESH" and o.name.startswith("UCX_") for o in r1kit.collect_hierarchy(root)
    )
    if has_ucx and "--no-wireframe" not in sys.argv:
        render_ruin_collision_wireframe(
            root,
            slug=slug,
            cam_angle_deg=cam_angle_deg,
            cam_elev_deg=cam_elev_deg,
            res=res,
        )

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
