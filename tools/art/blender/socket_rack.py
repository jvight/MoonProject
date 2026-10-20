"""Build script for the R1 SocketRack (`Assets/_Project/Art/R1/Models/Structures/SocketRack.fbx`).

The in-world goal board beside 07's charging pad (opening v3, slice 4.3): a low cream-enamel service board with three
bays in a row, one per part 07 can earn. Each bay's back plate carries the part's silhouette as a thin lit line
(`Outline`, `R1_RackOutline`) over a flat filled shape (`Fill`, `R1_RackFill`); Gameplay drives both through
`_EmissionColor`, lights the bay's `Lamp` and closes its two hinged `Clamp_L`/`Clamp_R` jaws when the part is fitted.
Spec: `agy-brands/Logs/socket_rack_art_spec.md`. Vacuum logic: power comes by cable from the pad; no hole patterns
(louvres, ribs and plain panels only).
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bmesh
import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

# Unity metres: X right, Y up, Z forward (the face the bays look out of). Root on the ground at the footprint centre.
RACK_DIMS: dict[str, float] = {
    "width": 1.50,
    "height": 1.30,
    # Posts: cream enamel, set back so the bay plates and the jaws stand clear in front of them
    "post_x": 0.70,
    "post_width": 0.10,
    "post_depth": 0.10,
    "post_center_z": 0.03,
    "post_bottom_y": 0.18,
    # Top rail: overhangs forward like a hood over the bays; carries the orange stripe and the label plate
    "rail_center_y": 1.22,
    "rail_height": 0.16,
    "rail_back_z": -0.02,
    "rail_front_z": 0.155,
    "stripe_center_y": 1.255,
    "stripe_height": 0.035,
    # Back board: one plain grey panel between the posts, the rack's back face seen from the basin
    "board_half_width": 0.655,
    "board_bottom_y": 0.18,
    "board_top_y": 1.14,
    "board_center_z": 0.04,
    "board_depth": 0.04,
    # Base skid and its louvred front below the bays
    "skid_center_y": 0.12,
    "skid_height": 0.12,
    "skid_center_z": 0.03,
    "skid_depth": 0.16,
    "louvre_rows": 3,
    "louvre_bottom_y": 0.27,
    "louvre_step_y": 0.065,
    "louvre_tilt_deg": 32.0,
    # Feet: two splayed steel skids with rubber pads; they set the 0.40 m depth
    "foot_x": 0.62,
    "foot_length": 0.40,
    "foot_splay_deg": 7.0,
    # Bays: centres x = +0.48, 0, -0.48 for bays 0..2 (left to right as the player faces the rack) at y = 0.72, z = 0.12
    "bay_x_step": 0.48,
    "bay_y": 0.72,
    "bay_z": 0.12,
    "plate_size": 0.42,
    "plate_z": -0.02,
    "plate_depth": 0.012,
    "rim_width": 0.018,
    "rim_height": 0.010,
    # Silhouette line: 7 mm wide, standing proud of the plate; the fill lies flat just behind it
    "line_width": 0.007,
    "line_depth": 0.004,
    "fill_depth": 0.0015,
    # Status lamp at the bay's top-right corner
    "lamp_x": 0.17,
    "lamp_y": 0.19,
    "lamp_z": 0.01,
    "lamp_radius": 0.016,
    # Clamp jaws: hinged on Y at the bay's sides, open 35 deg outward at rest
    "clamp_x": 0.19,
    "clamp_z": 0.03,
    "clamp_open_deg": 35.0,
    "clamp_height": 0.30,
}

RACK_MATERIALS: dict[str, dict[str, object]] = {
    # Thin lit line drawing of the part: warm amber, its intensity driven per bay by Gameplay
    "R1_RackOutline": {
        "base_color": r1kit._srgb_to_linear("#3A2E22"),
        "wear_color": r1kit._srgb_to_linear("#3A2E22"),
        "dust_color": r1kit._srgb_to_linear("#8D8B86"),
        "metallic": 0.0,
        "roughness": 0.5,
        "emission": r1kit._srgb_to_linear("#FFC27A"),
        "emission_strength": 1.0,
    },
    # Flat filled silhouette behind the line: a softer warm glow when the part is fitted
    "R1_RackFill": {
        "base_color": r1kit._srgb_to_linear("#2E2620"),
        "wear_color": r1kit._srgb_to_linear("#2E2620"),
        "dust_color": r1kit._srgb_to_linear("#8D8B86"),
        "metallic": 0.0,
        "roughness": 0.6,
        "emission": r1kit._srgb_to_linear("#FFB065"),
        "emission_strength": 1.0,
    },
}

Point2 = tuple[float, float]


def register_rack_materials() -> None:
    """Register R1_RackOutline and R1_RackFill into r1kit at runtime (as kestrel_debris does for its era slots); other
    builders that draw in the rack's outline language (relay.1's feed socket) call it too."""
    for name, spec in RACK_MATERIALS.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = tuple(r1kit.ALLOWED_MATERIALS) + (name,)
        r1kit.MATERIAL_SPECS[name] = spec


# ----------------------------------------------------------------------------------------------------------------
# Silhouettes (front view, bay-local metres, each inside 0.34 x 0.34 m centred on the bay)
# ----------------------------------------------------------------------------------------------------------------


def _circle(cx: float, cy: float, r: float, segments: int) -> list[Point2]:
    return [
        (cx + r * math.cos(2.0 * math.pi * i / segments), cy + r * math.sin(2.0 * math.pi * i / segments))
        for i in range(segments)
    ]


def _rect(cx: float, cy: float, w: float, h: float) -> list[Point2]:
    return [(cx - w * 0.5, cy - h * 0.5), (cx + w * 0.5, cy - h * 0.5), (cx + w * 0.5, cy + h * 0.5),
            (cx - w * 0.5, cy + h * 0.5)]


def _hexagon(cx: float, cy: float, across_flats: float) -> list[Point2]:
    r = across_flats / math.sqrt(3.0)
    return [(cx + r * math.cos(math.radians(60.0 * i)), cy + r * math.sin(math.radians(60.0 * i))) for i in range(6)]


def _silhouette(index: int) -> tuple[list[list[Point2]], list[list[Point2]], list[list[Point2]], list[tuple[list[Point2], float]]]:
    """(closed line loops, open line paths, convex fill polygons, fill ribbons (path, width)) for bay `index`."""
    if index == 0:
        # SSR-3 headlamp, after elder 06's own lamp: a round lens, a short hood above it, a U-shaped yoke below
        lens = _circle(0.0, 0.035, 0.085, 22)
        lens_inner = _circle(0.0, 0.035, 0.055, 18)
        hood = [(-0.105, 0.115), (0.105, 0.115), (0.085, 0.158), (-0.085, 0.158)]
        yoke = [(-0.098, 0.035), (-0.098, -0.075), (-0.05, -0.105), (0.05, -0.105), (0.098, -0.075), (0.098, 0.035)]
        stem = [(0.0, -0.105), (0.0, -0.148)]
        foot = _rect(0.0, -0.155, 0.13, 0.024)
        loops = [lens, lens_inner, hood, foot]
        paths = [yoke, stem]
        fills = [_circle(0.0, 0.035, 0.085, 22), hood, foot]
        ribbons = [(yoke, 0.022), (stem, 0.024)]
        return loops, paths, fills, ribbons
    if index == 1:
        # Cell pack: an upright block with three ribs, a carry handle on top and two contact tabs at the bottom
        block = _rect(0.0, -0.005, 0.22, 0.30)
        ribs = [[(-0.085, y), (0.085, y)] for y in (-0.075, -0.005, 0.065)]
        handle = [(-0.055, 0.145), (-0.055, 0.170), (0.055, 0.170), (0.055, 0.145)]
        tabs = [_rect(-0.055, -0.166, 0.04, 0.022), _rect(0.055, -0.166, 0.04, 0.022)]
        loops = [block] + tabs
        paths = ribs + [handle]
        fills = [block] + tabs
        ribbons = [(handle, 0.016)]
        return loops, paths, fills, ribbons
    # Hex charge adapter (Tycho socket kit): one hexagonal plate, one centred contact, a short cable tail
    hexagon = _hexagon(0.0, 0.03, 0.26)
    contact = _rect(0.0, 0.03, 0.062, 0.038)
    tail = [(0.0, 0.03 - 0.13), (0.0, -0.13), (0.035, -0.155), (0.085, -0.162)]
    loops = [hexagon, contact]
    paths = [tail]
    fills = [hexagon]
    ribbons = [(tail, 0.018)]
    return loops, paths, fills, ribbons


# ----------------------------------------------------------------------------------------------------------------
# Line ribbons and flat fills, built straight into a MeshBuilder's bmesh as closed shells
# ----------------------------------------------------------------------------------------------------------------


def _offsets(points: list[Point2], half_width: float, closed: bool) -> tuple[list[Point2], list[Point2]]:
    """Left and right offset polylines (mitred joins) at `half_width` either side of `points`."""
    n = len(points)
    left: list[Point2] = []
    right: list[Point2] = []
    for i in range(n):
        if closed:
            p_prev, p, p_next = points[i - 1], points[i], points[(i + 1) % n]
        else:
            p_prev = points[i - 1] if i > 0 else None
            p = points[i]
            p_next = points[i + 1] if i < n - 1 else None
        normals = []
        for a, b in ((p_prev, p), (p, p_next)):
            if a is None or b is None:
                continue
            dx, dy = b[0] - a[0], b[1] - a[1]
            length = math.hypot(dx, dy)
            normals.append((-dy / length, dx / length))
        nx = sum(v[0] for v in normals)
        ny = sum(v[1] for v in normals)
        length = math.hypot(nx, ny)
        nx, ny = nx / length, ny / length
        cos_half = max(0.35, nx * normals[0][0] + ny * normals[0][1])
        scale = half_width / cos_half
        left.append((p[0] + nx * scale, p[1] + ny * scale))
        right.append((p[0] - nx * scale, p[1] - ny * scale))
    return left, right


def _add_ribbon(mb: r1kit.MeshBuilder, origin_u: tuple[float, float, float], points: list[Point2], width: float,
                z_back: float, z_front: float, closed: bool, mat: str) -> None:
    """A flat band `width` wide along `points` in the plane facing +Z, from `z_back` to `z_front`: one closed shell."""
    left, right = _offsets(points, width * 0.5, closed)
    ox, oy, oz = origin_u

    def vert(p: Point2, z: float) -> bmesh.types.BMVert:
        return mb.bm.verts.new(mb._to_local_b((ox + p[0], oy + p[1], oz + z)))

    lf = [vert(p, z_front) for p in left]
    rf = [vert(p, z_front) for p in right]
    lb = [vert(p, z_back) for p in left]
    rb = [vert(p, z_back) for p in right]
    n = len(points)
    faces = []
    count = n if closed else n - 1
    for i in range(count):
        j = (i + 1) % n
        faces.append(mb.bm.faces.new([lf[i], rf[i], rf[j], lf[j]]))
        faces.append(mb.bm.faces.new([lb[j], rb[j], rb[i], lb[i]]))
        faces.append(mb.bm.faces.new([lf[j], lb[j], lb[i], lf[i]]))
        faces.append(mb.bm.faces.new([rf[i], rb[i], rb[j], rf[j]]))
    if not closed:
        faces.append(mb.bm.faces.new([lf[0], lb[0], rb[0], rf[0]]))
        faces.append(mb.bm.faces.new([lf[-1], rf[-1], rb[-1], lb[-1]]))
    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)


def _add_flat_prism(mb: r1kit.MeshBuilder, origin_u: tuple[float, float, float], polygon: list[Point2],
                    z_back: float, z_front: float, mat: str) -> None:
    """A convex polygon extruded from `z_back` to `z_front` facing +Z: one closed shell."""
    ox, oy, oz = origin_u
    front = [mb.bm.verts.new(mb._to_local_b((ox + p[0], oy + p[1], oz + z_front))) for p in polygon]
    back = [mb.bm.verts.new(mb._to_local_b((ox + p[0], oy + p[1], oz + z_back))) for p in polygon]
    n = len(polygon)
    faces = [mb.bm.faces.new(front), mb.bm.faces.new(list(reversed(back)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append(mb.bm.faces.new([front[i], back[i], back[j], front[j]]))
    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)


# ----------------------------------------------------------------------------------------------------------------
# Rack parts
# ----------------------------------------------------------------------------------------------------------------


def _build_frame(root: bpy.types.Object) -> None:
    """Posts, forward top rail with its stripe, the plain back board, the base skid and the louvres below the bays."""
    d = RACK_DIMS
    mb = r1kit.MeshBuilder("Frame", pivot_u=(0.0, 0.0, 0.0))
    post_h = d["rail_center_y"] - d["post_bottom_y"]
    for side in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(side * d["post_x"], d["post_bottom_y"] + post_h * 0.5, d["post_center_z"]),
            size_u=(d["post_width"], post_h, d["post_depth"]),
            mat="R1_CreamEnamel",
            bevel=0.012,
            segments=1,
        )
    rail_depth = d["rail_front_z"] - d["rail_back_z"]
    rail_z = (d["rail_front_z"] + d["rail_back_z"]) * 0.5
    mb.add_bevel_box(
        center_u=(0.0, d["rail_center_y"], rail_z),
        size_u=(d["width"], d["rail_height"], rail_depth),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, d["stripe_center_y"], d["rail_front_z"] + 0.003),
        size_u=(d["width"] - 0.06, d["stripe_height"], 0.008),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )
    board_h = d["board_top_y"] - d["board_bottom_y"]
    mb.add_bevel_box(
        center_u=(0.0, d["board_bottom_y"] + board_h * 0.5, d["board_center_z"]),
        size_u=(d["board_half_width"] * 2.0, board_h, d["board_depth"]),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    # Two stiffening ribs across the plain back face: what the basin sees is a working board, not a blank slab
    for y in (0.46, 0.98):
        mb.add_bevel_box(
            center_u=(0.0, y, d["board_center_z"] - d["board_depth"] * 0.5 - 0.009),
            size_u=(d["board_half_width"] * 2.0 - 0.04, 0.035, 0.018),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )
    mb.add_bevel_box(
        center_u=(0.0, d["skid_center_y"], d["skid_center_z"]),
        size_u=(d["width"] - 0.02, d["skid_height"], d["skid_depth"]),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    # Louvres under the bays: wide slanted slats, the rack's cooling (no holes)
    for row in range(int(d["louvre_rows"])):
        y = d["louvre_bottom_y"] + row * d["louvre_step_y"]
        mb.add_bevel_box(
            center_u=(0.0, y, 0.085),
            size_u=(1.16, 0.05, 0.010),
            mat="R1_GreyMetal",
            bevel=0.003,
            segments=1,
            rot_deg_u=(d["louvre_tilt_deg"], 0.0, 0.0),
        )
    # Bay mounting standoffs: one steel block carrying each bay plate off the board
    for i in range(3):
        mb.add_bevel_box(
            center_u=((1 - i) * d["bay_x_step"], d["bay_y"], 0.078),
            size_u=(0.30, 0.30, 0.036),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )
    mb.build_object(parent=root)


def _build_feet(root: bpy.types.Object) -> None:
    """Two splayed steel skid feet under the posts, each with rubber pads at its ends."""
    d = RACK_DIMS
    mb = r1kit.MeshBuilder("Feet", pivot_u=(0.0, 0.0, 0.0))
    for side in (-1.0, 1.0):
        x = side * d["foot_x"]
        yaw = side * d["foot_splay_deg"]
        mb.add_bevel_box(
            center_u=(x, 0.04, 0.0),
            size_u=(0.09, 0.05, d["foot_length"] - 0.03),
            mat="R1_BareSteel",
            bevel=0.008,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
        )
        for end in (-1.0, 1.0):
            dz = end * (d["foot_length"] * 0.5 - 0.045)
            dx = math.sin(math.radians(yaw)) * dz
            mb.add_bevel_box(
                center_u=(x + dx, 0.008, dz),
                size_u=(0.11, 0.016, 0.075),
                mat="R1_Rubber",
                bevel=0.0,
                segments=1,
                rot_deg_u=(0.0, yaw, 0.0),
            )
        # Gusset tying each foot to its post
        mb.add_strut(
            p0_u=(x, 0.06, -0.10),
            p1_u=(side * d["post_x"], 0.18, d["post_center_z"] - 0.03),
            width=0.03,
            thickness=0.012,
            mat="R1_BareSteel",
        )
    mb.build_object(parent=root)


def _build_power_cable(root: bpy.types.Object) -> None:
    """A short cable stub down the back of the right post to a plug at ground level (ties to the pad's power)."""
    mb = r1kit.MeshBuilder("PowerCable", pivot_u=(0.70, 0.10, -0.15))
    mb.add_tube_path(
        [(0.72, 0.96, -0.035), (0.735, 0.62, -0.05), (0.725, 0.26, -0.09), (0.70, 0.10, -0.15),
         (0.66, 0.035, -0.17)],
        radius=0.014,
        mat="R1_Rubber",
        segments=6,
    )
    mb.add_cylinder(center_u=(0.72, 0.985, -0.035), radius=0.024, length=0.05, mat="R1_BareSteel", axis_u="Y",
                    segments=10, bevel=0.004)
    mb.add_cylinder(center_u=(0.615, 0.035, -0.17), radius=0.026, length=0.07, mat="R1_BareSteel", axis_u="X",
                    segments=10, bevel=0.005)
    mb.build_object(parent=root)


def _build_label_plate(root: bpy.types.Object) -> None:
    """A small blank plate on the top rail's face (UI may put words on it later)."""
    mb = r1kit.MeshBuilder("LabelPlate", pivot_u=(0.0, 1.18, 0.16))
    mb.add_bevel_box(center_u=(0.0, 1.18, 0.159), size_u=(0.36, 0.06, 0.008), mat="R1_BareSteel", bevel=0.003,
                     segments=1)
    for x in (-0.165, 0.165):
        mb.add_cylinder(center_u=(x, 1.18, 0.164), radius=0.006, length=0.006, mat="R1_BareSteel", axis_u="Z",
                        segments=6, bevel=0.0)
    mb.build_object(parent=root)


def _build_bay(root: bpy.types.Object, index: int) -> None:
    """Bay `index`: its back plate with a raised rim and lamp bezel, the line silhouette, the fill, lamp and jaws."""
    d = RACK_DIMS
    # Bay 0 sits on the left as the player faces the rack's front (+Z toward them puts local +X on their left)
    bx = (1 - index) * d["bay_x_step"]
    by = d["bay_y"]
    bz = d["bay_z"]
    bay = r1kit.create_empty(f"Bay_{index}", parent=root, pivot_u=(bx, by, bz))
    bpy.context.view_layer.update()

    plate_front = d["plate_z"] + d["plate_depth"] * 0.5
    back = r1kit.MeshBuilder("BackPlate", pivot_u=(bx, by, bz + d["plate_z"]))
    back.add_bevel_box(center_u=(bx, by, bz + d["plate_z"]), size_u=(d["plate_size"], d["plate_size"],
                       d["plate_depth"]), mat="R1_GreyMetal", bevel=0.004, segments=1)
    half = d["plate_size"] * 0.5 - d["rim_width"] * 0.5
    rim_z = bz + plate_front + d["rim_height"] * 0.5
    for sx, sy, w, h in ((0.0, half, d["plate_size"], d["rim_width"]), (0.0, -half, d["plate_size"], d["rim_width"]),
                         (half, 0.0, d["rim_width"], d["plate_size"] - 2 * d["rim_width"]),
                         (-half, 0.0, d["rim_width"], d["plate_size"] - 2 * d["rim_width"])):
        back.add_bevel_box(center_u=(bx + sx, by + sy, rim_z), size_u=(w, h, d["rim_height"]), mat="R1_GreyMetal",
                           bevel=0.0, segments=0)
    back.add_cylinder(center_u=(bx + d["lamp_x"], by + d["lamp_y"], bz + d["lamp_z"] - 0.006),
                      radius=d["lamp_radius"] + 0.006, length=0.006, mat="R1_GreyMetal", axis_u="Z", segments=10,
                      bevel=0.0)
    back.build_object(parent=bay)

    loops, paths, fills, ribbons = _silhouette(index)
    origin = (bx, by, bz)
    line_back = plate_front + 0.0005
    line_front = line_back + d["line_depth"]
    outline = r1kit.MeshBuilder("Outline", pivot_u=origin)
    for loop in loops:
        _add_ribbon(outline, origin, loop, d["line_width"], line_back, line_front, True, "R1_RackOutline")
    for path in paths:
        _add_ribbon(outline, origin, path, d["line_width"], line_back, line_front, False, "R1_RackOutline")
    outline.build_object(parent=bay)

    fill = r1kit.MeshBuilder("Fill", pivot_u=(bx, by, bz - 0.001))
    fill_back = plate_front + 0.0002
    fill_front = fill_back + d["fill_depth"]
    for poly in fills:
        _add_flat_prism(fill, origin, poly, fill_back, fill_front, "R1_RackFill")
    for path, width in ribbons:
        _add_ribbon(fill, origin, path, width, fill_back, fill_front, False, "R1_RackFill")
    fill.build_object(parent=bay)

    r1kit.create_empty("PartSocket", parent=bay, pivot_u=(bx, by, bz + 0.04))

    lamp = r1kit.MeshBuilder("Lamp", pivot_u=(bx + d["lamp_x"], by + d["lamp_y"], bz + d["lamp_z"]))
    lamp.add_cylinder(center_u=(bx + d["lamp_x"], by + d["lamp_y"], bz + d["lamp_z"]), radius=d["lamp_radius"],
                      length=0.014, mat="R1_LampAmber", axis_u="Z", segments=10, bevel=0.003)
    lamp.build_object(parent=bay)

    for side, name in ((-1.0, "Clamp_L"), (1.0, "Clamp_R")):
        hx = bx + side * d["clamp_x"]
        hz = bz + d["clamp_z"]
        jaw = r1kit.MeshBuilder(name, pivot_u=(hx, by, hz))
        # Hinge barrel on the jaw's pivot line, and its two knuckles on the plate rim
        jaw.add_cylinder(center_u=(hx, by, hz), radius=0.011, length=d["clamp_height"], mat="R1_BareSteel",
                         axis_u="Y", segments=6, bevel=0.0)
        # Flat jaw reaching inward over the part's edge, a rubber pad on its inner lip
        jaw.add_bevel_box(center_u=(hx - side * 0.03, by, hz + 0.002), size_u=(0.05, d["clamp_height"] - 0.02, 0.010),
                          mat="R1_BareSteel", bevel=0.002, segments=1)
        jaw.add_bevel_box(center_u=(hx - side * 0.058, by, hz - 0.004), size_u=(0.012, d["clamp_height"] - 0.06,
                          0.014), mat="R1_Rubber", bevel=0.0, segments=0)
        jaw.build_object(parent=bay)


def pose_socket_rack_rest(root: bpy.types.Object) -> None:
    """Every bay's jaws open 35 deg outward (the rest pose Gameplay starts from)."""
    open_rad = math.radians(RACK_DIMS["clamp_open_deg"])
    for obj in r1kit.collect_hierarchy(root):
        if obj.name.startswith("Clamp_L"):
            # Unity yaw about +Y turns the left jaw's inner lip forward and out; Blender's Z is Unity's Y, sign flipped
            obj.rotation_euler = (0.0, 0.0, open_rad)
        elif obj.name.startswith("Clamp_R"):
            obj.rotation_euler = (0.0, 0.0, -open_rad)


def build_socket_rack() -> bpy.types.Object:
    """Build the complete SocketRack hierarchy in the current scene and bake vertex colours."""
    register_rack_materials()
    root = r1kit.create_empty("SocketRack", pivot_u=(0.0, 0.0, 0.0))
    _build_frame(root)
    _build_feet(root)
    _build_power_cable(root)
    _build_label_plate(root)
    for i in range(3):
        _build_bay(root, i)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=RACK_DIMS["width"] * 0.55)
    pose_socket_rack_rest(root)
    bpy.context.view_layer.update()
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build SocketRack FBX.")
    parser.add_argument("--fbx-out", type=Path, default=Path("Assets/_Project/Art/R1/Models/Structures/SocketRack.fbx"))
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_socket_rack()
    print(f"[SocketRack] Triangles: {r1kit.count_triangles(root)}")
    r1kit.export_fbx(root, args.fbx_out)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
