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


# ==============================================================================
# BRAND 2: KESSLER–ARAI FIELD SYSTEMS (Robot Maker of 07 & Elders, 1983)
# ==============================================================================

def build_kessler_a() -> Image.Image:
    """
    Kessler–Arai Direction A: 'The KA Single-Lens & Rocker-Bogie Badge'
    Chunky rounded-bezel industrial monogram uniting a stencil 'K–A' with 07's hooded single-lens
    eye and rocker-bogie suspension link—friendly, reliable, and built for 07's CRT boot screen.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Rounded squircle equipment bezel (x: 164..860, y: 78..582), echoing 07's visor hood
    bx0, by0, bx1, by1 = 164, 78, 860, 582
    d.rounded_rectangle((s(bx0), s(by0), s(bx1), s(by1)), radius=s(84), fill=WARM_DARK, outline=CREAM, width=s(12))
    d.rounded_rectangle((s(bx0 + 20), s(by0 + 20), s(bx1 - 20), s(by1 - 20)), radius=s(66), outline=CORAL, width=s(6))

    # Top visor hood brow bar in Coral-Orange (#FF8A5B)
    d.rounded_rectangle((s(bx0 + 54), s(by0 + 44), s(bx1 - 54), s(by0 + 72)), radius=s(10), fill=CORAL)

    # --- Left 'K' Monogram (x: 248..456, y: 184..444) ---
    # Heavy vertical stem in Cream
    d.rounded_rectangle((s(252), s(184), s(304), s(444)), radius=s(8), fill=CREAM)
    # Upper diagonal arm of K in Cream
    k_up = [(sf(314), sf(304)), (sf(418), sf(184)), (sf(466), sf(184)), (sf(356), sf(314))]
    d.polygon(k_up, fill=CREAM)
    # Lower diagonal strut of K in Coral-Orange (#FF8A5B) acting as articulated hydraulic link
    k_dn = [(sf(344), sf(296)), (sf(388), sf(296)), (sf(472), sf(444)), (sf(418), sf(444))]
    d.polygon(k_dn, fill=CORAL)

    # Central hyphen / structural coupling bridge linking K and A
    d.rounded_rectangle((s(452), s(300), s(532), s(336)), radius=s(8), fill=AMBER)

    # --- Right 'A' Survey Frame & 07 Hooded Single Lens (x: 528..772, y: 184..444) ---
    ax_c = 652
    # Left & right angled legs of 'A' in Cream
    a_left = [(sf(ax_c - 34), sf(184)), (sf(ax_c + 12), sf(184)), (sf(584), sf(444)), (sf(532), sf(444))]
    a_right = [(sf(ax_c - 12), sf(184)), (sf(ax_c + 34), sf(184)), (sf(772), sf(444)), (sf(720), sf(444))]
    d.polygon(a_left, fill=CREAM)
    d.polygon(a_right, fill=CREAM)
    # Crossbar of 'A'
    d.rectangle((s(572), s(352), s(732), s(388)), fill=CREAM)

    # 07's Iconic Single Round Survey Lens mounted at the 'A' upper hub (cx=652, cy=268)
    lx, ly, lr = 652, 268, 62
    d.ellipse((s(lx - lr - 10), s(ly - lr - 10), s(lx + lr + 10), s(ly + lr + 10)), fill=WARM_DARK)
    draw_thick_arc(d, lx, ly, lr, lr, 0, 360, 14.0, AMBER)
    # Inner warm amber lens core
    d.ellipse((s(lx - 32), s(ly - 32), s(lx + 32), s(ly + 32)), fill=CORAL)
    # Half-lidded shutter brow across top of lens (where 07's gentle melancholy lives, VISION.md:76)
    d.chord((s(lx - lr - 2), s(ly - lr - 2), s(lx + lr + 2), s(ly + lr + 2)), start=195, end=345, fill=CREAM)
    # Specular crescent arc on lower lens
    draw_thick_arc(d, lx, ly, 42, 42, 25, 155, 6.0, CREAM, round_caps=True)

    # --- Lower Rocker-Bogie Suspension Link Bar (y: 472..526) ---
    # Connects left wheel bogie pivot to right wheel bogie pivot
    rb_pts = [
        (sf(244), sf(516)),
        (sf(296), sf(474)),
        (sf(728), sf(474)),
        (sf(780), sf(516)),
        (sf(748), sf(532)),
        (sf(712), sf(502)),
        (sf(312), sf(502)),
        (sf(276), sf(532)),
    ]
    d.polygon(rb_pts, fill=CORAL)
    # Left and right trapezoidal wheel-pad feet (no round holes)
    d.polygon([(sf(224), sf(536)), (sf(296), sf(536)), (sf(284), sf(514)), (sf(236), sf(514))], fill=AMBER)
    d.polygon([(sf(728), sf(536)), (sf(800), sf(536)), (sf(788), sf(514)), (sf(740), sf(514))], fill=AMBER)

    # --- 2. Industrial Wordmark Lockup ---
    f_main = load_font("ChakraPetch-Bold.ttf", 72)
    draw_tracked_text(d, 512, 656, "KESSLER–ARAI", f_main, CREAM, tracking=6.5)

    # Coral-orange ribbon bar with dark knock-out 'FIELD SYSTEMS'
    d.rounded_rectangle((s(192), s(722), s(832), s(788)), radius=s(12), fill=CORAL)
    f_ribbon = load_font("ChakraPetch-Bold.ttf", 38)
    draw_tracked_text(d, 512, 755, "FIELD SYSTEMS", f_ribbon, WARM_DARK, tracking=10.0)

    # Chassis spec subline
    f_sub = load_font("IBMPlexMono-Bold.ttf", 21)
    draw_tracked_text(d, 512, 838, "SSR-3 / SSR-5 SERIES  ·  STUTTGART & TSUKUBA  ·  EST. 1974", f_sub, AMBER, tracking=2.0)
    draw_tracked_text(d, 512, 884, "MODULAR PLANETARY & WAYSTATION ROBOTICS", f_sub, CREAM, tracking=3.0)

    return downsample(img)


def build_kessler_b() -> Image.Image:
    """
    Kessler–Arai Direction B: 'The Rising Solar Disc & Crater-Ridge Lozenge'
    1980s Komatsu/Braun-inspired heavy machinery stamp pairing a warm solar disc (Arai)
    with a stepped crater-ridge chevron (Kessler) inside chamfered chassis brackets.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Chamfered octagonal industrial housing (x: 148..876, y: 84..576)
    cx, cy = 512, 330
    chamf = 72
    bx0, by0, bx1, by1 = 156, 84, 868, 576
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
    d.polygon(oct_pts, fill=WARM_DARK)

    # Heavy left & right Coral-Orange C-clamp bumper brackets
    bw = 26
    for side in (-1, 1):
        x_edge = bx0 if side == -1 else bx1
        x_in = x_edge - side * (chamf + 24)
        clamp = [
            (sf(x_in), sf(by0)),
            (sf(x_edge - side * chamf), sf(by0)),
            (sf(x_edge), sf(by0 + chamf)),
            (sf(x_edge), sf(by1 - chamf)),
            (sf(x_edge - side * chamf), sf(by1)),
            (sf(x_in), sf(by1)),
            (sf(x_in), sf(by1 - bw)),
            (sf(x_edge - side * (chamf - 10)), sf(by1 - bw)),
            (sf(x_edge - side * bw), sf(by1 - chamf + 10)),
            (sf(x_edge - side * bw), sf(by0 + chamf - 10)),
            (sf(x_edge - side * (chamf - 10)), sf(by0 + bw)),
            (sf(x_in), sf(by0 + bw)),
        ]
        d.polygon(clamp, fill=CORAL)

    # Top and bottom Cream tie-bars between clamps
    d.rectangle((s(bx0 + chamf + 38), s(by0 + 6), s(bx1 - chamf - 38), s(by0 + 20)), fill=CREAM)
    d.rectangle((s(bx0 + chamf + 38), s(by1 - 20), s(bx1 - chamf - 38), s(by1 - 6)), fill=CREAM)

    # --- Rising Amber Solar Disc (Arai) at (512, 292), r = 136 ---
    sun_y, sun_r = 294, 138
    d.ellipse((s(cx - sun_r), s(sun_y - sun_r), s(cx + sun_r), s(sun_y + sun_r)), fill=AMBER)
    # Two clean horizontal 1980s scan-cuts across the sun disc
    d.rectangle((s(cx - sun_r - 10), s(sun_y + 18), s(cx + sun_r + 10), s(sun_y + 32)), fill=WARM_DARK)
    d.rectangle((s(cx - sun_r - 10), s(sun_y + 52), s(cx + sun_r + 10), s(sun_y + 70)), fill=WARM_DARK)

    # --- Twin-Peak Crater Ridge & Survey Path Chevron (Kessler) ---
    # Dark knock-out border above the ridge
    ridge_ko = [
        (sf(236), sf(518)),
        (sf(396), sf(296)),
        (sf(472), sf(374)),
        (sf(512), sf(334)),
        (sf(552), sf(374)),
        (sf(628), sf(296)),
        (sf(788), sf(518)),
    ]
    d.polygon(ridge_ko, fill=WARM_DARK)

    # Left crater peak in Cream (#F4E6C8)
    left_peak = [
        (sf(252), sf(518)),
        (sf(398), sf(318)),
        (sf(488), sf(412)),
        (sf(438), sf(518)),
    ]
    d.polygon(left_peak, fill=CREAM)

    # Right crater peak in Coral-Orange (#FF8A5B)
    right_peak = [
        (sf(772), sf(518)),
        (sf(626), sf(318)),
        (sf(536), sf(412)),
        (sf(586), sf(518)),
    ]
    d.polygon(right_peak, fill=CORAL)

    # Central rover survey beam / road rising between the peaks toward the sun
    road_pts = [
        (sf(466), sf(518)),
        (sf(500), sf(404)),
        (sf(524), sf(404)),
        (sf(558), sf(518)),
    ]
    d.polygon(road_pts, fill=AMBER)

    # --- 2. Heavy Mechanical Wordmark Lockup ---
    f_main = load_font("ChakraPetch-Bold.ttf", 70)
    draw_tracked_text(d, 512, 650, "KESSLER – ARAI", f_main, CREAM, tracking=6.0)

    # Hazard-chevron flanked 'FIELD SYSTEMS' bar
    f_mid = load_font("ChakraPetch-Bold.ttf", 36)
    draw_tracked_text(d, 512, 742, "FIELD SYSTEMS", f_mid, CORAL, tracking=9.0)
    # 3 bold angled industrial stripes on left and right (no dots)
    for side in (-1, 1):
        for i, col in enumerate([CORAL, AMBER, CREAM]):
            bx = 512 + side * (252 + i * 34)
            stripe = [
                (sf(bx), sf(724)),
                (sf(bx + side * 20), sf(724)),
                (sf(bx + side * 8), sf(760)),
                (sf(bx - side * 12), sf(760)),
            ]
            d.polygon(stripe, fill=col)

    d.rectangle((s(156), s(796), s(868), s(802)), fill=AMBER)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 20)
    draw_tracked_text(d, 512, 842, "HEAVY LUNAR & PLANETARY FIELD EQUIPMENT  ·  1983", f_sub, CREAM, tracking=2.2)
    draw_tracked_text(d, 512, 886, "SSR-3 UTILITY CART  //  SSR-5 MODULAR SERVICE ROVER", f_sub, AMBER, tracking=2.0)

    return downsample(img)


def build_kessler_c() -> Image.Image:
    """
    Kessler–Arai Direction C: 'The 1983 SSR-5 Stamped Enamel Chassis Nameplate'
    Diegetic equipment plate ready to weld onto 07's rear flank (07-identity.md §3),
    featuring a geometric 2D SSR-5 rover silhouette and split-block 'K/A' stencil.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Stamped metal chassis plate frame (x: 104..920, y: 84..592)
    px0, py0, px1, py1 = 104, 84, 920, 592
    d.rounded_rectangle((s(px0), s(py0), s(px1), s(py1)), radius=s(24), fill=WARM_DARK, outline=CREAM, width=s(10))
    d.rounded_rectangle((s(px0 + 18), s(py0 + 18), s(px1 - 18), s(py1 - 18)), radius=s(14), outline=CORAL, width=s(5))

    # Corner L-shaped weld brackets (zero circular screw holes!)
    for cx0, cy0, dx, dy in [(px0 + 32, py0 + 32, 1, 1), (px1 - 32, py0 + 32, -1, 1), (px0 + 32, py1 - 32, 1, -1), (px1 - 32, py1 - 32, -1, -1)]:
        d.line([(s(cx0), s(cy0)), (s(cx0 + 32 * dx), s(cy0))], fill=AMBER, width=s(7))
        d.line([(s(cx0), s(cy0)), (s(cx0), s(cy0 + 32 * dy))], fill=AMBER, width=s(7))

    # Vertical divider at x = 524
    div_x = 524
    d.line([(s(div_x), s(py0 + 18)), (s(div_x), s(py1 - 18))], fill=CORAL, width=s(8))

    # --- LEFT HALF (x: 122..524): Iconic Geometric 2D SSR-5 Rover Profile Emblem ---
    # Whip antenna at rear-left (x=212) with diamond sonar loop and amber tip beacon
    d.line([(s(214), s(310)), (s(194), s(166))], fill=CREAM, width=s(7))
    draw_4pt_star(d, 194, 156, 20, 6, AMBER)

    # Folded solar wing along upper back
    d.polygon([(sf(204), sf(304)), (sf(334), sf(288)), (sf(340), sf(308)), (sf(208), sf(322))], fill=AMBER)

    # Rounded beetle/boxy carapace hull in Cream (#F4E6C8) with Coral-Orange (#FF8A5B) stripe
    hull_pts = [
        (sf(192), sf(416)),
        (sf(192), sf(334)),
        (sf(232), sf(312)),
        (sf(368), sf(312)),
        (sf(406), sf(348)),
        (sf(406), sf(416)),
    ]
    d.polygon(hull_pts, fill=CREAM)
    d.rectangle((s(192), s(368), s(406), s(392)), fill=CORAL)

    # Slender neck + hooded CRT sensor head looking forward-right
    d.polygon([(sf(348), sf(314)), (sf(374), sf(246)), (sf(398), sf(246)), (sf(378), sf(314))], fill=CREAM)
    # Visor hood & CRT head
    head_pts = [
        (sf(352), sf(248)),
        (sf(364), sf(188)),
        (sf(448), sf(188)),
        (sf(468), sf(214)),
        (sf(452), sf(256)),
    ]
    d.polygon(head_pts, fill=CREAM)
    # Amber lens glow at front of visor
    d.polygon([(sf(444), sf(198)), (sf(466), sf(214)), (sf(450), sf(248)), (sf(434), sf(248))], fill=AMBER)

    # Front scoop blade
    d.polygon([(sf(406), sf(396)), (sf(464), sf(414)), (sf(478), sf(448)), (sf(406), sf(448))], fill=CORAL)

    # Rocker-bogie struts & two chunky octagonal off-road wheels (octagonal outer + solid amber hub, no hole clusters)
    bogie_poly = [
        (sf(240), sf(466)),
        (sf(315), sf(402)),
        (sf(390), sf(466)),
        (sf(376), sf(478)),
        (sf(315), sf(424)),
        (sf(254), sf(478)),
    ]
    d.polygon(bogie_poly, fill=CORAL)
    for wx in (248, 382):
        wy = 466
        w_oct = reg_polygon(wx, wy, 44, 8, rot_deg=22.5)
        d.polygon(w_oct, fill=WARM_DARK)
        d.polygon(reg_polygon(wx, wy, 42, 8, rot_deg=22.5), outline=CREAM, width=s(8))
        d.polygon(reg_polygon(wx, wy, 18, 6, rot_deg=0), fill=AMBER)

    f_plate_tag = load_font("IBMPlexMono-Bold.ttf", 19)
    d.text((s(154), s(532)), "MODEL SSR-5  //  UNIT 07", font=f_plate_tag, fill=AMBER)

    # --- RIGHT HALF (x: 524..902): Stencil 'K/A' & Stamped Spec Bars ---
    f_ka = load_font("ChakraPetch-Bold.ttf", 148)
    draw_tracked_text(d, 712, 224, "K/A", f_ka, CREAM, tracking=8.0)
    # Stencil bridge cut across K/A
    d.rectangle((s(556), s(222), s(872), s(230)), fill=WARM_DARK)

    # Three stamped enamel spec bands
    specs = [
        ("KESSLER–ARAI 1983", CORAL, WARM_DARK),
        ("S/N 3-0417 // ILC", AMBER, WARM_DARK),
        ("MODE: CONTINUE", INDIGO_MID, CREAM),
    ]
    f_spec = load_font("IBMPlexMono-Bold.ttf", 22)
    for idx, (txt, bg_col, fg_col) in enumerate(specs):
        by = 344 + idx * 68
        d.rounded_rectangle((s(558), s(by), s(876), s(by + 52)), radius=s(8), fill=bg_col)
        draw_tracked_text(d, 717, by + 26, txt, f_spec, fg_col, tracking=1.5)

    # --- 2. Full-Width Stencil Wordmark Lockup Below Plate ---
    d.rectangle((s(px0), s(634), s(px1), s(642)), fill=CORAL)
    f_title = load_font("ChakraPetch-Bold.ttf", 52)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 22)
    f_sub2 = load_font("IBMPlexMono-Medium.ttf", 19)

    draw_tracked_text(d, 512, 704, "KESSLER–ARAI FIELD SYSTEMS", f_title, CREAM, tracking=4.5)
    draw_tracked_text(d, 512, 774, "ILC · SSR-5 STATION SERVICE ROVER · S/N 3-0417", f_sub, CORAL, tracking=2.5)
    d.rectangle((s(180), s(818), s(844), s(822)), fill=AMBER)
    draw_tracked_text(d, 512, 862, "SCAN  //  CUT  //  BUILD  //  TOW  //  WAKE ON CARRIER", f_sub2, AMBER, tracking=3.0)

    return downsample(img)


def build_kessler_crt_boot(logo_rgba: Image.Image, variant_code: str = "A") -> Image.Image:
    """
    Renders a 1024x1024 mock frame of 07's 1-colour amber CRT boot screen (matching R1CrtFace.shader
    and Logs/captures style: warm black #20140A/#120B05, beacon amber #FFB547, hot core #FFF3CC,
    phosphor halo bloom, horizontal scanlines, and 07's visor hood bezel).
    """
    # 1. Outer 07 Head Visor Hood & Bezel Frame (1024x1024)
    frame = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), (14, 11, 9, 255))
    fd = ImageDraw.Draw(frame)

    # Outer cream/orange visor hood casing around the CRT aperture
    fd.rounded_rectangle((28, 44, 996, 980), radius=76, fill=(42, 33, 26, 255), outline=(92, 72, 54, 255), width=4)
    # Warm coral-orange visor brow trim at top (07's hood signature)
    fd.rounded_rectangle((68, 56, 956, 86), radius=12, fill=(168, 82, 48, 255))

    # CRT glass screen aperture: 900 x 810 inside (62, 108, 962, 918)
    sx0, sy0, sx1, sy1 = 62, 108, 962, 918
    sw, sh = sx1 - sx0, sy1 - sy0

    # 2. Build radial warm-black phosphor screen base (#20140A center -> #120B05 edge, from R1CrtFace.shader:5-6)
    yy, xx = np.mgrid[0:sh, 0:sw].astype(np.float32)
    nx = (xx / sw - 0.5) * 2.0
    ny = (yy / sh - 0.5) * 2.0
    r2 = np.clip(nx * nx * 0.85 + ny * ny * 0.85, 0.0, 1.4)
    vig = np.clip(1.0 - 0.55 * r2, 0.25, 1.0)

    base_rgb = np.zeros((sh, sw, 3), dtype=np.float32)
    # _ScreenBaseColor = (32, 20, 10), _ScreenEdgeColor = (18, 11, 5)
    base_rgb[:, :, 0] = 18.0 + (34.0 - 18.0) * vig
    base_rgb[:, :, 1] = 11.0 + (21.0 - 11.0) * vig
    base_rgb[:, :, 2] = 5.0 + (10.0 - 5.0) * vig

    # 3. Compose 1-colour amber phosphor content layer (intensity 0.0 .. 1.0)
    content = Image.new("L", (sw, sh), 0)
    cd = ImageDraw.Draw(content)

    f_hdr = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Bold.ttf"), 17)
    f_boot_title = ImageFont.truetype(str(FONT_DIR / "ChakraPetch-Bold.ttf"), 33)
    f_boot_sub = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Bold.ttf"), 19)
    f_boot_dim = ImageFont.truetype(str(FONT_DIR / "IBMPlexMono-Medium.ttf"), 17)

    # Top CRT diagnostic header bar
    cd.text((38, 26), "KESSLER–ARAI ROM BIOS v1983.4", font=f_hdr, fill=185)
    cd.text((sw - 310, 26), "BUS: CONTINUE [LOCKED]", font=f_hdr, fill=185)
    cd.line([(38, 56), (sw - 38, 56)], fill=120, width=2)

    # Convert the brand logo's upper mark (or full lockup) into a crisp 1-colour amber phosphor stencil
    # Crop mark region (y: 65..615 of the 1024x1024 logo) so we can pair it with the exact CRT boot typography
    mark_crop = logo_rgba.crop((100, 68, 924, 608))
    mark_resized = mark_crop.resize((440, 288), Image.Resampling.LANCZOS)
    m_arr = np.array(mark_resized, dtype=np.float32)
    m_lum = 0.299 * m_arr[:, :, 0] + 0.587 * m_arr[:, :, 1] + 0.114 * m_arr[:, :, 2]
    m_alpha = m_arr[:, :, 3] / 255.0
    # Bright cream/amber/coral strokes become high phosphor intensity; dark plate backing becomes 0
    phosphor_mark = np.clip((m_lum - 50.0) / 140.0, 0.0, 1.0) * m_alpha
    mark_l = Image.fromarray((phosphor_mark * 255.0).astype(np.uint8), mode="L")
    content.paste(mark_l, ((sw - 440) // 2, 78))

    # Main 1-colour amber boot headline requested in TASK.md:
    # "KESSLER–ARAI FIELD SYSTEMS · SSR-5"
    headline = "KESSLER–ARAI FIELD SYSTEMS · SSR-5"
    hb = f_boot_title.getbbox(headline)
    hw = hb[2] - hb[0]
    cd.text(((sw - hw) // 2, 392), headline, font=f_boot_title, fill=255)

    # Double horizontal amber rule with diagnostic ticks
    cd.line([(64, 444), (sw - 64, 444)], fill=210, width=3)
    cd.line([(64, 452), (sw - 64, 452)], fill=110, width=1)

    # Self-test & identity lines (from docs/design/07-identity.md §3 & story-v4.md §4.1 — solid dashes, zero dot rows)
    boot_lines = [
        ("ILC · STATION SERVICE ROVER · S/N 3-0417 · BUILT 1983", 230, f_boot_sub),
        ("UNIT 07 · SSR-5 · CONTINUE · 13 148 DAYS", 245, f_boot_sub),
        ("OPTIC PROFILER Ø 0.245m ———————————— [OK]   CUTTER TURRET ———————————— [OK]", 175, f_boot_dim),
        ("ADAPTIVE SCHEDULER ————————————————— [WEIGHTS RETAINED · 36Y DRIFT]", 175, f_boot_dim),
        ("TRICKLE POST: WAKE ON CARRIER —————— [ORBITAL SIGNAL · DETECTED]", 240, f_boot_sub),
    ]
    for i, (btxt, val, fnt) in enumerate(boot_lines):
        cd.text((68, 474 + i * 42), btxt, font=fnt, fill=val)

    # Bottom command prompt bar
    cd.rectangle((68, 704, sw - 68, 756), outline=195, width=2)
    cd.text((88, 718), "> UPLINK · OFFLINE   //   BEGIN LUMEN RECOVERY PROTOCOL", font=f_boot_sub, fill=255)
    # Solid phosphor cursor block
    cd.rectangle((738, 719, 754, 741), fill=255)

    # 4. Apply R1CrtFace.shader phosphor bloom + hot core + scanlines
    c_norm = np.array(content, dtype=np.float32) / 255.0
    halo_tight = np.array(content.filter(ImageFilter.GaussianBlur(radius=3.2)), dtype=np.float32) / 255.0
    halo_wide = np.array(content.filter(ImageFilter.GaussianBlur(radius=14.0)), dtype=np.float32) / 255.0

    # _PhosphorColor = (255, 174, 56), _PhosphorGlowColor = (200, 106, 20), _HotCoreColor = (255, 243, 204)
    phos_r = base_rgb[:, :, 0] + halo_wide * 200.0 * 0.55 + halo_tight * 255.0 * 0.65 + c_norm * 255.0
    phos_g = base_rgb[:, :, 1] + halo_wide * 106.0 * 0.55 + halo_tight * 174.0 * 0.65 + c_norm * 184.0 + (c_norm ** 3) * 65.0
    phos_b = base_rgb[:, :, 2] + halo_wide * 20.0 * 0.55 + halo_tight * 56.0 * 0.65 + c_norm * 68.0 + (c_norm ** 4) * 135.0

    # Horizontal CRT scanlines (~270 lines across sh=810 -> 3px pitch) + subtle rolling refresh band
    scan_wave = 1.0 - 0.19 * (0.5 + 0.5 * np.cos(yy * (2.0 * math.pi / 3.0)))
    rolling_band = 1.0 + 0.07 * np.exp(-((yy - sh * 0.38) / 65.0) ** 2)
    mult = scan_wave * rolling_band

    scr_rgb = np.stack([phos_r * mult, phos_g * mult, phos_b * mult], axis=-1)
    scr_rgb = np.clip(scr_rgb, 0.0, 255.0).astype(np.uint8)
    scr_img = Image.fromarray(scr_rgb, mode="RGB")

    # Mask into rounded CRT bezel aperture
    scr_mask = Image.new("L", (sw, sh), 0)
    ImageDraw.Draw(scr_mask).rounded_rectangle((0, 0, sw - 1, sh - 1), radius=54, fill=255)
    frame.paste(scr_img, (sx0, sy0), scr_mask)

    # Inner CRT glass bezel rim highlight
    fd.rounded_rectangle((sx0, sy0, sx1, sy1), radius=54, outline=(120, 78, 36, 255), width=3)
    fd.text((68, 938), f"07 CRT FACE BOOT MOCKUP (VARIANT {variant_code}) · R1CrtFace.shader AMBER PHOSPHOR", font=f_hdr, fill=(185, 145, 95, 255))

    return frame.convert("RGB")


def generate_brand_kessler():
    out_dir = OUT_ROOT / "kessler-arai"
    out_dir.mkdir(parents=True, exist_ok=True)
    im_a = build_kessler_a()
    im_b = build_kessler_b()
    im_c = build_kessler_c()

    im_a.save(out_dir / "a.png")
    im_b.save(out_dir / "b.png")
    im_c.save(out_dir / "c.png")

    make_on_black(im_a, "KESSLER–ARAI // DIRECTION A", "SINGLE-LENS & ROCKER-BOGIE BADGE").save(out_dir / "a_black.png")
    make_on_black(im_b, "KESSLER–ARAI // DIRECTION B", "RISING SOLAR DISC & CRATER LOZENGE").save(out_dir / "b_black.png")
    make_on_black(im_c, "KESSLER–ARAI // DIRECTION C", "1983 SSR-5 CHASSIS NAMEPLATE").save(out_dir / "c_black.png")

    sheet = make_brand_sheet_on_black(
        [im_a, im_b, im_c],
        ["A · LENS & BOGIE BADGE", "B · SOLAR CRATER STAMP", "C · CHASSIS NAMEPLATE"],
        "2 · KESSLER–ARAI FIELD SYSTEMS (SSR-3 / SSR-5 · 1983)",
        AMBER,
    )
    sheet.save(out_dir / "on_black.png")

    # 1-colour amber CRT boot screens on 07's face
    crt_a = build_kessler_crt_boot(im_a, "A")
    crt_b = build_kessler_crt_boot(im_b, "B")
    crt_c = build_kessler_crt_boot(im_c, "C")
    crt_a.save(out_dir / "crt_a.png")
    crt_b.save(out_dir / "crt_b.png")
    crt_c.save(out_dir / "crt_c.png")
    crt_a.save(out_dir / "crt.png")
    print("[build_logos] Generated Kessler-Arai logos + CRT boot screens in", out_dir)


# ==============================================================================
# BRAND 3: TYCHO CONSORTIUM (Automated Meridian Station Operator)
# ==============================================================================

def build_tycho_a() -> Image.Image:
    """
    Tycho Consortium Direction A: 'The Hex-Dock & Isometric TC Prism'
    Precision hexagonal contact-dock emblem forming an interlocking isometric 'TC' prism—cold,
    corporate, and built to double as the physical alignment target on Meridian's hex charging docks.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx, cy = 512, 336
    r_outer = 252
    r_mid = 228
    r_inner = 206

    # Outer Clean White (#F0F6F7) regular hexagon (pointy-top, rot_deg=-90)
    d.polygon(reg_polygon(cx, cy, r_outer, 6, rot_deg=-90), fill=TYCHO_WHITE)
    d.polygon(reg_polygon(cx, cy, r_outer - 12, 6, rot_deg=-90), fill=TYCHO_PANEL)

    # Inner Muted Teal (#489EA2) hexagonal contact-dock ring
    d.polygon(reg_polygon(cx, cy, r_mid, 6, rot_deg=-90), fill=TYCHO_TEAL)
    d.polygon(reg_polygon(cx, cy, r_mid - 10, 6, rot_deg=-90), fill=TYCHO_PANEL)

    # 6 radial alignment dock contact bars at the 6 vertices (solid bars, zero holes)
    for i in range(6):
        ang = math.radians(-90 + i * 60.0)
        x0 = cx + (r_inner - 14) * math.cos(ang)
        y0 = cy + (r_inner - 14) * math.sin(ang)
        x1 = cx + (r_mid + 2) * math.cos(ang)
        y1 = cy + (r_mid + 2) * math.sin(ang)
        d.line([(sf(x0), sf(y0)), (sf(x1), sf(y1))], fill=TYCHO_TEAL, width=s(10))

    # --- Central Isometric Hexagonal Prism forming interlocking 'T' (White) and 'C' (Muted Teal) ---
    # Isometric cube/prism radius
    rp = 148
    # Vertices of inner hexagon: 0=Top, 1=TopRight, 2=BotRight, 3=Bot, 4=BotLeft, 5=TopLeft
    v = [
        (cx + rp * math.cos(math.radians(-90 + i * 60.0)), cy + rp * math.sin(math.radians(-90 + i * 60.0)))
        for i in range(6)
    ]

    # Top-and-Left 'T' Monogram in Clean White (#F0F6F7):
    # Top isometric diamond cap + central vertical stem plunging to bottom vertex
    t_cap = [
        (sf(v[0][0]), sf(v[0][1])),
        (sf(v[1][0]), sf(v[1][1])),
        (sf(cx), sf(cy - 18)),
        (sf(v[5][0]), sf(v[5][1])),
    ]
    d.polygon(t_cap, fill=TYCHO_WHITE)
    # Central vertical stem of 'T'
    stem_w = 24
    t_stem = [
        (sf(cx - stem_w), sf(cy - 32)),
        (sf(cx + stem_w), sf(cy - 32)),
        (sf(cx + stem_w), sf(v[3][1] - 14)),
        (sf(cx), sf(v[3][1])),
        (sf(cx - stem_w), sf(v[3][1] - 14)),
    ]
    d.polygon(t_stem, fill=TYCHO_WHITE)

    # Left isometric flank wing bar (completing the architectural symmetry)
    left_wing = [
        (sf(v[5][0]), sf(v[5][1] + 22)),
        (sf(cx - stem_w - 16), sf(cy + 10)),
        (sf(cx - stem_w - 16), sf(v[3][1] - 26)),
        (sf(v[4][0]), sf(v[4][1])),
    ]
    d.polygon(left_wing, fill=TYCHO_TEAL_DARK)

    # Right Isometric 'C' Chevron in Muted Teal (#489EA2):
    # Clean parallel-edged isometric 'C' on the right face of the cube (zero self-intersection)
    rx_in = cx + stem_w + 18     # 554
    rx_out = v[1][0]             # ~640
    # Slope of isometric right face is dy/dx = tan(30 deg) = +0.57735 from left to right (top edge)
    # and dy/dx = -0.57735 from left to right (bottom edge)
    dy_iso = (rx_out - rx_in) * math.tan(math.radians(30.0))  # ~49.7
    top_y_in = cy - 6
    bot_y_in = v[3][1] - 22
    bar_t = 34
    c_outer = [
        (sf(rx_in), sf(top_y_in)),
        (sf(rx_out), sf(top_y_in - dy_iso)),
        (sf(rx_out), sf(bot_y_in - dy_iso)),
        (sf(rx_in), sf(bot_y_in)),
        (sf(rx_in), sf(bot_y_in - bar_t)),
        (sf(rx_out - bar_t * 0.866), sf(bot_y_in - dy_iso - bar_t * 0.5)),
        (sf(rx_out - bar_t * 0.866), sf(top_y_in - dy_iso + bar_t * 1.5)),
        (sf(rx_in), sf(top_y_in + bar_t)),
    ]
    d.polygon(c_outer, fill=TYCHO_TEAL)

    # --- 2. Cold Corporate Wordmark Lockup ---
    f_main = load_font("ChakraPetch-Bold.ttf", 76)
    draw_tracked_text(d, 512, 650, "TYCHO", f_main, TYCHO_WHITE, tracking=18.0)

    # Muted teal rule + wide-tracked 'CONSORTIUM'
    d.rectangle((s(164), s(718), s(860), s(724)), fill=TYCHO_TEAL)
    f_cons = load_font("BeVietnamPro-SemiBold.ttf", 34)
    draw_tracked_text(d, 512, 764, "C O N S O R T I U M", f_cons, TYCHO_TEAL, tracking=6.5)
    d.rectangle((s(164), s(806), s(860), s(810)), fill=TYCHO_TEAL_DARK)

    f_sub = load_font("IBMPlexMono-Bold.ttf", 20)
    draw_tracked_text(d, 512, 852, "AUTOMATED LUNAR PROSPECTING  //  MERIDIAN STATION", f_sub, TYCHO_WHITE, tracking=2.2)
    draw_tracked_text(d, 512, 896, "HEX-DOCK TELEMETRY & RESOURCE EXTRACTION  ·  L5", f_sub, TYCHO_SLATE, tracking=2.0)

    return downsample(img)


def build_tycho_b() -> Image.Image:
    """
    Tycho Consortium Direction B: 'The Deep-Radar Hexagonal Sounding Array'
    Downward-stepping hexagonal radar chevrons beneath a stark white lunar datum spire,
    visually encoding Meridian Station's automated 36-year deep-ice radar soundings.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Wide flat-top hexagonal shield (cx=512, cy=336)
    cx, cy = 512, 336
    hex_w, hex_h = 376, 246
    shield_pts = [
        (sf(cx - 210), sf(cy - hex_h)),
        (sf(cx + 210), sf(cy - hex_h)),
        (sf(cx + hex_w), sf(cy)),
        (sf(cx + 210), sf(cy + hex_h)),
        (sf(cx - 210), sf(cy + hex_h)),
        (sf(cx - hex_w), sf(cy)),
    ]
    d.polygon(shield_pts, fill=TYCHO_PANEL, outline=TYCHO_TEAL, width=s(10))

    # Inner white frame lines along top and bottom flats
    d.line([(s(cx - 194), s(cy - hex_h + 22)), (s(cx + 194), s(cy - hex_h + 22))], fill=TYCHO_WHITE, width=s(6))
    d.line([(s(cx - 194), s(cy + hex_h - 22)), (s(cx + 194), s(cy + hex_h - 22))], fill=TYCHO_TEAL_DARK, width=s(6))

    # --- Three Nested Downward Half-Hexagonal Radar Sounding Chevrons (Muted Teal) ---
    datum_y = 268
    for idx, (span_x, depth_y, thickness, col) in enumerate([
        (268, 256, 22, TYCHO_TEAL_DARK),
        (206, 198, 22, TYCHO_TEAL),
        (144, 140, 22, (112, 196, 200, 255)),
    ]):
        # Half-hexagon chevron from left datum -> bottom-left -> bottom-right -> right datum
        w_flat = span_x * 0.46
        y_bot = datum_y + depth_y
        outer = [
            (sf(cx - span_x), sf(datum_y + 16)),
            (sf(cx - w_flat), sf(y_bot)),
            (sf(cx + w_flat), sf(y_bot)),
            (sf(cx + span_x), sf(datum_y + 16)),
            (sf(cx + span_x - thickness * 1.15), sf(datum_y + 16)),
            (sf(cx + w_flat - thickness * 0.6), sf(y_bot - thickness)),
            (sf(cx - w_flat + thickness * 0.6), sf(y_bot - thickness)),
            (sf(cx - span_x + thickness * 1.15), sf(datum_y + 16)),
        ]
        d.polygon(outer, fill=col)

    # Central vertical deep-sounding radar axis beam piercing through the chevrons
    d.rectangle((s(cx - 16), s(160), s(cx + 16), s(532)), fill=TYCHO_PANEL)
    d.rectangle((s(cx - 7), s(168), s(cx + 7), s(536)), fill=TYCHO_WHITE)

    # --- Stark Clean White Lunar Rim Horizon Bar & Tycho Central Spire ---
    d.rectangle((s(cx - 306), s(datum_y - 12), s(cx + 306), s(datum_y + 12)), fill=TYCHO_WHITE)

    # Central hexagonal radar spire / Tycho peak rising above the horizon bar
    spire_pts = [
        (sf(cx), sf(132)),
        (sf(cx + 58), sf(datum_y - 8)),
        (sf(cx - 58), sf(datum_y - 8)),
    ]
    d.polygon(spire_pts, fill=TYCHO_WHITE)
    # Inner teal facet on spire
    d.polygon([(sf(cx), sf(158)), (sf(cx + 32), sf(datum_y - 8)), (sf(cx), sf(datum_y - 8))], fill=TYCHO_TEAL)

    # --- 2. Corporate Lockup Below Shield ---
    f_main = load_font("ChakraPetch-Bold.ttf", 58)
    draw_tracked_text(d, 512, 650, "TYCHO CONSORTIUM", f_main, TYCHO_WHITE, tracking=6.0)

    # Hexagonal-end Muted Teal ribbon bar (wide with generous margin around text)
    rx0, ry0, rx1, ry1 = 108, 714, 916, 778
    rmid_y = (ry0 + ry1) / 2.0
    ribbon_hex = [
        (sf(rx0 + 26), sf(ry0)),
        (sf(rx1 - 26), sf(ry0)),
        (sf(rx1), sf(rmid_y)),
        (sf(rx1 - 26), sf(ry1)),
        (sf(rx0 + 26), sf(ry1)),
        (sf(rx0), sf(rmid_y)),
    ]
    d.polygon(ribbon_hex, fill=TYCHO_TEAL)
    f_rib = load_font("ChakraPetch-Bold.ttf", 27)
    draw_tracked_text(d, 512, rmid_y, "DEEP-RADAR & RESOURCE EXTRACTION", f_rib, TYCHO_PANEL, tracking=3.5)

    f_sub = load_font("IBMPlexMono-Bold.ttf", 20)
    draw_tracked_text(d, 512, 836, "UNCREWED PROSPECTING OPERATIONS  ·  RIM TERRACES", f_sub, TYCHO_WHITE, tracking=2.2)
    draw_tracked_text(d, 512, 882, "KESTREL-3 RELAY LINK  //  AUTONOMOUS SOUNDING QUEUE", f_sub, TYCHO_TEAL, tracking=2.0)

    return downsample(img)


def build_tycho_c() -> Image.Image:
    """
    Tycho Consortium Direction C: 'The Isometric Hex-Vault & Tri-Facet Syndicate Emblem'
    Late-1970s brutalist corporate mark built from three solid interlocking isometric chevron
    wings arranged in a 120-degree hexagonal turbine around a solid white core diamond (zero holes).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Brutalist chamfered rectangular corporate frame (x: 136..888, y: 78..592)
    bx0, by0, bx1, by1 = 136, 78, 888, 592
    ch = 48
    frame_pts = [
        (sf(bx0 + ch), sf(by0)),
        (sf(bx1 - ch), sf(by0)),
        (sf(bx1), sf(by0 + ch)),
        (sf(bx1), sf(by1 - ch)),
        (sf(bx1 - ch), sf(by1)),
        (sf(bx0 + ch), sf(by1)),
        (sf(bx0), sf(by1 - ch)),
        (sf(bx0), sf(by0 + ch)),
    ]
    d.polygon(frame_pts, fill=TYCHO_PANEL, outline=TYCHO_WHITE, width=s(8))

    # Top and bottom heavy corporate bracket bars
    d.rectangle((s(bx0 + ch + 16), s(by0 + 18), s(bx1 - ch - 16), s(by0 + 32)), fill=TYCHO_TEAL)
    d.rectangle((s(bx0 + ch + 16), s(by1 - 32), s(bx1 - ch - 16), s(by1 - 18)), fill=TYCHO_TEAL)

    # --- Three Solid Interlocking Isometric Chevron Wings (120-degree rotational triad, ZERO holes) ---
    cx, cy = 512, 336
    # Outer flat-top hexagonal perimeter ring in Muted Teal
    d.polygon(reg_polygon(cx, cy, 212, 6, rot_deg=0), outline=TYCHO_TEAL_DARK, width=s(5))

    # Build 3 solid heavy geometric chevron blades rotated by 0, 120, 240 degrees
    blade_colors = [TYCHO_WHITE, TYCHO_TEAL, (108, 192, 196, 255)]
    base_blade = [
        (0.0, -42.0),
        (0.0, -188.0),
        (126.0, -115.0),
        (126.0, 26.0),
        (74.0, 56.0),
        (74.0, -84.0),
    ]
    for i, col in enumerate(blade_colors):
        ang = math.radians(i * 120.0)
        ca, sa = math.cos(ang), math.sin(ang)
        pts = []
        for px, py in base_blade:
            rx = cx + px * ca - py * sa
            ry = cy + px * sa + py * ca
            pts.append((sf(rx), sf(ry)))
        d.polygon(pts, fill=col)

    # Central solid white hexagonal core prism (zero holes)
    d.polygon(reg_polygon(cx, cy, 32, 6, rot_deg=30), fill=TYCHO_WHITE)

    # Left and right corporate division stencil tags inside the frame
    f_vert = load_font("IBMPlexMono-Bold.ttf", 18)
    d.text((s(bx0 + 36), s(by1 - 68)), "DIV // 81-TC", font=f_vert, fill=TYCHO_SLATE)
    d.text((s(bx1 - 196), s(by1 - 68)), "MERIDIAN // 0.7", font=f_vert, fill=TYCHO_SLATE)

    # --- 2. Brutalist Corporate Wordmark Lockup ---
    d.rectangle((s(bx0), s(632), s(512), s(644)), fill=TYCHO_WHITE)
    d.rectangle((s(512), s(632), s(bx1), s(644)), fill=TYCHO_TEAL)

    f_main = load_font("ChakraPetch-Bold.ttf", 58)
    draw_tracked_text(d, 512, 704, "TYCHO // CONSORTIUM", f_main, TYCHO_WHITE, tracking=5.5)

    f_sub = load_font("IBMPlexMono-Bold.ttf", 22)
    f_sub2 = load_font("IBMPlexMono-Medium.ttf", 19)
    draw_tracked_text(d, 512, 774, "AUTONOMOUS RESOURCE COOPERATIVE  ·  MERIDIAN SECTOR", f_sub, TYCHO_TEAL, tracking=2.2)
    d.rectangle((s(180), s(818), s(844), s(822)), fill=TYCHO_SLATE)
    draw_tracked_text(d, 512, 862, "HEXAGONAL CONTACT DOCKS  //  UNCREWED PROSPECTING FLEET", f_sub2, TYCHO_WHITE, tracking=2.2)

    return downsample(img)


def generate_brand_tycho():
    out_dir = OUT_ROOT / "tycho"
    out_dir.mkdir(parents=True, exist_ok=True)
    im_a = build_tycho_a()
    im_b = build_tycho_b()
    im_c = build_tycho_c()

    im_a.save(out_dir / "a.png")
    im_b.save(out_dir / "b.png")
    im_c.save(out_dir / "c.png")

    make_on_black(im_a, "TYCHO // DIRECTION A", "HEX-DOCK & ISOMETRIC TC PRISM").save(out_dir / "a_black.png")
    make_on_black(im_b, "TYCHO // DIRECTION B", "DEEP-RADAR HEXAGONAL SOUNDING ARRAY").save(out_dir / "b_black.png")
    make_on_black(im_c, "TYCHO // DIRECTION C", "TRI-HEX SYNDICATE TRIAD").save(out_dir / "c_black.png")

    sheet = make_brand_sheet_on_black(
        [im_a, im_b, im_c],
        ["A · HEX-DOCK TC PRISM", "B · DEEP-RADAR ARRAY", "C · TRI-HEX SYNDICATE"],
        "3 · TYCHO CONSORTIUM (AUTOMATED MERIDIAN STATION · PROSPECTING)",
        TYCHO_TEAL,
    )
    sheet.save(out_dir / "on_black.png")
    print("[build_logos] Generated Tycho Consortium logos in", out_dir)


# ==============================================================================
# BRAND 4: VIS — Vanguard Interplanetary Structures (1960s–1985)
# ILC's structures contractor since the 1960s (cargo lander, habitat, workshop,
# radio tower, solar arrays, crates, the 1967 Scout). Heavy plant, ribbed
# cylinders, round hatches.
# ==============================================================================

VIS_STEEL = (108, 122, 140, 255)      # #6C7A8C structural truss steel
VIS_STEEL_DARK = (58, 68, 82, 255)    # #3A4452 dark rib shadow
VIS_PANEL_DARK = (20, 24, 32, 255)    # #141820 heavy plant dark plate
VIS_BRASS = (224, 166, 68, 255)       # #E0A644 1967 Scout-3 brass yoke gold


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
    """
    Draws the core 3-letter 'VIS' TRW/RCA-era heavy block stencil monogram around (cx, cy).
    Completely solid geometric polygons + clean stencil bridges (zero holes/dots).
    Base reference box at scale=1.0 is width ~380, height ~190.
    """
    def pt(dx: float, dy: float) -> tuple:
        return (sf(cx + dx * scale), sf(cy + dy * scale))

    # --- Letter V (left: x from -188 to -52, y from -92 to +92) ---
    # Outer V contour with flat heavy bottom apex and angled truss legs
    v_left_leg = [
        pt(-188, -92),
        pt(-142, -92),
        pt(-108, 42),
        pt(-114, 92),
        pt(-142, 92),
    ]
    v_right_leg = [
        pt(-98, -92),
        pt(-52, -92),
        pt(-98, 92),
        pt(-130, 92),
        pt(-118, 42),
    ]
    # Draw unified V and cut a vertical stencil bridge through the bottom apex
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
    # Stencil bridge cut at bottom center of V so left and right halves are stencil-separated
    d.polygon([pt(-125, 10), pt(-115, 10), pt(-115, 96), pt(-125, 96)], fill=bg_cut)

    # --- Letter I (center ribbed column: x from -24 to +24, y from -92 to +92) ---
    # Top and bottom heavy slab serifs/caps with a stencil split in the middle for the cross-bar
    d.polygon([pt(-24, -92), pt(24, -92), pt(24, -16), pt(-24, -16)], fill=col_i)
    d.polygon([pt(-24, 16), pt(24, 16), pt(24, 92), pt(-24, 92)], fill=col_i)

    # --- Letter S (right: x from +52 to +188, y from -92 to +92) ---
    # 1968 RCA-style chamfered geometric block S constructed in 3 stencil-bridged segments
    c_cham = 28
    # Top bar + upper-left drop of S
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
    # Middle diagonal/horizontal spine of S (separated by 10px stencil bridges)
    s_mid = [
        pt(64, -8),
        pt(176, -8),
        pt(188, 4),
        pt(188, 18),
        pt(52, 18),
        pt(52, -8),
    ]
    d.polygon(s_mid, fill=col_s)
    # Bottom right drop + bottom bar of S
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

    # Central horizontal pressure-seal tie-bar across the I (and notched into V/S)
    if col_bar is not None:
        d.polygon([pt(-36, -9), pt(36, -9), pt(36, 9), pt(-36, 9)], fill=col_bar)


def build_vis_a() -> Image.Image:
    """
    VIS Direction A (★ PICK): '1968 RCA/TRW Ribbed-Cylinder Block Stencil'
    Bold 3-letter 'VIS' industrial block-slab mark locked inside a ribbed pressure-cylinder
    stadium frame with horizontal stiffening ribs, plus 1967 -> 1985 wordmark evolution bar.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx, cy = 512, 310
    # Outer ribbed-cylinder pressure module backing plate
    d.rounded_rectangle((s(118), s(104), s(906), s(516)), radius=s(88), fill=VIS_PANEL_DARK)

    # Top and bottom external structural cylinder ribs (3 wide solid trapezoidal lugs on top & bottom)
    for rx0, rx1 in [(248, 368), (452, 572), (656, 776)]:
        # Top rib flange
        d.polygon(
            [(sf(rx0), sf(104)), (sf(rx0 + 14), sf(82)), (sf(rx1 - 14), sf(82)), (sf(rx1), sf(104))],
            fill=CREAM,
        )
        # Bottom rib flange
        d.polygon(
            [(sf(rx0), sf(516)), (sf(rx0 + 14), sf(538)), (sf(rx1 - 14), sf(538)), (sf(rx1), sf(516))],
            fill=CREAM,
        )

    # Main stadium pressure-hull border in Cream + inner Coral-Orange structural seal ring
    d.rounded_rectangle((s(126), s(112), s(898), s(508)), radius=s(82), outline=CREAM, width=s(11))
    d.rounded_rectangle((s(146), s(132), s(878), s(488)), radius=s(64), outline=CORAL, width=s(5))

    # Left and right round hatch / bulkhead side-flange clamps (solid geometric bars, zero holes)
    for side_x, dx in [(146, 1), (878, -1)]:
        d.polygon(
            [
                (sf(side_x), sf(238)),
                (sf(side_x + 32 * dx), sf(254)),
                (sf(side_x + 32 * dx), sf(366)),
                (sf(side_x), sf(382)),
            ],
            fill=CORAL,
        )
        d.line(
            [(sf(side_x + 42 * dx), sf(186)), (sf(side_x + 42 * dx), sf(434))],
            fill=VIS_STEEL_DARK,
            width=s(4),
        )

    # Horizontal ribbed-cylinder background grooves inside the frame
    for ry in [184, 436]:
        d.line([(s(204), s(ry)), (s(820), s(ry))], fill=VIS_STEEL_DARK, width=s(5))

    # Draw the monumental 3-letter 'VIS' stencil mark in the center
    draw_vis_monogram_a(
        d,
        cx=512,
        cy=310,
        scale=1.22,
        col_v=CREAM,
        col_i=CREAM,
        col_s=CREAM,
        col_bar=CORAL,
        bg_cut=VIS_PANEL_DARK,
    )

    # Stencilled spec micro-labels inside the badge
    f_spec = load_font("IBMPlexMono-Bold.ttf", 13.5)
    draw_tracked_text(d, 512, 160, "ILC-R STANDARD · RIBBED PRESSURE STRUCTURES · HULL STENCIL", f_spec, AMBER, tracking=1.6)
    draw_tracked_text(d, 512, 460, "LANDERS · HABITATS · WORKSHOPS · TOWERS · SOLAR ARRAYS", f_spec, VIS_STEEL, tracking=1.8)

    # --- 1985 Wordmark Lockup ---
    f_main = load_font("ChakraPetch-Bold.ttf", 50)
    f_sub = load_font("ChakraPetch-Bold.ttf", 34)
    f_meta = load_font("IBMPlexMono-Bold.ttf", 18)

    draw_tracked_text(d, 512, 598, "VANGUARD INTERPLANETARY", f_main, CREAM, tracking=5.8)

    # Coral-orange structural girder bar with 'STRUCTURES'
    d.rounded_rectangle((s(164), s(646), s(860), s(710)), radius=s(8), fill=CORAL)
    # Stencil notch cutouts on left and right of orange bar
    d.rectangle((s(164), s(672), s(186), s(684)), fill=VIS_PANEL_DARK)
    d.rectangle((s(838), s(672), s(860), s(684)), fill=VIS_PANEL_DARK)
    draw_tracked_text(d, 512, 678, "S T R U C T U R E S", f_sub, WARM_DARK, tracking=8.0)

    # --- 1967 -> 1985 Wordmark Maturation Comparison Strip ---
    d.rounded_rectangle((s(96), s(746), s(928), s(884)), radius=s(10), fill=VIS_PANEL_DARK, outline=VIS_STEEL_DARK, width=s(3))
    d.line([(s(512), s(758)), (s(512), s(872))], fill=VIS_STEEL_DARK, width=s(3))

    f_era_lbl = load_font("IBMPlexMono-Bold.ttf", 13.5)
    f_1967_wm = load_font("IBMPlexMono-SemiBold.ttf", 14.5)
    f_1985_wm = load_font("ChakraPetch-Bold.ttf", 16.5)

    # Left half (x: 96..512, center=304): 1967 Scout-3 Era Wordmark
    draw_tracked_text(d, 304, 772, "1967 WORDMARK (SCOUT-3 PROBE ERA)", f_era_lbl, VIS_BRASS, tracking=0.8)
    draw_tracked_text(d, 304, 808, "V.I.S. · VANGUARD INTERPLANETARY", f_1967_wm, CREAM, tracking=0.8)
    draw_tracked_text(d, 304, 836, "STRUCTURES CORPORATION", f_1967_wm, VIS_BRASS, tracking=2.2)
    # 1967 vector underline with delta arrow
    d.line([(s(134), s(858)), (s(462), s(858))], fill=CORAL, width=s(3))
    d.polygon([(sf(456), sf(851)), (sf(474), sf(858)), (sf(456), sf(865))], fill=CORAL)

    # Right half (x: 512..928, center=720): 1985 Matured ILC-R Wordmark
    draw_tracked_text(d, 720, 772, "1985 WORDMARK (LUMEN-04 / ILC-R ERA)", f_era_lbl, CORAL, tracking=0.8)
    draw_tracked_text(d, 720, 808, "VIS · VANGUARD INTERPLANETARY", f_1985_wm, CREAM, tracking=1.2)
    d.rectangle((s(556), s(828), s(884), s(858)), fill=CORAL)
    draw_tracked_text(d, 720, 843, "STRUCTURES  ·  ILC-R", f_era_lbl, WARM_DARK, tracking=2.8)

    draw_tracked_text(
        d,
        512,
        926,
        "ILC PRIME STRUCTURES CONTRACTOR  ·  1967 SCOUT-3  →  1985 LUMEN STATION",
        f_meta,
        AMBER,
        tracking=1.8,
    )

    return downsample(img)


def build_vis_b() -> Image.Image:
    """
    VIS Direction B: '1970s TRW Stepped-Monogram & Round Airlock Hatch'
    Inspired by Saul Bass's stepped TRW logotype locked inside VIS's signature round
    airlock hatch and ribbed cylinder cross-section (4 solid dogging latches, zero holes).
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx, cy = 512, 314
    r_outer = 228

    # 4 external solid rectangular hatch-dogging lugs at 45, 135, 225, 315 deg
    for ang_deg in [45, 135, 225, 315]:
        rad = math.radians(ang_deg)
        nx, ny = math.cos(rad), math.sin(rad)
        tx, ty = -ny, nx
        r0, r1, hw = 202, 252, 28
        pts = [
            (sf(cx + nx * r0 - tx * hw), sf(cy + ny * r0 - ty * hw)),
            (sf(cx + nx * r1 - tx * (hw - 6)), sf(cy + ny * r1 - ty * (hw - 6))),
            (sf(cx + nx * r1 + tx * (hw - 6)), sf(cy + ny * r1 + ty * (hw - 6))),
            (sf(cx + nx * r0 + tx * hw), sf(cy + ny * r0 + ty * hw)),
        ]
        d.polygon(pts, fill=CORAL)

    # Round hatch backing disc
    d.ellipse((s(cx - r_outer), s(cy - r_outer), s(cx + r_outer), s(cy + r_outer)), fill=VIS_PANEL_DARK)

    # Interior horizontal ribbed-cylinder bands + stepped VIS monogram clipped cleanly inside the round hatch
    hatch_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hatch_layer)
    for ry in [-132, -96, 96, 132]:
        hd.rectangle((s(cx - 190), s(cy + ry - 7), s(cx + 190), s(cy + ry + 7)), fill=VIS_STEEL_DARK)

    # TRW-inspired Stepped 'V - I - S' Monogram inside the circular hatch (fits inside r=184)
    # V (x: 344..450, y: 264..398)
    v_pts = [
        (sf(344), sf(264)),
        (sf(380), sf(264)),
        (sf(397), sf(354)),
        (sf(414), sf(264)),
        (sf(450), sf(264)),
        (sf(414), sf(398)),
        (sf(380), sf(398)),
    ]
    hd.polygon(v_pts, fill=CREAM)
    hd.rectangle((s(393), s(346), s(401), s(402)), fill=VIS_PANEL_DARK)

    # I (stepped up 20px: x: 476..526, y: 244..378)
    hd.rectangle((s(476), s(244), s(526), s(304)), fill=CORAL)
    hd.rectangle((s(476), s(318), s(526), s(378)), fill=CORAL)

    # S (stepped up another 20px: x: 552..674, y: 224..358)
    s_x0, s_x1 = 552, 674
    s_y0, s_y1 = 224, 358
    s_ym = (s_y0 + s_y1) // 2  # 291
    ch = 20
    hd.polygon(
        [
            (sf(s_x0 + ch), sf(s_y0)),
            (sf(s_x1), sf(s_y0)),
            (sf(s_x1), sf(s_y0 + 32)),
            (sf(s_x0 + 38), sf(s_y0 + 32)),
            (sf(s_x0 + 38), sf(s_ym - 12)),
            (sf(s_x0), sf(s_ym - 12)),
            (sf(s_x0), sf(s_y0 + ch)),
        ],
        fill=CREAM,
    )
    hd.polygon(
        [
            (sf(s_x0), sf(s_ym - 5)),
            (sf(s_x1 - 10), sf(s_ym - 5)),
            (sf(s_x1), sf(s_ym + 5)),
            (sf(s_x1), sf(s_ym + 14)),
            (sf(s_x0 + 10), sf(s_ym + 14)),
            (sf(s_x0), sf(s_ym + 4)),
        ],
        fill=AMBER,
    )
    hd.polygon(
        [
            (sf(s_x1 - 38), sf(s_ym + 22)),
            (sf(s_x1), sf(s_ym + 22)),
            (sf(s_x1), sf(s_y1 - ch)),
            (sf(s_x1 - ch), sf(s_y1)),
            (sf(s_x0), sf(s_y1)),
            (sf(s_x0), sf(s_y1 - 32)),
            (sf(s_x1 - 38), sf(s_y1 - 32)),
        ],
        fill=CREAM,
    )

    # Stepped baseline structural rule under V - I - S
    hd.line([(s(344), s(412)), (s(450), s(412))], fill=CORAL, width=s(6))
    hd.line([(s(476), s(392)), (s(526), s(392))], fill=AMBER, width=s(6))
    hd.line([(s(552), s(372)), (s(674), s(372))], fill=CORAL, width=s(6))

    hatch_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(hatch_mask).ellipse((s(cx - 188), s(cy - 188), s(cx + 188), s(cy + 188)), fill=255)
    img.paste(hatch_layer, (0, 0), hatch_mask)
    d = ImageDraw.Draw(img)

    # Crisp double circular airlock hatch rings
    draw_thick_arc(d, cx, cy, 220, 220, 0, 360, 12.0, CREAM)
    draw_thick_arc(d, cx, cy, 196, 196, 0, 360, 5.5, CORAL)

    # Typography below
    f_main = load_font("ChakraPetch-Bold.ttf", 50)
    f_sub = load_font("ChakraPetch-SemiBold.ttf", 32)
    f_meta = load_font("IBMPlexMono-Bold.ttf", 18)
    f_era = load_font("IBMPlexMono-SemiBold.ttf", 15)

    draw_tracked_text(d, 512, 608, "VANGUARD INTERPLANETARY", f_main, CREAM, tracking=5.8)
    draw_tracked_text(d, 512, 668, "STRUCTURES", f_sub, CORAL, tracking=14.0)

    d.line([(s(156), s(712)), (s(868), s(712))], fill=CREAM, width=s(4))
    d.line([(s(236), s(724)), (s(788), s(724))], fill=CORAL, width=s(3))

    # 1967 vs 1985 wordmark evolution callout box
    d.rounded_rectangle((s(96), s(752), s(928), s(864)), radius=s(8), fill=VIS_PANEL_DARK, outline=VIS_STEEL_DARK, width=s(3))
    draw_tracked_text(
        d,
        512,
        784,
        "1967: V.I.S. VANGUARD INTERPLANETARY STRUCTURES CORP. (SCOUT-3)",
        f_era,
        VIS_BRASS,
        tracking=1.0,
    )
    draw_tracked_text(
        d,
        512,
        830,
        "1985: VIS · VANGUARD INTERPLANETARY STRUCTURES · ILC-R (LUMEN-04)",
        f_era,
        CREAM,
        tracking=1.0,
    )

    draw_tracked_text(
        d,
        512,
        916,
        "HEAVY PLANT  ·  RIBBED CYLINDERS  ·  ROUND HATCHES  ·  EST. 1963",
        f_meta,
        AMBER,
        tracking=2.2,
    )

    return downsample(img)


def build_vis_c() -> Image.Image:
    """
    VIS Direction C: '1980s Integrated Girder-Truss & Corrugated Crate Stencil'
    Heavy octagonal crate/bulkhead stencil frame with top/bottom corrugated cylinder ribs,
    where V-I-S is carved from a continuous structural I-beam girder and tower truss.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Chamfered octagonal heavy-plant plate (x: 112..912, y: 108..516)
    ch = 58
    x0, y0, x1, y1 = 112, 108, 912, 516
    oct_pts = [
        (sf(x0 + ch), sf(y0)),
        (sf(ix1 := x1 - ch), sf(y0)),
        (sf(x1), sf(y0 + ch)),
        (sf(x1), sf(y1 - ch)),
        (sf(x1 - ch), sf(y1)),
        (sf(x0 + ch), sf(y1)),
        (sf(x0), sf(y1 - ch)),
        (sf(x0), sf(y0 + ch)),
    ]
    d.polygon(oct_pts, fill=VIS_PANEL_DARK)
    d.polygon(oct_pts, outline=CREAM, width=s(10))

    # Inner chamfered border
    ch2 = 48
    ix0, iy0, ix1, iy1 = 134, 130, 890, 494
    in_pts = [
        (sf(ix0 + ch2), sf(iy0)),
        (sf(ix1 - ch2), sf(iy0)),
        (sf(ix1), sf(iy0 + ch2)),
        (sf(ix1), sf(iy1 - ch2)),
        (sf(ix1 - ch2), sf(iy1)),
        (sf(ix0 + ch2), sf(iy1)),
        (sf(ix0), sf(iy1 - ch2)),
        (sf(ix0), sf(iy0 + ch2)),
    ]
    d.polygon(in_pts, outline=VIS_STEEL, width=s(4))

    # Top and bottom continuous I-beam structural chords inside the plate
    d.rectangle((s(184), s(166), s(840), s(194)), fill=CORAL)
    d.rectangle((s(184), s(430), s(840), s(458)), fill=CORAL)
    # Stencil breaks along the top/bottom chords
    for bx in [398, 594]:
        d.rectangle((s(bx), s(162), s(bx + 14), s(198)), fill=VIS_PANEL_DARK)
        d.rectangle((s(bx), s(426), s(bx + 14), s(462)), fill=VIS_PANEL_DARK)

    # Monolithic Girder 'V I S' between y=210 and y=414
    # V (x: 196..384)
    d.polygon(
        [
            (sf(196), sf(210)),
            (sf(248), sf(210)),
            (sf(290), sf(346)),
            (sf(332), sf(210)),
            (sf(384), sf(210)),
            (sf(316), sf(414)),
            (sf(264), sf(414)),
        ],
        fill=CREAM,
    )
    d.rectangle((s(285), s(336), s(295), s(418)), fill=VIS_PANEL_DARK)

    # I (x: 428..556 — wide girder H/I pillar with diagonal radio-tower truss brace)
    d.rectangle((s(428), s(210), s(556), s(248)), fill=CREAM)
    d.rectangle((s(428), s(376), s(556), s(414)), fill=CREAM)
    d.rectangle((s(468), s(258), s(516), s(366)), fill=AMBER)
    # Diagonal structural truss cut across the center column
    d.polygon(
        [(sf(462), sf(298)), (sf(522), sf(278)), (sf(522), sf(292)), (sf(462), sf(312))],
        fill=VIS_PANEL_DARK,
    )
    d.polygon(
        [(sf(462), sf(332)), (sf(522), sf(312)), (sf(522), sf(326)), (sf(462), sf(346))],
        fill=VIS_PANEL_DARK,
    )

    # S (x: 600..828 — heavy angular girder S)
    d.polygon(
        [
            (sf(628), sf(210)),
            (sf(828), sf(210)),
            (sf(828), sf(250)),
            (sf(652), sf(250)),
            (sf(652), sf(292)),
            (sf(600), sf(292)),
            (sf(600), sf(238)),
        ],
        fill=CREAM,
    )
    d.polygon(
        [
            (sf(600), sf(302)),
            (sf(800), sf(302)),
            (sf(828), sf(322)),
            (sf(628), sf(322)),
        ],
        fill=CORAL,
    )
    d.polygon(
        [
            (sf(776), sf(332)),
            (sf(828), sf(332)),
            (sf(828), sf(386)),
            (sf(800), sf(414)),
            (sf(600), sf(414)),
            (sf(600), sf(374)),
            (sf(776), sf(374)),
        ],
        fill=CREAM,
    )

    # Typography below
    f_main = load_font("ChakraPetch-Bold.ttf", 50)
    f_sub = load_font("IBMPlexMono-Bold.ttf", 21)
    f_era = load_font("IBMPlexMono-SemiBold.ttf", 15.5)

    draw_tracked_text(d, 512, 594, "VANGUARD INTERPLANETARY", f_main, CREAM, tracking=5.8)
    draw_tracked_text(d, 512, 654, "STRUCTURES", f_main, CORAL, tracking=12.0)

    d.rectangle((s(136), s(702), s(888), s(710)), fill=AMBER)

    # Dual 1967 / 1985 wordmark heritage bar
    d.rounded_rectangle((s(96), s(736), s(928), s(856)), radius=s(8), fill=VIS_PANEL_DARK, outline=VIS_STEEL_DARK, width=s(3))
    draw_tracked_text(d, 512, 768, "1967 ORIG:  V.I.S. · VANGUARD INTERPLANETARY STRUCTURES CORP.", f_era, VIS_BRASS, tracking=1.2)
    d.line([(s(140), s(796)), (s(884), s(796))], fill=VIS_STEEL_DARK, width=s(2))
    draw_tracked_text(d, 512, 826, "1985 SPEC:  VIS · VANGUARD INTERPLANETARY STRUCTURES · ILC-R", f_era, CREAM, tracking=1.2)

    draw_tracked_text(
        d,
        512,
        912,
        "LANDERS · HABITATS · WORKSHOPS · TOWERS · SOLAR ARRAYS · CRATES",
        f_sub,
        AMBER,
        tracking=1.5,
    )

    return downsample(img)


def build_vis_1967() -> Image.Image:
    """
    Generates Logs/logos/vis/1967.png (1024x1024):
    The 1967 original VIS wordmark & emblem from the 'Scout 3' soft-lander era
    (two years before the 1969 Moon landing). Shows the mid-1960s double-line 'V-I-S'
    inside an asymmetric Scout tripod/delta wing, slab-serifed 'CORPORATION' wordmark,
    and sweeping orbital vector tail.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Outer 1967 anodized brass-and-charcoal equipment plate so it reads on any background
    d.rounded_rectangle((s(56), s(56), s(968), s(968)), radius=s(28), fill=(18, 20, 28, 255), outline=VIS_BRASS, width=s(7))
    d.rounded_rectangle((s(74), s(74), s(950), s(950)), radius=s(20), outline=(92, 78, 52, 255), width=s(3))

    # Top era header bar
    d.rectangle((s(74), s(74), s(950), s(138)), fill=(32, 28, 24, 255))
    d.line([(s(74), s(138)), (s(950), s(138))], fill=VIS_BRASS, width=s(4))
    f_hdr = load_font("IBMPlexMono-Bold.ttf", 19)
    draw_tracked_text(
        d,
        512,
        106,
        "1967 ORIGINAL WORDMARK  ·  SCOUT-3 LUNAR SOFT-LANDER ERA",
        f_hdr,
        VIS_BRASS,
        tracking=2.0,
    )

    # --- 1967 Mid-Century Space-Age Emblem (y: 170..465) ---
    cx, cy = 512, 318
    # Tilted thin 1960s orbital ring
    draw_thick_arc(d, cx, cy + 8, 265, 82, -165, 175, 7.0, VIS_BRASS, rot_deg=-16, round_caps=True)
    draw_thick_arc(d, cx, cy + 8, 242, 66, -140, 150, 3.0, CORAL, rot_deg=-16, round_caps=True)

    # Central 1967 diamond/shield badge
    shield_pts = [
        (sf(512), sf(162)),
        (sf(716), sf(278)),
        (sf(652), sf(442)),
        (sf(372), sf(442)),
        (sf(308), sf(278)),
    ]
    d.polygon(shield_pts, fill=(24, 26, 36, 255), outline=CREAM, width=s(6))
    inner_shield = [
        (sf(512), sf(178)),
        (sf(696), sf(284)),
        (sf(638), sf(428)),
        (sf(386), sf(428)),
        (sf(328), sf(284)),
    ]
    d.polygon(inner_shield, outline=VIS_BRASS, width=s(3))

    # Scout-3 three-legged lander silhouette struts at base of shield
    d.line([(s(430), s(386)), (s(388), s(436))], fill=VIS_BRASS, width=s(6))
    d.line([(s(594), s(386)), (s(636), s(436))], fill=VIS_BRASS, width=s(6))
    d.line([(s(512), s(386)), (s(512), s(438))], fill=VIS_BRASS, width=s(6))
    for fx in [388, 512, 636]:
        d.line([(s(fx - 18), s(438)), (s(fx + 18), s(438))], fill=CORAL, width=s(6))

    # 1967 Thin Double-Line Inline 'V - I - S' Monogram inside the shield (kept cleanly inside pentagon)
    for col, w_line in [(CREAM, 14), ((24, 26, 36, 255), 4)]:
        d.line([(s(382), s(266)), (s(422), s(368)), (s(462), s(266))], fill=col, width=s(w_line), joint="curve")
    # I (with flared top and bottom slab serifs, double-stroke)
    d.line([(s(488), s(266)), (s(536), s(266))], fill=CREAM, width=s(10))
    d.line([(s(488), s(368)), (s(536), s(368))], fill=CREAM, width=s(10))
    d.line([(s(512), s(266)), (s(512), s(368))], fill=CREAM, width=s(15))
    d.line([(s(512), s(274)), (s(512), s(360))], fill=(24, 26, 36, 255), width=s(4))
    # S (slender angular 1960s double-line S)
    s67_pts = [
        (s(638), s(278)),
        (s(624), s(266)),
        (s(576), s(266)),
        (s(562), s(280)),
        (s(562), s(306)),
        (s(576), s(316)),
        (s(624), s(320)),
        (s(638), s(332)),
        (s(638), s(354)),
        (s(624), s(368)),
        (s(572), s(368)),
        (s(558), s(354)),
    ]
    d.line(s67_pts, fill=CREAM, width=s(14), joint="curve")
    d.line(s67_pts, fill=(24, 26, 36, 255), width=s(4), joint="curve")

    # 4-pointed 1967 astrometric star at apex
    draw_4pt_star(d, 512, 212, 22, 6, VIS_BRASS)

    # --- 1967 Full Wordmark Lockup (y: 490..720) ---
    f_w67_top = load_font("ChakraPetch-SemiBold.ttf", 40)
    f_w67_mid = load_font("ChakraPetch-Bold.ttf", 46)
    f_w67_corp = load_font("IBMPlexMono-SemiBold.ttf", 24)
    f_w67_sub = load_font("IBMPlexMono-Medium.ttf", 16)

    draw_tracked_text(d, 512, 514, "V A N G U A R D", f_w67_mid, CREAM, tracking=8.0)
    draw_tracked_text(d, 512, 574, "INTERPLANETARY STRUCTURES", f_w67_top, VIS_BRASS, tracking=3.8)
    draw_tracked_text(d, 512, 628, "C O R P O R A T I O N", f_w67_corp, CORAL, tracking=6.5)

    # Iconic 1967 sweeping tapered vector tail + delta dart beneath wordmark
    tail_pts = [
        (sf(132), sf(666)),
        (sf(796), sf(660)),
        (sf(782), sf(646)),
        (sf(884), sf(664)),
        (sf(782), sf(682)),
        (sf(796), sf(668)),
        (sf(132), sf(672)),
    ]
    d.polygon(tail_pts, fill=VIS_BRASS)

    draw_tracked_text(
        d,
        512,
        708,
        "AEROSPACE & LUNAR PROBE STRUCTURES  ·  SCOUT-3 CONTRACT (1967)",
        f_w67_sub,
        CREAM,
        tracking=1.6,
    )

    # --- Bottom 1967 Design Anatomy Box (y: 748..922) ---
    d.rounded_rectangle((s(88), s(748), s(936), s(920)), radius=s(12), fill=(12, 14, 20, 255), outline=(72, 64, 46, 255), width=s(3))
    f_box_t = load_font("IBMPlexMono-Bold.ttf", 15.5)
    f_box_b = load_font("IBMPlexMono-Regular.ttf", 13.0)
    draw_tracked_text(d, 512, 776, "1967 WORDMARK CHARACTERISTICS (BEFORE 1985 STANDARDIZATION)", f_box_t, VIS_BRASS, tracking=1.0)
    draw_tracked_text(d, 512, 814, "1. DOUBLE-LINE INLINE 'VIS' MONOGRAM: Intricate thin parallel strokes & 5-sided Scout shield.", f_box_b, CREAM, tracking=0.0)
    draw_tracked_text(d, 512, 846, "2. THREE-STACKED 'CORPORATION' TITLE: Wide-tracked 1960s aerospace lettering in brass gold.", f_box_b, CREAM, tracking=0.0)
    draw_tracked_text(d, 512, 878, "3. TAPERED VECTOR TAIL: Sweeping 1960s delta-arrow underline (retired in 1985 for hull stencils).", f_box_b, CORAL, tracking=0.0)

    return downsample(img)


def build_vis_1985() -> Image.Image:
    """
    Generates Logs/logos/vis/1985.png (1024x1024):
    The matured 1985 VIS wordmark & industrial stencil mark from the 'Lumen-04 / ILC-R'
    Departure era. Shows how the 1967 mark stripped its thin inline strokes, tripod shield,
    and 'CORPORATION' suffix to become a bold single-weight TRW/RCA-era stencil lockup.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Outer 1985 heavy-plant dark steel & coral-orange specification plate
    d.rounded_rectangle((s(56), s(56), s(968), s(968)), radius=s(28), fill=VIS_PANEL_DARK, outline=CORAL, width=s(7))
    d.rounded_rectangle((s(74), s(74), s(950), s(950)), radius=s(20), outline=VIS_STEEL_DARK, width=s(3))

    # Top era header bar
    d.rectangle((s(74), s(74), s(950), s(138)), fill=(34, 22, 18, 255))
    d.line([(s(74), s(138)), (s(950), s(138))], fill=CORAL, width=s(4))
    f_hdr = load_font("IBMPlexMono-Bold.ttf", 19)
    draw_tracked_text(
        d,
        512,
        106,
        "1985 MATURED WORDMARK  ·  LUMEN-04 / ILC-R HEAVY STRUCTURES ERA",
        f_hdr,
        CORAL,
        tracking=1.8,
    )

    # --- 1985 Matured Heavy Stencil Emblem (y: 162..452) ---
    d.rounded_rectangle((s(156), s(164), s(868), s(452)), radius=s(68), fill=(12, 15, 22, 255), outline=CREAM, width=s(9))
    d.rounded_rectangle((s(174), s(182), s(850), s(434)), radius=s(52), outline=CORAL, width=s(4))

    # Ribbed cylinder flanges on top and bottom
    for rx0, rx1 in [(272, 376), (460, 564), (648, 752)]:
        d.polygon([(sf(rx0), sf(164)), (sf(rx0 + 12), sf(146)), (sf(rx1 - 12), sf(146)), (sf(rx1), sf(164))], fill=CREAM)
        d.polygon([(sf(rx0), sf(452)), (sf(rx0 + 12), sf(470)), (sf(rx1 - 12), sf(470)), (sf(rx1), sf(452))], fill=CREAM)

    draw_vis_monogram_a(
        d,
        cx=512,
        cy=308,
        scale=1.18,
        col_v=CREAM,
        col_i=CREAM,
        col_s=CREAM,
        col_bar=CORAL,
        bg_cut=(12, 15, 22, 255),
    )

    # --- 1985 Matured Wordmark Lockup (y: 492..716) ---
    f_w85_main = load_font("ChakraPetch-Bold.ttf", 45)
    f_w85_sub = load_font("ChakraPetch-Bold.ttf", 34)
    f_w85_meta = load_font("IBMPlexMono-Bold.ttf", 15.0)

    # Top structural I-beam rule
    d.rectangle((s(124), s(494), s(900), s(504)), fill=CORAL)
    draw_tracked_text(d, 512, 548, "VANGUARD INTERPLANETARY", f_w85_main, CREAM, tracking=4.8)

    # Solid Coral-Orange Stencil Banner for 'STRUCTURES'
    d.rounded_rectangle((s(124), s(596), s(900), s(662)), radius=s(8), fill=CORAL)
    d.rectangle((s(124), s(622), s(148), s(636)), fill=VIS_PANEL_DARK)
    d.rectangle((s(876), s(622), s(900), s(636)), fill=VIS_PANEL_DARK)
    draw_tracked_text(d, 512, 629, "S T R U C T U R E S", f_w85_sub, WARM_DARK, tracking=9.0)

    draw_tracked_text(
        d,
        512,
        702,
        "ILC-R PRIME STRUCTURES CONTRACTOR · CARGO LANDERS · HABITATS · TOWERS · CRATES",
        f_w85_meta,
        AMBER,
        tracking=0.9,
    )

    # --- Bottom 1967 -> 1985 Maturation Summary Box (y: 748..922) ---
    d.rounded_rectangle((s(88), s(748), s(936), s(920)), radius=s(12), fill=(12, 14, 20, 255), outline=VIS_STEEL_DARK, width=s(3))
    f_box_t = load_font("IBMPlexMono-Bold.ttf", 15.5)
    f_box_b = load_font("IBMPlexMono-Regular.ttf", 13.0)
    draw_tracked_text(d, 512, 776, "HOW THE 1967 WORDMARK MATURED INTO THE 1985 VERSION", f_box_t, CORAL, tracking=1.0)
    draw_tracked_text(d, 512, 814, "1. INLINE -> SOLID STENCIL: 1967 thin double-lines fused into 1985 heavy single-weight slabs.", f_box_b, CREAM, tracking=0.0)
    draw_tracked_text(d, 512, 846, "2. STREAMLINED NAME: Dropped 'CORPORATION' & periods; locked into a 2-tier geometric grid.", f_box_b, CREAM, tracking=0.0)
    draw_tracked_text(d, 512, 878, "3. HULL-READY GEOMETRY: 24px stencil bridges & ribbed-cylinder frame survive vacuum abrasion.", f_box_b, AMBER, tracking=0.0)

    return downsample(img)


def build_vis_stencil() -> Image.Image:
    """
    Generates Logs/logos/vis/stencil.png (1024x1024 RGB):
    Hull-stencil mock: the VIS mark & wordmark stencilled on a ribbed cream enamel panel
    (#F4E6C8) with a warm coral-orange stripe (#FF8A5B), weathered/worn in Lunarion's
    low-poly/flat-facet art style (zero clusters of dots or holes).
    """
    img = Image.new("RGBA", (W, H), (235, 220, 190, 255))
    d = ImageDraw.Draw(img)

    # 1. Base Cream Enamel Hull Panel (#F4E6C8) with subtle horizontal structural rib bands
    d.rectangle((0, 0, W, H), fill=CREAM)

    # Horizontal ribbed hull corrugations (top lit bevel, mid face, lower shadow bevel)
    rib_ys = [88, 196, 780, 912]
    for ry in rib_ys:
        # Sunlit top bevel
        d.rectangle((0, s(ry - 18), W, s(ry - 6)), fill=(252, 242, 220, 255))
        # Raised rib face
        d.rectangle((0, s(ry - 6), W, s(ry + 14)), fill=(236, 221, 190, 255))
        # Earthlit underside shadow bevel
        d.rectangle((0, s(ry + 14), W, s(ry + 26)), fill=(196, 182, 154, 255))

    # Vertical hull plate seam lines near left and right edges + rectangular weld clamps (ZERO round rivets)
    for sx in [76, 948]:
        d.line([(s(sx), 0), (s(sx), H)], fill=(176, 162, 136, 255), width=s(5))
        d.line([(s(sx + 5), 0), (s(sx + 5), H)], fill=(250, 240, 216, 255), width=s(3))
        # Rectangular flat seam clamps (3 widely spaced clamps per seam, zero holes)
        for cy_clamp in [142, 512, 846]:
            d.rectangle(
                (s(sx - 18), s(cy_clamp - 12), s(sx + 22), s(cy_clamp + 12)),
                fill=(162, 148, 124, 255),
                outline=(120, 108, 90, 255),
                width=s(3),
            )
            # Subtle vertical rust/oxide streak running down from clamp (VISION.md line 243)
            d.polygon(
                [
                    (sf(sx - 8), sf(cy_clamp + 12)),
                    (sf(sx + 10), sf(cy_clamp + 12)),
                    (sf(sx + 4), sf(cy_clamp + 78)),
                    (sf(sx - 4), sf(cy_clamp + 64)),
                ],
                fill=(208, 142, 108, 255),
            )

    # Low-poly flat-facet sun-faded enamel patches on the cream hull
    fade_polys = [
        [(120, 40), (380, 40), (330, 165), (95, 140)],
        [(680, 215), (935, 215), (910, 340), (720, 310)],
        [(110, 795), (420, 805), (390, 895), (105, 885)],
    ]
    for poly in fade_polys:
        d.polygon([(sf(x), sf(y)) for x, y in poly], fill=(232, 216, 184, 255))

    # 2. Warm Coral-Orange Hull Stripe (#FF8A5B) across lower-middle panel (y: 556..718)
    stripe_y0, stripe_y1 = 556, 718
    d.rectangle((s(78), s(stripe_y0), s(946), s(stripe_y1)), fill=CORAL)
    # Narrow amber trim rule above the coral stripe
    d.rectangle((s(78), s(stripe_y0 - 18), s(946), s(stripe_y0 - 6)), fill=AMBER)

    # Worn / chipped low-poly facets along the orange stripe edges (exposing cream primer & slate metal)
    stripe_chips = [
        [(78, 556), (168, 556), (142, 588), (78, 596)],
        [(390, 556), (465, 556), (448, 574), (404, 570)],
        [(770, 556), (860, 556), (842, 584), (788, 576)],
        [(210, 696), (310, 688), (335, 718), (195, 718)],
        [(620, 692), (745, 684), (768, 718), (604, 718)],
        [(882, 640), (946, 622), (946, 718), (896, 718)],
    ]
    for chip in stripe_chips:
        d.polygon([(sf(x), sf(y)) for x, y in chip], fill=(236, 220, 188, 255))

    # Darker sun-baked oxide patch on right side of orange stripe
    d.polygon(
        [(sf(710), sf(580)), (sf(910), sf(572)), (sf(890), sf(668)), (sf(740), sf(650))],
        fill=RUST,
    )

    # 3. Stencilled 'VIS' Industrial Mark on the Cream Hull Panel (y: 232..514)
    stencil_ink = (36, 42, 52, 255)       # Worn charcoal stencil paint
    stencil_ink_faded = (58, 64, 74, 255) # Slightly sun-faded charcoal

    st_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(st_layer)

    # Outer stencil frame corners & horizontal ribbed-hull bars in charcoal stencil ink
    sd.polygon(
        [(sf(148), sf(232)), (sf(216), sf(232)), (sf(216), sf(248)), (sf(166), sf(248)),
         (sf(166), sf(498)), (sf(216), sf(498)), (sf(216), sf(514)), (sf(148), sf(514))],
        fill=stencil_ink,
    )
    sd.polygon(
        [(sf(876), sf(232)), (sf(808), sf(232)), (sf(808), sf(248)), (sf(858), sf(248)),
         (sf(858), sf(498)), (sf(808), sf(498)), (sf(808), sf(514)), (sf(876), sf(514))],
        fill=stencil_ink,
    )
    # Top and bottom broken stencil frame bars (with clear 20px breathing gap above/below VIS)
    for bx0, bx1 in [(242, 382), (412, 612), (642, 782)]:
        sd.rectangle((s(bx0), s(232), s(bx1), s(246)), fill=stencil_ink)
        sd.rectangle((s(bx0), s(500), s(bx1), s(514)), fill=stencil_ink)

    # Bold 3-letter 'VIS' stencil mark in center of cream panel (y spans 373 +- 106 = 267..479)
    draw_vis_monogram_a(
        sd,
        cx=512,
        cy=373,
        scale=1.15,
        col_v=stencil_ink,
        col_i=stencil_ink,
        col_s=stencil_ink_faded,
        col_bar=RUST,
        bg_cut=(0, 0, 0, 0),
    )

    # Re-punch the stencil bridges on st_layer with transparent alpha so cream hull shows through
    sd.rectangle((s(512 - 144), s(373 + 10), s(512 - 131), s(373 + 115)), fill=(0, 0, 0, 0))
    sd.rectangle((s(512 - 32), s(373 - 20), s(512 + 32), s(373 - 10)), fill=(0, 0, 0, 0))
    sd.rectangle((s(512 - 32), s(373 + 10), s(512 + 32), s(373 + 20)), fill=(0, 0, 0, 0))

    # 4. Stencilled Wordmark inside the Coral-Orange Stripe (knockout Cream stencil)
    f_stripe_main = load_font("ChakraPetch-Bold.ttf", 44)
    f_stripe_sub = load_font("IBMPlexMono-Bold.ttf", 21)
    draw_tracked_text(
        sd,
        512,
        610,
        "VANGUARD INTERPLANETARY",
        f_stripe_main,
        CREAM,
        tracking=5.0,
    )
    draw_tracked_text(
        sd,
        512,
        668,
        "STRUCTURES  ·  ILC-R PRIME CONTRACTOR",
        f_stripe_sub,
        (30, 24, 20, 255),
        tracking=3.2,
    )

    # 5. Stencilled Hull Manifest markings at top and bottom of panel (well clear of x=76 and x=948 clamps)
    f_hull_code = load_font("IBMPlexMono-Bold.ttf", 16)
    f_hull_sm = load_font("IBMPlexMono-SemiBold.ttf", 15)
    draw_tracked_text(
        sd,
        512,
        142,
        "ILC-R · PRESSURE MODULE HULL SEC-04 · LOT 85-V · MASS 4 200 KG",
        f_hull_code,
        stencil_ink_faded,
        tracking=1.2,
    )
    draw_tracked_text(
        sd,
        512,
        848,
        "FORM + WELD COMPATIBLE  //  ROVER WORK BAND 0.35–1.46m  //  VIS-1985",
        f_hull_sm,
        stencil_ink_faded,
        tracking=1.5,
    )

    # 6. Apply realistic low-poly wear, diagonal scrape scratches, and paint chips across the stencil layer
    wear_scratches = [
        # Diagonal micro-meteorite / cargo-handling scrape across V and I
        [(230, 290), (244, 284), (430, 445), (418, 452)],
        # Scrape across the S and right bracket
        [(630, 260), (642, 256), (790, 410), (778, 416)],
        # Chip on the V left leg
        [(264, 330), (306, 322), (298, 358), (258, 352)],
        # Chip on the S bottom bar
        [(690, 458), (748, 450), (742, 492), (684, 486)],
        # Scrape across the wordmark on the orange stripe
        [(310, 585), (324, 582), (410, 688), (396, 692)],
        [(660, 590), (672, 586), (735, 682), (722, 686)],
    ]
    for wpoly in wear_scratches:
        sd.polygon([(sf(x), sf(y)) for x, y in wpoly], fill=(0, 0, 0, 0))

    img.alpha_composite(st_layer)
    d = ImageDraw.Draw(img)

    # Draw visible pale metal/primer scratch traces along the main diagonal scrapes
    d.polygon(
        [(sf(232), sf(290)), (sf(240), sf(286)), (sf(426), sf(445)), (sf(420), sf(449))],
        fill=(212, 198, 172, 255),
    )
    d.polygon(
        [(sf(632), sf(260)), (sf(639), sf(257)), (sf(786), sf(410)), (sf(780), sf(414))],
        fill=(212, 198, 172, 255),
    )

    # 7. Lunar Regolith Dust Drift piled gently along the bottom rib ledge (VISION.md line 244)
    dust_poly = [
        (0, sf(946)),
        (sf(180), sf(924)),
        (sf(410), sf(936)),
        (sf(690), sf(918)),
        (sf(890), sf(932)),
        (W, sf(920)),
        (W, H),
        (0, H),
    ]
    d.polygon(dust_poly, fill=(166, 167, 171, 255))
    dust_shadow = [
        (0, sf(968)),
        (sf(240), sf(952)),
        (sf(560), sf(960)),
        (sf(820), sf(946)),
        (W, sf(954)),
        (W, H),
        (0, H),
    ]
    d.polygon(dust_shadow, fill=(120, 122, 128, 255))

    return downsample(img).convert("RGB")


def generate_brand_vis():
    out_dir = OUT_ROOT / "vis"
    out_dir.mkdir(parents=True, exist_ok=True)

    a = build_vis_a()
    b = build_vis_b()
    c = build_vis_c()
    w1967 = build_vis_1967()
    w1985 = build_vis_1985()
    stencil = build_vis_stencil()

    a.save(out_dir / "a.png")
    b.save(out_dir / "b.png")
    c.save(out_dir / "c.png")

    make_on_black(a, "VIS", "DIRECTION A").save(out_dir / "a_black.png")
    make_on_black(b, "VIS", "DIRECTION B").save(out_dir / "b_black.png")
    make_on_black(c, "VIS", "DIRECTION C").save(out_dir / "c_black.png")

    w1967.save(out_dir / "1967.png")
    w1985.save(out_dir / "1985.png")
    make_on_black(w1967, "VIS", "1967 WORDMARK").save(out_dir / "1967_black.png")
    make_on_black(w1985, "VIS", "1985 WORDMARK").save(out_dir / "1985_black.png")

    stencil.save(out_dir / "stencil.png")

    sheet = make_brand_sheet_on_black(
        [a, b, c],
        ["A · RCA/TRW RIBBED BLOCK", "B · STEPPED HATCH RING", "C · GIRDER CRATE STENCIL"],
        "4 · VIS (VANGUARD INTERPLANETARY STRUCTURES)",
        CORAL,
    )
    sheet.save(out_dir / "on_black.png")
    print("[build_logos] Generated VIS logos in", out_dir)


def generate_index_html():
    """Writes Logs/logos/index.html showing all 4 brands side by side with a one-line rationale each."""
    html = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Lunarion — Brand Identities &amp; Maker Logos (1970s–80s Concepts)</title>
  <style>
    :root {
      --bg: #0a0c12;
      --card: #121520;
      --border: #2a3142;
      --cream: #f4e6c8;
      --coral: #ff8a5b;
      --amber: #ffb547;
      --teal: #489ea2;
      --muted: #9aa6b8;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background: var(--bg);
      color: var(--cream);
      font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
      line-height: 1.5;
      padding: 32px 40px 64px;
      max-width: 1640px;
      margin: 0 auto;
    }
    header {
      border-bottom: 2px solid var(--border);
      padding-bottom: 20px;
      margin-bottom: 32px;
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      flex-wrap: wrap;
      gap: 16px;
    }
    h1 {
      font-size: 1.75rem;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--cream);
    }
    .subtitle {
      color: var(--amber);
      font-family: ui-monospace, 'Cascadia Mono', 'Consolas', monospace;
      font-size: 0.92rem;
      margin-top: 6px;
    }
    .controls {
      display: flex;
      gap: 10px;
    }
    .btn {
      background: #1a2030;
      color: var(--cream);
      border: 1px solid var(--border);
      padding: 8px 14px;
      border-radius: 6px;
      cursor: pointer;
      font-family: ui-monospace, monospace;
      font-size: 0.84rem;
    }
    .btn.active {
      border-color: var(--amber);
      color: var(--amber);
      background: #261e14;
    }
    section.brand {
      margin-bottom: 48px;
      background: #0e111a;
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 24px 28px 28px;
    }
    .brand-header {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      border-bottom: 1px solid var(--border);
      padding-bottom: 12px;
      margin-bottom: 20px;
      flex-wrap: wrap;
      gap: 8px;
    }
    .brand-title {
      font-size: 1.3rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      text-transform: uppercase;
    }
    .brand-meta {
      font-family: ui-monospace, monospace;
      font-size: 0.86rem;
      color: var(--muted);
    }
    .grid-3 {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 22px;
    }
    .card {
      background: var(--card);
      border: 1px solid var(--border);
      border-radius: 8px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }
    .card.pick {
      border-color: var(--amber);
      box-shadow: 0 0 0 1px rgba(255, 181, 71, 0.25);
    }
    .img-wrap {
      width: 100%;
      aspect-ratio: 1 / 1;
      background: #080a0f;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 12px;
      border-bottom: 1px solid var(--border);
    }
    body.checker .img-wrap.trans-preview {
      background-image:
        linear-gradient(45deg, #1a1e29 25%, transparent 25%),
        linear-gradient(-45deg, #1a1e29 25%, transparent 25%),
        linear-gradient(45deg, transparent 75%, #1a1e29 75%),
        linear-gradient(-45deg, transparent 75%, #1a1e29 75%);
      background-size: 24px 24px;
      background-position: 0 0, 0 12px, 12px -12px, -12px 0;
      background-color: #10131c;
    }
    .img-wrap img {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }
    .card-body {
      padding: 14px 16px 16px;
    }
    .card-label {
      font-family: ui-monospace, monospace;
      font-size: 0.9rem;
      font-weight: 700;
      color: var(--amber);
      margin-bottom: 6px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .badge-pick {
      background: var(--amber);
      color: #181006;
      font-size: 0.72rem;
      padding: 2px 7px;
      border-radius: 4px;
      font-weight: 800;
    }
    .rationale {
      font-size: 0.92rem;
      color: var(--cream);
    }
    .file-links {
      margin-top: 8px;
      font-family: ui-monospace, monospace;
      font-size: 0.78rem;
      color: var(--muted);
    }
    .file-links a {
      color: var(--coral);
      text-decoration: none;
    }
    .crt-section {
      margin-top: 26px;
      padding-top: 22px;
      border-top: 1px dashed var(--border);
    }
    .crt-title {
      font-family: ui-monospace, monospace;
      font-size: 0.95rem;
      color: var(--amber);
      margin-bottom: 14px;
      text-transform: uppercase;
    }
  </style>
</head>
<body class="checker">
  <header>
    <div>
      <h1>Lunarion — Maker &amp; Agency Brand Identities (1970s–80s Concepts)</h1>
      <div class="subtitle">1024×1024 Transparent PNGs + On-Black Frames + 07 CRT Boot Screen + VIS 1967→1985 &amp; Worn Hull Stencil · Zero Dot/Hole Clusters</div>
    </div>
    <div class="controls">
      <button class="btn active" id="btn-trans" onclick="setMode('trans')">Transparent PNGs (a/b/c.png)</button>
      <button class="btn" id="btn-black" onclick="setMode('black')">On-Black Frames (a/b/c_black.png)</button>
    </div>
  </header>

  <!-- 1. ILC -->
  <section class="brand">
    <div class="brand-header">
      <div class="brand-title" style="color: var(--coral);">1 · ILC (International Lunar Commission)</div>
      <div class="brand-meta">1969 onward · The international lunar agency (NASA-like role) · Earnest, geometric, 1970s–80s space-programme print style</div>
    </div>
    <div class="grid-3">
      <div class="card pick">
        <div class="img-wrap trans-preview"><img data-trans="ilc/a.png" data-black="ilc/a_black.png" src="ilc/a.png" alt="ILC Direction A" /></div>
        <div class="card-body">
          <div class="card-label"><span>ILC · DIRECTION A — 1975 ORBITAL WORM</span><span class="badge-pick">★ PICK</span></div>
          <p class="rationale">1975 Danne &amp; Blackburn "Worm"-inspired tubular logotype paired with an Earth-to-Moon transfer ellipse and L4/L5 apex star—earnest, optimistic, and instantly readable on a station bulkhead.</p>
          <div class="file-links">Files: <a href="ilc/a.png">ilc/a.png</a> (transparent) · <a href="ilc/a_black.png">ilc/a_black.png</a> · <a href="ilc/on_black.png">ilc/on_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="ilc/b.png" data-black="ilc/b_black.png" src="ilc/b.png" alt="ILC Direction B" /></div>
        <div class="card-body">
          <div class="card-label"><span>ILC · DIRECTION B — 1969 MISSION SEAL</span></div>
          <p class="rationale">1969–78 Apollo/UN-era circular agency insignia uniting Earth's meridian dome, the lunar disc, and a rising coral-orange delta vector—built for suit patches and lander hull decals.</p>
          <div class="file-links">Files: <a href="ilc/b.png">ilc/b.png</a> (transparent) · <a href="ilc/b_black.png">ilc/b_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="ilc/c.png" data-black="ilc/c_black.png" src="ilc/c.png" alt="ILC Direction C" /></div>
        <div class="card-body">
          <div class="card-label"><span>ILC · DIRECTION C — 1982 SWISS WAYSTATION GRID</span></div>
          <p class="rationale">1982 Lance Wyman / Otl Aicher architectural grid badge combining the L4/L5 transit diagram, stencil "ILC", and hydrological strata bars for silkscreened station bulkheads.</p>
          <div class="file-links">Files: <a href="ilc/c.png">ilc/c.png</a> (transparent) · <a href="ilc/c_black.png">ilc/c_black.png</a></div>
        </div>
      </div>
    </div>
  </section>

  <!-- 2. KESSLER-ARAI -->
  <section class="brand">
    <div class="brand-header">
      <div class="brand-title" style="color: var(--amber);">2 · Kessler–Arai Field Systems</div>
      <div class="brand-meta">Built 07 (SSR-5, 1983) &amp; the SSR-3 elders (01–06) · Industrial, friendly, reliable field equipment · Cream &amp; Orange family</div>
    </div>
    <div class="grid-3">
      <div class="card pick">
        <div class="img-wrap trans-preview"><img data-trans="kessler-arai/a.png" data-black="kessler-arai/a_black.png" src="kessler-arai/a.png" alt="Kessler-Arai Direction A" /></div>
        <div class="card-body">
          <div class="card-label"><span>KESSLER–ARAI · DIRECTION A — LENS &amp; BOGIE BADGE</span><span class="badge-pick">★ PICK</span></div>
          <p class="rationale">Chunky rounded-bezel industrial monogram uniting a stencil "K–A" with 07's hooded single-lens eye and rocker-bogie suspension link—warm, approachable, and unmistakable on 07's 1-colour amber CRT.</p>
          <div class="file-links">Files: <a href="kessler-arai/a.png">kessler-arai/a.png</a> (transparent) · <a href="kessler-arai/a_black.png">kessler-arai/a_black.png</a> · <a href="kessler-arai/crt.png">kessler-arai/crt.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="kessler-arai/b.png" data-black="kessler-arai/b_black.png" src="kessler-arai/b.png" alt="Kessler-Arai Direction B" /></div>
        <div class="card-body">
          <div class="card-label"><span>KESSLER–ARAI · DIRECTION B — SOLAR CRATER STAMP</span></div>
          <p class="rationale">1980s Komatsu/Braun-inspired heavy machinery stamp pairing a warm rising solar disc (Arai) with a stepped crater-ridge chevron (Kessler) inside chamfered chassis bumper clamps.</p>
          <div class="file-links">Files: <a href="kessler-arai/b.png">kessler-arai/b.png</a> (transparent) · <a href="kessler-arai/b_black.png">kessler-arai/b_black.png</a> · <a href="kessler-arai/crt_b.png">kessler-arai/crt_b.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="kessler-arai/c.png" data-black="kessler-arai/c_black.png" src="kessler-arai/c.png" alt="Kessler-Arai Direction C" /></div>
        <div class="card-body">
          <div class="card-label"><span>KESSLER–ARAI · DIRECTION C — 1983 CHASSIS NAMEPLATE</span></div>
          <p class="rationale">1983 stamped-enamel equipment nameplate pairing a geometric 2D SSR-5 rover profile with a split-block "K/A" stencil and CONTINUE bus spec bars, ready to weld onto 07's rear flank.</p>
          <div class="file-links">Files: <a href="kessler-arai/c.png">kessler-arai/c.png</a> (transparent) · <a href="kessler-arai/c_black.png">kessler-arai/c_black.png</a> · <a href="kessler-arai/crt_c.png">kessler-arai/crt_c.png</a></div>
        </div>
      </div>
    </div>

    <div class="crt-section">
      <div class="crt-title">07 CRT Face · 1-Colour Amber Boot Screen Mockups ("KESSLER–ARAI FIELD SYSTEMS · SSR-5" · R1CrtFace.shader Phosphor &amp; Scanlines)</div>
      <div class="grid-3">
        <div class="card pick">
          <div class="img-wrap"><img src="kessler-arai/crt.png" alt="07 CRT Boot Variant A" /></div>
          <div class="card-body">
            <div class="card-label"><span>07 CRT BOOT · DIRECTION A (crt.png)</span><span class="badge-pick">★ PICK</span></div>
            <p class="rationale">1-colour amber phosphor boot screen on 07's curved CRT face before the self-test lines finish: the hooded-lens "K–A" badge reads immediately through scanlines and phosphor bloom.</p>
          </div>
        </div>
        <div class="card">
          <div class="img-wrap"><img src="kessler-arai/crt_b.png" alt="07 CRT Boot Variant B" /></div>
          <div class="card-body">
            <div class="card-label"><span>07 CRT BOOT · DIRECTION B (crt_b.png)</span></div>
            <p class="rationale">The rising solar disc &amp; crater-ridge lozenge rendered as a 1-colour amber phosphor boot emblem above the SSR-5 self-test readout.</p>
          </div>
        </div>
        <div class="card">
          <div class="img-wrap"><img src="kessler-arai/crt_c.png" alt="07 CRT Boot Variant C" /></div>
          <div class="card-body">
            <div class="card-label"><span>07 CRT BOOT · DIRECTION C (crt_c.png)</span></div>
            <p class="rationale">The stamped SSR-5 rover profile &amp; "K/A" chassis nameplate rendered in 1-colour amber phosphor on 07's boot screen.</p>
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- 3. TYCHO CONSORTIUM -->
  <section class="brand">
    <div class="brand-header">
      <div class="brand-title" style="color: var(--teal);">3 · Tycho Consortium</div>
      <div class="brand-meta">Rival industrial prospecting operator of automated Meridian Station · Clean White + Muted Teal · Hexagonal motifs, colder, corporate</div>
    </div>
    <div class="grid-3">
      <div class="card pick">
        <div class="img-wrap trans-preview"><img data-trans="tycho/a.png" data-black="tycho/a_black.png" src="tycho/a.png" alt="Tycho Direction A" /></div>
        <div class="card-body">
          <div class="card-label"><span style="color: var(--teal);">TYCHO · DIRECTION A — HEX-DOCK TC PRISM</span><span class="badge-pick">★ PICK</span></div>
          <p class="rationale">Precision hexagonal contact-dock emblem forming an interlocking isometric "TC" prism in stark white and muted teal—cold, corporate, and built to double as the physical alignment target on Meridian's hex docks.</p>
          <div class="file-links">Files: <a href="tycho/a.png">tycho/a.png</a> (transparent) · <a href="tycho/a_black.png">tycho/a_black.png</a> · <a href="tycho/on_black.png">tycho/on_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="tycho/b.png" data-black="tycho/b_black.png" src="tycho/b.png" alt="Tycho Direction B" /></div>
        <div class="card-body">
          <div class="card-label"><span style="color: var(--teal);">TYCHO · DIRECTION B — DEEP-RADAR SOUNDING ARRAY</span></div>
          <p class="rationale">Downward-stepping half-hexagonal radar chevrons beneath a stark white lunar datum spire, visually encoding Meridian Station's automated 36-year deep-ice radar soundings.</p>
          <div class="file-links">Files: <a href="tycho/b.png">tycho/b.png</a> (transparent) · <a href="tycho/b_black.png">tycho/b_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="tycho/c.png" data-black="tycho/c_black.png" src="tycho/c.png" alt="Tycho Direction C" /></div>
        <div class="card-body">
          <div class="card-label"><span style="color: var(--teal);">TYCHO · DIRECTION C — TRI-FACET HEXAGONAL TURBINE</span></div>
          <p class="rationale">Late-1970s brutalist corporate mark built from three solid interlocking isometric chevron wings arranged in a 120° hexagonal turbine around a solid white core prism (zero holes).</p>
          <div class="file-links">Files: <a href="tycho/c.png">tycho/c.png</a> (transparent) · <a href="tycho/c_black.png">tycho/c_black.png</a></div>
        </div>
      </div>
    </div>
  </section>

  <!-- 4. VIS — VANGUARD INTERPLANETARY STRUCTURES -->
  <section class="brand">
    <div class="brand-header">
      <div class="brand-title" style="color: var(--coral);">4 · VIS — Vanguard Interplanetary Structures</div>
      <div class="brand-meta">ILC's structures contractor since the 1960s (lander, habitat, workshop, tower, solar arrays, crates, 1967 Scout) · Heavy plant, ribbed cylinders, round hatches</div>
    </div>
    <div class="grid-3">
      <div class="card pick">
        <div class="img-wrap trans-preview"><img data-trans="vis/a.png" data-black="vis/a_black.png" src="vis/a.png" alt="VIS Direction A" /></div>
        <div class="card-body">
          <div class="card-label"><span>VIS · DIRECTION A — RCA/TRW RIBBED BLOCK STENCIL</span><span class="badge-pick">★ PICK</span></div>
          <p class="rationale">1968 RCA/TRW-era heavy block-slab "VIS" stencil mark locked inside a ribbed pressure-cylinder stadium frame with side bulkhead lugs and a 1967→1985 wordmark evolution bar.</p>
          <div class="file-links">Files: <a href="vis/a.png">vis/a.png</a> (transparent) · <a href="vis/a_black.png">vis/a_black.png</a> · <a href="vis/on_black.png">vis/on_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="vis/b.png" data-black="vis/b_black.png" src="vis/b.png" alt="VIS Direction B" /></div>
        <div class="card-body">
          <div class="card-label"><span>VIS · DIRECTION B — TRW STEPPED MONOGRAM &amp; ROUND HATCH</span></div>
          <p class="rationale">Saul Bass / TRW-inspired stepped "V–I–S" heavy-plant monogram rising across a round airlock hatch with four solid dogging latches and ribbed interior pressure bands.</p>
          <div class="file-links">Files: <a href="vis/b.png">vis/b.png</a> (transparent) · <a href="vis/b_black.png">vis/b_black.png</a></div>
        </div>
      </div>
      <div class="card">
        <div class="img-wrap trans-preview"><img data-trans="vis/c.png" data-black="vis/c_black.png" src="vis/c.png" alt="VIS Direction C" /></div>
        <div class="card-body">
          <div class="card-label"><span>VIS · DIRECTION C — GIRDER-TRUSS &amp; CRATE STENCIL</span></div>
          <p class="rationale">Chamfered octagonal crate-and-bulkhead stencil uniting "V I S" between continuous top and bottom structural I-beam chords with a radio-tower truss pillar.</p>
          <div class="file-links">Files: <a href="vis/c.png">vis/c.png</a> (transparent) · <a href="vis/c_black.png">vis/c_black.png</a></div>
        </div>
      </div>
    </div>

    <div class="crt-section">
      <div class="crt-title">VIS Wordmark Maturation (1967 Scout-3 Era → 1985 Lumen-04 Era) &amp; Worn Cream/Orange Hull-Stencil Mockup</div>
      <div class="grid-3">
        <div class="card">
          <div class="img-wrap trans-preview"><img data-trans="vis/1967.png" data-black="vis/1967_black.png" src="vis/1967.png" alt="VIS 1967 Wordmark" /></div>
          <div class="card-body">
            <div class="card-label"><span>VIS · 1967 WORDMARK (vis/1967.png)</span></div>
            <p class="rationale">1967 Scout-3 soft-lander era heritage mark: thin double-line "VIS" monogram inside a tripod lander shield above the three-tier "VANGUARD INTERPLANETARY STRUCTURES CORPORATION" wordmark and vector tail.</p>
            <div class="file-links">Files: <a href="vis/1967.png">vis/1967.png</a> · <a href="vis/1967_black.png">vis/1967_black.png</a></div>
          </div>
        </div>
        <div class="card pick">
          <div class="img-wrap trans-preview"><img data-trans="vis/1985.png" data-black="vis/1985_black.png" src="vis/1985.png" alt="VIS 1985 Wordmark" /></div>
          <div class="card-body">
            <div class="card-label"><span>VIS · 1985 MATURED WORDMARK (vis/1985.png)</span><span class="badge-pick">★ 1985 SPEC</span></div>
            <p class="rationale">1985 Lumen-04 / ILC-R era matured standard: inline strokes fused into a single-weight hull stencil, "CORPORATION" dropped, and typography locked into a two-tier industrial grid.</p>
            <div class="file-links">Files: <a href="vis/1985.png">vis/1985.png</a> · <a href="vis/1985_black.png">vis/1985_black.png</a></div>
          </div>
        </div>
        <div class="card pick">
          <div class="img-wrap"><img src="vis/stencil.png" alt="VIS Worn Hull Stencil Mock" /></div>
          <div class="card-body">
            <div class="card-label"><span>VIS · WORN HULL STENCIL MOCK (vis/stencil.png)</span><span class="badge-pick">★ HULL MOCK</span></div>
            <p class="rationale">The 1985 "VIS" mark and wordmark spray-stencilled across a ribbed cream enamel hull panel (#F4E6C8) with a worn coral-orange stripe (#FF8A5B), low-poly paint chips, seam oxide streaks, and basal regolith dust.</p>
            <div class="file-links">File: <a href="vis/stencil.png">vis/stencil.png</a></div>
          </div>
        </div>
      </div>
    </div>
  </section>

  <script>
    function setMode(mode) {
      document.getElementById('btn-trans').classList.toggle('active', mode === 'trans');
      document.getElementById('btn-black').classList.toggle('active', mode === 'black');
      document.body.classList.toggle('checker', mode === 'trans');
      document.querySelectorAll('img[data-trans]').forEach(img => {
        img.src = mode === 'trans' ? img.getAttribute('data-trans') : img.getAttribute('data-black');
      });
    }
  </script>
</body>
</html>
"""
    index_path = OUT_ROOT / "index.html"
    index_path.write_text(html, encoding="utf-8")
    print("[build_logos] Wrote gallery to", index_path)


if __name__ == "__main__":
    # Round 2: ILC / Kessler-Arai / Tycho are DONE - only generate VIS and update index.html
    generate_brand_vis()
    generate_index_html()



