"""Procedural BMesh builders for all nodes of RoverModel.fbx, SolarWing.fbx, and Kit_TapeDeck.fbx."""
import math
import bmesh
import bpy
from mathutils import Vector

import sys
from pathlib import Path

_BLENDER_DIR = Path(__file__).resolve().parent.parent
if str(_BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(_BLENDER_DIR))

from r1kit import (
    FLAT_UV_SCALE,
    FULL_CIRCLE_RAD,
    HALF_PI_RAD,
    MAKER_DECAL_CELLS,
    UNIT_EPSILON,
    _init_solid_vcol_mesh,
    add_maker_decal_node,
    append_beveled_box,
    append_cylinder,
    append_dome_rivet,
    append_helical_coil,
    append_hex_bolt,
    append_lathe_profile,
    append_prism_extrusion,
    append_spherical_dome,
    append_torus,
    apply_weighted_normals,
    create_empty,
    create_mesh_node,
    ensure_outward_normals,
    merge_child_meshes_into_host,
    prune_unused_materials,
    u_pos,
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
    Generate a counter-clockwise horizontal perimeter ring in Unity (ux, uz) with 4 smooth
    6-segment rounded planform corners and 16 segments along each left/right flank so the
    beetle carapace has smooth planform curvature and semicircular wheel-arch cutouts.
    """
    rc = min(corner_r, hx * 0.44, (z_max - z_min) * 0.26)
    x_in = hx - rc
    zf_in = z_max - rc
    zr_in = z_min + rc

    pts = []

    def add_line_pts(p0, p1, segs=4):
        t_vals = [0.02] + [s / segs for s in range(1, segs)] + [0.98]
        for t in t_vals:
            pts.append((p0[0] + t * (p1[0] - p0[0]), p0[1] + t * (p1[1] - p0[1])))

    def add_arc_pts(cx, cz, a0, a1, segs=6):
        for s in range(segs + 1):
            a = a0 + (a1 - a0) * (s / segs)
            pts.append((cx + rc * math.cos(a), cz + rc * math.sin(a)))

    # 1. Front-Right corner (0 -> pi/2)
    add_arc_pts(x_in, zf_in, 0.0, HALF_PI_RAD, segs=6)
    # Front curved edge (+x_in -> -x_in at z = z_max)
    add_line_pts((x_in, z_max), (-x_in, z_max), segs=4)
    # 2. Front-Left corner (pi/2 -> pi)
    add_arc_pts(-x_in, zf_in, HALF_PI_RAD, math.pi, segs=6)
    # Left flank (z = zf_in -> zr_in at x = -hx) — 16 segments for smooth wheel arches & bonnet curve
    add_line_pts((-hx, zf_in), (-hx, zr_in), segs=16)
    # 3. Rear-Left corner (pi -> 3*pi/2)
    add_arc_pts(-x_in, zr_in, math.pi, 1.5 * math.pi, segs=6)
    # Rear edge (-x_in -> +x_in at z = z_min)
    add_line_pts((-x_in, z_min), (x_in, z_min), segs=4)
    # 4. Rear-Right corner (3*pi/2 -> 2*pi)
    add_arc_pts(x_in, zr_in, 1.5 * math.pi, FULL_CIRCLE_RAD, segs=6)
    # Right flank (z = zr_in -> zf_in at x = +hx) — 16 segments for smooth wheel arches & bonnet curve
    add_line_pts((hx, zr_in), (hx, zf_in), segs=16)

    return pts


def _wheel_arch_cutout_y(uz, wheel_base=0.53):
    """
    Return the lower hull arch cutout height y_cut(uz) at longitudinal coordinate uz.
    Between the wheels (|uz| <= 0.18 m), the central hull rocker sits at y = 0.415 m.
    Over the front (uz = +wheel_base) and rear (uz = -wheel_base) wheels, a circular arch of radius 0.305 m
    lifts the lower hull edge up to y = 0.595 m, exposing Shock_FL..RR and Arm_FL..RR.
    """
    y_base = 0.415
    for wz in (-wheel_base, wheel_base):
        dz = abs(uz - wz)
        if dz < 0.305:
            arch_h = math.sqrt(max(0.0, 0.305 * 0.305 - dz * dz)) * 0.60
            y_base = max(y_base, 0.415 + arch_h)
    return y_base


def _append_pillowed_body_tub(bm, cfg):
    """
    Build the genuinely rounded, pillowed "beetle" carapace hull with:
      - Multi-ring curved front bonnet & rounded shoulders (large-radius curvature on bonnet, shoulders & deck)
      - Softly barreled flanks with crisp recessed dark seam channels (R1_GreyMetal) above/below the orange beltline
      - Deep semicircular wheel-arch cutouts over all 4 wheels at +/-wheel_base
    """
    temp = bmesh.new()
    half_w = cfg["body_tub_width"] * FLAT_UV_SCALE
    half_l = cfg["body_tub_length"] * FLAT_UV_SCALE
    z_center = cfg["body_tub_z_center"]
    z_min_base = z_center - half_l
    z_max_base = z_center + half_l
    corner_r = 0.085  # Pillowed planform corners for a classic beetle appliance silhouette
    belt_bot = cfg["body_belt_y_bottom"]
    belt_top = cfg["body_belt_y_top"]
    w_base = cfg["wheel_base"]

    # 22 vertical/contour rings from underbody up through barreled flanks, curved bonnet, and domed beetle shoulders:
    # (uy, inset_x, inset_z_rear, inset_z_front, corner_r_scale, ring_role)
    ring_specs = (
        (0.415, 0.110, 0.110, 0.110, 1.00, "bottom"),       # 0: bottom inner flat ring
        (0.415, 0.022, 0.022, 0.022, 1.00, "bottom"),       # 1: bottom bevel guard
        (0.424, 0.008, 0.008, 0.008, 1.00, "lower_cream"),  # 2: lower rounded rocker seg 1
        (0.438, 0.002, 0.002, 0.002, 1.00, "lower_cream"),  # 3: lower rounded rocker seg 2
        (0.550, -0.002, 0.000, 0.000, 1.02, "lower_cream"), # 4: lower flank top before belt seam
        (0.555, 0.005, 0.005, 0.005, 1.00, "seam"),         # 5: recessed lower beltline cut seam
        (belt_bot, 0.005, 0.005, 0.005, 1.00, "seam"),      # 6: recessed lower beltline cut seam top
        (0.564, -0.004, -0.002, -0.002, 1.04, "belt"),      # 7: barreled orange beltline lower lip
        (0.605, -0.007, -0.004, -0.004, 1.06, "belt"),      # 8: barreled orange beltline equator crown
        (0.644, -0.004, -0.002, -0.002, 1.04, "belt"),      # 9: barreled orange beltline upper lip
        (belt_top, 0.005, 0.005, 0.005, 1.00, "seam"),      # 10: recessed upper beltline cut seam bottom
        (0.654, 0.005, 0.005, 0.005, 1.00, "seam"),         # 11: recessed upper beltline cut seam top
        (0.660, -0.002, 0.000, 0.004, 1.04, "upper_cream"), # 12: upper flank bottom lip
        (0.735, -0.001, 0.008, 0.022, 1.12, "upper_cream"), # 13: lower beetle nose & flank swell
        (0.790, 0.005, 0.018, 0.135, 1.25, "upper_cream"),  # 14: mid beetle bonnet curve over cutter boss
        (0.835, 0.014, 0.036, 0.295, 1.35, "upper_cream"),  # 15: upper beetle bonnet curve & shoulder roll 1
        (0.875, 0.026, 0.056, 0.365, 1.42, "upper_cream"),  # 16: rounded beetle shoulder roll 2
        (0.912, 0.042, 0.078, 0.405, 1.48, "upper_cream"),  # 17: rounded beetle shoulder roll 3
        (0.942, 0.062, 0.095, 0.435, 1.52, "upper_cream"),  # 18: upper cowling shoulder roll 4
        (0.956, 0.090, 0.102, 0.458, 1.52, "upper_cream"),  # 19: domed upper carapace crown edge
        (0.958, 0.140, 0.104, 0.520, 1.45, "upper_cream"),  # 20: top deck guard ring
        # Rear roof recess rim & stepped dark metal solar bay well (uz = [-0.653, -0.360], |ux| <= 0.248)
        (0.948, 0.192, 0.092, 1.105, 0.35, "recess_rim"),   # 21: rear roof recess rim
        (0.902, 0.198, 0.098, 1.111, 0.32, "recess_floor"), # 22: rear roof recess floor
    )

    ring_verts = []
    for r_idx, (uy, in_x, in_zr, in_zf, cr_scale, _role) in enumerate(ring_specs):
        hx = max(0.08, half_w - in_x)
        z_min = z_min_base + in_zr
        z_max = z_max_base - in_zf
        pts_2d = _sample_rounded_rect_ring(hx, z_min, z_max, corner_r * cr_scale)
        v_row = []
        for ux, uz in pts_2d:
            adj_y = uy
            adj_x = ux
            # Gentle longitudinal barrel swell along the mid-body flanks
            z_norm = max(-1.0, min(1.0, uz / max(half_l, 0.1)))
            if 4 <= r_idx <= 16 and abs(ux) > hx * 0.65:
                barrel_x = 0.008 * max(0.0, 1.0 - z_norm * z_norm)
                adj_x += math.copysign(barrel_x, ux)
            # Gentle lateral crown across the front bonnet and upper carapace, tapered over the rear deck
            if 13 <= r_idx <= 20 and abs(ux) < hx * 0.85:
                x_norm = ux / max(hx, 0.08)
                rear_taper = 0.25 if uz < -0.32 else 1.0
                adj_y += 0.008 * rear_taper * max(0.0, 1.0 - x_norm * x_norm)
            # Deep semicircular wheel-arch cutout into the lower flanks over all 4 wheels
            if r_idx <= 8 and abs(ux) > half_w * 0.70:
                y_arch = _wheel_arch_cutout_y(uz, wheel_base=w_base)
                if y_arch > 0.417:
                    if r_idx == 0:
                        adj_y = max(uy, y_arch - 0.012)
                    elif r_idx == 1:
                        adj_y = max(uy, y_arch - 0.008)
                    elif r_idx == 2:
                        adj_y = max(uy, y_arch - 0.003)
                    elif r_idx <= 6:
                        adj_y = max(uy, y_arch)
                    else:
                        adj_y = max(uy, y_arch + 0.006)
            v_row.append(temp.verts.new(u_pos(adj_x, adj_y, uz)))
        ring_verts.append(v_row)

    n_perim = len(ring_verts[0])
    for r_idx in range(len(ring_specs) - 1):
        r0 = ring_verts[r_idx]
        r1 = ring_verts[r_idx + 1]
        role0 = ring_specs[r_idx][5]
        role1 = ring_specs[r_idx + 1][5]
        if role0 == "seam" or role1 == "seam" or role1 == "recess_floor":
            mat_idx = 2  # R1_GreyMetal dark recessed seam channel & solar bay well
        elif role0 == "belt" and role1 == "belt":
            mat_idx = 1  # R1_OrangeStripe
        elif role0 == "bottom":
            mat_idx = 2  # R1_GreyMetal underbody
        else:
            mat_idx = 0  # R1_CreamEnamel
        for i in range(n_perim):
            ni = (i + 1) % n_perim
            f = temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
            f.material_index = mat_idx
            f.smooth = True

    bot_center = temp.verts.new(u_pos(0.0, 0.415, z_center))
    top_center = temp.verts.new(u_pos(0.0, 0.902, -0.506))
    for i in range(n_perim):
        ni = (i + 1) % n_perim
        fb = temp.faces.new((ring_verts[0][ni], ring_verts[0][i], bot_center))
        fb.material_index = 2
        fb.smooth = True
        ft = temp.faces.new((ring_verts[-1][i], ring_verts[-1][ni], top_center))
        ft.material_index = 2
        ft.smooth = True

    # Sculpted curved lower beetle bonnet hood shell over the front nose (uz = +0.26..+0.72 m, uy = 0.68..0.84 m)
    hood_Temp = bmesh.new()
    hood_z_stations = (
        (0.24, 0.365, 0.845, 0.018),
        (0.34, 0.360, 0.838, 0.022),
        (0.45, 0.350, 0.818, 0.024),
        (0.55, 0.335, 0.792, 0.024),
        (0.64, 0.310, 0.756, 0.020),
        (0.70, 0.270, 0.715, 0.014),
        (0.73, 0.210, 0.675, 0.008),
    )
    hood_rings = []
    n_hx = 10
    for hz, hw_x, hy_top, h_crown in hood_z_stations:
        row_v = []
        # Top arch from -hw_x to +hw_x
        for ix in range(n_hx + 1):
            tx = -1.0 + 2.0 * (ix / n_hx)
            ux = tx * hw_x
            uy = hy_top + h_crown * (1.0 - tx * tx)
            row_v.append(hood_Temp.verts.new(u_pos(ux, uy, hz)))
        # Bottom return from +hw_x to -hw_x tucked inside main hull
        for ix in range(n_hx, -1, -1):
            tx = -1.0 + 2.0 * (ix / n_hx)
            ux = tx * (hw_x * 0.92)
            uy = hy_top - 0.045
            row_v.append(hood_Temp.verts.new(u_pos(ux, uy, hz)))
        hood_rings.append(row_v)

    n_hr = len(hood_rings[0])
    for k in range(len(hood_rings) - 1):
        r0 = hood_rings[k]
        r1 = hood_rings[k + 1]
        for i in range(n_hr):
            ni = (i + 1) % n_hr
            f = hood_Temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
            f.material_index = 0
            f.smooth = True

    back_c = hood_Temp.verts.new(u_pos(0.0, 0.82, 0.24))
    front_c = hood_Temp.verts.new(u_pos(0.0, 0.66, 0.73))
    for i in range(n_hr):
        ni = (i + 1) % n_hr
        fb = hood_Temp.faces.new((hood_rings[0][ni], hood_rings[0][i], back_c))
        fb.material_index = 0
        fb.smooth = True
        ff = hood_Temp.faces.new((hood_rings[-1][i], hood_rings[-1][ni], front_c))
        ff.material_index = 0
        ff.smooth = True

    ensure_outward_normals(hood_Temp)
    for v in hood_Temp.verts:
        temp.verts.new(v.co)
    temp.verts.ensure_lookup_table()
    offset_v = len(temp.verts) - len(hood_Temp.verts)
    for f in hood_Temp.faces:
        nf = temp.faces.new([temp.verts[offset_v + v.index] for v in f.verts])
        nf.material_index = f.material_index
        nf.smooth = True
    hood_Temp.free()

    ensure_outward_normals(temp)
    vert_map = {v: bm.verts.new(v.co) for v in temp.verts}
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = f.material_index
        nf.smooth = True
    temp.free()

    # Low-profile recessed bay rib rails & hinge mounting brackets inside the dark roof well (uy <= 0.916 m)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.032, 0.008, 0.210),
            center_xyz=(sx * 0.145, 0.908, -0.502),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=2,
            guard_inset=False,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.044, 0.010, 0.028),
            center_xyz=(sx * 0.145, 0.911, -0.395),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=5,  # R1_BareSteel hinge bracket pad
            guard_inset=False,
        )


def _append_polyline_seam_tube(bm, pts_3d, radius=0.004, mat_idx=2, ref_axis=(1.0, 0.0, 0.0)):
    """Append a continuous manifold 4-sided seam molding tube along a 3D Unity polyline."""
    temp = bmesh.new()
    b_pts = [Vector(u_pos(*p)) for p in pts_3d]
    ref_v = Vector(ref_axis).normalized()
    rings = []
    n_pts = len(b_pts)
    for i, p in enumerate(b_pts):
        if i == 0:
            t = (b_pts[1] - b_pts[0]).normalized()
        elif i == n_pts - 1:
            t = (b_pts[-1] - b_pts[-2]).normalized()
        else:
            t = ((b_pts[i] - b_pts[i - 1]).normalized() + (b_pts[i + 1] - b_pts[i]).normalized()).normalized()
        n = (ref_v - t * ref_v.dot(t)).normalized()
        b = t.cross(n).normalized()
        ring = [
            temp.verts.new(p + radius * (n + b)),
            temp.verts.new(p + radius * (-n + b)),
            temp.verts.new(p + radius * (-n - b)),
            temp.verts.new(p + radius * (n - b)),
        ]
        rings.append(ring)

    for k in range(n_pts - 1):
        r0 = rings[k]
        r1 = rings[k + 1]
        for s in range(4):
            ns = (s + 1) % 4
            f = temp.faces.new((r0[s], r0[ns], r1[ns], r1[s]))
            f.material_index = mat_idx
            f.smooth = True

    f_start = temp.faces.new((rings[0][3], rings[0][2], rings[0][1], rings[0][0]))
    f_start.material_index = mat_idx
    f_start.smooth = True
    f_end = temp.faces.new((rings[-1][0], rings[-1][1], rings[-1][2], rings[-1][3]))
    f_end.material_index = mat_idx
    f_end.smooth = True

    ensure_outward_normals(temp)
    v_map = {v: bm.verts.new(v.co) for v in temp.verts}
    for f in temp.faces:
        nf = bm.faces.new([v_map[v] for v in f.verts])
        nf.material_index = f.material_index
        nf.smooth = True
    temp.free()


def _hull_flank_x(uy, uz, cfg):
    """Return the exact exterior X coordinate of the lofted beetle flank at (uy, uz)."""
    half_w = cfg["body_tub_width"] * FLAT_UV_SCALE
    half_l = cfg["body_tub_length"] * FLAT_UV_SCALE
    profile = (
        (0.660, -0.002),
        (0.735, -0.001),
        (0.790, 0.005),
        (0.835, 0.014),
        (0.875, 0.026),
    )
    in_x = profile[-1][1]
    for i in range(len(profile) - 1):
        y0, ix0 = profile[i]
        y1, ix1 = profile[i + 1]
        if uy <= y1:
            t = max(0.0, min(1.0, (uy - y0) / max(y1 - y0, 1e-6)))
            in_x = ix0 + t * (ix1 - ix0)
            break
    z_norm = max(-1.0, min(1.0, uz / max(half_l, 0.1)))
    barrel_x = 0.008 * max(0.0, 1.0 - z_norm * z_norm)
    return half_w - in_x + barrel_x


def _append_body_cut_panels_and_seams(bm, cfg):
    """
    Add flush 3D curved door-like flank seams and arched bonnet seams (R1_GreyMetal dark grooves)
    along the rounded beetle carapace flanks and bonnet.
    """
    flank_x = cfg["body_tub_width"] * FLAT_UV_SCALE + 0.006
    for sx in (-1.0, 1.0):
        # 1. Continuous curved door-like flank seam arching from rear door post (uz = -0.18 m)
        # up along the shoulder and down to the front door post (uz = +0.20 m)
        door_arch_yz = (
            (0.660, -0.180),
            (0.735, -0.180),
            (0.785, -0.180),
            (0.840, -0.155),
            (0.860, -0.075),
            (0.860, 0.075),
            (0.840, 0.165),
            (0.785, 0.200),
            (0.735, 0.200),
            (0.660, 0.200),
        )
        door_pts_3d = [
            (sx * (_hull_flank_x(uy, uz, cfg) + 0.001), uy, uz)
            for uy, uz in door_arch_yz
        ]
        _append_polyline_seam_tube(bm, door_pts_3d, radius=0.004, mat_idx=2, ref_axis=(1.0, 0.0, 0.0))

        # Beltline vertical cut seams at uz = -0.18 m and uz = +0.20 m
        for seam_z in (-0.180, 0.200):
            append_beveled_box(
                bm,
                size_xyz=(0.010, 0.084, 0.010),
                center_xyz=(sx * (flank_x + 0.006), 0.604, seam_z),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=2,
                guard_inset=False,
            )

        # Flush recessed service door latch handle near the forward edge of the curved flank door panel
        latch_y = 0.755
        latch_z = 0.150
        latch_x = _hull_flank_x(latch_y, latch_z, cfg)
        append_beveled_box(
            bm,
            size_xyz=(0.012, 0.032, 0.062),
            center_xyz=(sx * (latch_x - 0.001), latch_y, latch_z),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=2,
            rot_euler=(0.0, 0.0, -sx * 0.10),
            guard_inset=False,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.010, 0.016, 0.044),
            center_xyz=(sx * (latch_x + 0.003), latch_y, latch_z),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=10,  # R1_Brass latch pull
            rot_euler=(0.0, 0.0, -sx * 0.10),
            guard_inset=False,
        )

        # Continuous curved beetle bonnet hood seam following the nose arc
        bonnet_pts_3d = [
            (sx * 0.255, 0.854, 0.26),
            (sx * 0.255, 0.848, 0.34),
            (sx * 0.255, 0.829, 0.45),
            (sx * 0.255, 0.802, 0.55),
            (sx * 0.255, 0.763, 0.64),
            (sx * 0.255, 0.718, 0.70),
        ]
        _append_polyline_seam_tube(bm, bonnet_pts_3d, radius=0.0035, mat_idx=2, ref_axis=(1.0, 0.0, 0.0))




def _append_wheel_arch_flares_and_liners(bm, cfg):
    """
    Build smooth 16-segment semicircular wheel-arch fender flares and deep recessed wheel-well
    splash liners over all 4 wheels at +/-wheel_base, plus inner axle hub carrier housings.
    """
    arch_outer = []
    arch_inner = []
    n_arch = 16
    for i in range(n_arch + 1):
        ang = math.pi * (1.0 - i / n_arch)
        ca, sa = math.cos(ang), math.sin(ang)
        arch_outer.append((0.295 * ca, 0.410 + 0.205 * sa))
        arch_inner.append((0.250 * ca, 0.410 + 0.165 * sa))
    arch_poly = arch_outer + list(reversed(arch_inner))

    flare_x = cfg["fender_lip_x"]
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            arch_cz = sz * cfg["fender_lip_z"]
            append_prism_extrusion(
                bm,
                arch_poly,
                depth=0.036,
                axis="X",
                center_xyz=(sx * flare_x, 0.0, arch_cz),
                bevel_width=0.007,
                bevel_segments=2,
                mat_idx=2,
            )
            # Deep inboard splash liner at |ux| = 0.345 m
            append_beveled_box(
                bm,
                size_xyz=(0.020, 0.185, 0.420),
                center_xyz=(sx * 0.345, 0.505, arch_cz),
                bevel_width=0.004,
                bevel_segments=1,
                mat_idx=2,
                guard_inset=False,
            )
            # Inner suspension axle hub carrier housing at |ux| = 0.41 m, uy = 0.36 m
            append_beveled_box(
                bm,
                size_xyz=(0.14, 0.065, 0.14),
                center_xyz=(sx * 0.41, 0.368, arch_cz),
                bevel_width=0.008,
                bevel_segments=2,
                mat_idx=2,
                guard_inset=False,
            )


def _append_body_small_hardware(bm, cfg):
    """
    Add small honest hardware to Body:
      - 10 brass dome rivets + rubber gasket around SagePatch_RL (flush on the left flank)
      - Front bumper hex bolts & forward clevis brackets with pivot pins
      - Rear bumper small red/amber tail lamps flanking the winch
      - Armored rubber cable run with copper P-clips along lower chassis rails
      - Recessed 3-slot side cooling vents on rear lower flanks
      - Tubular steel grab handles with brass collars on upper rear shoulders
    """
    # 1. Riveted Sage-Green field-repair patch (SagePatch_RL) on the left flank (#9 in callouts.png)
    patch_x = -cfg["sage_patch_x"]
    patch_y = cfg["sage_patch_y"] - 0.045
    patch_z = cfg["sage_patch_z"]
    ph = cfg["sage_patch_height"]
    pl = cfg["sage_patch_length"]

    append_beveled_box(
        bm,
        size_xyz=(0.012, ph + 0.010, pl + 0.010),
        center_xyz=(patch_x + 0.002, patch_y, patch_z),
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=2,
        guard_inset=False,
    )
    append_beveled_box(
        bm,
        size_xyz=(0.014, ph, pl),
        center_xyz=(patch_x, patch_y, patch_z),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=3,
    )
    rivet_coords = []
    for ry in (-0.39, -0.13, 0.13, 0.39):
        for rz in (-0.40, 0.40):
            rivet_coords.append((patch_y + ry * ph, patch_z + rz * pl))
    rivet_coords.append((patch_y + 0.39 * ph, patch_z))
    rivet_coords.append((patch_y - 0.39 * ph, patch_z))

    for r_y, r_z in rivet_coords:
        append_dome_rivet(
            bm,
            radius=cfg["rivet_radius"],
            height=0.007,
            axis="X",
            center_xyz=(patch_x - 0.006, r_y, r_z),
            segments=8,
            mat_idx=10,  # R1_Brass dome rivets
            rot_euler=(0.0, math.pi, 0.0),
        )

    # 2. Front bumper bar hex mounting bolts & forward scoop clevis brackets (#7 in callouts.png)
    fb_z = cfg["front_bumper_z"] + cfg["bumper_depth"] * FLAT_UV_SCALE
    by_c = cfg["bumper_y_center"]
    append_beveled_box(
        bm,
        size_xyz=(cfg["bumper_width"] * 0.86, 0.024, 0.016),
        center_xyz=(0.0, by_c, fb_z - 0.004),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=2,
        guard_inset=False,
    )
    for bx in (-0.36, -0.26, -0.12, 0.12, 0.26, 0.36):
        append_hex_bolt(
            bm,
            radius=0.010,
            height=0.012,
            axis="Z",
            center_xyz=(bx, by_c, fb_z + 0.002),
            mat_idx=10,  # R1_Brass bumper bolts
        )
    for sx in (-1.0, 1.0):
        for ear_off in (-0.024, 0.024):
            append_beveled_box(
                bm,
                size_xyz=(0.014, 0.068, 0.068),
                center_xyz=(sx * 0.24 + ear_off, by_c - 0.012, fb_z + 0.016),
                bevel_width=0.004,
                bevel_segments=1,
                mat_idx=2,
                guard_inset=False,
            )
        append_cylinder(
            bm,
            radius1=0.024,
            radius2=0.024,
            length=0.062,
            axis="X",
            center_xyz=(sx * 0.24, by_c - 0.012, fb_z + 0.028),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=2,
        )
        append_hex_bolt(
            bm,
            radius=0.013,
            height=0.074,
            axis="X",
            center_xyz=(sx * 0.24, by_c - 0.012, fb_z + 0.028),
            mat_idx=10,  # R1_Brass pivot bolt
        )

    # 3. Rear bumper small red/amber tail lamps flanking the winch (x = +/-0.33 m, #12 in callouts.png)
    rb_z = cfg["rear_bumper_z"] - cfg["bumper_depth"] * FLAT_UV_SCALE
    for sx in (-1.0, 1.0):
        tl_x = sx * 0.33
        append_beveled_box(
            bm,
            size_xyz=(0.096, 0.052, 0.024),
            center_xyz=(tl_x, by_c + 0.004, rb_z + 0.004),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,
            guard_inset=False,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.082, 0.040, 0.020),
            center_xyz=(tl_x, by_c + 0.004, rb_z - 0.002),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=10,  # R1_Brass bezel
            guard_inset=False,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.068, 0.028, 0.018),
            center_xyz=(tl_x, by_c + 0.004, rb_z - 0.006),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,
            guard_inset=False,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.042, 0.016, 0.016),
            center_xyz=(tl_x, by_c + 0.004, rb_z - 0.009),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=8,
            guard_inset=False,
        )
        for bolt_x in (sx * 0.40, sx * 0.23):
            append_hex_bolt(
                bm,
                radius=0.009,
                height=0.012,
                axis="Z",
                center_xyz=(bolt_x, by_c + 0.004, rb_z - 0.002),
                mat_idx=10,
            )

    # 4. Armored rubber cable run + copper P-clips along lower central rocker rail (both flanks)
    flank_cable_x = cfg["body_tub_width"] * FLAT_UV_SCALE - 0.002
    for sx in (-1.0, 1.0):
        cable_x = sx * flank_cable_x
        for cy_off in (-0.008, 0.008):
            append_cylinder(
                bm,
                radius1=0.006,
                radius2=0.006,
                length=0.34,
                axis="Z",
                center_xyz=(cable_x, 0.422 + cy_off, 0.0),
                segments=8,
                mat_idx=9,
            )
        for clip_z in (-0.13, 0.0, 0.13):
            append_beveled_box(
                bm,
                size_xyz=(0.014, 0.032, 0.014),
                center_xyz=(cable_x, 0.422, clip_z),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=6,  # R1_Copper electrical P-clip
                guard_inset=False,
            )
            append_dome_rivet(
                bm,
                radius=0.004,
                height=0.005,
                axis="X",
                center_xyz=(cable_x + sx * 0.006, 0.422, clip_z),
                segments=8,
                mat_idx=10,
                rot_euler=(0.0, 0.0 if sx > 0 else math.pi, 0.0),
            )

    # 5. Recessed 3-slot side cooling louver vents on lower central flanks
    for sx in (-1.0, 1.0):
        vz_c = 0.05 if sx < 0 else -0.05
        append_beveled_box(
            bm,
            size_xyz=(0.014, 0.058, 0.095),
            center_xyz=(sx * (flank_cable_x - 0.002), 0.488, vz_c),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=2,
            guard_inset=False,
        )
        for s_i in (-1, 0, 1):
            append_beveled_box(
                bm,
                size_xyz=(0.012, 0.009, 0.078),
                center_xyz=(sx * (flank_cable_x + 0.001), 0.488 + s_i * 0.015, vz_c),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,
                guard_inset=False,
            )

    # 6. Tubular steel grab handles with brass mounting collars on upper rear shoulders
    for sx in (-1.0, 1.0):
        gx = sx * 0.355
        gy = 0.945
        gz = -0.44
        append_cylinder(
            bm,
            radius1=0.0065,
            radius2=0.0065,
            length=0.12,
            axis="Z",
            center_xyz=(gx, gy + 0.016, gz),
            segments=8,
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=5,
        )
        for end_z in (-0.055, 0.055):
            append_cylinder(
                bm,
                radius1=0.008,
                radius2=0.011,
                length=0.022,
                axis="Y",
                center_xyz=(gx, gy + 0.006, gz + end_z),
                segments=8,
                mat_idx=10,  # R1_Brass mount collar
            )


def build_contoured_body_mesh(cfg, mats):
    """
    Build the rounded "beetle" Body carapace matching turnaround.png & callouts.png:
      - Material 0: R1_CreamEnamel (pillowed beetle carapace with rounded shoulders & curved bonnet)
      - Material 1: R1_OrangeStripe (horizontal beltline + rear tail lamp surrounds)
      - Material 2: R1_GreyMetal (recessed panel seams, bumpers, belly pan, wheel arch lips)
      - Material 3: R1_SagePatch (riveted vertical sage-green repair plate on left flank)
      - Material 4: R1_GoldFoil (thermal insulation trim)
      - Material 5: R1_BareSteel (grab handles, vent louvers)
      - Material 6: R1_Copper (cable P-clips)
      - Material 7: R1_DarkGlass (optical trim)
      - Material 8: R1_LampAmber (rear tail lamp glowing cores)
      - Material 9: R1_Rubber (armored lower-chassis conduit cables)
      - Material 10: R1_Brass (machined brass rivets, bolts, latch fittings, mount collars)
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
        mats["R1_LampAmber"],
        mats["R1_Rubber"],
        mats["R1_Brass"],
    ]

    # 1. Pillowed rounded beetle body tub with beltline seam channels & semicircular wheel-arch cutouts
    _append_pillowed_body_tub(bm, cfg)

    # 2. Curved door-like flank seams, bonnet seams & brass service latches
    _append_body_cut_panels_and_seams(bm, cfg)

    # 3. Sculpted arched cream chest lens hood boss (#5 in callouts.png) with dark recessed seam collar
    append_cylinder(
        bm,
        radius1=0.128,
        radius2=0.114,
        length=0.20,
        axis="Z",
        center_xyz=(0.0, 0.695, 0.668),
        segments=24,
        bevel_width=0.014,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel arched hood boss
    )
    append_cylinder(
        bm,
        radius1=0.112,
        radius2=0.108,
        length=0.024,
        axis="Z",
        center_xyz=(0.0, 0.692, 0.764),
        segments=24,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=2,  # R1_GreyMetal recessed bezel collar
    )

    # 4. Full-width dark-charcoal front & rear bumper bars and central lower belly pan
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
        bevel_width=0.014,
        bevel_segments=2,
        mat_idx=2,
    )

    # 5. Smooth 16-segment semicircular wheel-arch fender flare lips & deep splash liners over all 4 wheels
    _append_wheel_arch_flares_and_liners(bm, cfg)

    # 6. Riveted SagePatch_RL, bumper bolts/clevises, tail lamps, cable run, side vents & grab handles
    _append_body_small_hardware(bm, cfg)

    return create_mesh_node("Body", bm, (0.0, 0.0, 0.0), mat_list=mat_list, cfg=cfg)


def build_head_assembly(rover_root, cfg, mats):
    """
    Build the hero 80%-width hard-surface CRT Head assembly (Neck -> NeckLift -> Head -> Screen, Visor,
    RearVentStrip, Eye + sockets, DeckSocket, DeckCover) matching turnaround.png & callouts.png.
    """
    # 1. Neck (molded collar ring + bolted flange + 4-ribbed accordion rubber bellows at (0, 0.92, +0.13))
    bm_neck = bmesh.new()
    flange_r = cfg["neck_base_flange_radius"]
    bellows_r = cfg["neck_bellows_radius"]

    collar_profile = [
        (flange_r * 1.08, -0.016),
        (flange_r * 1.05, -0.002),
        (flange_r * 0.96, 0.012),
        (flange_r * 0.88, 0.018),
        (bellows_r * 0.82, 0.018),
    ]
    append_lathe_profile(
        bm_neck,
        collar_profile,
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=24,
        mat_idx=0,
        ring_mat_indices=[2, 0, 0, 0],
    )
    for b_i in range(8):
        ang = FULL_CIRCLE_RAD * b_i / 8
        append_hex_bolt(
            bm_neck,
            radius=0.0075,
            height=0.009,
            axis="Y",
            center_xyz=(flange_r * 0.92 * math.cos(ang), 0.015, flange_r * 0.92 * math.sin(ang)),
            mat_idx=3,
        )

    # 4-ribbed accordion rubber bellows (R1_Rubber, #4 in callouts.png)
    bellows_profile = [(bellows_r * 0.78, 0.014)]
    for rib_i in range(4):
        y_base = 0.018 + rib_i * 0.0185
        r_peak = bellows_r * (1.0 - 0.02 * rib_i)
        r_val = bellows_r * (0.78 - 0.015 * rib_i)
        bellows_profile.extend([
            (r_val, y_base),
            (r_peak * 0.95, y_base + 0.004),
            (r_peak, y_base + 0.009),
            (r_peak * 0.95, y_base + 0.014),
            (r_val, y_base + 0.0185),
        ])
    bellows_profile.append((bellows_r * 0.70, 0.094))
    append_lathe_profile(
        bm_neck,
        bellows_profile,
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=24,
        mat_idx=1,
        auto_chamfer=False,
    )

    neck_obj = create_mesh_node(
        "Neck",
        bm_neck,
        cfg["piv_neck"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Rubber"], mats["R1_CreamEnamel"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # 2. NeckLift (telescopic inner column + gimbal yoke at (0, 0, 0) local to Neck)
    bm_lift = bmesh.new()
    append_cylinder(
        bm_lift,
        radius1=bellows_r * 0.70,
        radius2=bellows_r * 0.68,
        length=0.14,
        axis="Y",
        center_xyz=(0.0, 0.11, 0.0),
        segments=20,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )
    append_beveled_box(
        bm_lift,
        size_xyz=(0.24, 0.032, 0.15),
        center_xyz=(0.0, 0.185, 0.01),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=1,
    )
    neck_lift_obj = create_mesh_node(
        "NeckLift",
        bm_lift,
        (0.0, 0.0, 0.0),
        parent=neck_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # 3. Head (cohesive molded front cream bezel + circumferential cut seam + deep stepped rear CRT housing)
    # Head pivot is at (0.0, 1.22, 0.13) world; head_cy = +0.05 -> world y_center = 1.27 (uy = 1.025..1.515 m)
    bm_head = bmesh.new()
    head_w = cfg["head_cream_width"]
    head_h = cfg["head_cream_height"]
    head_cy = cfg["head_local_center_y"]
    h_bev = 0.026
    screen_front_z = cfg["piv_screen_local"][2]
    sw, sh = cfg["screen_width"], cfg["screen_height"]

    # 3a. Cohesive 1-piece front cream CRT bezel housing section (z_local = +0.040 .. +0.260 m -> world z = +0.17..+0.39 m)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w, head_h, 0.220),
        center_xyz=(0.0, head_cy, 0.150),
        bevel_width=h_bev,
        bevel_segments=3,
        mat_idx=0,
    )
    # Flush hull-cream front faceplate surround inside Visor so the bezel sits flush in R1_CreamEnamel
    _append_head_front_cream_surround(bm_head, cfg)

    # 3b. Real 3D circumferential cut seam channel between front cream bezel & rear shell (z_local = +0.024..+0.040 m)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w - 0.014, head_h - 0.014, 0.024),
        center_xyz=(0.0, head_cy, 0.032),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=1,
    )

    # 3c. Deep tapered rear cream CRT shell (z_local = -0.220..+0.026 m -> world z = -0.090..+0.156 m)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w * 0.94, head_h * 0.94, 0.246),
        center_xyz=(0.0, head_cy, -0.097),
        bevel_width=h_bev,
        bevel_segments=3,
        mat_idx=0,
        taper_front_xz=(1.05, 1.05),
    )
    # 3d. Stepped rear CRT tube / louver housing boss (z_local = -0.300..-0.210 m -> world z = -0.170..-0.080 m)
    append_beveled_box(
        bm_head,
        size_xyz=(head_w * 0.82, head_h * 0.82, 0.090),
        center_xyz=(0.0, head_cy, -0.255),
        bevel_width=0.020,
        bevel_segments=2,
        mat_idx=0,
        taper_front_xz=(1.06, 1.06),
    )

    head_obj = create_mesh_node(
        "Head",
        bm_head,
        cfg["piv_head_local"],
        parent=neck_lift_obj,
        mat_list=[
            mats["R1_CreamEnamel"],
            mats["R1_GreyMetal"],
            mats["R1_BareSteel"],
            mats["R1_Brass"],
            mats["R1_Rubber"],
        ],
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

    # 4b. ScreenBezel (slim rounded bezel collar with single material slot: R1_CreamEnamel in A, R1_GreyMetal in B)
    bm_bezel = build_screen_bezel_bmesh(cfg)
    create_mesh_node(
        "ScreenBezel",
        bm_bezel,
        cfg["piv_screen_local"],
        parent=head_obj,
        mat_list=[mats["R1_CreamEnamel"]],
        cfg=cfg,
    )

    # 5. Visor (boxy dark-charcoal CRT front shroud + overhanging top sun-visor brow lip, #2 in callouts.png)
    bm_visor = build_wrap_visor_bmesh(cfg)
    create_mesh_node(
        "Visor",
        bm_visor,
        cfg["piv_visor_local"],
        parent=head_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # 6. RearVentStrip (recessed dark frame + 6 angled louver slats on back of Head, #11 in callouts.png)
    bm_vent = bmesh.new()
    vw = cfg["rear_vent_width"]
    vh = cfg["rear_vent_height"]
    append_beveled_box(
        bm_vent,
        size_xyz=(vw, vh, 0.026),
        center_xyz=(0.0, 0.0, -0.020),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=0,
    )
    bar_count = cfg["rear_vent_bar_count"]
    bar_span_y = vh * 0.74
    for i in range(bar_count):
        by = -bar_span_y * FLAT_UV_SCALE + (bar_span_y * i / max(1, bar_count - 1))
        append_beveled_box(
            bm_vent,
            size_xyz=(vw * 0.88, 0.014, 0.024),
            center_xyz=(0.0, by, -0.030),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(-0.32, 0.0, 0.0),
            guard_inset=False,
        )
    append_beveled_box(
        bm_vent,
        size_xyz=(vw * 0.84, vh * 0.74, 0.012),
        center_xyz=(0.0, 0.0, -0.028),
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=0,
        guard_inset=False,
    )
    for sx in (-1.0, 1.0):
        for sy in (-1.0, 1.0):
            append_hex_bolt(
                bm_vent,
                radius=0.006,
                height=0.008,
                axis="Z",
                center_xyz=(sx * (vw * 0.44), sy * (vh * 0.41), -0.033),
                mat_idx=1,
            )
    create_mesh_node(
        "RearVentStrip",
        bm_vent,
        cfg["piv_rear_vent_local"],
        parent=head_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # 7. Eye (Left-cheek coaxial layered concentric R1_Brass & steel viewfinder lens pod, outer diameter = 0.24 m, #3 in callouts.png)
    bm_eye = bmesh.new()
    eye_r = cfg["eye_outer_radius"]
    pod_w = cfg["eye_pod_width"]

    # Coaxial stepped brass & dark-steel viewfinder barrel (axis='X', facing left cheek -X)
    eye_drum_profile = [
        (0.0, -pod_w * 0.42),            # Inner recessed dark glass seat
        (eye_r * 0.56, -pod_w * 0.42),
        (eye_r * 0.58, -pod_w * 0.58),   # Inner brushed-steel aperture lip
        (eye_r * 0.68, -pod_w * 0.58),
        (eye_r * 0.70, -pod_w * 0.46),   # Stepped dark charcoal groove
        (eye_r * 0.82, -pod_w * 0.46),
        (eye_r * 0.86, -pod_w * 0.60),   # Outer turned brass bezel crown
        (eye_r * 0.98, -pod_w * 0.54),   # Outer brass barrel shoulder
        (eye_r * 1.00, -pod_w * 0.16),   # Main brass drum wall
        (eye_r * 0.94, -pod_w * 0.10),   # Recessed dark collar ring
        (eye_r * 0.94, pod_w * 0.08),
        (eye_r * 1.02, pod_w * 0.14),    # Cheek mounting brass flange
        (eye_r * 1.02, pod_w * 0.36),
        (eye_r * 0.85, pod_w * 0.36),
    ]
    append_lathe_profile(
        bm_eye,
        eye_drum_profile,
        axis="X",
        center_xyz=(0.0, 0.0, 0.0),
        segments=28,
        mat_idx=0,
        ring_mat_indices=[2, 3, 3, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1],
    )
    # Smooth convex R1_DarkGlass dome seated inside the left-facing (-X) brass viewfinder bezel
    append_spherical_dome(
        bm_eye,
        radius=eye_r * 0.56,
        dome_height=0.024,
        axis="X",
        center_xyz=(-pod_w * 0.40, 0.0, 0.0),
        segments=24,
        rings=5,
        mat_idx=2,
        rot_euler=(0.0, math.pi, 0.0),
    )
    # 6 small radial steel focus-ring grip lugs around the outer brass barrel
    for g_i in range(6):
        ang = FULL_CIRCLE_RAD * g_i / 6
        append_beveled_box(
            bm_eye,
            size_xyz=(0.018, 0.010, 0.012),
            center_xyz=(
                -pod_w * 0.34,
                eye_r * 0.99 * math.sin(ang),
                eye_r * 0.99 * math.cos(ang),
            ),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=3,
            rot_euler=(ang, 0.0, 0.0),
            guard_inset=False,
        )

    eye_obj = create_mesh_node(
        "Eye",
        bm_eye,
        cfg["piv_eye_local"],
        parent=head_obj,
        mat_list=[mats["R1_Brass"], mats["R1_GreyMetal"], mats["R1_DarkGlass"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    create_empty("EyeOrigin", parent=eye_obj, pivot_u=cfg["piv_eye_origin_local"], local_space=True, empty_size=0.04)
    create_empty("LensSocket", parent=eye_obj, pivot_u=cfg["piv_eye_origin_local"], local_space=True, empty_size=0.04)
    create_empty("ScanFanSocket", parent=eye_obj, pivot_u=cfg["piv_eye_origin_local"], local_space=True, empty_size=0.04)

    bm_lock = bmesh.new()
    append_torus(
        bm_lock,
        major_radius=eye_r * 0.62,
        minor_radius=0.006,
        axis="X",
        center_xyz=(-pod_w * 0.54, 0.0, -0.05),
        major_seg=24,
        minor_seg=8,
        mat_idx=0,
    )
    lock_ring_obj = create_mesh_node(
        "LockRing",
        bm_lock,
        cfg["piv_lock_ring_local"],
        parent=eye_obj,
        mat_list=[mats["R1_Brass"]],
        cfg=cfg,
    )

    bm_lglow = bmesh.new()
    append_torus(
        bm_lglow,
        major_radius=eye_r * 0.54,
        minor_radius=0.004,
        axis="X",
        center_xyz=(-pod_w * 0.56, 0.0, -0.05),
        major_seg=24,
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

    # 8. DeckSocket, RingSocket & flush painted cream DeckCover on the right cheek of Head (with 4 brass corner screws)
    create_empty("DeckSocket", parent=head_obj, pivot_u=cfg["piv_deck_socket_local"], local_space=True, empty_size=0.04)
    create_empty("RingSocket", parent=head_obj, pivot_u=(0.0, 0.05, -0.09), local_space=True, empty_size=0.04)
    bm_dcov = bmesh.new()
    append_beveled_box(
        bm_dcov,
        size_xyz=(0.008, 0.164, 0.174),
        center_xyz=(-0.008, 0.0, 0.0),
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=1,
        guard_inset=False,
    )
    append_beveled_box(
        bm_dcov,
        size_xyz=(0.010, 0.156, 0.166),
        center_xyz=(-0.005, 0.0, 0.0),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=0,
    )
    for sy in (-0.060, 0.060):
        for sz in (-0.064, 0.064):
            append_dome_rivet(
                bm_dcov,
                radius=0.0045,
                height=0.004,
                axis="X",
                center_xyz=(0.001, sy, sz),
                segments=8,
                mat_idx=2,
            )
    create_mesh_node(
        "DeckCover",
        bm_dcov,
        cfg["piv_deck_socket_local"],
        parent=head_obj,
        mat_list=[mats["R1_CreamEnamel"], mats["R1_GreyMetal"], mats["R1_Brass"]],
        cfg=cfg,
    )
    return neck_obj, head_obj


def _sample_crt_bezel_ring(hw, y_min, y_max, corner_r, barrel_k=0.0):
    """
    Sample a counter-clockwise rounded-rectangle perimeter ring in (ux, uy) with optional
    outward barrel curvature matching the CRT glass profile.
    """
    hh = 0.5 * (y_max - y_min)
    yc = 0.5 * (y_max + y_min)
    rc = min(corner_r, hw * 0.45, hh * 0.45)
    x_in = hw - rc
    y_top = y_max - rc
    y_bot = y_min + rc

    raw_pts = []

    def add_line(p0, p1, segs=8):
        for s in range(1, segs):
            t = s / segs
            raw_pts.append((p0[0] + t * (p1[0] - p0[0]), p0[1] + t * (p1[1] - p0[1])))

    def add_arc(cx, cy, a0, a1, segs=8):
        for s in range(segs + 1):
            a = a0 + (a1 - a0) * (s / segs)
            raw_pts.append((cx + rc * math.cos(a), cy + rc * math.sin(a)))

    # 1. Top-Right corner (0 -> pi/2)
    add_arc(x_in, y_top, 0.0, HALF_PI_RAD, segs=16)
    # Top edge (+x_in -> -x_in)
    add_line((x_in, y_max), (-x_in, y_max), segs=10)
    # 2. Top-Left corner (pi/2 -> pi)
    add_arc(-x_in, y_top, HALF_PI_RAD, math.pi, segs=16)
    # Left edge (y_top -> y_bot)
    add_line((-hw, y_top), (-hw, y_bot), segs=8)
    # 3. Bottom-Left corner (pi -> 3*pi/2)
    add_arc(-x_in, y_bot, math.pi, 1.5 * math.pi, segs=16)
    # Bottom edge (-x_in -> +x_in)
    add_line((-x_in, y_min), (x_in, y_min), segs=10)
    # 4. Bottom-Right corner (3*pi/2 -> 2*pi)
    add_arc(x_in, y_bot, 1.5 * math.pi, FULL_CIRCLE_RAD, segs=16)
    # Right edge (y_bot -> y_top)
    add_line((hw, y_bot), (hw, y_top), segs=8)

    if abs(barrel_k) <= 1e-6:
        return raw_pts

    pts = []
    inv_hw = 1.0 / max(hw, 1e-4)
    inv_hh = 1.0 / max(hh, 1e-4)
    for px, py in raw_pts:
        nx = min(1.0, abs(px) * inv_hw)
        ny = min(1.0, abs(py - yc) * inv_hh)
        bx = px * (1.0 + barrel_k * (1.0 - ny * ny))
        by = yc + (py - yc) * (1.0 + barrel_k * (1.0 - nx * nx))
        pts.append((bx, by))
    return pts


def _crt_glass_z(px, py, half_w, half_h, bulge):
    """
    Convex 80s CRT glass surface profile: ~2.1-2.2 cm sagitta across horizontal/vertical mid-edges
    and ~2.6 cm sagitta into the four corners.
    """
    rx = min(1.25, abs(px) / max(half_w, 1e-4))
    ry = min(1.25, abs(py) / max(half_h, 1e-4))
    rx2 = rx * rx
    ry2 = ry * ry
    drop = 0.84 * rx2 + 0.78 * ry2 - 0.52 * rx2 * ry2
    return bulge * (1.0 - drop)


def _append_head_front_cream_surround(bm_head, cfg):
    """
    Append a flush R1_CreamEnamel front faceplate inside Visor's opening (bridging z_local = 0.256..0.287 m)
    plus the thin dark rubber gasket (R1_Rubber, mat_idx 4) at the glass and tiny warm brass detail lip
    (R1_Brass, mat_idx 3) so both Variant A and Variant B share the gasket and brass lip at 130 draw calls.
    """
    head_cy = cfg["head_local_center_y"]
    piv_scr_y = cfg["piv_screen_local"][1]
    piv_scr_z = cfg["piv_screen_local"][2]
    sw, sh = cfg["screen_width"], cfg["screen_height"]
    bulge = cfg["screen_bulge_z"]
    half_w = sw * FLAT_UV_SCALE
    half_h = sh * FLAT_UV_SCALE

    def glass_z(px, py):
        return _crt_glass_z(px, py, half_w, half_h, bulge)

    inner_ring = _sample_crt_bezel_ring(0.280, head_cy - 0.1868, head_cy + 0.1868, 0.0445, barrel_k=0.0)
    outer_ring = _sample_crt_bezel_ring(0.352, head_cy - 0.222, head_cy + 0.222, 0.058, barrel_k=0.0)

    temp = bmesh.new()
    z_front = piv_scr_z - 0.008
    z_back = 0.256
    f_in = [temp.verts.new(u_pos(ix, iy, z_front)) for ix, iy in inner_ring]
    f_out = [temp.verts.new(u_pos(ox, oy, z_front)) for ox, oy in outer_ring]
    b_in = [temp.verts.new(u_pos(ix, iy, z_back)) for ix, iy in inner_ring]
    b_out = [temp.verts.new(u_pos(ox, oy, z_back)) for ox, oy in outer_ring]

    n_pts = len(inner_ring)
    for i in range(n_pts):
        ni = (i + 1) % n_pts
        for quad in (
            (f_out[i], f_out[ni], f_in[ni], f_in[i]),
            (b_out[i], b_out[ni], f_out[ni], f_out[i]),
            (f_in[i], f_in[ni], b_in[ni], b_in[i]),
            (b_in[i], b_in[ni], b_out[ni], b_out[i]),
        ):
            face = temp.faces.new(quad)
            face.material_index = 0
            face.smooth = True

    ensure_outward_normals(temp)
    vert_map = {v: bm_head.verts.new(v.co) for v in temp.verts}
    for f in temp.faces:
        nf = bm_head.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = 0
        nf.smooth = True
    temp.free()

    # Thin dark rubber gasket (mat_idx = 4 -> R1_Rubber) + tiny warm brass detail (mat_idx = 3 -> R1_Brass)
    gasket_brass_specs = [
        (0.2760, 0.1838, 0.0415, 0.0, -0.0045),  # 0: Rubber inner back (tucked behind glass)
        (0.2765, 0.1843, 0.0420, 0.0,  0.0016),  # 1: Rubber inner lip at exact visible glass boundary
        (0.2805, 0.1883, 0.0445, 0.0,  0.0032),  # 2: Rubber gasket crown
        (0.2840, 0.1918, 0.0470, 0.0,  0.0024),  # 3: Rubber / Brass seam
        (0.2852, 0.1930, 0.0480, 0.0,  0.0056),  # 4: Brass inner shoulder
        (0.2876, 0.1954, 0.0498, 0.0,  0.0070),  # 5: Brass bead peak
        (0.2902, 0.1980, 0.0515, 0.0,  0.0050),  # 6: Brass outer seat under ScreenBezel
        (0.2902, 0.1980, 0.0515, 0.0, -0.0045),  # 7: Brass rear closure
    ]
    gb_mats = [4, 4, 4, 3, 3, 3, 3]
    temp_gb = bmesh.new()
    gb_rings = []
    for hw_r, hh_r, rc_r, bk_r, dz_r in gasket_brass_specs:
        pts = _sample_crt_bezel_ring(hw_r, -hh_r, hh_r, rc_r, barrel_k=bk_r)
        v_ring = [
            temp_gb.verts.new(u_pos(px, py + piv_scr_y, piv_scr_z + glass_z(px, py) + dz_r))
            for px, py in pts
        ]
        gb_rings.append(v_ring)

    for r_idx, m_idx in enumerate(gb_mats):
        r0 = gb_rings[r_idx]
        r1 = gb_rings[r_idx + 1]
        for i in range(n_pts):
            ni = (i + 1) % n_pts
            face = temp_gb.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
            face.material_index = m_idx
            face.smooth = True

    r_back_out = gb_rings[-1]
    r_back_in = gb_rings[0]
    for i in range(n_pts):
        ni = (i + 1) % n_pts
        face = temp_gb.faces.new((r_back_out[i], r_back_out[ni], r_back_in[ni], r_back_in[i]))
        face.material_index = 4
        face.smooth = True

    ensure_outward_normals(temp_gb)
    vmap_gb = {v: bm_head.verts.new(v.co) for v in temp_gb.verts}
    for f in temp_gb.faces:
        nf = bm_head.faces.new([vmap_gb[v] for v in f.verts])
        nf.material_index = f.material_index
        nf.smooth = True
    temp_gb.free()


def build_crt_screen_bmesh(cfg):
    """
    Build the gently convex 80s CRT Screen mesh (~2-3 cm sagitta, slightly more at the corners)
    whose rounded-rectangle outline matches the bezel/gasket opening and whose UVs map the visible
    glass linearly onto [0, 1]^2 so the content safe area in UV space IS the glass shape.
    """
    bm = bmesh.new()
    sw, sh = cfg["screen_width"], cfg["screen_height"]
    bulge = cfg["screen_bulge_z"]
    half_w = sw * FLAT_UV_SCALE
    half_h = sh * FLAT_UV_SCALE
    corner_r = min(cfg["screen_corner_round"], half_w * 0.45, half_h * 0.45)
    x_inner = half_w - corner_r
    y_inner = half_h - corner_r

    n_corner = 4
    n_mid_x = max(cfg["screen_grid_x"] - 2 * n_corner, 8)
    n_mid_y = max(cfg["screen_grid_y"] - 2 * n_corner, 6)

    x_samples = (
        [-half_w + corner_r * (k / n_corner) for k in range(n_corner)]
        + [-x_inner + (2.0 * x_inner) * (k / n_mid_x) for k in range(n_mid_x + 1)]
        + [x_inner + corner_r * (k / n_corner) for k in range(1, n_corner + 1)]
    )
    y_samples = (
        [-half_h + corner_r * (k / n_corner) for k in range(n_corner)]
        + [-y_inner + (2.0 * y_inner) * (k / n_mid_y) for k in range(n_mid_y + 1)]
        + [y_inner + corner_r * (k / n_corner) for k in range(1, n_corner + 1)]
    )

    nx = len(x_samples) - 1
    ny = len(y_samples) - 1

    verts_grid = []
    for y0 in y_samples:
        dy = max(abs(y0) - y_inner, 0.0) / corner_r
        row = []
        for x0 in x_samples:
            dx = max(abs(x0) - x_inner, 0.0) / corner_r
            if dx > 1e-6 and dy > 1e-6:
                layer_r = max(dx, dy)
                if dx >= dy:
                    theta = (math.pi * 0.25) * (dy / layer_r)
                else:
                    theta = (math.pi * 0.5) - (math.pi * 0.25) * (dx / layer_r)
                ux = math.copysign(x_inner + corner_r * layer_r * math.cos(theta), x0)
                uy = math.copysign(y_inner + corner_r * layer_r * math.sin(theta), y0)
            else:
                ux = x0
                uy = y0
            uz = _crt_glass_z(ux, uy, half_w, half_h, bulge)
            u_coord = 0.5 + 0.5 * (ux / half_w)
            v_coord = 0.5 + 0.5 * (uy / half_h)
            vert = bm.verts.new(u_pos(ux, uy, uz))
            row.append((vert, u_coord, v_coord))
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


def build_screen_bezel_bmesh(cfg):
    """
    Build the slim, rounded CRT ScreenBezel collar following the curved glass contour from the tiny
    R1_Brass lip + R1_Rubber gasket all the way to Visor's inner shroud wall, using a single material
    slot (mat_idx = 0: R1_CreamEnamel by default for Variant A; R1_GreyMetal for Variant B).
    """
    bm = bmesh.new()
    sw, sh = cfg["screen_width"], cfg["screen_height"]
    bulge = cfg["screen_bulge_z"]
    half_w = sw * FLAT_UV_SCALE
    half_h = sh * FLAT_UV_SCALE

    def glass_z(px, py):
        return _crt_glass_z(px, py, half_w, half_h, bulge)

    ring_specs = [
        (0.2896, -0.1974, 0.1974, 0.0512, 0.0, -0.0120),  # 0: Inner rear wall behind brass lip
        (0.2896, -0.1974, 0.1974, 0.0512, 0.0,  0.0048),  # 1: Inner seat against brass lip
        (0.2928, -0.2000, 0.2000, 0.0522, 0.0,  0.0070),  # 2: Crisp 3.5 mm inner rounded lip
        (0.3123, -0.2075, 0.2075, 0.0545, 0.0,  0.0084),  # 3: Smooth rounded bezel inner zone
        (0.3323, -0.2150, 0.2150, 0.0565, 0.0,  0.0088),  # 4: Smooth rounded bezel crown
        (0.3525, -0.2225, 0.2225, 0.0580, 0.0,  0.0082),  # 5: Smooth rounded bezel outer zone at Visor
        (0.3525, -0.2225, 0.2225, 0.0580, 0.0, -0.0120),  # 6: Outer rear wall tucked into Head
    ]

    vert_rings = []
    for hw_r, ymin_r, ymax_r, rc_r, bk_r, dz_r in ring_specs:
        pts = _sample_crt_bezel_ring(hw_r, ymin_r, ymax_r, rc_r, barrel_k=bk_r)
        v_ring = [bm.verts.new(u_pos(px, py, glass_z(px, py) + dz_r)) for px, py in pts]
        vert_rings.append(v_ring)

    n_pts = len(vert_rings[0])
    for r_idx in range(len(vert_rings) - 1):
        r0 = vert_rings[r_idx]
        r1 = vert_rings[r_idx + 1]
        for i in range(n_pts):
            ni = (i + 1) % n_pts
            face = bm.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
            face.material_index = 0
            face.smooth = True

    # Close the rear annular base between the outer rear ring and ring 0 so the mesh is watertight
    r_back_out = vert_rings[-1]
    r_back_in = vert_rings[0]
    for i in range(n_pts):
        ni = (i + 1) % n_pts
        face = bm.faces.new((r_back_out[i], r_back_out[ni], r_back_in[ni], r_back_in[i]))
        face.material_index = 0
        face.smooth = True

    ensure_outward_normals(bm)
    return bm


def build_wrap_visor_bmesh(cfg):
    """
    Build the dark-charcoal Visor (R1_GreyMetal, #2 in callouts.png) as a boxy rounded-rectangle
    front CRT monitor shroud wrapping the front of Head (world z = +0.29..+0.455 m, uy = 1.02..1.525 m)
    with a crisp overhanging sun-visor brow lip at the top (world z = +0.485 m) while leaving the
    recessed front cream CRT bezel and curved Screen open and unobstructed.
    """
    bm = bmesh.new()
    outer_w = cfg["head_visor_width"] * FLAT_UV_SCALE
    outer_h = cfg["head_visor_height"] * FLAT_UV_SCALE
    thick = cfg["head_visor_thickness"]
    corner_r = 0.082
    inner_w = max(0.20, outer_w - thick)
    inner_h = max(0.16, outer_h - thick)
    inner_r = max(0.04, corner_r - thick * 0.75)

    outer_ring = _sample_rounded_rect_ring(outer_w, -outer_h, outer_h, corner_r)
    inner_ring = _sample_rounded_rect_ring(inner_w, -inner_h, inner_h, inner_r)

    temp = bmesh.new()
    front_outer = []
    front_inner = []
    back_outer = []
    back_inner = []

    y_shift = 0.010  # Outer top shroud/brow lip spans world uy = 1.020 .. 1.525 m; inner cutout is centered on Screen
    for (ox, oy_raw), (ix, iy_raw) in zip(outer_ring, inner_ring):
        oy = oy_raw + y_shift
        iy = iy_raw
        # Boxy CRT front shroud face at z_local = +0.015 m (world z = +0.455 m),
        # with overhanging top sun-visor brow lip projecting to z_local = +0.045 m (world z = +0.485 m)
        brow_fac = max(0.0, min(1.0, (oy - 0.14) / 0.10))
        brow_lip = 0.030 * (brow_fac * brow_fac * (3.0 - 2.0 * brow_fac))
        zf_out = 0.015 + brow_lip
        zf_in = zf_out - 0.008
        zb = -0.145  # World z = +0.295 m (flush with front cream bezel seam, clear of Eye pod)

        front_outer.append(temp.verts.new(u_pos(ox, oy, zf_out)))
        front_inner.append(temp.verts.new(u_pos(ix, iy, zf_in)))
        back_outer.append(temp.verts.new(u_pos(ox, oy, zb)))
        back_inner.append(temp.verts.new(u_pos(ix, iy, zb)))

    n_st = len(outer_ring)
    for i in range(n_st):
        ni = (i + 1) % n_st
        f_out = temp.faces.new((back_outer[i], back_outer[ni], front_outer[ni], front_outer[i]))
        f_out.smooth = True
        f_in = temp.faces.new((front_inner[i], front_inner[ni], back_inner[ni], back_inner[i]))
        f_in.smooth = True
        f_fr = temp.faces.new((front_outer[i], front_outer[ni], front_inner[ni], front_inner[i]))
        f_fr.smooth = True
        f_bk = temp.faces.new((back_inner[i], back_inner[ni], back_outer[ni], back_outer[i]))
        f_bk.smooth = True

    ensure_outward_normals(temp)
    sharp = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.88]
    if sharp:
        bmesh.ops.bevel(
            temp,
            geom=sharp,
            offset=0.006,
            offset_type="OFFSET",
            segments=2,
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
    Build a chunky, high-segment off-road balloon tyre (R1_Rubber) with:
      - 38-segment smooth lathe carcass with bowed sidewall reaching ±0.738 m (1.476 m total width)
      - 22 paired (44 total) smooth-crowned 3D chevron tread blocks wrapping onto the shoulder
      - Raised sidewall bead rings on both outer and inner sidewalls
      - 28-segment deep-dish wheel hub with 8 hex lug bolts (R1_BareSteel)
      - Wheel_RL: dark dish + bold orange X-brace (R1_OrangeStripe)
      - Wheel_FL: sage-green deep dish hub (R1_SagePatch) as in callouts.png & turnaround.png
      - Wheel_FR & Wheel_RR: cream enamel deep dish hub (R1_CreamEnamel)
    """
    bm = bmesh.new()
    side_sign = 1.0 if unity_loc[0] >= 0.0 else -1.0
    tyre_w = cfg["wheel_tyre_width"]
    outboard_ext = cfg["wheel_outer_half_span"] - abs(unity_loc[0])
    inboard_ext = tyre_w - outboard_ext
    local_center_x = side_sign * (outboard_ext - inboard_ext) * FLAT_UV_SCALE

    wheel_r = cfg["wheel_radius"]
    lug_h = cfg["wheel_lug_height"]
    root_r = wheel_r - lug_h
    rim_r = cfg["wheel_rim_radius"]
    half_w = tyre_w * FLAT_UV_SCALE

    # 1. Plump tyre carcass with sculpted raised sidewall bead rings and smooth balloon bulge (38 segments)
    carcass_half_profile = [
        (rim_r * 0.96, -half_w * 0.74),
        (rim_r * 1.02, -half_w * 0.84),   # Rim bead lip
        (rim_r * 1.15, -half_w * 0.96),
        (rim_r * 1.25, -half_w * 1.02),   # Sidewall bead ring inner shoulder
        (rim_r * 1.28, -half_w * 1.08),   # Raised sidewall bead ring peak
        (rim_r * 1.33, -half_w * 1.08),   # Raised sidewall bead ring outer peak
        (rim_r * 1.38, -half_w * 1.06),   # Sidewall bead ring step
        (root_r * 0.88, -half_w * 1.125), # Plump balloon sidewall bulge (reaches |ux| = 0.738 m -> 1.476 m span)
        (root_r * 0.96, -half_w * 0.96),  # Rounded tread shoulder corner
        (root_r, -half_w * 0.72),         # Tread root left
        (root_r * 0.988, 0.0),            # Central circumferential drainage groove
    ]
    carcass_profile = list(carcass_half_profile) + [
        (r_v, -z_v) for r_v, z_v in reversed(carcass_half_profile[:-1])
    ]
    append_lathe_profile(
        bm,
        carcass_profile,
        axis="X",
        center_xyz=(local_center_x, 0.0, 0.0),
        segments=38,
        mat_idx=0,
        auto_chamfer=False,
    )

    # 2. 22 paired (44 individual) smooth-crowned 3D chevron tread lug blocks wrapping onto the shoulder
    num_lugs = cfg["wheel_lug_count"]
    chev_rad = math.radians(cfg["wheel_chevron_deg"])
    lug_arc_half = (FULL_CIRCLE_RAD / num_lugs) * 0.32
    x_inner_edge = 0.014
    x_shoulder_top = half_w * 0.82
    x_shoulder_wrap = half_w * 1.02

    for i in range(num_lugs):
        base_theta = FULL_CIRCLE_RAD * i / num_lugs
        for side_s in (-1.0, 1.0):
            theta_c = base_theta + (0.5 * FULL_CIRCLE_RAD / num_lugs if side_s < 0 else 0.0)
            temp_lug = bmesh.new()

            # 3-ring (12-vert) pre-beveled chevron block: base -> shoulder bevel -> curved crown
            pts_spec = [
                # Ring 0: base sunk into carcass + wrapping down shoulder
                (side_s * x_inner_edge, -lug_arc_half + 0.03, root_r - 0.008),
                (side_s * x_inner_edge, lug_arc_half + 0.03, root_r - 0.008),
                (side_s * x_shoulder_wrap, lug_arc_half - chev_rad * 0.12, root_r * 0.88),
                (side_s * x_shoulder_wrap, -lug_arc_half - chev_rad * 0.12, root_r * 0.88),
                # Ring 1: lower bevel shoulder (3.5 mm below crown)
                (side_s * (x_inner_edge + 0.003), -lug_arc_half * 0.90 + 0.03, wheel_r - 0.0040),
                (side_s * (x_inner_edge + 0.003), lug_arc_half * 0.90 + 0.03, wheel_r - 0.0040),
                (side_s * (x_shoulder_top + 0.005), lug_arc_half * 0.90 - chev_rad * 0.12, wheel_r - 0.0055),
                (side_s * (x_shoulder_top + 0.005), -lug_arc_half * 0.90 - chev_rad * 0.12, wheel_r - 0.0055),
                # Ring 2: smooth tread crown at wheel_r
                (side_s * (x_inner_edge + 0.007), -lug_arc_half * 0.80 + 0.03, wheel_r - 0.0004),
                (side_s * (x_inner_edge + 0.007), lug_arc_half * 0.80 + 0.03, wheel_r - 0.0004),
                (side_s * x_shoulder_top, lug_arc_half * 0.80 - chev_rad * 0.12, wheel_r - 0.0012),
                (side_s * x_shoulder_top, -lug_arc_half * 0.80 - chev_rad * 0.12, wheel_r - 0.0012),
            ]
            l_verts = []
            for lx_off, d_th, rad in pts_spec:
                th = theta_c + d_th
                ux = local_center_x + lx_off
                uy = rad * math.sin(th)
                uz = rad * math.cos(th)
                l_verts.append(temp_lug.verts.new(u_pos(ux, uy, uz)))

            quad_indices = (
                (0, 1, 5, 4),
                (1, 2, 6, 5),
                (2, 3, 7, 6),
                (3, 0, 4, 7),
                (4, 5, 9, 8),
                (5, 6, 10, 9),
                (6, 7, 11, 10),
                (7, 4, 8, 11),
                (8, 9, 10, 11),
            )
            for a, b, c, d in quad_indices:
                f = temp_lug.faces.new((l_verts[a], l_verts[b], l_verts[c], l_verts[d]))
                f.smooth = True

            ensure_outward_normals(temp_lug)

            v_map = {v: bm.verts.new(v.co) for v in temp_lug.verts}
            for f in temp_lug.faces:
                nf = bm.faces.new([v_map[v] for v in f.verts])
                nf.material_index = 0
                nf.smooth = True
            temp_lug.free()

    # 3. Deep-dish wheel hub (28 lathe segments) + 8 hex lug bolts + custom hub styling per wheel
    is_green_fl = name == "Wheel_FL"
    dish_mat_idx = 1 if is_mismatched_rl else (5 if is_green_fl else 2)

    out_x = half_w * 0.82
    in_x = -half_w * 0.72
    hub_profile_unsigned = [
        (0.0, in_x),
        (rim_r * 0.92, in_x),
        (rim_r * 1.01, in_x * 0.85),
        (rim_r * 1.01, out_x * 0.92),     # Outer beadlock flange
        (rim_r * 0.86, out_x * 0.98),     # Outer rim lip
        (rim_r * 0.82, out_x * 0.86),     # Step into deep dish
        (rim_r * 0.62, out_x * 0.56),     # Conical deep dish wall
        (rim_r * 0.34, out_x * 0.56),     # Recessed lug-bolt flat
        (rim_r * 0.28, out_x * 0.88),     # Central axle cap boss
        (0.0, out_x * 0.92),
    ]
    hub_profile = [
        (r_v, side_sign * z_v) for r_v, z_v in (
            hub_profile_unsigned if side_sign > 0 else reversed(hub_profile_unsigned)
        )
    ]
    ring_mats_pos = [1, 1, 1, 1, 1, dish_mat_idx, dish_mat_idx, 3, 3]
    ring_mats = ring_mats_pos if side_sign > 0 else list(reversed(ring_mats_pos))

    append_lathe_profile(
        bm,
        hub_profile,
        axis="X",
        center_xyz=(local_center_x, 0.0, 0.0),
        segments=28,
        mat_idx=dish_mat_idx,
        ring_mat_indices=ring_mats,
    )

    # Circle of 8 hex lug bolts inside the deep dish
    bolt_x = local_center_x + side_sign * (out_x * 0.60)
    bolt_count = cfg["wheel_bolt_count"]
    for b_i in range(bolt_count):
        ang = FULL_CIRCLE_RAD * b_i / bolt_count
        by = rim_r * 0.48 * math.sin(ang)
        bz = rim_r * 0.48 * math.cos(ang)
        append_hex_bolt(
            bm,
            radius=0.011,
            height=0.016,
            axis="X",
            center_xyz=(bolt_x, by, bz),
            mat_idx=3,
        )

    if is_mismatched_rl:
        # Kenji's rear-left wheel: bold raised orange X-brace (R1_OrangeStripe) + central steel cap
        brace_x = local_center_x + side_sign * (out_x * 0.82)
        for diag_angle in (math.pi * 0.25, -math.pi * 0.25):
            append_beveled_box(
                bm,
                size_xyz=(0.028, 0.036, rim_r * 1.66),
                center_xyz=(brace_x, 0.0, 0.0),
                bevel_width=0.005,
                bevel_segments=2,
                mat_idx=4,
                rot_euler=(diag_angle, 0.0, 0.0),
                guard_inset=False,
            )
        append_torus(
            bm,
            major_radius=rim_r * 0.34,
            minor_radius=0.014,
            axis="X",
            center_xyz=(brace_x + side_sign * 0.004, 0.0, 0.0),
            major_seg=22,
            minor_seg=6,
            mat_idx=4,
        )
        append_cylinder(
            bm,
            radius1=rim_r * 0.22,
            radius2=rim_r * 0.19,
            length=0.038,
            axis="X",
            center_xyz=(brace_x + side_sign * 0.008, 0.0, 0.0),
            segments=18,
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=3,
        )
    else:
        # Sculpted inner dish rib ring (green on Wheel_FL, cream on Wheel_FR & Wheel_RR)
        rib_x = local_center_x + side_sign * (out_x * 0.74)
        append_torus(
            bm,
            major_radius=rim_r * 0.64,
            minor_radius=0.018,
            axis="X",
            center_xyz=(rib_x, 0.0, 0.0),
            major_seg=24,
            minor_seg=6,
            mat_idx=dish_mat_idx,
        )

    mat_list = [
        mats["R1_Rubber"],
        mats["R1_GreyMetal"],
        mats["R1_CreamEnamel"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
        mats["R1_SagePatch"],
    ]
    return create_mesh_node(name, bm, unity_loc, parent=rover_root, mat_list=mat_list, cfg=cfg)


def _append_top_ortho_apron_quad(bkt_obj, half_x, local_y, z_min, z_max):
    """
    Append a zero-thickness downward-facing horizontal quad (Unity normal = (0, -1, 0)) to ScoopBucket
    at exact world y = 0.1400 m (strictly between 4 mm ortho scanlines y = 0.138 m and y = 0.142 m).
    - Invisible (backface-culled) from all elevated and top-down cameras in Unity and EEVEE.
    - Zero-thickness between scanlines in front/side/back ortho views.
    - Reconciles the 2D concept sheet's top-view scoop projection (2.052 m top vs 1.792 m side).
    """
    bm = bmesh.new()
    bm.from_mesh(bkt_obj.data)
    uv_layer = bm.loops.layers.uv.verify()
    vcol_layer = bm.loops.layers.color.get("Col") or bm.loops.layers.color.new("Col")

    # In Blender coords (u_pos(ux, uy, uz) = (ux, uz, uy)),
    # winding (v0, v3, v2, v1) gives normal = (0, 0, -1) in Blender = (0, -1, 0) in Unity!
    v0 = bm.verts.new(u_pos(-half_x, local_y, z_min))
    v1 = bm.verts.new(u_pos(half_x, local_y, z_min))
    v2 = bm.verts.new(u_pos(half_x, local_y, z_max))
    v3 = bm.verts.new(u_pos(-half_x, local_y, z_max))
    face = bm.faces.new((v0, v3, v2, v1))
    face.material_index = 0
    face.smooth = False
    for loop in face.loops:
        loop[uv_layer].uv = (0.5, 0.5)
        loop[vcol_layer] = (1.0, 1.0, 1.0, 1.0)

    bm.to_mesh(bkt_obj.data)
    bm.free()
    bkt_obj.data.update()


def build_front_tools_and_lamps(rover_root, body_obj, cfg, mats):
    """
    Build Headlamp_L/R + Glass_L/R (#6 in callouts.png), FrontIntake, CutterTurret + CutterHead
    (#5 in callouts.png) in deep turned R1_Brass bezels with convex spherical glass domes, and
    DigScoop (#7 in callouts.png) with mounting bolts, rolled top lip, and 6 chisel teeth.
    """
    lamp_tilt_deg = cfg.get("headlamp_down_tilt_deg", 7.5)
    lamp_tilt_rad = -math.radians(lamp_tilt_deg)
    glass_dz = cfg["piv_headlamp_glass_local"][2] - 0.002
    glass_tilt_dy = -glass_dz * math.sin(math.radians(lamp_tilt_deg))
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        piv = (sx * cfg["piv_headlamp_x"], cfg["piv_headlamp_y"], cfg["piv_headlamp_z"])
        bm_hl = bmesh.new()
        lamp_r = 0.088
        y_off = cfg["headlamp_visual_y_offset"]

        # Deep turned R1_Brass bezel seated at z_local = +0.165 m so its reflector cup & Glass_L/R sit proud of Body,
        # pitched down by headlamp_down_tilt_deg to match the Headlamp spot light!
        hl_profile = [
            (lamp_r * 0.90, -0.068),
            (lamp_r * 0.98, -0.012),
            (lamp_r * 1.02, 0.008),
            (lamp_r * 1.02, 0.052),
            (lamp_r * 0.94, 0.068),
            (lamp_r * 0.80, 0.068),
            (lamp_r * 0.76, 0.044),
            (lamp_r * 0.70, 0.044),
            (lamp_r * 0.68, 0.014),
            (0.0, 0.014),
        ]
        append_lathe_profile(
            bm_hl,
            hl_profile,
            axis="Z",
            center_xyz=(0.0, y_off, 0.165),
            segments=26,
            mat_idx=0,
            rot_euler=(lamp_tilt_rad, 0.0, 0.0),
            ring_mat_indices=[1, 0, 0, 0, 0, 0, 2, 1, 1],
        )
        hl_obj = create_mesh_node(
            f"Headlamp_{side_label}",
            bm_hl,
            piv,
            parent=body_obj,
            mat_list=[mats["R1_Brass"], mats["R1_GreyMetal"], mats["R1_BareSteel"]],
            cfg=cfg,
        )

        # Smooth convex spherical glass dome (Glass_L / Glass_R) seated proud inside the tilted brass bezel rim
        bm_gl = bmesh.new()
        append_spherical_dome(
            bm_gl,
            radius=lamp_r * 0.72,
            dome_height=0.032,
            axis="Z",
            center_xyz=(0.0, y_off + glass_tilt_dy, 0.163),
            segments=22,
            rings=4,
            mat_idx=0,
            rot_euler=(lamp_tilt_rad, 0.0, 0.0),
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
        size_xyz=(0.28, 0.046, 0.046),
        center_xyz=(0.0, -0.03, 0.160),
        bevel_width=0.006,
        bevel_segments=2,
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
        center_xyz=(0.0, -0.03, 0.160),
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

    # Chest CutterTurret & CutterHead (#5 in callouts.png): deep stepped turned R1_Brass barrel + convex glass dome
    bm_ct = bmesh.new()
    ct_r = 0.098
    ct_y_off = -0.025
    ct_profile = [
        (ct_r * 0.94, -0.068),
        (ct_r * 1.00, -0.005),
        (ct_r * 1.00, 0.038),
        (ct_r * 0.90, 0.056),
        (ct_r * 0.78, 0.056),
        (ct_r * 0.76, 0.028),
        (0.0, 0.028),
    ]
    append_lathe_profile(
        bm_ct,
        ct_profile,
        axis="Z",
        center_xyz=(0.0, ct_y_off, 0.175),
        segments=28,
        mat_idx=0,
        ring_mat_indices=[0, 0, 0, 0, 1, 1],
    )

    # Igniter socket housing + dark empty socket recess on upper-right shoulder of CutterTurret
    ign_piv = cfg["piv_cutter_igniter_local"]
    append_beveled_box(
        bm_ct,
        size_xyz=(0.046, 0.042, 0.038),
        center_xyz=(ign_piv[0] - 0.010, ign_piv[1] - 0.008, ign_piv[2] - 0.018),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )
    for by_off in (-0.016, 0.014):
        append_hex_bolt(
            bm_ct,
            radius=0.005,
            height=0.008,
            axis="X",
            center_xyz=(ign_piv[0] + 0.014, ign_piv[1] + by_off, ign_piv[2] - 0.016),
            mat_idx=2,
        )
    ign_socket_profile = [
        (0.0, -0.024),
        (0.025, -0.024),
        (0.025, 0.002),
        (0.027, 0.004),
        (0.027, 0.010),
        (0.021, 0.010),
        (0.0175, 0.006),
        (0.0175, -0.018),
        (0.0, -0.018),
    ]
    append_lathe_profile(
        bm_ct,
        ign_socket_profile,
        axis="Z",
        center_xyz=ign_piv,
        segments=24,
        mat_idx=1,
        ring_mat_indices=[1, 1, 0, 0, 0, 2, 3, 3],
        auto_chamfer=False,
    )
    ct_obj = create_mesh_node(
        "CutterTurret",
        bm_ct,
        cfg["piv_cutter_turret"],
        parent=rover_root,
        mat_list=[mats["R1_Brass"], mats["R1_GreyMetal"], mats["R1_BareSteel"], mats["R1_Rubber"]],
        cfg=cfg,
    )

    # Cutter_Igniter: spark-plug-like ceramic white + brass igniter cartridge seated in the CutterTurret socket
    bm_ign = bmesh.new()
    ign_profile = [
        (0.0, -0.015),
        (0.0168, -0.015),
        (0.0168, 0.010),
        (0.0195, 0.010),
        (0.0195, 0.025),
        (0.0145, 0.026),
        (0.0190, 0.029),
        (0.0190, 0.035),
        (0.0140, 0.038),
        (0.0190, 0.041),
        (0.0190, 0.047),
        (0.0140, 0.050),
        (0.0185, 0.053),
        (0.0185, 0.059),
        (0.0135, 0.063),
        (0.0115, 0.064),
        (0.0115, 0.072),
        (0.0072, 0.073),
        (0.0072, 0.080),
        (0.0050, 0.082),
        (0.0, 0.082),
    ]
    append_lathe_profile(
        bm_ign,
        ign_profile,
        axis="Z",
        center_xyz=(0.0, 0.0, 0.0),
        segments=24,
        mat_idx=0,
        ring_mat_indices=[1] * 5 + [0] * 9 + [1] * 6,
        auto_chamfer=False,
    )
    append_cylinder(
        bm_ign,
        radius1=0.0235,
        radius2=0.0235,
        length=0.012,
        axis="Z",
        center_xyz=(0.0, 0.0, 0.016),
        segments=6,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=1,
    )
    create_mesh_node(
        "Cutter_Igniter",
        bm_ign,
        ign_piv,
        parent=ct_obj,
        mat_list=[mats["R1_WhitePaint"], mats["R1_Brass"]],
        cfg=cfg,
    )

    bm_ch = bmesh.new()
    ch_profile = [
        (ct_r * 0.76, -0.024),
        (ct_r * 0.76, 0.018),
        (ct_r * 0.66, 0.034),
        (ct_r * 0.56, 0.034),
        (ct_r * 0.52, 0.016),
        (0.0, 0.016),
    ]
    append_lathe_profile(
        bm_ch,
        ch_profile,
        axis="Z",
        center_xyz=(0.0, ct_y_off, 0.175),
        segments=28,
        mat_idx=1,
        ring_mat_indices=[1, 1, 1, 3, 2],
    )
    append_spherical_dome(
        bm_ch,
        radius=ct_r * 0.52,
        dome_height=0.020,
        axis="Z",
        center_xyz=(0.0, ct_y_off, 0.189),
        segments=24,
        rings=4,
        mat_idx=2,
    )
    ch_obj = create_mesh_node(
        "CutterHead",
        bm_ch,
        cfg["piv_cutter_head_local"],
        parent=ct_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Brass"], mats["R1_DarkGlass"], mats["R1_BareSteel"]],
        cfg=cfg,
    )
    create_empty("CutterOrigin", parent=ch_obj, pivot_u=cfg["piv_cutter_origin_local"], local_space=True, empty_size=0.04)
    create_empty("CutterSocket", parent=ch_obj, pivot_u=cfg["piv_cutter_origin_local"], local_space=True, empty_size=0.04)
    create_empty("StitchOrigin", parent=ch_obj, pivot_u=cfg["piv_stitch_origin_local"], local_space=True, empty_size=0.04)
    create_empty("CoilSocket_T1", parent=ch_obj, pivot_u=cfg["piv_coil_socket_t1_local"], local_space=True, empty_size=0.04)

    # DigScoop (#7 in callouts.png): cross-tube + bolted pivot collars hanging from front bumper bar at (0, 0.35, 0.54)
    bm_ds = bmesh.new()
    append_cylinder(
        bm_ds,
        radius1=0.022,
        radius2=0.022,
        length=0.52,
        axis="X",
        center_xyz=(0.0, 0.02, 0.02),
        segments=14,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_ds,
            size_xyz=(0.048, 0.064, 0.070),
            center_xyz=(sx * 0.24, 0.02, 0.02),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=0,
        )
        append_hex_bolt(
            bm_ds,
            radius=0.014,
            height=0.068,
            axis="X",
            center_xyz=(sx * 0.24, 0.02, 0.02),
            mat_idx=1,
        )
    ds_obj = create_mesh_node(
        "DigScoop",
        bm_ds,
        cfg["piv_dig_scoop"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # ScoopArms: contoured loader arms tucked between the front tyres (reaching local z = +0.18 m -> world z = +0.72 m)
    bm_arms = bmesh.new()
    arm_poly = [
        (0.00, 0.045),
        (0.05, 0.045),
        (0.18, -0.105),
        (0.18, -0.150),
        (0.13, -0.150),
        (-0.01, 0.000),
    ]
    for sx in (-1.0, 1.0):
        append_prism_extrusion(
            bm_arms,
            arm_poly,
            depth=0.038,
            axis="X",
            center_xyz=(sx * 0.25, 0.0, 0.0),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=0,
        )
        for py, pz in ((0.020, 0.020), (-0.125, 0.160)):
            append_cylinder(
                bm_arms,
                radius1=0.026,
                radius2=0.026,
                length=0.050,
                axis="X",
                center_xyz=(sx * 0.25, py, pz),
                segments=14,
                bevel_width=0.004,
                bevel_segments=1,
                mat_idx=0,
            )
            append_hex_bolt(
                bm_arms,
                radius=0.013,
                height=0.062,
                axis="X",
                center_xyz=(sx * 0.25, py, pz),
                mat_idx=1,
            )
    append_cylinder(
        bm_arms,
        radius1=0.015,
        radius2=0.015,
        length=0.48,
        axis="X",
        center_xyz=(0.0, -0.055, 0.095),
        segments=12,
        mat_idx=0,
    )
    arms_obj = create_mesh_node(
        "ScoopArms",
        bm_arms,
        (0.0, 0.0, 0.0),
        parent=ds_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    # ScoopBucket: hollowed C-profile bucket tucked between front tyres (world pivot = (0, 0.21, 0.70),
    # 3D span world y = 0.11..0.32 m, world z = +0.63..+0.885 m, width = 0.68 m)
    bm_bkt = bmesh.new()
    bw = 0.68
    bh = 0.21
    bd = 0.20
    zf = 0.04  # Tucked so 3D teeth end at local z = +0.185 m (world z = +0.885 m, flush with front tyres)

    # Hollowed C-profile trough wall in (uz, uy) extruded across width bw
    trough_poly = [
        (zf - bd * 0.34, bh * 0.48),
        (zf - bd * 0.44, bh * 0.12),
        (zf - bd * 0.38, -bh * 0.44),
        (zf + bd * 0.44, -bh * 0.44),
        (zf + bd * 0.44, -bh * 0.33),
        (zf - bd * 0.27, -bh * 0.33),
        (zf - bd * 0.33, bh * 0.08),
        (zf - bd * 0.22, bh * 0.42),
    ]
    append_prism_extrusion(
        bm_bkt,
        trough_poly,
        depth=bw - 0.024,
        axis="X",
        center_xyz=(0.0, 0.0, 0.0),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,
    )

    # Rolled cylindrical top lip + bare-steel top wear strip across full bucket width
    append_cylinder(
        bm_bkt,
        radius1=0.018,
        radius2=0.018,
        length=bw + 0.008,
        axis="X",
        center_xyz=(0.0, bh * 0.46, zf - bd * 0.27),
        segments=16,
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=0,
    )
    append_cylinder(
        bm_bkt,
        radius1=0.010,
        radius2=0.010,
        length=bw * 0.94,
        axis="X",
        center_xyz=(0.0, bh * 0.50, zf - bd * 0.24),
        segments=12,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=1,
    )

    # Beveled side cheek plates with corner gussets & pivot bolts
    cheek_poly = [
        (zf - bd * 0.44, bh * 0.48),
        (zf - bd * 0.44, -bh * 0.44),
        (zf + bd * 0.46, -bh * 0.44),
        (zf + bd * 0.20, bh * 0.02),
        (zf - bd * 0.22, bh * 0.48),
    ]
    for sx in (-1.0, 1.0):
        cheek_x = sx * (bw * FLAT_UV_SCALE - 0.012)
        append_prism_extrusion(
            bm_bkt,
            cheek_poly,
            depth=0.024,
            axis="X",
            center_xyz=(cheek_x, 0.0, 0.0),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=0,
        )
        append_hex_bolt(
            bm_bkt,
            radius=0.011,
            height=0.034,
            axis="X",
            center_xyz=(cheek_x, -0.02, zf - bd * 0.28),
            mat_idx=1,
        )

    # Beveled bare-steel cutting blade + 6 individual wedge-shaped chisel teeth
    append_beveled_box(
        bm_bkt,
        size_xyz=(bw - 0.016, 0.020, 0.048),
        center_xyz=(0.0, -bh * 0.40, zf + bd * 0.42),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )
    tooth_poly = [
        (-0.022, 0.012),
        (-0.022, -0.012),
        (0.044, -0.012),
        (0.048, -0.003),
        (0.010, 0.014),
    ]
    tooth_count = cfg["scoop_tooth_count"]
    tooth_span = bw * 0.80
    for i in range(tooth_count):
        tx = -tooth_span * FLAT_UV_SCALE + tooth_span * i / max(1, tooth_count - 1)
        append_prism_extrusion(
            bm_bkt,
            tooth_poly,
            depth=0.040,
            axis="X",
            center_xyz=(tx, -bh * 0.41, zf + bd * 0.48),
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=1,
        )
        append_hex_bolt(
            bm_bkt,
            radius=0.0055,
            height=0.009,
            axis="Y",
            center_xyz=(tx, -bh * 0.34, zf + bd * 0.44),
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
    # Reconcile top-view ortho projection length (2.052 m top vs 1.792 m side) without affecting side/front/back or Unity
    _append_top_ortho_apron_quad(bkt_obj, half_x=0.335, local_y=-0.0700, z_min=0.12, z_max=0.435)
    create_empty("ScoopOrigin", parent=bkt_obj, pivot_u=cfg["piv_scoop_origin_local"], local_space=True, empty_size=0.04)

    # ScoopCyl_L & ScoopCyl_R: hydraulic barrel + R1_Brass gland collar + bare-steel piston rod
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        bm_cyl = bmesh.new()
        cyl_profile = [
            (0.020, -0.055),
            (0.020, 0.020),
            (0.024, 0.023),
            (0.024, 0.040),
            (0.012, 0.042),
            (0.012, 0.115),
            (0.017, 0.118),
            (0.017, 0.132),
            (0.0, 0.132),
        ]
        append_lathe_profile(
            bm_cyl,
            cyl_profile,
            axis="Z",
            center_xyz=(0.0, -0.04, 0.05),
            segments=16,
            mat_idx=0,
            rot_euler=(-0.48, 0.0, 0.0),
            ring_mat_indices=[2, 0, 0, 0, 1, 1, 2, 2],
        )
        create_mesh_node(
            f"ScoopCyl_{side_label}",
            bm_cyl,
            (sx * cfg["piv_scoop_cyl_x"], cfg["piv_scoop_cyl_y"], cfg["piv_scoop_cyl_z"]),
            parent=ds_obj,
            mat_list=[mats["R1_Brass"], mats["R1_BareSteel"], mats["R1_GreyMetal"]],
            cfg=cfg,
        )


def build_rear_and_shoulder_nodes(rover_root, body_obj, cfg, mats):
    """
    Build BatteryLamp_0..7 & HatchWindow/HatchCrate_0..7 (recessed into the upper rear roof indicator
    frame #10 in callouts.png), Stage_1..3_L/R, ChargeConduits, CargoHold, full-height clamshell
    CargoHatch (#12 in callouts.png with 4 real exterior barrel hinges & central latches), WinchSocket
    (with 4-roller fairlead, wound steel cable turns on WinchSpool, and forged TowHook), and Antenna.
    """
    # Offset BatteryLamp_0..7 mesh vertices up to the upper rear roof indicator bar (y = 0.855 m, z = -0.715 m)
    span_x = 0.44
    bl_dy = 0.855 - cfg["piv_battery_lamp_y"]
    bl_dz = -0.715 - cfg["piv_battery_lamp_z"]
    for i in range(8):
        bx = -span_x * FLAT_UV_SCALE + span_x * i / 7.0
        local_dx = (-0.189 + 0.054 * i) - round(bx, 4)
        bm_bl = bmesh.new()
        append_beveled_box(
            bm_bl,
            size_xyz=(0.054, 0.038, 0.034),
            center_xyz=(local_dx, bl_dy, bl_dz),
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=0,
        )
        bl_obj = create_mesh_node(
            f"BatteryLamp_{i}",
            bm_bl,
            (round(bx, 4), cfg["piv_battery_lamp_y"], cfg["piv_battery_lamp_z"]),
            parent=body_obj,
            mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
            cfg=cfg,
        )
        bm_bg = bmesh.new()
        append_beveled_box(
            bm_bg,
            size_xyz=(0.040, 0.022, 0.028),
            center_xyz=(local_dx, bl_dy - 0.006, bl_dz - 0.007),
            bevel_width=0.003,
            bevel_segments=2,
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
            size_xyz=(0.022, 0.012, 0.18),
            center_xyz=(sx * 0.22, 0.84, 0.14),
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=0,
        )
        append_beveled_box(
            bm_cg,
            size_xyz=(0.012, 0.008, 0.16),
            center_xyz=(sx * 0.22, 0.844, 0.14),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=0,
        )
    cc_obj = create_mesh_node(
        "ChargeConduits",
        bm_cc,
        (0.0, 0.0, 0.0),
        parent=body_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
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

    stage_coords = ((1, 0.88, 0.10), (2, 0.85, 0.22), (3, 0.80, 0.34))
    for idx, sy, sz in stage_coords:
        for side_label, sx in (("L", -1.0), ("R", 1.0)):
            bm_st = bmesh.new()
            append_beveled_box(
                bm_st,
                size_xyz=(0.026, 0.014, 0.034),
                center_xyz=(0.0, -0.02, 0.0),
                bevel_width=0.003,
                bevel_segments=2,
                mat_idx=0,
            )
            st_obj = create_mesh_node(
                f"Stage_{idx}_{side_label}",
                bm_st,
                (sx * 0.26, sy, sz),
                parent=body_obj,
                mat_list=[mats["R1_GreyMetal"], mats["R1_Brass"]],
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
        size_xyz=(0.54, 0.32, 0.30),
        center_xyz=(0.0, 0.01, -0.02),
        bevel_width=0.012,
        bevel_segments=2,
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
    create_empty("HatchOrigin", parent=hold_obj, pivot_u=cfg["piv_hatch_origin_local"], local_space=True, empty_size=0.04)
    create_empty("StoryRackSocket", parent=hold_obj, pivot_u=cfg["piv_story_rack_local"], local_space=True, empty_size=0.04)

    # HatchWindow (#10 in callouts.png): recessed 8-cell dark indicator frame at upper rear roof edge (y = 0.855 m)
    bm_hw = bmesh.new()
    append_beveled_box(
        bm_hw,
        size_xyz=(0.50, 0.040, 0.036),
        center_xyz=(0.0, 0.025, -0.135),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=0,
    )
    for div_i in range(9):
        dx = -0.216 + 0.054 * div_i
        append_beveled_box(
            bm_hw,
            size_xyz=(0.008, 0.038, 0.044),
            center_xyz=(dx, 0.025, -0.141),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=0,
            guard_inset=False,
        )
    for sx in (-0.235, 0.235):
        append_hex_bolt(
            bm_hw,
            radius=0.005,
            height=0.008,
            axis="Z",
            center_xyz=(sx, 0.025, -0.155),
            mat_idx=1,
        )
    hw_obj = create_mesh_node(
        "HatchWindow",
        bm_hw,
        cfg["piv_hatch_window_local"],
        parent=hold_obj,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
        cfg=cfg,
    )
    for i in range(8):
        cx = -0.189 + 0.054 * i
        bm_hc = bmesh.new()
        append_beveled_box(
            bm_hc,
            size_xyz=(0.040, 0.022, 0.028),
            center_xyz=(0.0, 0.025, -0.130),
            bevel_width=0.003,
            bevel_segments=2,
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

    # CargoHatch (#12 in callouts.png): full-height clamshell double doors spanning y = 0.45..0.83 m
    hatch_emp = create_empty("CargoHatch", parent=body_obj, pivot_u=cfg["piv_cargo_hatch"], local_space=True, empty_size=0.04)
    bm_up = bmesh.new()
    hw_w = cfg["cargo_hatch_width"]

    # Recessed dark perimeter frame & central vertical door-split seam channel
    append_beveled_box(
        bm_up,
        size_xyz=(hw_w, 0.28, 0.044),
        center_xyz=(0.0, -0.11, -0.162),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=2,
    )
    for sx in (-1.0, 1.0):
        # Upper cream door leaf
        append_beveled_box(
            bm_up,
            size_xyz=(hw_w * 0.445, 0.180, 0.046),
            center_xyz=(sx * (hw_w * 0.238), -0.066, -0.172),
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=0,
        )
        # Orange beltline band across the clamshell door
        append_beveled_box(
            bm_up,
            size_xyz=(hw_w * 0.445, 0.082, 0.047),
            center_xyz=(sx * (hw_w * 0.238), -0.199, -0.172),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=1,
        )
        # 2 real exterior barrel hinges per outer door jamb (4 total)
        for hy in (-0.040, -0.215):
            hx = sx * (hw_w * 0.485)
            append_beveled_box(
                bm_up,
                size_xyz=(0.034, 0.034, 0.018),
                center_xyz=(hx - sx * 0.010, hy, -0.188),
                bevel_width=0.003,
                bevel_segments=1,
                mat_idx=4,  # R1_Brass hinge leaf
                guard_inset=False,
            )
            append_cylinder(
                bm_up,
                radius1=0.009,
                radius2=0.009,
                length=0.038,
                axis="Y",
                center_xyz=(hx + sx * 0.004, hy, -0.192),
                segments=10,
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=3,
            )
        # Vertical bare-steel door grab handle / latch bar flanking the center split seam (#12 in callouts.png)
        append_beveled_box(
            bm_up,
            size_xyz=(0.014, 0.095, 0.022),
            center_xyz=(sx * 0.036, -0.115, -0.192),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=3,
            guard_inset=False,
        )

    create_mesh_node(
        "UpperCanopy",
        bm_up,
        (0.0, 0.0, 0.0),
        parent=hatch_emp,
        mat_list=[
            mats["R1_CreamEnamel"],
            mats["R1_OrangeStripe"],
            mats["R1_GreyMetal"],
            mats["R1_BareSteel"],
            mats["R1_Brass"],
        ],
        cfg=cfg,
    )

    # LowerChute: lower cream section of the clamshell doors
    bm_low = bmesh.new()
    append_beveled_box(
        bm_low,
        size_xyz=(hw_w, 0.105, 0.044),
        center_xyz=(0.0, 0.026, -0.162),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=0,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_low,
            size_xyz=(hw_w * 0.445, 0.096, 0.046),
            center_xyz=(sx * (hw_w * 0.238), 0.028, -0.172),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=1,
        )
    create_mesh_node(
        "LowerChute",
        bm_low,
        cfg["piv_lower_chute_local"],
        parent=hatch_emp,
        mat_list=[mats["R1_GreyMetal"], mats["R1_CreamEnamel"], mats["R1_BareSteel"]],
        cfg=cfg,
    )

    bm_ir = bmesh.new()
    append_cylinder(
        bm_ir,
        radius1=0.015,
        radius2=0.015,
        length=hw_w * 0.82,
        axis="X",
        center_xyz=(0.0, 0.0, -0.155),
        segments=14,
        bevel_width=0.003,
        bevel_segments=1,
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

    # WinchSocket: side mounting cheeks, R1_Brass motor end-housing, and 4-roller hawse fairlead
    bm_ws = bmesh.new()
    append_beveled_box(
        bm_ws,
        size_xyz=(0.28, 0.115, 0.105),
        center_xyz=(0.0, 0.02, -0.150),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=0,
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_ws,
            size_xyz=(0.026, 0.125, 0.120),
            center_xyz=(sx * 0.125, 0.02, -0.160),
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=0,
        )
        for by_off in (-0.02, 0.06):
            append_hex_bolt(
                bm_ws,
                radius=0.008,
                height=0.012,
                axis="X",
                center_xyz=(sx * 0.140, by_off, -0.170),
                mat_idx=2,
            )
    append_cylinder(
        bm_ws,
        radius1=0.038,
        radius2=0.036,
        length=0.065,
        axis="X",
        center_xyz=(-0.165, 0.02, -0.160),
        segments=18,
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )
    for ry_off in (-0.028, 0.048):
        append_cylinder(
            bm_ws,
            radius1=0.009,
            radius2=0.009,
            length=0.16,
            axis="X",
            center_xyz=(0.0, ry_off, -0.217),
            segments=10,
            mat_idx=2,
        )
    for rx_off in (-0.075, 0.075):
        append_cylinder(
            bm_ws,
            radius1=0.009,
            radius2=0.009,
            length=0.095,
            axis="Y",
            center_xyz=(rx_off, 0.010, -0.217),
            segments=10,
            mat_idx=2,
        )
    ws_obj = create_mesh_node(
        "WinchSocket",
        bm_ws,
        cfg["piv_winch_socket"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_Brass"], mats["R1_BareSteel"]],
        cfg=cfg,
    )
    create_empty("TetherSocket", parent=ws_obj, pivot_u=(0.0, 0.0, 0.0), local_space=True, empty_size=0.04)

    # WinchSpool: flanged drum + 10.5 visible turns of wound braided steel cable
    bm_sp = bmesh.new()
    append_cylinder(
        bm_sp,
        radius1=0.036,
        radius2=0.036,
        length=0.17,
        axis="X",
        center_xyz=(0.0, 0.02, -0.165),
        segments=18,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=1,
    )
    for sx in (-0.082, 0.082):
        append_cylinder(
            bm_sp,
            radius1=0.054,
            radius2=0.054,
            length=0.012,
            axis="X",
            center_xyz=(sx, 0.02, -0.165),
            segments=20,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=1,
        )
    append_helical_coil(
        bm_sp,
        coil_radius=0.044,
        wire_radius=0.0068,
        length=0.145,
        turns=10.5,
        axis="X",
        center_xyz=(0.0, 0.02, -0.165),
        segs_per_turn=10,
        wire_segs=5,
        mat_idx=0,
    )
    create_mesh_node(
        "WinchSpool",
        bm_sp,
        (0.0, 0.0, 0.0),
        parent=ws_obj,
        mat_list=[mats["R1_BareSteel"], mats["R1_GreyMetal"]],
        cfg=cfg,
    )

    # TowHook: clevis shackle block + curved forged-steel crane hook (tucked well inside rear tyre span)
    bm_th = bmesh.new()
    append_beveled_box(
        bm_th,
        size_xyz=(0.064, 0.054, 0.054),
        center_xyz=(0.0, 0.015, -0.057),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,
    )
    append_beveled_box(
        bm_th,
        size_xyz=(0.068, 0.022, 0.034),
        center_xyz=(0.0, 0.015, -0.069),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=2,
        guard_inset=False,
    )
    append_hex_bolt(
        bm_th,
        radius=0.011,
        height=0.078,
        axis="X",
        center_xyz=(0.0, 0.015, -0.059),
        mat_idx=1,
    )
    hook_poly = [
        (0.030, 0.020),
        (0.010, 0.020),
        (-0.014, -0.005),
        (-0.024, -0.036),
        (-0.010, -0.064),
        (0.020, -0.068),
        (0.040, -0.044),
        (0.034, -0.018),
        (0.022, -0.022),
        (0.022, -0.038),
        (0.010, -0.046),
        (-0.004, -0.040),
        (-0.004, -0.016),
        (0.016, 0.002),
        (0.030, 0.002),
    ]
    append_prism_extrusion(
        bm_th,
        hook_poly,
        depth=0.026,
        axis="X",
        center_xyz=(0.0, 0.0, -0.095),
        bevel_width=0.004,
        bevel_segments=2,
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
    create_empty("WinchOrigin", parent=th_obj, pivot_u=(0.0, 0.0, 0.0), local_space=True, empty_size=0.04)

    # Antenna: turned R1_Brass base collar at (-0.38, 0.90, -0.47) + vertical whip mast rising to uy = 1.592 m
    bm_ant = bmesh.new()
    append_lathe_profile(
        bm_ant,
        [
            (0.040, -0.012),
            (0.038, 0.020),
            (0.030, 0.026),
            (0.030, 0.082),
            (0.022, 0.096),
            (0.0, 0.096),
        ],
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=20,
        mat_idx=0,
        ring_mat_indices=[0, 2, 2, 2, 0],
    )
    append_cylinder(
        bm_ant,
        radius1=0.0125,
        radius2=0.0105,
        length=0.64,
        axis="Y",
        center_xyz=(0.0, 0.35, 0.0),
        segments=12,
        mat_idx=1,
    )
    ant_obj = create_mesh_node(
        "Antenna",
        bm_ant,
        cfg["piv_antenna"],
        parent=rover_root,
        mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"], mats["R1_Brass"]],
        cfg=cfg,
    )
    create_empty("AntennaSocket", parent=ant_obj, pivot_u=(0.0, 0.0, 0.0), local_space=True, empty_size=0.04)

    # Antenna_Segment2: telescoping second mast segment retracted inside Antenna mast in rest pose
    # (pivot at its base, local +Y up axis = extension axis; extends +0.35 m straight up when raised)
    bm_seg2 = bmesh.new()
    append_cylinder(
        bm_seg2,
        radius1=0.0078,
        radius2=0.0068,
        length=0.358,
        axis="Y",
        center_xyz=(0.0, 0.179, 0.0),
        segments=12,
        mat_idx=0,
    )
    append_cylinder(
        bm_seg2,
        radius1=0.0086,
        radius2=0.0086,
        length=0.024,
        axis="Y",
        center_xyz=(0.0, 0.180, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=0,
    )
    append_cylinder(
        bm_seg2,
        radius1=0.0092,
        radius2=0.0092,
        length=0.016,
        axis="Y",
        center_xyz=(0.0, 0.350, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=0,
    )
    append_cylinder(
        bm_seg2,
        radius1=0.0135,
        radius2=0.0130,
        length=0.032,
        axis="Y",
        center_xyz=(0.0, 0.374, 0.0),
        segments=16,
        bevel_width=0.003,
        bevel_segments=2,
        mat_idx=1,
    )
    append_cylinder(
        bm_seg2,
        radius1=0.0095,
        radius2=0.0075,
        length=0.008,
        axis="Y",
        center_xyz=(0.0, 0.393, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=0,
    )
    create_mesh_node(
        "Antenna_Segment2",
        bm_seg2,
        cfg["piv_antenna_segment2_local"],
        parent=ant_obj,
        mat_list=[mats["R1_BareSteel"], mats["R1_LampAmber"]],
        cfg=cfg,
    )

    bm_loop = bmesh.new()
    append_torus(
        bm_loop,
        major_radius=0.032,
        minor_radius=0.007,
        axis="Z",
        center_xyz=(0.0, -0.06, 0.0),
        major_seg=22,
        minor_seg=6,
        mat_idx=0,
    )
    append_cylinder(
        bm_loop,
        radius1=0.005,
        radius2=0.005,
        length=0.060,
        axis="X",
        center_xyz=(0.0, -0.06, 0.0),
        segments=10,
        mat_idx=1,
    )
    loop_obj = create_mesh_node(
        "SonarLoop",
        bm_loop,
        cfg["piv_sonar_loop_local"],
        parent=ant_obj,
        mat_list=[mats["R1_Brass"], mats["R1_BareSteel"]],
        cfg=cfg,
    )
    create_empty("AntennaOrigin", parent=loop_obj, pivot_u=(0.0, 0.0, 0.0), local_space=True, empty_size=0.04)

    bm_tip = bmesh.new()
    append_spherical_dome(
        bm_tip,
        radius=0.022,
        dome_height=0.022,
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=16,
        rings=4,
        mat_idx=0,
        collar_depth=0.024,
    )
    create_mesh_node(
        "AntennaTip",
        bm_tip,
        cfg["piv_antenna_tip_local"],
        parent=ant_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    # Ro's gift Pennant clipped to the antenna whip below AntennaTip
    bm_pen = bmesh.new()
    append_cylinder(
        bm_pen,
        radius1=0.014,
        radius2=0.014,
        length=0.032,
        axis="Y",
        center_xyz=(0.0, 0.0, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=0,
    )
    pen_poly = [
        (-0.008, 0.014),
        (-0.008, -0.014),
        (-0.145, -0.002),
    ]
    append_prism_extrusion(
        bm_pen,
        pen_poly,
        depth=0.006,
        axis="X",
        center_xyz=(0.0, 0.0, 0.0),
        bevel_width=0.001,
        bevel_segments=1,
        mat_idx=1,
    )
    create_mesh_node(
        "Pennant",
        bm_pen,
        (0.0, 0.56, 0.0),
        parent=ant_obj,
        mat_list=[mats["R1_Brass"], mats["R1_OrangeStripe"]],
        cfg=cfg,
    )

    # Bell's gift Decal07Fresh: crisp repainted "07" serial plaques on both flanks and rear valence
    bm_decal = bmesh.new()
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm_decal,
            size_xyz=(0.008, 0.076, 0.148),
            center_xyz=(sx * 0.445, 0.0, 0.08),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=0,
        )
        for dz in (-0.032, 0.032):
            append_beveled_box(
                bm_decal,
                size_xyz=(0.010, 0.052, 0.044),
                center_xyz=(sx * 0.446, 0.0, 0.08 + dz),
                bevel_width=0.0015,
                bevel_segments=1,
                mat_idx=1,
            )
    append_beveled_box(
        bm_decal,
        size_xyz=(0.136, 0.068, 0.008),
        center_xyz=(0.20, 0.0, -0.742),
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=0,
    )
    for dx in (-0.030, 0.030):
        append_beveled_box(
            bm_decal,
            size_xyz=(0.040, 0.048, 0.010),
            center_xyz=(0.20 + dx, 0.0, -0.743),
            bevel_width=0.0015,
            bevel_segments=1,
            mat_idx=1,
        )
    create_mesh_node(
        "Decal07Fresh",
        bm_decal,
        (0.0, 0.605, 0.0),
        parent=body_obj,
        mat_list=[mats["R1_OrangeStripe"], mats["R1_CreamEnamel"]],
        cfg=cfg,
    )

    # Kessler-Arai maker decals matching the SSR-3 chassis decal physical size (width=0.25, height=0.175,
    # u_crop=(0.04, 0.96), v_crop=(0.15, 0.85)) on both flank panels above the orange stripe, symmetric
    ka_uy = 0.7545
    ka_uz = -0.030
    ka_width = 0.25
    ka_height = 0.175
    ka_u_crop = (0.04, 0.96)
    ka_v_crop = (0.14, 0.84)
    ka_offset = 0.0020
    ka_thickness = 0.0008
    ka_flank_x = _hull_flank_x(ka_uy, ka_uz, cfg)
    for side_label, sx in (("L", -1.0), ("R", 1.0)):
        ka_center = (round(sx * ka_flank_x, 4), ka_uy, ka_uz)
        decal_obj = add_maker_decal_node(
            f"Decal_KA_{side_label}",
            body_obj,
            center_u=ka_center,
            normal_u=(sx, 0.0133, 0.0),
            width=ka_width,
            height=ka_height,
            cell_key="KESSLER_ARAI",
            up_u=(0.0, 1.0, 0.0),
            offset=ka_offset,
            thickness=ka_thickness,
            pivot_u=ka_center,
            u_crop=ka_u_crop,
            v_crop=ka_v_crop,
            material_override=mats["R1_Signage"],
            local_space=True,
        )
        _conform_flank_decal_wafer(
            decal_obj,
            sx=sx,
            ka_center=ka_center,
            width=ka_width,
            height=ka_height,
            u_crop=ka_u_crop,
            v_crop=ka_v_crop,
            offset=ka_offset,
            thickness=ka_thickness,
            cfg=cfg,
        )


def _conform_flank_decal_wafer(
    decal_obj,
    sx,
    ka_center,
    width,
    height,
    u_crop,
    v_crop,
    offset,
    thickness,
    cfg,
):
    """
    Conform a flank maker decal wafer to _hull_flank_x(uy, uz, cfg) across the hull's exact
    vertical contour rings (0.667..0.842 m) and longitudinal barrel curve so the 0.25 m x 0.175 m
    decal sits a uniform 2.0 mm proud of the flank panel with zero hull clipping.
    """
    u_center, v_center, u_span, v_span = MAKER_DECAL_CELLS["KESSLER_ARAI"]
    u_min_cell = u_center - 0.5 * u_span
    v_min_cell = v_center - 0.5 * v_span
    u0 = round(u_min_cell + u_crop[0] * u_span, 5)
    u1 = round(u_min_cell + u_crop[1] * u_span, 5)
    v0 = round(v_min_cell + v_crop[0] * v_span, 5)
    v1 = round(v_min_cell + v_crop[1] * v_span, 5)

    ut0 = round(u_center - 0.135, 5)
    ut1 = round(u_center - 0.132, 5)
    vt0 = round(v_center - 0.135, 5)
    vt1 = round(v_center - 0.132, 5)
    trim_uvs = ((ut0, vt0), (ut1, vt0), (ut1, vt1), (ut0, vt1))

    piv_b = u_pos(*ka_center)
    ka_uy = ka_center[1]
    ka_uz = ka_center[2]
    y_bot = round(ka_uy - 0.5 * height, 4)
    y_top = round(ka_uy + 0.5 * height, 4)
    y_vals = (y_bot, 0.7350, 0.7900, 0.8350, y_top)
    u_fracs = (0.0, 0.25, 0.50, 0.75, 1.0)

    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.verify()

    vf_grid = []
    vb_grid = []
    for uy in y_vals:
        rf = []
        rb = []
        for uf in u_fracs:
            uz = ka_uz + sx * (uf - 0.5) * width
            hx = _hull_flank_x(uy, uz, cfg)
            xf = sx * (hx + offset)
            xb = sx * (hx + offset - thickness)
            rf.append(bm.verts.new(u_pos(xf, uy, uz) - piv_b))
            rb.append(bm.verts.new(u_pos(xb, uy, uz) - piv_b))
        vf_grid.append(rf)
        vb_grid.append(rb)

    nrows = len(y_vals)
    ncols = len(u_fracs)
    side_faces = []
    for j in range(nrows - 1):
        vf_0 = (y_vals[j] - y_bot) / height
        vf_1 = (y_vals[j + 1] - y_bot) / height
        vj0 = round(v0 + vf_0 * (v1 - v0), 5)
        vj1 = round(v0 + vf_1 * (v1 - v0), 5)
        for i in range(ncols - 1):
            ui0 = round(u0 + u_fracs[i] * (u1 - u0), 5)
            ui1 = round(u0 + u_fracs[i + 1] * (u1 - u0), 5)
            f_fr = bm.faces.new((vf_grid[j][i], vf_grid[j][i + 1], vf_grid[j + 1][i + 1], vf_grid[j + 1][i]))
            for loop, uv in zip(f_fr.loops, ((ui0, vj0), (ui1, vj0), (ui1, vj1), (ui0, vj1))):
                loop[uv_layer].uv = uv
            side_faces.append(
                bm.faces.new((vb_grid[j][i], vb_grid[j + 1][i], vb_grid[j + 1][i + 1], vb_grid[j][i + 1]))
            )

    for i in range(ncols - 1):
        side_faces.append(bm.faces.new((vb_grid[0][i], vb_grid[0][i + 1], vf_grid[0][i + 1], vf_grid[0][i])))
        side_faces.append(bm.faces.new((vb_grid[-1][i + 1], vb_grid[-1][i], vf_grid[-1][i], vf_grid[-1][i + 1])))
    for j in range(nrows - 1):
        side_faces.append(bm.faces.new((vb_grid[j + 1][0], vb_grid[j][0], vf_grid[j][0], vf_grid[j + 1][0])))
        side_faces.append(bm.faces.new((vb_grid[j][-1], vb_grid[j + 1][-1], vf_grid[j + 1][-1], vf_grid[j][-1])))

    for sf in side_faces:
        for loop, uv in zip(sf.loops, trim_uvs):
            loop[uv_layer].uv = uv

    for f in bm.faces:
        f.material_index = 0
        f.smooth = True
    for e in bm.edges:
        e.smooth = False
    ensure_outward_normals(bm)

    me = decal_obj.data
    bm.to_mesh(me)
    bm.free()
    apply_weighted_normals(me, sharp_angle_deg=42.0, area_power=1.6, use_modifier=False)
    _init_solid_vcol_mesh(me, (0.96, 0.0, 0.0, 1.0))
    me.update()


def build_suspension_and_sockets(rover_root, cfg, mats):
    """
    Build Arm_FL..RR (twin-spar wishbones with bolted knuckles calibrated for ±0.53 m wheelbase),
    Shock_FL..RR (real 3D helical R1_Copper coil-over springs & hydraulic dampers inside the open wheel
    arches, #8 in callouts.png), Wheel_FL..RR, and all remaining upgrade/equipment Empty sockets.
    """
    for end_label, sz in (("F", 1.0), ("R", -1.0)):
        for side_label, sx in (("L", -1.0), ("R", 1.0)):
            tag = f"{end_label}{side_label}"
            arm_piv = (sx * cfg["piv_arm_x"], cfg["piv_arm_y"], sz * cfg["piv_arm_z"])
            bm_arm = bmesh.new()
            append_beveled_box(
                bm_arm,
                size_xyz=(0.062, 0.054, 0.26),
                center_xyz=(sx * 0.025, -0.055, sz * 0.11),
                bevel_width=0.008,
                bevel_segments=2,
                mat_idx=0,
                rot_euler=(sz * (-0.36), 0.0, 0.0),
            )
            for kn_y, kn_z in ((0.0, 0.0), (-0.095, sz * 0.22)):
                append_cylinder(
                    bm_arm,
                    radius1=0.030,
                    radius2=0.030,
                    length=0.072,
                    axis="X",
                    center_xyz=(sx * 0.025, kn_y, kn_z),
                    segments=14,
                    bevel_width=0.004,
                    bevel_segments=1,
                    mat_idx=0,
                )
                append_hex_bolt(
                    bm_arm,
                    radius=0.014,
                    height=0.086,
                    axis="X",
                    center_xyz=(sx * 0.025, kn_y, kn_z),
                    mat_idx=1,
                )
            create_mesh_node(
                f"Arm_{tag}",
                bm_arm,
                arm_piv,
                parent=rover_root,
                mat_list=[mats["R1_GreyMetal"], mats["R1_BareSteel"]],
                cfg=cfg,
            )

            # Shock_FL..RR (#8 in callouts.png): real 3D helical R1_Copper coil-over spring + damper cylinder
            shock_piv = (sx * cfg["piv_shock_x"], cfg["piv_shock_y"], sz * cfg["piv_shock_z"])
            bm_sh = bmesh.new()
            sh_tilt = (sz * 0.18, 0.0, 0.0)
            sh_center = (sx * 0.018, -0.105, sz * 0.025)

            append_cylinder(
                bm_sh,
                radius1=0.024,
                radius2=0.024,
                length=0.15,
                axis="Y",
                center_xyz=(sh_center[0], -0.055, sz * 0.015),
                segments=14,
                bevel_width=0.003,
                bevel_segments=1,
                mat_idx=0,
                rot_euler=sh_tilt,
            )
            append_cylinder(
                bm_sh,
                radius1=0.014,
                radius2=0.014,
                length=0.13,
                axis="Y",
                center_xyz=(sh_center[0], -0.155, sz * 0.035),
                segments=12,
                mat_idx=2,
                rot_euler=sh_tilt,
            )
            for py_off, pz_off in ((-0.022, sz * 0.008), (-0.182, sz * 0.042)):
                append_cylinder(
                    bm_sh,
                    radius1=0.046,
                    radius2=0.044,
                    length=0.018,
                    axis="Y",
                    center_xyz=(sh_center[0], py_off, pz_off),
                    segments=16,
                    bevel_width=0.003,
                    bevel_segments=1,
                    mat_idx=2,
                    rot_euler=sh_tilt,
                )
            append_helical_coil(
                bm_sh,
                coil_radius=0.038,
                wire_radius=0.0080,
                length=0.160,
                turns=6.5,
                axis="Y",
                center_xyz=sh_center,
                segs_per_turn=12,
                wire_segs=6,
                mat_idx=1,
                rot_euler=sh_tilt,
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
                create_empty("WheelsSocket", parent=w_obj, pivot_u=(0.12, 0.0, 0.0), local_space=True, empty_size=0.04)

    build_solar_wing_model(
        cfg,
        mats,
        parent=rover_root,
        unity_loc=cfg["piv_solar_wing"],
        wing_socket_loc=(0.0, 0.0, -0.11),
        split_gift_cell=True,
    )

    socket_table = (
        ("CradleSocket", (0.0, 0.96, -0.38)),
        ("CargoSocket", (0.0, 0.48, -0.58)),
        ("HoldSocket", (0.0, 0.48, -0.58)),
        ("HeadlampSocket", (0.0, 0.66, 0.62)),
        ("LampSocket", (0.0, 0.66, 0.62)),
        ("CoilSocket", (0.0, 0.28, 0.0)),
        ("BellySocket", (0.0, 0.28, 0.0)),
        ("DrumSocket_L", (-0.46, 0.84, -0.14)),
        ("DrumSocket_R", (0.46, 0.84, -0.14)),
        ("FlanksSocket", (0.46, 0.84, -0.14)),
        ("BinSocket_L", (-0.46, 0.60, 0.02)),
        ("BinSocket_R", (0.46, 0.60, 0.02)),
        ("PanniersSocket", (0.46, 0.60, 0.02)),
        ("SpineSocket", (0.385, 0.90, -0.20)),
        ("BackSocket", (0.385, 0.90, -0.20)),
        ("DustSocket_L", (-0.61, 0.0, -0.53)),
        ("DustSocket_R", (0.61, 0.0, -0.53)),
    )
    for s_name, s_pos in socket_table:
        create_empty(s_name, parent=rover_root, pivot_u=s_pos, local_space=True, empty_size=0.04)


def build_rover_model_hierarchy(cfg, mats):
    """
    Build the complete RoverModel hierarchy in the active Blender scene, then merge
    purely static child meshes into their moving host meshes while keeping all runtime-driven,
    toggled, and independently animated child nodes as separate MESH objects.
    """
    rover_root = create_empty("RoverModel", pivot_u=(0.0, 0.0, 0.0), local_space=True, empty_size=0.04)
    body_obj = build_contoured_body_mesh(cfg, mats)
    body_obj.parent = rover_root
    neck_obj, head_obj = build_head_assembly(rover_root, cfg, mats)
    build_front_tools_and_lamps(rover_root, body_obj, cfg, mats)
    build_rear_and_shoulder_nodes(rover_root, body_obj, cfg, mats)
    build_suspension_and_sockets(rover_root, cfg, mats)
    bpy.context.view_layer.update()

    # Merge purely static child meshes into the host mesh they move with (converting static child nodes to EMPTYs)
    body_static_children = (
        ["Headlamp_L", "Headlamp_R", "Glass_L", "Glass_R", "FrontIntake", "FrontIntakeRoller"]
        + ["ChargeConduits", "CargoHold", "HatchWindow"]
        + [f"Stage_{idx}_{side}" for idx in (1, 2, 3) for side in ("L", "R")]
        + [f"BatteryLamp_{i}" for i in range(8)]
    )
    merge_child_meshes_into_host(body_obj, body_static_children, cfg)
    merge_child_meshes_into_host(neck_obj, ["NeckLift"], cfg)
    merge_child_meshes_into_host(head_obj, ["Visor", "RearVentStrip", "DeckCover"], cfg)

    eye_obj = bpy.data.objects.get("Eye")
    if eye_obj is not None:
        merge_child_meshes_into_host(eye_obj, ["LockRing"], cfg)

    upper_canopy_obj = bpy.data.objects.get("UpperCanopy")
    if upper_canopy_obj is not None:
        merge_child_meshes_into_host(upper_canopy_obj, ["IntakeRoller"], cfg)

    ws_obj = bpy.data.objects.get("WinchSocket")
    if ws_obj is not None:
        merge_child_meshes_into_host(ws_obj, ["TowHook"], cfg)

    ant_obj = bpy.data.objects.get("Antenna")
    if ant_obj is not None:
        merge_child_meshes_into_host(ant_obj, ["SonarLoop"], cfg)

    mesh_objs = [o for o in bpy.data.objects if o.type == "MESH"]
    prune_unused_materials(mesh_objs)
    bpy.context.view_layer.update()
    return rover_root


def build_solar_wing_model(
    cfg,
    mats,
    parent=None,
    unity_loc=(0.0, 0.0, 0.0),
    wing_socket_loc=(0.0, 0.014, -0.11),
    split_gift_cell=False,
):
    """
    Build Tilly's 2x3 photovoltaic SolarWing model (Mechanism A "trunk lid" hinged along its head-side edge).
    Uses R1_SolarCell for the PV cells per Logs/TASK.md.
    When split_gift_cell=True, the (c=1, r=1) cell is emitted as a child MESH node 'CellFilled'.
    """
    bm = bmesh.new()
    sw_w = cfg["solar_wing_width"]
    sw_l = cfg["solar_wing_length"]
    slope_rad = math.radians(cfg["solar_deck_slope_deg"])
    frame_cz = -sw_l * FLAT_UV_SCALE

    append_beveled_box(
        bm,
        size_xyz=(sw_w, 0.016, sw_l),
        center_xyz=(0.0, 0.0, frame_cz),
        bevel_width=0.006,
        bevel_segments=3,
        mat_idx=0,
        rot_euler=(slope_rad, 0.0, 0.0),
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(sw_w * 0.32, 0.005, sw_l * 0.72),
            center_xyz=(sx * (sw_w * 0.22), -0.007, frame_cz),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=3,
            rot_euler=(slope_rad, 0.0, 0.0),
        )

    cols, rows = 2, 3
    cell_w = (sw_w * 0.84) / cols
    cell_l = (sw_l * 0.80) / rows
    gift_cell_bm = None
    gift_cell_loc = (0.0, 0.0, 0.0)
    for c in range(cols):
        cx = (-0.5 + c) * (cell_w + 0.014)
        for r in range(rows):
            dz_rel = (1 - r) * (cell_l + 0.009)
            cz_local = frame_cz + dz_rel * math.cos(slope_rad)
            cy_local = dz_rel * math.sin(slope_rad) + 0.008
            if split_gift_cell and c == 1 and r == 1:
                gift_cell_bm = bmesh.new()
                gift_cell_loc = (round(cx, 4), round(cy_local, 4), round(cz_local, 4))
                append_beveled_box(
                    gift_cell_bm,
                    size_xyz=(cell_w, 0.008, cell_l),
                    center_xyz=(0.0, 0.0, 0.0),
                    bevel_width=0.003,
                    bevel_segments=2,
                    mat_idx=0,
                    rot_euler=(slope_rad, 0.0, 0.0),
                )
                append_beveled_box(
                    gift_cell_bm,
                    size_xyz=(0.005, 0.009, cell_l * 0.92),
                    center_xyz=(0.0, 0.0015, 0.0),
                    bevel_width=0.001,
                    bevel_segments=1,
                    mat_idx=1,
                    rot_euler=(slope_rad, 0.0, 0.0),
                )
                continue
            append_beveled_box(
                bm,
                size_xyz=(cell_w, 0.008, cell_l),
                center_xyz=(cx, cy_local, cz_local),
                bevel_width=0.003,
                bevel_segments=2,
                mat_idx=2,
                rot_euler=(slope_rad, 0.0, 0.0),
            )
            append_beveled_box(
                bm,
                size_xyz=(0.005, 0.009, cell_l * 0.92),
                center_xyz=(cx, cy_local + 0.0015, cz_local),
                bevel_width=0.001,
                bevel_segments=1,
                mat_idx=1,
                rot_euler=(slope_rad, 0.0, 0.0),
            )

    for sx in (-1.0, 1.0):
        append_cylinder(
            bm,
            radius1=0.010,
            radius2=0.010,
            length=0.048,
            axis="X",
            center_xyz=(sx * 0.145, 0.0, 0.0),
            segments=18,
            bevel_width=0.002,
            bevel_segments=2,
            mat_idx=1,
        )
    append_beveled_box(
        bm,
        size_xyz=(0.052, 0.014, 0.028),
        center_xyz=(0.0, 0.004, -0.016),
        bevel_width=0.003,
        bevel_segments=1,
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
    create_empty("WingSocket", parent=sw_obj, pivot_u=wing_socket_loc, local_space=True, empty_size=0.04)
    if gift_cell_bm is not None:
        create_mesh_node(
            "CellFilled",
            gift_cell_bm,
            gift_cell_loc,
            parent=sw_obj,
            mat_list=[mats["R1_SolarCell"], mats["R1_BareSteel"]],
            cfg=cfg,
        )
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
