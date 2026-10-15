#!/usr/bin/env python3
"""
concept_compare: how closely an R1 model's silhouette matches its approved concept turnaround, view by view, in
numbers agy can iterate against.

    python tools/art/qa/concept_compare.py --fbx Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx \
        --config docs/art/contracts/rover.concept.json [--out Logs/qa/rover]

1. Headless Blender renders orthographic front, side, back and top silhouettes of the FBX at metres_per_pixel
   (blender_ortho_masks.py).
2. Each concept view is cut from the turnaround sheet by its crop box. The sheet's background colour, flood-filled
   from the crop's border, is keyed out; holes are filled and specks dropped.
3. The sheet's scale comes from the reference dimension: the reference view's silhouette height (or width) is that
   many metres. Every view uses that one scale, since a turnaround is drawn at one scale. Each concept mask is
   resampled to metres_per_pixel.
4. The two masks are aligned: front, side and back at the bottom-centre (the ground contact), top at the centroid.
   Each view reports IoU, both silhouettes' width and height in metres and their ratio.
Writes <out>/concept_compare.png (one heatmap panel per view: grey = both, red = model only, cyan = concept only)
and <out>/concept_compare.json. Exits 0 when every view's IoU reaches min_iou, 1 otherwise.

Config keys (docs/art/model-qa.md has an example):
  model              id used in reports
  concept            path of the turnaround image
  views              {"front": [x0, y0, x1, y1], ...} crop box of each view on the sheet, in pixels; any of front,
                     side, back, top
  reference          {"view": "front", "axis": "height" | "width", "metres": 1.59}
  front_axis         the direction the model faces in Blender space ("+Y", "-Y", "+X", "-X"; default "+Y")
  metres_per_pixel   render and comparison scale (default 0.004)
  background         [r, g, b] of the sheet (default: the median colour of the sheet's border)
  tolerance          colour distance still counted as background (default 12; painted shadows are keyed by hue)
  shadow_cos         a pixel of the background's hue (colour-vector cosine at least this), darker, is a painted
                     ground shadow and counts as background (default 0.999; null when the model's own dark paint
                     shares the background's hue)
  min_iou            pass threshold per view (default 0.8)
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import json
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

HERE = Path(__file__).resolve().parent
BLENDER = Path("D:/Blender/blender.exe")
RENDER = HERE / "blender_ortho_masks.py"
RENDER_TIMEOUT = 600
VIEW_ORDER = ("front", "side", "back", "top")
DEFAULTS = {"front_axis": "+Y", "metres_per_pixel": 0.004, "tolerance": 12, "shadow_cos": 0.999, "min_iou": 0.8}
SHADOW_MIN_BRIGHTNESS = 0.3
SPECK_SHARE = 0.02
ALPHA_SOLID = 128
PANEL_MARGIN = 24
LABEL_HEIGHT = 64
COLOURS = {"ground": (28, 30, 36), "both": (196, 198, 206), "model": (229, 72, 77), "concept": (61, 185, 207),
           "text": (233, 231, 225)}


def keyed_mask(rgb, background, tolerance, shadow_cos=DEFAULTS["shadow_cos"]):
    """Foreground of an RGB crop: everything not reachable from the crop's border through background pixels; holes
    filled and specks (components under SPECK_SHARE of the largest) dropped. A background pixel is within
    <tolerance> of the background colour, or (unless <shadow_cos> is None) a painted ground shadow: the background's
    own hue (cosine of the colour vectors at least <shadow_cos>), darker."""
    pixels = rgb.astype(np.float32)
    bg = np.array(background, np.float32)
    distance = np.sqrt(((pixels - bg) ** 2).sum(axis=2))
    near = distance <= tolerance
    if shadow_cos is not None:
        length = np.linalg.norm(pixels, axis=2)
        cosine = (pixels @ bg) / np.maximum(length * np.linalg.norm(bg), 1e-6)
        darker = length / np.linalg.norm(bg)
        near |= (cosine >= shadow_cos) & (darker >= SHADOW_MIN_BRIGHTNESS) & (darker <= 1.0)
    labels, _ = ndimage.label(near)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    outside = np.isin(labels, border[border > 0])
    return clean(~outside)


def clean(mask):
    mask = ndimage.binary_fill_holes(mask)
    labels, count = ndimage.label(mask)
    if count == 0:
        return mask
    sizes = ndimage.sum(mask, labels, range(1, count + 1))
    keep = [i + 1 for i, size in enumerate(sizes) if size >= SPECK_SHARE * sizes.max()]
    return np.isin(labels, keep)


def border_colour(rgb):
    edge = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    return [int(v) for v in np.median(edge, axis=0)]


def bbox(mask):
    rows, cols = np.nonzero(mask)
    if rows.size == 0:
        raise ValueError("empty silhouette")
    return rows.min(), rows.max() + 1, cols.min(), cols.max() + 1


def crop_to_content(mask):
    r0, r1, c0, c1 = bbox(mask)
    return mask[r0:r1, c0:c1]


def resample(mask, factor):
    height, width = mask.shape
    size = (max(1, round(width * factor)), max(1, round(height * factor)))
    image = Image.fromarray(mask.astype(np.uint8) * 255).resize(size, Image.BILINEAR)
    return np.asarray(image) >= 128


def align(model, concept, view):
    """Both masks on one canvas: elevations share bottom edge and horizontal centroid, the top view its centroid."""
    def anchor(mask):
        rows, cols = np.nonzero(mask)
        return (rows.mean() if view == "top" else mask.shape[0]), cols.mean()

    (mr, mc), (cr, cc) = anchor(model), anchor(concept)
    offset_r, offset_c = round(mr - cr), round(mc - cc)
    top = min(0, offset_r)
    left = min(0, offset_c)
    height = max(model.shape[0], concept.shape[0] + offset_r) - top
    width = max(model.shape[1], concept.shape[1] + offset_c) - left
    a = np.zeros((height, width), bool)
    b = np.zeros((height, width), bool)
    a[-top:-top + model.shape[0], -left:-left + model.shape[1]] = model
    b[offset_r - top:offset_r - top + concept.shape[0], offset_c - left:offset_c - left + concept.shape[1]] = concept
    return a, b


def iou(a, b):
    union = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum() / union) if union else 0.0


def size_m(mask, mpp):
    r0, r1, c0, c1 = bbox(mask)
    return [round(float(c1 - c0) * mpp, 4), round(float(r1 - r0) * mpp, 4)]


def concept_scale(masks, reference):
    """Metres per pixel of the concept sheet, from the reference view's silhouette extent."""
    r0, r1, c0, c1 = bbox(masks[reference["view"]])
    pixels = (r1 - r0) if reference.get("axis", "height") == "height" else (c1 - c0)
    return reference["metres"] / float(pixels)


def compare(model_masks, concept_masks, reference, mpp):
    """{view: {iou, model_size_m, concept_size_m, size_ratio, a, b}} for the views both sides have."""
    sheet_mpp = concept_scale(concept_masks, reference)
    results = {}
    for view in VIEW_ORDER:
        if view not in concept_masks or view not in model_masks:
            continue
        model = crop_to_content(model_masks[view])
        concept = resample(crop_to_content(concept_masks[view]), sheet_mpp / mpp)
        a, b = align(model, concept, view)
        model_size, concept_size = size_m(model, mpp), size_m(concept, mpp)
        results[view] = {"iou": round(iou(a, b), 4), "model_size_m": model_size, "concept_size_m": concept_size,
                         "size_ratio": [round(m / c, 3) if c else None for m, c in zip(model_size, concept_size)],
                         "a": a, "b": b}
    return results


def panel(a, b, title, lines):
    rgb = np.zeros(a.shape + (3,), np.uint8)
    rgb[:] = COLOURS["ground"]
    rgb[a & b] = COLOURS["both"]
    rgb[a & ~b] = COLOURS["model"]
    rgb[~a & b] = COLOURS["concept"]
    image = Image.new("RGB", (a.shape[1] + 2 * PANEL_MARGIN, a.shape[0] + 2 * PANEL_MARGIN + LABEL_HEIGHT),
                      COLOURS["ground"])
    image.paste(Image.fromarray(rgb), (PANEL_MARGIN, PANEL_MARGIN + LABEL_HEIGHT))
    draw = ImageDraw.Draw(image)
    draw.text((PANEL_MARGIN, 8), title, fill=COLOURS["text"], font=ImageFont.load_default(20))
    for i, line in enumerate(lines):
        draw.text((PANEL_MARGIN, 34 + 14 * i), line, fill=COLOURS["text"], font=ImageFont.load_default(12))
    return image


def heatmap(results, path):
    panels = [panel(r["a"], r["b"], f"{view}  IoU {r['iou']:.2f}",
                    [f"model {r['model_size_m'][0]:.2f} x {r['model_size_m'][1]:.2f} m",
                     f"concept {r['concept_size_m'][0]:.2f} x {r['concept_size_m'][1]:.2f} m"])
              for view, r in results.items()]
    legend = Image.new("RGB", (sum(p.width for p in panels), 30), COLOURS["ground"])
    draw = ImageDraw.Draw(legend)
    x = PANEL_MARGIN
    for key, label in (("both", "both"), ("model", "model only"), ("concept", "concept only")):
        draw.rectangle((x, 9, x + 14, 23), fill=COLOURS[key])
        draw.text((x + 20, 8), label, fill=COLOURS["text"], font=ImageFont.load_default(14))
        x += 150
    sheet = Image.new("RGB", (legend.width, max(p.height for p in panels) + legend.height), COLOURS["ground"])
    sheet.paste(legend, (0, 0))
    x = 0
    for p in panels:
        sheet.paste(p, (x, legend.height))
        x += p.width
    sheet.save(path)


def concept_masks(config):
    sheet = np.asarray(Image.open(config["concept"]).convert("RGB"))
    background = config.get("background") or border_colour(sheet)
    masks = {}
    for view, (x0, y0, x1, y1) in config["views"].items():
        masks[view] = keyed_mask(sheet[y0:y1, x0:x1], background, config.get("tolerance", DEFAULTS["tolerance"]),
                                 config.get("shadow_cos", DEFAULTS["shadow_cos"]))
    return masks


def model_masks(fbx, config, views, folder):
    res = subprocess.run([str(BLENDER), "-b", "--factory-startup", "--python", str(RENDER), "--", str(fbx),
                          str(folder), str(config.get("metres_per_pixel", DEFAULTS["metres_per_pixel"])),
                          config.get("front_axis", DEFAULTS["front_axis"]), ",".join(views)],
                         capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=RENDER_TIMEOUT)
    if res.returncode != 0 or "MASKS_OK" not in res.stdout:
        raise SystemExit(f"Blender could not render {fbx}:\n{res.stdout[-2000:]}{res.stderr[-2000:]}")
    masks = {}
    for view in views:
        alpha = np.asarray(Image.open(Path(folder) / f"mask_{view}.png").convert("RGBA"))[:, :, 3]
        masks[view] = clean(alpha >= ALPHA_SOLID)
    return masks


def worktree_root():
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"], capture_output=True,
                         text=True)
    return Path(res.stdout.strip()) if res.returncode == 0 else Path.cwd()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--fbx", required=True, help="the FBX to compare")
    ap.add_argument("--config", required=True, help="the model's concept comparison JSON")
    ap.add_argument("--out", help="output folder (default <worktree>/Logs/qa/<model>)")
    args = ap.parse_args()
    config = json.loads(Path(args.config).read_text(encoding="utf-8"))
    views = [v for v in VIEW_ORDER if v in config["views"]]
    if config["reference"]["view"] not in views:
        raise SystemExit("the reference view needs a crop box in 'views'")
    mpp = config.get("metres_per_pixel", DEFAULTS["metres_per_pixel"])
    out = Path(args.out) if args.out else worktree_root() / "Logs" / "qa" / config.get("model", "model")
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        rendered = model_masks(Path(args.fbx).resolve(), config, views, tmp)
    results = compare(rendered, concept_masks(config), config["reference"], mpp)
    heatmap(results, out / "concept_compare.png")
    threshold = config.get("min_iou", DEFAULTS["min_iou"])
    report = {"model": config.get("model", ""), "fbx": str(Path(args.fbx).resolve()), "concept": config["concept"],
              "metres_per_pixel": mpp, "min_iou": threshold,
              "views": {v: {k: r[k] for k in ("iou", "model_size_m", "concept_size_m", "size_ratio")}
                        for v, r in results.items()}}
    scores = [r["iou"] for r in results.values()]
    report["mean_iou"] = round(sum(scores) / len(scores), 4)
    report["pass"] = all(s >= threshold for s in scores)
    (out / "concept_compare.json").write_text(json.dumps(report, indent=1), encoding="utf-8")
    print(f"concept_compare {report['model']}: {'PASS' if report['pass'] else 'FAIL'} "
          f"(mean IoU {report['mean_iou']:.3f}, threshold {threshold})")
    for view, r in report["views"].items():
        print(f"- {view}: IoU {r['iou']:.3f}; model {r['model_size_m'][0]:.2f} x {r['model_size_m'][1]:.2f} m, "
              f"concept {r['concept_size_m'][0]:.2f} x {r['concept_size_m'][1]:.2f} m (ratio {r['size_ratio']})")
    print(f"heatmap: {out / 'concept_compare.png'}")
    return 0 if report["pass"] else 1


if __name__ == "__main__":
    sys.exit(main())
