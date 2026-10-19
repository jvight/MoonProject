"""
Deterministic generator for Rover 07's 9 Face Inventory UI icons (256x256 RGBA PNG + SVG):
  1. icon_metal_plates   (2-3 stacked plates with a bolt)
  2. icon_wiring_spool   (flanged drum with cable wrapped)
  3. icon_spare_igniter  (spark plug)
  4. icon_cutter         (nozzle tool with a short beam)
  5. icon_lens           (camera lens barrel seen 3/4)
  6. icon_empty_socket   (empty square bay with 4 corner brackets)
  7. icon_energy_battery (cell with terminal)
  8. icon_ice_core_tube  (capped tube with frost)
  9. icon_warning        (triangle with !)

Rules (Logs/TASK.md):
  - Silhouette first: bold filled shape whose outline alone identifies the object at 32-40 px.
  - Two tones only: solid body in white (#FFFFFF, 100% alpha) + at most 1-2 negative-space cut-outs
    and/or a second tone at 45% alpha (#FFFFFF at 45% alpha) for depth.
  - No strokes or gaps thinner than 16 px at 256 (6% of size). No hairlines, no concentric ring stacks,
    no hatching, no clusters of dots/holes.
  - Same optical size and weight across the set; 16 px padding; centred on the visual mass.
  - Proof sheet: Logs/captures/icons_sheet.png showing every icon at 256, 64, 40 and 32 px on dark CRT
    background (#1b1a17) tinted amber (#E8C27A), old vs new side by side.
"""

import hashlib
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[3]
OUT_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "UI" / "Face"
OLD_BACKUP_DIR = Path("C:/Users/jvigh/.gemini/antigravity-cli/brain/3b422e2c-750d-4b3d-b68d-f9aa733d9a6a/scratch/old_icons")

CANVAS_SIZE = 256
PADDING = 16.0
SECOND_TONE_ALPHA = 0.45

# Stroke weights stored on the <path> elements inside <g fill="#FFFFFF" ...> so that
# both standard SVG viewers (which render the <g> fills with stroke="none") and Unity's
# FaceArtReader.cs (which inspects stroke-width on <path>) parse cleanly.
SVG_PRIMARY_STROKE = 11.5
SVG_DETAIL_STROKE = 4.0


def rot_pt(pt, center, deg):
    rad = math.radians(deg)
    c, s = math.cos(rad), math.sin(rad)
    dx, dy = pt[0] - center[0], pt[1] - center[1]
    return (center[0] + dx * c - dy * s, center[1] + dx * s + dy * c)


def rot_poly(pts, center, deg):
    if abs(deg) < 1e-6:
        return [(float(x), float(y)) for x, y in pts]
    return [rot_pt(p, center, deg) for p in pts]


def chamfer_rect_poly(x, y, w, h, c, rot_deg=0.0, center=None):
    c = min(float(c), w * 0.48, h * 0.48)
    if c <= 0.5:
        pts = [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]
    else:
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
    if abs(rot_deg) > 1e-6:
        ctr = center if center is not None else (x + w * 0.5, y + h * 0.5)
        pts = rot_poly(pts, ctr, rot_deg)
    return pts


def round_rect_poly(x, y, w, h, r, segs_per_corner=8, rot_deg=0.0, center=None):
    r = min(float(r), w * 0.49, h * 0.49)
    if r <= 0.5:
        return chamfer_rect_poly(x, y, w, h, 0.0, rot_deg=rot_deg, center=center)
    corners = [
        (x + w - r, y + r, -90.0, 0.0),
        (x + w - r, y + h - r, 0.0, 90.0),
        (x + r, y + h - r, 90.0, 180.0),
        (x + r, y + r, 180.0, 270.0),
    ]
    pts = []
    for cx, cy, a0, a1 in corners:
        for i in range(segs_per_corner + 1):
            ang = math.radians(a0 + (a1 - a0) * (i / segs_per_corner))
            pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    if abs(rot_deg) > 1e-6:
        ctr = center if center is not None else (x + w * 0.5, y + h * 0.5)
        pts = rot_poly(pts, ctr, rot_deg)
    return pts


def regular_ngon(cx, cy, r, sides, start_deg=-90.0):
    pts = []
    for i in range(sides):
        ang = math.radians(start_deg + 360.0 * i / sides)
        pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    return pts


def ellipse_poly(cx, cy, rx, ry, rot_deg=0.0, segs=64):
    pts = []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        px = cx + rx * math.cos(ang)
        py = cy + ry * math.sin(ang)
        if abs(rot_deg) > 1e-6:
            px, py = rot_pt((px, py), (cx, cy), rot_deg)
        pts.append((px, py))
    return pts


def capsule_poly(p0, p1, radius, segs=16):
    """ Thick rounded stroke capsule between p0 and p1 with exact radius (width = 2*radius). """
    x0, y0 = p0
    x1, y1 = p1
    dx, dy = x1 - x0, y1 - y0
    base_ang = math.degrees(math.atan2(dy, dx))
    pts = []
    for i in range(segs + 1):
        ang = math.radians(base_ang - 90.0 + 180.0 * (i / segs))
        pts.append((x1 + radius * math.cos(ang), y1 + radius * math.sin(ang)))
    for i in range(segs + 1):
        ang = math.radians(base_ang + 90.0 + 180.0 * (i / segs))
        pts.append((x0 + radius * math.cos(ang), y0 + radius * math.sin(ang)))
    return pts


def poly_area_and_centroid(pts):
    n = len(pts)
    if n < 3:
        return 0.0, (128.0, 128.0)
    a = 0.0
    cx = 0.0
    cy = 0.0
    for i in range(n):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % n]
        cross = x0 * y1 - x1 * y0
        a += cross
        cx += (x0 + x1) * cross
        cy += (y0 + y1) * cross
    a *= 0.5
    if abs(a) < 1e-6:
        return 0.0, (128.0, 128.0)
    cx /= (6.0 * a)
    cy /= (6.0 * a)
    return abs(a), (cx, cy)


class TwoToneIconCanvas:
    """
    Holds two-tone filled silhouette geometry in 256x256 space:
      - primary_pos: 100% white (#FFFFFF) outer silhouette polygons
      - primary_cut: negative-space cut-out polygons subtracted from primary_pos
      - secondary_pos: 45% alpha white (#FFFFFF) depth tone polygons
      - secondary_cut: negative-space cut-out polygons subtracted from secondary_pos
    """

    def __init__(self, size=CANVAS_SIZE):
        self.size = size
        self.primary_pos = []
        self.primary_cut = []
        self.secondary_pos = []
        self.secondary_cut = []

    def add_primary(self, pts):
        self.primary_pos.append([(float(x), float(y)) for x, y in pts])

    def add_cutout(self, pts):
        self.primary_cut.append([(float(x), float(y)) for x, y in pts])

    def add_secondary(self, pts):
        self.secondary_pos.append([(float(x), float(y)) for x, y in pts])

    def add_secondary_cutout(self, pts):
        self.secondary_cut.append([(float(x), float(y)) for x, y in pts])

    def normalize_and_center(self, target_span=216.0):
        """
        Scales the icon uniformly to `target_span` optical size and translates it so the
        alpha-weighted visual mass centroid sits as close to (128, 128) as possible while
        respecting the 16 px padding ([16, 240] x [16, 240]).
        """
        all_pos = self.primary_pos + self.secondary_pos
        if not all_pos:
            return

        xs = [p[0] for poly in all_pos for p in poly]
        ys = [p[1] for poly in all_pos for p in poly]
        min_x, max_x = min(xs), max(xs)
        min_y, max_y = min(ys), max(ys)
        w = max_x - min_x
        h = max_y - min_y
        span = max(w, h)
        if span > 1e-4 and target_span is not None:
            scale = min(target_span / span, (self.size - 2.0 * PADDING) / span)
            bx = 0.5 * (min_x + max_x)
            by = 0.5 * (min_y + max_y)

            def scale_list(poly_list):
                return [[(bx + (x - bx) * scale, by + (y - by) * scale) for x, y in poly] for poly in poly_list]

            self.primary_pos = scale_list(self.primary_pos)
            self.primary_cut = scale_list(self.primary_cut)
            self.secondary_pos = scale_list(self.secondary_pos)
            self.secondary_cut = scale_list(self.secondary_cut)

        # Compute visual mass centroid via 256x256 rasterization
        alpha = self._rasterize_alpha(ss=2)
        total_w = float(alpha.sum())
        if total_w > 1e-3:
            coords = np.arange(self.size, dtype=np.float32) + 0.5
            gx, gy = np.meshgrid(coords, coords)
            cx_mass = float((gx * alpha).sum() / total_w)
            cy_mass = float((gy * alpha).sum() / total_w)
        else:
            cx_mass, cy_mass = 128.0, 128.0

        all_pos = self.primary_pos + self.secondary_pos
        xs = [p[0] for poly in all_pos for p in poly]
        ys = [p[1] for poly in all_pos for p in poly]
        min_x, max_x = min(xs), max(xs)
        min_y, max_y = min(ys), max(ys)

        # Desired shift to place visual mass at (128, 128), clamped to [PADDING, size - PADDING]
        dx = 128.0 - cx_mass
        dy = 128.0 - cy_mass
        dx = max(PADDING - min_x, min((self.size - PADDING) - max_x, dx))
        dy = max(PADDING - min_y, min((self.size - PADDING) - max_y, dy))

        def shift_list(poly_list):
            return [[(x + dx, y + dy) for x, y in poly] for poly in poly_list]

        self.primary_pos = shift_list(self.primary_pos)
        self.primary_cut = shift_list(self.primary_cut)
        self.secondary_pos = shift_list(self.secondary_pos)
        self.secondary_cut = shift_list(self.secondary_cut)

    def _rasterize_alpha(self, ss=8):
        grid_n = self.size * ss
        img_p = Image.new("L", (grid_n, grid_n), 0)
        draw_p = ImageDraw.Draw(img_p)
        for poly in self.primary_pos:
            draw_p.polygon([(x * ss, y * ss) for x, y in poly], fill=255)
        for poly in self.primary_cut:
            draw_p.polygon([(x * ss, y * ss) for x, y in poly], fill=0)

        img_s = Image.new("L", (grid_n, grid_n), 0)
        draw_s = ImageDraw.Draw(img_s)
        for poly in self.secondary_pos:
            draw_s.polygon([(x * ss, y * ss) for x, y in poly], fill=255)
        for poly in self.secondary_cut:
            draw_s.polygon([(x * ss, y * ss) for x, y in poly], fill=0)

        mask_p = np.asarray(img_p, dtype=np.float32) / 255.0
        mask_s = np.asarray(img_s, dtype=np.float32) / 255.0
        # Primary body (100% alpha) overrides secondary tone (45% alpha)
        sub_alpha = np.where(mask_p > 0.5, 1.0, np.where(mask_s > 0.5, SECOND_TONE_ALPHA, 0.0))
        return sub_alpha.reshape((self.size, ss, self.size, ss)).mean(axis=(1, 3))

    def render_png(self, out_path, supersample=8):
        alpha_f = self._rasterize_alpha(ss=supersample)
        alpha_u8 = np.clip(np.rint(alpha_f * 255.0), 0, 255).astype(np.uint8)
        rgba = np.empty((self.size, self.size, 4), dtype=np.uint8)
        rgba[:, :, 0] = 255
        rgba[:, :, 1] = 255
        rgba[:, :, 2] = 255
        rgba[:, :, 3] = alpha_u8
        Image.fromarray(rgba, mode="RGBA").save(out_path, optimize=True)

    @staticmethod
    def _polys_to_d(polys):
        parts = []
        for poly in polys:
            if len(poly) < 3:
                continue
            cmds = [f"M {poly[0][0]:.2f} {poly[0][1]:.2f}"]
            for x, y in poly[1:]:
                cmds.append(f"L {x:.2f} {y:.2f}")
            cmds.append("Z")
            parts.append(" ".join(cmds))
        return " ".join(parts)

    def to_svg(self):
        lines = [
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.size}" height="{self.size}" '
            f'viewBox="0 0 {self.size} {self.size}">'
        ]
        sec_all = self.secondary_pos + self.secondary_cut
        if sec_all:
            d_sec = self._polys_to_d(sec_all)
            lines.append(
                f'  <g fill="#FFFFFF" fill-opacity="{SECOND_TONE_ALPHA:.2f}" fill-rule="evenodd">\n'
                f'    <path d="{d_sec}" stroke="none" stroke-width="{SVG_DETAIL_STROKE:.2f}" />\n'
                f'  </g>'
            )
        pri_all = self.primary_pos + self.primary_cut
        if pri_all:
            d_pri = self._polys_to_d(pri_all)
            lines.append(
                f'  <g fill="#FFFFFF" fill-rule="evenodd">\n'
                f'    <path d="{d_pri}" stroke="none" stroke-width="{SVG_PRIMARY_STROKE:.2f}" />\n'
                f'  </g>'
            )
        lines.append("</svg>\n")
        return "\n".join(lines)


def stepped_plate_l_poly(x, y, w, h, c, cut_x, cut_y, cut_w, cut_h, cut_c):
    """
    Builds a single non-overlapping L-shaped polygon for a back stacked plate
    whose bottom-left region is occluded by (cut_x, cut_y, cut_w, cut_h) with chamfer cut_c.
    """
    # Outer plate lives in [x, x+w] x [y, y+h] with corner chamfer c.
    # Occluder expanded boundary has top edge at oy = cut_y, right edge at ox = cut_x + cut_w,
    # and top-right chamfer from (ox - cut_c, oy) to (ox, oy + cut_c).
    oy = cut_y
    ox = cut_x + cut_w
    return [
        (x + c, y),
        (x + w - c, y),
        (x + w, y + c),
        (x + w, y + h - c),
        (x + w - c, y + h),
        (ox, y + h),
        (ox, oy + cut_c),
        (ox - cut_c, oy),
        (x, oy),
        (x, y + c),
    ]


# ---------------------------------------------------------------------------
# 1. icon_metal_plates — "metal = 2-3 stacked plates with a bolt"
# ---------------------------------------------------------------------------
def build_icon_metal_plates():
    c = TwoToneIconCanvas()

    # 3 stacked rectangular hull plates stepped up-right:
    # Plate 1 (Front, 100% white): wide chamfered rectangular plate (148 x 108)
    c.add_primary(chamfer_rect_poly(16, 114, 148, 108, 20))

    # Cut-out 1: Bold hexagonal bolt head cut-out on the front plate
    c.add_cutout(regular_ngon(90, 168, 26, 6, start_deg=0.0))

    # Plate 2 (Middle stacked plate, 100% white): stepped up-right by (+36, -42),
    # separated from Plate 1 by an 18 px negative-space gap
    c.add_primary(
        stepped_plate_l_poly(
            52, 72, 148, 108, 20,
            cut_x=-2, cut_y=96, cut_w=184, cut_h=144, cut_c=24
        )
    )

    # Plate 3 (Back stacked plate, 45% second tone): stepped up-right by (+72, -84),
    # separated from Plate 2 by an 18 px negative-space gap
    c.add_secondary(
        stepped_plate_l_poly(
            88, 30, 148, 108, 20,
            cut_x=34, cut_y=54, cut_w=184, cut_h=144, cut_c=24
        )
    )

    c.normalize_and_center(target_span=220.0)
    return c


# ---------------------------------------------------------------------------
# 2. icon_wiring_spool — "spool = flanged drum with cable wrapped"
# ---------------------------------------------------------------------------
def build_icon_wiring_spool():
    c = TwoToneIconCanvas()

    # Top and bottom wide spool flanges (100% white)
    c.add_primary(round_rect_poly(16, 20, 180, 36, 16))
    c.add_primary(round_rect_poly(16, 200, 180, 36, 16))

    # Recessed drum core between flanges (45% second tone)
    c.add_secondary(chamfer_rect_poly(54, 56, 104, 144, 4))

    # Two bold wrapped cable coils around drum (100% white), separated by >=18 px gaps
    c.add_primary(round_rect_poly(40, 74, 132, 44, 20))
    c.add_primary(round_rect_poly(40, 138, 132, 44, 20))

    # Bold loose cable tail sweeping out and up on the right (24 px thick)
    c.add_primary(capsule_poly((154, 160), (218, 148), 12.0))
    c.add_primary(capsule_poly((218, 148), (224, 76), 12.0))

    c.normalize_and_center(target_span=218.0)
    return c


# ---------------------------------------------------------------------------
# 3. icon_spare_igniter — "igniter = spark plug"
# ---------------------------------------------------------------------------
def build_icon_spare_igniter():
    c = TwoToneIconCanvas()
    ctr = (128.0, 128.0)
    ang = -40.0

    def tr(lx, ly):
        return rot_pt((ctr[0] + lx, ctr[1] + ly), ctr, ang)

    # Unified 100% white stepped spark-plug silhouette (terminal -> insulator -> hex waist -> shell -> tip)
    plug_silhouette = [
        tr(-104, -18),
        tr(-76, -18),
        tr(-76, -34),
        tr(-22, -34),
        tr(-14, -58),
        tr(26, -58),
        tr(34, -34),
        tr(68, -34),
        tr(68, -14),
        tr(86, -14),
        tr(86, 14),
        tr(68, 14),
        tr(68, 34),
        tr(34, 34),
        tr(26, 58),
        tr(-14, 58),
        tr(-22, 34),
        tr(-76, 34),
        tr(-76, 18),
        tr(-104, 18),
    ]
    c.add_primary(plug_silhouette)

    # Cut-out 1: Bold wrench-flat slot inside the wide hex collar (22 px wide x 60 px tall)
    wrench_cut = [
        tr(-5, -30),
        tr(17, -30),
        tr(17, 30),
        tr(-5, 30),
    ]
    c.add_cutout(wrench_cut)

    # Bold L-shaped ground electrode strap arching from front shell over tip (100% white, 20 px thick)
    ground_hook = [
        tr(50, -34),
        tr(68, -34),
        tr(112, -34),
        tr(112, 8),
        tr(92, 8),
        tr(92, -14),
        tr(50, -14),
    ]
    c.add_primary(ground_hook)

    # Two bold spark burst rays shooting from the electrode gap (45% second tone, 20 px thick)
    c.add_secondary(capsule_poly(tr(96, 26), tr(120, 46), 10.0))
    c.add_secondary(capsule_poly(tr(122, -10), tr(144, -24), 10.0))

    c.normalize_and_center(target_span=220.0)
    return c


# ---------------------------------------------------------------------------
# 4. icon_cutter — "cutter = nozzle tool with a short beam"
# ---------------------------------------------------------------------------
def build_icon_cutter():
    c = TwoToneIconCanvas()

    # 1. Rear turret mount collet (45% second tone)
    c.add_secondary(chamfer_rect_poly(12, 82, 28, 92, 8))

    # 2. Industrial cutter barrel + tapered conical nozzle tip (100% white unified silhouette)
    nozzle_body = [
        (40, 72),
        (54, 58),
        (104, 58),
        (114, 76),
        (160, 102),
        (160, 154),
        (114, 180),
        (104, 198),
        (54, 198),
        (40, 184),
    ]
    c.add_primary(nozzle_body)

    # Cut-out 1: Bold horizontal plasma chamber slot inside tool barrel (26 px tall)
    c.add_cutout(round_rect_poly(58, 115, 48, 26, 12))

    # 3. Short focused horizontal cutting beam (100% white lance, 66 px long x 28 px thick)
    # Separated from the nozzle tip (x=160) by an 18 px negative-space gap (x=178..244)
    # with a 45% second-tone beam tail/core extension for two-tone depth along the beam axis
    c.add_primary(round_rect_poly(178, 114, 52, 28, 13))
    c.add_secondary(round_rect_poly(220, 118, 26, 20, 9))

    c.normalize_and_center(target_span=220.0)
    return c


# ---------------------------------------------------------------------------
# 5. icon_lens — "lens = camera lens barrel seen 3/4"
# ---------------------------------------------------------------------------
def build_icon_lens():
    c = TwoToneIconCanvas()
    ctr = (128.0, 128.0)
    ang = -18.0

    # 1. Rear stepped camera lens barrel cylinder (45% second tone, seen in 3/4)
    rear_pts = []
    for i in range(25):
        a = math.radians(90.0 + 180.0 * (i / 24))
        rear_pts.append(rot_pt((ctr[0] - 78.0 + 28.0 * math.cos(a), ctr[1] + 58.0 * math.sin(a)), ctr, ang))
    rear_pts.append(rot_pt((ctr[0] - 18.0, ctr[1] - 58.0), ctr, ang))
    rear_pts.append(rot_pt((ctr[0] - 18.0, ctr[1] + 58.0), ctr, ang))
    c.add_secondary(rear_pts)

    # 2. Main wide front lens barrel cylinder (100% white, crisp straight cylinder sides + 3/4 elliptical rims)
    hood_pts = []
    for i in range(25):
        a = math.radians(90.0 + 180.0 * (i / 24))
        hood_pts.append(rot_pt((ctr[0] - 18.0 + 30.0 * math.cos(a), ctr[1] + 86.0 * math.sin(a)), ctr, ang))
    for i in range(25):
        a = math.radians(-90.0 + 180.0 * (i / 24))
        hood_pts.append(rot_pt((ctr[0] + 46.0 + 48.0 * math.cos(a), ctr[1] + 86.0 * math.sin(a)), ctr, ang))
    c.add_primary(hood_pts)

    # Cut-out 1: Deep 3/4 elliptical optical aperture inside the front lens face
    c.add_cutout(
        ellipse_poly(
            ctr[0] + 48.0 * math.cos(math.radians(ang)),
            ctr[1] + 48.0 * math.sin(math.radians(ang)),
            32.0,
            60.0,
            rot_deg=ang,
            segs=48,
        )
    )

    # Inner 3/4 convex lens glint inside the dark optical cavity (45% second tone)
    c.add_secondary(
        ellipse_poly(
            ctr[0] + 54.0 * math.cos(math.radians(ang)),
            ctr[1] + 54.0 * math.sin(math.radians(ang)),
            16.0,
            34.0,
            rot_deg=ang,
            segs=36,
        )
    )

    c.normalize_and_center(target_span=216.0)
    return c


# ---------------------------------------------------------------------------
# 6. icon_empty_socket — "socket = empty square bay with 4 corner brackets"
# ---------------------------------------------------------------------------
def build_icon_empty_socket():
    c = TwoToneIconCanvas()

    # 4 bold L-shaped corner brackets (100% white, 28 px thick arms, 60 px gap between brackets)
    # Top-Left bracket
    c.add_primary([
        (42, 22), (98, 22), (98, 50), (50, 50), (50, 98), (22, 98), (22, 42)
    ])
    # Top-Right bracket
    c.add_primary([
        (158, 22), (214, 22), (234, 42), (234, 98), (206, 98), (206, 50), (158, 50)
    ])
    # Bottom-Right bracket
    c.add_primary([
        (206, 158), (234, 158), (234, 214), (214, 234), (158, 234), (158, 206), (206, 206)
    ])
    # Bottom-Left bracket
    c.add_primary([
        (22, 158), (50, 158), (50, 206), (98, 206), (98, 234), (42, 234), (22, 214)
    ])

    # Recessed inner square bay (45% second tone, 24 px frame wall around open empty center)
    c.add_secondary(chamfer_rect_poly(68, 68, 120, 120, 14))
    c.add_secondary_cutout(chamfer_rect_poly(92, 92, 72, 72, 8))

    c.normalize_and_center(target_span=212.0)
    return c


# ---------------------------------------------------------------------------
# 7. icon_energy_battery — "battery = cell with terminal"
# ---------------------------------------------------------------------------
def build_icon_energy_battery():
    c = TwoToneIconCanvas()

    # Unified 100% white battery cell + protruding top positive terminal post
    battery_silhouette = [
        (86, 26),
        (170, 26),
        (178, 34),
        (178, 58),
        (194, 58),
        (214, 78),
        (214, 198),
        (194, 218),
        (62, 218),
        (42, 198),
        (42, 78),
        (62, 58),
        (78, 58),
        (78, 34),
    ]
    c.add_primary(battery_silhouette)

    # Bottom depth base bevel under cell body (45% second tone, 18 px tall)
    base_bevel = [
        (62, 218),
        (194, 218),
        (182, 236),
        (74, 236),
    ]
    c.add_secondary(base_bevel)

    # Cut-out 1: Bold central high-voltage lightning bolt carved into the solid cell body
    bolt_cut = [
        (142, 78),
        (88, 148),
        (124, 148),
        (112, 204),
        (168, 134),
        (132, 134),
    ]
    c.add_cutout(bolt_cut)

    c.normalize_and_center(target_span=216.0)
    return c


# ---------------------------------------------------------------------------
# 8. icon_ice_core_tube — "ice core = capped tube with frost"
# ---------------------------------------------------------------------------
def build_icon_ice_core_tube():
    c = TwoToneIconCanvas()
    ctr = (128.0, 128.0)
    ang = -25.0

    def tr(lx, ly):
        return rot_pt((ctr[0] + lx, ctr[1] + ly), ctr, ang)

    # 1. Unified 100% white capped cylindrical sample tube (stepped end-caps + continuous solid cylinder)
    tube_silhouette = [
        tr(-98, -50),
        tr(-88, -60),
        tr(-62, -60),
        tr(-62, -46),
        tr(62, -46),
        tr(62, -60),
        tr(88, -60),
        tr(98, -50),
        tr(98, 50),
        tr(88, 60),
        tr(62, 60),
        tr(62, 46),
        tr(-62, 46),
        tr(-62, 60),
        tr(-88, 60),
        tr(-98, 50),
    ]
    c.add_primary(tube_silhouette)

    # 2. End-cap seal lugs at both ends of the tube (45% second tone)
    left_lug = [
        tr(-114, -28),
        tr(-98, -36),
        tr(-98, 36),
        tr(-114, 28),
    ]
    right_lug = [
        tr(98, -36),
        tr(114, -28),
        tr(114, 28),
        tr(98, 36),
    ]
    c.add_secondary(left_lug)
    c.add_secondary(right_lug)

    # Cut-out 1: Bold 6-pointed frost crystal carved out of the center of the solid tube
    # Built as a single 12-vertex star polygon so SVG evenodd and PNG match identically
    frost_pts = []
    r_outer = 34.0
    r_inner = 13.0
    for i in range(6):
        a0 = math.radians(ang + i * 60.0 - 14.0)
        a1 = math.radians(ang + i * 60.0)
        a2 = math.radians(ang + i * 60.0 + 14.0)
        a_mid = math.radians(ang + i * 60.0 + 30.0)
        frost_pts.append((ctr[0] + r_outer * math.cos(a0), ctr[1] + r_outer * math.sin(a0)))
        frost_pts.append((ctr[0] + (r_outer + 3.0) * math.cos(a1), ctr[1] + (r_outer + 3.0) * math.sin(a1)))
        frost_pts.append((ctr[0] + r_outer * math.cos(a2), ctr[1] + r_outer * math.sin(a2)))
        frost_pts.append((ctr[0] + r_inner * math.cos(a_mid), ctr[1] + r_inner * math.sin(a_mid)))
    c.add_cutout(frost_pts)

    c.normalize_and_center(target_span=220.0)
    return c


# ---------------------------------------------------------------------------
# 9. icon_warning — "warning = triangle with !"
# ---------------------------------------------------------------------------
def build_icon_warning():
    c = TwoToneIconCanvas()

    # Solid chamfered equilateral warning triangle (100% white)
    tri_body = [
        (114, 24),
        (142, 24),
        (236, 190),
        (220, 214),
        (36, 214),
        (20, 190),
    ]
    c.add_primary(tri_body)

    # Lower depth bevel strip under triangle base (45% second tone, 18 px tall)
    tri_base_bevel = [
        (36, 214),
        (220, 214),
        (208, 232),
        (48, 232),
    ]
    c.add_secondary(tri_base_bevel)

    # Cut-out 1 & 2: Bold negative-space exclamation mark (!) inside solid triangle
    bang_bar = [
        (113, 74),
        (143, 74),
        (139, 150),
        (117, 150),
    ]
    c.add_cutout(bang_bar)
    c.add_cutout(regular_ngon(128, 182, 15.0, 16))

    c.normalize_and_center(target_span=216.0)
    return c


# ---------------------------------------------------------------------------
# Unity .meta helpers (preserve existing GUIDs)
# ---------------------------------------------------------------------------
def deterministic_guid(rel_posix_path):
    return hashlib.md5(("MoonProject.FaceInventoryArt:" + rel_posix_path).encode("utf-8")).hexdigest()


def ensure_svg_meta(svg_path, rel_posix_path):
    meta_path = Path(str(svg_path) + ".meta")
    if meta_path.exists():
        return
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
    meta_path.write_text(meta_text, encoding="utf-8", newline="\n")


def ensure_png_meta(png_path, rel_posix_path, max_size=256):
    meta_path = Path(str(png_path) + ".meta")
    if meta_path.exists():
        return
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
    meta_path.write_text(meta_text, encoding="utf-8", newline="\n")


ICON_BUILDERS = (
    ("icon_metal_plates", build_icon_metal_plates),
    ("icon_wiring_spool", build_icon_wiring_spool),
    ("icon_spare_igniter", build_icon_spare_igniter),
    ("icon_cutter", build_icon_cutter),
    ("icon_lens", build_icon_lens),
    ("icon_empty_socket", build_icon_empty_socket),
    ("icon_energy_battery", build_icon_energy_battery),
    ("icon_ice_core_tube", build_icon_ice_core_tube),
    ("icon_warning", build_icon_warning),
)

ICON_LABELS = {
    "icon_metal_plates": "metal_plates (Stacked Plates + Bolt)",
    "icon_wiring_spool": "wiring_spool (Flanged Drum + Cable)",
    "icon_spare_igniter": "spare_igniter (Spark Plug)",
    "icon_cutter": "cutter (Nozzle Tool + Short Beam)",
    "icon_lens": "lens (Camera Lens Barrel 3/4)",
    "icon_empty_socket": "empty_socket (Square Bay + 4 Brackets)",
    "icon_energy_battery": "energy_battery (Cell + Terminal)",
    "icon_ice_core_tube": "ice_core_tube (Capped Tube + Frost)",
    "icon_warning": "warning (Triangle + !)",
}


def tint_icon_amber(rgba_img, tint_rgb=(0xE8, 0xC2, 0x7A)):
    """Tints an icon image (using its alpha mask) to amber (#E8C27A)."""
    arr = np.asarray(rgba_img.convert("RGBA"), dtype=np.float32)
    out = np.empty_like(arr, dtype=np.uint8)
    out[:, :, 0] = tint_rgb[0]
    out[:, :, 1] = tint_rgb[1]
    out[:, :, 2] = tint_rgb[2]
    out[:, :, 3] = np.clip(np.rint(arr[:, :, 3]), 0, 255).astype(np.uint8)
    return Image.fromarray(out, mode="RGBA")


def build_proof_sheet():
    """
    Writes Logs/captures/icons_sheet.png showing every icon at 256, 64, 40 and 32 px
    on the dark CRT background (#1b1a17) tinted amber (#E8C27A), old vs new side by side,
    plus a dedicated 32 px and 40 px glanceability row across all 9 icons at the bottom.
    """
    captures_dir = REPO_ROOT / "Logs" / "captures"
    captures_dir.mkdir(parents=True, exist_ok=True)

    bg_crt = (0x1B, 0x1A, 0x17, 255)       # #1b1a17
    amber_tint = (0xE8, 0xC2, 0x7A)        # #E8C27A
    amber_rgba = (0xE8, 0xC2, 0x7A, 255)
    muted_rgba = (175, 146, 92, 255)
    frame_rgba = (78, 66, 44, 255)
    subframe_rgba = (54, 46, 32, 255)

    try:
        font_h1 = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 18)
        font_card = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 14)
        font_sub = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 12)
    except Exception:
        font_h1 = ImageFont.load_default()
        font_card = ImageFont.load_default()
        font_sub = ImageFont.load_default()

    # 3 columns x 3 rows of side-by-side OLD vs NEW cards + bottom glanceability strip
    card_w, card_h = 680, 410
    margin_x, margin_y = 28, 56
    gap_x, gap_y = 20, 18
    sheet_w = margin_x * 2 + card_w * 3 + gap_x * 2
    sheet_h = margin_y + card_h * 3 + gap_y * 2 + 210

    sheet = Image.new("RGBA", (sheet_w, sheet_h), bg_crt)
    draw = ImageDraw.Draw(sheet)

    draw.text(
        (margin_x, 18),
        "07 FACE ICONS REDESIGN — OLD vs NEW AT 256, 64, 40, 32 PX ON #1b1a17 TINTED #E8C27A",
        fill=amber_rgba,
        font=font_h1,
    )

    for idx, (name, _) in enumerate(ICON_BUILDERS):
        col = idx % 3
        row = idx // 3
        cx = margin_x + col * (card_w + gap_x)
        cy = margin_y + row * (card_h + gap_y)

        draw.rectangle([cx, cy, cx + card_w, cy + card_h], outline=frame_rgba, width=1)
        draw.text((cx + 14, cy + 10), f"{idx + 1}. {ICON_LABELS.get(name, name)}", fill=amber_rgba, font=font_card)

        new_raw = Image.open(OUT_DIR / f"{name}.png").convert("RGBA")
        old_path = OLD_BACKUP_DIR / f"{name}.png"
        old_raw = Image.open(old_path).convert("RGBA") if old_path.exists() else new_raw

        old_256 = tint_icon_amber(old_raw, amber_tint)
        new_256 = tint_icon_amber(new_raw, amber_tint)

        # Left half of card: OLD (256 + 64, 40, 32)
        # Right half of card: NEW (256 + 64, 40, 32)
        for side_idx, (tag, img_256) in enumerate((("OLD", old_256), ("NEW", new_256))):
            sx = cx + 14 + side_idx * 332
            sy = cy + 36
            draw.text((sx, sy), f"{tag} (256 px)", fill=muted_rgba, font=font_sub)
            b256_y = sy + 18
            draw.rectangle([sx - 1, b256_y - 1, sx + 256, b256_y + 256], outline=subframe_rgba, width=1)
            sheet.alpha_composite(img_256, (sx, b256_y))

            # Bottom row inside side: 64 px, 40 px, 32 px side by side
            row_y = b256_y + 268
            cur_x = sx
            for sz in (64, 40, 32):
                down = img_256.resize((sz, sz), Image.Resampling.LANCZOS)
                draw.rectangle([cur_x - 2, row_y - 2, cur_x + sz + 2, row_y + sz + 2], outline=frame_rgba, width=1)
                sheet.alpha_composite(down, (cur_x, row_y))
                draw.text((cur_x + sz + 6, row_y + max(0, sz // 2 - 7)), f"{sz}px", fill=muted_rgba, font=font_sub)
                cur_x += sz + 48

    # Bottom strip: dedicated 40 px & 32 px side-by-side row across all 9 icons
    strip_y = margin_y + card_h * 3 + gap_y * 2 + 10
    draw.rectangle([margin_x, strip_y, sheet_w - margin_x, sheet_h - 16], outline=frame_rgba, width=1)
    draw.text(
        (margin_x + 16, strip_y + 10),
        "GLANCEABILITY CHECK — ALL 9 ICONS AT 40 PX AND 32 PX (OLD vs NEW SIDE BY SIDE)",
        fill=amber_rgba,
        font=font_card,
    )

    slot_w = (sheet_w - margin_x * 2 - 32) // 9
    for idx, (name, _) in enumerate(ICON_BUILDERS):
        ix = margin_x + 16 + idx * slot_w
        iy = strip_y + 38
        short_name = name.replace("icon_", "")
        draw.text((ix, iy), short_name, fill=amber_rgba, font=font_sub)

        new_256 = tint_icon_amber(Image.open(OUT_DIR / f"{name}.png"), amber_tint)
        old_path = OLD_BACKUP_DIR / f"{name}.png"
        old_256 = tint_icon_amber(Image.open(old_path), amber_tint) if old_path.exists() else new_256

        # 40 px row (OLD left, NEW right)
        r40_y = iy + 20
        draw.text((ix, r40_y + 12), "40:", fill=muted_rgba, font=font_sub)
        o40 = old_256.resize((40, 40), Image.Resampling.LANCZOS)
        n40 = new_256.resize((40, 40), Image.Resampling.LANCZOS)
        draw.rectangle([ix + 28 - 2, r40_y - 2, ix + 28 + 42, r40_y + 42], outline=subframe_rgba, width=1)
        sheet.alpha_composite(o40, (ix + 28, r40_y))
        draw.rectangle([ix + 80 - 2, r40_y - 2, ix + 80 + 42, r40_y + 42], outline=frame_rgba, width=1)
        sheet.alpha_composite(n40, (ix + 80, r40_y))

        # 32 px row (OLD left, NEW right)
        r32_y = r40_y + 52
        draw.text((ix, r32_y + 8), "32:", fill=muted_rgba, font=font_sub)
        o32 = old_256.resize((32, 32), Image.Resampling.LANCZOS)
        n32 = new_256.resize((32, 32), Image.Resampling.LANCZOS)
        draw.rectangle([ix + 28 - 2, r32_y - 2, ix + 28 + 34, r32_y + 34], outline=subframe_rgba, width=1)
        sheet.alpha_composite(o32, (ix + 28, r32_y))
        draw.rectangle([ix + 80 - 2, r32_y - 2, ix + 80 + 34, r32_y + 34], outline=frame_rgba, width=1)
        sheet.alpha_composite(n32, (ix + 80, r32_y))

    out_sheet = captures_dir / "icons_sheet.png"
    sheet.save(out_sheet, optimize=True)
    print(f"Saved proof sheet: {out_sheet}")


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name, fn in ICON_BUILDERS:
        canvas = fn()
        svg_path = OUT_DIR / f"{name}.svg"
        png_path = OUT_DIR / f"{name}.png"
        svg_path.write_text(canvas.to_svg(), encoding="utf-8", newline="\n")
        canvas.render_png(png_path, supersample=8)
        ensure_svg_meta(svg_path, f"Assets/_Project/Art/UI/Face/{name}.svg")
        ensure_png_meta(png_path, f"Assets/_Project/Art/UI/Face/{name}.png", max_size=256)

        all_pos = canvas.primary_pos + canvas.secondary_pos
        xs = [p[0] for poly in all_pos for p in poly]
        ys = [p[1] for poly in all_pos for p in poly]
        print(
            f"Generated {name}: bounds=({min(xs):.1f}, {min(ys):.1f})..({max(xs):.1f}, {max(ys):.1f})"
        )
    build_proof_sheet()


if __name__ == "__main__":
    main()
