# 0.6 Lunar Phenomena VFX — Concept & Preview Kit (`docs/art/concepts/0.6-phenomena/`)

Visual-only lunar surface phenomena kit for **0.6 ("A Living Moon", `docs/design/0.6-living-moon.md` §2)**. Every effect is authored strictly within the Art domain (`Assets/_Project/Shaders/Art/`, `Assets/_Project/Scripts/Art/`, `Assets/_Project/Config/`) with a dedicated tuning `ScriptableObject`, a thin zero-GC playable-preview component, procedural R1/Lofi meshes, and URP shaders.

![0.6 Lunar Phenomena Capture Sheet](sheet.png)

---

## 1. Deliverables & Asset Map

| # | Phenomenon | Shader | Materials (`Generated/Art/Phenomena/`) | Preview Prefab (`Generated/Art/Phenomena/`) | Playable Preview Component | Tuning Asset (`Assets/_Project/Config/`) |
|---|---|---|---|---|---|---|
| **1** | **Levitating dust glow** | `MoonProject/Art/LofiDustGlow` (`LofiDustGlow.shader`) | `M_DustGlowBand.mat`, `M_DustGlowMotes.mat` | `FX_LevitatingDustGlow.prefab` | `LevitatingDustGlowPreview` | `LevitatingDustGlowTuning.asset` |
| **2** | **Micrometeorite flash & cooling impact glass** | `MoonProject/Art/LofiImpactGlass` (`LofiImpactGlass.shader`) | `M_ImpactGlass.mat`, `M_ImpactFlash.mat` | `FX_MicrometeoriteFlash.prefab` | `MicrometeoriteFlashPreview` | `MicrometeoriteFlashTuning.asset` |
| **3** | **Gentle moonquake dust slide** | `MoonProject/Art/LofiDustSlide` (`LofiDustSlide.shader`) | `M_DustSlideSheet.mat`, `M_DustSlideSettle.mat`, `M_DustSlideGrain.mat` | `FX_MoonquakeDustSlide.prefab` | `MoonquakeDustSlidePreview` | `MoonquakeDustSlideTuning.asset` |
| **4** | **Earthrise light tint preset** | `MoonProject/Art/LofiDustGlow` + directional/ambient rig | `M_EarthriseDisc.mat` | `FX_EarthriseLightPreset.prefab` | `EarthriseLightPresetPreview` | `EarthriseLightPresetTuning.asset` |

All generated materials, meshes, and prefabs are built idempotently by `PhenomenaAssetBuilder.Build()` (`[MoonBuilder("Art/Phenomena", 108)]`).

---

## 2. Phenomenon Breakdown & Vacuum Physics Rules

### 2.1 Levitating Dust Glow (Row 1 of `sheet.png`)
- **Visual Design:** At the lunar terminator (dawn/dusk transition), solar UV photoemission charges fine regolith grains, lifting a soft forward-scattered horizon haze band (`28 m × 3.2 m`) along the ridge alongside `7` thin vertical columns of slowly rising crystalline motes (`0.22 m/s` upward drift).
- **Look A (Night) & Look C (Day):**
  - **Day C ("Low sun"):** Warm golden-silver forward scatter (`#EACC9E`, alpha `0.46`) catching grazing `11°` sunlight.
  - **Night A ("Moon grey"):** Cool earthshine silver-blue scatter (`#99BDF5`, alpha `0.40`).
- **R1 & Bloom Discipline:** Both the horizon band (`_BandMaxIntensity = 0.74`) and rising mote columns (`_MoteMaxIntensity = 0.96`) are clamped below `_BloomCeiling = 1.10` (`< Palette.BloomThreshold = 1.25`). Mote particles use the faceted octahedral `Mesh_FacetedMoteShard` mesh and diamond-anisotropic shader mask (**zero repeated round-dot patterns**).
- **Capture Sheet Frames (Row 1, left to right):**
  1. `t = 1.8 s` (Day C): Eased SmoothStep fade-in onset behind Rover 07.
  2. `t = 10.0 s` (Day C): Full terminator horizon haze band + rising mote columns under grazing low sun.
  3. `t = 12.0 s` (Night A): Full cool earthshine terminator glow behind Rover 07 with its warm eye fill.
  4. `t = 23.2 s` (Night A): Eased SmoothStep fade-out as levitated motes settle back toward the ridge.

### 2.2 Micrometeorite Flash & 10-Minute Cooling Impact Glass (Row 2 of `sheet.png`)
- **Visual Design:** A brief distant white starlight flash (`0.06 s` eased rise, `0.48 s` eased decay) accompanied by `24` ballistic ejecta shards following clean parabolic vacuum trajectories ($g = 1.62\text{ m/s}^2$, zero air drag), revealing a `1.5 m` diameter fresh crater (`Mesh_ImpactCraterAndGlass`) with central fused impact glass shards.
- **10-Minute (`600 s`) Thermal Cool-Down Curve:**
  - `t = 0 s`: White-hot core (`#FFF0CC`, intensity `1.85`, softly crossing `Palette.BloomThreshold`) with molten amber rim (`#FF7524`).
  - `t = 180 s` (3 min): Mid cool-down deep cherry-amber ember (`#9E290D`) on the central glass facets while the overturned crater rim catches grazing sunlight.
  - `t >= 600 s` (10 min+): Thermal heat reaches `0.0`, leaving dark vitreous impactite glass (`#1A212E`) with a **permanent non-zero glint floor** (`_PermanentGlintFloor = 0.24`, `#C7E6FF`) on its sharp facet ridges so the impact glass stays discoverable and collectable forever.
- **Capture Sheet Frames (Row 2, left to right):**
  1. `t = 0.07 s` (Night A): Peak white starlight flash burst + vacuum ballistic ejecta spray beside 07.
  2. `t = 18.0 s` (Night A): Flash gone; fresh crater with incandescent white-amber molten impact glass.
  3. `t = 180.0 s` (Day C, 3 min): Mid cool-down warm ember glow in grazing Day C sunlight.
  4. `t = 600.0 s` (Night A, 10 min): Fully cooled vitreous glass holding its faint permanent starlight glint.

### 2.3 Gentle Moonquake Dust Slide (Row 3 of `sheet.png`)
- **Visual Design:** Dislodged regolith sheets slowly (`1.65 m/s`) down a `25°` Mare crater slope (`8.5 m` long, `4.6 m` wide) in anisotropic mineral streams (`M_DustSlideSheet`), accompanied by `36` discrete angular grain clusters hopping in low parabolic arcs under lunar gravity ($1.62\text{ m/s}^2$) and a ground-skimming toe settle apron (`<= 0.36 m` high).
- **Vacuum Honesty:** Zero turbulent billowing smoke, zero upward buoyancy, and zero circular particle sprites. Once the slide finishes, the toe settle apron drops quadratically to the ground under lunar gravity.
- **Capture Sheet Frames (Row 3, left to right):**
  1. `t = 0.85 s` (Day C): Eased tremor onset loosening the top regolith layer beside 07.
  2. `t = 5.0 s` (Day C): Active down-slope sheeting streams + ballistic grain arcs + low toe settle veil.
  3. `t = 5.5 s` (Night A): Active sheeting in cool earthlit slate tones (`#57637A`).
  4. `t = 10.6 s` (Day C): Slope streams subsiding as the low toe settle veil drops cleanly onto the basalt floor.

### 2.4 Earthrise Light Tint Preset (Row 4 of `sheet.png`)
- **Visual Design:** Preview-only lighting and colour-grade preset for the Earthrise stillness beat ("Earth climbing over a ridge, the whole frame warm-blue").
- **Warm-Blue Duality:**
  - **Key Light:** Luminous cerulean-azure Earthshine (`#75B0FA`, intensity `0.94`, rising from `4.5°` to `14.5°` elevation).
  - **Counter-Rim Fill:** Warm golden-ivory crest bounce (`#F5BD85`, intensity `0.42`) kissing 07's chassis and ridge silhouettes.
  - **Trilight Ambient & Horizon Haze:** Cobalt sky (`#304A7A`), warm-slate equator (`#384261`), deep indigo ground (`#141C2E`), and warm-blue horizon glow (`_MoonHazeColor = #5C8FE0`).
- **Capture Sheet Frames (Row 4, left to right):**
  1. `t = 0.0 s` (`weight = 0.0`): Baseline Night A ("Moon grey") before Earth clears the ridge.
  2. `t = 2.5 s` (`weight = 0.50`): Earth climbing over the distant Mare ridge, warm-blue horizon haze waking.
  3. `t = 11.0 s` (`weight = 1.00`): Peak Earthrise stillness beat — whole frame bathed in serene warm-blue grade.
  4. `t = 23.0 s` (`weight = 0.50`): Eased SmoothStep blend-out returning toward baseline night.

---

## 3. Reproducing Captures

```bash
python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.Concept.PhenomenaConceptCapture.Capture
```
Individual cells are written to `Logs/captures/phenomena/*.png` and the composite 4×4 time-progression sheet is written to `docs/art/concepts/0.6-phenomena/sheet.png`.
