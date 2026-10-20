# 07 — Upgrade Visual Language & Concept Specification

**Status:** Concept Specification (`box/agy-07-upgrades` — Concept Only, no production FBX changes in this round)
**Contact Sheets:** `Logs/captures/upgrades_sheet.png` (master 5×2 + Full Growth sheet) and `Logs/captures/full_growth.png`
**Generator Script:** [`tools/art/blender/rover/render_upgrade_concepts.py`](../../tools/art/blender/rover/render_upgrade_concepts.py)
**Cross-References:** [`docs/design/07-identity.md`](07-identity.md), [`docs/design/opening-v3-purpose.md`](opening-v3-purpose.md), [`docs/design/maker-understanding.md`](maker-understanding.md), [`docs/design/tech-eras.md`](tech-eras.md), [`docs/art/r1-pipeline.md`](../art/r1-pipeline.md)

---

## 1. Why Previous Upgrades Read as Nothing (The 7 m Chase Camera Problem)

In gameplay (`Assets/_Project/Data/Tuning/RoverCameraTuning.asset`), the player views 07 from a chase camera positioned at:
- **Distance:** `~7.0–7.5 m` behind 07
- **Pitch:** `16°` downward toward `_targetHeight = 1.1 m`
- **Vertical FOV:** `52°`

At `7.0 m` distance (`52°` vertical FOV on a `1080p` viewport), **1 cm in world space subtends ~1.5 pixels**. Consequently:
1. **Sub-5 cm details vanish:** Sliding `Antenna_Segment2` up by `+0.35 m` on a `Ø 1.6 cm` rod adds `< 2.5 px` width (`+0.5%` silhouette area). Adding `Cutter_Igniter` (`4 cm × 9 cm × 5 cm`) on the side of the front cutter turret is both sub-pixel and **100% occluded by 07's chassis and CRT head** from the rear chase camera.
2. **Low front-nose attachments are self-occluded:** Anything mounted inside `|x| < 0.38 m, y < 0.85 m, z > +0.40 m` is hidden behind 07's upper hull and `0.77 m` wide CRT head when viewed from behind at `16°` pitch.

---

## 2. The 5 Upgrade Visual Language Rules

Every physical upgrade fitted to 07 across the R1–R11 opening and beyond must obey five mandatory rules:

### Rule 1 — Silhouette Transformation at 7 m (`>= 10–15%` Outline / Mass Change)
- Every upgrade must break 07's outer silhouette contour as seen from the **`7.0 m`, `16°` pitch chase camera**, producing at least **`>= 10–15%` new exterior silhouette contour** (or an unmistakable new external geometry volume >= `0.20 m` in span).
- **Front-mounted upgrades** (such as the R4 Igniter and R10–11 Headlamp) must wrap onto the **upper bonnet shoulders (`|x| >= 0.41 m, y >= 0.86 m`)** or **CRT visor flanks/crown (`|x| >= 0.48 m` or `y >= 1.55 m`)** so their silhouette and emissive read clearly from *behind* 07 during normal driving, not only in front 3/4 inspection views.

### Rule 2 — ONE Maker Accent per Upgrade (Color & Era Provenance)
Each upgrade carries **one signature maker color/emissive block** (`>= 20 cm` span) that identifies *whose hands built the donor hardware* (per [`docs/design/tech-eras.md`](tech-eras.md) and [`docs/design/maker-understanding.md`](maker-understanding.md)):
- **1974 Kessler–Arai / Lumen Series-7 (`Era A` — Elders 03, 04, 06):** Warm Cream Enamel (`R1_CreamEnamel` `#E6DAC3`) paired with bold Coral-Orange (`R1_OrangeStripe` `#FF8A5B`) identification blocks and warm Amber incandescent/plasma indicators (`R1_LampAmber` `#FFB547`).
- **1978 SSR-3 Deep-Crater Survey (`Era B` — Salvaged via Elder 06):** Brake-bent sheet metal in crisp White Enamel (`R1_WhitePaint` `#ECE8DF`) with a **single thin Coral-Orange stripe** (`R1_OrangeStripe`) and a large rectangular Amber sealed-beam lens (`R1_LampAmber`).
- **2004 Tycho Consortium (`Era D` — Salvaged in R10):** Clean hexagonal composite geometry in White Paint (`R1_WhitePaint` `#ECE8DF`) ringed by a bold **Tycho Teal hexagon** (`R1_TealMark` `#3E7A82`), machined copper inductive contact shoes (`R1_Copper`), and an Amber handshake lock core (`R1_LampAmber`).

### Rule 3 — Field-Bolted by 07 Itself (Salvaged, Tactile, Hand-Adapted)
Upgrades never magically morph 07's factory shell or look factory-molded into the 1974 chassis. Every upgrade visibly shows **how 07 attached it**:
1. **Standardized Kessler–Arai Socket Plates:** Heavy `R1_GreyMetal` gusset brackets bolted into 07's pre-drilled shoulder, flank, visor, or rear-valence sockets with oversized `R1_Brass` hex bolts (`12–16 mm` heads).
2. **Visible Field-Weld Beads:** Raised `R1_BareSteel` weld-bead strips where a non-standard donor bracket (1978 SSR-3 or 2004 Tycho) was bridged onto a 1974 Kessler–Arai mount.
3. **Cut Coral `07` Adapter Tag:** A small rectangular `R1_OrangeStripe` (`#FF8A5B`) adapter plate with `R1_Brass` corner rivets welded across the joint — 07's visual signature claiming the salvaged part.
4. **External Umbilical Routing:** Thick `R1_Rubber` and braided `R1_Copper` power/signal conduits routed externally from the upgrade into 07's hull sockets.

### Rule 4 — Strict Lunar Vacuum Logic
- **Zero aerodynamic motifs:** No air intakes, scoops, louvers, cooling fans, propellers, or wings.
- **Vacuum engineering only:** Heat rejection uses **exposed `R1_Copper` radiative cooling fin stacks**; thermal insulation uses **crinkled `R1_GoldFoil` Kapton MLI blankets**; RF telemetry uses **open parabolic mesh dishes, cross-braced trusses, and copper loop/dipole arrays**; mechanisms use **exposed pantograph linkages, worm gears, and sealed bellows**.

### Rule 5 — Calm, Cozy, Bilateral 07 Identity
- **Bilateral symmetry or balanced visual weight:** Even when a part is salvaged from another era, 07 mounts it symmetrically (or balances a dorsal mast with a symmetrical shoulder bridge) so 07's silhouette remains grounded, toy-like, and friendly.
- **Unobscured CRT Visage:** No upgrade ever crosses in front of the dark CRT glass (`R1_Screen`) or crowds the half-lidded brass camera eye (`R1_Brass` / `R1_Lens`).
- **5 Non-Overlapping Mounting Zones:** Each of the 5 upgrades occupies a dedicated structural zone on 07 so that when all 5 are fitted simultaneously, zero parts collide or clutter the silhouette:

| Zone | Structural Location on 07 | Assigned Upgrade | Primary Silhouette Axis |
| :--- | :--- | :--- | :--- |
| **Zone 1** | Rear Dorsal Shoulders & Roofline (`y = 0.88–2.22 m, z = -0.36 m`) | **(a) Antenna Extended** (R3) | **Vertical Crown (`+Y`)** |
| **Zone 2** | Front Bonnet Shoulders & Cutter Turret (`x = ±0.56 m, y = 0.98 m, z = +0.48..+0.88 m`) | **(b) Igniter Fitted** (R4) | **Front Upper Shoulders (`±X, +Z`)** |
| **Zone 3** | CRT Head Upper Brow Corners (`x = ±0.53 m, y = 1.54–1.80 m, z = +0.25 m`) | **(c) SSR-3 Headlamp** (R10–11) | **Upper Head Brow Width (`±X, +Y`)** |
| **Zone 4** | Mid-Hull Flank Sockets (`x = ±0.64 m, y = 0.62–1.18 m, z = -0.10 m`) | **(d) Cell Pack** (R11 Peak Key) | **Mid-Hull Width (`±X`)** |
| **Zone 5** | Rear Valence / Lower Bumper Outriggers (`x = ±0.64 m, y = 0.38–0.60 m, z = -0.78..-1.10 m`) | **(e) Hex Charge Adapter** (R10 Tycho Key) | **Rear Stance & Tail (`-Z, ±X`)** |

---

## 3. Concept Specifications: 5 Upgrades × 2 Variants (A & B)

### (a) Antenna Extended (`R3` — Elder 03's Gift)
- **Why Before Fails:** Sliding `Antenna_Segment2` up by `+0.35 m` on a `1.6 cm` rod changes the 7 m chase silhouette by only `+0.4%` (`< 2.5 px` wide).
- **Variant A ★ [RECOMMENDED] — *Dish-Crown Mast & Symmetric Telemetry Bridge*:**
  - Bolts a `0.86 m` wide `R1_CreamEnamel` + `R1_OrangeStripe` telemetry saddle bridge across both rear dorsal shoulder sockets (`y = 0.94 m, z = -0.36 m`), flanked by twin side-projecting cylindrical transceiver pods (`x = ±0.46 m`) with rear-facing `R1_LampAmber` lock windows.
  - Replaces the thin whip with a heavy twin-tube open-truss mast rising to **`y = 2.22 m`**, crowned with a canted **`Ø 0.34 m` brass/copper parabolic mesh dish**, helical copper feed coil, and a glowing `R1_LampAmber` call-sign dome.
  - **7 m Chase Readability:** **`+10.2%`** new exterior silhouette contour (`+10.5%` in 3/4 close-up; `+36%` total height increase).
- **Variant B — *Unfolding Dorsal Scissor-Truss Mast & Wide T-Array*:**
  - Bolts a 2-stage cross-braced pantograph scissor truss (`y = 0.98–1.98 m`) rising to **`y = 2.16 m`** and supporting a **`0.86 m` wide horizontal T-bar dipole array** with central `R1_OrangeStripe` transceiver box, twin outboard `R1_Brass` wave-trap loops (`x = ±0.42 m`), and a central glowing `R1_LampAmber` dome.
  - **7 m Chase Readability:** **`+11.9%`** new exterior silhouette contour (`+26.0%` in 3/4 close-up).

### (b) Igniter Fitted (`R4` — Elder 04's Gift)
- **Why Before Fails:** `Cutter_Igniter` (`4 cm × 9 cm × 5 cm`) sits low on the front cutter turret (`y = 0.66 m`) where 07's chassis completely hides it from the rear chase camera (`+0.0%` chase silhouette change).
- **Variant A ★ [RECOMMENDED] — *Twin-Shoulder Plasma Ignition Saddle & Glowing Coil Shroud*:**
  - Wraps a bolted `R1_GreyMetal` horseshoe yoke over 07's upper front bonnet (`y = 0.88 m, z = +0.40 m`) carrying **symmetric left/right canted `R1_OrangeStripe` ignition canisters (`x = ±0.56 m, y = 0.98 m, z = +0.48 m`)** with large glowing **`R1_LampAmber` plasma-coil sight cylinders** caged in `R1_Brass` ribs and outboard 4-plate **`R1_Copper` vacuum radiator stacks** (`x = ±0.70 m`). Positioned forward over the front fender shoulders, they leave the cheek brass camera eye 100% clear while breaking boldly into the open sky beside 07's neck in the **rear 7 m chase camera**.
  - Adds a heavy slotted `R1_GreyMetal` + `R1_Copper` finned thermal shroud (`Ø 0.35 m`) around the front cutter barrel (`z = +0.74 m`), linked to the shoulder coils by thick braided copper/rubber conduits.
  - **7 m Chase & 3/4 Readability:** **`+10.4%`** new exterior silhouette contour at the 7 m chase camera (`+7.9%` in 3/4 close-up).
- **Variant B — *Over-Bonnet Arc-Exciter Box & Outboard Copper Radiator Wings*:**
  - Bolts a chunky `0.66 m` wide `R1_CreamEnamel` + `R1_OrangeStripe` Arc-Exciter generator box atop the front nose (`y = 0.92–1.08 m`) with a wide panoramic `R1_LampAmber` coil window, flanked by **cantilevered 6-plate `R1_Copper` vacuum radiator wings at `x = ±0.56 m, y = 1.02 m`** (topped with coral/amber beacons) and a helical `R1_Copper` induction cage around the cutter nozzle.
  - **7 m Chase & 3/4 Readability:** **`+13.6%`** new exterior contour at the 7 m chase camera (`+9.6%` in 3/4 close-up).

### (c) SSR-3 Headlamp (`R10–11` — Salvaged Elder 06 White-Enamel Lamp)
- **Canon Anchor:** [`docs/design/maker-understanding.md` §2](maker-understanding.md) specifies *"06's white, boxy sealed-beam lamp welded beside 07's visor"* (`1978 SSR-3` Era B hardware).
- **Variant A ★ [RECOMMENDED] — *Twin Salvaged SSR-3 Box Floodlights on Upper Visor Ear-Brackets*:**
  - Welds symmetric `1978 SSR-3` brake-bent boxy floodlight housings (`0.26 m × 0.27 m × 0.29 m` each) in crisp **`R1_WhitePaint` (`#ECE8DF`)** with a thin **`R1_OrangeStripe` Era-B band** onto upper brow-corner `R1_GreyMetal` outrigger gussets (`x = ±0.53 m, y = 1.54 m, z = +0.25 m`), linked by an over-brow conduit bar with a cut coral `07` tag.
  - Because the pods mount at the **upper brow corners (`y = 1.54 m`)**, they sit completely ABOVE 07's cheek brass camera eye (`y = 1.11–1.35 m`), keeping both the CRT face and half-lidded brass lens 100% open in every view.
  - Front faces feature machined `R1_Brass` bezels and large rectangular `R1_LampAmber` sealed-beam lenses; rear faces feature backlit `R1_LampAmber` pilot slits and `R1_Copper` heatsink plugs facing the chase camera.
  - **7 m Chase Readability:** **Widens 07's upper head silhouette by `+71%` (`0.77 m -> 1.32 m`)**, adding **`+10.5%` new chase silhouette contour** (`+24.4%` in 3/4 close-up).
- **Variant B — *Over-Visor Salvaged SSR-3 Searchlight Rack & Twin Spot Bar*:**
  - Welds a tubular steel roll-cage arch over 07's CRT crown (`y = 1.56–1.80 m`) carrying a large central `0.40 m` wide `R1_WhitePaint` + `R1_OrangeStripe` sealed-beam searchlight flanked by two auxiliary white spot pods (`0.88 m` total span).
  - **7 m Chase Readability:** Raises 07's head crown by `+0.29 m`, adding **`+10.4%` new chase silhouette contour** (`+22.3%` in 3/4 close-up).

### (d) Cell Pack (`R11` Peak Key — Elder 06's Salvaged Battery Cells)
- **Variant A ★ [RECOMMENDED] — *Twin Flank Saddle-Pod Battery Panniers*:**
  - Bolts two heavy chamfered battery saddle-pods (`0.28 m W × 0.42 m H × 0.54 m L` each) onto 07's left and right mid-flank Kessler–Arai sockets (`x = ±0.64 m, y = 0.62–1.18 m, z = -0.10 m`) over crinkled **`R1_GoldFoil` Kapton MLI thermal blankets**.
  - Finished in `R1_CreamEnamel` with bold **`R1_OrangeStripe` end-caps**, twin `R1_GreyMetal` hold-down straps, top `R1_Brass` cell caps, a longitudinal `R1_Copper` busbar, and **3 vertical + 3 rear-facing `R1_LampAmber` charge-level bars** per pod.
  - **7 m Chase Readability:** **Projects cleanly outside the wheel fenders (`0.88 m -> 1.56 m` hull span)**, adding **`+13.1%` new chase silhouette contour** (`+7.0%` in 3/4 close-up) and giving 07 a grounded, expedition-ready stance.
- **Variant B — *High-Output Dorsal Power Backpack & Space Radiator Stack*:**
  - Mounts a `1.16 m` wide dorsal battery backpack (`y = 1.02–1.52 m, z = -0.54 m`) on a cantilevered shoulder cradle, topped with 4 tall upright `R1_Brass` / `R1_LampAmber` cylindrical cell towers, outboard `R1_GoldFoil` + 5-plate `R1_Copper` vacuum radiator wings (`x = ±0.67 m`), and a rear-facing 4-column `R1_LampAmber` charge matrix.
  - **7 m Chase Readability:** **`+18.2%` new chase silhouette contour** (`+17.7%` in 3/4 close-up).

### (e) Hex Charge Adapter (`R10` Tycho Key — 2004 Tycho Consortium Hex Contact Plate)
- **Canon Anchor:** [`docs/design/maker-understanding.md` §2](maker-understanding.md) and [`docs/design/opening-v3-purpose.md` §5](opening-v3-purpose.md) specify the 2004 Tycho Consortium hex contact plate (`R1_WhitePaint` composite + `R1_TealMark` `#3E7A82` hexagon) adapted onto 07's charge port so 07 can draw power from Tycho pads.
- **Variant A ★ [RECOMMENDED] — *Rear Pantograph Arm & Canted Tycho Hex Collector Shield*:**
  - Bolts a folding bare-steel/brass pantograph linkage onto 07's rear valence sockets (`y = 0.38–0.52 m, z = -0.74..-1.10 m`) carrying a **large `Ø 0.68 m` hexagonal Tycho collector shield canted at `36°` below/behind the rear bumper**, flanked by **twin face-on hexagonal White/Teal power-conditioning pods on rear bumper outriggers (`x = ±0.64 m, y = 0.50 m, z = -0.78 m`)** connected by armored rubber umbilicals.
  - The hexagonal shield displays `R1_WhitePaint` (`#ECE8DF`), a bold **`R1_TealMark` (`#3E7A82`) hexagonal livery ring**, **6 hexagonal `R1_Copper` inductive contact shoes**, and a central `R1_LampAmber` lock core.
  - **7 m Chase Readability:** Projects below the rear bumper and outside the rear fenders, adding **`+16.2%` new chase silhouette contour** (`+9.2%` in 3/4 close-up) with an unmistakable geometric hexagon and `#3E7A82` Tycho teal ring.
- **Variant B — *Twin Side-Outrigger Articulated Hex Collector Skids*:**
  - Bolts articulated worm-gear swing-arm outriggers onto 07's lower rear-flank sockets (`x = ±0.42..±0.78 m, y = 0.48 m, z = -0.84 m`), each carrying a canted **`Ø 0.58 m` Tycho White/Teal/Copper hexagonal collector shield** that folds down flat onto a Tycho pad when docking and projects `+0.42 m` outside each rear wheel while driving.
  - **7 m Chase Readability:** **`+15.7%` new chase silhouette contour** (`+4.6%` in 3/4 close-up).

---

## 4. Quantitative Silhouette Readability Summary

Measured directly from the rendered alpha masks (`Logs/captures/silhouette_metrics.json`):

| Upgrade | Recommended Variant | 7 m Chase New Contour (`%` of Base 07) | 3/4 Close-Up New Contour (`%` of Base) | Primary Maker Accent |
| :--- | :--- | :---: | :---: | :--- |
| **Before `(a)`** *(today's +0.35 m thin rod)* | Baseline failure reference | `+0.4%` | `+0.6%` | None (`1.6 cm` grey rod) |
| **Before `(b)`** *(today's 4 cm igniter)* | Baseline failure reference | `+0.0%` *(occluded)* | `+0.2%` | Tiny orange speck |
| **(a) Antenna Extended** (`R3`) | **Variant A ★** *(Dish-Crown & Bridge)* | **`+10.2%`** *(Var B: `+11.9%`)* | **`+10.5%`** *(Var B: `+26.0%`)* | `R1_LampAmber` + `R1_OrangeStripe` |
| **(b) Igniter Fitted** (`R4`) | **Variant A ★** *(Twin-Shoulder Coils)* | **`+10.4%`** *(Var B: `+13.6%`)* | **`+7.9%`** *(Var B: `+9.6%`)* | Twin `R1_LampAmber` Coils + `R1_OrangeStripe` |
| **(c) SSR-3 Headlamp** (`R10–11`) | **Variant A ★** *(Twin Visor Floodlights)* | **`+10.5%`** *(Var B: `+10.4%`)* | **`+24.4%`** *(Var B: `+22.3%`)* | `R1_WhitePaint` (`#ECE8DF`) + Thin Coral Stripe |
| **(d) Cell Pack** (`R11` Peak Key) | **Variant A ★** *(Twin Flank Panniers)* | **`+13.1%`** *(Var B: `+18.2%`)* | **`+7.0%`** *(Var B: `+17.7%`)* | `R1_GoldFoil` MLI + `R1_OrangeStripe` + 3× Amber Bars |
| **(e) Hex Charge Adapter** (`R10` Tycho) | **Variant A ★** *(Rear Pantograph Hex)* | **`+16.2%`** *(Var B: `+15.7%`)* | **`+9.2%`** *(Var B: `+4.6%`)* | `R1_WhitePaint` + `R1_TealMark` (`#3E7A82`) Hex |
| **FULLY GROWN 07** *(All 5 `A` Variants)* | **`a_A + b_A + c_A + d_A + e_A`** | **`+50.5%` new contour** (`+50.5%` net) | **`+24.1%` Front 3/4 / `+33.1%` Rear 3/4** | Full 3-Era Salvage Lineage (`1974 + 1978 + 2004`) |

---

## 5. Why the "Fully Grown 07" (`a_A + b_A + c_A + d_A + e_A`) Still Reads as Cute, Calm 07

1. **Zero Zone Collisions:** Because each recommended `Variant A` occupies its own structural zone (Zone 1 Dorsal Bridge/Dish, Zone 2 Front Bonnet Shoulders/Cutter, Zone 3 Visor Ear-Brackets, Zone 4 Mid-Flank Saddle-Pods, Zone 5 Rear Valence Pantograph), no two upgrades overlap or fight for the same silhouette edge.
2. **Bilateral Balance Preserved:** Every single recommended upgrade (`a_A` through `e_A`) is bilaterally symmetric across `X = 0` (with `a_A`'s dish mast balanced by its symmetric shoulder bridge and twin transceiver pods).
3. **The Face Remains Sacred:** In the Front 3/4 view (`Logs/captures/full_growth.png`), 07's dark CRT visor, warm amber scanline bar, and half-lidded brass lens remain 100% open and uncrowded. The twin white 1978 SSR-3 floodlights (`c_A`) sit outside the visor cheeks like cozy earmuffs, and the twin amber ignition coils (`b_A`) tuck onto the lower collar shoulders.
4. **Readable Narrative History:** At a single glance from the 7 m chase camera, the player can read 07's entire journey: 1974 cream/coral gifts from Elders 03, 04, and 06 (`a_A`, `b_A`, `d_A`), 1978 white-enamel survey lamps from SSR-3 (`c_A`), and the 2004 white-and-teal hexagonal Tycho interface (`e_A`) — all bolted on by 07 itself.
