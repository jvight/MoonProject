"""Builds "the sweeper" (Rô-bốt quét bụi, docs/design/0.6-living-moon.md §1, §1a.3, §1a.6) in Blender.

The script is the reproducible source of truth: starting from an empty factory scene, it builds the Lumen-family road
sweeper from bmesh primitives and modifiers, bakes Cycles ambient occlusion with a ground plane into UV1.x, writes
palette cell UVs into UV0, and optionally renders a 3/4 front Workbench preview:

    blender -b --factory-startup --python tools/blender/sweeper.py -- --preview Logs/sweeper/final_preview.png

Design brief & mechanical logic:
- Job: kept the solar-field road clear for rovers and still sweeps slow wide arcs around a dead solar array, pushing
  lunar regolith aside into clean ridges (no air on the Moon, so nothing is sucked up).
- Silhouette & purpose at 30 m: a low, wide, planted four-wheel utility chassis (~1.60 m long, ~1.19 m wide, ~0.80 m
  tall) pushing an articulated front sweeping rig: two wide outrigger rotary side-brushes with splayed bristle tufts
  ahead of a central cylindrical rotary broom and an angled V-chevron deflector plough that channels loosened dust
  outward to either side of the road.
- Lumen family look: rounded-corner Enamel cabinet hull, a wrap-around WarmAccent stripe with sun-bleached FadedAccent
  facets, WornPanel service hatches and fenders, CakedDust drift caps on sky-facing surfaces, a swappable rear battery
  pack with a 3-bar WarmLamp charge gauge, and an asymmetric corner-mounted industrial work floodlight (WarmLamp /
  LampGlass) so the machine reads strictly as utility equipment and never as a character with a face or head.
- Underside induction receiver: a flat round ChargePlate on the belly (Charcoal housing, concentric WarmAccent / Honey
  coil rings, Metal contact pad) that mates with Lumen's round amber-rimmed induction pads when the sweeper parks and
  lifts its front brush yoke.

Axes & conventions:
- Built with +X right, +Y forward, +Z up, in metres, resting on Z = 0.
- Every face's material_index equals its MoonProject.Art.PaletteSwatch enum value (0..30).
"""

import argparse
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

# Cycles AO bake settings matching rover08.py so quantized corner occlusion is deterministic and ready for LofiLit UV1.x.
AO_DISTANCE = 0.45
AO_SAMPLES = 64
AO_STRENGTH = 0.55
AO_STEPS = 32

# Palette table mirrored from Assets/_Project/Scripts/Art/PaletteSwatch.cs and Palette.cs (index == enum value).
SWATCHES = (
    ("SkyTop", "0B0E2A"),
    ("SkyHorizon", "3B2A6B"),
    ("DustLight", "A6A7AB"),
    ("DustMid", "787A80"),
    ("DustShadow", "3E4048"),
    ("RockLight", "6C6D72"),
    ("RockDark", "525359"),
    ("WarmLamp", "FFB547"),
    ("WarmAccent", "FF8A5B"),
    ("Cream", "F4E6C8"),
    ("TechGlow", "5FF3FF"),
    ("EarthOcean", "3FA7D6"),
    ("EarthLand", "7BD389"),
    ("BiolumTeal", "3CFFC2"),
    ("BiolumMagenta", "FF5FD2"),
    ("AlertSoft", "FF5A6E"),
    ("Charcoal", "2B2A3A"),
    ("Metal", "9A9CB0"),
    ("Sage", "86AE9C"),
    ("Honey", "FFD27A"),
    ("PilotLight", "FFC98A"),
    ("Enamel", "F6D7A7"),
    ("LampGlass", "3B302E"),
    ("EyeGlass", "242C3A"),
    ("Wood", "C07A4C"),
    ("SignalGlass", "2E2B33"),
    ("Rust", "B4552B"),
    ("FadedPaint", "B8AFA2"),
    ("CakedDust", "939499"),
    ("FadedAccent", "C68F76"),
    ("WornPanel", "867F77"),
)

ALONG_X = Matrix.Rotation(math.radians(90.0), 3, "Y")
ALONG_Y = Matrix.Rotation(math.radians(-90.0), 3, "X")

WHEEL_TRACK = 0.49
WHEEL_FRONT_Y = 0.12
WHEEL_REAR_Y = -0.48
WHEEL_CENTER_Z = 0.179
YOKE_PIVOT = Vector((0.0, 0.36, 0.22))
ROLLER_PIVOT = Vector((0.0, 0.42, 0.115))
SIDE_BRUSH_X = 0.425
SIDE_BRUSH_Y = 0.675
SIDE_BRUSH_Z = 0.082


def _to_linear(channel):
    """Converts a normalized sRGB channel in [0, 1] to linear RGB using the standard sRGB transfer curve."""
    return channel / 12.92 if channel <= 0.04045 else ((channel + 0.055) / 1.055) ** 2.4


class Palette:
    """Swatch lookup table keyed by PaletteSwatch name and integer index, with UV and linear RGB helpers."""

    columns = 8
    rows = 4

    def __init__(self):
        self.index = {name: idx for idx, (name, _) in enumerate(SWATCHES)}
        self.srgb = {idx: hex_code for idx, (_, hex_code) in enumerate(SWATCHES)}
        self.count = len(SWATCHES)

    def __getitem__(self, name):
        if name not in self.index:
            raise KeyError(f"sweeper: swatch {name} is not in PaletteSwatch")
        return self.index[name]

    def uv(self, index):
        """Centre of the swatch's cell in the 8x4 palette texture (row 0 at bottom)."""
        return ((index % self.columns + 0.5) / self.columns, (index // self.columns + 0.5) / self.rows)

    def linear(self, index):
        """Linear RGB tuple for a swatch index."""
        hex_code = self.srgb[index]
        return tuple(_to_linear(int(hex_code[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


class Kit:
    """Creates mesh pieces sharing the full 31-swatch material table so face.material_index == PaletteSwatch."""

    def __init__(self, palette):
        self.palette = palette
        self.materials = []
        for index, (name, _) in enumerate(SWATCHES):
            material = bpy.data.materials.new(f"Swatch{index:02d}_{name}")
            linear_rgb = palette.linear(index)
            material.diffuse_color = (*linear_rgb, 1.0)
            if material.use_nodes and material.node_tree:
                bsdf = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
                if bsdf is not None:
                    bsdf.inputs[0].default_value = (*linear_rgb, 1.0)
                    bsdf.inputs["Roughness"].default_value = 0.85
            self.materials.append(material)

    def piece(self, name, build_fn, swatch, matrix=Matrix.Identity(4)):
        """Creates a scene object from bmesh builder `build_fn(bm)` and paints all faces with `swatch`."""
        bm = bmesh.new()
        build_fn(bm)
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
        """Evaluates modifiers on `pieces`, merges them into one flat-shaded triangulated part at `pivot`."""
        depsgraph = bpy.context.evaluated_depsgraph_get()
        bm = bmesh.new()
        for piece in pieces:
            evaluated_obj = piece.evaluated_get(depsgraph)
            evaluated_mesh = bpy.data.meshes.new_from_object(evaluated_obj)
            evaluated_mesh.transform(evaluated_obj.matrix_world)
            bm.from_mesh(evaluated_mesh)
            bpy.data.meshes.remove(evaluated_mesh)
        for piece in pieces:
            data = piece.data
            bpy.data.objects.remove(piece)
            bpy.data.meshes.remove(data)
        pivot_vec = Vector(pivot)
        bm.transform(Matrix.Translation(-pivot_vec))
        bmesh.ops.triangulate(bm, faces=list(bm.faces), quad_method="FIXED", ngon_method="EAR_CLIP")
        obj = self._object(name, bm)
        for polygon in obj.data.polygons:
            polygon.use_smooth = False
        obj.location = pivot_vec
        if parent is not None:
            obj.parent = parent
            obj.location = pivot_vec - _world_pivot(parent)
        return obj


def _world_pivot(obj):
    total = Vector(obj.location)
    while obj.parent is not None:
        obj = obj.parent
        total += obj.location
    return total


def at(location, rotation=Matrix.Identity(3)):
    return Matrix.Translation(location) @ rotation.to_4x4()


def rot_xyz(rx=0.0, ry=0.0, rz=0.0):
    return (
        Matrix.Rotation(math.radians(rz), 3, "Z")
        @ Matrix.Rotation(math.radians(ry), 3, "Y")
        @ Matrix.Rotation(math.radians(rx), 3, "X")
    )


def box(size):
    def build(bm):
        bmesh.ops.create_cube(bm, size=1.0)
        bm.transform(Matrix.Diagonal((*size, 1.0)))
    return build


def tapered_hull(width, length, height, front_drop=0.08, front_narrow=0.88, top_inset=0.94):
    """Builds a hull box whose forward (+Y) nose slopes down and narrows slightly so direction reads in silhouette."""
    def build(bm):
        bmesh.ops.create_cube(bm, size=1.0)
        for vert in bm.verts:
            x = vert.co.x * width
            y = vert.co.y * length
            z = vert.co.z * height
            if vert.co.z > 0.0:
                x *= top_inset
            if vert.co.y > 0.0:
                x *= front_narrow
                if vert.co.z > 0.0:
                    z -= front_drop
            vert.co = Vector((x, y, z))
    return build


def cylinder(radius, depth, sides, top_radius=None):
    """Cone/cylinder centred on the origin along +Z."""
    def build(bm):
        bmesh.ops.create_cone(
            bm,
            cap_ends=True,
            segments=sides,
            radius1=radius,
            radius2=radius if top_radius is None else top_radius,
            depth=depth,
        )
    return build


def strut(kit, name, start, end, section, swatch):
    """A rectangular beam spanning from `start` to `end` with `(width, height)` cross-section."""
    start_vec = Vector(start)
    end_vec = Vector(end)
    axis = end_vec - start_vec
    rotation = axis.to_track_quat("Y", "Z").to_matrix()
    return kit.piece(
        name,
        box((section[0], axis.length + section[1], section[1])),
        swatch,
        at((start_vec + end_vec) * 0.5, rotation),
    )


def bevel(obj, width, segments=1, weighted=False, angle=38.0):
    modifier = obj.modifiers.new("Bevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "WEIGHT" if weighted else "ANGLE"
    modifier.angle_limit = math.radians(angle)
    modifier.harden_normals = False
    return obj


def round_corners(obj, radius, segments=2):
    """Weights vertical edges of a hull box so a weighted bevel rounds its cabinet corners like 07/08."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    weight = bm.edges.layers.float.new("bevel_weight_edge")
    for edge in bm.edges:
        direction = edge.verts[1].co - edge.verts[0].co
        if direction.length > 1e-5 and abs(direction.normalized().z) > 0.90:
            edge[weight] = 1.0
    bm.to_mesh(obj.data)
    bm.free()
    return bevel(obj, radius, segments, weighted=True)


def _build_sliced_main_hull(kit):
    """Builds the rounded Enamel cabinet hull with a flush bisected WarmAccent/FadedAccent stripe and roof dust cap."""
    raw_hull = kit.piece(
        "MainHullRaw",
        tapered_hull(0.88, 1.02, 0.35, front_drop=0.08, front_narrow=0.88, top_inset=0.94),
        "Enamel",
        at((0.0, -0.18, 0.43)),
    )
    bevel(round_corners(raw_hull, 0.14, 2), 0.025, 1)

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = raw_hull.evaluated_get(depsgraph)
    evaluated_mesh = bpy.data.meshes.new_from_object(evaluated_obj)
    evaluated_mesh.transform(evaluated_obj.matrix_world)
    raw_data = raw_hull.data
    bpy.data.objects.remove(raw_hull)
    bpy.data.meshes.remove(raw_data)

    palette = kit.palette

    def slice_and_paint(bm):
        bm.from_mesh(evaluated_mesh)
        # Bisect horizontal planes to create a flush wrap-around stripe with zero overhang or Z-fighting.
        for z_cut in (0.395, 0.460):
            geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
            bmesh.ops.bisect_plane(
                bm, geom=geom, dist=1e-4, plane_co=Vector((0.0, 0.0, z_cut)), plane_no=Vector((0.0, 0.0, 1.0))
            )
        # Inset the large sky-facing forward roof face and split it into CakedDust and sun-bleached FadedPaint facets.
        top_faces = [face for face in bm.faces if face.normal.z > 0.90 and face.calc_area() > 0.15]
        if top_faces:
            bmesh.ops.inset_region(bm, faces=top_faces, thickness=0.055, depth=0.004, use_boundary=True)
            geom_top = list(bm.verts) + list(bm.edges) + list(bm.faces)
            bmesh.ops.bisect_plane(
                bm,
                geom=geom_top,
                dist=1e-4,
                plane_co=Vector((0.06, 0.0, 0.58)),
                plane_no=Vector((1.0, 0.35, 0.0)).normalized(),
            )

        enamel_idx = palette["Enamel"]
        warm_idx = palette["WarmAccent"]
        faded_idx = palette["FadedAccent"]
        dust_idx = palette["CakedDust"]
        faded_paint_idx = palette["FadedPaint"]

        for face in bm.faces:
            center = face.calc_center_median()
            if 0.395 <= center.z <= 0.460 and abs(face.normal.z) < 0.45:
                if (center.x < -0.08 and center.y > -0.22) or (center.x > 0.12 and center.y < -0.42):
                    face.material_index = faded_idx
                else:
                    face.material_index = warm_idx
            elif face.normal.z > 0.92 and center.z > 0.55 and abs(center.x) < 0.34 and -0.58 < center.y < 0.24:
                face.material_index = dust_idx if center.x < 0.06 else faded_paint_idx
            else:
                face.material_index = enamel_idx

    hull_piece = kit.piece("MainHull", slice_and_paint, None)
    bpy.data.meshes.remove(evaluated_mesh)
    return hull_piece


def build_body(kit):
    """Builds the main utility hull, bumpers, fenders, rear swappable battery bay, and off-center work floodlight."""
    pieces = [
        _build_sliced_main_hull(kit),
        kit.piece("ChassisPan", box((0.76, 1.00, 0.11)), "Charcoal", at((0.0, -0.18, 0.21))),
        kit.piece("RearBumper", box((0.86, 0.08, 0.09)), "Charcoal", at((0.0, -0.70, 0.22))),
        kit.piece("TowLoop", box((0.20, 0.05, 0.028)), "Metal", at((0.0, -0.725, 0.215))),
        kit.piece("FrontBumper", box((0.82, 0.08, 0.09)), "Charcoal", at((0.0, 0.31, 0.22))),
        # Weathered WornPanel side service door on the right flank with a subtle Rust streak below its bottom seam.
        kit.piece(
            "MaintDoorR", box((0.012, 0.24, 0.14)), "WornPanel", at((0.422, -0.18, 0.35), rot_xyz(ry=-4.5, rz=1.5))
        ),
        kit.piece("RustSeamR", box((0.014, 0.07, 0.055)), "Rust", at((0.425, -0.22, 0.27), rot_xyz(ry=-4.5, rz=1.5))),
    ]

    for side in (-1.0, 1.0):
        vent_rot = rot_xyz(ry=-side * 4.5, rz=side * 1.5)
        pieces.append(
            kit.piece(f"VentFrame{side:+.0f}", box((0.014, 0.20, 0.11)), "Metal", at((side * 0.428, -0.48, 0.35), vent_rot))
        )
        for i in range(3):
            pieces.append(
                kit.piece(
                    f"VentSlat{side:+.0f}_{i}",
                    box((0.018, 0.16, 0.013)),
                    "Charcoal",
                    at((side * 0.431, -0.48, 0.322 + i * 0.028), vent_rot),
                )
            )
        for row_tag, wheel_y in (("F", WHEEL_FRONT_Y), ("R", WHEEL_REAR_Y)):
            pieces.append(
                kit.piece(
                    f"Fender_{row_tag}{side:+.0f}",
                    box((0.17, 0.38, 0.034)),
                    "WornPanel",
                    at((side * 0.465, wheel_y, 0.375)),
                )
            )
            if row_tag == "R":
                pieces.append(
                    kit.piece(
                        f"FenderDust_{row_tag}{side:+.0f}",
                        box((0.14, 0.30, 0.010)),
                        "CakedDust",
                        at((side * 0.465, wheel_y, 0.394)),
                    )
                )

    # Rear swappable battery bay & 3-bar diegetic WarmLamp charge gauge.
    batt_housing = kit.piece("BatteryHousing", box((0.68, 0.44, 0.075)), "WornPanel", at((0.0, -0.40, 0.625)))
    pieces.append(round_corners(batt_housing, 0.08, 1))
    pieces.extend([
        kit.piece("DeckDustDrift", box((0.24, 0.34, 0.012)), "CakedDust", at((0.17, -0.40, 0.665))),
        kit.piece("BatteryPack", box((0.32, 0.26, 0.048)), "Charcoal", at((-0.07, -0.39, 0.678))),
        kit.piece("BatteryLatchF", box((0.09, 0.028, 0.028)), "Metal", at((-0.07, -0.25, 0.672))),
        kit.piece("BatteryLatchR", box((0.09, 0.028, 0.028)), "Metal", at((-0.07, -0.53, 0.672))),
        kit.piece("BatteryHandle", box((0.18, 0.030, 0.036)), "Honey", at((-0.07, -0.39, 0.712))),
        kit.piece("SpecLabel", box((0.12, 0.07, 0.008)), "Cream", at((0.17, -0.26, 0.672))),
        kit.piece("GaugeBezel", box((0.19, 0.050, 0.016)), "LampGlass", at((-0.07, -0.57, 0.666))),
    ])
    for i in range(3):
        pieces.append(
            kit.piece(f"GaugeBar{i}", box((0.042, 0.028, 0.020)), "WarmLamp", at((-0.125 + i * 0.055, -0.57, 0.668)))
        )

    # Rear maintenance panel and protection grab rail.
    pieces.extend([
        kit.piece("RearDoor", box((0.48, 0.022, 0.18)), "WornPanel", at((0.0, -0.688, 0.39))),
        strut(kit, "GrabRailL", (-0.25, -0.68, 0.52), (-0.25, -0.74, 0.52), (0.024, 0.024), "Metal"),
        strut(kit, "GrabRailR", (0.25, -0.68, 0.52), (0.25, -0.74, 0.52), (0.024, 0.024), "Metal"),
        strut(kit, "GrabRailBar", (-0.25, -0.74, 0.52), (0.25, -0.74, 0.52), (0.024, 0.024), "Metal"),
    ])

    # Off-center utility floodlight stanchion on the front-left shoulder (avoids reading as a head or face).
    lamp_rot = rot_xyz(rx=-15.0, rz=-8.0)
    pieces.extend([
        kit.piece("LampBase", cylinder(0.046, 0.035, 8), "Charcoal", at((-0.24, 0.02, 0.565), rot_xyz(rx=-4.5))),
        strut(kit, "LampStanchion", (-0.24, 0.02, 0.562), (-0.24, 0.04, 0.74), (0.032, 0.032), "Metal"),
        strut(kit, "LampSideBrace", (-0.12, 0.02, 0.558), (-0.24, 0.03, 0.72), (0.020, 0.020), "Metal"),
        kit.piece("LampCowl", box((0.165, 0.10, 0.088)), "Charcoal", at((-0.24, 0.06, 0.745), lamp_rot)),
        kit.piece("LampHood", box((0.175, 0.045, 0.015)), "WornPanel", at((-0.24, 0.11, 0.780), lamp_rot)),
        kit.piece("LampGlassFrame", box((0.14, 0.020, 0.062)), "LampGlass", at((-0.24, 0.105, 0.742), lamp_rot)),
        kit.piece("LampGlow", box((0.115, 0.026, 0.044)), "WarmLamp", at((-0.24, 0.110, 0.742), lamp_rot)),
        kit.piece("BeaconRing", cylinder(0.030, 0.015, 8), "Charcoal", at((-0.24, 0.04, 0.785))),
        kit.piece("BeaconDome", cylinder(0.024, 0.022, 8, top_radius=0.016), "WarmLamp", at((-0.24, 0.04, 0.798))),
    ])

    for side in (-1.0, 1.0):
        pieces.append(
            kit.piece(f"HitchEar{side:+.0f}", box((0.055, 0.09, 0.075)), "Charcoal", at((side * 0.27, 0.35, 0.22)))
        )
        pieces.append(
            kit.piece(
                f"HitchPin{side:+.0f}", cylinder(0.026, 0.085, 8), "Metal", at((side * 0.27, 0.36, 0.22), ALONG_X)
            )
        )

    return kit.part("Body", pieces, Vector((0.0, 0.0, 0.0)))


def build_charge_plate(kit, body):
    """Builds the flat round underside induction receiver plate that couples with Lumen's round charging pads."""
    pieces = [
        kit.piece("ChargeHousing", cylinder(0.34, 0.032, 10, top_radius=0.36), "Charcoal", at((0.0, -0.18, 0.135))),
        kit.piece("CoilOuterRing", cylinder(0.28, 0.012, 10), "WarmAccent", at((0.0, -0.18, 0.116))),
        kit.piece("CoilInnerGap", cylinder(0.23, 0.014, 10), "Charcoal", at((0.0, -0.18, 0.115))),
        kit.piece("CoilSecondary", cylinder(0.19, 0.016, 10), "Honey", at((0.0, -0.18, 0.114))),
        kit.piece("ContactPad", cylinder(0.12, 0.018, 10), "Metal", at((0.0, -0.18, 0.113))),
    ]
    for i in range(4):
        angle = i * math.pi * 0.5 + math.pi * 0.25
        rx = math.cos(angle) * 0.23
        ry = -0.18 + math.sin(angle) * 0.23
        pieces.append(
            kit.piece(
                f"PlateRib{i}",
                box((0.028, 0.12, 0.02)),
                "Charcoal",
                at((rx, ry, 0.115), Matrix.Rotation(angle + math.pi * 0.5, 3, "Z")),
            )
        )
    return kit.part("ChargePlate", pieces, Vector((0.0, -0.18, 0.12)), body)


def build_suspension_and_wheels(kit, body):
    """Builds the left/right rocker suspension struts and four faceted Lumen wheels resting on Z = 0."""
    for side, side_tag in ((1.0, "R"), (-1.0, "L")):
        susp_pieces = [
            strut(
                kit,
                f"RockBeam_{side_tag}",
                (side * 0.41, WHEEL_REAR_Y, WHEEL_CENTER_Z),
                (side * 0.41, WHEEL_FRONT_Y, WHEEL_CENTER_Z),
                (0.04, 0.055),
                "Charcoal",
            ),
            strut(
                kit,
                f"StrutF_{side_tag}",
                (side * 0.39, -0.12, 0.280),
                (side * 0.41, WHEEL_FRONT_Y, WHEEL_CENTER_Z),
                (0.035, 0.045),
                "Metal",
            ),
            strut(
                kit,
                f"StrutR_{side_tag}",
                (side * 0.39, -0.24, 0.280),
                (side * 0.41, WHEEL_REAR_Y, WHEEL_CENTER_Z),
                (0.035, 0.045),
                "Metal",
            ),
            kit.piece(
                f"PivotBoss_{side_tag}",
                cylinder(0.045, 0.06, 8),
                "Metal",
                at((side * 0.40, -0.18, 0.270), ALONG_X),
            ),
        ]
        kit.part(f"Suspension_{side_tag}", susp_pieces, Vector((side * 0.40, -0.18, 0.24)), body)

        for row_tag, wheel_y in (("F", WHEEL_FRONT_Y), ("R", WHEEL_REAR_Y)):
            wx = side * WHEEL_TRACK
            wz = WHEEL_CENTER_Z
            tyre_radius = 0.174
            half_width = 0.072
            # Rear-left wheel carries a mismatched WornPanel replacement hub in the Lumen tradition.
            hub_swatch = "WornPanel" if (row_tag == "R" and side_tag == "L") else "Enamel"
            wheel_pieces = [
                bevel(
                    kit.piece("Tyre", cylinder(tyre_radius, 2.0 * half_width, 10), "Charcoal", at((wx, wheel_y, wz), ALONG_X)),
                    0.020,
                ),
                kit.piece("AxleStub", cylinder(0.032, 0.12, 6), "Metal", at((wx - side * 0.06, wheel_y, wz), ALONG_X)),
                kit.piece(
                    "OuterHub",
                    cylinder(0.11, 0.024, 10),
                    hub_swatch,
                    at((wx + side * (half_width + 0.008), wheel_y, wz), ALONG_X),
                ),
                kit.piece(
                    "HubCap",
                    cylinder(0.048, 0.028, 6, top_radius=0.034),
                    "Metal",
                    at((wx + side * (half_width + 0.020), wheel_y, wz), ALONG_X),
                ),
                kit.piece(
                    "HubBolt",
                    cylinder(0.016, 0.016, 6),
                    "WarmAccent",
                    at((wx + side * (half_width + 0.018), wheel_y, wz + 0.072), ALONG_X),
                ),
                kit.piece(
                    "InnerHub",
                    cylinder(0.09, 0.018, 8),
                    "Metal",
                    at((wx - side * (half_width + 0.006), wheel_y, wz), ALONG_X),
                ),
            ]
            for i in range(8):
                angle = (i + 0.5) * math.tau / 8.0
                rot = Matrix.Rotation(angle, 3, "X")
                lug_pos = Vector((wx, wheel_y, wz)) + (rot @ Vector((0.0, 0.0, 0.173)))
                wheel_pieces.append(
                    kit.piece(f"Lug{i}", box((2.0 * half_width * 0.86, 0.040, 0.022)), "Charcoal", at(lug_pos, rot))
                )
            kit.part(f"Wheel_{row_tag}{side_tag}", wheel_pieces, Vector((wx, wheel_y, wz)), body)


def build_brush_yoke(kit, body):
    """Builds the front lift boom, V-chevron dust deflector plough, roller hood, and outrigger brush arms."""
    pieces = []
    for side in (-1.0, 1.0):
        pieces.append(
            strut(kit, f"LiftArm{side:+.0f}", (side * 0.27, 0.36, 0.22), (side * 0.25, 0.45, 0.22), (0.045, 0.048), "Metal")
        )
        pieces.append(
            strut(
                kit, f"LiftRam{side:+.0f}", (side * 0.21, 0.31, 0.29), (side * 0.21, 0.40, 0.24), (0.026, 0.026), "Charcoal"
            )
        )
        v_rot = rot_xyz(rz=-side * 14.0)
        # Angled V-plough blade behind the roller pushes brushed regolith outward into side ridges (no air/suction).
        pieces.extend([
            kit.piece(f"PloughBlade{side:+.0f}", box((0.36, 0.034, 0.15)), "WornPanel", at((side * 0.17, 0.33, 0.13), v_rot)),
            kit.piece(f"PloughSkirt{side:+.0f}", box((0.36, 0.022, 0.045)), "Charcoal", at((side * 0.17, 0.335, 0.045), v_rot)),
            kit.piece(f"ChevronHood{side:+.0f}", box((0.40, 0.20, 0.052)), "WornPanel", at((side * 0.18, 0.43, 0.248), v_rot)),
            kit.piece(f"HoodDust{side:+.0f}", box((0.34, 0.13, 0.014)), "CakedDust", at((side * 0.18, 0.42, 0.278), v_rot)),
            kit.piece(
                f"HoodBumper{side:+.0f}", box((0.39, 0.028, 0.032)), "WarmAccent", at((side * 0.18, 0.525, 0.242), v_rot)
            ),
            strut(
                kit,
                f"OutriggerArm{side:+.0f}",
                (side * 0.28, 0.47, 0.23),
                (side * SIDE_BRUSH_X, SIDE_BRUSH_Y, 0.18),
                (0.044, 0.038),
                "Metal",
            ),
            kit.piece(f"OutriggerHinge{side:+.0f}", cylinder(0.040, 0.052, 8), "Charcoal", at((side * 0.28, 0.47, 0.23))),
            kit.piece(
                f"BrushMotor{side:+.0f}",
                cylinder(0.060, 0.058, 8, top_radius=0.046),
                "Charcoal",
                at((side * SIDE_BRUSH_X, SIDE_BRUSH_Y, 0.185)),
            ),
        ])

    pieces.append(kit.piece("CenterYokeBridge", box((0.16, 0.22, 0.068)), "Metal", at((0.0, 0.45, 0.252))))
    return kit.part("BrushYoke", pieces, YOKE_PIVOT, body)


def build_main_roller(kit, yoke):
    """Builds the central cylindrical rotary broom under the chevron hood, pivoted on its spin axis."""
    pieces = [
        kit.piece("RollerShaft", cylinder(0.040, 0.64, 8), "Charcoal", at(ROLLER_PIVOT, ALONG_X)),
        kit.piece("RollerCore", cylinder(0.070, 0.58, 8), "Honey", at(ROLLER_PIVOT, ALONG_X)),
    ]
    for i in range(8):
        angle = i * math.tau / 8.0
        rot = Matrix.Rotation(angle, 3, "X")
        fin_pos = ROLLER_PIVOT + (rot @ Vector((0.0, 0.0, 0.086)))
        swatch = "Honey" if i % 2 == 0 else "FadedAccent"
        pieces.append(kit.piece(f"RollerBlade{i}", box((0.56, 0.020, 0.034)), swatch, at(fin_pos, rot)))
    for k in (-1, 0, 1):
        pieces.append(
            kit.piece(
                f"RollerBand{k}",
                cylinder(0.095, 0.020, 8),
                "Charcoal",
                at(ROLLER_PIVOT + Vector((k * 0.16, 0.0, 0.0)), ALONG_X),
            )
        )
    return kit.part("MainRoller", pieces, ROLLER_PIVOT, yoke)


def build_side_brushes(kit, yoke):
    """Builds the twin outrigger rotary side-brushes with splayed bristle tufts that define the 1.2 m front width."""
    for side, side_tag in ((1.0, "R"), (-1.0, "L")):
        brush_pivot = Vector((side * SIDE_BRUSH_X, SIDE_BRUSH_Y, SIDE_BRUSH_Z))
        pieces = [
            kit.piece(
                "ShieldPlate",
                cylinder(0.145, 0.026, 10, top_radius=0.102),
                "Enamel",
                at(brush_pivot + Vector((0.0, 0.0, 0.052))),
            ),
            kit.piece("ShieldRim", cylinder(0.152, 0.016, 10), "WarmAccent", at(brush_pivot + Vector((0.0, 0.0, 0.042)))),
            kit.piece("ShieldDust", cylinder(0.095, 0.010, 8), "CakedDust", at(brush_pivot + Vector((0.0, 0.0, 0.066)))),
            kit.piece(
                "BristleHub",
                cylinder(0.120, 0.040, 10, top_radius=0.100),
                "Charcoal",
                at(brush_pivot + Vector((0.0, 0.0, 0.010))),
            ),
            kit.piece(
                "BristleCone",
                cylinder(0.160, 0.050, 10, top_radius=0.114),
                "Honey",
                at(brush_pivot + Vector((0.0, 0.0, -0.015))),
            ),
        ]
        for i in range(10):
            angle = i * math.tau / 10.0
            tuft_rot = Matrix.Rotation(angle, 3, "Z") @ Matrix.Rotation(math.radians(24.0), 3, "X")
            offset = Matrix.Rotation(angle, 3, "Z") @ Vector((0.0, 0.134, -0.016))
            tuft_swatch = "Honey" if i % 2 == 0 else "FadedAccent"
            pieces.append(kit.piece(f"Tuft{i}", box((0.038, 0.068, 0.044)), tuft_swatch, at(brush_pivot + offset, tuft_rot)))
        kit.part(f"SideBrush_{side_tag}", pieces, brush_pivot, yoke)


def build_sweeper(kit):
    """Assembles the complete articulated Sweeper hierarchy."""
    body = build_body(kit)
    build_charge_plate(kit, body)
    build_suspension_and_wheels(kit, body)
    yoke = build_brush_yoke(kit, body)
    build_main_roller(kit, yoke)
    build_side_brushes(kit, yoke)


def bake_occlusion(meshes):
    """Bakes Cycles ambient occlusion against a temporary ground plane into per-corner vertex colour 'AO'."""
    scene = bpy.context.scene
    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, 0.0))
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
    """Writes UV0 ('Palette') at the face's swatch cell centre and UV1.x ('Occlusion') with quantized AO."""
    warm_lamp_idx = palette["WarmLamp"]
    for obj in meshes:
        mesh = obj.data
        ao = [item.color[0] for item in mesh.color_attributes["AO"].data]
        while mesh.uv_layers:
            mesh.uv_layers.remove(mesh.uv_layers[0])
        mesh.uv_layers.new(name="Palette")
        mesh.uv_layers.new(name="Occlusion")
        palette_uv = mesh.uv_layers["Palette"]
        occlusion_uv = mesh.uv_layers["Occlusion"]
        for polygon in mesh.polygons:
            uv = palette.uv(polygon.material_index)
            is_emissive = polygon.material_index == warm_lamp_idx
            for loop in polygon.loop_indices:
                palette_uv.data[loop].uv = uv
                if is_emissive:
                    occlusion_uv.data[loop].uv = (0.0, 0.0)
                else:
                    occlusion = (1.0 - ao[loop]) * AO_STRENGTH
                    occlusion_uv.data[loop].uv = (round(max(0.0, occlusion) * AO_STEPS) / AO_STEPS, 0.0)


def render_preview(palette, meshes, path):
    """Renders a 3/4 front Workbench preview of the sweeper in palette colours darkened by baked occlusion."""
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

    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, 0.0))
    floor = bpy.context.active_object
    floor.data.color_attributes.new("Preview", "FLOAT_COLOR", "CORNER")
    for item in floor.data.color_attributes["Preview"].data:
        item.color = (*palette.linear(palette["DustMid"]), 1.0)

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera_data.lens = 45.0
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (2.10, 2.55, 1.32)
    target = Vector((0.0, 0.08, 0.34))
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


def main(argv):
    parser = argparse.ArgumentParser(prog="sweeper.py")
    parser.add_argument("--preview", help="optional Workbench preview PNG output path")
    args = parser.parse_args(argv)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    palette = Palette()
    kit = Kit(palette)
    build_sweeper(kit)
    meshes = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: obj.name)
    bake_occlusion(meshes)
    write_uvs(palette, meshes)
    if args.preview:
        preview_path = os.path.abspath(args.preview)
        os.makedirs(os.path.dirname(preview_path), exist_ok=True)
        render_preview(palette, meshes, preview_path)
    triangles = sum(len(obj.data.polygons) for obj in meshes)
    print(f"sweeper: {len(meshes)} parts, {triangles} triangles")


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
