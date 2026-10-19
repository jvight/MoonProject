#!/usr/bin/env python3
"""Deterministic procedural texture generator for the R1 stylized-realism material library.

Generates under Assets/_Project/Art/R1/Textures/:
- Per-material seamless, tileable surface detail texture sets (256x256) for each R1 library slot
  (grain, fine scratches, slight mottling; no baked panel frames, seam bands, bolts, or ribs):
    T_R1_<Surface>_Albedo.png
    T_R1_<Surface>_Normal.png
    T_R1_<Surface>_Mask.png     (R=Metallic, G=MicroWearNoise, B=MicroCavityNoise, A=Smoothness)
    T_R1_LampAmber_Emission.png
- Shared 8-row trim sheet (512x512) with panel seams, fasteners, and surface strips:
    T_R1_TrimSheet_Albedo.png
    T_R1_TrimSheet_Normal.png
    T_R1_TrimSheet_Mask.png
    T_R1_TrimSheet_Emission.png
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


def _fine_scratches(res: int, seed: int) -> np.ndarray:
    """Seamless high-frequency directional micro-scratches in [0, 1]."""
    n1 = _tileable_noise(res, 24, seed + 303)
    n2 = _tileable_noise(res, 48, seed + 404)
    aniso_x = 0.65 * np.roll(n1, 1, axis=1) + 0.35 * np.roll(n2, -1, axis=1)
    aniso_y = 0.65 * np.roll(n1, 1, axis=0) + 0.35 * np.roll(n2, -1, axis=0)
    scratches = np.maximum(
        np.clip(1.0 - np.abs(aniso_x - 0.5) * 10.0, 0.0, 1.0),
        np.clip(1.0 - np.abs(aniso_y - 0.5) * 12.0, 0.0, 1.0) * 0.7,
    )
    return scratches.astype(np.float32)


def _height_to_normal(height: np.ndarray, normal_strength: float) -> np.ndarray:
    """Convert a 2D height field in [0, 1] to tangent-space normal RGB [0..1]."""
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
    return np.clip(normal_rgb, 0.0, 1.0)


def _save_rgba_or_rgb(path: Path, array_01: np.ndarray) -> None:
    clamped = np.clip(np.rint(array_01 * 255.0), 0, 255).astype(np.uint8)
    mode = "RGBA" if clamped.shape[-1] == 4 else "RGB"
    # Flip vertically so V=0 is bottom in Unity UV space
    Image.fromarray(np.flipud(clamped), mode=mode).save(path, compress_level=9)


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
    mottle = _fbm(res, 6, seed)
    fine = _tileable_noise(res, 24, seed + 55)
    ultra_fine = _tileable_noise(res, 48, seed + 88)
    scratches = _fine_scratches(res, seed + 123)

    # Seamless, tileable micro-surface height (no panel frames, bolts, or ribs)
    height = 0.50 + (mottle - 0.5) * 0.025 + (fine - 0.5) * 0.018 + (ultra_fine - 0.5) * 0.010

    albedo = np.empty((res, res, 3), dtype=np.float32)
    albedo[..., 0] = base_rgb[0]
    albedo[..., 1] = base_rgb[1]
    albedo[..., 2] = base_rgb[2]
    albedo *= 0.97 + 0.06 * mottle[..., None] + 0.02 * (fine[..., None] - 0.5)

    metallic = np.full((res, res), metallic_val, dtype=np.float32)
    smoothness = np.full((res, res), smoothness_val, dtype=np.float32) + (mottle - 0.5) * 0.04
    micro_wear = np.clip(0.5 + (fine - 0.5) * 0.45 + scratches * 0.25, 0.0, 1.0)
    micro_cavity = np.clip(0.5 + (1.0 - mottle - 0.5) * 0.45, 0.0, 1.0)
    emission = np.zeros((res, res, 3), dtype=np.float32)
    normal_strength = 4.0

    if style == "enamel":
        # Smooth baked cream enamel with subtle orange-peel micro-grain and fine hairline scratches
        height += (fine - 0.5) * 0.012 - scratches * 0.008
        smoothness += (fine - 0.5) * 0.03 - scratches * 0.04
        normal_strength = 3.0

    elif style == "stripe":
        # Warm, slightly dusty orange painted stripe with subtle paint roller grain and scuff mottling
        paint_grain = 0.6 * fine + 0.4 * ultra_fine
        height += (paint_grain - 0.5) * 0.022 - scratches * 0.012
        albedo *= 0.97 + 0.06 * paint_grain[..., None]
        smoothness -= scratches * 0.05
        normal_strength = 3.5

    elif style == "greymetal":
        # Seamless painted structural grey metal with fine bead-blasted grain and micro-scratches
        blast = 0.5 * fine + 0.5 * ultra_fine
        height += (blast - 0.5) * 0.028 - scratches * 0.015
        albedo *= 0.96 + 0.08 * blast[..., None]
        smoothness += (blast - 0.5) * 0.05 - scratches * 0.04
        normal_strength = 4.2

    elif style == "steel":
        # Seamless anisotropic brushed bare steel
        brush_raw = _tileable_noise(res, 40, seed + 77)
        brush = (
            0.40 * brush_raw
            + 0.35 * np.roll(brush_raw, 1, axis=1)
            + 0.25 * np.roll(brush_raw, -1, axis=1)
        )
        height = 0.50 + (brush - 0.5) * 0.035 - scratches * 0.015
        albedo *= 0.95 + 0.10 * brush[..., None]
        smoothness += (brush - 0.5) * 0.08
        normal_strength = 4.5

    elif style == "rubber":
        # Seamless matte dark elastomer with fine stippled vulcanized grain (no baked ribs)
        stipple = 0.55 * ultra_fine + 0.45 * fine
        height = 0.50 + (stipple - 0.5) * 0.032
        albedo *= 0.96 + 0.08 * stipple[..., None]
        smoothness += (stipple - 0.5) * 0.03
        normal_strength = 4.5

    elif style == "foil":
        # Fine-crinkled golden Kapton thermal foil with sharp small angular facets and glints
        c1 = _tileable_noise(res, 24, seed + 13)
        c2 = _tileable_noise(res, 40, seed + 29)
        c3 = _tileable_noise(res, 64, seed + 47)
        c4 = _tileable_noise(res, 48, seed + 71)
        r1 = 1.0 - np.abs(c1 * 2.0 - 1.0)
        r2 = 1.0 - np.abs(c2 * 2.0 - 1.0)
        r3 = 1.0 - np.abs(c3 * 2.0 - 1.0)
        r4 = 1.0 - np.abs(c4 * 2.0 - 1.0)
        # Sharp crease network + small angular glint facets
        crinkle_h = 0.30 * (r1**1.4) + 0.35 * (r2**1.6) + 0.20 * (r3**1.8) + 0.15 * r4
        glints = np.clip((r2 * r3 - 0.42) * 3.2, 0.0, 1.0)
        height = 0.35 + 0.35 * crinkle_h + 0.08 * glints
        # Keep hue distinctly golden-yellow (never orange) with bright pale-gold glints
        gold_tint = np.empty_like(albedo)
        gold_tint[..., 0] = base_rgb[0] * (0.94 + 0.08 * crinkle_h) + 0.06 * glints
        gold_tint[..., 1] = base_rgb[1] * (0.93 + 0.09 * crinkle_h) + 0.11 * glints
        gold_tint[..., 2] = base_rgb[2] * (0.90 + 0.12 * crinkle_h) + 0.22 * glints
        albedo = np.clip(gold_tint, 0.0, 1.0)
        smoothness = np.clip(smoothness_val + (crinkle_h - 0.5) * 0.14 + glints * 0.12, 0.55, 0.96)
        micro_wear = np.clip(0.5 + (crinkle_h - 0.5) * 0.6, 0.0, 1.0)
        normal_strength = 9.5

    elif style == "glass":
        # Seamless smooth dark optical glass (no baked frame)
        height = 0.50 + (mottle - 0.5) * 0.005 + (fine - 0.5) * 0.003
        albedo *= 0.98 + 0.04 * mottle[..., None]
        smoothness = np.full((res, res), smoothness_val, dtype=np.float32) + (fine - 0.5) * 0.015
        normal_strength = 1.5

    elif style == "copper":
        # Seamless brownish-pink brushed copper metal
        grain_raw = _tileable_noise(res, 32, seed + 41)
        grain = 0.55 * grain_raw + 0.45 * np.roll(grain_raw, 1, axis=1)
        height = 0.50 + (grain - 0.5) * 0.028 - scratches * 0.012
        albedo *= 0.95 + 0.10 * grain[..., None]
        smoothness += (grain - 0.5) * 0.07
        normal_strength = 4.0

    elif style == "brass":
        # Seamless warm golden turned/brushed brass for lamp bezels, lens rings, and fittings
        grain_raw = _tileable_noise(res, 36, seed + 53)
        grain = (
            0.50 * grain_raw
            + 0.30 * np.roll(grain_raw, 1, axis=1)
            + 0.20 * np.roll(grain_raw, -1, axis=1)
        )
        height = 0.50 + (grain - 0.5) * 0.022 - scratches * 0.010
        albedo *= 0.96 + 0.08 * grain[..., None]
        smoothness += (grain - 0.5) * 0.06
        normal_strength = 3.6

    elif style == "patch":
        # Seamless sage-green coated fabric/composite patch with subtle woven texture (no perimeter frame)
        weave_u = 0.5 + 0.5 * np.sin(u * 48.0 * np.pi * 2.0)
        weave_v = 0.5 + 0.5 * np.sin(v * 48.0 * np.pi * 2.0)
        weave = (0.5 * weave_u + 0.5 * weave_v) * 0.35 + fine * 0.65
        height = 0.50 + (weave - 0.5) * 0.024
        albedo *= 0.96 + 0.08 * weave[..., None]
        smoothness += (weave - 0.5) * 0.04
        normal_strength = 3.8

    elif style == "lamp":
        # Smooth glowing warm amber lens with a small hot core (no concentric bullseye rings)
        rx = (u - 0.5) * 2.0
        ry = (v - 0.5) * 2.0
        r = np.sqrt(rx * rx + ry * ry)
        height = 0.50 + (mottle - 0.5) * 0.006 + (fine - 0.5) * 0.004
        normal_strength = 1.5
        # Smooth radial glow: warm amber body below bloom threshold + small hot centre core (> 0.85)
        core_profile = np.exp(-(r * r) / (2.0 * 0.17**2))
        halo_profile = np.exp(-(r * r) / (2.0 * 0.52**2))
        body_profile = 0.54 + 0.24 * halo_profile
        em_level = np.clip(body_profile + 0.36 * core_profile, 0.0, 1.0)
        emission[..., 0] = em_level * 1.00
        emission[..., 1] = em_level * 0.70 + 0.22 * (core_profile**2)
        emission[..., 2] = em_level * 0.24 + 0.34 * (core_profile**3)

    elif style == "solar":
        # Tileable photovoltaic solar cell: deep blue-violet glass cells + fine silver busbars and collector grid
        # 2x2 cells per [0, 1] UV tile so individual cells and busbars read clearly on both spheres and panels
        cell_u = (u * 2.0) % 1.0
        cell_v = (v * 2.0) % 1.0

        # Thin dark inter-cell gap at cell boundary (width ~ 1.8% of cell)
        edge_u = np.minimum(cell_u, 1.0 - cell_u)
        edge_v = np.minimum(cell_v, 1.0 - cell_v)
        cell_edge = np.minimum(edge_u, edge_v)
        gap_mask = 1.0 - np.clip((cell_edge - 0.008) / 0.010, 0.0, 1.0)

        # Two vertical silver busbars per cell at cell_u = 0.26 and 0.74
        busbar_dist = np.minimum(np.abs(cell_u - 0.26), np.abs(cell_u - 0.74))
        busbar_mask = (1.0 - np.clip((busbar_dist - 0.014) / 0.008, 0.0, 1.0)) * (1.0 - gap_mask)

        # Fine horizontal silver collector fingers (10 per cell)
        finger_phase = np.abs(((cell_v * 10.0 + 0.5) % 1.0) - 0.5)
        finger_mask = (1.0 - np.clip((finger_phase - 0.035) / 0.025, 0.0, 1.0)) * (1.0 - gap_mask) * 0.85

        silver_mask = np.clip(np.maximum(busbar_mask, finger_mask), 0.0, 1.0)

        # Deep blue-violet silicon cell base with subtle crystalline wafer variation
        wafer_var = 0.96 + 0.08 * (0.6 * mottle + 0.4 * fine)
        cell_rgb = np.stack(
            [
                base_rgb[0] * wafer_var,
                base_rgb[1] * wafer_var,
                base_rgb[2] * (0.97 + 0.06 * mottle),
            ],
            axis=-1,
        )
        silver_rgb = np.array([0.86, 0.88, 0.92], dtype=np.float32)
        gap_rgb = np.array([0.10, 0.12, 0.22], dtype=np.float32)

        albedo = cell_rgb * (1.0 - gap_mask[..., None]) + gap_rgb * gap_mask[..., None]
        albedo = albedo * (1.0 - silver_mask[..., None]) + silver_rgb * silver_mask[..., None]

        # Metallic/smooth on the silver grid; high smoothness low-metallic glass on the blue-violet cell
        metallic = 0.06 * (1.0 - silver_mask) + 0.92 * silver_mask
        smoothness = 0.90 * (1.0 - silver_mask) + 0.82 * silver_mask - 0.35 * gap_mask
        height = 0.50 + 0.035 * silver_mask - 0.045 * gap_mask + (fine - 0.5) * 0.005
        normal_strength = 4.5

    elif style == "basalt":
        # Weathered lunar basalt / breccia stone: matte volcanic grain, concave vesicle pits,
        # sub-angular mineral clasts, and soft conchoidal fracture steps
        coarse = _fbm(res, 4, seed + 19)
        med = _fbm(res, 10, seed + 37)
        cleave = 1.0 - np.abs(mottle * 2.0 - 1.0)

        rng = np.random.default_rng(seed + 410)
        vesicle_bowl = np.zeros((res, res), dtype=np.float32)
        vesicle_rim = np.zeros((res, res), dtype=np.float32)
        # 110 concave circular/oval gas vesicle pits across scales
        for i in range(110):
            vx = float(rng.random())
            vy = float(rng.random())
            vr = float(rng.uniform(0.010, 0.042)) if i < 45 else float(rng.uniform(0.005, 0.016))
            vdepth = float(rng.uniform(0.45, 0.95))
            aspect = float(rng.uniform(0.72, 1.38))
            ang = float(rng.uniform(0.0, np.pi))
            dx = (u - vx + 0.5) % 1.0 - 0.5
            dy = (v - vy + 0.5) % 1.0 - 0.5
            rx = (dx * np.cos(ang) + dy * np.sin(ang)) * aspect
            ry = -dx * np.sin(ang) + dy * np.cos(ang)
            rn = np.sqrt(rx * rx + ry * ry) / vr
            pit = (np.clip(1.0 - rn * rn, 0.0, 1.0) ** 1.6) * vdepth
            rim = np.exp(-(((rn - 1.05) / 0.22) ** 2)) * vdepth * 0.28
            vesicle_bowl = np.maximum(vesicle_bowl, pit)
            vesicle_rim = np.maximum(vesicle_rim, rim)

        # 36 sub-angular breccia mineral clasts
        clasts = np.zeros((res, res), dtype=np.float32)
        for _ in range(36):
            cx = float(rng.random())
            cy = float(rng.random())
            cr = float(rng.uniform(0.016, 0.048))
            ch = float(rng.uniform(0.30, 0.85))
            dx = (u - cx + 0.5) % 1.0 - 0.5
            dy = (v - cy + 0.5) % 1.0 - 0.5
            rn = (np.abs(dx) ** 1.6 + np.abs(dy) ** 1.6) ** (1.0 / 1.6) / cr
            clast = np.clip(1.0 - rn * rn, 0.0, 1.0) ** 0.85 * ch
            clasts = np.maximum(clasts, clast)

        flecks = np.clip((fine - 0.64) * 2.8, 0.0, 1.0)
        height = (
            0.50
            + (coarse - 0.5) * 0.055
            + (med - 0.5) * 0.035
            + (cleave - 0.5) * 0.028
            + (fine - 0.5) * 0.018
            + clasts * 0.032
            + vesicle_rim * 0.022
            - vesicle_bowl * 0.085
            + flecks * 0.012
        )
        albedo = np.empty((res, res, 3), dtype=np.float32)
        albedo[..., 0] = base_rgb[0]
        albedo[..., 1] = base_rgb[1]
        albedo[..., 2] = base_rgb[2]
        albedo *= (
            0.92
            + 0.12 * coarse[..., None]
            + 0.06 * (med[..., None] - 0.5)
            + 0.09 * clasts[..., None]
            + 0.05 * flecks[..., None]
            + 0.03 * vesicle_rim[..., None]
            - 0.24 * vesicle_bowl[..., None]
        )
        metallic = np.full((res, res), metallic_val, dtype=np.float32) + flecks * 0.03
        smoothness = (
            np.full((res, res), smoothness_val, dtype=np.float32)
            + (coarse - 0.5) * 0.04
            + clasts * 0.05
            + flecks * 0.05
            - vesicle_bowl * 0.09
        )
        # Mask.g feeds microAo in R1LitCore.hlsl: darken strongly inside vesicle pits and crevices
        micro_wear = np.clip(
            0.78 - vesicle_bowl * 0.72 - np.clip(0.5 - coarse, 0.0, 0.5) * 0.45 + clasts * 0.14,
            0.04,
            1.0,
        )
        micro_cavity = np.clip(
            0.72 - vesicle_bowl * 0.65 - (0.5 - coarse) * 0.35,
            0.04,
            1.0,
        )
        normal_strength = 8.4

    normal_rgb = _height_to_normal(height, normal_strength=normal_strength)

    mask_rgba = np.stack(
        [
            np.clip(metallic, 0.0, 1.0),
            np.clip(micro_wear, 0.0, 1.0),
            np.clip(micro_cavity, 0.0, 1.0),
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


def _tileable_aniso_noise(res: int, freq_u: int, freq_v: int, seed: int) -> np.ndarray:
    """Generate smooth tileable 2D value noise in [0, 1] with independent (freq_u, freq_v) periods."""
    rng = np.random.default_rng(seed)
    grid = rng.random((freq_v, freq_u), dtype=np.float32)

    cu = np.linspace(0.0, float(freq_u), res, endpoint=False, dtype=np.float32)
    cv = np.linspace(0.0, float(freq_v), res, endpoint=False, dtype=np.float32)
    gi_u = np.floor(cu).astype(np.int32)
    gi_v = np.floor(cv).astype(np.int32)
    gf_u = cu - gi_u
    gf_v = cv - gi_v
    # Quintic C2 interpolation so derivatives (normals) have zero grid-line artifacts
    uu = gf_u * gf_u * gf_u * (gf_u * (gf_u * 6.0 - 15.0) + 10.0)
    uv = gf_v * gf_v * gf_v * (gf_v * (gf_v * 6.0 - 15.0) + 10.0)

    u0 = gi_u % freq_u
    u1 = (gi_u + 1) % freq_u
    v0 = gi_v % freq_v
    v1 = (gi_v + 1) % freq_v

    g00 = grid[np.ix_(v0, u0)]
    g10 = grid[np.ix_(v0, u1)]
    g01 = grid[np.ix_(v1, u0)]
    g11 = grid[np.ix_(v1, u1)]

    ux = uu[None, :]
    uy = uv[:, None]
    nx0 = g00 * (1.0 - ux) + g10 * ux
    nx1 = g01 * (1.0 - ux) + g11 * ux
    return nx0 * (1.0 - uy) + nx1 * uy


def _sample_tileable_bilinear(field: np.ndarray, u_coord: np.ndarray, v_coord: np.ndarray) -> np.ndarray:
    """Sample a tileable (res, res) array at periodic coordinates (u_coord, v_coord) in [0, 1)."""
    res = field.shape[0]
    x = (u_coord % 1.0) * float(res)
    y = (v_coord % 1.0) * float(res)
    x0 = np.floor(x).astype(np.int32) % res
    y0 = np.floor(y).astype(np.int32) % res
    x1 = (x0 + 1) % res
    y1 = (y0 + 1) % res
    fx = (x - np.floor(x)).astype(np.float32)
    fy = (y - np.floor(y)).astype(np.float32)
    v00 = field[y0, x0]
    v10 = field[y0, x1]
    v01 = field[y1, x0]
    v11 = field[y1, x1]
    return (v00 * (1.0 - fx) + v10 * fx) * (1.0 - fy) + (v01 * (1.0 - fx) + v11 * fx) * fy


def _gaussian_blur_tileable(field: np.ndarray, sigma_px: float) -> np.ndarray:
    """Apply an exact periodic 2D Gaussian blur in frequency domain on a tileable (H, W) array."""
    if sigma_px <= 0.05:
        return field.astype(np.float32)
    h, w = field.shape[:2]
    fy = np.fft.fftfreq(h, d=1.0).astype(np.float32)[:, None]
    fx = np.fft.rfftfreq(w, d=1.0).astype(np.float32)[None, :]
    gauss_fft = np.exp(-2.0 * (np.pi ** 2) * (sigma_px ** 2) * (fx * fx + fy * fy)).astype(np.float32)
    spec = np.fft.rfft2(field.astype(np.float32))
    return np.fft.irfft2(spec * gauss_fft, s=(h, w)).astype(np.float32)


def _generate_regolith_maps(res: int = 1024, seed: int = 2026) -> dict[str, np.ndarray]:
    """Generate seamless, tileable powdery lunar regolith detail textures (1024x1024) for R1Terrain.shader.

    Captures the Apollo 11 close-up powdery & granular surface look:
    - Rounded, soft lumps and soft rounded clods at varied sizes (height blurred/rounded before deriving normals)
    - Zero sharp ridge lines, zero flake edges, zero directional streaks
    - Soft-edged irregular dimples with dust filling the low spots (zero clusters of same-size round holes/dots)
    - Fine isotropic sand/powder grain layer at a smaller scale
    - Soft mottled albedo, slightly lighter on lump tops, darker in the hollows (AO), no hard-edged speckles
    """
    coords = (np.arange(res, dtype=np.float32) + 0.5) / float(res)
    u, v = np.meshgrid(coords, coords)

    # 1. Multi-scale isotropic periodic domain-warp fields (equal U and V frequencies -> zero directional streaks)
    wx1 = _tileable_aniso_noise(res, 3, 3, seed + 11) - 0.5
    wy1 = _tileable_aniso_noise(res, 3, 3, seed + 23) - 0.5
    wx2 = _tileable_aniso_noise(res, 6, 6, seed + 37) - 0.5
    wy2 = _tileable_aniso_noise(res, 6, 6, seed + 53) - 0.5
    wx3 = _tileable_aniso_noise(res, 13, 13, seed + 71) - 0.5
    wy3 = _tileable_aniso_noise(res, 13, 13, seed + 89) - 0.5
    wx4 = _tileable_aniso_noise(res, 26, 26, seed + 103) - 0.5
    wy4 = _tileable_aniso_noise(res, 26, 26, seed + 113) - 0.5

    uw_broad = u + wx1 * 0.12 + wx2 * 0.055
    vw_broad = v + wy1 * 0.12 + wy2 * 0.055
    uw_med = u + wx2 * 0.065 + wx3 * 0.030
    vw_med = v + wy2 * 0.065 + wy3 * 0.030
    uw_fine = u + wx3 * 0.032 + wx4 * 0.015
    vw_fine = v + wy3 * 0.032 + wy4 * 0.015

    # 2. Continuous multi-scale isotropic lumpy regolith bed + continuous rounded clod bumps + soft dust-filled hollows
    octave_specs = [
        (2, 0.250, uw_broad, vw_broad, 12.0, seed + 121),
        (4, 0.200, uw_broad + 0.37, vw_broad + 0.61, 8.0, seed + 149),
        (7, 0.155, uw_med, vw_med, 5.2, seed + 179),
        (12, 0.115, uw_med + 0.19, vw_med + 0.73, 3.6, seed + 211),
        (20, 0.082, uw_fine, vw_fine, 2.5, seed + 241),
        (32, 0.054, uw_fine + 0.53, vw_fine + 0.29, 1.8, seed + 277),
        (50, 0.034, uw_fine + 0.29, vw_fine + 0.83, 1.3, seed + 313),
    ]

    lumpy_bed = np.zeros((res, res), dtype=np.float32)
    dimple_troughs = np.zeros((res, res), dtype=np.float32)
    continuous_clods = np.zeros((res, res), dtype=np.float32)

    for freq, weight, u_w, v_w, blur_sig, s_oct in octave_specs:
        na = _sample_tileable_bilinear(_tileable_aniso_noise(res, freq, freq, s_oct), u_w, v_w)
        nb = _sample_tileable_bilinear(
            _tileable_aniso_noise(res, freq, freq, s_oct + 17), v_w + 0.23, u_w + 0.47
        )
        nc = _sample_tileable_bilinear(
            _tileable_aniso_noise(res, freq + 1, freq + 1, s_oct + 31), u_w + 0.71, v_w + 0.13
        )

        # Smooth isotropic undulating mound field; upward quadratic bias rounds tops and fills low spots with dust
        centered = 0.5 * (na + nb) - 0.5
        soft_mound = centered + 0.30 * (centered * centered) - 0.025

        # Soft irregular hollows where warped isotropic fields dip simultaneously (zero rim, soft blurred edges)
        dip_a = np.clip((0.47 - na) / 0.47, 0.0, 1.0)
        dip_b = np.clip((0.49 - nb) / 0.49, 0.0, 1.0)
        soft_hollow = ((dip_a * dip_b) ** 1.75) * (0.70 + 0.50 * nc)

        # Soft rounded clod bumps at this scale where na and nc peak together
        bump_a = np.clip((na - 0.48) / 0.52, 0.0, 1.0)
        bump_c = np.clip((nc - 0.44) / 0.56, 0.0, 1.0)
        clod_oct = ((bump_a * bump_c) ** 1.80) * (0.65 + 0.55 * nb)

        lumpy_bed += _gaussian_blur_tileable(soft_mound, blur_sig * 0.65) * weight
        dimple_troughs += _gaussian_blur_tileable(soft_hollow, blur_sig * 0.85) * (weight * 1.15)
        continuous_clods += _gaussian_blur_tileable(clod_oct, blur_sig * 0.60) * (weight * 0.95)

    # 3. Scattered soft-edged irregular hollows of mixed size with dust filling the low spots
    rng = np.random.default_rng(seed + 700)
    irregular_dimples = np.zeros((res, res), dtype=np.float32)
    n_warp_local = _tileable_aniso_noise(res, 18, 18, seed + 563)
    n_warp_fine = _tileable_aniso_noise(res, 36, 36, seed + 587)

    for i in range(85):
        cx = float(rng.random())
        cy = float(rng.random())
        if i < 16:
            radius = float(rng.uniform(0.052, 0.095))
        elif i < 48:
            radius = float(rng.uniform(0.024, 0.050))
        else:
            radius = float(rng.uniform(0.011, 0.024))
        depth = float(rng.uniform(0.040, 0.085)) * ((radius / 0.032) ** 0.45)
        # Near-isotropic organic aspect (no rice-grain / pill streaks)
        aspect = float(rng.uniform(1.04, 1.36))
        ang = float(rng.uniform(0.0, np.pi))
        ca = float(np.cos(ang))
        sa = float(np.sin(ang))

        lobe_off_x = float(rng.uniform(0.28, 0.58)) * radius * (1.0 if rng.random() < 0.5 else -1.0)
        lobe_off_y = float(rng.uniform(-0.35, 0.35)) * radius
        lobe_scale = float(rng.uniform(0.68, 0.92))

        rmax = radius * 2.40
        half_px = int(np.ceil(rmax * res)) + 2
        ix_c = int(round(cx * res))
        iy_c = int(round(cy * res))
        ox = np.arange(-half_px, half_px + 1, dtype=np.float32)
        oy = np.arange(-half_px, half_px + 1, dtype=np.float32)
        ix = (ix_c + ox.astype(np.int32)) % res
        iy = (iy_c + oy.astype(np.int32)) % res

        dx_1d = ((ix_c + ox + 0.5) / float(res) - cx + 0.5) % 1.0 - 0.5
        dy_1d = ((iy_c + oy + 0.5) / float(res) - cy + 0.5) % 1.0 - 0.5
        dx = dx_1d[None, :]
        dy = dy_1d[:, None]

        w_loc_a = n_warp_fine[np.ix_(iy, ix)] - 0.5
        w_loc_b = n_warp_local[np.ix_(iy, ix)] - 0.5
        rx = (dx * ca + dy * sa) + (w_loc_a * 0.24 + w_loc_b * 0.38) * radius
        ry = (-dx * sa + dy * ca) * aspect + (w_loc_b * 0.34 - w_loc_a * 0.22) * radius

        d1 = np.sqrt(rx * rx + ry * ry) / radius
        rx2 = rx - lobe_off_x
        ry2 = ry - lobe_off_y
        d2 = np.sqrt(rx2 * rx2 + ry2 * ry2) / (radius * lobe_scale)

        k = 0.48
        h_blend = np.clip(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0)
        d_merged = d2 * (1.0 - h_blend) + d1 * h_blend - k * h_blend * (1.0 - h_blend)

        # Wide Gaussian-like C2 bell with dust-filled flat floor
        t_fall = np.clip(1.0 - d_merged * d_merged, 0.0, 1.0)
        dust_filled_bowl = ((t_fall ** 3.0) / (1.0 + 0.85 * (t_fall ** 3.0))) * 1.85
        bowl = (dust_filled_bowl * depth).astype(np.float32)

        sub = irregular_dimples[np.ix_(iy, ix)]
        irregular_dimples[np.ix_(iy, ix)] = sub + bowl * np.exp(-sub * 5.5)

    irregular_dimples = _gaussian_blur_tileable(irregular_dimples, sigma_px=5.5)

    # 4. Soft rounded clods at varied sizes (organic warped domes, blurred into pillowy soft bumps)
    clod_height_large = np.zeros((res, res), dtype=np.float32)
    clod_height_med = np.zeros((res, res), dtype=np.float32)
    clod_height_small = np.zeros((res, res), dtype=np.float32)
    cluster_field = _tileable_aniso_noise(res, 5, 5, seed + 601)

    for i in range(340):
        cx = float(rng.random())
        cy = float(rng.random())
        c_val = float(cluster_field[int(cy * res) % res, int(cx * res) % res])
        if i >= 60 and rng.random() > (0.32 + 0.68 * c_val):
            continue

        if i < 38:
            pr = float(rng.uniform(0.025, 0.048))
            tier = 0
        elif i < 145:
            pr = float(rng.uniform(0.012, 0.025))
            tier = 1
        else:
            size_u = float(rng.random()) ** 1.50
            pr = 0.0055 + 0.0075 * size_u
            tier = 2

        ph = float(rng.uniform(0.042, 0.092)) * ((pr / 0.018) ** 0.45)
        # Keep aspect close to 1 (1.0..1.32) so clods are rounded soft lumps, never elongated pills/flakes
        aspect = float(rng.uniform(1.02, 1.32))
        ang = float(rng.uniform(0.0, np.pi))
        ca = float(np.cos(ang))
        sa = float(np.sin(ang))

        lobe_x = float(rng.uniform(0.24, 0.52)) * pr * (1.0 if rng.random() < 0.5 else -1.0)
        lobe_y = float(rng.uniform(-0.28, 0.28)) * pr
        lobe_s = float(rng.uniform(0.68, 0.92))

        rmax = pr * 2.25
        half_px = int(np.ceil(rmax * res)) + 2
        ix_c = int(round(cx * res))
        iy_c = int(round(cy * res))
        ox = np.arange(-half_px, half_px + 1, dtype=np.float32)
        oy = np.arange(-half_px, half_px + 1, dtype=np.float32)
        ix = (ix_c + ox.astype(np.int32)) % res
        iy = (iy_c + oy.astype(np.int32)) % res

        dx_1d = ((ix_c + ox + 0.5) / float(res) - cx + 0.5) % 1.0 - 0.5
        dy_1d = ((iy_c + oy + 0.5) / float(res) - cy + 0.5) % 1.0 - 0.5
        dx = dx_1d[None, :]
        dy = dy_1d[:, None]

        w_a = n_warp_local[np.ix_(iy, ix)] - 0.5
        w_b = n_warp_fine[np.ix_(iy, ix)] - 0.5

        rx = (dx * ca + dy * sa) + (w_a * 0.30 + w_b * 0.16) * pr
        ry = (-dx * sa + dy * ca) * aspect + (w_b * 0.24 - w_a * 0.20) * pr

        d1 = np.sqrt(rx * rx + ry * ry) / pr
        rx2 = rx - lobe_x
        ry2 = ry - lobe_y
        d2 = np.sqrt(rx2 * rx2 + ry2 * ry2) / (pr * lobe_s)
        k_c = 0.45
        hb = np.clip(0.5 + 0.5 * (d2 - d1) / k_c, 0.0, 1.0)
        d_clod = d2 * (1.0 - hb) + d1 * hb - k_c * hb * (1.0 - hb)

        # Soft C3 bell dome that feathers smoothly into the surrounding powder bed
        falloff = np.clip(1.0 - d_clod * d_clod, 0.0, 1.0) ** 2.85
        clod = (falloff * ph * (0.90 + 0.20 * (w_b + 0.5))).astype(np.float32)

        target_buf = clod_height_large if tier == 0 else (clod_height_med if tier == 1 else clod_height_small)
        cur_h = target_buf[np.ix_(iy, ix)]
        target_buf[np.ix_(iy, ix)] = cur_h + clod * np.exp(-cur_h * 4.5)

    # Blur each clod size tier proportionally to its scale so every bump is soft and rounded
    clod_height = (
        _gaussian_blur_tileable(clod_height_large, sigma_px=6.5)
        + _gaussian_blur_tileable(clod_height_med, sigma_px=4.0)
        + _gaussian_blur_tileable(clod_height_small, sigma_px=2.4)
        + continuous_clods
    ).astype(np.float32)

    # 5. Dust filling the low spots + Gaussian rounding of the composite lump heightfield BEFORE deriving normals
    raw_lumps = 0.50 + lumpy_bed - dimple_troughs - irregular_dimples + clod_height
    lump_mean = float(np.mean(raw_lumps))
    centered_lumps = raw_lumps - lump_mean
    # Dust settles in hollows: softly compress negative excursions so low spots have powdery dust-filled bottoms
    dust_filled_lumps = np.where(
        centered_lumps < 0.0,
        centered_lumps / (1.0 - 2.6 * centered_lumps),
        centered_lumps,
    ).astype(np.float32)

    # Round/blur the composite heightfield so there are zero sharp ridge lines or flake edges
    lumps_blur_fine = _gaussian_blur_tileable(dust_filled_lumps, sigma_px=2.8)
    lumps_blur_broad = _gaussian_blur_tileable(dust_filled_lumps, sigma_px=6.5)
    rounded_lump_height = (0.54 * lumps_blur_fine + 0.46 * lumps_blur_broad).astype(np.float32)

    # 6. Fine isotropic grain layer (sand/powder) at a smaller scale
    grain_64 = _tileable_aniso_noise(res, 64, 64, seed + 401) - 0.5
    grain_112 = _tileable_aniso_noise(res, 112, 112, seed + 443) - 0.5
    grain_192 = _tileable_aniso_noise(res, 192, 192, seed + 487) - 0.5
    grain_288 = _tileable_aniso_noise(res, 288, 288, seed + 523) - 0.5

    rng_grain = np.random.default_rng(seed + 811)
    micro_sand_raw = rng_grain.normal(0.0, 1.0, (res, res)).astype(np.float32)
    micro_sand_iso = _gaussian_blur_tileable(micro_sand_raw, sigma_px=1.25)
    micro_sand_iso /= max(float(np.std(micro_sand_iso)), 1e-4)

    powder_grain = (
        0.24 * grain_64
        + 0.28 * grain_112
        + 0.24 * grain_192
        + 0.16 * grain_288
        + 0.10 * micro_sand_iso
    ).astype(np.float32)
    powder_grain = _gaussian_blur_tileable(powder_grain, sigma_px=0.90)

    # Normalize rounded height into [0.06, 0.94] centered at 0.50
    h_std = max(float(np.std(rounded_lump_height)), 1e-4)
    lump_norm = (rounded_lump_height - float(np.mean(rounded_lump_height))) / (h_std * 3.05)
    height_01 = np.clip(0.50 + lump_norm + powder_grain * 0.055, 0.06, 0.94).astype(np.float32)

    # 7. Derive tangent-space normal map from the blurred/rounded lump height + fine isotropic powder grain
    dh_lump_x = (
        0.40 * (np.roll(lump_norm, -3, axis=1) - np.roll(lump_norm, 3, axis=1)) / 6.0
        + 0.38 * (np.roll(lump_norm, -7, axis=1) - np.roll(lump_norm, 7, axis=1)) / 14.0
        + 0.22 * (np.roll(lump_norm, -13, axis=1) - np.roll(lump_norm, 13, axis=1)) / 26.0
    )
    dh_lump_y = (
        0.40 * (np.roll(lump_norm, -3, axis=0) - np.roll(lump_norm, 3, axis=0)) / 6.0
        + 0.38 * (np.roll(lump_norm, -7, axis=0) - np.roll(lump_norm, 7, axis=0)) / 14.0
        + 0.22 * (np.roll(lump_norm, -13, axis=0) - np.roll(lump_norm, 13, axis=0)) / 26.0
    )

    dh_grain_x = (np.roll(powder_grain, -1, axis=1) - np.roll(powder_grain, 1, axis=1)) * 0.5
    dh_grain_y = (np.roll(powder_grain, -1, axis=0) - np.roll(powder_grain, 1, axis=0)) * 0.5

    # Soft rounded lump normal (8.2) + fine isotropic sand/powder grain normal (2.2)
    nx = -(dh_lump_x * 8.2 + dh_grain_x * 2.2)
    ny = -(dh_lump_y * 8.2 + dh_grain_y * 2.2)
    nz = np.ones_like(height_01, dtype=np.float32)
    inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    normal_rgb = np.stack(
        [
            np.clip((nx * inv_len) * 0.5 + 0.5, 0.0, 1.0),
            np.clip((ny * inv_len) * 0.5 + 0.5, 0.0, 1.0),
            np.clip((nz * inv_len) * 0.5 + 0.5, 0.0, 1.0),
        ],
        axis=-1,
    )

    # 8. Soft multi-directional horizon & cavity ambient occlusion from the rounded heightfield
    horizon_occ = np.zeros((res, res), dtype=np.float32)
    directions = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1)]
    for step_px, weight in ((6, 0.26), (13, 0.34), (24, 0.24), (42, 0.16)):
        step_occ = np.zeros((res, res), dtype=np.float32)
        inv_dist = 1.0 / float(step_px)
        for dx_i, dy_i in directions:
            diag = 0.7071 if (dx_i != 0 and dy_i != 0) else 1.0
            neighbor_h = np.roll(np.roll(height_01, -dy_i * step_px, axis=0), -dx_i * step_px, axis=1)
            elev_diff = np.maximum(0.0, neighbor_h - height_01) * inv_dist * diag
            step_occ += elev_diff
        horizon_occ += (step_occ / float(len(directions))) * weight

    hollow_soft = _gaussian_blur_tileable(dimple_troughs + irregular_dimples, sigma_px=10.0)
    clod_soft = _gaussian_blur_tileable(clod_height, sigma_px=9.0)
    raw_ao = (
        0.90
        - horizon_occ * 7.8
        - hollow_soft * 0.75
        + np.clip(clod_soft * 0.85, 0.0, 0.045)
    ).astype(np.float32)
    micro_ao = np.clip(_gaussian_blur_tileable(raw_ao, sigma_px=5.5), 0.36, 0.97).astype(np.float32)

    # 9. Albedo: soft mottling, slightly lighter on lump tops, darker in the hollows (AO), no hard-edged speckles
    mottle_broad = _tileable_aniso_noise(res, 3, 3, seed + 641) - 0.5
    mottle_med = _tileable_aniso_noise(res, 7, 7, seed + 673) - 0.5
    mottle_fine = _tileable_aniso_noise(res, 15, 15, seed + 691) - 0.5
    mottle_soft = _gaussian_blur_tileable(
        0.50 * mottle_broad + 0.32 * mottle_med + 0.18 * mottle_fine, sigma_px=8.0
    )

    # Use broadly rounded height and soft AO for albedo so there are zero discrete oval spots or hard-edged speckles
    lump_top_soft = _gaussian_blur_tileable(lump_norm, sigma_px=9.5)
    ao_hollow_soft = _gaussian_blur_tileable(np.clip(0.86 - micro_ao, 0.0, 0.55), sigma_px=7.0)

    lum = (
        0.525
        + mottle_soft * 0.068
        + lump_top_soft * 0.095
        - ao_hollow_soft * 0.135
        + powder_grain * 0.024
    )
    lum = np.clip(lum, 0.43, 0.62).astype(np.float32)
    albedo_rgb = np.stack([lum * 0.995, lum * 1.000, lum * 1.005], axis=-1)
    albedo_rgba = np.concatenate(
        [np.clip(albedo_rgb, 0.0, 1.0), np.ones((res, res, 1), dtype=np.float32)], axis=-1
    )

    # 10. Mask channels:
    # R = Rounded Lump + Powder Height [0..1]
    # G = Soft Horizon & Hollow AO [0..1]
    # B = Soft Rounded Clod / Lump Top Prominence [0..1]
    # A = Matte Powdery Micro-Smoothness [0..1]
    clod_crust_detail = np.clip(
        0.48
        + lump_top_soft * 0.52
        + powder_grain * 0.14
        - hollow_soft * 0.85,
        0.0,
        1.0,
    ).astype(np.float32)
    smoothness = np.clip(
        0.040
        + np.clip(lump_top_soft * 0.02, -0.01, 0.015)
        + np.clip(powder_grain * 0.03, -0.01, 0.012),
        0.020,
        0.070,
    ).astype(np.float32)

    mask_rgba = np.stack([height_01, micro_ao, clod_crust_detail, smoothness], axis=-1)
    return {
        "albedo": albedo_rgba,
        "normal": normal_rgb,
        "mask": mask_rgba,
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
    # Row 7: SagePatch (left third) + SolarCell (middle third) + LampAmber (right third)
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
        s_alb = np.repeat(s["albedo"][::4, :, :], 2, axis=1)[:band_h, :res]
        s_nrm = np.repeat(s["normal"][::4, :, :], 2, axis=1)[:band_h, :res]
        s_msk = np.repeat(s["mask"][::4, :, :], 2, axis=1)[:band_h, :res]
        albedo[y0:y1] = s_alb
        normal[y0:y1] = s_nrm
        mask[y0:y1] = s_msk

    # Row 6: DarkGlass + Copper
    half_w = res // 2
    y0, y1 = 6 * band_h, 7 * band_h
    for x0, x1, name in ((0, half_w, "DarkGlass"), (half_w, res, "Copper")):
        part = by_name[name]
        w = x1 - x0
        albedo[y0:y1, x0:x1] = part["albedo"][::4, :w]
        normal[y0:y1, x0:x1] = part["normal"][::4, :w]
        mask[y0:y1, x0:x1] = part["mask"][::4, :w]
        emission[y0:y1, x0:x1] = part["emission"][::4, :w]

    # Row 7: SagePatch + SolarCell + LampAmber
    third_w = res // 3
    y0, y1 = 7 * band_h, 8 * band_h
    splits = [
        (0, third_w, "SagePatch"),
        (third_w, 2 * third_w, "SolarCell"),
        (2 * third_w, res, "LampAmber"),
    ]
    for x0, x1, name in splits:
        part = by_name[name]
        w = x1 - x0
        albedo[y0:y1, x0:x1] = part["albedo"][::4, :w]
        normal[y0:y1, x0:x1] = part["normal"][::4, :w]
        mask[y0:y1, x0:x1] = part["mask"][::4, :w]
        emission[y0:y1, x0:x1] = part["emission"][::4, :w]

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
        ("CreamEnamel", (0.92, 0.89, 0.82), 0.04, 0.72, 101, "enamel"),
        ("OrangeStripe", (0.85, 0.55, 0.35), 0.04, 0.56, 202, "stripe"),
        ("GreyMetal", (0.48, 0.51, 0.56), 0.32, 0.56, 303, "greymetal"),
        ("BareSteel", (0.74, 0.77, 0.81), 0.88, 0.68, 404, "steel"),
        ("Rubber", (0.17, 0.18, 0.21), 0.02, 0.20, 505, "rubber"),
        ("GoldFoil", (0.94, 0.79, 0.36), 0.94, 0.82, 606, "foil"),
        ("DarkGlass", (0.09, 0.12, 0.16), 0.10, 0.92, 707, "glass"),
        ("Copper", (0.76, 0.53, 0.49), 0.90, 0.72, 808, "copper"),
        ("SagePatch", (0.54, 0.64, 0.55), 0.04, 0.46, 909, "patch"),
        ("LampAmber", (0.98, 0.74, 0.32), 0.05, 0.86, 1010, "lamp"),
        ("SolarCell", (0.20, 0.22, 0.44), 0.08, 0.90, 1111, "solar"),
        ("Brass", (0.86, 0.69, 0.32), 0.92, 0.76, 1212, "brass"),
        ("Basalt", (0.39, 0.40, 0.43), 0.03, 0.22, 1212, "basalt"),
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

    regolith = _generate_regolith_maps(1024, 2026)
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Albedo.png", regolith["albedo"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Normal.png", regolith["normal"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Mask.png", regolith["mask"])

    from generate_signage_atlas import generate_signage_maps
    signage = generate_signage_maps(512)
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Signage_Albedo.png", signage["albedo"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Signage_Normal.png", signage["normal"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Signage_Mask.png", signage["mask"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Signage_Emission.png", signage["emission"])

    print(f"Generated {len(list(OUTPUT_DIR.glob('*.png')))} PNG textures in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()

