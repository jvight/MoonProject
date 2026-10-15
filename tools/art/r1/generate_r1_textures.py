#!/usr/bin/env python3
"""Deterministic procedural texture generator for the R1 stylized-realism material library.

Generates under Assets/_Project/Art/R1/Textures/:
- Shared 8-row trim sheet (512x512):
    T_R1_TrimSheet_Albedo.png
    T_R1_TrimSheet_Normal.png
    T_R1_TrimSheet_Mask.png     (R=Metallic, G=AO, B=Curvature, A=Smoothness)
    T_R1_TrimSheet_Emission.png
- Per-material [0,1] texture sets (256x256) for each R1 library surface so any
  Blender UV unwrap on a material slot never bleeds across neighbouring trim rows:
    T_R1_<Surface>_Albedo.png
    T_R1_<Surface>_Normal.png
    T_R1_<Surface>_Mask.png
    T_R1_LampAmber_Emission.png
"""

from __future__ import annotations

from pathlib import Path
import numpy as np
from PIL import Image


REPO_ROOT = Path(__file__).resolve().parents[3]
OUTPUT_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Textures"


def _tileable_noise(res: int, freq: int, seed: int) -> np.ndarray:
    """Generate smooth tileable 2D value noise in [0, 1] at resolution `res`."""
    rng = np.random.default_rng(seed)
    grid = rng.random((freq, freq), dtype=np.float32)

    coords = np.linspace(0.0, float(freq), res, endpoint=False, dtype=np.float32)
    gi = np.floor(coords).astype(np.int32)
    gf = coords - gi
    u = gf * gf * (3.0 - 2.0 * gf)

    i0 = gi % freq
    i1 = (gi + 1) % freq

    g00 = grid[np.ix_(i0, i0)]
    g10 = grid[np.ix_(i0, i1)]
    g01 = grid[np.ix_(i1, i0)]
    g11 = grid[np.ix_(i1, i1)]

    ux = u[None, :]
    uy = u[:, None]
    nx0 = g00 * (1.0 - ux) + g10 * ux
    nx1 = g01 * (1.0 - ux) + g11 * ux
    return nx0 * (1.0 - uy) + nx1 * uy


def _fbm(res: int, base_freq: int, seed: int) -> np.ndarray:
    return (
        0.55 * _tileable_noise(res, base_freq, seed)
        + 0.30 * _tileable_noise(res, base_freq * 2, seed + 101)
        + 0.15 * _tileable_noise(res, base_freq * 4, seed + 202)
    )


def _height_to_normal_and_curvature(
    height: np.ndarray, normal_strength: float, curv_strength: float
) -> tuple[np.ndarray, np.ndarray]:
    """Convert a 2D height field in [0, 1] to tangent-space normal RGB [0..1] and curvature [0..1]."""
    dh_dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    dh_dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5

    nx = -dh_dx * normal_strength
    ny = -dh_dy * normal_strength
    nz = np.ones_like(height, dtype=np.float32)
    inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    nx *= inv_len
    ny *= inv_len
    nz *= inv_len

    normal_rgb = np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)

    laplacian = (
        np.roll(height, -1, axis=1)
        + np.roll(height, 1, axis=1)
        + np.roll(height, -1, axis=0)
        + np.roll(height, 1, axis=0)
        - 4.0 * height
    )
    curvature = np.clip(0.5 - laplacian * curv_strength, 0.0, 1.0)
    return np.clip(normal_rgb, 0.0, 1.0), curvature


def _save_rgba_or_rgb(path: Path, array_01: np.ndarray) -> None:
    clamped = np.clip(np.rint(array_01 * 255.0), 0, 255).astype(np.uint8)
    mode = "RGBA" if clamped.shape[-1] == 4 else "RGB"
    # Flip vertically so V=0 is bottom in Unity UV space
    Image.fromarray(np.flipud(clamped), mode=mode).save(path, compress_level=9)


def _panel_bevel_frame(u: np.ndarray, v: np.ndarray, margin: float = 0.06) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Return (height, ao, convex_boost) for a subtle bevelled panel border so [0,1] unwraps have edge definition."""
    edge_dist = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
    groove = np.clip(edge_dist / max(margin, 1e-4), 0.0, 1.0)
    groove_smooth = groove * groove * (3.0 - 2.0 * groove)
    # Slight raised lip just inside the seam so curvature B > 0.5 catches edge wear
    lip = np.exp(-((edge_dist - margin * 1.15) ** 2) / (2.0 * (margin * 0.38) ** 2))
    height = 0.50 * groove_smooth + 0.14 * lip
    ao = 0.72 + 0.28 * groove_smooth
    convex = 0.26 * lip
    return height, ao, convex


def _generate_surface_maps(
    name: str,
    res: int,
    base_rgb: tuple[float, float, float],
    metallic_val: float,
    smoothness_val: float,
    seed: int,
    style: str,
) -> dict[str, np.ndarray]:
    coords = (np.arange(res, dtype=np.float32) + 0.5) / float(res)
    u, v = np.meshgrid(coords, coords)
    noise = _fbm(res, 4, seed)
    fine = _tileable_noise(res, 16, seed + 55)

    frame_h, frame_ao, frame_convex = _panel_bevel_frame(u, v, margin=0.055)
    height = frame_h + (noise - 0.5) * 0.06 + (fine - 0.5) * 0.025
    ao = frame_ao.copy()
    convex_extra = frame_convex.copy()

    albedo = np.empty((res, res, 3), dtype=np.float32)
    albedo[..., 0] = base_rgb[0]
    albedo[..., 1] = base_rgb[1]
    albedo[..., 2] = base_rgb[2]
    albedo *= 0.95 + 0.10 * noise[..., None]

    metallic = np.full((res, res), metallic_val, dtype=np.float32)
    smoothness = np.full((res, res), smoothness_val, dtype=np.float32) + (noise - 0.5) * 0.06
    emission = np.zeros((res, res, 3), dtype=np.float32)

    if style == "enamel":
        # Subtle horizontal seam at mid-height + 4 corner fasteners
        mid_seam = np.exp(-((v - 0.5) ** 2) / (2.0 * 0.008**2))
        mid_lip = np.exp(-((np.abs(v - 0.5) - 0.022) ** 2) / (2.0 * 0.009**2))
        height -= 0.18 * mid_seam
        height += 0.08 * mid_lip
        ao *= 1.0 - 0.22 * mid_seam
        convex_extra += 0.20 * mid_lip
        for cx, cy in ((0.12, 0.12), (0.88, 0.12), (0.12, 0.88), (0.88, 0.88)):
            r = np.sqrt((u - cx) ** 2 + (v - cy) ** 2)
            bolt = np.clip(1.0 - r / 0.028, 0.0, 1.0)
            ring = np.exp(-((r - 0.030) ** 2) / (2.0 * 0.006**2))
            height += 0.12 * bolt - 0.08 * ring
            ao *= 1.0 - 0.18 * ring
            convex_extra += 0.18 * bolt

    elif style == "stripe":
        # Subtle worn orange trim with diagonal weave/scuff variation
        diag = 0.5 + 0.5 * np.sin((u * 3.0 + v * 1.0) * np.pi * 2.0)
        scuff = (noise * 0.7 + fine * 0.3) * (0.85 + 0.15 * diag)
        height += (scuff - 0.5) * 0.08
        convex_extra += np.clip(scuff - 0.58, 0.0, 0.4) * 0.45
        smoothness -= (1.0 - scuff) * 0.08

    elif style == "greymetal":
        # Painted structural metal with subtle horizontal rib ridges
        ribs = 0.5 + 0.5 * np.cos(v * 6.0 * np.pi * 2.0)
        height += 0.07 * ribs
        convex_extra += 0.14 * np.clip(ribs - 0.65, 0.0, 1.0)
        ao *= 0.92 + 0.08 * ribs

    elif style == "steel":
        # Anisotropic brushed bare steel
        brush = _tileable_noise(res, 32, seed + 77)
        brush_h = 0.65 * np.roll(brush, 1, axis=1) + 0.35 * brush
        height += (brush_h - 0.5) * 0.07
        albedo *= 0.94 + 0.12 * brush_h[..., None]
        smoothness += (brush_h - 0.5) * 0.10
        convex_extra += 0.12 * np.clip(brush_h - 0.6, 0.0, 1.0)

    elif style == "rubber":
        # Ribbed dark matte rubber bellow/gasket
        ribs = 0.5 + 0.5 * np.sin(v * 10.0 * np.pi * 2.0)
        height = 0.25 + 0.42 * ribs + (fine - 0.5) * 0.04
        ao = 0.68 + 0.32 * ribs
        convex_extra = 0.18 * np.clip(ribs - 0.65, 0.0, 1.0)

    elif style == "foil":
        # Crinkled multi-facet Kapton gold foil
        crinkle_a = _tileable_noise(res, 10, seed + 13)
        crinkle_b = _tileable_noise(res, 18, seed + 29)
        ridges = 1.0 - np.abs(crinkle_a * 2.0 - 1.0)
        facets = 1.0 - np.abs(crinkle_b * 2.0 - 1.0)
        crinkle_h = 0.55 * ridges + 0.45 * facets
        height = 0.20 + 0.55 * crinkle_h
        ao = 0.78 + 0.22 * crinkle_h
        convex_extra = 0.30 * np.clip(crinkle_h - 0.60, 0.0, 1.0)
        albedo *= 0.90 + 0.20 * crinkle_h[..., None]

    elif style == "glass":
        # Smooth dark visor glass with crisp bevel frame
        height = frame_h * 0.45 + (noise - 0.5) * 0.01
        ao = 0.88 + 0.12 * (frame_ao - 0.72) / 0.28
        convex_extra = frame_convex * 0.7

    elif style == "copper":
        # Turned/brushed copper with subtle concentric/linear grain
        grain = _tileable_noise(res, 20, seed + 41)
        height += (grain - 0.5) * 0.06
        albedo *= 0.94 + 0.12 * grain[..., None]
        convex_extra += 0.15 * np.clip(grain - 0.60, 0.0, 1.0)

    elif style == "patch":
        # Sage-green repair patch with stitched/riveted perimeter inset
        inset_dist = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
        seam_ring = np.exp(-((inset_dist - 0.09) ** 2) / (2.0 * 0.010**2))
        height -= 0.14 * seam_ring
        ao *= 1.0 - 0.20 * seam_ring
        convex_extra += 0.16 * np.exp(-((inset_dist - 0.065) ** 2) / (2.0 * 0.012**2))

    elif style == "lamp":
        # Warm amber lamp lens with Fresnel concentric ribs and hot central filament core
        rx = (u - 0.5) * 2.0
        ry = (v - 0.5) * 2.0
        r = np.sqrt(rx * rx + ry * ry)
        fresnel_rings = 0.5 + 0.5 * np.cos(np.clip(r, 0.0, 1.0) * 5.0 * np.pi * 2.0)
        dome = np.clip(1.0 - r * r * 0.65, 0.1, 1.0)
        height = 0.30 + 0.45 * dome + 0.08 * fresnel_rings
        ao = 0.75 + 0.25 * dome
        # Radial glow profile: warm body across the lens, >0.85 hot core in the centre
        core_profile = np.exp(-(r * r) / (2.0 * 0.26**2))
        body_profile = np.clip(1.0 - 0.42 * r, 0.35, 1.0) * (0.92 + 0.08 * fresnel_rings)
        em_level = np.clip(0.58 * body_profile + 0.42 * core_profile, 0.0, 1.0)
        emission[..., 0] = em_level * 1.00
        emission[..., 1] = em_level * 0.68 + 0.20 * (core_profile**2)
        emission[..., 2] = em_level * 0.22 + 0.32 * (core_profile**3)

    normal_rgb, curvature = _height_to_normal_and_curvature(
        height,
        normal_strength=10.0 if style == "foil" else 6.0,
        curv_strength=3.2,
    )
    curvature = np.clip(curvature + convex_extra, 0.0, 1.0)

    mask_rgba = np.stack(
        [
            np.clip(metallic, 0.0, 1.0),
            np.clip(ao, 0.0, 1.0),
            np.clip(curvature, 0.0, 1.0),
            np.clip(smoothness, 0.0, 1.0),
        ],
        axis=-1,
    )

    albedo_rgba = np.concatenate(
        [np.clip(albedo, 0.0, 1.0), np.ones((res, res, 1), dtype=np.float32)], axis=-1
    )

    return {
        "name": name,
        "albedo": albedo_rgba,
        "normal": normal_rgb,
        "mask": mask_rgba,
        "emission": emission,
    }


def _generate_trim_sheet(res: int, surfaces: list[dict[str, np.ndarray]]) -> dict[str, np.ndarray]:
    """Assemble a 512x512 8-row shared trim sheet from the R1 surface definitions."""
    albedo = np.zeros((res, res, 4), dtype=np.float32)
    normal = np.zeros((res, res, 3), dtype=np.float32)
    mask = np.zeros((res, res, 4), dtype=np.float32)
    emission = np.zeros((res, res, 3), dtype=np.float32)

    band_h = res // 8
    by_name = {s["name"]: s for s in surfaces}

    # 8 horizontal rows (bottom to top in UV V):
    # Row 0: CreamEnamel
    # Row 1: OrangeStripe
    # Row 2: GreyMetal
    # Row 3: BareSteel
    # Row 4: Rubber
    # Row 5: GoldFoil
    # Row 6: DarkGlass (left half) + Copper (right half)
    # Row 7: SagePatch (left half) + LampAmber (right half)
    single_rows = [
        (0, "CreamEnamel"),
        (1, "OrangeStripe"),
        (2, "GreyMetal"),
        (3, "BareSteel"),
        (4, "Rubber"),
        (5, "GoldFoil"),
    ]

    for row_idx, surf_name in single_rows:
        s = by_name[surf_name]
        y0, y1 = row_idx * band_h, (row_idx + 1) * band_h
        # Tile horizontally twice across the 512 width, resample vertically to band_h
        s_alb = np.repeat(s["albedo"][::4, :, :], 2, axis=1)[:band_h, :res]
        s_nrm = np.repeat(s["normal"][::4, :, :], 2, axis=1)[:band_h, :res]
        s_msk = np.repeat(s["mask"][::4, :, :], 2, axis=1)[:band_h, :res]
        albedo[y0:y1] = s_alb
        normal[y0:y1] = s_nrm
        mask[y0:y1] = s_msk

    split_rows = [
        (6, "DarkGlass", "Copper"),
        (7, "SagePatch", "LampAmber"),
    ]
    half_w = res // 2
    for row_idx, left_name, right_name in split_rows:
        y0, y1 = row_idx * band_h, (row_idx + 1) * band_h
        left = by_name[left_name]
        right = by_name[right_name]

        albedo[y0:y1, :half_w] = left["albedo"][::4, :half_w]
        normal[y0:y1, :half_w] = left["normal"][::4, :half_w]
        mask[y0:y1, :half_w] = left["mask"][::4, :half_w]
        emission[y0:y1, :half_w] = left["emission"][::4, :half_w]

        albedo[y0:y1, half_w:] = right["albedo"][::4, :half_w]
        normal[y0:y1, half_w:] = right["normal"][::4, :half_w]
        mask[y0:y1, half_w:] = right["mask"][::4, :half_w]
        emission[y0:y1, half_w:] = right["emission"][::4, :half_w]

    return {
        "albedo": albedo,
        "normal": normal,
        "mask": mask,
        "emission": emission,
    }


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    surf_res = 256

    specs = [
        ("CreamEnamel", (0.91, 0.89, 0.83), 0.04, 0.72, 101, "enamel"),
        ("OrangeStripe", (0.90, 0.43, 0.14), 0.05, 0.58, 202, "stripe"),
        ("GreyMetal", (0.43, 0.46, 0.51), 0.35, 0.56, 303, "greymetal"),
        ("BareSteel", (0.68, 0.71, 0.75), 0.88, 0.68, 404, "steel"),
        ("Rubber", (0.14, 0.15, 0.18), 0.02, 0.22, 505, "rubber"),
        ("GoldFoil", (0.92, 0.69, 0.22), 0.92, 0.78, 606, "foil"),
        ("DarkGlass", (0.07, 0.09, 0.13), 0.10, 0.92, 707, "glass"),
        ("Copper", (0.83, 0.46, 0.27), 0.90, 0.72, 808, "copper"),
        ("SagePatch", (0.50, 0.61, 0.52), 0.04, 0.48, 909, "patch"),
        ("LampAmber", (0.98, 0.72, 0.28), 0.05, 0.84, 1010, "lamp"),
    ]

    surfaces: list[dict[str, np.ndarray]] = []
    for name, rgb, metallic, smoothness, seed, style in specs:
        surf = _generate_surface_maps(name, surf_res, rgb, metallic, smoothness, seed, style)
        surfaces.append(surf)
        _save_rgba_or_rgb(OUTPUT_DIR / f"T_R1_{name}_Albedo.png", surf["albedo"])
        _save_rgba_or_rgb(OUTPUT_DIR / f"T_R1_{name}_Normal.png", surf["normal"])
        _save_rgba_or_rgb(OUTPUT_DIR / f"T_R1_{name}_Mask.png", surf["mask"])
        if name == "LampAmber":
            _save_rgba_or_rgb(OUTPUT_DIR / f"T_R1_{name}_Emission.png", surf["emission"])

    trim = _generate_trim_sheet(512, surfaces)
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_TrimSheet_Albedo.png", trim["albedo"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_TrimSheet_Normal.png", trim["normal"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_TrimSheet_Mask.png", trim["mask"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_TrimSheet_Emission.png", trim["emission"])

    print(f"Generated {len(list(OUTPUT_DIR.glob('*.png')))} PNG textures in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
