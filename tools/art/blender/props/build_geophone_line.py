"""Generate the R1 Seismic Station Geophone Line props (`Assets/_Project/Art/R1/Models/Props/`):
1. Geophone_Sensor.fbx     (~0.12 m tall squat cylinder on 3 short spikes, faded orange cap, stencil 1/2/3)
2. Geophone_Ribbon_10m.fbx (10.0 m straight flat ribbon cable segment with Start_Anchor / End_Anchor)
3. Geophone_Plug.fbx       (flat-ribbon multi-pin telemetry plug that seats at Prop_SeismicStation)
4. Prop_GeophoneLine.fbx   (complete assembled 3-sensor geophone line with ribbon cable and station plug)

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_geophone_line.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Sequence, Tuple

import bmesh
import bpy

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"

# Flat multi-conductor seismic ribbon cable lying on the regolith:
# 5.2 cm wide, 8 mm thick (centerline at y_u = 0.005 m -> bottom at +0.001 m, top at +0.009 m).
RIBBON_WIDTH = 0.052
RIBBON_THICKNESS = 0.008
RIBBON_CENTER_Y = 0.005


def ensure_extended_r1_materials() -> None:
    """Register R1_Brass into r1kit at runtime (using only non-emissive R1 family materials)."""
    extra_specs = {
        "R1_Brass": {
            "base_color": r1kit._srgb_to_linear("#DBB052"),
            "wear_color": r1kit._srgb_to_linear("#F7D98C"),
            "dust_color": r1kit._srgb_to_linear("#8A8270"),
            "metallic": 0.92,
            "roughness": 0.24,
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


def _add_flat_ribbon_strip(
    mb: r1kit.MeshBuilder,
    points_u: Sequence[Tuple[float, float, float]],
    width: float,
    thickness: float,
    mat: str,
) -> None:
    """Sweep a 100% closed manifold chamfered flat-ribbon cross-section along a 3D path of Unity coordinates."""
    if len(points_u) < 2:
        return

    bm = mb.bm
    mat_idx = mb._mat_index(mat)
    hw = width * 0.5
    ht = thickness * 0.5
    ch_w = min(width * 0.14, 0.005)
    ch_t = min(thickness * 0.28, 0.002)

    # 8-point chamfered rectangular cross-section in local (right, up) coordinates
    profile_2d = [
        (-hw + ch_w, -ht),
        (hw - ch_w, -ht),
        (hw, -ht + ch_t),
        (hw, ht - ch_t),
        (hw - ch_w, ht),
        (-hw + ch_w, ht),
        (-hw, ht - ch_t),
        (-hw, -ht + ch_t),
    ]

    rings: List[List[bmesh.types.BMVert]] = []
    n_pts = len(points_u)
    for i, (px, py, pz) in enumerate(points_u):
        if i == 0:
            dx = points_u[1][0] - px
            dz = points_u[1][2] - pz
        elif i == n_pts - 1:
            dx = px - points_u[i - 1][0]
            dz = pz - points_u[i - 1][2]
        else:
            dx = points_u[i + 1][0] - points_u[i - 1][0]
            dz = points_u[i + 1][2] - points_u[i - 1][2]

        horiz_len = math.hypot(dx, dz)
        if horiz_len > 1e-6:
            # Tangent in XZ is (dx, dz)/horiz_len; right vector (+X when heading +Z) is (dz, -dx)/horiz_len
            rx = dz / horiz_len
            rz = -dx / horiz_len
        else:
            rx, rz = 1.0, 0.0

        ring: List[bmesh.types.BMVert] = []
        for lx, ly in profile_2d:
            vx_u = px + lx * rx
            vy_u = py + ly
            vz_u = pz + lx * rz
            ring.append(bm.verts.new(mb._to_local_b((vx_u, vy_u, vz_u))))
        rings.append(ring)

    n_prof = len(profile_2d)
    faces: List[bmesh.types.BMFace] = []
    for r_i in range(len(rings) - 1):
        r0 = rings[r_i]
        r1 = rings[r_i + 1]
        for s in range(n_prof):
            sn = (s + 1) % n_prof
            faces.append(bm.faces.new([r0[s], r0[sn], r1[sn], r1[s]]))

    faces.append(bm.faces.new(list(reversed(rings[0]))))
    faces.append(bm.faces.new(rings[-1]))

    r1kit.ensure_outward_normals(bm, per_shell=True)
    for f in faces:
        if f.is_valid:
            f.material_index = mat_idx
            f.smooth = False


def _add_flat_ribbon_with_conductors(
    mb: r1kit.MeshBuilder,
    points_u: Sequence[Tuple[float, float, float]],
    width: float = RIBBON_WIDTH,
    thickness: float = RIBBON_THICKNESS,
) -> None:
    """Build a multi-conductor flat seismic ribbon cable with raised Kapton/orange center tracer & outer conductor ribs."""
    # Main dark elastomer flat ribbon base
    _add_flat_ribbon_strip(mb, points_u, width=width, thickness=thickness, mat="R1_Rubber")

    # Center Kapton/GoldFoil insulated signal conductor rib raised 1.2 mm above top of ribbon
    center_pts = [(x, y + thickness * 0.35, z) for (x, y, z) in points_u]
    _add_flat_ribbon_strip(mb, center_pts, width=width * 0.24, thickness=thickness * 0.55, mat="R1_GoldFoil")

    # Left and right outer conductor ribs in GreyMetal
    n_pts = len(points_u)
    for side in (-1.0, 1.0):
        side_offset = side * (width * 0.31)
        side_pts: List[Tuple[float, float, float]] = []
        for i, (px, py, pz) in enumerate(points_u):
            if i == 0:
                dx = points_u[1][0] - px
                dz = points_u[1][2] - pz
            elif i == n_pts - 1:
                dx = px - points_u[i - 1][0]
                dz = pz - points_u[i - 1][2]
            else:
                dx = points_u[i + 1][0] - points_u[i - 1][0]
                dz = points_u[i + 1][2] - points_u[i - 1][2]
            hlen = math.hypot(dx, dz)
            rx, rz = (dz / hlen, -dx / hlen) if hlen > 1e-6 else (1.0, 0.0)
            side_pts.append((px + side_offset * rx, py + thickness * 0.28, pz + side_offset * rz))
        _add_flat_ribbon_strip(mb, side_pts, width=width * 0.14, thickness=thickness * 0.48, mat="R1_GreyMetal")


def _add_ribbon_end_ferrule(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    yaw_deg: float = 0.0,
) -> None:
    """Add a low-profile flat ribbon crimp ferrule & brass pin block at a segment endpoint."""
    cx, cy, cz = center_u
    rot = (0.0, yaw_deg, 0.0)
    # Flat grey-metal crimp shell
    mb.add_bevel_box(
        (cx, cy + 0.001, cz),
        (0.060, 0.012, 0.044),
        mat="R1_GreyMetal",
        bevel=0.002,
        segments=1,
        rot_deg_u=rot,
    )
    # Faded orange index stripe across top of ferrule
    mb.add_bevel_box(
        (cx, cy + 0.006, cz),
        (0.056, 0.005, 0.016),
        mat="R1_OrangeStripe",
        bevel=0.001,
        segments=1,
        rot_deg_u=rot,
    )
    # Brass alignment rivets on left and right ears
    rad = math.radians(yaw_deg)
    rx, rz = math.cos(rad), -math.sin(rad)
    for side in (-1.0, 1.0):
        mb.add_cylinder(
            (cx + side * 0.022 * rx, cy + 0.006, cz + side * 0.022 * rz),
            radius=0.0035,
            length=0.006,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.0008,
        )


def _add_ribbon_hold_down_clip(
    mb: r1kit.MeshBuilder,
    center_u: Tuple[float, float, float],
    yaw_deg: float = 0.0,
) -> None:
    """Add a low-profile regolith staple clip and settled dust clod holding down the flat ribbon cable."""
    cx, _, cz = center_u
    rot = (0.0, yaw_deg, 0.0)
    rad = math.radians(yaw_deg)
    rx, rz = math.cos(rad), -math.sin(rad)

    # Low-profile stamped grey-metal bridge strap over the 5.2 cm ribbon
    mb.add_bevel_box(
        (cx, 0.008, cz),
        (0.088, 0.012, 0.042),
        mat="R1_GreyMetal",
        bevel=0.0025,
        segments=1,
        rot_deg_u=rot,
        top_scale_xz=(0.88, 0.90),
    )
    # Faded orange identification marker tab on top of clip
    mb.add_bevel_box(
        (cx, 0.015, cz),
        (0.048, 0.005, 0.024),
        mat="R1_OrangeStripe",
        bevel=0.0015,
        segments=1,
        rot_deg_u=rot,
    )
    # Two short driven bare-steel anchor spikes on left/right flanges (sunk to y = -0.010 m)
    for side in (-1.0, 1.0):
        sx = cx + side * 0.035 * rx
        sz = cz + side * 0.035 * rz
        mb.add_cylinder(
            (sx, 0.003, sz),
            radius=0.0055,
            length=0.026,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.001,
        )
    # Small settled regolith dust patch beside the clip
    mb.add_cylinder(
        (cx + 0.020 * rx, 0.003, cz + 0.028),
        radius=0.028,
        radius_top=0.016,
        length=0.009,
        mat="R1_Basalt",
        axis_u="Y",
        segments=8,
        bevel=0.002,
    )


def _add_stencil_number_mesh(
    mb: r1kit.MeshBuilder,
    number: int,
    center_u: Tuple[float, float, float],
) -> None:
    """Build a crisp 1980s stencilled number plate (1, 2, or 3) in R1_CreamEnamel + dark R1_GreyMetal strokes."""
    cx, cy, cz = center_u

    # Chamfered dark grey-metal backing frame sitting on the faded orange cap
    mb.add_bevel_box(
        (cx, cy, cz),
        (0.021, 0.005, 0.026),
        mat="R1_GreyMetal",
        bevel=0.001,
        segments=1,
    )
    # Cream-enamel stencil background face
    mb.add_bevel_box(
        (cx, cy + 0.0018, cz),
        (0.018, 0.004, 0.023),
        mat="R1_CreamEnamel",
        bevel=0.0008,
        segments=1,
    )

    # Raised dark stencil numeral strokes on top of the cream plaque (y_top ~ cy + 0.0055)
    sy = cy + 0.0038
    sh = 0.0032
    sw = 0.0030

    if number == 1:
        # Upper and lower vertical stem segments (split by a 1.2 mm stencil bridge gap)
        mb.add_bevel_box((cx + 0.0005, sy, cz + 0.0042), (sw, sh, 0.0068), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx + 0.0005, sy, cz - 0.0036), (sw, sh, 0.0068), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        # Top nose serif bar and bottom base bar
        mb.add_bevel_box((cx - 0.0018, sy, cz + 0.0062), (0.0048, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx + 0.0005, sy, cz - 0.0072), (0.0096, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
    elif number == 2:
        # Top bar, middle bar, bottom bar
        mb.add_bevel_box((cx, sy, cz + 0.0068), (0.0110, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx, sy, cz + 0.0002), (0.0104, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx, sy, cz - 0.0068), (0.0114, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        # Upper-right vertical stroke & lower-left vertical stroke (with stencil gaps)
        mb.add_bevel_box((cx + 0.0042, sy, cz + 0.0035), (sw, sh, 0.0048), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx - 0.0042, sy, cz - 0.0035), (sw, sh, 0.0048), mat="R1_GreyMetal", bevel=0.0005, segments=1)
    else:
        # Digit 3: top bar, middle bar, bottom bar + upper-right & lower-right vertical strokes (with stencil gaps)
        mb.add_bevel_box((cx - 0.0004, sy, cz + 0.0068), (0.0110, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx + 0.0004, sy, cz + 0.0002), (0.0096, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx - 0.0004, sy, cz - 0.0068), (0.0110, sh, 0.0028), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx + 0.0042, sy, cz + 0.0035), (sw, sh, 0.0048), mat="R1_GreyMetal", bevel=0.0005, segments=1)
        mb.add_bevel_box((cx + 0.0042, sy, cz - 0.0035), (sw, sh, 0.0048), mat="R1_GreyMetal", bevel=0.0005, segments=1)


def _populate_sensor_housing_and_cap(
    housing_mb: r1kit.MeshBuilder,
    cap_mb: r1kit.MeshBuilder,
    center_xz: Tuple[float, float] = (0.0, 0.0),
) -> None:
    """Populate a geophone sensor's squat cylinder body, 3 short ground spikes, and faded orange cap (~0.12 m tall)."""
    cx, cz = center_xz

    # 1. Three short tapered BareSteel ground spikes (120 deg apart at 30, 150, 270 deg)
    #    extending down to y = -0.015 m (so total height from -0.015 m to +0.105 m is exactly 0.120 m!)
    for i in range(3):
        ang_deg = 30.0 + 120.0 * i
        ang_rad = math.radians(ang_deg)
        lx = cx + 0.056 * math.cos(ang_rad)
        lz = cz + 0.056 * math.sin(ang_rad)

        # Radial mounting lug on lower base ring
        housing_mb.add_bevel_box(
            (lx, 0.022, lz),
            (0.026, 0.018, 0.022),
            mat="R1_GreyMetal",
            bevel=0.002,
            segments=1,
            rot_deg_u=(0.0, -ang_deg, 0.0),
        )
        # Tapered ground spike driven into regolith (y = -0.015 .. +0.025 m, length = 0.040 m)
        sx = cx + 0.062 * math.cos(ang_rad)
        sz = cz + 0.062 * math.sin(ang_rad)
        housing_mb.add_cylinder(
            (sx, 0.005, sz),
            radius=0.0045,
            radius_top=0.0095,
            length=0.040,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.001,
        )
        # 6-sided hex spike drive nut on top of lug (y = 0.025 .. 0.033 m)
        housing_mb.add_cylinder(
            (sx, 0.029, sz),
            radius=0.0075,
            length=0.008,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.001,
        )
        # Small disturbed regolith collar where the spike pierces the ground
        housing_mb.add_cylinder(
            (sx, 0.003, sz),
            radius=0.016,
            radius_top=0.011,
            length=0.008,
            mat="R1_Basalt",
            axis_u="Y",
            segments=8,
            bevel=0.0015,
        )

    # 2. Lower machined GreyMetal base cup (y = 0.012 .. 0.028 m, radius = 0.056 m)
    housing_mb.add_cylinder(
        (cx, 0.020, cz),
        radius=0.056,
        length=0.016,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.0025,
    )

    # 3. Squat cylindrical CreamEnamel seismic sensor canister (y = 0.026 .. 0.076 m, radius = 0.052 m)
    housing_mb.add_cylinder(
        (cx, 0.051, cz),
        radius=0.052,
        length=0.050,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=16,
        bevel=0.0035,
    )
    # Recessed mid-body GreyMetal clamping band + Copper grounding lug
    housing_mb.add_cylinder(
        (cx, 0.050, cz),
        radius=0.054,
        length=0.010,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.0015,
    )
    housing_mb.add_bevel_box(
        (cx + 0.054, 0.036, cz),
        (0.014, 0.016, 0.018),
        mat="R1_Copper",
        bevel=0.0015,
        segments=1,
    )

    # 4. Inline flat-ribbon pass-through boots & strain-relief clamps on -Z and +Z (z = -0.090 .. +0.090 m)
    _add_flat_ribbon_with_conductors(
        housing_mb,
        [
            (cx, RIBBON_CENTER_Y, cz - 0.090),
            (cx, RIBBON_CENTER_Y + 0.003, cz - 0.045),
            (cx, RIBBON_CENTER_Y + 0.005, cz),
            (cx, RIBBON_CENTER_Y + 0.003, cz + 0.045),
            (cx, RIBBON_CENTER_Y, cz + 0.090),
        ],
    )
    for sz in (-1.0, 1.0):
        # Strain-relief gland block at z = +/- 0.062 m
        housing_mb.add_bevel_box(
            (cx, 0.012, cz + sz * 0.062),
            (0.058, 0.016, 0.028),
            mat="R1_GreyMetal",
            bevel=0.002,
            segments=1,
        )
        housing_mb.add_cylinder(
            (cx, 0.021, cz + sz * 0.062),
            radius=0.006,
            length=0.006,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.001,
        )

    # 5. Faded orange cap (`OrangeCap` mesh node, y = 0.074 .. 0.105 m):
    #    Squat chamfered R1_OrangeStripe cap dome + Brass bubble-level bezel + BareSteel top calibration stud
    cap_mb.add_cylinder(
        (cx, 0.085, cz),
        radius=0.055,
        radius_top=0.048,
        length=0.022,
        mat="R1_OrangeStripe",
        axis_u="Y",
        segments=16,
        bevel=0.004,
    )
    # Brass spirit-level bezel ring on rear-top of cap (z = +0.020 m)
    cap_mb.add_cylinder(
        (cx, 0.098, cz + 0.020),
        radius=0.014,
        length=0.006,
        mat="R1_Brass",
        axis_u="Y",
        segments=12,
        bevel=0.001,
    )
    cap_mb.add_cylinder(
        (cx, 0.100, cz + 0.020),
        radius=0.010,
        length=0.005,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=12,
        bevel=0.001,
    )
    # BareSteel top pickup collar & calibration stud reaching y = 0.105 m (so total height is -0.015 .. +0.105 = 0.120 m)
    cap_mb.add_cylinder(
        (cx - 0.022, 0.100, cz + 0.020),
        radius=0.006,
        length=0.010,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.001,
    )
    cap_mb.add_cylinder(
        (cx + 0.022, 0.100, cz + 0.020),
        radius=0.006,
        length=0.010,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.001,
    )


def apply_geophone_uvs_and_colors(
    root: bpy.types.Object,
    footprint_radius: float = 0.35,
) -> None:
    """Project UVs and bake vertex colors (R=AO, G=Wear, B=Dust) with sun-faded orange cap and dusty ribbon crown."""
    r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)

    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or "Col" not in obj.data.color_attributes:
            continue
        mesh = obj.data
        attr = mesh.color_attributes["Col"]
        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        for poly in mesh.polygons:
            n_w = (rot_mat @ poly.normal).normalized()
            mat_name = (
                mesh.materials[poly.material_index].name
                if poly.material_index < len(mesh.materials) and mesh.materials[poly.material_index]
                else ""
            )
            is_orange = mat_name == "R1_OrangeStripe"
            is_rubber = mat_name == "R1_Rubber"

            for li in poly.loop_indices:
                v_w = world_mat @ mesh.vertices[mesh.loops[li].vertex_index].co
                r, g, b, a = attr.data[li].color

                sunk_factor = max(0.0, min(1.0, (0.012 - v_w.z) / 0.022))
                top_factor = max(0.0, min(1.0, (n_w.z - 0.15) / 0.75))

                if is_orange:
                    # Sun-faded orange cap: elevated G (wear -> pale bleached cream-orange) + moderate B (lunar dust)
                    r_new = max(0.82, min(1.0, r * 0.96))
                    g_new = max(0.58, min(0.92, g * 0.65 + 0.52))
                    b_new = max(0.24, min(0.62, b * 0.60 + 0.26 * top_factor))
                elif is_rubber:
                    # Flat ribbon cable lying in regolith: dusty top crown
                    r_new = max(0.20, min(1.0, r * (0.90 - 0.25 * sunk_factor)))
                    g_new = max(0.0, min(0.18, g * (1.0 - 0.70 * top_factor)))
                    b_new = max(0.32, min(1.0, b + 0.42 * max(top_factor, 0.85 * sunk_factor)))
                else:
                    r_new = max(0.22, min(1.0, r * (0.92 - 0.22 * sunk_factor)))
                    g_new = max(0.0, min(1.0, g * 1.10))
                    b_new = max(0.0, min(1.0, b + 0.25 * max(top_factor, sunk_factor)))

                attr.data[li].color = (r_new, g_new, b_new, a)


def build_geophone_sensor() -> bpy.types.Object:
    """Build `Geophone_Sensor.fbx`: ~0.12 m tall squat cylinder on 3 short spikes, faded orange cap, stencil 1/2/3."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Geophone_Sensor", pivot_u=(0.0, 0.0, 0.0))

    housing_mb = r1kit.MeshBuilder("SensorHousing", pivot_u=(0.0, 0.0, 0.0))
    cap_mb = r1kit.MeshBuilder("OrangeCap", pivot_u=(0.0, 0.080, 0.0))
    _populate_sensor_housing_and_cap(housing_mb, cap_mb, (0.0, 0.0))
    housing_mb.build_object(parent=root)
    cap_mb.build_object(parent=root)

    # Three stencil number child meshes (Stencil_1, Stencil_2, Stencil_3) across the front-top of the faded orange cap
    stencil_positions = (
        (1, (-0.024, 0.096, -0.010)),
        (2, (0.000, 0.096, -0.012)),
        (3, (0.024, 0.096, -0.010)),
    )
    for num, pos_u in stencil_positions:
        st_mb = r1kit.MeshBuilder(f"Stencil_{num}", pivot_u=pos_u)
        _add_stencil_number_mesh(st_mb, num, pos_u)
        st_mb.build_object(parent=root)

    # Anchors
    r1kit.create_empty("Base_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, -0.090))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, 0.090))
    r1kit.create_empty("Cap_Anchor", parent=root, pivot_u=(0.0, 0.105, 0.0))

    apply_geophone_uvs_and_colors(root, footprint_radius=0.18)

    fbx_path = PROPS_DIR / "Geophone_Sensor.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_geophone_ribbon_10m() -> bpy.types.Object:
    """Build `Geophone_Ribbon_10m.fbx`: 10.0 m straight flat ribbon cable segment with Start_Anchor and End_Anchor."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Geophone_Ribbon_10m", pivot_u=(0.0, 0.0, 0.0))

    # 1. RibbonRun: 10.0 m flat multi-conductor ribbon cable from z = -5.00 m to z = +5.00 m
    ribbon_mb = r1kit.MeshBuilder("RibbonRun", pivot_u=(0.0, 0.0, 0.0))
    pts: List[Tuple[float, float, float]] = []
    n_steps = 40
    for i in range(n_steps + 1):
        t = i / n_steps
        z = -5.00 + 10.00 * t
        # Subtle ground-settled meander that vanishes at t = 0, 0.25, 0.50, 0.75, 1.00
        meander_x = 0.010 * math.sin(4.0 * math.pi * t) * math.sin(math.pi * t)
        pts.append((meander_x, RIBBON_CENTER_Y, z))

    _add_flat_ribbon_with_conductors(ribbon_mb, pts)

    # End crimp ferrules at z = -4.972 m and z = +4.972 m
    _add_ribbon_end_ferrule(ribbon_mb, (0.0, RIBBON_CENTER_Y, -4.972), yaw_deg=0.0)
    _add_ribbon_end_ferrule(ribbon_mb, (0.0, RIBBON_CENTER_Y, 4.972), yaw_deg=180.0)
    ribbon_mb.build_object(parent=root)

    # 2. RibbonClips: low-profile regolith hold-down staple clips & dust patches at z = -2.50, 0.00, +2.50 m
    clips_mb = r1kit.MeshBuilder("RibbonClips", pivot_u=(0.0, 0.0, 0.0))
    for z_clip in (-2.50, 0.00, 2.50):
        _add_ribbon_hold_down_clip(clips_mb, (0.0, 0.0, z_clip), yaw_deg=0.0)
    clips_mb.build_object(parent=root)

    # Child anchors matching the cable kit convention (10.0 m span from Start_Anchor to End_Anchor)
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, -5.00))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, 5.00))
    r1kit.create_empty("Clip_Anchor", parent=root, pivot_u=(0.0, 0.018, 0.0))
    r1kit.create_empty("Center_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, 0.0))

    apply_geophone_uvs_and_colors(root, footprint_radius=0.45)

    fbx_path = PROPS_DIR / "Geophone_Ribbon_10m.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def _populate_geophone_plug(
    lead_mb: r1kit.MeshBuilder,
    plug_mb: r1kit.MeshBuilder,
    z_offset: float = 0.0,
) -> None:
    """Populate the flat-ribbon ground strain-relief lead and ALSEP multi-pin telemetry plug head."""
    # 1. RibbonLead: flat ribbon cable from z = -0.35 m up to plug neck at z = +0.10 m, y = 0.065 m
    lead_pts: List[Tuple[float, float, float]] = [
        (0.0, RIBBON_CENTER_Y, z_offset - 0.35),
        (0.0, RIBBON_CENTER_Y, z_offset - 0.14),
        (0.0, 0.018, z_offset - 0.04),
        (0.0, 0.045, z_offset + 0.04),
        (0.0, 0.065, z_offset + 0.11),
    ]
    _add_flat_ribbon_with_conductors(lead_mb, lead_pts)
    _add_ribbon_end_ferrule(lead_mb, (0.0, RIBBON_CENTER_Y, z_offset - 0.325), yaw_deg=0.0)

    # Staked ground strain-relief clamp block at z = -0.14 m
    lead_mb.add_bevel_box(
        (0.0, 0.012, z_offset - 0.14),
        (0.116, 0.024, 0.088),
        mat="R1_GreyMetal",
        bevel=0.003,
        segments=1,
    )
    lead_mb.add_bevel_box(
        (0.0, 0.026, z_offset - 0.14),
        (0.082, 0.010, 0.068),
        mat="R1_CreamEnamel",
        bevel=0.002,
        segments=1,
    )
    lead_mb.add_bevel_box(
        (0.0, 0.015, z_offset - 0.14),
        (0.120, 0.010, 0.028),
        mat="R1_OrangeStripe",
        bevel=0.0015,
        segments=1,
    )
    for sx in (-1.0, 1.0):
        bx = sx * 0.044
        lead_mb.add_cylinder(
            (bx, 0.008, z_offset - 0.14),
            radius=0.006,
            length=0.046,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.001,
        )
        lead_mb.add_cylinder(
            (bx, 0.032, z_offset - 0.14),
            radius=0.009,
            length=0.006,
            mat="R1_Brass",
            axis_u="Y",
            segments=8,
            bevel=0.001,
        )

    # 2. PlugHead: flat multi-pin ALSEP telemetry plug at y = 0.065 m (seats into Prop_SeismicStation CentralBox)
    py = 0.065
    # Tapered elastomer ribbon-to-plug boot (z = +0.095 .. +0.135 m)
    plug_mb.add_bevel_box(
        (0.0, py, z_offset + 0.115),
        (0.068, 0.028, 0.042),
        mat="R1_Rubber",
        bevel=0.003,
        segments=1,
    )
    # Main machined GreyMetal rectangular connector body (z = +0.135 .. +0.215 m)
    plug_mb.add_bevel_box(
        (0.0, py, z_offset + 0.175),
        (0.096, 0.044, 0.080),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=2,
    )
    # CreamEnamel top cover plate & faded OrangeStripe collar band
    plug_mb.add_bevel_box(
        (0.0, py + 0.022, z_offset + 0.175),
        (0.082, 0.008, 0.066),
        mat="R1_CreamEnamel",
        bevel=0.002,
        segments=1,
    )
    plug_mb.add_bevel_box(
        (0.0, py, z_offset + 0.152),
        (0.100, 0.046, 0.020),
        mat="R1_OrangeStripe",
        bevel=0.002,
        segments=1,
    )
    # Top EVA pull-loop handle (y = 0.088 .. 0.106 m)
    for sx in (-0.024, 0.024):
        plug_mb.add_cylinder(
            (sx, py + 0.032, z_offset + 0.165),
            radius=0.0045,
            length=0.018,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=8,
            bevel=0.001,
        )
    plug_mb.add_bevel_box(
        (0.0, py + 0.040, z_offset + 0.165),
        (0.062, 0.009, 0.022),
        mat="R1_OrangeStripe",
        bevel=0.002,
        segments=1,
    )
    # Side captive jackscrews (BareSteel + Brass collars)
    for sx in (-1.0, 1.0):
        jx = sx * 0.054
        plug_mb.add_cylinder(
            (jx, py, z_offset + 0.182),
            radius=0.007,
            length=0.072,
            mat="R1_BareSteel",
            axis_u="Z",
            segments=8,
            bevel=0.001,
        )
        plug_mb.add_cylinder(
            (jx, py, z_offset + 0.222),
            radius=0.009,
            length=0.014,
            mat="R1_Brass",
            axis_u="Z",
            segments=8,
            bevel=0.001,
        )
    # Mating shroud & DarkGlass pin insulator block + 4 Copper/Brass ribbon contact pins (z = +0.215 .. +0.250 m)
    plug_mb.add_bevel_box(
        (0.0, py, z_offset + 0.224),
        (0.076, 0.032, 0.022),
        mat="R1_BareSteel",
        bevel=0.002,
        segments=1,
    )
    plug_mb.add_bevel_box(
        (0.0, py, z_offset + 0.236),
        (0.066, 0.022, 0.008),
        mat="R1_DarkGlass",
        bevel=0.001,
        segments=1,
    )
    for px in (-0.021, -0.007, 0.007, 0.021):
        plug_mb.add_cylinder(
            (px, py, z_offset + 0.244),
            radius=0.004,
            length=0.014,
            mat="R1_Copper",
            axis_u="Z",
            segments=8,
            bevel=0.0008,
        )


def build_geophone_plug() -> bpy.types.Object:
    """Build `Geophone_Plug.fbx`: flat-ribbon telemetry plug end that seats at Prop_SeismicStation."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Geophone_Plug", pivot_u=(0.0, 0.0, 0.0))

    lead_mb = r1kit.MeshBuilder("RibbonLead", pivot_u=(0.0, 0.0, 0.0))
    plug_mb = r1kit.MeshBuilder("PlugHead", pivot_u=(0.0, 0.065, 0.175))
    _populate_geophone_plug(lead_mb, plug_mb, z_offset=0.0)
    lead_mb.build_object(parent=root)
    plug_mb.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, -0.35))
    r1kit.create_empty("Plug_Anchor", parent=root, pivot_u=(0.0, 0.065, 0.25))
    r1kit.create_empty("Handle_Anchor", parent=root, pivot_u=(0.0, 0.105, 0.165))

    apply_geophone_uvs_and_colors(root, footprint_radius=0.28)

    fbx_path = PROPS_DIR / "Geophone_Plug.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_prop_geophone_line() -> bpy.types.Object:
    """Build `Prop_GeophoneLine.fbx`: assembled 3-sensor geophone line (sensors 1, 2, 3 + ribbon cable + station plug)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Prop_GeophoneLine", pivot_u=(0.0, 0.0, 0.0))

    # 1. Station plug at z = -1.35 m (mating head at z = -1.60 m pointing -Z toward station, ribbon running +Z)
    plug_mb = r1kit.MeshBuilder("StationPlug", pivot_u=(0.0, 0.065, -1.40))
    lead_mb = r1kit.MeshBuilder("RibbonCable", pivot_u=(0.0, 0.0, 0.0))
    _populate_geophone_plug(lead_mb, plug_mb, z_offset=-1.60)

    # Continuous flat ribbon cable connecting the plug through Sensor_1 (z = -0.50), Sensor_2 (z = +0.50), Sensor_3 (z = +1.50)
    ribbon_pts: List[Tuple[float, float, float]] = []
    n_steps = 36
    for i in range(n_steps + 1):
        t = i / n_steps
        z = -1.25 + 2.85 * t
        meander_x = 0.008 * math.sin(3.0 * math.pi * t) * math.sin(math.pi * t)
        ribbon_pts.append((meander_x, RIBBON_CENTER_Y, z))
    _add_flat_ribbon_with_conductors(lead_mb, ribbon_pts)
    for z_clip in (-0.95, 0.00, 1.00):
        _add_ribbon_hold_down_clip(lead_mb, (0.0, 0.0, z_clip), yaw_deg=0.0)

    lead_mb.build_object(parent=root)
    plug_mb.build_object(parent=root)

    # 2. Three numbered geophone sensors (Sensor_1, Sensor_2, Sensor_3) at z = -0.50, +0.50, +1.50 m
    for idx, z_s in ((1, -0.50), (2, 0.50), (3, 1.50)):
        s_mb = r1kit.MeshBuilder(f"Sensor_{idx}", pivot_u=(0.0, 0.0, z_s))
        _populate_sensor_housing_and_cap(s_mb, s_mb, (0.0, z_s))
        _add_stencil_number_mesh(s_mb, idx, (0.0, 0.096, z_s - 0.010))
        s_mb.build_object(parent=root)
        r1kit.create_empty(f"Sensor_{idx}_Anchor", parent=root, pivot_u=(0.0, 0.0, z_s))

    r1kit.create_empty("Plug_Anchor", parent=root, pivot_u=(0.0, 0.065, -1.35))
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, -1.95))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, RIBBON_CENTER_Y, 1.60))

    apply_geophone_uvs_and_colors(root, footprint_radius=0.55)

    fbx_path = PROPS_DIR / "Prop_GeophoneLine.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_geophone_sensor()
    build_geophone_ribbon_10m()
    build_geophone_plug()
    build_prop_geophone_line()


if __name__ == "__main__":
    main()
