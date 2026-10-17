"""Build HabitatModules.fbx (R1 Lumen Station ruins — 3 connected pressurized modules).

Includes:
- ModuleA_Main: Smooth 36-segment pressurized cylinder with domed front bulkhead, round/arched
  airlock door, recessed keypad box beside it, one warm-orange horizontal stripe band, and
  umbilical ground cables.
- AirlockPanel: Separate child mesh node mounted above the round airlock door with R1_Screen
  displaying `SEALED · 1988 · STANDBY`.
- BellowsJoints: Two solid, non-see-through dark rubber accordion bellows joints with smooth
  36-segment steel collars and tie-lugs.
- ModuleB_Lab: Smooth 36-segment pressurized cylinder with one warm-orange horizontal band,
  riveted sage patch, trapezoidal footings, and one readable `SEC-04 HABITAT & LAB` stencil sign.
- ModuleC_Crushed: Smooth 36-segment pressurized cylinder with domed end cap, one warm-orange
  horizontal band, trapezoidal footings, and caved-in structural hull dents (weathering in
  vertex colours; no coloured squares).
- Scan_1, Scan_2, Scan_3: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Callable, Optional, Tuple

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _add_trapezoidal_footings(
    mb: r1kit.MeshBuilder,
    x_positions: tuple[float, ...],
    z_center: float,
    radius: float,
    cy: float,
    axis: str = "x",
) -> None:
    """Add low, sturdy angled trapezoidal support footings along the bottom of a habitat cylinder."""
    for pos in x_positions:
        for side in (-1.0, 1.0):
            if axis == "x":
                fx = pos
                fz = z_center + side * (radius * 0.72)
                rot = (side * -14.0, 0.0, 0.0)
                pad_size = (0.46, 0.10, 0.44)
                leg_size = (0.38, 0.62, 0.34)
                pad_pos = (fx, 0.05, fz + side * 0.10)
                leg_pos = (fx, 0.32, fz)
            else:
                fx = z_center + side * (radius * 0.72)
                fz = pos
                rot = (0.0, 0.0, side * 14.0)
                pad_size = (0.44, 0.10, 0.46)
                leg_size = (0.34, 0.62, 0.38)
                pad_pos = (fx + side * 0.10, 0.05, fz)
                leg_pos = (fx, 0.32, fz)

            mb.add_bevel_box(
                leg_pos,
                leg_size,
                bevel=0.02,
                mat="R1_CreamEnamel",
                rot_deg_u=rot,
                top_scale_xz=(0.82, 0.82),
                segs=1,
            )
            mb.add_bevel_box(
                pad_pos,
                pad_size,
                bevel=0.015,
                mat="R1_GreyMetal",
                segs=1,
            )


def _add_module_orange_band(
    mb: r1kit.MeshBuilder,
    cyl_center_u: Tuple[float, float, float],
    radius: float,
    bays: tuple[Tuple[float, float], ...],
    axis: str = "x",
    segs: int = 32,
    dent_fn: Optional[Callable[[float, float, float], Tuple[float, float, float]]] = None,
) -> None:
    """Add ONE horizontal warm-orange stripe band (plus thin upper pin-stripe) along the cylinder sides."""
    if axis.lower() == "z":
        main_angles = ((158.0, 175.0),)
        pin_angles = ((179.0, 181.5),)
    else:
        main_angles = ((-85.0, -68.0), (68.0, 85.0))
        pin_angles = ((-90.0, -87.5), (87.5, 90.0))

    for t0, t1 in bays:
        for ang0, ang1 in main_angles:
            ruins_common.add_curved_cyl_band(
                mb,
                cyl_center_u=cyl_center_u,
                radius=radius,
                thickness=0.014,
                t0=t0,
                t1=t1,
                ang0_deg=ang0,
                ang1_deg=ang1,
                mat="R1_OrangeStripe",
                axis=axis,
                segs=segs,
                axial_segs=1,
                clip_min_y_u=None,
                dent_fn=dent_fn,
            )
        for ang0, ang1 in pin_angles:
            ruins_common.add_curved_cyl_band(
                mb,
                cyl_center_u=cyl_center_u,
                radius=radius,
                thickness=0.012,
                t0=t0,
                t1=t1,
                ang0_deg=ang0,
                ang1_deg=ang1,
                mat="R1_OrangeStripe",
                axis=axis,
                segs=segs,
                axial_segs=1,
                clip_min_y_u=None,
                dent_fn=dent_fn,
            )


def _build_main_module(root) -> None:
    """Build ModuleA_Main: leftmost front airlock dome module with round airlock door & keypad box."""
    mb = r1kit.MeshBuilder()
    cx = -3.45
    cy = 1.60
    cz = -0.08
    rad = 1.54
    segs = 32

    # Smooth 32-segment main pressurized cylinder with domed front & rear bulkheads (aligned along Z)
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(cx, cy, cz),
        radius=rad,
        total_length=2.68,
        dome_depth=0.34,
        mat="R1_CreamEnamel",
        axis="z",
        segs=segs,
        barrel_rings=1,
        cap_rings=3,
        clip_min_y_u=None,
    )

    # Thick smooth 32-segment dark-grey perimeter collar rings at front (-Z) and rear (+Z)
    for tz in (-1.02, 1.02):
        ruins_common.add_curved_cyl_band(
            mb,
            cyl_center_u=(cx, cy, cz),
            radius=rad,
            thickness=0.062,
            t0=tz - 0.13,
            t1=tz + 0.13,
            ang0_deg=0.0,
            ang1_deg=360.0,
            mat="R1_GreyMetal",
            axis="z",
            segs=segs,
            clip_min_y_u=None,
        )

    # ONE horizontal warm-orange stripe band along the outer side of ModuleA_Main
    _add_module_orange_band(
        mb,
        cyl_center_u=(cx, cy, cz),
        radius=rad,
        bays=((-0.86, 0.86),),
        axis="z",
        segs=segs,
    )

    # Side transition tunnel on the right (+X) connecting ModuleA_Main into Bellows 1 at x = -1.98
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(-2.32, cy, 0.05),
        radius=1.26,
        total_length=0.84,
        dome_depth=0.06,
        mat="R1_CreamEnamel",
        axis="x",
        segs=24,
        barrel_rings=1,
        cap_rings=2,
        clip_min_y_u=None,
    )

    # Low trapezoidal footings under ModuleA_Main
    _add_trapezoidal_footings(mb, (-0.58, 0.48), cx, rad, cy, axis="z")

    # Front Domed Bulkhead Seams (vertical center seam above door + horizontal mid-seam + top bolts)
    front_z = cz - 1.31
    mb.add_bevel_box((cx, 2.80, front_z), (0.028, 0.42, 0.08), bevel=0.004, mat="R1_GreyMetal", segs=1)
    for bx_off in (-0.08, 0.08):
        mb.add_Detail_Bolt((cx + bx_off, 2.94, front_z - 0.04), radius=0.022, height=0.018, axis="z", mat="R1_BareSteel")
    for sx in (-1.0, 1.0):
        mb.add_bevel_box((cx + sx * 1.04, 1.56, front_z + 0.04), (0.56, 0.026, 0.10), bevel=0.004, mat="R1_GreyMetal", segs=1)

    # Round / Arched Airlock Door centered on the lower front bulkhead (1.10 x 2.05 m clear leaf, 1.36 x 2.20 m outer frame)
    door_z = cz - 1.29
    # Outer thick dark-grey rounded-arch door bezel frame (24 smooth arch segments)
    mb.add_arch_housing(
        base_center_u=(cx, 0.03, door_z),
        width_x=1.36,
        height_y=2.20,
        depth_z=0.18,
        arch_radius=0.67,
        mat="R1_GreyMetal",
        bevel=0.022,
        arch_segments=12,
        arch_axis_u="X",
    )
    # Recessed cream-enamel arched door leaf (1.10 m W x 2.05 m H clear)
    mb.add_arch_housing(
        base_center_u=(cx, 0.05, door_z - 0.03),
        width_x=1.10,
        height_y=2.05,
        depth_z=0.15,
        arch_radius=0.54,
        mat="R1_CreamEnamel",
        bevel=0.016,
        arch_segments=12,
        arch_axis_u="X",
    )
    # Vertical center door split seam + central circular locking mechanism (wheel bezel & hub)
    mb.add_bevel_box((cx, 1.06, door_z - 0.09), (0.024, 1.94, 0.04), bevel=0.003, mat="R1_GreyMetal", segs=1)
    mb.add_ring(
        (cx, 1.08, door_z - 0.10),
        r_outer=0.22,
        r_inner=0.14,
        height=0.05,
        segs=24,
        axis="z",
        mat="R1_GreyMetal",
    )
    mb.add_cylinder(
        (cx, 1.08, door_z - 0.10),
        radius=0.14,
        height=0.05,
        segs=20,
        axis="z",
        mat="R1_CreamEnamel",
        bevel=0.008,
    )
    mb.add_bevel_box((cx - 0.03, 1.08, door_z - 0.125), (0.12, 0.036, 0.03), bevel=0.005, mat="R1_Copper", segs=1)

    # Recessed Keypad Box beside the round airlock door on the left, centered at Z = 1.25 m
    kx, ky, kz = cx - 0.90, 1.25, door_z + 0.05
    mb.add_bevel_box((kx, ky, kz), (0.36, 0.48, 0.14), bevel=0.016, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((kx, ky + 0.02, kz - 0.04), (0.28, 0.38, 0.08), bevel=0.010, mat="R1_CreamEnamel", segs=1)
    # Upper small dark status slot & amber LED
    mb.add_bevel_box((kx, ky + 0.12, kz - 0.075), (0.22, 0.11, 0.03), bevel=0.004, mat="R1_DarkGlass", segs=1)
    mb.add_bevel_box((kx - 0.05, ky + 0.13, kz - 0.088), (0.07, 0.022, 0.02), bevel=0.003, mat="R1_LampAmber", segs=1)
    # Angled lower keypad shelf with 3x3 tactile keys
    mb.add_bevel_box(
        (kx, ky - 0.09, kz - 0.085),
        (0.22, 0.18, 0.08),
        bevel=0.008,
        mat="R1_GreyMetal",
        rot_deg_u=(-26.0, 0.0, 0.0),
        segs=1,
    )
    for row_i in range(3):
        for col_i in range(3):
            bx = kx + (col_i - 1) * 0.055
            by = ky - 0.045 - row_i * 0.045
            bz = kz - 0.115 - row_i * 0.018
            mb.add_bevel_box(
                (bx, by, bz),
                (0.036, 0.028, 0.025),
                bevel=0.003,
                mat="R1_CreamEnamel",
                rot_deg_u=(-26.0, 0.0, 0.0),
                segs=1,
            )

    # Two dark rubber umbilical cables emerging from the lower-right of ModuleA_Main into the regolith
    for c_idx, z_off in enumerate((-0.72, -0.52)):
        pts = [
            (cx + 1.15, 0.36, z_off),
            (cx + 1.42, 0.22, z_off - 0.14),
            (cx + 1.72, 0.10, z_off - 0.28),
            (cx + 2.02, 0.05, z_off - 0.42 - c_idx * 0.06),
        ]
        mb.add_tube_path(pts, radius=0.042, mat="R1_Rubber", segments=10, cap_ends=True)

    ruins_common.to_ruin_object(mb, "ModuleA_Main", parent=root)


def _build_airlock_panel(root) -> None:
    """Build the separate `AirlockPanel` child mesh node (`SEALED · 1988 · STANDBY` / `SUIT 01-04 · OFFLINE` above the round airlock door)."""
    mb = r1kit.MeshBuilder()
    px, py, pz = -3.45, 2.46, -1.36

    # Outer dark-grey bezel housing
    mb.add_bevel_box((px, py, pz), (1.20, 0.35, 0.12), bevel=0.022, mat="R1_GreyMetal", segs=1)
    # Recessed bezel gasket trim (uses R1_GreyMetal so AirlockPanel has 2 material slots: R1_GreyMetal + R1_Signage)
    mb.add_bevel_box((px, py, pz - 0.045), (1.10, 0.26, 0.05), bevel=0.010, mat="R1_GreyMetal", segs=1)
    # Status display plate using R1_Signage AIRLOCK_STATUS cell ("SEALED · 1988 · STANDBY" / "SUIT 01-04 · OFFLINE")
    ruins_common.add_signage_plate(
        mb,
        center_u=(px, py, pz - 0.064),
        width=1.04,
        height=0.22,
        cell_name="AIRLOCK_STATUS",
        rot_deg_u=(0.0, 180.0, 0.0),
        thickness=0.024,
        frame_mat=None,
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "AirlockPanel", parent=root)


def _build_airlock_readout(root) -> None:
    """Build the dedicated B7 `AirlockReadout` CRT/LED panel surface beside the round airlock door at Rover 07 eye level."""
    mb = r1kit.MeshBuilder()
    kx, ky, kz = -4.35, 1.37, -1.392
    ruins_common.add_signage_plate(
        mb,
        center_u=(kx, ky, kz),
        width=0.22,
        height=0.11,
        cell_name="AIRLOCK_STATUS",
        rot_deg_u=(0.0, 180.0, 0.0),
        thickness=0.016,
        frame_mat="R1_Brass",
        rivets=False,
    )
    ruins_common.to_ruin_object(mb, "AirlockReadout", parent=root)


def _build_bellows_joints(root) -> None:
    """Build two solid, non-see-through dark rubber accordion bellows joints with smooth 32-seg collars."""
    mb = r1kit.MeshBuilder()
    cy = 1.60
    segs = 32

    for bx, r_val, r_pk, r_col in ((-1.52, 1.32, 1.45, 1.60), (2.12, 1.30, 1.43, 1.58)):
        # Solid, watertight revolved rubber accordion rings (nothing see-through)
        ruins_common.add_accordion_bellows(
            mb,
            center_u=(bx, cy, 0.0),
            length=0.94,
            r_valley=r_val,
            r_peak=r_pk,
            num_pleats=3,
            mat="R1_Rubber",
            axis="x",
            segs=segs,
            clip_min_y_u=None,
        )
        # Smooth 32-segment dark-grey structural end collars framing the bellows
        for end_off in (-0.44, 0.44):
            ruins_common.add_curved_cyl_band(
                mb,
                cyl_center_u=(bx, cy, 0.0),
                radius=r_col - 0.055,
                thickness=0.060,
                t0=end_off - 0.08,
                t1=end_off + 0.08,
                ang0_deg=0.0,
                ang1_deg=360.0,
                mat="R1_GreyMetal",
                axis="x",
                segs=segs,
                clip_min_y_u=None,
            )
        # Short horizontal collar tie-lugs bridging the dark grey rings (matching ruins_v1_dawn.png)
        for ang_deg in (-145.0, -120.0, -95.0, -55.0, 55.0, 95.0, 120.0, 145.0):
            ang = math.radians(ang_deg)
            ly = cy - (r_col - 0.02) * math.cos(ang)
            lz = (r_col - 0.02) * math.sin(ang)
            if ly > 0.25:
                mb.add_bevel_box(
                    (bx, ly, lz),
                    (0.92, 0.055, 0.055),
                    bevel=0.008,
                    mat="R1_GreyMetal",
                    segs=1,
                )

    ruins_common.to_ruin_object(mb, "BellowsJoints", parent=root)


def _build_lab_module(root) -> None:
    """Build ModuleB_Lab: middle smooth 32-segment cylinder with one orange band, sage patch & SEC-04 sign."""
    mb = r1kit.MeshBuilder()
    cx = 0.30
    cy = 1.60
    rad = 1.54
    segs = 32

    # Smooth 32-segment cream pressure hull with rounded end caps
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(cx, cy, 0.0),
        radius=rad,
        total_length=2.82,
        dome_depth=0.22,
        mat="R1_CreamEnamel",
        axis="x",
        segs=segs,
        barrel_rings=1,
        cap_rings=2,
        clip_min_y_u=None,
    )

    # Smooth 32-segment dark-grey stiffening rings at left, center, and right
    for rx in (-0.94, 0.0, 0.94):
        ruins_common.add_curved_cyl_band(
            mb,
            cyl_center_u=(cx, cy, 0.0),
            radius=rad,
            thickness=0.055,
            t0=rx - 0.09,
            t1=rx + 0.09,
            ang0_deg=0.0,
            ang1_deg=360.0,
            mat="R1_GreyMetal",
            axis="x",
            segs=segs,
            clip_min_y_u=None,
        )

    # ONE horizontal warm-orange stripe band across both bays of ModuleB_Lab
    _add_module_orange_band(
        mb,
        cyl_center_u=(cx, cy, 0.0),
        radius=rad,
        bays=((-0.85, -0.09), (0.09, 0.85)),
        axis="x",
        segs=segs,
    )

    # Curved riveted sage patch below the warm-orange stripe on the left bay (matching identity_v1_helios_lumen.png)
    ruins_common.add_curved_cyl_band(
        mb,
        cyl_center_u=(cx, cy, 0.0),
        radius=rad,
        thickness=0.016,
        t0=-0.64,
        t1=-0.24,
        ang0_deg=-65.0,
        ang1_deg=-48.0,
        mat="R1_SagePatch",
        axis="x",
        segs=segs,
        clip_min_y_u=None,
    )

    # Low angled trapezoidal support footings
    _add_trapezoidal_footings(mb, (-0.18, 0.78), 0.0, rad, cy, axis="x")

    # ONE stencil signage board on the Habitat building: `SEC-04 HABITAT & LAB` above the orange band
    sign_x = cx - 0.46
    sign_y = cy + 0.48
    sign_z = -rad + 0.06
    mb.add_bevel_box(
        (sign_x, sign_y, sign_z + 0.08),
        (1.06, 0.50, 0.18),
        bevel=0.014,
        mat="R1_GreyMetal",
        rot_deg_u=(-18.0, 180.0, 0.0),
        segs=1,
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(sign_x, sign_y, sign_z - 0.02),
        width=1.00,
        height=0.44,
        cell_name="SEC04_HABITAT",
        rot_deg_u=(-18.0, 180.0, 0.0),
    )

    ruins_common.to_ruin_object(mb, "ModuleB_Lab", parent=root)


def _crushed_dent_offset(ux: float, uy: float, uz: float) -> Tuple[float, float, float]:
    """Smooth inward structural buckling on the upper roof & upper-front shoulder of ModuleC_Crushed."""
    if uy <= 0.15:
        return (0.0, 0.0, 0.0)

    # Primary roof-and-front impact basin centered around ux = -0.15, uz = -0.45
    d1_sq = ((ux + 0.15) / 0.92) ** 2 + ((uz + 0.45) / 0.88) ** 2
    dent1 = 0.36 * math.exp(-d1_sq * 1.65)

    # Secondary crease basin centered around ux = +0.55, uz = -0.25
    d2_sq = ((ux - 0.55) / 0.68) ** 2 + ((uz + 0.25) / 0.78) ** 2
    dent2 = 0.24 * math.exp(-d2_sq * 1.85)

    top_weight = min(1.0, max(0.0, (uy - 0.15) / 0.75))
    inward = (dent1 + dent2) * top_weight

    r_yz = math.hypot(uy, uz)
    if r_yz < 1e-4:
        return (0.0, -inward, 0.0)
    return (0.0, -inward * (uy / r_yz) * 1.12, -inward * (uz / r_yz) * 0.82)


def _build_crushed_module(root) -> None:
    """Build ModuleC_Crushed: rightmost 32-seg cylinder with domed end cap, one orange band & roof dents."""
    mb = r1kit.MeshBuilder()
    cx = 4.02
    cy = 1.56
    rad = 1.52
    segs = 32

    # Smooth 32-segment cylinder with domed right end-cap and smooth structural dents (no coloured squares)
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(cx, cy, 0.0),
        radius=rad,
        total_length=2.96,
        dome_depth=0.44,
        mat="R1_CreamEnamel",
        axis="x",
        segs=segs,
        barrel_rings=5,
        cap_rings=2,
        clip_min_y_u=None,
        dent_fn=_crushed_dent_offset,
    )

    # Smooth 32-segment dark-grey stiffening rings (deformed by the same dent function)
    for rx in (-1.04, -0.06, 0.94):
        ruins_common.add_curved_cyl_band(
            mb,
            cyl_center_u=(cx, cy, 0.0),
            radius=rad,
            thickness=0.055,
            t0=rx - 0.09,
            t1=rx + 0.09,
            ang0_deg=0.0,
            ang1_deg=360.0,
            mat="R1_GreyMetal",
            axis="x",
            segs=segs,
            axial_segs=1,
            clip_min_y_u=None,
            dent_fn=_crushed_dent_offset,
        )

    # ONE horizontal warm-orange stripe band across both bays of ModuleC_Crushed
    _add_module_orange_band(
        mb,
        cyl_center_u=(cx, cy, 0.0),
        radius=rad,
        bays=((-0.95, -0.15), (0.03, 0.85)),
        axis="x",
        segs=segs,
        dent_fn=_crushed_dent_offset,
    )

    # Low angled trapezoidal support footings
    _add_trapezoidal_footings(mb, (3.42, 4.62), 0.0, rad, cy, axis="x")

    ruins_common.to_ruin_object(mb, "ModuleC_Crushed", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("HabitatModules")

    _build_main_module(root)
    _build_airlock_panel(root)
    _build_airlock_readout(root)
    _build_bellows_joints(root)
    _build_lab_module(root)
    _build_crushed_module(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (-3.45, 2.46, -1.44), parent=root)  # AirlockPanel (`SEALED · 1988 · STANDBY`)
    r1kit.add_empty("Scan_2", (3.88, 2.68, -0.45), parent=root)   # Dented/crushed module upper hull
    r1kit.add_empty("Scan_3", (-0.16, 2.04, -1.46), parent=root)  # `SEC-04 HABITAT & LAB` stencil signage

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="habitat_modules",
        fbx_name="HabitatModules.fbx",
        footprint_radius=6.2,
        cam_angle_deg=26.0,
        cam_elev_deg=16.0,
        skip_renders=skip_renders,
    )
    print(f"[habitat_modules] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")


if __name__ == "__main__":
    build(skip_renders="--skip-renders" in sys.argv)
