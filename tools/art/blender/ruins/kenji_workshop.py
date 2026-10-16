"""Build KenjiWorkshop.fbx (R1 Lumen Station ruins — Quonset arched hut with open end).

Includes:
- QuonsetShell: Corrugated/ribbed arched quonset hut with open front portal, foundation deck,
  entry ramp, rear bulkhead with gold MLI insulation & porthole, and WORKSHOP / PICT_ROVER /
  LUMEN / EVAC_1988 / HAZARD stencil signage.
- HoistRail: Overhead I-beam monorail projecting from the open portal with wheeled trolley,
  chain block, and spreader hook.
- HangingLamp: Industrial workshop shade lamp + bench task lamp using R1_LampAmber.
- WorkbenchAndRacks: Kenji's workbench with CRT diagnostic scope (R1_Screen), cassette deck,
  bench vise, wall pegboard tool racks with wrenches/calipers, and rolling drawer cabinet.
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


def _build_quonset_shell(root) -> None:
    mb = r1kit.MeshBuilder()

    # Foundation floor slab & side curb beams (width 5.0m, depth 5.6m, y_u = 0.0 .. 0.18)
    mb.add_bevel_box((0.0, 0.09, 0.0), (5.00, 0.18, 5.60), bevel=0.025, mat="R1_GreyMetal", segs=1)
    # Interior floor grating strips
    for gx in (-1.65, -0.55, 0.55, 1.65):
        mb.add_bevel_box((gx, 0.19, 0.0), (0.76, 0.03, 5.20), bevel=0.006, mat="R1_BareSteel", segs=1)
    # Side foundation curb walls (y = 0.18 .. 0.72)
    for sx in (-1.0, 1.0):
        mb.add_bevel_box((sx * 2.40, 0.45, 0.0), (0.24, 0.56, 5.56), bevel=0.02, mat="R1_GreyMetal", segs=1)
        mb.add_bevel_box((sx * 2.53, 0.52, 0.0), (0.06, 0.24, 5.50), bevel=0.01, mat="R1_OrangeStripe", segs=1)

    # Front entry apron ramp at z = -2.80 .. -3.35
    mb.add_bevel_box((0.0, 0.07, -3.05), (4.20, 0.14, 0.56), bevel=0.02, mat="R1_GreyMetal", rot_deg_u=(8.0, 0.0, 0.0), segs=1)

    # Quonset Arched Roof Shell (12 radial facets from -84 deg to +84 deg around center_y = 0.68, radius = 2.42)
    arch_cy = 0.68
    arch_r = 2.42
    num_arch_segs = 12
    a_start = -84.0
    a_end = 84.0
    da = (a_end - a_start) / num_arch_segs

    for i in range(num_arch_segs):
        a0 = a_start + i * da
        a1 = a0 + da
        amid = 0.5 * (a0 + a1)
        rad_mid = math.radians(amid)
        chord_w = 2.0 * arch_r * math.sin(math.radians(da * 0.5)) + 0.05

        px = arch_r * math.sin(rad_mid)
        py = arch_cy + arch_r * math.cos(rad_mid)

        # Outer shell panel
        panel_mat = "R1_SagePatch" if i in (1, 10) else "R1_CreamEnamel"
        mb.add_bevel_box(
            (px, py, 0.0),
            (chord_w, 0.09, 5.48),
            bevel=0.012,
            mat=panel_mat,
            rot_deg_u=(0.0, 0.0, amid),
            segs=1,
        )
        # Inner gold thermal blanket lining panels on upper ceiling
        if 3 <= i <= 8:
            px_in = (arch_r - 0.07) * math.sin(rad_mid)
            py_in = arch_cy + (arch_r - 0.07) * math.cos(rad_mid)
            mb.add_bevel_box(
                (px_in, py_in, 0.15),
                (chord_w * 0.88, 0.04, 4.80),
                bevel=0.008,
                mat="R1_GoldFoil",
                rot_deg_u=(0.0, 0.0, amid),
                segs=1,
            )

    # 6 Structural Arch Ribs along Z (including front open portal at z = -2.72 and rear at z = +2.72)
    for rz in (-2.72, -1.64, -0.55, 0.55, 1.64, 2.72):
        for i in range(num_arch_segs):
            amid = a_start + (i + 0.5) * da
            rad_mid = math.radians(amid)
            chord_w = 2.0 * (arch_r + 0.03) * math.sin(math.radians(da * 0.5)) + 0.04
            px = (arch_r + 0.02) * math.sin(rad_mid)
            py = arch_cy + (arch_r + 0.02) * math.cos(rad_mid)
            mb.add_bevel_box(
                (px, py, rz),
                (chord_w, 0.18, 0.14),
                bevel=0.015,
                mat="R1_GreyMetal",
                rot_deg_u=(0.0, 0.0, amid),
                segs=1,
            )

    # Closed Rear Bulkhead Wall at z = +2.70
    mb.add_bevel_box((0.0, 1.52, 2.70), (4.48, 2.52, 0.16), bevel=0.02, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((0.0, 2.65, 2.70), (2.85, 0.72, 0.16), bevel=0.02, mat="R1_GreyMetal", segs=1)
    # Rear bulkhead circular window & ECS vent box
    mb.add_cylinder((0.85, 1.85, 2.68), radius=0.38, height=0.22, segs=16, axis="z", mat="R1_GreyMetal", bevel=0.02)
    mb.add_cylinder((0.85, 1.85, 2.66), radius=0.28, height=0.26, segs=16, axis="z", mat="R1_DarkGlass", bevel=0.01)
    mb.add_bevel_box((-0.95, 1.95, 2.84), (0.88, 0.68, 0.28), bevel=0.02, mat="R1_GreyMetal", segs=1)

    # Open Front Portal Columns, Header Beam & Partially Rolled Shutter at z = -2.76
    for sx in (-1.0, 1.0):
        mb.add_bevel_box((sx * 2.18, 1.28, -2.76), (0.34, 2.36, 0.28), bevel=0.02, mat="R1_GreyMetal", segs=1)
        # Protective entry bollard
        mb.add_cylinder((sx * 2.02, 0.42, -3.02), radius=0.09, height=0.76, segs=10, axis="y", mat="R1_OrangeStripe", bevel=0.015)

    # Portal header transom beam & partially rolled shutter slats at y = 2.48 .. 2.78
    mb.add_bevel_box((0.0, 2.66, -2.78), (4.26, 0.34, 0.28), bevel=0.02, mat="R1_GreyMetal", segs=1)
    for slat_i in range(3):
        mb.add_bevel_box((0.0, 2.42 - slat_i * 0.11, -2.74), (3.96, 0.09, 0.06), bevel=0.008, mat="R1_BareSteel", segs=1)

    # Stencil Signage on Open Front Portal (facing -Z toward viewer, rot_deg_u = (0, 180, 0))
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.0, 2.66, -2.93),
        width=1.28,
        height=0.38,
        cell_name="WORKSHOP",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(-2.18, 1.88, -2.91),
        width=0.28,
        height=0.28,
        cell_name="LOGO",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(-2.18, 1.50, -2.91),
        width=0.28,
        height=0.28,
        cell_name="PICT_ROVER",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(2.18, 1.88, -2.91),
        width=0.28,
        height=0.28,
        cell_name="EVAC_1988",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.0, 0.11, -3.32),
        width=1.60,
        height=0.12,
        cell_name="HAZARD",
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat=None,
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "QuonsetShell", parent=root)


def _build_hoist_rail(root) -> None:
    """Build the overhead hoist I-beam monorail, wheeled trolley, chain block, and spreader hook."""
    mb = r1kit.MeshBuilder()
    rx = 0.38
    ry = 2.82

    # Ceiling hanger brackets attached to arch ribs
    for hz in (-2.70, -1.64, -0.55, 0.55, 1.64):
        mb.add_bevel_box((rx, ry + 0.16, hz), (0.22, 0.24, 0.12), bevel=0.012, mat="R1_GreyMetal", segs=1)

    # Overhead I-beam hoist rail extending from z = +2.10 out through the open portal to z = -3.25
    mb.add_bevel_box((rx, ry + 0.06, -0.58), (0.18, 0.04, 5.36), bevel=0.006, mat="R1_OrangeStripe", segs=1)
    mb.add_bevel_box((rx, ry, -0.58), (0.06, 0.12, 5.36), bevel=0.006, mat="R1_OrangeStripe", segs=1)
    mb.add_bevel_box((rx, ry - 0.06, -0.58), (0.18, 0.04, 5.36), bevel=0.006, mat="R1_BareSteel", segs=1)

    # Wheeled gantry trolley & chain hoist block at z = -1.25 (near open portal so clearly visible)
    tz = -1.25
    mb.add_bevel_box((rx, ry - 0.08, tz), (0.32, 0.18, 0.42), bevel=0.015, mat="R1_GreyMetal", segs=1)
    for wz in (-0.14, 0.14):
        for sx in (-0.12, 0.12):
            mb.add_cylinder((rx + sx, ry - 0.03, tz + wz), radius=0.055, height=0.04, segs=10, axis="x", mat="R1_BareSteel", bevel=0.005)
    # Geared chain drum housing
    mb.add_cylinder((rx, ry - 0.26, tz), radius=0.14, height=0.22, segs=12, axis="x", mat="R1_OrangeStripe", bevel=0.015)
    mb.add_cylinder((rx + 0.13, ry - 0.26, tz), radius=0.11, height=0.05, segs=12, axis="x", mat="R1_Brass", bevel=0.008)

    # Dual vertical hoist rods/chains & lifting spreader beam
    for cz in (-0.12, 0.12):
        mb.add_cylinder((rx, ry - 0.68, tz + cz), radius=0.022, height=0.72, segs=8, axis="y", mat="R1_BareSteel")
    mb.add_bevel_box((rx, ry - 1.06, tz), (0.72, 0.10, 0.36), bevel=0.015, mat="R1_OrangeStripe", segs=1)
    mb.add_ring((rx, ry - 1.18, tz), r_outer=0.09, r_inner=0.04, height=0.04, segs=10, axis="z", mat="R1_BareSteel")

    ruins_common.to_ruin_object(mb, "HoistRail", parent=root)


def _build_hanging_lamp(root) -> None:
    """Build the hanging workshop lamp (R1_LampAmber) and workbench gooseneck task lamp."""
    mb = r1kit.MeshBuilder()

    # 1. Main Hanging Industrial Shade Lamp at x = -0.55, y = 2.38, z = -0.45
    lx, ly, lz = -0.55, 2.38, -0.45
    mb.add_cylinder((lx, ly + 0.38, lz), radius=0.022, height=0.64, segs=8, axis="y", mat="R1_BareSteel")
    mb.add_cylinder((lx, ly + 0.08, lz), radius=0.11, height=0.14, segs=12, axis="y", mat="R1_GreyMetal", bevel=0.012)
    # Wide enamel reflector shade
    mb.add_cylinder((lx, ly - 0.02, lz), radius=0.34, height=0.10, segs=16, axis="y", mat="R1_CreamEnamel", bevel=0.02)
    # Bright glowing amber diffuser core (R1_LampAmber)
    mb.add_cylinder((lx, ly - 0.09, lz), radius=0.22, height=0.10, segs=14, axis="y", mat="R1_LampAmber", bevel=0.015)

    # 2. Articulated Bench Task Lamp over Kenji's workbench at x = -1.35, y = 1.48, z = -0.65
    bx, by, bz = -1.35, 1.48, -0.65
    mb.add_cylinder((bx - 0.22, by - 0.24, bz), radius=0.020, height=0.52, segs=8, axis="y", mat="R1_BareSteel", rot_deg_u=(0.0, 0.0, -22.0))
    mb.add_bevel_box((bx, by, bz), (0.22, 0.10, 0.32), bevel=0.015, mat="R1_OrangeStripe", rot_deg_u=(0.0, 0.0, -18.0), segs=1)
    mb.add_bevel_box((bx + 0.02, by - 0.04, bz), (0.18, 0.05, 0.26), bevel=0.008, mat="R1_LampAmber", rot_deg_u=(0.0, 0.0, -18.0), segs=1)

    ruins_common.to_ruin_object(mb, "HangingLamp", parent=root)


def _build_workbench_and_racks(root) -> None:
    """Build Kenji's workbench, CRT diagnostic scope, cassette deck, wall tool racks, and drawer chest."""
    mb = r1kit.MeshBuilder()

    # 1. Kenji's Heavy Workbench along left wall (x = -1.48, z = -1.65 .. +1.85, top at y = 0.92)
    wx = -1.48
    wz = 0.10
    w_len = 3.50
    mb.add_bevel_box((wx, 0.88, wz), (0.92, 0.09, w_len), bevel=0.015, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((wx + 0.44, 0.88, wz), (0.06, 0.10, w_len + 0.02), bevel=0.008, mat="R1_OrangeStripe", segs=1)
    # Lower storage shelf & 6 steel legs
    mb.add_bevel_box((wx, 0.34, wz), (0.82, 0.05, w_len - 0.12), bevel=0.008, mat="R1_GreyMetal", segs=1)
    for lz in (-1.52, 0.10, 1.72):
        for lx_off in (-0.38, 0.38):
            mb.add_bevel_box((wx + lx_off, 0.48, lz), (0.08, 0.78, 0.08), bevel=0.01, mat="R1_GreyMetal", segs=1)

    # Items on Kenji's Workbench:
    # A) Cassette-futurist Oscilloscope & Telemetry Terminal (with R1_Screen) at z = +0.35
    mb.add_bevel_box((wx - 0.05, 1.14, 0.35), (0.48, 0.42, 0.56), bevel=0.02, mat="R1_CreamEnamel", rot_deg_u=(0.0, 18.0, 0.0), segs=2)
    mb.add_bevel_box((wx + 0.16, 1.16, 0.28), (0.08, 0.28, 0.34), bevel=0.008, mat="R1_Screen", rot_deg_u=(0.0, 18.0, 0.0), segs=1)
    # B) Kenji's 1980s Cassette Tape Deck & "FOR 07 — KENJI" Cassette at z = -0.35
    mb.add_bevel_box((wx + 0.02, 1.00, -0.35), (0.36, 0.15, 0.42), bevel=0.015, mat="R1_GreyMetal", rot_deg_u=(0.0, 12.0, 0.0), segs=1)
    mb.add_bevel_box((wx + 0.18, 0.95, -0.72), (0.16, 0.04, 0.22), bevel=0.005, mat="R1_OrangeStripe", rot_deg_u=(0.0, -24.0, 0.0), segs=1)
    # C) Heavy Machinist Bench Vise at front-left corner of bench (z = -1.28)
    mb.add_bevel_box((wx + 0.24, 1.02, -1.28), (0.28, 0.20, 0.32), bevel=0.015, mat="R1_GreyMetal", segs=1)
    mb.add_cylinder((wx + 0.42, 1.02, -1.28), radius=0.028, height=0.26, segs=8, axis="x", mat="R1_BareSteel")
    # D) Disassembled Rover Servo Actuator (Brass + Copper coils) at z = +1.15
    mb.add_cylinder((wx + 0.05, 1.04, 1.15), radius=0.14, height=0.38, segs=12, axis="z", mat="R1_Brass", bevel=0.015)
    mb.add_cylinder((wx + 0.05, 1.04, 1.38), radius=0.10, height=0.18, segs=12, axis="z", mat="R1_Copper", bevel=0.01)

    # 2. Wall-mounted Tool Racks above Workbench (x = -1.98, y = 1.62, z = -1.35 .. +1.45)
    for rz in (-0.75, 0.65):
        mb.add_bevel_box((-1.98, 1.62, rz), (0.06, 0.86, 1.24), bevel=0.01, mat="R1_GreyMetal", rot_deg_u=(0.0, 0.0, -14.0), segs=1)
        # Mounted tools (wrenches, torque bars, calipers, T-handles) on the rack
        for ti in range(5):
            tz = rz - 0.44 + ti * 0.22
            t_len = 0.32 + (ti % 3) * 0.10
            t_mat = "R1_OrangeStripe" if ti == 2 else "R1_BareSteel"
            mb.add_bevel_box((-1.91, 1.64, tz), (0.04, t_len, 0.05), bevel=0.005, mat=t_mat, rot_deg_u=(0.0, 0.0, -14.0), segs=1)

    # 3. Right-wall Rolling Tool Drawer Chest & Spare Parts Rack (x = +1.62)
    mb.add_bevel_box((1.65, 0.68, -0.85), (0.68, 1.02, 1.12), bevel=0.02, mat="R1_OrangeStripe", segs=2)
    for dy in (0.34, 0.56, 0.78, 1.00):
        mb.add_bevel_box((1.30, dy, -0.85), (0.04, 0.16, 0.96), bevel=0.005, mat="R1_BareSteel", segs=1)
    # Spare rover wheel & copper cable spool rack on right-rear (x = +1.58, z = +0.85)
    mb.add_bevel_box((1.58, 0.78, 0.85), (0.72, 1.24, 1.36), bevel=0.02, mat="R1_GreyMetal", segs=1)
    for wz in (0.52, 1.18):
        mb.add_cylinder((1.52, 0.58, wz), radius=0.34, height=0.24, segs=14, axis="x", mat="R1_Rubber", bevel=0.02)
        mb.add_cylinder((1.52, 0.58, wz), radius=0.18, height=0.26, segs=12, axis="x", mat="R1_CreamEnamel", bevel=0.01)

    ruins_common.to_ruin_object(mb, "WorkbenchAndRacks", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("KenjiWorkshop")

    _build_quonset_shell(root)
    _build_hoist_rail(root)
    _build_hanging_lamp(root)
    _build_workbench_and_racks(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (-1.38, 1.12, -0.15), parent=root)  # Kenji's workbench cassette & scope
    r1kit.add_empty("Scan_2", (0.38, 1.85, -1.25), parent=root)   # Overhead hoist trolley & hook
    r1kit.add_empty("Scan_3", (0.0, 2.66, -2.96), parent=root)    # WORKSHOP portal header sign

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="kenji_workshop",
        fbx_name="KenjiWorkshop.fbx",
        footprint_radius=4.2,
        cam_angle_deg=24.0,
        cam_elev_deg=19.0,
        skip_renders=skip_renders,
    )
    print(f"[kenji_workshop] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")



if __name__ == "__main__":
    build()
