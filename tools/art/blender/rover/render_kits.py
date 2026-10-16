"""
Headless-Blender preview renderer for 07 v3's 10 R1 upgrade kits:
  - Renders close-up shots of all 10 kits mounted on 07 v3 (`Logs/renders/kit_*.png`)
  - Renders 07 v3 with all upgrades fitted (`Logs/renders/upgraded_front_3q.png`, `Logs/renders/upgraded_back_3q.png`)
  - Composites 07 v3 fully upgraded side-by-side with `agy-face/Logs/v3/upgraded.png` -> `Logs/renders/compare_upgraded.png`

Run:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/rover/render_kits.py
"""
from __future__ import annotations

import math
from pathlib import Path
import sys
import bpy
import numpy as np

SCRIPT_DIR = Path(__file__).resolve().parent
BLENDER_DIR = SCRIPT_DIR.parent
for _p in (BLENDER_DIR, SCRIPT_DIR):
    if str(_p) not in sys.path:
        sys.path.insert(0, str(_p))
sys.dont_write_bytecode = True

from build_rover import DIMENSIONS, MATERIALS  # noqa: E402
from r1kit import bake_vertex_colors, reset_scene  # noqa: E402
from render_suite import (  # noqa: E402
    aim_camera_at,
    enhance_materials_with_vertex_color_for_render,
    load_image_rgba,
    render_to_file,
    resize_rgba_bilinear,
    save_image_rgba,
    setup_studio_lighting_and_camera,
)
from rover_parts import (  # noqa: E402
    build_rover_model_hierarchy,
    build_solar_wing_model,
    build_tape_deck_model,
    configure_procedural_crt_screen_shader,
    create_material_library,
)
from kit_parts import (  # noqa: E402
    build_focus_lens_model,
    build_hover_coils_model,
    build_kit_battery_spine_model,
    build_kit_capacitor_drum_model,
    build_kit_cargo_rack_model,
    build_kit_lamp_bar_model,
    build_kit_side_bin_model,
    build_kit_tether_ring_model,
    build_lens_glass_model,
    build_scan_fan_model,
)


def _rename_hierarchy(root_obj, prefix: str):
    """Prefix root_obj and its descendants so multiple instances (L/R) never collide in bpy.data.objects."""
    root_obj.name = f"{prefix}_{root_obj.name}"
    for child in root_obj.children_recursive:
        child.name = f"{prefix}_{child.name}"


def build_fully_upgraded_rover_scene():
    """Build 07 v3 with SolarWing, Kit_TapeDeck, and all 10 R1 upgrade kits mounted at their exact v3 sockets."""
    reset_scene()
    mats = create_material_library(MATERIALS, for_render=False)
    rover_root = build_rover_model_hierarchy(DIMENSIONS, mats)

    # Hide base DeckCover when Kit_TapeDeck is mounted
    deck_cov = bpy.data.objects.get("DeckCover")
    if deck_cov is not None:
        deck_cov.hide_render = True
        deck_cov.hide_viewport = True

    # Mount SolarWing on SolarWing socket and Kit_TapeDeck on DeckSocket
    sw_sock = bpy.data.objects.get("SolarWing")
    if sw_sock is not None:
        sw_obj = build_solar_wing_model(DIMENSIONS, mats, parent=sw_sock, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(sw_obj, "MountedSW")

    deck_sock = bpy.data.objects.get("DeckSocket")
    if deck_sock is not None:
        td_obj = build_tape_deck_model(DIMENSIONS, mats, parent=deck_sock, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(td_obj, "MountedTD")

    # 1. Kit_LampBar on HeadlampSocket
    hl_sock = bpy.data.objects.get("HeadlampSocket")
    if hl_sock is not None:
        build_kit_lamp_bar_model(DIMENSIONS, mats, parent=hl_sock, unity_loc=(0.0, 0.0, 0.0))

    # 2. Kit_CapacitorDrum on DrumSocket_L (rotated 180 deg around Unity Y = Blender Z) and DrumSocket_R
    drum_l = bpy.data.objects.get("DrumSocket_L")
    if drum_l is not None:
        drum_l.rotation_euler = (0.0, 0.0, math.pi)
        d_l = build_kit_capacitor_drum_model(DIMENSIONS, mats, parent=drum_l, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(d_l, "L")
    drum_r = bpy.data.objects.get("DrumSocket_R")
    if drum_r is not None:
        d_r = build_kit_capacitor_drum_model(DIMENSIONS, mats, parent=drum_r, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(d_r, "R")

    # 3. Kit_BatterySpine on SpineSocket (plus mirrored visual twin on left solar rail for full-upgrade render)
    spine_sock = bpy.data.objects.get("SpineSocket")
    if spine_sock is not None:
        build_kit_battery_spine_model(DIMENSIONS, mats, parent=spine_sock, unity_loc=(0.0, 0.0, 0.0))

    # 4. Kit_TetherRing on RingSocket (rotated 180 deg around Unity Y = Blender Z)
    ring_sock = bpy.data.objects.get("RingSocket")
    if ring_sock is not None:
        ring_sock.rotation_euler = (0.0, 0.0, math.pi)
        build_kit_tether_ring_model(DIMENSIONS, mats, parent=ring_sock, unity_loc=(0.0, 0.0, 0.0))

    # 5. Kit_CargoRack on CargoSocket
    cargo_sock = bpy.data.objects.get("CargoSocket")
    if cargo_sock is not None:
        build_kit_cargo_rack_model(DIMENSIONS, mats, parent=cargo_sock, unity_loc=(0.0, 0.0, 0.0))

    # 6. Kit_SideBin on BinSocket_L (rotated 180 deg around Unity Y = Blender Z) and BinSocket_R
    bin_l = bpy.data.objects.get("BinSocket_L")
    if bin_l is not None:
        bin_l.rotation_euler = (0.0, 0.0, math.pi)
        b_l = build_kit_side_bin_model(DIMENSIONS, mats, parent=bin_l, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(b_l, "L")
    bin_r = bpy.data.objects.get("BinSocket_R")
    if bin_r is not None:
        b_r = build_kit_side_bin_model(DIMENSIONS, mats, parent=bin_r, unity_loc=(0.0, 0.0, 0.0))
        _rename_hierarchy(b_r, "R")

    # 7. HoverCoils on CoilSocket
    coil_sock = bpy.data.objects.get("CoilSocket")
    if coil_sock is not None:
        build_hover_coils_model(DIMENSIONS, mats, parent=coil_sock, unity_loc=(0.0, 0.0, 0.0))

    # 8. FocusLens on LensSocket
    lens_sock = bpy.data.objects.get("LensSocket")
    if lens_sock is not None:
        build_focus_lens_model(DIMENSIONS, mats, parent=lens_sock, unity_loc=(0.0, 0.0, 0.0))

    # 9. LensGlass on Eye
    eye_obj = bpy.data.objects.get("Eye")
    if eye_obj is not None:
        build_lens_glass_model(DIMENSIONS, mats, parent=eye_obj, unity_loc=(0.0, 0.0, 0.0))

    # 10. ScanFan on ScanFanSocket
    fan_sock = bpy.data.objects.get("ScanFanSocket")
    if fan_sock is not None:
        build_scan_fan_model(DIMENSIONS, mats, parent=fan_sock, unity_loc=(0.0, 0.0, 0.0))

    bpy.context.view_layer.update()
    all_meshes = [o for o in bpy.data.objects if o.type == "MESH" and not o.hide_render]
    bake_vertex_colors(all_meshes, rover_cfg=DIMENSIONS, include_ground_occluder=True)

    configure_procedural_crt_screen_shader(mats["R1_Screen"], MATERIALS["R1_Screen"])
    enhance_materials_with_vertex_color_for_render(mats)
    return rover_root, mats


def _set_hierarchy_hidden(root_name: str, hidden: bool):
    """Toggle hide_render and hide_viewport on `root_name` and all of its descendants."""
    root_obj = bpy.data.objects.get(root_name)
    if root_obj is None:
        return
    root_obj.hide_render = hidden
    root_obj.hide_viewport = hidden
    for child in root_obj.children_recursive:
        child.hide_render = hidden
        child.hide_viewport = hidden


def render_all_kit_previews(renders_dir: Path | None = None):
    """Render all 10 kit close-ups, `compare_upgraded.png`, and `compare_scanfan_focuslens.png` into `Logs/renders/`."""
    repo_root = SCRIPT_DIR.parents[3]
    if renders_dir is None:
        renders_dir = repo_root / "Logs" / "renders"
    renders_dir = Path(renders_dir).resolve()
    renders_dir.mkdir(parents=True, exist_ok=True)

    concept_upgraded = repo_root.parent / "agy-face" / "Logs" / "v3" / "upgraded.png"

    build_fully_upgraded_rover_scene()
    # Keep ScanFan hidden during default full-upgrade & FocusLens renders (matching ShowcaseSceneBuilder)
    _set_hierarchy_hidden("ScanFan", True)

    cam_obj, _ = setup_studio_lighting_and_camera(DIMENSIONS)
    cam_obj.data.type = "PERSP"
    cam_obj.data.lens = 35.0

    # 1. Render full-upgrade 3/4 front and 3/4 back views
    front_3q_path = renders_dir / "upgraded_front_3q.png"
    back_3q_path = renders_dir / "upgraded_back_3q.png"
    aim_camera_at(cam_obj, (-2.50, 1.34, 2.75), (-0.02, 0.72, 0.08))
    render_to_file(front_3q_path, 1376, 768)

    cam_obj.data.lens = 50.0
    aim_camera_at(cam_obj, (2.05, 1.42, -2.25), (0.0, 0.78, -0.10))
    render_to_file(back_3q_path, 1024, 1024)

    # 2. Composite Logs/renders/compare_upgraded.png side-by-side with concept upgraded.png
    if concept_upgraded.exists():
        concept_rgba = load_image_rgba(concept_upgraded)
        ch, cw, _ = concept_rgba.shape
        render_rgba = load_image_rgba(front_3q_path)
        if (render_rgba.shape[0], render_rgba.shape[1]) != (ch, cw):
            render_rgba = resize_rgba_bilinear(render_rgba, ch, cw)
        comp = np.concatenate([concept_rgba, render_rgba], axis=1)
        # 2px separator line
        comp[:, cw - 1 : cw + 1, :3] = 0.38
        save_image_rgba(renders_dir / "compare_upgraded.png", comp, include_alpha=False)

    # 3. Render close-ups of each of the 10 kits on 07 v3
    closeup_shots = (
        ("kit_lamp_bar.png", (-0.78, 0.76, 1.48), (0.0, 0.60, 0.78)),
        ("kit_capacitor_drum.png", (-1.25, 0.94, -0.48), (-0.52, 0.81, -0.14)),
        ("kit_battery_spine.png", (1.08, 1.28, -0.58), (0.38, 0.94, -0.20)),
        ("kit_tether_ring.png", (-0.75, 1.38, -0.82), (0.0, 1.26, -0.18)),
        ("kit_cargo_rack.png", (-1.18, 1.02, -1.62), (0.0, 0.64, -0.88)),
        ("kit_side_bin.png", (-1.22, 0.68, 0.44), (-0.52, 0.60, 0.02)),
        ("kit_hover_coils.png", (-0.88, 0.14, 0.02), (0.0, 0.22, 0.0)),
        ("kit_focus_lens.png", (-0.88, 1.30, 0.46), (-0.43, 1.23, 0.13)),
        ("kit_lens_glass.png", (-0.92, 1.25, 0.14), (-0.41, 1.23, 0.04)),
        ("kit_scan_fan.png", (-0.88, 1.30, 0.46), (-0.43, 1.23, 0.13)),
    )
    cam_obj.data.lens = 58.0
    for filename, c_pos, t_pos in closeup_shots:
        if filename == "kit_scan_fan.png":
            _set_hierarchy_hidden("FocusLens", True)
            _set_hierarchy_hidden("ScanFan", False)
        else:
            _set_hierarchy_hidden("FocusLens", False)
            _set_hierarchy_hidden("ScanFan", True)
        aim_camera_at(cam_obj, c_pos, t_pos)
        render_to_file(renders_dir / filename, 768, 768)

    _set_hierarchy_hidden("FocusLens", False)
    _set_hierarchy_hidden("ScanFan", True)

    # 4. Composite FocusLens (left) vs ScanFan (right) side-by-side -> compare_scanfan_focuslens.png
    lens_rgba = load_image_rgba(renders_dir / "kit_focus_lens.png")
    fan_rgba = load_image_rgba(renders_dir / "kit_scan_fan.png")
    lh, lw, _ = lens_rgba.shape
    if (fan_rgba.shape[0], fan_rgba.shape[1]) != (lh, lw):
        fan_rgba = resize_rgba_bilinear(fan_rgba, lh, lw)
    comp_sf = np.concatenate([lens_rgba, fan_rgba], axis=1)
    comp_sf[:, lw - 1 : lw + 1, :3] = 0.38
    save_image_rgba(renders_dir / "compare_scanfan_focuslens.png", comp_sf, include_alpha=False)


if __name__ == "__main__":
    render_all_kit_previews()

