# Rover 07 — Excavation & Core-Sampling Device Design (`DIG-DEVICE` Round 2)

**Status:** Approved Design Specification (Round 2: Honest Chase-Camera Verification & Variant A2 Forward-Tilted Outrigger Mast)  
**Target Asset:** `Assets/Art/Models/Rover/Rover.fbx` (`tools/art/blender/rover/rover_parts.py`)  
**Runtime Consumers:** `RoverModelNodes.cs`, `ExcavationSystem.cs`, `RoverBodyLanguage.cs`, `RoverFeel.cs`, `RoverAnimator.cs`

---

## 1. Executive Summary & Round-2 Verdict

Rover 07 is a compact lunar survey, salvage, and prospecting unit (`1.12 m` body width × `1.49 m` roof/CRT height × `1.56 m` chassis length) whose defining narrative beat in Act I is **discovering subsurface water-ice and salvageable mineral veins** beneath the dark regolith of Aristarchus Plateau.

Following Creative Director review of Round 1, we conducted a **Round 2 visual and mechanical study** in headless Blender 5.1 using the canonical Rover 07 v3 model (`R1_CreamEnamel` warm cream body with vertex-color AO/wear enhancement, `R1_Brass` lathe-turned bezels, `R1_OrangePaint` accents, procedural warm amber CRT eyes, and active twin `3400 K` halogen headlamp spot cones) to resolve four specific questions:
1. **Honest In-Game Chase Camera Framing:** In Round 1, the chase camera renders showed 07 as a tiny distant speck (`~40–60 m` away). In Round 2, `Cam_Chase` was rebuilt to match `Assets/Settings/RoverCameraTuning.asset` (`_distance = 7.5 m`, `_defaultPitch = 16°`, `_targetHeight = 1.1 m`, `_screenPosition.y = +0.10` lower-middle third of the frame) and `Assets/Prefabs/Rover.prefab` (`Rover/Camera` FOV), placing 07 in the **lower-middle third of the 16:9 viewport** (`y = 0.034..0.556` of screen height, `52.1%` tall × `26.3%` wide).
2. **Solving Variant A1's CRT Face Occlusion (`Variant A2` vs `Variant A1`):** In Round 1 (`Variant A1`), the vertical drill mast deployed right in front of 07's CRT face (`z = +1.08 m`), hiding 07's expression from front and front-three-quarter angles. We designed **`Variant A2` (Forward-Tilted Outrigger Drill Mast)**, which hinges from a lower hood outrigger mount (`(0.0, 0.80, +0.44)`), plants its wide stabilizer yoke low ahead of the front bumper (`(0.0, 0.64, +1.14)`), and tilts the twin-rail mast **`+22°` forward away from 07's face**.
3. **Material & Lighting Fidelity:** Restored 07's canonical warm cream enamel body (`R1_CreamEnamel`), dark basaltic micro-roughness lunar regolith, warm golden headlamp pools on the excavation mound, and a pure icy white-blue core sample (`#9BE0FF` / `#D1F0FF`) with **zero green color cast** on 07's body during the `done` pose.
4. **Making Variant B (Nose Hatch Drill) as Strong as Possible:** Upgraded Variant B with a recessed dark-steel armoured nose casemate, a proud **lathe-turned `R1_Brass` drill-guide collar ring** with 6 chrome studs (matching the v3 `CutterHead` bezel language), and split hazard-striped shutters so its purpose as a nose drill is unmistakable even when stowed.

### Round-2 Variant Comparison Summary

| Variant | Mechanical Concept | Honest Chase-Cam Visible Fraction (`Deploy` / `Mid-Dig` / `Done`) | 07's CRT Face Visibility (`Front-3/4` & `Side-3/4`) | Steered Wheel (`±28°`) & Headlamp Clearance | Stowed Nose Readability | Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Variant A2 — Forward-Tilted Outrigger Drill Mast** | Low-mounted twin-spar outrigger boom (`(0, 0.80, +0.44)`) that pitches down `+16°` ahead of the bumper while an open twin-rail mast tilts **`+22°` forward** (`y_max = 1.65 m` at `z = +1.53 m`), with wide outrigger stabilizer pads (`x = ±0.38 m`), brass spiral auger, and icy-blue `CoreSample`. | **PASS**<br>Deploy: **`12.7%`** (`y_max = 0.692`)<br>Mid-Dig: **`9.5%`** (`y_max = 0.621`)<br>Done: **`10.7%`** (`y_max = 0.635`) | **PASS (`100%` of CRT screen & eyes unobstructed while drilling and inspecting core)** | Wheel (`±28°`): **`3.0 cm`** (`PASS`, outriggers flare ahead of front tires)<br>Headlamps: **`PASS`**<br>CutterHead: **`PASS`** (spars straddle turret) | **Strong** — Twin orange/brass hood spars & folded mast frame the hood cleanly | **WINNER (Recommended Canonical Device)** |
| **Variant A1 — Round-1 Vertical Hood Mast** | Upper hood boom (`(0, 0.84, +0.34)`) with a vertical twin-column mast (`0°` world vertical at `z = +1.08 m`), telescoping stabilizer foot, brass auger, and core sample. | **PASS**<br>Deploy: **`16.9%`** (`y_max = 0.646`)<br>Mid-Dig: **`8.7%`** (`y_max = 0.662`)<br>Done: **`12.1%`** (`y_max = 0.694`) | **FAIL** — Vertical mast stands at `z = +1.08 m` (`y = 0.64..1.81 m`) directly across the center of 07's CRT face | Wheel (`±28°`): **`21.7 cm`** (`PASS`)<br>Headlamps: **`PASS`**<br>CutterHead: **`PASS`** | **Good** — Folded mast rests along upper hood | **SUPERSEDED BY A2** (Good chase silhouette, but blocks 07's CRT face from front/3/4 views) |
| **Variant B — Nose Hatch Drill (w/ Brass Collar)** | Armoured front nose casemate (`(0, 0.48, +0.74)`) with a proud lathe-turned `R1_Brass` collar ring, split upper/lower hazard shutters (`±78°`), and a `28°` downward-pitched telescoping arm + brass auger. | **FAIL**<br>Deploy: **`0.0%`** (`y_max = 0.332`)<br>Mid-Dig: **`0.0%`** (`y_max = 0.332`)<br>Done: **`0.0%`** (`y_max = 0.336`) | **PASS (`100%` of CRT screen & eyes unobstructed)** | Wheel (`±28°`): **`25.0 cm`** (`PASS`)<br>Headlamps: **`PASS`**<br>CutterHead: **`PASS`** (mounted on lower nose below cutter) | **Strong (Upgraded)** — Lathe-turned `R1_Brass` collar & split hazard shutters read clearly when stowed | **RUNNER-UP FOR FRONT/SIDE CUTSCENES, REJECTED FOR CHASE CAM** (`100%` occluded by 07's hull in gameplay chase view) |
| **Variant C — Bucket Loader** | Twin cheek-mounted loader arms (`(0, 0.48, +0.76)`) and a `0.68 m` wide 6-tooth excavator bucket that scoops regolith at the bumper and lifts to dump (`y_max = 1.00 m`). | **FAIL**<br>Deploy: **`0.0%`** (`y_max = 0.315`)<br>Mid-Dig: **`0.0%`** (`y_max = 0.315`)<br>Done: **`0.0%`** (`y_max = 0.452`, below `0.577` head) | **WARN** — Clear during low scoop, but wide bucket & ore load block 07's CRT face during high dump (`f = 88..112`) | Wheel (`±28°`): **`1.7 cm`** (`FAIL`, `< 2.0 cm` safety floor)<br>Headlamps: **`FAIL`** (`0.766 m` bucket sweeps across both beams) | **Fair** — Low chin bucket makes nose look front-heavy | **REJECTED** (Zero chase-cam visibility, blocks headlamps, tight tire clearance) |

---

## 2. Detailed Round-2 Findings

### 2.1 Honest Chase Camera Framing & What Actually Reads From Behind

When `Cam_Chase` is positioned at the true gameplay distance (`~7.2 m` behind 07 along a `16°` pitch ray, looking at `(0.0, 1.10 m, +0.25 m)` with horizontal FOV `46°` and vertical framing shift so 07 sits in the lower-middle third of the screen):
* **07's Screen Footprint:** Rover 07 spans `y = 0.034..0.556` from the bottom of the frame (`52.1%` of frame height) and `26.3%` of frame width.
* **The Occlusion Horizon:** Because the camera looks downward at `16°` over the top of 07's `1.49 m` CRT head (`y_screen_max = 0.535..0.556`) and `1.12 m` wide wheel stance, **any mechanism operating forward of the nose (`z = +0.80..+1.55 m`) that stays below `y = 1.15 m` and narrower than `|x| = 0.35 m` projects entirely inside 07's chassis silhouette (`y_screen < 0.46`)** and is `100%` hidden by the rover's body and head.
* **Why `A2` and `A1` Read Clearly While `B` and `C` Disappear:**
  * In **`Variant B`** (`y_max = 0.57 m`, `|x| ≤ 0.15 m`) and **`Variant C`** (`y_max = 0.52 m` during dig, `1.00 m` during dump), every vertex projects between `y_screen = 0.315` and `0.452`—well below 07's roofline (`0.535..0.577`). From behind, the player sees only 07's chassis brace reaction and the dust clods billowing out to the sides.
  * In **`Variant A2`**, although the mast base is planted low ahead of the bumper (`y = 0.64 m, z = +1.14 m`) and the mast leans `+22°` forward away from 07's face, the `0.92 m` twin-rail mast reaches **`y = 1.58..1.65 m` in world height** with a `0.45 m` wide orange/brass crown (`x = ±0.225 m`) and `0.77 m` wide front outrigger knuckles/pads (`x = ±0.387 m`). Consequently, `A2`'s upper rotary drive crown, twin brass beacon pods, central brass bell housing, and ascending icy-blue `CoreSample` project to **`y_screen = 0.621..0.692`**—rising cleanly above 07's CRT head (`y_screen = 0.535..0.552`) with **`9.5%–12.7%` direct unoccluded surface visibility** throughout deployment, drilling, and core presentation.

### 2.2 Variant A2 vs Variant A1 — Solving the CRT Face Occlusion

The Creative Director noted that while `Variant A1` read well from behind, its vertical mast stood directly in front of 07's face (`z = +1.08 m, y = 0.64..1.81 m`), blocking 07's CRT eyes from front and front-three-quarter views.

**How `Variant A2` solves this without sacrificing chase-camera readability:**
1. **Lower, Wider Outrigger Boom (`DrillBoom` at `(0.0, 0.80, +0.44)`):**
   * Instead of hinging high near the neck and holding the mast knuckle at `y = 0.90 m` in front of the CRT bezel, `A2`'s twin hood spars run low along the hood (`x = ±0.195 m`, straddling `CutterHead` cleanly) and pitch **downward `+16°`** during deployment.
   * This places the front outrigger cross-yoke low ahead of the front bumper at **`(0.0, 0.64 m, +1.14 m)`**—more than **`42 cm` below** the bottom of 07's CRT screen (`y = 1.06..1.45 m`).
2. **Forward-Tilted Open Twin-Rail Mast (`+22°` World Forward Lean):**
   * Rather than standing vertically (`0°`) in front of 07's face, `A2`'s `DrillMast` unfolds to `+6°` relative to the `+16°` downward-pitched boom, giving a **net `+22°` forward tilt away from 07's CRT screen**.
   * Because the mast base sits below the screen (`y = 0.64 m`) and tilts forward (`+Z`) as it rises, the entire mast corridor stays ahead of the front-three-quarter sightline to 07's face.
   * **Result:** In `Cam_Front34` and `Cam_Side34`, **100% of 07's warm amber CRT screen and both eyes remain completely visible and unobstructed** while 07 pitches its head down (`+16°`) and yaws (`-16°`) to watch the spinning brass auger and rising ice core!
3. **Wider Upper Crown & Outrigger Pads for Chase-Camera Readability:**
   * To compensate for the `+22°` forward lean when viewed from `Cam_Chase`, `A2` widens the open mast rails to `x = ±0.145 m` (enhancing the see-through window around the glowing `CoreSample`), caps the mast with a `0.42 m` safety-orange crown flanked by **twin `R1_Brass` telemetry/beacon pods (`x = ±0.225 m`)** and a central brass rotary bell, and flares the lower stabilizer foot outriggers to `x = ±0.355 m` ahead of the front tires.

### 2.3 Variant B Upgraded — Visible Armoured Nose Hatch & Lathe-Turned `R1_Brass` Collar

To ensure the owner's nose-drill concept (`Variant B`) was evaluated at its strongest possible visual execution:
* **Stowed Visual Readability (`NoseBayFrame` + `NoseHatch_Upper/Lower`):**
  * Mounted on the lower nose (`(0.0, 0.48 m, +0.74 m)`) below `CutterHead` (`y = 0.635 m`) and between `Headlamp_L/R` (`x = ±0.26 m`).
  * Framed by a dark-steel armoured casemate with top/bottom safety-orange bumper guards, four corner brass hex bolts, and a **proud lathe-turned `R1_Brass` drill-guide collar ring (`r = 0.076 m`) studded with 6 chrome rivets** that echoes the v3 `CutterHead` brass bezel directly above it.
  * Closed when stowed by split upper and lower `R1_CreamEnamel` & `R1_OrangePaint` hazard-striped shutters with central semicircular brass lips that form a sealed circular drill port around the chrome pilot tip.
* **Deployed Action:**
  * Upper and lower shutters swing open `±78°`, the internal brass trunnion (`DrillMount`) pitches down `28°`, and `DrillArm` + `DrillAuger` telescope forward and downward into the regolith mound right in front of the nose while both headlamps illuminate the auger flutes.
* **Assessment:** Variant B looks cohesive, purposeful, and industrial from front and side-three-quarter angles—even when stowed—and keeps 07's CRT face 100% clear. However, because the entire mechanism operates between `y = 0.00 m` and `0.57 m` directly in front of the nose, it remains **`0.0%` visible from the honest gameplay chase camera**.

---

## 3. Canonical Rig & Node Hierarchy Specification (`Variant A2 — Forward-Tilted Outrigger Drill Mast`)

### 3.1 Node Table & Pivot Coordinates (Unity Space: `+X` Right, `+Y` Up, `+Z` Forward)

All 5 animated parts are parented under `RoverModel` (or grouped under `Body/DigDrill`) using the existing rover material palette (`R1_DarkSteel`, `R1_OrangePaint`, `R1_Brass`, `R1_Chrome`, `R1_RubberDark`, plus `Study_IceCore` for the extracted sample):

| Node Name | Parent | Rest Local Position `(x, y, z)` (m) | World Rest Pos `(x, y, z)` (m) | Primary Animated Channels | Material(s) | Mechanical Role |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `DrillBoom` | `RoverModel` / `Body` | `(0.000, 0.800, 0.440)` | `(0.000, 0.800, 0.440)` | `localEulerAngles.x`: `0°` (stowed) → `-18°` (unfold lift) → `+16°` (drilling) → `+12°` (core inspect) | `R1_DarkSteel`, `R1_OrangePaint`, `R1_Brass`, `R1_Chrome` | Twin hood spars (`x = ±0.195 m`) straddling `CutterHead` with brass hydraulic cylinders, angled forward outrigger struts flaring ahead of the bumper (`z_local = +0.50..+0.72 m`) to `x = ±0.355 m` brass knuckle pods, and a `0.72 m` front cross-yoke. |
| `DrillMast` | `DrillBoom` | `(0.000, 0.040, 0.720)` | `(0.000, 0.840, 1.160)` *(unfolded)* | `localEulerAngles.x`: `-90°` (folded back on hood) → `-8°` (deploy) → `+6°` (`+22°` net world forward tilt during drill) → `+8°` (`+20°` net world forward tilt during core inspect) | `R1_OrangePaint`, `R1_DarkSteel`, `R1_Chrome`, `R1_Brass` | `0.92 m` open twin-rail mast (`x = ±0.145 m` orange columns + `x = ±0.108 m` chrome guide rods) topped by a `0.42 m` orange rotary crown cap, twin `R1_Brass` side beacon pods (`x = ±0.225 m`), and a central brass rotary bell housing. |
| `DrillFoot` | `DrillMast` | `(0.000, -0.480, 0.000)` | `(0.000, 0.360, 1.160)` *(unfolded)* | `localPosition.y`: `-0.480 m` (retracted) → `-0.700 m` (`dp_y = -0.22 m` planted on regolith) | `R1_Chrome`, `R1_OrangePaint`, `R1_RubberDark`, `R1_Brass` | Twin chrome jack rods (`x = ±0.145 m`) + splayed orange outrigger legs (`x = ±0.29 m`) + `0.64 m` rubber-soled stabilizer pad and central `R1_Brass` auger guide bushing. |
| `DrillAuger` | `DrillMast` | `(0.000, 0.220, 0.000)` | `(0.000, 1.060, 1.160)` *(unfolded)* | `localPosition.y`: `+0.220 m` → `-0.340 m` (`dp_y = -0.56 m` drilling) → `-0.120 m` (`dp_y = -0.34 m` during core display); `localEulerAngles.y`: continuous spin | `R1_Brass`, `R1_DarkSteel`, `R1_Chrome` | `0.68 m` steel drive shaft with brass chuck, 20 helical `R1_Brass` auger flights (`r = 0.046 m`), and conical `R1_Chrome` pilot bit. Stays in the lower mast bore during `done` so the upper mast window belongs cleanly to `CoreSample`. |
| `CoreSample` | `DrillMast` | `(0.000, -0.140, 0.000)` | `(0.000, 0.700, 1.160)` *(unfolded)* | `localPosition.y`: `-0.220 m` → `+0.480 m` (`dp_y = +0.62 m` raised into upper mast window); `localScale`: `0.001` (hidden) → `1.000` (`f = 84..118`) | `IceCore_Mat` (`#9BE0FF` icy white-blue emission), `R1_Brass` | `0.34 m` illuminated subsurface water-ice core cylinder (`r = 0.036 m`) with `R1_Brass` end-caps, hoisted into the open upper window of `DrillMast` at `f = 84..118` so it reads above 07's head from the chase camera and in front of 07's gaze from side/front views. |

### 3.2 Coherent Chassis Reaction Rule (`Body` + `CutterTurret` + `Neck`)

In Rover 07 v3, `CutterTurret` (`(0.0, 0.635, +0.648)`) and `Neck` (`(0.0, 0.875, +0.160)`) are siblings of `Body` under `RoverModel`. Whenever procedural or keyframed chassis pitch/heave (`body_pitch`, `body_dy`, `body_dz`) is applied during drilling brace (`+1.8°` nose pitch, `-12 mm` heave, `±4 mm` rotary shudder), **the exact same chassis transform delta must be applied coherently to `Body`, `CutterTurret`, and `Neck`** so the chest `CutterHead` never sinks into or separates from the front hood slope.

---

## 4. Full 6.0-Second Animation Choreography (`Variant A2`, 24 fps = 144 Frames)

| Phase | Frames (`24 fps`) | Time (`s`) | Mechanism Motion (`A2` Hierarchy) | Rover Body & Head Reaction | Visual & Lighting Readability |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Stowed (`stowed`)** | `f = 0 .. 12`<br>*(Key pose: `f = 0`)* | `0.00 – 0.50 s` | `DrillBoom` flat on lower hood (`0°`). `DrillMast` folded back `-90°` along hood (`y = 0.75..0.94 m`). `CoreSample` hidden (`scale = 0.001`). | Chassis level (`0°`). `Head` neutral (`0°, 0°`) with warm amber CRT eyes. | Twin `3400 K` headlamps cast warm golden pools across the dark basaltic regolith mound ahead of the nose. |
| **2. Unfold & Plant Ahead of Bumper (`deploy`)** | `f = 12 .. 36`<br>*(Key pose: `f = 26`)* | `0.50 – 1.50 s` | `DrillBoom` lifts to `-18°` (`f = 24`) so the mast clears the bumper, then pitches down to **`+16°`** (`f = 36`) while `DrillMast` unfolds to **`+6°` local (`+22°` world forward tilt)**. `DrillFoot` extends `dp_y = -0.22 m` (`f = 28..38`) to plant its wide outrigger pads ahead of the bumper. | As the foot plants (`f = 32..38`), chassis braces to **`+1.8°` pitch** and `-12 mm` heave. `Head` pitches **`+16°` down** and yaws **`-16°` right** to watch the auger touch down. | In `Cam_Chase`, the orange/brass mast crown rises to `y_screen = 0.692` (`12.7%` visible). In `Cam_Front34`, the `+22°` forward tilt leaves 07's CRT face 100% clear. |
| **3. Active Drilling (`mid_dig`)** | `f = 36 .. 95`<br>*(Key pose: `f = 60`)* | `1.50 – 3.96 s` | `DrillAuger` spins continuously (`52°/frame` = `1248°/s`) and feeds down `dp_y = -0.56 m` through the brass foot bushing into the regolith mound (`y_min = -0.275 m`) with `±0.45°` high-frequency mast vibration. | Chassis sustains `+1.8°` brace pitch with `±4 mm` rotary shudder. `Head` stays pitched `+16°` down / `-16°` yawed with focused CRT eyes; `100%` of the CRT face stays unobstructed. | Billowing regolith dust collar and 8 ballistic dust clods erupt inside the warm `3400 K` headlamp beams, visible around both front tires in `Cam_Chase` (`9.5%` mast visibility). |
| **4. Core Extraction & Presentation (`done`)** | `f = 84 .. 118`<br>*(Key pose: `f = 104`)* | `3.50 – 4.92 s` | `DrillFoot` retracts (`f = 90..99`). `DrillBoom` lifts slightly to `+12°` and `DrillMast` tilts to `+8°` (`+20°` net forward lean) while `DrillAuger` stays low (`dp_y = -0.34 m`) and the glowing icy-blue **`CoreSample`** rises to `dp_y = +0.62 m` in the upper mast window. | Shudder stops. `Head` lifts to `+6°` pitch and `-20°` yaw to admire the extracted ice core in the forward-tilted mast window. | `CoreSample` glows crisp icy white-blue (`3.6` emission, zero green cast on 07's cream hull) and rises above 07's head in `Cam_Chase` (`10.7%` visible, `y_screen_max = 0.635`). |
| **5. Fold & Stow** | `f = 112 .. 143` | `4.67 – 6.00 s` | `CoreSample` transfers/retracts (`f = 112..118`), `DrillAuger` retracts to rest (`f = 110..122`), `DrillBoom` swings through `-14°` (`f = 124`) back to `0°`, and `DrillMast` folds back to `-90°` (`f = 134`). | Chassis and `Head` settle smoothly back to `(0°, 0°, 0°)` rest pose for a seamless loop at `f = 143`. | Dust plume settles back into the lunar surface. |
