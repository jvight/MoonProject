# Rover 07 — Excavation & Core-Sampling Device Design (`DIG-DEVICE`)

**Status:** Approved Design Specification (Look-First Blender 5.1 & Chase-Camera Study)  
**Target Asset:** `Assets/Art/Models/Rover/Rover.fbx` (`tools/art/blender/rover/rover_parts.py`)  
**Runtime Consumers:** `RoverModelNodes.cs`, `ExcavationSystem.cs`, `RoverBodyLanguage.cs`, `RoverFeel.cs`, `RoverAnimator.cs`

---

## 1. Executive Summary & Look-First Verdict

Rover 07 is a compact lunar survey, salvage, and prospecting unit (`1.22 m` wide × `1.15 m` tall × `1.38 m` long) whose defining narrative beat in Act I is **discovering subsurface water-ice and salvageable mineral veins** beneath the dark regolith of Aristarchus Plateau.

To resolve how 07 physically digs and extracts resources—and to fix the broken placeholder scoop specification (whose pivot sat inside the solid chassis at `z = +0.58 m` and rotated backwards into the body tub)—we built, rigged, animated, and rendered three complete mechanical variants in headless Blender 5.1 under lunar-night headlamp lighting (`3400 K` twin beams + `13000 K` earthshine rim) from both the **actual in-game Chase Camera** (`distance = 7.5 m`, `height = 2.6 m`, `lookAhead = 1.6 m`, `vFOV = 52°`, camera at `(0.0, 3.167, -7.210)` looking at `(0.0, 0.45, +1.60)`) and a **close 3/4 side camera**:

| Variant | Concept | Chase-Cam Visibility | Min Clearances (Wheel / Cutter / Lamp / Head) | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| **Variant A — The Hood Drill Mast** | Compact twin-spar boom hinged on the upper hood (`z = +0.36 m`) carrying a folding vertical drill mast (`0.18 m` wide), twin-rod telescoping stabilizer foot, sliding rotary drive carriage, brass spiral auger, and translucent cyan core-sample barrel. | **PASS (`10.5%–15.9%` directly visible above/beside head, `max_y = 1.583 m`)** | Wheel: **`+210.1 mm`**<br>Cutter: **`+23.2 mm`** stowed / **`+66.0 mm`** deployed<br>Headlamps: **`+82.3 mm`** stowed / **`+20.3 mm`** deployed<br>Head (`+25°` nod): **`+34.8 mm`** | **WINNER (Recommended Canonical Device)** |
| **Variant B — The Nose Hatch Drill** | Armoured upper/lower clamshell hatch replacing `FrontIntake` (`z = +0.83 m`) with an internal 2-stage telescoping drill boom that extends forward (`+0.78 m`) and pitches down `50°` to drill in front of the bumper. | **FAIL (`0.0%` visible — `100%` occluded by `Body` tub, `max_y = 0.499 m`)** | Wheel: **`+221.3 mm`**<br>Cutter: **`+51.1 mm`**<br>Headlamps: **`+37.4 mm`**<br>Head (`+25°` nod): **`+379.2 mm`** | **REJECTED** (Invisible from Chase Camera; awkward `0.69 m` diagonal reach out of a `0.36 m` nose bay) |
| **Variant C — The Bucket Loader** | Twin dogleg loader arms pivoted on the front nose cheeks (`x = ±0.142 m, y = 0.45 m, z = +0.83 m`) holding a `0.68 m` 6-tooth excavator bucket in front of the bumper (`z = +0.96..+1.43 m`), performing a 3-stroke dig and high overhead lift (`s_smelter.png`). | **PARTIAL (`0.0%` visible during ground dig; peeks at `max_y = 0.972 m` on high lift)** | Wheel: **`+207.9 mm`**<br>Cutter: **`+110.8 mm`**<br>Headlamps: **`+53.9 mm`**<br>Head (`+25°` nod): **`+522.2 mm`** | **RUNNER-UP / OPTIONAL UPGRADE** (Clean side silhouette & smelter dump, but hidden during ground digging and feels like a bulldozer rather than a scientific core sampler) |

### Why Variant A Wins
1. **Unmistakable Readability from the Actual Chase Camera:** Because the Chase Camera sits behind and above 07 (`y = 3.167 m, z = -7.210 m`), anything operating below `y = 0.96 m` and narrower than the `0.88 m` body tub is completely hidden behind the rover's back. Variant A solves this naturally: when `DrillBoom` swings forward over the nose and `DrillMast` unfolds to vertical at `z = +1.08 m`, the upper mast and orange drive cap rise to **`y = 1.583 m`**—`43 cm` higher than 07's CRT head (`y = 1.15 m`). The player sees the mast rise over the hood, sees the rotary drive carriage feed down while headlamp-lit dust billows past both front tyres, and sees the glowing cyan **`CoreTube`** rise to eye level (`y = 1.18..1.46 m`) right beside 07's head.
2. **Iconic Character & Story Fit:** 07 is a curious, Wall-E / Curiosity / VIPER-inspired survey rover. Watching 07 plant a mechanical stabilizer foot, brace its suspension (`+3.0°` nose pitch), peer over its right cheek at the spinning brass auger, and then tilt its CRT head (`-11°` roll, `-22°` yaw) to inspect a glowing cylinder of subsurface lunar ice creates an unforgettable character moment.
3. **Zero Headlamp Obstruction & Dramatic Night Lighting:** The vertical mast is only `0.18 m` wide (`x = ±0.09 m`), sitting cleanly between `Headlamp_L` (`x = -0.26 m`) and `Headlamp_R` (`x = +0.26 m`). Both warm `3400 K` headlamp cones shine past the mast without shadow blockage, converging directly on the stabilizer pad, brass auger flutes, and expanding dust collar.

---

## 2. Detailed Variant Evaluation

### 2.1 Variant A — The Hood Drill Mast (Director's Choice — Recommended)

* **Stowed Architecture (`Frame 1`):**
  * `DrillBoom` rests flat along the sloped front hood between the neck collar and the chest cutter (`y = 0.855 m, z = +0.36..+0.82 m`).
  * To leave room for `CutterTurret` (`r = 0.088 m` at `z = +0.785 m`), `DrillBoom` uses **twin outboard box-section spars** at `x = ±0.168 m` joined by a rear cross-tube at `z = +0.36 m` and a front knuckle yolk at `z = +0.80 m`.
  * `DrillMast` folds backward `-82°` from the front knuckle so its `0.62 m` frame lies low along the hood slope (`y = 0.76..0.98 m, z = +0.28..+0.82 m`), painted in matching `RoverBody_Mat` warm grey (`#C8C2B8`) and `DarkIron_Mat` (`#2B2E34`) with a `#E06828` safety-orange upper drive cap.
  * Both `DrillFoot` and `DrillAuger` telescope inside the mast rails when stowed, so **nothing protrudes past the front bumper (`z = +0.822 m`)** during normal driving.
* **Deployed Architecture (`Frame 68 Mid-Dig` & `Frame 106 Done`):**
  * `DrillBoom` pitches up `-45°` to clear the nose and slides/arcs forward (`+0.28 m` reach along the hood rails), placing the front mast knuckle at `(0.0, 0.98 m, +1.08 m)`—cleanly ahead of the front bumper (`z = +0.80 m`) and front tyres (`z = +0.88 m`).
  * `DrillMast` unfolds to vertical (`0°` world pitch, spanning `y = 0.88..1.58 m` at `z = +1.08 m`).
  * `DrillFoot` telescopes `-0.71 m` downward on twin polished chrome jack rods (`x = ±0.052 m`), planting its rubber-soled, orange-trimmed stabilizer pad (`0.22 m × 0.16 m`) firmly on the regolith mound with a central brass guide bushing.
  * `DrillCarriage` feeds `-0.60 m` down the twin chrome guide rails while `DrillAuger` spins at `720 RPM` and extends `-0.32 m` through the stabilizer bushing into the regolith.
  * On completion (`Done`), `DrillCarriage` rises to the top of the mast (`y_local = +0.48 m`, world `y = 1.46 m`), withdrawing the auger from the ground and exposing the translucent cyan **`CoreTube`** (`y_world = 1.18..1.46 m`) right in front of 07's CRT face.

### 2.2 Variant B — The Nose Hatch Drill (Owner's Idea — Evaluated & Rejected)

* **How We Made It As Good As Possible:**
  * Replaced the static `FrontIntake` grille on the front nose (`y = 0.48 m, z = +0.64..+0.83 m`, between `Headlamp_L/R` at `x = ±0.26 m` and below `CutterTurret` at `y = 0.62 m`) with a recessed dark-iron drill bay (`NoseBayFrame`) and split armoured clamshell doors (`NoseHatch_Upper` with `#E06828` stripe and `NoseHatch_Lower` with hazard lip).
  * Built a 2-stage telescoping cylindrical boom (`NoseBoom_Stage1` + `NoseBoom_Stage2`) on an articulated pitch cradle (`NoseArmPivot`) that extends `+0.78 m` forward and pitches down `50°` (`28°` boom + `22°` rotary head) so the brass auger clears the front bumper and reaches the ground (`y = 0.0 m`) at `z = +1.14 m`.
* **Why It Still Loses:**
  1. **Zero Chase-Camera Visibility (`0.0%`):** In every single frame of the 6.0 s cycle (`Stowed`, `Mid-Dig`, and `Done`), `100%` of Variant B's vertices lie below `y = 0.56 m` and within `|x| ≤ 0.14 m`. From the Chase Camera at `(0, 3.167, -7.210)`, the rover's `0.88 m × 0.96 m` body tub blocks the entire line of sight down to `y = 0.0 m` at `z = +1.14 m`. The player never sees the hatch open, never sees the arm extend, never sees the auger spin, and never sees the core sample.
  2. **Geometric Reach Paradox:** Because the nose bay is at `y = 0.48 m` and the drill must reach `y = -0.20 m` (into the mound) at `z = +1.14 m` (ahead of the front tyres at `z = +0.88 m`), the mechanism requires a `0.91 m` total diagonal extension folded inside a `0.36 m` longitudinal bay forward of the front axle.

### 2.3 Variant C — The Bucket Loader (Current Spec Fixed — Runner-Up / Bulk-Ore Option)

* **How We Fixed the Existing Broken Spec:**
  * **Bug 1 in old spec:** `ScoopArms` was placed at `(0, 0.35, +0.58)`—deep inside the solid chassis behind the front axle (`z = +0.53 m`)—and `ScoopBucket` at `(0, -0.05, +0.26)` (`z = +0.84 m`), meaning the bucket was buried inside the front bumper when stowed!
  * **Bug 2 in old spec:** Rotating `ScoopArms` by `+55°` around `+X` swung a forward-pointing arm (`+Z`) **down and backward into the chassis** instead of lifting it, and `0.78 m`-wide straight arms collided with the front tyres (`Wheel_FL/FR` inner faces sit at `|x| = 0.47 m`, sweeping in to `|x| = 0.33 m` at `28°` steer).
  * **Our Clean Fix:** We moved the `ScoopArms` hinge forward to the lower nose cheeks at **`(0.0, 0.45 m, +0.83 m)`** with narrow inner cheek brackets (`x = ±0.142 m`, giving **`+207.9 mm`** clearance to the steered front tyres across full suspension travel) that dogleg outward *ahead* of the tyres (`z > +0.96 m`) to carry a **`0.68 m`-wide, 6-tooth excavator bucket** (`z = +0.96..+1.43 m`).
  * For digging, `ScoopArms` pitches **down `+18°`** while `ScoopBucket` curls **`-28°`** across three rhythmic bites; for the payload display / smelter dump beat (`s_smelter.png`), `ScoopArms` swings **up `-78°`** (raising the bucket above the nose to `y = 0.97 m` with **`+53.9 mm`** headlamp clearance) while `ScoopBucket` counter-rotates `+62°` to cradle the glowing ore before tipping `+48°`.
* **Why Variant A Still Beats Variant C for 07's Primary Device:**
  * During the entire ground-digging phase (`t = 0.8..3.4 s`), Variant C's bucket operates at `y = -0.10..+0.38 m` and is `0.0%` visible from the Chase Camera; only during the final overhead lift does it peek above the hood.
  * More importantly, a wide toothed bulldozer bucket turns 07 into a heavy earthmover, whereas Variant A's fold-out core drill captures the surgical, scientific spirit of prospecting for subsurface lunar water-ice. (If a bulk-ore Smelter loader upgrade is ever desired in a later act, Variant C's fixed coordinates in §3.3 are ready to drop in.)

---

## 3. Canonical Rig & Node Hierarchy Contract (`Variant A — Hood Drill Mast`)

### 3.1 Node Table & Pivot Coordinates (Unity Space: `+X` Right, `+Y` Up, `+Z` Forward)

All nodes are parented under `Body` → `DigDrill` (or aliased via `DigScoop` in `RoverModelNodes.cs` for zero-breakage backwards compatibility). Every mesh node has `scale = (1, 1, 1)` at rest and rotates cleanly around its local origin:

| Node Name | Parent | Local Position `(x, y, z)` (m) | World Rest Pos `(x, y, z)` (m) | Primary Animated Channel | Material(s) | Mechanical Role |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `DigDrill` | `Body` | `(0.000, 0.000, 0.000)` | `(0.000, 0.000, 0.000)` | None (Group Root) | — | Root container for the excavation assembly under `Body`. |
| `DrillMount` | `DigDrill` | `(0.000, 0.855, 0.360)` | `(0.000, 0.855, 0.360)` | Static | `DarkIron_Mat`, `RoverBody_Mat` | Low-profile twin-saddle hood hinge bracket mounted just ahead of `Neck` (`z = +0.16 m`) with rubber stow stops at `x = ±0.168 m`. |
| `DrillBoom` | `DigDrill` | `(0.000, 0.855, 0.360)` | `(0.000, 0.855, 0.360)` | `localPosition.z` (`+0.36 -> +0.64 m`), `localEulerAngles.x` (`0° -> -45° -> -12.6°`) | `RoverBody_Mat` (`#C8C2B8`), `DarkIron_Mat`, `Accent` (`#E06828`), `Chrome_Mat` | Twin outboard hood spars (`x = ±0.168 m`, `z = 0.00..+0.44 m` local) straddling `CutterTurret` (`+23.2 mm` stowed clearance) with front knuckle yolk at `z_local = +0.44 m`. |
| `DrillMast` | `DrillBoom` | `(0.000, 0.000, 0.440)` | `(0.000, 0.855, 0.800)` | `localEulerAngles.x` (`-82.0°` stowed -> `0.0°` world vertical when deployed) | `DarkIron_Mat`, `Chrome_Mat`, `Accent` (`#E06828`), `Brass_Mat` | `0.62 m` twin-rail guide mast (`0.18 m` wide) withchrome guide rods (`x = ±0.052 m`), orange upper drive cap (`y_local = +0.58 m`), and top beacon cone (`y_local = +0.66 m`). |
| `DrillFoot` | `DrillMast` | `(0.000, 0.020, 0.000)` | `(0.000, 0.858, 0.780)` | `localPosition.y` (`+0.02 m` stowed -> `-0.71 m` planted), `localScale.y` (`0.55 -> 1.0`) | `Chrome_Mat`, `DarkIron_Mat`, `Accent`, `Brass_Mat` | Twin telescoping jack rods (`x = ±0.052 m`, `L = 0.72 m`) + `0.22 m × 0.16 m` stabilizer pad & central brass auger bushing (`r = 0.038 m`). |
| `DrillCarriage` | `DrillMast` | `(0.000, 0.420, 0.000)` | `(0.000, 0.913, 0.384)` | `localPosition.y` (`+0.42 m` stowed -> `-0.18 m` drilling -> `+0.48 m` core inspect) | `DarkIron_Mat`, `Brass_Mat`, `Accent` | Sliding rotary drive gearbox (`0.175 m × 0.105 m × 0.145 m`) with brass motor bell housing and orange torque collar. |
| `DrillAuger` | `DrillCarriage` | `(0.000, 0.000, 0.000)` | `(0.000, 0.913, 0.384)` | `localPosition.y` (`0.00 m` stowed -> `-0.32 m` drilling -> `-0.14 m` inspect), `localEulerAngles.y` (`720 RPM` spin) | `Brass_Mat` (`#C89B3C`), `DarkIron_Mat` | `0.40 m` spiral-fluted brass auger shaft (`r = 0.024 m`, 14 helical flights `r = 0.044 m`) + dark-iron tri-cone pilot bit (`y_local = -0.43 m`). |
| `CoreTube` | `DrillCarriage` | `(0.000, 0.000, 0.000)` | `(0.000, 0.913, 0.384)` | `localPosition.y` (`0.00 -> -0.02 m`), `localScale` (`(0.65, 1.0, 0.65)` -> `(1.10, 1.0, 1.10)`) | `IceCore_Mat` (`#5FD9FF` emission), `Brass_Mat` | Translucent cyan-glowing subsurface ice core barrel (`r = 0.029 m`, `L = 0.28 m`) with brass end-caps, revealed in the upper mast window on extraction. |

### 3.2 `RoverModelNodes.cs` Contract Update

When updating `Assets/Scripts/R1/Player/RoverModelNodes.cs` to bind Variant A:

```csharp
// Canonical Dig / Core-Drill Assembly (Variant A — Hood Drill Mast)
public const string DigDrill      = "DigDrill";
public const string DrillMount    = "DrillMount";
public const string DrillBoom     = "DrillBoom";
public const string DrillMast     = "DrillMast";
public const string DrillFoot     = "DrillFoot";
public const string DrillCarriage = "DrillCarriage";
public const string DrillAuger    = "DrillAuger";
public const string CoreTube      = "CoreTube";
```

*(Note: Keep `DigScoop`, `ScoopArms`, and `ScoopBucket` constants as optional legacy/upgrade identifiers so existing code compiles without error during migration.)*

### 3.3 Reference Specification for Variant C (If Used Later as a Bulk-Ore Loader Upgrade)

Should a future milestone introduce a heavy bulk-ore loader attachment for Smelter hopper feeding, use these verified collision-free coordinates from our Variant C study:
* `DigScoop` (Empty under `Body`): `(0.000, 0.450, +0.620)`
* `ScoopArms` (Parented to `DigScoop`): local `(0.000, 0.000, +0.210)` → world **`(0.000, 0.450, +0.830)`**. Inner cheek plates at `x = ±0.142 m` (`z_local = 0.00..+0.13 m`) flaring via dogleg knees (`z_local = +0.13..+0.20 m`) to outer arms at `x = ±0.315 m` (`z_local = +0.19..+0.35 m`).
  * **Dig pitch:** `+18.0°` around `+X` (lowers bucket to `y = 0.0 m`).
  * **High-lift pitch:** `-78.0°` around `+X` (raises bucket above hood to `y = 0.97 m` with `+53.9 mm` headlamp clearance).
* `ScoopBucket` (Parented to `ScoopArms`): local `(0.000, -0.170, +0.340)` → world **`(0.000, 0.280, +1.170)`**. Width `0.68 m`, 6 brass teeth projecting forward to `z_local = +0.23 m`.
  * **Bite open:** `+24.0°`; **Dig curl:** `-28.0°`; **High-lift level compensation:** `+62.0°`; **Dump tip:** `+48.0°`.

---

## 4. Quantitative Clearance & Visibility Verification

All measurements were computed automatically in `Logs/dig/build_dig_study.py` by evaluating world-space vertex clouds against the canonical Rover 07 hierarchy across key animation frames (`Logs/dig/metrics.json`):

| Verification Check | Requirement | Variant A Measured Value | How Clearance Is Achieved |
| :--- | :--- | :--- | :--- |
| **Front Wheel Envelope** (`Wheel_FL`, `Wheel_FR`) | `> 0 mm` across full `±28°` steer and `+0.16 m` bump to `-0.22 m` droop | **`+210.1 mm`** (`21.0 cm`) | `DrillBoom` sits on the upper hood (`y = 0.855 m, |x| ≤ 0.19 m`), well inboard and above the front tyres (`|x| ∈ [0.33, 0.77] m, y ≤ 0.86 m`). Deployed `DrillMast` (`|x| ≤ 0.09 m, z = +1.08 m`) is `24 cm` inboard of the steered inner tyre shoulders. |
| **Chest Cutter Envelope** (`CutterTurret` + `CutterHead`) | `> 0 mm` to turret at `(0, 0.62, +0.785)`, `r = 0.088 m` | **`+23.2 mm`** (stowed)<br>**`+66.0 mm`** (deployed) | `DrillBoom` uses twin side spars at `x = ±0.168 m` (`inner face |x| = 0.146 m`), straddling the central `CutterTurret` boss (`|x| ≤ 0.088 m`) instead of passing over its top. |
| **Headlamp Physical & Optical Clearance** (`Headlamp_L/R` + `Glass_L/R`) | `> 0 mm` mesh clearance; must not block night light cones | **`+82.3 mm`** (stowed)<br>**`+20.3 mm`** (deployed) | Merged headlamp bezels occupy `|x| ∈ [0.168, 0.352] m, y ∈ [0.51, 0.70] m, z ∈ [0.68, 0.80] m`. Deployed `DrillMast` (`|x| ≤ 0.09 m`) and `DrillFoot` pad (`|x| ≤ 0.11 m` at ground `y = 0.0 m`) pass cleanly between the twin lamps, allowing both `3400 K` beams to converge on the borehole. |
| **Head Nod / Yaw Envelope** (`Neck` + `Head` + `FaceVisor`) | `> 0 mm` across full pitch (`+25°` down to `-55°` up) | **`+34.8 mm`** at `+25°` full down nod<br>**`+235.3 mm`** at `-55°` full up look<br>**`+95.9 mm`** in active drill pose | Stowed mast stays forward of `z = +0.28 m` (`y ≤ 0.986 m`), beneath the visor's downward arc. During the `0.15–0.95 s` hinge rise, `RoverBodyLanguage` perks `Head` `-10°` up and yaws `Neck` `-16°` left, maintaining `> 45 mm` dynamic clearance at all times. |
| **Chase Camera Readability** (`Cam_Chase` at `(0, 3.167, -7.210)`, `vFOV = 52°`) | Device & dig action must read clearly behind 07 | **`10.5%`** unoccluded (`Mid`, `max_y = 1.575 m`)<br>**`15.9%`** unoccluded (`Done`, `max_y = 1.583 m`) | Upper `DrillMast`, `#E06828` drive cap, sliding `DrillCarriage`, and glowing cyan `CoreTube` project `43 cm` above the CRT head (`y = 1.15 m`) and to the right of the `-16°..22°` yawed head. |

---

## 5. Animation Choreography, Body Language & Gameplay Loop

### 5.1 Gameplay Mechanics (Ruling 4 — Hold-to-Drill with Kept Progress)
* **Trigger:** When Rover 07 stops within `2.4 m` of an excavatable `RegolithMound` / `IceVeinDeposit` (within a `±40°` forward cone) and the player holds **Interact (`E` / Gamepad West)**, `ExcavationSystem` initiates the drill cycle.
* **Kept Progress on Release:** Drilling a deposit takes **`3.0 s` of active auger contact** (`progress ∈ [0.0, 1.0]`). If the player releases Interact early or nudges the throttle, `ExcavationSystem` **preserves `mound.DrillProgress`** and smoothly stows the mast (`0.65 s` quick-stow). Re-engaging resumes from the saved borehole depth.
* **Completion Payoff:** When `progress` reaches `1.0`, `DrillCarriage` automatically executes the **`Core Extraction & Inspection` beat (`1.2 s`)**, lifting the glowing `CoreTube` to eye level, spawning or awarding the extracted `IceCore` / `RegolithOre` resource, and folding the mast back onto the hood rubber stops.
* **How Smelter Feeding Works with Variant A:** At the Smelter / Refinery (`s_smelter.png`), 07 does not need a front-end bulldozer bucket to dump raw dirt; instead, 07 pulls up to the Smelter receiving chute and either ejects the canisters from its cargo bay or swings `DrillBoom` forward (`-25°`) to uncouple the glowing `CoreTube` directly into the smelter hopper.

### 5.2 Full 6.0-Second Keyframe Choreography (`24 fps` = 144 Frames)

| Phase | Time (s) | Frames | Primary Mechanism Motion (`DigDrill` Hierarchy) | Secondary Chassis & Head Motion (`RoverFeel` / `RoverBodyLanguage`) | VFX & Lighting |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **0. Stowed Rest** | `0.00 – 0.15 s` | `1 – 4` | `DrillBoom` at `(0, 0.855, +0.36)`, `pitch = 0°`. `DrillMast` folded `-82°` along hood. `DrillFoot` (`y = +0.02 m`) and `DrillAuger` (`y = 0.0 m`) nested inside mast. | Chassis level (`pitch = 0°`). `Head` neutral (`0°`), `FaceRenderer` default eyes. | Headlamps steady; `CoreTube` unlit (`scale = 0.65`, hidden inside carriage). |
| **1. Hinge Rise** | `0.15 – 0.95 s` | `4 – 23` | `DrillBoom` pitches up **`-45.0°`** around rear hood hinge (`z = +0.36 m`), lifting the folded mast high above the nose (`y = 1.64 m`) so it immediately pops above the CRT head in the Chase Camera. | `Head` perks up **`-10.0°`** to clear and watch the rising boom; slight rear suspension squat (`-0.5°` pitch). | Servo indicator light on `DrillMast` upper cap blinks warm amber. |
| **2. Forward Swing & Foot Plant** | `0.85 – 1.82 s` | `21 – 44` | `DrillBoom` slides/arcs forward `+0.28 m` (`z = +0.64 m`, `pitch = -12.6°`) while `DrillMast` unfolds from `-82°` to **`+9.6°` local (`0°` world vertical)** at `z = +1.08 m`. At `t = 1.35..1.82 s`, `DrillFoot` jacks telescope **`-0.71 m`** down to plant the stabilizer pad on the mound (`y = 0.0 m`). | As `DrillFoot` plants on the mound (`t = 1.65 s`), front chassis lifts slightly and braces with **`+3.0°` pitch** and `-16 mm` vertical preload. `Neck` yaws **`-16.0°` left** and `Head` pitches **`+14.0°` down** (`+5.0°` roll) to peer around the mast at the stabilizer pad. | Small dust puff under `DrillFoot` pad on touchdown (`t = 1.75 s`). |
| **3. Active Auger Spin & Feed (`Mid-Dig`)** | `1.85 – 3.75 s` | `45 – 90`<br>*(Key pose: `f = 68`)* | `DrillAuger` spins up to **`720 RPM`** (`+2160°/s` around local `Y`). `DrillCarriage` feeds down from `y_local = +0.42 m` to **`-0.18 m`** while `DrillAuger` telescopes an additional **`-0.32 m`** through the foot bushing into the regolith (`tip y = -0.35 m`). | Continuous **`14 Hz` rotary chassis shudder** (`±7 mm` vertical, `±0.65°` pitch) transmitted through the planted foot into `RoverFeel`. `FaceRenderer` narrows to focused `Squint` eyes. | Billowing ground-hugging **dust collar** expands radially (`r = 0.07 -> 0.58 m`) in the twin `3400 K` headlamp cones while low-gravity **`1/6 g` dark regolith clods** arc ballistically (`y ≤ 0.42 m`) past both front tyres. |
| **4. Core Extraction & Eye-Level Inspection (`Done`)** | `3.75 – 5.00 s` | `90 – 120`<br>*(Key pose: `f = 106`)* | Auger spin decelerates to a halt. `DrillCarriage` retracts upward to the top of the mast (**`y_local = +0.48 m`**, world `y = 1.46 m`), pulling the bit clear of the ground and revealing the glowing icy-cyan **`CoreTube`** in the upper mast window (`y_world = 1.18..1.46 m`). | Chassis shudder stops. `Neck` yaws further left (**`-22.0°`**) and `Head` tilts curiously (**`-11.0°` roll**, `+7.0°` pitch) so 07 looks directly at the glowing cyan core tube beside its face. `FaceRenderer` displays `Wide / CuriousSparkle` eyes. | `CoreTube` emission ramps to **`3.5 W` cyan (`#5FD9FF`)**, casting a cool cyan rim across 07's hood, neck, and CRT bezel. Dust plume settles in `1/6 g`. |
| **5. Fold & Stow Clunk** | `5.00 – 6.00 s` | `120 – 144` | `DrillFoot` retracts (`-0.71 -> +0.02 m`), `DrillMast` folds back to `-82°`, and `DrillBoom` settles onto the hood rubber stops at `(0, 0.855, +0.36)`. | At `t = 5.75 s`, a damped **`38 rad/s` mechanical clunk** rocks the chassis (`-1.2°` pitch bounce) as the mast latches home. `Neck` and `Head` return to `(0°, 0°, 0°)`. | `CoreTube` light fades as the sample transfers into internal storage. |

---

## 6. Audio Beat Sheet (R1 Audio Catalog Integration)

All sound beats map cleanly onto the existing R1 procedural/clip audio architecture (`RoverAudioController`):

| TimeWindow | Animation Beat | R1 Audio Event / Clip | Pitch / Envelope Notes |
| :--- | :--- | :--- | :--- |
| **`0.15 – 0.95 s`** | **1. Hinge Rise** | `sfx_rover_servo_lift` (or `cutter_deploy_servo` @ `0.88x` pitch) | Rising electric actuator whine (`0.85 -> 1.08` pitch) as `DrillBoom` lifts off the hood stops. |
| **`0.95 – 1.75 s`** | **2a. Mast Unfold** | `sfx_rover_servo_swing` (`0.95x` pitch) | Smooth hydraulic/ball-screw slew while `DrillMast` rotates to vertical. |
| **`1.70 – 1.85 s`** | **2b. Stabilizer Foot Plant** | `sfx_drill_foot_plant` (`rover_land_thud` + metallic latch click) | Crisp metallic *chak-THUD* as the twin jack rods lock and the stabilizer pad compresses the lunar regolith. |
| **`1.85 – 3.75 s`** | **3. Active Auger Spin & Grind** | `sfx_drill_auger_loop` (`cutter_spin_loop` @ `0.62x` pitch layered with granular regolith crunch) | Low-mid torque motor hum (`140 Hz` fundamental matching the `14 Hz` visual shudder sub-harmonic) + gritty basalt/ice crushing texture that deepens in pitch (`1.0 -> 0.82`) as borehole depth increases. |
| **`3.75 – 4.30 s`** | **4a. Core Withdrawal** | `sfx_rover_servo_retract` (`1.12x` pitch) + `sfx_regolith_slough` | High-torque upward hoist whir accompanied by a soft cascade of pebbles falling back into the borehole. |
| **`4.30 – 4.95 s`** | **4b. Ice Core Reveal & Inspect** | `sfx_core_extract_chime` (crystalline resonant bell + quiet CRT cheerful chirp) | Plays at `t = 4.35 s` (`Frame 105`) as the cyan `CoreTube` reaches peak height (`y = 1.46 m`) and 07 tilts its head (`-11°` roll) to admire the sample. |
| **`5.00 – 5.85 s`** | **5. Fold & Hood Latch Clunk** | `sfx_rover_servo_stow` -> `sfx_drill_stow_clunk` (`rover_latch_clunk` at `t = 5.75 s`) | Decelerating servo sweep ending in a solid, weighty rubber-and-iron *CLUNK* at `t = 5.75 s` synced with the `-1.2°` chassis settle bounce. |

---

## 7. Study Artifacts & Reproducibility

All study scripts, quantitative metrics, still poses, and animations are stored under `Logs/dig/` (git-ignored):
* **Generator & Evaluator Script:** `Logs/dig/build_dig_study.py` (invokes `tools/art/blender/rover/rover_Guarantee` / `rover_parts.build_rover_model_hierarchy` without modifying any file in `tools/art/blender/rover/*`).
* **Media Encoder Script:** `Logs/dig/encode_media.py`
* **Side-by-Side Comparison Gallery:** `Logs/dig/index.html`
* **Quantitative Clearance & Occlusion JSON:** `Logs/dig/metrics.json`
* **Animations (`.gif` & `.mp4`):**
  * `Logs/dig/variant_a/{a_chase,a_side}.{gif,mp4}`
  * `Logs/dig/variant_b/{b_chase,b_side}.{gif,mp4}`
  * `Logs/dig/variant_c/{c_chase,c_side}.{gif,mp4}`
* **High-Resolution Night Poses (`1280×720`):**
  * `Logs/dig/variant_{a,b,c}/poses/{a,b,c}_{chase,side}_{stowed,mid,done}.png`
