"""
R1 "Stylized Realism" BMesh geometry builders for 07 v3's 10 upgrade kits:
  1. Kit_LampBar        (HeadlampSocket)
  2. Kit_CapacitorDrum  (DrumSocket_L, DrumSocket_R)
  3. Kit_BatterySpine   (SpineSocket)
  4. Kit_TetherRing     (RingSocket)
  5. Kit_CargoRack      (CargoSocket)
  6. Kit_SideBin        (BinSocket_L, BinSocket_R)
  7. HoverCoils         (CoilSocket)
  8. FocusLens          (LensSocket)
  9. LensGlass          (Eye)
 10. ScanFan            (ScanFanSocket)

Every builder produces 100% closed manifold geometry (~1.3k-2.6k triangles per kit),
uses the R1_* material palette, bakes vertex colors (R=AO, G=wear, B=cavity, A=1),
and preserves every node name animated or referenced by RoverKit, RoverHoverCoils,
RoverController, and RoverBodyLanguage.
"""
from __future__ import annotations

import math
import bmesh
import bpy

import sys
from pathlib import Path

_BLENDER_DIR = Path(__file__).resolve().parent.parent
if str(_BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(_BLENDER_DIR))

from r1kit import (
    FULL_CIRCLE_RAD,
    HALF_PI_RAD as HALF_PI,
    append_beveled_box,
    append_cylinder,
    append_helical_coil,
    append_hex_bolt,
    append_lathe_profile,
    append_prism_extrusion,
    append_spherical_dome,
    append_torus,
    bake_vertex_colors,
    count_model_stats,
    create_empty,
    create_mesh_node,
    export_fbx,
    reset_scene,
)
from rover_parts import create_material_library
from build_rover import DIMENSIONS, MATERIALS


# ==============================================================================
# CLOSED-MANIFOLD MICRO-HARDWARE HELPERS
# ==============================================================================

def append_closed_bolt(
    bm,
    radius,
    height,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    include_washer=True,
    segments=6,
):
    """Append a 100% closed-manifold hex bolt + optional washer (pole-capped at both ends)."""
    append_hex_bolt(
        bm,
        radius,
        height,
        axis=axis,
        center_xyz=center_xyz,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
        include_washer=include_washer,
        closed_bottom=True,
        segments=segments,
    )


def append_closed_dome(
    bm,
    radius,
    dome_height,
    collar_depth=0.008,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    segments=24,
    rings=5,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
):
    """Append a 100% closed-manifold convex lens/lamp dome with a flat back cap."""
    append_spherical_dome(
        bm,
        radius,
        dome_height,
        axis=axis,
        center_xyz=center_xyz,
        segments=segments,
        rings=rings,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
        collar_depth=collar_depth,
        closed_bottom=True,
    )


# ==============================================================================
# 1. KIT_LAMPBAR (HeadlampSocket at (0.0, 0.66, 0.62))
#    Child nodes: Lamp_0, Lamp_1, Lamp_2
# ==============================================================================

def build_kit_lamp_bar_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_LampBar: deep cast-brass projector shrouds with radial copper cooling fins
    and dark-charcoal guard hoops over both front headlamps (x = +/-0.26 m), linked beneath
    the chest cutter by a tubular steel brush-guard bar with a central auxiliary fog lamp.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # Material indices for Kit_LampBar body:
    # 0: R1_GreyMetal, 1: R1_Brass, 2: R1_Copper, 3: R1_BareSteel, 4: R1_OrangeStripe, 5: R1_Rubber
    mat_list = [
        mats["R1_GreyMetal"],
        mats["R1_Brass"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
        mats["R1_Rubber"],
    ]

    lamp_x = 0.26
    lamp_y = -0.055
    lamp_z = 0.165
    bar_y = -0.132
    bar_z = 0.178

    # 1. Main horizontal dark-charcoal tubular brush-guard bar below chest cutter
    append_cylinder(
        bm,
        radius1=0.015,
        radius2=0.015,
        length=0.68,
        axis="X",
        center_xyz=(0.0, bar_y, bar_z),
        segments=14,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )
    # Secondary lower skid-brace bar
    append_cylinder(
        bm,
        radius1=0.011,
        radius2=0.011,
        length=0.48,
        axis="X",
        center_xyz=(0.0, bar_y - 0.032, bar_z - 0.016),
        segments=12,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=3,
    )

    # 2. Left & right chassis mounting brackets, projector barrels, copper fins & guard hoops
    for sx in (-1.0, 1.0):
        lx = sx * lamp_x
        # Heavy dark-charcoal mounting stanchions connecting brush-guard bar to nose hull
        append_beveled_box(
            bm,
            size_xyz=(0.038, 0.056, 0.088),
            center_xyz=(sx * 0.20, bar_y - 0.008, bar_z - 0.038),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,
        )
        # Orange safety accent band on each stanchion
        append_beveled_box(
            bm,
            size_xyz=(0.042, 0.018, 0.036),
            center_xyz=(sx * 0.20, bar_y + 0.004, bar_z - 0.012),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,
        )
        # Vertical riser strut from brush-guard bar up to headlamp shroud
        append_beveled_box(
            bm,
            size_xyz=(0.024, 0.068, 0.026),
            center_xyz=(lx, (lamp_y + bar_y) * 0.5 - 0.018, bar_z - 0.008),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=0,
        )

        # Coaxial cast-brass projector shroud collar with radial copper cooling fins (closed lathe)
        shroud_profile = [
            (0.0, -0.036),
            (0.088, -0.036),
            (0.104, -0.030),
            (0.104, -0.012),
            (0.112, -0.006),
            (0.112, 0.022),
            (0.105, 0.036),
            (0.084, 0.036),
            (0.078, 0.016),
            (0.0, 0.016),
        ]
        append_lathe_profile(
            bm,
            shroud_profile,
            axis="Z",
            center_xyz=(lx, lamp_y, lamp_z),
            segments=16,
            mat_idx=1,
            ring_mat_indices=[5, 0, 2, 1, 1, 1, 3, 0, 0],
            auto_chamfer=False,
        )

        # Copper heatsink fin ring around the projector barrel
        for f_i, fz in enumerate((-0.015,)):
            append_torus(
                bm,
                major_radius=0.108,
                minor_radius=0.006,
                axis="Z",
                center_xyz=(lx, lamp_y, lamp_z + fz),
                major_seg=16,
                minor_seg=6,
                mat_idx=2,
            )

        # Outer tubular dark-charcoal brush-guard ring framing each headlamp
        append_torus(
            bm,
            major_radius=0.114,
            minor_radius=0.0095,
            axis="Z",
            center_xyz=(lx, lamp_y, lamp_z + 0.032),
            major_seg=16,
            minor_seg=6,
            mat_idx=0,
        )

        # Horizontal stone-guard grille bars across the front of the headlamp hoop
        for gy in (-0.036, 0.036):
            append_cylinder(
                bm,
                radius1=0.0045,
                radius2=0.0045,
                length=0.195,
                axis="X",
                center_xyz=(lx, lamp_y + gy, lamp_z + 0.038),
                segments=8,
                bevel_width=0.001,
                bevel_segments=1,
                mat_idx=3,
            )

        # 2 brass hex bolts on each mounting flange
        for by in (-0.016, 0.016):
            append_closed_bolt(
                bm,
                radius=0.006,
                height=0.008,
                axis="X",
                center_xyz=(sx * 0.222, bar_y - 0.008 + by, bar_z - 0.035),
                mat_idx=1,
            )

    # 3. Central auxiliary fog-lamp housing on the middle of the brush-guard bar
    fog_profile = [
        (0.0, -0.028),
        (0.048, -0.028),
        (0.058, -0.018),
        (0.062, 0.014),
        (0.056, 0.026),
        (0.044, 0.026),
        (0.0, 0.012),
    ]
    append_lathe_profile(
        bm,
        fog_profile,
        axis="Z",
        center_xyz=(0.0, bar_y, bar_z + 0.006),
        segments=14,
        mat_idx=1,
        ring_mat_indices=[0, 0, 2, 1, 3, 0],
        auto_chamfer=False,
    )
    # Protective top visor hood over the central fog lamp
    append_beveled_box(
        bm,
        size_xyz=(0.115, 0.018, 0.052),
        center_xyz=(0.0, bar_y + 0.056, bar_z + 0.014),
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )

    root_obj = create_mesh_node(
        "Kit_LampBar",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )

    # 4. Required child lamp meshes: Lamp_0 (left), Lamp_1 (center), Lamp_2 (right)
    lamp_specs = (
        ("Lamp_0", (-lamp_x, lamp_y, lamp_z + 0.024), 0.078, 0.022),
        ("Lamp_1", (0.0, bar_y, bar_z + 0.024), 0.045, 0.016),
        ("Lamp_2", (lamp_x, lamp_y, lamp_z + 0.024), 0.078, 0.022),
    )
    for l_name, l_pos, l_rad, l_dome in lamp_specs:
        bm_lamp = bmesh.new()
        append_closed_dome(
            bm_lamp,
            radius=l_rad,
            dome_height=l_dome,
            collar_depth=0.006,
            axis="Z",
            center_xyz=(0.0, 0.0, 0.0),
            segments=14,
            rings=3,
            mat_idx=0,
        )
        create_mesh_node(
            l_name,
            bm_lamp,
            l_pos,
            parent=root_obj,
            mat_list=[mats["R1_LampAmber"]],
            cfg=cfg,
        )

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 2. KIT_CAPACITORDRUM (DrumSocket_L / DrumSocket_R at (+/-0.46, 0.84, -0.14))
#    Child node: Glow
# ==============================================================================

def build_kit_capacitor_drum_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_CapacitorDrum: horizontal cream-enamel & copper cylindrical capacitor/boost drum
    with dark-charcoal saddle brackets, brass end-caps, copper busbars, and central 'Glow' band.
    Symmetric across Z=0 so DrumSocket_L (rotated 180 deg Y) and DrumSocket_R mirror cleanly.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_CreamEnamel, 1: R1_GreyMetal, 2: R1_Copper, 3: R1_Brass, 4: R1_BareSteel, 5: R1_OrangeStripe, 6: R1_Rubber
    mat_list = [
        mats["R1_CreamEnamel"],
        mats["R1_GreyMetal"],
        mats["R1_Copper"],
        mats["R1_Brass"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
        mats["R1_Rubber"],
    ]

    drum_cx = 0.068
    drum_cy = -0.024
    drum_r = 0.064

    # 1. Main horizontal drum profile along Z (symmetric in Z around 0.0)
    drum_profile = [
        (0.0, -0.192),
        (drum_r * 0.48, -0.192),
        (drum_r * 0.56, -0.180),
        (drum_r * 0.84, -0.180),
        (drum_r * 0.96, -0.164),
        (drum_r * 0.96, -0.136),
        (drum_r * 1.00, -0.130),
        (drum_r * 1.00, -0.036),
        (drum_r * 0.88, -0.030),
        (drum_r * 0.88, 0.030),
        (drum_r * 1.00, 0.036),
        (drum_r * 1.00, 0.130),
        (drum_r * 0.96, 0.136),
        (drum_r * 0.96, 0.164),
        (drum_r * 0.84, 0.180),
        (drum_r * 0.56, 0.180),
        (drum_r * 0.48, 0.192),
        (0.0, 0.192),
    ]
    ring_mats = [4, 3, 3, 2, 2, 3, 0, 1, 1, 1, 0, 3, 2, 2, 3, 3, 4]
    append_lathe_profile(
        bm,
        drum_profile,
        axis="Z",
        center_xyz=(drum_cx, drum_cy, 0.0),
        segments=22,
        mat_idx=0,
        ring_mat_indices=ring_mats,
        auto_chamfer=False,
    )

    # 2. Inboard mounting baseplate & twin saddle clamp straps (at z = +/-0.102 m)
    append_beveled_box(
        bm,
        size_xyz=(0.024, 0.092, 0.280),
        center_xyz=(0.008, drum_cy, 0.0),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )
    for sz in (-1.0, 1.0):
        kz = sz * 0.098
        # Saddle cradle block
        append_beveled_box(
            bm,
            size_xyz=(0.068, 0.084, 0.034),
            center_xyz=(0.034, drum_cy - 0.012, kz),
            bevel_width=0.005,
            bevel_segments=2,
            mat_idx=1,
        )
        # Outer dark-charcoal retaining band around cylinder
        append_torus(
            bm,
            major_radius=drum_r * 1.02,
            minor_radius=0.009,
            axis="Z",
            center_xyz=(drum_cx, drum_cy, kz),
            major_seg=20,
            minor_seg=6,
            mat_idx=1,
        )
        # Warm-orange stripe band near each end of the cream shell
        append_torus(
            bm,
            major_radius=drum_r * 1.005,
            minor_radius=0.006,
            axis="Z",
            center_xyz=(drum_cx, drum_cy, sz * 0.062),
            major_seg=20,
            minor_seg=6,
            mat_idx=5,
        )
        # Copper cooling ring on each end cap
        append_torus(
            bm,
            major_radius=drum_r * 0.96,
            minor_radius=0.0065,
            axis="Z",
            center_xyz=(drum_cx, drum_cy, sz * 0.150),
            major_seg=20,
            minor_seg=6,
            mat_idx=2,
        )
        # Top brass terminal post + hex bolt
        append_closed_bolt(
            bm,
            radius=0.011,
            height=0.018,
            axis="Y",
            center_xyz=(drum_cx, drum_cy + drum_r + 0.008, sz * 0.145),
            mat_idx=3,
        )
        # Outboard steel clamp buckle
        append_beveled_box(
            bm,
            size_xyz=(0.016, 0.028, 0.026),
            center_xyz=(drum_cx + drum_r + 0.006, drum_cy, kz),
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=4,
        )

    # 3. Longitudinal top copper busbar rod connecting the two end-cap terminals
    append_cylinder(
        bm,
        radius1=0.0065,
        radius2=0.0065,
        length=0.290,
        axis="Z",
        center_xyz=(drum_cx, drum_cy + drum_r + 0.012, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=2,
    )

    root_obj = create_mesh_node(
        "Kit_CapacitorDrum",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )

    # 4. Required child node 'Glow' at the center of the drum
    bm_glow = bmesh.new()
    glow_profile = [
        (0.0, -0.032),
        (drum_r * 0.92, -0.032),
        (drum_r * 1.03, -0.024),
        (drum_r * 1.05, 0.0),
        (drum_r * 1.03, 0.024),
        (drum_r * 0.92, 0.032),
        (0.0, 0.032),
    ]
    append_lathe_profile(
        bm_glow,
        glow_profile,
        axis="Z",
        center_xyz=(0.0, 0.0, 0.0),
        segments=22,
        mat_idx=0,
        auto_chamfer=False,
    )
    create_mesh_node(
        "Glow",
        bm_glow,
        (drum_cx, drum_cy, 0.0),
        parent=root_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 3. KIT_BATTERYSPINE (SpineSocket at (0.385, 0.90, -0.20))
# ==============================================================================

def build_kit_battery_spine_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_BatterySpine: upper-deck reserve battery saddle rail housing twin cylindrical
    cream-and-brass cells end-to-end on a finned dark-charcoal heatsink cradle with copper
    busbars and warm-amber charge indicator rings.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_CreamEnamel, 1: R1_GreyMetal, 2: R1_Brass, 3: R1_Copper, 4: R1_BareSteel, 5: R1_LampAmber, 6: R1_OrangeStripe
    mat_list = [
        mats["R1_CreamEnamel"],
        mats["R1_GreyMetal"],
        mats["R1_Brass"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
        mats["R1_LampAmber"],
        mats["R1_OrangeStripe"],
    ]

    rail_cx = -0.008
    cell_cy = 0.046
    cell_r = 0.038

    # 1. Dark-charcoal base cradle rail sitting on the upper deck edge
    append_beveled_box(
        bm,
        size_xyz=(0.088, 0.028, 0.400),
        center_xyz=(rail_cx, 0.010, 0.0),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )

    # 2. 6 outboard heatsink cooling fins along +X and inboard terminal lugs
    for f_i in range(6):
        fz = -0.155 + f_i * 0.062
        append_beveled_box(
            bm,
            size_xyz=(0.022, 0.042, 0.014),
            center_xyz=(rail_cx + 0.044, 0.024, fz),
            bevel_width=0.0025,
            bevel_segments=1,
            mat_idx=1,
        )

    # 3. Continuous coaxial twin-cell cylindrical battery pack along Z
    cell_profile = [
        (0.0, -0.190),
        (cell_r * 0.55, -0.190),
        (cell_r * 0.88, -0.178),
        (cell_r * 0.96, -0.160),
        (cell_r * 1.00, -0.152),
        (cell_r * 1.00, -0.018),
        (cell_r * 0.90, -0.012),
        (cell_r * 0.90, 0.012),
        (cell_r * 1.00, 0.018),
        (cell_r * 1.00, 0.152),
        (cell_r * 0.96, 0.160),
        (cell_r * 0.88, 0.178),
        (cell_r * 0.55, 0.190),
        (0.0, 0.190),
    ]
    cell_ring_mats = [4, 2, 3, 2, 0, 1, 3, 1, 0, 2, 3, 2, 4]
    append_lathe_profile(
        bm,
        cell_profile,
        axis="Z",
        center_xyz=(rail_cx, cell_cy, 0.0),
        segments=20,
        mat_idx=0,
        ring_mat_indices=cell_ring_mats,
        auto_chamfer=False,
    )

    # 4. 3 warm-amber charge indicator rings (matching upgraded.png) + 4 brass clamp straps
    for gz in (-0.095, 0.0, 0.095):
        append_torus(
            bm,
            major_radius=cell_r * 1.02,
            minor_radius=0.0065,
            axis="Z",
            center_xyz=(rail_cx, cell_cy, gz),
            major_seg=20,
            minor_seg=6,
            mat_idx=5,
        )

    for sz in (-0.142, -0.048, 0.048, 0.142):
        append_torus(
            bm,
            major_radius=cell_r * 1.03,
            minor_radius=0.0055,
            axis="Z",
            center_xyz=(rail_cx, cell_cy, sz),
            major_seg=20,
            minor_seg=6,
            mat_idx=2,
        )
        # Saddle block under each brass strap
        append_beveled_box(
            bm,
            size_xyz=(0.076, 0.032, 0.022),
            center_xyz=(rail_cx, 0.024, sz),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=1,
        )

    # 5. Top longitudinal copper busbar + end terminal bolts
    append_beveled_box(
        bm,
        size_xyz=(0.016, 0.010, 0.340),
        center_xyz=(rail_cx, cell_cy + cell_r + 0.006, 0.0),
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=3,
    )
    for bz in (-0.155, 0.0, 0.155):
        append_closed_bolt(
            bm,
            radius=0.008,
            height=0.012,
            axis="Y",
            center_xyz=(rail_cx, cell_cy + cell_r + 0.012, bz),
            mat_idx=4,
        )

    # Orange identification stripe block on the inboard rail face
    append_beveled_box(
        bm,
        size_xyz=(0.012, 0.016, 0.140),
        center_xyz=(rail_cx - 0.042, 0.014, 0.0),
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=6,
    )

    root_obj = create_mesh_node(
        "Kit_BatterySpine",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )
    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 4. KIT_TETHERRING (RingSocket under Head at (0.0, 1.23, 0.04), rot (0, 180, 0))
#    Child node: Glow
# ==============================================================================

def build_kit_tether_ring_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_TetherRing: machined brass & dark-charcoal inductive pulse collar ring
    mounted on the rear of the CRT Head (RingSocket has localRotation = (0, 180, 0),
    so local +Z extends rearward behind Head). Includes child mesh 'Glow'.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_Brass, 1: R1_GreyMetal, 2: R1_Copper, 3: R1_BareSteel, 4: R1_OrangeStripe
    mat_list = [
        mats["R1_Brass"],
        mats["R1_GreyMetal"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
    ]

    ring_y = 0.030
    ring_z = 0.224
    major_r = 0.142

    # 1. Main machined brass & dark-charcoal toroidal collar ring framing the rear cathode housing
    append_torus(
        bm,
        major_radius=major_r,
        minor_radius=0.022,
        axis="Z",
        center_xyz=(0.0, ring_y, ring_z),
        major_seg=24,
        minor_seg=8,
        mat_idx=0,
        scale_xyz=(1.76, 1.00, 1.0),
    )
    # Inner dark-charcoal shielding bezel ring
    append_torus(
        bm,
        major_radius=major_r * 0.88,
        minor_radius=0.015,
        axis="Z",
        center_xyz=(0.0, ring_y, ring_z - 0.012),
        major_seg=20,
        minor_seg=6,
        mat_idx=1,
        scale_xyz=(1.76, 1.00, 1.0),
    )

    # 2. 4 forward mounting stanchions connecting the collar to the rear CRT housing + copper coils
    stanchion_angles = (
        math.radians(35.0),
        math.radians(145.0),
        math.radians(215.0),
        math.radians(325.0),
    )
    for ang in stanchion_angles:
        cx = major_r * 1.72 * math.cos(ang)
        cy = ring_y + major_r * 0.98 * math.sin(ang)
        # Mounting strut extending from rear head plate out to ring_z
        append_beveled_box(
            bm,
            size_xyz=(0.028, 0.024, 0.064),
            center_xyz=(cx * 0.94, cy, ring_z - 0.026),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,
            rot_euler=(0.0, 0.0, ang),
        )
        # Copper induction bobbin wound around the collar at each quadrant
        append_cylinder(
            bm,
            radius1=0.026,
            radius2=0.026,
            length=0.042,
            axis="Z",
            center_xyz=(cx, cy, ring_z + 0.004),
            segments=12,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=2,
        )
        # Brushed-steel end cap + hex bolt on each bobbin
        append_closed_bolt(
            bm,
            radius=0.012,
            height=0.014,
            axis="Z",
            center_xyz=(cx, cy, ring_z + 0.028),
            mat_idx=3,
        )

    # 3. Left/Right warm-orange telemetry clamp blocks & top/bottom brass bus bridges
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.028, 0.062, 0.038),
            center_xyz=(sx * major_r * 1.78, ring_y, ring_z + 0.004),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,
        )
    for sy in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.082, 0.024, 0.034),
            center_xyz=(0.0, ring_y + sy * major_r * 1.02, ring_z + 0.004),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=3,
        )

    root_obj = create_mesh_node(
        "Kit_TetherRing",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )

    # 4. Required child node 'Glow' (continuous amber inductive pulse ring on the rear face)
    bm_glow = bmesh.new()
    append_torus(
        bm_glow,
        major_radius=major_r * 1.01,
        minor_radius=0.012,
        axis="Z",
        center_xyz=(0.0, 0.0, 0.0),
        major_seg=24,
        minor_seg=8,
        mat_idx=0,
        scale_xyz=(1.76, 1.00, 1.0),
    )
    create_mesh_node(
        "Glow",
        bm_glow,
        (0.0, ring_y, ring_z + 0.020),
        parent=root_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 5. KIT_CARGORACK (CargoSocket at (0.0, 0.48, -0.58))
#    Child node: RelicSeat (empty with localPosition.z < 0)
# ==============================================================================

def build_kit_cargo_rack_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_CargoRack: rear cargo cradle with slatted steel deck, contoured cream-and-orange
    side clamp cheeks, longitudinal fore-and-aft orange tie-down straps, and pivoting tubular
    A-frame hoist bail straddling the cargo hatch. Includes empty child 'RelicSeat'.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_GreyMetal, 1: R1_BareSteel, 2: R1_OrangeStripe, 3: R1_CreamEnamel, 4: R1_Brass, 5: R1_Rubber
    mat_list = [
        mats["R1_GreyMetal"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
        mats["R1_CreamEnamel"],
        mats["R1_Brass"],
        mats["R1_Rubber"],
    ]

    # 1. Left & right longitudinal support rails extending aft from rear bumper (z in [-0.15, -0.46])
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.032, 0.036, 0.310),
            center_xyz=(sx * 0.32, 0.012, -0.305),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,
        )
        # Forward chassis mounting bracket plates + brass bolts at rear face of Body (z = -0.165)
        append_beveled_box(
            bm,
            size_xyz=(0.048, 0.072, 0.036),
            center_xyz=(sx * 0.32, 0.025, -0.172),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,
        )
        append_closed_bolt(
            bm,
            radius=0.009,
            height=0.014,
            axis="X",
            center_xyz=(sx * 0.348, 0.025, -0.172),
            mat_idx=4,
        )

    # 2. 5 transverse brushed-steel slatted floor bars + rear bumper bar (all behind z = -0.165)
    for s_i in range(5):
        sz = -0.195 - s_i * 0.056
        append_beveled_box(
            bm,
            size_xyz=(0.620, 0.018, 0.032),
            center_xyz=(0.0, 0.026, sz),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=1 if s_i % 2 == 0 else 0,
        )

    # Rear transverse tubular guard rail at z = -0.455 m
    append_cylinder(
        bm,
        radius1=0.016,
        radius2=0.016,
        length=0.680,
        axis="X",
        center_xyz=(0.0, 0.048, -0.455),
        segments=14,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )

    # 3. Twin longitudinal fore-and-aft warm-orange tie-down straps with brass tension buckles
    for sx in (-1.0, 1.0):
        sx_pos = sx * 0.145
        append_beveled_box(
            bm,
            size_xyz=(0.038, 0.012, 0.270),
            center_xyz=(sx_pos, 0.038, -0.310),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=2,
        )
        # Rear wrap-around strap hook & brass ratchet buckle
        append_beveled_box(
            bm,
            size_xyz=(0.044, 0.042, 0.026),
            center_xyz=(sx_pos, 0.032, -0.448),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=4,
        )
        # Rubber anti-slip cradle pads
        append_beveled_box(
            bm,
            size_xyz=(0.056, 0.014, 0.085),
            center_xyz=(sx_pos, 0.042, -0.305),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=5,
        )

    # 4. Contoured left & right side retaining cheeks (cream enamel + orange stripe)
    for sx in (-1.0, 1.0):
        append_beveled_box(
            bm,
            size_xyz=(0.030, 0.135, 0.250),
            center_xyz=(sx * 0.342, 0.102, -0.310),
            bevel_width=0.006,
            bevel_segments=2,
            mat_idx=3,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.034, 0.036, 0.225),
            center_xyz=(sx * 0.344, 0.108, -0.310),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=2,
        )
        # Inboard rubber cushion pad on each side clamp
        append_beveled_box(
            bm,
            size_xyz=(0.016, 0.082, 0.180),
            center_xyz=(sx * 0.322, 0.102, -0.310),
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=5,
        )

    # 5. Twin-arm tubular A-Frame Hoist Gantry (HoistBail) straddling outside of CargoHatch (behind z = -0.165)
    for sx in (-1.0, 1.0):
        # Upper gantry pivot hub mounted behind the rear chassis wall at z = -0.195
        append_cylinder(
            bm,
            radius1=0.028,
            radius2=0.028,
            length=0.036,
            axis="X",
            center_xyz=(sx * 0.348, 0.175, -0.195),
            segments=14,
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=4,
        )
        # Slanted tubular gantry upright arm rising from pivot hub to overhead crossbar
        append_beveled_box(
            bm,
            size_xyz=(0.026, 0.235, 0.028),
            center_xyz=(sx * 0.348, 0.285, -0.225),
            bevel_width=0.005,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(math.radians(-16.0), 0.0, 0.0),
        )
        # Rear diagonal strut from side rail up to gantry crown
        append_beveled_box(
            bm,
            size_xyz=(0.022, 0.250, 0.024),
            center_xyz=(sx * 0.344, 0.255, -0.335),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=1,
            rot_euler=(math.radians(24.0), 0.0, 0.0),
        )

    # Overhead gantry crossbar at y = 0.395 m, z = -0.255 m
    append_cylinder(
        bm,
        radius1=0.015,
        radius2=0.015,
        length=0.720,
        axis="X",
        center_xyz=(0.0, 0.395, -0.255),
        segments=14,
        bevel_width=0.004,
        bevel_segments=1,
        mat_idx=0,
    )
    # Central magnetic hoist latch block on the overhead crossbar
    append_beveled_box(
        bm,
        size_xyz=(0.110, 0.038, 0.044),
        center_xyz=(0.0, 0.395, -0.255),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=2,
    )

    root_obj = create_mesh_node(
        "Kit_CargoRack",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )

    # Required empty child node 'RelicSeat' with localPosition.z < 0
    create_empty("RelicSeat", parent=root_obj, pivot_u=(0.0, 0.08, -0.30), local_space=True, empty_size=0.04)

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 6. KIT_SIDEBIN (BinSocket_L / BinSocket_R at (+/-0.46, 0.60, 0.02))
# ==============================================================================

def build_kit_side_bin_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build Kit_SideBin: lower-flank cream-enamel and orange-striped cargo saddle pannier
    with beveled dark-charcoal weatherproof lid, twin steel toggle latches, grab rail,
    and 4 amber slot-indicator pips. Symmetric across Z=0 for BinSocket_L and BinSocket_R.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_CreamEnamel, 1: R1_OrangeStripe, 2: R1_GreyMetal, 3: R1_BareSteel, 4: R1_Brass, 5: R1_Rubber, 6: R1_LampAmber
    mat_list = [
        mats["R1_CreamEnamel"],
        mats["R1_OrangeStripe"],
        mats["R1_GreyMetal"],
        mats["R1_BareSteel"],
        mats["R1_Brass"],
        mats["R1_Rubber"],
        mats["R1_LampAmber"],
    ]

    bin_cx = 0.064
    bin_w = 0.124
    bin_h = 0.148
    bin_l = 0.310

    # 1. Inboard dark-charcoal mounting backplate
    append_beveled_box(
        bm,
        size_xyz=(0.022, bin_h * 0.90, bin_l * 0.92),
        center_xyz=(0.008, 0.0, 0.0),
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=2,
    )

    # 2. Main pillowed cream-enamel pannier tub
    append_beveled_box(
        bm,
        size_xyz=(bin_w, bin_h * 0.82, bin_l),
        center_xyz=(bin_cx, -0.012, 0.0),
        bevel_width=0.012,
        bevel_segments=3,
        mat_idx=0,
        taper_top_xy=(1.02, 1.02),
    )

    # 3. Continuous horizontal warm-orange beltline stripe across outboard & end faces
    append_beveled_box(
        bm,
        size_xyz=(bin_w + 0.006, 0.038, bin_l + 0.006),
        center_xyz=(bin_cx + 0.001, -0.008, 0.0),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )

    # 4. Rubber weather-seal gasket & overhanging dark-charcoal hinged top lid
    append_beveled_box(
        bm,
        size_xyz=(bin_w + 0.008, 0.012, bin_l + 0.008),
        center_xyz=(bin_cx, 0.046, 0.0),
        bevel_width=0.003,
        bevel_segments=2,
        mat_idx=5,
    )
    append_beveled_box(
        bm,
        size_xyz=(bin_w + 0.012, 0.036, bin_l + 0.012),
        center_xyz=(bin_cx + 0.002, 0.066, 0.0),
        bevel_width=0.008,
        bevel_segments=3,
        mat_idx=2,
        taper_top_xy=(0.94, 0.96),
    )

    # 5. Twin vertical dark-charcoal & steel toggle latches at z = +/-0.088 m + brass pins
    for sz in (-1.0, 1.0):
        lz = sz * 0.088
        append_beveled_box(
            bm,
            size_xyz=(0.018, 0.132, 0.028),
            center_xyz=(bin_cx + bin_w * 0.5 + 0.002, 0.004, lz),
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=2,
        )
        append_beveled_box(
            bm,
            size_xyz=(0.020, 0.046, 0.022),
            center_xyz=(bin_cx + bin_w * 0.5 + 0.005, 0.022, lz),
            bevel_width=0.003,
            bevel_segments=2,
            mat_idx=3,
        )
        append_closed_bolt(
            bm,
            radius=0.0065,
            height=0.010,
            axis="X",
            center_xyz=(bin_cx + bin_w * 0.5 + 0.012, 0.052, lz),
            mat_idx=4,
        )
        # Lower dark-charcoal corner bumper guards
        append_beveled_box(
            bm,
            size_xyz=(bin_w + 0.008, 0.024, 0.044),
            center_xyz=(bin_cx, -0.064, sz * (bin_l * 0.5 - 0.018)),
            bevel_width=0.004,
            bevel_segments=2,
            mat_idx=2,
        )

    # 6. Outboard brushed-steel tie-down grab bar along the lower flank
    append_cylinder(
        bm,
        radius1=0.0055,
        radius2=0.0055,
        length=0.150,
        axis="Z",
        center_xyz=(bin_cx + bin_w * 0.5 + 0.012, -0.038, 0.0),
        segments=12,
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=3,
    )

    # 7. 4 recessed warm-amber cargo-slot indicator pips along the upper lid bevel
    for p_i in range(4):
        pz = -0.096 + p_i * 0.064
        append_beveled_box(
            bm,
            size_xyz=(0.012, 0.010, 0.022),
            center_xyz=(bin_cx + bin_w * 0.5 + 0.004, 0.064, pz),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=6,
        )

    root_obj = create_mesh_node(
        "Kit_SideBin",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )
    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 7. HOVERCOILS (CoilSocket at (0.0, 0.28, 0.0))
#    Child nodes: Coil_FL, Coil_FR, Coil_RL, Coil_RR (all geometry local y <= 0)
#    Grandchild nodes: Glow_FL, Glow_FR, Glow_RL, Glow_RR
# ==============================================================================

def build_hover_coils_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build HoverCoils: belly cruciform distribution plate + 4 vertical electromagnetic
    repulsor coil assemblies (Coil_FL, Coil_FR, Coil_RL, Coil_RR) whose local geometry
    sits strictly at or below y = 0 so vertical squash/stretch extends downward.
    Each Coil_<corner> has a child Glow_<corner> ring at its lower tip.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm_plate = bmesh.new()
    # 0: R1_GreyMetal, 1: R1_Brass, 2: R1_Copper, 3: R1_BareSteel, 4: R1_OrangeStripe
    plate_mats = [
        mats["R1_GreyMetal"],
        mats["R1_Brass"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
    ]

    coil_x = 0.255
    coil_z = 0.240
    coil_pivot_y = -0.018

    # 1. Central belly distribution manifold box (y in [-0.026, 0.0])
    append_beveled_box(
        bm_plate,
        size_xyz=(0.240, 0.026, 0.300),
        center_xyz=(0.0, -0.013, 0.0),
        bevel_width=0.005,
        bevel_segments=1,
        mat_idx=0,
    )
    # Central brass inductive junction disc
    append_cylinder(
        bm_plate,
        radius1=0.072,
        radius2=0.064,
        length=0.016,
        axis="Y",
        center_xyz=(0.0, -0.022, 0.0),
        segments=14,
        bevel_width=0.003,
        bevel_segments=1,
        mat_idx=1,
    )

    # 2. 4 diagonal outrigger arms + copper power busbars reaching each corner coil mount
    corners = (
        ("FL", -coil_x, coil_z),
        ("FR", coil_x, coil_z),
        ("RL", -coil_x, -coil_z),
        ("RR", coil_x, -coil_z),
    )
    for tag, cx, cz in corners:
        ang_y = math.atan2(cx, cz)
        arm_len = math.hypot(cx, cz)
        append_beveled_box(
            bm_plate,
            size_xyz=(0.054, 0.022, arm_len),
            center_xyz=(cx * 0.5, -0.011, cz * 0.5),
            bevel_width=0.004,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(0.0, ang_y, 0.0),
        )
        # Copper high-current busbar along the underside of each outrigger arm
        append_beveled_box(
            bm_plate,
            size_xyz=(0.018, 0.010, arm_len * 0.86),
            center_xyz=(cx * 0.5, -0.022, cz * 0.5),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=2,
            rot_euler=(0.0, ang_y, 0.0),
        )
        # Upper mounting collar boss at each corner (y in [-0.022, 0.0])
        append_cylinder(
            bm_plate,
            radius1=0.066,
            radius2=0.066,
            length=0.020,
            axis="Y",
            center_xyz=(cx, -0.010, cz),
            segments=12,
            bevel_width=0.003,
            bevel_segments=1,
            mat_idx=0,
        )

    root_obj = create_mesh_node(
        "HoverCoils",
        bm_plate,
        unity_loc,
        parent=parent,
        mat_list=plate_mats,
        cfg=cfg,
    )

    # 3. 4 corner coil nodes (Coil_FL, Coil_FR, Coil_RL, Coil_RR) with all vertices at local y <= 0.0
    coil_mats = [
        mats["R1_GreyMetal"],
        mats["R1_Copper"],
        mats["R1_Brass"],
        mats["R1_BareSteel"],
    ]
    for tag, cx, cz in corners:
        bm_coil = bmesh.new()
        # Stepped damper core + upper cup + lower brass nozzle (strictly y in [-0.118, 0.0])
        core_profile = [
            (0.0, -0.118),
            (0.058, -0.118),
            (0.064, -0.106),
            (0.058, -0.090),
            (0.035, -0.084),
            (0.035, -0.022),
            (0.060, -0.018),
            (0.062, -0.003),
            (0.0, 0.0),
        ]
        append_lathe_profile(
            bm_coil,
            core_profile,
            axis="Y",
            center_xyz=(0.0, 0.0, 0.0),
            segments=12,
            mat_idx=0,
            ring_mat_indices=[3, 2, 2, 0, 3, 0, 0, 0],
            auto_chamfer=False,
        )
        # Helical copper spring winding (y in [-0.096, -0.016] -> strictly <= 0.0)
        append_helical_coil(
            bm_coil,
            coil_radius=0.054,
            wire_radius=0.0085,
            length=0.068,
            turns=3.5,
            axis="Y",
            center_xyz=(0.0, -0.056, 0.0),
            segs_per_turn=8,
            wire_segs=5,
            mat_idx=1,
        )

        coil_obj = create_mesh_node(
            f"Coil_{tag}",
            bm_coil,
            (cx, coil_pivot_y, cz),
            parent=root_obj,
            mat_list=coil_mats,
            cfg=cfg,
        )

        # Grandchild Glow_<corner> ring at the bottom of the coil (y in [-0.016, 0.0])
        bm_glow = bmesh.new()
        glow_profile = [
            (0.0, -0.016),
            (0.050, -0.016),
            (0.064, -0.008),
            (0.058, 0.0),
            (0.0, 0.0),
        ]
        append_lathe_profile(
            bm_glow,
            glow_profile,
            axis="Y",
            center_xyz=(0.0, 0.0, 0.0),
            segments=12,
            mat_idx=0,
            auto_chamfer=False,
        )
        create_mesh_node(
            f"Glow_{tag}",
            bm_glow,
            (0.0, -0.118, 0.0),
            parent=coil_obj,
            mat_list=[mats["R1_LampAmber"]],
            cfg=cfg,
        )

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 8. FOCUSLENS (LensSocket under Eye at (-0.365, 1.23, 0.12))
#    Child node: Glass
# ==============================================================================

def build_focus_lens_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build FocusLens: turned-brass & dark-charcoal telephoto focus barrel mounted on
    Eye/LensSocket outboard of the CRT head cheek, with copper induction coils,
    knurled steel focus grips, top rangefinder sight, and child mesh 'Glass' (R1_LampAmber).
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_Brass, 1: R1_GreyMetal, 2: R1_Copper, 3: R1_BareSteel, 4: R1_OrangeStripe
    mat_list = [
        mats["R1_Brass"],
        mats["R1_GreyMetal"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
        mats["R1_OrangeStripe"],
    ]

    barrel_cx = -0.056
    barrel_r = 0.052

    # 1. Coaxial stepped brass & charcoal telephoto focus barrel along Z (outboard of Head cheek)
    barrel_profile = [
        (0.0, -0.055),
        (barrel_r * 0.78, -0.055),
        (barrel_r * 0.92, -0.044),
        (barrel_r * 0.92, -0.016),
        (barrel_r * 1.00, -0.008),
        (barrel_r * 1.00, 0.020),
        (barrel_r * 0.94, 0.030),
        (barrel_r * 0.76, 0.030),
        (barrel_r * 0.72, 0.014),
        (0.0, 0.014),
    ]
    append_lathe_profile(
        bm,
        barrel_profile,
        axis="Z",
        center_xyz=(barrel_cx, 0.0, 0.0),
        segments=28,
        mat_idx=0,
        ring_mat_indices=[1, 1, 0, 2, 0, 0, 3, 1, 1],
    )

    # 2. 2 concentric copper induction coil rings around the barrel
    for cz in (-0.022, 0.004):
        append_torus(
            bm,
            major_radius=barrel_r * 1.01,
            minor_radius=0.0055,
            axis="Z",
            center_xyz=(barrel_cx, 0.0, cz),
            major_seg=24,
            minor_seg=8,
            mat_idx=2,
        )

    # 3. 8 radial brushed-steel knurled grip lugs around the focus ring
    for g_i in range(8):
        ang = FULL_CIRCLE_RAD * g_i / 8.0
        gx = barrel_cx + barrel_r * 1.01 * math.cos(ang)
        gy = barrel_r * 1.01 * math.sin(ang)
        append_beveled_box(
            bm,
            size_xyz=(0.009, 0.010, 0.020),
            center_xyz=(gx, gy, 0.010),
            bevel_width=0.002,
            bevel_segments=1,
            mat_idx=3,
            rot_euler=(0.0, 0.0, ang),
            guard_inset=False,
        )

    # 4. Inboard mounting bridge clamping back onto the Eye cheek drum + brass bolts
    append_beveled_box(
        bm,
        size_xyz=(0.046, 0.056, 0.062),
        center_xyz=(-0.024, 0.0, -0.034),
        bevel_width=0.005,
        bevel_segments=2,
        mat_idx=1,
    )
    for sy in (-0.018, 0.018):
        append_closed_bolt(
            bm,
            radius=0.006,
            height=0.010,
            axis="X",
            center_xyz=(-0.048, sy, -0.036),
            mat_idx=0,
        )

    # 5. Top auxiliary rangefinder sight pod (matching upgraded.png cheek detail)
    append_beveled_box(
        bm,
        size_xyz=(0.026, 0.022, 0.044),
        center_xyz=(barrel_cx, barrel_r + 0.010, -0.006),
        bevel_width=0.004,
        bevel_segments=2,
        mat_idx=1,
    )
    append_beveled_box(
        bm,
        size_xyz=(0.028, 0.009, 0.022),
        center_xyz=(barrel_cx, barrel_r + 0.020, -0.006),
        bevel_width=0.002,
        bevel_segments=1,
        mat_idx=4,
    )

    root_obj = create_mesh_node(
        "FocusLens",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )

    # 6. Required child node 'Glass' (convex glowing optical element at front of barrel)
    bm_glass = bmesh.new()
    append_closed_dome(
        bm_glass,
        radius=barrel_r * 0.74,
        dome_height=0.016,
        collar_depth=0.006,
        axis="Z",
        center_xyz=(0.0, 0.0, 0.0),
        segments=24,
        rings=4,
        mat_idx=0,
    )
    create_mesh_node(
        "Glass",
        bm_glass,
        (barrel_cx, 0.0, 0.020),
        parent=root_obj,
        mat_list=[mats["R1_LampAmber"]],
        cfg=cfg,
    )

    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 9. LENSGLASS (Mounted directly on Eye at (-0.365, 1.23, 0.04))
# ==============================================================================

def build_lens_glass_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build LensGlass: precision convex optical glass dome, stepped turned-brass retaining
    bezel ring, brushed-steel iris blades, and copper retaining ring seated in Eye's
    viewfinder aperture.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # 0: R1_DarkGlass, 1: R1_Brass, 2: R1_BareSteel, 3: R1_Copper, 4: R1_GreyMetal, 5: R1_LampAmber
    mat_list = [
        mats["R1_DarkGlass"],
        mats["R1_Brass"],
        mats["R1_BareSteel"],
        mats["R1_Copper"],
        mats["R1_GreyMetal"],
        mats["R1_LampAmber"],
    ]

    eye_r = cfg["eye_outer_radius"]
    pod_w = cfg["eye_pod_width"]
    ap_x = -pod_w * 0.44

    # 1. Stepped brass & steel optical retaining ring + convex dark-glass meniscus dome along -X
    lens_profile = [
        (0.0, -0.028),
        (eye_r * 0.36, -0.024),
        (eye_r * 0.54, -0.014),
        (eye_r * 0.62, -0.014),
        (eye_r * 0.68, -0.020),
        (eye_r * 0.74, -0.016),
        (eye_r * 0.74, 0.008),
        (eye_r * 0.62, 0.012),
        (0.0, 0.012),
    ]
    append_lathe_profile(
        bm,
        lens_profile,
        axis="X",
        center_xyz=(ap_x, 0.0, 0.0),
        segments=28,
        mat_idx=0,
        ring_mat_indices=[0, 0, 2, 1, 1, 4, 4, 4],
    )

    # 2. Copper anti-reflection retaining ring & warm-amber inner reticle ring
    append_torus(
        bm,
        major_radius=eye_r * 0.64,
        minor_radius=0.0045,
        axis="X",
        center_xyz=(ap_x - 0.016, 0.0, 0.0),
        major_seg=26,
        minor_seg=8,
        mat_idx=3,
    )
    append_torus(
        bm,
        major_radius=eye_r * 0.55,
        minor_radius=0.0035,
        axis="X",
        center_xyz=(ap_x - 0.015, 0.0, 0.0),
        major_seg=24,
        minor_seg=6,
        mat_idx=5,
    )

    # 3. 8 precision steel spanner-ring lugs around the brass bezel
    for b_i in range(8):
        ang = FULL_CIRCLE_RAD * b_i / 8.0
        by = eye_r * 0.70 * math.sin(ang)
        bz = eye_r * 0.70 * math.cos(ang)
        append_beveled_box(
            bm,
            size_xyz=(0.010, 0.008, 0.012),
            center_xyz=(ap_x - 0.016, by, bz),
            bevel_width=0.0015,
            bevel_segments=1,
            mat_idx=2,
            rot_euler=(ang, 0.0, 0.0),
            guard_inset=False,
        )

    root_obj = create_mesh_node(
        "LensGlass",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )
    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# 10. SCANFAN (ScanFanSocket under Eye at (-0.365, 1.23, 0.12))
# ==============================================================================

def build_scan_fan_model(cfg=DIMENSIONS, mats=None, parent=None, unity_loc=(0.0, 0.0, 0.0)):
    """
    Build ScanFan: optical laser-curtain projector shroud & radial collimator vane array
    mounted at Eye/ScanFanSocket outboard of the CRT head cheek. Material slot 0 is
    R1_LampAmber so RoverBodyLanguage's MaterialPropertyBlock emission glow drives the fan emitter.
    """
    if mats is None:
        mats = create_material_library(MATERIALS, for_render=False)

    bm = bmesh.new()
    # Slot 0 MUST be R1_LampAmber so RoverBodyLanguage._scanFan glow drives the emitter!
    # 0: R1_LampAmber, 1: R1_GreyMetal, 2: R1_Brass, 3: R1_Copper, 4: R1_BareSteel
    mat_list = [
        mats["R1_LampAmber"],
        mats["R1_GreyMetal"],
        mats["R1_Brass"],
        mats["R1_Copper"],
        mats["R1_BareSteel"],
    ]

    fan_cx = -0.056
    fan_cz = 0.032

    # 1. Base brass & dark-charcoal collimator collar ring
    collar_profile = [
        (0.0, -0.012),
        (0.040, -0.012),
        (0.046, -0.006),
        (0.046, 0.012),
        (0.042, 0.018),
        (0.032, 0.018),
        (0.0, 0.008),
    ]
    append_lathe_profile(
        bm,
        collar_profile,
        axis="Z",
        center_xyz=(fan_cx, 0.0, fan_cz),
        segments=24,
        mat_idx=2,
        ring_mat_indices=[1, 1, 2, 3, 4, 0],
    )

    # 2. Flared upper & lower fan-curtain shroud plates (prism extrusion)
    shroud_poly = [
        (-0.032, 0.006),
        (0.032, 0.006),
        (0.056, 0.046),
        (-0.056, 0.046),
    ]
    for sy in (-0.018, 0.018):
        append_prism_extrusion(
            bm,
            shroud_poly,
            depth=0.007,
            axis="Y",
            center_xyz=(fan_cx, sy, fan_cz),
            bevel_width=0.002,
            bevel_segments=2,
            mat_idx=1,
        )

    # 3. 7 radial collimator vanes + glowing R1_LampAmber emitter bars fanning across +/-24 deg
    for v_i in range(-3, 4):
        yaw = math.radians(v_i * 8.0)
        vx = fan_cx + 0.028 * math.sin(yaw)
        vz = fan_cz + 0.028 * math.cos(yaw)
        # Brushed-steel collimator vane
        append_beveled_box(
            bm,
            size_xyz=(0.0045, 0.030, 0.028),
            center_xyz=(vx, 0.0, vz),
            bevel_width=0.001,
            bevel_segments=1,
            mat_idx=4,
            rot_euler=(0.0, yaw, 0.0),
        )
        # Glowing amber laser-curtain emitter bar at the leading edge of each vane
        gx = fan_cx + 0.042 * math.sin(yaw)
        gz = fan_cz + 0.042 * math.cos(yaw)
        append_beveled_box(
            bm,
            size_xyz=(0.0065, 0.026, 0.010),
            center_xyz=(gx, 0.0, gz),
            bevel_width=0.0015,
            bevel_segments=1,
            mat_idx=0,
            rot_euler=(0.0, yaw, 0.0),
        )

    # 4. Horizontal glowing amber scan-line emitter bar & copper harmonic ring across the fan aperture
    append_beveled_box(
        bm,
        size_xyz=(0.098, 0.010, 0.014),
        center_xyz=(fan_cx, 0.0, fan_cz + 0.040),
        bevel_width=0.0025,
        bevel_segments=2,
        mat_idx=0,
    )
    append_torus(
        bm,
        major_radius=0.043,
        minor_radius=0.0045,
        axis="Z",
        center_xyz=(fan_cx, 0.0, fan_cz + 0.004),
        major_seg=22,
        minor_seg=8,
        mat_idx=3,
    )

    root_obj = create_mesh_node(
        "ScanFan",
        bm,
        unity_loc,
        parent=parent,
        mat_list=mat_list,
        cfg=cfg,
    )
    bpy.context.view_layer.update()
    return root_obj


# ==============================================================================
# SHARED STANDALONE KIT EXPORT RUNNER
# ==============================================================================

KIT_BUILDERS = {
    "Kit_LampBar": build_kit_lamp_bar_model,
    "Kit_CapacitorDrum": build_kit_capacitor_drum_model,
    "Kit_BatterySpine": build_kit_battery_spine_model,
    "Kit_TetherRing": build_kit_tether_ring_model,
    "Kit_CargoRack": build_kit_cargo_rack_model,
    "Kit_SideBin": build_kit_side_bin_model,
    "HoverCoils": build_hover_coils_model,
    "FocusLens": build_focus_lens_model,
    "LensGlass": build_lens_glass_model,
    "ScanFan": build_scan_fan_model,
}


def build_and_export_kit(kit_name: str, fbx_out_path: str | None = None):
    """Clear scene, build `kit_name`, bake R1 vertex colors, and export deterministic FBX."""
    from pathlib import Path

    repo_root = Path(__file__).resolve().parents[4]
    if fbx_out_path is None:
        out_path = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover" / f"{kit_name}.fbx"
    else:
        out_path = Path(fbx_out_path).resolve()

    out_path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    mats = create_material_library(MATERIALS, for_render=False)
    builder_fn = KIT_BUILDERS[kit_name]
    builder_fn(DIMENSIONS, mats)

    objs = list(bpy.data.objects)
    meshes = [o for o in objs if o.type == "MESH"]
    bake_vertex_colors(meshes, rover_cfg=DIMENSIONS, include_ground_occluder=False)
    export_fbx(objs, out_path, bake_space_transform=True)
    stats = count_model_stats(objs)
    print(
        f"[{kit_name}] Exported {out_path.name}: "
        f"nodes={stats[0]} (mesh={stats[1]}, empty={stats[2]}), verts={stats[3]}, tris={stats[4]}"
    )
    return stats
