"""Build RadioTower.fbx (R1 Lumen Station ruins — snapped & leaning lattice mast + fallen Dish).

Includes:
- TowerBase: Heavy anchored pedestal, telemetry junction cabinet with R1_Signage plates
  (LUMEN, PICT_RADIO, SEC_04, HAZARD), and lower vertical lattice mast section up to the
  snapped fracture point at y = 2.35 m.
- MastLeaning: Upper lattice mast section painted in aerospace orange/cream bands, snapped at
  the fracture point and leaning diagonally down to the ground with coaxial cable loops and
  top omni-antenna array.
- Dish: Required separate child mesh node for the fallen parabolic communications dish beside
  the tower, complete with segmented reflector panels, rear structural ribs, gold foil hub,
  and tripod copper/brass feed horn.
- Scan_1, Scan_2: Empty scan target nodes.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

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
    p0_u: tuple[float, float, float],
    p1_u: tuple[float, float, float],
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
        bevel=thickness * 0.18,
    )


def _build_tower_base(root) -> None:
    mb = r1kit.MeshBuilder()

    # Heavy octagonal/square base plinth at y_u = 0
    mb.add_bevel_box((0.0, 0.14, 0.0), (1.76, 0.28, 1.76), bevel=0.035, mat="R1_GreyMetal", segs=2)
    mb.add_bevel_box((0.0, 0.34, 0.0), (1.36, 0.16, 1.36), bevel=0.025, mat="R1_BareSteel", segs=1)

    # 4 corner anchor pads & diagonal buttress gussets
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_cylinder((sx * 0.72, 0.06, sz * 0.72), radius=0.18, height=0.12, segs=10, axis="y", mat="R1_BareSteel", bevel=0.015)
            mb.add_Detail_Bolt((sx * 0.72, 0.14, sz * 0.72), radius=0.035, height=0.03, axis="y", mat="R1_BareSteel")
            mb.add_bevel_box(
                (sx * 0.56, 0.38, sz * 0.56),
                (0.12, 0.42, 0.28),
                bevel=0.015,
                mat="R1_GreyMetal",
                rot_deg_u=(sz * -18.0, sx * sz * 45.0, 0.0),
                segs=1,
            )

    # Lower vertical square lattice mast (3 bays from y = 0.40 to y = 2.38)
    bay_y = [0.40, 1.06, 1.72, 2.38]
    half_w = [0.46, 0.42, 0.38, 0.35]

    for b in range(3):
        y0, y1 = bay_y[b], bay_y[b + 1]
        w0, w1 = half_w[b], half_w[b + 1]
        chord_mat = "R1_OrangeStripe" if b == 1 else "R1_GreyMetal"
        for sx in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                p0 = (sx * w0, y0, sz * w0)
                p1 = (sx * w1, y1, sz * w1)
                _add_strut_between_u(mb, p0, p1, 0.075, chord_mat)

        # Horizontal girts & X-diagonals on all 4 faces of the lattice bay
        for side in range(4):
            if side == 0:
                a0, b0, a1, b1 = (-w0, y0, -w0), (w0, y0, -w0), (-w1, y1, -w1), (w1, y1, -w1)
            elif side == 1:
                a0, b0, a1, b1 = (w0, y0, -w0), (w0, y0, w0), (w1, y1, -w1), (w1, y1, w1)
            elif side == 2:
                a0, b0, a1, b1 = (w0, y0, w0), (-w0, y0, w0), (w1, y1, w1), (-w1, y1, w1)
            else:
                a0, b0, a1, b1 = (-w0, y0, w0), (-w0, y0, -w0), (-w1, y1, w1), (-w1, y1, -w1)

            _add_strut_between_u(mb, a1, b1, 0.048, "R1_GreyMetal")
            _add_strut_between_u(mb, a0, b1, 0.040, "R1_BareSteel")
            _add_strut_between_u(mb, b0, a1, 0.040, "R1_BareSteel")

    # Snapped/twisted chord stubs protruding at the fracture plane y = 2.38
    mb.add_bevel_box((-0.36, 2.52, -0.34), (0.08, 0.36, 0.08), bevel=0.01, mat="R1_BareSteel", rot_deg_u=(18.0, 12.0, -24.0))
    mb.add_bevel_box((-0.34, 2.48, 0.36), (0.08, 0.32, 0.08), bevel=0.01, mat="R1_BareSteel", rot_deg_u=(-22.0, -14.0, -18.0))

    # Telemetry & Waveguide Junction Cabinet mounted on the front (-Z) of the base
    cx, cy, cz = 0.0, 0.92, -0.66
    mb.add_bevel_box((cx, cy, cz), (0.86, 1.04, 0.42), bevel=0.025, mat="R1_CreamEnamel", segs=2)
    mb.add_bevel_box((cx, cy + 0.44, cz - 0.02), (0.88, 0.16, 0.44), bevel=0.015, mat="R1_OrangeStripe", segs=1)
    # Recessed twin cabinet doors & latches
    for dx in (-0.20, 0.20):
        mb.add_bevel_box((cx + dx, cy - 0.05, cz - 0.20), (0.36, 0.72, 0.04), bevel=0.008, mat="R1_GreyMetal", segs=1)
    # Copper waveguide tubes & brass glands rising from cabinet into mast
    for wx in (-0.22, 0.0, 0.22):
        mb.add_cylinder((wx, cy + 0.56, cz + 0.06), radius=0.032, height=0.14, segs=10, axis="y", mat="R1_Brass", bevel=0.004)
        mb.add_cylinder((wx, 1.85, cz + 0.22), radius=0.024, height=0.95, segs=8, axis="y", mat="R1_Copper", bevel=0.004)
    # Amber diagnostic beacon on top of cabinet
    mb.add_cylinder((cx + 0.32, cy + 0.58, cz - 0.08), radius=0.045, height=0.10, segs=10, axis="y", mat="R1_LampAmber", bevel=0.008)

    # Signage plates on Telemetry Cabinet (facing -Z toward viewer, rot_deg_u = (0, 180, 0))
    ruins_common.add_signage_plate(
        mb,
        center_u=(cx - 0.20, cy + 0.12, cz - 0.22),
        width=0.32,
        height=0.22,
        cell_name="LUMEN",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(cx + 0.20, cy + 0.12, cz - 0.22),
        width=0.24,
        height=0.24,
        cell_name="PICT_RADIO",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(cx - 0.20, cy - 0.18, cz - 0.22),
        width=0.30,
        height=0.20,
        cell_name="SEC_04",
        rot_deg_u=(0.0, 180.0, 0.0),
    )
    ruins_common.add_signage_plate(
        mb,
        center_u=(cx, cy - 0.44, cz - 0.21),
        width=0.78,
        height=0.12,
        cell_name="HAZARD",
        rot_deg_u=(0.0, 180.0, 0.0),
        frame_mat=None,
        rivets=False,
    )

    ruins_common.to_ruin_object(mb, "TowerBase", parent=root)


def _build_leaning_mast(root) -> None:
    """Build the snapped upper lattice mast leaning from (0.28, 2.32, -0.05) down to (4.10, 0.32, -0.55)."""
    mb = r1kit.MeshBuilder()

    # Spine of the snapped upper mast (5 lattice bays, length ~4.35 m)
    start_u = Vector((0.28, 2.32, -0.05))
    end_u = Vector((4.12, 0.34, -0.55))
    spine_dir = (end_u - start_u).normalized()
    up_ref = Vector((0.0, 1.0, 0.0))
    side_z = spine_dir.cross(up_ref).normalized()
    side_y = side_z.cross(spine_dir).normalized()

    num_bays = 5
    ts = [i / num_bays for i in range(num_bays + 1)]
    widths = [0.34 - 0.032 * i for i in range(num_bays + 1)]

    def corner_pt(bay_idx: int, sy: float, sz: float) -> tuple[float, float, float]:
        t = ts[bay_idx]
        w = widths[bay_idx]
        p = start_u.lerp(end_u, t) + side_y * (sy * w) + side_z * (sz * w)
        return (p.x, p.y, p.z)

    for b in range(num_bays):
        # Alternating aerospace orange & cream enamel bands along the leaning mast
        chord_mat = "R1_OrangeStripe" if (b % 2 == 0) else "R1_CreamEnamel"
        brace_mat = "R1_BareSteel" if (b % 2 == 0) else "R1_GreyMetal"
        for sy in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                _add_strut_between_u(mb, corner_pt(b, sy, sz), corner_pt(b + 1, sy, sz), 0.064, chord_mat)

        corners0 = [corner_pt(b, -1, -1), corner_pt(b, 1, -1), corner_pt(b, 1, 1), corner_pt(b, -1, 1)]
        corners1 = [corner_pt(b + 1, -1, -1), corner_pt(b + 1, 1, -1), corner_pt(b + 1, 1, 1), corner_pt(b + 1, -1, 1)]
        for s in range(4):
            sn = (s + 1) % 4
            _add_strut_between_u(mb, corners1[s], corners1[sn], 0.044, "R1_GreyMetal")
            _add_strut_between_u(mb, corners0[s], corners1[sn], 0.036, brace_mat)
            _add_strut_between_u(mb, corners0[sn], corners1[s], 0.036, brace_mat)

    # Broken dish elevation gimbal yoke partway along the leaning mast (around t = 0.45)
    yoke_p = start_u.lerp(end_u, 0.42) + side_y * 0.35
    mb.add_bevel_box((yoke_p.x, yoke_p.y, yoke_p.z), (0.42, 0.36, 0.38), bevel=0.02, mat="R1_GreyMetal", rot_deg_u=(12.0, -8.0, -26.0))
    mb.add_cylinder((yoke_p.x, yoke_p.y + 0.18, yoke_p.z - 0.18), radius=0.14, height=0.28, segs=12, axis="z", mat="R1_BareSteel", bevel=0.015)

    # Coaxial umbilical cable loops (R1_Rubber) draping across the fracture zone
    for c_off in (-0.18, 0.16):
        _add_strut_between_u(mb, (0.05, 2.15, c_off), (0.68, 1.98, c_off - 0.06), 0.042, "R1_Rubber")
        _add_strut_between_u(mb, (0.68, 1.98, c_off - 0.06), (1.35, 1.72, c_off - 0.12), 0.042, "R1_Rubber")

    # Mast tip omni-directional dipole cross-arms, lightning arrestor & amber obstruction beacon at end_u
    tip_p = end_u + spine_dir * 0.28
    _add_strut_between_u(mb, (end_u.x, end_u.y, end_u.z), (tip_p.x, tip_p.y, tip_p.z), 0.07, "R1_BareSteel")
    # Crushed tip rest block on the ground so the leaning mast sits solidly at y >= 0
    mb.add_bevel_box((end_u.x - 0.12, 0.14, end_u.z), (0.46, 0.28, 0.46), bevel=0.02, mat="R1_GreyMetal", segs=1)
    # Crossed telemetry dipole booms at tip
    for arm_dir in (side_y, side_z):
        p_a = tip_p - arm_dir * 0.62
        p_b = tip_p + arm_dir * 0.62
        p_a_clamped = (p_a.x, max(0.08, p_a.y), p_a.z)
        p_b_clamped = (p_b.x, max(0.08, p_b.y), p_b.z)
        _add_strut_between_u(mb, p_a_clamped, p_b_clamped, 0.032, "R1_BareSteel")
    mb.add_cylinder((tip_p.x, max(0.18, tip_p.y + 0.08), tip_p.z), radius=0.075, height=0.14, segs=10, axis="x", mat="R1_LampAmber", bevel=0.01)

    ruins_common.to_ruin_object(mb, "MastLeaning", parent=root)


def _add_curved_dish_sector(
    mb: r1kit.MeshBuilder,
    dc: Vector,
    axis_u: Vector,
    right_u: Vector,
    up_u: Vector,
    r0: float,
    r1: float,
    ang0: float,
    ang1: float,
    thickness: float,
    mat: str,
) -> None:
    """Add a closed, manifold 6-quad parabolic dish panel sector in Unity space."""
    def dish_pt(r: float, ang: float, z_off: float) -> Vector:
        radial = right_u * math.cos(ang) + up_u * math.sin(ang)
        depth = 0.34 * ((r / 1.16) ** 2) + z_off
        p_u = dc + radial * r + axis_u * depth
        return mb._to_local_b((p_u.x, max(0.04, p_u.y), p_u.z))

    f00 = mb.bm.verts.new(dish_pt(r0, ang0, 0.0))
    f10 = mb.bm.verts.new(dish_pt(r1, ang0, 0.0))
    f11 = mb.bm.verts.new(dish_pt(r1, ang1, 0.0))
    f01 = mb.bm.verts.new(dish_pt(r0, ang1, 0.0))

    b00 = mb.bm.verts.new(dish_pt(r0, ang0, -thickness))
    b10 = mb.bm.verts.new(dish_pt(r1, ang0, -thickness))
    b11 = mb.bm.verts.new(dish_pt(r1, ang1, -thickness))
    b01 = mb.bm.verts.new(dish_pt(r0, ang1, -thickness))

    faces = [
        mb.bm.faces.new([f00, f10, f11, f01]),
        mb.bm.faces.new([b00, b01, b11, b10]),
        mb.bm.faces.new([f00, f01, b01, b00]),
        mb.bm.faces.new([f10, b10, b11, f11]),
        mb.bm.faces.new([f00, b00, b10, f10]),
        mb.bm.faces.new([f01, f11, b11, b01]),
    ]
    bmesh.ops.recalc_face_normals(mb.bm, faces=faces)
    mb._assign_faces(faces, mat)


def _build_fallen_dish(root) -> None:
    """Build the required separate `Dish` child mesh node lying beside the tower."""
    mb = r1kit.MeshBuilder()

    # Dish center on the ground to the left-front of the tower base
    # Boresight axis_u points upward, forward (-Z), and slightly right so the concave parabolic bowl faces the viewer
    dc = Vector((-1.82, 0.96, -0.45))
    axis_u = Vector((0.28, 0.68, -0.68)).normalized()
    right_u = axis_u.cross(Vector((0.0, 1.0, 0.0))).normalized()
    up_u = right_u.cross(axis_u).normalized()

    # Ground contact support cradle / bent mounting bracket under the dish rim (ensuring y >= 0)
    mb.add_bevel_box((-1.82, 0.12, -0.42), (1.32, 0.24, 1.12), bevel=0.025, mat="R1_GreyMetal", rot_deg_u=(0.0, 22.0, 0.0), segs=1)
    mb.add_bevel_box((-1.82, 0.42, -0.22), (0.64, 0.46, 0.58), bevel=0.02, mat="R1_BareSteel", rot_deg_u=(14.0, 22.0, -8.0), segs=1)

    # Central rear hub & Gold Kapton thermal blanket wrap
    hub_back = dc - axis_u * 0.24
    _add_strut_between_u(
        mb,
        (hub_back.x, hub_back.y, hub_back.z),
        (dc.x + axis_u.x * 0.06, dc.y + axis_u.y * 0.06, dc.z + axis_u.z * 0.06),
        0.48,
        "R1_GoldFoil",
    )

    # Segmented parabolic dish reflector (12 radial sectors x 2 concentric rings forming a 2.32m concave dish)
    num_petals = 12
    d_ang = (2.0 * math.pi) / num_petals
    gap = 0.012
    for i in range(num_petals):
        a0 = i * d_ang + gap
        a1 = (i + 1) * d_ang - gap
        ang_mid = 0.5 * (a0 + a1)
        radial_dir = right_u * math.cos(ang_mid) + up_u * math.sin(ang_mid)

        # Inner ring (r = 0.22 .. 0.68)
        in_mat = "R1_GoldFoil" if i == 9 else "R1_CreamEnamel"
        _add_curved_dish_sector(mb, dc, axis_u, right_u, up_u, 0.22, 0.67, a0, a1, 0.045, in_mat)

        # Outer ring (r = 0.69 .. 1.16) — sector 9 missing outer panel to show battle/impact damage
        if i != 9:
            out_mat = "R1_OrangeStripe" if i in (0, 1, 6, 7) else "R1_CreamEnamel"
            _add_curved_dish_sector(mb, dc, axis_u, right_u, up_u, 0.69, 1.16, a0, a1, 0.045, out_mat)

        # Rear radial structural rib behind each petal
        p_rim = dc + radial_dir * 1.16 + axis_u * 0.34
        _add_strut_between_u(
            mb,
            (hub_back.x, max(0.08, hub_back.y), hub_back.z),
            (p_rim.x, max(0.08, p_rim.y), p_rim.z),
            0.042,
            "R1_GreyMetal",
        )
        # Outer circumferential rim segment
        next_ang = ((i + 1) % num_petals) * d_ang + 0.5 * d_ang
        next_dir = right_u * math.cos(next_ang) + up_u * math.sin(next_ang)
        p_rim_next = dc + next_dir * 1.16 + axis_u * 0.34
        _add_strut_between_u(
            mb,
            (p_rim.x, max(0.08, p_rim.y), p_rim.z),
            (p_rim_next.x, max(0.08, p_rim_next.y), p_rim_next.z),
            0.055,
            "R1_BareSteel",
        )

    # Central waveguide feed horn & tripod sub-reflector struts along axis_u
    horn_tip = dc + axis_u * 0.96
    _add_strut_between_u(
        mb,
        (dc.x, dc.y, dc.z),
        (horn_tip.x, horn_tip.y, horn_tip.z),
        0.082,
        "R1_Copper",
    )
    # Sub-reflector cap & brass waveguide collar at horn_tip
    sub_p0 = horn_tip - axis_u * 0.08
    sub_p1 = horn_tip + axis_u * 0.06
    _add_strut_between_u(
        mb,
        (sub_p0.x, sub_p0.y, sub_p0.z),
        (sub_p1.x, sub_p1.y, sub_p1.z),
        0.24,
        "R1_Brass",
    )
    for leg_i in range(3):
        la = math.radians(leg_i * 120.0 + 30.0)
        ldir = right_u * math.cos(la) + up_u * math.sin(la)
        lbase = dc + ldir * 0.92 + axis_u * 0.21
        _add_strut_between_u(
            mb,
            (lbase.x, max(0.10, lbase.y), lbase.z),
            (horn_tip.x, horn_tip.y, horn_tip.z),
            0.032,
            "R1_BareSteel",
        )

    # Stencil LOGO plaque on the dish mounting base cradle facing -Z
    ruins_common.add_signage_plate(
        mb,
        center_u=(-1.82, 0.42, -0.53),
        width=0.28,
        height=0.28,
        cell_name="LOGO",
        rot_deg_u=(0.0, 180.0, 0.0),
    )

    ruins_common.to_ruin_object(mb, "Dish", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("RadioTower")

    _build_tower_base(root)
    _build_leaning_mast(root)
    _build_fallen_dish(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (-1.45, 1.25, -0.78), parent=root)  # Fallen Dish feed horn
    r1kit.add_empty("Scan_2", (0.0, 1.35, -0.88), parent=root)    # Telemetry cabinet & snapped mast base

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="radio_tower",
        fbx_name="RadioTower.fbx",
        footprint_radius=4.5,
        cam_angle_deg=28.0,
        cam_elev_deg=22.0,
        skip_renders=skip_renders,
    )
    print(f"[radio_tower] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")



if __name__ == "__main__":
    build()
