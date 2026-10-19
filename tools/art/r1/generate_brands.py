#!/usr/bin/env python3
"""
Deterministic generator for Lunarion R1 Maker Brands (Deliverables 1, 2, 4 of TASK.md):
  1. Clean production marks (2048x2048 RGBA transparent PNGs + 1-colour white mono versions):
     - Assets/_Project/Art/R1/Brands/ilc_mark.png, ilc_mark_mono.png
     - Assets/_Project/Art/R1/Brands/kessler_arai_mark.png, kessler_arai_mark_mono.png
     - Assets/_Project/Art/R1/Brands/tycho_mark.png, tycho_mark_mono.png
     - Assets/_Project/Art/R1/Brands/vis_mark.png, vis_mark_mono.png
     - Assets/_Project/Art/R1/Brands/vis_1967_mark.png, vis_1967_mark_mono.png
     Removes long descriptive sub-lines from concept sheets: mark + short wordmark only.

  2. Kessler-Arai CRT boot monoline linework (<= 12 single-width strokes in 0..1 space):
     - Assets/_Project/Art/R1/Brands/kessler_arai_crt_linework.json
     - Assets/_Project/Art/R1/Brands/kessler_arai_crt_linework_preview.png (1024x1024)
     - Logs/captures/kessler_arai_crt_linework_preview.png (1024x1024)

  4. Databank archive prints (1024x1024 off-white catalogue card with mark + one line of text):
     - Assets/_Project/Art/R1/Brands/print_ilc.png
     - Assets/_Project/Art/R1/Brands/print_kessler_arai.png
     - Assets/_Project/Art/R1/Brands/print_tycho.png
     - Assets/_Project/Art/R1/Brands/print_vis.png

Owner rule strictly enforced: ZERO clusters of identical dots/holes (trypophobia) in any motif.
"""

import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[2]
FONT_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Fonts"
BRANDS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Brands"
CAPTURES_DIR = REPO_ROOT / "Logs" / "captures"

# Canonical Lunarion & Brand Palettes (from docs/VISION.md & docs/design/tech-eras.md)
CREAM = (244, 230, 200, 255)          # #F4E6C8
CORAL = (255, 138, 91, 255)           # #FF8A5B
AMBER = (255, 181, 71, 255)           # #FFB547
RUST = (224, 109, 83, 255)            # #E06D53
EARTH_BLUE = (63, 167, 214, 255)      # #3FA7D6
SLATE_METAL = (94, 107, 125, 255)     # #5E6B7D
INDIGO_DEEP = (16, 20, 42, 255)       # #10142A
INDIGO_MID = (31, 36, 71, 255)        # #1F2447
WARM_DARK = (26, 20, 16, 255)         # #1A1410
WHITE = (255, 255, 255, 255)          # #FFFFFF
TRANSPARENT = (255, 255, 255, 0)

# Tycho Consortium Palette
TYCHO_WHITE = (240, 246, 247, 255)    # #F0F6F7
TYCHO_TEAL = (72, 158, 162, 255)      # #489EA2
TYCHO_TEAL_DARK = (42, 104, 110, 255) # #2A686E
TYCHO_TEAL_LIGHT = (112, 196, 200, 255)
TYCHO_PANEL = (16, 24, 28, 255)       # #10181C

# VIS Palette
VIS_STEEL = (108, 122, 140, 255)      # #6C7A8C
VIS_STEEL_DARK = (58, 68, 82, 255)    # #3A4452
VIS_PANEL_DARK = (20, 24, 32, 255)    # #141820
VIS_BRASS = (224, 166, 68, 255)       # #E0A644

# Supersampled 2048 production canvas: draw in 1024-unit coordinate space at scale=4 (4096x4096)
# and downsample with Lanczos to 2048x2048 for razor-sharp vector edges.
MARK_OUT_SIZE = 2048
MARK_SS = 4  # 4x relative to 1024 coordinate space -> 4096x4096 working canvas
W = 1024 * MARK_SS
H = 1024 * MARK_SS


def s(val: float) -> int:
    return int(round(val * MARK_SS))


def sf(val: float) -> float:
    return float(val * MARK_SS)


def load_font(name: str, size_1024: float, scale: float = MARK_SS) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    return ImageFont.truetype(str(path), int(round(size_1024 * scale)))


def draw_tracked_text(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple,
    tracking: float = 0.0,
    anchor_y: str = "center",
    scale: float = MARK_SS,
):
    """Draws horizontally centered text with explicit inter-character tracking (in 1024-space px)."""
    track_px = float(tracking * scale)
    advances = [font.getlength(ch) for ch in text]
    total_w = sum(advances) + track_px * max(0, len(text) - 1)
    bbox = font.getbbox(text)
    text_h = bbox[3] - bbox[1]
    x = float(cx * scale) - total_w / 2.0
    if anchor_y == "center":
        y = float(cy * scale) - bbox[1] - text_h / 2.0
    elif anchor_y == "top":
        y = float(cy * scale) - bbox[1]
    else:
        y = float(cy * scale) - bbox[3]

    for ch, adv in zip(text, advances):
        draw.text((x, y), ch, font=font, fill=fill)
        x += adv + track_px


def draw_thick_arc(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    rx: float,
    ry: float,
    deg_start: float,
    deg_end: float,
    thickness: float,
    fill: tuple,
    rot_deg: float = 0.0,
    round_caps: bool = False,
):
    """Draws a smooth polygon-ribbon elliptical arc with optional rotation and rounded caps."""
    steps = max(64, int(abs(deg_end - deg_start) * 1.5))
    r_half = sf(thickness) / 2.0
    c_rot = math.cos(math.radians(rot_deg))
    s_rot = math.sin(math.radians(rot_deg))
    cx_s, cy_s = sf(cx), sf(cy)
    rx_s, ry_s = sf(rx), sf(ry)

    outer_pts = []
    inner_pts = []
    centers = []
    for i in range(steps + 1):
        t = deg_start + (deg_end - deg_start) * (i / steps)
        rad = math.radians(t)
        px = rx_s * math.cos(rad)
        py = ry_s * math.sin(rad)
        tx = -rx_s * math.sin(rad)
        ty = ry_s * math.cos(rad)
        nx, ny = ty, -tx
        nlen = math.hypot(nx, ny) + 1e-9
        nx /= nlen
        ny /= nlen

        rx_pt = cx_s + px * c_rot - py * s_rot
        ry_pt = cy_s + px * s_rot + py * c_rot
        rnx = nx * c_rot - ny * s_rot
        rny = nx * s_rot + ny * c_rot

        centers.append((rx_pt, ry_pt))
        outer_pts.append((rx_pt + rnx * r_half, ry_pt + rny * r_half))
        inner_pts.append((rx_pt - rnx * r_half, ry_pt - rny * r_half))

    poly = outer_pts + inner_pts[::-1]
    draw.polygon(poly, fill=fill)
    if round_caps and centers:
        for cpt in (centers[0], centers[-1]):
            draw.ellipse(
                (cpt[0] - r_half, cpt[1] - r_half, cpt[0] + r_half, cpt[1] + r_half),
                fill=fill,
            )


def draw_4pt_star(draw: ImageDraw.ImageDraw, cx: float, cy: float, r_long: float, r_short: float, fill: tuple):
    pts = []
    for i in range(8):
        ang = math.radians(i * 45.0)
        r = sf(r_long if i % 2 == 0 else r_short)
        pts.append((sf(cx) + r * math.cos(ang), sf(cy) + r * math.sin(ang)))
    draw.polygon(pts, fill=fill)


def downsample_to(img_ss: Image.Image, target_size: int) -> Image.Image:
    return img_ss.resize((target_size, target_size), Image.Resampling.LANCZOS)


def to_white_mono(img_rgba: Image.Image) -> Image.Image:
    """
    Converts a rendered mono mask image (where positive stencil shapes are drawn with
    high RGB luminance and dark knockout cuts have low luminance or zero alpha) into
    a pure 1-colour white (255, 255, 255, alpha) RGBA image.
    """
    arr = np.array(img_rgba, dtype=np.float32)
    lum = (0.299 * arr[:, :, 0] + 0.587 * arr[:, :, 1] + 0.114 * arr[:, :, 2]) / 255.0
    alpha = arr[:, :, 3] / 255.0
    out_a = np.clip(lum * alpha * 255.0, 0.0, 255.0).astype(np.uint8)
    out = np.zeros_like(arr, dtype=np.uint8)
    out[:, :, 0] = 255
    out[:, :, 1] = 255
    out[:, :, 2] = 255
    out[:, :, 3] = out_a
    return Image.fromarray(out, mode="RGBA")


# ==============================================================================
# 1. ILC (Direction B: Round Seal + Short Wordmark)
# ==============================================================================

def build_ilc_mark(mono: bool = False, wordmark_color: tuple = None) -> Image.Image:
    """
    ILC Direction B clean production mark (mark + short wordmark only, no long sub-lines).
    When mono=True, draws positive stencil geometry in pure white against black/transparent knockout.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    bg_col = (0, 0, 0, 255) if mono else INDIGO_DEEP
    c_cream = WHITE if mono else CREAM
    c_blue = WHITE if mono else EARTH_BLUE
    c_coral = WHITE if mono else CORAL
    c_amber = WHITE if mono else AMBER
    c_slate = WHITE if mono else SLATE_METAL
    c_wm = WHITE if mono else (wordmark_color if wordmark_color is not None else CREAM)

    cx, cy = 512, 396
    r_outer = 292

    if not mono:
        d.ellipse((s(cx - r_outer), s(cy - r_outer), s(cx + r_outer), s(cy + r_outer)), fill=bg_col)
    draw_thick_arc(d, cx, cy, r_outer - 5, r_outer - 5, 0, 360, 12.0, c_cream)
    draw_thick_arc(d, cx, cy, r_outer - 26, r_outer - 26, 0, 360, 6.0, c_blue)

    r_inner = 252
    scene = Image.new("RGBA", (W, H), (0, 0, 0, 0) if mono else bg_col)
    sd = ImageDraw.Draw(scene)

    for side in (-1, 1):
        draw_thick_arc(sd, cx + side * 28, cy, 207, 207, -52 if side == 1 else 128, 52 if side == 1 else 232, 6.0, c_slate)
        draw_thick_arc(sd, cx + side * 12, cy, 226, 226, -62 if side == 1 else 118, 62 if side == 1 else 242, 4.2, c_blue)

    # Lower Earth Horizon Dome
    e_cy, e_r = cy + 300, 224
    sd.ellipse((s(cx - e_r), s(e_cy - e_r), s(cx + e_r), s(e_cy + e_r)), fill=c_blue)
    for m_rx in (65, 135):
        draw_thick_arc(sd, cx, e_cy, m_rx, e_r, 180, 360, 6.5, bg_col)
    draw_thick_arc(sd, cx, e_cy + 47, e_r + 12, 130, 190, 350, 6.5, bg_col)
    draw_thick_arc(sd, cx, e_cy, e_r, e_r, 180, 360, 8.5, c_cream)

    # Upper Center Full Moon Disc with crescent cutout
    m_cy, m_r = cy - 50, 104
    m_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    mld = ImageDraw.Draw(m_layer)
    mld.ellipse((s(cx - m_r), s(m_cy - m_r), s(cx + m_r), s(m_cy + m_r)), fill=c_cream)
    mld.ellipse((s(cx - m_r + 42), s(m_cy - m_r - 8), s(cx + m_r + 56), s(m_cy + m_r + 8)), fill=bg_col if mono else INDIGO_MID)
    m_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m_mask).ellipse((s(cx - m_r), s(m_cy - m_r), s(cx + m_r), s(m_cy + m_r)), fill=255)
    scene.paste(m_layer, (0, 0), m_mask)
    sd = ImageDraw.Draw(scene)
    draw_thick_arc(sd, cx, m_cy, m_r, m_r, 0, 360, 6.5, c_cream)

    # Orbital ring looping around the Moon
    draw_thick_arc(sd, cx, m_cy + 10, 198, 50, -35, 215, 17.0, bg_col, rot_deg=-14)
    draw_thick_arc(sd, cx, m_cy + 10, 198, 50, -35, 215, 9.0, c_amber, rot_deg=-14, round_caps=True)

    # Asymmetric Delta Ascent Chevron
    delta_outer = [
        (sf(cx), sf(cy - 222)),
        (sf(cx + 90), sf(cy + 122)),
        (sf(cx + 21), sf(cy + 52)),
        (sf(cx), sf(cy - 80)),
        (sf(cx - 21), sf(cy + 52)),
        (sf(cx - 90), sf(cy + 122)),
    ]
    delta_halo = [
        (sf(cx), sf(cy - 236)),
        (sf(cx + 104), sf(cy + 134)),
        (sf(cx), sf(cy + 68)),
        (sf(cx - 104), sf(cy + 134)),
    ]
    sd.polygon(delta_halo, fill=bg_col)
    sd.polygon(delta_outer, fill=c_coral)
    if not mono:
        delta_amber = [
            (sf(cx), sf(cy - 222)),
            (sf(cx + 90), sf(cy + 122)),
            (sf(cx + 50), sf(cy + 81)),
            (sf(cx), sf(cy - 135)),
        ]
        sd.polygon(delta_amber, fill=c_amber)

    # L4 & L5 astrometric 4-point stars
    draw_4pt_star(sd, cx - 158, cy - 108, 28, 7, c_amber)
    draw_4pt_star(sd, cx + 158, cy - 108, 28, 7, c_amber)

    seal_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(seal_mask).ellipse((s(cx - r_inner), s(cy - r_inner), s(cx + r_inner), s(cy + r_inner)), fill=255)
    img.paste(scene, (0, 0), seal_mask)
    d = ImageDraw.Draw(img)

    # Cardinal astrometric ticks on outer seal ring
    for ang_deg in (0, 90, 180, 270):
        rad = math.radians(ang_deg)
        x0 = cx + (r_outer - 26) * math.cos(rad)
        y0 = cy + (r_outer - 26) * math.sin(rad)
        x1 = cx + (r_outer + 10) * math.cos(rad)
        y1 = cy + (r_outer + 10) * math.sin(rad)
        d.line([(sf(x0), sf(y0)), (sf(x1), sf(y1))], fill=c_coral, width=s(8))

    # Short Wordmark Lockup (no long sub-lines)
    wy = 782
    f_mono = load_font("ChakraPetch-Bold.ttf", 104)
    draw_tracked_text(d, 512, wy, "I  L  C", f_mono, c_wm, tracking=20.0)
    for side in (-1, 1):
        for y_off, col, length in [(-22, c_coral, 185), (0, c_amber, 162), (22, c_blue, 140)]:
            x_inner = 512 + side * 224
            x_outer = x_inner + side * length
            d.line([(s(x_inner), s(wy + y_off)), (s(x_outer), s(wy + y_off))], fill=col, width=s(9))

    f_title = load_font("BeVietnamPro-SemiBold.ttf", 33)
    draw_tracked_text(d, 512, 896, "INTERNATIONAL LUNAR COMMISSION", f_title, c_wm, tracking=6.0)

    out = downsample_to(img, MARK_OUT_SIZE)
    return to_white_mono(out) if mono else out


# ==============================================================================
# 2. KESSLER-ARAI (Direction B: Rising Solar Disc & Twin Peaks + Short Wordmark)
# ==============================================================================

def build_kessler_mark(mono: bool = False, wordmark_color: tuple = None) -> Image.Image:
    """
    Kessler-Arai Direction B clean production mark (mark + short wordmark only).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    bg_col = (0, 0, 0, 255) if mono else WARM_DARK
    c_cream = WHITE if mono else CREAM
    c_coral = WHITE if mono else CORAL
    c_amber = WHITE if mono else AMBER
    c_wm = WHITE if mono else (wordmark_color if wordmark_color is not None else CREAM)
    c_wm_sub = WHITE if mono else (CORAL if wordmark_color is None else (196, 86, 46, 255))

    cx = 512
    chamf = 80
    bx0, by0, bx1, by1 = 128, 108, 896, 636
    if not mono:
        oct_pts = [
            (sf(bx0 + chamf), sf(by0)),
            (sf(bx1 - chamf), sf(by0)),
            (sf(bx1), sf(by0 + chamf)),
            (sf(bx1), sf(by1 - chamf)),
            (sf(bx1 - chamf), sf(by1)),
            (sf(bx0 + chamf), sf(by1)),
            (sf(bx0), sf(by1 - chamf)),
            (sf(bx0), sf(by0 + chamf)),
        ]
        d.polygon(oct_pts, fill=bg_col)

    # Heavy left & right C-clamp bumper brackets
    bw = 28
    for side in (-1, 1):
        x_edge = bx0 if side == -1 else bx1
        x_in = x_edge - side * (chamf + 28)
        clamp = [
            (sf(x_in), sf(by0)),
            (sf(x_edge - side * chamf), sf(by0)),
            (sf(x_edge), sf(by0 + chamf)),
            (sf(x_edge), sf(by1 - chamf)),
            (sf(x_edge - side * chamf), sf(by1)),
            (sf(x_in), sf(by1)),
            (sf(x_in), sf(by1 - bw)),
            (sf(x_edge - side * (chamf - 11)), sf(by1 - bw)),
            (sf(x_edge - side * bw), sf(by1 - chamf + 11)),
            (sf(x_edge - side * bw), sf(by0 + chamf - 11)),
            (sf(x_edge - side * (chamf - 11)), sf(by0 + bw)),
            (sf(x_in), sf(by0 + bw)),
        ]
        d.polygon(clamp, fill=c_coral)

    # Top and bottom tie-bars between clamps
    d.rectangle((s(bx0 + chamf + 42), s(by0 + 7), s(bx1 - chamf - 42), s(by0 + 22)), fill=c_cream)
    d.rectangle((s(bx0 + chamf + 42), s(by1 - 22), s(bx1 - chamf - 42), s(by1 - 7)), fill=c_cream)

    # Rising Solar Disc (Arai)
    sun_y, sun_r = 334, 150
    d.ellipse((s(cx - sun_r), s(sun_y - sun_r), s(cx + sun_r), s(sun_y + sun_r)), fill=c_amber)
    # Two horizontal 1980s scan-cuts across the sun disc
    d.rectangle((s(cx - sun_r - 12), s(sun_y + 20), s(cx + sun_r + 12), s(sun_y + 36)), fill=bg_col)
    d.rectangle((s(cx - sun_r - 12), s(sun_y + 56), s(cx + sun_r + 12), s(sun_y + 76)), fill=bg_col)

    # Twin-Peak Crater Ridge & Survey Path Chevron (Kessler)
    ridge_ko = [
        (sf(214), sf(574)),
        (sf(386), sf(336)),
        (sf(468), sf(420)),
        (sf(512), sf(376)),
        (sf(556), sf(420)),
        (sf(638), sf(336)),
        (sf(810), sf(574)),
    ]
    d.polygon(ridge_ko, fill=bg_col)

    left_peak = [
        (sf(232), sf(574)),
        (sf(388), sf(358)),
        (sf(486), sf(460)),
        (sf(432), sf(574)),
    ]
    d.polygon(left_peak, fill=c_cream)

    right_peak = [
        (sf(792), sf(574)),
        (sf(636), sf(358)),
        (sf(538), sf(460)),
        (sf(592), sf(574)),
    ]
    d.polygon(right_peak, fill=c_coral)

    road_pts = [
        (sf(462), sf(574)),
        (sf(499), sf(452)),
        (sf(525), sf(452)),
        (sf(562), sf(574)),
    ]
    d.polygon(road_pts, fill=c_amber)

    # Short Wordmark Lockup (KESSLER - ARAI / FIELD SYSTEMS only)
    f_main = load_font("ChakraPetch-Bold.ttf", 78)
    draw_tracked_text(d, 512, 742, "KESSLER \u2013 ARAI", f_main, c_wm, tracking=7.0)

    f_mid = load_font("ChakraPetch-Bold.ttf", 40)
    draw_tracked_text(d, 512, 846, "FIELD SYSTEMS", f_mid, c_wm_sub, tracking=10.0)
    for side in (-1, 1):
        for i, col in enumerate([c_coral, c_amber, c_wm]):
            bx = 512 + side * (268 + i * 36)
            stripe = [
                (sf(bx), sf(826)),
                (sf(bx + side * 22), sf(826)),
                (sf(bx + side * 9), sf(866)),
                (sf(bx - side * 13), sf(866)),
            ]
            d.polygon(stripe, fill=col)

    out = downsample_to(img, MARK_OUT_SIZE)
    return to_white_mono(out) if mono else out


# ==============================================================================
# 3. TYCHO CONSORTIUM (Direction B: Wide Teal Hexagon + Sounding Chevrons)
# ==============================================================================

def build_tycho_mark(mono: bool = False, wordmark_color: tuple = None) -> Image.Image:
    """
    Tycho Direction B clean production mark (mark + short wordmark only, no sub-lines).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    bg_col = (0, 0, 0, 255) if mono else TYCHO_PANEL
    c_white = WHITE if mono else TYCHO_WHITE
    c_teal = WHITE if mono else TYCHO_TEAL
    c_teal_dark = WHITE if mono else TYCHO_TEAL_DARK
    c_teal_light = WHITE if mono else TYCHO_TEAL_LIGHT
    c_wm = WHITE if mono else (wordmark_color if wordmark_color is not None else TYCHO_WHITE)
    c_rule = WHITE if mono else TYCHO_TEAL

    cx, cy = 512, 396
    hex_w, hex_h = 412, 270
    shield_pts = [
        (sf(cx - 230), sf(cy - hex_h)),
        (sf(cx + 230), sf(cy - hex_h)),
        (sf(cx + hex_w), sf(cy)),
        (sf(cx + 230), sf(cy + hex_h)),
        (sf(cx - 230), sf(cy + hex_h)),
        (sf(cx - hex_w), sf(cy)),
    ]
    if not mono:
        d.polygon(shield_pts, fill=bg_col)
    # Draw crisp hexagonal perimeter ribbon
    hex_in_w, hex_in_h = hex_w - 13, hex_h - 11
    shield_in_pts = [
        (sf(cx - 222), sf(cy - hex_in_h)),
        (sf(cx + 222), sf(cy - hex_in_h)),
        (sf(cx + hex_in_w), sf(cy)),
        (sf(cx + 222), sf(cy + hex_in_h)),
        (sf(cx - 222), sf(cy + hex_in_h)),
        (sf(cx - hex_in_w), sf(cy)),
    ]
    border_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(border_layer)
    bd.polygon(shield_pts, fill=c_teal)
    bd.polygon(shield_in_pts, fill=(0, 0, 0, 0))
    img.alpha_composite(border_layer)
    d = ImageDraw.Draw(img)

    # Inner frame lines along top and bottom flats
    d.line([(s(cx - 212), s(cy - hex_h + 25)), (s(cx + 212), s(cy - hex_h + 25))], fill=c_white, width=s(7))
    d.line([(s(cx - 212), s(cy + hex_h - 25)), (s(cx + 212), s(cy + hex_h - 25))], fill=c_teal_dark, width=s(7))

    # Three Nested Downward Half-Hexagonal Radar Sounding Chevrons
    datum_y = 322
    for span_x, depth_y, thickness, col in [
        (294, 280, 24, c_teal_dark),
        (226, 216, 24, c_teal),
        (158, 152, 24, c_teal_light),
    ]:
        w_flat = span_x * 0.46
        y_bot = datum_y + depth_y
        outer = [
            (sf(cx - span_x), sf(datum_y + 18)),
            (sf(cx - w_flat), sf(y_bot)),
            (sf(cx + w_flat), sf(y_bot)),
            (sf(cx + span_x), sf(datum_y + 18)),
            (sf(cx + span_x - thickness * 1.15), sf(datum_y + 18)),
            (sf(cx + w_flat - thickness * 0.6), sf(y_bot - thickness)),
            (sf(cx - w_flat + thickness * 0.6), sf(y_bot - thickness)),
            (sf(cx - span_x + thickness * 1.15), sf(datum_y + 18)),
        ]
        d.polygon(outer, fill=col)

    # Central vertical deep-sounding radar axis beam piercing through the chevrons
    d.rectangle((s(cx - 18), s(202), s(cx + 18), s(612)), fill=bg_col)
    d.rectangle((s(cx - 8), s(212), s(cx + 8), s(616)), fill=c_white)

    # Stark Clean White Lunar Rim Horizon Bar & Tycho Central Spire
    d.rectangle((s(cx - 336), s(datum_y - 13), s(cx + 336), s(datum_y + 13)), fill=c_white)

    spire_pts = [
        (sf(cx), sf(172)),
        (sf(cx + 64), sf(datum_y - 9)),
        (sf(cx - 64), sf(datum_y - 9)),
    ]
    d.polygon(spire_pts, fill=c_white)
    if not mono:
        d.polygon([(sf(cx), sf(200)), (sf(cx + 35), sf(datum_y - 9)), (sf(cx), sf(datum_y - 9))], fill=c_teal)

    # Short Wordmark Lockup (TYCHO CONSORTIUM only)
    f_main = load_font("ChakraPetch-Bold.ttf", 68)
    draw_tracked_text(d, 512, 764, "TYCHO CONSORTIUM", f_main, c_wm, tracking=8.0)
    d.rectangle((s(168), s(834), s(856), s(844)), fill=c_rule)

    out = downsample_to(img, MARK_OUT_SIZE)
    return to_white_mono(out) if mono else out


# ==============================================================================
# 4. VIS 1985 (Direction A: Ribbed-Cylinder Block Stencil + Short Wordmark)
# ==============================================================================

def draw_vis_monogram_a(
    d: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    col_v: tuple,
    col_i: tuple,
    col_s: tuple,
    col_bar: tuple,
    bg_cut: tuple,
):
    def pt(dx: float, dy: float) -> tuple:
        return (sf(cx + dx * scale), sf(cy + dy * scale))

    # V
    v_full = [
        pt(-188, -92),
        pt(-140, -92),
        pt(-120, 18),
        pt(-100, -92),
        pt(-52, -92),
        pt(-98, 92),
        pt(-142, 92),
    ]
    d.polygon(v_full, fill=col_v)
    d.polygon([pt(-125, 10), pt(-115, 10), pt(-115, 96), pt(-125, 96)], fill=bg_cut)

    # I
    d.polygon([pt(-24, -92), pt(24, -92), pt(24, -16), pt(-24, -16)], fill=col_i)
    d.polygon([pt(-24, 16), pt(24, 16), pt(24, 92), pt(-24, 92)], fill=col_i)

    # S
    c_cham = 28
    s_top = [
        pt(52 + c_cham, -92),
        pt(188, -92),
        pt(188, -52),
        pt(96, -52),
        pt(96, -18),
        pt(52, -18),
        pt(52, -92 + c_cham),
    ]
    d.polygon(s_top, fill=col_s)
    s_mid = [
        pt(64, -8),
        pt(176, -8),
        pt(188, 4),
        pt(188, 18),
        pt(52, 18),
        pt(52, -8),
    ]
    d.polygon(s_mid, fill=col_s)
    s_bot = [
        pt(144, 28),
        pt(188, 28),
        pt(188, 92 - c_cham),
        pt(188 - c_cham, 92),
        pt(52, 92),
        pt(52, 52),
        pt(144, 52),
    ]
    d.polygon(s_bot, fill=col_s)

    if col_bar is not None:
        d.polygon([pt(-36, -9), pt(36, -9), pt(36, 9), pt(-36, 9)], fill=col_bar)


def build_vis_mark(mono: bool = False, wordmark_color: tuple = None) -> Image.Image:
    """
    VIS Direction A clean production mark (ribbed cylinder VIS stadium + short wordmark only).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    bg_col = (0, 0, 0, 255) if mono else VIS_PANEL_DARK
    c_cream = WHITE if mono else CREAM
    c_coral = WHITE if mono else CORAL
    c_steel = WHITE if mono else VIS_STEEL_DARK
    c_wm = WHITE if mono else (wordmark_color if wordmark_color is not None else CREAM)

    # Outer ribbed-cylinder pressure module backing plate (y: 148..604)
    if not mono:
        d.rounded_rectangle((s(108), s(148), s(916), s(604)), radius=s(94), fill=bg_col)

    # Top and bottom external structural cylinder ribs (3 wide solid trapezoidal lugs)
    rib_col = c_cream if wordmark_color is None else VIS_STEEL_DARK
    for rx0, rx1 in [(242, 366), (450, 574), (658, 782)]:
        d.polygon(
            [(sf(rx0), sf(148)), (sf(rx0 + 15), sf(122)), (sf(rx1 - 15), sf(122)), (sf(rx1), sf(148))],
            fill=rib_col,
        )
        d.polygon(
            [(sf(rx0), sf(604)), (sf(rx0 + 15), sf(630)), (sf(rx1 - 15), sf(630)), (sf(rx1), sf(604))],
            fill=rib_col,
        )

    # Main stadium pressure-hull border + inner structural seal ring
    d.rounded_rectangle((s(116), s(156), s(908), s(596)), radius=s(88), outline=c_cream, width=s(13))
    d.rounded_rectangle((s(138), s(178), s(886), s(574)), radius=s(68), outline=c_coral, width=s(6))

    # Left and right bulkhead side-flange clamps
    for side_x, dx in [(138, 1), (886, -1)]:
        d.polygon(
            [
                (sf(side_x), sf(304)),
                (sf(side_x + 34 * dx), sf(320)),
                (sf(side_x + 34 * dx), sf(432)),
                (sf(side_x), sf(448)),
            ],
            fill=c_coral,
        )

    # Horizontal ribbed-cylinder grooves
    for ry in [232, 520]:
        d.line([(s(198), s(ry)), (s(826), s(ry))], fill=c_steel, width=s(6))

    # Monumental 3-letter 'VIS' stencil mark in center
    draw_vis_monogram_a(
        d,
        cx=512,
        cy=376,
        scale=1.32,
        col_v=c_cream,
        col_i=c_cream,
        col_s=c_cream,
        col_bar=c_coral,
        bg_cut=bg_col,
    )

    # Short 1985 Wordmark Lockup (VANGUARD INTERPLANETARY / STRUCTURES)
    f_main = load_font("ChakraPetch-Bold.ttf", 54)
    f_sub = load_font("ChakraPetch-Bold.ttf", 38)
    draw_tracked_text(d, 512, 718, "VANGUARD INTERPLANETARY", f_main, c_wm, tracking=6.0)

    if mono:
        # In 1-colour mono stencil, draw crisp outlined girder bar + positive white text
        d.rounded_rectangle((s(156), s(776), s(868), s(852)), radius=s(10), outline=WHITE, width=s(7))
        draw_tracked_text(d, 512, 814, "S T R U C T U R E S", f_sub, WHITE, tracking=9.0)
    else:
        d.rounded_rectangle((s(156), s(776), s(868), s(852)), radius=s(10), fill=c_coral)
        d.rectangle((s(156), s(806), s(182), s(822)), fill=bg_col)
        d.rectangle((s(842), s(806), s(868), s(822)), fill=bg_col)
        draw_tracked_text(d, 512, 814, "S T R U C T U R E S", f_sub, WARM_DARK, tracking=9.0)

    out = downsample_to(img, MARK_OUT_SIZE)
    return to_white_mono(out) if mono else out


# ==============================================================================
# 5. VIS 1967 (Scout-3 Era Emblem + Wordmark, clean without anatomy plates)
# ==============================================================================

def build_vis_1967_mark(mono: bool = False, wordmark_color: tuple = None) -> Image.Image:
    """
    VIS 1967 original Scout-3 soft-lander era emblem & wordmark (transparent background,
    mark + short wordmark + vector tail only, no framing boxes or anatomy callouts).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    bg_col = (0, 0, 0, 255) if mono else (24, 26, 36, 255)
    c_cream = WHITE if mono else CREAM
    c_brass = WHITE if mono else VIS_BRASS
    c_coral = WHITE if mono else CORAL
    c_wm_main = WHITE if mono else (wordmark_color if wordmark_color is not None else CREAM)
    c_wm_sub = WHITE if mono else ((142, 98, 28, 255) if wordmark_color is not None else VIS_BRASS)
    c_wm_corp = WHITE if mono else ((186, 74, 44, 255) if wordmark_color is not None else CORAL)

    cx, cy = 512, 356
    # Tilted thin 1960s orbital ring
    draw_thick_arc(d, cx, cy + 10, 316, 98, -165, 175, 9.0, c_brass, rot_deg=-16, round_caps=True)
    draw_thick_arc(d, cx, cy + 10, 288, 78, -140, 150, 4.0, c_coral, rot_deg=-16, round_caps=True)

    # Central 1967 5-sided Scout shield badge
    shield_pts = [
        (sf(512), sf(164)),
        (sf(756), sf(304)),
        (sf(680), sf(502)),
        (sf(344), sf(502)),
        (sf(268), sf(304)),
    ]
    if not mono:
        d.polygon(shield_pts, fill=bg_col)
    d.polygon(shield_pts, outline=c_cream if wordmark_color is None else (42, 38, 34, 255), width=s(8))

    inner_shield = [
        (sf(512), sf(184)),
        (sf(732), sf(312)),
        (sf(662), sf(484)),
        (sf(362), sf(484)),
        (sf(292), sf(312)),
    ]
    d.polygon(inner_shield, outline=c_brass, width=s(4))

    # Scout-3 three-legged lander silhouette struts at base of shield
    d.line([(s(414), s(434)), (s(364), s(494))], fill=c_brass, width=s(7))
    d.line([(s(610), s(434)), (s(660), s(494))], fill=c_brass, width=s(7))
    d.line([(s(512), s(434)), (s(512), s(496))], fill=c_brass, width=s(7))
    for fx in [364, 512, 660]:
        d.line([(s(fx - 22), s(496)), (s(fx + 22), s(496))], fill=c_coral, width=s(7))

    # 1967 Thin Double-Line Inline 'V - I - S' Monogram inside the shield
    for col, w_line in [(c_cream, 17), (bg_col, 5)]:
        d.line([(s(356), s(290)), (s(404), s(412)), (s(452), s(290))], fill=col, width=s(w_line), joint="curve")
    # I
    d.line([(s(482), s(290)), (s(542), s(290))], fill=c_cream, width=s(12))
    d.line([(s(482), s(412)), (s(542), s(412))], fill=c_cream, width=s(12))
    d.line([(s(512), s(290)), (s(512), s(412))], fill=c_cream, width=s(18))
    d.line([(s(512), s(300)), (s(512), s(402))], fill=bg_col, width=s(5))
    # S
    s67_pts = [
        (s(662), s(304)),
        (s(646), s(290)),
        (s(588), s(290)),
        (s(572), s(306)),
        (s(572), s(338)),
        (s(588), s(350)),
        (s(646), s(354)),
        (s(662), s(368)),
        (s(662), s(396)),
        (s(646), s(412)),
        (s(584), s(412)),
        (s(568), s(396)),
    ]
    d.line(s67_pts, fill=c_cream, width=s(17), joint="curve")
    d.line(s67_pts, fill=bg_col, width=s(5), joint="curve")

    # 4-pointed 1967 astrometric star at apex
    draw_4pt_star(d, 512, 224, 26, 7, c_brass)

    # 1967 Short Wordmark Lockup + Tapered Vector Tail
    f_w67_top = load_font("ChakraPetch-SemiBold.ttf", 46)
    f_w67_mid = load_font("ChakraPetch-Bold.ttf", 54)
    f_w67_corp = load_font("IBMPlexMono-SemiBold.ttf", 28)

    draw_tracked_text(d, 512, 616, "V A N G U A R D", f_w67_mid, c_wm_main, tracking=10.0)
    draw_tracked_text(d, 512, 688, "INTERPLANETARY STRUCTURES", f_w67_top, c_wm_sub, tracking=4.5)
    draw_tracked_text(d, 512, 754, "C O R P O R A T I O N", f_w67_corp, c_wm_corp, tracking=8.0)

    # Sweeping tapered vector tail + delta dart beneath wordmark
    tail_pts = [
        (sf(112), sf(810)),
        (sf(818), sf(803)),
        (sf(802), sf(786)),
        (sf(916), sf(807)),
        (sf(802), sf(828)),
        (sf(818), sf(812)),
        (sf(112), sf(817)),
    ]
    d.polygon(tail_pts, fill=c_wm_sub)

    out = downsample_to(img, MARK_OUT_SIZE)
    return to_white_mono(out) if mono else out



# ==============================================================================
# DELIVERABLE 2: KESSLER-ARAI CRT BOOT MONOLINE LINEWORK (<= 12 strokes)
# ==============================================================================

def build_ka_crt_linework_data() -> dict:
    """
    Constructs the ordered monoline polyline representation of the Kessler-Arai B mark
    in normalized 0..1 screen space (origin top-left, x right, y down), with 11 single-width
    strokes (<= 12 strokes limit) so 07's CRT face can draw it stroke by stroke at boot.
    """
    # Map reference 1024-space coordinates (x in 128..896, y in 108..636) into a centered
    # normalized 0..1 viewport box (x in 0.12..0.88, y in 0.14..0.68), leaving room below
    # for the boot text on 07's screen or centered when drawn standalone.
    def norm_pt(x_1024: float, y_1024: float) -> list:
        nx = (x_1024 - 512.0) / 1024.0 + 0.5
        ny = (y_1024 - 372.0) / 1024.0 + 0.5
        return [round(nx, 4), round(ny, 4)]

    # Solar disc center (512, 334), radius 142, with two horizontal scan cuts at y=354 and y=390
    sun_cx, sun_cy, sun_r = 512.0, 334.0, 142.0
    # Upper solar dome arc from angle 190 deg (left horizon above cuts) through 270 deg (top) to 350 deg (right)
    sun_arc_pts = []
    for i in range(25):
        deg = 188.0 + (352.0 - 188.0) * (i / 24.0)
        rad = math.radians(deg)
        sun_arc_pts.append(norm_pt(sun_cx + sun_r * math.cos(rad), sun_cy + sun_r * math.sin(rad)))

    strokes = [
        {
            "id": "left_bracket",
            "label": "Left Chamfered C-Clamp Bracket",
            "closed": False,
            "points": [
                norm_pt(236, 122),
                norm_pt(208, 122),
                norm_pt(142, 188),
                norm_pt(142, 556),
                norm_pt(208, 622),
                norm_pt(236, 622),
            ],
        },
        {
            "id": "right_bracket",
            "label": "Right Chamfered C-Clamp Bracket",
            "closed": False,
            "points": [
                norm_pt(788, 122),
                norm_pt(816, 122),
                norm_pt(882, 188),
                norm_pt(882, 556),
                norm_pt(816, 622),
                norm_pt(788, 622),
            ],
        },
        {
            "id": "top_tie_bar",
            "label": "Top Structural Tie Bar",
            "closed": False,
            "points": [
                norm_pt(256, 122),
                norm_pt(768, 122),
            ],
        },
        {
            "id": "bottom_tie_bar",
            "label": "Bottom Structural Tie Bar",
            "closed": False,
            "points": [
                norm_pt(256, 622),
                norm_pt(768, 622),
            ],
        },
        {
            "id": "solar_disc_arc",
            "label": "Rising Solar Disc Arc (Arai)",
            "closed": False,
            "points": sun_arc_pts,
        },
        {
            "id": "solar_scan_chord_upper",
            "label": "Solar Disc Upper Scan Chord",
            "closed": False,
            "points": [
                norm_pt(376, 344),
                norm_pt(648, 344),
            ],
        },
        {
            "id": "solar_scan_chord_lower",
            "label": "Solar Disc Lower Scan Chord",
            "closed": False,
            "points": [
                norm_pt(404, 376),
                norm_pt(620, 376),
            ],
        },
        {
            "id": "left_crater_peak",
            "label": "Left Crater Peak Ridge (Kessler)",
            "closed": True,
            "points": [
                norm_pt(232, 574),
                norm_pt(388, 358),
                norm_pt(486, 460),
                norm_pt(432, 574),
            ],
        },
        {
            "id": "right_crater_peak",
            "label": "Right Crater Peak Ridge (Arai)",
            "closed": True,
            "points": [
                norm_pt(792, 574),
                norm_pt(636, 358),
                norm_pt(538, 460),
                norm_pt(592, 574),
            ],
        },
        {
            "id": "survey_beam_left",
            "label": "Central Survey Path Left Edge",
            "closed": False,
            "points": [
                norm_pt(462, 574),
                norm_pt(499, 452),
                norm_pt(525, 452),
                norm_pt(562, 574),
            ],
        },
        {
            "id": "horizon_baseline",
            "label": "Crater Basin Datum Horizon",
            "closed": False,
            "points": [
                norm_pt(212, 574),
                norm_pt(812, 574),
            ],
        },
    ]

    assert len(strokes) <= 12, f"Expected <= 12 strokes, got {len(strokes)}"

    return {
        "brand": "kessler_arai",
        "direction": "B",
        "name": "Kessler-Arai CRT Boot Monoline Mark",
        "coordinateSpace": "0..1 normalized (origin top-left, x right, y down)",
        "strokeWidthNormalized": 0.0085,
        "strokeCount": len(strokes),
        "strokes": strokes,
    }


def render_ka_crt_linework_preview(data: dict) -> Image.Image:
    """
    Renders a 1024x1024 CRT amber phosphor preview of the monoline K-A B mark strokes
    being drawn on 07's CRT screen (matching R1CrtFace.shader palette and scanlines).
    """
    out_size = 1024
    frame = Image.new("RGBA", (out_size, out_size), (14, 11, 9, 255))
    fd = ImageDraw.Draw(frame)

    # Outer 07 visor hood bezel
    fd.rounded_rectangle((28, 44, 996, 980), radius=76, fill=(42, 33, 26, 255), outline=(92, 72, 54, 255), width=4)
    fd.rounded_rectangle((68, 56, 956, 86), radius=12, fill=(168, 82, 48, 255))

    sx0, sy0, sx1, sy1 = 62, 108, 962, 918
    sw, sh = sx1 - sx0, sy1 - sy0

    # Radial warm-black phosphor screen base (#20140A center -> #120B05 edge)
    yy, xx = np.mgrid[0:sh, 0:sw].astype(np.float32)
    nx = (xx / sw - 0.5) * 2.0
    ny = (yy / sh - 0.5) * 2.0
    r2 = np.clip(nx * nx * 0.85 + ny * ny * 0.85, 0.0, 1.4)
    vig = np.clip(1.0 - 0.55 * r2, 0.25, 1.0)

    base_rgb = np.zeros((sh, sw, 3), dtype=np.float32)
    base_rgb[:, :, 0] = 18.0 + (34.0 - 18.0) * vig
    base_rgb[:, :, 1] = 11.0 + (21.0 - 11.0) * vig
    base_rgb[:, :, 2] = 5.0 + (10.0 - 5.0) * vig

    # Draw monoline vector strokes at 3x supersampling for crisp CRT beam width
    ss = 3
    content_ss = Image.new("L", (sw * ss, sh * ss), 0)
    cd = ImageDraw.Draw(content_ss)

    # Map normalized 0..1 stroke coordinates into a 620x620 vector viewport centered in upper screen
    vp_size = 560 * ss
    vp_x0 = (sw * ss - vp_size) / 2.0
    vp_y0 = 26 * ss
    beam_w = max(2, int(round(data["strokeWidthNormalized"] * vp_size)))

    for stroke in data["strokes"]:
        pts = [(vp_x0 + pt[0] * vp_size, vp_y0 + pt[1] * vp_size) for pt in stroke["points"]]
        if stroke.get("closed", False):
            pts = pts + [pts[0]]
        cd.line(pts, fill=255, width=beam_w, joint="curve")
        # Round beam caps at vertices
        r_cap = beam_w / 2.0
        for px, py in pts:
            cd.ellipse((px - r_cap, py - r_cap, px + r_cap, py + r_cap), fill=255)

    content = content_ss.resize((sw, sh), Image.Resampling.LANCZOS)
    cd_1x = ImageDraw.Draw(content)

    f_hdr = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Bold.ttf"), 17)
    f_title = ImageFont.truetype(str(FONT_DIR / "ChakraPetch-Bold.ttf"), 32)
    f_sub = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Bold.ttf"), 18)

    cd_1x.text((38, 24), "KESSLER\u2013ARAI ROM BIOS v1983.4", font=f_hdr, fill=190)
    cd_1x.text((sw - 330, 24), f"VECTOR BOOT: {data['strokeCount']} STROKES", font=f_hdr, fill=190)
    cd_1x.line([(38, 54), (sw - 38, 54)], fill=120, width=2)

    headline = "KESSLER\u2013ARAI FIELD SYSTEMS \u00b7 SSR-5"
    hb = f_title.getbbox(headline)
    hw = hb[2] - hb[0]
    cd_1x.text(((sw - hw) // 2, 578), headline, font=f_title, fill=255)
    cd_1x.line([(68, 632), (sw - 68, 632)], fill=200, width=2)

    sub1 = "UNIT 07 \u00b7 S/N 3-0417 \u00b7 MONOLINE CRT BEAM TRACE [OK]"
    sb1 = f_sub.getbbox(sub1)
    cd_1x.text(((sw - (sb1[2] - sb1[0])) // 2, 654), sub1, font=f_sub, fill=220)

    sub2 = "MODE: CONTINUE \u00b7 WAKE ON CARRIER"
    sb2 = f_sub.getbbox(sub2)
    cd_1x.text(((sw - (sb2[2] - sb2[0])) // 2, 692), sub2, font=f_sub, fill=185)

    # Apply R1CrtFace.shader phosphor bloom + hot core + scanlines
    c_norm = np.array(content, dtype=np.float32) / 255.0
    halo_tight = np.array(content.filter(ImageFilter.GaussianBlur(radius=3.0)), dtype=np.float32) / 255.0
    halo_wide = np.array(content.filter(ImageFilter.GaussianBlur(radius=13.0)), dtype=np.float32) / 255.0

    phos_r = base_rgb[:, :, 0] + halo_wide * 200.0 * 0.55 + halo_tight * 255.0 * 0.65 + c_norm * 255.0
    phos_g = base_rgb[:, :, 1] + halo_wide * 106.0 * 0.55 + halo_tight * 174.0 * 0.65 + c_norm * 184.0 + (c_norm ** 3) * 65.0
    phos_b = base_rgb[:, :, 2] + halo_wide * 20.0 * 0.55 + halo_tight * 56.0 * 0.65 + c_norm * 68.0 + (c_norm ** 4) * 135.0

    scan_wave = 1.0 - 0.19 * (0.5 + 0.5 * np.cos(yy * (2.0 * math.pi / 3.0)))
    rolling_band = 1.0 + 0.07 * np.exp(-((yy - sh * 0.38) / 65.0) ** 2)
    mult = scan_wave * rolling_band

    scr_rgb = np.stack([phos_r * mult, phos_g * mult, phos_b * mult], axis=-1)
    scr_rgb = np.clip(scr_rgb, 0.0, 255.0).astype(np.uint8)
    scr_img = Image.fromarray(scr_rgb, mode="RGB")

    scr_mask = Image.new("L", (sw, sh), 0)
    ImageDraw.Draw(scr_mask).rounded_rectangle((0, 0, sw - 1, sh - 1), radius=54, fill=255)
    frame.paste(scr_img, (sx0, sy0), scr_mask)

    fd.rounded_rectangle((sx0, sy0, sx1, sy1), radius=54, outline=(120, 78, 36, 255), width=3)
    fd.text((68, 938), "07 CRT BOOT LINEWORK PREVIEW \u00b7 MONOLINE K-A MARK (11 STROKES)", font=f_hdr, fill=(185, 145, 95, 255))

    return frame.convert("RGB")


# ==============================================================================
# DELIVERABLE 4: DATABANK ARCHIVE PRINTS (1024x1024 off-white catalogue cards)
# ==============================================================================

def build_archive_print(mark_for_card: Image.Image, one_line_text: str, catalog_code: str, accent_col: tuple) -> Image.Image:
    """
    Renders a 1024x1024 off-white databank archive print (for docs/design/databank.md's
    Maker Cards): the chosen mark on a warm off-white archival catalogue card with one
    line of text.
    """
    out_size = 1024
    ss = 2
    cw, ch = out_size * ss, out_size * ss

    # Warm off-white archival card stock (#F3E7CC) with subtle procedural paper fiber warmth
    rng = np.random.default_rng(abs(hash(catalog_code)) % (2**32))
    paper = np.zeros((ch, cw, 3), dtype=np.float32)
    yy, xx = np.mgrid[0:ch, 0:cw].astype(np.float32)
    nx = (xx / cw - 0.5) * 2.0
    ny = (yy / ch - 0.5) * 2.0
    vig = 1.0 - 0.045 * (nx * nx + ny * ny)
    grain = rng.normal(0.0, 1.8, size=(ch, cw)).astype(np.float32)

    paper[:, :, 0] = np.clip(243.0 * vig + grain, 220, 252)
    paper[:, :, 1] = np.clip(231.0 * vig + grain * 0.9, 208, 244)
    paper[:, :, 2] = np.clip(204.0 * vig + grain * 0.7, 180, 224)

    card = Image.fromarray(paper.astype(np.uint8), mode="RGB").convert("RGBA")
    d = ImageDraw.Draw(card)

    ink_dark = (26, 22, 18, 255)
    ink_muted = (92, 84, 72, 255)

    # Outer archival card frame border (in 2048 ss space)
    m = 36 * ss
    d.rectangle((m, m, cw - m, ch - m), outline=ink_dark, width=3 * ss)
    d.rectangle((m + 10 * ss, m + 10 * ss, cw - m - 10 * ss, ch - m - 10 * ss), outline=(186, 172, 148, 255), width=1 * ss)

    # Top-left and top-right tiny archival index micro-header
    f_idx = load_font("IBMPlexMono-Medium.ttf", 14, scale=ss)
    d.text((64 * ss, 56 * ss), f"ARCHIVE PRINT // {catalog_code}", font=f_idx, fill=ink_muted)
    d.text((cw - 290 * ss, 56 * ss), "ILC-R DATABANK SPEC", font=f_idx, fill=ink_muted)
    d.line([(64 * ss, 84 * ss), (cw - 64 * ss, 84 * ss)], fill=(192, 178, 154, 255), width=2 * ss)

    # Composite the mark in the upper-center of the card (740x740 at 1024-scale -> 1480x1480 in ss)
    mark_size = 740 * ss
    mark_resized = mark_for_card.resize((mark_size, mark_size), Image.Resampling.LANCZOS)
    mx = (cw - mark_size) // 2
    my = 76 * ss
    card.alpha_composite(mark_resized, (mx, my))
    d = ImageDraw.Draw(card)

    # Bottom archival rule bar + accent colour bar
    rule_y = 848 * ss
    d.rectangle((72 * ss, rule_y, 220 * ss, rule_y + 6 * ss), fill=accent_col)
    d.rectangle((232 * ss, rule_y, cw - 72 * ss, rule_y + 6 * ss), fill=ink_dark)

    # The one line of text from databank.md / databank-entries.md
    f_line = load_font("IBMPlexMono-Bold.ttf", 21.5, scale=ss)
    draw_tracked_text(d, 512, 908, one_line_text, f_line, ink_dark, tracking=1.6, scale=ss)

    return card.resize((out_size, out_size), Image.Resampling.LANCZOS).convert("RGB")


# ==============================================================================
# DELIVERABLE 5 (Round 3): MAKER DECAL ATLAS (T_R1_MakerDecals_Albedo.png)
# ==============================================================================

def build_vis_stencil_word() -> Image.Image:
    """
    Cell 2: Bold 'VIS' spacecraft hull stencil wordmark with transparent stencil-bridge cuts
    and faded coral/charcoal registration bars.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    col_ink = (34, 38, 46, 255)
    col_coral = CORAL

    # Top and bottom broken stencil alignment bars (with wide stencil bridge gaps)
    for by in (244, 780):
        for x0, x1 in ((128, 368), (416, 608), (656, 896)):
            d.rectangle((s(x0), s(by - 12), s(x1), s(by + 12)), fill=col_coral if by == 244 else col_ink)

    # Bold monumental 'VIS' stencil letters in the center (y: 300..724)
    draw_vis_monogram_a(
        d,
        cx=512,
        cy=512,
        scale=2.05,
        col_v=col_ink,
        col_i=col_ink,
        col_s=col_ink,
        col_bar=col_coral,
        bg_cut=(0, 0, 0, 0),
    )

    return downsample_to(img, MARK_OUT_SIZE)


def _weather_decal_cell(
    mark_2048: Image.Image,
    cell_size: int,
    seed: int,
    sun_fade: float = 0.24,
    edge_wear: float = 0.42,
    scorch_corner: bool = False,
) -> Image.Image:
    """
    Downsample a 2048x2048 mark into `cell_size x cell_size` and bake realistic lunar UV sun-bleaching,
    directional brush-scuffed edges, and crisp angular/linear paint chips into RGB & alpha
    (specifically tuned for URP Lit _AlphaClip = 1, _Cutoff = 0.38).
    Strictly zero circular dots or hole clusters.
    """
    # Unpremultiply RGB before Lanczos downsampling so dark wordmarks on transparent backgrounds
    # do not get darkened/frayed by transparent black (0,0,0,0) neighbours, then boost thin-stroke alpha
    src_arr = np.array(mark_2048, dtype=np.float32)
    cell = mark_2048.resize((cell_size, cell_size), Image.Resampling.LANCZOS).convert("RGBA")
    _ = src_arr

    # 1. Draw crisp angular/linear paint-flake scratches onto an abrasion mask
    scratch_mask = Image.new("L", (cell_size, cell_size), 0)
    sd = ImageDraw.Draw(scratch_mask)
    rng = np.random.default_rng(seed)

    # Diagonal & horizontal linear scrape lines (aspect ratio >> 8:1, never dots)
    for _ in range(32):
        x0 = float(rng.uniform(28, cell_size - 28))
        y0 = float(rng.uniform(28, cell_size - 28))
        ang = float(rng.choice([-0.36, -0.20, 0.0, 0.19, 0.35]) + rng.uniform(-0.07, 0.07))
        length = float(rng.uniform(22.0, 64.0))
        width = int(rng.integers(2, 5))
        x1 = x0 + length * math.cos(ang)
        y1 = y0 + length * math.sin(ang)
        sd.line([(x0, y0), (x1, y1)], fill=int(rng.integers(180, 255)), width=width)

    # Small elongated angular polygon paint chips (3-4 vertex shards with length >= 3.5x width)
    for _ in range(22):
        cx = float(rng.uniform(34, cell_size - 34))
        cy = float(rng.uniform(34, cell_size - 34))
        ang = float(rng.uniform(-0.65, 0.65))
        ca, sa = math.cos(ang), math.sin(ang)
        hl = float(rng.uniform(10.0, 22.0))
        hw = float(rng.uniform(2.0, 3.8))
        shard = [
            (cx - hl * ca, cy - hl * sa),
            (cx + 0.2 * hl * ca - hw * sa, cy + 0.2 * hl * sa + hw * ca),
            (cx + hl * ca, cy + hl * sa),
            (cx - 0.3 * hl * ca + hw * sa, cy - 0.3 * hl * sa - hw * ca),
        ]
        sd.polygon(shard, fill=255)

    arr = np.array(cell, dtype=np.float32)
    rgb = arr[:, :, :3]
    raw_alpha = arr[:, :, 3] / 255.0

    # Boost anti-aliased text/line alpha so fine wordmarks survive _AlphaClip (_Cutoff=0.38) crisply
    alpha = np.clip((raw_alpha - 0.08) * 1.45, 0.0, 1.0)
    # Restore clean dark ink RGB on partially transparent edge pixels where Lanczos mixed with (0,0,0,0)
    valid_a = np.maximum(raw_alpha, 1e-3)
    for c_i in range(3):
        rgb[:, :, c_i] = np.where(raw_alpha > 0.05, np.clip(rgb[:, :, c_i] / np.sqrt(valid_a), 0.0, 255.0), 0.0)

    yy, xx = np.mgrid[0:cell_size, 0:cell_size].astype(np.float32)
    nx = xx / float(cell_size)
    ny = yy / float(cell_size)

    # 2. Sun-faded colour: directional UV bleaching + smooth multi-harmonic wave mottling
    wave_fade = (
        0.50
        + 0.25 * np.sin(nx * 11.0 + 0.7 * seed) * np.cos(ny * 9.0 - 0.4 * seed)
        + 0.25 * np.cos((nx * 0.8 - ny * 0.6) * 17.0 + 1.1 * seed)
    )
    uv_exposure = np.clip(1.05 - 0.38 * ny - 0.15 * nx, 0.45, 1.0)
    fade_factor = np.clip(sun_fade * (0.55 * uv_exposure + 0.45 * wave_fade), 0.08, 0.48)

    # Warm sun-bleached lunar chalk target colour (applied more strongly to bright/mid tones while keeping dark ink legible)
    chalk = np.array([232.0, 220.0, 196.0], dtype=np.float32)
    lum = 0.299 * rgb[:, :, 0] + 0.587 * rgb[:, :, 1] + 0.114 * rgb[:, :, 2]
    # Keep dark wordmark ink dark enough (around #353840) to contrast against R1_CreamEnamel while fading badges & brights
    ink_preserve = np.clip((lum - 20.0) / 90.0, 0.42, 1.0)
    eff_fade = fade_factor * ink_preserve

    for c_i in range(3):
        desat = rgb[:, :, c_i] * (1.0 - 0.20 * sun_fade) + lum * (0.20 * sun_fade)
        rgb[:, :, c_i] = desat * (1.0 - eff_fade) + chalk[c_i] * eff_fade

    # Directional linear brush-scuff streaks across the paint surface
    streak_coord_1 = nx * math.cos(0.27) + ny * math.sin(0.27)
    streak_coord_2 = nx * math.cos(-0.19) + ny * math.sin(-0.19)
    linear_scuff = (
        0.5
        + 0.30 * np.sin(streak_coord_1 * 95.0 + seed * 1.7)
        + 0.20 * np.cos(streak_coord_2 * 145.0 - seed * 2.3)
    )
    scuff_tint = np.clip((linear_scuff - 0.60) * 34.0, -8.0, 16.0)
    for c_i in range(3):
        rgb[:, :, c_i] = np.clip(rgb[:, :, c_i] + scuff_tint, 0.0, 255.0)

    # 3. Crisp edge scuffing & linear chips in alpha (protecting lower wordmark zone ny > 0.66 from heavy alpha holes)
    a_img = Image.fromarray(np.clip(alpha * 255.0, 0.0, 255.0).astype(np.uint8), mode="L")
    a_blur_tight = np.array(a_img.filter(ImageFilter.GaussianBlur(radius=2.2)), dtype=np.float32) / 255.0
    a_blur_wide = np.array(a_img.filter(ImageFilter.GaussianBlur(radius=5.5)), dtype=np.float32) / 255.0

    # Protect thin wordmark text in the lower region (ny > 0.56) so letters stay 100% readable
    emblem_zone = np.clip((0.58 - ny) / 0.08, 0.12, 1.0)

    edge_band = np.clip((alpha > 0.05) * (1.0 - a_blur_tight * 0.80), 0.0, 1.0) * emblem_zone
    outer_halo_band = np.clip((alpha > 0.05) * (1.0 - a_blur_wide * 0.62), 0.0, 1.0) * emblem_zone

    edge_wave = (
        0.50
        + 0.24 * np.sin((nx * 31.0 + ny * 13.0) + seed * 0.9)
        + 0.16 * np.cos((nx * 17.0 - ny * 33.0) - seed * 1.4)
        + 0.10 * np.sin((nx * 55.0 + ny * 7.0) + seed * 2.1)
    )
    scratches = np.array(scratch_mask, dtype=np.float32) / 255.0

    # Crisp alpha-clip cutouts for edge scuffs and linear scratch chips
    edge_cut = (edge_band * edge_wave) > (0.36 - 0.16 * edge_wear)
    chip_cut = (scratches * (0.30 + 0.70 * outer_halo_band) * emblem_zone) > (0.58 - 0.28 * edge_wear)
    cut_mask = np.logical_or(edge_cut, chip_cut).astype(np.float32)

    # Also add surface paint-scuff lightening right around the chips/scratches
    scuff_halo = np.clip(scratches * 0.65 + edge_band * edge_wave * 0.55, 0.0, 1.0)
    for c_i in range(3):
        rgb[:, :, c_i] = np.clip(rgb[:, :, c_i] * (1.0 - 0.18 * scuff_halo) + chalk[c_i] * (0.18 * scuff_halo), 0.0, 255.0)

    new_alpha = alpha * (1.0 - 0.92 * cut_mask)

    # 4. Optional thermal scorch / plasma-cut corner (for Tycho mark on Kestrel_HullPanel)
    if scorch_corner:
        # Diagonal plasma-cut & char zone across the upper-right corner of the hexagon emblem
        scorch_dist = (nx - 0.52) * 0.78 - (ny - 0.44) * 0.62
        scorch_wave = 0.035 * np.sin(ny * 27.0 + 1.1) + 0.025 * np.cos(nx * 23.0 - 0.7)
        s_val = scorch_dist + scorch_wave
        # Dark soot/char gradient on the surviving paint before the cut line
        scorch_zone = np.clip((s_val - 0.02) * 4.8, 0.0, 1.0)
        soot_col = np.array([28.0, 22.0, 18.0], dtype=np.float32)
        amber_char = np.array([148.0, 74.0, 32.0], dtype=np.float32)
        char_rim = np.clip(1.0 - np.abs(s_val - 0.17) * 18.0, 0.0, 1.0)
        for c_i in range(3):
            rgb[:, :, c_i] = rgb[:, :, c_i] * (1.0 - 0.88 * scorch_zone) + soot_col[c_i] * (0.88 * scorch_zone)
            rgb[:, :, c_i] = rgb[:, :, c_i] * (1.0 - 0.55 * char_rim) + amber_char[c_i] * (0.55 * char_rim)
        # Sharp plasma-cut notch severing the upper-right corner of the hexagon (s_val > 0.20)
        burn_cut = (s_val > 0.20).astype(np.float32)
        new_alpha = new_alpha * (1.0 - burn_cut)

    # Enforce generous transparent border inside the cell itself (outer 12 px of the cell is strictly 0 alpha)
    border = 12
    new_alpha[:border, :] = 0.0
    new_alpha[-border:, :] = 0.0
    new_alpha[:, :border] = 0.0
    new_alpha[:, -border:] = 0.0

    out_a = np.clip(new_alpha * 255.0, 0.0, 255.0).astype(np.uint8)
    out_rgb = np.clip(rgb, 0.0, 255.0).astype(np.uint8)
    out_rgba = np.dstack([out_rgb, out_a])
    out_rgba[out_a < 8] = (0, 0, 0, 0)
    return Image.fromarray(out_rgba, mode="RGBA")


def build_maker_decals_atlas() -> Image.Image:
    """
    Build `Assets/_Project/Art/R1/Brands/T_R1_MakerDecals_Albedo.png` (2048x2048 RGBA)
    containing a clean 3x2 grid of ONLY these 6 cells:
      1. VIS 1985 mark        (col 0, row 0 - top-left)
      2. "VIS" stencil word   (col 1, row 0 - top-center)
      3. VIS 1967 wordmark    (col 2, row 0 - top-right)
      4. ILC seal             (col 0, row 1 - bottom-left)
      5. Tycho mark           (col 1, row 1 - bottom-center)
      6. Kessler-Arai mark    (col 2, row 1 - bottom-right)
    Each cell is 552x552 px centered in a 682x1024 grid slot -> >=130 px horizontal and
    >=472 px vertical transparent gap between cells (16+ transparent texels at mip level 3).
    """
    atlas = Image.new("RGBA", (2048, 2048), (0, 0, 0, 0))
    cell_size = 552

    ink_dark = (30, 32, 38, 255)
    cells = [
        # (col, row, mark_img, seed, sun_fade, edge_wear, scorch_corner)
        (0, 0, build_vis_mark(mono=False, wordmark_color=ink_dark), 101, 0.22, 0.42, False),
        (1, 0, build_vis_stencil_word(), 202, 0.24, 0.48, False),
        (2, 0, build_vis_1967_mark(mono=False, wordmark_color=(38, 32, 26, 255)), 303, 0.32, 0.52, False),
        (0, 1, build_ilc_mark(mono=False, wordmark_color=ink_dark), 404, 0.22, 0.40, False),
        (1, 1, build_tycho_mark(mono=False, wordmark_color=(24, 32, 36, 255)), 505, 0.24, 0.48, True),
        (2, 1, build_kessler_mark(mono=False, wordmark_color=ink_dark), 606, 0.20, 0.38, False),
    ]

    col_centers = (342, 1024, 1706)
    row_centers = (512, 1536)

    for col, row, mark_img, seed, sun_fade, edge_wear, scorch in cells:
        weathered = _weather_decal_cell(
            mark_img,
            cell_size=cell_size,
            seed=seed,
            sun_fade=sun_fade,
            edge_wear=edge_wear,
            scorch_corner=scorch,
        )
        cx = col_centers[col]
        cy = row_centers[row]
        atlas.alpha_composite(weathered, (cx - cell_size // 2, cy - cell_size // 2))

    return atlas


def main():
    import sys

    BRANDS_DIR.mkdir(parents=True, exist_ok=True)
    CAPTURES_DIR.mkdir(parents=True, exist_ok=True)

    if "--only-decal-atlas" in sys.argv:
        print("[generate_brands] Building 2048x2048 T_R1_MakerDecals_Albedo.png...")
        decal_atlas = build_maker_decals_atlas()
        decal_atlas.save(BRANDS_DIR / "T_R1_MakerDecals_Albedo.png")
        print("[generate_brands] Done!")
        return

    # 1. Clean 2048x2048 production marks + 1-colour white mono versions
    print("[generate_brands] Building 2048x2048 colour and mono marks...")
    ilc_col = build_ilc_mark(mono=False)
    ilc_mono = build_ilc_mark(mono=True)
    ilc_col.save(BRANDS_DIR / "ilc_mark.png")
    ilc_mono.save(BRANDS_DIR / "ilc_mark_mono.png")

    ka_col = build_kessler_mark(mono=False)
    ka_mono = build_kessler_mark(mono=True)
    ka_col.save(BRANDS_DIR / "kessler_arai_mark.png")
    ka_mono.save(BRANDS_DIR / "kessler_arai_mark_mono.png")

    tycho_col = build_tycho_mark(mono=False)
    tycho_mono = build_tycho_mark(mono=True)
    tycho_col.save(BRANDS_DIR / "tycho_mark.png")
    tycho_mono.save(BRANDS_DIR / "tycho_mark_mono.png")

    vis_col = build_vis_mark(mono=False)
    vis_mono = build_vis_mark(mono=True)
    vis_col.save(BRANDS_DIR / "vis_mark.png")
    vis_mono.save(BRANDS_DIR / "vis_mark_mono.png")

    vis67_col = build_vis_1967_mark(mono=False)
    vis67_mono = build_vis_1967_mark(mono=True)
    vis67_col.save(BRANDS_DIR / "vis_1967_mark.png")
    vis67_mono.save(BRANDS_DIR / "vis_1967_mark_mono.png")

    # 2. Kessler-Arai CRT boot monoline linework (<= 12 strokes) + preview PNG
    print("[generate_brands] Building Kessler-Arai CRT boot monoline polyline asset & preview...")
    linework_data = build_ka_crt_linework_data()
    json_str = json.dumps(linework_data, indent=2) + "\n"
    (BRANDS_DIR / "kessler_arai_crt_linework.json").write_text(json_str, encoding="utf-8")

    crt_preview = render_ka_crt_linework_preview(linework_data)
    crt_preview.save(BRANDS_DIR / "kessler_arai_crt_linework_preview.png")
    crt_preview.save(CAPTURES_DIR / "kessler_arai_crt_linework_preview.png")

    # 4. Databank archive prints (1024x1024 off-white catalogue cards with one line of text)
    print("[generate_brands] Building 1024x1024 databank archive prints...")
    ink_wordmark = (24, 22, 20, 255)
    ilc_card_mark = build_ilc_mark(mono=False, wordmark_color=ink_wordmark)
    ka_card_mark = build_kessler_mark(mono=False, wordmark_color=ink_wordmark)
    tycho_card_mark = build_tycho_mark(mono=False, wordmark_color=ink_wordmark)
    vis_card_mark = build_vis_mark(mono=False, wordmark_color=ink_wordmark)

    print_ilc = build_archive_print(
        ilc_card_mark,
        "INTERNATIONAL LUNAR COMMISSION \u00b7 ILC \u00b7 EST. 1969",
        "MKR-01 // ILC",
        EARTH_BLUE,
    )
    print_ilc.save(BRANDS_DIR / "print_ilc.png")

    print_ka = build_archive_print(
        ka_card_mark,
        "KESSLER\u2013ARAI FIELD SYSTEMS \u00b7 FIELD ROBOTICS \u00b7 SSR SERIES",
        "MKR-02 // K-A",
        CORAL,
    )
    print_ka.save(BRANDS_DIR / "print_kessler_arai.png")

    print_tycho = build_archive_print(
        tycho_card_mark,
        "TYCHO CONSORTIUM \u00b7 MERIDIAN STATION \u00b7 UNCREWED",
        "MKR-03 // TYC",
        TYCHO_TEAL,
    )
    print_tycho.save(BRANDS_DIR / "print_tycho.png")

    print_vis = build_archive_print(
        vis_card_mark,
        "VANGUARD INTERPLANETARY STRUCTURES \u00b7 LANDERS \u00b7 MODULES",
        "MKR-04 // VIS",
        CORAL,
    )
    print_vis.save(BRANDS_DIR / "print_vis.png")

    # 5. Maker decal atlas (2048x2048 RGBA with 6 weathered cells)
    print("[generate_brands] Building 2048x2048 T_R1_MakerDecals_Albedo.png...")
    decal_atlas = build_maker_decals_atlas()
    decal_atlas.save(BRANDS_DIR / "T_R1_MakerDecals_Albedo.png")

    print("[generate_brands] Done! Generated all brand assets in", BRANDS_DIR)



if __name__ == "__main__":
    main()

