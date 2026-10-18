"""
Deterministic headless Blender generator for the 07 Face Inventory technical line-art diagrams:
  - Assets/_Project/Art/UI/Face/rover_07_line_3q.png   (2048x2048 RGBA, crisp #FFE29E single-weight lines)
  - Assets/_Project/Art/UI/Face/rover_07_line_side.png (2048x2048 RGBA, crisp #FFE29E single-weight lines)
  - Assets/_Project/Art/UI/Face/rover_07_anchors.json  (normalized u,v and pixel coordinates for the 5 device callouts)

Usage:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/render_face_inventory_assets.py
"""

import json
import math
import os
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

SCRIPT_DIR = Path(__file__).resolve().parent
BLENDER_ROOT = SCRIPT_DIR.parent
REPO_ROOT = SCRIPT_DIR.parents[3]

if str(BLENDER_ROOT) not in sys.path:
    sys.path.insert(0, str(BLENDER_ROOT))
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import build_rover
import rover_parts


# 1. Omit the flat top-ortho helper quad under ScoopBucket so it never draws a stray line
rover_parts._append_top_ortho_apron_quad = lambda *a, **kw: None

# 2. Make polyline seam tubes hair-thin (radius=0.0006) so Freestyle renders them as a single-weight stroke
_orig_seam_tube = rover_parts._append_polyline_seam_tube


def _thin_seam_tube(bm, pts_3d, radius=0.004, mat_idx=2, ref_axis=(1.0, 0.0, 0.0)):
    return _orig_seam_tube(bm, pts_3d, radius=0.0006, mat_idx=mat_idx, ref_axis=ref_axis)


rover_parts._append_polyline_seam_tube = _thin_seam_tube


def add_flank_socket_ports(body_obj, cfg, mats):
    """Add two crisp modular equipment socket receptacles on the left flank at socket_1 and socket_2."""
    specs = (
        ("SocketPort_1", (-0.442, 0.765, 0.065), -0.10),
        ("SocketPort_2", (-0.452, 0.545, -0.105), 0.0),
    )
    for name, piv_u, tilt_z in specs:
        bm = bmesh.new()
        rover_parts.append_beveled_box(
            bm,
            size_xyz=(0.016, 0.096, 0.096),
            center_xyz=(0.0, 0.0, 0.0),
            bevel_width=0.014,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(0.0, 0.0, tilt_z),
            guard_inset=False,
        )
        rover_parts.append_cylinder(
            bm,
            radius1=0.032,
            radius2=0.032,
            length=0.022,
            axis="X",
            center_xyz=(-0.004, 0.0, 0.0),
            segments=18,
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=1,
            rot_euler=(0.0, 0.0, tilt_z),
        )
        rover_parts.append_cylinder(
            bm,
            radius1=0.018,
            radius2=0.018,
            length=0.028,
            axis="X",
            center_xyz=(-0.006, 0.0, 0.0),
            segments=14,
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=2,
            rot_euler=(0.0, 0.0, tilt_z),
        )
        rover_parts.create_mesh_node(
            name,
            bm,
            piv_u,
            parent=body_obj,
            mat_list=[mats["R1_GreyMetal"], mats["R1_Brass"], mats["R1_DarkGlass"]],
            cfg=cfg,
        )


def hide_kit_era_dressing():
    """Match RoverPrefabBuilder.HideKitEraDressing(m): remove CellFilled, Decal07Fresh, Pennant."""
    for name in ("CellFilled", "Decal07Fresh", "Pennant"):
        obj = bpy.data.objects.get(name)
        if obj is not None:
            bpy.data.objects.remove(obj, do_unlink=True)


def clean_body_crease_marks():
    """Mark clean feature edges for Freestyle while avoiding multi-line bunching on Body's micro-seams."""
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        me = obj.data
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.edges.ensure_lookup_table()

        is_body = obj.name == "Body"
        is_coil = "Shock_" in obj.name or "WinchSpool" in obj.name

        mark_indices = []
        for e in bm.edges:
            if len(e.link_faces) == 2:
                f0, f1 = e.link_faces[0], e.link_faces[1]
                ang = e.calc_face_angle(0.0)
                if is_body:
                    v0_z = e.verts[0].co.z
                    v1_z = e.verts[1].co.z
                    in_micro_seam = (0.548 <= v0_z <= 0.562 and 0.548 <= v1_z <= 0.562) or (
                        0.646 <= v0_z <= 0.658 and 0.646 <= v1_z <= 0.658
                    )
                    if in_micro_seam:
                        if abs(v0_z - 0.564) < 0.002 and abs(v1_z - 0.564) < 0.002:
                            mark_indices.append(e.index)
                        elif abs(v0_z - 0.644) < 0.002 and abs(v1_z - 0.644) < 0.002:
                            mark_indices.append(e.index)
                        continue
                    if f0.material_index != f1.material_index:
                        mark_indices.append(e.index)
                    elif ang > math.radians(48.0):
                        mark_indices.append(e.index)
                elif is_coil:
                    pass
                else:
                    if f0.material_index != f1.material_index:
                        mark_indices.append(e.index)
                    elif ang > math.radians(44.0):
                        mark_indices.append(e.index)
        bm.to_mesh(me)
        bm.free()
        if mark_indices:
            attr = me.attributes.get("freestyle_edge")
            if attr is None:
                attr = me.attributes.new(name="freestyle_edge", type="BOOLEAN", domain="EDGE")
            mark_set = set(mark_indices)
            for idx in range(len(me.edges)):
                attr.data[idx].value = idx in mark_set
        me.update()


def setup_scene():
    build_rover.reset_scene()
    mats = build_rover.create_material_library(build_rover.MATERIALS, for_render=False)
    cfg = dict(build_rover.DIMENSIONS)
    # Place Antenna on rear-left shoulder (-0.36, 0.91, -0.46) matching rover_parts.py L3040 comment & variant_B.png
    cfg["piv_antenna"] = (-0.36, 0.91, -0.46)
    cfg["piv_sonar_loop_local"] = (0.0, 0.24, 0.0)
    rover_root = rover_parts.build_rover_model_hierarchy(cfg, mats)

    hide_kit_era_dressing()
    body_obj = bpy.data.objects["Body"]
    add_flank_socket_ports(body_obj, cfg, mats)
    clean_body_crease_marks()

    holdout_mat = bpy.data.materials.new(name="HoldoutMat")
    holdout_mat.use_nodes = True
    nodes = holdout_mat.node_tree.nodes
    links = holdout_mat.node_tree.links
    nodes.clear()
    out_node = nodes.new("ShaderNodeOutputMaterial")
    hold_node = nodes.new("ShaderNodeHoldout")
    links.new(hold_node.outputs["Holdout"], out_node.inputs["Surface"])

    for obj in bpy.data.objects:
        if obj.type == "MESH":
            obj.data.materials.clear()
            obj.data.materials.append(holdout_mat)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = True
    scene.render.resolution_x = 2048
    scene.render.resolution_y = 2048
    scene.render.resolution_percentage = 100
    scene.eevee.taa_render_samples = 16

    if hasattr(scene, "view_settings"):
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"

    scene.render.use_freestyle = True
    scene.render.line_thickness_mode = "ABSOLUTE"
    scene.render.line_thickness = 1.0

    vl = scene.view_layers[0]
    vl.use_freestyle = True
    fs = vl.freestyle_settings
    fs.mode = "EDITOR"
    fs.use_smoothness = True
    fs.crease_angle = math.radians(134.0)

    while len(fs.linesets) > 0:
        fs.linesets.remove(fs.linesets[0])

    ls = fs.linesets.new("TechLineSet")
    ls.select_by_visibility = True
    ls.visibility = "VISIBLE"
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_contour = True
    ls.select_crease = True
    ls.select_edge_mark = True
    ls.select_external_contour = True
    ls.select_material_boundary = False
    ls.select_suggestive_contour = False
    ls.edge_type_negation = "INCLUSIVE"
    ls.edge_type_combination = "OR"

    style = ls.linestyle

    def srgb_to_lin(c):
        return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92

    style.color = (srgb_to_lin(1.0), srgb_to_lin(226.0 / 255.0), srgb_to_lin(158.0 / 255.0))
    style.alpha = 1.0
    style.thickness = 3.5
    style.caps = "ROUND"
    style.chaining = "PLAIN"
    style.use_same_object = False

    return scene, rover_root


def get_world_point(unity_xyz):
    return Vector(rover_parts.u_pos(*unity_xyz))


def compute_anchors(scene, cam_obj):
    bpy.context.view_layer.update()
    eye_obj = bpy.data.objects.get("Eye")
    cutter_head = bpy.data.objects.get("CutterHead")
    s1_obj = bpy.data.objects.get("SocketPort_1")
    s2_obj = bpy.data.objects.get("SocketPort_2")
    ant_obj = bpy.data.objects.get("Antenna")

    lens_wp = (eye_obj.matrix_world @ Vector((-0.035, 0.0, 0.0))) if eye_obj else get_world_point((-0.39, 1.20, 0.13))
    cutter_wp = (
        (cutter_head.matrix_world @ Vector((0.0, 0.195, -0.025)))
        if cutter_head
        else get_world_point((0.0, 0.695, 0.785))
    )
    s1_wp = (s1_obj.matrix_world @ Vector((-0.008, 0.0, 0.0))) if s1_obj else get_world_point((-0.448, 0.765, 0.065))
    s2_wp = (s2_obj.matrix_world @ Vector((-0.008, 0.0, 0.0))) if s2_obj else get_world_point((-0.456, 0.545, -0.105))
    ant_wp = (ant_obj.matrix_world @ Vector((0.0, 0.0, 0.38))) if ant_obj else get_world_point((-0.36, 1.29, -0.46))

    pts_world = {
        "lens": lens_wp,
        "cutter_nozzle": cutter_wp,
        "socket_1": s1_wp,
        "socket_2": s2_wp,
        "antenna": ant_wp,
    }

    res_x = scene.render.resolution_x
    res_y = scene.render.resolution_y
    out = {}
    for k, wp in pts_world.items():
        co_2d = world_to_camera_view(scene, cam_obj, wp)
        u = float(co_2d.x)
        v_top = float(1.0 - co_2d.y)
        out[k] = {
            "u": round(u, 4),
            "v": round(v_top, 4),
            "v_bottom_up": round(float(co_2d.y), 4),
            "px": [int(round(u * res_x)), int(round(v_top * res_y))],
        }
    return out


def make_ortho_camera(name, location, target, ortho_scale):
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam_obj = bpy.data.objects.new(name, cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)
    cam_obj.location = Vector(location)
    direction = Vector(target) - cam_obj.location
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return cam_obj


def normalize_amber_alpha_png(png_path):
    """Ensure every pixel has exact #FFE29E sRGB (255, 226, 158) with clean un-fringed alpha for mipmapping."""
    import struct
    import zlib
    import numpy as np

    img = bpy.data.images.load(str(png_path))
    w, h = img.size[0], img.size[1]
    arr = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(arr)
    bpy.data.images.remove(img)

    # Blender image pixels are bottom-to-top; flip vertically for standard top-to-bottom PNG scanlines
    arr = arr.reshape((h, w, 4))[::-1, :, :]
    alpha = np.clip(np.rint(arr[:, :, 3] * 255.0), 0, 255).astype(np.uint8)

    out = np.empty((h, w, 4), dtype=np.uint8)
    out[:, :, 0] = 255
    out[:, :, 1] = 226
    out[:, :, 2] = 158
    out[:, :, 3] = alpha

    # Prepend filter byte 0 (None) to each scanline
    raw_rows = np.hstack([np.zeros((h, 1), dtype=np.uint8), out.reshape((h, w * 4))]).tobytes()

    def make_chunk(chunk_type, data):
        crc = zlib.crc32(chunk_type + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + chunk_type + data + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    idat = zlib.compress(raw_rows, level=9)
    png_bytes = b"\x89PNG\r\n\x1a\n" + make_chunk(b"IHDR", ihdr) + make_chunk(b"IDAT", idat) + make_chunk(b"IEND", b"")
    with open(png_path, "wb") as f:
        f.write(png_bytes)


def deterministic_guid(rel_posix_path):
    import hashlib
    return hashlib.md5(("MoonProject.FaceInventoryArt:" + rel_posix_path).encode("utf-8")).hexdigest()


def write_folder_meta(folder_path, rel_posix_path):
    guid = deterministic_guid(rel_posix_path)
    meta_text = (
        f"fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        f"folderAsset: yes\n"
        f"DefaultImporter:\n"
        f"  externalObjects: {{}}\n"
        f"  userData: \n"
        f"  assetBundleName: \n"
        f"  assetBundleVariant: \n"
    )
    meta_path = Path(str(folder_path) + ".meta")
    if not meta_path.exists():
        meta_path.write_text(meta_text, encoding="utf-8", newline="\n")


def write_json_meta(json_path, rel_posix_path):
    guid = deterministic_guid(rel_posix_path)
    meta_text = (
        f"fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        f"TextScriptImporter:\n"
        f"  externalObjects: {{}}\n"
        f"  userData: \n"
        f"  assetBundleName: \n"
        f"  assetBundleVariant: \n"
    )
    Path(str(json_path) + ".meta").write_text(meta_text, encoding="utf-8", newline="\n")


def write_png_meta(png_path, rel_posix_path, max_size=2048):
    guid = deterministic_guid(rel_posix_path)
    meta_text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: -0.25
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    Path(str(png_path) + ".meta").write_text(meta_text, encoding="utf-8", newline="\n")


def main():
    ui_dir = REPO_ROOT / "Assets" / "_Project" / "Art" / "UI"
    out_dir = ui_dir / "Face"
    out_dir.mkdir(parents=True, exist_ok=True)
    write_folder_meta(ui_dir, "Assets/_Project/Art/UI")
    write_folder_meta(out_dir, "Assets/_Project/Art/UI/Face")

    scene, _ = setup_scene()

    # 1. 3/4 front-left view (shows all 4 wheels including Wheel_RR peeking under the belly, CRT head, cutter, sockets, antenna)
    for obj in bpy.data.objects:
        if obj.name == "Neck":
            obj.rotation_euler = (math.radians(2.0), 0.0, math.radians(-10.0))
        elif obj.name == "CutterTurret":
            obj.rotation_euler = (0.0, 0.0, math.radians(-8.0))
        elif obj.name == "DigScoop":
            obj.rotation_euler = (math.radians(-12.0), 0.0, 0.0)

    cam_3q = make_ortho_camera(
        "Cam_3Q",
        location=(-4.25, 2.85, 1.26),
        target=(0.0, -0.02, 0.77),
        ortho_scale=2.48,
    )
    scene.camera = cam_3q
    path_3q = out_dir / "rover_07_line_3q.png"
    scene.render.filepath = str(path_3q)
    bpy.ops.render.render(write_still=True)
    normalize_amber_alpha_png(path_3q)
    write_png_meta(path_3q, "Assets/_Project/Art/UI/Face/rover_07_line_3q.png", max_size=2048)
    anchors_3q = compute_anchors(scene, cam_3q)

    # 2. Pure left-side view
    for obj in bpy.data.objects:
        if obj.name in ("Neck", "CutterTurret"):
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "DigScoop":
            obj.rotation_euler = (0.0, 0.0, 0.0)
            obj.location.y -= 0.08

    cam_side = make_ortho_camera(
        "Cam_Side",
        location=(-4.0, -0.02, 0.80),
        target=(0.0, -0.02, 0.80),
        ortho_scale=2.24,
    )
    scene.camera = cam_side
    path_side = out_dir / "rover_07_line_side.png"
    scene.render.filepath = str(path_side)
    bpy.ops.render.render(write_still=True)
    normalize_amber_alpha_png(path_side)
    write_png_meta(path_side, "Assets/_Project/Art/UI/Face/rover_07_line_side.png", max_size=2048)
    anchors_side = compute_anchors(scene, cam_side)

    anchors_payload = {
        "image_size": [2048, 2048],
        "coordinate_space": "u is left-to-right [0..1], v is top-to-bottom [0..1] (UI Toolkit top-left origin); v_bottom_up is bottom-to-top [0..1]; px is [x, y] in 2048x2048 pixels.",
        "anchors": anchors_3q,
        "rover_07_line_3q": anchors_3q,
        "rover_07_line_side": anchors_side,
    }
    anchors_path = out_dir / "rover_07_anchors.json"
    with open(anchors_path, "w", encoding="utf-8") as f:
        json.dump(anchors_payload, f, indent=2)
        f.write("\n")
    write_json_meta(anchors_path, "Assets/_Project/Art/UI/Face/rover_07_anchors.json")

    print(f"Saved {path_3q}")
    print(f"Saved {path_side}")
    print(f"Saved {anchors_path}")


if __name__ == "__main__":
    main()
