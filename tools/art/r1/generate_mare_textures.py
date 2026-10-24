#!/usr/bin/env python3
"""Deterministic texture generator for The Long Mare (0.6) R1 environment kit.

Generates into Assets/_Project/Art/R1/Textures/:
  1. T_R1_MareBasalt_{Albedo,Normal,Mask}.png
     Deep black-blue flood-basalt regolith (#101420 trough .. #161C2C mid .. #222B40 crest),
     windless micro-ripple normal, tiling-break detail, smoothness ~0.14-0.22, metallic ~0.04.
     Strictly zero repeated round holes or dot clusters (owner trypophobia rule).
  2. T_R1_MareSwirl_{Albedo,Normal,Mask}.png
     High-albedo magnetically shielded lunar swirl ribbon (#C8CEDC edge feather .. #D0D6E4 mid .. #D8DFEC core),
     fine powdery silicate normal, matte smoothness ~0.08, metallic 0.00.
  3. T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png
     Directional pale ejecta-ray wayfinding dust streaks (#4A5264 .. #686E82) feathered across dark basalt,
     directional windless streak normal, smoothness ~0.10, metallic 0.02.
  4. T_R1_Ilmenite_{Albedo,Normal,Mask}.png
     Dark iron-titanium oxide (FeTiO3) outcrop surface (#181A22 base with warm bronze-black #5C4938/#6E5849
     and cool gunmetal #5A6475 stepped tabular/hexagonal crystal cleavage facets),
     metallic ~0.82, smoothness ~0.48, non-emissive.
"""

from __future__ import annotations

from pathlib import Path
import numpy as np

from generate_r1_textures import (
    _fbm,
    _height_to_normal,
    _save_rgba_or_rgb,
    _tileable_noise,
)

SIZE = 256


def _hex_to_rgb01(hex_str: str) -> np.ndarray:
    h = hex_str.lstrip("#")
    return np.array([int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


def _generate_mare_basalt(out_dir: Path) -> None:
    """Build T_R1_MareBasalt_{Albedo,Normal,Mask}.png (256x256 seamless)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    # Broad flood-basalt swell + directional windless compression ripples (no circular holes/pits)
    swell = _fbm(SIZE, base_freq=4, seed=601)
    fine_grain = _fbm(SIZE, base_freq=12, seed=602)
    micro_grain = _tileable_noise(SIZE, freq=32, seed=603)

    # Subtle sinuous basaltic flow-front undulation (anisotropic, non-repetitive)
    warp_x = x + 0.06 * np.sin(2.0 * np.pi * (y * 2.0 + swell * 0.35))
    warp_y = y + 0.05 * np.cos(2.0 * np.pi * (x * 2.0 - swell * 0.30))
    ripple = 0.5 + 0.5 * np.sin(2.0 * np.pi * (warp_x * 5.0 + warp_y * 2.0 + fine_grain * 0.42))
    ripple_secondary = 0.5 + 0.5 * np.cos(2.0 * np.pi * (warp_x * 3.0 - warp_y * 4.0 + swell * 0.38))
    flow_band = 0.62 * ripple + 0.38 * ripple_secondary

    trough_rgb = _hex_to_rgb01("#1E2536")
    mid_rgb = _hex_to_rgb01("#283146")
    crest_rgb = _hex_to_rgb01("#38445E")

    blend_t = np.clip(0.48 * swell + 0.34 * flow_band + 0.18 * fine_grain, 0.0, 1.0)
    low_mask = np.clip(blend_t * 2.0, 0.0, 1.0)[..., None]
    high_mask = np.clip((blend_t - 0.5) * 2.0, 0.0, 1.0)[..., None]

    rgb = trough_rgb * (1.0 - low_mask) + mid_rgb * low_mask
    rgb = rgb * (1.0 - high_mask) + crest_rgb * high_mask
    rgb += (micro_grain[..., None] - 0.5) * 0.018

    height = 0.50 * swell + 0.30 * flow_band + 0.14 * fine_grain + 0.06 * micro_grain
    normal = _height_to_normal(height, normal_strength=3.2)

    # MaskMap: R = Metallic (~0.04), G = MicroAO, B = MicroDetail (tiling-break), A = Smoothness (~0.14..0.22)
    metal = np.clip(0.035 + 0.025 * (flow_band - 0.5) + 0.015 * (micro_grain - 0.5), 0.01, 0.08)
    ao = np.clip(0.76 + 0.24 * (0.55 * swell + 0.45 * flow_band), 0.62, 1.0)
    detail = np.clip(0.35 + 0.40 * fine_grain + 0.25 * micro_grain, 0.0, 1.0)
    smooth = np.clip(0.13 + 0.07 * flow_band + 0.04 * (fine_grain - 0.5), 0.08, 0.24)

    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Albedo.png", np.dstack([rgb, np.ones((SIZE, SIZE), dtype=np.float32)]))
    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_mare_swirl(out_dir: Path) -> None:
    """Build T_R1_MareSwirl_{Albedo,Normal,Mask}.png (256x256 pale lunar swirl overlay/decal)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    n_macro = _fbm(SIZE, base_freq=4, seed=611)
    n_wisp = _fbm(SIZE, base_freq=8, seed=612)
    n_dust = _tileable_noise(SIZE, freq=24, seed=613)

    # Sinuous magnetically-shielded swirl filaments (Reiner Gamma archetype)
    phase1 = 2.0 * np.pi * (x * 2.0 + 0.22 * np.sin(2.0 * np.pi * y * 1.0) + (n_macro - 0.5) * 0.35)
    phase2 = 2.0 * np.pi * (y * 2.0 - 0.18 * np.cos(2.0 * np.pi * x * 1.0) + (n_wisp - 0.5) * 0.28)
    ribbon = 0.55 * (0.5 + 0.5 * np.sin(phase1)) + 0.45 * (0.5 + 0.5 * np.cos(phase2))
    swirl_t = np.clip(0.52 * ribbon + 0.32 * n_macro + 0.16 * n_wisp, 0.0, 1.0)

    edge_rgb = _hex_to_rgb01("#C8CEDC")
    mid_rgb = _hex_to_rgb01("#D0D6E4")
    core_rgb = _hex_to_rgb01("#D8DFEC")

    low_m = np.clip(swirl_t * 2.0, 0.0, 1.0)[..., None]
    high_m = np.clip((swirl_t - 0.5) * 2.0, 0.0, 1.0)[..., None]
    rgb = edge_rgb * (1.0 - low_m) + mid_rgb * low_m
    rgb = rgb * (1.0 - high_m) + core_rgb * high_m
    rgb += (n_dust[..., None] - 0.5) * 0.016

    height = 0.45 * ribbon + 0.35 * n_wisp + 0.20 * n_dust
    normal = _height_to_normal(height, normal_strength=1.8)

    # Powdery unweathered silicate dust: metallic 0.00, smoothness ~0.08
    metal = np.zeros((SIZE, SIZE), dtype=np.float32)
    ao = np.clip(0.86 + 0.14 * swirl_t, 0.78, 1.0)
    detail = np.clip(0.45 + 0.35 * ribbon + 0.20 * n_dust, 0.0, 1.0)
    smooth = np.clip(0.06 + 0.04 * swirl_t + 0.02 * (n_dust - 0.5), 0.04, 0.12)

    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Albedo.png", np.dstack([rgb, np.ones((SIZE, SIZE), dtype=np.float32)]))
    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_mare_ejecta_ray(out_dir: Path) -> None:
    """Build T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png (256x256 directional wayfinding ejecta streaks)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    # Anisotropic streaks running along V (y) with feathered cross-axis modulation along U (x)
    n_long = _fbm(SIZE, base_freq=4, seed=621)
    n_cross = _fbm(SIZE, base_freq=8, seed=622)
    n_fine = _tileable_noise(SIZE, freq=28, seed=623)

    streak_wave = (
        0.50 * (0.5 + 0.5 * np.sin(2.0 * np.pi * (x * 4.0 + (n_long - 0.5) * 0.45)))
        + 0.32 * (0.5 + 0.5 * np.cos(2.0 * np.pi * (x * 7.0 - (n_cross - 0.5) * 0.35)))
        + 0.18 * n_long
    )
    lateral_env = 0.65 + 0.35 * np.sin(np.pi * x)
    ray_t = np.clip(streak_wave * lateral_env, 0.0, 1.0)

    edge_rgb = _hex_to_rgb01("#4A5264")
    mid_rgb = _hex_to_rgb01("#5A6276")
    core_rgb = _hex_to_rgb01("#686E82")

    low_m = np.clip(ray_t * 2.0, 0.0, 1.0)[..., None]
    high_m = np.clip((ray_t - 0.5) * 2.0, 0.0, 1.0)[..., None]
    rgb = edge_rgb * (1.0 - low_m) + mid_rgb * low_m
    rgb = rgb * (1.0 - high_m) + core_rgb * high_m
    rgb += (n_fine[..., None] - 0.5) * 0.018

    height = 0.55 * streak_wave + 0.25 * n_long + 0.20 * n_fine
    normal = _height_to_normal(height, normal_strength=2.1)

    metal = np.full((SIZE, SIZE), 0.02, dtype=np.float32)
    ao = np.clip(0.80 + 0.20 * ray_t, 0.72, 1.0)
    detail = np.clip(0.35 + 0.45 * streak_wave + 0.20 * n_fine, 0.0, 1.0)
    smooth = np.clip(0.08 + 0.05 * ray_t, 0.06, 0.15)

    _save_rgba_or_rgb(
        out_dir / "T_R1_MareEjectaRay_Albedo.png", np.dstack([rgb, np.ones((SIZE, SIZE), dtype=np.float32)])
    )
    _save_rgba_or_rgb(out_dir / "T_R1_MareEjectaRay_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareEjectaRay_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_ilmenite(out_dir: Path) -> None:
    """Build T_R1_Ilmenite_{Albedo,Normal,Mask}.png (256x256 FeTiO3 stepped crystal cleavage facets)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    n_macro = _fbm(SIZE, base_freq=4, seed=631)
    n_micro = _tileable_noise(SIZE, freq=24, seed=632)

    # Three-axis hexagonal/tabular crystal cleavage planes (0, 60, 120 deg)
    u0 = x * 6.0 + (n_macro - 0.5) * 0.35
    u1 = (0.5 * x + 0.8660254 * y) * 6.0 + (n_macro - 0.5) * 0.30
    u2 = (-0.5 * x + 0.8660254 * y) * 6.0 - (n_macro - 0.5) * 0.30

    # Stepped terrace quantization so flat cleavage plates catch low polar sun with a calm metallic glint
    step0 = np.floor((u0 % 1.0) * 4.0) / 3.0
    step1 = np.floor((u1 % 1.0) * 4.0) / 3.0
    step2 = np.floor((u2 % 1.0) * 3.0) / 2.0
    terrace = np.clip(0.42 * step0 + 0.36 * step1 + 0.22 * step2, 0.0, 1.0)

    # Crisp cleavage seam lines where crystal plates step down
    seam0 = np.exp(-(((u0 * 4.0) % 1.0 - 0.5) ** 2) / 0.018)
    seam1 = np.exp(-(((u1 * 4.0) % 1.0 - 0.5) ** 2) / 0.022)
    seam_mask = np.clip(np.maximum(seam0, seam1), 0.0, 1.0)

    base_black_rgb = _hex_to_rgb01("#242834")
    gunmetal_rgb = _hex_to_rgb01("#525C70")
    bronze_glint_rgb = _hex_to_rgb01("#7A624C")

    # Base body is dark iron-titanium (#181A22) with gunmetal & warm bronze-black tabular facet tint
    facet_t = np.clip(0.65 * terrace + 0.35 * n_macro, 0.0, 1.0)[..., None]
    bronze_t = np.clip((terrace - 0.52) * 1.85, 0.0, 1.0)[..., None]
    rgb = base_black_rgb * (1.0 - facet_t * 0.55) + gunmetal_rgb * (facet_t * 0.55)
    rgb = rgb * (1.0 - bronze_t * 0.62) + bronze_glint_rgb * (bronze_t * 0.62)
    rgb *= (1.0 - 0.22 * seam_mask[..., None])
    rgb += (n_micro[..., None] - 0.5) * 0.015

    height = np.clip(0.58 * terrace + 0.26 * n_macro - 0.16 * seam_mask + 0.08 * n_micro, 0.0, 1.0)
    normal = _height_to_normal(height, normal_strength=4.0)

    # MaskMap: R = Metallic (~0.82 on crystal plates, lower in seams), G = MicroAO, B = MicroDetail, A = Smoothness (~0.48)
    metal = np.clip(0.78 + 0.14 * terrace - 0.18 * seam_mask, 0.52, 0.92)
    ao = np.clip(0.88 - 0.26 * seam_mask + 0.08 * terrace, 0.55, 1.0)
    detail = np.clip(0.40 + 0.45 * terrace + 0.15 * n_micro, 0.0, 1.0)
    smooth = np.clip(0.42 + 0.16 * terrace - 0.14 * seam_mask + 0.04 * (n_micro - 0.5), 0.26, 0.62)

    _save_rgba_or_rgb(out_dir / "T_R1_Ilmenite_Albedo.png", np.dstack([rgb, np.ones((SIZE, SIZE), dtype=np.float32)]))
    _save_rgba_or_rgb(out_dir / "T_R1_Ilmenite_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_Ilmenite_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def main() -> int:
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures"
    out_dir.mkdir(parents=True, exist_ok=True)

    _generate_mare_basalt(out_dir)
    _generate_mare_swirl(out_dir)
    _generate_mare_ejecta_ray(out_dir)
    _generate_ilmenite(out_dir)

    print(f"Generated 12 Mare R1 textures in {out_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
