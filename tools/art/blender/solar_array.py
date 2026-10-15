"""Build script for the R1 SolarArray structure (`Assets/_Project/Art/R1/Models/Structures/SolarArray.fbx`)."""

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

# Single table of dimensions taken from DESIGN.md (sections 1.2, 2, and 3.2)
SOLAR_ARRAY_DIMS: dict[str, float] = {
    # Overall footprint and heights (DESIGN.md §2 & §3.2)
    "footprint_width": 2.40,
    "footprint_depth": 1.60,
    "waist_height": 1.10,
    "total_height": 1.85,
    # Moving-part ranges (TASK.md & DESIGN.md §3.2)
    "default_elevation_deg": 35.0,
    "min_elevation_deg": 10.0,
    "max_elevation_deg": 75.0,
    "wing_dihedral_deg": 5.0,
    "active_azimuth_deg": 35.0,
    "active_elevation_deg": 55.0,
    "active_wing_fold_deg": 36.0,
    # Tripod base & truss legs (Stage 1 & Stage 2)
    "footpad_radius_from_center": 0.74,
    "footpad_size": 0.28,
    "footpad_height": 0.036,
    "leg_top_radius": 0.25,
    "leg_bottom_y": 0.05,
    "leg_top_y": 0.72,
    "leg_chord_spacing": 0.095,
    "leg_strut_thick": 0.034,
    "collar_y": 0.73,
    "collar_radius": 0.33,
    "collar_height": 0.050,
    # Octagonal tracking gearbox housing (Stage 3)
    "gearbox_center_y": 0.89,
    "gearbox_width": 0.90,
    "gearbox_depth": 0.80,
    "gearbox_height": 0.30,
    "gearbox_chamfer": 0.22,
    "stripe_center_y": 0.85,
    "stripe_height": 0.072,
    # Power cable gland & socket
    "socket_power_x": -0.15,
    "socket_power_y": 0.68,
    "socket_power_z": 0.26,
    # Azimuth & Elevation joints (Stage 4)
    "azimuth_pivot_y": 1.04,
    "turntable_radius": 0.27,
    "turntable_height": 0.050,
    "elevation_pivot_y": 1.35,
    "spine_length_z": 1.14,
    # Butterfly solar wings (2x4 cell grid per wing)
    "wing_hinge_x": 0.075,
    "wing_width": 1.10,
    "wing_length": 1.30,
    "wing_thickness": 0.044,
    "wing_center_offset_z": -0.18,
    "cell_cols": 2.0,
    "cell_rows": 4.0,
    "corner_patch_size": 0.16,
}

TRIPOD_ANGLES_DEG: tuple[float, float, float] = (48.0, -48.0, 180.0)


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: Three bolted tripod footpads with clevis brackets."""
    d = SOLAR_ARRAY_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    r_foot = d["footpad_radius_from_center"]
    pad_s = d["footpad_size"]
    pad_h = d["footpad_height"]

    for ang_deg in TRIPOD_ANGLES_DEG:
        ang = math.radians(ang_deg)
        fx = r_foot * math.sin(ang)
        fz = r_foot * math.cos(ang)

        # Bevelled square footpad plate oriented along the leg ray
        mb.add_bevel_box(
            center_u=(fx, pad_h * 0.5, fz),
            size_u=(pad_s, pad_h, pad_s),
            mat="R1_GreyMetal",
            bevel=0.010,
            segments=1,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

        # Twin vertical clevis hinge lugs on top of the footpad
        perp_x = math.cos(ang)
        perp_z = -math.sin(ang)
        for side in (-1.0, 1.0):
            lx = fx + side * perp_x * (d["leg_chord_spacing"] * 0.5)
            lz = fz + side * perp_z * (d["leg_chord_spacing"] * 0.5)
            mb.add_bevel_box(
                center_u=(lx, pad_h + 0.032, lz),
                size_u=(0.032, 0.068, 0.082),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
                rot_deg_u=(0.0, ang_deg, 0.0),
            )

        # Steel clevis pin through the footpad lugs
        mb.add_cylinder(
            center_u=(fx, pad_h + 0.036, fz),
            radius=0.016,
            length=d["leg_chord_spacing"] + 0.06,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.003,
            rot_deg_u=(0.0, ang_deg, 0.0),
        )

        # 4 corner anchor bolts per footpad
        bolts: list[tuple[float, float, float]] = []
        b_off = pad_s * 0.36
        for sx in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                bx = fx + sx * b_off * math.cos(ang) + sz * b_off * math.sin(ang)
                bz = fz - sx * b_off * math.sin(ang) + sz * b_off * math.cos(ang)
                bolts.append((bx, pad_h + 0.006, bz))
        mb.add_bolts(
            bolts,
            radius=0.014,
            height=0.010,
            axis_u="Y",
            mat="R1_BareSteel",
            bevel_segments=0,
        )

    return mb.build_object(parent=root)


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Three dark-charcoal open-truss legs with zig-zag web bracing and upper waist collar."""
    d = SOLAR_ARRAY_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    r_bot = d["footpad_radius_from_center"]
    r_top = d["leg_top_radius"]
    y_bot = d["leg_bottom_y"]
    y_top = d["leg_top_y"]
    half_chord = d["leg_chord_spacing"] * 0.5
    thick = d["leg_strut_thick"]

    for ang_deg in TRIPOD_ANGLES_DEG:
        ang = math.radians(ang_deg)
        dir_x = math.sin(ang)
        dir_z = math.cos(ang)
        perp_x = math.cos(ang)
        perp_z = -math.sin(ang)

        # Left and right main chord struts of the open truss leg (matching s_solar_array.png)
        p_bot_l = (r_bot * dir_x - half_chord * perp_x, y_bot, r_bot * dir_z - half_chord * perp_z)
        p_bot_r = (r_bot * dir_x + half_chord * perp_x, y_bot, r_bot * dir_z + half_chord * perp_z)
        p_top_l = (r_top * dir_x - half_chord * perp_x, y_top, r_top * dir_z - half_chord * perp_z)
        p_top_r = (r_top * dir_x + half_chord * perp_x, y_top, r_top * dir_z + half_chord * perp_z)

        mb.add_strut(p_bot_l, p_top_l, width=thick, thickness=thick, mat="R1_GreyMetal", bevel=0.005)
        mb.add_strut(p_bot_r, p_top_r, width=thick, thickness=thick, mat="R1_GreyMetal", bevel=0.005)

        # 4 zig-zag Warren truss diagonal web bars + horizontal ties between the two chords
        n_bays = 4
        for i in range(n_bays):
            t0 = (i + 0.08) / (n_bays + 0.15)
            t1 = (i + 0.92) / (n_bays + 0.15)
            pt0_l = tuple(p_bot_l[k] * (1.0 - t0) + p_top_l[k] * t0 for k in range(3))
            pt0_r = tuple(p_bot_r[k] * (1.0 - t0) + p_top_r[k] * t0 for k in range(3))
            pt1_l = tuple(p_bot_l[k] * (1.0 - t1) + p_top_l[k] * t1 for k in range(3))
            pt1_r = tuple(p_bot_r[k] * (1.0 - t1) + p_top_r[k] * t1 for k in range(3))

            mb.add_strut(pt0_l, pt0_r, width=0.020, thickness=0.020, mat="R1_GreyMetal", bevel=0.0)
            if i % 2 == 0:
                mb.add_strut(pt0_l, pt1_r, width=0.018, thickness=0.018, mat="R1_GreyMetal", bevel=0.0)
            else:
                mb.add_strut(pt0_r, pt1_l, width=0.018, thickness=0.018, mat="R1_GreyMetal", bevel=0.0)

    # Upper tripod mounting waist collar ring
    mb.add_cylinder(
        center_u=(0.0, d["collar_y"], 0.0),
        radius=d["collar_radius"],
        length=d["collar_height"],
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=12,
        bevel=0.008,
        bevel_segments=1,
    )

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Octagonal cream-enamel gearbox housing, continuous orange stripe, dark-glass bezel, and Socket_Power_1."""
    d = SOLAR_ARRAY_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    cy = d["gearbox_center_y"]
    gw = d["gearbox_width"]
    gd = d["gearbox_depth"]
    gh = d["gearbox_height"]
    chamfer = d["gearbox_chamfer"]

    # Main octagonal cream-enamel tracking gearbox housing with generous bevels (matching s_solar_array.png)
    mb.add_oct_prism(
        center_u=(0.0, cy, 0.0),
        width_x=gw,
        height_y=gh,
        depth_z=gd,
        chamfer=chamfer,
        mat="R1_CreamEnamel",
        bevel=0.034,
        bevel_segments=1,
        top_inset=0.032,
    )

    # Single continuous horizontal warm-orange stripe (#FF8A5B) wrapping the octagonal housing
    mb.add_oct_prism(
        center_u=(0.0, d["stripe_center_y"], 0.0),
        width_x=gw + 0.014,
        height_y=d["stripe_height"],
        depth_z=gd + 0.014,
        chamfer=chamfer,
        mat="R1_OrangeStripe",
        bevel=0.006,
        bevel_segments=1,
        top_inset=0.0,
    )

    # Front recessed 3-pip charge-rate bezel on +Z face
    front_z = gd * 0.5
    mb.add_bevel_box(
        center_u=(0.0, d["stripe_center_y"], front_z + 0.008),
        size_u=(0.24, 0.076, 0.024),
        mat="R1_GreyMetal",
        bevel=0.007,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, d["stripe_center_y"], front_z + 0.014),
        size_u=(0.21, 0.050, 0.018),
        mat="R1_DarkGlass",
        bevel=0.0,
        segments=0,
    )

    # Circular sun-sensor bezel on upper front face of gearbox (matching s_solar_array.png)
    mb.add_cylinder(
        center_u=(0.0, cy + 0.055, front_z - 0.005),
        radius=0.058,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.006,
    )

    # Under-belly work-lamp collar beneath the gearbox
    mb.add_cylinder(
        center_u=(0.0, cy - gh * 0.5 - 0.012, 0.0),
        radius=0.12,
        length=0.026,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.005,
    )

    # Lower armored power cable gland receptacle on the front-right underside
    sx = d["socket_power_x"]
    sy = d["socket_power_y"]
    sz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(sx, sy + 0.04, sz - 0.03),
        radius=0.046,
        length=0.09,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=8,
        bevel=0.005,
        rot_deg_u=(35.0, 0.0, 0.0),
    )
    mb.add_cylinder(
        center_u=(sx, sy + 0.01, sz - 0.005),
        radius=0.032,
        length=0.05,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        rot_deg_u=(35.0, 0.0, 0.0),
    )

    stage_3_obj = mb.build_object(parent=root)

    # Power socket empty on the base cable gland (+Z pointing outward)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_3_obj,
        pivot_u=(sx, sy, sz),
        outward_dir_u=(0.0, -0.5, 0.866),
    )
    return stage_3_obj


def _build_wing_mesh(
    name: str,
    side: float,
    parent: bpy.types.Object,
) -> bpy.types.Object:
    """Build one foldable butterfly solar wing (`Wing_L` for side=+1, `Wing_R` for side=-1)."""
    d = SOLAR_ARRAY_DIMS
    hinge_x = side * d["wing_hinge_x"]
    hinge_y = d["elevation_pivot_y"]
    hinge_z = d["wing_center_offset_z"]

    mb = r1kit.MeshBuilder(name, pivot_u=(hinge_x, hinge_y, hinge_z))

    w_width = d["wing_width"]
    w_len = d["wing_length"]
    w_thick = d["wing_thickness"]
    cx = hinge_x + side * (w_width * 0.5 + 0.025)
    cy = hinge_y
    cz = hinge_z

    # 1. Outer bevelled dark-charcoal aluminum frame (R1_GreyMetal)
    mb.add_bevel_box(
        center_u=(cx, cy + w_thick * 0.14, cz),
        size_u=(w_width, w_thick * 0.72, w_len),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )

    # 2. Crinkled gold Kapton thermal foil backing on the underside & lower rim (R1_GoldFoil, visible along bottom lip)
    mb.add_bevel_box(
        center_u=(cx, cy - w_thick * 0.28, cz + 0.006),
        size_u=(w_width - 0.008, w_thick * 0.48, w_len + 0.006),
        mat="R1_GoldFoil",
        bevel=0.008,
        segments=1,
    )

    # 3. Hinge knuckle straps connecting wing frame to root hinge axis
    for z_knuckle in (-0.34, 0.34):
        mb.add_bevel_box(
            center_u=(hinge_x + side * 0.025, cy, cz + z_knuckle),
            size_u=(0.065, w_thick + 0.008, 0.090),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )

    # 4. 2x4 grid of photovoltaic cell tiles (R1_SolarCell) + silver busbars (R1_BareSteel)
    cols = int(d["cell_cols"])
    rows = int(d["cell_rows"])
    margin_x = 0.060
    margin_z = 0.060
    gap = 0.020
    usable_w = w_width - 2.0 * margin_x
    usable_l = w_len - 2.0 * margin_z
    cell_w = (usable_w - (cols - 1) * gap) / cols
    cell_l = (usable_l - (rows - 1) * gap) / rows

    x_start = cx - usable_w * 0.5 + cell_w * 0.5
    z_start = cz - usable_l * 0.5 + cell_l * 0.5
    cell_y = cy + w_thick * 0.5 + 0.002

    for c_idx in range(cols):
        tile_x = x_start + c_idx * (cell_w + gap)
        for r_idx in range(rows):
            tile_z = z_start + r_idx * (cell_l + gap)
            mb.add_bevel_box(
                center_u=(tile_x, cell_y, tile_z),
                size_u=(cell_w, 0.008, cell_l),
                mat="R1_SolarCell",
                bevel=0.0,
                segments=0,
                top_scale_xz=(0.985, 0.985),
            )
        # Longitudinal silver busbars running down each column of solar cells (matching s_solar_array.png)
        for bus_off in (-cell_w * 0.22, cell_w * 0.22):
            mb.add_bevel_box(
                center_u=(tile_x + bus_off, cell_y + 0.004, cz),
                size_u=(0.008, 0.004, usable_l - 0.02),
                mat="R1_BareSteel",
                bevel=0.0,
                segments=0,
            )

    # Transverse silver interconnect strips between cell rows
    for r_idx in range(rows - 1):
        bar_z = z_start + r_idx * (cell_l + gap) + (cell_l + gap) * 0.5
        mb.add_bevel_box(
            center_u=(cx, cell_y + 0.003, bar_z),
            size_u=(usable_w - 0.02, 0.004, 0.010),
            mat="R1_BareSteel",
            bevel=0.0,
            segments=0,
        )

    # 5. Salvaged copper/brass corner clip patches with steel rivets on Wing_R (side < 0, matching s_solar_array.png)
    if side < 0.0:
        outer_x = cx - w_width * 0.5
        patch_s = d["corner_patch_size"]
        for corner_sign_z in (-1.0, 1.0):
            corner_z = cz + corner_sign_z * (w_len * 0.5)
            px = outer_x + patch_s * 0.42
            pz = corner_z - corner_sign_z * (patch_s * 0.42)
            mb.add_bevel_box(
                center_u=(px, cy + 0.004, pz),
                size_u=(patch_s, w_thick + 0.016, patch_s),
                mat="R1_Copper",
                bevel=0.006,
                segments=1,
            )
            mb.add_bolts(
                [
                    (px - 0.035, cy + w_thick * 0.5 + 0.014, pz + corner_sign_z * 0.035),
                    (px + 0.035, cy + w_thick * 0.5 + 0.014, pz - corner_sign_z * 0.035),
                ],
                radius=0.011,
                height=0.008,
                axis_u="Y",
                mat="R1_BareSteel",
                bevel_segments=0,
            )

    return mb.build_object(parent=parent)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: `StatusLamp` (`R1_LampAmber`) and tracking hierarchy `Azimuth` -> `Elevation` -> `Wing_L`, `Wing_R`."""
    d = SOLAR_ARRAY_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. StatusLamp (separate mesh using ONLY R1_LampAmber)
    front_z = d["gearbox_depth"] * 0.5
    lamp_mb = r1kit.MeshBuilder("StatusLamp", pivot_u=(0.0, d["stripe_center_y"], front_z + 0.020))
    # 3 charge-rate pips inside the front bezel
    for pip_idx in (-1, 0, 1):
        lamp_mb.add_bevel_box(
            center_u=(pip_idx * 0.056, d["stripe_center_y"], front_z + 0.022),
            size_u=(0.044, 0.032, 0.012),
            mat="R1_LampAmber",
            bevel=0.0,
            segments=0,
        )
    # Upper circular sun-sensor amber lens dome
    lamp_mb.add_cylinder(
        center_u=(0.0, d["gearbox_center_y"] + 0.055, front_z + 0.008),
        radius=0.044,
        length=0.020,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=10,
        bevel=0.005,
    )
    # Under-belly work lamp emitter disc
    lamp_mb.add_cylinder(
        center_u=(0.0, d["gearbox_center_y"] - d["gearbox_height"] * 0.5 - 0.022, 0.0),
        radius=0.095,
        length=0.014,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )
    lamp_mb.build_object(parent=stage_4)

    # 2. Azimuth (turntable yaw node at (0, 1.04, 0), rotates 360 deg around Y)
    az_pivot = (0.0, d["azimuth_pivot_y"], 0.0)
    az_mb = r1kit.MeshBuilder("Azimuth", pivot_u=az_pivot)
    # Geared dark-metal turntable base ring
    az_mb.add_cylinder(
        center_u=(0.0, d["azimuth_pivot_y"] + 0.022, 0.0),
        radius=d["turntable_radius"],
        length=0.044,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=14,
        bevel=0.008,
    )
    az_mb.add_cylinder(
        center_u=(0.0, d["azimuth_pivot_y"] + 0.054, 0.0),
        radius=d["turntable_radius"] * 0.78,
        length=0.028,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.005,
    )
    # Central copper/brass azimuth mast column
    az_mb.add_cylinder(
        center_u=(0.0, d["azimuth_pivot_y"] + 0.14, 0.0),
        radius=0.082,
        length=0.17,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.006,
    )
    # Upper steel trunnion collar beneath the elevation pivot
    az_mb.add_cylinder(
        center_u=(0.0, d["elevation_pivot_y"] - 0.065, 0.0),
        radius=0.062,
        length=0.08,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=10,
        bevel=0.005,
    )
    # Offset vertical pitch-actuator jackscrew rod on the right side (matching s_solar_array.png)
    az_mb.add_cylinder(
        center_u=(-0.11, d["azimuth_pivot_y"] + 0.14, -0.03),
        radius=0.018,
        length=0.19,
        mat="R1_Copper",
        axis_u="Y",
        segments=6,
        bevel=0.0,
        bevel_segments=0,
    )
    az_obj = az_mb.build_object(parent=stage_4)

    # 3. Elevation (pitch trunnion node at (0, 1.35, -0.18), pitches 10..75 deg around X)
    el_pivot = (0.0, d["elevation_pivot_y"], 0.0)
    el_mb = r1kit.MeshBuilder("Elevation", pivot_u=el_pivot)
    # Central spine beam between the two butterfly wings
    el_mb.add_bevel_box(
        center_u=(0.0, d["elevation_pivot_y"], d["wing_center_offset_z"]),
        size_u=(0.10, 0.048, d["spine_length_z"]),
        mat="R1_GreyMetal",
        bevel=0.007,
        segments=1,
    )
    # Front and rear wing-folding barrel hinges
    for z_knuckle in (-0.34, 0.34):
        el_mb.add_cylinder(
            center_u=(0.0, d["elevation_pivot_y"] + 0.006, d["wing_center_offset_z"] + z_knuckle),
            radius=0.030,
            length=0.18,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=8,
            bevel=0.004,
        )
        el_mb.add_cylinder(
            center_u=(0.0, d["elevation_pivot_y"] + 0.006, d["wing_center_offset_z"] + z_knuckle),
            radius=0.015,
            length=0.22,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=6,
            bevel=0.0,
            bevel_segments=0,
        )
    el_obj = el_mb.build_object(parent=az_obj)

    # 4. Wing_L (+X viewer-left wing) and Wing_R (-X viewer-right wing with copper corner patches)
    _build_wing_mesh("Wing_L", side=1.0, parent=el_obj)
    _build_wing_mesh("Wing_R", side=-1.0, parent=el_obj)

    return stage_4


def pose_solar_array_rest(root: bpy.types.Object) -> None:
    """Set SolarArray to its default deployed tracking pose (35 deg elevation, slight butterfly dihedral)."""
    d = SOLAR_ARRAY_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "Azimuth":
            obj.rotation_euler = (0.0, 0.0, 0.0)
        elif obj.name == "Elevation":
            # In Blender (Y=forward, Z=up), rotating -X tilts the +Y (front) edge down toward the sun
            obj.rotation_euler = (math.radians(-d["default_elevation_deg"]), 0.0, 0.0)
        elif obj.name == "Wing_L":
            obj.rotation_euler = (0.0, math.radians(-d["wing_dihedral_deg"]), 0.0)
        elif obj.name == "Wing_R":
            obj.rotation_euler = (0.0, math.radians(d["wing_dihedral_deg"]), 0.0)


def pose_solar_array_active(root: bpy.types.Object) -> None:
    """Set SolarArray to its rotated + steep-elevation + folded-wing end pose."""
    d = SOLAR_ARRAY_DIMS
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "Azimuth":
            obj.rotation_euler = (0.0, 0.0, math.radians(-d["active_azimuth_deg"]))
        elif obj.name == "Elevation":
            obj.rotation_euler = (math.radians(-d["active_elevation_deg"]), 0.0, 0.0)
        elif obj.name == "Wing_L":
            obj.rotation_euler = (0.0, math.radians(-d["active_wing_fold_deg"]), 0.0)
        elif obj.name == "Wing_R":
            obj.rotation_euler = (0.0, math.radians(d["active_wing_fold_deg"]), 0.0)


def build_solar_array() -> bpy.types.Object:
    """Build the complete SolarArray hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("SolarArray", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_solar_array_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=SOLAR_ARRAY_DIMS["footprint_width"] * 0.52)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build SolarArray FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/SolarArray.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_solar_array()
    tris = r1kit.count_triangles(root)
    print(f"[SolarArray] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        r1kit.render_structure_suite(
            root=root,
            slug="solar_array",
            out_dir=args.renders_dir,
            target_height=SOLAR_ARRAY_DIMS["total_height"],
            footprint_radius=SOLAR_ARRAY_DIMS["footprint_width"] * 0.50,
            pose_rest_fn=pose_solar_array_rest,
            pose_active_fn=pose_solar_array_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
