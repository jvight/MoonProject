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
    # 1. Physics Footprint & Chunky Off-Road Tyres (Concept-true wheelbase & track)
    "wheel_outer_half_span": 0.722,      # With 0.016 m sidewall bulge -> 0.738 m (1.476 m outer width across tyres)
    "wheel_track": 0.61,                 # +/-0.61 m wheel hub center X
    "wheel_base": 0.53,                  # +/-0.53 m front/rear axle Z (concept wheelbase -> 1.79 m side span)
    "wheel_radius": 0.35,                # 0.35 m rolling radius (diameter = 0.70 m)
    "wheel_tyre_width": 0.228,           # 0.228 m wide carcass + 2x0.016 m sidewall bulge = 0.26 m total section
    "wheel_rim_radius": 0.185,           # 0.37 m deep-dish wheel rim
    "wheel_lug_count": 22,               # 22 paired smooth-crowned chevron tread lugs (44 lug blocks per wheel)
    "wheel_lug_height": 0.020,           # 2.0 cm deep off-road tread lug relief
    "wheel_chevron_deg": 24.0,           # Crisp directional chevron lug angle
    "wheel_bolt_count": 8,               # 8 hex lug bolts around the deep-dish hub circle

    # 2. Pillowed "Beetle" Carapace Hull, Rounded Shoulders, Curved Bonnet, Seams & Wheel Arches
    "body_tub_width": 0.88,              # 0.88 m barreled beetle flank width (x = +/-0.44 m, inside inner tyres)
    "body_tub_length": 1.49,             # Spans z = -0.745 m to +0.745 m
    "body_tub_z_center": 0.00,           # Centered between front and rear axles
    "body_tub_y_min": 0.42,              # Bottom of lower cream skirt between wheel arches
    "body_tub_y_max": 0.965,             # Top of domed upper beetle carapace cowling
    "body_deck_top_y": 0.955,            # Upper deck / neck collar seating height
    "body_corner_bevel": 0.036,          # Crisp rounded planform corners
    "body_panel_bevel": 0.014,           # Crisp 1.4 cm 2-segment bevel on large hull panels
    "body_small_bevel": 0.006,           # 6 mm 2-segment chamfer on hatches, frames, and brackets
    "body_seam_depth": 0.007,            # 7 mm recessed 3D panel seam groove depth
    "body_belt_y_bottom": 0.56,          # Single continuous warm-orange beltline (y = 0.56..0.65 m)
    "body_belt_y_top": 0.65,
    "wheel_arch_radius": 0.365,          # Deep wheel-well arch cutout radius around front/rear axles
    "wheel_arch_lift_y": 0.145,          # Deep 14.5 cm arch cutout into lower hull flanks over all 4 wheels

    # 3. Bumpers, Belly Pan, Fender Arch Lips, Sage Patch & Empty Solar Rails
    "bumper_width": 0.88,                # Dark-charcoal front/rear bumper beams tucked between wheels
    "bumper_height": 0.082,              # y = 0.384..0.466 m
    "bumper_depth": 0.12,
    "bumper_y_center": 0.425,
    "bumper_bevel": 0.014,               # Crisp 1.4 cm 2-segment bumper bevel
    "front_bumper_z": 0.715,
    "rear_bumper_z": -0.715,
    "belly_pan_width": 0.78,
    "belly_pan_height": 0.080,
    "belly_pan_length": 0.56,            # Central belly pan between front & rear wheel arches (y_min = 0.335 m)
    "belly_pan_y": 0.375,
    "fender_lip_width": 0.052,           # Smooth 16-segment semicircular wheel-arch fender flares
    "fender_lip_height": 0.042,
    "fender_lip_length": 0.42,
    "fender_lip_x": 0.452,
    "fender_lip_y": 0.545,
    "fender_lip_z": 0.53,
    "sage_patch_x": 0.454,               # SagePatch_RL on left flank with 10 brass dome rivets
    "sage_patch_y": 0.575,
    "sage_patch_z": -0.16,
    "sage_patch_thickness": 0.014,
    "sage_patch_height": 0.22,
    "sage_patch_length": 0.24,
    "rivet_radius": 0.0075,
    "solar_deck_slope_deg": 6.0,
    "solar_rail_width": 0.040,
    "solar_rail_height": 0.024,
    "solar_rail_length": 0.42,
    "solar_rail_x": 0.25,
    "solar_rail_y": 0.955,
    "solar_rail_z": -0.26,
    "solar_pad_width": 0.44,
    "solar_pad_length": 0.38,

    # 4. Hero CRT Head, Screen, Boxy Front Shroud Visor, Neck & Cheek Pods (Moved back to concept Z)
    "piv_neck": (0.0, 0.92, 0.13),       # Concept-true Neck pivot (moved back from +0.24 to +0.13)
    "piv_head_local": (0.0, 0.26, 0.0),  # Head pivot ((0, 1.18, +0.13) world)
    "neck_base_flange_radius": 0.185,    # Molded collar ring + bolted flange
    "neck_bellows_radius": 0.155,        # 4-ribbed rubber bellows
    "head_cream_width": 0.75,            # 0.75 m cream CRT monitor casing
    "head_cream_height": 0.50,           # 0.50 m tall (y = 1.01..1.51 m world)
    "head_cream_depth": 0.48,            # Front bezel + middle cream shell + stepped rear housing
    "head_local_center_y": 0.08,         # Centers head at y = 1.26 m world (chin at 1.01 m, crown at 1.51 m)
    "head_local_center_z": 0.03,         # Spans z = -0.21 m local (-0.08 world) to +0.27 m local (+0.40 world)
    "head_corner_bevel": 0.022,          # Crisp 2.2 cm 2-segment CRT housing bevels
    "head_bevel_segments": 2,
    "head_visor_width": 0.77,            # 0.77 m across dark CRT front shroud
    "head_visor_height": 0.51,           # Wraps front screen from chin (1.02 m) to brow (1.53 m)
    "head_visor_depth": 0.28,            # Deep dark-charcoal front CRT bezel shroud + top brow cap
    "head_visor_thickness": 0.036,
    "piv_screen_local": (0.0, 0.08, 0.295),     # World (0.00, 1.26, +0.425) framed by shroud
    "piv_visor_local": (0.0, 0.08, 0.305),      # World (0.00, 1.26, +0.435)
    "piv_rear_vent_local": (0.0, 0.08, -0.275), # World (0.00, 1.26, -0.145), extends back to -0.21 m world
    "screen_width": 0.58,                # 0.58 m edge-to-edge warm amber screen
    "screen_height": 0.39,               # 0.39 m tall (y = 1.065..1.455 m world)
    "screen_bulge_z": 0.018,             # Gentle convex CRT glass crown
    "screen_corner_round": 0.075,
    "screen_grid_x": 8,
    "screen_grid_y": 6,
    "rear_vent_width": 0.48,             # 0.48 m wide rear louver grille
    "rear_vent_height": 0.26,            # 0.26 m tall rear louver grille
    "rear_vent_bar_count": 6,            # 6 recessed angled louver slats
    "piv_eye_local": (-0.365, 0.05, -0.09),     # World (-0.365, 1.23, +0.04) on exposed cream left cheek
    "piv_eye_origin_local": (0.0, 0.0, 0.08),   # World (-0.365, 1.23, +0.12)
    "piv_lock_ring_local": (0.0, 0.0, 0.05),    # World (-0.365, 1.23, +0.09)
    "eye_outer_radius": 0.12,            # 0.24 m layered brass viewfinder pod
    "eye_pod_width": 0.090,
    "piv_deck_socket_local": (0.365, 0.05, -0.09),  # World (+0.365, 1.23, +0.04) on right cheek of Head

    # 5. Front Lamps, Chest Cutter, Tucked Scoop, Rear Hatch, Tucked Winch, Antenna & Suspension Pivots
    "piv_headlamp_x": 0.26,              # 0.26 m from center on front nose
    "piv_headlamp_y": 0.63,              # Node pivot Y
    "piv_headlamp_z": 0.56,              # Node pivot Z (tucked onto curved beetle nose)
    "headlamp_visual_y_offset": -0.025,  # Places brass lamp bezel center at y = 0.605 m on the beltline
    "headlamp_outer_radius": 0.098,      # 0.196 m deep brass bezels + convex glass domes
    "piv_headlamp_glass_local": (0.0, -0.002, 0.02),
    "piv_front_intake": (0.0, 0.50, 0.58),
    "piv_front_intake_roller_local": (0.0, -0.01, 0.012),
    "piv_cutter_turret": (0.0, 0.72, 0.55),
    "piv_cutter_head_local": (0.0, 0.0, 0.05),
    "piv_cutter_origin_local": (0.0, 0.01, 0.12),
    "piv_stitch_origin_local": (0.0, -0.04, 0.11),
    "piv_coil_socket_t1_local": (0.0, 0.0, 0.04),
    "cutter_outer_radius": 0.112,        # 0.224 m coaxial stepped brass cutter collar
    "cutter_visual_y_offset": -0.055,    # Centers cutter at y = 0.665 m in the curved beetle bonnet boss
    "piv_dig_scoop": (0.0, 0.35, 0.54),
    "piv_scoop_bucket_local": (0.0, -0.13, 0.16),  # World (0.00, 0.22, +0.70) tucked between front wheels
    "piv_scoop_origin_local": (0.0, -0.08, 0.18),  # World (0.00, 0.14, +0.88) tooth tip
    "piv_scoop_cyl_x": 0.24,
    "piv_scoop_cyl_y": 0.08,
    "piv_scoop_cyl_z": 0.04,
    "scoop_bucket_width": 0.68,          # 0.68 m wide hollowed trenching bucket matching concept front/top
    "scoop_bucket_height": 0.24,         # Spans y = 0.11..0.35 m world
    "scoop_bucket_depth": 0.26,
    "scoop_tooth_count": 6,
    "battery_bar_span_x": 0.50,          # 8 segments spanning x = -0.25..+0.25 m inside recessed dark frame
    "piv_battery_lamp_y": 0.462,         # Seated above rear bumper & below CargoHatch doors
    "piv_battery_lamp_z": -0.600,        # Flush on the rear lower valence face
    "piv_cargo_hold": (0.0, 0.64, -0.42),
    "piv_hatch_origin_local": (0.0, -0.04, -0.16),
    "piv_story_rack_local": (0.0, -0.03, 0.02),
    "piv_hatch_window_local": (0.0, 0.19, -0.16),
    "piv_cargo_hatch": (0.0, 0.80, -0.58),
    "piv_lower_chute_local": (0.0, -0.32, 0.0),
    "piv_intake_roller_local": (0.0, -0.28, -0.02),
    "cargo_hatch_width": 0.58,           # Rear clamshell hatch with 4 hinges & central latch
    "piv_winch_socket": (0.0, 0.38, -0.60),
    "piv_tow_hook_local": (0.0, 0.0, -0.12),    # World (0.0, 0.38, -0.72), loop tucked inside z = -0.885 m
    "piv_antenna": (0.36, 0.91, -0.46),
    "piv_sonar_loop_local": (0.0, 0.14, 0.0),
    "piv_antenna_tip_local": (0.0, 0.66, 0.0),
    "piv_arm_x": 0.42,
    "piv_arm_y": 0.46,
    "piv_arm_z": 0.31,
    "piv_shock_x": 0.42,
    "piv_shock_y": 0.66,
    "piv_shock_z": 0.43,
    "piv_solar_wing": (0.0, 0.96, -0.06),

    # 6. Kit Dimensions (SolarWing.fbx & Kit_TapeDeck.fbx)
    "solar_wing_width": 0.56,
    "solar_wing_length": 0.48,
    "tape_deck_width": 0.09,
    "tape_deck_height": 0.21,
    "tape_deck_depth": 0.21,

    # 7. Weighted Normals & Vertex Color Baking (R = AO, G = Edge Wear, B = Cavity Dust, A = 1)
    "weighted_normal_sharp_deg": 42.0,
    "weighted_normal_area_power": 1.6,
    "uv_tile_scale": 0.5,
    "ao_samples": 32,
    "ao_distance": 0.85,
    "ao_ground_size": 4.0,
    "ao_ground_z_offset": -0.005,
    "ao_min_floor": 0.12,
    "ao_gamma": 0.95,
    "cavity_from_ao_weight": 0.85,
    "curvature_ref_edge_len": 0.022,
    "curvature_min_angle": 0.14,
    "curvature_min_edge_len": 0.003,
    "curvature_max_density_boost": 2.4,
    "convex_bias": 0.03,
    "convex_gain": 2.10,
    "concave_bias": 0.03,
    "concave_gain": 2.25,

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
    "turnaround_px_per_m": 198.0,
    "turnaround_ground_y_px": 539,
    "turnaround_front_cx_px": 164,
    "turnaround_side_cx_px": 502,
    "turnaround_back_cx_px": 865,
    "turnaround_top_cx_px": 1194,
    "turnaround_top_cy_px": 366,
    "persp_lens_mm": 52.0,
    "persp_render_size_px": 1024,
}

# Allowed R1_* Material Slot Definitions (including R1_Brass for machined brass bezels/rings)
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
    "R1_Brass": {
        "base_color": (0.86, 0.69, 0.32),
        "metallic": 0.92,
        "roughness": 0.24,
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
