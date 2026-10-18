"""
Deterministic generator for the 9 Face Inventory UI icons (256x256 PNG + SVG + Unity .meta files):
  1. icon_metal_plates   (TẤM KIM LOẠI)
  2. icon_wiring_spool   (CUỘN DÂY DẪN)
  3. icon_spare_igniter  (ĐÁNH LỬA DỰ PHÒNG — from igniter_icon_source.png)
  4. icon_ice_core_tube  (ỐNG LÕI BĂNG — from icecore_icon_source.png)
  5. icon_lens           (THẤU KÍNH QUÉT)
  6. icon_cutter         (MŨI CẮT ĐÁ)
  7. icon_empty_socket   (Ổ CẮM TRỐNG)
  8. icon_warning        (LỖI warning glyph)
  9. icon_energy_battery (NĂNG LƯỢNG battery pictogram)

Outputs both clean single-weight vector SVGs and 4x-supersampled SDF anti-aliased 256x256 RGBA PNGs
in Assets/_Project/Art/UI/Face/ with matching #FFE29E amber ink.
"""

import hashlib
import math
from pathlib import Path
import numpy as np
from PIL import Image

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[3]
OUT_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "UI" / "Face"

AMBER_HEX = "#FFE29E"
AMBER_RGB = (255, 226, 158)
STROKE_MAIN = 5.2
STROKE_SUB = 3.8


def rot_pt(pt, center, deg):
    rad = math.radians(deg)
    c, s = math.cos(rad), math.sin(rad)
    dx, dy = pt[0] - center[0], pt[1] - center[1]
    return (center[0] + dx * c - dy * s, center[1] + dx * s + dy * c)


def point_in_poly(pt, poly):
    x, y = pt
    inside = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y):
            x_int = (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1
            if x < x_int:
                inside = not inside
    return inside


def clip_polyline_outside_poly(pts, clip_poly, step=1.0):
    """Subdivide a polyline and split into visible sub-polylines strictly outside clip_poly."""
    dense = []
    for i in range(len(pts) - 1):
        p0, p1 = pts[i], pts[i + 1]
        dist = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
        steps = max(1, int(math.ceil(dist / step)))
        for k in range(steps):
            t = k / steps
            dense.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t))
    dense.append(pts[-1])

    segments = []
    cur = []
    for p in dense:
        if not point_in_poly(p, clip_poly):
            cur.append(p)
        else:
            if len(cur) >= 2:
                segments.append(cur)
            cur = []
    if len(cur) >= 2:
        segments.append(cur)
    return segments


class VectorIconCanvas:
    """Collects 2D vector strokes in [0..256]x[0..256] space and exports both SVG and supersampled SDF PNG."""

    def __init__(self, size=256):
        self.size = size
        self.polylines = []  # list of (pts_list, width, closed)
        self.svg_elements = []

    def add_polyline(self, pts, width=STROKE_MAIN, closed=False):
        if len(pts) < 2:
            return
        clean_pts = [(float(x), float(y)) for x, y in pts]
        self.polylines.append((clean_pts, float(width), bool(closed)))
        d_parts = [f"M {clean_pts[0][0]:.2f} {clean_pts[0][1]:.2f}"]
        for x, y in clean_pts[1:]:
            d_parts.append(f"L {x:.2f} {y:.2f}")
        if closed:
            d_parts.append("Z")
        d_str = " ".join(d_parts)
        self.svg_elements.append(
            f'  <path d="{d_str}" stroke="{AMBER_HEX}" stroke-width="{width:.2f}" '
            f'stroke-linecap="round" stroke-linejoin="round" fill="none" />'
        )

    def add_line(self, p0, p1, width=STROKE_MAIN):
        self.add_polyline([p0, p1], width=width, closed=False)

    def add_circle(self, cx, cy, r, width=STROKE_MAIN, segs=72):
        pts = [
            (cx + r * math.cos(2.0 * math.pi * i / segs), cy + r * math.sin(2.0 * math.pi * i / segs))
            for i in range(segs)
        ]
        self.polylines.append((pts, float(width), True))
        self.svg_elements.append(
            f'  <circle cx="{cx:.2f}" cy="{cy:.2f}" r="{r:.2f}" stroke="{AMBER_HEX}" '
            f'stroke-width="{width:.2f}" fill="none" />'
        )

    def add_arc(self, cx, cy, rx, ry, deg_start, deg_end, rot_deg=0.0, width=STROKE_MAIN, segs=48):
        pts = []
        for i in range(segs + 1):
            t = i / segs
            ang = math.radians(deg_start + (deg_end - deg_start) * t)
            px = cx + rx * math.cos(ang)
            py = cy + ry * math.sin(ang)
            if rot_deg != 0.0:
                px, py = rot_pt((px, py), (cx, cy), rot_deg)
            pts.append((px, py))
        self.add_polyline(pts, width=width, closed=False)

    def add_ellipse(self, cx, cy, rx, ry, rot_deg=0.0, width=STROKE_MAIN, segs=64):
        pts = []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            px = cx + rx * math.cos(ang)
            py = cy + ry * math.sin(ang)
            if rot_deg != 0.0:
                px, py = rot_pt((px, py), (cx, cy), rot_deg)
            pts.append((px, py))
        self.add_polyline(pts, width=width, closed=True)

    def add_chamfer_rect(self, x, y, w, h, c, rot_deg=0.0, center=None, width=STROKE_MAIN):
        c = min(c, w * 0.45, h * 0.45)
        pts = [
            (x + c, y),
            (x + w - c, y),
            (x + w, y + c),
            (x + w, y + h - c),
            (x + w - c, y + h),
            (x + c, y + h),
            (x, y + h - c),
            (x, y + c),
        ]
        if rot_deg != 0.0:
            ctr = center if center is not None else (x + w * 0.5, y + h * 0.5)
            pts = [rot_pt(p, ctr, rot_deg) for p in pts]
        self.add_polyline(pts, width=width, closed=True)
        return pts

    def add_bezier(self, p0, p1, p2, p3, width=STROKE_MAIN, segs=36):
        pts = []
        for i in range(segs + 1):
            t = i / segs
            u = 1.0 - t
            x = (u**3) * p0[0] + 3 * (u**2) * t * p1[0] + 3 * u * (t**2) * p2[0] + (t**3) * p3[0]
            y = (u**3) * p0[1] + 3 * (u**2) * t * p1[1] + 3 * u * (t**2) * p2[1] + (t**3) * p3[1]
            pts.append((x, y))
        self.add_polyline(pts, width=width, closed=False)

    def to_svg(self):
        header = (
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.size}" height="{self.size}" '
            f'viewBox="0 0 {self.size} {self.size}">\n'
        )
        footer = "\n</svg>\n"
        return header + "\n".join(self.svg_elements) + footer

    def render_png(self, out_path, supersample=2):
        """Analytical SDF capsule rasterizer at supersampled resolution, downsampled to 256x256."""
        ss = supersample
        grid_n = self.size * ss
        coords = (np.arange(grid_n, dtype=np.float32) + 0.5) / ss
        gx, gy = np.meshgrid(coords, coords)

        # Accumulate maximum coverage across all strokes via signed distance to each segment
        max_cov = np.zeros((grid_n, grid_n), dtype=np.float32)
        aa_half = 0.65 / ss

        for pts, width, closed in self.polylines:
            half_w = width * 0.5
            seg_pts = list(pts) + ([pts[0]] if closed else [])
            min_dist_sq = np.full((grid_n, grid_n), 1e9, dtype=np.float32)

            for i in range(len(seg_pts) - 1):
                x0, y0 = seg_pts[i]
                x1, y1 = seg_pts[i + 1]
                # Bounding box culling in grid coordinates
                pad = half_w + 2.0
                ix0 = max(0, int((min(x0, x1) - pad) * ss))
                ix1 = min(grid_n, int(math.ceil((max(x0, x1) + pad) * ss)))
                iy0 = max(0, int((min(y0, y1) - pad) * ss))
                iy1 = min(grid_n, int(math.ceil((max(y0, y1) + pad) * ss)))
                if ix0 >= ix1 or iy0 >= iy1:
                    continue

                sub_x = gx[iy0:iy1, ix0:ix1]
                sub_y = gy[iy0:iy1, ix0:ix1]
                vx, vy = x1 - x0, y1 - y0
                l2 = vx * vx + vy * vy
                if l2 < 1e-8:
                    d2 = (sub_x - x0) ** 2 + (sub_y - y0) ** 2
                else:
                    t = np.clip(((sub_x - x0) * vx + (sub_y - y0) * vy) / l2, 0.0, 1.0)
                    px = x0 + t * vx
                    py = y0 + t * vy
                    d2 = (sub_x - px) ** 2 + (sub_y - py) ** 2
                min_dist_sq[iy0:iy1, ix0:ix1] = np.minimum(min_dist_sq[iy0:iy1, ix0:ix1], d2)

            dist = np.sqrt(min_dist_sq)
            cov = np.clip((half_w + aa_half - dist) / (2.0 * aa_half), 0.0, 1.0)
            max_cov = np.maximum(max_cov, cov)

        # Downsample ss x ss blocks to 256 x 256
        cov_256 = max_cov.reshape((self.size, ss, self.size, ss)).mean(axis=(1, 3))
        alpha_u8 = np.clip(np.rint(cov_256 * 255.0), 0, 255).astype(np.uint8)

        rgba = np.empty((self.size, self.size, 4), dtype=np.uint8)
        rgba[:, :, 0] = AMBER_RGB[0]
        rgba[:, :, 1] = AMBER_RGB[1]
        rgba[:, :, 2] = AMBER_RGB[2]
        rgba[:, :, 3] = alpha_u8
        Image.fromarray(rgba, mode="RGBA").save(out_path, optimize=True)


# ---------------------------------------------------------------------------
# Icon 1: icon_metal_plates (TẤM KIM LOẠI — stacked/overlapping metal plates with rivets)
# ---------------------------------------------------------------------------
def build_icon_metal_plates():
    c = VectorIconCanvas()

    # Front plate (lower-left foreground, tilted -6 deg)
    f_ctr = (110.0, 150.0)
    f_pts = c.add_chamfer_rect(34, 92, 152, 114, 16, rot_deg=-6.0, center=f_ctr, width=STROKE_MAIN)

    # 3D bottom/left thickness lip on front plate
    lip_offset = (0.0, 10.0)
    p_bl1 = (f_pts[4][0] + lip_offset[0], f_pts[4][1] + lip_offset[1])
    p_bl2 = (f_pts[5][0] + lip_offset[0], f_pts[5][1] + lip_offset[1])
    p_bl3 = (f_pts[6][0] + lip_offset[0], f_pts[6][1] + lip_offset[1])
    c.add_polyline([f_pts[4], p_bl1, p_bl2, p_bl3, f_pts[6]], width=STROKE_MAIN)
    c.add_line(f_pts[5], p_bl2, width=STROKE_SUB)

    # Inner recessed panel frame on front plate
    c.add_chamfer_rect(52, 110, 116, 78, 8, rot_deg=-6.0, center=f_ctr, width=STROKE_SUB)

    # 4 corner rivets on front plate
    for rx, ry in ((52, 110), (168, 110), (168, 188), (52, 188)):
        # Move slightly toward corners outside inner frame
        cx = 110 + (rx - 110) * 1.10
        cy = 149 + (ry - 149) * 1.12
        rcx, rcy = rot_pt((cx, cy), f_ctr, -6.0)
        c.add_circle(rcx, rcy, 5.2, width=STROKE_SUB)

    # Diagonal technical sheen ticks inside front plate
    for dx, dy, l in ((86, 156, 26), (104, 162, 34)):
        p0 = rot_pt((dx, dy), f_ctr, -6.0)
        p1 = rot_pt((dx + l, dy - l * 0.75), f_ctr, -6.0)
        c.add_line(p0, p1, width=STROKE_SUB)

    # Back plate (upper-right, tilted +8 deg, clipped against front plate polygon + thickness hull)
    clip_hull = [f_pts[7], f_pts[0], f_pts[1], f_pts[2], f_pts[3], f_pts[4], p_bl1, p_bl2, p_bl3, f_pts[6]]
    b_ctr = (148.0, 96.0)
    bx, by, bw, bh, bc = 74, 42, 148, 108, 15
    b_raw = [
        (bx + bc, by),
        (bx + bw - bc, by),
        (bx + bw, by + bc),
        (bx + bw, by + bh - bc),
        (bx + bw - bc, by + bh),
        (bx + bc, by + bh),
        (bx, by + bh - bc),
        (bx, by + bc),
    ]
    b_pts = [rot_pt(p, b_ctr, 8.0) for p in b_raw]
    for seg in clip_polyline_outside_poly(b_pts + [b_pts[0]], clip_hull):
        c.add_polyline(seg, width=STROKE_MAIN)

    # Back plate inner groove (clipped)
    bi_raw = [
        (bx + 18 + 8, by + 18),
        (bx + bw - 18 - 8, by + 18),
        (bx + bw - 18, by + 18 + 8),
        (bx + bw - 18, by + bh - 18 - 8),
        (bx + bw - 18 - 8, by + bh - 18),
        (bx + 18 + 8, by + bh - 18),
        (bx + 18, by + bh - 18 - 8),
        (bx + 18, by + 18 + 8),
    ]
    bi_pts = [rot_pt(p, b_ctr, 8.0) for p in bi_raw]
    for seg in clip_polyline_outside_poly(bi_pts + [bi_pts[0]], clip_hull):
        c.add_polyline(seg, width=STROKE_SUB)

    # Visible rivets on the upper-left, upper-right, and lower-right of back plate
    for rx, ry in ((bx + 16, by + 16), (bx + bw - 16, by + 16), (bx + bw - 16, by + bh - 16)):
        rcx, rcy = rot_pt((rx, ry), b_ctr, 8.0)
        if not point_in_poly((rcx, rcy), clip_hull):
            c.add_circle(rcx, rcy, 5.0, width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 2: icon_wiring_spool (CUỘN DÂY DẪN — spool with wound wire and loose end)
# ---------------------------------------------------------------------------
def build_icon_wiring_spool():
    c = VectorIconCanvas()
    cx = 110.0
    top_y = 64.0
    bot_y = 186.0
    flange_rx = 66.0
    flange_ry = 22.0

    # Top flange upper ellipse
    c.add_ellipse(cx, top_y, flange_rx, flange_ry, width=STROKE_MAIN)
    # Top flange lower rim thickness arc (front half)
    c.add_arc(cx, top_y + 10.0, flange_rx, flange_ry, 0.0, 180.0, width=STROKE_MAIN)
    c.add_line((cx - flange_rx, top_y), (cx - flange_rx, top_y + 10.0), width=STROKE_MAIN)
    c.add_line((cx + flange_rx, top_y), (cx + flange_rx, top_y + 10.0), width=STROKE_MAIN)

    # Central arbor hole & keyway in top flange
    c.add_ellipse(cx, top_y, 20.0, 7.5, width=STROKE_MAIN)
    c.add_ellipse(cx, top_y, 34.0, 12.0, width=STROKE_SUB)
    # 2 flange inspection holes
    c.add_ellipse(cx - 44.0, top_y, 7.0, 3.2, width=STROKE_SUB)
    c.add_ellipse(cx + 44.0, top_y, 7.0, 3.2, width=STROKE_SUB)

    # Wound cable drum between y = 84 and y = 172 (width rx = 46)
    drum_rx = 46.0
    drum_ry = 14.0
    coil_ys = [92.0, 107.0, 122.0, 137.0, 152.0, 167.0]
    # Left & right scalloped/straight drum profile
    c.add_line((cx - drum_rx, top_y + 19.0), (cx - drum_rx, bot_y - 4.0), width=STROKE_MAIN)
    c.add_line((cx + drum_rx, top_y + 19.0), (cx + drum_rx, bot_y - 4.0), width=STROKE_MAIN)

    # Individual wound wire turns (front visible arcs)
    for wy in coil_ys:
        c.add_arc(cx, wy, drum_rx, drum_ry, 4.0, 176.0, width=STROKE_SUB)

    # Bottom flange: front arc & back outer shoulders (outside the drum width +-46)
    # Angle where x = cx +- 46 on ellipse rx=66: cos(theta) = 46/66 -> theta = +-45.8 deg
    c.add_arc(cx, bot_y, flange_rx, flange_ry, -44.0, 224.0, width=STROKE_MAIN)
    # Bottom flange lower thickness rim
    c.add_arc(cx, bot_y + 11.0, flange_rx, flange_ry, 0.0, 180.0, width=STROKE_MAIN)
    c.add_line((cx - flange_rx, bot_y), (cx - flange_rx, bot_y + 11.0), width=STROKE_MAIN)
    c.add_line((cx + flange_rx, bot_y), (cx + flange_rx, bot_y + 11.0), width=STROKE_MAIN)

    # Loose wire tail unwinding from lower coil (cx + drum_rx, 152) into an S-curve with crimped fork lug
    c.add_bezier((cx + drum_rx, 152.0), (196.0, 164.0), (226.0, 198.0), (222.0, 144.0), width=STROKE_MAIN)
    c.add_bezier((222.0, 144.0), (218.0, 102.0), (196.0, 114.0), (204.0, 80.0), width=STROKE_MAIN)
    # Crimped terminal sleeve + 2 fork prongs at (204, 80)
    c.add_chamfer_rect(196, 66, 16, 16, 3, rot_deg=12.0, center=(204, 74), width=STROKE_SUB)
    c.add_polyline([(199, 65), (198, 50), (203, 46)], width=STROKE_SUB)
    c.add_polyline([(211, 67), (214, 52), (210, 47)], width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 3: icon_spare_igniter (ĐÁNH LỬA DỰ PHÒNG — from igniter_icon_source.png)
# ---------------------------------------------------------------------------
def build_icon_spare_igniter():
    c = VectorIconCanvas()
    # Diagonal axis at -34 deg (from bottom-left (48, 174) to top-right (206, 67))
    origin = (124.0, 122.0)
    angle = -34.0

    def tr(lx, ly):
        return rot_pt((origin[0] + lx, origin[1] + ly), origin, angle)

    def add_local_box(lx0, lx1, half_h, ch=4.0, w=STROKE_MAIN):
        pts = [
            tr(lx0 + ch, -half_h),
            tr(lx1 - ch, -half_h),
            tr(lx1, -half_h + ch),
            tr(lx1, half_h - ch),
            tr(lx1 - ch, half_h),
            tr(lx0 + ch, half_h),
            tr(lx0, half_h - ch),
            tr(lx0, -half_h + ch),
        ]
        c.add_polyline(pts, width=w, closed=True)

    # 1. Rear end-cap & finned heatsink barrel (lx = -94 .. -50)
    add_local_box(-94, -82, 12, ch=3.0, w=STROKE_MAIN)
    # 3 radial cooling fins (clean non-overlapping profile)
    for fx in (-80, -67, -54):
        add_local_box(fx, fx + 9, 22, ch=2.5, w=STROKE_MAIN)

    # 2. Hanging hazard/serial tag on lower-left heatsink (matches igniter_icon_source.png)
    ring_p = tr(-74, 22)
    c.add_circle(ring_p[0], ring_p[1] + 5, 5.0, width=STROKE_SUB)
    c.add_chamfer_rect(38, 186, 40, 26, 4, rot_deg=14.0, center=(58, 199), width=STROKE_SUB)
    c.add_line((46, 196), (68, 201), width=STROKE_SUB)

    # 3. 3-shed ribbed ceramic high-voltage insulator (lx = -43 .. -8)
    for sx in (-43, -30, -17):
        add_local_box(sx, sx + 9, 17, ch=3.5, w=STROKE_MAIN)

    # 4. Central machined hex gland collar (lx = -6 .. +24, half_h = 27)
    add_local_box(-6, 24, 27, ch=5.0, w=STROKE_MAIN)
    # Hex flats lines + central locking cross-pin circle
    c.add_line(tr(-4, -12), tr(22, -12), width=STROKE_SUB)
    c.add_line(tr(-4, 12), tr(22, 12), width=STROKE_SUB)
    pin_c = tr(9, 0)
    c.add_circle(pin_c[0], pin_c[1], 5.5, width=STROKE_SUB)

    # 5. Stepped brass nozzle barrel (lx = +24 .. +64)
    add_local_box(24, 46, 18, ch=3.0, w=STROKE_MAIN)
    add_local_box(46, 64, 13, ch=2.5, w=STROKE_MAIN)

    # 6. Central spark electrode needle & two curved L-shaped ground prongs (lx = +64 .. +92)
    c.add_line(tr(64, 0), tr(82, 0), width=STROKE_MAIN)
    # Upper & lower side ground electrodes
    c.add_polyline([tr(64, -10), tr(88, -12), tr(92, -5)], width=STROKE_MAIN)
    c.add_polyline([tr(64, 10), tr(88, 12), tr(92, 5)], width=STROKE_MAIN)
    # Crisp spark burst rays off the tip
    c.add_line(tr(96, 0), tr(108, 0), width=STROKE_SUB)
    c.add_line(tr(94, -12), tr(104, -20), width=STROKE_SUB)
    c.add_line(tr(94, 12), tr(104, 20), width=STROKE_SUB)

    # 7. Coiled braided copper lead wire looping under from hex collar bottom to 2-pin connector plug
    lead_start = tr(10, 27)
    c.add_bezier(lead_start, (132, 206), (192, 224), (206, 174), width=STROKE_MAIN)
    # Identification label sleeve clipped onto the wire loop
    c.add_chamfer_rect(144, 194, 30, 16, 3, rot_deg=-10.0, center=(159, 202), width=STROKE_SUB)
    # 2-pin cylindrical connector plug at (206, 154) pointing up-left
    plug_ctr = (204.0, 152.0)
    c.add_chamfer_rect(190, 136, 28, 34, 5, rot_deg=-24.0, center=plug_ctr, width=STROKE_MAIN)
    # Two contact pins protruding from top of plug
    p_pin1_0 = rot_pt((198, 136), plug_ctr, -24.0)
    p_pin1_1 = rot_pt((198, 122), plug_ctr, -24.0)
    p_pin2_0 = rot_pt((210, 136), plug_ctr, -24.0)
    p_pin2_1 = rot_pt((210, 122), plug_ctr, -24.0)
    c.add_line(p_pin1_0, p_pin1_1, width=STROKE_SUB)
    c.add_line(p_pin2_0, p_pin2_1, width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 4: icon_ice_core_tube (ỐNG LÕI BĂNG — from icecore_icon_source.png)
# ---------------------------------------------------------------------------
def build_icon_ice_core_tube():
    c = VectorIconCanvas()
    # Oriented at +15 deg (left end slightly higher, right end slightly lower, matching icecore_icon_source.png)
    origin = (128.0, 132.0)
    angle = 15.0

    def tr(lx, ly):
        return rot_pt((origin[0] + lx, origin[1] + ly), origin, angle)

    def add_local_box(lx0, lx1, ly0, ly1, ch=4.0, w=STROKE_MAIN):
        pts = [
            tr(lx0 + ch, ly0),
            tr(lx1 - ch, ly0),
            tr(lx1, ly0 + ch),
            tr(lx1, ly1 - ch),
            tr(lx1 - ch, ly1),
            tr(lx0 + ch, ly1),
            tr(lx0, ly1 - ch),
            tr(lx0, ly0 + ch),
        ]
        c.add_polyline(pts, width=w, closed=True)

    # 1. Left stepped cryo end-cap + pressure knob (lx = -102 .. -68)
    add_local_box(-102, -90, -14, 14, ch=3.0, w=STROKE_MAIN)
    add_local_box(-90, -68, -28, 28, ch=5.0, w=STROKE_MAIN)
    c.add_line(tr(-78, -26), tr(-78, 26), width=STROKE_SUB)

    # 2. Left & Right heavy copper containment bands (lx = -68..-52 and +42..+58)
    add_local_box(-68, -52, -33, 33, ch=3.5, w=STROKE_MAIN)
    add_local_box(42, 58, -33, 33, ch=3.5, w=STROKE_MAIN)

    # 3. Top tubular carry handle spanning between the two containment bands (ly = -33 .. -54)
    c.add_polyline([tr(-60, -33), tr(-54, -52), tr(44, -52), tr(50, -33)], width=STROKE_MAIN)
    c.add_polyline([tr(-48, -33), tr(-44, -43), tr(34, -43), tr(38, -33)], width=STROKE_SUB)

    # 4. Central cylinder body (lx = -52 .. +42, ly = -29 .. +29)
    c.add_line(tr(-52, -29), tr(42, -29), width=STROKE_MAIN)
    c.add_line(tr(-52, 29), tr(42, 29), width=STROKE_MAIN)

    # 5. Armored sight-glass viewport window framing 3 segmented ice core cylinders (lx = -42 .. +32, ly = -16 .. +12)
    add_local_box(-42, 32, -16, 12, ch=6.0, w=STROKE_MAIN)
    # 3 segmented ice core cylinders + crystalline facet lines inside window
    c.add_line(tr(-18, -15), tr(-18, 11), width=STROKE_SUB)
    c.add_line(tr(8, -15), tr(8, 11), width=STROKE_SUB)
    c.add_line(tr(-32, -4), tr(-22, 5), width=STROKE_SUB)
    c.add_line(tr(-10, 4), tr(2, -6), width=STROKE_SUB)
    c.add_line(tr(14, -5), tr(24, 4), width=STROKE_SUB)

    # 6. Lower identification plaque ("CORE 41 · H2O" plate at ly = +17 .. +29)
    add_local_box(-26, 16, 18, 29, ch=2.0, w=STROKE_SUB)

    # 7. Right ribbed pressure canister section & stepped brass end-cap (lx = +58 .. +102)
    add_local_box(58, 84, -28, 28, ch=4.0, w=STROKE_MAIN)
    c.add_line(tr(67, -27), tr(67, 27), width=STROKE_SUB)
    c.add_line(tr(76, -27), tr(76, 27), width=STROKE_SUB)
    add_local_box(84, 96, -22, 22, ch=3.5, w=STROKE_MAIN)
    add_local_box(96, 104, -11, 11, ch=2.5, w=STROKE_MAIN)

    return c


# ---------------------------------------------------------------------------
# Icon 5: icon_lens (THẤU KÍNH QUÉT — scan/viewfinder lens icon)
# ---------------------------------------------------------------------------
def build_icon_lens():
    c = VectorIconCanvas()
    cx, cy = 128.0, 128.0

    # Outer circular brass bezel housing (r = 84)
    c.add_circle(cx, cy, 84.0, width=STROKE_MAIN)

    # 4 diagonal mounting flange ears at 45, 135, 225, 315 deg with bolt holes
    for deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(deg)
        ex = cx + 92.0 * math.cos(rad)
        ey = cy + 92.0 * math.sin(rad)
        c.add_arc(cx, cy, 98.0, 98.0, deg - 12.0, deg + 12.0, width=STROKE_MAIN)
        p_l0 = (cx + 84.0 * math.cos(math.radians(deg - 12.0)), cy + 84.0 * math.sin(math.radians(deg - 12.0)))
        p_l1 = (cx + 98.0 * math.cos(math.radians(deg - 12.0)), cy + 98.0 * math.sin(math.radians(deg - 12.0)))
        p_r0 = (cx + 84.0 * math.cos(math.radians(deg + 12.0)), cy + 84.0 * math.sin(math.radians(deg + 12.0)))
        p_r1 = (cx + 98.0 * math.cos(math.radians(deg + 12.0)), cy + 98.0 * math.sin(math.radians(deg + 12.0)))
        c.add_line(p_l0, p_l1, width=STROKE_MAIN)
        c.add_line(p_r0, p_r1, width=STROKE_MAIN)
        c.add_circle(ex, ey, 3.8, width=STROKE_SUB)

    # Middle calibrated bayonet ring (r = 64)
    c.add_circle(cx, cy, 64.0, width=STROKE_MAIN)

    # Inner optical glass aperture ring (r = 44)
    c.add_circle(cx, cy, 44.0, width=STROKE_MAIN)

    # Curved lens specular highlight arc inside upper-left of optical glass
    c.add_arc(cx, cy, 32.0, 32.0, 200.0, 255.0, width=STROKE_SUB)

    # Scan crosshair reticle ticks (cardinal ticks bridging r=44..64 and inner target circle r=12)
    for deg in (0.0, 90.0, 180.0, 270.0):
        rad = math.radians(deg)
        c.add_line(
            (cx + 44.0 * math.cos(rad), cy + 44.0 * math.sin(rad)),
            (cx + 64.0 * math.cos(rad), cy + 64.0 * math.sin(rad)),
            width=STROKE_SUB,
        )
        c.add_line(
            (cx + 18.0 * math.cos(rad), cy + 18.0 * math.sin(rad)),
            (cx + 30.0 * math.cos(rad), cy + 30.0 * math.sin(rad)),
            width=STROKE_SUB,
        )
    c.add_circle(cx, cy, 10.0, width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 6: icon_cutter (MŨI CẮT ĐÁ — cutter nozzle/turret icon)
# ---------------------------------------------------------------------------
def build_icon_cutter():
    c = VectorIconCanvas()

    # Rear turret mounting flange plate (left, x = 28..48, y = 64..192)
    c.add_chamfer_rect(28, 64, 20, 128, 4, width=STROKE_MAIN)
    c.add_circle(38, 82, 3.5, width=STROKE_SUB)
    c.add_circle(38, 174, 3.5, width=STROKE_SUB)

    # Main turret barrel housing (x = 48..112, y = 76..180)
    c.add_chamfer_rect(48, 76, 64, 104, 6, width=STROKE_MAIN)
    # 3 horizontal cooling louver slots on barrel
    for ly in (98, 128, 158):
        c.add_line((62, ly), (98, ly), width=STROKE_SUB)

    # Stepped brass collar ring (x = 112..130, y = 84..172)
    c.add_chamfer_rect(112, 84, 18, 88, 4, width=STROKE_MAIN)

    # Conical 6-blade iris nozzle head (x = 130..174, tapering from y = 92..164 to y = 110..146)
    c.add_polyline([(130, 92), (174, 110), (174, 146), (130, 164)], width=STROKE_MAIN, closed=True)
    # Iris blade seam lines inside cone
    c.add_line((130, 114), (174, 122), width=STROKE_SUB)
    c.add_line((130, 142), (174, 134), width=STROKE_SUB)

    # Emitter nozzle tip ring (x = 174..184, y = 116..140)
    c.add_chamfer_rect(174, 116, 10, 24, 2, width=STROKE_MAIN)

    # Focused plasma cutting beam & impact target arcs on right (x = 188..230)
    c.add_line((188, 128), (226, 128), width=STROKE_MAIN)
    c.add_line((192, 114), (220, 92), width=STROKE_SUB)
    c.add_line((192, 142), (220, 164), width=STROKE_SUB)
    c.add_line((196, 102), (208, 76), width=STROKE_SUB)
    c.add_line((196, 154), (208, 180), width=STROKE_SUB)
    c.add_arc(216, 128, 14, 34, -55.0, 55.0, width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 7: icon_empty_socket (Ổ CẮM TRỐNG — empty equipment socket icon)
# ---------------------------------------------------------------------------
def build_icon_empty_socket():
    c = VectorIconCanvas()
    cx, cy = 128.0, 128.0

    # Outer chamfered octagonal mounting flange plate
    c.add_chamfer_rect(34, 34, 188, 188, 30, width=STROKE_MAIN)
    # Recessed inner bevel border
    c.add_chamfer_rect(50, 50, 156, 156, 22, width=STROKE_SUB)

    # 4 corner mounting bolt heads
    for bx, by in ((68, 68), (188, 68), (188, 188), (68, 188)):
        c.add_circle(bx, by, 6.5, width=STROKE_SUB)

    # Central circular bayonet receptacle collar (r = 52) with 4 cardinal locking keyways
    for start_deg in (12.0, 102.0, 192.0, 282.0):
        c.add_arc(cx, cy, 52.0, 52.0, start_deg, start_deg + 66.0, width=STROKE_MAIN)
        # Keyway notch tabs at each gap
        notch_deg = start_deg - 12.0
        for d in (notch_deg - 10.0, notch_deg + 10.0):
            r_in, r_out = 52.0, 60.0
            rad = math.radians(d)
            c.add_line(
                (cx + r_in * math.cos(rad), cy + r_in * math.sin(rad)),
                (cx + r_out * math.cos(rad), cy + r_out * math.sin(rad)),
                width=STROKE_MAIN,
            )
        c.add_arc(cx, cy, 60.0, 60.0, notch_deg - 10.0, notch_deg + 10.0, width=STROKE_MAIN)

    # Inner dashed empty-port ring (8 arcs at r = 32)
    for i in range(8):
        a0 = i * 45.0 + 8.0
        c.add_arc(cx, cy, 32.0, 32.0, a0, a0 + 26.0, width=STROKE_SUB)

    # Center alignment cross
    c.add_line((cx - 10.0, cy), (cx + 10.0, cy), width=STROKE_SUB)
    c.add_line((cx, cy - 10.0), (cx, cy + 10.0), width=STROKE_SUB)

    return c


# ---------------------------------------------------------------------------
# Icon 8: icon_warning (warning glyph for the LỖI badge)
# ---------------------------------------------------------------------------
def build_icon_warning():
    c = VectorIconCanvas()

    # Chamfered outer warning triangle
    pts_outer = [
        (120, 34),
        (136, 34),
        (228, 196),
        (218, 212),
        (38, 212),
        (28, 196),
    ]
    c.add_polyline(pts_outer, width=STROKE_MAIN, closed=True)

    # Inner technical border triangle
    pts_inner = [
        (123, 56),
        (133, 56),
        (206, 186),
        (200, 196),
        (56, 196),
        (50, 186),
    ]
    c.add_polyline(pts_inner, width=STROKE_SUB, closed=True)

    # Bold central exclamation mark (!)
    # Tapered upper bar
    c.add_polyline([(121, 90), (135, 90), (132, 150), (124, 150)], width=STROKE_MAIN, closed=True)
    # Lower dot
    c.add_circle(128, 174, 7.0, width=STROKE_MAIN)

    return c


# ---------------------------------------------------------------------------
# Icon 9: icon_energy_battery (energy/battery pictogram)
# ---------------------------------------------------------------------------
def build_icon_energy_battery():
    c = VectorIconCanvas()

    # Two top battery terminal posts with collar caps
    c.add_chamfer_rect(72, 36, 32, 26, 4, width=STROKE_MAIN)
    c.add_chamfer_rect(152, 36, 32, 26, 4, width=STROKE_MAIN)

    # Main battery casing (x = 46..210, y = 62..218)
    c.add_chamfer_rect(46, 62, 164, 156, 14, width=STROKE_MAIN)
    # Reinforced top manifold seam line
    c.add_line((46, 86), (210, 86), width=STROKE_MAIN)

    # Side recessed grip ribs
    for gy in (112, 148, 184):
        c.add_line((46, gy), (58, gy), width=STROKE_SUB)
        c.add_line((198, gy), (210, gy), width=STROKE_SUB)

    # Central high-voltage lightning bolt glyph inside battery body
    bolt_pts = [
        (138, 98),
        (98, 152),
        (128, 152),
        (116, 200),
        (160, 142),
        (130, 142),
    ]
    c.add_polyline(bolt_pts, width=STROKE_MAIN, closed=True)

    return c


# ---------------------------------------------------------------------------
# Unity .meta helpers (matching render_face_inventory_assets.py)
# ---------------------------------------------------------------------------
def deterministic_guid(rel_posix_path):
    return hashlib.md5(("MoonProject.FaceInventoryArt:" + rel_posix_path).encode("utf-8")).hexdigest()


def write_svg_meta(svg_path, rel_posix_path):
    guid = deterministic_guid(rel_posix_path)
    meta_text = (
        f"fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        f"DefaultImporter:\n"
        f"  externalObjects: {{}}\n"
        f"  userData: \n"
        f"  assetBundleName: \n"
        f"  assetBundleVariant: \n"
    )
    Path(str(svg_path) + ".meta").write_text(meta_text, encoding="utf-8", newline="\n")


def write_png_meta(png_path, rel_posix_path, max_size=256):
    guid = deterministic_guid(rel_posix_path)
    meta_text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: -0.25
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    Path(str(png_path) + ".meta").write_text(meta_text, encoding="utf-8", newline="\n")


ICON_BUILDERS = (
    ("icon_metal_plates", build_icon_metal_plates),
    ("icon_wiring_spool", build_icon_wiring_spool),
    ("icon_spare_igniter", build_icon_spare_igniter),
    ("icon_ice_core_tube", build_icon_ice_core_tube),
    ("icon_lens", build_icon_lens),
    ("icon_cutter", build_icon_cutter),
    ("icon_empty_socket", build_icon_empty_socket),
    ("icon_warning", build_icon_warning),
    ("icon_energy_battery", build_icon_energy_battery),
)

ICON_LABELS = {
    "icon_metal_plates": "TẤM KIM LOẠI (Metal Plates)",
    "icon_wiring_spool": "CUỘN DÂY ĐIỆN (Wiring Spool)",
    "icon_spare_igniter": "BỘ ĐÁNH LỬA DỰ PHÒNG (Spare Igniter)",
    "icon_ice_core_tube": "ỐNG LÕI BĂNG (Ice Core Tube)",
    "icon_lens": "THẤU KÍNH QUÉT (Scan Lens)",
    "icon_cutter": "MŨI CẮT ĐÁ (Rock Cutter)",
    "icon_empty_socket": "Ổ CẮM TRỐNG (Empty Socket)",
    "icon_warning": "LỖI / CẢNH BÁO (Warning)",
    "icon_energy_battery": "NĂNG LƯỢNG / PIN (Energy Battery)",
}


def build_contact_sheets():
    import json
    from PIL import Image, ImageDraw, ImageFont

    logs_dir = REPO_ROOT / "Logs"
    logs_dir.mkdir(parents=True, exist_ok=True)
    bg_dark = (13, 12, 10, 255)  # #0D0C0A
    border_col = (106, 90, 58, 255)
    amber_col = (255, 226, 158, 255)
    muted_col = (195, 168, 112, 255)
    red_col = (255, 92, 68, 255)

    try:
        font_title = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 16)
        font_card = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 14)
        font_sub = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 12)
    except Exception:
        font_title = ImageFont.load_default()
        font_card = ImageFont.load_default()
        font_sub = ImageFont.load_default()

    # 1. Icons-only comparison sheet (3x3 grid + 64x64 preview + 3D source comparison)
    sheet_w, sheet_h = 1360, 1160
    sheet = Image.new("RGBA", (sheet_w, sheet_h), bg_dark)
    draw = ImageDraw.Draw(sheet)

    draw.text(
        (32, 20),
        "07 FACE INVENTORY - 9 TECHNICAL LINE-ART ICONS (256x256 + 64x64 IN-PANEL PREVIEW)",
        fill=amber_col,
        font=font_title,
    )

    for idx, (name, _) in enumerate(ICON_BUILDERS):
        col = idx % 3
        row = idx // 3
        cell_x = 32 + col * 440
        cell_y = 60 + row * 350

        # Card frame
        draw.rectangle([cell_x, cell_y, cell_x + 416, cell_y + 326], outline=border_col, width=1)
        draw.text((cell_x + 14, cell_y + 10), f"{idx + 1}. {name}", fill=amber_col, font=font_card)
        draw.text((cell_x + 14, cell_y + 28), ICON_LABELS.get(name, ""), fill=muted_col, font=font_sub)

        icon_img = Image.open(OUT_DIR / f"{name}.png").convert("RGBA")

        # 256x256 main preview box
        box256_x, box256_y = cell_x + 14, cell_y + 54
        draw.rectangle([box256_x - 1, box256_y - 1, box256_x + 256, box256_y + 256], outline=(58, 49, 32, 255), width=1)
        sheet.alpha_composite(icon_img, (box256_x, box256_y))

        # 64x64 in-panel slot preview box
        icon_64 = icon_img.resize((64, 64), Image.Resampling.LANCZOS)
        box64_x, box64_y = cell_x + 292, cell_y + 54
        draw.text((box64_x, box64_y - 16), "64x64 HUD", fill=muted_col, font=font_sub)
        draw.rectangle([box64_x - 4, box64_y - 4, box64_x + 68, box64_y + 68], outline=border_col, width=1)
        sheet.alpha_composite(icon_64, (box64_x, box64_y))

        # 36x36 mini badge preview box
        icon_36 = icon_img.resize((36, 36), Image.Resampling.LANCZOS)
        box36_x, box36_y = cell_x + 292, cell_y + 152
        draw.text((box36_x, box36_y - 16), "36x36", fill=muted_col, font=font_sub)
        draw.rectangle([box36_x - 3, box36_y - 3, box36_x + 39, box36_y + 39], outline=border_col, width=1)
        sheet.alpha_composite(icon_36, (box36_x, box36_y))

        # If this is igniter or ice_core_tube, also show the 3D source render thumbnail in bottom-right of card
        src_path = None
        if name == "icon_spare_igniter":
            src_path = Path("D:/Project/MoonProject-wt/agy-props2/Logs/renders/igniter_icon_source.png")
        elif name == "icon_ice_core_tube":
            src_path = Path("D:/Project/MoonProject-wt/agy-icecore/Logs/renders/icecore_icon_source.png")
        if src_path and src_path.exists():
            src_img = Image.open(src_path).convert("RGBA").resize((96, 96), Image.Resampling.LANCZOS)
            sx, sy = cell_x + 292, cell_y + 214
            draw.text((sx, sy - 14), "3D Source", fill=muted_col, font=font_sub)
            draw.rectangle([sx - 1, sy - 1, sx + 96, sy + 96], outline=(58, 49, 32, 255), width=1)
            sheet.alpha_composite(src_img, (sx, sy))

    icons_sheet_path = logs_dir / "face_inventory_icons_sheet.png"
    sheet.save(icons_sheet_path)
    print(f"Saved {icons_sheet_path}")

    # 2. Full Face Inventory Assets Contact Sheet (Rover 3/4 + Rover Side + Anchors + 9 Icons)
    full_w, full_h = 2048, 1760
    full_sheet = Image.new("RGBA", (full_w, full_h), bg_dark)
    fdraw = ImageDraw.Draw(full_sheet)

    fdraw.text(
        (36, 20),
        "07 FACE INVENTORY ART ASSETS - ROVER TECHNICAL LINE DRAWINGS (2048x2048) + CALLOUT ANCHORS + 9 ITEM/DEVICE ICONS (256x256)",
        fill=amber_col,
        font=font_title,
    )

    anchors_path = OUT_DIR / "rover_07_anchors.json"
    anchors_data = json.loads(anchors_path.read_text(encoding="utf-8")) if anchors_path.exists() else {}

    callout_offsets = {
        "lens": (-180, -56),
        "cutter_nozzle": (-195, -44),
        "socket_1": (74, -52),
        "socket_2": (76, 48),
        "antenna": (68, -28),
    }

    rover_views = [
        ("rover_07_line_3q.png", "rover_07_line_3q.png (2048x2048 RGBA - Primary 3/4 Technical Isometric View)"),
        ("rover_07_line_side.png", "rover_07_line_side.png (2048x2048 RGBA - Left Side Orthographic Elevation)"),
    ]
    for v_idx, (v_file, v_title) in enumerate(rover_views):
        vx = 36 + v_idx * 996
        vy = 56
        vw, vh = 960, 880
        fdraw.rectangle([vx, vy, vx + vw, vy + vh], outline=border_col, width=1)
        fdraw.text((vx + 16, vy + 12), v_title, fill=amber_col, font=font_card)

        v_path = OUT_DIR / v_file
        if v_path.exists():
            r_img = Image.open(v_path).convert("RGBA")
            r_thumb = r_img.resize((820, 820), Image.Resampling.LANCZOS)
            rx, ry = vx + 70, vy + 44
            full_sheet.alpha_composite(r_thumb, (rx, ry))

            # Overlay callout anchors from JSON (top-level keys rover_07_line_3q and rover_07_line_side)
            v_key = Path(v_file).stem
            v_anchors = anchors_data.get(v_key, {})
            for a_name, a_coord in v_anchors.items():
                ax = rx + a_coord["u"] * 820.0
                ay = ry + a_coord["v"] * 820.0
                dx, dy = callout_offsets.get(a_name, (40, -24))
                lx, ly = ax + dx, ay + dy
                fdraw.line([(ax, ay), (lx, ly)], fill=red_col, width=2)
                fdraw.ellipse([ax - 5, ay - 5, ax + 5, ay + 5], fill=red_col, outline=amber_col, width=1)
                label_txt = f"{a_name} ({a_coord['u']:.3f}, {a_coord['v']:.3f})"
                fdraw.rectangle([lx - 4, ly - 10, lx + len(label_txt) * 7 + 6, ly + 10], fill=(22, 18, 14, 235), outline=red_col, width=1)
                fdraw.text((lx + 2, ly - 7), label_txt, fill=amber_col, font=font_sub)

    # Bottom section: 9 icons in a 5 + 4 grid
    fdraw.text(
        (36, 956),
        "DELIVERABLE 2 - 9 ITEM & DEVICE ICONS (256x256 PNG + SVG, #FFE29E TECHNICAL LINE WORK)",
        fill=amber_col,
        font=font_title,
    )
    for idx, (name, _) in enumerate(ICON_BUILDERS):
        col = idx % 5
        row = idx // 5
        cx = 36 + col * 398
        cy = 990 + row * 365
        fdraw.rectangle([cx, cy, cx + 382, cy + 345], outline=border_col, width=1)
        fdraw.text((cx + 12, cy + 10), f"{name} (.png + .svg)", fill=amber_col, font=font_card)
        fdraw.text((cx + 12, cy + 28), ICON_LABELS.get(name, ""), fill=muted_col, font=font_sub)

        icon_img = Image.open(OUT_DIR / f"{name}.png").convert("RGBA")
        full_sheet.alpha_composite(icon_img, (cx + 14, cy + 56))

        icon_64 = icon_img.resize((64, 64), Image.Resampling.LANCZOS)
        bx, by = cx + 294, cy + 72
        fdraw.text((bx, by - 16), "64x64", fill=muted_col, font=font_sub)
        fdraw.rectangle([bx - 3, by - 3, bx + 67, by + 67], outline=border_col, width=1)
        full_sheet.alpha_composite(icon_64, (bx, by))

    full_sheet_path = logs_dir / "face_inventory_assets_sheet.png"
    full_sheet.save(full_sheet_path)
    print(f"Saved {full_sheet_path}")


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name, fn in ICON_BUILDERS:
        canvas = fn()
        svg_path = OUT_DIR / f"{name}.svg"
        png_path = OUT_DIR / f"{name}.png"
        svg_path.write_text(canvas.to_svg(), encoding="utf-8", newline="\n")
        canvas.render_png(png_path, supersample=2)
        write_svg_meta(svg_path, f"Assets/_Project/Art/UI/Face/{name}.svg")
        write_png_meta(png_path, f"Assets/_Project/Art/UI/Face/{name}.png", max_size=256)
        print(f"Generated {name}.svg and {name}.png")
    build_contact_sheets()


if __name__ == "__main__":
    main()

