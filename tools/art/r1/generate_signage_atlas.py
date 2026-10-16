#!/usr/bin/env python3
"""Deterministic 1980s stencil signage, logo, hazard-stripe, and pictogram atlas generator for R1_Signage.

Produces 512x512 Albedo, Normal, and Mask maps (rendered at 1024x1024 2x supersampling for crisp edges)
with the following UV layout (V=0 at bottom, V=1 at top):
  Row 3 (V = 0.75 .. 1.00):
    - LOGO          (U = 0.00 .. 0.25): Circular Lumen Commission crescent-and-horizon emblem
    - LUMEN         (U = 0.25 .. 0.75): Bold stencil 'LUMEN' + 'INTERNATIONAL LUNAR PROGRAMME'
    - EVAC_1988     (U = 0.75 .. 1.00): Stencil plaque 'EVAC 1988'
  Row 2 (V = 0.50 .. 0.75):
    - SEC04_HABITAT (U = 0.00 .. 0.50): Two-line stencil 'SEC-04' / 'HABITAT & LAB'
    - SEC_04        (U = 0.50 .. 0.75): Large stencil 'SEC-04'
    - HABITAT_LAB   (U = 0.75 .. 1.00): Stencil 'HABITAT & LAB'
  Row 1 (V = 0.25 .. 0.50):
    - WORKSHOP      (U = 0.00 .. 0.50): Bold stencil 'WORKSHOP'
    - HAZARD        (U = 0.50 .. 1.00): Diagonal 45-deg amber/charcoal hazard warning stripes
  Row 0 (V = 0.00 .. 0.25):
    - PICT_ROVER    (U = 0.00 .. 0.25): Cassette-futurism rover pictogram tile on warm orange band
    - PICT_RADIO    (U = 0.25 .. 0.50): Cassette-futurism radio mast pictogram tile on warm orange band
    - PICT_BOTANY   (U = 0.50 .. 0.75): Cassette-futurism botany dome pictogram tile on warm orange band
    - PICT_AIRLOCK  (U = 0.75 .. 1.00): Cassette-futurism airlock portal pictogram tile on warm orange band
"""

from __future__ import annotations

import math
import numpy as np
from PIL import Image, ImageDraw


# Canonical UV cell bounds (u_min, v_min, u_max, v_max) with V=0 at bottom, V=1 at top
SIGNAGE_UV_CELLS: dict[str, tuple[float, float, float, float]] = {
    "LOGO": (0.00, 0.75, 0.25, 1.00),
    "LUMEN": (0.25, 0.75, 0.75, 1.00),
    "EVAC_1988": (0.75, 0.75, 1.00, 1.00),
    "SEC04_HABITAT": (0.00, 0.50, 0.50, 0.75),
    "SEC_04": (0.50, 0.50, 0.75, 0.75),
    "HABITAT_LAB": (0.75, 0.50, 1.00, 0.75),
    "WORKSHOP": (0.00, 0.25, 0.50, 0.50),
    "HAZARD": (0.50, 0.25, 1.00, 0.50),
    "PICT_ROVER": (0.00, 0.00, 0.25, 0.25),
    "PICT_RADIO": (0.25, 0.00, 0.50, 0.25),
    "PICT_BOTANY": (0.50, 0.00, 0.75, 0.25),
    "PICT_AIRLOCK": (0.75, 0.00, 1.00, 0.25),
}

# Palette colours in 0..255 sRGB matching identity_v1_helios_lumen.png & identity_v3_cassette_futurism.png
CREAM_BG = (236, 226, 204)
CREAM_TILE = (242, 233, 212)
STENCIL_DARK = (32, 36, 48)
NAVY_DARK = (24, 29, 46)
ORANGE_BAND = (224, 114, 68)
AMBER_STRIPE = (232, 164, 56)
HAZARD_DARK = (36, 40, 50)
SAGE_ACCENT = (134, 162, 138)


def _draw_thick_seg(
    draw: ImageDraw.ImageDraw,
    x0: float,
    y0: float,
    x1: float,
    y1: float,
    w: float,
    color: tuple[int, int, int],
) -> None:
    dx = x1 - x0
    dy = y1 - y0
    length = math.hypot(dx, dy)
    if length < 1e-4:
        r = w * 0.5
        draw.ellipse([x0 - r, y0 - r, x0 + r, y0 + r], fill=color)
        return
    nx = -dy / length * (w * 0.5)
    ny = dx / length * (w * 0.5)
    poly = [(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)]
    draw.polygon(poly, fill=color)


# Vector strokes in normalized glyph coordinates [0..1] x [0..1] (y=0 top, y=1 bottom)
# Each tuple is (x0, y0, x1, y1)
GLYPH_STROKES: dict[str, list[tuple[float, float, float, float]]] = {
    "A": [(0.08, 0.96, 0.48, 0.04), (0.92, 0.96, 0.52, 0.04), (0.24, 0.64, 0.76, 0.64)],
    "B": [
        (0.12, 0.04, 0.12, 0.96),
        (0.12, 0.08, 0.68, 0.08),
        (0.68, 0.08, 0.84, 0.22),
        (0.84, 0.22, 0.84, 0.36),
        (0.84, 0.36, 0.68, 0.48),
        (0.12, 0.48, 0.68, 0.48),
        (0.68, 0.48, 0.88, 0.62),
        (0.88, 0.62, 0.88, 0.80),
        (0.88, 0.80, 0.68, 0.92),
        (0.12, 0.92, 0.68, 0.92),
    ],
    "C": [
        (0.86, 0.22, 0.68, 0.08),
        (0.68, 0.08, 0.30, 0.08),
        (0.30, 0.08, 0.12, 0.26),
        (0.12, 0.26, 0.12, 0.74),
        (0.12, 0.74, 0.30, 0.92),
        (0.30, 0.92, 0.68, 0.92),
        (0.68, 0.92, 0.86, 0.78),
    ],
    "D": [
        (0.12, 0.04, 0.12, 0.96),
        (0.12, 0.08, 0.62, 0.08),
        (0.62, 0.08, 0.86, 0.28),
        (0.86, 0.28, 0.86, 0.72),
        (0.86, 0.72, 0.62, 0.92),
        (0.12, 0.92, 0.62, 0.92),
    ],
    "E": [
        (0.14, 0.04, 0.14, 0.96),
        (0.14, 0.08, 0.88, 0.08),
        (0.14, 0.50, 0.74, 0.50),
        (0.14, 0.92, 0.88, 0.92),
    ],
    "F": [
        (0.14, 0.04, 0.14, 0.96),
        (0.14, 0.08, 0.88, 0.08),
        (0.14, 0.50, 0.74, 0.50),
    ],
    "G": [
        (0.86, 0.22, 0.68, 0.08),
        (0.68, 0.08, 0.30, 0.08),
        (0.30, 0.08, 0.12, 0.26),
        (0.12, 0.26, 0.12, 0.74),
        (0.12, 0.74, 0.30, 0.92),
        (0.30, 0.92, 0.72, 0.92),
        (0.72, 0.92, 0.86, 0.76),
        (0.86, 0.76, 0.86, 0.52),
        (0.52, 0.52, 0.86, 0.52),
    ],
    "H": [(0.14, 0.04, 0.14, 0.96), (0.86, 0.04, 0.86, 0.96), (0.14, 0.50, 0.86, 0.50)],
    "I": [(0.50, 0.04, 0.50, 0.96), (0.24, 0.08, 0.76, 0.08), (0.24, 0.92, 0.76, 0.92)],
    "K": [(0.14, 0.04, 0.14, 0.96), (0.86, 0.06, 0.20, 0.54), (0.34, 0.44, 0.88, 0.96)],
    "L": [(0.14, 0.04, 0.14, 0.96), (0.14, 0.92, 0.88, 0.92)],
    "M": [
        (0.10, 0.96, 0.10, 0.04),
        (0.10, 0.06, 0.50, 0.62),
        (0.90, 0.06, 0.50, 0.62),
        (0.90, 0.96, 0.90, 0.04),
    ],
    "N": [(0.12, 0.96, 0.12, 0.04), (0.12, 0.08, 0.88, 0.92), (0.88, 0.96, 0.88, 0.04)],
    "O": [
        (0.30, 0.08, 0.70, 0.08),
        (0.70, 0.08, 0.88, 0.26),
        (0.88, 0.26, 0.88, 0.74),
        (0.88, 0.74, 0.70, 0.92),
        (0.70, 0.92, 0.30, 0.92),
        (0.30, 0.92, 0.12, 0.74),
        (0.12, 0.74, 0.12, 0.26),
        (0.12, 0.26, 0.30, 0.08),
    ],
    "P": [
        (0.14, 0.04, 0.14, 0.96),
        (0.14, 0.08, 0.68, 0.08),
        (0.68, 0.08, 0.86, 0.22),
        (0.86, 0.22, 0.86, 0.40),
        (0.86, 0.40, 0.68, 0.54),
        (0.14, 0.54, 0.68, 0.54),
    ],
    "R": [
        (0.14, 0.04, 0.14, 0.96),
        (0.14, 0.08, 0.68, 0.08),
        (0.68, 0.08, 0.86, 0.22),
        (0.86, 0.22, 0.86, 0.38),
        (0.86, 0.38, 0.68, 0.52),
        (0.14, 0.52, 0.68, 0.52),
        (0.56, 0.52, 0.88, 0.96),
    ],
    "S": [
        (0.84, 0.20, 0.68, 0.08),
        (0.68, 0.08, 0.28, 0.08),
        (0.28, 0.08, 0.14, 0.22),
        (0.14, 0.22, 0.14, 0.38),
        (0.14, 0.38, 0.30, 0.48),
        (0.30, 0.48, 0.70, 0.52),
        (0.70, 0.52, 0.86, 0.64),
        (0.86, 0.64, 0.86, 0.80),
        (0.86, 0.80, 0.68, 0.92),
        (0.68, 0.92, 0.28, 0.92),
        (0.28, 0.92, 0.12, 0.78),
    ],
    "T": [(0.08, 0.08, 0.92, 0.08), (0.50, 0.08, 0.50, 0.96)],
    "U": [
        (0.14, 0.04, 0.14, 0.74),
        (0.14, 0.74, 0.30, 0.92),
        (0.30, 0.92, 0.70, 0.92),
        (0.70, 0.92, 0.86, 0.74),
        (0.86, 0.74, 0.86, 0.04),
    ],
    "V": [(0.10, 0.04, 0.50, 0.96), (0.90, 0.04, 0.50, 0.96)],
    "W": [
        (0.06, 0.04, 0.26, 0.96),
        (0.26, 0.96, 0.50, 0.34),
        (0.50, 0.34, 0.74, 0.96),
        (0.74, 0.96, 0.94, 0.04),
    ],
    "-": [(0.18, 0.50, 0.82, 0.50)],
    ".": [(0.44, 0.84, 0.56, 0.84)],
    "·": [(0.44, 0.50, 0.56, 0.50)],
    "&": [
        (0.72, 0.28, 0.56, 0.08),
        (0.56, 0.08, 0.30, 0.08),
        (0.30, 0.08, 0.18, 0.24),
        (0.18, 0.24, 0.24, 0.40),
        (0.24, 0.40, 0.84, 0.92),
        (0.72, 0.28, 0.16, 0.64),
        (0.16, 0.64, 0.16, 0.80),
        (0.16, 0.80, 0.34, 0.92),
        (0.34, 0.92, 0.66, 0.92),
        (0.66, 0.92, 0.88, 0.62),
    ],
    "0": [
        (0.30, 0.08, 0.70, 0.08),
        (0.70, 0.08, 0.86, 0.26),
        (0.86, 0.26, 0.86, 0.74),
        (0.86, 0.74, 0.70, 0.92),
        (0.70, 0.92, 0.30, 0.92),
        (0.30, 0.92, 0.14, 0.74),
        (0.14, 0.74, 0.14, 0.26),
        (0.14, 0.26, 0.30, 0.08),
    ],
    "1": [(0.26, 0.22, 0.52, 0.08), (0.52, 0.08, 0.52, 0.92), (0.24, 0.92, 0.78, 0.92)],
    "3": [
        (0.16, 0.16, 0.32, 0.08),
        (0.32, 0.08, 0.70, 0.08),
        (0.70, 0.08, 0.86, 0.24),
        (0.86, 0.24, 0.68, 0.48),
        (0.40, 0.48, 0.68, 0.48),
        (0.68, 0.48, 0.88, 0.66),
        (0.88, 0.66, 0.88, 0.80),
        (0.88, 0.80, 0.68, 0.92),
        (0.68, 0.92, 0.30, 0.92),
        (0.30, 0.92, 0.14, 0.80),
    ],
    "4": [
        (0.68, 0.06, 0.12, 0.66),
        (0.12, 0.66, 0.90, 0.66),
        (0.68, 0.06, 0.68, 0.96),
    ],
    "7": [(0.12, 0.08, 0.88, 0.08), (0.88, 0.08, 0.38, 0.96)],
    "8": [
        (0.30, 0.08, 0.70, 0.08),
        (0.70, 0.08, 0.84, 0.22),
        (0.84, 0.22, 0.84, 0.38),
        (0.84, 0.38, 0.70, 0.48),
        (0.30, 0.48, 0.70, 0.48),
        (0.30, 0.48, 0.16, 0.38),
        (0.16, 0.38, 0.16, 0.22),
        (0.16, 0.22, 0.30, 0.08),
        (0.70, 0.48, 0.88, 0.62),
        (0.88, 0.62, 0.88, 0.78),
        (0.88, 0.78, 0.70, 0.92),
        (0.70, 0.92, 0.30, 0.92),
        (0.30, 0.92, 0.12, 0.78),
        (0.12, 0.78, 0.12, 0.62),
        (0.12, 0.62, 0.30, 0.48),
    ],
    "9": [
        (0.30, 0.08, 0.70, 0.08),
        (0.70, 0.08, 0.86, 0.24),
        (0.86, 0.24, 0.86, 0.74),
        (0.86, 0.74, 0.68, 0.92),
        (0.68, 0.92, 0.28, 0.92),
        (0.86, 0.50, 0.30, 0.50),
        (0.30, 0.50, 0.14, 0.36),
        (0.14, 0.36, 0.14, 0.24),
        (0.14, 0.24, 0.30, 0.08),
    ],
}

STENCIL_BRIDGE_CHARS = {"A", "B", "D", "O", "P", "R", "0", "4", "8", "9", "&"}


def _draw_stencil_text(
    draw: ImageDraw.ImageDraw,
    text: str,
    cx: float,
    cy: float,
    char_w: float,
    char_h: float,
    stroke_w: float,
    spacing: float,
    color: tuple[int, int, int],
    bg_color: tuple[int, int, int],
    stencil_bridges: bool = True,
) -> None:
    n = len(text)
    if n == 0:
        return
    total_w = n * char_w + (n - 1) * spacing
    x_start = cx - total_w * 0.5
    y_top = cy - char_h * 0.5

    for i, ch in enumerate(text.upper()):
        gx = x_start + i * (char_w + spacing)
        if ch == " ":
            continue
        strokes = GLYPH_STROKES.get(ch, [])
        for sx0, sy0, sx1, sy1 in strokes:
            _draw_thick_seg(
                draw,
                gx + sx0 * char_w,
                y_top + sy0 * char_h,
                gx + sx1 * char_w,
                y_top + sy1 * char_h,
                stroke_w,
                color,
            )
        # Cut crisp military/aerospace stencil bridges across enclosed loops
        if stencil_bridges and ch in STENCIL_BRIDGE_CHARS:
            bridge_w = max(3.0, stroke_w * 0.36)
            mid_x = gx + char_w * 0.50
            draw.rectangle(
                [mid_x - bridge_w * 0.5, y_top - 2, mid_x + bridge_w * 0.5, y_top + char_h + 2],
                fill=bg_color,
            )


def _draw_lumen_logo(draw: ImageDraw.ImageDraw, cx: float, cy: float, radius: float) -> None:
    """Draw the circular Lumen Commission emblem matching identity_v1_helios_lumen.png."""
    # Outer dark navy circle
    r_out = radius
    draw.ellipse([cx - r_out, cy - r_out, cx + r_out, cy + r_out], fill=NAVY_DARK)

    # Outer coral-orange ring
    r1 = radius * 0.86
    draw.ellipse([cx - r1, cy - r1, cx + r1, cy + r1], fill=ORANGE_BAND)
    r1_in = radius * 0.76
    draw.ellipse([cx - r1_in, cy - r1_in, cx + r1_in, cy + r1_in], fill=NAVY_DARK)

    # Middle warm-amber ring
    r2 = radius * 0.68
    draw.ellipse([cx - r2, cy - r2, cx + r2, cy + r2], fill=AMBER_STRIPE)
    r2_in = radius * 0.56
    draw.ellipse([cx - r2_in, cy - r2_in, cx + r2_in, cy + r2_in], fill=NAVY_DARK)

    # Cream crescent moon inside the dark core
    r_moon = radius * 0.44
    draw.ellipse([cx - r_moon, cy - r_moon * 1.05, cx + r_moon, cy + r_moon * 0.95], fill=CREAM_TILE)
    # Cut out inner crescent shadow shifted upper-right
    r_cut = radius * 0.34
    cx_cut = cx + radius * 0.14
    cy_cut = cy - radius * 0.10
    draw.ellipse([cx_cut - r_cut, cy_cut - r_cut, cx_cut + r_cut, cy_cut + r_cut], fill=NAVY_DARK)

    # Lower arched horizon bands in navy + orange + amber across bottom quarter of circle
    horizon_y1 = cy + radius * 0.42
    horizon_y2 = cy + radius * 0.64
    draw.rectangle([cx - radius * 0.72, horizon_y1, cx + radius * 0.72, horizon_y1 + radius * 0.08], fill=NAVY_DARK)
    draw.rectangle(
        [cx - radius * 0.66, horizon_y1 + radius * 0.08, cx + radius * 0.66, horizon_y2],
        fill=AMBER_STRIPE,
    )
    draw.rectangle(
        [cx - radius * 0.56, horizon_y2 + radius * 0.04, cx + radius * 0.56, horizon_y2 + radius * 0.13],
        fill=ORANGE_BAND,
    )


def _draw_hazard_cell(canvas: Image.Image, x0: int, y0: int, x1: int, y1: int) -> None:
    """Draw 45-degree amber-yellow and dark-charcoal diagonal hazard stripes strictly inside [x0..x1, y0..y1]."""
    w = x1 - x0
    h = y1 - y0
    cell_img = Image.new("RGB", (w, h), color=AMBER_STRIPE)
    cell_draw = ImageDraw.Draw(cell_img)
    stripe_pitch = 64
    stripe_w = 32
    for offset in range(-h - stripe_pitch, w + h + stripe_pitch, stripe_pitch):
        poly = [
            (offset, h),
            (offset + stripe_w, h),
            (offset + stripe_w + h, 0),
            (offset + h, 0),
        ]
        cell_draw.polygon(poly, fill=HAZARD_DARK)
    border_h = 8
    cell_draw.rectangle([0, 0, w - 1, border_h], fill=HAZARD_DARK)
    cell_draw.rectangle([0, h - border_h, w - 1, h - 1], fill=HAZARD_DARK)
    cell_draw.rectangle([0, 0, border_h, h - 1], fill=HAZARD_DARK)
    canvas.paste(cell_img, (x0, y0))



def _draw_pictogram_tile_frame(draw: ImageDraw.ImageDraw, x0: int, y0: int, x1: int, y1: int) -> tuple[float, float, float]:
    """Draw the warm-orange stripe background and rounded cream pictogram badge frame."""
    draw.rectangle([x0, y0, x1, y1], fill=ORANGE_BAND)
    pad = 26
    bx0, by0, bx1, by1 = x0 + pad, y0 + pad, x1 - pad, y1 - pad
    draw.rounded_rectangle([bx0, by0, bx1, by1], radius=28, fill=CREAM_TILE, outline=STENCIL_DARK, width=8)
    cx = 0.5 * (bx0 + bx1)
    cy = 0.5 * (by0 + by1)
    size = (bx1 - bx0) * 0.68
    return cx, cy, size


def _draw_pictograms(draw: ImageDraw.ImageDraw, res: int) -> None:
    """Draw the 4 cassette-futurism pictograms in Row 0 (bottom row, y = 0.75*res .. res)."""
    y0 = int(res * 0.75)
    y1 = res
    cell_w = res // 4

    # 1. PICT_ROVER (U = 0.00..0.25)
    cx, cy, s = _draw_pictogram_tile_frame(draw, 0, y0, cell_w, y1)
    # Rover body & cab
    draw.rounded_rectangle(
        [cx - s * 0.42, cy - s * 0.06, cx + s * 0.42, cy + s * 0.22],
        radius=8,
        fill=STENCIL_DARK,
    )
    draw.rounded_rectangle(
        [cx + s * 0.02, cy - s * 0.30, cx + s * 0.36, cy - s * 0.02],
        radius=6,
        fill=STENCIL_DARK,
    )
    # Antenna mast on left rear
    _draw_thick_seg(draw, cx - s * 0.24, cy - s * 0.06, cx - s * 0.24, cy - s * 0.34, 7, STENCIL_DARK)
    draw.ellipse([cx - s * 0.29, cy - s * 0.40, cx - s * 0.19, cy - s * 0.30], fill=STENCIL_DARK)
    # Two wheels with cream hub cutouts
    for wx in (cx - s * 0.24, cx + s * 0.24):
        wy = cy + s * 0.26
        wr = s * 0.16
        draw.ellipse([wx - wr, wy - wr, wx + wr, wy + wr], fill=STENCIL_DARK)
        draw.ellipse([wx - wr * 0.42, wy - wr * 0.42, wx + wr * 0.42, wy + wr * 0.42], fill=CREAM_TILE)

    # 2. PICT_RADIO (U = 0.25..0.50)
    cx, cy, s = _draw_pictogram_tile_frame(draw, cell_w, y0, 2 * cell_w, y1)
    # A-frame lattice radio tower
    top_y = cy - s * 0.26
    bot_y = cy + s * 0.42
    _draw_thick_seg(draw, cx, top_y, cx - s * 0.24, bot_y, 8, STENCIL_DARK)
    _draw_thick_seg(draw, cx, top_y, cx + s * 0.24, bot_y, 8, STENCIL_DARK)
    _draw_thick_seg(draw, cx - s * 0.12, cy + s * 0.04, cx + s * 0.12, cy + s * 0.04, 6, STENCIL_DARK)
    _draw_thick_seg(draw, cx - s * 0.18, cy + s * 0.24, cx + s * 0.18, cy + s * 0.24, 6, STENCIL_DARK)
    draw.ellipse([cx - 8, top_y - 8, cx + 8, top_y + 8], fill=STENCIL_DARK)
    # Concentric radio wave arcs on left & right of mast tip
    for r_arc in (s * 0.20, s * 0.34):
        bbox = [cx - r_arc, top_y - r_arc, cx + r_arc, top_y + r_arc]
        draw.arc(bbox, start=-45, end=45, fill=STENCIL_DARK, width=7)
        draw.arc(bbox, start=135, end=225, fill=STENCIL_DARK, width=7)

    # 3. PICT_BOTANY (U = 0.50..0.75)
    cx, cy, s = _draw_pictogram_tile_frame(draw, 2 * cell_w, y0, 3 * cell_w, y1)
    # Arch greenhouse dome + horizontal ground baseline
    dome_r = s * 0.42
    base_y = cy + s * 0.30
    draw.pieslice(
        [cx - dome_r, base_y - dome_r * 1.65, cx + dome_r, base_y + dome_r * 0.2],
        start=180,
        end=360,
        outline=STENCIL_DARK,
        width=8,
    )
    _draw_thick_seg(draw, cx - dome_r * 1.1, base_y, cx + dome_r * 1.1, base_y, 8, STENCIL_DARK)
    # Central seedling stem + two symmetric leaves
    _draw_thick_seg(draw, cx, base_y, cx, cy - s * 0.12, 7, STENCIL_DARK)
    draw.polygon(
        [(cx, cy + s * 0.10), (cx - s * 0.22, cy - s * 0.02), (cx - s * 0.08, cy - s * 0.14)],
        fill=STENCIL_DARK,
    )
    draw.polygon(
        [(cx, cy + s * 0.10), (cx + s * 0.22, cy - s * 0.02), (cx + s * 0.08, cy - s * 0.14)],
        fill=STENCIL_DARK,
    )

    # 4. PICT_AIRLOCK (U = 0.75..1.00)
    cx, cy, s = _draw_pictogram_tile_frame(draw, 3 * cell_w, y0, 4 * cell_w, y1)
    # Airlock hatch portal frame on left + bold outward arrow on right
    px0, py0, px1, py1 = cx - s * 0.38, cy - s * 0.36, cx + s * 0.04, cy + s * 0.36
    draw.rounded_rectangle([px0, py0, px1, py1], radius=10, outline=STENCIL_DARK, width=8)
    # Inner angled door leaf
    draw.polygon(
        [(px0 + 6, py0 + 6), (px0 + s * 0.22, py0 + s * 0.12), (px0 + s * 0.22, py1 - s * 0.12), (px0 + 6, py1 - 6)],
        fill=STENCIL_DARK,
    )
    # Right-pointing EVA arrow
    _draw_thick_seg(draw, cx - s * 0.02, cy, cx + s * 0.34, cy, 12, STENCIL_DARK)
    draw.polygon(
        [(cx + s * 0.44, cy), (cx + s * 0.22, cy - s * 0.20), (cx + s * 0.22, cy + s * 0.20)],
        fill=STENCIL_DARK,
    )


def generate_signage_maps(res: int = 512, seed: int = 1988) -> dict[str, np.ndarray]:
    """Generate 512x512 Albedo, Normal, and Mask maps for R1_Signage."""
    hi_res = res * 2  # 1024x1024 supersampled canvas
    canvas = Image.new("RGB", (hi_res, hi_res), color=CREAM_BG)
    draw = ImageDraw.Draw(canvas)

    row_h = hi_res // 4  # 256 px per row at 1024x1024

    # -------------------------------------------------------------------------
    # Row 3 (Top row in PNG, y = 0 .. 256 -> UV V = 0.75 .. 1.00):
    #   LOGO (0..256), LUMEN (256..768), EVAC_1988 (768..1024)
    # -------------------------------------------------------------------------
    _draw_lumen_logo(draw, cx=128.0, cy=128.0, radius=104.0)

    # LUMEN title block (x = 256..768, y = 0..256)
    _draw_stencil_text(
        draw,
        "LUMEN",
        cx=512.0,
        cy=92.0,
        char_w=68.0,
        char_h=96.0,
        stroke_w=17.0,
        spacing=16.0,
        color=NAVY_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=False,
    )
    _draw_thick_seg(draw, 300.0, 162.0, 724.0, 162.0, 5.0, NAVY_DARK)
    _draw_stencil_text(
        draw,
        "ILP · EST. 1973",
        cx=512.0,
        cy=202.0,
        char_w=20.0,
        char_h=30.0,
        stroke_w=5.0,
        spacing=6.0,
        color=NAVY_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # EVAC 1988 stencil plaque (x = 768..1024, y = 0..256)
    draw.rounded_rectangle([784, 24, 1008, 232], radius=14, outline=STENCIL_DARK, width=6)
    draw.rectangle([796, 36, 1000, 62], fill=ORANGE_BAND)
    _draw_stencil_text(
        draw,
        "EVAC",
        cx=896.0,
        cy=110.0,
        char_w=38.0,
        char_h=54.0,
        stroke_w=10.0,
        spacing=9.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "1988",
        cx=896.0,
        cy=182.0,
        char_w=38.0,
        char_h=54.0,
        stroke_w=10.0,
        spacing=9.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # -------------------------------------------------------------------------
    # Row 2 (y = 256 .. 512 -> UV V = 0.50 .. 0.75):
    #   SEC04_HABITAT (0..512), SEC_04 (512..768), HABITAT_LAB (768..1024)
    # -------------------------------------------------------------------------
    # Combined two-line SEC-04 / HABITAT & LAB (matching identity_v1_helios_lumen.png top-right)
    _draw_stencil_text(
        draw,
        "SEC-04",
        cx=256.0,
        cy=338.0,
        char_w=58.0,
        char_h=82.0,
        stroke_w=14.0,
        spacing=12.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "HABITAT & LAB",
        cx=256.0,
        cy=438.0,
        char_w=26.0,
        char_h=44.0,
        stroke_w=8.0,
        spacing=7.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # Standalone SEC-04 (x = 512..768, y = 256..512)
    _draw_stencil_text(
        draw,
        "SEC-04",
        cx=640.0,
        cy=384.0,
        char_w=32.0,
        char_h=64.0,
        stroke_w=9.0,
        spacing=7.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # Standalone HABITAT & LAB (x = 768..1024, y = 256..512)
    _draw_stencil_text(
        draw,
        "HABITAT",
        cx=896.0,
        cy=342.0,
        char_w=24.0,
        char_h=46.0,
        stroke_w=7.5,
        spacing=6.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "& LAB",
        cx=896.0,
        cy=424.0,
        char_w=26.0,
        char_h=46.0,
        stroke_w=7.5,
        spacing=7.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # -------------------------------------------------------------------------
    # Row 1 (y = 512 .. 768 -> UV V = 0.25 .. 0.50):
    #   WORKSHOP (0..512), HAZARD (512..1024)
    # -------------------------------------------------------------------------
    draw.rounded_rectangle([18, 530, 494, 750], radius=14, outline=STENCIL_DARK, width=6)
    draw.rectangle([28, 540, 484, 570], fill=SAGE_ACCENT)
    _draw_stencil_text(
        draw,
        "WORKSHOP",
        cx=256.0,
        cy=636.0,
        char_w=42.0,
        char_h=72.0,
        stroke_w=12.0,
        spacing=10.0,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "SEC-04 ·LUM-07",
        cx=256.0,
        cy=716.0,
        char_w=18.0,
        char_h=28.0,
        stroke_w=5.0,
        spacing=5.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # Hazard stripes cell (x = 512..1024, y = 512..768)
    _draw_hazard_cell(canvas, 512, 512, 1024, 768)

    # -------------------------------------------------------------------------
    # Row 0 (Bottom row in PNG, y = 768 .. 1024 -> UV V = 0.00 .. 0.25):
    #   4 Pictogram tiles on warm orange band
    # -------------------------------------------------------------------------
    _draw_pictograms(draw, hi_res)

    # Downsample 1024 -> 512 with high-quality box/lanczos filter
    img_512 = canvas.resize((res, res), resample=Image.Resampling.LANCZOS)
    # Flip vertically so row 0 of the returned numpy array corresponds to V=0 (bottom),
    # matching generate_r1_textures._save_rgba_or_rgb which calls np.flipud() on save.
    albedo_topdown = np.asarray(img_512, dtype=np.float32) / 255.0
    albedo_v0 = np.flipud(albedo_topdown).copy()

    # Add subtle deterministic baked-enamel micro-grain & stencil paint wear
    rng = np.random.default_rng(seed)
    grain = rng.normal(0.0, 0.010, size=(res, res)).astype(np.float32)
    albedo_rgb = np.clip(albedo_v0 + grain[..., None], 0.0, 1.0)

    # Compute ink mask (1 where dark stencil ink or orange/amber paint is printed, 0 on cream base)
    lum = 0.299 * albedo_rgb[..., 0] + 0.587 * albedo_rgb[..., 1] + 0.114 * albedo_rgb[..., 2]
    ink_mask = np.clip((0.78 - lum) * 2.5, 0.0, 1.0)

    # Height map: subtle raised silkscreen ink & enamel grain -> tangent normal map
    height = 0.50 + 0.025 * ink_mask + grain * 0.6
    dh_dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    dh_dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    normal_strength = 3.2
    nx = -dh_dx * normal_strength
    ny = -dh_dy * normal_strength
    nz = np.ones_like(height, dtype=np.float32)
    inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    normal_rgb = np.stack(
        [(nx * inv_len) * 0.5 + 0.5, (ny * inv_len) * 0.5 + 0.5, (nz * inv_len) * 0.5 + 0.5],
        axis=-1,
    )

    # Mask map: R=Metallic (0.04), G=MicroWear, B=MicroCavity, A=Smoothness (0.64 on cream, 0.46 on matte ink)
    metallic = np.full((res, res), 0.04, dtype=np.float32)
    micro_wear = np.clip(0.50 + grain * 14.0, 0.0, 1.0)
    micro_cavity = np.clip(0.50 - grain * 12.0, 0.0, 1.0)
    smoothness = np.clip(0.64 - 0.18 * ink_mask + grain * 2.0, 0.20, 0.85)
    mask_rgba = np.stack([metallic, micro_wear, micro_cavity, smoothness], axis=-1)

    albedo_rgba = np.concatenate(
        [albedo_rgb, np.ones((res, res, 1), dtype=np.float32)],
        axis=-1,
    )
    return {
        "name": "Signage",
        "albedo": albedo_rgba,
        "normal": np.clip(normal_rgb, 0.0, 1.0),
        "mask": mask_rgba,
    }


def _save_png(path: Path, arr: np.ndarray) -> None:
    u8 = np.clip(np.rint(arr * 255.0), 0, 255).astype(np.uint8)
    mode = "RGBA" if u8.shape[-1] == 4 else "RGB"
    Image.fromarray(np.flipud(u8), mode=mode).save(path, compress_level=9)



def main() -> None:
    from pathlib import Path
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures"
    out_dir.mkdir(parents=True, exist_ok=True)
    maps = generate_signage_maps(512)
    _save_png(out_dir / "T_R1_Signage_Albedo.png", maps["albedo"])
    _save_png(out_dir / "T_R1_Signage_Normal.png", maps["normal"])
    _save_png(out_dir / "T_R1_Signage_Mask.png", maps["mask"])
    print(f"Saved T_R1_Signage_Albedo/Normal/Mask.png to {out_dir}")


if __name__ == "__main__":
    main()


