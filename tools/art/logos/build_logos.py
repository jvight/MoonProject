#!/usr/bin/env python3
"""
Deterministic 4x-supersampled geometric vector generator for Lunarion maker & agency brand concepts:
  1. ILC (International Lunar Commission) — 1969 onward; earnest, geometric, international, 70s/80s space-programme print style.
  2. Kessler-Arai Field Systems — robot maker of 07 (SSR-5, 1983) and SSR-3 elders; industrial, friendly, reliable field equipment; cream/orange family + 07 CRT amber boot screen.
  3. Tycho Consortium — automated Meridian Station operator; clean white + muted teal, hexagonal motifs, colder, corporate.

Owner rule strictly enforced: ZERO clusters of identical dots/holes (trypophobia) in any motif.
All outputs are 1024x1024 PNGs (transparent background + on-black versions) under Logs/logos/<brand>/.
"""

import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[2]
FONT_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Fonts"
OUT_ROOT = REPO_ROOT / "Logs" / "logos"

# Supersampling factor: draw at 3072x3072 (3x) and downsample with Lanczos to 1024x1024 for razor-sharp edges
OUT_SIZE = 1024
SS = 3
W = OUT_SIZE * SS
H = OUT_SIZE * SS

# Canonical Lunarion & Brand Palettes (from docs/VISION.md & docs/design/story-v4.md)
CREAM = (244, 230, 200, 255)        # #F4E6C8
CORAL = (255, 138, 91, 255)         # #FF8A5B
AMBER = (255, 181, 71, 255)         # #FFB547
RUST = (224, 109, 83, 255)          # #E06D53
EARTH_BLUE = (63, 167, 214, 255)    # #3FA7D6
SLATE_METAL = (94, 107, 125, 255)   # #5E6B7D
INDIGO_DEEP = (16, 20, 42, 255)     # #10142A
INDIGO_MID = (31, 36, 71, 255)      # #1F2447
WARM_DARK = (26, 20, 16, 255)       # #1A1410
BLACK_BG = (10, 12, 18, 255)        # #0A0C12

# Tycho Consortium Palette (clean white + muted teal, colder, corporate)
TYCHO_WHITE = (240, 246, 247, 255)  # #F0F6F7
TYCHO_TEAL = (72, 158, 162, 255)    # #489EA2
TYCHO_TEAL_DARK = (42, 104, 110, 255)  # #2A686E
TYCHO_SLATE = (110, 128, 138, 255)  # #6E808A
TYCHO_PANEL = (16, 24, 28, 255)     # #10181C


def load_font(name: str, size_1024: float) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    return ImageFont.truetype(str(path), int(round(size_1024 * SS)))


def s(val: float) -> int:
    return int(round(val * SS))


def sf(val: float) -> float:
    return float(val * SS)


def draw_tracked_text(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple,
    tracking: float = 0.0,
    anchor_y: str = "center",
):
    """Draws horizontally centered text with explicit inter-character tracking (in 1024-space px)."""
    track_px = sf(tracking)
    advances = [font.getlength(ch) for ch in text]
    total_w = sum(advances) + track_px * max(0, len(text) - 1)
    bbox = font.getbbox(text)
    text_h = bbox[3] - bbox[1]
    x = sf(cx) - total_w / 2.0
    if anchor_y == "center":
        y = sf(cy) - bbox[1] - text_h / 2.0
    elif anchor_y == "top":
        y = sf(cy) - bbox[1]
    else:
        y = sf(cy) - bbox[3]

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
    steps = max(48, int(abs(deg_end - deg_start) * 1.2))
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
        # Tangent in unrotated ellipse
        tx = -rx_s * math.sin(rad)
        ty = ry_s * math.cos(rad)
        # Normal outward
        nx, ny = ty, -tx
        nlen = math.hypot(nx, ny) + 1e-9
        nx /= nlen
        ny /= nlen

        # Rotate point and normal
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
    """Draws a crisp 1970s astrometric 4-pointed star."""
    pts = []
    for i in range(8):
        ang = math.radians(i * 45.0)
        r = sf(r_long if i % 2 == 0 else r_short)
        pts.append((sf(cx) + r * math.cos(ang), sf(cy) + r * math.sin(ang)))
    draw.polygon(pts, fill=fill)


def reg_polygon(cx: float, cy: float, r: float, n: int, rot_deg: float = 0.0) -> list:
    pts = []
    for i in range(n):
        ang = math.radians(rot_deg + i * 360.0 / n)
        pts.append((sf(cx) + sf(r) * math.cos(ang), sf(cy) + sf(r) * math.sin(ang)))
    return pts


def downsample(img_ss: Image.Image) -> Image.Image:
    return img_ss.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)


def make_on_black(transparent_img: Image.Image, brand_label: str, sub_label: str) -> Image.Image:
    """Composites a 1024x1024 transparent logo onto a rich matte black #0A0C12 presentation frame."""
    bg = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), BLACK_BG)
    d = ImageDraw.Draw(bg)
    # Subtle period frame border
    d.rectangle((24, 24, OUT_SIZE - 25, OUT_SIZE - 25), outline=(45, 52, 68, 255), width=2)
    # Corner registration ticks
    for x0, y0, dx, dy in [(24, 24, 1, 1), (OUT_SIZE - 25, 24, -1, 1), (24, 999, 1, -1), (OUT_SIZE - 25, 999, -1, -1)]:
        d.line([(x0, y0), (x0 + 18 * dx, y0)], fill=(110, 120, 140, 255), width=2)
        d.line([(x0, y0), (x0, y0 + 18 * dy)], fill=(110, 120, 140, 255), width=2)
    bg.alpha_composite(transparent_img)
    return bg.convert("RGB")


def make_brand_sheet_on_black(imgs_abc: list, titles: list, brand_header: str, accent_rgb: tuple) -> Image.Image:
    """Creates a single 1024x1024 'on_black.png' comparison sheet with all 3 directions + monochrome stamps on black."""
    sheet = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), BLACK_BG)
    d = ImageDraw.Draw(sheet)
    f_hdr = ImageFont.truetype(str(FONT_DIR / "ChakraPetch-Bold.ttf"), 24)
    f_sub = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Medium.ttf"), 14)
    f_lbl = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Bold.ttf"), 15)

    d.rectangle((20, 20, 1004, 1004), outline=(48, 56, 72, 255), width=2)
    d.text((44, 38), brand_header, font=f_hdr, fill=CREAM)
    d.text((44, 68), "1970s–80s MARK + WORDMARK CONCEPTS · COLOUR & MONOCHROME CRT STAMP ON BLACK", font=f_sub, fill=accent_rgb)
    d.line([(44, 96), (980, 96)], fill=(55, 64, 82, 255), width=2)

    # Top row: 3 full-colour lockups at 304x304 each
    for idx, (im, title) in enumerate(zip(imgs_abc, titles)):
        x0 = 40 + idx * 316
        y0 = 116
        d.rectangle((x0, y0, x0 + 308, y0 + 460), fill=(15, 18, 26, 255), outline=(45, 52, 68, 255), width=1)
        thumb = im.resize((296, 296), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x0 + 6, y0 + 18))
        d.line([(x0 + 16, y0 + 330), (x0 + 292, y0 + 330)], fill=(45, 52, 68, 255), width=1)
        d.text((x0 + 18, y0 + 344), title, font=f_lbl, fill=accent_rgb)

        # Extract alpha channel to render a 1-colour CRT/stencil monochrome version below
        mono_thumb = im.resize((160, 160), Image.Resampling.LANCZOS)
        arr = np.array(mono_thumb, dtype=np.float32)
        alpha = arr[:, :, 3]
        # Only take pixels with luminance > 45 so dark badge fills become negative space in monochrome stencil
        lum = 0.299 * arr[:, :, 0] + 0.587 * arr[:, :, 1] + 0.114 * arr[:, :, 2]
        mask = np.clip((lum - 55.0) / 80.0, 0.0, 1.0) * (alpha / 255.0)
        mono_rgba = np.zeros_like(arr, dtype=np.uint8)
        mono_rgba[:, :, 0] = accent_rgb[0]
        mono_rgba[:, :, 1] = accent_rgb[1]
        mono_rgba[:, :, 2] = accent_rgb[2]
        mono_rgba[:, :, 3] = (mask * 255.0).astype(np.uint8)

        d.rectangle((x0, y0 + 382, x0 + 308, y0 + 850), fill=(12, 14, 20, 255), outline=(40, 46, 60, 255), width=1)
        d.text((x0 + 18, y0 + 394), "1-COLOUR STENCIL / CRT STAMP:", font=f_sub, fill=(150, 160, 175, 255))
        mono_im = Image.fromarray(mono_rgba, mode="RGBA").resize((250, 250), Image.Resampling.LANCZOS)
        sheet.alpha_composite(mono_im, (x0 + 29, y0 + 420))

    return sheet.convert("RGB")


# ==============================================================================
# BRAND 1: ILC (International Lunar Commission, 1969 onward)
# ==============================================================================

def build_ilc_a() -> Image.Image:
    """
    ILC Direction A: 'The 1975 Orbital Worm & Crescent Trajectory'
    1975 Danne & Blackburn NASA 'Worm'-inspired continuous tubular logotype + geometric Earth-to-Moon
    transfer ellipse and L4/L5 Lagrange star.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # --- 1. Geometric Mark (centered around y=340) ---
    # Deep indigo backing disc with clean double-ring frame so transparent PNG pops on any surface
    cx, cy = 512, 330
    d.ellipse((s(cx - 236), s(cy - 236), s(cx + 236), s(cy + 236)), fill=INDIGO_DEEP)
    draw_thick_arc(d, cx, cy, 232, 232, 0, 360, 7.0, CREAM)
    draw_thick_arc(d, cx, cy, 214, 214, 0, 360, 2.5, SLATE_METAL)

    # Lower-left Earth horizon hemisphere in 70s silkscreen ocean blue (#3FA7D6) with 3 wide latitude scan bars
    earth_cx, earth_cy, earth_r = 415, 415, 118
    earth_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ed = ImageDraw.Draw(earth_layer)
    ed.ellipse(
        (s(earth_cx - earth_r), s(earth_cy - earth_r), s(earth_cx + earth_r), s(earth_cy + earth_r)),
        fill=EARTH_BLUE,
    )
    # Cut 3 horizontal 1970s scan-stripes across lower Earth (thick solid bars, zero dots)
    for sy_off, sh in [(28, 12), (54, 14), (82, 16)]:
        ed.rectangle(
            (s(earth_cx - earth_r - 10), s(earth_cy + sy_off), s(earth_cx + earth_r + 10), s(earth_cy + sy_off + sh)),
            fill=INDIGO_DEEP,
        )
    # Mask Earth inside inner circle
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.ellipse((s(cx - 208), s(cy - 208), s(cx + 208), s(cy + 208)), fill=255)
    img.paste(earth_layer, (0, 0), mask)
    d = ImageDraw.Draw(img)

    # Upper-right Cream Lunar Disc & Crescent Cutout (strictly clipped to Moon disc)
    moon_cx, moon_cy, moon_r = 566, 276, 96
    d.ellipse((s(moon_cx - moon_r - 10), s(moon_cy - moon_r - 10), s(moon_cx + moon_r + 10), s(moon_cy + moon_r + 10)), fill=INDIGO_DEEP)
    moon_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    mld = ImageDraw.Draw(moon_layer)
    mld.ellipse((s(moon_cx - moon_r), s(moon_cy - moon_r), s(moon_cx + moon_r), s(moon_cy + moon_r)), fill=CREAM)
    mld.ellipse((s(moon_cx - moon_r - 28), s(moon_cy - moon_r - 8), s(moon_cx + moon_r - 54), s(moon_cy + moon_r + 8)), fill=INDIGO_MID)
    moon_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(moon_mask).ellipse((s(moon_cx - moon_r), s(moon_cy - moon_r), s(moon_cx + moon_r), s(moon_cy + moon_r)), fill=255)
    img.paste(moon_layer, (0, 0), moon_mask)
    d = ImageDraw.Draw(img)
    draw_thick_arc(d, moon_cx, moon_cy, moon_r, moon_r, 0, 360, 5.5, CREAM)

    # Inclined Elliptical Transfer Trajectory (#FF8A5B Coral-Orange) sweeping from Earth around Moon
    # Draw dark knock-out gap first so the orbit weaves cleanly across the Moon and border
    draw_thick_arc(d, 500, 342, 205, 86, -155, 165, 28.0, INDIGO_DEEP, rot_deg=-30, round_caps=True)
    draw_thick_arc(d, 500, 342, 205, 86, -155, 165, 16.0, CORAL, rot_deg=-30, round_caps=True)
    # Inner amber telemetry trace parallel to main orbit
    draw_thick_arc(d, 500, 342, 182, 68, -120, 130, 5.5, AMBER, rot_deg=-30, round_caps=True)

    # L4/L5 Halo Orbit Apex Star at upper-left/top of trajectory
    star_x, star_y = 392, 206
    draw_4pt_star(d, star_x, star_y, 34, 9, AMBER)
    draw_thick_arc(d, star_x, star_y, 18, 18, 0, 360, 3.0, CREAM)

    # --- 2. 1975 "Worm"-Style Continuous Tubular "I L C" Wordmark ---
    wy = 690
    stroke_w = 26.0
    # Letter 'I': vertical continuous capsule at x = 330
    ix = 328
    y_top, y_bot = wy - 64, wy + 64
    d.line([(s(ix), s(y_top)), (s(ix), s(y_bot))], fill=CREAM, width=s(stroke_w))
    d.ellipse((s(ix - stroke_w / 2), s(y_top - stroke_w / 2), s(ix + stroke_w / 2), s(y_top + stroke_w / 2)), fill=CREAM)
    d.ellipse((s(ix - stroke_w / 2), s(y_bot - stroke_w / 2), s(ix + stroke_w / 2), s(y_bot + stroke_w / 2)), fill=CREAM)

    # Letter 'L': continuous 1975 radiused bend from x=420..526
    lx0, lx1 = 424, 526
    r_bend = 38
    # Vertical stem
    d.line([(s(lx0), s(y_top)), (s(lx0), s(y_bot - r_bend))], fill=CREAM, width=s(stroke_w))
    d.ellipse((s(lx0 - stroke_w / 2), s(y_top - stroke_w / 2), s(lx0 + stroke_w / 2), s(y_top + stroke_w / 2)), fill=CREAM)
    # 90-degree rounded corner arc
    draw_thick_arc(d, lx0 + r_bend, y_bot - r_bend, r_bend, r_bend, 90, 180, stroke_w, CREAM)
    # Horizontal foot
    d.line([(s(lx0 + r_bend), s(y_bot)), (s(lx1), s(y_bot))], fill=CREAM, width=s(stroke_w))
    d.ellipse((s(lx1 - stroke_w / 2), s(y_bot - stroke_w / 2), s(lx1 + stroke_w / 2), s(y_bot + stroke_w / 2)), fill=CREAM)

    # Letter 'C': continuous tubular C arc at x=645
    cx_c, cy_c, r_c = 646, wy, 64
    draw_thick_arc(d, cx_c, cy_c, r_c, r_c, 42, 318, stroke_w, CREAM, round_caps=True)
    # Coral-orange inline horizon bar inside the 'C' counter (70s space-agency signature detail)
    d.line([(s(cx_c - 10), s(cy_c)), (s(cx_c + 72), s(cy_c))], fill=CORAL, width=s(12))
    d.ellipse((s(cx_c - 16), s(cy_c - 6), s(cx_c - 4), s(cy_c + 6)), fill=CORAL)
    d.ellipse((s(cx_c + 66), s(cy_c - 6), s(cx_c + 78), s(cy_c + 6)), fill=CORAL)

    # --- 3. Sub-headers & Rule Bars ---
    d.rectangle((s(152), s(792), s(872), s(798)), fill=CORAL)
    f_title = load_font("BeVietnamPro-SemiBold.ttf", 31)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 21)
    draw_tracked_text(d, 512, 838, "INTERNATIONAL LUNAR COMMISSION", f_title, CREAM, tracking=6.5)
    draw_tracked_text(d, 512, 888, "EST. 1969  ·  EARTH–MOON L4/L5 PROGRAMME", f_sub, AMBER, tracking=3.0)
    d.rectangle((s(280), s(922), s(744), s(926)), fill=EARTH_BLUE)

    return downsample(img)


def build_ilc_b() -> Image.Image:
    """
    ILC Direction B: 'The 1969 Apollo-Era International Mission Seal'
    Circular agency patch uniting Earth's meridian dome, a rising Coral-Orange delta vector,
    the full Moon disc, and L4/L5 astrometric stars.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx, cy = 512, 338
    r_outer = 248

    # Outer Cream ring + Indigo seal field
    d.ellipse((s(cx - r_outer), s(cy - r_outer), s(cx + r_outer), s(cy + r_outer)), fill=INDIGO_DEEP)
    draw_thick_arc(d, cx, cy, r_outer - 4, r_outer - 4, 0, 360, 10.0, CREAM)
    draw_thick_arc(d, cx, cy, r_outer - 22, r_outer - 22, 0, 360, 5.0, EARTH_BLUE)

    # Interior masked scene inside r_inner = 216
    r_inner = 214
    scene = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(scene)

    # International olive/meridian latitude arcs on left and right flanks
    for side in (-1, 1):
        draw_thick_arc(sd, cx + side * 24, cy, 176, 176, -52 if side == 1 else 128, 52 if side == 1 else 232, 5.0, SLATE_METAL)
        draw_thick_arc(sd, cx + side * 10, cy, 192, 192, -62 if side == 1 else 118, 62 if side == 1 else 242, 3.5, EARTH_BLUE)

    # Lower Earth Horizon Dome (#3FA7D6) with latitude/longitude grid curves
    e_cy, e_r = cy + 255, 190
    sd.ellipse((s(cx - e_r), s(e_cy - e_r), s(cx + e_r), s(e_cy + e_r)), fill=EARTH_BLUE)
    # Clean dark meridian curves on Earth dome
    for m_rx in (55, 115):
        draw_thick_arc(sd, cx, e_cy, m_rx, e_r, 180, 360, 4.5, INDIGO_DEEP)
    draw_thick_arc(sd, cx, e_cy + 40, e_r + 10, 110, 190, 350, 4.5, INDIGO_DEEP)
    draw_thick_arc(sd, cx, e_cy, e_r, e_r, 180, 360, 7.0, CREAM)

    # Upper Center Full Moon Disc (#F4E6C8) with strictly clipped geometric crescent shade
    m_cy, m_r = cy - 42, 88
    m_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    mld = ImageDraw.Draw(m_layer)
    mld.ellipse((s(cx - m_r), s(m_cy - m_r), s(cx + m_r), s(m_cy + m_r)), fill=CREAM)
    mld.ellipse((s(cx - m_r + 36), s(m_cy - m_r - 6), s(cx + m_r + 48), s(m_cy + m_r + 6)), fill=INDIGO_MID)
    m_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m_mask).ellipse((s(cx - m_r), s(m_cy - m_r), s(cx + m_r), s(m_cy + m_r)), fill=255)
    scene.paste(m_layer, (0, 0), m_mask)
    sd = ImageDraw.Draw(scene)
    draw_thick_arc(sd, cx, m_cy, m_r, m_r, 0, 360, 5.5, CREAM)

    # Orbital ring looping horizontally around the Moon
    draw_thick_arc(sd, cx, m_cy + 8, 168, 42, -35, 215, 14.0, INDIGO_DEEP, rot_deg=-14)
    draw_thick_arc(sd, cx, m_cy + 8, 168, 42, -35, 215, 7.5, AMBER, rot_deg=-14, round_caps=True)

    # Iconic 1970s Asymmetric Delta Ascent Chevron (#FF8A5B Coral-Orange) piercing upward
    delta_outer = [
        (sf(cx), sf(cy - 188)),          # Apex
        (sf(cx + 76), sf(cy + 104)),     # Right wing tip
        (sf(cx + 18), sf(cy + 44)),      # Inner right notch
        (sf(cx), sf(cy - 68)),           # Inner center spine
        (sf(cx - 18), sf(cy + 44)),      # Inner left notch
        (sf(cx - 76), sf(cy + 104)),     # Left wing tip
    ]
    # Dark knock-out halo around the delta chevron for crisp separation
    delta_halo = [
        (sf(cx), sf(cy - 200)),
        (sf(cx + 88), sf(cy + 114)),
        (sf(cx), sf(cy + 58)),
        (sf(cx - 88), sf(cy + 114)),
    ]
    sd.polygon(delta_halo, fill=INDIGO_DEEP)
    sd.polygon(delta_outer, fill=CORAL)
    # Amber Mach-stripe highlight on right delta facet
    delta_amber = [
        (sf(cx), sf(cy - 188)),
        (sf(cx + 76), sf(cy + 104)),
        (sf(cx + 42), sf(cy + 69)),
        (sf(cx), sf(cy - 115)),
    ]
    sd.polygon(delta_amber, fill=AMBER)

    # Two L4 & L5 astrometric 4-point stars flanking the upper Moon
    draw_4pt_star(sd, cx - 134, cy - 92, 24, 6, AMBER)
    draw_4pt_star(sd, cx + 134, cy - 92, 24, 6, AMBER)

    # Paste masked scene into seal
    seal_mask = Image.new("L", (W, H), 0)
    smd = ImageDraw.Draw(seal_mask)
    smd.ellipse((s(cx - r_inner), s(cy - r_inner), s(cx + r_inner), s(cy + r_inner)), fill=255)
    img.paste(scene, (0, 0), seal_mask)
    d = ImageDraw.Draw(img)

    # Cardinal astrometric ticks on outer seal ring
    for ang_deg in (0, 90, 180, 270):
        rad = math.radians(ang_deg)
        x0 = cx + (r_outer - 22) * math.cos(rad)
        y0 = cy + (r_outer - 22) * math.sin(rad)
        x1 = cx + (r_outer + 8) * math.cos(rad)
        y1 = cy + (r_outer + 8) * math.sin(rad)
        d.line([(sf(x0), sf(y0)), (sf(x1), sf(y1))], fill=CORAL, width=s(7))

    # --- 2. Wordmark Banner & International Typography ---
    # Winged monogram header at y = 668 (using clean tracked letters without round dots)
    f_mono = load_font("ChakraPetch-Bold.ttf", 92)
    draw_tracked_text(d, 512, 666, "I  L  C", f_mono, CREAM, tracking=18.0)
    # Left and right triple wing stripes (1970s mission badge motif)
    for side in (-1, 1):
        for idx, (y_off, col, length) in enumerate([(-20, CORAL, 175), (0, AMBER, 155), (20, EARTH_BLUE, 135)]):
            x_inner = 512 + side * 205
            x_outer = x_inner + side * length
            d.line([(s(x_inner), s(666 + y_off)), (s(x_outer), s(666 + y_off))], fill=col, width=s(8))

    f_title = load_font("BeVietnamPro-SemiBold.ttf", 30)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 20)
    draw_tracked_text(d, 512, 772, "INTERNATIONAL LUNAR COMMISSION", f_title, CREAM, tracking=5.5)
    d.rectangle((s(160), s(816), s(864), s(821)), fill=EARTH_BLUE)
    draw_tracked_text(d, 512, 858, "LUMEN WAYSTATION  //  HYDROLOGICAL SURVEY  //  1969", f_sub, AMBER, tracking=2.2)
    draw_tracked_text(d, 512, 902, "WATER IS GRAVITY  //  L4–L5 ARK TRANSIT CORRIDOR", f_sub, CORAL, tracking=2.0)

    return downsample(img)


def build_ilc_c() -> Image.Image:
    """
    ILC Direction C: '1982 Swiss Grid & Waystation Stencil System'
    Lance Wyman / Otl Aicher architectural grid badge designed for silkscreened station bulkheads,
    shipping crates, and monochrome terminal stamps.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Outer Swiss modular grid frame (x: 112..912, y: 88..616)
    gx0, gy0, gx1, gy1 = 112, 88, 912, 616
    g_mid_x = 492
    g_mid_y = 352

    # Deep indigo backing inside the modular frame
    d.rounded_rectangle((s(gx0), s(gy0), s(gx1), s(gy1)), radius=s(28), fill=INDIGO_DEEP, outline=CREAM, width=s(10))

    # Structural divider bars
    d.line([(s(g_mid_x), s(gy0)), (s(g_mid_x), s(gy1))], fill=CREAM, width=s(10))
    d.line([(s(g_mid_x), s(g_mid_y)), (s(gx1), s(g_mid_y))], fill=CREAM, width=s(10))

    # --- LEFT PANEL (x: 112..492, y: 88..616): Earth-Moon-L4/L5 Geometric Transit Diagram ---
    # Lower-left Earth ring (#3FA7D6)
    ex, ey, er = 236, 476, 78
    draw_thick_arc(d, ex, ey, er, er, 0, 360, 14.0, EARTH_BLUE)
    draw_thick_arc(d, ex, ey, er - 28, er - 28, 0, 360, 8.0, EARTH_BLUE)
    # Crosshair datum lines
    d.line([(s(ex - er - 18), s(ey)), (s(ex + er + 18), s(ey))], fill=CREAM, width=s(4))
    d.line([(s(ex), s(ey - er - 18)), (s(ex), s(ey + er + 18))], fill=CREAM, width=s(4))

    # 45-degree Transit Corridor Beam (#FF8A5B) from Earth to Moon & L4/L5 Apex
    mx, my, mr = 368, 244, 66
    d.line([(s(ex + 42), s(ey - 42)), (s(mx - 32), s(my + 32))], fill=CORAL, width=s(16))
    # Parallel amber telemetry line
    d.line([(s(ex + 22), s(ey - 56)), (s(mx - 52), s(my + 18))], fill=AMBER, width=s(6))

    # Upper-right Lunar Disc (#F4E6C8) with crescent cut
    d.ellipse((s(mx - mr), s(my - mr), s(mx + mr), s(my + mr)), fill=CREAM)
    d.ellipse((s(mx - mr - 18), s(my - mr + 12), s(mx + mr - 42), s(my + mr - 12)), fill=INDIGO_DEEP)
    draw_thick_arc(d, mx, my, mr, mr, 0, 360, 7.0, CREAM)

    # Upper-left L4/L5 Halo Ark Equilateral Triangle Marker (#FFB547)
    tx, ty = 216, 198
    tri_pts = reg_polygon(tx, ty, 34, 3, rot_deg=-90)
    d.polygon(tri_pts, fill=AMBER)
    d.line([(s(tx + 28), s(ty + 8)), (s(mx - mr - 8), s(my - 12))], fill=AMBER, width=s(5))
    d.line([(s(ex - 12), s(ey - er - 12)), (s(tx - 6), s(ty + 28))], fill=AMBER, width=s(5))

    # Panel corner spec code
    f_tag = load_font("IBMPlexMono-Bold.ttf", 18)
    d.text((s(gx0 + 26), s(gy1 - 42)), "FIG. 01 // L4–L5 CORRIDOR", font=f_tag, fill=AMBER)

    # --- TOP-RIGHT PANEL (x: 492..912, y: 88..352): Heavy Stencil 'ILC' Block Monogram ---
    # Coral-orange corner index block (cleanly inset inside the rounded top-right corner)
    d.rounded_rectangle((s(gx1 - 102), s(gy0 + 20), s(gx1 - 22), s(gy0 + 76)), radius=s(8), fill=CORAL)
    f_yr = load_font("IBMPlexMono-Bold.ttf", 22)
    draw_tracked_text(d, gx1 - 62, gy0 + 48, "69", f_yr, INDIGO_DEEP, tracking=1.0)

    f_ilc = load_font("ChakraPetch-Bold.ttf", 156)
    draw_tracked_text(d, 682, 224, "ILC", f_ilc, CREAM, tracking=12.0)
    # Stencil bridge cuts across the 'ILC' letters for authentic 1980s crate/bulkhead stencil look
    d.rectangle((s(520), s(218), s(855), s(226)), fill=INDIGO_DEEP)

    # --- BOTTOM-RIGHT PANEL (x: 492..912, y: 352..616): Hydrological Survey Strata Bars ---
    # Three bold horizontal geological/hydrological strata bands (Regolith -> Basalt Cap -> Deep Water-Ice)
    px0, px1 = g_mid_x + 32, gx1 - 32
    # Band 1: Surface Regolith (#F4E6C8)
    d.rectangle((s(px0), s(392), s(px1), s(422)), fill=CREAM)
    # Band 2: Basalt Cap (#FF8A5B) with central drill-shaft notch
    d.rectangle((s(px0), s(442), s(684), s(472)), fill=CORAL)
    d.rectangle((s(720), s(442), s(px1), s(472)), fill=CORAL)
    # Vertical drill core line piercing down into the water-ice band
    d.rectangle((s(696), s(382), s(708), s(532)), fill=AMBER)
    # Band 3: Sub-surface Water-Ice Reservoir (#3FA7D6) with wave chevron
    d.rectangle((s(px0), s(492), s(px1), s(532)), fill=EARTH_BLUE)
    d.text((s(px0), s(556)), "HYDROLOGICAL SURVEY · LUMEN-04", font=f_tag, fill=CREAM)

    # --- 2. Swiss International Wordmark Lockup Below Frame ---
    d.rectangle((s(gx0), s(656), s(580), s(668)), fill=CORAL)
    d.rectangle((s(596), s(656), s(gx1), s(668)), fill=EARTH_BLUE)

    f_title = load_font("ChakraPetch-Bold.ttf", 41)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 22)
    f_sub2 = load_font("IBMPlexMono-Medium.ttf", 19)

    draw_tracked_text(d, 512, 726, "INTERNATIONAL LUNAR COMMISSION", f_title, CREAM, tracking=3.8)
    draw_tracked_text(d, 512, 792, "ORBITAL ARK & WAYSTATION AUTHORITY  ·  EST. 1969", f_sub, AMBER, tracking=2.5)
    d.rectangle((s(gx0), s(838), s(gx1), s(842)), fill=SLATE_METAL)
    draw_tracked_text(d, 512, 878, "STANDARD INSIGNIA SPEC 74-B  ·  GENEVA / TSUKUBA / HOUSTON / L5", f_sub2, CREAM, tracking=1.8)

    return downsample(img)


def generate_brand_ilc():
    out_dir = OUT_ROOT / "ilc"
    out_dir.mkdir(parents=True, exist_ok=True)
    im_a = build_ilc_a()
    im_b = build_ilc_b()
    im_c = build_ilc_c()

    im_a.save(out_dir / "a.png")
    im_b.save(out_dir / "b.png")
    im_c.save(out_dir / "c.png")

    make_on_black(im_a, "ILC // DIRECTION A", "1975 ORBITAL WORM & CRESCENT").save(out_dir / "a_black.png")
    make_on_black(im_b, "ILC // DIRECTION B", "1969 APOLLO-ERA MISSION SEAL").save(out_dir / "b_black.png")
    make_on_black(im_c, "ILC // DIRECTION C", "1982 SWISS WAYSTATION STENCIL").save(out_dir / "c_black.png")

    sheet = make_brand_sheet_on_black(
        [im_a, im_b, im_c],
        ["A · 1975 ORBITAL WORM", "B · 1969 AGENCY SEAL", "C · 1982 SWISS STENCIL"],
        "1 · ILC (INTERNATIONAL LUNAR COMMISSION · 1969–1988)",
        CORAL,
    )
    sheet.save(out_dir / "on_black.png")
    print("[build_logos] Generated ILC logos in", out_dir)


if __name__ == "__main__":
    generate_brand_ilc()
