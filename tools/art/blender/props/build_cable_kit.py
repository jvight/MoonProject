"""Generate the R1 Ground Power-Cable Kit props (`Assets/_Project/Art/R1/Models/Props/`):
- Deliverable 1:
  * Cable_Straight_2m.fbx
  * Cable_Bend_45.fbx
  * Cable_Bend_90.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_cable_kit.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Sequence, Tuple

import bpy
from mathutils import Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"

# Heavy insulated power cable Ø ~5 cm (radius 0.025 m), lying on regolith slightly sunk
# (centerline at y_u = 0.019 m -> bottom at -0.006 m sunk into regolith, top crown at +0.044 m).
CABLE_RADIUS = 0.025
CABLE_CENTER_Y = 0.019


def ensure_extended_r1_materials() -> None:
    """Register R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime."""
    extra_specs = {
        "R1_Screen": {
            "base_color": r1kit._srgb_to_linear("#1A140E"),
            "wear_color": r1kit._srgb_to_linear("#34281C"),
            "dust_color": r1kit._srgb_to_linear("#5E564C"),
            "metallic": 0.05,
            "roughness": 0.12,
            "emission": r1kit._srgb_to_linear("#FFB24A"),
            "emission_strength": 1.0,
        },
        "R1_Brass": {
            "base_color": r1kit._srgb_to_linear("#DBB052"),
            "wear_color": r1kit._srgb_to_linear("#F7D98C"),
            "dust_color": r1kit._srgb_to_linear("#8A8270"),
            "metallic": 0.92,
            "roughness": 0.24,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_Signage": {
            "base_color": r1kit._srgb_to_linear("#EDE3CE"),
            "wear_color": r1kit._srgb_to_linear("#464952"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.04,
            "roughness": 0.36,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
    }
    allowed = list(r1kit.ALLOWED_MATERIALS)
    for name, spec in extra_specs.items():
        if name not in allowed:
            allowed.append(name)
        r1kit.MATERIAL_SPECS[name] = spec
    r1kit.ALLOWED_MATERIALS = tuple(allowed)


def _add_coupling_collar(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    yaw_deg: float = 0.0,
    pitch_deg: float = 0.0,
) -> None:
    """Add an R1 industrial cable coupling collar (grey metal sleeve, steel lock ring, orange band, brass index lug)."""
    cx, cy, cz = center_u
    rot = (pitch_deg, yaw_deg, 0.0)
    # Main machined grey-metal coupling sleeve
    mb.add_cylinder(
        (cx, cy, cz),
        radius=0.030,
        length=0.068,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=12,
        bevel=0.003,
        rot_deg_u=rot,
    )
    # Bare-steel bayonet lock collar
    mb.add_cylinder(
        (cx, cy, cz),
        radius=0.0325,
        length=0.026,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=12,
        bevel=0.002,
        rot_deg_u=rot,
    )
    # Warm-orange identification stripe band on inboard side of collar
    rad_yaw = math.radians(yaw_deg)
    inboard_offset = -0.026
    ox = cx + math.sin(rad_yaw) * inboard_offset
    oz = cz + math.cos(rad_yaw) * inboard_offset
    mb.add_cylinder(
        (ox, cy, oz),
        radius=0.031,
        length=0.016,
        mat="R1_OrangeStripe",
        axis_u="Z",
        segments=12,
        bevel=0.002,
        rot_deg_u=rot,
    )


def _add_armor_rib_ring(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    yaw_deg: float = 0.0,
    mat: str = "R1_Rubber",
) -> None:
    """Add a raised elastomer/metal wear ring around the Ø 5 cm cable so the silhouette reads as heavy armored conduit."""
    mb.add_cylinder(
        center_u,
        radius=0.0285,
        length=0.024,
        mat=mat,
        axis_u="Z",
        segments=12,
        bevel=0.0025,
        rot_deg_u=(0.0, yaw_deg, 0.0),
    )


def _add_regolith_stake_clip(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    yaw_deg: float = 0.0,
) -> None:
    """Build an R1 cable hold-down clip & regolith stake at `center_u` oriented at `yaw_deg` (around Unity Y)."""
    cx, _, cz = center_u
    rot = (0.0, yaw_deg, 0.0)
    rad = math.radians(yaw_deg)
    # Local right (+X_local) and forward (+Z_local) unit vectors in Unity XZ plane
    rx, rz = math.cos(rad), -math.sin(rad)

    # 1. Stamped grey-metal regolith baseplate (slightly sunk into regolith from y = -0.004 to +0.016 m)
    mb.add_bevel_box(
        (cx, 0.006, cz),
        (0.168, 0.020, 0.116),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
        rot_deg_u=rot,
    )

    # 2. Arched cream-enamel saddle clamp bridging over the Ø 5 cm cable
    mb.add_bevel_box(
        (cx, 0.031, cz),
        (0.088, 0.044, 0.092),
        mat="R1_CreamEnamel",
        bevel=0.006,
        segments=2,
        rot_deg_u=rot,
        top_scale_xz=(0.82, 0.90),
    )

    # 3. High-visibility orange keeper strap across top of saddle + brass inspection stud
    mb.add_bevel_box(
        (cx, 0.054, cz),
        (0.072, 0.010, 0.042),
        mat="R1_OrangeStripe",
        bevel=0.002,
        segments=1,
        rot_deg_u=rot,
    )
    mb.add_cylinder(
        (cx, 0.061, cz),
        radius=0.008,
        length=0.006,
        mat="R1_Brass",
        axis_u="Y",
        segments=8,
        bevel=0.001,
    )

    # 4. Left & right driven regolith stake pins (sunk to y = -0.016 m) + hex hold-down bolts on flanges
    for side in (-1.0, 1.0):
        sx = cx + side * 0.062 * rx
        sz = cz + side * 0.062 * rz
        # Driven stake pin body extending into regolith (y = -0.016 .. +0.024 m)
        mb.add_cylinder(
            (sx, 0.004, sz),
            radius=0.011,
            length=0.040,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.002,
            radius_top=0.014,
        )
        # Washer collar + 6-sided hex bolt head on top of flange (y = 0.016 .. 0.032 m)
        mb.add_cylinder(
            (sx, 0.020, sz),
            radius=0.016,
            length=0.008,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
        )
        mb.add_cylinder(
            (sx, 0.027, sz),
            radius=0.010,
            length=0.010,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.0015,
        )


def apply_cable_dust_uvs_and_colors(
    root: bpy.types.Object,
    footprint_radius: float = 0.45,
    extra_top_dust: float = 0.34,
) -> None:
    """Project UVs so a dust layer reads cleanly on top and bake regolith-sunk + top-crown vertex colors."""
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)

    # Refine UVs on cable meshes so U runs along the horizontal path and V maps bottom (0.0) -> top crown (1.0)
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        if not mesh.uv_layers:
            mesh.uv_layers.new(name="UVMap")
        uv_layer = mesh.uv_layers.active.data
        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        if obj.name in ("CableRun", "CableStub", "CableLead"):
            for poly in mesh.polygons:
                n_w = (rot_mat @ poly.normal).normalized()
                for li in poly.loop_indices:
                    v_w = world_mat @ mesh.vertices[mesh.loops[li].vertex_index].co
                    # Horizontal path coordinate along X+Y in Blender (X+Z in Unity)
                    u_coord = (v_w.x + v_w.y) * 1.25
                    # Vertical coordinate mapped so top of cable (z_w ~ 0.044, n_w.z > 0) sits cleanly in upper V band
                    h_norm = max(0.0, min(1.0, (v_w.z + 0.010) / 0.060))
                    up_bias = max(0.0, min(1.0, 0.5 + 0.5 * n_w.z))
                    v_coord = 0.10 + 0.85 * (0.55 * h_norm + 0.45 * up_bias)
                    uv_layer[li].uv = (u_coord, v_coord)

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)

    # Shape vertex colors (R=AO, G=Wear, B=Dust) so regolith dust reads clearly on the top crown and sunk base
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or "Col" not in obj.data.color_attributes:
            continue
        mesh = obj.data
        attr = mesh.color_attributes["Col"]
        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        for poly in mesh.polygons:
            n_w = (rot_mat @ poly.normal).normalized()
            mat_name = mesh.materials[poly.material_index].name if poly.material_index < len(mesh.materials) and mesh.materials[poly.material_index] else ""
            is_rubber = mat_name == "R1_Rubber"
            for li in poly.loop_indices:
                v_w = world_mat @ mesh.vertices[mesh.loops[li].vertex_index].co
                r, g, b, a = attr.data[li].color

                # Ground-contact AO darkening where the cable sinks into the regolith (v_w.z <= 0.012 m)
                sunk_factor = max(0.0, min(1.0, (0.018 - v_w.z) / 0.024))
                # Upward-facing crown factor (n_w.z > 0 in Blender = normalWS.y > 0 in Unity)
                top_factor = max(0.0, min(1.0, (n_w.z - 0.15) / 0.75))

                r_new = max(0.16, min(1.0, r * (0.90 - 0.28 * sunk_factor)))
                # Keep G (edge wear) low on rubber top crown so R1LitCore doesn't suppress top dustMask
                if is_rubber:
                    g_new = max(0.0, min(0.18, g * (1.0 - 0.75 * top_factor)))
                else:
                    g_new = max(0.0, min(1.0, g * 1.15))

                dust_boost = extra_top_dust * max(top_factor, 0.80 * sunk_factor)
                b_new = max(0.0, min(1.0, b + dust_boost))
                attr.data[li].color = (r_new, g_new, b_new, a)


def build_cable_straight_2m() -> bpy.types.Object:
    """Build `Cable_Straight_2m.fbx`: 2.0 m heavy Ø 5 cm power cable slightly sunk in regolith with stake clip."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Cable_Straight_2m", pivot_u=(0.0, 0.0, 0.0))

    # 1. Heavy Ø 5 cm insulated cable run from z = -1.00 m to z = +1.00 m
    cable = r1kit.MeshBuilder("CableRun", pivot_u=(0.0, 0.0, 0.0))
    pts: List[Tuple[float, float, float]] = []
    n_steps = 20
    for i in range(n_steps + 1):
        t = i / n_steps
        z = -1.00 + 2.00 * t
        # Subtle settled S-curve with zero offset and zero slope at t = 0, 0.5, 1.0
        meander_x = 0.012 * math.sin(2.0 * math.pi * t) * math.sin(math.pi * t)
        pts.append((meander_x, CABLE_CENTER_Y, z))

    cable.add_tube_path(pts, radius=CABLE_RADIUS, mat="R1_Rubber", segments=12)

    # End coupling collars at z = -0.962 m and z = +0.962 m (staying within [-1.00, +1.00] m)
    _add_coupling_collar(cable, (0.0, CABLE_CENTER_Y, -0.962), yaw_deg=0.0)
    _add_coupling_collar(cable, (0.0, CABLE_CENTER_Y, 0.962), yaw_deg=180.0)

    # Intermediate ribbed armor rings along the 2 m span
    for t_rib, mat_rib in ((0.22, "R1_Rubber"), (0.36, "R1_GreyMetal"), (0.64, "R1_GreyMetal"), (0.78, "R1_Rubber")):
        z_rib = -1.00 + 2.00 * t_rib
        x_rib = 0.012 * math.sin(2.0 * math.pi * t_rib) * math.sin(math.pi * t_rib)
        _add_armor_rib_ring(cable, (x_rib, CABLE_CENTER_Y, z_rib), yaw_deg=0.0, mat=mat_rib)

    cable.build_object(parent=root)

    # 2. Cable clip / regolith stake at the 2 m interval center (0, 0, 0)
    stake = r1kit.MeshBuilder("StakeClip", pivot_u=(0.0, 0.0, 0.0))
    _add_regolith_stake_clip(stake, (0.0, 0.0, 0.0), yaw_deg=0.0)
    stake.build_object(parent=root)

    # Child anchors
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, CABLE_CENTER_Y, -1.00))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, CABLE_CENTER_Y, 1.00))
    r1kit.create_empty("Clip_Anchor", parent=root, pivot_u=(0.0, 0.064, 0.0))

    apply_cable_dust_uvs_and_colors(root, footprint_radius=0.35)

    fbx_path = PROPS_DIR / "Cable_Straight_2m.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _sample_bend_arc(
    bend_deg: float,
    arc_radius: float,
    lead_len: float,
    arc_steps: int = 16,
) -> Tuple[List[Tuple[float, float, float]], Tuple[float, float, float], float]:
    """Generate a centered horizontal bend path in Unity XZ with straight lead-ins and return (points, mid_pt, mid_yaw)."""
    bend_rad = math.radians(bend_deg)
    # Before centering: start at (0, y, 0) heading +Z; straight lead_len, then arc of radius R turning +X, then lead_len.
    raw_pts: List[Tuple[float, float]] = [(0.0, 0.0), (0.0, lead_len * 0.5)]
    cx_arc = arc_radius
    cz_arc = lead_len
    for i in range(arc_steps + 1):
        th = bend_rad * (i / arc_steps)
        px = cx_arc - arc_radius * math.cos(th)
        pz = cz_arc + arc_radius * math.sin(th)
        raw_pts.append((px, pz))

    end_arc_x, end_arc_z = raw_pts[-1]
    dir_x = math.sin(bend_rad)
    dir_z = math.cos(bend_rad)
    raw_pts.append((end_arc_x + dir_x * (lead_len * 0.5), end_arc_z + dir_z * (lead_len * 0.5)))
    raw_pts.append((end_arc_x + dir_x * lead_len, end_arc_z + dir_z * lead_len))

    # Midpoint of the arc (th = bend_rad * 0.5)
    mid_th = bend_rad * 0.5
    mid_raw_x = cx_arc - arc_radius * math.cos(mid_th)
    mid_raw_z = cz_arc + arc_radius * math.sin(mid_th)

    # Center the path around the bounding-box center so root (0,0,0) sits cleanly under the piece
    xs = [p[0] for p in raw_pts]
    zs = [p[1] for p in raw_pts]
    off_x = 0.5 * (min(xs) + max(xs))
    off_z = 0.5 * (min(zs) + max(zs))

    pts_u = [(px - off_x, CABLE_CENTER_Y, pz - off_z) for px, pz in raw_pts]
    mid_u = (mid_raw_x - off_x, 0.0, mid_raw_z - off_z)
    return pts_u, mid_u, bend_deg * 0.5


def build_cable_bend_45() -> bpy.types.Object:
    """Build `Cable_Bend_45.fbx`: 45-degree heavy Ø 5 cm power cable bend with regolith stake clip."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Cable_Bend_45", pivot_u=(0.0, 0.0, 0.0))

    pts_u, mid_u, mid_yaw = _sample_bend_arc(bend_deg=45.0, arc_radius=1.10, lead_len=0.18, arc_steps=14)
    start_u = pts_u[0]
    end_u = pts_u[-1]

    cable = r1kit.MeshBuilder("CableRun", pivot_u=(0.0, 0.0, 0.0))
    cable.add_tube_path(pts_u, radius=CABLE_RADIUS, mat="R1_Rubber", segments=12)

    # Start collar (yaw = 0 deg, inset 0.038 m from start_u along +Z)
    _add_coupling_collar(cable, (start_u[0], CABLE_CENTER_Y, start_u[2] + 0.038), yaw_deg=0.0)
    # End collar (yaw = 45 deg, inset 0.038 m from end_u along -45 deg tangent)
    rad45 = math.radians(45.0)
    _add_coupling_collar(
        cable,
        (end_u[0] - math.sin(rad45) * 0.038, CABLE_CENTER_Y, end_u[2] - math.cos(rad45) * 0.038),
        yaw_deg=225.0,
    )

    # Ribbed armor rings at 1/4 and 3/4 along the arc
    for idx_r in (4, len(pts_u) - 5):
        p_prev = pts_u[idx_r - 1]
        p_cur = pts_u[idx_r]
        p_next = pts_u[idx_r + 1]
        yaw_r = math.degrees(math.atan2(p_next[0] - p_prev[0], p_next[2] - p_prev[2]))
        _add_armor_rib_ring(cable, p_cur, yaw_deg=yaw_r, mat="R1_GreyMetal")

    cable.build_object(parent=root)

    # Regolith stake clip at the apex of the 45° bend
    stake = r1kit.MeshBuilder("StakeClip", pivot_u=(0.0, 0.0, 0.0))
    _add_regolith_stake_clip(stake, mid_u, yaw_deg=mid_yaw)
    stake.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=start_u)
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=end_u)
    r1kit.create_empty("Clip_Anchor", parent=root, pivot_u=(mid_u[0], 0.064, mid_u[2]))

    apply_cable_dust_uvs_and_colors(root, footprint_radius=0.35)

    fbx_path = PROPS_DIR / "Cable_Bend_45.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_cable_bend_90() -> bpy.types.Object:
    """Build `Cable_Bend_90.fbx`: 90-degree heavy Ø 5 cm power cable bend with regolith stake clip."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Cable_Bend_90", pivot_u=(0.0, 0.0, 0.0))

    pts_u, mid_u, mid_yaw = _sample_bend_arc(bend_deg=90.0, arc_radius=0.85, lead_len=0.18, arc_steps=18)
    start_u = pts_u[0]
    end_u = pts_u[-1]

    cable = r1kit.MeshBuilder("CableRun", pivot_u=(0.0, 0.0, 0.0))
    cable.add_tube_path(pts_u, radius=CABLE_RADIUS, mat="R1_Rubber", segments=12)

    # Start collar (yaw = 0 deg, inset 0.038 m from start_u along +Z)
    _add_coupling_collar(cable, (start_u[0], CABLE_CENTER_Y, start_u[2] + 0.038), yaw_deg=0.0)
    # End collar (yaw = 90 deg, inset 0.038 m from end_u along -X)
    _add_coupling_collar(cable, (end_u[0] - 0.038, CABLE_CENTER_Y, end_u[2]), yaw_deg=270.0)

    # Ribbed armor rings flanking the 90° bend
    for idx_r, mat_r in ((5, "R1_GreyMetal"), (8, "R1_Rubber"), (len(pts_u) - 9, "R1_Rubber"), (len(pts_u) - 6, "R1_GreyMetal")):
        p_prev = pts_u[idx_r - 1]
        p_cur = pts_u[idx_r]
        p_next = pts_u[idx_r + 1]
        yaw_r = math.degrees(math.atan2(p_next[0] - p_prev[0], p_next[2] - p_prev[2]))
        _add_armor_rib_ring(cable, p_cur, yaw_deg=yaw_r, mat=mat_r)

    cable.build_object(parent=root)

    # Regolith stake clip at the apex of the 90° bend
    stake = r1kit.MeshBuilder("StakeClip", pivot_u=(0.0, 0.0, 0.0))
    _add_regolith_stake_clip(stake, mid_u, yaw_deg=mid_yaw)
    stake.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=start_u)
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=end_u)
    r1kit.create_empty("Clip_Anchor", parent=root, pivot_u=(mid_u[0], 0.064, mid_u[2]))

    apply_cable_dust_uvs_and_colors(root, footprint_radius=0.35)

    fbx_path = PROPS_DIR / "Cable_Bend_90.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_cable_broken_end() -> bpy.types.Object:
    """Build `Cable_BrokenEnd.fbx`: torn cable end with frayed copper strands and cracked sleeve (the cut that killed the post)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Cable_BrokenEnd", pivot_u=(0.0, 0.0, 0.0))

    # 1. CableStub: intact heavy Ø 5 cm cable lead from Start_Anchor (z = -0.45 m) to the break zone (z = +0.12 m)
    stub = r1kit.MeshBuilder("CableStub", pivot_u=(0.0, 0.0, 0.0))
    stub_pts: List[Tuple[float, float, float]] = [
        (0.0, CABLE_CENTER_Y, -0.45),
        (0.006, CABLE_CENTER_Y, -0.30),
        (0.0, CABLE_CENTER_Y, -0.16),
        (-0.008, CABLE_CENTER_Y + 0.002, -0.02),
        (0.0, CABLE_CENTER_Y + 0.006, 0.12),
    ]
    stub.add_tube_path(stub_pts, radius=CABLE_RADIUS, mat="R1_Rubber", segments=12)
    _add_coupling_collar(stub, (0.0, CABLE_CENTER_Y, -0.412), yaw_deg=0.0)
    _add_armor_rib_ring(stub, (0.005, CABLE_CENTER_Y, -0.29), yaw_deg=0.0, mat="R1_Rubber")
    _add_regolith_stake_clip(stub, (0.0, 0.0, -0.16), yaw_deg=0.0)
    stub.build_object(parent=root)

    # 2. CrackedSleeve: split cream-enamel & orange high-voltage sleeve sheared open at z = +0.02 .. +0.25 m
    sleeve = r1kit.MeshBuilder("CrackedSleeve", pivot_u=(0.0, 0.0, 0.0))
    # Rear intact grey-metal gland ring + orange hazard band before the fracture
    sleeve.add_cylinder(
        (0.0, 0.025, 0.045),
        radius=0.033,
        length=0.048,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=12,
        bevel=0.003,
    )
    sleeve.add_cylinder(
        (0.0, 0.025, 0.082),
        radius=0.0345,
        length=0.028,
        mat="R1_OrangeStripe",
        axis_u="Z",
        segments=12,
        bevel=0.0025,
    )

    # Cracked, splayed-apart CreamEnamel outer conduit shell shards (split open along top & sides)
    sleeve.add_bevel_box(
        (-0.022, 0.038, 0.148),
        (0.032, 0.042, 0.115),
        mat="R1_CreamEnamel",
        bevel=0.004,
        segments=1,
        rot_deg_u=(-8.0, -14.0, 18.0),
    )
    sleeve.add_bevel_box(
        (0.024, 0.034, 0.142),
        (0.030, 0.040, 0.105),
        mat="R1_CreamEnamel",
        bevel=0.004,
        segments=1,
        rot_deg_u=(-5.0, 16.0, -22.0),
    )
    # Lower cracked cream saddle shard resting in the regolith
    sleeve.add_bevel_box(
        (0.004, 0.010, 0.165),
        (0.054, 0.020, 0.120),
        mat="R1_CreamEnamel",
        bevel=0.003,
        segments=1,
        rot_deg_u=(4.0, 5.0, 0.0),
    )
    # Detached cracked sleeve shard lying on the dust beside the cut
    sleeve.add_bevel_box(
        (0.088, 0.011, 0.235),
        (0.036, 0.018, 0.064),
        mat="R1_CreamEnamel",
        bevel=0.003,
        segments=1,
        rot_deg_u=(6.0, 32.0, -10.0),
    )
    sleeve.add_bevel_box(
        (0.088, 0.019, 0.225),
        (0.038, 0.008, 0.024),
        mat="R1_OrangeStripe",
        bevel=0.0015,
        segments=1,
        rot_deg_u=(6.0, 32.0, -10.0),
    )

    # Peeled-back bare-steel retaining clamp strap + sheared brass bolt
    sleeve.add_bevel_box(
        (0.036, 0.052, 0.165),
        (0.014, 0.036, 0.022),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
        rot_deg_u=(-18.0, 24.0, -35.0),
    )
    sleeve.add_cylinder(
        (0.044, 0.064, 0.165),
        radius=0.007,
        length=0.012,
        mat="R1_Brass",
        axis_u="Y",
        segments=6,
        bevel=0.001,
    )

    # Torn inner GoldFoil Kapton thermal wrap + jagged inner Rubber core jacket at the mouth of the cut
    sleeve.add_cylinder(
        (0.0, 0.026, 0.165),
        radius=0.024,
        length=0.065,
        mat="R1_GoldFoil",
        axis_u="Z",
        segments=10,
        bevel=0.002,
        radius_top=0.028,
    )
    sleeve.add_cylinder(
        (0.0, 0.026, 0.195),
        radius=0.021,
        length=0.050,
        mat="R1_Rubber",
        axis_u="Z",
        segments=10,
        bevel=0.002,
        radius_top=0.025,
    )
    sleeve.build_object(parent=root)

    # 3. FrayedStrands: splayed multi-gauge copper power cores & braided shield wires bursting from the cut
    strands = r1kit.MeshBuilder("FrayedStrands", pivot_u=(0.0, 0.0, 0.0))

    # Three heavy twisted primary copper phase cores (radius 0.0085 m)
    heavy_cores: Sequence[Sequence[Tuple[float, float, float]]] = (
        (
            (-0.006, 0.028, 0.19),
            (-0.018, 0.044, 0.26),
            (-0.042, 0.068, 0.33),
            (-0.064, 0.082, 0.39),
        ),
        (
            (0.008, 0.024, 0.19),
            (0.028, 0.022, 0.27),
            (0.058, 0.015, 0.35),
            (0.086, 0.012, 0.42),
        ),
        (
            (0.0, 0.020, 0.19),
            (-0.012, 0.016, 0.28),
            (-0.034, 0.011, 0.36),
            (-0.052, 0.009, 0.44),
        ),
    )
    for core_pts in heavy_cores:
        strands.add_tube_path(core_pts, radius=0.0085, mat="R1_Copper", segments=8)

    # Eight finer frayed copper conductor strands fanning out in 3D
    fine_strands: Sequence[Sequence[Tuple[float, float, float]]] = (
        (
            (0.0, 0.032, 0.20),
            (0.014, 0.058, 0.27),
            (0.022, 0.088, 0.34),
            (0.012, 0.098, 0.38),
        ),
        (
            (-0.010, 0.026, 0.20),
            (-0.045, 0.036, 0.27),
            (-0.088, 0.028, 0.33),
            (-0.118, 0.014, 0.37),
        ),
        (
            (0.010, 0.028, 0.20),
            (0.048, 0.042, 0.26),
            (0.092, 0.038, 0.32),
            (0.122, 0.022, 0.36),
        ),
        (
            (-0.004, 0.022, 0.21),
            (0.008, 0.018, 0.30),
            (0.018, 0.012, 0.39),
            (0.026, 0.008, 0.45),
        ),
        (
            (0.006, 0.030, 0.20),
            (0.032, 0.062, 0.26),
            (0.056, 0.074, 0.31),
        ),
        (
            (-0.008, 0.024, 0.20),
            (-0.032, 0.018, 0.27),
            (-0.072, 0.010, 0.33),
        ),
        (
            (0.0, 0.034, 0.20),
            (-0.016, 0.064, 0.25),
            (-0.008, 0.092, 0.29),
        ),
        (
            (0.012, 0.022, 0.20),
            (0.042, 0.014, 0.26),
            (0.068, 0.009, 0.31),
        ),
    )
    for f_pts in fine_strands:
        strands.add_tube_path(f_pts, radius=0.0048, mat="R1_Copper", segments=6)

    # Three recoiled bare-steel / brass armor shield strands near the torn collar
    shield_strands: Sequence[ Tuple[Sequence[Tuple[float, float, float]], str] ] = (
        (
            (
                (-0.016, 0.034, 0.19),
                (-0.046, 0.056, 0.22),
                (-0.068, 0.048, 0.20),
            ),
            "R1_BareSteel",
        ),
        (
            (
                (0.016, 0.032, 0.19),
                (0.052, 0.048, 0.23),
                (0.074, 0.036, 0.25),
            ),
            "R1_Brass",
        ),
        (
            (
                (0.004, 0.036, 0.19),
                (0.018, 0.068, 0.22),
                (0.038, 0.078, 0.21),
            ),
            "R1_BareSteel",
        ),
    )
    for s_pts, s_mat in shield_strands:
        strands.add_tube_path(s_pts, radius=0.0042, mat=s_mat, segments=6)

    strands_obj = strands.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, CABLE_CENTER_Y, -0.45))
    r1kit.create_empty("Break_Anchor", parent=root, pivot_u=(0.0, 0.038, 0.28))
    r1kit.create_empty("Clip_Anchor", parent=root, pivot_u=(0.0, 0.064, -0.16))

    apply_cable_dust_uvs_and_colors(root, footprint_radius=0.35)

    # Boost metallic edge-wear glint (G) and clean AO (R) on FrayedStrands so sheared copper glints in headlamp light
    if strands_obj.data and "Col" in strands_obj.data.color_attributes:
        attr = strands_obj.data.color_attributes["Col"]
        for item in attr.data:
            r, g, b, a = item.color
            item.color = (max(0.88, r), max(0.78, g), min(0.16, b * 0.35), a)

    fbx_path = PROPS_DIR / "Cable_BrokenEnd.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_cable_plug() -> bpy.types.Object:
    """Build `Cable_Plug.fbx`: heavy R1 power plug end that seats into LanderServicePort and ChargingPad sockets."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Cable_Plug", pivot_u=(0.0, 0.0, 0.0))

    # 1. CableLead: regolith cable entry (-0.46 m), heavy strain-relief anchor block (-0.14 m), and S-sweep up to y = 0.20 m
    lead = r1kit.MeshBuilder("CableLead", pivot_u=(0.0, 0.0, 0.0))
    lead_pts: List[Tuple[float, float, float]] = [
        (0.0, CABLE_CENTER_Y, -0.46),
        (0.008, CABLE_CENTER_Y, -0.31),
        (0.0, 0.036, -0.14),
        (-0.008, 0.068, 0.02),
        (0.0, 0.142, 0.13),
        (0.0, 0.200, 0.23),
    ]
    lead.add_tube_path(lead_pts, radius=CABLE_RADIUS, mat="R1_Rubber", segments=12)

    # Input coupling collar at z = -0.422 m matching Cable_Straight_2m
    _add_coupling_collar(lead, (0.0, CABLE_CENTER_Y, -0.422), yaw_deg=0.0)

    # Heavy ground strain-relief anchor block at z = -0.14 m
    lead.add_bevel_box(
        (0.0, 0.005, -0.14),
        (0.256, 0.020, 0.200),
        mat="R1_Rubber",
        bevel=0.004,
        segments=1,
    )
    lead.add_bevel_box(
        (0.0, 0.044, -0.14),
        (0.232, 0.064, 0.176),
        mat="R1_GreyMetal",
        bevel=0.008,
        segments=2,
    )
    lead.add_bevel_box(
        (0.0, 0.084, -0.14),
        (0.136, 0.024, 0.148),
        mat="R1_CreamEnamel",
        bevel=0.005,
        segments=1,
    )
    lead.add_bevel_box(
        (0.0, 0.048, -0.14),
        (0.238, 0.022, 0.056),
        mat="R1_OrangeStripe",
        bevel=0.003,
        segments=1,
    )
    # Driven regolith stakes & top hex anchor bolts on strain-relief block
    for sx in (-1.0, 1.0):
        bx = sx * 0.088
        lead.add_cylinder(
            (bx, 0.036, -0.14),
            radius=0.012,
            length=0.104,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.002,
        )
        lead.add_cylinder(
            (bx, 0.082, -0.14),
            radius=0.018,
            length=0.010,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
        )
        lead.add_cylinder(
            (bx, 0.092, -0.14),
            radius=0.012,
            length=0.012,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.0015,
        )

    lead.build_object(parent=root)

    # 2. PlugHead: bayonet power plug at y = 0.200 m matching ChargingPad (r=0.075 GreyMetal / r=0.052 Copper) & LanderServicePort
    plug = r1kit.MeshBuilder("PlugHead", pivot_u=(0.0, 0.200, 0.34))
    py = 0.200

    # Tapered ribbed rubber strain-relief boot (z = 0.195 .. 0.255)
    plug.add_cylinder(
        (0.0, py, 0.225),
        radius=0.036,
        radius_top=0.058,
        length=0.060,
        mat="R1_Rubber",
        axis_u="Z",
        segments=14,
        bevel=0.004,
    )
    # Main GreyMetal gland collar body (radius = 0.075 m matching ChargingPad's 0.075 m gland receptacle)
    plug.add_cylinder(
        (0.0, py, 0.290),
        radius=0.075,
        length=0.076,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=16,
        bevel=0.006,
    )
    # Warm-orange safety collar band
    plug.add_cylinder(
        (0.0, py, 0.264),
        radius=0.077,
        length=0.022,
        mat="R1_OrangeStripe",
        axis_u="Z",
        segments=16,
        bevel=0.003,
    )
    # Cream-enamel side protective grip shells on left and right of plug body
    for sx in (-1.0, 1.0):
        plug.add_bevel_box(
            (sx * 0.068, py, 0.296),
            (0.022, 0.084, 0.064),
            mat="R1_CreamEnamel",
            bevel=0.004,
            segments=1,
        )

    # Top D-ring pull/latch handle for rover or EVA crew grip (y = 0.268 .. 0.316)
    for sz in (0.266, 0.322):
        plug.add_cylinder(
            (0.0, py + 0.082, sz),
            radius=0.009,
            length=0.036,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
        )
    plug.add_bevel_box(
        (0.0, py + 0.104, 0.294),
        (0.044, 0.022, 0.084),
        mat="R1_OrangeStripe",
        bevel=0.004,
        segments=1,
    )
    plug.add_cylinder(
        (0.0, py + 0.074, 0.294),
        radius=0.012,
        length=0.052,
        mat="R1_Brass",
        axis_u="X",
        segments=8,
        bevel=0.0015,
    )

    # Bare-steel bayonet locking ring (radius 0.070 m) + 3 radial locking lugs (90, 210, 330 deg)
    plug.add_cylinder(
        (0.0, py, 0.346),
        radius=0.070,
        length=0.038,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=16,
        bevel=0.004,
    )
    for ang_deg in (90.0, 210.0, 330.0):
        rad = math.radians(ang_deg)
        lx = math.cos(rad) * 0.070
        ly = py + math.sin(rad) * 0.070
        plug.add_bevel_box(
            (lx, ly, 0.348),
            (0.018, 0.018, 0.024),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
            rot_deg_u=(0.0, 0.0, ang_deg),
        )

    # Brass guide collar ring (z = 0.365 .. 0.387)
    plug.add_cylinder(
        (0.0, py, 0.376),
        radius=0.062,
        length=0.022,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.003,
    )

    # Heavy copper mating contact barrel (radius = 0.052 m matching ChargingPad's R1_Copper socket barrel!)
    plug.add_cylinder(
        (0.0, py, 0.416),
        radius=0.052,
        length=0.060,
        mat="R1_Copper",
        axis_u="Z",
        segments=16,
        bevel=0.004,
    )
    # DarkGlass front insulator well + 3 brass/copper contact pins (matching LanderServicePort / DeadChargingPost)
    plug.add_cylinder(
        (0.0, py, 0.448),
        radius=0.040,
        length=0.010,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=14,
        bevel=0.002,
    )
    for ang_deg in (90.0, 210.0, 330.0):
        rad = math.radians(ang_deg)
        px = math.cos(rad) * 0.022
        pin_y = py + math.sin(rad) * 0.022
        plug.add_cylinder(
            (px, pin_y, 0.456),
            radius=0.011,
            length=0.010,
            mat="R1_Brass",
            axis_u="Z",
            segments=8,
            bevel=0.0015,
        )
        plug.add_cylinder(
            (px, pin_y, 0.468),
            radius=0.0075,
            length=0.024,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.0015,
        )

    plug.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, CABLE_CENTER_Y, -0.46))
    r1kit.create_empty("Plug_Anchor", parent=root, pivot_u=(0.0, 0.200, 0.48))
    r1kit.create_empty("Handle_Anchor", parent=root, pivot_u=(0.0, 0.304, 0.294))

    apply_cable_dust_uvs_and_colors(root, footprint_radius=0.35)

    fbx_path = PROPS_DIR / "Cable_Plug.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_cable_straight_2m()
    build_cable_bend_45()
    build_cable_bend_90()
    build_cable_broken_end()
    build_cable_plug()


if __name__ == "__main__":
    main()
