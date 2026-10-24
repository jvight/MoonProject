"""Concept sheet generator for 0.6 Rover Upgrades (Cell Pack III) and Field Charge Pylon.

Deliverables produced under docs/art/concepts/0.6-upgrades/:
  - cellpack3_sheet.png : 3 variants of Cell Pack III (Back socket, 8 cells, ~370 m range),
                          each stacking on top of the V1 cell pods, rendered from 7.5 m
                          chase cam (16 deg pitch, 52 deg FOV), front 3/4, and rear 3/4 in Day C.
  - pylon_sheet.png     : 3 variants of Field Charge Pylon (placeable forward charger on a
                          predefined pad, 2-3 m tall, same maker family as ChargingPad and
                          relay masts, vacuum logic), rendered from chase cam, front 3/4,
                          and rear 3/4 in Day C alongside Rover 07 for scale.
  - grown_0.6.png       : One "fully grown 0.6" Rover 07 with V1 headlamp + V1 cells +
                          Cell Pack III (recommended Variant A), rendered from 7.5 m chase cam,
                          front 3/4, and rear 3/4 in Day C, with growth progression strip.

Run headless via:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P docs/art/concepts/0.6-upgrades/render_06_upgrades.py
"""

from __future__ import annotations

import json
import math
import shutil
import subprocess
import sys
from pathlib import Path

import numpy as np

REPO_ROOT = Path(__file__).resolve().parents[4]
BLENDER_DIR = REPO_ROOT / "tools" / "art" / "blender"
ROVER_DIR = BLENDER_DIR / "rover"
OUT_DIR = Path(__file__).resolve().parent
TMP_DIR = OUT_DIR / "_tmp_renders"

IN_BLENDER = "--compose-only" not in sys.argv

if IN_BLENDER:
    import bmesh
    import bpy
    from mathutils import Euler, Matrix, Vector

    for p in (str(BLENDER_DIR), str(ROVER_DIR)):
        if p not in sys.path:
            sys.path.insert(0, p)

    import r1kit  # noqa: E402
    from build_kit_07_upgrades import (  # noqa: E402
        build_kit_cell_pod,
        build_kit_ssr3_headlamp,
    )
    from build_rover import DIMENSIONS, MATERIALS, build_rover_model_hierarchy  # noqa: E402
    from render_suite import enhance_materials_with_vertex_color_for_render  # noqa: E402
    from rover_parts import (  # noqa: E402
        configure_procedural_crt_screen_shader,
        create_material_library,
    )
else:
    from PIL import Image, ImageDraw, ImageFont


# ---------------------------------------------------------------------------
# Material & Vertex-Colour Helpers
# ---------------------------------------------------------------------------


def sync_all_vertex_color_attributes() -> None:
    """Ensure every mesh has both 'Col' and 'Color' attributes populated so both rover and r1kit shaders read AO/wear/cavity."""
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.data is None:
            continue
        me = obj.data
        if len(me.loops) == 0:
            continue
        src_attr = me.color_attributes.get("Col") or me.color_attributes.get("Color")
        if src_attr is None:
            ca = me.color_attributes.new(name="Col", type="FLOAT_COLOR", domain="CORNER")
            arr = np.tile(np.array([0.92, 0.06, 0.02, 1.0], dtype=np.float32), len(me.loops))
            ca.data.foreach_set("color", arr)
            src_attr = ca

        buf = np.zeros(len(me.loops) * 4, dtype=np.float32)
        src_attr.data.foreach_get("color", buf)

        for target_name in ("Col", "Color"):
            if target_name not in me.color_attributes:
                ca_t = me.color_attributes.new(name=target_name, type="FLOAT_COLOR", domain="CORNER")
                ca_t.data.foreach_set("color", buf)
        me.color_attributes.active_color = me.color_attributes["Col"]
        me.update()


def find_hierarchy_object(root: bpy.types.Object, name_prefix: str) -> bpy.types.Object:
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == name_prefix or obj.name.startswith(name_prefix + "."):
            return obj
    raise KeyError(f"Could not find '{name_prefix}' under '{root.name}'")


def attach_to_socket(child_root: bpy.types.Object, socket_obj: bpy.types.Object, rot_y_deg: float = 0.0) -> None:
    child_root.parent = socket_obj
    child_root.location = (0.0, 0.0, 0.0)
    child_root.rotation_euler = (0.0, 0.0, math.radians(rot_y_deg))
    child_root.scale = (1.0, 1.0, 1.0)


def _add_soft_box(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    size_u: tuple[float, float, float],
    mat: str,
    round_ratio: float = 0.42,
    segments: int = 4,
) -> None:
    w, h, dp = size_u
    mb.add_bevel_box(
        center_u=center_u,
        size_u=size_u,
        mat=mat,
        bevel=min(w, h, dp) * round_ratio,
        segments=segments,
    )


def _add_pill(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    radius: float,
    length: float,
    axis_u: str,
    mat: str,
    segments: int = 20,
) -> None:
    mb.add_cylinder(
        center_u=center_u,
        radius=radius,
        length=length,
        mat=mat,
        axis_u=axis_u,
        segments=segments,
        bevel=radius * 0.9,
        bevel_segments=4,
    )


def finalize_render_materials() -> None:
    """Restore 07's canonical R1 render materials + vertex-color AO/wear/cavity shaders after all r1kit.MeshBuilder builds complete."""
    sync_all_vertex_color_attributes()
    rover_mats = create_material_library(MATERIALS, for_render=False)
    configure_procedural_crt_screen_shader(rover_mats["R1_Screen"], MATERIALS["R1_Screen"])
    enhance_materials_with_vertex_color_for_render(rover_mats)
    # Also ensure R1_Basalt exists if used
    if "R1_Basalt" not in rover_mats and "R1_GreyMetal" in rover_mats:
        rover_mats["R1_Basalt"] = rover_mats["R1_GreyMetal"]


def build_base_rover_07(include_v1_upgrades: bool = True) -> dict[str, bpy.types.Object]:
    """Build Rover 07 with its real builder hierarchy, CRT screen shader, and optional V1 upgrades."""
    rover_mats = create_material_library(MATERIALS, for_render=False)
    rover_root = build_rover_model_hierarchy(DIMENSIONS, rover_mats)

    rv_meshes = [
        o
        for o in r1kit.collect_hierarchy(rover_root)
        if o.type == "MESH" and not o.name.startswith("Decal_KA_")
    ]
    r1kit.bake_vertex_colors(rv_meshes, rover_cfg=DIMENSIONS, include_ground_occluder=True)

    parts: dict[str, bpy.types.Object] = {"rover_root": rover_root}

    if include_v1_upgrades:
        brow_socket = find_hierarchy_object(rover_root, "BrowSocket")
        headlamp = build_kit_ssr3_headlamp()
        attach_to_socket(headlamp, brow_socket)
        parts["headlamp"] = headlamp

        pod_r_socket = find_hierarchy_object(rover_root, "PodSocket_R")
        pod_r = build_kit_cell_pod()
        pod_r.name = "Kit_CellPod_R"
        attach_to_socket(pod_r, pod_r_socket, rot_y_deg=0.0)
        parts["pod_r"] = pod_r

        pod_l_socket = find_hierarchy_object(rover_root, "PodSocket_L")
        pod_l = build_kit_cell_pod()
        pod_l.name = "Kit_CellPod_L"
        attach_to_socket(pod_l, pod_l_socket, rot_y_deg=180.0)
        parts["pod_l"] = pod_l

    finalize_render_materials()
    bpy.context.view_layer.update()
    return parts


# ---------------------------------------------------------------------------
# Item 1: Cell Pack III Variants (Back socket, 8 cells, stacking on V1 pods)
# ---------------------------------------------------------------------------
# Reference coordinates in Unity world space:
# - V1 CellPod_R / CellPod_L sit at x = +/-0.548 m (span +/-0.4505..0.6455 m),
#   y = 0.685..1.075 m (center 0.88 m), z = -0.17..+0.17 m.
# - Tyre outer edge is at |x| = 0.738 m.
# - CRT Head sits at |x| <= 0.375 m, y = 1.01..1.53 m, z = -0.21..+0.435 m.
# - V1 Headlamp pills sit at x = +/-0.454 m, y = 1.40 m (1.307..1.493 m), z = 0.05..0.41 m.
# - Each Cell Pack III variant mounts to the Back socket (origin 0,0,0 on Rover_07)
#   and stacks directly on top of the left and right V1 cell pods at y = 1.07..1.29 m,
#   |x| = 0.44..0.65 m (100% inside the +/-0.738 m tyre stance!).


def build_cellpack3_variant_a() -> bpy.types.Object:
    """Variant A (Recommended): Twin Piggyback Pill Capsules ('Saddle-Top Pills').

    Two horizontal fore-and-aft rounded R1_CreamEnamel pill pods nestled directly on top
    of the left and right V1 cell pods via contoured dark-grey metal/rubber saddle collars,
    echoing the exact capsule shape of Kit_SSR3Headlamp one step lower on the shoulders.
    One warm amber accent per pod: a flush curved amber 3rd charge bar (ChargeBar_2) on the
    outer/rear flank that completes a 3-bar vertical column above V1's 2 charge bars.
    """
    root = r1kit.create_empty("Kit_CellPack3_VarA", pivot_u=(0.0, 0.0, 0.0))

    shell_mb = r1kit.MeshBuilder("CellPack3A_Shell", pivot_u=(0.0, 0.0, 0.0))
    collar_mb = r1kit.MeshBuilder("CellPack3A_Collar", pivot_u=(0.0, 0.0, 0.0))
    amber_mb = r1kit.MeshBuilder("CellPack3A_AmberBar", pivot_u=(0.0, 0.0, 0.0))

    px = 0.542
    py = 1.182
    pz = -0.025
    pill_r = 0.092
    pill_len = 0.335

    for sx in (-1.0, 1.0):
        x_c = sx * px
        # 1. Soft rubber + grey-metal contoured saddle collar resting directly on V1 pod top (y = 1.075 m)
        _add_soft_box(collar_mb, (x_c, 1.076, pz + 0.01), (0.172, 0.038, 0.272), "R1_Rubber", round_ratio=0.42)
        _add_soft_box(collar_mb, (x_c, 1.102, pz + 0.005), (0.160, 0.040, 0.242), "R1_GreyMetal", round_ratio=0.40)
        # Inboard gland bridge tying into upper body deck shoulder (x = +/-0.43 m, y = 0.96 m)
        collar_mb.add_tube_path(
            points_u=[
                (sx * 0.415, 0.955, pz - 0.06),
                (sx * 0.455, 1.040, pz - 0.05),
                (sx * 0.495, 1.102, pz - 0.04),
            ],
            radius=0.024,
            mat="R1_GreyMetal",
            segments=12,
        )

        # 2. Horizontal fore-and-aft R1_CreamEnamel pill capsule stacked on the V1 cell pod
        _add_pill(shell_mb, (x_c, py, pz), radius=pill_r, length=pill_len, axis_u="Z", mat="R1_CreamEnamel", segments=24)
        # Subtle cream enamel cradle band around waist of pill
        _add_soft_box(shell_mb, (x_c, py - 0.038, pz), (0.186, 0.058, 0.162), "R1_CreamEnamel", round_ratio=0.44)
        # Dark grey metal bezel framing the single amber accent
        _add_soft_box(
            collar_mb,
            (x_c + sx * (pill_r - 0.006), py - 0.004, pz - 0.01),
            (0.032, 0.048, 0.152),
            "R1_GreyMetal",
            round_ratio=0.42,
        )
        # 3. ONE warm amber accent: 3rd charge bar on outer flank + small rear pilot dot (like V1 headlamp)
        _add_soft_box(
            amber_mb,
            (x_c + sx * (pill_r + 0.002), py - 0.004, pz - 0.01),
            (0.024, 0.034, 0.132),
            "R1_LampAmber",
            round_ratio=0.45,
        )
        amber_mb.add_cylinder(
            center_u=(x_c, py, pz - pill_len * 0.5 + 0.002),
            radius=0.028,
            length=0.014,
            mat="R1_LampAmber",
            axis_u="Z",
            segments=14,
            bevel=0.005,
        )

    for mb in (shell_mb, collar_mb, amber_mb):
        obj = mb.build_object(parent=root)
        obj.rotation_euler = (0.0, 0.0, 0.0)

    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.6)
    sync_all_vertex_color_attributes()
    return root


def build_cellpack3_variant_b() -> bpy.types.Object:
    """Variant B: Stacked Soft Twin Lozenge Blisters + Low Rear Equaliser Arch ('Double-Decker Cushions').

    Matches the exact super-rounded cross-section of V1's Kit_CellPod in a plump upper
    second-storey lozenge blister seated on top of each V1 pod via a recessed dark rubber
    gasket, joined across the rear deck behind the CRT head by a low curved R1_CreamEnamel
    & R1_GreyMetal equaliser bridge with one warm amber capsule window.
    """
    root = r1kit.create_empty("Kit_CellPack3_VarB", pivot_u=(0.0, 0.0, 0.0))

    shell_mb = r1kit.MeshBuilder("CellPack3B_Shell", pivot_u=(0.0, 0.0, 0.0))
    collar_mb = r1kit.MeshBuilder("CellPack3B_Collar", pivot_u=(0.0, 0.0, 0.0))
    amber_mb = r1kit.MeshBuilder("CellPack3B_AmberBar", pivot_u=(0.0, 0.0, 0.0))

    px = 0.546
    py = 1.168
    pz = -0.015

    for sx in (-1.0, 1.0):
        x_c = sx * px
        # Recessed rubber waist gasket sitting directly on V1 pod crown (y = 1.075 m)
        _add_soft_box(collar_mb, (x_c, 1.076, pz + 0.01), (0.174, 0.032, 0.295), "R1_Rubber", round_ratio=0.42)
        # Upper soft rounded cream-enamel lozenge blister (matching V1 pod's 0.42 roundness)
        _add_soft_box(shell_mb, (x_c, py, pz), (0.188, 0.172, 0.315), "R1_CreamEnamel", round_ratio=0.44)
        # Soft domed top cap pill
        _add_soft_box(shell_mb, (x_c, py + 0.078, pz), (0.142, 0.048, 0.245), "R1_CreamEnamel", round_ratio=0.46)
        # Dark grey bezel + ONE amber accent continuing V1's outer vertical charge bar column
        _add_soft_box(collar_mb, (x_c + sx * 0.088, py, pz), (0.028, 0.042, 0.138), "R1_GreyMetal", round_ratio=0.42)
        _add_soft_box(amber_mb, (x_c + sx * 0.094, py, pz), (0.022, 0.026, 0.118), "R1_LampAmber", round_ratio=0.45)

    # Soft curved equaliser bridge linking left & right upper blisters behind CRT head (z = -0.22 m)
    collar_mb.add_tube_path(
        points_u=[
            (-0.475, 1.145, -0.12),
            (-0.360, 1.155, -0.21),
            (-0.180, 1.165, -0.235),
            (0.0, 1.168, -0.240),
            (0.180, 1.165, -0.235),
            (0.360, 1.155, -0.21),
            (0.475, 1.145, -0.12),
        ],
        radius=0.024,
        mat="R1_GreyMetal",
        segments=14,
    )
    # Central rounded cream-enamel junction pill on the rear bridge
    _add_pill(shell_mb, (0.0, 1.168, -0.240), radius=0.044, length=0.24, axis_u="X", mat="R1_CreamEnamel", segments=18)

    for mb in (shell_mb, collar_mb, amber_mb):
        obj = mb.build_object(parent=root)
        obj.rotation_euler = (0.0, 0.0, 0.0)

    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.6)
    sync_all_vertex_color_attributes()
    return root


def build_cellpack3_variant_c() -> bpy.types.Object:
    """Variant C: Stacked Twin Domed Cell Canisters ('Twin Flasks').

    On top of each V1 cell pod sits a contoured grey-metal double-cup cradle holding two
    plump, upright hemispherical-domed R1_CreamEnamel cell flasks (4 vertical domed flasks
    total, reading literally as the extra cell canisters of an 8-cell pack), each with a
    warm amber glowing collar ring at its base.
    """
    root = r1kit.create_empty("Kit_CellPack3_VarC", pivot_u=(0.0, 0.0, 0.0))

    shell_mb = r1kit.MeshBuilder("CellPack3C_Shell", pivot_u=(0.0, 0.0, 0.0))
    collar_mb = r1kit.MeshBuilder("CellPack3C_Collar", pivot_u=(0.0, 0.0, 0.0))
    amber_mb = r1kit.MeshBuilder("CellPack3C_AmberBar", pivot_u=(0.0, 0.0, 0.0))

    px = 0.544
    flask_z_offsets = (-0.082, 0.072)

    for sx in (-1.0, 1.0):
        x_c = sx * px
        # Contoured cradle tray on top of V1 cell pod (y = 1.075 m)
        _add_soft_box(collar_mb, (x_c, 1.082, -0.005), (0.176, 0.044, 0.318), "R1_Rubber", round_ratio=0.42)
        _add_soft_box(collar_mb, (x_c, 1.108, -0.005), (0.166, 0.036, 0.302), "R1_GreyMetal", round_ratio=0.40)
        # Inboard tie-down strut to upper body tub
        collar_mb.add_tube_path(
            points_u=[
                (sx * 0.415, 0.955, -0.04),
                (sx * 0.460, 1.040, -0.02),
                (sx * 0.495, 1.105, -0.005),
            ],
            radius=0.020,
            mat="R1_GreyMetal",
            segments=12,
        )

        # Twin upright domed cream-enamel cell flasks per side
        for fz in flask_z_offsets:
            _add_pill(shell_mb, (x_c, 1.182, fz), radius=0.072, length=0.205, axis_u="Y", mat="R1_CreamEnamel", segments=22)
            # Grey-metal cup collar around flask base
            collar_mb.add_cylinder(
                center_u=(x_c, 1.122, fz),
                radius=0.076,
                length=0.028,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=24,
                bevel=0.005,
            )
            # ONE warm amber ring collar around each flask neck
            amber_mb.add_cylinder(
                center_u=(x_c, 1.142, fz),
                radius=0.0745,
                length=0.018,
                mat="R1_LampAmber",
                axis_u="Y",
                segments=24,
                bevel=0.004,
            )

    for mb in (shell_mb, collar_mb, amber_mb):
        obj = mb.build_object(parent=root)
        obj.rotation_euler = (0.0, 0.0, 0.0)

    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=0.6)
    sync_all_vertex_color_attributes()
    return root


# ---------------------------------------------------------------------------
# Item 2: Field Charge Pylon Variants (2-3 m tall, ChargingPad + Relay Mast family)
# ---------------------------------------------------------------------------
# Shared maker family cues (VIS / ILC-R / SSR-3):
# - Chamfered octagonal R1_GreyMetal contact pad with R1_Rubber wheel lanes at x = +/-0.61 m
#   (matching 07's exact wheel track), raised R1_Copper belly contact shoes on white ceramic
#   stand-offs (R1_Signage), and R1_LampAmber entry guide strips.
# - Rounded R1_CreamEnamel equipment housing with R1_OrangeStripe beltline band, riveted
#   R1_SagePatch service plate, and a 4-bar vertical R1_LampAmber charge-state totem.
# - 2.4 m - 2.86 m tall mast/portal topped by a warm R1_LampAmber cap beacon ("charge here").
# - Strict vacuum logic: rigid R1_Copper radiative cooling fins, ceramic stand-off insulators,
#   heavy surface cable glands, and fold-out/telescoping mechanical structure.


def _add_shared_contact_pad(
    mb: r1kit.MeshBuilder,
    pad_center_z: float = 0.15,
    pad_width: float = 3.20,
    pad_depth: float = 2.85,
    deck_h: float = 0.14,
) -> None:
    """Build the shared VIS/ILC-R drive-on contact apron with +/-0.61 m wheel lanes and copper contact shoes."""
    # 1. Chamfered octagonal R1_GreyMetal base deck
    mb.add_oct_prism(
        center_u=(0.0, deck_h * 0.5, pad_center_z),
        width_x=pad_width,
        height_y=deck_h,
        depth_z=pad_depth,
        chamfer=0.48,
        mat="R1_GreyMetal",
        bevel=0.018,
        bevel_segments=2,
        top_inset=0.04,
    )
    # Fold-out deck hinge knuckles along the center-line & side seams (M3-22 unfold logic)
    for sx in (-1.0, 1.0):
        for hz in (pad_center_z - 0.75, pad_center_z + 0.75):
            mb.add_cylinder(
                center_u=(sx * 0.96, deck_h + 0.008, hz),
                radius=0.032,
                length=0.14,
                mat="R1_BareSteel",
                axis_u="Z",
                segments=12,
                bevel=0.006,
            )

    # 4 corner leveling jack feet
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            fx = sx * (pad_width * 0.5 - 0.18)
            fz = pad_center_z + sz * (pad_depth * 0.5 - 0.18)
            mb.add_cylinder(
                center_u=(fx, 0.025, fz),
                radius=0.14,
                length=0.05,
                mat="R1_Rubber",
                axis_u="Y",
                segments=16,
                bevel=0.008,
            )
            mb.add_cylinder(
                center_u=(fx, 0.09, fz),
                radius=0.045,
                length=0.12,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=8,
                bevel=0.006,
            )

    # Front drive-on wedge ramp (+Z edge) with twin warm amber entry guide strips
    ramp_z = pad_center_z + pad_depth * 0.5 + 0.24
    mb.add_bevel_box(
        center_u=(0.0, 0.055, ramp_z),
        size_u=(2.10, 0.11, 0.52),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=2,
        rot_deg_u=(10.0, 0.0, 0.0),
    )
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.86, deck_h + 0.014, pad_center_z + pad_depth * 0.5 - 0.26),
            size_u=(0.065, 0.024, 0.42),
            mat="R1_LampAmber",
            bevel=0.005,
            segments=1,
        )

    # Twin R1_Rubber wheel lanes at Rover 07's exact +/-0.61 m wheel track + front wheel stops
    lane_len = pad_depth - 0.46
    for sx in (-1.0, 1.0):
        wx = sx * 0.61
        mb.add_bevel_box(
            center_u=(wx, deck_h + 0.012, pad_center_z + 0.04),
            size_u=(0.36, 0.024, lane_len),
            mat="R1_Rubber",
            bevel=0.006,
            segments=1,
        )
        # Raised rear wheel chock so 07 stops directly over the copper shoes
        mb.add_bevel_box(
            center_u=(wx, deck_h + 0.048, pad_center_z - lane_len * 0.5 + 0.14),
            size_u=(0.38, 0.076, 0.14),
            mat="R1_BareSteel",
            bevel=0.012,
            segments=2,
        )

    # Central raised inductive/contact charging assembly: ceramic stand-offs + twin R1_Copper contact shoes
    mb.add_bevel_box(
        center_u=(0.0, deck_h + 0.018, pad_center_z - 0.02),
        size_u=(0.68, 0.036, 1.18),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=2,
    )
    for sx in (-1.0, 1.0):
        cx = sx * 0.19
        # White ceramic insulator plinth
        mb.add_bevel_box(
            center_u=(cx, deck_h + 0.045, pad_center_z - 0.02),
            size_u=(0.16, 0.032, 0.88),
            mat="R1_Signage",
            bevel=0.008,
            segments=1,
        )
        # Raised R1_Copper contact shoe
        mb.add_bevel_box(
            center_u=(cx, deck_h + 0.074, pad_center_z - 0.02),
            size_u=(0.13, 0.034, 0.80),
            mat="R1_Copper",
            bevel=0.010,
            segments=2,
        )
    # Central warm-amber "pad live" alignment strip between the copper shoes
    mb.add_bevel_box(
        center_u=(0.0, deck_h + 0.044, pad_center_z - 0.02),
        size_u=(0.055, 0.022, 0.72),
        mat="R1_LampAmber",
        bevel=0.005,
        segments=1,
    )


def _add_radiative_fin_bank(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    fin_count: int = 5,
    span_x: float = 0.36,
    fin_w: float = 0.016,
    fin_h: float = 0.24,
    fin_d: float = 0.22,
) -> None:
    """Add rigid vacuum radiative heat-rejection fins (R1_Copper plates on R1_BareSteel manifold)."""
    cx, cy, cz = center_u
    mb.add_cylinder(
        center_u=(cx, cy, cz),
        radius=0.032,
        length=span_x + 0.06,
        mat="R1_BareSteel",
        axis_u="X",
        segments=12,
        bevel=0.005,
    )
    for i in range(fin_count):
        t = (i / max(1, fin_count - 1)) - 0.5
        fx = cx + t * span_x
        mb.add_bevel_box(
            center_u=(fx, cy, cz),
            size_u=(fin_w, fin_h, fin_d),
            mat="R1_Copper",
            bevel=0.004,
            segments=1,
        )


def _add_ceramic_insulators(
    mb: r1kit.MeshBuilder,
    positions_u: list[tuple[float, float, float]],
    height: float = 0.14,
    radius: float = 0.036,
) -> None:
    """Add ridged ceramic high-voltage stand-off insulators (vacuum logic)."""
    for px, py, pz in positions_u:
        mb.add_cylinder(
            center_u=(px, py + height * 0.5, pz),
            radius=radius * 0.62,
            length=height,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.004,
        )
        for ring_i in range(3):
            ry = py + height * (0.22 + 0.28 * ring_i)
            mb.add_cylinder(
                center_u=(px, ry, pz),
                radius=radius,
                length=0.022,
                mat="R1_Signage",
                axis_u="Y",
                segments=16,
                bevel=0.005,
            )


def build_pylon_variant_a() -> bpy.types.Object:
    """Variant A (Recommended): Gooseneck Lantern Bollard & Fold-Out Contact Apron (2.58 m tall).

    Directly bridges ChargingPad's rounded cream-enamel rear bollard (orange beltline,
    sage patch, 4-bar vertical amber charge totem) with a telescoping open-lattice /
    twin-tube gooseneck mast (y = 1.24..2.58 m) that cantilevers forward over the contact
    apron to suspend a warm amber lantern head and radial copper radiative cooling crown
    directly above the parking spot.
    """
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("FieldChargePylon_VarA", pivot_u=(0.0, 0.0, 0.0))
    mb = r1kit.MeshBuilder("PylonA_Body", pivot_u=(0.0, 0.0, 0.0))

    deck_h = 0.14
    _add_shared_contact_pad(mb, pad_center_z=0.18, pad_width=3.24, pad_depth=2.86, deck_h=deck_h)

    # Rear bollard plinth extension at z = -1.16 m
    bz = -1.16
    mb.add_bevel_box(
        center_u=(0.0, deck_h + 0.04, bz),
        size_u=(1.12, 0.08, 0.72),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=2,
    )

    # Rounded-arch R1_CreamEnamel transformer bollard (y = 0.18 .. 1.32 m)
    bollard_w = 0.86
    bollard_h = 1.14
    bollard_d = 0.52
    mb.add_arch_housing(
        base_center_u=(0.0, deck_h + 0.06, bz),
        width_x=bollard_w,
        height_y=bollard_h,
        depth_z=bollard_d,
        arch_radius=0.22,
        mat="R1_CreamEnamel",
        bevel=0.022,
        arch_segments=10,
        arch_axis_u="X",
    )
    # Warm R1_OrangeStripe beltline band around the bollard
    mb.add_bevel_box(
        center_u=(0.0, 0.76, bz),
        size_u=(bollard_w + 0.022, 0.11, bollard_d + 0.022),
        mat="R1_OrangeStripe",
        bevel=0.008,
        segments=1,
    )
    # Riveted R1_SagePatch field service plate on right flank + rear
    mb.add_bevel_box(
        center_u=(bollard_w * 0.5 + 0.012, 0.54, bz),
        size_u=(0.024, 0.28, 0.32),
        mat="R1_SagePatch",
        bevel=0.006,
        segments=1,
    )
    # 4-bar vertical R1_LampAmber charge-state totem facing the pad (+Z face of bollard)
    front_z = bz + bollard_d * 0.5 + 0.014
    mb.add_bevel_box(
        center_u=(0.0, 0.82, front_z),
        size_u=(0.30, 0.52, 0.028),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    for bar_i in range(4):
        by = 0.63 + bar_i * 0.115
        mb.add_bevel_box(
            center_u=(0.0, by, front_z + 0.012),
            size_u=(0.22, 0.068, 0.020),
            mat="R1_LampAmber",
            bevel=0.005,
            segments=1,
        )

    # Heavy copper bus conduits running from bollard base into pad contact shoes
    for sx in (-1.0, 1.0):
        mb.add_tube_path(
            points_u=[
                (sx * 0.22, 0.34, bz + 0.24),
                (sx * 0.20, 0.22, bz + 0.42),
                (sx * 0.19, deck_h + 0.04, -0.42),
            ],
            radius=0.028,
            mat="R1_Copper",
            segments=12,
        )

    # Rear radiative cooling fin bank on -Z face of bollard (vacuum heat rejection)
    _add_radiative_fin_bank(
        mb,
        center_u=(0.0, 0.56, bz - bollard_d * 0.5 - 0.10),
        fin_count=6,
        span_x=0.52,
        fin_w=0.016,
        fin_h=0.36,
        fin_d=0.18,
    )
    # Ceramic stand-off insulators on bollard shoulders
    _add_ceramic_insulators(
        mb,
        positions_u=[(-0.30, 1.32, bz), (0.30, 1.32, bz)],
        height=0.15,
        radius=0.038,
    )

    # Telescoping twin-chord lattice gooseneck mast rising from y = 1.32 m to y = 2.58 m
    # and arching forward to suspend the warm amber lantern at z = -0.32 m
    for sx in (-1.0, 1.0):
        mx = sx * 0.15
        mb.add_tube_path(
            points_u=[
                (mx, 1.30, bz),
                (mx, 1.85, bz + 0.04),
                (mx, 2.28, bz + 0.22),
                (mx, 2.50, bz + 0.52),
                (mx, 2.54, -0.32),
            ],
            radius=0.032,
            mat="R1_GreyMetal",
            segments=12,
        )
    # Cross-rungs & diagonal lattice braces along the riser (AntennaMast family cue)
    for ry, rz in ((1.46, bz + 0.01), (1.70, bz + 0.03), (1.96, bz + 0.08), (2.22, bz + 0.19)):
        mb.add_strut((-0.15, ry, rz), (0.15, ry, rz), width=0.028, thickness=0.028, mat="R1_BareSteel")
    mb.add_strut((-0.15, 1.46, bz + 0.01), (0.15, 1.70, bz + 0.03), width=0.020, thickness=0.020, mat="R1_BareSteel")
    mb.add_strut((0.15, 1.70, bz + 0.03), (-0.15, 1.96, bz + 0.08), width=0.020, thickness=0.020, mat="R1_BareSteel")
    mb.add_strut((-0.15, 1.96, bz + 0.08), (0.15, 2.22, bz + 0.19), width=0.020, thickness=0.020, mat="R1_BareSteel")

    # Overhead lantern head at (0.0, 2.48, -0.32): cream enamel hood + warm amber beacon drum + radiative fin crown
    lz = -0.32
    mb.add_cylinder(
        center_u=(0.0, 2.52, lz),
        radius=0.28,
        radius_top=0.20,
        length=0.11,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=24,
        bevel=0.012,
    )
    mb.add_cylinder(
        center_u=(0.0, 2.45, lz),
        radius=0.29,
        length=0.032,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=24,
        bevel=0.005,
    )
    # Glowing R1_LampAmber lantern drum + downward flood lens ("charge here" beacon)
    mb.add_cylinder(
        center_u=(0.0, 2.34, lz),
        radius=0.21,
        radius_top=0.24,
        length=0.18,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=24,
        bevel=0.014,
    )
    # Protective bare-steel guard cage ribs around lantern
    for ang_deg in (0, 90, 180, 270):
        ang = math.radians(ang_deg)
        gx = 0.23 * math.sin(ang)
        gz = lz + 0.23 * math.cos(ang)
        mb.add_strut((gx, 2.44, gz), (gx * 0.75, 2.22, lz + (gz - lz) * 0.75), width=0.016, thickness=0.016, mat="R1_BareSteel")

    obj = mb.build_object(parent=root)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    r1kit.bake_vertex_colors(root)
    r1kit.ensure_materials(use_vertex_color_shading=True)
    sync_all_vertex_color_attributes()
    return root


def build_pylon_variant_b() -> bpy.types.Object:
    """Variant B: Twin-Column Portal Arch & Overhead Charge Beam (2.42 m tall).

    Flanks the drive-on contact pad with twin canted R1_CreamEnamel & R1_GreyMetal side
    columns (x = +/-1.14 m) joined at y = 2.22..2.42 m by an overhead cross-beam carrying
    a wide downward-shining R1_LampAmber light bar, ceramic insulators, and a top bank of
    vertical R1_Copper radiative cooling plates.
    """
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("FieldChargePylon_VarB", pivot_u=(0.0, 0.0, 0.0))
    mb = r1kit.MeshBuilder("PylonB_Body", pivot_u=(0.0, 0.0, 0.0))

    deck_h = 0.14
    _add_shared_contact_pad(mb, pad_center_z=0.10, pad_width=3.36, pad_depth=2.84, deck_h=deck_h)

    pz = -0.36
    for sx in (-1.0, 1.0):
        px = sx * 1.14
        # Base steel footing on deck flank
        mb.add_bevel_box(
            center_u=(px, deck_h + 0.05, pz),
            size_u=(0.38, 0.10, 0.56),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=2,
        )
        # Lower R1_CreamEnamel rounded transformer pier (y = 0.20 .. 1.38 m)
        mb.add_arch_housing(
            base_center_u=(px, deck_h + 0.08, pz),
            width_x=0.32,
            height_y=1.18,
            depth_z=0.46,
            arch_radius=0.14,
            mat="R1_CreamEnamel",
            bevel=0.018,
            arch_segments=8,
            arch_axis_u="Z",
        )
        # Warm R1_OrangeStripe beltline band around each pier
        mb.add_bevel_box(
            center_u=(px, 0.82, pz),
            size_u=(0.34, 0.10, 0.48),
            mat="R1_OrangeStripe",
            bevel=0.006,
            segments=1,
        )
        # Riveted R1_SagePatch inspection panel on outer face
        mb.add_bevel_box(
            center_u=(px + sx * 0.165, 0.54, pz),
            size_u=(0.022, 0.28, 0.28),
            mat="R1_SagePatch",
            bevel=0.005,
            segments=1,
        )
        # 3-bar vertical R1_LampAmber charge meter on front (+Z) face of each pier
        for bar_i in range(3):
            mb.add_bevel_box(
                center_u=(px, 0.52 + bar_i * 0.11, pz + 0.238),
                size_u=(0.16, 0.062, 0.022),
                mat="R1_LampAmber",
                bevel=0.005,
                segments=1,
            )
        # Upper canted R1_GreyMetal lattice legs rising from pier top (y = 1.38 m) to portal beam (y = 2.24 m)
        for sz in (-0.12, 0.12):
            mb.add_strut(
                (px, 1.34, pz + sz),
                (sx * 0.94, 2.24, pz + sz * 0.8),
                width=0.052,
                thickness=0.052,
                mat="R1_GreyMetal",
            )
        mb.add_strut(
            (px, 1.52, pz - 0.12),
            (sx * 1.00, 2.02, pz + 0.10),
            width=0.028,
            thickness=0.028,
            mat="R1_BareSteel",
        )
        # Copper power bus pipe running down inner pier face to pad shoes
        mb.add_tube_path(
            points_u=[
                (px - sx * 0.16, 0.44, pz),
                (sx * 0.62, 0.20, pz + 0.12),
                (sx * 0.22, deck_h + 0.04, pz + 0.24),
            ],
            radius=0.026,
            mat="R1_Copper",
            segments=12,
        )

    # Overhead portal cross-beam (y = 2.24 m) in R1_CreamEnamel + downward R1_LampAmber light bar
    mb.add_bevel_box(
        center_u=(0.0, 2.26, pz),
        size_u=(2.12, 0.22, 0.34),
        mat="R1_CreamEnamel",
        bevel=0.024,
        segments=2,
    )
    mb.add_bevel_box(
        center_u=(0.0, 2.13, pz),
        size_u=(1.48, 0.075, 0.22),
        mat="R1_LampAmber",
        bevel=0.012,
        segments=2,
    )
    # Top vacuum radiative cooling fin bank + ceramic insulators (total height = 2.44 m)
    _add_radiative_fin_bank(
        mb,
        center_u=(0.0, 2.40, pz),
        fin_count=7,
        span_x=0.86,
        fin_w=0.016,
        fin_h=0.18,
        fin_d=0.24,
    )
    _add_ceramic_insulators(
        mb,
        positions_u=[(-0.68, 2.36, pz), (0.68, 2.36, pz)],
        height=0.14,
        radius=0.036,
    )

    obj = mb.build_object(parent=root)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    r1kit.bake_vertex_colors(root)
    r1kit.ensure_materials(use_vertex_color_shading=True)
    sync_all_vertex_color_attributes()
    return root


def build_pylon_variant_c() -> bpy.types.Object:
    """Variant C: Telescoping Tripod Beacon Mast & Hinged Contact Apron (2.86 m tall).

    Directly shares the tripod + cylindrical drum + open-lattice mast grammar of AntennaMast
    and PowerPylon: a 3-legged splayed tripod supporting a cylindrical R1_CreamEnamel
    transformer drum (orange stripe, sage patch, 4-bar amber charge totem, side copper
    cable reels) topped by a 2.86 m telescoping lattice mast with a 360-deg warm-amber
    beacon lantern, paired with a fold-out contact apron extending forward (+Z).
    """
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("FieldChargePylon_VarC", pivot_u=(0.0, 0.0, 0.0))
    mb = r1kit.MeshBuilder("PylonC_Body", pivot_u=(0.0, 0.0, 0.0))

    deck_h = 0.14
    _add_shared_contact_pad(mb, pad_center_z=0.26, pad_width=3.16, pad_depth=2.76, deck_h=deck_h)

    # Rear tripod + cylindrical drum centered at z = -1.28 m
    mz = -1.28
    drum_r = 0.42
    # 3 splayed tripod legs (two rear-side outriggers + one front leg tying into pad hinge)
    tripod_angles_deg = (0.0, 128.0, -128.0)
    for ang_deg in tripod_angles_deg:
        ang = math.radians(ang_deg)
        fx = 0.88 * math.sin(ang)
        fz = mz + 0.88 * math.cos(ang)
        mb.add_strut(
            (0.0, 0.34, mz),
            (fx, 0.08, fz),
            width=0.075,
            thickness=0.065,
            mat="R1_GreyMetal",
        )
        mb.add_cylinder(
            center_u=(fx, 0.04, fz),
            radius=0.15,
            length=0.08,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=12,
            bevel=0.010,
        )

    # Cylindrical R1_CreamEnamel transformer drum (y = 0.26 .. 1.16 m)
    mb.add_cylinder(
        center_u=(0.0, 0.71, mz),
        radius=drum_r,
        length=0.90,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=28,
        bevel=0.024,
        bevel_segments=2,
    )
    # Warm R1_OrangeStripe belly band around drum
    mb.add_cylinder(
        center_u=(0.0, 0.76, mz),
        radius=drum_r + 0.010,
        length=0.11,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=28,
        bevel=0.005,
    )
    # Curved R1_SagePatch service plate on right side of drum
    mb.add_bevel_box(
        center_u=(drum_r + 0.008, 0.56, mz),
        size_u=(0.024, 0.28, 0.28),
        mat="R1_SagePatch",
        bevel=0.006,
        segments=1,
    )
    # Side R1_Copper umbilical cable spools on left/right flanks
    for sx in (-1.0, 1.0):
        mb.add_cylinder(
            center_u=(sx * (drum_r + 0.05), 0.54, mz - 0.06),
            radius=0.13,
            length=0.08,
            mat="R1_Copper",
            axis_u="X",
            segments=18,
            bevel=0.008,
        )

    # 4-bar vertical R1_LampAmber charge-state totem on front (+Z) of drum facing 07
    mb.add_bevel_box(
        center_u=(0.0, 0.74, mz + drum_r + 0.016),
        size_u=(0.28, 0.48, 0.030),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    for bar_i in range(4):
        mb.add_bevel_box(
            center_u=(0.0, 0.56 + bar_i * 0.108, mz + drum_r + 0.028),
            size_u=(0.20, 0.064, 0.020),
            mat="R1_LampAmber",
            bevel=0.005,
            segments=1,
        )

    # Telescoping 3-chord open-lattice mast rising from y = 1.16 m to y = 2.52 m
    chord_r = 0.16
    for c_idx in range(3):
        c_ang = math.radians(c_idx * 120.0)
        cx0 = chord_r * math.sin(c_ang)
        cz0 = mz + chord_r * math.cos(c_ang)
        cx1 = (chord_r * 0.72) * math.sin(c_ang)
        cz1 = mz + (chord_r * 0.72) * math.cos(c_ang)
        mb.add_pipe((cx0, 1.16, cz0), (cx1, 2.52, cz1), radius=0.024, mat="R1_GreyMetal", segments=10)

    # Horizontal triangular collar rings along the mast
    for ry, r_scale in ((1.38, 0.94), (1.72, 0.86), (2.06, 0.79), (2.40, 0.73)):
        mb.add_cylinder(
            center_u=(0.0, ry, mz),
            radius=chord_r * r_scale + 0.02,
            length=0.036,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=18,
            bevel=0.005,
        )

    # Radial vacuum radiative cooling fins around upper mast neck (y = 2.18 m)
    for fin_ang_deg in (45, 135, 225, 315):
        fa = math.radians(fin_ang_deg)
        mb.add_bevel_box(
            center_u=(0.22 * math.sin(fa), 2.22, mz + 0.22 * math.cos(fa)),
            size_u=(0.018, 0.26, 0.18),
            mat="R1_Copper",
            bevel=0.004,
            segments=1,
            rot_deg_u=(0.0, fin_ang_deg, 0.0),
        )
    _add_ceramic_insulators(
        mb,
        positions_u=[(-0.24, 1.16, mz), (0.24, 1.16, mz)],
        height=0.14,
        radius=0.036,
    )

    # Masthead 360-deg warm-amber beacon lantern + cream cap dome (total height = 2.86 m)
    mb.add_cylinder(
        center_u=(0.0, 2.66, mz),
        radius=0.17,
        length=0.24,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=24,
        bevel=0.012,
    )
    mb.add_cylinder(
        center_u=(0.0, 2.81, mz),
        radius=0.20,
        radius_top=0.11,
        length=0.09,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=24,
        bevel=0.010,
    )

    obj = mb.build_object(parent=root)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    r1kit.bake_vertex_colors(root)
    r1kit.ensure_materials(use_vertex_color_shading=True)
    sync_all_vertex_color_attributes()
    return root


# ---------------------------------------------------------------------------
# Day C Lighting, Lunar Ground, & Camera Setup
# ---------------------------------------------------------------------------


def setup_day_c_environment(
    center_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    ground_size: float = 28.0,
    ground_y_u: float = 0.0,
) -> dict[str, bpy.types.Object]:
    """Configure Day C lighting (low golden lunar sun + cool earthshine sky fill + warm rim + lunar ground)."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"

    if hasattr(scene, "eevee"):
        if hasattr(scene.eevee, "taa_render_samples"):
            scene.eevee.taa_render_samples = 64
        if hasattr(scene.eevee, "use_raytracing"):
            scene.eevee.use_raytracing = True

    # World shader: dark lunar sky with subtle horizon glow
    world = bpy.data.worlds.get("DayC_World") or bpy.data.worlds.new("DayC_World")
    scene.world = world
    world.use_nodes = True
    w_nt = world.node_tree
    w_nt.nodes.clear()
    w_out = w_nt.nodes.new("ShaderNodeOutputWorld")
    w_bg = w_nt.nodes.new("ShaderNodeBackground")
    w_bg.inputs["Color"].default_value = (0.016, 0.022, 0.042, 1.0)
    w_bg.inputs["Strength"].default_value = 0.85
    w_nt.links.new(w_bg.outputs["Background"], w_out.inputs["Surface"])

    # Lunar basalt regolith ground plane with gentle distant mare rim
    cx, _, cz = center_u
    bpy.ops.mesh.primitive_grid_add(
        x_subdivisions=48,
        y_subdivisions=48,
        size=ground_size,
        location=r1kit.u_pos(cx, ground_y_u, cz),
    )
    ground = bpy.context.active_object
    ground.name = "_DayC_Ground"
    for v in ground.data.vertices:
        wx = v.co.x
        wy = v.co.y
        r_dist = math.hypot(wx, wy)
        if r_dist > 3.2:
            t = (r_dist - 3.2) / (ground_size * 0.5)
            v.co.z += (t ** 1.7) * 0.45 + 0.025 * math.sin(wx * 0.9) * math.cos(wy * 0.9)
    for poly in ground.data.polygons:
        poly.use_smooth = True
    ground.data.update()

    gmat = bpy.data.materials.new("_DayC_GroundMat")
    gmat.use_nodes = True
    gbsdf = next(n for n in gmat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    gbsdf.inputs["Base Color"].default_value = (0.14, 0.145, 0.165, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.92
    ground.data.materials.append(gmat)

    # Day C Key Sun (low warm golden sun, 18 deg elevation)
    sun_data = bpy.data.lights.new("DayC_GoldenSun", type="SUN")
    sun_data.energy = 4.6
    sun_data.color = (1.0, 0.84, 0.62)
    sun_data.angle = math.radians(2.2)
    sun_obj = bpy.data.objects.new("DayC_GoldenSun", sun_data)
    scene.collection.objects.link(sun_obj)

    # Day C Cool Earthshine Fill
    fill_data = bpy.data.lights.new("DayC_CoolFill", type="SUN")
    fill_data.energy = 1.35
    fill_data.color = (0.58, 0.74, 0.98)
    fill_data.angle = math.radians(28.0)
    fill_obj = bpy.data.objects.new("DayC_CoolFill", fill_data)
    scene.collection.objects.link(fill_obj)

    # Warm Rim Light
    rim_data = bpy.data.lights.new("DayC_Rim", type="SUN")
    rim_data.energy = 1.45
    rim_data.color = (1.0, 0.88, 0.70)
    rim_data.angle = math.radians(8.0)
    rim_obj = bpy.data.objects.new("DayC_Rim", rim_data)
    scene.collection.objects.link(rim_obj)

    # Camera
    cam_data = bpy.data.cameras.new("DayC_Cam")
    cam_obj = bpy.data.objects.new("DayC_Cam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    return {
        "ground": ground,
        "sun": sun_obj,
        "fill": fill_obj,
        "rim": rim_obj,
        "camera": cam_obj,
    }


def aim_camera_and_day_c_lights(
    env: dict[str, bpy.types.Object],
    cam_pos_u: tuple[float, float, float],
    target_u: tuple[float, float, float],
    vfov_deg: float = 52.0,
) -> None:
    """Position camera in Unity coordinates with exact vertical FOV and orient Day C sun/fill relative to view."""
    cam_obj = env["camera"]
    pos_b = r1kit.u_pos(*cam_pos_u)
    tgt_b = r1kit.u_pos(*target_u)
    cam_obj.location = pos_b
    view_dir_b = (tgt_b - pos_b).normalized()
    cam_obj.rotation_euler = view_dir_b.to_track_quat("-Z", "Y").to_euler("XYZ")

    cam_data = cam_obj.data
    cam_data.type = "PERSP"
    cam_data.sensor_fit = "VERTICAL"
    cam_data.sensor_height = 24.0
    cam_data.lens = 12.0 / math.tan(math.radians(vfov_deg * 0.5))

    # Orient Day C low golden sun ~48 deg off the camera azimuth at 19 deg elevation so every angle
    # (Chase cam, Front 3/4, Rear 3/4) receives consistent Day C raking golden key + cool earthshine fill
    cam_az_b = math.atan2(pos_b.y - tgt_b.y, pos_b.x - tgt_b.x)
    sun_az_b = cam_az_b - math.radians(48.0)
    sun_el = math.radians(19.0)
    sun_from_b = Vector((math.cos(sun_el) * math.cos(sun_az_b), math.cos(sun_el) * math.sin(sun_az_b), math.sin(sun_el)))
    env["sun"].rotation_euler = (-sun_from_b).to_track_quat("-Z", "Y").to_euler("XYZ")

    fill_az_b = cam_az_b + math.radians(62.0)
    fill_el = math.radians(38.0)
    fill_from_b = Vector((math.cos(fill_el) * math.cos(fill_az_b), math.cos(fill_el) * math.sin(fill_az_b), math.sin(fill_el)))
    env["fill"].rotation_euler = (-fill_from_b).to_track_quat("-Z", "Y").to_euler("XYZ")

    rim_az_b = cam_az_b + math.radians(162.0)
    rim_el = math.radians(24.0)
    rim_from_b = Vector((math.cos(rim_el) * math.cos(rim_az_b), math.cos(rim_el) * math.sin(rim_az_b), math.sin(rim_el)))
    env["rim"].rotation_euler = (-rim_from_b).to_track_quat("-Z", "Y").to_euler("XYZ")


def render_image(filepath: Path, res_x: int = 960, res_y: int = 720, transparent: bool = False) -> Path:
    filepath = Path(filepath).resolve()
    filepath.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = transparent
    scene.render.use_freestyle = False
    scene.render.filepath = str(filepath)
    bpy.ops.render.render(write_still=True)
    return filepath


# ---------------------------------------------------------------------------
# Camera Presets for Rover 07 & Field Charge Pylon
# ---------------------------------------------------------------------------

# Exact 7.5 m chase cam (16 deg pitch, 52 deg vertical FOV)
CHASE_DIST = 7.5
CHASE_PITCH_RAD = math.radians(16.0)
CHASE_TARGET_U = (0.0, 1.02, 0.0)
CHASE_CAM_POS_U = (
    0.0,
    CHASE_TARGET_U[1] + CHASE_DIST * math.sin(CHASE_PITCH_RAD),
    CHASE_TARGET_U[2] - CHASE_DIST * math.cos(CHASE_PITCH_RAD),
)

ROVER_VIEWS = {
    "chase": (CHASE_CAM_POS_U, CHASE_TARGET_U, 52.0),
    "front34": ((-2.15, 1.62, 2.38), (0.0, 0.96, 0.04), 35.0),
    "rear34": ((-2.10, 1.68, -2.32), (0.0, 0.98, -0.06), 35.0),
}

# For Field Charge Pylon (2.4-2.86 m tall, with Rover 07 parked at z = +0.55 m facing -Z into the pylon)
PYLON_VIEWS = {
    # 7.5 m chase cam behind Rover 07 (at z = +0.55 m facing -Z, so camera is at z = +0.55 + 7.21 = +7.76 m looking -Z)
    "chase": (
        (0.0, 1.15 + CHASE_DIST * math.sin(CHASE_PITCH_RAD), 0.55 + CHASE_DIST * math.cos(CHASE_PITCH_RAD)),
        (0.0, 1.15, -0.25),
        52.0,
    ),
    "front34": ((-3.55, 2.35, 3.85), (0.0, 1.18, -0.10), 38.0),
    "rear34": ((3.45, 2.40, -3.95), (0.0, 1.22, -0.25), 38.0),
}


# ---------------------------------------------------------------------------
# Render Pass 1: Rover 07 Cell Pack III Variants & Silhouette Analysis
# ---------------------------------------------------------------------------


def set_hierarchy_visibility(root: bpy.types.Object, visible: bool) -> None:
    for obj in r1kit.collect_hierarchy(root):
        obj.hide_render = not visible
        obj.hide_viewport = not visible


def compute_silhouette_stats(mask_path: Path) -> dict[str, float]:
    """Compute alpha > 0.15 pixel count and bounding box from a transparent chase-cam render."""
    img = Image.open(mask_path).convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3] > 38
    px_count = int(np.sum(alpha))
    ys, xs = np.where(alpha)
    if len(xs) == 0:
        return {"pixels": 0, "x_min": 0, "x_max": 0, "y_min": 0, "y_max": 0}
    return {
        "pixels": px_count,
        "x_min": int(xs.min()),
        "x_max": int(xs.max()),
        "y_min": int(ys.min()),
        "y_max": int(ys.max()),
        "width_px": int(xs.max() - xs.min() + 1),
        "height_px": int(ys.max() - ys.min() + 1),
    }


def make_silhouette_diff_badge(
    v1_mask_path: Path,
    var_mask_path: Path,
    out_path: Path,
    crop_box: tuple[int, int, int, int] = (290, 220, 670, 540),
) -> Path:
    """Create a zoomed 7.5 m chase-cam silhouette comparison badge (V1 in slate blue, Cell Pack III addition in warm amber)."""
    im_v1 = np.array(Image.open(v1_mask_path).convert("RGBA"))
    im_var = np.array(Image.open(var_mask_path).convert("RGBA"))
    x0, y0, x1, y1 = crop_box
    a_v1 = im_v1[y0:y1, x0:x1, 3] > 38
    a_var = im_var[y0:y1, x0:x1, 3] > 38

    h, w = a_v1.shape
    canvas = np.zeros((h, w, 4), dtype=np.uint8)
    canvas[:, :, 0] = 18
    canvas[:, :, 1] = 23
    canvas[:, :, 2] = 36
    canvas[:, :, 3] = 255

    # Subtle grid lines
    canvas[::32, :, :3] = 28
    canvas[:, ::32, :3] = 28

    # V1 baseline silhouette in cool slate
    canvas[a_v1, 0] = 88
    canvas[a_v1, 1] = 104
    canvas[a_v1, 2] = 132

    # Added Cell Pack III silhouette in glowing warm amber (#FFB547)
    added = a_var & (~a_v1)
    canvas[added, 0] = 255
    canvas[added, 1] = 181
    canvas[added, 2] = 71

    # Draw outer tyre stance vertical dashed guide lines (exact outer x of V1/07 tyres in the crop)
    ys, xs = np.where(a_v1)
    if len(xs) > 0:
        lx, rx = int(xs.min()), int(xs.max())
        for y in range(12, h - 12, 6):
            canvas[y : y + 3, max(0, lx - 1) : min(w, lx + 1), :3] = (110, 195, 135)
            canvas[y : y + 3, max(0, rx - 1) : min(w, rx + 1), :3] = (110, 195, 135)

    badge_img = Image.fromarray(canvas, mode="RGBA")
    badge_img.save(out_path)
    return out_path


def render_all_rover_views() -> dict[str, dict]:
    """Build Rover 07 + V1 + Cell Pack III (Variants A, B, C) and render all Day C views + chase-cam silhouette masks."""
    r1kit.reset_scene()
    parts = build_base_rover_07(include_v1_upgrades=True)
    rover_root = parts["rover_root"]

    var_a = build_cellpack3_variant_a()
    var_b = build_cellpack3_variant_b()
    var_c = build_cellpack3_variant_c()
    for v_obj in (var_a, var_b, var_c):
        v_obj.parent = rover_root

    finalize_render_materials()
    env = setup_day_c_environment(center_u=(0.0, 0.0, 0.0), ground_size=26.0, ground_y_u=0.0)
    bpy.context.view_layer.update()

    configs = {
        "bare": {"v1": False, "var": None},
        "v1": {"v1": True, "var": None},
        "var_a": {"v1": True, "var": var_a},
        "var_b": {"v1": True, "var": var_b},
        "var_c": {"v1": True, "var": var_c},
    }

    results: dict[str, dict] = {}
    for cfg_name, cfg in configs.items():
        # Toggle V1 upgrades
        for k in ("headlamp", "pod_r", "pod_l"):
            set_hierarchy_visibility(parts[k], cfg["v1"])
        # Toggle Cell Pack III variants
        for v_obj in (var_a, var_b, var_c):
            set_hierarchy_visibility(v_obj, v_obj == cfg["var"])

        bpy.context.view_layer.update()
        results[cfg_name] = {"views": {}, "stats": {}}

        for view_name, (cam_pos, tgt_pos, vfov) in ROVER_VIEWS.items():
            aim_camera_and_day_c_lights(env, cam_pos, tgt_pos, vfov_deg=vfov)
            env["ground"].hide_render = False
            out_p = TMP_DIR / f"rover_{cfg_name}_{view_name}.png"
            render_image(out_p, res_x=960, res_y=720, transparent=False)
            results[cfg_name]["views"][view_name] = str(out_p)

        # Render transparent chase-cam silhouette mask for exact 7.5 m silhouette measurement
        aim_camera_and_day_c_lights(env, *ROVER_VIEWS["chase"])
        env["ground"].hide_render = True
        mask_p = TMP_DIR / f"mask_{cfg_name}_chase.png"
        render_image(mask_p, res_x=960, res_y=720, transparent=True)
        env["ground"].hide_render = False
        results[cfg_name]["mask"] = str(mask_p)

        # Measure world X half-span of the active Cell Pack III variant to verify inside +/-0.738 m tyre stance
        if cfg["var"] is not None:
            max_abs_x = 0.0
            max_y = 0.0
            for o in r1kit.collect_hierarchy(cfg["var"]):
                if o.type == "MESH":
                    for v in o.bound_box:
                        wv = o.matrix_world @ Vector(v)
                        max_abs_x = max(max_abs_x, abs(wv.x))
                        max_y = max(max_y, wv.z)  # Blender Z is Unity Y
            results[cfg_name]["stats"]["max_abs_x_m"] = round(max_abs_x, 3)
            results[cfg_name]["stats"]["max_y_m"] = round(max_y, 3)

    return results


# ---------------------------------------------------------------------------
# Render Pass 2: Field Charge Pylon Variants (with Rover 07 for Scale)
# ---------------------------------------------------------------------------


def render_all_pylon_views() -> dict[str, dict]:
    """Build all 3 Field Charge Pylon variants alongside Rover 07 and render Chase, Front 3/4, Rear 3/4 in Day C."""
    r1kit.reset_scene()
    parts = build_base_rover_07(include_v1_upgrades=True)
    rover_root = parts["rover_root"]
    var_a_pack = build_cellpack3_variant_a()
    var_a_pack.parent = rover_root

    # Park Rover 07 on the front apron of the Field Charge Pylon (z = +0.55 m, y = 0.14 m deck height, facing -Z toward pylon)
    rover_root.location = r1kit.u_pos(0.0, 0.14, 0.55)
    rover_root.rotation_euler = (0.0, 0.0, math.pi)

    pylon_a = build_pylon_variant_a()
    pylon_b = build_pylon_variant_b()
    pylon_c = build_pylon_variant_c()
    pylons = {
        "pylon_a": pylon_a,
        "pylon_b": pylon_b,
        "pylon_c": pylon_c,
    }

    finalize_render_materials()

    env = setup_day_c_environment(center_u=(0.0, 0.0, 0.0), ground_size=34.0, ground_y_u=0.0)

    # Warm amber point light above the pad to cast the pylon's "charge here" amber pool onto the deck & 07
    pt_data = bpy.data.lights.new("PylonAmberPool", type="POINT")
    pt_data.color = (1.0, 0.68, 0.25)
    pt_data.energy = 38.0
    pt_data.shadow_soft_size = 0.35
    pt_obj = bpy.data.objects.new("PylonAmberPool", pt_data)
    pt_obj.location = r1kit.u_pos(0.0, 2.15, -0.35)
    bpy.context.scene.collection.objects.link(pt_obj)

    results: dict[str, dict] = {}
    for p_key, p_obj in pylons.items():
        for other_k, other_obj in pylons.items():
            set_hierarchy_visibility(other_obj, other_k == p_key)
        bpy.context.view_layer.update()

        # Measure bounding box height & pad width
        max_y = 0.0
        max_x = 0.0
        for o in r1kit.collect_hierarchy(p_obj):
            if o.type == "MESH":
                for v in o.bound_box:
                    wv = o.matrix_world @ Vector(v)
                    max_y = max(max_y, wv.z)
                    max_x = max(max_x, abs(wv.x) * 2.0)

        results[p_key] = {
            "views": {},
            "height_m": round(max_y, 2),
            "width_m": round(max_x, 2),
            "tris": r1kit.count_triangles(p_obj),
        }

        for view_name, (cam_pos, tgt_pos, vfov) in PYLON_VIEWS.items():
            aim_camera_and_day_c_lights(env, cam_pos, tgt_pos, vfov_deg=vfov)
            out_p = TMP_DIR / f"{p_key}_{view_name}.png"
            render_image(out_p, res_x=960, res_y=720, transparent=False)
            results[p_key]["views"][view_name] = str(out_p)

    return results


# ---------------------------------------------------------------------------
# Sheet Compositing Helpers (PIL)
# ---------------------------------------------------------------------------


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (
        ["C:/Windows/Fonts/consolab.ttf", "C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf"]
        if bold
        else ["C:/Windows/Fonts/consola.ttf", "C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"]
    )
    for c in candidates:
        if Path(c).exists():
            try:
                return ImageFont.truetype(c, size=size)
            except Exception:
                pass
    return ImageFont.load_default()


def crop_and_resize(img_path: Path, target_w: int, target_h: int, zoom: float = 1.0, y_bias: float = 0.0) -> Image.Image:
    im = Image.open(img_path).convert("RGB")
    w, h = im.size
    target_aspect = target_w / target_h
    crop_h = h / zoom
    crop_w = crop_h * target_aspect
    if crop_w > w:
        crop_w = w / zoom
        crop_h = crop_w / target_aspect
    cx = w * 0.5
    cy = h * (0.5 + y_bias)
    x0 = max(0, int(round(cx - crop_w * 0.5)))
    y0 = max(0, int(round(cy - crop_h * 0.5)))
    x1 = min(w, int(round(x0 + crop_w)))
    y1 = min(h, int(round(y0 + crop_h)))
    cropped = im.crop((x0, y0, x1, y1))
    return cropped.resize((target_w, target_h), Image.Resampling.LANCZOS)


def compose_cellpack3_sheet(rover_res: dict[str, dict], out_path: Path) -> None:
    """Compose docs/art/concepts/0.6-upgrades/cellpack3_sheet.png."""
    W, H = 2570, 1780
    sheet = Image.new("RGB", (W, H), (14, 17, 25))
    draw = ImageDraw.Draw(sheet)

    f_title = load_font(28, bold=True)
    f_sub = load_font(16, bold=False)
    f_hdr = load_font(17, bold=True)
    f_body = load_font(13, bold=False)
    f_small = load_font(13, bold=False)

    # Header bar
    draw.rectangle([0, 0, W, 94], fill=(22, 27, 40))
    draw.line([(0, 94), (W, 94)], fill=(255, 181, 71), width=3)
    draw.text(
        (28, 18),
        "0.6 UPGRADE CONCEPT SHEET — CELL PACK III  (Back Socket · 8 Cells · ~370 m Range)",
        fill=(242, 227, 198),
        font=f_title,
    )
    draw.text(
        (28, 56),
        "Stacks on top of round-3 V1 flank pods (Kit_CellPod) · R1_CreamEnamel + 1 R1_LampAmber accent · Inside ±0.738 m tyre stance · <= 20% silhouette growth · Day C lighting",
        fill=(180, 192, 214),
        font=f_sub,
    )

    # Column headers
    col_x = [590, 1155, 1720, 2285]
    col_w = [545, 545, 545, 255]
    col_titles = [
        "7.5 m CHASE CAM (16° Pitch · 52° FOV)",
        "FRONT 3/4  (Day C)",
        "REAR 3/4  (Day C)",
        "7.5 m SILHOUETTE DIFF",
    ]
    for cx, cw, ct in zip(col_x, col_w, col_titles):
        draw.rectangle([cx, 108, cx + cw, 146], fill=(26, 32, 48))
        draw.text((cx + 14, 117), ct, fill=(255, 181, 71), font=f_hdr)

    rows = [
        (
            "var_a",
            "VARIANT A (RECOMMENDED)",
            "Twin Piggyback Pill Capsules ('Saddle-Top Pills')",
            True,
            [
                "• Fore-and-aft R1_CreamEnamel pill pods on V1 cell pods",
                "• Echoes V1 Kit_SSR3Headlamp capsule form on shoulders",
                "• Contoured R1_GreyMetal + R1_Rubber saddle collars",
                "• 1 Amber Accent: 3rd vertical bar above V1's 2-bar column",
            ],
        ),
        (
            "var_b",
            "VARIANT B",
            "Stacked Soft Twin Lozenge Blisters + Rear Bridge",
            False,
            [
                "• Second-storey soft lozenge blisters on V1 pod footprint",
                "• Recessed dark rubber waist gasket on V1 pod crown",
                "• Low curved equaliser bridge behind CRT head + capsule",
                "• 1 Amber Accent: Flush upper bar above V1's 2-bar column",
            ],
        ),
        (
            "var_c",
            "VARIANT C",
            "Stacked Twin Domed Vertical Canisters ('Twin Flasks')",
            False,
            [
                "• Contoured grey-metal twin-cup cradle on each V1 cell pod",
                "• 2 plump hemispherical-domed R1_CreamEnamel flasks/side",
                "• Reads literally as +2 visible cell canister pairs (6->8)",
                "• 1 Amber Accent: Warm R1_LampAmber collar ring at base",
            ],
        ),
    ]

    row_h = 515
    img_h = 465
    for idx, (v_key, tag, subtitle, is_rec, bullets) in enumerate(rows):
        ry = 158 + idx * row_h
        border_col = (255, 181, 71) if is_rec else (52, 62, 84)
        bg_col = (24, 30, 44) if is_rec else (19, 24, 35)
        draw.rectangle([20, ry, W - 20, ry + row_h - 14], fill=bg_col, outline=border_col, width=2 if is_rec else 1)

        # Left info card
        lx = 34
        draw.rectangle(
            [lx, ry + 16, lx + 536, ry + 52],
            fill=(255, 181, 71) if is_rec else (42, 52, 72),
        )
        draw.text(
            (lx + 12, ry + 23),
            tag,
            fill=(16, 18, 24) if is_rec else (242, 227, 198),
            font=f_hdr,
        )
        draw.text((lx, ry + 64), subtitle, fill=(242, 227, 198), font=f_hdr)

        by = ry + 100
        for b_line in bullets:
            draw.text((lx, by), b_line, fill=(196, 206, 224), font=f_body)
            by += 26

        # Quantitative metrics box
        st = rover_res[v_key]["stats"]
        v1_st = rover_res["v1"]["stats"]
        draw.rectangle([lx, ry + 224, lx + 536, ry + 482], fill=(14, 18, 27), outline=(64, 78, 104), width=1)
        draw.text((lx + 12, ry + 236), "QUANTITATIVE 7.5 m CHASE-CAM CHECKS", fill=(255, 181, 71), font=f_body)
        metrics_lines = [
            f"Chase-Cam Growth vs V1    : +{st['pct_over_v1']:.1f}%  (limit <= 20%)",
            f"Total Growth vs Bare 07   : +{st['pct_over_bare']:.1f}%  (V1 alone: +{v1_st['pct_over_bare']:.1f}%)",
            f"Outer X Half-Span         : ±{st['max_abs_x_m']:.3f} m",
            f"07 Tyre Outer Stance      : ±0.738 m  (INSIDE by {(0.738 - st['max_abs_x_m']) * 100:.1f} cm)",
            f"Top Crown Height (Unity Y): {st['max_y_m']:.3f} m  (Head top: 1.530 m)",
            "Socket / Stacking         : Back -> stacks on V1 Kit_CellPod",
            "Material Palette          : R1_CreamEnamel + R1_GreyMetal",
            "Amber Accent              : 1 R1_LampAmber charge bar / side",
        ]
        my = ry + 268
        for ml in metrics_lines:
            draw.text((lx + 12, my), ml, fill=(214, 224, 240), font=f_small)
            my += 25

        # Paste 3 camera views
        chase_im = crop_and_resize(rover_res[v_key]["views"]["chase"], col_w[0], img_h, zoom=1.32, y_bias=-0.02)
        front_im = crop_and_resize(rover_res[v_key]["views"]["front34"], col_w[1], img_h, zoom=1.08, y_bias=-0.02)
        rear_im = crop_and_resize(rover_res[v_key]["views"]["rear34"], col_w[2], img_h, zoom=1.08, y_bias=-0.02)

        sheet.paste(chase_im, (col_x[0], ry + 18))
        sheet.paste(front_im, (col_x[1], ry + 18))
        sheet.paste(rear_im, (col_x[2], ry + 18))

        # Paste 7.5 m chase-cam inset zoom + silhouette diff badge in 4th column
        chase_zoom = crop_and_resize(rover_res[v_key]["views"]["chase"], col_w[3], 215, zoom=2.45, y_bias=-0.04)
        sheet.paste(chase_zoom, (col_x[3], ry + 18))
        draw.rectangle([col_x[3], ry + 18, col_x[3] + col_w[3], ry + 44], fill=(14, 18, 28))
        draw.text((col_x[3] + 8, ry + 22), "7.5 m Chase Cam (2.4x Zoom)", fill=(242, 227, 198), font=f_small)

        badge_im = Image.open(rover_res[v_key]["badge"]).convert("RGB").resize((col_w[3], 215), Image.Resampling.NEAREST)
        sheet.paste(badge_im, (col_x[3], ry + 245))
        draw.rectangle([col_x[3], ry + 245, col_x[3] + col_w[3], ry + 271], fill=(14, 18, 28))
        draw.text((col_x[3] + 8, ry + 249), f"Amber = +{st['pct_over_v1']:.1f}% vs V1 | Green = Tyres", fill=(255, 181, 71), font=f_small)

    sheet.save(out_path, format="PNG")


def compose_pylon_sheet(pylon_res: dict[str, dict], out_path: Path) -> None:
    """Compose docs/art/concepts/0.6-upgrades/pylon_sheet.png."""
    W, H = 2430, 1780
    sheet = Image.new("RGB", (W, H), (14, 17, 25))
    draw = ImageDraw.Draw(sheet)

    f_title = load_font(28, bold=True)
    f_sub = load_font(16, bold=False)
    f_hdr = load_font(17, bold=True)
    f_body = load_font(13, bold=False)
    f_small = load_font(13, bold=False)

    # Header bar
    draw.rectangle([0, 0, W, 94], fill=(22, 27, 40))
    draw.line([(0, 94), (W, 94)], fill=(255, 181, 71), width=3)
    draw.text(
        (28, 18),
        "0.6 STRUCTURE CONCEPT SHEET — FIELD CHARGE PYLON  (Forward Charger on Predefined Pad)",
        fill=(242, 227, 198),
        font=f_title,
    )
    draw.text(
        (28, 56),
        "VIS / ILC-R maker family (ChargingPad + AntennaMast / PowerPylon) · 2.5–2.9 m tall · Purpose reads at a glance ('charge here') · Vacuum radiative cooling · Day C with 07",
        fill=(180, 192, 214),
        font=f_sub,
    )

    col_x = [590, 1190, 1790]
    col_w = [580, 580, 580]
    col_titles = [
        "7.5 m CHASE CAM (Approaching / Docked · 16° · 52° FOV)",
        "FRONT 3/4  (Day C · With Rover 07 for Scale)",
        "REAR 3/4  (Day C · Vacuum Radiators & Busbars)",
    ]
    for cx, cw, ct in zip(col_x, col_w, col_titles):
        draw.rectangle([cx, 108, cx + cw, 146], fill=(26, 32, 48))
        draw.text((cx + 14, 117), ct, fill=(255, 181, 71), font=f_hdr)

    rows = [
        (
            "pylon_a",
            "VARIANT A (RECOMMENDED)",
            "Gooseneck Lantern Bollard & Contact Apron",
            True,
            [
                "• Direct field cousin of ChargingPad: rounded R1_CreamEnamel",
                "  rear bollard + R1_OrangeStripe belt + R1_SagePatch plate",
                "• Telescoping open-lattice gooseneck mast cantilevers a caged",
                "  R1_LampAmber lantern directly over copper contact shoes",
                "• 4-bar vertical amber charge totem faces 07's visor on pad",
                "• Vacuum logic: rear R1_Copper radiative fins + insulators",
            ],
        ),
        (
            "pylon_b",
            "VARIANT B",
            "Twin-Column Portal Arch & Overhead Charge Beam",
            False,
            [
                "• Twin canted R1_CreamEnamel + lattice side piers (x = ±1.14 m)",
                "  framing the 3.35 m chamfered drive-on contact apron",
                "• Overhead cross-beam carries a wide downward R1_LampAmber",
                "  light bar framing 07 in a glowing doorway on the mare plain",
                "• Dual 3-bar amber charge meters on both front piers",
                "• Vacuum logic: roof-mounted 7-plate R1_Copper fin bank",
            ],
        ),
        (
            "pylon_c",
            "VARIANT C",
            "Telescoping Tripod Beacon Mast & Contact Apron",
            False,
            [
                "• Shares AntennaMast / PowerPylon grammar: 3-legged tripod",
                "  + cylindrical R1_CreamEnamel drum + 3-chord lattice mast",
                "• 360° masthead R1_LampAmber beacon visible across Long Mare",
                "  + 4-bar charge totem on drum front + fold-out contact apron",
                "• Side R1_Copper umbilical reels & hinged ±0.61 m wheel tracks",
                "• Vacuum logic: radial copper radiative fins & insulators",
            ],
        ),
    ]

    row_h = 515
    img_h = 465
    for idx, (p_key, tag, subtitle, is_rec, bullets) in enumerate(rows):
        ry = 158 + idx * row_h
        border_col = (255, 181, 71) if is_rec else (52, 62, 84)
        bg_col = (24, 30, 44) if is_rec else (19, 24, 35)
        draw.rectangle([20, ry, W - 20, ry + row_h - 14], fill=bg_col, outline=border_col, width=2 if is_rec else 1)

        lx = 34
        draw.rectangle(
            [lx, ry + 16, lx + 536, ry + 52],
            fill=(255, 181, 71) if is_rec else (42, 52, 72),
        )
        draw.text(
            (lx + 12, ry + 23),
            tag,
            fill=(16, 18, 24) if is_rec else (242, 227, 198),
            font=f_hdr,
        )
        draw.text((lx, ry + 64), subtitle, fill=(242, 227, 198), font=f_hdr)

        by = ry + 98
        for b_line in bullets:
            draw.text((lx, by), b_line, fill=(196, 206, 224), font=f_body)
            by += 24

        info = pylon_res[p_key]
        draw.rectangle([lx, ry + 258, lx + 536, ry + 482], fill=(14, 18, 27), outline=(64, 78, 104), width=1)
        draw.text((lx + 12, ry + 270), "SPECIFICATION & MAKER FAMILY CHECKS", fill=(255, 181, 71), font=f_body)
        spec_lines = [
            f"Total Mast / Portal Height : {info['height_m']:.2f} m  (target: 2.0–3.0 m)",
            f"Contact Apron Width        : {info['width_m']:.2f} m  (wheel track: ±0.61 m)",
            "Maker Family               : VIS / ILC-R (ChargingPad + Relay Masts)",
            "Charge Readability         : Overhead amber beacon + 4-bar totem",
            "Contact Mechanism          : Raised R1_Copper belly shoes + ramps",
            "Vacuum Logic               : R1_Copper radiative fins + insulators",
            "Craft Recipe (0.6)         : 3M 2W 1O · 0.4 Charge on predefined pad",
        ]
        sy = ry + 300
        for sl in spec_lines:
            draw.text((lx + 12, sy), sl, fill=(214, 224, 240), font=f_small)
            sy += 24

        chase_im = crop_and_resize(info["views"]["chase"], col_w[0], img_h, zoom=1.10, y_bias=-0.02)
        front_im = crop_and_resize(info["views"]["front34"], col_w[1], img_h, zoom=1.06, y_bias=-0.02)
        rear_im = crop_and_resize(info["views"]["rear34"], col_w[2], img_h, zoom=1.06, y_bias=-0.02)

        sheet.paste(chase_im, (col_x[0], ry + 18))
        sheet.paste(front_im, (col_x[1], ry + 18))
        sheet.paste(rear_im, (col_x[2], ry + 18))

    sheet.save(out_path, format="PNG")


def compose_grown_06_sheet(rover_res: dict[str, dict], out_path: Path) -> None:
    """Compose docs/art/concepts/0.6-upgrades/grown_0.6.png showing fully grown 0.6 Rover 07."""
    W, H = 2280, 1420
    sheet = Image.new("RGB", (W, H), (14, 17, 25))
    draw = ImageDraw.Draw(sheet)

    f_title = load_font(28, bold=True)
    f_sub = load_font(16, bold=False)
    f_hdr = load_font(18, bold=True)
    f_body = load_font(14, bold=False)
    f_small = load_font(13, bold=False)

    # Header bar
    draw.rectangle([0, 0, W, 94], fill=(22, 27, 40))
    draw.line([(0, 94), (W, 94)], fill=(255, 181, 71), width=3)
    draw.text(
        (28, 18),
        "ROVER 07 — 'FULLY GROWN 0.6' CONFIGURATION  (V1 Headlamp + V1 Cell Pods + Cell Pack III Variant A)",
        fill=(242, 227, 198),
        font=f_title,
    )
    draw.text(
        (28, 56),
        "BrowSocket: Kit_SSR3Headlamp · PodSocket_L/R: Kit_CellPod · Back: Kit_CellPack3_VarA (stacking on V1 pods) · DeckSocket: Kit_TapeDeck · Day C lighting",
        fill=(180, 192, 214),
        font=f_sub,
    )

    # Top Hero Row: 3 large views of Fully Grown 0.6 (Chase Cam, Front 3/4, Rear 3/4)
    hero_w = 720
    hero_h = 640
    hero_xs = [30, 780, 1530]
    hero_Specs = [
        ("chase", "7.5 m CHASE CAM  (16° Pitch · 52° FOV — Primary Gameplay View)", 1.30),
        ("front34", "FRONT 3/4 VIEW  (Day C — V1 Brow Pills + Stacked Flank Pods)", 1.08),
        ("rear34", "REAR 3/4 VIEW  (Day C — Saddle-Top Pills + 3-Bar Amber Column)", 1.08),
    ]
    for hx, (v_name, h_title, zoom) in zip(hero_xs, hero_Specs):
        draw.rectangle([hx, 112, hx + hero_w, 152], fill=(26, 32, 48), outline=(255, 181, 71), width=1)
        draw.text((hx + 14, 121), h_title, fill=(255, 181, 71), font=f_hdr)
        im = crop_and_resize(rover_res["var_a"]["views"][v_name], hero_w, hero_h, zoom=zoom, y_bias=-0.02)
        sheet.paste(im, (hx, 154))
        draw.rectangle([hx, 154, hx + hero_w, 154 + hero_h], outline=(64, 78, 104), width=2)

    # Bottom Row: 7.5 m Chase-Cam Growth Progression Strip (Base 07 -> V1 Round-3 -> Fully Grown 0.6 + Silhouette Diff)
    bot_y = 820
    draw.rectangle([30, bot_y, W - 30, H - 24], fill=(20, 25, 37), outline=(64, 78, 104), width=1)
    draw.text(
        (50, bot_y + 16),
        "07 GROWTH PROGRESSION AT 7.5 m CHASE CAM (2.2x Crop) — Small, Cute, Calm, Inside ±0.738 m Tyre Stance",
        fill=(242, 227, 198),
        font=f_hdr,
    )

    st_v1 = rover_res["v1"]["stats"]
    st_a = rover_res["var_a"]["stats"]
    prog_cards = [
        ("bare", "STAGE 0: BASE ROVER 07 (4 Cells · 185 m)", "Baseline 7.5 m Chase Silhouette (0.0%)"),
        ("v1", "STAGE 1: ROUND-3 V1 (Headlamp + V1 Cell Pods · 6 Cells)", f"+{st_v1['pct_over_bare']:.1f}% vs Base · Inside ±0.646 m"),
        ("var_a", "STAGE 2: FULLY GROWN 0.6 (+ Cell Pack III Var A · 8 Cells)", f"+{st_a['pct_over_v1']:.1f}% vs V1 (+{st_a['pct_over_bare']:.1f}% total) · ±{st_a['max_abs_x_m']:.3f} m"),
    ]
    pw, ph = 500, 460
    for idx, (cfg_k, p_title, p_sub) in enumerate(prog_cards):
        px = 50 + idx * 535
        py = bot_y + 58
        im_p = crop_and_resize(rover_res[cfg_k]["views"]["chase"], pw, ph, zoom=2.15, y_bias=-0.03)
        sheet.paste(im_p, (px, py))
        draw.rectangle([px, py, px + pw, py + 58], fill=(14, 18, 28))
        draw.text((px + 12, py + 8), p_title, fill=(255, 181, 71) if cfg_k == "var_a" else (242, 227, 198), font=f_body)
        draw.text((px + 12, py + 32), p_sub, fill=(180, 196, 220), font=f_small)
        draw.rectangle([px, py, px + pw, py + ph], outline=(255, 181, 71) if cfg_k == "var_a" else (58, 70, 92), width=2)

    # Right-hand summary & silhouette diff card
    sx = 1665
    sy = bot_y + 58
    sw = 565
    badge_im = Image.open(rover_res["var_a"]["badge"]).convert("RGB").resize((sw, 265), Image.Resampling.NEAREST)
    sheet.paste(badge_im, (sx, sy))
    draw.rectangle([sx, sy, sx + sw, sy + 34], fill=(14, 18, 28))
    draw.text((sx + 12, sy + 7), "7.5 m Chase-Cam Silhouette Diff (Amber = Cell Pack III Var A)", fill=(255, 181, 71), font=f_body)
    draw.rectangle([sx, sy, sx + sw, sy + 265], outline=(255, 181, 71), width=2)

    summary_lines = [
        "WHY THIS CONFIGURATION WORKS FOR 0.6:",
        "• Visibly fills the upper shoulder notch between CRT head and V1",
        f"  flank pods from the 7.5 m chase cam (+{st_a['pct_over_v1']:.1f}% over V1; +{st_a['pct_over_bare']:.1f}% total).",
        "• Repeating the rounded R1_CreamEnamel capsule form of V1's",
        "  Kit_SSR3Headlamp on top of V1's Kit_CellPod unifies all three",
        "  upgrades into one calm, friendly retro-futurist family.",
        f"• Outer width (±{st_a['max_abs_x_m']:.3f} m) stays {(0.738 - st_a['max_abs_x_m']) * 100:.1f} cm inside 07's ±0.738 m tyres.",
    ]
    ty = sy + 280
    for sl in summary_lines:
        draw.text((sx + 6, ty), sl, fill=(214, 224, 240), font=f_small)
        ty += 24

    sheet.save(out_path, format="PNG")


def compose_all_from_meta(meta_path: Path) -> None:
    meta = json.loads(meta_path.read_text(encoding="utf-8"))
    rover_res = meta["rover"]
    pylon_res = meta["pylon"]

    for cfg_name, cfg_data in rover_res.items():
        st = compute_silhouette_stats(Path(cfg_data["mask"]))
        cfg_data["stats"].update(st)

    bare_px = rover_res["bare"]["stats"]["pixels"]
    v1_px = rover_res["v1"]["stats"]["pixels"]
    rover_res["v1"]["stats"]["pct_over_bare"] = round((v1_px - bare_px) * 100.0 / max(1, bare_px), 2)

    for v_key in ("var_a", "var_b", "var_c"):
        v_px = rover_res[v_key]["stats"]["pixels"]
        rover_res[v_key]["stats"]["pct_over_v1"] = round((v_px - v1_px) * 100.0 / max(1, v1_px), 2)
        rover_res[v_key]["stats"]["pct_over_bare"] = round((v_px - bare_px) * 100.0 / max(1, bare_px), 2)
        badge_p = TMP_DIR / f"badge_{v_key}.png"
        make_silhouette_diff_badge(Path(rover_res["v1"]["mask"]), Path(rover_res[v_key]["mask"]), badge_p)
        rover_res[v_key]["badge"] = str(badge_p)

    cellpack_sheet_path = OUT_DIR / "cellpack3_sheet.png"
    pylon_sheet_path = OUT_DIR / "pylon_sheet.png"
    grown_sheet_path = OUT_DIR / "grown_0.6.png"

    print(f"[0.6-upgrades] Compositing {cellpack_sheet_path.name}...")
    compose_cellpack3_sheet(rover_res, cellpack_sheet_path)

    print(f"[0.6-upgrades] Compositing {pylon_sheet_path.name}...")
    compose_pylon_sheet(pylon_res, pylon_sheet_path)

    print(f"[0.6-upgrades] Compositing {grown_sheet_path.name}...")
    compose_grown_06_sheet(rover_res, grown_sheet_path)

    summary = {
        "rover": {k: v["stats"] for k, v in rover_res.items()},
        "pylon": {
            k: {"height_m": v["height_m"], "width_m": v["width_m"], "tris": v["tris"]}
            for k, v in pylon_res.items()
        },
    }
    print("[0.6-upgrades] METRICS_JSON=" + json.dumps(summary))


def main() -> None:
    meta_path = TMP_DIR / "meta.json"
    if not IN_BLENDER:
        try:
            compose_all_from_meta(meta_path)
        finally:
            if TMP_DIR.exists():
                shutil.rmtree(TMP_DIR, ignore_errors=True)
        return

    TMP_DIR.mkdir(parents=True, exist_ok=True)
    print("[0.6-upgrades] Rendering Rover 07 Cell Pack III variants & chase-cam masks...")
    rover_res = render_all_rover_views()

    print("[0.6-upgrades] Rendering Field Charge Pylon variants with Rover 07 in Day C...")
    pylon_res = render_all_pylon_views()

    meta_path.write_text(
        json.dumps({"rover": rover_res, "pylon": pylon_res}, indent=2),
        encoding="utf-8",
    )
    subprocess.run(["python", str(Path(__file__).resolve()), "--compose-only"], check=True)


if __name__ == "__main__":
    main()
