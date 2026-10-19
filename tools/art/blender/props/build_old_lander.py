"""Build R1 old robotic lander story prop and blast ground decal in Blender 5.1:
  - Prop_OldLander.fbx (~3.0 m tall 1960s Surveyor-style robotic lander, tilted ~8 deg with one half-buried footpad)
  - Decal_OldLanderBlast.fbx (soft, slightly brighter disturbed-regolith patch with three footpad scuffs)

Run headless:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_old_lander.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

import r1kit  # noqa: E402


from mathutils import Euler, Vector  # noqa: E402


EXTENDED_R1_MATERIAL_SPECS = {
    "R1_WhitePaint": {
        "base_color": r1kit._srgb_to_linear("#E8EAE6"),
        "wear_color": r1kit._srgb_to_linear("#C2C7CC"),
        "dust_color": r1kit._srgb_to_linear("#8D8B86"),
        "metallic": 0.02,
        "roughness": 0.82,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_TealMark": {
        "base_color": r1kit._srgb_to_linear("#2F6E73"),
        "wear_color": r1kit._srgb_to_linear("#A8BDC2"),
        "dust_color": r1kit._srgb_to_linear("#788086"),
        "metallic": 0.04,
        "roughness": 0.65,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_SilverFoil": {
        "base_color": r1kit._srgb_to_linear("#C9CED2"),
        "wear_color": r1kit._srgb_to_linear("#EBF0F5"),
        "dust_color": r1kit._srgb_to_linear("#8A8E94"),
        "metallic": 0.85,
        "roughness": 0.45,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
}


def ensure_extended_r1_materials() -> None:
    """Register Era-P/Era-S palette slots in r1kit at runtime."""
    for name, spec in EXTENDED_R1_MATERIAL_SPECS.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = tuple(r1kit.ALLOWED_MATERIALS) + (name,)
        r1kit.MATERIAL_SPECS[name] = spec


# ---------------------------------------------------------------------------
# Pose helper: ~8-degree lander tilt toward the front-right (+60 deg) half-buried
# footpad so the tilt and buried footpad read unmistakably from front & 3/4 views.
#   sqrt(6.8^2 + 4.2^2) = 7.99 deg (~8.0 deg total tilt)
# ---------------------------------------------------------------------------
TILT_PITCH_DEG = -4.2  # tilts +Z_u downward
TILT_ROLL_DEG = 6.8    # tilts +X_u downward into the front-right (+60 deg) buried footpad
PIVOT_U = (-0.36, 0.04, -0.20)  # hinge line between the two unburied footpads
_TILT_MAT_B = Euler(
    (math.radians(TILT_PITCH_DEG), math.radians(TILT_ROLL_DEG), 0.0),
    "XYZ",
).to_matrix()


def _pose_pt(pt_u: tuple[float, float, float]) -> tuple[float, float, float]:
    """Transform a lander-local Unity point (x, y, z) by the ~8-degree lunar settling tilt."""
    piv_b = Vector((PIVOT_U[0], PIVOT_U[2], PIVOT_U[1]))
    pt_b = Vector((pt_u[0], pt_u[2], pt_u[1]))
    out_b = piv_b + (_TILT_MAT_B @ (pt_b - piv_b))
    return (float(out_b.x), float(out_b.z), float(out_b.y))


def _pose_rot(rot_deg_u: tuple[float, float, float] = (0.0, 0.0, 0.0)) -> tuple[float, float, float]:
    """Compose a lander-local Unity Euler rotation with the ~8-degree settling tilt."""
    local_mat_b = Euler(
        (
            math.radians(rot_deg_u[0]),
            math.radians(rot_deg_u[2]),
            math.radians(-rot_deg_u[1]),
        ),
        "XYZ",
    ).to_matrix()
    comb_e = (_TILT_MAT_B @ local_mat_b).to_euler("XYZ")
    return (
        math.degrees(comb_e.x),
        -math.degrees(comb_e.z),
        math.degrees(comb_e.y),
    )


def _cyl_pt(radius: float, deg: float, y: float) -> tuple[float, float, float]:
    """Return lander-local Unity point at cylindrical (radius, azimuth_deg_from_+Z, y)."""
    rad = math.radians(deg)
    return (radius * math.sin(rad), y, radius * math.cos(rad))


# ===========================================================================
# 1. Prop_OldLander.fbx (~3.0 m tall 1960s Surveyor-style lander)
# ===========================================================================
def build_prop_old_lander(out_fbx: Path) -> None:
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Prop_OldLander", None, (0.0, 0.0, 0.0))

    # Three primary corner angles of the triangular spaceframe (deg from +Z_u):
    #   Corner 0: +60 deg (Front-Right, Leg 1 - half-buried below Y=0)
    #   Corner 1: -60 deg (Front-Left,  Leg 2 - resting on Y=0)
    #   Corner 2: 180 deg (Rear,        Leg 3 - resting on Y=0)
    corner_angles = (60.0, -60.0, 180.0)
    r_lower = 0.74
    y_lower = 0.56
    r_upper = 0.42
    y_upper = 1.30

    lower_corners = [_cyl_pt(r_lower, a, y_lower) for a in corner_angles]
    upper_corners = [_cyl_pt(r_upper, a, y_upper) for a in corner_angles]

    # -----------------------------------------------------------------------
    # A. Spaceframe (triangular bare-aluminium truss, retro-thruster, tanks)
    # -----------------------------------------------------------------------
    sf = r1kit.MeshBuilder("Spaceframe", pivot_u=(0.0, 0.85, 0.0))

    for i in range(3):
        j = (i + 1) % 3
        p0_l = _pose_pt(lower_corners[i])
        p1_l = _pose_pt(lower_corners[j])
        p0_u = _pose_pt(upper_corners[i])
        p1_u = _pose_pt(upper_corners[j])

        # Main lower perimeter bare-aluminium tubes
        sf.add_pipe(p0_l, p1_l, radius=0.036, mat="R1_BareSteel", segments=10)
        # Secondary lower inner chord tubes
        p0_li = _pose_pt(_cyl_pt(r_lower * 0.70, corner_angles[i], y_lower - 0.06))
        p1_li = _pose_pt(_cyl_pt(r_lower * 0.70, corner_angles[j], y_lower - 0.06))
        sf.add_pipe(p0_li, p1_li, radius=0.028, mat="R1_BareSteel", segments=8)
        sf.add_pipe(p0_l, p0_li, radius=0.026, mat="R1_BareSteel", segments=8)

        # Upper perimeter triangle tubes
        sf.add_pipe(p0_u, p1_u, radius=0.032, mat="R1_BareSteel", segments=10)

        # Primary rising corner columns & V-diagonals
        sf.add_pipe(p0_l, p0_u, radius=0.034, mat="R1_BareSteel", segments=10)
        mid_low = _pose_pt(
            (
                (lower_corners[i][0] + lower_corners[j][0]) * 0.5,
                y_lower,
                (lower_corners[i][2] + lower_corners[j][2]) * 0.5,
            )
        )
        sf.add_pipe(mid_low, p0_u, radius=0.026, mat="R1_BareSteel", segments=8)
        sf.add_pipe(mid_low, p1_u, radius=0.026, mat="R1_BareSteel", segments=8)

        # Machined aluminium corner gusset blocks at lower & upper vertices
        sf.add_bevel_box(
            center_u=p0_l,
            size_u=(0.14, 0.11, 0.14),
            mat="R1_BareSteel",
            bevel=0.012,
            segments=1,
            rot_deg_u=_pose_rot((0.0, corner_angles[i], 0.0)),
        )
        sf.add_bevel_box(
            center_u=p0_u,
            size_u=(0.11, 0.09, 0.11),
            mat="R1_BareSteel",
            bevel=0.010,
            segments=1,
            rot_deg_u=_pose_rot((0.0, corner_angles[i], 0.0)),
        )

    # Central mast support spider pyramid & upper mast collar
    mast_base_u = _pose_pt((0.0, 1.44, 0.0))
    for i in range(3):
        sf.add_pipe(_pose_pt(upper_corners[i]), mast_base_u, radius=0.030, mat="R1_BareSteel", segments=8)

    sf.add_cylinder(
        center_u=mast_base_u,
        radius=0.14,
        length=0.12,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.010,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )

    # Central solid retro-motor casing & spent nozzle bell underneath the spaceframe
    sf.add_cylinder(
        center_u=_pose_pt((0.0, 0.70, 0.0)),
        radius=0.28,
        radius_top=0.24,
        length=0.34,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=16,
        bevel=0.014,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    sf.add_cylinder(
        center_u=_pose_pt((0.0, 0.38, 0.0)),
        radius=0.19,
        radius_top=0.11,
        length=0.28,
        mat="R1_Rubber",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    for a in corner_angles:
        sf.add_pipe(
            _pose_pt(_cyl_pt(0.24, a, 0.58)),
            _pose_pt(_cyl_pt(r_lower, a, y_lower)),
            radius=0.028,
            mat="R1_BareSteel",
            segments=8,
        )

    # Spherical helium/propellant pressure tanks inside the open spaceframe (bare metal & copper, no gold foil)
    tank_specs = (
        (0.26, 0.88, -0.18, 0.16, "R1_BareSteel"),
        (-0.26, 0.88, -0.18, 0.15, "R1_BareSteel"),
        (0.0, 0.86, -0.34, 0.14, "R1_Copper"),
    )
    for tx, ty, tz, trad, tmat in tank_specs:
        t_pos = _pose_pt((tx, ty, tz))
        sf.add_cylinder(
            center_u=t_pos,
            radius=trad * 0.82,
            radius_top=trad,
            length=trad * 1.65,
            mat=tmat,
            axis_u="Y",
            segments=14,
            bevel=trad * 0.28,
            bevel_segments=2,
            rot_deg_u=_pose_rot(),
        )
        sf.add_torus(
            center_u=t_pos,
            major_radius=trad * 0.96,
            minor_radius=0.012,
            mat="R1_BareSteel",
            axis_u="Y",
            major_segments=14,
            minor_segments=6,
            rot_deg_u=_pose_rot(),
        )

    # Vernier attitude control thrusters near the 3 lower corners
    for a in corner_angles:
        v_pos = _pose_pt(_cyl_pt(r_lower * 0.88, a + 18.0, y_lower - 0.10))
        sf.add_cylinder(
            center_u=v_pos,
            radius=0.048,
            radius_top=0.026,
            length=0.14,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=10,
            bevel=0.004,
            bevel_segments=1,
            rot_deg_u=_pose_rot((12.0, a, 0.0)),
        )

    sf.build_object(parent=root)

    # -----------------------------------------------------------------------
    # B. LandingLegs (3 hinged A-frame bare-aluminium landing legs + round crushable footpads;
    #    Leg 1 at +60 deg is half-buried below Y = 0 with disturbed regolith collar)
    # -----------------------------------------------------------------------
    legs = r1kit.MeshBuilder("LandingLegs", pivot_u=(0.0, 0.35, 0.0))

    leg_configs = (
        # (azimuth_deg, foot_world_u, is_buried)
        (60.0, (1.08, -0.065, 0.62), True),     # Front-Right: half-buried down to Y = -0.14 m
        (-60.0, (-1.08, 0.075, 0.62), False),   # Front-Left: resting on Y = 0.00 m
        (180.0, (0.0, 0.075, -1.24), False),    # Rear: resting on Y = 0.00 m
    )

    for az_deg, foot_u, is_buried in leg_configs:
        az_rad = math.radians(az_deg)
        tan_x = math.cos(az_rad)
        tan_z = -math.sin(az_rad)
        rad_x = math.sin(az_rad)
        rad_z = math.cos(az_rad)

        c_low = _pose_pt(_cyl_pt(r_lower, az_deg, y_lower))
        hinge_spread = 0.16
        h_left = (c_low[0] + tan_x * hinge_spread, c_low[1], c_low[2] + tan_z * hinge_spread)
        h_right = (c_low[0] - tan_x * hinge_spread, c_low[1], c_low[2] - tan_z * hinge_spread)

        wrist_u = (foot_u[0] - rad_x * 0.04, foot_u[1] + 0.09, foot_u[2] - rad_z * 0.04)

        # A-frame twin lower tubular bare-aluminium struts
        legs.add_pipe(h_left, wrist_u, radius=0.032, mat="R1_BareSteel", segments=10)
        legs.add_pipe(h_right, wrist_u, radius=0.032, mat="R1_BareSteel", segments=10)

        # Crossbar between the A-frame legs
        mid_l = tuple(h_left[k] * 0.52 + wrist_u[k] * 0.48 for k in range(3))
        mid_r = tuple(h_right[k] * 0.52 + wrist_u[k] * 0.48 for k in range(3))
        legs.add_pipe(mid_l, mid_r, radius=0.024, mat="R1_BareSteel", segments=8)

        # Hinge knuckles at the spaceframe attachment
        legs.add_hinge(
            center_u=c_low,
            axis_u="X",
            length=0.26,
            knuckle_radius=0.028,
            leaf_width=0.075,
            leaf_thickness=0.018,
            rot_deg_u=_pose_rot((0.0, az_deg, 0.0)),
            mat_body="R1_BareSteel",
            mat_pin="R1_BareSteel",
        )

        # Telescoping hydraulic shock absorber strut from upper spaceframe corner to leg mid-wrist
        c_up = _pose_pt(_cyl_pt(r_upper + 0.04, az_deg, y_upper - 0.04))
        shock_bot = tuple((mid_l[k] + mid_r[k]) * 0.5 + (wrist_u[k] - c_low[k]) * 0.18 for k in range(3))
        shock_mid = tuple(c_up[k] * 0.56 + shock_bot[k] * 0.44 for k in range(3))
        legs.add_pipe(c_up, shock_mid, radius=0.042, mat="R1_BareSteel", segments=10)
        legs.add_pipe(shock_mid, shock_bot, radius=0.030, mat="R1_BareSteel", segments=10)
        legs.add_cylinder(
            center_u=shock_mid,
            radius=0.046,
            length=0.05,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=10,
            bevel=0.005,
            bevel_segments=1,
            rot_deg_u=_pose_rot((25.0, az_deg, 0.0)),
        )

        # Round crushable honeycomb bare-aluminium footpad (no gold foil or cream)
        pad_rot = (0.0, az_deg, 0.0) if not is_buried else (-5.0, az_deg, -6.0)
        legs.add_cylinder(
            center_u=(foot_u[0], foot_u[1] - 0.030, foot_u[2]),
            radius=0.22,
            radius_top=0.26,
            length=0.090,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=18,
            bevel=0.012,
            bevel_segments=1,
            rot_deg_u=pad_rot,
        )
        legs.add_cylinder(
            center_u=(foot_u[0], foot_u[1] + 0.035, foot_u[2]),
            radius=0.26,
            radius_top=0.14,
            length=0.060,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=18,
            bevel=0.008,
            bevel_segments=1,
            rot_deg_u=pad_rot,
        )
        legs.add_torus(
            center_u=(foot_u[0], foot_u[1] + 0.012, foot_u[2]),
            major_radius=0.255,
            minor_radius=0.018,
            mat="R1_BareSteel",
            axis_u="Y",
            major_segments=18,
            minor_segments=8,
            rot_deg_u=pad_rot,
        )
        legs.add_bevel_box(
            center_u=wrist_u,
            size_u=(0.11, 0.09, 0.11),
            mat="R1_BareSteel",
            bevel=0.010,
            segments=1,
            rot_deg_u=pad_rot,
        )

        # Mounded lunar regolith burial collar around the half-buried footpad at Y = 0
        if is_buried:
            legs.add_torus(
                center_u=(foot_u[0] + 0.02, 0.008, foot_u[2] + 0.02),
                major_radius=0.24,
                minor_radius=0.052,
                mat="R1_Basalt",
                axis_u="Y",
                major_segments=18,
                minor_segments=8,
                rot_deg_u=(2.0, az_deg, 0.0),
            )
            legs.add_cylinder(
                center_u=(foot_u[0] + 0.05, -0.012, foot_u[2] + 0.03),
                radius=0.29,
                radius_top=0.21,
                length=0.075,
                mat="R1_Basalt",
                axis_u="Y",
                segments=16,
                bevel=0.018,
                bevel_segments=1,
            )

    legs.build_object(parent=root)

    # -----------------------------------------------------------------------
    # C. MastAndSolar (Tall central mast reaching ~2.86 m above ground / 3.00 m total height,
    #    with flat solar panel; planar array exposed separately on Array_Hinge)
    # -----------------------------------------------------------------------
    mast = r1kit.MeshBuilder("MastAndSolar", pivot_u=(0.0, 2.15, 0.0))

    m_bot = _pose_pt((0.0, 1.40, 0.0))
    m_mid = _pose_pt((0.0, 2.18, 0.0))
    m_top = _pose_pt((0.0, 2.84, 0.0))
    mast.add_pipe(m_bot, m_mid, radius=0.046, mat="R1_BareSteel", segments=12)
    mast.add_pipe(m_mid, m_top, radius=0.038, mat="R1_BareSteel", segments=12)

    for ym in (1.60, 1.90, 2.20, 2.48, 2.74):
        mast.add_cylinder(
            center_u=_pose_pt((0.0, ym, 0.0)),
            radius=0.058,
            length=0.036,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.005,
            bevel_segments=1,
            rot_deg_u=_pose_rot(),
        )
    mast.add_pipe(
        _pose_pt((0.048, 1.44, 0.02)),
        _pose_pt((0.044, 2.52, 0.02)),
        radius=0.018,
        mat="R1_Copper",
        segments=8,
    )

    gimbal_u = _pose_pt((0.0, 2.52, 0.0))
    mast.add_bevel_box(
        center_u=gimbal_u,
        size_u=(0.24, 0.16, 0.18),
        mat="R1_BareSteel",
        bevel=0.014,
        segments=1,
        rot_deg_u=_pose_rot(),
    )
    mast.add_cylinder(
        center_u=gimbal_u,
        radius=0.055,
        length=0.58,
        mat="R1_BareSteel",
        axis_u="X",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    mast.add_cylinder(
        center_u=_pose_pt((0.14, 2.52, 0.0)),
        radius=0.072,
        length=0.05,
        mat="R1_Brass",
        axis_u="X",
        segments=12,
        bevel=0.006,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )

    # Omni tip cone at top of mast
    mast.add_cylinder(
        center_u=_pose_pt((0.0, 2.86, 0.0)),
        radius=0.048,
        radius_top=0.022,
        length=0.08,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.005,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )

    # 1) Small flat Solar Panel on the +X side of the mast gimbal, tilted forward (+Z) so the dark indigo
    #    R1_SolarCell photovoltaic cells and silver busbars are clearly visible from the front/3/4 camera!
    sp_center_local = (0.48, 2.48, 0.08)
    sp_rot = _pose_rot((-34.0, 12.0, -8.0))
    sp_p_rad = math.radians(-34.0)
    # Normal to the tilted panel (+Y tilted toward +Z by 34 deg)
    sp_ny = math.cos(sp_p_rad)
    sp_nz = -math.sin(sp_p_rad)

    mast.add_bevel_box(
        center_u=_pose_pt(sp_center_local),
        size_u=(0.76, 0.036, 0.56),
        mat="R1_BareSteel",
        bevel=0.008,
        segments=1,
        rot_deg_u=sp_rot,
    )
    mast.add_bevel_box(
        center_u=_pose_pt(
            (
                sp_center_local[0],
                sp_center_local[1] + sp_ny * 0.015,
                sp_center_local[2] + sp_nz * 0.015,
            )
        ),
        size_u=(0.70, 0.020, 0.50),
        mat="R1_SolarCell",
        bevel=0.004,
        segments=1,
        rot_deg_u=sp_rot,
    )
    for sx_off in (-0.22, 0.0, 0.22):
        mast.add_bevel_box(
            center_u=_pose_pt(
                (
                    sp_center_local[0] + sx_off,
                    sp_center_local[1] + sp_ny * 0.022,
                    sp_center_local[2] + sp_nz * 0.022,
                )
            ),
            size_u=(0.018, 0.016, 0.48),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=sp_rot,
        )
    mast.add_pipe(
        _pose_pt((0.24, 2.52, 0.0)),
        _pose_pt(sp_center_local),
        radius=0.028,
        mat="R1_BareSteel",
        segments=8,
    )

    mast.build_object(parent=root)

    # -----------------------------------------------------------------------
    # C2. Array_Hinge (Flat rectangular Planar Array Antenna on its own hinge node,
    #     tilting about the mast arm axis like Surveyor's two-axis drive; default pose unchanged)
    # -----------------------------------------------------------------------
    array_hinge_pivot_u = _pose_pt((-0.22, 2.52, 0.0))
    array_hinge = r1kit.MeshBuilder("Array_Hinge", pivot_u=array_hinge_pivot_u)

    pa_center_local = (-0.38, 2.56, -0.04)
    pa_rot = _pose_rot((-52.0, -16.0, 6.0))
    array_hinge.add_cylinder(
        center_u=array_hinge_pivot_u,
        radius=0.068,
        length=0.048,
        mat="R1_BareSteel",
        axis_u="X",
        segments=12,
        bevel=0.005,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    array_hinge.add_bevel_box(
        center_u=_pose_pt(pa_center_local),
        size_u=(0.68, 0.048, 0.74),
        mat="R1_BareSteel",
        bevel=0.010,
        segments=1,
        rot_deg_u=pa_rot,
    )
    array_hinge.add_bevel_box(
        center_u=_pose_pt((pa_center_local[0], pa_center_local[1] + 0.012, pa_center_local[2] + 0.014)),
        size_u=(0.60, 0.036, 0.66),
        mat="R1_WhitePaint",
        bevel=0.006,
        segments=1,
        rot_deg_u=pa_rot,
    )
    for row_off in (-0.20, -0.07, 0.07, 0.20):
        array_hinge.add_bevel_box(
            center_u=_pose_pt((pa_center_local[0] + row_off, pa_center_local[1] + 0.020, pa_center_local[2] + 0.020)),
            size_u=(0.034, 0.024, 0.58),
            mat="R1_Copper",
            bevel=0.003,
            segments=1,
            rot_deg_u=pa_rot,
        )
    array_hinge.add_pipe(
        array_hinge_pivot_u,
        _pose_pt(pa_center_local),
        radius=0.030,
        mat="R1_BareSteel",
        segments=8,
    )

    array_hinge.build_object(parent=root)

    # -----------------------------------------------------------------------
    # D. TvCamera (Surveyor vertical television camera with rotating mirror hood)
    # -----------------------------------------------------------------------
    cam_x, cam_z = 0.28, 0.26
    cam = r1kit.MeshBuilder("TvCamera", pivot_u=_pose_pt((cam_x, 1.48, cam_z)))

    cam.add_bevel_box(
        center_u=_pose_pt((cam_x, 1.22, cam_z)),
        size_u=(0.18, 0.10, 0.18),
        mat="R1_BareSteel",
        bevel=0.010,
        segments=1,
        rot_deg_u=_pose_rot((0.0, 20.0, 0.0)),
    )
    cam.add_cylinder(
        center_u=_pose_pt((cam_x, 1.38, cam_z)),
        radius=0.078,
        radius_top=0.068,
        length=0.28,
        mat="R1_WhitePaint",
        axis_u="Y",
        segments=14,
        bevel=0.008,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    cam.add_cylinder(
        center_u=_pose_pt((cam_x, 1.54, cam_z)),
        radius=0.076,
        length=0.048,
        mat="R1_Brass",
        axis_u="Y",
        segments=14,
        bevel=0.005,
        bevel_segments=1,
        rot_deg_u=_pose_rot(),
    )
    hood_yaw = 18.0
    cam.add_bevel_box(
        center_u=_pose_pt((cam_x, 1.67, cam_z)),
        size_u=(0.15, 0.22, 0.16),
        mat="R1_WhitePaint",
        bevel=0.012,
        segments=1,
        rot_deg_u=_pose_rot((0.0, hood_yaw, 0.0)),
    )
    visor_off_x = math.sin(math.radians(hood_yaw)) * 0.08
    visor_off_z = math.cos(math.radians(hood_yaw)) * 0.08
    cam.add_bevel_box(
        center_u=_pose_pt((cam_x + visor_off_x, 1.71, cam_z + visor_off_z)),
        size_u=(0.16, 0.12, 0.12),
        mat="R1_BareSteel",
        bevel=0.008,
        segments=1,
        rot_deg_u=_pose_rot((0.0, hood_yaw, 0.0)),
    )
    mirror_off_x = math.sin(math.radians(hood_yaw)) * 0.06
    mirror_off_z = math.cos(math.radians(hood_yaw)) * 0.06
    cam.add_bevel_box(
        center_u=_pose_pt((cam_x + mirror_off_x, 1.66, cam_z + mirror_off_z)),
        size_u=(0.11, 0.13, 0.024),
        mat="R1_DarkGlass",
        bevel=0.003,
        segments=1,
        rot_deg_u=_pose_rot((42.0, hood_yaw, 0.0)),
    )

    cam.build_object(parent=root)

    # -----------------------------------------------------------------------
    # E. ScoopArm (Surveyor pantograph surface-sampler scoop arm folded down)
    # -----------------------------------------------------------------------
    scoop = r1kit.MeshBuilder("ScoopArm", pivot_u=_pose_pt((-0.14, 0.46, 0.56)))

    base_s = (-0.14, 0.58, 0.50)
    scoop.add_bevel_box(
        center_u=_pose_pt(base_s),
        size_u=(0.16, 0.14, 0.16),
        mat="R1_BareSteel",
        bevel=0.010,
        segments=1,
        rot_deg_u=_pose_rot((0.0, -14.0, 0.0)),
    )
    scoop.add_cylinder(
        center_u=_pose_pt((base_s[0], base_s[1] - 0.02, base_s[2] + 0.06)),
        radius=0.045,
        length=0.18,
        mat="R1_Brass",
        axis_u="X",
        segments=10,
        bevel=0.004,
        bevel_segments=1,
        rot_deg_u=_pose_rot((0.0, -14.0, 0.0)),
    )

    hinge_pts = [
        (-0.14, 0.56, 0.54),
        (-0.17, 0.45, 0.66),
        (-0.20, 0.35, 0.77),
        (-0.23, 0.26, 0.87),
    ]
    for idx in range(len(hinge_pts) - 1):
        p_a = hinge_pts[idx]
        p_b = hinge_pts[idx + 1]
        up_a = _pose_pt((p_a[0] - 0.03, p_a[1] + 0.055, p_a[2]))
        dn_a = _pose_pt((p_a[0] + 0.03, p_a[1] - 0.055, p_a[2]))
        up_b = _pose_pt((p_b[0] - 0.03, p_b[1] + 0.055, p_b[2]))
        dn_b = _pose_pt((p_b[0] + 0.03, p_b[1] - 0.055, p_b[2]))
        scoop.add_strut(up_a, dn_b, width=0.030, thickness=0.022, mat="R1_BareSteel", bevel=0.003)
        scoop.add_strut(dn_a, up_b, width=0.030, thickness=0.022, mat="R1_BareSteel", bevel=0.003)
        scoop.add_cylinder(
            center_u=_pose_pt(((p_a[0] + p_b[0]) * 0.5, (p_a[1] + p_b[1]) * 0.5, (p_a[2] + p_b[2]) * 0.5)),
            radius=0.018,
            length=0.09,
            mat="R1_Brass",
            axis_u="X",
            segments=8,
            bevel=0.002,
            bevel_segments=1,
            rot_deg_u=_pose_rot((0.0, -14.0, 0.0)),
        )

    tip_u = (-0.25, 0.21, 0.94)
    scoop_rot = _pose_rot((32.0, -14.0, 0.0))
    scoop.add_bevel_box(
        center_u=_pose_pt(tip_u),
        size_u=(0.13, 0.10, 0.18),
        mat="R1_TealMark",
        bevel=0.008,
        segments=1,
        rot_deg_u=scoop_rot,
    )
    scoop.add_bevel_box(
        center_u=_pose_pt((tip_u[0] - 0.015, tip_u[1] - 0.035, tip_u[2] + 0.075)),
        size_u=(0.14, 0.065, 0.09),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
        rot_deg_u=scoop_rot,
    )

    scoop.build_object(parent=root)

    # -----------------------------------------------------------------------
    # F. ThermalBoxes (Chalky white thermal paint R1_WhitePaint chalked toward grey-tan,
    #    with a small silver mirror patch R1_SilverFoil topping each compartment;
    #    Compartment A centered on the front (+Z) bay between Leg 1 & Leg 2 so its
    #    "S-3" stencil is 100% unobstructed by leg shock struts!)
    # -----------------------------------------------------------------------
    tboxes = r1kit.MeshBuilder("ThermalBoxes", pivot_u=(0.0, 0.90, 0.0))

    # Compartment A: Front bay (X=-0.04, Y=0.92, Z=+0.34, yaw=0 deg, untapered vertical door)
    box_a_center = (-0.04, 0.92, 0.34)
    box_a_rot = _pose_rot((0.0, 0.0, 0.0))
    tboxes.add_bevel_box(
        center_u=_pose_pt(box_a_center),
        size_u=(0.50, 0.44, 0.34),
        mat="R1_WhitePaint",
        bevel=0.016,
        segments=2,
        rot_deg_u=box_a_rot,
    )
    tboxes.add_bevel_box(
        center_u=_pose_pt((box_a_center[0], box_a_center[1] + 0.225, box_a_center[2])),
        size_u=(0.42, 0.028, 0.26),
        mat="R1_SilverFoil",
        bevel=0.005,
        segments=1,
        rot_deg_u=box_a_rot,
    )
    tboxes.add_bevel_box(
        center_u=_pose_pt((box_a_center[0], box_a_center[1] - 0.17, box_a_center[2])),
        size_u=(0.52, 0.042, 0.36),
        mat="R1_Copper",
        bevel=0.006,
        segments=1,
        rot_deg_u=box_a_rot,
    )

    # Compartment B: Left-Rear bay (X=-0.42, Y=0.90, Z=-0.16, yaw=-120 deg)
    box_b_center = (-0.42, 0.90, -0.16)
    box_b_rot = _pose_rot((0.0, -120.0, 0.0))
    tboxes.add_bevel_box(
        center_u=_pose_pt(box_b_center),
        size_u=(0.50, 0.42, 0.34),
        mat="R1_WhitePaint",
        bevel=0.016,
        segments=2,
        rot_deg_u=box_b_rot,
    )
    tboxes.add_bevel_box(
        center_u=_pose_pt((box_b_center[0], box_b_center[1] + 0.215, box_b_center[2])),
        size_u=(0.42, 0.028, 0.26),
        mat="R1_SilverFoil",
        bevel=0.005,
        segments=1,
        rot_deg_u=box_b_rot,
    )
    tboxes.add_bevel_box(
        center_u=_pose_pt((box_b_center[0], box_b_center[1] - 0.16, box_b_center[2])),
        size_u=(0.52, 0.042, 0.36),
        mat="R1_Copper",
        bevel=0.006,
        segments=1,
        rot_deg_u=box_b_rot,
    )

    tboxes.build_object(parent=root)

    # -----------------------------------------------------------------------
    # G. SerialStencil (Faded stencilled serial "S-3" proud on Compartment A's
    #    vertical +Z door; generic livery, NO real agency logos, flags, or names)
    # -----------------------------------------------------------------------
    stencil = r1kit.MeshBuilder("SerialStencil", pivot_u=_pose_pt((box_a_center[0], box_a_center[1], box_a_center[2] + 0.172)))

    def _add_stencil_bar(u_x: float, u_y: float, w: float, h: float) -> None:
        # Compartment A front wall is at z = box_a_center[2] + 0.17; place 14 mm thick bar centered at +0.173
        cx = box_a_center[0] + u_x
        cy = box_a_center[1] + u_y
        cz = box_a_center[2] + 0.173
        stencil.add_bevel_box(
            center_u=_pose_pt((cx, cy, cz)),
            size_u=(w, h, 0.016),
            mat="R1_Rubber",
            bevel=0.002,
            segments=1,
            rot_deg_u=box_a_rot,
        )

    # Note: Looking at the +Z door from outside (+Z looking toward -Z),
    # visual LEFT is +X and visual RIGHT is -X!
    # So we place 'S' on visual left (+X = +0.11) and '3' on visual right (-X = -0.11),
    # with clean 3 mm stencil-bridge gaps between horizontal and vertical bars.
    # Letter 'S' (at u_x = +0.11):
    #   top bar, upper-left (+X) vertical, middle bar, lower-right (-X) vertical, bottom bar
    _add_stencil_bar(0.110, 0.064, 0.090, 0.024)    # top bar
    _add_stencil_bar(0.143, 0.032, 0.024, 0.034)    # upper-left vertical
    _add_stencil_bar(0.110, 0.000, 0.090, 0.024)    # middle bar
    _add_stencil_bar(0.077, -0.032, 0.024, 0.034)   # lower-right vertical
    _add_stencil_bar(0.110, -0.064, 0.090, 0.024)   # bottom bar

    # Hyphen '-' (at u_x = 0.00)
    _add_stencil_bar(0.000, 0.000, 0.054, 0.024)

    # Digit '3' (at u_x = -0.11):
    #   top bar, upper-right (-X) vertical, middle bar, lower-right (-X) vertical, bottom bar
    _add_stencil_bar(-0.110, 0.064, 0.090, 0.024)   # top bar
    _add_stencil_bar(-0.143, 0.032, 0.024, 0.034)   # upper-right vertical
    _add_stencil_bar(-0.106, 0.000, 0.082, 0.024)   # middle bar
    _add_stencil_bar(-0.143, -0.032, 0.024, 0.034)  # lower-right vertical
    _add_stencil_bar(-0.110, -0.064, 0.090, 0.024)  # bottom bar

    stencil.build_object(parent=root)

    # -----------------------------------------------------------------------
    # H. RemovedPartSpots (Two visible spots where a crew walked up and removed
    #    parts to take home: empty brackets, unbolted clamps, and cut cable ends)
    # -----------------------------------------------------------------------
    removed = r1kit.MeshBuilder("RemovedPartSpots", pivot_u=(0.0, 0.88, 0.25))

    # --- Spot 1: Stripped auxiliary instrument bracket on the Front-Right shoulder (X=+0.44, Y=0.88, Z=+0.24) ---
    s1_center = (0.44, 0.88, 0.24)
    s1_rot = _pose_rot((0.0, 52.0, 0.0))
    removed.add_bevel_box(
        center_u=_pose_pt(s1_center),
        size_u=(0.28, 0.24, 0.026),
        mat="R1_TealMark",
        bevel=0.004,
        segments=1,
        rot_deg_u=s1_rot,
    )
    for dy_b in (-0.10, 0.10):
        removed.add_bevel_box(
            center_u=_pose_pt((s1_center[0] + 0.03, s1_center[1] + dy_b, s1_center[2] + 0.025)),
            size_u=(0.30, 0.030, 0.062),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
            rot_deg_u=s1_rot,
        )
    for dx_s in (-0.10, 0.10):
        for dy_s in (-0.07, 0.07):
            removed.add_cylinder(
                center_u=_pose_pt((s1_center[0] + 0.03 + dx_s * 0.6, s1_center[1] + dy_s, s1_center[2] + 0.035)),
                radius=0.015,
                length=0.045,
                mat="R1_BareSteel",
                axis_u="Z",
                segments=8,
                bevel=0.002,
                bevel_segments=1,
                rot_deg_u=s1_rot,
            )
    s1_cables = (
        [(0.34, 0.98, 0.18), (0.42, 0.96, 0.26), (0.50, 0.92, 0.31)],
        [(0.32, 0.89, 0.19), (0.41, 0.87, 0.27), (0.49, 0.83, 0.32)],
        [(0.35, 0.80, 0.20), (0.43, 0.78, 0.28), (0.51, 0.74, 0.32)],
    )
    for c_path in s1_cables:
        posed_path = [_pose_pt(p) for p in c_path]
        removed.add_tube_path(posed_path, radius=0.016, mat="R1_Rubber", segments=8, cap_ends=True)
        tip_a = posed_path[-2]
        tip_b = posed_path[-1]
        ext_tip = tuple(tip_b[k] + (tip_b[k] - tip_a[k]) * 0.42 for k in range(3))
        removed.add_pipe(tip_b, ext_tip, radius=0.012, mat="R1_Copper", segments=8)

    # --- Spot 2: Removed cylindrical sensor/battery canister cradle on the Front-Left shoulder (X=-0.46, Y=0.86, Z=+0.24) ---
    s2_center = (-0.46, 0.86, 0.24)
    s2_rot = _pose_rot((0.0, -52.0, 0.0))
    for dy_c in (-0.09, 0.09):
        removed.add_torus(
            center_u=_pose_pt((s2_center[0] - 0.03, s2_center[1] + dy_c, s2_center[2] + 0.02)),
            major_radius=0.105,
            minor_radius=0.022,
            mat="R1_BareSteel",
            axis_u="Y",
            major_segments=14,
            minor_segments=8,
            arc_deg=190.0,
            start_deg=-95.0,
            rot_deg_u=s2_rot,
        )
    removed.add_bevel_box(
        center_u=_pose_pt((s2_center[0], s2_center[1], s2_center[2])),
        size_u=(0.12, 0.28, 0.032),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
        rot_deg_u=s2_rot,
    )
    removed.add_strut(
        _pose_pt((s2_center[0] - 0.06, s2_center[1] + 0.09, s2_center[2] + 0.06)),
        _pose_pt((s2_center[0] - 0.16, s2_center[1] - 0.04, s2_center[2] + 0.14)),
        width=0.030,
        thickness=0.018,
        mat="R1_BareSteel",
        bevel=0.002,
    )
    s2_cables = (
        [(-0.34, 0.96, 0.18), (-0.43, 0.94, 0.26), (-0.52, 0.90, 0.32)],
        [(-0.36, 0.78, 0.19), (-0.44, 0.76, 0.27), (-0.53, 0.72, 0.32)],
    )
    for c_path in s2_cables:
        posed_path = [_pose_pt(p) for p in c_path]
        removed.add_tube_path(posed_path, radius=0.016, mat="R1_Rubber", segments=8, cap_ends=True)
        tip_a = posed_path[-2]
        tip_b = posed_path[-1]
        ext_tip = tuple(tip_b[k] + (tip_b[k] - tip_a[k]) * 0.42 for k in range(3))
        removed.add_pipe(tip_b, ext_tip, radius=0.012, mat="R1_Copper", segments=8)

    removed.build_object(parent=root)

    # -----------------------------------------------------------------------
    # I. Semantic Empty Anchors & Maker Decal (Decal_VIS_1: 1967 VIS wordmark)
    # -----------------------------------------------------------------------
    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("MastTop_Anchor", root, _pose_pt((0.0, 2.86, 0.0)))
    r1kit.create_empty("Camera_Anchor", root, _pose_pt((cam_x, 1.66, cam_z)))
    r1kit.create_empty("Scoop_Anchor", root, _pose_pt(tip_u))
    r1kit.create_empty("RemovedSpot_1_Anchor", root, _pose_pt(s1_center))
    r1kit.create_empty("RemovedSpot_2_Anchor", root, _pose_pt(s2_center))

    decal_n_b = _TILT_MAT_B @ Vector((0.0, 1.0, 0.0))
    decal_up_b = _TILT_MAT_B @ Vector((0.0, 0.0, 1.0))
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_VIS_1",
        cell_key="VIS_1967",
        center_u=_pose_pt((box_a_center[0], box_a_center[1] + 0.140, box_a_center[2] + 0.170)),
        normal_u=(float(decal_n_b.x), float(decal_n_b.z), float(decal_n_b.y)),
        up_u=(float(decal_up_b.x), float(decal_up_b.z), float(decal_up_b.y)),
        width=0.175,
        height=0.1225,
        v_crop=(0.20, 0.90),
    )

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.65)
    _boost_weathering(root, extra_dust=0.28, extra_wear=0.18, darken_ao=0.88)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def _boost_weathering(
    root: bpy.types.Object,
    extra_dust: float = 0.20,
    extra_wear: float = 0.12,
    darken_ao: float = 0.88,
) -> None:
    """Enhance vertex-color edge wear (G) and dust (B) for decades-old lunar weathering (chalking white paint toward grey-tan)."""
    strut_meshes = {"Spaceframe", "LandingLegs", "MastAndSolar", "ScoopArm", "RemovedPartSpots"}
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or not obj.data.color_attributes or obj.name.startswith("Decal_"):
            continue
        is_strut_mesh = obj.name in strut_meshes
        attr = obj.data.color_attributes[0]
        for item in attr.data:
            r, g, b, a = item.color
            new_g = (g * 0.52) if is_strut_mesh else (g * 1.18 + extra_wear)
            item.color = (
                max(0.16, min(1.0, r * darken_ao)),
                max(0.0, min(1.0, new_g)),
                max(0.0, min(1.0, b + extra_dust)),
                a,
            )


# ===========================================================================
# 2. Decal_OldLanderBlast.fbx (Small, soft, slightly brighter disturbed-regolith
#    patch with the three footpad scuffs)
# ===========================================================================
def build_decal_old_lander_blast(out_fbx: Path) -> None:
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Decal_OldLanderBlast", None, (0.0, 0.0, 0.0))

    # -----------------------------------------------------------------------
    # A. BlastPatch (Soft, organic circular/lobed disturbed-regolith patch under
    #    the retro-thruster and 3 legs; R1_CreamEnamel overridden in Unity with
    #    R1_OldLanderBlastDecal so it reads as slightly brighter disturbed regolith)
    # -----------------------------------------------------------------------
    patch = r1kit.MeshBuilder("BlastPatch", pivot_u=(0.0, 0.0, 0.0))

    # Central disturbed regolith disc
    patch.add_cylinder(
        center_u=(0.0, 0.007, -0.06),
        radius=1.46,
        radius_top=1.24,
        length=0.018,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=28,
        bevel=0.005,
        bevel_segments=1,
    )
    # Inner swept annular blast ring directly under the retro nozzle
    patch.add_torus(
        center_u=(0.0, 0.012, 0.0),
        major_radius=0.50,
        minor_radius=0.014,
        mat="R1_CreamEnamel",
        axis_u="Y",
        major_segments=20,
        minor_segments=8,
    )
    # Three soft round disturbed-regolith lobes centered under the 3 landing footpads
    lobe_centers = (
        (0.96, 0.006, 0.55, 0.62),
        (-0.96, 0.006, 0.55, 0.58),
        (0.0, 0.006, -1.10, 0.58),
    )
    for lx, ly, lz, lrad in lobe_centers:
        patch.add_cylinder(
            center_u=(lx, ly, lz),
            radius=lrad,
            radius_top=lrad * 0.78,
            length=0.016,
            mat="R1_CreamEnamel",
            axis_u="Y",
            segments=20,
            bevel=0.005,
            bevel_segments=1,
        )

    patch.build_object(parent=root)

    # -----------------------------------------------------------------------
    # B. FootpadScuffs (Three distinct footpad skid/crush scuffs & displaced
    #    regolith rims matching Prop_OldLander's 3 footpads)
    # -----------------------------------------------------------------------
    scuffs = r1kit.MeshBuilder("FootpadScuffs", pivot_u=(0.0, 0.01, 0.0))

    scuff_configs = (
        # (azimuth_deg, pad_xz, is_deep_buried)
        (60.0, (1.08, 0.62), True),     # Front-Right: deep buried footpad scuff
        (-60.0, (-1.08, 0.62), False),  # Front-Left: surface footpad scuff
        (180.0, (0.0, -1.24), False),   # Rear: surface footpad scuff
    )
    for az_deg, (px, pz), is_deep in scuff_configs:
        az_rad = math.radians(az_deg)
        rad_x = math.sin(az_rad)
        rad_z = math.cos(az_rad)

        skid_x = px - rad_x * 0.16
        skid_z = pz - rad_z * 0.16
        scuffs.add_bevel_box(
            center_u=(skid_x, 0.011, skid_z),
            size_u=(0.32, 0.016, 0.46),
            mat="R1_Basalt",
            bevel=0.005,
            segments=1,
            rot_deg_u=(0.0, az_deg, 0.0),
            top_scale_xz=(0.76, 0.82),
        )
        rim_h = 0.028 if is_deep else 0.018
        rim_r = 0.34 if is_deep else 0.30
        scuffs.add_torus(
            center_u=(px, 0.011 + rim_h * 0.35, pz),
            major_radius=rim_r,
            minor_radius=rim_h,
            mat="R1_Basalt" if is_deep else "R1_CreamEnamel",
            axis_u="Y",
            major_segments=18,
            minor_segments=8,
            arc_deg=230.0 if is_deep else 190.0,
            start_deg=az_deg - 95.0,
        )

    scuffs.build_object(parent=root)

    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.85)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def main() -> None:
    repo_root = HERE.parents[3]
    props_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"
    props_dir.mkdir(parents=True, exist_ok=True)

    build_prop_old_lander(props_dir / "Prop_OldLander.fbx")
    build_decal_old_lander_blast(props_dir / "Decal_OldLanderBlast.fbx")


if __name__ == "__main__":
    main()
