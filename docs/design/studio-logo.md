# Studio Identity Specification — "Outsight Worlds" (`OUTSIGHT WORLDS` · Finalized Direction C)

> **Status:** `[APPROVED — ROUND 2 FINAL]` — Studio = **Outsight Worlds**; Logo = **Direction C** (*The Leaving Line*)  
> **Round 2 Final Gallery & Exports:** `Logs/studio/final/index.html` (`Logs/studio/final/`)  
> **Round 1 Concepts Gallery (`a` · `b` · `c` · `d`):** `Logs/studio/index.html`  
> **BootGate 4.0 s Timing Spec:** `Logs/studio/final/BOOT_SPLASH_SPEC.md`, `Logs/studio/final/boot_splash_timing.json`  
> **Deterministic Generators:** `tools/art/studio/build_studio_final.py` (Round 2 Final), `tools/art/studio/build_studio_logos.py` (Round 1)  
> **Related Specs:** `docs/VISION.md`, `ProjectSettings/ProjectSettings.asset` (`m_SplashScreenBackgroundColor: #0B0E2A`), `Assets/_Project/Scripts/UI/Motion/BootGate.cs`

---

## 1. Round 2 Owner Decision & Finalized Studio Identity

- **Studio Name:** **Outsight Worlds**
  - **Primary Stacked Wordmark:** **`OUTSIGHT`** large (`BeVietnamPro-SemiBold`, `56 px`, `+17.5 px` tracking) + **`WORLDS`** small (`BeVietnamPro-Medium`, `18.5 px`, `+23.5 px` tracking) centered `54 px` below.
  - **One-Line Lockups:**
    - **Horizontal One-Line Lockup (`oneline_*`):** Mark on the left + **`OUTSIGHT WORLDS`** on a single horizontal line (`42 px`, `+10.5 px` tracking).
    - **Stacked Mark + One-Line Wordmark (`oneline_stacked_*`):** Mark centered above a single-line **`OUTSIGHT WORLDS`** baseline.
- **Selected Direction:** **Direction `C` (*The Leaving Line*)** — a single unbroken geometric ribbon that traces a circle, inflects smoothly across its center with $C^1$ tangent continuity, and steps straight out through an opening in its own rim (`-36°`).
- **Owner Rule (Strict):** **Zero clusters of identical dots or holes (trypophobia).**

### 1.1 Fixing the Cigarette Risk: Two Tip Treatments (`tip_variants_comparison.png`)

In Round 1, a uniform-width white stroke ending in a perpendicular gap and rectangular amber block carried a risk of reading as a filter cigarette. Round 2 replaces that segmented block entirely and evaluates both owner-requested refinements side-by-side (`Logs/studio/final/tip_variants_comparison.png`):

| Variant | Files (`Logs/studio/final/`) | Geometry & Visual Read | Selection Status |
| :--- | :--- | :--- | :--- |
| **Variant `C1` — *Gentle Taper + Tiny 4-Point Glint*** | `variant_glint_on_black.png`, `variant_glint_on_navy.png`, `variant_glint_on_offwhite.png` (+ all primary exports) | As the ribbon steps out of the ring, it narrows gracefully along a convex curve (`26 px → 11.6 px`) into a smooth rounded tip—eliminating any parallel cylinder shaft—and culminates in a poised, solid **4-point star glint** (`#E2A856`, `r = 19.5 px`, zero center pinhole) at the stroke's end (`u = 229`). | **★ PRIMARY PICK** — Zero cigarette silhouette; crisp at `32×32` favicon scale; converts cleanly to `1-colour` monochrome; anchors the `4.0 s` `BootGate` glint beat. |
| **Variant `C2` — *Comet-Trail Fine Taper*** | `variant_taper_on_black.png`, `variant_taper_on_navy.png`, `variant_taper_on_offwhite.png` | The departing stroke follows a continuous parabolic comet-trail taper (`26 px → 2.9 px` fine apex at `u = 244`), warming smoothly from warm ivory into ochre-amber (`#E2A856`) along the outer tail with no detached element. | **Alternate** — Preserved side-by-side in `Logs/studio/final/` for comparison. |

---

## 2. Final Surface Palette & Export Matrix (`Logs/studio/final/`)

| Surface / Role | Hex | RGBA | Export Files (`Logs/studio/final/`) |
| :--- | :--- | :--- | :--- |
| **Gallery Black** | `#0B0C10` | `(11, 12, 16, 255)` | `lockup_on_black.png` (`on_black.png`), `mark_on_black.png`, `oneline_on_black.png`, `oneline_stacked_on_black.png`, `one_colour_on_black.png` |
| **BootGate Calm Navy** | `#0B0E2A` | `(11, 14, 42, 255)` | `lockup_on_navy.png` (`on_navy.png`), `mark_on_navy.png`, `oneline_on_navy.png`, `oneline_stacked_on_navy.png`, `one_colour_on_navy.png` |
| **Archival Off-White** | `#F4EFE6` | `(244, 239, 230, 255)` | `lockup_on_offwhite.png` (`on_offwhite.png`), `mark_on_offwhite.png`, `oneline_on_offwhite.png`, `oneline_stacked_on_offwhite.png`, `one_colour_on_offwhite.png` |
| **Transparent `2048×2048` px** | `RGBA(0,0,0,0)` | `6144×6144` → `2048×2048` Lanczos | `transparent_2048.png` (`lockup_transparent_2048.png`), `mark_transparent_2048.png`, `oneline_transparent_2048.png`, `one_colour_transparent_2048.png` |
| **Favicons (`64×64` & `32×32`)** | `#0B0C10` / `#0B0E2A` / Transparent | Mark-only at `1.82×` optical fill | `favicon_64.png`, `favicon_32.png`, `favicon_64_navy.png`, `favicon_32_navy.png`, `favicon_64_transparent.png`, `favicon_32_transparent.png` |

---

## 3. Final `4.0 s` `BootGate` Splash Storyboard, Sequence & Timing Spec

Designed to run inside the existing `MoonProject.UI.BootGate` dark screen (`#0B0E2A`, `Assets/_Project/Scripts/UI/Motion/BootGate.cs`) with `minSeconds = 4.0f` and `maxSeconds = 5.0f`:

- **4-Frame Storyboard (`1024×1024`):** `Logs/studio/final/splash_1.png`–`splash_4.png` + 2×2 composite sheet `Logs/studio/final/splash_storyboard.png`
- **16-Frame Sequence (`0.00 s – 4.00 s`):** `Logs/studio/final/frames/frame_00.png`–`frame_15.png` + 4×4 contact strip `Logs/studio/final/splash_sequence_strip.png`
- **Separated UI Layers (`1024×1024`):** `Logs/studio/final/layers/boot_layer_ring.png`, `boot_layer_stroke.png`, `boot_layer_glint.png`, `boot_layer_wordmark.png`
- **Machine-Readable Timing Curve Data:** `Logs/studio/final/boot_splash_timing.json` & `Logs/studio/final/BOOT_SPLASH_SPEC.md`

| Beat | Window | Keyframe Curves (`SmoothStep`) | Visual Description |
| :--- | :--- | :--- | :--- |
| **1 · Stroke Draws Out of Ring** | `0.00 s – 1.45 s` | `ring_alpha`: `0.10..0.95 s` (`0 → 1`)<br>`stroke_progress`: `0.20..1.45 s` (`0 → 1`) | On `#0B0E2A` `BootGate` dark, the cradle ring eases in as the ribbon traces counter-clockwise, inflects across the center, and steps out through the upper-right opening. |
| **2 · Departure & 4-Point Glint** | `1.30 s – 1.95 s` | `glint_alpha`: `1.30..1.72 s` (`0 → 1`)<br>`glint_scale`: `0.35 → 1.12` (`1.30..1.62 s`) → `1.00` (`1.95 s`) | As the stroke reaches its rounded outward tip, the warm `#E2A856` 4-point glint blooms cleanly at its end and settles into poise. |
| **3 · Wordmark Fades & Hold** | `1.50 s – 3.12 s` | `word_alpha`: `1.50..2.20 s` (`0 → 1`)<br>`word_dy`: `+8 px → 0 px`<br>**Hold:** `2.20..3.12 s` (`0.92 s` stillness) | **`OUTSIGHT`** + **`WORLDS`** fades in below the mark; the complete lockup holds in calm stillness for nearly a full second. |
| **4 · Dissolve into BootGate Dark** | `3.12 s – 4.00 s` | `main_alpha`: `1 - SmoothStep(3.12, 3.84, t)`<br>`glint_alpha`: `1 - SmoothStep(3.48, 4.00, t)` | Ring, stroke, and wordmark dissolve smoothly into `#0B0E2A`, while the tiny 4-point outward glint lingers last before `BootGate` lifts into the title screen. |

---

## 4. Round 1 Exploration Reference (`Logs/studio/<a|b|c|d>/`)

All four Round 1 concept directions (`a` *The Outer Aperture*, `b` *The Open Horizon*, `c` *The Leaving Line*, `d` *Two Perspectives*) remain archived in `Logs/studio/a..d/` and browsable in `Logs/studio/index.html`.
