#!/usr/bin/env python3
"""
compare_solar_mechanisms: Builds, renders (4-frame closed->open strip), and measures
minimum 3D clearance across the full head/neck/kit pose space for three candidate
SolarWing mechanisms on 07:

  - Mechanism A ("trunk lid"): Single 2x3 panel in a shallow roof recess behind the
    head's sweep, hinged on its head-side edge, lifting its rear edge up and away (0-70 deg).
  - Mechanism B ("butterfly halves"): Two symmetric 1x3 half-panels hinged along the spine,
    opening left and right like wing covers (0-65 deg).
  - Mechanism C ("rear mast sky-lift"): Panel on a rear articulated mast/hinge at the back
    of the roof recess that rises and tilts from the rear edge (0-70 deg).

Run:
    python tools/art/qa/compare_solar_mechanisms.py
"""
from __future__ import annotations

import copy
import json
import math
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BLENDER = Path("D:/Blender/blender.exe")


def worktree_root() -> Path:
    res = subprocess.run(
        ["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
    )
    return Path(res.stdout.strip()) if res.returncode == 0 else Path.cwd()


def run_in_blender() -> int:
    import bmesh
    import bpy
    from mathutils import Euler, Matrix, Quaternion, Vector
    from mathutils.bvhtree import BVHTree

    root = worktree_root()
    rover_dir = root / "tools" / "art" / "blender" / "rover"
    qa_dir = root / "tools" / "art" / "qa"
    for p in (str(rover_dir), str(qa_dir)):
        if p not in sys.path:
            sys.path.insert(0, p)

    import build_rover
    import mesh_ops
    import render_suite
    import rover_clearance
    import rover_parts

    renders_dir = root / "Logs" / "renders"
    renders_dir.mkdir(parents=True, exist_ok=True)
    fbx_dir = root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover"

    # Patch _append_pillowed_body_tub and build_contoured_body_mesh to include the recessed solar bay
    def build_recessed_body_tub(bm, cfg):
        temp = bmesh.new()
        half_w = cfg["body_tub_width"] * rover_parts.FLAT_UV_SCALE
        half_l = cfg["body_tub_length"] * rover_parts.FLAT_UV_SCALE
        z_center = cfg["body_tub_z_center"]
        z_min_base = z_center - half_l
        z_max_base = z_center + half_l
        corner_r = 0.085
        belt_bot = cfg["body_belt_y_bottom"]
        belt_top = cfg["body_belt_y_top"]
        w_base = cfg["wheel_base"]

        # Rings 0..20 build the outer beetle hull & front deck; Rings 21..23 step into the rear roof recess
        ring_specs = (
            (0.415, 0.110, 0.110, 0.110, 1.00, "bottom"),
            (0.415, 0.022, 0.022, 0.022, 1.00, "bottom"),
            (0.424, 0.008, 0.008, 0.008, 1.00, "lower_cream"),
            (0.438, 0.002, 0.002, 0.002, 1.00, "lower_cream"),
            (0.550, -0.002, 0.000, 0.000, 1.02, "lower_cream"),
            (0.555, 0.005, 0.005, 0.005, 1.00, "seam"),
            (belt_bot, 0.005, 0.005, 0.005, 1.00, "seam"),
            (0.564, -0.004, -0.002, -0.002, 1.04, "belt"),
            (0.605, -0.007, -0.004, -0.004, 1.06, "belt"),
            (0.644, -0.004, -0.002, -0.002, 1.04, "belt"),
            (belt_top, 0.005, 0.005, 0.005, 1.00, "seam"),
            (0.654, 0.005, 0.005, 0.005, 1.00, "seam"),
            (0.660, -0.002, 0.000, 0.004, 1.04, "upper_cream"),
            (0.735, -0.001, 0.008, 0.022, 1.12, "upper_cream"),
            (0.790, 0.005, 0.018, 0.135, 1.25, "upper_cream"),
            (0.835, 0.014, 0.036, 0.295, 1.35, "upper_cream"),
            (0.875, 0.026, 0.056, 0.365, 1.42, "upper_cream"),
            (0.912, 0.042, 0.078, 0.405, 1.48, "upper_cream"),
            (0.942, 0.062, 0.095, 0.435, 1.52, "upper_cream"),
            (0.956, 0.090, 0.102, 0.458, 1.52, "upper_cream"),
            (0.958, 0.140, 0.104, 0.520, 1.45, "upper_cream"),
            # Rear roof recess rim & stepped dark metal solar bay well (uz = [-0.653, -0.360], |ux| <= 0.248)
            (0.948, 0.192, 0.092, 1.105, 0.35, "recess_rim"),
            (0.902, 0.198, 0.098, 1.111, 0.32, "recess_floor"),
        )

        ring_verts = []
        for r_idx, (uy, in_x, in_zr, in_zf, cr_scale, role) in enumerate(ring_specs):
            hx = max(0.08, half_w - in_x)
            z_min = z_min_base + in_zr
            z_max = z_max_base - in_zf
            pts_2d = rover_parts._sample_rounded_rect_ring(hx, z_min, z_max, corner_r * cr_scale)
            v_row = []
            for ux, uz in pts_2d:
                adj_y = uy
                adj_x = ux
                z_norm = max(-1.0, min(1.0, uz / max(half_l, 0.1)))
                if 4 <= r_idx <= 16 and abs(ux) > hx * 0.65:
                    barrel_x = 0.008 * max(0.0, 1.0 - z_norm * z_norm)
                    adj_x += math.copysign(barrel_x, ux)
                if 13 <= r_idx <= 20 and abs(ux) < hx * 0.85:
                    x_norm = ux / max(hx, 0.08)
                    # Taper lateral crown down over the rear deck so rear rim stays <= 0.958 m
                    rear_taper = 0.25 if uz < -0.32 else 1.0
                    adj_y += 0.008 * rear_taper * max(0.0, 1.0 - x_norm * x_norm)
                if r_idx <= 8 and abs(ux) > half_w * 0.70:
                    y_arch = rover_parts._wheel_arch_cutout_y(uz, wheel_base=w_base)
                    if y_arch > 0.417:
                        if r_idx == 0:
                            adj_y = max(uy, y_arch - 0.012)
                        elif r_idx == 1:
                            adj_y = max(uy, y_arch - 0.008)
                        elif r_idx == 2:
                            adj_y = max(uy, y_arch - 0.003)
                        elif r_idx <= 6:
                            adj_y = max(uy, y_arch)
                        else:
                            adj_y = max(uy, y_arch + 0.006)
                v_row.append(temp.verts.new(rover_parts.unity_to_blender_pos(adj_x, adj_y, uz)))
            ring_verts.append(v_row)

        n_perim = len(ring_verts[0])
        for r_idx in range(len(ring_specs) - 1):
            r0 = ring_verts[r_idx]
            r1 = ring_verts[r_idx + 1]
            role0 = ring_specs[r_idx][5]
            role1 = ring_specs[r_idx + 1][5]
            if role0 == "seam" or role1 == "seam" or role1 == "recess_floor":
                mat_idx = 2  # R1_GreyMetal
            elif role0 == "belt" and role1 == "belt":
                mat_idx = 1  # R1_OrangeStripe
            elif role0 == "bottom":
                mat_idx = 2  # R1_GreyMetal
            else:
                mat_idx = 0  # R1_CreamEnamel
            for i in range(n_perim):
                ni = (i + 1) % n_perim
                f = temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
                f.material_index = mat_idx
                f.smooth = True

        bot_center = temp.verts.new(rover_parts.unity_to_blender_pos(0.0, 0.415, z_center))
        top_center = temp.verts.new(rover_parts.unity_to_blender_pos(0.0, 0.902, -0.506))
        for i in range(n_perim):
            ni = (i + 1) % n_perim
            fb = temp.faces.new((ring_verts[0][ni], ring_verts[0][i], bot_center))
            fb.material_index = 2
            fb.smooth = True
            ft = temp.faces.new((ring_verts[-1][i], ring_verts[-1][ni], top_center))
            ft.material_index = 2
            ft.smooth = True

        # Lower beetle bonnet hood shell over front nose
        hood_Temp = bmesh.new()
        hood_z_stations = (
            (0.24, 0.365, 0.845, 0.018),
            (0.34, 0.360, 0.838, 0.022),
            (0.45, 0.350, 0.818, 0.024),
            (0.55, 0.335, 0.792, 0.024),
            (0.64, 0.310, 0.756, 0.020),
            (0.70, 0.270, 0.715, 0.014),
            (0.73, 0.210, 0.675, 0.008),
        )
        hood_rings = []
        n_hx = 10
        for hz, hw_x, hy_top, h_crown in hood_z_stations:
            row_v = []
            for ix in range(n_hx + 1):
                tx = -1.0 + 2.0 * (ix / n_hx)
                ux = tx * hw_x
                uy = hy_top + h_crown * (1.0 - tx * tx)
                row_v.append(hood_Temp.verts.new(rover_parts.unity_to_blender_pos(ux, uy, hz)))
            for ix in range(n_hx, -1, -1):
                tx = -1.0 + 2.0 * (ix / n_hx)
                ux = tx * (hw_x * 0.92)
                uy = hy_top - 0.045
                row_v.append(hood_Temp.verts.new(rover_parts.unity_to_blender_pos(ux, uy, hz)))
            hood_rings.append(row_v)

        n_hr = len(hood_rings[0])
        for k in range(len(hood_rings) - 1):
            r0 = hood_rings[k]
            r1 = hood_rings[k + 1]
            for i in range(n_hr):
                ni = (i + 1) % n_hr
                f = hood_Temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
                f.material_index = 0
                f.smooth = True

        back_c = hood_Temp.verts.new(rover_parts.unity_to_blender_pos(0.0, 0.82, 0.24))
        front_c = hood_Temp.verts.new(rover_parts.unity_to_blender_pos(0.0, 0.66, 0.73))
        for i in range(n_hr):
            ni = (i + 1) % n_hr
            fb = hood_Temp.faces.new((hood_rings[0][ni], hood_rings[0][i], back_c))
            fb.material_index = 0
            fb.smooth = True
            ff = hood_Temp.faces.new((hood_rings[-1][i], hood_rings[-1][ni], front_c))
            ff.material_index = 0
            ff.smooth = True

        rover_parts.ensure_outward_normals(hood_Temp)
        for v in hood_Temp.verts:
            temp.verts.new(v.co)
        temp.verts.ensure_lookup_table()
        offset_v = len(temp.verts) - len(hood_Temp.verts)
        for f in hood_Temp.faces:
            nf = temp.faces.new([temp.verts[offset_v + v.index] for v in f.verts])
            nf.material_index = f.material_index
            nf.smooth = True
        hood_Temp.free()

        rover_parts.ensure_outward_normals(temp)
        vert_map = {v: bm.verts.new(v.co) for v in temp.verts}
        for f in temp.faces:
            nf = bm.faces.new([vert_map[v] for v in f.verts])
            nf.material_index = f.material_index
            nf.smooth = True
        temp.free()

        # Low-profile recessed bay rib rails & hinge mounting pads inside the dark roof well (uy <= 0.916 m)
        for sx in (-1.0, 1.0):
            rover_parts.append_beveled_box(
                bm,
                size_xyz=(0.032, 0.008, 0.210),
                center_xyz=(sx * 0.145, 0.908, -0.502),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=2,
                guard_inset=False,
            )
            rover_parts.append_beveled_box(
                bm,
                size_xyz=(0.044, 0.010, 0.028),
                center_xyz=(sx * 0.145, 0.911, -0.395),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_BareSteel hinge pad
                guard_inset=False,
            )

    def build_recessed_body_mesh(cfg, mats):
        bm = bmesh.new()
        mat_list = [
            mats["R1_CreamEnamel"],
            mats["R1_OrangeStripe"],
            mats["R1_GreyMetal"],
            mats["R1_SagePatch"],
            mats["R1_GoldFoil"],
            mats["R1_BareSteel"],
            mats["R1_Copper"],
            mats["R1_DarkGlass"],
            mats["R1_LampAmber"],
            mats["R1_Rubber"],
            mats["R1_Brass"],
        ]
        build_recessed_body_tub(bm, cfg)
        rover_parts._append_body_cut_panels_and_seams(bm, cfg)
        rover_parts.append_cylinder(
            bm, radius1=0.128, radius2=0.114, length=0.20, axis="Z",
            center_xyz=(0.0, 0.695, 0.668), segments=24, bevel_width=0.014, bevel_segments=2, mat_idx=0,
        )
        rover_parts.append_cylinder(
            bm, radius1=0.112, radius2=0.108, length=0.024, axis="Z",
            center_xyz=(0.0, 0.692, 0.764), segments=24, bevel_width=0.004, bevel_segments=1, mat_idx=2,
        )
        rover_parts.append_beveled_box(
            bm, size_xyz=(cfg["bumper_width"], cfg["bumper_height"], cfg["bumper_depth"]),
            center_xyz=(0.0, cfg["bumper_y_center"], cfg["front_bumper_z"]),
            bevel_width=cfg["bumper_bevel"], bevel_segments=2, mat_idx=2,
        )
        rover_parts.append_beveled_box(
            bm, size_xyz=(cfg["bumper_width"], cfg["bumper_height"], cfg["bumper_depth"]),
            center_xyz=(0.0, cfg["bumper_y_center"], cfg["rear_bumper_z"]),
            bevel_width=cfg["bumper_bevel"], bevel_segments=2, mat_idx=2,
        )
        rover_parts.append_beveled_box(
            bm, size_xyz=(cfg["belly_pan_width"], cfg["belly_pan_height"], cfg["belly_pan_length"]),
            center_xyz=(0.0, cfg["belly_pan_y"], 0.0),
            bevel_width=0.014, bevel_segments=2, mat_idx=2,
        )
        rover_parts._append_wheel_arch_flares_and_liners(bm, cfg)
        rover_parts._append_body_small_hardware(bm, cfg)
        return rover_parts.create_mesh_node("Body", bm, (0.0, 0.0, 0.0), mat_list=mat_list, cfg=cfg)

    rover_parts._append_pillowed_body_tub = build_recessed_body_tub
    rover_parts.build_contoured_body_mesh = build_recessed_body_mesh

    # Helper to build Mechanism A, B, or C on SolarWing
    def make_wing_builder(mechanism: str):
        def _builder(cfg, mats, parent=None, unity_loc=(0.0, 0.0, 0.0), wing_socket_loc=(0.0, 0.0, -0.12), split_gift_cell=False):
            mat_list = [
                mats["R1_GreyMetal"],
                mats["R1_BareSteel"],
                mats["R1_SolarCell"],
                mats["R1_GoldFoil"],
                mats["R1_Copper"],
            ]
            sw_w = cfg["solar_wing_width"]   # 0.40 m
            sw_l = cfg["solar_wing_length"]  # 0.22 m

            if mechanism == "A":
                # Mechanism A ("trunk lid"): hinged at head-side edge (z_local = 0), extending to z_local = -sw_l
                bm = bmesh.new()
                frame_cz = -sw_l * 0.5
                rover_parts.append_beveled_box(
                    bm, size_xyz=(sw_w, 0.016, sw_l), center_xyz=(0.0, 0.0, frame_cz),
                    bevel_width=0.006, bevel_segments=2, mat_idx=0,
                )
                for sx in (-1.0, 1.0):
                    rover_parts.append_beveled_box(
                        bm, size_xyz=(sw_w * 0.32, 0.005, sw_l * 0.72), center_xyz=(sx * (sw_w * 0.22), -0.007, frame_cz),
                        bevel_width=0.002, bevel_segments=1, mat_idx=3,
                    )
                cols, rows = 2, 3
                cell_w = (sw_w * 0.84) / cols
                cell_l = (sw_l * 0.80) / rows
                gift_cell_bm = None
                gift_cell_loc = (0.0, 0.0, 0.0)
                for c in range(cols):
                    cx = (-0.5 + c) * (cell_w + 0.014)
                    for r in range(rows):
                        cz_local = frame_cz + (1 - r) * (cell_l + 0.009)
                        cy_local = 0.008
                        if split_gift_cell and c == 1 and r == 1:
                            gift_cell_bm = bmesh.new()
                            gift_cell_loc = (round(cx, 4), round(cy_local, 4), round(cz_local, 4))
                            rover_parts.append_beveled_box(
                                gift_cell_bm, size_xyz=(cell_w, 0.008, cell_l), center_xyz=(0.0, 0.0, 0.0),
                                bevel_width=0.003, bevel_segments=2, mat_idx=0,
                            )
                            rover_parts.append_beveled_box(
                                gift_cell_bm, size_xyz=(0.005, 0.009, cell_l * 0.92), center_xyz=(0.0, 0.0015, 0.0),
                                bevel_width=0.001, bevel_segments=1, mat_idx=1,
                            )
                            continue
                        rover_parts.append_beveled_box(
                            bm, size_xyz=(cell_w, 0.008, cell_l), center_xyz=(cx, cy_local, cz_local),
                            bevel_width=0.003, bevel_segments=2, mat_idx=2,
                        )
                        rover_parts.append_beveled_box(
                            bm, size_xyz=(0.005, 0.009, cell_l * 0.92), center_xyz=(cx, cy_local + 0.0015, cz_local),
                            bevel_width=0.001, bevel_segments=1, mat_idx=1,
                        )
                # Head-side hinge knuckles & pin along local z = 0
                for sx in (-1.0, 1.0):
                    rover_parts.append_cylinder(
                        bm, radius1=0.010, radius2=0.010, length=0.048, axis="X",
                        center_xyz=(sx * 0.145, 0.0, 0.0), segments=14, bevel_width=0.002, bevel_segments=1, mat_idx=1,
                    )
                rover_parts.append_beveled_box(
                    bm, size_xyz=(0.052, 0.014, 0.028), center_xyz=(0.0, 0.004, -0.016),
                    bevel_width=0.003, bevel_segments=1, mat_idx=4,
                )
                sw_obj = rover_parts.create_mesh_node("SolarWing", bm, unity_loc, parent=parent, mat_list=mat_list, cfg=cfg)
                rover_parts.create_empty_node("WingSocket", wing_socket_loc, parent=sw_obj)
                if gift_cell_bm is not None:
                    rover_parts.create_mesh_node(
                        "CellFilled", gift_cell_bm, gift_cell_loc, parent=sw_obj,
                        mat_list=[mats["R1_SolarCell"], mats["R1_BareSteel"]], cfg=cfg,
                    )
                return sw_obj

            elif mechanism == "B":
                # Mechanism B ("butterfly halves"): two 1x3 half-panels hinged along the spine
                sw_root = rover_parts.create_empty_node("SolarWing", unity_loc, parent=parent)
                rover_parts.create_empty_node("WingSocket", wing_socket_loc, parent=sw_root)
                half_w = sw_w * 0.46
                frame_cz = -sw_l * 0.5
                for side_name, sx in (("SolarWing_L", -1.0), ("SolarWing_R", 1.0)):
                    bm = bmesh.new()
                    panel_cx = sx * (half_w * 0.5 + 0.008)
                    rover_parts.append_beveled_box(
                        bm, size_xyz=(half_w, 0.016, sw_l), center_xyz=(panel_cx, 0.0, frame_cz),
                        bevel_width=0.005, bevel_segments=2, mat_idx=0,
                    )
                    cell_w = half_w * 0.80
                    cell_l = (sw_l * 0.80) / 3
                    for r in range(3):
                        cz_local = frame_cz + (1 - r) * (cell_l + 0.009)
                        rover_parts.append_beveled_box(
                            bm, size_xyz=(cell_w, 0.008, cell_l), center_xyz=(panel_cx, 0.008, cz_local),
                            bevel_width=0.003, bevel_segments=2, mat_idx=2,
                        )
                    # Spine hinge barrel along Z
                    rover_parts.append_cylinder(
                        bm, radius1=0.009, radius2=0.009, length=sw_l * 0.75, axis="Z",
                        center_xyz=(sx * 0.006, 0.0, frame_cz), segments=12, bevel_width=0.002, bevel_segments=1, mat_idx=1,
                    )
                    rover_parts.create_mesh_node(
                        side_name, bm, (sx * 0.012, 0.0, 0.0), parent=sw_root, mat_list=mat_list, cfg=cfg
                    )
                return sw_root

            else:
                # Mechanism C ("rear mast sky-lift"): hinged at rear edge (z = -0.614), lifting front edge up
                bm = bmesh.new()
                frame_cz = +sw_l * 0.5
                rover_parts.append_beveled_box(
                    bm, size_xyz=(sw_w, 0.016, sw_l), center_xyz=(0.0, 0.018, frame_cz),
                    bevel_width=0.006, bevel_segments=2, mat_idx=0,
                )
                cols, rows = 2, 3
                cell_w = (sw_w * 0.84) / cols
                cell_l = (sw_l * 0.80) / rows
                for c in range(cols):
                    cx = (-0.5 + c) * (cell_w + 0.014)
                    for r in range(rows):
                        cz_local = frame_cz + (1 - r) * (cell_l + 0.009)
                        rover_parts.append_beveled_box(
                            bm, size_xyz=(cell_w, 0.008, cell_l), center_xyz=(cx, 0.026, cz_local),
                            bevel_width=0.003, bevel_segments=2, mat_idx=2,
                        )
                # Rear pedestal struts + hinge barrel
                for sx in (-1.0, 1.0):
                    rover_parts.append_beveled_box(
                        bm, size_xyz=(0.024, 0.024, 0.028), center_xyz=(sx * 0.145, 0.008, 0.008),
                        bevel_width=0.003, bevel_segments=1, mat_idx=1,
                    )
                    rover_parts.append_cylinder(
                        bm, radius1=0.010, radius2=0.010, length=0.048, axis="X",
                        center_xyz=(sx * 0.145, 0.0, 0.0), segments=14, bevel_width=0.002, bevel_segments=1, mat_idx=1,
                    )
                sw_obj = rover_parts.create_mesh_node("SolarWing", bm, unity_loc, parent=parent, mat_list=mat_list, cfg=cfg)
                rover_parts.create_empty_node("WingSocket", (0.0, 0.018, frame_cz), parent=sw_obj)
                return sw_obj

        return _builder

    def mount_kits_in_scene():
        for sock_name in ("DrumSocket_L", "BinSocket_L", "RingSocket"):
            sock = bpy.data.objects.get(sock_name)
            if sock is not None:
                sock.rotation_mode = "XYZ"
                sock.rotation_euler = (0.0, 0.0, math.pi)
        mounts = [
            ("Kit_LampBar.fbx", "HeadlampSocket", "Kit_LampBar::"),
            ("Kit_CapacitorDrum.fbx", "DrumSocket_L", "Kit_CapacitorDrum_L::"),
            ("Kit_CapacitorDrum.fbx", "DrumSocket_R", "Kit_CapacitorDrum_R::"),
            ("Kit_BatterySpine.fbx", "SpineSocket", "Kit_BatterySpine::"),
            ("Kit_TetherRing.fbx", "RingSocket", "Kit_TetherRing::"),
            ("Kit_CargoRack.fbx", "CargoSocket", "Kit_CargoRack::"),
            ("Kit_SideBin.fbx", "BinSocket_L", "Kit_SideBin_L::"),
            ("Kit_SideBin.fbx", "BinSocket_R", "Kit_SideBin_R::"),
            ("HoverCoils.fbx", "CoilSocket", "HoverCoils::"),
            ("FocusLens.fbx", "LensSocket", "FocusLens::"),
            ("LensGlass.fbx", "Eye", "LensGlass::"),
            ("ScanFan.fbx", "ScanFanSocket", "ScanFan::"),
            ("Kit_TapeDeck.fbx", "DeckSocket", "Kit_TapeDeck::"),
        ]
        for fbx_name, sock_name, prefix in mounts:
            sock = bpy.data.objects.get(sock_name)
            fbx_path = fbx_dir / fbx_name
            if sock is None or not fbx_path.exists():
                continue
            kit_root = rover_clearance._import_fbx_root(fbx_path, prefix=prefix)
            kit_root.parent = sock
            kit_root.matrix_parent_inverse = Matrix.Identity(4)
            kit_root.location = (0.0, 0.0, 0.0)
            kit_root.rotation_mode = "XYZ"
            kit_root.rotation_euler = (0.0, 0.0, 0.0)
            kit_root.scale = (1.0, 1.0, 1.0)
        bpy.context.view_layer.update()

    def apply_wing_pose(mechanism: str, frac: float):
        sw = bpy.data.objects.get("SolarWing")
        if mechanism == "A":
            ang = frac * 70.0
            sw.rotation_mode = "QUATERNION"
            sw.rotation_quaternion = rover_clearance.unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), ang).to_quaternion()
        elif mechanism == "B":
            ang = frac * 60.0
            sw_l = bpy.data.objects.get("SolarWing_L")
            sw_r = bpy.data.objects.get("SolarWing_R")
            if sw_l and sw_r:
                sw_l.rotation_mode = "QUATERNION"
                sw_r.rotation_mode = "QUATERNION"
                sw_l.rotation_quaternion = rover_clearance.unity_axis_angle_to_blender_matrix((0.0, 0.0, 1.0), -ang).to_quaternion()
                sw_r.rotation_quaternion = rover_clearance.unity_axis_angle_to_blender_matrix((0.0, 0.0, 1.0), +ang).to_quaternion()
        else:
            ang = frac * 65.0
            lift = frac * 0.035
            sw.location = Vector((0.0, -0.614, 0.948 + lift))
            sw.rotation_mode = "QUATERNION"
            sw.rotation_quaternion = rover_clearance.unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), -ang).to_quaternion()
        bpy.context.view_layer.update()

    yaw_samples = [-125.0, -100.0, -75.0, -50.0, -25.0, 0.0, 25.0, 50.0, 75.0, 100.0, 125.0]
    lift_samples = [-6.0, 0.0, 5.5, 11.0]
    pitch_samples = [-25.0, 0.0, 20.0, 45.0, 55.0]
    wing_fracs = [i / 7.0 for i in range(8)]

    results = {}

    for mech in ("A", "B", "C"):
        mesh_ops.clear_scene()
        cfg = copy.deepcopy(build_rover.DIMENSIONS)
        cfg["solar_wing_width"] = 0.40
        cfg["solar_wing_length"] = 0.220
        cfg["solar_deck_slope_deg"] = 0.0
        if mech in ("A", "B"):
            cfg["piv_solar_wing"] = (0.0, 0.948, -0.394)
        else:
            cfg["piv_solar_wing"] = (0.0, 0.948, -0.614)

        rover_parts.build_solar_wing_model = make_wing_builder(mech)
        mats = rover_parts.create_material_library(build_rover.MATERIALS, for_render=True)
        rover_root = rover_parts.build_rover_model_hierarchy(cfg, mats)
        rv_meshes = [o for o in bpy.data.objects if o.type == "MESH"]
        mesh_ops.bake_model_vertex_colors(rv_meshes, cfg, include_ground_occluder=True)

        mount_kits_in_scene()

        rover_parts.configure_procedural_crt_screen_shader(mats["R1_Screen"], build_rover.MATERIALS["R1_Screen"])
        render_suite.enhance_materials_with_vertex_color_for_render(mats)
        cam_obj, _ = render_suite.setup_studio_lighting_and_camera(cfg)

        scene = bpy.context.scene
        scene.render.resolution_x = 520
        scene.render.resolution_y = 440
        cam_obj.data.type = "PERSP"
        cam_obj.data.lens = 50.0
        cam_pos = Vector((-1.32, -0.95, 1.88))
        target_pos = Vector((0.0, -0.28, 1.04))
        cam_obj.location = cam_pos
        direction = (target_pos - cam_pos).normalized()
        cam_obj.rotation_mode = "QUATERNION"
        cam_obj.rotation_quaternion = direction.to_track_quat("-Z", "Y")

        for idx, frac in enumerate((0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0)):
            apply_wing_pose(mech, frac)
            frame_path = renders_dir / f"_mech_{mech}_f{idx}.png"
            scene.render.filepath = str(frame_path)
            bpy.ops.render.render(write_still=True)

        strip_out = renders_dir / f"mechanism_{mech}_strip.png"

        # Measure minimum clearance across the full pose grid
        apply_wing_pose(mech, 0.0)
        wing_node = bpy.data.objects.get("SolarWing")
        neck_node = bpy.data.objects.get("Neck")
        neck_lift_node = bpy.data.objects.get("NeckLift")
        head_node = bpy.data.objects.get("Head")

        m_neck_rest = neck_node.matrix_world.copy()
        m_neck_lift_rest = neck_lift_node.matrix_world.copy()
        m_head_rest = head_node.matrix_world.copy()
        m_neck_to_lift_rest = m_neck_rest.inverted() @ m_neck_lift_rest
        m_lift_to_head_rest = m_neck_lift_rest.inverted() @ m_head_rest

        wing_meshes = [o for o in [wing_node] + list(wing_node.children_recursive) if o.type == "MESH"]
        wing_mesh_set = set(wing_meshes)
        head_descendants = set([head_node] + list(head_node.children_recursive))
        antenna_node = bpy.data.objects.get("Antenna")
        antenna_descendants = set([antenna_node] + list(antenna_node.children_recursive)) if antenna_node else set()

        kit_prefixes = [
            "Kit_BatterySpine", "Kit_TetherRing", "Kit_TapeDeck", "FocusLens", "Kit_CargoRack",
        ]
        kit_mesh_map = {k: [] for k in kit_prefixes}
        head_meshes, neck_meshes, antenna_meshes, body_meshes = [], [], [], []
        for obj in bpy.context.scene.objects:
            if obj.type != "MESH" or obj in wing_mesh_set or obj.name == "GroundOccluder":
                continue
            matched = next((kp for kp in kit_prefixes if obj.name.startswith(kp + "::")), None)
            if matched:
                kit_mesh_map[matched].append(obj)
            elif any(obj.name.startswith(p + "::") for p in ("Kit_CapacitorDrum", "Kit_SideBin", "Kit_LampBar", "HoverCoils", "LensGlass", "ScanFan")):
                if obj in head_descendants:
                    head_meshes.append(obj)
                else:
                    body_meshes.append(obj)
            elif obj in (neck_node, neck_lift_node):
                neck_meshes.append(obj)
            elif obj in head_descendants:
                head_meshes.append(obj)
            elif obj in antenna_descendants:
                antenna_meshes.append(obj)
            else:
                body_meshes.append(obj)

        world_inv = Matrix.Identity(4)
        static_targets = {
            "Body": rover_clearance.extract_combined_geometry([ bpy.data.objects.get("Body") ], world_inv),
            "Antenna": rover_clearance.extract_combined_geometry(antenna_meshes, world_inv),
            "Kit_BatterySpine": rover_clearance.extract_combined_geometry(kit_mesh_map["Kit_BatterySpine"], world_inv),
            "Kit_CargoRack": rover_clearance.extract_combined_geometry(kit_mesh_map["Kit_CargoRack"], world_inv),
        }
        neck_base_geom = rover_clearance.extract_combined_geometry(neck_meshes, m_neck_rest.inverted())
        head_inv = m_head_rest.inverted()
        head_targets = {
            "Head": rover_clearance.extract_combined_geometry(head_meshes, head_inv),
            "Kit_TetherRing": rover_clearance.extract_combined_geometry(kit_mesh_map["Kit_TetherRing"], head_inv),
            "Kit_TapeDeck": rover_clearance.extract_combined_geometry(kit_mesh_map["Kit_TapeDeck"], head_inv),
            "FocusLens": rover_clearance.extract_combined_geometry(kit_mesh_map["FocusLens"], head_inv),
        }

        wing_geoms_world = []
        for frac in wing_fracs:
            apply_wing_pose(mech, frac)
            wing_geoms_world.append((frac, rover_clearance.extract_combined_geometry(wing_meshes, world_inv, dense_samples=True)))

        all_target_names = ["Head", "Neck", "Body", "Antenna", "Kit_BatterySpine", "Kit_TetherRing", "Kit_TapeDeck", "FocusLens", "Kit_CargoRack"]
        min_by_target = {k: 999.0 for k in all_target_names}
        overlaps_by_target = {k: 0 for k in all_target_names}

        for frac, w_geom in wing_geoms_world:
            for t_name, t_geom in static_targets.items():
                u_m, s_m, ov = rover_clearance.query_pair_clearance_fast(w_geom, t_geom, world_inv)
                if ov > 0:
                    overlaps_by_target[t_name] += 1
                min_by_target[t_name] = min(min_by_target[t_name], s_m * 1000.0)

        for yaw_deg in yaw_samples:
            rot_yaw = rover_clearance.unity_axis_angle_to_blender_matrix((0.0, 1.0, 0.0), yaw_deg)
            m_neck_world = m_neck_rest @ rot_yaw
            m_neck_world_inv = m_neck_world.inverted()
            for frac, w_geom in wing_geoms_world:
                u_m, s_m, ov = rover_clearance.query_pair_clearance_fast(w_geom, neck_base_geom, m_neck_world_inv)
                if ov > 0:
                    overlaps_by_target["Neck"] += 1
                min_by_target["Neck"] = min(min_by_target["Neck"], s_m * 1000.0)

            for nl_deg in lift_samples:
                rot_nl = rover_clearance.unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), nl_deg)
                m_lift_world = m_neck_world @ m_neck_to_lift_rest @ rot_nl
                for pitch_deg in pitch_samples:
                    rot_pitch = rover_clearance.unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), -pitch_deg)
                    m_head_world = m_lift_world @ m_lift_to_head_rest @ rot_pitch
                    m_head_world_inv = m_head_world.inverted()
                    for frac, w_geom in wing_geoms_world:
                        for t_name, t_geom in head_targets.items():
                            u_m, s_m, ov = rover_clearance.query_pair_clearance_fast(w_geom, t_geom, m_head_world_inv)
                            if ov > 0:
                                overlaps_by_target[t_name] += 1
                            min_by_target[t_name] = min(min_by_target[t_name], s_m * 1000.0)

        inter_half_mm = None
        if mech == "B":
            apply_wing_pose("B", 1.0)
            sw_l = bpy.data.objects.get("SolarWing_L")
            sw_r = bpy.data.objects.get("SolarWing_R")
            g_l = rover_clearance.extract_combined_geometry([sw_l], world_inv, dense_samples=True)
            g_r = rover_clearance.extract_combined_geometry([sw_r], world_inv)
            _, s_m, _ = rover_clearance.query_pair_clearance_fast(g_l, g_r, world_inv)
            inter_half_mm = round(s_m * 1000.0, 2)

        overall_min = round(min(min_by_target.values()), 2)
        results[mech] = {
            "overall_min_signed_mm": overall_min,
            "total_overlaps": sum(overlaps_by_target.values()),
            "inter_half_mm_at_full_open": inter_half_mm,
            "strip_render": str(strip_out.relative_to(root)),
            "targets_min_signed_mm": {k: round(v, 2) for k, v in min_by_target.items()},
            "targets_overlaps": overlaps_by_target,
        }
        print(f"Mechanism {mech}: min_signed={overall_min:.2f} mm, overlaps={sum(overlaps_by_target.values())}")
        for k, v in min_by_target.items():
            print(f"  {k:<18}: {v:7.2f} mm (overlaps={overlaps_by_target[k]})")

    out_json = root / "Logs" / "clearance_mechanisms.json"
    out_json.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(f"Saved mechanism comparison to {out_json}")
    return 0


def stitch_strips(root: Path) -> None:
    from PIL import Image, ImageDraw

    renders_dir = root / "Logs" / "renders"
    mech_titles = {
        "A": "Mechanism A: Trunk Lid (Head-side hinge, rear lifts 0-70 deg)",
        "B": "Mechanism B: Butterfly Halves (Spine hinges, open L/R 0-60 deg)",
        "C": "Mechanism C: Rear Mast Sky-Lift (Rear hinge + lift 0-65 deg)",
    }
    for mech in ("A", "B", "C"):
        strip_frames = []
        for idx, frac in enumerate((0.0, 1.0 / 3.0, 2.0 / 3.0, 1.0)):
            frame_path = renders_dir / f"_mech_{mech}_f{idx}.png"
            strip_frames.append((frac, Image.open(frame_path).convert("RGB")))
            frame_path.unlink(missing_ok=True)
        fw, fh = strip_frames[0][1].size
        header_h = 44
        strip_img = Image.new("RGB", (fw * 4, fh + header_h), (24, 28, 34))
        draw = ImageDraw.Draw(strip_img)
        for idx, (frac, im) in enumerate(strip_frames):
            strip_img.paste(im, (idx * fw, header_h))
            draw.text((idx * fw + 14, 14), f"{mech_titles[mech]}  |  Open {int(round(frac * 100))}%", fill=(235, 232, 225))
        strip_out = renders_dir / f"mechanism_{mech}_strip.png"
        strip_img.save(strip_out)
        print(f"Saved strip: {strip_out}")


def main() -> int:
    if "--in-blender" in sys.argv:
        return run_in_blender()
    root = worktree_root()
    cmd = [
        str(BLENDER),
        "-b",
        "--factory-startup",
        "--python-exit-code",
        "1",
        "-P",
        str(Path(__file__).resolve()),
        "--",
        "--in-blender",
    ]
    res = subprocess.run(cmd)
    if res.returncode != 0:
        return res.returncode
    stitch_strips(root)
    return 0


if __name__ == "__main__":
    sys.exit(main())
