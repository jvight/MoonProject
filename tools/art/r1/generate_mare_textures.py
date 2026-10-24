#!/usr/bin/env python3
"""Deterministic texture generator for The Long Mare (0.6) R1 environment kit.

Generates into Assets/_Project/Art/R1/Textures/:
  1. T_R1_MareBasalt_{Albedo,Normal,Mask}.png
     Dark blue-black flood-basalt regolith & rock surface (#161B26 .. #222938 .. #2C3548),
     organic domain-warped weathered FBM grain with subtle fine relief (no periodic wave grids,
     no checker patterns, and zero repeated round holes or dot clusters).
  2. T_R1_MareSwirl_{Albedo,Normal,Mask}.png (512x512 RGBA)
     High-albedo magnetically shielded lunar swirl ribbon (#B4BDD0 edge feather .. #D8E0F0 core)
     with a soft, sinuous, multi-filament Reiner-Gamma alpha mask that feathers smoothly to alpha=0
     at all UV edges (zero hard polygon edges).
  3. T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png (512x512 RGBA)
     Soft, streaky radial ejecta dust rays (#566074 .. #8892A8) with an anisotropic streaky
     feathered alpha mask that dissolves smoothly to alpha=0 at all UV edges.
  4. T_R1_Ilmenite_{Albedo,Normal,Mask}.png
     Dark iron-titanium oxide (FeTiO3) mineral surface (#1E232E base with calm gunmetal #3E4656
     and warm bronze-black #584A3E organic mineral bands, no checker/grid seams),
     metallic ~0.72, smoothness ~0.48, non-emissive.
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
DECAL_SIZE = 512


def _hex_to_rgb01(hex_str: str) -> np.ndarray:
    h = hex_str.lstrip("#")
    return np.array([int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


def _sample_wrapped(grid: np.ndarray, u: np.ndarray, v: np.ndarray) -> np.ndarray:
    """Bilinearly sample a tileable (H, W) array at normalized coordinates (u, v) with wrap-around."""
    h, w = grid.shape
    px = (u % 1.0) * w
    py = (v % 1.0) * h
    x0 = np.floor(px).astype(np.int32) % w
    y0 = np.floor(py).astype(np.int32) % h
    x1 = (x0 + 1) % w
    y1 = (y0 + 1) % h
    fx = (px - np.floor(px)).astype(np.float32)
    fy = (py - np.floor(py)).astype(np.float32)

    v00 = grid[y0, x0]
    v10 = grid[y0, x1]
    v01 = grid[y1, x0]
    v11 = grid[y1, x1]
    return (v00 * (1.0 - fx) + v10 * fx) * (1.0 - fy) + (v01 * (1.0 - fx) + v11 * fx) * fy


def _generate_mare_basalt(out_dir: Path) -> None:
    """Build T_R1_MareBasalt_{Albedo,Normal,Mask}.png (256x256 seamless, subtle organic basalt, no grid)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    warp_u = _fbm(SIZE, base_freq=3, seed=601)
    warp_v = _fbm(SIZE, base_freq=3, seed=602)
    base_cloud = _fbm(SIZE, base_freq=2, seed=603)
    med_weathered = _fbm(SIZE, base_freq=5, seed=604)
    fine_grain = _fbm(SIZE, base_freq=10, seed=605)
    micro_grain = _tileable_noise(SIZE, freq=22, seed=606)

    wx = x + (warp_u - 0.5) * 0.24
    wy = y + (warp_v - 0.5) * 0.24
    broad = _sample_wrapped(base_cloud, wx, wy)
    medium = _sample_wrapped(med_weathered, wx + 0.17, wy - 0.11)
    fine = _sample_wrapped(fine_grain, x + (warp_v - 0.5) * 0.10, y - (warp_u - 0.5) * 0.10)

    # Smooth, subtle weathered basalt variation (zero periodic sine waves, zero checker grids)
    blend_t = np.clip(0.54 * broad + 0.31 * medium + 0.15 * fine, 0.0, 1.0)

    trough_rgb = _hex_to_rgb01("#1D2332")
    mid_rgb = _hex_to_rgb01("#252D3F")
    crest_rgb = _hex_to_rgb01("#2F384E")

    low_mask = np.clip(blend_t * 1.85, 0.0, 1.0)[..., None]
    high_mask = np.clip((blend_t - 0.48) * 1.85, 0.0, 1.0)[..., None]

    rgb = trough_rgb * (1.0 - low_mask) + mid_rgb * low_mask
    rgb = rgb * (1.0 - 0.55 * high_mask) + crest_rgb * (0.55 * high_mask)
    rgb += (micro_grain[..., None] - 0.5) * 0.010

    height = np.clip(0.55 * broad + 0.30 * medium + 0.11 * fine + 0.04 * micro_grain, 0.0, 1.0)
    normal = _height_to_normal(height, normal_strength=1.15)

    # MaskMap: R = Metallic (~0.04), G = MicroAO, B = MicroDetail, A = Smoothness (~0.15..0.22)
    metal = np.clip(0.035 + 0.015 * (broad - 0.5), 0.02, 0.06)
    ao = np.clip(0.84 + 0.16 * (blend_t - 0.35), 0.74, 1.0)
    detail = np.clip(0.42 + 0.32 * fine + 0.16 * micro_grain, 0.15, 0.85)
    smooth = np.clip(0.15 + 0.06 * broad + 0.03 * (fine - 0.5), 0.11, 0.24)

    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Albedo.png", np.dstack([rgb, np.ones((SIZE, SIZE), dtype=np.float32)]))
    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareBasalt_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_mare_swirl(out_dir: Path) -> None:
    """Build T_R1_MareSwirl_{Albedo,Normal,Mask}.png (512x512 soft, sinuous, feathered RGBA swirl decal)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, DECAL_SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, DECAL_SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    warp_u = _fbm(DECAL_SIZE, base_freq=3, seed=611)
    warp_v = _fbm(DECAL_SIZE, base_freq=3, seed=612)
    wisp_field = _fbm(DECAL_SIZE, base_freq=5, seed=613)
    fine_dust = _fbm(DECAL_SIZE, base_freq=10, seed=614)

    wisp = _sample_wrapped(wisp_field, x * 1.5 + (warp_u - 0.5) * 0.28, y + (warp_v - 0.5) * 0.18)

    # Sinuous Reiner-Gamma S-curve centerline along v = y
    v_phase = 2.0 * np.pi * y
    u_center = (
        0.50
        + 0.135 * np.sin(v_phase - 0.35)
        + 0.042 * np.cos(2.0 * v_phase + 0.55)
        + (warp_u - 0.5) * 0.080
    )
    du = x - u_center

    # 1. Wide soft feathered dust veil around the swirl
    halo = np.exp(-((du / 0.255) ** 2)) * (0.58 + 0.42 * wisp)

    # 2. Primary sinuous double-filament Reiner-Gamma ribbon with a soft dark central lane
    core_env = np.exp(-((du / 0.112) ** 2))
    dark_lane_offset = 0.015 * np.sin(v_phase + 0.75) + (warp_v - 0.5) * 0.024
    dark_lane = 1.0 - 0.44 * np.exp(-(((du - dark_lane_offset) / 0.028) ** 2))
    main_ribbon = core_env * dark_lane * (0.80 + 0.20 * fine_dust)

    # 3. Secondary wispy flanking filaments and eddies
    du_fil1 = x - (u_center + 0.115 * np.cos(v_phase * 0.5 + 0.35) + 0.054)
    du_fil2 = x - (u_center - 0.110 * np.sin(v_phase * 0.5 - 0.25) - 0.050)
    fil1 = np.exp(-((du_fil1 / 0.048) ** 2)) * np.clip(wisp * 1.25 - 0.16, 0.0, 1.0)
    fil2 = np.exp(-((du_fil2 / 0.044) ** 2)) * np.clip((1.0 - wisp) * 1.25 - 0.18, 0.0, 1.0)

    # Strictly zero alpha at all 4 UV borders (u=0, u=1, v=0, v=1) so polygon edges are invisible
    edge_u = np.sin(np.pi * np.clip(x, 0.0, 1.0)) ** 1.65
    edge_v = np.sin(np.pi * np.clip(y, 0.0, 1.0)) ** 1.20
    border_feather = edge_u * edge_v

    raw_swirl = np.clip(0.42 * halo + 0.74 * main_ribbon + 0.46 * (fil1 + fil2), 0.0, 1.0)
    alpha = np.clip((raw_swirl ** 1.08) * border_feather * 0.88, 0.0, 0.88)

    edge_rgb = _hex_to_rgb01("#AEBAD0")
    mid_rgb = _hex_to_rgb01("#CBD4E6")
    core_rgb = _hex_to_rgb01("#E2E9F6")

    low_m = np.clip(raw_swirl * 2.0, 0.0, 1.0)[..., None]
    high_m = np.clip((raw_swirl - 0.45) * 1.8, 0.0, 1.0)[..., None]
    rgb = edge_rgb * (1.0 - low_m) + mid_rgb * low_m
    rgb = rgb * (1.0 - high_m) + core_rgb * high_m
    rgb += (fine_dust[..., None] - 0.5) * 0.015

    height = np.clip(0.50 + 0.22 * (raw_swirl - 0.5) + 0.10 * (fine_dust - 0.5), 0.0, 1.0)
    normal = _height_to_normal(height, normal_strength=0.45)

    metal = np.zeros((DECAL_SIZE, DECAL_SIZE), dtype=np.float32)
    ao = np.clip(0.92 + 0.08 * raw_swirl, 0.86, 1.0)
    detail = np.clip(0.45 + 0.35 * raw_swirl + 0.20 * fine_dust, 0.0, 1.0)
    smooth = np.clip(0.08 + 0.05 * raw_swirl, 0.05, 0.14)

    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Albedo.png", np.dstack([np.clip(rgb, 0.0, 1.0), alpha]))
    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareSwirl_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_mare_ejecta_ray(out_dir: Path) -> None:
    """Build T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png (512x512 soft, streaky RGBA ejecta dust rays)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, DECAL_SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, DECAL_SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    warp_u = _fbm(DECAL_SIZE, base_freq=2, seed=621)
    n_base = _fbm(DECAL_SIZE, base_freq=3, seed=622)
    n_fine = _fbm(DECAL_SIZE, base_freq=6, seed=623)
    along_mod = _fbm(DECAL_SIZE, base_freq=3, seed=624)

    # Anisotropic streaky lookup: stretched along y (length), compressed across x (width)
    streak_coarse = _sample_wrapped(n_base, x * 4.0 + (warp_u - 0.5) * 0.25, y * 1.0)
    streak_fine = _sample_wrapped(n_fine, x * 8.0 + (warp_u - 0.5) * 0.18, y * 1.0)

    # Soft bundle of radial dust streaks centered around x = 0.5
    ray_centers = (-0.21, -0.11, -0.02, 0.06, 0.14, 0.22)
    ray_widths = (0.065, 0.082, 0.098, 0.086, 0.074, 0.062)
    ray_weights = (0.55, 0.82, 1.00, 0.88, 0.72, 0.52)

    ray_bundle = np.zeros_like(x)
    for rc, rw, rwt in zip(ray_centers, ray_widths, ray_weights):
        c_pos = 0.50 + rc * (0.76 + 0.46 * y) + (warp_u - 0.5) * 0.032
        ray_bundle += rwt * np.exp(-(((x - c_pos) / rw) ** 2))
    ray_bundle = np.clip(ray_bundle / 1.82, 0.0, 1.0)

    streaky_mask = np.clip(0.58 * streak_coarse + 0.42 * streak_fine, 0.0, 1.0)
    streaky_mask = np.clip((streaky_mask - 0.20) / 0.68, 0.0, 1.0)

    # Smooth border feathering envelope: strictly 0.0 at all 4 UV borders
    edge_u = np.sin(np.pi * np.clip(x, 0.0, 1.0)) ** 1.75
    edge_v = np.sin(np.pi * np.clip(y, 0.0, 1.0)) ** 1.30
    border_feather = edge_u * edge_v

    ray_t = np.clip(ray_bundle * (0.42 + 0.58 * streaky_mask) * (0.65 + 0.35 * along_mod), 0.0, 1.0)
    alpha = np.clip((ray_t ** 1.06) * border_feather * 0.74, 0.0, 0.74)

    edge_rgb = _hex_to_rgb01("#5D677C")
    mid_rgb = _hex_to_rgb01("#79849B")
    core_rgb = _hex_to_rgb01("#9AA5BC")

    low_m = np.clip(ray_t * 2.0, 0.0, 1.0)[..., None]
    high_m = np.clip((ray_t - 0.45) * 1.8, 0.0, 1.0)[..., None]
    rgb = edge_rgb * (1.0 - low_m) + mid_rgb * low_m
    rgb = rgb * (1.0 - high_m) + core_rgb * high_m

    height = np.clip(0.50 + 0.18 * (ray_t - 0.5) + 0.08 * (streak_fine - 0.5), 0.0, 1.0)
    normal = _height_to_normal(height, normal_strength=0.40)

    metal = np.full((DECAL_SIZE, DECAL_SIZE), 0.01, dtype=np.float32)
    ao = np.clip(0.90 + 0.10 * ray_t, 0.82, 1.0)
    detail = np.clip(0.40 + 0.40 * ray_t + 0.20 * streak_fine, 0.0, 1.0)
    smooth = np.clip(0.08 + 0.05 * ray_t, 0.06, 0.14)

    _save_rgba_or_rgb(out_dir / "T_R1_MareEjectaRay_Albedo.png", np.dstack([np.clip(rgb, 0.0, 1.0), alpha]))
    _save_rgba_or_rgb(out_dir / "T_R1_MareEjectaRay_Normal.png", normal)
    _save_rgba_or_rgb(out_dir / "T_R1_MareEjectaRay_Mask.png", np.stack([metal, ao, detail, smooth], axis=-1))


def _generate_ilmenite(out_dir: Path) -> None:
    """Build T_R1_Ilmenite_{Albedo,Normal,Mask}.png (256x256 organic FeTiO3 mineral ore, no checker grid)."""
    y, x = np.meshgrid(
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        np.linspace(0.0, 1.0, SIZE, endpoint=False, dtype=np.float32),
        indexing="ij",
    )

    warp_u = _fbm(SIZE, base_freq=3, seed=631)
    warp_v = _fbm(SIZE, base_freq=3, seed=632)
    ore_broad = _fbm(SIZE, base_freq=2, seed=633)
    ore_vein = _fbm(SIZE, base_freq=4, seed=634)
    ore_fine = _fbm(SIZE, base_freq=8, seed=635)
    n_micro = _tileable_noise(SIZE, freq=20, seed=636)

    wx = x + (warp_u - 0.5) * 0.26
    wy = y + (warp_v - 0.5) * 0.26
    broad = _sample_wrapped(ore_broad, wx, wy)
    vein = _sample_wrapped(ore_vein, wx + 0.21, wy - 0.14)
    fine = _sample_wrapped(ore_fine, x + (warp_v - 0.5) * 0.12, y - (warp_u - 0.5) * 0.12)

    luster = np.clip(0.52 * broad + 0.34 * vein + 0.14 * fine, 0.0, 1.0)

    base_black_rgb = _hex_to_rgb01("#232834")
    gunmetal_rgb = _hex_to_rgb01("#424B5C")
    bronze_glint_rgb = _hex_to_rgb01("#615040")

    facet_t = np.clip((luster - 0.22) / 0.58, 0.0, 1.0)[..., None]
    bronze_t = np.clip((vein - 0.38) / 0.48, 0.0, 1.0)[..., None]

    rgb = base_black_rgb * (1.0 - facet_t * 0.65) + gunmetal_rgb * (facet_t * 0.65)
    rgb = rgb * (1.0 - bronze_t * 0.55) + bronze_glint_rgb * (bronze_t * 0.55)
    rgb += (n_micro[..., None] - 0.5) * 0.012

    height = np.clip(0.54 * broad + 0.32 * vein + 0.14 * fine, 0.0, 1.0)
    normal = _height_to_normal(height, normal_strength=1.35)

    # MaskMap: R = Metallic (~0.72), G = MicroAO, B = MicroDetail, A = Smoothness (~0.48)
    metal = np.clip(0.62 + 0.20 * luster, 0.52, 0.84)
    ao = np.clip(0.84 + 0.16 * (luster - 0.35), 0.72, 1.0)
    detail = np.clip(0.42 + 0.36 * vein + 0.16 * fine, 0.15, 0.88)
    smooth = np.clip(0.42 + 0.16 * luster + 0.03 * (n_micro - 0.5), 0.34, 0.58)

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
