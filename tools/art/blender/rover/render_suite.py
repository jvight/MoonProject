"""Headless Blender acceptance render suite for 07 v3 ('Amber CRT') and turnaround comparison."""
import math
from pathlib import Path
import bpy
import numpy as np
from mathutils import Matrix

from mesh_ops import COLOR_ATTR_NAME, FLAT_UV_SCALE, unity_to_blender_vec


def load_image_rgba(filepath):
    """Load an image file via bpy.data.images into a top-to-bottom float32 RGBA numpy array (H, W, 4) in sRGB."""
    img = bpy.data.images.load(str(filepath), check_existing=False)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    arr = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(arr)
    bpy.data.images.remove(img)
    arr = arr.reshape((h, w, 4))
    # Blender stores pixels bottom-to-top; flip vertically to top-to-bottom image coordinates
    return arr[::-1, :, :].copy()


def save_image_rgba(filepath, arr_top_down, include_alpha=False):
    """Save a top-to-bottom float32 RGBA numpy array (H, W, 4) in sRGB to PNG via bpy.data.images."""
    h, w, _ = arr_top_down.shape
    flipped = np.clip(arr_top_down[::-1, :, :], 0.0, 1.0).astype(np.float32)
    img = bpy.data.images.new(name="_TempSaveImg", width=w, height=h, alpha=include_alpha)
    img.colorspace_settings.name = "Non-Color"
    img.pixels.foreach_set(flipped.ravel())
    img.filepath_raw = str(filepath)
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def resize_rgba_bilinear(arr, new_h, new_w):
    """Resize a (H, W, 4) float32 array to (new_h, new_w, 4) using vectorized bilinear interpolation."""
    old_h, old_w, _ = arr.shape
    if (old_h, old_w) == (new_h, new_w):
        return arr.copy()
    ys = np.linspace(0.0, old_h - 1.0, new_h, dtype=np.float32)
    xs = np.linspace(0.0, old_w - 1.0, new_w, dtype=np.float32)
    y0 = np.floor(ys).astype(np.int32)
    x0 = np.floor(xs).astype(np.int32)
    y1 = np.minimum(y0 + 1, old_h - 1)
    x1 = np.minimum(x0 + 1, old_w - 1)
    wy = (ys - y0)[:, None, None]
    wx = (xs - x0)[None, :, None]

    Ia = arr[y0[:, None], x0[None, :]]
    Ib = arr[y0[:, None], x1[None, :]]
    Ic = arr[y1[:, None], x0[None, :]]
    Id = arr[y1[:, None], x1[None, :]]
    top = Ia * (1.0 - wx) + Ib * wx
    bot = Ic * (1.0 - wx) + Id * wx
    return top * (1.0 - wy) + bot * wy


def alpha_paste_onto(canvas_rgba, src_rgba, left_x, top_y):
    """Alpha-composite src_rgba onto canvas_rgba at (left_x, top_y) with clipping."""
    ch, cw, _ = canvas_rgba.shape
    sh, sw, _ = src_rgba.shape
    x0_dst = max(0, left_x)
    y0_dst = max(0, top_y)
    x1_dst = min(cw, left_x + sw)
    y1_dst = min(ch, top_y + sh)
    if x0_dst >= x1_dst or y0_dst >= y1_dst:
        return
    x0_src = x0_dst - left_x
    y0_src = y0_dst - top_y
    x1_src = x0_src + (x1_dst - x0_dst)
    y1_src = y0_src + (y1_dst - y0_dst)

    sub_src = src_rgba[y0_src:y1_src, x0_src:x1_src]
    sub_dst = canvas_rgba[y0_dst:y1_dst, x0_dst:x1_dst]
    alpha = sub_src[:, :, 3:4]
    sub_dst[:, :, :3] = sub_src[:, :, :3] * alpha + sub_dst[:, :, :3] * (1.0 - alpha)
    sub_dst[:, :, 3:4] = alpha + sub_dst[:, :, 3:4] * (1.0 - alpha)


def enhance_materials_with_vertex_color_for_render(mats):
    """
    Wire the baked 'Col' vertex color attribute (R=AO, G=wear, B=cavity) into the Principled BSDF
    materials for the renders using the same 0.52 edge-wear threshold as R1LitCore.hlsl.
    """
    wear_targets = {
        "R1_CreamEnamel": (0.48, 0.45, 0.40, 1.0),
        "R1_OrangeStripe": (0.62, 0.58, 0.52, 1.0),
        "R1_SagePatch": (0.54, 0.56, 0.53, 1.0),
        "R1_GreyMetal": (0.52, 0.55, 0.60, 1.0),
        "R1_Copper": (0.90, 0.66, 0.34, 1.0),
        "R1_BareSteel": (0.72, 0.75, 0.78, 1.0),
        "R1_Rubber": (0.20, 0.19, 0.18, 1.0),
    }
    for name, mat in mats.items():
        if name in ("R1_Screen", "R1_LampAmber"):
            continue
        nt = mat.node_tree
        nodes = nt.nodes
        links = nt.links
        bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is None:
            continue

        base_rgba = tuple(bsdf.inputs["Base Color"].default_value)
        attr_node = nodes.new(type="ShaderNodeAttribute")
        attr_node.attribute_name = COLOR_ATTR_NAME
        attr_node.attribute_type = "GEOMETRY"

        sep_rgb = nodes.new(type="ShaderNodeSeparateColor")
        links.new(attr_node.outputs["Color"], sep_rgb.inputs["Color"])

        ao_map = nodes.new(type="ShaderNodeMapRange")
        ao_map.inputs["From Min"].default_value = 0.0
        ao_map.inputs["From Max"].default_value = 1.0
        ao_map.inputs["To Min"].default_value = 0.26
        ao_map.inputs["To Max"].default_value = 1.0
        links.new(sep_rgb.outputs["Red"], ao_map.inputs["Value"])

        wear_map = nodes.new(type="ShaderNodeMapRange")
        wear_map.clamp = True
        wear_map.inputs["From Min"].default_value = 0.52
        wear_map.inputs["From Max"].default_value = 1.0
        wear_map.inputs["To Min"].default_value = 0.0
        wear_map.inputs["To Max"].default_value = 0.65 if name != "R1_Rubber" else 0.35
        links.new(sep_rgb.outputs["Green"], wear_map.inputs["Value"])

        wear_mix = nodes.new(type="ShaderNodeMix")
        wear_mix.data_type = "RGBA"
        links.new(wear_map.outputs["Result"], wear_mix.inputs["Factor"])
        wear_mix.inputs["A"].default_value = base_rgba
        wear_mix.inputs["B"].default_value = wear_targets.get(
            name,
            (
                min(1.0, base_rgba[0] * 1.25 + 0.12),
                min(1.0, base_rgba[1] * 1.22 + 0.11),
                min(1.0, base_rgba[2] * 1.20 + 0.10),
                1.0,
            ),
        )

        cav_mix = nodes.new(type="ShaderNodeMix")
        cav_mix.data_type = "RGBA"
        cav_scale = nodes.new(type="ShaderNodeMath")
        cav_scale.operation = "MULTIPLY"
        links.new(sep_rgb.outputs["Blue"], cav_scale.inputs[0])
        cav_scale.inputs[1].default_value = 0.42
        links.new(cav_scale.outputs[0], cav_mix.inputs["Factor"])
        links.new(wear_mix.outputs["Result"], cav_mix.inputs["A"])
        if name in ("R1_Rubber", "R1_GreyMetal"):
            cav_mix.inputs["B"].default_value = (0.19, 0.18, 0.16, 1.0)
        else:
            cav_mix.inputs["B"].default_value = (
                base_rgba[0] * 0.48 + 0.05,
                base_rgba[1] * 0.46 + 0.05,
                base_rgba[2] * 0.44 + 0.04,
                1.0,
            )

        ao_mul = nodes.new(type="ShaderNodeVectorMath")
        ao_mul.operation = "SCALE"
        links.new(cav_mix.outputs["Result"], ao_mul.inputs["Vector"])
        links.new(ao_map.outputs["Result"], ao_mul.inputs["Scale"])
        links.new(ao_mul.outputs["Vector"], bsdf.inputs["Base Color"])


def setup_studio_lighting_and_camera(cfg):
    """Create balanced 3-point studio lighting + world ambient matching the concept turnaround mood."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = cfg["render_eevee_samples"]
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"

    if scene.world is None:
        scene.world = bpy.data.worlds.new("StudioWorld")
    scene.world.use_nodes = True
    wnodes = scene.world.node_tree.nodes
    wlinks = scene.world.node_tree.links
    wnodes.clear()
    w_out = wnodes.new(type="ShaderNodeOutputWorld")
    w_bg = wnodes.new(type="ShaderNodeBackground")
    bg_rgb = cfg["render_bg_linear"]
    w_bg.inputs["Color"].default_value = (bg_rgb[0], bg_rgb[1], bg_rgb[2], 1.0)
    w_bg.inputs["Strength"].default_value = 1.0
    wlinks.new(w_bg.outputs["Background"], w_out.inputs["Surface"])

    lights = []
    light_specs = (
        ("KeyLight", "SUN", 2.6, (0.98, 0.94, 0.88), (math.radians(48.0), 0.0, math.radians(32.0))),
        ("FillLight", "SUN", 1.35, (0.85, 0.90, 1.00), (math.radians(58.0), 0.0, math.radians(-135.0))),
        ("FrontFill", "SUN", 1.15, (0.98, 0.95, 0.90), (math.radians(72.0), 0.0, math.radians(175.0))),
        ("BackRim", "SUN", 1.25, (0.95, 0.92, 0.86), (math.radians(52.0), 0.0, math.radians(-25.0))),
    )
    for l_name, l_type, energy, col, rot in light_specs:
        ldata = bpy.data.lights.new(name=l_name, type=l_type)
        ldata.energy = energy
        ldata.color = col
        ldata.angle = math.radians(25.0)
        lobj = bpy.data.objects.new(name=l_name, object_data=ldata)
        scene.collection.objects.link(lobj)
        lobj.rotation_euler = rot
        lights.append(lobj)

    cam_data = bpy.data.cameras.new(name="RenderCam")
    cam_obj = bpy.data.objects.new(name="RenderCam", object_data=cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    return cam_obj, lights


def aim_camera_at(cam_obj, cam_unity_pos, target_unity_pos, up_unity=(0.0, 1.0, 0.0)):
    """Position and aim cam_obj from cam_unity_pos toward target_unity_pos using Unity coordinates."""
    c_pos = unity_to_blender_vec(cam_unity_pos)
    t_pos = unity_to_blender_vec(target_unity_pos)
    u_vec = unity_to_blender_vec(up_unity).normalized()
    forward = (t_pos - c_pos).normalized()
    right = forward.cross(u_vec).normalized()
    true_up = right.cross(forward).normalized()

    rot_mat = Matrix((
        (right.x, true_up.x, -forward.x),
        (right.y, true_up.y, -forward.y),
        (right.z, true_up.z, -forward.z),
    ))
    cam_obj.location = c_pos
    cam_obj.rotation_euler = rot_mat.to_euler()


def render_to_file(filepath, res_x, res_y, transparent=False):
    """Render the current scene camera to filepath at (res_x, res_y)."""
    scene = bpy.context.scene
    scene.render.resolution_x = int(res_x)
    scene.render.resolution_y = int(res_y)
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = bool(transparent)
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    scene.render.filepath = str(filepath)
    bpy.ops.render.render(write_still=True)


def render_orthographic_views_and_turnaround(cam_obj, renders_dir, concept_turnaround_path, cfg):
    """
    Render orthographic front, side, back, and top views at the exact scale of turnaround.png,
    save individual PNGs, and composite compare_turnaround.png side-by-side with turnaround.png.
    """
    cam_data = cam_obj.data
    cam_data.type = "ORTHO"
    ortho_size = cfg["ortho_render_size_px"]
    ortho_scale_m = cfg["ortho_render_scale_m"]
    cam_data.ortho_scale = ortho_scale_m
    px_per_m_render = ortho_size / ortho_scale_m

    target_y = cfg["ortho_target_y"]
    dist = cfg["ortho_cam_distance"]

    raw_arrays = {}
    ortho_specs = (
        ("front", (0.0, target_y, dist), (0.0, target_y, 0.0), (0.0, 1.0, 0.0)),
        ("side", (-dist, target_y, 0.0), (0.0, target_y, 0.0), (0.0, 1.0, 0.0)),
        ("back", (0.0, target_y, -dist), (0.0, target_y, 0.0), (0.0, 1.0, 0.0)),
        ("top", (0.0, dist, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, -1.0)),
    )

    bg_norm = [c / 255.0 for c in cfg["turnaround_bg_srgb"]]
    for vname, c_pos, t_pos, up_v in ortho_specs:
        aim_camera_at(cam_obj, c_pos, t_pos, up_unity=up_v)
        tmp_path = renders_dir / f"_ortho_{vname}_rgba.png"
        render_to_file(tmp_path, ortho_size, ortho_size, transparent=True)
        arr_rgba = load_image_rgba(tmp_path)
        tmp_path.unlink(missing_ok=True)
        if vname == "side":
            # Flip horizontally so the Side view faces right while showing left-flank features as in turnaround.png
            arr_rgba = arr_rgba[:, ::-1, :].copy()
        raw_arrays[vname] = arr_rgba

        card = np.empty((ortho_size, ortho_size, 4), dtype=np.float32)
        card[:, :, 0] = bg_norm[0]
        card[:, :, 1] = bg_norm[1]
        card[:, :, 2] = bg_norm[2]
        card[:, :, 3] = 1.0
        alpha_paste_onto(card, arr_rgba, 0, 0)
        save_image_rgba(renders_dir / f"{vname}.png", card, include_alpha=False)

    tw, th = cfg["turnaround_width_px"], cfg["turnaround_height_px"]
    target_px_per_m = cfg["turnaround_px_per_m"]
    scale_ratio = target_px_per_m / px_per_m_render
    scaled_size = int(round(ortho_size * scale_ratio))

    model_sheet = np.empty((th, tw, 4), dtype=np.float32)
    model_sheet[:, :, 0] = bg_norm[0]
    model_sheet[:, :, 1] = bg_norm[1]
    model_sheet[:, :, 2] = bg_norm[2]
    model_sheet[:, :, 3] = 1.0

    ground_y_px = cfg["turnaround_ground_y_px"]
    cy_scaled = scaled_size * FLAT_UV_SCALE
    ground_row_in_scaled = int(round(cy_scaled + target_y * target_px_per_m))

    view_placements = (
        ("front", cfg["turnaround_front_cx_px"], ground_y_px - ground_row_in_scaled),
        ("side", cfg["turnaround_side_cx_px"], ground_y_px - ground_row_in_scaled),
        ("back", cfg["turnaround_back_cx_px"], ground_y_px - ground_row_in_scaled),
        ("top", cfg["turnaround_top_cx_px"], int(round(cfg["turnaround_top_cy_px"] - cy_scaled))),
    )

    for vname, dest_cx, dest_top_y in view_placements:
        rgba_scaled = resize_rgba_bilinear(raw_arrays[vname], scaled_size, scaled_size)
        dest_left_x = int(round(dest_cx - cy_scaled))
        alpha_paste_onto(model_sheet, rgba_scaled, dest_left_x, dest_top_y)

    concept_rgba = load_image_rgba(concept_turnaround_path)
    comp = np.empty((th * 2, tw, 4), dtype=np.float32)
    comp[:th, :, :] = concept_rgba
    comp[th:, :, :] = model_sheet

    # Add subtle horizontal alignment ticks at Ground (0.0m), Tyre Top (0.70m), Deck (0.91m), Head Top (1.55m)
    for y_m, col_rgb in (
        (0.0, (0.43, 0.49, 0.57)),
        (0.70, (0.37, 0.43, 0.51)),
        (0.91, (0.37, 0.43, 0.51)),
        (1.55, (0.49, 0.55, 0.63)),
    ):
        y_row = int(round(ground_y_px - y_m * target_px_per_m))
        for sheet_offset in (0, th):
            y_abs = sheet_offset + y_row
            if 0 <= y_abs < th * 2:
                for x_tick in range(10, 1020, 16):
                    comp[y_abs, x_tick : x_tick + 6, 0] = col_rgb[0]
                    comp[y_abs, x_tick : x_tick + 6, 1] = col_rgb[1]
                    comp[y_abs, x_tick : x_tick + 6, 2] = col_rgb[2]

    comp[th - 1 : th + 1, :, :3] = 0.38
    save_image_rgba(renders_dir / "compare_turnaround.png", comp, include_alpha=False)


def render_perspective_acceptance_views(cam_obj, rover_root, renders_dir, cfg, mats):
    """Render all required perspective views (matching Round 1's view set) into Logs/renders/."""
    from rover_parts import build_solar_wing_model, build_tape_deck_model

    cam_data = cam_obj.data
    cam_data.type = "PERSP"
    cam_data.lens = cfg["persp_lens_mm"]
    res = cfg["persp_render_size_px"]

    # 1. three_quarter_front.png
    aim_camera_at(cam_obj, (-2.05, 1.38, 2.25), (0.0, 0.78, 0.12))
    render_to_file(renders_dir / "three_quarter_front.png", res, res)

    # 2. three_quarter_back.png (base 07 with empty solar rails)
    aim_camera_at(cam_obj, (-2.10, 1.48, -2.20), (0.0, 0.80, -0.05))
    render_to_file(renders_dir / "three_quarter_back.png", res, res)

    # 3. scoop_folded_vs_dropped.png (1536x768 side-by-side)
    aim_camera_at(cam_obj, (-1.85, 0.95, 1.85), (0.0, 0.48, 0.55))
    folded_path = renders_dir / "_scoop_folded.png"
    render_to_file(folded_path, 768, 768)

    scoop_arms = bpy.data.objects.get("ScoopArms")
    scoop_bkt = bpy.data.objects.get("ScoopBucket")
    if scoop_arms and scoop_bkt:
        scoop_arms.rotation_euler = (math.radians(-18.0), 0.0, 0.0)
        scoop_bkt.rotation_euler = (math.radians(12.0), 0.0, 0.0)
        bpy.context.view_layer.update()
    dropped_path = renders_dir / "_scoop_dropped.png"
    render_to_file(dropped_path, 768, 768)
    if scoop_arms and scoop_bkt:
        scoop_arms.rotation_euler = (0.0, 0.0, 0.0)
        scoop_bkt.rotation_euler = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()

    arr_f = load_image_rgba(folded_path)
    arr_d = load_image_rgba(dropped_path)
    folded_path.unlink(missing_ok=True)
    dropped_path.unlink(missing_ok=True)
    scoop_comp = np.concatenate([arr_f, arr_d], axis=1)
    save_image_rgba(renders_dir / "scoop_folded_vs_dropped.png", scoop_comp, include_alpha=False)

    # 4. hatch_open.png
    up_can = bpy.data.objects.get("UpperCanopy")
    low_ch = bpy.data.objects.get("LowerChute")
    if up_can and low_ch:
        up_can.rotation_euler = (math.radians(52.0), 0.0, 0.0)
        low_ch.rotation_euler = (math.radians(-18.0), 0.0, 0.0)
        bpy.context.view_layer.update()
    aim_camera_at(cam_obj, (-1.65, 1.25, -2.05), (0.0, 0.72, -0.35))
    render_to_file(renders_dir / "hatch_open.png", res, res)
    if up_can and low_ch:
        up_can.rotation_euler = (0.0, 0.0, 0.0)
        low_ch.rotation_euler = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()

    # 5. neck_turned_120.png (Neck yawed 120 deg around Blender Z = Unity Y)
    neck_obj = bpy.data.objects.get("Neck")
    if neck_obj:
        neck_obj.rotation_euler = (0.0, 0.0, math.radians(120.0))
        bpy.context.view_layer.update()
    aim_camera_at(cam_obj, (-1.95, 1.45, -2.10), (0.0, 0.88, 0.0))
    render_to_file(renders_dir / "neck_turned_120.png", res, res)
    if neck_obj:
        neck_obj.rotation_euler = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()

    # 6. with_solar_wing.png (attach temporary SolarWing to RoverModel/SolarWing socket)
    sw_socket = bpy.data.objects.get("SolarWing")
    sw_temp = build_solar_wing_model(cfg, mats, parent=sw_socket, unity_loc=(0.0, 0.0, 0.0))
    aim_camera_at(cam_obj, (1.95, 1.52, -2.05), (0.0, 0.82, -0.10))
    render_to_file(renders_dir / "with_solar_wing.png", res, res)
    for child in list(sw_temp.children):
        bpy.data.objects.remove(child, do_unlink=True)
    sw_mesh = sw_temp.data
    bpy.data.objects.remove(sw_temp, do_unlink=True)
    bpy.data.meshes.remove(sw_mesh, do_unlink=True)

    # 7. with_tape_deck.png (hide DeckCover, attach temporary Kit_TapeDeck to Head/DeckSocket)
    deck_cov = bpy.data.objects.get("DeckCover")
    deck_sock = bpy.data.objects.get("DeckSocket")
    if deck_cov:
        deck_cov.hide_render = True
    td_temp = build_tape_deck_model(cfg, mats, parent=deck_sock, unity_loc=(0.0, 0.0, 0.0))
    aim_camera_at(cam_obj, (1.45, 1.38, 1.45), (0.12, 1.18, 0.28))
    render_to_file(renders_dir / "with_tape_deck.png", res, res)
    for child in list(td_temp.children):
        c_mesh = child.data
        bpy.data.objects.remove(child, do_unlink=True)
        if c_mesh:
            bpy.data.meshes.remove(c_mesh, do_unlink=True)
    td_mesh = td_temp.data
    bpy.data.objects.remove(td_temp, do_unlink=True)
    bpy.data.meshes.remove(td_mesh, do_unlink=True)
    if deck_cov:
        deck_cov.hide_render = False


def render_wireframe_and_vertex_colors(cam_obj, mesh_objects, renders_dir, cfg):
    """Render wireframe.png and vertex_colors.png (showing R=AO, G=wear, B=cavity, and combined RGB)."""
    res = cfg["persp_render_size_px"]
    aim_camera_at(cam_obj, (-2.05, 1.38, 2.25), (0.0, 0.78, 0.12))

    orig_mats = {}
    wire_mat = bpy.data.materials.new(name="_TempWireframeMat")
    wire_mat.use_nodes = True
    wnodes = wire_mat.node_tree.nodes
    wlinks = wire_mat.node_tree.links
    wnodes.clear()
    w_out = wnodes.new(type="ShaderNodeOutputMaterial")
    w_bsdf = wnodes.new(type="ShaderNodeBsdfPrincipled")
    w_wire = wnodes.new(type="ShaderNodeWireframe")
    w_wire.use_pixel_size = True
    w_wire.inputs["Size"].default_value = 1.4
    w_mix = wnodes.new(type="ShaderNodeMix")
    w_mix.data_type = "RGBA"
    w_mix.inputs["A"].default_value = (0.72, 0.75, 0.80, 1.0)
    w_mix.inputs["B"].default_value = (0.12, 0.14, 0.18, 1.0)
    wlinks.new(w_wire.outputs["Fac"], w_mix.inputs["Factor"])
    wlinks.new(w_mix.outputs["Result"], w_bsdf.inputs["Base Color"])
    wlinks.new(w_bsdf.outputs["BSDF"], w_out.inputs["Surface"])

    for obj in mesh_objects:
        orig_mats[obj.name] = list(obj.data.materials)
        obj.data.materials.clear()
        obj.data.materials.append(wire_mat)

    render_to_file(renders_dir / "wireframe.png", res, res)

    vc_mat = bpy.data.materials.new(name="_TempVertexColorMat")
    vc_mat.use_nodes = True
    vnodes = vc_mat.node_tree.nodes
    vlinks = vc_mat.node_tree.links
    vnodes.clear()
    v_out = vnodes.new(type="ShaderNodeOutputMaterial")
    v_em = vnodes.new(type="ShaderNodeEmission")
    v_attr = vnodes.new(type="ShaderNodeAttribute")
    v_attr.attribute_name = COLOR_ATTR_NAME
    v_attr.attribute_type = "GEOMETRY"
    v_sep = vnodes.new(type="ShaderNodeSeparateColor")
    vlinks.new(v_attr.outputs["Color"], v_sep.inputs["Color"])
    vlinks.new(v_em.outputs["Emission"], v_out.inputs["Surface"])

    for obj in mesh_objects:
        obj.data.materials.clear()
        obj.data.materials.append(vc_mat)

    panel_arrays = []
    for ch_name, out_socket in (
        ("R_AO", v_sep.outputs["Red"]),
        ("G_Wear", v_sep.outputs["Green"]),
        ("B_Cavity", v_sep.outputs["Blue"]),
        ("RGB_Combined", v_attr.outputs["Color"]),
    ):
        for l in list(v_em.inputs["Color"].links):
            vlinks.remove(l)
        vlinks.new(out_socket, v_em.inputs["Color"])
        tmp_p = renders_dir / f"_vc_{ch_name}.png"
        render_to_file(tmp_p, 768, 768)
        panel_arrays.append(load_image_rgba(tmp_p))
        tmp_p.unlink(missing_ok=True)

    top_row = np.concatenate([panel_arrays[0], panel_arrays[1]], axis=1)
    bot_row = np.concatenate([panel_arrays[2], panel_arrays[3]], axis=1)
    vc_grid = np.concatenate([top_row, bot_row], axis=0)
    save_image_rgba(renders_dir / "vertex_colors.png", vc_grid, include_alpha=False)

    for obj in mesh_objects:
        obj.data.materials.clear()
        for m in orig_mats.get(obj.name, []):
            obj.data.materials.append(m)

    bpy.data.materials.remove(wire_mat, do_unlink=True)
    bpy.data.materials.remove(vc_mat, do_unlink=True)
