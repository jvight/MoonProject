#!/usr/bin/env python3
"""
The look report of docs/features/M3-18 ("Acceptance") over the fixed look capture set.

For each frame (PNG) it measures:
  spread    the 5th-95th percentile of luminance (sRGB, Rec. 709 weights, 0..1); acceptance >= 0.40;
  flatness  the share of the lower 45% of the frame within +-0.04 of that region's most common luminance;
            acceptance <= 0.30;
  top 1%    what the brightest 1% of pixels are: their share that is warm, cream, cool (Earth, or a moonlit rim) or neutral
            (grey or lavender, the "prop panel" case), their mean colour and where they sit. A mask
            (report/<frame>-top1.png) shows them in magenta over the dimmed frame, for the eye to judge.

Usage:
  python tools/look_report.py capture                  # shoot the set in this worktree (unity_batch, GPU), report it
  python tools/look_report.py report [FOLDER]          # report a capture folder (default: the newest one)
  python tools/look_report.py diff BEFORE AFTER        # before/after per frame
Capture folders are Logs/look-captures/<stamp>/ (written by MoonProject.Tests.PlayMode.Look.LookCaptureSession).
report writes FOLDER/report/look.md; diff writes AFTER/report/diff.md.
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image

SPREAD_LOW, SPREAD_HIGH = 5, 95
SPREAD_TARGET = 0.40
GROUND_SHARE = 0.45
FLATNESS_BAND = 0.04
FLATNESS_TARGET = 0.30
TOP_PERCENT = 1.0
HISTOGRAM_BINS = 256
# Hues (degrees) from red through amber to yellow read as warm light; below WARM_SATURATION the same hues are cream.
RED_WRAP = 345.0
WARM_HUE_MAX = 65.0
WARM_SATURATION = 0.25
CREAM_SATURATION = 0.06
TINT_SATURATION = 0.12
MASK_DIM = 0.25
MASK_COLOUR = (255, 0, 255)
CAPTURE_ROOT = Path("Logs") / "look-captures"
CAPTURE_CATEGORY = "LookCapture"
CATEGORIES = ("warm", "cream", "cool", "neutral")


@dataclass(frozen=True)
class FrameMetrics:
    name: str
    p5: float
    p95: float
    flatness: float
    top: dict
    top_rgb: tuple
    top_centre: tuple

    @property
    def spread(self):
        return self.p95 - self.p5

    @property
    def passes(self):
        return self.spread >= SPREAD_TARGET and self.flatness <= FLATNESS_TARGET


def load_rgb(path):
    """The image as floats in 0..1, shape (height, width, 3)."""
    with Image.open(path) as image:
        return np.asarray(image.convert("RGB"), dtype=np.float64) / 255.0


def luminance(rgb):
    return rgb[..., 0] * 0.2126 + rgb[..., 1] * 0.7152 + rgb[..., 2] * 0.0722


def flatness(lum):
    """Share of the lower GROUND_SHARE of the frame within FLATNESS_BAND of that region's most common value."""
    ground = lum[int(round(lum.shape[0] * (1.0 - GROUND_SHARE))):, :].ravel()
    counts, edges = np.histogram(ground, bins=HISTOGRAM_BINS, range=(0.0, 1.0))
    peak = int(np.argmax(counts))
    mode = (edges[peak] + edges[peak + 1]) / 2.0
    return float(np.mean(np.abs(ground - mode) <= FLATNESS_BAND))


def hue_saturation(rgb):
    """HSV hue in degrees and saturation, per pixel."""
    high = rgb.max(axis=-1)
    low = rgb.min(axis=-1)
    chroma = high - low
    saturation = np.where(high > 0, chroma / np.maximum(high, 1e-9), 0.0)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    safe = np.maximum(chroma, 1e-9)
    hue = np.select([high == r, high == g], [((g - b) / safe) % 6.0, (b - r) / safe + 2.0], (r - g) / safe + 4.0)
    return hue * 60.0, saturation


def classify(rgb):
    """Each pixel's category: 0 warm, 1 cream, 2 cool, 3 neutral (see CATEGORIES)."""
    hue, saturation = hue_saturation(rgb)
    warm_hue = (hue >= RED_WRAP) | (hue <= WARM_HUE_MAX)
    warm = warm_hue & (saturation >= WARM_SATURATION)
    cream = ~warm & warm_hue & (saturation >= CREAM_SATURATION)
    cool = ~warm & ~cream & (rgb[..., 2] > rgb[..., 0]) & (saturation >= TINT_SATURATION)
    return np.select([warm, cream, cool], [0, 1, 2], 3)


def brightest(lum):
    """The mask of the brightest TOP_PERCENT of pixels."""
    return lum >= np.percentile(lum, 100.0 - TOP_PERCENT)


def measure(path):
    rgb = load_rgb(path)
    lum = luminance(rgb)
    mask = brightest(lum)
    top = rgb[mask]
    kinds = classify(top)
    shares = {name: float(np.mean(kinds == i)) for i, name in enumerate(CATEGORIES)}
    rows, cols = np.nonzero(mask)
    centre = (float(cols.mean() / lum.shape[1]), float(rows.mean() / lum.shape[0]))
    p5, p95 = np.percentile(lum, [SPREAD_LOW, SPREAD_HIGH])
    return FrameMetrics(Path(path).stem, float(p5), float(p95), flatness(lum), shares,
                        tuple(int(round(c * 255)) for c in top.mean(axis=0)), centre), rgb, mask


def write_mask(rgb, mask, path):
    image = (rgb * MASK_DIM * 255.0).astype(np.uint8)
    image[mask] = MASK_COLOUR
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(image).save(path)


def frames_in(folder):
    return sorted(p for p in Path(folder).glob("*.png"))


def describe_top(m):
    shares = ", ".join(f"{name} {m.top[name]:.0%}" for name in CATEGORIES)
    x, y = m.top_centre
    return f"{shares}; mean rgb{m.top_rgb}; centred at {x:.0%} across, {y:.0%} down"


def report(folder):
    folder = Path(folder)
    frames = frames_in(folder)
    if not frames:
        raise SystemExit(f"no PNG frames in {folder}")
    lines = [f"# Look report: {folder.name}", "",
             f"Acceptance (M3-18): spread (p{SPREAD_HIGH} - p{SPREAD_LOW}) >= {SPREAD_TARGET:.2f}, ground flatness "
             f"<= {FLATNESS_TARGET:.2f}; the brightest {TOP_PERCENT:g}% are warm lights, Earth or 07's cream, never "
             "a prop panel (judge on report/<frame>-top1.png; 'neutral' is the panel-like share).", "",
             "| Frame | p5 | p95 | Spread | Flatness | Pass | Brightest 1% |", "|---|---:|---:|---:|---:|---|---|"]
    metrics = []
    for path in frames:
        m, rgb, mask = measure(path)
        metrics.append(m)
        write_mask(rgb, mask, folder / "report" / f"{m.name}-top1.png")
        lines.append(f"| {m.name} | {m.p5:.2f} | {m.p95:.2f} | {m.spread:.2f} | {m.flatness:.2f} | "
                     f"{'yes' if m.passes else 'no'} | {describe_top(m)} |")
    text = "\n".join(lines) + "\n"
    out = folder / "report" / "look.md"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8")
    print(text, end="")
    print(f"[look_report] {out}")
    return metrics


def diff(before, after):
    before, after = Path(before), Path(after)
    old = {p.stem: measure(p)[0] for p in frames_in(before)}
    new = {p.stem: measure(p)[0] for p in frames_in(after)}
    names = sorted(set(old) | set(new))
    lines = [f"# Look diff: {before.name} -> {after.name}", "",
             "| Frame | Spread before | after | delta | Flatness before | after | delta | Neutral top 1% before "
             "| after |", "|---|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for name in names:
        a, b = old.get(name), new.get(name)
        if a is None or b is None:
            lines.append(f"| {name} | {'missing' if a is None else 'present'} | "
                         f"{'missing' if b is None else 'present'} | | | | | | |")
            continue
        lines.append(f"| {name} | {a.spread:.2f} | {b.spread:.2f} | {b.spread - a.spread:+.2f} | {a.flatness:.2f} | "
                     f"{b.flatness:.2f} | {b.flatness - a.flatness:+.2f} | {a.top['neutral']:.0%} | "
                     f"{b.top['neutral']:.0%} |")
    text = "\n".join(lines) + "\n"
    out = after / "report" / "diff.md"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8")
    print(text, end="")
    print(f"[look_report] {out}")


def newest(root):
    folders = sorted(p for p in Path(root).iterdir() if p.is_dir()) if Path(root).exists() else []
    if not folders:
        raise SystemExit(f"no capture folders in {root}; run `python tools/look_report.py capture` first")
    return folders[-1]


def worktree_root():
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"], capture_output=True,
                         text=True)
    if res.returncode != 0:
        raise SystemExit(f"not inside a git worktree: {res.stderr.strip()}")
    return Path(res.stdout.strip())


def capture(root):
    known = set(p.name for p in (root / CAPTURE_ROOT).glob("*")) if (root / CAPTURE_ROOT).exists() else set()
    res = subprocess.run([sys.executable, str(root / "tools" / "unity_batch.py"), "tests", "--project", str(root),
                          "--platform", "playmode", "--category", CAPTURE_CATEGORY], cwd=root)
    fresh = [p for p in (root / CAPTURE_ROOT).glob("*") if p.is_dir() and p.name not in known] \
        if (root / CAPTURE_ROOT).exists() else []
    if res.returncode != 0 or not fresh:
        raise SystemExit("the look capture failed (see the unity_batch log above)")
    return sorted(fresh)[-1]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("capture", help="shoot the look set in this worktree, then report it")
    rep = sub.add_parser("report", help="report a capture folder")
    rep.add_argument("folder", nargs="?", help="capture folder (default: the newest under Logs/look-captures)")
    dif = sub.add_parser("diff", help="compare two capture folders")
    dif.add_argument("before")
    dif.add_argument("after")
    args = ap.parse_args()
    if args.cmd == "capture":
        root = worktree_root()
        report(capture(root))
    elif args.cmd == "report":
        report(args.folder or newest(worktree_root() / CAPTURE_ROOT))
    else:
        diff(args.before, args.after)


if __name__ == "__main__":
    main()
