"""Render the R1 Spare Igniter as a 512x512 RGBA icon source (`Logs/renders/igniter_icon_source.png`).

Deliverable 3 (`Logs/TASK.md`):
- Turntable-ready render of the igniter on a transparent background (512x512 px, line-art friendly)
  saved to `Logs/renders/igniter_icon_source.png` for the UI icon later.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import build_open_crate  # noqa: E402
import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def render_igniter_icon(
    out_path: Path = REPO_ROOT / "Logs" / "renders" / "igniter_icon_source.png",
) -> Path:
    """Build the standalone Igniter model and render a 512x512 RGBA turntable icon source."""
    out_path = Path(out_path).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)

    ruins_common.reset_ruins_scene()

    mb = r1kit.MeshBuilder("Igniter_IconSource", pivot_u=(0.0, 0.0, 0.0))
    build_open_crate.build_igniter_into(mb, center_u=(0.0, 0.0, 0.0))
    igniter_obj = ruins_common.to_ruin_object(mb, "Igniter_IconSource", parent=None, origin_u=(0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(igniter_obj, texel_scale=0.85)
    ruins_common.apply_signage_uvs(igniter_obj)
    ruins_common.bake_ruins_vertex_colors(igniter_obj, footprint_radius=0.14)
    ruins_common.attach_signage_texture_for_render(use_vertex_color_shading=True)

    # Slight turntable presentation tilt so the top tag, ceramic insulator sheds,
    # brass hex collar, copper spark tip, and braided copper lead all separate cleanly in silhouette
    igniter_obj.rotation_euler = Euler(
        (math.radians(8.0), math.radians(-10.0), math.radians(-28.0)),
        "XYZ",
    )

    scene = bpy.context.scene
    ruins_common._configure_fast_cycles(scene, 512, 512)
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"

    # Studio 3-point lighting tuned for clear form separation & crisp material value contrast
    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs[0].default_value = (0.18, 0.21, 0.26, 1.0)
        bg.inputs[1].default_value = 0.75

    def _add_sun(name: str, energy: float, color: tuple[float, float, float], rot_deg: tuple[float, float, float]) -> None:
        ldata = bpy.data.lights.new(name=name, type="SUN")
        ldata.energy = energy
        ldata.color = color
        lobj = bpy.data.objects.new(name, ldata)
        scene.collection.objects.link(lobj)
        lobj.rotation_euler = Euler(tuple(math.radians(a) for a in rot_deg), "XYZ")

    # Warm grazing key light, cool shadow fill, and crisp back-rim separation light
    _add_sun("KeySun", 3.6, (1.00, 0.95, 0.86), (48.0, 12.0, -36.0))
    _add_sun("FillSun", 1.35, (0.76, 0.85, 1.00), (62.0, -24.0, 118.0))
    _add_sun("RimSun", 2.4, (1.00, 0.88, 0.72), (-38.0, 18.0, 152.0))

    # Camera framing the 0.18 m igniter tightly in 512x512
    bpy.context.view_layer.update()
    pts_w = [igniter_obj.matrix_world @ Vector(c) for c in igniter_obj.bound_box]
    min_v = Vector((min(p.x for p in pts_w), min(p.y for p in pts_w), min(p.z for p in pts_w)))
    max_v = Vector((max(p.x for p in pts_w), max(p.y for p in pts_w), max(p.z for p in pts_w)))
    center = 0.5 * (min_v + max_v)
    span = max((max_v - min_v).length, 0.18)

    cam_data = bpy.data.cameras.new("IgniterIconCam")
    cam_data.lens = 70.0
    cam_data.clip_start = 0.01
    cam_data.clip_end = 10.0
    cam_obj = bpy.data.objects.new("IgniterIconCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    az = math.radians(-58.0)
    el = math.radians(28.0)
    dist = span * 1.18
    cam_pos = center + Vector(
        (
            dist * math.cos(el) * math.cos(az),
            dist * math.cos(el) * math.sin(az),
            dist * math.sin(el),
        )
    )
    cam_obj.location = cam_pos
    direction = (center - cam_pos).normalized()
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()

    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)
    print(f"[igniter_icon] Rendered {out_path}")
    return out_path


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Render 512x512 RGBA igniter icon source.")
    parser.add_argument(
        "--out",
        type=Path,
        default=REPO_ROOT / "Logs" / "renders" / "igniter_icon_source.png",
    )
    args = parser.parse_args(argv)
    render_igniter_icon(out_path=args.out)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
