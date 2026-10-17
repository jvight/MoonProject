# Scale Bible & Canonical Dimensions (`docs/design/scale.md`)

> **Status**: Complete Measurement Audit & Canonical Scale Specification (No models resized in this task; all proposed model changes are marked with `[PROPOSAL]` for Director review).
> **Coordinate Conventions**:
> - **Blender / Contract JSONs (`tools/art/qa/contracts/*.json`)**: `X` = Width (left/right), `Y` = Length/Depth (back/front, `+Y` forward on Rover 07), `Z` = Height (vertical up from ground `Z = 0`).
> - **Unity Prefabs (`Assets/_Project/Generated/Art/`)**: `X` = Width, `Y` = Height (vertical up), `Z` = Length/Depth (`+Z` forward). All tables below list dimensions in unified **`Width (X) × Length (Y_bl / Z_u) × Height (Z_bl / Y_u)`** order in metres.
> - **Visual Sheets**: `Logs/scale/lineup.png`, `Logs/scale/07_vs_world.png`, `Logs/scale/07_ortho_dimensions.png` (`07_front.png`, `07_side.png`, `07_top.png`).

---

## 1. Executive Summary — Scale Inconsistency Audit & `[PROPOSAL]` List

Across all **39 R1 FBX models** and **116 procedural prefabs** (155 total generated prefabs), we audited every doorway, bay, pad, human-scale prop, rover-built structure, crate, and rock against two primary anchors: **Rover 07 (`1.4726 m W × 2.0143 m L × 1.5902 m H`)** and a **1.750 m Human Astronaut**.

### Summary of Findings & Proposed Resizes (`[PROPOSAL]`)

1. **`[PROPOSAL]` `ChargingPad.fbx` — 3 Critical Collisions with Rover 07 (Cannot Park Without Clipping)**:
   - **Finding 1a (Wheel-Guide Rail Collision)**: The raised steel/rubber guide rails (`GuideRail_L/R` at `X = ±0.680 m`, inner faces at `±0.629 m`) leave only **`1.2580 m`** clear width between rails — **`0.2146 m` (21.5 cm) narrower** than 07's **`1.4726 m`** outer tyre span (`X = ±0.7363 m`)! When 07 parks on the pad, its `0.2566 m`-wide tyres sit directly on top of and clip through the raised `0.15 m` guide rails. Furthermore, the recessed rubber wheel lanes (`wheel_lane_x = ±0.48 m`, width `0.28 m`) were built for a `0.96 m` track rather than 07's `1.220 m` track (`±0.610 m`).
   - **Finding 1b (Entry Ramps Too Narrow)**: The front entry ramp (`ramp_width_front = 0.980 m`, `X = ±0.490 m`) and side ramps (`0.900 m`) are narrower than 07's `1.220 m` hub-to-hub wheel track (`1.4726 m` outer tyre width) — 07's wheels completely miss the front ramp when driving onto the pad!
   - **Finding 1c (Deck Length / Rear Bollard Collision)**: From the rear bollard bezel (`Y = -0.751 m`) to the front deck edge (`Y = +0.917 m`), usable flat deck length is only **`1.668 m`**. When 07 centers over `ContactShoes` (`Y = +0.04 m`), 07's rear tyres (`Y = -0.839 m`) and rear bumper (`Y = -0.824 m`) **penetrate `0.088 m` into the rear totem bollard**.
   - **`[PROPOSAL]` (`ChargingPad.fbx` / `tools/art/blender/charging_pad.py`)**:
     - Move `wheel_lane_x` from `±0.48 m` to **`±0.610 m`** (`1.27×` wider track) and widen wheel lanes from `0.28 m` to **`0.34 m`**.
     - Move `guide_rail_x` outward from `±0.680 m` (`1.258 m` clear) to **`±0.860 m`** (**`1.620 m` clear width**, giving `+0.074 m` clearance per side outside 07's `1.473 m` tyres).
     - Widen `ramp_width_front` by **`1.63×`** (`0.980 m -> 1.600 m`) so both tyres ride centered up the ramp.
     - Lengthen the pad deck by **`+0.54 m`** (`overall Y: 2.661 m -> 3.200 m`, moving rear bollard front face from `Y = -0.751 m` to `Y = -1.100 m`) so 07 (`1.759 m` wheel span, `2.014 m` total length) parks with `+0.22 m` rear wheel clearance.

2. **`[PROPOSAL]` `KenjiWorkshop.fbx` — Overhead Hoist Rail & Hanging Lamp Collide with 07's Head**:
   - **Finding**: The front Quonset doorway frame has a generous **`3.240 m` clear width** (`X = ±1.620 m`) and **`1.935 m` lintel height** (`1.855 m` above the `0.080 m` concrete floor slab), which clears 07 (`1.473 m W × 1.591 m H` on ground, or `1.671 m` on the `0.08 m` slab). **However**, two interior ceiling props hang directly inside the doorway path (`Y = -1.73..-1.39 m`):
     - `HoistRail` (and its chain hook at `X = -0.10 m, Y = -1.62 m`) hangs down to **`Z = 1.2680 m`** (`1.188 m` above the floor slab) — colliding with 07's `1.525 m` Head (`1.605 m` on slab) by **`-0.337 m`** (`-0.403 m` vs 07's `1.591 m` antenna)!
     - `HangingLamp` (`X = +0.71..+1.05 m, Y = -1.73..-1.39 m`) hangs down to **`Z = 1.3500 m`** (`1.270 m` above slab) — **`0.255 m` below 07's Head** on the slab!
   - **`[PROPOSAL]` (`KenjiWorkshop.fbx` / `tools/art/blender/ruins/kenji_workshop.py`)**:
     - Shorten the `HoistRail` chain drop by **`-0.48 m`** (raising hook bottom from `Z = 1.268 m` to **`Z = 1.750 m`**, or slide the hoist trolley to `X = -1.25 m` off the centreline).
     - Shorten the `HangingLamp` cord by **`-0.40 m`** (raising shade bottom from `Z = 1.350 m` to **`Z = 1.750 m`**) so 07 has ≥ `0.08 m` vertical clearance even when driving onto the `0.08 m` floor slab.

3. **`[PROPOSAL]` `UpgradeBay.fbx` — Narrow Guide-Rail Troughs & Rest-Pose Wrench Arms Collide with 07**:
   - **Finding 3a (Wheel Guide Rails)**: `GuideRail_L/R` are centered at `X = ±0.720 m` (`1.440 m` center track vs 07's `1.220 m` hub track at `±0.610 m`), and the U-channel trough between each rail's raised `0.036 m` steel curbs is only **`0.056 m` (5.6 cm) wide** (`X = ±0.692..±0.748 m`), whereas 07's tyres are **`0.2566 m` (25.7 cm) wide** (`X = ±0.480..±0.736 m`). 07's tyres cannot sit in the troughs and ride on top of the inner steel curb flanges.
   - **Finding 3b (Wrench Arms in Rest Pose)**: In the exported FBX pose (`pose_upgrade_bay_rest`), `WrenchArm_L` reaches inward to `X = +0.3256 m` (`Z = 0.98..1.59 m`) and `WrenchArm_R` reaches inward to `X = -0.1850 m` (`Z = 1.03..1.59 m`), leaving only **`0.5106 m` horizontal clearance** at head height (`Z = 1.01..1.525 m`), colliding with 07's `0.770 m`-wide `Head` (`X = ±0.385 m`) by **`-0.259 m`**!
   - **`[PROPOSAL]` (`UpgradeBay.fbx` / `tools/art/blender/upgrade_bay.py`)**:
     - Recenter `GuideRail_L/R` from `X = ±0.720 m` to **`X = ±0.610 m`** (07's exact wheel track) and widen each rail's inner trough by **`5.7×`** (`0.056 m -> 0.320 m` clear between curbs, outer rail width `0.40 m`) so 07's `0.257 m` tyres nest inside the guide channels with `3.1 cm` play per side.
     - Rotate `WrenchArm_L/R` rest pose outward so their tips sit at **`X = ±0.60 m`** (`1.20 m` clear horizontal gap vs `0.51 m` today), clearing 07's `0.770 m` Head by `+0.215 m` per side until the active upgrade animation triggers.

4. **`[PROPOSAL]` `VehiclePad.fbx` — Narrow Front Ramp Lip & Low Overhead Hoist Spreader**:
   - **Finding 4a (`RampLip` Width)**: The front entry `RampLip` has an outer width of `1.595 m`, a tread width of `1.440 m`, and raised cream side curbs whose inner faces sit at `X = ±0.7025 m` (**`1.4050 m` clear width**) — **`0.0676 m` (6.8 cm) narrower** than 07's `1.4726 m` outer tyre span!
   - **Finding 4b (`HoistHook` Spreader Height)**: The overhead crane's 4-corner `HoistHook` spreader frame (`X = -0.356..+0.476 m, Y = -0.266..+0.306 m`) hangs down to **`Z = 1.2325 m`** over the center of the pad (`1.0725 m` above the `0.16 m` pad deck) — colliding with 07's `1.525 m` Head (`1.685 m` on deck) by **`-0.453 m`**!
   - **`[PROPOSAL]` (`VehiclePad.fbx` / `tools/art/blender/vehicle_pad.py`)**:
     - Scale `RampLip` width by **`1.25×`** (`1.405 m` clear -> **`1.760 m` clear**, `1.950 m` outer width) so 07's `1.473 m` tyres have `+0.144 m` margin per side.
     - Raise `HoistHook` rest Z by **`+0.55 m`** (from `Z = 1.2325 m` to **`Z = 1.780 m`**, or raise `CraneBoom` mast height by `+0.40 m` from `2.285 m` to `2.685 m`) so 07 can drive onto the `0.16 m` deck (`top Z = 1.751 m`) without its head hitting the clamp jaws.

5. **`[PROPOSAL]` `HabitatModules.fbx` & `KenjiWorkshop.fbx` & Procedural Crew Props — Human-Scale Mismatches vs 1.75 m Crew**:
   - **Finding 5a (`HabitatModules.fbx` Airlock Door)**: On `ModuleA_Main`, the front airlock door outer frame is `1.22 m W × 1.48 m H` and the inner door leaf opening is **`0.96 m W × 1.32 m H`** (threshold at `Z = 0.12 m`, lintel at `Z = 1.44 m`), with the keypad centered at `Z = 0.84 m` (hip height). A `1.75 m` human (`1.82 m` in an EVA helmet) is **`0.43 m` (25%) taller than the door opening** and would have to stoop deeply to enter!
   - **Finding 5b (`KenjiWorkshop.fbx` Storage Cabinet)**: Inside `WorkbenchAndRacks`, the upright storage cabinet at `X = +1.36 m` is only **`1.16 m` tall** (`0.70 m W × 0.74 m D × 1.16 m H` above the `0.08 m` slab) — chest-high on a `1.75 m` human rather than a full `1.80–2.00 m` workshop locker.
   - **Finding 5c (Procedural `GarageSite.cs` & `GaragePort.prefab` Doors)**: `GarageSite.cs` crew door leaf is `0.74 m W × 1.80 m H` (barely 5 cm above a bare `1.75 m` human, too tight for an EVA suit), and the roll-up rover door lintel is at `Z = 1.600 m` (`GaragePort.prefab` total height `1.680 m`), leaving only **`0.009 m` (9 mm)** vertical clearance for R1 07 (`1.591 m`) and colliding by `-0.045 m` with procedural `RoverModel.prefab` (`1.645 m`).
   - **Finding 5d (Procedural `RoverBayMeshes.cs` Mug)**: `MugProfile` has radius `0.055 m` (**`0.110 m` diameter × `0.110 m` height** = ~1.05 L volume), ~`1.3×` oversized compared to a canonical `0.085 m Ø × 0.095 m H` mug.
   - **`[PROPOSAL]`**:
     - `HabitatModules.fbx`: Scale `ModuleA_Main` airlock door assembly by **`1.15× W × 1.55× H`** (inner leaf `0.96×1.32 m -> 1.10×2.05 m`, outer frame `1.22×1.48 m -> 1.40×2.22 m`) and raise `AirlockPanel` keypad by **`+0.41 m`** (`Z: 0.84 m -> 1.25 m`).
     - `KenjiWorkshop.fbx`: Scale `WorkbenchAndRacks` right-hand cabinet height by **`1.55×`** (`1.16 m -> 1.80 m`) and raise workbench top by **`+0.08 m`** (`0.795 m -> 0.880 m` above slab).
     - `GaragePort.prefab` / `GarageSite.cs`: Scale `GaragePort` height by **`1.25×`** (`1.68 m -> 2.10 m`, rover door clear opening `2.20×1.60 m -> 2.20×2.00 m`) and crew door to **`0.90 m W × 2.05 m H`**.
     - `RoverBayMeshes.cs` (`MugProfile`): Scale mug by **`0.78×`** (`0.110 m Ø × 0.110 m H -> 0.085 m Ø × 0.095 m H`).

6. **`[PROPOSAL]` `CargoLander.fbx` — Tight Internal Fore-Aft Rover Bay Depth**:
   - **Finding**: The rover bay portal (`1.94 m W × 1.96 m H`, sill at `Z = 0.48 m`, lintel at `Z = 2.44 m`) and `RampDoor` (`1.92 m W × 1.96 m L`, `13.6°` slope) give 07 (`1.473 m W × 1.591 m H`) `+0.223 m` side clearance per side and `+0.369 m` overhead clearance. However, the internal bay depth from `Bay_Connector` (`Y = -0.49 m`) to the ramp hinge (`Y = +1.66 m`) is **`2.150 m`**, leaving only **`0.136 m` total fore-aft clearance** (`6.8 cm` front/back) for 07's `2.014 m` overall length (`1.759 m` wheel span)!
   - **`[PROPOSAL]` (`CargoLander.fbx` / `tools/art/blender/cargo_lander.py`)**: Shift the internal rear bay bulkhead (`Bay_Connector`) back by **`-0.35 m`** (`Y = -0.49 m -> -0.84 m`, increasing internal bay depth from `2.15 m` to **`2.50 m`**) so 07 fits inside with `0.24 m` fore-aft margin.

7. **`[PROPOSAL]` `Smelter.fbx`, `StorageRack.fbx`, `PowerPylon.fbx`, `BatteryBank.fbx` — Rover Interaction Reach Mismatches**:
   - **Finding 7a (`Smelter.fbx` Ore Hopper)**: `Hopper` / `HopperDoor` feed mouth sits at **`Z = 0.85..1.30 m`** (`HopperDoor` pivot at `Z = 0.65 m`), whereas 07's `ScoopBucket` operates at **`Z = 0.12..0.34 m`** (`DigScoop` pivot `Z = 0.35 m`) and `CutterOrigin` is at `Z = 0.73 m` — 07 cannot lift ore to `0.85–1.30 m` without a ramp or lower chute!
   - **Finding 7b (`StorageRack.fbx` Upper Tier)**: Lower cradles (`Cradle_1..3`) pivot at `Z = 0.22 m` (`Z = 0.22..0.52 m`, matching 07's `0.14–0.48 m` scoop/chute band), but upper cradles (`Cradle_4..6`) pivot at **`Z = 0.90 m`** (`Z = 0.90..1.14 m`, `PipLamp_4..6` at `1.53 m`), which sits above 07's `0.73 m` cutter and `0.80 m` `CargoHatch`.
   - **Finding 7c (`PowerPylon.fbx` KnifeSwitch)**: `CableDrum_1..3` (`Z = 0.36 m`) and `Socket_Power_1..3` (`Z = 0.30 m`) match 07's `0.38 m` winch/tow height, but `KnifeSwitch` sits at **`Z = 1.22..1.39 m`** (`+0.57 m` above 07's `0.73 m` cutter/manipulator reach).
   - **Finding 7d (`BatteryBank.fbx` Power Sockets)**: `Socket_Power_1/2` sit at **`Z = 0.90 m` and `1.01 m`**, whereas every other structure places `Socket_Power_*` at **`Z = 0.08–0.45 m`** (`UpgradeBay` `0.08 m`, `VehiclePad` `0.16 m`, `ChargingPad` `0.20 m`, `StorageRack` `0.22 m`, `PowerPylon` `0.30 m`, `SolarArray` `0.30 m`, `Fabricator` `0.31/0.45 m`, `Smelter` `0.42 m`, `AntennaMast` `0.44 m`).
   - **`[PROPOSAL]`**:
     - `Smelter.fbx`: Lower `Hopper` / `HopperDoor` by **`-0.50 m`** (`hinge Z: 0.65 -> 0.25 m`, mouth `Z: 0.35..0.80 m`) or add a `0.55 m`-high front approach ramp.
     - `StorageRack.fbx`: Scale rack height by **`0.86×`** (`1.62 m -> 1.40 m`) and lower `Cradle_4..6` pivot from `Z = 0.90 m` to **`Z = 0.68 m`** so both tiers fall within 07's `0.22–0.73 m` reach envelope.
     - `PowerPylon.fbx`: Lower `KnifeSwitch` by **`-0.55 m`** (`Z: 1.30 m -> 0.75 m`).
     - `BatteryBank.fbx`: Lower `Socket_Power_1/2` by **`-0.55 m`** (`Z: 0.90/1.01 m -> 0.35 m`).

8. **`[PROPOSAL]` Crates & Rocks — Cross-Builder Scale Divergence**:
   - **Finding 8a (Crates Span a 4.5× Range Across Builders)**:
     - `SupplyShelfMeshes.cs` `ShelfCrate_*`: `0.300 × 0.349 × 0.124 m` (shelf tray)
     - `StorageRack.fbx` `Cradle_1 / Cradle_3` crates: `0.310 × 0.240 × 0.220 m` and `0.340 × 0.260 × 0.240 m` (rover rack crate)
     - `LootContainerMeshes.cs` `SpareCrate`: `0.412 × 0.199 × 0.308 m`; `LogCache`: `0.524 × 0.413 × 0.328 m`
     - `CargoLander.fbx` `Crate` (+ `CrateLid`): `0.812 × 0.566 × 0.510 m`
     - `LootContainerMeshes.cs` `CoilCrate`: `0.824 × 0.539 × 0.480 m`
     - `ScatterSet.fbx` `SealedCrates`: 3-crate cluster (`1.420 × 1.120 × 1.315 m`) whose primary crate is **`0.920 × 0.680 × 0.640 m`** (`2.7×` wider and **`19.5×` larger by volume** than the `0.34 × 0.26 × 0.24 m` R1 supply crate inside `StorageRack.fbx`, and too wide to fit inside 07's `0.58 m` `CargoHatch` or `StorageRack`'s `0.46 m` bays!).
   - **Finding 8b (R1 `Rock_00..09` vs Procedural `Rock_00..05` Compression)**:
     - Procedural `Generated/Art/Rocks/Rock_00..05` span a clean 10× progression from `0.35 m` (`Rock_00`: `0.35 × 0.32 × 0.21 m`, drive-over cobble below 07's `0.335 m` belly) to `3.40 m` (`Rock_05`: `3.40 × 3.22 × 2.71 m`, large landmark boulder).
     - R1 `Art/R1/Models/Rocks/Rock_00..04` (intended as the small cobble tier in `rocks.py`) were exported at **`0.94–1.07 m W × 0.79–0.87 m L × 0.46–0.62 m H`** — nearly `1 m` wide boulders that exceed 07's `0.335 m` belly clearance! Meanwhile R1 `Rock_05..09` are **`1.47–1.55 m W × 1.22–1.39 m L × 0.82–1.02 m H`** — only `1.5×` wider than `Rock_00..04`, leaving R1 with **zero small cobbles (`0.3–0.45 m`)** and **zero large boulders (`2.4–3.4 m`)**.
   - **Finding 8c (Legacy Procedural `Generated/Art/Rover/` vs R1 `Art/R1/Models/Rover/`)**:
     - Procedural `Generated/Art/Rover/RoverModel.prefab` is `1.630 × 2.273 × 1.645 m` (`1.11×` wider, `1.13×` longer than R1 `RoverModel.fbx`), and procedural kits in `Generated/Art/Rover/` diverge up to `3.9×` from R1 kits (`Kit_LampBar` procedural `1.212 × 0.283 × 0.750 m` vs R1 `0.767 × 0.269 × 0.190 m`; `FocusLens` procedural `0.343 × 0.079 × 0.341 m` vs R1 `0.116 × 0.101 × 0.135 m`).
   - **`[PROPOSAL]`**:
     - Standardize crates into **3 canonical tiers**:
       1. **Tier S (Rack / Rover Cargo Crate)**: `0.36 m W × 0.28 m L × 0.24 m H` (fits 2-abreast inside 07's `0.58 m` `CargoHatch` and inside `StorageRack.fbx` `0.46 m` cradles).
       2. **Tier M (Field Loot / Salvage Crate)**: `0.60 m W × 0.44 m L × 0.38 m H` (scale `ScatterSet.fbx` `SealedCrates` by **`0.70×`** from `0.92×0.68×0.64 m` to `0.64×0.48×0.45 m`).
       3. **Tier L (Heavy Lander / Depot Pallet Crate)**: `0.82 m W × 0.56 m L × 0.50 m H` (`CargoLander.fbx` `Crate` and `CoilCrate`).
     - Re-tier R1 `Rocks/Rock_00..09.fbx` in `tools/art/blender/rocks.py`:
       - Scale `Rock_00..04` by **`0.38×–0.45×`** (`-> 0.35–0.45 m W × 0.17–0.24 m H`, drive-over cobbles).
       - Scale `Rock_05..06` by **`0.75×–0.80×`** (`-> 1.15–1.22 m W`, medium rocks), keep `Rock_07` at `1.55 m W`, and scale `Rock_08..09` by **`1.65×–2.00×`** (`-> 2.43–3.02 m W`, large boulders).

---

## 2. Canonical Reference Scale Anchors

### 2.1 Human Astronaut Reference (`1.750 m` Bare / `1.820 m` Suited)

| Feature / Ergonomic Anchor | Canonical Dimension | Notes / Usage |
| :--- | :--- | :--- |
| **Standing Height (Bare Crew)** | **1.750 m** | Primary human silhouette reference in `Logs/scale/lineup.png` and `07_vs_world.png` |
| **Standing Height (EVA Helmet)** | **1.820 m** | Minimum vertical clearance baseline for exterior airlocks and hatches |
| **Eye Level** | **1.630 m** | Horizon / status monitor / window center height |
| **Shoulder Height & Width** | **1.450 m H × 0.460 m W** | `0.620 m W` across EVA suit arms |
| **Waist / Wrist / Counter Reach** | **0.900–0.920 m H** | Standard standing workbench & lab counter height |
| **Crew Doorway (Interior / Garage)** | **0.900 m W × 2.050 m H** | Clear opening (`1.00 m × 2.15 m` outer frame) |
| **Crew Airlock Door (Exterior EVA)** | **1.100 m W × 2.050 m H** | Clear opening (`1.36 m × 2.20 m` outer pressure frame) |
| **Wall Keypad / Switch / Plaque** | **1.200–1.350 m H** | Center height above floor |
| **Standing Workbench / Desk** | **1.600 m W × 0.800 m D × 0.900 m H** | Seated desk top: `0.750 m H`; standing chart/light table: `1.050 m H` |
| **Crew Chair / Lab Stool** | **0.480 m W × 0.480 m D × 0.840 m H** | Seat pan height: **0.460 m** |
| **Crew Storage Cabinet / Locker** | **0.800 m W × 0.600 m D × 1.900 m H** | Full-height crew equipment locker |
| **Crew Ladder** | **0.480 m W**, **0.300 m** rung pitch | Rail Ø `0.032 m`, rung Ø `0.024 m` |
| **Crew Mug** | **0.085 m Ø × 0.095 m H** | Standard ~350 ml insulated crew mug |

### 2.2 Rover 07 (`Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx`)

| Measurement / Sub-Feature | Node(s) | Measured Value | Coordinate Span (Blender `X` W, `Y` L, `Z` H) | Canonical Spec |
| :--- | :--- | :--- | :--- | :--- |
| **Overall Bounding Box** | All 58 meshes | **1.4726 m W × 2.0143 m L × 1.5902 m H** | `X: [-0.7363..+0.7363]`, `Y: [-0.8793..+1.1350]`, `Z: [0.0007..1.5909]` | `1.47 m × 2.01 m × 1.59 m` |
| **Wheel Footprint (No Scoop)** | `Wheel_FL..RR` | **1.4726 m W × 1.7586 m L** | `X: [-0.7363..+0.7363]`, `Y: [-0.8793..+0.8793]` | `1.47 m × 1.76 m` |
| **Chassis Tub (`Body`)** | `Body` | **0.9600 m W × 1.6510 m L × 0.6325 m H** | `X: [-0.4800..+0.4800]`, `Y: [-0.8240..+0.8270]`, `Z: [0.3350..0.9675]` | `0.96 m × 1.65 m × 0.63 m` |
| **Height to Top of Head** | `Head` | **1.5250 m** | `Head` span `X: [-0.3850..+0.3850]`, `Y: [-0.2114..+0.4550]`, `Z: [1.0100..1.5250]` | `1.525 m` (`Head` `0.77 × 0.67 × 0.52 m`) |
| **Height to Antenna Tip** | `AntennaTip` | **1.5909 m** | Pivot `(0.36, -0.46, 1.57)`, mesh top `Z = 1.5909 m` (`Antenna` rod top `1.5800 m`) | `1.591 m` |
| **Wheel Diameter & Width** | `Wheel_FL..RR` | **Ø 0.6986 m × 0.2566 m W** | `Z: [0.0007..0.6993]`, hub `Z = 0.3500 m` | `Ø 0.70 m × 0.26 m W` |
| **Wheel Track (Hub Centers)** | `Wheel_FL / FR` | **1.2200 m** | Hubs at `X = ±0.6100 m` (`1.4726 m` outer, `0.9594 m` inner clear between tyres) | `1.220 m` |
| **Wheelbase (Hub Centers)** | `Wheel_FL / RL` | **1.0600 m** | Hubs at `Y = ±0.5300 m` (`1.7586 m` outer front-to-rear tyre span) | `1.060 m` |
| **Ground Clearance (Belly)** | `Body` / `Arm_*` | **0.3350 m** | `Body` min `Z = 0.3350 m`; `Arm_*` min `Z = 0.3358 m`; `WinchSocket` min `Z = 0.3125 m` | `0.335 m` (`0.122 m` at front `ScoopBucket`) |
| **CRT Screen** | `Screen` | **0.5800 m W × 0.3900 m H** | `X: [-0.2900..+0.2900]`, `Y: [0.4050..0.4430]`, `Z: [1.0650..1.4550]`, pivot `Z = 1.260 m` | `0.58 m × 0.39 m` (center `Z = 1.26 m`) |
| **Scan Lens (`Eye`)** | `Eye` / `EyeOrigin` | **Ø 0.2448 m** at **Z = 1.2300 m** | Pivot `(-0.365, 0.040, 1.230)`, `Z: [1.1076..1.3524]`, `LensGlass` Ø `0.180 m` | `Z = 1.230 m`, Ø `0.245 m` |
| **Front Dig Scoop** | `DigScoop` / `ScoopBucket` | **0.6915 m W × 0.4830 m L × 0.2199 m H** | `X: [-0.3458..+0.3457]`, `Y: [0.6520..1.1350]`, `Z: [0.1219..0.3418]`, `ScoopOrigin` `Z = 0.14 m` | Work band `Z = 0.12–0.35 m` |
| **Front Cutter Turret** | `CutterTurret` / `CutterOrigin` | **0.1750 m W × 0.2624 m L × 0.1552 m H** | Pivot `(0.0, 0.72, 0.73)`, mesh `Z: [0.6574..0.8126]` | Work height `Z = 0.730 m` |
| **Rear Cargo Hold & Hatch** | `CargoHatch` / `LowerChute` | **0.5800 m W × 0.3550 m L × 0.2300 m H** | `CargoSocket` / `LowerChute` at `Z = 0.480 m`, `CargoHold` `Z = 0.640 m`, `CargoHatch` `Z = 0.800 m` | Loading band `Z = 0.48–0.80 m` |
| **Rear Winch & Tow Hook** | `WinchSocket` / `TowHook` | **0.2850 m W × 0.1550 m L × 0.1350 m H** | `WinchSocket` `(0.0, -0.60, 0.38)`, `TowHook` / `WinchOrigin` `(0.0, -0.72, 0.38)` | Cable/tow height `Z = 0.380 m` |
| **Solar Wing** | `SolarWing` | **0.4000 m W × 0.2300 m L × 0.0238 m H** | Pivot `(0.0, -0.394, 0.948)`, `Z: [0.9401..0.9639]` | Deck height `Z = 0.948 m` |

---

## 3. Detailed 5-Category Scale Consistency Analysis

### 3.1 Question 1: Does 07 Fit Through Every Door and Bay?

Rover 07 requires a minimum envelope of **`1.4726 m` W × `2.0143 m` L (`1.7586 m` wheel span) × `1.5902 m` H (`1.5250 m` head)**. With a safe driving margin (`+0.20 m` per side, `+0.15 m` overhead), rover portals should provide at least **`1.88 m` W × `1.75 m` H** clear opening.

| Structure / Portal | Portal / Opening Dimensions | Clear Width vs 07 (`1.473 m`) | Clear Height vs 07 (`1.591 m`) | Verdict & Exact Interference |
| :--- | :--- | :--- | :--- | :--- |
| **`KenjiWorkshop.fbx`** (Outer Frame) | `3.240 m W × 1.855 m H` (above `0.08 m` slab; lintel `Z = 1.935 m`) | `+1.767 m` (`+0.884 m`/side) | `+0.264 m` (on `0.08 m` slab) | **Outer frame PASS** |
| **`KenjiWorkshop.fbx`** (`HoistRail` & `HangingLamp`) | `HoistRail` at `Z = 1.268 m` (`1.188 m` above slab); `HangingLamp` at `Z = 1.350 m` | Centered in doorway (`X = -0.10 m` & `+0.88 m`) | **`-0.323 m` (ground) / `-0.403 m` (slab)** | **FAIL (`[PROPOSAL]`)** — Overhead hoist hook & lamp hang below 07's `1.525 m` Head and `1.591 m` Antenna. |
| **`CargoLander.fbx`** (Rover Bay & `RampDoor`) | Bay: `1.940 m W × 1.960 m H` (`Z = 0.48..2.44 m`); Ramp: `1.920 m W × 1.960 m L` (`13.6°`); Internal depth: `2.150 m` (`Y = -0.49..+1.66 m`) | `+0.447 m` (`+0.224 m`/side on ramp) | `+0.369 m` | **Width/Height PASS; Depth TIGHT (`[PROPOSAL]`)** — `2.150 m` internal bay depth vs `2.014 m` 07 length leaves only `0.136 m` total fore-aft margin. |
| **`UpgradeBay.fbx`** (Portal, `GuideRail_L/R`, `WrenchArm_L/R`) | Columns: `2.120 m W`; Bridge: `2.420 m H`; `HoistClaw` rest `Z = 1.748 m`; `GuideRail` trough: **`0.056 m W`** at `X = ±0.720 m`; `WrenchArm` rest gap: **`0.511 m W`** at `Z = 0.98..1.59 m` | Portal: `+0.647 m`; **WrenchArms: `-0.259 m` vs `0.77 m` Head**; **Rails: `0.056 m` trough vs `0.257 m` tyre** | `+0.157 m` under `HoistClaw` (`Z = 1.748 m`) | **FAIL (`[PROPOSAL]`)** — Tyres ride on top of `0.056 m` guide-rail curbs (`±0.72 m` vs `±0.61 m` track) and rest-pose `WrenchArm_L/R` collide with 07's Head. |
| **`HabitatModules.fbx`** (`ModuleA_Main` Airlock Door) | Inner door leaf: `0.960 m W × 1.320 m H` (`Z = 0.12..1.44 m`); Outer frame: `1.220 m W × 1.480 m H` | **`-0.513 m`** (and crew-only, not rover door) | **`-0.271 m` vs 07; `-0.430 m` vs `1.75 m` Human!** | **FAIL (`[PROPOSAL]`)** — Too small for both 07 and a `1.75 m` human crew member. |
| **Procedural `GaragePort.prefab` / `Site_garage.prefab`** | Rover roll-up door opening: `2.200 m W × 1.600 m H` (`GaragePort` total height `1.680 m`) | `+0.727 m` (`+0.364 m`/side) | **`+0.009 m` (9 mm!) vs R1 07 (`-0.045 m` vs proc 07)** | **TIGHT/FAIL (`[PROPOSAL]`)** — 9 mm overhead clearance leaves zero suspension bounce margin. |
| **Procedural `RoverBay.prefab`** (`Shutter`) | `Shutter` opening: `3.180 m W × 3.250 m H`; `Turntable` Ø `3.536 m` | `+1.707 m` | `+1.659 m` | **PASS** |

### 3.2 Question 2: Are the ChargingPad and VehiclePad Big Enough for 07 to Park On?

| Pad Model | Overall Bounds (`W × L × H`) | Deck / Usable Parking Area | Entry Ramp Clear Width vs 07 (`1.473 m`) | Overhead / Lateral Obstructions | Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`ChargingPad.fbx`** | `2.6150 × 2.6613 × 1.5500 m` | Guide rails limit clear width to **`1.2580 m`** (`X = ±0.629 m`); usable flat length is **`1.668 m`** (`Y = -0.751..+0.917 m`); wheel lanes at `X = ±0.48 m` (`0.96 m` track) | Front ramp: **`0.980 m`** (`-0.493 m` narrower than 07's tyres!); Side ramps: **`0.900 m`** | Guide rails (`X = ±0.629..±0.728 m`) sit directly inside 07's tyres (`X = ±0.480..±0.736 m`); rear bollard (`Y = -0.751 m`) hits 07's rear tyres (`Y = -0.839 m`) | **FAIL (`[PROPOSAL]`)** — 3 direct geometric collisions (guide rails, front ramp, rear bollard). |
| **`VehiclePad.fbx`** | `3.7605 × 3.3889 × 2.2851 m` | Inner octagonal curb: `2.560 m W × 2.160 m L` (`1.968 m` L at `X = ±0.736 m`, giving `+0.105 m` front/rear wheel margin) | `RampLip` clear width between side curbs: **`1.4050 m`** (`-0.068 m` narrower than 07's `1.473 m` tyres!) | `HoistHook` spreader hangs at **`Z = 1.2325 m`** over pad center (`1.073 m` above `0.16 m` deck), colliding with 07's `1.525 m` Head by `-0.453 m` | **FAIL (`[PROPOSAL]`)** — Widen `RampLip` (`1.25×`) and raise `HoistHook` (`+0.55 m`). |
| **Procedural `StationPad_160.prefab`** | `3.3175 × 3.3193 × 0.0310 m` | `2.76 m` inscribed between `PadLamp_0..5` (`r = 1.38 m`) | Flush `0.02 m` pad (360° entry) | None | **PASS** |
| **Procedural `StationPad_240.prefab`** | `4.9173 × 4.9626 × 0.0310 m` | `4.36 m` inscribed between `PadLamp_0..5` (`r = 2.18 m`) | Flush `0.02 m` pad (360° entry) | None | **PASS** |
| **Procedural `StationPad_300.prefab`** | `6.2020 × 6.1161 × 0.0310 m` | `5.56 m` inscribed between `PadLamp_0..5` (`r = 2.78 m`) | Flush `0.02 m` pad (360° entry) | None | **PASS** |

### 3.3 Question 3: Are Human-Scale Props Consistent with a 1.75 m Human?

| Prop / Feature | Source Model / Script | Measured Dimensions (`W × L × H`) | Comparison vs `1.750 m` Human Ergonomics | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| **Habitat Airlock Door** | `HabitatModules.fbx` (`ModuleA_Main`) | Leaf: **`0.96 × 1.32 m`** (Z `0.12..1.44 m`); Frame: `1.22 × 1.48 m`; Keypad: `Z = 0.84 m` | **25% (`0.43 m`) shorter** than a `1.75 m` human (`0.50 m` shorter than `1.82 m` EVA helmet); keypad at hip height | **FAIL (`[PROPOSAL]`)** — Scale door `1.15× W × 1.55× H` (`-> 1.10 × 2.05 m`), keypad `Z -> 1.25 m`. |
| **Workshop Workbench** | `KenjiWorkshop.fbx` (`WorkbenchAndRacks`) | `1.62 m W × 0.78 m D × 0.795 m H` (`Z = 0.875 m` world / `0.795 m` above `0.08 m` slab) | Worktop is `0.105 m` lower than canonical `0.90 m` standing bench (reads as seated desk height) | **TIGHT (`[PROPOSAL]`)** — Raise worktop `+0.085 m` to `0.88 m` above slab. |
| **Workshop Storage Cabinet** | `KenjiWorkshop.fbx` (`WorkbenchAndRacks`) | `0.70 m W × 0.74 m D × 1.16 m H` (`Z = 1.24 m` world / `1.16 m` above slab) | Only chest-high (`66%` of human height) instead of a `1.80 m` standing locker | **FAIL (`[PROPOSAL]`)** — Scale cabinet height `1.55×` (`1.16 -> 1.80 m`). |
| **Garage Crew Door** | `GarageSite.cs` (`Site_garage.prefab`) | Leaf: `0.74 m W × 1.80 m H`; Frame: `0.84 × 1.90 m` | Barely clears `1.75 m` bare head by `5 cm`; too narrow/low for `1.82 m` EVA suit | **TIGHT (`[PROPOSAL]`)** — Resize to `0.90 m W × 2.05 m H`. |
| **Crew Ladder** | `LanderMeshes.cs` (`Lander.prefab`) | `0.48 m W`, `0.30 m` rung spacing | Matches standard `0.48 m / 0.30 m` human ladder ergonomics | **PASS** |
| **Lab Light / Chart Table** | `LabStationMeshes.cs` (`Lander.prefab`) | `1.64 m W × 1.14 m D × 1.05 m H` (above lab floor) | Ideal standing chart/drafting table height (`1.05 m`) for `1.75 m` human | **PASS** |
| **Workshop Mug** | `RoverBayMeshes.cs` (`MugProfile`) | **`0.110 m Ø × 0.110 m H`** (`r = 0.055 m`) | ~1.05 litre bucket-sized mug (`1.3×` oversized vs `0.085 m Ø × 0.095 m H` mug) | **OVERSIZED (`[PROPOSAL]`)** — Scale `0.78×` to `0.085 m Ø × 0.095 m H`. |
| **Astronaut Boot Relic** | `Relic_astronaut_boot.prefab` | `0.211 m W × 0.369 m L × 0.365 m H` | Matches an oversized EVA lunar overshoe for a `1.75 m` astronaut | **PASS** |

### 3.4 Question 4: Are Rover-Built Structures Consistent with 07's Reach and Height?

Rover 07's physical interaction points sit in three vertical bands:
- **Low Band (`Z = 0.08–0.48 m`)**: `ScoopBucket` (`0.12–0.34 m`), `BellySocket` (`0.28 m`), `WinchSocket`/`TowHook` (`0.38 m`), `CargoSocket`/`LowerChute` (`0.48 m`).
- **Mid Band (`Z = 0.48–0.80 m`)**: `FrontIntake` (`0.50 m`), `CargoHold` (`0.64 m`), `CutterOrigin` (`0.73 m`), `CargoHatch` (`0.80 m`).
- **Upper Sensor Band (`Z = 0.95–1.59 m`)**: `SolarWing` (`0.95 m`), `EyeOrigin` (`1.23 m`), `Screen` (`1.26 m`), `Head` top (`1.525 m`), `AntennaTip` (`1.591 m`).

| Structure (`R1/Models/Structures/`) | Overall Bounds (`W × L × H`) | Power Socket Height(s) | Key Interaction / Mechanism Heights vs 07 | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| **`ChargingPad.fbx`** | `2.615 × 2.661 × 1.550 m` | `Socket_Power_1`: `Z = 0.20 m` | `ContactShoes` at `Z = 0.095..0.155 m` (matches `0.28 m` `BellySocket`); `TotemBar` at `Z = 0.72..1.35 m` (matches `1.23 m` `Eye`); **pad rails/ramp/bollard collide with 07** | **FAIL (`[PROPOSAL]`)** — See §1.1. |
| **`VehiclePad.fbx`** | `3.761 × 3.389 × 2.285 m` | `Socket_Power_1`: `Z = 0.16 m` | Deck at `Z = 0.16 m`; **front `RampLip` (`1.405 m`) too narrow and `HoistHook` (`Z = 1.233 m`) collides with 07's Head** | **FAIL (`[PROPOSAL]`)** — See §1.4. |
| **`UpgradeBay.fbx`** | `3.196 × 2.530 × 2.745 m` | `Socket_Power_1`: `Z = 0.08 m` | `HoistClaw` rest `Z = 1.748 m` (clears `1.591 m` 07); **`GuideRail` troughs (`0.056 m`) and `WrenchArm_L/R` rest pose (`0.511 m` gap at `Z = 0.98..1.59 m`) collide with 07** | **FAIL (`[PROPOSAL]`)** — See §1.3. |
| **`Smelter.fbx`** | `3.267 × 1.859 × 2.380 m` | `Socket_Power_1`: `Z = 0.42 m` | Ingot conveyor discharge at `Z = 0.27 m` (matches low band), `TapSpout` at `Z = 0.56 m`, **but `Hopper` / `HopperDoor` ore feed mouth sits at `Z = 0.85..1.30 m` (`+0.51 m` above 07's `0.34 m` `ScoopBucket`)** | **FAIL (`[PROPOSAL]`)** — Lower hopper `-0.50 m` or add ramp. |
| **`StorageRack.fbx`** | `1.789 × 0.918 × 1.620 m` | `Socket_Power_1`: `Z = 0.22 m` | Lower `Cradle_1..3` at `Z = 0.22..0.52 m` (matches 07 scoop/chute), **but upper `Cradle_4..6` pivot at `Z = 0.90 m` (`Z = 0.90..1.14 m`), above 07's `0.73 m` cutter and `0.80 m` hatch** | **TIGHT (`[PROPOSAL]`)** — Lower upper tier `-0.22 m` (`Z -> 0.68 m`). |
| **`PowerPylon.fbx`** | `0.777 × 0.803 × 1.946 m` | `Socket_Power_1..3`: `Z = 0.30 m` | `CableDrum_1..3` at `Z = 0.36 m` (matches 07 `0.38 m` winch), **but `KnifeSwitch` sits at `Z = 1.22..1.39 m` (`+0.57 m` above 07's `0.73 m` manipulator reach)** | **TIGHT (`[PROPOSAL]`)** — Lower `KnifeSwitch` `-0.55 m` (`Z -> 0.75 m`). |
| **`BatteryBank.fbx`** | `1.462 × 0.987 × 1.598 m` | **`Socket_Power_1/2`: `Z = 0.90 / 1.01 m`** | Cells `Cell_1..8` at `Z = 0.34..1.05 m`; **power sockets at `Z = 0.90–1.01 m` are `0.55 m` higher than the `0.08–0.45 m` socket band on all other structures** | **INCONSISTENT (`[PROPOSAL]`)** — Lower sockets to `Z = 0.35 m`. |
| **`SolarArray.fbx`** | `2.406 × 1.566 × 1.939 m` | `Socket_Power_1`: `Z = 0.30 m` | `BreakerHandle` at `Z = 0.72 m` (matches 07's `0.73 m` `CutterOrigin`!); `YawHead` at `Z = 0.90 m`; `SunSensor` at `Z = 1.73 m` | **PASS** |
| **`Fabricator.fbx`** | `2.212 × 1.561 × 1.840 m` | `Socket_Power_1/2`: `Z = 0.31 / 0.45 m` | Workbed at `Z = 0.82 m` (matches 07's `0.80 m` `CargoHatch`); `PartBayDoor` at `Z = 0.38..0.72 m`; `ToolHead` at `Z = 1.02..1.35 m` | **PASS** |
| **`AntennaMast.fbx`** | `1.553 × 1.265 × 2.797 m` | `Socket_Power_1`: `Z = 0.44 m` | Equipment cabinet `Z = 0.18..1.38 m`, meter face at `Z = 0.65..1.05 m`, dish pivot at `Z = 2.15 m` | **PASS** |

### 3.5 Question 5: Are Crates, Rocks, and Scatter Props Consistent Across Builders?

| Category | Asset / Sub-Mesh | Builder / Source | Measured Size (`W × L × H`) | Consistency Analysis & `[PROPOSAL]` |
| :--- | :--- | :--- | :--- | :--- |
| **Crate (UI Pip)** | `HatchCrate_0..7` | `RoverModel.fbx` | `0.040 × 0.028 × 0.022 m` | Cargo-hold window indicator pips (not world crates). |
| **Crate (Shelf Tray)** | `ShelfCrate_Metal/Optics/OrangeBeads/Wiring` | `SupplyShelfMeshes.cs` | `0.300 × 0.349 × 0.124 m` | Open flat shelf trays for base interior shelves. |
| **Crate (Tier S — Rack)** | `Cradle_1` / `Cradle_3` Crates | `StorageRack.fbx` | `0.310 × 0.240 × 0.220 m` & `0.340 × 0.260 × 0.240 m` | **Canonical Tier S Rover Supply Crate** (`0.36 × 0.28 × 0.24 m`). Fits `StorageRack` cradles (`0.46 m` W) and 07 `CargoHatch` (`0.58 m` W). |
| **Crate (Tier M — Field)** | `SpareCrate` / `TapeCase` / `LogCache` | `LootContainerMeshes.cs` | `0.412×0.199×0.308 m` / `0.384×0.279×0.160 m` / `0.524×0.413×0.328 m` | Field loot containers. |
| **Crate (Tier L — Lander)** | `Crate` (+ `CrateLid`) / `CoilCrate` / `CrewCrate` | `CargoLander.fbx` / `LootContainerMeshes.cs` / `DepotSite.cs` | `0.812×0.566×0.510 m` / `0.824×0.539×0.480 m` / `0.850×0.548×0.360 m` | **Canonical Tier L Heavy Depot Crate** (`0.82 × 0.56 × 0.50 m`). |
| **Crate (Oversized Scatter)** | `SealedCrates` (3-crate cluster) | `ScatterSet.fbx` | Cluster: `1.420 × 1.120 × 1.315 m`; Main crate: **`0.920 × 0.680 × 0.640 m`** | **INCONSISTENT (`[PROPOSAL]`)** — `2.7×` wider (`19.5×` volume) than `StorageRack`'s R1 crate despite identical visual styling! Scale `0.70×` (`-> 0.64 × 0.48 × 0.45 m`). |
| **Rocks (R1 Small Tier)** | `Rock_00..04.fbx` | `tools/art/blender/rocks.py` | **`0.942–1.072 m W × 0.786–0.875 m L × 0.460–0.620 m H`** | **INCONSISTENT (`[PROPOSAL]`)** — Built as `~1.0 m` boulders instead of `0.35–0.45 m` cobbles; taller than 07's `0.335 m` belly! Scale `0.38×–0.45×`. |
| **Rocks (R1 Large Tier)** | `Rock_05..09.fbx` | `tools/art/blender/rocks.py` | **`1.471–1.547 m W × 1.216–1.388 m L × 0.820–1.020 m H`** | **INCONSISTENT (`[PROPOSAL]`)** — Compressed into `1.47–1.55 m` (only `1.5×` wider than `Rock_00..04`). Scale `Rock_05..06` `0.75–0.80×` (`~1.2 m`) and `Rock_08..09` `1.65–2.00×` (`2.4–3.0 m`). |
| **Rocks (Procedural)** | `Generated/Art/Rocks/Rock_00..05.prefab` | `RockModelBuilder.cs` | `0.347×0.317×0.214 m` (`Rock_00`) up to `3.397×3.218×2.707 m` (`Rock_05`) | **PASS** — Clean geometric progression across 6 sizes (`0.35 m`, `0.50 m`, `0.85 m`, `1.29 m`, `2.14 m`, `3.40 m`). |

---

## 4. Complete Measured Inventory — All 39 R1 FBX Models

Every R1 FBX model under `Assets/_Project/Art/R1/Models/` (and its generated prefab under `Assets/_Project/Generated/Art/R1/Models/`) is enforced by `tools/art/qa/model_lint.py` and `Assets/_Project/Scripts/Art/Tests/ScaleContractTests.cs` against its contract JSON in `tools/art/qa/contracts/*.json`.

| Category | Model Name | Contract JSON | Width X (m) | Length Y (m) | Height Z (m) | X Span (m) | Y Span (m) | Z Span (m) | Contract Tol | Triangles | Scale Audit & `[PROPOSAL]` |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| `Lander` | `CargoLander` | `cargo_lander.json` | **13.1200** | **5.2236** | **5.7500** | `[-6.56..6.56]` | `[-2.61..2.61]` | `[-0.00..5.75]` | ±0.250 m | 38,052 | `[PROPOSAL]` Deepen internal rover bay by `+0.35 m` (`Y: -0.84..+1.66 m`, `2.50 m` clear depth vs `2.15 m` today) so 07 (`2.014 m` L) has `0.24 m` fore/aft margin. |
| `Rocks` | `Rock_00` | `rock_00.json` | **0.9680** | **0.8437** | **0.5600** | `[-0.48..0.49]` | `[-0.42..0.42]` | `[-0.10..0.46]` | ±0.048 m | 96 | `[PROPOSAL]` Scale `0.38×` (`0.97×0.84×0.56 m -> 0.37×0.32×0.21 m`) so `Rock_00..04` act as true cobbles/small rocks (`0.30–0.45 m`) below 07's `0.335 m` belly clearance. |
| `Rocks` | `Rock_01` | `rock_01.json` | **1.0140** | **0.7860** | **0.6200** | `[-0.50..0.51]` | `[-0.39..0.40]` | `[-0.11..0.51]` | ±0.051 m | 96 | `[PROPOSAL]` Scale `0.38×` (`1.01×0.79×0.62 m -> 0.39×0.30×0.24 m`). |
| `Rocks` | `Rock_02` | `rock_02.json` | **1.0721** | **0.8749** | **0.4600** | `[-0.53..0.54]` | `[-0.45..0.42]` | `[-0.08..0.38]` | ±0.054 m | 96 | `[PROPOSAL]` Scale `0.38×` (`1.07×0.87×0.46 m -> 0.41×0.33×0.17 m`). |
| `Rocks` | `Rock_03` | `rock_03.json` | **0.9415** | **0.8248** | **0.5800** | `[-0.47..0.47]` | `[-0.42..0.40]` | `[-0.10..0.48]` | ±0.047 m | 96 | `[PROPOSAL]` Scale `0.42×` (`0.94×0.82×0.58 m -> 0.40×0.35×0.24 m`). |
| `Rocks` | `Rock_04` | `rock_04.json` | **0.9682** | **0.7968** | **0.5200** | `[-0.46..0.51]` | `[-0.40..0.40]` | `[-0.09..0.43]` | ±0.048 m | 96 | `[PROPOSAL]` Scale `0.45×` (`0.97×0.80×0.52 m -> 0.44×0.36×0.23 m`). |
| `Rocks` | `Rock_05` | `rock_05.json` | **1.5334** | **1.3233** | **0.8800** | `[-0.78..0.75]` | `[-0.67..0.65]` | `[-0.14..0.74]` | ±0.077 m | 288 | `[PROPOSAL]` Scale `0.75×` (`1.53×1.32×0.88 m -> 1.15×0.99×0.66 m`) for medium boulder tier (`1.0–1.2 m`). |
| `Rocks` | `Rock_06` | `rock_06.json` | **1.5224** | **1.2164** | **1.0200** | `[-0.78..0.74]` | `[-0.55..0.67]` | `[-0.16..0.86]` | ±0.076 m | 284 | `[PROPOSAL]` Scale `0.80×` (`1.52×1.22×1.02 m -> 1.22×0.97×0.82 m`) for medium boulder tier. |
| `Rocks` | `Rock_07` | `rock_07.json` | **1.5465** | **1.3753** | **0.8200** | `[-0.82..0.73]` | `[-0.63..0.74]` | `[-0.13..0.69]` | ±0.077 m | 288 | Pass as medium-large boulder (`1.55 × 1.38 × 0.82 m`). |
| `Rocks` | `Rock_08` | `rock_08.json` | **1.4705** | **1.3091** | **0.8987** | `[-0.78..0.69]` | `[-0.68..0.63]` | `[-0.15..0.75]` | ±0.074 m | 284 | `[PROPOSAL]` Scale `1.65×` (`1.47×1.31×0.90 m -> 2.43×2.16×1.48 m`) to restore large boulder tier (`2.4–3.2 m`). |
| `Rocks` | `Rock_09` | `rock_09.json` | **1.5116** | **1.3876** | **0.9400** | `[-0.76..0.75]` | `[-0.68..0.71]` | `[-0.15..0.79]` | ±0.076 m | 288 | `[PROPOSAL]` Scale `2.00×` (`1.51×1.39×0.94 m -> 3.02×2.78×1.88 m`) to match procedural `Rock_05` (`3.40 m`) large obstacle tier. |
| `Rover` | `FocusLens` | `focus_lens.json` | **0.1160** | **0.1010** | **0.1345** | `[-0.11..0.00]` | `[-0.07..0.04]` | `[-0.06..0.08]` | ±0.020 m | 2,340 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `HoverCoils` | `hover_coils.json` | **0.6420** | **0.6120** | **0.1520** | `[-0.32..0.32]` | `[-0.31..0.31]` | `[-0.15..0.00]` | ±0.032 m | 2,996 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_BatterySpine` | `kit_battery_spine.json` | **0.1030** | **0.4000** | **0.1060** | `[-0.06..0.05]` | `[-0.20..0.20]` | `[-0.00..0.10]` | ±0.020 m | 2,976 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_CapacitorDrum` | `kit_capacitor_drum.json` | **0.1523** | **0.3840** | **0.1568** | `[-0.01..0.15]` | `[-0.19..0.19]` | `[-0.10..0.06]` | ±0.020 m | 2,992 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_CargoRack` | `kit_cargo_rack.json` | **0.7320** | **0.3210** | **0.4250** | `[-0.37..0.37]` | `[-0.47..-0.15]` | `[-0.01..0.41]` | ±0.037 m | 1,904 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_LampBar` | `kit_lamp_bar.json` | **0.7670** | **0.2694** | **0.1902** | `[-0.38..0.38]` | `[0.04..0.31]` | `[-0.17..0.02]` | ±0.038 m | 2,768 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_SideBin` | `kit_side_bin.json` | **0.1465** | **0.3192** | **0.1600** | `[-0.00..0.14]` | `[-0.16..0.16]` | `[-0.08..0.08]` | ±0.020 m | 1,612 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_TapeDeck` | `tape_deck.json` | **0.0960** | **0.2184** | **0.2184** | `[-0.04..0.06]` | `[-0.11..0.11]` | `[-0.11..0.11]` | ±0.020 m | 1,372 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_TetherRing` | `kit_tether_ring.json` | **0.5772** | **0.0930** | **0.3280** | `[-0.29..0.29]` | `[0.17..0.26]` | `[-0.13..0.19]` | ±0.029 m | 1,984 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `LensGlass` | `lens_glass.json` | **0.0400** | **0.1800** | **0.1800** | `[-0.07..-0.03]` | `[-0.09..0.09]` | `[-0.09..0.09]` | ±0.020 m | 1,672 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `RoverModel` | `rover.json` | **1.4726** | **2.0143** | **1.5902** | `[-0.74..0.74]` | `[-0.88..1.14]` | `[0.00..1.59]` | ±0.101 m | 57,980 | **Canonical Anchor** (`1.473 × 2.014 × 1.591 m`; wheel footprint `1.473 × 1.759 m`, head `1.525 m`). |
| `Rover` | `ScanFan` | `scan_fan.json` | **0.1451** | **0.1239** | **0.0940** | `[-0.16..-0.01]` | `[-0.04..0.08]` | `[-0.04..0.05]` | ±0.020 m | 2,980 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `SolarWing` | `solar_wing.json` | **0.4000** | **0.2300** | **0.0238** | `[-0.20..0.20]` | `[-0.22..0.01]` | `[-0.01..0.01]` | ±0.020 m | 1,664 | Pass (matches 07 socket & clearance contract). |
| `Ruins` | `FallenSolarArray` | `fallen_solar_array.json` | **6.7515** | **2.4479** | **1.0589** | `[-3.31..3.44]` | `[-1.12..1.33]` | `[-0.07..0.99]` | ±0.250 m | 4,268 | Pass (`6.75 × 2.45 × 1.06 m` wrecked wing). |
| `Ruins` | `HabitatModules` | `habitat_modules.json` | **10.5400** | **3.2100** | **3.2221** | `[-5.05..5.49]` | `[-1.60..1.60]` | `[-0.02..3.21]` | ±0.250 m | 11,828 | `[PROPOSAL]` Resize `ModuleA_Main` airlock door `1.15× W × 1.55× H` (leaf `0.96×1.32 m -> 1.10×2.05 m`, frame `1.22×1.48 m -> 1.36×2.20 m`, keypad `Z: 0.84 -> 1.25 m`) to fit a `1.75 m` suited human. |
| `Ruins` | `KenjiWorkshop` | `kenji_workshop.json` | **5.2596** | **5.5800** | **3.0800** | `[-2.63..2.63]` | `[-2.77..2.81]` | `[-0.00..3.08]` | ±0.250 m | 9,896 | `[PROPOSAL]` Raise `HoistRail` by `+0.48 m` (`Z: 1.268 -> 1.750 m`) and `HangingLamp` by `+0.40 m` (`Z: 1.350 -> 1.750 m`) to clear 07 (`1.591 m`); scale storage cabinet height `1.55×` (`1.16 -> 1.80 m`). |
| `Ruins` | `RadioTower` | `radio_tower.json` | **5.8331** | **3.7527** | **7.2959** | `[-2.86..2.97]` | `[-1.86..1.90]` | `[-0.08..7.22]` | ±0.250 m | 9,540 | Pass (`5.83 × 3.75 × 7.30 m` leaning truss + base bunker). |
| `Ruins` | `ScatterSet` | `scatter_set.json` | **4.4400** | **2.3451** | **1.4262** | `[-2.36..2.08]` | `[-1.22..1.13]` | `[-0.04..1.39]` | ±0.222 m | 7,880 | `[PROPOSAL]` Scale `SealedCrates` by `0.70×` (`0.92×0.68×0.64 m -> 0.64×0.48×0.45 m`) to match medium field crates, or standardize 3 crate tiers. |
| `Structures` | `AntennaMast` | `antenna_mast.json` | **1.5534** | **1.2646** | **2.7970** | `[-0.63..0.93]` | `[-0.63..0.63]` | `[-0.00..2.80]` | ±0.140 m | 5,252 | Pass (`1.55 × 1.26 × 2.80 m`; `Socket_Power_1` at `Z = 0.44 m`). |
| `Structures` | `BatteryBank` | `battery_bank.json` | **1.4618** | **0.9870** | **1.5975** | `[-0.77..0.69]` | `[-0.47..0.52]` | `[-0.00..1.60]` | ±0.080 m | 5,446 | `[PROPOSAL]` Lower `Socket_Power_1/2` by `-0.55 m` (`Z: 0.90/1.01 m -> 0.35 m`) to match the `0.08–0.45 m` rover-socket band on all other structures. |
| `Structures` | `ChargingPad` | `charging_pad.json` | **2.6150** | **2.6613** | **1.5500** | `[-1.31..1.31]` | `[-1.35..1.31]` | `[-0.00..1.55]` | ±0.133 m | 4,696 | `[PROPOSAL]` Widen `GuideRail_L/R` center X from `±0.68 m` (`1.258 m` clear) to `±0.86 m` (`1.62 m` clear), move wheel lanes from `±0.48 m` to `±0.61 m` (matching 07's `1.22 m` track), widen front ramp `1.63×` (`0.98 -> 1.60 m`), and lengthen deck `+0.54 m` (`2.66 -> 3.20 m`, moving rear bollard to `Y = -1.10 m`). |
| `Structures` | `Fabricator` | `fabricator.json` | **2.2120** | **1.5614** | **1.8400** | `[-1.11..1.11]` | `[-0.61..0.95]` | `[-0.00..1.84]` | ±0.111 m | 5,484 | Pass (`2.21 × 1.56 × 1.84 m`; workbed at `Z = 0.82 m`, `Socket_Power_1/2` at `Z = 0.31/0.45 m`). |
| `Structures` | `PowerPylon` | `power_pylon.json` | **0.7770** | **0.8030** | **1.9457** | `[-0.40..0.37]` | `[-0.37..0.43]` | `[-0.00..1.95]` | ±0.097 m | 3,264 | `[PROPOSAL]` Lower `KnifeSwitch` by `-0.55 m` (`Z: 1.22..1.39 m -> 0.67..0.84 m`) to sit within 07's `0.73 m` manipulator reach. |
| `Structures` | `Smelter` | `smelter.json` | **3.2668** | **1.8590** | **2.3800** | `[-1.59..1.68]` | `[-0.94..0.91]` | `[-0.00..2.38]` | ±0.163 m | 5,340 | `[PROPOSAL]` Lower `Hopper` / `HopperDoor` feed mouth by `-0.50 m` (`Z: 0.85..1.30 m -> 0.35..0.80 m`, hinge `0.65 -> 0.25 m`) or add a `0.55 m` feed ramp so 07's `ScoopBucket` (`Z = 0.12..0.34 m`) can dump ore into it. |
| `Structures` | `SolarArray` | `solar_array.json` | **2.4059** | **1.5662** | **1.9385** | `[-1.21..1.20]` | `[-0.88..0.69]` | `[-0.00..1.94]` | ±0.120 m | 3,616 | Pass (`2.41 × 1.57 × 1.94 m`; `Socket_Power_1` at `Z = 0.30 m`, `BreakerHandle` at `Z = 0.72 m`). |
| `Structures` | `StorageRack` | `storage_rack.json` | **1.7890** | **0.9175** | **1.6200** | `[-0.89..0.89]` | `[-0.49..0.43]` | `[-0.00..1.62]` | ±0.089 m | 5,800 | `[PROPOSAL]` Lower upper tier `Cradle_4..6` by `-0.22 m` (`Z: 0.90 -> 0.68 m`) and overall height `0.86×` (`1.62 -> 1.40 m`) so upper bins sit below 07's `0.73 m` manipulator/cutter reach and `0.80 m` cargo hatch. |
| `Structures` | `UpgradeBay` | `upgrade_bay.json` | **3.1956** | **2.5300** | **2.7450** | `[-1.60..1.60]` | `[-1.26..1.26]` | `[-0.00..2.75]` | ±0.160 m | 8,484 | `[PROPOSAL]` Move `GuideRail_L/R` from `X = ±0.72 m` (`0.056 m` trough) to `X = ±0.61 m` with `0.32 m` trough width (`5.7×` wider trough) to fit 07's `0.257 m` tyres, and retract `WrenchArm_L/R` rest pose outward by `0.35 m` per side (`X = ±0.55 m`, `1.10 m` clear width vs `0.53 m` today) to clear 07's `0.77 m` Head. |
| `Structures` | `VehiclePad` | `vehicle_pad.json` | **3.7605** | **3.3889** | **2.2851** | `[-2.05..1.71]` | `[-1.51..1.88]` | `[-0.00..2.28]` | ±0.188 m | 6,492 | `[PROPOSAL]` Widen `RampLip` by `1.25×` (`1.405 m` clear -> `1.76 m` clear, `1.95 m` outer) to clear 07's `1.473 m` tyres, and raise `HoistHook` rest Z by `+0.52 m` (`1.233 -> 1.750 m`) to clear 07 (`1.591 m`). |

---

## 5. Complete Measured Inventory — All 116 Procedural Prefabs (`Assets/_Project/Generated/Art/`)

For procedural prefabs that include ground-blending `DustSkirt` or millimeter-offset `Weather_*` overlays, **Core Size (`W × L × H`)** measures the structural mesh geometry (excluding `DustSkirt` and `Weather_*`), while **Full Renderer Size** includes all child renderers.

| Category | Prefab Name | Asset Path | Core Width X (m) | Core Length Z (m) | Core Height Y (m) | Full Renderer Size `W × L × H` (m) | Triangles |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| `Base` | `CassetteShelf` | `Assets/_Project/Generated/Art/Base/CassetteShelf.prefab` | **0.7564** | **0.3226** | **1.2267** | `1.1240 × 0.6650 × 1.2367` | 13 |
| `Base` | `Lander` | `Assets/_Project/Generated/Art/Base/Lander.prefab` | **21.9445** | **25.4713** | **8.9174** | `22.3445 × 25.6655 × 8.9174` | 168 |
| `Base` | `MuseumShelf` | `Assets/_Project/Generated/Art/Base/MuseumShelf.prefab` | **3.1563** | **1.2311** | **2.0183** | `3.2803 × 1.3311 × 2.0183` | 12 |
| `Base` | `RadioTower_L1` | `Assets/_Project/Generated/Art/Base/RadioTower_L1.prefab` | **2.4709** | **2.6584** | **9.4765** | `2.4709 × 2.8031 × 9.4765` | 16 |
| `Base` | `RadioTower_L2` | `Assets/_Project/Generated/Art/Base/RadioTower_L2.prefab` | **2.4709** | **2.6584** | **12.7288** | `2.4709 × 2.8031 × 12.7288` | 16 |
| `Base` | `RadioTower_L3` | `Assets/_Project/Generated/Art/Base/RadioTower_L3.prefab` | **2.4709** | **2.6584** | **15.9286** | `2.4709 × 2.8031 × 15.9286` | 16 |
| `Base` | `RoverBay` | `Assets/_Project/Generated/Art/Base/RoverBay.prefab` | **6.2349** | **6.3639** | **4.8502** | `6.4067 × 6.5754 × 4.8502` | 35 |
| `Base` | `ShelfCrate_Metal` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Metal.prefab` | **0.3000** | **0.3490** | **0.1240** | `0.3000 × 0.3490 × 0.1240` | 1 |
| `Base` | `ShelfCrate_Optics` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Optics.prefab` | **0.3000** | **0.3490** | **0.1240** | `0.3000 × 0.3490 × 0.1240` | 1 |
| `Base` | `ShelfCrate_OrangeBeads` | `Assets/_Project/Generated/Art/Base/ShelfCrate_OrangeBeads.prefab` | **0.3000** | **0.3490** | **0.1241** | `0.3000 × 0.3490 × 0.1241` | 1 |
| `Base` | `ShelfCrate_Wiring` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Wiring.prefab` | **0.3000** | **0.3490** | **0.1232** | `0.3000 × 0.3490 × 0.1232` | 1 |
| `Base` | `StationPad_160` | `Assets/_Project/Generated/Art/Base/StationPad_160.prefab` | **3.3175** | **3.3193** | **0.0310** | `3.3175 × 3.3193 × 0.0310` | 7 |
| `Base` | `StationPad_240` | `Assets/_Project/Generated/Art/Base/StationPad_240.prefab` | **4.9173** | **4.9626** | **0.0310** | `4.9173 × 4.9626 × 0.0310` | 7 |
| `Base` | `StationPad_300` | `Assets/_Project/Generated/Art/Base/StationPad_300.prefab` | **6.2020** | **6.1161** | **0.0310** | `6.2020 × 6.1161 × 0.0310` | 7 |
| `Fragments` | `Fragment_BoostCoils_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_0.prefab` | **0.9032** | **0.5610** | **0.4696** | `0.9032 × 0.5610 × 0.4696` | 6 |
| `Fragments` | `Fragment_BoostCoils_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_1.prefab` | **0.8600** | **0.5962** | **0.4101** | `0.8600 × 0.5962 × 0.4101` | 6 |
| `Fragments` | `Fragment_BoostCoils_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_2.prefab` | **1.4536** | **0.8752** | **0.7575** | `1.4536 × 0.8752 × 0.7575` | 6 |
| `Fragments` | `Fragment_CargoCradle_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_CargoCradle_0.prefab` | **0.6522** | **1.0406** | **0.5284** | `0.6522 × 1.0406 × 0.5284` | 6 |
| `Fragments` | `Fragment_CargoCradle_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_CargoCradle_1.prefab` | **0.7144** | **0.7450** | **0.3973** | `0.7144 × 0.7450 × 0.3973` | 6 |
| `Fragments` | `Fragment_CellPack_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_0.prefab` | **0.5414** | **0.4074** | **0.2901** | `0.5414 × 0.4074 × 0.2901` | 6 |
| `Fragments` | `Fragment_CellPack_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_1.prefab` | **0.4932** | **0.3638** | **0.2427** | `0.4932 × 0.3638 × 0.2427` | 6 |
| `Fragments` | `Fragment_CellPack_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_2.prefab` | **0.9234** | **0.4466** | **0.3638** | `0.9234 × 0.4466 × 0.3638` | 6 |
| `Fragments` | `Fragment_FocusLens_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_0.prefab` | **0.3482** | **0.4372** | **0.2513** | `0.3482 × 0.4372 × 0.2513` | 6 |
| `Fragments` | `Fragment_FocusLens_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_1.prefab` | **0.5042** | **0.4350** | **0.1644** | `0.5042 × 0.4350 × 0.1644` | 6 |
| `Fragments` | `Fragment_FocusLens_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_2.prefab` | **0.4808** | **0.4810** | **0.3311** | `0.4808 × 0.4810 × 0.3311` | 6 |
| `Fragments` | `Fragment_HoverJump_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_0.prefab` | **0.4650** | **0.4692** | **0.3887** | `0.4650 × 0.4692 × 0.3887` | 6 |
| `Fragments` | `Fragment_HoverJump_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_1.prefab` | **0.9102** | **0.5048** | **0.4067** | `0.9102 × 0.5048 × 0.4067` | 6 |
| `Fragments` | `Fragment_HoverJump_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_2.prefab` | **0.5164** | **0.5864** | **0.8487** | `0.5164 × 0.5864 × 0.8487` | 6 |
| `Fragments` | `Fragment_SideBins_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_SideBins_0.prefab` | **0.9472** | **0.6824** | **0.7926** | `0.9472 × 0.6824 × 0.7926` | 6 |
| `Fragments` | `Fragment_SideBins_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_SideBins_1.prefab` | **1.7296** | **0.7574** | **0.6730** | `1.7296 × 0.7574 × 0.6730` | 6 |
| `Fragments` | `Fragment_WarmHeadlamp_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_WarmHeadlamp_0.prefab` | **0.4412** | **0.4450** | **0.3187** | `0.4412 × 0.4450 × 0.3187` | 6 |
| `Fragments` | `Fragment_WarmHeadlamp_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_WarmHeadlamp_1.prefab` | **0.8398** | **0.5928** | **0.7365** | `0.8398 × 0.5928 × 0.7365` | 6 |
| `Friends` | `Bell` | `Assets/_Project/Generated/Art/Friends/Bell.prefab` | **1.2594** | **0.6770** | **1.9060** | `1.2594 × 0.6770 × 1.9060` | 25 |
| `Friends` | `Bell_Broken` | `Assets/_Project/Generated/Art/Friends/Bell_Broken.prefab` | **1.7921** | **1.6161** | **1.2351** | `1.7921 × 1.6161 × 1.2351` | 25 |
| `Friends` | `Part_BellCone` | `Assets/_Project/Generated/Art/Friends/Part_BellCone.prefab` | **0.3110** | **0.3058** | **0.2672** | `0.3110 × 0.3058 × 0.2672` | 1 |
| `Friends` | `Part_BellKnob` | `Assets/_Project/Generated/Art/Friends/Part_BellKnob.prefab` | **0.3323** | **0.1824** | **0.2650** | `0.3323 × 0.1824 × 0.2650` | 1 |
| `Friends` | `Part_BellValve` | `Assets/_Project/Generated/Art/Friends/Part_BellValve.prefab` | **0.1954** | **0.1256** | **0.3175** | `0.1954 × 0.1256 × 0.3175` | 1 |
| `Friends` | `Part_TillyCell` | `Assets/_Project/Generated/Art/Friends/Part_TillyCell.prefab` | **0.3080** | **0.1600** | **0.1506** | `0.3080 × 0.1600 × 0.1506` | 1 |
| `Friends` | `Part_TillyLens` | `Assets/_Project/Generated/Art/Friends/Part_TillyLens.prefab` | **0.2609** | **0.2041** | **0.3191** | `0.2609 × 0.2041 × 0.3191` | 1 |
| `Friends` | `Part_TillyRotor` | `Assets/_Project/Generated/Art/Friends/Part_TillyRotor.prefab` | **0.3625** | **0.1860** | **0.3077** | `0.3625 × 0.1860 × 0.3077` | 1 |
| `Friends` | `Tilly` | `Assets/_Project/Generated/Art/Friends/Tilly.prefab` | **0.5692** | **0.5692** | **0.6170** | `0.5692 × 0.5692 × 0.6170` | 12 |
| `Friends` | `Tilly_Broken` | `Assets/_Project/Generated/Art/Friends/Tilly_Broken.prefab` | **0.5299** | **0.5747** | **0.6509** | `0.5299 × 0.5747 × 0.6509` | 12 |
| `Landmarks` | `ChasmBridge_Cable` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Cable.prefab` | **14.0756** | **2.5736** | **0.2368** | `14.0756 × 2.5736 × 0.2368` | 1 |
| `Landmarks` | `ChasmBridge_Far` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Far.prefab` | **0.7000** | **3.1850** | **5.4818** | `1.1000 × 3.5569 × 5.4818` | 3 |
| `Landmarks` | `ChasmBridge_Near` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Near.prefab` | **3.1000** | **3.2332** | **5.8718** | `3.5200 × 3.6044 × 5.8718` | 2 |
| `Landmarks` | `DishFrame_0` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_0.prefab` | **6.5267** | **3.5215** | **5.7681** | `6.5726 × 3.9033 × 5.7681` | 2 |
| `Landmarks` | `DishFrame_1` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_1.prefab` | **5.7870** | **3.7024** | **6.1700** | `5.9586 × 4.0840 × 6.1700` | 2 |
| `Landmarks` | `DishFrame_2` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_2.prefab` | **3.5843** | **6.4217** | **5.5330** | `3.7559 × 6.4217 × 5.5330` | 2 |
| `Landmarks` | `RoutePost` | `Assets/_Project/Generated/Art/Landmarks/RoutePost.prefab` | **0.2200** | **0.2200** | **1.3500** | `0.6200 × 0.6044 × 1.3600` | 3 |
| `Pickups` | `BlueprintTube` | `Assets/_Project/Generated/Art/Pickups/BlueprintTube.prefab` | **0.4740** | **0.1008** | **0.1060** | `0.4740 × 0.1008 × 0.1060` | 1 |
| `Pickups` | `Cassette_after_dark_1` | `Assets/_Project/Generated/Art/Pickups/Cassette_after_dark_1.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Cassette_dust_and_honey` | `Assets/_Project/Generated/Art/Pickups/Cassette_dust_and_honey.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Cassette_slow_orbit` | `Assets/_Project/Generated/Art/Pickups/Cassette_slow_orbit.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Material_Metal` | `Assets/_Project/Generated/Art/Pickups/Material_Metal.prefab` | **0.3558** | **0.2529** | **0.1430** | `0.3558 × 0.2529 × 0.1430` | 1 |
| `Pickups` | `Material_Optics` | `Assets/_Project/Generated/Art/Pickups/Material_Optics.prefab` | **0.3331** | **0.2550** | **0.1430** | `0.3331 × 0.2550 × 0.1430` | 1 |
| `Pickups` | `Material_OrangeBeads` | `Assets/_Project/Generated/Art/Pickups/Material_OrangeBeads.prefab` | **0.3253** | **0.3172** | **0.2721** | `0.3253 × 0.3172 × 0.2721` | 1 |
| `Pickups` | `Material_Wiring` | `Assets/_Project/Generated/Art/Pickups/Material_Wiring.prefab` | **0.2936** | **0.3330** | **0.1775** | `0.2936 × 0.3330 × 0.1775` | 1 |
| `Pickups` | `Part_RelaySpare` | `Assets/_Project/Generated/Art/Pickups/Part_RelaySpare.prefab` | **0.3497** | **0.3270** | **0.4372** | `0.3497 × 0.3270 × 0.4372` | 1 |
| `Pickups` | `Part_TowerCoil` | `Assets/_Project/Generated/Art/Pickups/Part_TowerCoil.prefab` | **0.3440** | **0.3272** | **0.3550** | `0.3440 × 0.3272 × 0.3550` | 1 |
| `Pickups` | `Part_Transformer` | `Assets/_Project/Generated/Art/Pickups/Part_Transformer.prefab` | **0.4324** | **0.2526** | **0.3502** | `0.4324 × 0.2526 × 0.3502` | 1 |
| `Props` | `Cabinet` | `Assets/_Project/Generated/Art/Props/Cabinet.prefab` | **0.9800** | **0.5500** | **1.1600** | `1.3000 × 0.8640 × 1.1700` | 6 |
| `Props` | `CoilCrate` | `Assets/_Project/Generated/Art/Props/CoilCrate.prefab` | **0.8240** | **0.5390** | **0.4800** | `1.2000 × 0.8774 × 0.4900` | 6 |
| `Props` | `Derelict_06` | `Assets/_Project/Generated/Art/Props/Derelict_06.prefab` | **1.3907** | **1.3893** | **1.5934** | `1.7409 × 1.5184 × 1.5934` | 9 |
| `Props` | `Derelict_Drone` | `Assets/_Project/Generated/Art/Props/Derelict_Drone.prefab` | **0.6189** | **1.9780** | **3.2068** | `0.7925 × 2.1700 × 3.2068` | 7 |
| `Props` | `Derelict_HaulBot` | `Assets/_Project/Generated/Art/Props/Derelict_HaulBot.prefab` | **1.4625** | **1.9481** | **0.8030** | `1.8260 × 1.9481 × 0.8100` | 9 |
| `Props` | `Derelict_Mule` | `Assets/_Project/Generated/Art/Props/Derelict_Mule.prefab` | **1.9526** | **2.1228** | **2.0828** | `2.2108 × 2.2017 × 2.0828` | 8 |
| `Props` | `HullFragment_0` | `Assets/_Project/Generated/Art/Props/HullFragment_0.prefab` | **1.7266** | **2.2000** | **1.2719** | `1.7266 × 2.2000 × 1.2719` | 2 |
| `Props` | `HullFragment_1` | `Assets/_Project/Generated/Art/Props/HullFragment_1.prefab` | **2.1974** | **1.4139** | **0.9911** | `2.1974 × 1.4139 × 0.9911` | 2 |
| `Props` | `HullFragment_2` | `Assets/_Project/Generated/Art/Props/HullFragment_2.prefab` | **1.7454** | **1.3084** | **0.9206** | `1.7454 × 1.3084 × 0.9206` | 2 |
| `Props` | `HullFragment_3` | `Assets/_Project/Generated/Art/Props/HullFragment_3.prefab` | **1.5173** | **1.7076** | **1.3069** | `1.5173 × 1.7076 × 1.3069` | 2 |
| `Props` | `JumperWreck` | `Assets/_Project/Generated/Art/Props/JumperWreck.prefab` | **1.8244** | **1.9467** | **0.6114** | `2.2244 × 2.3268 × 0.6114` | 5 |
| `Props` | `JunctionBox` | `Assets/_Project/Generated/Art/Props/JunctionBox.prefab` | **0.4640** | **0.3190** | **0.8600** | `0.4640 × 0.4582 × 0.8700` | 6 |
| `Props` | `LightPool` | `Assets/_Project/Generated/Art/Props/LightPool.prefab` | **2.0000** | **2.0000** | **0.2400** | `2.0000 × 2.0000 × 0.2400` | 1 |
| `Props` | `LogCache` | `Assets/_Project/Generated/Art/Props/LogCache.prefab` | **0.5240** | **0.4129** | **0.3278** | `0.9080 × 0.7214 × 0.3378` | 2 |
| `Props` | `OrangeBeadPatch_0` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_0.prefab` | **2.2912** | **2.2805** | **0.2433** | `2.2912 × 2.2805 × 0.2433` | 1 |
| `Props` | `OrangeBeadPatch_1` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_1.prefab` | **1.8889** | **1.6472** | **0.2393** | `1.8889 × 1.6472 × 0.2393` | 1 |
| `Props` | `OrangeBeadPatch_2` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_2.prefab` | **3.3165** | **2.5804** | **0.2443** | `3.3165 × 2.5804 × 0.2443` | 1 |
| `Props` | `RockfallPocket` | `Assets/_Project/Generated/Art/Props/RockfallPocket.prefab` | **1.4407** | **1.1776** | **0.7688** | `1.8208 × 1.3605 × 0.7788` | 3 |
| `Props` | `SpareCrate` | `Assets/_Project/Generated/Art/Props/SpareCrate.prefab` | **0.4124** | **0.1990** | **0.3084** | `0.4124 × 0.1990 × 0.3084` | 5 |
| `Props` | `SupplyPod` | `Assets/_Project/Generated/Art/Props/SupplyPod.prefab` | **2.4350** | **1.7078** | **3.7384** | `2.6411 × 1.9022 × 3.7384` | 7 |
| `Props` | `TapeCase` | `Assets/_Project/Generated/Art/Props/TapeCase.prefab` | **0.3840** | **0.2790** | **0.1600** | `0.7500 × 0.6338 × 0.1700` | 6 |
| `Props` | `TillySled` | `Assets/_Project/Generated/Art/Props/TillySled.prefab` | **1.5483** | **1.6114** | **0.9070** | `1.8574 × 1.9150 × 0.9070` | 10 |
| `Relay` | `Part_RelayModule` | `Assets/_Project/Generated/Art/Relay/Part_RelayModule.prefab` | **0.3450** | **0.1750** | **0.2920** | `0.3450 × 0.1750 × 0.2920` | 1 |
| `Relay` | `RelayHut` | `Assets/_Project/Generated/Art/Relay/RelayHut.prefab` | **3.2225** | **2.1842** | **2.2839** | `3.4225 × 2.4822 × 2.2914` | 2 |
| `Relay` | `RelayMast` | `Assets/_Project/Generated/Art/Relay/RelayMast.prefab` | **5.4800** | **7.7157** | **9.9518** | `5.8800 × 8.0477 × 9.9518` | 10 |
| `Relay` | `RelayMast_Broken` | `Assets/_Project/Generated/Art/Relay/RelayMast_Broken.prefab` | **5.4800** | **7.7157** | **9.7094** | `5.8800 × 8.0270 × 9.7094` | 10 |
| `Relics` | `Relic_astronaut_boot` | `Assets/_Project/Generated/Art/Relics/Relic_astronaut_boot.prefab` | **0.2110** | **0.4000** | **0.3841** | `0.2110 × 0.4000 × 0.3841` | 1 |
| `Relics` | `Relic_cassette_player` | `Assets/_Project/Generated/Art/Relics/Relic_cassette_player.prefab` | **0.0792** | **0.2700** | **0.1386** | `0.0792 × 0.2700 × 0.1386` | 1 |
| `Relics` | `Relic_garden_gnome` | `Assets/_Project/Generated/Art/Relics/Relic_garden_gnome.prefab` | **0.1970** | **0.2175** | **0.5000** | `0.1970 × 0.2175 × 0.5000` | 1 |
| `Relics` | `Relic_golden_record` | `Assets/_Project/Generated/Art/Relics/Relic_golden_record.prefab` | **0.4000** | **0.1163** | **0.3761** | `0.4000 × 0.1163 × 0.3761` | 1 |
| `Relics` | `Relic_rubber_duck` | `Assets/_Project/Generated/Art/Relics/Relic_rubber_duck.prefab` | **0.1592** | **0.2000** | **0.1663** | `0.1592 × 0.2000 × 0.1663` | 1 |
| `Relics` | `Relic_teapot` | `Assets/_Project/Generated/Art/Relics/Relic_teapot.prefab` | **0.2361** | **0.3500** | **0.2102** | `0.2361 × 0.3500 × 0.2102` | 1 |
| `Rocks` | `Rock_00` | `Assets/_Project/Generated/Art/Rocks/Rock_00.prefab` | **0.3500** | **0.3084** | **0.1786** | `0.3500 × 0.3084 × 0.1786` | 1 |
| `Rocks` | `Rock_01` | `Assets/_Project/Generated/Art/Rocks/Rock_01.prefab` | **0.8000** | **0.7218** | **0.5586** | `0.8000 × 0.7218 × 0.5586` | 1 |
| `Rocks` | `Rock_02` | `Assets/_Project/Generated/Art/Rocks/Rock_02.prefab` | **0.9804** | **1.0000** | **1.2134** | `0.9804 × 1.0000 × 1.2134` | 1 |
| `Rocks` | `Rock_03` | `Assets/_Project/Generated/Art/Rocks/Rock_03.prefab` | **1.3000** | **0.9970** | **0.3666** | `1.3000 × 0.9970 × 0.3666` | 1 |
| `Rocks` | `Rock_04` | `Assets/_Project/Generated/Art/Rocks/Rock_04.prefab` | **1.8000** | **1.7948** | **1.2080** | `1.8000 × 1.7948 × 1.2080` | 1 |
| `Rocks` | `Rock_05` | `Assets/_Project/Generated/Art/Rocks/Rock_05.prefab` | **3.4000** | **3.2234** | **2.7057** | `3.4000 × 3.2234 × 2.7057` | 1 |
| `Rover` | `FocusLens` | `Assets/_Project/Generated/Art/Rover/FocusLens.prefab` | **0.3428** | **0.0793** | **0.3407** | `0.3428 × 0.0793 × 0.3407` | 2 |
| `Rover` | `HoverCoils` | `Assets/_Project/Generated/Art/Rover/HoverCoils.prefab` | **0.5278** | **0.8000** | **0.1450** | `0.5278 × 0.8000 × 0.1450` | 9 |
| `Rover` | `Kit_BatterySpine` | `Assets/_Project/Generated/Art/Rover/Kit_BatterySpine.prefab` | **0.1004** | **0.4400** | **0.1191** | `0.1004 × 0.4400 × 0.1191` | 1 |
| `Rover` | `Kit_CapacitorDrum` | `Assets/_Project/Generated/Art/Rover/Kit_CapacitorDrum.prefab` | **0.3889** | **0.5400** | **0.4465** | `0.3889 × 0.5400 × 0.4465` | 2 |
| `Rover` | `Kit_CargoRack` | `Assets/_Project/Generated/Art/Rover/Kit_CargoRack.prefab` | **0.8704** | **0.6222** | **0.4502** | `0.8704 × 0.6222 × 0.4502` | 2 |
| `Rover` | `Kit_LampBar` | `Assets/_Project/Generated/Art/Rover/Kit_LampBar.prefab` | **1.2120** | **0.2829** | **0.7500** | `1.2120 × 0.2829 × 0.7500` | 4 |
| `Rover` | `Kit_SideBin` | `Assets/_Project/Generated/Art/Rover/Kit_SideBin.prefab` | **0.2090** | **0.3120** | **0.1920** | `0.2090 × 0.3120 × 0.1920` | 1 |
| `Rover` | `Kit_TapeDeck` | `Assets/_Project/Generated/Art/Rover/Kit_TapeDeck.prefab` | **0.0880** | **0.3000** | **0.1540** | `0.0880 × 0.3000 × 0.1540` | 1 |
| `Rover` | `Kit_TetherRing` | `Assets/_Project/Generated/Art/Rover/Kit_TetherRing.prefab` | **0.2120** | **0.0240** | **0.2120** | `0.2120 × 0.0240 × 0.2120` | 2 |
| `Rover` | `LensGlass` | `Assets/_Project/Generated/Art/Rover/LensGlass.prefab` | **0.3120** | **0.0200** | **0.3120** | `0.3120 × 0.0200 × 0.3120` | 1 |
| `Rover` | `RoverModel` | `Assets/_Project/Generated/Art/Rover/RoverModel.prefab` | **1.6300** | **2.2733** | **1.6447** | `1.6300 × 2.2733 × 1.6447` | 89 |
| `Rover` | `ScanFan` | `Assets/_Project/Generated/Art/Rover/ScanFan.prefab` | **0.0738** | **0.0129** | **0.0738** | `0.0738 × 0.0129 × 0.0738` | 1 |
| `Sites` | `GaragePort` | `Assets/_Project/Generated/Art/Sites/GaragePort.prefab` | **2.7000** | **1.3299** | **1.6800** | `2.7000 × 1.3299 × 1.6800` | 3 |
| `Sites` | `Site_depot` | `Assets/_Project/Generated/Art/Sites/Site_depot.prefab` | **12.7000** | **13.1989** | **10.0599** | `12.9088 × 13.5087 × 10.0599` | 15 |
| `Sites` | `Site_drill` | `Assets/_Project/Generated/Art/Sites/Site_drill.prefab` | **15.1612** | **11.9014** | **10.2876** | `15.5251 × 12.2069 × 10.2876` | 19 |
| `Sites` | `Site_garage` | `Assets/_Project/Generated/Art/Sites/Site_garage.prefab` | **10.8317** | **12.4099** | **9.8300** | `11.2317 × 12.6413 × 9.8300` | 20 |
| `Sites` | `Site_kestrel` | `Assets/_Project/Generated/Art/Sites/Site_kestrel.prefab` | **12.4800** | **9.4348** | **9.4690** | `12.8469 × 9.8052 × 9.4690` | 23 |
| `Sites` | `Site_lander` | `Assets/_Project/Generated/Art/Sites/Site_lander.prefab` | **8.7603** | **8.2809** | **4.2010** | `9.1265 × 8.6408 × 4.2010` | 20 |
