# 0.6 — The Mare: Round 2 Concept Look-Development (`r2`)

Round 2 look-development for **The Mare** (`docs/design/0.6-world-regions.md` §3.1), evolving **V1 ("The Lunar Swirl")** into finished R1 stylized-realism concept paintings and resolving every critique from the Round 1 review:

1. **Dark black-blue flood-basalt plain (not white sand)**: The Mare floor sits significantly darker and cooler (`#101420`–`#222B40`) than the `#787A80` neutral grey home basin, so the bleached **pale lunar swirl (`#C8CEDC`–`#D8DFEC`)** reads unmistakably as the single bright sinuous ribbon winding across the plain.
2. **Non-emissive bronze-black metallic Ilmenite (`FeTiO3`)**: No glowing crystals, no cyan/orange emissive seams. Ilmenite reads as stepped, blocky tabular seams of dark metallic-black mineral (`#181A22`) embedded in fractured basalt, distinguished by its **warm bronze-black (`#5C4938` / `#6E5849`) and titanium-gunmetal (`#5A6475`) specular sheen** under grazing light.
3. **Pure black star sky & zero cyan neon**: The sky is deep space black (`#05070E`–`#0A0F1A`) with crisp pinpoint stars and a huge low Earth — never purple. All cyan/green neon rings and wireframe overlays are removed.
4. **Finished concept paintings (no call-out labels)**: Three unobstructed `1920x1080` paintings (`1_hook.png`, `2_day.png`, `3_night.png`) plus an unlabelled contact sheet (`sheet.png`).

---

## 1. Deliverables

| File | Shot | Look Phase | Description |
| :--- | :--- | :--- | :--- |
| [`1_hook.png`](./1_hook.png) | **1 — Hook Vista from West Rim at Dusk** | Dusk rim graze → Earthlit plain | Looking west from the grey highland rim (`#6C6D72` / `#3E4048`) down into the vast sunken black-blue basalt plain (`#101420`–`#1F273B`). The pale lunar swirl winds across the dark floor as a giant sinuous ribbon toward the far Mare Dish's single warm amber blink (`#FFB547`), under a huge low Earth and black star sky. |
| [`2_day.png`](./2_day.png) | **2 — Inside the Mare by Day** | **"C Low sun"** (`~5°` sun elevation) | Warm low grazing sunlight (`#FFF1D6`) and long pitch-dark shadows (`#0A0D16`) across black-blue basalt swells, faint pale ejecta rays, a stepped bronze-black metallic Ilmenite outcrop (right, non-glowing), the stalled 1980s ILC survey cart derelict at the rille mouth (left), and Rover 07 small for scale on the swirl path. |
| [`3_night.png`](./3_night.png) | **3 — Inside the Mare at Night** | **"A Moon grey"** (Earthlit night) | Exact same camera and layout as `2_day.png` under cool neutral grey-blue earthshine (`#8E98A8`). **Rover 07's warm amber headlamp cone (`#FFB547`) is the ONLY warm light in the scene besides the far dish's single warm amber blink (`#FFB547`).** |
| [`sheet.png`](./sheet.png) | **Clean Contact Sheet** | All 3 views | Side-by-side / stacked visual comparison of `1_hook.png`, `2_day.png`, and `3_night.png` with zero call-out labels on the artwork. |

---

## 2. Palette Hex Specification

Anchored to `docs/VISION.md` §Palette, `Assets/_Project/Scripts/Art/Palette.cs`, and `R1MaterialRecipes.cs`:

### 2.1 Terrain & Geology (The Mare vs. Home Basin)

| Surface / Role | Hex | sRGB `(R, G, B)` | Material / Value Notes |
| :--- | :---: | :---: | :--- |
| **Home Basin Regolith (Reference)** | `#787A80` | `(120, 122, 128)` | Neutral mid-grey highland dust (`R1_Regolith`); seen on the foreground west rim in `1_hook.png`. |
| **Mare Basalt — Deep Trough / Shadow** | `#101420` | `(16, 20, 32)` | Sunken flood-basalt troughs and rille floors; deep indigo-black value anchor. |
| **Mare Basalt — Mid Plain (`R1_MareBasalt`)** | `#161C2C` | `(22, 28, 44)` | Primary black-blue flood-basalt floor (`Smoothness 0.14`, `Metallic 0.04`). Clearly darker and bluer-black than `#787A80`. |
| **Mare Basalt — Grazing Swell Crest** | `#222B40` | `(34, 43, 64)` | Gentle long-wavelength wrinkle-ridge crests catching low grazing light. |
| **Faint Crater Ejecta Rays** | `#4A5264` | `(74, 82, 100)` | Subtle secondary radial dust streaks (`15–25%` blend over `#161C2C`). |
| **Pale Lunar Swirl — Day ("C Low sun")** | `#D0D6E4` | `(208, 214, 228)` | High-albedo magnetically shielded regolith ribbon (`#C8CEDC` edge feather to `#D8DFEC` core, `Smoothness 0.08`). |
| **Pale Lunar Swirl — Night ("A Moon grey")** | `#8E96A8` | `(142, 150, 168)` | The same high-albedo swirl ribbon under cool earthshine — still reads 3–4 stops brighter than the `#141926` night basalt floor. |

### 2.2 Ilmenite (`FeTiO3`) — Non-Emissive Metallic Mineral

| Surface / Role | Hex | sRGB `(R, G, B)` | Material / Value Notes |
| :--- | :---: | :---: | :--- |
| **Ilmenite Base Ore (`R1_Ilmenite`)** | `#181A22` | `(24, 26, 34)` | Dense iron-titanium oxide seam embedded in fractured dark basalt (`Metallic 0.82`, `Smoothness 0.48`, **`Emission #000000`**). |
| **Ilmenite Bronze-Black Specular Glint (Day)** | `#6E5849` | `(110, 88, 73)` | Warm bronze-black specular response on stepped tabular facets under "C Low sun" (`#5C4938` mid-bronze transition). |
| **Ilmenite Titanium-Steel Edge Glint (Night)** | `#5A6475` | `(90, 100, 117)` | Cool gunmetal-steel specular edge catch under "A Moon grey" earthshine or 07's headlamp cone. |

### 2.3 Sky, Lighting Looks & Accents

| Element / Lighting Phase | Hex | sRGB `(R, G, B)` | Lighting & Readability Role |
| :--- | :---: | :---: | :--- |
| **Space Sky — Zenith to Horizon** | `#05070E` → `#0A0F1A` | `(5, 7, 14)` → `(10, 15, 26)` | Pure black-indigo vacuum sky with pinpoint stars (`#EBF0FA`). **Never purple.** |
| **Look "C Low sun" — Day Key Light** | `#FFF1D6` | `(255, 241, 214)` | Warm low grazing sun (`~5°` elevation), casting crisp long shadows (`#0A0D16`) and warm dusk rim glints (`#B8986A`). |
| **Look "A Moon grey" — Night Earthshine Key** | `#8E98A8` | `(142, 152, 168)` | Cool, near-neutral grey-blue earthlight (`#141926` night mare floor, `#242C3D` earthlit swells). |
| **Warm Amber Lamp & Mare Dish Blink** | `#FFB547` | `(255, 181, 71)` | `Palette.WarmAmber` — 07's headlamp cone, 07's CRT phosphor eyes, and the single distant warm amber beacon blink on the Mare Dish. |
| **Earth Anchor (Horizon)** | `#3FA7D6` / `#7BD389` | `(63, 167, 214)` / `(123, 211, 137)` | `Palette.EarthOcean` & `Palette.EarthLand` with `#F2F5FA` cloud swirls, hanging huge and low. |
| **Rover 07 & Survey Cart Derelict** | `#F4E6C8` / `#FF8A5B` / `#86AE9C` | `(244, 230, 200)` / `(255, 138, 91)` / `(134, 174, 156)` | 07's `CreamBody`, `OrangeStripe`, and `GreenAccent`. The stalled 1980s survey cart uses sun-bleached/weathered variants (`#B8AFA2` cream, `#C68F76` faded orange, zero active lights). |

---

## 3. What Reads as "Go There" (Without Glowing Crystals or Neon)

The Mare achieves clear spatial navigation and resource/landmark readability purely through **value contrast, silhouette, specular response, and temperature discipline**:

1. **Macro Navigation — The Pale Lunar Swirl Highway (`#D0D6E4` on `#161C2C`)**:
   - Because the Mare floor is dropped to a deep black-blue basalt (`~8–12%` reflectance) while the magnetically shielded swirl dust retains high albedo (`~75–85%` reflectance), the sinuous swirl reads from the West Rim (`1_hook.png`) and at ground level (`2_day.png`, `3_night.png`) as an unmistakable bright S-curve across the dark sea. Players naturally follow the ribbon out into the plain and back toward the basin rim without needing UI waypoints.
2. **Horizon Anchor — One Warm Amber Blink on the Mare Dish (`#FFB547`)**:
   - Across an entirely cool, lifeless black-blue plain under a black starry sky, **one tiny warm amber beacon blink (`#FFB547`)** atop the tall vertical lattice silhouette of the Mare Dish pulls the eye from kilometers away. At night (`3_night.png`), it is the **only** warm point in the world besides 07's own headlamp cone, creating an immediate emotional tether between rover and goal.
3. **Midground Point-of-Interest — The Stalled Survey Cart at the Rille Mouth**:
   - Instead of a glowing cyan ring or wireframe beacon, the rille mouth reads through **topographic break + human-made geometric contrast**: a sharp shadow lip where the smooth basalt plain drops into a collapsed sinuous channel, paired with the tilted, boxy silhouette and sun-bleached cream/terracotta hull (`#B8AFA2` / `#C68F76`) of the 1980s ILC survey cart against dark `#161C2C` basalt.
4. **Harvestable Resource — Ilmenite Outcrops Read by Shape & Specular Glint (Never Emission)**:
   - Surrounding basalt boulders and swells are rounded, matte (`Smoothness 0.14`), and non-metallic (`Metallic 0.04`).
   - **Ilmenite (`FeTiO3`) outcrops** break all three properties without emitting a single photon:
     - **Silhouette**: Stepped, blocky, tabular hex/columnar fracture seams (`45°–90°` stepped cleavage planes) jutting out of low basalt swells.
     - **Surface**: High metallic (`0.82`) and mid-high smoothness (`0.48`) with a warm bronze-black specular tint (`#6E5849` in low sun, `#5A6475` in earthlight).
     - **In motion**: As Rover 07 drives past (or sweeps its `#FFB547` headlamp cone across the plain at night), the flat stepped facets of an Ilmenite seam catch a crisp, unmistakable **bronze-metallic specular flash** against the matte black-blue dust.

---

## 4. How the Lunar Swirl Can Be Built in Unity URP

The sinuous Pale Lunar Swirl can be implemented cleanly in our existing Unity 6 URP pipeline (`Assets/_Project/Shaders/` + `R1MaterialRecipes.cs`) with **zero per-frame CPU cost** and **no heavy overdraw**:

### Approach A (Recommended): World-Space Spline Mask + Dual-Octave Curl Noise in the Mare Terrain Shader
Best for seamless integration across the large flood-basalt mesh (`MareTerrain`):

1. **Baked Swirl Mask Texture (`R8` or `RG8`, `1024×1024`, ~1 MB)**:
   - Author the Mare's primary S-curve swirl path (and secondary rille/ejecta masks) as a `UnityEngine.Splines.SplineContainer` in the scene.
   - An Editor bake step (`MareSwirlBaker.cs`) rasterizes distance-to-spline into an `RG8` world-space mask texture covering the Mare bounds:
     - **Channel `R` (Swirl Core & Loops)**: Signed/normalized distance to the sinuous swirl spline, modulated by a 2-octave procedural curl/flow noise along the spline tangent so the ribbon splits into authentic Reiner Gamma-style double ribbons and wispy magnetic loop tails.
     - **Channel `G` (Faint Ejecta Rays)**: Radial streak mask for secondary `#4A5264` crater-ejecta rays.
2. **Shader Graph / HLSL Blending (`R1_MareSurface.shader`)**:
   - Sample `MareSwirlMask` using world-space `XZ` coordinates (`uv = (worldPos.xz - _MareBoundsMin.xy) / _MareBoundsSize.xy`).
   - Perturb the lookup UV slightly with world-space tiling Perlin/Simplex noise (`scale 0.04`, `strength 3.5m`) to keep close-up edges powdery and organic rather than pixelated.
   - Compute `swirlFactor = smoothstep(_SwirlEdgeOuter, _SwirlEdgeInner, mask.r)` and blend surface properties:
     - `Albedo`: `lerp(_BasaltColor /* #161C2C */, _SwirlColor /* #D0D6E4 */, swirlFactor)` (with secondary `lerp` for ejecta rays `mask.g` toward `#4A5264`).
     - `Smoothness`: `lerp(0.14, 0.08, swirlFactor)` (bleached swirl dust is slightly more powdery/matte than compacted basalt).
     - `Normal`: Blend from coarse basalt macro-undulation normals to fine windless dust micro-grain normals inside the swirl.
3. **Why this works well for R1**:
   - **Single pass, zero decal overdraw**: The entire Mare floor renders in one opaque terrain pass.
   - **Automatic Day/Night ("C Low sun" / "A Moon grey") response**: Because the swirl is pure **albedo + roughness** (`#D0D6E4`, zero emission), it naturally gleams under the low `5°` warm directional sun (`2_day.png`) and automatically drops to cool silver-grey (`#8E96A8`) under ambient earthshine (`3_night.png`), while catching a bright warm hotspot inside 07's `#FFB547` spotlight cone.

### Approach B (Alternative / Supplemental): URP Decal Projector / Spline Ribbon Mesh for Local Hero Bends
- Enable `URP Decal Renderer Feature` (DBuffer or Screen-Space Decals affecting BaseColor and Normal/Smoothness only) or lay a conformant spline-extruded strip mesh (`2–5 cm` above terrain with vertex-alpha feathered borders) for hand-placed hero swirl curls around the **Mare Dish** and **Rille Mouth**.
- Useful for art-directing exact hero framing in camera vistas (`1_hook.png`) without rebaking the global world-space mask.

### Ilmenite Shader Recipe (`R1_Ilmenite` in `R1MaterialRecipes.cs`)
- Standard `Universal Render Pipeline/Lit` material:
  - `_BaseColor`: `#181A22` (dark iron-titanium black) with a subtle bronze tint in the specular/albedo highlight (`#4A3B32` on weathered bevels).
  - `_Metallic`: `0.82`
  - `_Smoothness`: `0.48` (with facet roughness variation `0.35–0.60` via triplanar detail mask so flat cleavage faces glint sharply while rough fracture steps stay dark).
  - `_EmissionColor`: `#000000` (Emission **disabled**).
