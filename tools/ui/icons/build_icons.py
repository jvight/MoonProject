#!/usr/bin/env python3
"""
Deterministic SVG generator and CRT review sheet renderer for Task ICON-SET.

Writes all 30 inventory & equipment icons to Assets/_Project/UI/Icons/:
  - 18 hold-item icons: item_<id>.svg
  - 12 rover-kit icons: kit_<socket>.svg (one per RoverSocket)

Every icon adheres strictly to:
  - viewBox="0 0 64 64", width="64", height="64"
  - strokes only (2.2 primary / 1.5 detail) with round caps and joins,
    and at most one flat fill per icon
  - single colour (currentColor) so the UI tints the three states
  - path commands supported by Assets/_Project/Scripts/UI/Editor/SvgPathParser.cs:
    absolute M, C, A (circular rx == ry, rotation == 0, from != to) and Z only.
"""
from __future__ import annotations

import argparse
import math
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

REPO_ROOT = Path(__file__).resolve().parents[3]
DEFAULT_ICON_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Icons"
DEFAULT_REVIEW_DIR = REPO_ROOT / "Logs" / "icons"
FONT_DIR = REPO_ROOT / "Assets" / "_Project" / "UI" / "Fonts"

SVG_NAMESPACE = "http://www.w3.org/2000/svg"
VIEWBOX_SIZE = 64.0
PRIMARY_STROKE = "2.2"
DETAIL_STROKE = "1.5"
CIRCULAR_TOLERANCE = 0.001

MOVE_TO = "MoveTo"
CUBIC_TO = "CubicTo"
ARC_TO = "ArcTo"
CLOSE = "Close"


# ---------------------------------------------------------------------------
# Exact Python port of Assets/_Project/Scripts/UI/Editor/SvgPathParser.cs
# ---------------------------------------------------------------------------

class _Reader:
    """Character-by-character reader matching SvgPathParser.Reader in C#."""

    def __init__(self, text: str) -> None:
        self._text = text
        self.position = 0

    @property
    def peek(self) -> str:
        return self._text[self.position]

    def next(self) -> str:
        ch = self._text[self.position]
        self.position += 1
        return ch

    def skip_separators(self) -> bool:
        n = len(self._text)
        while self.position < n and (self._text[self.position].isspace() or self._text[self.position] == ","):
            self.position += 1
        return self.position < n

    def point(self) -> Tuple[float, float]:
        x = self.number()
        y = self.number()
        return (x, y)

    def number(self) -> float:
        if not self.skip_separators():
            raise ValueError("SVG path: ended in the middle of a command.")

        begin = self.position
        if self.peek in ("-", "+"):
            self.position += 1

        dot = False
        n = len(self._text)
        while self.position < n:
            c = self._text[self.position]
            if c.isdigit():
                self.position += 1
            elif c == "." and not dot:
                dot = True
                self.position += 1
            elif c in ("e", "E") and self.position > begin:
                self.position += 1
                if self.position < n and self._text[self.position] in ("-", "+"):
                    self.position += 1
            else:
                break

        token = self._text[begin:self.position]
        try:
            value = float(token)
        except ValueError as exc:
            raise ValueError(f"SVG path: '{token}' at {begin} is not a number.") from exc
        if math.isnan(value) or math.isinf(value):
            raise ValueError(f"SVG path: '{token}' at {begin} is not a finite number.")
        return value


def parse_svg_path(data: str) -> Tuple[List[str], List[float]]:
    """
    Port of MoonProject.UI.Editor.SvgPathParser.Parse.
    Supports ONLY absolute M, C (repeatable), circular unrotated A (repeatable), and Z.
    Raises ValueError on any unsupported command or malformed token.
    """
    if data is None:
        raise ValueError("data cannot be None")

    reader = _Reader(data)
    command = "\0"
    current = (0.0, 0.0)
    start = (0.0, 0.0)
    commands: List[str] = []
    values: List[float] = []

    while reader.skip_separators():
        if reader.peek.isalpha():
            command = reader.next()
        elif command not in ("C", "A"):
            raise ValueError(f"SVG path: a number with no command at {reader.position}.")

        if command == "M":
            current = start = reader.point()
            commands.append(MOVE_TO)
            values.extend((current[0], current[1]))
            command = "\0"
        elif command == "C":
            first = reader.point()
            second = reader.point()
            current = reader.point()
            commands.append(CUBIC_TO)
            values.extend((first[0], first[1], second[0], second[1], current[0], current[1]))
        elif command == "A":
            current = _parse_arc(reader, current, commands, values)
        elif command == "Z":
            commands.append(CLOSE)
            current = start
            command = "\0"
        else:
            raise ValueError(f"SVG path: command '{command}' is not supported.")

    return commands, values


def _parse_arc(
    reader: _Reader,
    from_pt: Tuple[float, float],
    commands: List[str],
    values: List[float],
) -> Tuple[float, float]:
    rx = reader.number()
    ry = reader.number()
    rotation = reader.number()
    large = reader.number() != 0.0
    sweep = reader.number() != 0.0
    to_pt = reader.point()

    if abs(rx - ry) > CIRCULAR_TOLERANCE or abs(rotation) > CIRCULAR_TOLERANCE:
        raise ValueError("SVG path: only circular, unrotated arcs are supported.")
    if rx <= 0.0:
        raise ValueError("SVG path: arc radius must be positive.")

    hx = (from_pt[0] - to_pt[0]) * 0.5
    hy = (from_pt[1] - to_pt[1]) * 0.5
    half_sqr = hx * hx + hy * hy
    if half_sqr <= 1e-8:
        raise ValueError("SVG path: degenerate zero-length arc endpoints.")

    half_mag = math.sqrt(half_sqr)
    radius = max(rx, half_mag)
    reach = math.sqrt(max(0.0, radius * radius - half_sqr) / half_sqr)
    sign = 1.0 if (large != sweep) else -1.0
    mid_x = (from_pt[0] + to_pt[0]) * 0.5
    mid_y = (from_pt[1] + to_pt[1]) * 0.5
    centre_x = hy * (sign * reach) + mid_x
    centre_y = -hx * (sign * reach) + mid_y

    start_angle = math.degrees(math.atan2(from_pt[1] - centre_y, from_pt[0] - centre_x))
    delta = math.degrees(math.atan2(to_pt[1] - centre_y, to_pt[0] - centre_x)) - start_angle
    if sweep and delta < 0.0:
        delta += 360.0
    elif (not sweep) and delta > 0.0:
        delta -= 360.0

    commands.append(ARC_TO)
    values.extend((centre_x, centre_y, radius, start_angle, start_angle + delta, 1.0 if sweep else 0.0))
    return to_pt


def path_bounds(data: str) -> Tuple[float, float, float, float]:
    """
    Returns (min_x, min_y, max_x, max_y) across all vertices, cubic control points,
    and circular arc points of `data`.
    """
    commands, values = parse_svg_path(data)
    if not commands:
        raise ValueError("SVG path is empty.")

    xs: List[float] = []
    ys: List[float] = []
    idx = 0
    cur = (0.0, 0.0)
    start = (0.0, 0.0)

    for cmd in commands:
        if cmd == MOVE_TO:
            cur = start = (values[idx], values[idx + 1])
            idx += 2
            xs.append(cur[0])
            ys.append(cur[1])
        elif cmd == CUBIC_TO:
            x1, y1, x2, y2, x3, y3 = values[idx:idx + 6]
            idx += 6
            xs.extend((cur[0], x1, x2, x3))
            ys.extend((cur[1], y1, y2, y3))
            cur = (x3, y3)
        elif cmd == ARC_TO:
            cx, cy, r, a0, a1, _sweep = values[idx:idx + 6]
            idx += 6
            steps = max(16, int(abs(a1 - a0) / 4.0) + 2)
            for s in range(steps + 1):
                ang = math.radians(a0 + (a1 - a0) * (s / steps))
                xs.append(cx + r * math.cos(ang))
                ys.append(cy + r * math.sin(ang))
            end_rad = math.radians(a1)
            cur = (cx + r * math.cos(end_rad), cy + r * math.sin(end_rad))
        elif cmd == CLOSE:
            cur = start

    return (min(xs), min(ys), max(xs), max(ys))


# ---------------------------------------------------------------------------
# Deterministic SVG path geometry helpers (M, C, circular A, Z only)
# ---------------------------------------------------------------------------

def _fmt(val: float) -> str:
    rounded = round(float(val), 2)
    if abs(rounded) < 1e-4:
        rounded = 0.0
    text = f"{rounded:.2f}".rstrip("0").rstrip(".")
    return text if text else "0"


def _seg(x0: float, y0: float, x1: float, y1: float) -> str:
    """Collinear cubic Bézier segment from (x0, y0) to (x1, y1)."""
    return f"C{_fmt(x0)} {_fmt(y0)} {_fmt(x1)} {_fmt(y1)} {_fmt(x1)} {_fmt(y1)}"


def line(x0: float, y0: float, x1: float, y1: float) -> str:
    """Single straight line segment encoded as M + C."""
    return f"M{_fmt(x0)} {_fmt(y0)}{_seg(x0, y0, x1, y1)}"


def polyline(points: Sequence[Tuple[float, float]], closed: bool = False) -> str:
    """Polyline or polygon encoded strictly with M, collinear C, and optional Z."""
    if len(points) < 2:
        raise ValueError("polyline requires at least 2 points")
    parts = [f"M{_fmt(points[0][0])} {_fmt(points[0][1])}"]
    for i in range(len(points) - 1):
        x0, y0 = points[i]
        x1, y1 = points[i + 1]
        parts.append(_seg(x0, y0, x1, y1))
    if closed:
        x0, y0 = points[-1]
        x1, y1 = points[0]
        if math.hypot(x1 - x0, y1 - y0) > 1e-3:
            parts.append(_seg(x0, y0, x1, y1))
        parts.append("Z")
    return "".join(parts)


def cubic_chain(
    start: Tuple[float, float],
    segments: Sequence[Tuple[Tuple[float, float], Tuple[float, float], Tuple[float, float]]],
    closed: bool = False,
) -> str:
    """Explicit cubic Bézier chain starting at `start`."""
    parts = [f"M{_fmt(start[0])} {_fmt(start[1])}"]
    for c1, c2, end in segments:
        parts.append(
            f"C{_fmt(c1[0])} {_fmt(c1[1])} {_fmt(c2[0])} {_fmt(c2[1])} {_fmt(end[0])} {_fmt(end[1])}"
        )
    if closed:
        parts.append("Z")
    return "".join(parts)


def circle(cx: float, cy: float, r: float) -> str:
    """Full circle encoded as two 180-degree circular arcs so from != to."""
    x_left = cx - r
    x_right = cx + r
    rf = _fmt(r)
    return (
        f"M{_fmt(x_left)} {_fmt(cy)}"
        f"A{rf} {rf} 0 1 1 {_fmt(x_right)} {_fmt(cy)}"
        f"A{rf} {rf} 0 1 1 {_fmt(x_left)} {_fmt(cy)}Z"
    )


def arc(cx: float, cy: float, r: float, start_deg: float, end_deg: float, closed: bool = False) -> str:
    """
    Circular unrotated arc from `start_deg` to `end_deg` (degrees, 0=+X, 90=+Y).
    Splits arcs > 180 degrees into two steps so endpoints are never coincident.
    """
    delta = end_deg - start_deg
    if abs(delta) < 1e-3:
        raise ValueError("arc delta must be non-zero")
    steps = 2 if abs(delta) > 175.0 else 1
    step_deg = delta / steps
    sweep_flag = 1 if step_deg > 0 else 0
    rf = _fmt(r)

    a0 = math.radians(start_deg)
    x0 = cx + r * math.cos(a0)
    y0 = cy + r * math.sin(a0)
    parts = [f"M{_fmt(x0)} {_fmt(y0)}"]
    cur_x, cur_y = x0, y0
    for s in range(1, steps + 1):
        ang = math.radians(start_deg + step_deg * s)
        nx = cx + r * math.cos(ang)
        ny = cy + r * math.sin(ang)
        large_flag = 1 if abs(step_deg) > 180.0 else 0
        parts.append(f"A{rf} {rf} 0 {large_flag} {sweep_flag} {_fmt(nx)} {_fmt(ny)}")
        cur_x, cur_y = nx, ny
    if closed:
        if math.hypot(x0 - cur_x, y0 - cur_y) > 1e-3:
            parts.append(_seg(cur_x, cur_y, x0, y0))
        parts.append("Z")
    return "".join(parts)


def round_rect(x: float, y: float, w: float, h: float, r: float) -> str:
    """Rounded rectangle using collinear C sides and circular A corners."""
    r = min(r, w * 0.5 - 0.05, h * 0.5 - 0.05)
    if r <= 0.05:
        return polyline([(x, y), (x + w, y), (x + w, y + h), (x, y + h)], closed=True)
    rf = _fmt(r)
    x0, y0 = x, y
    x1, y1 = x + w, y + h
    return (
        f"M{_fmt(x0 + r)} {_fmt(y0)}"
        f"{_seg(x0 + r, y0, x1 - r, y0)}"
        f"A{rf} {rf} 0 0 1 {_fmt(x1)} {_fmt(y0 + r)}"
        f"{_seg(x1, y0 + r, x1, y1 - r)}"
        f"A{rf} {rf} 0 0 1 {_fmt(x1 - r)} {_fmt(y1)}"
        f"{_seg(x1 - r, y1, x0 + r, y1)}"
        f"A{rf} {rf} 0 0 1 {_fmt(x0)} {_fmt(y1 - r)}"
        f"{_seg(x0, y1 - r, x0, y0 + r)}"
        f"A{rf} {rf} 0 0 1 {_fmt(x0 + r)} {_fmt(y0)}Z"
    )


def tilted_ellipse(cx: float, cy: float, rx: float, ry: float, angle_deg: float = 0.0) -> str:
    """
    4-segment cubic Bézier approximation of an ellipse rotated by `angle_deg`,
    since SvgPathParser.cs intentionally rejects elliptical or rotated A commands.
    """
    k = 0.55228475
    ang = math.radians(angle_deg)
    ca, sa = math.cos(ang), math.sin(ang)

    def tr(px: float, py: float) -> Tuple[float, float]:
        return (cx + px * ca - py * sa, cy + px * sa + py * ca)

    p0 = tr(rx, 0.0)
    p1 = tr(0.0, ry)
    p2 = tr(-rx, 0.0)
    p3 = tr(0.0, -ry)

    segs = [
        (tr(rx, k * ry), tr(k * rx, ry), p1),
        (tr(-k * rx, ry), tr(-rx, k * ry), p2),
        (tr(-rx, -k * ry), tr(-k * rx, -ry), p3),
        (tr(k * rx, -ry), tr(rx, -k * ry), p0),
    ]
    return cubic_chain(p0, segs, closed=True)


def regular_polygon(cx: float, cy: float, r: float, sides: int, start_deg: float = -90.0) -> str:
    pts = []
    for i in range(sides):
        a = math.radians(start_deg + i * (360.0 / sides))
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return polyline(pts, closed=True)


# ---------------------------------------------------------------------------
# Icon specification & all 30 deterministic icon definitions
# ---------------------------------------------------------------------------

@dataclass(frozen=True)
class IconSpec:
    filename: str
    icon_id: str
    kind: str  # "item" or "kit"
    localization_key: str
    code_symbol: str
    label: str
    primary: Tuple[str, ...]
    detail: Tuple[str, ...]
    fill: Optional[str] = None
    socket_index: Optional[int] = None
    dim_in_05: bool = False


def _build_item_specs() -> List[IconSpec]:
    specs: List[IconSpec] = []

    # 1. item_metal.svg -> material.metal (SalvageMaterial.Metal)
    # Stepped structural I-beam / riveted hull plate in angled elevation.
    specs.append(IconSpec(
        filename="item_metal.svg",
        icon_id="metal",
        kind="item",
        localization_key="material.metal",
        code_symbol="SalvageMaterial.Metal",
        label="METAL",
        primary=(
            polyline([
                (9, 17), (51, 11), (55, 19), (48, 21),
                (50, 41), (56, 43), (52, 51), (12, 55),
                (8, 47), (15, 45), (13, 25), (8, 23),
            ], closed=True),
            line(13, 25, 48, 21),
            line(15, 45, 50, 41),
        ),
        detail=(
            line(19, 31, 44, 28),
            line(20, 39, 45, 36),
            circle(17, 19.5, 1.6),
            circle(31, 17.5, 1.6),
            circle(45, 15.5, 1.6),
            circle(19, 49.5, 1.6),
            circle(33, 47.5, 1.6),
            circle(47, 45.5, 1.6),
        ),
        fill=circle(31, 17.5, 1.6),
    ))

    # 2. item_wiring.svg -> material.wiring (SalvageMaterial.Wiring)
    # Flanged copper cable spool on the left with a sweeping 3-conductor pigtail fan on the right.
    specs.append(IconSpec(
        filename="item_wiring.svg",
        icon_id="wiring",
        kind="item",
        localization_key="material.wiring",
        code_symbol="SalvageMaterial.Wiring",
        label="WIRING",
        primary=(
            round_rect(9, 13, 28, 6, 2.8),
            round_rect(9, 47, 28, 6, 2.8),
            line(13, 19, 13, 47),
            line(33, 19, 33, 47),
            cubic_chain((33, 37), [((42, 37), (43, 27), (47, 25))], closed=False),
            line(47, 25, 55, 15),
            line(47, 25, 57, 23),
            line(47, 25, 55, 31),
        ),
        detail=(
            line(13, 24, 33, 24),
            line(13, 30, 33, 30),
            line(13, 36, 33, 36),
            line(13, 42, 33, 42),
            line(19, 19, 17, 47),
            line(27, 19, 25, 47),
            circle(55, 15, 1.7),
            circle(57, 23, 1.7),
            circle(55, 31, 1.7),
        ),
        fill=circle(47, 25, 2.2),
    ))

    # 3. item_optics.svg -> material.optics (SalvageMaterial.Optics)
    # Mounted biconvex optical lens in top/bottom clamps splitting an incident ray into a 3-ray fan.
    specs.append(IconSpec(
        filename="item_optics.svg",
        icon_id="optics",
        kind="item",
        localization_key="material.optics",
        code_symbol="SalvageMaterial.Optics",
        label="OPTICS",
        primary=(
            "M28 12A25 25 0 0 1 28 52A25 25 0 0 1 28 12Z",
            polyline([(21, 8), (35, 8), (35, 13), (21, 13)], closed=True),
            polyline([(21, 51), (35, 51), (35, 56), (21, 56)], closed=True),
            line(7, 32, 20, 32),
            line(36, 26, 56, 16),
            line(36, 32, 57, 32),
            line(36, 38, 56, 48),
        ),
        detail=(
            line(20, 32, 36, 26),
            line(20, 32, 36, 32),
            line(20, 32, 36, 38),
            line(28, 13, 28, 51),
        ),
        fill=circle(57, 32, 2.1),
    ))

    # 4. item_circuit_chips.svg -> material.circuit_chips (SalvageMaterial.CircuitChips)
    # Chamfered QFP IC microchip package with 16 gull-wing pins and internal silicon die traces.
    specs.append(IconSpec(
        filename="item_circuit_chips.svg",
        icon_id="circuit_chips",
        kind="item",
        localization_key="material.circuit_chips",
        code_symbol="SalvageMaterial.CircuitChips",
        label="CHIPS",
        primary=(
            polyline([
                (16, 22), (22, 16), (48, 16), (48, 48), (16, 48),
            ], closed=True),
            # Top pins
            line(23, 9, 23, 16), line(29, 9, 29, 16), line(35, 9, 35, 16), line(41, 9, 41, 16),
            # Bottom pins
            line(23, 48, 23, 55), line(29, 48, 29, 55), line(35, 48, 35, 55), line(41, 48, 41, 55),
            # Left pins
            line(9, 23, 16, 23), line(9, 29, 16, 29), line(9, 35, 16, 35), line(9, 41, 16, 41),
            # Right pins
            line(48, 23, 55, 23), line(48, 29, 55, 29), line(48, 35, 55, 35), line(48, 41, 55, 41),
        ),
        detail=(
            round_rect(24, 24, 16, 16, 1.8),
            line(29, 20, 29, 24), line(35, 20, 35, 24),
            line(29, 40, 29, 44), line(35, 40, 35, 44),
            line(20, 29, 24, 29), line(20, 35, 24, 35),
            line(40, 29, 44, 29), line(40, 35, 44, 35),
            line(28, 32, 36, 32), line(32, 28, 32, 36),
        ),
        fill=circle(22, 22, 1.9),
    ))

    # 5. item_orange_beads.svg -> material.orange_beads (SalvageMaterial.OrangeBeads)
    # Cluster of 5 volcanic pyroclastic glass spherules of graded radii with crescent highlights.
    specs.append(IconSpec(
        filename="item_orange_beads.svg",
        icon_id="orange_beads",
        kind="item",
        localization_key="material.orange_beads",
        code_symbol="SalvageMaterial.OrangeBeads",
        label="BEADS",
        primary=(
            circle(23, 34, 11.5),
            circle(41, 21, 8.5),
            circle(42, 43, 7.5),
            circle(16, 15, 4.8),
            circle(26, 52, 4.2),
        ),
        detail=(
            arc(23, 34, 7.5, 200, 295),
            arc(41, 21, 5.2, 200, 295),
            arc(42, 43, 4.4, 200, 295),
            arc(16, 15, 2.4, 205, 290),
        ),
        fill=circle(26, 52, 1.9),
    ))

    # 6. item_ilmenite.svg -> material.ilmenite (SalvageMaterial.Ilmenite)
    # Asymmetric craggy twin-peaked lunar basalt ore chunk with flat cleaved bottom and sharp facet planes.
    specs.append(IconSpec(
        filename="item_ilmenite.svg",
        icon_id="ilmenite",
        kind="item",
        localization_key="material.ilmenite",
        code_symbol="SalvageMaterial.Ilmenite",
        label="ILMENITE",
        primary=(
            polyline([
                (8, 47), (11, 26), (22, 11), (31, 21),
                (44, 9), (56, 25), (52, 48), (35, 54),
            ], closed=True),
        ),
        detail=(
            line(22, 11, 29, 35),
            line(31, 21, 29, 35),
            line(44, 9, 42, 33),
            line(42, 33, 29, 35),
            line(8, 47, 29, 35),
            line(29, 35, 35, 54),
            line(42, 33, 52, 48),
            line(11, 26, 29, 35),
        ),
        fill=polyline([(31, 21), (37, 27), (29, 35)], closed=True),
    ))

    # 7. item_water_ice.svg -> material.water_ice (SalvageMaterial.WaterIce)
    # 6-pointed dendritic frost snowflake crystal with branched tips and hexagonal core.
    ice_primary: List[str] = [
        regular_polygon(32, 32, 9.0, 6, start_deg=-90.0),
    ]
    ice_detail: List[str] = []
    for k in range(6):
        ang_deg = -90.0 + k * 60.0
        a = math.radians(ang_deg)
        x_tip = 32.0 + 24.5 * math.cos(a)
        y_tip = 32.0 + 24.5 * math.sin(a)
        ice_primary.append(line(32, 32, x_tip, y_tip))
        # Outer chevron branches at radius 15.5
        bx = 32.0 + 15.5 * math.cos(a)
        by = 32.0 + 15.5 * math.sin(a)
        b_left = math.radians(ang_deg - 38.0)
        b_right = math.radians(ang_deg + 38.0)
        ice_detail.append(polyline([
            (bx + 6.2 * math.cos(b_left), by + 6.2 * math.sin(b_left)),
            (bx, by),
            (bx + 6.2 * math.cos(b_right), by + 6.2 * math.sin(b_right)),
        ], closed=False))
    specs.append(IconSpec(
        filename="item_water_ice.svg",
        icon_id="water_ice",
        kind="item",
        localization_key="material.water_ice",
        code_symbol="SalvageMaterial.WaterIce",
        label="WATER ICE",
        primary=tuple(ice_primary),
        detail=tuple(ice_detail),
        fill=circle(32, 32, 2.4),
    ))

    # 8. item_rare_earths.svg -> material.rare_earths (SalvageMaterial.RareEarths)
    # Three intersecting 60-degree tilted lanthanide magnetic orbitals around a heavy nucleus.
    specs.append(IconSpec(
        filename="item_rare_earths.svg",
        icon_id="rare_earths",
        kind="item",
        localization_key="material.rare_earths",
        code_symbol="SalvageMaterial.RareEarths",
        label="RARE EARTHS",
        primary=(
            tilted_ellipse(32, 32, 23.5, 9.2, 30.0),
            tilted_ellipse(32, 32, 23.5, 9.2, -30.0),
            circle(32, 32, 5.2),
        ),
        detail=(
            tilted_ellipse(32, 32, 9.2, 23.5, 0.0),
            circle(32, 8.5, 2.1),
            circle(11.6, 43.8, 2.1),
            circle(52.4, 43.8, 2.1),
        ),
        fill=circle(32, 32, 2.4),
    ))

    # 9. item_glass.svg -> material.glass (SalvageMaterial.Glass)
    # Octagonal bevel-cut lunar viewport glass pane with mounting corner notches and specular diagonal streaks.
    specs.append(IconSpec(
        filename="item_glass.svg",
        icon_id="glass",
        kind="item",
        localization_key="material.glass",
        code_symbol="SalvageMaterial.Glass",
        label="GLASS",
        primary=(
            polyline([
                (19, 9), (45, 9), (54, 18), (54, 46),
                (45, 55), (19, 55), (10, 46), (10, 18),
            ], closed=True),
        ),
        detail=(
            polyline([
                (22, 14), (42, 14), (49, 21), (49, 43),
                (42, 50), (22, 50), (15, 43), (15, 21),
            ], closed=True),
            line(21, 41, 41, 21),
            line(29, 43, 43, 29),
            line(21, 30, 30, 21),
            line(10, 18, 15, 21),
            line(54, 18, 49, 21),
            line(54, 46, 49, 43),
            line(10, 46, 15, 43),
        ),
        fill=circle(23, 19, 1.7),
    ))

    # 10. item_alloy.svg -> material.alloy (SalvageMaterial.Alloy)
    # Two stacked trapezoidal smelted metal bullion bars with bevelled inner ridges and metallurgy hallmark.
    specs.append(IconSpec(
        filename="item_alloy.svg",
        icon_id="alloy",
        kind="item",
        localization_key="material.alloy",
        code_symbol="SalvageMaterial.Alloy",
        label="ALLOY",
        primary=(
            polyline([(7, 52), (13, 36), (51, 36), (57, 52)], closed=True),
            polyline([(14, 36), (20, 20), (44, 20), (50, 36)], closed=True),
        ),
        detail=(
            polyline([(13, 48), (17, 40), (47, 40), (51, 48)], closed=True),
            polyline([(20, 32), (23, 24), (41, 24), (44, 32)], closed=True),
            line(7, 52, 13, 48),
            line(57, 52, 51, 48),
            line(14, 36, 20, 32),
            line(50, 36, 44, 32),
        ),
        fill=polyline([(29, 44), (32, 41), (35, 44), (32, 47)], closed=True),
    ))

    # 11. item_coolant.svg -> material.coolant (SalvageMaterial.Coolant)
    # Wide-shouldered chamfered cryo-dewar flask with top T-valve handwheel, shoulder guards, and cryo droplet.
    specs.append(IconSpec(
        filename="item_coolant.svg",
        icon_id="coolant",
        kind="item",
        localization_key="material.coolant",
        code_symbol="SalvageMaterial.Coolant",
        label="COOLANT",
        primary=(
            polyline([
                (17, 23), (24, 17), (40, 17), (47, 23),
                (47, 49), (41, 55), (23, 55), (17, 49),
            ], closed=True),
            line(32, 10, 32, 17),
            line(20, 10, 44, 10),
            line(20, 8, 20, 12),
            line(44, 8, 44, 12),
            polyline([(17, 24), (12, 26), (12, 34), (17, 36)], closed=False),
            polyline([(47, 24), (52, 26), (52, 34), (47, 36)], closed=False),
        ),
        detail=(
            "M32 25C32 25 38 33 38 37A6 6 0 1 1 26 37C26 33 32 25 32 25Z",
            line(22, 47, 42, 47),
            line(25, 51, 39, 51),
        ),
        fill=circle(32, 37, 2.3),
    ))

    # 12. item_crystal.svg -> material.crystal (SalvageMaterial.Crystal)
    # Twin terminated hexagonal quartz crystal spires with a side crystal spur on a mineral matrix base.
    specs.append(IconSpec(
        filename="item_crystal.svg",
        icon_id="crystal",
        kind="item",
        localization_key="material.crystal",
        code_symbol="SalvageMaterial.Crystal",
        label="CRYSTAL",
        primary=(
            polyline([(19, 53), (19, 21), (28, 7), (37, 21), (37, 53)], closed=True),
            polyline([(37, 30), (49, 17), (54, 29), (45, 53), (37, 53)], closed=False),
            polyline([(19, 35), (10, 27), (8, 39), (19, 53)], closed=False),
            line(12, 53, 52, 53),
        ),
        detail=(
            line(28, 7, 28, 53),
            polyline([(19, 21), (28, 25), (37, 21)], closed=False),
            line(49, 17, 41, 53),
            line(10, 27, 19, 44),
        ),
        fill=polyline([(28, 7), (37, 21), (28, 25)], closed=True),
    ))

    # 13. item_superconductor.svg -> material.superconductor (SalvageMaterial.Superconductor)
    # Cryo-magnetic toroid ring with 4 bold protruding clamp blocks at cardinal points and levitating flux star.
    specs.append(IconSpec(
        filename="item_superconductor.svg",
        icon_id="superconductor",
        kind="item",
        localization_key="material.superconductor",
        code_symbol="SalvageMaterial.Superconductor",
        label="SUPERCOND.",
        primary=(
            circle(32, 32, 18.5),
            circle(32, 32, 10.0),
            round_rect(27, 7, 10, 8, 1.5),
            round_rect(27, 49, 10, 8, 1.5),
            round_rect(7, 27, 8, 10, 1.5),
            round_rect(49, 27, 8, 10, 1.5),
        ),
        detail=(
            line(39.1, 24.9, 45.1, 18.9),
            line(24.9, 24.9, 18.9, 18.9),
            line(24.9, 39.1, 18.9, 45.1),
            line(39.1, 39.1, 45.1, 45.1),
            polyline([(32, 24), (35, 29), (40, 32), (35, 35), (32, 40), (29, 35), (24, 32), (29, 29)], closed=True),
        ),
        fill=polyline([(32, 27.5), (36.5, 32), (32, 36.5), (27.5, 32)], closed=True),
    ))

    return specs


def _build_kit_specs() -> List[IconSpec]:
    specs: List[IconSpec] = []

    # 1. kit_cutter.svg -> RoverSocket.Cutter (0) / RoverKitPiece.FocusLens (kit_focus_lens.png)
    # Circular brass cheek ring with bolted flange tabs, inner convex lens dome, and side-mounted
    # cylindrical emitter barrel with twin horizontal fork prongs projecting a converging laser beam.
    specs.append(IconSpec(
        filename="kit_cutter.svg",
        icon_id="cutter",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Cutter / RoverKitPiece.FocusLens",
        label="CUTTER · FOCUS LENS",
        socket_index=0,
        primary=(
            circle(20, 32, 12.0),
            polyline([(17, 20), (17, 15), (23, 15), (23, 20)], closed=False),
            polyline([(17, 44), (17, 49), (23, 49), (23, 44)], closed=False),
            polyline([(8, 29), (5.5, 29), (5.5, 35), (8, 35)], closed=False),
            polyline([(32, 26), (43, 26), (43, 38), (32, 38)], closed=False),
            line(43, 28, 48, 28),
            line(43, 36, 48, 36),
            line(48, 28, 57, 32),
            line(48, 36, 57, 32),
        ),
        detail=(
            circle(20, 32, 7.5),
            arc(20, 32, 4.5, 200, 300),
            line(36, 26, 36, 38),
            line(39.5, 26, 39.5, 38),
            line(43, 32, 57, 32),
        ),
        fill=circle(57, 32, 2.3),
    ))

    # 2. kit_tether.svg -> RoverSocket.Tether (1) / RoverKitPiece.TetherRing (kit_tether_ring.png)
    # Segmented brass collar ring with 8 radial clamp lugs around the circular emitter pod,
    # paying out a taut tether cable to a 3-prong grapple claw hook.
    tether_lugs: List[str] = []
    for k in range(8):
        ang = math.radians(k * 45.0)
        x0 = 22.0 + 11.5 * math.cos(ang)
        y0 = 38.0 + 11.5 * math.sin(ang)
        x1 = 22.0 + 15.2 * math.cos(ang)
        y1 = 38.0 + 15.2 * math.sin(ang)
        tether_lugs.append(line(x0, y0, x1, y1))
    specs.append(IconSpec(
        filename="kit_tether.svg",
        icon_id="tether",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Tether / RoverKitPiece.TetherRing",
        label="TETHER · TETHER RING",
        socket_index=1,
        primary=(
            circle(22, 38, 11.5),
            *tether_lugs,
            line(30, 30, 49, 13),
            arc(46, 16, 7.5, -135, 45),
            polyline([(38, 13), (40.7, 10.7), (43, 14)], closed=False),
            polyline([(48, 19), (51.3, 21.3), (49, 24)], closed=False),
        ),
        detail=(
            circle(22, 38, 7.0),
            line(19, 38, 25, 38),
            line(22, 35, 22, 41),
        ),
        fill=circle(46, 16, 2.1),
    ))

    # 3. kit_lamp.svg -> RoverSocket.Lamp (2) / RoverKitPiece.LampBar (kit_lamp_bar.png)
    # Front tubular bumper bar carrying twin large caged headlamps (with horizontal protective grill bars)
    # flanking a smaller central round spotlight, with upward illumination rays.
    specs.append(IconSpec(
        filename="kit_lamp.svg",
        icon_id="lamp",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Lamp / RoverKitPiece.LampBar",
        label="LAMP · LAMP BAR",
        socket_index=2,
        primary=(
            round_rect(6, 43, 52, 5, 2.2),
            polyline([(15, 48), (15, 54), (21, 54), (21, 48)], closed=False),
            polyline([(43, 48), (43, 54), (49, 54), (49, 48)], closed=False),
            circle(16, 31, 9.0),
            circle(48, 31, 9.0),
            circle(32, 32, 4.6),
        ),
        detail=(
            line(7.5, 28, 24.5, 28),
            line(7.5, 34, 24.5, 34),
            line(39.5, 28, 56.5, 28),
            line(39.5, 34, 56.5, 34),
            line(13, 18, 9, 9),
            line(20, 18, 19, 8),
            line(32, 23, 32, 11),
            line(44, 18, 45, 8),
            line(51, 18, 55, 9),
        ),
        fill=circle(32, 32, 2.1),
    ))

    # 4. kit_belly.svg -> RoverSocket.Belly (3) / RoverKitPiece.HoverCoils (kit_hover_coils.png)
    # Underbody V-braced mounting plate with twin vertical helical induction coil stacks,
    # beveled hexagonal thruster pads underneath, and a central upward leap chevron.
    specs.append(IconSpec(
        filename="kit_belly.svg",
        icon_id="belly",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Belly / RoverKitPiece.HoverCoils",
        label="BELLY · HOVER COILS",
        socket_index=3,
        primary=(
            round_rect(7, 10, 50, 5, 2.0),
            polyline([(24, 15), (32, 24), (40, 15)], closed=False),
            # Left induction coil stack + beveled thruster pad
            round_rect(8, 18, 15, 5, 2.2),
            round_rect(8, 25, 15, 5, 2.2),
            round_rect(8, 32, 15, 5, 2.2),
            polyline([(7, 39), (24, 39), (22, 45), (9, 45)], closed=True),
            # Right induction coil stack + beveled thruster pad
            round_rect(41, 18, 15, 5, 2.2),
            round_rect(41, 25, 15, 5, 2.2),
            round_rect(41, 32, 15, 5, 2.2),
            polyline([(40, 39), (57, 39), (55, 45), (42, 45)], closed=True),
        ),
        detail=(
            polyline([(27, 35), (32, 28), (37, 35)], closed=False),
            line(32, 28, 32, 43),
            arc(15.5, 44, 7.5, 35, 145),
            arc(15.5, 44, 12.0, 45, 135),
            arc(48.5, 44, 7.5, 35, 145),
            arc(48.5, 44, 12.0, 45, 135),
        ),
        fill=circle(32, 22, 2.0),
    ))

    # 5. kit_flanks.svg -> RoverSocket.Flanks (4) / RoverKitPiece.CapacitorDrums (kit_capacitor_drum.png)
    # Horizontal cylindrical capacitor drum with domed end-caps, two heavy vertical clamp straps
    # with top bolt blocks, central glowing energy band, and top horizontal busbar rod.
    specs.append(IconSpec(
        filename="kit_flanks.svg",
        icon_id="flanks",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Flanks / RoverKitPiece.CapacitorDrums",
        label="FLANKS · CAPACITOR DRUMS",
        socket_index=4,
        primary=(
            line(16, 22, 48, 22),
            line(16, 48, 48, 48),
            arc(16, 35, 13.0, 90, 270),
            arc(48, 35, 13.0, -90, 90),
            round_rect(19, 19, 5.5, 32, 1.5),
            round_rect(39.5, 19, 5.5, 32, 1.5),
            line(14, 13, 50, 13),
            line(21.8, 13, 21.8, 19),
            line(42.2, 13, 42.2, 19),
        ),
        detail=(
            line(29, 22, 29, 48),
            line(35, 22, 35, 48),
            line(16, 22, 16, 48),
            line(48, 22, 48, 48),
            polyline([(30.5, 30), (34, 35), (30.5, 40)], closed=False),
            circle(21.8, 13, 1.8),
            circle(42.2, 13, 1.8),
        ),
        fill=polyline([(30.5, 32), (33.5, 32), (33.5, 38), (30.5, 38)], closed=True),
    ))

    # 6. kit_panniers.svg -> RoverSocket.Panniers (5) / RoverKitPiece.SideBins (kit_side_bin.png)
    # Rectangular side pannier chest with overhanging beveled lid, 4 glowing status pips along
    # the upper rim, two vertical latch straps, and a lower horizontal grab rail.
    specs.append(IconSpec(
        filename="kit_panniers.svg",
        icon_id="panniers",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Panniers / RoverKitPiece.SideBins",
        label="PANNIERS · SIDE BINS",
        socket_index=5,
        primary=(
            round_rect(10, 22, 44, 26, 2.5),
            polyline([(8, 22), (12, 14), (52, 14), (56, 22)], closed=True),
            round_rect(18, 13, 5, 36, 1.2),
            round_rect(41, 13, 5, 36, 1.2),
            polyline([(16, 48), (16, 54), (48, 54), (48, 48)], closed=False),
        ),
        detail=(
            line(10, 35, 18, 35),
            line(23, 35, 41, 35),
            line(46, 35, 54, 35),
            circle(26.5, 26.5, 1.3),
            circle(30.2, 26.5, 1.3),
            circle(33.8, 26.5, 1.3),
            circle(37.5, 26.5, 1.3),
        ),
        fill=polyline([(29, 33), (35, 33), (35, 37), (29, 37)], closed=True),
    ))

    # 7. kit_back.svg -> RoverSocket.Back (6) / RoverKitPiece.BatterySpine (kit_battery_spine.png)
    # Long cylindrical segmented battery spine cradled in a 6-slot notched comb rack base,
    # with 3 glowing ring bands and a top busbar rod fastened by 3 hex bolts.
    specs.append(IconSpec(
        filename="kit_back.svg",
        icon_id="back",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Back / RoverKitPiece.BatterySpine",
        label="BACK · BATTERY SPINE",
        socket_index=6,
        primary=(
            round_rect(8, 20, 48, 18, 5.5),
            line(13, 12, 51, 12),
            line(18, 12, 18, 20),
            line(32, 12, 32, 20),
            line(46, 12, 46, 20),
            polyline([(7, 38), (7, 50), (57, 50), (57, 38)], closed=True),
            line(15.5, 38, 15.5, 50),
            line(23.8, 38, 23.8, 50),
            line(32.0, 38, 32.0, 50),
            line(40.2, 38, 40.2, 50),
            line(48.5, 38, 48.5, 50),
        ),
        detail=(
            line(19.5, 20, 19.5, 38),
            line(22.0, 20, 22.0, 38),
            line(30.8, 20, 30.8, 38),
            line(33.2, 20, 33.2, 38),
            line(42.0, 20, 42.0, 38),
            line(44.5, 20, 44.5, 38),
            circle(18, 12, 2.0),
            circle(32, 12, 2.0),
            circle(46, 12, 2.0),
        ),
        fill=circle(32, 12, 2.0),
    ))

    # 8. kit_hold.svg -> RoverSocket.Hold (7) / RoverKitPiece.CargoRack (kit_cargo_rack.png)
    # Rear fold-down tubular cargo cradle basket with slatted floor deck, two vertical tie-down straps
    # with square centre buckles, side retaining cage walls, angled side struts, and top crossbar sleeve.
    specs.append(IconSpec(
        filename="kit_hold.svg",
        icon_id="hold",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Hold / RoverKitPiece.CargoRack",
        label="HOLD · CARGO RACK",
        socket_index=7,
        primary=(
            line(11, 13, 53, 13),
            round_rect(25, 10, 14, 6, 2.0),
            polyline([(9, 23), (7, 46), (57, 46), (55, 23)], closed=False),
            polyline([(7, 46), (7, 52), (57, 52), (57, 46)], closed=True),
            line(13, 13, 8, 36),
            line(51, 13, 56, 36),
            line(22, 13, 22, 52),
            line(42, 13, 42, 52),
            round_rect(19, 29, 6, 6, 1.2),
            round_rect(39, 29, 6, 6, 1.2),
        ),
        detail=(
            line(8, 32, 19, 32),
            line(25, 32, 39, 32),
            line(45, 32, 56, 32),
            line(15, 46, 15, 52),
            line(28, 46, 28, 52),
            line(36, 46, 36, 52),
            line(49, 46, 49, 52),
        ),
        fill=polyline([(27, 11.5), (37, 11.5), (37, 14.5), (27, 14.5)], closed=True),
    ))

    # 9. kit_wing.svg -> RoverSocket.Wing (8) / RoverKitPiece.SolarCell (upgraded_back_3q.png solar wing)
    # Bevel-framed rectangular solar panel wing subdivided into a 3 x 2 grid of 6 photovoltaic cells
    # with silver busbars and two rear hinge mounting brackets.
    specs.append(IconSpec(
        filename="kit_wing.svg",
        icon_id="wing",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Wing / RoverKitPiece.SolarCell",
        label="WING · SOLAR CELL",
        socket_index=8,
        primary=(
            polyline([(9, 16), (55, 16), (58, 44), (6, 44)], closed=True),
            line(7.5, 30, 56.5, 30),
            line(24.5, 16, 23.5, 44),
            line(39.5, 16, 40.5, 44),
            polyline([(17, 44), (17, 52), (24, 52), (24, 44)], closed=False),
            polyline([(40, 44), (40, 52), (47, 52), (47, 44)], closed=False),
        ),
        detail=(
            line(16.5, 19, 15.5, 41),
            line(32.0, 19, 32.0, 41),
            line(47.5, 19, 48.5, 41),
            line(12, 23, 52, 23),
            line(10, 37, 54, 37),
        ),
        fill=circle(50, 21, 1.9),
    ))

    # 10. kit_deck.svg -> RoverSocket.Deck (9) / RoverKitPiece.TapeDeck (upgraded_back_3q.png tape deck)
    # Compact armoured cassette deck with recessed tape window, twin spoked reel hubs, magnetic tape bridge,
    # and 4 mechanical piano-key transport buttons along the bottom.
    specs.append(IconSpec(
        filename="kit_deck.svg",
        icon_id="deck",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Deck / RoverKitPiece.TapeDeck",
        label="DECK · TAPE DECK",
        socket_index=9,
        primary=(
            round_rect(8, 12, 48, 39, 4.0),
            round_rect(13, 17, 38, 21, 2.5),
            circle(23, 27.5, 5.5),
            circle(41, 27.5, 5.5),
        ),
        detail=(
            line(23, 22, 41, 22),
            polyline([(28, 38), (30, 34), (34, 34), (36, 38)], closed=False),
            line(23, 22, 23, 24.5),
            line(18.2, 30.2, 20.5, 28.8),
            line(27.8, 30.2, 25.5, 28.8),
            line(41, 22, 41, 24.5),
            line(36.2, 30.2, 38.5, 28.8),
            line(45.8, 30.2, 43.5, 28.8),
            round_rect(13, 42, 7.5, 5, 1.0),
            round_rect(23, 42, 7.5, 5, 1.0),
            round_rect(33, 42, 7.5, 5, 1.0),
            round_rect(43, 42, 7.5, 5, 1.0),
        ),
        fill=circle(46.8, 44.5, 1.7),
    ))

    # 11. kit_wheels.svg -> RoverSocket.Wheels (10, 0.6 socket) / RoverAbility.MagneticTreads
    # 07's deep-lugged wheel with 12 protruding outer tread blocks, inner rim, 6 spoke ribs,
    # and magnetic grip flux lines locking onto the lunar surface below.
    wheel_lugs: List[str] = []
    wheel_spokes: List[str] = []
    for k in range(12):
        ang = math.radians(k * 30.0)
        x0 = 32.0 + 15.0 * math.cos(ang)
        y0 = 28.0 + 15.0 * math.sin(ang)
        x1 = 32.0 + 18.8 * math.cos(ang)
        y1 = 28.0 + 18.8 * math.sin(ang)
        wheel_lugs.append(line(x0, y0, x1, y1))
    for k in range(6):
        ang = math.radians(k * 60.0)
        x0 = 32.0 + 4.5 * math.cos(ang)
        y0 = 28.0 + 4.5 * math.sin(ang)
        x1 = 32.0 + 10.0 * math.cos(ang)
        y1 = 28.0 + 10.0 * math.sin(ang)
        wheel_spokes.append(line(x0, y0, x1, y1))
    specs.append(IconSpec(
        filename="kit_wheels.svg",
        icon_id="wheels",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Wheels / RoverAbility.MagneticTreads",
        label="WHEELS · MAG TREADS",
        socket_index=10,
        dim_in_05=True,
        primary=(
            circle(32, 28, 15.0),
            circle(32, 28, 10.0),
            *wheel_lugs,
            line(9, 54, 55, 54),
            polyline([(15, 47), (20, 54), (24, 49)], closed=False),
            polyline([(49, 47), (44, 54), (40, 49)], closed=False),
        ),
        detail=(
            circle(32, 28, 4.5),
            *wheel_spokes,
            arc(32, 28, 22.0, 55, 125),
        ),
        fill=circle(32, 28, 2.2),
    ))

    # 12. kit_antenna.svg -> RoverSocket.Antenna (11, 0.6 socket) / RoverAbility.WideSonar
    # 07's oval brass-rimmed louvered rear transducer grille (upgraded_back_3q.png) + vertical
    # antenna mast with top beacon bulb radiating wide concentric sonar wavefront arcs.
    specs.append(IconSpec(
        filename="kit_antenna.svg",
        icon_id="antenna",
        kind="kit",
        localization_key="",
        code_symbol="RoverSocket.Antenna / RoverAbility.WideSonar",
        label="ANTENNA · WIDE SONAR",
        socket_index=11,
        dim_in_05=True,
        primary=(
            round_rect(15, 37, 34, 18, 9.0),
            line(32, 37, 32, 19.5),
            circle(32, 16, 3.2),
            arc(32, 16, 10.0, -35, 35),
            arc(32, 16, 10.0, 145, 215),
            arc(32, 16, 17.5, -35, 35),
            arc(32, 16, 17.5, 145, 215),
        ),
        detail=(
            line(22, 41.5, 42, 41.5),
            line(20, 44.5, 44, 44.5),
            line(20, 47.5, 44, 47.5),
            line(22, 50.5, 42, 50.5),
            line(27, 37, 37, 37),
        ),
        fill=circle(32, 16, 2.0),
    ))

    return specs


def _build_struct_specs() -> List[IconSpec]:
    specs: List[IconSpec] = []

    # 1. struct_charging_pad.svg -> structure.charging_pad.name (StructureIds.ChargingPad)
    # Chamfered octagonal deck pad with dual induction rings, corner guide lugs, and central charge bolt.
    specs.append(IconSpec(
        filename="Structures/struct_charging_pad.svg",
        icon_id="charging_pad",
        kind="struct",
        localization_key="structure.charging_pad.name",
        code_symbol="StructureIds.ChargingPad",
        label="CHARGING PAD",
        primary=(
            polyline([
                (18, 10), (46, 10), (54, 18), (54, 46),
                (46, 54), (18, 54), (10, 46), (10, 18),
            ], closed=True),
            circle(32, 32, 15.0),
        ),
        detail=(
            circle(32, 32, 9.5),
            line(14, 22, 18, 18),
            line(46, 18, 50, 22),
            line(50, 42, 46, 46),
            line(18, 46, 14, 42),
            line(32, 10, 32, 17),
            line(32, 47, 32, 54),
            line(10, 32, 17, 32),
            line(47, 32, 54, 32),
        ),
        fill=polyline([(34, 24), (27, 33), (32, 33), (30, 40), (37, 31), (32, 31)], closed=True),
    ))

    # 2. struct_solar_array.svg -> structure.solar_array.name (StructureIds.SolarArray)
    # Tilted twin-wing photovoltaic panel array on an articulated central mast and tripod plinth.
    specs.append(IconSpec(
        filename="Structures/struct_solar_array.svg",
        icon_id="solar_array",
        kind="struct",
        localization_key="structure.solar_array.name",
        code_symbol="StructureIds.SolarArray",
        label="SOLAR ARRAY",
        primary=(
            polyline([(8, 16), (52, 12), (56, 34), (12, 38)], closed=True),
            line(32, 36, 32, 50),
            polyline([(18, 54), (24, 50), (40, 50), (46, 54)], closed=True),
        ),
        detail=(
            line(10, 27, 54, 23),
            line(22.7, 14.7, 26.7, 36.7),
            line(37.3, 13.3, 41.3, 35.3),
            line(26, 44, 32, 39),
            line(38, 44, 32, 39),
        ),
        fill=circle(32, 39, 2.2),
    ))

    # 3. struct_battery_bank.svg -> structure.battery_bank.name (StructureIds.BatteryBank)
    # Armored three-cell accumulator cabinet with top busbar manifold and charge status bars.
    specs.append(IconSpec(
        filename="Structures/struct_battery_bank.svg",
        icon_id="battery_bank",
        kind="struct",
        localization_key="structure.battery_bank.name",
        code_symbol="StructureIds.BatteryBank",
        label="BATTERY BANK",
        primary=(
            round_rect(8, 16, 48, 36, 3.0),
            line(12, 11, 52, 11),
            line(16, 11, 16, 16),
            line(32, 11, 32, 16),
            line(48, 11, 48, 16),
            round_rect(12, 21, 10, 26, 2.0),
            round_rect(27, 21, 10, 26, 2.0),
            round_rect(42, 21, 10, 26, 2.0),
        ),
        detail=(
            line(14, 28, 20, 28),
            line(14, 34, 20, 34),
            line(14, 40, 20, 40),
            line(29, 28, 35, 28),
            line(29, 34, 35, 34),
            line(29, 40, 35, 40),
            line(44, 28, 50, 28),
            line(44, 34, 50, 34),
            line(44, 40, 50, 40),
            line(10, 55, 54, 55),
        ),
        fill=circle(32, 11, 2.2),
    ))

    # 4. struct_power_pylon.svg -> structure.power_pylon.name (StructureIds.PowerPylon)
    # Tapered lattice power relay tower with triple ceramic insulator cross-arms and top emitter sphere.
    specs.append(IconSpec(
        filename="Structures/struct_power_pylon.svg",
        icon_id="power_pylon",
        kind="struct",
        localization_key="structure.power_pylon.name",
        code_symbol="StructureIds.PowerPylon",
        label="POWER PYLON",
        primary=(
            polyline([(20, 52), (28, 16), (36, 16), (44, 52)], closed=True),
            round_rect(14, 52, 36, 4, 1.5),
            line(14, 23, 50, 23),
            line(17, 32, 47, 32),
            circle(32, 11, 3.5),
        ),
        detail=(
            line(26, 25, 38, 36),
            line(38, 25, 26, 36),
            line(24, 36, 40, 49),
            line(40, 36, 24, 49),
            line(14, 21, 14, 26),
            line(50, 21, 50, 26),
            line(17, 30, 17, 35),
            line(47, 30, 47, 35),
        ),
        fill=circle(32, 11, 2.2),
    ))

    # 5. struct_antenna_mast.svg -> structure.antenna_mast.name (StructureIds.AntennaMast)
    # Tall guyed communications mast with tilted parabolic dish, feed horn, and signal wave arcs.
    specs.append(IconSpec(
        filename="Structures/struct_antenna_mast.svg",
        icon_id="antenna_mast",
        kind="struct",
        localization_key="structure.antenna_mast.name",
        code_symbol="StructureIds.AntennaMast",
        label="ANTENNA MAST",
        primary=(
            polyline([(24, 54), (30, 22), (34, 22), (40, 54)], closed=True),
            line(12, 54, 52, 54),
            arc(28, 18, 10.5, 115, 295),
            line(28, 18, 38, 12),
        ),
        detail=(
            line(28, 32, 36, 40),
            line(36, 32, 28, 40),
            line(26, 42, 38, 51),
            line(38, 42, 26, 51),
            line(29, 27, 15, 54),
            line(35, 27, 49, 54),
            arc(38, 12, 7.0, -55, 25),
            arc(38, 12, 12.5, -55, 25),
        ),
        fill=circle(38, 12, 2.1),
    ))

    return specs


ITEM_SPECS: Tuple[IconSpec, ...] = tuple(_build_item_specs())
KIT_SPECS: Tuple[IconSpec, ...] = tuple(_build_kit_specs())
STRUCT_SPECS: Tuple[IconSpec, ...] = tuple(_build_struct_specs())
ALL_SPECS: Tuple[IconSpec, ...] = ITEM_SPECS + KIT_SPECS + STRUCT_SPECS
SPECS_BY_FILENAME: Dict[str, IconSpec] = {spec.filename: spec for spec in ALL_SPECS}


# ---------------------------------------------------------------------------
# SVG serialization & validation
# ---------------------------------------------------------------------------

def render_icon_svg(spec: IconSpec) -> str:
    """Renders `spec` into deterministic, newline-normalized SVG XML text."""
    lines = [
        f'<svg xmlns="{SVG_NAMESPACE}" viewBox="0 0 64 64" width="64" height="64">',
        f'  <g fill="none" stroke="currentColor" stroke-width="{PRIMARY_STROKE}" '
        f'stroke-linecap="round" stroke-linejoin="round">',
    ]
    if spec.fill:
        lines.append(
            f'    <path fill="currentColor" stroke="currentColor" stroke-width="{DETAIL_STROKE}" '
            f'stroke-linecap="round" stroke-linejoin="round" d="{spec.fill}"/>'
        )
    if spec.primary:
        primary_d = "".join(spec.primary)
        lines.append(
            f'    <path fill="none" stroke="currentColor" stroke-width="{PRIMARY_STROKE}" '
            f'stroke-linecap="round" stroke-linejoin="round" d="{primary_d}"/>'
        )
    if spec.detail:
        detail_d = "".join(spec.detail)
        lines.append(
            f'    <path fill="none" stroke="currentColor" stroke-width="{DETAIL_STROKE}" '
            f'stroke-linecap="round" stroke-linejoin="round" d="{detail_d}"/>'
        )
    lines.append("  </g>")
    lines.append("</svg>")
    return "\n".join(lines) + "\n"


def validate_svg_text(svg_text: str, filename: str = "<svg>") -> None:
    """
    Validates that `svg_text` obeys every rule in Logs/TASK.md:
      - viewBox="0 0 64 64"
      - strokes only (2.2 primary / 1.5 detail) with round caps and joins
      - at most one flat fill per icon
      - single colour (currentColor)
      - every path parses with SvgPathParser rules and stays inside [0, 64]x[0, 64].
    """
    root = ET.fromstring(svg_text)
    if root.attrib.get("viewBox") != "0 0 64 64":
        raise ValueError(f"{filename}: viewBox must be '0 0 64 64'")

    ns = {"svg": SVG_NAMESPACE}
    paths = root.findall(".//svg:path", ns)
    if not paths:
        raise ValueError(f"{filename}: contains no <path> elements")

    fill_count = 0
    for elem in root.iter():
        for attr in ("fill", "stroke"):
            val = elem.attrib.get(attr)
            if val is not None and val not in ("none", "currentColor"):
                raise ValueError(f"{filename}: disallowed colour {attr}='{val}' (must be currentColor or none)")

    for path_el in paths:
        fill_val = path_el.attrib.get("fill", "none")
        if fill_val == "currentColor":
            fill_count += 1
        sw = path_el.attrib.get("stroke-width")
        if sw not in (PRIMARY_STROKE, DETAIL_STROKE):
            raise ValueError(f"{filename}: path stroke-width '{sw}' must be {PRIMARY_STROKE} or {DETAIL_STROKE}")
        if path_el.attrib.get("stroke-linecap") != "round":
            raise ValueError(f"{filename}: path stroke-linecap must be 'round'")
        if path_el.attrib.get("stroke-linejoin") != "round":
            raise ValueError(f"{filename}: path stroke-linejoin must be 'round'")

        d = path_el.attrib.get("d", "")
        min_x, min_y, max_x, max_y = path_bounds(d)
        half_stroke = float(sw) * 0.5
        if (
            min_x - half_stroke < 0.0
            or min_y - half_stroke < 0.0
            or max_x + half_stroke > VIEWBOX_SIZE
            or max_y + half_stroke > VIEWBOX_SIZE
        ):
            raise ValueError(
                f"{filename}: path bounds ({min_x:.2f}, {min_y:.2f})..({max_x:.2f}, {max_y:.2f}) "
                f"with stroke {sw} exceed 0..64 viewBox"
            )

    if fill_count > 1:
        raise ValueError(f"{filename}: has {fill_count} filled paths (at most 1 flat fill allowed)")


def build_all_icons(output_dir: Path = DEFAULT_ICON_DIR) -> List[Path]:
    """Deterministically writes all icon SVGs to `output_dir` and validates each one."""
    output_dir.mkdir(parents=True, exist_ok=True)
    written: List[Path] = []
    for spec in ALL_SPECS:
        svg_text = render_icon_svg(spec)
        validate_svg_text(svg_text, spec.filename)
        out_path = output_dir / spec.filename
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_bytes(svg_text.encode("utf-8"))
        written.append(out_path)
    return written


# ---------------------------------------------------------------------------
# CRT Review Sheet & R2 Layout Composite Renderer (Pillow)
# ---------------------------------------------------------------------------

CRT_BG = (16, 12, 8)
CRT_WELL = (24, 18, 12)
CRT_CARD = (20, 15, 10)
AMBER_BRIGHT = (243, 179, 64)
AMBER_MID = (196, 134, 42)
AMBER_DIM = (118, 79, 26)
AMBER_FAINT = (78, 52, 18)
AMBER_GHOST = (52, 36, 13)
CYAN_FOCUS = (103, 216, 214)
CREAM_TEXT = (242, 224, 190)


def _sample_path_polylines(
    d: str,
) -> List[List[Tuple[float, float]]]:
    """Converts an SVG `d` string via `parse_svg_path` into dense polylines in 0..64 space."""
    commands, values = parse_svg_path(d)
    polylines: List[List[Tuple[float, float]]] = []
    current_poly: List[Tuple[float, float]] = []
    cur = (0.0, 0.0)
    start = (0.0, 0.0)
    idx = 0

    for cmd in commands:
        if cmd == MOVE_TO:
            if len(current_poly) >= 2:
                polylines.append(current_poly)
            cur = start = (values[idx], values[idx + 1])
            idx += 2
            current_poly = [cur]
        elif cmd == CUBIC_TO:
            x1, y1, x2, y2, x3, y3 = values[idx:idx + 6]
            idx += 6
            x0, y0 = cur
            dist = math.hypot(x3 - x0, y3 - y0)
            # Check if collinear straight segment
            if math.hypot(x1 - x0, y1 - y0) < 1e-3 and math.hypot(x3 - x2, y3 - y2) < 1e-3:
                current_poly.append((x3, y3))
            else:
                steps = max(12, int(dist * 0.8) + 6)
                for s in range(1, steps + 1):
                    t = s / steps
                    mt = 1.0 - t
                    px = mt**3 * x0 + 3 * mt**2 * t * x1 + 3 * mt * t**2 * x2 + t**3 * x3
                    py = mt**3 * y0 + 3 * mt**2 * t * y1 + 3 * mt * t**2 * y2 + t**3 * y3
                    current_poly.append((px, py))
            cur = (x3, y3)
        elif cmd == ARC_TO:
            cx, cy, r, a0, a1, _sweep = values[idx:idx + 6]
            idx += 6
            steps = max(16, int(abs(a1 - a0) / 5.0) + 4)
            for s in range(1, steps + 1):
                ang = math.radians(a0 + (a1 - a0) * (s / steps))
                current_poly.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
            end_rad = math.radians(a1)
            cur = (cx + r * math.cos(end_rad), cy + r * math.sin(end_rad))
        elif cmd == CLOSE:
            if current_poly and math.hypot(current_poly[-1][0] - start[0], current_poly[-1][1] - start[1]) > 1e-3:
                current_poly.append(start)
            cur = start

    if len(current_poly) >= 2:
        polylines.append(current_poly)
    return polylines


def render_svg_file_to_rgba(
    svg_path: Path,
    size_px: int,
    color: Tuple[int, int, int, int],
    ss: int = 4,
):
    """
    Renders an SVG file from disk at `size_px x size_px` with 4x supersampling,
    round caps and round joins, tinted with `color`.
    """
    from PIL import Image, ImageDraw

    root = ET.fromstring(svg_path.read_text(encoding="utf-8"))
    ns = {"svg": SVG_NAMESPACE}
    canvas_sz = size_px * ss
    scale = canvas_sz / VIEWBOX_SIZE

    img = Image.new("RGBA", (canvas_sz, canvas_sz), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    for path_el in root.findall(".//svg:path", ns):
        d = path_el.attrib.get("d", "")
        sw = float(path_el.attrib.get("stroke-width", PRIMARY_STROKE))
        is_fill = path_el.attrib.get("fill") == "currentColor"
        polylines = _sample_path_polylines(d)

        px_w = max(1, int(round(sw * scale)))
        rad = px_w * 0.5

        for poly in polylines:
            scaled = [(x * scale, y * scale) for x, y in poly]
            if is_fill and len(scaled) >= 3:
                draw.polygon(scaled, fill=color)
            if len(scaled) >= 2:
                draw.line(scaled, fill=color, width=px_w, joint="curve")
                for vx, vy in scaled:
                    draw.ellipse((vx - rad, vy - rad, vx + rad, vy + rad), fill=color)

    return img.resize((size_px, size_px), Image.Resampling.LANCZOS)


def _load_font(name: str, size: int):
    from PIL import ImageFont

    path = FONT_DIR / name
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def _apply_crt_finish(base_img):
    """Applies warm phosphor bloom, horizontal CRT scanlines, and edge vignette."""
    from PIL import Image, ImageChops, ImageDraw, ImageFilter

    w, h = base_img.size
    rgb = base_img.convert("RGB")
    bloom = rgb.filter(ImageFilter.GaussianBlur(radius=3.2))
    bloom_faint = Image.blend(Image.new("RGB", (w, h), (0, 0, 0)), bloom, 0.24)
    lit = ImageChops.screen(rgb, bloom_faint).convert("RGBA")

    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    odraw = ImageDraw.Draw(overlay)
    for y in range(0, h, 3):
        odraw.line([(0, y), (w, y)], fill=(0, 0, 0, 54), width=1)
    odraw.rectangle((2, 2, w - 3, h - 3), outline=(118, 79, 26, 120), width=2)
    return Image.alpha_composite(lit, overlay).convert("RGB")


def render_sheet_items(svg_dir: Path, out_path: Path) -> None:
    """
    Renders Logs/icons/sheet_items.png: all 18 hold-item icons on the CRT background
    at both 32 px and 64 px with their real localization IDs.
    """
    from PIL import Image, ImageDraw

    w, h = 1280, 780
    img = Image.new("RGBA", (w, h), (*CRT_BG, 255))
    draw = ImageDraw.Draw(img)

    f_title = _load_font("ChakraPetch-Bold.ttf", 22)
    f_sub = _load_font("IBMPlexMono-Regular.ttf", 13)
    f_id = _load_font("IBMPlexMono-Bold.ttf", 13)
    f_key = _load_font("IBMPlexMono-Regular.ttf", 10)

    draw.text((32, 22), "07 CRT INVENTORY ICON SHEET — 18 HOLD ITEMS (64 PX & 32 PX)", font=f_title, fill=AMBER_BRIGHT)
    draw.text(
        (32, 50),
        "Source: Assets/_Project/UI/Icons/item_<id>.svg  |  viewBox 64x64  |  strokes 2.2 / 1.5  |  SvgPathParser M/C/A/Z",
        font=f_sub,
        fill=AMBER_MID,
    )

    cols = 6
    cell_w = 198
    cell_h = 216
    start_x = 32
    start_y = 84

    for idx, spec in enumerate(ITEM_SPECS):
        r = idx // cols
        c = idx % cols
        x0 = start_x + c * (cell_w + 6)
        y0 = start_y + r * (cell_h + 10)

        draw.rounded_rectangle((x0, y0, x0 + cell_w, y0 + cell_h), radius=8, fill=CRT_CARD, outline=AMBER_DIM, width=1)
        draw.text((x0 + 10, y0 + 10), f"{idx + 1:02d}. {spec.label}", font=f_id, fill=AMBER_BRIGHT)
        draw.text((x0 + 10, y0 + 28), f"item_{spec.icon_id}.svg", font=f_key, fill=CREAM_TEXT)
        draw.text((x0 + 10, y0 + 43), spec.localization_key, font=f_key, fill=AMBER_MID)

        # 64px well
        w64_x, w64_y = x0 + 16, y0 + 68
        draw.rounded_rectangle((w64_x, w64_y, w64_x + 84, w64_y + 84), radius=6, fill=CRT_WELL, outline=AMBER_DIM, width=1)
        svg_path = svg_dir / spec.filename
        ic64 = render_svg_file_to_rgba(svg_path, 64, (*AMBER_BRIGHT, 255))
        img.alpha_composite(ic64, (w64_x + 10, w64_y + 10))
        draw.text((w64_x + 24, w64_y + 90), "64 px", font=f_key, fill=AMBER_MID)

        # 32px well (actual 32px HUD/slot scale)
        w32_x, w32_y = x0 + 120, y0 + 94
        draw.rounded_rectangle((w32_x, w32_y, w32_x + 52, w32_y + 52), radius=5, fill=CRT_WELL, outline=AMBER_DIM, width=1)
        ic32 = render_svg_file_to_rgba(svg_path, 32, (*AMBER_BRIGHT, 255))
        img.alpha_composite(ic32, (w32_x + 10, w32_y + 10))
        draw.text((w32_x + 10, w32_y + 58), "32 px", font=f_key, fill=AMBER_MID)

        # Also show cyan focus state at 32px in top-right of card
        ic32_cyan = render_svg_file_to_rgba(svg_path, 24, (*CYAN_FOCUS, 255))
        img.alpha_composite(ic32_cyan, (x0 + cell_w - 34, y0 + 64))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    _apply_crt_finish(img).save(out_path, "PNG")


def render_sheet_kits(svg_dir: Path, out_path: Path) -> None:
    """
    Renders Logs/icons/sheet_kits.png: all 12 RoverSocket kit icons on the CRT background
    at both 64 px and 32 px across all three states (faint outline, lit amber, cyan focus).
    """
    from PIL import Image, ImageDraw

    w, h = 1360, 860
    img = Image.new("RGBA", (w, h), (*CRT_BG, 255))
    draw = ImageDraw.Draw(img)

    f_title = _load_font("ChakraPetch-Bold.ttf", 22)
    f_sub = _load_font("IBMPlexMono-Regular.ttf", 13)
    f_id = _load_font("IBMPlexMono-Bold.ttf", 13)
    f_key = _load_font("IBMPlexMono-Regular.ttf", 10)

    draw.text(
        (32, 20),
        "07 CRT EQUIPMENT KIT SHEET — 12 ROVER SOCKETS × 3 STATES (64 PX & 32 PX)",
        font=f_title,
        fill=AMBER_BRIGHT,
    )
    draw.text(
        (32, 48),
        "States: FAINT (empty socket outline)  |  LIT (installed amber)  |  CYAN (focus / drag target)  —  R1 3D kit silhouettes",
        font=f_sub,
        fill=AMBER_MID,
    )

    cols = 4
    cell_w = 318
    cell_h = 242
    start_x = 32
    start_y = 82

    states = [
        ("FAINT", (*AMBER_FAINT, 220), AMBER_GHOST),
        ("LIT", (*AMBER_BRIGHT, 255), AMBER_DIM),
        ("CYAN", (*CYAN_FOCUS, 255), CYAN_FOCUS),
    ]

    for idx, spec in enumerate(KIT_SPECS):
        r = idx // cols
        c = idx % cols
        x0 = start_x + c * (cell_w + 8)
        y0 = start_y + r * (cell_h + 10)

        draw.rounded_rectangle((x0, y0, x0 + cell_w, y0 + cell_h), radius=8, fill=CRT_CARD, outline=AMBER_DIM, width=1)
        tag_06 = " [0.6]" if spec.dim_in_05 else ""
        draw.text((x0 + 10, y0 + 9), f"[{spec.socket_index:02d}] {spec.label}{tag_06}", font=f_id, fill=AMBER_BRIGHT)
        draw.text((x0 + 10, y0 + 27), f"{spec.filename} ({spec.localization_key})", font=f_key, fill=CREAM_TEXT)

        svg_path = svg_dir / spec.filename
        for s_idx, (s_name, rgba, border_col) in enumerate(states):
            sx = x0 + 14 + s_idx * 99
            sy = y0 + 50
            # 64px box
            draw.rounded_rectangle((sx, sy, sx + 88, sy + 88), radius=6, fill=CRT_WELL, outline=border_col, width=1)
            ic64 = render_svg_file_to_rgba(svg_path, 64, rgba)
            img.alpha_composite(ic64, (sx + 12, sy + 12))

            # 32px box below
            s32_x = sx + 20
            s32_y = sy + 96
            draw.rounded_rectangle((s32_x, s32_y, s32_x + 48, s32_y + 48), radius=5, fill=CRT_WELL, outline=border_col, width=1)
            ic32 = render_svg_file_to_rgba(svg_path, 32, rgba)
            img.alpha_composite(ic32, (s32_x + 8, s32_y + 8))

            draw.text((sx + 10, s32_y + 52), f"{s_name} 64/32", font=f_key, fill=AMBER_MID)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    _apply_crt_finish(img).save(out_path, "PNG")


def render_in_layout(svg_dir: Path, out_path: Path) -> None:
    """
    Renders Logs/icons/in_layout.png (1152x768): composite putting the production
    SVG icons into the Round-2 Variant A layout (12 socket slots around 07 on the left,
    4x6 hold grid on the right with drag ghost and Base Storage drawer).
    """
    from PIL import Image, ImageDraw

    w, h = 1152, 768
    img = Image.new("RGBA", (w, h), (*CRT_BG, 255))
    draw = ImageDraw.Draw(img)

    f_hdr = _load_font("ChakraPetch-Bold.ttf", 20)
    f_sub = _load_font("IBMPlexMono-Bold.ttf", 14)
    f_num = _load_font("IBMPlexMono-Bold.ttf", 13)
    f_sm = _load_font("IBMPlexMono-Regular.ttf", 12)

    # Outer CRT bezel frame + header bar
    draw.rounded_rectangle((16, 14, w - 16, h - 14), radius=18, fill=CRT_BG, outline=AMBER_DIM, width=2)
    draw.line([(36, 58), (w - 36, 58)], fill=AMBER_DIM, width=2)
    draw.text((40, 26), "07 // ROVER SYSTEMS & CARGO HOLD", font=f_hdr, fill=AMBER_BRIGHT)
    draw.text((w - 310, 30), "[TAB] CLOSE   [LMB] DRAG / FIT", font=f_sm, fill=AMBER_MID)

    # Left Pane: 07 Chassis Diagram + 12 Equipment Kit Sockets
    left_box = (36, 74, 548, 704)
    draw.rounded_rectangle(left_box, radius=10, fill=CRT_CARD, outline=AMBER_DIM, width=1)
    draw.text((54, 88), "CHASSIS SOCKETS (12)", font=f_sub, fill=AMBER_BRIGHT)

    # Central schematic silhouette of 07 inside the left pane
    rcx, rcy = 292, 390
    # 4 rover wheels in schematic
    for wx in (rcx - 78, rcx + 52):
        for wy in (rcy - 92, rcy + 44):
            draw.rounded_rectangle((wx, wy, wx + 26, wy + 48), radius=5, fill=CRT_WELL, outline=AMBER_DIM, width=2)
    # Chassis body & mast schematic
    draw.rounded_rectangle((rcx - 56, rcy - 105, rcx + 56, rcy + 105), radius=14, fill=CRT_WELL, outline=AMBER_MID, width=2)
    draw.rounded_rectangle((rcx - 42, rcy - 88, rcx + 42, rcy - 28), radius=8, fill=CRT_CARD, outline=AMBER_DIM, width=1)
    draw.rounded_rectangle((rcx - 40, rcy - 16, rcx + 40, rcy + 78), radius=6, fill=CRT_CARD, outline=AMBER_DIM, width=1)
    draw.ellipse((rcx - 16, rcy - 68, rcx + 16, rcy - 36), outline=AMBER_BRIGHT, width=2)

    # 12 Socket callout slots arranged around 07's schematic
    # Each entry: (filename, slot_x, slot_y, anchor_x, anchor_y, state)
    socket_layout = [
        ("kit_lamp.svg", 220, 126, rcx, rcy - 105, "lit"),
        ("kit_cutter.svg", 316, 126, rcx + 28, rcy - 82, "cyan"),
        ("kit_tether.svg", 64, 186, rcx - 32, rcy - 62, "lit"),
        ("kit_antenna.svg", 436, 186, rcx + 22, rcy - 52, "faint"),
        ("kit_wing.svg", 64, 286, rcx - 42, rcy - 10, "lit"),
        ("kit_deck.svg", 436, 286, rcx + 42, rcy - 10, "lit"),
        ("kit_flanks.svg", 64, 386, rcx - 56, rcy + 18, "lit"),
        ("kit_panniers.svg", 436, 386, rcx + 56, rcy + 18, "lit"),
        ("kit_belly.svg", 64, 486, rcx - 20, rcy + 36, "faint"),
        ("kit_back.svg", 436, 486, rcx + 20, rcy + 52, "lit"),
        ("kit_wheels.svg", 168, 594, rcx - 65, rcy + 92, "faint"),
        ("kit_hold.svg", 360, 594, rcx, rcy + 105, "lit"),
    ]

    for fname, sx, sy, ax, ay, state in socket_layout:
        cx_s, cy_s = sx + 28, sy + 28
        line_col = CYAN_FOCUS if state == "cyan" else (AMBER_MID if state == "lit" else AMBER_FAINT)
        draw.line([(cx_s, cy_s), (ax, ay)], fill=line_col, width=1)
        draw.ellipse((ax - 3, ay - 3, ax + 3, ay + 3), fill=line_col)

        border = CYAN_FOCUS if state == "cyan" else (AMBER_BRIGHT if state == "lit" else AMBER_FAINT)
        bw = 2 if state in ("cyan", "lit") else 1
        draw.rounded_rectangle((sx, sy, sx + 56, sy + 56), radius=7, fill=CRT_WELL, outline=border, width=bw)

        rgba = (*CYAN_FOCUS, 255) if state == "cyan" else ((*AMBER_BRIGHT, 255) if state == "lit" else (*AMBER_FAINT, 210))
        icon_img = render_svg_file_to_rgba(svg_dir / fname, 40, rgba)
        img.alpha_composite(icon_img, (sx + 8, sy + 8))

        short_lbl = fname.replace("kit_", "").replace(".svg", "").upper()
        draw.text((sx + 4, sy + 59), short_lbl, font=f_sm, fill=line_col)

    # Right Pane: 24-Slot Hold Grid (6x4) with all 18 item icons + Base Storage bar
    right_box = (568, 74, w - 36, 566)
    draw.rounded_rectangle(right_box, radius=10, fill=CRT_CARD, outline=AMBER_DIM, width=1)
    draw.text((588, 88), "ROVER HOLD — 18 / 24 SLOTS", font=f_sub, fill=AMBER_BRIGHT)

    # 24-segment capacity bar
    for seg in range(24):
        bx = 830 + seg * 11
        by = 91
        fill_c = AMBER_BRIGHT if seg < 18 else CRT_WELL
        draw.rectangle((bx, by, bx + 8, by + 12), fill=fill_c, outline=AMBER_DIM, width=1)

    grid_x0, grid_y0 = 588, 122
    slot_sz = 78
    slot_gap = 10
    quantities = [8, 5, 3, 4, 12, 6, 4, 2, 5, 3, 2, 4, 1, 1, 1, 1, 1, 1]

    for s_idx in range(24):
        gr = s_idx // 6
        gc = s_idx % 6
        gx = grid_x0 + gc * (slot_sz + slot_gap)
        gy = grid_y0 + gr * (slot_sz + slot_gap)

        is_focus = s_idx == 2  # optics focused in cyan
        if s_idx < len(ITEM_SPECS):
            spec = ITEM_SPECS[s_idx]
            border = CYAN_FOCUS if is_focus else AMBER_MID
            bw = 2 if is_focus else 1
            draw.rounded_rectangle((gx, gy, gx + slot_sz, gy + slot_sz), radius=7, fill=CRT_WELL, outline=border, width=bw)
            rgba = (*CYAN_FOCUS, 255) if is_focus else (*AMBER_BRIGHT, 255)
            ic = render_svg_file_to_rgba(svg_dir / spec.filename, 52, rgba)
            img.alpha_composite(ic, (gx + 13, gy + 8))
            qty = quantities[s_idx]
            if qty > 1:
                draw.text((gx + slot_sz - 22, gy + slot_sz - 19), f"×{qty}", font=f_num, fill=CREAM_TEXT)
        else:
            draw.rounded_rectangle((gx, gy, gx + slot_sz, gy + slot_sz), radius=7, fill=CRT_BG, outline=AMBER_GHOST, width=1)

    # Lower-right Base Storage transfer strip showing 32px icons in context
    base_box = (568, 582, w - 36, 704)
    draw.rounded_rectangle(base_box, radius=10, fill=CRT_CARD, outline=AMBER_DIM, width=1)
    draw.text((588, 594), "BASE STORAGE LINK — 32 PX COMPACT STRIP", font=f_sub, fill=AMBER_MID)
    for b_idx in range(10):
        spec = ITEM_SPECS[b_idx]
        bx = 588 + b_idx * 51
        by = 624
        draw.rounded_rectangle((bx, by, bx + 44, by + 44), radius=5, fill=CRT_WELL, outline=AMBER_DIM, width=1)
        ic32 = render_svg_file_to_rgba(svg_dir / spec.filename, 32, (*AMBER_BRIGHT, 255))
        img.alpha_composite(ic32, (bx + 6, by + 6))
        draw.text((bx + 4, by + 48), spec.icon_id[:5].upper(), font=f_sm, fill=AMBER_MID)

    # Bottom status footer
    draw.text(
        (40, 720),
        "FOCUS: CUTTER [kit_cutter.svg] + OPTICS [item_optics.svg]   |   ALL 30 PRODUCTION SVGS RENDERED DIRECTLY",
        font=f_sm,
        fill=CYAN_FOCUS,
    )

    out_path.parent.mkdir(parents=True, exist_ok=True)
    _apply_crt_finish(img).save(out_path, "PNG")


def render_review_sheets(svg_dir: Path = DEFAULT_ICON_DIR, review_dir: Path = DEFAULT_REVIEW_DIR) -> List[Path]:
    """Generates sheet_items.png, sheet_kits.png, and in_layout.png under `review_dir`."""
    review_dir.mkdir(parents=True, exist_ok=True)
    p_items = review_dir / "sheet_items.png"
    p_kits = review_dir / "sheet_kits.png"
    p_layout = review_dir / "in_layout.png"
    render_sheet_items(svg_dir, p_items)
    render_sheet_kits(svg_dir, p_kits)
    render_in_layout(svg_dir, p_layout)
    return [p_items, p_kits, p_layout]


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Build MoonProject SVG icon set and optional CRT review sheets.")
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_ICON_DIR, help="Output directory for *.svg files.")
    parser.add_argument("--review-dir", type=Path, default=DEFAULT_REVIEW_DIR, help="Output directory for review PNGs.")
    parser.add_argument("--skip-sheets", action="store_true", help="Only write the deterministic SVG files.")
    args = parser.parse_args(argv)

    written = build_all_icons(args.out_dir)
    print(f"Wrote {len(written)} SVG icons to {args.out_dir}")
    if not args.skip_sheets:
        sheets = render_review_sheets(args.out_dir, args.review_dir)
        for sheet in sheets:
            print(f"Rendered review sheet: {sheet}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
