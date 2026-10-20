"""Build script for the R1 AntennaMast structure (`Assets/_Project/Art/R1/Models/Structures/AntennaMast.fbx`)."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

# Single table of dimensions taken from DESIGN.md (§1.2, §2, §3.9) and s_antenna_mast.png
ANTENNA_MAST_DIMS: dict[str, float] = {
    "footprint_width": 1.40,
    "footprint_depth": 1.40,
    "total_height": 2.80,
    "cabinet_height": 1.55,
    # Splayed quadruped leg base & cabinet body
    "leg_foot_xy": 0.54,
    "leg_hip_xy": 0.36,
    "collar_bot_y": 0.23,
    "collar_top_y": 0.31,
    "cab_width": 0.90,
    "cab_depth": 0.86,
    "cab_low_top_y": 1.22,
    "cab_brow_top_y": 1.51,
    # Beltline orange stripe (lower cabinet, matching s_antenna_mast.png) & viewer-right (-X) sage salvage patch
    "stripe_center_y": 0.44,
    "stripe_height": 0.11,
    "patch_center_y": 0.78,
    "patch_width_z": 0.36,
    "patch_height_y": 0.38,
    "patch_thick_x": 0.022,
    # Front reel-to-reel window & tape reels
    "window_center_y": 0.78,
    "window_width_x": 0.62,
    "window_height_y": 0.40,
    "reel_x": 0.16,
    "reel_y": 0.78,
    "reel_z": 0.45,
    "reel_radius": 0.115,
    # Upper sloped brow azimuth dial
    "dial_y": 1.38,
    "dial_z": 0.43,
    "dial_tilt_deg": 20.0,
    # Lattice mast & outrigger arms (3 raisable segments under Mast empty)
    "mast_base_y": 1.55,
    "mast_seg1_top_y": 1.93,
    "mast_seg2_top_y": 2.30,
    "mast_top_y": 2.68,
    "mast_overlap": 0.05,
    "mast_base_half_w": 0.145,
    "mast_top_half_w": 0.072,
    # Parabolic wire-mesh dish (Ø 0.95 m) on viewer-left (+X) outrigger
    "dish_az_x": 0.44,
    "dish_az_y": 2.12,
    "dish_az_z": 0.08,
    "dish_piv_y": 2.22,
    "dish_diameter": 0.95,
    "dish_depth": 0.18,
    # Crossed-loop brass direction-finding antenna on viewer-right (-X) outrigger
    "sonar_x": -0.38,
    "sonar_y": 2.28,
    "sonar_z": 0.0,
    "sonar_loop_len": 0.24,
    # Top masthead omni-beacon
    "beacon_y": 2.72,
    # Power socket on lower viewer-right (-X) cabinet beltline
    "socket_power_x": -0.49,
    "socket_power_y": 0.44,
    "socket_power_z": 0.0,
}


def _build_stage_1(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 1: 4 splayed quadruped landing legs with circular footpads, under-cabinet chassis frame, and Socket_Power_1."""
    d = ANTENNA_MAST_DIMS
    mb = r1kit.MeshBuilder("Stage_1", pivot_u=(0.0, 0.0, 0.0))

    foot_xy = d["leg_foot_xy"]
    hip_xy = d["leg_hip_xy"]
    y_bot = d["collar_bot_y"]
    y_top = d["collar_top_y"]
    cw = d["cab_width"]
    cd = d["cab_depth"]

    # Under-cabinet dark-charcoal chassis frame (y = 0.23 .. 0.31 m)
    mb.add_bevel_box(
        center_u=(0.0, 0.5 * (y_bot + y_top), 0.0),
        size_u=(cw - 0.04, y_top - y_bot, cd - 0.04),
        mat="R1_GreyMetal",
        bevel=0.016,
        segments=1,
    )

    # 4 splayed quadruped landing legs with circular ground footpads, diagonal struts, and corner hip brackets (matching s_antenna_mast.png)
    bolts: list[tuple[float, float, float]] = []
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            fx = sx * foot_xy
            fz = sz * foot_xy
            hx = sx * hip_xy
            hz = sz * hip_xy

            # Circular ground footpad disc
            mb.add_cylinder(
                center_u=(fx, 0.018, fz),
                radius=0.095,
                radius_top=0.072,
                length=0.036,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=10,
                bevel=0.005,
            )
            bolts.append((fx + sx * 0.045, 0.038, fz + sz * 0.045))

            # Main splayed leg tube + inner diagonal tie strut
            mb.add_pipe(
                p0_u=(fx, 0.032, fz),
                p1_u=(hx, y_bot + 0.03, hz),
                radius=0.028,
                mat="R1_GreyMetal",
                segments=8,
            )
            mb.add_strut(
                p0_u=(fx * 0.86, 0.08, fz * 0.86),
                p1_u=(hx * 0.65, y_bot + 0.01, hz * 0.65),
                width=0.024,
                thickness=0.024,
                mat="R1_BareSteel",
                bevel=0.0,
            )

            # Corner hip clevis block under the cabinet corner
            mb.add_bevel_box(
                center_u=(hx, y_bot + 0.025, hz),
                size_u=(0.14, 0.09, 0.14),
                mat="R1_GreyMetal",
                bevel=0.008,
                segments=1,
            )

    mb.add_bolts(bolts, radius=0.013, height=0.010, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    # Lower viewer-right (-X) heavy cylindrical power cable gland receptacle on the beltline (matching s_antenna_mast.png)
    px = d["socket_power_x"]
    py = d["socket_power_y"]
    pz = d["socket_power_z"]
    mb.add_cylinder(
        center_u=(px + 0.032, py, pz),
        radius=0.058,
        length=0.048,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=10,
        bevel=0.005,
    )
    mb.add_cylinder(
        center_u=(px + 0.008, py, pz),
        radius=0.038,
        length=0.036,
        mat="R1_Copper",
        axis_u="X",
        segments=10,
        bevel=0.004,
    )

    stage_1_obj = mb.build_object(parent=root)
    r1kit.create_empty(
        name="Socket_Power_1",
        parent=stage_1_obj,
        pivot_u=(px, py, pz),
        outward_dir_u=(-1.0, 0.0, 0.0),
    )
    return stage_1_obj


def _build_stage_2(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 2: Recessed front reel-to-reel bay frame & window, sloped dial bezel, rear vent, and coax conduits."""
    d = ANTENNA_MAST_DIMS
    mb = r1kit.MeshBuilder("Stage_2", pivot_u=(0.0, 0.0, 0.0))

    front_z = d["cab_depth"] * 0.5  # 0.43 m
    wy = d["window_center_y"]
    ww = d["window_width_x"]
    wh = d["window_height_y"]

    # 1. Front (+Z) recessed reel-to-reel telemetry tape window surround & dark-glass backing
    mb.add_bevel_box(
        center_u=(0.0, wy, front_z + 0.008),
        size_u=(ww, wh, 0.034),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_bevel_box(
        center_u=(0.0, wy, front_z + 0.012),
        size_u=(ww - 0.056, wh - 0.056, 0.022),
        mat="R1_DarkGlass",
        bevel=0.0,
        segments=0,
    )
    # Tape guide rollers and central magnetic read/write head block inside the lower window
    for rx_guide in (-0.21, -0.08, 0.08, 0.21):
        mb.add_cylinder(
            center_u=(rx_guide, wy - 0.12, front_z + 0.026),
            radius=0.015,
            length=0.020,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.0,
            bevel_segments=0,
        )
    mb.add_bevel_box(
        center_u=(0.0, wy - 0.118, front_z + 0.026),
        size_u=(0.082, 0.044, 0.022),
        mat="R1_BareSteel",
        bevel=0.004,
        segments=1,
    )
    # Lower horizontal tape-status meter strip inside the bottom of the reel frame (matching s_antenna_mast.png)
    mb.add_bevel_box(
        center_u=(0.0, wy - 0.165, front_z + 0.024),
        size_u=(ww - 0.10, 0.032, 0.016),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )

    # 2. Upper sloped brow azimuth dial housing & brass bezel ring at (0.0, 1.38, 0.43)
    dy = d["dial_y"]
    dz = d["dial_z"]
    dtilt = d["dial_tilt_deg"]
    mb.add_cylinder(
        center_u=(0.0, dy, dz - 0.016),
        radius=0.118,
        length=0.038,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=12,
        bevel=0.006,
        rot_deg_u=(dtilt, 0.0, 0.0),
    )
    mb.add_cylinder(
        center_u=(0.0, dy, dz - 0.004),
        radius=0.098,
        length=0.022,
        mat="R1_Copper",
        axis_u="Z",
        segments=12,
        bevel=0.004,
        rot_deg_u=(dtilt, 0.0, 0.0),
    )

    # 3. External copper & grey-metal coaxial conduit pipes running up the viewer-left (+X) cabinet flank
    side_x = d["cab_width"] * 0.5  # 0.45 m
    for z_pipe, mat_p in ((-0.12, "R1_GreyMetal"), (0.04, "R1_Copper")):
        mb.add_tube_path(
            [
                (side_x - 0.02, 0.36, z_pipe),
                (side_x + 0.030, 0.42, z_pipe),
                (side_x + 0.030, 1.18, z_pipe),
                (side_x - 0.04, 1.28, z_pipe),
            ],
            radius=0.017,
            mat=mat_p,
            segments=6,
            cap_ends=True,
        )
    for cy_clamp in (0.58, 0.96):
        mb.add_bevel_box(
            center_u=(side_x + 0.020, cy_clamp, -0.04),
            size_u=(0.036, 0.032, 0.24),
            mat="R1_GreyMetal",
            bevel=0.0,
            segments=0,
        )

    # 4. Rear (-Z) ventilation louver grille
    mb.add_vent_grille(
        center_u=(0.0, 0.78, -front_z - 0.006),
        width=0.40,
        height=0.34,
        depth=0.024,
        slat_count=3,
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat="R1_GreyMetal",
        back_mat="R1_DarkGlass",
    )

    # 5. Heavy roof mast base turret flange ring at y = 1.51 .. 1.56 m with perimeter hex bolts
    my = d["mast_base_y"]
    mb.add_bevel_box(
        center_u=(0.0, my - 0.02, 0.0),
        size_u=(0.42, 0.048, 0.42),
        mat="R1_GreyMetal",
        bevel=0.010,
        segments=1,
    )
    mb.add_cylinder(
        center_u=(0.0, my + 0.01, 0.0),
        radius=0.18,
        length=0.040,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.005,
    )
    roof_bolts = [
        (sx * 0.16, my + 0.010, sz * 0.16)
        for sx in (-1.0, 1.0)
        for sz in (-1.0, 1.0)
    ]
    mb.add_bolts(roof_bolts, radius=0.012, height=0.010, axis_u="Y", mat="R1_BareSteel", bevel_segments=0)

    return mb.build_object(parent=root)


def _build_stage_3(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 3: Rounded cream-enamel telemetry cabinet shell, sloped upper brow, orange beltline stripe, and right-side sage salvage panel."""
    d = ANTENNA_MAST_DIMS
    mb = r1kit.MeshBuilder("Stage_3", pivot_u=(0.0, 0.0, 0.0))

    y0 = d["collar_top_y"]     # 0.31 m
    y1 = d["cab_low_top_y"]    # 1.22 m
    y2 = d["cab_brow_top_y"]   # 1.51 m
    cw = d["cab_width"]
    cd = d["cab_depth"]

    # 1. Lower main cream-enamel cabinet body (y = 0.31 .. 1.22 m)
    low_h = y1 - y0
    mb.add_bevel_box(
        center_u=(0.0, y0 + low_h * 0.5, 0.0),
        size_u=(cw, low_h, cd),
        mat="R1_CreamEnamel",
        bevel=0.036,
        segments=2,
    )

    # 2. Upper sloped/tapered cream-enamel brow crown (y = 1.22 .. 1.51 m)
    brow_h = y2 - y1
    mb.add_bevel_box(
        center_u=(0.0, y1 + brow_h * 0.5, -0.015),
        size_u=(cw, brow_h, cd - 0.03),
        mat="R1_CreamEnamel",
        bevel=0.030,
        segments=2,
        top_scale_xz=(0.82, 0.78),
    )

    # 3. Rounded roof cap (y = 1.49 .. 1.55 m)
    mb.add_bevel_box(
        center_u=(0.0, 1.52, -0.015),
        size_u=(cw * 0.74, 0.06, cd * 0.70),
        mat="R1_CreamEnamel",
        bevel=0.018,
        segments=1,
        top_scale_xz=(0.90, 0.90),
    )

    # 4. Horizontal seam trim band at y = 1.22 m + lower corner bumper plates with hex rivets (matching s_antenna_mast.png)
    mb.add_bevel_box(
        center_u=(0.0, y1, 0.0),
        size_u=(cw + 0.012, 0.014, cd + 0.012),
        mat="R1_GreyMetal",
        bevel=0.0,
        segments=0,
    )
    seam_bolts: list[tuple[float, float, float]] = []
    for sx in (-cw * 0.38, cw * 0.38):
        for by_off in (-0.04, 0.04):
            seam_bolts.append((sx, y1 + by_off, cd * 0.5 + 0.004))
            seam_bolts.append((sx, y0 + 0.08 + by_off * 0.6, cd * 0.5 + 0.004))
    mb.add_bolts(seam_bolts, radius=0.011, height=0.008, axis_u="Z", mat="R1_BareSteel", bevel_segments=0)

    # 5. Continuous horizontal warm-orange beltline stripe (#FF8A5B) wrapping the lower cabinet at y = 0.44 m
    mb.add_bevel_box(
        center_u=(0.0, d["stripe_center_y"], 0.0),
        size_u=(cw + 0.012, d["stripe_height"], cd + 0.012),
        mat="R1_OrangeStripe",
        bevel=0.006,
        segments=1,
    )

    # 6. Riveted sage-green salvaged maintenance panel (R1_SagePatch) on the viewer-right (-X) cabinet face at y = 0.78 m
    rx = -cw * 0.5
    py = d["patch_center_y"]
    pw_z = d["patch_width_z"]
    ph_y = d["patch_height_y"]
    pt_x = d["patch_thick_x"]
    mb.add_bevel_box(
        center_u=(rx - pt_x * 0.4, py, 0.0),
        size_u=(pt_x, ph_y, pw_z),
        mat="R1_SagePatch",
        bevel=0.007,
        segments=1,
    )
    patch_rivets: list[tuple[float, float, float]] = []
    for ry_off in (-ph_y * 0.38, ph_y * 0.38):
        for rz_off in (-pw_z * 0.38, pw_z * 0.38):
            patch_rivets.append((rx - pt_x - 0.002, py + ry_off, rz_off))
    mb.add_bolts(patch_rivets, radius=0.012, height=0.008, axis_u="X", mat="R1_BareSteel", bevel_segments=0)

    return mb.build_object(parent=root)


def _add_mast_lattice_span(
    mb: r1kit.MeshBuilder,
    y_start: float,
    y_end: float,
    n_bays: int,
    hw_fn,
    sign_offset: int = 0,
) -> None:
    """Add 4 tapered corner legs, horizontal perimeter ties, and diagonal Warren bracing across `y_start..y_end`."""
    hw_s = hw_fn(y_start)
    hw_e = hw_fn(y_end)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_strut(
                p0_u=(sx * hw_s, y_start, sz * hw_s),
                p1_u=(sx * hw_e, y_end, sz * hw_e),
                width=0.026,
                thickness=0.026,
                mat="R1_GreyMetal",
                bevel=0.004,
            )

    for i in range(n_bays + 1):
        t = i / n_bays
        yi = y_start + t * (y_end - y_start)
        hwi = hw_fn(yi)
        mb.add_strut((-hwi, yi, -hwi), (hwi, yi, -hwi), 0.018, 0.018, "R1_GreyMetal", bevel=0.0)
        mb.add_strut((-hwi, yi, hwi), (hwi, yi, hwi), 0.018, 0.018, "R1_GreyMetal", bevel=0.0)
        mb.add_strut((-hwi, yi, -hwi), (-hwi, yi, hwi), 0.018, 0.018, "R1_GreyMetal", bevel=0.0)
        mb.add_strut((hwi, yi, -hwi), (hwi, yi, hwi), 0.018, 0.018, "R1_GreyMetal", bevel=0.0)

        if i < n_bays:
            t_next = (i + 1) / n_bays
            yn = y_start + t_next * (y_end - y_start)
            hwn = hw_fn(yn)
            sign = 1.0 if ((i + sign_offset) % 2 == 0) else -1.0
            for sz_face in (-1.0, 1.0):
                mb.add_strut(
                    (-sign * hwi, yi, sz_face * hwi),
                    (sign * hwn, yn, sz_face * hwn),
                    0.016,
                    0.016,
                    "R1_GreyMetal",
                    bevel=0.0,
                )
            for sx_face in (-1.0, 1.0):
                mb.add_strut(
                    (sx_face * hwi, yi, -sign * hwi),
                    (sx_face * hwn, yn, sign * hwn),
                    0.016,
                    0.016,
                    "R1_GreyMetal",
                    bevel=0.0,
                )


def _build_mast(stage_4: bpy.types.Object) -> tuple[bpy.types.Object, bpy.types.Object, bpy.types.Object, bpy.types.Object]:
    """Build the `Mast` empty and its 3 raisable child meshes (`Mast_Seg1`, `Mast_Seg2`, `Mast_Seg3`)."""
    d = ANTENNA_MAST_DIMS
    y0 = d["mast_base_y"]       # 1.55 m
    y_s1 = d["mast_seg1_top_y"] # 1.93 m
    y_s2 = d["mast_seg2_top_y"] # 2.30 m
    y1 = d["mast_top_y"]        # 2.68 m
    overlap = d["mast_overlap"] # 0.05 m
    hw0 = d["mast_base_half_w"] # 0.145 m
    hw1 = d["mast_top_half_w"]  # 0.072 m

    def hw_at(y: float) -> float:
        return hw0 + ((y - y0) / (y1 - y0)) * (hw1 - hw0)

    mast_empty = r1kit.create_empty("Mast", parent=stage_4, pivot_u=(0.0, y0, 0.0))
    bpy.context.view_layer.update()

    # 1. Mast_Seg1: z 1.55-1.93 (lower third, tapering as today), pivot (0,0,1.55) + lock collar at z = 1.93
    seg1_mb = r1kit.MeshBuilder("Mast_Seg1", pivot_u=(0.0, y0, 0.0))
    _add_mast_lattice_span(seg1_mb, y0, y_s1, n_bays=2, hw_fn=hw_at, sign_offset=0)
    seg1_mb.add_cylinder(
        center_u=(0.0, 0.5 * (y0 + y_s1), 0.0),
        radius=0.020,
        length=(y_s1 - y0),
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    hw_193 = hw_at(y_s1)
    # Visible collar/clamp ring ("lock") at junction z = 1.93
    seg1_mb.add_bevel_box(
        center_u=(0.0, y_s1 - 0.006, 0.0),
        size_u=(2.0 * hw_193 + 0.046, 0.036, 2.0 * hw_193 + 0.046),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    seg1_mb.add_bevel_box(
        center_u=(0.0, y_s1 - 0.006, 0.0),
        size_u=(2.0 * hw_193 + 0.078, 0.024, 0.064),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    seg1_obj = seg1_mb.build_object(parent=mast_empty)

    # 2. Mast_Seg2: z 1.93-2.30, pivot (0,0,1.93), 0.05 m base overlap lip (down to 1.88), lock collar at z = 2.30,
    #    plus outrigger arms for DishAzimuth (+X) and SonarLoop (-X)
    seg2_mb = r1kit.MeshBuilder("Mast_Seg2", pivot_u=(0.0, y_s1, 0.0))
    y_s2_lip = y_s1 - overlap  # 1.88 m
    # 0.05 m base overlap lip sleeve (1.88..1.935 m) so telescoped Seg2 never shows a gap
    seg2_mb.add_bevel_box(
        center_u=(0.0, y_s2_lip + 0.0275, 0.0),
        size_u=(2.0 * hw_193 - 0.016, 0.055, 2.0 * hw_193 - 0.016),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    seg2_mb.add_bevel_box(
        center_u=(0.0, y_s2_lip + 0.008, 0.0),
        size_u=(2.0 * hw_193 + 0.008, 0.016, 2.0 * hw_193 + 0.008),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    _add_mast_lattice_span(seg2_mb, y_s1, y_s2, n_bays=1, hw_fn=hw_at, sign_offset=0)
    seg2_mb.add_cylinder(
        center_u=(0.0, 0.5 * (y_s2_lip + y_s2), 0.0),
        radius=0.018,
        length=(y_s2 - y_s2_lip),
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    hw_230 = hw_at(y_s2)
    # Visible collar/clamp ring ("lock") at junction z = 2.30
    seg2_mb.add_bevel_box(
        center_u=(0.0, y_s2 - 0.006, 0.0),
        size_u=(2.0 * hw_230 + 0.044, 0.036, 2.0 * hw_230 + 0.044),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    seg2_mb.add_bevel_box(
        center_u=(0.0, y_s2 - 0.006, 0.0),
        size_u=(2.0 * hw_230 + 0.074, 0.024, 0.058),
        mat="R1_Copper",
        bevel=0.004,
        segments=1,
    )

    # Viewer-left (+X) outrigger truss arm supporting DishAzimuth at (0.44, 2.12, 0.08)
    dax = d["dish_az_x"]
    day = d["dish_az_y"]
    daz = d["dish_az_z"]
    seg2_mb.add_strut(
        p0_u=(0.10, day - 0.02, 0.0),
        p1_u=(dax, day - 0.02, daz),
        width=0.036,
        thickness=0.036,
        mat="R1_GreyMetal",
        bevel=0.004,
    )
    seg2_mb.add_strut(
        p0_u=(0.11, y_s1 + 0.015, 0.0),
        p1_u=(dax - 0.02, day - 0.03, daz),
        width=0.024,
        thickness=0.024,
        mat="R1_GreyMetal",
        bevel=0.0,
    )
    seg2_mb.add_cylinder(
        center_u=(dax, day - 0.015, daz),
        radius=0.056,
        length=0.034,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    # Viewer-right (-X) horizontal brass boom supporting SonarLoop at (-0.38, 2.28, 0.0) (matching s_antenna_mast.png)
    sy_s = d["sonar_y"]
    seg2_mb.add_cylinder(
        center_u=(-0.09, sy_s, 0.0),
        radius=0.036,
        length=0.055,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    seg2_mb.add_cylinder(
        center_u=(-0.23, sy_s, 0.0),
        radius=0.016,
        length=0.28,
        mat="R1_Copper",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    seg2_obj = seg2_mb.build_object(parent=mast_empty)

    # 3. Mast_Seg3: z 2.30-2.68 with beacon pedestal/cage, pivot (0,0,2.30), 0.05 m base overlap lip (down to 2.25)
    seg3_mb = r1kit.MeshBuilder("Mast_Seg3", pivot_u=(0.0, y_s2, 0.0))
    y_s3_lip = y_s2 - overlap  # 2.25 m
    # 0.05 m base overlap lip sleeve (2.25..2.305 m) so telescoped Seg3 never shows a gap
    seg3_mb.add_bevel_box(
        center_u=(0.0, y_s3_lip + 0.0275, 0.0),
        size_u=(2.0 * hw_230 - 0.016, 0.055, 2.0 * hw_230 - 0.016),
        mat="R1_GreyMetal",
        bevel=0.005,
        segments=1,
    )
    seg3_mb.add_bevel_box(
        center_u=(0.0, y_s3_lip + 0.008, 0.0),
        size_u=(2.0 * hw_230 + 0.008, 0.016, 2.0 * hw_230 + 0.008),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    _add_mast_lattice_span(seg3_mb, y_s2, y1, n_bays=1, hw_fn=hw_at, sign_offset=1)
    seg3_mb.add_cylinder(
        center_u=(0.0, 0.5 * (y_s3_lip + y1), 0.0),
        radius=0.018,
        length=(y1 - y_s3_lip),
        mat="R1_Copper",
        axis_u="Y",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    # Top masthead beacon pedestal cap (y = 2.66 .. 2.71 m) + 4 guard cage ribs reaching y = 2.80 m
    seg3_mb.add_cylinder(
        center_u=(0.0, y1 + 0.015, 0.0),
        radius=0.078,
        length=0.036,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.005,
    )
    for angle_deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(angle_deg)
        gx = 0.062 * math.cos(rad)
        gz = 0.062 * math.sin(rad)
        seg3_mb.add_strut(
            p0_u=(gx, y1 + 0.02, gz),
            p1_u=(gx * 0.45, d["total_height"] - 0.005, gz * 0.45),
            width=0.012,
            thickness=0.012,
            mat="R1_GreyMetal",
            bevel=0.0,
        )
    seg3_obj = seg3_mb.build_object(parent=mast_empty)

    return mast_empty, seg1_obj, seg2_obj, seg3_obj


def _build_dish_and_azimuth(parent_obj: bpy.types.Object) -> None:
    """Build `DishAzimuth` (360° yaw mount) and its child `Dish` (Ø 0.95 m parabolic wire-mesh dish)."""
    d = ANTENNA_MAST_DIMS
    ax = d["dish_az_x"]
    ay = d["dish_az_y"]
    az = d["dish_az_z"]
    py = d["dish_piv_y"]

    # 1. DishAzimuth: rotary turntable + U-yoke + brass worm-gear elevation sector
    az_mb = r1kit.MeshBuilder("DishAzimuth", pivot_u=(ax, ay, az))
    az_mb.add_cylinder(
        center_u=(ax, ay + 0.022, az),
        radius=0.048,
        length=0.044,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )
    for side_x in (-0.048, 0.048):
        az_mb.add_strut(
            p0_u=(ax + side_x, ay + 0.03, az),
            p1_u=(ax + side_x, py + 0.01, az),
            width=0.024,
            thickness=0.034,
            mat="R1_GreyMetal",
            bevel=0.003,
        )
    az_mb.add_cylinder(
        center_u=(ax, py, az),
        radius=0.018,
        length=0.125,
        mat="R1_BareSteel",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    az_mb.add_cylinder(
        center_u=(ax - 0.028, py - 0.02, az - 0.01),
        radius=0.042,
        length=0.018,
        mat="R1_Copper",
        axis_u="X",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )
    az_obj = az_mb.build_object(parent=parent_obj)

    # 2. Dish: Ø 0.95 m open parabolic wire-mesh dish (12 radial parabolic ribs + 3 concentric rings + tripod feed horn)
    dish_mb = r1kit.MeshBuilder("Dish", pivot_u=(ax, py, az))
    r_max = d["dish_diameter"] * 0.5  # 0.475 m
    depth_z = d["dish_depth"]         # 0.18 m
    apex_z = az + 0.045

    # Rear mounting hub & copper coax feed boss
    dish_mb.add_cylinder(
        center_u=(ax, py, apex_z - 0.015),
        radius=0.085,
        length=0.042,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=10,
        bevel=0.005,
    )
    dish_mb.add_cylinder(
        center_u=(ax, py, apex_z + 0.012),
        radius=0.042,
        length=0.028,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.0,
        bevel_segments=0,
    )

    n_ribs = 12
    ring_radii = (r_max * 0.45, r_max * 0.74, r_max)
    ring_mats = ("R1_BareSteel", "R1_BareSteel", "R1_GreyMetal")
    ring_thick = (0.012, 0.014, 0.022)

    def _dish_pt(r: float, theta_rad: float) -> tuple[float, float, float]:
        u = r / r_max
        z_off = depth_z * (u * u)
        return (
            ax + r * math.cos(theta_rad),
            py + r * math.sin(theta_rad),
            apex_z + z_off,
        )

    # 12 radial ribs (2 parabolic chords each)
    for i in range(n_ribs):
        th = (2.0 * math.pi * i) / n_ribs
        p0 = _dish_pt(0.072, th)
        p1 = _dish_pt(ring_radii[1], th)
        p2 = _dish_pt(r_max, th)
        dish_mb.add_strut(p0, p1, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)
        dish_mb.add_strut(p1, p2, 0.014, 0.014, "R1_GreyMetal", bevel=0.0)

    # 3 concentric 12-sided polygon hoops forming the wire-mesh cage
    for r_ring, m_ring, t_ring in zip(ring_radii, ring_mats, ring_thick):
        for i in range(n_ribs):
            th0 = (2.0 * math.pi * i) / n_ribs
            th1 = (2.0 * math.pi * ((i + 1) % n_ribs)) / n_ribs
            dish_mb.add_strut(
                _dish_pt(r_ring, th0),
                _dish_pt(r_ring, th1),
                t_ring,
                t_ring,
                m_ring,
                bevel=0.0,
            )

    # Tripod feed-horn struts converging to the copper sub-reflector feed horn at z = apex_z + 0.34 m
    focal_z = apex_z + 0.34
    for angle_deg in (90.0, 210.0, 330.0):
        th_f = math.radians(angle_deg)
        p_base = _dish_pt(ring_radii[1], th_f)
        dish_mb.add_strut(
            p_base,
            (ax, py, focal_z - 0.015),
            0.012,
            0.012,
            "R1_BareSteel",
            bevel=0.0,
        )
    dish_mb.add_cylinder(
        center_u=(ax, py, focal_z),
        radius=0.044,
        radius_top=0.028,
        length=0.056,
        mat="R1_Copper",
        axis_u="Z",
        segments=8,
        bevel=0.004,
    )

    dish_mb.build_object(parent=az_obj)


def _build_stage_4(root: bpy.types.Object) -> bpy.types.Object:
    """Stage 4: `Mast` (`Mast_Seg1..3`), `DishAzimuth` -> `Dish`, `SonarLoop`, `TapeReel_L/R`, `BeaconLamp`, and `DialLamp`."""
    d = ANTENNA_MAST_DIMS
    stage_4 = r1kit.create_empty("Stage_4", parent=root, pivot_u=(0.0, 0.0, 0.0))

    # 1. Lattice Mast empty and 3 raisable segments
    _mast_empty, _seg1_obj, seg2_obj, seg3_obj = _build_mast(stage_4)

    # 2. DishAzimuth and Ø 0.95 m parabolic Dish (re-parented under Mast_Seg2)
    _build_dish_and_azimuth(seg2_obj)

    # 3. SonarLoop: crossed-loop brass figure-8 direction-finding antenna on viewer-right (-X) outrigger (re-parented under Mast_Seg2)
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
    # 4 diagonal figure-8 brass loop petals at +-45° and +-135° in the XY plane + orthogonal Z cross-loop
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
        # Two sides of the petal + rounded tip chord
        p_l = (mid_x + px_n * w_l, mid_y + py_n * w_l, sz)
        p_r = (mid_x - px_n * w_l, mid_y - py_n * w_l, sz)
        p_t1 = (tip_x + px_n * (w_l * 0.55), tip_y + py_n * (w_l * 0.55), sz)
        p_t2 = (tip_x - px_n * (w_l * 0.55), tip_y - py_n * (w_l * 0.55), sz)
        sonar_mb.add_strut((sx, sy, sz), p_l, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut((sx, sy, sz), p_r, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_l, p_t1, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_r, p_t2, 0.015, 0.015, "R1_Copper", bevel=0.0)
        sonar_mb.add_strut(p_t1, p_t2, 0.015, 0.015, "R1_Copper", bevel=0.0)
    sonar_mb.build_object(parent=seg2_obj)

    # 4. TapeReel_L (+X) and TapeReel_R (-X) inside the front recessed telemetry window
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

    # 5. BeaconLamp (re-parented under Mast_Seg3, only R1_LampAmber)
    by = d["beacon_y"]
    beacon_mb = r1kit.MeshBuilder("BeaconLamp", pivot_u=(0.0, by, 0.0))
    beacon_mb.add_cylinder(
        center_u=(0.0, by + 0.022, 0.0),
        radius=0.046,
        radius_top=0.032,
        length=0.072,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=10,
        bevel=0.006,
    )
    beacon_mb.build_object(parent=seg3_obj)

    # 6. DialLamp (child of Stage_4, only R1_LampAmber)
    dy = d["dial_y"]
    dz = d["dial_z"]
    dtilt = d["dial_tilt_deg"]
    dial_mb = r1kit.MeshBuilder("DialLamp", pivot_u=(0.0, dy, dz))
    dial_mb.add_cylinder(
        center_u=(0.0, dy, dz + 0.006),
        radius=0.078,
        length=0.018,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=10,
        bevel=0.003,
        rot_deg_u=(dtilt, 0.0, 0.0),
    )
    dial_mb.build_object(parent=stage_4)

    return stage_4


def pose_antenna_mast_rest(root: bpy.types.Object) -> None:
    """Set AntennaMast moving parts to rest pose (0° dish pitch, 0° azimuth, 0° sonar & tape reels)."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name in ("DishAzimuth", "Dish", "SonarLoop", "TapeReel_L", "TapeReel_R"):
            obj.rotation_euler = (0.0, 0.0, 0.0)


def pose_antenna_mast_active(root: bpy.types.Object) -> None:
    """Set AntennaMast moving parts to active tracking pose (DishAzimuth yawed, Dish pitched +32° upward, SonarLoop & reels spun)."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.name == "DishAzimuth":
            obj.rotation_euler = r1kit.u_rot(0.0, -28.0, 0.0)
        elif obj.name == "Dish":
            obj.rotation_euler = r1kit.u_rot(-32.0, 0.0, 0.0)
        elif obj.name == "SonarLoop":
            obj.rotation_euler = r1kit.u_rot(0.0, 42.0, 0.0)
        elif obj.name == "TapeReel_L":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, 48.0)
        elif obj.name == "TapeReel_R":
            obj.rotation_euler = r1kit.u_rot(0.0, 0.0, 48.0)


def render_antenna_mast_compare(root: bpy.types.Object, out_dir: Path) -> None:
    """Render side-by-side concept comparison (`Logs/renders/compare_antenna_mast.png`) against `s_antenna_mast.png`."""
    d = ANTENNA_MAST_DIMS
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    r1kit.ensure_materials(use_vertex_color_shading=True)
    temp_objs = r1kit._setup_preview_lighting_and_plinth(d["footprint_width"] * 0.72)

    cam_data = bpy.data.cameras.new("_CompareCam")
    cam_obj = bpy.data.objects.new("_CompareCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    temp_objs.append(cam_obj)

    # Match the front-right (-X, +Z) three-quarter angle of s_antenna_mast.png so the right sage patch,
    # front tape reels, left parabolic dish, and right sonar loop all line up with the concept!
    pose_antenna_mast_active(root)
    bpy.context.view_layer.update()
    mid_y = d["total_height"] * 0.48
    dist = d["total_height"] * 1.95
    r1kit._aim_camera_at(
        cam_obj,
        (-dist * 0.42, mid_y + dist * 0.16, dist * 0.92),
        (0.0, mid_y * 0.95, 0.0),
        lens_mm=48.0,
    )
    p_hero = out_dir / "_tmp_antenna_mast_concept_match.png"
    r1kit._render_to_file(p_hero, res_x=960, res_y=960)

    concept_path = Path(r"D:\Project\MoonProject-wt\agy-concept\Logs\base\s_antenna_mast.png")
    if concept_path.exists():
        r1kit.compose_side_by_side(
            concept_path,
            p_hero,
            out_dir / "compare_antenna_mast.png",
            target_height=768,
        )
    p_hero.unlink(missing_ok=True)

    pose_antenna_mast_rest(root)
    bpy.context.view_layer.update()

    for tob in temp_objs:
        if tob.name in bpy.data.objects:
            bpy.data.objects.remove(tob, do_unlink=True)
    plinth_mat = bpy.data.materials.get("_PreviewPlinthMat")
    if plinth_mat is not None:
        bpy.data.materials.remove(plinth_mat)
    r1kit.ensure_materials(use_vertex_color_shading=False)


def build_antenna_mast() -> bpy.types.Object:
    """Build the complete AntennaMast hierarchy in the current Blender scene and bake vertex colours."""
    root = r1kit.create_empty("AntennaMast", pivot_u=(0.0, 0.0, 0.0))
    _build_stage_1(root)
    _build_stage_2(root)
    _build_stage_3(root)
    _build_stage_4(root)
    pose_antenna_mast_rest(root)
    bpy.context.view_layer.update()
    r1kit.bake_vertex_colors(root, footprint_radius=ANTENNA_MAST_DIMS["footprint_width"] * 0.65)
    return root


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build AntennaMast FBX and preview renders.")
    parser.add_argument(
        "--fbx-out",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Structures/AntennaMast.fbx"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)

    r1kit.reset_scene()
    root = build_antenna_mast()
    tris = r1kit.count_triangles(root)
    print(f"[AntennaMast] Triangles: {tris}")
    r1kit.export_fbx(root, args.fbx_out)

    if not args.skip_renders:
        render_antenna_mast_compare(root, args.renders_dir)
        r1kit.render_structure_suite(
            root=root,
            slug="antenna_mast",
            out_dir=args.renders_dir,
            target_height=ANTENNA_MAST_DIMS["total_height"],
            footprint_radius=ANTENNA_MAST_DIMS["footprint_width"] * 0.65,
            pose_rest_fn=pose_antenna_mast_rest,
            pose_active_fn=pose_antenna_mast_active,
            pose_rest_name="pose_rest",
            pose_active_name="pose_active",
        )


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
