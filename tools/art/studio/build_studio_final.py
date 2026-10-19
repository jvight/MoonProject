#!/usr/bin/env python3
"""
Deterministic 4x-supersampled geometric vector generator for Round 2 (Final):
Studio = "Outsight Worlds"
Logo   = Direction C ("The Leaving Line" — a stroke stepping out of the ring)

Round 2 deliverables under `Logs/studio/final/`:
  1. Fix the cigarette risk on the departing stroke's end:
     - Show BOTH tip treatments:
       (a) PRIMARY (`glint`): The stroke gently tapers to a smooth rounded tip as it steps out of the ring,
           culminating in a calm, tiny 4-point star glint / bright point at the stroke's end.
       (b) ALTERNATE (`taper`): The stroke tapers continuously to a fine needle point like a calm comet trail.
     - Side-by-side comparison sheet (`tip_variants_comparison.png`) + individual renders.
  2. Wordmarks:
     - Primary stacked wordmark: "OUTSIGHT" large + "WORLDS" small, tracked, below.
     - One-line lockups:
       * Horizontal one-line lockup (`oneline_*`: Mark on left + "OUTSIGHT WORLDS" on a single line).
       * Stacked mark with single-line wordmark (`oneline_wordmark_*`: Mark above + "OUTSIGHT WORLDS" on one line).
  3. Full Export Matrix (`Logs/studio/final/`):
     - Mark-only + Full lockup + One-line lockup on:
       * Black (`#0B0C10`)
       * Calm Navy (`#0B0E2A`, the boot screen)
       * Off-White (`#F4EFE6`)
     - Transparent 2048x2048 px exports (`transparent_2048.png`, `mark_transparent_2048.png`,
       `oneline_transparent_2048.png`, `one_colour_transparent_2048.png`, etc.)
     - Favicons at `64x64` (`favicon_64.png`) and `32x32` (`favicon_32.png`)
     - 1-Colour (monochrome) versions (`one_colour_on_black.png`, `one_colour_on_navy.png`,
       `one_colour_on_offwhite.png`, `one_colour_transparent_2048.png`)
  4. Boot Splash (`~4.0 s total` for `BootGate` dark `#0B0E2A`):
     - Final 4-frame storyboard (`splash_1.png`..`splash_4.png` + `splash_storyboard.png`)
     - 16-frame animation sequence (`frames/frame_00.png`..`frame_15.png` + `splash_sequence_strip.png`)
     - Layer elements (`layers/ring.png`, `layers/stroke.png`, `layers/glint.png`, `layers/wordmark.png`)
     - Machine-readable timing spec (`boot_splash_timing.json` + `BOOT_SPLASH_SPEC.md`)
"""

import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[2]
FONT_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Fonts"
OUT_ROOT = REPO_ROOT / "Logs" / "studio"
FINAL_DIR = OUT_ROOT / "final"

# Standard output size is 1024x1024 (with 4x supersampling -> 4096x4096).
# For 2048x2048 transparent exports we render at 6144x6144 (3x supersampling of 2048, or 6x of 1024) and Lanczos-downsample.
OUT_SIZE = 1024
SS_DEFAULT = 4

# Palettes
BG_BLACK = (11, 12, 16, 255)          # #0B0C10 — Deep gallery obsidian
BG_NAVY = (11, 14, 42, 255)           # #0B0E2A — Calm deep navy boot screen (BootGate / ProjectSettings)
BG_OFFWHITE = (244, 239, 230, 255)    # #F4EFE6 — Warm archival cotton paper

INK_ON_DARK = (244, 239, 230, 255)    # #F4EFE6 — Warm ivory primary on black & navy
SUB_ON_BLACK = (164, 161, 154, 255)   # #A4A19A — Quiet warm stone for "WORLDS" on black
SUB_ON_NAVY = (168, 172, 192, 255)    # #A8ACC0 — Calm starlight slate for "WORLDS" on navy
ACCENT_ON_DARK = (226, 168, 86, 255)  # #E2A856 — Calm ochre-amber glint warmth on dark
CORE_ON_DARK = (255, 250, 238, 255)   # #FFFAEE — Soft luminous ivory core at center of glint

INK_ON_WHITE = (18, 20, 28, 255)      # #12141C — Deep charcoal ink on off-white
SUB_ON_WHITE = (104, 102, 98, 255)    # #686662 — Muted warm slate for "WORLDS" on off-white
ACCENT_ON_WHITE = (196, 122, 44, 255) # #C47A2C — Warm sienna-ochre glint accent on paper


def load_font(name: str, size_1024: float, ss: float = SS_DEFAULT) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    return ImageFont.truetype(str(path), max(1, int(round(size_1024 * ss))))


def draw_tracked_text(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple,
    tracking: float = 0.0,
    anchor_x: str = "center",
    anchor_y: str = "center",
    ss: float = SS_DEFAULT,
) -> float:
    """Draws text with explicit inter-character tracking (in 1024-space px). Returns total width in 1024-space."""
    track_px = float(tracking * ss)
    advances = [font.getlength(ch) for ch in text]
    total_w = sum(advances) + track_px * max(0, len(text) - 1)
    bbox = font.getbbox(text)
    text_h = bbox[3] - bbox[1]

    if anchor_x == "center":
        x = float(cx * ss) - total_w / 2.0
    elif anchor_x == "left":
        x = float(cx * ss)
    else:
        x = float(cx * ss) - total_w

    if anchor_y == "center":
        y = float(cy * ss) - bbox[1] - text_h / 2.0
    elif anchor_y == "top":
        y = float(cy * ss) - bbox[1]
    else:
        y = float(cy * ss) - bbox[3]

    for ch, adv in zip(text, advances):
        draw.text((x, y), ch, font=font, fill=fill)
        x += adv + track_px

    return total_w / ss


def measure_tracked_text(text: str, font: ImageFont.FreeTypeFont, tracking: float, ss: float = SS_DEFAULT) -> float:
    track_px = float(tracking * ss)
    advances = [font.getlength(ch) for ch in text]
    total_w = sum(advances) + track_px * max(0, len(text) - 1)
    return total_w / ss


def apply_layer_opacity(layer: Image.Image, opacity: float) -> Image.Image:
    """Multiplies the alpha channel of an RGBA layer by `opacity` (0..1) without polygon junction seams."""
    if opacity >= 0.999:
        return layer
    if opacity <= 0.001:
        return Image.new("RGBA", layer.size, (0, 0, 0, 0))
    r, g, b, a = layer.split()
    a = a.point(lambda p: int(round(p * opacity)))
    return Image.merge("RGBA", (r, g, b, a))


def lerp_color(c1: tuple, c2: tuple, t: float) -> tuple:
    t_c = max(0.0, min(1.0, t))
    return tuple(int(round(c1[i] + (c2[i] - c1[i]) * t_c)) for i in range(4))


def draw_thick_arc_poly(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    r: float,
    deg_start: float,
    deg_end: float,
    thickness: float,
    fill: tuple,
    chamfer_deg: float = 0.0,
    ss: float = SS_DEFAULT,
):
    """Draws a precision anti-aliased circular arc ribbon in supersampled coordinates."""
    steps = max(120, int(abs(deg_end - deg_start) * 1.8))
    r_outer = (r + thickness / 2.0) * ss
    r_inner = (r - thickness / 2.0) * ss
    cxs, cys = cx * ss, cy * ss

    outer_pts = []
    inner_pts = []
    for i in range(steps + 1):
        t = i / steps
        ang_o = math.radians(deg_start + (deg_end - deg_start) * t)
        ang_i = math.radians(
            (deg_start + chamfer_deg) + ((deg_end - chamfer_deg) - (deg_start + chamfer_deg)) * t
        )
        outer_pts.append((cxs + r_outer * math.cos(ang_o), cys + r_outer * math.sin(ang_o)))
        inner_pts.append((cxs + r_inner * math.cos(ang_i), cys + r_inner * math.sin(ang_i)))

    poly = outer_pts + list(reversed(inner_pts))
    draw.polygon(poly, fill=fill)


def draw_four_point_glint(
    draw: ImageDraw.ImageDraw,
    gx: float,
    gy: float,
    radius_main: float,
    radius_minor: float,
    waist: float,
    rot_deg: float,
    fill: tuple,
    core_fill: tuple | None = None,
    core_radius: float = 0.0,
    ss: float = SS_DEFAULT,
):
    """
    Draws a crisp, calm 4-point star glint (concave astroid curve) centered at (gx, gy) in 1024-space.
    Each quadrant connects a cardinal tip to the adjacent tip via a smooth cubic Bezier curve pulled
    inward toward `waist` along the 45-degree diagonals.
    """
    if radius_main <= 0.1:
        return

    rad_rot = math.radians(rot_deg)
    cr, sr = math.cos(rad_rot), math.sin(rad_rot)
    gxs, gys = gx * ss, gy * ss

    def to_pt(lx: float, ly: float) -> tuple[float, float]:
        rx = lx * cr - ly * sr
        ry = lx * sr + ly * cr
        return (gxs + rx * ss, gys + ry * ss)

    # 4 cardinal tips: (0, -R_main), (+R_minor, 0), (0, +R_main), (-R_minor, 0)
    tips = [
        (0.0, -radius_main),
        (radius_minor, 0.0),
        (0.0, radius_main),
        (-radius_minor, 0.0),
    ]
    diags = [
        (waist, -waist),
        (waist, waist),
        (-waist, waist),
        (-waist, -waist),
    ]

    pts = []
    steps_per_quad = 32
    for q in range(4):
        p0 = tips[q]
        p3 = tips[(q + 1) % 4]
        mid = diags[q]
        # Control points pulled gently toward the diagonal waist for a clean, sharp-pointed astroid star
        p1 = (p0[0] * 0.32 + mid[0] * 0.68, p0[1] * 0.32 + mid[1] * 0.68)
        p2 = (p3[0] * 0.32 + mid[0] * 0.68, p3[1] * 0.32 + mid[1] * 0.68)
        for i in range(steps_per_quad):
            t = i / steps_per_quad
            mt = 1.0 - t
            bx = (mt**3) * p0[0] + 3 * (mt**2) * t * p1[0] + 3 * mt * (t**2) * p2[0] + (t**3) * p3[0]
            by = (mt**3) * p0[1] + 3 * (mt**2) * t * p1[1] + 3 * mt * (t**2) * p2[1] + (t**3) * p3[1]
            pts.append(to_pt(bx, by))

    draw.polygon(pts, fill=fill)

    if core_fill is not None and core_radius > 0.1:
        rc = core_radius * ss
        draw.ellipse((gxs - rc, gys - rc, gxs + rc, gys + rc), fill=core_fill)


def draw_mark_c_final(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    scale: float,
    ink: tuple,
    accent: tuple,
    variant: str = "glint",
    one_colour: bool = False,
    circle_alpha: float = 1.0,
    line_progress: float = 1.0,
    glint_scale: float = 1.0,
    glint_alpha: float = 1.0,
    ss: float = SS_DEFAULT,
):
    """
    Draws finalized Direction C ('The Leaving Line' — Outsight Worlds):
      - `variant="glint"` (PRIMARY):
        The continuous ribbon traces the upper-left arc of the ring (26 px thick), inflects smoothly across
        the center with C^1 continuity, and steps straight out through the upper-right opening (-36 deg).
        As it steps out of the ring, the stroke gently narrows (26 px -> 14.5 px) into a smooth rounded dome cap
        at u = 200 px, and poised just off its tip at u = 232 px shines a calm 4-point star glint (zero cigarette look!).
      - `variant="taper"` (ALTERNATE — Comet Trail):
        As the stroke steps out from the center through the upper-right opening, it tapers continuously to a
        fine needle point (26 px -> 1.6 px at u = 250 px) like a calm comet trail, with a subtle warm tip glow.
    """
    exit_deg = -36.0
    exit_rad = math.radians(exit_deg)
    ux, uy = math.cos(exit_rad), math.sin(exit_rad)
    vx, vy = -uy, ux  # +90 deg clockwise from exit vector (lower-right)

    # Shift circle center slightly lower-left along -u so the outward line + glint balance optically at (cx, cy)
    ccx = cx - 18.0 * scale * ux
    ccy = cy - 18.0 * scale * uy

    R1 = 172.0 * scale
    thick = 26.0 * scale
    half_t = thick / 2.0

    def to_screen(u_val: float, v_val: float) -> tuple[float, float]:
        return ((ccx + u_val * ux + v_val * vx) * ss, (ccy + u_val * uy + v_val * vy) * ss)

    def to_world_1024(u_val: float, v_val: float) -> tuple[float, float]:
        return (ccx + u_val * ux + v_val * vx, ccy + u_val * uy + v_val * vy)

    # 1. Lower-right cradle arc: from phi = +26 deg clockwise to phi = 190 deg
    if circle_alpha > 0.005:
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
            chamfer_deg=3.0,
            ss=ss,
        )

    # 2. Continuous Unbroken Ribbon (Outer arc -> C^1 inner turn -> Straight departure along +u)
    if line_progress > 0.005:
        phi_start_outer = math.radians(334.0)
        phi_star = math.radians(222.0)
        sin_ps = math.sin(phi_star)
        cos_ps = math.cos(phi_star)
        r_turn = (-R1 * sin_ps) / (1.0 - sin_ps)
        uc = (R1 - r_turn) * cos_ps
        vc = (R1 - r_turn) * sin_ps

        spine_uv = []  # List of (u, v, half_width)
        # Segment A: Outer circle arc from 334 deg down to 222 deg (uniform half_t)
        outer_steps = 80
        for i in range(outer_steps):
            t = i / outer_steps
            phi = phi_start_outer + (phi_star - phi_start_outer) * t
            spine_uv.append((R1 * math.cos(phi), R1 * math.sin(phi), half_t))

        # Segment B: C^1 inner tangent turn from 222 deg down to 90 deg (landing on v = 0 at u = uc)
        turn_steps = 72
        for i in range(turn_steps + 1):
            t = i / turn_steps
            phi = phi_star + (math.radians(90.0) - phi_star) * t
            u_pt = uc + r_turn * math.cos(phi)
            v_pt = vc + r_turn * math.sin(phi)
            spine_uv.append((u_pt, v_pt, half_t))

        # Segment C: Straight departing line from u_exit = uc (at v = 0) outward through the ring gap
        u_exit = uc
        straight_steps = 96

        if variant == "glint":
            # Smooth convex comet-caliber narrowing over the outer half (26 px -> 11.6 px rounded tip at u = 202)
            # Preserves full architectural weight inside the ring while eliminating any blunt-cylinder cigarette look
            u_main_end = 202.0 * scale
            half_t_end = 5.8 * scale
            for i in range(1, straight_steps + 1):
                t = i / straight_steps
                u_pt = u_exit + (u_main_end - u_exit) * t
                if t <= 0.35:
                    hw = half_t
                else:
                    tt = (t - 0.35) / 0.65
                    ease = tt ** 1.85
                    hw = half_t + (half_t_end - half_t) * ease
                spine_uv.append((u_pt, 0.0, hw))
        else:
            # Variant "taper" (Parabolic convex comet-trail taper to a fine point at u = 244)
            u_main_end = 244.0 * scale
            half_t_end = 1.45 * scale
            for i in range(1, straight_steps + 1):
                t = i / straight_steps
                u_pt = u_exit + (u_main_end - u_exit) * t
                tt = t ** 1.95
                hw = half_t * (1.0 - tt) + half_t_end * tt
                spine_uv.append((u_pt, 0.0, hw))

        total_pts = len(spine_uv)
        n_draw = max(2, int(round(total_pts * min(1.0, line_progress))))
        active_spine = spine_uv[:n_draw]

        left_edge = []
        right_edge = []
        for idx, (u_p, v_p, hw) in enumerate(active_spine):
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
            left_edge.append(to_screen(u_p + nu * hw, v_p + nv * hw))
            right_edge.append(to_screen(u_p - nu * hw, v_p - nv * hw))

        # Add a smooth semicircular rounded cap at the forward tip of the stroke!
        u_last, v_last, hw_last = active_spine[-1]
        if len(active_spine) >= 2:
            du_end = u_last - active_spine[-2][0]
            dv_end = v_last - active_spine[-2][1]
        else:
            du_end, dv_end = 1.0, 0.0
        ang_forward = math.atan2(dv_end, du_end)
        cap_pts = []
        cap_steps = 24
        for i in range(1, cap_steps):
            # Sweep from +90 deg (left_edge) through 0 deg (forward apex) to -90 deg (right_edge)
            ang = ang_forward + (math.pi / 2.0) - math.pi * (i / cap_steps)
            cu = u_last + hw_last * math.cos(ang)
            cv = v_last + hw_last * math.sin(ang)
            cap_pts.append(to_screen(cu, cv))

        poly_stroke = left_edge + cap_pts + list(reversed(right_edge))
        draw.polygon(poly_stroke, fill=ink)

        # In "taper" (comet-trail) variant, if not one_colour, add a subtle warm ochre-amber comet-tip overlay
        # along the final tapered point (u = 178..244) so it keeps the studio's warm outward focal point
        # without any rectangular segment or gap.
        if variant == "taper" and not one_colour and line_progress > 0.78:
            taper_start_idx = total_pts - int(straight_steps * 0.45)
            if n_draw > taper_start_idx + 2:
                sub_spine = active_spine[taper_start_idx:]
                for seg_i in range(len(sub_spine) - 1):
                    t_col = (seg_i + 1) / max(1, len(sub_spine) - 1)
                    seg_col = lerp_color(ink, accent, min(1.0, t_col ** 0.9))
                    u0, v0, hw0 = sub_spine[seg_i]
                    u1, v1, hw1 = sub_spine[seg_i + 1]
                    quad = [
                        to_screen(u0, v0 + hw0),
                        to_screen(u1, v1 + hw1),
                        to_screen(u1, v1 - hw1),
                        to_screen(u0, v0 - hw0),
                    ]
                    draw.polygon(quad, fill=seg_col)
                if cap_pts:
                    tip_fan = [to_screen(u_last, v_last + hw_last)] + cap_pts + [to_screen(u_last, v_last - hw_last)]
                    draw.polygon(tip_fan, fill=accent)

    # 3. For "glint" variant (PRIMARY): Draw the calm, solid 4-point star glint poised just beyond the stroke's tip
    if variant == "glint" and glint_alpha > 0.005 and glint_scale > 0.01:
        u_glint = 229.0 * scale
        gx, gy = to_world_1024(u_glint, 0.0)
        g_col_base = ink if one_colour else accent
        g_col = (g_col_base[0], g_col_base[1], g_col_base[2], int(round(g_col_base[3] * glint_alpha)))

        draw_four_point_glint(
            draw,
            gx,
            gy,
            radius_main=19.5 * scale * glint_scale,
            radius_minor=19.5 * scale * glint_scale,
            waist=3.8 * scale * glint_scale,
            rot_deg=0.0,  # Cardinal vertical/horizontal axes — timeless optical star glint, zero center hole
            fill=g_col,
            core_fill=None,
            core_radius=0.0,
            ss=ss,
        )


def draw_wordmark_outsight_worlds(
    draw: ImageDraw.ImageDraw,
    cx: float,
    y_main: float,
    ink: tuple,
    sub_ink: tuple,
    layout: str = "stacked",
    scale: float = 1.0,
    ss: float = SS_DEFAULT,
):
    """
    Draws the 'Outsight Worlds' wordmark:
      - `layout="stacked"`: 'OUTSIGHT' large (56 px) + 'WORLDS' small (18.5 px), wide-tracked, 54 px below.
      - `layout="oneline_below"`: 'OUTSIGHT' + 'WORLDS' on a single baseline below the mark.
    """
    if layout == "stacked":
        font_main = load_font("BeVietnamPro-SemiBold.ttf", 56.0 * scale, ss=ss)
        font_sub = load_font("BeVietnamPro-Medium.ttf", 18.5 * scale, ss=ss)
        draw_tracked_text(draw, cx, y_main, "OUTSIGHT", font_main, ink, tracking=17.5 * scale, ss=ss)
        draw_tracked_text(draw, cx, y_main + 54.0 * scale, "WORLDS", font_sub, sub_ink, tracking=23.5 * scale, ss=ss)
    elif layout == "oneline_below":
        font_main = load_font("BeVietnamPro-SemiBold.ttf", 42.0 * scale, ss=ss)
        font_sub = load_font("BeVietnamPro-Regular.ttf", 42.0 * scale, ss=ss)
        track = 11.5 * scale
        gap = 34.0 * scale
        w1 = measure_tracked_text("OUTSIGHT", font_main, track, ss=ss)
        w2 = measure_tracked_text("WORLDS", font_sub, track, ss=ss)
        total_w = w1 + gap + w2
        x_start = cx - total_w / 2.0
        draw_tracked_text(
            draw, x_start, y_main, "OUTSIGHT", font_main, ink, tracking=track, anchor_x="left", ss=ss
        )
        draw_tracked_text(
            draw, x_start + w1 + gap, y_main, "WORLDS", font_sub, sub_ink, tracking=track, anchor_x="left", ss=ss
        )


def draw_horizontal_oneline_lockup(
    draw: ImageDraw.ImageDraw,
    cx: float,
    cy: float,
    ink: tuple,
    sub_ink: tuple,
    accent: tuple,
    variant: str = "glint",
    one_colour: bool = False,
    ss: float = SS_DEFAULT,
):
    """
    Draws the Horizontal One-Line Lockup:
    Mark on the left + 'OUTSIGHT WORLDS' on one horizontal line to the right, optically centered on the canvas.
    """
    mark_scale = 0.34
    font_main = load_font("BeVietnamPro-SemiBold.ttf", 42.0, ss=ss)
    font_sub = load_font("BeVietnamPro-Regular.ttf", 42.0, ss=ss)
    track = 10.5
    word_gap = 28.0
    mark_to_text_gap = 46.0

    w_out = measure_tracked_text("OUTSIGHT", font_main, track, ss=ss)
    w_wld = measure_tracked_text("WORLDS", font_sub, track, ss=ss)
    mark_diam = 360.0 * mark_scale  # ~122.4 px
    total_w = mark_diam + mark_to_text_gap + w_out + word_gap + w_wld

    left_x = cx - total_w / 2.0
    mark_cx = left_x + mark_diam / 2.0
    text_x = left_x + mark_diam + mark_to_text_gap

    draw_mark_c_final(
        draw,
        mark_cx,
        cy,
        scale=mark_scale,
        ink=ink,
        accent=accent,
        variant=variant,
        one_colour=one_colour,
        ss=ss,
    )
    draw_tracked_text(
        draw, text_x, cy, "OUTSIGHT", font_main, ink, tracking=track, anchor_x="left", anchor_y="center", ss=ss
    )
    draw_tracked_text(
        draw,
        text_x + w_out + word_gap,
        cy,
        "WORLDS",
        font_sub,
        sub_ink,
        tracking=track,
        anchor_x="left",
        anchor_y="center",
        ss=ss,
    )


def make_calm_navy_boot_canvas(w_px: int, h_px: int) -> Image.Image:
    """Creates the calm deep-navy (#0B0E2A) boot screen canvas."""
    overlay = Image.new("RGBA", (w_px, h_px), BG_NAVY)
    d = ImageDraw.Draw(overlay)
    cx, cy = w_px / 2.0, h_px / 2.0
    max_r = math.hypot(cx, cy)
    for step in range(24, 0, -1):
        t = step / 24.0
        r = max_r * t
        r_col = int(round(9 + 6 * (1.0 - t)))
        g_col = int(round(12 + 8 * (1.0 - t)))
        b_col = int(round(36 + 20 * (1.0 - t)))
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(r_col, g_col, b_col, 255))
    return overlay


def render_canvas_1024(bg_color: tuple | None, draw_fn, out_px: int = 1024, ss: int = 4) -> Image.Image:
    w_hi = out_px * ss
    h_hi = out_px * ss
    scale_coord = (out_px / 1024.0) * ss
    img = Image.new("RGBA", (w_hi, h_hi), bg_color if bg_color is not None else (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    draw_fn(d, scale_coord)
    return img.resize((out_px, out_px), Image.Resampling.LANCZOS)


def render_all_final_exports():
    FINAL_DIR.mkdir(parents=True, exist_ok=True)
    cx, cy_mark_lockup, y_word_lockup = 512.0, 404.0, 748.0
    cx_mark_only, cy_mark_only = 512.0, 512.0

    surfaces = [
        ("black", BG_BLACK, INK_ON_DARK, SUB_ON_BLACK, ACCENT_ON_DARK),
        ("navy", BG_NAVY, INK_ON_DARK, SUB_ON_NAVY, ACCENT_ON_DARK),
        ("offwhite", BG_OFFWHITE, INK_ON_WHITE, SUB_ON_WHITE, ACCENT_ON_WHITE),
    ]

    # --------------------------------------------------------------------------
    # 1. FULL LOCKUP (Mark + Stacked "OUTSIGHT" / "WORLDS") on Black, Navy, Off-White
    # --------------------------------------------------------------------------
    for surf_name, bg, ink, sub_ink, accent in surfaces:
        def _draw_full(d, ss_val, _ink=ink, _sub=sub_ink, _acc=accent):
            draw_mark_c_final(d, cx, cy_mark_lockup, 1.0, _ink, _acc, variant="glint", ss=ss_val)
            draw_wordmark_outsight_worlds(d, cx, y_word_lockup, _ink, _sub, layout="stacked", ss=ss_val)

        img = render_canvas_1024(bg, _draw_full, out_px=1024, ss=4)
        img.save(FINAL_DIR / f"lockup_on_{surf_name}.png")
        img.save(FINAL_DIR / f"on_{surf_name}.png")  # Clean shorthand alias

    # --------------------------------------------------------------------------
    # 2. MARK-ONLY on Black, Navy, Off-White (1024x1024)
    # --------------------------------------------------------------------------
    for surf_name, bg, ink, _sub_ink, accent in surfaces:
        def _draw_mark(d, ss_val, _ink=ink, _acc=accent):
            draw_mark_c_final(d, cx_mark_only, cy_mark_only, 1.15, _ink, _acc, variant="glint", ss=ss_val)

        img = render_canvas_1024(bg, _draw_mark, out_px=1024, ss=4)
        img.save(FINAL_DIR / f"mark_on_{surf_name}.png")

    # --------------------------------------------------------------------------
    # 3. ONE-LINE LOCKUPS (Horizontal Banner + Single-Line Below Mark) on Black, Navy, Off-White
    # --------------------------------------------------------------------------
    for surf_name, bg, ink, sub_ink, accent in surfaces:
        # 3a. Horizontal one-line lockup ([Mark] OUTSIGHT WORLDS)
        def _draw_horiz(d, ss_val, _ink=ink, _sub=sub_ink, _acc=accent):
            draw_horizontal_oneline_lockup(d, 512.0, 512.0, _ink, _sub, _acc, variant="glint", ss=ss_val)

        img_h = render_canvas_1024(bg, _draw_horiz, out_px=1024, ss=4)
        img_h.save(FINAL_DIR / f"oneline_on_{surf_name}.png")

        # 3b. Stacked mark with single-line "OUTSIGHT WORLDS" wordmark below
        def _draw_oneline_below(d, ss_val, _ink=ink, _sub=sub_ink, _acc=accent):
            draw_mark_c_final(d, cx, 420.0, 0.96, _ink, _acc, variant="glint", ss=ss_val)
            draw_wordmark_outsight_worlds(d, cx, 768.0, _ink, _sub, layout="oneline_below", ss=ss_val)

        img_b = render_canvas_1024(bg, _draw_oneline_below, out_px=1024, ss=4)
        img_b.save(FINAL_DIR / f"oneline_stacked_on_{surf_name}.png")

    # --------------------------------------------------------------------------
    # 4. TRANSPARENT 2048x2048 EXPORTS (Full Lockup, Mark-Only, One-Line, 1-Colour)
    # --------------------------------------------------------------------------
    # Render at 2048x2048 with 3x supersampling (6144x6144 -> 2048x2048 Lanczos)
    def _draw_tr_full(d, ss_val):
        draw_mark_c_final(d, cx, cy_mark_lockup, 1.0, INK_ON_DARK, ACCENT_ON_DARK, variant="glint", ss=ss_val)
        draw_wordmark_outsight_worlds(d, cx, y_word_lockup, INK_ON_DARK, SUB_ON_BLACK, layout="stacked", ss=ss_val)

    img_tr_2048 = render_canvas_1024(None, _draw_tr_full, out_px=2048, ss=3)
    img_tr_2048.save(FINAL_DIR / "transparent_2048.png")
    img_tr_2048.save(FINAL_DIR / "lockup_transparent_2048.png")

    def _draw_tr_mark(d, ss_val):
        draw_mark_c_final(d, cx_mark_only, cy_mark_only, 1.15, INK_ON_DARK, ACCENT_ON_DARK, variant="glint", ss=ss_val)

    img_tr_mark_2048 = render_canvas_1024(None, _draw_tr_mark, out_px=2048, ss=3)
    img_tr_mark_2048.save(FINAL_DIR / "mark_transparent_2048.png")

    def _draw_tr_oneline(d, ss_val):
        draw_horizontal_oneline_lockup(d, 512.0, 512.0, INK_ON_DARK, SUB_ON_BLACK, ACCENT_ON_DARK, variant="glint", ss=ss_val)

    img_tr_oneline_2048 = render_canvas_1024(None, _draw_tr_oneline, out_px=2048, ss=3)
    img_tr_oneline_2048.save(FINAL_DIR / "oneline_transparent_2048.png")

    # --------------------------------------------------------------------------
    # 5. 1-COLOUR (MONOCHROME) VERSIONS (Black, Navy, Off-White, Transparent 2048)
    # --------------------------------------------------------------------------
    for surf_name, bg, ink, _sub_ink, _accent in surfaces:
        def _draw_1c(d, ss_val, _ink=ink):
            draw_mark_c_final(d, cx, cy_mark_lockup, 1.0, _ink, _ink, variant="glint", one_colour=True, ss=ss_val)
            draw_wordmark_outsight_worlds(d, cx, y_word_lockup, _ink, _ink, layout="stacked", ss=ss_val)

        img_1c = render_canvas_1024(bg, _draw_1c, out_px=1024, ss=4)
        img_1c.save(FINAL_DIR / f"one_colour_on_{surf_name}.png")

    def _draw_1c_tr_2048(d, ss_val):
        draw_mark_c_final(d, cx, cy_mark_lockup, 1.0, INK_ON_DARK, INK_ON_DARK, variant="glint", one_colour=True, ss=ss_val)
        draw_wordmark_outsight_worlds(d, cx, y_word_lockup, INK_ON_DARK, INK_ON_DARK, layout="stacked", ss=ss_val)

    img_1c_tr_2048 = render_canvas_1024(None, _draw_1c_tr_2048, out_px=2048, ss=3)
    img_1c_tr_2048.save(FINAL_DIR / "one_colour_transparent_2048.png")

    # --------------------------------------------------------------------------
    # 6. FAVICONS (64x64 and 32x32)
    # --------------------------------------------------------------------------
    # Optical scale 1.82 so the mark fills the icon square with clean breathing margin
    def _draw_fav(d, ss_val):
        draw_mark_c_final(d, 512.0, 512.0, 1.82, INK_ON_DARK, ACCENT_ON_DARK, variant="glint", ss=ss_val)

    fav_hi = render_canvas_1024(BG_BLACK, _draw_fav, out_px=512, ss=4)
    fav_hi.resize((64, 64), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_64.png")
    fav_hi.resize((32, 32), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_32.png")

    fav_navy_hi = render_canvas_1024(BG_NAVY, _draw_fav, out_px=512, ss=4)
    fav_navy_hi.resize((64, 64), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_64_navy.png")
    fav_navy_hi.resize((32, 32), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_32_navy.png")

    fav_tr_hi = render_canvas_1024(None, _draw_fav, out_px=512, ss=4)
    fav_tr_hi.resize((64, 64), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_64_transparent.png")
    fav_tr_hi.resize((32, 32), Image.Resampling.LANCZOS).save(FINAL_DIR / "favicon_32_transparent.png")

    # --------------------------------------------------------------------------
    # 7. TIP RISK FIX COMPARISON: BOTH VARIANTS (C1 'Glint' vs. C2 'Comet Taper')
    # --------------------------------------------------------------------------
    for surf_name, bg, ink, sub_ink, accent in surfaces:
        # Variant 1: Glint (Primary)
        img_g = Image.open(FINAL_DIR / f"lockup_on_{surf_name}.png")
        img_g.save(FINAL_DIR / f"variant_glint_on_{surf_name}.png")

        # Variant 2: Comet Taper (Alternate)
        def _draw_taper(d, ss_val, _ink=ink, _sub=sub_ink, _acc=accent):
            draw_mark_c_final(d, cx, cy_mark_lockup, 1.0, _ink, _acc, variant="taper", ss=ss_val)
            draw_wordmark_outsight_worlds(d, cx, y_word_lockup, _ink, _sub, layout="stacked", ss=ss_val)

        img_t = render_canvas_1024(bg, _draw_taper, out_px=1024, ss=4)
        img_t.save(FINAL_DIR / f"variant_taper_on_{surf_name}.png")

    # Build a side-by-side 2048x1024 comparison sheet (`tip_variants_comparison.png`) on Navy (#0B0E2A)
    comp_sheet = Image.new("RGBA", (2048, 1024), BG_NAVY)
    left_g = Image.open(FINAL_DIR / "variant_glint_on_navy.png")
    right_t = Image.open(FINAL_DIR / "variant_taper_on_navy.png")
    comp_sheet.paste(left_g, (0, 0))
    comp_sheet.paste(right_t, (1024, 0))
    dc = ImageDraw.Draw(comp_sheet)
    dc.line([(1024, 0), (1024, 1024)], fill=(244, 239, 230, 55), width=2)
    f_badge = load_font("IBMPlexMono-Medium.ttf", 19, ss=1)
    f_note = load_font("BeVietnamPro-Regular.ttf", 16, ss=1)
    draw_tracked_text(
        dc, 512.0, 56.0, "VARIANT C1 [PRIMARY PICK] · STROKE + 4-POINT GLINT", f_badge, ACCENT_ON_DARK, tracking=1.8, ss=1
    )
    draw_tracked_text(
        dc,
        512.0,
        88.0,
        "Gentle taper to rounded tip + calm 4-point star glint: zero cigarette silhouette, crisp at 32 px & 1-colour.",
        f_note,
        (244, 239, 230, 195),
        tracking=0.2,
        ss=1,
    )
    draw_tracked_text(
        dc, 1536.0, 56.0, "VARIANT C2 [ALTERNATE] · COMET-TRAIL FINE TAPER", f_badge, (168, 172, 192, 235), tracking=1.8, ss=1
    )
    draw_tracked_text(
        dc,
        1536.0,
        88.0,
        "Continuous parabolic taper to a fine comet point with warm ochre fade along the outward tail.",
        f_note,
        (244, 239, 230, 175),
        tracking=0.2,
        ss=1,
    )
    comp_sheet.save(FINAL_DIR / "tip_variants_comparison.png")

    print("[build_studio_final] Rendered all static lockups, mark-only, 2048px transparent, 1-colour, favicons, and tip comparison.")


# ==============================================================================
# 8. ~4.0-SECOND BOOT SPLASH STORYBOARD, FRAME SEQUENCE & TIMING SPEC (`BootGate`)
# ==============================================================================

def eval_boot_splash_state(t_sec: float) -> dict:
    """
    Evaluates the exact procedural animation state at time `t_sec` in [0.0, 4.0] seconds
    for the BootGate dark (#0B0E2A) studio splash screen:
      - 0.00 s – 0.25 s: Quiet BootGate hold on #0B0E2A; ring begins easing in.
      - 0.20 s – 1.45 s: Ring eases to 100%; stroke draws smoothly around the top-left, inflects
                         across the center, and steps out through the upper-right opening.
      - 1.30 s – 1.85 s: As the stroke reaches its outward tip, the 4-point glint blooms at the tip
                         (subtle scale pulse 0.35 -> 1.12 -> 1.00, alpha 0 -> 1.0).
      - 1.55 s – 2.25 s: Wordmark ("OUTSIGHT" + "WORLDS") fades in calmly below (y: +8 px -> 0 px).
      - 2.25 s – 3.15 s: Calm stillness hold (~0.90 s) at full lockup.
      - 3.15 s – 4.00 s: Smooth dissolve into #0B0E2A BootGate dark; the outward glint lingers
                         brightly past the ring/wordmark fade before reaching 0.0 at t = 4.00 s.
    """
    def smoothstep(edge0: float, edge1: float, x: float) -> float:
        if x <= edge0:
            return 0.0
        if x >= edge1:
            return 1.0
        u = (x - edge0) / (edge1 - edge0)
        return u * u * (3.0 - 2.0 * u)

    # Dissolve envelope (3.12 s -> 3.84 s for ring/stroke/wordmark; 3.48 s -> 4.00 s for glint so it lingers last)
    fade_out_main = 1.0 - smoothstep(3.12, 3.84, t_sec)
    fade_out_glint = 1.0 - smoothstep(3.48, 4.00, t_sec)

    # 1. Ring alpha & stroke progress
    ring_in = smoothstep(0.10, 0.95, t_sec)
    ring_alpha = ring_in * fade_out_main

    stroke_progress = smoothstep(0.20, 1.45, t_sec)
    stroke_alpha = smoothstep(0.15, 0.65, t_sec) * fade_out_main

    # 2. Glint alpha & scale
    glint_in = smoothstep(1.30, 1.72, t_sec)
    glint_alpha = glint_in * fade_out_glint
    if t_sec < 1.30:
        glint_scale = 0.35
    elif t_sec < 1.62:
        u = smoothstep(1.30, 1.62, t_sec)
        glint_scale = 0.35 + (1.12 - 0.35) * u
    elif t_sec < 1.95:
        u = smoothstep(1.62, 1.95, t_sec)
        glint_scale = 1.12 + (1.00 - 1.12) * u
    else:
        glint_scale = 1.00 - 0.12 * smoothstep(3.30, 4.00, t_sec)

    # 3. Wordmark alpha & subtle vertical rise
    word_in = smoothstep(1.50, 2.20, t_sec)
    word_alpha = word_in * fade_out_main
    word_dy = 8.0 * (1.0 - word_in)

    return {
        "t_sec": round(t_sec, 3),
        "ring_alpha": round(ring_alpha, 4),
        "stroke_progress": round(stroke_progress, 4),
        "stroke_alpha": round(stroke_alpha, 4),
        "glint_alpha": round(glint_alpha, 4),
        "glint_scale": round(glint_scale, 4),
        "word_alpha": round(word_alpha, 4),
        "word_dy": round(word_dy, 3),
    }


def render_boot_splash_frame(
    state: dict,
    out_px: int = 1024,
    ss: int = 3,
    caption_title: str | None = None,
    caption_time: str | None = None,
    caption_desc: str | None = None,
) -> Image.Image:
    wb, hb = out_px * ss, out_px * ss
    sc = (out_px / 1024.0) * ss
    canvas = make_calm_navy_boot_canvas(wb, hb)
    cx, cy_mark, y_word = 512.0, 404.0, 738.0
    mark_scale = 0.88

    # Layer 1: Lower-right cradle ring
    if state["ring_alpha"] > 0.005:
        l_ring = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        draw_mark_c_final(
            ImageDraw.Draw(l_ring),
            cx,
            cy_mark,
            scale=mark_scale,
            ink=INK_ON_DARK,
            accent=ACCENT_ON_DARK,
            variant="glint",
            circle_alpha=1.0,
            line_progress=0.0,
            glint_alpha=0.0,
            ss=sc,
        )
        canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_ring, state["ring_alpha"]))

    # Layer 2: Continuous stroke drawing out of the ring
    if state["stroke_alpha"] > 0.005 and state["stroke_progress"] > 0.005:
        l_stroke = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        draw_mark_c_final(
            ImageDraw.Draw(l_stroke),
            cx,
            cy_mark,
            scale=mark_scale,
            ink=INK_ON_DARK,
            accent=ACCENT_ON_DARK,
            variant="glint",
            circle_alpha=0.0,
            line_progress=state["stroke_progress"],
            glint_alpha=0.0,
            ss=sc,
        )
        canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_stroke, state["stroke_alpha"]))

    # Layer 3: 4-Point Glint at the departing tip
    if state["glint_alpha"] > 0.005:
        l_glint = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
        draw_mark_c_final(
            ImageDraw.Draw(l_glint),
            cx,
            cy_mark,
            scale=mark_scale,
            ink=INK_ON_DARK,
            accent=ACCENT_ON_DARK,
            variant="glint",
            circle_alpha=0.0,
            line_progress=0.0,
            glint_scale=state["glint_scale"],
            glint_alpha=1.0,
            ss=sc,
        )
        canvas = Image.alpha_composite(canvas, apply_layer_opacity(l_glint, state["glint_alpha"]))

    # Layer 4: Wordmark ("OUTSIGHT" / "WORLDS") + optional storyboard caption
    layer_ui = Image.new("RGBA", (wb, hb), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer_ui)
    if state["word_alpha"] > 0.005:
        wa = state["word_alpha"]
        w_ink = (INK_ON_DARK[0], INK_ON_DARK[1], INK_ON_DARK[2], int(round(255 * wa)))
        w_sub = (SUB_ON_NAVY[0], SUB_ON_NAVY[1], SUB_ON_NAVY[2], int(round(230 * wa)))
        draw_wordmark_outsight_worlds(
            d, cx, y_word + state["word_dy"], w_ink, w_sub, layout="stacked", ss=sc
        )

    if caption_title and caption_time and caption_desc:
        font_cap = load_font("IBMPlexMono-Medium.ttf", 18, ss=sc)
        font_sub = load_font("BeVietnamPro-Regular.ttf", 16.5, ss=sc)
        d.line(
            [(48 * sc, 912 * sc), ((1024 - 48) * sc, 912 * sc)],
            fill=(244, 239, 230, 38),
            width=max(1, int(round(sc))),
        )
        draw_tracked_text(
            d, 512.0, 942.0, f"{caption_title}   ·   {caption_time}", font_cap, (226, 168, 86, 225), tracking=2.0, ss=sc
        )
        draw_tracked_text(
            d, 512.0, 976.0, caption_desc, font_sub, (244, 239, 230, 180), tracking=0.25, ss=sc
        )

    return Image.alpha_composite(canvas, layer_ui).resize((out_px, out_px), Image.Resampling.LANCZOS)


def render_final_boot_splash_and_sequence():
    FINAL_DIR.mkdir(parents=True, exist_ok=True)
    frames_dir = FINAL_DIR / "frames"
    frames_dir.mkdir(parents=True, exist_ok=True)
    layers_dir = FINAL_DIR / "layers"
    layers_dir.mkdir(parents=True, exist_ok=True)

    # 1. Final 4-Frame Storyboard (~4.0 s total)
    storyboard_specs = [
        {
            "idx": 1,
            "t_sample": 0.92,
            "time": "0.0 s – 1.2 s",
            "title": "1 · STROKE DRAWS OUT OF RING",
            "desc": "On BootGate dark (#0B0E2A), the ribbon traces the circle and inflects toward the upper-right opening.",
        },
        {
            "idx": 2,
            "t_sample": 1.65,
            "time": "1.2 s – 2.0 s",
            "title": "2 · DEPARTURE & 4-POINT GLINT",
            "desc": "The stroke steps cleanly out of the ring; the warm 4-point glint blooms at its tip as wordmark begins.",
        },
        {
            "idx": 3,
            "t_sample": 2.65,
            "time": "2.0 s – 3.2 s",
            "title": "3 · WORDMARK FADES & CALM HOLD",
            "desc": "'OUTSIGHT WORLDS' settles at 100% opacity; the full lockup holds in quiet stillness for ~0.9 s.",
        },
        {
            "idx": 4,
            "t_sample": 3.58,
            "time": "3.2 s – 4.0 s",
            "title": "4 · DISSOLVE INTO BOOTGATE DARK",
            "desc": "Ring, stroke, and wordmark dissolve into #0B0E2A; the tiny outward glint lingers last before Title.",
        },
    ]

    sb_images = []
    for sp in storyboard_specs:
        st = eval_boot_splash_state(sp["t_sample"])
        img = render_boot_splash_frame(
            st,
            out_px=1024,
            ss=3,
            caption_title=sp["title"],
            caption_time=sp["time"],
            caption_desc=sp["desc"],
        )
        img.save(FINAL_DIR / f"splash_{sp['idx']}.png")
        sb_images.append(img)

    # Save 2x2 storyboard sheet (`splash_storyboard.png` at 1024x1024)
    sheet = Image.new("RGBA", (1024, 1024), BG_NAVY)
    half = 512
    for img_f, (px, py) in zip(sb_images, [(0, 0), (half, 0), (0, half), (half, half)]):
        sheet.paste(img_f.resize((half, half), Image.Resampling.LANCZOS), (px, py))
    ds = ImageDraw.Draw(sheet)
    ds.line([(half, 0), (half, 1024)], fill=(244, 239, 230, 48), width=2)
    ds.line([(0, half), (1024, half)], fill=(244, 239, 230, 48), width=2)
    sheet.save(FINAL_DIR / "splash_storyboard.png")

    # 2. 16-Frame Sequence (0.00 s -> 4.00 s, every ~0.266 s) for UI / BootGate reference + contact strip
    seq_count = 16
    seq_frames = []
    seq_states = []
    for i in range(seq_count):
        t_val = 4.0 * (i / (seq_count - 1))
        st = eval_boot_splash_state(t_val)
        seq_states.append(st)
        frm = render_boot_splash_frame(st, out_px=512, ss=2)
        # Stamp subtle timecode in top-left corner of each sequence frame
        df = ImageDraw.Draw(frm)
        f_tc = load_font("IBMPlexMono-Medium.ttf", 14, ss=1)
        df.text((18, 16), f"f{i:02d} · {t_val:0.2f}s", font=f_tc, fill=(226, 168, 86, 190))
        frm.save(frames_dir / f"frame_{i:02d}.png")
        seq_frames.append(frm)

    # Save 4x4 contact sheet (`splash_sequence_strip.png` at 1024x1024)
    strip = Image.new("RGBA", (1024, 1024), BG_NAVY)
    cell = 256
    for idx, frm in enumerate(seq_frames):
        gx = (idx % 4) * cell
        gy = (idx // 4) * cell
        strip.paste(frm.resize((cell, cell), Image.Resampling.LANCZOS), (gx, gy))
    d_st = ImageDraw.Draw(strip)
    for k in range(1, 4):
        d_st.line([(k * cell, 0), (k * cell, 1024)], fill=(244, 239, 230, 38), width=1)
        d_st.line([(0, k * cell), (1024, k * cell)], fill=(244, 239, 230, 38), width=1)
    strip.save(FINAL_DIR / "splash_sequence_strip.png")

    # 3. Export separated transparent UI layers (`layers/`) so BootGate UI can animate them directly if desired
    cx, cy_mark, y_word = 512.0, 404.0, 738.0
    mark_scale = 0.88
    layer_ring = render_canvas_1024(
        None,
        lambda d, s_v: draw_mark_c_final(
            d, cx, cy_mark, mark_scale, INK_ON_DARK, ACCENT_ON_DARK, variant="glint",
            circle_alpha=1.0, line_progress=0.0, glint_alpha=0.0, ss=s_v
        ),
        out_px=1024,
        ss=3,
    )
    layer_ring.save(layers_dir / "boot_layer_ring.png")

    layer_stroke = render_canvas_1024(
        None,
        lambda d, s_v: draw_mark_c_final(
            d, cx, cy_mark, mark_scale, INK_ON_DARK, ACCENT_ON_DARK, variant="glint",
            circle_alpha=0.0, line_progress=1.0, glint_alpha=0.0, ss=s_v
        ),
        out_px=1024,
        ss=3,
    )
    layer_stroke.save(layers_dir / "boot_layer_stroke.png")

    layer_glint = render_canvas_1024(
        None,
        lambda d, s_v: draw_mark_c_final(
            d, cx, cy_mark, mark_scale, INK_ON_DARK, ACCENT_ON_DARK, variant="glint",
            circle_alpha=0.0, line_progress=0.0, glint_scale=1.0, glint_alpha=1.0, ss=s_v
        ),
        out_px=1024,
        ss=3,
    )
    layer_glint.save(layers_dir / "boot_layer_glint.png")

    layer_wordmark = render_canvas_1024(
        None,
        lambda d, s_v: draw_wordmark_outsight_worlds(
            d, cx, y_word, INK_ON_DARK, SUB_ON_NAVY, layout="stacked", ss=s_v
        ),
        out_px=1024,
        ss=3,
    )
    layer_wordmark.save(layers_dir / "boot_layer_wordmark.png")

    # 4. Write machine-readable JSON timing spec + Markdown implementation spec for BootGate
    timing_spec = {
        "studio": "Outsight Worlds",
        "direction": "C (The Leaving Line — Glint Primary)",
        "total_duration_seconds": 4.0,
        "bootgate_background_hex": "#0B0E2A",
        "bootgate_background_rgba": [11, 14, 42, 255],
        "bootgate_integration": {
            "target_class": "MoonProject.UI.BootGate (Assets/_Project/Scripts/UI/Motion/BootGate.cs)",
            "recommended_min_seconds": 4.0,
            "recommended_max_seconds": 5.0,
            "clock": "Time.unscaledDeltaTime (clamped to <= 0.1f per tick so initial shader/scene hitches do not skip the reveal)",
        },
        "phases": [
            {
                "phase": 1,
                "name": "Stroke Draws Out of Ring",
                "t_start": 0.0,
                "t_end": 1.45,
                "ring_alpha": {"t0": 0.10, "t1": 0.95, "from": 0.0, "to": 1.0, "ease": "smoothstep"},
                "stroke_trim_end": {"t0": 0.20, "t1": 1.45, "from": 0.0, "to": 1.0, "ease": "smoothstep"},
                "stroke_alpha": {"t0": 0.15, "t1": 0.65, "from": 0.0, "to": 1.0, "ease": "smoothstep"},
            },
            {
                "phase": 2,
                "name": "4-Point Glint Bloom at Tip",
                "t_start": 1.30,
                "t_end": 1.95,
                "glint_alpha": {"t0": 1.30, "t1": 1.72, "from": 0.0, "to": 1.0, "ease": "smoothstep"},
                "glint_scale_overshoot": {"t0": 1.30, "t1": 1.62, "from": 0.35, "to": 1.12, "ease": "smoothstep"},
                "glint_scale_settle": {"t0": 1.62, "t1": 1.95, "from": 1.12, "to": 1.00, "ease": "smoothstep"},
            },
            {
                "phase": 3,
                "name": "Wordmark Fade & Calm Hold",
                "t_start": 1.50,
                "t_end": 3.15,
                "wordmark_alpha": {"t0": 1.50, "t1": 2.20, "from": 0.0, "to": 1.0, "ease": "smoothstep"},
                "wordmark_offset_y_px": {"t0": 1.50, "t1": 2.20, "from": 8.0, "to": 0.0, "ease": "smoothstep"},
                "still_hold_window": [2.20, 3.15],
            },
            {
                "phase": 4,
                "name": "Dissolve into BootGate Dark",
                "t_start": 3.12,
                "t_end": 4.00,
                "main_dissolve_alpha": {"t0": 3.12, "t1": 3.84, "from": 1.0, "to": 0.0, "ease": "smoothstep"},
                "glint_dissolve_alpha": {"t0": 3.48, "t1": 4.00, "from": 1.0, "to": 0.0, "ease": "smoothstep"},
            },
        ],
        "sampled_frames_16": seq_states,
    }
    (FINAL_DIR / "boot_splash_timing.json").write_text(json.dumps(timing_spec, indent=2), encoding="utf-8")

    spec_md = """# Outsight Worlds — 4.0 s BootGate Splash Timing Specification

## 1. Overview & `BootGate` Integration
- **Studio Identity:** **Outsight Worlds** (`OUTSIGHT` + `WORLDS`) — Direction **C** (*The Leaving Line*, Glint Primary)
- **Target Surface:** Existing `BootGate` dark screen (`#0B0E2A` / `RGBA(11, 14, 42, 255)`, matching `ProjectSettings.asset` `m_SplashScreenBackgroundColor`)
- **Total Duration:** **`4.00 s`** (`minSeconds = 4.0f` on `BootGate`), followed immediately by `BootGate.Tick(unscaledDeltaTime)` releasing the gate once steady frames are confirmed.
- **Frame Hitch Protection:** Accumulate splash time via `Mathf.Min(Time.unscaledDeltaTime, 0.05f)` so early scene-load or shader-warm hitches never skip the stroke draw or glint bloom.

## 2. Timeline & Easing Curves (`0.00 s → 4.00 s`)

All transitions use standard cubic Hermite `SmoothStep(t0, t1, t)` (`u*u*(3 - 2*u)` where `u = Clamp01((t - t0) / (t1 - t0))`):

| Phase | Window | Element | Parameter | Curve / Keyframes | Visual Read |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1 · Stroke Draws Out** | `0.00 s – 1.45 s` | Lower-Right Ring (`boot_layer_ring.png`) | `alpha` | `SmoothStep(0.10, 0.95, t)` (`0% → 100%`) | Ring emerges softly out of `#0B0E2A`. |
| | `0.20 s – 1.45 s` | Leaving Stroke (`boot_layer_stroke.png`) | `trimEnd` / `progress` | `SmoothStep(0.20, 1.45, t)` (`0% → 100%`) | Ribbon traces around top-left, inflects across center, and steps out of the ring at `-36°`. |
| | `0.15 s – 0.65 s` | Leaving Stroke | `alpha` | `SmoothStep(0.15, 0.65, t)` (`0% → 100%`) | Smooth fade-in as the stroke begins tracing. |
| **2 · Outward Glint** | `1.30 s – 1.95 s` | 4-Point Glint (`boot_layer_glint.png`) | `alpha` | `SmoothStep(1.30, 1.72, t)` (`0% → 100%`) | Warm `#E2A856` 4-point star glint ignites at the stroke's outward tip (`u = 229`). |
| | `1.30 s – 1.95 s` | 4-Point Glint | `scale` | `0.35 → 1.12` (`1.30..1.62 s`), then `1.12 → 1.00` (`1.62..1.95 s`) | Subtle, weightless bloom and settle (no flashy lens flare). |
| **3 · Wordmark & Hold** | `1.50 s – 2.20 s` | Wordmark (`boot_layer_wordmark.png`) | `alpha` & `offsetY` | `alpha: 0% → 100%`, `y: +8 px → 0 px` (`SmoothStep(1.50, 2.20, t)`) | `OUTSIGHT` + `WORLDS` rises gently into place below the mark. |
| | `2.20 s – 3.12 s` | Full Lockup | *Hold* | All layers at `100%` for `0.92 s` | Calm, poised stillness before title transition. |
| **4 · Dissolve** | `3.12 s – 3.84 s` | Ring, Stroke, Wordmark | `alpha` | `1.0 - SmoothStep(3.12, 3.84, t)` (`100% → 0%`) | Ring, stroke, and wordmark dissolve into `#0B0E2A`. |
| | `3.48 s – 4.00 s` | 4-Point Glint | `alpha` & `scale` | `alpha: 1.0 - SmoothStep(3.48, 4.00, t)`, `scale: 1.00 → 0.88` | The outward glint lingers brightly past the ring/wordmark, then fades to `0%` as `BootGate` opens. |

## 3. Ready-to-Use Assets in `Logs/studio/final/`
- **4-Frame Storyboard:** `splash_1.png`, `splash_2.png`, `splash_3.png`, `splash_4.png`, and `splash_storyboard.png` (2×2 sheet).
- **16-Frame Sequence (`0.00 s – 4.00 s`):** `frames/frame_00.png` .. `frames/frame_15.png` and `splash_sequence_strip.png` (4×4 sheet).
- **Separated Transparent UI Layers (`1024×1024`):** `layers/boot_layer_ring.png`, `layers/boot_layer_stroke.png`, `layers/boot_layer_glint.png`, `layers/boot_layer_wordmark.png`.
- **JSON Curve Data:** `boot_splash_timing.json`.
"""
    (FINAL_DIR / "BOOT_SPLASH_SPEC.md").write_text(spec_md, encoding="utf-8")
    print("[build_studio_final] Rendered 4-frame storyboard, 16-frame sequence, UI layers, and timing specs.")


def generate_final_html_galleries():
    final_html = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>OUTSIGHT WORLDS — Final Studio Identity (Direction C · Round 2)</title>
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
      max-width: 980px;
    }
    .section-title {
      font-size: 1.12rem;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ochre);
      margin: 36px 0 14px;
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 12px;
    }
    .section-note {
      font-size: 0.86rem;
      color: var(--muted);
      font-weight: 400;
      letter-spacing: 0;
      text-transform: none;
    }
    .grid-2 { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
    .grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; }
    .grid-4 { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; }
    @media (max-width: 1200px) {
      .grid-3, .grid-4 { grid-template-columns: repeat(2, 1fr); }
    }
    .card {
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: 10px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }
    .card.primary {
      border-color: rgba(226, 168, 86, 0.72);
      box-shadow: 0 0 0 1px rgba(226, 168, 86, 0.25);
    }
    .card img.hero {
      width: 100%;
      aspect-ratio: 1 / 1;
      display: block;
      background: #0B0C10;
    }
    .card-body {
      padding: 14px 18px 16px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      flex: 1;
    }
    .card-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }
    .card-title {
      font-size: 0.90rem;
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
    }
    .desc {
      font-size: 0.85rem;
      color: #D8D4CA;
      flex: 1;
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
    .checker {
      background-image:
        linear-gradient(45deg, #181C26 25%, transparent 25%),
        linear-gradient(-45deg, #181C26 25%, transparent 25%),
        linear-gradient(45deg, transparent 75%, #181C26 75%),
        linear-gradient(-45deg, transparent 75%, #181C26 75%);
      background-size: 24px 24px;
      background-position: 0 0, 0 12px, 12px -12px, -12px 0;
      background-color: #10131B;
    }
    .favicon-bar {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 16px 20px;
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: 10px;
      flex-wrap: wrap;
    }
    .favicon-item {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 0.84rem;
      color: var(--ivory);
    }
    .favicon-item img {
      border-radius: 4px;
      border: 1px solid rgba(255,255,255,0.16);
    }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>OUTSIGHT WORLDS — FINALIZED STUDIO IDENTITY (DIRECTION C)</h1>
      <p class="subtitle">
        Round 2 Owner Pick: Studio = <strong>Outsight Worlds</strong> (<code>OUTSIGHT</code> + <code>WORLDS</code>), Direction <strong>C</strong> (a single stroke stepping out of the ring). Includes tip risk comparison (Glint vs. Comet Taper), Full &amp; One-Line Lockups, Mark-Only, Black / Calm Navy (<code>#0B0E2A</code>) / Off-White / 1-Colour / Transparent 2048 px exports, Favicons (64/32), and the 4.0 s <code>BootGate</code> Splash Storyboard &amp; Sequence.
      </p>
    </div>
    <div class="links">
      <a href="../index.html">← Back to Round 1 Concepts Gallery (A · B · C · D)</a> ·
      <a href="BOOT_SPLASH_SPEC.md">BOOT_SPLASH_SPEC.md</a> ·
      <a href="boot_splash_timing.json">boot_splash_timing.json</a>
    </div>
  </header>

  <!-- 1. CIGARETTE RISK FIX: BOTH TIP TREATMENTS -->
  <div class="section-title">
    <span>1 · Tip Refinement Comparison: Fixing the Cigarette Risk (Both Shown · C1 Picked as Primary)</span>
    <span class="section-note">Full comparison sheet: <a href="tip_variants_comparison.png" style="color:var(--ochre)">tip_variants_comparison.png</a></span>
  </div>
  <div class="grid-2">
    <div class="card primary">
      <img class="hero" src="variant_glint_on_navy.png" alt="Variant C1 Glint on Navy" />
      <div class="card-body">
        <div class="card-head">
          <span class="card-title">VARIANT C1 · GENTLE TAPER + TINY 4-POINT GLINT</span>
          <span class="badge">★ PRIMARY PICK</span>
        </div>
        <p class="desc">
          The departing stroke narrows gracefully into a rounded tip (`26 px → 11.6 px`) with zero parallel-cylinder block, culminating in a poised, solid 4-point star glint (`#E2A856`). Reads unmistakably at 32 px, converts cleanly to 1-colour monochrome, and gives the 4.0 s boot splash its signature glint beat.
        </p>
        <div class="links">
          <a href="variant_glint_on_black.png">on_black</a> ·
          <a href="variant_glint_on_navy.png">on_navy (#0B0E2A)</a> ·
          <a href="variant_glint_on_offwhite.png">on_offwhite</a>
        </div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="variant_taper_on_navy.png" alt="Variant C2 Comet Taper on Navy" />
      <div class="card-body">
        <div class="card-head">
          <span class="card-title">VARIANT C2 · COMET-TRAIL FINE TAPER</span>
          <span class="badge">ALTERNATE</span>
        </div>
        <p class="desc">
          The departing stroke follows a smooth parabolic curve (`26 px → 2.9 px` fine apex) like a calm comet trail lifting off the canvas, warming smoothly into ochre-amber along its outer tail.
        </p>
        <div class="links">
          <a href="variant_taper_on_black.png">on_black</a> ·
          <a href="variant_taper_on_navy.png">on_navy (#0B0E2A)</a> ·
          <a href="variant_taper_on_offwhite.png">on_offwhite</a>
        </div>
      </div>
    </div>
  </div>

  <!-- 2. FULL LOCKUP ("OUTSIGHT" LARGE + "WORLDS" SMALL, TRACKED, BELOW) -->
  <div class="section-title">
    <span>2 · Full Stacked Lockup ("OUTSIGHT" + "WORLDS") — Black, Boot Navy (#0B0E2A), Off-White, Transparent 2048 px</span>
  </div>
  <div class="grid-4">
    <div class="card primary">
      <img class="hero" src="lockup_on_black.png" alt="Full Lockup on Black" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">ON BLACK (#0B0C10)</span></div>
        <div class="links"><a href="lockup_on_black.png">lockup_on_black.png</a> · <a href="on_black.png">on_black.png</a></div>
      </div>
    </div>
    <div class="card primary">
      <img class="hero" src="lockup_on_navy.png" alt="Full Lockup on Navy" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">ON BOOT NAVY (#0B0E2A)</span></div>
        <div class="links"><a href="lockup_on_navy.png">lockup_on_navy.png</a> · <a href="on_navy.png">on_navy.png</a></div>
      </div>
    </div>
    <div class="card primary">
      <img class="hero" src="lockup_on_offwhite.png" alt="Full Lockup on Off-White" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">ON OFF-WHITE (#F4EFE6)</span></div>
        <div class="links"><a href="lockup_on_offwhite.png">lockup_on_offwhite.png</a> · <a href="on_offwhite.png">on_offwhite.png</a></div>
      </div>
    </div>
    <div class="card primary">
      <img class="hero checker" src="transparent_2048.png" alt="Transparent 2048 px Full Lockup" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">TRANSPARENT (2048 × 2048 PX)</span></div>
        <div class="links"><a href="transparent_2048.png">transparent_2048.png</a> · <a href="lockup_transparent_2048.png">lockup_transparent_2048.png</a></div>
      </div>
    </div>
  </div>

  <!-- 3. MARK-ONLY EXPORTS -->
  <div class="section-title">
    <span>3 · Mark-Only Exports — Black, Boot Navy (#0B0E2A), Off-White, Transparent 2048 px</span>
  </div>
  <div class="grid-4">
    <div class="card">
      <img class="hero" src="mark_on_black.png" alt="Mark on Black" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">MARK ON BLACK</span></div>
        <div class="links"><a href="mark_on_black.png">mark_on_black.png</a></div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="mark_on_navy.png" alt="Mark on Navy" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">MARK ON BOOT NAVY (#0B0E2A)</span></div>
        <div class="links"><a href="mark_on_navy.png">mark_on_navy.png</a></div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="mark_on_offwhite.png" alt="Mark on Off-White" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">MARK ON OFF-WHITE</span></div>
        <div class="links"><a href="mark_on_offwhite.png">mark_on_offwhite.png</a></div>
      </div>
    </div>
    <div class="card">
      <img class="hero checker" src="mark_transparent_2048.png" alt="Mark Transparent 2048 px" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">MARK TRANSPARENT (2048 PX)</span></div>
        <div class="links"><a href="mark_transparent_2048.png">mark_transparent_2048.png</a></div>
      </div>
    </div>
  </div>

  <!-- 4. ONE-LINE LOCKUPS & 1-COLOUR VERSIONS -->
  <div class="section-title">
    <span>4 · One-Line Lockups &amp; 1-Colour Monochrome Versions</span>
  </div>
  <div class="grid-4">
    <div class="card">
      <img class="hero" src="oneline_on_black.png" alt="Horizontal One-Line Lockup on Black" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">HORIZONTAL ONE-LINE LOCKUP</span></div>
        <div class="links">
          <a href="oneline_on_black.png">on_black</a> ·
          <a href="oneline_on_navy.png">on_navy</a> ·
          <a href="oneline_on_offwhite.png">on_offwhite</a> ·
          <a href="oneline_transparent_2048.png">transparent_2048</a>
        </div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="oneline_stacked_on_black.png" alt="Stacked One-Line Wordmark on Black" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">MARK + ONE-LINE WORDMARK</span></div>
        <div class="links">
          <a href="oneline_stacked_on_black.png">on_black</a> ·
          <a href="oneline_stacked_on_navy.png">on_navy</a> ·
          <a href="oneline_stacked_on_offwhite.png">on_offwhite</a>
        </div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="one_colour_on_black.png" alt="1-Colour on Black" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">1-COLOUR MONOCHROME (DARK)</span></div>
        <div class="links">
          <a href="one_colour_on_black.png">on_black</a> ·
          <a href="one_colour_on_navy.png">on_navy</a> ·
          <a href="one_colour_transparent_2048.png">transparent_2048</a>
        </div>
      </div>
    </div>
    <div class="card">
      <img class="hero" src="one_colour_on_offwhite.png" alt="1-Colour on Off-White" />
      <div class="card-body">
        <div class="card-head"><span class="card-title">1-COLOUR MONOCHROME (LIGHT)</span></div>
        <div class="links">
          <a href="one_colour_on_offwhite.png">one_colour_on_offwhite.png</a>
        </div>
      </div>
    </div>
  </div>

  <!-- 5. FAVICONS 64 & 32 PX -->
  <div class="section-title">
    <span>5 · Favicons (64×64 px &amp; 32×32 px)</span>
  </div>
  <div class="favicon-bar">
    <div class="favicon-item">
      <img src="favicon_64.png" width="64" height="64" alt="64px" />
      <span><strong>64×64</strong> (<a href="favicon_64.png" style="color:var(--ochre)">favicon_64.png</a>)</span>
    </div>
    <div class="favicon-item">
      <img src="favicon_32.png" width="32" height="32" alt="32px" />
      <span><strong>32×32</strong> (<a href="favicon_32.png" style="color:var(--ochre)">favicon_32.png</a>)</span>
    </div>
    <div class="favicon-item">
      <img src="favicon_64_navy.png" width="64" height="64" alt="64px Navy" />
      <span><strong>64×64 Navy</strong> (<a href="favicon_64_navy.png" style="color:var(--ochre)">favicon_64_navy.png</a>)</span>
    </div>
    <div class="favicon-item">
      <img src="favicon_32_navy.png" width="32" height="32" alt="32px Navy" />
      <span><strong>32×32 Navy</strong> (<a href="favicon_32_navy.png" style="color:var(--ochre)">favicon_32_navy.png</a>)</span>
    </div>
    <div class="favicon-item">
      <img class="checker" src="favicon_64_transparent.png" width="64" height="64" alt="64px Transparent" />
      <span><strong>64/32 Transparent</strong> (<a href="favicon_64_transparent.png" style="color:var(--ochre)">64</a> · <a href="favicon_32_transparent.png" style="color:var(--ochre)">32</a>)</span>
    </div>
  </div>

  <!-- 6. 4-SECOND BOOT SPLASH STORYBOARD & 16-FRAME SEQUENCE -->
  <div class="section-title">
    <span>6 · 4.0-Second BootGate Splash Storyboard &amp; 16-Frame Sequence (#0B0E2A)</span>
    <span class="section-note">
      2×2 Storyboard: <a href="splash_storyboard.png" style="color:var(--ochre)">splash_storyboard.png</a> ·
      16-Frame Strip: <a href="splash_sequence_strip.png" style="color:var(--ochre)">splash_sequence_strip.png</a> ·
      Spec: <a href="BOOT_SPLASH_SPEC.md" style="color:var(--ochre)">BOOT_SPLASH_SPEC.md</a>
    </span>
  </div>
  <div class="grid-4">
    <div class="card"><img class="hero" src="splash_1.png" alt="Splash Frame 1" /></div>
    <div class="card"><img class="hero" src="splash_2.png" alt="Splash Frame 2" /></div>
    <div class="card"><img class="hero" src="splash_3.png" alt="Splash Frame 3" /></div>
    <div class="card"><img class="hero" src="splash_4.png" alt="Splash Frame 4" /></div>
  </div>
</body>
</html>
"""
    (FINAL_DIR / "index.html").write_text(final_html, encoding="utf-8")

    # Also update top-level `Logs/studio/index.html` to include a prominent Round 2 Final banner + section at the top
    root_index_path = OUT_ROOT / "index.html"
    if root_index_path.exists():
        orig = root_index_path.read_text(encoding="utf-8")
        banner_marker = "<!-- ROUND 2 FINAL BANNER -->"
        banner_block = """<!-- ROUND 2 FINAL BANNER -->
  <div class="splash-block" style="border-color: rgba(226, 168, 86, 0.75); margin-bottom: 32px;">
    <div class="splash-header">
      <div class="splash-title" style="color: var(--ochre);">★ ROUND 2 OWNER DECISION — FINALIZED: OUTSIGHT WORLDS (DIRECTION C · Logs/studio/final/)</div>
      <div class="links">
        <a href="final/index.html"><strong>Open Full Round 2 Final Gallery (final/index.html) →</strong></a> ·
        <a href="final/tip_variants_comparison.png">Tip Comparison (Glint vs. Taper)</a> ·
        <a href="final/transparent_2048.png">Transparent 2048 px</a> ·
        <a href="final/BOOT_SPLASH_SPEC.md">4.0s BootGate Spec</a>
      </div>
    </div>
    <div class="grid-4">
      <div class="frame-card"><img src="final/lockup_on_black.png" alt="Final Lockup on Black" /></div>
      <div class="frame-card"><img src="final/lockup_on_navy.png" alt="Final Lockup on Navy" /></div>
      <div class="frame-card"><img src="final/lockup_on_offwhite.png" alt="Final Lockup on Off-White" /></div>
      <div class="frame-card"><img src="final/splash_storyboard.png" alt="Final 4.0s Splash Storyboard" /></div>
    </div>
  </div>
  <!-- END ROUND 2 FINAL BANNER -->"""
        if banner_marker not in orig and "</header>" in orig:
            updated = orig.replace("</header>", "</header>\n\n  " + banner_block, 1)
            root_index_path.write_text(updated, encoding="utf-8")

    print("[build_studio_final] Wrote Logs/studio/final/index.html and updated Logs/studio/index.html.")


if __name__ == "__main__":
    render_all_final_exports()
    render_final_boot_splash_and_sequence()
    generate_final_html_galleries()

