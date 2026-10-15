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
        # Fractured lunar basalt stone: matte volcanic grain, small vesicles, and subtle mineral flecks
        coarse = _fbm(res, 4, seed + 19)
        vesicles = np.clip((ultra_fine - 0.70) * 3.4, 0.0, 1.0)
        flecks = np.clip((fine - 0.68) * 3.1, 0.0, 1.0)
        cleave = 1.0 - np.abs(mottle * 2.0 - 1.0)
        height = (
            0.50
            + (coarse - 0.5) * 0.045
            + (cleave - 0.5) * 0.032
            + (fine - 0.5) * 0.020
            - vesicles * 0.038
            + flecks * 0.014
        )
        albedo = np.empty((res, res, 3), dtype=np.float32)
        albedo[..., 0] = base_rgb[0]
        albedo[..., 1] = base_rgb[1]
        albedo[..., 2] = base_rgb[2]
        albedo *= (
            0.93
            + 0.11 * coarse[..., None]
            + 0.05 * (cleave[..., None] - 0.5)
            + 0.06 * flecks[..., None]
            - 0.09 * vesicles[..., None]
        )
        metallic = np.full((res, res), metallic_val, dtype=np.float32) + flecks * 0.04
        smoothness = (
            np.full((res, res), smoothness_val, dtype=np.float32)
            + (coarse - 0.5) * 0.06
            + flecks * 0.08
            - vesicles * 0.07
        )
        micro_wear = np.clip(0.48 + (cleave - 0.5) * 0.45 + flecks * 0.28, 0.0, 1.0)
        micro_cavity = np.clip(0.56 - vesicles * 0.42 - (0.5 - coarse) * 0.30, 0.0, 1.0)
        normal_strength = 5.8

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


def _generate_regolith_maps(res: int = 512, seed: int = 2026) -> dict[str, np.ndarray]:
    """Generate seamless, tileable lunar regolith detail textures (512x512) for R1Terrain.shader."""
    coords = (np.arange(res, dtype=np.float32) + 0.5) / float(res)
    u, v = np.meshgrid(coords, coords)

    broad = _fbm(res, 4, seed)
    medium = _fbm(res, 12, seed + 17)
    fine = _tileable_noise(res, 32, seed + 33)
    ultra_fine = _tileable_noise(res, 64, seed + 71)
    grain = _tileable_noise(res, 96, seed + 113)

    # Subtle wind/impact regolith micro-ripples (tileable periodic phase)
    phase = u * 6.0 * np.pi * 2.0 + v * 2.0 * np.pi * 2.0 + (broad - 0.5) * 2.2
    ripple = 0.5 + 0.5 * np.sin(phase)

    crater_bowl = np.zeros((res, res), dtype=np.float32)
    crater_rim = np.zeros((res, res), dtype=np.float32)
    pebbles = np.zeros((res, res), dtype=np.float32)

    rng = np.random.default_rng(seed + 500)
    # 85 seamless wrap-around micro-craters across [0, 1]^2
    for _ in range(85):
        cx = float(rng.random())
        cy = float(rng.random())
        radius = float(rng.uniform(0.014, 0.058))
        depth = float(rng.uniform(0.35, 0.85)) * (radius / 0.045) ** 0.45
        dx = (u - cx + 0.5) % 1.0 - 0.5
        dy = (v - cy + 0.5) % 1.0 - 0.5
        rn = np.sqrt(dx * dx + dy * dy) / radius

        bowl = np.clip(1.0 - rn * rn, 0.0, 1.0) ** 1.35 * depth
        rim = np.exp(-(((rn - 1.06) / 0.26) ** 2)) * depth * 0.42
        crater_bowl = np.maximum(crater_bowl, bowl)
        crater_rim = np.maximum(crater_rim, rim)

    # 140 tiny half-buried basalt clasts / regolith pebbles
    for _ in range(140):
        px = float(rng.random())
        py = float(rng.random())
        pr = float(rng.uniform(0.006, 0.018))
        ph = float(rng.uniform(0.25, 0.65))
        dx = (u - px + 0.5) % 1.0 - 0.5
        dy = (v - py + 0.5) % 1.0 - 0.5
        rn = np.sqrt(dx * dx + dy * dy) / pr
        dome = (np.clip(1.0 - rn * rn, 0.0, 1.0) ** 1.5) * ph
        pebbles = np.maximum(pebbles, dome)

    height = (
        0.50
        + (broad - 0.5) * 0.045
        + (medium - 0.5) * 0.032
        + (ripple - 0.5) * 0.014
        + (fine - 0.5) * 0.020
        + (ultra_fine - 0.5) * 0.012
        + (grain - 0.5) * 0.008
        + crater_rim * 0.042
        - crater_bowl * 0.068
        + pebbles * 0.036
    )

    normal_rgb = _height_to_normal(height, normal_strength=6.8)

    # Near-neutral grey regolith albedo (no purple wash); subtle brighten on rims/clasts, darken in bowls
    lum = (
        0.76
        + (broad - 0.5) * 0.08
        + (medium - 0.5) * 0.06
        + (fine - 0.5) * 0.04
        + (grain - 0.5) * 0.03
        + crater_rim * 0.06
        + pebbles * 0.04
        - crater_bowl * 0.09
    )
    lum = np.clip(lum, 0.52, 0.94)
    albedo_rgb = np.stack([lum * 0.995, lum * 1.000, lum * 1.005], axis=-1)
    albedo_rgba = np.concatenate(
        [np.clip(albedo_rgb, 0.0, 1.0), np.ones((res, res, 1), dtype=np.float32)], axis=-1
    )

    # Mask channels for R1Terrain:
    # R = pore/grain variation, G = clast/rim ridge mask, B = micro-cavity AO, A = micro-smoothness
    pore_var = np.clip(0.50 + (medium - 0.5) * 0.35 + (fine - 0.5) * 0.25 + crater_rim * 0.20 - crater_bowl * 0.25, 0.0, 1.0)
    clast_mask = np.clip(pebbles * 0.85 + crater_rim * 0.55 + (ultra_fine - 0.5) * 0.25, 0.0, 1.0)
    micro_ao = np.clip(0.84 - crater_bowl * 0.42 - np.clip(0.5 - medium, 0.0, 0.5) * 0.28 - pebbles * 0.08, 0.25, 1.0)
    smoothness = np.clip(0.09 + pebbles * 0.11 + (grain - 0.5) * 0.04 - crater_bowl * 0.03, 0.04, 0.26)

    mask_rgba = np.stack([pore_var, clast_mask, micro_ao, smoothness], axis=-1)
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

    regolith = _generate_regolith_maps(512, 2026)
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Albedo.png", regolith["albedo"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Normal.png", regolith["normal"])
    _save_rgba_or_rgb(OUTPUT_DIR / "T_R1_Regolith_Mask.png", regolith["mask"])

    print(f"Generated {len(list(OUTPUT_DIR.glob('*.png')))} PNG textures in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
