# Wear, Physical Repair & The Reclaimers — Design Specification

> **Status:** **DESIGN ONLY** — Implementation waits for the Core Feel Gate ([`docs/ROADMAP.md`](../ROADMAP.md), *"Cổng lõi cảm giác"*). Core design frame and Round 3 owner review picks locked (`[DECIDED]`, 2026-10-17) per [`docs/VISION.md`](../VISION.md#L180-L198) Ruling 17; detailed tuning ranges remain `[PROPOSAL]` for balancing.  
> **Bilingual Twin:** [`wear-and-reclaimers-vi.md`](wear-and-reclaimers-vi.md)  
> **Concept Art Review Board:** [`Logs/wear/index.html`](../../Logs/wear/index.html) (`Logs/concepts/*.png`)

---

## Executive Summary & Design Frame

> [!IMPORTANT]
> **Design-Only Gate:** This specification and its concept art define the complete design for Wear, Physical Repair, and the Reclaimers. **No gameplay code or assets for this feature are implemented yet**—implementation waits until the Core Feel Gate in [`docs/ROADMAP.md`](../ROADMAP.md) (*"Cổng lõi cảm giác"*) is passed and approved by the owner.

On 2026-10-17, the project owner established two foundational shifts locked in [`docs/VISION.md`](../VISION.md#L180-L198) (**Ruling 17 — Wear, Repair, Reclaimers, and Death**) and locked the Round 3 review picks (`[DECIDED]`):

1. **07 has a physical Wear ("Hull & Subsystem Integrity") meter alongside its Battery.** Driving too fast into hard obstacles, enduring harsh regional phenomena, or being grappled by automated **Reclaimer** machines drains wear across **4 distinct subsystem failure milestones (`75%`, `50%`, `25%`, `10%`)**. At `0%` wear, **07 dies**.
2. **Every step of damage, repair, and threat obeys physical and vacuum logic ([`vacuum-shapes.md`](vacuum-shapes.md)):**
   - **Battery stays range, never death** ([`docs/VISION.md` Ruling 15](../VISION.md#L163-L176)): `0%` battery still triggers the `25%`-speed reserve trickle crawl (`M3-20`). Death happens *only* at `0%` wear.
   - **Bounces stay joyful:** Low-gravity hops, cushioned landings on lunar regolith, and gentle bumps **never** cost wear.
   - **07 has no hands** ([`docs/VISION.md` Ruling 14](../VISION.md#L158-L162)): Field repair (**Repair Kit — ORU Module Swap**) and base repair (**Service Gantry**) are 100% physical, mechanical operations using 07's small fold-out service arm and central chest cutter/welder emitter (`CutterOrigin`), or the overhead arms of the Service Gantry.
   - **Vacuum lighting & physics everywhere:** Because the Moon sits in a hard vacuum with no air to scatter light or suspend dust haze, searchlights and headlamps show **only as hot glowing lenses and sharp-edged pools of light where they hit the ground or a surface** (never visible light cones or vertical beams in empty space), dust flies in clean ballistic arcs and falls at once, and welding sparks fly in short straight streaks.
   - **Quiet tension, not combat:** 07 has no weapons. **Reclaimers**—1980s Departure-era automated salvage machines still feeding the L5 Mass Driver for the Arks ([`story-v4.md`](story-v4.md))—are slow, readable, avoidable industrial hazards that follow a **Stealth → Understand → Transponder Immunity → Reprogrammed Ally** progression arc.

### Owner Review Picks (2026-10-17 — All `[DECIDED]`)

| Topic | Owner Decision (`[DECIDED]`) | Section |
| :--- | :--- | :---: |
| **1. Death Consequence** | **Default selected (`[DECIDED]`):** Tilly (`friend.tilly`) tows 07's lightened hull back to the last Service Gantry / Rover Bay with all installed kits and progress kept; the hold's loose cargo stays in the ejected **Cargo Beacon Pod** at the wreck site (or in `Sorting Bin 03` at the Reclamation Yard). | [§1.6](#16-consequence-of-death-story-logical-physical-return--considered-alternatives) |
| **2. No-Death Mode Name** | **Named `"Chỉ khám phá"` / `"Explore only"` (`[DECIDED]`).** The no-wear / no-death mode is officially named `"Explore only"` (`"Chỉ khám phá"`) across all docs and UI. | [§1.9](#19-new-game-modes-survival-explore-only-and-hardcore-10-opt-in) |
| **3. Hardcore One-Life** | **Deferred to `1.0` as an opt-in mode (`[DECIDED]`).** Ship `Survival` (default) and `Explore only` (`Chỉ khám phá`) first; add `Hardcore` later in `1.0` as an opt-in toggle. | [§1.9](#19-new-game-modes-survival-explore-only-and-hardcore-10-opt-in) |
| **4. Reclaimer Lineup** | **Keep all three (`[DECIDED]`):** `SHEAR-85` Nimble Spotter, `BALER-IV` Heavy Clamp-Hauler, and `GOLIATH-L5` Yard Gantry-Crane. | [§3.4](#34-the-3-reclaimer-machine-types--vacuum-shapes-audit-decided) |
| **5. Clearer Wear Milestones** | **4 escalating subsystem failure milestones (`75%`, `50%`, `25%`, `10%` — `[DECIDED]`):** Each milestone breaks a specific 1980s rover subsystem that the player both sees on 07 v3 and feels in the controls. | [§1.4](#14-the-4-subsystem-wear-milestones-75-50-25-10--decided) |
| **6. Repair Kit as Module Swap** | **Single-use ORU Module Swap Cartridge (`[DECIDED]`):** 07 can only field-repair if it has crafted a **Repair Kit (`item.repair_kit`)**; each kit works once, swaps the most severe failed subsystem module (leaving the broken unit on the ground as debris), welds the skin plate, and restores one milestone of wear. | [§2.1](#21-field-repair-the-repair-kit-as-an-oru-module-swap-decided) |

---

## 1. The Wear Model

### 1.1 The Impact Rule: Relative Normal Speed, Threshold & Curve

In [`RoverController.cs`](../../Assets/_Project/Scripts/Rover/RoverController.cs#L38-L40), 07's physics body is driven by a rigid sphere collider (`_body`, `_sphere`) whose baseline locomotion tuning lives in [`DriveSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/DriveSettings.cs#L18-L24), [`BoostSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/BoostSettings.cs#L15-L24), and [`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs#L20-L50):
- **Standard forward cruise (`_topSpeed`):** `8.0 m/s` (`DriveSettings.cs:18`)
- **Reverse top speed (`_reverseTopSpeed`):** `3.5 m/s` (`DriveSettings.cs:22`)
- **Boosted flat speed (`_topSpeed + _extraSpeed`):** `9.6 m/s` (`BoostSettings.cs:15`, engaging above `85%` cruise = `6.8 m/s`)
- **Downhill / leap-assisted peak speed:** `10.0–12.5 m/s`
- **Cushioned leap touchdown (`_cushionLandingSpeed`):** `1.6 m/s` (`HoverJumpSettings.cs:48`)

#### Normal-Speed Formulation (`[PROPOSAL]`)
Glancing scrapes along a crater wall or boulder side should spark harmlessly without punishing the player. Therefore, impact wear depends strictly on the **inward normal component of relative velocity** at the contact point:

$$v_\perp = \max\!\big(0,\; -(\mathbf{v}_{07} - \mathbf{v}_{\text{collider}}) \cdot \mathbf{n}_{\text{contact}}\big)$$

where $\mathbf{n}_{\text{contact}}$ is the unit surface normal pointing out of the obstacle toward 07.

- **Hard Impact Threshold ($v_{\text{min}}$):** **`6.8 m/s`** (`[PROPOSAL]` range: `6.5–7.5 m/s`, equal to `85%` of top cruise speed).
  - If $v_\perp < v_{\text{min}}$, **wear loss is `0`**. Bumping a rock while parking, reversing (`3.5 m/s`), crawling on low battery (`2.0 m/s`), or grazing a wall at a shallow $25^\circ$ angle at full cruise ($8.0 \times \sin 25^\circ = 3.38\text{ m/s} < 6.8\text{ m/s}$) costs **zero wear**.
- **Damage Curve Above Threshold ($v_\perp \ge v_{\text{min}}$):**
  Above $v_{\text{min}} = 6.8\text{ m/s}$ up to a saturation speed $v_{\text{max}} = 12.0\text{ m/s}$, wear loss scales smoothly via a convex power curve ($p = 1.5$) so barely exceeding the threshold is a mild warning tap while a full-boost downhill head-on slam is a heavy hit:

$$\Delta W(v_\perp) = \text{round}\!\left(W_{\text{hit,min}} + (W_{\text{hit,max}} - W_{\text{hit,min}}) \cdot \operatorname{clamp01}\!\left(\frac{v_\perp - v_{\text{min}}}{v_{\text{max}} - v_{\text{min}}}\right)^{1.5}\right)$$

| Impact Scenario | Relative Normal Speed $v_\perp$ | Formula Output ($\Delta W$ out of `100`) | Player Feel & Consequence |
| :--- | :---: | :---: | :--- |
| Gentle bump / reverse / shallow scrape ($< 30^\circ$) | `< 6.8 m/s` | **`0%`** | Harmless metallic clink + ballistic dust grains; zero wear loss. |
| Threshold brush (late braking into boulder) | `6.8 – 7.2 m/s` | **`4% – 6%`** | Mild wince chirp, short straight spark streak, HUD wear bar fades in. |
| Full unbraked head-on cruise slam (`100%` cruise) | `8.0 m/s` | **`12%`** | Audible panel thud, CRT face winces (`> <`), ~8 full slams to die from `100%`. |
| Full Boost Coil head-on crash (`8.0 + 1.6 m/s`) | `9.6 m/s` | **`24%`** | Heavy crunch, short straight spark streaks, drops one full milestone in a single hard crash. |
| Steep downhill + Boost runaway slam | `11.5 – 12.0+ m/s` | **`38% – 40%` (cap)** | Major structural hit; never a one-shot kill from full health (minimum 3 worst-case crashes to reach `0%`). |

- **Post-Impact Grace Cooldown:** **`1.0 s`** (`[PROPOSAL]` range `0.8–1.2 s`). Multi-point physics contacts or bouncing off one rock into an adjacent rock within `1.0 s` only register the single highest $v_\perp$ contact, preventing unfair physics-pinball instant deaths.

---

### 1.2 Which Surfaces Are Hard vs. Safe

To preserve Pillar 2 (*"Weightless joy"* — low-gravity hops and tumbling across lunar hills are a reward, not a hazard), colliders are split into two physical material categories via Unity layer / `PhysicsMaterial` tags:

| Surface Category | Examples in World | Wear on Contact? | Physical / Gameplay Rationale |
| :--- | :--- | :---: | :--- |
| **SAFE (Soft / Granular Regolith)** | Lunar mare dust, highland regolith slopes, crater rim dust banks, compacted dirt roads (`Terrain` layer). | **NEVER (`0%`)** | Deep lunar regolith compresses and cushions wheel/hull touchdowns. Combined with `HoverJumpSettings._cushionLandingSpeed = 1.6 m/s`, **vertical landings on terrain never cost wear at any height**. |
| **HARD (Rock, Metal & Structure)** | Discrete basalt boulders, jagged crater ejecta outcrops, crashed satellite trusses, abandoned rover hulls, base structures (`StructureDefinition`), mass-driver rails, Reclaimer chassis. | **YES (if $v_\perp \ge 6.8\text{ m/s}$)** | Unyielding solid basalt and cold-rolled steel deform 07's stamped hull plates when struck head-on at high speed. |

> [!IMPORTANT]
> **Vertical Plunge Exception:** Even if 07 leaps (`HoverJump`) and lands *on top* of a flat metal pad or rock slab, its vertical velocity is governed by `HoverJumpSettings._cushionLandingSpeed = 1.6 m/s` (`<< 6.8 m/s`), so normal vertical landings on pads or rocks never hurt 07. Only slamming *horizontally* (or diving un-cushioned with empty battery/disabled coils down a cliff) into a hard rock or metal face at $v_\perp \ge 6.8\text{ m/s}$ causes wear.

---

### 1.3 The Creak Warning (Readable Before It Hurts)

Damage is never a surprise. Before 07 reaches a speed where a head-on collision with a hard object can hurt it, the rover's physical frame warns the player:
- **Activation Threshold:** Starts softly at **`6.5 m/s`** (`~81%` of cruise speed, just before the `6.8 m/s` impact threshold) and rises in volume and pitch density through `8.0 m/s` (cruise), `9.6 m/s` (boost), and `12.0 m/s` (downhill overspeed).
- **Audio Layer:** A tactile 1980s mechanical chassis rattle—coil-spring tension creaks, rubber bushing groans, and a faint high-harmonic D-major pentatonic wind-less frame hum (`A3–E4` chassis resonance transmitted through the suspension struts `Arm_FL..RR`).
- **Visual Micro-Cue:** Above `7.2 m/s`, the rear whip antenna (`Antenna` / `AntennaTip` in [`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs#L54-L57)) bends backward into a deeper arc and the CRT scanlines exhibit a subtle `1 px` horizontal vibration.

---

### 1.4 The 4 Subsystem Wear Milestones (`75%`, `50%`, `25%`, `10%` — `[DECIDED]`)

Wear is tracked as a normalized float `WearIntegrity` $\in [0.0, 1.0]$ (`100%` = pristine, `0%` = dead). Per the owner's Round 3 direction, wear is not a vague health bar: **it is divided into 4 distinct subsystem failure milestones (`75%`, `50%`, `25%`, `10%`)**. Above `75%` (`100%–76%`), 07 only accumulates cosmetic paint/dust scuffs. Crossing each milestone downward breaks a **specific 1980s mechanical or electrical subsystem** in a way the player **immediately sees on 07 v3** ([`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs)) and **feels in the controls**.

On the HUD ([`CellRowSettings.cs`](../../Assets/_Project/Scripts/UI/Tuning/CellRowSettings.cs)), the Wear Gauge is a **4-notch segmented subsystem bar** beside the battery row (hidden at `100%` or in `"Explore only"` mode) with four engraved milestone ticks (`75% ANT`, `50% SUSP`, `25% WHEEL`, `10% CRT`) whose fault indicators light up as each subsystem fails.

![07 v3 across the 4 wear milestones and the 3-frame death moment](../../Logs/concepts/wear_stages_and_death.png)

#### Overview Table: Baseline + 4 Subsystem Failure Milestones

| Milestone & Range | Failed Subsystem | Visual on 07 v3 (`RoverModelNodes.cs`) | Control & Gameplay Effect (Numbers) | Sound & CRT Face (`FaceDisplay.cs`) |
| :--- | :--- | :--- | :--- | :--- |
| **0. Nominal / Scuffed**<br>**`100% – 76%`** | **None** *(cosmetic hull scuffs only below `100%`)* | Clean white carapace (`Body`) at `100%`; light regolith dust/scuffs on front blade (`DigScoop`) at `99%–76%`. Straight `Antenna`, 3 bright front lamps, level `Shock_FL..RR`. | **`100%` nominal:** `8.0 m/s` cruise, `9.6 m/s` boost, `7.0 m` leap, `1.0×` scan speed, `0°/s` steering pull. | Quiet motor hum; calm CRT eyes (`\| \|` or `^ ^`); `0.35 s` wince chirp (`F#4 → D4`) on hit. |
| **Milestone 1 (`≤ 75%`)**<br>**`75% – 51%`** | **Comms Mast & Telemetry Loop**<br>*(Bent Antenna & Radio Static)* | Rear whip **`Antenna` bends `18°` to port** and wobbles loosely on bumps; **`AntennaTip` amber lamp stutters (`3 Hz`)**; first dent crease on `Body` upper-right shoulder. | - **Driving/Boost:** `100%` (`8.0 / 9.6 m/s`).<br>- **Compass/Beacon Ping:** Markers beyond **`120 m` jitter `±4°`** and drop out for `0.4 s` every `3.0 s`.<br>- **Scanner Range:** **`-15%` (`0.85×`)** effective lock distance. | Analog S-band radio static crackle & tape flutter on transmissions; `Antenna` spring twangs (`B3`). CRT scanlines shimmer `1 px` during static bursts. |
| **Milestone 2 (`≤ 50%`)**<br>**`50% – 26%`** | **Port Suspension Strut & Headlamp**<br>*(Corner Sag & One-Eyed Beam)* | **`Shock_FL` sags `-7 cm` (`-0.07 m`)** and `Arm_FL` pitches up `11°`, giving 07 a permanent **`6.5°` port-forward lean**; **left outer headlamp (`Lamp_0` on `HeadlampSocket`) dies completely** (dark glass, `_EmissionStrength = 0`); deeper port flank dents. | - **Cruise Speed:** **`-10%` (`7.2 m/s`)**.<br>- **Asymmetric Steering:** Turning left is **`-18%` sluggish (`0.82×`)**, turning right is **`+10%` sharper**.<br>- **Bump Steer:** Rough ground pulls nose **`2.5°/s` left** (`0.8°/s` on flat).<br>- **Night Light Pool:** **`-40%` port width** (one-eyed beam). | Bottomed-out metal-on-rubber *clunk* (`D2`) on front-left bumps; strut groan (`A2 → D3`) in turns. `Head` counter-tilts `3°`; left CRT eye squints half-shut (`- O`, `FaceFeeling.Tired`). |
| **Milestone 3 (`≤ 25%`)**<br>**`25% – 11%`** | **Front-Left Wheel Hub Motor**<br>*(Seized, Dragging Wheel & Heavy Pull)* | **`Wheel_FL` hub motor seizes and locks** (`0 rad/s` for `1.2 s`, jerks `30°`, locks again) at a **`5°` bent camber angle**; **`Wheel_FL` carves a continuous flat-bottomed drag furrow in the regolith** with low ballistic dust ribbons and periodic short straight hub sparks. | - **Top Speed Capped at `70%` (`5.6 m/s`)** *(below `6.8 m/s` impact threshold!)*.<br>- **Boost Coils Disabled.**<br>- **Constant Leftward Pull:** Dragging `Wheel_FL` pulls 07 **`8.5°/s` to port**, requiring **`~18%` right counter-steer (`D` / stick)** to hold a straight line.<br>- **Acceleration:** **`-35%` (`0.65×`)**. | Gritty rhythmic *scritch-grind-drag* of locked chevron rubber skidding through regolith + strained 3-motor whine (`E3 → F#3`) + seized gear click (`D3`). Strained `> <` (`FaceFeeling.Worried`) eyes. |
| **Milestone 4 (`≤ 10%`)**<br>**`10% – 1%`** | **CRT Deflection Board & Main Capacitor Bus**<br>*(CRT Roll/Tear, Scan Glitch & Coil Misfire)* | **CRT `Screen` rolls vertically and tears horizontally** (`R1CrtFace.shader` `_FaceCrtState.z = 0.85`, `VerticalJitter = 0.75`); `ConduitGlow` & `BatteryLamp_0..7` flicker; `CargoHatch` ajar `4°`; **short straight sparks misfire from `Coil_FL` & `StitchOrigin`**. | - **Limp Speed:** **`55%` (`4.4 m/s`)** + `8.5°/s` left pull.<br>- **Leap Coils Misfire:** Pressing `Space` fires only starboard coils—producing a **`0.35 m` lopsided sideways sputter-hop (`12°` roll kick)** + spark shower.<br>- **Scanner Glitch:** Lock time **`+60%` (`1.6×`)** with a `0.25 s` horizontal tear glitch at `70%` progress. | CRT flyback whine + `60 Hz` sync-roll buzz (`B1`), `1.5 Hz` relay tick (`D4`), and hollow capacitor *pop-sputter* on `Space`. CRT eyes shear horizontally (`±12%` UV) and roll across the glass. |

#### Detailed Breakdown Per Milestone

1. **Milestone 1 (`75%` Wear): Bent Antenna & Radio Static (`Antenna` / `AntennaTip` / `SonarLoop`)**
   - **1980s Physical Logic:** The whip mast and exposed S-band telemetry loop on 07's upper-rear deck take the first whiplash of a high-speed jolt, bending the spring mast and cracking the coaxial shield braid.
   - **Visual on Model:** `RoverModelNodes.Antenna` tilts `18°` to port (`localEulerAngles.z = 18f`) with `2.5×` spring-oscillation damping decay on terrain bumps; `AntennaTip` amber LED stutters irregularly (`_EmissionStrength` modulating between `0.2` and `1.0` at `3 Hz`); first dent normal-map/blendshape appears on `Body` upper-right shoulder.
   - **Control & Gameplay Effect:** Driving speed (`8.0 m/s`), boost (`9.6 m/s`), and leap (`7.0 m`) remain `100%` untouched so early exploration stays brisk. However, distant compass/beacon markers (`> 120 m`) jitter horizontally by `±4°` and briefly blank out (`0.4 s` every `3.0 s`), and optical/inductive scanner lock range (`ScanFanSocket`) drops by `-15%` (`0.85×`).
   - **Sound:** Warm 1980s analog FM static crackle and tape-azimuth flutter whenever beacon pings or Tilly voice/telemetry cues play; metallic whip-spring *twang* (`B3`) when cresting craters.
   - **Face Reaction:** Brief `0.6 s` `FaceFeeling.Startled` looking up-left toward the rear antenna (`LookAt(-0.2f, 0.6f, -0.5f)`), followed by a subtle `1 px` scanline shimmer during static bursts.
   - **One-Time Warning Beat (Crossing `75%`):** A sharp metallic *twang-snap* from the rear deck, a `0.35 s` burst of filtered pentatonic static (`F#4 → D4`), a single short straight spark streak from `AntennaOrigin`, and HUD Milestone 1 (`75% ANT`) locks on in warm amber.

2. **Milestone 2 (`50%` Wear): One Headlamp Dies & Suspension Corner Sags (`Lamp_0` + `Shock_FL` / `Arm_FL`)**
   - **1980s Physical Logic:** Repeated hard impacts rupture the nitrogen-oil bladder seal inside the front-left shock absorber (`Shock_FL`) and snap the hot tungsten-halogen filament inside the port outer headlamp (`Lamp_0`).
   - **Visual on Model:**
     - `Shock_FL` collapses `-0.07 m` (`-7 cm`) along local Y and `Arm_FL` pitches up `11°`, tilting 07's entire `Body` and `Head` into a **permanent `6.5°` port-forward lean**.
     - `HeadlampSocket` / `Lamp_0` (front-left outer round lamp) goes **100% dark** (`_EmissionStrength = 0.0` via `MaterialPropertyBlock`, cracked smoked lens), while `Lamp_2` (front-right) stays at `1.0` and `Lamp_1` (center) dims to `0.7`, projecting a visibly lopsided, one-eyed pool of light onto the regolith in front of 07.
   - **Control & Gameplay Effect:**
     - **Top Cruise Speed:** Reduced by **`-10%` to `7.2 m/s`** due to uneven corner weight distribution.
     - **Asymmetric Cornering:** Left turns (loading the bottomed-out front-left strut) respond **`-18%` slower (`0.82×` yaw rate)**, whereas right turns pivot **`+10%` sharper (`1.10×`)**.
     - **Bump Steer:** Driving over uneven craters or rocks kicks the nose **`2.5°/s` to the left** (`0.8°/s` drift on smooth ground).
     - **Night Illumination:** Port-side ground light pool width is reduced by **`-40%`**.
   - **Sound:** Every compression on the front-left wheel produces a dry, bottomed-out metal-on-rubber *clunk* (`D2`) instead of a pneumatic hiss, paired with a low coil groan (`A2 → D3`) during hard turns.
   - **Face Reaction:** `Head` counter-rolls `3°` to starboard on `Neck` to compensate for the `6.5°` hull lean; 07's left CRT eye squints down to `55%` height (`- O`, mirroring its dead left headlamp!) in a weary `FaceFeeling.Tired` expression.
   - **One-Time Warning Beat (Crossing `50%`):** The left headlamp flashes bright orange for `0.12 s` and pops dark with a glass-filament *ting*, immediately followed by a heavy hydraulic *thud-hiss* as `Shock_FL` drops `7 cm` and the camera tilts `2°` with the chassis lurch. HUD Milestone 2 (`50% SUSP`) locks on in deep ochre.

3. **Milestone 3 (`25%` Wear): Wheel Hub Motor Burns Out — Locked Dragging Wheel (`Wheel_FL`)**
   - **1980s Physical Logic:** Like the Soviet *Lunokhod* and NASA *Apollo LRV*, each of 07's four wheels is driven by an independent sealed brushed-DC hub motor and harmonic/planetary gearhead inside the wheel drum. At `25%` wear, the abused front-left hub motor (`Wheel_FL`) shorts its copper armature windings and seizes its planetary reduction gear.
   - **Visual on Model:**
     - **`Wheel_FL` locks stationary and cants at a `5°` bent-axle camber:** While `Wheel_FR`, `Wheel_RL`, and `Wheel_RR` spin smoothly, `Wheel_FL` stays locked (`0 rad/s` for `1.2 s`, violently jerks `30°` as torque overcomes the jammed tooth, then locks solid again).
     - **Persistent Regolith Drag Furrow:** Instead of leaving crisp chevron tread imprints, the locked `Wheel_FL` plows a **continuous, flat-bottomed skid trench (drag trail) in the lunar regolith**, kicking up low ballistic ribbons of grey dust behind the tyre and popping short straight orange sparks from the hub every `3–4 s`.
   - **Control & Gameplay Effect:**
     - **Top Speed Capped at `70%` (`5.6 m/s` forward / `2.4 m/s` reverse):** Three working motors dragging one locked wheel lowers maximum speed to `5.6 m/s` and cuts standing acceleration by **`-35%` (`0.65×`)**.
     - **Boost Coils Disabled:** Wheel boost (`BoostSettings`) is locked out to prevent burning out the remaining three hub motors.
     - **Constant `8.5°/s` Leftward Steering Drag:** Because the locked front-left tyre drags like an anchor on the port side, 07 constantly yaws **`8.5°/s` to the left** while driving forward. The player must hold **`~18%` continuous right-steering correction (`D` or right stick)** to track straight, and left turns pivot tightly around the dragging wheel while right turns require wide torque effort (`0.65×` right yaw rate).
   - **Sound:** A gritty, tactile *scritch-scrape-drag* of locked rubber-and-wire tread plowing regolith, a strained three-motor electric harmonic whine (`E3 → F#3`), and a periodic metallic gear-tooth *clack* (`D3`) every `1.2 s` when the seized hub jerks.
   - **Face Reaction:** Strained `FaceFeeling.Worried` (`> <`) whenever driving forward as 07 braces against the dragging wheel; when stopped, 07 turns its `Head` down-left (`LookAt(-0.5f, -0.6f, 0.6f)`) to stare at its jammed `Wheel_FL`.
   - **One-Time Warning Beat (Crossing `25%`):** A bright commutator arc flashes inside `Wheel_FL`'s hub with a shower of short straight sparks and a loud mechanical *CRUNCH-LOCK* (`A3 → D2`). 07's nose yaws `15°` sharply to port as the wheel locks up and begins carving its drag trench in the dust. HUD Milestone 3 (`25% WHEEL`) turns coral-red (`#C86446`).

4. **Milestone 4 (`10%` Wear): CRT Face Rolls & Tears, Scans Glitch, Leap Coils Misfire (`Screen` + `ScanFanSocket` + `Coil_FL..RR`)**
   - **1980s Physical Logic:** With the chassis heavily battered, main-bus voltage regulation destabilizes: the 1980s analog CRT horizontal/vertical sync capacitors on `Screen` lose lock, the optical scanner's sweep servo stutters, and the high-current solid-state relays driving the four electromagnetic Leap Coils (`Coil_FL..RR`) misfire out of phase.
   - **Visual on Model:**
     - **CRT Face Rolls & Tears (`R1CrtFace.shader`):** `_FaceCrtState.z` (`LowPower = 0.85`), `_FaceCrtState.w` (`VerticalJitter = 0.75`), and accelerated `_FacePhases.y` (`RollingBandPhase`) cause 07's amber CRT eyes to **split horizontally across a jagged scanline tear (`±12%` UV shear) and roll vertically up the curved glass every `1.8 s`** before snapping back.
     - **Bus & Coil Arcing:** Side charge conduits (`ConduitGlow`) and rear `BatteryLamp_0..7` flicker erratically; rear `CargoHatch` hangs `4°` ajar; short straight spark streaks pop from `StitchOrigin` and `Coil_FL`.
   - **Control & Gameplay Effect:**
     - **Limp-Home Speed (`55%` = `4.4 m/s`):** Main bus brownout caps forward speed at **`4.4 m/s`** (with the `8.5°/s` leftward wheel drag still active).
     - **Leap Coils Misfire (`HoverJumpSettings`):** Pressing `Space` misfires the port-front coil (`Coil_FL`) in a burst of short straight sparks while firing the starboard coils—resulting in a **`0.35 m` lopsided sideways sputter-hop (`12°` roll kick)** instead of a `7.0 m` leap. *(Inside a Reclaimer's clamp, firing the misfiring coils still dumps enough raw magnetic flux to add `+40%` break-free slip.)*
     - **Scanner Glitch (`ScanFanSocket`):** Scan lock duration takes **`+60%` longer (`1.6×`)**, and at `70%` scan progress the scan fan stutters for `0.25 s` with a horizontal CRT tear before completing lock.
   - **Sound:** High-voltage CRT flyback whine + `60 Hz` vertical-sync hum (`B1`), urgent `1.5 Hz` Geiger-like relay clicking (`D4`), and a hollow capacitor *pop-sputter* whenever `Space` is pressed.
   - **Face Reaction:** Painful, horizontally torn `> <` eyes rolling across the phosphor screen, visibly fighting to keep sync lock.
   - **One-Time Warning Beat (Crossing `10%`):** All four `Coil_FL..RR` rings flash amber and vent a simultaneous burst of short straight sparks onto the dust with a descending 3-note brownout chord (`A3 → F#3 → D3`); the CRT screen pinches halfway (`ScreenVerticalPinch = 0.45`) and tears horizontally before catching itself, and the final HUD segment blinks at `2.0 Hz` coral-red.

> [!TIP]
> **Why Milestones 3 & 4 (`≤ 25%` and `≤ 10%`) Cap Top Speed Below `6.8 m/s` (`5.6 m/s` and `4.4 m/s`):** Because both `5.6 m/s` and `4.4 m/s` are below the `6.8 m/s` hard-impact damage threshold, a badly damaged 07 limping home with a dragging wheel on flat ground **cannot accidentally kill itself by bumping a rock**. To lose the final `25%` wear, 07 must either careen down a steep crater slope above `6.8 m/s`, drive unprotected into an active regional hazard, or get clamped by a Reclaimer.

---

### 1.5 The Death Moment & Restart Flow (Storyboard)

When `WearIntegrity` reaches `0%`, there is **no harsh arcade explosion, no red screen flash, and no "GAME OVER" text overlay**. Consistent with Pillar 1 (*"Grow, and go farther"* / *"Alone, and at peace"*) and [`R1CrtFace.shader`](../../Assets/_Project/Shaders/R1/R1CrtFace.shader#L34-L37) (which already implements CRT vertical line pinch via `_FaceContentState.y` and horizontal phosphor line glow via `_FaceContentState.z`), 07's death is a quiet, poignant `2.8 s` power-collapse vignette:

| Frame | Time Window | Visual & Shader Action (`R1CrtFace.shader` / `RoverModelNodes`) | Audio & Camera Action |
| :--- | :---: | :--- | :--- |
| **Frame 1: Final Shudder & Beacon Eject** | `0.0 s – 0.6 s` | Final impact sparks fly in short straight ballistic streaks across the regolith. All four suspension struts (`Shock_FL..RR`) compress as 07's chassis sags `8 cm` downward and its neck gimbal (`Neck`) droops `12°` forward. If the hold (`CargoHold`) contains any items, the rear double-door `CargoHatch` pops open and physically ejects a sealed coral-and-brass **Cargo Beacon Pod** onto the dust beside 07. | Player input releases smoothly. Camera dollies out `1.8 m` and lowers to eye level. A soft, mournful two-note descending D-major pentatonic cello/synth interval (`A4 → D4`) sighs over a gentle mechanical relay latch-off. |
| **Frame 2: CRT Line Collapse** | `0.6 s – 1.5 s` | [`FaceDisplay.cs`](../../Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs) drives `ScreenVerticalPinch` (`0.0 → 1.0`) and `ScreenLineGlow` (`0.0 → 1.0`). 07's two warm amber pill eyes pinch vertically inward until they collapse into a **single razor-thin bright horizontal amber phosphor line** across the center of the dark CRT glass, which then contracts horizontally into a single dying center dot. Simultaneously, the three round front lamps (`R1_LampAmber`) and rear/side battery pips (`BatteryLamp_0..7`) dim from warm gold to dark red embers. | A classic 1980s analog CRT high-voltage capacitor discharge whisper (*fweeee-tik*) fades down as the radio carrier hum thins into pure lunar vacuum silence. |
| **Frame 3: Stillness & Relay Handshake** | `1.5 s – 2.8 s` | The CRT center dot vanishes into dark reflective glass. The rear `AntennaTip` light goes out. 07 sits motionless in the sharp lunar shadow under the starfield and dark, city-less Earth ([`story-v4.md`](story-v4.md) Decision 3), while the ejected **Cargo Beacon Pod** beside it pulses its warm amber top lamp at `0.5 Hz` (illuminating the dust around it with zero light cone in vacuum). At `2.8 s`, the view fades cleanly to black (`1.2 s` transition) accompanied by a warm cassette-deck relay click, and opens inside the last docked **Service Gantry** or **Rover Bay**. | Silence for `1.0 s`, followed by the distant, reassuring hum of the base `Service Gantry` / `Rover Bay` capacitor charging up as 07's CRT boots back up (`BootProgress: 0.0 → 1.0`). |

---

### 1.6 Consequence of Death, Story-Logical Physical Return & Considered Alternatives

#### A. Default Consequence (`[DECIDED]` — Locked by Owner Pick, 2026-10-17)
1. **Where 07 Restarts:** 07 wakes up at the **last Service Gantry or Rover Bay** it docked at (defaulting to the Home Basin `Rover Bay` before a forward Gantry is built).
2. **What 07 Keeps:** All installed chassis/tool kits, battery/hold upgrades, unlocked Fabricator recipes, scanned encyclopaedia entries, reprogrammed allies, and story progress remain **100% intact** (any crafted unused Repair Kits in the hold stay in the Cargo Beacon Pod with the rest of the hold's loose items).
3. **What Stays at the Wreck Site:** Only the **loose materials and items currently in `CargoHold`** ([`M3-25-cargo-and-storage.md`](../features/M3-25-cargo-and-storage.md)) remain behind, sealed inside the ejected **Cargo Beacon Pod** at the exact death coordinates (or inside **Sorting Bin 03** of the Reclamation Yard if 07 died inside a Reclaimer's clamps — see §1.6.B & §3.2).
   - The **Cargo Beacon Pod** glows with a bright amber locator lamp (casting a warm pool of light onto the surrounding regolith or scrap bin) and appears on the compass/scanner HUD from any distance.
   - Driving up to the Cargo Beacon Pod and pressing Interact (`E`) or touching it with the magnetic collector reloads all stored stacks back into 07's hold.
   - **Multiple Deaths Rule:** Up to `3` active Cargo Beacons can exist in the world simultaneously (preventing accidental erasure of a rich haul if 07 crashes again on the recovery trip).

#### B. How 07 Physically Comes Back in Story Logic (One Clear Physical Rule — `[DECIDED]`)
Ruling 17 requires a **100% physical, in-story explanation** so returning to base never reads as videogame teleportation, memory cloning, or spare-hull magic. Everything follows **one clear, physical rule in one sentence**:

> **The One-Sentence Physical Rule (`[DECIDED]`):** Whenever 07 hits `0%` wear—whether from a field crash or inside a Reclaimer's clamps—07 unlatches its heavy **Cargo Beacon Pod** (leaving it at the crash site, or inside **Sorting Bin 03** after the Reclaimer hauls 07 to the Reclamation Yard) while **Tilly** (`friend.tilly`, the crew's small flying helper drone, or the base's automated winch crawler) tows 07's lightened hull back to the last **Service Gantry / Rover Bay** to be hammered flat and welded back together.

- **How the Player Sees This on Wake-Up:** When the `1.2 s` black fade lifts inside the Service Gantry / Rover Bay (representing a few quiet hours on the Moon), 07's CRT screen boots (`BootProgress 0 → 1`) just as the overhead gantry welding arm retracts from a glowing seam on 07's freshly hammered white hull (and Tilly hovers beside the pad with a welcoming chirp). There is only **one `07` body**—towed home lightened of its cargo pod and physically repaired at base.

#### C. Alternatives Considered & Rejected in Round 3 Review

| Consequence Model | Status | Summary & Why Default Won |
| :--- | :---: | :--- |
| **Default: Gantry Tow-Back + Full Cargo Beacon at Wreck Site / Bin 03** | **`[DECIDED]` (Selected)** | Tilly tows 07's lightened hull to the last Dock/Gantry with all kits and progress intact; `100%` of loose hold cargo stays in the Cargo Beacon Pod at the wreck site (or in `Sorting Bin 03` at the Reclamation Yard). Zero permanent loss, clean physical rule, and turns recovery into a purposeful excursion. |
| **Alternative 1: "Tilly's Heavy Tow" (Keep 75% Cargo, Spill 25%)** | *Rejected* | Eliminated backtracking but permanently deleted `25%` of common ore and removed the salvage-yard heist payoff. |
| **Alternative 2: "Stranded Hull & Service Mule"** | *Rejected* | Driving an un-upgraded spare utility mule across distant `0.6+` hazard regions risked soft-locking or exhausting the player. |

---

### 1.7 Proposed Tuning Numbers & Ranges (`[PROPOSAL]`)

| Parameter | Proposed Default | Tuning Range | Code / Asset Location | Notes |
| :--- | :---: | :---: | :--- | :--- |
| `MaxWearIntegrity` | **`100.0`** | `100 – 150` | `WearSettings.cs` (new) | Upgraded to `150` with Hardened Hull kit. |
| `CreakStartSpeed` | **`6.5 m/s`** | `6.0 – 7.2 m/s` | `WearSettings.cs` | `81%` of `8.0 m/s` cruise (`DriveSettings._topSpeed`). |
| `HardImpactMinSpeed` ($v_{\text{min}}$) | **`6.8 m/s`** | `6.5 – 7.5 m/s` | `WearSettings.cs` | Relative normal speed $v_\perp$ below this deals `0` wear. |
| `HardImpactMaxSpeed` ($v_{\text{max}}$) | **`12.0 m/s`** | `11.0 – 14.0 m/s` | `WearSettings.cs` | Speed where impact wear reaches `MaxHitWear`. |
| `MinHitWear` ($W_{\text{hit,min}}$) | **`4.0`** (`4%`) | `3.0 – 6.0` | `WearSettings.cs` | Wear lost right at $v_\perp = 6.8\text{ m/s}$. |
| `MaxHitWear` ($W_{\text{hit,max}}$) | **`40.0`** (`40%`) | `30.0 – 50.0` | `WearSettings.cs` | Cap per single impact (never 1-shots from full health). |
| `ImpactCurveExponent` ($p$) | **`1.5`** | `1.2 – 2.0` | `WearSettings.cs` | Smooth convex ramp between `6.8` and `12.0 m/s`. |
| `PostHitGraceSeconds` | **`1.0 s`** | `0.8 – 1.5 s` | `WearSettings.cs` | Prevents multi-contact bounce double-hits. |
| `Milestone1_AntennaThreshold` | **`75%`** | `70% – 80%` | `WearSettings.cs` | Bent `Antenna` (`18°`), radio static, `-15%` scan range, `±4°` distant compass jitter. |
| `Milestone2_StrutLampThreshold` | **`50%`** | `45% – 55%` | `WearSettings.cs` | `Lamp_0` dark, `Shock_FL` sags `-7 cm` (`6.5°` lean), `0.90×` speed (`7.2 m/s`), `0.82×` left turn rate. |
| `Milestone3_WheelMotorThreshold` | **`25%`** | `20% – 30%` | `WearSettings.cs` | `Wheel_FL` hub motor seizes & drags furrow, `0.70×` speed (`5.6 m/s`), Boost off, `8.5°/s` left yaw pull (`~18%` counter-steer). |
| `Milestone4_CrtBusThreshold` | **`10%`** | `8% – 15%` | `WearSettings.cs` | CRT rolls/tears (`LowPower = 0.85`), `0.55×` speed (`4.4 m/s`), Leap Coils misfire (`0.35 m` sputter), `+60%` (`1.6×`) scan lock time. |

---

### 1.8 Interaction with Battery (`M3-20`), Leap Coils & Tow

1. **Battery (`M3-20`):**
   - **Strictly Orthogonal Channels:** Battery measures *energy/range*; Wear measures *structural & subsystem integrity*.
   - **Empty Battery Crawl Immunity:** When battery hits `0%`, 07 enters `25%` crawl speed (`2.0 m/s`) with Tilly (`friend.tilly`) flying out to lead with her lamp. Because `2.0 m/s << 6.8 m/s`, **07 cannot take impact wear while crawling on an empty battery** on normal terrain.
   - **Combined Low-Battery + Damaged Milestones:** If Battery is `0%` (`0.25×` speed) while 07 is in Milestone 2 (`0.90×`), Milestone 3 (`0.70×`), or Milestone 4 (`0.55×`), speed uses `min(batteryMult, wearMult) = 0.25×` (`2.0 m/s`) rather than multiplying them into an unplayable crawl.
2. **Leap Coils ([`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs)):**
   - **Above `10%` Wear (`100%–11%`, including Milestones 1–3):** Leap Coils work at `100%` effectiveness (`0.8 m` tap hop, `7.0 m` full leap, `1.6 m/s` cushioned touchdown) so even with a seized front-left wheel (`25%–11%`), 07 can still hop over rocks on its way home. Cushioned vertical landings on regolith or structures cost **0 wear**.
   - **At Milestone 4 (`≤ 10%` Wear):** Main bus voltage sag causes the Leap Coils to **misfire out of phase**. Pressing `Space` fires only the starboard coils with a shower of short straight sparks from `Coil_FL`, producing a **`0.35 m` lopsided sideways sputter-hop (`12°` roll kick)** and a hollow capacitor pop.
   - **While Clamped by a Reclaimer:** Triggering a Leap Coil discharge (`Space`) acts as an electromagnetic counter-pulse that immediately adds **`+60%` break-free progress** (`+40%` if misfiring at `≤ 10%` wear) against the Reclaimer's magnetic clamp (see §3.2).
3. **Tow & Recovery:**
   - **Towing Heavy Objects:** Pulling a heavy scrap object or relic via 07's tether/tow hitch naturally keeps speed below `6.0 m/s`, so normal towing never triggers impact wear. If a towed object swings into 07's rear bumper on a sudden stop, **towed-payload self-contacts are ignored (`0` wear)** so towing physics never punish the player.
   - **Unsticking:** When 07 is automatically lifted on a soft arc to get unstuck ([`docs/VISION.md` Ruling 7](../VISION.md#L122-L125)), it grants a `1.5 s` impact-wear immunity window so settling down never damages 07.

---

### 1.9 New Game Modes (`Survival`, `Explore only`, and `Hardcore` `1.0` Opt-In — `[DECIDED]`)

At **New Game** creation, the player chooses between experience modes locked by the owner's Round 3 review (`[DECIDED]`):

| Mode | Status | Rules & Behaviour | Target Player |
| :--- | :---: | :--- | :--- |
| **1. Survival (Default)** | **`[DECIDED]`** | Full Wear (`75% / 50% / 25% / 10%` subsystem milestones), Physical Repair (single-use **Repair Kit** module swap & **Service Gantry**), Reclaimers, Regional Hazards, and Death/Cargo-Beacon restart loop active. Battery stays non-lethal range (`M3-20`). | Default experience: quiet exploration enriched by tactile engineering preparation and readable tension. |
| **2. "Explore only" / "Chỉ khám phá"** | **`[DECIDED]`** | **No wear, no death.** `WearIntegrity` is locked at `100%` (HUD wear bar never appears, no subsystem ever fails). Hard impacts still play harmless metallic clinks and ballistic dust grains. Reclaimers still patrol the `0.6+` horizons as atmospheric world-machines, **never clamp 07**, and can still be scanned and reprogrammed as base-scrap allies! | Players who want the pure contemplative *lonely moon exploration* experience without structural damage or cargo recovery runs. |
| **3. Hardcore ("One-Life")** | **`[DECIDED]` (Deferred to `1.0` Opt-In)** | Identical to Survival, except if `WearIntegrity` hits `0%`, the save ends permanently after Frame 3 of the death collapse. **Not in `0.5.7`/`0.6`; ships later in `1.0` as an explicit opt-in toggle.** | Veteran players seeking high-stakes ironman tension on a repeat playthrough in `1.0`. |

---

## 2. Physical Repair (Ruling 14: 07 Has No Hands)

Per [`docs/VISION.md` Ruling 14](../VISION.md#L158-L162), **07 has no hands, no robotic fingers, and no magic repair clouds**. Every repair action—whether in the field or at base—must be a physical operation performed by real mechanisms on 07's v3 body ([`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs)) or on a base structure.

### 2.1 Field Repair: The Repair Kit as an ORU Module Swap (`[DECIDED]`)

> [!IMPORTANT]
> **Is Self-Repair by Putting Another Piece On Technically Feasible? (Honest Engineering Answer):**
> - **1. A plate patch fixes only the outer skin:** Real spaceflight precedents exist for emergency skin patching and vacuum welding. On **Apollo 17 (December 1972)**, astronauts Gene Cernan and Harrison Schmitt repaired their Lunar Roving Vehicle's broken rear fender extension on the Moon using laminated photomaps and duct-tape clamps to stop abrasive regolith rooster-tails from blinding them and overheating the rover's instruments. Three years earlier, on **Soyuz 6 (October 1969)**, the Soviet **Vulkan** experiment proved that compact electron-beam and low-voltage consumable-electrode arc welding works cleanly in hard vacuum.
> - **2. Why our subsystem milestones need a module swap, not just a plate:** Welding a flat metal plate onto 07's hood cannot un-seize a burnt-out brushed DC wheel hub motor (`25%`), recharge a blown nitrogen suspension strut (`50%`), or fix a shorted CRT deflection board (`10%`). Real space hardware—from the **Hubble Space Telescope** to the **International Space Station (ISS)**—is engineered around **Line-Replaceable Units / Orbital Replacement Units (ORUs)**: modular, self-contained mechanical/electrical cartridges with standardized cam-latches and blind-mate connectors that a single robotic manipulator arm (like *Canadarm2 / Dextre*) can unlatch, eject, and swap in vacuum.
> - **3. Therefore, 07's Field Repair Kit (`item.repair_kit`) is a single-use ORU Spare Cartridge:** Each crafted kit is a standardized 1980s Lumen maintenance cartridge holding the spare mechanical/electrical module for the failed subsystem (a wheel hub motor core, a strut sleeve, a sealed headlamp bulb, or a deflection relay board), plus a blind-mate copper cable splice and a stamped outer skin cover plate (`R1_SagePatch`).

![4-frame storyboard of 07 swapping its burnt wheel hub motor using a Field Repair Kit](../../Logs/concepts/repair_kit_module_swap.png)

#### Rules of the Field Repair Kit (`[DECIDED]`)
1. **Single Use (Consumed on Repair):** One crafted **Field Repair Kit (`item.repair_kit`)** performs **exactly one field repair** and is consumed (`1 crafted kit = 1 repair`).
2. **What One Kit Repairs:** Applying a kit always repairs **the most severe currently failed subsystem** and **restores one full milestone of wear**:
   - Used at **Milestone 4 (`10% – 1%`, CRT/Bus/Coil failure)** $\rightarrow$ swaps the shorted deflection/relay module, clears Milestone 4, and restores wear to **`25%`** (top of Milestone 3).
   - Used at **Milestone 3 (`25% – 11%`, Seized Wheel Hub Motor)** $\rightarrow$ swaps the burnt `Wheel_FL` hub motor core, unlocks the wheel, clears Milestone 3, and restores wear to **`50%`** (top of Milestone 2).
   - Used at **Milestone 2 (`50% – 26%`, Strut Sag & Dead Headlamp)** $\rightarrow$ swaps the blown `Shock_FL` strut sleeve and `Lamp_0` unit, levels 07's chassis, relights the headlamp, clears Milestone 2, and restores wear to **`75%`** (top of Milestone 1).
   - Used at **Milestone 1 (`75% – 51%`, Bent Antenna & Radio Static)** $\rightarrow$ swaps the bent antenna mast coupler, clears Milestone 1, and restores wear to **`90%`** (field repair cap—leaving only minor cosmetic scuffs).
3. **No Kit, No Field Repair:** 07 **cannot** repair itself in the field unless it has crafted at least one `item.repair_kit` at the **Fabricator** and carries it in `CargoHold`. Pressing the Repair key (`R`) with `0` kits in the hold plays an empty-latch click (`D3`) and flashes the empty kit indicator. **Only the base Service Gantry (`structure.service_gantry`) repairs 07 fully to `100%` without kits.**

#### Crafting Recipe (`SalvageMaterial.cs` & `M3-27`)
Crafted at the **Fabricator** (`StructureIds.Fabricator = "structure.fabricator"`) using existing materials from [`SalvageMaterial.cs`](../../Assets/_Project/Scripts/Core/SalvageMaterial.cs#L12-L41):

| Item ID | Name | Tier & Milestone | Fabricator Recipe (`SalvageMaterial`) | Why These Materials? | Craft Time | Hold Stack Size | Effect (`1 Use`) |
| :--- | :--- | :--- | :---: | :--- | :---: | :---: | :--- |
| `item.repair_kit` | **Field Repair Kit (ORU Spare Cartridge)** | **T1 (`0.5.7`)** | **`2 × Metal` + `2 × Wiring` + `1 × Optics`** | `2 Metal` (hub/strut housing + `R1_SagePatch` skin plate), `2 Wiring` (motor armature coils + copper cable splice), `1 Optics` (sealed headlamp/telemetry lens) | `3.0 s` | `3` per hold slot | **Repairs the most severe failed subsystem + restores `1` wear milestone (consumed)** |

#### 4-Frame Physical Storyboard (`4.5 s` Sequence — Wheel Hub Motor Swap Example)
When `WearIntegrity` is in a damaged milestone (e.g., `≤ 25%` with a seized front-left wheel hub motor) and 07 is stationary with at least one `item.repair_kit` in `CargoHold`, pressing **Field Repair (`R`)** triggers a **`4.5 s` 4-step physical module swap** using 07's small fold-out **side service arm** and its central **chest weld emitter (`CutterOrigin`)**:

| Frame | Time Window | Physical Action on 07 v3 (`RoverModelNodes.cs`) | Audio & Visual Details (Vacuum Logic) |
| :--- | :---: | :--- | :--- |
| **Frame 1: Unlatch the Broken Module** | `0.0 s – 1.1 s` | 07 locks its rear brakes. Its small articulated brass-and-steel **service arm** unfolds from the port flank rail, reaches down into the center hub of `Wheel_FL`, grips the cam-lock ring of the burnt-out brushed DC hub motor core, and twists it `45°` counter-clockwise to unlatch it. | Crisp mechanical cam-latch *clack-unk* (`A3 → D3`). 07's CRT `Head` pitches down-left (`LookAt(-0.5, -0.6, 0.6)`) with focused `\| \|` eyes watching the wheel hub. |
| **Frame 2: Drop It on the Ground (Persistent Debris)** | `1.1 s – 2.2 s` | The service arm slides the scorched, blackened hub motor cylinder (charred copper windings and cracked brass end-cap) out of `Wheel_FL`'s axle cavity and **drops it onto the lunar regolith** beside the tyre. **It stays on the ground as a small piece of persistent physical debris** (`Debris_BurntModule`), marking where 07 repaired itself on the Moon. | Heavy `1/6 g` thud in the dust (`D2`) kicking up a tiny low ballistic arc of grey regolith grains (zero dust cloud in vacuum). |
| **Frame 3: Seat the New Module** | `2.2 s – 3.4 s` | The service arm swings back to the open rear `CargoHold` rack, pulls the fresh **ORU Repair Kit Cartridge** (clean brushed steel & sage-green housing with bright copper coils and blind-mate gold pins), and slides it squarely into `Wheel_FL`'s open hub socket until the cam-latch clicks home. `Wheel_FL` immediately snaps from its `5°` bent camber back to `0°` upright and spins a brisk test quarter-turn! | Satisfying precision latch *click-whirrr* (`D4 → F#4`) as the new hub motor energizes. One `item.repair_kit` is consumed from `CargoHold`. |
| **Frame 4: Weld the Skin Plate & Cable Splice** | `3.4 s – 4.5 s` | The service arm holds the kit's stamped sage-green protective cover plate (`R1_SagePatch` `#627360` with 4 brass corner rivets) and copper cable splice flush over the wheel-arch/hub seam. **07's own central brass chest emitter (`CutterOrigin`, between the three front lamps)** angles down toward the seam and fires a blazing golden-amber vacuum arc-weld beam across the plate edge for `0.9 s`, then the service arm folds back flush. | Crisp arc-weld sizzle with **short, straight ballistic spark streaks** hitting the regolith (zero smoke). 07's CRT eyes squint (`> <`) during the weld flash, then beam with happy crescents (`^ ^`) and a rising D-major pentatonic triad (`D4 → F#4 → A4`) as `WearIntegrity` steps up to `50%`! |

---

### 2.2 Base Repair: The Service Gantry (`structure.service_gantry`)

![Service Gantry concept art with 07 v3 inside](../../Logs/concepts/service_gantry.png)

While single-use Field Repair Kits consume crafted cartridges in `CargoHold` and step wear up by one milestone at a time (up to `90%`), the **Service Gantry** is an early, cheap, permanent base structure that **overhauls 07 all the way to `100%` Pristine** automatically using base electricity ([`PowerGrid.cs`](../../Assets/_Project/Scripts/Gameplay/Building/PowerGrid.cs)).

#### Structure Specification (`StructureDefinition.cs` Compatible)

| Field (`StructureDefinition.cs`) | Value (`[PROPOSAL]`) | Design Rationale |
| :--- | :--- | :--- |
| **`_id` (`StructureIds`)** | `"structure.service_gantry"` | Added alongside `SolarPanel`, `BatteryRack`, `Fabricator`, `RoverBay` in [`StructureIds.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs#L12-L20). |
| **`_nameKey`** | `"building.service_gantry.name"` | Display name: **Service Gantry** (*Khung Bảo Dưỡng*). |
| **Footprint (`_width × _depth × _height`)** | **`2.8 m × 2.4 m × 2.35 m`** | Compact drive-through arch (`07 v3` is `1.48 m` wide × `1.49 m` long × `1.76 m` tall), slightly smaller than `UpgradeBay.fbx` (`3.20 m × 2.82 m` in [`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py)). |
| **`_maxGroundSlope` / `_clearance`** | **`15.0°` / `0.25 m`** | Matches standard base pad placement rules ([`StructureDefinition.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureDefinition.cs)). |
| **`_buildCost` (`Recipe`)** | **`3 × Metal` + `2 × Wiring` + `1 × Optics`** | Early and cheap T1 recipe—buildable within the first 15 minutes right after a Solar Panel and Battery Rack. |
| **`_buildSeconds`** | **`4.0 s`** | Fast assembly animation on the build pad. |
| **Power (`_consumeWatts` on `PowerGrid.cs`)** | **`120 W` (active repair) / `10 W` (idle standby)** | Draws from connected `SolarPanel` / `BatteryRack` network (`PowerGrid.cs`). If the grid has partial power (`0 < satisfaction < 1`), repair speed scales linearly with available power; at `0 W` during unbuffered lunar night, its work-lamp dims and repair pauses. |
| **`_ports` (`StructurePort[]`)** | **`2` ports:**<br>`(-1.30, 0.08, 0.0)` dir `(-1, 0, 0)`<br>`(+1.30, 0.08, 0.0)` dir `(+1, 0, 0)` | Left and right plinth cable sockets for daisy-chaining into the base power grid. |
| **Repair Rate** | **`+14% Wear / s`** (`~7.0 s` from `1% → 100%`) | Fast enough to feel snappy, slow enough (`3–7 s`) for the player to enjoy the tactile hammering, module calibration, and welding animation. |

#### Silhouette, Readability at a Glance & 1980s Retro-Future Style
- **Reads at a Glance:** Even from `60 m` away across the basin, the Service Gantry is unmistakable: an open **drive-through A-frame portal** with yellow-and-black hazard wheel guides on a dark iron ramp (`R1_Charcoal` `#26292E`), two cream structural uprights (`R1_HullCream` `#E4DFD3`), an angled 1980s dark-glass CRT diagnostic board on the right pillar (`HULL: 100%`), and **two prominent overhead articulated brass/steel tools** hanging from a crossbeam rail:
  1. **Port Arm:** A chunky brass-and-steel **pneumatic planishing hammer** (`R1_Brass` + `R1_Steel`) that physically taps out hull dents (`tap-tap-clink` rhythm tuned to `D3/A3`).
  2. **Starboard Arm:** An articulated copper **arc-welder nozzle** with coiled orange power leads that stitches cracked seams with short, straight ballistic golden sparks and buffs off temporary field `R1_SagePatch` cover plates so 07 leaves with a clean `100%` factory-white hull.
- **Bonus Convenience:** While parked inside a powered Service Gantry, 07 also receives a standard base umbilical trickle charge (`+5%/s` battery), making the Service Gantry the natural "pit-stop arch" at the entrance to every base and forward outpost.

---

### 2.3 Optional Later Upgrades: Structural Mitigation Kits (`0.6+`)

As 07 explores rougher `0.6+` regions, the player can craft and install permanent structural kits at the **Rover Bay / Upgrade Bay** ([`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py)) that visibly alter 07's v3 silhouette and raise its durability:

| Kit ID | Name & Milestone | Recipe (`SalvageMaterial`) | Mechanical Effect on Wear Model | Visible Change on 07 v3 (`RoverModelNodes`) |
| :--- | :--- | :--- | :--- | :--- |
| `kit.bumper_frame` | **Spring-Steel Bullbar Bumper** (`0.5.7` late / `0.6` early) | `4 × Metal` + `2 × Alloy` + `1 × Wiring` | Raises hard-impact speed threshold $v_{\text{min}}$ from **`6.8 m/s` → `8.4 m/s`**. *(At `8.0 m/s` unboosted top cruise speed, 07 can now hit boulders head-on for **`0` wear**!)* | Bolts a heavy tubular charcoal-iron and brass leaf-spring bumper bar across 07's front fascia above the front blade (`DigScoop`) with two rubber impact buffers. |
| `kit.suspension_dampers` | **Nitrogen Piggyback Dampers** (`0.6`) | `2 × Alloy` + `2 × Coolant` + `2 × Wiring` | Raises $v_{\text{min}}$ by **`+1.2 m/s`** (up to **`9.6 m/s`** with Bullbar Bumper, covering full flat Boost!) and reduces high-speed impact curve $\Delta W$ by **`-30%`**. | Mounts finned copper-and-brass piggyback gas reservoirs onto all four shock absorbers (`Shock_FL..RR`) and thickens the coil springs. |
| `kit.hardened_hull` | **Ilmenite-Titanium Laminate Hull** (`0.6+`) | `4 × Alloy` + `3 × Ilmenite` + `2 × OrangeBeads` | Increases `MaxWearIntegrity` from **`100` → `150`** (`+50%` hull pool) and reduces **Reclaimer clamp crush rate** and **micrometeoroid wear** by **`-40%`**. | Adds riveted brushed-titanium and sage-green armor brow plates (`R1_SagePatch` + `R1_Steel`) over 07's wheel arches, flank shoulders, and CRT visor rim. |

---

## 3. Reclaimers: The Machines That Recycle Old Robots

### 3.1 Story & Lore Integration (`story-v4.md` & `inventions.md`)

To fit seamlessly into [`story-v4.md`](story-v4.md) (Decisions 1–5) and [`inventions.md`](inventions.md#L211-L234) (§2.7 *Mass Driver Rail*):

- **Who Built Them & Why (`1984–1987`):**
  During the final four years of the Departure (`1984–1988`) caused by the **Long Dim / Aerosol Winter** ([`story-v4.md`](story-v4.md) Decision 1), construction of the orbital **Arks in their Earth–Moon L4/L5 halo orbit** ([`story-v4.md`](story-v4.md) Decision 4) hit a severe material bottleneck: raw lunar regolith smelting could not produce refined titanium-alloy plates, high-purity copper windings, and radiation-hardened optics fast enough before the **October 1988 solar storm**.
  - The **ILC Ark Material Command** (`[PROPOSAL]`) contracted the **Tycho Consortium** (`[PROPOSAL]`, the automated industrial agency behind uncrewed **Meridian Station** in [`story-v4.md`](story-v4.md) §0.2 & Decision 5) to deploy **Automated Reclamation Divisions (`ARD-01` through `ARD-06`)**.
  - Their directive (`DIRECTIVE SCRAP-04`) was simple and unsentimental: patrol the lunar mares and highland haul-roads, sweep every piece of hardware with an inductive/optical transponder reader, and **harvest any vehicle, satellite, or apparatus that did not broadcast a valid 1988 Ark Priority Transponder Tag**. Decommissioned 1970s survey rovers, spent landers, and abandoned cables were dragged back to **Reclamation Yards**, sorted into alloy/optics bins, compacted into standard slugs, and fired up the **L5 Mass Driver Rail** to build the Ark hulls.
- **Why They Target 07 in `2024` (36 Years Later):**
  When the **28–29 October 1988 solar storm** hit ([`story-v4.md`](story-v4.md) §2.2), the Lumen Station crew of four (**Ines Kalani, Kenji Morrow, Theo Lindqvist, Ro Amadi**) stayed in their pressure suits to save the water-ice survey and set the rovers to `CONTINUE` before their suits went offline in the sealed habitat (`SUIT 01-04 · OFFLINE · 29 OCT 1988`, [`story-v4.md`](story-v4.md) Decision 2). No one ever sent the `STOP-HARVEST` shutdown code to the automated Reclaimers. Protected by thick cast-iron magnetic-core memory and radiation shielding, the Reclaimers have kept running their loops for **36 years** under the completely dark, city-less Earth ([`story-v4.md`](story-v4.md) Decision 3).
  - Because `07` is a light **Lumen** survey rover woken in `2024` without a `1988` Ark Priority Transponder, any Reclaimer that catches `07` in its inspection light reads:  
    `UNIT: LUMEN-07 // TRANSPONDER: EXPIRED (1988) // CLASSIFICATION: GRADE-A RECLAIMABLE ALLOY & OPTICS`.
  - They are not evil, angry, or aggressive. They are patient, heavy 1980s municipal recycling trucks doing their job for an evacuation that ended 36 years ago.
- **Where Their Yard & The Mass Driver Are:**
  - **The Home Basin (`0.5`) has ZERO Reclaimers** ([`docs/VISION.md` Ruling 17](../VISION.md#L196)): the Home Basin is surrounded by a steep highland ring ridge that the heavy tracked Reclaimers never cross.
  - **Forward Reclamation Yard (`Region 2: The Long Mare` eastern border, `0.6`):** A terraced basalt basin where `ARD-04` operates a regional sorting yard and rail spur feeding northeast toward the **Primary L5 Mass Driver Complex (`Region 6: The Industrial Dark`, [`0.6-world-regions.md`](0.6-world-regions.md))**.

---

### 3.2 Behaviour: Quiet Tension, Grapple Clamps & Yard Capture

Reclaimers never shoot projectiles, never sprint erratically, and never create twitch-combat panic. Their threat is **slow, mechanical, spatial tension**:

![Reclaimer inspection light sweeping the dark mare with 07 hiding in crater shadow](../../Logs/concepts/reclaimer_searchlight_mare.png)

#### 4-Stage Behavioural State Machine (`[PROPOSAL]`)

```mermaid
stateDiagram-v2
    [*] --> Patrol: Fixed route / scrap heap loop (2.2–3.2 m/s)
    Patrol --> Investigate: 07 enters amber light pool on ground OR uses unshielded headlamp/scanner within 35 m
    Investigate --> Patrol: 07 breaks LOS into crater shadow & stays dark/still for 6.0 s
    Investigate --> ApproachAndClamp: Searchlight pool holds on 07 for 2.5 s ("UNTAGGED UNIT LOCKED")
    ApproachAndClamp --> Patrol: 07 outruns (>8.0 m/s) or leaps behind terrain obstacle
    ApproachAndClamp --> TowAndCrush: Front electromagnetic clamps latch onto 07 (within 3.5 m)
    TowAndCrush --> EscapedImmunity: 07 shakes free (Boost / Leap pulse / 4× A-D wiggle)
    EscapedImmunity --> Patrol: 4.0 s magnetic saturation cooldown expires
    TowAndCrush --> YardCaptureDeath: WearIntegrity reaches 0% while held in clamps
```

1. **Patrol (`2.2–3.2 m/s`):**
   - Reclaimers follow predictable compacted haul-tracks or circle high-value satellite debris fields without kicking up any atmospheric dust clouds.
   - Each unit sweeps a **sharp-edged elliptical pool of amber-orange inspection light across the lunar regolith** (`28 m` range for Spotters, `38 m` range for Haulers) in a steady `6.0 s` pendulum arc, accompanied by a rhythmic mechanical shutter tick (`72 BPM`). Because the Moon is a hard vacuum with no air to scatter light, there is **no visible light cone in empty space**—only the glaring hot lens on the machine and the bright moving pool of light where it hits the ground.
2. **Spot & Investigate (`2.5 s` Verification Window):**
   - If 07 drives into the amber light pool on the ground—or leaves its own bright headlamps (`L`) switched on within `35 m` in line-of-sight—the Reclaimer stops its sweep, centres its light pool on 07, and shifts its shutter tick to double-time (`144 BPM`) while querying 07's transponder for `2.5 s`.
   - **Immediate Counterplay:** If 07 switches off its headlamps, darts behind a boulder or crater rim into razor-sharp lunar shadow, and stays still before the `2.5 s` query finishes, the Reclaimer's optical/inductive sensor loses lock, emits a low two-note `B2 → D2` stand-down hum, and resumes patrolling.
3. **Approach & Magnetic Clamp (`4.5–6.0 m/s`):**
   - Once locked (`UNTAGGED SCRAP CONFIRMED`), the Reclaimer energizes its front copper-wound **electromagnetic grapple clamps** and trundles toward 07.
   - Because 07's top cruise speed is `8.0 m/s` (`9.6 m/s` with boost), **a healthy 07 is always faster than a Heavy Hauler (`4.5 m/s`)** and can outrun or leap over a crater wall to escape—unless 07 is cornered in a wreck trench, slowed by Milestone 3/4 wear (`≤ 25%`), or tethered by a fast **Nimble Spotter (`6.5 m/s`)**.
4. **Held in Clamps — Drag, Wear Drain & Shake-Free Escape:**
   - If the clamps latch (`< 3.5 m`), 07 is pulled into the Reclaimer's front magnetic jaw.
   - **While Held:** The Reclaimer drags 07 backward along the haul road toward the **Reclamation Yard** at `2.5 m/s` while the clamping magnets compress 07's hull, draining **`7.0% Wear / s`** (`[PROPOSAL]` range `6%–9%/s`) with rhythmic hydraulic groans and hull creaks.
   - **How 07 Shakes Free (Active Physical Counterplay):**
     A small 3-bar **Magnetic Slip Gauge** appears beside 07:
     - **Option A — Leap Coil Counter-Pulse (`Space`):** Discharging 07's electromagnetic Leap Coils inside the clamp creates an opposing magnetic flux spike that immediately fills **`+60%` of the break-free gauge** (`+40%` if coils are misfiring at `≤ 10%` wear) and shoves the clamp jaws apart!
     - **Option B — Boost Coil Surge (`Shift`):** Firing wheel Boost Coils adds **`+45%/s`** slip progress.
     - **Option C — Wheel Torque Wiggle (`A / D` alternating):** Rocking 07's high-torque wheels left and right shears the magnetic contact air-gap (`+25%` per rhythmic rock; 4 rocks breaks free even with no Leap or Boost kit!).
     - Upon reaching `100%` slip (typically within `1.5–2.5 s`, losing only `10%–18%` wear), the magnetic lock snaps with a resonant brass-iron *CLANG*, kicking 07 backward `4 m` and granting a **`4.0 s` Magnetic Saturation Immunity window** so 07 can cleanly drive away.

#### What Happens If Wear Hits Zero Inside a Reclaimer's Clamps? (`[DECIDED]`)

> [!IMPORTANT]
> **One-Sentence Physical Rule in a Reclaimer's Clamps ("Sorted at the Yard" — `[DECIDED]`):**  
> If 07 hits `0%` wear inside a Reclaimer's clamps, **the Reclaimer hauls 07 to the Reclamation Yard and dumps 07's Cargo Beacon Pod into `SORTING BIN 03: UNTAGGED UNITS`, while Tilly (`friend.tilly`) tows 07's lightened hull back to the last Service Gantry / Rover Bay to be hammered flat and welded back together.**
> - **Why This Works Cleanly:**
>   1. **Zero mixed metaphors:** There is no memory backup, no spare chassis, and no duplicate hull—it is the exact same physical tow-and-repair rule as a field crash (§1.6.B), except the Reclaimer carried your dropped Cargo Beacon Pod all the way to `Sorting Bin 03` at the Reclamation Yard!
>   2. **Great gameplay payoff:** Your Cargo Beacon Pod sits inside `Sorting Bin 03` glowing warmly on your compass (marking the Reclamation Yard's location) right beside **bonus piles of high-tier T2 salvage (`Alloy`, `CircuitChips`, `Optics`)** that the Reclaimers gathered from other 1980s wrecks, turning your capture into a rewarding salvage-yard heist.

![The Reclamation Yard at Lunar Dusk](../../Logs/concepts/reclamation_yard_dusk.png)

---

### 3.3 Progression Arc: Counterplay → Understanding → Ally

Every threat in *Lunarion* follows a non-violent four-step mastery arc:

| Stage | Player Action | Mechanical & Narrative Payoff |
| :--- | :--- | :--- |
| **1. Counterplay (Avoid & Hide)** | - Switch off headlamps (`L`) when approaching an amber horizon light pool.<br>- Use crater rims, basalt boulders, and razor-sharp lunar shadows to stay out of the sweeping light pool on the ground.<br>- If grazed by the outer edge of the light pool, **stop moving immediately** in shadow (Reclaimer eddy-current sensors filter out stationary rocks).<br>- If clamped, use **Leap pulse + A/D wiggle** to pop free. | Immediate tension and stealth gameplay on the Long Mare without needing any special gear. |
| **2. Understanding (Scan a Dormant/Broken Unit)** | - Along the western rim of the Long Mare (or just outside the Yard), 07 discovers a **crushed `SHEAR-85` Spotter** pinned under a basalt rockslide with its inspection lamp still clicking weakly (`D4` tick).<br>- Scanning (`Left Click / E`) its exposed **Magnetic-Core Transponder Reader** unlocks the encyclopaedia entry *"1985 Ark Directive SCRAP-04"* and the **Ark Transponder Tag** schematic. | Reveals the story of the L4/L5 Ark material drive and explains *why* the Reclaimers grab untagged rovers. |
| **3. Immunity (Craft the Ark Transponder)** | - Craft the **Ark Transponder Tag (`kit.ark_transponder`)** at the Fabricator:<br>  **Recipe:** `1 × CircuitChips` + `2 × Wiring` + `1 × Optics` (`T2`, `0.6`).<br>- Visually mounts a compact brass-and-sage dipole transponder box onto 07's rear `Antenna` mast. | When any Reclaimer's inspection light sweeps over 07, its reader chimes a warm two-note harmonic handshake (`D4 → A4`), flashes green-amber (`TAG VALID: ARK MAINT. 07`), and **completely ignores 07**—letting 07 stroll right through the Reclamation Yard! |
| **4. Ally & Automation (Reprogram a Reclaimer)** | - In mid/late `0.6`, craft a **Home-Beacon Override Cartridge (`item.reclaimer_override`)**:<br>  **Recipe:** `2 × CircuitChips` + `1 × Crystal` + `2 × Wiring`.<br>- Approach a tagged Heavy Hauler (or repair a dormant Hauler in the Yard), open its rear 1980s magnetic-tape service hatch with 07's chest beam, and slot in the cartridge. | Overwrites the Hauler's drop-off coordinates from `YARD BIN 01` to 07's **Base Storage Yard (`M3-25`)**. The friendly Hauler patrols the mare autonomously and delivers **`4–6` units of `Metal`, `Wiring`, `Ilmenite`, or `Alloy`** to 07's base every lunar day! |

---

### 3.4 The 3 Reclaimer Machine Types & Vacuum-Shapes Audit (`[DECIDED]`)

![The 3 Reclaimer Machine Types Sheet](../../Logs/concepts/reclaimer_types_sheet.png)

Per the owner's Round 3 decision (`[DECIDED]`), **all three Reclaimer machine types (`SHEAR-85`, `BALER-IV`, and `GOLIATH-L5`) are kept in the `0.6+` lineup**. All three are designed as heavy `1984–1987` **Tycho / Ark Material Command** industrial hardware: weathered mustard-ochre (`#C88A32`) and charcoal-iron (`#26292E`) cast armor, faded black-and-amber hazard chevrons, heavy copper electromagnet windings (`#B86B35`), and shuttered searchlight lenses that cast sharp pools of light on the regolith without volumetric air cones.

#### Type A: `SHEAR-85` Nimble Spotter ("Tag-Hound" — `[DECIDED]`)
- **Role:** Fast perimeter scout and magnetic snare unit that patrols ridge roads and flags untagged scrap for the Heavy Haulers.
- **Dimensions & Locomotion:** `2.2 m` long × `1.6 m` wide × `1.8 m` tall (slightly larger than `07 v3`). Moves at `3.2 m/s` patrol / `6.5 m/s` pursuit on **four independent multi-jointed hydraulic spider-wheel legs** (articulated steel legs ending in heavy grooved iron drum wheels that step over crater rocks and pivot `360°` in place).
- **Tools & Silhouette:** Topped by a rotating turret carrying a large **shuttered amber-red searchlight lens**, a parabolic S-band transponder dish, and a front-mounted **electromagnetic snare winch** on a telescoping steel boom.
- **Behaviour:** If it locks onto an untagged 07, it extends its magnetic winch cable (`8 m` reach) to tether 07 (`-40%` top speed, `-3.5% Wear/s`) while sounding a rhythmic `A3` beacon ping to summon the nearest Heavy Hauler. 07 can snap the tether with a single **Leap Coil hop**, a **Boost surge**, or by sweeping 07's **chest Cutter Beam** across the taut cable!

#### Type B: `BALER-IV` Heavy Clamp-Hauler ("Scrap-Crawler" — `[DECIDED]`)
- **Role:** Slow, unstoppable heavy salvage bulldozer that clamps and drags untagged rovers and satellite frames back to the Reclamation Yard.
- **Dimensions & Locomotion:** `4.8 m` long × `3.2 m` wide × `3.0 m` tall (`~3×` 07's mass). Trundles at `2.2 m/s` patrol / `4.5 m/s` approach on **four heavy articulated steel tank-tread bogies** (`R1_Charcoal` tracks with brass bogie pins) that leave deep double-track imprints in the lunar dust (with zero smoke or dust clouds).
- **Tools & Silhouette:**
  - **Front:** Two massive horseshoe-shaped **copper-wound electromagnetic grapple clamps** mounted on hydraulic rams, plus a roof-mounted `1980s` shuttered amber floodlight lens and an inductive tag-reader coil.
  - **Rear:** An open slanted steel **scrap dump-hopper** filled with crushed satellite struts and tangled copper harnesses, plus a rear magnetic-cassette service hatch for Phase 4 reprogramming.

#### Type C: `GOLIATH-L5` Yard Gantry-Crane & Jaw-Crusher (Horizon Landmark — `[DECIDED]`)
- **Role:** Colossal rail-bound industrial landmark at the heart of the **Reclamation Yard**, visible from kilometers away across the Long Mare.
- **Dimensions & Locomotion:** `14.0 m` tall × `18.0 m` span, riding back and forth at `1.0 m/s` along **heavy iron railway tracks** straddling three recessed steel **Sorting Bins (`BIN 01`, `BIN 02`, `BIN 03`)**.
- **Tools & Silhouette:** A towering steel bridge truss carrying a suspended `3.0 m` circular **copper-wound industrial lifting electromagnet** and hydraulic jaw-crusher on braided steel cables, with glowing amber point-lamps along its upper rail.

#### Mandatory Vacuum-Shapes Audit ([`vacuum-shapes.md`](vacuum-shapes.md))
Per `RULES.md` ("Vacuum logic") and [`vacuum-shapes.md`](vacuum-shapes.md), every machine passes the 6-point gate with **zero atmospheric shapes or volumetric air cones**:

| Machine | 1. How does it stay up? | 2. How does it turn? | 3. How does it dump heat? | 4. Why this silhouette? | 5. Any banned Earth-air shapes or light cones? | 6. Could it exist in *2001* / Apollo? |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| **`SHEAR-85` Spotter** | 4 articulated hydraulic legs with grooved steel drum wheels on regolith. | Independent wheel-leg differential torque + turret ring gear. | Dorsal conductive copper radiator fins (radiative, zero fans). | Low-CG quadruped surveyor built to straddle crater boulders. | **NONE** (0 rotors, 0 wings, 0 fins, 0 light cones in space). | **YES** (Lunokhod + Apollo LRV + industrial spider-excavator). |
| **`BALER-IV` Hauler** | 4 heavy articulated steel tank-tread bogies under `1/6 g` ballast weight. | Skid-steer differential track drive. | Rear flat black-anodized radiative heat-sink plates (`R1_Charcoal`). | Bulldozer + open ore-hopper + front horseshoe electromagnets. | **NONE** (0 exhaust stacks with smoke, 0 fans, 0 light cones in space). | **YES** (1980s strip-mine crawler adapted for vacuum). |
| **`GOLIATH-L5` Yard Crane** | Flanged steel railway wheels on bolted basalt rail sleepers. | Linear rail travel + overhead trolley winch. | Passive structural steel truss radiation into `3 K` black sky. | Bridge crane straddling sorting bins at the Mass Driver loader. | **NONE** (0 wind vanes, 0 cooling towers, 0 vertical air beams). | **YES** (Heavy industrial rail gantry). |

---

### 3.5 Sound Language (D Major Pentatonic: `D E F# A B`)

Consistent with [`docs/VISION.md`](../VISION.md) Pillar 5 and our audio direction, Reclaimers never use harsh dissonant horror screeching or combat sirens. Their soundscape is **heavy, mechanical, harmonic, and hypnotic**—pitched strictly to **D major pentatonic (`D E F# A B`)**:

1. **The Low Motor Drone (`D1 + A1` Open Fifth):**
   - A deep, warm sub-bass hum (`36.7 Hz` `D1` fundamental + `55.0 Hz` `A1` fifth) generated by high-torque brushed DC traction motors and steel track links clanking slowly over basalt (`clack-thrum... clack-thrum`). In vacuum, the sound gets louder and fuller when 07's wheels touch the same rock slab or haul road (ground-borne vibration conduction).
2. **The Searchlight Sweep Tick (`F#4` Relay Metronome):**
   - As the shuttered amber inspection lamp swings left and right, a crisp mechanical escapement relay ticks on **`F#4` (`370 Hz`)** at `72 BPM` (1.2 ticks/s).
   - If 07 enters the ground light pool (`Investigate` state), the tick doubles to `144 BPM` and alternates **`F#4 – A4`** like a questioning telegraph.
3. **The Magnetic Clamp Clunk & Eddy-Current Hum (`D2` Thud + `B2 → E3` Hum):**
   - When the horseshoe electromagnet energizes, a resonant transformer hum rises from **`B2` to `E3`**, culminating in a heavy damped brass-on-iron **`D2` clunk** when the jaws latch.
4. **The Transponder Handshake Chime (`D4 → F#4 → A4`):**
   - Once 07 crafts the **Ark Transponder Tag**, being swept by a Reclaimer's inspection light produces a warm, reassuring three-note major triad arpeggio (`D4 – F#4 – A4`) as the machine salutes 07 and rolls peacefully past.

---

## 4. Regional Hazards (`0.6+`, 1–2 Lines Each)

Beyond hard driving impacts and Reclaimers, each `0.6+` world region ([`0.6-world-regions.md`](0.6-world-regions.md)) features a signature **lunar physical phenomenon** that gradually drains wear if entered unprotected—and each is neutralized by a **crafted protection kit** that doubles as a natural **progression gate** into that region:

| Region (`0.6-world-regions.md`) | Lunar Phenomenon & Wear Drain (1–2 Lines) | Crafted Protection & Region Gate (`SalvageMaterial`) |
| :--- | :--- | :--- |
| **1. The Long Mare (`Region 2`) — Micrometeoroid Showers** | Periodic hypervelocity dust-grain showers streak silently from the black sky, peppering exposed plains with tiny bright impact sparks (`-4% Wear/hit` unless 07 shelters under a gantry roof, boulder overhang, or wreck arch). | **Whipple Canopy Shield (`kit.whipple_shield`):** Crafted from `3 × Alloy + 2 × Metal`. Bolts a spaced dual-layer sacrificial brass-and-sage bumper canopy over 07's head and shoulders, reducing micrometeoroid wear to **`0%`**. |
| **2. Mare Rims & Dawn Terminator — Electrostatic Dust Levitation** | Along sunlit/shadowed terminator ridges, solar UV charges razor-sharp silicate grains that hop electrostatically up to `1.5 m`, clinging to 07's joints and abrading wheel bearings (`-2.5% Wear/s` while driving fast across active terminator zones). | **Copper Grounding Whiskers & Seal Boots (`kit.grounding_whiskers`):** Crafted from `3 × Wiring + 2 × OrangeBeads`. Trailing braided copper discharge brushes on 07's rear bumper bleed off static charge and negate dust abrasion. |
| **3. The Glass Field (`Region 3` — Aristarchus) — Razor Impactite & Thermal Vent Shock** | Fields of volcanic orange glass (`OrangeBeads`) and fractured impact-melt ribbons slice unarmored rubber tyres above `5.0 m/s` (`-3% Wear/s`), while active outgassing fissures vent sudden ballistic vapour pulses. | **Sintered Ilmenite Tyre Treads (`kit.sintered_treads`):** Crafted from `4 × Ilmenite + 2 × Glass + 2 × Metal`. Replaces 07's rubber chevron treads with woven steel-ilmenite mesh tyres (like the Apollo LRV), granting full immunity to glass shards. |
| **4. The South Pole (`Region 5` — Shackleton Interior) — Deep Cryogenic Cold (`40 K`)** | Inside permanently shadowed polar craters where sunlight has not touched in two billion years, `40 K` extreme cold makes standard hull steel brittle (`2.0×` impact wear) and steadily freezes unheated actuator seals (`-2.0% Wear/s` after `20 s` in deep shadow). | **Closed-Loop Thermal Circulation Jacket (`kit.cryo_heater`):** Crafted from `3 × Coolant + 2 × OrangeBeads + 2 × Alloy`. Circulates warm fluid from 07's battery bus through copper manifold pipes on `Body`, keeping 07 warm and immune in eternal shadow. |
| **5. The Industrial Dark (`Region 6` — Mass Driver) — Leaking Cryo-Hydrazine & Inductive Arcing** | Cracked Departure-era propellant pipelines and unshielded Mass Driver capacitor banks leak frozen corrosive thruster salts and high-voltage ground arcs across the factory floor (`-5.0% Wear/s` in hazard pools). | **Fluoropolymer Dielectric Undercoat (`kit.chemical_seal`):** Crafted from `2 × Superconductor + 2 × Coolant + 2 × Glass`. Insulates 07's lower tub and wheel hubs against chemical corrosion and inductive rail arcs. |

---

## 5. Phasing, Code Domains & Automated Tests (Design Only — Post Core Feel Gate)

> [!NOTE]
> **Implementation Gate (`docs/ROADMAP.md`, "Cổng lõi cảm giác"):** This entire feature is **design only** right now. None of the classes, assets, or tests below should be implemented until the project passes the **Core Feel Gate** ([`docs/ROADMAP.md`](../ROADMAP.md), *"Cổng lõi cảm giác"*).

### 5.1 Milestone Phasing (After Core Feel Gate)

| Milestone | Scope Shipped (Post Core Feel Gate) | Player Experience Goal |
| :--- | :--- | :--- |
| **Phase A (`0.5.7` Base Building Slice)** | 1. **Core Wear Model & 4 Milestones:** Relative normal speed $v_\perp$ impact math (`6.8 m/s` threshold), hard vs. regolith collider classification, creak warning (`6.5 m/s+`), and the **4 subsystem failure milestones (`75%` bent antenna/static, `50%` dead lamp/strut sag, `25%` seized dragging wheel hub motor, `10%` CRT tear/scan glitch/leap misfire)**.<br>2. **Death & Restart Loop (`[DECIDED]`):** 3-frame CRT line collapse (`R1CrtFace.shader`), **Cargo Beacon Pod** drop & recovery (`M3-25`), Tilly tow-back & restart at last Service Gantry / Rover Bay.<br>3. **Physical Repair (`[DECIDED]`):** Single-use **Field Repair Kit** (`item.repair_kit`, `2 Metal + 2 Wiring + 1 Optics`, `4.5 s` 4-frame ORU module swap leaving broken module debris on the ground) + buildable **Service Gantry** (`structure.service_gantry`, `3 Metal + 2 Wiring + 1 Optics`, `120 W` on `PowerGrid.cs`).<br>4. **New Game Modes (`[DECIDED]`):** **Survival** (default) and **"Explore only" / "Chỉ khám phá"** (no wear, no death). *(Opt-in **Hardcore** deferred to `1.0`.)* | Establishes the tactile loop of careful high-speed driving, joyous penalty-free regolith bouncing, creative subsystem failures, ORU module swaps in the field, and returning home to the Service Gantry. |
| **Phase B (`0.6+` World Regions Slice)** | 1. **All 3 Reclaimers (`[DECIDED]`):** `SHEAR-85` Spotter, `BALER-IV` Heavy Hauler, and `GOLIATH-L5` Yard Gantry-Crane.<br>2. **Reclamation Yard & Capture Rule (`[DECIDED]`):** Sorting Bins (`Bin 03` cargo recovery + bonus T2 salvage) at the Long Mare / Industrial Dark border.<br>3. **Mastery Arc:** Dormant Reclaimer scan → **Ark Transponder Tag** (`kit.ark_transponder`) → **Home-Beacon Override Cartridge** (reprogrammed Hauler delivering scrap to base).<br>4. **Regional Hazards & Mitigation Kits:** Bullbar Bumper, Nitrogen Dampers, Hardened Hull, plus the 5 regional hazard protection kits. | Expands wear from driving physics into world exploration, stealth tension, region gating, and late-game base automation. |

---

### 5.2 Code Domains Touched (Planned for Implementation)

| Domain / Assembly | Existing Files Referenced | Planned New / Modified Types |
| :--- | :--- | :--- |
| **`Core` (`Project.Core`)** | [`SalvageMaterial.cs`](../../Assets/_Project/Scripts/Core/SalvageMaterial.cs) | `GameMode.cs` (`Survival`, `ExploreOnly`, and `1.0` opt-in `Hardcore`), `WearState.cs` (pure C# serializable struct, milestone evaluator & impact math helper). |
| **`Rover` (`Project.Rover`)** | [`RoverController.cs`](../../Assets/_Project/Scripts/Rover/RoverController.cs), [`DriveSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/DriveSettings.cs), [`BoostSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/BoostSettings.cs), [`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs), [`FaceDisplay.cs`](../../Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs), [`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs) | `WearSettings.cs` (`ScriptableObject`), `RoverWearController.cs` (`OnCollisionEnter` $v_\perp$ filter, `75%/50%/25%/10%` subsystem milestone modifiers, `Wheel_FL` lock/furrow trigger, creak audio trigger, death sequence), `RepairKitApplicator.cs` (`4.5 s` 4-frame ORU module swap state machine driving the side service arm, dropping persistent `Debris_BurntModule`, seating the new ORU cartridge, and firing `CutterOrigin` across the skin plate). |
| **`Gameplay` (`Project.Gameplay`)** | [`StructureDefinition.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureDefinition.cs), [`StructureIds.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs), [`PowerGrid.cs`](../../Assets/_Project/Scripts/Gameplay/Building/PowerGrid.cs) | `StructureIds.ServiceGantry`, `ServiceGantryController.cs` (pad trigger, `PowerGrid` satisfaction check, `+14%/s` repair tick to `100%`, arm animations), `CargoBeaconPod.cs` (world entity storing dropped `CargoHold` inventory stacks), `RespawnCoordinator.cs`. |
| **`World` (`Project.World` — `0.6+`)** | `WorldRegions`, `SalvageNode` | `ReclaimerController.cs` (`Patrol / Investigate / Clamp / Tow / Ally` state machine), `ReclaimerSearchlight.cs` (ground light-pool LOS + shadow raycast check), `ReclamationYardBin.cs` (`Bin 03` cargo deposit), `RegionalHazardZone.cs`. |
| **`UI` (`Project.UI`)** | [`CellRowSettings.cs`](../../Assets/_Project/Scripts/UI/Tuning/CellRowSettings.cs) | `WearGaugeView.cs` (4-milestone segmented subsystem bar beside battery row, auto-hidden at `100%` or in `ExploreOnly` mode), `MagneticSlipPromptView.cs` (break-free widget when clamped). |
| **`Art & Shaders`** | [`R1CrtFace.shader`](../../Assets/_Project/Shaders/R1/R1CrtFace.shader), [`build_rover.py`](../../tools/art/blender/rover/build_rover.py), [`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py) | Dent blendshapes, side service arm + ORU cartridge & `Debris_BurntModule` mesh nodes + `R1_SagePatch` cover plate on `Rover_07`, `ServiceGantry.fbx` generator (`tools/art/blender/service_gantry.py`), `CargoBeacon.fbx`, Reclaimer meshes (`0.6+`). |
| **`Audio` (`Project.Audio`)** | Procedural / clip synth | Chassis creak loop (`6.5–12.0 m/s`), milestone warning beats (`75%` static snap, `50%` bulb pop/strut thud, `25%` hub gear crunch & regolith drag loop, `10%` CRT sync buzz & coil sputter), CRT collapse discharge, Reclaimer `D1/A1` motor drone & `F#4` shutter tick. |

---

### 5.3 Planned Automated Test Suite (`EditMode` & `PlayMode`)

When implementation opens after the Core Feel Gate, every mechanic will ship with deterministic EditMode and PlayMode tests:

1. **Impact Threshold & Curve Math (`RoverWearMathTests.cs` — EditMode):**
   - `ImpactBelowThreshold_DealsZeroWear`: Verifies $v_\perp \in [0, 6.79]\text{ m/s}$ returns exactly `0f` wear loss.
   - `GlancingScrapeAtFullCruise_DealsZeroWear`: Verifies `8.0 m/s` velocity at $20^\circ$ incidence angle ($v_\perp = 8.0 \sin 20^\circ = 2.74\text{ m/s} < 6.8\text{ m/s}$) returns `0f` wear loss.
   - `RegolithSurface_NeverDealsWearAtAnySpeed`: Verifies contact with a `Terrain`/`Regolith` collider at `15.0 m/s` returns `0f` wear loss.
   - `HardImpactAboveThreshold_FollowsConvexCurveAndCapsAtMax`: Verifies $v_\perp = 6.8\text{ m/s} \to 4.0$, $v_\perp = 8.0\text{ m/s} \to \approx 12.0$, $v_\perp = 9.6\text{ m/s} \to \approx 24.0$, and $v_\perp \ge 12.0\text{ m/s} \to 40.0$ (capped).
   - `PostHitGraceWindow_SuppressesSecondaryBounce`: Verifies a second contact `0.4 s` after a hit deals `0` additional wear unless its $v_\perp$ exceeds the first hit.
2. **Subsystem Wear Milestones (`RoverWearMilestoneTests.cs` — EditMode):**
   - `MilestoneTransitions_At75_50_25_And_10Percent`: Verifies `Nominal (100–76%)`, `Milestone1_Antenna (75–51%)`, `Milestone2_StrutLamp (50–26%)`, `Milestone3_WheelMotor (25–11%)`, and `Milestone4_CrtBus (10–1%)`.
   - `Milestone1_BendsAntennaAndReducesScanRangeBy15Percent`: Verifies `Antenna` bend angle `18°`, scan range multiplier `0.85f`, and `100%` driving speed (`8.0 m/s`).
   - `Milestone2_KillsPortHeadlampSagsFrontLeftStrutAndSkewsSteering`: Verifies at `≤ 50%` wear, `Lamp_0` emission is `0f`, `Shock_FL` Y-offset is `-0.07f` (`6.5°` lean), cruise speed is `7.2 m/s` (`0.90f`), and left turn rate multiplier is `0.82f`.
   - `Milestone3_SeizesFrontLeftWheelCapsSpeedAt5Point6AndAppliesLeftYawPull`: Verifies at `≤ 25%` wear, `Wheel_FL` locks and carves a regolith drag furrow, max cruise speed is `5.6 m/s` (`< 6.8 m/s` impact threshold), Boost is disabled, and `8.5°/s` leftward pull requires `~18%` right counter-steer.
   - `Milestone4_TearsCrtGlitchesScannerAndMisfiresLeapCoils`: Verifies at `≤ 10%` wear, max speed is `4.4 m/s`, pressing `Space` triggers a `0.35 m` lopsided misfire sputter-hop, and scan lock duration multiplier is `1.6f`.
3. **Death, Restart & Cargo Beacon Recovery (`RoverDeathAndBeaconPlayModeTests.cs` — PlayMode):**
   - `ZeroWear_TriggersCrtCollapseAndSpawnsCargoBeacon`: Reduces wear to `0%` while holding `3 Metal + 2 Optics`; asserts CRT `ScreenVerticalPinch` reaches `1.0`, a `CargoBeaconPod` spawns at the death position containing `3 Metal + 2 Optics`, and 07's hold is emptied.
   - `RestartFlow_PlacesRoverAtLastServiceGantryWithKitsIntact`: Asserts 07 restarts at the last visited `ServiceGantry` (or `RoverBay`) with `100%` wear and all equipped kits preserved.
   - `CargoBeaconRecovery_RestoresDroppedHoldInventory`: Drives 07 to the `CargoBeaconPod`, triggers recovery, and asserts `3 Metal + 2 Optics` return to 07's hold and the beacon despawns.
4. **Physical Repair — ORU Module Swap & Service Gantry (`RepairPlayModeTests.cs` — PlayMode):**
   - `RepairKit_SingleUse_SwapsMostSevereFailedModuleSpawnsDebrisAndRestoresOneMilestone`: Applies `item.repair_kit` at `20%` wear (Milestone 3); asserts the `4.5 s` 4-frame sequence unlatches the broken hub motor, spawns persistent `Debris_BurntModule` on the ground, seats the new ORU module, welds the skin plate, consumes `1` kit, clears Milestone 3, and restores wear to `50%`.
   - `FieldRepair_WithoutCraftedKit_CannotRun`: Asserts pressing Repair (`R`) with `0` `item.repair_kit` in `CargoHold` performs no repair.
   - `ServiceGantry_RepairsRoverTo100PercentOnlyWhenPoweredByGrid`: Places damaged 07 (`50%` wear) inside `ServiceGantry` with `0 W` power (asserts wear stays `50%`), then connects a `SolarPanel` (`120 W+`) and asserts wear climbs to `100%`.
5. **"Explore only" ("Chỉ khám phá") Mode Invariance (`ExploreOnlyModeTests.cs` — EditMode & PlayMode):**
   - `ExploreOnlyMode_HardImpactAndReclaimerClamp_NeverReduceWear`: In `GameMode.ExploreOnly`, slams 07 into a hard boulder at `12.0 m/s` and simulates hazard exposure; asserts `WearIntegrity` remains `100%` and HUD wear bar stays hidden.

---

## 6. Risks and Answers

### 6.1 Risk: Frustration and Lost Time
- **The Risk:** In a relaxing exploration game, dying 2 km from home or losing hours of progress to an unseen rock on a dark slope can cause players to quit in frustration.
- **Our Answer (5 Layers of Protection):**
  1. **Zero Permanent Progress Loss (`[DECIDED]`):** Installed kits, base structures, blueprints, and story progress are **never** lost on death—only the current trip's unbanked hold cargo stays in the Cargo Beacon.
  2. **Joyful Bounces Stay Free:** `100%` of regolith landings, low-gravity hops, and bumps below `6.8 m/s` cost **zero wear**. You only take impact wear when driving near full cruise/boost speed (`6.8–12.0 m/s`) straight into a solid rock or metal wall.
  3. **Loud Early Warning + Self-Protecting Milestone 3/4 Speed Cap:** The `6.5 m/s` chassis creak warns you before impact speed is reached, and **Milestone 3 (`≤ 25%`) caps top speed at `5.6 m/s`** (`4.4 m/s` at `≤ 10%`)—meaning a badly damaged 07 limping across flat terrain *cannot accidentally kill itself* on a stray rock.
  4. **Forward Service Gantries Shorten Every Trip:** Because the **Service Gantry** costs only `3 Metal + 2 Wiring + 1 Optics`, players naturally build one at every forward outpost, ensuring a restart is never more than `60–90 seconds` from the wreck site.
  5. **Bullbar Bumper Kit Eliminates Cruise-Speed Impacts:** By mid-game (`0.6`), installing the **Bullbar Bumper (`kit.bumper_frame`)** raises $v_{\text{min}}$ to `8.4 m/s`, making normal `8.0 m/s` cruise driving completely immune to rock impacts.

### 6.2 Risk: Breaking the Solitude Pillar (`Pillar 6 — "Alone, and at peace"`)
- **The Risk:** If Reclaimers feel like aggressive patrol guards or combat enemies chasing 07 across every hill, the lonely, contemplative magic of the Moon evaporates.
- **Our Answer:**
  1. **The Home Basin (`0.5`) Is 100% Sanctuary:** No Reclaimer ever enters the Home Basin.
  2. **Slow Industrial Cadence, Not Aggression:** Reclaimers move at `2.2–3.2 m/s` along fixed historical haul-tracks and scrap yards. They never hunt 07 outside their patrol corridors, never fire weapons, and announce their presence from afar via warm, sweeping amber light pools on the ground and low `D1/A1` motor hums.
  3. **From Stranger to Quiet Worker:** Once 07 crafts the **Ark Transponder Tag**, Reclaimers become peaceful background machinery—lumbering 1980s relics of the Departure that salute 07 with a gentle `D–F#–A` chime and can even be reprogrammed to haul scrap to 07's base. They reinforce the loneliness of the 1988 evacuation rather than breaking it.

### 6.3 Risk: Alienating Players Who Want Pure Exploration → The "Explore only" ("Chỉ khám phá") Mode Answer (`[DECIDED]`)
- **The Risk:** Some players fell in love with *Lunarion* specifically for stress-free, zero-fail-state lunar tinkering.
- **Our Answer:** **"Explore only" (`"Chỉ khám phá"`)** is a first-class option on the New Game screen (not a hidden cheat). In **"Explore only"** mode, wear is locked at `100%`, the wear HUD gauge never appears, and Reclaimers never clamp 07—yet the player still gets to experience the full story, scan the Reclaimers, visit the Reclamation Yard, and reprogram a Hauler to bring scrap home.

### 6.4 What *Subnautica* Teaches Here, and Where We Improve on It

| What *Subnautica* Taught Us (And We Keep) | Where *Subnautica* Stumbled → How *Lunarion* Improves on It |
| :--- | :--- |
| **1. Vehicle hull damage makes safe havens feel warm.** Returning a dented *Seamoth* into a *Moonpool* makes your base feel like a real shelter. Our **Service Gantry** recreates that exact tactile relief when the hammer and welding arms descend onto 07. | **1. In *Subnautica*, jumping out of your vehicle with a handheld *Repair Tool* broke vehicle immersion.** In *Lunarion*, you *are* the rover (`Ruling 14`). Applying a **Field Repair Kit (`item.repair_kit`)** is a 4-step physical ORU module swap using 07's own side service arm and central chest weld-beam (`CutterOrigin`) that leaves the broken module on the ground as real debris. |
| **2. Readable spatial hazards create unforgettable stories without giving the player a gun.** Our **Reclaimers** use slow ground searchlight pools, acoustic signatures, and light-discipline counterplay (`headlamps off`). | **2. *Subnautica* threats stayed permanent nuisances even in late game.** Once you scanned a Leviathan, it still bit your sub every time you passed. In *Lunarion*, every threat follows **Counterplay → Understand → Transponder Immunity → Reprogrammed Ally**, turning yesterday's hazard into tomorrow's automated scrap-hauler. |
| **3. Losing your vehicle in deep water was devastating (destroying installed upgrade modules).** | **3. Zero permanent vehicle/kit loss (`[DECIDED]`).** In *Lunarion*, Tilly always tows 07's lightened hull back to the Gantry with 100% of its kits intact; only loose hold cargo stays in a clearly beaconed pod—or lands inside **Sorting Bin 03** at the Reclamation Yard alongside bonus T2 loot! |
| **4. *Subnautica*'s Seamoth took annoying chip damage from bumping small fish at cruise speed, and had no intermediate subsystem states.** | **4. Strict Normal-Speed Threshold ($v_\perp \ge 6.8\text{ m/s}$) + 4 Creative Subsystem Milestones (`75% / 50% / 25% / 10%`).** Gentle bumps, shallow scrapes, and all regolith landings deal **`0` wear**, while crossing `75% / 50% / 25% / 10%` produces distinct, tangible 1980s mechanical failures (bent antenna static, sagging strut + dead lamp, locked dragging wheel carving a regolith furrow at `5.6 m/s`, and CRT tearing). |

