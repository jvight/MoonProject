"""
Runs inside headless Blender: converts an R1 FBX into a GLB for the 3D review page.

    blender -b --factory-startup --python tools/art/review/blender_fbx_to_glb.py -- in.fbx out.glb

The GLB carries no vertex colours. glTF multiplies COLOR_0 into the base colour, but R1 vertex colours encode AO,
edge wear and cavity dust (docs/art/r1-pipeline.md), which would darken and tint the review model. Material
names keep their R1 slot (a '.001'-style duplicate is folded into its slot), and transforms are applied. The last
line printed is "REVIEW_GLB <json>" with the model's triangle, mesh and slot counts.
"""
import json
import re
import sys

import bpy

SLOT_SUFFIX = re.compile(r"\.\d{3}$")


def fold_duplicate_slots():
    """Points every material slot at the material named after its R1 slot, renaming one when none has the name."""
    by_slot = {}
    for material in bpy.data.materials:
        slot = SLOT_SUFFIX.sub("", material.name)
        if material.name == slot or slot not in by_slot:
            by_slot[slot] = material
    for slot, material in by_slot.items():
        if material.name != slot:
            material.name = slot
    for obj in bpy.data.objects:
        for material_slot in getattr(obj, "material_slots", []):
            if material_slot.material is not None:
                material_slot.material = by_slot[SLOT_SUFFIX.sub("", material_slot.material.name)]


def stats():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    triangles = sum(len(poly.vertices) - 2 for obj in meshes for poly in obj.data.polygons)
    slots = sorted({slot.material.name for obj in meshes for slot in obj.material_slots if slot.material})
    return {"triangles": triangles, "meshes": len(meshes), "slots": slots}


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise SystemExit("usage: blender -b --factory-startup --python blender_fbx_to_glb.py -- in.fbx out.glb")
    source, target = args
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source)
    fold_duplicate_slots()
    bpy.ops.export_scene.gltf(filepath=target, export_format="GLB", export_apply=True,
                              export_vertex_color="NONE", export_all_vertex_colors=False,
                              export_active_vertex_color_when_no_material=False)
    print("REVIEW_GLB " + json.dumps(stats()))


main()
