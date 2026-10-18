"""Build R1 Opening / Story prop: Ines's 1988 sealed ice-core sample tube ("CORE 41 · H2O").

Outputs:
  - Assets/_Project/Art/R1/Models/Props/IceCoreTube.fbx
      (~0.45 m long, ~0.082 m diameter, lying on its side with pivot at its centre (0,0,0))
      Child meshes:
        * CryoSleeve   (R1_BareSteel, R1_GreyMetal, R1_OrangeStripe, R1_Brass, R1_Copper, R1_Rubber)
        * CoreSample   (R1_CreamEnamel -> R1_IceCoreFrost in Unity, UV Zone B [0.54..0.96])
        * SightWindow  (R1_DarkGlass -> R1_IceCoreWindow in Unity, R1_Brass retainer bolts)
        * LabelPlate   (R1_Signage -> R1_IceCoreLabel in Unity, [0,1] UVs for 'CORE 41 · H₂O · I.V. 10/88')
        * CarryHandle  (R1_BareSteel, R1_OrangeStripe, R1_Rubber, R1_Brass)
        * FrostCap     (R1_CreamEnamel -> R1_IceCoreFrost in Unity, UV Zone A [0.04..0.46])
      Anchors:
        * Center_Anchor (0, 0, 0)
        * Window_Anchor (0, 0.024, -0.025)
        * Handle_Anchor (0, 0.036, 0.032)
        * Frost_Anchor  (-0.215, 0, 0)
  - Logs/renders/icecore_icon_source.png (512x512 transparent PNG hold icon source)
  - Logs/renders/icecore_crate_and_07.png (eclipse dark under 07's lamps next to 07 + inside Crate_M)

Run headless:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_ice_core_tube.py
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


def _project_planar_uv(
    obj: bpy.types.Object,
    x_min: float,
    x_max: float,
    y_min: float,
    y_max: float,
    u_min: float = 0.0,
    u_max: float = 1.0,
    v_min: float = 0.0,
    v_max: float = 1.0,
    basis_u_b: Vector = Vector((1.0, 0.0, 0.0)),
    basis_v_b: Vector = Vector((0.0, 0.0, 1.0)),
) -> None:
    """Project world-space Blender coordinates along (basis_u_b, basis_v_b) into [u_min..u_max] x [v_min..v_max]."""
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
            u = u_min + u01 * (u_max - u_min)
            v_coord = v_min + v01 * (v_max - v_min)
            uv_layer[li].uv = (u, v_coord)


def _boost_frost_vertex_colors(obj: bpy.types.Object, base_ao: float = 0.88, min_glint: float = 0.42) -> None:
    """Boost vertex AO (R) and crystalline facet edge-wear glint (G) so frost/ice catches 07's lamps."""
    mesh = obj.data
    for attr_name in ("Col", "Color"):
        ca = mesh.color_attributes.get(attr_name)
        if ca is None:
            continue
        for poly in mesh.polygons:
            n = poly.normal
            facet_glint = min(1.0, max(min_glint, 0.45 + 0.35 * abs(n.x) + 0.25 * max(0.0, n.z - 0.3 * n.y)))
            for li in poly.loop_indices:
                c = ca.data[li].color
                r = max(base_ao, float(c[0]))
                g = max(facet_glint, float(c[1]))
                b = min(0.22, float(c[2]))
                ca.data[li].color = (r, g, b, 1.0)


def _center_hierarchy_at_origin(root: bpy.types.Object) -> None:
    """Shift all child mesh vertices and empties so the combined bounding-box centre is exactly (0, 0, 0)."""
    bpy.context.view_layer.update()
    meshes = [o for o in r1kit.collect_hierarchy(root) if o.type == "MESH"]
    empties = [o for o in r1kit.collect_hierarchy(root) if o.type == "EMPTY" and o != root]

    min_v = Vector((1e9, 1e9, 1e9))
    max_v = Vector((-1e9, -1e9, -1e9))
    for obj in meshes:
        for v in obj.data.vertices:
            w = obj.matrix_world @ v.co
            min_v.x = min(min_v.x, w.x)
            min_v.y = min(min_v.y, w.y)
            min_v.z = min(min_v.z, w.z)
            max_v.x = max(max_v.x, w.x)
            max_v.y = max(max_v.y, w.y)
            max_v.z = max(max_v.z, w.z)

    center_b = 0.5 * (min_v + max_v)
    for obj in meshes:
        inv_rot = obj.matrix_world.to_3x3().inverted()
        local_shift = inv_rot @ center_b
        for v in obj.data.vertices:
            v.co -= local_shift
        obj.data.update()
    for emp in empties:
        if emp.name == "Center_Anchor":
            emp.location = (0.0, 0.0, 0.0)
        else:
            emp.location = Vector(emp.location) - center_b


def build_ice_core_tube() -> bpy.types.Object:
    """Build Ines's 1988 sealed ice-core sample tube ('CORE 41 · H2O') centered at (0, 0, 0)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("IceCoreTube", pivot_u=(0.0, 0.0, 0.0))

    # -------------------------------------------------------------------------
    # 1. CryoSleeve: ribbed aluminium cryo sleeve (~0.45 m long, ~0.082 m dia)
    #    Note: MeshBuilder coordinates are Unity (x_right, y_up, z_fwd).
    #    Front of prop is -Z_u (-Y_b); top of prop is +Y_u (+Z_b).
    # -------------------------------------------------------------------------
    sleeve = r1kit.MeshBuilder("CryoSleeve", pivot_u=(0.0, 0.0, 0.0))

    # Central recessed instrument/window barrel section (x in [-0.095, +0.095], radius 0.0315 m)
    sleeve.add_cylinder((0.0, 0.0, 0.0), 0.0315, 0.194, mat="R1_GreyMetal", axis_u="X", segments=12, bevel=0.0015)

    # Left & Right machined aluminium cryo chambers (radius 0.0355 m)
    for cx in (-0.153, 0.153):
        sleeve.add_cylinder((cx, 0.0, 0.0), 0.0355, 0.122, mat="R1_BareSteel", axis_u="X", segments=12, bevel=0.0018)

    # 6 circumferential aluminium cooling ribs (3 per side, outer radius 0.0398 m -> 79.6 mm dia)
    for rx in (-0.176, -0.150, -0.124, 0.124, 0.150, 0.176):
        sleeve.add_cylinder((rx, 0.0, 0.0), 0.0398, 0.010, mat="R1_BareSteel", axis_u="X", segments=12, bevel=0.0015)

    # 4 axial stiffening strakes per side bridging the cooling ribs
    for cx in (-0.150, 0.150):
        for ang_deg in (35.0, 125.0, 215.0, 305.0):
            ang = math.radians(ang_deg)
            y_u = math.sin(ang) * 0.0372
            z_u = math.cos(ang) * 0.0372
            sleeve.add_bevel_box(
                (cx, y_u, z_u),
                (0.076, 0.0045, 0.0045),
                mat="R1_GreyMetal",
                bevel=0.001,
                segments=1,
                rot_deg_u=(-ang_deg, 0.0, 0.0),
            )

    # Twin coral-orange thermal hazard / cryo seal bands flanking the window bay (x = +-0.098 m)
    for ox in (-0.098, 0.098):
        sleeve.add_cylinder((ox, 0.0, 0.0), 0.0388, 0.017, mat="R1_OrangeStripe", axis_u="X", segments=12, bevel=0.0016)
        sleeve.add_cylinder((ox * 0.84, 0.0, 0.0), 0.0375, 0.010, mat="R1_BareSteel", axis_u="X", segments=12, bevel=0.0014)

    # Octagonal rubber shock bumper rings near both ends (x = +-0.199 m, radius 0.0410 m -> 0.082 m dia)
    for bx in (-0.199, 0.199):
        sleeve.add_cylinder((bx, 0.0, 0.0), 0.0410, 0.015, mat="R1_Rubber", axis_u="X", segments=10, bevel=0.002)
        # Bottom anti-roll stabilizer feet pads so the tube sits steadily on its side
        sleeve.add_bevel_box((bx, -0.0365, 0.0), (0.022, 0.009, 0.044), mat="R1_Rubber", bevel=0.0018, segments=1)

    # Brass bayonet locking collars & end-cap plugs (spanning X = [-0.225, +0.225] -> 0.450 m total length)
    for ex_collar, ex_cap in ((-0.213, -0.221), (0.213, 0.221)):
        sleeve.add_cylinder((ex_collar, 0.0, 0.0), 0.0388, 0.013, mat="R1_Brass", axis_u="X", segments=12, bevel=0.0015)
        sleeve.add_cylinder((ex_cap, 0.0, 0.0), 0.0320, 0.008, mat="R1_GreyMetal", axis_u="X", segments=10, bevel=0.0015)

    # +X Valve & Cryo Pressure-Relief Manifold (brass relief valve + copper capillary purge line)
    sleeve.add_cylinder((0.221, 0.012, -0.010), 0.0095, 0.008, mat="R1_Brass", axis_u="X", segments=8, bevel=0.0012)
    sleeve.add_cylinder((0.0, -0.0355, 0.0), 0.0032, 0.350, mat="R1_Copper", axis_u="X", segments=6, bevel=0.0006)

    # Angled lower-front mounting boss backing the LabelPlate (x in [-0.080, +0.080])
    sleeve.add_bevel_box(
        (0.0, -0.0125, -0.0315),
        (0.162, 0.028, 0.012),
        mat="R1_GreyMetal",
        bevel=0.002,
        segments=1,
        rot_deg_u=(-26.0, 0.0, 0.0),
    )
    sleeve.build_object(parent=root)

    # -------------------------------------------------------------------------
    # 2. CoreSample: Frosted grey lunar ice-core visible inside the window
    # -------------------------------------------------------------------------
    core = r1kit.MeshBuilder("CoreSample", pivot_u=(0.0, 0.0, 0.0))
    core.add_cylinder((0.0, 0.012, -0.013), 0.0215, 0.146, mat="R1_CreamEnamel", axis_u="X", segments=10, bevel=0.0015)

    for idx, rx in enumerate((-0.045, -0.015, 0.015, 0.045)):
        core.add_cylinder(
            (rx, 0.013, -0.014),
            0.0228,
            0.011,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=8,
            bevel=0.0012,
            rot_deg_u=(idx * 18.0, 0.0, 0.0),
        )

    core_crystals = [
        ((-0.038, 0.024, -0.029), (0.018, 0.009, 0.008), (18.0, -14.0, 12.0)),
        ((0.0, 0.025, -0.030), (0.022, 0.010, 0.009), (14.0, -6.0, 16.0)),
        ((0.038, 0.024, -0.029), (0.018, 0.009, 0.008), (22.0, 12.0, -14.0)),
    ]
    for pos_u, sz_u, rot_u in core_crystals:
        core.add_bevel_box(pos_u, sz_u, mat="R1_CreamEnamel", bevel=0.0012, segments=1, rot_deg_u=rot_u)

    core_obj = core.build_object(parent=root)

    # -------------------------------------------------------------------------
    # 3. SightWindow: Clear sight-window bezel, specular glass reflection strips, brass bolts
    # -------------------------------------------------------------------------
    window = r1kit.MeshBuilder("SightWindow", pivot_u=(0.0, 0.0, 0.0))
    win_rot_u = (42.0, 0.0, 0.0)

    # Top and bottom armoured sight-window frame rails (leaving the central aperture open to CoreSample)
    window.add_bevel_box((0.0, 0.0335, -0.0165), (0.158, 0.006, 0.007), mat="R1_DarkGlass", bevel=0.0012, segments=1, rot_deg_u=win_rot_u)
    window.add_bevel_box((0.0, 0.0145, -0.0325), (0.158, 0.006, 0.007), mat="R1_DarkGlass", bevel=0.0012, segments=1, rot_deg_u=win_rot_u)

    for sx in (-0.075, 0.075):
        window.add_bevel_box((sx, 0.0240, -0.0245), (0.008, 0.028, 0.007), mat="R1_DarkGlass", bevel=0.0012, segments=1, rot_deg_u=win_rot_u)

    # Angled bevelled quartz glass reflection panes & measurement reticle strips across the window aperture
    window.add_bevel_box((0.0, 0.0298, -0.0205), (0.142, 0.0045, 0.0035), mat="R1_DarkGlass", bevel=0.0008, segments=1, rot_deg_u=(36.0, 0.0, 0.0))
    window.add_bevel_box((0.0, 0.0188, -0.0295), (0.142, 0.0045, 0.0035), mat="R1_DarkGlass", bevel=0.0008, segments=1, rot_deg_u=(48.0, 0.0, 0.0))

    for gx in (-0.038, 0.0, 0.038):
        window.add_bevel_box((gx, 0.0245, -0.0255), (0.0035, 0.022, 0.004), mat="R1_DarkGlass", bevel=0.0007, segments=1, rot_deg_u=win_rot_u)

    # Brass retainer bolts along the top and bottom window rails
    for bx in (-0.056, 0.0, 0.056):
        window.add_bevel_box((bx, 0.0345, -0.0175), (0.0045, 0.0035, 0.0045), mat="R1_Brass", bevel=0.0008, segments=1, rot_deg_u=win_rot_u)
        window.add_bevel_box((bx, 0.0155, -0.0335), (0.0045, 0.0035, 0.0045), mat="R1_Brass", bevel=0.0008, segments=1, rot_deg_u=win_rot_u)

    win_obj = window.build_object(parent=root)

    # -------------------------------------------------------------------------
    # 4. LabelPlate: Stencilled plaque reading 'CORE 41 · H₂O · I.V. 10/88'
    # -------------------------------------------------------------------------
    label = r1kit.MeshBuilder("LabelPlate", pivot_u=(0.0, 0.0, 0.0))
    label.add_bevel_box(
        (0.0, -0.0108, -0.0352),
        (0.156, 0.026, 0.004),
        mat="R1_Signage",
        bevel=0.0008,
        segments=1,
        rot_deg_u=(-26.0, 0.0, 0.0),
    )
    label_obj = label.build_object(parent=root)

    # -------------------------------------------------------------------------
    # 5. CarryHandle: Hinged carry handle loop on the upper-rear shoulder
    # -------------------------------------------------------------------------
    handle = r1kit.MeshBuilder("CarryHandle", pivot_u=(0.0, 0.0, 0.0))
    for lx in (-0.082, 0.082):
        handle.add_bevel_box((lx, 0.028, 0.018), (0.012, 0.014, 0.014), mat="R1_BareSteel", bevel=0.0018, segments=1, rot_deg_u=(-35.0, 0.0, 0.0))
        handle.add_cylinder((lx, 0.030, 0.020), 0.0038, 0.016, mat="R1_Brass", axis_u="X", segments=6, bevel=0.0008)
        handle.add_bevel_box((lx, 0.034, 0.027), (0.006, 0.009, 0.018), mat="R1_BareSteel", bevel=0.0014, segments=1, rot_deg_u=(-22.0, 0.0, 0.0))

    handle.add_cylinder((0.0, 0.0355, 0.032), 0.0040, 0.168, mat="R1_BareSteel", axis_u="X", segments=8, bevel=0.0008)
    handle.add_cylinder((0.0, 0.0355, 0.032), 0.0056, 0.092, mat="R1_Rubber", axis_u="X", segments=8, bevel=0.0012)
    for gx in (-0.036, 0.036):
        handle.add_cylinder((gx, 0.0355, 0.032), 0.0060, 0.012, mat="R1_OrangeStripe", axis_u="X", segments=8, bevel=0.001)
    handle.build_object(parent=root)

    # -------------------------------------------------------------------------
    # 6. FrostCap: Crystalline rime-frost accumulation on the -X cold-trap end
    # -------------------------------------------------------------------------
    frost = r1kit.MeshBuilder("FrostCap", pivot_u=(0.0, 0.0, 0.0))
    frost.add_cylinder((-0.2145, 0.0, 0.0), 0.0398, 0.021, mat="R1_CreamEnamel", axis_u="X", segments=8, bevel=0.0022, rot_deg_u=(12.0, 0.0, 0.0))

    frost_rings = [
        (-0.186, 0.0408, 0.015, 15.0),
        (-0.162, 0.0404, 0.016, 36.0),
        (-0.136, 0.0398, 0.014, 58.0),
    ]
    for fx, rad, dep, twist in frost_rings:
        frost.add_cylinder((fx, 0.0, 0.0), rad, dep, mat="R1_CreamEnamel", axis_u="X", segments=8, bevel=0.002, rot_deg_u=(twist, 0.0, 0.0))

    crystal_clusters = [
        ((-0.218, 0.016, -0.012), (0.014, 0.024, 0.026), (24.0, 15.0, -18.0)),
        ((-0.198, 0.023, -0.026), (0.034, 0.017, 0.016), (38.0, -12.0, 14.0)),
        ((-0.164, 0.022, -0.029), (0.042, 0.015, 0.014), (28.0, 18.0, -10.0)),
        ((-0.128, 0.025, -0.025), (0.036, 0.013, 0.012), (35.0, 10.0, -12.0)),
        ((-0.184, 0.026, 0.023), (0.038, 0.015, 0.014), (-28.0, 22.0, 14.0)),
        ((-0.146, 0.026, 0.023), (0.034, 0.013, 0.012), (-26.0, -16.0, 10.0)),
        ((-0.168, -0.017, -0.030), (0.038, 0.014, 0.012), (18.0, 20.0, -15.0)),
    ]
    for pos_u, sz_u, rot_u in crystal_clusters:
        frost.add_bevel_box(pos_u, sz_u, mat="R1_CreamEnamel", bevel=0.0018, segments=1, rot_deg_u=rot_u)

    frost_obj = frost.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Center_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Window_Anchor", parent=root, pivot_u=(0.0, 0.024, -0.025))
    r1kit.create_empty("Handle_Anchor", parent=root, pivot_u=(0.0, 0.036, 0.032))
    r1kit.create_empty("Frost_Anchor", parent=root, pivot_u=(-0.215, 0.0, 0.0))

    # Center the combined mesh bounds at (0, 0, 0)
    _center_hierarchy_at_origin(root)

    # Apply base triplanar UVs, then override LabelPlate, CoreSample, and FrostCap with exact planar UVs
    r1kit.apply_triplanar_uvs(root)

    tilt_rad = math.radians(-26.0)
    basis_u_label = Vector((1.0, 0.0, 0.0))
    basis_v_label = Vector((0.0, -math.sin(tilt_rad), math.cos(tilt_rad)))
    center_v_label = Vector((0.0, -0.0352, -0.0108)).dot(basis_v_label)
    _project_planar_uv(
        label_obj,
        x_min=-0.078,
        x_max=0.078,
        y_min=center_v_label - 0.013,
        y_max=center_v_label + 0.013,
        u_min=0.0,
        u_max=1.0,
        v_min=0.0,
        v_max=1.0,
        basis_u_b=basis_u_label,
        basis_v_b=basis_v_label,
    )

    _project_planar_uv(
        core_obj,
        x_min=-0.075,
        x_max=0.075,
        y_min=-0.038,
        y_max=0.038,
        u_min=0.54,
        u_max=0.96,
        v_min=0.06,
        v_max=0.94,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, -0.65, 0.76)),
    )

    _project_planar_uv(
        frost_obj,
        x_min=-0.225,
        x_max=-0.100,
        y_min=-0.042,
        y_max=0.042,
        u_min=0.04,
        u_max=0.46,
        v_min=0.06,
        v_max=0.94,
        basis_u_b=Vector((1.0, 0.0, 0.0)),
        basis_v_b=Vector((0.0, -0.60, 0.80)),
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.24)
    _boost_frost_vertex_colors(frost_obj, base_ao=0.92, min_glint=0.52)
    _boost_frost_vertex_colors(core_obj, base_ao=0.88, min_glint=0.42)
    _boost_frost_vertex_colors(win_obj, base_ao=0.90, min_glint=0.58)

    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    fbx_path = PROPS_DIR / "IceCoreTube.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _apply_preview_textures_for_render(repo_root: Path) -> None:
    """Assign the generated label and frost textures to temporary preview materials for Blender renders."""
    tex_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures"
    label_png = tex_dir / "T_R1_IceCoreLabel_Albedo.png"
    frost_png = tex_dir / "T_R1_IceCoreFrost_Albedo.png"

    label_obj = bpy.data.objects.get("LabelPlate")
    if label_obj is not None and label_png.exists():
        mat = bpy.data.materials.new(name="Preview_IceCoreLabel")
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        tex_node = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex_node.image = bpy.data.images.load(str(label_png))
        mat.node_tree.links.new(bsdf.inputs["Base Color"], tex_node.outputs["Color"])
        bsdf.inputs["Roughness"].default_value = 0.28
        label_obj.data.materials.clear()
        label_obj.data.materials.append(mat)

    if frost_png.exists():
        mat_frost = bpy.data.materials.new(name="Preview_IceCoreFrost")
        bsdf = mat_frost.node_tree.nodes.get("Principled BSDF")
        tex_node = mat_frost.node_tree.nodes.new("ShaderNodeTexImage")
        tex_node.image = bpy.data.images.load(str(frost_png))
        mat_frost.node_tree.links.new(bsdf.inputs["Base Color"], tex_node.outputs["Color"])
        bsdf.inputs["Roughness"].default_value = 0.12
        bsdf.inputs["Metallic"].default_value = 0.18
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = 0.92

        for obj_name in ("FrostCap", "CoreSample"):
            obj = bpy.data.objects.get(obj_name)
            if obj is not None:
                obj.data.materials.clear()
                obj.data.materials.append(mat_frost)

    win_obj = bpy.data.objects.get("SightWindow")
    if win_obj is not None:
        for slot in win_obj.material_slots:
            if slot.material and slot.material.name.startswith("R1_DarkGlass"):
                mat_win = slot.material.copy()
                mat_win.name = "Preview_SightWindowGlass"
                bsdf = mat_win.node_tree.nodes.get("Principled BSDF")
                if bsdf is not None:
                    bsdf.inputs["Base Color"].default_value = (0.48, 0.68, 0.86, 1.0)
                    bsdf.inputs["Roughness"].default_value = 0.04
                    bsdf.inputs["Metallic"].default_value = 0.42
                slot.material = mat_win


def render_hold_icon(out_png: Path) -> None:
    """Render a 512x512 transparent PNG of IceCoreTube for the hold icon source."""
    out_png.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene

    for obj in list(scene.objects):
        if obj.type in {"LIGHT", "CAMERA"}:
            bpy.data.objects.remove(obj, do_unlink=True)

    for candidate in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        try:
            scene.render.engine = candidate
            break
        except Exception:
            continue

    world = scene.world or bpy.data.worlds.new("IconWorld")
    scene.world = world
    bg = world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs[0].default_value = (0.14, 0.18, 0.24, 1.0)
        bg.inputs[1].default_value = 0.85

    key_data = bpy.data.lights.new(name="IconKey", type="SUN")
    key_data.energy = 3.8
    key_data.color = (1.0, 0.95, 0.86)
    key_obj = bpy.data.objects.new(name="IconKey", object_data=key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = Euler((math.radians(48.0), 0.0, math.radians(24.0)), "XYZ")

    rim_data = bpy.data.lights.new(name="IconFrostRim", type="SUN")
    rim_data.energy = 2.4
    rim_data.color = (0.76, 0.90, 1.0)
    rim_obj = bpy.data.objects.new(name="IconFrostRim", object_data=rim_data)
    scene.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = Euler((math.radians(36.0), 0.0, math.radians(-135.0)), "XYZ")

    fill_data = bpy.data.lights.new(name="IconFill", type="SUN")
    fill_data.energy = 1.6
    fill_data.color = (0.90, 0.94, 1.0)
    fill_obj = bpy.data.objects.new(name="IconFill", object_data=fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = Euler((math.radians(72.0), 0.0, math.radians(-12.0)), "XYZ")

    cam_data = bpy.data.cameras.new(name="IconCam")
    cam_data.lens = 48.0
    cam_data.clip_start = 0.01
    cam_data.clip_end = 20.0
    cam_obj = bpy.data.objects.new(name="IconCam", object_data=cam_data)
    scene.collection.objects.link(cam_obj)

    cam_pos = Vector((-0.07, -0.66, 0.27))
    target = Vector((-0.005, -0.005, 0.0))
    direction = (target - cam_pos).normalized()
    cam_obj.location = cam_pos
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam_obj

    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = str(out_png)
    bpy.ops.render.render(write_still=True)


def render_crate_and_rover_eclipse_shot(repo_root: Path, root: bpy.types.Object, out_png: Path) -> None:
    """Stage IceCoreTube inside an open Crate_M next to Rover 07 in eclipse dark under 07's lamps."""
    out_png.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene

    for obj in list(scene.objects):
        if obj.type in {"LIGHT", "CAMERA"}:
            bpy.data.objects.remove(obj, do_unlink=True)

    # Import Crate_M.fbx at (0.52, -0.05, 0.0) and orient it so its front faces -Y (toward the camera) and its Lid swings UP & BACK
    crate_fbx = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Crates" / "Crate_M.fbx"
    if crate_fbx.exists():
        before_crate = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(crate_fbx))
        crate_objs = [o for o in bpy.data.objects if o not in before_crate]
        for o in crate_objs:
            if o.type == "MESH" and o.data is not None:
                for slot in o.material_slots:
                    if slot.material is not None:
                        base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                        if base_m is not None:
                            slot.material = base_m
        crate_root = bpy.data.objects.get("Crate_M")
        lid = bpy.data.objects.get("Lid")
        cut_pt = bpy.data.objects.get("CutPoint")
        if crate_root is not None:
            crate_root.location = Vector((0.52, -0.05, 0.0))
            bpy.context.view_layer.update()
            if cut_pt is not None and cut_pt.matrix_world.translation.y > crate_root.matrix_world.translation.y:
                crate_root.rotation_euler.z += math.pi
                bpy.context.view_layer.update()
        if lid is not None:
            lid.rotation_mode = "XYZ"
            best_ang = -75.0
            best_z = -1e9
            for test_deg in (-75.0, 75.0):
                lid.rotation_euler = Euler((math.radians(test_deg), 0.0, 0.0), "XYZ")
                bpy.context.view_layer.update()
                z_val = cut_pt.matrix_world.translation.z if cut_pt is not None else lid.matrix_world.translation.z
                if z_val > best_z:
                    best_z = z_val
                    best_ang = test_deg
            lid.rotation_euler = Euler((math.radians(best_ang), 0.0, 0.0), "XYZ")
            bpy.context.view_layer.update()

    # Place IceCoreTube inside Crate_M's open cavity at (0.52, -0.06, 0.22) tilted toward the camera
    root.location = Vector((0.52, -0.06, 0.22))
    root.rotation_euler = Euler((math.radians(-18.0), 0.0, 0.0), "XYZ")

    # Also place a second foreground IceCoreTube resting on the regolith right in front of Crate_M under 07's lamps
    fg_root = bpy.data.objects.new("IceCoreTube_Foreground", None)
    scene.collection.objects.link(fg_root)
    for child in root.children:
        if child.type == "MESH":
            dup = child.copy()
            dup.data = child.data.copy()
            scene.collection.objects.link(dup)
            dup.parent = fg_root
    fg_root.location = Vector((0.46, -0.46, 0.042))
    fg_root.rotation_euler = Euler((math.radians(-12.0), 0.0, math.radians(-10.0)), "XYZ")

    # Import Rover 07 parked cleanly to the left of Crate_M, aiming its headlamps at Crate_M and IceCoreTube
    rover_fbx = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover" / "RoverModel.fbx"
    if rover_fbx.exists():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(rover_fbx))
        rover_objs = [o for o in bpy.data.objects if o not in before]
        for o in rover_objs:
            if o.type == "MESH" and o.data is not None:
                for slot in o.material_slots:
                    if slot.material is not None:
                        base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                        if base_m is not None:
                            slot.material = base_m
            if o.parent is None:
                o.location = Vector((-1.32, 0.22, 0.0))
                o.rotation_euler.z += math.radians(-118.0)

    # Lunar basalt ground plane
    floor_builder = r1kit.MeshBuilder("EclipseBasaltFloor", pivot_u=(0.0, 0.0, 0.0))
    floor_builder.add_bevel_box((0.0, -0.02, 0.0), (12.0, 0.04, 12.0), mat="R1_Basalt", bevel=0.002, segments=1)
    floor_builder.build_object()

    # Eclipse dark environment
    world = scene.world or bpy.data.worlds.new("EclipseWorld")
    scene.world = world
    bg = world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs[0].default_value = (0.04, 0.06, 0.11, 1.0)
        bg.inputs[1].default_value = 0.65

    earth_data = bpy.data.lights.new(name="EclipseEarthlight", type="SUN")
    earth_data.energy = 0.85
    earth_data.color = (0.58, 0.72, 0.98)
    earth_obj = bpy.data.objects.new(name="EclipseEarthlight", object_data=earth_data)
    scene.collection.objects.link(earth_obj)
    earth_obj.rotation_euler = Euler((math.radians(48.0), 0.0, math.radians(32.0)), "XYZ")

    # 07's warm twin headlamps shining directly onto Crate_M and IceCoreTube
    for idx, lamp_pos in enumerate((Vector((-0.52, -0.08, 0.72)), Vector((-0.42, 0.12, 0.72)))):
        spot_data = bpy.data.lights.new(name=f"Rover07_Headlamp_{idx}", type="SPOT")
        spot_data.energy = 95.0
        spot_data.color = (1.0, 0.86, 0.64)
        spot_data.spot_size = math.radians(66.0)
        spot_data.spot_blend = 0.40
        spot_data.shadow_soft_size = 0.05
        spot_obj = bpy.data.objects.new(name=f"Rover07_Headlamp_{idx}", object_data=spot_data)
        scene.collection.objects.link(spot_obj)
        spot_obj.location = lamp_pos
        aim = (Vector((0.48, -0.22, 0.14)) - lamp_pos).normalized()
        spot_obj.rotation_euler = aim.to_track_quat("-Z", "Y").to_euler()

    # Cool grazing specular bounce light so the frost and sight window catch light vividly
    glint_data = bpy.data.lights.new(name="FrostGlintFill", type="POINT")
    glint_data.energy = 16.0
    glint_data.color = (0.78, 0.90, 1.0)
    glint_obj = bpy.data.objects.new(name="FrostGlintFill", object_data=glint_data)
    scene.collection.objects.link(glint_obj)
    glint_obj.location = Vector((0.38, -0.82, 0.48))

    cam_data = bpy.data.cameras.new(name="EclipseShotCam")
    cam_data.lens = 38.0
    cam_data.clip_start = 0.02
    cam_data.clip_end = 50.0
    cam_obj = bpy.data.objects.new(name="EclipseShotCam", object_data=cam_data)
    scene.collection.objects.link(cam_obj)

    cam_pos = Vector((0.42, -1.85, 0.98))
    target = Vector((0.02, -0.10, 0.28))
    direction = (target - cam_pos).normalized()
    cam_obj.location = cam_pos
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam_obj

    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.filepath = str(out_png)
    bpy.ops.render.render(write_still=True)


def main() -> int:
    renders_dir = REPO_ROOT / "Logs" / "renders"
    renders_dir.mkdir(parents=True, exist_ok=True)

    root = build_ice_core_tube()
    _apply_preview_textures_for_render(REPO_ROOT)
    render_hold_icon(renders_dir / "icecore_icon_source.png")
    render_crate_and_rover_eclipse_shot(REPO_ROOT, root, renders_dir / "icecore_crate_and_07.png")
    print(f"Rendered hold icon and crate/07 shots to {renders_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
