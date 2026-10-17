#!/usr/bin/env python3
"""Deterministic readout & beacon texture generator for R1 Opening v2 story props.

Generates:
  - T_R1_LanderReserve_Albedo.png / T_R1_LanderReserve_Emission.png (256x128, [0,1] UV)
  - T_R1_AirlockReadout_Albedo.png / T_R1_AirlockReadout_Emission.png (256x128, [0,1] UV)
  - T_R1_PeakBeaconRed_Albedo.png / T_R1_PeakBeaconRed_Emission.png (128x128)
"""

from __future__ import annotations

from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

import generate_signage_atlas as gsa


def _build_readout_pair(line1: str, line2: str, w: int = 256, h: int = 128) -> tuple[Image.Image, Image.Image]:
    """Render a 2x supersampled 1980s amber CRT/LED two-line readout screen covering [0, 1] UVs."""
    hi_w, hi_h = w * 2, h * 2
    albedo = Image.new("RGBA", (hi_w, hi_h), color=(*gsa.STATUS_BEZEL_BG, 255))
    emission = Image.new("RGB", (hi_w, hi_h), color=(0, 0, 0))
    d_alb = ImageDraw.Draw(albedo)
    d_em = ImageDraw.Draw(emission)

    d_alb.rounded_rectangle(
        [10, 10, hi_w - 11, hi_h - 11],
        radius=14,
        fill=(*gsa.STATUS_SCREEN_BG, 255),
        outline=(*gsa.STATUS_AMBER_DIM, 255),
        width=5,
    )
    d_em.rounded_rectangle(
        [10, 10, hi_w - 11, hi_h - 11],
        radius=14,
        fill=(14, 8, 2),
        outline=(92, 56, 18),
        width=5,
    )

    for sy in range(18, hi_h - 16, 8):
        d_alb.line([(16, sy), (hi_w - 16, sy)], fill=(*gsa.STATUS_SCANLINE, 255), width=2)

    # Line 1 (Primary bright amber)
    gsa._draw_stencil_text(
        d_alb,
        line1,
        cx=hi_w * 0.5,
        cy=hi_h * 0.34,
        char_w=15.0,
        char_h=44.0,
        stroke_w=5.8,
        spacing=4.0,
        color=gsa.STATUS_AMBER_BRIGHT,
        bg_color=gsa.STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    gsa._draw_stencil_text(
        d_alb,
        line1,
        cx=hi_w * 0.5,
        cy=hi_h * 0.34,
        char_w=15.0,
        char_h=44.0,
        stroke_w=2.4,
        spacing=4.0,
        color=gsa.STATUS_AMBER_CORE,
        bg_color=gsa.STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    gsa._draw_stencil_text(
        d_em,
        line1,
        cx=hi_w * 0.5,
        cy=hi_h * 0.34,
        char_w=15.0,
        char_h=44.0,
        stroke_w=6.0,
        spacing=4.0,
        color=(236, 168, 62),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )
    gsa._draw_stencil_text(
        d_em,
        line1,
        cx=hi_w * 0.5,
        cy=hi_h * 0.34,
        char_w=15.0,
        char_h=44.0,
        stroke_w=2.6,
        spacing=4.0,
        color=(255, 236, 172),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )

    # Horizontal divider rule
    mid_y = hi_h * 0.55
    gsa._draw_thick_seg(d_alb, 28.0, mid_y, hi_w - 28.0, mid_y, 3.0, gsa.STATUS_AMBER_RULE)
    gsa._draw_thick_seg(d_em, 28.0, mid_y, hi_w - 28.0, mid_y, 3.0, (78, 48, 16))

    # Line 2 (Secondary dimmer amber)
    gsa._draw_stencil_text(
        d_alb,
        line2,
        cx=hi_w * 0.5,
        cy=hi_h * 0.74,
        char_w=14.0,
        char_h=34.0,
        stroke_w=4.6,
        spacing=4.0,
        color=gsa.STATUS_AMBER_DIM,
        bg_color=gsa.STATUS_SCREEN_BG,
        stencil_bridges=True,
    )
    gsa._draw_stencil_text(
        d_em,
        line2,
        cx=hi_w * 0.5,
        cy=hi_h * 0.74,
        char_w=14.0,
        char_h=34.0,
        stroke_w=4.6,
        spacing=4.0,
        color=(156, 96, 30),
        bg_color=(14, 8, 2),
        stencil_bridges=True,
    )

    alb_out = albedo.resize((w, h), resample=Image.Resampling.LANCZOS)
    em_out = emission.resize((w, h), resample=Image.Resampling.LANCZOS)
    return alb_out, em_out


def _build_peak_beacon_red_pair(res: int = 128) -> tuple[Image.Image, Image.Image]:
    """Generate crimson-red Fresnel lens albedo & emission textures for the R1 PeakBeacon Lamp."""
    y, x = np.mgrid[0:res, 0:res].astype(np.float32)
    u = (x + 0.5) / res - 0.5
    v = (y + 0.5) / res - 0.5
    r = np.hypot(u, v) / 0.5

    # Fresnel concentric ribs + hot central core
    ribs = 0.88 + 0.12 * np.cos(r * 18.0 * np.pi)
    core = np.exp(-((r * 2.1) ** 2))

    alb_r = np.clip((0.78 + 0.22 * core) * ribs, 0.0, 1.0)
    alb_g = np.clip((0.14 + 0.32 * core) * ribs, 0.0, 1.0)
    alb_b = np.clip((0.12 + 0.24 * core) * ribs, 0.0, 1.0)
    alb_a = np.ones_like(alb_r)
    alb_u8 = np.clip(np.rint(np.stack([alb_r, alb_g, alb_b, alb_a], axis=-1) * 255.0), 0, 255).astype(np.uint8)

    # Emission map encodes crimson-red body (luma ~ 0.72, below HotCoreThreshold 0.85) and hot center core (luma > 0.88)
    em_r = np.clip(0.74 * ribs + 0.26 * core, 0.0, 1.0)
    em_g = np.clip(0.14 * ribs + 0.34 * core, 0.0, 1.0)
    em_b = np.clip(0.11 * ribs + 0.24 * core, 0.0, 1.0)
    em_u8 = np.clip(np.rint(np.stack([em_r, em_g, em_b], axis=-1) * 255.0), 0, 255).astype(np.uint8)

    return Image.fromarray(alb_u8, mode="RGBA"), Image.fromarray(em_u8, mode="RGB")


def main() -> None:
    repo_root = Path(__file__).resolve().parents[3]
    out_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures"
    out_dir.mkdir(parents=True, exist_ok=True)

    lr_alb, lr_em = _build_readout_pair("LANDER RESERVE · 35%", "EXTERNAL PAD REQUIRED")
    lr_alb.save(out_dir / "T_R1_LanderReserve_Albedo.png", compress_level=9)
    lr_em.save(out_dir / "T_R1_LanderReserve_Emission.png", compress_level=9)

    ar_alb, ar_em = _build_readout_pair("HABITAT SEAL · LOCKED", "SUIT 01-04 · OFFLINE")
    ar_alb.save(out_dir / "T_R1_AirlockReadout_Albedo.png", compress_level=9)
    ar_em.save(out_dir / "T_R1_AirlockReadout_Emission.png", compress_level=9)

    pb_alb, pb_em = _build_peak_beacon_red_pair(128)
    pb_alb.save(out_dir / "T_R1_PeakBeaconRed_Albedo.png", compress_level=9)
    pb_em.save(out_dir / "T_R1_PeakBeaconRed_Emission.png", compress_level=9)
    print(f"Saved story prop textures to {out_dir}")


if __name__ == "__main__":
    main()
