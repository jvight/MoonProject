"""Build FallenSolarArray.fbx (R1 Lumen Station ruins — two broken wings, toppled gearbox, cable stub).

Includes:
- ToppledGearbox: Sheared anchor plinth and toppled heliostat tracking gearbox with exposed
  brass ring gear (R1_Brass), copper drive coils (R1_Copper), gold foil thermal wrap (R1_GoldFoil),
  fault beacon (R1_LampAmber), and LUMEN / SEC_04 / HAZARD stencil signage.
- SolarWingLeft: 3-bay articulated solar wing on the ground with deep-indigo photovoltaic cell
  arrays (R1_SolarCell), silver busbar strips, rear truss ribs, and one buckled sub-panel
  exposing gold Kapton backing.
- SolarWingRight: Snapped right solar wing lying canted/partially twisted on the ground, showing
  both photovoltaic cells and gold foil backing + structural space-frame ribs.
- CableStub: Heavy severed power umbilical stub (R1_Rubber conduit, R1_Brass collar, frayed
  R1_Copper conductor bundle) emerging from a ground junction box.
- Scan_1, Scan_2, Scan_3: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _build_toppled_gearbox(root) -> None:
    mb = r1kit.MeshBuilder()

    # Sheared anchor pedestal base on the ground at (0, 0, 0.35)
    mb.add_cylinder((0.0, 0.10, 0.35), radius=0.68, height=0.20, segs=16, axis="y", mat="R1_GreyMetal", bevel=0.025)
    mb.add_ring((0.0, 0.22, 0.35), r_outer=0.52, r_inner=0.34, height=0.10, segs=16, axis="y", mat="R1_BareSteel")
    for a_deg in range(0, 360, 45):
        a = math.radians(a_deg)
        mb.add_Detail_Bolt((0.58 * math.cos(a), 0.21, 0.35 + 0.58 * math.sin(a)), radius=0.028, height=0.03, axis="y", mat="R1_BareSteel")

    # Lower snapped pedestal column stump
    mb.add_cylinder((0.0, 0.42, 0.32), radius=0.34, height=0.38, segs=16, axis="y", mat="R1_CreamEnamel", bevel=0.02, rot_deg_u=(-14.0, 0.0, 0.0))

    # Toppled Gearbox Housing pitched forward toward -Z (from z=+0.15 down to z=-0.95, resting on ground cradle)
    mb.add_bevel_box((0.0, 0.16, -0.78), (0.86, 0.32, 0.64), bevel=0.02, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box(
        (0.0, 0.56, -0.36),
        (0.92, 0.68, 1.32),
        bevel=0.03,
        mat="R1_CreamEnamel",
        rot_deg_u=(24.0, 0.0, -5.0),
        segs=2,
    )
    mb.add_bevel_box(
        (0.0, 0.62, -0.58),
        (0.96, 0.24, 0.46),
        bevel=0.02,
        mat="R1_OrangeStripe",
        rot_deg_u=(24.0, 0.0, -5.0),
        segs=1,
    )

    # Exposed Rotary Drive Ring Gear (R1_Brass) & Worm Shaft (R1_BareSteel) + Copper motor coils
    mb.add_cylinder((0.0, 0.84, -0.28), radius=0.38, height=0.16, segs=20, axis="y", mat="R1_Brass", bevel=0.015, rot_deg_u=(24.0, 0.0, -5.0))
    for tooth_i in range(12):
        ta = math.radians(tooth_i * 30.0)
        tx = 0.40 * math.cos(ta)
        tz = -0.28 + 0.40 * math.sin(ta)
        mb.add_bevel_box((tx, 0.84, tz), (0.07, 0.12, 0.07), bevel=0.008, mat="R1_Brass", rot_deg_u=(24.0, tooth_i * 30.0, 0.0), segs=1)

    mb.add_cylinder((0.0, 0.72, -0.82), radius=0.22, height=0.44, segs=14, axis="z", mat="R1_Copper", bevel=0.015, rot_deg_u=(24.0, 0.0, -5.0))
    mb.add_bevel_box((0.0, 0.78, 0.08), (0.64, 0.44, 0.36), bevel=0.018, mat="R1_GoldFoil", rot_deg_u=(24.0, 0.0, -5.0), segs=1)

    # Transverse wing drive axle shaft & broken yokes on left (-X) and right (+X)
    mb.add_cylinder((0.0, 0.58, -0.45), radius=0.11, height=1.68, segs=14, axis="x", mat="R1_BareSteel", bevel=0.015, rot_deg_u=(0.0, 0.0, -6.0))
    # Fault indicator beacon (R1_LampAmber) on top-front of gearbox
    mb.add_cylinder((0.32, 0.86, -0.68), radius=0.055, height=0.12, segs=10, axis="y", mat="R1_LampAmber", bevel=0.01)

    # Stencil Signage on front & top of toppled gearbox
    ruins_common.add_signage_plate(
        mb,
        center_u=(-0.18, 0.52, -1.02),
        width=0.42,
        height=0.24,
        cell_name="LUMEN",
        rot_deg_u=(24.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.24, 0.52, -1.02),
        width=0.28,
        height=0.22,
        cell_name="SEC_04",
        rot_deg_u=(24.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.0, 0.28, -1.11),
        width=0.76,
        height=0.12,
        cell_name="HAZARD",
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat=None,
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "ToppledGearbox", parent=root)


def _build_solar_wing(
    root,
    name: str,
    side_sign: float,
    wing_tilt_deg: tuple[float, float, float],
    show_foil_bay: int,
) -> None:
    """Build a 3-bay broken solar array wing resting on the ground (`y >= 0`)."""
    from mathutils import Euler, Vector

    mb = r1kit.MeshBuilder()

    # 3 articulated bays extending outward along X from x = side_sign * 0.88 to side_sign * 4.25
    bay_centers_x = [side_sign * 1.42, side_sign * 2.52, side_sign * 3.62]
    bay_ys = [0.38, 0.28, 0.22]
    bay_zs = [-0.32, -0.18, -0.04]
    bay_w = 1.02
    bay_d = 1.88

    # Ground rest blocks under outer wing corners so geometry sits cleanly on regolith (y >= 0)
    for bx, bz in ((bay_centers_x[0], bay_zs[0]), (bay_centers_x[2], bay_zs[2])):
        mb.add_bevel_box((bx, 0.09, bz), (0.64, 0.18, 1.32), bevel=0.015, mat="R1_GreyMetal", segs=1)

    for bi in range(3):
        bx = bay_centers_x[bi]
        by = bay_ys[bi]
        bz = bay_zs[bi]
        # Slight per-bay buckle angle where hinges bent on impact
        buckle_z = side_sign * (5.0 if bi == 0 else (-3.5 if bi == 1 else 4.5))
        rot = (wing_tilt_deg[0] + bi * 1.5, wing_tilt_deg[1] + bi * side_sign * 2.5, wing_tilt_deg[2] + buckle_z)
        rot_b = Euler(
            (
                math.radians(rot[0]),
                math.radians(rot[2]),
                math.radians(-rot[1]),
            ),
            "XYZ",
        ).to_matrix()

        def bay_pt(lx: float, ly: float, lz: float) -> tuple[float, float, float]:
            rb = rot_b @ Vector((lx, lz, ly))
            return (bx + rb.x, max(0.04, by + rb.z), bz + rb.y)

        # Structural perimeter frame of the wing bay
        mb.add_bevel_box(bay_pt(0.0, 0.0, 0.0), (bay_w, 0.07, bay_d), bevel=0.012, mat="R1_GreyMetal", rot_deg_u=rot, segs=1)
        # Orange/Cream end-cap bumper rails at front & back edges of the wing
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                bay_pt(0.0, 0.015, sz * (bay_d * 0.5 + 0.03)),
                (bay_w * 0.96, 0.085, 0.07),
                bevel=0.01,
                mat="R1_OrangeStripe" if bi == 2 else "R1_CreamEnamel",
                rot_deg_u=rot,
                segs=1,
            )

        # Rear structural cross-ribs & gold Kapton backing sheet
        mb.add_bevel_box(bay_pt(0.0, -0.025, 0.0), (bay_w * 0.92, 0.03, bay_d * 0.92), bevel=0.005, mat="R1_GoldFoil", rot_deg_u=rot, segs=1)
        for rz_off in (-0.56, 0.0, 0.56):
            mb.add_bevel_box(bay_pt(0.0, -0.038, rz_off), (bay_w * 0.96, 0.045, 0.07), bevel=0.006, mat="R1_BareSteel", rot_deg_u=rot, segs=1)

        # Top surface: 2x3 sub-grid of deep-indigo R1_SolarCell wafers + silver busbars
        for gx in (-0.24, 0.24):
            for gz in (-0.56, 0.0, 0.56):
                is_peeled = (bi == show_foil_bay and gz < 0.0 and gx * side_sign > 0.0)
                cell_mat = "R1_GoldFoil" if is_peeled else "R1_SolarCell"
                extra_rot = (rot[0] - 12.0, rot[1] + 8.0, rot[2] + side_sign * 10.0) if is_peeled else rot
                ly_cell = 0.065 if is_peeled else 0.038
                mb.add_bevel_box(
                    bay_pt(gx, ly_cell, gz),
                    (0.42, 0.024, 0.52),
                    bevel=0.004,
                    mat=cell_mat,
                    rot_deg_u=extra_rot,
                    segs=1,
                )
        # Silver collector busbar strip between cell columns & cross-straps
        mb.add_bevel_box(bay_pt(0.0, 0.042, 0.0), (0.045, 0.022, bay_d * 0.92), bevel=0.003, mat="R1_BareSteel", rot_deg_u=rot, segs=1)
        for gz_bar in (-0.28, 0.28):
            mb.add_bevel_box(bay_pt(0.0, 0.041, gz_bar), (bay_w * 0.90, 0.020, 0.032), bevel=0.003, mat="R1_BareSteel", rot_deg_u=rot, segs=1)

        # Inter-bay piano hinge lugs
        if bi < 2:
            hx = 0.5 * (bay_centers_x[bi] + bay_centers_x[bi + 1])
            hy = 0.5 * (bay_ys[bi] + bay_ys[bi + 1]) + 0.03
            hz = 0.5 * (bay_zs[bi] + bay_zs[bi + 1])
            for h_off in (-0.62, 0.0, 0.62):
                mb.add_cylinder((hx, hy, hz + h_off), radius=0.038, height=0.22, segs=10, axis="z", mat="R1_BareSteel", bevel=0.005)

    ruins_common.to_ruin_object(mb, name, parent=root)


def _build_cable_stub(root) -> None:
    """Build the severed power umbilical cable stub and ground junction box."""
    mb = r1kit.MeshBuilder()

    # Ground power junction box at (0.65, 0.14, -1.52)
    jx, jy, jz = 0.65, 0.14, -1.52
    mb.add_bevel_box((jx, jy, jz), (0.48, 0.28, 0.44), bevel=0.02, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((jx, jy + 0.15, jz), (0.42, 0.04, 0.38), bevel=0.008, mat="R1_OrangeStripe", segs=1)
    mb.add_cylinder((jx - 0.12, jy + 0.10, jz + 0.24), radius=0.065, height=0.14, segs=12, axis="z", mat="R1_Brass", bevel=0.008)

    # Segmented heavy armored rubber cable stub curving from junction box toward gearbox and snapping
    cable_pts = [
        (0.53, 0.18, -1.32),
        (0.42, 0.16, -1.16),
        (0.28, 0.14, -1.04),
        (-0.22, 0.12, -1.28),
        (-0.48, 0.11, -1.42),
    ]
    for i, (cx, cy, cz) in enumerate(cable_pts):
        yaw = -28.0 + i * 18.0
        mb.add_cylinder((cx, cy, cz), radius=0.058, height=0.26, segs=12, axis="z", mat="R1_Rubber", bevel=0.01, rot_deg_u=(0.0, yaw, 0.0))
        mb.add_ring((cx, cy, cz), r_outer=0.066, r_inner=0.050, height=0.04, segs=12, axis="z", mat="R1_BareSteel")

    # Severed cable end collar (R1_Brass) & splayed copper conductor strands (R1_Copper) at (-0.62, 0.12, -1.52)
    mb.add_cylinder((-0.58, 0.12, -1.48), radius=0.068, height=0.10, segs=12, axis="z", mat="R1_Brass", bevel=0.008, rot_deg_u=(0.0, 42.0, 0.0))
    for strand_i, (sx_off, sy_off, rot_s) in enumerate(((-0.04, 0.02, 28.0), (0.0, 0.04, 42.0), (0.04, 0.01, 56.0))):
        mb.add_cylinder(
            (-0.68 + sx_off, 0.12 + sy_off, -1.56),
            radius=0.018,
            height=0.18,
            segs=8,
            axis="z",
            mat="R1_Copper",
            bevel=0.003,
            rot_deg_u=(rot_s * 0.3, rot_s, 0.0),
        )

    ruins_common.to_ruin_object(mb, "CableStub", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("FallenSolarArray")

    _build_toppled_gearbox(root)
    _build_solar_wing(root, "SolarWingLeft", side_sign=-1.0, wing_tilt_deg=(8.0, -4.0, -5.0), show_foil_bay=1)
    _build_solar_wing(root, "SolarWingRight", side_sign=1.0, wing_tilt_deg=(-12.0, 8.0, 7.0), show_foil_bay=2)
    _build_cable_stub(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (0.0, 0.85, -0.45), parent=root)     # Toppled gearbox & brass ring gear
    r1kit.add_empty("Scan_2", (-0.58, 0.18, -1.48), parent=root)   # Severed umbilical cable stub
    r1kit.add_empty("Scan_3", (-2.52, 0.35, -0.18), parent=root)   # Fractured solar wing array

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="fallen_solar_array",
        fbx_name="FallenSolarArray.fbx",
        footprint_radius=4.5,
        cam_angle_deg=26.0,
        cam_elev_deg=25.0,
        skip_renders=skip_renders,
    )
    print(f"[fallen_solar_array] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")



if __name__ == "__main__":
    build()
