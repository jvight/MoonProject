"""
Runs inside headless Blender: imports an FBX and writes the facts model_lint.py judges, as JSON.

    blender -b --factory-startup --python tools/art/qa/blender_inspect.py -- in.fbx facts.json

Coordinates are Blender world space after a default FBX import (Z up, metres), the space agy authors in. Per node:
type, parent, world pivot, world rotation (degrees) and scale. Per mesh: triangles, material slots, colour
attributes, and topology counts:
- non-manifold edges: more than two faces, or loose wire edges;
- boundary edges: open borders, normal on cards and decals;
- inconsistent edges: two faces wind the shared edge the same way, so one of their normals is flipped;
- inverted shells: closed parts whose normals point inward (negative signed volume).
"""
import json
import math
import sys

import bmesh
import bpy

SLOT_SUFFIX_DIGITS = 3


def slot_name(name):
    head, _, tail = name.rpartition(".")
    return head if head and tail.isdigit() and len(tail) == SLOT_SUFFIX_DIGITS else name


def topology(obj):
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    mesh.transform(obj.matrix_world)
    non_manifold = sum(1 for e in mesh.edges if len(e.link_faces) > 2 or not e.link_faces)
    boundary = sum(1 for e in mesh.edges if len(e.link_faces) == 1)
    inconsistent = 0
    for edge in mesh.edges:
        if len(edge.link_faces) != 2:
            continue
        a, b = edge.verts
        directions = []
        for face in edge.link_faces:
            loop = next(l for l in face.loops if l.edge == edge)
            directions.append(loop.vert == a)
        inconsistent += directions[0] == directions[1]
    inverted = 0
    shells = 0
    seen = set()
    for face in mesh.faces:
        if face.index in seen:
            continue
        stack, shell = [face], []
        seen.add(face.index)
        while stack:
            current = stack.pop()
            shell.append(current)
            for edge in current.edges:
                for other in edge.link_faces:
                    if other.index not in seen:
                        seen.add(other.index)
                        stack.append(other)
        closed = all(len(e.link_faces) == 2 for f in shell for e in f.edges)
        if not closed:
            continue
        shells += 1
        volume = sum(f.calc_center_median().dot(f.normal) * f.calc_area() for f in shell) / 3.0
        inverted += volume < 0
    mesh.free()
    return {"non_manifold_edges": non_manifold, "boundary_edges": boundary, "inconsistent_edges": inconsistent,
            "closed_shells": shells, "inverted_shells": inverted}


def mesh_bounds(obj):
    if not obj.data.vertices:
        return None
    mw = obj.matrix_world
    verts = [mw @ v.co for v in obj.data.vertices]
    bmin = [round(min(v[i] for v in verts), 4) for i in range(3)]
    bmax = [round(max(v[i] for v in verts), 4) for i in range(3)]
    return {"min": bmin, "max": bmax, "size": [round(bmax[i] - bmin[i], 4) for i in range(3)]}


def union_bounds(bounds_list):
    valid = [b for b in bounds_list if b is not None]
    if not valid:
        return None
    bmin = [round(min(b["min"][i] for b in valid), 4) for i in range(3)]
    bmax = [round(max(b["max"][i] for b in valid), 4) for i in range(3)]
    return {"min": bmin, "max": bmax, "size": [round(bmax[i] - bmin[i], 4) for i in range(3)]}


def node_facts(obj):
    euler = obj.matrix_world.to_euler()
    facts = {"name": obj.name, "type": obj.type, "parent": obj.parent.name if obj.parent else None,
             "pivot": [round(v, 5) for v in obj.matrix_world.translation],
             "rotation": [round(math.degrees(a), 4) for a in euler],
             "scale": [round(v, 5) for v in obj.matrix_world.to_scale()],
             "local_scale": [round(v, 5) for v in obj.scale]}
    if obj.type == "MESH":
        data = obj.data
        facts["triangles"] = sum(len(p.vertices) - 2 for p in data.polygons)
        facts["slots"] = sorted({slot_name(s.material.name) for s in obj.material_slots if s.material})
        facts["empty_slots"] = sum(1 for s in obj.material_slots if s.material is None)
        facts["color_attributes"] = [a.name for a in data.color_attributes]
        bounds = mesh_bounds(obj)
        if bounds is not None:
            facts["bounds"] = bounds
        facts.update(topology(obj))
    return facts


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise SystemExit("usage: blender -b --factory-startup --python blender_inspect.py -- in.fbx facts.json")
    source, target = args
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source)
    nodes = [node_facts(obj) for obj in sorted(bpy.context.scene.objects, key=lambda o: o.name)]
    bounds = union_bounds([n.get("bounds") for n in nodes if n["type"] == "MESH"])
    payload = {"source": source, "nodes": nodes}
    if bounds is not None:
        payload["bounds"] = bounds
    with open(target, "w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=1)
    print(f"INSPECT_OK {target} {len(nodes)} nodes")


main()
