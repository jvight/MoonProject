"""Build script for relay.2's hut (`Assets/_Project/Art/R1/Models/Structures/RelayHut.fbx`).

A 1980s SSR-3 network relay hut, the same family as relay.1's dead-mast cabinet and the service cabinet: cream enamel
walls on a grey skid, the network's orange band, a grey roof slab overhanging all round, and a 2.4 m mast stub on the
roof carrying a caged lamp housing. Nodes World places by path (contract): `RelayHut`, `RelayHut/Door` (fixed, it
never opens), `RelayHut/MastStub` and `RelayHut/MastStub/RelayLamp` (the lens; dark until lit through its
`_EmissionColor`). Footprint about 2.2 x 2.1 m, 1.8 m to the roof's top, root on the ground at the footprint centre,
the door facing +Z. Vacuum logic: no rain hood or gutter; the roof is a radiator and shade slab.

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/relay_hut.py
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
FBX_OUT = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Structures" / "RelayHut.fbx"

# Unity metres: X right, Y up, Z forward (the door's face). Root on the ground at the footprint centre.
HUT_DIMS: dict[str, float] = {
    "width": 2.2,
    "depth": 2.1,
    "skid_height": 0.10,
    "wall_top_y": 1.70,
    "roof_top_y": 1.80,
    "roof_overhang": 0.16,
    "band_y": 1.05,
    "band_height": 0.12,
    "door_x": -0.45,
    "door_width": 0.82,
    "door_height": 1.48,
    "mast_x": 0.62,
    "mast_z": -0.52,
    "mast_height": 2.4,
    "mast_radius": 0.055,
}


def _build_shell(root: bpy.types.Object) -> None:
    """Skid, walls with corner trims and the orange band, side louvres, a sage salvage patch, the door frame, a label
    plate and the cable conduit at the back."""
    d = HUT_DIMS
    w, dp = d["width"], d["depth"]
    y0, y1 = d["skid_height"], d["wall_top_y"]
    wall_h = y1 - y0
    cy = y0 + wall_h * 0.5
    mb = r1kit.MeshBuilder("Shell", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(0.0, y0 * 0.5, 0.0), size_u=(w + 0.06, y0, dp + 0.06), mat="R1_GreyMetal",
                     bevel=0.02, segments=1)
    mb.add_bevel_box(center_u=(0.0, cy, 0.0), size_u=(w, wall_h, dp), mat="R1_CreamEnamel", bevel=0.035,
                     segments=2)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(center_u=(sx * (w * 0.5 - 0.01), cy, sz * (dp * 0.5 - 0.01)),
                             size_u=(0.07, wall_h, 0.07), mat="R1_GreyMetal", bevel=0.012, segments=1)
    band_y, band_h = d["band_y"], d["band_height"]
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(0.0, band_y, sz * (dp * 0.5 + 0.003)), size_u=(w - 0.08, band_h, 0.008),
                         mat="R1_OrangeStripe", bevel=0.003, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(sx * (w * 0.5 + 0.003), band_y, 0.0), size_u=(0.008, band_h, dp - 0.08),
                         mat="R1_OrangeStripe", bevel=0.003, segments=1)
    # Three wide slanted louvres high on the +X wall: the electronics' cooling
    for i in range(3):
        mb.add_bevel_box(center_u=(w * 0.5 + 0.01, 1.30 + i * 0.09, -0.25), size_u=(0.014, 0.06, 0.62),
                         mat="R1_GreyMetal", bevel=0.003, segments=1, rot_deg_u=(0.0, 0.0, -30.0))
    # A riveted sage salvage panel patched over the -X wall, a little crooked
    mb.add_bevel_box(center_u=(-w * 0.5 - 0.008, 0.62, 0.30), size_u=(0.012, 0.46, 0.52), mat="R1_SagePatch",
                     bevel=0.004, segments=1, rot_deg_u=(3.0, 0.0, 0.0))
    for y, z in ((0.80, 0.08), (0.44, 0.50)):
        mb.add_cylinder(center_u=(-w * 0.5 - 0.016, y, z), radius=0.012, length=0.008, mat="R1_BareSteel",
                        axis_u="X", segments=6, bevel=0.0)
    # Door frame on the front wall and a blank label plate over it
    front = dp * 0.5
    dx, dw, dh = d["door_x"], d["door_width"], d["door_height"]
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(dx + sx * (dw * 0.5 + 0.03), y0 + dh * 0.5 + 0.02, front + 0.012),
                         size_u=(0.06, dh + 0.04, 0.024), mat="R1_GreyMetal", bevel=0.006, segments=1)
    mb.add_bevel_box(center_u=(dx, y0 + dh + 0.06, front + 0.012), size_u=(dw + 0.12, 0.06, 0.024),
                     mat="R1_GreyMetal", bevel=0.006, segments=1)
    mb.add_bevel_box(center_u=(0.48, 1.42, front + 0.008), size_u=(0.52, 0.14, 0.012), mat="R1_BareSteel",
                     bevel=0.004, segments=1)
    # Armoured cable conduit leaving the back wall low, to the network
    back = -dp * 0.5
    mb.add_cylinder(center_u=(0.55, 0.32, back - 0.02), radius=0.06, length=0.05, mat="R1_GreyMetal", axis_u="Z",
                    segments=12, bevel=0.006)
    mb.add_tube_path([(0.55, 0.32, back - 0.04), (0.55, 0.26, back - 0.30), (0.62, 0.05, back - 0.60),
                      (0.70, 0.04, back - 1.10)], radius=0.035, mat="R1_GreyMetal", segments=10)
    mb.build_object(parent=root)


def _build_roof(root: bpy.types.Object) -> None:
    """A grey slab overhanging all round (shade and radiator, no rain logic), a raised cooling box and two lifting
    eyes at its corners."""
    d = HUT_DIMS
    o = d["roof_overhang"]
    y1, top = d["wall_top_y"], d["roof_top_y"]
    mb = r1kit.MeshBuilder("Roof", pivot_u=(0.0, y1, 0.0))
    mb.add_bevel_box(center_u=(0.0, (y1 + top) * 0.5, 0.0), size_u=(d["width"] + 2 * o, top - y1,
                     d["depth"] + 2 * o), mat="R1_GreyMetal", bevel=0.025, segments=2)
    mb.add_bevel_box(center_u=(-0.45, top + 0.07, 0.15), size_u=(0.62, 0.14, 0.48), mat="R1_CreamEnamel",
                     bevel=0.03, segments=2)
    for sx, sz in ((-1.0, 1.0), (1.0, -1.0)):
        mb.add_torus(center_u=(sx * 0.95, top + 0.05, sz * 0.90), major_radius=0.04, minor_radius=0.01,
                     mat="R1_BareSteel", axis_u="X", major_segments=12, minor_segments=6)
    mb.build_object(parent=root)


def _build_door(root: bpy.types.Object) -> None:
    """The fixed steel door: cream outer skin with a recessed grey panel, a lever handle and two hinge barrels."""
    d = HUT_DIMS
    front = d["depth"] * 0.5
    dx, dw, dh = d["door_x"], d["door_width"], d["door_height"]
    y0 = d["skid_height"]
    cy = y0 + dh * 0.5 + 0.02
    mb = r1kit.MeshBuilder("Door", pivot_u=(dx, y0, front))
    mb.add_bevel_box(center_u=(dx, cy, front + 0.014), size_u=(dw, dh, 0.03), mat="R1_CreamEnamel", bevel=0.01,
                     segments=1)
    mb.add_bevel_box(center_u=(dx, cy + 0.2, front + 0.031), size_u=(dw - 0.22, 0.52, 0.008), mat="R1_GreyMetal",
                     bevel=0.004, segments=1)
    mb.add_bevel_box(center_u=(dx + dw * 0.5 - 0.11, cy - 0.06, front + 0.045), size_u=(0.14, 0.03, 0.03),
                     mat="R1_BareSteel", bevel=0.008, segments=1)
    for y in (y0 + 0.30, y0 + dh - 0.24):
        mb.add_cylinder(center_u=(dx - dw * 0.5 + 0.01, y, front + 0.03), radius=0.018, length=0.14,
                        mat="R1_BareSteel", axis_u="Y", segments=8, bevel=0.0)
    mb.build_object(parent=root)


def _build_mast(root: bpy.types.Object) -> None:
    """The roof mast stub: a bolted base flange, a steel tube with two collars and a guy-lug, and at its top the caged
    lamp housing; the lens is its own child node (`RelayLamp`)."""
    d = HUT_DIMS
    mx, mz = d["mast_x"], d["mast_z"]
    base = d["roof_top_y"]
    top = base + d["mast_height"]
    r = d["mast_radius"]
    mb = r1kit.MeshBuilder("MastStub", pivot_u=(mx, base, mz))
    mb.add_cylinder(center_u=(mx, base + 0.02, mz), radius=0.16, length=0.04, mat="R1_GreyMetal", axis_u="Y",
                    segments=16, bevel=0.008)
    mb.add_cylinder(center_u=(mx, (base + top - 0.24) * 0.5, mz), radius=r, length=top - 0.24 - base,
                    mat="R1_BareSteel", axis_u="Y", segments=12, bevel=0.0)
    for y in (base + 0.85, base + 1.65):
        mb.add_cylinder(center_u=(mx, y, mz), radius=r + 0.018, length=0.05, mat="R1_GreyMetal", axis_u="Y",
                        segments=12, bevel=0.006)
    mb.add_bevel_box(center_u=(mx + 0.09, base + 1.65, mz), size_u=(0.10, 0.04, 0.03), mat="R1_GreyMetal",
                     bevel=0.006, segments=1)
    # Lamp housing: a grey base cup, three cage bars and a cap over the lens
    lamp_y = top - 0.12
    mb.add_cylinder(center_u=(mx, top - 0.22, mz), radius=0.10, length=0.05, mat="R1_GreyMetal", axis_u="Y",
                    segments=16, bevel=0.01)
    for angle in (0.0, 120.0, 240.0):
        a = math.radians(angle)
        cx, cz = mx + math.cos(a) * 0.09, mz + math.sin(a) * 0.09
        mb.add_strut((cx, top - 0.20, cz), (cx, top - 0.02, cz), 0.014, 0.014, "R1_BareSteel", bevel=0.003)
    mb.add_cylinder(center_u=(mx, top, mz), radius=0.11, length=0.04, mat="R1_GreyMetal", axis_u="Y", segments=16,
                    bevel=0.012)
    mast = mb.build_object(parent=root)

    lens = r1kit.MeshBuilder("RelayLamp", pivot_u=(mx, lamp_y, mz))
    lens.add_cylinder(center_u=(mx, lamp_y, mz), radius=0.07, length=0.15, mat="R1_LampAmber", axis_u="Y",
                      segments=16, bevel=0.025, bevel_segments=2)
    lens.build_object(parent=mast)


def build_relay_hut() -> bpy.types.Object:
    """Build the RelayHut hierarchy in the current scene and bake its vertex colours."""
    root = r1kit.create_empty("RelayHut", pivot_u=(0.0, 0.0, 0.0))
    _build_shell(root)
    _build_roof(root)
    _build_door(root)
    _build_mast(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=HUT_DIMS["width"] * 0.6)
    return root


def main() -> None:
    r1kit.reset_scene()
    root = build_relay_hut()
    print(f"[RelayHut] Triangles: {r1kit.count_triangles(root)}")
    r1kit.export_fbx(root, FBX_OUT)


if __name__ == "__main__":
    main()
