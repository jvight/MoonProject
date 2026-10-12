# M3-27: Materials and crafting (two families, few kinds, short chains)

> Owner: Director (design), drafted by the game design session. **Draft for the owner's approval.**
> The owner, 2026-10-12: "Little by little there will be more kinds of materials, like ores, electronic chips… varied,
> serving the base and upgrading 07, customised to fit the game and give it its own direction."
>
> Builds on:
> - GDD v3 §4.2 (T1 Metal/Wiring/Optics; T2 Alloy, Circuitry, Crystal; T3 Superconductor; components);
> - M3-22 (home pads), M3-25 (cargo and shelves), M3-26 (cutter tiers), M3-20 (battery);
> - the writing style and glossary.
>
> `SalvageMaterial` today is Metal 0, Wiring 1, Optics 2, saved: never renumbered. Rulings 5, 10, 11, 12 and 14
> apply.

## 1. The direction: what humanity left, and what the Moon holds
- **Salvage**: humanity's leftovers. It is finite and story-laden, and the beam **cuts** it from wrecks. Its best
  part, the circuit chip, **can't be made up here**. When the last chip is used, it's gone. That is the game's
  loneliness in material form.
- **Lunar**: the Moon's own geology, real and named for what it is. It is **dug or drilled**, then **refined at
  home** by machines that grow the base. It is plentiful and patient: the Moon gives slowly, and never runs out where
  it matters.
- **The arc:** 07 starts on humanity's scraps and learns to live on the Moon's own gifts. Progression reads as moving
  from scavenger to settler.

Learned and improved:

| Reference | Pain | Ours |
|---|---|---|
| Subnautica | 30+ raw items and many look-alikes; chains you need a wiki for | ≤ 8 raw and ≤ 5 refined by 1.0; chains at most 2 steps; every recipe row says where its input comes from |
| Astroneer | resources everywhere, so finding them has no meaning | each lunar material belongs to **one region** with one look and one story |
| Satisfactory | factories as the goal, with logistics the player must manage | **automatic home refining** at a visible, calm pace; the base works while 07 explores, with no belts to plan |

## 2. The materials
Glossary rows to add to `writing-style.md` (en / vi).

### 2.1 Raw (8)
| # | Name (en / vi) | Family | Ver | Source and verb | Used for | Bundle look (colour **and** shape, colour-blind safe) |
|---|---|---|---|---|---|---|
| 0 | **Metal / Kim loại** | salvage | 0.5 | cut from wrecks | everything structural | grey stacked plates |
| 1 | **Wiring / Dây điện** | salvage | 0.5 | cut | power, kits, masts | amber coil |
| 2 | **Optics / Thấu kính** | salvage | 0.5 | cut | lenses, lamps, sensors, Focus Lens (M3-26) | cyan glass discs in a ring |
| 3 | **Circuit Chips / Chip mạch** | salvage | 0.7 | cut from **sealed hulls** (Thermal Cutter, M3-26) | the lab's Reader II, Sister Drones, Superconductor | green boards in a clear sleeve, each with a tiny crew label |
| 4 | **Orange Glass Beads / Hạt thủy tinh cam** | lunar | 0.6 | **dug** (the beam's dig, no new tool) at the basin's old vent field | Glass | orange beads in a round woven sack |
| 5 | **Ilmenite / Ilmenit** | lunar | 0.6 | **drilled** (Core Drill) from dark basalt strata on the rim terraces | Alloy | black glittering lumps in a net |
| 6 | **Water Ice / Băng nước** | lunar | 0.7 | drilled in the Shadowed Crater's permanent shadow (Floodlights to see it) | Coolant, Crystal | a pale frosted cylinder core |
| 7 | **Rare Earths / Đất hiếm** | lunar | 0.8 | drilled on the Peak road (KREEP rock) | Superconductor | dull violet-grey nuggets in a small heavy box |

### 2.2 Refined (5), all made at home
| Name (en / vi) | From | Where | Ver | Used for | Look |
|---|---|---|---|---|---|
| **Glass / Thủy tinh** | 2 Orange Beads | the Smelter | 0.6 | Twin Solar Wing, Floodlights, Wide Sonar's dish, the biodome's panes | warm orange panes, clear |
| **Alloy / Hợp kim** | 2 Ilmenite | the Smelter | 0.6 | Cutter Ribs (M3-26), Magnetic Treads, Heavy Tow, shelf tiers (M3-25), Bay Wing, Fabricator | brushed silver ingots |
| **Coolant / Chất làm mát** | 1 Water Ice | the Ice Still | 0.7 | Thermal Cutter (replaces GDD v3's Heat Sink component), Power Core | cyan-white canisters |
| **Crystal / Tinh thể** | Water Ice + Moss's care | the Biodome | 0.7 | Twin Solar Wing, Sister Drones (GDD v3) | soft teal crystals (biolum palette, gently alive) |
| **Superconductor / Siêu dẫn** | 1 Rare Earths + 1 Circuit Chips | the Smelter II | 0.8 | Star Tracker, Anchor Line, the Great Dish | a dark bar with a faint violet edge (not emissive) |

**Count:** 3 at the start, 8 raw + 5 refined = **13 by 1.0**. Components (Tower Coil and so on) stay separate story
finds, never "materials".

**Changes to GDD v3 §4.2** (for the Director):
- Alloy is refined from Ilmenite, no longer cut from heavy hulls. Heavy hulls give big Metal yields (M3-26 hull
  plates), and Heavy Tow drags them home.
- Circuitry is renamed **Circuit Chips**.
- Superconductor is refined, not found.
- The Heat Sink component is cut in favour of Coolant.

## 3. Restraint and the "aha"
- **One new material per region, one at a time.** Each arrives in four steps:
  1. **Seen first** (ruling 10's spirit): the basin's orange patches are visible from 0.5. Ines's chart note says
     "Orange glass? Check later." / "Thủy tinh cam? Để sau xem."
  2. **A place card** that says what it is and what it becomes: "Orange glass beads: volcanic glass. The Smelter
     turns them into Glass."
  3. **The first dig or drill:** 07 chirps **Curious**, and the bundle's own look and pickup note.
  4. **The first refined unit at home:** it lands on the shelves with a glow, and its first recipe lights on the
     board.
- **A purpose the player can read:** no material exists only to make another material that only makes another. Every
  raw has exactly one refined product, or is used directly (salvage).

## 4. Crafting chains (at most 2 steps)
**Raw → (home building) → refined → kit or building.**
- **Refining is automatic.** Raw lunar on the shelves is refined by its building at a visible pace (Smelter: 1 unit
  every 20 s; Ice Still: 1 per 15 s; Biodome: 1 Crystal per 3 min). The player never loads a machine or picks a
  recipe for it.
- **The base works while 07 explores.** Returning home shows new crates glowing on the shelves (DESIGN #1: home
  changes every session).
- **Salvage is never refined.** Metal, Wiring and Optics are used as they are.
- **Recipe rows always say where an input comes from** (first-hour-qa): "Hợp kim 2 (có 0) · làm ở Lò luyện từ Ilmenit
  · Ilmenit ở bậc thềm vành".

## 5. Base building: each material grows home
All stand on M3-22's home pads; ruling 14 applies (they work by themselves, and 07 only feeds and repairs).

| Building | Ver | Enabled by | Job | Visible at home |
|---|---|---|---|---|
| Supply shelves (M3-25) | 0.5.x | — | hold everything, one crate per stack | open racks under the belly that fill |
| **Smelter / Lò luyện** | 0.6 | the Core Drill era; built with Metal + Wiring | Beads → Glass, Ilmenite → Alloy | a squat furnace with a warm window; vacuum: no smoke, a glowing vent |
| Fabricator (M3-22) | 0.6 | Alloy | repeat components | as M3-22 |
| **Ice Still / Tháp chưng băng** | 0.7 | Water Ice; built with Alloy + Glass | Ice → Coolant | a tall frosted tank with a slow drip light |
| Biodome (GDD v3) | 0.7 | Moss home + Glass panes | grows Crystal from ice | a glass dome with a green glow |
| **Smelter II** | 0.8 | upgrade in place; Alloy + Coolant | Rare Earths + Chips → Superconductor | a second crucible arm, a hotter window |

**Home pads:** M3-22's 5 hold the Smelter, Fabricator, Ice Still, Drone Hangar and Paint Booth. The Bay Wing uses the
bay's side pad, and the Biodome has its own spot (GDD v3).

## 6. Interplay
- **Cargo (M3-25):**
  - more types means more slots per trip. Raw lunar stacks **6 per slot** (compact sacks and cores); salvage stays
    at 4; Rare Earths is heavy at 3.
  - Refined goods live at home and ride in 07 only with "Take for the trip" (0.6 field building).
  - A region trip carries 2–4 types, so the Cargo Pod (0.6) matters exactly when ores arrive.
- **Cutter and drill (M3-26):**
  - the **Core Drill** (0.6) is a beam kit: a short drill head on a swing arm under 07's nose, driven by the same
    charge run (the run feeds the drill).
  - E stays "the beam works on what glows": cut on salvage, dig on loose beads, drill on a glowing ore vein.
  - Cutter tiers speed up drilling as they do cutting. The Thermal Cutter opens sealed hulls for chips.
- **Battery (M3-20):** Coolant goes into the **Power Core** (0.8) and the Thermal Cutter. **No new battery mechanic:**
  ice is not fuel (it would turn range into chores).
- **Story:**
  - **Chips are the crew's handwriting in silicon.** Each sealed hull's chips carry a tiny label ("NAV · K.M.").
  - The first chip found shows Ro's note once: "Kenji says these can't be made up here. Treat them like the last
    tea." / "Kenji bảo thứ này trên đây không làm ra được. Giữ như gói trà cuối cùng nhé."
  - Ines's notes name the geology (orange glass, the dark ilmenite bands, the shadowed ice). The lunar family is her
    voice, as salvage is Kenji's.

## 7. Pacing (first three hours)
| When | New | Aha |
|---|---|---|
| 0.5, min 0–100 | Metal, Wiring, Optics | the salvage loop; the orange patches seen from the basin, Ines's pencil note |
| 0.6, ~h 1:40–2:00 | **Orange Glass Beads** (dug, no tool) and the **Smelter** | "the Moon itself is a material": the first Glass glows on the shelves |
| 0.6, ~h 2:00–2:40 | the **Core Drill**, **Ilmenite**, **Alloy** | the terraces' dark bands become Cutter Ribs and Magnetic Treads |
| 0.7 (~h 3+) | Water Ice → Coolant, Crystal; Circuit Chips | the dark crater's ice; the last human chips |
| 0.8 | Rare Earths → Superconductor | the Peak's rock and the crew's chips together make the uplink |

About one new material every 40–60 minutes. Never two in the same trip.

## 8. Contracts, migration, split
**Core:**
- `SalvageMaterial` **appends, never renumbers**: CircuitChips 3, OrangeBeads 4, Ilmenite 5, WaterIce 6,
  RareEarths 7, Glass 8, Alloy 9, Coolant 10, Crystal 11, Superconductor 12.
  - The type keeps its name for save compatibility. A doc comment notes it now means "any material".
- `MaterialFamily` (Salvage, Lunar, Refined).
- A **`MaterialCatalog`** SO (gameplay): name and job keys, family, stack size per slot, bundle prefab, icon, and
  where it is found (for recipe rows).
- `MaterialSalvaged` gains a `Source` field (Cut, Dig, Drill, Refine, Crate). `MaterialRefined(material, units,
  building)` is new.

**Migration:**
- Stock arrays grow to the enum's count, and old saves pad the new entries with zero.
- No content floor. M3-25's shelves already hold any material.

| Ver | Session | Tasks | Size |
|---|---|---|---|
| 0.5.x | Director | Core: the appended enum values, `MaterialFamily`, the `Source` field | S |
| 0.5.x | gameplay | `MaterialCatalog` with the 3 current materials; stock arrays by catalog count; a migration test | S |
| 0.5.x | world + story | the basin's orange vent field (visible, not yet usable); Ines's pencil note on the chart | S |
| 0.6 | gameplay | dig on bead patches; the Core Drill kit (drill = beam work on ore veins); the Smelter as a pad building with auto-refining; recipes and source rows; economy tests (2× rule per version) | L |
| 0.6 | art | the bead, ilmenite, glass and alloy bundles; the Core Drill head; the Smelter; the ilmenite strata on the terraces (with world) | M |
| 0.6 | rover / audio / ui | the drill arm motion and the charge run feeding it; drill and smelter sounds; place cards and recipe source rows | S each |
| 0.7 | all | Water Ice, the Ice Still, Coolant; Crystal via the Biodome; Circuit Chips from sealed hulls | M–L |
| 0.8 | all | Rare Earths, Smelter II, Superconductor | M |

## 9. Acceptance
- By 1.0: ≤ 8 raw and ≤ 5 refined. Never two new materials in one trip. Every chain ≤ 2 steps.
- Every material has a distinct bundle by colour **and** shape, a glossary name (en / vi), a place card, and a
  recipe row that says where it comes from.
- Home refining needs no input, and its output is visible on the shelves.
- Old saves load with every new material at 0. No enum value is renumbered.
