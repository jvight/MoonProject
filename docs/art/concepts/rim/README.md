# Crater-Rim Slope Material & Wall-Base Talus Scatter Kit (`box/agy-rimmat`)

## 1. Apollo & LRO Oblique Photographic Reference Analysis

To replace the artificial heightmap-wall look (near-vertical slabs with repeated horizontal elevation stripes and stretched planar UVs), the crater-rim slope material and wall-base talus kit are grounded in oblique orbital and surface photography of three benchmark lunar craters (described below; no external images copied):

### Tycho Crater Rim (LRO NAC Oblique Views of Inner Terraced Walls & Scarps)
- **Angle-of-Repose Threshold (~38–42°):** In LRO Narrow Angle Camera (NAC) oblique views across Tycho's terraced inner rim, cohesive grey regolith blankets every slump terrace, rounded shoulder, and moderate slope (`25–35°`). Coherent darker bedrock and impact-melt crags crop out **only on the steepest headwall scarps (`> 43°`, peaking on `65–85°` faces)** where regolith slides off downslope.
- **Discontinuous 3D Outcrops, Not Horizontal Stripes:** Even on steep headwalls, exposed rock forms irregular, 3D-warped crags and diagonal fracture blocks separated by regolith-filled chutes—never continuous horizontal contour rings or uniform layer-cake bands.
- **Wall-Base Talus Aprons:** Below every steep scarp, mass wasting deposits an apron of sub-rounded to sub-angular boulders and cobble clusters (`0.5–4 m`) embedded in fine regolith along the break-in-slope.

### Copernicus Crater Rim (Apollo 12 / Apollo 17 Orbital Obliques & LRO NAC)
- **Softly Rounded Regolith-Mantled Shoulders (`25–35°`):** Apollo orbital oblique photographs of the Copernicus massif and terraced slump blocks show broad, softly rounded highland shoulders coated in a continuous, matte, mid-grey regolith mantle (`DustMid` / `DustLight` value range).
- **Large-Scale 3D Albedo Drift (`50–180 m`):** Across the rim, surface tone shifts gradually over `50–180 m` and `15–35 m` patches due to downslope regolith creep and optical maturity variations, breaking up texture repetition without high-frequency speckle or circular pit patterns.

### Shackleton Crater Rim (LRO Oblique South-Polar Low-Sun Views)
- **Grazing Polar Sunlight (`Day C` Low Sun):** Under low grazing illumination (`8–15°` elevation), rounded `25–35°` regolith shoulders exhibit smooth, velvety terminator roll-off rather than harsh binary contour steps, while steep `70–85°` rocky scarps and wall-base boulder aprons cast long, crisp directional shadows across the basin floor.
- **Earthshine & Shadow Readability (`Night A` Earthlight):** In cool blue Earthshine and deep shadow, the matte regolith mantle holds soft trilight form while darker fresh-rock exposures on steep scarps and rounded `0.5–4 m` talus boulders remain distinct in silhouette and under rover headlamps.

---

## 2. Slope Material Architecture & Asset Paths for World

| Asset / Shader | Path | Purpose |
| :--- | :--- | :--- |
| **Rim Slope Material (Generated)** | `Assets/_Project/Generated/Art/R1/Materials/R1_RimSlope.mat` | Standalone triplanar crater-rim slope material built by `R1MaterialLibrary.BuildRimMaterials()` (`R1MaterialLibrary.LoadRimSlopeMaterial()`). Assignable to any dedicated rim/slump mesh. |
| **Terrain & Rim Shader** | `Assets/_Project/Shaders/World/R1Terrain.shader` (`MoonProject/World/R1Terrain`) | Upgraded world terrain shader (used by both `Assets/_Project/Generated/World/M_Ground.mat` and `R1_RimSlope.mat`). Automatically applies the triplanar rim material to all terrain slopes and crater rims. |
| **Talus Kit Material (Generated)** | `Assets/_Project/Generated/Art/R1/Materials/R1_RimTalus.mat` | World-space triplanar `MoonProject/R1/Lit` material (`R1MaterialLibrary.LoadRimTalusMaterial()`) shared by `RimTalus_00..07`. |

### Key Technical Changes in `R1Terrain.shader` & `R1_RimSlope.mat`
1. **World-Space Triplanar Projection (Zero Vertical UV Stretching):**
   - Slopes and near-vertical faces sample regolith detail (`SampleTriplanarRegolithOnSlope` over `_BaseMap`, `_BumpMap`, `_MaskMap`) and exposed basalt rock (`_RockMap`, `_RockBumpMap`, `_RockMaskMap`) across all three world planes (`YZ`, `XZ`, `XY`) weighted by tightened geometry normal weights `pow(abs(geomNormalWS), 4.0)`.
   - World-space tangent frame switches reference axis when `abs(geomNormalWS.x) > 0.82`, preventing tangent singularities on east/west-facing vertical cliffs.
2. **3D Isotropic Macro Variation (Zero Horizontal Stripe Banding):**
   - Removed the legacy elevation-periodic `yBeddingBase` / `AccumulateBrokenRockLedges2x2` horizontal strata rings and neutralized `TerrainPainter`'s 22 m `position.y` vertex-color stripes on slopes.
   - Replaced with 3D quintic value noise (`RimNoise3D`) and 3-octave orthogonal-rotated 3D FBM (`RimFbm3D`) at **large (`50–180 m`)**, **medium (`15–35 m`)**, and **fine (`4–9 m`)** world-space scales.
3. **Slope-Angle Calibration (`25–35°` Mantle vs Steepest-Parts Fresh Rock):**
   - **`25–35°` slopes (`geomNormalWS.y ∈ [0.819, 0.906]`):** `steepestSlopeMask` is `0.0`, keeping slump shoulders and talus aprons in a cohesive **regolith-mantled grey** (`mantledGreyMod`) with gentle 3D macro drift.
   - **Steepest faces (`> 43°`, `geomNormalWS.y < 0.73`, full strength on `65–85°` headwalls):** `steepestSlopeMask` blends in **subtle darker fresh-rock exposures** (`0.66x–0.76x` of mantled grey) modulated by 3D `outcropPatch` lobes and 3D isotropic fracture normals.
4. **Zero Repeated Round-Hole / Dot Patterns:**
   - All slope detail, 3D noise octaves, and talus meshes are completely free of circular pit stamps or polka-dot hole patterns.

---

## 3. Talus / Scree Scatter Kit (`RimTalus_00` .. `RimTalus_07`) for World

Eight weathered, rounded lunar debris pieces (`0.56 m` to `3.86 m`) are provided for World to scatter along crater-rim wall bases, slump toes, and talus aprons. Every piece is authored in `tools/art/blender/rocks.py` (`--only-rim-talus`) with:
- **Softly weathered, dust-mantled R1 silhouette** (`smooth_angle_deg = 52°`, rounded bevels, zero circular pits `pits_u = []`).
- **Buried base skirt (`18–22%` below `z = 0`)** so pieces can be placed on `0–35°` slopes and wall bases without floating edges.
- **`isReadable = true`** via `R1ModelPostprocessor` so World's `ScatterBuilder` / `ClodMesh` / instanced placement can read mesh buffers directly or instantiate the generated prefabs via `WorldPaths.R1RockPrefab("RimTalus_00")` .. `"RimTalus_07"`.

| ID | FBX Source Path | Generated Prefab Path | Footprint (`W × D × H` m) | Tris | Role in Wall-Base Scatter |
| :--- | :--- | :--- | :--- | ---: | :--- |
| `RimTalus_00` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_00.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_00.prefab` | `0.56 × 0.46 × 0.34 m` | 140 | Small rounded regolith-mantled cobble clast (outer apron toe) |
| `RimTalus_01` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_01.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_01.prefab` | `0.86 × 0.68 × 0.48 m` | 140 | Low weathered scree wedge (outer-to-mid apron) |
| `RimTalus_02` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_02.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_02.prefab` | `1.22 × 0.94 × 0.62 m` | 192 | Gently tilted rounded talus slab (mid apron) |
| `RimTalus_03` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_03.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_03.prefab` | `1.64 × 1.32 × 0.92 m` | 320 | Pillowed hummock boulder with companion clast (mid apron) |
| `RimTalus_04` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_04.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_04.prefab` | `2.12 × 1.56 × 1.05 m` | 252 | Elongated weathered slump block (inner-to-mid apron) |
| `RimTalus_05` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_05.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_05.prefab` | `2.68 × 2.08 × 1.24 m` | 468 | 3-lobe rounded scree cluster (wall-base apron) |
| `RimTalus_06` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_06.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_06.prefab` | `3.28 × 2.46 × 1.48 m` | 408 | Broad weathered rim-shoulder block (cliff-foot anchor) |
| `RimTalus_07` | `Assets/_Project/Art/R1/Models/Rocks/RimTalus_07.fbx` | `Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_07.prefab` | `3.86 × 2.92 × 1.76 m` | 560 | Large wall-base talus anchor boulder with scree skirt |

---

## 4. Proof Sheet (`docs/art/concepts/rim/sheet.png`)

`docs/art/concepts/rim/sheet.png` (`2568 × 1448` 2×2 comparison grid) is rendered deterministically by `MoonProject.Art.Editor.R1RimMaterialCapture.BuildAndCapture`:
- **Top-Left (`BEFORE — Day C Low Sun`):** Legacy horizontal stripe-banded slope material (`_LegacyBandedRim = 1`) on the `25–35°` test slope (left half of frame) and near-vertical `78–82°` crater-rim face (right half of frame).
- **Top-Right (`AFTER — Day C Low Sun`):** New triplanar `R1_RimSlope.mat` + `RimTalus_00..07` scatter kit along the wall base under grazing Day C low sun, showing smooth regolith-mantled grey on the `25–35°` slope, subtle darker fresh-rock exposures only on the steepest cliff face, and zero horizontal stripe banding.
- **Bottom-Left (`BEFORE — Night A Earthlight`):** Legacy horizontal stripe-banded material under Night A cool blue Earthlight + Rover `07` headlamp.
- **Bottom-Right (`AFTER — Night A Earthlight`):** New triplanar `R1_RimSlope.mat` + `RimTalus_00..07` scatter kit under Night A cool blue Earthlight + Rover `07` headlamp.

To regenerate the proof sheet from the project root:
```powershell
python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1RimMaterialCapture.BuildAndCapture
```
