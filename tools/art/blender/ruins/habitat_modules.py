"""Build HabitatModules.fbx (R1 Lumen Station ruins — 3 connected pressurized modules).

Includes:
- ModuleA_Main: Sealed main pressurized cylinder on cradle footings with airlock vestibule,
  hatch door, ladder, roof radiator louvres, and SEC-04 HABITAT & LAB stencil signage.
- AirlockPanel: Separate child mesh node with R1_Screen status display beside the airlock door.
- BellowsJoints: Two corrugated expansion bellows joints with tie-rods and clamping rings.
- ModuleB_Lab: Secondary pressurized botany/lab cylinder with sage-green stripe, dome cap,
  and copper/brass umbilical manifold.
- ModuleC_Crushed: Depressurized crushed/holed module with buckled hull plates, exposed
  internal ring frames, torn gold Kapton MLI foil, and damaged cradle footing.
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


def _add_cradle_footing(mb: r1kit.MeshBuilder, x_u: float, z_u: float, radius: float, center_y: float, damaged: bool = False) -> None:
    """Add a heavy steel saddle cradle + twin legs + footing pads down to y_u = 0."""
    tilt_z = -7.5 if damaged else 0.0
    # Saddle arc beam under cylinder
    mb.add_bevel_box(
        (x_u, center_y - radius * 0.78, z_u),
        (0.32, 0.36, radius * 1.85),
        bevel=0.025,
        mat="R1_GreyMetal",
        rot_deg_u=(tilt_z, 0.0, 0.0),
        segs=1,
    )
    for sz in (-1.0, 1.0):
        leg_z = z_u + sz * (radius * 0.78)
        leg_h = center_y - radius * 0.65
        # Vertical/angled H-beam leg
        mb.add_bevel_box(
            (x_u, leg_h * 0.5 + 0.08, leg_z),
            (0.24, leg_h, 0.26),
            bevel=0.02,
            mat="R1_GreyMetal",
            rot_deg_u=(sz * -6.0 + tilt_z, 0.0, 0.0),
            segs=1,
        )
        # Diagonal knee brace
        mb.add_bevel_box(
            (x_u, leg_h * 0.68, leg_z - sz * 0.24),
            (0.14, leg_h * 0.55, 0.14),
            bevel=0.015,
            mat="R1_BareSteel",
            rot_deg_u=(sz * 28.0, 0.0, 0.0),
            segs=1,
        )
        # Footing pad on regolith at y_u = 0
        mb.add_cylinder(
            (x_u, 0.05, leg_z + sz * 0.06),
            radius=0.34,
            height=0.10,
            segs=8,
            axis="y",
            mat="R1_GreyMetal",
            bevel=0.015,
        )


def _build_main_module(root) -> None:
    mb = r1kit.MeshBuilder()
    cy = 1.92
    rad = 1.62
    # Main cylinder spans X = -1.10 to +4.10 (length 5.20m) along X axis
    cx = 1.50
    length = 5.20

    # Primary cream enamel pressure hull
    mb.add_cylinder((cx, cy, 0.0), radius=rad, height=length, segs=14, axis="x", mat="R1_CreamEnamel", bevel=0.04)

    # End bulkheads
    for ex in (-1.10, 4.10):
        mb.add_cylinder((ex, cy, 0.0), radius=rad * 0.94, height=0.22, segs=12, axis="x", mat="R1_GreyMetal", bevel=0.03)
        mb.add_ring((ex, cy, 0.0), r_outer=rad * 1.02, r_inner=rad * 0.88, height=0.14, segs=12, axis="x", mat="R1_BareSteel")

    # Warm orange identity stripe bands & structural stiffening rings along X
    for rx in (-0.75, 0.55, 2.35, 3.75):
        mb.add_ring((rx, cy, 0.0), r_outer=rad + 0.045, r_inner=rad - 0.02, height=0.18, segs=12, axis="x", mat="R1_GreyMetal")

    for ox in (-0.25, 3.25):
        mb.add_ring((ox, cy, 0.0), r_outer=rad + 0.018, r_inner=rad - 0.01, height=0.42, segs=12, axis="x", mat="R1_OrangeStripe")

    # Longitudinal upper & side stringer conduits
    for angle_deg in (35.0, 145.0, -25.0):
        a = math.radians(angle_deg)
        sy = cy + (rad + 0.04) * math.sin(a)
        sz = (rad + 0.04) * math.cos(a)
        mb.add_cylinder((cx, sy, sz), radius=0.035, height=length - 0.35, segs=8, axis="x", mat="R1_BareSteel", bevel=0.005)

    # Gold Kapton thermal blanket recessed bay on front-left & back
    mb.add_bevel_box((0.05, cy + 0.38, -rad + 0.04), (0.95, 0.62, 0.12), bevel=0.015, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((0.05, cy + 0.38, -rad - 0.01), (0.84, 0.50, 0.04), bevel=0.008, mat="R1_GoldFoil", segs=1)

    # Dark glass porthole windows on front (-Z) and back (+Z)
    for wx in (0.05, 3.05):
        for sz in (-1.0, 1.0):
            wz = sz * (rad - 0.02)
            mb.add_cylinder((wx, cy + 0.15, wz), radius=0.30, height=0.12, segs=10, axis="z", mat="R1_GreyMetal", bevel=0.018)
            mb.add_cylinder((wx, cy + 0.15, wz + sz * 0.03), radius=0.22, height=0.08, segs=10, axis="z", mat="R1_DarkGlass", bevel=0.008)

    # Roof ECS heat-exchanger unit & louvre rack
    mb.add_bevel_box((1.45, cy + rad + 0.18, 0.0), (2.20, 0.34, 1.05), bevel=0.025, mat="R1_GreyMetal", segs=1)
    for lx in range(5):
        mb.add_bevel_box((0.70 + lx * 0.38, cy + rad + 0.36, 0.0), (0.22, 0.06, 0.88), bevel=0.008, mat="R1_BareSteel", segs=1)
    # Copper coolant pipes into roof ECS
    for pz in (-0.32, 0.32):
        mb.add_cylinder((2.75, cy + rad + 0.12, pz), radius=0.032, height=0.55, segs=8, axis="x", mat="R1_Copper", bevel=0.005)

    # Three heavy cradle footings under Main Module
    for fx in (-0.55, 1.45, 3.55):
        _add_cradle_footing(mb, fx, 0.0, rad, cy, damaged=False)

    # Sealed Airlock Vestibule projecting toward -Z at X = 1.65
    ax = 1.65
    az = -rad - 0.42
    # Vestibule tunnel & collar
    mb.add_bevel_box((ax, cy - 0.08, az + 0.18), (1.56, 2.15, 0.92), bevel=0.035, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((ax, cy - 0.08, az - 0.26), (1.64, 2.22, 0.18), bevel=0.025, mat="R1_GreyMetal", segs=1)
    # Sealed pressure hatch door recessed in frame
    mb.add_bevel_box((ax - 0.08, cy - 0.12, az - 0.32), (1.02, 1.72, 0.10), bevel=0.025, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((ax - 0.08, cy - 0.12, az - 0.35), (0.86, 1.54, 0.06), bevel=0.015, mat="R1_GreyMetal", segs=1)
    # Hatch window & dogging wheel
    mb.add_cylinder((ax - 0.08, cy + 0.28, az - 0.37), radius=0.18, height=0.06, segs=10, axis="z", mat="R1_DarkGlass", bevel=0.008)
    mb.add_ring((ax - 0.08, cy - 0.25, az - 0.39), r_outer=0.16, r_inner=0.04, height=0.05, segs=10, axis="z", mat="R1_BareSteel")
    # Overhead amber airlock beacon
    mb.add_bevel_box((ax - 0.08, cy + 0.92, az - 0.36), (0.28, 0.12, 0.12), bevel=0.015, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((ax - 0.08, cy + 0.92, az - 0.41), (0.22, 0.08, 0.06), bevel=0.01, mat="R1_LampAmber", segs=1)

    # Airlock access platform, steps & safety handrails down to y_u = 0
    mb.add_bevel_box((ax - 0.08, 0.72, az - 0.68), (1.42, 0.10, 0.68), bevel=0.015, mat="R1_GreyMetal", segs=1)
    for step_i in range(3):
        sy = 0.18 + step_i * 0.20
        sz = az - 1.18 + step_i * 0.18
        mb.add_bevel_box((ax - 0.08, sy, sz), (1.12, 0.06, 0.22), bevel=0.008, mat="R1_BareSteel", segs=1)
    for hx_side in (-0.64, 0.48):
        mb.add_cylinder((ax + hx_side, 0.62, az - 0.85), radius=0.024, height=1.24, segs=8, axis="y", mat="R1_OrangeStripe", bevel=0.004)
        mb.add_cylinder((ax + hx_side, 1.18, az - 0.62), radius=0.024, height=0.55, segs=8, axis="z", mat="R1_OrangeStripe", bevel=0.004)

    # Stencil Signage plates on Main Module (facing -Z toward viewer, rot_deg_u = (0, 180, 0))
    # Proud hull mounting boss for main SEC-04 HABITAT & LAB plaque so it never clips into cylinder curvature
    mb.add_bevel_box((2.95, cy + 0.62, -rad + 0.12), (1.24, 0.62, 0.32), bevel=0.018, mat="R1_GreyMetal", segs=1)
    ruins_common.add_signage_plate(
        mb,
        center_u=(2.95, cy + 0.62, -rad - 0.06),
        width=1.18,
        height=0.56,
        cell_name="SEC04_HABITAT",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(ax - 0.08, cy + 1.16, az - 0.22),
        width=0.92,
        height=0.34,
        cell_name="LUMEN",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(ax - 0.68, cy + 0.42, az - 0.35),
        width=0.26,
        height=0.26,
        cell_name="LOGO",
        rot_deg_u=(0.0, 180.0, 0.0),
        rivets=False,
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(ax - 0.68, cy + 0.08, az - 0.35),
        width=0.26,
        height=0.26,
        cell_name="EVAC_1988",
        rot_deg_u=(0.0, 180.0, 0.0),
        rivets=False,
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(ax - 0.68, cy - 0.26, az - 0.35),
        width=0.26,
        height=0.26,
        cell_name="PICT_AIRLOCK",
        rot_deg_u=(0.0, 180.0, 0.0),
        rivets=False,
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(ax - 0.08, 0.72, az - 1.03),
        width=1.32,
        height=0.14,
        cell_name="HAZARD",
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat=None,
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "ModuleA_Main", parent=root)


def _build_airlock_panel(root) -> None:
    """Build the required separate `AirlockPanel` child mesh node using R1_Screen."""
    mb = r1kit.MeshBuilder()
    px, py, pz = 2.22, 1.84, -2.38

    mb.add_bevel_box((px, py, pz), (0.38, 0.52, 0.14), bevel=0.018, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((px, py + 0.07, pz - 0.06), (0.30, 0.26, 0.04), bevel=0.008, mat="R1_Rubber", segs=1)
    mb.add_bevel_box((px, py + 0.07, pz - 0.078), (0.25, 0.21, 0.02), bevel=0.004, mat="R1_Screen", segs=1)
    for kx in (-0.08, 0.0, 0.08):
        mb.add_bevel_box((px + kx, py - 0.12, pz - 0.07), (0.045, 0.045, 0.03), bevel=0.004, mat="R1_CreamEnamel", segs=1)
    mb.add_cylinder((px - 0.09, py - 0.19, pz - 0.07), radius=0.022, height=0.04, segs=8, axis="z", mat="R1_Brass", bevel=0.003)
    mb.add_cylinder((px + 0.08, py - 0.19, pz - 0.07), radius=0.020, height=0.03, segs=8, axis="z", mat="R1_LampAmber", bevel=0.003)

    ruins_common.to_ruin_object(mb, "AirlockPanel", parent=root)


def _build_bellows_joints(root) -> None:
    """Build corrugated expansion bellows joints connecting the 3 modules."""
    mb = r1kit.MeshBuilder()
    cy = 1.92

    for i in range(4):
        bx = -1.22 - i * 0.24
        mb.add_cylinder((bx, cy, 0.0), radius=1.38, height=0.16, segs=10, axis="x", mat="R1_Rubber", bevel=0.025)
        mb.add_ring((bx - 0.10, cy, 0.0), r_outer=1.43, r_inner=1.30, height=0.05, segs=10, axis="x", mat="R1_BareSteel")
    for a_deg in (35.0, 145.0, -35.0, -145.0):
        a = math.radians(a_deg)
        mb.add_cylinder((-1.62, cy + 1.44 * math.sin(a), 1.44 * math.cos(a)), radius=0.028, height=1.18, segs=6, axis="x", mat="R1_BareSteel")

    bx2 = 2.85
    for i in range(4):
        bz = 1.68 + i * 0.24
        mb.add_cylinder((bx2, cy, bz), radius=1.32, height=0.16, segs=10, axis="z", mat="R1_Rubber", bevel=0.025)
        mb.add_ring((bx2, cy, bz + 0.10), r_outer=1.37, r_inner=1.24, height=0.05, segs=10, axis="z", mat="R1_BareSteel")
    for a_deg in (35.0, 145.0, -35.0, -145.0):
        a = math.radians(a_deg)
        mb.add_cylinder((bx2 + 1.38 * math.cos(a), cy + 1.38 * math.sin(a), 2.08), radius=0.028, height=1.12, segs=6, axis="z", mat="R1_BareSteel")

    ruins_common.to_ruin_object(mb, "BellowsJoints", parent=root)


def _build_lab_module(root) -> None:
    """Build ModuleB_Lab (connected behind ModuleA_Main along +Z at x=2.85, z=2.55..6.35)."""
    mb = r1kit.MeshBuilder()
    bx = 2.85
    cy = 1.92
    rad = 1.48
    cz = 4.45
    length = 3.80

    mb.add_cylinder((bx, cy, cz), radius=rad, height=length, segs=12, axis="z", mat="R1_CreamEnamel", bevel=0.04)
    mb.add_ring((bx, cy, cz - 0.65), r_outer=rad + 0.02, r_inner=rad - 0.01, height=0.48, segs=12, axis="z", mat="R1_SagePatch")
    for rz in (2.85, 4.45, 6.05):
        mb.add_ring((bx, cy, rz), r_outer=rad + 0.045, r_inner=rad - 0.02, height=0.16, segs=12, axis="z", mat="R1_GreyMetal")

    mb.add_cylinder((bx, cy, 6.42), radius=rad * 0.88, height=0.24, segs=12, axis="z", mat="R1_GreyMetal", bevel=0.03)
    mb.add_cylinder((bx, cy, 6.56), radius=0.52, height=0.16, segs=10, axis="z", mat="R1_BareSteel", bevel=0.02)
    for px_off in (-0.28, 0.28):
        mb.add_cylinder((bx + px_off, cy - 0.25, 6.62), radius=0.06, height=0.22, segs=8, axis="z", mat="R1_Copper", bevel=0.008)
        mb.add_cylinder((bx + px_off, cy + 0.25, 6.62), radius=0.05, height=0.20, segs=8, axis="z", mat="R1_Brass", bevel=0.006)

    for fz in (3.25, 5.55):
        mb.add_bevel_box((bx, cy - rad * 0.78, fz), (rad * 1.85, 0.34, 0.30), bevel=0.025, mat="R1_GreyMetal", segs=1)
        for sx in (-1.0, 1.0):
            lx = bx + sx * (rad * 0.78)
            leg_h = cy - rad * 0.65
            mb.add_bevel_box((lx, leg_h * 0.5 + 0.08, fz), (0.24, leg_h, 0.24), bevel=0.02, mat="R1_GreyMetal", segs=1)
            mb.add_cylinder((lx, 0.05, fz), radius=0.32, height=0.10, segs=8, axis="y", mat="R1_GreyMetal", bevel=0.015)

    ruins_common.add_signage_plate(
        mb,
        center_u=(bx + rad + 0.02, cy + 0.25, 4.10),
        width=0.72,
        height=0.48,
        cell_name="HABITAT_LAB",
        rot_deg_u=(0.0, 90.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(bx + rad + 0.02, cy + 0.25, 4.82),
        width=0.34,
        height=0.34,
        cell_name="PICT_BOTANY",
        rot_deg_u=(0.0, 90.0, 0.0),
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "ModuleB_Lab", parent=root)


def _build_crushed_module(root) -> None:
    """Build ModuleC_Crushed (crushed/holed module on left X = -2.15 .. -6.15 with hull breach)."""
    mb = r1kit.MeshBuilder()
    cy = 1.82
    rad = 1.54

    mb.add_cylinder((-2.85, cy + 0.06, 0.0), radius=rad, height=1.40, segs=12, axis="x", mat="R1_CreamEnamel", bevel=0.03)
    mb.add_ring((-2.25, cy + 0.06, 0.0), r_outer=rad + 0.04, r_inner=rad - 0.02, height=0.16, segs=12, axis="x", mat="R1_GreyMetal")

    mb.add_cylinder(
        (-5.35, cy - 0.14, -0.06),
        radius=rad * 0.96,
        height=1.45,
        segs=12,
        axis="x",
        mat="R1_CreamEnamel",
        bevel=0.03,
        rot_deg_u=(3.0, 2.0, 5.5),
    )
    mb.add_ring((-5.95, cy - 0.20, -0.08), r_outer=rad * 0.98, r_inner=rad * 0.82, height=0.16, segs=12, axis="x", mat="R1_GreyMetal")
    mb.add_cylinder((-6.05, cy - 0.20, -0.08), radius=rad * 0.88, height=0.18, segs=12, axis="x", mat="R1_GreyMetal", bevel=0.025)

    mb.add_cylinder((-4.10, cy - 0.04, 0.0), radius=rad * 0.82, height=1.50, segs=10, axis="x", mat="R1_Rubber", bevel=0.02)

    for rx, _tilt in ((-3.62, -6.0), (-4.10, 8.0), (-4.58, -5.0)):
        mb.add_ring(
            (rx, cy - 0.02, 0.0),
            r_outer=rad * 0.99,
            r_inner=rad * 0.83,
            height=0.12,
            segs=12,
            axis="x",
            mat="R1_GreyMetal",
        )
    for angle_deg, bend_deg in ((65.0, -14.0), (115.0, 9.0), (-45.0, 12.0), (-110.0, -8.0), (165.0, 6.0)):
        a = math.radians(angle_deg)
        sy = cy + (rad * 0.92) * math.sin(a)
        sz = (rad * 0.92) * math.cos(a)
        mb.add_bevel_box(
            (-4.10, sy, sz),
            (1.48, 0.08, 0.09),
            bevel=0.01,
            mat="R1_BareSteel",
            rot_deg_u=(bend_deg, -bend_deg * 0.6, bend_deg * 0.8),
            segs=1,
        )

    torn_panels = [
        ((-3.82, cy + 1.05, -0.95), (0.68, 0.52, 0.08), (28.0, 14.0, -18.0), "R1_CreamEnamel"),
        ((-4.38, cy + 0.88, -1.12), (0.62, 0.48, 0.08), (-32.0, -18.0, 22.0), "R1_CreamEnamel"),
        ((-4.08, cy - 0.42, -1.36), (0.85, 0.56, 0.08), (16.0, -8.0, 9.0), "R1_OrangeStripe"),
        ((-4.12, cy + 0.58, -1.15), (0.58, 0.44, 0.05), (42.0, -15.0, 18.0), "R1_GoldFoil"),
        ((-3.75, cy + 0.25, -1.24), (0.46, 0.38, 0.05), (-24.0, 20.0, -15.0), "R1_GoldFoil"),
        ((-4.45, cy + 1.18, -0.52), (0.54, 0.42, 0.06), (18.0, 25.0, -28.0), "R1_GoldFoil"),
    ]
    for pos, size, rot, mat in torn_panels:
        mb.add_bevel_box(pos, size, bevel=0.01, mat=mat, rot_deg_u=rot, segs=1)

    mb.add_cylinder((-3.85, cy + 0.42, -0.88), radius=0.038, height=0.72, segs=8, axis="x", mat="R1_Copper", rot_deg_u=(15.0, -22.0, 18.0))
    mb.add_cylinder((-4.32, cy + 0.15, -0.92), radius=0.032, height=0.65, segs=8, axis="x", mat="R1_BareSteel", rot_deg_u=(-18.0, 26.0, -14.0))

    _add_cradle_footing(mb, -2.75, 0.0, rad, cy + 0.06, damaged=False)
    _add_cradle_footing(mb, -5.35, -0.06, rad * 0.95, cy - 0.12, damaged=True)

    mb.add_bevel_box((-5.25, cy + 0.18, -rad + 0.08), (0.68, 0.42, 0.22), bevel=0.015, mat="R1_GreyMetal", segs=1)
    ruins_common.add_signage_plate(
        mb,
        center_u=(-5.25, cy + 0.18, -rad - 0.06),
        width=0.64,
        height=0.38,
        cell_name="SEC_04",
        rot_deg_u=(0.0, 180.0, 0.0),
    )

    ruins_common.to_ruin_object(mb, "ModuleC_Crushed", parent=root)



def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("HabitatModules")

    _build_main_module(root)
    _build_airlock_panel(root)
    _build_bellows_joints(root)
    _build_lab_module(root)
    _build_crushed_module(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (2.22, 1.84, -2.46), parent=root)   # Airlock status panel
    r1kit.add_empty("Scan_2", (-4.10, 2.25, -1.25), parent=root)  # Crushed module hull breach
    r1kit.add_empty("Scan_3", (2.95, 2.48, -1.52), parent=root)   # SEC-04 HABITAT & LAB main signage

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="habitat_modules",
        fbx_name="HabitatModules.fbx",
        footprint_radius=6.2,
        cam_angle_deg=32.0,
        cam_elev_deg=21.0,
        skip_renders=skip_renders,
    )
    print(f"[habitat_modules] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")



if __name__ == "__main__":
    build()
