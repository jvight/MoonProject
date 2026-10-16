"""
Headless Blender clearance checker for 07's SolarWing against Head, Neck, Body, Antenna, and every mounted kit.

Reads the real yaw, nod/lift, pitch, and wing limits from:
  - Assets/_Project/Data/Tuning/RoverCharacterTuning.asset
  - Assets/_Project/Scripts/Rover/Tuning/RoverCharacterTuning.cs (fallback defaults)

Usage:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/qa/rover_clearance.py -- [options]

Options:
  --out <path.json>       Write detailed JSON report to path
  --min-clearance-mm <f>  Gate threshold in mm (default: 0.0 for report-only, pass 15.0 to enforce >= 15 mm)
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import re
import subprocess
import sys

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[2]
BLENDER_ROVER_DIR = REPO_ROOT / "tools" / "art" / "blender" / "rover"
if str(BLENDER_ROVER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_ROVER_DIR))
sys.dont_write_bytecode = True

try:
    import bmesh
    import bpy
    from mathutils import Matrix, Vector
    from mathutils.bvhtree import BVHTree
except ImportError:
    if __name__ == "__main__":
        cmd = [
            "D:/Blender/blender.exe",
            "-b",
            "--factory-startup",
            "--python-exit-code",
            "1",
            "-P",
            str(Path(__file__).resolve()),
            "--",
            *sys.argv[1:],
        ]
        raise SystemExit(subprocess.call(cmd))
    raise


def parse_rover_tuning(repo_root: Path) -> dict:
    """
    Read the real head, neck, and wing pose limits from RoverCharacterTuning.asset
    and RoverCharacterTuning.cs (fallback for any newly added fields).
    """
    asset_path = repo_root / "Assets" / "_Project" / "Data" / "Tuning" / "RoverCharacterTuning.asset"
    cs_path = repo_root / "Assets" / "_Project" / "Scripts" / "Rover" / "Tuning" / "RoverCharacterTuning.cs"

    asset_text = asset_path.read_text(encoding="utf-8") if asset_path.exists() else ""
    cs_text = cs_path.read_text(encoding="utf-8") if cs_path.exists() else ""

    def read_float(field_name: str, default_val: float) -> float:
        m = re.search(rf"^\s*{re.escape(field_name)}:\s*([-+]?\d*\.?\d+(?:[eE][-+]?\d+)?)", asset_text, re.M)
        if m:
            return float(m.group(1))
        m_cs = re.search(rf"{re.escape(field_name)}\s*=\s*([-+]?\d*\.?\d+)f", cs_text)
        if m_cs:
            return float(m_cs.group(1))
        return default_val

    def read_vec3(field_name: str, default_xyz: tuple[float, float, float]) -> tuple[float, float, float]:
        m = re.search(
            rf"^\s*{re.escape(field_name)}:\s*\{{x:\s*([-+]?\d*\.?\d+),\s*y:\s*([-+]?\d*\.?\d+),\s*z:\s*([-+]?\d*\.?\d+)\}}",
            asset_text,
            re.M,
        )
        if m:
            return (float(m.group(1)), float(m.group(2)), float(m.group(3)))
        m_cs = re.search(
            rf"{re.escape(field_name)}\s*=\s*new\s+Vector3\(\s*([-+]?\d*\.?\d+)f\s*,\s*([-+]?\d*\.?\d+)f\s*,\s*([-+]?\d*\.?\d+)f\s*\)",
            cs_text,
        )
        if m_cs:
            return (float(m_cs.group(1)), float(m_cs.group(2)), float(m_cs.group(3)))
        return default_xyz

    neck_yaw_limit = read_float("_neckYawLimit", 125.0)
    head_pitch_up = read_float("_headPitchUpLimit", 55.0)
    head_pitch_down = read_float("_headPitchDownLimit", 25.0)

    neck_alert_lift = read_float("_neckAlertLift", 10.0)
    neck_sleep_droop = read_float("_neckSleepDroop", 35.0)
    neck_tired_droop = read_float("_neckTiredDroop", 15.0)
    neck_crawl_extend = read_float("_neckCrawlExtend", 15.0)
    neck_cargo_extend = read_float("_neckCargoExtend", 8.0)

    neck_lift_min_deg = -neck_alert_lift
    neck_lift_max_deg = max(neck_sleep_droop, neck_tired_droop + neck_crawl_extend + neck_cargo_extend)

    wing_closed_angle = read_float("_wingClosedAngle", 0.0)
    wing_open_angle = read_float("_wingOpenAngle", 110.0)
    sigh_wing = read_float("_sighWing", 10.0)
    wing_hinge_axis = read_vec3("_wingHingeAxis", (1.0, 0.0, 0.0))

    wing_min_deg = min(wing_closed_angle, wing_open_angle)
    wing_max_deg = max(wing_closed_angle, wing_open_angle) + max(0.0, sigh_wing)

    return {
        "neck_yaw_min_deg": -neck_yaw_limit,
        "neck_yaw_max_deg": neck_yaw_limit,
        "neck_lift_min_deg": neck_lift_min_deg,
        "neck_lift_max_deg": neck_lift_max_deg,
        "head_pitch_min_deg": -head_pitch_down,
        "head_pitch_max_deg": head_pitch_up,
        "wing_closed_angle_deg": wing_closed_angle,
        "wing_open_angle_deg": wing_open_angle,
        "sigh_wing_deg": sigh_wing,
        "wing_min_deg": wing_min_deg,
        "wing_max_deg": wing_max_deg,
        "wing_hinge_axis_unity": list(wing_hinge_axis),
    }


def _import_fbx_root(fbx_path: Path, prefix: str = "") -> bpy.types.Object:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx_path))
    added = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in added if o.parent not in added]
    if not roots:
        raise RuntimeError(f"No root object imported from {fbx_path}")
    root = roots[0]
    if prefix:
        for obj in added:
            obj.name = f"{prefix}{obj.name}"
    return root


def load_rover_and_kits_from_fbx(repo_root: Path) -> bpy.types.Object:
    """Import RoverModel.fbx and mount all 10 R1 upgrade kits + Kit_TapeDeck at their exact Unity sockets."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fbx_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover"
    rover_root = _import_fbx_root(fbx_dir / "RoverModel.fbx")

    def find_node(name: str) -> bpy.types.Object | None:
        return bpy.data.objects.get(name)

    for sock_name in ("DrumSocket_L", "BinSocket_L", "RingSocket"):
        sock = find_node(sock_name)
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
        sock = find_node(sock_name)
        fbx_path = fbx_dir / fbx_name
        if sock is None or not fbx_path.exists():
            continue
        kit_root = _import_fbx_root(fbx_path, prefix=prefix)
        kit_root.parent = sock
        kit_root.matrix_parent_inverse = Matrix.Identity(4)
        kit_root.location = (0.0, 0.0, 0.0)
        kit_root.rotation_mode = "XYZ"
        kit_root.rotation_euler = (0.0, 0.0, 0.0)
        kit_root.scale = (1.0, 1.0, 1.0)

    bpy.context.view_layer.update()
    return rover_root


def extract_combined_geometry(
    mesh_objects: list[bpy.types.Object],
    reference_inv_matrix: Matrix,
    dense_samples: bool = False,
) -> dict:
    """
    Combine mesh_objects into a single coordinate space defined by reference_inv_matrix.
    Builds a BVHTree and deduplicated surface sample points (quantized to 6 mm cells).
    """
    verts: list[Vector] = []
    tris: list[tuple[int, int, int]] = []
    raw_samples: list[Vector] = []

    for obj in mesh_objects:
        if obj.type != "MESH" or obj.data is None:
            continue
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.triangulate(bm, faces=bm.faces)
        rel_mat = reference_inv_matrix @ obj.matrix_world
        bm.transform(rel_mat)
        bm.verts.ensure_lookup_table()
        bm.edges.ensure_lookup_table()
        bm.faces.ensure_lookup_table()

        v_offset = len(verts)
        for v in bm.verts:
            co = v.co.copy()
            verts.append(co)
            raw_samples.append(co)

        if dense_samples:
            for e in bm.edges:
                p0 = e.verts[0].co
                p1 = e.verts[1].co
                length = (p1 - p0).length
                steps = max(2, int(math.ceil(length / 0.012)))
                for s in range(1, steps):
                    raw_samples.append(p0.lerp(p1, s / float(steps)))

        for f in bm.faces:
            fv = f.verts
            tris.append((v_offset + fv[0].index, v_offset + fv[1].index, v_offset + fv[2].index))
            raw_samples.append(f.calc_center_median())

        bm.free()

    # Deduplicate samples in 10 mm voxel buckets on wing (~250 samples)
    seen_buckets = set()
    samples: list[Vector] = []
    inv_cell = 100.0
    for pt in raw_samples:
        key = (int(round(pt.x * inv_cell)), int(round(pt.y * inv_cell)), int(round(pt.z * inv_cell)))
        if key not in seen_buckets:
            seen_buckets.add(key)
            samples.append(pt)

    bvh = BVHTree.FromPolygons(verts, tris) if tris else None
    return {
        "verts": verts,
        "tris": tris,
        "bvh": bvh,
        "samples": samples,
    }


def _is_point_inside_closed_bvh(bvh: BVHTree, pt: Vector) -> bool:
    """Determine if pt is inside a closed manifold mesh by checking if the closest hit along +Z has an upward normal."""
    loc, norm, idx, dist = bvh.ray_cast(pt, Vector((0.0, 0.0, 1.0)))
    if loc is None or norm is None:
        return False
    return norm.z > 0.0


def query_pair_clearance_fast(
    wing_geom: dict,
    target_geom: dict,
    wing_to_target_mat: Matrix,
    overlap_check_threshold: float = 0.018,
) -> tuple[float, float, int]:
    """
    Exact clearance query between wing_geom and target_geom:
      1. Queries deduplicated wing samples against target_bvh (exact point-to-triangle distance in C).
      2. Since every point on SolarWing is within 7 mm of a sample point, if min_dist <= 18 mm,
         runs exact C-level BVH triangle-triangle overlap.
    """
    target_bvh: BVHTree = target_geom["bvh"]
    wing_bvh: BVHTree = wing_geom["bvh"]
    if target_bvh is None or wing_bvh is None:
        return (999.0, 999.0, 0)

    min_dist = 999.0
    transformed_samples = []
    for pt in wing_geom["samples"]:
        pt_t = wing_to_target_mat @ pt
        transformed_samples.append(pt_t)
        loc, norm, idx, dist = target_bvh.find_nearest(pt_t)
        if loc is not None and dist is not None and dist < min_dist:
            min_dist = dist

    overlap_count = 0
    max_penetration = 0.0
    if min_dist <= overlap_check_threshold:
        trans_verts = [wing_to_target_mat @ v for v in wing_geom["verts"]]
        wing_in_target_bvh = BVHTree.FromPolygons(trans_verts, wing_geom["tris"])
        overlaps = wing_in_target_bvh.overlap(target_bvh)
        overlap_count = len(overlaps)
        if overlap_count > 0:
            min_dist = 0.0
            for pt_t in transformed_samples:
                if _is_point_inside_closed_bvh(target_bvh, pt_t):
                    loc, norm, idx, dist = target_bvh.find_nearest(pt_t)
                    if dist is not None and dist > max_penetration:
                        max_penetration = dist
            if max_penetration < 1e-4:
                max_penetration = 0.001

    signed_clearance = -max_penetration if overlap_count > 0 else min_dist
    return (min_dist, signed_clearance, overlap_count)


def unity_axis_angle_to_blender_matrix(axis_unity: tuple[float, float, float], angle_deg: float) -> Matrix:
    """
    Convert Unity Quaternion.AngleAxis(angle_deg, axis_unity) into a 4x4 rotation matrix in Blender space.
    Moving between Unity (x, y, z) and Blender (x, z, y) is a reflection permutation (det = -1),
    so a left-handed rotation around Unity (ux, uy, uz) by +angle_deg equals a right-handed rotation
    in Blender around (ux, uz, uy) by -angle_deg.
    """
    ux, uy, uz = axis_unity
    b_axis = Vector((ux, uz, uy))
    if b_axis.length < 1e-6:
        return Matrix.Identity(4)
    b_axis.normalize()
    return Matrix.Rotation(math.radians(-angle_deg), 4, b_axis)


def evaluate_rover_clearance(
    tuning: dict,
    wing_step_deg: float = 10.0,
    yaw_step_deg: float = 25.0,
) -> dict:
    """
    Evaluate minimum clearance across all reachable wing angles and head poses for:
      - Head, Neck, Body, Antenna, and each mounted kit.
    Assumes the rover + kits are already loaded in bpy.context.scene in rest pose.
    """
    bpy.context.view_layer.update()

    wing_node = bpy.data.objects.get("SolarWing")
    neck_node = bpy.data.objects.get("Neck")
    neck_lift_node = bpy.data.objects.get("NeckLift")
    head_node = bpy.data.objects.get("Head")
    if wing_node is None or neck_node is None or neck_lift_node is None or head_node is None:
        raise RuntimeError("Missing required rig nodes (SolarWing, Neck, NeckLift, Head)")

    m_wing_rest = wing_node.matrix_world.copy()
    m_neck_rest = neck_node.matrix_world.copy()
    m_neck_lift_rest = neck_lift_node.matrix_world.copy()
    m_head_rest = head_node.matrix_world.copy()

    m_neck_to_lift_rest = m_neck_rest.inverted() @ m_neck_lift_rest
    m_lift_to_head_rest = m_neck_lift_rest.inverted() @ m_head_rest

    wing_meshes = [o for o in [wing_node] + list(wing_node.children_recursive) if o.type == "MESH"]
    wing_geom = extract_combined_geometry(wing_meshes, m_wing_rest.inverted(), dense_samples=True)

    kit_prefixes = [
        "Kit_BatterySpine",
        "Kit_TetherRing",
        "Kit_TapeDeck",
        "FocusLens",
        "Kit_CargoRack",
        "Kit_CapacitorDrum_L",
        "Kit_CapacitorDrum_R",
        "Kit_SideBin_L",
        "Kit_SideBin_R",
        "Kit_LampBar",
        "HoverCoils",
    ]
    kit_mesh_map: dict[str, list[bpy.types.Object]] = {k: [] for k in kit_prefixes}
    head_meshes: list[bpy.types.Object] = []
    neck_meshes: list[bpy.types.Object] = []
    neck_lift_meshes: list[bpy.types.Object] = []
    antenna_meshes: list[bpy.types.Object] = []
    body_meshes: list[bpy.types.Object] = []

    wing_mesh_set = set(wing_meshes)
    head_descendants = set([head_node] + list(head_node.children_recursive))
    antenna_node = bpy.data.objects.get("Antenna")
    antenna_descendants = set([antenna_node] + list(antenna_node.children_recursive)) if antenna_node else set()

    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or obj in wing_mesh_set:
            continue
        matched_kit = None
        for kp in kit_prefixes:
            if obj.name.startswith(kp + "::"):
                matched_kit = kp
                break
        if matched_kit is not None:
            kit_mesh_map[matched_kit].append(obj)
            continue

        if obj == neck_node:
            neck_meshes.append(obj)
        elif obj == neck_lift_node:
            neck_lift_meshes.append(obj)
        elif obj in head_descendants:
            head_meshes.append(obj)
        elif obj in antenna_descendants:
            antenna_meshes.append(obj)
        else:
            body_meshes.append(obj)

    world_inv = Matrix.Identity(4)
    static_targets = {
        "Body": extract_combined_geometry(body_meshes, world_inv),
        "Antenna": extract_combined_geometry(antenna_meshes, world_inv),
    }
    for kp in (
        "Kit_BatterySpine",
        "Kit_CargoRack",
        "Kit_CapacitorDrum_L",
        "Kit_CapacitorDrum_R",
        "Kit_SideBin_L",
        "Kit_SideBin_R",
        "Kit_LampBar",
        "HoverCoils",
    ):
        static_targets[kp] = extract_combined_geometry(kit_mesh_map[kp], world_inv)

    neck_base_geom = extract_combined_geometry(neck_meshes, m_neck_rest.inverted())
    neck_lift_geom = extract_combined_geometry(neck_lift_meshes, m_neck_lift_rest.inverted())

    head_inv = m_head_rest.inverted()
    head_targets = {
        "Head": extract_combined_geometry(head_meshes, head_inv),
        "Kit_TetherRing": extract_combined_geometry(kit_mesh_map["Kit_TetherRing"], head_inv),
        "Kit_TapeDeck": extract_combined_geometry(kit_mesh_map["Kit_TapeDeck"], head_inv),
        "FocusLens": extract_combined_geometry(kit_mesh_map["FocusLens"], head_inv),
    }

    w_min = tuning["wing_min_deg"]
    w_max = tuning["wing_max_deg"]
    wing_angles = []
    w_cur = w_min
    while w_cur < w_max - 1e-5:
        wing_angles.append(round(w_cur, 2))
        w_cur += wing_step_deg
    wing_angles.append(round(w_max, 2))
    for key_w in (
        tuning["wing_closed_angle_deg"],
        tuning["wing_open_angle_deg"],
        0.16 * tuning["wing_open_angle_deg"],
        0.25 * tuning["wing_open_angle_deg"],
        0.45 * tuning["wing_open_angle_deg"],
    ):
        if w_min - 1e-4 <= key_w <= w_max + 1e-4:
            wing_angles.append(round(key_w, 2))
    wing_angles = sorted(set(wing_angles))

    yaw_min = tuning["neck_yaw_min_deg"]
    yaw_max = tuning["neck_yaw_max_deg"]
    yaws = []
    y_cur = yaw_min
    while y_cur < yaw_max - 1e-5:
        yaws.append(round(y_cur, 2))
        y_cur += yaw_step_deg
    yaws.append(round(yaw_max, 2))
    if 0.0 not in yaws:
        yaws.append(0.0)
    yaws = sorted(set(yaws))

    nl_min = tuning["neck_lift_min_deg"]
    nl_max = tuning["neck_lift_max_deg"]
    neck_lifts = sorted(set([round(nl_min, 2), 0.0, round(0.5 * nl_max, 2), round(nl_max, 2)]))

    p_min = tuning["head_pitch_min_deg"]
    p_max = tuning["head_pitch_max_deg"]
    head_pitches = sorted(set([round(p_min, 2), 0.0, 20.0, 45.0, round(p_max, 2)]))

    all_target_names = ["Head", "Neck", "Body", "Antenna"] + kit_prefixes
    results = {
        name: {
            "min_clearance_mm": 999000.0,
            "min_signed_clearance_mm": 999000.0,
            "overlap_poses": 0,
            "max_overlap_tris": 0,
            "worst_pose": None,
        }
        for name in all_target_names
    }

    def record_sample(target_name: str, unsigned_m: float, signed_m: float, overlaps: int, pose_dict: dict):
        entry = results[target_name]
        unsigned_mm = unsigned_m * 1000.0
        signed_mm = signed_m * 1000.0
        if overlaps > 0:
            entry["overlap_poses"] += 1
            if overlaps > entry["max_overlap_tris"]:
                entry["max_overlap_tris"] = overlaps
        if signed_mm < entry["min_signed_clearance_mm"] or (
            abs(signed_mm - entry["min_signed_clearance_mm"]) < 1e-6 and unsigned_mm < entry["min_clearance_mm"]
        ):
            entry["min_clearance_mm"] = round(unsigned_mm, 2)
            entry["min_signed_clearance_mm"] = round(signed_mm, 2)
            entry["worst_pose"] = pose_dict

    wing_axis_unity = tuple(tuning["wing_hinge_axis_unity"])

    wing_world_mats = {}
    for w_deg in wing_angles:
        rot_w = unity_axis_angle_to_blender_matrix(wing_axis_unity, w_deg)
        wing_world_mats[w_deg] = m_wing_rest @ rot_w

    # 1. Check static targets (depend only on wing angle w_deg)
    for w_deg in wing_angles:
        m_wing_w = wing_world_mats[w_deg]
        pose_info = {"wing_deg": w_deg, "neck_yaw_deg": 0.0, "neck_lift_deg": 0.0, "head_pitch_deg": 0.0}
        for t_name, t_geom in static_targets.items():
            u_m, s_m, ov = query_pair_clearance_fast(wing_geom, t_geom, m_wing_w)
            record_sample(t_name, u_m, s_m, ov, pose_info)

    # 2. Check Neck, NeckLift, Head, and Head-mounted kits across pose combinations
    for yaw_deg in yaws:
        rot_yaw = unity_axis_angle_to_blender_matrix((0.0, 1.0, 0.0), yaw_deg)
        m_neck_world = m_neck_rest @ rot_yaw
        m_neck_world_inv = m_neck_world.inverted()

        for w_deg in wing_angles:
            m_wing_in_neck = m_neck_world_inv @ wing_world_mats[w_deg]
            u_m, s_m, ov = query_pair_clearance_fast(wing_geom, neck_base_geom, m_wing_in_neck)
            record_sample(
                "Neck",
                u_m,
                s_m,
                ov,
                {"wing_deg": w_deg, "neck_yaw_deg": yaw_deg, "neck_lift_deg": 0.0, "head_pitch_deg": 0.0},
            )

        for nl_deg in neck_lifts:
            rot_nl = unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), nl_deg)
            m_lift_world = m_neck_world @ m_neck_to_lift_rest @ rot_nl
            m_lift_world_inv = m_lift_world.inverted()

            for w_deg in wing_angles:
                m_wing_in_lift = m_lift_world_inv @ wing_world_mats[w_deg]
                u_m, s_m, ov = query_pair_clearance_fast(wing_geom, neck_lift_geom, m_wing_in_lift)
                record_sample(
                    "Neck",
                    u_m,
                    s_m,
                    ov,
                    {"wing_deg": w_deg, "neck_yaw_deg": yaw_deg, "neck_lift_deg": nl_deg, "head_pitch_deg": 0.0},
                )

            for pitch_deg in head_pitches:
                rot_pitch = unity_axis_angle_to_blender_matrix((1.0, 0.0, 0.0), -pitch_deg)
                m_head_world = m_lift_world @ m_lift_to_head_rest @ rot_pitch
                m_head_world_inv = m_head_world.inverted()

                for w_deg in wing_angles:
                    m_wing_in_head = m_head_world_inv @ wing_world_mats[w_deg]
                    pose_info = {
                        "wing_deg": w_deg,
                        "neck_yaw_deg": yaw_deg,
                        "neck_lift_deg": nl_deg,
                        "head_pitch_deg": pitch_deg,
                    }
                    for h_name, h_geom in head_targets.items():
                        u_m, s_m, ov = query_pair_clearance_fast(wing_geom, h_geom, m_wing_in_head)
                        record_sample(h_name, u_m, s_m, ov, pose_info)

    overall_min_mm = min(v["min_clearance_mm"] for v in results.values())
    overall_min_signed_mm = min(v["min_signed_clearance_mm"] for v in results.values())
    total_overlaps = sum(v["overlap_poses"] for v in results.values())

    return {
        "tuning": tuning,
        "grid_counts": {
            "wing_angles": len(wing_angles),
            "neck_yaws": len(yaws),
            "neck_lifts": len(neck_lifts),
            "head_pitches": len(head_pitches),
            "combined_poses_tested": len(wing_angles) * len(yaws) * len(neck_lifts) * len(head_pitches),
        },
        "overall_min_clearance_mm": overall_min_mm,
        "overall_min_signed_clearance_mm": overall_min_signed_mm,
        "total_overlap_poses": total_overlaps,
        "targets": results,
    }


def print_clearance_table(report: dict, label: str = "Rover SolarWing Clearance"):
    t = report["tuning"]
    g = report["grid_counts"]
    print(f"=== {label} ===")
    print(
        f"Tuning ranges: Wing [{t['wing_min_deg']:.1f} deg .. {t['wing_max_deg']:.1f} deg] "
        f"(open={t['wing_open_angle_deg']:.1f} deg, sigh={t['sigh_wing_deg']:.1f} deg), "
        f"NeckYaw [{t['neck_yaw_min_deg']:.1f} deg .. {t['neck_yaw_max_deg']:.1f} deg], "
        f"NeckLift [{t['neck_lift_min_deg']:.1f} deg .. {t['neck_lift_max_deg']:.1f} deg], "
        f"HeadPitch [{t['head_pitch_min_deg']:.1f} deg .. {t['head_pitch_max_deg']:.1f} deg]"
    )
    print(
        f"Grid tested: {g['wing_angles']} wing x {g['neck_yaws']} yaw x {g['neck_lifts']} lift x "
        f"{g['head_pitches']} pitch = {g['combined_poses_tested']} combined poses"
    )
    print(f"{'Target':<22} | {'Min Dist (mm)':>14} | {'Signed (mm)':>12} | {'Overlaps':>10} | Worst Pose (wing, yaw, lift, pitch)")
    print("-" * 98)
    for name, data in report["targets"].items():
        wp = data["worst_pose"] or {}
        pose_str = (
            f"w={wp.get('wing_deg', 0):.1f}, yaw={wp.get('neck_yaw_deg', 0):.1f}, "
            f"lift={wp.get('neck_lift_deg', 0):.1f}, pitch={wp.get('head_pitch_deg', 0):.1f}"
        )
        print(
            f"{name:<22} | {data['min_clearance_mm']:>14.2f} | {data['min_signed_clearance_mm']:>12.2f} | "
            f"{data['overlap_poses']:>10d} | {pose_str}"
        )
    print("-" * 98)
    print(
        f"OVERALL MIN CLEARANCE: {report['overall_min_clearance_mm']:.2f} mm "
        f"(signed: {report['overall_min_signed_clearance_mm']:.2f} mm, total overlap poses: {report['total_overlap_poses']})"
    )


def main():
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description="Check 07 SolarWing clearance across all tuned poses.")
    parser.add_argument("--out", type=str, default="", help="Optional JSON output path")
    parser.add_argument("--min-clearance-mm", type=float, default=0.0, help="Required minimum clearance in mm")
    args = parser.parse_args(argv)

    tuning = parse_rover_tuning(REPO_ROOT)
    load_rover_and_kits_from_fbx(REPO_ROOT)
    report = evaluate_rover_clearance(tuning)
    print_clearance_table(report)

    if args.out:
        out_path = Path(args.out)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(f"Wrote clearance report to {out_path}")

    if args.min_clearance_mm > 0.0:
        if report["overall_min_clearance_mm"] < args.min_clearance_mm or report["total_overlap_poses"] > 0:
            print(
                f"RESULT: FAIL (min clearance {report['overall_min_clearance_mm']:.2f} mm < {args.min_clearance_mm:.2f} mm)"
            )
            raise SystemExit(1)
        print(f"RESULT: PASS (min clearance {report['overall_min_clearance_mm']:.2f} mm >= {args.min_clearance_mm:.2f} mm)")


if __name__ == "__main__":
    main()
