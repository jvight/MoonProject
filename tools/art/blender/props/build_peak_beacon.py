"""Generate Deliverable 6 R1 story prop:
- Assets/_Project/Art/R1/Models/Props/PeakBeacon.fbx

Replaces the legacy procedural PeakBeacon model while preserving:
- Child mesh node `Mast` at pivot (0, 0, 0)
- Child mesh node `Lamp` at pivot (0, 6.72, 0) matching PeakBeaconBuilder.LampNodeName = "Lamp" and LampHeight = 6.72 m.

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_peak_beacon.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Tuple

import bpy

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402


PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"


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


def build_peak_beacon() -> bpy.types.Object:
    """Build R1 PeakBeacon (~2.25 W x 2.45 L x 6.96 H m, Lamp at Y = 6.72 m)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("PeakBeacon", pivot_u=(0.0, 0.0, 0.0))

    # 1. Mast: heavy summit base, gold-foil telemetry battery cabinet, 6.5 m lattice mast, and Earth-aimed dish at Y=4.40 m
    mast = r1kit.MeshBuilder("Mast", pivot_u=(0.0, 0.0, 0.0))

    # Octagonal summit anchor pad + 4 splayed quadruped landing legs (footprint ~1.84 x 1.84 m)
    mast.add_oct_prism((0.0, 0.05, 0.0), 1.24, 0.10, 1.24, chamfer=0.26, mat="R1_GreyMetal", bevel=0.014, bevel_segments=1)
    mast.add_oct_prism((0.0, 0.48, 0.0), 0.92, 0.12, 0.92, chamfer=0.18, mat="R1_GreyMetal", bevel=0.014, bevel_segments=1)

    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            fx = sx * 0.78
            fz = sz * 0.78
            hx = sx * 0.42
            hz = sz * 0.42
            # Circular footpad anchored in summit regolith
            mast.add_cylinder((fx, 0.025, fz), 0.13, 0.05, mat="R1_Rubber", axis_u="Y", segments=10, bevel=0.005)
            mast.add_cylinder(
                (fx, 0.065, fz),
                0.11,
                0.04,
                mat="R1_GreyMetal",
                axis_u="Y",
                segments=10,
                bevel=0.005,
                radius_top=0.08,
            )
            # Splayed tubular leg + diagonal brace
            mast.add_pipe((fx, 0.07, fz), (hx, 0.50, hz), radius=0.038, mat="R1_GreyMetal", segments=8)
            mast.add_strut((fx * 0.84, 0.10, fz * 0.84), (hx * 0.75, 0.44, hz * 0.75), 0.028, 0.028, "R1_BareSteel", bevel=0.002)

    # Lower summit RTG/battery & telemetry cabinet (Y = 0.54 .. 1.62 m)
    mast.add_bevel_box((0.0, 1.08, 0.0), (0.80, 1.08, 0.80), mat="R1_GoldFoil", bevel=0.018, segments=1)
    mast.add_bevel_box((0.0, 1.08, -0.39), (0.68, 0.88, 0.06), mat="R1_CreamEnamel", bevel=0.010, segments=1)
    mast.add_bevel_box((0.0, 1.08, 0.39), (0.68, 0.88, 0.06), mat="R1_CreamEnamel", bevel=0.010, segments=1)
    mast.add_bevel_box((0.0, 0.68, 0.0), (0.83, 0.09, 0.83), mat="R1_OrangeStripe", bevel=0.008, segments=1)
    mast.add_bevel_box((-0.405, 1.12, 0.0), (0.04, 0.42, 0.44), mat="R1_SagePatch", bevel=0.006, segments=1)
    # Auxiliary solar trickle winglet on +X cabinet face
    mast.add_bevel_box((0.46, 1.15, 0.0), (0.14, 0.56, 0.52), mat="R1_GreyMetal", bevel=0.008, segments=1, rot_deg_u=(0.0, 0.0, -22.0))
    mast.add_bevel_box((0.48, 1.16, 0.0), (0.03, 0.50, 0.46), mat="R1_SolarCell", bevel=0.004, segments=1, rot_deg_u=(0.0, 0.0, -22.0))
    # Upper cabinet transition cap at Y = 1.64 m
    mast.add_oct_prism((0.0, 1.64, 0.0), 0.84, 0.08, 0.84, chamfer=0.16, mat="R1_GreyMetal", bevel=0.012, bevel_segments=1)

    # Tapered 4-chord lattice tower from Y = 1.66 m to Y = 6.44 m (6 bays)
    y_bays = [1.66, 2.46, 3.26, 4.06, 4.86, 5.66, 6.44]

    def _half_w(y_val: float) -> float:
        t = (y_val - 1.66) / (6.44 - 1.66)
        return 0.25 * (1.0 - t) + 0.11 * t

    # 4 corner chord pipes + central copper/rubber coax feeder line
    mast.add_pipe((0.0, 1.66, -0.05), (0.0, 6.44, -0.05), radius=0.022, mat="R1_Rubber", segments=8)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            hw_bot = _half_w(1.66)
            hw_top = _half_w(6.44)
            mast.add_pipe(
                (sx * hw_bot, 1.66, sz * hw_bot),
                (sx * hw_top, 6.44, sz * hw_top),
                radius=0.022,
                mat="R1_GreyMetal",
                segments=8,
            )

    # Horizontal collars and X/Z diagonal truss struts across the 6 bays
    for b_i in range(len(y_bays) - 1):
        yb = y_bays[b_i]
        yt = y_bays[b_i + 1]
        hwb = _half_w(yb)
        hwt = _half_w(yt)
        # Horizontal ring struts at top of bay
        for side in (-1.0, 1.0):
            mast.add_strut((-hwt, yt, side * hwt), (hwt, yt, side * hwt), 0.022, 0.022, "R1_GreyMetal", bevel=0.0)
            mast.add_strut((side * hwt, yt, -hwt), (side * hwt, yt, hwt), 0.022, 0.022, "R1_GreyMetal", bevel=0.0)
        # Alternating diagonal cross-braces on all 4 faces
        flip = 1.0 if (b_i % 2 == 0) else -1.0
        for side in (-1.0, 1.0):
            mast.add_strut(
                (-flip * hwb, yb, side * hwb),
                (flip * hwt, yt, side * hwt),
                0.016,
                0.016,
                "R1_BareSteel",
                bevel=0.0,
            )
            mast.add_strut(
                (side * hwb, yb, -flip * hwb),
                (side * hwt, yt, flip * hwt),
                0.016,
                0.016,
                "R1_BareSteel",
                bevel=0.0,
            )

    # High-gain Earth-aimed parabolic dish at Y = 4.40 m (matching PeakBeaconBuilder DishHeight = 4.4 m)
    # Mounted on a heavy outrigger bracket on +Z face, pitched upward -36 deg around X toward Earth
    dish_cy = 4.40
    dish_cz = 0.42
    dish_pitch_deg = -36.0
    pitch_rad = math.radians(dish_pitch_deg)
    cp, sp = math.cos(pitch_rad), math.sin(pitch_rad)

    def _dish_world(lx: float, ly: float, lz: float) -> Tuple[float, float, float]:
        # Rotate around X by dish_pitch_deg (tilts +Z axis upward toward +Y and +Z)
        ry = ly * cp - lz * sp
        rz = ly * sp + lz * cp
        return (lx, dish_cy + ry, dish_cz + rz)

    # Heavy azimuth/elevation mount bracket connecting mast at Y=4.40 to dish hub
    mast.add_bevel_box((0.0, dish_cy, 0.18), (0.34, 0.24, 0.32), mat="R1_GreyMetal", bevel=0.008, segments=1)
    mast.add_cylinder((0.0, dish_cy, 0.34), 0.065, 0.24, mat="R1_BareSteel", axis_u="X", segments=10, bevel=0.004)

    # Stepped parabolic dish shell (outer radius = 0.92 m, depth = 0.28 m)
    mast.add_cylinder(
        _dish_world(0.0, 0.0, -0.02),
        radius=0.22,
        length=0.06,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=14,
        bevel=0.005,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )
    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.05),
        radius=0.22,
        radius_top=0.58,
        length=0.10,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel=0.005,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )
    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.16),
        radius=0.58,
        radius_top=0.90,
        length=0.12,
        mat="R1_CreamEnamel",
        axis_u="Z",
        segments=16,
        bevel=0.006,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )
    # Orange rim band on dish perimeter
    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.23),
        radius=0.91,
        radius_top=0.92,
        length=0.035,
        mat="R1_OrangeStripe",
        axis_u="Z",
        segments=16,
        bevel=0.004,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )
    # Central dark receiver aperture inside dish
    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.225),
        radius=0.38,
        length=0.018,
        mat="R1_GreyMetal",
        axis_u="Z",
        segments=14,
        bevel=0.003,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )

    # Tripod feed-horn struts + brass/copper Cassegrain sub-reflector feed horn
    feed_pt = _dish_world(0.0, 0.0, 0.78)
    for angle_deg in (90.0, 210.0, 330.0):
        rad = math.radians(angle_deg)
        p_rim = _dish_world(0.68 * math.cos(rad), 0.68 * math.sin(rad), 0.19)
        mast.add_strut(p_rim, feed_pt, 0.020, 0.020, "R1_BareSteel", bevel=0.0)

    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.80),
        radius=0.075,
        radius_top=0.048,
        length=0.11,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.004,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )
    mast.add_cylinder(
        _dish_world(0.0, 0.0, 0.74),
        radius=0.095,
        length=0.024,
        mat="R1_Brass",
        axis_u="Z",
        segments=10,
        bevel=0.003,
        rot_deg_u=(dish_pitch_deg, 0.0, 0.0),
    )

    # Top masthead beacon pedestal cup at Y = 6.44..6.52 m (directly supporting Lamp at Y = 6.72 m)
    mast.add_cylinder((0.0, 6.46, 0.0), 0.18, 0.05, mat="R1_GreyMetal", axis_u="Y", segments=14, bevel=0.005)
    mast.add_cylinder((0.0, 6.50, 0.0), 0.20, 0.04, mat="R1_Brass", axis_u="Y", segments=14, bevel=0.004)
    # 4 protective guard ribs around the Lamp globe meeting at top cap (Y = 6.96 m)
    for angle_deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(angle_deg)
        cx = math.cos(rad)
        cz = math.sin(rad)
        mast.add_tube_path(
            [
                (cx * 0.19, 6.50, cz * 0.19),
                (cx * 0.25, 6.64, cz * 0.25),
                (cx * 0.25, 6.82, cz * 0.25),
                (cx * 0.14, 6.94, cz * 0.14),
            ],
            radius=0.012,
            mat="R1_BareSteel",
            segments=6,
        )
    mast.add_cylinder((0.0, 6.95, 0.0), 0.15, 0.028, mat="R1_GreyMetal", axis_u="Y", segments=12, bevel=0.004, radius_top=0.09)

    mast.build_object(parent=root)

    # 2. Lamp: child mesh at exact (0.0, 6.72, 0.0) with name "Lamp" (matching PeakBeaconBuilder.LampNodeName = "Lamp")
    lamp = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 6.72, 0.0))
    lamp.add_cylinder(
        (0.0, 6.60, 0.0),
        0.17,
        0.12,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        radius_top=0.21,
    )
    lamp.add_cylinder(
        (0.0, 6.72, 0.0),
        0.22,
        0.14,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=16,
        bevel=0.010,
    )
    lamp.add_cylinder(
        (0.0, 6.84, 0.0),
        0.21,
        0.12,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=16,
        bevel=0.008,
        radius_top=0.15,
    )
    lamp.build_object(parent=root)

    r1kit.create_empty("Dish_Anchor", parent=root, pivot_u=(0.0, 4.40, 0.42))
    r1kit.create_empty("SummitBase_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.10)

    fbx_path = PROPS_DIR / "PeakBeacon.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_peak_beacon()


if __name__ == "__main__":
    main()
