"""
Headless-Blender builder for 07's first two visible upgrades (opening v3 5.2), in 07's own soft shape language: the
owner's round-3 V1 ("pill pods") at 1.5x, the size that reads from the 7.5 m chase cam.

- `Assets/_Project/Art/R1/Models/Rover/Kit_SSR3Headlamp.fbx` (c): elder 06's sealed-beam lamp redrawn for 07, two
  cream enamel pill lamps lying fore-and-aft, tucked tight to the head's upper sides on short grey saddles. Root =
  07's `Neck/NeckLift/Head/BrowSocket`. `Lens_L` / `Lens_R` (R1_LampAmber: the round front lens and a small rear
  pilot, so the lamp reads lit from the chase cam behind 07) are Rover's glow renderers; `Beam_L` / `Beam_R` sit at
  the lens fronts facing +Z, for Rover's spot lights. The amber lens is its one accent.
- `Assets/_Project/Art/R1/Models/Rover/Kit_CellPod.fbx` (d): one soft rounded saddle pod of the auxiliary cell pack,
  mounted with identity on each of 07's `PodSocket_R` / `PodSocket_L` (+X outboard; Rover turns the left socket half
  round, as it does DrumSocket_L). It hugs the flank in the gap between the front and rear tyres, inside the stance.
  Its one accent is a short slim charge bar of two segments on the outer face (`ChargeBar_0..1`, R1_LampAmber, bottom
  to top), lit by Rover. Symmetric front to back, so the same kit fits both sides.

Every node name is unique. Unity metres: X right, Y up, Z forward (07's front).

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/build_kit_07_upgrades.py
"""
from __future__ import annotations

import sys
from pathlib import Path

import bpy

SCRIPT_DIR = Path(__file__).resolve().parent
BLENDER_DIR = SCRIPT_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

REPO_ROOT = SCRIPT_DIR.parents[3]
ROVER_MODELS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover"

# Kit-local metres from BrowSocket (the brow's centre on the crown's front edge, world (0, 1.53, 0.30) at rest). The
# pills hug the 0.75 m head's sides just under the crown, their fronts flush with the screen's bezel.
HEADLAMP_DIMS: dict[str, float] = {
    "pill_radius": 0.093,
    "pill_length": 0.36,
    "pill_x": 0.454,
    "pill_y": -0.13,
    "pill_z": -0.07,
    "lens_radius": 0.066,
    "pilot_radius": 0.026,
    "saddle_x": 0.385,
}

# Kit-local metres from PodSocket (the pod's inner face centre on the flank, +X outboard). 0.34 m deep so it stands
# in the gap between the front and rear tyres (z -0.18..0.18); the outer face stays inside the tyre stance.
POD_DIMS: dict[str, float] = {
    "width": 0.195,
    "height": 0.39,
    "depth": 0.34,
    "round": 0.42,
    "bar_low_y": -0.02,
    "bar_step": 0.075,
    "bar_height": 0.039,
    "bar_length": 0.135,
}


def _pill_z(mb: r1kit.MeshBuilder, center, radius: float, length: float, mat: str) -> None:
    """A capsule lying along Z: a cylinder whose deep bevel rounds both ends into domes."""
    mb.add_cylinder(center_u=center, radius=radius, length=length, mat=mat, axis_u="Z", segments=16,
                    bevel=radius * 0.9, bevel_segments=4)


# ----------------------------------------------------------------------------------------------------------------
# Kit_SSR3Headlamp
# ----------------------------------------------------------------------------------------------------------------


def _build_headlamp_side(root: bpy.types.Object, side: float, suffix: str) -> None:
    """One pill lamp on a short grey saddle against the head's side, its round lens forward and a small pilot aft."""
    d = HEADLAMP_DIMS
    x, y, z = side * d["pill_x"], d["pill_y"], d["pill_z"]
    r, length = d["pill_radius"], d["pill_length"]
    front = z + length * 0.5
    back = z - length * 0.5

    mb = r1kit.MeshBuilder(f"Housing_{suffix}", pivot_u=(x, y, z))
    _pill_z(mb, (x, y, z), r, length, "R1_CreamEnamel")
    mb.add_bevel_box(center_u=(side * d["saddle_x"], y, z), size_u=(0.03, 0.075, 0.15), mat="R1_GreyMetal",
                     bevel=0.012, segments=2)
    housing = mb.build_object(parent=root)

    lens = r1kit.MeshBuilder(f"Lens_{suffix}", pivot_u=(x, y, front))
    lens.add_cylinder(center_u=(x, y, front - 0.002), radius=d["lens_radius"], length=0.024, mat="R1_LampAmber",
                      axis_u="Z", segments=20, bevel=0.012, bevel_segments=2)
    lens.add_cylinder(center_u=(x, y, back + 0.002), radius=d["pilot_radius"], length=0.012, mat="R1_LampAmber",
                      axis_u="Z", segments=12, bevel=0.005, bevel_segments=1)
    lens.build_object(parent=housing)
    r1kit.create_empty(f"Beam_{suffix}", parent=root, pivot_u=(x, y, front + 0.012))


def build_kit_ssr3_headlamp() -> bpy.types.Object:
    """Build the Kit_SSR3Headlamp hierarchy and bake its vertex colours."""
    root = r1kit.create_empty("Kit_SSR3Headlamp", pivot_u=(0.0, 0.0, 0.0))
    _build_headlamp_side(root, 1.0, "R")
    _build_headlamp_side(root, -1.0, "L")
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.6)
    return root


# ----------------------------------------------------------------------------------------------------------------
# Kit_CellPod
# ----------------------------------------------------------------------------------------------------------------


def _build_pod_body(root: bpy.types.Object) -> None:
    """One soft rounded cream pod against the flank, and the short cable gland on its inner face into the body."""
    d = POD_DIMS
    w, h, dp = d["width"], d["height"], d["depth"]
    mb = r1kit.MeshBuilder("Pod", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(w * 0.5 - 0.01, 0.0, 0.0), size_u=(w, h, dp), mat="R1_CreamEnamel",
                     bevel=min(w, h, dp) * d["round"], segments=4)
    mb.add_cylinder(center_u=(0.0, h * 0.25, 0.0), radius=0.024, length=0.03, mat="R1_GreyMetal", axis_u="X",
                    segments=12, bevel=0.006, bevel_segments=1)
    mb.build_object(parent=root)


def _build_pod_charge_bar(root: bpy.types.Object) -> None:
    """A short slim charge bar of two segments on the outer face, bottom to top, each its own node for Rover."""
    d = POD_DIMS
    x = d["width"] - 0.009
    for i in range(2):
        y = d["bar_low_y"] + i * d["bar_step"]
        mb = r1kit.MeshBuilder(f"ChargeBar_{i}", pivot_u=(x, y, 0.0))
        mb.add_bevel_box(center_u=(x, y, 0.0), size_u=(0.014, d["bar_height"], d["bar_length"]),
                         mat="R1_LampAmber", bevel=0.009, segments=2)
        mb.build_object(parent=root)


def build_kit_cell_pod() -> bpy.types.Object:
    """Build the Kit_CellPod hierarchy and bake its vertex colours."""
    root = r1kit.create_empty("Kit_CellPod", pivot_u=(0.0, 0.0, 0.0))
    _build_pod_body(root)
    _build_pod_charge_bar(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.35)
    return root


def main() -> None:
    builds = (
        (build_kit_ssr3_headlamp, ROVER_MODELS_DIR / "Kit_SSR3Headlamp.fbx"),
        (build_kit_cell_pod, ROVER_MODELS_DIR / "Kit_CellPod.fbx"),
    )
    for build, out in builds:
        r1kit.reset_scene()
        root = build()
        print(f"[{root.name}] Triangles: {r1kit.count_triangles(root)}")
        r1kit.export_fbx(r1kit.collect_hierarchy(root), out, bake_space_transform=True)


if __name__ == "__main__":
    main()
