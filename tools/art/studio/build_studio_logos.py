#!/usr/bin/env python3
"""
Deterministic 4x-supersampled geometric vector generator for the "Outsight" ("OUTSIGHT" / "Outsight Games")
studio identity concepts and 5-second calm navy boot splash storyboards.

Studio brief:
  - "Outsight" (shown as "OUTSIGHT" or "Outsight Games"): the owner's studio for ALL future titles.
  - Meaning: the opposite of insight, looking outward beyond what humans know; games about worlds and
    perspectives nobody has made, places people have not explored or understood yet.
  - NOT moon-specific, NOT sci-fi-only. Timeless, calm, curious, a little mysterious.
  - Owner rule (strict): ZERO clusters of identical dots/holes (trypophobia).
"""

import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[2]
FONT_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Fonts"
OUT_ROOT = REPO_ROOT / "Logs" / "studio"

# 4x supersampling (4096x4096 -> 1024x1024 Lanczos) for razor-sharp anti-aliased vector geometry
OUT_SIZE = 1024
SS = 4
W = OUT_SIZE * SS
H = OUT_SIZE * SS

# Studio Palettes (Timeless, calm, literary/modernist — not sci-fi)
BG_BLACK = (11, 12, 16, 255)          # #0B0C10 — Deep gallery obsidian
BG_OFFWHITE = (244, 239, 230, 255)    # #F4EFE6 — Warm archival cotton paper
BG_BOOT_NAVY = (11, 14, 42, 255)      # #0B0E2A — Calm deep navy boot screen (matches ProjectSettings)

INK_ON_BLACK = (244, 239, 230, 255)   # #F4EFE6 — Warm ivory primary on black
SUB_ON_BLACK = (158, 156, 150, 255)   # #9E9C96 — Quiet warm stone for secondary sub-label
ACCENT_ON_BLACK = (226, 168, 86, 255) # #E2A856 — Calm ochre-amber focal warmth

INK_ON_WHITE = (18, 20, 28, 255)      # #12141C — Deep charcoal ink on off-white
SUB_ON_WHITE = (104, 102, 98, 255)    # #686662 — Muted warm slate for secondary sub-label
ACCENT_ON_WHITE = (196, 122, 44, 255) # #C47A2C — Warm sienna-ochre focal accent on paper


def s(val: float) -> int:
    return int(round(val * SS))


def sf(val: float) -> float:
    return float(val * SS)


def load_font(name: str, size_1024: float, ss: int = SS) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    return ImageFont.truetype(str(path), int(round(size_1024 * ss)))


def draw_tracked_text(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple,
    tracking: float = 0.0,
    anchor_y: str = "center",
    ss: int = SS,
):
    """Draws horizontally centered text with explicit inter-character tracking (in 1024-space px)."""
    track_px = float(tracking * ss)
    advances = [font.getlength(ch) for ch in text]
    total_w = sum(advances) + track_px * max(0, len(text) - 1)
    bbox = font.getbbox(text)
    text_h = bbox[3] - bbox[1]
    x = float(cx * ss) - total_w / 2.0
    if anchor_y == "center":
        y = float(cy * ss) - bbox[1] - text_h / 2.0
    elif anchor_y == "top":
        y = float(cy * ss) - bbox[1]
    else:
        y = float(cy * ss) - bbox[3]

    for ch, adv in zip(text, advances):
        draw.text((x, y), ch, font=font, fill=fill)
        x += adv + track_px


def rot_pt(x: float, y: float, cx: float, cy: float, deg: float) -> tuple[float, float]:
    rad = math.radians(deg)
    ca, sa = math.cos(rad), math.sin(rad)
    dx, dy = x - cx, y - cy
    return (cx + dx * ca - dy * sa, cy + dx * sa + dy * ca)


def draw_thick_arc_poly(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    r: float,
    deg_start: float,
    deg_end: float,
    thickness: float,
    fill: tuple,
    round_caps: bool = False,
    chamfer_deg: float = 0.0,
    ss: int = SS,
):
    """Draws a precision anti-aliased circular arc ribbon in supersampled coordinates."""
    steps = max(96, int(abs(deg_end - deg_start) * 1.5))
    r_outer = (r + thickness / 2.0) * ss
    r_inner = (r - thickness / 2.0) * ss
    cxs, cys = cx * ss, cy * ss

    outer_pts = []
    inner_pts = []
    for i in range(steps + 1):
        t = i / steps
        ang_o = math.radians(deg_start + (deg_end - deg_start) * t)
        # Optional slight angular taper/chamfer on inner edge at the gap ends
        ang_i = math.radians(
            (deg_start + chamfer_deg) + ((deg_end - chamfer_deg) - (deg_start + chamfer_deg)) * t
        )
        outer_pts.append((cxs + r_outer * math.cos(ang_o), cys + r_outer * math.sin(ang_o)))
        inner_pts.append((cxs + r_inner * math.cos(ang_i), cys + r_inner * math.sin(ang_i)))

    poly = outer_pts + list(reversed(inner_pts))
    draw.polygon(poly, fill=fill)

    if round_caps:
        rc = (thickness / 2.0) * ss
        for deg in (deg_start, deg_end):
            rad = math.radians(deg)
            px = cxs + r * ss * math.cos(rad)
            py = cys + r * ss * math.sin(rad)
            draw.ellipse((px - rc, py - rc, px + rc, py + rc), fill=fill)


def vesica_points(
    cx: float,
    cy: float,
    half_len: float,
    half_width: float,
    tilt_deg: float,
    ss: int = SS,
    steps: int = 96,
) -> list[tuple[float, float]]:
    """
    Constructs a true geometric vesica piscis (almond lens) from two circular arcs meeting at
    (-half_len, 0) and (+half_len, 0) with maximum half-height `half_width` at the center,
    then rotates by `tilt_deg` around (cx, cy).
    """
    L = half_len * ss
    H_v = half_width * ss
    # Circle radius R such that R - d = H_v and R^2 = L^2 + d^2  =>  R = (L^2 + H_v^2) / (2 * H_v)
    R = (L * L + H_v * H_v) / (2.0 * H_v)
    d = R - H_v
    ang_max = math.atan2(L, d)

    pts_local = []
    # Top arc: center at (0, +d), angle from -ang_max to +ang_max around -Y
    for i in range(steps + 1):
        t = -ang_max + (2.0 * ang_max) * (i / steps)
        x = R * math.sin(t)
        y = d - R * math.cos(t)
        pts_local.append((x, y))
    # Bottom arc: center at (0, -d), angle from +ang_max to -ang_max around +Y
    for i in range(1, steps):
        t = ang_max - (2.0 * ang_max) * (i / steps)
        x = R * math.sin(t)
        y = -d + R * math.cos(t)
        pts_local.append((x, y))

    rad = math.radians(tilt_deg)
    ca, sa = math.cos(rad), math.sin(rad)
    cxs, cys = cx * ss, cy * ss
    return [(cxs + x * ca - y * sa, cys + x * sa + y * ca) for (x, y) in pts_local]


def draw_wordmark_outsight(
    draw: ImageDraw.ImageDraw,
    cx: float,
    y_main: float,
    ink: tuple,
    sub_ink: tuple,
    accent: tuple | None = None,
    show_games: bool = True,
    style: str = "geometric",
    ss: int = SS,
):
    """
    Draws the studio wordmark ('OUTSIGHT' + optional 'GAMES' sub-label) with timeless geometric proportions.
    """
    if style == "mixed_case":
        font_main = load_font("BeVietnamPro-SemiBold.ttf", 52, ss=ss)
        draw_tracked_text(draw, cx, y_main, "Outsight Games", font_main, ink, tracking=3.5, ss=ss)
        return

    font_main = load_font("BeVietnamPro-SemiBold.ttf", 56, ss=ss)
    draw_tracked_text(draw, cx, y_main, "OUTSIGHT", font_main, ink, tracking=17.5, ss=ss)

    if show_games:
        font_sub = load_font("BeVietnamPro-Medium.ttf", 18, ss=ss)
        draw_tracked_text(draw, cx, y_main + 54, "G A M E S", font_sub, sub_ink, tracking=14.0, ss=ss)


# ==============================================================================
# DIRECTION A — "THE OUTER APERTURE" (An Eye/Lens Looking Out Through a Ring)
# ==============================================================================

def draw_mark_a(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    ink: tuple,
    accent: tuple,
    bg_cut: tuple | None = None,
    ring_gap_scale: float = 1.0,
    lens_reach: float = 1.0,
    iris_alpha: float = 1.0,
    ring_alpha: float = 1.0,
    ss: int = SS,
):
    """
    Draws Direction A:
      - A bold circular ring ('O' / boundary of the known world) centered slightly lower-left of the mark center,
        with a clean opening on its upper-right arc (-28 deg).
      - Nested inside and reaching *outward* through the ring's opening is a calm vesica piscis (almond eye/lens)
        tilted along the -28 deg outward axis.
      - Inside the lens, poised toward the outer opening, sits a solid circular iris ring and warm ochre-amber
        outward aperture disc (zero clusters of dots/holes).
    """
    gaze_deg = -28.0
    gaze_rad = math.radians(gaze_deg)
    ux, uy = math.cos(gaze_rad), math.sin(gaze_rad)

    # Ring center shifted slightly opposite the gaze so the overallsilhouette is optically centered at (cx, cy)
    rcx = cx - 14.0 * scale * ux
    rcy = cy - 14.0 * scale * uy
    r_ring = 176.0 * scale
    t_ring = 26.0 * scale

    # 1. Outer Ring with an opening centered at gaze_deg (-28 deg)
    half_gap = 42.0 * ring_gap_scale
    deg_start = gaze_deg + half_gap
    deg_end = gaze_deg + 360.0 - half_gap

    ink_ring = (ink[0], ink[1], ink[2], int(round(ink[3] * ring_alpha)))
    if ring_alpha > 0.01:
        draw_thick_arc_poly(
            draw,
            rcx,
            rcy,
            r_ring,
            deg_start=deg_start,
            deg_end=deg_end,
            thickness=t_ring,
            fill=ink_ring,
            round_caps=False,
            chamfer_deg=4.0,
            ss=ss,
        )

    # 2. Vesica Lens reaching outward through the ring gap
    if lens_reach > 0.01:
        ink_lens = (ink[0], ink[1], ink[2], int(round(ink[3] * min(1.0, lens_reach * 1.15))))
        # Center of the lens is shifted outward along the gaze vector so its outer apex projects beyond the ring
        shift_dist = (18.0 + 36.0 * lens_reach) * scale
        lcx = rcx + shift_dist * ux
        lcy = rcy + shift_dist * uy

        outer_len = 152.0 * scale * (0.45 + 0.55 * lens_reach)
        outer_wid = 76.0 * scale * (0.45 + 0.55 * lens_reach)
        t_lens = 22.0 * scale

        # If bg_cut is provided, carve a clean breathing collar around the lens where it passes the ring
        if bg_cut is not None and bg_cut[3] > 0:
            collar_pts = vesica_points(lcx, lcy, outer_len + 12.0 * scale, outer_wid + 12.0 * scale, gaze_deg, ss=ss)
            draw.polygon(collar_pts, fill=bg_cut)

        # Outer vesica contour drawn via clean even-odd mask so semi-transparent alpha is 100% seamless
        pts_out = vesica_points(lcx, lcy, outer_len, outer_wid, gaze_deg, ss=ss)
        pts_in = vesica_points(
            lcx,
            lcy,
            max(10.0 * scale, outer_len - t_lens * 1.55),
            max(6.0 * scale, outer_wid - t_lens),
            gaze_deg,
            ss=ss,
        )
        # Split at exact horizontal indices 0 and steps (where steps = len(pts_out) // 2)
        half_idx = len(pts_out) // 2
        poly_top = pts_out[: half_idx + 1] + list(reversed(pts_in[: half_idx + 1]))
        poly_bot = pts_out[half_idx:] + [pts_out[0], pts_in[0]] + list(reversed(pts_in[half_idx:]))
        draw.polygon(poly_top, fill=ink_lens)
        draw.polygon(poly_bot, fill=ink_lens)

    # 3. Outward-gazing Iris & Warm Focal Aperture Disc inside the lens
    if iris_alpha > 0.01:
        icx = rcx + (54.0 + 20.0 * lens_reach) * scale * ux
        icy = rcy + (54.0 + 20.0 * lens_reach) * scale * uy
        r_iris_out = 42.0 * scale
        r_pupil = 20.0 * scale

        ink_iris = (ink[0], ink[1], ink[2], int(round(ink[3] * iris_alpha)))
        acc_iris = (accent[0], accent[1], accent[2], int(round(accent[3] * iris_alpha)))

        # Outer iris ring
        draw_thick_arc_poly(
            draw,
            icx,
            icy,
            r_iris_out - 6.5 * scale,
            0.0,
            360.0,
            13.0 * scale,
            fill=ink_iris,
            ss=ss,
        )

        # Warm solid focal disc shifted slightly toward the outward tip of the lens (looking out!)
        pcx = icx + 8.0 * scale * ux
        pcy = icy + 8.0 * scale * uy
        pr = r_pupil * ss
        draw.ellipse((pcx * ss - pr, pcy * ss - pr, pcx * ss + pr, pcy * ss + pr), fill=acc_iris)


def render_direction_lockups(
    dir_code: str,
    draw_mark_fn,
    wordmark_style: str = "geometric",
    show_games: bool = True,
):
    out_dir = OUT_ROOT / dir_code
    out_dir.mkdir(parents=True, exist_ok=True)

    cx, cy_mark, y_word = 512.0, 415.0, 760.0

    # 1. On Black (1024x1024)
    img_blk = Image.new("RGBA", (W, H), BG_BLACK)
    d_blk = ImageDraw.Draw(img_blk)
    draw_mark_fn(d_blk, cx, cy_mark, 1.0, INK_ON_BLACK, ACCENT_ON_BLACK, bg_cut=BG_BLACK)
    draw_wordmark_outsight(
        d_blk, cx, y_word, INK_ON_BLACK, SUB_ON_BLACK, ACCENT_ON_BLACK, show_games=show_games, style=wordmark_style
    )
    img_blk_1024 = img_blk.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)
    img_blk_1024.save(out_dir / "on_black.png")

    # 2. On Off-White (1024x1024)
    img_wht = Image.new("RGBA", (W, H), BG_OFFWHITE)
    d_wht = ImageDraw.Draw(img_wht)
    draw_mark_fn(d_wht, cx, cy_mark, 1.0, INK_ON_WHITE, ACCENT_ON_WHITE, bg_cut=BG_OFFWHITE)
    draw_wordmark_outsight(
        d_wht, cx, y_word, INK_ON_WHITE, SUB_ON_WHITE, ACCENT_ON_WHITE, show_games=show_games, style=wordmark_style
    )
    img_wht_1024 = img_wht.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)
    img_wht_1024.save(out_dir / "on_offwhite.png")

    # 3. Transparent Lockup (1024x1024) & Favicon 64x64 check
    img_tr = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d_tr = ImageDraw.Draw(img_tr)
    draw_mark_fn(d_tr, cx, cy_mark, 1.0, INK_ON_BLACK, ACCENT_ON_BLACK, bg_cut=None)
    draw_wordmark_outsight(
        d_tr, cx, y_word, INK_ON_BLACK, SUB_ON_BLACK, ACCENT_ON_BLACK, show_games=show_games, style=wordmark_style
    )
    img_tr.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS).save(out_dir / "transparent.png")

    # Favicon mark-only at 64x64 (rendered from 512x512 supersampled canvas)
    fav_img = Image.new("RGBA", (W, H), BG_BLACK)
    d_fav = ImageDraw.Draw(fav_img)
    draw_mark_fn(d_fav, 512.0, 512.0, 1.85, INK_ON_BLACK, ACCENT_ON_BLACK, bg_cut=BG_BLACK)
    fav_img.resize((64, 64), Image.Resampling.LANCZOS).save(out_dir / "favicon_64.png")

    print(f"[build_studio_logos] Rendered Direction {dir_code.upper()} -> {out_dir}")


def make_calm_navy_boot_canvas(w_px: int, h_px: int) -> Image.Image:
    """Creates the calm deep-navy (#0B0E2A) boot screen canvas with a very gentle vignette."""
    overlay = Image.new("RGBA", (w_px, h_px), BG_BOOT_NAVY)
    d = ImageDraw.Draw(overlay)
    cx, cy = w_px / 2.0, h_px / 2.0
    max_r = math.hypot(cx, cy)
    for step in range(24, 0, -1):
        t = step / 24.0
        r = max_r * t
        r_col = int(round(8 + 8 * (1.0 - t)))
        g_col = int(round(11 + 10 * (1.0 - t)))
        b_col = int(round(32 + 24 * (1.0 - t)))
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(r_col, g_col, b_col, 255))
    return overlay


def apply_layer_opacity(layer: Image.Image, opacity: float) -> Image.Image:
    """Multiplies the alpha channel of an RGBA layer by `opacity` (0..1) without polygon junction seams."""
    if opacity >= 0.999:
        return layer
    if opacity <= 0.001:
        return Image.new("RGBA", layer.size, (0, 0, 0, 0))
    r, g, b, a = layer.split()
    a = a.point(lambda p: int(round(p * opacity)))
    return Image.merge("RGBA", (r, g, b, a))


def render_storyboard_a():
    """
    Renders the 4-frame 5-second boot 'splash' storyboard for Direction A on the calm navy (#0B0E2A)
    boot screen before the game's title (subtle animated reveal, no flashy effects).
    Each frame is saved at 1024x1024 + a 2x2 storyboard sheet.
    """
    out_dir = OUT_ROOT / "a"
    out_dir.mkdir(parents=True, exist_ok=True)

    frames_spec = [
        {
            "idx": 1,
            "time": "0.0 s – 1.2 s",
            "title": "1 · QUIET EMERGENCE",
            "desc": "Calm navy (#0B0E2A) holds for 0.4 s; the outer ring eases in softly at 38% opacity.",
            "ring_alpha": 0.38,
            "ring_gap": 0.28,
            "lens_alpha": 0.0,
            "lens_reach": 0.0,
            "iris_alpha": 0.0,
            "word_alpha": 0.0,
            "word_dy": 10.0,
        },
        {
            "idx": 2,
            "time": "1.2 s – 2.5 s",
            "title": "2 · THE RING PARTS OUTWARD",
            "desc": "The upper-right arc smoothly opens as the vesica lens glides outward across the boundary.",
            "ring_alpha": 0.82,
            "ring_gap": 0.88,
            "lens_alpha": 0.78,
            "lens_reach": 0.78,
            "iris_alpha": 0.48,
            "word_alpha": 0.22,
            "word_dy": 6.0,
        },
        {
            "idx": 3,
            "time": "2.5 s – 4.0 s",
            "title": "3 · OUTWARD FOCUS & LOCKUP",
            "desc": "The warm ochre-amber iris settles at the outer apex; 'OUTSIGHT' fades in calmly below.",
            "ring_alpha": 1.0,
            "ring_gap": 1.0,
            "lens_alpha": 1.0,
            "lens_reach": 1.0,
            "iris_alpha": 1.0,
            "word_alpha": 1.0,
            "word_dy": 0.0,
        },
        {
            "idx": 4,
            "time": "4.0 s – 5.0 s",
            "title": "4 · STILL HOLD & DISSOLVE",
            "desc": "Ring and wordmark ease into the navy dark; the warm outward iris lingers last before Title.",
            "ring_alpha": 0.22,
            "ring_gap": 1.0,
            "lens_alpha": 0.28,
            "lens_reach": 1.0,
            "iris_alpha": 0.82,
            "word_alpha": 0.22,
            "word_dy": 0.0,
        },
    ]

    rendered_frames = []
    ss_boot = 3
    wb, hb = OUT_SIZE * ss_boot, OUT_SIZE * ss_boot

    for f in frames_spec:
        canvas = make_calm_navy_boot_canvas(wb, hb)
        cx, cy_mark, y_word = 512.0, 410.0, 742.0

        # 1. Outer Ring layer
        if f["ring_alpha"] > 0.01:
            l_ring = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
            draw_mark_a(
                ImageDraw.Draw(l_ring),
                cx,
                cy_mark,
                scale=0.88,
                ink=INK_ON_BLACK,
                accent=ACCENT_ON_BLACK,
                bg_cut=None,
                ring_gap_scale=f["ring_gap"],
                lens_reach=0.0,
                iris_alpha=0.0,
                ring_alpha=1.0,
                ss=ss_boot,
            )
            canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_ring, f["ring_alpha"]))

        # 2. Vesica Lens layer
        if f["lens_alpha"] > 0.01 and f["lens_reach"] > 0.01:
            l_lens = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
            draw_mark_a(
                ImageDraw.Draw(l_lens),
                cx,
                cy_mark,
                scale=0.88,
                ink=INK_ON_BLACK,
                accent=ACCENT_ON_BLACK,
                bg_cut=None,
                ring_gap_scale=f["ring_gap"],
                lens_reach=f["lens_reach"],
                iris_alpha=0.0,
                ring_alpha=0.0,
                ss=ss_boot,
            )
            canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_lens, f["lens_alpha"]))

        # 3. Iris & Warm Aperture Disc layer
        if f["iris_alpha"] > 0.01:
            l_iris = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
            gaze_rad = math.radians(-28.0)
            ux, uy = math.cos(gaze_rad), math.sin(gaze_rad)
            scale = 0.88
            rcx = cx - 14.0 * scale * ux
            rcy = cy_mark - 14.0 * scale * uy
            icx = rcx + (54.0 + 20.0 * f["lens_reach"]) * scale * ux
            icy = rcy + (54.0 + 20.0 * f["lens_reach"]) * scale * uy
            d_ir = ImageDraw.Draw(l_iris)
            draw_thick_arc_poly(d_ir, icx, icy, (42.0 - 6.5) * scale, 0.0, 360.0, 13.0 * scale, fill=INK_ON_BLACK, ss=ss_boot)
            pcx = icx + 8.0 * scale * ux
            pcy = icy + 8.0 * scale * uy
            pr = 20.0 * scale * ss_boot
            d_ir.ellipse((pcx * ss_boot - pr, pcy * ss_boot - pr, pcx * ss_boot + pr, pcy * ss_boot + pr), fill=ACCENT_ON_BLACK)
            canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_iris, f["iris_alpha"]))

        layer_ui = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer_ui)
        if f["word_alpha"] > 0.01:
            wa = f["word_alpha"]
            w_ink = (INK_ON_BLACK[0], INK_ON_BLACK[1], INK_ON_BLACK[2], int(round(255 * wa)))
            w_sub = (SUB_ON_BLACK[0], SUB_ON_BLACK[1], SUB_ON_BLACK[2], int(round(220 * wa)))
            draw_wordmark_outsight(
                d, cx, y_word + f["word_dy"], w_ink, w_sub, show_games=True, ss=ss_boot
            )

        font_cap = load_font("IBMPlexMono-Medium.ttf", 18, ss=ss_boot)
        font_sub = load_font("BeVietnamPro-Regular.ttf", 17, ss=ss_boot)
        d.line(
            [(48 * ss_boot, 912 * ss_boot), ((1024 - 48) * ss_boot, 912 * ss_boot)],
            fill=(244, 239, 230, 38),
            width=max(1, ss_boot),
        )
        draw_tracked_text(
            d, 512.0, 942.0, f"{f['title']}   ·   {f['time']}", font_cap, (226, 168, 86, 220), tracking=2.0, ss=ss_boot
        )
        draw_tracked_text(
            d, 512.0, 976.0, f["desc"], font_sub, (244, 239, 230, 175), tracking=0.3, ss=ss_boot
        )

        comp = Image.alpha_composite(canvas, layer_ui).resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)
        comp.save(out_dir / f"splash_{f['idx']}.png")
        rendered_frames.append(comp)

    # Also save a combined 2x2 1024x1024 storyboard sheet (`splash_storyboard.png`)
    sheet = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), BG_BOOT_NAVY)
    half = OUT_SIZE // 2
    positions = [(0, 0), (half, 0), (0, half), (half, half)]
    for img_f, (px, py) in zip(rendered_frames, positions):
        thumb = img_f.resize((half, half), Image.Resampling.LANCZOS)
        sheet.paste(thumb, (px, py))
    d_s = ImageDraw.Draw(sheet)
    d_s.line([(half, 0), (half, OUT_SIZE)], fill=(244, 239, 230, 45), width=2)
    d_s.line([(0, half), (OUT_SIZE, half)], fill=(244, 239, 230, 45), width=2)
    sheet.save(out_dir / "splash_storyboard.png")
    print("[build_studio_logos] Rendered Direction A 4-frame boot splash storyboard")


# ==============================================================================
# DIRECTION B — "THE OPEN HORIZON" (A Horizon Line with an Opening)
# ==============================================================================

def draw_mark_b(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    ink: tuple,
    accent: tuple,
    bg_cut: tuple | None = None,
    ss: int = SS,
):
    """
    Draws Direction B ('The Open Horizon'):
      - A wide, calm horizontal datum line (the horizon) that parts cleanly at the center.
      - Above the horizon rises a poised semicircular vault arch framing an unseen sky.
      - Through the central opening in the horizon, a warm ochre perspective wedge passes forward and downward
        from a vertical threshold portal, while a quiet lower counter-arc completes the 'O' silhouette.
      - Simple, architectural, zero dot/hole clusters, reads effortlessly at 16 px and 1024 px.
    """
    y_horiz = cy + 18.0 * scale
    r_arch = 166.0 * scale
    t_stroke = 25.0 * scale

    # 1. Upper Sky Vault Arch (from 190 deg to 350 deg — a calm 160-deg dome hovering above the horizon)
    draw_thick_arc_poly(
        draw,
        cx,
        y_horiz - 12.0 * scale,
        r_arch,
        deg_start=192.0,
        deg_end=348.0,
        thickness=t_stroke,
        fill=ink,
        round_caps=False,
        chamfer_deg=3.0,
        ss=ss,
    )

    # 2. Lower Counter-Arc (completing the implied 'O' below the parted horizon, broken at center for the path)
    draw_thick_arc_poly(
        draw,
        cx,
        y_horiz - 12.0 * scale,
        r_arch,
        deg_start=16.0,
        deg_end=64.0,
        thickness=t_stroke,
        fill=ink,
        round_caps=False,
        ss=ss,
    )
    draw_thick_arc_poly(
        draw,
        cx,
        y_horiz - 12.0 * scale,
        r_arch,
        deg_start=116.0,
        deg_end=164.0,
        thickness=t_stroke,
        fill=ink,
        round_caps=False,
        ss=ss,
    )

    # 4. The Threshold Opening & Outward Perspective Wedge passing through the horizon gap
    # The warm ochre-amber perspective beam starts with a semicircular crown inside the upper dome
    # and widens smoothly downward through the parted horizon, terminating along the outer circle arc.
    c_arch_y = y_horiz - 12.0 * scale
    r_outer_circle = r_arch + t_stroke / 2.0
    crown_y = c_arch_y - 72.0 * scale
    r_crown = 21.0 * scale
    bot_half_w = 48.0 * scale
    bot_y_edge = c_arch_y + math.sqrt(max(0.0, r_outer_circle**2 - bot_half_w**2))

    # Slope of the perspective beam edge from (r_crown, crown_y) to (bot_half_w, bot_y_edge)
    dx_beam = bot_half_w - r_crown
    dy_beam = bot_y_edge - crown_y
    beam_len = math.hypot(dx_beam, dy_beam)
    # Outward unit normal to the right beam edge
    nx, ny = dy_beam / beam_len, -dx_beam / beam_len

    # Re-draw the parted horizon wings so their inner bevels are 100% parallel to the beam (14 px gap)
    gap_channel = 14.0 * scale
    wing_outer = 218.0 * scale
    h_bar = 25.0 * scale
    y_top_bar = y_horiz - h_bar / 2.0
    y_bot_bar = y_horiz + h_bar / 2.0

    def beam_half_w_at(y_val: float) -> float:
        return r_crown + (y_val - crown_y) * (dx_beam / dy_beam)

    # Clear previous horizon wing area by drawing horizon wings after computing exact parallel bevels
    w_top_inner = beam_half_w_at(y_top_bar) + gap_channel / nx
    w_bot_inner = beam_half_w_at(y_bot_bar) + gap_channel / nx

    left_wing = [
        ((cx - wing_outer) * ss, y_top_bar * ss),
        ((cx - w_top_inner) * ss, y_top_bar * ss),
        ((cx - w_bot_inner) * ss, y_bot_bar * ss),
        ((cx - wing_outer) * ss, y_bot_bar * ss),
    ]
    right_wing = [
        ((cx + w_top_inner) * ss, y_top_bar * ss),
        ((cx + wing_outer) * ss, y_top_bar * ss),
        ((cx + wing_outer) * ss, y_bot_bar * ss),
        ((cx + w_bot_inner) * ss, y_bot_bar * ss),
    ]
    draw.polygon(left_wing, fill=ink)
    draw.polygon(right_wing, fill=ink)

    # Upper portal arch hood framing the crown of the light beam with a uniform 13 px negative-space channel
    hood_gap = 13.0 * scale
    hood_thick = 20.0 * scale
    r_hood_in = r_crown + hood_gap
    r_hood_out = r_hood_in + hood_thick
    jamb_bot_y = y_top_bar - 13.0 * scale

    # Semicircular upper arch of the portal hood
    draw_thick_arc_poly(
        draw,
        cx,
        crown_y,
        (r_hood_in + r_hood_out) / 2.0,
        deg_start=180.0,
        deg_end=360.0,
        thickness=hood_thick,
        fill=ink,
        ss=ss,
    )
    # Tapered left & right jambs running parallel to the light beam down to just above the horizon bar
    dy_jamb = jamb_bot_y - crown_y
    jx_shift = dy_jamb * (dx_beam / dy_beam)
    left_jamb = [
        ((cx - r_hood_out) * ss, crown_y * ss),
        ((cx - r_hood_in) * ss, crown_y * ss),
        ((cx - r_hood_in - jx_shift) * ss, jamb_bot_y * ss),
        ((cx - r_hood_out - jx_shift) * ss, jamb_bot_y * ss),
    ]
    right_jamb = [
        ((cx + r_hood_in) * ss, crown_y * ss),
        ((cx + r_hood_out) * ss, crown_y * ss),
        ((cx + r_hood_out + jx_shift) * ss, jamb_bot_y * ss),
        ((cx + r_hood_in + jx_shift) * ss, jamb_bot_y * ss),
    ]
    draw.polygon(left_jamb, fill=ink)
    draw.polygon(right_jamb, fill=ink)

    # Build the golden perspective light beam polygon (semicircular top crown + curved bottom arc)
    beam_pts = []
    # Top semicircular crown (from 180 deg to 360 deg)
    for i in range(49):
        ang = math.radians(180.0 + 180.0 * (i / 48.0))
        beam_pts.append(((cx + r_crown * math.cos(ang)) * ss, (crown_y + r_crown * math.sin(ang)) * ss))
    # Bottom arc along the outer circle radius (from right corner to left corner around 90 deg)
    ang_right = math.degrees(math.atan2(bot_y_edge - c_arch_y, bot_half_w))
    ang_left = math.degrees(math.atan2(bot_y_edge - c_arch_y, -bot_half_w))
    for i in range(33):
        ang = math.radians(ang_right + (ang_left - ang_right) * (i / 32.0))
        beam_pts.append(
            ((cx + r_outer_circle * math.cos(ang)) * ss, (c_arch_y + r_outer_circle * math.sin(ang)) * ss)
        )
    draw.polygon(beam_pts, fill=accent)


# ==============================================================================
# DIRECTION C — "THE LEAVING LINE" (A Single Line Leaving a Circle)
# ==============================================================================

def draw_mark_c(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    ink: tuple,
    accent: tuple,
    bg_cut: tuple | None = None,
    line_progress: float = 1.0,
    circle_alpha: float = 1.0,
    ss: int = SS,
):
    """
    Draws Direction C ('The Leaving Line'):
      - An iconic circular ring ('O', radius 170 px) with a clean opening on its upper-right rim (-36 deg).
      - From the upper-right lip of the opening, the ribbon sweeps counter-clockwise around the top and left,
        then smoothly peels inward with exact C^1 tangent continuity onto the center axis and shoots straight
        outward at -36 deg through the opening into the unexplored space beyond the circle.
      - The lower-right arc completes the circle silhouette, separated from the peeling inner trajectory by a
        crisp negative-space notch. The outward tip of the departing line carries a warm ochre-amber segment.
    """
    exit_deg = -36.0
    exit_rad = math.radians(exit_deg)
    ux, uy = math.cos(exit_rad), math.sin(exit_rad)
    vx, vy = -uy, ux  # +90 deg clockwise from exit vector (lower-right)

    # Shift circle center slightly lower-left along -u so the outward line tip balances optically
    ccx = cx - 18.0 * scale * ux
    ccy = cy - 18.0 * scale * uy

    R1 = 172.0 * scale
    thick = 26.0 * scale
    half_t = thick / 2.0

    def to_screen(u_val: float, v_val: float) -> tuple[float, float]:
        return ((ccx + u_val * ux + v_val * vx) * ss, (ccy + u_val * uy + v_val * vy) * ss)

    # 1. Lower-right cradle arc: from phi = +26 deg (bottom lip of upper-right opening) clockwise to phi = 190 deg,
    # leaving a crisp, intentional negative-space breathing channel before the upper ribbon peels inward.
    if circle_alpha > 0.01:
        c_ink = (ink[0], ink[1], ink[2], int(round(ink[3] * circle_alpha)))
        draw_thick_arc_poly(
            draw,
            ccx,
            ccy,
            R1,
            deg_start=exit_deg + 26.0,
            deg_end=exit_deg + 190.0,
            thickness=thick,
            fill=c_ink,
            round_caps=False,
            chamfer_deg=3.0,
            ss=ss,
        )

    # 2. ONE Continuous Unbroken Ribbon: starts at phi = 334 deg (top lip of opening), sweeps counter-clockwise
    # around the top-left (334 -> 222 deg), peels smoothly inward with exact C^1 tangent continuity (222 -> 90 deg),
    # and shoots straight outward along +u through the opening at phi = 0!
    if line_progress > 0.01:
        phi_start_outer = math.radians(334.0)
        phi_star = math.radians(222.0)
        sin_ps = math.sin(phi_star)
        cos_ps = math.cos(phi_star)
        r_turn = (-R1 * sin_ps) / (1.0 - sin_ps)
        uc = (R1 - r_turn) * cos_ps
        vc = (R1 - r_turn) * sin_ps

        spine_uv = []
        # Segment A: Outer circle arc from 334 deg down to 222 deg
        outer_steps = 72
        for i in range(outer_steps):
            t = i / outer_steps
            phi = phi_start_outer + (phi_star - phi_start_outer) * t
            spine_uv.append((R1 * math.cos(phi), R1 * math.sin(phi)))

        # Segment B: C^1 inner tangent turn from 222 deg down to 90 deg (landing on v = 0)
        turn_steps = 64
        for i in range(turn_steps + 1):
            t = i / turn_steps
            phi = phi_star + (math.radians(90.0) - phi_star) * t
            u_pt = uc + r_turn * math.cos(phi)
            v_pt = vc + r_turn * math.sin(phi)
            spine_uv.append((u_pt, v_pt))

        # Segment C: Straight departing line from u_exit = uc (at v = 0) out to u_main_end (just outside the ring)
        u_exit = uc
        u_main_end = 184.0 * scale
        u_tip_full = 248.0 * scale
        straight_steps = 72
        for i in range(1, straight_steps + 1):
            t = i / straight_steps
            u_pt = u_exit + (u_main_end - u_exit) * t
            spine_uv.append((u_pt, 0.0))

        # Truncate spine by line_progress (0..1) for smooth storyboard animation
        total_pts = len(spine_uv)
        n_draw = max(2, int(round(total_pts * min(1.0, line_progress / 0.82))))
        active_spine = spine_uv[:n_draw]

        left_edge = []
        right_edge = []
        for idx, (u_p, v_p) in enumerate(active_spine):
            if idx == 0:
                du = active_spine[1][0] - u_p
                dv = active_spine[1][1] - v_p
            elif idx == len(active_spine) - 1:
                du = u_p - active_spine[idx - 1][0]
                dv = v_p - active_spine[idx - 1][1]
            else:
                du = active_spine[idx + 1][0] - active_spine[idx - 1][0]
                dv = active_spine[idx + 1][1] - active_spine[idx - 1][1]
            dlen = max(1e-6, math.hypot(du, dv))
            nu, nv = -dv / dlen, du / dlen
            left_edge.append(to_screen(u_p + nu * half_t, v_p + nv * half_t))
            right_edge.append(to_screen(u_p - nu * half_t, v_p - nv * half_t))

        draw.polygon(left_edge + list(reversed(right_edge)), fill=ink)

        # Segment D: Warm ochre-amber outward beacon segment separated by a crisp 8 px architectural gap
        if line_progress > 0.84:
            tip_t = min(1.0, (line_progress - 0.84) / 0.16)
            u_acc_start = (184.0 + 8.0) * scale
            u_acc_end = u_acc_start + (u_tip_full - u_acc_start) * tip_t
            if u_acc_end > u_acc_start + 2.0 * scale:
                acc_poly = [
                    to_screen(u_acc_start, -half_t),
                    to_screen(u_acc_end, -half_t),
                    to_screen(u_acc_end, +half_t),
                    to_screen(u_acc_start, +half_t),
                ]
                draw.polygon(acc_poly, fill=accent)


def render_storyboard_c():
    """
    Renders the 4-frame 5-second boot 'splash' storyboard for Direction C ('The Leaving Line')
    on the calm navy (#0B0E2A) boot screen before the game's title.
    """
    out_dir = OUT_ROOT / "c"
    out_dir.mkdir(parents=True, exist_ok=True)

    frames_spec = [
        {
            "idx": 1,
            "time": "0.0 s – 1.2 s",
            "title": "1 · THE KNOWN CIRCLE",
            "desc": "Calm navy (#0B0E2A) holds briefly; the circular arc begins tracing softly at 42% opacity.",
            "mark_alpha": 0.42,
            "circle_alpha": 1.0,
            "line_progress": 0.42,
            "tip_override_alpha": 0.0,
            "word_alpha": 0.0,
            "word_dy": 10.0,
        },
        {
            "idx": 2,
            "time": "1.2 s – 2.5 s",
            "title": "2 · TANGENTIAL INFLECTION",
            "desc": "Rather than closing the loop, the ribbon peels smoothly inward toward the upper-right opening.",
            "mark_alpha": 0.84,
            "circle_alpha": 1.0,
            "line_progress": 0.80,
            "tip_override_alpha": 0.0,
            "word_alpha": 0.22,
            "word_dy": 6.0,
        },
        {
            "idx": 3,
            "time": "2.5 s – 4.0 s",
            "title": "3 · DEPARTURE BEYOND THE RING",
            "desc": "The single line exits the open rim into uncharted space with a warm ochre tip; wordmark locks in.",
            "mark_alpha": 1.0,
            "circle_alpha": 1.0,
            "line_progress": 1.0,
            "tip_override_alpha": 0.0,
            "word_alpha": 1.0,
            "word_dy": 0.0,
        },
        {
            "idx": 4,
            "time": "4.0 s – 5.0 s",
            "title": "4 · STILL HOLD & DISSOLVE",
            "desc": "Circle and wordmark dissolve into the calm navy dark; the warm departing tip lingers last.",
            "mark_alpha": 0.22,
            "circle_alpha": 1.0,
            "line_progress": 0.82,
            "tip_override_alpha": 0.82,
            "word_alpha": 0.22,
            "word_dy": 0.0,
        },
    ]

    rendered_frames = []
    ss_boot = 3
    wb, hb = OUT_SIZE * ss_boot, OUT_SIZE * ss_boot

    for f in frames_spec:
        canvas = make_calm_navy_boot_canvas(wb, hb)
        cx, cy_mark, y_word = 512.0, 410.0, 742.0

        l_mark = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        draw_mark_c(
            ImageDraw.Draw(l_mark),
            cx,
            cy_mark,
            scale=0.88,
            ink=INK_ON_BLACK,
            accent=ACCENT_ON_BLACK,
            line_progress=f["line_progress"],
            circle_alpha=f["circle_alpha"],
            ss=ss_boot,
        )
        canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_mark, f["mark_alpha"]))

        if f["tip_override_alpha"] > 0.01:
            l_tip = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
            exit_rad = math.radians(-36.0)
            ux, uy = math.cos(exit_rad), math.sin(exit_rad)
            vx, vy = -uy, ux
            scale = 0.88
            ccx = cx - 18.0 * scale * ux
            ccy = cy_mark - 18.0 * scale * uy
            half_t = 13.0 * scale

            def to_scr(u_val: float, v_val: float) -> tuple[float, float]:
                return ((ccx + u_val * ux + v_val * vx) * ss_boot, (ccy + u_val * uy + v_val * vy) * ss_boot)

            u_acc_start = 192.0 * scale
            u_acc_end = 248.0 * scale
            ImageDraw.Draw(l_tip).polygon(
                [
                    to_scr(u_acc_start, -half_t),
                    to_scr(u_acc_end, -half_t),
                    to_scr(u_acc_end, +half_t),
                    to_scr(u_acc_start, +half_t),
                ],
                fill=ACCENT_ON_BLACK,
            )
            canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_tip, f["tip_override_alpha"]))

        layer_ui = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer_ui)
        if f["word_alpha"] > 0.01:
            wa = f["word_alpha"]
            w_ink = (INK_ON_BLACK[0], INK_ON_BLACK[1], INK_ON_BLACK[2], int(round(255 * wa)))
            w_sub = (SUB_ON_BLACK[0], SUB_ON_BLACK[1], SUB_ON_BLACK[2], int(round(220 * wa)))
            draw_wordmark_outsight(
                d, cx, y_word + f["word_dy"], w_ink, w_sub, show_games=True, ss=ss_boot
            )

        font_cap = load_font("IBMPlexMono-Medium.ttf", 18, ss=ss_boot)
        font_sub = load_font("BeVietnamPro-Regular.ttf", 17, ss=ss_boot)
        d.line(
            [(48 * ss_boot, 912 * ss_boot), ((1024 - 48) * ss_boot, 912 * ss_boot)],
            fill=(244, 239, 230, 38),
            width=max(1, ss_boot),
        )
        draw_tracked_text(
            d, 512.0, 942.0, f"{f['title']}   ·   {f['time']}", font_cap, (226, 168, 86, 220), tracking=2.0, ss=ss_boot
        )
        draw_tracked_text(
            d, 512.0, 976.0, f["desc"], font_sub, (244, 239, 230, 175), tracking=0.3, ss=ss_boot
        )

        comp = Image.alpha_composite(canvas, layer_ui).resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)
        comp.save(out_dir / f"splash_{f['idx']}.png")
        rendered_frames.append(comp)

    sheet = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), BG_BOOT_NAVY)
    half = OUT_SIZE // 2
    positions = [(0, 0), (half, 0), (0, half), (half, half)]
    for img_f, (px, py) in zip(rendered_frames, positions):
        thumb = img_f.resize((half, half), Image.Resampling.LANCZOS)
        sheet.paste(thumb, (px, py))
    d_s = ImageDraw.Draw(sheet)
    d_s.line([(half, 0), (half, OUT_SIZE)], fill=(244, 239, 230, 45), width=2)
    d_s.line([(0, half), (OUT_SIZE, half)], fill=(244, 239, 230, 45), width=2)
    sheet.save(out_dir / "splash_storyboard.png")
    print("[build_studio_logos] Rendered Direction C 4-frame boot splash storyboard")


# ==============================================================================
# DIRECTION D — "TWO PERSPECTIVES" (Negative-Space Shape Reading Two Ways)
# ==============================================================================

def draw_mark_d(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    ink: tuple,
    accent: tuple,
    bg_cut: tuple | None = None,
    ss: int = SS,
):
    """
    Draws Direction D ('Two Perspectives'):
      - A reversible figure-ground circular medallion ('O') that reads two ways simultaneously:
          1. PERSPECTIVE 1 — THE OUTWARD EYE: A bold circular medallion pierced by a negative-space almond eye,
             containing a circular iris and a warm ochre-amber pupil with a lower catchlight wedge.
          2. PERSPECTIVE 2 — THE CANYON PORTAL & HORIZON: Flip figure and ground, and the iris walls become
             two towering canyon cliffs framing a perspective pathway that leads outward toward a warm sun
             hanging above a distant horizon.
      - 100% symmetric, crisp geometric arcs and planes (zero stray notches, zero dot/hole clusters).
    """
    w_img, h_img = draw.im.size
    R_med = 176.0 * scale * ss
    cxs, cys = cx * ss, cy * ss

    # 1. Outer circular medallion mask (solid disk of radius R_med = 176 px)
    mask_med = Image.new("L", (w_img, h_img), 0)
    ImageDraw.Draw(mask_med).ellipse((cxs - R_med, cys - R_med, cxs + R_med, cys + R_med), fill=255)

    # 2. Negative-space Vesica (Eye Sclera / Sky Vault) carved cleanly inside the medallion
    mask_vesica = Image.new("L", (w_img, h_img), 0)
    v_pts = vesica_points(cx, cy, half_len=152.0 * scale, half_width=86.0 * scale, tilt_deg=0.0, ss=ss, steps=128)
    ImageDraw.Draw(mask_vesica).polygon(v_pts, fill=255)

    # Medallion minus vesica = upper and lower eyelid/vault crescents
    mask_ink = Image.composite(Image.new("L", (w_img, h_img), 0), mask_med, mask_vesica)

    # 3. Central Iris / Canyon Cliffs (radius 68 px) inside the negative-space eye
    r_iris = 68.0 * scale * ss
    mask_iris = Image.new("L", (w_img, h_img), 0)
    di = ImageDraw.Draw(mask_iris)
    di.ellipse((cxs - r_iris, cys - r_iris, cxs + r_iris, cys + r_iris), fill=255)

    # Carve the circular cradle for the warm Pupil / Distant Sun at (cx, cy - 12)
    sun_cy = (cy - 12.0 * scale) * ss
    r_sun = 25.0 * scale * ss
    r_sun_gap = (25.0 + 11.0) * scale * ss
    di.ellipse((cxs - r_sun_gap, sun_cy - r_sun_gap, cxs + r_sun_gap, sun_cy + r_sun_gap), fill=0)

    # Carve the negative-space Perspective Pathway / Lower Iris Catchlight Wedge opening downward from the sun cradle
    # to the lower sclera of the eye — turning the two sides of the iris into canyon portal walls!
    w_top = 13.0 * scale * ss
    w_bot = 46.0 * scale * ss
    y_top_path = sun_cy + 18.0 * scale * ss
    y_bot_path = (cy + 92.0 * scale) * ss
    di.polygon(
        [
            (cxs - w_top, y_top_path),
            (cxs + w_top, y_top_path),
            (cxs + w_bot, y_bot_path),
            (cxs - w_bot, y_bot_path),
        ],
        fill=0,
    )

    # Clip the iris strictly inside the vesica with a 12 px breathing collar at top & bottom eyelids
    mask_vesica_inner = Image.new("L", (w_img, h_img), 0)
    v_in_pts = vesica_points(cx, cy, half_len=140.0 * scale, half_width=74.0 * scale, tilt_deg=0.0, ss=ss, steps=128)
    ImageDraw.Draw(mask_vesica_inner).polygon(v_in_pts, fill=255)
    mask_iris = Image.composite(mask_iris, Image.new("L", (w_img, h_img), 0), mask_vesica_inner)

    # Combine outer medallion and inner iris/cliffs
    mask_final = Image.composite(Image.new("L", (w_img, h_img), 255), mask_ink, mask_iris)
    draw.bitmap((0, 0), mask_final, fill=ink)

    # 4. Warm Ochre-Amber Pupil / Rising World Disc at the focal apex of the perspective corridor
    draw.ellipse((cxs - r_sun, sun_cy - r_sun, cxs + r_sun, sun_cy + r_sun), fill=accent)


def generate_index_html():
    html = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>OUTSIGHT — Studio Identity Concepts &amp; 5s Boot Splash Storyboards</title>
  <style>
    :root {
      --bg-page: #0B0D14;
      --bg-card: #121622;
      --bg-navy: #0B0E2A;
      --border: #232A3E;
      --ivory: #F4EFE6;
      --ochre: #E2A856;
      --muted: #9A9892;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background: var(--bg-page);
      color: var(--ivory);
      font-family: "Be Vietnam Pro", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      line-height: 1.55;
      padding: 36px 42px 72px;
      max-width: 1680px;
      margin: 0 auto;
    }
    header {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: flex-end;
      gap: 20px;
      padding-bottom: 24px;
      border-bottom: 1px solid var(--border);
      margin-bottom: 32px;
    }
    h1 {
      font-size: 1.75rem;
      font-weight: 600;
      letter-spacing: 0.06em;
      color: var(--ivory);
    }
    .subtitle {
      color: var(--muted);
      font-size: 0.94rem;
      margin-top: 6px;
      max-width: 920px;
    }
    .controls {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .btn {
      background: #171C2B;
      color: var(--ivory);
      border: 1px solid var(--border);
      padding: 8px 15px;
      border-radius: 6px;
      font-size: 0.82rem;
      letter-spacing: 0.04em;
      cursor: pointer;
      transition: all 0.15s ease;
    }
    .btn.active, .btn:hover {
      background: var(--ochre);
      color: #0B0C10;
      border-color: var(--ochre);
      font-weight: 600;
    }
    .section-title {
      font-size: 1.15rem;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ochre);
      margin: 34px 0 14px;
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .section-note {
      font-size: 0.88rem;
      color: var(--muted);
      font-weight: 400;
      letter-spacing: 0;
      text-transform: none;
    }
    .grid-4 {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 20px;
    }
    @media (max-width: 1280px) {
      .grid-4 { grid-template-columns: repeat(2, 1fr); }
    }
    .card {
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: 10px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }
    .card.pick {
      border-color: rgba(226, 168, 86, 0.65);
      box-shadow: 0 0 0 1px rgba(226, 168, 86, 0.22);
    }
    .pair-wrap {
      display: grid;
      grid-template-columns: 1fr 1fr;
      border-bottom: 1px solid var(--border);
      background: #0B0C10;
    }
    body.mode-black .pair-wrap,
    body.mode-white .pair-wrap {
      grid-template-columns: 1fr;
    }
    body.mode-black .img-white { display: none; }
    body.mode-white .img-black { display: none; }
    .pair-wrap img {
      width: 100%;
      aspect-ratio: 1 / 1;
      display: block;
    }
    .img-black { border-right: 1px solid var(--border); }
    .card-body {
      padding: 16px 18px 18px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      flex: 1;
    }
    .card-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }
    .dir-title {
      font-size: 0.92rem;
      font-weight: 600;
      letter-spacing: 0.05em;
      color: var(--ivory);
    }
    .badge {
      background: rgba(226, 168, 86, 0.16);
      color: var(--ochre);
      border: 1px solid rgba(226, 168, 86, 0.45);
      font-size: 0.70rem;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 4px;
      letter-spacing: 0.06em;
      white-space: nowrap;
    }
    .rationale {
      font-size: 0.87rem;
      color: #D8D4CA;
      flex: 1;
    }
    .favicon-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding-top: 8px;
      border-top: 1px solid rgba(255,255,255,0.06);
      font-size: 0.76rem;
      color: var(--muted);
    }
    .favicon-row img {
      border-radius: 3px;
      border: 1px solid rgba(255,255,255,0.14);
      image-rendering: auto;
    }
    .links {
      font-size: 0.76rem;
      color: var(--muted);
    }
    .links a {
      color: var(--ochre);
      text-decoration: none;
    }
    .links a:hover { text-decoration: underline; }
    .splash-block {
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 20px;
      margin-bottom: 24px;
    }
    .splash-header {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 14px;
      flex-wrap: wrap;
      gap: 10px;
    }
    .splash-title {
      font-size: 1.0rem;
      font-weight: 600;
      color: var(--ivory);
      letter-spacing: 0.04em;
    }
    .frame-card {
      background: var(--bg-navy);
      border: 1px solid #242B52;
      border-radius: 8px;
      overflow: hidden;
    }
    .frame-card img {
      width: 100%;
      aspect-ratio: 1 / 1;
      display: block;
    }
  </style>
</head>
<body class="mode-both">
  <header>
    <div>
      <h1>OUTSIGHT — STUDIO LOGO CONCEPTS (A · B · C · D)</h1>
      <p class="subtitle">
        Studio identity for <strong>"Outsight"</strong> (shown as <code>OUTSIGHT</code> or <code>Outsight Games</code>) — the opposite of insight, looking outward beyond what humans know; games about worlds and perspectives nobody has made yet. Timeless, calm, curious, a little mysterious. Zero dot/hole clusters.
      </p>
    </div>
    <div class="controls">
      <button class="btn active" id="btn-both" onclick="setMode('both')">Side-by-Side (Black + Off-White)</button>
      <button class="btn" id="btn-black" onclick="setMode('black')">On Black (1024 px)</button>
      <button class="btn" id="btn-white" onclick="setMode('white')">On Off-White (1024 px)</button>
    </div>
  </header>

  <div class="section-title">
    <span>1 · Four Distinct Studio Logo Directions (Mark + Wordmark · 1024×1024 PNG)</span>
    <span class="section-note">Click any image link below a card to open the full 1024×1024 PNG</span>
  </div>

  <div class="grid-4">
    <!-- DIRECTION A -->
    <div class="card pick">
      <div class="pair-wrap">
        <img class="img-black" src="a/on_black.png" alt="Direction A on Black" />
        <img class="img-white" src="a/on_offwhite.png" alt="Direction A on Off-White" />
      </div>
      <div class="card-body">
        <div class="card-head">
          <span class="dir-title">A · THE OUTER APERTURE</span>
          <span class="badge">★ BEST PICK #1</span>
        </div>
        <p class="rationale">
          A circular boundary ("O") that parts on its upper-right arc as a calm vesica lens looks outward beyond the ring—turning "insight" inside out into a timeless mark that reads at 16 px and 4K.
        </p>
        <div class="favicon-row">
          <span>Favicon scale:</span>
          <img src="a/favicon_64.png" width="32" height="32" alt="32px" title="32x32 px" />
          <img src="a/favicon_64.png" width="16" height="16" alt="16px" title="16x16 px" />
        </div>
        <div class="links">
          1024 px: <a href="a/on_black.png">a/on_black.png</a> · <a href="a/on_offwhite.png">a/on_offwhite.png</a> · <a href="a/transparent.png">a/transparent.png</a>
        </div>
      </div>
    </div>

    <!-- DIRECTION B -->
    <div class="card">
      <div class="pair-wrap">
        <img class="img-black" src="b/on_black.png" alt="Direction B on Black" />
        <img class="img-white" src="b/on_offwhite.png" alt="Direction B on Off-White" />
      </div>
      <div class="card-body">
        <div class="card-head">
          <span class="dir-title">B · THE OPEN HORIZON</span>
        </div>
        <p class="rationale">
          A quiet horizontal datum that parts at its center to let a vaulted portal and warm perspective light-beam pass across the threshold—evoking an unmapped doorway on the edge of a calm horizon.
        </p>
        <div class="favicon-row">
          <span>Favicon scale:</span>
          <img src="b/favicon_64.png" width="32" height="32" alt="32px" title="32x32 px" />
          <img src="b/favicon_64.png" width="16" height="16" alt="16px" title="16x16 px" />
        </div>
        <div class="links">
          1024 px: <a href="b/on_black.png">b/on_black.png</a> · <a href="b/on_offwhite.png">b/on_offwhite.png</a> · <a href="b/transparent.png">b/transparent.png</a>
        </div>
      </div>
    </div>

    <!-- DIRECTION C -->
    <div class="card pick">
      <div class="pair-wrap">
        <img class="img-black" src="c/on_black.png" alt="Direction C on Black" />
        <img class="img-white" src="c/on_offwhite.png" alt="Direction C on Off-White" />
      </div>
      <div class="card-body">
        <div class="card-head">
          <span class="dir-title">C · THE LEAVING LINE</span>
          <span class="badge">★ BEST PICK #2</span>
        </div>
        <p class="rationale">
          A single unbroken geometric ribbon that traces a circle, smoothly inflects across its center, and departs straight through an opening in its own rim—an iconic monogram of leaving the known loop.
        </p>
        <div class="favicon-row">
          <span>Favicon scale:</span>
          <img src="c/favicon_64.png" width="32" height="32" alt="32px" title="32x32 px" />
          <img src="c/favicon_64.png" width="16" height="16" alt="16px" title="16x16 px" />
        </div>
        <div class="links">
          1024 px: <a href="c/on_black.png">c/on_black.png</a> · <a href="c/on_offwhite.png">c/on_offwhite.png</a> · <a href="c/transparent.png">c/transparent.png</a>
        </div>
      </div>
    </div>

    <!-- DIRECTION D -->
    <div class="card">
      <div class="pair-wrap">
        <img class="img-black" src="d/on_black.png" alt="Direction D on Black" />
        <img class="img-white" src="d/on_offwhite.png" alt="Direction D on Off-White" />
      </div>
      <div class="card-body">
        <div class="card-head">
          <span class="dir-title">D · TWO PERSPECTIVES</span>
        </div>
        <p class="rationale">
          A reversible figure-ground medallion whose negative space reads simultaneously as an open eye with a warm pupil and as a canyon portal opening onto a distant sun—two perspectives in one shape.
        </p>
        <div class="favicon-row">
          <span>Favicon scale:</span>
          <img src="d/favicon_64.png" width="32" height="32" alt="32px" title="32x32 px" />
          <img src="d/favicon_64.png" width="16" height="16" alt="16px" title="16x16 px" />
        </div>
        <div class="links">
          1024 px: <a href="d/on_black.png">d/on_black.png</a> · <a href="d/on_offwhite.png">d/on_offwhite.png</a> · <a href="d/transparent.png">d/transparent.png</a>
        </div>
      </div>
    </div>
  </div>

  <!-- 5-SECOND BOOT SPLASH STORYBOARDS FOR BEST 2 DIRECTIONS -->
  <div class="section-title">
    <span>2 · 5-Second Calm Navy (#0B0E2A) Boot "Splash" Storyboards (Best 2 Directions: A &amp; C)</span>
    <span class="section-note">Subtle animated reveal before the game's title screen — weightless easing, zero flashy effects</span>
  </div>

  <div class="splash-block">
    <div class="splash-header">
      <div class="splash-title">STORYBOARD 1 — DIRECTION A ("THE OUTER APERTURE" · 0.0 s → 5.0 s)</div>
      <div class="links">
        Composite 2×2 sheet: <a href="a/splash_storyboard.png">a/splash_storyboard.png</a> · Frames:
        <a href="a/splash_1.png">splash_1.png</a> · <a href="a/splash_2.png">splash_2.png</a> ·
        <a href="a/splash_3.png">splash_3.png</a> · <a href="a/splash_4.png">splash_4.png</a>
      </div>
    </div>
    <div class="grid-4">
      <div class="frame-card"><img src="a/splash_1.png" alt="Direction A Frame 1" /></div>
      <div class="frame-card"><img src="a/splash_2.png" alt="Direction A Frame 2" /></div>
      <div class="frame-card"><img src="a/splash_3.png" alt="Direction A Frame 3" /></div>
      <div class="frame-card"><img src="a/splash_4.png" alt="Direction A Frame 4" /></div>
    </div>
  </div>

  <div class="splash-block">
    <div class="splash-header">
      <div class="splash-title">STORYBOARD 2 — DIRECTION C ("THE LEAVING LINE" · 0.0 s → 5.0 s)</div>
      <div class="links">
        Composite 2×2 sheet: <a href="c/splash_storyboard.png">c/splash_storyboard.png</a> · Frames:
        <a href="c/splash_1.png">splash_1.png</a> · <a href="c/splash_2.png">splash_2.png</a> ·
        <a href="c/splash_3.png">splash_3.png</a> · <a href="c/splash_4.png">splash_4.png</a>
      </div>
    </div>
    <div class="grid-4">
      <div class="frame-card"><img src="c/splash_1.png" alt="Direction C Frame 1" /></div>
      <div class="frame-card"><img src="c/splash_2.png" alt="Direction C Frame 2" /></div>
      <div class="frame-card"><img src="c/splash_3.png" alt="Direction C Frame 3" /></div>
      <div class="frame-card"><img src="c/splash_4.png" alt="Direction C Frame 4" /></div>
    </div>
  </div>

  <script>
    function setMode(mode) {
      document.body.className = 'mode-' + mode;
      document.getElementById('btn-both').classList.toggle('active', mode === 'both');
      document.getElementById('btn-black').classList.toggle('active', mode === 'black');
      document.getElementById('btn-white').classList.toggle('active', mode === 'white');
    }
  </script>
</body>
</html>
"""
    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    index_path = OUT_ROOT / "index.html"
    index_path.write_text(html, encoding="utf-8")
    print("[build_studio_logos] Wrote side-by-side gallery to", index_path)


if __name__ == "__main__":
    render_direction_lockups("a", draw_mark_a, wordmark_style="geometric", show_games=True)
    render_storyboard_a()
    render_direction_lockups("b", draw_mark_b, wordmark_style="geometric", show_games=True)
    render_direction_lockups("c", draw_mark_c, wordmark_style="geometric", show_games=False)
    render_storyboard_c()
    render_direction_lockups("d", draw_mark_d, wordmark_style="mixed_case", show_games=True)
    generate_index_html()




