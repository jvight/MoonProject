# 0.6 "The Long Mare" (*Biển Dài*) — Visual Look-Dev & Concept Specification (`[PROPOSAL]`)

> **Scope:** Concept look-development for 0.6 **The Long Mare** (`docs/design/0.6-world-regions.md` §0, §3.1, §5). Preparation only — no code or `Assets/` changes.
> **Canonical References:**
> - Creative pillars, solitude & palette: `docs/VISION.md:39-60`, `docs/VISION.md:221-248`, `Assets/_Project/Scripts/Art/Palette.cs:76-115`
> - Region design, lures, derelicts & 0.6 slice plan: `docs/design/0.6-world-regions.md:18-34`, `133-167`, `235-308`
> - Polar sun ("C Low sun") & earthshine ("A Moon grey") light model: `docs/design/0.6-living-moon.md:76-114`, `Assets/_Project/Scripts/Core/Events/WorldEvents.cs:18-24`
> - R1 stylized-realism pipeline & material slots: `docs/art/r1-pipeline.md:1-196`, `Assets/_Project/Scripts/Art/Editor/R1MaterialRecipes.cs:31-180`
> - Vacuum silhouette law & true scale: `docs/design/vacuum-shapes.md:11-101`, `docs/design/scale.md:119-139`

---

## 1. Deliverables & Image Manifest (`docs/art/concepts/0.6-mare/`)

All 9 individual look-dev frames are rendered at **`1920×1080` PNG** using headless Blender (`D:/Blender/blender.exe`) with `tools/art/blender/r1kit.py`, real imported R1 FBX assets (`RoverModel.fbx`, `SolarWing.fbx`, `Kit_BatterySpine.fbx`, `Kit_LampBar.fbx`, `SSR3_Chassis.fbx`, `SSR3_Tracks.fbx`, `SSR3_Topper_01.fbx`, `AntennaMast.fbx`, `RelayHut.fbx`, `RelayContactPad.fbx`, `Crate_M_Open.fbx`, `Rock_00..13.fbx`), and `T_R1_Earth_Albedo.png`, followed by a deterministic atmospheric & bloom paint-over pass:

| File | Resolution | Direction & View Description |
| :--- | :---: | :--- |
| [`sheet.png`](sheet.png) | `2560×1660` | **Master 3-Column Comparison Sheet** (`V1` \| `V2` \| `V3` across Row 1 Hook Vista, Row 2 Mare Day, Row 3 Mare Night, with hex swatch bars and feature callouts) |
| [`v1_1_hook.png`](v1_1_hook.png) | `1920×1080` | **V1 · View 1 (The HOOK Vista — Dusk / "C Low Sun"):** West rim rille pass mouth looking across the indigo-slate basalt plain, classic *Reiner Gamma* S-ribbon swirl, far amber dish blink, and huge low Earth |
| [`v1_2_day.png`](v1_2_day.png) | `1920×1080` | **V1 · View 2 (Inside the Mare — Day / "C Low Sun"):** Radial ejecta rays, stepped hexagonal basalt-Ilmenite prisms, open-truss mare dish site, stalled `SSR-3` tracked survey cart at the rille mouth with `Cell Pack III` fragments, and 07 (`1.59 m`) for scale |
| [`v1_3_night.png`](v1_3_night.png) | `1920×1080` | **V1 · View 3 (Inside the Mare — Night / "A Moon Grey"):** Cool near-neutral Earthshine over View 2, 07's warm headlamp cone, luminous Earthlit swirl highway, glinting Ilmenite `FeTiO3` mineral seams, and the warm dish lighthouse |
| [`v2_1_hook.png`](v2_1_hook.png) | `1920×1080` | **V2 · View 1 (The HOOK Vista — Dusk / "C Low Sun"):** Terraced tectonic wrinkle ridges (`dorsa`) and 3 braided magnetic-dipole swirl loops converging on the raised dish plinth under a huge low Earth |
| [`v2_2_day.png`](v2_2_day.png) | `1920×1080` | **V2 · View 2 (Inside the Mare — Day / "C Low Sun"):** Diagonal ejecta bands, `55°` tilted strike-ridge Ilmenite ore-fin dikes with Ines's 1988 coral-orange survey collar, plinth relay dish, stalled articulated cart, and 07 |
| [`v2_3_night.png`](v2_3_night.png) | `1920×1080` | **V2 · View 3 (Inside the Mare — Night / "A Moon Grey"):** Cool Moon-grey Earthshine across the terraced ridges and braided swirl arcs, warm headlamp beam, cyan-and-bronze Ilmenite dike glint, and dish beacon |
| [`v3_1_hook.png`](v3_1_hook.png) | `1920×1080` | **V3 · View 1 (The HOOK Vista — Dusk / "C Low Sun"):** Satin vitreous obsidian-indigo impact-melt sea softly reflecting Earth and the amber beacon, crossed by "The Great Eddy" spiral-tail swirl vortex |
| [`v3_2_day.png`](v3_2_day.png) | `1920×1080` | **V3 · View 2 (Inside the Mare — Day / "C Low Sun"):** Silver-gold radial ejecta rays, fractured Ilmenite geode crystal spires in pale ejecta halos, VIS cylindrical archive dish tower, stalled Lumen service cart, and 07 |
| [`v3_3_night.png`](v3_3_night.png) | `1920×1080` | **V3 · View 3 (Inside the Mare — Night / "A Moon Grey"):** Deep-sea calm under cool Moon-grey Earthshine, satin melt reflections, glowing geode crystal facets in 07's headlamp, and warm VIS dish beacon |

---

## 2. Owner Taste & Design Guardrails Enforced Across All 3 Directions

1. **R1 Stylized Realism (`docs/art/r1-pipeline.md`):**
   - Uses canonical R1 chamfered hard-surface silhouettes (`15–25 mm` primary hull bevels, `6–12 mm` frame chamfers), flat-planed weighted normals, and low-frequency patchy ground variation (`docs/VISION.md:138-139` Ruling 9: *"Ground variation is patchy, never confetti"*).
2. **Solitude — Polished, Lonely, but Peaceful (`docs/VISION.md:39-60` Pillar 6):**
   - Rover 07 is kept at its canonical true scale (`1.47 m W × 2.01 m L × 1.59 m H`, `docs/design/scale.md:123`) against a `~1.2 × 0.9 km` dark basalt basin and a `20–24 m` tall comms dish, making the player *feel* how small 07 is and how far the horizon is.
3. **Physical Polar Lighting — Day = `"C Low sun"`, Night = `"A Moon grey"` (`docs/VISION.md:211-240`, `WorldEvents.cs:18-24`):**
   - **Day (`"C Low sun"`):** A low polar Sun (`~5.5°` elevation) skims the horizon, throwing long rhythmic shadows across the basalt swells and warming sunlit rims/highlights (`#FFE4B5` / `#FFB547`) while keeping shadow planes cool indigo-slate.
   - **Night (`"A Moon grey"`):** The Sun is below the horizon; only cool, near-neutral Moon-grey Earthshine (`#A6A7AB` lit dust, `#787A80` mid, `#3E4048` shadow) from the fixed low Earth lights the world, making 07's warm headlamp (`#FFB547`), the Mare Dish beacon (`#FFB547`), and the cyan scan brackets (`#5FF3FF`) precious islands of warmth and life.
4. **Strict Vacuum Logic & Zero Hole Clusters (`docs/design/vacuum-shapes.md:18-45`, `tech-eras.md:28-29`):**
   - **Zero** wings, balloons, parachutes, kites, propellers, wind turbines, or atmospheric smoke plumes.
   - **Zero** clusters of identical holes or circular pitting patterns on rocks, terrain, or hardware.
   - Every object's purpose reads from its silhouette at `30 m`: parabolic wire-mesh dishes for deep-space radio, open battery bays with cyan-bracketed cells for range upgrades, and cleaved metallic crystal seams for drillable titanium ore.

---

## 3. Ranking of the 3 Visual Directions (`[PROPOSAL]`)

| Rank | Direction | Verdict & Rationale |
| :---: | :--- | :--- |
| **#1 ★** | **V1 — Sinuous Ribbon & Columnar Basalt** (*Recommended*) | **Best balance of wayfinding clarity, true lunar geology, and R1 feel.** A single bold *Reiner Gamma*-style S-curve ribbon (`18–26 m` wide) gives the player an unmistakable, effortless road from the rille exit to the Mare Dish without competing curves. The **stepped hexagonal basalt-Ilmenite prisms** have the cleanest silhouette contrast against smooth mare swells and read immediately at `30–60 m` as a mineable mineral resource. |
| **#2** | **V3 — Vitreous Impact-Melt Sea & Spiral Eddy Swirl** | **Strongest "Subnautica abyssal calm" mood.** Contrasting matte pale swirl dust (`roughness 0.90`) against satin-smooth dark obsidian impact-melt (`roughness 0.42`) produces gorgeous grazing reflections of Earth and the amber dish blink at night. Ranked #2 only because the large spiral eddy loop could briefly tempt players to drive in a circle before continuing to the dish (can be cherry-picked into V1 by using V3's satin roughness contrast on V1's single S-ribbon). |
| **#3** | **V2 — Tectonic Wrinkle Ridges & Magnetic Arc Loops** | **Strongest graphic low-poly structure and crew-lore integration.** The terraced `dorsa` wrinkle ridges and Ines's 1988 coral-orange survey collar on the `55°` Ilmenite ore dikes are super readable up close, but the 3 braided magnetic-dipole swirl arcs split the visual path at the rille exit into three lanes rather than one singular highway. **Cherry-pick recommendation:** bring V2's **painted coral-orange Ines survey collar** onto V1's Ilmenite outcrops! |

---

## 4. Per-Direction Breakdown: Palette (Hex), Swirl Construction, and What Reads as "Go There"

### Direction V1 — "Sinuous Ribbon & Columnar Basalt" (`v1_1_hook.png`, `v1_2_day.png`, `v1_3_night.png`)

#### 4.1.1 V1 Palette Table (Hex)
| Role | Day (`"C Low sun"`) | Night (`"A Moon grey"`) | Usage & Material Slot Mapping |
| :--- | :---: | :---: | :--- |
| **Sky Top / Zenith** | `#0B0E2A` | `#0B0E2A` | Canonical `PaletteSwatch.SkyTop` deep space indigo |
| **Sky Horizon Band** | `#3B2A6B` / `#5E3E68` | `#2C2254` | Canonical `PaletteSwatch.SkyHorizon` violet glow (+ warm low-sun rim at dusk) |
| **Mare Basalt Trough** | `#121522` | `#0F121D` | Deep black-blue flood basalt floor (darker than basin `Charcoal` `#2B2A3A`) |
| **Mare Basalt Swell** | `#202538` | `#1B2030` | Low-frequency basalt swells (`wavelength 70–90 m`, `roughness 0.82`) |
| **Ejecta Rays** | `#686E82` | `#5E6478` | Radial crater ejecta rays & outcrop base aprons (`DustShadow`–`DustMid` bridge) |
| **Lunar Swirl Ribbon** | `#D4D8E4` | `#B8C0D4` | High-albedo unweathered feldspathic regolith highway (`roughness 0.94`) |
| **Ilmenite Ore Vein** | `#78A8D8` / `#E08A48` | `#4EB8FF` / `#E08A48` | Specular metallic `FeTiO3` stair-step plates (`R1_BareSteel` + `R1_Copper` sheen) |
| **Piezo Seam / Scan** | `#5FF3FF` | `#5FF3FF` | Canonical `PaletteSwatch.TechGlow` (`Cell Pack III` brackets & Ilmenite core seam) |
| **Mare Dish / 07 Warmth** | `#FFB547` / `#F4E6C8` | `#FFB547` / `#F4E6C8` | Canonical `R1_LampAmber` (`#FFB547`) beacon/headlamp + `R1_CreamEnamel` (`#F4E6C8`) |
| **Huge Low Earth** | `#3FA7D6` / `#7BD389` | `#3FA7D6` / `#7BD389` | Canonical `EarthOcean` / `EarthLand` (`T_R1_Earth_Albedo.png`) |

#### 4.1.2 V1 Swirl Construction (Terrain & Decals in Unity URP) (`[PROPOSAL]`)
1. **Base Terrain Splat / Vertex Channel (`MareMask.r`):**
   - Each streamed mare terrain chunk (`64×64 m` mesh grid, `0.6-world-regions.md` §7 Slice 1 & 3) evaluates the signed distance `d(x, z)` to an authored **Catmull-Rom world spline** running from the west rille exit `(x, z)` through the mid-mare to the Comms Dish anchor.
   - The primary ribbon weight is `swirlCore = exp(-pow(d / width(t), 2.2))` (`width = 18..26 m`), combined with a secondary domain-warped Perlin FBM loop `swirlWisp` so the outer boundary has authentic *Reiner Gamma* magnetic-standoff wisps without high-frequency noise.
2. **Crisp Close-Range Ribbon Decal (`URP DecalProjector` / Skirt Mesh):**
   - Because a `64×64` chunk vertex grid has `~1–4 m` vertex spacing, we overlay a **spline-extruded decal strip (`MoonProject/R1/SwirlRibbonDecal`)** (`ZWrite Off`, `Offset -1, -1`) along the centerline with a tileable `512×512` feather mask (`256 px/m` texel density).
   - In the shader, `swirlWeight` interpolates albedo from `#121522` (basalt) to `#D4D8E4` (swirl dust), increases roughness from `0.82 → 0.94`, and adds a subtle **opposition-surge retro-reflection term** (`pow(saturate(dot(V, L_earth)), 4.0) * 0.14`) so that under Night (`"A Moon grey"`), the swirl glows softly under Earthshine like a natural moonlit road.

#### 4.1.3 What Reads as "Go There" in V1
- **At `500–1,000 m` (Hook Vista from the West Rim):**
  1. **The Giant Pale S-Curve on Black Basalt:** The `10:1` value contrast between the `#D4D8E4` swirl ribbon and the `#121522` basalt sea acts as a literal painted arrow across the basin floor.
  2. **The Amber Blink + Vertical Signal Pillar (`#FFB547`):** Placed directly at the focal terminus of the S-curve under the huge low Earth. In a cold indigo-slate frame where `98%` of pixels are cool (`#0B0E2A`–`#686E82`), the pulsing `#FFB547` dish masthead and its faint vertical pillar are the only warm beacon on the horizon (`VISION.md` Pillar 4: *"Warmth in the cold"*).
  3. **Three Peripheral Future-Region Lures (`0.6-world-regions.md` §3.1 R1):** Gold-lit steps + Atlas's warm work lamp on the north-right **Rim Terraces (0.7)**, the cyan-emerald **Lava Tube Skylight (0.8)** glow on the mid-far right plain, and the dark **Shadowed Crater (0.8)** bowl on the far left.
- **At `15–60 m` (Inside the Mare — Day & Night):**
  1. **Stalled `SSR-3` Survey Cart at the Rille Mouth:** Framed right at the pass exit with its open rear battery bay and **3 glowing cyan `Cell Pack III` blueprint brackets (`#5FF3FF`)** — teaching the range gate *"at the door"* (`0.6-world-regions.md` §1 R5).
  2. **Stepped Hexagonal Ilmenite Outcrops:** Dark basalt columns split open to reveal mirror-bright steel-blue (`#78A8D8`) and copper-bronze (`#E08A48`) stair-step crystal plates plus a glowing `#5FF3FF` piezo-mineral seam and loose ore chunks at scoop height (`0.2–0.4 m`).

---

### Direction V2 — "Tectonic Wrinkle Ridges & Magnetic Arc Loops" (`v2_1_hook.png`, `v2_2_day.png`, `v2_3_night.png`)

#### 4.2.1 V2 Palette Table (Hex)
| Role | Day (`"C Low sun"`) | Night (`"A Moon grey"`) | Usage & Material Slot Mapping |
| :--- | :---: | :---: | :--- |
| **Sky Top / Horizon** | `#0B0E2A` / `#342864` | `#0B0E2A` / `#26204B` | Deep indigo sky to slate-violet horizon glow |
| **Lava Flow Plates** | `#101420` | `#0D111B` | Flat, smooth cooled gunmetal-blue basalt plates (`roughness 0.72`) |
| **Wrinkle Ridge Step** | `#242B42` | `#1E2538` | Terraced tectonic `dorsa` compression ridges catching low-sun rims |
| **Diagonal Ejecta Band** | `#747B90` | `#666D82` | Linear crater ejecta rays crossing basalt plates |
| **Magnetic Swirl Loops** | `#DCE0EC` / `#848A9E` | `#C2C8DC` / `#6E7488` | 3 braided magnetic-dipole arc loops converging on the dish plinth |
| **Ilmenite Dike Vein** | `#E08A48` / `#52B8FF` | `#E08A48` / `#52B8FF` | `55°` tilted strike-ridge ore blade with copper-bronze & titanium-blue facets |
| **Ines Survey Collar** | `#FF8A5B` | `#FF8A5B` | Canonical `R1_OrangeStripe` painted crew core-drill target ring on outcrops |
| **Mare Dish & Plinth** | `#FFB547` / `#E8B84B` | `#FFB547` / `#E8B84B` | Twin-tower parabolic dish (`R1_GoldFoil` + `R1_LampAmber` beacon) on a `3.5 m` ridge plinth |

#### 4.2.2 V2 Swirl Construction (Terrain & Decals in Unity URP) (`[PROPOSAL]`)
1. **Terraced Ridge Heightfield (`tanh` Step Function):**
   - Chunk height evaluation adds `0.55 * tanh(sin(x * 0.065 + 0.35 * sin(z * 0.035)) * 3.2)` so the mare floor forms crisp, flat-topped tectonic wrinkle ridges (`dorsa`) with faceted low-poly bevel steps that catch sharp grazing highlights under `"C Low sun"`.
2. **Analytical Magnetic-Dipole Ribbon Strips:**
   - The 3 braided swirl arcs follow the horizontal field lines of a virtual crustal magnetic dipole centered under the Mare Dish plinth: `arc_k(t) = lerp(rilleExitX, dishX, t) + offset_k * sin(pi * t)`.
   - Exported or generated per chunk as **conformant low-poly ribbon meshes** (`y + 0.015 m`) with vertex-alpha edge feathering, ensuring razor-sharp graphic boundaries at any camera distance with zero texture memory overhead.

#### 4.2.3 What Reads as "Go There" in V2
- **Converging Field Lines + Elevated Plinth Silhouette:** All three pale magnetic swirl arcs physically pinch together at the raised `3.5 m` basalt plinth where the twin-tower Comms Dish stands, turning the entire basin floor into a geometric perspective funnel pointing at the amber beacon.
- **Double-Coded Ilmenite Outcrops (Natural Vein + Crew Survey Collar):** Each `55°` tilted ore-fin dike is encircled by a bold coral-orange painted **1988 Ines geological survey collar (`R1_OrangeStripe` `#FF8A5B`)** and a `#5FF3FF` core target face, so the player reads both *"natural titanium vein"* and *"the crew marked this exact seam for core drilling"*.

---

### Direction V3 — "Vitreous Impact-Melt Sea & Spiral Eddy Swirl" (`v3_1_hook.png`, `v3_2_day.png`, `v3_3_night.png`)

#### 4.3.1 V3 Palette Table (Hex)
| Role | Day (`"C Low sun"`) | Night (`"A Moon grey"`) | Usage & Material Slot Mapping |
| :--- | :---: | :---: | :--- |
| **Sky Top / Horizon** | `#0B0E2A` / `#2A3168` | `#0B0E18` / `#1F2650` | Deep abyssal indigo-blue sky gradient |
| **Vitreous Melt Sea** | `#0E121E` | `#0B0E18` | Dark obsidian-indigo impact-melt plain (`roughness 0.42` satin sheen) |
| **Satin Melt Swell** | `#1C2338` | `#171D30` | Grazing specular reflection of Earth and warm amber lamps across the floor |
| **Silver Ejecta Rays** | `#848A9E` | `#70788E` | Long feathered radial ejecta streaks |
| **The Great Eddy Swirl** | `#E4E6F0` / `#7E869E` | `#C8D0E4` / `#646C82` | Spiral-tail vortex of electrostatically levitated/sorted cream-silver dust |
| **Ilmenite Geode Spires** | `#42D4FF` / `#FFB547` | `#42D4FF` / `#FFB547` | Radiating geometric `FeTiO3` crystal clusters inside fractured basalt geode mounds |
| **VIS Archive Dish** | `#F4E6C8` / `#FF8A5B` | `#F4E6C8` / `#FFB547` | `24 m` tall `R1_CreamEnamel` cylindrical tower + `R1_OrangeStripe` band + `#FFB547` beacon |

#### 4.3.2 V3 Swirl Construction (Terrain & Decals in Unity URP) (`[PROPOSAL]`)
1. **Material Roughness Inversion (`Satin Basalt` vs `Matte Swirl Dust`):**
   - Uses a 2-channel region mask (`R` = logarithmic spiral + tail SDF for "The Great Eddy", `G` = radial ejecta rays) driving **both albedo and smoothness** in `MoonProject/R1/MareTerrain`.
   - Where `R == 0` (exposed impact-melt basalt), smoothness is `0.58` (`roughness 0.42`), catching a broad, calm oceanic sheen from the low Sun, Earth, and the distant amber dish beacon. Where `R == 1` (swirl dust), smoothness drops to `0.10` (`roughness 0.90`), creating a velvety chalk-white dust pattern floating on top of dark satin glass.

#### 4.3.3 What Reads as "Go There" in V3
- **Amber Lighthouse Reflected on a Dark Glass Sea:** The satin obsidian floor catches a soft vertical specular reflection streak beneath the Mare Dish's `#FFB547` beacon and beneath Earth, doubling the vertical presence of the landmark on the horizon.
- **3-Tier Ilmenite Geode Readability (`100 m → 50 m → 30 m`):**
  1. At `100 m`: a bright circular **pale ejecta ring** on the dark floor marks every outcrop location.
  2. At `50 m`: the **fractured dark geode crown** breaks the flat horizon line.
  3. At `30 m`: the **radiating cyan-steel & warm-amber `FeTiO3` crystal spires** and loose scoop-height nodules glint in 07's headlamp.
