"""Build script for the R1 VehiclePad structure (`Assets/_Project/Art/R1/Models/Structures/VehiclePad.fbx`)."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

# Single table of dimensions taken from DESIGN.md (§1.2, §2, §3.10) and s_vehicle_pad.png
VEHICLE_PAD_DIMS: dict[str, float] = {
    "footprint_width": 3.40,
    "footprint_depth": 2.80,
    "total_height": 1.85,
    "deck_height": 0.22,
    # Octagonal deck dimensions
    "deck_core_w": 3.04,
    "deck_core_d": 2.40,
    "subframe_h": 0.12,
    # Corner cantilever crane post (back-right -X, -Z corner, matching s_vehicle_pad.png)
    "crane_x": -1.18,
    "crane_z": -0.62,
    "crane_post_w": 0.36,
    "crane_post_d": 0.36,
    "crane_shoulder_y": 1.56,
    "stripe_center_y": 1.16,
    "stripe_height": 0.12,
    "patch_center_y": 0.80,
    # Protruding lower weld control console & Socket_Power_1 on crane post front (+Z) face
    "console_x": -1.18,
    "console_y": 0.48,
    "console_z": -0.38,
    "socket_power_x": -1.36,
    "socket_power_y": 0.16,
    "socket_power_z": -0.36,
    # Rotating CraneBoom, HoistHook, and BoomLamp
    "boom_tip_x": 0.08,
    "boom_tip_y": 1.74,
    "boom_tip_z": 0.08,
    "hook_pivot_y": 1.66,
    "hook_drop_y": 0.20,
    "boom_lamp_x": -0.12,
    "boom_lamp_y": 1.58,
    "boom_lamp_z": -0.04,
    # Hinged front (+Z) roll-off RampLip (0° .. -25°)
    "ramp_pivot_y": 0.12,
    "ramp_pivot_z": 1.24,
    "ramp_width_x": 1.92,
    "ramp_length_z": 0.22,
    "ramp_drop_deg": 25.0,
    # 4 corner bollard lamps
    "bollard_x": 1.36,
    "bollard_z": 0.98,
    "bollard_lamp_y": 0.40,
}


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Heavy octagonal under-deck chassis frame, 8 perimeter ground-anchor outriggers, crane foundation, and Socket_Power_1."""
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    cw = d["deck_core_w"]
    cd = d["deck_core_d"]
    sh = d["subframe_h"]

    # Main central dark-charcoal under-deck chassis slab
    mb.add_bevel_box(
        center_u=(0.0, sh * 0.5, 0.0),
        size_u=(cw, sh, cd),
        mat="R1_GreyMetal",
        bevel=0.020,
        segments=1,
        top_scale_xz=(0.98, 0.98),
    )

    # 8 perimeter ground-anchor outrigger brackets around the 3.40 x 2.80 m octagonal footprint (matching s_vehicle_pad.png)
    bolts: list[tuple[float, float, float]] = []
    outriggers = [
        (1.56, -0.44, 0.24, 0.22, 0.0),
        (1.56, 0.44, 0.24, 0.22, 0.0),
        (-1.56, -0.44, 0.24, 0.22, 0.0),
        (-1.56, 0.44, 0.24, 0.22, 0.0),
        (-0.56, -1.26, 0.22, 0.24, 0.0),
        (0.56, -1.26, 0.22, 0.24, 0.0),
        (1.38, 1.02, 0.24, 0.24, -40.0),
        (-1.38, 1.02, 0.24, 0.24, 40.0),
    ]
    for ox, oz, ow, od, oyaw in outriggers:
        mb.add_bevel_box(
            center_u=(ox, 0.042, oz),
            size_u=(ow, 0.084, od),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
            rot_deg_u=(0.0, oyaw, 0.0),
        )
        bolts.append((ox, 0.088, oz))

    # Stepped corner crane foundation plinth at (-1.18, y = 0.12 .. 0.26 m, -0.62)
    cx = d["crane_x"]
    cz = d["crane_z"]
    mb.add_bevel_box(
        center_u=(cx, 0.19, cz),
        size_u=(0.54, 0.14, 0.54),
        mat="R1_GreyMetal",
        bevel=0.014,
        segments=1,
        top_scale_xz=(0.92, 0.92),
    )
    for bx_off in (-0.21, 0.21):
        for bz_off in (-0.21, 0.21):
            bolts.append((cx + bx_off, 0.264, cz + bz_off))

    mb.add_bolts(bolts, radius=0.015, height=0.012, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    # Lower power cable gland receptacle on the crane post base (matching s_vehicle_pad.png)
    px = d["socket_power_x"]
    py = d["socket_power_y"]
    pz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(px + 0.032, py, pz),
        radius=0.052,
        length=0.056,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=10,
        bevel=0.005,
    )
    mb.add_cylinder(
        center_u=(px + 0.004, py, pz),
        radius=0.034,
        length=0.036,
        mat="R1_Copper",
        axis_u="X",
        segments=10,
        bevel=0.004,
    )

    stage_1_obj = mb.build_object(parent=root)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(px, py, pz),
        outward_dir_u=(-1.0, 0.0, 0.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Recessed non-slip rubber & steel grate assembly deck, central alignment ring, weld control console, and bollard pedestals."""
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    sh = d["subframe_h"]       # 0.12 m
    dh = d["deck_height"]      # 0.22 m
    deck_cy = 0.5 * (sh + dh)  # 0.17 m

    # 1. Central dark-charcoal assembly floor bed (y = 0.12 .. 0.21 m)
    mb.add_bevel_box(
        center_u=(0.0, deck_cy, 0.04),
        size_u=(2.48, dh - sh - 0.01, 2.08),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )

    # 6 longitudinal dark-rubber non-slip tread strips across the wheel tracks (matching s_vehicle_pad.png)
    for tx in (-0.82, -0.52, -0.22, 0.22, 0.52, 0.82):
        mb.add_bevel_box(
            center_u=(tx, dh - 0.004, 0.06),
            size_u=(0.18, 0.016, 1.84),
            mat="R1_Rubber",
            bevel=0.0,
            segments=0,
        )

    # 4 brushed-steel wheel guide & tie-down strips + central circular calibration boss
    for sx_rail in (-0.98, -0.37, 0.37, 0.98):
        mb.add_bevel_box(
            center_u=(sx_rail, dh - 0.002, 0.06),
            size_u=(0.055, 0.018, 1.88),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )
    mb.add_cylinder(
        center_u=(0.0, dh - 0.002, 0.04),
        radius=0.34,
        length=0.018,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(0.0, dh + 0.004, 0.04),
        radius=0.18,
        length=0.016,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.0,
        bevel_segments=0,
    )

    # 2. Protruding lower weld/crane control console box on the front (+Z) face of the crane post (matching s_vehicle_pad.png)
    con_x = d["console_x"]
    con_y = d["console_y"]
    con_z = d["console_z"]
    mb.add_bevel_box(
        center_u=(con_x, con_y, con_z),
        size_u=(0.24, 0.26, 0.16),
        mat="R1_SagePatch" if False else "R1_GreyMetal",
        bevel=0.012,
        segments=1,
    )
    # Sloped dark-glass operator panel + copper button on the front of the console
    mb.add_bevel_box(
        center_u=(con_x, con_y + 0.03, con_z + 0.075),
        size_u=(0.16, 0.12, 0.024),
        mat="R1_DarkGlass",
        bevel=0.0,
        segments=0,
        rot_deg_u=(-12.0, 0.0, 0.0),
    )
    mb.add_cylinder(
        center_u=(con_x - 0.035, con_y - 0.05, con_z + 0.082),
        radius=0.018,
        length=0.020,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Twin curved power & hydraulic conduits descending from the bottom of the control console to the base gland
    for x_off, mat_p in ((-0.04, "R1_GreyMetal"), (0.04, "R1_Copper")):
        mb.add_tube_path(
            [
                (con_x + x_off, con_y - 0.12, con_z + 0.02),
                (con_x + x_off - 0.04, 0.28, con_z + 0.04),
                (d["socket_power_x"] + 0.06, 0.18, d["socket_power_z"]),
            ],
            radius=0.018,
            mat=mat_p,
            segments=6,
            cap_ends=True,
        )

    # Coiled welding hose hung on the inner (+X) flank of the crane post at y = 1.02 m (matching s_vehicle_pad.png)
    cx = d["crane_x"]
    cz = d["crane_z"]
    mb.add_cylinder(
        center_u=(cx + 0.19, 1.02, cz),
        radius=0.11,
        length=0.048,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=10,
        bevel=0.006,
    )
    mb.add_cylinder(
        center_u=(cx + 0.195, 1.02, cz),
        radius=0.065,
        length=0.056,
        mat="R1_Copper",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # 3. 4 corner bollard lamp bases & top caps at (+-1.36, 0.22 .. 0.44, +-0.98)
    bx_b = d["bollard_x"]
    bz_b = d["bollard_z"]
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            px_b = sx * bx_b
            pz_b = sz * bz_b
            mb.add_cylinder(
                center_u=(px_b, dh + 0.065, pz_b),
                radius=0.072,
                radius_top=0.056,
                length=0.13,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=10,
                bevel=0.005,
            )
            mb.add_cylinder(
                center_u=(px_b, dh + 0.23, pz_b),
                radius=0.052,
                length=0.018,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=8,
                bevel=0.0,
                bevel_segments=0,
            )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Chamfered cream-enamel octagonal deck rim curbs, 1.85 m corner crane post, orange stripe, and sage salvage patches."""
    d = VEHICLE_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    # 1. Seamless octagonal cream-enamel perimeter deck rim curbs (left, right, back, and 4 mitred 45° corners)
    curb_y = 0.155
    curb_h = 0.15
    # Left (+X) and right (-X) side curbs
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 1.38, curb_y, 0.0),
            size_u=(0.28, curb_h, 1.56),
            mat="R1_CreamEnamel",
            bevel=0.020,
            segments=2,
            top_scale_xz=(0.92, 0.98),
        )
    # Rear (-Z) curb
    mb.add_bevel_box(
        center_u=(0.0, curb_y, -1.10),
        size_u=(1.96, curb_h, 0.28),
        mat="R1_CreamEnamel",
        bevel=0.020,
        segments=2,
        top_scale_xz=(0.98, 0.92),
    )
    # 4 mitred corner curb segments completing the continuous octagonal rim
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            yaw = -42.0 if (sx * sz > 0) else 42.0
            mb.add_bevel_box(
                center_u=(sx * 1.18, curb_y, sz * 0.94),
                size_u=(0.28, curb_h, 0.54),
                mat="R1_CreamEnamel",
                bevel=0.018,
                segments=2,
                rot_deg_u=(0.0, yaw, 0.0),
            )

    # Top perimeter hex anchor bolts along the cream-enamel deck curbs
    rim_bolts: list[tuple[float, float, float]] = []
    for sx in (-1.0, 1.0):
        for bz in (-0.64, -0.20, 0.22, 0.64):
            rim_bolts.append((sx * 1.38, curb_y + curb_h * 0.5 + 0.004, bz))
    for bx in (-0.74, -0.24, 0.24, 0.74):
        rim_bolts.append((bx, curb_y + curb_h * 0.5 + 0.004, -1.10))
    mb.add_bolts(rim_bolts, radius=0.013, height=0.010, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    # 2. Corner cantilever crane post at (-1.18, y = 0.26 .. 1.56 m, -0.62)
    cx = d["crane_x"]
    cz = d["crane_z"]
    pw = d["crane_post_w"]
    pd = d["crane_post_d"]
    y0 = 0.26
    y_mid = 1.30
    y_top = d["crane_shoulder_y"]

    # Lower main cream-enamel column shell (y = 0.26 .. 1.30 m)
    mb.add_bevel_box(
        center_u=(cx, 0.5 * (y0 + y_mid), cz),
        size_u=(pw, y_mid - y0, pd),
        mat="R1_CreamEnamel",
        bevel=0.026,
        segments=2,
    )
    # Upper tapered cream-enamel crane shoulder (y = 1.30 .. 1.56 m)
    mb.add_bevel_box(
        center_u=(cx, 0.5 * (y_mid + y_top), cz),
        size_u=(pw + 0.02, y_top - y_mid, pd + 0.02),
        mat="R1_CreamEnamel",
        bevel=0.022,
        segments=2,
        top_scale_xz=(0.88, 0.88),
    )
    # Horizontal panel seam trim ring at y = 1.30 m
    mb.add_bevel_box(
        center_u=(cx, y_mid, cz),
        size_u=(pw + 0.016, 0.016, pd + 0.016),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )

    # 3. Continuous horizontal warm-orange beltline stripe (#FF8A5B) wrapping the crane post at y = 1.16 m
    mb.add_bevel_box(
        center_u=(cx, d["stripe_center_y"], cz),
        size_u=(pw + 0.014, d["stripe_height"], pd + 0.014),
        mat="R1_OrangeStripe",
        bevel=0.006,
        segments=1,
    )

    # 4. Riveted sage-green salvaged maintenance panel (R1_SagePatch) on the front (+Z) face of the crane post
    front_cz = cz + pd * 0.5
    py = d["patch_center_y"]
    mb.add_bevel_box(
        center_u=(cx, py, front_cz + 0.010),
        size_u=(0.24, 0.32, 0.022),
        mat="R1_SagePatch",
        bevel=0.006,
        segments=1,
    )
    patch_rivets = [
        (cx + rx_o, py + ry_o, front_cz + 0.022)
        for rx_o in (-0.088, 0.088)
        for ry_o in (-0.12, 0.12)
    ]
    mb.add_bolts(patch_rivets, radius=0.011, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

    # Second sage-green salvage patch on the front-left (+X, +Z) octagonal curb face
    mb.add_bevel_box(
        center_u=(-1.525, curb_y, 0.06),
        size_u=(0.020, 0.11, 0.36),
        mat="R1_SagePatch",
        bevel=0.005,
        segments=1,
    )

    return mb.build_object(parent=root)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: `CraneBoom` -> (`HoistHook`, `BoomLamp`), hinged front `RampLip`, and 4 corner `BollardLamp_1..4`."""
    d = VEHICLE_PAD_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. CraneBoom: rotating slewing turret + kingpost + cream-enamel cantilevered jib boom + hydraulic luffing cylinder + top tension cable
    cx = d["crane_x"]
    cy = d["crane_shoulder_y"]
    cz = d["crane_z"]
    tx = d["boom_tip_x"]
    ty = d["boom_tip_y"]
    tz = d["boom_tip_z"]

    boom_mb = r1kit.MeshBuilder("CraneBoom", pivot_u=(cx, cy, cz))
    # Slewing ring turntable & vertical cream-enamel kingpost turret reaching total_height = 1.85 m
    boom_mb.add_cylinder(
        center_u=(cx, cy + 0.025, cz),
        radius=0.16,
        length=0.05,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.005,
    )
    boom_mb.add_bevel_box(
        center_u=(cx, cy + 0.15, cz),
        size_u=(0.28, 0.25, 0.28),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=2,
        top_scale_xz=(0.86, 0.86),
    )
    # Sage-green circular shoulder boss on the side of the crane head (matching s_vehicle_pad.png)
    boom_mb.add_cylinder(
        center_u=(cx, cy + 0.14, cz + 0.145),
        radius=0.052,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Main cantilevered cream-enamel jib boom reaching from (cx, cy + 0.12, cz) to (tx, ty, tz) (matching s_vehicle_pad.png)
    boom_mb.add_strut(
        p0_u=(cx, cy + 0.12, cz),
        p1_u=(tx, ty, tz),
        width=0.13,
        thickness=0.15,
        mat="R1_CreamEnamel",
        bevel=0.010,
    )
    # Boom nose sheave block at (tx, ty - 0.02, tz) with orange warning stripe
    boom_mb.add_bevel_box(
        center_u=(tx, ty - 0.01, tz),
        size_u=(0.15, 0.13, 0.15),
        mat="R1_OrangeStripe",
        bevel=0.008,
        segments=1,
    )

    # Stationary upper hoist guide sleeve under the boom tip (y = 1.46 .. 1.72 m) so HoistHook telescopes with zero air gap!
    boom_mb.add_cylinder(
        center_u=(tx, 1.60, tz),
        radius=0.032,
        length=0.22,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Lower diagonal hydraulic luffing cylinder (copper barrel + bare-steel piston rod) bracing the boom
    hyd_p0 = (cx + 0.08, cy - 0.14, cz + 0.06)
    hyd_mid = (cx * 0.62 + tx * 0.38, cy * 0.62 + (ty - 0.06) * 0.38 - 0.07, cz * 0.62 + tz * 0.38)
    hyd_p1 = (cx * 0.38 + tx * 0.62, cy * 0.38 + (ty - 0.06) * 0.62 - 0.03, cz * 0.38 + tz * 0.62)
    boom_mb.add_pipe(hyd_p0, hyd_mid, radius=0.034, mat="R1_Copper", segments=8)
    boom_mb.add_pipe(hyd_mid, hyd_p1, radius=0.020, mat="R1_BareSteel", segments=8)

    # Upper tension guy-cable rod from kingpost peak (y = 1.84 m) to outer boom tip
    boom_mb.add_pipe(
        p0_u=(cx, d["total_height"] - 0.015, cz),
        p1_u=(tx, ty + 0.06, tz),
        radius=0.014,
        mat="R1_BareSteel",
        segments=8,
    )

    # Under-boom floodlamp shade housing at BoomLamp position (matching s_vehicle_pad.png)
    blx = d["boom_lamp_x"]
    bly = d["boom_lamp_y"]
    blz = d["boom_lamp_z"]
    boom_mb.add_cylinder(
        center_u=(blx, bly + 0.03, blz),
        radius=0.095,
        radius_top=0.048,
        length=0.065,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.005,
    )
    boom_obj = boom_mb.build_object(parent=stage_4)

    # 2. HoistHook (child of CraneBoom): telescoping twin steel cables + sheave pulley block + forged cargo hook
    hy_piv = d["hook_pivot_y"]
    hook_mb = r1kit.MeshBuilder("HoistHook", pivot_u=(tx, hy_piv, tz))
    # Twin vertical hoist cables extending from y = 1.71 m (inside the stationary sleeve!) down to the sheave block at y = 1.38 m
    cable_top_y = 1.71
    cable_bot_y = 1.38
    cable_cy = 0.5 * (cable_top_y + cable_bot_y)
    cable_len = cable_top_y - cable_bot_y
    for cx_off in (-0.018, 0.018):
        hook_mb.add_cylinder(
            center_u=(tx + cx_off, cable_cy, tz),
            radius=0.010,
            length=cable_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    # Pulley sheave block & copper wheel
    sheave_y = 1.36
    hook_mb.add_bevel_box(
        center_u=(tx, sheave_y, tz),
        size_u=(0.10, 0.12, 0.082),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    hook_mb.add_cylinder(
        center_u=(tx, sheave_y, tz),
        radius=0.048,
        length=0.094,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.004,
    )
    # Forged steel C-hook & twin spreader prongs below the sheave block
    hook_mb.add_cylinder(
        center_u=(tx, sheave_y - 0.08, tz),
        radius=0.018,
        length=0.056,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    for px_h in (-1.0, 1.0):
        hook_mb.add_strut(
            p0_u=(tx, sheave_y - 0.10, tz),
            p1_u=(tx + px_h * 0.062, sheave_y - 0.17, tz),
            width=0.022,
            thickness=0.026,
            mat="R1_BareSteel",
            bevel=0.003,
        )
        hook_mb.add_strut(
            p0_u=(tx + px_h * 0.062, sheave_y - 0.17, tz),
            p1_u=(tx + px_h * 0.028, sheave_y - 0.22, tz),
            width=0.018,
            thickness=0.022,
            mat="R1_BareSteel",
            bevel=0.003,
        )
    hook_mb.build_object(parent=boom_obj)

    # 3. BoomLamp (child of CraneBoom, only R1_LampAmber)
    bl_mb = r1kit.MeshBuilder("BoomLamp", pivot_u=(blx, bly, blz))
    bl_mb.add_cylinder(
        center_u=(blx, bly, blz),
        radius=0.068,
        radius_top=0.048,
        length=0.022,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=10,
        bevel=0.003,
    )
    bl_mb.build_object(parent=boom_obj)

    # 4. RampLip (child of Stage_4): hinged front (+Z) roll-off ramp lip along z = 1.24 .. 1.42 m
    ry_p = d["ramp_pivot_y"]
    rz_p = d["ramp_pivot_z"]
    rw_x = d["ramp_width_x"]
    rl_z = d["ramp_length_z"]
    ramp_mb = r1kit.MeshBuilder("RampLip", pivot_u=(0.0, ry_p, rz_p))
    # Hinge barrel knuckles along the pivot line
    for hx_k in (-0.76, -0.38, 0.0, 0.38, 0.76):
        ramp_mb.add_cylinder(
            center_u=(hx_k, ry_p, rz_p),
            radius=0.026,
            length=0.12,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    # Tapered wedge ramp deck plate reaching out to z = 1.40 m (footprint_depth / 2 = 1.40 m)
    ramp_mb.add_bevel_box(
        center_u=(0.0, ry_p - 0.01, rz_p + rl_z * 0.44),
        size_u=(rw_x, 0.068, rl_z),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=2,
        rot_deg_u=(10.0, 0.0, 0.0),
    )
    # Cream-enamel side end-caps + 6 raised bare-steel traction cleats across the ramp lip (matching s_vehicle_pad.png)
    for sx_end in (-1.0, 1.0):
        ramp_mb.add_bevel_box(
            center_u=(sx_end * (rw_x * 0.5 + 0.05), ry_p, rz_p + rl_z * 0.40),
            size_u=(0.10, 0.084, rl_z),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
            rot_deg_u=(10.0, 0.0, 0.0),
        )
    for tx_cleat in (-0.70, -0.42, -0.14, 0.14, 0.42, 0.70):
        ramp_mb.add_bevel_box(
            center_u=(tx_cleat, ry_p + 0.024, rz_p + rl_z * 0.46),
            size_u=(0.16, 0.014, 0.12),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
            rot_deg_u=(10.0, 0.0, 0.0),
        )
    ramp_mb.build_object(parent=stage_4)

    # 5. BollardLamp_1..4 (children of Stage_4, only R1_LampAmber)
    bx_b = d["bollard_x"]
    bz_b = d["bollard_z"]
    by_b = d["bollard_lamp_y"]
    bollard_coords = [
        ("BollardLamp_1", bx_b, by_b, bz_b),
        ("BollardLamp_2", -bx_b, by_b, bz_b),
        ("BollardLamp_3", bx_b, by_b, -bz_b),
        ("BollardLamp_4", -bx_b, by_b, -bz_b),
    ]
    for b_name, bx_i, by_i, bz_i in bollard_coords:
        b_mb = r1kit.MeshBuilder(b_name, pivot_u=(bx_i, by_i, bz_i))
        b_mb.add_cylinder(
            center_u=(bx_i, by_i, bz_i),
            radius=0.046,
            radius_top=0.038,
            length=0.078,
            mat="R1_LampAmber",
            axis_u="Y",
            segments=10,
            bevel=0.005,
        )
        b_mb.build_object(parent=stage_4)

    return stage_4


def pose_vehicle_pad_rest(root: bpy.types.Object) -> None:
    """Set VehiclePad moving parts to rest pose (CraneBoom at 0°, HoistHook raised, RampLip stowed at 0°)."""
    d = VEHICLE_PAD_DIMS
    rel_x = d["boom_tip_x"] - d["crane_x"]
    rel_y = d["hook_pivot_y"] - d["crane_shoulder_y"]
    rel_z = d["boom_tip_z"] - d["crane_z"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name in ("CraneBoom", "RampLip"):
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "HoistHook":
            obj.location = r1kit.u_pos(rel_x, rel_y, rel_z)
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_vehicle_pad_active(root: bpy.types.Object) -> None:
    """Set VehiclePad moving parts to active assembly pose (CraneBoom slewed, HoistHook lowered -0.20 m, RampLip pitched -25°)."""
    d = VEHICLE_PAD_DIMS
    rel_x = d["boom_tip_x"] - d["crane_x"]
    rel_y = d["hook_pivot_y"] - d["crane_shoulder_y"] - d["hook_drop_y"]
    rel_z = d["boom_tip_z"] - d["crane_z"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "CraneBoom":
            obj.rotation_euler = r1kit.u_rot(0.0, -24.0, 0.0)
        elif obj.name == "HoistHook":
            obj.location = r1kit.u_pos(rel_x, rel_y, rel_z)
        elif obj.name == "RampLip":
            obj.rotation_euler = r1kit.u_rot(d["ramp_drop_deg"], 0.0, 0.0)


def render_vehicle_pad_compare(root: bpy.types.Object, out_dir: Path) -> None:
    """Render side-by-side concept comparison (`Logs/renders/compare_vehicle_pad.png`) against `s_vehicle_pad.png`."""
    d = VEHICLE_PAD_DIMS
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    r1kit.ensure_materials(use_vertex_color_shading=True)
    temp_objs = r1kit._setup_preview_lighting_and_plinth(d["footprint_width"] * 0.62)

    cam_data = bpy.data.cameras.new("_CompareCam")
    cam_obj = bpy.data.objects.new("_CompareCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.append(cam_obj)

    # Place a compact Rover 07 stand-in on the pad in active assembly pose matching s_vehicle_pad.png
    rover_obj = r1kit.build_rover_07_standin(
        pos_u=(0.08, d["deck_height"], 0.04),
        rover_width=1.28,
        rover_height=1.18,
        rover_length=1.54,
        yaw_deg_u=24.0,
    )
    temp_objs.append(rover_obj)

    pose_vehicle_pad_active(root)
    bpy.context.view_layer.update()
    mid_y = d["total_height"] * 0.46
    dist = d["footprint_width"] * 1.42
    r1kit._aim_camera_at(
        cam_obj,
        (-dist * 0.56, mid_y + dist * 0.36, dist * 0.82),
        (0.0, mid_y * 0.90, 0.0),
        lens_mm=46.0,
    )
    p_hero = out_dir / "_tmp_vehicle_pad_concept_match.png"
    r1kit._render_to_file(p_hero, res_x=1120, res_y=840)

    concept_path = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\s_vehicle_pad.png")
    if concept_path.exists():
        r1kit.compose_side_by_side(
            concept_path,
            p_hero,
            out_dir / "compare_vehicle_pad.png",
            target_height=768,
        )
    p_hero.unlink(missing_ok=True)

    pose_vehicle_pad_rest(root)
    bpy.context.view_layer.update()

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    plinth_mat = bpy.data.materials.get("_PreviewPlinthMat")
    if plinth_mat is not None:
        bpy.data.materials.remove(plinth_mat)
    r1kit.ensure_materials(use_vertex_color_shading=False)


def build_vehicle_pad() -> bpy.types.Object:
    """Build the complete VehiclePad hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("VehiclePad", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_vehicle_pad_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=VEHICLE_PAD_DIMS["footprint_width"] * 0.56)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build VehiclePad FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/VehiclePad.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_vehicle_pad()
    tris = r1kit.count_triangles(root)
    print(f"[VehiclePad] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        render_vehicle_pad_compare(root, args.renders_dir)
        r1kit.render_structure_suite(
            root=root,
            slug="vehicle_pad",
            out_dir=args.renders_dir,
            target_height=VEHICLE_PAD_DIMS["total_height"],
            footprint_radius=VEHICLE_PAD_DIMS["footprint_width"] * 0.56,
            pose_rest_fn=pose_vehicle_pad_rest,
            pose_active_fn=pose_vehicle_pad_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
