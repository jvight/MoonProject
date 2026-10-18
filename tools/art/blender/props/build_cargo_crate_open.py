"""Build the R1 open lander cargo crate (`Crate_Cargo_Open.fbx`).

Deliverable specifications:
  - Output FBX: Assets/_Project/Art/R1/Models/Crates/Crate_Cargo_Open.fbx
  - True crate shell size: 1.40 m long (X) x 0.90 m wide (Z) x 0.70 m tall (Y), sitting on a low skid/pallet.
  - Same R1 visual family as CargoLander (R1_CreamEnamel panels, R1_OrangeStripe stripe, stencils "LUMEN CARGO" +
    part number "LC-04 · PAD ASSY 01/02", R1_GreyMetal frame/skid, R1_BareSteel latches/hinges/handles, R1_GoldFoil
    thermal liner, R1_Brass pins, R1_Rubber shock mounts & interior foam cradles).
  - One side panel (`SidePanel_Open` on -Z) hinged down to the ground (open), and `Lid` lying beside it on the ground (+X).
  - Inside: raised shock-mounted deck with dark `R1_Rubber` foam cradles featuring TWO empty cut-outs shaped for the
    two ChargingPad fragments:
      * Cut-out 0 (left, X = -0.32 m): octagonal/round 0.52 m recess for `PadFragment_CoilPlate` (0.50 x 0.50 x 0.12 m)
      * Cut-out 1 (right, X = +0.34 m): elongated 0.31 x 0.82 m rail channel for `PadFragment_GuideRail` (0.28 x 0.90 x 0.18 m)
  - Two named empty transforms `PartSocket_0` (-0.32, 0.55, 0.00) and `PartSocket_1` (0.34, 0.55, 0.00) raised so a
    part placed there shows clearly above the rim from a camera 4 m behind and 2 m up (from both -Z and +Z).
  - Collider-friendly split shell meshes (`Crate_Cargo_Open` base/skid/floor/cradle with max Y = 0.535 m below the
    sockets, `Wall_Back`, `Wall_Left`, `Wall_Right`, `SidePanel_Open`, `Lid`) so neither BoxColliders nor per-mesh
    convex MeshColliders ever bridge over `PartSocket_0` or `PartSocket_1`.

Run standalone via headless Blender:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_cargo_crate_open.py
"""

from __future__ import annotations

import math
from pathlib import Path
import sys

import bpy
from mathutils import Euler, Vector

HERE = Path(__file__).resolve().parent
BLENDER_DIR = HERE.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402


CRATES_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Crates"
RENDERS_DIR = REPO_ROOT / "Logs" / "renders" / "cargo_crate_open"
LABEL_TEX_PATH = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Textures" / "T_R1_CargoCrateLabel_Albedo.png"


def ensure_extended_r1_materials() -> None:
    """Register R1_Brass and R1_Signage into r1kit at runtime so model_lint and r1kit recognise them."""
    extra_specs = {
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


def _rotate_mesh_around_pivot(
    obj: bpy.types.Object,
    pivot_u: tuple[float, float, float],
    rot_deg_b: tuple[float, float, float],
) -> None:
    """Rotate all vertices of `obj` around `pivot_u` in world space while keeping object rotation at (0,0,0)."""
    piv_b = Vector((pivot_u[0], pivot_u[2], pivot_u[1]))
    rot_mat = Euler([math.radians(a) for a in rot_deg_b], "XYZ").to_matrix().to_4x4()
    world_mat = obj.matrix_world
    inv_world = world_mat.inverted()

    for v in obj.data.vertices:
        w = world_mat @ v.co
        w_rot = piv_b + (rot_mat @ (w - piv_b))
        v.co = inv_world @ w_rot
    obj.data.update()


def _project_planar_uv(
    obj: bpy.types.Object,
    x_min: float,
    x_max: float,
    y_min: float,
    y_max: float,
    basis_u_b: Vector = Vector((1.0, 0.0, 0.0)),
    basis_v_b: Vector = Vector((0.0, 0.0, 1.0)),
) -> None:
    """Project world-space Blender coordinates along (basis_u_b, basis_v_b) into [0..1] x [0..1] UVs."""
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    world_mat = obj.matrix_world
    span_x = max(1e-6, x_max - x_min)
    span_y = max(1e-6, y_max - y_min)
    bu = basis_u_b.normalized()
    bv = basis_v_b.normalized()
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            v_w = world_mat @ mesh.vertices[mesh.loops[li].vertex_index].co
            px = v_w.dot(bu)
            py = v_w.dot(bv)
            u01 = max(0.0, min(1.0, (px - x_min) / span_x))
            v01 = max(0.0, min(1.0, (py - y_min) / span_y))
            uv_layer[li].uv = (u01, v01)


def _bind_signage_texture_for_preview() -> None:
    """Bind T_R1_CargoCrateLabel_Albedo.png to R1_Signage material nodes in Blender for preview renders."""
    if not LABEL_TEX_PATH.exists():
        return
    mat = bpy.data.materials.get("R1_Signage")
    if mat is None or mat.node_tree is None:
        return
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        return
    tex_node = nt.nodes.new("ShaderNodeTexImage")
    tex_node.image = bpy.data.images.load(str(LABEL_TEX_PATH), check_existing=True)
    tex_node.interpolation = "Linear"
    tex_node.extension = "CLIP"
    nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])


class _CrateMeshBuilder(r1kit.MeshBuilder):
    """MeshBuilder subclass that defaults bevel segments to 1 for clean low-poly R1 topology."""

    def add_bevel_box(
        self,
        center_u: tuple[float, float, float],
        size_u: tuple[float, float, float],
        mat: str,
        bevel: float = 0.004,
        segments: int = 1,
        rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ) -> None:
        super().add_bevel_box(
            center_u=center_u,
            size_u=size_u,
            mat=mat,
            bevel=bevel,
            segments=segments,
            rot_deg_u=rot_deg_u,
        )

    def add_cylinder(
        self,
        center_u: tuple[float, float, float],
        radius: float,
        length: float,
        mat: str,
        axis_u: str = "Y",
        segments: int = 12,
        bevel: float = 0.002,
        bevel_segments: int = 1,
        **kwargs: object,
    ) -> None:
        super().add_cylinder(
            center_u=center_u,
            radius=radius,
            length=length,
            mat=mat,
            axis_u=axis_u,
            segments=segments,
            bevel=bevel,
            bevel_segments=bevel_segments,
            **kwargs,
        )


def _add_skid_and_floor(mb: r1kit.MeshBuilder) -> None:
    """Build the low aerospace landing skid/pallet (Y = 0.00..0.11 m) and structural floor deck (Y = 0.11..0.45 m).

    Exact footprint: X in [-0.70, +0.70] (1.40 m long), Z in [-0.45, +0.45] (0.90 m wide).
    """
    # 1. Four corner rubber shock-absorber landing pads on the ground (Y = 0.00..0.03 m)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.62, 0.015, sz * 0.39),
                size_u=(0.16, 0.030, 0.12),
                mat="R1_Rubber",
                bevel=0.004,
            )
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, 0.015, sz * 0.39),
            size_u=(0.18, 0.030, 0.12),
            mat="R1_Rubber",
            bevel=0.004,
        )

    # 2. Three longitudinal heavy-duty pallet skid runners along X (Y = 0.02..0.085 m, X in [-0.70, +0.70])
    for z_run in (-0.39, 0.0, 0.39):
        mb.add_bevel_box(
            center_u=(0.0, 0.052, z_run),
            size_u=(1.40, 0.064, 0.095),
            mat="R1_GreyMetal",
            bevel=0.006,
        )
        # End bumper caps & tie-down clevis lugs at X = +-0.68 m
        for sx in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.675, 0.052, z_run),
                size_u=(0.045, 0.052, 0.075),
                mat="R1_BareSteel",
                bevel=0.004,
            )
            mb.add_cylinder(
                center_u=(sx * 0.685, 0.052, z_run),
                radius=0.010,
                length=0.085,
                mat="R1_Brass",
                axis_u="Z",
                segments=8,
                bevel=0.002,
            )

    # 3. Four transverse steel forklift / crane cross-bearers along Z (Y = 0.065..0.115 m, Z in [-0.45, +0.45])
    for x_beam in (-0.61, -0.21, 0.21, 0.61):
        mb.add_bevel_box(
            center_u=(x_beam, 0.090, 0.0),
            size_u=(0.085, 0.050, 0.90),
            mat="R1_BareSteel",
            bevel=0.005,
        )

    # 4. Lower structural floor slab & perimeter sill frame (Y = 0.11..0.16 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.135, 0.0),
        size_u=(1.36, 0.050, 0.86),
        mat="R1_GreyMetal",
        bevel=0.006,
    )

    # 5. Internal raised shock-mounted cargo deck (Y = 0.16..0.45 m)
    #    Matches CargoLander's lower bay construction: dark charcoal structural deck + quilted R1_GoldFoil insulation
    #    blanket panels + machined steel vertical damper columns + coral-orange trim band on the exposed front face.
    mb.add_bevel_box(
        center_u=(0.0, 0.305, 0.02),
        size_u=(1.26, 0.290, 0.78),
        mat="R1_GreyMetal",
        bevel=0.008,
    )
    # Gold Kapton thermal blanket bays inset into the front (-Z) exposed deck face (visible because -Z panel is open!)
    for x_foil in (-0.34, 0.34):
        mb.add_bevel_box(
            center_u=(x_foil, 0.295, -0.372),
            size_u=(0.46, 0.190, 0.024),
            mat="R1_GoldFoil",
            bevel=0.004,
        )
        # Quilted seam ribs across the gold foil
        for y_seam in (0.245, 0.295, 0.345):
            mb.add_bevel_box(
                center_u=(x_foil, y_seam, -0.382),
                size_u=(0.45, 0.012, 0.012),
                mat="R1_Brass",
                bevel=0.002,
            )

    # Vertical steel shock-mount struts along the open -Z deck front
    for x_strut in (-0.60, -0.08, 0.08, 0.60):
        mb.add_bevel_box(
            center_u=(x_strut, 0.305, -0.376),
            size_u=(0.052, 0.280, 0.036),
            mat="R1_BareSteel",
            bevel=0.004,
        )
    # Lower front orange identification stripe along the deck sill
    mb.add_bevel_box(
        center_u=(0.0, 0.175, -0.382),
        size_u=(1.24, 0.032, 0.024),
        mat="R1_OrangeStripe",
        bevel=0.003,
    )
    # Angled central front backing boss for the front LabelPlate plaque
    mb.add_bevel_box(
        center_u=(0.0, 0.305, -0.388),
        size_u=(0.44, 0.125, 0.030),
        mat="R1_GreyMetal",
        bevel=0.004,
    )


def _add_foam_cradles(mb: r1kit.MeshBuilder) -> None:
    """Build the interior foam cradles with TWO empty cut-outs shaped for the two ChargingPad fragments.

    All geometry here stays at Y <= 0.535 m, strictly below PartSocket_0 (Y = 0.55 m) and PartSocket_1 (Y = 0.55 m),
    so even a convex hull collider on the base mesh leaves the sockets and everything above them 100% unobstructed.

    - Cut-out 0 (left, centered at X = -0.32 m, Z = 0.00 m):
      Octagonal/circular recess (0.52 m inner span) for `PadFragment_CoilPlate` (0.50 x 0.50 x 0.12 m).
    - Cut-out 1 (right, centered at X = +0.34 m, Z = 0.00 m):
      Elongated rail recess (0.31 m wide x 0.80 m long) for `PadFragment_GuideRail` (0.28 x 0.90 x 0.18 m).
    """
    y_floor_top = 0.455
    y_bolster_top = 0.535
    bolster_h = y_bolster_top - y_floor_top
    bolster_cy = 0.5 * (y_floor_top + y_bolster_top)

    # Recessed cradle floor liners inside both cut-outs (Y = 0.44..0.46 m)
    # Left Cut-out 0 floor (octagonal/circular pad seat with copper/brass grounding contacts)
    mb.add_cylinder(
        center_u=(-0.32, 0.450, 0.0),
        radius=0.255,
        length=0.022,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=8,
        bevel=0.003,
    )
    mb.add_cylinder(
        center_u=(-0.32, 0.456, 0.0),
        radius=0.14,
        length=0.014,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=8,
        bevel=0.002,
    )
    for ang_deg in (0.0, 90.0, 180.0, 270.0):
        rad = math.radians(ang_deg)
        mb.add_bevel_box(
            center_u=(-0.32 + math.cos(rad) * 0.21, 0.460, math.sin(rad) * 0.21),
            size_u=(0.045, 0.012, 0.045),
            mat="R1_OrangeStripe",
            bevel=0.002,
        )

    # Right Cut-out 1 floor (elongated guide-rail seat with transverse shock strips)
    mb.add_bevel_box(
        center_u=(0.34, 0.450, 0.0),
        size_u=(0.30, 0.022, 0.78),
        mat="R1_CreamEnamel",
        bevel=0.003,
    )
    for z_seat in (-0.28, 0.0, 0.28):
        mb.add_bevel_box(
            center_u=(0.34, 0.459, z_seat),
            size_u=(0.24, 0.012, 0.055),
            mat="R1_OrangeStripe",
            bevel=0.002,
        )

    # --- Surround Foam Bolsters (`R1_Rubber`, Y = 0.455..0.535 m) ---
    # 1. Outer Left bolster (X in [-0.62, -0.58])
    mb.add_bevel_box(
        center_u=(-0.595, bolster_cy, 0.0),
        size_u=(0.055, bolster_h, 0.78),
        mat="R1_Rubber",
        bevel=0.006,
    )
    # 2. Left Cut-out 0 front & back foam bolsters (leaving 0.52 m octagonal opening around (-0.32, 0.0))
    mb.add_bevel_box(
        center_u=(-0.32, bolster_cy, 0.325),
        size_u=(0.52, bolster_h, 0.13),
        mat="R1_Rubber",
        bevel=0.006,
    )
    mb.add_bevel_box(
        center_u=(-0.32, bolster_cy, -0.325),
        size_u=(0.52, bolster_h, 0.13),
        mat="R1_Rubber",
        bevel=0.006,
    )
    # 4 diagonal corner wedges forming the octagonal/round Cut-out 0 cavity around (-0.32, 0.0)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(-0.32 + sx * 0.215, bolster_cy, sz * 0.215),
                size_u=(0.13, bolster_h, 0.085),
                mat="R1_Rubber",
                bevel=0.005,
                segments=1,
                rot_deg_u=(0.0, -sx * sz * 45.0, 0.0),
            )
            # Brass spring-clip retainers on the 4 octagonal bolster corners
            mb.add_bevel_box(
                center_u=(-0.32 + sx * 0.195, y_bolster_top - 0.004, sz * 0.195),
                size_u=(0.028, 0.012, 0.028),
                mat="R1_Brass",
                bevel=0.002,
            )

    # 3. Center divider foam bolster between Cut-out 0 (left) and Cut-out 1 (right) (X in [-0.06, +0.18])
    mb.add_bevel_box(
        center_u=(0.06, bolster_cy, 0.0),
        size_u=(0.24, bolster_h, 0.78),
        mat="R1_Rubber",
        bevel=0.006,
    )
    # Recessed steel tie-down rail down the spine of the center foam divider
    mb.add_bevel_box(
        center_u=(0.06, y_bolster_top - 0.004, 0.0),
        size_u=(0.055, 0.014, 0.70),
        mat="R1_BareSteel",
        bevel=0.003,
    )

    # 4. Right Cut-out 1 outer right foam bolster (X in [+0.50, +0.62], leaving X in [+0.18, +0.50] = 0.32 m wide slot!)
    mb.add_bevel_box(
        center_u=(0.56, bolster_cy, 0.0),
        size_u=(0.12, bolster_h, 0.78),
        mat="R1_Rubber",
        bevel=0.006,
    )
    # Right Cut-out 1 rear end-stop bolster (Z in [+0.36, +0.41], leaving 0.76 m open toward -Z front!)
    mb.add_bevel_box(
        center_u=(0.34, bolster_cy, 0.385),
        size_u=(0.32, bolster_h, 0.05),
        mat="R1_Rubber",
        bevel=0.005,
    )
    # Lateral guide-rail alignment nibs & brass clips along Cut-out 1 edges
    for z_clip in (-0.24, 0.0, 0.24):
        for x_edge in (0.175, 0.505):
            mb.add_bevel_box(
                center_u=(x_edge, y_bolster_top - 0.004, z_clip),
                size_u=(0.026, 0.012, 0.042),
                mat="R1_Brass",
                bevel=0.002,
            )


def _build_wall_back(root: bpy.types.Object) -> bpy.types.Object:
    """Build the rear (+Z) wall (`Wall_Back`): corner stanchions to Y=0.70 m, central rim at Y=0.58 m."""
    mb = _CrateMeshBuilder("Wall_Back", pivot_u=(0.0, 0.11, 0.42))

    # Main central wall core (X in [-0.62, +0.62], Y in [0.11, 0.58], Z in [+0.39, +0.45])
    mb.add_bevel_box(
        center_u=(0.0, 0.345, 0.42),
        size_u=(1.24, 0.47, 0.060),
        mat="R1_CreamEnamel",
        bevel=0.006,
    )
    # Top dark charcoal structural rim rail along the central wall (Y = 0.555..0.585 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.570, 0.42),
        size_u=(1.24, 0.030, 0.066),
        mat="R1_GreyMetal",
        bevel=0.004,
    )
    # Signature CargoLander coral-orange horizontal band across the rear wall (Y = 0.37..0.45 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.410, 0.423),
        size_u=(1.25, 0.076, 0.064),
        mat="R1_OrangeStripe",
        bevel=0.004,
    )
    # Vertical stiffening ribs on the rear wall
    for rx in (-0.46, -0.24, 0.24, 0.46):
        mb.add_bevel_box(
            center_u=(rx, 0.335, 0.425),
            size_u=(0.036, 0.43, 0.068),
            mat="R1_CreamEnamel",
            bevel=0.004,
        )

    # Two heavy rear corner stanchions / hoist posts rising to full crate height Y = 0.70 m
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.65, 0.405, 0.415),
            size_u=(0.10, 0.59, 0.070),
            mat="R1_GreyMetal",
            bevel=0.006,
        )
        # Top corner bumper cap & crane hoist lug at Y = 0.66..0.70 m
        mb.add_bevel_box(
            center_u=(sx * 0.65, 0.680, 0.415),
            size_u=(0.10, 0.040, 0.070),
            mat="R1_BareSteel",
            bevel=0.004,
        )
        mb.add_cylinder(
            center_u=(sx * 0.65, 0.680, 0.415),
            radius=0.012,
            length=0.074,
            mat="R1_Brass",
            axis_u="Z",
            segments=8,
            bevel=0.002,
        )

    return mb.build_object(parent=root)


def _build_side_wall(root: bpy.types.Object, name: str, side_sign: float) -> bpy.types.Object:
    """Build `Wall_Left` (side_sign=-1) or `Wall_Right` (side_sign=+1) at X = +-0.66 m."""
    x_wall = side_sign * 0.66
    mb = _CrateMeshBuilder(name, pivot_u=(x_wall, 0.11, 0.0))

    # Central side wall panel (Y in [0.11, 0.58], Z in [-0.38, +0.38])
    mb.add_bevel_box(
        center_u=(x_wall, 0.345, 0.0),
        size_u=(0.072, 0.47, 0.76),
        mat="R1_CreamEnamel",
        bevel=0.006,
    )
    # Top charcoal rim rail (Y = 0.555..0.585 m)
    mb.add_bevel_box(
        center_u=(x_wall, 0.570, 0.0),
        size_u=(0.078, 0.030, 0.76),
        mat="R1_GreyMetal",
        bevel=0.004,
    )
    # Horizontal coral-orange stripe wrapping around the end wall (Y = 0.37..0.45 m)
    mb.add_bevel_box(
        center_u=(x_wall + side_sign * 0.003, 0.410, 0.0),
        size_u=(0.076, 0.076, 0.77),
        mat="R1_OrangeStripe",
        bevel=0.004,
    )
    # Front and rear corner stanchions on the side wall rising to Y = 0.70 m
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(x_wall, 0.405, sz * 0.405),
            size_u=(0.080, 0.59, 0.090),
            mat="R1_GreyMetal",
            bevel=0.006,
        )
        mb.add_bevel_box(
            center_u=(x_wall, 0.680, sz * 0.405),
            size_u=(0.080, 0.040, 0.090),
            mat="R1_BareSteel",
            bevel=0.004,
        )

    # Recessed exterior bail handle & steel latch plates on the outer face
    x_outer = x_wall + side_sign * 0.038
    mb.add_bevel_box(
        center_u=(x_outer, 0.285, 0.0),
        size_u=(0.018, 0.095, 0.24),
        mat="R1_GreyMetal",
        bevel=0.003,
    )
    mb.add_bevel_box(
        center_u=(x_outer + side_sign * 0.008, 0.285, 0.0),
        size_u=(0.016, 0.024, 0.18),
        mat="R1_BareSteel",
        bevel=0.003,
    )
    for sz in (-0.24, 0.24):
        mb.add_bevel_box(
            center_u=(x_outer, 0.515, sz),
            size_u=(0.018, 0.075, 0.048),
            mat="R1_BareSteel",
            bevel=0.003,
        )
        mb.add_bevel_box(
            center_u=(x_outer + side_sign * 0.004, 0.535, sz),
            size_u=(0.014, 0.024, 0.032),
            mat="R1_Brass",
            bevel=0.002,
        )

    return mb.build_object(parent=root)


def _build_side_panel_open(root: bpy.types.Object) -> bpy.types.Object:
    """Build `SidePanel_Open`: the -Z long side panel hinged down to the ground (open).

    Constructed flat along the ground in front of the -Z sill (Z in [-0.88, -0.43], Y in [0.00, 0.125]) so the
    mesh vertices already sit in the open, ground-resting pose with object rotation = (0, 0, 0).
    """
    hinge_pivot_u = (0.0, 0.11, -0.44)
    mb = _CrateMeshBuilder("SidePanel_Open", pivot_u=hinge_pivot_u)

    # 1. Heavy steel piano-hinge knuckles along the front sill (Z = -0.44 m, Y = 0.105 m)
    for hx in (-0.50, -0.25, 0.0, 0.25, 0.50):
        mb.add_cylinder(
            center_u=(hx, 0.105, -0.44),
            radius=0.020,
            length=0.075,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.003,
        )
        mb.add_cylinder(
            center_u=(hx, 0.105, -0.44),
            radius=0.009,
            length=0.085,
            mat="R1_Brass",
            axis_u="X",
            segments=8,
            bevel=0.0015,
        )

    # 2. Build the panel leaf horizontally, then pitch it down by 11.5 deg around hinge_pivot_u so its leading
    #    edge rests on the ground at Y = 0.00 m, Z = -0.88 m!
    #    In un-tilted horizontal pose from Z = -0.44 to -0.88 (length 0.44 m), centered at Z = -0.66, Y = 0.085:
    mb.add_bevel_box(
        center_u=(0.0, 0.082, -0.66),
        size_u=(1.22, 0.048, 0.44),
        mat="R1_CreamEnamel",
        bevel=0.005,
    )
    # Side frame rails of the fold-down panel
    for sx in (-0.59, 0.59):
        mb.add_bevel_box(
            center_u=(sx, 0.084, -0.66),
            size_u=(0.045, 0.054, 0.44),
            mat="R1_GreyMetal",
            bevel=0.004,
        )
    # Leading edge bumper beam (which was the top rim when closed, now resting on the ground at Z = -0.865 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.084, -0.865),
        size_u=(1.22, 0.056, 0.042),
        mat="R1_GreyMetal",
        bevel=0.004,
    )
    # Coral-orange hazard/identification stripe across the upward-facing inner face and leading lip
    mb.add_bevel_box(
        center_u=(0.0, 0.106, -0.815),
        size_u=(1.12, 0.014, 0.058),
        mat="R1_OrangeStripe",
        bevel=0.002,
    )
    # Anti-slip tread ribs & cam-lock latch tongues on the upward-facing inner face
    for rx in (-0.42, -0.21, 0.0, 0.21, 0.42):
        mb.add_bevel_box(
            center_u=(rx, 0.106, -0.62),
            size_u=(0.032, 0.014, 0.28),
            mat="R1_GreyMetal",
            bevel=0.002,
        )
    for lx in (-0.46, 0.46):
        mb.add_bevel_box(
            center_u=(lx, 0.108, -0.865),
            size_u=(0.052, 0.022, 0.052),
            mat="R1_BareSteel",
            bevel=0.003,
        )
        mb.add_bevel_box(
            center_u=(lx, 0.114, -0.865),
            size_u=(0.028, 0.016, 0.028),
            mat="R1_Brass",
            bevel=0.002,
        )

    panel_obj = mb.build_object(parent=root)
    # Tilt the panel leaf down by -11.5 deg around X so the leading edge kisses Y = 0.00 m on the ground
    _rotate_mesh_around_pivot(panel_obj, hinge_pivot_u, (-11.5, 0.0, 0.0))
    return panel_obj


def _build_lid_on_ground(root: bpy.types.Object) -> tuple[bpy.types.Object, bpy.types.Object]:
    """Build `Lid` and `Lid/Seal` lying beside the crate on the ground (+X side, X in [+0.78, +1.58], Z in [-0.45, +0.45])."""
    lid_center_x = 1.18
    lid_pivot_u = (lid_center_x, 0.0, 0.0)
    lid_mb = _CrateMeshBuilder("Lid", pivot_u=lid_pivot_u)

    # Rubber corner rest pads on the ground (Y = 0.00..0.015 m)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            lid_mb.add_bevel_box(
                center_u=(lid_center_x + sx * 0.35, 0.008, sz * 0.39),
                size_u=(0.10, 0.016, 0.12),
                mat="R1_Rubber",
                bevel=0.003,
            )

    # Main CreamEnamel lid shell (0.80 m wide in X x 0.90 m long in Z x 0.056 m thick, Y = 0.012..0.068 m)
    lid_mb.add_bevel_box(
        center_u=(lid_center_x, 0.040, 0.0),
        size_u=(0.78, 0.054, 0.88),
        mat="R1_CreamEnamel",
        bevel=0.006,
    )
    # Perimeter GreyMetal bumper frame around the lid
    for sx in (-1.0, 1.0):
        lid_mb.add_bevel_box(
            center_u=(lid_center_x + sx * 0.375, 0.042, 0.0),
            size_u=(0.050, 0.058, 0.90),
            mat="R1_GreyMetal",
            bevel=0.005,
        )
    for sz in (-1.0, 1.0):
        lid_mb.add_bevel_box(
            center_u=(lid_center_x, 0.042, sz * 0.425),
            size_u=(0.80, 0.058, 0.050),
            mat="R1_GreyMetal",
            bevel=0.005,
        )

    # Coral-orange identification stripe across the top of the lid
    lid_mb.add_bevel_box(
        center_u=(lid_center_x, 0.068, 0.24),
        size_u=(0.70, 0.012, 0.11),
        mat="R1_OrangeStripe",
        bevel=0.002,
    )
    lid_mb.add_bevel_box(
        center_u=(lid_center_x, 0.068, -0.24),
        size_u=(0.70, 0.012, 0.06),
        mat="R1_OrangeStripe",
        bevel=0.002,
    )

    # Structural roof ribs & machined steel cam-lock latches on the lid
    for sz in (-0.34, 0.34):
        lid_mb.add_bevel_box(
            center_u=(lid_center_x, 0.068, sz),
            size_u=(0.68, 0.012, 0.032),
            mat="R1_CreamEnamel",
            bevel=0.002,
        )
    for sx in (-0.38, 0.38):
        for sz in (-0.26, 0.26):
            lid_mb.add_bevel_box(
                center_u=(lid_center_x + sx, 0.048, sz),
                size_u=(0.032, 0.042, 0.064),
                mat="R1_BareSteel",
                bevel=0.003,
            )
            lid_mb.add_bevel_box(
                center_u=(lid_center_x + sx, 0.066, sz),
                size_u=(0.022, 0.014, 0.036),
                mat="R1_Brass",
                bevel=0.002,
            )

    lid_obj = lid_mb.build_object(parent=root)

    # Child `Seal` (Lid/Seal) for container contract compatibility
    seal_mb = _CrateMeshBuilder("Seal", pivot_u=lid_pivot_u)
    seal_mb.add_bevel_box(
        center_u=(lid_center_x - 0.36, 0.066, 0.0),
        size_u=(0.022, 0.014, 0.42),
        mat="R1_LampAmber",
        bevel=0.002,
    )
    seal_obj = seal_mb.build_object(parent=lid_obj)

    # Child `CutPoint` (Lid/CutPoint) placed at the crate center (0, 0.45, 0) so distance checks measure to crate center
    r1kit.create_empty("CutPoint", parent=lid_obj, pivot_u=(0.0, 0.45, 0.0))

    return lid_obj, seal_obj


def _build_rim_glow(root: bpy.types.Object) -> bpy.types.Object:
    """Build `RimGlow` along the inner upper bezel of `Wall_Back` so its convex hull never crosses the sockets."""
    rim_mb = _CrateMeshBuilder("RimGlow", pivot_u=(0.0, 0.56, 0.39))
    for rx in (-0.36, 0.0, 0.36):
        rim_mb.add_bevel_box(
            center_u=(rx, 0.560, 0.390),
            size_u=(0.28, 0.018, 0.018),
            mat="R1_LampAmber",
            bevel=0.002,
        )
    return rim_mb.build_object(parent=root)


def _build_label_plates(root: bpy.types.Object) -> list[bpy.types.Object]:
    """Build stencilled plaques reading 'LUMEN CARGO · PART NO. LC-04 · PAD ASSY 01/02' on front, back, and lid.

    Each plaque is its own separate flat mesh (`LabelPlate`, `LabelPlate_Back`, `LabelPlate_Lid`) so per-mesh convex
    colliders stay 6 mm flat plates on their respective surfaces and never bridge across the crate interior.
    """
    plates: list[bpy.types.Object] = []

    # 1. Front plaque on the exposed lower front deck (-Z side, tilted toward the -Z camera)
    front_mb = _CrateMeshBuilder("LabelPlate", pivot_u=(0.0, 0.305, -0.404))
    front_mb.add_bevel_box(
        center_u=(0.0, 0.305, -0.404),
        size_u=(0.42, 0.105, 0.008),
        mat="R1_Signage",
        bevel=0.0015,
    )
    front_obj = front_mb.build_object(parent=root)
    _project_planar_uv(
        front_obj,
        x_min=-0.21,
        x_max=0.21,
        y_min=0.2525,
        y_max=0.3575,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 0.0, 1.0)),
    )
    plates.append(front_obj)

    # 2. Rear exterior plaque on `Wall_Back` (+Z outer face, readable from +Z behind the crate)
    back_mb = _CrateMeshBuilder("LabelPlate_Back", pivot_u=(0.0, 0.255, 0.453))
    back_mb.add_bevel_box(
        center_u=(0.0, 0.255, 0.453),
        size_u=(0.56, 0.140, 0.008),
        mat="R1_Signage",
        bevel=0.0015,
    )
    back_obj = back_mb.build_object(parent=root)
    _project_planar_uv(
        back_obj,
        x_min=0.28,
        x_max=-0.28,
        y_min=0.185,
        y_max=0.325,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 0.0, 1.0)),
    )
    plates.append(back_obj)

    # 3. Upward-facing plaque on top of `Lid` (readable from overhead / 4 m behind & 2 m up)
    lid_mb = _CrateMeshBuilder("LabelPlate_Lid", pivot_u=(1.18, 0.070, 0.0))
    lid_mb.add_bevel_box(
        center_u=(1.18, 0.070, 0.0),
        size_u=(0.56, 0.008, 0.140),
        mat="R1_Signage",
        bevel=0.0015,
    )
    lid_obj = lid_mb.build_object(parent=root)
    _project_planar_uv(
        lid_obj,
        x_min=0.90,
        x_max=1.46,
        y_min=-0.070,
        y_max=0.070,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 1.0, 0.0)),
    )
    plates.append(lid_obj)

    return plates


def _project_label_plate_uvs(plates: list[bpy.types.Object]) -> None:
    """Project planar [0..1] UVs onto LabelPlate, LabelPlate_Back, and LabelPlate_Lid after apply_triplanar_uvs."""
    front_obj, back_obj, lid_obj = plates
    _project_planar_uv(
        front_obj,
        x_min=-0.21,
        x_max=0.21,
        y_min=0.2525,
        y_max=0.3575,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 0.0, 1.0)),
    )
    _project_planar_uv(
        back_obj,
        x_min=0.28,
        x_max=-0.28,
        y_min=0.185,
        y_max=0.325,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 0.0, 1.0)),
    )
    _project_planar_uv(
        lid_obj,
        x_min=0.90,
        x_max=1.46,
        y_min=-0.070,
        y_max=0.070,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, 1.0, 0.0)),
    )


def build_cargo_crate_open() -> bpy.types.Object:
    """Build the complete `Crate_Cargo_Open` hierarchy and bake R1 vertex colours."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    # 1. Root mesh: low skid/pallet + floor + raised deck + sculpted 2-cutout foam cradles (max Y = 0.535 m)
    base_mb = _CrateMeshBuilder("Crate_Cargo_Open", pivot_u=(0.0, 0.0, 0.0))
    _add_skid_and_floor(base_mb)
    _add_foam_cradles(base_mb)
    root_obj = base_mb.build_object()

    # 2. Separate collider-friendly shell walls & hinged-down front panel
    _build_wall_back(root_obj)
    _build_side_wall(root_obj, "Wall_Left", -1.0)
    _build_side_wall(root_obj, "Wall_Right", 1.0)
    _build_side_panel_open(root_obj)

    # 3. Detached Lid lying beside the crate on the ground (+X) with Seal & CutPoint
    _, seal_obj = _build_lid_on_ground(root_obj)

    # 4. RimGlow strip on inner back wall & stencilled LabelPlates ("LUMEN CARGO" + part number)
    rim_obj = _build_rim_glow(root_obj)
    plates = _build_label_plates(root_obj)

    # 5. Raised empty part sockets over the two foam cradle cut-outs (Y = 0.55 m) + ItemSocket
    r1kit.create_empty("PartSocket_0", parent=root_obj, pivot_u=(-0.32, 0.55, 0.0))
    r1kit.create_empty("PartSocket_1", parent=root_obj, pivot_u=(0.34, 0.55, 0.0))
    r1kit.create_empty("ItemSocket", parent=root_obj, pivot_u=(0.0, 0.48, 0.0))

    # 6. Apply triplanar UVs first, then exact [0, 1] planar UVs on the three LabelPlate plaques
    r1kit.apply_triplanar_uvs(root_obj, texel_scale=0.85)
    _project_label_plate_uvs(plates)

    # 7. Prune unused slots and bake R1 vertex colours (AO in R, edge wear in G, dust in B)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root_obj))
    r1kit.bake_vertex_colors(root_obj, footprint_radius=0.85)

    # Keep emissive RimGlow & Seal clean and bright, and keep LabelPlates crisp
    for glow_obj in (rim_obj, seal_obj):
        for attr_name in ("Col", "Color"):
            ca = glow_obj.data.color_attributes.get(attr_name)
            if ca is not None:
                for idx in range(len(ca.data)):
                    ca.data[idx].color = (1.0, 0.35, 0.0, 1.0)

    for plate_obj in plates:
        for attr_name in ("Col", "Color"):
            ca = plate_obj.data.color_attributes.get(attr_name)
            if ca is not None:
                for idx in range(len(ca.data)):
                    ca.data[idx].color = (0.96, 0.14, 0.06, 1.0)

    _bind_signage_texture_for_preview()
    return root_obj


def main() -> None:
    CRATES_DIR.mkdir(parents=True, exist_ok=True)
    root_obj = build_cargo_crate_open()
    fbx_path = CRATES_DIR / "Crate_Cargo_Open.fbx"
    r1kit.export_fbx(root_obj, fbx_path)
    print(f"Exported {fbx_path}")


if __name__ == "__main__":
    main()
