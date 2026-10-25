"""
Headless Blender 5.1 concept builder & render suite for the end-of-0.6 Caravan ("Đi tìm ánh sáng"),
Skid Sled / Coupler family variants, and 3 dedicated long-range vehicle concepts.

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P docs/art/concepts/caravan/render_caravan_concepts.py
"""
from __future__ import annotations

import math
import subprocess
import sys
from pathlib import Path

OUT_DIR = Path(__file__).resolve().parent
REPO_ROOT = OUT_DIR.parents[3]
BLENDER_DIR = REPO_ROOT / "tools" / "art" / "blender"
ROVER_DIR = BLENDER_DIR / "rover"

COMPOSITE_ONLY = "--composite-only" in sys.argv

if not COMPOSITE_ONLY:
    import bmesh
    import bpy
    import numpy as np
    from mathutils import Euler, Matrix, Vector

    for _p in (BLENDER_DIR, ROVER_DIR):
        if str(_p) not in sys.path:
            sys.path.insert(0, str(_p))
    sys.dont_write_bytecode = True

    import r1kit  # noqa: E402
    from build_rover import DIMENSIONS, MATERIALS  # noqa: E402
    from rover_parts import (  # noqa: E402
        build_rover_model_hierarchy,
        build_solar_wing_model,
        configure_procedural_crt_screen_shader,
        create_material_library,
    )
    import solar_array  # noqa: E402
    import battery_bank  # noqa: E402
    import antenna_mast  # noqa: E402
else:
    from PIL import Image, ImageDraw, ImageFont


# ==============================================================================
# 1. FAST ANALYTICAL VERTEX-COLOR & MATERIAL SETUP
# ==============================================================================
def fast_bake_vcol_hierarchy(root_or_objs) -> None:
    """
    Populate a deterministic R=AO, G=convex bevel wear, B=cavity/dust vertex-color layer
    on every mesh without slow Cycles CPU raytracing.
    """
    if isinstance(root_or_objs, bpy.types.Object):
        objs = r1kit.collect_hierarchy(root_or_objs)
    else:
        objs = list(root_or_objs)

    bpy.context.view_layer.update()
    for obj in objs:
        if obj.type != "MESH" or obj.data is None:
            continue
        if obj.name.startswith("Decal_"):
            obj.hide_render = True
            continue
        me = obj.data
        n_loops = len(me.loops)
        if n_loops == 0:
            continue

        # Keep existing active attribute name or create 'Color'
        ca = me.color_attributes.active_color
        if ca is None:
            ca = me.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="CORNER")
            me.color_attributes.active_color = ca
            me.color_attributes.render_color_index = 0

        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bm.edges.ensure_lookup_table()
        bm.faces.ensure_lookup_table()
        bm.normal_update()

        n_verts = len(bm.verts)
        v_convex = np.zeros(n_verts, dtype=np.float32)
        v_concave = np.zeros(n_verts, dtype=np.float32)

        for e in bm.edges:
            if not e.is_manifold:
                continue
            ang = e.calc_face_angle_signed()
            v0, v1 = e.verts[0].index, e.verts[1].index
            if ang > 0.08:
                val = min(1.0, ang / 0.90)
                if val > v_convex[v0]:
                    v_convex[v0] = val
                if val > v_convex[v1]:
                    v_convex[v1] = val
            elif ang < -0.06:
                val = min(1.0, -ang / 0.75)
                if val > v_concave[v0]:
                    v_concave[v0] = val
                if val > v_concave[v1]:
                    v_concave[v1] = val

        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()
        rgba = np.ones((n_loops, 4), dtype=np.float32)

        loop_idx = 0
        for f in bm.faces:
            fn_w = (rot_mat @ f.normal).normalized()
            min_edge = min((e.calc_length() for e in f.edges), default=0.1)
            wear_mask = 1.0 if min_edge < 0.028 else max(0.0, (0.048 - min_edge) / 0.020)
            for l in f.loops:
                vi = l.vert.index
                w_pos = world_mat @ l.vert.co
                # Analytical AO from world height, normal downwardness, and concavity
                h_ao = min(1.0, max(0.35, 0.55 + 0.45 * min(1.0, max(0.0, w_pos.z) / 0.55)))
                n_ao = 0.78 + 0.22 * fn_w.z
                cav = float(v_concave[vi])
                r_ao = float(np.clip(h_ao * n_ao * (1.0 - 0.38 * cav), 0.24, 1.0))
                g_wear = float(np.clip(v_convex[vi] * wear_mask * 0.85, 0.0, 0.88))
                ground_dust = max(0.0, min(0.45, (0.38 - max(0.0, w_pos.z)) / 0.38 * 0.40))
                b_dust = float(np.clip(max(cav * 0.65, ground_dust), 0.0, 0.85))
                rgba[loop_idx] = (r_ao, g_wear, b_dust, 1.0)
                loop_idx += 1

        bm.free()
        ca.data.foreach_set("color", rgba.ravel())
        me.update()


def finalize_all_render_materials() -> dict[str, bpy.types.Material]:
    """
    Configure all R1 materials with vertex-color shading (using active color attribute on each mesh)
    plus the procedural Amber CRT screen shader on R1_Screen.
    """
    mats = r1kit.ensure_materials(use_vertex_color_shading=True)
    for m_name, props in MATERIALS.items():
        if m_name not in mats:
            mat = bpy.data.materials.get(m_name) or bpy.data.materials.new(name=m_name)
            mat.use_nodes = True
            mats[m_name] = mat
    if "R1_Screen" in mats:
        configure_procedural_crt_screen_shader(mats["R1_Screen"], MATERIALS["R1_Screen"])

    # Make every ShaderNodeVertexColor read the active color layer (works for both 'Col' and 'Color')
    for mat in bpy.data.materials:
        if not mat.use_nodes or mat.node_tree is None:
            continue
        for node in mat.node_tree.nodes:
            if node.type == "VERTEX_COLOR":
                node.layer_name = ""
    return mats


def build_rover_instance(name_prefix: str = "Rover07", with_solar_wing: bool = True) -> bpy.types.Object:
    """
    Build Rover 07 (1.47m W x 2.01m L x 1.59m H) and return a top-level wrapper Empty so we can
    place or rotate the entire rover cleanly in any scene.
    """
    mats = create_material_library(MATERIALS, for_render=True)
    for k, v in r1kit.ensure_materials(use_vertex_color_shading=False).items():
        mats[k] = v
    rover_root = build_rover_model_hierarchy(DIMENSIONS, mats)
    if with_solar_wing:
        sw_socket = bpy.data.objects.get("SolarWing")
        if sw_socket is not None:
            build_solar_wing_model(DIMENSIONS, mats, parent=sw_socket, unity_loc=(0.0, 0.0, 0.0))

    # Hide Decal_KA_* wafer nodes in render so no un-textured decal cards appear
    for obj in r1kit.collect_hierarchy(rover_root):
        if obj.name.startswith("Decal_"):
            obj.hide_render = True
        # Prefix names so multiple rover builds never collide
        if name_prefix != "RoverModel":
            obj.name = f"{name_prefix}_{obj.name}"

    return rover_root


def place_object_u(
    obj: bpy.types.Object,
    pos_u: tuple[float, float, float],
    yaw_deg_u: float = 0.0,
    pitch_deg_u: float = 0.0,
    roll_deg_u: float = 0.0,
) -> None:
    """Place a root object at Unity coordinates (X_right, Y_up, Z_fwd) and Unity Euler orientation."""
    obj.location = r1kit.u_pos(*pos_u)
    obj.rotation_euler = r1kit.u_rot(pitch_deg_u, yaw_deg_u, roll_deg_u)


def setup_camera_and_render(
    cam_pos_u: tuple[float, float, float],
    target_u: tuple[float, float, float],
    out_path: Path,
    res_x: int = 1280,
    res_y: int = 720,
    fov_deg: float | None = None,
    lens_mm: float = 45.0,
    ortho_scale: float | None = None,
    freestyle: bool = True,
) -> Path:
    """Aim camera in Unity coordinates and render via Eevee + Freestyle."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 32
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"

    cam_obj = bpy.data.objects.get("_ConceptCam")
    if cam_obj is None:
        cam_data = bpy.data.cameras.new("_ConceptCam")
        cam_obj = bpy.data.objects.new("_ConceptCam", cam_data)
        bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    cam_data = cam_obj.data
    cam_data.clip_start = 0.10
    cam_data.clip_end = 400.0

    pos_b = r1kit.u_pos(*cam_pos_u)
    tgt_b = r1kit.u_pos(*target_u)
    cam_obj.location = pos_b
    direction = (tgt_b - pos_b).normalized()
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler("XYZ")

    if ortho_scale is not None:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho_scale
    else:
        cam_data.type = "PERSP"
        if fov_deg is not None:
            cam_data.lens_unit = "FOV"
            cam_data.angle = math.radians(fov_deg)
        else:
            cam_data.lens_unit = "MILLIMETERS"
            cam_data.lens = lens_mm

    r1kit._render_to_file(out_path, res_x=res_x, res_y=res_y, freestyle=freestyle)
    return out_path


# ==============================================================================
# 2. CARAVAN HARDWARE: COUPLER RACK, RIGID COUPLERS, AND SKID SLED VARIANTS A/B/C
# ==============================================================================
def build_rover_coupler_rack(parent_rover: bpy.types.Object) -> bpy.types.Object:
    """
    Build the R1 Coupler Rack mounted on 07's rear deck (around CargoSocket / TowHook, Z = -0.46..-0.82 m).
    Holds 2 stowed spare short rigid couplers, a heavy copper-wound winch drum & fairlead at TowHook,
    and 1 warm amber convoy status beacon.
    """
    mb = r1kit.MeshBuilder("CouplerRack_07", pivot_u=(0.0, 0.0, 0.0))

    # Rear deck mounting saddle frame (straddling 07's rear chute/bumper at Y=0.44..0.88, Z=-0.48..-0.78)
    for sx in (-1.0, 1.0):
        # Soft pillowed cream-enamel side cheek plates with orange stripe band
        mb.add_bevel_box(
            center_u=(sx * 0.36, 0.68, -0.60),
            size_u=(0.075, 0.38, 0.28),
            mat="R1_CreamEnamel",
            bevel=0.022,
            segments=2,
            top_scale_xz=(0.88, 0.86),
        )
        mb.add_bevel_box(
            center_u=(sx * 0.365, 0.64, -0.60),
            size_u=(0.082, 0.068, 0.29),
            mat="R1_OrangeStripe",
            bevel=0.010,
            segments=1,
        )
        # Stowed spare short rigid coupler clamped vertically on each side of the rack!
        mb.add_cylinder(
            center_u=(sx * 0.42, 0.72, -0.62),
            radius=0.032,
            length=0.52,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=12,
            bevel=0.006,
        )
        mb.add_cylinder(
            center_u=(sx * 0.42, 0.72, -0.62),
            radius=0.038,
            length=0.14,
            mat="R1_OrangeStripe",
            axis_u="Y",
            segments=12,
            bevel=0.005,
        )
        for ey in (0.45, 0.99):
            mb.add_cylinder(
                center_u=(sx * 0.42, ey, -0.62),
                radius=0.044,
                length=0.065,
                mat="R1_Brass",
                axis_u="X",
                segments=12,
                bevel=0.008,
            )

    # Upper arched tubular cross-bar & tool tray
    mb.add_bevel_box(
        center_u=(0.0, 0.86, -0.62),
        size_u=(0.78, 0.065, 0.20),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=2,
    )
    # Single warm amber convoy beacon on top-left of coupler rack
    mb.add_cylinder(
        center_u=(-0.28, 0.92, -0.62),
        radius=0.042,
        length=0.045,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.005,
    )
    mb.add_cylinder(
        center_u=(-0.28, 0.96, -0.62),
        radius=0.035,
        radius_top=0.024,
        length=0.055,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.006,
    )

    # Heavy Winch Drum & Fairlead housing at WinchSocket/TowHook (0, 0.38, -0.72)
    mb.add_bevel_box(
        center_u=(0.0, 0.40, -0.73),
        size_u=(0.38, 0.16, 0.18),
        mat="R1_GreyMetal",
        bevel=0.018,
        segments=2,
    )
    mb.add_wire_spool(
        center_u=(0.0, 0.40, -0.74),
        flange_radius=0.095,
        width=0.22,
        axis_u="X",
        coil_mat="R1_Copper",
        flange_mat="R1_BareSteel",
        hub_mat="R1_Brass",
    )
    # Tow clevis jaw at Z = -0.82
    mb.add_bevel_box(
        center_u=(0.0, 0.36, -0.83),
        size_u=(0.14, 0.08, 0.12),
        mat="R1_Brass",
        bevel=0.012,
        segments=2,
    )
    return mb.build_object(parent=parent_rover)


def build_winch_cable_link(
    p0_u: tuple[float, float, float],
    p1_u: tuple[float, float, float],
    name: str = "WinchTowCable",
) -> bpy.types.Object:
    """Build the physical braided steel/copper winch tow cable between 07's rear winch and Car 1's front hitch."""
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    pts = []
    n_pts = 14
    for i in range(n_pts + 1):
        t = i / n_pts
        x = p0_u[0] * (1.0 - t) + p1_u[0] * t
        z = p0_u[2] * (1.0 - t) + p1_u[2] * t
        # Subtle tensioned catenary sag (3.5 cm)
        sag = 0.035 * 4.0 * t * (1.0 - t)
        y = p0_u[1] * (1.0 - t) + p1_u[1] * t - sag
        pts.append((x, y, z))

    mb.add_tube_path(pts, radius=0.022, mat="R1_BareSteel", segments=10)

    # End strain-relief sleeves & high-visibility orange safety collars along the tow line
    for t_mark, mat_m, r_m in ((0.08, "R1_Brass", 0.036), (0.32, "R1_OrangeStripe", 0.034), (0.68, "R1_OrangeStripe", 0.034), (0.92, "R1_Brass", 0.036)):
        pa = tuple(p0_u[k] * (1.0 - (t_mark - 0.03)) + p1_u[k] * (t_mark - 0.03) for k in range(3))
        pb = tuple(p0_u[k] * (1.0 - (t_mark + 0.03)) + p1_u[k] * (t_mark + 0.03) for k in range(3))
        mb.add_tube_path([pa, pb], radius=r_m, mat=mat_m, segments=10)

    return mb.build_object()


def build_short_rigid_coupler(
    p0_u: tuple[float, float, float],
    p1_u: tuple[float, float, float],
    name: str = "RigidCoupler",
) -> bpy.types.Object:
    """
    Build a short rigid A-frame / tubular drawbar coupler between sled cars, with brass ball-joint clevises,
    coral-orange safety sleeve, and a looped dark-rubber power/telemetry jumper cable.
    """
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    mid_u = tuple(0.5 * (p0_u[k] + p1_u[k]) for k in range(3))

    # Central heavy tubular drawbar
    mb.add_pipe(p0_u, p1_u, radius=0.042, mat="R1_GreyMetal", segments=12)

    # Twin A-frame stabilizer struts from front/rear clevises to center collar
    p_a1 = tuple(p0_u[k] * 0.72 + p1_u[k] * 0.28 for k in range(3))
    p_a2 = tuple(p0_u[k] * 0.28 + p1_u[k] * 0.72 for k in range(3))
    for sx in (-1.0, 1.0):
        mb.add_strut(
            (p0_u[0] + sx * 0.16, p0_u[1], p0_u[2]),
            (mid_u[0] + sx * 0.06, mid_u[1], mid_u[2]),
            width=0.038,
            thickness=0.034,
            mat="R1_GreyMetal",
            bevel=0.006,
        )
        mb.add_strut(
            (p1_u[0] + sx * 0.16, p1_u[1], p1_u[2]),
            (mid_u[0] + sx * 0.06, mid_u[1], mid_u[2]),
            width=0.038,
            thickness=0.034,
            mat="R1_GreyMetal",
            bevel=0.006,
        )

    # Central pillowed cream/orange damper collar
    mb.add_tube_path([p_a1, p_a2], radius=0.064, mat="R1_CreamEnamel", segments=14)
    p_o1 = tuple(p0_u[k] * 0.58 + p1_u[k] * 0.42 for k in range(3))
    p_o2 = tuple(p0_u[k] * 0.42 + p1_u[k] * 0.58 for k in range(3))
    mb.add_tube_path([p_o1, p_o2], radius=0.068, mat="R1_OrangeStripe", segments=14)

    # Brass spherical hitch knuckles + vertical drop pins at both ends
    for ep in (p0_u, p1_u):
        mb.add_cylinder(
            center_u=ep,
            radius=0.065,
            length=0.085,
            mat="R1_Brass",
            axis_u="Y",
            segments=14,
            bevel=0.012,
        )
        mb.add_cylinder(
            center_u=(ep[0], ep[1] + 0.055, ep[2]),
            radius=0.024,
            length=0.06,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.004,
        )

    # Looped rubber HV power/data umbilical jumper arched alongside the drawbar
    umb_pts = []
    for i in range(9):
        t = i / 8.0
        ux = (p0_u[0] * (1.0 - t) + p1_u[0] * t) + 0.14 * math.sin(math.pi * t)
        uy = (p0_u[1] * (1.0 - t) + p1_u[1] * t) + 0.11 * math.sin(math.pi * t)
        uz = p0_u[2] * (1.0 - t) + p1_u[2] * t
        umb_pts.append((ux, uy, uz))
    mb.add_tube_path(umb_pts, radius=0.018, mat="R1_Rubber", segments=8)

    return mb.build_object()


def build_sled_variant(
    variant: str = "A",
    name: str = "SkidSled",
    deck_width: float = 1.48,
    deck_length: float = 1.84,
    include_front_coupler: bool = False,
) -> bpy.types.Object:
    """
    Build one of the 3 R1 Skid Sled / Coupler family variants in 07's soft rounded shape language:
      - 'A': Simple Runner Sled (twin wide upturned ski runners at +/-0.61m track + pillowed cream/coral bolster)
      - 'B': Roller Sled (upturned boat-hull pan + 4 wide elastomer/brass drum rollers on articulated bogies)
      - 'C': Tracked Dolly (stadium-loop rubber crawler pods with semicircular rounded ends + geared turntable bolster)
    Returns root Empty at (0,0,0) with deck top at Y = 0.22 m.
    """
    root = r1kit.create_empty(name, pivot_u=(0.0, 0.0, 0.0))
    mb = r1kit.MeshBuilder(f"{name}_Mesh", pivot_u=(0.0, 0.0, 0.0))

    hw = deck_width * 0.5
    hl = deck_length * 0.5
    track_x = 0.58  # Matches 07's 1.22m wheel track footprint

    # --- Shared Soft Pillowed R1 Upper Cargo Deck & Side Bolsters (Y = 0.14 .. 0.28 m) ---
    # Central dark-metal load bed with recessed rubber anti-slip pads
    mb.add_bevel_box(
        center_u=(0.0, 0.18, 0.0),
        size_u=(deck_width - 0.16, 0.075, deck_length - 0.14),
        mat="R1_GreyMetal",
        bevel=0.022,
        segments=2,
    )
    for sz in (-0.48, 0.0, 0.48):
        mb.add_bevel_box(
            center_u=(0.0, 0.222, sz * hl),
            size_u=(deck_width - 0.34, 0.018, 0.28),
            mat="R1_Rubber",
            bevel=0.006,
            segments=1,
        )

    # Soft rounded Cream-Enamel side bolsters & front/rear curved bumpers with Coral-Orange beltline
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * (hw - 0.085), 0.215, 0.0),
            size_u=(0.17, 0.135, deck_length - 0.06),
            mat="R1_CreamEnamel",
            bevel=0.038,
            segments=3,
            top_scale_xz=(0.86, 0.94),
        )
        # Continuous coral-orange stripe along outer flank
        mb.add_bevel_box(
            center_u=(sx * (hw - 0.078), 0.205, 0.0),
            size_u=(0.178, 0.046, deck_length - 0.12),
            mat="R1_OrangeStripe",
            bevel=0.012,
            segments=2,
        )
        # 2 Brass tie-down lashing rings per side
        for sz in (-0.52, 0.52):
            mb.add_torus(
                center_u=(sx * (hw - 0.01), 0.23, sz * hl),
                major_radius=0.032,
                minor_radius=0.009,
                mat="R1_Brass",
                axis_u="Z",
                major_segments=14,
                minor_segments=8,
            )

    # Rounded front & rear cross-bolster aprons + central clevis hitch jaws
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, 0.20, sz * (hl - 0.07)),
            size_u=(deck_width - 0.12, 0.11, 0.15),
            mat="R1_CreamEnamel",
            bevel=0.032,
            segments=2,
        )
        mb.add_bevel_box(
            center_u=(0.0, 0.19, sz * (hl + 0.04)),
            size_u=(0.24, 0.085, 0.12),
            mat="R1_GreyMetal",
            bevel=0.018,
            segments=2,
        )
        mb.add_cylinder(
            center_u=(0.0, 0.19, sz * (hl + 0.07)),
            radius=0.045,
            length=0.095,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.008,
        )

    # Single warm amber side/rear convoy marker lamp on left-rear bolster corner
    mb.add_cylinder(
        center_u=(-hw + 0.08, 0.295, -hl + 0.16),
        radius=0.032,
        length=0.036,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.006,
    )
    # Sage salvage plate on right-front bolster
    mb.add_bevel_box(
        center_u=(hw - 0.005, 0.22, hl * 0.35),
        size_u=(0.018, 0.075, 0.26),
        mat="R1_SagePatch",
        bevel=0.005,
        segments=1,
    )

    # --- Variant-Specific Running Gear (Y = 0.00 .. 0.16 m) ---
    if variant == "A":
        # Variant A: Simple Runner Sled — twin wide upturned ski runners with curled tips & shock blocks
        runner_w = 0.22
        for sx in (-1.0, 1.0):
            rx = sx * track_x
            # Flat ground-contact ski sole (Y = 0.00..0.042)
            mb.add_bevel_box(
                center_u=(rx, 0.022, 0.0),
                size_u=(runner_w, 0.044, deck_length - 0.22),
                mat="R1_BareSteel",
                bevel=0.012,
                segments=2,
            )
            # Wear ridge along ski spine
            mb.add_bevel_box(
                center_u=(rx, 0.052, 0.0),
                size_u=(runner_w * 0.62, 0.032, deck_length - 0.28),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )
            # Upturned curved bow (+Z) and stern (-Z) ski tips so it glides over rocks in both directions
            for sz in (-1.0, 1.0):
                tip_pts = [
                    (rx, 0.022, sz * (hl - 0.14)),
                    (rx, 0.055, sz * (hl + 0.02)),
                    (rx, 0.125, sz * (hl + 0.15)),
                    (rx, 0.185, sz * (hl + 0.19)),
                ]
                for idx in range(len(tip_pts) - 1):
                    mb.add_strut(
                        tip_pts[idx],
                        tip_pts[idx + 1],
                        width=runner_w * (0.95 - 0.12 * idx),
                        thickness=0.040,
                        mat="R1_BareSteel",
                        bevel=0.010,
                    )
            # 3 elastomeric rubber + brass shock-absorber stanchions per runner
            for sz in (-0.58, 0.0, 0.58):
                mb.add_bevel_box(
                    center_u=(rx, 0.105, sz * hl),
                    size_u=(0.16, 0.095, 0.20),
                    mat="R1_Rubber",
                    bevel=0.016,
                    segments=2,
                )
                mb.add_cylinder(
                    center_u=(rx + sx * 0.07, 0.105, sz * hl),
                    radius=0.024,
                    length=0.09,
                    mat="R1_Brass",
                    axis_u="Y",
                    segments=10,
                    bevel=0.004,
                )

    elif variant == "B":
        # Variant B: Roller Sled — upturned boat-hull skid pan + 4 wide low-pressure barrel rollers on rocking bogies
        # Central smooth curved belly pan (sits at Y=0.07m so rollers protrude below to Y=0.00m)
        mb.add_bevel_box(
            center_u=(0.0, 0.11, 0.0),
            size_u=(deck_width - 0.42, 0.07, deck_length - 0.16),
            mat="R1_CreamEnamel",
            bevel=0.028,
            segments=2,
            top_scale_xz=(1.08, 1.06),
        )
        # 4 wide barrel rollers (Left/Right x Front/Rear bogies)
        roller_r = 0.135
        roller_w = 0.34
        for sx in (-1.0, 1.0):
            rx = sx * (track_x - 0.03)
            # Rocker bogie side beam
            mb.add_bevel_box(
                center_u=(rx, 0.135, 0.0),
                size_u=(0.10, 0.065, deck_length * 0.68),
                mat="R1_GreyMetal",
                bevel=0.016,
                segments=2,
            )
            mb.add_cylinder(
                center_u=(rx + sx * 0.05, 0.14, 0.0),
                radius=0.048,
                length=0.05,
                mat="R1_Brass",
                axis_u="X",
                segments=12,
                bevel=0.006,
            )
            for sz in (-0.50, 0.50):
                rz = sz * hl
                # Wide rubber drum roller with central brass divider band & end hubs
                mb.add_cylinder(
                    center_u=(rx, roller_r, rz),
                    radius=roller_r,
                    length=roller_w,
                    mat="R1_Rubber",
                    axis_u="X",
                    segments=20,
                    bevel=0.024,
                    bevel_segments=2,
                )
                mb.add_cylinder(
                    center_u=(rx, roller_r, rz),
                    radius=roller_r + 0.005,
                    length=0.048,
                    mat="R1_OrangeStripe",
                    axis_u="X",
                    segments=20,
                    bevel=0.006,
                )
                mb.add_cylinder(
                    center_u=(rx, roller_r, rz),
                    radius=roller_r * 0.52,
                    length=roller_w + 0.03,
                    mat="R1_Brass",
                    axis_u="X",
                    segments=14,
                    bevel=0.008,
                )

    elif variant == "C":
        # Variant C: Tracked Dolly — twin stadium-loop rubber crawler pods + geared turntable bolster
        # Central 360-deg geared turntable ring between track chassis and upper bolster
        mb.add_cylinder(
            center_u=(0.0, 0.15, 0.0),
            radius=0.48,
            length=0.055,
            mat="R1_Brass",
            axis_u="Y",
            segments=24,
            bevel=0.008,
        )
        mb.add_bevel_box(
            center_u=(0.0, 0.11, 0.0),
            size_u=(deck_width - 0.18, 0.055, 0.52),
            mat="R1_GreyMetal",
            bevel=0.015,
            segments=2,
        )
        # Left & Right stadium-loop rubber track pods (rounded semicircular ends, NOT boxy!)
        pod_r = 0.135
        pod_span_z = deck_length * 0.56
        pod_w = 0.25
        for sx in (-1.0, 1.0):
            rx = sx * track_x
            # Upper/lower rubber track belt span
            mb.add_bevel_box(
                center_u=(rx, pod_r, 0.0),
                size_u=(pod_w, pod_r * 2.0, pod_span_z),
                mat="R1_Rubber",
                bevel=0.022,
                segments=2,
            )
            # Semicircular front & rear track idler ends
            for sz in (-0.5, 0.5):
                iz = sz * pod_span_z
                mb.add_cylinder(
                    center_u=(rx, pod_r, iz),
                    radius=pod_r,
                    length=pod_w,
                    mat="R1_Rubber",
                    axis_u="X",
                    segments=20,
                    bevel=0.022,
                    bevel_segments=2,
                )
                mb.add_cylinder(
                    center_u=(rx, pod_r, iz),
                    radius=pod_r * 0.62,
                    length=pod_w + 0.028,
                    mat="R1_Brass",
                    axis_u="X",
                    segments=14,
                    bevel=0.008,
                )
            # Center road-wheel & cream-enamel armored track shield
            mb.add_cylinder(
                center_u=(rx, pod_r * 0.85, 0.0),
                radius=pod_r * 0.52,
                length=pod_w + 0.024,
                mat="R1_GreyMetal",
                axis_u="X",
                segments=14,
                bevel=0.006,
            )
            mb.add_bevel_box(
                center_u=(rx + sx * 0.08, pod_r * 1.12, 0.0),
                size_u=(0.12, 0.11, pod_span_z + 0.16),
                mat="R1_CreamEnamel",
                bevel=0.028,
                segments=2,
            )
            mb.add_bevel_box(
                center_u=(rx + sx * 0.086, pod_r * 1.12, 0.0),
                size_u=(0.125, 0.036, pod_span_z + 0.08),
                mat="R1_OrangeStripe",
                bevel=0.008,
                segments=1,
            )

    if include_front_coupler:
        # Attach a projecting rigid coupler drawbar on the front (+Z) for the Sled Variants showcase
        p_hitch = (0.0, 0.19, hl + 0.07)
        p_tip = (0.0, 0.34, hl + 0.78)
        mb.add_pipe(p_hitch, p_tip, radius=0.038, mat="R1_GreyMetal", segments=12)
        for sx in (-1.0, 1.0):
            mb.add_strut(
                (sx * 0.22, 0.19, hl + 0.02),
                (0.0, 0.27, hl + 0.44),
                width=0.034,
                thickness=0.030,
                mat="R1_GreyMetal",
                bevel=0.006,
            )
        p_c1 = (0.0, 0.25, hl + 0.34)
        p_c2 = (0.0, 0.29, hl + 0.56)
        mb.add_tube_path([p_c1, p_c2], radius=0.058, mat="R1_OrangeStripe", segments=12)
        mb.add_cylinder(
            center_u=p_tip,
            radius=0.058,
            length=0.075,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.010,
        )

    mb.build_object(parent=root)
    return root


# ==============================================================================
# 3. PACKED CARAVAN PAYLOAD BUILDERS (CARS 1, 2, AND 3)
# ==============================================================================
def build_car1_packed_solar(sled_variant: str = "A") -> bpy.types.Object:
    """
    Build Car 1: Packed R1 SolarArray mounted on a Skid Sled (1.48m W x 1.84m L).
    Uses the real SolarArray structure builder with wings folded flat/clamshell over the octagonal
    cream/orange gearbox and tripod truss legs stowed horizontally along the sled bolsters.
    """
    car_root = build_sled_variant(variant=sled_variant, name="Car1_PackedSolar", deck_width=1.48, deck_length=1.84)

    # Build authentic SolarArray stages 3 & 4 at origin first so child local offsets are exact, then place on sled
    sa_root = r1kit.create_empty("SolarArray_Packed", parent=car_root, pivot_u=(0.0, 0.0, 0.0))
    solar_array._build_stage_3(sa_root)
    solar_array._build_stage_4(sa_root)
    sa_root.location = r1kit.u_pos(0.0, -0.44, 0.0)

    # Pose SolarArray into compact horizontal packed state on the sled cradle:
    # Elevation horizontal (0 deg) and Wing_L / Wing_R folded downward (+/-46 deg) tent-style over the octagonal gearbox!
    for obj in r1kit.collect_hierarchy(sa_root):
        if obj.name == "Azimuth":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Elevation":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Wing_L":
            obj.rotation_euler = (0.0, math.radians(46.0), 0.0)
        elif obj.name == "Wing_R":
            obj.rotation_euler = (0.0, math.radians(-46.0), 0.0)

    # Add custom R1 travel cradle & stowed tripod truss legs strapped along the sled bolsters
    cradle_mb = r1kit.MeshBuilder("Car1_TravelCradle", pivot_u=(0.0, 0.0, 0.0))
    # Gearbox base saddle collar on sled deck
    cradle_mb.add_oct_prism(
        center_u=(0.0, 0.28, 0.0),
        width_x=0.96,
        height_y=0.12,
        depth_z=0.86,
        chamfer=0.22,
        mat="R1_GreyMetal",
        bevel=0.016,
    )
    # Stowed tripod truss legs + circular footpads clamped along left & right sled bolsters
    for sx in (-1.0, 1.0):
        lx = sx * 0.56
        cradle_mb.add_pipe((lx, 0.34, -0.58), (lx, 0.34, 0.58), radius=0.028, mat="R1_GreyMetal", segments=10)
        cradle_mb.add_cylinder(
            center_u=(lx, 0.35, -0.64),
            radius=0.11,
            length=0.04,
            mat="R1_CreamEnamel",
            axis_u="Z",
            segments=12,
            bevel=0.008,
        )
    # Padded coral-orange & rubber travel clamp arch locking the folded solar wings against vibration
    for sz in (-0.38, 0.28):
        cradle_mb.add_bevel_box(
            center_u=(0.0, 0.94, sz),
            size_u=(1.36, 0.048, 0.085),
            mat="R1_OrangeStripe",
            bevel=0.012,
            segments=2,
        )
        for sx in (-1.0, 1.0):
            cradle_mb.add_strut(
                (sx * 0.64, 0.26, sz),
                (sx * 0.64, 0.94, sz),
                width=0.042,
                thickness=0.042,
                mat="R1_GreyMetal",
                bevel=0.006,
            )
            cradle_mb.add_cylinder(
                center_u=(sx * 0.64, 0.55, sz),
                radius=0.032,
                length=0.08,
                mat="R1_Brass",
                axis_u="Y",
                segments=10,
                bevel=0.005,
            )
    cradle_mb.build_object(parent=car_root)
    return car_root


def build_car2_packed_battery(sled_variant: str = "A") -> bpy.types.Object:
    """
    Build Car 2: Packed R1 BatteryBank mounted on a Skid Sled (1.48m W x 1.68m L).
    Uses the real BatteryBank structure builder seated in a shock-isolated sled cradle with
    SafetyBar locked, Plungers seated, SightGlasses & RoofBeacon glowing warm amber, and travel straps.
    """
    car_root = build_sled_variant(variant=sled_variant, name="Car2_PackedBattery", deck_width=1.48, deck_length=1.68)

    bb_root = r1kit.create_empty("BatteryBank_Packed", parent=car_root, pivot_u=(0.0, 0.0, 0.0))
    battery_bank._build_stage_1(bb_root)
    battery_bank._build_stage_2(bb_root)
    battery_bank._build_stage_3(bb_root)
    battery_bank._build_stage_4(bb_root)
    battery_bank.pose_battery_bank_rest(bb_root)
    bb_root.location = r1kit.u_pos(0.0, 0.20, 0.0)

    # Add travel hold-down straps, corner shock bumpers, and coiled HV umbilical cables on sled deck
    clamp_mb = r1kit.MeshBuilder("Car2_TravelClamps", pivot_u=(0.0, 0.0, 0.0))
    for sx in (-1.0, 1.0):
        # Diagonal padded hold-down struts from sled bolsters to BatteryBank mid-belt
        clamp_mb.add_strut(
            (sx * 0.64, 0.26, 0.46),
            (sx * 0.56, 0.82, 0.30),
            width=0.045,
            thickness=0.035,
            mat="R1_OrangeStripe",
            bevel=0.008,
        )
        clamp_mb.add_strut(
            (sx * 0.64, 0.26, -0.46),
            (sx * 0.56, 0.82, -0.30),
            width=0.045,
            thickness=0.035,
            mat="R1_OrangeStripe",
            bevel=0.008,
        )
        # Coiled copper/rubber HV jumper spool mounted on rear deck corner
        clamp_mb.add_wire_spool(
            center_u=(sx * 0.48, 0.34, -0.58),
            flange_radius=0.105,
            width=0.12,
            axis_u="X",
            coil_mat="R1_Copper",
            flange_mat="R1_GreyMetal",
            hub_mat="R1_Brass",
        )
    clamp_mb.build_object(parent=car_root)
    return car_root


def build_car3_folded_mast(sled_variant: str = "A") -> bpy.types.Object:
    """
    Build Car 3: Folded R1 AntennaMast (Radio Mast) mounted on a Skid Sled (1.50m W x 2.02m L).
    Uses the real AntennaMast cabinet (Stages 2, 3, 4) seated toward the front of the sled with
    Mast_Seg2 & Mast_Seg3 telescoped inside Mast_Seg1 and the entire Mast folded horizontally (-82 deg)
    backward onto a padded rear travel crutch, with the parabolic Dish stowed flat on top.
    """
    car_root = build_sled_variant(variant=sled_variant, name="Car3_FoldedMast", deck_width=1.50, deck_length=2.02)

    am_root = r1kit.create_empty("AntennaMast_Folded", parent=car_root, pivot_u=(0.0, 0.0, 0.0))
    antenna_mast._build_stage_2(am_root)
    antenna_mast._build_stage_3(am_root)
    antenna_mast._build_stage_4(am_root)
    am_root.location = r1kit.u_pos(0.0, -0.06, 0.42)

    bpy.context.view_layer.update()
    # Telescope Mast_Seg2 & Mast_Seg3 partially down and fold Mast backward (+84 deg around Blender X = toward Unity -Z)
    for obj in r1kit.collect_hierarchy(am_root):
        if obj.name == "Mast":
            # Pivot Mast backward over the rear sled crutch (-Z in Unity = -Y in Blender -> rotate +84 deg around Blender X)
            obj.rotation_euler = (math.radians(84.0), 0.0, 0.0)
        elif obj.name == "Mast_Seg2":
            obj.location = r1kit.u_pos(0.0, 0.24, 0.0)
        elif obj.name == "Mast_Seg3":
            obj.location = r1kit.u_pos(0.0, 0.48, 0.0)
        elif obj.name == "DishAzimuth":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Dish":
            # Dish faces upward/horizontal like a stowed dish over the folded mast
            obj.rotation_euler = (math.radians(-12.0), 0.0, 0.0)

    # Build base adapter cradle under the cabinet + rear padded A-frame travel crutch supporting the folded mast
    crutch_mb = r1kit.MeshBuilder("Car3_MastCrutch", pivot_u=(0.0, 0.0, 0.0))
    # Cabinet base adapter ring on front half of sled (Z = +0.42)
    crutch_mb.add_bevel_box(
        center_u=(0.0, 0.24, 0.42),
        size_u=(1.04, 0.10, 0.98),
        mat="R1_GreyMetal",
        bevel=0.022,
        segments=2,
    )
    # Rear A-frame travel crutch at Z = -0.58 supporting the folded lattice mast at Y = 1.40
    for sx in (-1.0, 1.0):
        crutch_mb.add_strut(
            (sx * 0.48, 0.24, -0.58),
            (sx * 0.20, 1.40, -0.58),
            width=0.052,
            thickness=0.048,
            mat="R1_GreyMetal",
            bevel=0.008,
        )
        crutch_mb.add_strut(
            (sx * 0.48, 0.24, -0.22),
            (sx * 0.20, 1.40, -0.58),
            width=0.042,
            thickness=0.040,
            mat="R1_GreyMetal",
            bevel=0.006,
        )
    # Padded cream/orange U-saddle at top of rear crutch
    crutch_mb.add_bevel_box(
        center_u=(0.0, 1.40, -0.58),
        size_u=(0.56, 0.085, 0.16),
        mat="R1_CreamEnamel",
        bevel=0.020,
        segments=2,
    )
    crutch_mb.add_bevel_box(
        center_u=(0.0, 1.44, -0.58),
        size_u=(0.48, 0.045, 0.18),
        mat="R1_OrangeStripe",
        bevel=0.010,
        segments=1,
    )
    crutch_mb.build_object(parent=car_root)
    return car_root


# ==============================================================================
# 4. BASIN FLOOR ENVIRONMENT & DAY C ("LOW SUN") LIGHTING
# ==============================================================================
def build_basin_floor_and_day_c_lighting(
    curve_fn=None,
    include_tracks: bool = True,
) -> list[bpy.types.Object]:
    """
    Create the Mare Imbrium basin floor with subtle regolith undulations, wheel/sled tracks,
    scattered weathered R1 basalt boulders, distant rim ridge, crescent Earth in the sky,
    and Day C ('Low sun') dramatic grazing warm sunlight + cool Earthshine fill.
    """
    env_objs: list[bpy.types.Object] = []

    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("DayC_World")
    scene.world.use_nodes = True
    w_nt = scene.world.node_tree
    w_nt.nodes.clear()
    w_out = w_nt.nodes.new("ShaderNodeOutputWorld")
    w_bg = w_nt.nodes.new("ShaderNodeBackground")
    # Deep lunar indigo sky with subtle cool Earthshine horizon glow
    w_bg.inputs["Color"].default_value = r1kit._srgb_to_linear("#0D1326")
    w_bg.inputs["Strength"].default_value = 0.90
    w_nt.links.new(w_bg.outputs["Background"], w_out.inputs["Surface"])

    # Sculpted basin floor mesh (120m x 120m) with gentle mare swells, distant crater rim wall, and caravan ruts
    bm = bmesh.new()
    nx, nz = 96, 96
    span = 110.0
    verts_grid = []
    for iz in range(nz + 1):
        vz = -span * 0.5 + span * (iz / nz)
        row = []
        for ix in range(nx + 1):
            vx = -span * 0.5 + span * (ix / nx)
            dist_c = math.hypot(vx, vz + 4.0)
            # Keep the immediate caravan corridor (dist < 14m) flat with only gentle 2cm micro-undulation
            corridor_flat = min(1.0, max(0.0, (dist_c - 10.0) / 16.0))
            swell = (
                0.28 * math.sin(vx * 0.11 + 0.4) * math.cos(vz * 0.09 - 0.3)
                + 0.14 * math.cos(vx * 0.23 - vz * 0.19)
            ) * corridor_flat
            # Distant crater rim rise beyond 28m
            rim_rise = max(0.0, (dist_c - 26.0) / 28.0) ** 1.8 * 6.5 * (
                0.75 + 0.25 * math.sin(math.atan2(vz, vx) * 5.0 + 1.1)
            )
            vy = swell + rim_rise

            # Subtle twin wheel/runner ruts along the caravan path (Z from -13.0 to +1.0)
            if include_tracks and -13.5 <= vz <= 1.2:
                cx_path = curve_fn(vz) if curve_fn is not None else 0.0
                for rut_x in (cx_path - 0.59, cx_path + 0.59):
                    dx_r = abs(vx - rut_x)
                    if dx_r < 0.22:
                        vy -= 0.024 * math.cos((dx_r / 0.22) * math.pi * 0.5)
                    elif dx_r < 0.34:
                        vy += 0.008 * math.sin(((dx_r - 0.22) / 0.12) * math.pi)

            v = bm.verts.new(r1kit.u_pos(vx, vy, vz))
            row.append(v)
        verts_grid.append(row)

    for iz in range(nz):
        for ix in range(nx):
            f = bm.faces.new((
                verts_grid[iz][ix],
                verts_grid[iz][ix + 1],
                verts_grid[iz + 1][ix + 1],
                verts_grid[iz + 1][ix],
            ))
            f.smooth = True

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    floor_me = bpy.data.meshes.new("BasinFloor_Mesh")
    bm.to_mesh(floor_me)
    bm.free()

    reg_mat = bpy.data.materials.new("R1_LunarRegolith_DayC")
    reg_mat.use_nodes = True
    r_nt = reg_mat.node_tree
    r_bsdf = r_nt.nodes["Principled BSDF"]
    # Warm-cool lunar mare regolith grey-taupe so cream & coral hardware pop cleanly
    r_bsdf.inputs["Base Color"].default_value = r1kit._srgb_to_linear("#5E626C")
    r_bsdf.inputs["Roughness"].default_value = 0.88
    r_bsdf.inputs["Metallic"].default_value = 0.02
    floor_me.materials.append(reg_mat)

    floor_obj = bpy.data.objects.new("BasinFloor", floor_me)
    bpy.context.collection.objects.link(floor_obj)
    env_objs.append(floor_obj)

    # Scatter weathered R1 basalt boulders flanking the caravan trail
    rock_mb = r1kit.MeshBuilder("BasinBoulders", pivot_u=(0.0, 0.0, 0.0))
    boulder_specs = [
        ((-3.8, 0.0, 3.2), (1.15, 0.68, 0.95), 101, 18.0),
        ((4.2, 0.0, 1.8), (1.45, 0.82, 1.20), 102, -32.0),
        ((-4.5, 0.0, -3.5), (0.95, 0.55, 0.85), 103, 45.0),
        ((3.9, 0.0, -6.2), (1.30, 0.75, 1.10), 104, -15.0),
        ((-3.6, 0.0, -9.8), (1.10, 0.62, 0.92), 105, 64.0),
        ((4.8, 0.0, -11.5), (1.60, 0.95, 1.35), 106, 12.0),
        ((-7.5, 0.0, 8.5), (2.40, 1.35, 1.95), 107, -28.0),
        ((8.2, 0.0, 9.2), (2.80, 1.55, 2.20), 108, 36.0),
        ((-2.4, 0.0, 0.4), (0.48, 0.26, 0.42), 109, 22.0),
        ((2.5, 0.0, -2.8), (0.52, 0.28, 0.44), 110, -40.0),
        ((-2.3, 0.0, -6.4), (0.44, 0.24, 0.38), 111, 15.0),
    ]
    for pos_u, sz_u, seed_v, yaw_v in boulder_specs:
        rock_mb.add_weathered_rock(
            center_u=pos_u,
            size_u=sz_u,
            seed=seed_v,
            lon_segs=14,
            lat_rings=7,
            rot_deg_u=(0.0, yaw_v, 0.0),
            mat="R1_Basalt",
        )
    env_objs.append(rock_mb.build_object())

    # Day C ("Low sun") Lighting Setup:
    # 1. Grazing Golden-Warm Low Key Sun (11 deg elevation above horizon, casting crisp long caravan shadows)
    sun_key = bpy.data.lights.new(name="DayC_LowSunKey", type="SUN")
    sun_key.energy = 4.25
    sun_key.color = r1kit._srgb_to_linear("#FFDFB3")[:3]
    sun_key.angle = math.radians(2.2)
    sun_key_obj = bpy.data.objects.new("DayC_LowSunKey", sun_key)
    bpy.context.collection.objects.link(sun_key_obj)
    # In Blender (Z=up), rotation X = 77 deg means sun is 13 deg above horizon!
    sun_key_obj.rotation_euler = Euler((math.radians(76.5), math.radians(-8.0), math.radians(48.0)), "XYZ")
    env_objs.append(sun_key_obj)

    # 2. Cool Blue Earthshine Sky Fill (so shadow sides of cream/coral hulls remain rich and readable)
    sun_fill = bpy.data.lights.new(name="DayC_EarthshineFill", type="SUN")
    sun_fill.energy = 1.45
    sun_fill.color = r1kit._srgb_to_linear("#7BA8F0")[:3]
    sun_fill.angle = math.radians(35.0)
    sun_fill_obj = bpy.data.objects.new("DayC_EarthshineFill", sun_fill)
    bpy.context.collection.objects.link(sun_fill_obj)
    sun_fill_obj.rotation_euler = Euler((math.radians(52.0), math.radians(12.0), math.radians(-132.0)), "XYZ")
    env_objs.append(sun_fill_obj)

    # 3. Warm Regolith Bounce / Back-Rim Sun
    sun_rim = bpy.data.lights.new(name="DayC_RimBounce", type="SUN")
    sun_rim.energy = 1.35
    sun_rim.color = r1kit._srgb_to_linear("#FFD29D")[:3]
    sun_rim.angle = math.radians(12.0)
    sun_rim_obj = bpy.data.objects.new("DayC_RimBounce", sun_rim)
    bpy.context.collection.objects.link(sun_rim_obj)
    sun_rim_obj.rotation_euler = Euler((math.radians(68.0), 0.0, math.radians(-165.0)), "XYZ")
    env_objs.append(sun_rim_obj)

    return env_objs


# ==============================================================================
# 5. THREE DEDICATED LONG-RANGE VEHICLE CONCEPTS (WITH 07 DOCKED AT TRUE SCALE)
# ==============================================================================
def build_concept1_seatruck_carrier() -> bpy.types.Object:
    """
    Concept 1: 'Nomad' Modular Carrier Cab (Seatruck-style Front-Dock Hauler, 3.25m W x 4.90m L x 2.38m H).
    07 drives up the central belly ramp into the front U-shaped horseshoe docking collar so 07's own
    Amber CRT head peers out over the rounded cream-enamel bow visor as the live pilot brain!
    6 wide-track Ø 1.12m balloon wheels on articulated rocker-bogies + side battery/cargo saddle pods + rear caravan coupler.
    """
    root = r1kit.create_empty("Concept1_SeatruckCarrier", pivot_u=(0.0, 0.0, 0.0))

    # Place real Rover 07 (1.47x2.01x1.59m) docked inside the forward horseshoe bay (elevated at Y = 0.34m, Z = +0.72m)
    rover = build_rover_instance(name_prefix="C1_Rover07", with_solar_wing=True)
    rover.parent = root
    place_object_u(rover, (0.0, 0.34, 0.72), yaw_deg_u=0.0)

    mb = r1kit.MeshBuilder("Concept1_CarrierBody", pivot_u=(0.0, 0.0, 0.0))

    # 1. Central Docking Cradle Bed & Folded Rear Belly Ramp (Y = 0.28..0.38m, Z = -2.10..+1.55m)
    mb.add_bevel_box(
        center_u=(0.0, 0.32, 0.15),
        size_u=(1.68, 0.11, 3.10),
        mat="R1_GreyMetal",
        bevel=0.028,
        segments=2,
    )
    # Brass wheel-chock guides locking 07's tyres on the dock platform
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.61, 0.385, 0.72),
            size_u=(0.28, 0.035, 1.85),
            mat="R1_Brass",
            bevel=0.008,
            segments=1,
        )
    # Articulated rear boarding ramp (sloping down from Z = -1.35 to -2.32 so 07 can roll on/off)
    mb.add_bevel_box(
        center_u=(0.0, 0.19, -1.835),
        size_u=(1.56, 0.075, 1.02),
        mat="R1_GreyMetal",
        bevel=0.018,
        segments=2,
        rot_deg_u=(15.0, 0.0, 0.0),
    )
    for sx in (-0.58, 0.58):
        mb.add_bevel_box(
            center_u=(sx, 0.22, -1.825),
            size_u=(0.28, 0.025, 0.98),
            mat="R1_Rubber",
            bevel=0.006,
            segments=1,
            rot_deg_u=(15.0, 0.0, 0.0),
        )

    # 2. Pillowed Cream-Enamel Bow Horseshoe Collar & Side Saddle Hull Pods (wrapping around 07!)
    # Front lower bumper apron under 07's chin (leaving 07's CRT head & scoop clear above Y = 1.05m!)
    mb.add_bevel_box(
        center_u=(0.0, 0.58, 1.88),
        size_u=(2.48, 0.44, 0.52),
        mat="R1_CreamEnamel",
        bevel=0.12,
        segments=4,
        top_scale_xz=(0.90, 0.85),
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.56, 1.91),
        size_u=(2.52, 0.11, 0.54),
        mat="R1_OrangeStripe",
        bevel=0.025,
        segments=2,
    )

    # Left & Right Rounded Pillowed Saddle Pods (housing auxiliary battery cells, docking clamps & cargo bays)
    for sx in (-1.0, 1.0):
        px = sx * 1.06
        # Main soft-pillowed cream hull pontoon
        mb.add_bevel_box(
            center_u=(px, 0.96, 0.05),
            size_u=(0.68, 1.08, 3.65),
            mat="R1_CreamEnamel",
            bevel=0.16,
            segments=4,
            top_scale_xz=(0.82, 0.88),
        )
        # Continuous Coral-Orange R1 beltline
        mb.add_bevel_box(
            center_u=(px + sx * 0.015, 0.86, 0.05),
            size_u=(0.70, 0.14, 3.68),
            mat="R1_OrangeStripe",
            bevel=0.028,
            segments=2,
        )
        # Pneumatic Brass Docking Clamp Arms reaching inward to lock onto 07's waist
        mb.add_cylinder(
            center_u=(sx * 0.68, 0.88, 0.72),
            radius=0.075,
            length=0.32,
            mat="R1_Brass",
            axis_u="X",
            segments=14,
            bevel=0.012,
        )
        mb.add_bevel_box(
            center_u=(sx * 0.52, 0.88, 0.72),
            size_u=(0.08, 0.18, 0.36),
            mat="R1_Rubber",
            bevel=0.018,
            segments=2,
        )
        # Side recessed battery cylinder array (4 vertical cells per side pod)
        for cz in (-0.85, -0.35, 0.15, 0.65):
            mb.add_cylinder(
                center_u=(px + sx * 0.12, 1.18, cz),
                radius=0.14,
                length=0.68,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=14,
                bevel=0.018,
            )
            mb.add_cylinder(
                center_u=(px + sx * 0.12, 1.54, cz),
                radius=0.11,
                length=0.05,
                mat="R1_Brass",
                axis_u="Y",
                segments=12,
                bevel=0.006,
            )

    # Sage salvage plate on left-rear pontoon shoulder
    mb.add_bevel_box(
        center_u=(-1.40, 1.05, -0.85),
        size_u=(0.025, 0.34, 0.58),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )

    # 3. Overhead Rounded Arch Roll-Cage & Folded Solar Canopy (behind 07's head at Z = -0.55..-1.55m, Y = 1.50..2.22m)
    mb.add_arch_housing(
        base_center_u=(0.0, 1.42, -0.85),
        width_x=2.36,
        height_y=0.76,
        depth_z=1.55,
        arch_radius=0.58,
        mat="R1_CreamEnamel",
        bevel=0.045,
        arch_segments=10,
        arch_axis_u="X",
    )
    # Solar panel array inset on top of rear canopy arch
    mb.add_bevel_box(
        center_u=(0.0, 2.19, -0.85),
        size_u=(1.62, 0.04, 1.28),
        mat="R1_SolarCell",
        bevel=0.008,
        segments=1,
    )
    # Single warm amber docking/convoy beacon on top of canopy brow
    mb.add_cylinder(
        center_u=(0.0, 2.26, -0.18),
        radius=0.068,
        radius_top=0.045,
        length=0.095,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=14,
        bevel=0.010,
    )

    # 4. Six Large Wide-Track R1 Balloon Wheels (Ø 1.08m, radius 0.54m) on Rocker-Bogie Arms
    w_rad = 0.54
    w_width = 0.36
    for sx in (-1.0, 1.0):
        wx = sx * 1.44
        # Rocker-bogie suspension beam
        mb.add_bevel_box(
            center_u=(sx * 1.24, 0.58, 0.0),
            size_u=(0.14, 0.14, 3.05),
            mat="R1_GreyMetal",
            bevel=0.024,
            segments=2,
        )
        for wz in (-1.42, 0.0, 1.42):
            mb.add_cylinder(
                center_u=(wx, w_rad, wz),
                radius=w_rad,
                length=w_width,
                mat="R1_Rubber",
                axis_u="X",
                segments=24,
                bevel=0.042,
                bevel_segments=2,
            )
            mb.add_cylinder(
                center_u=(wx + sx * 0.02, w_rad, wz),
                radius=w_rad * 0.54,
                length=w_width + 0.02,
                mat="R1_CreamEnamel",
                axis_u="X",
                segments=18,
                bevel=0.020,
            )
            mb.add_cylinder(
                center_u=(wx + sx * 0.04, w_rad, wz),
                radius=w_rad * 0.24,
                length=w_width + 0.05,
                mat="R1_Brass",
                axis_u="X",
                segments=12,
                bevel=0.010,
            )

    # Rear Caravan Hitch & Short Rigid Coupler Socket at Z = -2.38m
    mb.add_bevel_box(
        center_u=(0.0, 0.42, -1.75),
        size_u=(0.42, 0.18, 0.28),
        mat="R1_Brass",
        bevel=0.022,
        segments=2,
    )

    mb.build_object(parent=root)
    return root


def build_concept2_crawler_base() -> bpy.types.Object:
    """
    Concept 2: 'Boreas' Tracked Crawler Base (3.90m W x 7.20m L x 2.68m H).
    Mobile forward base on 4 massive stadium-loop rubber crawler tracks (soft rounded ends, NOT military/boxy).
    Wide rear ramp dropped so Rover 07 drives into the central open charge-dock deck, flanked by a portside
    30-cell submarine battery vault, starboard 3-slot cargo rack, overhead gantry arch, and dorsal solar wings.
    """
    root = r1kit.create_empty("Concept2_CrawlerBase", pivot_u=(0.0, 0.0, 0.0))

    # Place real Rover 07 (1.47x2.01x1.59m) parked on the central garage deck (Y = 0.56m, Z = -0.35m)
    rover = build_rover_instance(name_prefix="C2_Rover07", with_solar_wing=True)
    rover.parent = root
    place_object_u(rover, (0.0, 0.56, -0.35), yaw_deg_u=0.0)

    mb = r1kit.MeshBuilder("Concept2_CrawlerBody", pivot_u=(0.0, 0.0, 0.0))

    # 1. Main Pillowed Lower Chassis Tub & Central Roll-On Garage Deck (Y = 0.36..0.58m, Z = -2.65..+2.65m)
    mb.add_bevel_box(
        center_u=(0.0, 0.46, 0.0),
        size_u=(3.10, 0.22, 5.40),
        mat="R1_GreyMetal",
        bevel=0.055,
        segments=3,
    )
    # Inductive brass charging pad & wheel guides under 07 on the central deck
    mb.add_bevel_box(
        center_u=(0.0, 0.575, -0.35),
        size_u=(1.62, 0.03, 2.35),
        mat="R1_Rubber",
        bevel=0.008,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, 0.585, -0.35),
        radius=0.42,
        length=0.025,
        mat="R1_Copper",
        axis_u="Y",
        segments=20,
        bevel=0.005,
    )

    # Wide Dropped Rear Boarding Ramp (Z = -2.62 .. -4.12m, sloping from Y=0.50 down to Y=0.05)
    mb.add_bevel_box(
        center_u=(0.0, 0.275, -3.37),
        size_u=(1.92, 0.095, 1.56),
        mat="R1_CreamEnamel",
        bevel=0.025,
        segments=2,
        rot_deg_u=(16.5, 0.0, 0.0),
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.305, -3.36),
        size_u=(1.48, 0.035, 1.48),
        mat="R1_Rubber",
        bevel=0.008,
        segments=1,
        rot_deg_u=(16.5, 0.0, 0.0),
    )
    for sx in (-0.88, 0.88):
        mb.add_bevel_box(
            center_u=(sx, 0.315, -3.37),
            size_u=(0.14, 0.06, 1.54),
            mat="R1_OrangeStripe",
            bevel=0.012,
            segments=2,
            rot_deg_u=(16.5, 0.0, 0.0),
        )

    # 2. Quad Stadium-Loop Rubber Crawler Tracks (Left/Right x Front/Rear, rounded semicircular ends!)
    t_rad = 0.42
    t_span = 1.65
    t_width = 0.52
    for sx in (-1.0, 1.0):
        tx = sx * 1.68
        for tz_c in (-1.45, 1.45):
            # Stadium rubber track belt
            mb.add_bevel_box(
                center_u=(tx, t_rad, tz_c),
                size_u=(t_width, t_rad * 2.0, t_span),
                mat="R1_Rubber",
                bevel=0.045,
                segments=2,
            )
            for sz_end in (-0.5, 0.5):
                iz = tz_c + sz_end * t_span
                mb.add_cylinder(
                    center_u=(tx, t_rad, iz),
                    radius=t_rad,
                    length=t_width,
                    mat="R1_Rubber",
                    axis_u="X",
                    segments=24,
                    bevel=0.045,
                    bevel_segments=2,
                )
                mb.add_cylinder(
                    center_u=(tx + sx * 0.02, t_rad, iz),
                    radius=t_rad * 0.58,
                    length=t_width + 0.04,
                    mat="R1_Brass",
                    axis_u="X",
                    segments=16,
                    bevel=0.012,
                )
            # Soft rounded Cream-Enamel armored track fender skirt + Coral-Orange stripe
            mb.add_bevel_box(
                center_u=(tx + sx * 0.04, t_rad * 1.28, tz_c),
                size_u=(t_width + 0.08, 0.34, t_span + 0.52),
                mat="R1_CreamEnamel",
                bevel=0.085,
                segments=3,
                top_scale_xz=(0.88, 0.92),
            )
            mb.add_bevel_box(
                center_u=(tx + sx * 0.05, t_rad * 1.24, tz_c),
                size_u=(t_width + 0.10, 0.095, t_span + 0.46),
                mat="R1_OrangeStripe",
                bevel=0.018,
                segments=2,
            )

    # 3. Rounded Submarine Bow Telemetry Cabin (Z = +1.35..+2.75m, Y = 0.56..2.35m)
    mb.add_arch_housing(
        base_center_u=(0.0, 0.56, 1.95),
        width_x=2.85,
        height_y=1.68,
        depth_z=1.45,
        arch_radius=0.72,
        mat="R1_CreamEnamel",
        bevel=0.085,
        arch_segments=12,
        arch_axis_u="X",
    )
    mb.add_bevel_box(
        center_u=(0.0, 1.08, 1.96),
        size_u=(2.90, 0.16, 1.48),
        mat="R1_OrangeStripe",
        bevel=0.025,
        segments=2,
    )
    # Recessed dark-glass instrument visor & reel-to-reel telemetry drum on forward brow
    mb.add_bevel_box(
        center_u=(0.0, 1.58, 2.66),
        size_u=(1.55, 0.42, 0.08),
        mat="R1_DarkGlass",
        bevel=0.022,
        segments=2,
    )
    # Single warm amber dome beacon on forward brow roof
    mb.add_cylinder(
        center_u=(0.0, 2.32, 2.10),
        radius=0.085,
        radius_top=0.055,
        length=0.12,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=14,
        bevel=0.012,
    )

    # 4. Port & Starboard Side Superstructures Framing 07's Garage Bay (leaving the center open so 07 is 100% visible!)
    # Left (-X) Port Wall: Rounded Cream Battery Vault + 6 heavy cylindrical accumulator cells
    mb.add_bevel_box(
        center_u=(-1.18, 1.26, -0.55),
        size_u=(0.62, 1.38, 3.30),
        mat="R1_CreamEnamel",
        bevel=0.11,
        segments=3,
        top_scale_xz=(0.86, 0.94),
    )
    mb.add_bevel_box(
        center_u=(-1.20, 1.08, -0.55),
        size_u=(0.65, 0.14, 3.34),
        mat="R1_OrangeStripe",
        bevel=0.022,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(-1.50, 1.38, -1.20),
        size_u=(0.025, 0.42, 0.68),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )
    for cz in (-1.75, -1.15, -0.55, 0.05, 0.65):
        mb.add_cylinder(
            center_u=(-1.18, 1.45, cz),
            radius=0.18,
            length=0.95,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.020,
        )
        mb.add_cylinder(
            center_u=(-1.18, 1.96, cz),
            radius=0.13,
            length=0.06,
            mat="R1_Brass",
            axis_u="Y",
            segments=12,
            bevel=0.008,
        )

    # Right (+X) Starboard Wall: Open Tubular Gantry & 3 Stowed R1 Supply Crates + Folded Solar Wing Leaf
    mb.add_bevel_box(
        center_u=(1.18, 0.88, -0.55),
        size_u=(0.58, 0.62, 3.30),
        mat="R1_CreamEnamel",
        bevel=0.09,
        segments=3,
    )
    for cz in (-1.65, -0.55, 0.55):
        mb.add_supply_crate(
            center_u=(1.18, 1.36, cz),
            size_u=(0.48, 0.36, 0.72),
            rot_deg_u=(0.0, 90.0, 0.0),
        )

    # Overhead Gantry Crane Arch & Dorsal Solar Wing Array spanning at Z = +0.85m
    mb.add_arch_housing(
        base_center_u=(0.0, 1.85, 0.65),
        width_x=2.76,
        height_y=0.62,
        depth_z=0.95,
        arch_radius=0.45,
        mat="R1_GreyMetal",
        bevel=0.035,
        arch_segments=10,
        arch_axis_u="X",
    )
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.78, 2.50, 0.95),
            size_u=(1.18, 0.045, 1.85),
            mat="R1_SolarCell",
            bevel=0.010,
            segments=1,
            rot_deg_u=(0.0, 0.0, sx * -8.0),
        )

    mb.build_object(parent=root)
    return root


def build_concept3_rail_sled() -> bpy.types.Object:
    """
    Concept 3: 'Tereshkova' Linear-Induction Rail Sled (2.95m W x 5.80m L x 2.05m H on a 9.6m twin-tubular lunar rail spine).
    Double-ended low-slung R1 ferry car riding on pinch-roller magnetic bogies along an elevated modular lunar monorail/duorail
    with a central copper linear-induction stator strip. Rover 07 drives up the fold-down side boarding ramp onto the
    turntable dock pad. Zero dust kickup, zero wheel slip, bidirectional trunk-line transit.
    """
    root = r1kit.create_empty("Concept3_RailSled", pivot_u=(0.0, 0.0, 0.0))

    # Place real Rover 07 (1.47x2.01x1.59m) docked on the central ferry deck (Y = 0.46m, Z = 0.0m)
    rover = build_rover_instance(name_prefix="C3_Rover07", with_solar_wing=True)
    rover.parent = root
    place_object_u(rover, (0.0, 0.46, 0.0), yaw_deg_u=0.0)

    mb = r1kit.MeshBuilder("Concept3_RailSledBody", pivot_u=(0.0, 0.0, 0.0))

    # 1. Modular Twin-Tubular Lunar Rail Spine + Copper Linear-Induction Stator Strip (Z = -4.8m .. +4.8m)
    rail_len = 9.60
    rail_gauge_x = 0.68
    # Steel cross-sleepers & basalt anchor pads every 0.80m
    for iz in range(-5, 6):
        sz = iz * 0.86
        mb.add_bevel_box(
            center_u=(0.0, 0.06, sz),
            size_u=(1.88, 0.12, 0.24),
            mat="R1_GreyMetal",
            bevel=0.020,
            segments=2,
        )
        for sx in (-1.0, 1.0):
            mb.add_cylinder(
                center_u=(sx * 0.82, 0.02, sz),
                radius=0.14,
                length=0.04,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=10,
                bevel=0.006,
            )
    # Twin cylindrical guide rails (Y = 0.20m, X = +/-0.68m)
    for sx in (-1.0, 1.0):
        mb.add_pipe(
            (sx * rail_gauge_x, 0.20, -rail_len * 0.5),
            (sx * rail_gauge_x, 0.20, rail_len * 0.5),
            radius=0.062,
            mat="R1_BareSteel",
            segments=16,
        )
    # Central Copper Linear-Induction Stator Fin & GreyMetal spine beam
    mb.add_bevel_box(
        center_u=(0.0, 0.16, 0.0),
        size_u=(0.24, 0.09, rail_len),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.23, 0.0),
        size_u=(0.08, 0.08, rail_len - 0.10),
        mat="R1_Copper",
        bevel=0.012,
        segments=2,
    )

    # 2. Four Articulated Pinch-Roller & Linear-Induction Bogies under the Sled (Z = +/-1.85m, X = +/-0.68m)
    for sz in (-1.85, 1.85):
        # Central copper induction coil saddle straddling the stator fin
        mb.add_bevel_box(
            center_u=(0.0, 0.28, sz),
            size_u=(0.46, 0.16, 0.68),
            mat="R1_Copper",
            bevel=0.024,
            segments=2,
        )
        for sx in (-1.0, 1.0):
            bx = sx * rail_gauge_x
            mb.add_bevel_box(
                center_u=(bx, 0.28, sz),
                size_u=(0.34, 0.20, 0.76),
                mat="R1_GreyMetal",
                bevel=0.025,
                segments=2,
            )
            # Upper & lower angled rubber/brass pinch rollers gripping the tubular rail
            for rz_off in (-0.24, 0.24):
                mb.add_cylinder(
                    center_u=(bx, 0.31, sz + rz_off),
                    radius=0.085,
                    length=0.22,
                    mat="R1_Rubber",
                    axis_u="X",
                    segments=14,
                    bevel=0.012,
                )
                mb.add_cylinder(
                    center_u=(bx, 0.31, sz + rz_off),
                    radius=0.048,
                    length=0.26,
                    mat="R1_Brass",
                    axis_u="X",
                    segments=12,
                    bevel=0.006,
                )

    # 3. Low-Slung Double-Ended Pillowed R1 Ferry Deck (Y = 0.36..0.48m, Z = -2.75..+2.75m)
    mb.add_bevel_box(
        center_u=(0.0, 0.41, 0.0),
        size_u=(2.42, 0.12, 5.20),
        mat="R1_CreamEnamel",
        bevel=0.055,
        segments=3,
    )
    mb.add_bevel_box(
        center_u=(0.0, 0.41, 0.0),
        size_u=(2.46, 0.048, 5.24),
        mat="R1_OrangeStripe",
        bevel=0.012,
        segments=2,
    )
    # Central 360-deg Turntable Docking Pad under 07 (so 07 can drive on from either side or end!)
    mb.add_cylinder(
        center_u=(0.0, 0.46, 0.0),
        radius=1.08,
        length=0.035,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=32,
        bevel=0.008,
    )
    mb.add_torus(
        center_u=(0.0, 0.47, 0.0),
        major_radius=1.06,
        minor_radius=0.025,
        mat="R1_Brass",
        axis_u="Y",
        major_segments=32,
        minor_segments=10,
    )

    # Fold-down Side Roll-On Boarding Ramp on viewer-right (+X) so 07's loading path from the regolith is unmistakable!
    mb.add_bevel_box(
        center_u=(1.78, 0.24, 0.0),
        size_u=(1.26, 0.075, 1.82),
        mat="R1_CreamEnamel",
        bevel=0.020,
        segments=2,
        rot_deg_u=(0.0, 0.0, 18.5),
    )
    mb.add_bevel_box(
        center_u=(1.76, 0.27, 0.0),
        size_u=(1.20, 0.028, 1.42),
        mat="R1_Rubber",
        bevel=0.006,
        segments=1,
        rot_deg_u=(0.0, 0.0, 18.5),
    )

    # 4. Symmetrical Front & Rear Pillowed Control Bolsters, Capacitor Drums & Rail Couplers (Z = +/-2.15m)
    for sz in (-1.0, 1.0):
        bz = sz * 2.12
        # Soft rounded R1 end bulkhead cowling
        mb.add_arch_housing(
            base_center_u=(0.0, 0.46, bz),
            width_x=2.28,
            height_y=0.78,
            depth_z=0.92,
            arch_radius=0.42,
            mat="R1_CreamEnamel",
            bevel=0.055,
            arch_segments=10,
            arch_axis_u="X",
        )
        mb.add_bevel_box(
            center_u=(0.0, 0.78, bz),
            size_u=(2.32, 0.11, 0.95),
            mat="R1_OrangeStripe",
            bevel=0.020,
            segments=2,
        )
        # Twin horizontal Gold-Foil / Brass Flywheel Capacitor Drums on each end deck
        for sx in (-0.58, 0.58):
            mb.add_cylinder(
                center_u=(sx, 1.32, bz),
                radius=0.22,
                length=0.68,
                mat="R1_GreyMetal",
                axis_u="Z",
                segments=16,
                bevel=0.024,
            )
            mb.add_foil_wrap(
                (sx, 1.32, bz - 0.24),
                (sx, 1.32, bz + 0.24),
                radius=0.23,
                rings=5,
                segments=14,
            )
        # Automatic Brass Rail-Car Coupler projecting from each nose/tail at Z = +/-2.78m
        mb.add_cylinder(
            center_u=(0.0, 0.42, sz * 2.82),
            radius=0.095,
            length=0.38,
            mat="R1_Brass",
            axis_u="Z",
            segments=16,
            bevel=0.016,
        )

    # Sage salvage patch on portside bolster
    mb.add_bevel_box(
        center_u=(-1.20, 0.72, 0.0),
        size_u=(0.14, 0.36, 1.95),
        mat="R1_SagePatch",
        bevel=0.022,
        segments=2,
    )
    # Single warm amber rail-transit beacon on forward (+Z) bolster arch
    mb.add_cylinder(
        center_u=(0.0, 1.32, 2.12),
        radius=0.072,
        radius_top=0.048,
        length=0.11,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=14,
        bevel=0.010,
    )

    mb.build_object(parent=root)
    return root


# ==============================================================================
# 6. SHEET RENDERING & PIL ANNOTATION COMPOSITING
# ==============================================================================
def _load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (
        ["C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf", "C:/Windows/Fonts/consolab.ttf"]
        if bold
        else ["C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/consola.ttf"]
    )
    for fp in candidates:
        if Path(fp).exists():
            try:
                return ImageFont.truetype(fp, size=size)
            except Exception:
                pass
    return ImageFont.load_default()


def _draw_badge(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font,
    bg_rgb: tuple[int, int, int] = (28, 34, 52),
    border_rgb: tuple[int, int, int] = (255, 138, 91),
    text_rgb: tuple[int, int, int] = (242, 227, 198),
    pad_x: int = 12,
    pad_y: int = 6,
) -> tuple[int, int, int, int]:
    bbox = draw.textbbox((0, 0), text, font=font)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    x0, y0 = xy
    rect = (x0, y0, x0 + tw + pad_x * 2, y0 + th + pad_y * 2)
    draw.rounded_rectangle(rect, radius=6, fill=bg_rgb, outline=border_rgb, width=2)
    draw.text((x0 + pad_x, y0 + pad_y - 2), text, font=font, fill=text_rgb)
    return rect


def render_sheet1_caravan(tmp_dir: Path) -> Path:
    """
    Build and render Sheet 1: `docs/art/concepts/caravan/caravan_sheet.png`
    - 07 towing a 3-car train on the basin floor under Day C ('Low sun'):
      * Car 1: Packed SolarArray on a skid sled
      * Car 2: Packed BatteryBank on a skid sled
      * Car 3: Folded AntennaMast (radio mast) on a skid sled
      * Coupler rack on 07's rear deck + physical winch cable + short rigid couplers between cars
    - Views:
      1. Chase-cam view (7.5 m distance, 16 deg pitch, 52 deg FOV)
      2. Side view (full caravan profile)
      3. 3/4 rear view (convoy perspective looking forward from behind Car 3 toward 07)
    """
    p_chase = tmp_dir / "_s1_chase.png"
    p_side = tmp_dir / "_s1_side.png"
    p_rear34 = tmp_dir / "_s1_rear34.png"

    if not COMPOSITE_ONLY:
        print("[Caravan] Building Scene 1: 07 + 3-car Caravan on Basin Floor (Day C Low Sun)...")
        r1kit.reset_scene()

        # Gentle S-curve path x(z) so the 4-unit caravan reads with natural towed articulation in 3D
        def curve_x(z_u: float) -> float:
            if z_u >= -1.0:
                return 0.0
            t = min(1.0, (-z_u - 1.0) / 8.5)
            return -0.52 * (t ** 1.35)

        def curve_yaw_deg(z_u: float) -> float:
            dz = 0.05
            dx = curve_x(z_u + dz) - curve_x(z_u - dz)
            return math.degrees(math.atan2(dx, 2.0 * dz))

        build_basin_floor_and_day_c_lighting(curve_fn=curve_x, include_tracks=True)

        # 1. Lead Hauler: Rover 07 at (0, 0, 0) + Coupler Rack on rear deck
        rover07 = build_rover_instance(name_prefix="RoverModel", with_solar_wing=True)
        place_object_u(rover07, (0.0, 0.0, 0.0), yaw_deg_u=0.0)
        build_rover_coupler_rack(rover07)

        # 2. Car 1: Packed SolarArray on Skid Sled (Z = -2.85 m)
        z1 = -2.85
        x1 = curve_x(z1)
        yaw1 = curve_yaw_deg(z1)
        car1 = build_car1_packed_solar(sled_variant="A")
        place_object_u(car1, (x1, 0.0, z1), yaw_deg_u=yaw1)

        # Physical Winch Cable from 07's rear Winch/TowHook (0, 0.36, -0.83) to Car 1's front hitch
        rad1 = math.radians(yaw1)
        c1_front_hitch = (x1 + 0.99 * math.sin(rad1), 0.19, z1 + 0.99 * math.cos(rad1))
        c1_rear_hitch = (x1 - 0.99 * math.sin(rad1), 0.19, z1 - 0.99 * math.cos(rad1))
        build_winch_cable_link((0.0, 0.36, -0.83), c1_front_hitch, name="WinchCable_07_to_Car1")

        # 3. Car 2: Packed BatteryBank on Skid Sled (Z = -5.50 m)
        z2 = -5.50
        x2 = curve_x(z2)
        yaw2 = curve_yaw_deg(z2)
        car2 = build_car2_packed_battery(sled_variant="A")
        place_object_u(car2, (x2, 0.0, z2), yaw_deg_u=yaw2)
        rad2 = math.radians(yaw2)
        c2_front_hitch = (x2 + 0.91 * math.sin(rad2), 0.19, z2 + 0.91 * math.cos(rad2))
        c2_rear_hitch = (x2 - 0.91 * math.sin(rad2), 0.19, z2 - 0.91 * math.cos(rad2))
        build_short_rigid_coupler(c1_rear_hitch, c2_front_hitch, name="RigidCoupler_Car1_to_Car2")

        # 4. Car 3: Folded AntennaMast on Skid Sled (Z = -8.35 m)
        z3 = -8.35
        x3 = curve_x(z3)
        yaw3 = curve_yaw_deg(z3)
        car3 = build_car3_folded_mast(sled_variant="A")
        place_object_u(car3, (x3, 0.0, z3), yaw_deg_u=yaw3)
        rad3 = math.radians(yaw3)
        c3_front_hitch = (x3 + 1.08 * math.sin(rad3), 0.19, z3 + 1.08 * math.cos(rad3))
        build_short_rigid_coupler(c2_rear_hitch, c3_front_hitch, name="RigidCoupler_Car2_to_Car3")

        # Bake analytical vertex colors & finalize all materials
        fast_bake_vcol_hierarchy(bpy.data.objects)
        finalize_all_render_materials()

        # --- Render the 3 Required Views ---
        chase_dist = 7.50
        chase_pitch_rad = math.radians(16.0)
        chase_yaw_rad = math.radians(-34.0)
        chase_tgt = (-0.12, 0.62, -2.35)
        horiz_d = chase_dist * math.cos(chase_pitch_rad)
        chase_cam = (
            chase_tgt[0] + horiz_d * math.sin(chase_yaw_rad),
            chase_tgt[1] + chase_dist * math.sin(chase_pitch_rad),
            chase_tgt[2] - horiz_d * math.cos(chase_yaw_rad),
        )
        setup_camera_and_render(
            cam_pos_u=chase_cam,
            target_u=chase_tgt,
            out_path=p_chase,
            res_x=1360,
            res_y=780,
            fov_deg=52.0,
        )
        setup_camera_and_render(
            cam_pos_u=(-10.8, 1.35, -4.15),
            target_u=(-0.20, 0.72, -4.15),
            out_path=p_side,
            res_x=2200,
            res_y=640,
            ortho_scale=11.4,
        )
        setup_camera_and_render(
            cam_pos_u=(4.65, 2.35, -12.40),
            target_u=(-0.18, 0.68, -3.80),
            out_path=p_rear34,
            res_x=1000,
            res_y=780,
            fov_deg=44.0,
        )
        return OUT_DIR / "caravan_sheet.png"

    # --- Composite `caravan_sheet.png` (2400 x 1620) ---
    W, H = 2400, 1620
    sheet = Image.new("RGB", (W, H), (14, 18, 30))
    draw = ImageDraw.Draw(sheet)

    f_title = _load_font(30, bold=True)
    f_sub = _load_font(18, bold=False)
    f_hdr = _load_font(20, bold=True)
    f_body = _load_font(16, bold=False)
    f_tag = _load_font(15, bold=True)

    # Header bar
    draw.rectangle((0, 0, W, 86), fill=(20, 26, 42))
    draw.line((0, 86, W, 86), fill=(255, 138, 91), width=3)
    draw.text(
        (28, 14),
        'CARAVAN LOOK-TEST — 07 TOWING 3-CAR EXPEDITION TRAIN ("ĐI TÌM ÁNH SÁNG")',
        font=f_title,
        fill=(242, 227, 198),
    )
    draw.text(
        (28, 52),
        "Day C ('Low Sun' 13° grazing key + cool Earthshine fill)  |  Lead: Rover 07 (1.47×2.01×1.59m) + Rear Coupler Rack  |  Car 1: Packed SolarArray  |  Car 2: Packed BatteryBank  |  Car 3: Folded AntennaMast",
        font=f_sub,
        fill=(185, 196, 218),
    )

    # Top-Left: Chase-cam view (1360 x 780)
    img_chase = Image.open(p_chase).convert("RGB")
    sheet.paste(img_chase, (24, 104))
    draw.rounded_rectangle((24, 104, 24 + 1360, 104 + 780), radius=8, outline=(72, 84, 112), width=2)
    _draw_badge(
        draw,
        (40, 118),
        "1. CHASE-CAM VIEW  (d = 7.5 m, Pitch = 16°, FOV = 52° — Canonical Gameplay Towing Read)",
        f_hdr,
    )
    _draw_badge(draw, (40, 832), "Lead: 07 + Rear Coupler Rack + Braided Winch Tow Line (1.0 m)", f_tag, border_rgb=(219, 176, 82))

    # Top-Right: 3/4 Rear view (1000 x 780 -> cropped/fitted to 968 x 780)
    img_rear = Image.open(p_rear34).convert("RGB").resize((968, 780), Image.Resampling.LANCZOS)
    sheet.paste(img_rear, (1408, 104))
    draw.rounded_rectangle((1408, 104, 1408 + 968, 104 + 780), radius=8, outline=(72, 84, 112), width=2)
    _draw_badge(
        draw,
        (1424, 118),
        "3. 3/4 REAR CONVOY VIEW  (Trail Articulation & Coupler Chain)",
        f_hdr,
    )
    _draw_badge(draw, (1424, 832), "Tail Beacon: Car 3 Folded Mast Dish & Amber Convoy Lamp", f_tag, border_rgb=(219, 176, 82))

    # Bottom Full-Width: Side Profile View (2352 x 680)
    img_side = Image.open(p_side).convert("RGB").resize((2352, 680), Image.Resampling.LANCZOS)
    # Flip horizontally so 07 leads toward the RIGHT (+X on page) for intuitive left-to-right convoy reading
    img_side = img_side.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    sheet.paste(img_side, (24, 904))
    draw.rounded_rectangle((24, 904, 24 + 2352, 904 + 680), radius=8, outline=(72, 84, 112), width=2)
    _draw_badge(
        draw,
        (40, 918),
        "2. FULL CARAVAN SIDE ELEVATION  (Total Train Length = 9.85 m  |  1.22 m Shared Wheel/Runner Track)",
        f_hdr,
    )

    # Callout badges aligned beneath each unit in the Side View (Car 3 on left -> Car 2 -> Car 1 -> 07 on right)
    f_callout = _load_font(13, bold=True)
    callouts = [
        (56, 1528, "CAR 3: FOLDED ANTENNA MAST (1.50×2.02m) — Telescoped Mast + Dish"),
        (680, 1528, "CAR 2: PACKED BATTERY BANK (1.48×1.68m) — 4-Cell Vault + HV Spools"),
        (1290, 1528, "CAR 1: PACKED SOLAR ARRAY (1.48×1.84m) — Folded Wings + Tripod Legs"),
        (1895, 1528, "LEAD: ROVER 07 (1.47×2.01×1.59m) + Rear Coupler Rack"),
    ]
    for cx, cy, c_txt in callouts:
        _draw_badge(draw, (cx, cy), c_txt, f_callout, bg_rgb=(22, 28, 44), border_rgb=(120, 165, 130))

    out_file = OUT_DIR / "caravan_sheet.png"
    sheet.save(out_file, format="PNG")
    for p in (p_chase, p_side, p_rear34):
        p.unlink(missing_ok=True)
    print(f"[Caravan] Saved {out_file}")
    return out_file


def render_sheet2_sled_variants(tmp_dir: Path) -> Path:
    """
    Build and render Sheet 2: `docs/art/concepts/caravan/sled_variants.png`
    - 3 variants of the skid sled / coupler family in 07's soft R1 shape language:
      * Variant A: Simple Runner Sled (twin wide upturned ski runners + rigid A-frame coupler)
      * Variant B: Roller Sled (upturned boat-hull pan + 4 wide elastomer/brass drum rollers on articulated bogies)
      * Variant C: Tracked Dolly (stadium-loop rubber crawler pods + 360° geared turntable bolster)
    - Each column shows:
      1. Top Hero 3/4 View (sled + coupler hitched behind 07's rear deck for scale & coupler read)
      2. Middle Close-Up Running-Gear / Side-Underside View (isolating the runner / roller / tracked dolly mechanism)
      3. Bottom One-Line Pro / Con & Spec Card
    """
    hero_paths = {v: tmp_dir / f"_s2_hero_{v}.png" for v in ("A", "B", "C")}
    detail_paths = {v: tmp_dir / f"_s2_detail_{v}.png" for v in ("A", "B", "C")}

    if not COMPOSITE_ONLY:
        print("[Caravan] Building Scene 2: Sled / Coupler Family Variants (A, B, C)...")
        for var_code in ("A", "B", "C"):
            r1kit.reset_scene()
            build_basin_floor_and_day_c_lighting(curve_fn=None, include_tracks=True)

            rover = build_rover_instance(name_prefix=f"Sled{var_code}_Rover", with_solar_wing=True)
            place_object_u(rover, (0.0, 0.0, 1.85), yaw_deg_u=0.0)
            build_rover_coupler_rack(rover)

            sled = build_sled_variant(
                variant=var_code,
                name=f"SledVariant_{var_code}",
                deck_width=1.48,
                deck_length=1.84,
                include_front_coupler=False,
            )
            place_object_u(sled, (0.0, 0.0, -0.55), yaw_deg_u=0.0)

            load_mb = r1kit.MeshBuilder(f"SledLoad_{var_code}", pivot_u=(0.0, 0.0, 0.0))
            load_mb.add_supply_crate(center_u=(0.0, 0.44, -0.22), size_u=(0.96, 0.42, 0.82))
            load_mb.add_wire_spool(center_u=(-0.32, 0.36, 0.48), flange_radius=0.13, width=0.18, axis_u="X")
            load_mb.add_wire_spool(center_u=(0.32, 0.36, 0.48), flange_radius=0.13, width=0.18, axis_u="X")
            load_mb.build_object(parent=sled)

            build_short_rigid_coupler((0.0, 0.36, 1.02), (0.0, 0.19, 0.44), name=f"Coupler_{var_code}")

            fast_bake_vcol_hierarchy(bpy.data.objects)
            finalize_all_render_materials()

            setup_camera_and_render(
                cam_pos_u=(-2.75, 1.48, -2.65),
                target_u=(0.0, 0.42, 0.05),
                out_path=hero_paths[var_code],
                res_x=760,
                res_y=640,
                fov_deg=44.0,
            )
            setup_camera_and_render(
                cam_pos_u=(-2.38, 0.56, 0.30),
                target_u=(0.0, 0.20, -0.34),
                out_path=detail_paths[var_code],
                res_x=760,
                res_y=460,
                fov_deg=42.0,
            )
        return OUT_DIR / "sled_variants.png"

    # --- Composite `sled_variants.png` (2400 x 1540) ---
    W, H = 2400, 1540
    sheet = Image.new("RGB", (W, H), (14, 18, 30))
    draw = ImageDraw.Draw(sheet)

    f_title = _load_font(30, bold=True)
    f_sub = _load_font(18, bold=False)
    f_hdr = _load_font(22, bold=True)
    f_pro = _load_font(16, bold=True)
    f_body = _load_font(16, bold=False)

    draw.rectangle((0, 0, W, 86), fill=(20, 26, 42))
    draw.line((0, 86, W, 86), fill=(255, 138, 91), width=3)
    draw.text(
        (28, 14),
        "SKID SLED & RIGID COUPLER FAMILY — 3 RUNNING-GEAR VARIANTS (R1 SHAPE LANGUAGE)",
        font=f_title,
        fill=(242, 227, 198),
    )
    draw.text(
        (28, 52),
        "Shared 1.48 m × 1.84 m R1 Pillowed Bolster Deck  |  1.22 m Track (rides in 07's wheel ruts)  |  Articulated Short Rigid A-Frame Coupler + Brass Knuckles",
        font=f_sub,
        fill=(185, 196, 218),
    )

    specs = [
        (
            "A",
            "VARIANT A — SIMPLE RUNNER SLED  [RECOMMENDED]",
            "Twin 0.22 m upturned steel ski runners + elastomeric shock blocks + rigid A-frame drawbar.",
            "PRO: Cleanest silhouette; zero moving parts to jam in dust; glides squarely inside 07's 1.22 m wheel ruts.",
            "CON: Higher sliding drag on bare basalt shelves; relies on ski tip curl to ride over sharp crater ledges.",
            (255, 181, 71),
        ),
        (
            "B",
            "VARIANT B — ROLLER SLED",
            "Upturned cream boat-hull pan + 4 wide Ø 0.27 m rubber/brass drum rollers on rocking bogie beams.",
            "PRO: Articulated roller bogies visibly bounce over pebbles and reduce towing resistance on hard rock.",
            "CON: Busier underside silhouette; small drum rollers visually compete with 07's iconic Ø 0.70 m tyres.",
            (120, 165, 130),
        ),
        (
            "C",
            "VARIANT C — TRACKED DOLLY",
            "Twin stadium-loop rubber crawler pods (rounded ends) + 360° geared brass turntable bolster.",
            "PRO: Highest terrain authority & grade capability; turntable bolster prevents jackknifing in tight turns.",
            "CON: Highest mechanical/visual weight; reads like an autonomous mini-crawler rather than a passive sled.",
            (130, 155, 205),
        ),
    ]

    col_w = 760
    gaps = [24, 820, 1616]
    for idx, (v_code, title_txt, desc_txt, pro_txt, con_txt, accent_rgb) in enumerate(specs):
        x0 = gaps[idx]
        # Column frame
        draw.rounded_rectangle((x0, 104, x0 + col_w, H - 20), radius=10, fill=(20, 25, 40), outline=accent_rgb, width=2)

        # Hero image (top)
        im_h = Image.open(hero_paths[v_code]).convert("RGB")
        sheet.paste(im_h, (x0, 104))
        _draw_badge(draw, (x0 + 16, 118), title_txt, f_hdr, border_rgb=accent_rgb)

        # Detail image (middle)
        im_d = Image.open(detail_paths[v_code]).convert("RGB")
        sheet.paste(im_d, (x0, 752))
        draw.line((x0, 750, x0 + col_w, 750), fill=accent_rgb, width=2)
        _draw_badge(
            draw,
            (x0 + 16, 766),
            f"Close-Up: Variant {v_code} Running Gear & Short Rigid Coupler",
            f_pro,
            border_rgb=accent_rgb,
        )

        # Bottom Pro/Con card (y = 1220 .. 1520)
        draw.line((x0, 1216, x0 + col_w, 1216), fill=accent_rgb, width=2)
        draw.text((x0 + 20, 1230), desc_txt, font=f_body, fill=(210, 218, 235))

        # One-line PRO box
        draw.rounded_rectangle((x0 + 16, 1272, x0 + col_w - 16, 1380), radius=6, fill=(22, 42, 34), outline=(120, 165, 130), width=2)
        # Wrap text cleanly across 2 lines inside the box
        words_p = pro_txt.split(" ")
        mid_p = len(words_p) // 2 + 1
        draw.text((x0 + 28, 1286), " ".join(words_p[:mid_p]), font=f_pro, fill=(168, 232, 182))
        draw.text((x0 + 28, 1316), " ".join(words_p[mid_p:]), font=f_pro, fill=(168, 232, 182))

        # One-line CON box
        draw.rounded_rectangle((x0 + 16, 1394, x0 + col_w - 16, 1502), radius=6, fill=(46, 26, 28), outline=(255, 138, 91), width=2)
        words_c = con_txt.split(" ")
        mid_c = len(words_c) // 2 + 1
        draw.text((x0 + 28, 1408), " ".join(words_c[:mid_c]), font=f_pro, fill=(255, 182, 152))
        draw.text((x0 + 28, 1438), " ".join(words_c[mid_c:]), font=f_pro, fill=(255, 182, 152))

        hero_paths[v_code].unlink(missing_ok=True)
        detail_paths[v_code].unlink(missing_ok=True)

    out_file = OUT_DIR / "sled_variants.png"
    sheet.save(out_file, format="PNG")
    print(f"[Caravan] Saved {out_file}")
    return out_file


def render_sheet3_vehicle_concepts(tmp_dir: Path) -> Path:
    """
    Build and render Sheet 3: `docs/art/concepts/caravan/vehicle_concepts.png`
    - 3 concepts for a dedicated vehicle 07 can drive/dock into for long range later:
      1. Concept 1: 'Nomad' Modular Carrier Cab (Seatruck-style Front-Dock Hauler, 3.25m W x 4.90m L x 2.38m H)
      2. Concept 2: 'Boreas' Tracked Crawler Base (Mobile Forward Base on Stadium Tracks, 3.90m W x 7.20m L x 2.68m H)
      3. Concept 3: 'Tereshkova' Linear-Induction Rail Sled (Trunk-Line Monorail/Duorail Ferry, 2.95m W x 5.80m L x 2.05m H)
    - Each row/column shows:
      * 3/4 Hero Docking View (with real 07 docked inside for unmistakable scale)
      * Orthographic Side Scale Profile (showing exact meter dimensions relative to 07's 1.59m height)
    """
    hero_imgs = {c: tmp_dir / f"_s3_hero_{c}.png" for c in ("C1", "C2", "C3")}
    side_imgs = {c: tmp_dir / f"_s3_side_{c}.png" for c in ("C1", "C2", "C3")}

    if not COMPOSITE_ONLY:
        print("[Caravan] Building Scene 3: 3 Dedicated Long-Range Vehicle Concepts...")
        builders = [
            (
                "C1",
                build_concept1_seatruck_carrier,
                (-4.20, 2.55, 4.45),
                (0.0, 0.95, 0.10),
                (-10.0, 1.15, -0.20),
                (0.0, 1.15, -0.20),
                6.2,
            ),
            (
                "C2",
                build_concept2_crawler_base,
                (-5.40, 3.20, -5.60),
                (0.0, 1.15, -0.45),
                (-12.0, 1.30, -0.60),
                (0.0, 1.30, -0.60),
                8.8,
            ),
            (
                "C3",
                build_concept3_rail_sled,
                (4.80, 2.55, 4.90),
                (0.20, 0.85, 0.0),
                (-12.0, 1.05, 0.0),
                (0.0, 1.05, 0.0),
                8.8,
            ),
        ]

        for code, fn_build, h_cam, h_tgt, s_cam, s_tgt, s_ortho in builders:
            r1kit.reset_scene()
            build_basin_floor_and_day_c_lighting(curve_fn=None, include_tracks=(code != "C3"))
            fn_build()
            fast_bake_vcol_hierarchy(bpy.data.objects)
            finalize_all_render_materials()

            setup_camera_and_render(
                cam_pos_u=h_cam,
                target_u=h_tgt,
                out_path=hero_imgs[code],
                res_x=760,
                res_y=640,
                fov_deg=44.0,
            )
            setup_camera_and_render(
                cam_pos_u=s_cam,
                target_u=s_tgt,
                out_path=side_imgs[code],
                res_x=760,
                res_y=440,
                ortho_scale=s_ortho,
            )
        return OUT_DIR / "vehicle_concepts.png"

    # --- Composite `vehicle_concepts.png` (2400 x 1560) ---
    W, H = 2400, 1560
    sheet = Image.new("RGB", (W, H), (14, 18, 30))
    draw = ImageDraw.Draw(sheet)

    f_title = _load_font(30, bold=True)
    f_sub = _load_font(18, bold=False)
    f_hdr = _load_font(21, bold=True)
    f_pro = _load_font(16, bold=True)
    f_body = _load_font(15, bold=False)

    draw.rectangle((0, 0, W, 86), fill=(20, 26, 42))
    draw.line((0, 86, W, 86), fill=(255, 138, 91), width=3)
    draw.text(
        (28, 14),
        "LONG-RANGE VEHICLE CONCEPTS — DEDICATED CARRIERS 07 DRIVES / DOCKS INTO (POST-0.6)",
        font=f_title,
        fill=(242, 227, 198),
    )
    draw.text(
        (28, 52),
        "Shared R1 Soft Pillowed Shape Language (Cream Enamel, Coral Belt, Sage Patch, 1 Amber Accent)  |  Shown with Canonical Rover 07 (1.47×2.01×1.59 m) Docked Inside",
        font=f_sub,
        fill=(185, 196, 218),
    )

    v_specs = [
        (
            "C1",
            "1. 'NOMAD' MODULAR CARRIER CAB  [RECOMMENDED]",
            "Scale: 3.25 m W × 4.90 m L × 2.38 m H  |  6× Ø 1.08 m Balloon Wheels",
            "DOCKING: 07 drives up rear belly ramp into the front horseshoe collar; 07's CRT head peers over the bow visor as the live pilot!",
            "ROLE: Seatruck-style long-range prime mover; carries 8 HV battery cells + side saddle bays + pulls the 3-car sled train.",
            (255, 181, 71),
        ),
        (
            "C2",
            "2. 'BOREAS' TRACKED CRAWLER BASE",
            "Scale: 3.90 m W × 7.20 m L × 2.68 m H  |  Quad Stadium Rubber Tracks",
            "DOCKING: Wide rear clamshell ramp drops to regolith; 07 drives into the central open charge-dock bay under the gantry arch.",
            "ROLE: Self-contained mobile forward base; portside submarine battery vault + starboard 3-crate rack + dorsal solar wings.",
            (120, 165, 130),
        ),
        (
            "C3",
            "3. 'TERESHKOVA' LINEAR-INDUCTION RAIL SLED",
            "Scale: 2.95 m W × 5.80 m L × 2.05 m H  |  1.36 m Twin-Tubular Rail Spine",
            "DOCKING: Side fold-down ramp loads 07 onto a 360° brass turntable pad; pinch-roller mag-bogies grip the copper stator rail.",
            "ROLE: High-speed zero-dust trunk-line ferry between established bases (Mare Imbrium <-> Kepler); player-laid rail segments.",
            (130, 155, 205),
        ),
    ]

    col_w = 760
    gaps = [24, 820, 1616]
    for idx, (code, title_txt, scale_txt, dock_txt, role_txt, accent_rgb) in enumerate(v_specs):
        x0 = gaps[idx]
        draw.rounded_rectangle((x0, 104, x0 + col_w, H - 20), radius=10, fill=(20, 25, 40), outline=accent_rgb, width=2)

        # Top Hero 3/4 view (760 x 640)
        im_h = Image.open(hero_imgs[code]).convert("RGB")
        sheet.paste(im_h, (x0, 104))
        _draw_badge(draw, (x0 + 16, 118), title_txt, f_hdr, border_rgb=accent_rgb)
        _draw_badge(draw, (x0 + 16, 692), scale_txt, f_pro, bg_rgb=(20, 26, 42), border_rgb=accent_rgb)

        # Middle Orthographic Side Scale Profile (760 x 440)
        im_s = Image.open(side_imgs[code]).convert("RGB")
        sheet.paste(im_s, (x0, 752))
        draw.line((x0, 750, x0 + col_w, 750), fill=accent_rgb, width=2)
        _draw_badge(
            draw,
            (x0 + 16, 766),
            "Orthographic Side Scale Profile (with 07 Docked at 1:1 Scale)",
            f_pro,
            border_rgb=accent_rgb,
        )

        # Bottom Purpose & Docking Card (y = 1200 .. 1540)
        draw.line((x0, 1196, x0 + col_w, 1196), fill=accent_rgb, width=2)

        # Docking box
        draw.rounded_rectangle((x0 + 16, 1212, x0 + col_w - 16, 1360), radius=6, fill=(24, 34, 52), outline=accent_rgb, width=2)
        w_d = dock_txt.split(" ")
        p1, p2 = len(w_d) // 3 + 1, (2 * len(w_d)) // 3 + 1
        draw.text((x0 + 28, 1226), " ".join(w_d[:p1]), font=f_pro, fill=(242, 227, 198))
        draw.text((x0 + 28, 1258), " ".join(w_d[p1:p2]), font=f_pro, fill=(242, 227, 198))
        draw.text((x0 + 28, 1290), " ".join(w_d[p2:]), font=f_pro, fill=(242, 227, 198))

        # Role box
        draw.rounded_rectangle((x0 + 16, 1374, x0 + col_w - 16, 1522), radius=6, fill=(22, 40, 34), outline=(120, 165, 130), width=2)
        w_r = role_txt.split(" ")
        q1, q2 = len(w_r) // 3 + 1, (2 * len(w_r)) // 3 + 1
        draw.text((x0 + 28, 1388), " ".join(w_r[:q1]), font=f_pro, fill=(175, 232, 188))
        draw.text((x0 + 28, 1420), " ".join(w_r[q1:q2]), font=f_pro, fill=(175, 232, 188))
        draw.text((x0 + 28, 1452), " ".join(w_r[q2:]), font=f_pro, fill=(175, 232, 188))

        hero_imgs[code].unlink(missing_ok=True)
        side_imgs[code].unlink(missing_ok=True)

    out_file = OUT_DIR / "vehicle_concepts.png"
    sheet.save(out_file, format="PNG")
    print(f"[Caravan] Saved {out_file}")
    return out_file


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    tmp_dir = REPO_ROOT / "Logs" / "_caravan_tmp"
    tmp_dir.mkdir(parents=True, exist_ok=True)

    render_sheet1_caravan(tmp_dir)
    render_sheet2_sled_variants(tmp_dir)
    render_sheet3_vehicle_concepts(tmp_dir)

    if not COMPOSITE_ONLY:
        print("[Caravan] Invoking system Python with Pillow to composite annotated sheets...")
        subprocess.check_call(["python", str(Path(__file__).resolve()), "--composite-only"])
    else:
        try:
            tmp_dir.rmdir()
        except OSError:
            pass
        print("[Caravan] All 3 concept sheets rendered and composited successfully.")


if __name__ == "__main__":
    main()
