#!/usr/bin/env python3
"""
Prepares downloaded CC0 texture sets (ambientCG, Poly Haven) as R1 terrain layers for the look-max spike, outside git:
the albedo is desaturated to lunar grey and scaled so its mean brightness matches the shipped regolith
(T_R1_Regolith_Albedo), so exposure comparisons stay fair; the OpenGL normal map is copied as is (Unity's convention);
the mask follows R1Terrain's packing (R metal = 0, G = ambient occlusion, B = detail = 1,
A = smoothness = 1 - roughness).
LookMaxTerrainDetail loads the results at runtime; nothing is imported into the project.

Usage:
  python tools/art/look_max_cc0.py RAW_DIR OUT_DIR [--size 4096]
RAW_DIR holds one folder per set as downloaded (ambientCG: <Id>_4K-JPG_Color.jpg, _NormalGL, _AmbientOcclusion,
_Roughness; Poly Haven: <id>_diff_4k.png, _nor_gl_4k, _ao_4k, _rough_4k).
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None
SHIPPED_REGOLITH = Path("Assets/_Project/Art/R1/Textures/T_R1_Regolith_Albedo.png")
SETS = {
    "Ground054": ("{id}_4K-JPG_Color.jpg", "{id}_4K-JPG_NormalGL.jpg", "{id}_4K-JPG_AmbientOcclusion.jpg",
                  "{id}_4K-JPG_Roughness.jpg"),
    "Rock030": ("{id}_4K-JPG_Color.jpg", "{id}_4K-JPG_NormalGL.jpg", "{id}_4K-JPG_AmbientOcclusion.jpg",
                "{id}_4K-JPG_Roughness.jpg"),
    "Gravel023": ("{id}_4K-JPG_Color.jpg", "{id}_4K-JPG_NormalGL.jpg", "{id}_4K-JPG_AmbientOcclusion.jpg",
                  "{id}_4K-JPG_Roughness.jpg"),
    "dry_ground_rocks": ("{id}_diff_4k.png", "{id}_nor_gl_4k.png", "{id}_ao_4k.png", "{id}_rough_4k.png"),
}
REC709 = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)


def load(path: Path, size: int, mode: str) -> np.ndarray:
    image = Image.open(path).convert(mode)
    if image.size != (size, size):
        image = image.resize((size, size), Image.LANCZOS)
    return np.asarray(image, dtype=np.float32) / 255.0


def save(array: np.ndarray, path: Path) -> None:
    Image.fromarray(np.clip(array * 255.0 + 0.5, 0, 255).astype(np.uint8)).save(path)


def mean_luminance(rgb: np.ndarray) -> float:
    return float((rgb @ REC709).mean())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("raw", type=Path)
    parser.add_argument("out", type=Path)
    parser.add_argument("--size", type=int, default=4096)
    args = parser.parse_args()
    target = mean_luminance(load(SHIPPED_REGOLITH, 512, "RGB"))
    for set_id, names in SETS.items():
        folder = args.raw / set_id
        if not folder.is_dir():
            print(f"skip {set_id}: no {folder}", file=sys.stderr)
            continue

        color, normal, ao, rough = (folder / name.format(id=set_id) for name in names)
        albedo = load(color, args.size, "RGB")
        grey = albedo @ REC709
        grey *= target / max(float(grey.mean()), 1e-4)
        out = args.out / set_id
        out.mkdir(parents=True, exist_ok=True)
        save(np.repeat(grey[..., None], 3, axis=2), out / "albedo.png")
        save(load(normal, args.size, "RGB"), out / "normal.png")
        occlusion = load(ao, args.size, "L")
        smooth = 1.0 - load(rough, args.size, "L")
        mask = np.stack([np.zeros_like(occlusion), occlusion, np.ones_like(occlusion), smooth], axis=2)
        save(mask, out / "mask.png")
        print(f"{set_id}: mean grey {float(grey.mean()):.3f} (shipped {target:.3f}) -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
