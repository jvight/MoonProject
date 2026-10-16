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
    """Ensure imported FBX materials use the canonical R1 Principled BSDF materials + signage/screen textures."""
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
    """Build dark lunar sky, cool sky fill, low warm dawn sun, and sculpted crater-ridge terrain."""
    world = bpy.data.worlds.new("ClusterDawnWorld")
    scene.world = world
    ruins_common.setup_lunar_world_shader(
        world,
        sky_cam_rgba=(0.0028, 0.0048, 0.0140, 1.0),
        sky_fill_rgba=(0.11, 0.15, 0.28, 1.0),
        fill_strength=0.78,
    )

    # Sculpted lunar regolith ground + background crater ridge matching ruins_v1_dawn.png
    bpy.ops.mesh.primitive_grid_add(
        x_subdivisions=96,
        y_subdivisions=96,
        size=96.0,
        location=(0.0, 12.0, 0.0),
    )
    ground = bpy.context.active_object
    ground.name = "LunarRegolithGround"
    for v in ground.data.vertices:
        wx = v.co.x
        wy = v.co.y + 12.0
        # Keep the ruins footprints flat near z=0, and rise into the distant crater rim behind y > 11m
        if wy > 10.5:
            t_ridge = min(1.0, (wy - 10.5) / 18.0)
            # Crater ridge is higher on far left and far right, dipping slightly in the middle behind the radio tower
            saddle = 1.0 + 0.32 * math.sin((wx - 2.0) * 0.11) + 0.22 * ((wx / 22.0) ** 2)
            ridge_h = (t_ridge ** 1.45) * 4.8 * saddle
            ridge_detail = 0.18 * math.sin(wx * 0.45 + wy * 0.25) + 0.10 * math.cos(wx * 0.9 - wy * 0.4)
            v.co.z = ridge_h + ridge_detail * t_ridge
        else:
            # Gentle micro-undulation in open plaza (clamped near 0 under structures)
            v.co.z = 0.025 * math.sin(wx * 0.5) * math.cos(wy * 0.5)

    for poly in ground.data.polygons:
        poly.use_smooth = True
    ground.data.update()

    gmat = bpy.data.materials.new("LunarRegolithMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.18, 0.19, 0.22, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.94
    ground.data.materials.append(gmat)

    # Subtle distant Earth in upper-left sky (`ruins_v1_dawn.png`)
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=32,
        ring_count=16,
        radius=1.85,
        location=(-8.2, 42.0, 13.8),
    )
    earth = bpy.context.active_object
    earth.name = "DistantEarth"
    for poly in earth.data.polygons:
        poly.use_smooth = True
    emat = bpy.data.materials.new("DistantEarthMat")
    emat.use_nodes = True
    ebsdf = next(n for n in emat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    ebsdf.inputs["Base Color"].default_value = (0.36, 0.52, 0.72, 1.0)
    ebsdf.inputs["Roughness"].default_value = 0.85
    if "Emission Color" in ebsdf.inputs:
        ebsdf.inputs["Emission Color"].default_value = (0.18, 0.28, 0.42, 1.0)
    if "Emission Strength" in ebsdf.inputs:
        ebsdf.inputs["Emission Strength"].default_value = 0.35
    earth.data.materials.append(emat)

    # Low grazing warm polar dawn sun (right-to-left grazing angle matching ruins_v1_dawn.png)
    sun_data = bpy.data.lights.new("PolarDawnSun", type="SUN")
    sun_data.energy = 5.4
    sun_data.color = (1.0, 0.75, 0.48)
    sun_data.angle = math.radians(1.0)
    sun_obj = bpy.data.objects.new("PolarDawnSun", sun_data)
    sun_obj.rotation_euler = (math.radians(80.5), math.radians(6.0), math.radians(-54.0))
    scene.collection.objects.link(sun_obj)

    # Cool lunar shadow sky fill
    fill_data = bpy.data.lights.new("PolarSkyFill", type="SUN")
    fill_data.energy = 1.15
    fill_data.color = (0.55, 0.70, 0.95)
    fill_data.angle = math.radians(28.0)
    fill_obj = bpy.data.objects.new("PolarSkyFill", fill_data)
    fill_obj.rotation_euler = (math.radians(46.0), 0.0, math.radians(130.0))
    scene.collection.objects.link(fill_obj)

    # Warm interior workshop & airlock point lights
    for idx, (pt_pos, col, energy) in enumerate([
        ((6.45, -0.35, 1.45), (1.0, 0.66, 0.24), 28.0),   # KenjiWorkshop interior amber lamp
        ((-8.25, 0.35, 1.65), (1.0, 0.72, 0.32), 14.0),   # HabitatModules AirlockPanel screen
    ]):
        pdata = bpy.data.lights.new(f"ClusterPt_{idx}", type="POINT")
        pdata.color = col
        pdata.energy = energy
        pdata.shadow_soft_size = 0.32
        pobj = bpy.data.objects.new(f"ClusterPt_{idx}", pdata)
        pobj.location = Vector(pt_pos)
        scene.collection.objects.link(pobj)


def _populate_cluster_layout() -> None:
    """Place the 5 ruins pieces matching the exact composition of ruins_v1_dawn.png."""
    # 1. Habitat on the left: ModuleA_Main airlock dome on the left, ModuleB & ModuleC extending behind
    _import_ruin_fbx("HabitatModules.fbx", location_bl=(-4.80, 1.50, 0.0), yaw_deg=-10.0)
    # 2. Radio tower behind the plaza on its rocky mound, leaning ~21 deg right against the dark sky
    _import_ruin_fbx("RadioTower.fbx", location_bl=(1.80, 9.20, 0.0), yaw_deg=-8.0)
    # 3. Kenji's workshop on the right, open front portal angled toward the camera
    _import_ruin_fbx("KenjiWorkshop.fbx", location_bl=(6.35, 1.20, 0.0), yaw_deg=-16.0)
    # 4. Fallen solar array in the right foreground, tilted toward the plaza
    _import_ruin_fbx("FallenSolarArray.fbx", location_bl=(2.15, -6.20, 0.0), yaw_deg=-20.0)
    # 5. Scatter set tucked beside the rear-left habitat/plaza perimeter
    _import_ruin_fbx("ScatterSet.fbx", location_bl=(-6.8, 6.8, 0.0), yaw_deg=18.0)


def render_cluster_and_compare() -> Path:
    """Arrange the 5 ruins pieces like ruins_v1_dawn.png and build Logs/renders/compare_concept.png."""
    ruins_common.RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    ruins_common.reset_ruins_scene()
    scene = bpy.context.scene
    # Match the exact 1376x768 (16:9) resolution of ruins_v1_dawn.png
    ruins_common._configure_fast_cycles(scene, 1376, 768)

    _populate_cluster_layout()
    _rebind_imported_r1_materials()
    _setup_dawn_environment(scene)

    cam_data = bpy.data.cameras.new("ClusterCam")
    cam_data.lens = 30.0
    cam_obj = bpy.data.objects.new("ClusterCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    cam_pos = Vector((0.0, -15.2, 2.05))
    target = Vector((0.1, 2.0, 1.85))
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
        target_height=768,
        divider_px=12,
    )
    return compare_path


def render_scale_with_07() -> Path:
    """Render Logs/renders/ruins_scale.png with a 1.48 x 1.59 x 1.76 m reference box for Rover 07."""
    ruins_common.RENDERS_DIR.mkdir(parents=True, exist_ok=True)
    ruins_common.reset_ruins_scene()
    scene = bpy.context.scene
    ruins_common._configure_fast_cycles(scene, 1376, 768)

    _populate_cluster_layout()

    # Build the exact 1.48 x 1.59 x 1.76 m reference box for Rover 07 in the open foreground ground
    # where Rover 07 stands in ruins_v1_dawn.png (between HabitatModules on the left and FallenSolarArray on the right)
    mb_07 = r1kit.MeshBuilder("Rover07_ScaleBox_148x159x176")
    rover_w, rover_h, rover_l = 1.48, 1.59, 1.76
    rc_u = (-2.10, rover_h * 0.5, -6.60)
    mb_07.add_bevel_box(rc_u, (rover_w, rover_h, rover_l), bevel=0.03, mat="R1_CreamEnamel", rot_deg_u=(0.0, 62.0, 0.0), segs=1)
    mb_07.add_bevel_box(
        (-2.10, rover_h * 0.52, -6.60),
        (rover_w + 0.02, 0.22, rover_l + 0.02),
        bevel=0.015,
        mat="R1_OrangeStripe",
        rot_deg_u=(0.0, 62.0, 0.0),
        segs=1,
    )
    obj_07 = ruins_common.to_ruin_object(mb_07, "Rover07_ScaleBox_148x159x176")
    r1kit.apply_triplanar_uvs(obj_07, texel_scale=0.85)
    r1kit._init_solid_vcol(obj_07, (0.94, 0.08, 0.04, 1.0))

    _rebind_imported_r1_materials()
    _setup_dawn_environment(scene)

    cam_data = bpy.data.cameras.new("ScaleCam")
    cam_data.lens = 30.0
    cam_obj = bpy.data.objects.new("ScaleCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    cam_pos = Vector((0.0, -15.2, 2.05))
    target = Vector((0.1, 2.0, 1.85))
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
