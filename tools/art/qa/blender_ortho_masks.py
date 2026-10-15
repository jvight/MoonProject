"""
Runs inside headless Blender: renders an FBX's silhouette from orthographic front, side, back and top cameras at a
fixed metres-per-pixel, for concept_compare.py.

    blender -b --factory-startup --python tools/art/qa/blender_ortho_masks.py -- in.fbx out_dir mpp front_axis views

<front_axis> is the direction the model faces in Blender space after import ("+Y", "-X", ...). The views follow a
turnaround sheet:
- front: the camera faces the model's front;
- side: the camera is on the model's right, so the front points to the image's right;
- back: the camera is behind the model;
- top: seen from above, with the front pointing down the image.
Each view is written as mask_<view>.png: white on transparent, no anti-aliasing, the model's bounding box plus a
margin, so one pixel is exactly <mpp> metres.
"""
import math
import sys

import bpy
from mathutils import Vector

MARGIN_PX = 8
UP = Vector((0.0, 0.0, 1.0))
AXES = {"+X": (1, 0, 0), "-X": (-1, 0, 0), "+Y": (0, 1, 0), "-Y": (0, -1, 0)}


def world_bounds():
    points = [obj.matrix_world @ Vector(corner) for obj in bpy.context.scene.objects if obj.type == "MESH"
              for corner in obj.bound_box]
    if not points:
        raise SystemExit("the FBX has no meshes")
    low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    return low, high


def view_frames(front):
    """(camera direction from the model, image up, image right) per view."""
    right = front.cross(UP)
    return {"front": (front, UP, -right), "side": (right, UP, front), "back": (-front, UP, right),
            "top": (UP, -front, -right)}


def extent(low, high, axis):
    return abs(sum((high[i] - low[i]) * abs(axis[i]) for i in range(3)))


def setup_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "SINGLE"
    scene.display.shading.single_color = (1.0, 1.0, 1.0)
    scene.display.render_aa = "OFF"
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "Standard"
    return scene


def render(scene, camera, low, high, name, direction, up, right, mpp, out_dir):
    centre = (low + high) / 2.0
    width_px = math.ceil(extent(low, high, right) / mpp) + 2 * MARGIN_PX
    height_px = math.ceil(extent(low, high, up) / mpp) + 2 * MARGIN_PX
    scene.render.resolution_x = width_px
    scene.render.resolution_y = height_px
    camera.data.ortho_scale = max(width_px, height_px) * mpp
    distance = (high - low).length * 2.0 + 1.0
    camera.location = centre + direction * distance
    camera.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    # to_track_quat picks an up axis; turn about the view axis until the camera's up matches the view's.
    camera_up = camera.rotation_euler.to_matrix() @ Vector((0.0, 1.0, 0.0))
    twist = math.atan2(camera_up.cross(up).dot(-direction), camera_up.dot(up))
    camera.rotation_euler.rotate_axis("Z", -twist)
    camera.data.clip_end = distance * 2.0
    scene.render.filepath = f"{out_dir}/mask_{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"MASK {name} {width_px}x{height_px}")


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 5:
        raise SystemExit("usage: ... -- in.fbx out_dir mpp front_axis front,side,back,top")
    source, out_dir, mpp, front_axis, views = args
    if front_axis not in AXES:
        raise SystemExit(f"front_axis must be one of {', '.join(AXES)}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source)
    for obj in bpy.context.scene.objects:
        obj.hide_render = obj.type != "MESH"
    low, high = world_bounds()
    scene = setup_scene()
    camera = bpy.data.objects.new("QACamera", bpy.data.cameras.new("QACamera"))
    camera.data.type = "ORTHO"
    scene.collection.objects.link(camera)
    scene.camera = camera
    frames = view_frames(Vector(AXES[front_axis]))
    for name in views.split(","):
        direction, up, right = frames[name]
        render(scene, camera, low, high, name, direction, up, right, float(mpp), out_dir)
    print("MASKS_OK")


main()
