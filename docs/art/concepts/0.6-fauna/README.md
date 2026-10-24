# 0.6 Machine Fauna — Models, Prefabs & Procedural Motion (`Art`)

This directory holds the visual proof capture sheet (`fauna_sheet.png`) for the four **0.6 machine fauna** species built in the `Art` domain (`tools/art/blender/fauna.py`, `R1FaunaBuilder.cs`, `FaunaMotionTuning.cs`, `FaunaIdleMotion.cs`).

![0.6 Machine Fauna Sheet](fauna_sheet.png)

## Species Overview

| Species | FBX / Prefab | Maker Family | World Footprint (`W × L × H`) | Tris / Meshes | Single Lamp | Child Motion Pivots |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Panel Mite** (`FaunaSpecies.PanelMite`) | `PanelMite.fbx` / `PanelMite.prefab` | **Lumen** (`R1_CreamEnamel`, `R1_OrangeStripe`, `R1_DarkGlass` belly) | `0.29 m × 0.26 m × 0.135 m` (palm-sized; used in lines of 5–12) | `2,860` tris / `9` meshes | Warm amber dorsal dome (`Lamp` -> `R1_LampAmber`) | `Body`, `Lamp`, `Wiper`, `Leg_L0`, `Leg_L1`, `Leg_L2`, `Leg_R0`, `Leg_R1`, `Leg_R2` |
| **2. The Sweeper** (`FaunaSpecies.Sweeper`) | `Sweeper.fbx` / `Sweeper.prefab` | **Lumen** (`R1_CreamEnamel`, `R1_OrangeStripe`, `R1_GreyMetal`) | `1.88 m × 2.42 m × 1.18 m` (`1.20×` Rover 07's `2.01 m` length) | `6,064` tris / `8` meshes | Warm amber mast lamp (`Lamp` -> `R1_LampAmber`) | `Body`, `Lamp`, `BrushArm`, `BrushArm/Brush`, `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR` |
| **3. Survey Flock Drone** (`FaunaSpecies.SurveyDrone`) | `SurveyDrone.fbx` / `SurveyDrone.prefab` | **Tycho Consortium** (`R1_Rubber` dark slate, `R1_GoldFoil`, `R1_WhitePaint`) | `0.62 m × 0.62 m × 0.42 m` (small hex body; used in flocks of 4–6) | `3,264` tris / `7` meshes | Cyan front status lens (`Lamp` -> `R1_FaunaLampCyan`) | `Body`, `Lamp`, `Scanner`, `Nozzle_FL`, `Nozzle_FR`, `Nozzle_RL`, `Nozzle_RR` |
| **4. The Trencher** (`FaunaSpecies.Trencher`) | `Trencher.fbx` / `Trencher.prefab` | **Lumen** (`R1_CreamEnamel`, `R1_OrangeStripe`, `R1_BareSteel`, `R1_Rubber`) | `1.96 m × 3.27 m × 1.84 m` (heavy tracked cable-layer, larger than 07) | `5,112` tris / `11` meshes | Warm amber roof beacon (`Lamp` -> `R1_LampAmber`) | `Body`, `Lamp`, `TrenchBoom`, `TrenchBoom/TrenchWheel`, `CableSpool`, `Roller_FL`, `Roller_FR`, `Roller_RL`, `Roller_RR` |

## Procedural Motion (`FaunaIdleMotion` + `FaunaMotionTuning`)

- **Pure visual component (`Assets/_Project/Scripts/Art/FaunaIdleMotion.cs`):** Driven by `Speed` (m/s) or optional world-space `Transform` delta (`DeriveSpeedFromTransformDelta`), with a per-instance `PhaseOffsetSeconds` so lines of mites and flocks of drones do not move in lockstep.
- **Zero GC per frame:** `FaunaMotionTuning.EvaluatePose` returns a stack-allocated `FaunaMotionPose` value struct, and `FaunaIdleMotion.ApplyPose` updates cached pivot transforms and a single pre-allocated `MaterialPropertyBlock` with `0 B` managed allocations per frame.
- **Lamp glow contract:** Every species has exactly one `Lamp` child renderer driven via `_EmissionStrength` on its `MaterialPropertyBlock` (never overwriting the authored `_EmissionColor`), with a slow breathing cycle (`0.36 Hz`, strictly `< 3 Hz`).

## Proof Sheet Panels (`fauna_sheet.png`, 2×3 Grid)

1. **Top-Left (`fauna_panel_mite_dayC.png`):** Line of 7 `PanelMite` crawlers marching in alternating tripod gait beside Rover 07 in Day C ("Low sun") lighting.
2. **Top-Right (`fauna_sweeper_dayC.png`):** `Sweeper` (`2.42 m` L, wide `1.88 m` rotary brush head) beside Rover 07 in Day C lighting.
3. **Middle-Left (`fauna_survey_drone_dayC.png`):** Flock of 5 Tycho `SurveyDrone` hex drones hovering with gimballed cold-gas nozzles (plus one landed on skids) beside Rover 07 in Day C lighting.
4. **Middle-Right (`fauna_trencher_dayC.png`):** `Trencher` (`3.27 m` L × `1.84 m` H) with its forward toothed `TrenchWheel` and rear `CableSpool` laying cable beside Rover 07 in Day C lighting.
5. **Bottom-Left (`fauna_all_lineup_dayC.png`):** All four 0.6 machine species together beside Rover 07 in Day C lighting.
6. **Bottom-Right (`fauna_all_night_lamps.png`):** Night A / Eclipse capture of all four species beside Rover 07 with their warm Lumen (`PanelMite`, `Sweeper`, `Trencher`) and cyan Tycho (`SurveyDrone`) lamps lit.
