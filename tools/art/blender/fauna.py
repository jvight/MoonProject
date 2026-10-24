"""Generate the four 0.6 Machine Fauna R1 models (`Assets/_Project/Art/R1/Models/Fauna/`):
1. PanelMite.fbx     — Palm-sized 6-legged solar-panel dust-wiper crawler (Lumen family).
2. Sweeper.fbx       — Slow wheeled dust sweeper with wide front rotary brush head (~1.2x 07's length, Lumen family).
3. SurveyDrone.fbx   — Small hexagonal cold-gas thruster drone with 4 De Laval nozzles (Tycho / Meridian family).
4. Trencher.fbx      — Slow tracked crawler with front trenching cutter wheel and rear cable spool (bigger than 07, Lumen family).

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/fauna.py
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import bmesh
import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
REPO_ROOT = THIS_DIR.parents[2]

if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402

FAUNA_MODELS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Fauna"
RENDERS_DIR = REPO_ROOT / "Logs" / "renders" / "fauna"


def ensure_extended_r1_materials() -> None:
    """Register R1_WhitePaint, R1_TealMark, and R1_SilverFoil into r1kit at runtime."""
    extra_specs = {
        "R1_WhitePaint": {
            "base_color": r1kit._srgb_to_linear("#E8EAE6"),
            "wear_color": r1kit._srgb_to_linear("#C2C7CC"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.02,
            "roughness": 0.82,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_TealMark": {
            "base_color": r1kit._srgb_to_linear("#2F6E73"),
            "wear_color": r1kit._srgb_to_linear("#A8BDC2"),
            "dust_color": r1kit._srgb_to_linear("#788086"),
            "metallic": 0.04,
            "roughness": 0.65,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_SilverFoil": {
            "base_color": r1kit._srgb_to_linear("#C9CED2"),
            "wear_color": r1kit._srgb_to_linear("#EBF0F5"),
            "dust_color": r1kit._srgb_to_linear("#8A8E94"),
            "metallic": 0.85,
            "roughness": 0.45,
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


def _boost_top_dust_cap(root_obj: bpy.types.Object, top_boost: float = 0.38) -> None:
    """Enhance upward-facing vertex colour B (cavity/top dust) so every fauna shell reads as dust-capped on the Moon."""
    for obj in r1kit.collect_hierarchy(root_obj):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        if "Col" not in mesh.color_attributes:
            continue
        attr = mesh.color_attributes["Col"]
        rot_mat = obj.matrix_world.to_3x3().normalized()
        for poly in mesh.polygons:
            world_n = (rot_mat @ poly.normal).normalized()
            # In Blender Z-up, world_n.z > 0.35 means upward-facing top surface
            up_factor = max(0.0, (world_n.z - 0.30) / 0.70)
            if up_factor <= 0.0:
                continue
            add_dust = top_boost * (up_factor ** 1.35)
            for loop_idx in poly.loop_indices:
                r, g, b, a = attr.data[loop_idx].color
                attr.data[loop_idx].color = (
                    r,
                    g,
                    min(1.0, max(b, b + add_dust)),
                    a,
                )


def _finalize_and_export(
    root: bpy.types.Object,
    fbx_name: str,
    footprint_radius: float,
    render_previews: bool = False,
    preview_slug: str = "",
) -> Path:
    """Apply UVs, prune unused materials, bake dust-capped vertex colors, and export FBX."""
    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=footprint_radius)
    _boost_top_dust_cap(root, top_boost=0.36)

    FAUNA_MODELS_DIR.mkdir(parents=True, exist_ok=True)
    fbx_path = FAUNA_MODELS_DIR / fbx_name
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")

    if render_previews and preview_slug:
        RENDERS_DIR.mkdir(parents=True, exist_ok=True)
        r1kit.render_turntable(root, RENDERS_DIR, preview_slug)

    return fbx_path


# ---------------------------------------------------------------------------
# 1. PANEL MITE (PanelMite.fbx)
#    Palm-sized 6-legged solar-panel service crawler (Lumen family).
#    Target size: ~0.28 m W (X) x 0.32 m L (Z_u / Y_bl) x 0.135 m H (Y_u / Z_bl)
#    Child pivots: Body, Lamp, Wiper, Leg_L0, Leg_L1, Leg_L2, Leg_R0, Leg_R1, Leg_R2
# ---------------------------------------------------------------------------
def build_panel_mite(render_previews: bool = False) -> bpy.types.Object:
    """Build Species 1: PanelMite (~0.28 m W x 0.32 m L x 0.135 m H)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("PanelMite", pivot_u=(0.0, 0.0, 0.0))

    # --- Body: rounded cream carapace, faded orange collar, solar cell dorsal inset, rubber underbelly ---
    body_mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.056, 0.0))

    # Dark rubber underbelly chassis pan (y = 0.024 .. 0.044 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.034, -0.004),
        size_u=(0.124, 0.020, 0.176),
        mat="R1_Rubber",
        bevel=0.004,
        segments=2,
    )

    # Faded coral-orange perimeter bumper band (y = 0.044 .. 0.060 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.052, -0.002),
        size_u=(0.144, 0.018, 0.198),
        mat="R1_OrangeStripe",
        bevel=0.005,
        segments=2,
    )

    # Warm cream enamel upper carapace shell (y = 0.058 .. 0.104 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.081, -0.004),
        size_u=(0.136, 0.046, 0.184),
        mat="R1_CreamEnamel",
        bevel=0.012,
        segments=2,
    )

    # Dorsal solar-cell recharge patch inset on the carapace back (y = 0.103 .. 0.114 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.108, -0.014),
        size_u=(0.096, 0.010, 0.116),
        mat="R1_GreyMetal",
        bevel=0.002,
        segments=1,
    )
    body_mb.add_bevel_box(
        center_u=(0.0, 0.113, -0.014),
        size_u=(0.084, 0.006, 0.102),
        mat="R1_SolarCell",
        bevel=0.0015,
        segments=1,
    )
    # Brass charge-contact strip at rear of solar patch
    body_mb.add_bevel_box(
        center_u=(0.0, 0.114, -0.072),
        size_u=(0.058, 0.008, 0.014),
        mat="R1_Brass",
        bevel=0.0015,
        segments=1,
    )

    # Short rear telemetry stub pin (y = 0.100 .. 0.135 m)
    body_mb.add_cylinder(
        center_u=(-0.036, 0.118, -0.074),
        radius=0.005,
        radius_top=0.0035,
        length=0.034,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.001,
    )

    # Forward nose visor cowl framing the single warm lamp (z = +0.082 .. +0.108 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.074, 0.094),
        size_u=(0.072, 0.034, 0.028),
        mat="R1_GreyMetal",
        bevel=0.004,
        segments=1,
    )
    body_mb.add_cylinder(
        center_u=(0.0, 0.074, 0.108),
        radius=0.019,
        length=0.010,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel=0.002,
    )

    body_mb.build_object(parent=root)

    # --- Lamp: single tiny warm amber eye in the front nose bezel ---
    lamp_mb = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 0.074, 0.114))
    lamp_mb.add_cylinder(
        center_u=(0.0, 0.074, 0.114),
        radius=0.014,
        radius_top=0.011,
        length=0.010,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=12,
        bevel=0.002,
    )
    lamp_mb.build_object(parent=root)

    # --- Wiper: front electrostatic dust-wiper bar spanning across the nose ---
    wiper_mb = r1kit.MeshBuilder("Wiper", pivot_u=(0.0, 0.028, 0.106))
    # Twin forward linkage struts from nose underside to wiper bar
    for sx in (-1.0, 1.0):
        wiper_mb.add_strut(
            p0_u=(sx * 0.038, 0.032, 0.088),
            p1_u=(sx * 0.052, 0.018, 0.142),
            width=0.010,
            thickness=0.009,
            mat="R1_BareSteel",
            bevel=0.0015,
        )
    # Crossbar carrier (z = 0.144 m, width = 0.210 m)
    wiper_mb.add_bevel_box(
        center_u=(0.0, 0.018, 0.146),
        size_u=(0.210, 0.012, 0.018),
        mat="R1_GreyMetal",
        bevel=0.002,
        segments=1,
    )
    # Copper electrostatic discharge wire strip along top of wiper bar
    wiper_mb.add_bevel_box(
        center_u=(0.0, 0.025, 0.146),
        size_u=(0.194, 0.006, 0.010),
        mat="R1_Copper",
        bevel=0.001,
        segments=1,
    )
    # Rubber squeegee blade skimming the panel surface (bottom at y = 0.003 m)
    wiper_mb.add_bevel_box(
        center_u=(0.0, 0.008, 0.148),
        size_u=(0.204, 0.010, 0.012),
        mat="R1_Rubber",
        bevel=0.0015,
        segments=1,
    )
    wiper_mb.build_object(parent=root)

    # --- 6 Articulated Legs: Leg_L0, Leg_L1, Leg_L2, Leg_R0, Leg_R1, Leg_R2 ---
    leg_specs = [
        ("Leg_L0", -1.0, 0.058, 0.072),
        ("Leg_L1", -1.0, 0.000, 0.000),
        ("Leg_L2", -1.0, -0.058, -0.074),
        ("Leg_R0", 1.0, 0.058, 0.072),
        ("Leg_R1", 1.0, 0.000, 0.000),
        ("Leg_R2", 1.0, -0.058, -0.074),
    ]
    for leg_name, side, hip_z, foot_z in leg_specs:
        hip_x = side * 0.068
        hip_y = 0.048
        knee_x = side * 0.106
        knee_y = 0.068
        knee_z = 0.55 * hip_z + 0.45 * foot_z
        foot_x = side * (0.132 if abs(hip_z) < 0.01 else 0.124)

        leg_mb = r1kit.MeshBuilder(leg_name, pivot_u=(hip_x, hip_y, hip_z))
        # Brass hip socket drum
        leg_mb.add_cylinder(
            center_u=(hip_x, hip_y, hip_z),
            radius=0.011,
            length=0.018,
            mat="R1_Brass",
            axis_u="Z",
            segments=10,
            bevel=0.002,
        )
        # Upper femur strut (hip -> knee)
        leg_mb.add_strut(
            p0_u=(hip_x, hip_y, hip_z),
            p1_u=(knee_x, knee_y, knee_z),
            width=0.012,
            thickness=0.012,
            mat="R1_GreyMetal",
            bevel=0.002,
        )
        # Rubber knee bellows joint
        leg_mb.add_cylinder(
            center_u=(knee_x, knee_y, knee_z),
            radius=0.010,
            length=0.016,
            mat="R1_Rubber",
            axis_u="Z",
            segments=10,
            bevel=0.002,
        )
        # Lower tibia needle strut (knee -> ankle at y = 0.010)
        leg_mb.add_strut(
            p0_u=(knee_x, knee_y, knee_z),
            p1_u=(foot_x, 0.010, foot_z),
            width=0.010,
            thickness=0.010,
            mat="R1_BareSteel",
            bevel=0.0015,
        )
        # Magnetic / rubberized foot pad resting cleanly on ground y = 0.000 m
        leg_mb.add_cylinder(
            center_u=(foot_x, 0.005, foot_z),
            radius=0.014,
            radius_top=0.011,
            length=0.010,
            mat="R1_Rubber",
            axis_u="Y",
            segments=10,
            bevel=0.0015,
        )
        leg_mb.build_object(parent=root)

    _finalize_and_export(
        root,
        fbx_name="PanelMite.fbx",
        footprint_radius=0.22,
        render_previews=render_previews,
        preview_slug="panel_mite",
    )
    return root


# ---------------------------------------------------------------------------
# 2. THE SWEEPER (Sweeper.fbx)
#    Slow wheeled dust sweeper with a wide brush head (~1.2x 07's length, Lumen family).
#    Rover 07 length = 2.014 m -> Sweeper length = ~2.42 m, width = ~1.84 m, height = ~1.18 m.
#    Child pivots: Body, Lamp, BrushArm, Brush, Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR
# ---------------------------------------------------------------------------
def build_sweeper(render_previews: bool = False) -> bpy.types.Object:
    """Build Species 2: Sweeper (~1.84 m W x 2.42 m L x 1.18 m H, ~1.2x 07's length)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Sweeper", pivot_u=(0.0, 0.0, 0.0))

    # --- Body: rounded cream-enamel utility hull & rear regolith hopper ---
    body_mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.46, -0.18))

    # Lower structural frame chassis (z = -1.18 .. +0.58 m, y = 0.18 .. 0.36 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.27, -0.30),
        size_u=(1.04, 0.18, 1.76),
        mat="R1_GreyMetal",
        bevel=0.025,
        segments=2,
    )
    # Rear heavy bumper bar + tow clevis (extends back to z = -1.21 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.28, -1.16),
        size_u=(1.14, 0.12, 0.10),
        mat="R1_BareSteel",
        bevel=0.015,
        segments=2,
    )

    # Faded coral-orange wrap-around beltline bumper band (y = 0.36 .. 0.48 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.42, -0.26),
        size_u=(1.18, 0.12, 1.74),
        mat="R1_OrangeStripe",
        bevel=0.022,
        segments=2,
    )

    # Main warm cream enamel upper cowling & dust collection hopper (y = 0.47 .. 0.90 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.68, -0.28),
        size_u=(1.12, 0.44, 1.66),
        mat="R1_CreamEnamel",
        bevel=0.065,
        segments=3,
    )

    # Sloped rear hopper cleanout hatch & louvred radiator deck (y = 0.88 .. 1.02 m, z = -0.96 .. -0.18 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.94, -0.56),
        size_u=(0.88, 0.14, 0.78),
        mat="R1_CreamEnamel",
        bevel=0.028,
        segments=2,
    )
    # Bare-steel radiator ribs on rear hopper lid
    for r_i in range(4):
        rz = -0.82 + r_i * 0.16
        body_mb.add_bevel_box(
            center_u=(0.0, 1.015, rz),
            size_u=(0.66, 0.024, 0.055),
            mat="R1_BareSteel",
            bevel=0.005,
            segments=1,
        )

    # Twin side cyclonic dust-separator canisters + copper transfer pipes
    for sx in (-1.0, 1.0):
        body_mb.add_cylinder(
            center_u=(sx * 0.48, 0.76, -0.08),
            radius=0.14,
            length=0.44,
            mat="R1_GreyMetal",
            axis_u="Y",
            segments=14,
            bevel=0.012,
        )
        body_mb.add_cylinder(
            center_u=(sx * 0.48, 0.99, -0.08),
            radius=0.12,
            radius_top=0.09,
            length=0.06,
            mat="R1_Brass",
            axis_u="Y",
            segments=14,
            bevel=0.008,
        )
        # Copper dust suction pipe running forward toward the brush yoke
        body_mb.add_strut(
            p0_u=(sx * 0.46, 0.56, 0.48),
            p1_u=(sx * 0.48, 0.78, -0.02),
            width=0.058,
            thickness=0.058,
            mat="R1_Copper",
            bevel=0.008,
        )

    # Forward sensor turret hood (y = 0.86 .. 1.18 m, z = +0.06 .. +0.52 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 1.02, 0.28),
        size_u=(0.56, 0.32, 0.46),
        mat="R1_CreamEnamel",
        bevel=0.042,
        segments=2,
    )
    # Dark-glass optical road-follower slit below the round lamp bezel
    body_mb.add_bevel_box(
        center_u=(0.0, 0.93, 0.51),
        size_u=(0.36, 0.065, 0.030),
        mat="R1_DarkGlass",
        bevel=0.006,
        segments=1,
    )
    # Brass mounting bezel for the single warm hood lamp
    body_mb.add_cylinder(
        center_u=(0.0, 1.06, 0.51),
        radius=0.078,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.006,
    )

    body_mb.build_object(parent=root)

    # --- Lamp: single warm amber eye in the forward hood bezel ---
    lamp_mb = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 1.06, 0.53))
    lamp_mb.add_cylinder(
        center_u=(0.0, 1.06, 0.53),
        radius=0.058,
        radius_top=0.048,
        length=0.028,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel=0.005,
    )
    lamp_mb.build_object(parent=root)

    # --- 4 Rugged Lunar Utility Wheels: Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR ---
    # Radius = 0.28 m (bottom at y = 0.000 m, center at y = 0.280 m)
    wheel_specs = [
        ("Wheel_FL", -0.66, 0.28, 0.26),
        ("Wheel_FR", 0.66, 0.28, 0.26),
        ("Wheel_RL", -0.66, 0.28, -0.76),
        ("Wheel_RR", 0.66, 0.28, -0.76),
    ]
    for w_name, wx, wy, wz in wheel_specs:
        w_mb = r1kit.MeshBuilder(w_name, pivot_u=(wx, wy, wz))
        # Main rubber tyre cylinder (radius = 0.280 m, width = 0.20 m)
        w_mb.add_cylinder(
            center_u=(wx, wy, wz),
            radius=0.280,
            length=0.20,
            mat="R1_Rubber",
            axis_u="X",
            segments=20,
            bevel=0.024,
            bevel_segments=2,
        )
        # Grey metal wheel dish
        w_mb.add_cylinder(
            center_u=(wx, wy, wz),
            radius=0.175,
            length=0.216,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=16,
            bevel=0.012,
        )
        # Brass axle hubcap
        w_mb.add_cylinder(
            center_u=(wx, wy, wz),
            radius=0.075,
            length=0.232,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.008,
        )
        w_mb.build_object(parent=root)

    # --- BrushArm: articulated twin-arm yoke + wide 1.84 m front sweeper hood ---
    arm_mb = r1kit.MeshBuilder("BrushArm", pivot_u=(0.0, 0.36, 0.52))
    for sx in (-1.0, 1.0):
        # Main lift arm strut from chassis front (z=0.48) to wide hood endplate (z=0.96)
        arm_mb.add_strut(
            p0_u=(sx * 0.42, 0.36, 0.48),
            p1_u=(sx * 0.78, 0.26, 0.94),
            width=0.085,
            thickness=0.075,
            mat="R1_GreyMetal",
            bevel=0.010,
        )
        # Upper hydraulic leveling cylinder (BareSteel)
        arm_mb.add_strut(
            p0_u=(sx * 0.38, 0.52, 0.46),
            p1_u=(sx * 0.68, 0.38, 0.88),
            width=0.048,
            thickness=0.048,
            mat="R1_BareSteel",
            bevel=0.006,
        )
        # Side endplate cheek of the wide sweeper head (x = ±0.89 m, z = 0.74 .. 1.21 m)
        arm_mb.add_bevel_box(
            center_u=(sx * 0.88, 0.24, 0.97),
            size_u=(0.08, 0.34, 0.48),
            mat="R1_GreyMetal",
            bevel=0.012,
            segments=2,
        )
        # Brass bearing housing on outer cheek
        arm_mb.add_cylinder(
            center_u=(sx * 0.92, 0.20, 0.98),
            radius=0.072,
            length=0.040,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.006,
        )

    # Wide curved/bevelled upper dust shroud spanning x = -0.88 .. +0.88 m (total width = 1.84 m)
    arm_mb.add_bevel_box(
        center_u=(0.0, 0.38, 0.97),
        size_u=(1.76, 0.11, 0.44),
        mat="R1_CreamEnamel",
        bevel=0.024,
        segments=2,
    )
    # Faded orange hazard bumper across the front nose of the brush hood (z = +1.15 .. +1.21 m)
    arm_mb.add_bevel_box(
        center_u=(0.0, 0.32, 1.17),
        size_u=(1.80, 0.085, 0.08),
        mat="R1_OrangeStripe",
        bevel=0.014,
        segments=2,
    )
    # Rear flexible rubber dust-curtain baffle behind the brush roller
    arm_mb.add_bevel_box(
        center_u=(0.0, 0.16, 0.75),
        size_u=(1.68, 0.22, 0.045),
        mat="R1_Rubber",
        bevel=0.008,
        segments=1,
    )
    arm_obj = arm_mb.build_object(parent=root)

    # --- Brush: wide rotary sweeper cylinder inside the front shroud (spins around local X) ---
    brush_mb = r1kit.MeshBuilder("Brush", pivot_u=(0.0, 0.20, 0.98))
    # Central steel drive drum (radius = 0.095 m, length = 1.66 m)
    brush_mb.add_cylinder(
        center_u=(0.0, 0.20, 0.98),
        radius=0.095,
        length=1.66,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=16,
        bevel=0.010,
    )
    # Segmented rubber/brass sweeping paddle rings along the 1.66 m width (outer radius = 0.195 m, touching y = 0.005 m)
    ring_xs = [-0.70, -0.47, -0.24, 0.0, 0.24, 0.47, 0.70]
    for idx, rx in enumerate(ring_xs):
        mat_name = "R1_Rubber" if idx % 2 == 0 else "R1_Brass"
        brush_mb.add_cylinder(
            center_u=(rx, 0.20, 0.98),
            radius=0.196,
            length=0.15,
            mat=mat_name,
            axis_u="X",
            segments=16,
            bevel=0.016,
            bevel_segments=2,
        )
    # 6 longitudinal beater paddles spanning the full roller width so rotation reads unmistakably
    for p_i in range(6):
        ang = math.radians(p_i * 60.0)
        py = 0.20 + 0.155 * math.cos(ang)
        pz = 0.98 + 0.155 * math.sin(ang)
        brush_mb.add_bevel_box(
            center_u=(0.0, py, pz),
            size_u=(1.62, 0.042, 0.042),
            mat="R1_Rubber",
            bevel=0.006,
            segments=1,
            rot_deg_u=(-math.degrees(ang), 0.0, 0.0),
        )
    brush_mb.build_object(parent=arm_obj)

    _finalize_and_export(
        root,
        fbx_name="Sweeper.fbx",
        footprint_radius=1.35,
        render_previews=render_previews,
        preview_slug="sweeper",
    )
    return root


# ---------------------------------------------------------------------------
# 3. SURVEY FLOCK DRONE (SurveyDrone.fbx)
#    Tycho / Meridian small hexagonal drone with 4 cold-gas De Laval nozzles.
#    Vacuum rule: NO rotors, NO propellers, NO wings/fins.
#    Target size: ~0.68 m W (X) x 0.68 m L (Z_u / Y_bl) x 0.42 m H (Y_u / Z_bl)
#    Child pivots: Body, Lamp, Scanner, Nozzle_FL, Nozzle_FR, Nozzle_RL, Nozzle_RR
# ---------------------------------------------------------------------------
def build_survey_drone(render_previews: bool = False) -> bpy.types.Object:
    """Build Species 3: SurveyDrone (~0.68 m W x 0.68 m L x 0.42 m H, Tycho/Meridian family)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("SurveyDrone", pivot_u=(0.0, 0.0, 0.0))

    # --- Body: hexagonal chalky-white composite hull, teal markings, top cold-gas tank, landing skids ---
    body_mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.20, 0.0))

    # Main hexagonal composite hull (segments=6 gives a crisp regular hexagonal prism!)
    body_mb.add_cylinder(
        center_u=(0.0, 0.20, 0.0),
        radius=0.22,
        length=0.11,
        mat="R1_WhitePaint",
        axis_u="Y",
        segments=6,
        bevel=0.014,
        bevel_segments=2,
    )
    # Muted teal Tycho/Meridian identification band around mid-hull (segments=6)
    body_mb.add_cylinder(
        center_u=(0.0, 0.215, 0.0),
        radius=0.228,
        length=0.036,
        mat="R1_TealMark",
        axis_u="Y",
        segments=6,
        bevel=0.005,
        bevel_segments=1,
    )
    # Lower machined bare-steel avionics baseplate (segments=6)
    body_mb.add_cylinder(
        center_u=(0.0, 0.135, 0.0),
        radius=0.175,
        length=0.032,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=6,
        bevel=0.005,
    )

    # Top cold-gas (N2) spherical/domed pressure vessel wrapped in crinkled GoldFoil + Brass fill valve
    body_mb.add_cylinder(
        center_u=(0.0, 0.285, 0.0),
        radius=0.135,
        radius_top=0.095,
        length=0.075,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=12,
        bevel=0.014,
        bevel_segments=2,
    )
    body_mb.add_cylinder(
        center_u=(0.0, 0.332, 0.0),
        radius=0.042,
        length=0.028,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.004,
    )

    # Rear telemetry whip antenna rising to y = 0.42 m
    body_mb.add_cylinder(
        center_u=(-0.06, 0.335, -0.09),
        radius=0.008,
        radius_top=0.005,
        length=0.17,
        mat="R1_BareSteel",
        axis_u="Y",
        segments=8,
        bevel=0.0015,
    )

    # Forward lamp housing collar on +Z hex face
    body_mb.add_bevel_box(
        center_u=(0.0, 0.20, 0.205),
        size_u=(0.088, 0.056, 0.042),
        mat="R1_GreyMetal",
        bevel=0.006,
        segments=1,
    )
    body_mb.add_cylinder(
        center_u=(0.0, 0.20, 0.228),
        radius=0.026,
        length=0.016,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel=0.003,
    )

    # 4 diagonal outrigger struts + copper high-pressure gas feed lines to the 4 corner thruster mounts
    corners = [
        (-0.24, 0.24),
        (0.24, 0.24),
        (-0.24, -0.24),
        (0.24, -0.24),
    ]
    for cx, cz in corners:
        sx = -1.0 if cx < 0 else 1.0
        sz = -1.0 if cz < 0 else 1.0
        body_mb.add_strut(
            p0_u=(sx * 0.13, 0.20, sz * 0.13),
            p1_u=(cx, 0.20, cz),
            width=0.032,
            thickness=0.026,
            mat="R1_BareSteel",
            bevel=0.004,
        )
        body_mb.add_strut(
            p0_u=(sx * 0.09, 0.265, sz * 0.09),
            p1_u=(cx * 0.92, 0.225, cz * 0.92),
            width=0.016,
            thickness=0.016,
            mat="R1_Copper",
            bevel=0.002,
        )

    # Twin tubular landing skids resting at y = 0.000 m so ground-alignment passes cleanly
    for sx in (-1.0, 1.0):
        skid_x = sx * 0.165
        # Horizontal skid runner bar (z = -0.18 .. +0.18 m, bottom at y = 0.000 m)
        body_mb.add_bevel_box(
            center_u=(skid_x, 0.012, 0.0),
            size_u=(0.024, 0.024, 0.36),
            mat="R1_BareSteel",
            bevel=0.004,
            segments=1,
        )
        # Front and rear rubber skid feet pads at y = 0.000 m
        for sz in (-1.0, 1.0):
            body_mb.add_bevel_box(
                center_u=(skid_x, 0.010, sz * 0.145),
                size_u=(0.032, 0.020, 0.056),
                mat="R1_Rubber",
                bevel=0.003,
                segments=1,
            )
            # Angled skid leg strut up to lower hull
            body_mb.add_strut(
                p0_u=(skid_x, 0.020, sz * 0.11),
                p1_u=(sx * 0.125, 0.135, sz * 0.09),
                width=0.020,
                thickness=0.020,
                mat="R1_GreyMetal",
                bevel=0.003,
            )

    body_obj = body_mb.build_object(parent=root)

    # --- Lamp: single forward survey/status lamp lens (parent=Body so it hovers with Body) ---
    lamp_mb = r1kit.MeshBuilder("Lamp", pivot_u=(0.0, 0.20, 0.238))
    lamp_mb.add_cylinder(
        center_u=(0.0, 0.20, 0.238),
        radius=0.020,
        radius_top=0.016,
        length=0.014,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=12,
        bevel=0.0025,
    )
    lamp_mb.build_object(parent=body_obj)

    # --- Scanner: underbelly rotating optical survey turret (parent=Body) ---
    scan_mb = r1kit.MeshBuilder("Scanner", pivot_u=(0.0, 0.092, 0.02))
    scan_mb.add_cylinder(
        center_u=(0.0, 0.092, 0.02),
        radius=0.072,
        length=0.052,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=6,
        bevel=0.006,
    )
    # Forward-angled dark-glass optical lens + brass ring
    scan_mb.add_cylinder(
        center_u=(0.0, 0.084, 0.078),
        radius=0.028,
        length=0.022,
        mat="R1_Brass",
        axis_u="Z",
        segments=12,
        bevel=0.003,
    )
    scan_mb.add_cylinder(
        center_u=(0.0, 0.084, 0.088),
        radius=0.021,
        length=0.012,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=12,
        bevel=0.002,
    )
    scan_mb.build_object(parent=body_obj)

    # --- 4 Cold-Gas De Laval Thruster Nozzles: Nozzle_FL, Nozzle_FR, Nozzle_RL, Nozzle_RR (parent=Body) ---
    nozzle_specs = [
        ("Nozzle_FL", -0.255, 0.20, 0.255),
        ("Nozzle_FR", 0.255, 0.20, 0.255),
        ("Nozzle_RL", -0.255, 0.20, -0.255),
        ("Nozzle_RR", 0.255, 0.20, -0.255),
    ]
    for n_name, nx, ny, nz in nozzle_specs:
        n_mb = r1kit.MeshBuilder(n_name, pivot_u=(nx, ny, nz))
        # Upper gimbal actuator block (GreyMetal + TealMark cap)
        n_mb.add_bevel_box(
            center_u=(nx, ny + 0.018, nz),
            size_u=(0.068, 0.044, 0.068),
            mat="R1_GreyMetal",
            bevel=0.006,
            segments=1,
        )
        n_mb.add_bevel_box(
            center_u=(nx, ny + 0.042, nz),
            size_u=(0.056, 0.014, 0.056),
            mat="R1_TealMark",
            bevel=0.003,
            segments=1,
        )
        # Throat valve collar (BareSteel)
        n_mb.add_cylinder(
            center_u=(nx, ny - 0.018, nz),
            radius=0.026,
            length=0.032,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=12,
            bevel=0.003,
        )
        # Flared conical De Laval thruster bell pointing down (radius flares from 0.024 at top to 0.058 at bottom!)
        n_mb.add_cylinder(
            center_u=(nx, ny - 0.072, nz),
            radius=0.058,
            radius_top=0.024,
            length=0.084,
            mat="R1_Brass",
            axis_u="Y",
            segments=14,
            bevel=0.004,
            bevel_segments=1,
        )
        # Copper regenerative cooling coil ring around nozzle mid-bell
        n_mb.add_cylinder(
            center_u=(nx, ny - 0.052, nz),
            radius=0.044,
            length=0.016,
            mat="R1_Copper",
            axis_u="Y",
            segments=14,
            bevel=0.002,
        )
        n_mb.build_object(parent=body_obj)

    _finalize_and_export(
        root,
        fbx_name="SurveyDrone.fbx",
        footprint_radius=0.45,
        render_previews=render_previews,
        preview_slug="survey_drone",
    )
    return root


# ---------------------------------------------------------------------------
# 4. THE TRENCHER (Trencher.fbx)
#    Slow tracked crawler with a front trenching wheel and a rear cable spool
#    laying cable behind it (bigger than 07, Lumen family).
#    Rover 07 = 1.47 m W x 2.01 m L x 1.59 m H.
#    Trencher = ~1.96 m W (X) x 3.28 m L (Z_u / Y_bl) x 1.84 m H (Y_u / Z_bl).
#    Child pivots: Body, Lamp, Track_L, Track_R, Roller_FL, Roller_FR, Roller_RL, Roller_RR,
#                  TrenchBoom, TrenchWheel, CableSpool
# ---------------------------------------------------------------------------
def build_trencher(render_previews: bool = False) -> bpy.types.Object:
    """Build Species 4: Trencher (~1.96 m W x 3.28 m L x 1.84 m H, bigger than 07)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("Trencher", pivot_u=(0.0, 0.0, 0.0))

    # --- Body: heavy industrial Lumen hull, offset forward cab, rear A-frame spool gantry ---
    body_mb = r1kit.MeshBuilder("Body", pivot_u=(0.0, 0.72, -0.08))

    # Lower structural belly frame between the crawler tracks (y = 0.28 .. 0.58 m, z = -1.16 .. +0.86 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.43, -0.15),
        size_u=(1.06, 0.30, 2.02),
        mat="R1_GreyMetal",
        bevel=0.028,
        segments=2,
    )

    # Faded coral-orange heavy perimeter rub-rail beltline (y = 0.58 .. 0.72 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.65, -0.14),
        size_u=(1.22, 0.14, 2.12),
        mat="R1_OrangeStripe",
        bevel=0.024,
        segments=2,
    )

    # Main warm cream enamel upper engineering deck & power-conditioning enclosure (y = 0.72 .. 1.24 m)
    body_mb.add_bevel_box(
        center_u=(0.0, 0.98, -0.02),
        size_u=(1.16, 0.52, 1.72),
        mat="R1_CreamEnamel",
        bevel=0.055,
        segments=3,
    )

    # Side radiator louvre banks + heavy copper power busbars on both flanks
    for sx in (-1.0, 1.0):
        body_mb.add_bevel_box(
            center_u=(sx * 0.59, 0.98, 0.06),
            size_u=(0.06, 0.32, 0.88),
            mat="R1_BareSteel",
            bevel=0.008,
            segments=1,
        )
        body_mb.add_strut(
            p0_u=(sx * 0.56, 1.24, 0.42),
            p1_u=(sx * 0.56, 1.24, -0.48),
            width=0.055,
            thickness=0.045,
            mat="R1_Copper",
            bevel=0.006,
        )

    # Offset forward operator/guidance cab turret on right-center deck (y = 1.22 .. 1.84 m, z = +0.06 .. +0.76 m)
    body_mb.add_bevel_box(
        center_u=(0.18, 1.53, 0.41),
        size_u=(0.68, 0.62, 0.70),
        mat="R1_CreamEnamel",
        bevel=0.048,
        segments=2,
    )
    # Dark-glass optical guidance visor slit across the cab front
    body_mb.add_bevel_box(
        center_u=(0.18, 1.48, 0.76),
        size_u=(0.48, 0.14, 0.04),
        mat="R1_DarkGlass",
        bevel=0.006,
        segments=1,
    )
    # Brass lamp bezel on the upper cab brow
    body_mb.add_cylinder(
        center_u=(0.18, 1.68, 0.76),
        radius=0.075,
        length=0.042,
        mat="R1_Brass",
        axis_u="Z",
        segments=16,
        bevel=0.006,
    )

    # Left-deck auxiliary hydraulic/winch cabinet (y = 1.22 .. 1.52 m)
    body_mb.add_bevel_box(
        center_u=(-0.32, 1.37, 0.32),
        size_u=(0.36, 0.30, 0.64),
        mat="R1_GreyMetal",
        bevel=0.022,
        segments=2,
    )

    # Rear A-frame cable-spool support gantry (supports CableSpool at (0, 0.98, -1.04))
    for sx in (-1.0, 1.0):
        # Forward A-frame leg
        body_mb.add_strut(
            p0_u=(sx * 0.52, 0.72, -0.62),
            p1_u=(sx * 0.52, 1.02, -1.04),
            width=0.095,
            thickness=0.085,
            mat="R1_GreyMetal",
            bevel=0.010,
        )
        # Rear A-frame leg
        body_mb.add_strut(
            p0_u=(sx * 0.52, 0.58, -1.18),
            p1_u=(sx * 0.52, 1.02, -1.04),
            width=0.095,
            thickness=0.085,
            mat="R1_GreyMetal",
            bevel=0.010,
        )
        # Brass axle pillow-block bearing housing at spool pivot
        body_mb.add_bevel_box(
            center_u=(sx * 0.54, 0.98, -1.04),
            size_u=(0.11, 0.18, 0.18),
            mat="R1_Brass",
            bevel=0.012,
            segments=1,
        )

    # Rear cable-laying chute & fairlead guide trailing behind the spool (extends to z = -1.64 m)
    body_mb.add_strut(
        p0_u=(0.0, 0.46, -1.14),
        p1_u=(0.0, 0.22, -1.58),
        width=0.28,
        thickness=0.12,
        mat="R1_BareSteel",
        bevel=0.012,
    )
    # Trailing trunk cable segment paying out from bottom of spool through rear fairlead chute to ground
    body_mb.add_strut(
        p0_u=(0.0, 0.54, -1.04),
        p1_u=(0.0, 0.12, -1.62),
        width=0.095,
        thickness=0.095,
        mat="R1_Rubber",
        bevel=0.012,
    )

    body_mb.build_object(parent=root)

    # --- Lamp: single warm amber work/status lamp on the forward cab brow ---
    lamp_mb = r1kit.MeshBuilder("Lamp", pivot_u=(0.18, 1.68, 0.785))
    lamp_mb.add_cylinder(
        center_u=(0.18, 1.68, 0.785),
        radius=0.056,
        radius_top=0.046,
        length=0.028,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=16,
        bevel=0.005,
    )
    lamp_mb.build_object(parent=root)

    # --- Left & Right Armored Crawler Tracks: Track_L, Track_R ---
    # Span x = -0.98 .. +0.98 m (total width = 1.96 m), bottom at y = 0.000 m
    for track_name, sx in (("Track_L", -1.0), ("Track_R", 1.0)):
        tx = sx * 0.76
        t_mb = r1kit.MeshBuilder(track_name, pivot_u=(tx, 0.30, -0.06))
        # Main rubber crawler belt block (bottom at y = 0.000 m, height = 0.58 m, length = 2.24 m)
        t_mb.add_bevel_box(
            center_u=(tx, 0.29, -0.06),
            size_u=(0.38, 0.58, 2.24),
            mat="R1_Rubber",
            bevel=0.14,
            segments=3,
        )
        # Outer armored bogie side girder (x reaches ±0.98 m)
        t_mb.add_bevel_box(
            center_u=(sx * 0.86, 0.29, -0.06),
            size_u=(0.24, 0.32, 1.86),
            mat="R1_GreyMetal",
            bevel=0.024,
            segments=2,
        )
        # Faded orange hazard mudguard strip on top of track frame
        t_mb.add_bevel_box(
            center_u=(tx, 0.60, -0.06),
            size_u=(0.40, 0.06, 1.92),
            mat="R1_OrangeStripe",
            bevel=0.010,
            segments=1,
        )
        t_mb.build_object(parent=root)

    # --- 4 Track Drive Sprockets / Idlers: Roller_FL, Roller_FR, Roller_RL, Roller_RR ---
    roller_specs = [
        ("Roller_FL", -0.76, 0.30, 0.82),
        ("Roller_FR", 0.76, 0.30, 0.82),
        ("Roller_RL", -0.76, 0.30, -0.94),
        ("Roller_RR", 0.76, 0.30, -0.94),
    ]
    for r_name, rx, ry, rz in roller_specs:
        r_mb = r1kit.MeshBuilder(r_name, pivot_u=(rx, ry, rz))
        r_mb.add_cylinder(
            center_u=(rx, ry, rz),
            radius=0.22,
            length=0.42,
            mat="R1_BareSteel",
            axis_u="X",
            segments=14,
            bevel=0.015,
        )
        r_mb.add_cylinder(
            center_u=(rx, ry, rz),
            radius=0.11,
            length=0.44,
            mat="R1_Brass",
            axis_u="X",
            segments=12,
            bevel=0.008,
        )
        r_mb.build_object(parent=root)

    # --- TrenchBoom: heavy forward twin-beam cutter boom ---
    boom_mb = r1kit.MeshBuilder("TrenchBoom", pivot_u=(0.0, 0.66, 0.78))
    for sx in (-1.0, 1.0):
        # Main boom side arm extending forward to the cutter wheel axle at z = +1.18 m
        boom_mb.add_strut(
            p0_u=(sx * 0.26, 0.68, 0.74),
            p1_u=(sx * 0.22, 0.52, 1.20),
            width=0.12,
            thickness=0.11,
            mat="R1_GreyMetal",
            bevel=0.012,
        )
        # Hydraulic lift cylinder above boom arm
        boom_mb.add_strut(
            p0_u=(sx * 0.24, 0.96, 0.72),
            p1_u=(sx * 0.22, 0.66, 1.12),
            width=0.065,
            thickness=0.065,
            mat="R1_BareSteel",
            bevel=0.008,
        )
    # Upper curved spoil guard hood over the cutter wheel (with faded orange stripe)
    boom_mb.add_bevel_box(
        center_u=(0.0, 0.96, 1.12),
        size_u=(0.48, 0.12, 0.56),
        mat="R1_OrangeStripe",
        bevel=0.018,
        segments=2,
    )
    boom_obj = boom_mb.build_object(parent=root)

    # --- TrenchWheel: large rotary bucket/cutter wheel at nose (z = +1.18 m, reaches z = +1.64 m) ---
    wheel_mb = r1kit.MeshBuilder("TrenchWheel", pivot_u=(0.0, 0.52, 1.18))
    # Main heavy steel cutter disc (radius = 0.39 m, thickness = 0.24 m)
    wheel_mb.add_cylinder(
        center_u=(0.0, 0.52, 1.18),
        radius=0.39,
        length=0.24,
        mat="R1_GreyMetal",
        axis_u="X",
        segments=18,
        bevel=0.018,
    )
    # Faded orange hub ring + brass axle cap
    wheel_mb.add_cylinder(
        center_u=(0.0, 0.52, 1.18),
        radius=0.24,
        length=0.27,
        mat="R1_OrangeStripe",
        axis_u="X",
        segments=16,
        bevel=0.012,
    )
    wheel_mb.add_cylinder(
        center_u=(0.0, 0.52, 1.18),
        radius=0.11,
        length=0.31,
        mat="R1_Brass",
        axis_u="X",
        segments=12,
        bevel=0.008,
    )
    # 10 heavy bare-steel digging teeth around the cutter rim (outer reach = 0.46 m -> z = +1.64 m, y_min = 0.06 m)
    for t_i in range(10):
        ang = math.radians(t_i * 36.0)
        ty = 0.52 + 0.41 * math.cos(ang)
        tz = 1.18 + 0.41 * math.sin(ang)
        wheel_mb.add_bevel_box(
            center_u=(0.0, ty, tz),
            size_u=(0.28, 0.10, 0.08),
            mat="R1_BareSteel",
            bevel=0.008,
            segments=1,
            rot_deg_u=(-math.degrees(ang), 0.0, 0.0),
        )
    wheel_mb.build_object(parent=boom_obj)

    # --- CableSpool: large rear cable drum paying out trunk cable (pivot at (0, 0.98, -1.04)) ---
    spool_mb = r1kit.MeshBuilder("CableSpool", pivot_u=(0.0, 0.98, -1.04))
    # Wound rubber trunk cable core cylinder (radius = 0.36 m, width = 0.78 m)
    spool_mb.add_cylinder(
        center_u=(0.0, 0.98, -1.04),
        radius=0.36,
        length=0.78,
        mat="R1_Rubber",
        axis_u="X",
        segments=20,
        bevel=0.018,
    )
    # Copper conductor coil accent bands along the wound cable drum
    for cx in (-0.22, 0.0, 0.22):
        spool_mb.add_cylinder(
            center_u=(cx, 0.98, -1.04),
            radius=0.372,
            length=0.08,
            mat="R1_Copper",
            axis_u="X",
            segments=20,
            bevel=0.008,
        )
    # Left and right heavy spool flanges (radius = 0.48 m) + crossbars so rotation is visually unmistakable
    for sx in (-1.0, 1.0):
        fx = sx * 0.42
        spool_mb.add_cylinder(
            center_u=(fx, 0.98, -1.04),
            radius=0.48,
            length=0.06,
            mat="R1_GreyMetal",
            axis_u="X",
            segments=20,
            bevel=0.012,
        )
        # Faded orange spoke bar across outer face of each flange
        spool_mb.add_bevel_box(
            center_u=(sx * 0.455, 0.98, -1.04),
            size_u=(0.03, 0.84, 0.14),
            mat="R1_OrangeStripe",
            bevel=0.006,
            segments=1,
        )
        spool_mb.add_bevel_box(
            center_u=(sx * 0.455, 0.98, -1.04),
            size_u=(0.03, 0.14, 0.84),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
        )
    spool_mb.build_object(parent=root)

    _finalize_and_export(
        root,
        fbx_name="Trencher.fbx",
        footprint_radius=1.75,
        render_previews=render_previews,
        preview_slug="trencher",
    )
    return root


def build_all_fauna(render_previews: bool = False) -> None:
    """Build all 4 machine fauna FBX models."""
    build_panel_mite(render_previews=render_previews)
    build_sweeper(render_previews=render_previews)
    build_survey_drone(render_previews=render_previews)
    build_trencher(render_previews=render_previews)


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Build the four 0.6 Machine Fauna R1 FBX models.")
    parser.add_argument(
        "--only",
        choices=["panel_mite", "sweeper", "survey_drone", "trencher"],
        default=None,
        help="Build only a single fauna species.",
    )
    parser.add_argument(
        "--render-previews",
        action="store_true",
        help="Render turntable preview sheets to Logs/renders/fauna/.",
    )
    args = parser.parse_args(argv)

    builders = {
        "panel_mite": build_panel_mite,
        "sweeper": build_sweeper,
        "survey_drone": build_survey_drone,
        "trencher": build_trencher,
    }
    if args.only:
        builders[args.only](render_previews=args.render_previews)
    else:
        build_all_fauna(render_previews=args.render_previews)
    return 0


if __name__ == "__main__":
    user_argv: List[str] = []
    if "--" in sys.argv:
        user_argv = sys.argv[sys.argv.index("--") + 1 :]
    raise SystemExit(main(user_argv))
