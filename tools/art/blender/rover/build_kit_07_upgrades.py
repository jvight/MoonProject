"""
Headless-Blender builder for 07's first two visible upgrades (opening v3 5.2, the owner's slim round-2 picks):

- `Assets/_Project/Art/R1/Models/Rover/Kit_SSR3Headlamp.fbx` (c = A): elder 06's sealed-beam lamp adapted to 07, two
  compact white box floodlights on brake-bent brackets at the head's brow corners, like ears. Root = 07's
  `Neck/Head/BrowSocket`. `Lens_L` / `Lens_R` (R1_LampAmber: the front sealed beam and the rear pilot slit, so the
  lamp reads lit from the chase cam behind 07) are Rover's glow renderers; `Beam_L` / `Beam_R` sit at the lens
  fronts facing +Z, for Rover's spot lights. Its one accent is the thin orange stripe round each housing.
- `Assets/_Project/Art/R1/Models/Rover/Kit_CellPod.fbx` (d = A): one battery saddle pod of the auxiliary cell pack,
  mounted with identity on each of 07's `PodSocket_R` / `PodSocket_L` (+X outboard; Rover turns the left socket half
  round, as it does DrumSocket_L), so its outer face stays inside the tyre stance. Latched straps, a bail handle and a cable into the body; its one
  accent is three short charge bars on the outer face (`ChargeBar_0..2`, R1_LampAmber, bottom to top), lit by Rover.
  The pod is symmetric front to back, so the same kit fits both sides.

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

# Kit-local metres from BrowSocket (the brow's centre on the crown's front edge, world (0, 1.53, 0.30) at rest).
# The housings sit just outboard of the 0.75 m head, their tops ~0.1 m above the crown: ears, not antlers.
HEADLAMP_DIMS: dict[str, float] = {
    "lamp_x": 0.482,
    "lamp_y": -0.01,
    "lamp_z": 0.0,
    "housing_width": 0.205,
    "housing_height": 0.215,
    "housing_depth": 0.225,
    "lens_size": 0.150,
    "bezel_width": 0.016,
    "head_half_width": 0.375,
    "crown_y": -0.02,
}

# Kit-local metres from PodSocket (the pod's inner face centre on the flank, +X outboard). 0.34 m deep so it stands in
# the gap between the front and rear tyres (z -0.18..0.18), its underside just above the tyre tops.
POD_DIMS: dict[str, float] = {
    "width": 0.215,
    "height": 0.34,
    "depth": 0.34,
    "bevel": 0.028,
    "strap_inset_z": 0.10,
    "bar_width": 0.020,
    "bar_height": 0.034,
    "bar_length": 0.11,
    "bar_step": 0.052,
}


# ----------------------------------------------------------------------------------------------------------------
# Kit_SSR3Headlamp
# ----------------------------------------------------------------------------------------------------------------


def _build_headlamp_side(root: bpy.types.Object, side: float, suffix: str) -> None:
    """One floodlight: a cream box housing with a thin orange stripe, a raised grey bezel round the square lens, a
    brake-bent bracket and gusset down to the crown's corner, and a short cable into the head."""
    d = HEADLAMP_DIMS
    cx, cy, cz = side * d["lamp_x"], d["lamp_y"], d["lamp_z"]
    w, h, dp = d["housing_width"], d["housing_height"], d["housing_depth"]
    front = cz + dp * 0.5
    back = cz - dp * 0.5

    mb = r1kit.MeshBuilder(f"Housing_{suffix}", pivot_u=(cx, cy, cz))
    mb.add_bevel_box(center_u=(cx, cy, cz), size_u=(w, h, dp), mat="R1_CreamEnamel", bevel=0.018, segments=2)
    mb.add_bevel_box(center_u=(cx, cy - h * 0.18, cz), size_u=(w + 0.006, 0.020, dp + 0.006), mat="R1_OrangeStripe",
                     bevel=0.004, segments=1)
    half = d["lens_size"] * 0.5 + d["bezel_width"] * 0.5
    bw = d["bezel_width"]
    for sy in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(cx, cy + sy * half, front + 0.008), size_u=(d["lens_size"] + 2 * bw, bw, 0.018),
                         mat="R1_GreyMetal", bevel=0.004, segments=1)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(cx + sx * half, cy, front + 0.008), size_u=(bw, d["lens_size"], 0.018),
                         mat="R1_GreyMetal", bevel=0.004, segments=1)
    # Dark reflector field behind the sealed beam, and the rear pilot slit's grey hood
    mb.add_bevel_box(center_u=(cx, cy, front + 0.001), size_u=(d["lens_size"], d["lens_size"], 0.004),
                     mat="R1_DarkGlass", bevel=0.0, segments=0)
    mb.add_bevel_box(center_u=(cx, cy + 0.045, back - 0.006), size_u=(0.13, 0.034, 0.014), mat="R1_GreyMetal",
                     bevel=0.003, segments=1)
    # Brake-bent bracket: a plate along the crown's corner, welded to the housing's inner face, with a gusset
    inner_x = cx - side * w * 0.5
    head_x = side * d["head_half_width"]
    plate_x = (inner_x + head_x - side * 0.07) * 0.5
    plate_w = abs(inner_x - (head_x - side * 0.07))
    mb.add_bevel_box(center_u=(plate_x, d["crown_y"] + 0.006, cz - 0.02), size_u=(plate_w, 0.012, 0.12),
                     mat="R1_GreyMetal", bevel=0.003, segments=1)
    mb.add_strut((inner_x - side * 0.004, cy - h * 0.40, cz - 0.02), (inner_x - side * 0.05, d["crown_y"] + 0.012,
                 cz - 0.02), 0.012, 0.09, "R1_GreyMetal", bevel=0.003)
    # Power cable from the housing's back into the crown
    mb.add_tube_path([(cx - side * 0.04, cy - 0.03, back - 0.004), (cx - side * 0.06, cy - 0.06, back - 0.05),
                      (head_x - side * 0.12, d["crown_y"] + 0.02, back - 0.04),
                      (head_x - side * 0.16, d["crown_y"] - 0.004, back - 0.01)],
                     radius=0.010, mat="R1_Rubber", segments=8)
    housing = mb.build_object(parent=root)

    lens = r1kit.MeshBuilder(f"Lens_{suffix}", pivot_u=(cx, cy, front))
    lens.add_bevel_box(center_u=(cx, cy, front + 0.008), size_u=(d["lens_size"] - 0.004, d["lens_size"] - 0.004,
                       0.014), mat="R1_LampAmber", bevel=0.006, segments=2)
    lens.add_bevel_box(center_u=(cx, cy + 0.045, back - 0.002), size_u=(0.11, 0.016, 0.010), mat="R1_LampAmber",
                       bevel=0.003, segments=1)
    lens.build_object(parent=housing)
    r1kit.create_empty(f"Beam_{suffix}", parent=root, pivot_u=(cx, cy, front + 0.02))


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
    """The cream chamfered cell box, two grey straps with over-centre latches, a bail handle on top and the cable that
    leaves the inner face into 07's flank."""
    d = POD_DIMS
    w, h, dp = d["width"], d["height"], d["depth"]
    cx = w * 0.5
    mb = r1kit.MeshBuilder("Pod", pivot_u=(0.0, 0.0, 0.0))
    mb.add_bevel_box(center_u=(cx, 0.0, 0.0), size_u=(w, h, dp), mat="R1_CreamEnamel", bevel=d["bevel"], segments=2)
    # A grey back plate against the flank, the pod's mount
    mb.add_bevel_box(center_u=(0.008, 0.0, 0.0), size_u=(0.016, h - 0.04, dp - 0.06), mat="R1_GreyMetal",
                     bevel=0.004, segments=1)
    for sz in (-1.0, 1.0):
        z = sz * d["strap_inset_z"]
        mb.add_bevel_box(center_u=(cx + 0.004, 0.0, z), size_u=(w + 0.008, h + 0.010, 0.030), mat="R1_GreyMetal",
                         bevel=0.004, segments=1)
        # Over-centre latch on the outer face, high on the strap: a steel lever over a grey keeper
        mb.add_bevel_box(center_u=(w + 0.010, h * 0.30, z), size_u=(0.012, 0.050, 0.034), mat="R1_GreyMetal",
                         bevel=0.003, segments=1)
        mb.add_bevel_box(center_u=(w + 0.018, h * 0.30 - 0.012, z), size_u=(0.010, 0.060, 0.020),
                         mat="R1_BareSteel", bevel=0.003, segments=1, rot_deg_u=(0.0, 0.0, -8.0 * sz))
    # Bail handle across the top, on two grey lugs
    top = h * 0.5
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(cx, top + 0.010, sz * 0.070), size_u=(0.044, 0.020, 0.026), mat="R1_GreyMetal",
                         bevel=0.004, segments=1)
    mb.add_tube_path([(cx, top + 0.016, -0.070), (cx, top + 0.042, -0.058), (cx, top + 0.052, -0.025),
                      (cx, top + 0.052, 0.025), (cx, top + 0.042, 0.058), (cx, top + 0.016, 0.070)],
                     radius=0.009, mat="R1_BareSteel", segments=8)
    # Dark window round the charge bars on the outer face
    mb.add_bevel_box(center_u=(w + 0.001, -0.02, 0.0), size_u=(0.006, 0.19, d["bar_length"] + 0.05),
                     mat="R1_DarkGlass", bevel=0.004, segments=1)
    # Power cable: out of the inner face's top through a gland, into the flank
    mb.add_cylinder(center_u=(0.030, top - 0.05, 0.0), radius=0.020, length=0.03, mat="R1_GreyMetal", axis_u="X",
                    segments=10, bevel=0.003)
    mb.add_tube_path([(0.040, top - 0.05, 0.0), (0.020, top + 0.004, 0.0), (-0.010, top - 0.01, 0.0),
                      (-0.030, top - 0.06, 0.0)], radius=0.012, mat="R1_Rubber", segments=8)
    mb.build_object(parent=root)


def _build_pod_charge_bars(root: bpy.types.Object) -> None:
    """Three short horizontal amber bars, bottom to top, each its own node so Rover can light the charge level."""
    d = POD_DIMS
    x = d["width"] + 0.006
    for i in range(3):
        y = -0.02 - d["bar_step"] + i * d["bar_step"]
        mb = r1kit.MeshBuilder(f"ChargeBar_{i}", pivot_u=(x, y, 0.0))
        mb.add_bevel_box(center_u=(x, y, 0.0), size_u=(d["bar_width"], d["bar_height"], d["bar_length"]),
                         mat="R1_LampAmber", bevel=0.006, segments=1)
        mb.build_object(parent=root)


def build_kit_cell_pod() -> bpy.types.Object:
    """Build the Kit_CellPod hierarchy and bake its vertex colours."""
    root = r1kit.create_empty("Kit_CellPod", pivot_u=(0.0, 0.0, 0.0))
    _build_pod_body(root)
    _build_pod_charge_bars(root)
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
