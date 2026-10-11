"""Writes the Lunarion wordmark (docs/features/M3-09-title-collections.md) as SVG and renders a PNG preview.

Blender sets "Lunarion" in the project's own Be Vietnam Pro, the outlines are read back as cubic Beziers and written
as SVG paths; the dot of the "i" is replaced by a thin crescent, the mark's only ornament. The preview is rendered
from the written SVG files themselves (imported back), so it shows exactly what ships.

    blender -b --factory-startup --python tools/blender/wordmark.py -- --font <BeVietnamPro-Regular.ttf>
        --out-dir docs/design [--preview docs/design/lunarion-wordmark-preview.png]
"""

import argparse
import math
import os
import sys

import bpy
from mathutils import Vector

WORD = "Lunarion"
# Open tracking for a calm title, as a factor of the font's own advance.
TRACKING = 1.06
# SVG units per em, and the margin around the mark in em.
UNITS = 100.0
MARGIN = 0.18
# The crescent against the dot it replaces: outer radius, the inner circle's radius and its offset (fractions of
# the outer radius). A thin moon bulging left with its horns to the right, nearly upright: tilted further it reads
# as an apostrophe, and opening up it reads as a breve (Vietnamese "ă"). Its body is nudged right over the stem.
CRESCENT_SCALE = 1.35
CRESCENT_INNER = 0.86
CRESCENT_OFFSET = (0.37, 0.07)
CRESCENT_NUDGE = 0.22
# Pulls the L toward the u (em): L's open right side leaves a hole the tracking would widen.
L_KERN = 0.045

VARIANTS = {
    "on-dark": {"ink": "#F4E6C8", "paper": "#0B0E2A"},
    "on-light": {"ink": "#2B2A3A", "paper": "#F4E6C8"},
}


def outlines(font_path):
    """Closed Bezier contours of WORD, each a list of (co, handle_left, handle_right) in em units."""
    font = bpy.data.fonts.load(font_path)
    curve = bpy.data.curves.new("Word", "FONT")
    curve.body = WORD
    curve.font = font
    curve.size = 1.0
    curve.space_character = TRACKING
    holder = bpy.data.objects.new("Word", curve)
    bpy.context.scene.collection.objects.link(holder)
    shape = holder.to_curve(bpy.context.evaluated_depsgraph_get())
    contours = []
    for spline in shape.splines:
        points = [(p.co.xy.copy(), p.handle_left.xy.copy(), p.handle_right.xy.copy()) for p in spline.bezier_points]
        if points:
            contours.append(points)
    holder.to_curve_clear()
    return contours


def bounds(points):
    xs = [p[0].x for p in points]
    ys = [p[0].y for p in points]
    return min(xs), min(ys), max(xs), max(ys)


def find_dot(contours):
    """The i's tittle: the one small contour sitting wholly above the x-height."""
    x_height = max(bounds(c)[3] for c in contours if bounds(c)[3] - bounds(c)[1] < 0.6) * 0.8
    candidates = [c for c in contours if bounds(c)[1] > x_height and bounds(c)[2] - bounds(c)[0] < 0.2]
    if len(candidates) != 1:
        raise RuntimeError(f"wordmark: expected one dot above the x-height, found {len(candidates)}")
    return candidates[0]


def kerned(contour, shift):
    offset = Vector((shift, 0.0))
    return [(co + offset, left + offset, right + offset) for co, left, right in contour]


def path_data(contour, to_svg):
    first = contour[0]
    parts = [f"M{fmt(to_svg(first[0]))}"]
    for current, following in zip(contour, contour[1:] + contour[:1]):
        parts.append(f"C{fmt(to_svg(current[2]))} {fmt(to_svg(following[1]))} {fmt(to_svg(following[0]))}")
    parts.append("Z")
    return "".join(parts)


def crescent_data(centre, radius, to_svg):
    """Outer circle minus an offset inner circle, as two SVG arcs between their intersections (SVG's y runs down,
    so the sweeps are mirrored from Blender's)."""
    inner = radius * CRESCENT_INNER
    offset = Vector(CRESCENT_OFFSET) * radius
    distance = offset.length
    along = (radius * radius - inner * inner + distance * distance) / (2.0 * distance)
    across = math.sqrt(radius * radius - along * along)
    axis = offset.normalized()
    normal = Vector((-axis.y, axis.x))
    base = centre + axis * along
    first = base + normal * across
    second = base - normal * across
    scaled_outer = radius * UNITS
    scaled_inner = inner * UNITS
    return (f"M{fmt(to_svg(first))}"
            f"A{num(scaled_outer)} {num(scaled_outer)} 0 1 0 {fmt(to_svg(second))}"
            f"A{num(scaled_inner)} {num(scaled_inner)} 0 1 1 {fmt(to_svg(first))}Z")


def num(value):
    text = f"{value:.2f}".rstrip("0").rstrip(".")
    return "0" if text == "-0" else text


def fmt(point):
    return f"{num(point[0])} {num(point[1])}"


def write_svgs(contours, out_dir):
    dot = find_dot(contours)
    first = min(contours, key=lambda c: bounds(c)[0])
    contours = [kerned(c, L_KERN) if c is first else c for c in contours if c is not dot] + [dot]
    letters = contours[:-1]
    dot = contours[-1]
    min_x = min(bounds(c)[0] for c in letters) - MARGIN
    min_y = min(bounds(c)[1] for c in contours) - MARGIN
    max_x = max(bounds(c)[2] for c in contours) + MARGIN
    dot_box = bounds(dot)
    radius = (dot_box[2] - dot_box[0]) * 0.5 * CRESCENT_SCALE
    centre = Vector(((dot_box[0] + dot_box[2]) * 0.5 + radius * CRESCENT_NUDGE, (dot_box[1] + dot_box[3]) * 0.5))
    max_y = max(max(bounds(c)[3] for c in contours), centre.y + radius) + MARGIN

    def to_svg(point):
        return ((point[0] - min_x) * UNITS, (max_y - point[1]) * UNITS)

    width = num((max_x - min_x) * UNITS)
    height = num((max_y - min_y) * UNITS)
    letters_d = "".join(path_data(c, to_svg) for c in letters)
    moon_d = crescent_data(centre, radius, to_svg)
    written = {}
    for name, colours in VARIANTS.items():
        path = os.path.join(out_dir, f"lunarion-wordmark-{name}.svg")
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(
                f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" '
                f'height="{height}">\n'
                f"  <title>Lunarion</title>\n"
                f'  <rect width="{width}" height="{height}" fill="{colours["paper"]}"/>\n'
                f'  <g fill="{colours["ink"]}">\n'
                f'    <path fill-rule="evenodd" d="{letters_d}"/>\n'
                f'    <path d="{moon_d}"/>\n'
                f"  </g>\n"
                f"</svg>\n")
        written[name] = path
    return written


def render_preview(svgs, path):
    """Imports each written SVG back and renders them stacked: the in-game cream mark over the night, then ink."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    stacked_height = 0.0
    frame = None
    for name in ("on-dark", "on-light"):
        before = set(bpy.data.objects)
        bpy.ops.import_curve.svg(filepath=svgs[name])
        imported = [obj for obj in bpy.data.objects if obj not in before]
        low = Vector((min(v[0] for o in imported for v in o.bound_box), min(v[1] for o in imported for v in o.bound_box)))
        high = Vector((max(v[0] for o in imported for v in o.bound_box), max(v[1] for o in imported for v in o.bound_box)))
        size = high - low
        # Document order: the paper first, then the ink a hair above it, so nothing z-fights.
        for depth, obj in enumerate(sorted(imported, key=lambda o: o.name)):
            obj.location.y -= stacked_height + high.y
            obj.location.x -= low.x
            obj.location.z = depth * 0.001
        stacked_height += size.y
        frame = size
    camera_data = bpy.data.cameras.new("Preview")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max(frame.x, stacked_height)
    camera = bpy.data.objects.new("Preview", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (frame.x * 0.5, -stacked_height * 0.5, 1.0)
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "MATERIAL"
    scene.view_settings.view_transform = "Standard"
    pixels_per_unit = 1200 / frame.x
    scene.render.resolution_x = 1200
    scene.render.resolution_y = int(round(stacked_height * pixels_per_unit))
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 100
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main(argv):
    parser = argparse.ArgumentParser(prog="wordmark.py")
    parser.add_argument("--font", required=True)
    parser.add_argument("--out-dir", required=True)
    parser.add_argument("--preview")
    args = parser.parse_args(argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    svgs = write_svgs(outlines(os.path.abspath(args.font)), os.path.abspath(args.out_dir))
    if args.preview:
        render_preview(svgs, os.path.abspath(args.preview))
    print("wordmark: " + ", ".join(sorted(svgs.values())))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
