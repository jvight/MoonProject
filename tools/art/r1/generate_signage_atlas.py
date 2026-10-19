#!/usr/bin/env python3
"""Deterministic 1980s stencil signage, logo, hazard-stripe, and pictogram atlas generator for R1_Signage.

Produces 512x512 Albedo, Normal, and Mask maps (rendered at 1024x1024 2x supersampling for crisp edges)
with the following UV layout (V=0 at bottom, V=1 at top):
  Row 3 (V = 0.75 .. 1.00):
    - LOGO          (U = 0.00 .. 0.25): Circular Lumen Commission crescent-and-horizon emblem
    - LUMEN         (U = 0.25 .. 0.75): Bold stencil 'LUMEN' + 'INTERNATIONAL LUNAR PROGRAMME'
    - EVAC_1988     (U = 0.75 .. 1.00): Stencil plaque 'EVAC 1988'
  Row 2 (V = 0.50 .. 0.75):
    - SEC04_HABITAT  (U = 0.00 .. 0.50): Two-line stencil 'SEC-04' / 'HABITAT & LAB'
    - AIRLOCK_STATUS (U = 0.50 .. 1.00): Amber status display 'SEALED · 1988 · STANDBY' / 'SUIT 01-04 · OFFLINE'
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


from pathlib import Path


# Canonical UV cell bounds (u_min, v_min, u_max, v_max) with V=0 at bottom, V=1 at top
SIGNAGE_UV_CELLS: dict[str, tuple[float, float, float, float]] = {
    "LOGO": (0.00, 0.75, 0.25, 1.00),
    "ILC_SEAL": (0.00, 0.75, 0.25, 1.00),
    "LUMEN": (0.25, 0.75, 0.75, 1.00),
    "VIS_STENCIL": (0.25, 0.75, 0.75, 1.00),
    "EVAC_1988": (0.75, 0.75, 1.00, 1.00),
    "SEC_04": (0.75, 0.75, 1.00, 1.00),
    "SEC04_HABITAT": (0.00, 0.50, 0.50, 0.75),
    "VIS_1985": (0.00, 0.50, 0.50, 0.75),
    "AIRLOCK_STATUS": (0.50, 0.50, 1.00, 0.75),
    "HABITAT_LAB": (0.75, 0.50, 1.00, 0.75),
    "WORKSHOP": (0.00, 0.25, 0.50, 0.50),
    "HAZARD": (0.50, 0.25, 1.00, 0.50),
    "PICT_ROVER": (0.00, 0.00, 0.25, 0.25),
    "KA_MARK": (0.00, 0.00, 0.25, 0.25),
    "PICT_RADIO": (0.25, 0.00, 0.50, 0.25),
    "VIS_1967": (0.25, 0.00, 0.50, 0.25),
    "PICT_BOTANY": (0.50, 0.00, 0.75, 0.25),
    "CAUTION_RAD": (0.50, 0.00, 0.75, 0.25),
    "TYCHO_PANEL": (0.50, 0.00, 0.75, 0.25),
    "PICT_AIRLOCK": (0.75, 0.00, 1.00, 0.25),
    "LUMEN_STN": (0.75, 0.00, 1.00, 0.25),
    "TYCHO_MARK": (0.75, 0.00, 1.00, 0.25),
}

# Palette colours in 0..255 sRGB matching identity_v1_helios_lumen.png & identity_v3_cassette_futurism.png
CREAM_BG = (236, 226, 204)
CREAM_TILE = (242, 233, 212)
SCOUT_TAN_BG = (204, 179, 134)
TYCHO_HULL_BG = (224, 220, 210)
STENCIL_DARK = (32, 36, 48)
NAVY_DARK = (24, 29, 46)
ORANGE_BAND = (224, 114, 68)
AMBER_STRIPE = (232, 164, 56)
HAZARD_DARK = (36, 40, 50)
SAGE_ACCENT = (134, 162, 138)
STATUS_BEZEL_BG = (16, 12, 9)
STATUS_SCREEN_BG = (22, 15, 10)
STATUS_SCANLINE = (30, 20, 12)
STATUS_AMBER_BRIGHT = (255, 192, 78)
STATUS_AMBER_CORE = (255, 234, 164)
STATUS_AMBER_DIM = (182, 114, 38)
STATUS_AMBER_RULE = (132, 82, 28)

BRANDS_DIR = Path(__file__).resolve().parents[3] / "Assets" / "_Project" / "Art" / "R1" / "Brands"


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
    "Y": [
        (0.10, 0.04, 0.50, 0.52),
        (0.90, 0.04, 0.50, 0.52),
        (0.50, 0.52, 0.50, 0.96),
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


PICKED_DIR = Path(__file__).resolve().parents[3] / "Logs" / "picked"


def _stamp_worn_brand_mark(
    canvas: Image.Image,
    mark_filename: str,
    box: tuple[int, int, int, int],
    bg_color: tuple[int, int, int] = CREAM_BG,
    sun_fade: float = 0.22,
    scorch_strength: float = 0.0,
    badge_only_crop_frac: float | None = None,
    wordmark_split_y: float = 0.58,
    seed: int = 1985,
) -> None:
    """Load a production brand mark PNG from Assets/_Project/Art/R1/Brands/, apply era-specific
    solar UV sun-fading and non-periodic angular paint wear, and composite onto canvas.
    """
    x0, y0, x1, y1 = box
    w = max(1, x1 - x0)
    h = max(1, y1 - y0)
    mark_path = BRANDS_DIR / mark_filename
    if not mark_path.exists():
        return

    src = Image.open(mark_path).convert("RGBA")
    alpha_np = np.asarray(src, dtype=np.uint8)[..., 3]
    ys, xs = np.nonzero(alpha_np > 8)
    if len(xs) > 0 and len(ys) > 0:
        pad = 16
        bx0 = max(0, int(xs.min()) - pad)
        by0 = max(0, int(ys.min()) - pad)
        bx1 = min(src.width, int(xs.max()) + pad + 1)
        by1 = min(src.height, int(ys.max()) + pad + 1)
        if badge_only_crop_frac is not None:
            by1 = min(by1, by0 + int(round((by1 - by0) * badge_only_crop_frac)))
        src = src.crop((bx0, by0, bx1, by1))

    # Convert light-cream poster sub-caption letters (below wordmark_split_y) to dark stencil charcoal
    # when stamping onto a light hull background so the wordmark is crisp and legible.
    src_arr = np.asarray(src, dtype=np.float32) / 255.0
    bg_norm = np.asarray(bg_color, dtype=np.float32) / 255.0
    bg_luma = 0.299 * bg_norm[0] + 0.587 * bg_norm[1] + 0.114 * bg_norm[2]
    if bg_luma > 0.55 and badge_only_crop_frac is None:
        sh, sw = src_arr.shape[:2]
        split_row = int(round(sh * wordmark_split_y))
        sub = src_arr[split_row:, :, :]
        sub_rgb = sub[..., :3]
        sub_a = sub[..., 3]
        luma = 0.299 * sub_rgb[..., 0] + 0.587 * sub_rgb[..., 1] + 0.114 * sub_rgb[..., 2]
        chroma = np.max(sub_rgb, axis=-1) - np.min(sub_rgb, axis=-1)
        is_cream_letter = (sub_a > 0.15) & (luma > 0.76) & (chroma < 0.24)
        dark_ink = np.array([0.15, 0.17, 0.22], dtype=np.float32)
        sub_rgb[is_cream_letter] = dark_ink
        if bg_color == SCOUT_TAN_BG:
            is_colored_wordmark = (sub_a > 0.15) & (~is_cream_letter) & (luma > 0.42)
            sub_rgb[is_colored_wordmark] *= 0.48
        src_arr[split_row:, :, :3] = sub_rgb
        src = Image.fromarray(np.clip(np.rint(src_arr * 255.0), 0, 255).astype(np.uint8), mode="RGBA")

    # Preserve aspect ratio inside target box
    scale = min(w / max(1, src.width), h / max(1, src.height))
    nw = max(1, int(round(src.width * scale)))
    nh = max(1, int(round(src.height * scale)))
    resized = src.resize((nw, nh), resample=Image.Resampling.LANCZOS)

    patch = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = (w - nw) // 2
    py = (h - nh) // 2
    patch.paste(resized, (px, py))

    arr = np.asarray(patch, dtype=np.float32) / 255.0
    rgb = arr[..., :3]
    alpha = arr[..., 3]

    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    u = xx / max(1.0, float(w))
    v = yy / max(1.0, float(h))

    # Smooth solar UV bleaching gradient (stronger toward upper sun-facing half & outer edges)
    edge_dist = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
    edge_fade = np.exp(-edge_dist * 10.0) * 0.16
    uv_bleach = 0.65 + 0.35 * (0.65 * (1.0 - v) + 0.35 * (1.0 - u))
    fade_mix = np.clip(sun_fade * uv_bleach + edge_fade, 0.0, 0.78)
    rgb_worn = rgb * (1.0 - fade_mix[..., None]) + bg_norm[None, None, :] * fade_mix[..., None]
    alpha_worn = alpha

    base_crop = np.asarray(canvas.crop((x0, y0, x0 + w, y0 + h)), dtype=np.float32) / 255.0
    comp = base_crop * (1.0 - alpha_worn[..., None]) + rgb_worn * alpha_worn[..., None]

    if scorch_strength > 0.0:
        # Smooth thermal scorch & crash-site soot around panel edges for Kestrel-3 Tycho debris
        edge_dist = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
        edge_scorch = np.exp(-edge_dist * 8.5)
        diag_soot = np.clip(1.0 - (0.6 * u + 0.4 * v), 0.0, 1.0) ** 1.8
        soot = np.clip(scorch_strength * (0.70 * edge_scorch + 0.30 * diag_soot), 0.0, 0.72)
        soot_col = np.array([0.15, 0.14, 0.13], dtype=np.float32)
        comp = comp * (1.0 - soot[..., None]) + soot_col[None, None, :] * soot[..., None]

    out_u8 = np.clip(np.rint(comp * 255.0), 0, 255).astype(np.uint8)
    canvas.paste(Image.fromarray(out_u8, mode="RGB"), (x0, y0))


def _stamp_vis_hull_stencil(
    canvas: Image.Image,
    box: tuple[int, int, int, int],
    bg_color: tuple[int, int, int] = CREAM_BG,
    sun_fade: float = 0.14,
) -> None:
    """Stamp the owner's picked 1985 VIS hull stencil (Logs/picked/vis_stencil.png central bracketed
    '[ V I S ]' + coral-orange 'VANGUARD INTERPLANETARY / STRUCTURES' block) into box.
    """
    x0, y0, x1, y1 = box
    w = max(1, x1 - x0)
    h = max(1, y1 - y0)
    stencil_path = PICKED_DIR / "vis_stencil.png"
    if not stencil_path.exists():
        _stamp_worn_brand_mark(canvas, "vis_mark.png", box, bg_color=bg_color, sun_fade=sun_fade)
        return

    src = Image.open(stencil_path).convert("RGB")
    # Crop exact bracketed '[ V I S ]' + coral-orange banner from the 2048x2048 picked sheet
    sw, sh = src.size
    crop_box = (
        int(round(sw * 0.125)),
        int(round(sh * 0.210)),
        int(round(sw * 0.875)),
        int(round(sh * 0.705)),
    )
    cropped = src.crop(crop_box)
    scale = min(w / max(1, cropped.width), h / max(1, cropped.height))
    nw = max(1, int(round(cropped.width * scale)))
    nh = max(1, int(round(cropped.height * scale)))
    resized = np.asarray(cropped.resize((nw, nh), resample=Image.Resampling.LANCZOS), dtype=np.float32) / 255.0

    # Replace the sheet's pale cream background (#F3E8CC) with bg_color while preserving dark navy,
    # coral-orange, amber rule, and the worn diagonal scrape marks.
    sheet_bg = np.array([243.0 / 255.0, 232.0 / 255.0, 204.0 / 255.0], dtype=np.float32)
    bg_norm = np.asarray(bg_color, dtype=np.float32) / 255.0
    diff = np.linalg.norm(resized - sheet_bg[None, None, :], axis=-1)
    fg_mask = np.clip((diff - 0.04) / 0.08, 0.0, 1.0)
    fg_rgb = resized * (1.0 - sun_fade) + bg_norm[None, None, :] * sun_fade

    px = x0 + (w - nw) // 2
    py = y0 + (h - nh) // 2
    base_crop = np.asarray(canvas.crop((px, py, px + nw, py + nh)), dtype=np.float32) / 255.0
    comp = base_crop * (1.0 - fg_mask[..., None]) + fg_rgb * fg_mask[..., None]
    out_u8 = np.clip(np.rint(comp * 255.0), 0, 255).astype(np.uint8)
    canvas.paste(Image.fromarray(out_u8, mode="RGB"), (px, py))


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


def _draw_row0_brand_cells(canvas: Image.Image, draw: ImageDraw.ImageDraw, res: int) -> None:
    """Populate Row 0 (bottom row in PNG, y = 0.75*res .. res -> UV V = 0.00 .. 0.25) with era-correct maker marks:
    - Cell 0 (U = 0.00..0.25, KA_MARK / PICT_ROVER): Kessler-Arai Direction B mark on warm cream enamel
    - Cell 1 (U = 0.25..0.50, VIS_1967 / PICT_RADIO): 1967 VIS wordmark sun-bleached on 1960s Scout tan enamel
    - Cell 2 (U = 0.50..0.75, TYCHO_PANEL / CAUTION_RAD): Scorched Tycho Direction B mark + 'TYCHO · KESTREL-3'
    - Cell 3 (U = 0.75..1.00, TYCHO_MARK / LUMEN_STN): Large scorched Tycho Direction B mark on Kestrel hull skin
    """
    y0 = int(res * 0.75)
    y1 = res
    cell_w = res // 4

    # 1. KA_MARK / PICT_ROVER (x = 0 .. 256, y = 768 .. 1024):
    # Worn 1978-1986 Kessler-Arai chassis mark on warm cream enamel
    draw.rectangle([0, y0, cell_w - 1, y1 - 1], fill=CREAM_BG)
    draw.rounded_rectangle([8, y0 + 8, cell_w - 9, y1 - 9], radius=12, fill=CREAM_TILE, outline=(54, 58, 70), width=4)
    _stamp_worn_brand_mark(
        canvas,
        "kessler_arai_mark.png",
        (14, y0 + 12, cell_w - 14, y1 - 12),
        bg_color=CREAM_TILE,
        sun_fade=0.18,
        wordmark_split_y=0.58,
        seed=1982,
    )

    # 2. VIS_1967 / PICT_RADIO (x = 256 .. 512, y = 768 .. 1024):
    # Sun-bleached 1967 VIS pentagon shield & wordmark on Scout tan/ochre thermal enamel
    x1_0, x1_1 = cell_w, 2 * cell_w
    draw.rectangle([x1_0, y0, x1_1 - 1, y1 - 1], fill=SCOUT_TAN_BG)
    draw.rounded_rectangle(
        [x1_0 + 8, y0 + 8, x1_1 - 9, y1 - 9],
        radius=8,
        fill=SCOUT_TAN_BG,
        outline=(86, 74, 58),
        width=4,
    )
    _stamp_worn_brand_mark(
        canvas,
        "vis_1967_mark.png",
        (x1_0 + 14, y0 + 12, x1_1 - 14, y1 - 12),
        bg_color=SCOUT_TAN_BG,
        sun_fade=0.28,
        wordmark_split_y=0.56,
        seed=1967,
    )

    # 3. TYCHO_PANEL / CAUTION_RAD (x = 512 .. 768, y = 768 .. 1024):
    # Scorched 1996 Tycho Colony Corp hexagon mark + 'TYCHO · KESTREL-3' stencil
    x2_0, x2_1 = 2 * cell_w, 3 * cell_w
    draw.rectangle([x2_0, y0, x2_1 - 1, y1 - 1], fill=TYCHO_HULL_BG)
    draw.rounded_rectangle(
        [x2_0 + 8, y0 + 8, x2_1 - 9, y1 - 9],
        radius=10,
        fill=TYCHO_HULL_BG,
        outline=(44, 52, 58),
        width=4,
    )
    _stamp_worn_brand_mark(
        canvas,
        "tycho_mark.png",
        (x2_0 + 14, y0 + 12, x2_1 - 14, y1 - 42),
        bg_color=TYCHO_HULL_BG,
        sun_fade=0.16,
        scorch_strength=0.28,
        wordmark_split_y=0.60,
        seed=1996,
    )
    _draw_stencil_text(
        draw,
        "TYCHO · KESTREL-3",
        cx=x2_0 + cell_w * 0.5,
        cy=y1 - 24.0,
        char_w=9.5,
        char_h=18.0,
        stroke_w=3.2,
        spacing=2.8,
        color=(36, 48, 54),
        bg_color=TYCHO_HULL_BG,
        stencil_bridges=True,
    )

    # 4. TYCHO_MARK / LUMEN_STN (x = 768 .. 1024, y = 768 .. 1024):
    # Large scorched Tycho Colony Corp hexagon mark on Kestrel-3 hull panel
    x3_0, x3_1 = 3 * cell_w, 4 * cell_w
    draw.rectangle([x3_0, y0, x3_1 - 1, y1 - 1], fill=TYCHO_HULL_BG)
    _stamp_worn_brand_mark(
        canvas,
        "tycho_mark.png",
        (x3_0 + 12, y0 + 12, x3_1 - 12, y1 - 12),
        bg_color=TYCHO_HULL_BG,
        sun_fade=0.14,
        scorch_strength=0.32,
        wordmark_split_y=0.60,
        seed=1997,
    )


def generate_signage_maps(res: int = 1024, seed: int = 1988) -> dict[str, np.ndarray]:
    """Generate 1024x1024 Albedo, Normal, Mask, and Emission maps for R1_Signage."""
    hi_res = 1024  # Canonical 1024x1024 layout canvas
    canvas = Image.new("RGB", (hi_res, hi_res), color=CREAM_BG)
    draw = ImageDraw.Draw(canvas)

    # -------------------------------------------------------------------------
    # Row 3 (Top row in PNG, y = 0 .. 256 -> UV V = 0.75 .. 1.00):
    #   LOGO / ILC_SEAL (0..256), LUMEN / VIS_STENCIL (256..768), SEC_04 (768..1024)
    # -------------------------------------------------------------------------
    # 1. LOGO / ILC_SEAL (x = 0..256, y = 0..256): Worn, sun-faded ILC round seal
    _stamp_worn_brand_mark(
        canvas,
        "ilc_mark.png",
        (12, 10, 244, 246),
        bg_color=CREAM_BG,
        sun_fade=0.22,
        wordmark_split_y=0.62,
        seed=1979,
    )

    # 2. LUMEN / VIS_STENCIL (x = 256..768, y = 0..256):
    # Worn 1985 VIS mark (left) + picked '[ V I S ]' hull stencil (right)
    _stamp_worn_brand_mark(
        canvas,
        "vis_mark.png",
        (264, 14, 488, 242),
        bg_color=CREAM_BG,
        sun_fade=0.18,
        wordmark_split_y=0.62,
        seed=1985,
    )
    _stamp_vis_hull_stencil(
        canvas,
        (494, 14, 758, 242),
        bg_color=CREAM_BG,
        sun_fade=0.14,
    )

    # 3. SEC_04 / EVAC_1988 (x = 768..1024, y = 0..256):
    # Worn 1985 VIS crate plaque with orange header band, VIS mark + VIS stencil, and 'SEC-04'
    draw.rounded_rectangle([776, 14, 1014, 242], radius=12, outline=STENCIL_DARK, width=5)
    draw.rectangle([784, 22, 1006, 50], fill=ORANGE_BAND)
    _draw_stencil_text(
        draw,
        "VIS · SEC-04",
        cx=895.0,
        cy=36.0,
        char_w=11.0,
        char_h=18.0,
        stroke_w=3.4,
        spacing=3.5,
        color=CREAM_TILE,
        bg_color=ORANGE_BAND,
        stencil_bridges=True,
    )
    _stamp_vis_hull_stencil(
        canvas,
        (784, 54, 1006, 194),
        bg_color=CREAM_BG,
        sun_fade=0.14,
    )
    _draw_stencil_text(
        draw,
        "SEC-04",
        cx=895.0,
        cy=216.0,
        char_w=22.0,
        char_h=30.0,
        stroke_w=6.0,
        spacing=5.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # -------------------------------------------------------------------------
    # Row 2 (y = 256 .. 512 -> UV V = 0.50 .. 0.75):
    #   SEC04_HABITAT / VIS_1985 (0..512), AIRLOCK_STATUS (512..1024)
    # -------------------------------------------------------------------------
    # Large 1985 VIS mark (left) + '[ V I S ]' hull stencil (upper right) + 'SEC-04 · HABITAT' (lower right)
    _stamp_worn_brand_mark(
        canvas,
        "vis_mark.png",
        (12, 264, 244, 504),
        bg_color=CREAM_BG,
        sun_fade=0.18,
        wordmark_split_y=0.62,
        seed=198501,
    )
    _stamp_vis_hull_stencil(
        canvas,
        (248, 264, 500, 424),
        bg_color=CREAM_BG,
        sun_fade=0.14,
    )
    _draw_thick_seg(draw, 256.0, 432.0, 492.0, 432.0, 4.5, STENCIL_DARK)
    _draw_stencil_text(
        draw,
        "SEC-04 · HABITAT",
        cx=374.0,
        cy=468.0,
        char_w=11.5,
        char_h=32.0,
        stroke_w=5.2,
        spacing=3.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # AIRLOCK_STATUS cell (x = 512..1024, y = 256..512 -> UV U = 0.50..1.00, V = 0.50..0.75):
    # Dark status display backing with warm amber stencil/segment text:
    #   Primary line:   HABITAT SEAL · LOCKED
    #   Secondary line: SUIT 01-04 · OFFLINE (dimmer)
    emissive_canvas = Image.new("RGB", (hi_res, hi_res), color=(0, 0, 0))
    em_draw = ImageDraw.Draw(emissive_canvas)

    draw.rectangle([512, 256, 1023, 511], fill=STATUS_BEZEL_BG)
    draw.rounded_rectangle(
        [524, 270, 1012, 498],
        radius=14,
        fill=STATUS_SCREEN_BG,
        outline=STATUS_AMBER_DIM,
        width=5,
    )
    em_draw.rounded_rectangle(
        [524, 270, 1012, 498],
        radius=14,
        fill=(14, 8, 2),
        outline=(92, 56, 18),
        width=5,
    )
    for sy in range(278, 492, 8):
        draw.line([(530, sy), (1006, sy)], fill=STATUS_SCANLINE, width=2)

    # Primary status line: HABITAT SEAL · LOCKED
    _draw_stencil_text(
        draw,
        "HABITAT SEAL · LOCKED",
        cx=768.0,
        cy=342.0,
        char_w=15.0,
        char_h=46.0,
        stroke_w=6.0,
        spacing=4.2,
        color=STATUS_AMBER_BRIGHT,
        bg_color=STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "HABITAT SEAL · LOCKED",
        cx=768.0,
        cy=342.0,
        char_w=15.0,
        char_h=46.0,
        stroke_w=2.6,
        spacing=4.2,
        color=STATUS_AMBER_CORE,
        bg_color=STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        em_draw,
        "HABITAT SEAL · LOCKED",
        cx=768.0,
        cy=342.0,
        char_w=15.0,
        char_h=46.0,
        stroke_w=6.2,
        spacing=4.2,
        color=(236, 168, 62),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )
    _draw_stencil_text(
        em_draw,
        "HABITAT SEAL · LOCKED",
        cx=768.0,
        cy=342.0,
        char_w=15.0,
        char_h=46.0,
        stroke_w=2.8,
        spacing=4.2,
        color=(255, 236, 172),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )

    # Horizontal status divider rule
    _draw_thick_seg(draw, 548.0, 394.0, 988.0, 394.0, 3.0, STATUS_AMBER_RULE)
    _draw_thick_seg(em_draw, 548.0, 394.0, 988.0, 394.0, 3.0, (78, 48, 16))

    # Secondary dimmer status line: SUIT 01-04 · OFFLINE
    _draw_stencil_text(
        draw,
        "SUIT 01-04 · OFFLINE",
        cx=768.0,
        cy=440.0,
        char_w=14.0,
        char_h=34.0,
        stroke_w=4.6,
        spacing=4.2,
        color=STATUS_AMBER_DIM,
        bg_color=STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        em_draw,
        "SUIT 01-04 · OFFLINE",
        cx=768.0,
        cy=440.0,
        char_w=14.0,
        char_h=34.0,
        stroke_w=4.6,
        spacing=4.2,
        color=(142, 86, 26),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )

    # -------------------------------------------------------------------------
    # Row 1 (y = 512 .. 768 -> UV V = 0.25 .. 0.50):
    #   WORKSHOP (0..512), HAZARD (512..1024)
    # -------------------------------------------------------------------------
    draw.rounded_rectangle([18, 530, 494, 750], radius=14, outline=STENCIL_DARK, width=6)
    draw.rectangle([28, 540, 484, 570], fill=SAGE_ACCENT)
    _stamp_worn_brand_mark(
        canvas,
        "vis_mark.png",
        (28, 576, 184, 742),
        bg_color=CREAM_BG,
        sun_fade=0.18,
        wordmark_split_y=0.62,
        seed=198503,
    )
    _draw_stencil_text(
        draw,
        "WORKSHOP",
        cx=336.0,
        cy=632.0,
        char_w=30.0,
        char_h=60.0,
        stroke_w=10.0,
        spacing=7.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )
    _draw_stencil_text(
        draw,
        "VIS · SEC-04 · LUM-07",
        cx=336.0,
        cy=712.0,
        char_w=11.5,
        char_h=24.0,
        stroke_w=4.2,
        spacing=3.5,
        color=STENCIL_DARK,
        bg_color=CREAM_BG,
        stencil_bridges=True,
    )

    # Hazard stripes cell (x = 512..1024, y = 512..768)
    _draw_hazard_cell(canvas, 512, 512, 1024, 768)

    # -------------------------------------------------------------------------
    # Row 0 (Bottom row in PNG, y = 768 .. 1024 -> UV V = 0.00 .. 0.25):
    #   KA_MARK, VIS_1967, TYCHO_PANEL, TYCHO_MARK
    # -------------------------------------------------------------------------
    _draw_row0_brand_cells(canvas, draw, hi_res)

    if res != hi_res:
        img_out = canvas.resize((res, res), resample=Image.Resampling.LANCZOS)
        em_out = emissive_canvas.resize((res, res), resample=Image.Resampling.LANCZOS)
    else:
        img_out = canvas
        em_out = emissive_canvas

    # Flip vertically so row 0 of the returned numpy array corresponds to V=0 (bottom),
    # matching generate_r1_textures._save_rgba_or_rgb which calls np.flipud() on save.
    albedo_topdown = np.asarray(img_out, dtype=np.float32) / 255.0
    albedo_v0 = np.flipud(albedo_topdown).copy()
    emission_topdown = np.asarray(em_out, dtype=np.float32) / 255.0
    emission_v0 = np.clip(np.flipud(emission_topdown).copy(), 0.0, 1.0)
    # Zero out any filter ringing outside the AIRLOCK_STATUS cell (U = 0.50..1.00, V = 0.50..0.75)
    u_mid = res // 2
    v_lo = res // 2
    v_hi = (res * 3) // 4
    emission_clean = np.zeros_like(emission_v0)
    emission_clean[v_lo:v_hi, u_mid:res] = emission_v0[v_lo:v_hi, u_mid:res]

    # Add subtle deterministic baked-enamel micro-grain & stencil paint wear
    rng = np.random.default_rng(seed)
    grain = rng.normal(0.0, 0.008, size=(res, res)).astype(np.float32)
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

    # Mask map: R=Metallic (0.04), G=MicroWear, B=MicroCavity, A=Smoothness (0.64 on cream, 0.46 on matte ink, 0.84 on status glass)
    metallic = np.full((res, res), 0.04, dtype=np.float32)
    micro_wear = np.clip(0.50 + grain * 14.0, 0.0, 1.0)
    micro_cavity = np.clip(0.50 - grain * 12.0, 0.0, 1.0)
    smoothness = np.clip(0.64 - 0.18 * ink_mask + grain * 2.0, 0.20, 0.85)
    smoothness[v_lo:v_hi, u_mid:res] = 0.84
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
        "emission": emission_clean,
    }


def _save_png(path: Path, arr: np.ndarray) -> None:
    u8 = np.clip(np.rint(arr * 255.0), 0, 255).astype(np.uint8)
    mode = "RGBA" if u8.shape[-1] == 4 else "RGB"
    Image.fromarray(np.flipud(u8), mode=mode).save(path, compress_level=9)


def main() -> None:
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures"
    out_dir.mkdir(parents=True, exist_ok=True)
    maps = generate_signage_maps(1024)
    _save_png(out_dir / "T_R1_Signage_Albedo.png", maps["albedo"])
    _save_png(out_dir / "T_R1_Signage_Normal.png", maps["normal"])
    _save_png(out_dir / "T_R1_Signage_Mask.png", maps["mask"])
    _save_png(out_dir / "T_R1_Signage_Emission.png", maps["emission"])
    print(f"Saved T_R1_Signage_Albedo/Normal/Mask/Emission.png to {out_dir}")


if __name__ == "__main__":
    main()


