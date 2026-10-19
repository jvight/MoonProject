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


if __name__ == "__main__":
    generate_brand_ilc()
    generate_brand_kessler()

