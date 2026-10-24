# The Long Mare (`0.6`) — R1 Environment Kit (`box/agy-mare06`)

Showcase and technical specification for **The Long Mare (`0.6`)** R1 environment kit (`Assets/_Project/Art/R1/Models/Mare/`, `Assets/_Project/Art/R1/Textures/T_R1_Mare*`, and `Assets/_Project/Generated/Art/R1/`), built deterministically via headless Blender (`tools/art/blender/mare_kit.py`), procedural texture synthesis (`tools/art/r1/generate_mare_textures.py`), and `R1MareKitBuilder.cs`.

---

## Showcase Capture Sheet (`kit_sheet.png`)

![The Long Mare 0.6 R1 Environment Kit — Day C (Low Sun) & Night A (Earthlit) with Rover 07 for Scale](kit_sheet.png)

- **Top Row — Low Sun Day C (`7.5°` grazing polar sun `#FFF6EA`):**
  - **Top-Left (`mare_kit_dayC_overview.png`):** Full kit lineup across the dark `#161C2C` `R1_MareBasalt` plain, crossed by the sinuous pale `#D0D6E4` `Decal_MareSwirl` ribbon and directional `Decal_MareEjectaRay` wayfinding streaks, framed by the 4 `Rille_*` channel chunks in the background, the 6 `MareRock_00..05` basalt boulders casting long raking shadows, the 2 `MareOutcrop_Ilmenite_00..01` drill-target outcrops, the 3 Mare derelicts, and **Rover `07` (`1.59 m` tall)** in center-foreground for scale.
  - **Top-Right (`mare_kit_dayC_closeup.png`):** Hero close-up with **Rover `07`** beside `MareOutcrop_Ilmenite_00` (showing the stepped hexagonal/tabular `R1_Ilmenite` warm bronze-black `#6E5849` and gunmetal specular glint at 07's drill height), `Derelict_MareConvoy` (3 haul bots in a line, middle bot with `StrappedLoad`), `Derelict_MareSurveyDrone` (4 De Laval cold-gas RCS nozzles in `ImpactFurrow`), and `Derelict_TardigradeCapsule` on `Decal_MareSwirl`.
- **Bottom Row — Earthlit Night A (cool `#9AB0D2` Earthshine + `#05070E` deep sky + `07`'s warm headlamp):**
  - **Bottom-Left (`mare_kit_nightA_overview.png`):** Full kit lineup under Earthlit Night A. The pale `Decal_MareSwirl` (`#8E96A8`) and `Decal_MareEjectaRay` reflect cool Earthshine across the ink-dark `#0B0E18` mare basalt as giant natural wayfinding lines. All rocks, rille walls, Ilmenite outcrops, the stopped convoy, and the crashed survey drone are strictly non-emissive (`Emission = #000000`).
  - **Bottom-Right (`mare_kit_nightA_closeup.png`):** Hero close-up under Earthlit Night A showing `07`'s warm headlamp beam raking across `MareOutcrop_Ilmenite_00`'s metallic `FeTiO3` cleavage facets and illuminating `Derelict_MareConvoy`, while `Derelict_TardigradeCapsule` in the foreground shows its **tiny warm amber lit window (`TinyWindow`)**.

---

## 1. Mare Ground Material & Wayfinding Decals

| Asset | Source Texture / FBX | Generated Material / Prefab | Key Parameters & Visual Rules |
|---|---|---|---|
| **Mare Basalt Ground** | `T_R1_MareBasalt_{Albedo,Normal,Mask}.png` + `MareGround_Patch.fbx` | `R1_MareBasalt.mat` / `MareGround_Patch.prefab` | Deep black-blue flood-basalt (`#101420` trough to `#161C2C` mid to `#222B40` crest), `Smoothness 0.08..0.24`, `Metallic ~0.04`, world-space tiling-break (`_ColorVarScale = 0.62`, `_ColorVarAmount = 0.085`), zero hole clusters. |
| **Pale Lunar Swirl Overlay / Decal** | `T_R1_MareSwirl_{Albedo,Normal,Mask}.png` + `Decal_MareSwirl.fbx` | `R1_MareSwirl.mat` / `Decal_MareSwirl.prefab` | High-albedo magnetically shielded silicate ribbon (`#C8CEDC`..`#D8DFEC`), matte `Smoothness 0.04..0.12`, `Metallic 0.00`, `Emission = 0`, `ShadowCastingMode.Off`, no colliders. |
| **Pale Ejecta-Ray Decal** | `T_R1_MareEjectaRay_{Albedo,Normal,Mask}.png` + `Decal_MareEjectaRay.fbx` | `R1_MareEjectaRay.mat` / `Decal_MareEjectaRay.prefab` | Directional radial dust streaks (`#4A5264`..`#686E82`), `Smoothness 0.06..0.15`, `Emission = 0`, `ShadowCastingMode.Off`, no colliders. |

---

## 2. Mare Rock Kit (6 Basalt Boulders + 2 Ilmenite Drill-Target Outcrops)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `MareRock_00.fbx` | `mare_rock_00.json` | `1.92 × 1.56 × 0.62` | 228 | Low asymmetric flood-basalt shelf boulder with wide regolith skirt. |
| `MareRock_01.fbx` | `mare_rock_01.json` | `2.41 × 2.02 × 1.18` | 228 | Angular tilted mare basalt erratic that casts a crisp shadow blade at low sun. |
| `MareRock_02.fbx` | `mare_rock_02.json` | `2.86 × 2.38 × 1.45` | 288 | Twin-lobe fractured basalt outcrop with central split seam and flanking spur. |
| `MareRock_03.fbx` | `mare_rock_03.json` | `2.52 × 2.18 × 2.15` | 228 | Tall craggy basalt monolith/tooth casting a long landmark shadow across the plain. |
| `MareRock_04.fbx` | `mare_rock_04.json` | `3.74 × 2.80 × 1.02` | 288 | Broad tabular flood-basalt bench with stepped horizontal lava-flow ledges. |
| `MareRock_05.fbx` | `mare_rock_05.json` | `1.33 × 1.14 × 0.54` | 168 | Compact jagged basalt foreground scatter boulder. |
| `MareOutcrop_Ilmenite_00.fbx` | `mare_outcrop_ilmenite_00.json` | `3.09 × 2.50 × 1.62` | 576 | Medium stepped `FeTiO3` drill target (`HostRock` in `R1_MareBasalt` + stepped hexagonal/tabular `IlmeniteVein` in `R1_Ilmenite`, `DrillTarget_Anchor` at `Y = 0.58 m`, non-emissive). |
| `MareOutcrop_Ilmenite_01.fbx` | `mare_outcrop_ilmenite_01.json` | `3.91 × 3.04 × 1.97` | 620 | Large columnar `FeTiO3` seam outcrop (`HostRock` + `IlmeniteVein` + `DrillTarget_Anchor` at `Y = 0.62 m`, non-emissive warm bronze-black/gunmetal glint). |

---

## 3. Rille Pass Modular Pieces (4 West-Rim Channel Chunks)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `Rille_Wall_Straight.fbx` | `rille_wall_straight.json` | `4.99 × 12.00 × 5.13` | 500 | 12 m straight rille channel wall with 3 stepped basalt cliff strata (`CliffWall`), sloped `TalusApron`, and `Start_Anchor` / `End_Anchor`. |
| `Rille_Wall_Curve.fbx` | `rille_wall_curve.json` | `5.32 × 12.37 × 5.13` | 708 | 12 m 30° sinuous bend rille wall chunk (`CliffWall` + `TalusApron` + `Start_Anchor` / `End_Anchor`). |
| `Rille_Slope_Ramp.fbx` | `rille_slope_ramp.json` | `4.95 × 12.05 × 3.74` | 356 | 12 m driveable rille floor/rim transition ramp (`SlopeBed` + `LowRimWall` + `Start_Anchor` / `End_Anchor`). |
| `Rille_Mouth_Corner.fbx` | `rille_mouth_corner.json` | `6.27 × 8.16 × 5.45` | 260 | Craggy rille-mouth headland buttress (`HeadlandButtress` + `TalusApron` + `Start_Anchor` / `End_Anchor`). |

---

## 4. Mare Derelict Set (Same Maker Families, Vacuum Logic, No Floating Loot)

| Model | Contract | Dimensions (`X × Z × Y` m) | Triangles | Description |
|---|---|---|---|---|
| `Derelict_MareConvoy.fbx` | `derelict_mare_convoy.json` | `1.52 × 7.40 × 0.99` | 5648 | **The Stopped Convoy:** 3 Kessler–Arai / ILC haul bots (`HaulBot_Lead`, `HaulBot_Cargo`, `HaulBot_Rear`) stopped in a line facing `+Z` in `WheelRuts`, all dark (`Emission = 0`), middle bot carrying `StrappedLoad` (salvage crate + copper drum + tension straps), `CutPoint`, physical salvage sockets (`FragmentSocket_CutterRibs_0..2`), and maker decals (`Decal_KA_1`, `Decal_KA_2`, `Decal_ILC_1`). |
| `Derelict_MareSurveyDrone.fbx` | `derelict_mare_survey_drone.json` | `1.60 × 2.04 × 0.98` | 1164 | **Crashed Survey Drone:** Tycho / Kessler–Arai vacuum survey hopper (`DroneBody`, `ThrusterPods` with 4 canted brass De Laval cold-gas RCS bells — zero rotors/propellers/wings, `SurveyBoom` with corona sphere, unlit `Eye_Dead`) pitched into `ImpactFurrow`, with `CutPoint`, `FragmentSocket_Spool_0`, and `Decal_Tycho_1`. |
| `Derelict_TardigradeCapsule.fbx` | `derelict_tardigrade_capsule.json` | `1.04 × 1.07 × 0.52` | 856 | **Half-Buried Tardigrade Experiment Capsule:** Small sealed vault in `BurialMound` (a nod to *Beresheet*), white enamel & gold MLI `CapsuleBody`, bolted `ArchivePlaque`, `Decal_ILC_1`, and a **tiny warm amber lit window (`TinyWindow` / `R1_TardigradeWindow.mat`)** — bloom-safe (`_BloomSafeMax = 1.04 < Palette.BloomThreshold`). |
