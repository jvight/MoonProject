"""Build script for the R1 ChargingPad structure (`Assets/_Project/Art/R1/Models/Structures/ChargingPad.fbx`)."""

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

# Single table of dimensions taken from DESIGN.md (sections 1.2, 2, and 3.1)
CHARGING_PAD_DIMS: dict[str, float] = {
    # Overall footprint and heights (DESIGN.md §2 & §3.1 + Scale Bible §1.1)
    "footprint_width": 3.04,
    "footprint_depth": 3.40,
    "deck_height": 0.16,
    "bollard_height": 1.55,
    "ramp_angle_deg": 11.0,
    # Moving-part ranges (TASK.md & DESIGN.md §3.1)
    "contact_shoes_rise": 0.08,
    "cooling_louver_open_deg": 20.0,
    "chock_lock_deg": 65.0,
    "clamp_arm_lock_deg": 80.0,
    # Wheel chocks & side clamp arms (07 wheelbase ±0.53 m, track ±0.61 m, tyre R=0.35 m, body width 0.96 m)
    "chock_x": 0.61,
    "chock_hinge_y": 0.16,
    "chock_hinge_z": 0.765,
    "chock_width": 0.28,
    "chock_length": 0.26,
    "clamp_hinge_x": 0.75,
    "clamp_hinge_y": 0.16,
    "clamp_hinge_z": 0.0,
    # Octagonal deck & 3-sided ramp geometry (widened for >=0.10m side clearance + <=12 deg ramp for 07)
    "deck_core_width": 2.12,
    "deck_core_depth": 2.46,
    "deck_core_center_z": -0.10,
    "deck_chamfer": 0.16,
    "ramp_length": 0.68,
    "ramp_width_front": 1.88,
    "ramp_width_side": 0.90,
    "ramp_thickness": 0.064,
    "guide_rail_x": 0.92,
    "guide_rail_width": 0.095,
    "guide_rail_height": 0.070,
    "guide_rail_length": 1.86,
    "wheel_lane_x": 0.61,
    "wheel_pad_width": 0.36,
    "wheel_pad_length": 1.38,
    "well_width": 0.58,
    "well_depth": 0.72,
    "well_center_z": 0.04,
    # Rear power bollard (DESIGN.md §3.1: 1.55 m tall curved-top cream-enamel locker bollard)
    "plinth_width": 0.76,
    "plinth_height": 0.22,
    "plinth_depth": 0.50,
    "bollard_center_z": -1.35,
    "bollard_base_y": 0.22,
    "bollard_width": 0.62,
    "bollard_depth": 0.44,
    "bollard_arch_radius": 0.19,
    # Single worn warm-orange stripe (#FF8A5B)
    "stripe_center_y": 0.78,
    "stripe_height": 0.115,
    # Salvaged sage-green side bracket (#78A582)
    "patch_center_y": 0.80,
    "patch_height": 0.54,
    "patch_depth": 0.26,
    "patch_thickness": 0.026,
    # Front 4-bar totem lamp bezel & bars
    "totem_bezel_y": 0.90,
    "totem_bezel_width": 0.21,
    "totem_bezel_height": 0.42,
    "totem_bar_width": 0.145,
    "totem_bar_height": 0.052,
    "totem_bar_depth": 0.022,
    "totem_bar_1_y": 0.76,
    "totem_bar_step_y": 0.092,
    # Cooling louver on rear of bollard
    "louver_hinge_y": 1.24,
    "louver_hinge_z": -1.57,
    "louver_width": 0.40,
    "louver_height": 0.44,
    "louver_depth": 0.046,
    # Contact shoes in center of deck
    "shoes_pivot_y": 0.10,
    "shoes_pivot_z": 0.04,
    "shoe_plate_x": 0.17,
    "shoe_plate_width": 0.15,
    "shoe_plate_length": 0.52,
    "shoe_plate_top_y": 0.21,
    # Rear power socket
    "socket_power_y": 0.20,
    "socket_power_z": -1.64,
}


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Octagonal foundation deck, perimeter anchor bolts, central contact well, chock slots, and plinth."""
    d = CHARGING_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    # Main octagonal deck foundation
    mb.add_oct_prism(
        center_u=(0.0, d["deck_height"] * 0.5, d["deck_core_center_z"]),
        width_x=d["deck_core_width"],
        height_y=d["deck_height"],
        depth_z=d["deck_core_depth"],
        chamfer=d["deck_chamfer"],
        mat="R1_GreyMetal",
        bevel=0.018,
        bevel_segments=2,
        top_inset=0.03,
    )

    # Central contact shoe recess rim & dark well pan
    mb.add_bevel_box(
        center_u=(0.0, d["deck_height"] - 0.015, d["well_center_z"]),
        size_u=(d["well_width"], 0.04, d["well_depth"]),
        mat="R1_Rubber",
        bevel=0.008,
        segments=1,
    )

    # Rear bollard charcoal base plinth extending behind deck
    mb.add_bevel_box(
        center_u=(0.0, d["plinth_height"] * 0.5, d["bollard_center_z"] - 0.03),
        size_u=(d["plinth_width"], d["plinth_height"], d["plinth_depth"]),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=1,
    )

    # Non-slip rubber wheel pads on left and right wheel lanes between the front and rear chock slots
    for side in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(side * d["wheel_lane_x"], d["deck_height"] + 0.005, 0.0),
            size_u=(d["wheel_pad_width"], 0.014, d["wheel_pad_length"]),
            mat="R1_Rubber",
            bevel=0.005,
            segments=1,
        )

    # Four dark recessed wheel-chock slots in the plate so the player sees "something closes here" when open
    slot_w = d["chock_width"] + 0.11
    slot_len = d["chock_length"] + 0.12
    slot_center_z = d["chock_hinge_z"] + d["chock_length"] * 0.5 + 0.008
    for sx in (-1.0, 1.0):
        cx = sx * d["chock_x"]
        for sz in (-1.0, 1.0):
            cz = sz * slot_center_z
            # Dark recess well pan (top at deck_height + 0.0015 m, below the chock's 0.0045..0.009 m surfaces)
            mb.add_bevel_box(
                center_u=(cx, d["deck_height"] - 0.0105, cz),
                size_u=(slot_w, 0.024, slot_len),
                mat="R1_DarkGlass",
                bevel=0.004,
                segments=1,
            )
            # Bare-steel slot coaming rails framing the left and right sides of each chock recess
            for rail_s in (-1.0, 1.0):
                mb.add_bevel_box(
                    center_u=(cx + rail_s * (slot_w * 0.5 + 0.010), d["deck_height"] + 0.006, cz),
                    size_u=(0.022, 0.016, slot_len + 0.02),
                    mat="R1_BareSteel",
                    bevel=0.003,
                    segments=1,
                )

    # Top deck hex bolts around octagon corners
    top_bolts: list[tuple[float, float, float]] = []
    for side in (-1.0, 1.0):
        for z_pos in (-1.14, 0.94):
            top_bolts.append((side * 0.84, d["deck_height"] + 0.008, z_pos))
    mb.add_bolts(top_bolts, radius=0.017, height=0.012, axis_u="Y", mat="R1_BareSteel")

    # Vertical chamfer-face bolt pairs matching s_charging_pad.png
    chamfer_bolts: list[tuple[float, float, float]] = []
    cx_mid = d["deck_core_width"] * 0.5 - d["deck_chamfer"] * 0.5
    cz_mid = d["deck_core_depth"] * 0.5 - d["deck_chamfer"] * 0.5
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            for offset in (-0.035, 0.035):
                bx = sx * (cx_mid + offset)
                bz = d["deck_core_center_z"] + sz * (cz_mid - offset)
                for by in (0.055, 0.125):
                    chamfer_bolts.append((bx, by, bz))
    mb.add_bolts(chamfer_bolts, radius=0.014, height=0.012, axis_u="Z", mat="R1_BareSteel")

    return mb.build_object(parent=root)


def _add_sloped_ramp(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    width: float,
    length: float,
    thickness: float,
    pitch_deg: float,
    yaw_deg: float,
) -> None:
    """Add a 15-deg sloped entry ramp with raised side cheeks and recessed dark rubber tread."""
    # Main grey metal ramp wedge
    mb.add_bevel_box(
        center_u=center_u,
        size_u=(width, thickness, length),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=1,
        rot_deg_u=(-pitch_deg, yaw_deg, 0.0),
    )
    # Recessed black rubber non-slip tread mat in center of ramp
    cx, cy, cz = center_u
    mb.add_bevel_box(
        center_u=(cx, cy + 0.010, cz),
        size_u=(width - 0.18, thickness * 0.85, length - 0.08),
        mat="R1_Rubber",
        bevel=0.006,
        segments=1,
        rot_deg_u=(-pitch_deg, yaw_deg, 0.0),
    )


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: 15-deg entry wheel ramps (Front, Left, Right), wheel-guide rails, inner frame, and power socket."""
    d = CHARGING_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    ramp_len = d["ramp_length"]
    ramp_h = d["deck_height"]
    ramp_pitch = d["ramp_angle_deg"]
    ramp_thick = d["ramp_thickness"]
    front_ramp_z = d["deck_core_center_z"] + d["deck_core_depth"] * 0.5 + 0.24
    side_ramp_x = d["deck_core_width"] * 0.5 + 0.20

    # Front (+Z) 11-deg entry ramp
    _add_sloped_ramp(
        mb,
        center_u=(0.0, ramp_h * 0.52, front_ramp_z),
        width=d["ramp_width_front"],
        length=ramp_len,
        thickness=ramp_thick,
        pitch_deg=ramp_pitch,
        yaw_deg=0.0,
    )

    # Left (-X) and Right (+X) 11-deg side ramps
    for side in (-1.0, 1.0):
        _add_sloped_ramp(
            mb,
            center_u=(side * side_ramp_x, ramp_h * 0.52, d["deck_core_center_z"]),
            width=d["ramp_width_side"],
            length=ramp_len,
            thickness=ramp_thick,
            pitch_deg=ramp_pitch,
            yaw_deg=side * 90.0,
        )

    # Raised left & right wheel-guide rails split around the central side-clamp bay (z_u in [-0.22, +0.22])
    rail_seg_len = 0.68
    for side in (-1.0, 1.0):
        for seg_z in (-0.58, 0.56):
            mb.add_bevel_box(
                center_u=(
                    side * d["guide_rail_x"],
                    d["deck_height"] + d["guide_rail_height"] * 0.5,
                    seg_z,
                ),
                size_u=(d["guide_rail_width"], d["guide_rail_height"], rail_seg_len),
                mat="R1_GreyMetal",
                bevel=0.012,
                segments=1,
                top_scale_xz=(0.74, 0.92),
            )
            mb.add_bevel_box(
                center_u=(
                    side * (d["guide_rail_x"] - 0.042),
                    d["deck_height"] + d["guide_rail_height"] * 0.45,
                    seg_z,
                ),
                size_u=(0.018, 0.032, rail_seg_len - 0.10),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
            )
        # Side-clamp hinge cradle and dark recess pocket at the plate edge (z_u = 0.0)
        mb.add_bevel_box(
            center_u=(side * (d["clamp_hinge_x"] + 0.16), d["deck_height"] - 0.010, d["clamp_hinge_z"]),
            size_u=(0.36, 0.023, 0.40),
            mat="R1_DarkGlass",
            bevel=0.004,
            segments=1,
        )

    # Internal bollard structural frame columns (welded in Stage 2 before Stage 3 shell)
    for side in (-1.0, 1.0):
        mb.add_strut(
            p0_u=(side * 0.22, d["plinth_height"], d["bollard_center_z"]),
            p1_u=(side * 0.22, 1.32, d["bollard_center_z"]),
            width=0.055,
            thickness=0.055,
            mat="R1_GreyMetal",
        )
    mb.add_strut(
        p0_u=(-0.22, 0.78, d["bollard_center_z"]),
        p1_u=(0.22, 0.78, d["bollard_center_z"]),
        width=0.05,
        thickness=0.05,
        mat="R1_GreyMetal",
    )

    # Rear heavy power cable gland receptacle
    mb.add_cylinder(
        center_u=(0.0, d["socket_power_y"], d["bollard_center_z"] - 0.24),
        radius=0.075,
        length=0.10,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=12,
        bevel=0.01,
    )
    mb.add_cylinder(
        center_u=(0.0, d["socket_power_y"], d["socket_power_z"] + 0.02),
        radius=0.052,
        length=0.06,
        mat="R1_Copper",
        axis_u="Z",
        segments=12,
        bevel=0.008,
    )

    stage_2_obj = mb.build_object(parent=root)

    # Power socket empty on the rear receptacle pointing -Z outward
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_2_obj,
        pivot_u=(0.0, d["socket_power_y"], d["socket_power_z"]),
        outward_dir_u=(0.0, 0.0, -1.0),
    )
    return stage_2_obj


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Curved-top cream-enamel bollard shell, continuous orange stripe, and sage salvage bracket."""
    d = CHARGING_PAD_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    shell_h = d["bollard_height"] - d["bollard_base_y"]
    # Main curved-top cream-enamel bollard housing arched front-to-back (matching s_charging_pad.png)
    mb.add_arch_housing(
        base_center_u=(0.0, d["bollard_base_y"], d["bollard_center_z"]),
        width_x=d["bollard_width"],
        height_y=shell_h,
        depth_z=d["bollard_depth"],
        arch_radius=d["bollard_arch_radius"],
        mat="R1_CreamEnamel",
        bevel=0.026,
        arch_segments=10,
        arch_axis_u="Z",
    )

    # Single continuous worn warm-orange beltline stripe (#FF8A5B) wrapping the bollard
    mb.add_bevel_box(
        center_u=(0.0, d["stripe_center_y"], d["bollard_center_z"]),
        size_u=(d["bollard_width"] + 0.012, d["stripe_height"], d["bollard_depth"] + 0.012),
        mat="R1_OrangeStripe",
        bevel=0.008,
        segments=1,
    )

    # Front panel trim seam strip at y = 1.18 m (matching s_charging_pad.png)
    front_z = d["bollard_center_z"] + d["bollard_depth"] * 0.5
    mb.add_bevel_box(
        center_u=(0.0, 1.19, front_z + 0.002),
        size_u=(d["bollard_width"] - 0.06, 0.014, 0.012),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )

    # Front dark-glass / charcoal recessed bezel for the 4-bar vertical totem lamp
    mb.add_bevel_box(
        center_u=(0.0, d["totem_bezel_y"], front_z + 0.005),
        size_u=(d["totem_bezel_width"], d["totem_bezel_height"], 0.026),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, d["totem_bezel_y"], front_z + 0.011),
        size_u=(d["totem_bezel_width"] - 0.032, d["totem_bezel_height"] - 0.032, 0.020),
        mat="R1_DarkGlass",
        bevel=0.005,
        segments=1,
    )

    # Rear dark housing recess for the cooling louver
    back_z = d["bollard_center_z"] - d["bollard_depth"] * 0.5
    mb.add_bevel_box(
        center_u=(0.0, d["louver_hinge_y"] - d["louver_height"] * 0.5, back_z - 0.004),
        size_u=(d["louver_width"] + 0.04, d["louver_height"] + 0.04, 0.024),
        mat="R1_DarkGlass",
        bevel=0.006,
        segments=1,
    )

    # Salvaged sage-green side bracket (#78A582) on the viewer-facing left (+X) side of the bollard (matching s_charging_pad.png)
    side_x = d["bollard_width"] * 0.5 + 0.010
    mb.add_bevel_box(
        center_u=(side_x, d["patch_center_y"], d["bollard_center_z"] - 0.01),
        size_u=(d["patch_thickness"], d["patch_height"], d["patch_depth"]),
        mat="R1_SagePatch",
        bevel=0.009,
        segments=1,
    )
    # Stepped rear mounting ear on the sage-green bracket reaching toward the rear louver
    mb.add_bevel_box(
        center_u=(side_x, d["patch_center_y"] + 0.04, d["bollard_center_z"] - 0.14),
        size_u=(d["patch_thickness"], 0.25, 0.16),
        mat="R1_SagePatch",
        bevel=0.008,
        segments=1,
    )

    # Rivets on the sage-green salvage bracket and front locker panel corners
    rivets: list[tuple[float, float, float]] = [
        (side_x + 0.012, d["patch_center_y"] + 0.21, d["bollard_center_z"] + 0.06),
        (side_x + 0.012, d["patch_center_y"] - 0.21, d["bollard_center_z"] + 0.06),
        (side_x + 0.012, d["patch_center_y"] + 0.21, d["bollard_center_z"] - 0.08),
        (side_x + 0.012, d["patch_center_y"] - 0.21, d["bollard_center_z"] - 0.08),
    ]
    mb.add_bolts(rivets, radius=0.015, height=0.010, axis_u="X", mat="R1_BareSteel")

    front_bolts: list[tuple[float, float, float]] = []
    for side in (-1.0, 1.0):
        for by in (0.30, 1.13, 1.24):
            front_bolts.append((side * 0.23, by, front_z + 0.006))
    mb.add_bolts(front_bolts, radius=0.013, height=0.010, axis_u="Z", mat="R1_BareSteel")

    return mb.build_object(parent=root)


def _build_wheel_chocks(stage_4: bpy.types.Object) -> None:
    """Build 4 chunky VIS wheel chocks (`Chock_FL`, `Chock_FR`, `Chock_RL`, `Chock_RR`) flush in deck slots at rest."""
    d = CHARGING_PAD_DIMS
    hy = d["chock_hinge_y"]
    cw = d["chock_width"]
    clen = d["chock_length"]

    chock_specs = (
        ("Chock_FL", -1.0, 1.0),
        ("Chock_FR", 1.0, 1.0),
        ("Chock_RL", -1.0, -1.0),
        ("Chock_RR", 1.0, -1.0),
    )
    for name, sx, sz in chock_specs:
        hx = sx * d["chock_x"]
        hz = sz * d["chock_hinge_z"]
        mb = r1kit.MeshBuilder(name, pivot_u=(hx, hy, hz))

        # Heavy bare-steel hinge barrel along the pivot line
        mb.add_cylinder(
            center_u=(hx, hy - 0.002, hz),
            radius=0.018,
            length=cw + 0.016,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.004,
        )
        # Main bare-steel chock wedge plate (top at hy + 0.0045 m, clearly visible above the 0.0015 m dark recess)
        mb.add_bevel_box(
            center_u=(hx, hy - 0.0135, hz + sz * (clen * 0.48)),
            size_u=(cw, 0.036, clen - 0.015),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )
        # Worn warm-amber/orange perimeter hazard frame (top at hy + 0.007 m around outer lip and side cheeks)
        mb.add_bevel_box(
            center_u=(hx, hy - 0.004, hz + sz * (clen * 0.53)),
            size_u=(cw - 0.018, 0.022, clen - 0.035),
            mat="R1_OrangeStripe",
            bevel=0.005,
            segments=1,
        )
        # Moulded black rubber tyre-cradle pad on the wheel-facing (top when open) face (top at hy + 0.0095 m)
        mb.add_bevel_box(
            center_u=(hx, hy + 0.0015, hz + sz * (clen * 0.45)),
            size_u=(cw - 0.068, 0.016, clen - 0.090),
            mat="R1_Rubber",
            bevel=0.004,
            segments=1,
        )

        mb.build_object(parent=stage_4)


def _build_side_clamp_arms(stage_4: bpy.types.Object) -> None:
    """Build 2 side clamp arms (`ClampArm_L`, `ClampArm_R`) folded low along the plate sides that swing up/in 80 deg."""
    d = CHARGING_PAD_DIMS
    hy = d["clamp_hinge_y"]
    hz = d["clamp_hinge_z"]

    for side, name in ((-1.0, "ClampArm_L"), (1.0, "ClampArm_R")):
        hx = side * d["clamp_hinge_x"]
        mb = r1kit.MeshBuilder(name, pivot_u=(hx, hy, hz))

        # Heavy hinge barrel along the plate-edge pivot line (axis Z in r1kit = Y in Blender)
        mb.add_cylinder(
            center_u=(hx, hy, hz),
            radius=0.022,
            length=0.34,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.004,
        )
        # Flat base beam folded along the side of the plate in the open pose (0 deg)
        mb.add_bevel_box(
            center_u=(hx + side * 0.115, hy + 0.012, hz),
            size_u=(0.23, 0.034, 0.30),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )
        # Two shallow-angled dogleg claw arms (folded low outward when open; arch up and in at 80 deg when locked)
        for z_off in (-0.095, 0.095):
            mb.add_strut(
                p0_u=(hx + side * 0.18, hy + 0.018, hz + z_off),
                p1_u=(hx + side * 0.355, hy + 0.235, hz + z_off),
                width=0.046,
                thickness=0.044,
                mat="R1_BareSteel",
            )

        # Worn amber/orange hazard edge strip along the outer spine of the clamp arm
        mb.add_bevel_box(
            center_u=(hx + side * 0.12, hy + 0.026, hz),
            size_u=(0.14, 0.014, 0.26),
            mat="R1_OrangeStripe",
            bevel=0.004,
            segments=1,
        )
        mb.add_strut(
            p0_u=(hx + side * 0.20, hy + 0.012, hz),
            p1_u=(hx + side * 0.375, hy + 0.225, hz),
            width=0.18,
            thickness=0.026,
            mat="R1_OrangeStripe",
        )

        # Upper bare-steel jaw backing plate (tilted 10 deg in open pose so at +80 deg locked pose it stands vertical)
        mb.add_bevel_box(
            center_u=(hx + side * 0.352, hy + 0.245, hz),
            size_u=(0.095, 0.040, 0.25),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
            rot_deg_u=(0.0, 0.0, side * 10.0),
        )
        # Chunky black rubber flank bumper block (inner face rests at |X| = 0.484 m against 07's 0.480 m flank)
        mb.add_bevel_box(
            center_u=(hx + side * 0.338, hy + 0.290, hz),
            size_u=(0.125, 0.068, 0.23),
            mat="R1_Rubber",
            bevel=0.007,
            segments=1,
            rot_deg_u=(0.0, 0.0, side * 10.0),
        )

        mb.build_object(parent=stage_4)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: Moving parts (`ContactShoes`, `CoolingLouver`, `Chock_*`, `ClampArm_*`) and lamps."""
    d = CHARGING_PAD_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. ContactShoes (pivot at (0, 0.10, 0.04); rises +0.08 m along +Y)
    shoes_pivot = (0.0, d["shoes_pivot_y"], d["shoes_pivot_z"])
    shoes_mb = r1kit.MeshBuilder("ContactShoes", pivot_u=shoes_pivot)
    for side in (-1.0, 1.0):
        sx = side * d["shoe_plate_x"]
        # Top curved/bevelled brass inductive contact shoe
        shoes_mb.add_bevel_box(
            center_u=(sx, d["shoe_plate_top_y"], d["shoes_pivot_z"]),
            size_u=(d["shoe_plate_width"], 0.030, d["shoe_plate_length"]),
            mat="R1_Copper",
            bevel=0.008,
            segments=1,
            top_scale_xz=(0.82, 0.92),
        )
        # Base rail inside the well
        shoes_mb.add_bevel_box(
            center_u=(sx, d["shoes_pivot_y"] + 0.01, d["shoes_pivot_z"]),
            size_u=(0.12, 0.020, d["shoe_plate_length"] - 0.06),
            mat="R1_Copper",
            bevel=0.005,
            segments=1,
        )
        # Crossed brass scissor linkage struts under each shoe (matching s_charging_pad.png)
        shoes_mb.add_strut(
            p0_u=(sx - 0.032, d["shoes_pivot_y"] + 0.012, d["shoes_pivot_z"] - 0.19),
            p1_u=(sx - 0.032, d["shoe_plate_top_y"] - 0.008, d["shoes_pivot_z"] + 0.19),
            width=0.022,
            thickness=0.030,
            mat="R1_Copper",
        )
        shoes_mb.add_strut(
            p0_u=(sx + 0.032, d["shoes_pivot_y"] + 0.012, d["shoes_pivot_z"] + 0.19),
            p1_u=(sx + 0.032, d["shoe_plate_top_y"] - 0.008, d["shoes_pivot_z"] - 0.19),
            width=0.022,
            thickness=0.030,
            mat="R1_Copper",
        )
        # Center scissor pivot pin
        shoes_mb.add_cylinder(
            center_u=(sx, (d["shoes_pivot_y"] + d["shoe_plate_top_y"]) * 0.5, d["shoes_pivot_z"]),
            radius=0.018,
            length=0.11,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.003,
        )
    # Crossbar linking left and right shoes
    shoes_mb.add_cylinder(
        center_u=(0.0, (d["shoes_pivot_y"] + d["shoe_plate_top_y"]) * 0.5, d["shoes_pivot_z"]),
        radius=0.020,
        length=d["shoe_plate_x"] * 2.0,
        mat="R1_Copper",
        axis_u="X",
        segments=10,
        bevel=0.004,
    )
    shoes_mb.build_object(parent=stage_4)

    # 2. CoolingLouver (pivot at top hinge (0, 1.24, -1.57); opens +20 deg about hinge axis X)
    louver_pivot = (0.0, d["louver_hinge_y"], d["louver_hinge_z"])
    louver_mb = r1kit.MeshBuilder("CoolingLouver", pivot_u=louver_pivot)
    # Top hinge barrel
    louver_mb.add_cylinder(
        center_u=louver_pivot,
        radius=0.020,
        length=d["louver_width"] + 0.04,
        mat="R1_BareSteel",
        axis_u="X",
        segments=10,
        bevel=0.004,
    )
    # Hinged hood frame with sage-green outer skin & dark louvers (matching s_charging_pad.png)
    louver_mb.add_bevel_box(
        center_u=(0.0, d["louver_hinge_y"] - d["louver_height"] * 0.5, d["louver_hinge_z"] - 0.055),
        size_u=(d["louver_width"], d["louver_height"], d["louver_depth"]),
        mat="R1_SagePatch",
        bevel=0.010,
        segments=1,
        rot_deg_u=(-14.0, 0.0, 0.0),
    )
    # 5 horizontal louver slats inside the hood
    for i in range(5):
        sy = d["louver_hinge_y"] - 0.075 - i * 0.075
        sz = d["louver_hinge_z"] - 0.032 - i * 0.018
        louver_mb.add_bevel_box(
            center_u=(0.0, sy, sz),
            size_u=(d["louver_width"] - 0.05, 0.022, 0.060),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
            rot_deg_u=(-28.0, 0.0, 0.0),
        )
    louver_mb.build_object(parent=stage_4)

    # 3. GuideStrip_L and GuideStrip_R (separate meshes using ONLY R1_LampAmber, flush on top of ramp side cheeks)
    ramp_pitch = d["ramp_angle_deg"]
    ramp_len = d["ramp_length"]
    front_half_w = d["ramp_width_front"] * 0.5 - 0.045
    side_half_w = d["ramp_width_side"] * 0.5 - 0.045
    strip_y = d["deck_height"] * 0.52 + d["ramp_thickness"] * 0.5 + 0.004
    front_ramp_z = d["deck_core_center_z"] + d["deck_core_depth"] * 0.5 + 0.24
    side_ramp_x = d["deck_core_width"] * 0.5 + 0.20

    for side, name in ((-1.0, "GuideStrip_L"), (1.0, "GuideStrip_R")):
        g_mb = r1kit.MeshBuilder(name, pivot_u=(side * front_half_w, strip_y, front_ramp_z))
        # Strip on the front (+Z) ramp cheek
        g_mb.add_bevel_box(
            center_u=(side * front_half_w, strip_y, front_ramp_z),
            size_u=(0.038, 0.022, ramp_len - 0.05),
            mat="R1_LampAmber",
            bevel=0.004,
            segments=1,
            rot_deg_u=(-ramp_pitch, 0.0, 0.0),
        )
        # Two strips on the corresponding side (-X or +X) ramp cheeks
        for z_cheek in (-side_half_w, side_half_w):
            g_mb.add_bevel_box(
                center_u=(side * side_ramp_x, strip_y, d["deck_core_center_z"] + z_cheek),
                size_u=(0.038, 0.022, ramp_len - 0.05),
                mat="R1_LampAmber",
                bevel=0.004,
                segments=1,
                rot_deg_u=(-ramp_pitch, side * 90.0, 0.0),
            )
        g_mb.build_object(parent=stage_4)

    # 4. TotemBar_1 .. TotemBar_4 (4 vertical progress bars on the bollard front, bottom to top)
    front_z = d["bollard_center_z"] + d["bollard_depth"] * 0.5 + 0.018
    for idx in range(4):
        bar_name = f"TotemBar_{idx + 1}"
        by = d["totem_bar_1_y"] + idx * d["totem_bar_step_y"]
        b_mb = r1kit.MeshBuilder(bar_name, pivot_u=(0.0, by, front_z))
        b_mb.add_bevel_box(
            center_u=(0.0, by, front_z),
            size_u=(d["totem_bar_width"], d["totem_bar_height"], d["totem_bar_depth"]),
            mat="R1_LampAmber",
            bevel=0.006,
            segments=1,
        )
        b_mb.build_object(parent=stage_4)

    # 5. Four Wheel Chocks (`Chock_FL`, `Chock_FR`, `Chock_RL`, `Chock_RR`)
    _build_wheel_chocks(stage_4)

    # 6. Two Side Clamp Arms (`ClampArm_L`, `ClampArm_R`)
    _build_side_clamp_arms(stage_4)

    return stage_4


def pose_charging_pad_rest(root: bpy.types.Object) -> None:
    """Set ChargingPad moving parts to their rest/open pose (shoes down, louver closed, chocks & clamps open)."""
    d = CHARGING_PAD_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "ContactShoes":
            obj.location = r1kit.u_pos(0.0, d["shoes_pivot_y"], d["shoes_pivot_z"])
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "CoolingLouver":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name.startswith("Chock_") or obj.name.startswith("ClampArm_"):
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_charging_pad_active(root: bpy.types.Object) -> None:
    """Set ChargingPad moving parts to their locked/active pose (chocks ~65 deg, clamp arms ~80 deg)."""
    d = CHARGING_PAD_DIMS
    chock_rad = math.radians(d["chock_lock_deg"])
    clamp_rad = math.radians(d["clamp_arm_lock_deg"])
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "ContactShoes":
            obj.location = r1kit.u_pos(
                0.0,
                d["shoes_pivot_y"] + d["contact_shoes_rise"],
                d["shoes_pivot_z"],
            )
        elif obj.name == "CoolingLouver":
            # In Blender (Y=forward, Z=up), rotating -X swings the lower (-Z) edge backward (-Y) by +20 deg
            obj.rotation_euler = (math.radians(-d["cooling_louver_open_deg"]), 0.0, 0.0)
        elif obj.name in ("Chock_FL", "Chock_FR"):
            # Front (+Y) chocks hinge up toward the front tyres via +65 deg about Blender +X
            obj.rotation_euler = (chock_rad, 0.0, 0.0)
        elif obj.name in ("Chock_RL", "Chock_RR"):
            # Rear (-Y) chocks hinge up toward the rear tyres via -65 deg about Blender +X
            obj.rotation_euler = (-chock_rad, 0.0, 0.0)
        elif obj.name == "ClampArm_L":
            # Left (-X) clamp arm swings up/in toward 07's left flank via +80 deg about Blender +Y
            obj.rotation_euler = (0.0, clamp_rad, 0.0)
        elif obj.name == "ClampArm_R":
            # Right (+X) clamp arm swings up/in toward 07's right flank via -80 deg about Blender +Y
            obj.rotation_euler = (0.0, -clamp_rad, 0.0)


def build_charging_pad() -> bpy.types.Object:
    """Build the complete ChargingPad hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("ChargingPad", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_charging_pad_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=CHARGING_PAD_DIMS["footprint_width"] * 0.55)
    return root


def render_charging_pad_open_and_locked() -> None:
    """Render Day C (dawn) and Earthlit night captures of ChargingPad: (1) open with no rover, (2) locked with 07."""
    from mathutils import Euler, Vector
    from ruins import ruins_common as rc

    # 1. Pad OPEN (rest pose) with no rover (Day C + Earthlit night, 3/4 chase camera view)
    rc.reset_ruins_scene()
    open_group = bpy.data.objects.new("_ChargingPadOpenScene", None)
    bpy.context.scene.collection.objects.link(open_group)
    pad_open = build_charging_pad()
    pose_charging_pad_rest(pad_open)
    pad_open.parent = open_group
    bpy.context.view_layer.update()
    rc.render_ruin_day_night(
        open_group,
        slug="charging_pad_open",
        cam_angle_deg=142.0,
        cam_elev_deg=24.0,
        res=(1024, 768),
    )

    # 2. Pad LOCKED (posed in capture only) with Rover 07 at the centre of the pad (Day C + Earthlit night)
    rc.reset_ruins_scene()
    scene_group = bpy.data.objects.new("_ChargingPadScaleScene", None)
    bpy.context.scene.collection.objects.link(scene_group)

    pad_root = build_charging_pad()
    pose_charging_pad_active(pad_root)
    pad_root.parent = scene_group

    rover_fbx = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")
    if rover_fbx.exists():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(rover_fbx.resolve()))
        rover_objs = [o for o in bpy.data.objects if o not in before]
        for o in rover_objs:
            if o.type == "MESH" and o.data is not None:
                for slot in o.material_slots:
                    if slot.material is not None:
                        base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                        if base_m is not None:
                            slot.material = base_m
            if o.parent is None:
                o.parent = scene_group
                o.location = Vector((0.0, 0.0, CHARGING_PAD_DIMS["deck_height"]))
                o.rotation_euler = Euler((0.0, 0.0, math.radians(180.0)), "XYZ")

    bpy.context.view_layer.update()
    rc.render_ruin_day_night(
        scene_group,
        slug="charging_pad_locked",
        cam_angle_deg=142.0,
        cam_elev_deg=22.0,
        res=(1024, 768),
    )


def render_charging_pad_with_rover() -> None:
    """Backward-compatible entry point that renders both open (no rover) and locked (with 07) captures."""
    render_charging_pad_open_and_locked()


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build ChargingPad FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/ChargingPad.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_charging_pad()
    tris = r1kit.count_triangles(root)
    print(f"[ChargingPad] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        r1kit.render_structure_suite(
            root=root,
            slug="charging_pad",
            out_dir=args.renders_dir,
            target_height=CHARGING_PAD_DIMS["bollard_height"],
            footprint_radius=CHARGING_PAD_DIMS["footprint_width"] * 0.52,
            pose_rest_fn=pose_charging_pad_rest,
            pose_active_fn=pose_charging_pad_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )
        render_charging_pad_open_and_locked()


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
