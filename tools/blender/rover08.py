"""Builds rover 08 (docs/design/gdd-v3.md §13.3) in Blender and exports it as FBX for Unity.

The script is the source of truth: it starts from an empty scene, so the same script, palette and Blender version give
the same bytes. Art's Rover08ModelBuilder runs it headless and imports the result:

    blender -b --factory-startup --python tools/blender/rover08.py -- --palette <palette.json> --fbx <out.fbx>
        [--preview <out.png>]

08 is 07's younger sibling: the same six wheels on rocker-bogies, rounded radio-cabinet body, slender neck and one
round lens, but clean. No rust, no patch, no dust, all wheels matching, a straight antenna, fresh cream "08" paint,
an intact pair of folded solar wings where 07 has one tired wing, and a young cyan eye where 07's is warm amber.

Every face carries one palette swatch as its material index (index == PaletteSwatch value). Before export that
becomes UV0 at the swatch's cell centre (Palette.Uv), a baked Cycles AO term becomes UV1.x (the occlusion LofiLit
reads), and the materials are dropped, so Unity sees one submesh per part painted by M_LowPoly.

Axes: built with +X right, +Y forward, +Z up, in 07's metres, then scaled by SCALE; the export maps Blender +Y to
Unity +Z. Each part's origin is its pivot (wheel centre, bogie hinge, neck base, head hinge, wing hinge).
"""

import argparse
import datetime
import hashlib
import json
import math
import os
import sys

import bmesh
import bpy
from io_scene_fbx import export_fbx_bin, fbx_utils
from mathutils import Matrix, Vector

# 08 against 07's metres: a hand smaller, a younger sibling rather than a child.
SCALE = 0.9

# Cycles AO bake: reach (metres, after SCALE), samples, and how far a fully occluded corner leans toward the shader's
# occlusion tint. Values are quantized so float noise can never change the exported bytes.
AO_DISTANCE = 0.45
AO_SAMPLES = 64
AO_STRENGTH = 0.55
AO_STEPS = 32

# Pinned FBX creation time: Blender's exporter stamps now() into the header otherwise.
FBX_TIME = datetime.datetime(2026, 10, 11, 0, 0, 0)

WHEEL_RADIUS = 0.35
WHEEL_TRACK = 0.64
WHEEL_BASE = 0.8
LID_TOP = 0.95
BOGIE_HINGE = Vector((0.48, 0.12, 0.66))
NECK_BASE = Vector((0.0, 0.30, LID_TOP))
HEAD_HINGE = NECK_BASE + Vector((0.0, 0.07, 0.27))
EYE_CENTRE = Vector((0.0, 0.21, 0.16))
WING_HINGE = Vector((0.18, 0.06, LID_TOP + 0.012))
ANTENNA_BASE = Vector((0.38, -0.52, LID_TOP))
ANTENNA_LENGTH = 0.56


class Palette:
    """Swatch indices and colours read from the JSON Art's builder writes from Palette.cs."""

    def __init__(self, path):
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)
        self.columns = data["columns"]
        self.rows = data["rows"]
        self.index = {entry["name"]: entry["index"] for entry in data["swatches"]}
        self.srgb = {entry["index"]: entry["srgb"] for entry in data["swatches"]}
        self.count = len(data["swatches"])

    def __getitem__(self, name):
        if name not in self.index:
            raise KeyError(f"rover08: swatch {name} is not in the palette")
        return self.index[name]

    def uv(self, index):
        return ((index % self.columns + 0.5) / self.columns, (index // self.columns + 0.5) / self.rows)

    def linear(self, index):
        return tuple(_to_linear(int(self.srgb[index][i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def _to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


class Kit:
    """Builds pieces as objects that all share one material list, so a face's material index is its swatch."""

    def __init__(self, palette):
        self.palette = palette
        self.materials = []
        for index in range(palette.count):
            material = bpy.data.materials.new(f"Swatch{index:02d}")
            material.diffuse_color = (*palette.linear(index), 1.0)
            self.materials.append(material)

    def piece(self, name, build, swatch, matrix=Matrix.Identity(4)):
        """A new object from bmesh geometry made by build(bm); every face painted swatch (None: build paints)."""
        bm = bmesh.new()
        build(bm)
        bm.transform(matrix)
        if swatch is not None:
            index = self.palette[swatch]
            for face in bm.faces:
                face.material_index = index
        return self._object(name, bm)

    def _object(self, name, bm):
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        for material in self.materials:
            mesh.materials.append(material)
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        return obj

    def part(self, name, pieces, pivot, parent=None):
        """Applies every piece's modifiers, merges them into one object whose origin is pivot (world)."""
        depsgraph = bpy.context.evaluated_depsgraph_get()
        bm = bmesh.new()
        for piece in pieces:
            evaluated_object = piece.evaluated_get(depsgraph)
            evaluated = bpy.data.meshes.new_from_object(evaluated_object)
            evaluated.transform(evaluated_object.matrix_world)
            bm.from_mesh(evaluated)
            bpy.data.meshes.remove(evaluated)
        for piece in pieces:
            data = piece.data
            bpy.data.objects.remove(piece)
            bpy.data.meshes.remove(data)
        bm.transform(Matrix.Translation(-pivot))
        obj = self._object(name, bm)
        obj.location = pivot
        if parent is not None:
            obj.parent = parent
            obj.location = pivot - parent_pivot(parent)
        return obj


def parent_pivot(obj):
    total = Vector(obj.location)
    while obj.parent is not None:
        obj = obj.parent
        total += obj.location
    return total


def box(size):
    def build(bm):
        bmesh.ops.create_cube(bm, size=1.0)
        bm.transform(Matrix.Diagonal((*size, 1.0)))
    return build


def cylinder(radius, depth, sides, top_radius=None):
    """Along +Z, centred on the origin."""
    def build(bm):
        bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius,
                              radius2=radius if top_radius is None else top_radius, depth=depth)
    return build


def ico(radius, subdivisions=0):
    def build(bm):
        bmesh.ops.create_icosphere(bm, subdivisions=subdivisions + 1, radius=radius)
    return build


def at(location, rotation=Matrix.Identity(3)):
    return Matrix.Translation(location) @ rotation.to_4x4()


ALONG_X = Matrix.Rotation(math.radians(90.0), 3, "Y")
ALONG_Y = Matrix.Rotation(math.radians(-90.0), 3, "X")


def bevel(obj, width, segments=1, weighted=False, angle=40.0):
    modifier = obj.modifiers.new("Bevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "WEIGHT" if weighted else "ANGLE"
    modifier.angle_limit = math.radians(angle)
    modifier.harden_normals = False
    return obj


def round_corners(obj, radius, segments=3):
    """Bevel-weights the box's vertical edges so a weighted bevel rounds them like 07's cabinet corners."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    weight = bm.edges.layers.float.new("bevel_weight_edge")
    for edge in bm.edges:
        direction = edge.verts[1].co - edge.verts[0].co
        if abs(direction.normalized().z) > 0.99:
            edge[weight] = 1.0
    bm.to_mesh(obj.data)
    bm.free()
    return bevel(obj, radius, segments, weighted=True)


def repaint_facing(obj, direction, min_dot, swatch_index):
    for polygon in obj.data.polygons:
        if polygon.normal.dot(direction) >= min_dot:
            polygon.material_index = swatch_index


def strut(kit, name, start, end, section, swatch):
    """A box from start to end with the given (width, height) cross section."""
    axis = end - start
    rotation = axis.to_track_quat("Y", "Z").to_matrix()
    return kit.piece(name, box((section[0], axis.length + section[1], section[1])), swatch,
                     at((start + end) * 0.5, rotation))


def mirror_x(obj):
    """Mirrors a finished part's mesh across its own YZ plane (left from right), keeping faces outward."""
    obj.data.transform(Matrix.Diagonal((-1.0, 1.0, 1.0, 1.0)))
    obj.data.flip_normals()
    obj.data.update()


def build_body(kit):
    pieces = [bevel(kit.piece("Belly", box((0.72, 1.12, 0.06)), "Charcoal", at((0, 0.03, 0.31))), 0.02)]
    tub = kit.piece("Tub", box((0.88, 1.30, 0.50)), "Enamel", at((0, 0.03, 0.59)))
    pieces.append(bevel(round_corners(tub, 0.16), 0.04))
    pieces.append(round_corners(kit.piece("Stripe", box((0.888, 1.308, 0.10)), "WarmAccent", at((0, 0.03, 0.75))),
                                0.164))
    lid = kit.piece("Lid", box((0.94, 1.36, 0.06)), "Enamel", at((0, 0.03, 0.87)))
    pieces.append(bevel(round_corners(lid, 0.19), 0.015))
    cushion = kit.piece("Cushion", box((0.84, 1.26, 0.05)), "Enamel", at((0, 0.03, 0.925)))
    pieces.append(bevel(round_corners(cushion, 0.15), 0.03))

    pieces.append(kit.piece("Grille", box((0.42, 0.02, 0.2)), "Metal", at((0, 0.685, 0.60))))
    for i in range(3):
        pieces.append(kit.piece(f"Slat{i}", box((0.32, 0.014, 0.014)), "Charcoal",
                                at((0, 0.698, 0.55 + i * 0.05))))
    pieces.append(kit.piece("LampHood", cylinder(0.075, 0.03, 10), "Charcoal", at((0, 0.69, 0.44), ALONG_Y)))
    pieces.append(kit.piece("LampGlass", cylinder(0.055, 0.02, 10), "WarmLamp", at((0, 0.705, 0.44), ALONG_Y)))
    pieces.append(kit.piece("RearPanel", box((0.5, 0.02, 0.22)), "Metal", at((0, -0.625, 0.60))))
    for side in (-1.0, 1.0):
        for i in range(6):
            pieces.append(kit.piece(f"Rivet{side:+.0f}{i}", ico(0.012), "Metal",
                                    at((side * 0.455, -0.5 + i * 0.2, 0.87))))
        pieces.append(kit.piece(f"HingeCap{side:+.0f}", cylinder(0.06, 0.03, 10), "Metal",
                                at((side * 0.452, BOGIE_HINGE.y, BOGIE_HINGE.z), ALONG_X)))
        flank = Matrix(((0, side, 0), (0, 0, 1), (side, 0, 0))).transposed()
        pieces.append(serial(kit, f"Serial{side:+.0f}", at((side * 0.448, -0.11, 0.75), flank)))
    back = Matrix(((1, 0, 0), (0, 0, 1), (0, -1, 0))).transposed()
    pieces.append(serial(kit, "SerialBack", at((0, -0.628, 0.75), back)))
    return kit.part("Body", pieces, Vector((0, 0, 0)))


def serial(kit, name, placement):
    """Fresh cream "08" painted on the stripe, set in Blender's own font; placement turns the text's +Z outward."""
    curve = bpy.data.curves.new("Serial", "FONT")
    curve.body = "08"
    curve.size = 0.12
    curve.extrude = 0.004
    curve.resolution_u = 2
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    holder = bpy.data.objects.new("SerialText", curve)
    bpy.context.scene.collection.objects.link(holder)
    text = bpy.data.meshes.new_from_object(holder.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    bpy.data.objects.remove(holder)
    bpy.data.curves.remove(curve)
    obj = kit.piece(name, text_builder(text), "Cream", placement)
    bpy.data.meshes.remove(text)
    return obj


def text_builder(mesh):
    def build(bm):
        bm.from_mesh(mesh)
    return build


def build_bogie(kit):
    """The right rocker-bogie in its hinge's space (arms in the x = 0 plane, axle stubs out to the wheels)."""
    hinge = Vector((0, 0, 0))
    front = Vector((0, WHEEL_BASE - BOGIE_HINGE.y, WHEEL_RADIUS - BOGIE_HINGE.z))
    middle = Vector((0, -BOGIE_HINGE.y, WHEEL_RADIUS - BOGIE_HINGE.z))
    rear = Vector((0, -WHEEL_BASE - BOGIE_HINGE.y, WHEEL_RADIUS - BOGIE_HINGE.z))
    pivot = Vector((0, -0.4 - BOGIE_HINGE.y, 0.6 - BOGIE_HINGE.z))
    section = (0.04, 0.055)
    pieces = [strut(kit, "ArmFront", hinge, front, section, "Charcoal"),
              strut(kit, "ArmBack", hinge, pivot, section, "Charcoal"),
              strut(kit, "ArmMiddle", pivot, middle, section, "Charcoal"),
              strut(kit, "ArmRear", pivot, rear, section, "Charcoal"),
              kit.piece("HingeBoss", cylinder(0.05, 0.07, 10), "Metal", at(hinge, ALONG_X)),
              kit.piece("PivotBoss", cylinder(0.04, 0.06, 8), "Metal", at(pivot, ALONG_X))]
    inner = WHEEL_TRACK - BOGIE_HINGE.x
    for i, axle in enumerate((front, middle, rear)):
        pieces.append(kit.piece(f"Axle{i}", cylinder(0.03, inner + 0.03, 8), "Metal",
                                at((inner * 0.5, axle.y, axle.z), ALONG_X)))
    for piece in pieces:
        piece.location += BOGIE_HINGE
    return kit.part("Bogie_R", pieces, BOGIE_HINGE)


def build_wheel(kit, name):
    """A right wheel (hub facing +X) at the origin: twelve faceted lugs on a chamfered tyre, cream hub."""
    half = 0.11
    tyre = bevel(kit.piece("Tyre", cylinder(0.335, 2 * half, 12), "Charcoal", at((0, 0, 0), ALONG_X)), 0.035)
    pieces = [tyre]
    for i in range(12):
        angle = (i + 0.5) * math.tau / 12
        rotation = Matrix.Rotation(angle, 3, "X")
        pieces.append(kit.piece(f"Lug{i}", box((0.2, 0.06, 0.035)), "Charcoal",
                                at(rotation @ Vector((0, 0, 0.335)), rotation)))
    pieces.append(kit.piece("Hub", cylinder(0.22, 0.03, 12), "Cream", at((half + 0.012, 0, 0), ALONG_X)))
    pieces.append(kit.piece("Cap", cylinder(0.09, 0.036, 8, top_radius=0.06), "Metal",
                            at((half + 0.045, 0, 0), ALONG_X)))
    pieces.append(kit.piece("Bolt", ico(0.02), "WarmAccent", at((half + 0.03, 0, 0.16))))
    pieces.append(kit.piece("Inner", cylinder(0.16, 0.02, 10), "Metal", at((-half - 0.008, 0, 0), ALONG_X)))
    return kit.part(name, pieces, Vector((0, 0, 0)))


def build_neck(kit):
    pieces = [kit.piece("Collar", cylinder(0.09, 0.04, 10), "Charcoal", at(NECK_BASE + Vector((0, 0, 0.02)))),
              kit.piece("Post", cylinder(0.045, 0.26, 8), "Metal", at(NECK_BASE + Vector((0, 0, 0.15)))),
              kit.piece("Yoke", box((0.16, 0.08, 0.04)), "Charcoal", at(NECK_BASE + Vector((0, 0.04, 0.26))))]
    return kit.part("Neck", pieces, NECK_BASE)


def build_head(kit, neck):
    """Hooded sensor head in the hinge's space; a boolean cuts the lens recess, a mirror pairs the ear pods."""
    shell = bevel(kit.piece("Shell", box((0.54, 0.42, 0.40)), "Enamel", at((0, 0, 0.2))), 0.12, segments=2)
    recess = kit.piece("Recess", cylinder(0.165, 0.2, 16), "Charcoal", at(EYE_CENTRE + Vector((0, 0.1, 0)), ALONG_Y))
    cut = shell.modifiers.new("Recess", "BOOLEAN")
    cut.operation = "DIFFERENCE"
    cut.solver = "EXACT"
    cut.material_mode = "INDEX"
    cut.object = recess
    recess.hide_render = True

    hood = bevel(kit.piece("Hood", box((0.6, 0.46, 0.22)), "Enamel", at((0, -0.01, 0.3))), 0.08)
    repaint_facing(hood, Vector((0, 0, -1)), 0.9, kit.palette["Charcoal"])
    # Small metal hinge bolts, never dark discs: a dark round pod beside the lens reads as a second eye.
    ear = kit.piece("Ear", cylinder(0.035, 0.02, 8), "Metal", at((0.272, -0.03, 0.19), ALONG_X))
    pair = ear.modifiers.new("Pair", "MIRROR")
    pair.use_axis[0] = True
    back = kit.piece("BackPanel", box((0.3, 0.04, 0.2)), "Metal", at((0, -0.215, 0.19)))
    tab = kit.piece("Tab", box((0.09, 0.02, 0.04)), "WarmAccent", at((-0.16, -0.205, 0.335)))
    pieces = [shell, hood, ear, back, tab]
    for piece in pieces + [recess]:
        piece.location += HEAD_HINGE
    head = kit.part("Head", pieces, HEAD_HINGE, neck)
    cutter = recess.data
    bpy.data.objects.remove(recess)
    bpy.data.meshes.remove(cutter)

    eye_world = HEAD_HINGE + EYE_CENTRE
    eye_pieces = [kit.piece("Bezel", cylinder(0.155, 0.06, 16), "Metal", at(eye_world + Vector((0, -0.03, 0)), ALONG_Y)),
                  kit.piece("Ring", cylinder(0.135, 0.03, 16), "Charcoal", at(eye_world + Vector((0, 0.005, 0)), ALONG_Y)),
                  kit.piece("Lens", cylinder(0.12, 0.02, 16, top_radius=0.09), "EyeGlass",
                            at(eye_world + Vector((0, 0.03, 0)), ALONG_Y))]
    kit.part("Eye", eye_pieces, eye_world, head)
    return head


def build_wing(kit, side):
    """One folded solar wing in its hinge's space: a 2 x 4 grid of cells inset in a metal frame, solidified."""
    p = kit.palette
    hinge = Vector((side * WING_HINGE.x, WING_HINGE.y, WING_HINGE.z))

    def panel(bm):
        bmesh.ops.create_grid(bm, x_segments=2, y_segments=4, size=0.5)
        bm.transform(Matrix.Diagonal((0.30, 0.62, 1.0, 1.0)))
        bm.transform(Matrix.Translation((0, -0.31, 0)))
        cells = list(bm.faces)
        frame = bmesh.ops.inset_individual(bm, faces=cells, thickness=0.016, depth=0.0)["faces"]
        for face in cells:
            face.material_index = p["SkyHorizon"]
        for face in frame:
            face.material_index = p["Metal"]

    cells = kit.piece(f"Cells{side:+.0f}", panel, None, at(hinge))
    shell = cells.modifiers.new("Thickness", "SOLIDIFY")
    shell.thickness = 0.012
    shell.offset = 1.0
    bar = kit.piece(f"Hinge{side:+.0f}", cylinder(0.012, 0.3, 6), "Charcoal", at(hinge, ALONG_X))
    wing = kit.part("SolarWing_R" if side > 0 else "SolarWing_L", [cells, bar], hinge)
    for polygon in wing.data.polygons:
        if polygon.normal.z < 0.9 and polygon.material_index == p["SkyHorizon"]:
            polygon.material_index = p["Metal"]
    return wing


def build_antenna(kit):
    """A straight whip (07's is bent) on a dark mount; the tip is its own node so it can blink the call sign."""
    pieces = [kit.piece("Mount", cylinder(0.03, 0.04, 8), "Charcoal", at(ANTENNA_BASE + Vector((0, 0, 0.02)))),
              kit.piece("Whip", cylinder(0.007, ANTENNA_LENGTH, 6), "Metal",
                        at(ANTENNA_BASE + Vector((0, 0, 0.04 + ANTENNA_LENGTH * 0.5))))]
    antenna = kit.part("Antenna", pieces, ANTENNA_BASE)
    tip = ANTENNA_BASE + Vector((0, 0, 0.04 + ANTENNA_LENGTH + 0.012))
    kit.part("AntennaTip", [kit.piece("Tip", ico(0.018, 1), "PilotLight", at(tip))], tip, antenna)
    return antenna


def build_rover(kit):
    build_body(kit)
    bogie_right = build_bogie(kit)
    bogie_left = kit.part("Bogie_L", [_copy(bogie_right)], bogie_right.location.copy())
    mirror_x(bogie_left)
    bogie_left.location.x = -BOGIE_HINGE.x
    wheel_right = build_wheel(kit, "WheelRight")
    for row, z in (("F", WHEEL_BASE), ("M", 0.0), ("R", -WHEEL_BASE)):
        for side, letter in ((1.0, "R"), (-1.0, "L")):
            wheel = kit.part(f"Wheel_{row}{letter}", [_copy(wheel_right)], Vector((0, 0, 0)))
            if side < 0:
                mirror_x(wheel)
            wheel.location = Vector((side * WHEEL_TRACK, z, WHEEL_RADIUS))
    template = wheel_right.data
    bpy.data.objects.remove(wheel_right)
    bpy.data.meshes.remove(template)
    neck = build_neck(kit)
    build_head(kit, neck)
    build_wing(kit, 1.0)
    build_wing(kit, -1.0)
    build_antenna(kit)


def _copy(obj):
    duplicate = obj.copy()
    duplicate.data = obj.data.copy()
    duplicate.parent = None
    duplicate.location = obj.location.copy()
    bpy.context.scene.collection.objects.link(duplicate)
    return duplicate


def finish():
    """Scales to 08's size and triangulates so the AO bake and the export see the final faces."""
    for obj in bpy.context.scene.objects:
        obj.location *= SCALE
        if obj.type == "MESH":
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            bm.transform(Matrix.Scale(SCALE, 4))
            bmesh.ops.triangulate(bm, faces=list(bm.faces), quad_method="FIXED", ngon_method="EAR_CLIP")
            bm.to_mesh(obj.data)
            bm.free()
            for polygon in obj.data.polygons:
                polygon.use_smooth = False


def bake_occlusion(meshes):
    """Cycles AO into a per-corner colour; a ground plane is present only for the bake, so feet darken."""
    scene = bpy.context.scene
    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0, 0, 0))
    ground = bpy.context.active_object
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = AO_SAMPLES
    scene.cycles.seed = 0
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.use_denoising = False
    scene.world = scene.world or bpy.data.worlds.new("World")
    scene.world.light_settings.distance = AO_DISTANCE
    scene.render.bake.target = "VERTEX_COLORS"
    for obj in scene.objects:
        obj.select_set(False)
    for obj in meshes:
        attribute = obj.data.color_attributes.new("AO", "FLOAT_COLOR", "CORNER")
        obj.data.color_attributes.active_color = attribute
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.bake(type="AO")
    bpy.data.objects.remove(ground)


def write_uvs(palette, meshes):
    """UV0: the face's swatch cell centre. UV1.x: quantized occlusion (1 - AO) * AO_STRENGTH."""
    for obj in meshes:
        mesh = obj.data
        ao = [item.color[0] for item in mesh.color_attributes["AO"].data]
        # Text meshes bring a UV map of their own; only the two the shader reads may reach the FBX, in this order.
        while mesh.uv_layers:
            mesh.uv_layers.remove(mesh.uv_layers[0])
        # Adding a layer reallocates the mesh's attributes, so the layers are looked up again once both exist.
        mesh.uv_layers.new(name="Palette")
        mesh.uv_layers.new(name="Occlusion")
        palette_uv = mesh.uv_layers["Palette"]
        occlusion_uv = mesh.uv_layers["Occlusion"]
        for polygon in mesh.polygons:
            uv = palette.uv(polygon.material_index)
            for loop in polygon.loop_indices:
                palette_uv.data[loop].uv = uv
                occlusion = (1.0 - ao[loop]) * AO_STRENGTH
                occlusion_uv.data[loop].uv = (round(max(0.0, occlusion) * AO_STEPS) / AO_STEPS, 0.0)


def render_preview(palette, meshes, path):
    """Workbench render of 08 in palette colours darkened by its own occlusion, at a 3/4 chase-like angle."""
    scene = bpy.context.scene
    for obj in meshes:
        mesh = obj.data
        mesh.color_attributes.new("Preview", "FLOAT_COLOR", "CORNER")
        preview = mesh.color_attributes["Preview"]
        occlusion = mesh.uv_layers["Occlusion"]
        for polygon in mesh.polygons:
            colour = palette.linear(polygon.material_index)
            for loop in polygon.loop_indices:
                shade = 1.0 - occlusion.data[loop].uv[0]
                preview.data[loop].color = (*(c * shade for c in colour), 1.0)
        mesh.color_attributes.active_color = preview
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.data.color_attributes.new("Preview", "FLOAT_COLOR", "CORNER")
    for item in floor.data.color_attributes["Preview"].data:
        item.color = (*palette.linear(palette["DustMid"]), 1.0)
    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera_data.lens = 45.0
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (2.7, 3.3, 1.7)
    target = Vector((0.0, 0.0, 0.62))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "VERTEX"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = False
    scene.display.shading.background_type = "WORLD"
    scene.world.color = palette.linear(palette["SkyTop"])
    scene.view_settings.view_transform = "Standard"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 600
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(floor)
    bpy.data.objects.remove(camera)
    for obj in meshes:
        obj.data.color_attributes.remove(obj.data.color_attributes["Preview"])


def export_fbx(meshes, path):
    """Exports with materials and colour attributes dropped, a pinned timestamp and stable object ids."""
    for obj in meshes:
        obj.data.materials.clear()
        obj.data.color_attributes.remove(obj.data.color_attributes["AO"])
    header = export_fbx_bin.fbx_header_elements
    export_fbx_bin.fbx_header_elements = lambda root, scene_data, time=None: header(root, scene_data, FBX_TIME)
    fbx_utils._key_to_uuid = _stable_key_to_uuid
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="Y",
                             axis_up="Z", bake_space_transform=False, use_mesh_modifiers=False,
                             mesh_smooth_type="FACE", use_tspace=False, use_custom_props=False,
                             add_leaf_bones=False, bake_anim=False, path_mode="STRIP", colors_type="NONE")


def _stable_key_to_uuid(uuids, key):
    """fbx_utils._key_to_uuid without Python's per-process string hash, so ids repeat run to run."""
    if isinstance(key, int) and 0 <= key < 2 ** 63:
        uuid = key
    else:
        digest = hashlib.blake2b(repr(key).encode("utf-8"), digest_size=8).digest()
        uuid = int.from_bytes(digest, "little") // 2
    if uuid > int(1e9) and uuid % int(1e9) not in uuids:
        uuid = uuid % int(1e9)
    while uuid in uuids:
        uuid += 1
    return fbx_utils.UUID(uuid)


def main(argv):
    parser = argparse.ArgumentParser(prog="rover08.py")
    parser.add_argument("--palette", required=True, help="palette JSON written by Art's Rover08ModelBuilder")
    parser.add_argument("--fbx", required=True, help="output FBX path")
    parser.add_argument("--preview", help="optional Workbench preview PNG path")
    args = parser.parse_args(argv)
    fbx_path = os.path.abspath(args.fbx)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    palette = Palette(os.path.abspath(args.palette))
    kit = Kit(palette)
    build_rover(kit)
    finish()
    meshes = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: obj.name)
    bake_occlusion(meshes)
    write_uvs(palette, meshes)
    if args.preview:
        render_preview(palette, meshes, os.path.abspath(args.preview))
    export_fbx(meshes, fbx_path)
    triangles = sum(len(obj.data.polygons) for obj in meshes)
    print(f"rover08: {len(meshes)} parts, {triangles} triangles -> {fbx_path}")


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
