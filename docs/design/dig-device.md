# Digging & Ice-Coring Device Study (`DIG-DEVICE` — Round 3)

**Target Model:** Rover `07` v3 (`Assets/_Project/Art/Models/Rover/Rover.glb` / `tools/art/blender/rover/build_rover.py`)  
**Study Scripts & Renders:** `Logs/dig/build_dig_study.py`, `Logs/dig/encode_media.py`, `Logs/dig/index.html`, `Logs/dig/metrics.json` (git-ignored)  
**Round 1–2 Backup:** `Logs/r2/dig/` (git-ignored)  
**Author:** `agy-scoop`  
**Status:** Round 3 Complete — Recommending **Variant 3 (`combo` — The Combination: Dust Fountain + Singing Core Bell)**; Round 1–2 Variants (`A2`, `A1`, `B`, `C`) **REJECTED BY OWNER**.

---

## 1. Owner Verdict on Round 1–2 (`A2`, `A1`, `B`, `C` — REJECTED)

> **Owner Verdict (Round 2 Review):**  
> *"The drill looks too crude and doesn't fit the game's vibe. Is there something more creative and sensible? 07 can't always have a pointed spike sticking out in front."*

### Summary of Rejected Round 1–2 Variants

| Variant | Concept | Status | Why Rejected |
| --- | --- | --- | --- |
| **`A2`** | Forward-Tilted Survey Corer Mast (`-112°` stowed tilt) | **REJECTED BY OWNER** | Left a permanent pointed auger spike (`z = +1.05 m`, `27.5 cm` nose protrusion) sticking out in front of 07's face at all times; industrial drill look felt too crude for the game's warm, quiet 1980s tone. |
| **`A1`** | Vertical Survey Corer Mast (`0°` stowed tilt) | **REJECTED (Round 2)** | Permanent vertical drill tower (`+46 cm` above head roof) competed with 07's CRT head silhouette. |
| **`B`** | Nose Hatch Auger Drill | **REJECTED BY OWNER** | Replaced 07's lower front bumper with a mechanical drill press; occluded from the chase camera (`0.0%` mesh visibility) and violated 07's friendly character vibe. |
| **`C`** | Articulated Front Bucket Loader | **REJECTED BY OWNER** | Wrapped `1.12 m`-wide bulldozer loader arms around 07's front face and headlamps (`27.7 cm` nose protrusion), turning a gentle scientific instrument into a construction tractor. |

---

## 2. Round 3 Design Principles & Honest Chase-Camera Setup

From the Owner's verdict and `RULES.md`:
1. **100% Hidden or Flush When Stowed (`0.0 mm` Nose Protrusion):** When 07 is driving across the Mare, nothing sticks out in front of the bumper (`z = +0.775 m`) or above the head roof. 07 keeps its clean, friendly CRT face, warm headlamps, central camera lens, and compact silhouette (`stowed_front` and `stowed_back` renders verify zero visual intrusion).
2. **Vacuum-Logical (`RULES.md` — "Vacuum logic"):** The Moon has no air (`1/6 g`, hard vacuum). Regolith does not billow into atmospheric smoke clouds; instead, every dislodged grain follows a crisp **ballistic parabola ("rooster-tail" spray)** and lands cleanly. Valid lunar physics include **cold-gas ($O_2$) thrusters/nozzles**, **ultrasonic/piezo standing-wave resonance**, **electrostatic travelling waves on charged dust**, and **cryogenic cold-finger traps**.
3. **Musical Mechanics in D Major Pentatonic (`RULES.md` — "Musical mechanics"):** Every dig/core cycle has a clear **Start beat**, **Working rhythm**, and **Done beat** pitched to D major pentatonic (`D4 = 293.7 Hz`, `E4 = 329.6 Hz`, `F#4 = 370.0 Hz`, `A4 = 440.0 Hz`, `B4 = 493.9 Hz`, `D5 = 587.3 Hz`, `F#5 = 740.0 Hz`, `A5 = 880.0 Hz`).
4. **Two Distinct Excavation Jobs in the Story (`docs/design/story-v4.md`):**
   - **Job 1 — Uncovering Buried Relics & Anchors:** Gently brushing regolith off buried survey canisters, cable junctions, and keepsakes in front of 07 so 07's CRT eye and headlamps can inspect them.
   - **Job 2 — Pulling a Subsurface Ice Core Sample:** Extracting a cylindrical core of ancient buried water ice (the story's climax proof) and holding it up with a bright frost glint.
5. **Honest Gameplay Chase Camera (`Assets/_Project/Data/Tuning/RoverCameraTuning.asset`):**
   - `_distance = 7.5 m`, `_defaultPitch = 16°`, `_targetHeight = 1.1 m`, `_screenPosition = (0.0, 0.1)`, `FOV = 55°` vertical (`16:9` aspect).
   - Exact Unity camera position relative to Rover root: `(0.0, +2.780 m, -6.834 m)` looking down at `20.04°` pitch (`16° + 4.04°` screen-Y composition offset). Rover 07 occupies `20.5%` screen width and `40.8%` screen height.

---

## 3. Recommended Winner: Variant 3 (`combo`) — Dust Fountain + Singing Core Bell

We recommend **Variant 3 (`combo` — The Combination: Dust Fountain + Singing Core Bell)** as the production design for Rover 07.

### 3.1 Why Combining Both Flush Systems Beats Any Single Device

| Criterion | `bell` Alone (Singing Core Bell) | `fountain` Alone (Dust Fountain) | `combo` (Dust Fountain + Singing Core Bell) |
| --- | --- | --- | --- |
| **Job 1: Uncovering Buried Relics** | **5 / 10** — Because the bell is under 07's belly (`z = +0.08 m`), 07 would have to drive blind *on top* of a buried relic mound to uncover it, risking high-centering and leaving the uncovered relic hidden under the floor pan away from 07's eye and front tether. | **10 / 10** — Twin bumper de Laval nozzles puff rhythmic cold-gas $O_2$ jets right in front of 07 (`z = +0.80..1.35 m`), brushing regolith away in headlamp-lit parabolic rooster-tail arcs while 07 watches with curious head tilts. | **10 / 10** — Uses the front bumper **Dust Fountain** for all surface uncovering in front of 07's gaze. |
| **Job 2: Pulling a Deep Ice Core** | **10 / 10** — Authentic JPL/Honeybee ultrasonic resonance corer; lowers from a flush belly hatch, fluidizes consolidated regolith into concentric Chladni wave rings with almost zero push-force, and lifts a glowing glass ice core into a flush rear-deck sight-glass (`z = -0.44 m`). | **4 / 10** — Cold-gas puffs disperse loose surface dust effortlessly, but gas jets alone cannot cut or lift an intact cylindrical core of hard, cryogenic subsurface ice. | **10 / 10** — Uses the under-belly **Singing Core Bell** + rear-deck **Dorsal Sight-Glass** to extract and display the glowing cylindrical ice core. |
| **Stowed Appearance (`0.0 mm` Spike)** | **10 / 10** — 100% hidden behind flush belly clamshell doors (`y = 0.335 m`) and flush rear-deck sight-glass cap (`y = 0.953 m`). | **10 / 10** — 100% hidden behind flush dark-steel/brass shutter flaps inside the front bumper channel (`z = +0.7735 m`, `0.0 mm` nose protrusion). | **10 / 10** — Because *both* subsystems stow 100% flush inside existing hull surfaces, combining them adds **zero external clutter** when driving. |
| **Chase-Camera Readability** | **9 / 10** — Expanding golden wave rings (`1.26 m` diameter) + twin lateral rooster-tail sprays (`45.1%` total action vis) + rear-deck core reveal (`14.7%` device vis at `done`). | **9 / 10** — Wide parabolic rooster-tail dust sheets (`50.9%` total action vis) arcing out on both sides of 07's front fenders. | **10 / 10** — Delivers a two-stage visual story: forward rooster-tail uncovering (`f12–56`) followed by under-belly golden wave rings + lateral rooster-tails + rear-deck glowing ice core reveal (`f44–124`). |

---

## 4. Detailed Evaluation of All Four Round 3 Variants

### 4.1 Variant 1 (`bell`): Director's Idea 1 — The Singing Core Bell (Chuông lõi hát)
- **Inspiration:** Real JPL / Honeybee Robotics ultrasonic planetary corers + 1980s brass acoustic laboratory instruments.
- **Sub-mesh Hierarchy (9 parts):**
  - `BellyBayRing` (`R1_Brass`, lathe-turned bezel flush at `y = 0.340 m, z = +0.08 m`)
  - `BellyHatch_L` & `BellyHatch_R` (`R1_DarkSteel` + `R1_Brass` split semicircular clamshell doors, flush at `y = 0.335 m` when closed; roll open `±68°`)
  - `BellStem` (`R1_Chrome` telescopic actuator shaft with 3 `R1_Copper` magnetostrictive transducer coil rings; lowers `-0.320 m`)
  - `SingingBell` (`R1_Brass` lathe-turned resonator bell with `R1_LampAmber` acoustic rim, `R = 0.154 m`; presses onto the regolith at `y = -0.014 m` and hums/rotates)
  - `CoreTube` (`Study_IceCore` borosilicate glass tube + `Study_FrostGlint` inside the bell throat)
  - `DorsalSightRing`, `DorsalSightCap`, `DorsalCoreColumn` (flush brass sight-glass receiver at `(0.0, 0.945, -0.44)` on 07's rear deck behind the solar panel; when the core rises at `done`, the cap flips open `-72°` and the glowing ice core column + 6-ray diamond frost glint rises `+0.19 m` directly in front of the Chase Camera).
- **Vacuum Physics Visuals:** Ultrasonic vibration fluidizes the surrounding Mare regolith into **3 concentric golden standing-wave rings** (`SonicWaveRing_0..2`, expanding from `R = 0.20 m` to `0.63 m` past all four wheels) while kicking loose grains outward in **6 parabolic 1/6 g rooster-tail ribbons** (`RoosterArc_L/R_0..2`) and 20 ballistic grain clusters (`SprayGrain_0..19`).

### 4.2 Variant 2 (`fountain`): Director's Idea 2 — The Dust Fountain (Vòi phun bụi)
- **Inspiration:** Lunar cold-gas ($O_2$) attitude thrusters / surveyor gas-jet erosion experiments + 1980s brass pneumatic nozzles.
- **Sub-mesh Hierarchy (6 parts):**
  - `BumperBay_L` & `BumperBay_R` (`R1_DarkSteel` & `R1_Brass` recessed housings tucked inside the front bumper channel at `x = ±0.195 m, y = 0.425 m, z = +0.752 m`)
  - `BumperFlap_L` & `BumperFlap_R` (`R1_DarkSteel` shutter flaps with `R1_Brass` trim strip; sit 100% flush at `z = +0.7735 m` when closed (`0.0000 m` protrusion beyond `FRONT_BUMPER_FACE_Z = 0.775 m`); flip down `-74°` on deploy)
  - `FountainNozzle_L` & `FountainNozzle_R` (lathe-turned `R1_Brass`, `R1_Copper`, and `R1_OrangePaint` convergent-divergent de Laval nozzles; slide forward `+0.052 m` to `z = +0.826 m`, pitch down `19°..23°`, and sweep `±14°..21°` in yaw).
- **Vacuum Physics Visuals:** Three rhythmic cold-gas $O_2$ puffs (`ColdGasJet_L/R` 3-needle supersonic Mach plumes) sweep the regolith mound away in high, laminar 1/6 g parabolic rooster-tail arcs (`y_max = 1.05 m`, `x = ±1.18 m`), gently uncovering the buried brass/amber `UncoveredRelic` in front of 07's headlamps like brushing snow off a keepsake.

### 4.3 Variant 3 (`combo`): The Combination — Dust Fountain + Singing Core Bell (Recommended Winner)
- **Sub-mesh Hierarchy (15 parts):** Combines the 6-part `fountain` front-bumper assembly (`Combo_BumperBay_L/R`, `Combo_BumperFlap_L/R`, `Combo_FountainNozzle_L/R`) with the 9-part `bell` belly-corer and rear-deck sight-glass assembly (`Combo_BellyBayRing`, `Combo_BellyHatch_L/R`, `Combo_BellStem`, `Combo_SingingBell`, `Combo_CoreTube`, `Combo_DorsalSightRing`, `Combo_DorsalSightCap`, `Combo_DorsalCoreColumn`).
- **Two-Stage Choreography (`144` frames / `6.0 s`):**
  1. **Frames `12–56` (`0.50–2.33 s` — Uncover):** Front bumper flaps snap open (`-74°`), brass de Laval nozzles extend `+5.2 cm`, and three rhythmic cold-$O_2$ puffs (`D5–E5–F#5`) sweep the surface mound away in high parabolic rooster-tail arcs, uncovering the gleaming relic/ice lens in front of 07.
  2. **Frames `42–122` (`1.75–5.08 s` — Sing & Core):** Belly clamshell doors roll open (`±68°`), the brass Singing Core Bell lowers onto the soil (`D4 + A4 + F#5` chord), golden standing-wave rings + lateral rooster-tail sprays ripple outward on both sides of 07, and at `f = 84..122` the glowing glass ice core + 6-ray diamond frost glint rises into the rear-deck dorsal sight-glass (`D4→F#4→A4→D5→F#5` arpeggio) while 07 turns its CRT head (`-22°` yaw, `+6°` pitch) to admire the core.

### 4.4 Variant 4 (`ring`): `[PROPOSAL]` Author's Idea — Electrostatic Chladni Ring & Cryo-Trap Lantern (Vòng Âm Thoa Tĩnh Điện)
- **Inspiration:** 1980s geophysical loop detectors + lunar electrostatic dust levitation + cryogenic cold-finger core traps.
- **Sub-mesh Hierarchy (4 parts):**
  - `ChinMount` (`R1_DarkSteel` & `R1_Brass` flush pivot block under the front chin at `(0.0, 0.362, +0.50)`)
  - `HaloArm` (twin slender `R1_Brass` & `R1_Chrome` parallel pantograph links, `|x| <= 0.131 m`, `0.54 m` long; folds `180°` flat against the belly pan at `y = 0.334..0.382 m` when stowed)
  - `ChladniRing` (horizontal concentric `R1_Brass` & `R1_Copper` tuning-fork ring, `R = 0.204 m`, with `R1_LampAmber` rim; swings out `150°` to hover horizontally at `(0.0, 0.07, +1.02)` over the mound)
  - `CryoLantern` (`Study_IceCore` borosilicate glass cylinder with 4 `R1_Copper` cryo-fins and top `Study_FrostGlint` diamond star; condenses and lifts the ice core, swinging up and out to `x = +0.35 m, y = 0.80..1.20 m` beside 07's right cheek at `done`).

---

## 5. Exact Clearance & Chase-Camera Visibility Metrics

All values below are computed directly from the evaluated 3D meshes in Blender (`Logs/dig/metrics.json`), including the `±28°` front-wheel steering excursion (`0.045 m` inward tyre edge swing):

| Metric | Requirement | `combo` (Winner) | `bell` (Director 1) | `fountain` (Director 2) | `ring` (Author Proposal) |
| --- | --- | --- | --- | --- | --- |
| **Part Count** | Modular rigid children | `15` | `9` | `6` | `4` |
| **Min Steered Wheel Clearance (`±28°`)** | `>= 0.050 m` | **`0.1951 m`** (PASS) | **`0.2890 m`** (PASS) | **`0.1951 m`** (PASS) | **`0.1268 m`** (PASS) |
| **Min Suspension Wishbone/Shock Clearance** | `>= 0.050 m` | **`0.2384 m`** (PASS) | **`0.2955 m`** (PASS) | **`0.2384 m`** (PASS) | **`0.2567 m`** (PASS) |
| **Min Head / Neck Gimbal Clearance** | `>= 0.050 m` | **`0.1757 m`** (PASS) | **`0.1759 m`** (PASS) | **`0.5534 m`** (PASS) | **`0.2395 m`** (PASS) |
| **Stowed Belly Ground Clearance** | `>= 0.250 m` | **`0.3310 m`** (PASS) | **`0.3310 m`** (PASS) | **`0.3940 m`** (PASS) | **`0.3340 m`** (PASS) |
| **Stowed Nose Protrusion Beyond Bumper (`z=+0.775m`)** | `0.000 m` (Flush) | **`0.0000 m`** (FLUSH) | **`0.0000 m`** (FLUSH) | **`0.0000 m`** (FLUSH) | **`0.0000 m`** (FLUSH) |
| **Chase-Cam Total Action Vis (`deploy` / `mid_dig`)** | Readable in gameplay | **`40.2%` / `37.2%`** (`42.9%` screen X-span) | **`32.6%` / `45.1%`** (`37.7%` screen X-span) | **`32.5%` / `50.9%`** (`34.2%` screen X-span) | **`21.2%` / `34.6%`** (`38.4%` screen X-span) |
| **Chase-Cam Device Vis at `done` (Core Display)** | Core & frost glint visible | **`10.4%`** (dorsal core column + frost glint) | **`14.7%`** (dorsal core column + frost glint) | `0.0%` (front relic glint visible on ground) | **`4.3%`** (CryoLantern beside right cheek) |

---

## 6. Musical Beats — Timing, Easing & D-Major Pentatonic Sound Design

Every variant is choreographed to **D major pentatonic** (`D4 = 293.7 Hz`, `E4 = 329.6 Hz`, `F#4 = 370.0 Hz`, `A4 = 440.0 Hz`, `B4 = 493.9 Hz`, `D5 = 587.3 Hz`, `F#5 = 740.0 Hz`, `A5 = 880.0 Hz`):

| Variant | Beat 1: Start / Deploy | Beat 2: Working Rhythm (Fluidize / Puff) | Beat 3: Done / Core & Relic Reveal | Beat 4: Stow |
| --- | --- | --- | --- | --- |
| **`combo` (Recommended Winner)** | **0.50–0.85 s (`f12–20`):** `smoothstep` bumper flap snap (`-74°`) + nozzle slide (`+5.2 cm`). Sound: crisp brass shutter click + rising fifth chime `D4 → A4`. | **Stage 2A (`0.85–2.33 s`, `f20–56`):** 3 rhythmic cold-$O_2$ puffs in 3/4 meter (`D5 – E5 – F#5` breathy valve transients) brushing the mound clear.<br>**Stage 2B (`2.08–3.92 s`, `f50–94`):** Belly hatch opens (`F#4`); brass bell kisses soil with a warm Tibetan singing-bowl chord (`D4 + A4 + F#5`) pulsing at `4 Hz` as golden wave rings expand. | **3.75–5.08 s (`f90–122`):** `easeOutBack` dorsal sight-glass cap flip (`-72°`) + glowing ice core column rise (`+19 cm`). Sound: vacuum seal pop + crystalline glass-harp arpeggio `D4 → F#4 → A4 → D5 → F#5` (frost glint shimmer on `A5`). | **5.10–5.85 s (`f122–140`):** `easeInOutCubic` retract. Sound: descending warm triad `A4 → F#4 → D4` + soft pneumatic latch click. |
| **`bell` (Director 1 — Singing Core Bell)** | **0.50–1.08 s (`f12–26`):** `smoothstep` belly clamshell roll (`±68°`) + stem drop (`-32 cm`). Sound: warm brass iris latch + `D4 → A4` bell strike as rim touches regolith. | **1.08–3.75 s (`f26–90`):** Continuous `4.5°/f` rotation + `±3 mm` magnetostrictive vertical hum. Sound: sustained D-major-pentatonic singing-bowl chord (`D4` fundamental + `A4` fifth + `E5/F#5` overtone shimmer) synchronized with 3 expanding Chladni wave rings. | **3.50–5.00 s (`f84–120`):** `smoothstep` lift of `CoreTube` & `DorsalCoreColumn`. Sound: crystalline celesta/glass-tube glissando `F#4 → A4 → B4 → D5 → F#5` as the frost glint flashes behind 07's head. | **5.00–5.67 s (`f120–136`):** `smoothstep` retract. Sound: soft damped bell mute (`F#4 → D4`) + flush belly hatch click. |
| **`fountain` (Director 2 — The Dust Fountain)** | **0.50–0.92 s (`f12–22`):** Quick `smoothstep` flap drop (`-74°`) & nozzle slide (`+5.2 cm`). Sound: twin brass camera-shutter snicks (`A4 → D5`). | **0.92–3.83 s (`f22–92`):** Sinusoidal yaw sweep (`±14°..21°`) + 3 harmonic pressure waves. Sound: rhythmic cold-gas $O_2$ pan-pipe puffs pitched to `D5 – F#5 – A5 – F#5` with silky ballistic dust-grain patter in `1/6 g`. | **3.67–5.00 s (`f88–120`):** Gentle settle as last rooster-tail arc lands. Sound: warm 1980s synth discovery chime (`D4 – F#4 – B4 – D5`) as the uncovered brass/amber relic gleams in the headlamp beam. | **5.00–5.50 s (`f120–132`):** `smoothstep` nozzle retract. Sound: short valve sigh (`E4 → D4`) + flush bumper flap snap. |
| **`ring` (Author Proposal — Chladni Ring)** | **0.50–1.25 s (`f12–30`):** `smoothstep` `150°` swing-out on twin brass pantograph links. Sound: tape-transport servo whir + tuning-fork ping (`A4`). | **1.25–3.67 s (`f30–88`):** Counter-rotating ring harmonic (`±0.5°` flutter). Sound: warm analog Rhodes/tuning-fork arpeggio (`D4 – E4 – F#4 – A4`) driving travelling-wave electrostatic dust fountains. | **3.67–4.83 s (`f88–116`):** `smoothstep` upward arc (`-48°` pitch, `+34°` yaw) to present `CryoLantern` beside 07's right cheek. Sound: cryo-latch chime (`B4 → D5 → F#5`). | **4.83–5.58 s (`f116–134`):** `smoothstep` fold under chin. Sound: warm descending interval (`A4 → D4`) + magnetic chin-latch lock. |

---

## 7. Deliverable Media Index (`Logs/dig/`)

- **Comparison Gallery (Chase GIFs First):** `Logs/dig/index.html`
- **Quantitative Metrics JSON:** `Logs/dig/metrics.json`
- **Variant 3 (`combo` — Recommended Winner):**
  - Chase & Side Animations: `Logs/dig/variant_combo/combo_chase.gif`, `combo_chase.mp4`, `combo_side.gif`, `combo_side.mp4`
  - Stowed Proof & Key Poses: `Logs/dig/variant_combo/poses/combo_stowed_front.png`, `combo_stowed_back.png`, `combo_chase_{stowed,deploy,mid,done}.png`, `combo_side_{stowed,deploy,mid,done}.png`, `combo_front_{stowed,deploy,mid,done}.png`
- **Variant 1 (`bell` — Director's Idea 1: Singing Core Bell):**
  - Chase & Side Animations: `Logs/dig/variant_bell/bell_chase.gif`, `bell_chase.mp4`, `bell_side.gif`, `bell_side.mp4`
  - Stowed Proof & Key Poses: `Logs/dig/variant_bell/poses/bell_stowed_front.png`, `bell_stowed_back.png`, `bell_chase_{stowed,deploy,mid,done}.png`, `bell_side_{stowed,deploy,mid,done}.png`, `bell_front_{stowed,deploy,mid,done}.png`
- **Variant 2 (`fountain` — Director's Idea 2: Dust Fountain):**
  - Chase & Side Animations: `Logs/dig/variant_fountain/fountain_chase.gif`, `fountain_chase.mp4`, `fountain_side.gif`, `fountain_side.mp4`
  - Stowed Proof & Key Poses: `Logs/dig/variant_fountain/poses/fountain_stowed_front.png`, `fountain_stowed_back.png`, `fountain_chase_{stowed,deploy,mid,done}.png`, `fountain_side_{stowed,deploy,mid,done}.png`, `fountain_front_{stowed,deploy,mid,done}.png`
- **Variant 4 (`ring` — Author Proposal: Chladni Ring & Cryo-Trap Lantern):**
  - Chase & Side Animations: `Logs/dig/variant_ring/ring_chase.gif`, `ring_chase.mp4`, `ring_side.gif`, `ring_side.mp4`
  - Stowed Proof & Key Poses: `Logs/dig/variant_ring/poses/ring_stowed_front.png`, `ring_stowed_back.png`, `ring_chase_{stowed,deploy,mid,done}.png`, `ring_side_{stowed,deploy,mid,done}.png`, `ring_front_{stowed,deploy,mid,done}.png`
