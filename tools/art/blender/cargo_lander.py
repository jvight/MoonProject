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
    # Overall envelope & scale targets (height 5.2-5.8 m, footprint 6.0-7.5 m)
    "target_height": 5.58,
    "footprint_radius": 3.56,
    # Lower capsule section (continuous circular capsule flowing down to ~0.54 m skirt)
    "hull_y_bot": 0.54,
    "hull_y_mid": 2.68,
    "low_core_radius": 1.90,
    "low_panel_radius": 1.92,
    "low_panel_thick": 0.042,
    "low_stripe_y": 1.24,
    "low_stripe_h": 0.15,
    # Arched rover bay opening cut into the lower capsule (>= 1.9 m x >= 1.9 m inside)
    "bay_width": 1.94,
    "bay_height": 1.96,
    "bay_y_floor": 0.48,
    "bay_y_ceil": 2.44,
    "bay_arch_radius": 0.42,
    "bay_z_front": 1.66,
    "bay_z_back": -1.18,
    # Underbelly thermal blanket & descent engine nozzle
    "belly_y_bot": 0.36,
    "belly_radius": 1.68,
    "engine_y_center": 0.36,
    "engine_r_top": 0.38,
    "engine_r_bot": 0.62,
    "engine_len": 0.36,
    # 4 splayed landing legs (dark charcoal struts, gold foil only on upper damper sleeve, visible joints, saucer pads)
    "leg_angles_deg": (45.0, 135.0, 225.0, 315.0),
    "leg_upper_r": 1.88,
    "leg_upper_y": 2.18,
    "leg_foot_r": 3.22,
    "leg_foot_y": 0.09,
    "leg_inner_r": 1.72,
    "leg_inner_y": 0.68,
    "leg_foil_t0": 0.42,
    "leg_foil_t1": 0.66,
    "leg_brace_t": 0.68,
    "leg_foil_radius": 0.124,
    "leg_tube_radius": 0.092,
    "leg_piston_radius": 0.068,
    "footpad_radius": 0.34,
    "footpad_height": 0.09,
    # Middle cylindrical capsule section (tier 2)
    "drum_y_bot": 2.68,
    "drum_y_top": 4.42,
    "drum_core_radius": 1.78,
    "drum_panel_radius": 1.80,
    "drum_panel_thick": 0.040,
    "stripe_y": 3.48,
    "stripe_h": 0.19,
    "sage_patch_angle_deg": 46.0,
    "sage_patch_span_deg": 28.0,
    "sage_patch_h": 0.62,
    # Upper cargo hold compartment (behind CargoHatch on +Z)
    "hold_width": 1.04,
    "hold_height": 1.18,
    "hold_y_floor": 2.92,
    "hold_y_ceil": 4.10,
    "hold_z_front": 1.66,
    "hold_z_back": 0.68,
    "hatch_pivot_u": (-0.52, 3.51, 1.72),
    # Battery Crate & CrateLid inside the upper hold
    "crate_pivot_u": (0.0, 2.92, 1.14),
    "crate_size_u": (0.74, 0.44, 0.52),
    "crate_lid_pivot_u": (0.0, 3.36, 0.88),
    "crate_lid_size_u": (0.76, 0.07, 0.54),
    "crate_cell_offsets_x": (-0.21, 0.0, 0.21),
    "crate_cell_radius": 0.080,
    "crate_cell_height": 0.34,
    # Round mid-body solar wing gimbals & large Wing_L / Wing_R (each wing ~as wide as body is tall)
    "wing_mount_x": 1.80,
    "wing_pivot_x": 1.94,
    "wing_pivot_y": 3.48,
    "wing_pivot_z": 0.0,
    "wing_drum_radius": 0.52,
    "wing_drum_length": 0.34,
    "wing_panel_start": 0.42,
    "wing_panel_length": 4.20,
    "wing_panel_chord": 2.18,
    "wing_panel_thick": 0.048,
    "wing_collector_tilt_deg": 38.0,
    "wing_sub_cols": 5,
    "wing_sub_rows": 2,
    # Upper conical crew / avionics capsule section (tier 3)
    "cone_y_bot": 4.42,
    "cone_y_top": 5.38,
    "cone_r_bot": 1.78,
    "cone_r_top": 1.06,
    "cone_panel_thick": 0.038,
    "rcs_y": 4.82,
    "rcs_radius": 1.56,
    "rcs_angles_deg": (68.0, 158.0, -158.0, -68.0),
    # Top docking collar & dome cap
    "cap_y_bot": 5.38,
    "cap_y_top": 5.58,
    "cap_radius": 1.04,
    # RampDoor geometry & pose angles (1.94 m length from y=0.48 sill -> 13.6 deg slope <= 15 deg)
    "ramp_pivot_u": (0.0, 0.48, 1.66),
    "ramp_width": 1.92,
    "ramp_length": 1.94,
    "ramp_thick": 0.084,
    "ramp_open_deg": 103.6,
    "hatch_open_deg": -108.0,
    "crate_lid_open_deg": -74.0,
    "wing_fold_deg": -76.0,
    "wing_track_pitch_deg": 18.0,
    "charge_contacts_lift_y": 0.16,
    # Bay lights
    "bay_light_1_pivot_u": (0.0, 2.55, 1.81),
    "bay_light_2_pivot_u": (0.0, 2.40, 0.24),
    "bay_light_3_pivot_u": (0.92, 1.54, 0.32),
    "bay_light_4_pivot_u": (-0.92, 1.54, 0.32),
    # ChargeContacts & Bay repair parts
    "charge_contacts_pivot_u": (0.0, 0.48, 0.20),
    "bay_cable_pivot_u": (-0.88, 1.16, 0.22),
    "bay_socket_pivot_u": (-0.88, 1.30, -0.36),
    # Power sockets at the tips of the 4 downward-curving lower-capsule cable stubs
    "power_sockets_u": {
        "Socket_Power_1": (2.10, 0.58, 0.48),
        "Socket_Power_2": (2.10, 0.58, -0.48),
        "Socket_Power_3": (-2.10, 0.58, 0.48),
        "Socket_Power_4": (-2.10, 0.58, -0.48),
    },
    # Rover 07 scale stand-in
    "rover_07_width": 1.52,
    "rover_07_height": 1.55,
    "rover_07_length": 2.04,
}


def _add_lower_capsule_and_bay(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the continuous circular lower capsule, arched >= 1.9 m x 1.9 m rover bay, underbelly foil, and cable stubs."""
    y_bot = d["hull_y_bot"]
    y_mid = d["hull_y_mid"]
    hull_h = y_mid - y_bot
    hull_cy = (y_bot + y_mid) * 0.5
    core_r = d["low_core_radius"]
    panel_r = d["low_panel_radius"]
    panel_t = d["low_panel_thick"]
    bay_w = d["bay_width"]
    bay_y_floor = d["bay_y_floor"]
    bay_y_ceil = d["bay_y_ceil"]
    bay_arch_r = d["bay_arch_radius"]
    bay_z_front = d["bay_z_front"]
    bay_z_back = d["bay_z_back"]

    # Circular gold Kapton thermal blanket underbelly disk & central descent thruster nozzle bell
    belly_h = y_bot - d["belly_y_bot"]
    belly_cy = (d["belly_y_bot"] + y_bot) * 0.5
    mb.add_cylinder(
        center_u=(0.0, belly_cy, 0.0),
        radius=d["belly_radius"] * 0.92,
        radius_top=d["belly_radius"],
        length=belly_h,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=28,
        bevel=0.018,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, d["engine_y_center"] + 0.09, 0.0),
        radius=d["engine_r_top"] * 1.14,
        length=0.10,
        mat="R1_Copper",
        axis_u="Y",
        segments=20,
        bevel=0.008,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, d["engine_y_center"] - 0.05, 0.0),
        radius=d["engine_r_bot"],
        radius_top=d["engine_r_top"],
        length=d["engine_len"],
        mat="R1_BareSteel",
        axis_u="Y",
        segments=24,
        bevel=0.010,
        bevel_segments=1,
    )

    # Recessed dark cylindrical seam core across the 298 deg arc outside the +Z rover bay opening
    mb.add_curved_patch(
        center_y=hull_cy,
        height_y=hull_h - 0.04,
        cyl_radius=core_r - 0.05,
        thickness=0.06,
        angle_center_deg=180.0,
        angle_span_deg=298.0,
        mat="R1_DarkGlass",
        arc_segments=24,
        bevel=0.0,
    )

    # Smooth circular lower capsule skirt collars (y_bot ≈ 0.54 m) across the non-bay arc + full waist transition collar
    mb.add_curved_patch(
        center_y=y_bot + 0.045,
        height_y=0.09,
        cyl_radius=panel_r - 0.02,
        thickness=panel_t + 0.026,
        angle_center_deg=180.0,
        angle_span_deg=298.0,
        mat="R1_GreyMetal",
        arc_segments=24,
        bevel=0.008,
    )
    mb.add_curved_patch(
        center_y=y_bot + 0.12,
        height_y=0.09,
        cyl_radius=panel_r,
        thickness=panel_t + 0.014,
        angle_center_deg=180.0,
        angle_span_deg=298.0,
        mat="R1_CreamEnamel",
        arc_segments=24,
        bevel=0.010,
    )

    # Smooth circular waist transition collar at y_mid stepping from lower capsule (1.92 m) to middle drum (1.80 m)
    mb.add_curved_patch(
        center_y=y_mid - 0.06,
        height_y=0.12,
        cyl_radius=panel_r - 0.01,
        thickness=panel_t + 0.018,
        angle_center_deg=180.0,
        angle_span_deg=296.0,
        mat="R1_CreamEnamel",
        arc_segments=24,
        bevel=0.010,
    )
    mb.add_cylinder(
        center_u=(0.0, y_mid, 0.0),
        radius=d["drum_panel_radius"] + d["drum_panel_thick"] + 0.008,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=32,
        bevel=0.004,
    )

    # 7 curved cylindrical CreamEnamel armor panel sectors around the lower capsule (continuing seams, stripe, bolts, hatches)
    low_sector_specs = (
        (51.0, 38.0),
        (90.0, 38.0),
        (135.0, 42.0),
        (180.0, 44.0),
        (-135.0, 42.0),
        (-90.0, 38.0),
        (-51.0, 38.0),
    )
    low_band0_y0 = y_bot + 0.18
    low_band0_y1 = d["low_stripe_y"] - d["low_stripe_h"] * 0.5 - 0.015
    low_band1_y0 = d["low_stripe_y"] + d["low_stripe_h"] * 0.5 + 0.015
    low_band1_y1 = y_mid - 0.12

    for ang, span in low_sector_specs:
        mb.add_curved_patch(
            center_y=(low_band0_y0 + low_band0_y1) * 0.5,
            height_y=low_band0_y1 - low_band0_y0,
            cyl_radius=panel_r,
            thickness=panel_t,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_CreamEnamel",
            arc_segments=5,
            bevel=0.008,
        )
        # On ±90 deg sectors, the upper panel has a recessed rounded service hatch built via add_rounded_portal_ring below
        if abs(abs(ang) - 90.0) > 1.0:
            mb.add_curved_patch(
                center_y=(low_band1_y0 + low_band1_y1) * 0.5,
                height_y=low_band1_y1 - low_band1_y0,
                cyl_radius=panel_r,
                thickness=panel_t,
                angle_center_deg=ang,
                angle_span_deg=span,
                mat="R1_CreamEnamel",
                arc_segments=5,
                bevel=0.008,
            )
        mb.add_curved_patch(
            center_y=d["low_stripe_y"],
            height_y=d["low_stripe_h"],
            cyl_radius=panel_r + 0.004,
            thickness=panel_t + 0.008,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_OrangeStripe",
            arc_segments=5,
            bevel=0.004,
        )

        # Corner panel bolts along each lower-capsule armor plate
        bolt_r = panel_r + panel_t + 0.005
        half_span = span * 0.5 - 3.2
        for off_deg in (-half_span, half_span):
            brad = math.radians(ang + off_deg)
            bx = math.sin(brad) * bolt_r
            bz = math.cos(brad) * bolt_r
            for by in (low_band0_y0 + 0.08, low_band1_y1 - 0.08):
                mb.add_bolts(
                    [(bx, by, bz)],
                    radius=0.015,
                    height=0.012,
                    axis_u="Z" if abs(bz) >= abs(bx) else "X",
                    bevel_segments=0,
                )

    # Flush curved service hatches on the ±90 deg lower-capsule flanks & louver cooling grilles on ±135 deg
    side_arc_w = panel_r * math.radians(38.0)
    for side_ang in (-90.0, 90.0):
        mb.add_rounded_portal_ring(
            w_in=0.58,
            y_bot_in=1.54,
            y_top_in=2.34,
            r_bot_in=0.12,
            r_top_in=0.12,
            w_out=side_arc_w,
            y_bot_out=low_band1_y0,
            y_top_out=low_band1_y1,
            r_bot_out=0.01,
            r_top_out=0.01,
            thickness=panel_t,
            mat="R1_CreamEnamel",
            cyl_radius=panel_r,
            yaw_deg=side_ang,
            corner_segments=4,
            sub_rings=1,
            bevel=0.006,
        )
        mb.add_rounded_portal_ring(
            w_in=0.54,
            y_bot_in=1.56,
            y_top_in=2.32,
            r_bot_in=0.10,
            r_top_in=0.10,
            w_out=0.60,
            y_bot_out=1.53,
            y_top_out=2.35,
            r_bot_out=0.13,
            r_top_out=0.13,
            thickness=0.036,
            mat="R1_GreyMetal",
            cyl_radius=panel_r - 0.006,
            yaw_deg=side_ang,
            corner_segments=4,
            sub_rings=1,
            bevel=0.005,
        )
        mb.add_rounded_panel(
            width_x=0.54,
            y_bot=1.56,
            y_top=2.32,
            r_bot=0.10,
            r_top=0.10,
            thickness=panel_t,
            mat="R1_CreamEnamel",
            cyl_radius=panel_r,
            yaw_deg=side_ang,
            corner_segments=4,
            cols=4,
            bevel=0.006,
        )
        sign_x = 1.0 if side_ang > 0.0 else -1.0
        mb.add_bevel_box(
            center_u=(sign_x * (panel_r + panel_t + 0.008), 1.94, -0.17),
            size_u=(0.028, 0.16, 0.07),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )

    hatch_shell_r = panel_r + panel_t
    for vent_ang in (-135.0, 135.0):
        vrad = math.radians(vent_ang)
        vr = hatch_shell_r + 0.008
        mb.add_vent_grille(
            center_u=(math.sin(vrad) * vr, 1.94, math.cos(vrad) * vr),
            width=0.52,
            height=0.32,
            depth=0.032,
            slat_count=4,
            rot_deg_u=(0.0, vent_ang, 0.0),
        )

    # Rear lower-capsule external propellant/coolant bottles on -Z (integrated onto the curved rear capsule wall)
    for tank_x in (-0.44, 0.44):
        mb.add_cylinder(
            center_u=(tank_x, hull_cy + 0.08, -panel_r - 0.18),
            radius=0.22,
            length=1.14,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=16,
            bevel=0.016,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(tank_x, hull_cy + 0.28, -panel_r - 0.18),
            radius=0.226,
            length=0.15,
            mat="R1_OrangeStripe",
            axis_u="Y",
            segments=16,
            bevel=0.005,
        )

    # Arched rover bay portal cut directly into the front (+Z) of the circular lower capsule
    # 1. Thick R1_CreamEnamel arched portal shroud framing the rover bay entrance (embedded into ±32 deg capsule flanks)
    mb.add_rounded_portal_ring(
        w_in=bay_w + 0.04,
        y_bot_in=bay_y_floor,
        y_top_in=bay_y_ceil + 0.02,
        r_bot_in=0.05,
        r_top_in=bay_arch_r + 0.02,
        w_out=bay_w + 0.34,
        y_bot_out=bay_y_floor - 0.02,
        y_top_out=y_mid - 0.01,
        r_bot_out=0.04,
        r_top_out=0.14,
        thickness=0.34,
        mat="R1_CreamEnamel",
        z_front=bay_z_front + 0.14,
        cyl_radius=None,
        corner_segments=12,
        sub_rings=1,
        bevel=0.018,
    )

    # 2. Inner recessed R1_GreyMetal arched door-seal gasket frame (clear opening = 1.94 m wide x 1.96 m high)
    mb.add_rounded_portal_ring(
        w_in=bay_w,
        y_bot_in=bay_y_floor,
        y_top_in=bay_y_ceil,
        r_bot_in=0.04,
        r_top_in=bay_arch_r,
        w_out=bay_w + 0.09,
        y_bot_out=bay_y_floor - 0.01,
        y_top_out=bay_y_ceil + 0.05,
        r_bot_out=0.06,
        r_top_out=bay_arch_r + 0.04,
        thickness=0.28,
        mat="R1_GreyMetal",
        z_front=bay_z_front + 0.08,
        cyl_radius=None,
        corner_segments=10,
        sub_rings=1,
        bevel=0.012,
    )

    # Sill threshold beam under the arched portal entrance
    mb.add_bevel_box(
        center_u=(0.0, bay_y_floor - 0.03, bay_z_front - 0.03),
        size_u=(bay_w + 0.32, 0.08, 0.28),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )

    # Perimeter bolts following the vertical jambs and rounded arch of the bay frame
    arch_bolt_pts: list[tuple[float, float, float]] = []
    jamb_x = bay_w * 0.5 + 0.10
    bolt_z = bay_z_front + 0.14
    for sign_x in (-1.0, 1.0):
        for by in (0.72, 1.12, 1.52, 1.92):
            arch_bolt_pts.append((sign_x * jamb_x, by, bolt_z))
        arch_cx = sign_x * (bay_w * 0.5 - bay_arch_r)
        arch_cy = bay_y_ceil - bay_arch_r
        arch_r_bolt = bay_arch_r + 0.10
        for a_deg in (24.0, 62.0):
            arad = math.radians(a_deg)
            arch_bolt_pts.append(
                (
                    arch_cx + sign_x * arch_r_bolt * math.cos(arad),
                    arch_cy + arch_r_bolt * math.sin(arad),
                    bolt_z,
                )
            )
    mb.add_bolts(arch_bolt_pts, radius=0.015, height=0.012, axis_u="Z", bevel_segments=0)

    # Interior rover bay floor deck, wheel guide tracks, side bulkheads, and arched ceiling vault (all inside R_low)
    bay_depth = bay_z_front - bay_z_back
    bay_cz = (bay_z_front + bay_z_back) * 0.5
    mb.add_bevel_box(
        center_u=(0.0, bay_y_floor - 0.035, bay_cz),
        size_u=(bay_w + 0.10, 0.07, bay_depth + 0.08),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sign_x * 0.84, bay_y_floor + 0.022, bay_cz),
            size_u=(0.08, 0.044, bay_depth - 0.18),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )
        # Interior side walls closing the bay cleanly inside the cylindrical capsule
        mb.add_bevel_box(
            center_u=(sign_x * (bay_w * 0.5 + 0.04), (bay_y_floor + bay_y_ceil) * 0.5, bay_cz),
            size_u=(0.08, bay_y_ceil - bay_y_floor + 0.04, bay_depth),
            mat="R1_CreamEnamel",
            bevel=0.008,
            segments=1,
        )

    # Interior rear bulkhead wall & bay ceiling deck
    mb.add_bevel_box(
        center_u=(0.0, (bay_y_floor + bay_y_ceil) * 0.5, bay_z_back - 0.04),
        size_u=(bay_w + 0.12, bay_y_ceil - bay_y_floor + 0.06, 0.08),
        mat="R1_CreamEnamel",
        bevel=0.008,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, bay_y_ceil + 0.035, bay_cz),
        size_u=(bay_w + 0.10, 0.07, bay_depth + 0.08),
        mat="R1_CreamEnamel",
        bevel=0.008,
        segments=1,
    )

    # Arched structural bay ribs matching the rounded portal opening
    for rib_z in (-0.82, 0.0, 0.82):
        mb.add_rounded_portal_ring(
            w_in=bay_w - 0.06,
            y_bot_in=bay_y_floor + 0.01,
            y_top_in=bay_y_ceil - 0.03,
            r_bot_in=0.04,
            r_top_in=bay_arch_r - 0.02,
            w_out=bay_w + 0.02,
            y_bot_out=bay_y_floor,
            y_top_out=bay_y_ceil + 0.02,
            r_bot_out=0.05,
            r_top_out=bay_arch_r + 0.02,
            thickness=0.065,
            mat="R1_GreyMetal",
            z_front=rib_z + 0.032,
            cyl_radius=None,
            corner_segments=3,
            sub_rings=1,
            bevel=0.0,
        )

    # Interior left wall (+X) equipment locker
    mb.add_bevel_box(
        center_u=(bay_w * 0.5 - 0.07, 1.22, -0.25),
        size_u=(0.13, 0.62, 1.14),
        mat="R1_CreamEnamel",
        bevel=0.010,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(bay_w * 0.5 - 0.138, 1.22, -0.25),
        size_u=(0.02, 0.10, 1.10),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )
    # Interior right wall (-X) power bus panel & upstream trunk cable feeding Bay_CableGap / Bay_CableFixed
    mb.add_bevel_box(
        center_u=(-bay_w * 0.5 + 0.045, 1.22, -0.08),
        size_u=(0.08, 0.84, 1.62),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=1,
    )
    c_piv_x, c_piv_y, c_piv_z = d["bay_cable_pivot_u"]
    mb.add_pipe(
        p0_u=(c_piv_x, c_piv_y, bay_z_front - 0.34),
        p1_u=(c_piv_x, c_piv_y, c_piv_z + 0.24),
        radius=0.034,
        mat="R1_Rubber",
        segments=10,
    )
    # Interior rear bulkhead power transformer & copper coil
    mb.add_bevel_box(
        center_u=(0.32, 1.42, bay_z_back + 0.09),
        size_u=(0.68, 0.82, 0.18),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.44, 1.36, bay_z_back + 0.11),
        radius=0.18,
        length=0.76,
        mat="R1_Copper",
        axis_u="Y",
        segments=12,
        bevel=0.008,
    )

    # Thick power cable root glands on lower capsule flanks + short downward-curving cable stubs to Socket_Power_1..4
    for _, (sx, sy, sz) in sorted(d["power_sockets_u"].items()):
        sign_x = 1.0 if sx >= 0.0 else -1.0
        hull_x_at_z = math.sqrt(max(0.5, (panel_r + panel_t) ** 2 - sz * sz))
        gland_x = sign_x * (hull_x_at_z - 0.02)
        gland_y = 1.12
        gland_z = sz

        # Bolted GreyMetal angled bulkhead gland housing on the lower capsule wall
        mb.add_bevel_box(
            center_u=(gland_x, gland_y, gland_z),
            size_u=(0.18, 0.26, 0.24),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=1,
            rot_deg_u=(0.0, 0.0, -sign_x * 16.0),
        )
        mb.add_cylinder(
            center_u=(gland_x + sign_x * 0.08, gland_y - 0.02, gland_z),
            radius=0.088,
            radius_top=0.076,
            length=0.13,
            mat="R1_Copper",
            axis_u="X",
            segments=14,
            bevel=0.008,
            bevel_segments=1,
            rot_deg_u=(0.0, 0.0, -sign_x * 24.0),
        )

        # Smooth downward-curving thick R1_Rubber cable stub ending at Socket_Power_1..4
        cable_pts: list[tuple[float, float, float]] = []
        n_curve = 6
        p0 = (gland_x + sign_x * 0.09, gland_y - 0.03, gland_z)
        p1 = (sx + sign_x * 0.03, gland_y - 0.10, gland_z)
        p2 = (sx + sign_x * 0.04, sy + 0.22, sz)
        p3 = (sx, sy + 0.03, sz)
        for c_i in range(n_curve + 1):
            t = c_i / n_curve
            u = 1.0 - t
            pt = tuple(
                (u**3) * p0[k]
                + 3.0 * (u**2) * t * p1[k]
                + 3.0 * u * (t**2) * p2[k]
                + (t**3) * p3[k]
                for k in range(3)
            )
            cable_pts.append(pt)

        mb.add_tube_path(
            points_u=cable_pts,
            radius=0.062,
            mat="R1_Rubber",
            segments=12,
            cap_ends=True,
        )

        # Terminal coupling ferrule & socket receptacle right at the Socket_Power_1..4 empty
        mb.add_cylinder(
            center_u=(sx, sy + 0.04, sz),
            radius=0.078,
            length=0.08,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.008,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sx, sy, sz),
            radius=0.070,
            length=0.05,
            mat="R1_Copper",
            axis_u="Y",
            segments=14,
            bevel=0.006,
            bevel_segments=1,
        )


def _add_splayed_landing_legs(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the 4 dark charcoal splayed legs with gold foil only on the upper damper sleeve, visible joints, and saucer pads."""
    for ang_deg in d["leg_angles_deg"]:
        rad = math.radians(ang_deg)
        sx = math.sin(rad)
        cz = math.cos(rad)
        tx = math.cos(rad)
        tz = -math.sin(rad)

        p_up = (sx * d["leg_upper_r"], d["leg_upper_y"], cz * d["leg_upper_r"])
        p_foot = (sx * d["leg_foot_r"], d["leg_foot_y"], cz * d["leg_foot_r"])
        p_in = (sx * d["leg_inner_r"], d["leg_inner_y"], cz * d["leg_inner_r"])

        t_foil0 = d["leg_foil_t0"]
        t_foil1 = d["leg_foil_t1"]
        t_brace = d["leg_brace_t"]
        p_foil0 = tuple(p_up[k] * (1.0 - t_foil0) + p_foot[k] * t_foil0 for k in range(3))
        p_foil1 = tuple(p_up[k] * (1.0 - t_foil1) + p_foot[k] * t_foil1 for k in range(3))
        p_brace = tuple(p_up[k] * (1.0 - t_brace) + p_foot[k] * t_brace for k in range(3))

        # 1. Upper hull trunnion fairing (R1_CreamEnamel) + visible dark GreyMetal clevis yoke & BareSteel pivot pin
        mb.add_bevel_box(
            center_u=(p_up[0] - sx * 0.10, p_up[1] - 0.04, p_up[2] - cz * 0.10),
            size_u=(0.34, 0.50, 0.32),
            mat="R1_CreamEnamel",
            bevel=0.028,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
            top_scale_xz=(0.82, 0.85),
        )
        mb.add_bevel_box(
            center_u=(p_up[0] - sx * 0.01, p_up[1] - 0.02, p_up[2] - cz * 0.01),
            size_u=(0.24, 0.36, 0.28),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=p_up,
            radius=0.054,
            length=0.36,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.006,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

        # 2. Dark charcoal (R1_GreyMetal) upper primary strut & lower telescoping strut
        mb.add_pipe(
            p0_u=p_up,
            p1_u=p_brace,
            radius=d["leg_tube_radius"],
            mat="R1_GreyMetal",
            segments=14,
        )
        mb.add_pipe(
            p0_u=p_brace,
            p1_u=p_foot,
            radius=d["leg_piston_radius"],
            mat="R1_GreyMetal",
            segments=14,
        )

        # 3. Gold Kapton thermal foil wrap ONLY on the upper damper sleeve (t = 0.42..0.66)
        mb.add_foil_wrap(
            p0_u=p_foil0,
            p1_u=p_foil1,
            radius=d["leg_foil_radius"],
            rings=4,
            segments=14,
            foil_mat="R1_GoldFoil",
            band_mat="R1_Rubber",
        )

        # 4. Visible mid-leg knee / damper clevis joint at p_brace (t = 0.68) with BareSteel cross-pin
        mb.add_bevel_box(
            center_u=(p_brace[0] - sx * 0.03, p_brace[1] - 0.02, p_brace[2] - cz * 0.03),
            size_u=(0.24, 0.22, 0.26),
            mat="R1_GreyMetal",
            bevel=0.014,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=p_brace,
            radius=0.048,
            length=0.30,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.005,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

        # 5. Lower hull inner trunnion joint at p_in + dark charcoal diagonal damper strut (no gold foil)
        mb.add_bevel_box(
            center_u=(p_in[0] - sx * 0.04, p_in[1], p_in[2] - cz * 0.04),
            size_u=(0.24, 0.24, 0.22),
            mat="R1_GreyMetal",
            bevel=0.012,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_cylinder(
            center_u=p_in,
            radius=0.044,
            length=0.28,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.005,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )
        mb.add_pipe(
            p0_u=p_in,
            p1_u=p_brace,
            radius=0.048,
            mat="R1_GreyMetal",
            segments=12,
        )

        # Secondary A-frame side tie-rods flanking the central damper strut
        for side_sign in (-1.0, 1.0):
            spread = 0.26 * side_sign
            p_in_side = (p_in[0] + tx * spread, p_in[1] - 0.03, p_in[2] + tz * spread)
            p_mid_side = (p_brace[0] + tx * (spread * 0.36), p_brace[1] - 0.03, p_brace[2] + tz * (spread * 0.36))
            mb.add_pipe(
                p0_u=p_in_side,
                p1_u=p_mid_side,
                radius=0.026,
                mat="R1_GreyMetal",
                segments=8,
            )

        # 6. Smooth circular saucer footpad resting flat on y = 0.0 with visible ankle ball-joint yoke
        pad_r = d["footpad_radius"]
        pad_h = d["footpad_height"]
        fx, _, fz = p_foot
        mb.add_cylinder(
            center_u=(fx, pad_h * 0.32, fz),
            radius=pad_r,
            radius_top=pad_r * 0.92,
            length=pad_h * 0.64,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=24,
            bevel=0.012,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(fx, pad_h * 0.78, fz),
            radius=pad_r * 0.88,
            radius_top=pad_r * 0.46,
            length=pad_h * 0.44,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=24,
            bevel=0.010,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(fx, pad_h + 0.035, fz),
            radius=pad_r * 0.36,
            radius_top=pad_r * 0.28,
            length=0.08,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=16,
            bevel=0.006,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(fx, pad_h + 0.055, fz),
            radius=0.038,
            length=0.24,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.004,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )


def _add_middle_cargo_drum_and_hold(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the middle cylindrical capsule drum, orange stripe, sage patch, round wing gimbals, and upper cargo hold."""
    y_bot = d["drum_y_bot"]
    y_top = d["drum_y_top"]
    drum_h = y_top - y_bot
    drum_cy = (y_bot + y_top) * 0.5
    core_r = d["drum_core_radius"]
    panel_r = d["drum_panel_radius"]
    panel_t = d["drum_panel_thick"]

    # Recessed dark seam core across the 316 deg arc outside the +Z Upper Cargo Hold opening
    mb.add_curved_patch(
        center_y=drum_cy,
        height_y=drum_h - 0.04,
        cyl_radius=core_r - 0.06,
        thickness=0.06,
        angle_center_deg=180.0,
        angle_span_deg=316.0,
        mat="R1_DarkGlass",
        arc_segments=24,
        bevel=0.0,
    )

    # Smooth CreamEnamel structural collars at bottom and top of the middle drum
    for ring_y in (y_bot + 0.055, y_top - 0.055):
        mb.add_cylinder(
            center_u=(0.0, ring_y, 0.0),
            radius=panel_r + panel_t + 0.008,
            length=0.09,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=32,
            bevel=0.014,
            bevel_segments=1,
        )
    mb.add_cylinder(
        center_u=(0.0, y_top, 0.0),
        radius=panel_r + panel_t + 0.002,
        length=0.024,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=32,
        bevel=0.004,
    )

    # 7 smooth curved CreamEnamel armor panel sectors around the middle drum
    panel_angles = (45.0, 90.0, 135.0, 180.0, -135.0, -90.0, -45.0)
    lower_band_h = 0.66
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
            arc_segments=5,
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
            arc_segments=5,
            bevel=0.008,
        )
        mb.add_curved_patch(
            center_y=d["stripe_y"],
            height_y=d["stripe_h"],
            cyl_radius=panel_r + 0.004,
            thickness=panel_t + 0.006,
            angle_center_deg=ang,
            angle_span_deg=span,
            mat="R1_OrangeStripe",
            arc_segments=5,
            bevel=0.004,
        )

    # Corner panel rivets/bolts on the front-right (-45 deg) and front-left (+45 deg) drum panels
    bolt_r = panel_r + panel_t + 0.006
    for p_ang in (-45.0, 45.0):
        for off_deg in (-16.0, 16.0):
            brad = math.radians(p_ang + off_deg)
            for by in (y_bot + 0.16, y_bot + 0.68, y_top - 0.74, y_top - 0.16):
                mb.add_bolts(
                    [(math.sin(brad) * bolt_r, by, math.cos(brad) * bolt_r)],
                    radius=0.015,
                    height=0.012,
                    axis_u="Z",
                    bevel_segments=0,
                )

    # Bolted R1_SagePatch field-service replacement plate on the +46 deg front-left flank
    mb.add_curved_patch(
        center_y=upper_band_y,
        height_y=d["sage_patch_h"],
        cyl_radius=panel_r + 0.006,
        thickness=panel_t + 0.010,
        angle_center_deg=d["sage_patch_angle_deg"],
        angle_span_deg=d["sage_patch_span_deg"],
        mat="R1_SagePatch",
        arc_segments=5,
        bevel=0.007,
    )

    # Upper Cargo Hold recessed compartment on +Z with rounded portal surround (housing Crate behind CargoHatch)
    hold_w = d["hold_width"]
    hold_h = d["hold_height"]
    hold_y_floor = d["hold_y_floor"]
    hold_y_ceil = d["hold_y_ceil"]
    hold_cy = (hold_y_floor + hold_y_ceil) * 0.5
    hold_z_front = d["hold_z_front"]
    hold_z_back = d["hold_z_back"]
    hold_depth = hold_z_front - hold_z_back
    hold_cz = (hold_z_front + hold_z_back) * 0.5

    # Curved 0 deg (+Z) sector surround with square outer seams matching ±45 deg panels and rounded inner CargoHatch opening
    drum_sector_arc_w = panel_r * math.radians(43.2)
    mb.add_rounded_portal_ring(
        w_in=hold_w + 0.02,
        y_bot_in=hold_y_floor,
        y_top_in=hold_y_ceil,
        r_bot_in=0.14,
        r_top_in=0.14,
        w_out=drum_sector_arc_w,
        y_bot_out=y_bot + 0.09,
        y_top_out=y_top - 0.09,
        r_bot_out=0.01,
        r_top_out=0.01,
        thickness=panel_t,
        mat="R1_CreamEnamel",
        cyl_radius=panel_r,
        yaw_deg=0.0,
        corner_segments=5,
        sub_rings=1,
        bevel=0.008,
    )
    # Continuation of R1_OrangeStripe across the left and right cheeks of the 0 deg sector surround
    for sign_x in (-1.0, 1.0):
        mb.add_curved_patch(
            center_y=d["stripe_y"],
            height_y=d["stripe_h"],
            cyl_radius=panel_r + 0.004,
            thickness=panel_t + 0.006,
            angle_center_deg=sign_x * 19.2,
            angle_span_deg=4.8,
            mat="R1_OrangeStripe",
            arc_segments=2,
            bevel=0.003,
        )
    mb.add_rounded_portal_ring(
        w_in=hold_w - 0.02,
        y_bot_in=hold_y_floor + 0.02,
        y_top_in=hold_y_ceil - 0.02,
        r_bot_in=0.12,
        r_top_in=0.12,
        w_out=hold_w + 0.06,
        y_bot_out=hold_y_floor - 0.02,
        y_top_out=hold_y_ceil + 0.02,
        r_bot_out=0.15,
        r_top_out=0.15,
        thickness=0.05,
        mat="R1_GreyMetal",
        cyl_radius=panel_r - 0.012,
        yaw_deg=0.0,
        corner_segments=5,
        sub_rings=1,
        bevel=0.006,
    )

    # Interior hold floor shelf, ceiling, back wall, and side cheek walls
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

    # Prominent round mid-body solar-wing deployment gimbal drums on +X and -X
    wx = d["wing_mount_x"]
    wy = d["wing_pivot_y"]
    wz = d["wing_pivot_z"]
    w_rad = d["wing_drum_radius"]
    w_len = d["wing_drum_length"]
    for sign_x in (-1.0, 1.0):
        mb.add_cylinder(
            center_u=(sign_x * wx, wy, wz),
            radius=w_rad * 1.12,
            length=w_len * 0.54,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=24,
            bevel=0.018,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sign_x * (wx + sign_x * 0.065), wy, wz),
            radius=w_rad * 0.86,
            length=w_len * 0.64,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=24,
            bevel=0.014,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(sign_x * (wx + sign_x * 0.14), wy, wz),
            radius=w_rad * 0.50,
            length=0.08,
            mat="R1_GoldFoil",
            axis_u="X",
            segments=18,
            bevel=0.008,
            bevel_segments=1,
        )
        drum_bolts: list[tuple[float, float, float]] = []
        for b_i in range(8):
            ba = (2.0 * math.pi * b_i) / 8.0
            by = wy + math.sin(ba) * (w_rad * 0.96)
            bz = wz + math.cos(ba) * (w_rad * 0.96)
            drum_bolts.append((sign_x * (wx + sign_x * 0.085), by, bz))
        mb.add_bolts(drum_bolts, radius=0.015, height=0.012, axis_u="X", bevel_segments=0)


def _add_upper_conical_section_and_top(mb: r1kit.MeshBuilder, d: dict[str, Any]) -> None:
    """Build the smooth upper tapered conical crew/avionics section, viewports, 4 RCS thruster quads, and top docking cap."""
    y_bot = d["cone_y_bot"]
    y_top = d["cone_y_top"]
    r_bot = d["cone_r_bot"]
    r_top = d["cone_r_top"]
    cone_h = y_top - y_bot
    cone_cy = (y_bot + y_top) * 0.5

    # Smooth recessed dark frustum core inside the conical section
    mb.add_cylinder(
        center_u=(0.0, cone_cy, 0.0),
        radius=r_bot - 0.015,
        radius_top=r_top - 0.015,
        length=cone_h,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=28,
        bevel=0.0,
    )

    # 8 smooth tapered conical CreamEnamel armor panels (5 arc segments each = 40 segments around circle)
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
            arc_segments=5,
            bevel=0.008,
        )
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

    # Smooth curved dark-glass avionics / observation viewports on forward panels (-36 deg, 0 deg, +36 deg)
    vp_y_bot = y_bot + 0.26
    vp_y_top = y_bot + 0.68
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
            thickness=0.022,
            angle_center_deg=vp_ang,
            angle_span_deg=21.0,
            mat="R1_GreyMetal",
            arc_segments=4,
            bevel=0.005,
        )
        mb.add_conical_panel(
            y_bot=vp_y_bot,
            y_top=vp_y_top,
            r_bot=vp_r0 + 0.006,
            r_top=vp_r1 + 0.006,
            thickness=0.018,
            angle_center_deg=vp_ang,
            angle_span_deg=17.0,
            mat="R1_DarkGlass",
            arc_segments=4,
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
            pod_size=0.22,
            nozzle_length=0.13,
            nozzle_radius=0.048,
        )

    # Smooth circular top docking collar ring, gold thermal blanket ring, dome hatch, and comms antenna
    cap_y0 = d["cap_y_bot"]
    cap_y1 = d["cap_y_top"]
    cap_r = d["cap_radius"]
    mb.add_cylinder(
        center_u=(0.0, cap_y0 + 0.04, 0.0),
        radius=cap_r + 0.03,
        length=0.08,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=28,
        bevel=0.014,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, cap_y0 + 0.095, 0.0),
        radius=cap_r * 0.86,
        length=0.045,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=24,
        bevel=0.008,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, (cap_y0 + cap_y1) * 0.5 + 0.04, 0.0),
        radius=cap_r * 0.76,
        radius_top=cap_r * 0.58,
        length=cap_y1 - cap_y0,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=24,
        bevel=0.016,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(-0.28, cap_y1 + 0.08, -0.18),
        radius=0.028,
        radius_top=0.014,
        length=0.18,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.003,
    )


def _build_ramp_door(root: bpy.types.Object, d: dict[str, Any]) -> bpy.types.Object:
    """Build the hinged `RampDoor` with an arched top matching the rover bay opening and <= 15 deg lowered slope."""
    piv = d["ramp_pivot_u"]
    px, py, pz = piv
    rw = d["ramp_width"]
    rl = d["ramp_length"]
    rt = d["ramp_thick"]
    arch_r = d["bay_arch_radius"] - 0.02

    mb = r1kit.MeshBuilder("RampDoor", pivot_u=piv)

    # Main arched-top structural ramp slab (extends from y = py to py + rl when closed)
    mb.add_rounded_panel(
        width_x=rw,
        y_bot=py,
        y_top=py + rl,
        r_bot=0.05,
        r_top=arch_r,
        thickness=rt,
        mat="R1_GreyMetal",
        z_front=pz + rt * 0.5,
        cyl_radius=None,
        corner_segments=8,
        cols=4,
        bevel=0.014,
    )

    # Outer armoured CreamEnamel skin panels (+Z side when closed, upper panel arched to match door contour)
    mb.add_bevel_box(
        center_u=(px, py + 0.53, pz + rt * 0.5 + 0.008),
        size_u=(rw - 0.14, 0.86, 0.024),
        mat="R1_CreamEnamel",
        bevel=0.007,
        segments=1,
    )
    mb.add_rounded_panel(
        width_x=rw - 0.14,
        y_bot=py + 1.00,
        y_top=py + rl - 0.08,
        r_bot=0.05,
        r_top=max(0.12, arch_r - 0.07),
        thickness=0.024,
        mat="R1_CreamEnamel",
        z_front=pz + rt * 0.5 + 0.020,
        cyl_radius=None,
        corner_segments=8,
        cols=4,
        bevel=0.007,
    )

    # Continuation of the lower capsule R1_OrangeStripe across the RampDoor exterior
    mb.add_bevel_box(
        center_u=(px, d["low_stripe_y"], pz + rt * 0.5 + 0.024),
        size_u=(rw - 0.12, d["low_stripe_h"], 0.014),
        mat="R1_OrangeStripe",
        bevel=0.004,
        segments=1,
    )

    # Heavy 3-knuckle bottom barrel hinges along the sill axis
    for hx in (-0.66, 0.0, 0.66):
        mb.add_hinge(
            center_u=(px + hx, py + 0.02, pz + rt * 0.45),
            axis_u="X",
            length=0.24,
            knuckle_radius=0.032,
        )

    # Inner deck side wheel-guide curbs + recessed CreamEnamel tread plate (-Z side when closed, top when lowered)
    for sign_x in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(px + sign_x * (rw * 0.5 - 0.06), py + (rl - arch_r) * 0.5 + 0.04, pz - rt * 0.5 - 0.024),
            size_u=(0.10, rl - arch_r - 0.08, 0.052),
            mat="R1_GreyMetal",
            bevel=0.008,
            segments=1,
        )
    mb.add_rounded_panel(
        width_x=rw - 0.28,
        y_bot=py + 0.12,
        y_top=py + rl - 0.12,
        r_bot=0.05,
        r_top=max(0.10, arch_r - 0.12),
        thickness=0.020,
        mat="R1_CreamEnamel",
        z_front=pz - rt * 0.5 + 0.004,
        cyl_radius=None,
        corner_segments=6,
        cols=4,
        bevel=0.005,
    )

    return mb.build_object(parent=root)


def _build_solar_wing(root: bpy.types.Object, name: str, sign_x: float, d: dict[str, Any]) -> bpy.types.Object:
    """Build large `Wing_L` (+X) or `Wing_R` (-X) mounted on the round mid-body gimbal with a 2x5 solar cell array."""
    piv = (sign_x * d["wing_pivot_x"], d["wing_pivot_y"], d["wing_pivot_z"])
    px, py, pz = piv
    mb = r1kit.MeshBuilder(name, pivot_u=piv)

    # Smooth circular gimbal root hub at pivot
    mb.add_cylinder(
        center_u=(px + sign_x * 0.09, py, pz),
        radius=0.18,
        length=0.26,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=18,
        bevel=0.012,
        bevel_segments=1,
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

    def _panel_yz(local_y: float, local_z: float) -> tuple[float, float]:
        wy = py + local_y * cos_t - local_z * sin_t
        wz = pz + local_y * sin_t + local_z * cos_t
        return wy, wz

    # Heavy diagonal Y-yoke spreader struts from round gimbal hub to inner corners & mid-span of solar wing frame
    for sign_z in (-1.0, 1.0):
        corner_y, corner_z = _panel_yz(0.0, sign_z * (p_chord * 0.42))
        mb.add_strut(
            p0_u=(px + sign_x * 0.12, py, pz),
            p1_u=(px + sign_x * (p_start + 0.08), corner_y, corner_z),
            width=0.064,
            thickness=0.064,
            mat="R1_GreyMetal",
            bevel=0.006,
        )
        brace_y, brace_z = _panel_yz(-0.04, sign_z * (p_chord * 0.36))
        mb.add_pipe(
            p0_u=(px + sign_x * 0.10, py - 0.24, pz + sign_z * 0.10),
            p1_u=(px + sign_x * (p_start + p_len * 0.30), brace_y, brace_z),
            radius=0.028,
            mat="R1_BareSteel",
            segments=8,
        )

    # Main large solar wing structural frame
    mb.add_bevel_box(
        center_u=(panel_cx, py, pz),
        size_u=(p_len, p_thick, p_chord),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )
    # Underside gold Kapton thermal foil backing sheet (-Y local face of solar wing)
    back_y, back_z = _panel_yz(-p_thick * 0.5 - 0.006, 0.0)
    mb.add_bevel_box(
        center_u=(panel_cx, back_y, back_z),
        size_u=(p_len - 0.10, 0.014, p_chord - 0.10),
        mat="R1_GoldFoil",
        bevel=0.004,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )

    # 2 x 5 sub-panel grid of deep-blue R1_SolarCell modules with silver R1_BareSteel busbars on top (+Y local)
    cols = d["wing_sub_cols"]
    rows = d["wing_sub_rows"]
    margin_x = 0.08
    margin_z = 0.08
    usable_x = p_len - margin_x * 2.0
    usable_z = p_chord - margin_z * 2.0
    step_x = usable_x / cols
    step_z = usable_z / rows
    cell_w = step_x - 0.048
    cell_d = step_z - 0.048

    # Central longitudinal silver divider bar along the full wing span
    mid_bar_y, mid_bar_z = _panel_yz(p_thick * 0.5 + 0.009, 0.0)
    mb.add_bevel_box(
        center_u=(panel_cx, mid_bar_y, mid_bar_z),
        size_u=(p_len - 0.06, 0.018, 0.038),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
        rot_deg_u=(-tilt_deg, 0.0, 0.0),
    )

    for c_idx in range(cols):
        cx_local = p_start + margin_x + step_x * (c_idx + 0.5)
        cx_world = px + sign_x * cx_local
        if c_idx > 0:
            rib_x = px + sign_x * (p_start + margin_x + step_x * c_idx)
            rib_y, rib_z = _panel_yz(p_thick * 0.5 + 0.009, 0.0)
            mb.add_bevel_box(
                center_u=(rib_x, rib_y, rib_z),
                size_u=(0.036, 0.020, p_chord - 0.04),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )
        for r_idx in range(rows):
            cz_local = -usable_z * 0.5 + step_z * (r_idx + 0.5)
            cell_y, cell_z = _panel_yz(p_thick * 0.5 + 0.007, cz_local)
            mb.add_bevel_box(
                center_u=(cx_world, cell_y, cell_z),
                size_u=(cell_w, 0.016, cell_d),
                mat="R1_SolarCell",
                bevel=0.004,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )
            bus_y, bus_z = _panel_yz(p_thick * 0.5 + 0.016, cz_local)
            mb.add_bevel_box(
                center_u=(cx_world, bus_y, bus_z),
                size_u=(cell_w - 0.04, 0.007, 0.018),
                mat="R1_BareSteel",
                bevel=0.002,
                segments=1,
                rot_deg_u=(-tilt_deg, 0.0, 0.0),
            )

    return mb.build_object(parent=root)


def _build_cargo_hatch(root: bpy.types.Object, d: dict[str, Any]) -> bpy.types.Object:
    """Build curved rounded-corner `CargoHatch` covering the upper cargo hold above the bay, hinged on its -X edge."""
    piv = d["hatch_pivot_u"]
    px, py, pz = piv
    hw = d["hold_width"]
    hh = d["hold_height"]
    panel_r = d["drum_panel_radius"]
    panel_t = d["drum_panel_thick"]

    mb = r1kit.MeshBuilder("CargoHatch", pivot_u=piv)

    # Smooth curved rounded-corner outer CreamEnamel hatch door flush with the middle drum
    mb.add_rounded_panel(
        width_x=hw,
        y_bot=py - hh * 0.5,
        y_top=py + hh * 0.5,
        r_bot=0.13,
        r_top=0.13,
        thickness=panel_t + 0.008,
        mat="R1_CreamEnamel",
        cyl_radius=panel_r,
        yaw_deg=0.0,
        corner_segments=5,
        cols=4,
        bevel=0.008,
    )
    # Continuation of the horizontal R1_OrangeStripe band across the full width of CargoHatch
    hatch_span_deg = math.degrees(hw / panel_r)
    mb.add_curved_patch(
        center_y=d["stripe_y"],
        height_y=d["stripe_h"],
        cyl_radius=panel_r + 0.005,
        thickness=panel_t + 0.012,
        angle_center_deg=0.0,
        angle_span_deg=hatch_span_deg,
        mat="R1_OrangeStripe",
        arc_segments=5,
        bevel=0.004,
    )

    # Two side barrel hinges along the -X vertical hinge axis
    for hy_off in (-0.32, 0.32):
        mb.add_hinge(
            center_u=(px + 0.01, py + hy_off, pz + 0.055),
            axis_u="Y",
            length=0.18,
            knuckle_radius=0.026,
        )

    # Right-hand (+X) recessed vertical dog-latch handle slot
    mb.add_bevel_box(
        center_u=(0.36, py - 0.18, pz + 0.095),
        size_u=(0.09, 0.22, 0.038),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )

    # Curved rounded-corner inner structural backing frame on CargoHatch
    mb.add_rounded_panel(
        width_x=hw - 0.08,
        y_bot=py - hh * 0.5 + 0.04,
        y_top=py + hh * 0.5 - 0.04,
        r_bot=0.10,
        r_top=0.10,
        thickness=0.024,
        mat="R1_GreyMetal",
        cyl_radius=panel_r - 0.024,
        yaw_deg=0.0,
        corner_segments=4,
        cols=4,
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

    # 3 smooth cylindrical spare R1 battery cells seated inside the crate rack
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
            segments=12,
            bevel=0.008,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(bx, by + 0.04, cz),
            radius=cell_r + 0.004,
            length=0.06,
            mat="R1_OrangeStripe",
            axis_u="Y",
            segments=12,
            bevel=0.003,
        )
        mb.add_cylinder(
            center_u=(bx, cy + wall_t + cell_h + 0.02, cz),
            radius=cell_r * 0.52,
            length=0.04,
            mat="R1_Copper",
            axis_u="Y",
            segments=10,
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
    """Build `BayLight_1` (exterior arch crown porch light) and `BayLight_2`..`BayLight_4` (interior bay lights)."""
    lights: list[bpy.types.Object] = []

    p1 = d["bay_light_1_pivot_u"]
    mb1 = r1kit.MeshBuilder("BayLight_1", pivot_u=p1)
    mb1.add_bevel_box(
        center_u=p1,
        size_u=(0.64, 0.12, 0.12),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=2,
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
            segments=2,
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
            segments=12,
        )
        mb_gap.add_cylinder(
            center_u=(cx + 0.03, cy - 0.03, stub_z1),
            radius=0.042,
            length=0.04,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=12,
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
                segments=8,
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
        segments=14,
        bevel=0.004,
    )
    for pin_y, pin_z in ((-0.026, -0.026), (-0.026, 0.026), (0.026, 0.0)):
        mb_sock.add_cylinder(
            center_u=(sx + 0.078, sy + pin_y, sz + pin_z),
            radius=0.014,
            length=0.035,
            mat="R1_Copper",
            axis_u="X",
            segments=8,
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
        segments=14,
    )
    for collar_z in (-0.14, 0.14):
        mb_fix.add_cylinder(
            center_u=(cx, cy, cz + collar_z),
            radius=0.048,
            length=0.075,
            mat="R1_Copper",
            axis_u="Z",
            segments=14,
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
        segments=16,
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
        segments=12,
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
    _add_lower_capsule_and_bay(body_mb, d)
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
    r1kit.bake_vertex_colors(root, footprint_radius=d["footprint_radius"] * 1.45)
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
    temp_objs = r1kit._setup_preview_lighting_and_plinth(d["footprint_radius"] * 1.55)

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

    mid_y = d["target_height"] * 0.48
    pose_rest(root)
    _set_bay_repair_visibility(root, repaired=True)
    bpy.context.view_layer.update()

    # 1. Standard views: front, side, back, three_quarter
    standard_views = {
        "cargo_lander_front.png": ((0.0, mid_y + 0.30, 14.2), (0.0, mid_y, 0.0), 42.0),
        "cargo_lander_side.png": ((-12.8, mid_y + 0.25, 0.0), (0.0, mid_y, 0.0), 42.0),
        "cargo_lander_back.png": ((0.0, mid_y + 0.30, -14.2), (0.0, mid_y, 0.0), 42.0),
        "cargo_lander_three_quarter.png": ((-4.4, mid_y + 0.55, 13.0), (0.0, mid_y, 0.0), 42.0),
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
            ((0.0, 2.40, 1.76), 105.0),
            ((0.0, 2.28, 0.25), 125.0),
            ((0.76, 1.54, 0.32), 70.0),
            ((-0.76, 1.54, 0.32), 70.0),
            ((0.0, 3.65, 1.60), 80.0),
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
    r1kit._aim_camera_at(cam_obj, (-2.85, 1.92, 6.8), (0.0, 1.32, 0.55), lens_mm=44.0)
    p_ramp = out_dir / "cargo_lander_ramp_open_bay_lit.png"
    r1kit._render_to_file(p_ramp, res_x=1120, res_y=840)
    rendered.append(p_ramp)

    # 3. Scale shot with a 1.52 x 1.55 m stand-in for 07 at the ramp + concept comparison (matching opening_dawn.png)
    rover_standin = r1kit.build_rover_07_standin(
        pos_u=(1.95, 0.0, 4.35),
        rover_width=d["rover_07_width"],
        rover_height=d["rover_07_height"],
        rover_length=d["rover_07_length"],
        yaw_deg_u=-142.0,
    )
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (-3.10, 1.90, 9.8), (0.35, 2.35, 0.50), lens_mm=38.0)
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
    r1kit._aim_camera_at(cam_obj, (-4.2, mid_y + 0.55, 12.6), (0.0, mid_y, 0.0), lens_mm=42.0)
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
    r1kit._aim_camera_at(cam_obj, (0.88, 3.82, 3.55), (-0.05, 3.26, 1.18), lens_mm=44.0)
    p_hold = out_dir / "cargo_lander_hold_open_crate.png"
    r1kit._render_to_file(p_hold, res_x=1120, res_y=840)
    rendered.append(p_hold)

    # 6. Bay close-up: broken (`Bay_CableGap` + `Bay_ConnectorSocket`) vs repaired (`Bay_CableFixed` + `Bay_Connector`)
    pose_rest(root)
    children["RampDoor"].rotation_euler = r1kit.u_rot(d["ramp_open_deg"], 0.0, 0.0)
    _set_bay_repair_visibility(root, repaired=False)
    bpy.context.view_layer.update()
    r1kit._aim_camera_at(cam_obj, (0.52, 1.48, 1.54), (-0.86, 1.22, -0.08), lens_mm=42.0)
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
    r1kit._aim_camera_at(cam_obj, (-4.4, mid_y + 0.55, 13.0), (0.0, mid_y, 0.0), lens_mm=42.0)
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
