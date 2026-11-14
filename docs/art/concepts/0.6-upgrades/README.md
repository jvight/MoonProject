# 0.6 Upgrades & Field Charge Pylon — Concept Art & Design Rationale

This directory contains the 0.6 concept renders, quantitative 7.5 m chase-camera silhouette checks, and variant recommendations for **Rover 07's Cell Pack III** (`Back` socket) and the **Field Charge Pylon** (placeable forward charger on a predefined pad), together with the **"Fully Grown 0.6"** composite of Rover 07.

---

## Deliverables in This Folder

| File | Description |
|---|---|
| [`cellpack3_sheet.png`](cellpack3_sheet.png) | **Cell Pack III** (`Back` socket, 8 cells, ~370 m range) — **3 variants** (`A`, `B`, `C`), each stacking on top of the round-3 V1 flank cell pods (`Kit_CellPod`), rendered from the **7.5 m chase cam** (16° pitch, 52° FOV), **front 3/4**, and **rear 3/4** in **Day C** lighting, with 7.5 m silhouette-diff overlays. |
| [`pylon_sheet.png`](pylon_sheet.png) | **Field Charge Pylon** (placeable forward charger on a predefined pad) — **3 variants** (`A`, `B`, `C`) in the VIS / ILC-R maker family of `ChargingPad` and the relay masts (`AntennaMast`, `PowerPylon`, `RelayContactPad`), 2.50–2.86 m tall, rendered in **Day C** lighting with Rover 07 docked on the apron for scale and context. |
| [`grown_0.6.png`](grown_0.6.png) | **"Fully Grown 0.6" Rover 07** equipped with round-3 V1 headlamp (`Kit_SSR3Headlamp` on `BrowSocket`) + V1 flank cell pods (`Kit_CellPod` on `PodSocket_L/R`) + **Cell Pack III Variant A** (`Back` socket, stacking on V1 pods), rendered from the **7.5 m chase cam**, **front 3/4**, and **rear 3/4** in **Day C** lighting, plus a 3-stage growth progression strip (`Base 07` → `Round-3 V1` → `Fully Grown 0.6`). |
| [`render_06_upgrades.py`](render_06_upgrades.py) | Self-contained headless Blender + PIL concept builder and sheet compositor. |

---

## 1. Cell Pack III (`Back` Socket · 8 Cells · ~370 m Range)

### Design Constraints & Context
- **Gameplay Role (`docs/design/0.6-world-regions.md`):** Crafted at the Home Bench (`3M 2W 2R · 0.6 ⚡`) once Rare (`R`) drops from South Relay / Crater Mine unlock Tier 2 crafting. Expands 07's battery from 6 cells (~275 m, Cell Pack II) to **8 cells (~370 m)**, turning South Relay from a razor's-edge round trip into a comfortable 5-bar margin and opening the northern push into Long Mare.
- **Socket & Stacking (`Logs/TASK.md`):** Attaches via the `Back` upgrade slot and **physically stacks on top of the round-3 V1 flank cell pods (`Kit_CellPod`)** along the upper shoulder troughs (`x ≈ ±0.48–0.51 m`, resting on the V1 pod crown at `y ≈ 1.08 m`), leaving the rear solar deck (`y ≈ 0.90 m`, `z ∈ [-0.88, -0.18]`) and `DeckSocket` (`Kit_TapeDeck`) completely unobstructed.
- **Visual & Proportion Rules (`docs/design/0.5.5-owner-feedback.md`, `Logs/TASK.md`):**
  1. Must visibly change 07's silhouette from the **7.5 m chase camera (`16°` pitch, `52°` vertical FOV)** by **≤ 20%**.
  2. Must stay strictly **inside 07's `±0.738 m` outer tyre stance**.
  3. Must use **`R1_CreamEnamel`** as the primary body material (`R1_GreyMetal` + `R1_Rubber` mounting collars/gaskets) with **exactly one `R1_LampAmber` accent** per pod.
  4. Must preserve 07's character: **small, cute, calm, friendly retro-futurist instrument** — no bulky industrial box backpacks, no jagged fins, no wide wing-like protrusions.

### Quantitative 7.5 m Chase-Camera Silhouette & Geometry Metrics

All silhouette measurements are computed directly from alpha masks rendered through the exact gameplay camera (`distance = 7.5 m`, `pitch = 16°`, `vertical FOV = 52°`, looking at `(0, 0.72, 0)`):

| Configuration | 7.5 m Chase Mask Pixels | Growth vs Round-3 V1 (`≤ 20%` cap) | Total Growth vs Bare 07 | Max Outer Half-Span (`|x|`) | Clearance Inside `±0.738 m` Tyres | Top Crown Height (`y`) | Upgrade Triangles |
|---|---:|---:|---:|---:|---:|---:|---:|
| **Stage 0 — Base Rover 07** (4 cells) | `18,597 px` | — | `0.00%` | `±0.738 m` (tyres) | `0.0 cm` (baseline) | `1.530 m` (CRT head) | — |
| **Stage 1 — Round-3 V1** (`Kit_SSR3Headlamp` + `Kit_CellPod`, 6 cells) | `20,030 px` | `0.00%` | `+7.71%` | `±0.646 m` (pods) | `9.2 cm` inside tyres | `1.530 m` (CRT head) | `1,636` |
| **Variant A (Recommended)** — *Twin Piggyback Pill Capsules* | `20,771 px` | **`+3.70%`** | **`+11.69%`** | **`±0.648 m`** | **`9.0 cm` inside tyres** | **`1.274 m`** | `2,224` |
| **Variant B** — *Stacked Soft Twin Lozenge Blisters + Bridge* | `20,816 px` | **`+3.92%`** | **`+11.93%`** | **`±0.651 m`** | **`8.7 cm` inside tyres** | **`1.270 m`** | `1,196` |
| **Variant C** — *Stacked Twin Domed Vertical Canisters* | `20,730 px` | **`+3.49%`** | **`+11.47%`** | **`±0.632 m`** | **`10.6 cm` inside tyres** | **`1.285 m`** | `1,852` |

---

### Ranked Variant Breakdown — Cell Pack III

#### Rank 1 (RECOMMENDED): Variant A — Twin Piggyback Pill Capsules (*"Saddle-Top Pills"*)
- **Form & Stacking:** A pair of plump, horizontal `R1_CreamEnamel` pill capsules (`radius = 0.088 m`, `length = 0.62 m`) with hemispherical domed end-caps, seated fore-and-aft in contoured `R1_GreyMetal` + `R1_Rubber` saddle cradles directly on top of the left and right V1 `Kit_CellPod` bodies (`x = ±0.505 m`, `y = 1.158 m`), tucked slightly inward toward the upper chassis shoulder.
- **Single `R1_LampAmber` Accent:** A third vertical glowing amber charge bar (`y = 1.158 m`) on the outer flank of each upper pill capsule, aligned directly above the two vertical amber charge bars on the V1 `Kit_CellPod` (`y = 0.90 m`). Together they form a clean **3-bar vertical charge column** (`4 cells → 2 bars at 6 cells → 3 bars at 8 cells`).
- **Why It Wins:**
  1. **Unifies the Whole 0.6 Rover Family:** Round-3 V1 introduced two distinct shapes: the rounded twin pill capsules of `Kit_SSR3Headlamp` on the brow and the soft chamfered saddlebags of `Kit_CellPod` on the flanks. Variant A repeats the **exact rounded pill-capsule vocabulary of `Kit_SSR3Headlamp`** one step larger on top of the V1 flank pods, tying the brow and flanks into a single coherent VIS design language.
  2. **Best 7.5 m Chase-Cam Silhouette Read (`+3.70%` vs V1, `+11.69%` vs Base 07):** From the 16° chase camera, the twin domed pill crowns fill the empty V-notch between the V1 flank pods and the CRT neck without squaring off 07's shoulders or competing with the CRT head (`y = 1.274 m` vs head top `1.530 m`).
  3. **Intuitive Progression Read:** Adding a third vertical `R1_LampAmber` bar above V1's 2-bar column communicates "tier-2 battery expansion" at a glance from both the chase view and 3/4 views.

#### Rank 2: Variant B — Stacked Soft Twin Lozenge Blisters + Rear Equaliser Bridge
- **Form & Stacking:** A second-storey pair of soft, deep-beveled `R1_CreamEnamel` lozenge blisters (`0.22 m × 0.165 m × 0.66 m`) that continue the exact rounded-box planform of the V1 `Kit_CellPod` upward across a recessed `R1_Rubber` waist gasket (`y = 1.075 m`), tied together behind the CRT neck (`z = -0.26 m`, `y = 1.115 m`) by a low curved `R1_GreyMetal` equaliser busbar with a small central `R1_CreamEnamel` junction capsule.
- **Single `R1_LampAmber` Accent:** One flush vertical amber charge bar (`x = ±0.625 m`, `y = 1.168 m`) recessed in a dark rubber bezel directly above V1's two flank bars.
- **Pros & Cons:**
  - *Pros:* Very clean monolithic flank read; the low rear equaliser bridge explicitly justifies using the `Back` socket while staying well clear of `SolarWing` (`y = 0.90 m`) and `DeckSocket`.
  - *Cons:* Because the upper blister shares the box-lozenge profile of the lower V1 pod, it makes 07's rear haunches look slightly taller and more slab-sided from the 7.5 m chase cam than Variant A's cylindrical pill capsules.

#### Rank 3: Variant C — Stacked Twin Domed Vertical Cell Canisters (*"Twin Flasks"*)
- **Form & Stacking:** A contoured `R1_GreyMetal` twin-cup manifold cradle (`x = ±0.495 m`, `y = 1.095 m`) mounted on top of each V1 `Kit_CellPod`, holding **two vertical hemispherical-domed `R1_CreamEnamel` cell flasks per side** (fore flask at `z = +0.08 m`, aft flask at `z = -0.20 m`; **4 flasks total**), linked at their crowns by a short `R1_GreyMetal` manifold bridge.
- **Single `R1_LampAmber` Accent:** A warm glowing `R1_LampAmber` collar ring (`y = 1.122 m`) seated around the base of the aft canister on each side (visible from chase and rear 3/4 views).
- **Pros & Cons:**
  - *Pros:* Most literal "cell count" read (`+2` visible vertical canister pairs = `+2` cells from 6 to 8) and highest vacuum-pressure-vessel authenticity (`docs/design/vacuum-shapes.md`).
  - *Cons:* The two vertical domes per side introduce a slightly busier 4-bump rhythm along the shoulder profile compared to the calm, continuous horizontal flow of Variant A.

---

## 2. Field Charge Pylon (Placeable Forward Charger on Predefined Pad)

### Design Constraints & Context
- **Gameplay Role (`docs/design/0.6-world-regions.md`, `docs/design/craft-catalogue.md`):** Crafted on-site at a predefined dormant pad (`3M 2W 1O · 0.4 ⚡`) such as the **Mare Waystation** (`(0, +720)`) halfway up Long Mare. Once built, it serves as a permanent forward charging point (~50% charge rate of the Home `ChargingPad`) and a gentle warm amber beacon on the dark mare horizon.
- **Maker Family (`docs/design/maker-understanding.md`, `Logs/TASK.md`):** Built by the same VIS / ILC-R automated infrastructure family as the Home **`ChargingPad`** (`tools/art/blender/charging_pad.py`) and the relay structures (**`AntennaMast`**, **`PowerPylon`**, **`RelayContactPad`**):
  - Chamfered octagonal/hexagonal `R1_GreyMetal` contact deck (`3.15–3.35 m` wide) with corner anchor feet, approach ramps, `±0.61 m` `R1_DarkMetal` wheel guide tracks matching 07's wheel track, and raised central **`R1_Copper` contact shoes** that mate with 07's belly contacts.
  - Rounded `R1_CreamEnamel` thermal-blanket capacitor housings with **`R1_OrangeStripe`** beltlines and riveted **`R1_SagePatch`** maintenance plates.
  - Open triangulated `R1_GreyMetal` / `R1_DarkMetal` lattice mastwork (`2.50–2.86 m` tall) carrying **`R1_LampAmber`** beacons and vertical charge-bar totems.
- **Vacuum Logic (`docs/design/vacuum-shapes.md`):**
  - **No fans, vents, or louvers:** Charging transfer heat is rejected exclusively via **large vertical `R1_Copper` radiative fin banks** (`6–7` parallel plates with dark-metal collector manifolds) mounted on the shaded rear/top of the transformer housing.
  - **High-voltage isolation in vacuum:** Heavy `R1_Copper` busbars pass through **ribbed `R1_CreamEnamel` ceramic stand-off insulator stacks** before reaching the deck contact shoes.
  - **Packaged-and-deployed geometry:** Splayed outrigger legs, hinge knuckles, and fold-out apron ramps clearly show how the pylon folded inside a compact cargo canister before being deployed on the predefined pad.

### Quantitative Specification Table — Field Charge Pylon

| Variant | Total Height (`2–3 m` target) | Apron Width | Triangle Count | Primary Far/Mid Read ("Charge Here") | Vacuum Thermal & HV Hardware |
|---|---:|---:|---:|---|---|
| **Variant A (Recommended)** — *Gooseneck Lantern Bollard & Contact Apron* | **`2.58 m`** | **`3.23 m`** | `5,680` | Overhanging caged **`R1_LampAmber` lantern** cantilevered directly above the copper contact shoes + **4-bar vertical amber charge totem** facing 07's visor | 6-plate vertical `R1_Copper` radiative fin bank on rear bollard + twin ceramic stand-off insulators + coiled copper umbilical |
| **Variant B** — *Twin-Column Portal Arch & Overhead Charge Beam* | **`2.50 m`** | **`3.35 m`** | `5,352` | Wide **overhead `R1_LampAmber` light bar** across the portal cross-beam + **dual 3-bar amber charge columns** on left & right piers | 7-plate vertical `R1_Copper` radiative fin array along top cross-beam + twin ceramic busbar feedthroughs at pier bases |
| **Variant C** — *Telescoping Tripod Beacon Mast & Contact Apron* | **`2.86 m`** | **`3.15 m`** | `5,932` | **`360°` masthead `R1_LampAmber` lantern** (`y = 2.66 m`) + **4-bar vertical amber charge totem** on front of cylindrical capacitor drum | 6 radial `R1_Copper` radiative fins around rear of capacitor drum + 3 ceramic insulator stacks + twin side copper cable reels |

---

### Ranked Variant Breakdown — Field Charge Pylon

#### Rank 1 (RECOMMENDED): Variant A — Gooseneck Lantern Bollard & Fold-Out Contact Apron
- **Architecture:** Combines a compact chamfered octagonal drive-on contact apron (`3.23 m` wide) with a rounded `R1_CreamEnamel` capacitor bollard at the rear (`z = -1.28 m`, `y = 0.14–1.26 m`, complete with `R1_OrangeStripe` beltline and riveted `R1_SagePatch` repair panel) and an open-lattice `R1_GreyMetal` **gooseneck mast** (`2.58 m` tall) that arches forward (`z = -0.45 m`) to suspend a caged **`R1_LampAmber` down-lantern** directly over the central `R1_Copper` contact shoes.
- **Why It Wins:**
  1. **Unmistakable "Charge Here" Purpose at Every Distance:** Unlike a purely vertical relay mast (which can be mistaken for a comms antenna from 100 m away), the **forward-cantilevered gooseneck lantern hanging over a drive-on pad** creates a universal "streetlamp over a parking spot / service bay" silhouette. The pool of warm amber light falls directly onto the copper contact shoes where 07 parks.
  2. **Strongest Kinship with Home `ChargingPad`:** The rear bollard directly echoes the rounded `R1_CreamEnamel` + `R1_OrangeStripe` terminal bollard of the Home `ChargingPad`, immediately teaching the player that this is its field-deployable sibling.
  3. **Ideal Framing in the 7.5 m Chase Camera:** When 07 drives onto the apron facing the bollard, the 4-bar vertical `R1_LampAmber` charge totem sits directly at 07's eye level while the gooseneck lantern frames the top of the shot at `y = 2.58 m` without blocking the player's view of 07.
  4. **Clear Vacuum Engineering:** In the rear 3/4 view, the 6-plate `R1_Copper` radiative cooling fin bank, twin ceramic stand-off insulators, and coiled copper umbilical cable make its thermodynamic and electrical function completely legible.

#### Rank 2: Variant C — Telescoping Tripod Beacon Mast & Hinged Contact Apron
- **Architecture:** Built around a cylindrical `R1_CreamEnamel` pressure-vessel capacitor drum (`z = -1.25 m`) supported by three splayed `R1_GreyMetal` tripod legs, topped by a 3-chord triangulated lattice mast (`2.86 m` tall) with a `360°` caged `R1_LampAmber` beacon and twin side-mounted `R1_Copper` cable reels feeding a hinged fold-out contact apron (`3.15 m` wide).
- **Pros & Cons:**
  - *Pros:* Best "packaged-and-deployed" portable expedition kit feel (`docs/design/vacuum-shapes.md` Rule 5) and strongest structural kinship with `AntennaMast` and `PowerPylon`. Great omnidirectional horizon visibility across Long Mare.
  - *Cons:* From 120+ metres away, a straight vertical tripod mast reads closer to a relay/navigation beacon than an immediate drive-on charging bay until the lower apron comes into view.

#### Rank 3: Variant B — Twin-Column Portal Arch & Overhead Charge Beam
- **Architecture:** Two canted `R1_CreamEnamel` + open-lattice side piers (`x = ±1.14 m`) straddling the `3.35 m` contact apron and linked at `y = 2.18 m` by an overhead `R1_CreamEnamel` bridge beam (`2.50 m` total height to top of copper radiator fins) carrying a wide downward-facing `R1_LampAmber` light bar.
- **Pros & Cons:**
  - *Pros:* Driving *into* a glowing amber archway is deeply satisfying and frames 07 beautifully from both front and chase views.
  - *Cons:* Twin piers + overhead beam feel slightly more permanent/architectural than a 1-person field kit (`3M 2W 1O`), and the side piers require slightly more precise lateral alignment when approaching from an oblique angle.

---

## 3. "Fully Grown 0.6" Rover 07 (`grown_0.6.png`)

The composite sheet [`grown_0.6.png`](grown_0.6.png) shows Rover 07 at the culmination of `0.6` with:
1. **`BrowSocket`:** `Kit_SSR3Headlamp` (Round-3 V1 twin cream pill headlamps with `R1_LampAmber` lenses).
2. **`PodSocket_L` / `PodSocket_R`:** `Kit_CellPod` (Round-3 V1 flank saddlebag cell pods with 2 vertical `R1_LampAmber` charge bars per side).
3. **`Back` Socket (stacking on V1 pods):** **Cell Pack III — Variant A (*Twin Piggyback Pill Capsules*)**, nesting a pair of rounded `R1_CreamEnamel` pill capsules on top of the V1 flank pods and adding the 3rd vertical `R1_LampAmber` charge bar to complete the 3-bar column.
4. **`DeckSocket`:** `Kit_TapeDeck` (twin-reel cassette recorder on the rear deck).

### Summary of 0.6 Character & Proportion Check
- **Silhouette growth:** `+7.71%` at Stage 1 (Round-3 V1) → **`+11.69%` total over bare 07** at Stage 2 (Fully Grown 0.6), adding **`+3.70%`** over V1 — well inside the `≤ 20%` budget while clearly filling the upper shoulder notch from the 7.5 m chase cam.
- **Stance discipline:** Maximum outer upgrade width is **`±0.648 m`**, staying **`9.0 cm` inside** 07's `±0.738 m` outer tyre stance so the wheels remain the widest, most grounded part of the silhouette.
- **Head hierarchy:** Top of Cell Pack III sits at **`y = 1.274 m`**, staying **`25.6 cm` below** the CRT head crown (`y = 1.530 m`) so 07's expressive visor and brow headlamps remain the undisputed focal point.

---

## 4. Reproducing the Renders

All geometry, materials, Day C lighting, silhouette masks, and composite sheets are generated deterministically by [`render_06_upgrades.py`](render_06_upgrades.py) using headless Blender 5.1 (`D:/Blender/blender.exe`) and system Python (`PIL`):

```powershell
D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P docs/art/concepts/0.6-upgrades/render_06_upgrades.py
```
