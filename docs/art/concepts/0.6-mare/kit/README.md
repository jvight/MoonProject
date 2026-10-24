# The Long Mare (`0.6`) — R1 Environment Kit (`box/agy-mare06`)

Showcase and technical specification for **The Long Mare (`0.6`)** R1 environment kit (`Assets/_Project/Art/R1/Models/Mare/`, `Assets/_Project/Art/R1/Textures/T_R1_Mare*`, `Assets/_Project/Shaders/R1/R1MareDecal.shader`, and `Assets/_Project/Generated/Art/R1/`), built deterministically via headless Blender (`tools/art/blender/mare_kit.py`), procedural texture synthesis (`tools/art/r1/generate_mare_textures.py`), and `R1MareKitBuilder.cs`.

---

## Showcase Capture Sheet (`kit_sheet.png`)

![The Long Mare 0.6 R1 Environment Kit — Wide Establishing Shots & Close Hero Shots (Day C Low Sun & Night A Earthlit)](kit_sheet.png)

- **Top Row — Wide Establishing Shots (Kit Laid Out on the Mare Plain, Long Shadows, Earth Low on the Horizon):**
  - **Top-Left (`mare_kit_dayC_overview.png` — Low Sun Day C):** Wide establishing view of the full Mare kit laid out across the dark `#1D2332` `R1_MareBasalt` plain under a `9.0°` grazing polar sun (`#FFF6E8`), with long crisp shadows raking across the sinuous, feathered pale `Decal_MareSwirl` S-curve and soft streaky `Decal_MareEjectaRay` dust rays, the 4 `Rille_*` channel pieces framing the West-Rim Rille Pass along the left flank, the 6 natural rounded-weathered basalt boulders (`MareRock_00..05`), the 2 `MareOutcrop_Ilmenite_00..01` drill-target outcrops, the 3 Mare derelicts, **Rover `07` (`1.59 m` tall)** for scale, and a **huge half-lit Earth low on the horizon**.
  - **Top-Right (`mare_kit_nightA_overview.png` — Earthlit Night A):** Same wide establishing view under cool `#829FD4` Earthshine and a `#05070E` deep sky with the low Earth gleaming on the horizon. The soft, sinuous `Decal_MareSwirl` and streaky `Decal_MareEjectaRay` reflect cool Earthshine across the dark basalt plain while `07`'s warm headlamp cone sweeps across the foreground derelicts.
- **Bottom Row — Close Hero Shots (Day C Low Sun & Night A Earthlit):**
  - **Bottom-Left (`mare_kit_dayC_closeup.png` — Low Sun Day C):** Close inspection view of **Rover `07`** alongside the natural rounded-weathered basalt boulders (`MareRock_00..05`), `MareOutcrop_Ilmenite_00` & `01` (catching a calm bronze-gunmetal `FeTiO3` metallic sheen with zero checker grids), the softened `Derelict_MareConvoy` (3 haul bots in `07`'s rounded/pillowed shape language with multi-segment bevels, rocker-bogie suspension tubes, and `StrappedLoad`), `Derelict_MareSurveyDrone` (4 De Laval cold-gas RCS nozzles in `ImpactFurrow`), and `Derelict_TardigradeCapsule` resting on the soft feathered `Decal_MareSwirl`.
  - **Bottom-Right (`mare_kit_nightA_closeup.png` — Earthlit Night A):** Close inspection view under Earthlit Night A showing `07`'s warm amber headlamp illuminating the natural basalt rocks, `MareOutcrop_Ilmenite_00`, and `Derelict_MareConvoy`, while `Derelict_TardigradeCapsule` in the foreground displays its **tiny warm amber lit window (`TinyWindow`)**.

---

## 1. Mare Ground Material & Feathered Wayfinding Decals

| Asset | Source Texture / FBX | Generated Material / Prefab | Key Parameters & Visual Rules |
|---|---|---|---|
| **Mare Basalt Ground** | `T_R1_MareBasalt_{Albedo,Normal,Mask}.png` + `MareGround_Patch.fbx` | `R1_MareBasalt.mat` (`MoonProject/R1/Lit`) / `MareGround_Patch.prefab` | Dark blue-black flood-basalt (`#1D2332`..`#2F384E`), subtle domain-warped weathered FBM grain (no periodic wave/checker grid, zero round hole clusters), `_BumpScale = 0.55`, world-space tiling-break (`_ColorVarScale = 0.55`, `_ColorVarAmount = 0.065`). |
| **Pale Lunar Swirl Decal** | `T_R1_MareSwirl_{Albedo,Normal,Mask}.png` (`512×512` RGBA) + `Decal_MareSwirl.fbx` | `R1_MareSwirl.mat` (`MoonProject/R1/MareDecal`) / `Decal_MareSwirl.prefab` | Soft, sinuous, multi-filament *Reiner Gamma*-style S-curve pattern (`#AEBAD0`..`#E2E9F6`) with alpha-blended UV + vertex-alpha feathering that dissolves smoothly to `alpha = 0` at all borders (zero hard polygon edges), `Emission = 0`, `ShadowCastingMode.Off`, no colliders. |
| **Pale Ejecta-Ray Decal** | `T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png` (`512×512` RGBA) + `Decal_MareEjectaRay.fbx` | `R1_MareEjectaRay.mat` (`MoonProject/R1/MareDecal`) / `Decal_MareEjectaRay.prefab` | Soft, streaky anisotropic radial crater-ejecta dust rays (`#5D677C`..`#9AA5BC`) with alpha-blended border feathering (zero hard polygon edges), `Emission = 0`, `ShadowCastingMode.Off`, no colliders. |

---

## 2. Mare Rock Kit (6 Natural Basalt Boulders + 2 Ilmenite Drill-Target Outcrops)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `MareRock_00.fbx` | `mare_rock_00.json` | `1.90 × 1.44 × 0.62` | 248 | Low irregular rounded flood-basalt pillow boulder with companion weathered lobe and soft regolith skirt (not boxy). |
| `MareRock_01.fbx` | `mare_rock_01.json` | `2.16 × 1.88 × 1.18` | 248 | Asymmetric rounded-weathered mare basalt erratic with soft weathered edges and companion boulder lobe. |
| `MareRock_02.fbx` | `mare_rock_02.json` | `2.77 × 2.16 × 1.45` | 280 | Multi-lobe rounded weathered basalt outcrop with soft crevices and flanking boulder shells. |
| `MareRock_03.fbx` | `mare_rock_03.json` | `2.36 × 1.92 × 2.12` | 248 | Tall rounded-weathered basalt erratic casting a long landmark shadow across the plain. |
| `MareRock_04.fbx` | `mare_rock_04.json` | `3.50 × 2.66 × 1.02` | 280 | Broad low weathered flood-basalt hummock with rounded companion lobes. |
| `MareRock_05.fbx` | `mare_rock_05.json` | `1.16 × 1.07 × 0.54` | 224 | Compact rounded-weathered basalt foreground cobble boulder. |
| `MareOutcrop_Ilmenite_00.fbx` | `mare_outcrop_ilmenite_00.json` | `2.76 × 2.24 × 1.60` | 608 | Medium natural `FeTiO3` drill target (`HostRock` rounded basalt cradle in `R1_MareBasalt` + organic weathered `IlmeniteVein` lobes in `R1_Ilmenite`, `DrillTarget_Anchor` at `Y = 0.58 m`, non-emissive bronze-gunmetal sheen, no checker grid). |
| `MareOutcrop_Ilmenite_01.fbx` | `mare_outcrop_ilmenite_01.json` | `3.58 × 2.67 × 1.95` | 688 | Large natural `FeTiO3` seam outcrop (`HostRock` + multi-lobe weathered `IlmeniteVein` + `DrillTarget_Anchor` at `Y = 0.62 m`, non-emissive bronze-gunmetal sheen). |

---

## 3. Rille Pass Modular Pieces (4 West-Rim Channel Chunks)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `Rille_Wall_Straight.fbx` | `rille_wall_straight.json` | `5.56 × 12.94 × 5.16` | 600 | 12 m straight weathered basalt rille channel wall (`CliffWall` + `TalusApron` + `Start_Anchor` / `End_Anchor`). |
| `Rille_Wall_Curve.fbx` | `rille_wall_curve.json` | `5.66 × 12.96 × 5.16` | 600 | 12 m 30° sinuous bend weathered basalt rille wall (`CliffWall` + `TalusApron` + `Start_Anchor` / `End_Anchor`). |
| `Rille_Slope_Ramp.fbx` | `rille_slope_ramp.json` | `5.47 × 12.47 × 3.70` | 708 | 12 m driveable rille floor/rim transition ramp (`SlopeBed` + weathered `LowRimWall` + `Start_Anchor` / `End_Anchor`). |
| `Rille_Mouth_Corner.fbx` | `rille_mouth_corner.json` | `6.40 × 7.96 × 5.25` | 448 | Weathered basalt rille-mouth headland buttress (`HeadlandButtress` + `TalusApron` + `Start_Anchor` / `End_Anchor`). |

---

## 4. Mare Derelict Set (Same Maker Families, Vacuum Logic, No Floating Loot)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `Derelict_MareConvoy.fbx` | `derelict_mare_convoy.json` | `1.58 × 7.40 × 1.08` | 14116 | **The Stopped Convoy:** 3 Kessler–Arai / ILC haul bots (`HaulBot_Lead`, `HaulBot_Cargo`, `HaulBot_Rear`) softened in **Rover `07`'s shape language** (pillowed cream-enamel beetle hulls, multi-segment bevels, domed nose bonnets, wrap-around orange beltlines, tubular rocker-bogie suspension arms, and rounded hooded drooped sensor heads), stopped in a line facing `+Z` in `WheelRuts`, all dark (`Emission = 0`), middle bot carrying `StrappedLoad` (salvage crate + copper drum + tension straps), `CutPoint`, `FragmentSocket_CutterRibs_0..2`, and maker decals. |
| `Derelict_MareSurveyDrone.fbx` | `derelict_mare_survey_drone.json` | `1.64 × 2.08 × 0.90` | 2120 | **Crashed Survey Drone:** Tycho / Kessler–Arai vacuum survey hopper (`DroneBody`, `ThrusterPods` with 4 canted brass De Laval cold-gas RCS bells — zero rotors/propellers/wings, `SurveyBoom` with corona sphere, unlit `Eye_Dead`) pitched into `ImpactFurrow`, with `CutPoint`, `FragmentSocket_Spool_0`, and `Decal_Tycho_1`. |
| `Derelict_TardigradeCapsule.fbx` | `derelict_tardigrade_capsule.json` | `1.02 × 0.95 × 0.55` | 1224 | **Half-Buried Tardigrade Experiment Capsule:** Small sealed vault in `BurialMound` (a nod to *Beresheet*), white enamel & gold MLI `CapsuleBody`, bolted `ArchivePlaque`, `Decal_ILC_1`, and a **tiny warm amber lit window (`TinyWindow` / `R1_TardigradeWindow.mat`)** — bloom-safe (`_BloomSafeMax = 1.02 < Palette.BloomThreshold`). |
