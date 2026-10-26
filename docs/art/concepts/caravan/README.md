# Caravan, Skid Sled & Long-Range Vehicle Concepts (`[PROPOSAL]`)

> **Branch:** `box/agy-caravan`  
> **Context:** End-of-0.6 *"Đi tìm ánh sáng"* expedition caravan (Rover `07` towing a 3-car train of packed base infrastructure across the basin floor), plus the skid-sled/coupler running-gear family and 3 post-0.6 long-range carrier vehicle concepts.  
> **Generator Script:** [`render_caravan_concepts.py`](./render_caravan_concepts.py) (headless Blender 5.1 + `r1kit` procedural builders)

---

## 1. Caravan Look-Test — `07` Towing a 3-Car Train (`caravan_sheet.png`)

![Caravan Look-Test Sheet](./caravan_sheet.png)

### Train Manifest & Canonical Dimensions (`ScaleContractTests`)

| Unit | Role in *"Đi tìm ánh sáng"* | Deployed Footprint (`W × L × H`) | Packed Sled Envelope (`W × L × H`) | Key Visual & Mechanical Read |
| :--- | :--- | :---: | :---: | :--- |
| **Lead Hauler: Rover `07`** | Prime mover, winch tractor & live CRT head | `1.47 m × 2.01 m × 1.59 m` | `1.47 m × 2.14 m × 1.59 m` *(with rear Coupler Rack)* | Pillowed cream-enamel beetle hull, coral beltline, `SolarWing`, rear **Coupler Rack** holding **2 spare short rigid couplers**, copper-wound winch drum at `TowHook` (`Z = -0.72 m`), and 1 warm amber convoy beacon (`R1_LampAmber`). |
| **Link 0 → 1** | Physical Winch Tow Line | — | `1.00 m` active span (`3.5 cm` sag) | Braided `R1_BareSteel` cable with `R1_Brass` strain-relief sleeves and `R1_OrangeStripe` visibility collars; doubles as the steep-grade winch recovery line (`docs/design/research/caravan-tow.md`). |
| **Car 1: Packed `SolarArray`** | Generation pallet (unpacked first at new camp) | `2.41 m × 1.57 m × 1.94 m` | `1.48 m × 1.84 m × 1.08 m` | Octagonal cream/coral gearbox (`Stage_3`) seated in sled cradle; butterfly `Wing_L`/`Wing_R` folded downward (`±46°` A-frame tent fold) under padded coral travel clamp arches; 3 tripod truss legs stowed along side bolsters. |
| **Link 1 → 2** | Short Rigid Coupler #1 | — | `0.76 m` clevis-to-clevis | Tubular `R1_GreyMetal` drawbar with twin A-frame stabilizer struts, pillowed cream/coral damper sleeve, `R1_Brass` spherical ball-joint knuckles, and looped `R1_Rubber` HV power/telemetry jumper. |
| **Car 2: Packed `BatteryBank`** | Night-buffer energy vault (keeps train alive in shadow) | `1.46 m × 0.99 m × 1.60 m` | `1.48 m × 1.68 m × 1.42 m` | Rounded cream submarine vault (`Stage_2`) + 4 dark-charcoal cylindrical cells (`Stage_3`); `SafetyBar` locked down, `Plunger_1..4` seated, `SightGlass_1..4` + `RoofBeacon` glowing warm amber, diagonal orange hold-down struts + rear HV cable spools. |
| **Link 2 → 3** | Short Rigid Coupler #2 | — | `0.76 m` clevis-to-clevis | Identical modular short rigid drawbar coupler swapped from `07`'s rear Coupler Rack. |
| **Car 3: Folded `AntennaMast`** | Telemetry & Earth-link radio mast (caboose) | `1.55 m × 1.26 m × 2.80 m` | `1.50 m × 2.02 m × 1.58 m` | Cream/coral/sage cabinet (`Stage_3`) with front reel-to-reel tape window seated forward (`Z = +0.42 m`); lattice `Mast` (`Mast_Seg1..3`) telescoped and folded backward (`84°`) onto a rear padded A-frame crutch (`Z = -0.58 m`); `Ø 0.95 m` wire-mesh `Dish` stowed horizontally on top; masthead `BeaconLamp` serves as the end-of-train tail light. |

### `[PROPOSAL]` Recommendation for Sheet 1 (`caravan_sheet.png`)

- **Lock the hybrid coupling architecture (`Winch Cable 0→1` + `Short Rigid Couplers 1→2→3`):**
  - Connecting `07` to **Car 1** via the physical **Winch Tow Cable** (`1.0 m` normal tow length, extendable to `15 m` for steep-slope winching) directly supports the core *"winch-up-the-crater-lip"* gameplay loop in `docs/design/research/caravan-tow.md` (`07` unhooks, climbs a `22°` terrace lip solo, anchors, and winches the 3-car train up).
  - Connecting **Car 1 → Car 2 → Car 3** via **Short Rigid A-Frame Couplers** (`0.76 m` span with spherical brass knuckles) prevents rear-end accordion collisions during lunar downhill braking (`1.62 m/s²` gravity means low friction and long stopping distances) and makes backing up / parking the train tractable.
- **Lock the low-to-high-to-long silhouette ordering (`SolarArray` → `BatteryBank` → `AntennaMast`):**
  - From the **chase camera (`7.5 m` distance, `16°` pitch, `52°` FOV)**, keeping **Car 1 (`Packed SolarArray`, `1.08 m` tall)** lowest immediately behind `07` preserves an unobstructed sightline to `07`'s rear Coupler Rack, winch line, and rear wheels, while **Car 2 (`BatteryBank`, `1.42 m`)** and **Car 3 (`Folded AntennaMast`, `1.36 m` with `Ø 0.95 m` dish)** step out cleanly along the turn arc without occluding `07`.

---

## 2. Skid Sled & Coupler Family Variants (`sled_variants.png`)

![Sled Variants Sheet](./sled_variants.png)

### Variant Comparison Table

| Variant | Running Gear & Mechanism | One-Line Readability & Feel Summary | Best Fit |
| :--- | :--- | :--- | :--- |
| **A — Simple Runner Sled** *(Recommended)* | Twin `0.22 m` wide upturned `R1_BareSteel` ski runners (`1.22 m` track matching `07`'s wheel ruts) + 3 elastomeric rubber/brass shock stanchions per runner + rigid A-frame drawbar. | **PRO:** Cleanest silhouette; zero moving parts to jam in abrasive dust; glides squarely inside `07`'s `1.22 m` wheel ruts. **CON:** Higher sliding drag on bare basalt shelves; relies on ski tip curl to ride over sharp ledges. | **Default 0.6 Caravan Sled** (Cars 1–3) |
| **B — Roller Sled** | Upturned cream boat-hull belly pan + 4 wide `Ø 0.27 m` rubber/brass barrel rollers mounted on rocking bogie beams. | **PRO:** Articulated roller bogies visibly bounce over pebbles and reduce towing resistance on hard rock. **CON:** Busier underside silhouette; small drum rollers visually compete with `07`'s iconic `Ø 0.70 m` balloon tyres. | Optional mid-game upgrade for rocky highland crossings |
| **C — Tracked Dolly** | Twin compact stadium-loop rubber crawler pods (semicircular rounded ends, cream bogie shields, brass hubs) + `360°` geared brass turntable bolster. | **PRO:** Highest terrain authority & grade capability; turntable bolster prevents jackknifing in tight turns. **CON:** Highest mechanical/visual weight; reads like an autonomous mini-crawler rather than a passive cargo pallet. | Heavy salvage recovery dolly (moving multi-ton ruin reactors) |

### `[PROPOSAL]` Recommendation for Sheet 2 (`sled_variants.png`)

- **Recommend Variant A (Simple Runner Sled) as the canonical 0.6 caravan sled:**
  1. **Visual Hierarchy:** Because Variant A has no wheels or tracks, `07` remains the undisputed mechanical hero of the train—the 4 balloon tyres belong exclusively to `07`, while the three towed cars read immediately as passive expedition sleds.
  2. **Shared Track Geometry (`1.22 m`):** Setting the twin ski runners at `X = ±0.58 m` (`0.22 m` sole width) places them dead-center inside the `1.22 m` ruts carved by `07`'s `0.26 m` wide tyres. Visually and physically, `07` breaks the trail in deep regolith and the sleds ski effortlessly along the compacted ruts.
  3. **Authentic Lunar Vacuum Logic:** Abrasive lunar fines destroy small exposed roller bearings (Variant B) and complex miniature track pins (Variant C). Sacrificial hardened-steel ski soles (`R1_BareSteel`) with rubber shock blocks are mechanically honest, low-poly (`~1.1k tris` per sled), and trivial to animate with terrain-normal pitch/roll alignment.

---

## 3. Dedicated Long-Range Vehicle Concepts (`vehicle_concepts.png`)

![Long-Range Vehicle Concepts Sheet](./vehicle_concepts.png)

### Concept Comparison Table (All Built at 1:1 Scale with Docked Rover `07`)

| Concept | Envelope (`W × L × H`) | How `07` Docks & Drives | Locomotion & Payload Capacity | Strengths & Tradeoffs |
| :--- | :---: | :--- | :--- | :--- |
| **1. "Nomad" Modular Carrier Cab** *(Seatruck-style Front-Dock Hauler)* | `3.25 m × 4.90 m × 2.38 m` | `07` drives up the rear-center belly ramp into the **front U-shaped horseshoe collar**; pneumatic brass clamp arms lock `07`'s waist so **`07`'s own Amber CRT head peers over the bow visor as the live pilot!** | `6 × Ø 1.08 m` wide-track R1 balloon wheels on rocker-bogies; 8 vertical HV battery cells in twin side pontoons; overhead solar canopy arch; rear caravan hitch. | **PRO:** Preserves `07`'s face, personality, and expressive CRT eyes in every camera angle while tripling wheel scale and range. **CON:** Moderate cargo capacity unless towing sled cars behind its rear hitch. |
| **2. "Boreas" Tracked Crawler Base** *(Mobile Forward Base on Stadium Tracks)* | `3.90 m × 7.20 m × 2.68 m` | Wide rear clamshell ramp (`1.92 m` width) drops to the regolith; `07` drives into the **open central Roll-On Charge Garage** (`Y = 0.56 m` deck) onto a copper inductive pad under the gantry arch. | Quad stadium-loop rubber crawler tracks (soft rounded ends, cream fenders); portside submarine battery vault (5 heavy cells); starboard 3-crate cargo rack; dorsal butterfly solar wings. | **PRO:** True "home on tracks" (`docs/design/ice-and-vehicles.md`); carries a full base worth of power & cargo on its own deck without a trailing train. **CON:** Largest footprint (`3.9 m` wide); requires wide valley corridors. |
| **3. "Tereshkova" Linear-Induction Rail Sled** *(High-Speed Trunk-Line Ferry)* | `2.95 m × 5.80 m × 2.05 m` *(on `1.36 m` gauge twin-tube rail)* | Fold-down side boarding ramp (`+X`) lets `07` drive onto a **central `360°` brass turntable pad** (`Y = 0.46 m` deck height) from either side or end. | 4 articulated pinch-roller & copper linear-induction bogies gripping a modular twin-tubular steel rail spine; symmetrical front/rear gold-foil flywheel capacitor drums; automatic brass rail couplers. | **PRO:** Zero regolith dust kickup, zero wheel slip, highest speed for commuting between permanent bases (*Mare Imbrium ↔ Kepler*). **CON:** Bound to player-built rail splines; cannot free-roam off-track. |

### `[PROPOSAL]` Recommendation for Sheet 3 (`vehicle_concepts.png`)

- **Primary Recommendation for First Playable Long-Range Vehicle (0.7 / 0.8): Concept 1 — "Nomad" Modular Carrier Cab (Seatruck-style Front-Dock Hauler), paired later with Concept 2 ("Boreas" Crawler Base) for endgame basin crossings:**
  1. **Why Concept 1 ("Nomad" Carrier Cab) wins first:**
     - **Zero Loss of Character Identity:** In *MoonProject*, `07`'s expressiveCRT head, neck yaw, and amber scanline eyes are the emotional heart of the game (`docs/VISION.md`). Unlike an enclosed truck cab that swallows the player avatar, Concept 1's **open horseshoe bow collar** turns `07` *into* the cockpit brain of a heavy 6×6 prime mover—when the player steers or looks around, `07`'s head still turns and emotes right at the bow of the vehicle.
     - **Seamless Caravan Integration:** Concept 1 uses the exact same rear brass clevis hitch as `07`, meaning it can either run solo as a fast heavy scout or hook directly onto the 3-car Variant A sled train to haul the entire base up steeper crater exits without winching every terrace.
  2. **How Concept 2 ("Boreas") and Concept 3 ("Tereshkova") fit the long-term roadmap:**
     - **Concept 2 ("Boreas" Tracked Crawler Base)** is the ideal **Act III mobile forward base** once the player crosses into vast, flat mare basins and wants a self-contained rolling workshop where `07` docks on an open inductive deck.
     - **Concept 3 ("Tereshkova" Linear-Induction Rail Sled)** is the ideal **infrastructure logistics payoff** once two permanent outposts are established and the player lays a trunk line across a hazardous shadow zone.
