"""Generate Deliverable 5 R1 story props:
- Assets/_Project/Art/R1/Models/Props/DeadAntennaMast.fbx
- Assets/_Project/Art/R1/Models/Props/SignalPillar.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_dead_antenna_and_signal.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import antenna_mast  # noqa: E402
import r1kit  # noqa: E402
import relay_repair  # noqa: E402
import socket_rack  # noqa: E402


PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"


def _build_bent_weathered_dish(az_obj: bpy.types.Object) -> bpy.types.Object:
    """Build a weathered, drooped/bent parabolic dish with a snapped feed-horn strut for DeadAntennaMast."""
    d = antenna_mast.ANTENNA_MAST_DIMS
    ax = d["dish_az_x"]
    py = d["dish_piv_y"]
    az = d["dish_az_z"]

    dish_mb = r1kit.MeshBuilder("Dish", pivot_u=(ax, py, az))
    r_max = d["dish_diameter"] * 0.5  # 0.475 m
    depth_z = d["dish_depth"]         # 0.18 m
    apex_z = az + 0.045

    # Helper that rotates dish coordinates around (ax, py, az) into a drooped, storm/impact-bent posture
    # (+28 deg downward droop around X, -14 deg yaw twist around Y, +11 deg roll around Z)
    pitch_rad = math.radians(28.0)
    yaw_rad = math.radians(-14.0)
    roll_rad = math.radians(11.0)
    cp, sp = math.cos(pitch_rad), math.sin(pitch_rad)
    cy, sy = math.cos(yaw_rad), math.sin(yaw_rad)
    cr, sr = math.cos(roll_rad), math.sin(roll_rad)

    def _transform_bent_u(pt_u: Tuple[float, float, float], warp: float = 0.0) -> Tuple[float, float, float]:
        dx = pt_u[0] - ax
        dy = pt_u[1] - py
        dz = pt_u[2] - apex_z + 0.045
        # Roll in XY
        x1 = dx * cr - dy * sr
        y1 = dx * sr + dy * cr
        z1 = dz + warp
        # Pitch downward around X (tilts +Z forward/down toward -Y)
        x2 = x1
        y2 = y1 * cp - z1 * sp
        z2 = y1 * sp + z1 * cp
        # Slight yaw around Y
        x3 = x2 * cy + z2 * sy
        y3 = y2
        z3 = -x2 * sy + z2 * cy
        return (ax + x3, py + y3, az + z3)

    # Rear mounting hub & weathered steel boss
    dish_mb.add_cylinder(
        center_u=_transform_bent_u((ax, py, apex_z - 0.015)),
        radius=0.085,
        length=0.042,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.005,
        rot_deg_u=(28.0, -14.0, 11.0),
    )
    dish_mb.add_cylinder(
        center_u=_transform_bent_u((ax, py, apex_z + 0.012)),
        radius=0.042,
        length=0.028,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
        rot_deg_u=(28.0, -14.0, 11.0),
    )

    n_ribs = 12
    ring_radii = (r_max * 0.45, r_max * 0.74, r_max)
    ring_mats = ("R1_BareSteel", "R1_BareSteel", "R1_GreyMetal")
    ring_thick = (0.012, 0.014, 0.022)

    def _dish_pt(r: float, theta_rad: float, rib_idx: int) -> Tuple[float, float, float]:
        u = r / r_max
        z_off = depth_z * (u * u)
        # Dent/warp the upper-left sector of the dish rim (ribs 2, 3, 4) so the dish silhouette is visibly bent
        warp_z = 0.0
        r_eff = r
        if rib_idx in (2, 3, 4) and u > 0.55:
            dent_strength = 0.095 if rib_idx == 3 else 0.055
            warp_z = -dent_strength * ((u - 0.45) / 0.55)
            r_eff = r * (0.90 if rib_idx == 3 else 0.94)
        raw_u = (
            ax + r_eff * math.cos(theta_rad),
            py + r_eff * math.sin(theta_rad),
            apex_z + z_off,
        )
        return _transform_bent_u(raw_u, warp=warp_z)

    # 12 radial ribs
    for i in range(n_ribs):
        th = (2.0 * math.pi * i) / n_ribs
        p0 = _dish_pt(0.072, th, i)
        p1 = _dish_pt(ring_radii[1], th, i)
        p2 = _dish_pt(r_max, th, i)
        dish_mb.add_strut(p0, p1, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)
        dish_mb.add_strut(p1, p2, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)

    # 3 concentric 12-sided polygon hoops
    for r_ring, m_ring, t_ring in zip(ring_radii, ring_mats, ring_thick):
        for i in range(n_ribs):
            i_next = (i + 1) % n_ribs
            th0 = (2.0 * math.pi * i) / n_ribs
            th1 = (2.0 * math.pi * i_next) / n_ribs
            dish_mb.add_strut(
                _dish_pt(r_ring, th0, i),
                _dish_pt(r_ring, th1, i_next),
                t_ring,
                t_ring,
                m_ring,
                bevel=0.0,
            )

    # Off-kilter feed horn (one of the 3 tripod struts is snapped and bent aside)
    focal_u = _transform_bent_u((ax - 0.03, py - 0.04, apex_z + 0.31))
    for idx_f, (angle_deg, rib_i) in enumerate(((90.0, 3), (210.0, 7), (330.0, 11))):
        th_f = math.radians(angle_deg)
        p_base = _dish_pt(ring_radii[1], th_f, rib_i)
        if idx_f == 0:
            # Snapped upper strut hanging loose
            snapped_tip = _transform_bent_u((ax + 0.08, py + 0.06, apex_z + 0.21))
            dish_mb.add_strut(p_base, snapped_tip, 0.012, 0.012, "R1_BareSteel", bevel=0.0)
        else:
            dish_mb.add_strut(p_base, focal_u, 0.012, 0.012, "R1_BareSteel", bevel=0.0)

    dish_mb.add_cylinder(
        center_u=focal_u,
        radius=0.044,
        radius_top=0.028,
        length=0.056,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
        rot_deg_u=(34.0, -18.0, 15.0),
    )

    return dish_mb.build_object(parent=az_obj)


def _build_dead_mast_lattice(stage_4: bpy.types.Object) -> bpy.types.Object:
    """Build the single-mesh dark-charcoal lattice `Mast` (`Stage_4/Mast`) for `DeadAntennaMast`."""
    d = antenna_mast.ANTENNA_MAST_DIMS
    y0 = d["mast_base_y"]       # 1.55 m
    y1 = d["mast_top_y"]        # 2.68 m
    hw0 = d["mast_base_half_w"] # 0.145 m
    hw1 = d["mast_top_half_w"]  # 0.072 m

    def hw_at(y: float) -> float:
        return hw0 + ((y - y0) / (y1 - y0)) * (hw1 - hw0)

    mb = r1kit.MeshBuilder("Mast", pivot_u=(0.0, y0, 0.0))
    antenna_mast._add_mast_lattice_span(mb, y0, y1, n_bays=4, hw_fn=hw_at, sign_offset=0)

    # Central copper coaxial feed line running up the center of the lattice mast
    mb.add_cylinder(
        center_u=(0.0, 0.5 * (y0 + y1), 0.0),
        radius=0.018,
        length=(y1 - y0),
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Viewer-left (+X) outrigger truss arm supporting DishAzimuth at (0.44, 2.12, 0.08)
    dax = d["dish_az_x"]
    day = d["dish_az_y"]
    daz = d["dish_az_z"]
    mb.add_strut(
        p0_u=(0.10, day - 0.02, 0.0),
        p1_u=(dax, day - 0.02, daz),
        width=0.036,
        thickness=0.036,
        mat="R1_GreyMetal",
        bevel=0.004,
    )
    mb.add_strut(
        p0_u=(0.12, day - 0.24, 0.0),
        p1_u=(dax - 0.02, day - 0.03, daz),
        width=0.024,
        thickness=0.024,
        mat="R1_GreyMetal",
        bevel=0.0,
    )
    mb.add_cylinder(
        center_u=(dax, day - 0.015, daz),
        radius=0.056,
        length=0.034,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    # Viewer-right (-X) horizontal brass boom supporting SonarLoop at (-0.38, 2.28, 0.0)
    sy_s = d["sonar_y"]
    mb.add_cylinder(
        center_u=(-0.09, sy_s, 0.0),
        radius=0.036,
        length=0.055,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    mb.add_cylinder(
        center_u=(-0.23, sy_s, 0.0),
        radius=0.016,
        length=0.28,
        mat="R1_Copper",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Top masthead beacon pedestal flange (y = 2.665 .. 2.693 m) beneath BeaconLamp
    mb.add_cylinder(
        center_u=(0.0, y1 - 0.001, 0.0),
        radius=0.074,
        length=0.028,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    return mb.build_object(parent=stage_4)


def _build_beacon_lamp_and_lens(mast_obj: bpy.types.Object) -> Tuple[bpy.types.Object, bpy.types.Object]:
    """Build `BeaconLamp` (weathered ~11.6 cm guard cage & collar) and child `RelayLamp_Lens` (~10.2 cm R1_LampAmber lens)."""
    d = antenna_mast.ANTENNA_MAST_DIMS
    by = d["beacon_y"]  # 2.72 m

    # 1. BeaconLamp: weathered cage & base mount (~0.116 m W x 0.116 m D x 0.116 m H, y = 2.684 .. 2.800 m)
    cage_mb = r1kit.MeshBuilder("BeaconLamp", pivot_u=(0.0, by, 0.0))
    # Heavy lower socket collar (y = 2.684 .. 2.704 m)
    cage_mb.add_cylinder(
        center_u=(0.0, 2.694, 0.0),
        radius=0.058,
        radius_top=0.055,
        length=0.020,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.003,
    )
    # Copper gland gasket ring (y = 2.702 .. 2.712 m)
    cage_mb.add_cylinder(
        center_u=(0.0, 2.707, 0.0),
        radius=0.053,
        length=0.010,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.002,
    )
    # 4 weathered bare-steel guard ribs rising around the lens to the top cap
    rib_angles = (45.0, 135.0, 225.0, 315.0)
    mid_pts: List[Tuple[float, float, float]] = []
    for angle_deg in rib_angles:
        rad = math.radians(angle_deg)
        c_a, s_a = math.cos(rad), math.sin(rad)
        p_bot = (0.054 * c_a, 2.704, 0.054 * s_a)
        p_mid = (0.055 * c_a, 2.752, 0.055 * s_a)
        p_top = (0.038 * c_a, 2.792, 0.038 * s_a)
        mid_pts.append(p_mid)
        cage_mb.add_strut(p_bot, p_mid, 0.010, 0.010, "R1_BareSteel", bevel=0.0)
        cage_mb.add_strut(p_mid, p_top, 0.010, 0.010, "R1_BareSteel", bevel=0.0)

    # Mid-height horizontal cage guard hoop connecting the 4 ribs at y = 2.752 m
    for idx_r in range(len(mid_pts)):
        p0 = mid_pts[idx_r]
        p1 = mid_pts[(idx_r + 1) % len(mid_pts)]
        cage_mb.add_strut(p0, p1, 0.009, 0.009, "R1_GreyMetal", bevel=0.0)

    # Top storm hood cap plate (y = 2.788 .. 2.800 m)
    cage_mb.add_cylinder(
        center_u=(0.0, 2.794, 0.0),
        radius=0.042,
        radius_top=0.026,
        length=0.012,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.003,
    )
    beacon_obj = cage_mb.build_object(parent=mast_obj)

    # 2. RelayLamp_Lens: separate child mesh node with its own renderer and R1_LampAmber slot
    #    (~0.102 m W x 0.102 m D x 0.102 m H, y = 2.694 .. 2.796 m, inside the weathered cage)
    lens_mb = r1kit.MeshBuilder("RelayLamp_Lens", pivot_u=(0.0, by, 0.0))
    # Lower Fresnel glass skirt (y = 2.694 .. 2.742 m, diameter 10.2 cm)
    lens_mb.add_cylinder(
        center_u=(0.0, 2.718, 0.0),
        radius=0.051,
        radius_top=0.048,
        length=0.048,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.004,
    )
    # Upper Fresnel glass tier (y = 2.738 .. 2.776 m)
    lens_mb.add_cylinder(
        center_u=(0.0, 2.757, 0.0),
        radius=0.046,
        radius_top=0.040,
        length=0.038,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.005,
    )
    # Domed amber crown (y = 2.772 .. 2.796 m)
    lens_mb.add_cylinder(
        center_u=(0.0, 2.784, 0.0),
        radius=0.037,
        radius_top=0.020,
        length=0.024,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=12,
        bevel=0.005,
    )
    lens_obj = lens_mb.build_object(parent=beacon_obj)
    return beacon_obj, lens_obj


def _build_dead_mast_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Build `Stage_4` for `DeadAntennaMast`, keeping all existing node names plus `BeaconLamp/RelayLamp_Lens`."""
    d = antenna_mast.ANTENNA_MAST_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. Single-mesh Lattice Mast (`Stage_4/Mast`)
    mast_obj = _build_dead_mast_lattice(stage_4)

    # 2. DishAzimuth and bent, weathered Dish (`Stage_4/Mast/DishAzimuth/Dish`)
    antenna_mast._build_dish_and_azimuth(mast_obj)
    az_obj = bpy.data.objects.get("DishAzimuth")
    old_dish = bpy.data.objects.get("Dish")
    if old_dish is not None:
        old_mesh = old_dish.data
        bpy.data.objects.remove(old_dish, do_unlink=True)
        if old_mesh is not None and old_mesh.users == 0:
            bpy.data.meshes.remove(old_mesh)
    if az_obj is not None:
        _build_bent_weathered_dish(az_obj)

    # 3. SonarLoop (`Stage_4/Mast/SonarLoop`)
    sx = d["sonar_x"]
    sy = d["sonar_y"]
    sz = d["sonar_z"]
    arm_l = d["sonar_loop_len"]
    sonar_mb = r1kit.MeshBuilder("SonarLoop", pivot_u=(sx, sy, sz))
    sonar_mb.add_cylinder(
        center_u=(sx, sy, sz),
        radius=0.028,
        length=0.048,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    for diag_deg in (42.0, 138.0, 222.0, 318.0):
        rad = math.radians(diag_deg)
        dx = math.cos(rad)
        dy_l = math.sin(rad)
        px_n = -dy_l
        py_n = dx
        tip_x = sx + dx * arm_l
        tip_y = sy + dy_l * arm_l
        mid_x = sx + dx * (arm_l * 0.68)
        mid_y = sy + dy_l * (arm_l * 0.68)
        w_l = 0.046
        p_l = (mid_x + px_n * w_l, mid_y + py_n * w_l, sz)
        p_r = (mid_x - px_n * w_l, mid_y - py_n * w_l, sz)
        p_t1 = (tip_x + px_n * (w_l * 0.55), tip_y + py_n * (w_l * 0.55), sz)
        p_t2 = (tip_x - px_n * (w_l * 0.55), tip_y - py_n * (w_l * 0.55), sz)
        sonar_mb.add_strut((sx, sy, sz), p_l, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut((sx, sy, sz), p_r, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_l, p_t1, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_r, p_t2, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_t1, p_t2, 0.015, 0.015, "R1_Copper", bevel=0.0)
    sonar_mb.build_object(parent=mast_obj)

    # 4. TapeReel_L and TapeReel_R (`Stage_4/TapeReel_L`, `Stage_4/TapeReel_R`)
    rx_reel = d["reel_x"]
    ry_reel = d["reel_y"]
    rz_reel = d["reel_z"]
    r_reel = d["reel_radius"]
    for side, r_name in ((1.0, "TapeReel_L"), (-1.0, "TapeReel_R")):
        cx_r = side * rx_reel
        reel_mb = r1kit.MeshBuilder(r_name, pivot_u=(cx_r, ry_reel, rz_reel))
        reel_mb.add_cylinder(
            center_u=(cx_r, ry_reel, rz_reel + 0.008),
            radius=r_reel,
            length=0.014,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=12,
            bevel=0.0,
            bevel_segments=0,
        )
        reel_mb.add_cylinder(
            center_u=(cx_r, ry_reel, rz_reel + 0.016),
            radius=r_reel * 0.74,
            length=0.014,
            mat="R1_GreyMetal",
            axis_u="Z",
            segments=10,
            bevel=0.0,
            bevel_segments=0,
        )
        for spk_deg in (30.0, 150.0, 270.0):
            rad_s = math.radians(spk_deg)
            reel_mb.add_strut(
                (cx_r + 0.024 * math.cos(rad_s), ry_reel + 0.024 * math.sin(rad_s), rz_reel + 0.024),
                (cx_r + (r_reel - 0.01) * math.cos(rad_s), ry_reel + (r_reel - 0.01) * math.sin(rad_s), rz_reel + 0.024),
                0.022,
                0.008,
                "R1_BareSteel",
                bevel=0.0,
            )
        reel_mb.add_cylinder(
            center_u=(cx_r, ry_reel, rz_reel + 0.026),
            radius=0.030,
            length=0.016,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
        reel_mb.build_object(parent=stage_4)

    # 5. BeaconLamp (weathered cage) + child RelayLamp_Lens (R1_LampAmber lens)
    _build_beacon_lamp_and_lens(mast_obj)

    # 6. DialLamp (`Stage_4/DialLamp`, unlit R1_DarkGlass on DeadAntennaMast)
    dy = d["dial_y"]
    dz = d["dial_z"]
    dtilt = d["dial_tilt_deg"]
    dial_mb = r1kit.MeshBuilder("DialLamp", pivot_u=(0.0, dy, dz))
    dial_mb.add_cylinder(
        center_u=(0.0, dy, dz + 0.006),
        radius=0.078,
        length=0.018,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=10,
        bevel=0.003,
        rot_deg_u=(dtilt, 0.0, 0.0),
    )
    dial_mb.build_object(parent=stage_4)

    return stage_4


# relay.1's feed socket: a bolted-on housing on the cabinet's +X flank, clear of the conduit pipes (z <= 0.08)
FEED_SOCKET_DIMS: dict[str, float] = {
    "flank_x": antenna_mast.ANTENNA_MAST_DIMS["cab_width"] * 0.5,
    "center_y": 0.74,
    "center_z": 0.25,
    "depth": 0.11,
    "wall": 0.025,
    "clearance": 0.01,
    "line_width": 0.007,
    "line_depth": 0.004,
}


def _build_feed_socket(root: bpy.types.Object) -> None:
    """relay.1's empty feed socket: an open dark housing cut to the feed unit's cross-section, the severed stub of
    the failed unit's connector at its back, and a thin `R1_RackOutline` frame round the mouth in the socket rack's
    bay language (Gameplay drives its `_EmissionColor`). `FeedSocket_Anchor` sits at the mouth, facing out (+X)."""
    d = FEED_SOCKET_DIMS
    u = relay_repair.FEED_UNIT_DIMS
    x0 = d["flank_x"]
    cy, cz = d["center_y"], d["center_z"]
    wall = d["wall"]
    open_z = u["body_width"] + 2 * d["clearance"]
    open_y = u["body_height"] + 2 * d["clearance"]
    out_z = open_z + 2 * wall
    out_y = open_y + 2 * wall
    mouth_x = x0 + d["depth"]
    mid_x = x0 + d["depth"] * 0.5

    mb = r1kit.MeshBuilder("FeedSocket", pivot_u=(x0, cy, cz))
    mb.add_bevel_box(center_u=(x0 + 0.005, cy, cz), size_u=(0.010, out_y + 0.05, out_z + 0.05), mat="R1_GreyMetal",
                     bevel=0.004, segments=1)
    for sy in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(mid_x, cy + sy * (open_y + wall) * 0.5, cz), size_u=(d["depth"], wall, out_z),
                         mat="R1_GreyMetal", bevel=0.005, segments=1)
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(center_u=(mid_x, cy, cz + sz * (open_z + wall) * 0.5), size_u=(d["depth"], open_y, wall),
                         mat="R1_GreyMetal", bevel=0.005, segments=1)
    # Dark rubber liner on the socket's back: the hole reads as a hole
    mb.add_bevel_box(center_u=(x0 + 0.014, cy, cz), size_u=(0.008, open_y, open_z), mat="R1_Rubber", bevel=0.0,
                     segments=0)
    # Two clamp straps over the flange, one each side, off-centre so they don't read as a dotted row
    for y_off, z_off in ((0.04, -(out_z * 0.5 + 0.012)), (-0.05, out_z * 0.5 + 0.012)):
        mb.add_bevel_box(center_u=(x0 + 0.014, cy + y_off, cz + z_off), size_u=(0.010, 0.05, 0.022),
                         mat="R1_GreyMetal", bevel=0.003, segments=1)
    # The failed unit's connector, snapped off: its collar, a scorched copper face, the bent blade and a jagged lip
    stub_x = x0 + 0.018
    mb.add_cylinder(center_u=(stub_x + 0.014, cy, cz), radius=u["connector_radius"] * 0.9, length=0.028,
                    mat="R1_GreyMetal", axis_u="X", segments=12, bevel=0.003)
    mb.add_cylinder(center_u=(stub_x + 0.030, cy, cz), radius=u["connector_radius"] * 0.6, length=0.006,
                    mat="R1_Copper", axis_u="X", segments=12, bevel=0.0)
    mb.add_strut((stub_x + 0.032, cy + 0.006, cz), (stub_x + 0.056, cy + 0.022, cz - 0.016), 0.012, 0.024,
                 "R1_Copper", bevel=0.002)
    for y_off, z_off, reach in ((0.034, 0.010, 0.030), (-0.020, 0.030, 0.018), (-0.028, -0.026, 0.024)):
        mb.add_bevel_box(center_u=(stub_x + 0.028 + reach * 0.5, cy + y_off, cz + z_off),
                         size_u=(reach, 0.012, 0.016), mat="R1_GreyMetal", bevel=0.002, segments=1,
                         rot_deg_u=(25.0 * y_off / abs(y_off), 0.0, 0.0))
    mb.build_object(parent=root)

    lw, ld = d["line_width"], d["line_depth"]
    frame_z = open_z + wall
    frame_y = open_y + wall
    line_x = mouth_x + ld * 0.5 + 0.0005
    outline = r1kit.MeshBuilder("FeedSocket_Outline", pivot_u=(mouth_x, cy, cz))
    for sy in (-1.0, 1.0):
        outline.add_bevel_box(center_u=(line_x, cy + sy * frame_y * 0.5, cz), size_u=(ld, lw, frame_z + lw),
                              mat="R1_RackOutline", bevel=0.0, segments=0)
    for sz in (-1.0, 1.0):
        outline.add_bevel_box(center_u=(line_x, cy, cz + sz * frame_z * 0.5), size_u=(ld, frame_y - lw, lw),
                              mat="R1_RackOutline", bevel=0.0, segments=0)
    outline.build_object(parent=root)

    r1kit.create_empty("FeedSocket_Anchor", parent=root, pivot_u=(mouth_x, cy, cz), outward_dir_u=(1.0, 0.0, 0.0))


def build_dead_antenna_mast() -> bpy.types.Object:
    """Build DeadAntennaMast (relay.1 silhouette on the west rim: weathered, bent dish, caged RelayLamp_Lens, and the
    empty `FeedSocket` its spare feed unit plugs into)."""
    r1kit.reset_scene()
    socket_rack.register_rack_materials()
    r1kit.ensure_materials(use_vertex_color_shading=False)

    root = r1kit.create_empty("DeadAntennaMast", pivot_u=(0.0, 0.0, 0.0))
    antenna_mast._build_stage_1(root)
    antenna_mast._build_stage_2(root)
    antenna_mast._build_stage_3(root)
    _build_dead_mast_stage_4(root)
    _build_feed_socket(root)

    # Replace emissive materials (R1_LampAmber, R1_Screen) with cold dark R1_DarkGlass on all nodes EXCEPT RelayLamp_Lens
    mats = r1kit.ensure_materials(use_vertex_color_shading=False)
    dark_glass_mat = mats["R1_DarkGlass"]
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or obj.name == "RelayLamp_Lens":
            continue
        mesh = obj.data
        for slot_idx, mat in enumerate(mesh.materials):
            if mat is not None and mat.name in ("R1_LampAmber", "R1_Screen"):
                mesh.materials[slot_idx] = dark_glass_mat

        # Deduplicate material slots if R1_DarkGlass now appears twice on a mesh
        slot_names = [m.name if m else "" for m in mesh.materials]
        if len(set(slot_names)) < len(slot_names):
            canonical_order = []
            remap = {}
            for old_i, m_name in enumerate(slot_names):
                if m_name not in canonical_order:
                    canonical_order.append(m_name)
                remap[old_i] = canonical_order.index(m_name)
            for poly in mesh.polygons:
                poly.material_index = remap.get(poly.material_index, 0)
            mesh.materials.clear()
            for m_name in canonical_order:
                mesh.materials.append(mats[m_name])

    antenna_mast.pose_antenna_mast_rest(root)
    r1kit.apply_triplanar_uvs(root, center_lamp_amber=True)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(
        root,
        footprint_radius=antenna_mast.ANTENNA_MAST_DIMS["footprint_width"] * 0.55,
    )

    # Boost dust and cavity darkening so DeadAntennaMast reads as cold, weathered, and long-dead;
    # on RelayLamp_Lens, darken vertex AO (R) so the amber lens reads as a dark dead lamp by day when unlit.
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or "Col" not in obj.data.color_attributes:
            continue
        attr = obj.data.color_attributes["Col"]
        if obj.name == "RelayLamp_Lens":
            for item in attr.data:
                r, g, b, a = item.color
                item.color = (
                    max(0.18, min(0.26, r * 0.24)),
                    max(0.0, min(1.0, g * 0.45)),
                    max(0.55, min(0.82, b + 0.45)),
                    a,
                )
        else:
            for item in attr.data:
                r, g, b, a = item.color
                item.color = (
                    max(0.18, min(1.0, r * 0.84)),
                    max(0.0, min(1.0, g * 1.18 + 0.06)),
                    max(0.0, min(1.0, b + 0.24)),
                    a,
                )

    fbx_path = PROPS_DIR / "DeadAntennaMast.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _add_segmented_vertical_cylinder(
    bm: bmesh.types.BMesh,
    radius: float,
    height_y: float,
    radial_segs: int,
    height_segs: int,
    mat_idx: int,
) -> None:
    """Add a closed, watertight manifold vertical cylinder with height subdivisions along Blender +Z (Unity +Y)."""
    rings: List[List[bmesh.types.BMVert]] = []
    for h_i in range(height_segs + 1):
        z_bl = (height_y * h_i) / height_segs
        ring: List[bmesh.types.BMVert] = []
        for s_i in range(radial_segs):
            ang = (2.0 * math.pi * s_i) / radial_segs
            x_bl = radius * math.cos(ang)
            y_bl = radius * math.sin(ang)
            ring.append(bm.verts.new((x_bl, y_bl, z_bl)))
        rings.append(ring)

    faces: List[bmesh.types.BMFace] = []
    for h_i in range(height_segs):
        r0 = rings[h_i]
        r1 = rings[h_i + 1]
        for s_i in range(radial_segs):
            s_next = (s_i + 1) % radial_segs
            f = bm.faces.new((r0[s_i], r0[s_next], r1[s_next], r1[s_i]))
            f.material_index = mat_idx
            f.smooth = True
            faces.append(f)

    # Bottom and top end-caps so the mesh is 100% closed manifold (boundary_edges == 0)
    f_bot = bm.faces.new(list(reversed(rings[0])))
    f_bot.material_index = mat_idx
    f_bot.smooth = False
    faces.append(f_bot)

    f_top = bm.faces.new(rings[-1])
    f_top.material_index = mat_idx
    f_top.smooth = False
    faces.append(f_top)

    bmesh.ops.recalc_face_normals(bm, faces=faces)


def build_signal_pillar() -> bpy.types.Object:
    """Build B11 'pillar of light' stylised vacuum signal beam (~0.88 W x 0.88 L x 60.0 H m)."""
    r1kit.reset_scene()
    mats = r1kit.ensure_materials(use_vertex_color_shading=False)

    root = r1kit.create_empty("SignalPillar", pivot_u=(0.0, 0.0, 0.0))

    pillar_height = 60.0
    r_core = 0.14
    r_halo = 0.44

    bm = bmesh.new()
    # Two concentric closed manifold cylinders: inner core (r=0.14 m) and outer soft envelope (r=0.44 m)
    _add_segmented_vertical_cylinder(bm, radius=r_core, height_y=pillar_height, radial_segs=16, height_segs=16, mat_idx=0)
    _add_segmented_vertical_cylinder(bm, radius=r_halo, height_y=pillar_height, radial_segs=20, height_segs=16, mat_idx=0)

    me = bpy.data.meshes.new("PillarBeam")
    bm.to_mesh(me)
    bm.free()

    # Use R1_LampAmber slot in FBX (replaced in Unity prefab by R1_SignalPillar.mat with MoonProject/R1/SignalPillar shader)
    me.materials.append(mats["R1_LampAmber"])

    beam_obj = bpy.data.objects.new("PillarBeam", me)
    bpy.context.collection.objects.link(beam_obj)
    beam_obj.parent = root
    beam_obj.location = (0.0, 0.0, 0.0)

    r1kit.apply_weighted_normals(beam_obj, use_modifier=True)

    # Set clean cylindrical UVs (U = angle around cylinder, V = normalized height 0..1)
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv_layer = me.uv_layers.active.data
    for poly in me.polygons:
        for loop_idx in poly.loop_indices:
            v_co = me.vertices[me.loops[loop_idx].vertex_index].co
            ang01 = (math.atan2(v_co.y, v_co.x) / (2.0 * math.pi)) % 1.0
            h01 = max(0.0, min(1.0, v_co.z / pillar_height))
            uv_layer[loop_idx].uv = (ang01, h01)

    # Bake custom vertex colors encoding vertical height fade in R, core-vs-halo weight in G, and soft top fade in B
    if "Col" in me.color_attributes:
        me.color_attributes.remove(me.color_attributes["Col"])
    col_attr = me.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    for poly in me.polygons:
        is_cap = abs(poly.normal.z) > 0.8
        for loop_idx in poly.loop_indices:
            v_co = me.vertices[me.loops[loop_idx].vertex_index].co
            rad = math.hypot(v_co.x, v_co.y)
            h01 = max(0.0, min(1.0, v_co.z / pillar_height))
            # Soft fade at the top and zero on end-caps
            vert_fade = 0.0 if is_cap else ((1.0 - h01) ** 1.65)
            is_inner = 1.0 if rad < 0.25 else 0.42
            col_attr.data[loop_idx].color = (
                max(0.02, min(1.0, vert_fade)),
                is_inner,
                h01,
                1.0,
            )

    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Top_Anchor", parent=root, pivot_u=(0.0, pillar_height, 0.0))

    fbx_path = PROPS_DIR / "SignalPillar.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_dead_antenna_mast()
    build_signal_pillar()


if __name__ == "__main__":
    main()
