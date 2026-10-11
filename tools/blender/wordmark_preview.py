"""Composites the Lunarion wordmark over a title capture, as the title would show it (M3-09): cream with a very soft
warm bloom, in the sky, never over 07.

    python tools/blender/wordmark_preview.py --geometry <outlines.json> --background <title capture.png>
        --out <preview.png> [--orbit] [--width 0.3] [--centre 0.81 0.1]

The geometry comes from tools/blender/wordmark.py; outlines are filled even-odd and stroked with round joins exactly
as the SVG is, at SUPERSAMPLE times the size, then reduced.
"""

import argparse
import json

from PIL import Image, ImageChops, ImageDraw, ImageFilter

INK = (244, 230, 200)
BLOOM = (255, 181, 71)
BLOOM_RADIUS = 0.012
BLOOM_STRENGTH = 0.35
# Defaults for the mark's width against the frame and its centre (fractions of the frame): the open sky right of
# Earth and The Peak's beacon in the title frame.
WIDTH_FRACTION = 0.3
CENTRE = (0.81, 0.1)
SUPERSAMPLE = 4


def mask(geometry, scale, orbit):
    width = int(geometry["size"][0] * scale)
    height = int(geometry["size"][1] * scale)
    fill = Image.new("1", (width, height), 0)
    for outline in geometry["letters"]:
        layer = Image.new("1", (width, height), 0)
        ImageDraw.Draw(layer).polygon([(x * scale, y * scale) for x, y in outline], fill=1)
        fill = ImageChops.logical_xor(fill, layer)
    fill = fill.convert("L")
    ImageDraw.Draw(fill).polygon([(x * scale, y * scale) for x, y in geometry["moon"]], fill=255)
    stroke = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(stroke)
    soften = geometry["soften"] * scale
    for outline in geometry["letters"] + [geometry["moon"]]:
        points = [(x * scale, y * scale) for x, y in outline]
        stroke_round(draw, points + points[:1], soften)
    if orbit:
        for run in geometry["orbit"]:
            stroke_round(draw, [(x * scale, y * scale) for x, y in run], geometry["orbitStroke"] * scale)
    return ImageChops.lighter(fill, stroke)


def stroke_round(draw, points, width):
    draw.line(points, fill=255, width=max(1, int(round(width))), joint="curve")
    radius = width * 0.5
    for x, y in (points[0], points[-1]):
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=255)


def main():
    parser = argparse.ArgumentParser(prog="wordmark_preview.py")
    parser.add_argument("--geometry", required=True)
    parser.add_argument("--background", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--orbit", action="store_true")
    parser.add_argument("--width", type=float, default=WIDTH_FRACTION)
    parser.add_argument("--centre", type=float, nargs=2, default=CENTRE)
    args = parser.parse_args()
    with open(args.geometry, encoding="utf-8") as handle:
        geometry = json.load(handle)
    frame = Image.open(args.background).convert("RGB")
    target_width = frame.width * args.width
    scale = target_width / geometry["size"][0]
    big = mask(geometry, scale * SUPERSAMPLE, args.orbit)
    alpha = big.resize((int(big.width / SUPERSAMPLE), int(big.height / SUPERSAMPLE)), Image.LANCZOS)
    left = int(frame.width * args.centre[0] - alpha.width / 2)
    top = int(frame.height * args.centre[1] - alpha.height / 2)
    full = Image.new("L", frame.size, 0)
    full.paste(alpha, (left, top))
    glow = full.filter(ImageFilter.GaussianBlur(frame.width * BLOOM_RADIUS)).point(lambda v: int(v * BLOOM_STRENGTH))
    frame = Image.composite(Image.new("RGB", frame.size, BLOOM), frame, glow)
    frame = Image.composite(Image.new("RGB", frame.size, INK), frame, full)
    frame.save(args.out, quality=90, optimize=True)


if __name__ == "__main__":
    main()
