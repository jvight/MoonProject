"""Procedural generator for the R1 crew Cargo Lander (`CargoLander.fbx`).

Run standalone via Blender:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/cargo_lander.py
"""

from __future__ import annotations

import argparse
import math
from pathlib import Path
import sys
from typing import Any

import bpy
from mathutils import Euler

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit


CARGO_LANDER_DIMS: dict[str, Any] = {
    # Overall envelope & scale targets (height 5.2-5.8 m, footprint 6.0-7.5 m, wingspan <= 10.5 m)
    "target_height": 5.56,
    "footprint_radius": 3.42,
    # Lower bay hull section (tier 1)
    "hull_y_bot": 0.36,
    "hull_y_mid": 2.48,
    "hull_oct_radius": 1.66,
    "bay_width": 1.94,
    "bay_height": 1.96,
    "bay_y_floor": 0.36,
    "bay_y_ceil": 2.32,
    "bay_z_front": 1.58,
    "bay_z_back": -1.22,
    # Underbelly thermal blanket & descent engine
    "belly_y_bot": 0.20,
    "belly_oct_radius": 1.48,
    "engine_y_center": 0.24,
    "engine_r_top": 0.34,
    "engine_r_bot": 0.56,
    "engine_len": 0.34,
    # 4 splayed landing legs (collinear straight primary strut from upper clevis to ankle)
    "leg_angles_deg": (45.0, 135.0, 225.0, 315.0),
    "leg_upper_r": 1.62,
    "leg_upper_y": 2.06,
    "leg_foot_r": 3.12,
    "leg_foot_y": 0.08,
    "leg_inner_r": 1.46,
    "leg_inner_y": 0.62,
    "leg_foil_t0": 0.36,
    "leg_foil_t1": 0.74,
    "leg_brace_t": 0.58,
    "leg_foil_radius": 0.118,
    "leg_aux_foil_radius": 0.082,
    "leg_tube_radius": 0.088,
    "leg_piston_radius": 0.064,
    "footpad_radius": 0.29,
    "footpad_height": 0.08,
    # Middle cylindrical cargo drum (tier 2)
    "drum_y_bot": 2.48,
    "drum_y_top": 4.18,
    "drum_core_radius": 1.53,
    "drum_panel_radius": 1.54,
    "drum_panel_thick": 0.038,
    "stripe_y": 3.26,
    "stripe_h": 0.18,
    "sage_patch_angle_deg": 46.0,
    "sage_patch_span_deg": 28.0,
    "sage_patch_h": 0.58,
    # Upper cargo hold compartment (behind CargoHatch on +Z)
    "hold_width": 0.98,
    "hold_height": 1.18,
    "hold_y_floor": 2.78,
    "hold_y_ceil": 3.96,
    "hold_z_front": 1.42,
    "hold_z_back": 0.54,
    "hatch_pivot_u": (-0.50, 3.37, 1.58),
    # Battery Crate & CrateLid inside the upper hold
    "crate_pivot_u": (0.0, 2.78, 0.98),
    "crate_size_u": (0.72, 0.44, 0.52),
    "crate_lid_pivot_u": (0.0, 3.22, 0.72),
    "crate_lid_size_u": (0.74, 0.07, 0.54),
    "crate_cell_offsets_x": (-0.20, 0.0, 0.20),
    "crate_cell_radius": 0.078,
    "crate_cell_height": 0.34,
    # Solar wing mounts & Wing_L / Wing_R
    "wing_mount_x": 1.54,
    "wing_pivot_x": 1.68,
    "wing_pivot_y": 3.72,
    "wing_pivot_z": 0.0,
    "wing_drum_radius": 0.38,
    "wing_drum_length": 0.28,
    "wing_panel_start": 0.46,
    "wing_panel_length": 2.54,
    "wing_panel_chord": 1.24,
    "wing_panel_thick": 0.042,
    "wing_collector_tilt_deg": 32.0,
    "wing_sub_cols": 4,
    "wing_sub_rows": 2,
    # Upper conical crew / avionics module (tier 3)
    "cone_y_bot": 4.18,
    "cone_y_top": 5.26,
    "cone_r_bot": 1.52,
    "cone_r_top": 0.94,
    "cone_panel_thick": 0.036,
    "rcs_y": 4.62,
    "rcs_radius": 1.34,
    "rcs_angles_deg": (68.0, 158.0, -158.0, -68.0),
    # Top docking collar & antenna mast
    "cap_y_bot": 5.26,
    "cap_y_top": 5.42,
    "cap_radius": 0.92,
    # RampDoor geometry & pose angles
    "ramp_pivot_u": (0.0, 0.36, 1.58),
    "ramp_width": 1.92,
    "ramp_length": 1.94,
    "ramp_thick": 0.082,
    "ramp_open_deg": 101.2,
    "hatch_open_deg": -108.0,
    "crate_lid_open_deg": -74.0,
    "wing_fold_deg": 78.0,
    "wing_track_pitch_deg": 24.0,
    "charge_contacts_lift_y": 0.16,
    # Bay lights
    "bay_light_1_pivot_u": (0.0, 2.45, 1.66),
    "bay_light_2_pivot_u": (0.0, 2.30, 0.24),
    "bay_light_3_pivot_u": (0.92, 1.54, 0.32),
    "bay_light_4_pivot_u": (-0.92, 1.54, 0.32),
    # ChargeContacts & Bay repair parts
    "charge_contacts_pivot_u": (0.0, 0.36, 0.20),
    "bay_cable_pivot_u": (-0.88, 1.06, 0.22),
    "bay_socket_pivot_u": (-0.88, 1.20, -0.36),
    # Power sockets on lower hull
    "power_sockets_u": {
        "Socket_Power_1": (1.58, 0.92, 0.56),
        "Socket_Power_2": (1.58, 0.92, -0.56),
        "Socket_Power_3": (-1.58, 0.92, 0.56),
        "Socket_Power_4": (-1.58, 0.92, -0.56),
    },
    # Rover 07 scale stand-in
    "rover_07_width": 1.52,
    "rover_07_height": 1.55,
    "rover_07_length": 2.04,
}


def _add_lower_hull_and_bay(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the lower octagonal cream hull, recessed >= 1.9 m x 1.9 m rover bay, underbelly gold foil, and descent thruster."""
    y_bot = d["hull_y_bot"]
    y_mid = d["hull_y_mid"]
    hull_h = y_mid - y_bot
    hull_cy = (y_bot + y_mid) * 0.5
    bay_w = d["bay_width"]
    bay_z_front = d["bay_z_front"]
    bay_z_back = d["bay_z_back"]

    # Underbelly gold Kapton thermal blanket octagon
    belly_h = y_bot - d["belly_y_bot"]
    belly_cy = (d["belly_y_bot"] + y_bot) * 0.5
    belly_w = d["belly_oct_radius"] * 2.0
    mb.add_oct_prism(
        center_u=(0.0, belly_cy, 0.0),
        width_x=belly_w,
        height_y=belly_h,
        depth_z=belly_w,
        chamfer=belly_w * 0.28,
        mat="R1_GoldFoil",
        bevel=0.016,
        bevel_segments=1,
    )

    # Central descent thruster nozzle bell under belly
    mb.add_cylinder(
        center_u=(0.0, d["engine_y_center"] + 0.10, 0.0),
        radius=d["engine_r_top"] * 1.12,
        length=0.10,
        mat="R1_Copper",
        axis_u="Y",
        segments=14,
        bevel=0.008,
    )
    mb.add_cylinder(
        center_u=(0.0, d["engine_y_center"] - 0.04, 0.0),
        radius=d["engine_r_bot"],
        radius_top=d["engine_r_top"],
        length=d["engine_len"],
        mat="R1_BareSteel",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        bevel_segments=1,
    )

    # Lower dark base skirt ring & warm CreamEnamel waist transition ring at y_mid (matching opening_dawn.png)
    hull_w = d["hull_oct_radius"] * 2.0
    mb.add_oct_prism(
        center_u=(0.0, y_bot + 0.05, 0.0),
        width_x=hull_w,
        height_y=0.10,
        depth_z=hull_w,
        chamfer=hull_w * 0.28,
        mat="R1_GreyMetal",
        bevel=0.012,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, y_mid - 0.07, 0.0),
        radius=d["hull_oct_radius"] - 0.02,
        length=0.14,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=24,
        bevel=0.016,
        bevel_segments=1,
    )
    # Thin dark seam ring between lower bay hull and middle drum
    mb.add_cylinder(
        center_u=(0.0, y_mid, 0.0),
        radius=d["drum_panel_radius"] + 0.01,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=24,
        bevel=0.004,
    )

    # Left and right lower hull walls (leaving the central 1.94 m x 1.96 m rover bay open on +Z)
    side_wall_w = 0.22
    side_wall_x = bay_w * 0.5 + side_wall_w * 0.5
    bay_depth = bay_z_front - bay_z_back
    bay_cz = (bay_z_front + bay_z_back) * 0.5

    for sign_x in (-1.0, 1.0):
        # Primary side sheer bulkheads flanking the bay
        mb.add_bevel_box(
            center_u=(sign_x * side_wall_x, hull_cy, bay_cz),
            size_u=(side_wall_w, hull_h - 0.14, bay_depth),
            mat="R1_CreamEnamel",
            bevel=0.018,
            segments=1,
        )
        # Outer rounded cheek bays on +/- X
        mb.add_bevel_box(
            center_u=(sign_x * (side_wall_x + 0.22), hull_cy, 0.0),
            size_u=(0.24, hull_h - 0.18, 1.44),
            mat="R1_CreamEnamel",
            bevel=0.018,
            segments=1,
        )
        # Louvered side avionics cooling vents
        mb.add_vent_grille(
            center_u=(sign_x * (side_wall_x + 0.34), y_bot + 1.46, 0.0),
            width=0.52,
            height=0.30,
            depth=0.032,
            slat_count=4,
            rot_deg_u=(0.0, 90.0 if sign_x > 0 else -90.0, 0.0),
        )
        # Diagonal bevelled corner piers where the 4 splayed landing legs attach
        for sign_z in (-1.0, 1.0):
            ang_y = (45.0 if sign_z > 0 else 135.0) * sign_x
            mb.add_bevel_box(
                center_u=(sign_x * 1.12, hull_cy, sign_z * 1.12),
                size_u=(0.68, hull_h - 0.14, 0.24),
                mat="R1_CreamEnamel",
                bevel=0.016,
                segments=1,
                rot_deg_u=(0.0, ang_y, 0.0),
            )

    # Rear lower hull bulkhead closing the back of the bay (-Z)
    mb.add_bevel_box(
        center_u=(0.0, hull_cy, bay_z_back - 0.16),
        size_u=(bay_w + 0.58, hull_h - 0.14, 0.32),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=1,
    )
    for tank_x in (-0.42, 0.42):
        mb.add_cylinder(
            center_u=(tank_x, hull_cy + 0.06, bay_z_back - 0.44),
            radius=0.21,
            length=1.08,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=12,
            bevel=0.014,
        )
        mb.add_cylinder(
            center_u=(tank_x, hull_cy + 0.26, bay_z_back - 0.44),
            radius=0.216,
            length=0.14,
            mat="R1_OrangeStripe",
            axis_u="Y",
            segments=12,
            bevel=0.005,
        )

    # Front bay portal surround on +Z: sculpted CreamEnamel outer shroud + GreyMetal inner door gasket frame
    pillar_w = 0.20
    pillar_x = bay_w * 0.5 + pillar_w * 0.5
    for sign_x in (-1.0, 1.0):
        # CreamEnamel portal cheek column
        mb.add_bevel_box(
            center_u=(sign_x * pillar_x, hull_cy - 0.02, bay_z_front - 0.04),
            size_u=(pillar_w, hull_h - 0.10, 0.24),
            mat="R1_CreamEnamel",
            bevel=0.022,
            segments=1,
        )
        # Inner GreyMetal door seal jamb
        mb.add_bevel_box(
            center_u=(sign_x * (bay_w * 0.5 + 0.03), hull_cy - 0.02, bay_z_front + 0.02),
            size_u=(0.06, hull_h - 0.16, 0.16),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        mb.add_bolt_row(
            start_u=(sign_x * pillar_x, y_bot + 0.45, bay_z_front + 0.08),
            end_u=(sign_x * pillar_x, y_mid - 0.35, bay_z_front + 0.08),
            count=4,
            radius=0.015,
            height=0.012,
            axis_u="Z",
        )

    # CreamEnamel portal header beam (houses BayLight_1 above the bay opening) & GreyMetal sill
    mb.add_bevel_box(
        center_u=(0.0, y_mid - 0.08, bay_z_front - 0.03),
        size_u=(bay_w + pillar_w * 2.0, 0.16, 0.26),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, y_bot + 0.04, bay_z_front - 0.04),
        size_u=(bay_w + pillar_w * 2.0, 0.09, 0.26),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )

    # Interior bay floor deck, wheel guide tracks, warm cream ribbed walls, and rear bay diagnostic locker
    mb.add_bevel_box(
        center_u=(0.0, d["bay_y_floor"] - 0.03, bay_cz),
        size_u=(bay_w + 0.08, 0.08, bay_depth + 0.08),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sign_x * 0.84, d["bay_y_floor"] + 0.024, bay_cz),
            size_u=(0.08, 0.048, bay_depth - 0.18),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )

    mb.add_bevel_box(
        center_u=(0.0, d["bay_y_ceil"] + 0.04, bay_cz),
        size_u=(bay_w + 0.08, 0.08, bay_depth + 0.08),
        mat="R1_CreamEnamel",
        bevel=0.008,
        segments=1,
    )
    for rib_z in (-0.85, -0.18, 0.48, 1.12):
        mb.add_bevel_box(
            center_u=(0.0, d["bay_y_ceil"] - 0.025, rib_z),
            size_u=(bay_w - 0.04, 0.05, 0.07),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )
        for sign_x in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sign_x * (bay_w * 0.5 - 0.025), hull_cy, rib_z),
                size_u=(0.05, hull_h - 0.24, 0.07),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
            )

    # Interior left wall (+X) equipment locker
    mb.add_bevel_box(
        center_u=(bay_w * 0.5 - 0.08, 1.12, -0.25),
        size_u=(0.14, 0.62, 1.16),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(bay_w * 0.5 - 0.145, 1.12, -0.25),
        size_u=(0.02, 0.10, 1.12),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )
    # Interior right wall (-X) power bus panel & upstream trunk cable feeding Bay_CableGap / Bay_CableFixed
    mb.add_bevel_box(
        center_u=(-bay_w * 0.5 + 0.05, 1.12, -0.08),
        size_u=(0.08, 0.84, 1.64),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    c_piv_x, c_piv_y, c_piv_z = d["bay_cable_pivot_u"]
    mb.add_pipe(
        p0_u=(c_piv_x, c_piv_y, bay_z_front - 0.32),
        p1_u=(c_piv_x, c_piv_y, c_piv_z + 0.24),
        radius=0.034,
        mat="R1_Rubber",
        segments=8,
    )
    # Interior rear bulkhead power transformer & copper coil
    mb.add_bevel_box(
        center_u=(0.32, 1.34, bay_z_back + 0.10),
        size_u=(0.68, 0.82, 0.18),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.44, 1.28, bay_z_back + 0.12),
        radius=0.18,
        length=0.76,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.008,
    )

    # Power socket manifold housings on lower hull sides matching Socket_Power_1..4
    for _, (sx, sy, sz) in sorted(d["power_sockets_u"].items()):
        sign_x = 1.0 if sx >= 0.0 else -1.0
        mb.add_bevel_box(
            center_u=(sx - sign_x * 0.04, sy, sz),
            size_u=(0.12, 0.26, 0.26),
            mat="R1_GreyMetal",
            bevel=0.010,
            segments=1,
        )
        mb.add_cylinder(
            center_u=(sx, sy, sz),
            radius=0.082,
            length=0.08,
            mat="R1_Copper",
            axis_u="X",
            segments=10,
            bevel=0.006,
        )
        mb.add_cylinder(
            center_u=(sx + sign_x * 0.025, sy, sz),
            radius=0.048,
            length=0.05,
            mat="R1_DarkGlass",
            axis_u="X",
            segments=8,
            bevel=0.003,
        )


def _add_splayed_landing_legs(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the 4 collinear splayed landing legs with arched cream clevis mounts, gold foil wraps, and saucer footpads."""
    for ang_deg in d["leg_angles_deg"]:
        rad = math.radians(ang_deg)
        sx = math.sin(rad)
        cz = math.cos(rad)
        tx = math.cos(rad)
        tz = -math.sin(rad)

        p_up = (sx * d["leg_upper_r"], d["leg_upper_y"], cz * d["leg_upper_r"])
        p_foot = (sx * d["leg_foot_r"], d["leg_foot_y"], cz * d["leg_foot_r"])
        p_in = (sx * d["leg_inner_r"], d["leg_inner_y"], cz * d["leg_inner_r"])

        # Collinear points along the straight main leg axis from p_up to p_foot
        t_foil0 = d["leg_foil_t0"]
        t_foil1 = d["leg_foil_t1"]
        t_brace = d["leg_brace_t"]
        p_foil0 = tuple(p_up[k] * (1.0 - t_foil0) + p_foot[k] * t_foil0 for k in range(3))
        p_foil1 = tuple(p_up[k] * (1.0 - t_foil1) + p_foot[k] * t_foil1 for k in range(3))
        p_brace = tuple(p_up[k] * (1.0 - t_brace) + p_foot[k] * t_brace for k in range(3))

        # Sculpted CreamEnamel upper clevis housing + GreyMetal inner yoke (matching opening_dawn.png)
        mb.add_bevel_box(
            center_u=(p_up[0] - sx * 0.10, p_up[1] - 0.06, p_up[2] - cz * 0.10),
            size_u=(0.36, 0.56, 0.34),
            mat="R1_CreamEnamel",
            bevel=0.028,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
            top_scale_xz=(0.82, 0.85),
        )
        mb.add_bevel_box(
            center_u=(p_up[0] - sx * 0.02, p_up[1] - 0.04, p_up[2] - cz * 0.02),
            size_u=(0.24, 0.42, 0.28),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=p_up,
            radius=0.052,
            length=0.38,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.005,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

        # Continuous straight primary leg tube from upper clevis to ankle
        mb.add_pipe(
            p0_u=p_up,
            p1_u=p_foil1,
            radius=d["leg_tube_radius"],
            mat="R1_GreyMetal",
            segments=10,
        )
        mb.add_pipe(
            p0_u=p_foil1,
            p1_u=p_foot,
            radius=d["leg_piston_radius"],
            mat="R1_GreyMetal",
            segments=10,
        )

        # Gold Kapton thermal foil sleeve wrapping the middle third of the primary leg strut
        mb.add_foil_wrap(
            p0_u=p_foil0,
            p1_u=p_foil1,
            radius=d["leg_foil_radius"],
            rings=5,
            segments=10,
            foil_mat="R1_GoldFoil",
            band_mat="R1_Rubber",
        )

        # Diagonal inner damper strut from lower hull out to the leg's gold-foil section, wrapped in gold foil
        mb.add_pipe(
            p0_u=p_in,
            p1_u=p_brace,
            radius=0.042,
            mat="R1_GreyMetal",
            segments=8,
        )
        brace_f0 = tuple(p_in[k] * 0.82 + p_brace[k] * 0.18 for k in range(3))
        brace_f1 = tuple(p_in[k] * 0.16 + p_brace[k] * 0.84 for k in range(3))
        mb.add_foil_wrap(
            p0_u=brace_f0,
            p1_u=brace_f1,
            radius=d["leg_aux_foil_radius"],
            rings=4,
            segments=8,
            foil_mat="R1_GoldFoil",
            band_mat="R1_Rubber",
        )

        # Secondary A-frame side tie-rods flanking the central damper strut
        for side_sign in (-1.0, 1.0):
            spread = 0.24 * side_sign
            p_in_side = (p_in[0] + tx * spread, p_in[1] - 0.04, p_in[2] + tz * spread)
            p_mid_side = (p_brace[0] + tx * (spread * 0.35), p_brace[1] - 0.04, p_brace[2] + tz * (spread * 0.35))
            mb.add_pipe(
                p0_u=p_in_side,
                p1_u=p_mid_side,
                radius=0.024,
                mat="R1_BareSteel",
                segments=6,
            )

        # Circular saucer footpad resting flat on y = 0.0
        pad_r = d["footpad_radius"]
        pad_h = d["footpad_height"]
        fx, _, fz = p_foot
        mb.add_cylinder(
            center_u=(fx, pad_h * 0.5, fz),
            radius=pad_r,
            radius_top=pad_r * 0.78,
            length=pad_h,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.010,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(fx, pad_h + 0.025, fz),
            radius=pad_r * 0.42,
            length=0.05,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=10,
            bevel=0.005,
        )


def _add_middle_cargo_drum_and_hold(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the middle cylindrical cargo drum, thin dark seam channels, orange stripe, sage patch, and open upper cargo hold."""
    y_bot = d["drum_y_bot"]
    y_top = d["drum_y_top"]
    drum_h = y_top - y_bot
    drum_cy = (y_bot + y_top) * 0.5
    core_r = d["drum_core_radius"]
    panel_r = d["drum_panel_radius"]
    panel_t = d["drum_panel_thick"]

    # Recessed dark seam core across the 316 deg arc outside the +Z Upper Cargo Hold opening (keeps the hold unobstructed!)
    mb.add_curved_patch(
        center_y=drum_cy,
        height_y=drum_h - 0.04,
        cyl_radius=core_r - 0.06,
        thickness=0.06,
        angle_center_deg=180.0,
        angle_span_deg=316.0,
        mat="R1_DarkGlass",
        arc_segments=20,
        bevel=0.004,
    )

    # Warm CreamEnamel structural waist collars at bottom and top of the middle drum (matching opening_dawn.png)
    for ring_y in (y_bot + 0.055, y_top - 0.055):
        mb.add_cylinder(
            center_u=(0.0, ring_y, 0.0),
            radius=panel_r + panel_t + 0.008,
            length=0.09,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=24,
            bevel=0.012,
            bevel_segments=1,
        )
    # Thin dark separation seam at top of drum (y_top)
    mb.add_cylinder(
        center_u=(0.0, y_top, 0.0),
        radius=panel_r + panel_t + 0.002,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=24,
        bevel=0.004,
    )

    # Segmented curved CreamEnamel armor panels around the drum (leaving 0 deg +Z open for the Upper Cargo Hold + CargoHatch)
    panel_angles = (45.0, 90.0, 135.0, 180.0, -135.0, -90.0, -45.0)
    lower_band_h = 0.64
    lower_band_y = y_bot + 0.09 + lower_band_h * 0.5
    upper_band_h = 0.72
    upper_band_y = y_top - 0.09 - upper_band_h * 0.5

    for ang in panel_angles:
        span = 43.2
        mb.add_curved_patch(
            center_y=lower_band_y,
            height_y=lower_band_h,
            cyl_radius=panel_r,
            thickness=panel_t,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_CreamEnamel",
            arc_segments=4,
            bevel=0.008,
        )
        mb.add_curved_patch(
            center_y=upper_band_y,
            height_y=upper_band_h,
            cyl_radius=panel_r,
            thickness=panel_t,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_CreamEnamel",
            arc_segments=4,
            bevel=0.008,
        )
        # Continuous horizontal R1_OrangeStripe mission band around y = 3.26 m
        mb.add_curved_patch(
            center_y=d["stripe_y"],
            height_y=d["stripe_h"],
            cyl_radius=panel_r + 0.004,
            thickness=panel_t + 0.006,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_OrangeStripe",
            arc_segments=4,
            bevel=0.004,
        )

    # Corner panel rivets/bolts on the front-right (-45 deg) and front-left (+45 deg) drum panels (matching opening_dawn.png)
    bolt_r = panel_r + panel_t + 0.006
    for p_ang in (-45.0, 45.0):
        for off_deg in (-16.0, 16.0):
            brad = math.radians(p_ang + off_deg)
            for by in (y_bot + 0.16, y_bot + 0.66, y_top - 0.72, y_top - 0.16):
                mb.add_bolts(
                    [(math.sin(brad) * bolt_r, by, math.cos(brad) * bolt_r)],
                    radius=0.015,
                    height=0.012,
                    axis_u="Z",
                    bevel_segments=0,
                )

    # Bolted R1_SagePatch field-service replacement plate on the +46 deg front-left flank
    sage_ang = d["sage_patch_angle_deg"]
    sage_span = d["sage_patch_span_deg"]
    mb.add_curved_patch(
        center_y=upper_band_y,
        height_y=d["sage_patch_h"],
        cyl_radius=panel_r + 0.006,
        thickness=panel_t + 0.010,
        angle_center_deg=sage_ang,
        angle_span_deg=sage_span,
        mat="R1_SagePatch",
        arc_segments=4,
        bevel=0.007,
    )

    # Upper Cargo Hold recessed compartment on +Z (housing Crate behind CargoHatch)
    hold_w = d["hold_width"]
    hold_h = d["hold_height"]
    hold_y_floor = d["hold_y_floor"]
    hold_y_ceil = d["hold_y_ceil"]
    hold_cy = (hold_y_floor + hold_y_ceil) * 0.5
    hold_z_front = d["hold_z_front"]
    hold_z_back = d["hold_z_back"]
    hold_depth = hold_z_front - hold_z_back
    hold_cz = (hold_z_front + hold_z_back) * 0.5

    # CreamEnamel curved filler panels above and below the CargoHatch on the 0 deg (+Z) sector
    mb.add_curved_patch(
        center_y=(y_bot + 0.08 + hold_y_floor) * 0.5,
        height_y=hold_y_floor - (y_bot + 0.08),
        cyl_radius=panel_r,
        thickness=panel_t,
        angle_center_deg=0.0,
        angle_span_deg=43.2,
        mat="R1_CreamEnamel",
        arc_segments=4,
        bevel=0.006,
    )
    mb.add_curved_patch(
        center_y=(hold_y_ceil + y_top - 0.08) * 0.5,
        height_y=(y_top - 0.08) - hold_y_ceil,
        cyl_radius=panel_r,
        thickness=panel_t,
        angle_center_deg=0.0,
        angle_span_deg=43.2,
        mat="R1_CreamEnamel",
        arc_segments=4,
        bevel=0.006,
    )

    # Interior hold floor shelf, ceiling, back wall, and CreamEnamel side cheek surrounds
    mb.add_bevel_box(
        center_u=(0.0, hold_y_floor - 0.03, hold_cz),
        size_u=(hold_w + 0.12, 0.06, hold_depth),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, hold_y_ceil + 0.03, hold_cz),
        size_u=(hold_w + 0.12, 0.06, hold_depth),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, hold_cy, hold_z_back - 0.03),
        size_u=(hold_w + 0.12, hold_h + 0.08, 0.06),
        mat="R1_CreamEnamel",
        bevel=0.006,
        segments=1,
    )
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sign_x * (hold_w * 0.5 + 0.05), hold_cy, hold_cz),
            size_u=(0.10, hold_h + 0.06, hold_depth),
            mat="R1_CreamEnamel",
            bevel=0.010,
            segments=1,
        )
    for rail_x in (-0.26, 0.26):
        mb.add_bevel_box(
            center_u=(rail_x, hold_y_floor + 0.012, hold_cz),
            size_u=(0.05, 0.024, hold_depth - 0.12),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
        )

    # Left (+X) and Right (-X) circular solar-wing deployment gimbal drums (matching opening_dawn.png)
    wx = d["wing_mount_x"]
    wy = d["wing_pivot_y"]
    wz = d["wing_pivot_z"]
    w_rad = d["wing_drum_radius"]
    w_len = d["wing_drum_length"]
    for sign_x in (-1.0, 1.0):
        mb.add_cylinder(
            center_u=(sign_x * wx, wy, wz),
            radius=w_rad * 1.10,
            length=w_len * 0.52,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=16,
            bevel=0.014,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sign_x * (wx + sign_x * 0.055), wy, wz),
            radius=w_rad * 0.84,
            length=w_len * 0.62,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=14,
            bevel=0.012,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sign_x * (wx + sign_x * 0.12), wy, wz),
            radius=w_rad * 0.48,
            length=0.07,
            mat="R1_GoldFoil",
            axis_u="X",
            segments=12,
            bevel=0.006,
        )
        drum_bolts: list[tuple[float, float, float]] = []
        for b_i in range(8):
            ba = (2.0 * math.pi * b_i) / 8.0
            by = wy + math.sin(ba) * (w_rad * 0.95)
            bz = wz + math.cos(ba) * (w_rad * 0.95)
            drum_bolts.append((sign_x * (wx + sign_x * 0.075), by, bz))
        mb.add_bolts(drum_bolts, radius=0.015, height=0.012, axis_u="X", bevel_segments=0)


def _add_upper_conical_section_and_top(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the upper tapered conical crew/avionics section, viewports, 4 RCS thruster quads, and top docking cap."""
    y_bot = d["cone_y_bot"]
    y_top = d["cone_y_top"]
    r_bot = d["cone_r_bot"]
    r_top = d["cone_r_top"]
    cone_h = y_top - y_bot
    cone_cy = (y_bot + y_top) * 0.5

    # Recessed dark frustum core inside the conical section so narrow panel seams read cleanly
    mb.add_cylinder(
        center_u=(0.0, cone_cy, 0.0),
        radius=r_bot - 0.015,
        radius_top=r_top - 0.015,
        length=cone_h,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=24,
        bevel=0.006,
    )

    # 8 tapered trapezoidal CreamEnamel armor panels with tight 1.8 deg seam channels (matching opening_dawn.png)
    for idx in range(8):
        ang = idx * 45.0
        mb.add_conical_panel(
            y_bot=y_bot + 0.02,
            y_top=y_top - 0.02,
            r_bot=r_bot,
            r_top=r_top,
            thickness=d["cone_panel_thick"],
            angle_center_deg=ang,
            angle_span_deg=43.2,
            mat="R1_CreamEnamel",
            arc_segments=4,
            bevel=0.008,
        )
        # Lower perimeter bolts along the base of each conical panel
        for b_off in (-15.0, 15.0):
            brad = math.radians(ang + b_off)
            br = r_bot + d["cone_panel_thick"]
            mb.add_bolts(
                [(math.sin(brad) * br, y_bot + 0.11, math.cos(brad) * br)],
                radius=0.014,
                height=0.012,
                axis_u="Z",
                bevel_segments=0,
            )

    # Recessed dark-glass avionics / crew observation viewports on forward panels (-35 deg, 0 deg, +35 deg)
    vp_y_bot = y_bot + 0.30
    vp_y_top = y_bot + 0.72
    vp_t0 = (vp_y_bot - y_bot) / cone_h
    vp_t1 = (vp_y_top - y_bot) / cone_h
    vp_r0 = r_bot * (1.0 - vp_t0) + r_top * vp_t0 + d["cone_panel_thick"]
    vp_r1 = r_bot * (1.0 - vp_t1) + r_top * vp_t1 + d["cone_panel_thick"]
    for vp_ang in (-36.0, 0.0, 36.0):
        mb.add_conical_panel(
            y_bot=vp_y_bot - 0.025,
            y_top=vp_y_top + 0.025,
            r_bot=vp_r0 - 0.004,
            r_top=vp_r1 - 0.004,
            thickness=0.020,
            angle_center_deg=vp_ang,
            angle_span_deg=21.0,
            mat="R1_GreyMetal",
            arc_segments=3,
            bevel=0.005,
        )
        mb.add_conical_panel(
            y_bot=vp_y_bot,
            y_top=vp_y_top,
            r_bot=vp_r0 + 0.006,
            r_top=vp_r1 + 0.006,
            thickness=0.016,
            angle_center_deg=vp_ang,
            angle_span_deg=17.0,
            mat="R1_DarkGlass",
            arc_segments=3,
            bevel=0.003,
        )

    # 4 quadrant RCS attitude-control thruster pods
    for rcs_ang in d["rcs_angles_deg"]:
        rad = math.radians(rcs_ang)
        rx = math.sin(rad) * d["rcs_radius"]
        rz = math.cos(rad) * d["rcs_radius"]
        mb.add_thruster_quad(
            center_u=(rx, d["rcs_y"], rz),
            outward_yaw_deg=rcs_ang,
            pod_size=0.20,
            nozzle_length=0.12,
            nozzle_radius=0.044,
        )

    # Top docking collar ring, gold thermal blanket ring, dome hatch, and comms antennas
    cap_y0 = d["cap_y_bot"]
    cap_y1 = d["cap_y_top"]
    cap_r = d["cap_radius"]
    mb.add_cylinder(
        center_u=(0.0, cap_y0 + 0.035, 0.0),
        radius=cap_r + 0.03,
        length=0.07,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=20,
        bevel=0.010,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, cap_y0 + 0.085, 0.0),
        radius=cap_r * 0.86,
        length=0.04,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=16,
        bevel=0.006,
    )
    mb.add_cylinder(
        center_u=(0.0, (cap_y0 + cap_y1) * 0.5 + 0.03, 0.0),
        radius=cap_r * 0.76,
        radius_top=cap_r * 0.60,
        length=cap_y1 - cap_y0,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=16,
        bevel=0.012,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.26, cap_y1 + 0.08, -0.16),
        radius=0.028,
        radius_top=0.014,
        length=0.18,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.003,
    )


def _build_ramp_door(root: bpy.types.Object, d: dict[str, Any]) -> bpy.types.Object:
    """Build the hinged `RampDoor` that covers the 1.94 x 1.96 m rover bay and lowers to <= 15 deg."""
    piv = d["ramp_pivot_u"]
    px, py, pz = piv
    rw = d["ramp_width"]
    rl = d["ramp_length"]
    rt = d["ramp_thick"]

    mb = r1kit.MeshBuilder("RampDoor", pivot_u=piv)

    # Main structural ramp frame (extends from y = py to py + rl when closed at rotation = 0)
    mb.add_bevel_box(
        center_u=(px, py + rl * 0.5, pz),
        size_u=(rw, rl, rt),
        mat="R1_GreyMetal",
        bevel=0.014,
        segments=1,
    )
    # Outer armoured CreamEnamel skin panels (+Z side when closed)
    for seg_i, (sy0, sy1) in enumerate(((0.12, 0.94), (0.98, 1.80))):
        seg_h = sy1 - sy0
        seg_cy = py + (sy0 + sy1) * 0.5
        mb.add_bevel_box(
            center_u=(px, seg_cy, pz + rt * 0.5 + 0.008),
            size_u=(rw - 0.14, seg_h, 0.022),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        if seg_i == 0:
            mb.add_bevel_box(
                center_u=(px, py + 0.40, pz + rt * 0.5 + 0.016),
                size_u=(rw - 0.12, 0.13, 0.012),
                mat="R1_OrangeStripe",
                bevel=0.004,
                segments=1,
            )

    # Leading-edge tapered toe transition lip at top of door (touches ground when lowered)
    mb.add_bevel_box(
        center_u=(px, py + rl - 0.05, pz - 0.01),
        size_u=(rw - 0.04, 0.12, rt * 0.86),
        mat="R1_BareSteel",
        bevel=0.008,
        segments=1,
        top_scale_xz=(0.98, 0.35),
    )

    # Heavy 3-knuckle bottom barrel hinges along the sill axis
    for hx in (-0.66, 0.0, 0.66):
        mb.add_hinge(
            center_u=(px + hx, py + 0.02, pz + rt * 0.45),
            axis_u="X",
            length=0.24,
            knuckle_radius=0.032,
        )

    # Inner deck side wheel-guide curbs + twin recessed CreamEnamel tread plates (matching opening_dawn.png)
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(px + sign_x * (rw * 0.5 - 0.06), py + rl * 0.5, pz - rt * 0.5 - 0.025),
            size_u=(0.10, rl - 0.12, 0.055),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
    for sy0, sy1 in ((0.14, 0.94), (1.00, 1.80)):
        mb.add_bevel_box(
            center_u=(px, py + (sy0 + sy1) * 0.5, pz - rt * 0.5 - 0.008),
            size_u=(rw - 0.28, sy1 - sy0, 0.018),
            mat="R1_CreamEnamel",
            bevel=0.006,
            segments=1,
        )

    return mb.build_object(parent=root)


def _build_solar_wing(root: bpy.types.Object, name: str, sign_x: float, d: dict[str, Any]) -> bpy.types.Object:
    """Build `Wing_L` (+X) or `Wing_R` (-X) with root yoke, diagonal spreader struts, and tilted 2x4 solar cell array."""
    piv = (sign_x * d["wing_pivot_x"], d["wing_pivot_y"], d["wing_pivot_z"])
    px, py, pz = piv
    mb = r1kit.MeshBuilder(name, pivot_u=piv)

    # Gimbal root hub at pivot
    mb.add_cylinder(
        center_u=(px + sign_x * 0.08, py, pz),
        radius=0.14,
        length=0.22,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=12,
        bevel=0.010,
    )

    p_start = d["wing_panel_start"]
    p_len = d["wing_panel_length"]
    p_chord = d["wing_panel_chord"]
    p_thick = d["wing_panel_thick"]
    tilt_deg = d["wing_collector_tilt_deg"]
    tilt_rad = math.radians(tilt_deg)
    cos_t = math.cos(tilt_rad)
    sin_t = math.sin(tilt_rad)
    panel_cx = px + sign_x * (p_start + p_len * 0.5)

    # Helper to convert panel-local (y_normal, z_chord) offsets into tilted world Unity coordinates
    def _panel_yz(local_y: float, local_z: float) -> tuple[float, float]:
        wy = py + local_y * cos_t - local_z * sin_t
        wz = pz + local_y * sin_t + local_z * cos_t
        return wy, wz

    # Diagonal Y-yoke spreader struts from root hub to inner corners of tilted solar wing frame
    for sign_z in (-1.0, 1.0):
        corner_y, corner_z = _panel_yz(0.0, sign_z * (p_chord * 0.42))
        mb.add_strut(
            p0_u=(px + sign_x * 0.12, py, pz),
            p1_u=(px + sign_x * (p_start + 0.06), corner_y, corner_z),
            width=0.052,
            thickness=0.052,
            mat="R1_GreyMetal",
            bevel=0.005,
        )
        brace_y, brace_z = _panel_yz(-0.03, sign_z * (p_chord * 0.34))
        mb.add_pipe(
            p0_u=(px + sign_x * 0.08, py - 0.18, pz + sign_z * 0.08),
            p1_u=(px + sign_x * (p_start + p_len * 0.32), brace_y, brace_z),
            radius=0.022,
            mat="R1_BareSteel",
            segments=6,
        )

    # Main rectangular solar wing structural frame tilted toward +Z so cells read clearly from front/3-quarter views
    mb.add_bevel_box(
        center_u=(panel_cx, py, pz),
        size_u=(p_len, p_thick, p_chord),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )
    # Underside gold Kapton thermal foil backing sheet (-Y local face of solar wing)
    back_y, back_z = _panel_yz(-p_thick * 0.5 - 0.005, 0.0)
    mb.add_bevel_box(
        center_u=(panel_cx, back_y, back_z),
        size_u=(p_len - 0.08, 0.012, p_chord - 0.08),
        mat="R1_GoldFoil",
        bevel=0.003,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )

    # 2 x 4 sub-panel grid of deep-blue R1_SolarCell modules with silver R1_BareSteel busbars on top (+Y local)
    cols = d["wing_sub_cols"]
    rows = d["wing_sub_rows"]
    margin_x = 0.07
    margin_z = 0.06
    usable_x = p_len - margin_x * 2.0
    usable_z = p_chord - margin_z * 2.0
    step_x = usable_x / cols
    step_z = usable_z / rows
    cell_w = step_x - 0.042
    cell_d = step_z - 0.042

    # Central longitudinal silver divider bar along the wing span
    mid_bar_y, mid_bar_z = _panel_yz(p_thick * 0.5 + 0.008, 0.0)
    mb.add_bevel_box(
        center_u=(panel_cx, mid_bar_y, mid_bar_z),
        size_u=(p_len - 0.04, 0.016, 0.032),
        mat="R1_BareSteel",
        bevel=0.003,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )

    for c_idx in range(cols):
        cx_local = p_start + margin_x + step_x * (c_idx + 0.5)
        cx_world = px + sign_x * cx_local
        if c_idx > 0:
            rib_x = px + sign_x * (p_start + margin_x + step_x * c_idx)
            rib_y, rib_z = _panel_yz(p_thick * 0.5 + 0.008, 0.0)
            mb.add_bevel_box(
                center_u=(rib_x, rib_y, rib_z),
                size_u=(0.032, 0.018, p_chord - 0.02),
                mat="R1_BareSteel",
                bevel=0.003,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )
        for r_idx in range(rows):
            cz_local = -usable_z * 0.5 + step_z * (r_idx + 0.5)
            cell_y, cell_z = _panel_yz(p_thick * 0.5 + 0.006, cz_local)
            mb.add_bevel_box(
                center_u=(cx_world, cell_y, cell_z),
                size_u=(cell_w, 0.014, cell_d),
                mat="R1_SolarCell",
                bevel=0.003,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )
            bus_y, bus_z = _panel_yz(p_thick * 0.5 + 0.014, cz_local)
            mb.add_bevel_box(
                center_u=(cx_world, bus_y, bus_z),
                size_u=(cell_w - 0.03, 0.006, 0.016),
                mat="R1_BareSteel",
                bevel=0.002,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )

    return mb.build_object(parent=root)


def _build_cargo_hatch(root: bpy.types.Object, d: dict[str, Any]) -> bpy.types.Object:
    """Build `CargoHatch` covering the upper cargo hold above the bay, hinged on its -X vertical edge."""
    piv = d["hatch_pivot_u"]
    px, py, pz = piv
    hh = d["hold_height"]

    mb = r1kit.MeshBuilder("CargoHatch", pivot_u=piv)

    # Curved outer CreamEnamel hatch door flush with the middle drum
    mb.add_curved_patch(
        center_y=py,
        height_y=hh,
        cyl_radius=d["drum_panel_radius"],
        thickness=d["drum_panel_thick"] + 0.008,
        angle_center_deg=0.0,
        angle_span_deg=35.5,
        mat="R1_CreamEnamel",
        arc_segments=4,
        bevel=0.008,
    )
    # Continuation of the horizontal R1_OrangeStripe band across the full width of CargoHatch
    mb.add_curved_patch(
        center_y=d["stripe_y"],
        height_y=d["stripe_h"],
        cyl_radius=d["drum_panel_radius"] + 0.005,
        thickness=d["drum_panel_thick"] + 0.012,
        angle_center_deg=0.0,
        angle_span_deg=35.5,
        mat="R1_OrangeStripe",
        arc_segments=4,
        bevel=0.004,
    )

    # Two side barrel hinges along the -X vertical hinge axis (matching opening_dawn.png)
    for hy_off in (-0.34, 0.34):
        mb.add_hinge(
            center_u=(px + 0.03, py + hy_off, pz - 0.03),
            axis_u="Y",
            length=0.18,
            knuckle_radius=0.026,
        )

    # Right-hand (+X) recessed vertical dog-latch handle slot
    mb.add_bevel_box(
        center_u=(0.35, py - 0.18, pz + 0.008),
        size_u=(0.09, 0.22, 0.036),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )

    # Curved inner structural backing frame on CargoHatch (never pokes through the curved front skin)
    mb.add_curved_patch(
        center_y=py,
        height_y=hh - 0.08,
        cyl_radius=d["drum_panel_radius"] - 0.024,
        thickness=0.024,
        angle_center_deg=0.0,
        angle_span_deg=33.5,
        mat="R1_GreyMetal",
        arc_segments=4,
        bevel=0.005,
    )

    return mb.build_object(parent=root)


def _build_crate_and_lid(root: bpy.types.Object, d: dict[str, Any]) -> tuple[bpy.types.Object, bpy.types.Object]:
    """Build `Crate` and its hinged child `CrateLid` inside the upper cargo hold, containing 3 spare battery cells."""
    c_piv = d["crate_pivot_u"]
    cx, cy, cz = c_piv
    cw, ch, cd = d["crate_size_u"]

    mb = r1kit.MeshBuilder("Crate", pivot_u=c_piv)

    wall_t = 0.045
    mb.add_bevel_box(
        center_u=(cx, cy + wall_t * 0.5, cz),
        size_u=(cw, wall_t, cd),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    for sign_z in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(cx, cy + ch * 0.5, cz + sign_z * (cd * 0.5 - wall_t * 0.5)),
            size_u=(cw, ch, wall_t),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(cx, cy + ch * 0.52, cz + sign_z * (cd * 0.5 + 0.006)),
            size_u=(cw - 0.06, 0.075, 0.014),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(cx + sign_x * (cw * 0.5 - wall_t * 0.5), cy + ch * 0.5, cz),
            size_u=(wall_t, ch, cd - wall_t * 2.0),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(cx + sign_x * (cw * 0.5 + 0.018), cy + ch * 0.55, cz),
            size_u=(0.036, 0.05, 0.22),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
        )

    # 3 cylindrical spare R1 battery cells seated inside the crate rack
    cell_r = d["crate_cell_radius"]
    cell_h = d["crate_cell_height"]
    for off_x in d["crate_cell_offsets_x"]:
        bx = cx + off_x
        by = cy + wall_t + cell_h * 0.5
        mb.add_cylinder(
            center_u=(bx, by, cz),
            radius=cell_r,
            length=cell_h,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=10,
            bevel=0.008,
        )
        mb.add_cylinder(
            center_u=(bx, by + 0.04, cz),
            radius=cell_r + 0.004,
            length=0.06,
            mat="R1_OrangeStripe",
            axis_u="Y",
            segments=10,
            bevel=0.003,
        )
        mb.add_cylinder(
            center_u=(bx, cy + wall_t + cell_h + 0.02, cz),
            radius=cell_r * 0.52,
            length=0.04,
            mat="R1_Copper",
            axis_u="Y",
            segments=8,
            bevel=0.004,
        )

    crate_obj = mb.build_object(parent=root)

    # Hinged CrateLid parented to Crate
    l_piv = d["crate_lid_pivot_u"]
    lx, ly, lz = l_piv
    lw, lh, ld = d["crate_lid_size_u"]
    lid_mb = r1kit.MeshBuilder("CrateLid", pivot_u=l_piv)
    lid_mb.add_bevel_box(
        center_u=(lx, ly + lh * 0.5, lz + ld * 0.5),
        size_u=(lw, lh, ld),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=1,
    )
    for cl_x in (-0.22, 0.22):
        lid_mb.add_bevel_box(
            center_u=(lx + cl_x, ly + lh * 0.35, lz + ld + 0.012),
            size_u=(0.08, 0.055, 0.028),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )
    lid_obj = lid_mb.build_object(parent=crate_obj)
    return crate_obj, lid_obj


def _build_bay_lights(root: bpy.types.Object, d: dict[str, Any]) -> list[bpy.types.Object]:
    """Build `BayLight_1` (exterior header porch light) and `BayLight_2`..`BayLight_4` (interior bay lights)."""
    lights: list[bpy.types.Object] = []

    p1 = d["bay_light_1_pivot_u"]
    mb1 = r1kit.MeshBuilder("BayLight_1", pivot_u=p1)
    mb1.add_bevel_box(
        center_u=p1,
        size_u=(0.64, 0.12, 0.12),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb1.add_bevel_box(
        center_u=(p1[0], p1[1] - 0.005, p1[2] + 0.048),
        size_u=(0.54, 0.075, 0.036),
        mat="R1_LampAmber",
        bevel=0.006,
        segments=1,
    )
    lights.append(mb1.build_object(parent=root))

    p2 = d["bay_light_2_pivot_u"]
    mb2 = r1kit.MeshBuilder("BayLight_2", pivot_u=p2)
    mb2.add_bevel_box(
        center_u=p2,
        size_u=(0.74, 0.07, 0.24),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    mb2.add_bevel_box(
        center_u=(p2[0], p2[1] - 0.028, p2[2]),
        size_u=(0.64, 0.032, 0.16),
        mat="R1_LampAmber",
        bevel=0.005,
        segments=1,
    )
    lights.append(mb2.build_object(parent=root))

    for name, p_wall, sign_x in (
        ("BayLight_3", d["bay_light_3_pivot_u"], 1.0),
        ("BayLight_4", d["bay_light_4_pivot_u"], -1.0),
    ):
        mbw = r1kit.MeshBuilder(name, pivot_u=p_wall)
        mbw.add_bevel_box(
            center_u=p_wall,
            size_u=(0.08, 0.16, 0.42),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
        mbw.add_bevel_box(
            center_u=(p_wall[0] - sign_x * 0.032, p_wall[1], p_wall[2]),
            size_u=(0.036, 0.095, 0.34),
            mat="R1_LampAmber",
            bevel=0.005,
            segments=1,
        )
        lights.append(mbw.build_object(parent=root))

    return lights


def _build_charge_contacts(root: bpy.types.Object, d: dict[str, Any]) -> bpy.types.Object:
    """Build `ChargeContacts` in the bay floor that rise to meet Rover 07's belly contacts."""
    piv = d["charge_contacts_pivot_u"]
    px, py, pz = piv
    mb = r1kit.MeshBuilder("ChargeContacts", pivot_u=piv)

    mb.add_bevel_box(
        center_u=(px, py + 0.035, pz),
        size_u=(0.64, 0.05, 0.56),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(px, py + 0.068, pz),
        size_u=(0.68, 0.022, 0.60),
        mat="R1_OrangeStripe",
        bevel=0.004,
        segments=1,
    )
    for sx in (-0.18, 0.18):
        mb.add_strut(
            p0_u=(px + sx, py + 0.01, pz - 0.18),
            p1_u=(px + sx, py + 0.085, pz + 0.18),
            width=0.036,
            thickness=0.028,
            mat="R1_GreyMetal",
            bevel=0.003,
        )
        mb.add_bevel_box(
            center_u=(px + sx, py + 0.092, pz),
            size_u=(0.18, 0.042, 0.42),
            mat="R1_Copper",
            bevel=0.008,
            segments=1,
        )

    return mb.build_object(parent=root)


def _build_bay_repair_parts(root: bpy.types.Object, d: dict[str, Any]) -> dict[str, bpy.types.Object]:
    """Build the broken (`Bay_CableGap`, `Bay_ConnectorSocket`) and repaired (`Bay_CableFixed`, `Bay_Connector`) bay parts."""
    c_piv = d["bay_cable_pivot_u"]
    cx, cy, cz = c_piv
    s_piv = d["bay_socket_pivot_u"]
    sx, sy, sz = s_piv
    parts: dict[str, bpy.types.Object] = {}

    # 1. Bay_CableGap: severed power trunk cable with frayed copper/steel strands and a clear missing gap
    mb_gap = r1kit.MeshBuilder("Bay_CableGap", pivot_u=c_piv)
    for sign_z in (-1.0, 1.0):
        stub_z0 = cz + sign_z * 0.24
        stub_z1 = cz + sign_z * 0.11
        mb_gap.add_pipe(
            p0_u=(cx, cy, stub_z0),
            p1_u=(cx + 0.03, cy - 0.03, stub_z1),
            radius=0.036,
            mat="R1_Rubber",
            segments=8,
        )
        mb_gap.add_cylinder(
            center_u=(cx + 0.03, cy - 0.03, stub_z1),
            radius=0.042,
            length=0.04,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.004,
        )
        for strand_dy, strand_dx, mat_name in (
            (0.028, 0.045, "R1_Copper"),
            (-0.024, 0.058, "R1_Copper"),
            (0.0, 0.068, "R1_BareSteel"),
        ):
            mb_gap.add_pipe(
                p0_u=(cx + 0.03, cy - 0.03, stub_z1),
                p1_u=(cx + 0.03 + strand_dx, cy - 0.03 + strand_dy, cz + sign_z * 0.04),
                radius=0.011,
                mat=mat_name,
                segments=6,
            )
    parts["Bay_CableGap"] = mb_gap.build_object(parent=root)

    # 2. Bay_ConnectorSocket: empty unmated bulkhead receptacle with exposed 3-pin copper socket
    mb_sock = r1kit.MeshBuilder("Bay_ConnectorSocket", pivot_u=s_piv)
    mb_sock.add_bevel_box(
        center_u=(sx, sy, sz),
        size_u=(0.10, 0.26, 0.26),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb_sock.add_bevel_box(
        center_u=(sx + 0.045, sy, sz),
        size_u=(0.03, 0.22, 0.22),
        mat="R1_OrangeStripe",
        bevel=0.005,
        segments=1,
    )
    mb_sock.add_cylinder(
        center_u=(sx + 0.06, sy, sz),
        radius=0.072,
        length=0.05,
        mat="R1_DarkGlass",
        axis_u="X",
        segments=10,
        bevel=0.004,
    )
    for pin_y, pin_z in ((-0.026, -0.026), (-0.026, 0.026), (0.026, 0.0)):
        mb_sock.add_cylinder(
            center_u=(sx + 0.078, sy + pin_y, sz + pin_z),
            radius=0.014,
            length=0.035,
            mat="R1_Copper",
            axis_u="X",
            segments=6,
            bevel=0.002,
        )
    parts["Bay_ConnectorSocket"] = mb_sock.build_object(parent=root)

    # 3. Bay_CableFixed: intact heavy replacement cable bridging the gap with copper splice collars and wall clamp
    mb_fix = r1kit.MeshBuilder("Bay_CableFixed", pivot_u=c_piv)
    mb_fix.add_pipe(
        p0_u=(cx, cy, cz - 0.25),
        p1_u=(cx, cy, cz + 0.25),
        radius=0.038,
        mat="R1_Rubber",
        segments=10,
    )
    for collar_z in (-0.14, 0.14):
        mb_fix.add_cylinder(
            center_u=(cx, cy, cz + collar_z),
            radius=0.048,
            length=0.075,
            mat="R1_Copper",
            axis_u="Z",
            segments=10,
            bevel=0.006,
        )
    mb_fix.add_bevel_box(
        center_u=(cx - 0.01, cy, cz),
        size_u=(0.095, 0.11, 0.09),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    parts["Bay_CableFixed"] = mb_fix.build_object(parent=root)

    # 4. Bay_Connector: mated heavy industrial plug seated into the socket with copper collar & lead-in cable
    mb_con = r1kit.MeshBuilder("Bay_Connector", pivot_u=s_piv)
    mb_con.add_bevel_box(
        center_u=(sx, sy, sz),
        size_u=(0.10, 0.26, 0.26),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb_con.add_cylinder(
        center_u=(sx + 0.09, sy, sz),
        radius=0.078,
        length=0.11,
        mat="R1_Copper",
        axis_u="X",
        segments=12,
        bevel=0.008,
    )
    mb_con.add_bevel_box(
        center_u=(sx + 0.14, sy, sz + 0.04),
        size_u=(0.10, 0.13, 0.16),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb_con.add_pipe(
        p0_u=(sx + 0.13, sy, sz + 0.11),
        p1_u=(cx, cy, cz - 0.24),
        radius=0.036,
        mat="R1_Rubber",
        segments=8,
    )
    parts["Bay_Connector"] = mb_con.build_object(parent=root)

    return parts


def pose_rest(root: bpy.types.Object) -> None:
    """Reset `CargoLander` and all child parts to their canonical zero-rotation rest pose."""
    d = CARGO_LANDER_DIMS
    children = {o.name: o for o in r1kit.collect_hierarchy(root)}

    for obj in children.values():
        obj.rotation_euler = (0.0, 0.0, 0.0)
        obj.hide_render = False
        obj.hide_viewport = False

    if "RampDoor" in children:
        children["RampDoor"].location = r1kit.u_pos(*d["ramp_pivot_u"])
    if "CargoHatch" in children:
        children["CargoHatch"].location = r1kit.u_pos(*d["hatch_pivot_u"])
    if "CrateLid" in children:
        cx, cy, cz = d["crate_pivot_u"]
        lx, ly, lz = d["crate_lid_pivot_u"]
        children["CrateLid"].location = r1kit.u_pos(lx - cx, ly - cy, lz - cz)
    if "ChargeContacts" in children:
        children["ChargeContacts"].location = r1kit.u_pos(*d["charge_contacts_pivot_u"])
    if "Bay_CableFixed" in children:
        children["Bay_CableFixed"].location = r1kit.u_pos(*d["bay_cable_pivot_u"])
    if "Bay_Connector" in children:
        children["Bay_Connector"].location = r1kit.u_pos(*d["bay_socket_pivot_u"])


def _prepare_ao_bake_pose(root: bpy.types.Object) -> None:
    """Translate doors forward and separate overlapping swap parts during Cycles AO baking so interiors and doors bake cleanly."""
    d = CARGO_LANDER_DIMS
    children = {o.name: o for o in r1kit.collect_hierarchy(root)}
    if "RampDoor" in children:
        rx, ry, rz = d["ramp_pivot_u"]
        children["RampDoor"].location = r1kit.u_pos(rx, ry + 0.45, rz + 2.20)
    if "CargoHatch" in children:
        hx, hy, hz = d["hatch_pivot_u"]
        children["CargoHatch"].location = r1kit.u_pos(hx, hy, hz + 2.20)
    if "CrateLid" in children:
        cx, cy, cz = d["crate_pivot_u"]
        lx, ly, lz = d["crate_lid_pivot_u"]
        children["CrateLid"].location = r1kit.u_pos(lx - cx, (ly - cy) + 0.35, (lz - cz) + 0.65)
    if "Bay_CableFixed" in children:
        cx, cy, cz = d["bay_cable_pivot_u"]
        children["Bay_CableFixed"].location = r1kit.u_pos(cx + 0.32, cy, cz)
    if "Bay_Connector" in children:
        sx, sy, sz = d["bay_socket_pivot_u"]
        children["Bay_Connector"].location = r1kit.u_pos(sx + 0.32, sy, sz)
    bpy.context.view_layer.update()


def build_asset() -> bpy.types.Object:
    """Build the complete `CargoLander` hierarchy and bake vertex colours."""
    d = CARGO_LANDER_DIMS
    root = r1kit.create_empty("CargoLander", pivot_u=(0.0, 0.0, 0.0))

    # Static merged lander body
    body_mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.0, 0.0))
    _add_lower_hull_and_bay(body_mb, d)
    _add_splayed_landing_legs(body_mb, d)
    _add_middle_cargo_drum_and_hold(body_mb, d)
    _add_upper_conical_section_and_top(body_mb, d)
    body_mb.build_object(parent=root)

    # Moving & separate parts
    _build_ramp_door(root, d)
    _build_solar_wing(root, "Wing_L", 1.0, d)
    _build_solar_wing(root, "Wing_R", -1.0, d)
    _build_cargo_hatch(root, d)
    _build_crate_and_lid(root, d)
    _build_bay_lights(root, d)
    _build_charge_contacts(root, d)
    _build_bay_repair_parts(root, d)

    # Power socket empties
    for sock_name, sock_pos_u in sorted(d["power_sockets_u"].items()):
        r1kit.create_empty(sock_name, parent=root, pivot_u=sock_pos_u)

    # Bake vertex colours with doors separated so both exterior and interior faces receive clean AO
    _prepare_ao_bake_pose(root)
    r1kit.bake_vertex_colors(root, footprint_radius=d["footprint_radius"] * 1.35)
    pose_rest(root)
    bpy.context.view_layer.update()
    return root


def _set_bay_repair_visibility(root: bpy.types.Object, repaired: bool) -> None:
    """Toggle render visibility between broken (`Bay_CableGap`, `Bay_ConnectorSocket`) and repaired (`Bay_CableFixed`, `Bay_Connector`)."""
    children = {o.name: o for o in r1kit.collect_hierarchy(root)}
    for broken_name in ("Bay_CableGap", "Bay_ConnectorSocket"):
        if broken_name in children:
            children[broken_name].hide_render = repaired
    for fixed_name in ("Bay_CableFixed", "Bay_Connector"):
        if fixed_name in children:
            children[fixed_name].hide_render = not repaired


def render_cargo_lander_suite(root: bpy.types.Object, out_dir: Path) -> list[Path]:
    """Render all required orthographic/hero views, functional state shots, vertex-colour grid, and concept comparison."""
    d = CARGO_LANDER_DIMS
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    rendered: list[Path] = []

    r1kit.ensure_materials(use_vertex_color_shading=True)
    temp_objs = r1kit._setup_preview_lighting_and_plinth(d["footprint_radius"] * 1.38)

    # Warm dawn sun lighting matching opening_dawn.png (shining from front-left onto the front face)
    for tob in temp_objs:
        if tob.name == "_KeySun":
            tob.rotation_euler = Euler((math.radians(-52.0), 0.0, math.radians(-26.0)), "XYZ")
            tob.data.energy = 4.2
        elif tob.name == "_EarthFill":
            tob.rotation_euler = Euler((math.radians(-58.0), 0.0, math.radians(42.0)), "XYZ")
            tob.data.energy = 1.65

    cam_data = bpy.data.cameras.new("_LanderCam")
    cam_obj = bpy.data.objects.new("_LanderCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.append(cam_obj)

    mid_y = d["target_height"] * 0.47
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    bpy.context.view_layer.update()

    # 1. Standard views: front, side, back, three_quarter
    standard_views = {
        "cargo_lander_front.png": ((0.0, mid_y + 0.25, 11.6), (0.0, mid_y, 0.0), 45.0),
        "cargo_lander_side.png": ((-11.4, mid_y + 0.25, 0.0), (0.0, mid_y, 0.0), 45.0),
        "cargo_lander_back.png": ((0.0, mid_y + 0.25, -11.6), (0.0, mid_y, 0.0), 45.0),
        "cargo_lander_three_quarter.png": ((-3.6, mid_y + 0.45, 10.6), (0.0, mid_y, 0.0), 45.0),
    }
    for fname, (c_pos, c_tgt, lens) in standard_views.items():
        r1kit._aim_camera_at(cam_obj, c_pos, c_tgt, lens_mm=lens)
        p = out_dir / fname
        r1kit._render_to_file(p, res_x=1120, res_y=840)
        rendered.append(p)

    # 2. Ramp lowered with the bay lit (warm interior point lights + raised ChargeContacts)
    children = {o.name: o for o in r1kit.collect_hierarchy(root)}
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    children["RampDoor"].rotation_euler = r1kit.u_rot(d["ramp_open_deg"], 0.0, 0.0)
    cc_x, cc_y, cc_z = d["charge_contacts_pivot_u"]
    children["ChargeContacts"].location = r1kit.u_pos(cc_x, cc_y + d["charge_contacts_lift_y"], cc_z)

    bay_lamp_objs: list[bpy.types.Object] = []
    for l_idx, (l_pos_u, l_energy) in enumerate(
        (
            ((0.0, 2.30, 1.68), 95.0),
            ((0.0, 2.18, 0.25), 115.0),
            ((0.76, 1.48, 0.32), 65.0),
            ((-0.76, 1.48, 0.32), 65.0),
            ((0.0, 3.55, 1.45), 75.0),
        )
    ):
        pt_data = bpy.data.lights.new(name=f"_BayPt_{l_idx}", type="POINT")
        pt_data.energy = l_energy
        pt_data.color = (1.0, 0.68, 0.28)
        pt_data.shadow_soft_size = 0.25
        pt_obj = bpy.data.objects.new(f"_BayPt_{l_idx}", pt_data)
        bpy.context.collection.objects.link(pt_obj)
        pt_obj.location = r1kit.u_pos(*l_pos_u)
        bay_lamp_objs.append(pt_obj)

    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (-2.8, 1.85, 6.4), (0.0, 1.22, 0.55), lens_mm=44.0)
    p_ramp = out_dir / "cargo_lander_ramp_open_bay_lit.png"
    r1kit._render_to_file(p_ramp, res_x=1120, res_y=840)
    rendered.append(p_ramp)

    # 3. Scale shot with a 1.52 x 1.55 m stand-in for 07 at the ramp + concept comparison (matching opening_dawn.png)
    rover_standin = r1kit.build_rover_07_standin(
        pos_u=(1.85, 0.0, 4.15),
        rover_width=d["rover_07_width"],
        rover_height=d["rover_07_height"],
        rover_length=d["rover_07_length"],
        yaw_deg_u=-142.0,
    )
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (-2.65, 1.82, 8.8), (0.35, 2.22, 0.50), lens_mm=40.0)
    p_scale = out_dir / "cargo_lander_scale_07.png"
    r1kit._render_to_file(p_scale, res_x=1120, res_y=840)
    rendered.append(p_scale)

    concept_ref = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\opening_dawn.png")
    if concept_ref.exists():
        p_compare = out_dir / "compare_concept.png"
        r1kit.compose_side_by_side(concept_ref, p_scale, p_compare, target_height=840)
        rendered.append(p_compare)

    rover_mesh = rover_standin.data
    bpy.data.objects.remove(rover_standin, do_unlink=True)
    bpy.data.meshes.remove(rover_mesh)

    # 4. Wings folded vs tracking (side-by-side comparison + individual shots)
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    children["Wing_L"].rotation_euler = r1kit.u_rot(0.0, 0.0, -d["wing_fold_deg"])
    children["Wing_R"].rotation_euler = r1kit.u_rot(0.0, 0.0, d["wing_fold_deg"])
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (-3.8, mid_y + 0.5, 10.4), (0.0, mid_y, 0.0), lens_mm=45.0)
    p_wings_folded = out_dir / "cargo_lander_wings_folded.png"
    r1kit._render_to_file(p_wings_folded, res_x=960, res_y=720)
    rendered.append(p_wings_folded)

    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    children["Wing_L"].rotation_euler = r1kit.u_rot(d["wing_track_pitch_deg"], 0.0, 10.0)
    children["Wing_R"].rotation_euler = r1kit.u_rot(d["wing_track_pitch_deg"], 0.0, -10.0)
    bpy.context.view_layer.update()
    p_wings_tracking = out_dir / "cargo_lander_wings_tracking.png"
    r1kit._render_to_file(p_wings_tracking, res_x=960, res_y=720)
    rendered.append(p_wings_tracking)

    p_wings_comp = out_dir / "cargo_lander_wings_compare.png"
    r1kit.compose_side_by_side(p_wings_folded, p_wings_tracking, p_wings_comp, target_height=720)
    rendered.append(p_wings_comp)

    # 5. Cargo hold open with Crate & CrateLid open showing the 3 spare battery cells
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    children["CargoHatch"].rotation_euler = r1kit.u_rot(0.0, d["hatch_open_deg"], 0.0)
    children["CrateLid"].rotation_euler = r1kit.u_rot(d["crate_lid_open_deg"], 0.0, 0.0)
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (0.85, 3.65, 3.25), (-0.05, 3.12, 1.02), lens_mm=44.0)
    p_hold = out_dir / "cargo_lander_hold_open_crate.png"
    r1kit._render_to_file(p_hold, res_x=1120, res_y=840)
    rendered.append(p_hold)

    # 6. Bay close-up: broken (`Bay_CableGap` + `Bay_ConnectorSocket`) vs repaired (`Bay_CableFixed` + `Bay_Connector`)
    pose_rest(root)
    children["RampDoor"].rotation_euler = r1kit.u_rot(d["ramp_open_deg"], 0.0, 0.0)
    _set_bay_repair_visibility(root, repaired=False)
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (0.52, 1.38, 1.48), (-0.86, 1.12, -0.08), lens_mm=42.0)
    p_bay_broken = out_dir / "cargo_lander_bay_broken.png"
    r1kit._render_to_file(p_bay_broken, res_x=960, res_y=720)
    rendered.append(p_bay_broken)

    _set_bay_repair_visibility(root, repaired=True)
    bpy.context.view_layer.update()
    p_bay_repaired = out_dir / "cargo_lander_bay_repaired.png"
    r1kit._render_to_file(p_bay_repaired, res_x=960, res_y=720)
    rendered.append(p_bay_repaired)

    p_bay_comp = out_dir / "cargo_lander_bay_compare.png"
    r1kit.compose_side_by_side(p_bay_broken, p_bay_repaired, p_bay_comp, target_height=720)
    rendered.append(p_bay_comp)

    # Remove temporary bay point lights before rendering vertex-colour sheet
    for pt_obj in bay_lamp_objs:
        pt_data = pt_obj.data
        bpy.data.objects.remove(pt_obj, do_unlink=True)
        bpy.data.lights.remove(pt_data)

    # 7. Vertex-colour 2x2 inspection sheet (R=AO, G=Wear, B=Cavity, RGB)
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (-3.6, mid_y + 0.45, 10.6), (0.0, mid_y, 0.0), lens_mm=45.0)
    p_vcol = out_dir / "cargo_lander_vertex_color.png"
    r1kit.render_vertex_color_grid(root, p_vcol, res_x=640, res_y=480)
    rendered.append(p_vcol)

    # Restore all objects to rest pose & full visibility
    pose_rest(root)
    bpy.context.view_layer.update()

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    plinth_mat = bpy.data.materials.get("_PreviewPlinthMat")
    if plinth_mat is not None:
        bpy.data.materials.remove(plinth_mat)

    r1kit.ensure_materials(use_vertex_color_shading=False)
    return rendered


def generate(
    fbx_out: Path | None = None,
    render_dir: Path | None = None,
    do_renders: bool = True,
) -> bpy.types.Object:
    """Clean scene, build `CargoLander`, export FBX, and optionally render the diagnostic & showcase suite."""
    r1kit.reset_scene()
    root = build_asset()

    fbx_path = Path(fbx_out) if fbx_out is not None else Path("Assets/_Project/Art/R1/Models/Lander/CargoLander.fbx")
    r1kit.export_fbx(root, fbx_path)
    tris = r1kit.count_triangles(root)
    print(f"[CargoLander] Exported {fbx_path} ({tris} triangles)")

    if do_renders:
        out_renders = Path(render_dir) if render_dir is not None else Path("Logs/renders")
        render_cargo_lander_suite(root, out_renders)
        print(f"[CargoLander] Rendered suite to {out_renders}")

    return root


def main(argv: list[str] | None = None) -> None:
    """CLI entry point for building CargoLander.fbx and diagnostic/showcase renders."""
    parser = argparse.ArgumentParser(description="Build CargoLander FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Lander/CargoLander.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    generate(
        fbx_out=args.fbx_out,
        render_dir=args.renders_dir,
        do_renders=not args.skip_renders,
    )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
