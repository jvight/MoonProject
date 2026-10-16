"""Render the R1 Lumen Station ruins cluster comparison (`compare_concept.png`) and 07 scale shot (`ruins_scale.png`).

Imports the 5 exported FBXs from Assets/_Project/Art/R1/Models/Ruins/:
- HabitatModules.fbx
- RadioTower.fbx
- KenjiWorkshop.fbx
- FallenSolarArray.fbx
- ScatterSet.fbx
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402

CONCEPT_DAWN_PATH = Path(r"D:\Project\MoonProject-wt\agy-concepts2\Logs\concepts\ruins_v1_dawn.png")


def _import_ruin_fbx(fbx_name: str, location_bl: tuple[float, float, float], yaw_deg: float = 0.0) -> bpy.types.Object:
    fbx_path = ruins_common.RUINS_FBX_DIR / fbx_name
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx_path))
    imported = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in imported if o.parent is None]
    root = roots[0] if roots else imported[0]
    root.location = Vector(location_bl)
    root.rotation_euler = Euler((0.0, 0.0, math.radians(yaw_deg)), "XYZ")
    return root


def _rebind_imported_r1_materials() -> None:
    """Ensure imported FBX materials use the canonical R1 Principled BSDF materials + signage texture."""
    ruins_common.register_ruins_materials()
    canonical = r1kit.ensure_materials(use_vertex_color_shading=True)
    ruins_common.attach_signage_texture_for_render(use_vertex_color_shading=True)

    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.data is None:
            continue
        for slot in obj.material_slots:
            if slot.material is None:
                continue
            base_name = slot.material.name.split(".")[0]
            if base_name in canonical:
                slot.material = canonical[base_name]


def _setup_dawn_environment(scene: bpy.types.Scene) -> None:
    world = bpy.data.worlds.new("ClusterDawnWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.14, 0.16, 0.23, 1.0)
        bg.inputs["Strength"].default_value = 0.70

    # Lunar cratered regolith ground plane
    bpy.ops.mesh.primitive_plane_add(size=160.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "LunarRegolithGround"
    gmat = bpy.data.materials.new("LunarRegolithMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.21, 0.22, 0.25, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.93
    ground.data.materials.append(gmat)

    # Low grazing warm polar dawn sun (right-to-left grazing angle matching ruins_v1_dawn.png)
    sun_data = bpy.data.lights.new("PolarDawnSun", type="SUN")
    sun_data.energy = 5.2
    sun_data.color = (1.0, 0.76, 0.52)
    sun_data.angle = math.radians(1.0)
    sun_obj = bpy.data.objects.new("PolarDawnSun", sun_data)
    sun_obj.rotation_euler = (math.radians(80.0), math.radians(6.0), math.radians(-52.0))
    scene.collection.objects.link(sun_obj)

    # Cool lunar shadow sky fill
    fill_data = bpy.data.lights.new("PolarSkyFill", type="SUN")
    fill_data.energy = 1.25
    fill_data.color = (0.58, 0.70, 0.94)
    fill_data.angle = math.radians(28.0)
    fill_obj = bpy.data.objects.new("PolarSkyFill", fill_data)
    fill_obj.rotation_euler = (math.radians(46.0), 0.0, math.radians(130.0))
    scene.collection.objects.link(fill_obj)

    # Warm interior workshop & airlock point lights
    for idx, (pt_pos, col, energy) in enumerate([
        ((6.8, -0.5, 2.2), (1.0, 0.68, 0.26), 95.0),     # KenjiWorkshop interior amber lamp
        ((-1.2, -2.5, 1.9), (0.35, 0.95, 0.68), 35.0),   # HabitatModules AirlockPanel screen
    ]):
        pdata = bpy.data.lights.new(f"ClusterPt_{idx}", type="POINT")
        pdata.color = col
        pdata.energy = energy
        pdata.shadow_soft_size = 0.45
        pobj = bpy.data.objects.new(f"ClusterPt_{idx}", pdata)
        pobj.location = Vector(pt_pos)
        scene.collection.objects.link(pobj)


def render_cluster_and_compare() -> Path:
    """Arrange the 5 ruins pieces like ruins_v1_dawn.png and build Logs/renders/compare_concept.png."""
    ruins_common.RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    ruins_common.reset_ruins_scene()
    scene = bpy.context.scene
    ruins_common._configure_fast_cycles(scene, 1024, 1024)

    _import_ruin_fbx("HabitatModules.fbx", location_bl=(-2.8, 0.5, 0.0), yaw_deg=14.0)
    _import_ruin_fbx("RadioTower.fbx", location_bl=(2.4, 4.2, 0.0), yaw_deg=-18.0)
    _import_ruin_fbx("KenjiWorkshop.fbx", location_bl=(7.2, -0.2, 0.0), yaw_deg=-28.0)
    _import_ruin_fbx("FallenSolarArray.fbx", location_bl=(-4.6, -5.2, 0.0), yaw_deg=22.0)
    _import_ruin_fbx("ScatterSet.fbx", location_bl=(2.2, -4.6, 0.0), yaw_deg=12.0)

    _rebind_imported_r1_materials()
    _setup_dawn_environment(scene)

    cam_data = bpy.data.cameras.new("ClusterCam")
    cam_data.lens = 38.0
    cam_obj = bpy.data.objects.new("ClusterCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    cam_pos = Vector((1.8, -16.8, 5.2))
    target = Vector((0.8, -0.4, 1.5))
    cam_obj.location = cam_pos
    cam_obj.rotation_euler = (target - cam_pos).to_track_quat("-Z", "Y").to_euler()

    cluster_path = ruins_common.RENDERS_DIR / "ruins_cluster_dawn.png"
    scene.render.filepath = str(cluster_path)
    bpy.ops.render.render(write_still=True)

    compare_path = ruins_common.RENDERS_DIR / "compare_concept.png"
    left_img_path = CONCEPT_DAWN_PATH if CONCEPT_DAWN_PATH.exists() else cluster_path
    r1kit.compose_side_by_side(
        left_path=left_img_path,
        right_path=cluster_path,
        out_path=compare_path,
        target_height=1024,
        divider_px=12,
    )
    return compare_path


def render_scale_with_07() -> Path:
    """Render Logs/renders/ruins_scale.png with a 1.48 x 1.59 x 1.76 m reference box for Rover 07."""
    ruins_common.RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    ruins_common.reset_ruins_scene()
    scene = bpy.context.scene
    ruins_common._configure_fast_cycles(scene, 1280, 720)

    _import_ruin_fbx("HabitatModules.fbx", location_bl=(-4.2, 2.2, 0.0), yaw_deg=8.0)
    _import_ruin_fbx("RadioTower.fbx", location_bl=(2.8, 3.6, 0.0), yaw_deg=-10.0)
    _import_ruin_fbx("KenjiWorkshop.fbx", location_bl=(7.6, 1.2, 0.0), yaw_deg=-22.0)
    _import_ruin_fbx("FallenSolarArray.fbx", location_bl=(-4.8, -3.8, 0.0), yaw_deg=15.0)
    _import_ruin_fbx("ScatterSet.fbx", location_bl=(3.8, -2.8, 0.0), yaw_deg=10.0)

    # Build the exact 1.48 x 1.59 x 1.76 m reference box for Rover 07 at (0.0, 0.0, -2.2) in Unity space
    mb_07 = r1kit.MeshBuilder("Rover07_ScaleBox_148x159x176")
    rover_w, rover_h, rover_l = 1.48, 1.59, 1.76
    rc_u = (0.0, rover_h * 0.5, -2.2)
    mb_07.add_bevel_box(rc_u, (rover_w, rover_h, rover_l), bevel=0.025, mat="R1_OrangeStripe", segs=1)
    mb_07.add_bevel_box((0.0, rover_h * 0.72, -2.2), (rover_w + 0.02, 0.28, rover_l + 0.02), bevel=0.01, mat="R1_CreamEnamel", segs=1)
    ruins_common.add_signage_plate(
        mb_07,
        center_u=(0.0, rover_h * 0.72, -2.2 - rover_l * 0.5 - 0.015),
        width=0.42,
        height=0.24,
        cell_name="PICT_ROVER",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    obj_07 = ruins_common.to_ruin_object(mb_07, "Rover07_ScaleBox_148x159x176")
    r1kit.apply_triplanar_uvs(obj_07, texel_scale=0.85)
    ruins_common.apply_signage_uvs(obj_07)
    r1kit._init_solid_vcol(obj_07, (0.94, 0.08, 0.04, 1.0))

    _rebind_imported_r1_materials()
    _setup_dawn_environment(scene)

    cam_data = bpy.data.cameras.new("ScaleCam")
    cam_data.lens = 36.0
    cam_obj = bpy.data.objects.new("ScaleCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    cam_pos = Vector((0.8, -13.2, 3.8))
    target = Vector((0.6, -0.5, 1.4))
    cam_obj.location = cam_pos
    cam_obj.rotation_euler = (target - cam_pos).to_track_quat("-Z", "Y").to_euler()

    scale_path = ruins_common.RENDERS_DIR / "ruins_scale.png"
    scene.render.filepath = str(scale_path)
    bpy.ops.render.render(write_still=True)
    return scale_path


def build() -> None:
    comp_path = render_cluster_and_compare()
    scale_path = render_scale_with_07()
    print(f"[ruins_renders] Rendered {comp_path.name} and {scale_path.name}")


if __name__ == "__main__":
    build()
