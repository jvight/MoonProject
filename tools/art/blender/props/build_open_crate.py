"""Build R1 Open Medium Cargo Crate (`Crate_M_Open.fbx`) with visible contents.

Deliverable 1 (`Logs/TASK.md`):
- Open variant of `Crate_M` with `Lid` open ~110 deg resting on its rear hinge.
- Separate direct child MESH objects for gameplay collection:
  * `Igniter`     : ~0.18 m 1980s ignition exciter module (ribbed ceramic insulator,
                    braided copper lead, stencilled tag, brass hex collar & spark tip).
  * `MetalBundle` : 2 strapped structural alloy plates with rubber shims & ratchet buckles.
  * `WiringSpool` : 1 small industrial copper wiring spool with flanged drum & wound turns.
- `RimGlow`       : faint warm inner glow strip on the crate upper rim (bloom-safe).
- Preserves `Lid`, `Lid/Seal`, `Lid/CutPoint`, and `ItemSocket` hierarchy.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import crates  # noqa: E402
import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


LID_OPEN_ANGLE_DEG = 110.0


def _rotate_Vec_around_pivot_u(
    pt_u: tuple[float, float, float],
    pivot_u: tuple[float, float, float],
    rx_deg_bl: float,
) -> tuple[float, float, float]:
    """Rotate a Unity point around a Unity pivot by `rx_deg_bl` around Blender +X."""
    p_b = r1kit.u_pos(*pt_u)
    piv_b = r1kit.u_pos(*pivot_u)
    rot_m = Matrix.Rotation(math.radians(rx_deg_bl), 3, "X")
    rel = rot_m @ (p_b - piv_b)
    out_b = piv_b + rel
    return (float(out_b.x), float(out_b.z), float(out_b.y))


def build_igniter_into(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
    pitch_deg: float = 0.0,
    yaw_deg: float = 0.0,
) -> None:
    """Build the ~0.18 m 1980s spare igniter module into `mb`.

    Local layout before rotation (centred around `center_u`, total length ~0.182 m along X):
    - X in [-0.090, -0.014]: die-cast exciter housing (`R1_GreyMetal`) + orange collar (`R1_OrangeStripe`)
      + top stencilled tag (`R1_Signage`) + brass terminal studs (`R1_Brass`).
    - X in [-0.014, +0.048]: 4-shed high-voltage ribbed porcelain insulator (`R1_CreamEnamel`).
    - X in [+0.048, +0.091]: brass hex gland nut (`R1_Brass`), steel igniter barrel (`R1_BareSteel`),
      and exposed copper spark electrode tip (`R1_Copper`).
    - Braided high-temp copper lead (`R1_Copper`) curving alongside the housing (-Z side)
      from a rubber strain-relief boot (`R1_Rubber`) to a 2-pin brass coupler plug (`R1_Brass`).
    """
    cx, cy, cz = center_u

    # Build in a temporary MeshBuilder at origin so we can apply clean pitch/yaw + translation
    # while keeping signage plate registration accurate when pitch/yaw == 0, or using explicit geometry.
    # For full compatibility with signage UV projection in both crate and standalone icon builds,
    # we place primitives relative to `center_u`.
    # 1. Rear die-cast ignition exciter box (X: -0.088 .. -0.014, W_z: 0.056, H_y: 0.046)
    mb.add_bevel_box(
        center_u=(cx - 0.051, cy, cz - 0.004),
        size_u=(0.074, 0.046, 0.056),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=2,
    )
    # Warm-orange identification band around the forward end of the exciter housing
    mb.add_bevel_box(
        center_u=(cx - 0.022, cy, cz - 0.004),
        size_u=(0.016, 0.048, 0.058),
        mat="R1_OrangeStripe",
        bevel=0.0025,
        segments=1,
    )
    # Rear cooling fins / grip ribs on exciter housing
    for fx in (-0.078, -0.064, -0.050, -0.038):
        mb.add_bevel_box(
            center_u=(cx + fx, cy, cz - 0.004),
            size_u=(0.005, 0.050, 0.060),
            mat="R1_BareSteel",
            bevel=0.0012,
            segments=1,
        )
    # Four corner mounting lugs with hex bolts on exciter housing
    for sx in (-0.080, -0.022):
        for sz in (-0.024, 0.016):
            mb.add_cylinder(
                center_u=(cx + sx, cy + 0.0245, cz + sz),
                radius=0.0032,
                length=0.005,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=6,
                bevel=0.0008,
                bevel_segments=1,
            )

    # Top stencilled specification tag plate (`R1_Signage` + tight `R1_Brass` bezel)
    mb.add_bevel_box(
        center_u=(cx - 0.052, cy + 0.0235, cz - 0.004),
        size_u=(0.050, 0.0035, 0.038),
        mat="R1_Brass",
        bevel=0.001,
        segments=1,
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(cx - 0.052, cy + 0.0245, cz - 0.004),
        width=0.044,
        height=0.032,
        cell_name="HAZARD",
        rot_deg_u=(90.0, 0.0, 0.0),
        thickness=0.003,
        frame_mat=None,
        rivets=False,
    )

    # 2. High-voltage ribbed ceramic insulator stack (X: -0.014 .. +0.048)
    # Central ceramic core cylinder
    mb.add_cylinder(
        center_u=(cx + 0.017, cy, cz - 0.004),
        radius=0.015,
        length=0.062,
        mat="R1_CreamEnamel",
        axis_u="X",
        segments=18,
        bevel=0.002,
        bevel_segments=1,
    )
    # 4 prominent porcelain sheds (insulator ribs)
    for rx in (-0.006, 0.009, 0.024, 0.039):
        mb.add_cylinder(
            center_u=(cx + rx, cy, cz - 0.004),
            radius=0.023,
            length=0.008,
            mat="R1_CreamEnamel",
            axis_u="X",
            segments=20,
            bevel=0.0022,
            bevel_segments=1,
        )
    # Dark rubber HV seal O-rings at ends of ceramic stack
    for rx in (-0.013, 0.046):
        mb.add_cylinder(
            center_u=(cx + rx, cy, cz - 0.004),
            radius=0.018,
            length=0.004,
            mat="R1_Rubber",
            axis_u="X",
            segments=16,
            bevel=0.001,
            bevel_segments=1,
        )

    # 3. Brass hex gland nut + steel igniter nozzle barrel + copper spark tip (X: +0.048 .. +0.091)
    mb.add_cylinder(
        center_u=(cx + 0.054, cy, cz - 0.004),
        radius=0.019,
        length=0.012,
        mat="R1_Brass",
        axis_u="X",
        segments=6,
        bevel=0.002,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(cx + 0.070, cy, cz - 0.004),
        radius=0.012,
        length=0.022,
        mat="R1_BareSteel",
        axis_u="X",
        segments=16,
        bevel=0.0018,
        bevel_segments=1,
    )
    # Copper central electrode core & twin ground shroud prongs at tip (+X end)
    mb.add_cylinder(
        center_u=(cx + 0.084, cy, cz - 0.004),
        radius=0.0055,
        length=0.014,
        mat="R1_Copper",
        axis_u="X",
        segments=12,
        bevel=0.0015,
        bevel_segments=1,
    )
    for sy in (-0.009, 0.009):
        mb.add_bevel_box(
            center_u=(cx + 0.083, cy + sy, cz - 0.004),
            size_u=(0.014, 0.0035, 0.006),
            mat="R1_BareSteel",
            bevel=0.0008,
            segments=1,
        )

    # 4. Braided copper lead with rubber strain-relief boot and brass coupler plug (+Z side)
    # Rubber strain-relief gland exiting side of exciter housing
    mb.add_cylinder(
        center_u=(cx - 0.062, cy - 0.004, cz + 0.029),
        radius=0.008,
        length=0.014,
        mat="R1_Rubber",
        axis_u="Z",
        segments=12,
        bevel=0.0015,
        bevel_segments=1,
    )
    # Segmented/braided copper lead running forward along +Z side of the module
    lead_nodes = [
        (cx - 0.062, cy - 0.004, cz + 0.036, 0.0, 0.0),
        (cx - 0.048, cy - 0.004, cz + 0.040, 0.0, 14.0),
        (cx - 0.030, cy - 0.003, cz + 0.042, 0.0, 4.0),
        (cx - 0.012, cy - 0.002, cz + 0.041, 0.0, -8.0),
        (cx + 0.006, cy - 0.002, cz + 0.038, 0.0, -12.0),
        (cx + 0.024, cy - 0.002, cz + 0.034, 0.0, -10.0),
    ]
    for lx, ly, lz, rx_d, ry_d in lead_nodes:
        mb.add_cylinder(
            center_u=(lx, ly, lz),
            radius=0.0062,
            length=0.021,
            mat="R1_Copper",
            axis_u="X",
            segments=12,
            bevel=0.0015,
            bevel_segments=1,
            rot_deg_u=(rx_d, ry_d, 0.0),
        )
        # Braided collar ring
        mb.add_cylinder(
            center_u=(lx, ly, lz),
            radius=0.0072,
            length=0.005,
            mat="R1_Brass",
            axis_u="X",
            segments=10,
            bevel=0.0008,
            bevel_segments=1,
            rot_deg_u=(rx_d, ry_d, 0.0),
        )

    # Terminal coupler plug at the end of the braided lead
    mb.add_cylinder(
        center_u=(cx + 0.042, cy - 0.002, cz + 0.031),
        radius=0.0095,
        length=0.020,
        mat="R1_Brass",
        axis_u="X",
        segments=12,
        bevel=0.002,
        bevel_segments=1,
    )
    mb.add_bevel_box(
        center_u=(cx + 0.042, cy + 0.009, cz + 0.031),
        size_u=(0.014, 0.004, 0.012),
        mat="R1_OrangeStripe",
        bevel=0.001,
        segments=1,
    )
    for pz in (-0.0035, 0.0035):
        mb.add_cylinder(
            center_u=(cx + 0.055, cy - 0.002, cz + 0.031 + pz),
            radius=0.0022,
            length=0.009,
            mat="R1_BareSteel",
            axis_u="X",
            segments=8,
            bevel=0.0005,
            bevel_segments=1,
        )


def build_metal_bundle_into(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Build 2 strapped structural alloy plates (`MetalBundle`) into `mb`."""
    cx, cy, cz = center_u
    plate_w = 0.148
    plate_d = 0.250
    plate_h = 0.022
    gap_h = 0.008

    # Plate 1 (Bottom heavy hull plate in R1_GreyMetal)
    p1_y = cy - (plate_h * 0.5 + gap_h * 0.5)
    mb.add_bevel_box(
        center_u=(cx, p1_y, cz),
        size_u=(plate_w, plate_h, plate_d),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=2,
    )
    # Rubber separator skids between Plate 1 and Plate 2 so the two-plate stack is unmistakable
    for sz in (-0.088, 0.0, 0.088):
        mb.add_bevel_box(
            center_u=(cx, cy, cz + sz),
            size_u=(plate_w * 0.88, gap_h, 0.020),
            mat="R1_Rubber",
            bevel=0.0015,
            segments=1,
        )

    # Plate 2 (Top brushed steel deck plate in R1_BareSteel, slightly stepped so both edges read clearly)
    p2_y = cy + (plate_h * 0.5 + gap_h * 0.5)
    mb.add_bevel_box(
        center_u=(cx + 0.004, p2_y, cz - 0.003),
        size_u=(plate_w * 0.95, plate_h, plate_d * 0.95),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=2,
    )
    # Raised longitudinal stiffening ribs on top plate
    for rx in (-0.042, 0.048):
        mb.add_bevel_box(
            center_u=(cx + rx, p2_y + plate_h * 0.5 + 0.003, cz - 0.003),
            size_u=(0.016, 0.006, plate_d * 0.82),
            mat="R1_GreyMetal",
            bevel=0.0015,
            segments=1,
        )
    # Corner alignment studs on top plate
    for sx in (-0.056, 0.062):
        for sz in (-0.098, 0.092):
            mb.add_cylinder(
                center_u=(cx + sx, p2_y + plate_h * 0.5 + 0.003, cz + sz),
                radius=0.005,
                length=0.006,
                mat="R1_Brass",
                axis_u="Y",
                segments=6,
                bevel=0.001,
                bevel_segments=1,
            )

    # Two warm-orange cargo tension straps (`R1_OrangeStripe`) wrapping both plates + brass ratchet buckles
    total_h = plate_h * 2.0 + gap_h
    for sz in (-0.062, 0.058):
        mb.add_bevel_box(
            center_u=(cx + 0.002, cy, cz + sz),
            size_u=(plate_w + 0.010, total_h + 0.010, 0.022),
            mat="R1_OrangeStripe",
            bevel=0.0025,
            segments=1,
        )
        # Ratchet buckle on top of each strap
        mb.add_bevel_box(
            center_u=(cx + 0.002, cy + total_h * 0.5 + 0.008, cz + sz),
            size_u=(0.034, 0.010, 0.028),
            mat="R1_Brass",
            bevel=0.002,
            segments=1,
        )


def build_wiring_spool_into(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    """Build 1 small industrial copper wiring spool (`WiringSpool`) into `mb`."""
    cx, cy, cz = center_u
    flange_r = 0.072
    flange_h = 0.010
    drum_h = 0.078
    half_h = drum_h * 0.5

    # Bottom and top flanged reel discs (`R1_GreyMetal`)
    for sy in (-1.0, 1.0):
        fy = cy + sy * (half_h + flange_h * 0.5)
        mb.add_cylinder(
            center_u=(cx, fy, cz),
            radius=flange_r,
            length=flange_h,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=24,
            bevel=0.002,
            bevel_segments=1,
        )
    # Warm-orange identification collar ring on top flange
    mb.add_ring(
        center_u=(cx, cy + half_h + flange_h + 0.0015, cz),
        r_outer=flange_r * 0.88,
        r_inner=flange_r * 0.64,
        height=0.003,
        segs=24,
        axis="y",
        mat="R1_OrangeStripe",
    )
    # Central steel spindle hub & brass locking arbor nut
    mb.add_cylinder(
        center_u=(cx, cy, cz),
        radius=0.022,
        length=drum_h + flange_h * 2.0 + 0.012,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=16,
        bevel=0.002,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(cx, cy + half_h + flange_h + 0.008, cz),
        radius=0.016,
        length=0.008,
        mat="R1_Brass",
        axis_u="Y",
        segments=6,
        bevel=0.0015,
        bevel_segments=1,
    )

    # Wound copper cable core drum (`R1_Copper`) + stacked coil turn ridges so individual wraps read
    mb.add_cylinder(
        center_u=(cx, cy, cz),
        radius=0.054,
        length=drum_h,
        mat="R1_Copper",
        axis_u="Y",
        segments=24,
        bevel=0.002,
        bevel_segments=1,
    )
    n_turns = 7
    for i in range(n_turns):
        ty = cy - half_h + (i + 0.5) * (drum_h / n_turns)
        mb.add_cylinder(
            center_u=(cx, ty, cz),
            radius=0.0585,
            length=(drum_h / n_turns) * 0.78,
            mat="R1_Copper",
            axis_u="Y",
            segments=24,
            bevel=0.0022,
            bevel_segments=1,
        )

    # Rubber cable-keeper clip on top flange rim holding the free copper wire tail
    mb.add_bevel_box(
        center_u=(cx - flange_r * 0.78, cy + half_h + flange_h * 0.6, cz + 0.018),
        size_u=(0.018, 0.016, 0.022),
        mat="R1_Rubber",
        bevel=0.002,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(cx - flange_r * 0.78, cy + half_h + flange_h + 0.004, cz + 0.032),
        radius=0.0045,
        length=0.024,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.001,
        bevel_segments=1,
    )


def build_open_crate() -> bpy.types.Object:
    """Build `Crate_M_Open` with open lid (~110 deg), `RimGlow`, `Igniter`, `MetalBundle`, and `WiringSpool`."""
    spec = crates.CRATE_SPECS["Crate_M"]
    wx, hy, dz = spec["size_u"]
    hx = wx * 0.5
    hz = dz * 0.5
    hinge_y = float(spec["hinge_y"])
    hinge_z = float(spec["hinge_z"])
    item_y = float(spec["item_y"])
    cw = float(spec["corner_w"])
    skid_h = float(spec["skid_h"])
    inset = float(spec["wall_inset"])
    bev = float(spec["bevel_main"])

    body_mat = spec["body_mat"]
    lid_mat = spec["lid_mat"]
    side_accent_mat = spec["side_accent_mat"]
    bumper_mat = spec["bumper_mat"]

    mb_body = r1kit.MeshBuilder("Crate_M_Open", pivot_u=(0.0, 0.0, 0.0))

    # 1. Base skids
    skid_w = cw * 1.15
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (hx - skid_w * 0.5), skid_h * 0.5, 0.0),
            size_u=(skid_w, skid_h, dz),
            mat=bumper_mat,
            bevel=min(0.004, skid_h * 0.25),
            segments=1,
        )
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, skid_h * 0.5, sz * (hz - skid_w * 0.5)),
            size_u=(wx - skid_w * 1.6, skid_h * 0.85, skid_w * 0.9),
            mat=bumper_mat,
            bevel=min(0.004, skid_h * 0.22),
            segments=1,
        )

    # 2. Lower tub walls & floor + raised interior equipment cradle tray so contents sit high & visible
    tub_bot = skid_h * 0.85
    tub_top = hinge_y - 0.004
    tub_h = tub_top - tub_bot
    tub_cy = 0.5 * (tub_bot + tub_top)
    shell_w = wx - inset * 2.0
    shell_d = dz - inset * 2.0
    wall_t = max(0.024, min(wx, dz) * 0.085)

    floor_h = max(0.022, tub_h * 0.24)
    mb_body.add_bevel_box(
        center_u=(0.0, tub_bot + floor_h * 0.5, 0.0),
        size_u=(shell_w, floor_h, shell_d),
        mat=body_mat,
        bevel=bev,
        segments=2,
    )
    # Raised matte painted-metal cargo deck (`y = 0.082 .. 0.162`) with equipment partition ribs
    cradle_top = 0.162
    cradle_h = cradle_top - (tub_bot + floor_h - 0.002)
    mb_body.add_bevel_box(
        center_u=(0.0, cradle_top - cradle_h * 0.5, 0.0),
        size_u=(shell_w - wall_t * 1.8, cradle_h, shell_d - wall_t * 1.8),
        mat=bumper_mat,
        bevel=0.004,
        segments=1,
    )
    # Rubber shock-absorbing partition dividers between Left (MetalBundle), Center (Igniter), Right (WiringSpool)
    for div_x in (-0.055, 0.068):
        mb_body.add_bevel_box(
            center_u=(div_x, cradle_top + 0.012, 0.0),
            size_u=(0.012, 0.024, shell_d - wall_t * 2.1),
            mat="R1_Rubber",
            bevel=0.002,
            segments=1,
        )
    # Raised angled presentation cradle in center bay for the Spare Igniter
    mb_body.add_bevel_box(
        center_u=(0.006, cradle_top + 0.018, 0.032),
        size_u=(0.098, 0.036, 0.110),
        mat="R1_Rubber",
        bevel=0.003,
        segments=1,
    )

    # Front (+Z) and Rear (-Z) exterior tub walls (R1_CreamEnamel) + interior matte grey liner (R1_GreyMetal)
    wall_outer_t = wall_t * 0.56
    wall_inner_t = wall_t * 0.48
    liner_bot = cradle_top - 0.004
    liner_top = tub_top - 0.002
    liner_h = liner_top - liner_bot
    liner_cy = 0.5 * (liner_bot + liner_top)
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, tub_cy, sz * (shell_d * 0.5 - wall_outer_t * 0.5)),
            size_u=(shell_w, tub_h, wall_outer_t),
            mat=body_mat,
            bevel=bev * 0.85,
            segments=1,
        )
        mb_body.add_bevel_box(
            center_u=(0.0, liner_cy, sz * (shell_d * 0.5 - wall_t + wall_inner_t * 0.5)),
            size_u=(shell_w - wall_t * 1.2, liner_h, wall_inner_t),
            mat=bumper_mat,
            bevel=0.004,
            segments=1,
        )
    # Left (-X) and Right (+X) exterior tub walls (R1_SagePatch) + interior matte grey liner (R1_GreyMetal)
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 - wall_outer_t * 0.5), tub_cy, 0.0),
            size_u=(wall_outer_t, tub_h, shell_d - wall_t * 1.6),
            mat=side_accent_mat,
            bevel=bev * 0.85,
            segments=1,
        )
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 - wall_t + wall_inner_t * 0.5), liner_cy, 0.0),
            size_u=(wall_inner_t, liner_h, shell_d - wall_t * 1.9),
            mat=bumper_mat,
            bevel=0.004,
            segments=1,
        )

    # 3. Corner bumper columns
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb_body.add_bevel_box(
                center_u=(sx * (hx - cw * 0.5), (skid_h + tub_top) * 0.5, sz * (hz - cw * 0.5)),
                size_u=(cw, tub_top - skid_h * 0.8, cw),
                mat=bumper_mat,
                bevel=min(0.006, cw * 0.16),
                segments=1,
            )

    # 4. Warm-orange waist band
    band_h = max(0.032, hy * 0.14)
    band_y = tub_bot + tub_h * 0.52
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, band_y, sz * (shell_d * 0.5 + 0.003)),
            size_u=(wx - cw * 1.85, band_h, 0.010),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + 0.003), band_y, 0.0),
            size_u=(0.010, band_h, dz - cw * 1.85),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )

    # 5. Stiffening ribs
    rib_t = min(inset * 0.72, 0.012)
    rib_w = max(0.016, wx * 0.036)
    rib_h = tub_h * 0.82
    n_rx = int(spec["rib_count_x"])
    usable_x = wx - cw * 2.4
    for i in range(n_rx):
        rx = 0.0 if n_rx == 1 else (-usable_x * 0.5 + usable_x * (i / (n_rx - 1)))
        for sz in (-1.0, 1.0):
            if sz > 0.0 and abs(rx) < spec["signage_size"][0] * 0.62:
                continue
            mb_body.add_bevel_box(
                center_u=(rx, tub_cy, sz * (shell_d * 0.5 + rib_t * 0.45)),
                size_u=(rib_w, rib_h, rib_t),
                mat=bumper_mat,
                bevel=0.0025,
                segments=1,
            )

    n_rz = int(spec["rib_count_z"])
    usable_z = dz - cw * 2.4
    for i in range(n_rz):
        rz = 0.0 if n_rz == 1 else (-usable_z * 0.5 + usable_z * (i / (n_rz - 1)))
        for sx in (-1.0, 1.0):
            mb_body.add_bevel_box(
                center_u=(sx * (shell_w * 0.5 + rib_t * 0.45), tub_cy, rz),
                size_u=(rib_t, rib_h, rib_w),
                mat=bumper_mat,
                bevel=0.0025,
                segments=1,
            )

    # 6. Side bail handles
    handle_d = min(0.14, dz * 0.36)
    handle_out = min(inset * 0.88, 0.014)
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + handle_out * 0.5), tub_top - 0.028, 0.0),
            size_u=(handle_out, 0.026, handle_d),
            mat=bumper_mat,
            bevel=0.0035,
            segments=1,
        )
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + handle_out * 0.72), tub_top - 0.028, 0.0),
            size_u=(handle_out * 0.55, 0.018, handle_d * 0.62),
            mat="R1_Rubber",
            bevel=0.0025,
            segments=1,
        )

    # 7. Front signage plate (non-emissive SEC-04 stencil cell)
    sig_w, sig_h = spec["signage_size"]
    sig_z = shell_d * 0.5 + 0.004
    ruins_common.add_signage_plate(
        mb_body,
        center_u=(0.0, band_y, sig_z),
        width=sig_w,
        height=sig_h,
        cell_name="SEC04_HABITAT",
        rot_deg_u=(0.0, 0.0, 0.0),
        thickness=0.008,
        frame_mat=bumper_mat,
        rivets=True,
    )

    # 8. Rear hinge knuckles & lower latch keepers
    knuckle_r = min(0.012, inset * 0.85)
    knuckle_len = min(0.08, wx * 0.18)
    for hx_off in (-wx * 0.24, wx * 0.24):
        mb_body.add_cylinder(
            center_u=(hx_off, hinge_y, -hinge_z),
            radius=knuckle_r,
            length=knuckle_len,
            mat=bumper_mat,
            axis_u="X",
            segments=8,
            bevel=0.0025,
            bevel_segments=1,
        )
        mb_body.add_cylinder(
            center_u=(hx_off, hinge_y, -hinge_z),
            radius=knuckle_r * 0.55,
            length=knuckle_len * 1.18,
            mat=bumper_mat,
            axis_u="X",
            segments=6,
            bevel=0.002,
            bevel_segments=1,
        )

    for lx in spec["latch_offsets_x"]:
        mb_body.add_bevel_box(
            center_u=(lx, hinge_y - 0.022, shell_d * 0.5 + rib_t * 0.5),
            size_u=(0.036, 0.028, rib_t),
            mat=bumper_mat,
            bevel=0.003,
            segments=1,
        )

    root_obj = ruins_common.to_ruin_object(mb_body, "Crate_M_Open", parent=None, origin_u=(0.0, 0.0, 0.0))

    # =========================================================================
    # Child `RimGlow`: faint warm inner glow strip along the upper crate rim
    # Centered cleanly on the flat top of the tub walls without bevel overlap
    # =========================================================================
    rim_pivot_u = (0.0, tub_top - 0.004, 0.0)
    mb_rim = r1kit.MeshBuilder("RimGlow", pivot_u=rim_pivot_u)
    wall_mid_x = shell_w * 0.5 - wall_t * 0.5
    wall_mid_z = shell_d * 0.5 - wall_t * 0.5
    strip_t = 0.010
    strip_h = 0.006
    rim_cy = tub_top + 0.002
    for sz in (-1.0, 1.0):
        mb_rim.add_bevel_box(
            center_u=(0.0, rim_cy, sz * wall_mid_z),
            size_u=(wall_mid_x * 2.0 + strip_t, strip_h, strip_t),
            mat="R1_LampAmber",
            bevel=0.0015,
            segments=1,
        )
    for sx in (-1.0, 1.0):
        mb_rim.add_bevel_box(
            center_u=(sx * wall_mid_x, rim_cy, 0.0),
            size_u=(strip_t, strip_h, wall_mid_z * 2.0 - strip_t - 0.001),
            mat="R1_LampAmber",
            bevel=0.0015,
            segments=1,
        )
    ruins_common.to_ruin_object(mb_rim, "RimGlow", parent=root_obj, origin_u=rim_pivot_u)

    # =========================================================================
    # Child `Lid` (pivoted on rear hinge `(0.0, hinge_y, -hinge_z)`, open 110 deg)
    # =========================================================================
    lid_pivot_u = (0.0, hinge_y, -hinge_z)
    mb_lid = r1kit.MeshBuilder("Lid", pivot_u=lid_pivot_u)

    lid_bot = hinge_y + 0.004
    lid_rib_h = max(0.012, hy * 0.045)
    lid_shell_top = hy - lid_rib_h
    lid_shell_h = lid_shell_top - lid_bot
    lid_shell_cy = 0.5 * (lid_bot + lid_shell_top)

    # Main bevelled lid shell in dark matte R1_GreyMetal so the open lid underside & border are calm charcoal grey
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_shell_cy, 0.0),
        size_u=(wx - inset * 1.2, lid_shell_h, dz - inset * 1.2),
        mat=bumper_mat,
        bevel=bev,
        segments=2,
    )
    # Exterior top lid plate (R1_CreamEnamel) between the corner bumpers on the back of the lid (zero coplanar faces)
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_shell_top - 0.008, 0.0),
        size_u=(wx - cw * 2.05, 0.020, dz - cw * 2.05),
        mat=lid_mat,
        bevel=0.004,
        segments=1,
    )
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb_lid.add_bevel_box(
                center_u=(sx * (hx - cw * 0.5), (lid_bot + hy) * 0.5, sz * (hz - cw * 0.5)),
                size_u=(cw, hy - lid_bot, cw),
                mat=bumper_mat,
                bevel=min(0.005, cw * 0.15),
                segments=1,
            )
    for i in range(n_rx):
        rx = 0.0 if n_rx == 1 else (-usable_x * 0.5 + usable_x * (i / (n_rx - 1)))
        mb_lid.add_bevel_box(
            center_u=(rx, hy - lid_rib_h * 0.5, 0.0),
            size_u=(rib_w * 1.15, lid_rib_h, dz - cw * 1.6),
            mat=bumper_mat,
            bevel=min(0.003, lid_rib_h * 0.25),
            segments=1,
        )
    strap_w = max(0.036, wx * 0.09)
    for st_x in spec["strap_offsets_x"]:
        mb_lid.add_bevel_box(
            center_u=(st_x, lid_shell_cy + lid_shell_h * 0.15, 0.0),
            size_u=(strap_w, lid_shell_h * 0.78, dz - inset * 0.8),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )
    # Underside lid inner pressure-pad frame + dark R1_GreyMetal baffle plate (stepped proud, zero coplanar faces)
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_bot + 0.002, 0.0),
        size_u=(shell_w - wall_t * 1.9, 0.010, shell_d - wall_t * 1.9),
        mat=bumper_mat,
        bevel=0.003,
        segments=1,
    )
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_bot - 0.002, 0.0),
        size_u=(shell_w - wall_t * 2.5, 0.008, shell_d - wall_t * 2.5),
        mat=bumper_mat,
        bevel=0.0025,
        segments=1,
    )
    latch_z = shell_d * 0.5 + rib_t * 0.55
    for lx in spec["latch_offsets_x"]:
        mb_lid.add_bevel_box(
            center_u=(lx, hinge_y + 0.004, latch_z),
            size_u=(0.042, 0.056, rib_t * 1.1),
            mat=bumper_mat,
            bevel=0.003,
            segments=1,
        )
        mb_lid.add_bevel_box(
            center_u=(lx, hinge_y - 0.012, latch_z + 0.003),
            size_u=(0.028, 0.022, 0.008),
            mat=bumper_mat,
            bevel=0.002,
            segments=1,
        )

    # Rotate Lid mesh vertices open by +110 deg around Blender +X at the rear hinge pivot (0,0,0 in mb_lid)
    open_rot = Matrix.Rotation(math.radians(LID_OPEN_ANGLE_DEG), 3, "X")
    bmesh.ops.rotate(mb_lid.bm, verts=list(mb_lid.bm.verts), cent=(0.0, 0.0, 0.0), matrix=open_rot)
    lid_obj = ruins_common.to_ruin_object(mb_lid, "Lid", parent=root_obj, origin_u=lid_pivot_u)

    # =========================================================================
    # Child `Lid/Seal` (rotated open 110 deg with the lid, clean non-overlapping gasket loop)
    # =========================================================================
    mb_seal = r1kit.MeshBuilder("Seal", pivot_u=lid_pivot_u)
    seal_h = 0.006
    seal_cy = lid_bot - 0.002
    for sz in (-1.0, 1.0):
        mb_seal.add_bevel_box(
            center_u=(0.0, seal_cy, sz * wall_mid_z),
            size_u=(wall_mid_x * 2.0 + strip_t, seal_h, strip_t),
            mat="R1_LampAmber",
            bevel=0.0015,
            segments=1,
        )
    for sx in (-1.0, 1.0):
        mb_seal.add_bevel_box(
            center_u=(sx * wall_mid_x, seal_cy, 0.0),
            size_u=(strip_t, seal_h, wall_mid_z * 2.0 - strip_t - 0.001),
            mat="R1_LampAmber",
            bevel=0.0015,
            segments=1,
        )
    bmesh.ops.rotate(mb_seal.bm, verts=list(mb_seal.bm.verts), cent=(0.0, 0.0, 0.0), matrix=open_rot)
    ruins_common.to_ruin_object(mb_seal, "Seal", parent=lid_obj, origin_u=lid_pivot_u)

    cut_point_open_u = _rotate_Vec_around_pivot_u((0.0, hinge_y, hinge_z), lid_pivot_u, LID_OPEN_ANGLE_DEG)
    r1kit.create_empty("CutPoint", parent=lid_obj, pivot_u=cut_point_open_u)
    r1kit.create_empty("ItemSocket", parent=root_obj, pivot_u=(0.0, item_y, 0.0))

    # =========================================================================
    # Three Separate Content Child MESH Objects (`Igniter`, `MetalBundle`, `WiringSpool`)
    # =========================================================================
    # 1. `Igniter` (centre-front bay, resting on presentation cradle at y=0.205, z=0.032)
    igniter_pivot_u = (0.006, 0.205, 0.032)
    mb_igniter = r1kit.MeshBuilder("Igniter", pivot_u=igniter_pivot_u)
    build_igniter_into(mb_igniter, center_u=igniter_pivot_u)
    ruins_common.to_ruin_object(mb_igniter, "Igniter", parent=root_obj, origin_u=igniter_pivot_u)

    # 2. `MetalBundle` (left bay, 2 strapped structural plates at x=-0.150, y=0.190, z=-0.005)
    bundle_pivot_u = (-0.150, 0.190, -0.005)
    mb_bundle = r1kit.MeshBuilder("MetalBundle", pivot_u=bundle_pivot_u)
    build_metal_bundle_into(mb_bundle, center_u=bundle_pivot_u)
    ruins_common.to_ruin_object(mb_bundle, "MetalBundle", parent=root_obj, origin_u=bundle_pivot_u)

    # 3. `WiringSpool` (right bay, 1 small copper wiring spool at x=+0.158, y=0.212, z=-0.010)
    spool_pivot_u = (0.158, 0.212, -0.010)
    mb_spool = r1kit.MeshBuilder("WiringSpool", pivot_u=spool_pivot_u)
    build_wiring_spool_into(mb_spool, center_u=spool_pivot_u)
    ruins_common.to_ruin_object(mb_spool, "WiringSpool", parent=root_obj, origin_u=spool_pivot_u)

    # 4. `Decal_VIS_1` (VIS 1985 mark on the flat front panel corner)
    dx, dy = spec["decal_center_xy"]
    d_size = float(spec["decal_size"])
    r1kit.add_maker_decal_node(
        parent=root_obj,
        name="Decal_VIS_1",
        cell_key="VIS_1985",
        center_u=(dx, dy, shell_d * 0.5),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=d_size,
        height=d_size,
    )

    return root_obj


def export_open_crate(
    out_dir: Path = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Crates",
) -> Path:
    """Build, UV-unwrap, bake vertex colours, and export `Crate_M_Open.fbx`."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    ruins_common.reset_ruins_scene()
    root = build_open_crate()
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    ruins_common.apply_signage_uvs(root)
    ruins_common.bake_ruins_vertex_colors(root, footprint_radius=0.55)

    fbx_path = out_dir / "Crate_M_Open.fbx"
    r1kit.export_fbx(root, fbx_path)
    print(f"[open_crate] Exported {fbx_path}")
    return fbx_path


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export R1 Crate_M_Open.fbx.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Crates",
    )
    args = parser.parse_args(argv)
    export_open_crate(out_dir=args.out_dir)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
