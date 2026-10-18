"""Build KenjiWorkshop.fbx (R1 Lumen Station ruins — corrugated quonset arch open at the front).

Includes:
- QuonsetShell: Smooth 36-segment corrugated cream quonset arch (`R1_CreamEnamel`), dark-grey
  front arch trim (`R1_GreyMetal`), recessed cream front gable wall with ONE horizontal warm-orange
  band (`R1_OrangeStripe`) and a rectangular louvered vent at the top, open front rectangular portal,
  and warm-lit interior floor and rear bulkhead.
- HoistRail: Horizontal overhead I-beam hoist rail across the interior with warm-orange trolley
  block and hanging hook visible through the open front.
- HangingLamp: Warm glowing cage lantern (`R1_LampAmber`) hanging on the right inside the open portal.
- WorkbenchAndRacks: Kenji's workbench visible on the left inside the workshop, with bench vice,
  tool box, wall tool rail, riveted sage tool board, rear cabinet, and one `WORKSHOP` stencil plate.
- Scan_1, Scan_2, Scan_3: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bmesh

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _add_corrugated_quonset_roof(
    mb: r1kit.MeshBuilder,
    arch_cy: float = 0.45,
    r_in: float = 2.46,
    r_valley: float = 2.55,
    r_peak: float = 2.60,
    z_start: float = -2.58,
    z_end: float = 2.72,
    num_corrugations: int = 16,
    arch_segs: int = 36,
    mat: str = "R1_CreamEnamel",
) -> None:
    """Build a closed, manifold 36-segment corrugated cream quonset arch shell."""
    # Arch spans from right ground (y=0.02) over top (y=arch_cy+r) to left ground (y=0.02)
    ang_min = -math.asin(min(0.95, (arch_cy - 0.02) / r_in))
    ang_max = math.pi - ang_min
    angles = [ang_min + (ang_max - ang_min) * (i / arch_segs) for i in range(arch_segs + 1)]

    axial_steps = num_corrugations * 2
    z_vals = [z_start + (z_end - z_start) * (k / axial_steps) for k in range(axial_steps + 1)]

    in_grid: List[List[bmesh.types.BMVert]] = []
    out_grid: List[List[bmesh.types.BMVert]] = []

    for k, zv in enumerate(z_vals):
        r_out = r_peak if (k % 2 == 1) else r_valley
        ir: List[bmesh.types.BMVert] = []
        orr: List[bmesh.types.BMVert] = []
        for ang in angles:
            ca = math.cos(ang)
            sa = math.sin(ang)
            iy = max(0.02, arch_cy + r_in * sa)
            oy = max(0.02, arch_cy + r_out * sa)
            ir.append(mb.bm.verts.new(mb._to_local_b((r_in * ca, iy, zv))))
            orr.append(mb.bm.verts.new(mb._to_local_b((r_out * ca, oy, zv))))
        in_grid.append(ir)
        out_grid.append(orr)

    faces: List[bmesh.types.BMFace] = []
    for k in range(axial_steps):
        for i in range(arch_segs):
            j = i + 1
            faces.append(mb.bm.faces.new([out_grid[k][i], out_grid[k][j], out_grid[k + 1][j], out_grid[k + 1][i]]))
            faces.append(mb.bm.faces.new([in_grid[k][j], in_grid[k][i], in_grid[k + 1][i], in_grid[k + 1][j]]))
        faces.append(mb.bm.faces.new([in_grid[k][0], out_grid[k][0], out_grid[k + 1][0], in_grid[k + 1][0]]))
        faces.append(mb.bm.faces.new([out_grid[k][-1], in_grid[k][-1], in_grid[k + 1][-1], out_grid[k + 1][-1]]))

    for i in range(arch_segs):
        j = i + 1
        faces.append(mb.bm.faces.new([out_grid[0][i], in_grid[0][i], in_grid[0][j], out_grid[0][j]]))
        faces.append(mb.bm.faces.new([in_grid[-1][i], out_grid[-1][i], out_grid[-1][j], in_grid[-1][j]]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)


def _add_arch_rim_band(
    mb: r1kit.MeshBuilder,
    arch_cy: float,
    r_in: float,
    r_out: float,
    z_center: float,
    depth_z: float,
    arch_segs: int = 36,
    mat: str = "R1_GreyMetal",
) -> None:
    """Add a smooth 36-segment semicircular arch trim frame at `z_center`."""
    ang_min = -math.asin(min(0.95, (arch_cy - 0.02) / r_in))
    ang_max = math.pi - ang_min
    angles = [ang_min + (ang_max - ang_min) * (i / arch_segs) for i in range(arch_segs + 1)]

    pts_xy: List[Tuple[float, float]] = []
    for ang in angles:
        pts_xy.append((r_out * math.cos(ang), max(0.02, arch_cy + r_out * math.sin(ang))))
    for ang in reversed(angles):
        pts_xy.append((r_in * math.cos(ang), max(0.02, arch_cy + r_in * math.sin(ang))))

    ruins_common.add_extruded_profile_z(
        mb,
        center_u=(0.0, 0.0, z_center),
        pts_xy=pts_xy,
        depth_z=depth_z,
        mat=mat,
        bevel=0.012,
    )


def _build_quonset_shell(root) -> None:
    mb = r1kit.MeshBuilder()
    arch_cy = 0.45
    r_gable = 2.43
    front_z = -2.62

    # 1. Warm interior floor slab (`y = 0.0 .. 0.08`) + outer dark-grey foundation curb
    mb.add_bevel_box((0.0, 0.03, 0.06), (5.16, 0.06, 5.44), bevel=0.012, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((0.0, 0.065, 0.08), (4.88, 0.04, 5.26), bevel=0.008, mat="R1_CreamEnamel", segs=1)

    # 2. Smooth 36-segment Corrugated Cream Quonset Arch Roof (`R1_CreamEnamel`)
    _add_corrugated_quonset_roof(
        mb,
        arch_cy=arch_cy,
        r_in=2.45,
        r_valley=2.55,
        r_peak=2.60,
        z_start=-2.58,
        z_end=2.72,
        num_corrugations=16,
        arch_segs=36,
        mat="R1_CreamEnamel",
    )

    # 3. Smooth 36-segment Front & Rear Dark-Grey Semicircular Arch Frames (`R1_GreyMetal`)
    _add_arch_rim_band(mb, arch_cy=arch_cy, r_in=2.38, r_out=2.63, z_center=-2.66, depth_z=0.22, arch_segs=36, mat="R1_GreyMetal")
    _add_arch_rim_band(mb, arch_cy=arch_cy, r_in=2.40, r_out=2.62, z_center=2.72, depth_z=0.18, arch_segs=36, mat="R1_GreyMetal")

    # 4. Closed Rear Arched Bulkhead Wall (`z = +2.66`, warm cream interior face)
    rear_pts: List[Tuple[float, float]] = [(-2.42, 0.04), (2.42, 0.04)]
    for i in range(25):
        ang = math.pi * (i / 24)
        rear_pts.append((2.44 * math.cos(ang), max(0.04, arch_cy + 2.44 * math.sin(ang))))
    ruins_common.add_extruded_profile_z(
        mb,
        center_u=(0.0, 0.0, 2.66),
        pts_xy=rear_pts,
        depth_z=0.12,
        mat="R1_CreamEnamel",
        bevel=0.008,
    )

    # 5. Front Recessed Gable Wall around the Open Rectangular Portal (`x = -1.68 .. +1.68, y = 0.04 .. 2.02`)
    door_w_half = 1.68
    door_h = 2.02

    def x_on_arch(y_val: float) -> float:
        dy = min(r_gable - 0.02, max(-r_gable + 0.02, y_val - arch_cy))
        return math.sqrt(max(0.05, r_gable * r_gable - dy * dy))

    # Left & Right Cream Jamb Walls (`y = 0.04 .. 2.04`) profiled to the circular arch
    for sx in (-1.0, 1.0):
        jamb_pts: List[Tuple[float, float]] = [
            (sx * door_w_half, 0.04),
            (sx * door_w_half, door_h + 0.02),
        ]
        for k in range(8, -1, -1):
            yv = 0.04 + (door_h - 0.02) * (k / 8.0)
            jamb_pts.append((sx * x_on_arch(yv), yv))
        if sx < 0.0:
            jamb_pts.reverse()
        ruins_common.add_extruded_profile_z(
            mb,
            center_u=(0.0, 0.0, front_z),
            pts_xy=jamb_pts,
            depth_z=0.12,
            mat="R1_CreamEnamel",
            bevel=0.006,
        )
        # Vertical panel seam groove on front cream jamb wall (matching ruins_v1_dawn.png)
        mb.add_bevel_box((sx * 2.04, 0.96, front_z - 0.055), (0.024, 1.72, 0.03), bevel=0.003, mat="R1_GreyMetal", segs=1)

    # ONE Horizontal Warm-Orange Band (`R1_OrangeStripe`) across the front arch above the portal (`y = 2.00 .. 2.34`)
    orange_y0, orange_y1 = 2.00, 2.34
    orange_pts: List[Tuple[float, float]] = [
        (-x_on_arch(orange_y0), orange_y0),
        (x_on_arch(orange_y0), orange_y0),
        (x_on_arch(orange_y1 * 0.5 + orange_y0 * 0.5), 0.5 * (orange_y0 + orange_y1)),
        (x_on_arch(orange_y1), orange_y1),
        (-x_on_arch(orange_y1), orange_y1),
        (-x_on_arch(orange_y1 * 0.5 + orange_y0 * 0.5), 0.5 * (orange_y0 + orange_y1)),
    ]
    ruins_common.add_extruded_profile_z(
        mb,
        center_u=(0.0, 0.0, front_z - 0.01),
        pts_xy=orange_pts,
        depth_z=0.13,
        mat="R1_OrangeStripe",
        bevel=0.008,
    )

    # Upper Cream Arched Gable Wall above the orange band (`y = 2.32 .. top of arch`)
    top_pts: List[Tuple[float, float]] = []
    ang_top0 = math.asin(min(0.98, (2.32 - arch_cy) / r_gable))
    ang_top1 = math.pi - ang_top0
    for i in range(19):
        ang = ang_top0 + (ang_top1 - ang_top0) * (i / 18.0)
        top_pts.append((r_gable * math.cos(ang), arch_cy + r_gable * math.sin(ang)))
    ruins_common.add_extruded_profile_z(
        mb,
        center_u=(0.0, 0.0, front_z),
        pts_xy=top_pts,
        depth_z=0.12,
        mat="R1_CreamEnamel",
        bevel=0.006,
    )
    # Subtle vertical panel seams on upper cream gable wall
    for sx in (-1.10, -0.55, 0.55, 1.10):
        seam_h = max(0.18, (arch_cy + math.sqrt(max(0.1, r_gable * r_gable - sx * sx))) - 2.35)
        mb.add_bevel_box((sx, 2.34 + seam_h * 0.5, front_z - 0.055), (0.020, seam_h, 0.03), bevel=0.003, mat="R1_GreyMetal", segs=1)

    # Top Rectangular Louvered Vent centered above the warm-orange band (`x = 0.0, y = 2.60, z = front_z - 0.06`)
    vx, vy, vz = 0.0, 2.60, front_z - 0.06
    mb.add_bevel_box((vx, vy, vz), (0.86, 0.28, 0.08), bevel=0.012, mat="R1_GreyMetal", segs=1)
    for li in range(4):
        ly = vy - 0.08 + li * 0.054
        mb.add_bevel_box(
            (vx, ly, vz - 0.035),
            (0.76, 0.026, 0.04),
            bevel=0.004,
            mat="R1_BareSteel",
            rot_deg_u=(18.0, 0.0, 0.0),
            segs=1,
        )

    # Dark-Grey Portal Jamb & Lintel Frame around the open rectangular doorway
    for sx in (-1.0, 1.0):
        mb.add_bevel_box((sx * 1.68, 1.02, front_z - 0.02), (0.12, 2.00, 0.18), bevel=0.012, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((0.0, 1.99, front_z - 0.02), (3.48, 0.11, 0.18), bevel=0.012, mat="R1_GreyMetal", segs=1)

    # Interior horizontal dark-grey dado rail along the rear bulkhead & side walls (matching ruins_v1_dawn.png)
    mb.add_bevel_box((0.0, 1.32, 2.58), (4.36, 0.10, 0.06), bevel=0.008, mat="R1_GreyMetal", segs=1)

    ruins_common.to_ruin_object(mb, "QuonsetShell", parent=root)


def _build_hoist_rail(root) -> None:
    """Build the overhead I-beam hoist rail across the interior with warm-orange trolley and hook (min Z >= 1.75 m)."""
    mb = r1kit.MeshBuilder()
    ry = 2.12
    rz = -1.62

    # Ceiling hanger brackets supporting the transverse hoist beam
    for hx in (-1.35, 1.35):
        mb.add_bevel_box((hx, ry + 0.18, rz), (0.12, 0.36, 0.12), bevel=0.010, mat="R1_GreyMetal", segs=1)

    # Transverse overhead I-beam hoist rail visible right inside the open doorway
    mb.add_bevel_box((0.0, ry + 0.05, rz), (3.12, 0.035, 0.16), bevel=0.005, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((0.0, ry, rz), (3.12, 0.10, 0.05), bevel=0.005, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((0.0, ry - 0.05, rz), (3.12, 0.035, 0.16), bevel=0.005, mat="R1_BareSteel", segs=1)

    # Warm-orange trolley block & compact hanging hoist hook near the center (`x = -0.10`, hook bottom at 1.750 m)
    tx = -0.10
    mb.add_bevel_box((tx, ry - 0.12, rz), (0.38, 0.15, 0.24), bevel=0.014, mat="R1_OrangeStripe", segs=1)
    mb.add_cylinder((tx, ry - 0.22, rz), radius=0.024, height=0.12, segs=10, axis="y", mat="R1_GreyMetal")
    mb.add_bevel_box((tx, ry - 0.27, rz), (0.12, 0.09, 0.10), bevel=0.012, mat="R1_OrangeStripe", segs=1)
    mb.add_ring((tx, 1.812, rz), r_outer=0.062, r_inner=0.030, height=0.036, segs=16, axis="z", mat="R1_GreyMetal")

    ruins_common.to_ruin_object(mb, "HoistRail", parent=root)


def _build_hanging_lamp(root) -> None:
    """Build the warm glowing cage lantern (`R1_LampAmber`) hanging on the right inside the open doorway (min Z = 1.75 m)."""
    mb = r1kit.MeshBuilder()
    lx, ly, lz = 0.88, 1.94, -1.56

    # Suspension conduit rod & top dome cap (cord shortened by -0.40 m so lamp bottom sits at 1.750 m)
    mb.add_cylinder((lx, ly + 0.24, lz), radius=0.022, height=0.32, segs=12, axis="y", mat="R1_GreyMetal")
    mb.add_cylinder((lx, ly + 0.12, lz), radius=0.15, height=0.08, segs=20, axis="y", mat="R1_GreyMetal", bevel=0.015)

    # Warm glowing rounded lantern bulb (`R1_LampAmber`, 24 segments)
    ruins_common.add_domed_cylinder(
        mb,
        center_u=(lx, ly - 0.02, lz),
        radius=0.15,
        total_length=0.29,
        dome_depth=0.08,
        mat="R1_LampAmber",
        axis="y",
        segs=24,
        barrel_rings=2,
        cap_rings=3,
        clip_min_y_u=None,
    )

    # Protective dark-grey wireframe cage rings around the glowing bulb (matching ruins_v1_dawn.png)
    for ry_off in (0.04, -0.06):
        mb.add_ring((lx, ly + ry_off, lz), r_outer=0.168, r_inner=0.150, height=0.022, segs=24, axis="y", mat="R1_GreyMetal")
    mb.add_cylinder((lx, ly - 0.17, lz), radius=0.085, height=0.04, segs=16, axis="y", mat="R1_GreyMetal", bevel=0.008)

    ruins_common.to_ruin_object(mb, "HangingLamp", parent=root)


def _build_workbench_and_racks(root) -> None:
    """Build Kenji's workbench visible on the left inside the workshop, tool board, and rear cabinet."""
    mb = r1kit.MeshBuilder()

    # 1. Kenji's Workbench on the left side of the interior (`x = -0.78, z = +0.28`, top at `y = 0.880 m`)
    wx, wz = -0.78, 0.28
    w_width = 1.62
    w_depth = 0.78
    # Sturdy tabletop (top face at y = 0.845 + 0.035 = 0.880 m)
    mb.add_bevel_box((wx, 0.845, wz), (w_width, 0.07, w_depth), bevel=0.012, mat="R1_GreyMetal", segs=1)
    # Under-table frame & right-side tool drawer
    mb.add_bevel_box((wx, 0.765, wz), (w_width - 0.12, 0.09, w_depth - 0.10), bevel=0.008, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((wx + 0.42, 0.645, wz), (0.56, 0.16, w_depth - 0.12), bevel=0.010, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((wx + 0.42, 0.645, wz - w_depth * 0.5 + 0.04), (0.22, 0.035, 0.04), bevel=0.004, mat="R1_GreyMetal", segs=1)

    # 4 square legs & lower stretcher rails
    for lx_off in (-0.72, 0.72):
        for lz_off in (-0.32, 0.32):
            mb.add_bevel_box((wx + lx_off, 0.4225, wz + lz_off), (0.075, 0.805, 0.075), bevel=0.008, mat="R1_GreyMetal", segs=1)
        mb.add_bevel_box((wx + lx_off, 0.22, wz), (0.06, 0.05, w_depth - 0.08), bevel=0.006, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((wx, 0.22, wz + 0.30), (w_width - 0.10, 0.05, 0.06), bevel=0.006, mat="R1_GreyMetal", segs=1)

    # Machinist bench vice & small instrument box on top of the workbench
    mb.add_bevel_box((wx + 0.18, 0.935, wz - 0.14), (0.26, 0.12, 0.24), bevel=0.010, mat="R1_GreyMetal", segs=1)
    mb.add_cylinder((wx + 0.18, 0.935, wz - 0.28), radius=0.020, height=0.14, segs=10, axis="z", mat="R1_BareSteel")
    mb.add_bevel_box((wx - 0.38, 0.915, wz + 0.06), (0.34, 0.08, 0.22), bevel=0.008, mat="R1_OrangeStripe", segs=1)

    # 2. Interior Wall Rail & Riveted Sage Tool Board (`R1_SagePatch`)
    mb.add_bevel_box((-1.32, 1.12, 0.45), (0.06, 0.56, 0.06), bevel=0.006, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box(
        (-2.12, 1.38, 0.15),
        (0.06, 0.68, 1.48),
        bevel=0.010,
        mat="R1_SagePatch",
        rot_deg_u=(0.0, 0.0, -14.0),
        segs=1,
    )

    # 3. Rear-right storage cabinet (`R1_CreamEnamel` / `R1_GreyMetal`, 1.80 m tall)
    mb.add_bevel_box((1.36, 0.90, 1.65), (0.68, 1.72, 0.72), bevel=0.018, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((1.36, 1.77, 1.65), (0.70, 0.06, 0.74), bevel=0.008, mat="R1_GreyMetal", segs=1)

    ruins_common.to_ruin_object(mb, "WorkbenchAndRacks", parent=root)


def _build_collision_proxies(root) -> None:
    """Build authored convex hull collision proxies (`UCX_<Part>_<n>`, <= 32 faces each).

    Leaves the 3.24 m wide x 1.98 m high front portal doorway and interior floor open so
    Rover 07 (1.47 W x 2.01 L x 1.59 H) can drive into the workshop and scan the interior.
    """
    # Left lower curved side wall
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_1",
        ruins_common.convex_box_points_u(
            center_u=(-2.42, 1.02, 0.06),
            size_u=(0.28, 2.00, 5.36),
            clip_min_y_u=0.02,
        ),
    )
    # Right lower curved side wall
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_2",
        ruins_common.convex_box_points_u(
            center_u=(2.42, 1.02, 0.06),
            size_u=(0.28, 2.00, 5.36),
            clip_min_y_u=0.02,
        ),
    )
    # Closed rear arched bulkhead wall
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_3",
        ruins_common.convex_box_points_u(
            center_u=(0.0, 1.46, 2.66),
            size_u=(4.84, 2.88, 0.20),
            top_scale_xz=(0.72, 1.0),
            clip_min_y_u=0.02,
        ),
    )
    # Front-left portal jamb wall (leaves x in [-1.62, +1.62] open)
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_4",
        ruins_common.convex_box_points_u(
            center_u=(-2.03, 1.02, -2.62),
            size_u=(0.82, 2.00, 0.20),
            clip_min_y_u=0.02,
        ),
    )
    # Front-right portal jamb wall (leaves x in [-1.62, +1.62] open)
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_5",
        ruins_common.convex_box_points_u(
            center_u=(2.03, 1.02, -2.62),
            size_u=(0.82, 2.00, 0.20),
            clip_min_y_u=0.02,
        ),
    )
    # Upper roof arch & front gable lintel above y = 1.98 m (rests on side/rear/jamb walls)
    ruins_common.add_convex_proxy(
        root,
        "UCX_QuonsetShell_6",
        ruins_common.convex_box_points_u(
            center_u=(0.0, 2.49, 0.06),
            size_u=(4.52, 1.02, 5.36),
            top_scale_xz=(0.56, 1.0),
            clip_min_y_u=1.98,
        ),
    )
    # Kenji's workbench on the left side of the interior (top at y = 0.94 m, below Scan_1 at y = 0.96 m)
    ruins_common.add_convex_proxy(
        root,
        "UCX_WorkbenchAndRacks_1",
        ruins_common.convex_box_points_u(
            center_u=(-0.78, 0.48, 0.28),
            size_u=(1.62, 0.92, 0.78),
            clip_min_y_u=0.02,
        ),
    )
    # Rear-right storage cabinet
    ruins_common.add_convex_proxy(
        root,
        "UCX_WorkbenchAndRacks_2",
        ruins_common.convex_box_points_u(
            center_u=(1.36, 0.91, 1.65),
            size_u=(0.70, 1.78, 0.74),
            clip_min_y_u=0.02,
        ),
    )


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("KenjiWorkshop")

    _build_quonset_shell(root)
    _build_hoist_rail(root)
    _build_hanging_lamp(root)
    _build_workbench_and_racks(root)
    _build_collision_proxies(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (-0.78, 0.96, 0.28), parent=root)   # Kenji's workbench
    r1kit.add_empty("Scan_2", (-0.10, 1.96, -1.62), parent=root)  # Overhead hoist trolley & hook
    r1kit.add_empty("Scan_3", (0.88, 1.94, -1.56), parent=root)   # Warm hanging workshop lamp

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="kenji_workshop",
        fbx_name="KenjiWorkshop.fbx",
        footprint_radius=4.0,
        cam_angle_deg=-14.0,
        cam_elev_deg=7.5,
        skip_renders=skip_renders,
    )
    wire_path = ruins_common.render_ruin_collision_wireframe(
        root,
        slug="kenji_workshop",
        cam_angle_deg=-18.0,
        cam_elev_deg=18.0,
    )
    print(f"[kenji_workshop] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}, {wire_path.name}")


if __name__ == "__main__":
    build(skip_renders="--skip-renders" in sys.argv)
