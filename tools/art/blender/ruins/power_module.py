"""Procedural Blender builder for the standalone towable R1 `PowerModule` model.

Opening v3 slice 3.2, beat R6: Rover 07 tows the Lumen habitat's 1980s VIS-built
power/battery module home with its winch, where it becomes the `BatteryBank`
structure's frame.

Features & contract:
- Roughly the `BatteryBank` structure's footprint (~1.46 m wide x ~0.98 m deep,
  ~0.88 m tall low-slung heavy battery sled).
- Worn 1980s Lumen VIS cream/coral livery (`R1_CreamEnamel`, `R1_OrangeStripe`,
  `R1_GreyMetal`, `R1_BareSteel`, `R1_SagePatch`, `R1_Rubber`, `R1_Brass`,
  `R1_Copper`, `R1_DarkGlass`, `R1_LampAmber`, `R1_Signage`).
- Clearly "an electrical box you could drag":
  * Severed/unplugged dark rubber umbilical cable stubs (`R1_Rubber` + `R1_Copper`
    + `R1_Brass` strain-relief glands) and recessed DC busbar terminal bay.
  * `Lid`: sealed top vault lid with `R1_Rubber` perimeter compression gasket,
    `R1_BareSteel`/`R1_Brass` over-centre toggle latches, and lifting lugs.
  * Louvered cooling vent grilles (`add_vent_grille`) on front and rear bulkheads.
  * `Decal_VIS_1`: VIS 1985 maker decal wafer on the front upper-left panel.
  * `PowerModule_StatusLamp`: separate mesh renderer node on the front bulkhead
    with a ~7.6 cm `R1_Brass` bezel and ~6.8 cm `R1_LampAmber` domed lens.
  * `TowHitch`: sturdy forged steel towing eye/ring on the front face (`+Z` in
    Unity / `+Y` in Blender) with its pivot at the exact eye centre.
  * `Skid`: low underside sled node with two upturned steel runners (`y = 0.0`)
    so it reads as able to slide on regolith.

Usage (headless):
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/ruins/power_module.py -- --skip-renders
"""

from __future__ import annotations

import argparse
from pathlib import Path
import sys
from typing import Any

BLENDER_DIR = Path(__file__).resolve().parents[1]
RUINS_DIR = Path(__file__).resolve().parent
for _p in (BLENDER_DIR, RUINS_DIR):
    if str(_p) not in sys.path:
        sys.path.insert(0, str(_p))

import r1kit  # noqa: E402
import ruins_common  # noqa: E402

# Pivot coordinates in Unity space (X_right, Y_up, Z_fwd)
ROOT_PIVOT_U = (0.0, 0.0, 0.0)
SKID_PIVOT_U = (0.0, 0.0, 0.0)
LID_PIVOT_U = (0.0, 0.72, 0.0)
TOW_HITCH_PIVOT_U = (0.0, 0.21, 0.475)
STATUS_LAMP_PIVOT_U = (0.49, 0.56, 0.394)


def _build_skid_node(root: Any) -> Any:
    """Build the low underside `Skid` node with two upturned runners on `y = 0.0`."""
    mb = r1kit.MeshBuilder("Skid", pivot_u=SKID_PIVOT_U)

    runner_w = 0.13
    runner_z_span = 0.76
    shoe_h = 0.022
    beam_h = 0.068

    for sx in (-1.0, 1.0):
        rx = sx * 0.52

        # Flat regolith wear shoe sitting flush on Y = 0.0
        mb.add_bevel_box(
            center_u=(rx, shoe_h * 0.5, 0.0),
            size_u=(runner_w, shoe_h, runner_z_span),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
        )

        # Main longitudinal runner channel beam above the wear shoe
        mb.add_bevel_box(
            center_u=(rx, shoe_h + beam_h * 0.5 - 0.002, 0.0),
            size_u=(runner_w * 0.90, beam_h, runner_z_span * 0.96),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )

        # Upturned front (+Z) and rear (-Z) sled prows so the runners glide over regolith
        for sz, tilt_deg in ((1.0, -26.0), (-1.0, 26.0)):
            mb.add_bevel_box(
                center_u=(rx, 0.044, sz * 0.402),
                size_u=(runner_w * 0.94, 0.036, 0.115),
                mat="R1_BareSteel",
                bevel=0.005,
                segments=1,
                rot_deg_u=(tilt_deg, 0.0, 0.0),
            )
            mb.add_bevel_box(
                center_u=(rx, 0.066, sz * 0.388),
                size_u=(runner_w * 0.84, 0.042, 0.095),
                mat="R1_GreyMetal",
                bevel=0.005,
                segments=1,
                rot_deg_u=(tilt_deg * 0.8, 0.0, 0.0),
            )

        # 3 vertical riser stanchions per runner bolting into the lower chassis tub
        for rz in (-0.28, 0.0, 0.28):
            mb.add_bevel_box(
                center_u=(rx, 0.088, rz),
                size_u=(runner_w * 0.82, 0.036, 0.085),
                mat="R1_GreyMetal",
                bevel=0.004,
                segments=1,
            )
            mb.add_bolts(
                positions_u=[
                    (rx + sx * (runner_w * 0.42), 0.056, rz - 0.024),
                    (rx + sx * (runner_w * 0.42), 0.056, rz + 0.024),
                ],
                radius=0.009,
                height=0.008,
                axis_u="X",
                mat="R1_BareSteel",
            )

    # 3 heavy transverse crossmember spreader bars tying the left & right runners together
    for cz in (-0.28, 0.0, 0.28):
        mb.add_bevel_box(
            center_u=(0.0, 0.062, cz),
            size_u=(1.08, 0.044, 0.072),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )

    return ruins_common.to_ruin_object(mb, "Skid", parent=root, origin_u=SKID_PIVOT_U)


def _build_tow_hitch_node(root: Any) -> Any:
    """Build the front `TowHitch` node on +Z with its pivot at the exact eye centre."""
    px, py, pz = TOW_HITCH_PIVOT_U
    mb = r1kit.MeshBuilder("TowHitch", pivot_u=TOW_HITCH_PIVOT_U)

    # 1. Sturdy forged steel horizontal towing eye/ring centered at exact TOW_HITCH_PIVOT_U
    # Major radius 0.048 m, minor radius 0.015 m -> 6.6 cm clear inner hook aperture,
    # outer edge at z = 0.475 + 0.063 = 0.538 m.
    mb.add_torus(
        center_u=(px, py, pz),
        major_radius=0.048,
        minor_radius=0.015,
        mat="R1_BareSteel",
        axis_u="Y",
        major_segments=24,
        minor_segments=12,
    )

    # 2. Forged drawbar neck behind the eye ring (z = 0.402..0.426 m, leaving the eye hole open)
    mb.add_bevel_box(
        center_u=(px, py, 0.415),
        size_u=(0.054, 0.032, 0.032),
        mat="R1_BareSteel",
        bevel=0.005,
        segments=1,
    )

    # 3. Heavy clevis bracket & upper/lower jaw plates bolted to the front chassis face
    mb.add_bevel_box(
        center_u=(px, py, 0.398),
        size_u=(0.18, 0.096, 0.022),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    for sy in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(px, py + sy * 0.027, 0.412),
            size_u=(0.092, 0.018, 0.036),
            mat="R1_GreyMetal",
            bevel=0.004,
            segments=1,
        )
    # Vertical clevis kingpin + brass locking collar
    mb.add_cylinder(
        center_u=(px, py, 0.414),
        radius=0.014,
        length=0.082,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=12,
        bevel=0.003,
        bevel_segments=1,
    )
    mb.add_cylinder(
        center_u=(px, py + 0.042, 0.414),
        radius=0.019,
        length=0.012,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.002,
        bevel_segments=1,
    )

    # Orange tow-point callout chevrons flanking the clevis bracket
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(px + sx * 0.068, py, 0.402),
            size_u=(0.026, 0.076, 0.018),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )

    return ruins_common.to_ruin_object(mb, "TowHitch", parent=root, origin_u=TOW_HITCH_PIVOT_U)


def _build_lid_node(root: Any) -> Any:
    """Build the sealed top `Lid` node (`R1_Rubber` gasket, `R1_CreamEnamel` cap, latches & lugs)."""
    mb = r1kit.MeshBuilder("Lid", pivot_u=LID_PIVOT_U)

    # 1. Continuous dark rubber perimeter compression seal gasket at the lid seam (y = 0.712..0.734)
    mb.add_bevel_box(
        center_u=(0.0, 0.723, 0.0),
        size_u=(1.33, 0.022, 0.79),
        mat="R1_Rubber",
        bevel=0.005,
        segments=1,
    )

    # 2. Main chamfered cream-enamel insulated lid shell (y = 0.732..0.822)
    mb.add_bevel_box(
        center_u=(0.0, 0.777, 0.0),
        size_u=(1.31, 0.090, 0.77),
        mat="R1_CreamEnamel",
        bevel=0.016,
        segments=2,
        top_scale_xz=(0.96, 0.94),
    )

    # 3. 4 heavy grey-metal corner bumper caps on the lid
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.625, 0.778, sz * 0.358),
                size_u=(0.088, 0.094, 0.088),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )

    # 4. Coral/orange VIS livery longitudinal crown stripe across the lid
    mb.add_bevel_box(
        center_u=(0.0, 0.784, 0.0),
        size_u=(1.22, 0.078, 0.16),
        mat="R1_OrangeStripe",
        bevel=0.006,
        segments=1,
    )

    # 5. 4 transverse grey-metal structural stiffening ribs across the lid top (y = 0.818..0.852)
    for rx in (-0.46, -0.16, 0.16, 0.46):
        mb.add_bevel_box(
            center_u=(rx, 0.832, 0.0),
            size_u=(0.052, 0.036, 0.72),
            mat="R1_GreyMetal",
            bevel=0.005,
            segments=1,
        )

    # 6. 4 corner crane/winch lifting lugs reaching y = 0.880 m
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.46, 0.856, sz * 0.28),
                size_u=(0.038, 0.048, 0.068),
                mat="R1_BareSteel",
                bevel=0.005,
                segments=1,
            )
            mb.add_cylinder(
                center_u=(sx * 0.46, 0.864, sz * 0.28),
                radius=0.012,
                length=0.046,
                mat="R1_Brass",
                axis_u="X",
                segments=10,
                bevel=0.002,
                bevel_segments=1,
            )

    # 7. Over-centre toggle latches clamping the sealed lid across the rubber gasket seam
    # Front (+Z) and rear (-Z) latches
    for sz in (-1.0, 1.0):
        for lx in (-0.36, 0.36):
            mb.add_bevel_box(
                center_u=(lx, 0.732, sz * 0.394),
                size_u=(0.048, 0.068, 0.022),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
            )
            mb.add_bevel_box(
                center_u=(lx, 0.708, sz * 0.399),
                size_u=(0.034, 0.026, 0.016),
                mat="R1_Brass",
                bevel=0.003,
                segments=1,
            )
    # Left (-X) and right (+X) side latches
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.664, 0.732, 0.0),
            size_u=(0.022, 0.068, 0.052),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
        )
        mb.add_bevel_box(
            center_u=(sx * 0.669, 0.708, 0.0),
            size_u=(0.016, 0.026, 0.036),
            mat="R1_Brass",
            bevel=0.003,
            segments=1,
        )

    # 8. Top stencil warning plate ("HAZARD") on the center of the lid
    ruins_common.add_signage_plate(
        mb,
        center_u=(0.0, 0.826, 0.20),
        width=0.28,
        height=0.11,
        thickness=0.012,
        rot_deg_u=(-90.0, 0.0, 0.0),
        cell_name="HAZARD",
    )

    return ruins_common.to_ruin_object(mb, "Lid", parent=root, origin_u=LID_PIVOT_U)


def _build_status_lamp_node(root: Any) -> Any:
    """Build the separate `PowerModule_StatusLamp` child mesh node (~7.6 cm brass bezel + ~6.8 cm amber lens)."""
    lx, ly, lz = STATUS_LAMP_PIVOT_U
    mb = r1kit.MeshBuilder("PowerModule_StatusLamp", pivot_u=STATUS_LAMP_PIVOT_U)

    # Machined brass bezel collar (7.6 cm outer diameter = 0.038 m radius)
    mb.add_cylinder(
        center_u=(lx, ly, lz + 0.007),
        radius=0.038,
        length=0.018,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.003,
        bevel_segments=1,
    )
    # Domed amber lens (6.8 cm diameter = 0.034 m radius) bulging forward (+Z) in front of the node pivot
    mb.add_cylinder(
        center_u=(lx, ly, lz + 0.015),
        radius=0.034,
        length=0.010,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel=0.004,
        bevel_segments=2,
    )
    mb.add_cylinder(
        center_u=(lx, ly, lz + 0.021),
        radius=0.030,
        radius_top=0.018,
        length=0.006,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel=0.002,
        bevel_segments=1,
    )

    return ruins_common.to_ruin_object(mb, "PowerModule_StatusLamp", parent=root, origin_u=STATUS_LAMP_PIVOT_U)


def _add_cable_stubs(mb: r1kit.MeshBuilder) -> None:
    """Add heavy severed/unplugged rubber umbilical cable stubs with brass glands & copper cores."""
    # 1. Left (-X) lower bulkhead dual umbilical manifold (matching HabitatModules' twin power umbilicals)
    mb.add_bevel_box(
        center_u=(-0.658, 0.30, 0.0),
        size_u=(0.032, 0.24, 0.42),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    mb.add_bolts(
        positions_u=[
            (-0.675, 0.39, -0.17),
            (-0.675, 0.39, 0.17),
            (-0.675, 0.21, -0.17),
            (-0.675, 0.21, 0.17),
        ],
        radius=0.009,
        height=0.008,
        axis_u="X",
        mat="R1_BareSteel",
    )

    # Two severed left-side umbilical stubs curving down and rearward
    left_stubs = [
        (
            0.024,
            [
                (-0.665, 0.31, 0.11),
                (-0.702, 0.28, 0.09),
                (-0.715, 0.20, 0.02),
                (-0.706, 0.11, -0.09),
            ],
        ),
        (
            0.022,
            [
                (-0.665, 0.27, -0.11),
                (-0.700, 0.24, -0.14),
                (-0.712, 0.16, -0.22),
                (-0.702, 0.09, -0.32),
            ],
        ),
    ]
    for rad, pts in left_stubs:
        p0 = pts[0]
        p_end = pts[-1]
        # Strain-relief brass & steel bulkhead gland
        mb.add_cylinder(
            center_u=(p0[0] - 0.012, p0[1], p0[2]),
            radius=rad * 1.48,
            length=0.032,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.003,
            bevel_segments=1,
        )
        mb.add_cylinder(
            center_u=(p0[0] - 0.026, p0[1], p0[2]),
            radius=rad * 1.25,
            length=0.020,
            mat="R1_BareSteel",
            axis_u="X",
            segments=10,
            bevel=0.002,
            bevel_segments=1,
        )
        # Curved rubber umbilical stub
        mb.add_tube_path(
            points_u=pts,
            radius=rad,
            mat="R1_Rubber",
            segments=12,
            cap_ends=True,
        )
        # Exposed copper conductor core stubs at the severed end
        for ox, oy in ((-0.006, 0.005), (0.006, 0.004), (0.0, -0.006)):
            mb.add_cylinder(
                center_u=(p_end[0] + ox, p_end[1] - 0.012 + oy, p_end[2] - 0.014),
                radius=0.0055,
                length=0.028,
                mat="R1_Copper",
                axis_u="Z",
                segments=8,
                bevel=0.001,
                bevel_segments=1,
                rot_deg_u=(32.0, 0.0, 0.0),
            )

    # 2. Right (+X) lower bulkhead auxiliary sense/ground cable stub
    r_pts = [
        (0.665, 0.25, -0.14),
        (0.700, 0.22, -0.17),
        (0.712, 0.14, -0.25),
    ]
    mb.add_cylinder(
        center_u=(0.674, 0.25, -0.14),
        radius=0.032,
        length=0.028,
        mat="R1_Brass",
        axis_u="X",
        segments=12,
        bevel=0.003,
        bevel_segments=1,
    )
    mb.add_tube_path(
        points_u=r_pts,
        radius=0.020,
        mat="R1_Rubber",
        segments=10,
        cap_ends=True,
    )
    mb.add_cylinder(
        center_u=(0.712, 0.125, -0.264),
        radius=0.008,
        length=0.026,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0015,
        bevel_segments=1,
        rot_deg_u=(35.0, 0.0, 0.0),
    )

    # 3. Rear (-Z) high-current feeder socket & severed cable stub
    mb.add_bevel_box(
        center_u=(0.28, 0.25, -0.398),
        size_u=(0.20, 0.14, 0.028),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.28, 0.25, -0.416),
        radius=0.034,
        length=0.026,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel=0.003,
        bevel_segments=1,
    )
    mb.add_tube_path(
        points_u=[
            (0.28, 0.25, -0.422),
            (0.29, 0.21, -0.442),
            (0.32, 0.14, -0.446),
        ],
        radius=0.022,
        mat="R1_Rubber",
        segments=10,
        cap_ends=True,
    )
    mb.add_cylinder(
        center_u=(0.325, 0.122, -0.448),
        radius=0.009,
        length=0.024,
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0015,
        bevel_segments=1,
    )


def build_power_module() -> Any:
    """Construct the complete `PowerModule` hierarchy and return the root Blender object."""
    mb = r1kit.MeshBuilder("PowerModule", pivot_u=ROOT_PIVOT_U)

    # 1. Lower structural chassis cradle (y = 0.09..0.24 m) sitting on top of `Skid`
    mb.add_bevel_box(
        center_u=(0.0, 0.165, 0.0),
        size_u=(1.33, 0.15, 0.79),
        mat="R1_GreyMetal",
        bevel=0.012,
        segments=2,
    )

    # 2. Main 1980s Lumen VIS cream-enamel insulated battery vault shell (y = 0.23..0.72 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.475, 0.0),
        size_u=(1.31, 0.49, 0.77),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=2,
    )

    # 3. 4 full-height heavy corner bumper posts (`R1_GreyMetal`)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                center_u=(sx * 0.625, 0.405, sz * 0.358),
                size_u=(0.086, 0.62, 0.086),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )

    # 4. Warm coral / international-orange VIS livery horizontal waist stripe (`R1_OrangeStripe`)
    band_y = 0.375
    band_h = 0.088
    for sz in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(0.0, band_y, sz * 0.388),
            size_u=(1.18, band_h, 0.012),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )
    for sx in (-1.0, 1.0):
        mb.add_bevel_box(
            center_u=(sx * 0.658, band_y, 0.0),
            size_u=(0.012, band_h, 0.64),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )

    # 5. Front (+Z) bulkhead details:
    # - Center-left louvered cooling vent grille (`R1_GreyMetal` + `R1_DarkGlass`)
    mb.add_vent_grille(
        center_u=(-0.02, 0.565, 0.388),
        width=0.36,
        height=0.19,
        depth=0.026,
        slat_count=5,
        rot_deg_u=(0.0, 0.0, 0.0),
        frame_mat="R1_GreyMetal",
        back_mat="R1_DarkGlass",
    )

    # - Upper-right recessed charge monitoring bezel + mounting flange for `PowerModule_StatusLamp`
    mb.add_bevel_box(
        center_u=(0.41, 0.56, 0.388),
        size_u=(0.34, 0.19, 0.020),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    # Charge-state sight window beside the status lamp
    mb.add_bevel_box(
        center_u=(0.32, 0.56, 0.396),
        size_u=(0.11, 0.11, 0.012),
        mat="R1_Brass",
        bevel=0.003,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.32, 0.56, 0.400),
        size_u=(0.084, 0.084, 0.008),
        mat="R1_DarkGlass",
        bevel=0.002,
        segments=1,
    )

    # - Lower-center reinforced tow-hitch bolster plate on front (+Z) chassis beam
    mb.add_bevel_box(
        center_u=(0.0, 0.21, 0.394),
        size_u=(0.34, 0.13, 0.020),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    mb.add_bolts(
        positions_u=[
            (-0.14, 0.25, 0.405),
            (0.14, 0.25, 0.405),
            (-0.14, 0.17, 0.405),
            (0.14, 0.17, 0.405),
        ],
        radius=0.010,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
    )

    # - Front lower-left "LUMEN" stencil nameplate
    ruins_common.add_signage_plate(
        mb,
        center_u=(-0.38, 0.22, 0.398),
        width=0.28,
        height=0.086,
        thickness=0.012,
        rot_deg_u=(0.0, 0.0, 0.0),
        cell_name="LUMEN",
    )

    # 6. Rear (-Z) bulkhead details:
    # - Second louvered battery exhaust vent grille
    mb.add_vent_grille(
        center_u=(-0.26, 0.555, -0.388),
        width=0.42,
        height=0.21,
        depth=0.026,
        slat_count=5,
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat="R1_GreyMetal",
        back_mat="R1_DarkGlass",
    )
    # - Sage-green field-service replacement cover plate (`R1_SagePatch`) + hex bolts
    mb.add_bevel_box(
        center_u=(0.28, 0.555, -0.390),
        size_u=(0.38, 0.21, 0.018),
        mat="R1_SagePatch",
        bevel=0.004,
        segments=1,
    )
    mb.add_bolts(
        positions_u=[
            (0.12, 0.63, -0.400),
            (0.44, 0.63, -0.400),
            (0.12, 0.48, -0.400),
            (0.44, 0.48, -0.400),
        ],
        radius=0.010,
        height=0.008,
        axis_u="Z",
        mat="R1_BareSteel",
    )
    # - Two heavy rear lid hinges along the top-rear seam
    for hx in (-0.38, 0.38):
        mb.add_hinge(
            center_u=(hx, 0.715, -0.394),
            axis_u="X",
            length=0.16,
            knuckle_radius=0.018,
            leaf_width=0.056,
            leaf_thickness=0.014,
            mat_body="R1_GreyMetal",
            mat_pin="R1_BareSteel",
        )

    # 7. Right (+X) side recessed DC busbar terminal bay & sage inspection plate
    mb.add_bevel_box(
        center_u=(0.658, 0.54, -0.10),
        size_u=(0.026, 0.20, 0.28),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    for bz in (-0.15, -0.05):
        mb.add_bevel_box(
            center_u=(0.669, 0.54, bz),
            size_u=(0.014, 0.13, 0.036),
            mat="R1_Copper",
            bevel=0.003,
            segments=1,
        )
        mb.add_cylinder(
            center_u=(0.678, 0.57, bz),
            radius=0.012,
            length=0.016,
            mat="R1_Brass",
            axis_u="X",
            segments=10,
            bevel=0.002,
            bevel_segments=1,
        )
    mb.add_bevel_box(
        center_u=(0.660, 0.54, 0.17),
        size_u=(0.018, 0.20, 0.20),
        mat="R1_SagePatch",
        bevel=0.004,
        segments=1,
    )

    # 8. Severed/unplugged umbilical cable stubs on left (-X), right (+X), and rear (-Z)
    _add_cable_stubs(mb)

    root = ruins_common.to_ruin_object(mb, "PowerModule", parent=None, origin_u=ROOT_PIVOT_U)

    # Child nodes
    _build_skid_node(root)
    _build_tow_hitch_node(root)
    _build_lid_node(root)
    _build_status_lamp_node(root)

    # VIS 1985 maker decal on the flat front (+Z) upper-left cream enamel panel
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_VIS_1",
        cell_key="VIS_1985",
        center_u=(-0.38, 0.565, 0.385),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.18,
        height=0.18,
    )

    return root


def export_and_render(
    fbx_path: Path = Path("Assets/_Project/Art/R1/Models/Ruins/PowerModule.fbx"),
    skip_renders: bool = False,
) -> Path:
    """Build, UV-unwrap, bake vertex colours, export FBX, and optionally render preview images."""
    ruins_common.reset_ruins_scene()
    root = build_power_module()

    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    ruins_common.apply_signage_uvs(root)
    ruins_common.bake_ruins_vertex_colors(root, footprint_radius=1.25)

    out_path = Path(fbx_path)
    r1kit.export_fbx(root, out_path)
    if not skip_renders:
        ruins_common.render_ruin_day_night(
            root,
            slug="power_module",
            cam_angle_deg=142.0,
            cam_elev_deg=24.0,
        )
    return out_path


def build(skip_renders: bool = False) -> Path:
    """Standard ruins entry point used by `tools/art/blender/build_all.py`."""
    return export_and_render(skip_renders=skip_renders)


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export the R1 PowerModule FBX.")
    parser.add_argument(
        "--out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Ruins/PowerModule.fbx"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)
    out = export_and_render(fbx_path=args.out, skip_renders=args.skip_renders)
    print(f"[power_module] Exported {out}")


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
