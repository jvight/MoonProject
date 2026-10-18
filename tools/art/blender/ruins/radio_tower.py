"""Build RadioTower.fbx (R1 Lumen Station ruins — slender leaning lattice mast on rocky mound + fallen Dish).

Includes:
- TowerBase: Low flat-topped rocky lunar basalt mound (`R1_Basalt`), heavy steel anchor footing,
  and snapped lower lattice stub where the mast buckled.
- MastLeaning: Slender 8-bay steel lattice mast (`R1_GreyMetal` / `R1_BareSteel`) snapped at the base
  and leaning ~21 deg from vertical into the sky, with whip antenna and slack rubber cable loops.
- Dish: Separate child mesh node for the smooth 28-segment round parabolic communications dish
  fallen on its tilted pedestal beside the mast on the rocky mound.
- Scan_1, Scan_2: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bmesh
from mathutils import Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _add_strut_between_u(
    mb: r1kit.MeshBuilder,
    p0_u: Tuple[float, float, float],
    p1_u: Tuple[float, float, float],
    thickness: float,
    mat: str,
) -> None:
    """Add a bevelled rectangular strut connecting p0_u and p1_u in Unity coordinates."""
    mb.add_strut(
        p0_u,
        p1_u,
        width=thickness,
        thickness=thickness,
        mat=mat,
        bevel=thickness * 0.16,
    )


def _add_rocky_mound(
    mb: r1kit.MeshBuilder,
    center_xz: Tuple[float, float] = (0.0, 0.0),
    rx_base: float = 3.10,
    rz_base: float = 1.95,
    height: float = 0.68,
    segs: int = 28,
    mat: str = "R1_Basalt",
) -> None:
    """Build a closed, manifold flat-topped rocky lunar mound (`R1_Basalt`) matching ruins_v1_dawn.png."""
    cx, cz = center_xz
    levels = [
        (0.00, 1.00, 0.08),
        (height * 0.44, 0.91, 0.12),
        (height * 0.85, 0.81, 0.10),
        (height, 0.75, 0.05),
    ]

    rings: List[List[bmesh.types.BMVert]] = []
    for lvl_idx, (yv, scale_r, crag_amp) in enumerate(levels):
        ring: List[bmesh.types.BMVert] = []
        for s in range(segs):
            ang = (2.0 * math.pi * s) / segs
            crag = 1.0 + crag_amp * (
                0.55 * math.sin(ang * 5.0 + 0.4)
                + 0.35 * math.cos(ang * 8.0 - 0.7)
                + 0.25 * math.sin(ang * 3.0 + lvl_idx * 0.6)
            )
            ux = cx + rx_base * scale_r * crag * math.cos(ang)
            uz = cz + rz_base * scale_r * crag * math.sin(ang)
            uy = yv
            if 0 < lvl_idx < len(levels) - 1:
                uy += 0.04 * math.cos(ang * 6.0 + lvl_idx)
            ring.append(mb.bm.verts.new(mb._to_local_b((ux, uy, uz))))
        rings.append(ring)

    faces: List[bmesh.types.BMFace] = []
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for s in range(segs):
            sn = (s + 1) % segs
            faces.append(mb.bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    faces.append(mb.bm.faces.new(list(reversed(rings[0]))))
    faces.append(mb.bm.faces.new(rings[-1]))

    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)

    # Angular fractured basalt rock ledges along the front and side cliff rim of the mound
    rock_ledges = [
        ((-1.65, 0.30, -1.18), (1.15, 0.56, 0.68), (8.0, -18.0, -6.0)),
        ((-0.45, 0.32, -1.36), (1.35, 0.60, 0.72), (6.0, 8.0, 4.0)),
        ((0.88, 0.30, -1.28), (1.28, 0.56, 0.70), (10.0, 22.0, -5.0)),
        ((1.95, 0.26, -0.78), (1.05, 0.50, 0.66), (5.0, 42.0, -8.0)),
        ((-2.20, 0.25, -0.45), (0.96, 0.48, 0.74), (-6.0, -48.0, 7.0)),
    ]
    for r_pos, r_size, r_rot in rock_ledges:
        mb.add_bevel_box(r_pos, r_size, bevel=0.04, mat=mat, rot_deg_u=r_rot, segs=1)


def _build_tower_base(root) -> None:
    """Build the rocky basalt mound, anchored steel footing, and snapped lower mast stub."""
    mb = r1kit.MeshBuilder()

    # 1. Low flat-topped rocky lunar mound (`R1_Basalt`)
    _add_rocky_mound(
        mb,
        center_xz=(0.0, 0.0),
        rx_base=2.85,
        rz_base=1.82,
        height=0.68,
        segs=28,
        mat="R1_Basalt",
    )

    # 2. Anchored steel foundation plinth on the left side of the mound (`x = -0.95, y = 0.68, z = 0.0`)
    bx, by, bz = -0.95, 0.68, 0.0
    mb.add_bevel_box((bx, by + 0.06, bz), (0.96, 0.16, 0.96), bevel=0.02, mat="R1_GreyMetal", segs=1)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_cylinder(
                (bx + sx * 0.36, by + 0.14, bz + sz * 0.36),
                radius=0.08,
                height=0.12,
                segs=12,
                axis="y",
                mat="R1_BareSteel",
                bevel=0.01,
            )

    # 3. Short lower lattice stub (`y = 0.72 .. 1.32`) that snapped when the mast leaned right (+X)
    w0, w1 = 0.24, 0.22
    y0, y1 = by + 0.08, by + 0.62
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            p0 = (bx + sx * w0, y0, bz + sz * w0)
            p1 = (bx + sx * w1 + 0.06, y1, bz + sz * w1)
            _add_strut_between_u(mb, p0, p1, 0.048, "R1_GreyMetal")

    # Snapped diagonal braces & one auxiliary support strut leaning left from the snapped base onto the mound
    _add_strut_between_u(mb, (bx - w0, y0, bz - w0), (bx + w1 + 0.06, y1, bz - w1), 0.032, "R1_BareSteel")
    _add_strut_between_u(mb, (bx - w0, y0, bz + w0), (bx + w1 + 0.06, y1, bz + w1), 0.032, "R1_BareSteel")
    for sz in (-0.20, 0.20):
        _add_strut_between_u(
            mb,
            (bx - 0.68, by + 0.04, bz + sz),
            (bx - 0.06, by + 1.18, bz + sz * 0.8),
            0.038,
            "R1_GreyMetal",
        )
        _add_strut_between_u(
            mb,
            (bx - 0.42, by + 0.56, bz + sz * 0.9),
            (bx - 0.10, by + 0.52, bz + sz * 0.9),
            0.028,
            "R1_BareSteel",
        )

    ruins_common.to_ruin_object(mb, "TowerBase", parent=root)


def _build_leaning_mast(root) -> None:
    """Build the slender steel lattice mast leaning ~21 deg from vertical into the lunar sky."""
    mb = r1kit.MeshBuilder()

    # Mast spine leaning upward and right (+X) at ~21 degrees from vertical (matching ruins_v1_dawn.png)
    start_u = Vector((-0.92, 0.88, 0.0))
    end_u = Vector((1.04, 6.08, 0.06))
    spine_dir = (end_u - start_u).normalized()
    side_z = Vector((0.0, 0.0, 1.0))
    side_x = side_z.cross(spine_dir).normalized()

    num_bays = 8
    ts = [i / num_bays for i in range(num_bays + 1)]
    widths = [0.22 - 0.009 * i for i in range(num_bays + 1)]

    def corner_pt(bay_idx: int, sx: float, sz: float) -> Tuple[float, float, float]:
        t = ts[bay_idx]
        w = widths[bay_idx]
        p = start_u.lerp(end_u, t) + side_x * (sx * w) + side_z * (sz * w)
        return (p.x, p.y, p.z)

    for b in range(num_bays):
        for sx in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                _add_strut_between_u(
                    mb,
                    corner_pt(b, sx, sz),
                    corner_pt(b + 1, sx, sz),
                    0.044,
                    "R1_GreyMetal",
                )

        corners0 = [corner_pt(b, -1, -1), corner_pt(b, 1, -1), corner_pt(b, 1, 1), corner_pt(b, -1, 1)]
        corners1 = [corner_pt(b + 1, -1, -1), corner_pt(b + 1, 1, -1), corner_pt(b + 1, 1, 1), corner_pt(b + 1, -1, 1)]
        for s in range(4):
            sn = (s + 1) % 4
            # Horizontal girt at top of bay
            _add_strut_between_u(mb, corners1[s], corners1[sn], 0.030, "R1_GreyMetal")
            # X-bracing diagonals on each lattice face
            _add_strut_between_u(mb, corners0[s], corners1[sn], 0.025, "R1_BareSteel")
            _add_strut_between_u(mb, corners0[sn], corners1[s], 0.025, "R1_BareSteel")

    # Top antenna collar, side standoffs & slender vertical whip antenna rising into the sky
    top_collar = end_u + spine_dir * 0.06
    _add_strut_between_u(
        mb,
        (end_u.x - side_x.x * 0.24, end_u.y - side_x.y * 0.24, end_u.z),
        (end_u.x + side_x.x * 0.24, end_u.y + side_x.y * 0.24, end_u.z),
        0.045,
        "R1_GreyMetal",
    )
    whip_tip = top_collar + spine_dir * 1.15
    _add_strut_between_u(
        mb,
        (top_collar.x, top_collar.y, top_collar.z),
        (whip_tip.x, whip_tip.y, whip_tip.z),
        0.026,
        "R1_BareSteel",
    )
    # Small side telemetry dipole rod near top of mast
    side_arm_base = start_u.lerp(end_u, 0.92) + side_x * 0.16
    side_arm_tip = side_arm_base + spine_dir * 0.55 + side_x * 0.08
    _add_strut_between_u(
        mb,
        (side_arm_base.x, side_arm_base.y, side_arm_base.z),
        (side_arm_tip.x, side_arm_tip.y, side_arm_tip.z),
        0.022,
        "R1_BareSteel",
    )

    # Slack coaxial cable loops (`R1_Rubber`) hanging near the lower snapped bays of the mast
    cable_pts_1 = [
        (-0.35, 2.35, -0.14),
        (-0.22, 1.48, -0.18),
        (-0.38, 0.88, -0.16),
        (-0.68, 0.70, -0.10),
    ]
    mb.add_tube_path(cable_pts_1, radius=0.022, mat="R1_Rubber", segments=8, cap_ends=True)

    cable_pts_2 = [
        (-0.52, 1.92, 0.14),
        (-0.78, 1.24, 0.18),
        (-1.12, 0.72, 0.12),
    ]
    mb.add_tube_path(cable_pts_2, radius=0.020, mat="R1_Rubber", segments=8, cap_ends=True)

    ruins_common.to_ruin_object(mb, "MastLeaning", parent=root)


def _add_round_parabolic_dish(
    mb: r1kit.MeshBuilder,
    dc: Vector,
    axis_u: Vector,
    right_u: Vector,
    up_u: Vector,
    radius: float = 1.12,
    depth: float = 0.30,
    thickness: float = 0.036,
    segs: int = 28,
    rings_count: int = 4,
) -> None:
    """Build a smooth, continuous 28-segment round parabolic dish bowl with dark-grey rim and steel ribs."""
    r_fracs = [k / rings_count for k in range(rings_count + 1)]

    def pt_on_dish(r_frac: float, ang: float, z_off: float) -> Vector:
        r = radius * r_frac
        radial = right_u * math.cos(ang) + up_u * math.sin(ang)
        parab_z = depth * (r_frac * r_frac) + z_off
        p_u = dc + radial * r + axis_u * parab_z
        return mb._to_local_b((p_u.x, p_u.y, p_u.z))

    front_rings: List[List[bmesh.types.BMVert]] = []
    back_rings: List[List[bmesh.types.BMVert]] = []

    for rf in r_fracs:
        fr: List[bmesh.types.BMVert] = []
        br: List[bmesh.types.BMVert] = []
        for s in range(segs):
            ang = (2.0 * math.pi * s) / segs
            fr.append(mb.bm.verts.new(pt_on_dish(rf, ang, 0.0)))
            br.append(mb.bm.verts.new(pt_on_dish(rf, ang, -thickness)))
        front_rings.append(fr)
        back_rings.append(br)

    dish_faces: List[bmesh.types.BMFace] = []
    rim_faces: List[bmesh.types.BMFace] = []

    # Center cap quads/n-gon (inner ring rf = 1/rings_count)
    dish_faces.append(mb.bm.faces.new(front_rings[1]))
    rim_faces.append(mb.bm.faces.new(list(reversed(back_rings[1]))))

    for r_idx in range(1, rings_count):
        f0 = front_rings[r_idx]
        f1 = front_rings[r_idx + 1]
        b0 = back_rings[r_idx]
        b1 = back_rings[r_idx + 1]
        for s in range(segs):
            sn = (s + 1) % segs
            face_f = mb.bm.faces.new([f0[s], f1[s], f1[sn], f0[sn]])
            face_b = mb.bm.faces.new([b0[sn], b1[sn], b1[s], b0[s]])
            if r_idx == rings_count - 1:
                rim_faces.append(face_f)
            else:
                dish_faces.append(face_f)
            rim_faces.append(face_b)

    # Outer perimeter rim band
    f_out = front_rings[-1]
    b_out = back_rings[-1]
    for s in range(segs):
        sn = (s + 1) % segs
        rim_faces.append(mb.bm.faces.new([f_out[s], b_out[s], b_out[sn], f_out[sn]]))

    all_faces = dish_faces + rim_faces
    bmesh.ops.recalc_face_normals(mb.bm, faces=all_faces)
    mb._assign_faces(dish_faces, "R1_CreamEnamel")
    mb._assign_faces(rim_faces, "R1_GreyMetal")

    # Slender radial & concentric structural ribs inside/behind the dish (matching ruins_v1_dawn.png)
    num_ribs = 12
    for i in range(num_ribs):
        ang = (2.0 * math.pi * i) / num_ribs
        radial = right_u * math.cos(ang) + up_u * math.sin(ang)
        p_in = dc + radial * (radius * 0.14) + axis_u * (depth * 0.02)
        p_mid = dc + radial * (radius * 0.56) + axis_u * (depth * 0.31)
        p_out = dc + radial * (radius * 0.98) + axis_u * (depth * 0.96)
        _add_strut_between_u(mb, (p_in.x, p_in.y, p_in.z), (p_mid.x, p_mid.y, p_mid.z), 0.022, "R1_BareSteel")
        _add_strut_between_u(mb, (p_mid.x, p_mid.y, p_mid.z), (p_out.x, p_out.y, p_out.z), 0.022, "R1_BareSteel")

        # Mid-radius concentric spider ring segment
        ang_next = (2.0 * math.pi * ((i + 1) % num_ribs)) / num_ribs
        radial_next = right_u * math.cos(ang_next) + up_u * math.sin(ang_next)
        p_mid_next = dc + radial_next * (radius * 0.56) + axis_u * (depth * 0.31)
        _add_strut_between_u(
            mb,
            (p_mid.x, p_mid.y, p_mid.z),
            (p_mid_next.x, p_mid_next.y, p_mid_next.z),
            0.020,
            "R1_BareSteel",
        )


def _build_fallen_dish(root) -> None:
    """Build the separate `Dish` child mesh node fallen beside the mast on the rocky mound."""
    mb = r1kit.MeshBuilder()

    # Fallen conical/cylindrical pedestal mount resting on the rocky mound (`x = 0.62, y = 0.86, z = -0.04`)
    ped_p0 = Vector((0.46, 0.72, -0.02))
    ped_p1 = Vector((0.92, 1.28, -0.04))
    _add_strut_between_u(mb, (ped_p0.x, ped_p0.y, ped_p0.z), (ped_p1.x, ped_p1.y, ped_p1.z), 0.38, "R1_GreyMetal")
    mb.add_bevel_box(
        (0.56, 0.86, -0.02),
        (0.56, 0.34, 0.52),
        bevel=0.02,
        mat="R1_GreyMetal",
        rot_deg_u=(0.0, 0.0, -36.0),
        segs=1,
    )

    # Dish center on the right side of the mound, tilted upward & right (matching ruins_v1_dawn.png)
    dc = Vector((1.06, 1.54, -0.05))
    axis_u = Vector((0.68, 0.68, -0.27)).normalized()
    right_u = axis_u.cross(Vector((0.0, 1.0, 0.0))).normalized()
    up_u = right_u.cross(axis_u).normalized()

    _add_round_parabolic_dish(
        mb,
        dc=dc,
        axis_u=axis_u,
        right_u=right_u,
        up_u=up_u,
        radius=1.10,
        depth=0.28,
        thickness=0.036,
        segs=28,
        rings_count=4,
    )

    # Central waveguide feed horn & 4-legged sub-reflector tripod along axis_u
    horn_tip = dc + axis_u * 0.78
    _add_strut_between_u(
        mb,
        (dc.x, dc.y, dc.z),
        (dc.x + axis_u.x * 0.24, dc.y + axis_u.y * 0.24, dc.z + axis_u.z * 0.24),
        0.12,
        "R1_GreyMetal",
    )
    sub_p0 = horn_tip - axis_u * 0.06
    sub_p1 = horn_tip + axis_u * 0.05
    _add_strut_between_u(
        mb,
        (sub_p0.x, sub_p0.y, sub_p0.z),
        (sub_p1.x, sub_p1.y, sub_p1.z),
        0.16,
        "R1_Copper",
    )

    for leg_i in range(4):
        la = math.radians(leg_i * 90.0 + 45.0)
        ldir = right_u * math.cos(la) + up_u * math.sin(la)
        lbase = dc + ldir * 0.86 + axis_u * 0.18
        _add_strut_between_u(
            mb,
            (lbase.x, lbase.y, lbase.z),
            (horn_tip.x, horn_tip.y, horn_tip.z),
            0.024,
            "R1_BareSteel",
        )

    ruins_common.to_ruin_object(mb, "Dish", parent=root)


def _build_collision_proxies(root) -> None:
    """Build authored convex hull collision proxies (`UCX_<Part>_<n>`, <= 32 faces each)."""
    # 1. Low flat-topped rocky lunar basalt mound (8-sided convex frustum, 10 faces / 28 tris)
    ruins_common.add_convex_proxy(
        root,
        "UCX_TowerBase_1",
        ruins_common.convex_prism_points_u(
            center_u=(0.0, 0.34, 0.0),
            radius_a=2.70,
            radius_b=1.72,
            length=0.68,
            axis="y",
            segs=8,
            scale_pos_end=(0.76, 0.76),
            clip_min_y_u=0.01,
        ),
    )
    # 2. Steel foundation plinth & snapped lower mast stub on the left of the mound
    #    (kept behind z = -0.20 so Scan_2 at (-0.82, 1.32, -0.22) is unblocked)
    ruins_common.add_convex_proxy(
        root,
        "UCX_TowerBase_2",
        ruins_common.convex_box_points_u(
            center_u=(-1.06, 1.22, 0.04),
            size_u=(0.96, 1.12, 0.48),
            top_scale_xz=(0.68, 0.72),
            clip_min_y_u=0.64,
        ),
    )
    # 3. Slender leaning steel lattice mast spine (~21 deg from vertical)
    ruins_common.add_convex_proxy(
        root,
        "UCX_MastLeaning_1",
        ruins_common.convex_strut_points_u(
            p0_u=(-0.92, 0.88, 0.0),
            p1_u=(1.04, 6.08, 0.06),
            half_w=0.22,
            half_d=0.20,
        ),
    )
    # 4. Fallen parabolic dish & pedestal resting on the right side of the mound
    ruins_common.add_convex_proxy(
        root,
        "UCX_Dish_1",
        ruins_common.convex_prism_points_u(
            center_u=(0.96, 1.44, -0.05),
            radius_a=1.04,
            radius_b=1.04,
            length=0.62,
            axis="y",
            segs=8,
            rot_deg_u=(18.0, 0.0, -42.0),
            clip_min_y_u=0.64,
        ),
    )


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("RadioTower")

    _build_tower_base(root)
    _build_leaning_mast(root)
    _build_fallen_dish(root)
    _build_collision_proxies(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (1.58, 2.06, -0.24), parent=root)   # Fallen Dish feed horn
    r1kit.add_empty("Scan_2", (-0.82, 1.32, -0.22), parent=root)  # Snapped mast base on rocky mound

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="radio_tower",
        fbx_name="RadioTower.fbx",
        footprint_radius=4.5,
        cam_angle_deg=18.0,
        cam_elev_deg=12.0,
        skip_renders=skip_renders,
    )
    wire_path = ruins_common.render_ruin_collision_wireframe(
        root,
        slug="radio_tower",
        cam_angle_deg=18.0,
        cam_elev_deg=16.0,
    )
    print(f"[radio_tower] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}, {wire_path.name}")


if __name__ == "__main__":
    build(skip_renders="--skip-renders" in sys.argv)
