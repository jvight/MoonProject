"""
Headless Blender concept generator and contact-sheet compositor for 07's Upgrade Visual Language.
CONCEPT ONLY — does not modify any game FBX or production asset.

Generates:
  - 5 upgrades x 2 variants (A & B) + Before baseline, each rendered at:
      1) Chase Camera (~7.0 m distance, 16 deg pitch, 52 deg FOV) + silhouette mask
      2) 3/4 Close-Up view showing the bolted Kessler-Arai mount, weld beads, '07' tag, and maker accent
  - "Fully Grown 07" with all 5 recommended upgrades mounted simultaneously (Chase + Front 3/4 + Rear 3/4)
  - Master labelled contact sheet: Logs/captures/upgrades_sheet.png
  - Full-growth showcase sheet:   Logs/captures/full_growth.png
  - Per-upgrade comparison strips: Logs/captures/upgrade_<id>.png

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/render_upgrade_concepts.py
"""
from __future__ import annotations

import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np

SCRIPT_DIR = Path(__file__).resolve().parent
BLENDER_DIR = SCRIPT_DIR.parent
for _p in (BLENDER_DIR, SCRIPT_DIR):
    if str(_p) not in sys.path:
        sys.path.insert(0, str(_p))
sys.dont_write_bytecode = True

IN_BLENDER = "--compose-only" not in sys.argv
if IN_BLENDER:
    import bmesh
    import bpy
    from build_rover import DIMENSIONS, MATERIALS  # noqa: E402
    from r1kit import (  # noqa: E402
        _unity_rot_matrix,
        append_beveled_box,
        append_cylinder,
        append_dome_rivet,
        append_helical_coil,
        append_hex_bolt,
        append_lathe_profile,
        append_prism_extrusion,
        append_spherical_dome,
        append_torus,
        bake_vertex_colors,
        create_mesh_node,
        reset_scene,
        u_pos,
    )
    from render_suite import (  # noqa: E402
        aim_camera_at,
        enhance_materials_with_vertex_color_for_render,
        render_to_file,
        setup_studio_lighting_and_camera,
    )
    from rover_parts import (  # noqa: E402
        build_rover_model_hierarchy,
        configure_procedural_crt_screen_shader,
        create_material_library,
    )
else:
    MATERIALS = {}



# ==============================================================================
# EXTENDED CONCEPT MATERIAL PALETTE (Adds Canon Era-B White & Tycho Teal)
# ==============================================================================
CONCEPT_MATERIALS = dict(MATERIALS)
CONCEPT_MATERIALS["R1_WhitePaint"] = {
    "base_color": (0.94, 0.92, 0.88),
    "metallic": 0.04,
    "roughness": 0.36,
    "emission_color": (0.0, 0.0, 0.0),
    "emission_strength": 0.0,
}
CONCEPT_MATERIALS["R1_TealMark"] = {
    "base_color": (0.10, 0.50, 0.56),  # Canon Tycho Consortium #3E7A82 boosted for studio readability
    "metallic": 0.08,
    "roughness": 0.32,
    "emission_color": (0.0, 0.0, 0.0),
    "emission_strength": 0.0,
}
CONCEPT_MATERIALS["R1_TealLamp"] = {
    "base_color": (0.24, 0.94, 1.00),  # Tycho Teal status lamp emissive
    "metallic": 0.0,
    "roughness": 0.16,
    "emission_color": (0.22, 0.92, 1.00),
    "emission_strength": 2.4,
}


# ==============================================================================
# SHARED MOUNTING HARDWARE HELPERS (Bolted Kessler-Arai Sockets, Welds & '07' Tag)
# ==============================================================================

def _add_weld_bead_strip(
    bm: bmesh.types.BMesh,
    p0: tuple[float, float, float],
    p1: tuple[float, float, float],
    beads: int = 7,
    radius: float = 0.009,
    mat_idx: int = 3,
):
    """Append a row of overlapping bright field-weld globules along segment p0 -> p1."""
    for i in range(beads):
        t = i / max(1, beads - 1)
        cx = p0[0] + (p1[0] - p0[0]) * t
        cy = p0[1] + (p1[1] - p0[1]) * t
        cz = p0[2] + (p1[2] - p0[2]) * t
        append_cylinder(
            bm,
            radius1=radius,
            radius2=radius * 0.85,
            length=radius * 1.6,
            axis="Y",
            center_xyz=(cx, cy, cz),
            segments=8,
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=mat_idx,
        )


def _add_07_adapter_tag(
    bm: bmesh.types.BMesh,
    center_xyz: tuple[float, float, float],
    normal_axis: str = "X",
    sign_dir: float = 1.0,
    mat_orange: int = 1,
    mat_cream: int = 0,
    mat_steel: int = 3,
):
    """
    Append a cut coral-orange '07' adapter plaque with two cream glyph blocks and corner rivets
    (per maker-understanding.md §1 rule 3: '07's bright weld beads and a cut 07 tag join it to the frame').
    """
    cx, cy, cz = center_xyz
    if normal_axis == "X":
        append_beveled_box(
            bm,
            size_xyz=(0.012, 0.068, 0.118),
            center_xyz=(cx, cy, cz),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=mat_orange,
        )
        # '0' and '7' cream stencil relief blocks
        append_beveled_box(
            bm,
            size_xyz=(0.014, 0.044, 0.036),
            center_xyz=(cx + sign_dir * 0.002, cy, cz - 0.024),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.014, 0.044, 0.036),
            center_xyz=(cx + sign_dir * 0.002, cy, cz + 0.024),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )
        for dz in (-0.048, 0.048):
            append_dome_rivet(
                bm,
                radius=0.005,
                height=0.005,
                axis="X",
                center_xyz=(cx + sign_dir * 0.006, cy, cz + dz),
                segments=8,
                mat_idx=mat_steel,
            )
    elif normal_axis == "Z":
        append_beveled_box(
            bm,
            size_xyz=(0.118, 0.068, 0.012),
            center_xyz=(cx, cy, cz),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=mat_orange,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.036, 0.044, 0.014),
            center_xyz=(cx - 0.024, cy, cz + sign_dir * 0.002),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.036, 0.044, 0.014),
            center_xyz=(cx + 0.024, cy, cz + sign_dir * 0.002),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )
    else:  # Y
        append_beveled_box(
            bm,
            size_xyz=(0.118, 0.012, 0.068),
            center_xyz=(cx, cy, cz),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=mat_orange,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.036, 0.014, 0.044),
            center_xyz=(cx - 0.024, cy + sign_dir * 0.002, cz),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.036, 0.014, 0.044),
            center_xyz=(cx + 0.024, cy + sign_dir * 0.002, cz),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=mat_cream,
        )


def _append_regular_hex_prism(
    bm: bmesh.types.BMesh,
    radius: float,
    depth: float,
    axis: str,
    center_xyz: tuple[float, float, float],
    bevel_width: float,
    mat_idx: int,
    rot_euler: tuple[float, float, float] = (0.0, 0.0, 0.0),
):
    """Append a 6-sided regular hexagonal plate/prism."""
    poly = [
        (radius * math.cos(i * math.pi / 3.0 + math.pi / 6.0), radius * math.sin(i * math.pi / 3.0 + math.pi / 6.0))
        for i in range(6)
    ]
    append_prism_extrusion(
        bm,
        poly,
        depth=depth,
        axis=axis,
        center_xyz=center_xyz,
        bevel_width=bevel_width,
        bevel_segments=2,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
    )


def _standard_mat_list(mats: dict) -> list:
    """
    Standard 10-slot material table used by all concept builders:
      0: R1_CreamEnamel
      1: R1_OrangeStripe
      2: R1_GreyMetal
      3: R1_BareSteel
      4: R1_Brass
      5: R1_Copper
      6: R1_Rubber
      7: R1_GoldFoil
      8: R1_WhitePaint
      9: R1_TealMark
     10: R1_LampAmber
     11: R1_DarkGlass
    """
    return [
        mats["R1_CreamEnamel"],   # 0
        mats["R1_OrangeStripe"],  # 1
        mats["R1_GreyMetal"],     # 2
        mats["R1_BareSteel"],     # 3
        mats["R1_Brass"],         # 4
        mats["R1_Copper"],        # 5
        mats["R1_Rubber"],        # 6
        mats["R1_GoldFoil"],      # 7
        mats["R1_WhitePaint"],    # 8
        mats["R1_TealMark"],      # 9
        mats["R1_LampAmber"],     # 10
        mats["R1_DarkGlass"],     # 11
        mats["R1_TealLamp"],      # 12
    ]


# ==============================================================================
# UPGRADE (a): ANTENNA EXTENDED (R3 — FirstCharge)
# ==============================================================================

def build_upgrade_a_variant_A(rover_root, mats):
    """
    Upgrade (a) Variant A [RECOMMENDED]: 'Dish-Crown Mast & Symmetric Telemetry Bridge'
    - Bolted Kessler-Arai dorsal transceiver bridge spanning rear shoulders (x = -0.38..+0.38, y = 0.93, z = -0.46)
    - Left shoulder counter-mounted inductive direction-finder loop & preamp cylinder (y = 0.91..1.48 m)
    - Right shoulder tall stepped coaxial mast rising to y = 2.24 m (+0.71 m above head!) with a canted
      0.44 m open parabolic ring-dish receiver, coral call-sign drum, and large glowing Amber beacon cage at apex.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Bolted Kessler-Arai Dorsal Transceiver Bridge across rear shoulders (z = -0.46 m)
    append_beveled_box(
        bm,
        size_xyz=(0.78, 0.055, 0.115),
        center_xyz=(0.0, 0.935, -0.46),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    for sx in (-1.0, 1.0):
        # Heavy socket clamping saddle on each rear shoulder
        append_beveled_box(
            bm,
            size_xyz=(0.145, 0.095, 0.155),
            center_xyz=(sx * 0.36, 0.92, -0.46),
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=2,
        )
        for bz in (-0.055, 0.055):
            for bx in (-0.045, 0.045):
                append_hex_bolt(
                    bm,
                    radius=0.010,
                    height=0.014,
                    axis="Y",
                    center_xyz=(sx * 0.36 + bx, 0.972, -0.46 + bz),
                    mat_idx=4,  # R1_Brass
                )
        _add_weld_bead_strip(
            bm,
            (sx * 0.29, 0.91, -0.52),
            (sx * 0.29, 0.91, -0.40),
            beads=6,
            radius=0.009,
            mat_idx=3,
        )

    # Stamped '07' coral adapter tag on right antenna base pedestal
    _add_07_adapter_tag(bm, (0.435, 0.92, -0.46), normal_axis="X", sign_dir=1.0)

    # 2. Left-Shoulder Counter-Unit: Cream/Brass Preamp Canister + Twin Orthogonal Direction-Finding Loops
    lx, lz = -0.36, -0.46
    append_cylinder(
        bm,
        radius1=0.062,
        radius2=0.058,
        length=0.24,
        axis="Y",
        center_xyz=(lx, 1.08, lz),
        segments=18,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
    )
    append_cylinder(
        bm,
        radius1=0.066,
        radius2=0.066,
        length=0.045,
        axis="Y",
        center_xyz=(lx, 1.16, lz),
        segments=18,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=1,  # R1_OrangeStripe collar
    )
    # Crossed brass & copper D/F loops at y = 1.34 m
    append_torus(
        bm,
        major_radius=0.115,
        minor_radius=0.014,
        axis="Z",
        center_xyz=(lx, 1.34, lz),
        major_seg=22,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
    )
    append_torus(
        bm,
        major_radius=0.105,
        minor_radius=0.011,
        axis="X",
        center_xyz=(lx, 1.34, lz),
        major_seg=22,
        minor_seg=8,
        mat_idx=5,  # R1_Copper
    )
    append_spherical_dome(
        bm,
        radius=0.026,
        dome_height=0.024,
        axis="Y",
        center_xyz=(lx, 1.47, lz),
        segments=14,
        rings=4,
        mat_idx=10,  # R1_LampAmber status pip
    )

    # 3. Right-Shoulder Tall Extended Mast + Parabolic Ring-Dish Receiver + Lantern Beacon Tip
    rx, rz = 0.36, -0.46
    # Base armored boot & copper loading coil (y = 0.96..1.38 m)
    append_cylinder(
        bm,
        radius1=0.052,
        radius2=0.044,
        length=0.38,
        axis="Y",
        center_xyz=(rx, 1.15, rz),
        segments=18,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,
    )
    append_helical_coil(
        bm,
        coil_radius=0.058,
        wire_radius=0.010,
        length=0.26,
        turns=6.5,
        axis="Y",
        center_xyz=(rx, 1.16, rz),
        segs_per_turn=14,
        wire_segs=6,
        mat_idx=5,  # R1_Copper
    )
    # Upper telescoping thick mast tube rising to y = 2.15 m
    append_cylinder(
        bm,
        radius1=0.028,
        radius2=0.022,
        length=0.88,
        axis="Y",
        center_xyz=(rx, 1.72, rz),
        segments=16,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=3,  # R1_BareSteel
    )
    # Locking brass collars at telescoping joints
    for cy in (1.36, 1.68, 2.02):
        append_cylinder(
            bm,
            radius1=0.038,
            radius2=0.036,
            length=0.042,
            axis="Y",
            center_xyz=(rx, cy, rz),
            segments=16,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass
        )

    # Canted Parabolic Ring-Dish Receiver at y = 1.84 m (Ø 0.44 m, canted toward sky/Earth)
    dish_y = 1.84
    dish_rot = (math.radians(-22.0), math.radians(15.0), 0.0)
    # Outer brass collector ring (Ø 0.44 m)
    append_torus(
        bm,
        major_radius=0.215,
        minor_radius=0.018,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.04),
        major_seg=28,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
        rot_euler=dish_rot,
    )
    # Inner copper sub-ring (Ø 0.28 m)
    append_torus(
        bm,
        major_radius=0.135,
        minor_radius=0.012,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.06),
        major_seg=24,
        minor_seg=6,
        mat_idx=5,  # R1_Copper
        rot_euler=dish_rot,
    )
    # Central cream & coral transceiver feed drum + GoldFoil backing
    append_cylinder(
        bm,
        radius1=0.075,
        radius2=0.065,
        length=0.11,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.03),
        segments=18,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
        rot_euler=dish_rot,
    )
    append_cylinder(
        bm,
        radius1=0.078,
        radius2=0.078,
        length=0.042,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.04),
        segments=18,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=1,  # R1_OrangeStripe maker accent band
        rot_euler=dish_rot,
    )
    append_cylinder(
        bm,
        radius1=0.068,
        radius2=0.068,
        length=0.018,
        axis="Z",
        center_xyz=(rx, dish_y, rz - 0.025),
        segments=16,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=7,  # R1_GoldFoil rear MLI blanket
        rot_euler=dish_rot,
    )
    # 4 radial cross-dipole struts supporting the outer ring
    for ang_deg in (0.0, 90.0):
        append_beveled_box(
            bm,
            size_xyz=(0.43, 0.018, 0.018),
            center_xyz=(rx, dish_y, rz + 0.045),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=3,
            rot_euler=(dish_rot[0], dish_rot[1], math.radians(ang_deg)),
        )

    # Apex Lantern Call-Sign Beacon Tip at y = 2.18 m (Ø 0.11 m glowing amber dome + brass cage)
    append_cylinder(
        bm,
        radius1=0.056,
        radius2=0.048,
        length=0.115,
        axis="Y",
        center_xyz=(rx, 2.16, rz),
        segments=18,
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber
    )
    append_spherical_dome(
        bm,
        radius=0.052,
        dome_height=0.048,
        axis="Y",
        center_xyz=(rx, 2.215, rz),
        segments=18,
        rings=5,
        mat_idx=10,
    )
    append_torus(
        bm,
        major_radius=0.062,
        minor_radius=0.008,
        axis="Y",
        center_xyz=(rx, 2.16, rz),
        major_seg=18,
        minor_seg=6,
        mat_idx=4,  # R1_Brass guard ring
    )

    return create_mesh_node("Upgrade_A_VarA", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_a_variant_B(rover_root, mats):
    """
    Upgrade (a) Variant B: 'Unfolding Dorsal Scissor-Truss Mast & Wide T-Array'
    - Center-mounted articulated scissor/box-truss mast unfolding from rear deck (z = -0.56 m) up to y = 2.20 m
    - Wide horizontal T-bar telemetry cross-arm (0.86 m span) with twin brass wave-trap rings,
      central coral-orange transceiver box, and top amber call-sign dome.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    mz = -0.55
    # 1. Bolted rear-deck base cradle & hinge clevises (y = 0.90..1.04 m)
    append_beveled_box(
        bm,
        size_xyz=(0.64, 0.075, 0.16),
        center_xyz=(0.0, 0.935, mz),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=2,
    )
    _add_07_adapter_tag(bm, (0.0, 0.94, mz - 0.082), normal_axis="Z", sign_dir=-1.0)
    for sx in (-0.24, 0.24):
        append_cylinder(
            bm,
            radius1=0.036,
            radius2=0.036,
            length=0.085,
            axis="X",
            center_xyz=(sx, 0.99, mz),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass hinge drum
        )

    # 2. Unfolding 2-stage Scissor / A-frame Lattice Mast (y = 0.98..1.98 m)
    for sx in (-1.0, 1.0):
        # Lower truss legs angling inward from x = +/-0.22 to +/-0.12 at knee (y = 1.48 m)
        append_beveled_box(
            bm,
            size_xyz=(0.042, 0.54, 0.048),
            center_xyz=(sx * 0.17, 1.24, mz),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,  # R1_CreamEnamel
            rot_euler=(0.0, 0.0, sx * math.radians(11.0)),
        )
        # Brass knee joint clevis at y = 1.48 m
        append_cylinder(
            bm,
            radius1=0.034,
            radius2=0.034,
            length=0.068,
            axis="X",
            center_xyz=(sx * 0.12, 1.48, mz),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,
        )
        # Upper truss legs rising from knee (y = 1.48 m) to T-head (y = 1.96 m)
        append_beveled_box(
            bm,
            size_xyz=(0.038, 0.50, 0.044),
            center_xyz=(sx * 0.11, 1.72, mz),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=2,  # R1_GreyMetal
        )
    # Cross-braces along mast
    for by in (1.14, 1.34, 1.62, 1.82):
        append_beveled_box(
            bm,
            size_xyz=(0.28, 0.026, 0.030),
            center_xyz=(0.0, by, mz),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=3,
        )
    # Coiled copper umbilical feed running up center of truss
    append_helical_coil(
        bm,
        coil_radius=0.032,
        wire_radius=0.008,
        length=0.82,
        turns=9.0,
        axis="Y",
        center_xyz=(0.0, 1.48, mz),
        segs_per_turn=12,
        wire_segs=6,
        mat_idx=5,
    )

    # 3. Wide Horizontal T-Bar Cross-Arm (0.86 m wide at y = 1.98 m) + Central Coral Box + Twin Loop Rings
    ty = 1.98
    append_beveled_box(
        bm,
        size_xyz=(0.86, 0.042, 0.046),
        center_xyz=(0.0, ty, mz),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,
    )
    # Central Coral-Orange Transceiver Housing (Maker Accent)
    append_beveled_box(
        bm,
        size_xyz=(0.28, 0.16, 0.16),
        center_xyz=(0.0, ty, mz),
        bevel_width=0.012,
        bevel_segments=2,
        mat_idx=1,  # R1_OrangeStripe
    )
    append_beveled_box(
        bm,
        size_xyz=(0.22, 0.10, 0.175),
        center_xyz=(0.0, ty, mz),
        bevel_width=0.006,
        bevel_segments=1,
        mat_idx=0,  # R1_CreamEnamel faceplate
    )
    # Wingtip Brass & Copper Telemetry Loops (x = +/-0.42 m)
    for sx in (-1.0, 1.0):
        wx = sx * 0.42
        append_torus(
            bm,
            major_radius=0.115,
            minor_radius=0.016,
            axis="Z",
            center_xyz=(wx, ty, mz),
            major_seg=22,
            minor_seg=8,
            mat_idx=4,  # R1_Brass
        )
        append_cylinder(
            bm,
            radius1=0.036,
            radius2=0.036,
            length=0.075,
            axis="Z",
            center_xyz=(wx, ty, mz),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=5,  # R1_Copper hub
        )

    # Central Top Glowing Amber Call-Sign Dome (y = 2.14 m)
    append_cylinder(
        bm,
        radius1=0.062,
        radius2=0.054,
        length=0.095,
        axis="Y",
        center_xyz=(0.0, 2.11, mz),
        segments=18,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=10,
    )
    append_spherical_dome(
        bm,
        radius=0.056,
        dome_height=0.052,
        axis="Y",
        center_xyz=(0.0, 2.155, mz),
        segments=18,
        rings=5,
        mat_idx=10,
    )

    return create_mesh_node("Upgrade_A_VarB", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


# ==============================================================================
# UPGRADE (b): IGNITER FITTED (R4 — PowerOn, Cutter Turret Ignition Pack)
# ==============================================================================

def build_upgrade_b_variant_A(rover_root, mats):
    """
    Upgrade (b) Variant A [RECOMMENDED]: 'Twin-Shoulder Plasma Ignition Saddle & Glowing Coil Shroud'
    - Bolts a symmetric horseshoe saddle yoke across the upper front bonnet/collar shoulders (x = +/-0.54 m,
      y = 0.90..1.28 m, z = 0.36 m) so the twin glowing amber coil canisters and canted copper radiator
      shields break boldly into the open sky beside 07's neck/head in the 7 m rear chase camera!
    - Encloses CutterTurret (z = 0.62..0.92 m) in a heavy perforated brass & steel heat shroud with
      thick braided copper bus conduits and a glowing top ignition chamber window.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Bolted Horseshoe Saddle Yoke across upper front bonnet (y = 0.84..0.98 m, z = 0.40 m)
    append_beveled_box(
        bm,
        size_xyz=(0.96, 0.085, 0.24),
        center_xyz=(0.0, 0.88, 0.40),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    _add_07_adapter_tag(bm, (0.0, 0.928, 0.44), normal_axis="Y", sign_dir=1.0)

    # 2. Symmetric Twin Shoulder Plasma-Coil Canisters & Outboard Copper Vanes (x = +/-0.56 m, y = 0.98 m, z = 0.48 m)
    # Positioned forward over the front fender shoulders so the cheek brass eye is 100% clear while breaking the 7m chase outline!
    for sx in (-1.0, 1.0):
        cx = sx * 0.56
        cy = 0.98
        cz = 0.48
        tilt_z = -sx * math.radians(16.0)

        # Heavy cantilevered mounting bracket & bolts on upper bonnet shoulder
        append_beveled_box(
            bm,
            size_xyz=(0.28, 0.15, 0.26),
            center_xyz=(sx * 0.43, 0.90, cz - 0.02),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,
        )
        for bz in (-0.08, 0.08):
            append_hex_bolt(
                bm,
                radius=0.012,
                height=0.018,
                axis="X",
                center_xyz=(sx * 0.572, 0.90, cz - 0.02 + bz),
                mat_idx=4,
            )
        _add_weld_bead_strip(
            bm,
            (sx * 0.33, 0.89, cz - 0.12),
            (sx * 0.33, 0.89, cz + 0.08),
            beads=6,
            radius=0.009,
            mat_idx=3,
        )

        # Coral-Orange Ignition Pack Casing Block (Maker Accent)
        append_beveled_box(
            bm,
            size_xyz=(0.25, 0.26, 0.32),
            center_xyz=(cx, cy, cz),
            bevel_width=0.016,
            bevel_segments=2,
            mat_idx=1,  # R1_OrangeStripe
            rot_euler=(0.0, 0.0, tilt_z),
        )
        # Glowing Amber Plasma-Coil Sight Cylinder (projects high & wide in rear chase cam!)
        append_cylinder(
            bm,
            radius1=0.095,
            radius2=0.095,
            length=0.26,
            axis="Z",
            center_xyz=(cx + sx * 0.035, cy + 0.08, cz),
            segments=20,
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=10,  # R1_LampAmber glowing coil core
            rot_euler=(0.0, 0.0, tilt_z),
        )
        # Brass protective rib rings around the glowing coil window
        for rz in (-0.08, 0.0, 0.08):
            append_torus(
                bm,
                major_radius=0.101,
                minor_radius=0.011,
                axis="Z",
                center_xyz=(cx + sx * 0.035, cy + 0.08, cz + rz),
                major_seg=20,
                minor_seg=6,
                mat_idx=4,  # R1_Brass
                rot_euler=(0.0, 0.0, tilt_z),
            )
        # Brass end-caps on coil canister
        for rz in (-0.145, 0.145):
            append_cylinder(
                bm,
                radius1=0.103,
                radius2=0.086,
                length=0.040,
                axis="Z",
                center_xyz=(cx + sx * 0.035, cy + 0.08, cz + rz),
                segments=20,
                bevel_width=0.005,
                bevel_segments=2,
                mat_idx=4,
                rot_euler=(0.0, 0.0, tilt_z),
            )
        # Outboard Copper Vacuum Cooling Fin Stack on outer cheek of canister (x = +/-0.70 m)
        for fi in range(4):
            fz = cz - 0.10 + fi * 0.066
            append_beveled_box(
                bm,
                size_xyz=(0.15, 0.25, 0.016),
                center_xyz=(cx + sx * 0.14, cy + 0.02, fz),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_Copper
                rot_euler=(0.0, 0.0, tilt_z),
            )

        # Heavy Braided Copper High-Voltage Conduit curving from shoulder canister down to CutterTurret
        for step_i in range(6):
            t = step_i / 5.0
            px = sx * (0.50 * (1.0 - t) + 0.16 * t)
            py = 0.90 * (1.0 - t) + 0.70 * t
            pz = 0.54 * (1.0 - t) + 0.72 * t
            append_cylinder(
                bm,
                radius1=0.028,
                radius2=0.028,
                length=0.085,
                axis="Z",
                center_xyz=(px, py, pz),
                segments=12,
                bevel_width=0.003,
                bevel_segments=1,
                mat_idx=5 if step_i % 2 == 0 else 6,  # Copper / Rubber armored conduit
            )

    # 3. Heavy Perforated Heat Shroud & Ignition Chamber around Front CutterTurret (y = 0.665, z = 0.64..0.90 m)
    ct_y = 0.665
    append_cylinder(
        bm,
        radius1=0.172,
        radius2=0.156,
        length=0.23,
        axis="Z",
        center_xyz=(0.0, ct_y, 0.74),
        segments=24,
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    for fz in (0.68, 0.76, 0.83):
        append_torus(
            bm,
            major_radius=0.176,
            minor_radius=0.015,
            axis="Z",
            center_xyz=(0.0, ct_y, fz),
            major_seg=24,
            minor_seg=8,
            mat_idx=5,  # R1_Copper
        )
    append_torus(
        bm,
        major_radius=0.146,
        minor_radius=0.019,
        axis="Z",
        center_xyz=(0.0, ct_y, 0.865),
        major_seg=24,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
    )
    append_beveled_box(
        bm,
        size_xyz=(0.25, 0.12, 0.19),
        center_xyz=(0.0, ct_y + 0.145, 0.71),
        bevel_width=0.010,
        bevel_segments=2,
        mat_idx=4,  # R1_Brass housing
    )
    append_beveled_box(
        bm,
        size_xyz=(0.19, 0.09, 0.15),
        center_xyz=(0.0, ct_y + 0.175, 0.72),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber hot ignition core
    )

    return create_mesh_node("Upgrade_B_VarA", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_b_variant_B(rover_root, mats):
    """
    Upgrade (b) Variant B: 'Over-Bonnet Arc-Exciter Box & Outboard Copper Radiator Wings'
    - Bolts a chunky industrial Arc-Exciter Generator Box (0.66 m wide x 0.28 m tall) atop the front bonnet
      plus cantilevered outboard copper radiator fin wings at x = +/-0.58 m, y = 1.00 m (breaking boldly
      outside the front fenders/shoulders in the rear chase camera) and a helical copper nozzle cage.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Central Over-Bonnet Arc-Exciter Box (y = 0.80..1.08 m, z = 0.44..0.76 m)
    append_beveled_box(
        bm,
        size_xyz=(0.66, 0.25, 0.30),
        center_xyz=(0.0, 0.92, 0.56),
        bevel_width=0.014,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
    )
    # Coral-orange top safety cap plate
    append_beveled_box(
        bm,
        size_xyz=(0.60, 0.055, 0.26),
        center_xyz=(0.0, 1.05, 0.55),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=1,  # R1_OrangeStripe
    )
    _add_07_adapter_tag(bm, (0.0, 1.08, 0.55), normal_axis="Y", sign_dir=1.0)

    # Wide Panoramic Glowing Coil Window across front/top bevel of Exciter Box
    append_beveled_box(
        bm,
        size_xyz=(0.48, 0.13, 0.14),
        center_xyz=(0.0, 0.96, 0.68),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber
    )
    for bx in (-0.16, -0.055, 0.055, 0.16):
        append_beveled_box(
            bm,
            size_xyz=(0.018, 0.14, 0.15),
            center_xyz=(bx, 0.96, 0.68),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass window grille bars
        )

    # 2. Outboard Cantilevered Vacuum Radiator Fin Wings (x = +/-0.58 m, y = 0.86..1.24 m, z = 0.42 m)
    # Projects outside both front wheels & neck in the 7 m chase camera!
    for sx in (-1.0, 1.0):
        rx = sx * 0.56
        ry = 1.02
        rz = 0.42
        append_beveled_box(
            bm,
            size_xyz=(0.26, 0.28, 0.26),
            center_xyz=(rx, ry, rz),
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal frame
        )
        # 6 vertical copper radiator plates per side (vacuum radiative cooling)
        for fi in range(6):
            fz = rz - 0.11 + fi * 0.044
            append_beveled_box(
                bm,
                size_xyz=(0.32, 0.36, 0.016),
                center_xyz=(rx + sx * 0.04, ry + 0.02, fz),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_Copper
            )
        # Top coral & amber status beacon cap on each radiator wing (visible from rear chase camera!)
        append_beveled_box(
            bm,
            size_xyz=(0.24, 0.075, 0.22),
            center_xyz=(rx + sx * 0.03, ry + 0.21, rz),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=1,  # R1_OrangeStripe
        )
        append_beveled_box(
            bm,
            size_xyz=(0.18, 0.065, 0.18),
            center_xyz=(rx + sx * 0.03, ry + 0.26, rz),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber
        )

    # 3. Helical Copper Induction Cage around CutterTurret Nozzle (z = 0.66..0.92 m)
    append_helical_coil(
        bm,
        coil_radius=0.168,
        wire_radius=0.019,
        length=0.22,
        turns=4.5,
        axis="Z",
        center_xyz=(0.0, 0.665, 0.76),
        segs_per_turn=18,
        wire_segs=8,
        mat_idx=5,  # R1_Copper
    )
    append_torus(
        bm,
        major_radius=0.170,
        minor_radius=0.020,
        axis="Z",
        center_xyz=(0.0, 0.665, 0.88),
        major_seg=24,
        minor_seg=8,
        mat_idx=4,  # R1_Brass muzzle ring
    )

    return create_mesh_node("Upgrade_B_VarB", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


# ==============================================================================
# UPGRADE (c): SSR-3 HEADLAMP (R10-11 — Salvaged Elder 06 White Enamel Lamp)
# ==============================================================================

def build_upgrade_c_variant_A(rover_root, mats):
    """
    Upgrade (c) Variant A [RECOMMENDED]: 'Twin Salvaged SSR-3 Box Floodlights on Upper Visor Ear-Brackets'
    - Canon (maker-understanding.md §2): '06's white, boxy sealed-beam lamp welded beside 07's visor'
    - Mounts symmetric boxy 1978 R1_WhitePaint floodlight housings (with thin R1_OrangeStripe era-B band,
      rivets, bright weld beads, and cut '07' tag) onto upper brow-corner ear brackets at x = +/-0.52 m,
      y = 1.54 m, z = 0.25 m — sitting ABOVE the left/right cheek brass camera eyes (y = 1.11..1.35 m)
      so both the CRT face and half-lidded brass eye remain 100% open and unobscured!
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Raised Over-Brow Tubular Steel Conduit Bridge & Visor Clamp Arch (y = 1.56 m, z = 0.24 m)
    append_beveled_box(
        bm,
        size_xyz=(1.02, 0.045, 0.068),
        center_xyz=(0.0, 1.55, 0.24),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    _add_07_adapter_tag(bm, (0.0, 1.555, 0.28), normal_axis="Z", sign_dir=1.0)

    # 2. Symmetric Left & Right Salvaged SSR-3 Boxy White-Enamel Floodlight Pods at Upper Brow Corners (x = +/-0.53 m, y = 1.54 m, z = 0.25 m)
    for sx in (-1.0, 1.0):
        px = sx * 0.53
        py = 1.54
        pz = 0.25

        # Field-welded Kessler-Arai gusset bracket joining pod to upper Head brow corner (x = +/-0.385 m, y = 1.48 m)
        append_beveled_box(
            bm,
            size_xyz=(0.18, 0.16, 0.22),
            center_xyz=(sx * 0.41, py - 0.04, pz - 0.02),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal bracket
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.385, py - 0.11, pz - 0.10),
            (sx * 0.385, py - 0.01, pz - 0.10),
            beads=5,
            radius=0.009,
            mat_idx=3,  # Bright bare-steel weld beads
        )
        for by in (-0.07, 0.01):
            append_hex_bolt(
                bm,
                radius=0.011,
                height=0.018,
                axis="Z",
                center_xyz=(sx * 0.41, py + by, pz - 0.132),
                mat_idx=4,  # R1_Brass bolts visible from rear chase camera
            )

        # Salvaged 1978 SSR-3 Brake-Bent Boxy Housing in R1_WhitePaint (0.26 m W x 0.27 m H x 0.29 m D)
        append_beveled_box(
            bm,
            size_xyz=(0.26, 0.27, 0.29),
            center_xyz=(px, py, pz),
            bevel_width=0.016,
            bevel_segments=2,
            mat_idx=8,  # R1_WhitePaint (1978 Era-B donor livery!)
        )
        # Thin 1978 Era-B R1_OrangeStripe identification band wrapping the white housing
        append_beveled_box(
            bm,
            size_xyz=(0.268, 0.038, 0.298),
            center_xyz=(px, py - 0.055, pz),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,  # R1_OrangeStripe
        )
        # Brake-bent top glare hood in R1_WhitePaint extending forward & upward
        append_beveled_box(
            bm,
            size_xyz=(0.27, 0.040, 0.14),
            center_xyz=(px, py + 0.125, pz + 0.135),
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=8,
            rot_euler=(math.radians(8.0), 0.0, 0.0),
        )
        # Top Amber Clearance Strobe Pod atop each SSR-3 housing (y = 1.70 m)
        append_beveled_box(
            bm,
            size_xyz=(0.14, 0.055, 0.14),
            center_xyz=(px, py + 0.155, pz),
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber
        )
        # Front Machined Brass Bezel + Large Glowing R1_LampAmber Sealed-Beam Rectangular Lens
        append_beveled_box(
            bm,
            size_xyz=(0.215, 0.210, 0.034),
            center_xyz=(px, py + 0.01, pz + 0.145),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=4,  # R1_Brass bezel
        )
        append_beveled_box(
            bm,
            size_xyz=(0.185, 0.180, 0.030),
            center_xyz=(px, py + 0.01, pz + 0.158),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=10,  # R1_LampAmber sealed-beam emitter
        )
        # Rear-facing Amber Pilot Indicator Slit & Copper Heatsink Plug (visible from rear chase camera!)
        append_beveled_box(
            bm,
            size_xyz=(0.16, 0.058, 0.026),
            center_xyz=(px, py + 0.04, pz - 0.145),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber rear pilot glow
        )
        append_cylinder(
            bm,
            radius1=0.044,
            radius2=0.038,
            length=0.050,
            axis="Z",
            center_xyz=(px, py - 0.050, pz - 0.152),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=5,  # R1_Copper heatsink cap
        )

    return create_mesh_node("Upgrade_C_VarA", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)




def build_upgrade_c_variant_B(rover_root, mats):
    """
    Upgrade (c) Variant B: 'Over-Visor Salvaged SSR-3 Searchlight Rack & Twin Spot Bar'
    - Welds a tubular roll-cage arch above 07's CRT Head crown (y = 1.51..1.82 m) carrying Elder 06's
      large central boxy R1_WhitePaint searchlight (0.40 m wide x 0.24 m tall) flanked by two auxiliary
      white-enamel spot pods (total roof span = 0.86 m).
    - Raises 07's head silhouette by +0.29 m in the chase camera!
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    ry = 1.66
    rz = 0.22
    # 1. Tubular Steel Roll-Cage Arch welded to Head sides (x = +/-0.39 m, y = 1.38..1.66 m)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.048, 0.30, 0.095),
            center_xyz=(sx * 0.39, 1.50, rz),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal upright stanchion
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.375, 1.36, rz - 0.04),
            (sx * 0.375, 1.36, rz + 0.04),
            beads=5,
            radius=0.008,
            mat_idx=3,
        )
        for by in (1.40, 1.48):
            append_hex_bolt(
                bm,
                radius=0.010,
                height=0.016,
                axis="X",
                center_xyz=(sx * 0.418, by, rz),
                mat_idx=4,
            )

    # Horizontal crossbar spanning x = -0.44..+0.44 m at y = 1.56 m
    append_beveled_box(
        bm,
        size_xyz=(0.88, 0.042, 0.085),
        center_xyz=(0.0, 1.56, rz),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,
    )
    _add_07_adapter_tag(bm, (0.28, 1.56, rz - 0.046), normal_axis="Z", sign_dir=-1.0)

    # 2. Central Large Salvaged SSR-3 Searchlight Box in R1_WhitePaint (0.40 m W x 0.24 m H x 0.26 m D)
    append_beveled_box(
        bm,
        size_xyz=(0.40, 0.24, 0.26),
        center_xyz=(0.0, ry + 0.03, rz + 0.02),
        bevel_width=0.015,
        bevel_segments=2,
        mat_idx=8,  # R1_WhitePaint
    )
    # Era-B thin R1_OrangeStripe wrap band
    append_beveled_box(
        bm,
        size_xyz=(0.408, 0.032, 0.266),
        center_xyz=(0.0, ry - 0.03, rz + 0.02),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=1,  # R1_OrangeStripe
    )
    # Front Brass Frame & Twin Glowing Amber Sealed-Beam Lenses
    append_beveled_box(
        bm,
        size_xyz=(0.35, 0.18, 0.030),
        center_xyz=(0.0, ry + 0.03, rz + 0.15),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=4,  # R1_Brass
    )
    append_beveled_box(
        bm,
        size_xyz=(0.31, 0.145, 0.026),
        center_xyz=(0.0, ry + 0.03, rz + 0.162),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber
    )
    # Rear-facing backlit amber status bar on back of central searchlight (for chase camera readability)
    append_beveled_box(
        bm,
        size_xyz=(0.26, 0.055, 0.024),
        center_xyz=(0.0, ry + 0.04, rz - 0.112),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=10,
    )

    # 3. Flanking Auxiliary White-Enamel Spot Pods (x = +/-0.34 m, y = 1.66 m)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.17, 0.18, 0.20),
            center_xyz=(sx * 0.34, ry + 0.01, rz + 0.01),
            bevel_width=0.010,
            bevel_segments=2,
            mat_idx=8,  # R1_WhitePaint
        )
        append_cylinder(
            bm,
            radius1=0.064,
            radius2=0.060,
            length=0.035,
            axis="Z",
            center_xyz=(sx * 0.34, ry + 0.01, rz + 0.115),
            segments=16,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber
        )

    return create_mesh_node("Upgrade_C_VarB", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


# ==============================================================================
# UPGRADE (d): CELL PACK (R11 — Peak Key, Salvaged Elder 06 Battery Cells)
# ==============================================================================

def build_upgrade_d_variant_A(rover_root, mats):
    """
    Upgrade (d) Variant A [RECOMMENDED]: 'Twin Flank Saddle-Pod Battery Panniers'
    - Bolts two chunky chamfered battery saddle-pods (0.30 m W x 0.44 m H x 0.56 m L each) onto 07's
      left and right mid-flank Kessler-Arai sockets (x = +/-0.64 m, y = 0.62..1.18 m, z = -0.10 m),
      projecting cleanly past the wheel fenders and widening 07's silhouette by +77% (0.88 m -> 1.56 m)!
    - Features GoldFoil MLI thermal blankets, coral end-caps, copper busbars, and 3 glowing vertical/rear
      Amber charge-level bars per pod.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    for sx in (-1.0, 1.0):
        px = sx * 0.64
        py = 0.84
        pz = -0.10

        # 1. Bolted Kessler-Arai Flank Cradle Frame & Upper Shoulder Tie-Strut
        append_beveled_box(
            bm,
            size_xyz=(0.18, 0.36, 0.58),
            center_xyz=(sx * 0.48, py, pz),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.435, 0.94, pz - 0.24),
            (sx * 0.435, 0.94, pz + 0.24),
            beads=8,
            radius=0.009,
            mat_idx=3,
        )
        # GoldFoil thermal blanket layer wrapping the inner & upper saddle-pod shoulder
        append_beveled_box(
            bm,
            size_xyz=(0.08, 0.36, 0.52),
            center_xyz=(sx * 0.52, py + 0.04, pz),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=7,  # R1_GoldFoil
        )

        # 2. Main Chamfered Saddle-Pod Battery Housing (0.28 m W x 0.42 m H x 0.54 m L)
        append_beveled_box(
            bm,
            size_xyz=(0.28, 0.42, 0.54),
            center_xyz=(px, py, pz),
            bevel_width=0.028,
            bevel_segments=2,
            mat_idx=0,  # R1_CreamEnamel
        )
        # Coral-Orange End-Cap Accent Bands at front and rear of pod
        for sz in (-1.0, 1.0):
            append_beveled_box(
                bm,
                size_xyz=(0.288, 0.428, 0.076),
                center_xyz=(px, py, pz + sz * 0.215),
                bevel_width=0.012,
                bevel_segments=2,
                mat_idx=1,  # R1_OrangeStripe
            )

        # Twin Heavy Grey-Metal Straps + Brass Buckles wrapping the pod
        for sz in (-0.12, 0.12):
            append_beveled_box(
                bm,
                size_xyz=(0.298, 0.438, 0.040),
                center_xyz=(px, py, pz + sz),
                bevel_width=0.006,
                bevel_segments=1,
                mat_idx=2,
            )
            append_hex_bolt(
                bm,
                radius=0.015,
                height=0.024,
                axis="X",
                center_xyz=(px + sx * 0.15, py + 0.09, pz + sz),
                mat_idx=4,  # R1_Brass
            )

        # Top Twin Brass Cell Caps & Longitudinal Copper Busbar rising to y = 1.16 m
        for sz in (-0.15, 0.15):
            append_cylinder(
                bm,
                radius1=0.078,
                radius2=0.070,
                length=0.075,
                axis="Y",
                center_xyz=(px, py + 0.235, pz + sz),
                segments=18,
                bevel_width=0.006,
                bevel_segments=2,
                mat_idx=4,  # R1_Brass cell cap
            )
        append_beveled_box(
            bm,
            size_xyz=(0.052, 0.032, 0.42),
            center_xyz=(px, py + 0.275, pz),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=5,  # R1_Copper top busbar
        )

        # 3. Glowing Amber Charge-Level Sight Bars (3 vertical bars on outer flank + rear-facing gauge window!)
        for bi, bz in enumerate((-0.12, 0.0, 0.12)):
            append_beveled_box(
                bm,
                size_xyz=(0.034, 0.24, 0.048),
                center_xyz=(px + sx * 0.135, py - 0.01, pz + bz),
                bevel_width=0.005,
                bevel_segments=1,
                mat_idx=10,  # R1_LampAmber
            )
        # Rear-facing glowing charge bar on the back end of each pod (directly faces the 7 m chase camera!)
        for ry_off in (-0.10, 0.0, 0.10):
            append_beveled_box(
                bm,
                size_xyz=(0.18, 0.052, 0.032),
                center_xyz=(px, py + ry_off, pz - 0.272),
                bevel_width=0.005,
                bevel_segments=1,
                mat_idx=10,  # R1_LampAmber
            )

    _add_07_adapter_tag(bm, (-0.785, 0.84, -0.10), normal_axis="X", sign_dir=-1.0)
    return create_mesh_node("Upgrade_D_VarA", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_d_variant_B(rover_root, mats):
    """
    Upgrade (d) Variant B: 'High-Output Dorsal Power Backpack & Space Radiator Stack'
    - Mounts a wide dorsal battery backpack (1.16 m wide x 0.36 m tall x 0.42 m deep) on a raised
      cantilevered cradle over 07's rear upper deck (y = 1.00..1.48 m, z = -0.54 m), with twin outboard
      GoldFoil & Copper vacuum radiator wings projecting at x = +/-0.66 m (into the open shoulder sky beside
      the CRT head!) and 4 upright brass/amber cell towers.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    by = 1.16
    bz = -0.54

    # 1. Cantilevered Dorsal Cradle Legs bolted to rear shoulder sockets (x = +/-0.40 m)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.11, 0.26, 0.38),
            center_xyz=(sx * 0.40, 1.02, bz),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.35, 0.92, bz - 0.15),
            (sx * 0.35, 0.92, bz + 0.15),
            beads=6,
            radius=0.009,
            mat_idx=3,
        )

    # 2. Main Wide Dorsal Battery Backpack Shell (1.16 m W x 0.28 m H x 0.40 m D)
    append_beveled_box(
        bm,
        size_xyz=(1.16, 0.28, 0.40),
        center_xyz=(0.0, by, bz),
        bevel_width=0.022,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
    )
    # Wide Coral-Orange Identification Band wrapping the backpack
    append_beveled_box(
        bm,
        size_xyz=(1.17, 0.075, 0.408),
        center_xyz=(0.0, by + 0.045, bz),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=1,  # R1_OrangeStripe
    )
    _add_07_adapter_tag(bm, (0.0, by + 0.045, bz - 0.206), normal_axis="Z", sign_dir=-1.0)

    # 3. 4 Tall Upright Cylindrical High-Density Cell Towers crowning the Backpack (y = 1.28..1.52 m)
    for cx in (-0.44, -0.15, 0.15, 0.44):
        append_cylinder(
            bm,
            radius1=0.088,
            radius2=0.080,
            length=0.20,
            axis="Y",
            center_xyz=(cx, by + 0.22, bz + 0.02),
            segments=18,
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=4,  # R1_Brass cell crown
        )
        append_cylinder(
            bm,
            radius1=0.062,
            radius2=0.062,
            length=0.055,
            axis="Y",
            center_xyz=(cx, by + 0.33, bz + 0.02),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber top cell beacon
        )

    # Outboard Copper Vacuum Radiator Fin Wings & GoldFoil Shrouds (x = +/-0.66 m, y = 1.04..1.38 m)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.08, 0.26, 0.34),
            center_xyz=(sx * 0.61, by, bz),
            bevel_width=0.006,
            bevel_segments=1,
            mat_idx=7,  # R1_GoldFoil MLI blanket
        )
        for fi in range(5):
            fz = bz - 0.14 + fi * 0.07
            append_beveled_box(
                bm,
                size_xyz=(0.18, 0.32, 0.018),
                center_xyz=(sx * 0.67, by + 0.02, fz),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_Copper
            )

    # 4. Rear-Facing 4-Column Glowing Amber Charge Matrix (facing straight into the chase camera!)
    append_beveled_box(
        bm,
        size_xyz=(0.88, 0.18, 0.034),
        center_xyz=(0.0, by - 0.01, bz - 0.20),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,  # Dark bezel frame
    )
    for cx in (-0.31, -0.105, 0.105, 0.31):
        append_beveled_box(
            bm,
            size_xyz=(0.14, 0.13, 0.038),
            center_xyz=(cx, by - 0.01, bz - 0.208),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=10,  # R1_LampAmber
        )

    return create_mesh_node("Upgrade_D_VarB", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


# ==============================================================================
# UPGRADE (e): HEX CHARGE ADAPTER (R10 / Tycho Consortium Hex Contact Plate)
# ==============================================================================

def build_upgrade_e_variant_A(rover_root, mats):
    """
    Upgrade (e) Variant A [RECOMMENDED]: 'Rear Pantograph Arm & Canted Tycho Hex Collector Shield'
    - Canon (maker-understanding.md §2 & opening-v3-purpose.md §5): Tycho Consortium hex contact plate
      (R1_WhitePaint composite + R1_TealMark #3E7A82 hexagonal livery) adapted onto 07's charge port.
    - Mounts a folding pantograph arm on 07's rear valence/bumper sockets (z = -0.72..-1.14 m) holding a
      large Ø 0.68 m hexagonal Tycho collector shield canted at 36 deg below/behind the rear bumper,
      flanked by twin face-on hexagonal White/Teal Tycho converter pods on rear bumper outriggers (x = +/-0.64 m).
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Twin Rear-Bumper Outrigger Tycho Hexagonal Power-Conditioning Pods (x = +/-0.64 m, y = 0.50 m, z = -0.78 m)
    # Oriented with axis='Z' so the White + Teal hexagonal faces point directly into the 7m chase camera outside the rear wheels!
    for sx in (-1.0, 1.0):
        px = sx * 0.64
        py = 0.50
        pz = -0.78
        # Kessler-Arai welded outrigger mounting bracket from rear bumper corner
        append_beveled_box(
            bm,
            size_xyz=(0.30, 0.14, 0.16),
            center_xyz=(sx * 0.46, py, pz + 0.04),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.36, py - 0.06, pz - 0.04),
            (sx * 0.36, py + 0.06, pz - 0.04),
            beads=5,
            radius=0.008,
            mat_idx=3,
        )
        # Hexagonal Tycho White-and-Teal Converter Pod facing the chase camera (axis='Z')
        _append_regular_hex_prism(
            bm,
            radius=0.185,
            depth=0.18,
            axis="Z",
            center_xyz=(px, py, pz),
            bevel_width=0.010,
            mat_idx=8,  # R1_WhitePaint
        )
        _append_regular_hex_prism(
            bm,
            radius=0.145,
            depth=0.192,
            axis="Z",
            center_xyz=(px, py, pz - 0.004),
            bevel_width=0.006,
            mat_idx=9,  # R1_TealMark (#3E7A82 Tycho accent!)
        )
        _append_regular_hex_prism(
            bm,
            radius=0.085,
            depth=0.204,
            axis="Z",
            center_xyz=(px, py, pz - 0.008),
            bevel_width=0.005,
            mat_idx=5,  # R1_Copper inductive hub
        )
        _append_regular_hex_prism(
            bm,
            radius=0.042,
            depth=0.214,
            axis="Z",
            center_xyz=(px, py, pz - 0.012),
            bevel_width=0.004,
            mat_idx=10,  # R1_LampAmber pilot core
        )
        # Armored umbilical cable curving from converter pod to central hex arm
        append_beveled_box(
            bm,
            size_xyz=(0.38, 0.042, 0.042),
            center_xyz=(sx * 0.36, 0.44, -0.88),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=6,  # R1_Rubber cable
        )

    # 2. Central Articulated Pantograph Linkage bolted to Rear Valence (y = 0.38..0.60 m, z = -0.72..-1.06 m)
    for sx in (-0.18, 0.18):
        append_beveled_box(
            bm,
            size_xyz=(0.052, 0.095, 0.14),
            center_xyz=(sx, 0.46, -0.74),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal mount block
        )
        # Lower & upper pantograph links angling back & down to the collector shield
        append_beveled_box(
            bm,
            size_xyz=(0.040, 0.046, 0.34),
            center_xyz=(sx, 0.44, -0.90),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=3,  # R1_BareSteel link
            rot_euler=(math.radians(-12.0), 0.0, 0.0),
        )
        append_cylinder(
            bm,
            radius1=0.030,
            radius2=0.030,
            length=0.075,
            axis="X",
            center_xyz=(sx, 0.42, -1.02),
            segments=14,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass pivot joint
        )

    # Cut '07' coral adapter plaque welded onto top cross-member of pantograph
    _add_07_adapter_tag(bm, (0.0, 0.52, -0.88), normal_axis="Y", sign_dir=1.0)

    # 3. Large Canted Tycho Hexagonal Collector Shield (Ø 0.68 m across corners, canted at 36 deg at y = 0.38 m, z = -1.10 m)
    # Extending below/behind the rear bumper so the lower half of the hexagon projects into open silhouette space!
    hx, hy, hz = 0.0, 0.38, -1.10
    hex_tilt = (math.radians(-36.0), 0.0, 0.0)

    # Outer White Composite Hexagonal Shield Plate (R1_WhitePaint)
    _append_regular_hex_prism(
        bm,
        radius=0.34,
        depth=0.062,
        axis="Z",
        center_xyz=(hx, hy, hz),
        bevel_width=0.010,
        mat_idx=8,  # R1_WhitePaint
        rot_euler=hex_tilt,
    )
    # Bold Tycho Teal Hexagonal Livery Ring (R1_TealMark #3E7A82 — One Maker Accent!)
    _append_regular_hex_prism(
        bm,
        radius=0.28,
        depth=0.072,
        axis="Z",
        center_xyz=(hx, hy, hz - 0.005),
        bevel_width=0.008,
        mat_idx=9,  # R1_TealMark
        rot_euler=hex_tilt,
    )
    # Inner Recessed White Composite Core Plate
    _append_regular_hex_prism(
        bm,
        radius=0.20,
        depth=0.082,
        axis="Z",
        center_xyz=(hx, hy, hz - 0.008),
        bevel_width=0.006,
        mat_idx=8,  # R1_WhitePaint
        rot_euler=hex_tilt,
    )
    # 6 Copper Inductive Contact Shoes arranged in a hexagon on the collector face
    rot_m = _unity_rot_matrix(hex_tilt)
    for i in range(6):
        ang = i * math.pi / 3.0
        r_pad = 0.148
        dx = r_pad * math.cos(ang)
        dy_local = r_pad * math.sin(ang)
        dz_local = -0.042
        off_b = rot_m @ u_pos(dx, dy_local, dz_local)
        wx = hx + off_b.x
        wy = hy + off_b.z
        wz = hz + off_b.y
        _append_regular_hex_prism(
            bm,
            radius=0.042,
            depth=0.028,
            axis="Z",
            center_xyz=(wx, wy, wz),
            bevel_width=0.003,
            mat_idx=5,  # R1_Copper contact shoe
            rot_euler=hex_tilt,
        )

    # Central Glowing Amber Charge-Handshake Core
    _append_regular_hex_prism(
        bm,
        radius=0.068,
        depth=0.092,
        axis="Z",
        center_xyz=(hx, hy, hz - 0.010),
        bevel_width=0.005,
        mat_idx=10,  # R1_LampAmber
        rot_euler=hex_tilt,
    )

    return create_mesh_node("Upgrade_E_VarA", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_e_variant_B(rover_root, mats):
    """
    Upgrade (e) Variant B: 'Twin Side-Outrigger Articulated Hex Collector Skids'
    - Bolts two articulated swing-arm outriggers onto 07's lower rear-flank sockets (x = +/-0.45..+/-0.78 m,
      y = 0.48 m, z = -0.82 m), each carrying a face-on canted Ø 0.56 m Tycho hexagonal contact shield
      (R1_WhitePaint + R1_TealMark + R1_Copper) that folds down flat onto a Tycho hex pad when docking
      and projects +0.42 m outside both rear wheels in the chase camera silhouette.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    for sx in (-1.0, 1.0):
        pz = -0.76
        # 1. Bolted Rear-Flank Socket Hinge Box & Worm-Gear Actuator (x = +/-0.44 m, y = 0.54 m, z = -0.74 m)
        append_beveled_box(
            bm,
            size_xyz=(0.16, 0.18, 0.22),
            center_xyz=(sx * 0.42, 0.52, -0.72),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.36, 0.44, -0.80),
            (sx * 0.36, 0.60, -0.80),
            beads=6,
            radius=0.008,
            mat_idx=3,
        )
        append_cylinder(
            bm,
            radius1=0.046,
            radius2=0.046,
            length=0.22,
            axis="Z",
            center_xyz=(sx * 0.52, 0.56, pz),
            segments=16,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass actuator drum
        )
        # Articulated Twin Strut Arm reaching out past the rear wheels to x = +/-0.76 m
        for dy in (-0.05, 0.05):
            append_beveled_box(
                bm,
                size_xyz=(0.36, 0.046, 0.048),
                center_xyz=(sx * 0.59, 0.50 + dy, pz - 0.04),
                bevel_width=0.005,
                bevel_segments=1,
                mat_idx=3,  # R1_BareSteel
                rot_euler=(0.0, -sx * math.radians(16.0), -sx * math.radians(10.0)),
            )

        # 2. Canted Face-On Tycho Hexagonal Collector Shield (Ø 0.58 m, axis='Z') at x = +/-0.78 m, y = 0.48 m, z = -0.84 m
        hx = sx * 0.78
        hy = 0.48
        hz = -0.84
        rot_u = (math.radians(-24.0), sx * math.radians(16.0), 0.0)

        # White Composite Outer Hex Plate
        _append_regular_hex_prism(
            bm,
            radius=0.295,
            depth=0.068,
            axis="Z",
            center_xyz=(hx, hy, hz),
            bevel_width=0.010,
            mat_idx=8,  # R1_WhitePaint
            rot_euler=rot_u,
        )
        # Tycho Teal Inner Hex Ring (R1_TealMark #3E7A82)
        _append_regular_hex_prism(
            bm,
            radius=0.240,
            depth=0.078,
            axis="Z",
            center_xyz=(hx, hy, hz - 0.005),
            bevel_width=0.007,
            mat_idx=9,  # R1_TealMark
            rot_euler=rot_u,
        )
        # Copper Inductive Contact Ring
        _append_regular_hex_prism(
            bm,
            radius=0.165,
            depth=0.088,
            axis="Z",
            center_xyz=(hx, hy, hz - 0.010),
            bevel_width=0.005,
            mat_idx=5,  # R1_Copper
            rot_euler=rot_u,
        )
        # Central Amber Lock Core
        _append_regular_hex_prism(
            bm,
            radius=0.076,
            depth=0.098,
            axis="Z",
            center_xyz=(hx, hy, hz - 0.015),
            bevel_width=0.004,
            mat_idx=10,  # R1_LampAmber
            rot_euler=rot_u,
        )

    _add_07_adapter_tag(bm, (-0.52, 0.62, -0.76), normal_axis="Y", sign_dir=1.0)
    return create_mesh_node("Upgrade_E_VarB", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


# ==============================================================================
# ROUND 2 BUILDERS: SLIMMED a–d (PICKED 'A' DESIGNS, -35..40% VOLUME, 6–8% SIL)
# + REDESIGNED (e) HEX CHARGE ADAPTER AT CHARGING CONTACT (FRONT BUMPER / FLANK)
# ==============================================================================

def build_upgrade_a_variant_A_r2(rover_root, mats):
    """
    Round 2 Slimmed Upgrade (a) Variant A [OWNER PICK, SLIMMED ~36% VOLUME]:
    'Dish-Crown Mast & Symmetric Telemetry Bridge (Slim)'
    - Thinner dorsal bridge (0.72 x 0.036 x 0.080 m) tucked tight to rear shoulders (y = 0.922, z = -0.46)
    - Compact left-shoulder preamp cylinder (r = 0.044 m) & smaller crossed D/F loops (r = 0.084 m, y = 1.21 m)
    - Slimmer right-shoulder stepped mast rising to y = 2.02 m with a compact Ø 0.33 m parabolic ring-dish
      receiver (y = 1.70 m), coral call-sign drum, and tidy Amber apex lantern (r = 0.039 m).
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Slimmed Bolted Kessler-Arai Dorsal Transceiver Bridge across rear shoulders (z = -0.46 m)
    append_beveled_box(
        bm,
        size_xyz=(0.72, 0.036, 0.080),
        center_xyz=(0.0, 0.922, -0.46),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.108, 0.064, 0.112),
            center_xyz=(sx * 0.35, 0.912, -0.46),
            bevel_width=0.008,
            bevel_segments=2,
            mat_idx=2,
        )
        for bz in (-0.038, 0.038):
            for bx in (-0.032, 0.032):
                append_hex_bolt(
                    bm,
                    radius=0.008,
                    height=0.011,
                    axis="Y",
                    center_xyz=(sx * 0.35 + bx, 0.948, -0.46 + bz),
                    mat_idx=4,  # R1_Brass
                )
        _add_weld_bead_strip(
            bm,
            (sx * 0.295, 0.905, -0.50),
            (sx * 0.295, 0.905, -0.42),
            beads=5,
            radius=0.007,
            mat_idx=3,
        )

    _add_07_adapter_tag(bm, (0.408, 0.912, -0.46), normal_axis="X", sign_dir=1.0)

    # 2. Slimmed Left-Shoulder Counter-Unit: Compact Preamp Canister + Smaller Crossed D/F Loops
    lx, lz = -0.35, -0.46
    append_cylinder(
        bm,
        radius1=0.048,
        radius2=0.044,
        length=0.18,
        axis="Y",
        center_xyz=(lx, 1.03, lz),
        segments=18,
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
    )
    append_cylinder(
        bm,
        radius1=0.051,
        radius2=0.051,
        length=0.036,
        axis="Y",
        center_xyz=(lx, 1.085, lz),
        segments=18,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=1,  # R1_OrangeStripe collar
    )
    append_torus(
        bm,
        major_radius=0.095,
        minor_radius=0.011,
        axis="Z",
        center_xyz=(lx, 1.235, lz),
        major_seg=20,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
    )
    append_torus(
        bm,
        major_radius=0.086,
        minor_radius=0.0095,
        axis="X",
        center_xyz=(lx, 1.235, lz),
        major_seg=20,
        minor_seg=8,
        mat_idx=5,  # R1_Copper
    )
    append_spherical_dome(
        bm,
        radius=0.022,
        dome_height=0.020,
        axis="Y",
        center_xyz=(lx, 1.34, lz),
        segments=14,
        rings=4,
        mat_idx=10,  # R1_LampAmber status pip
    )

    # 3. Slimmed Right-Shoulder Mast + Compact Ø 0.37 m Parabolic Ring-Dish + Amber Beacon Tip
    rx, rz = 0.35, -0.46
    append_cylinder(
        bm,
        radius1=0.042,
        radius2=0.035,
        length=0.30,
        axis="Y",
        center_xyz=(rx, 1.09, rz),
        segments=18,
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=2,
    )
    append_helical_coil(
        bm,
        coil_radius=0.047,
        wire_radius=0.008,
        length=0.21,
        turns=6.0,
        axis="Y",
        center_xyz=(rx, 1.095, rz),
        segs_per_turn=14,
        wire_segs=6,
        mat_idx=5,  # R1_Copper
    )
    append_cylinder(
        bm,
        radius1=0.023,
        radius2=0.017,
        length=0.82,
        axis="Y",
        center_xyz=(rx, 1.62, rz),
        segments=16,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=3,  # R1_BareSteel
    )
    for cy in (1.27, 1.56, 1.89):
        append_cylinder(
            bm,
            radius1=0.030,
            radius2=0.029,
            length=0.034,
            axis="Y",
            center_xyz=(rx, cy, rz),
            segments=16,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass
        )

    # Compact Canted Parabolic Ring-Dish Receiver at y = 1.72 m (Ø 0.37 m)
    dish_y = 1.72
    dish_rot = (math.radians(-22.0), math.radians(15.0), 0.0)
    append_torus(
        bm,
        major_radius=0.184,
        minor_radius=0.014,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.03),
        major_seg=26,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
        rot_euler=dish_rot,
    )
    append_torus(
        bm,
        major_radius=0.116,
        minor_radius=0.010,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.045),
        major_seg=22,
        minor_seg=6,
        mat_idx=5,  # R1_Copper
        rot_euler=dish_rot,
    )
    append_cylinder(
        bm,
        radius1=0.060,
        radius2=0.052,
        length=0.090,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.022),
        segments=18,
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=0,  # R1_CreamEnamel
        rot_euler=dish_rot,
    )
    append_cylinder(
        bm,
        radius1=0.062,
        radius2=0.062,
        length=0.034,
        axis="Z",
        center_xyz=(rx, dish_y, rz + 0.030),
        segments=18,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=1,  # R1_OrangeStripe maker accent band
        rot_euler=dish_rot,
    )
    append_cylinder(
        bm,
        radius1=0.054,
        radius2=0.054,
        length=0.015,
        axis="Z",
        center_xyz=(rx, dish_y, rz - 0.020),
        segments=16,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=7,  # R1_GoldFoil rear MLI blanket
        rot_euler=dish_rot,
    )
    for ang_deg in (0.0, 90.0):
        append_beveled_box(
            bm,
            size_xyz=(0.360, 0.014, 0.014),
            center_xyz=(rx, dish_y, rz + 0.034),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=3,
            rot_euler=(dish_rot[0], dish_rot[1], math.radians(ang_deg)),
        )

    # Tidy Apex Lantern Call-Sign Beacon Tip at y = 2.02..2.09 m
    append_cylinder(
        bm,
        radius1=0.046,
        radius2=0.040,
        length=0.092,
        axis="Y",
        center_xyz=(rx, 2.025, rz),
        segments=18,
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber
    )
    append_spherical_dome(
        bm,
        radius=0.043,
        dome_height=0.040,
        axis="Y",
        center_xyz=(rx, 2.072, rz),
        segments=18,
        rings=5,
        mat_idx=10,
    )
    append_torus(
        bm,
        major_radius=0.052,
        minor_radius=0.0065,
        axis="Y",
        center_xyz=(rx, 2.025, rz),
        major_seg=18,
        minor_seg=6,
        mat_idx=4,  # R1_Brass guard ring
    )

    return create_mesh_node("Upgrade_A_VarA_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_b_variant_A_r2(rover_root, mats):
    """
    Round 2 Slimmed Upgrade (b) Variant A [OWNER PICK, SLIMMED ~38% VOLUME]:
    'Twin-Shoulder Plasma Ignition Saddle & Glowing Coil Shroud (Slim)'
    - Thinner horseshoe yoke hugging upper front bonnet (0.80 x 0.046 x 0.16 m at y = 0.855, z = 0.42)
    - Compact canted coral ignition packs tucked closer to shoulders (x = +/-0.49 m, y = 0.945 m, z = 0.48 m;
      0.185 x 0.185 x 0.235 m) with r = 0.072 m Amber plasma-coil cylinders & tight 4-plate copper radiator fins
    - Sleeker perforated heat shroud (r = 0.144 m) & low-profile top Amber ignition window on CutterTurret.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Slimmed Horseshoe Saddle Yoke hugging upper front bonnet (y = 0.855 m, z = 0.42 m)
    append_beveled_box(
        bm,
        size_xyz=(0.80, 0.046, 0.16),
        center_xyz=(0.0, 0.855, 0.42),
        bevel_width=0.007,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    _add_07_adapter_tag(bm, (0.0, 0.884, 0.44), normal_axis="Y", sign_dir=1.0)

    # 2. Compact Symmetric Twin Shoulder Plasma-Coil Canisters (x = +/-0.525 m, y = 0.985 m, z = 0.46 m)
    for sx in (-1.0, 1.0):
        cx = sx * 0.525
        cy = 0.985
        cz = 0.46
        tilt_z = -sx * math.radians(15.0)

        # Tighter mounting bracket & bolts on upper bonnet shoulder
        append_beveled_box(
            bm,
            size_xyz=(0.19, 0.11, 0.19),
            center_xyz=(sx * 0.42, 0.895, cz - 0.02),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,
        )
        for bz in (-0.058, 0.058):
            append_hex_bolt(
                bm,
                radius=0.009,
                height=0.014,
                axis="X",
                center_xyz=(sx * 0.518, 0.895, cz - 0.02 + bz),
                mat_idx=4,
            )
        _add_weld_bead_strip(
            bm,
            (sx * 0.33, 0.885, cz - 0.095),
            (sx * 0.33, 0.885, cz + 0.065),
            beads=5,
            radius=0.007,
            mat_idx=3,
        )

        # Slimmed Coral-Orange Ignition Pack Casing Block (-35% volume vs Round 1)
        append_beveled_box(
            bm,
            size_xyz=(0.205, 0.205, 0.255),
            center_xyz=(cx, cy, cz),
            bevel_width=0.012,
            bevel_segments=2,
            mat_idx=1,  # R1_OrangeStripe
            rot_euler=(0.0, 0.0, tilt_z),
        )
        # Compact Glowing Amber Plasma-Coil Sight Cylinder
        append_cylinder(
            bm,
            radius1=0.082,
            radius2=0.082,
            length=0.215,
            axis="Z",
            center_xyz=(cx + sx * 0.028, cy + 0.068, cz),
            segments=18,
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=10,  # R1_LampAmber glowing coil core
            rot_euler=(0.0, 0.0, tilt_z),
        )
        for rz in (-0.065, 0.0, 0.065):
            append_torus(
                bm,
                major_radius=0.088,
                minor_radius=0.009,
                axis="Z",
                center_xyz=(cx + sx * 0.028, cy + 0.068, cz + rz),
                major_seg=18,
                minor_seg=6,
                mat_idx=4,  # R1_Brass
                rot_euler=(0.0, 0.0, tilt_z),
            )
        for rz in (-0.120, 0.120):
            append_cylinder(
                bm,
                radius1=0.088,
                radius2=0.074,
                length=0.032,
                axis="Z",
                center_xyz=(cx + sx * 0.028, cy + 0.068, cz + rz),
                segments=18,
                bevel_width=0.004,
                bevel_segments=2,
                mat_idx=4,
                rot_euler=(0.0, 0.0, tilt_z),
            )
        # Compact 4-Plate Outboard Copper Vacuum Cooling Fin Stack (reaching x = +/-0.64 m, inside tyres!)
        for fi in range(4):
            fz = cz - 0.082 + fi * 0.055
            append_beveled_box(
                bm,
                size_xyz=(0.112, 0.195, 0.013),
                center_xyz=(cx + sx * 0.114, cy + 0.018, fz),
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_Copper
                rot_euler=(0.0, 0.0, tilt_z),
            )

        # Slimmer Braided Copper Conduit curving into CutterTurret
        for step_i in range(6):
            t = step_i / 5.0
            px = sx * (0.46 * (1.0 - t) + 0.14 * t)
            py = 0.90 * (1.0 - t) + 0.69 * t
            pz = 0.52 * (1.0 - t) + 0.72 * t
            append_cylinder(
                bm,
                radius1=0.021,
                radius2=0.021,
                length=0.072,
                axis="Z",
                center_xyz=(px, py, pz),
                segments=12,
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5 if step_i % 2 == 0 else 6,
            )

    # 3. Sleeker Perforated Heat Shroud & Ignition Chamber around Front CutterTurret (y = 0.665, z = 0.74 m)
    ct_y = 0.665
    append_cylinder(
        bm,
        radius1=0.146,
        radius2=0.134,
        length=0.195,
        axis="Z",
        center_xyz=(0.0, ct_y, 0.735),
        segments=24,
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    for fz in (0.68, 0.74, 0.80):
        append_torus(
            bm,
            major_radius=0.150,
            minor_radius=0.011,
            axis="Z",
            center_xyz=(0.0, ct_y, fz),
            major_seg=24,
            minor_seg=8,
            mat_idx=5,  # R1_Copper
        )
    append_torus(
        bm,
        major_radius=0.130,
        minor_radius=0.014,
        axis="Z",
        center_xyz=(0.0, ct_y, 0.845),
        major_seg=24,
        minor_seg=8,
        mat_idx=4,  # R1_Brass
    )
    append_beveled_box(
        bm,
        size_xyz=(0.185, 0.088, 0.145),
        center_xyz=(0.0, ct_y + 0.122, 0.71),
        bevel_width=0.008,
        bevel_segments=2,
        mat_idx=4,  # R1_Brass housing
    )
    append_beveled_box(
        bm,
        size_xyz=(0.145, 0.068, 0.115),
        center_xyz=(0.0, ct_y + 0.145, 0.715),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=10,  # R1_LampAmber hot ignition core
    )

    return create_mesh_node("Upgrade_B_VarA_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_c_variant_A_r2(rover_root, mats):
    """
    Round 2 Slimmed Upgrade (c) Variant A [OWNER PICK, SLIMMED ~35% VOLUME]:
    'Twin Salvaged SSR-3 Box Floodlights on Upper Visor Ear-Brackets (Slim)'
    - Low-profile over-brow conduit bar (0.90 x 0.030 x 0.048 m at y = 1.535, z = 0.24)
    - Compact 1978 SSR-3 white-enamel floodlight pods (0.220 x 0.230 x 0.240 m — 34% smaller volume!)
      tucked tight to upper brow corners (x = +/-0.502 m, y = 1.520 m, z = 0.25 m), keeping the CRT visor
      and cheek brass camera eye 100% open and cozy.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Slimmed Over-Brow Tubular Steel Conduit Bridge (y = 1.535 m, z = 0.24 m)
    append_beveled_box(
        bm,
        size_xyz=(0.90, 0.030, 0.048),
        center_xyz=(0.0, 1.535, 0.24),
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal
    )
    _add_07_adapter_tag(bm, (0.0, 1.540, 0.268), normal_axis="Z", sign_dir=1.0)

    # 2. Slimmed Left & Right Salvaged SSR-3 Boxy White-Enamel Floodlight Pods (x = +/-0.502 m, y = 1.520 m, z = 0.25 m)
    for sx in (-1.0, 1.0):
        px = sx * 0.502
        py = 1.520
        pz = 0.25

        # Compact field-welded Kessler-Arai gusset bracket
        append_beveled_box(
            bm,
            size_xyz=(0.14, 0.12, 0.17),
            center_xyz=(sx * 0.405, py - 0.03, pz - 0.015),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal bracket
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.388, py - 0.085, pz - 0.085),
            (sx * 0.388, py - 0.005, pz - 0.085),
            beads=5,
            radius=0.007,
            mat_idx=3,
        )
        for by in (-0.052, 0.005):
            append_hex_bolt(
                bm,
                radius=0.0085,
                height=0.014,
                axis="Z",
                center_xyz=(sx * 0.405, py + by, pz - 0.102),
                mat_idx=4,  # R1_Brass
            )

        # Compact 1978 SSR-3 Brake-Bent Boxy Housing in R1_WhitePaint (0.220 m W x 0.230 m H x 0.240 m D)
        append_beveled_box(
            bm,
            size_xyz=(0.220, 0.230, 0.240),
            center_xyz=(px, py, pz),
            bevel_width=0.012,
            bevel_segments=2,
            mat_idx=8,  # R1_WhitePaint
        )
        # Thin 1978 Era-B R1_OrangeStripe identification band
        append_beveled_box(
            bm,
            size_xyz=(0.226, 0.030, 0.246),
            center_xyz=(px, py - 0.045, pz),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=1,  # R1_OrangeStripe
        )
        # Brake-bent top glare hood in R1_WhitePaint
        append_beveled_box(
            bm,
            size_xyz=(0.226, 0.030, 0.112),
            center_xyz=(px, py + 0.105, pz + 0.108),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=8,
            rot_euler=(math.radians(8.0), 0.0, 0.0),
        )
        # Low-profile Top Amber Clearance Strobe Pod
        append_beveled_box(
            bm,
            size_xyz=(0.112, 0.038, 0.112),
            center_xyz=(px, py + 0.128, pz),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber
        )
        # Front Machined Brass Bezel + Glowing R1_LampAmber Sealed-Beam Rectangular Lens
        append_beveled_box(
            bm,
            size_xyz=(0.184, 0.180, 0.028),
            center_xyz=(px, py + 0.008, pz + 0.120),
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=4,  # R1_Brass bezel
        )
        append_beveled_box(
            bm,
            size_xyz=(0.158, 0.154, 0.024),
            center_xyz=(px, py + 0.008, pz + 0.130),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=10,  # R1_LampAmber sealed-beam emitter
        )
        # Rear-facing Amber Pilot Indicator Slit & Copper Heatsink Plug
        append_beveled_box(
            bm,
            size_xyz=(0.134, 0.048, 0.022),
            center_xyz=(px, py + 0.034, pz - 0.120),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=10,  # R1_LampAmber rear pilot glow
        )
        append_cylinder(
            bm,
            radius1=0.036,
            radius2=0.032,
            length=0.040,
            axis="Z",
            center_xyz=(px, py - 0.040, pz - 0.125),
            segments=14,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=5,  # R1_Copper heatsink cap
        )

    return create_mesh_node("Upgrade_C_VarA_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_d_variant_A_r2(rover_root, mats):
    """
    Round 2 Slimmed Upgrade (d) Variant A [OWNER PICK, SLIMMED ~36% VOLUME]:
    'Twin Flank Saddle-Pod Battery Panniers (Slim)'
    - Tucked tightly against 07's mid-hull flanks at x = +/-0.572 m, y = 0.845 m, z = -0.10 m
      (outer face at x = +/-0.686 m, neatly INSIDE the outer tyre sidewalls at +/-0.738 m!)
    - Slimmed chamfered pods (0.228 m W x 0.36 m H x 0.46 m L — 36% volume reduction vs Round 1)
      with GoldFoil MLI backing, coral end-caps, brass cell caps, copper busbars, and 3 Amber charge bars.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    for sx in (-1.0, 1.0):
        px = sx * 0.572
        py = 0.845
        pz = -0.10

        # 1. Slimmed Bolted Kessler-Arai Flank Cradle Frame & GoldFoil MLI Blanket
        append_beveled_box(
            bm,
            size_xyz=(0.12, 0.30, 0.48),
            center_xyz=(sx * 0.462, py, pz),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.436, 0.93, pz - 0.20),
            (sx * 0.436, 0.93, pz + 0.20),
            beads=7,
            radius=0.007,
            mat_idx=3,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.065, 0.31, 0.44),
            center_xyz=(sx * 0.495, py + 0.03, pz),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=7,  # R1_GoldFoil
        )

        # 2. Compact Chamfered Saddle-Pod Battery Housing (0.228 m W x 0.36 m H x 0.46 m L)
        append_beveled_box(
            bm,
            size_xyz=(0.228, 0.36, 0.46),
            center_xyz=(px, py, pz),
            bevel_width=0.022,
            bevel_segments=2,
            mat_idx=0,  # R1_CreamEnamel
        )
        for sz in (-1.0, 1.0):
            append_beveled_box(
                bm,
                size_xyz=(0.234, 0.366, 0.065),
                center_xyz=(px, py, pz + sz * 0.184),
                bevel_width=0.010,
                bevel_segments=2,
                mat_idx=1,  # R1_OrangeStripe
            )

        # Twin Grey-Metal Straps + Brass Buckles
        for sz in (-0.10, 0.10):
            append_beveled_box(
                bm,
                size_xyz=(0.242, 0.374, 0.034),
                center_xyz=(px, py, pz + sz),
                bevel_width=0.005,
                bevel_segments=1,
                mat_idx=2,
            )
            append_hex_bolt(
                bm,
                radius=0.012,
                height=0.018,
                axis="X",
                center_xyz=(px + sx * 0.121, py + 0.080, pz + sz),
                mat_idx=4,  # R1_Brass
            )

        # Top Twin Brass Cell Caps & Longitudinal Copper Busbar rising to y = 1.09 m
        for sz in (-0.125, 0.125):
            append_cylinder(
                bm,
                radius1=0.064,
                radius2=0.058,
                length=0.062,
                axis="Y",
                center_xyz=(px, py + 0.200, pz + sz),
                segments=18,
                bevel_width=0.005,
                bevel_segments=2,
                mat_idx=4,  # R1_Brass cell cap
            )
        append_beveled_box(
            bm,
            size_xyz=(0.042, 0.026, 0.355),
            center_xyz=(px, py + 0.234, pz),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=5,  # R1_Copper top busbar
        )

        # 3. Glowing Amber Charge-Level Sight Bars (3 vertical on outer flank + 3 rear-facing)
        for bz in (-0.10, 0.0, 0.10):
            append_beveled_box(
                bm,
                size_xyz=(0.028, 0.20, 0.040),
                center_xyz=(px + sx * 0.111, py - 0.008, pz + bz),
                bevel_width=0.004,
                bevel_segments=1,
                mat_idx=10,  # R1_LampAmber
            )
        for ry_off in (-0.085, 0.0, 0.085):
            append_beveled_box(
                bm,
                size_xyz=(0.15, 0.045, 0.028),
                center_xyz=(px, py + ry_off, pz - 0.232),
                bevel_width=0.004,
                bevel_segments=1,
                mat_idx=10,  # R1_LampAmber
            )

    _add_07_adapter_tag(bm, (-0.688, 0.845, -0.10), normal_axis="X", sign_dir=-1.0)
    return create_mesh_node("Upgrade_D_VarA_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def _local_to_world(center_xyz, local_xyz, rot_euler):
    """Transform a local (x, y, z) offset by Euler(rot_euler, 'XYZ') and add to center_xyz."""
    from mathutils import Euler, Vector
    v = Euler(rot_euler, "XYZ").to_matrix() @ Vector(local_xyz)
    return (center_xyz[0] + v.x, center_xyz[1] + v.y, center_xyz[2] + v.z)


def build_upgrade_e_variant_A_r2(rover_root, mats):
    """
    Round 2 REDESIGNED Upgrade (e) Variant A [RECOMMENDED]:
    'Fold-Down Front-Bumper Hex Contact Shoe with Teal Hex-Ring Collar & Side Teal Status Lamp'
    - Purpose-first at 07's CHARGING CONTACT (front lower bumper & belly contact shoes — 100% CLEAN REAR!).
    - Fold-down hexagonal Tycho contact shoe under the front lower bumper (z = +0.845 m, y = 0.255 m, pitched at -30 deg
      using axis="Z" so its hexagonal face, #3E7A82 R1_TealMark hex-ring collar, R1_WhitePaint body, 6 copper contact pads,
      and central glowing R1_TealLamp core face forward/downward toward the charging pad with zero scoop occlusion!).
    - Side-Visible Teal Hex-Ring Bumper Collar Cheeks + small glowing Teal Status Lamps (R1_TealLamp) on the left/right
      ends of the front lower bumper (x = +/-0.548 m, y = 0.425 m, z = +0.69 m) and lower mid-flank contact rail
      (x = +/-0.555 m, y = 0.465 m, z = +0.02 m) so it reads clearly from the side, front 3/4, and 7 m chase cam.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    # 1. Front Lower-Bumper Clamping Collar & Fold-Down Spring Hinge Assembly (y = 0.28..0.44 m, z = +0.70..+0.84 m)
    append_beveled_box(
        bm,
        size_xyz=(1.04, 0.058, 0.110),
        center_xyz=(0.0, 0.415, 0.735),
        bevel_width=0.006,
        bevel_segments=2,
        mat_idx=2,  # R1_GreyMetal under-bumper clamp bar
    )
    # Teal Hex-Collar Trim Strip along the Front Lower Bumper Lip
    append_beveled_box(
        bm,
        size_xyz=(0.96, 0.036, 0.042),
        center_xyz=(0.0, 0.382, 0.785),
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=9,  # R1_TealMark (#3E7A82)
    )
    _add_07_adapter_tag(bm, (0.28, 0.425, 0.795), normal_axis="Z", sign_dir=1.0)
    for sx in (-1.0, 1.0):
        # Welded bumper hanger brackets & spring-loaded fold-down struts under the front bumper lip
        append_beveled_box(
            bm,
            size_xyz=(0.078, 0.092, 0.120),
            center_xyz=(sx * 0.21, 0.375, 0.775),
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=2,
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.21 - 0.032, 0.415, 0.79),
            (sx * 0.21 + 0.032, 0.415, 0.79),
            beads=5,
            radius=0.007,
            mat_idx=3,
        )
        append_cylinder(
            bm,
            radius1=0.025,
            radius2=0.025,
            length=0.095,
            axis="X",
            center_xyz=(sx * 0.21, 0.338, 0.805),
            segments=14,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass hinge barrel
        )
        # Fold-down strut arm angling down to front hex contact shoe
        append_beveled_box(
            bm,
            size_xyz=(0.040, 0.130, 0.042),
            center_xyz=(sx * 0.155, 0.295, 0.825),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=3,  # R1_BareSteel
            rot_euler=(math.radians(-24.0), 0.0, -sx * math.radians(14.0)),
        )
        append_helical_coil(
            bm,
            coil_radius=0.028,
            wire_radius=0.006,
            length=0.090,
            turns=4.5,
            axis="Y",
            center_xyz=(sx * 0.155, 0.300, 0.825),
            segs_per_turn=12,
            wire_segs=6,
            mat_idx=5,  # R1_Copper contact-pressure spring
        )

    # 2. Primary Fold-Down Hex Contact Shoe under Front Lower Bumper (x = 0.0, y = 0.252 m, z = +0.848 m)
    # Using axis="Z" with rot_euler=(-28 deg, 0, 0) so local +Z points forward & slightly upward toward the camera,
    # keeping all concentric hex layers and 6 copper contacts 100% locked onto the front face!
    f_shoe_pos = (0.0, 0.252, 0.848)
    f_shoe_rot = (math.radians(-28.0), 0.0, 0.0)
    # Outer Teal Hex-Ring Collar (R1_TealMark #3E7A82)
    _append_regular_hex_prism(
        bm,
        radius=0.205,
        depth=0.052,
        axis="Z",
        center_xyz=_local_to_world(f_shoe_pos, (0.0, 0.0, 0.0), f_shoe_rot),
        bevel_width=0.008,
        mat_idx=9,  # R1_TealMark
        rot_euler=f_shoe_rot,
    )
    # White Composite Hex Plug Body (R1_WhitePaint)
    _append_regular_hex_prism(
        bm,
        radius=0.168,
        depth=0.056,
        axis="Z",
        center_xyz=_local_to_world(f_shoe_pos, (0.0, 0.0, 0.012), f_shoe_rot),
        bevel_width=0.007,
        mat_idx=8,  # R1_WhitePaint
        rot_euler=f_shoe_rot,
    )
    # Inner Teal Hex Bezel Ring
    _append_regular_hex_prism(
        bm,
        radius=0.130,
        depth=0.058,
        axis="Z",
        center_xyz=_local_to_world(f_shoe_pos, (0.0, 0.0, 0.022), f_shoe_rot),
        bevel_width=0.005,
        mat_idx=9,  # R1_TealMark
        rot_euler=f_shoe_rot,
    )
    # 6 Machined Copper Contact Shoes + Central Glowing Teal Handshake Core (strictly in local XY plane at lz = +0.032 m)
    for i in range(6):
        ang = i * math.pi / 3.0
        lx_p = 0.084 * math.cos(ang)
        ly_p = 0.084 * math.sin(ang)
        _append_regular_hex_prism(
            bm,
            radius=0.026,
            depth=0.060,
            axis="Z",
            center_xyz=_local_to_world(f_shoe_pos, (lx_p, ly_p, 0.032), f_shoe_rot),
            bevel_width=0.003,
            mat_idx=5,  # R1_Copper
            rot_euler=f_shoe_rot,
        )
    _append_regular_hex_prism(
        bm,
        radius=0.040,
        depth=0.064,
        axis="Z",
        center_xyz=_local_to_world(f_shoe_pos, (0.0, 0.0, 0.035), f_shoe_rot),
        bevel_width=0.004,
        mat_idx=12,  # R1_TealLamp glowing center plug core
        rot_euler=f_shoe_rot,
    )

    # Secondary Horizontal Belly Contact Shoe Pad tucked right behind the fold-down shoe (y = 0.195 m, z = +0.72 m)
    _append_regular_hex_prism(
        bm,
        radius=0.175,
        depth=0.036,
        axis="Y",
        center_xyz=(0.0, 0.195, 0.72),
        bevel_width=0.006,
        mat_idx=9,  # R1_TealMark
    )
    _append_regular_hex_prism(
        bm,
        radius=0.130,
        depth=0.044,
        axis="Y",
        center_xyz=(0.0, 0.192, 0.72),
        bevel_width=0.004,
        mat_idx=5,  # R1_Copper bottom contact shoe
    )

    # 3. Side-Visible Teal Hex-Ring Bumper Collar Cheeks & Small Glowing Teal Status Lamps (x = +/-0.548 m)
    # Mounted at the front lower bumper corners (z = +0.69 m) and lower mid-flank contact rail (z = +0.02 m)
    # NOTE: Zero parts on or near the rear bumper — 07's rear is 100% clean!
    for sx in (-1.0, 1.0):
        # Front lower-bumper corner Teal Hex Collar + Small Glowing Teal Status Lamp
        bx = sx * 0.548
        by = 0.425
        bz = 0.69
        _append_regular_hex_prism(
            bm,
            radius=0.106,
            depth=0.088,
            axis="X",
            center_xyz=(bx, by, bz),
            bevel_width=0.007,
            mat_idx=9,  # R1_TealMark hex collar
        )
        _append_regular_hex_prism(
            bm,
            radius=0.080,
            depth=0.100,
            axis="X",
            center_xyz=(bx + sx * 0.008, by, bz),
            bevel_width=0.005,
            mat_idx=8,  # R1_WhitePaint
        )
        append_cylinder(
            bm,
            radius1=0.054,
            radius2=0.050,
            length=0.108,
            axis="X",
            center_xyz=(bx + sx * 0.012, by, bz),
            segments=16,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,  # R1_Brass lamp bezel
        )
        append_cylinder(
            bm,
            radius1=0.040,
            radius2=0.036,
            length=0.118,
            axis="X",
            center_xyz=(bx + sx * 0.018, by, bz),
            segments=16,
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=12,  # R1_TealLamp bright glowing side status lamp!
        )

        # Lower mid-flank belly contact shoe & side status lamp in the wheel gap (z = +0.02 m, y = 0.465 m)
        rx = sx * 0.558
        ry = 0.465
        rz = 0.02
        _append_regular_hex_prism(
            bm,
            radius=0.112,
            depth=0.088,
            axis="X",
            center_xyz=(rx, ry, rz),
            bevel_width=0.006,
            mat_idx=9,  # R1_TealMark
        )
        _append_regular_hex_prism(
            bm,
            radius=0.082,
            depth=0.100,
            axis="X",
            center_xyz=(rx + sx * 0.008, ry, rz),
            bevel_width=0.005,
            mat_idx=5,  # R1_Copper
        )
        append_cylinder(
            bm,
            radius1=0.042,
            radius2=0.038,
            length=0.114,
            axis="X",
            center_xyz=(rx + sx * 0.014, ry, rz),
            segments=16,
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=12,  # R1_TealLamp side status lamp
        )
        # Tidy lower-rocker copper/rubber umbilical linking front bumper collar to mid-flank contact rail
        append_beveled_box(
            bm,
            size_xyz=(0.038, 0.038, 0.66),
            center_xyz=(sx * 0.512, 0.442, 0.35),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=6,  # R1_Rubber armored conduit
        )

    return create_mesh_node("Upgrade_E_VarA_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)


def build_upgrade_e_variant_B_r2(rover_root, mats):
    """
    Round 2 REDESIGNED Upgrade (e) Variant B:
    'Compact Lower-Flank Clip-On Hex Adapter Puck (Teal Ring + Copper Contacts)'
    - Purpose-first: clips a compact, tidy hexagonal adapter puck (Ø 0.33 m) directly over 07's existing
      charge port on the lower hull flank (x = +/-0.558 m, y = 0.475 m, z = +0.02 m, in the open window
      between front & rear tyres — 100% CLEAN REAR!).
    - Features a spring-loaded belly-lip C-clamp, bold R1_TealMark (#3E7A82) hex ring, R1_WhitePaint body,
      6 machined R1_Copper contact pins, glowing R1_TealLamp center status LED, and flat under-belly cross-bus.
    """
    bm = bmesh.new()
    mlist = _standard_mat_list(mats)

    for sx in (-1.0, 1.0):
        puck_pos = (sx * 0.558, 0.475, 0.02)
        puck_rot = (0.0, sx * math.radians(14.0), -sx * math.radians(14.0))

        # 1. Spring-Clip Lower-Flank Port Bracket hooking under the belly pan lip
        append_beveled_box(
            bm,
            size_xyz=(0.14, 0.24, 0.24),
            center_xyz=(sx * 0.475, 0.465, 0.02),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=2,  # R1_GreyMetal clip body
        )
        _add_weld_bead_strip(
            bm,
            (sx * 0.445, 0.575, -0.07),
            (sx * 0.445, 0.575, 0.11),
            beads=5,
            radius=0.007,
            mat_idx=3,
        )
        for dz in (-0.085, 0.085):
            append_hex_bolt(
                bm,
                radius=0.011,
                height=0.018,
                axis="X",
                center_xyz=(sx * 0.545, 0.560, 0.02 + dz),
                mat_idx=4,  # R1_Brass cam-lock bolts
            )

        # 2. Compact Clip-On Hexagonal Adapter Puck ("The Plug", Ø 0.33 m across corners)
        # All concentric layers transformed along local X via _local_to_world so pins and rings stay 100% aligned!
        # Outer Teal Hex Ring Collar (R1_TealMark #3E7A82)
        _append_regular_hex_prism(
            bm,
            radius=0.172,
            depth=0.076,
            axis="X",
            center_xyz=_local_to_world(puck_pos, (0.0, 0.0, 0.0), puck_rot),
            bevel_width=0.008,
            mat_idx=9,  # R1_TealMark
            rot_euler=puck_rot,
        )
        # White Composite Hex Plug Body (R1_WhitePaint)
        _append_regular_hex_prism(
            bm,
            radius=0.142,
            depth=0.082,
            axis="X",
            center_xyz=_local_to_world(puck_pos, (sx * 0.012, 0.0, 0.0), puck_rot),
            bevel_width=0.006,
            mat_idx=8,  # R1_WhitePaint
            rot_euler=puck_rot,
        )
        # Inner Teal Hex Ring Bezel
        _append_regular_hex_prism(
            bm,
            radius=0.112,
            depth=0.086,
            axis="X",
            center_xyz=_local_to_world(puck_pos, (sx * 0.022, 0.0, 0.0), puck_rot),
            bevel_width=0.005,
            mat_idx=9,  # R1_TealMark
            rot_euler=puck_rot,
        )
        # Recessed Copper Hex Contact Plate + 6 Protruding Copper Contact Pins
        _append_regular_hex_prism(
            bm,
            radius=0.084,
            depth=0.090,
            axis="X",
            center_xyz=_local_to_world(puck_pos, (sx * 0.030, 0.0, 0.0), puck_rot),
            bevel_width=0.004,
            mat_idx=5,  # R1_Copper
            rot_euler=puck_rot,
        )
        for i in range(6):
            ang = i * math.pi / 3.0
            ly_p = 0.056 * math.cos(ang)
            lz_p = 0.056 * math.sin(ang)
            append_cylinder(
                bm,
                radius1=0.018,
                radius2=0.016,
                length=0.098,
                axis="X",
                center_xyz=_local_to_world(puck_pos, (sx * 0.038, ly_p, lz_p), puck_rot),
                segments=12,
                bevel_width=0.002,
                bevel_segments=1,
                mat_idx=5,  # R1_Copper contact pin
                rot_euler=puck_rot,
            )
        # Central Glowing Teal Status Lamp on Puck Face
        append_cylinder(
            bm,
            radius1=0.028,
            radius2=0.026,
            length=0.104,
            axis="X",
            center_xyz=_local_to_world(puck_pos, (sx * 0.042, 0.0, 0.0), puck_rot),
            segments=14,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=12,  # R1_TealLamp
            rot_euler=puck_rot,
        )

        # 3. Coiled Copper/Rubber Plug Umbilical routing forward along lower rocker to a small front-corner teal repeater
        append_helical_coil(
            bm,
            coil_radius=0.028,
            wire_radius=0.007,
            length=0.22,
            turns=5.5,
            axis="Z",
            center_xyz=(sx * 0.508, 0.435, 0.24),
            segs_per_turn=12,
            wire_segs=6,
            mat_idx=5,  # R1_Copper
        )

    # 4. Flat Under-Belly Cross-Bus linking left/right flank pucks (y = 0.225..0.26 m, z = +0.02 m — invisible from rear!)
    append_beveled_box(
        bm,
        size_xyz=(0.96, 0.042, 0.14),
        center_xyz=(0.0, 0.245, 0.02),
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=2,  # R1_GreyMetal belly bus clamp
    )
    _append_regular_hex_prism(
        bm,
        radius=0.185,
        depth=0.036,
        axis="Y",
        center_xyz=(0.0, 0.205, 0.02),
        bevel_width=0.006,
        mat_idx=9,  # R1_TealMark flat belly contact pad
    )
    _add_07_adapter_tag(bm, (-0.495, 0.555, 0.18), normal_axis="X", sign_dir=-1.0)
    return create_mesh_node("Upgrade_E_VarB_R2", bm, (0.0, 0.0, 0.0), parent=rover_root, mat_list=mlist, cfg=DIMENSIONS)




# ==============================================================================
# SCENE SETUP, VISIBILITY TOGGLES & RENDERING PIPELINE
# ==============================================================================

def _set_obj_hidden(name: str, hidden: bool):
    obj = bpy.data.objects.get(name)
    if obj is None:
        return
    obj.hide_render = hidden
    obj.hide_viewport = hidden
    for ch in obj.children_recursive:
        ch.hide_render = hidden
        ch.hide_viewport = hidden


def _configure_baseline_rover_state(mode: str):
    """
    Configure built-in RoverModel nodes for a given comparison state:
      - Always hide late-game gifts Pennant and Decal07Fresh so 07 matches R1-R11 opening state.
      - For 'before_a': show today's thin +0.35m Antenna_Segment2 rod so the owner sees why it reads as nothing.
      - For 'var_a_*': hide built-in thin Antenna_Segment2 / AntennaTip when replaced by Upgrade (a) variants.
      - For 'before_b': show today's tiny 4cm Cutter_Igniter cartridge.
      - For 'var_b_*': hide built-in tiny Cutter_Igniter when replaced by Upgrade (b) variants.
    """
    seg2 = bpy.data.objects.get("Antenna_Segment2")
    tip = bpy.data.objects.get("AntennaTip")
    ign = bpy.data.objects.get("Cutter_Igniter")

    # Reset default positions
    _set_obj_hidden("Antenna", False)
    if seg2 is not None:
        seg2.location.z = DIMENSIONS["piv_antenna_segment2_local"][1]
        _set_obj_hidden("Antenna_Segment2", False)
    if tip is not None:
        tip.location.z = DIMENSIONS["piv_antenna_tip_local"][1]
        _set_obj_hidden("AntennaTip", False)
    if ign is not None:
        _set_obj_hidden("Cutter_Igniter", False)

    if mode == "before_a":
        # Today's +0.35 m thin rod extension
        if seg2 is not None:
            seg2.location.z = DIMENSIONS["piv_antenna_segment2_local"][1] + 0.35
        if tip is not None:
            tip.location.z = DIMENSIONS["piv_antenna_tip_local"][1] + 0.35
    elif mode in ("a_A", "a_B", "a_A_r2", "full_growth", "full_growth_r2"):
        _set_obj_hidden("Antenna", True)
        _set_obj_hidden("Antenna_Segment2", True)
        _set_obj_hidden("AntennaTip", True)

    if mode in ("b_A", "b_B", "b_A_r2", "full_growth", "full_growth_r2"):
        _set_obj_hidden("Cutter_Igniter", True)

    # IMPORTANT: Hide Pennant & Decal07Fresh AFTER resetting Antenna so recursive unhide never exposes Pennant!
    _set_obj_hidden("Pennant", True)
    _set_obj_hidden("Decal07Fresh", True)

    bpy.context.view_layer.update()


# ==============================================================================
# PILLOW CONTACT SHEET & SILHOUETTE METRICS COMPOSITOR
# ==============================================================================

UPGRADE_SPEC_META = [
    {
        "id": "a",
        "code": "(a)",
        "title": "ANTENNA EXTENDED",
        "region": "R3 — Elder 03's Gift (Telemetry Range)",
        "maker": "1974 Kessler–Arai / Lumen Series-7",
        "accent_name": "R1_LampAmber (#FFB547) + R1_OrangeStripe (#FF8A5B)",
        "accent_hex": "#FFB547",
        "accent2_hex": "#FF8A5B",
        "zone": "Zone 1: Rear Dorsal Shoulders & Roofline (y = 0.88–2.16 m)",
        "problem": "Before: Antenna_Segment2 slides +0.35 m on a Ø 1.6 cm rod (+0.5% silhouette; ~2 px wide at 7 m).",
        "varA_title": "Variant A ★ [RECOMMENDED]: Dish-Crown Mast & Symmetric Telemetry Bridge",
        "varA_desc": "Bolts a cream/coral telemetry bridge across both rear shoulder sockets + 2.16 m twin-strut mast + Ø 0.34 m parabolic dish + twin side transceiver pods.",
        "varB_title": "Variant B: Unfolding Dorsal Scissor-Truss Mast & Wide T-Array",
        "varB_desc": "Bolts a cross-braced pantograph scissor truss rising to 2.14 m + 0.90 m wide T-bar dipole array with dual glowing amber tip beacons.",
    },
    {
        "id": "b",
        "code": "(b)",
        "title": "IGNITER FITTED",
        "region": "R4 — Elder 04's Gift (Thermal Cutter Ignition)",
        "maker": "1974 Kessler–Arai / Lumen Heavy Mining",
        "accent_name": "R1_LampAmber (#FFB547) Plasma Coil + R1_OrangeStripe (#FF8A5B)",
        "accent_hex": "#FFB547",
        "accent2_hex": "#FF8A5B",
        "zone": "Zone 2: Front Bonnet Shoulders & Cutter Turret (x = ±0.41 m, z = +0.45..+0.88 m)",
        "problem": "Before: Cutter_Igniter is a 4 cm × 9 cm side cartridge occluded by 07's body from the rear chase cam.",
        "varA_title": "Variant A ★ [RECOMMENDED]: Twin-Shoulder Plasma Ignition Saddle & Coil Shroud",
        "varA_desc": "Wraps a bolted horseshoe yoke over the front bonnet with twin outboard Amber plasma-coil canisters (breaking past the head outline from behind) + copper-finned turret shroud.",
        "varB_title": "Variant B: Over-Bonnet Arc-Exciter Box & Outboard Copper Radiator Wings",
        "varB_desc": "Bolts a 0.58 m wide Arc-Exciter generator box atop the front nose + cantilevered 5-plate copper vacuum radiator wings at x = ±0.47 m + helical copper nozzle coil.",
    },
    {
        "id": "c",
        "code": "(c)",
        "title": "SSR-3 HEADLAMP",
        "region": "R10–11 — Salvaged Elder 06 Sealed-Beam Lamp",
        "maker": "1978 SSR-3 Deep-Crater Survey (Era B Donor)",
        "accent_name": "R1_WhitePaint (#ECE8DF) + Thin R1_OrangeStripe + R1_LampAmber Lens",
        "accent_hex": "#ECE8DF",
        "accent2_hex": "#FFB547",
        "zone": "Zone 3: CRT Head Visor Flanks / Crown (x = ±0.495 m, y = 1.35–1.80 m)",
        "problem": "Before: No dedicated high-power crater floodlight; small nose/face details vanish at 7 m.",
        "varA_title": "Variant A ★ [RECOMMENDED]: Twin Salvaged SSR-3 Box Floodlights on Visor Ear-Brackets",
        "varA_desc": "Welds symmetric 1978 white-enamel brake-bent floodlight pods onto outrigger gussets beside 07's CRT visor (widening the head +53% to 1.18 m) with rear amber pilot slits.",
        "varB_title": "Variant B: Over-Visor Salvaged SSR-3 Searchlight Rack & Twin Spot Bar",
        "varB_desc": "Welds a tubular roll-cage arch over the CRT crown carrying Elder 06's central 0.40 m white-enamel searchlight box + twin auxiliary white spot pods (+0.29 m height).",
    },
    {
        "id": "d",
        "code": "(d)",
        "title": "CELL PACK (PEAK KEY)",
        "region": "R11 — Elder 06's Salvaged Battery Cells",
        "maker": "1974 Kessler–Arai / Salvaged High-Capacity Cells",
        "accent_name": "R1_GoldFoil MLI + R1_OrangeStripe Caps + 3× R1_LampAmber Charge Bars",
        "accent_hex": "#FFB547",
        "accent2_hex": "#D8A24A",
        "zone": "Zone 4: Mid-Hull Flank Sockets (x = ±0.56 m, y = 0.60–1.00 m, z = -0.10 m)",
        "problem": "Before: No visible exterior battery expansion; mid-hull flanks between wheels are narrow.",
        "varA_title": "Variant A ★ [RECOMMENDED]: Twin Flank Saddle-Pod Battery Panniers",
        "varA_desc": "Bolts chunky chamfered cream/coral battery panniers with GoldFoil MLI blankets, brass cell caps, and 3 glowing Amber charge bars onto both flanks (hull width +54% to 1.36 m).",
        "varB_title": "Variant B: High-Output Dorsal Power Backpack & Space Radiator Stack",
        "varB_desc": "Mounts a 0.82 m wide dorsal battery backpack over the rear deck with 4 upright brass cell crowns, outboard copper radiator fins, and a 4-column rear Amber charge matrix.",
    },
    {
        "id": "e",
        "code": "(e)",
        "title": "HEX CHARGE ADAPTER",
        "region": "R10 — Tycho Consortium Hex Contact Plate (Tycho Key)",
        "maker": "2004 Tycho Consortium (Era D Donor Hardware)",
        "accent_name": "R1_WhitePaint (#ECE8DF) + R1_TealMark (#3E7A82) Hex Ring + R1_Copper",
        "accent_hex": "#3E7A82",
        "accent2_hex": "#ECE8DF",
        "zone": "Zone 5: Rear Valence / Lower Bumper Sockets (y = 0.44–0.85 m, z = -0.68..-1.08 m)",
        "problem": "Before: No visible Tycho interface; 07's 1974 round sockets cannot mate with 2004 Tycho hex pads.",
        "varA_title": "Variant A ★ [RECOMMENDED]: Rear Pantograph Arm & Canted Tycho Hex Collector Shield",
        "varA_desc": "Bolts a folding pantograph arm + canted Ø 0.54 m White/Teal hexagonal collector plate (with 6 copper contact shoes & amber lock core) + twin rear-corner hex converter pods.",
        "varB_title": "Variant B: Twin Side-Outrigger Articulated Hex Collector Skids",
        "varB_desc": "Bolts articulated worm-gear swing arms onto lower left/right flank sockets carrying canted Ø 0.44 m White/Teal/Copper hexagonal collector shields (+0.28 m per side).",
    },
]


def compose_contact_sheets(repo_root: Path):
    """
    Use Pillow + NumPy to compute exact silhouette area changes and compose:
      1) Logs/captures/upgrades_sheet.png (Master 5-Upgrade x 2-Variant + Full Growth Contact Sheet)
      2) Logs/captures/full_growth.png    (Dedicated High-Res Full Growth Comparison Sheet)
      3) Logs/captures/upgrade_{a..e}.png (Per-upgrade inspection boards)
      4) Logs/captures/silhouette_metrics.json
    """
    from PIL import Image, ImageDraw, ImageFont

    captures_dir = repo_root / "Logs" / "captures"
    raw_dir = captures_dir / "_raw"
    font_dir = repo_root / "Assets" / "_Project" / "UI" / "Fonts"

    def load_font(name: str, size: int):
        fp = font_dir / name
        if fp.exists():
            return ImageFont.truetype(str(fp), size)
        return ImageFont.load_default()

    f_h1 = load_font("BeVietnamPro-SemiBold.ttf", 30)
    f_h2 = load_font("BeVietnamPro-SemiBold.ttf", 21)
    f_h3 = load_font("BeVietnamPro-SemiBold.ttf", 17)
    f_body = load_font("BeVietnamPro-Regular.ttf", 15)
    f_small = load_font("BeVietnamPro-Regular.ttf", 13)
    f_mono = load_font("IBMPlexMono-Bold.ttf", 15)
    f_mono_lg = load_font("IBMPlexMono-Bold.ttf", 18)
    f_mono_sm = load_font("IBMPlexMono-Bold.ttf", 13)

    def wrap_text(draw, text: str, font, max_w: int) -> list[str]:
        words = text.split()
        lines = []
        cur = ""
        for w in words:
            test = f"{cur} {w}".strip()
            bbox = draw.textbbox((0, 0), test, font=font)
            if bbox[2] - bbox[0] <= max_w or not cur:
                cur = test
            else:
                lines.append(cur)
                cur = w
        if cur:
            lines.append(cur)
        return lines

    def load_rgba(name: str) -> Image.Image:
        return Image.open(raw_dir / name).convert("RGBA")

    def alpha_mask(img: Image.Image) -> np.ndarray:
        arr = np.array(img)
        return arr[:, :, 3] > 32

    # Compute union bounding box across all *_chase.png images so every chase view uses the EXACT same crop & scale!
    chase_files = [f"{u}_before_chase.png" for u in ("a", "b", "c", "d", "e")] + [
        f"{u}_{v}_chase.png" for u in ("a", "b", "c", "d", "e") for v in ("A", "B")
    ] + ["full_before_chase.png", "full_after_chase.png"]

    ymin_all, ymax_all, xmin_all, xmax_all = 9999, 0, 9999, 0
    for cf in chase_files:
        m = alpha_mask(load_rgba(cf))
        ys, xs = np.where(m)
        if len(ys) > 0:
            ymin_all = min(ymin_all, int(ys.min()))
            ymax_all = max(ymax_all, int(ys.max()))
            xmin_all = min(xmin_all, int(xs.min()))
            xmax_all = max(xmax_all, int(xs.max()))

    # Pad the union chase bounding box slightly and make it square-ish for clean framing
    pad = 42
    cy_mid = (ymin_all + ymax_all) // 2
    cx_mid = (xmin_all + xmax_all) // 2
    half_span = max((ymax_all - ymin_all) // 2, (xmax_all - xmin_all) // 2) + pad
    c_x0 = max(0, cx_mid - half_span)
    c_y0 = max(0, cy_mid - half_span)
    c_x1 = min(960, cx_mid + half_span)
    c_y1 = min(960, cy_mid + half_span)
    chase_crop_box = (c_x0, c_y0, c_x1, c_y1)

    def render_on_studio_bg(rgba_img: Image.Image, crop_box: tuple[int, int, int, int] | None, target_size: tuple[int, int], draw_grid: bool = False) -> Image.Image:
        if crop_box is not None:
            rgba_img = rgba_img.crop(crop_box)
        w, h = target_size
        resized = rgba_img.resize((w, h), Image.Resampling.LANCZOS)

        # Warm dark lunar slate radial/vertical studio backdrop so cream, white, teal, and amber pop
        y_grad = np.linspace(0.0, 1.0, h)[:, None]
        x_grad = np.linspace(-1.0, 1.0, w)[None, :]
        rad = np.clip(1.0 - 0.35 * (x_grad**2 + (y_grad - 0.55) ** 2), 0.65, 1.0)
        bg = np.zeros((h, w, 4), dtype=np.uint8)
        bg[:, :, 0] = np.clip((34 + 14 * (1.0 - y_grad)) * rad, 20, 65).astype(np.uint8)
        bg[:, :, 1] = np.clip((37 + 15 * (1.0 - y_grad)) * rad, 22, 70).astype(np.uint8)
        bg[:, :, 2] = np.clip((45 + 18 * (1.0 - y_grad)) * rad, 28, 82).astype(np.uint8)
        bg[:, :, 3] = 255
        bg_img = Image.fromarray(bg, "RGBA")

        if draw_grid:
            gd = ImageDraw.Draw(bg_img)
            for gx in range(0, w, w // 6):
                gd.line([(gx, 0), (gx, h)], fill=(58, 63, 74, 140), width=1)
            for gy in range(0, h, h // 6):
                gd.line([(0, gy), (w, gy)], fill=(58, 63, 74, 140), width=1)

        bg_img.alpha_composite(resized)
        return bg_img.convert("RGB")

    def make_silhouette_diff_inset(
        base_rgba: Image.Image,
        var_rgba: Image.Image,
        crop_box: tuple[int, int, int, int],
        target_size: tuple[int, int],
        add_rgb: tuple[int, int, int] = (255, 181, 71),
    ) -> Image.Image:
        """Create a crisp silhouette comparison inset: base 07 in cool slate, added upgrade pixels in bright accent."""
        b_crop = base_rgba.crop(crop_box).resize(target_size, Image.Resampling.NEAREST)
        v_crop = var_rgba.crop(crop_box).resize(target_size, Image.Resampling.NEAREST)
        bm = alpha_mask(b_crop)
        vm = alpha_mask(v_crop)
        added = vm & (~bm)
        common = vm & bm

        w, h = target_size
        out = np.zeros((h, w, 3), dtype=np.uint8)
        out[:, :] = (20, 22, 28)
        out[common] = (108, 116, 132)
        out[added] = add_rgb
        img = Image.fromarray(out, "RGB")
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, w - 1, h - 1], outline=(88, 96, 112), width=2)
        return img

    # Measure exact silhouette metrics for all 5 upgrades x 2 variants + full growth
    base_clean_chase = load_rgba("full_before_chase.png")
    base_clean_px = int(alpha_mask(base_clean_chase).sum())

    metrics: dict[str, dict] = {"base_chase_px": base_clean_px, "upgrades": {}}

    for spec in UPGRADE_SPEC_META:
        uid = spec["id"]
        bef_chase = load_rgba(f"{uid}_before_chase.png")
        bef_close = load_rgba(f"{uid}_before_closeup.png")
        bef_chase_px = int(alpha_mask(bef_chase).sum())
        bef_close_px = int(alpha_mask(bef_close).sum())

        metrics["upgrades"][uid] = {
            "before_chase_px": bef_chase_px,
            "before_closeup_px": bef_close_px,
        }
        for v in ("A", "B"):
            vc = load_rgba(f"{uid}_{v}_chase.png")
            vcl = load_rgba(f"{uid}_{v}_closeup.png")
            vc_m = alpha_mask(vc)
            bm_m = alpha_mask(base_clean_chase)
            vcl_m = alpha_mask(vcl)
            bcl_m = alpha_mask(bef_close)

            # Added silhouette pixels outside base 07 outline + net total area delta
            added_chase_px = int((vc_m & (~bm_m)).sum())
            net_chase_pct = (int(vc_m.sum()) - base_clean_px) / float(base_clean_px) * 100.0
            new_contour_chase_pct = added_chase_px / float(base_clean_px) * 100.0

            added_close_px = int((vcl_m & (~bcl_m)).sum())
            net_close_pct = (int(vcl_m.sum()) - bef_close_px) / float(bef_close_px) * 100.0
            new_contour_close_pct = added_close_px / float(bef_close_px) * 100.0

            metrics["upgrades"][uid][v] = {
                "chase_px": int(vc_m.sum()),
                "net_chase_pct": round(net_chase_pct, 1),
                "new_contour_chase_pct": round(new_contour_chase_pct, 1),
                "closeup_px": int(vcl_m.sum()),
                "net_closeup_pct": round(net_close_pct, 1),
                "new_contour_closeup_pct": round(new_contour_close_pct, 1),
            }

    # Full growth metrics
    fg_chase = load_rgba("full_after_chase.png")
    fg_f3q = load_rgba("full_after_front3q.png")
    fg_r3q = load_rgba("full_after_rear3q.png")
    fb_f3q = load_rgba("full_before_front3q.png")
    fb_r3q = load_rgba("full_before_rear3q.png")

    fg_chase_m = alpha_mask(fg_chase)
    fb_chase_m = alpha_mask(base_clean_chase)
    metrics["full_growth"] = {
        "net_chase_pct": round((int(fg_chase_m.sum()) - base_clean_px) / float(base_clean_px) * 100.0, 1),
        "new_contour_chase_pct": round(int((fg_chase_m & (~fb_chase_m)).sum()) / float(base_clean_px) * 100.0, 1),
        "net_front3q_pct": round(
            (int(alpha_mask(fg_f3q).sum()) - int(alpha_mask(fb_f3q).sum()))
            / float(alpha_mask(fb_f3q).sum())
            * 100.0,
            1,
        ),
        "net_rear3q_pct": round(
            (int(alpha_mask(fg_r3q).sum()) - int(alpha_mask(fb_r3q).sum()))
            / float(alpha_mask(fb_r3q).sum())
            * 100.0,
            1,
        ),
    }

    (captures_dir / "silhouette_metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    print("Saved silhouette metrics:", json.dumps(metrics, indent=2))

    # ==========================================================================
    # BUILD MASTER CONTACT SHEET: Logs/captures/upgrades_sheet.png
    # ==========================================================================
    sheet_w = 2640
    header_h = 152
    row_h = 468
    fg_row_h = 540
    sheet_h = header_h + 5 * row_h + fg_row_h + 24

    master = Image.new("RGB", (sheet_w, sheet_h), (18, 20, 25))
    mdraw = ImageDraw.Draw(master)

    # Top Title Banner
    mdraw.rectangle([0, 0, sheet_w, header_h - 8], fill=(26, 29, 36), outline=(66, 72, 86), width=2)
    mdraw.rectangle([0, 0, 12, header_h - 8], fill=(255, 181, 71))
    mdraw.text(
        (28, 16),
        "07 — UPGRADE VISUAL LANGUAGE CONCEPT SHEET  (CONCEPT ONLY — 5 UPGRADES × 2 VARIANTS + FULL GROWTH)",
        fill=(244, 238, 225),
        font=f_h1,
    )
    mdraw.text(
        (28, 58),
        "OWNER'S #1 RULE: Every upgrade must VISIBLY transform 07 at the ~7.0 m Chase Camera (16° pitch, 52° FOV) & in Silhouette — never a 5 cm invisible detail.",
        fill=(255, 181, 71),
        font=f_h3,
    )
    rules_bar = (
        "5 VISUAL LANGUAGE RULES:   [1] >=10–15% Silhouette Contour / Mass Change at 7m   |   "
        "[2] ONE Maker Accent (Amber #FFB547 / Coral #FF8A5B, 1978 SSR-3 White #ECE8DF, 2004 Tycho Teal #3E7A82)   |   "
        "[3] Bolted On by 07 Itself (K-A Sockets, Weld Beads, Cut '07' Tag, Umbilicals)   |   "
        "[4] Vacuum Logic (Copper Fins, MLI Foil, Truss/Dish)   |   [5] Calm Symmetric 07"
    )
    mdraw.text((28, 92), rules_bar, fill=(195, 204, 218), font=f_small)
    mdraw.text(
        (28, 116),
        "SILHOUETTE INSET KEY:   Slate Grey = Base R1 07 Footprint   |   Amber (#FFB547) = Variant A Added Silhouette   |   Coral (#FF8A5B) = Variant B Added Silhouette",
        fill=(162, 174, 192),
        font=f_mono_sm,
    )

    def hex_to_rgb(h: str) -> tuple[int, int, int]:
        h = h.lstrip("#")
        return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))

    # Layout columns per row:
    # Col 0 (Info card): x = 16..516 (w = 500)
    # Col 1 (BEFORE):    x = 530..1150 (w = 620)
    # Col 2 (VARIANT A): x = 1164..1894 (w = 730)
    # Col 3 (VARIANT B): x = 1908..2624 (w = 716)

    for idx, spec in enumerate(UPGRADE_SPEC_META):
        uid = spec["id"]
        ry = header_h + idx * row_h
        um = metrics["upgrades"][uid]

        # Row background container
        mdraw.rectangle([12, ry, sheet_w - 12, ry + row_h - 10], fill=(24, 27, 34), outline=(54, 60, 72), width=2)

        # --- COL 0: UPGRADE SPEC & RULES CARD ---
        ix0, iy0, ix1, iy1 = 22, ry + 10, 516, ry + row_h - 20
        mdraw.rectangle([ix0, iy0, ix1, iy1], fill=(30, 34, 43), outline=(72, 80, 96), width=1)
        # Badge
        acc_rgb = hex_to_rgb(spec["accent_hex"])
        acc2_rgb = hex_to_rgb(spec["accent2_hex"])
        mdraw.rectangle([ix0, iy0, ix1, iy0 + 44], fill=(38, 43, 54))
        mdraw.rectangle([ix0, iy0, ix0 + 8, iy0 + 44], fill=acc_rgb)
        mdraw.text((ix0 + 18, iy0 + 9), f"{spec['code']} {spec['title']}", fill=(246, 240, 228), font=f_h2)
        mdraw.text((ix0 + 16, iy0 + 52), f"Unlock: {spec['region']}", fill=(255, 196, 102), font=f_mono_sm)
        mdraw.text((ix0 + 16, iy0 + 74), f"Maker:  {spec['maker']}", fill=(210, 218, 230), font=f_small)

        # Swatches
        mdraw.rectangle([ix0 + 16, iy0 + 98, ix0 + 38, iy0 + 116], fill=acc_rgb, outline=(220, 220, 220))
        mdraw.rectangle([ix0 + 44, iy0 + 98, ix0 + 66, iy0 + 116], fill=acc2_rgb, outline=(220, 220, 220))
        mdraw.text((ix0 + 74, iy0 + 98), spec["accent_name"], fill=(230, 234, 240), font=f_small)

        # Zone & Problem
        y_cursor = iy0 + 126
        for line in wrap_text(mdraw, spec["zone"], f_mono_sm, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(130, 210, 220), font=f_mono_sm)
            y_cursor += 18
        y_cursor += 6
        for line in wrap_text(mdraw, spec["problem"], f_small, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(235, 150, 140), font=f_small)
            y_cursor += 17

        # Variant A & B summaries
        y_cursor += 8
        mdraw.line([(ix0 + 14, y_cursor), (ix1 - 14, y_cursor)], fill=(64, 72, 88), width=1)
        y_cursor += 8
        for line in wrap_text(mdraw, spec["varA_title"], f_h3, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(255, 195, 90), font=f_h3)
            y_cursor += 20
        for line in wrap_text(mdraw, spec["varA_desc"], f_small, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(208, 214, 224), font=f_small)
            y_cursor += 16

        y_cursor += 8
        for line in wrap_text(mdraw, spec["varB_title"], f_h3, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(255, 150, 112), font=f_h3)
            y_cursor += 20
        for line in wrap_text(mdraw, spec["varB_desc"], f_small, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(192, 198, 210), font=f_small)
            y_cursor += 16

        # --- COL 1: BEFORE (CURRENT BASELINE) ---
        bx0, by0, bx1, by1 = 528, ry + 10, 1148, ry + row_h - 20
        mdraw.rectangle([bx0, by0, bx1, by1], fill=(28, 31, 39), outline=(74, 80, 94), width=1)
        mdraw.rectangle([bx0, by0, bx1, by0 + 36], fill=(44, 48, 58))
        bef_label = "BEFORE (Today's +0.35m Thin Rod)" if uid == "a" else (
            "BEFORE (Today's 4cm Igniter Detail)" if uid == "b" else "BEFORE (Base R1 07 — No Upgrade)"
        )
        mdraw.text((bx0 + 14, by0 + 8), bef_label, fill=(210, 216, 226), font=f_h3)

        bef_chase_rgba = load_rgba(f"{uid}_before_chase.png")
        bef_close_rgba = load_rgba(f"{uid}_before_closeup.png")

        img_bef_chase = render_on_studio_bg(bef_chase_rgba, chase_crop_box, (286, 360), draw_grid=True)
        img_bef_close = render_on_studio_bg(bef_close_rgba, None, (304, 360), draw_grid=False)
        master.paste(img_bef_chase, (bx0 + 10, by0 + 44))
        master.paste(img_bef_close, (bx0 + 304, by0 + 44))

        # Baseline silhouette inset
        sil_bef = make_silhouette_diff_inset(base_clean_chase, bef_chase_rgba, chase_crop_box, (86, 86), add_rgb=(220, 90, 90))
        master.paste(sil_bef, (bx0 + 16, by0 + 50))
        mdraw.text((bx0 + 16, by0 + 382), "Chase Cam (7.0 m, 16°)", fill=(185, 194, 208), font=f_mono_sm)
        mdraw.text((bx0 + 312, by0 + 382), "3/4 Mount Close-Up", fill=(185, 194, 208), font=f_mono_sm)

        # --- COL 2 & COL 3: VARIANT A (RECOMMENDED) & VARIANT B ---
        for v_idx, (v_letter, cx0, cx1, is_rec) in enumerate([
            ("A", 1160, 1888, True),
            ("B", 1900, 2624, False),
        ]):
            cy0, cy1 = ry + 10, ry + row_h - 20
            vm = um[v_letter]
            border_col = (255, 181, 71) if is_rec else (110, 122, 142)
            hdr_bg = (62, 48, 26) if is_rec else (38, 44, 56)
            badge_col = (255, 181, 71) if is_rec else (255, 138, 91)

            mdraw.rectangle([cx0, cy0, cx1, cy1], fill=(28, 31, 39), outline=border_col, width=2 if is_rec else 1)
            mdraw.rectangle([cx0, cy0, cx1, cy0 + 38], fill=hdr_bg)

            v_hdr = f"VARIANT {v_letter} ★ RECOMMENDED" if is_rec else f"VARIANT {v_letter} (ALTERNATIVE)"
            mdraw.text((cx0 + 14, cy0 + 8), v_hdr, fill=badge_col, font=f_h3)

            metric_str = f"Chase Sil: +{vm['new_contour_chase_pct']:.1f}% new (+{vm['net_chase_pct']:.1f}% net) | 3/4: +{vm['new_contour_closeup_pct']:.1f}%"
            mdraw.text((cx0 + 275, cy0 + 10), metric_str, fill=(242, 244, 248), font=f_mono_sm)

            v_chase_rgba = load_rgba(f"{uid}_{v_letter}_chase.png")
            v_close_rgba = load_rgba(f"{uid}_{v_letter}_closeup.png")

            img_v_chase = render_on_studio_bg(v_chase_rgba, chase_crop_box, (336, 360), draw_grid=True)
            img_v_close = render_on_studio_bg(v_close_rgba, None, (362, 360), draw_grid=False)
            master.paste(img_v_chase, (cx0 + 10, cy0 + 44))
            master.paste(img_v_close, (cx0 + 354, cy0 + 44))

            # Silhouette diff inset in top-left of Chase panel
            sil_v = make_silhouette_diff_inset(
                base_clean_chase,
                v_chase_rgba,
                chase_crop_box,
                (104, 104),
                add_rgb=(255, 181, 71) if is_rec else (255, 138, 91),
            )
            master.paste(sil_v, (cx0 + 16, cy0 + 50))
            mdraw.rectangle([cx0 + 16, cy0 + 156, cx0 + 168, cy0 + 178], fill=(20, 22, 28))
            mdraw.text(
                (cx0 + 20, cy0 + 158),
                f"+{vm['new_contour_chase_pct']:.1f}% 7m Sil",
                fill=badge_col,
                font=f_mono_sm,
            )

            mdraw.text((cx0 + 16, cy0 + 382), f"Chase Cam (7.0 m, 16°) — Var {v_letter}", fill=(215, 222, 234), font=f_mono_sm)
            mdraw.text((cx0 + 362, cy0 + 382), f"3/4 Close-Up — Bolted Mount & Maker Accent", fill=(215, 222, 234), font=f_mono_sm)

        # Also save a standalone high-res per-upgrade row image Logs/captures/upgrade_<uid>.png
        row_crop = master.crop((0, ry - 4, sheet_w, ry + row_h - 4))
        row_crop.save(captures_dir / f"upgrade_{uid}.png")

    # ==========================================================================
    # BOTTOM ROW ON MASTER SHEET: FULLY GROWN 07 (ALL 5 RECOMMENDED UPGRADES)
    # ==========================================================================
    fy = header_h + 5 * row_h + 4
    fg_m = metrics["full_growth"]
    mdraw.rectangle([12, fy, sheet_w - 12, fy + fg_row_h - 8], fill=(25, 30, 38), outline=(255, 181, 71), width=3)
    mdraw.rectangle([12, fy, sheet_w - 12, fy + 46], fill=(58, 46, 26))
    mdraw.text(
        (28, fy + 10),
        "FULLY GROWN 07 — ALL 5 RECOMMENDED UPGRADES MOUNTED SIMULTANEOUSLY  (a_A + b_A + c_A + d_A + e_A)",
        fill=(255, 195, 92),
        font=f_h2,
    )
    mdraw.text(
        (1340, fy + 13),
        f"7m Chase Silhouette Expansion: +{fg_m['new_contour_chase_pct']:.1f}% new contour (+{fg_m['net_chase_pct']:.1f}% net)  |  Front 3/4: +{fg_m['net_front3q_pct']:.1f}%  |  Rear 3/4: +{fg_m['net_rear3q_pct']:.1f}%",
        fill=(245, 247, 250),
        font=f_mono,
    )

    # 5 panels across bottom row:
    # 1) Base 07 7m Chase  2) Fully Grown 07 7m Chase (+ Silhouette Diff)  3) Base Front 3/4  4) Fully Grown Front 3/4  5) Fully Grown Rear 3/4
    p_y0 = fy + 56
    p_h = 430

    p1 = render_on_studio_bg(base_clean_chase, chase_crop_box, (420, p_h), draw_grid=True)
    p2 = render_on_studio_bg(fg_chase, chase_crop_box, (480, p_h), draw_grid=True)
    p3 = render_on_studio_bg(fb_f3q, None, (480, p_h), draw_grid=False)
    p4 = render_on_studio_bg(fg_f3q, None, (580, p_h), draw_grid=False)
    p5 = render_on_studio_bg(fg_r3q, None, (580, p_h), draw_grid=False)

    master.paste(p1, (26, p_y0))
    master.paste(p2, (460, p_y0))
    master.paste(p3, (954, p_y0))
    master.paste(p4, (1448, p_y0))
    master.paste(p5, (2042, p_y0))

    sil_fg = make_silhouette_diff_inset(base_clean_chase, fg_chase, chase_crop_box, (128, 128), add_rgb=(255, 181, 71))
    master.paste(sil_fg, (470, p_y0 + 10))
    mdraw.rectangle([470, p_y0 + 140, 650, p_y0 + 164], fill=(20, 22, 28))
    mdraw.text((476, p_y0 + 143), f"+{fg_m['new_contour_chase_pct']:.1f}% 7m Growth", fill=(255, 181, 71), font=f_mono_sm)

    mdraw.text((34, p_y0 + p_h + 8), "1. BEFORE — Base R1 07 at 7.0 m Chase Cam", fill=(195, 204, 218), font=f_mono_sm)
    mdraw.text((468, p_y0 + p_h + 8), "2. FULLY GROWN 07 — 7.0 m Chase Cam (All 5 Zones Active)", fill=(255, 195, 92), font=f_mono_sm)
    mdraw.text((962, p_y0 + p_h + 8), "3. BEFORE — Base R1 07 Front 3/4", fill=(195, 204, 218), font=f_mono_sm)
    mdraw.text((1456, p_y0 + p_h + 8), "4. FULLY GROWN 07 — Front 3/4 (Calm CRT Face Unobscured!)", fill=(255, 195, 92), font=f_mono_sm)
    mdraw.text((2050, p_y0 + p_h + 8), "5. FULLY GROWN 07 — Rear 3/4 (All 5 Non-Overlapping Zones)", fill=(255, 195, 92), font=f_mono_sm)

    master_path = captures_dir / "upgrades_sheet.png"
    master.save(master_path)
    print("Saved master contact sheet:", master_path)

    # ==========================================================================
    # BUILD DEDICATED HIGH-RES FULL-GROWTH SHEET: Logs/captures/full_growth.png
    # ==========================================================================
    fg_w, fg_h = 2280, 1340
    fg_sheet = Image.new("RGB", (fg_w, fg_h), (18, 20, 25))
    fdraw = ImageDraw.Draw(fg_sheet)

    fdraw.rectangle([0, 0, fg_w, 128], fill=(28, 32, 40), outline=(255, 181, 71), width=2)
    fdraw.rectangle([0, 0, 12, 128], fill=(255, 181, 71))
    fdraw.text(
        (28, 16),
        "FULLY GROWN 07 — ALL 5 RECOMMENDED UPGRADES MOUNTED TOGETHER (NON-OVERLAPPING ZONE ARCHITECTURE)",
        fill=(246, 240, 228),
        font=f_h1,
    )
    fdraw.text(
        (28, 58),
        f"Cohesion Proof: 5 Distinct Mounting Zones  |  +{fg_m['new_contour_chase_pct']:.1f}% New 7m Chase Silhouette (+{fg_m['net_chase_pct']:.1f}% Net Area)  |  CRT Face & Half-Lidded Eye Remain 100% Calm, Cute & Bilateral",
        fill=(255, 181, 71),
        font=f_h3,
    )
    fdraw.text(
        (28, 92),
        "Z1 Dorsal Shoulders: (a) Dish-Crown Mast & Bridge [Amber/Coral]   |   Z2 Bonnet & Cutter: (b) Twin-Shoulder Plasma Coils [Amber/Coral]   |   "
        "Z3 Visor Ears: (c) SSR-3 Box Floodlights [1978 White/Orange]   |   Z4 Mid-Flanks: (d) Battery Saddle-Pods [GoldFoil/Amber]   |   Z5 Rear Valence: (e) Tycho Hex Pantograph [2004 White/Teal #3E7A82]",
        fill=(198, 208, 222),
        font=f_small,
    )

    # Top Row (y = 144..714): BEFORE vs AFTER across 1) 7.0 m Chase Camera  2) Front 3/4  3) Rear 3/4
    cols = [
        ("7.0 m CHASE CAMERA (16° PITCH, 52° FOV)", base_clean_chase, fg_chase, chase_crop_box, True),
        ("FRONT 3/4 HERO VIEW (FACE & CUTTER ZONES)", fb_f3q, fg_f3q, None, False),
        ("REAR 3/4 HERO VIEW (DORSAL, FLANK & TYCHO ZONES)", fb_r3q, fg_r3q, None, False),
    ]
    col_w = 736
    for ci, (ctitle, b_img, a_img, cbox, is_chase) in enumerate(cols):
        cx = 20 + ci * (col_w + 16)
        cy = 142
        ch = 1176
        fdraw.rectangle([cx, cy, cx + col_w, cy + ch], fill=(25, 28, 36), outline=(72, 80, 96), width=2)
        fdraw.rectangle([cx, cy, cx + col_w, cy + 40], fill=(40, 45, 56))
        fdraw.text((cx + 16, cy + 9), ctitle, fill=(244, 238, 225), font=f_h3)

        # Top sub-panel: BEFORE (Base R1 07)
        sub_w, sub_h = col_w - 24, 510
        im_before = render_on_studio_bg(b_img, cbox, (sub_w, sub_h), draw_grid=is_chase)
        fg_sheet.paste(im_before, (cx + 12, cy + 48))
        fdraw.rectangle([cx + 20, cy + 56, cx + 310, cy + 88], fill=(20, 22, 28))
        fdraw.text((cx + 28, cy + 62), "BEFORE: Base R1 07 (Unmodified)", fill=(195, 204, 218), font=f_mono)

        # Bottom sub-panel: AFTER (Fully Grown 07 — All 5 Recommended Upgrades)
        im_after = render_on_studio_bg(a_img, cbox, (sub_w, sub_h), draw_grid=is_chase)
        fg_sheet.paste(im_after, (cx + 12, cy + 610))
        fdraw.rectangle([cx + 20, cy + 618, cx + 485, cy + 650], fill=(58, 44, 22), outline=(255, 181, 71), width=1)
        fdraw.text((cx + 28, cy + 624), "AFTER: Fully Grown 07 (a_A + b_A + c_A + d_A + e_A)", fill=(255, 195, 92), font=f_mono)

        if is_chase:
            sil_inset = make_silhouette_diff_inset(b_img, a_img, cbox, (164, 164), add_rgb=(255, 181, 71))
            fg_sheet.paste(sil_inset, (cx + sub_w - 160, cy + 622))
            fdraw.rectangle([cx + sub_w - 160, cy + 788, cx + sub_w + 4, cy + 812], fill=(20, 22, 28))
            fdraw.text(
                (cx + sub_w - 154, cy + 791),
                f"+{fg_m['new_contour_chase_pct']:.1f}% 7m Contour",
                fill=(255, 181, 71),
                font=f_mono_sm,
            )

        # Zone callout footer inside each column
        if ci == 0:
            footer_txt = "At 7.0 m chase cam: SSR-3 white floodlights widen head +53%; Flank pods widen hull +54%; Mast rises +36%; Tycho hex shield anchors rear."
        elif ci == 1:
            footer_txt = "Front 3/4: CRT face & half-lidded brass eye remain 100% open & cozy. Twin shoulder plasma coils + SSR-3 ear lamps frame the visage."
        else:
            footer_txt = "Rear 3/4: Every maker era reads cleanly — 1974 Cream/Coral bridge, 1978 White/Orange SSR-3 lamps, 2004 White/Teal #3E7A82 Tycho hex shield."
        y_ft = cy + 1130
        for line in wrap_text(fdraw, footer_txt, f_small, col_w - 28):
            fdraw.text((cx + 14, y_ft), line, fill=(210, 218, 230), font=f_small)
            y_ft += 18

    fg_path = captures_dir / "full_growth.png"
    fg_sheet.save(fg_path)
    print("Saved full growth sheet:", fg_path)


UPGRADE_SPEC_META_R2 = [
    {
        "id": "a",
        "code": "(a)",
        "title": "ANTENNA EXTENDED (SLIM PICK A)",
        "region": "R3 — Elder 03's Gift (Telemetry Range)",
        "maker": "1974 Kessler–Arai / Lumen Series-7",
        "accent_name": "R1_LampAmber (#FFB547) + R1_OrangeStripe (#FF8A5B)",
        "accent_hex": "#FFB547",
        "accent2_hex": "#FF8A5B",
        "zone": "Zone 1: Rear Dorsal Shoulders & Roofline (y = 0.90–2.04 m)",
        "slim_note": "Round 2 Slim (-36% vol): Bridge thinned 0.055->0.036 m; dish Ø 0.43->0.33 m; mast apex 2.22->2.04 m; preamp r 0.062->0.044 m.",
        "varA_title": "Owner Pick A ★ (Slimmed): Dish-Crown Mast & Telemetry Bridge",
        "varA_desc": "Low-profile bolted dorsal bridge across rear shoulder sockets + compact left D/F loop + 2.04 m stepped mast with Ø 0.33 m parabolic ring-dish & Amber lantern tip.",
    },
    {
        "id": "b",
        "code": "(b)",
        "title": "IGNITER FITTED (SLIM PICK A)",
        "region": "R4 — Elder 04's Gift (Thermal Cutter Ignition)",
        "maker": "1974 Kessler–Arai / Lumen Heavy Mining",
        "accent_name": "R1_LampAmber (#FFB547) Plasma Coil + R1_OrangeStripe (#FF8A5B)",
        "accent_hex": "#FFB547",
        "accent2_hex": "#FF8A5B",
        "zone": "Zone 2: Front Bonnet Shoulders & Cutter Turret (x = ±0.49 m, z = +0.42..+0.84 m)",
        "slim_note": "Round 2 Slim (-38% vol): Canisters pulled inward x = ±0.56->±0.49 m & slimmed -43%; copper fins tucked inside tyre span (x = ±0.59 m); sleeker r = 0.144 m turret shroud.",
        "varA_title": "Owner Pick A ★ (Slimmed): Twin-Shoulder Plasma Ignition Saddle & Shroud",
        "varA_desc": "Thin horseshoe bonnet yoke + compact canted coral/amber plasma-coil pods on front shoulders (visible past head at 7 m without shoulder bulk) + copper-finned turret shroud.",
    },
    {
        "id": "c",
        "code": "(c)",
        "title": "SSR-3 HEADLAMP (SLIM PICK A)",
        "region": "R10–11 — Salvaged Elder 06 Sealed-Beam Lamp",
        "maker": "1978 SSR-3 Deep-Crater Survey (Era B Donor)",
        "accent_name": "R1_WhitePaint (#ECE8DF) + Thin R1_OrangeStripe + R1_LampAmber Lens",
        "accent_hex": "#ECE8DF",
        "accent2_hex": "#FFB547",
        "zone": "Zone 3: CRT Head Upper Brow Corners (x = ±0.482 m, y = 1.40–1.64 m)",
        "slim_note": "Round 2 Slim (-40% vol): White SSR-3 floodlight housings reduced 0.26×0.27×0.29 m -> 0.205×0.215×0.225 m (-44% vol) & pulled tighter to visor corners (x = ±0.482 m).",
        "varA_title": "Owner Pick A ★ (Slimmed): Twin Salvaged SSR-3 Box Floodlights on Visor Ears",
        "varA_desc": "Compact 1978 white-enamel brake-bent floodlight pods welded onto tight brow-corner gussets above the cheek brass eye, with front Amber sealed-beam lenses & rear pilot slits.",
    },
    {
        "id": "d",
        "code": "(d)",
        "title": "CELL PACK (SLIM PICK A)",
        "region": "R11 — Elder 06's Salvaged Battery Cells (Peak Key)",
        "maker": "1974 Kessler–Arai / Salvaged High-Capacity Cells",
        "accent_name": "R1_GoldFoil MLI + R1_OrangeStripe Caps + 3× R1_LampAmber Charge Bars",
        "accent_hex": "#FFB547",
        "accent2_hex": "#D8A24A",
        "zone": "Zone 4: Mid-Hull Flank Sockets (x = ±0.558 m, y = 0.66–1.07 m, z = -0.10 m)",
        "slim_note": "Round 2 Slim (-42% vol): Saddle-pods reduced 0.28×0.42×0.54 m -> 0.215×0.34×0.44 m & tucked to x = ±0.558 m so outer faces (±0.665 m) sit INSIDE the outer tyre stance (±0.738 m)!",
        "varA_title": "Owner Pick A ★ (Slimmed): Twin Flank Saddle-Pod Battery Panniers",
        "varA_desc": "Body-hugging chamfered cream/coral battery panniers over GoldFoil MLI blankets with top brass cell caps, copper busbar, and 3 vertical + 3 rear-facing Amber charge bars.",
    },
    {
        "id": "e",
        "code": "(e)",
        "title": "HEX CHARGE ADAPTER (REDESIGNED)",
        "region": "R10 — Tycho Consortium Hex Contact Dock Plug",
        "maker": "2004 Tycho Consortium (Era D Donor Hardware)",
        "accent_name": "R1_TealMark (#3E7A82) Hex Collar + R1_Copper Contacts + R1_TealLamp",
        "accent_hex": "#3E7A82",
        "accent2_hex": "#33DDE8",
        "zone": "Zone 5 (NEW): Charging Contact — Front Lower Bumper / Belly / Lower Flank Port",
        "slim_note": "Round 2 Purpose-First Redesign: Removed from rear bumper ('07's butt')! Placed where 07 actually meets a charger: at the lower charging contact (front lower bumper / lower flank).",
        "varA_title": "New Variant A ★ [RECOMMENDED]: Fold-Down Front-Bumper Hex Contact Shoe",
        "varA_desc": "Fold-down hex contact shoe under the front lower bumper & belly with a bold Teal hex-ring collar (#3E7A82), 6 copper contacts, and a small glowing Teal status lamp visible from the side.",
        "varB_title": "New Variant B: Compact Lower-Flank Clip-On Hex Adapter Puck",
        "varB_desc": "Compact Ø 0.29 m hexagonal adapter puck that clips directly over 07's existing charge port on the lower hull flank (between wheels), with Teal hex ring, 6 copper contact pins & Teal LED.",
    },
]


def compose_contact_sheets_r2(repo_root: Path):
    """
    Compose Round 2 deliverables:
      1) Logs/captures/upgrades_sheet_r2.png
      2) Logs/captures/full_growth_r2.png
      3) Logs/captures/upgrade_{a..e}_r2.png
      4) Logs/captures/silhouette_metrics_r2.json
    """
    from PIL import Image, ImageDraw, ImageFont

    captures_dir = repo_root / "Logs" / "captures"
    raw_dir = captures_dir / "_raw"
    font_dir = repo_root / "Assets" / "_Project" / "UI" / "Fonts"

    def load_font(name: str, size: int):
        fp = font_dir / name
        if fp.exists():
            return ImageFont.truetype(str(fp), size)
        return ImageFont.load_default()

    f_h1 = load_font("BeVietnamPro-SemiBold.ttf", 29)
    f_h2 = load_font("BeVietnamPro-SemiBold.ttf", 20)
    f_h3 = load_font("BeVietnamPro-SemiBold.ttf", 16)
    f_small = load_font("BeVietnamPro-Regular.ttf", 13)
    f_mono = load_font("IBMPlexMono-Bold.ttf", 15)
    f_mono_sm = load_font("IBMPlexMono-Bold.ttf", 13)

    def wrap_text(draw, text: str, font, max_w: int) -> list[str]:
        words = text.split()
        lines = []
        cur = ""
        for w in words:
            test = f"{cur} {w}".strip()
            bbox = draw.textbbox((0, 0), test, font=font)
            if bbox[2] - bbox[0] <= max_w or not cur:
                cur = test
            else:
                lines.append(cur)
                cur = w
        if cur:
            lines.append(cur)
        return lines

    def load_rgba(name: str) -> Image.Image:
        return Image.open(raw_dir / name).convert("RGBA")

    def alpha_mask(img: Image.Image) -> np.ndarray:
        arr = np.array(img)
        return arr[:, :, 3] > 32

    chase_files = [
        "full_before_chase.png",
        "full_after_chase.png",
        "full_after_r2_chase.png",
        "a_before_chase.png",
        "b_before_chase.png",
        "a_A_chase.png",
        "b_A_chase.png",
        "c_A_chase.png",
        "d_A_chase.png",
        "a_A_r2_chase.png",
        "b_A_r2_chase.png",
        "c_A_r2_chase.png",
        "d_A_r2_chase.png",
        "e_A_r2_chase.png",
        "e_B_r2_chase.png",
    ]
    ymin_all, ymax_all, xmin_all, xmax_all = 9999, 0, 9999, 0
    for cf in chase_files:
        p = raw_dir / cf
        if not p.exists():
            continue
        m = alpha_mask(load_rgba(cf))
        ys, xs = np.where(m)
        if len(ys) > 0:
            ymin_all = min(ymin_all, int(ys.min()))
            ymax_all = max(ymax_all, int(ys.max()))
            xmin_all = min(xmin_all, int(xs.min()))
            xmax_all = max(xmax_all, int(xs.max()))

    pad = 42
    cy_mid = (ymin_all + ymax_all) // 2
    cx_mid = (xmin_all + xmax_all) // 2
    half_span = max((ymax_all - ymin_all) // 2, (xmax_all - xmin_all) // 2) + pad
    c_x0 = max(0, cx_mid - half_span)
    c_y0 = max(0, cy_mid - half_span)
    c_x1 = min(960, cx_mid + half_span)
    c_y1 = min(960, cy_mid + half_span)
    chase_crop_box = (c_x0, c_y0, c_x1, c_y1)

    def render_on_studio_bg(rgba_img: Image.Image, crop_box: tuple[int, int, int, int] | None, target_size: tuple[int, int], draw_grid: bool = False) -> Image.Image:
        if crop_box is not None:
            rgba_img = rgba_img.crop(crop_box)
        w, h = target_size
        resized = rgba_img.resize((w, h), Image.Resampling.LANCZOS)

        y_grad = np.linspace(0.0, 1.0, h)[:, None]
        x_grad = np.linspace(-1.0, 1.0, w)[None, :]
        rad = np.clip(1.0 - 0.35 * (x_grad**2 + (y_grad - 0.55) ** 2), 0.65, 1.0)
        bg = np.zeros((h, w, 4), dtype=np.uint8)
        bg[:, :, 0] = np.clip((34 + 14 * (1.0 - y_grad)) * rad, 20, 65).astype(np.uint8)
        bg[:, :, 1] = np.clip((37 + 15 * (1.0 - y_grad)) * rad, 22, 70).astype(np.uint8)
        bg[:, :, 2] = np.clip((45 + 18 * (1.0 - y_grad)) * rad, 28, 82).astype(np.uint8)
        bg[:, :, 3] = 255
        bg_img = Image.fromarray(bg, "RGBA")

        if draw_grid:
            gd = ImageDraw.Draw(bg_img)
            for gx in range(0, w, w // 6):
                gd.line([(gx, 0), (gx, h)], fill=(58, 63, 74, 140), width=1)
            for gy in range(0, h, h // 6):
                gd.line([(0, gy), (w, gy)], fill=(58, 63, 74, 140), width=1)

        bg_img.alpha_composite(resized)
        return bg_img.convert("RGB")

    def make_silhouette_diff_inset(
        base_rgba: Image.Image,
        var_rgba: Image.Image,
        crop_box: tuple[int, int, int, int],
        target_size: tuple[int, int],
        add_rgb: tuple[int, int, int] = (255, 181, 71),
        r1_rgba: Image.Image | None = None,
    ) -> Image.Image:
        """Create a silhouette inset: slate = base 07, add_rgb = Round 2 silhouette, optional cyan = bulk shaved from R1."""
        b_crop = base_rgba.crop(crop_box).resize(target_size, Image.Resampling.NEAREST)
        v_crop = var_rgba.crop(crop_box).resize(target_size, Image.Resampling.NEAREST)
        bm = alpha_mask(b_crop)
        vm = alpha_mask(v_crop)
        added = vm & (~bm)
        common = vm & bm

        w, h = target_size
        out = np.zeros((h, w, 3), dtype=np.uint8)
        out[:, :] = (20, 22, 28)
        out[common] = (108, 116, 132)
        if r1_rgba is not None:
            r1_crop = r1_rgba.crop(crop_box).resize(target_size, Image.Resampling.NEAREST)
            r1m = alpha_mask(r1_crop)
            shaved = (r1m & (~bm)) & (~vm)
            out[shaved] = (72, 195, 210)  # Teal/Cyan = excess bulk shaved off in Round 2
        out[added] = add_rgb
        img = Image.fromarray(out, "RGB")
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, w - 1, h - 1], outline=(88, 96, 112), width=2)
        return img

    base_clean_chase = load_rgba("full_before_chase.png")
    base_clean_px = int(alpha_mask(base_clean_chase).sum())
    bm_m = alpha_mask(base_clean_chase)

    metrics_r2: dict[str, dict] = {"base_chase_px": base_clean_px, "upgrades": {}}

    # Compute metrics for a..d (Slim A vs Round 1 A) and e (New Var A & New Var B)
    for spec in UPGRADE_SPEC_META_R2:
        uid = spec["id"]
        bef_chase = load_rgba(f"{uid}_before_chase.png")
        bef_close_name = "e_before_r2_closeup.png" if uid == "e" else f"{uid}_before_closeup.png"
        bef_close = load_rgba(bef_close_name)
        bef_close_px = int(alpha_mask(bef_close).sum())
        bcl_m = alpha_mask(bef_close)

        metrics_r2["upgrades"][uid] = {}

        if uid in ("a", "b", "c", "d"):
            # Round 1 A reference
            r1_chase = load_rgba(f"{uid}_A_chase.png")
            r1_m = alpha_mask(r1_chase)
            r1_new_pct = int((r1_m & (~bm_m)).sum()) / float(base_clean_px) * 100.0

            # Round 2 Slim A
            r2_chase = load_rgba(f"{uid}_A_r2_chase.png")
            r2_close = load_rgba(f"{uid}_A_r2_closeup.png")
            r2_m = alpha_mask(r2_chase)
            r2_cl_m = alpha_mask(r2_close)

            added_chase_px = int((r2_m & (~bm_m)).sum())
            net_chase_pct = (int(r2_m.sum()) - base_clean_px) / float(base_clean_px) * 100.0
            new_contour_chase_pct = added_chase_px / float(base_clean_px) * 100.0
            new_contour_close_pct = int((r2_cl_m & (~bcl_m)).sum()) / float(bef_close_px) * 100.0
            slim_reduction_pct = (r1_new_pct - new_contour_chase_pct) / max(0.1, r1_new_pct) * 100.0

            metrics_r2["upgrades"][uid]["A_r1_contour_pct"] = round(r1_new_pct, 1)
            metrics_r2["upgrades"][uid]["A_slim"] = {
                "chase_px": int(r2_m.sum()),
                "net_chase_pct": round(net_chase_pct, 1),
                "new_contour_chase_pct": round(new_contour_chase_pct, 1),
                "new_contour_closeup_pct": round(new_contour_close_pct, 1),
                "silhouette_reduction_vs_r1_pct": round(slim_reduction_pct, 1),
            }
        else:
            for v in ("A", "B"):
                vc = load_rgba(f"e_{v}_r2_chase.png")
                vcl = load_rgba(f"e_{v}_r2_closeup.png")
                vc_m = alpha_mask(vc)
                vcl_m = alpha_mask(vcl)
                added_chase_px = int((vc_m & (~bm_m)).sum())
                net_chase_pct = (int(vc_m.sum()) - base_clean_px) / float(base_clean_px) * 100.0
                new_contour_chase_pct = added_chase_px / float(base_clean_px) * 100.0
                new_contour_close_pct = int((vcl_m & (~bcl_m)).sum()) / float(bef_close_px) * 100.0
                metrics_r2["upgrades"]["e"][v] = {
                    "chase_px": int(vc_m.sum()),
                    "net_chase_pct": round(net_chase_pct, 1),
                    "new_contour_chase_pct": round(new_contour_chase_pct, 1),
                    "new_contour_closeup_pct": round(new_contour_close_pct, 1),
                }

    fg_r1_chase = load_rgba("full_after_chase.png")
    fg_r2_chase = load_rgba("full_after_r2_chase.png")
    fg_r2_f3q = load_rgba("full_after_r2_front3q.png")
    fg_r2_r3q = load_rgba("full_after_r2_rear3q.png")
    fb_f3q = load_rgba("full_before_front3q.png")
    fb_r3q = load_rgba("full_before_rear3q.png")

    r1_fg_m = alpha_mask(fg_r1_chase)
    r2_fg_m = alpha_mask(fg_r2_chase)
    r1_fg_pct = int((r1_fg_m & (~bm_m)).sum()) / float(base_clean_px) * 100.0
    r2_fg_pct = int((r2_fg_m & (~bm_m)).sum()) / float(base_clean_px) * 100.0

    metrics_r2["full_growth_r2"] = {
        "r1_new_contour_chase_pct": round(r1_fg_pct, 1),
        "net_chase_pct": round((int(r2_fg_m.sum()) - base_clean_px) / float(base_clean_px) * 100.0, 1),
        "new_contour_chase_pct": round(r2_fg_pct, 1),
        "net_front3q_pct": round(
            (int(alpha_mask(fg_r2_f3q).sum()) - int(alpha_mask(fb_f3q).sum()))
            / float(alpha_mask(fb_f3q).sum())
            * 100.0,
            1,
        ),
        "net_rear3q_pct": round(
            (int(alpha_mask(fg_r2_r3q).sum()) - int(alpha_mask(fb_r3q).sum()))
            / float(alpha_mask(fb_r3q).sum())
            * 100.0,
            1,
        ),
    }

    (captures_dir / "silhouette_metrics_r2.json").write_text(json.dumps(metrics_r2, indent=2), encoding="utf-8")
    print("Saved Round 2 silhouette metrics:", json.dumps(metrics_r2, indent=2))

    # ==========================================================================
    # BUILD ROUND 2 MASTER SHEET: Logs/captures/upgrades_sheet_r2.png
    # ==========================================================================
    sheet_w = 2640
    header_h = 152
    row_h = 468
    fg_row_h = 540
    sheet_h = header_h + 5 * row_h + fg_row_h + 24

    master = Image.new("RGB", (sheet_w, sheet_h), (18, 20, 25))
    mdraw = ImageDraw.Draw(master)

    mdraw.rectangle([0, 0, sheet_w, header_h - 8], fill=(26, 29, 36), outline=(66, 72, 86), width=2)
    mdraw.rectangle([0, 0, 12, header_h - 8], fill=(72, 195, 210))
    mdraw.text(
        (28, 16),
        "07 — UPGRADE VISUAL LANGUAGE: ROUND 2 CONCEPT SHEET  (SLIMMED a–d PICK 'A' + PURPOSE-FIRST (e) HEX PLUG)",
        fill=(244, 238, 225),
        font=f_h1,
    )
    mdraw.text(
        (28, 56),
        "ROUND 2 OWNER NOTES:  [1] Slimmed (a)–(d) Pick 'A' by ~35–40% Mass/Volume (6–8% 7m Silhouette, Not Bulky!)   |   [2] Redesigned (e) Hex Plug at CHARGING CONTACT (Front Lower Bumper / Flank — Nothing on 07's Butt!)",
        fill=(255, 188, 82),
        font=f_h3,
    )
    rules_bar = (
        "ROUND 2 TARGETS:   (a)–(d) keep Owner Pick 'A' idea with thinner brackets, smaller pods & tighter body fit (6–8% 7m silhouette each)   |   "
        "(e) 2 new purpose-first plug variants at the charger contact (Var A: Under-Bumper Fold-Down Hex Shoe + Side Teal Lamp; Var B: Lower-Flank Clip-On Hex Puck)"
    )
    mdraw.text((28, 92), rules_bar, fill=(195, 204, 218), font=f_small)
    mdraw.text(
        (28, 116),
        "SILHOUETTE INSET KEY:   Slate Grey = Base R1 07   |   Amber (#FFB547) = Round 2 Slim Added Silhouette (6–8%)   |   Cyan (#48C3D2) = Excess Bulk Shaved Off from Round 1",
        fill=(140, 212, 225),
        font=f_mono_sm,
    )

    def hex_to_rgb(h: str) -> tuple[int, int, int]:
        h = h.lstrip("#")
        return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))

    for idx, spec in enumerate(UPGRADE_SPEC_META_R2):
        uid = spec["id"]
        ry = header_h + idx * row_h
        um = metrics_r2["upgrades"][uid]

        mdraw.rectangle([12, ry, sheet_w - 12, ry + row_h - 10], fill=(24, 27, 34), outline=(54, 60, 72), width=2)

        # --- COL 0: SPEC CARD ---
        ix0, iy0, ix1, iy1 = 22, ry + 10, 516, ry + row_h - 20
        mdraw.rectangle([ix0, iy0, ix1, iy1], fill=(30, 34, 43), outline=(72, 80, 96), width=1)
        acc_rgb = hex_to_rgb(spec["accent_hex"])
        acc2_rgb = hex_to_rgb(spec["accent2_hex"])
        mdraw.rectangle([ix0, iy0, ix1, iy0 + 44], fill=(38, 43, 54))
        mdraw.rectangle([ix0, iy0, ix0 + 8, iy0 + 44], fill=acc_rgb)
        mdraw.text((ix0 + 18, iy0 + 9), f"{spec['code']} {spec['title']}", fill=(246, 240, 228), font=f_h2)
        mdraw.text((ix0 + 16, iy0 + 52), f"Unlock: {spec['region']}", fill=(255, 196, 102), font=f_mono_sm)
        mdraw.text((ix0 + 16, iy0 + 74), f"Maker:  {spec['maker']}", fill=(210, 218, 230), font=f_small)

        mdraw.rectangle([ix0 + 16, iy0 + 98, ix0 + 38, iy0 + 116], fill=acc_rgb, outline=(220, 220, 220))
        mdraw.rectangle([ix0 + 44, iy0 + 98, ix0 + 66, iy0 + 116], fill=acc2_rgb, outline=(220, 220, 220))
        mdraw.text((ix0 + 74, iy0 + 98), spec["accent_name"], fill=(230, 234, 240), font=f_small)

        y_cursor = iy0 + 126
        for line in wrap_text(mdraw, spec["zone"], f_mono_sm, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(130, 210, 220), font=f_mono_sm)
            y_cursor += 18
        y_cursor += 4
        for line in wrap_text(mdraw, spec["slim_note"], f_small, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(140, 228, 190), font=f_small)
            y_cursor += 17

        y_cursor += 8
        mdraw.line([(ix0 + 14, y_cursor), (ix1 - 14, y_cursor)], fill=(64, 72, 88), width=1)
        y_cursor += 8
        for line in wrap_text(mdraw, spec["varA_title"], f_h3, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(255, 195, 90), font=f_h3)
            y_cursor += 19
        for line in wrap_text(mdraw, spec["varA_desc"], f_small, 464):
            mdraw.text((ix0 + 16, y_cursor), line, fill=(208, 214, 224), font=f_small)
            y_cursor += 16

        if "varB_title" in spec:
            y_cursor += 6
            for line in wrap_text(mdraw, spec["varB_title"], f_h3, 464):
                mdraw.text((ix0 + 16, y_cursor), line, fill=(110, 220, 235), font=f_h3)
                y_cursor += 19
            for line in wrap_text(mdraw, spec["varB_desc"], f_small, 464):
                mdraw.text((ix0 + 16, y_cursor), line, fill=(192, 198, 210), font=f_small)
                y_cursor += 16

        # --- COL 1: BEFORE (CURRENT BASELINE) ---
        bx0, by0, bx1, by1 = 528, ry + 10, 1148, ry + row_h - 20
        mdraw.rectangle([bx0, by0, bx1, by1], fill=(28, 31, 39), outline=(74, 80, 94), width=1)
        mdraw.rectangle([bx0, by0, bx1, by0 + 36], fill=(44, 48, 58))
        bef_label = (
            "BEFORE (Today's +0.35m Thin Rod)"
            if uid == "a"
            else (
                "BEFORE (Today's 4cm Igniter Detail)"
                if uid == "b"
                else (
                    "BEFORE (Front Bumper & Lower Flank — No Plug)"
                    if uid == "e"
                    else "BEFORE (Base R1 07 — No Upgrade)"
                )
            )
        )
        mdraw.text((bx0 + 14, by0 + 8), bef_label, fill=(210, 216, 226), font=f_h3)

        bef_chase_rgba = load_rgba(f"{uid}_before_chase.png")
        bef_close_rgba = load_rgba("e_before_r2_closeup.png" if uid == "e" else f"{uid}_before_closeup.png")

        img_bef_chase = render_on_studio_bg(bef_chase_rgba, chase_crop_box, (286, 360), draw_grid=True)
        img_bef_close = render_on_studio_bg(bef_close_rgba, None, (304, 360), draw_grid=False)
        master.paste(img_bef_chase, (bx0 + 10, by0 + 44))
        master.paste(img_bef_close, (bx0 + 304, by0 + 44))

        sil_bef = make_silhouette_diff_inset(base_clean_chase, bef_chase_rgba, chase_crop_box, (86, 86), add_rgb=(220, 90, 90))
        master.paste(sil_bef, (bx0 + 16, by0 + 50))
        mdraw.text((bx0 + 16, by0 + 382), "Chase Cam (7.0 m, 16°)", fill=(185, 194, 208), font=f_mono_sm)
        mdraw.text((bx0 + 312, by0 + 382), "3/4 Mount Close-Up", fill=(185, 194, 208), font=f_mono_sm)

        if uid in ("a", "b", "c", "d"):
            # --- COL 2: AFTER — SLIMMED VARIANT A (ROUND 2) ---
            cx0, cy0, cx1, cy1 = 1160, ry + 10, 1980, ry + row_h - 20
            vm = um["A_slim"]
            r1_pct = um["A_r1_contour_pct"]
            mdraw.rectangle([cx0, cy0, cx1, cy1], fill=(28, 31, 39), outline=(255, 181, 71), width=2)
            mdraw.rectangle([cx0, cy0, cx1, cy0 + 38], fill=(62, 48, 26))
            mdraw.text((cx0 + 14, cy0 + 8), f"AFTER — SLIMMED PICK A ★ (ROUND 2)", fill=(255, 188, 82), font=f_h3)
            metric_str = f"7m Chase Sil: +{vm['new_contour_chase_pct']:.1f}% (was +{r1_pct:.1f}% in R1)  |  3/4: +{vm['new_contour_closeup_pct']:.1f}%"
            mdraw.text((cx0 + 345, cy0 + 10), metric_str, fill=(242, 244, 248), font=f_mono_sm)

            r2_chase_rgba = load_rgba(f"{uid}_A_r2_chase.png")
            r2_close_rgba = load_rgba(f"{uid}_A_r2_closeup.png")
            r1_chase_rgba = load_rgba(f"{uid}_A_chase.png")

            img_r2_chase = render_on_studio_bg(r2_chase_rgba, chase_crop_box, (380, 360), draw_grid=True)
            img_r2_close = render_on_studio_bg(r2_close_rgba, None, (412, 360), draw_grid=False)
            master.paste(img_r2_chase, (cx0 + 10, cy0 + 44))
            master.paste(img_r2_close, (cx0 + 398, cy0 + 44))

            sil_r2 = make_silhouette_diff_inset(
                base_clean_chase,
                r2_chase_rgba,
                chase_crop_box,
                (108, 108),
                add_rgb=(255, 181, 71),
                r1_rgba=r1_chase_rgba,
            )
            master.paste(sil_r2, (cx0 + 16, cy0 + 50))
            mdraw.rectangle([cx0 + 16, cy0 + 160, cx0 + 196, cy0 + 182], fill=(20, 22, 28))
            mdraw.text(
                (cx0 + 20, cy0 + 162),
                f"+{vm['new_contour_chase_pct']:.1f}% 7m (R1: +{r1_pct:.1f}%)",
                fill=(255, 188, 82),
                font=f_mono_sm,
            )
            mdraw.text((cx0 + 16, cy0 + 382), f"Chase Cam (7.0 m, 16°) — Slim Pick A", fill=(215, 222, 234), font=f_mono_sm)
            mdraw.text((cx0 + 406, cy0 + 382), f"3/4 Close-Up — Thinner Brackets & Smaller Pods (-35..40% Vol)", fill=(215, 222, 234), font=f_mono_sm)

            # --- COL 3: ROUND 1 (BULKY) vs ROUND 2 (SLIM) REFERENCE ---
            rx0, ry0, rx1, ry1 = 1992, ry + 10, 2624, ry + row_h - 20
            mdraw.rectangle([rx0, ry0, rx1, ry1], fill=(26, 30, 38), outline=(92, 148, 165), width=1)
            mdraw.rectangle([rx0, ry0, rx1, ry0 + 38], fill=(32, 46, 56))
            mdraw.text((rx0 + 14, ry0 + 8), "SLIM-DOWN PROOF (R1 Bulky -> R2 Slim)", fill=(130, 218, 232), font=f_h3)
            mdraw.text(
                (rx0 + 375, ry0 + 10),
                f"-{vm['silhouette_reduction_vs_r1_pct']:.0f}% Silhouette Bulk",
                fill=(150, 235, 195),
                font=f_mono_sm,
            )

            img_r1_ref = render_on_studio_bg(r1_chase_rgba, chase_crop_box, (298, 360), draw_grid=True)
            img_r2_ref = render_on_studio_bg(r2_chase_rgba, chase_crop_box, (298, 360), draw_grid=True)
            master.paste(img_r1_ref, (rx0 + 10, ry0 + 44))
            master.paste(img_r2_ref, (rx0 + 318, ry0 + 44))

            mdraw.rectangle([rx0 + 16, ry0 + 52, rx0 + 235, ry0 + 76], fill=(48, 28, 28))
            mdraw.text((rx0 + 22, ry0 + 55), f"Round 1 A: +{r1_pct:.1f}% (Bulky)", fill=(245, 155, 145), font=f_mono_sm)
            mdraw.rectangle([rx0 + 324, ry0 + 52, rx0 + 555, ry0 + 76], fill=(24, 52, 42))
            mdraw.text((rx0 + 330, ry0 + 55), f"Round 2 Slim A: +{vm['new_contour_chase_pct']:.1f}% ★", fill=(140, 235, 190), font=f_mono_sm)

            mdraw.text((rx0 + 16, ry0 + 382), "Round 1 Pick A (Before Slimming)", fill=(185, 194, 208), font=f_mono_sm)
            mdraw.text((rx0 + 324, ry0 + 382), "Round 2 Slimmed Pick A (-35..40% Vol)", fill=(140, 235, 190), font=f_mono_sm)
        else:
            # Row (e): 2 NEW VARIANTS AT THE CHARGING CONTACT (FRONT LOWER BUMPER vs LOWER FLANK PORT)
            for v_letter, cx0, cx1, is_rec, v_sub in [
                ("A", 1160, 1888, True, "Fold-Down Front-Bumper Hex Shoe + Side Teal Lamp"),
                ("B", 1900, 2624, False, "Compact Lower-Flank Clip-On Hex Adapter Puck"),
            ]:
                cy0, cy1 = ry + 10, ry + row_h - 20
                vm = um[v_letter]
                border_col = (72, 215, 228) if is_rec else (110, 122, 142)
                hdr_bg = (24, 56, 64) if is_rec else (38, 44, 56)
                badge_col = (95, 232, 245) if is_rec else (180, 210, 225)

                mdraw.rectangle([cx0, cy0, cx1, cy1], fill=(28, 31, 39), outline=border_col, width=2 if is_rec else 1)
                mdraw.rectangle([cx0, cy0, cx1, cy0 + 38], fill=hdr_bg)

                v_hdr = f"NEW VARIANT {v_letter} ★ RECOMMENDED" if is_rec else f"NEW VARIANT {v_letter} (ALTERNATIVE)"
                mdraw.text((cx0 + 14, cy0 + 8), v_hdr, fill=badge_col, font=f_h3)

                metric_str = f"Chase Sil: +{vm['new_contour_chase_pct']:.1f}% | Low 3/4: +{vm['new_contour_closeup_pct']:.1f}%"
                mdraw.text((cx0 + 340, cy0 + 10), metric_str, fill=(242, 244, 248), font=f_mono_sm)

                v_chase_rgba = load_rgba(f"e_{v_letter}_r2_chase.png")
                v_close_rgba = load_rgba(f"e_{v_letter}_r2_closeup.png")

                img_v_chase = render_on_studio_bg(v_chase_rgba, chase_crop_box, (336, 360), draw_grid=True)
                img_v_close = render_on_studio_bg(v_close_rgba, None, (362, 360), draw_grid=False)
                master.paste(img_v_chase, (cx0 + 10, cy0 + 44))
                master.paste(img_v_close, (cx0 + 354, cy0 + 44))

                sil_v = make_silhouette_diff_inset(
                    base_clean_chase,
                    v_chase_rgba,
                    chase_crop_box,
                    (104, 104),
                    add_rgb=(72, 215, 228) if is_rec else (255, 181, 71),
                )
                master.paste(sil_v, (cx0 + 16, cy0 + 50))
                mdraw.rectangle([cx0 + 16, cy0 + 156, cx0 + 178, cy0 + 178], fill=(20, 22, 28))
                mdraw.text(
                    (cx0 + 20, cy0 + 158),
                    f"+{vm['new_contour_chase_pct']:.1f}% Belly/Side",
                    fill=badge_col,
                    font=f_mono_sm,
                )

                mdraw.text((cx0 + 16, cy0 + 382), f"Chase Cam (7.0 m) — Clean Rear!", fill=(215, 222, 234), font=f_mono_sm)
                mdraw.text((cx0 + 354, cy0 + 382), v_sub, fill=badge_col, font=f_mono_sm)

        row_crop = master.crop((0, ry - 4, sheet_w, ry + row_h - 4))
        row_crop.save(captures_dir / f"upgrade_{uid}_r2.png")

    # ==========================================================================
    # BOTTOM ROW ON ROUND 2 MASTER SHEET: NEW SLIM FULLY GROWN 07
    # ==========================================================================
    fy = header_h + 5 * row_h + 4
    fg_m = metrics_r2["full_growth_r2"]
    mdraw.rectangle([12, fy, sheet_w - 12, fy + fg_row_h - 8], fill=(25, 30, 38), outline=(72, 215, 228), width=3)
    mdraw.rectangle([12, fy, sheet_w - 12, fy + 46], fill=(24, 52, 60))
    mdraw.text(
        (28, fy + 10),
        "ROUND 2 FULLY GROWN 07 — SLIM a–d + NEW UNDER-BUMPER HEX PLUG (e_A)  [SMALL, CUTE, CALM — NOT BULKY!]",
        fill=(110, 232, 245),
        font=f_h2,
    )
    mdraw.text(
        (1355, fy + 13),
        f"7m Chase Contour: +{fg_m['new_contour_chase_pct']:.1f}% (down from +{fg_m['r1_new_contour_chase_pct']:.1f}% in Round 1!)  |  Front 3/4: +{fg_m['net_front3q_pct']:.1f}%  |  Rear 3/4: +{fg_m['net_rear3q_pct']:.1f}%",
        fill=(245, 247, 250),
        font=f_mono,
    )

    p_y0 = fy + 56
    p_h = 430

    p1 = render_on_studio_bg(base_clean_chase, chase_crop_box, (420, p_h), draw_grid=True)
    p2 = render_on_studio_bg(fg_r1_chase, chase_crop_box, (440, p_h), draw_grid=True)
    p3 = render_on_studio_bg(fg_r2_chase, chase_crop_box, (520, p_h), draw_grid=True)
    p4 = render_on_studio_bg(fg_r2_f3q, None, (580, p_h), draw_grid=False)
    p5 = render_on_studio_bg(fg_r2_r3q, None, (580, p_h), draw_grid=False)

    master.paste(p1, (24, p_y0))
    master.paste(p2, (456, p_y0))
    master.paste(p3, (908, p_y0))
    master.paste(p4, (1440, p_y0))
    master.paste(p5, (2032, p_y0))

    sil_fg_r2 = make_silhouette_diff_inset(
        base_clean_chase,
        fg_r2_chase,
        chase_crop_box,
        (124, 124),
        add_rgb=(255, 181, 71),
        r1_rgba=fg_r1_chase,
    )
    master.paste(sil_fg_r2, (918, p_y0 + 10))
    mdraw.rectangle([918, p_y0 + 138, 1120, p_y0 + 162], fill=(20, 22, 28))
    mdraw.text((924, p_y0 + 141), f"+{fg_m['new_contour_chase_pct']:.1f}% R2 (R1: +{fg_m['r1_new_contour_chase_pct']:.1f}%)", fill=(255, 188, 82), font=f_mono_sm)

    mdraw.text((30, p_y0 + p_h + 8), "1. BEFORE — Base R1 07 (7.0 m Chase)", fill=(195, 204, 218), font=f_mono_sm)
    mdraw.text((462, p_y0 + p_h + 8), f"2. ROUND 1 (+{fg_m['r1_new_contour_chase_pct']:.1f}% — Too Bulky, Hex on Butt)", fill=(235, 145, 135), font=f_mono_sm)
    mdraw.text((914, p_y0 + p_h + 8), f"3. ROUND 2 SLIM ★ (+{fg_m['new_contour_chase_pct']:.1f}% — Cute, Calm, Not Bulky)", fill=(120, 235, 195), font=f_mono_sm)
    mdraw.text((1446, p_y0 + p_h + 8), "4. ROUND 2 FRONT 3/4 — Under-Bumper Hex Plug & Open CRT Face", fill=(255, 195, 92), font=f_mono_sm)
    mdraw.text((2038, p_y0 + p_h + 8), "5. ROUND 2 REAR 3/4 — Clean Rear Bumper (No Hex on 07's Butt!)", fill=(255, 195, 92), font=f_mono_sm)

    master_r2_path = captures_dir / "upgrades_sheet_r2.png"
    master.save(master_r2_path)
    print("Saved Round 2 master contact sheet:", master_r2_path)

    # ==========================================================================
    # BUILD DEDICATED HIGH-RES ROUND 2 FULL-GROWTH SHEET: Logs/captures/full_growth_r2.png
    # ==========================================================================
    fg_w, fg_h = 2280, 1340
    fg_sheet = Image.new("RGB", (fg_w, fg_h), (18, 20, 25))
    fdraw = ImageDraw.Draw(fg_sheet)

    fdraw.rectangle([0, 0, fg_w, 128], fill=(24, 34, 42), outline=(72, 215, 228), width=2)
    fdraw.rectangle([0, 0, 12, 128], fill=(72, 215, 228))
    fdraw.text(
        (28, 16),
        "ROUND 2 FULLY GROWN 07 — SLIMMED UPGRADES (a–d) + UNDER-BUMPER HEX CONTACT SHOE (e_A)",
        fill=(246, 240, 228),
        font=f_h1,
    )
    fdraw.text(
        (28, 58),
        f"Small, Cute, Calm 07 Preserved:  +{fg_m['new_contour_chase_pct']:.1f}% 7m Chase Contour (Slimmed from +{fg_m['r1_new_contour_chase_pct']:.1f}% in R1!)  |  Flank Pods Inside Wheel Track  |  Zero Hex on 07's Butt",
        fill=(110, 232, 245),
        font=f_h3,
    )
    fdraw.text(
        (28, 92),
        "Z1 Dorsal: (a_A Slim) Ø 0.33m Dish & Bridge   |   Z2 Bonnet: (b_A Slim) Tucked Shoulder Coils   |   "
        "Z3 Visor: (c_A Slim) Compact SSR-3 Floodlights   |   Z4 Flanks: (d_A Slim) Tight Saddle-Pods   |   Z5 Front Bumper/Belly: (e_A New) Fold-Down Hex Contact Plug + Side Teal Lamp",
        fill=(198, 208, 222),
        font=f_small,
    )

    cols = [
        ("7.0 m CHASE CAMERA (16° PITCH, 52° FOV)", base_clean_chase, fg_r2_chase, chase_crop_box, True),
        ("FRONT 3/4 HERO VIEW (UNDER-BUMPER HEX PLUG & CALM FACE)", fb_f3q, fg_r2_f3q, None, False),
        ("REAR 3/4 HERO VIEW (CLEAN REAR — NO HEX ON 07'S BUTT!)", fb_r3q, fg_r2_r3q, None, False),
    ]
    col_w = 736
    for ci, (ctitle, b_img, a_img, cbox, is_chase) in enumerate(cols):
        cx = 20 + ci * (col_w + 16)
        cy = 142
        ch = 1176
        fdraw.rectangle([cx, cy, cx + col_w, cy + ch], fill=(25, 28, 36), outline=(72, 80, 96), width=2)
        fdraw.rectangle([cx, cy, cx + col_w, cy + 40], fill=(34, 45, 56))
        fdraw.text((cx + 16, cy + 9), ctitle, fill=(244, 238, 225), font=f_h3)

        sub_w, sub_h = col_w - 24, 510
        im_before = render_on_studio_bg(b_img, cbox, (sub_w, sub_h), draw_grid=is_chase)
        fg_sheet.paste(im_before, (cx + 12, cy + 48))
        fdraw.rectangle([cx + 20, cy + 56, cx + 310, cy + 88], fill=(20, 22, 28))
        fdraw.text((cx + 28, cy + 62), "BEFORE: Base R1 07 (Unmodified)", fill=(195, 204, 218), font=f_mono)

        im_after = render_on_studio_bg(a_img, cbox, (sub_w, sub_h), draw_grid=is_chase)
        fg_sheet.paste(im_after, (cx + 12, cy + 610))
        fdraw.rectangle([cx + 20, cy + 618, cx + 520, cy + 650], fill=(22, 50, 58), outline=(72, 215, 228), width=1)
        fdraw.text((cx + 28, cy + 624), "AFTER: Round 2 Slim Full Growth (a–d Slim + e_A Plug)", fill=(110, 232, 245), font=f_mono)

        if is_chase:
            sil_inset = make_silhouette_diff_inset(
                b_img,
                a_img,
                cbox,
                (164, 164),
                add_rgb=(255, 181, 71),
                r1_rgba=fg_r1_chase,
            )
            fg_sheet.paste(sil_inset, (cx + sub_w - 160, cy + 622))
            fdraw.rectangle([cx + sub_w - 160, cy + 788, cx + sub_w + 4, cy + 812], fill=(20, 22, 28))
            fdraw.text(
                (cx + sub_w - 154, cy + 791),
                f"+{fg_m['new_contour_chase_pct']:.1f}% R2 (R1: +{fg_m['r1_new_contour_chase_pct']:.1f}%)",
                fill=(255, 188, 82),
                font=f_mono_sm,
            )

        if ci == 0:
            footer_txt = "7.0 m Chase: Every upgrade reads clearly (6–8% each), flank pods tuck inside the tyre stance, and under-belly hex contact shoe + side teal lamps peek cleanly below."
        elif ci == 1:
            footer_txt = "Front 3/4: Fold-down Tycho hex contact shoe + Teal collar & side Teal status lamp tuck neatly under the front bumper where 07 meets the charging pad."
        else:
            footer_txt = "Rear 3/4: 07's rear bumper, hatch & winch are 100% clean (no hex on 07's butt!). Slimmed flank pods, visor lamps & dish mast keep 07 small, cute & calm."
        y_ft = cy + 1130
        for line in wrap_text(fdraw, footer_txt, f_small, col_w - 28):
            fdraw.text((cx + 14, y_ft), line, fill=(210, 218, 230), font=f_small)
            y_ft += 18

    fg_r2_path = captures_dir / "full_growth_r2.png"
    fg_sheet.save(fg_r2_path)
    print("Saved Round 2 full growth sheet:", fg_r2_path)


def main():
    repo_root = SCRIPT_DIR.parents[3]
    if not IN_BLENDER:
        if "--r1" in sys.argv:
            compose_contact_sheets(repo_root)
        else:
            compose_contact_sheets_r2(repo_root)
        return

    captures_dir = repo_root / "Logs" / "captures"
    raw_dir = captures_dir / "_raw"
    captures_dir.mkdir(parents=True, exist_ok=True)
    raw_dir.mkdir(parents=True, exist_ok=True)

    reset_scene()
    mats = create_material_library(CONCEPT_MATERIALS, for_render=False)
    rover_root = build_rover_model_hierarchy(DIMENSIONS, mats)

    # Bake vertex colors on base RoverModel so AO (R) and Edge Wear (G) are crisp and bright
    rv_meshes = [o for o in bpy.data.objects if o.type == "MESH" and not o.name.startswith("Decal_KA_")]
    bake_vertex_colors(rv_meshes, rover_cfg=DIMENSIONS, include_ground_occluder=True)

    # Build Round 2 concept variant meshes (plus Round 1 meshes if missing) parented under rover_root
    variant_objs = {
        "a_A_r2": build_upgrade_a_variant_A_r2(rover_root, mats),
        "b_A_r2": build_upgrade_b_variant_A_r2(rover_root, mats),
        "c_A_r2": build_upgrade_c_variant_A_r2(rover_root, mats),
        "d_A_r2": build_upgrade_d_variant_A_r2(rover_root, mats),
        "e_A_r2": build_upgrade_e_variant_A_r2(rover_root, mats),
        "e_B_r2": build_upgrade_e_variant_B_r2(rover_root, mats),
    }
    for v_obj in variant_objs.values():
        bake_vertex_colors([v_obj], rover_cfg=DIMENSIONS, include_ground_occluder=False)

    # Configure procedural CRT Screen shader + vertex-color shading
    configure_procedural_crt_screen_shader(mats["R1_Screen"], CONCEPT_MATERIALS["R1_Screen"])
    enhance_materials_with_vertex_color_for_render(mats)
    # Restore bright emissive Teal Status Lamp shader (enhance_materials_with_vertex_color_for_render only skips R1_Screen/R1_LampAmber)
    if "R1_TealLamp" in mats:
        tl_mat = mats["R1_TealLamp"]
        if tl_mat.use_nodes and tl_mat.node_tree:
            bsdf = next((n for n in tl_mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if bsdf:
                for link in list(tl_mat.node_tree.links):
                    if link.to_node == bsdf and link.to_socket.name == "Base Color":
                        tl_mat.node_tree.links.remove(link)
                bsdf.inputs["Base Color"].default_value = (0.34, 0.86, 0.92, 1.0)
                if "Emission Color" in bsdf.inputs:
                    bsdf.inputs["Emission Color"].default_value = (0.34, 0.86, 0.92, 1.0)
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value = 4.2
    cam_obj, _ = setup_studio_lighting_and_camera(DIMENSIONS)
    cam_obj.data.type = "PERSP"

    def hide_all_variants():
        for v_obj in variant_objs.values():
            v_obj.hide_render = True
            v_obj.hide_viewport = True

    # Camera definitions:
    chase_cam_pos = (-1.15, 2.89, -6.63)
    chase_target_pos = (0.0, 0.96, 0.0)
    chase_lens_mm = 34.8

    # Close-up 3/4 cameras per upgrade (with (e) now framing the Front Lower Bumper & Lower Flank Charge Port!)
    closeup_cameras_r2 = {
        "a": ((-1.85, 1.85, -2.05), (0.05, 1.44, -0.46), 44.0),   # Rear-3/4 framing dorsal shoulders & slimmed dish mast
        "b": ((-1.45, 1.25, 1.85), (0.0, 0.82, 0.55), 48.0),      # Front-3/4 framing cutter turret & slimmed bonnet saddle
        "c": ((-1.55, 1.58, 1.75), (0.0, 1.38, 0.22), 46.0),      # Front-3/4 framing CRT Head visor & slimmed SSR-3 floodlights
        "d": ((-1.85, 1.38, -1.75), (0.0, 0.88, -0.22), 45.0),    # Rear-side 3/4 framing slimmed flank saddle-pods
        "e": ((-1.72, 0.72, 1.08), (-0.22, 0.38, 0.30), 36.0),    # Low Front-Side 3/4 framing Front Lower Bumper & Lower Flank Charge Port!
    }

    render_res = 960
    hide_all_variants()

    # Render Before close-up for (e) from the new Low Front-Side 3/4 camera (and any missing before images)
    _configure_baseline_rover_state("base")
    c_pos, t_pos, lens_mm = closeup_cameras_r2["e"]
    cam_obj.data.lens = lens_mm
    aim_camera_at(cam_obj, c_pos, t_pos)
    render_to_file(raw_dir / "e_before_r2_closeup.png", 768, 640, transparent=True)

    # Render Slimmed Pick A for (a)..(d)
    for upg_id in ("a", "b", "c", "d"):
        key = f"{upg_id}_A_r2"
        hide_all_variants()
        _configure_baseline_rover_state(key)
        variant_objs[key].hide_render = False
        variant_objs[key].hide_viewport = False
        bpy.context.view_layer.update()

        cam_obj.data.lens = chase_lens_mm
        aim_camera_at(cam_obj, chase_cam_pos, chase_target_pos)
        render_to_file(raw_dir / f"{key}_chase.png", render_res, render_res, transparent=True)

        c_pos, t_pos, lens_mm = closeup_cameras_r2[upg_id]
        cam_obj.data.lens = lens_mm
        aim_camera_at(cam_obj, c_pos, t_pos)
        render_to_file(raw_dir / f"{key}_closeup.png", 768, 640, transparent=True)

    # Render Redesigned (e) Variant A and Variant B
    for var_letter in ("A", "B"):
        key = f"e_{var_letter}_r2"
        hide_all_variants()
        _configure_baseline_rover_state("base")
        variant_objs[key].hide_render = False
        variant_objs[key].hide_viewport = False
        bpy.context.view_layer.update()

        cam_obj.data.lens = chase_lens_mm
        aim_camera_at(cam_obj, chase_cam_pos, chase_target_pos)
        render_to_file(raw_dir / f"{key}_chase.png", render_res, render_res, transparent=True)

        c_pos, t_pos, lens_mm = closeup_cameras_r2["e"]
        cam_obj.data.lens = lens_mm
        aim_camera_at(cam_obj, c_pos, t_pos)
        render_to_file(raw_dir / f"{key}_closeup.png", 768, 640, transparent=True)

    # Render ROUND 2 FULLY GROWN 07 (Slim a_A + b_A + c_A + d_A + New Recommended e_A)
    hide_all_variants()
    _configure_baseline_rover_state("full_growth_r2")
    for k in ("a_A_r2", "b_A_r2", "c_A_r2", "d_A_r2", "e_A_r2"):
        variant_objs[k].hide_render = False
        variant_objs[k].hide_viewport = False
    bpy.context.view_layer.update()

    cam_obj.data.lens = chase_lens_mm
    aim_camera_at(cam_obj, chase_cam_pos, chase_target_pos)
    render_to_file(raw_dir / "full_after_r2_chase.png", render_res, render_res, transparent=True)

    cam_obj.data.lens = 38.0
    aim_camera_at(cam_obj, (-2.35, 1.62, 2.55), (0.0, 1.12, 0.02))
    render_to_file(raw_dir / "full_after_r2_front3q.png", 960, 800, transparent=True)

    aim_camera_at(cam_obj, (-2.30, 1.68, -2.45), (0.0, 1.12, -0.15))
    render_to_file(raw_dir / "full_after_r2_rear3q.png", 960, 800, transparent=True)

    print("All Round 2 raw concept renders completed in:", raw_dir)
    subprocess.run(["python", str(Path(__file__).resolve()), "--compose-only"], check=True)


if __name__ == "__main__":
    main()
