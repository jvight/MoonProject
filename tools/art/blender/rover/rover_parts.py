"""Procedural BMesh builders for all nodes of RoverModel.fbx, SolarWing.fbx, and Kit_TapeDeck.fbx."""
import math
import bmesh
import bpy
from mathutils import Vector

from mesh_ops import (
    FLAT_UV_SCALE,
    FULL_CIRCLE_RAD,
    HALF_PI_RAD,
    UNIT_EPSILON,
    append_beveled_box,
    append_cylinder,
    append_torus,
    create_empty_node,
    create_mesh_node,
    ensure_outward_normals,
    unity_to_blender_pos,
)


def create_material_library(mat_defs, for_render=False):
    """Create the allowed R1_* Blender materials with principled PBR properties."""
    mats = {}
    for name, props in mat_defs.items():
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        nodes.clear()

        out_node = nodes.new(type="ShaderNodeOutputMaterial")
        bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
        links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])

        base_col = props["base_color"]
        bsdf.inputs["Base Color"].default_value = (base_col[0], base_col[1], base_col[2], 1.0)
        bsdf.inputs["Metallic"].default_value = props["metallic"]
        bsdf.inputs["Roughness"].default_value = props["roughness"]

        if name == "R1_Screen" and for_render:
            configure_procedural_crt_screen_shader(mat, props)
        elif props["emission_strength"] > 0.0:
            em_col = props["emission_color"]
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (em_col[0], em_col[1], em_col[2], 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = props["emission_strength"]

        mats[name] = mat
    return mats


def configure_procedural_crt_screen_shader(mat, props):
    """
    Configure a procedural UV shader for R1_Screen (used in renders only) so the CRT face glows warm
    amber edge-to-edge with horizontal scanlines and two distinct vertical pill eye shapes as in turnaround.png.
    """
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()

    out_node = nodes.new(type="ShaderNodeOutputMaterial")
    bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (0.14, 0.08, 0.03, 1.0)
    bsdf.inputs["Roughness"].default_value = props["roughness"]
    bsdf.inputs["Metallic"].default_value = 0.0
    links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])

    tex_coord = nodes.new(type="ShaderNodeTexCoord")
    sep_xyz = nodes.new(type="ShaderNodeSeparateXYZ")
    links.new(tex_coord.outputs["UV"], sep_xyz.inputs["Vector"])

    def make_eye_mask(u_center, v_center, half_w, half_h):
        du = nodes.new(type="ShaderNodeMath")
        du.operation = "SUBTRACT"
        links.new(sep_xyz.outputs["X"], du.inputs[0])
        du.inputs[1].default_value = u_center

        au = nodes.new(type="ShaderNodeMath")
        au.operation = "ABSOLUTE"
        links.new(du.outputs[0], au.inputs[0])

        nu = nodes.new(type="ShaderNodeMath")
        nu.operation = "DIVIDE"
        links.new(au.outputs[0], nu.inputs[0])
        nu.inputs[1].default_value = half_w

        dv = nodes.new(type="ShaderNodeMath")
        dv.operation = "SUBTRACT"
        links.new(sep_xyz.outputs["Y"], dv.inputs[0])
        dv.inputs[1].default_value = v_center

        av = nodes.new(type="ShaderNodeMath")
        av.operation = "ABSOLUTE"
        links.new(dv.outputs[0], av.inputs[0])

        nv = nodes.new(type="ShaderNodeMath")
        nv.operation = "DIVIDE"
        links.new(av.outputs[0], nv.inputs[0])
        nv.inputs[1].default_value = half_h

        p_u = nodes.new(type="ShaderNodeMath")
        p_u.operation = "POWER"
        links.new(nu.outputs[0], p_u.inputs[0])
        p_u.inputs[1].default_value = 2.6

        p_v = nodes.new(type="ShaderNodeMath")
        p_v.operation = "POWER"
        links.new(nv.outputs[0], p_v.inputs[0])
        p_v.inputs[1].default_value = 2.6

        dist_sum = nodes.new(type="ShaderNodeMath")
        dist_sum.operation = "ADD"
        links.new(p_u.outputs[0], dist_sum.inputs[0])
        links.new(p_v.outputs[0], dist_sum.inputs[1])

        ramp = nodes.new(type="ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "EASE"
        ramp.color_ramp.elements[0].position = 0.40
        ramp.color_ramp.elements[0].color = (1.0, 1.0, 1.0, 1.0)
        ramp.color_ramp.elements[1].position = 1.40
        ramp.color_ramp.elements[1].color = (0.0, 0.0, 0.0, 1.0)
        links.new(dist_sum.outputs[0], ramp.inputs["Fac"])
        return ramp.outputs["Color"]

    left_eye = make_eye_mask(0.35, 0.50, 0.068, 0.165)
    right_eye = make_eye_mask(0.65, 0.50, 0.068, 0.165)

    eyes_max = nodes.new(type="ShaderNodeMath")
    eyes_max.operation = "MAXIMUM"
    links.new(left_eye, eyes_max.inputs[0])
    links.new(right_eye, eyes_max.inputs[1])

    dx_c = nodes.new(type="ShaderNodeMath")
    dx_c.operation = "SUBTRACT"
    links.new(sep_xyz.outputs["X"], dx_c.inputs[0])
    dx_c.inputs[1].default_value = 0.5

    ax_c = nodes.new(type="ShaderNodeMath")
    ax_c.operation = "MULTIPLY"
    links.new(dx_c.outputs[0], ax_c.inputs[0])
    links.new(dx_c.outputs[0], ax_c.inputs[1])

    dy_c = nodes.new(type="ShaderNodeMath")
    dy_c.operation = "SUBTRACT"
    links.new(sep_xyz.outputs["Y"], dy_c.inputs[0])
    dy_c.inputs[1].default_value = 0.5

    ay_c = nodes.new(type="ShaderNodeMath")
    ay_c.operation = "MULTIPLY"
    links.new(dy_c.outputs[0], ay_c.inputs[0])
    links.new(dy_c.outputs[0], ay_c.inputs[1])

    rad_sq = nodes.new(type="ShaderNodeMath")
    rad_sq.operation = "ADD"
    links.new(ax_c.outputs[0], rad_sq.inputs[0])
    links.new(ay_c.outputs[0], rad_sq.inputs[1])

    vig_ramp = nodes.new(type="ShaderNodeValToRGB")
    vig_ramp.color_ramp.interpolation = "EASE"
    vig_ramp.color_ramp.elements[0].position = 0.06
    vig_ramp.color_ramp.elements[0].color = (0.78, 0.36, 0.06, 1.0)
    vig_ramp.color_ramp.elements[1].position = 0.46
    vig_ramp.color_ramp.elements[1].color = (0.38, 0.16, 0.02, 1.0)
    links.new(rad_sq.outputs[0], vig_ramp.inputs["Fac"])

    scan_mul = nodes.new(type="ShaderNodeMath")
    scan_mul.operation = "MULTIPLY"
    links.new(sep_xyz.outputs["Y"], scan_mul.inputs[0])
    scan_mul.inputs[1].default_value = 220.0

    scan_sin = nodes.new(type="ShaderNodeMath")
    scan_sin.operation = "SINE"
    links.new(scan_mul.outputs[0], scan_sin.inputs[0])

    scan_remap = nodes.new(type="ShaderNodeMapRange")
    scan_remap.inputs["From Min"].default_value = -1.0
    scan_remap.inputs["From Max"].default_value = 1.0
    scan_remap.inputs["To Min"].default_value = 0.86
    scan_remap.inputs["To Max"].default_value = 1.05
    links.new(scan_sin.outputs[0], scan_remap.inputs["Value"])

    mix_col = nodes.new(type="ShaderNodeMix")
    mix_col.data_type = "RGBA"
    links.new(eyes_max.outputs[0], mix_col.inputs["Factor"])
    links.new(vig_ramp.outputs["Color"], mix_col.inputs["A"])
    mix_col.inputs["B"].default_value = (1.0, 0.90, 0.45, 1.0)

    final_mul = nodes.new(type="ShaderNodeVectorMath")
    final_mul.operation = "SCALE"
    links.new(mix_col.outputs["Result"], final_mul.inputs["Vector"])
    links.new(scan_remap.outputs["Result"], final_mul.inputs["Scale"])

    links.new(final_mul.outputs["Vector"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(final_mul.outputs["Vector"], bsdf.inputs["Emission Color"])
    if "Emission Strength" in bsdf.inputs:
        bsdf.inputs["Emission Strength"].default_value = props["emission_strength"]


def _sample_rounded_rect_ring(hx, z_min, z_max, corner_r):
    """
    Generate a 32-point counter-clockwise horizontal perimeter ring in Unity (ux, uz)
    with 4 rounded corners (5 segments each) and 3 segments along each straight side so
    flat flanks have coplanar interior vertices.
    """
    rc = min(corner_r, hx * 0.45, (z_max - z_min) * 0.22)
    x_in = hx - rc
    zf_in = z_max - rc
    zr_in = z_min + rc

    pts = []

    def add_line_pts(p0, p1, segs=2):
        for s in range(1, segs):
            t = s / segs
            pts.append((p0[0] + t * (p1[0] - p0[0]), p0[1] + t * (p1[1] - p0[1])))

    def add_arc_pts(cx, cz, a0, a1, segs=4):
        for s in range(segs + 1):
            a = a0 + (a1 - a0) * (s / segs)
            pts.append((cx + rc * math.cos(a), cz + rc * math.sin(a)))

    # 1. Front-Right corner (0 -> pi/2)
    add_arc_pts(x_in, zf_in, 0.0, HALF_PI_RAD)
    # Front straight edge (+x_in -> -x_in at z = z_max)
    add_line_pts((x_in, z_max), (-x_in, z_max))
    # 2. Front-Left corner (pi/2 -> pi)
    add_arc_pts(-x_in, zf_in, HALF_PI_RAD, math.pi)
    # Left straight edge (z = zf_in -> zr_in at x = -hx)
    add_line_pts((-hx, zf_in), (-hx, zr_in))
    # 3. Rear-Left corner (pi -> 3*pi/2)
    add_arc_pts(-x_in, zr_in, math.pi, 1.5 * math.pi)
    # Rear straight edge (-x_in -> +x_in at z = z_min)
    add_line_pts((-x_in, z_min), (x_in, z_min))
    # 4. Rear-Right corner (3*pi/2 -> 2*pi)
    add_arc_pts(x_in, zr_in, 1.5 * math.pi, FULL_CIRCLE_RAD)
    # Right straight edge (z = zr_in -> zf_in at x = +hx)
    add_line_pts((hx, zr_in), (hx, zf_in))

    return pts


def _append_pillowed_body_tub(bm, cfg):
    """
    Build the main rounded-cream + orange-beltline Body tub as a pure-quad horizontal-ring loft.
    Eliminates bevel-clamp overlaps and bisect_plane triangles so smooth shading is 100% pinch-free
    and the horizontal beltline (y = 0.56..0.65 m) has razor-sharp horizontal borders.
    """
    temp = bmesh.new()
    half_w = cfg["body_tub_width"] * FLAT_UV_SCALE
    half_l = cfg["body_tub_length"] * FLAT_UV_SCALE
    z_center = cfg["body_tub_z_center"]
    z_min_base = z_center - half_l
    z_max_base = z_center + half_l
    corner_r = cfg["body_corner_bevel"]
    belt_bot = cfg["body_belt_y_bottom"]
    belt_top = cfg["body_belt_y_top"]

    # (uy, inset_x, inset_z_rear, inset_z_front)
    ring_specs = (
        (0.435, 0.140, 0.140, 0.140),  # 0: bottom inner flat ring
        (0.435, 0.036, 0.036, 0.036),  # 1: bottom flat guard ring
        (0.448, 0.012, 0.012, 0.012),  # 2: bottom bevel
        (0.468, 0.000, 0.000, 0.000),  # 3: lower flank bottom guard
        (0.545, 0.000, 0.000, 0.000),  # 4: lower flank top guard
        (belt_bot, 0.000, 0.000, 0.000),      # 5: exact bottom of orange beltline (0.560 m)
        (0.574, -0.002, -0.002, -0.002),      # 6: beltline lower guard
        (0.636, -0.002, -0.002, -0.002),      # 7: beltline upper guard
        (belt_top, 0.000, 0.000, 0.000),      # 8: exact top of orange beltline (0.650 m)
        (0.665, 0.000, 0.000, 0.004),  # 9: upper flank bottom guard
        (0.770, 0.006, 0.006, 0.065),  # 10: upper flank top guard + front hood glacis start
        (0.822, 0.022, 0.022, 0.118),  # 11: pillowed shoulder bevel seg 1
        (0.865, 0.048, 0.048, 0.168),  # 12: pillowed shoulder bevel seg 2
        (0.894, 0.082, 0.080, 0.212),  # 13: pillowed shoulder bevel seg 3
        (0.908, 0.118, 0.115, 0.248),  # 14: pillowed shoulder bevel seg 4
        (0.910, 0.142, 0.140, 0.272),  # 15: top deck flat guard ring
        (0.910, 0.260, 0.260, 0.380),  # 16: top deck inner flat ring
    )

    ring_verts = []
    for r_idx, (uy, in_x, in_zr, in_zf) in enumerate(ring_specs):
        hx = max(0.08, half_w - in_x)
        z_min = z_min_base + in_zr
        z_max = z_max_base - in_zf
        pts_2d = _sample_rounded_rect_ring(hx, z_min, z_max, corner_r)
        v_row = []
        for ux, uz in pts_2d:
            adj_y = uy
            # Gentle wheel-arch relief on the lower side skirt below the beltline guard
            if r_idx in (1, 2, 3) and abs(ux) > half_w * 0.72:
                for axle_z in (-cfg["wheel_base"], cfg["wheel_base"]):
                    dz = abs(uz - axle_z)
                    if dz < cfg["wheel_arch_radius"]:
                        lift = 0.042 * math.sqrt(max(0.0, 1.0 - (dz / cfg["wheel_arch_radius"]) ** 2))
                        adj_y = min(0.532, max(adj_y, 0.435 + lift))
            v_row.append(temp.verts.new(unity_to_blender_pos(ux, adj_y, uz)))
        ring_verts.append(v_row)

    n_perim = len(ring_verts[0])
    # Connect quad strips between consecutive rings
    for r_idx in range(len(ring_specs) - 1):
        r0 = ring_verts[r_idx]
        r1 = ring_verts[r_idx + 1]
        is_belt_band = 5 <= r_idx <= 7
        is_bottom_band = r_idx <= 1
        mat_idx = 1 if is_belt_band else (2 if is_bottom_band else 0)
        for i in range(n_perim):
            ni = (i + 1) % n_perim
            f = temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
            f.material_index = mat_idx
            f.smooth = True

    # Cap bottom inner ring (Ring 0) and top inner ring (Ring 16) around central vertices (which lie inside flat guard rings)
    bot_center = temp.verts.new(unity_to_blender_pos(0.0, 0.435, z_center))
    top_center = temp.verts.new(unity_to_blender_pos(0.0, 0.910, z_center - 0.05))
    for i in range(n_perim):
        ni = (i + 1) % n_perim
        fb = temp.faces.new((ring_verts[0][ni], ring_verts[0][i], bot_center))
        fb.material_index = 2
        fb.smooth = True
        ft = temp.faces.new((ring_verts[-1][i], ring_verts[-1][ni], top_center))
        ft.material_index = 0
        ft.smooth = True

    ensure_outward_normals(temp)
    vert_map = {v: bm.verts.new(v.co) for v in temp.verts}
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = f.material_index
        nf.smooth = True
    temp.free()


def build_contoured_body_mesh(cfg, mats):
    """
    Build the soft, pillowed, compact buggy Body hull matching turnaround.png:
      - Material 0: R1_CreamEnamel (main rounded tub & central nose boss)
      - Material 1: R1_OrangeStripe (single continuous horizontal beltline y = 0.56..0.65 m)
      - Material 2: R1_GreyMetal (front/rear bumper beams, belly pan, arched wheel-well lips, solar rails)
      - Material 3: R1_SagePatch (riveted vertical sage-green repair plate on left flank)
      - Material 4: R1_GoldFoil (quilted thermal blanket pad between the solar mounting rails)
      - Material 5: R1_BareSteel (rivets and rail tie-downs)
      - Material 6: R1_Copper (solar rail end-clamps & shoulder trim)
      - Material 7: R1_DarkGlass (recessed upper-rear solar mounting bay liner)
    """
    bm = bmesh.new()
    mat_list = [
        mats["R1_CreamEnamel"],
        mats["R1_OrangeStripe"],
        mats["R1_GreyMetal"],
        mats["R1_SagePatch"],
        mats["R1_GoldFoil"],
        mats["R1_BareSteel"],
        mats["R1_Copper"],
        mats["R1_DarkGlass"],
    ]

    # 1. Pure-quad pillowed cream + orange tub loft
    _append_pillowed_body_tub(bm, cfg)

    # 2. Central raised rounded cream nose boss framing the chest CutterTurret (as in turnaround.png & callouts.png)
    append_beveled_box(
        bm,
        size_xyz=(0.28, 0.22, 0.26),
        center_xyz=(0.0, 0.67, 0.60),
        bevel_width=0.052,
        bevel_segments=3,
        mat_idx=0,
        taper_top_xy=(0.82, 0.85),
    )

    # 3. Full-width dark-charcoal front & rear bumper beams and lower chassis pan
    append_beveled_box(
        bm,
        size_xyz=(cfg["bumper_width"], cfg["bumper_height"], cfg["bumper_depth"]),
        center_xyz=(0.0, cfg["bumper_y_center"], cfg["front_bumper_z"]),
        bevel_width=cfg["bumper_bevel"],
        bevel_segments=2,
        mat_idx=2,
    )
    append_beveled_box(
        bm,
        size_xyz=(cfg["bumper_width"], cfg["bumper_height"], cfg["bumper_depth"]),
        center_xyz=(0.0, cfg["bumper_y_center"], cfg["rear_bumper_z"]),
        bevel_width=cfg["bumper_bevel"],
        bevel_segments=2,
        mat_idx=2,
    )
    append_beveled_box(
        bm,
        size_xyz=(cfg["belly_pan_width"], cfg["belly_pan_height"], cfg["belly_pan_length"]),
        center_xyz=(0.0, cfg["belly_pan_y"], 0.0),
        bevel_width=0.018,
        bevel_segments=2,
        mat_idx=2,
    )

    # 4. Arched dark-charcoal fender flare lips over all 4 wheel wells (meeting the tyres cleanly)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            append_beveled_box(
                bm,
                size_xyz=(cfg["fender_lip_width"], cfg["fender_lip_height"], cfg["fender_lip_length"]),
                center_xyz=(sx * cfg["fender_lip_x"], cfg["fender_lip_y"], sz * cfg["fender_lip_z"]),
                bevel_width=0.016,
                bevel_segments=2,
                mat_idx=2,
            )

    # 5. Riveted vertical Sage-Green field-repair patch (SagePatch_RL) on the left flank
    patch_x = -cfg["sage_patch_x"]
    append_beveled_box(
        bm,
        size_xyz=(cfg["sage_patch_thickness"], cfg["sage_patch_height"], cfg["sage_patch_length"]),
        center_xyz=(patch_x, cfg["sage_patch_y"], cfg["sage_patch_z"]),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=3,
    )
    for ry in (-1.0, 0.0, 1.0):
        for rz in (-1.0, 1.0):
            append_cylinder(
                bm,
                radius1=cfg["rivet_radius"],
                radius2=cfg["rivet_radius"],
                length=0.012,
                axis="X",
                center_xyz=(
                    patch_x - 0.006,
                    cfg["sage_patch_y"] + ry * (cfg["sage_patch_height"] * 0.38),
                    cfg["sage_patch_z"] + rz * (cfg["sage_patch_length"] * 0.40),
                ),
                segments=8,
                mat_idx=5,
            )

    # 6. Empty Solar Mounting Rails + Dark Recessed Bay + Gold-Foil pad on the upper-rear deck
    slope_rad = math.radians(cfg["solar_deck_slope_deg"])
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(cfg["solar_rail_width"], cfg["solar_rail_height"], cfg["solar_rail_length"]),
            center_xyz=(sx * cfg["solar_rail_x"], cfg["solar_rail_y"], cfg["solar_rail_z"]),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,
            rot_euler=(slope_rad, 0.0, 0.0),
        )
        append_beveled_box(
            bm,
            size_xyz=(cfg["solar_rail_width"] * 1.05, 0.010, cfg["solar_rail_length"] * 0.96),
            center_xyz=(sx * cfg["solar_rail_x"], cfg["solar_rail_y"] + 0.012, cfg["solar_rail_z"]),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=6,
            rot_euler=(slope_rad, 0.0, 0.0),
        )

    # Dark recessed bay liner + central gold-foil thermal pad between the empty rails
    append_beveled_box(
        bm,
        size_xyz=(cfg["solar_pad_width"], 0.014, cfg["solar_pad_length"]),
        center_xyz=(0.0, cfg["solar_rail_y"] - 0.004, cfg["solar_rail_z"]),
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=7,
        rot_euler=(slope_rad, 0.0, 0.0),
    )
    append_beveled_box(
        bm,
        size_xyz=(cfg["solar_pad_width"] * 0.56, 0.016, cfg["solar_pad_length"] * 0.56),
        center_xyz=(0.0, cfg["solar_rail_y"] - 0.002, cfg["solar_rail_z"]),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=4,
        rot_euler=(slope_rad, 0.0, 0.0),
    )

    return create_mesh_node("Body", bm, (0.0, 0.0, 0.0), mat_list=mat_list, cfg=cfg)


def build_head_assembly(rover_root, cfg, mats):
    """
    Build the hero 80%-width pillowed CRT Head assembly (Neck -> NeckLift -> Head -> Screen, Visor,
    RearVentStrip, Eye + sockets, DeckSocket, DeckCover) matching turnaround.png.
    """
    # 1. Neck (base collar ring at (0, 0.92, +0.24))
    bm_neck = bmesh.new()
    append_cylinder(
        bm_neck,
        radius1=cfg["neck_base_flange_radius"],
        radius2=cfg["neck_base_flange_radius"] * 0.92,
        length=0.045,
        axis="Y",
        center_xyz=(0.0, 0.005, 0.0),
        segments=24,
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=0,
    )
    append_torus(
        bm_neck,
        major_radius=cfg["neck_bellows_radius"],
        minor_radius=0.022,
        axis="Y",
        center_xyz=(0.0, 0.035, 0.0),
        major_seg=24,
        minor_seg=8,
        mat_idx=1,
    )
    append_torus(
        bm_neck,
        major_radius=cfg["neck_bellows_radius"] * 0.96,
        minor_radius=0.020,
        axis="Y",
        center_xyz=(0.0, 0.068, 0.0),
        major_seg=24,
        minor_seg=8,
        mat_idx=1,
    )
    neck_obj = create_mesh_node(
        "Neck",
        bm_neck,
        cfg["piv_neck"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Rubber"]],
        cfg=cfg,
    )

    # 2. NeckLift (telescopic inner column at (0, 0, 0) local to Neck)
    bm_lift = bmesh.new()
    append_cylinder(
        bm_lift,
        radius1=cfg["neck_bellows_radius"] * 0.80,
        radius2=cfg["neck_bellows_radius"] * 0.80,
        length=0.16,
        axis="Y",
        center_xyz=(0.0, 0.11, 0.0),
        segments=20,
        bevel_width=0.006,
        bevel_segments=1,
        mat_idx=0,
    )
    neck_lift_obj = create_mesh_node(
        "NeckLift",
        bm_lift,
        (0.0, 0.0, 0.0),
        parent=neck_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    # 3. Head (pillowed cream CRT housing at (0, 0.26, 0) local to NeckLift -> (0, 1.18, +0.24) world)
    bm_head = bmesh.new()
    head_w = cfg["head_cream_width"]
    head_h = cfg["head_cream_height"]
    head_d = cfg["head_cream_depth"]
    head_cy = cfg["head_local_center_y"]
    head_cz = cfg["head_local_center_z"]

    # Main pillowed cream CRT monitor shell (front face stays behind z = +0.24 m local so Screen is 100% clear)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w, head_h, head_d),
        center_xyz=(0.0, head_cy, head_cz),
        bevel_width=cfg["head_corner_bevel"],
        bevel_segments=cfg["head_bevel_segments"],
        mat_idx=0,
        taper_front_xz=(1.03, 1.02),
    )
    # Softly stepped rear cathode-gun housing pillow on the back of Head (rear face at z = -0.24 m local)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w * 0.82, head_h * 0.76, 0.08),
        center_xyz=(0.0, head_cy, head_cz - head_d * FLAT_UV_SCALE - 0.015),
        bevel_width=0.028,
        bevel_segments=3,
        mat_idx=0,
    )
    # Dark-charcoal inner screen surround gasket frame recessed behind Screen
    screen_front_z = cfg["piv_screen_local"][2]
    bezel_frame_w = cfg["screen_width"] + 0.036
    bezel_frame_h = cfg["screen_height"] + 0.036
    append_beveled_box(
        bm_head,
        size_xyz=(bezel_frame_w, bezel_frame_h, 0.025),
        center_xyz=(0.0, head_cy, screen_front_z - 0.012),
        bevel_width=0.012,
        bevel_segments=2,
        mat_idx=1,
    )
    head_obj = create_mesh_node(
        "Head",
        bm_head,
        cfg["piv_head_local"],
        parent=neck_lift_obj,
        mat_list=[mats["R1_CreamEnamel"], mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    # 4. Screen (single dedicated curved 4:3 CRT glass mesh with 0..1 UVs and R1_Screen material)
    bm_scr = build_crt_screen_bmesh(cfg)
    create_mesh_node(
        "Screen",
        bm_scr,
        cfg["piv_screen_local"],
        parent=head_obj,
        mat_list=[mats["R1_Screen"]],
        custom_uv=True,
        cfg=cfg,
    )

    # 5. Visor (seamless inverted-U dark-charcoal wrap-around anti-glare hood spanning 0.80 m width)
    bm_visor = build_wrap_visor_bmesh(cfg)
    create_mesh_node(
        "Visor",
        bm_visor,
        cfg["piv_visor_local"],
        parent=head_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    # 6. RearVentStrip (0.46 m W x 0.25 m H 5-bar glowing amber louver grille on the back of Head)
    bm_vent = bmesh.new()
    vw = cfg["rear_vent_width"]
    vh = cfg["rear_vent_height"]
    append_beveled_box(
        bm_vent,
        size_xyz=(vw, vh, 0.026),
        center_xyz=(0.0, 0.0, 0.0),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=0,
    )
    bar_count = cfg["rear_vent_bar_count"]
    bar_span_y = vh * 0.74
    for i in range(bar_count):
        by = -bar_span_y * FLAT_UV_SCALE + (bar_span_y * i / max(1, bar_count - 1))
        append_beveled_box(
            bm_vent,
            size_xyz=(vw * 0.88, vh / (bar_count * 2.0), 0.022),
            center_xyz=(0.0, by, -0.008),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=1,
        )
    create_mesh_node(
        "RearVentStrip",
        bm_vent,
        cfg["piv_rear_vent_local"],
        parent=head_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_LampAmber"]],
        cfg=cfg,
    )

    # 7. Eye (Left-cheek turned brass & charcoal optical scan lens pod, outer diameter = 0.24 m)
    bm_eye = bmesh.new()
    eye_r = cfg["eye_outer_radius"]
    append_cylinder(
        bm_eye,
        radius1=eye_r,
        radius2=eye_r,
        length=cfg["eye_pod_width"],
        axis="X",
        center_xyz=(0.0, 0.0, 0.0),
        segments=24,
        bevel_width=0.012,
        bevel_segments=2,
        mat_idx=0,
    )
    append_cylinder(
        bm_eye,
        radius1=eye_r * 0.84,
        radius2=eye_r * 0.84,
        length=cfg["eye_pod_width"] + 0.018,
        axis="X",
        center_xyz=(-0.006, 0.0, 0.0),
        segments=24,
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=1,
    )
    append_cylinder(
        bm_eye,
        radius1=eye_r * 0.46,
        radius2=eye_r * 0.40,
        length=0.048,
        axis="Z",
        center_xyz=(-0.01, 0.0, 0.042),
        segments=20,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,
    )
    eye_obj = create_mesh_node(
        "Eye",
        bm_eye,
        cfg["piv_eye_local"],
        parent=head_obj,
        mat_list=[mats["R1_Copper"], mats["R1_GreyMetal"], mats["R1_DarkGlass"]],
        cfg=cfg,
    )

    create_empty_node("EyeOrigin", cfg["piv_eye_origin_local"], parent=eye_obj)
    create_empty_node("LensSocket", cfg["piv_eye_origin_local"], parent=eye_obj)
    create_empty_node("ScanFanSocket", cfg["piv_eye_origin_local"], parent=eye_obj)

    bm_lock = bmesh.new()
    append_torus(
        bm_lock,
        major_radius=eye_r * 0.48,
        minor_radius=0.008,
        axis="Z",
        center_xyz=(-0.01, 0.0, 0.0),
        major_seg=20,
        minor_seg=8,
        mat_idx=0,
    )
    lock_ring_obj = create_mesh_node(
        "LockRing",
        bm_lock,
        cfg["piv_lock_ring_local"],
        parent=eye_obj,
        mat_list=[mats["R1_Copper"]],
        cfg=cfg,
    )

    bm_lglow = bmesh.new()
    append_torus(
        bm_lglow,
        major_radius=eye_r * 0.42,
        minor_radius=0.005,
        axis="Z",
        center_xyz=(-0.01, 0.0, 0.004),
        major_seg=20,
        minor_seg=6,
        mat_idx=0,
    )
    create_mesh_node(
        "LockGlow",
        bm_lglow,
        (0.0, 0.0, 0.0),
        parent=lock_ring_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    # 8. DeckSocket & DeckCover on the right cheek of Head
    create_empty_node("DeckSocket", cfg["piv_deck_socket_local"], parent=head_obj)
    bm_dcov = bmesh.new()
    append_beveled_box(
        bm_dcov,
        size_xyz=(0.022, 0.15, 0.16),
        center_xyz=(0.0, 0.0, 0.0),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,
    )
    create_mesh_node(
        "DeckCover",
        bm_dcov,
        cfg["piv_deck_socket_local"],
        parent=head_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )
    return neck_obj, head_obj


def build_crt_screen_bmesh(cfg):
    """Build the curved 4:3 Screen mesh with clean [0, 1]^2 UVs and gentle convex glass crown."""
    bm = bmesh.new()
    nx, ny = cfg["screen_grid_x"], cfg["screen_grid_y"]
    sw, sh = cfg["screen_width"], cfg["screen_height"]
    bulge = cfg["screen_bulge_z"]
    corner_round = cfg["screen_corner_round"]

    verts_grid = []
    for j in range(ny + 1):
        v_norm = j / ny
        yn = v_norm * 2.0 - 1.0
        row = []
        for i in range(nx + 1):
            u_norm = i / nx
            xn = u_norm * 2.0 - 1.0
            scale_corner = 1.0 - corner_round * (xn * xn) * (yn * yn)
            ux = (xn * scale_corner) * (sw * FLAT_UV_SCALE)
            uy = (yn * scale_corner) * (sh * FLAT_UV_SCALE)
            uz = bulge * (1.0 - 0.65 * xn * xn) * (1.0 - 0.65 * yn * yn)
            vert = bm.verts.new(unity_to_blender_pos(ux, uy, uz))
            row.append((vert, u_norm, v_norm))
        verts_grid.append(row)

    uv_layer = bm.loops.layers.uv.verify()
    for j in range(ny):
        for i in range(nx):
            v00, u00, w00 = verts_grid[j][i]
            v10, u10, w10 = verts_grid[j][i + 1]
            v11, u11, w11 = verts_grid[j + 1][i + 1]
            v01, u01, w01 = verts_grid[j + 1][i]
            face = bm.faces.new((v00, v01, v11, v10))
            face.material_index = 0
            face.smooth = True
            uvs = ((u00, w00), (u01, w01), (u11, w11), (u10, w10))
            for loop, uv in zip(face.loops, uvs):
                loop[uv_layer].uv = uv
    bm.normal_update()
    return bm


def build_wrap_visor_bmesh(cfg):
    """
    Build the dark-charcoal Visor as a single seamless inverted-U arch hood wrapping the top
    and sides of the CRT head (0.80 m outer width, no corner seam overlaps).
    """
    bm = bmesh.new()
    outer_w = cfg["head_visor_width"] * FLAT_UV_SCALE
    inner_w = outer_w - cfg["head_visor_thickness"]
    top_outer_y = cfg["head_visor_height"] * FLAT_UV_SCALE + cfg["head_visor_thickness"] * 0.45
    top_inner_y = top_outer_y - cfg["head_visor_thickness"]
    bot_y = -cfg["head_visor_height"] * FLAT_UV_SCALE + 0.02
    z_front = 0.03
    z_back = -cfg["head_visor_depth"] + 0.03

    temp = bmesh.new()
    pts_2d = [
        (-outer_w, bot_y),
        (-outer_w, top_outer_y),
        (outer_w, top_outer_y),
        (outer_w, bot_y),
        (inner_w, bot_y),
        (inner_w, top_inner_y),
        (-inner_w, top_inner_y),
        (-inner_w, bot_y),
    ]
    z_rings = (z_back, z_front)
    ring_verts = []
    for rz in z_rings:
        r_list = []
        for px, py in pts_2d:
            cheek_in = -0.015 if py < 0.0 else 0.0
            adj_x = px + math.copysign(cheek_in, px)
            adj_z = rz + (0.025 if (rz > 0.0 and py > 0.0) else 0.0)
            r_list.append(temp.verts.new(unity_to_blender_pos(adj_x, py, adj_z)))
        ring_verts.append(r_list)

    u_quads = ((0, 1, 6, 7), (1, 2, 5, 6), (2, 3, 4, 5))
    for a, b, c, d in u_quads:
        f_back = temp.faces.new((ring_verts[0][d], ring_verts[0][c], ring_verts[0][b], ring_verts[0][a]))
        f_back.smooth = True
        f_front = temp.faces.new((ring_verts[-1][a], ring_verts[-1][b], ring_verts[-1][c], ring_verts[-1][d]))
        f_front.smooth = True

    perim_pairs = ((0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 0))
    for r_i in range(len(z_rings) - 1):
        r0 = ring_verts[r_i]
        r1 = ring_verts[r_i + 1]
        for i0, i1 in perim_pairs:
            f_side = temp.faces.new((r0[i0], r0[i1], r1[i1], r1[i0]))
            f_side.smooth = True

    ensure_outward_normals(temp)
    sharp = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.35]
    if sharp:
        bmesh.ops.bevel(
            temp,
            geom=sharp,
            offset=0.012,
            offset_type="OFFSET",
            segments=3,
            profile=0.5,
            affect="EDGES",
            clamp_overlap=True,
        )

    ensure_outward_normals(temp)
    vert_map = {v: bm.verts.new(v.co) for v in temp.verts}
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = 0
        nf.smooth = True
    temp.free()
    return bm


def build_balloon_wheel(name, unity_loc, is_mismatched_rl, rover_root, cfg, mats):
    """
    Build a seamless, single-surface plump balloon tyre where the 14 chevron tread bars are
    sculpted directly into the toroidal mesh (zero intersecting boxes, ~2,000 tris per wheel).
    """
    bm = bmesh.new()
    side_sign = 1.0 if unity_loc[0] >= 0.0 else -1.0
    tyre_w = cfg["wheel_tyre_width"]
    outboard_ext = cfg["wheel_outer_half_span"] - abs(unity_loc[0])
    inboard_ext = tyre_w - outboard_ext
    local_center_x = side_sign * (outboard_ext - inboard_ext) * FLAT_UV_SCALE

    base_r = cfg["wheel_radius"] - cfg["wheel_lug_height"]
    lug_h = cfg["wheel_lug_height"]
    rim_r = cfg["wheel_rim_radius"]
    mid_r = FLAT_UV_SCALE * (base_r + rim_r)
    minor_r = (base_r - rim_r) * FLAT_UV_SCALE
    half_w = tyre_w * FLAT_UV_SCALE

    num_lugs = cfg["wheel_lug_count"]
    rings_per_lug = cfg["wheel_rings_per_lug"]
    n_theta = num_lugs * rings_per_lug
    n_phi = cfg["wheel_profile_segments"]
    chevron_skew = math.radians(cfg["wheel_chevron_deg"]) * 0.22

    # Crisp flat-topped plateau waveform across each lug pitch: [valley, top, top, valley]
    lug_wave_table = (0.0, 0.96, 0.96, 0.0) if rings_per_lug == 4 else (0.0, 0.45, 1.0, 1.0, 0.45, 0.0)

    temp_tyre = bmesh.new()
    tyre_verts = []
    for i in range(n_theta):
        sub_i = i % rings_per_lug
        lug_w = lug_wave_table[sub_i]
        base_theta = FULL_CIRCLE_RAD * i / n_theta
        row = []
        for j in range(n_phi):
            phi = FULL_CIRCLE_RAD * j / n_phi
            cp, sp = math.cos(phi), math.sin(phi)
            ux_off = half_w * math.copysign(abs(sp) ** 0.82, sp)
            ux = local_center_x + ux_off

            tread_mask = max(0.0, min(1.0, (cp - 0.15) / 0.55)) ** 1.4
            center_groove = min(1.0, (abs(sp) / 0.14) ** 2)
            shoulder_fade = max(0.0, min(1.0, (0.92 - abs(sp)) / 0.25))
            lug_relief = lug_h * lug_w * tread_mask * center_groove * shoulder_fade

            theta = base_theta + chevron_skew * (1.0 - abs(sp)) * tread_mask
            r = mid_r + minor_r * cp + lug_relief
            uy = r * math.sin(theta)
            uz = r * math.cos(theta)
            row.append(temp_tyre.verts.new(unity_to_blender_pos(ux, uy, uz)))
        tyre_verts.append(row)

    for i in range(n_theta):
        ni = (i + 1) % n_theta
        for j in range(n_phi):
            nj = (j + 1) % n_phi
            f = temp_tyre.faces.new((tyre_verts[i][j], tyre_verts[ni][j], tyre_verts[ni][nj], tyre_verts[i][nj]))
            f.smooth = True

    ensure_outward_normals(temp_tyre)
    t_map = {v: bm.verts.new(v.co) for v in temp_tyre.verts}
    for f in temp_tyre.faces:
        nf = bm.faces.new([t_map[v] for v in f.verts])
        nf.material_index = 0
        nf.smooth = True
    temp_tyre.free()

    # Wheel hub dish
    append_cylinder(
        bm,
        radius1=rim_r * 1.02,
        radius2=rim_r * 1.02,
        length=tyre_w * 0.72,
        axis="X",
        center_xyz=(local_center_x, 0.0, 0.0),
        segments=20,
        bevel_width=0.010,
        bevel_segments=1,
        mat_idx=1,
    )
    outer_hub_x = local_center_x + side_sign * (tyre_w * 0.35)
    append_torus(
        bm,
        major_radius=rim_r * 0.86,
        minor_radius=0.026,
        axis="X",
        center_xyz=(outer_hub_x, 0.0, 0.0),
        major_seg=22,
        minor_seg=8,
        mat_idx=1 if is_mismatched_rl else 2,
    )

    if is_mismatched_rl:
        # Kenji's rear-left replacement wheel: charcoal dish + central steel cap + 4 bold orange cross-bars
        for diag_angle in (math.pi * 0.25, -math.pi * 0.25):
            append_beveled_box(
                bm,
                size_xyz=(0.024, 0.030, rim_r * 1.62),
                center_xyz=(outer_hub_x + side_sign * 0.010, 0.0, 0.0),
                bevel_width=0.005,
                bevel_segments=1,
                mat_idx=4,
                rot_euler=(diag_angle, 0.0, 0.0),
            )
        append_cylinder(
            bm,
            radius1=rim_r * 0.28,
            radius2=rim_r * 0.25,
            length=0.042,
            axis="X",
            center_xyz=(outer_hub_x + side_sign * 0.012, 0.0, 0.0),
            segments=16,
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=3,
        )
    else:
        append_torus(
            bm,
            major_radius=rim_r * 0.56,
            minor_radius=0.022,
            axis="X",
            center_xyz=(outer_hub_x + side_sign * 0.006, 0.0, 0.0),
            major_seg=20,
            minor_seg=8,
            mat_idx=2,
        )
        append_cylinder(
            bm,
            radius1=rim_r * 0.34,
            radius2=rim_r * 0.30,
            length=0.038,
            axis="X",
            center_xyz=(outer_hub_x + side_sign * 0.008, 0.0, 0.0),
            segments=16,
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=3,
        )

    mat_list = [
        mats["R1_Rubber"],
        mats["R1_GreyMetal"],
        mats["R1_CreamEnamel"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
    ]
    return create_mesh_node(name, bm, unity_loc, parent=rover_root, mat_list=mat_list, cfg=cfg)


def build_front_tools_and_lamps(rover_root, body_obj, cfg, mats):
    """Build Headlamp_L/R, FrontIntake, CutterTurret + CutterHead, and DigScoop visible below the nose."""
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        piv = (sx * cfg["piv_headlamp_x"], cfg["piv_headlamp_y"], cfg["piv_headlamp_z"])
        bm_hl = bmesh.new()
        lamp_r = cfg["headlamp_outer_radius"]
        y_off = cfg["headlamp_visual_y_offset"]
        append_cylinder(
            bm_hl,
            radius1=lamp_r,
            radius2=lamp_r * 0.96,
            length=0.14,
            axis="Z",
            center_xyz=(0.0, y_off, -0.015),
            segments=22,
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=1,
        )
        append_torus(
            bm_hl,
            major_radius=lamp_r * 0.90,
            minor_radius=0.018,
            axis="Z",
            center_xyz=(0.0, y_off, 0.052),
            major_seg=22,
            minor_seg=8,
            mat_idx=0,
        )
        hl_obj = create_mesh_node(
            f"Headlamp_{side_label}",
            bm_hl,
            piv,
            parent=body_obj,
            mat_list=[mats["R1_Copper"], mats["R1_GreyMetal"]],
            cfg=cfg,
        )

        bm_gl = bmesh.new()
        append_cylinder(
            bm_gl,
            radius1=lamp_r * 0.78,
            radius2=lamp_r * 0.66,
            length=0.034,
            axis="Z",
            center_xyz=(0.0, y_off, 0.036),
            segments=22,
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=0,
        )
        create_mesh_node(
            f"Glass_{side_label}",
            bm_gl,
            cfg["piv_headlamp_glass_local"],
            parent=hl_obj,
            mat_list=[mats["R1_LampAmber"]],
            cfg=cfg,
        )

    bm_fi = bmesh.new()
    append_beveled_box(
        bm_fi,
        size_xyz=(0.28, 0.048, 0.045),
        center_xyz=(0.0, -0.03, 0.01),
        bevel_width=0.006,
        bevel_segments=1,
        mat_idx=0,
    )
    fi_obj = create_mesh_node(
        "FrontIntake",
        bm_fi,
        cfg["piv_front_intake"],
        parent=body_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )
    bm_fir = bmesh.new()
    append_cylinder(
        bm_fir,
        radius1=0.012,
        radius2=0.012,
        length=0.22,
        axis="X",
        center_xyz=(0.0, -0.03, 0.01),
        segments=14,
        mat_idx=0,
    )
    create_mesh_node(
        "FrontIntakeRoller",
        bm_fir,
        cfg["piv_front_intake_roller_local"],
        parent=fi_obj,
        mat_list=[mats["R1_Copper"]],
        cfg=cfg,
    )

    bm_ct = bmesh.new()
    ct_r = cfg["cutter_outer_radius"]
    ct_y_off = cfg["cutter_visual_y_offset"]
    append_cylinder(
        bm_ct,
        radius1=ct_r,
        radius2=ct_r * 0.94,
        length=0.14,
        axis="Z",
        center_xyz=(0.0, ct_y_off, -0.01),
        segments=24,
        bevel_width=0.012,
        bevel_segments=2,
        mat_idx=0,
    )
    ct_obj = create_mesh_node(
        "CutterTurret",
        bm_ct,
        cfg["piv_cutter_turret"],
        parent=rover_root,
        mat_list=[mats["R1_Copper"], mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    bm_ch = bmesh.new()
    append_cylinder(
        bm_ch,
        radius1=ct_r * 0.76,
        radius2=ct_r * 0.68,
        length=0.09,
        axis="Z",
        center_xyz=(0.0, ct_y_off, 0.0),
        segments=20,
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=0,
    )
    append_torus(
        bm_ch,
        major_radius=ct_r * 0.44,
        minor_radius=0.012,
        axis="Z",
        center_xyz=(0.0, ct_y_off - 0.01, 0.045),
        major_seg=20,
        minor_seg=8,
        mat_idx=1,
    )
    ch_obj = create_mesh_node(
        "CutterHead",
        bm_ch,
        cfg["piv_cutter_head_local"],
        parent=ct_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Copper"]],
        cfg=cfg,
    )
    create_empty_node("CutterOrigin", cfg["piv_cutter_origin_local"], parent=ch_obj)
    create_empty_node("CutterSocket", cfg["piv_cutter_origin_local"], parent=ch_obj)
    create_empty_node("StitchOrigin", cfg["piv_stitch_origin_local"], parent=ch_obj)
    create_empty_node("CoilSocket_T1", cfg["piv_coil_socket_t1_local"], parent=ch_obj)

    bm_ds = bmesh.new()
    append_beveled_box(
        bm_ds,
        size_xyz=(0.54, 0.055, 0.075),
        center_xyz=(0.0, 0.02, 0.0),
        bevel_width=0.008,
        bevel_segments=1,
        mat_idx=0,
    )
    ds_obj = create_mesh_node(
        "DigScoop",
        bm_ds,
        cfg["piv_dig_scoop"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    bm_arms = bmesh.new()
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_arms,
            size_xyz=(0.040, 0.050, 0.28),
            center_xyz=(sx * 0.25, -0.07, 0.11),
            bevel_width=0.007,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(0.48, 0.0, 0.0),
        )
        append_cylinder(
            bm_arms,
            radius1=0.026,
            radius2=0.026,
            length=0.055,
            axis="X",
            center_xyz=(sx * 0.25, 0.01, 0.02),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,
        )
    arms_obj = create_mesh_node(
        "ScoopArms",
        bm_arms,
        (0.0, 0.0, 0.0),
        parent=ds_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    bm_bkt = bmesh.new()
    bw = cfg["scoop_bucket_width"]
    bh = cfg["scoop_bucket_height"]
    bd = cfg["scoop_bucket_depth"]
    append_beveled_box(
        bm_bkt,
        size_xyz=(bw, bh, bd * 0.35),
        center_xyz=(0.0, 0.02, -bd * 0.25),
        bevel_width=0.012,
        bevel_segments=2,
        mat_idx=0,
        rot_euler=(0.18, 0.0, 0.0),
    )
    append_beveled_box(
        bm_bkt,
        size_xyz=(bw, bh * 0.22, bd),
        center_xyz=(0.0, -bh * 0.38, 0.04),
        bevel_width=0.008,
        bevel_segments=1,
        mat_idx=0,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_bkt,
            size_xyz=(0.030, bh * 0.88, bd * 0.88),
            center_xyz=(sx * (bw * FLAT_UV_SCALE - 0.014), -0.01, 0.02),
            bevel_width=0.007,
            bevel_segments=1,
            mat_idx=0,
        )
    tooth_count = cfg["scoop_tooth_count"]
    tooth_span = bw * 0.78
    for i in range(tooth_count):
        tx = -tooth_span * FLAT_UV_SCALE + tooth_span * i / max(1, tooth_count - 1)
        append_beveled_box(
            bm_bkt,
            size_xyz=(0.046, 0.024, 0.062),
            center_xyz=(tx, -bh * 0.44, bd * 0.52),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,
        )
    bkt_obj = create_mesh_node(
        "ScoopBucket",
        bm_bkt,
        cfg["piv_scoop_bucket_local"],
        parent=arms_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"], mats["R1_OrangeStripe"]],
        cfg=cfg,
    )
    create_empty_node("ScoopOrigin", cfg["piv_scoop_origin_local"], parent=bkt_obj)

    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        bm_cyl = bmesh.new()
        append_cylinder(
            bm_cyl,
            radius1=0.020,
            radius2=0.020,
            length=0.15,
            axis="Z",
            center_xyz=(0.0, -0.04, 0.05),
            segments=14,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(0.45, 0.0, 0.0),
        )
        create_mesh_node(
            f"ScoopCyl_{side_label}",
            bm_cyl,
            (sx * cfg["piv_scoop_cyl_x"], cfg["piv_scoop_cyl_y"], cfg["piv_scoop_cyl_z"]),
            parent=ds_obj,
            mat_list=[mats["R1_Copper"], mats["R1_BareSteel"]],
            cfg=cfg,
        )


def build_rear_and_shoulder_nodes(rover_root, body_obj, cfg, mats):
    """Build BatteryLamp_0..7, Stage_1..3_L/R, ChargeConduits, CargoHold, CargoHatch, WinchSocket, Antenna."""
    span_x = cfg["battery_bar_span_x"]
    for i in range(8):
        bx = -span_x * FLAT_UV_SCALE + span_x * i / 7.0
        bm_bl = bmesh.new()
        append_beveled_box(
            bm_bl,
            size_xyz=(0.060, 0.030, 0.042),
            center_xyz=(0.0, 0.0, 0.0),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,
        )
        bl_obj = create_mesh_node(
            f"BatteryLamp_{i}",
            bm_bl,
            (round(bx, 4), cfg["piv_battery_lamp_y"], cfg["piv_battery_lamp_z"]),
            parent=body_obj,
            mat_list=[mats["R1_GreyMetal"]],
            cfg=cfg,
        )
        bm_bg = bmesh.new()
        append_beveled_box(
            bm_bg,
            size_xyz=(0.046, 0.020, 0.022),
            center_xyz=(0.0, 0.004, -0.012),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=0,
        )
        create_mesh_node(
            f"BatteryGlass_{i}",
            bm_bg,
            (0.0, 0.006, -0.005),
            parent=bl_obj,
            mat_list=[mats["R1_LampAmber"]],
            cfg=cfg,
        )

    bm_cc = bmesh.new()
    bm_cg = bmesh.new()
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_cc,
            size_xyz=(0.020, 0.012, 0.22),
            center_xyz=(sx * 0.22, 0.82, 0.24),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=0,
        )
        append_beveled_box(
            bm_cg,
            size_xyz=(0.010, 0.008, 0.20),
            center_xyz=(sx * 0.22, 0.824, 0.24),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=0,
        )
    cc_obj = create_mesh_node(
        "ChargeConduits",
        bm_cc,
        (0.0, 0.0, 0.0),
        parent=body_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )
    create_mesh_node(
        "ConduitGlow",
        bm_cg,
        (0.0, 0.0, 0.0),
        parent=cc_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    stage_coords = ((1, 0.86, 0.18), (2, 0.83, 0.32), (3, 0.78, 0.46))
    for idx, sy, sz in stage_coords:
        for side_label, sx in (("L", -1.0), ("R", 1.0)):
            bm_st = bmesh.new()
            append_beveled_box(
                bm_st,
                size_xyz=(0.024, 0.014, 0.032),
                center_xyz=(0.0, -0.02, 0.0),
                bevel_width=0.003,
                bevel_segments=1,
                mat_idx=0,
            )
            st_obj = create_mesh_node(
                f"Stage_{idx}_{side_label}",
                bm_st,
                (sx * 0.26, sy, sz),
                parent=body_obj,
                mat_list=[mats["R1_GreyMetal"], mats["R1_Copper"]],
                cfg=cfg,
            )
            bm_sg = bmesh.new()
            append_beveled_box(
                bm_sg,
                size_xyz=(0.016, 0.010, 0.022),
                center_xyz=(0.0, -0.018, 0.0),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=0,
            )
            create_mesh_node(
                f"StageGlow_{idx}_{side_label}",
                bm_sg,
                (0.0, 0.0, -0.006),
                parent=st_obj,
                mat_list=[mats["R1_LampAmber"]],
                cfg=cfg,
            )

    bm_hold = bmesh.new()
    append_beveled_box(
        bm_hold,
        size_xyz=(0.54, 0.30, 0.34),
        center_xyz=(0.0, 0.02, -0.04),
        bevel_width=0.012,
        bevel_segments=1,
        mat_idx=0,
    )
    hold_obj = create_mesh_node(
        "CargoHold",
        bm_hold,
        cfg["piv_cargo_hold"],
        parent=body_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )
    create_empty_node("HatchOrigin", cfg["piv_hatch_origin_local"], parent=hold_obj)
    create_empty_node("StoryRackSocket", cfg["piv_story_rack_local"], parent=hold_obj)

    bm_hw = bmesh.new()
    append_beveled_box(
        bm_hw,
        size_xyz=(0.48, 0.030, 0.038),
        center_xyz=(0.0, 0.0, -0.02),
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=0,
    )
    hw_obj = create_mesh_node(
        "HatchWindow",
        bm_hw,
        cfg["piv_hatch_window_local"],
        parent=hold_obj,
        mat_list=[mats["R1_GreyMetal"]],
        cfg=cfg,
    )
    for i in range(8):
        cx = -0.189 + 0.054 * i
        bm_hc = bmesh.new()
        append_beveled_box(
            bm_hc,
            size_xyz=(0.034, 0.014, 0.022),
            center_xyz=(0.0, 0.0, -0.018),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=0,
        )
        create_mesh_node(
            f"HatchCrate_{i}",
            bm_hc,
            (round(cx, 4), 0.0, -0.012),
            parent=hw_obj,
            mat_list=[mats["R1_LampAmber"]],
            cfg=cfg,
        )

    hatch_emp = create_empty_node("CargoHatch", cfg["piv_cargo_hatch"], parent=body_obj)
    bm_up = bmesh.new()
    hw_w = cfg["cargo_hatch_width"]
    append_beveled_box(
        bm_up,
        size_xyz=(hw_w, 0.24, 0.05),
        center_xyz=(0.0, -0.10, -0.03),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=2,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_up,
            size_xyz=(hw_w * 0.43, 0.14, 0.045),
            center_xyz=(sx * (hw_w * 0.235), -0.06, -0.038),
            bevel_width=0.008,
            bevel_segments=1,
            mat_idx=0,
        )
        append_beveled_box(
            bm_up,
            size_xyz=(hw_w * 0.43, 0.092, 0.046),
            center_xyz=(sx * (hw_w * 0.235), -0.175, -0.038),
            bevel_width=0.007,
            bevel_segments=1,
            mat_idx=1,
        )
    append_beveled_box(
        bm_up,
        size_xyz=(0.046, 0.092, 0.054),
        center_xyz=(0.0, -0.14, -0.042),
        bevel_width=0.006,
        bevel_segments=1,
        mat_idx=2,
    )
    create_mesh_node(
        "UpperCanopy",
        bm_up,
        (0.0, 0.0, 0.0),
        parent=hatch_emp,
        mat_list=[mats["R1_CreamEnamel"], mats["R1_OrangeStripe"], mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    bm_low = bmesh.new()
    append_beveled_box(
        bm_low,
        size_xyz=(hw_w, 0.085, 0.05),
        center_xyz=(0.0, 0.04, -0.032),
        bevel_width=0.008,
        bevel_segments=1,
        mat_idx=0,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_low,
            size_xyz=(hw_w * 0.43, 0.055, 0.046),
            center_xyz=(sx * (hw_w * 0.235), 0.04, -0.038),
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=1,
        )
    create_mesh_node(
        "LowerChute",
        bm_low,
        cfg["piv_lower_chute_local"],
        parent=hatch_emp,
        mat_list=[mats["R1_GreyMetal"], mats["R1_CreamEnamel"]],
        cfg=cfg,
    )

    bm_ir = bmesh.new()
    append_cylinder(
        bm_ir,
        radius1=0.016,
        radius2=0.016,
        length=hw_w * 0.82,
        axis="X",
        center_xyz=(0.0, 0.0, -0.015),
        segments=14,
        mat_idx=0,
    )
    create_mesh_node(
        "IntakeRoller",
        bm_ir,
        cfg["piv_intake_roller_local"],
        parent=hatch_emp,
        mat_list=[mats["R1_Copper"]],
        cfg=cfg,
    )

    bm_ws = bmesh.new()
    append_beveled_box(
        bm_ws,
        size_xyz=(0.28, 0.11, 0.12),
        center_xyz=(0.0, 0.02, -0.04),
        bevel_width=0.012,
        bevel_segments=1,
        mat_idx=0,
    )
    ws_obj = create_mesh_node(
        "WinchSocket",
        bm_ws,
        cfg["piv_winch_socket"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Copper"]],
        cfg=cfg,
    )
    create_empty_node("TetherSocket", (0.0, 0.0, 0.0), parent=ws_obj)

    bm_sp = bmesh.new()
    append_cylinder(
        bm_sp,
        radius1=0.040,
        radius2=0.040,
        length=0.16,
        axis="X",
        center_xyz=(0.0, 0.02, -0.05),
        segments=16,
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=0,
    )
    create_mesh_node(
        "WinchSpool",
        bm_sp,
        (0.0, 0.0, 0.0),
        parent=ws_obj,
        mat_list=[mats["R1_BareSteel"]],
        cfg=cfg,
    )

    bm_th = bmesh.new()
    append_beveled_box(
        bm_th,
        size_xyz=(0.068, 0.058, 0.058),
        center_xyz=(0.0, 0.01, 0.04),
        bevel_width=0.006,
        bevel_segments=1,
        mat_idx=0,
    )
    append_torus(
        bm_th,
        major_radius=0.025,
        minor_radius=0.007,
        axis="X",
        center_xyz=(0.0, -0.02, 0.02),
        major_seg=16,
        minor_seg=6,
        mat_idx=1,
    )
    th_obj = create_mesh_node(
        "TowHook",
        bm_th,
        cfg["piv_tow_hook_local"],
        parent=ws_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"], mats["R1_OrangeStripe"]],
        cfg=cfg,
    )
    create_empty_node("WinchOrigin", (0.0, 0.0, 0.0), parent=th_obj)

    bm_ant = bmesh.new()
    append_cylinder(
        bm_ant,
        radius1=0.034,
        radius2=0.026,
        length=0.10,
        axis="Y",
        center_xyz=(0.0, 0.03, 0.0),
        segments=16,
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=0,
    )
    append_cylinder(
        bm_ant,
        radius1=0.010,
        radius2=0.008,
        length=0.62,
        axis="Y",
        center_xyz=(0.015, 0.34, -0.02),
        segments=12,
        mat_idx=1,
        rot_euler=(0.05, 0.0, -0.04),
    )
    ant_obj = create_mesh_node(
        "Antenna",
        bm_ant,
        cfg["piv_antenna"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"], mats["R1_Copper"]],
        cfg=cfg,
    )
    create_empty_node("AntennaSocket", (0.0, 0.0, 0.0), parent=ant_obj)

    bm_loop = bmesh.new()
    append_torus(
        bm_loop,
        major_radius=0.056,
        minor_radius=0.008,
        axis="Z",
        center_xyz=(0.0, -0.08, 0.0),
        major_seg=20,
        minor_seg=6,
        mat_idx=0,
    )
    loop_obj = create_mesh_node(
        "SonarLoop",
        bm_loop,
        cfg["piv_sonar_loop_local"],
        parent=ant_obj,
        mat_list=[mats["R1_Copper"], mats["R1_BareSteel"]],
        cfg=cfg,
    )
    create_empty_node("AntennaOrigin", (0.0, 0.0, 0.0), parent=loop_obj)

    bm_tip = bmesh.new()
    append_cylinder(
        bm_tip,
        radius1=0.020,
        radius2=0.018,
        length=0.052,
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=14,
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=0,
    )
    create_mesh_node(
        "AntennaTip",
        bm_tip,
        cfg["piv_antenna_tip_local"],
        parent=ant_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )


def build_suspension_and_sockets(rover_root, cfg, mats):
    """Build Arm_FL..RR, Shock_FL..RR, Wheel_FL..RR, and all remaining upgrade/equipment Empty sockets."""
    for end_label, sz in (("F", 1.0), ("R", -1.0)):
        for side_label, sx in (("L", -1.0), ("R", 1.0)):
            tag = f"{end_label}{side_label}"
            arm_piv = (sx * cfg["piv_arm_x"], cfg["piv_arm_y"], sz * cfg["piv_arm_z"])
            bm_arm = bmesh.new()
            append_beveled_box(
                bm_arm,
                size_xyz=(0.056, 0.060, 0.35),
                center_xyz=(sx * 0.03, -0.06, sz * 0.15),
                bevel_width=0.008,
                bevel_segments=1,
                mat_idx=0,
                rot_euler=(sz * 0.32, 0.0, 0.0),
            )
            create_mesh_node(
                f"Arm_{tag}",
                bm_arm,
                arm_piv,
                parent=rover_root,
                mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
                cfg=cfg,
            )

            shock_piv = (sx * cfg["piv_shock_x"], cfg["piv_shock_y"], sz * cfg["piv_shock_z"])
            bm_sh = bmesh.new()
            append_cylinder(
                bm_sh,
                radius1=0.024,
                radius2=0.022,
                length=0.22,
                axis="Y",
                center_xyz=(sx * 0.01, -0.11, sz * 0.04),
                segments=14,
                bevel_width=0.003,
                bevel_segments=1,
                mat_idx=0,
                rot_euler=(sz * (-0.28), 0.0, 0.0),
            )
            for ring_i in range(4):
                ry = -0.05 - 0.036 * ring_i
                rz = sz * (0.015 + 0.010 * ring_i)
                append_torus(
                    bm_sh,
                    major_radius=0.034,
                    minor_radius=0.008,
                    axis="Y",
                    center_xyz=(sx * 0.01, ry, rz),
                    major_seg=14,
                    minor_seg=5,
                    mat_idx=1,
                )
            create_mesh_node(
                f"Shock_{tag}",
                bm_sh,
                shock_piv,
                parent=rover_root,
                mat_list=[mats["R1_GreyMetal"], mats["R1_Copper"], mats["R1_BareSteel"]],
                cfg=cfg,
            )

            wheel_piv = (sx * cfg["wheel_track"], cfg["wheel_radius"], sz * cfg["wheel_base"])
            is_rl = tag == "RL"
            w_obj = build_balloon_wheel(f"Wheel_{tag}", wheel_piv, is_rl, rover_root, cfg, mats)
            if tag == "RR":
                create_empty_node("WheelsSocket", (0.12, 0.0, 0.0), parent=w_obj)

    sw_emp = create_empty_node("SolarWing", cfg["piv_solar_wing"], parent=rover_root)
    create_empty_node("WingSocket", (0.0, 0.0, -0.24), parent=sw_emp)

    socket_table = (
        ("CradleSocket", (0.0, 0.96, -0.46)),
        ("CargoSocket", (0.0, 0.48, -0.64)),
        ("HoldSocket", (0.0, 0.48, -0.64)),
        ("HeadlampSocket", (0.0, 0.68, 0.73)),
        ("LampSocket", (0.0, 0.68, 0.73)),
        ("CoilSocket", (0.0, 0.28, 0.0)),
        ("BellySocket", (0.0, 0.28, 0.0)),
        ("DrumSocket_L", (-0.46, 0.84, -0.18)),
        ("DrumSocket_R", (0.46, 0.84, -0.18)),
        ("FlanksSocket", (0.46, 0.84, -0.18)),
        ("BinSocket_L", (-0.46, 0.60, 0.02)),
        ("BinSocket_R", (0.46, 0.60, 0.02)),
        ("PanniersSocket", (0.46, 0.60, 0.02)),
        ("SpineSocket", (0.0, 0.95, -0.08)),
        ("BackSocket", (0.0, 0.95, -0.08)),
        ("DustSocket_L", (-0.64, 0.0, -0.80)),
        ("DustSocket_R", (0.64, 0.0, -0.80)),
    )
    for s_name, s_pos in socket_table:
        create_empty_node(s_name, s_pos, parent=rover_root)


def build_rover_model_hierarchy(cfg, mats):
    """Build the complete 122-node RoverModel hierarchy in the active Blender scene."""
    rover_root = create_empty_node("RoverModel", (0.0, 0.0, 0.0))
    body_obj = build_contoured_body_mesh(cfg, mats)
    body_obj.parent = rover_root
    build_head_assembly(rover_root, cfg, mats)
    build_front_tools_and_lamps(rover_root, body_obj, cfg, mats)
    build_rear_and_shoulder_nodes(rover_root, body_obj, cfg, mats)
    build_suspension_and_sockets(rover_root, cfg, mats)
    bpy.context.view_layer.update()
    return rover_root


def build_solar_wing_model(cfg, mats, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Tilly's 2x3 photovoltaic SolarWing model (2 nodes: SolarWing mesh + WingSocket empty).
    Uses R1_SolarCell for the PV cells per Logs/TASK.md.
    """
    bm = bmesh.new()
    sw_w = cfg["solar_wing_width"]
    sw_l = cfg["solar_wing_length"]
    slope_rad = math.radians(cfg["solar_deck_slope_deg"])

    append_beveled_box(
        bm,
        size_xyz=(sw_w, 0.026, sw_l),
        center_xyz=(0.0, 0.0, -sw_l * FLAT_UV_SCALE),
        bevel_width=0.010,
        bevel_segments=3,
        mat_idx=0,
        rot_euler=(slope_rad, 0.0, 0.0),
    )
    append_beveled_box(
        bm,
        size_xyz=(sw_w * 0.90, 0.008, sw_l * 0.90),
        center_xyz=(0.0, -0.010, -sw_l * FLAT_UV_SCALE),
        bevel_width=0.003,
        bevel_segments=2,
        mat_idx=3,
        rot_euler=(slope_rad, 0.0, 0.0),
    )

    cols, rows = 2, 3
    cell_w = (sw_w * 0.84) / cols
    cell_l = (sw_l * 0.84) / rows
    for c in range(cols):
        cx = (-0.5 + c) * (cell_w + 0.018)
        for r in range(rows):
            cz_local = -0.09 - r * (cell_l + 0.016)
            cy_local = -cz_local * math.tan(slope_rad) + 0.012
            append_beveled_box(
                bm,
                size_xyz=(cell_w, 0.010, cell_l),
                center_xyz=(cx, cy_local, cz_local),
                bevel_width=0.004,
                bevel_segments=2,
                mat_idx=2,
                rot_euler=(slope_rad, 0.0, 0.0),
            )
            append_beveled_box(
                bm,
                size_xyz=(0.006, 0.012, cell_l * 0.92),
                center_xyz=(cx, cy_local + 0.002, cz_local),
                bevel_width=0.001,
                bevel_segments=1,
                mat_idx=1,
                rot_euler=(slope_rad, 0.0, 0.0),
            )

    for sx in (-1.0, 1.0):
        append_cylinder(
            bm,
            radius1=0.016,
            radius2=0.016,
            length=0.06,
            axis="X",
            center_xyz=(sx * (sw_w * 0.36), 0.0, 0.0),
            segments=18,
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=1,
        )
    append_beveled_box(
        bm,
        size_xyz=(0.06, 0.022, 0.04),
        center_xyz=(0.0, 0.008, -0.02),
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=4,
    )

    mat_list = [
        mats["R1_GreyMetal"],
        mats["R1_BareSteel"],
        mats["R1_SolarCell"],
        mats["R1_GoldFoil"],
        mats["R1_Copper"],
    ]
    sw_obj = create_mesh_node("SolarWing", bm, unity_loc, parent=parent, mat_list=mat_list, cfg=cfg)
    create_empty_node("WingSocket", (0.0, 0.02, -0.24), parent=sw_obj)
    bpy.context.view_layer.update()
    return sw_obj


def build_tape_deck_model(cfg, mats, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """Build the 8-node Kit_TapeDeck right-cheek cassette deck model."""
    bm_body = bmesh.new()
    td_w = cfg["tape_deck_width"]
    td_h = cfg["tape_deck_height"]
    td_d = cfg["tape_deck_depth"]

    append_beveled_box(
        bm_body,
        size_xyz=(td_w * 0.45, td_h * 1.04, td_d * 1.04),
        center_xyz=(-td_w * 0.22, 0.0, 0.0),
        bevel_width=0.012,
        bevel_segments=3,
        mat_idx=1,
    )
    append_beveled_box(
        bm_body,
        size_xyz=(td_w * 0.75, td_h * 0.92, td_d * 0.92),
        center_xyz=(td_w * 0.12, 0.0, 0.0),
        bevel_width=0.012,
        bevel_segments=3,
        mat_idx=0,
    )
    td_root = create_mesh_node(
        "Kit_TapeDeck",
        bm_body,
        unity_loc,
        parent=parent,
        mat_list=[mats["R1_GreyMetal"], mats["R1_CreamEnamel"], mats["R1_BareSteel"], mats["R1_Copper"]],
        cfg=cfg,
    )

    bm_door = bmesh.new()
    append_beveled_box(
        bm_door,
        size_xyz=(0.020, td_h * 0.68, td_d * 0.72),
        center_xyz=(0.008, 0.04, 0.0),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,
    )
    append_beveled_box(
        bm_door,
        size_xyz=(0.014, td_h * 0.42, td_d * 0.52),
        center_xyz=(0.014, 0.04, 0.0),
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=1,
    )
    create_mesh_node(
        "CassetteDoor",
        bm_door,
        (0.035, -0.04, 0.0),
        parent=td_root,
        mat_list=[mats["R1_BareSteel"], mats["R1_DarkGlass"], mats["R1_OrangeStripe"]],
        cfg=cfg,
    )

    for reel_name, rz in (("Reel_L", 0.035), ("Reel_R", -0.035)):
        bm_reel = bmesh.new()
        append_cylinder(
            bm_reel,
            radius1=0.022,
            radius2=0.022,
            length=0.014,
            axis="X",
            center_xyz=(0.008, 0.0, 0.0),
            segments=20,
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=0,
        )
        create_mesh_node(
            reel_name,
            bm_reel,
            (0.03, 0.0, rz),
            parent=td_root,
            mat_list=[mats["R1_BareSteel"], mats["R1_Copper"]],
            cfg=cfg,
        )

    key_specs = (
        ("Key_Play", 0.04, mats["R1_OrangeStripe"]),
        ("Key_Stop", 0.0, mats["R1_GreyMetal"]),
        ("Key_FF", -0.04, mats["R1_GreyMetal"]),
    )
    for k_name, kz, k_mat in key_specs:
        bm_key = bmesh.new()
        append_beveled_box(
            bm_key,
            size_xyz=(0.024, 0.020, 0.028),
            center_xyz=(0.0, 0.010, 0.0),
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=0,
        )
        create_mesh_node(
            k_name,
            bm_key,
            (0.025, 0.072, kz),
            parent=td_root,
            mat_list=[k_mat, mats["R1_BareSteel"]],
            cfg=cfg,
        )

    bm_led = bmesh.new()
    append_cylinder(
        bm_led,
        radius1=0.008,
        radius2=0.008,
        length=0.012,
        axis="X",
        center_xyz=(0.004, 0.0, 0.0),
        segments=14,
        bevel_width=0.002,
        bevel_segments=2,
        mat_idx=0,
    )
    create_mesh_node(
        "RunLED",
        bm_led,
        (0.038, 0.052, 0.065),
        parent=td_root,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )
    bpy.context.view_layer.update()
    return td_root
