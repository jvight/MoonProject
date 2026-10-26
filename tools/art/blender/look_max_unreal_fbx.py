"""Turn the look-max Unreal export (scene.obj + poses.json, written by
MoonProject.Art.PlayModeTests.LookMaxSession.Scene_IsExported_ForUnreal) into one FBX for Unreal: the scene's meshes,
a camera per still pose plus the camera move's two ends, and the day sun.

scene.obj is already in Blender's frame (x = Unity z, y = -Unity x, z = Unity y, metres), so it is imported without
axis conversion; the cameras and the sun get the same mapping here. Blender's default FBX axes then land in Unreal at
X = Unity z, Y = Unity x, Z = Unity y (centimetres), the mapping poses.json gives.

D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/look_max_unreal_fbx.py -- <FOLDER>
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def blender_point(unity: list[float]) -> Vector:
    return Vector((unity[2], -unity[0], unity[1]))


def look_rotation(eye: Vector, target: Vector):
    return (target - eye).to_track_quat("-Z", "Y").to_euler()


def add_camera(name: str, eye_unity: list[float], look_unity: list[float], fov_vertical: float) -> None:
    data = bpy.data.cameras.new(name)
    data.sensor_fit = "VERTICAL"
    data.angle = math.radians(fov_vertical)
    data.clip_start = 0.1
    data.clip_end = 5000.0
    camera = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(camera)
    eye = blender_point(eye_unity)
    camera.location = eye
    camera.rotation_euler = look_rotation(eye, blender_point(look_unity))


def add_sun(toward_sun_unity: list[float]) -> None:
    data = bpy.data.lights.new("Sun_DayC", type="SUN")
    sun = bpy.data.objects.new("Sun_DayC", data)
    bpy.context.scene.collection.objects.link(sun)
    toward = blender_point(toward_sun_unity)
    sun.rotation_euler = (-toward).to_track_quat("-Z", "Y").to_euler()


def main() -> None:
    folder = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
    poses = json.loads((folder / "poses.json").read_text(encoding="utf-8"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=str(folder / "scene.obj"), forward_axis="Y", up_axis="Z")
    scene = bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = poses["image"]
    for pose in poses["poses"]:
        add_camera(pose["name"], pose["eye"]["unity"], pose["look"]["unity"], pose["fovVertical"])

    move = poses["move"]
    add_camera(move["name"] + "_start", move["fromEye"]["unity"], move["fromLook"]["unity"], move["fovVertical"])
    add_camera(move["name"] + "_end", move["toEye"]["unity"], move["toLook"]["unity"], move["fovVertical"])
    add_sun(poses["day"]["towardSun"]["unity"])
    out = folder / "lunarion_basin_vista.fbx"
    bpy.ops.export_scene.fbx(filepath=str(out), object_types={"MESH", "CAMERA", "LIGHT"},
                             apply_scale_options="FBX_SCALE_UNITS", mesh_smooth_type="FACE", use_mesh_modifiers=False)
    meshes = sum(1 for o in bpy.data.objects if o.type == "MESH")
    print(f"LOOK-MAX-FBX {out} meshes={meshes} cameras={len(poses['poses']) + 2}")


main()
