"""
Deterministic headless-Blender builder for 07 v3 ('Amber CRT'), SolarWing, and Kit_TapeDeck.

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/build_rover.py
"""
from pathlib import Path
import sys
import bpy

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))
sys.dont_write_bytecode = True

from mesh_ops import bake_model_vertex_colors, clear_scene, export_fbx
from render_suite import (
    enhance_materials_with_vertex_color_for_render,
    render_orthographic_views_and_turnaround,
    render_perspective_acceptance_views,
    render_wireframe_and_vertex_colors,
    setup_studio_lighting_and_camera,
)
from rover_parts import (
    build_rover_model_hierarchy,
    build_solar_wing_model,
    build_tape_deck_model,
    create_material_library,
)

# ==============================================================================
# NAMED DIMENSION & PROPORTION TABLE (Measured from Logs/v3/turnaround.png)
# Known calibration scale:
#   - Outer width across tyres = 1.52 m (294 px in Front/Back/Top -> 193.42 px/m)
#   - Wheel diameter           = 0.70 m (139 px in Front/Back/Top -> 198.57 px/m)
# ==============================================================================
DIMENSIONS = {
    # 1. Physics Footprint & Balloon Wheels
    "wheel_outer_half_span": 0.76,       # 1.52 m outer width across tyres (x = +/-0.76 m)
    "wheel_track": 0.64,                 # +/-0.64 m wheel hub center X (physics track preserved)
    "wheel_base": 0.80,                  # +/-0.80 m front/rear axle Z (physics wheelbase preserved)
    "wheel_radius": 0.35,                # 0.35 m rolling radius (diameter = 0.70 m)
    "wheel_tyre_width": 0.29,            # 56 px = 0.29 m plump balloon section (spans x = +/-0.47 m to +/-0.76 m)
    "wheel_rim_radius": 0.185,           # 74 px diameter = 0.37 m dished hub rim
    "wheel_rings_per_lug": 4,            # 4 circumferential rings per lug pitch (48 rings total)
    "wheel_profile_segments": 12,        # Smooth balloon sidewall cross-section
    "wheel_lug_count": 12,               # 12 chunky chevron tread bars per tyre
    "wheel_lug_height": 0.019,           # Low, rounded tread relief integrated into balloon mesh
    "wheel_chevron_deg": 20.0,           # Moderate chevron angle

    # 2. Pillowed Buggy Body Tub & Fenders
    "body_tub_width": 0.98,              # 192 px = 0.98 m (x = +/-0.49 m, meets inner tyre face at +/-0.47 m)
    "body_tub_length": 1.44,             # Spans z = -0.68 m to +0.76 m
    "body_tub_z_center": 0.04,           # Centered slightly forward between axles
    "body_tub_y_min": 0.45,              # Bottom of cream enamel shell above dark bumper/rocker
    "body_tub_y_max": 0.92,              # Top of upper equipment deck before slope
    "body_deck_top_y": 0.91,             # 180 px = 0.91 m upper deck / neck collar height
    "body_subdiv_cuts": 4,               # Smooth subdivision before sculpting
    "body_corner_bevel": 0.105,          # Generous rounded hull corners
    "body_bevel_segments": 4,            # Multi-segment smooth bevels
    "body_shoulder_inward_taper": 0.09,  # Soft inward curve at upper shoulders
    "body_end_inward_taper": 0.04,       # Subtle planform rounding at nose and tail
    "body_hood_slope_start_z": 0.28,     # Front chest hood slopes down ahead of neck
    "body_hood_slope_span_z": 0.48,
    "body_hood_drop_y": 0.18,
    "body_rear_slope_start_z": -0.50,
    "body_rear_slope_span_z": 0.22,
    "body_rear_drop_y": 0.07,
    "body_belt_y_bottom": 0.56,          # Single continuous warm-orange beltline (y = 0.56..0.65 m)
    "body_belt_y_top": 0.65,
    "wheel_arch_radius": 0.37,           # Wheel-well arch cutout radius around front/rear axles
    "wheel_arch_lift_y": 0.075,          # Arch rise over tyres

    # 3. Bumpers, Belly Pan, Fender Flares, Sage Patch & Empty Solar Rails
    "bumper_width": 1.00,                # 198 px = 1.00 m dark-charcoal front/rear bumper beams
    "bumper_height": 0.085,              # y = 0.38..0.465 m
    "bumper_depth": 0.14,
    "bumper_y_center": 0.425,
    "bumper_bevel": 0.024,
    "front_bumper_z": 0.71,
    "rear_bumper_z": -0.65,
    "belly_pan_width": 0.88,
    "belly_pan_height": 0.09,
    "belly_pan_length": 1.24,
    "belly_pan_y": 0.39,
    "fender_lip_width": 0.08,            # Flared wheel-arch lips reaching x = +/-0.50 m
    "fender_lip_height": 0.055,
    "fender_lip_length": 0.36,
    "fender_lip_x": 0.465,
    "fender_lip_y": 0.505,
    "fender_lip_z": 0.56,
    "sage_patch_x": 0.488,               # SagePatch_RL on left flank (51x45 px = 0.26 m x 0.23 m)
    "sage_patch_y": 0.575,
    "sage_patch_z": -0.24,
    "sage_patch_thickness": 0.018,
    "sage_patch_height": 0.23,
    "sage_patch_length": 0.26,
    "rivet_radius": 0.008,
    "solar_deck_slope_deg": 8.0,
    "solar_rail_width": 0.042,
    "solar_rail_height": 0.026,
    "solar_rail_length": 0.48,
    "solar_rail_x": 0.27,
    "solar_rail_y": 0.915,
    "solar_rail_z": -0.20,
    "solar_pad_width": 0.48,
    "solar_pad_length": 0.44,

    # 4. Hero 80%-Width CRT Head, Screen, Wrap-Around Visor, Neck & Cheek Pods
    "piv_neck": (0.0, 0.92, 0.24),       # Preserved Neck pivot
    "piv_head_local": (0.0, 0.26, 0.0),  # Preserved Head pivot ((0, 1.18, +0.24) world)
    "neck_base_flange_radius": 0.19,     # Wide, low-profile collar ring (0.08 m visible neck gap)
    "neck_bellows_radius": 0.165,
    "head_cream_width": 0.68,            # 132 px = 0.68 m cream CRT monitor casing
    "head_cream_height": 0.50,           # 99 px = 0.50 m tall (y = 0.99..1.49 m world)
    "head_cream_depth": 0.44,            # Deep pillowed CRT monitor housing
    "head_local_center_y": 0.06,         # Centers head at y = 1.24 m world (chin at 0.99 m, crown at 1.49 m)
    "head_local_center_z": 0.02,         # Front face at z = +0.24 m local; rear face at z = -0.20 m local
    "head_corner_bevel": 0.068,          # Generous pillowed CRT corner bevels
    "head_bevel_segments": 4,
    "head_visor_width": 0.80,            # 156 px = 0.80 m across visor (~81.6% of body width)
    "head_visor_height": 0.51,           # Wraps sides from y = 1.01 m to brow at y = 1.55 m
    "head_visor_depth": 0.27,            # 56 px = 0.27 m deep anti-glare hood
    "head_visor_thickness": 0.048,
    "piv_screen_local": (0.0, 0.06, 0.248),     # World (0.00, 1.24, +0.488) cleanly in front of Head face
    "piv_visor_local": (0.0, 0.06, 0.26),       # World (0.00, 1.24, +0.50)
    "piv_rear_vent_local": (0.0, 0.06, -0.255), # World (0.00, 1.24, -0.015) cleanly proud of rear cap
    "screen_width": 0.56,                # 109 px = 0.56 m edge-to-edge warm amber screen
    "screen_height": 0.40,               # 79 px = 0.40 m tall (y = 1.04..1.44 m world)
    "screen_bulge_z": 0.018,             # Gentle convex CRT glass crown
    "screen_corner_round": 0.08,
    "screen_grid_x": 8,
    "screen_grid_y": 6,
    "rear_vent_width": 0.46,             # 90 px = 0.46 m wide rear louver grille
    "rear_vent_height": 0.25,            # 50 px = 0.25 m tall rear louver grille
    "rear_vent_bar_count": 5,            # 5 horizontal glowing amber louver slots
    "piv_eye_local": (-0.35, 0.05, 0.06),       # World (-0.35, 1.23, +0.30) on left cheek of wider head
    "piv_eye_origin_local": (0.0, 0.0, 0.08),   # World (-0.35, 1.23, +0.38)
    "piv_lock_ring_local": (0.0, 0.0, 0.05),    # World (-0.35, 1.23, +0.35)
    "eye_outer_radius": 0.12,            # 47 px diameter = 0.24 m bold brass viewfinder pod
    "eye_pod_width": 0.095,
    "piv_deck_socket_local": (0.35, 0.05, 0.04),  # World (+0.35, 1.23, +0.28) on right cheek of wider head

    # 5. Front Lamps, Chest Cutter, Scoop, Rear Hatch, Winch, Antenna & Suspension Pivots
    "piv_headlamp_x": 0.28,              # 54 px = 0.28 m from center
    "piv_headlamp_y": 0.68,              # Preserved node pivot Y
    "piv_headlamp_z": 0.71,              # Preserved node pivot Z
    "headlamp_visual_y_offset": -0.055,  # Places 0.20 m lamp center at y = 0.625 m on the beltline
    "headlamp_outer_radius": 0.10,       # 39 px diameter = 0.20 m round brass & amber headlamps
    "piv_headlamp_glass_local": (0.0, -0.002, 0.02),
    "piv_front_intake": (0.0, 0.54, 0.70),
    "piv_front_intake_roller_local": (0.0, -0.01, 0.012),
    "piv_cutter_turret": (0.0, 0.78, 0.72),
    "piv_cutter_head_local": (0.0, 0.0, 0.06),
    "piv_cutter_origin_local": (0.0, 0.01, 0.14),
    "piv_stitch_origin_local": (0.0, -0.04, 0.13),
    "piv_coil_socket_t1_local": (0.0, 0.0, 0.04),
    "cutter_outer_radius": 0.115,        # 48 px diameter = 0.23 m coaxial brass cutter collar
    "cutter_visual_y_offset": -0.095,    # Centers cutter at y = 0.685 m in the raised nose boss
    "piv_dig_scoop": (0.0, 0.36, 0.70),
    "piv_scoop_bucket_local": (0.0, -0.12, 0.20),  # World (0.00, 0.24, +0.90) visible below nose
    "piv_scoop_origin_local": (0.0, -0.10, 0.18),  # World (0.00, 0.14, +1.08) tooth tip
    "piv_scoop_cyl_x": 0.24,
    "piv_scoop_cyl_y": 0.08,
    "piv_scoop_cyl_z": 0.04,
    "scoop_bucket_width": 0.66,          # 130 px = 0.66 m wide trenching bucket
    "scoop_bucket_height": 0.23,         # Visible below front bumper in Front, Side, Top views
    "scoop_bucket_depth": 0.26,
    "scoop_tooth_count": 5,
    "battery_bar_span_x": 0.52,          # 8 segments spanning x = -0.26..+0.26 m
    "piv_battery_lamp_y": 0.462,         # Seated above rear bumper (y = 0.405) & below CargoHatch doors
    "piv_battery_lamp_z": -0.722,        # Flush on the rear lower valence face
    "piv_cargo_hold": (0.0, 0.64, -0.46),
    "piv_hatch_origin_local": (0.0, -0.04, -0.18),
    "piv_story_rack_local": (0.0, -0.03, 0.02),
    "piv_hatch_window_local": (0.0, 0.19, -0.16),
    "piv_cargo_hatch": (0.0, 0.80, -0.64),
    "piv_lower_chute_local": (0.0, -0.32, 0.0),
    "piv_intake_roller_local": (0.0, -0.28, -0.02),
    "cargo_hatch_width": 0.61,           # 119 px = 0.61 m rear double-door hatch width
    "piv_winch_socket": (0.0, 0.36, -0.68),
    "piv_tow_hook_local": (0.0, 0.0, -0.18),
    "piv_antenna": (-0.36, 0.92, -0.48),
    "piv_sonar_loop_local": (0.0, 0.16, 0.0),
    "piv_antenna_tip_local": (0.03, 0.66, -0.04),
    "piv_arm_x": 0.44,
    "piv_arm_y": 0.48,
    "piv_arm_z": 0.42,
    "piv_shock_x": 0.44,
    "piv_shock_y": 0.68,
    "piv_shock_z": 0.52,
    "piv_solar_wing": (0.0, 0.95, 0.04),

    # 6. Kit Dimensions (SolarWing.fbx & Kit_TapeDeck.fbx)
    "solar_wing_width": 0.56,
    "solar_wing_length": 0.48,
    "tape_deck_width": 0.09,
    "tape_deck_height": 0.21,
    "tape_deck_depth": 0.21,

    # 7. Vertex Color Baking (R = Cycles AO, G = Convex Edge Wear, B = Crease/Cavity Dust, A = 1)
    "uv_tile_scale": 0.5,
    "ao_samples": 24,
    "ao_distance": 0.90,
    "ao_ground_size": 4.0,
    "ao_ground_z_offset": -0.005,
    "ao_min_floor": 0.12,
    "ao_gamma": 0.85,
    "cavity_from_ao_weight": 0.72,
    "curvature_ref_edge_len": 0.024,
    "curvature_min_angle": 0.06,
    "curvature_min_edge_len": 0.004,
    "curvature_max_density_boost": 2.0,
    "curvature_smooth_steps": 1,
    "curvature_self_weight": 0.65,
    "convex_bias": 0.04,
    "convex_gain": 1.65,
    "concave_bias": 0.05,
    "concave_gain": 1.85,

    # 8. Turnaround & Render Calibration
    "render_eevee_samples": 32,
    "render_bg_linear": (0.034, 0.046, 0.064),
    "turnaround_bg_srgb": (52, 60, 71),
    "ortho_render_size_px": 1024,
    "ortho_render_scale_m": 2.40,
    "ortho_target_y": 0.78,
    "ortho_cam_distance": 6.0,
    "turnaround_width_px": 1376,
    "turnaround_height_px": 768,
    "turnaround_px_per_m": 194.0,
    "turnaround_ground_y_px": 539,
    "turnaround_front_cx_px": 164,
    "turnaround_side_cx_px": 502,
    "turnaround_back_cx_px": 865,
    "turnaround_top_cx_px": 1194,
    "turnaround_top_cy_px": 366,
    "persp_lens_mm": 52.0,
    "persp_render_size_px": 1024,
}

# Allowed R1_* Material Slot Definitions (including R1_SolarCell for SolarWing PV cells)
MATERIALS = {
    "R1_CreamEnamel": {
        "base_color": (0.78, 0.67, 0.49),
        "metallic": 0.04,
        "roughness": 0.38,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_OrangeStripe": {
        "base_color": (0.76, 0.30, 0.16),
        "metallic": 0.04,
        "roughness": 0.48,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_GreyMetal": {
        "base_color": (0.085, 0.095, 0.115),
        "metallic": 0.45,
        "roughness": 0.52,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_BareSteel": {
        "base_color": (0.42, 0.44, 0.48),
        "metallic": 0.80,
        "roughness": 0.34,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_Rubber": {
        "base_color": (0.055, 0.058, 0.062),
        "metallic": 0.0,
        "roughness": 0.78,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_GoldFoil": {
        "base_color": (0.72, 0.52, 0.14),
        "metallic": 0.85,
        "roughness": 0.28,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_DarkGlass": {
        "base_color": (0.03, 0.04, 0.07),
        "metallic": 0.15,
        "roughness": 0.14,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_SolarCell": {
        "base_color": (0.06, 0.04, 0.18),
        "metallic": 0.25,
        "roughness": 0.18,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_Copper": {
        "base_color": (0.72, 0.43, 0.16),
        "metallic": 0.88,
        "roughness": 0.28,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_SagePatch": {
        "base_color": (0.24, 0.42, 0.28),
        "metallic": 0.15,
        "roughness": 0.52,
        "emission_color": (0.0, 0.0, 0.0),
        "emission_strength": 0.0,
    },
    "R1_LampAmber": {
        "base_color": (1.0, 0.78, 0.24),
        "metallic": 0.0,
        "roughness": 0.20,
        "emission_color": (1.0, 0.76, 0.22),
        "emission_strength": 1.65,
    },
    "R1_Screen": {
        "base_color": (0.86, 0.52, 0.13),
        "metallic": 0.0,
        "roughness": 0.16,
        "emission_color": (0.95, 0.62, 0.18),
        "emission_strength": 1.35,
    },
}


def count_model_stats(objects):
    """Return (total_nodes, mesh_nodes, empty_nodes, total_verts, total_tris) for objects."""
    mesh_objs = [o for o in objects if o.type == "MESH"]
    empty_objs = [o for o in objects if o.type == "EMPTY"]
    verts = sum(len(o.data.vertices) for o in mesh_objs)
    tris = sum(sum(max(0, len(p.vertices) - 2) for p in o.data.polygons) for o in mesh_objs)
    return len(objects), len(mesh_objs), len(empty_objs), verts, tris


def main():
    repo_root = SCRIPT_DIR.parents[3]
    fbx_out_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Rover"
    renders_dir = repo_root / "Logs" / "renders"
    concept_turnaround = repo_root.parent / "agy-face" / "Logs" / "v3" / "turnaround.png"

    fbx_out_dir.mkdir(parents=True, exist_ok=True)
    renders_dir.mkdir(parents=True, exist_ok=True)

    # 1. Build, bake vertex colors, and export SolarWing.fbx
    clear_scene()
    mats_sw = create_material_library(MATERIALS, for_render=False)
    build_solar_wing_model(DIMENSIONS, mats_sw)
    sw_objs = list(bpy.data.objects)
    sw_meshes = [o for o in sw_objs if o.type == "MESH"]
    bake_model_vertex_colors(sw_meshes, DIMENSIONS, include_ground_occluder=False)
    export_fbx(fbx_out_dir / "SolarWing.fbx", sw_objs)
    sw_stats = count_model_stats(sw_objs)

    # 2. Build, bake vertex colors, and export Kit_TapeDeck.fbx
    clear_scene()
    mats_td = create_material_library(MATERIALS, for_render=False)
    build_tape_deck_model(DIMENSIONS, mats_td)
    td_objs = list(bpy.data.objects)
    td_meshes = [o for o in td_objs if o.type == "MESH"]
    bake_model_vertex_colors(td_meshes, DIMENSIONS, include_ground_occluder=False)
    export_fbx(fbx_out_dir / "Kit_TapeDeck.fbx", td_objs)
    td_stats = count_model_stats(td_objs)

    # 3. Build, bake vertex colors, and export RoverModel.fbx
    clear_scene()
    mats_rv = create_material_library(MATERIALS, for_render=False)
    rover_root = build_rover_model_hierarchy(DIMENSIONS, mats_rv)
    rv_objs = list(bpy.data.objects)
    rv_meshes = [o for o in rv_objs if o.type == "MESH"]
    bake_model_vertex_colors(rv_meshes, DIMENSIONS, include_ground_occluder=True)
    export_fbx(fbx_out_dir / "RoverModel.fbx", rv_objs)
    rv_stats = count_model_stats(rv_objs)

    # 4. Configure render-only procedural Screen shader + vertex-color shading and render all acceptance images
    from rover_parts import configure_procedural_crt_screen_shader

    configure_procedural_crt_screen_shader(mats_rv["R1_Screen"], MATERIALS["R1_Screen"])
    enhance_materials_with_vertex_color_for_render(mats_rv)
    cam_obj, _ = setup_studio_lighting_and_camera(DIMENSIONS)

    render_orthographic_views_and_turnaround(cam_obj, renders_dir, concept_turnaround, DIMENSIONS)
    render_perspective_acceptance_views(cam_obj, rover_root, renders_dir, DIMENSIONS, mats_rv)
    render_wireframe_and_vertex_colors(cam_obj, rv_meshes, renders_dir, DIMENSIONS)

    print("=== BUILD SUMMARY ===")
    print(f"RoverModel.fbx   : nodes={rv_stats[0]} (mesh={rv_stats[1]}, empty={rv_stats[2]}), verts={rv_stats[3]}, tris={rv_stats[4]}")
    print(f"SolarWing.fbx    : nodes={sw_stats[0]} (mesh={sw_stats[1]}, empty={sw_stats[2]}), verts={sw_stats[3]}, tris={sw_stats[4]}")
    print(f"Kit_TapeDeck.fbx : nodes={td_stats[0]} (mesh={td_stats[1]}, empty={td_stats[2]}), verts={td_stats[3]}, tris={td_stats[4]}")


if __name__ == "__main__":
    main()
