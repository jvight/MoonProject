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


def _ensure_ice_core_glyphs() -> None:
    """Register glyph strokes needed for 'CORE 41 · H₂O · I.V. 10/88' without altering existing readouts."""
    gsa.GLYPH_STROKES.setdefault(
        "2",
        [
            (0.14, 0.24, 0.30, 0.08),
            (0.30, 0.08, 0.70, 0.08),
            (0.70, 0.08, 0.86, 0.22),
            (0.86, 0.22, 0.86, 0.38),
            (0.86, 0.38, 0.14, 0.92),
            (0.14, 0.92, 0.88, 0.92),
        ],
    )
    gsa.GLYPH_STROKES.setdefault(
        "₂",
        [
            (0.18, 0.54, 0.32, 0.42),
            (0.32, 0.42, 0.68, 0.42),
            (0.68, 0.42, 0.82, 0.52),
            (0.82, 0.52, 0.82, 0.64),
            (0.82, 0.64, 0.18, 0.98),
            (0.18, 0.98, 0.84, 0.98),
        ],
    )
    gsa.GLYPH_STROKES.setdefault(
        "/",
        [
            (0.20, 0.94, 0.80, 0.06),
        ],
    )


def _build_ice_core_label(w: int = 512, h: int = 128) -> Image.Image:
    """Render a 2x supersampled 1980s stencilled cryo specimen label reading 'CORE 41 · H₂O · I.V. 10/88'."""
    _ensure_ice_core_glyphs()
    hi_w, hi_h = w * 2, h * 2
    albedo = Image.new("RGBA", (hi_w, hi_h), color=(*gsa.CREAM_BG, 255))
    draw = ImageDraw.Draw(albedo)

    prev_mid_dot = gsa.GLYPH_STROKES["·"]
    prev_period = gsa.GLYPH_STROKES["."]
    try:
        gsa.GLYPH_STROKES["·"] = [(0.34, 0.50, 0.66, 0.50)]
        gsa.GLYPH_STROKES["."] = [(0.36, 0.88, 0.64, 0.88)]

        # Outer dark anodized border frame + inner cream specimen plate
        draw.rounded_rectangle(
            [6, 6, hi_w - 7, hi_h - 7],
            radius=14,
            fill=(*gsa.CREAM_TILE, 255),
            outline=(*gsa.STENCIL_DARK, 255),
            width=6,
        )

        # Top warm orange identification header band
        header_bottom = int(hi_h * 0.29)
        draw.rectangle([12, 12, hi_w - 13, header_bottom], fill=(*gsa.ORANGE_BAND, 255))
        draw.line([(12, header_bottom), (hi_w - 13, header_bottom)], fill=(*gsa.STENCIL_DARK, 255), width=4)

        # Sub-header stencil text inside orange band
        gsa._draw_stencil_text(
            draw,
            "LUMEN HYDROLOGY · CRYO SPECIMEN",
            cx=hi_w * 0.5,
            cy=(12 + header_bottom) * 0.5,
            char_w=17.0,
            char_h=28.0,
            stroke_w=4.2,
            spacing=5.0,
            color=gsa.CREAM_TILE,
            bg_color=gsa.ORANGE_BAND,
            stencil_bridges=True,
        )

        # Corner mounting rivet marks
        for rx in (28, hi_w - 29):
            for ry in (header_bottom + 20, hi_h - 24):
                draw.ellipse([rx - 7, ry - 7, rx + 7, ry + 7], fill=(154, 160, 170, 255), outline=(*gsa.STENCIL_DARK, 255), width=2)

        # Main stencilled label text: CORE 41 · H₂O · I.V. 10/88
        gsa._draw_stencil_text(
            draw,
            "CORE 41 · H₂O · I.V. 10/88",
            cx=hi_w * 0.5,
            cy=hi_h * 0.65,
            char_w=27.0,
            char_h=76.0,
            stroke_w=8.4,
            spacing=7.5,
            color=gsa.STENCIL_DARK,
            bg_color=gsa.CREAM_TILE,
            stencil_bridges=True,
        )
    finally:
        gsa.GLYPH_STROKES["·"] = prev_mid_dot
        gsa.GLYPH_STROKES["."] = prev_period

    return albedo.resize((w, h), resample=Image.Resampling.LANCZOS)


def _build_ice_core_frost_albedo(res: int = 128) -> Image.Image:
    """Generate a two-zone 128x128 albedo map for IceCoreTube's frost end (U < 0.5) and internal frosted grey core (U >= 0.5)."""
    y, x = np.mgrid[0:res, 0:res].astype(np.float32)
    u = (x + 0.5) / res
    v = (y + 0.5) / res

    # Deterministic multi-frequency crystalline interference pattern (Voronoi/facet-like rime grain)
    c1 = np.sin(u * 38.0 * np.pi + np.cos(v * 29.0 * np.pi) * 1.4)
    c2 = np.cos(v * 44.0 * np.pi - np.sin(u * 31.0 * np.pi) * 1.6)
    c3 = np.sin((u + v) * 64.0 * np.pi) * np.cos((u - v) * 52.0 * np.pi)
    crystal = np.clip(0.5 + 0.24 * c1 + 0.16 * c2 + 0.14 * c3, 0.0, 1.0)
    sparkle = np.power(crystal, 3.0)

    # Zone A (U < 0.5): Crystalline white-blue rime frost on one end of the cryo sleeve
    frost_r = 0.82 + 0.14 * crystal + 0.04 * sparkle
    frost_g = 0.90 + 0.08 * crystal + 0.02 * sparkle
    frost_b = 0.96 + 0.04 * crystal

    # Zone B (U >= 0.5): Frosted grey lunar core sample with stratigraphic dust/ice bands along U/V
    strata = (
        0.50
        + 0.22 * np.sin(u * 26.0 * np.pi)
        + 0.15 * np.cos(u * 58.0 * np.pi + 0.7)
        + 0.13 * np.sin(u * 110.0 * np.pi + v * 4.0 * np.pi)
    )
    strata = np.clip(strata, 0.0, 1.0)
    # Subtle hydrated blue-ice mineral horizon near U = 0.74..0.80
    h2o_band = np.exp(-(((u - 0.76) / 0.045) ** 2))
    core_r = 0.52 + 0.20 * strata + 0.10 * crystal - 0.06 * h2o_band + 0.08 * sparkle
    core_g = 0.58 + 0.20 * strata + 0.10 * crystal + 0.04 * h2o_band + 0.08 * sparkle
    core_b = 0.65 + 0.20 * strata + 0.10 * crystal + 0.12 * h2o_band + 0.07 * sparkle

    is_core = (u >= 0.5).astype(np.float32)
    r = np.clip(frost_r * (1.0 - is_core) + core_r * is_core, 0.0, 1.0)
    g = np.clip(frost_g * (1.0 - is_core) + core_g * is_core, 0.0, 1.0)
    b = np.clip(frost_b * (1.0 - is_core) + core_b * is_core, 0.0, 1.0)
    a = np.ones_like(r)

    rgba_u8 = np.clip(np.rint(np.stack([r, g, b, a], axis=-1) * 255.0), 0, 255).astype(np.uint8)
    return Image.fromarray(rgba_u8, mode="RGBA")


def _build_cargo_crate_label(w: int = 512, h: int = 128) -> Image.Image:
    """Render a 2x supersampled 1980s stencilled lander cargo crate plaque reading 'LUMEN CARGO' + part number."""
    _ensure_ice_core_glyphs()
    hi_w, hi_h = w * 2, h * 2
    albedo = Image.new("RGBA", (hi_w, hi_h), color=(*gsa.CREAM_BG, 255))
    draw = ImageDraw.Draw(albedo)

    prev_mid_dot = gsa.GLYPH_STROKES["·"]
    prev_period = gsa.GLYPH_STROKES["."]
    try:
        gsa.GLYPH_STROKES["·"] = [(0.34, 0.50, 0.66, 0.50)]
        gsa.GLYPH_STROKES["."] = [(0.36, 0.88, 0.64, 0.88)]

        # Outer dark anodized border frame + inner cream enamel plate
        draw.rounded_rectangle(
            [6, 6, hi_w - 7, hi_h - 7],
            radius=14,
            fill=(*gsa.CREAM_TILE, 255),
            outline=(*gsa.STENCIL_DARK, 255),
            width=6,
        )

        # Top coral-orange identification header band with part number
        header_bottom = int(hi_h * 0.32)
        draw.rectangle([12, 12, hi_w - 13, header_bottom], fill=(*gsa.ORANGE_BAND, 255))
        draw.line([(12, header_bottom), (hi_w - 13, header_bottom)], fill=(*gsa.STENCIL_DARK, 255), width=4)

        gsa._draw_stencil_text(
            draw,
            "PART NO. LC-04 · PAD ASSY 01/02",
            cx=hi_w * 0.5,
            cy=(12 + header_bottom) * 0.5,
            char_w=17.5,
            char_h=30.0,
            stroke_w=4.6,
            spacing=5.5,
            color=gsa.CREAM_TILE,
            bg_color=gsa.ORANGE_BAND,
            stencil_bridges=True,
        )

        # Corner mounting rivet marks
        for rx in (28, hi_w - 29):
            for ry in (header_bottom + 22, hi_h - 24):
                draw.ellipse(
                    [rx - 7, ry - 7, rx + 7, ry + 7],
                    fill=(154, 160, 170, 255),
                    outline=(*gsa.STENCIL_DARK, 255),
                    width=2,
                )

        # Main bold stencilled label text: LUMEN CARGO
        gsa._draw_stencil_text(
            draw,
            "LUMEN CARGO",
            cx=hi_w * 0.5,
            cy=hi_h * 0.67,
            char_w=52.0,
            char_h=92.0,
            stroke_w=12.5,
            spacing=14.0,
            color=gsa.STENCIL_DARK,
            bg_color=gsa.CREAM_TILE,
            stencil_bridges=True,
        )
    finally:
        gsa.GLYPH_STROKES["·"] = prev_mid_dot
        gsa.GLYPH_STROKES["."] = prev_period

    return albedo.resize((w, h), resample=Image.Resampling.LANCZOS)


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

    ic_alb = _build_ice_core_label(512, 128)
    ic_alb.save(out_dir / "T_R1_IceCoreLabel_Albedo.png", compress_level=9)

    ic_frost = _build_ice_core_frost_albedo(128)
    ic_frost.save(out_dir / "T_R1_IceCoreFrost_Albedo.png", compress_level=9)

    cc_alb = _build_cargo_crate_label(512, 128)
    cc_alb.save(out_dir / "T_R1_CargoCrateLabel_Albedo.png", compress_level=9)
    print(f"Saved story prop textures to {out_dir}")


if __name__ == "__main__":
    main()

