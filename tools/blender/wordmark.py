"""Writes the Lunarion wordmark (docs/features/M3-09-title-collections.md) as SVG, plus its outlines as JSON.

Blender sets "Lunarion" in the project's own Be Vietnam Pro and the outlines are read back as cubic Beziers. The
letters are spaced by hand (each letter is the group of contours sharing its columns), the dot of the "i" becomes a
crescent tilted like Earth's lit side in our sky, and a round stroke in the ink colour softens every corner and adds
a little weight (old, warm), the same in the SVG and in the preview. One variant adds a thin orbit round the "o".

    blender -b --factory-startup --python tools/blender/wordmark.py -- --font <BeVietnamPro-Regular.ttf>
        --out-dir docs/design [--geometry <outlines.json>]

The geometry JSON (sampled outlines in SVG units) feeds tools/blender/wordmark_preview.py, which composites the
mark over a title capture.
"""

import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector

WORD = "Lunarion"
# Open tracking for a calm title, as a factor of the font's own advance.
TRACKING = 1.08
# Extra space after each letter but the last (em): L-u closes the L's open side, r-i opens the tight pair.
SPACING = (-0.075, 0.0, 0.0, 0.0, 0.028, 0.006, 0.0)
# SVG units per em, and the margin around the mark in em.
UNITS = 100.0
MARGIN = 0.22
# Width of the ink-coloured round stroke round every outline (em): half of it is added weight, and its round joins
# soften the terminals.
SOFTEN = 0.016
# The crescent against the dot it replaces: outer radius, the inner circle's radius and its offset (fractions of
# the outer radius), and how far its lit side turns up from straight left: Earth's lit limb faces up and left in the
# title frame, so the crescent does too.
CRESCENT_SCALE = 2.0
CRESCENT_INNER = 0.86
CRESCENT_OFFSET = 0.37
CRESCENT_TILT_DEGREES = 45.0
CRESCENT_LIFT = 0.5
# The orbit round the "o": radii against the o's half width, tilt, stroke and the arc it skips behind the letter.
ORBIT_RADII = (1.75, 0.5)
ORBIT_TILT_DEGREES = -14.0
ORBIT_STROKE = 0.012
ORBIT_SAMPLES = 96
CURVE_SAMPLES = 12

VARIANTS = {
    "on-dark": {"ink": "#F4E6C8", "paper": "#0B0E2A", "orbit": False},
    "on-light": {"ink": "#2B2A3A", "paper": "#F4E6C8", "orbit": False},
    "orbit-on-dark": {"ink": "#F4E6C8", "paper": "#0B0E2A", "orbit": True},
    "orbit-on-light": {"ink": "#2B2A3A", "paper": "#F4E6C8", "orbit": True},
}


def outlines(font_path):
    """Closed Bezier contours of WORD, each a list of (co, handle_left, handle_right) in em units."""
    curve = bpy.data.curves.new("Word", "FONT")
    curve.body = WORD
    curve.font = bpy.data.fonts.load(font_path)
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


def letters_of(contours):
    """Groups contours into letters by overlapping columns, left to right (holes and the i's dot join theirs)."""
    groups = []
    for contour in sorted(contours, key=lambda c: bounds(c)[0]):
        low, _, high, _ = bounds(contour)
        if groups and low < groups[-1]["high"]:
            groups[-1]["contours"].append(contour)
            groups[-1]["high"] = max(groups[-1]["high"], high)
        else:
            groups.append({"contours": [contour], "high": high})
    if len(groups) != len(WORD):
        raise RuntimeError(f"wordmark: found {len(groups)} letters in {WORD!r}")
    return [group["contours"] for group in groups]


def shifted(contour, shift):
    offset = Vector((shift, 0.0))
    return [(co + offset, left + offset, right + offset) for co, left, right in contour]


def sample(contour):
    """The contour as a closed polyline (em), CURVE_SAMPLES points per Bezier segment."""
    points = []
    for current, following in zip(contour, contour[1:] + contour[:1]):
        p0, p1, p2, p3 = current[0], current[2], following[1], following[0]
        for step in range(CURVE_SAMPLES):
            t = step / CURVE_SAMPLES
            u = 1.0 - t
            points.append(p0 * u ** 3 + p1 * 3 * u * u * t + p2 * 3 * u * t * t + p3 * t ** 3)
    return points


def path_data(contour, to_svg):
    parts = [f"M{fmt(to_svg(contour[0][0]))}"]
    for current, following in zip(contour, contour[1:] + contour[:1]):
        parts.append(f"C{fmt(to_svg(current[2]))} {fmt(to_svg(following[1]))} {fmt(to_svg(following[0]))}")
    parts.append("Z")
    return "".join(parts)


def crescent(centre, radius):
    """Outer circle minus an offset inner circle: the two intersections and the polyline outline (em)."""
    inner = radius * CRESCENT_INNER
    tilt = math.radians(CRESCENT_TILT_DEGREES)
    # The inner circle sits opposite the lit side, which faces left turned by the tilt.
    axis = Vector((math.cos(tilt), -math.sin(tilt)))
    distance = radius * CRESCENT_OFFSET
    along = (radius * radius - inner * inner + distance * distance) / (2.0 * distance)
    across = math.sqrt(radius * radius - along * along)
    normal = Vector((-axis.y, axis.x))
    first = centre + axis * along + normal * across
    second = centre + axis * along - normal * across
    inner_centre = centre + axis * distance
    outline = (arc_points(centre, radius, first, second, -axis)
               + arc_points(inner_centre, inner, second, first, -axis))
    return first, second, inner, outline


def arc_points(centre, radius, start, end, through):
    """Points on the circle from start to end, taking the way round that passes the direction through."""
    a0 = math.atan2(start.y - centre.y, start.x - centre.x)
    a1 = math.atan2(end.y - centre.y, end.x - centre.x)
    sweeps = ((a1 - a0) % math.tau, (a1 - a0) % math.tau - math.tau)
    sweep = max(sweeps, key=lambda s: math.cos(a0 + s * 0.5) * through.x + math.sin(a0 + s * 0.5) * through.y)
    steps = 48
    return [centre + Vector((math.cos(a0 + sweep * i / steps), math.sin(a0 + sweep * i / steps))) * radius
            for i in range(steps + 1)]


def orbit(o_contours):
    """A tilted ellipse round the o, as polylines (em) with the stretch behind the letter left out."""
    low, bottom, high, top = bounds(max(o_contours, key=lambda c: bounds(c)[2] - bounds(c)[0]))
    centre = Vector(((low + high) * 0.5, (bottom + top) * 0.5))
    half = (high - low) * 0.5
    tilt = math.radians(ORBIT_TILT_DEGREES)
    runs = [[]]
    for i in range(ORBIT_SAMPLES + 1):
        angle = math.tau * i / ORBIT_SAMPLES
        local = Vector((math.cos(angle) * half * ORBIT_RADII[0], math.sin(angle) * half * ORBIT_RADII[1]))
        point = centre + Vector((local.x * math.cos(tilt) - local.y * math.sin(tilt),
                                 local.x * math.sin(tilt) + local.y * math.cos(tilt)))
        behind = math.sin(angle) > 0.0 and abs(point.x - centre.x) < half * 1.15
        if behind:
            if runs[-1]:
                runs.append([])
        else:
            runs[-1].append(point)
    return [run for run in runs if len(run) > 1]


def num(value):
    text = f"{value:.2f}".rstrip("0").rstrip(".")
    return "0" if text == "-0" else text


def fmt(point):
    return f"{num(point[0])} {num(point[1])}"


def build(contours):
    letters = letters_of(contours)
    shift = 0.0
    placed = []
    for index, letter in enumerate(letters):
        placed.append([shifted(c, shift) for c in letter])
        if index < len(SPACING):
            shift += SPACING[index]
    i_letter = placed[WORD.index("i")]
    dot = max(i_letter, key=lambda c: bounds(c)[1])
    i_letter.remove(dot)
    dot_box = bounds(dot)
    radius = (dot_box[2] - dot_box[0]) * 0.5 * CRESCENT_SCALE
    centre = Vector(((dot_box[0] + dot_box[2]) * 0.5, dot_box[1] + radius * (1.0 - CRESCENT_OFFSET) + CRESCENT_LIFT * radius))
    return [c for letter in placed for c in letter], (centre, radius), orbit(placed[WORD.index("o")])


def write(contours, out_dir, geometry_path):
    letters, (centre, radius), orbit_runs = build(contours)
    first, second, inner, moon_outline = crescent(centre, radius)
    pad = MARGIN + SOFTEN
    orbit_points = [p for run in orbit_runs for p in run]
    min_x = min(min(bounds(c)[0] for c in letters), min(p.x for p in orbit_points)) - pad
    max_x = max(max(bounds(c)[2] for c in letters), max(p.x for p in orbit_points)) + pad
    min_y = min(min(bounds(c)[1] for c in letters), min(p.y for p in orbit_points)) - pad
    max_y = max(max(bounds(c)[3] for c in letters), centre.y + radius, max(p.y for p in orbit_points)) + pad

    def to_svg(point):
        return ((point[0] - min_x) * UNITS, (max_y - point[1]) * UNITS)

    width = num((max_x - min_x) * UNITS)
    height = num((max_y - min_y) * UNITS)
    letters_d = "".join(path_data(c, to_svg) for c in letters)
    outer = num(radius * UNITS)
    inner_r = num(inner * UNITS)
    moon_d = (f"M{fmt(to_svg(first))}A{outer} {outer} 0 1 0 {fmt(to_svg(second))}"
              f"A{inner_r} {inner_r} 0 1 1 {fmt(to_svg(first))}Z")
    orbit_d = "".join("M" + " L".join(fmt(to_svg(p)) for p in run) for run in orbit_runs)
    soften = num(SOFTEN * UNITS)
    for name, variant in VARIANTS.items():
        orbit_element = (f'    <path fill="none" stroke-width="{num(ORBIT_STROKE * UNITS)}" stroke-linecap="round" '
                         f'd="{orbit_d}"/>\n') if variant["orbit"] else ""
        path = os.path.join(out_dir, f"lunarion-wordmark-v2-{name}.svg")
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(
                f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" '
                f'height="{height}">\n'
                f"  <title>Lunarion</title>\n"
                f'  <rect width="{width}" height="{height}" fill="{variant["paper"]}"/>\n'
                f'  <g fill="{variant["ink"]}" stroke="{variant["ink"]}" stroke-width="{soften}" '
                f'stroke-linejoin="round">\n'
                f'    <path fill-rule="evenodd" d="{letters_d}"/>\n'
                f'    <path d="{moon_d}"/>\n'
                f"{orbit_element}"
                f"  </g>\n"
                f"</svg>\n")
    if geometry_path:
        geometry = {
            "size": [float(width), float(height)],
            "soften": SOFTEN * UNITS,
            "orbitStroke": ORBIT_STROKE * UNITS,
            "letters": [[list(to_svg(p)) for p in sample(c)] for c in letters],
            "moon": [list(to_svg(p)) for p in moon_outline],
            "orbit": [[list(to_svg(p)) for p in run] for run in orbit_runs],
        }
        with open(geometry_path, "w", encoding="utf-8") as handle:
            json.dump(geometry, handle)


def main(argv):
    parser = argparse.ArgumentParser(prog="wordmark.py")
    parser.add_argument("--font", required=True)
    parser.add_argument("--out-dir", required=True)
    parser.add_argument("--geometry")
    args = parser.parse_args(argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    write(outlines(os.path.abspath(args.font)), os.path.abspath(args.out_dir),
          os.path.abspath(args.geometry) if args.geometry else None)
    print("wordmark: written to " + os.path.abspath(args.out_dir))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
