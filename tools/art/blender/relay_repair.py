"""Build script for relay.1's repair props (opening v3, slice 5.1), SSR-3 / VIS era, 1980s field kit:

- `Assets/_Project/Art/R1/Models/Structures/ServiceCabinet.fbx`: the worn field cabinet at a mast's foot, knee-to-hip
  high on 07, its door hanging open on a sagging hinge, a shelf inside and a warm strip lamp (`InteriorLamp`,
  `R1_LampAmber`, with `Light_Anchor` for Gameplay's light) so the part on the shelf reads at night. The door's cut seal bead is its own node (`DoorSeal`).
- `Assets/_Project/Art/R1/Models/Props/FeedUnitSpare.fbx`: the labelled spare (`RELAY SPARE · FEED UNIT`), a carryable
  power feed module: a box body with a top handle, one chunky keyed connector at the front, a label plate and a short
  cable pigtail out of the back. Its cross-section is `FEED_UNIT_DIMS`, which the mast's `FeedSocket` is cut to.
- `Assets/_Project/Art/R1/Models/Structures/RelayContactPad.fbx`: the low contact plate at the mast's foot that charges
  07 once the relay is lit: the charging pad's language (wheel pads on 07's track, copper contact shoes, a conduit and
  a status lamp `PadLamp`), older, smaller and weathered.

Every node name is unique (the R1 import rejects repeated names). Vacuum logic: no rain hoods; cooling by louvres and
plain panels, no hole patterns.

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/relay_repair.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

REPO_ROOT = THIS_DIR.parents[2]
MODELS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models"

# Unity metres: X right, Y up, Z forward. The feed unit's connector faces +Z; its root sits under the body's centre.
FEED_UNIT_DIMS: dict[str, float] = {
    "body_width": 0.18,
    "body_height": 0.14,
    "body_length": 0.28,
    "cap_depth": 0.022,
    "connector_radius": 0.050,
    "connector_length": 0.050,
    "handle_rise": 0.055,
    "handle_radius": 0.011,
    "cable_radius": 0.011,
}

# The cabinet's front (+Z) faces the player; the door hinges on the viewer's right (-X) and hangs open.
CABINET_DIMS: dict[str, float] = {
    "width": 0.62,
    "depth": 0.42,
    "height": 0.84,
    "skid_height": 0.05,
    "wall": 0.024,
    "jamb_width": 0.030,
    "shelf_y": 0.40,
    "lamp_y": 0.785,
    "lamp_z": 0.11,
    "lamp_length": 0.40,
    "door_open_deg": 112.0,
    "door_sag_deg": 5.0,
}

# The pad's long axis (Z) runs along 07; +Z is the entry end, the conduit leaves from -Z toward the mast.
PAD_DIMS: dict[str, float] = {
    "width": 1.90,
    "length": 2.30,
    "deck_height": 0.06,
    "wheel_x": 0.61,
    "wheel_z": 0.88,
    "wheel_pad_width": 0.30,
    "wheel_pad_length": 0.46,
    "shoe_x": 0.12,
}


# ----------------------------------------------------------------------------------------------------------------
# Feed unit spare
# ----------------------------------------------------------------------------------------------------------------


def _build_feed_unit_body(root: bpy.types.Object) -> None:
    """Cream body with grey end caps, an orange band, side grip ribs and rubber bumpers on the back corners."""
    d = FEED_UNIT_DIMS
    w, h, length = d["body_width"], d["body_height"], d["body_length"]
    cy = h * 0.5
    mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(0.0, cy, 0.0), size_u=(w, h, length), mat="R1_CreamEnamel", bevel=0.012, segments=1)
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(0.0, cy, sz * (length * 0.5 - d["cap_depth"] * 0.5 + 0.004)),
                         size_u=(w + 0.012, h + 0.012, d["cap_depth"]), mat="R1_GreyMetal", bevel=0.006, segments=1)
    mb.add_bevel_box(center_u=(0.0, cy, -0.035), size_u=(w + 0.006, h + 0.006, 0.030), mat="R1_OrangeStripe",
                     bevel=0.004, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 + 0.006), cy + 0.030, 0.050), size_u=(0.012, 0.018, 0.12),
                         mat="R1_GreyMetal", bevel=0.003, segments=1)
        mb.add_bevel_box(center_u=(sx * (w * 0.5 + 0.004), cy - 0.040, -length * 0.5 + 0.010),
                         size_u=(0.020, 0.040, 0.030), mat="R1_Rubber", bevel=0.004, segments=1)
    mb.build_object(parent=root)


def _build_feed_unit_handle(root: bpy.types.Object) -> None:
    """A steel bail handle across the top, on two grey mounting blocks: 07's grip."""
    d = FEED_UNIT_DIMS
    top = d["body_height"]
    mb = r1kit.MeshBuilder("Handle", pivot_u=(0.0, top, 0.0))
    for z in (-0.075, 0.075):
        mb.add_bevel_box(center_u=(0.0, top + 0.010, z), size_u=(0.050, 0.020, 0.030), mat="R1_GreyMetal",
                         bevel=0.004, segments=1)
    rise = d["handle_rise"]
    mb.add_tube_path([(0.0, top + 0.016, -0.075), (0.0, top + rise * 0.85, -0.060), (0.0, top + rise, -0.030),
                      (0.0, top + rise, 0.030), (0.0, top + rise * 0.85, 0.060), (0.0, top + 0.016, 0.075)],
                     radius=d["handle_radius"], mat="R1_BareSteel", segments=8)
    mb.add_cylinder(center_u=(0.0, top + rise, 0.0), radius=d["handle_radius"] + 0.006, length=0.07,
                    mat="R1_Rubber", axis_u="Z", segments=8, bevel=0.0)
    mb.build_object(parent=root)
    r1kit.create_empty("Grip_Anchor", parent=root, pivot_u=(0.0, top + rise, 0.0))


def _build_feed_unit_connector(root: bpy.types.Object) -> None:
    """One chunky round connector at the front: a steel coupling collar on a grey boss, a copper face with a single
    keyed blade and one offset pin (two different contacts, so it reads as 'plugs in one way')."""
    d = FEED_UNIT_DIMS
    cy = d["body_height"] * 0.5
    z0 = d["body_length"] * 0.5 + 0.004
    r = d["connector_radius"]
    cl = d["connector_length"]
    mb = r1kit.MeshBuilder("Connector", pivot_u=(0.0, cy, z0))
    mb.add_cylinder(center_u=(0.0, cy, z0 + cl * 0.5), radius=r, length=cl, mat="R1_GreyMetal", axis_u="Z",
                    segments=14, bevel=0.006)
    mb.add_cylinder(center_u=(0.0, cy, z0 + cl * 0.40), radius=r + 0.008, length=0.022, mat="R1_BareSteel",
                    axis_u="Z", segments=14, bevel=0.003)
    mb.add_cylinder(center_u=(0.0, cy, z0 + cl + 0.002), radius=r * 0.78, length=0.006, mat="R1_Copper",
                    axis_u="Z", segments=14, bevel=0.0)
    mb.add_bevel_box(center_u=(0.0, cy + 0.008, z0 + cl + 0.016), size_u=(0.050, 0.012, 0.024), mat="R1_Copper",
                     bevel=0.002, segments=1)
    mb.add_cylinder(center_u=(0.018, cy - 0.020, z0 + cl + 0.014), radius=0.008, length=0.022, mat="R1_Copper",
                    axis_u="Z", segments=8, bevel=0.0)
    # The orientation key: a raised steel rib on top of the boss
    mb.add_bevel_box(center_u=(0.0, cy + r + 0.004, z0 + cl * 0.55), size_u=(0.016, 0.010, cl * 0.8),
                     mat="R1_BareSteel", bevel=0.002, segments=1)
    mb.build_object(parent=root)
    r1kit.create_empty("Connector_Anchor", parent=root, pivot_u=(0.0, cy, z0 + cl + 0.028),
                       outward_dir_u=(0.0, 0.0, 1.0))


def _build_feed_unit_label(root: bpy.types.Object) -> None:
    """A riveted steel label plate on the right flank (UI puts `RELAY SPARE · FEED UNIT` on it)."""
    d = FEED_UNIT_DIMS
    x = -(d["body_width"] * 0.5 + 0.003)
    cy = d["body_height"] * 0.5 - 0.008
    mb = r1kit.MeshBuilder("LabelPlate", pivot_u=(x, cy, 0.035))
    mb.add_bevel_box(center_u=(x, cy, 0.035), size_u=(0.006, 0.060, 0.130), mat="R1_Signage", bevel=0.002,
                     segments=1)
    for z in (-0.024, 0.094):
        mb.add_cylinder(center_u=(x - 0.003, cy, z), radius=0.005, length=0.004, mat="R1_BareSteel", axis_u="X",
                        segments=6, bevel=0.0)
    mb.build_object(parent=root)


def _build_feed_unit_pigtail(root: bpy.types.Object) -> None:
    """A short rubber cable out of the back through a strain relief, curling on the ground to a small grey plug."""
    d = FEED_UNIT_DIMS
    z_back = -d["body_length"] * 0.5 - 0.004
    y0 = 0.060
    cr = d["cable_radius"]
    mb = r1kit.MeshBuilder("Pigtail", pivot_u=(0.0, y0, z_back))
    mb.add_cylinder(center_u=(0.0, y0, z_back - 0.016), radius=0.022, length=0.032, mat="R1_Rubber", axis_u="Z",
                    segments=10, bevel=0.003, radius_top=0.015)
    path = [(0.0, y0, z_back - 0.030), (0.004, y0 - 0.012, z_back - 0.070), (0.020, cr + 0.010, z_back - 0.110),
            (0.060, cr, z_back - 0.140), (0.110, cr, z_back - 0.135), (0.140, cr, z_back - 0.100)]
    mb.add_tube_path(path, radius=cr, mat="R1_Rubber", segments=8)
    mb.add_cylinder(center_u=(0.152, cr + 0.006, z_back - 0.080), radius=0.017, length=0.044, mat="R1_GreyMetal",
                    axis_u="Z", segments=10, bevel=0.003, rot_deg_u=(0.0, 30.0, 0.0))
    mb.build_object(parent=root)


def build_feed_unit_spare() -> bpy.types.Object:
    """Build the FeedUnitSpare hierarchy in the current scene and bake its vertex colours."""
    root = r1kit.create_empty("FeedUnitSpare", pivot_u=(0.0, 0.0, 0.0))
    _build_feed_unit_body(root)
    _build_feed_unit_handle(root)
    _build_feed_unit_connector(root)
    _build_feed_unit_label(root)
    _build_feed_unit_pigtail(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=FEED_UNIT_DIMS["body_length"] * 0.6)
    return root


# ----------------------------------------------------------------------------------------------------------------
# Service cabinet
# ----------------------------------------------------------------------------------------------------------------


def _build_cabinet_body(root: bpy.types.Object) -> None:
    """The open-fronted shell: back, sides, top and floor in cream enamel, a grey liner and door jambs, two skids,
    the beltline stripe, side louvres, a sage salvage patch and the hinge knuckles."""
    d = CABINET_DIMS
    w, dp, h, t = d["width"], d["depth"], d["height"], d["wall"]
    y0 = d["skid_height"]
    box_h = h - y0
    cy = y0 + box_h * 0.5
    mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(0.0, cy, -dp * 0.5 + t * 0.5), size_u=(w, box_h, t), mat="R1_CreamEnamel",
                     bevel=0.008, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 - t * 0.5), cy, 0.0), size_u=(t, box_h, dp), mat="R1_CreamEnamel",
                         bevel=0.008, segments=1)
    # The top overhangs a little all round: a lid plate bolted on, not a rain hood
    mb.add_bevel_box(center_u=(0.0, h - t * 0.5 + 0.006, 0.006), size_u=(w + 0.03, t, dp + 0.03),
                     mat="R1_CreamEnamel", bevel=0.010, segments=1)
    mb.add_bevel_box(center_u=(0.0, y0 + t * 0.5, 0.0), size_u=(w, t, dp), mat="R1_CreamEnamel", bevel=0.006,
                     segments=1)
    # Grey liner on the inside back so the lit shelf reads against a darker field
    mb.add_bevel_box(center_u=(0.0, cy, -dp * 0.5 + t + 0.004), size_u=(w - 2 * t - 0.004, box_h - 2 * t, 0.008),
                     mat="R1_GreyMetal", bevel=0.002, segments=1)
    jw = d["jamb_width"]
    front = dp * 0.5 + 0.004
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 - jw * 0.5), cy, front), size_u=(jw, box_h, 0.012),
                         mat="R1_GreyMetal", bevel=0.003, segments=1)
    for y in (y0 + jw * 0.5, h - t - jw * 0.5 + 0.004):
        mb.add_bevel_box(center_u=(0.0, y, front), size_u=(w - 2 * jw, jw, 0.012), mat="R1_GreyMetal", bevel=0.003,
                         segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * 0.20, y0 * 0.5, 0.0), size_u=(0.08, y0, dp + 0.06), mat="R1_GreyMetal",
                         bevel=0.008, segments=1)
    # Beltline stripe round the back and both flanks (the door carries its own length of it)
    stripe_y = y0 + 0.13
    mb.add_bevel_box(center_u=(0.0, stripe_y, -dp * 0.5 - 0.002), size_u=(w + 0.004, 0.05, 0.006),
                     mat="R1_OrangeStripe", bevel=0.002, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 + 0.002), stripe_y, 0.0), size_u=(0.006, 0.05, dp + 0.004),
                         mat="R1_OrangeStripe", bevel=0.002, segments=1)
    # Three wide slanted louvres high on the +X flank: the electronics bay's cooling
    for i in range(3):
        mb.add_bevel_box(center_u=(w * 0.5 + 0.006, 0.56 + i * 0.07, -0.02), size_u=(0.010, 0.045, 0.26),
                         mat="R1_GreyMetal", bevel=0.002, segments=1, rot_deg_u=(0.0, 0.0, -30.0))
    # A riveted sage salvage panel patched over the -X flank, slightly crooked
    mb.add_bevel_box(center_u=(-w * 0.5 - 0.008, 0.52, 0.05), size_u=(0.010, 0.22, 0.20), mat="R1_SagePatch",
                     bevel=0.003, segments=1, rot_deg_u=(4.0, 0.0, 0.0))
    for y, z in ((0.62, -0.03), (0.42, 0.13)):
        mb.add_cylinder(center_u=(-w * 0.5 - 0.014, y, z), radius=0.008, length=0.006, mat="R1_BareSteel",
                        axis_u="X", segments=6, bevel=0.0)
    # Hinge knuckles on the -X jamb; the lower one is bent, which is why the door sags
    hx = -w * 0.5 + 0.004
    hz = dp * 0.5 + 0.012
    for y, tilt in ((y0 + 0.14, 8.0), (h - 0.14, 0.0)):
        mb.add_cylinder(center_u=(hx, y, hz), radius=0.012, length=0.07, mat="R1_BareSteel", axis_u="Y",
                        segments=8, bevel=0.0, rot_deg_u=(0.0, 0.0, tilt))
    mb.build_object(parent=root)


def _build_cabinet_shelf(root: bpy.types.Object) -> None:
    """A steel shelf with a front lip on two brackets; the spare lies on it, connector toward the door."""
    d = CABINET_DIMS
    w, dp, t = d["width"], d["depth"], d["wall"]
    sy = d["shelf_y"]
    inner_w = w - 2 * t - 0.006
    mb = r1kit.MeshBuilder("Shelf", pivot_u=(0.0, sy, 0.0))
    mb.add_bevel_box(center_u=(0.0, sy, -0.01), size_u=(inner_w, 0.012, dp - 2 * t - 0.02), mat="R1_BareSteel",
                     bevel=0.003, segments=1)
    mb.add_bevel_box(center_u=(0.0, sy + 0.014, dp * 0.5 - t - 0.02), size_u=(inner_w, 0.028, 0.008),
                     mat="R1_BareSteel", bevel=0.002, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_strut((sx * (inner_w * 0.5 - 0.01), sy - 0.006, -0.12), (sx * (inner_w * 0.5 - 0.01), sy - 0.10,
                     -dp * 0.5 + t + 0.01), 0.020, 0.008, "R1_GreyMetal", bevel=0.002)
    mb.build_object(parent=root)
    r1kit.create_empty("Part_Anchor", parent=root, pivot_u=(0.0, sy + 0.006, -0.01))


def _build_cabinet_lamp(root: bpy.types.Object) -> None:
    """A warm strip lamp in a grey channel along the inside front of the lid, shining down on the shelf: wide enough
    to read through the open door from 15 m at night. `Light_Anchor` is where Gameplay hangs its small warm light."""
    d = CABINET_DIMS
    ly, lz = d["lamp_y"], d["lamp_z"]
    length = d["lamp_length"]
    mb = r1kit.MeshBuilder("InteriorLamp", pivot_u=(0.0, ly, lz))
    mb.add_bevel_box(center_u=(0.0, ly + 0.020, lz), size_u=(length + 0.04, 0.026, 0.056), mat="R1_GreyMetal",
                     bevel=0.004, segments=1)
    mb.add_cylinder(center_u=(0.0, ly, lz), radius=0.015, length=length, mat="R1_LampAmber", axis_u="X",
                    segments=10, bevel=0.004)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (length * 0.5 + 0.008), ly + 0.004, lz), size_u=(0.014, 0.036, 0.044),
                         mat="R1_GreyMetal", bevel=0.003, segments=1)
    mb.build_object(parent=root)
    r1kit.create_empty("Light_Anchor", parent=root, pivot_u=(0.0, ly - 0.04, lz))


def _build_cabinet_door(root: bpy.types.Object) -> None:
    """The door, pivoted on its hinge line: cream outside with its stripe, a grey inner skin with a rubber gasket,
    a latch, and the cut seal bead along its free edge as its own node (`DoorSeal`)."""
    d = CABINET_DIMS
    w, dp, h, y0 = d["width"], d["depth"], d["height"], d["skid_height"]
    hx = -w * 0.5 + 0.004
    hz = dp * 0.5 + 0.012
    door_w = w - 0.012
    door_h = h - y0 - 0.02
    cy = y0 + 0.01 + door_h * 0.5
    cx = hx + door_w * 0.5 + 0.008
    door = r1kit.MeshBuilder("Door", pivot_u=(hx, cy, hz))
    door.add_bevel_box(center_u=(cx, cy, hz + 0.006), size_u=(door_w, door_h, 0.020), mat="R1_CreamEnamel",
                       bevel=0.006, segments=1)
    door.add_bevel_box(center_u=(cx, cy, hz - 0.006), size_u=(door_w - 0.05, door_h - 0.05, 0.006),
                       mat="R1_GreyMetal", bevel=0.002, segments=1)
    door.add_bevel_box(center_u=(cx, y0 + 0.13, hz + 0.017), size_u=(door_w, 0.05, 0.004), mat="R1_OrangeStripe",
                       bevel=0.001, segments=1)
    for sy in (-1.0, 1.0):
        door.add_bevel_box(center_u=(cx, cy + sy * (door_h * 0.5 - 0.03), hz - 0.010),
                           size_u=(door_w - 0.06, 0.010, 0.008), mat="R1_Rubber", bevel=0.0, segments=0)
    for sx in (-1.0, 1.0):
        door.add_bevel_box(center_u=(cx + sx * (door_w * 0.5 - 0.03), cy, hz - 0.010),
                           size_u=(0.010, door_h - 0.07, 0.008), mat="R1_Rubber", bevel=0.0, segments=0)
    free_x = hx + door_w + 0.008
    door.add_bevel_box(center_u=(free_x - 0.045, cy + 0.04, hz + 0.024), size_u=(0.024, 0.12, 0.016),
                       mat="R1_BareSteel", bevel=0.004, segments=1)
    door.add_bevel_box(center_u=(free_x - 0.045, cy + 0.04, hz + 0.034), size_u=(0.018, 0.022, 0.012),
                       mat="R1_GreyMetal", bevel=0.002, segments=1)
    door_obj = door.build_object(parent=root)

    seal = r1kit.MeshBuilder("DoorSeal", pivot_u=(free_x, cy, hz))
    steps = 7
    for i in range(steps):
        ya = cy - door_h * 0.44 + door_h * 0.88 * i / steps
        yb = cy - door_h * 0.44 + door_h * 0.88 * (i + 1) / steps
        jog = 0.004 if i % 2 == 0 else -0.003
        seal.add_strut((free_x - 0.002, ya, hz + 0.012), (free_x + jog, yb, hz + 0.014), 0.010, 0.009,
                       "R1_BareSteel", bevel=0.002)
    seal.build_object(parent=door_obj)


def pose_service_cabinet_rest(root: bpy.types.Object) -> None:
    """The door hangs open on its bent lower hinge (Blender Z is Unity Y; +Z swings the free edge out to +Z)."""
    d = CABINET_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "Door":
            obj.rotation_euler = (0.0, math.radians(d["door_sag_deg"]), math.radians(d["door_open_deg"]))


def build_service_cabinet() -> bpy.types.Object:
    """Build the ServiceCabinet hierarchy in the current scene and bake its vertex colours."""
    root = r1kit.create_empty("ServiceCabinet", pivot_u=(0.0, 0.0, 0.0))
    _build_cabinet_body(root)
    _build_cabinet_shelf(root)
    _build_cabinet_lamp(root)
    _build_cabinet_door(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=CABINET_DIMS["width"] * 0.6)
    pose_service_cabinet_rest(root)
    bpy.context.view_layer.update()
    return root


# ----------------------------------------------------------------------------------------------------------------
# Relay contact pad
# ----------------------------------------------------------------------------------------------------------------


def _build_pad_deck(root: bpy.types.Object) -> None:
    """A low chamfered steel deck with faded entry chevrons, a welded sage patch over a crack and its broken corner
    lying beside it."""
    d = PAD_DIMS
    w, length, dh = d["width"], d["length"], d["deck_height"]
    mb = r1kit.MeshBuilder("Deck", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(0.0, dh * 0.5, 0.0), size_u=(w, dh, length), mat="R1_GreyMetal", bevel=0.045,
                     segments=1, top_scale_xz=(0.95, 0.96))
    # Two raised steel kerbs outside 07's tyres: guides worn down to a low lip
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 - 0.07), dh + 0.012, 0.0), size_u=(0.06, 0.024, length - 0.30),
                         mat="R1_BareSteel", bevel=0.008, segments=1)
    for i, z in enumerate((length * 0.5 - 0.10, length * 0.5 - 0.19)):
        mb.add_bevel_box(center_u=(0.0, dh + 0.001, z), size_u=(w - 0.40 - i * 0.12, 0.004, 0.05),
                         mat="R1_OrangeStripe", bevel=0.001, segments=1)
    mb.add_bevel_box(center_u=(0.42, dh + 0.003, -0.38), size_u=(0.34, 0.008, 0.26), mat="R1_SagePatch",
                     bevel=0.003, segments=1, rot_deg_u=(0.0, 9.0, 0.0))
    mb.add_bevel_box(center_u=(-w * 0.5 - 0.14, 0.03, length * 0.5 - 0.20), size_u=(0.22, 0.03, 0.18),
                     mat="R1_GreyMetal", bevel=0.012, segments=1, rot_deg_u=(6.0, 28.0, -11.0))
    mb.build_object(parent=root)


def _build_pad_wheel_pads(root: bpy.types.Object) -> None:
    """Rubber wheel pads on 07's track (±0.61 m) under its four wheels; the front-left one is torn half away."""
    d = PAD_DIMS
    y = d["deck_height"] + 0.006
    mb = r1kit.MeshBuilder("WheelPads", pivot_u=(0.0, y, 0.0))
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            torn = sx > 0 and sz > 0
            pad_length = d["wheel_pad_length"] * (0.55 if torn else 1.0)
            pz = sz * d["wheel_z"] - (d["wheel_pad_length"] - pad_length) * 0.5 * sz
            mb.add_bevel_box(center_u=(sx * d["wheel_x"], y, pz), size_u=(d["wheel_pad_width"], 0.012, pad_length),
                             mat="R1_Rubber", bevel=0.004, segments=1)
    mb.build_object(parent=root)


def _build_pad_contact_shoes(root: bpy.types.Object) -> None:
    """Two copper contact shoes on a rubber insulator block at the pad's centre, where 07's belly contact lands."""
    d = PAD_DIMS
    y = d["deck_height"]
    mb = r1kit.MeshBuilder("ContactShoes", pivot_u=(0.0, y, 0.0))
    mb.add_bevel_box(center_u=(0.0, y + 0.010, 0.0), size_u=(0.42, 0.020, 0.52), mat="R1_Rubber", bevel=0.006,
                     segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * d["shoe_x"], y + 0.030, 0.0), size_u=(0.10, 0.020, 0.40), mat="R1_Copper",
                         bevel=0.006, segments=1)
    mb.build_object(parent=root)
    r1kit.create_empty("Contact_Anchor", parent=root, pivot_u=(0.0, y + 0.040, 0.0))


def _build_pad_conduit(root: bpy.types.Object) -> None:
    """A junction box at the pad's back edge and the armoured conduit running off toward the mast."""
    d = PAD_DIMS
    z_back = -d["length"] * 0.5
    mb = r1kit.MeshBuilder("Conduit", pivot_u=(0.0, 0.0, z_back))
    mb.add_bevel_box(center_u=(0.0, 0.09, z_back - 0.06), size_u=(0.26, 0.18, 0.16), mat="R1_CreamEnamel",
                     bevel=0.012, segments=1)
    mb.add_bevel_box(center_u=(0.0, 0.12, z_back + 0.022), size_u=(0.20, 0.03, 0.006), mat="R1_OrangeStripe",
                     bevel=0.002, segments=1)
    mb.add_tube_path([(0.0, 0.045, z_back - 0.14), (0.0, 0.035, z_back - 0.30), (0.06, 0.028, z_back - 0.55),
                      (0.05, 0.028, z_back - 0.85)], radius=0.026, mat="R1_GreyMetal", segments=8)
    mb.add_cylinder(center_u=(0.0, 0.045, z_back - 0.155), radius=0.036, length=0.03, mat="R1_BareSteel",
                    axis_u="Z", segments=10, bevel=0.003)
    mb.build_object(parent=root)

    lamp = r1kit.MeshBuilder("PadLamp", pivot_u=(0.0, 0.18, z_back - 0.06))
    lamp.add_cylinder(center_u=(0.0, 0.186, z_back - 0.06), radius=0.034, length=0.012, mat="R1_GreyMetal",
                      axis_u="Y", segments=12, bevel=0.002)
    lamp.add_cylinder(center_u=(0.0, 0.200, z_back - 0.06), radius=0.026, length=0.018, mat="R1_LampAmber",
                      axis_u="Y", segments=12, bevel=0.005)
    lamp.build_object(parent=root)


def build_relay_contact_pad() -> bpy.types.Object:
    """Build the RelayContactPad hierarchy in the current scene and bake its vertex colours."""
    root = r1kit.create_empty("RelayContactPad", pivot_u=(0.0, 0.0, 0.0))
    _build_pad_deck(root)
    _build_pad_wheel_pads(root)
    _build_pad_contact_shoes(root)
    _build_pad_conduit(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=PAD_DIMS["length"] * 0.55)
    return root


def main() -> None:
    builds = (
        (build_service_cabinet, MODELS_DIR / "Structures" / "ServiceCabinet.fbx"),
        (build_feed_unit_spare, MODELS_DIR / "Props" / "FeedUnitSpare.fbx"),
        (build_relay_contact_pad, MODELS_DIR / "Structures" / "RelayContactPad.fbx"),
    )
    for build, out in builds:
        r1kit.reset_scene()
        root = build()
        print(f"[{root.name}] Triangles: {r1kit.count_triangles(root)}")
        r1kit.export_fbx(root, out)


if __name__ == "__main__":
    main()
