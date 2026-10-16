"""Build FallenSolarArray.fbx (R1 Lumen Station ruins — two clean tilted blue PV wings + toppled gearbox).

Includes:
- ToppledGearbox: Rounded cream-and-orange heliostat gearbox (`R1_CreamEnamel`, `R1_OrangeStripe`)
  with a smooth 32-segment concentric cylindrical rotary drive hub, steel drive axles, and gold
  foil rear thermal blanket.
- SolarWingLeft: Clean rectangular tilted blue PV wing (`R1_SolarCell`) in a brushed steel/grey
  frame with crisp silver cell-grid bars on top and crumpled gold Kapton foil (`R1_GoldFoil`)
  backing draped beneath its raised rear edge.
- SolarWingRight: Clean rectangular tilted blue PV wing (`R1_SolarCell`) on the right with crisp
  silver cell-grid bars on top and crumpled gold Kapton foil (`R1_GoldFoil`) backing underneath.
- CableStub: Severed dark-rubber power umbilical cable (`R1_Rubber`) with brass collar (`R1_Brass`)
  and copper conductor core (`R1_Copper`) behind the gearbox.
- Scan_1, Scan_2, Scan_3: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bmesh
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _add_crumpled_gold_foil_skirt(
    mb: r1kit.MeshBuilder,
    wing_cx: float,
    wing_cz: float,
    wing_w: float,
    wing_d: float,
    tilt_deg: float,
    yaw_deg: float,
    roll_deg: float,
) -> None:
    """Add a closed, manifold crumpled gold-foil backing blanket (`R1_GoldFoil`) under a tilted solar wing."""
    rot_b = Euler(
        (
            math.radians(tilt_deg),
            math.radians(roll_deg),
            math.radians(-yaw_deg),
        ),
        "XYZ",
    ).to_matrix()

    cy = 0.06 + (wing_d * 0.5) * math.sin(math.radians(abs(tilt_deg)))

    def panel_world(lx: float, ly: float, lz: float) -> Tuple[float, float, float]:
        rb = rot_b @ Vector((lx, lz, ly))
        return (wing_cx + rb.x, max(0.02, cy + rb.z), wing_cz + rb.y)

    # Build a closed crinkled prism under the raised rear (+Z) and outer (+X) portion of the tilted wing
    nx = 6
    nz = 4
    xs = [-wing_w * 0.44 + wing_w * 0.94 * (i / nx) for i in range(nx + 1)]
    zs = [-wing_d * 0.10 + wing_d * 0.58 * (j / nz) for j in range(nz + 1)]

    top_grid: List[List[bmesh.types.BMVert]] = []
    bot_grid: List[List[bmesh.types.BMVert]] = []

    for j, lz in enumerate(zs):
        t_row: List[bmesh.types.BMVert] = []
        b_row: List[bmesh.types.BMVert] = []
        z_frac = j / nz
        for i, lx in enumerate(xs):
            i_frac = i / nx
            # Deterministic crinkle facets on the lower draped gold foil
            crinkle_x = 0.055 * math.sin(i * 2.3 + j * 1.9 + wing_cx)
            crinkle_z = 0.060 * math.cos(i * 1.7 - j * 2.5 + wing_cx)
            crinkle_y = 0.045 * math.sin(i * 2.9 + j * 2.1)

            tx, ty, tz = panel_world(lx, -0.028, lz)
            # Billow slightly outward on the right (+X) and rear (+Z) edges so the crinkled gold foil is visible beside/under the wing
            out_x = (0.18 * (1.0 - z_frac * 0.3)) if i == nx else ((-0.05) if i == 0 else 0.0)
            out_z = 0.12 if j == nz else (-0.06 if j == 0 else 0.0)
            bx = tx + out_x + crinkle_x
            bz = tz + out_z + crinkle_z
            by = max(0.02, 0.025 + 0.04 * max(0.0, crinkle_y) + 0.03 * i_frac)
            if ty <= by + 0.035:
                ty = by + 0.040

            t_row.append(mb.bm.verts.new(mb._to_local_b((tx, ty, tz))))
            b_row.append(mb.bm.verts.new(mb._to_local_b((bx, by, bz))))
        top_grid.append(t_row)
        bot_grid.append(b_row)

    faces: List[bmesh.types.BMFace] = []
    for j in range(nz):
        for i in range(nx):
            faces.append(mb.bm.faces.new([top_grid[j][i], top_grid[j][i + 1], top_grid[j + 1][i + 1], top_grid[j + 1][i]]))
            faces.append(mb.bm.faces.new([bot_grid[j][i + 1], bot_grid[j][i], bot_grid[j + 1][i], bot_grid[j + 1][i + 1]]))

    for i in range(nx):
        faces.append(mb.bm.faces.new([top_grid[0][i + 1], top_grid[0][i], bot_grid[0][i], bot_grid[0][i + 1]]))
        faces.append(mb.bm.faces.new([top_grid[-1][i], top_grid[-1][i + 1], bot_grid[-1][i + 1], bot_grid[-1][i]]))

    for j in range(nz):
        faces.append(mb.bm.faces.new([top_grid[j][0], top_grid[j + 1][0], bot_grid[j + 1][0], bot_grid[j][0]]))
        faces.append(mb.bm.faces.new([top_grid[j + 1][-1], top_grid[j][-1], bot_grid[j][-1], bot_grid[j + 1][-1]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, "R1_GoldFoil")


def _build_toppled_gearbox(root) -> None:
    """Build the rounded cream-and-orange gearbox with concentric cylindrical drive hub between the wings."""
    mb = r1kit.MeshBuilder()
    gx, gy, gz = 0.0, 0.44, -0.05
    g_rot = (8.0, -14.0, 0.0)

    # 1. Main rounded cream-enamel gearbox body (`R1_CreamEnamel`)
    mb.add_bevel_box(
        (gx - 0.14, gy, gz),
        (0.76, 0.80, 0.84),
        bevel=0.08,
        mat="R1_CreamEnamel",
        rot_deg_u=g_rot,
        segs=3,
    )

    # 2. Wide warm-orange wrap band (`R1_OrangeStripe`) over the top and front/back faces of the gearbox
    mb.add_bevel_box(
        (gx - 0.12, gy + 0.01, gz),
        (0.34, 0.82, 0.86),
        bevel=0.07,
        mat="R1_OrangeStripe",
        rot_deg_u=g_rot,
        segs=3,
    )

    # 3. Recessed lower dark-grey inspection plate on the front face (`R1_GreyMetal`)
    mb.add_bevel_box(
        (gx - 0.16, gy - 0.18, gz - 0.38),
        (0.46, 0.28, 0.12),
        bevel=0.015,
        mat="R1_GreyMetal",
        rot_deg_u=g_rot,
        segs=1,
    )

    # 4. Smooth 32-segment Concentric Cylindrical Rotary Drive Hub on the right (+X) side of the gearbox
    # Outer cream cylindrical collar
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(gx + 0.30, gy, gz + 0.02),
        radius=0.39,
        total_length=0.28,
        dome_depth=0.03,
        mat="R1_CreamEnamel",
        axis="x",
        segs=32,
        barrel_rings=1,
        cap_rings=2,
        rot_deg_u=g_rot,
    )
    # Warm-orange cylindrical face disk (`R1_OrangeStripe`)
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(gx + 0.40, gy, gz + 0.03),
        radius=0.33,
        total_length=0.14,
        dome_depth=0.02,
        mat="R1_OrangeStripe",
        axis="x",
        segs=32,
        barrel_rings=1,
        cap_rings=2,
        rot_deg_u=g_rot,
    )
    # Inner dark-grey & bare-steel concentric drive bearing housing (`R1_GreyMetal` / `R1_BareSteel`)
    mb.add_cylinder(
        (gx + 0.50, gy, gz + 0.04),
        radius=0.19,
        height=0.16,
        segs=24,
        axis="x",
        mat="R1_GreyMetal",
        bevel=0.015,
        rot_deg_u=g_rot,
    )
    mb.add_cylinder(
        (gx + 0.62, gy - 0.02, gz + 0.04),
        radius=0.085,
        height=0.36,
        segs=20,
        axis="x",
        mat="R1_BareSteel",
        bevel=0.008,
        rot_deg_u=g_rot,
    )
    # Left (-X) drive axle connecting into SolarWingLeft
    mb.add_cylinder(
        (gx - 0.62, gy + 0.02, gz - 0.04),
        radius=0.085,
        height=0.42,
        segs=20,
        axis="x",
        mat="R1_BareSteel",
        bevel=0.008,
        rot_deg_u=g_rot,
    )

    # 5. Crumpled Gold Kapton thermal blanket pad (`R1_GoldFoil`) tucked behind the gearbox (`+Z`)
    mb.add_bevel_box(
        (gx + 0.04, 0.34, gz + 0.48),
        (0.66, 0.52, 0.28),
        bevel=0.03,
        mat="R1_GoldFoil",
        rot_deg_u=(-16.0, 12.0, -8.0),
        segs=2,
    )

    ruins_common.to_ruin_object(mb, "ToppledGearbox", parent=root)


def _build_clean_solar_wing(
    root,
    name: str,
    wing_cx: float,
    wing_cz: float,
    wing_w: float = 2.48,
    wing_d: float = 1.68,
    tilt_deg: float = -23.0,
    yaw_deg: float = 0.0,
    roll_deg: float = 0.0,
) -> None:
    """Build a clean rectangular blue PV wing with silver cell grid and crumpled gold-foil backing."""
    mb = r1kit.MeshBuilder()

    rot = (tilt_deg, yaw_deg, roll_deg)
    rot_b = Euler(
        (
            math.radians(tilt_deg),
            math.radians(roll_deg),
            math.radians(-yaw_deg),
        ),
        "XYZ",
    ).to_matrix()

    cy = 0.06 + (wing_d * 0.5) * math.sin(math.radians(abs(tilt_deg)))

    def wing_pt(lx: float, ly: float, lz: float) -> Tuple[float, float, float]:
        rb = rot_b @ Vector((lx, lz, ly))
        return (wing_cx + rb.x, max(0.02, cy + rb.z), wing_cz + rb.y)

    # 1. Crumpled Gold-Foil backing (`R1_GoldFoil`) underneath the raised rear/outer edges of the wing
    _add_crumpled_gold_foil_skirt(
        mb,
        wing_cx=wing_cx,
        wing_cz=wing_cz,
        wing_w=wing_w,
        wing_d=wing_d,
        tilt_deg=tilt_deg,
        yaw_deg=yaw_deg,
        roll_deg=roll_deg,
    )

    # 2. Clean rectangular metallic perimeter chassis & central divider bar (`R1_GreyMetal` + `R1_BareSteel` trim)
    mb.add_bevel_box(
        wing_pt(0.0, 0.0, 0.0),
        (wing_w, 0.068, wing_d),
        bevel=0.012,
        mat="R1_GreyMetal",
        rot_deg_u=rot,
        segs=1,
    )
    # Brushed steel upper bezel frame rails (left, center divider, right, front, back)
    frame_y = 0.032
    for fx in (-wing_w * 0.485, 0.0, wing_w * 0.485):
        bar_w = 0.065 if fx == 0.0 else 0.075
        mb.add_bevel_box(
            wing_pt(fx, frame_y, 0.0),
            (bar_w, 0.024, wing_d * 0.99),
            bevel=0.004,
            mat="R1_BareSteel",
            rot_deg_u=rot,
            segs=1,
        )
    for fz in (-wing_d * 0.48, wing_d * 0.48):
        mb.add_bevel_box(
            wing_pt(0.0, frame_y, fz),
            (wing_w * 0.99, 0.024, 0.075),
            bevel=0.004,
            mat="R1_BareSteel",
            rot_deg_u=rot,
            segs=1,
        )

    # 3. Two large clean deep-blue PV sub-panels (`R1_SolarCell`) with crisp silver cell-grid bars
    sub_w = (wing_w - 0.22) * 0.5
    sub_d = wing_d - 0.16
    for bay_sign in (-1.0, 1.0):
        bay_lx = bay_sign * (sub_w * 0.5 + 0.032)
        mb.add_bevel_box(
            wing_pt(bay_lx, 0.028, 0.0),
            (sub_w, 0.020, sub_d),
            bevel=0.003,
            mat="R1_SolarCell",
            rot_deg_u=rot,
            segs=1,
        )
        # 3 vertical + 3 horizontal crisp silver grid lines per PV sub-panel (forming a 4x4 cell grid per bay)
        for col_i in (-1, 0, 1):
            gx = bay_lx + col_i * (sub_w * 0.25)
            mb.add_bevel_box(
                wing_pt(gx, 0.036, 0.0),
                (0.014, 0.010, sub_d * 0.98),
                bevel=0.002,
                mat="R1_BareSteel",
                rot_deg_u=rot,
                segs=1,
            )
        for row_i in (-1, 0, 1):
            gz = row_i * (sub_d * 0.25)
            mb.add_bevel_box(
                wing_pt(bay_lx, 0.036, gz),
                (sub_w * 0.98, 0.010, 0.014),
                bevel=0.002,
                mat="R1_BareSteel",
                rot_deg_u=rot,
                segs=1,
            )

    ruins_common.to_ruin_object(mb, name, parent=root)


def _build_cable_stub(root) -> None:
    """Build the severed power umbilical cable stub (`R1_Rubber`, `R1_Brass`, `R1_Copper`) behind the gearbox."""
    mb = r1kit.MeshBuilder()

    # Brass gland collar on the rear-lower side of the gearbox
    mb.add_cylinder(
        (-0.24, 0.22, 0.42),
        radius=0.068,
        height=0.14,
        segs=16,
        axis="z",
        mat="R1_Brass",
        bevel=0.008,
    )

    # Smooth 14-segment dark-rubber umbilical cable curving along the ground
    pts = [
        (-0.24, 0.22, 0.46),
        (-0.32, 0.14, 0.72),
        (-0.46, 0.09, 0.98),
        (-0.64, 0.08, 1.18),
    ]
    mb.add_tube_path(pts, radius=0.048, mat="R1_Rubber", segments=14, cap_ends=True)

    # Severed brass end ring & copper core stub
    mb.add_cylinder(
        (-0.66, 0.08, 1.21),
        radius=0.056,
        height=0.08,
        segs=16,
        axis="z",
        mat="R1_Brass",
        bevel=0.006,
        rot_deg_u=(0.0, -35.0, 0.0),
    )
    mb.add_cylinder(
        (-0.72, 0.08, 1.27),
        radius=0.028,
        height=0.12,
        segs=12,
        axis="z",
        mat="R1_Copper",
        bevel=0.004,
        rot_deg_u=(0.0, -35.0, 0.0),
    )

    ruins_common.to_ruin_object(mb, "CableStub", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("FallenSolarArray")

    _build_toppled_gearbox(root)
    _build_clean_solar_wing(
        root,
        "SolarWingLeft",
        wing_cx=-2.02,
        wing_cz=0.12,
        wing_w=2.44,
        wing_d=1.68,
        tilt_deg=26.0,
        yaw_deg=8.0,
        roll_deg=-4.0,
    )
    _build_clean_solar_wing(
        root,
        "SolarWingRight",
        wing_cx=2.04,
        wing_cz=-0.16,
        wing_w=2.44,
        wing_d=1.68,
        tilt_deg=28.0,
        yaw_deg=-10.0,
        roll_deg=-5.0,
    )
    _build_cable_stub(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (0.12, 0.82, -0.05), parent=root)    # Toppled cream-and-orange gearbox
    r1kit.add_empty("Scan_2", (-0.64, 0.14, 1.18), parent=root)    # Severed power umbilical cable stub
    r1kit.add_empty("Scan_3", (-2.02, 0.42, 0.12), parent=root)    # Left photovoltaic solar wing

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="fallen_solar_array",
        fbx_name="FallenSolarArray.fbx",
        footprint_radius=4.2,
        cam_angle_deg=18.0,
        cam_elev_deg=22.0,
        skip_renders=skip_renders,
    )
    print(f"[fallen_solar_array] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")


if __name__ == "__main__":
    build()
