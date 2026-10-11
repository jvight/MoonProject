# Lofi Lunar: Game Design v3

> **APPROVED by the owner on 2026-10-11.** Written by the story & flow session, reviewed by the game design session
> (`docs/design/review-gdd-v3.md`), finalized with the owner's answers (§11). The Vietnamese edition is
> `docs/design/gdd-v3-vi.md`.
>
> Where this document conflicts with VISION, DESIGN, STORY, ROADMAP or a feature spec, this document wins until those
> are updated (§10 lists the exact text). The opening storyboard in `docs/design/cutscenes.md` stays, with the boot
> readout added (§8). `docs/design/game-flow.md`'s session tables are superseded by §9.
>
> **The owner's two founding decisions:**
> 1. **Progression first.** "The focus should be development: upgrading the robot, unlocking and crafting new
>    features, more technology, going farther." The crew's memories are seasoning.
> 2. **The battery is fuel.** "07 must go back to charge to keep moving, and upgrades gradually let it go farther and
>    explore more." The calm rules (§3.1) keep it gentle.

## 1. The promise and the pillars
**The promise.** *A small, tired rover wakes on a dark moon, rebuilds itself and its station piece by piece, and
pushes its battery's reach a little farther every night until it can call Earth, and you never once feel stressed.*

**Pillars.**
1. **Grow, and go farther.** Every trip leaves 07 or home more capable, and every upgrade opens a place, a tool or
   more range. You can see how far you've come: on 07, at home and on the chart.
2. **Weightless calm.** Nothing snaps, nothing punishes. The battery is range, never a threat: an empty battery means
   a slow, quiet crawl home with a friend's light ahead, never loss and never failure.
3. **Free to roam.** A few main recovery targets give the game its direction (§8). Everything else (sites, finds,
   friends, secrets) is found by roaming, in any order, with no checklist.
4. **Warmth in the cold.** Cool world, warm meaning. Chargers, home and friends are the warm lights.
5. **Every touch sings, and every piece reads.** Every action has a sound in D major pentatonic. Every kit piece and
   structure reads at 30 m. 07's single gentle lens never changes.
6. **Alone, and at peace.** The owner's bar for look and sound: small 07, a vast moon, a beautiful quiet.

## 2. The core loop at each scale
| Scale | Loop | Battery's role |
|---|---|---|
| **Seconds** (10–60 s) | notice a light → ping → it answers → drive → the beam works (cut, dig, tow, stitch) → the melody climbs → a find | driving and beam work draw charge; the flank lamps answer |
| **A trip** (5–15 min) | read the range (flank lamps, the chargers on the chart) → drive out → salvage, plus the site's **heart** (a blueprint or a component) → top up at a lit mast or a pylon → home: the **lab** decodes, the **bay** builds and fits → the dock's evening: a full charge and the next light | the trip's shape: how far, where to top up, when to turn back |
| **A session** (20–40 min) | 2–3 trips and at least one **capability jump**: a kit, a bigger cell, a mast or pylon that extends range, a structure, a tower level | ends on the dock: cells full, the next light named |
| **A version** (1–2 h) | see a region → need an ability and range → find and build them → enter → light its mast, build its pylons → its tier of materials → see the next region | range grows by cells, efficiency, masts and pylons |
| **The arc** (5–6 h) | the four main recovery targets: **Power → Link → Dish → Answer** (§8), reached by whatever route the player chooses | the climb up the Peak road is the longest trip the battery ever makes |
| **After** | free roam: Ro's letters, moon drift, the last finds, photo mode, paint | the whole network charges, so range no longer limits anything |

**The rule of the find.** Every trip's heart is something the player **brings home**, visibly: a tube, a component on
07's back, a relic in the rack, a friend alongside. A find goes up the belly lift into the lab; materials go to the bay.

## 3. Battery and range
### 3.1 The rules (the calm). These become VISION ruling 15.
1. **No death, no loss, no failure screen.** Whatever 07 carries stays with it. Nothing breaks, resets or falls off.
2. **A verb in progress always completes.** A cut, stitch, dig, feed or tow that starts on the last charge finishes on
   the reserve, and the crawl starts after. A leap that can't be paid for never charges: the coils fizzle softly at
   the lip, and 07 glances at its lamps. A leap never fails mid-air.
3. **Never stranded.** At empty, 07 drops into a **low-power crawl**: 35% of cruise (~2.3 m/s), quiet, the lamp at a
   glow, the beam, leap and boost asleep, the sonar still on. The crawl never runs out: Kenji's sealed reserve cell,
   the one that kept 07 alive for decades, always moves it. The nearest charger's pillar stands on the horizon.
4. **Tilly comes by herself** (once she is home). After ~5 s of crawl she flies out and leads with her lamp, and the
   crawl rises to 55% (~3.6 m/s). A friend's light ahead, never a menu.
5. **Never far from a charger.** No point on a 0.5 trip is more than **300 m from a charger**, so the longest crawl
   takes **≤ 130 s** (≤ ~85 s with Tilly). Later regions keep the rule through masts and pylon pads (§5).
6. **Standing still never drains.** Stargazing, the wide shot and pauses cost nothing. In light, with the repaired
   solar cell, 07 slowly trickles back up to **one cell, never more**: you can always get home, but waiting is never a
   strategy. Range is measured in driving and work, never in minutes.
7. **Gentle, early warnings that name only what you know.** No red, no alarm. A spoken hint names only home or a
   charger the player has lit. An unlit charger just raises its pillar.
8. **Calm crawls.** A crawl is a pillar-6 moment: the camera eases wider, the radio thins, motes hang in 07's dimmed
   lamp, and the charger's pillar sits ahead.

### 3.2 What draws charge (tuning targets for `BatteryTuning`)
| Use | Cost | Note |
|---|---|---|
| Driving at cruise, flat | **1 cell ≈ 120 m** | uphill ×1.5, downhill ×0.5 |
| Towing | **driving cost ×1.25, no beam drain** | the game's best verb stays joyful |
| Beam work (cut, dig, stitch, feed) | **1 cell ≈ 60 s** | one site's work ≈ 0.4–0.6 cell, so range is mostly about driving |
| Hover-Jump | full leap ≈ ¼ cell; tap hop ≈ ¹⁄₂₀ | the capacitor drums pay first |
| Boost | driving cost ×2 while boosting | the drums pay first |
| Lights | +20% driving cost while the lamp bar or floodlights are on in the dark | 07 switches them by itself |
| Heavy tools (Thermal Cutter, Anchor reel) | 1 cell ≈ 30 s | revisit in 0.7 against real vaults (review §1.2) |
| Sonar ping | ~free | never discourage pinging (ruling 3) |
| Standing still | 0 | in light, trickles back up to 1 cell |
| Starting battery | **4 cells** | |

### 3.3 Chargers
- **The dock at home:** a full charge in ~4 s, inside the dock's evening. Each cell fills with a rising note
  (D E F# A B …). Docking keeps the day (saves).
- **Every lit relay mast** has a **4 m** charge pad: a worn painted ring with a low ramp lip at the mast's foot.
  - Dormant, the pad is dust-capped, a promise that reads at 50 m.
  - Lit, the mast's lamp pours a warm pool onto it, visible from 200 m.
  - Cells fill in ~3 s. At relay.2, the pad sits on the home side, never on the chasm's run-up line.
- **Field charge pylons** (0.6+), built by the player on pylon pads (§6.2), fill gaps where no mast stands.
- **Radio-hop** between lit masts is free and lands 07 on a pad.
- The **survey chart** in the lab shows every charger's circle, so trips are planned at a glance.

### 3.4 How 07 shows it
- **Flank lamps:** a strip of small amber lamps along 07's stripe, one per cell, **at most 8**. The Range-Extender and
  the Power Core each add one longer, brighter lamp at the strip's end, so the strip stays countable. It never sits on
  the rear as a pair (P3).
- **Body language:** below 30% the eye dims a little and the wing droops. A charge perks 07 up: head lift, brighter
  eye, an antenna wiggle.
- **The HUD:** a thin segmented arc around the compass. It reveals itself the first time 07 drops below 70%, and it
  hides while the cells are full and while 07 works the beam (the flank lamps carry it then).

### 3.5 Warnings
| Charge | What happens |
|---|---|
| 50% | the lamps show half; nothing else |
| 30% | one soft falling two-note (A → F#); the eye dims a little; the nearest charger's pillar rises for 20 s; the next line, once, naming home or a **lit** charger only: "Charge is getting low. The dock can top you up." |
| 15% | 07 glances toward the nearest charger |
| 0% | a soft power-down sigh; the crawl begins (§3.1) |

### 3.6 Power upgrades (all visible, ruling 11)
| Upgrade | Ver | Needs | Does | Visible (slot, §4.4) |
|---|---|---|---|---|
| **Solar cell repaired** (Tilly's gift ✔) | 0.5 | Tilly home | in light, trickles back up to 1 cell | the wing's missing cell is back |
| **Capacitor drums** (Boost Coils ✔) | 0.5 | blueprint + Wiring | boosts and leaps draw from the drums first; downhill rolls and chargers refill them | flanks: twin drums |
| **Cell Pack II** | 0.5 | blueprint (relay.2's service cabinet) + Metal + Optics | 4 → 6 cells | back: the battery spine begins; 6 lamps |
| **Efficient Hubs** | 0.6 | blueprint + Alloy + Wiring | −25% driving cost | wheels: copper-coiled hubs |
| **Cell Pack III** | 0.6 | blueprint (Bell's signal) + Alloy + Optics | 6 → 8 cells | back: the spine grows; 8 lamps |
| **Twin Solar Wing** | 0.7 | blueprint + Crystal + Optics | a faster trickle (still capped at 1 cell); abilities recover faster | body: a second wing; both fold flat while driving |
| **Range-Extender** | 0.7 | blueprint (a sealed wreck) + Circuitry + Alloy | +3 cells | back: merges into the spine; one long lamp |
| **Power Core** | 0.8 | Superconductor + Power Cell (Kenji's last sheet, already on the board) | +3 cells; every ability at full strength | back: a glowing core window in the spine; one long lamp |

### 3.7 Range by version (comfortable radius from a charger, keeping a 30% margin and a site's work)
| Version | Cells | Radius | Covers | Chargers |
|---|---|---|---|---|
| 0.5 start | 4 | ~140 m | the inner basin: the depot, Tilly's crater, the garage | the dock |
| 0.5 + masts and Cell Pack II | 6 | ~220 m from any charger | the outer basin and the canyon | the dock + 4 basin masts |
| 0.6 | 8, −25% drive | ~370 m | the Rim Terraces (or the crater, §5) | + region masts, the first pylons |
| 0.7 | 8 + 3 | ~500 m | the second mid region | + masts, pylons, solar arrays |
| 0.8 | 8 + 6, full power | ~650 m | the Peak road | + the Peak's foot mast, road-camp pylons |

### 3.8 "I need more range" moments in 0.5 (they fire on the critical path; review §1.3)
1. **Tilly and her parts:** about 28% left on the way home, ~60 m from the dock. The 30% warning fires within sight
   of home. *The dock charges.*
2. **The chasm:** about 38% at the lip. The warning fires as the player looks across, and relay.2's dormant pad and
   pillar sit 20 m away. *Masts charge.* Its service cabinet holds the **Cell Pack II** blueprint.
3. **The garage:** it lands close to the warning. Good tension, no crawl.
4. **Kestrel-3,** visible from the first frame, is **over budget on 4 cells.** It opens after the mound mast (60 m
   from it) is lit or Cell Pack II is fitted: a real range gate, seen first.

## 4. The tech tree, crafting and materials
### 4.1 How an unlock happens
1. **Find:** a blueprint or a component in a site's heart, carried home on 07.
2. **Decode:** up the belly lift into the **lab**, 07's beam feeds the tube into the **reader**: a slot at lens
   height, a spinning reel and a paper tape running to the board. It decodes in ~4 s (a moment, never a timer).
   - A node lights on the **tech board**, and the lander's big porthole gains **one lamp per unlocked node**, so its
     face counts 07's growth at 30 m.
3. **Hand-off:** a lamp pulse runs from the lab down the lander's leg and along the ground cable to the bay. The
   plotter starts drawing while 07 rides down: one continuous action.
4. **Build:** 07 parks on the bay's turntable and feeds the hopper (materials and the component), and the arms fit
   the kit (the install moment).
5. **Show:** one visible change, in its slot (§4.4).

Station nodes (tower levels, masts, structures) are fed and stitched at their own ports or pads.

### 4.2 Materials, components, blueprints
| Kind | What | Where | From |
|---|---|---|---|
| **T1 scrap** | Metal, Wiring, Optics ✔ | every basin wreck, with signatures per site (P9) | 0.5 |
| **T2 refined** | Alloy (heavy hulls), Circuitry (sealed vaults), Crystal (the crater; Moss grows it) | heavy, sealed and dark wrecks, in both mid regions (§5) | 0.6–0.7 |
| **T3 rare** | Superconductor | the wrecks of the Peak road and the dish | 0.8 |
| **Components** | Tower Coil, Transformer, Relay Spare ✔ (P1); Servo Pack, Gyro, Lens Array; Heat Sink; Power Cell | the first of each is a heart find; the **Fabricator** (§6.2) then makes any component already found once | 0.5–0.8 |
| **Blueprints** | one per kit or structure | site hearts, sealed vaults, masts' service cabinets; Bell's signals point at them | 0.5–0.8 |

**Rules for recipes:**
- Recipes grow by tier: T1 in 0.5, then Alloy and components, then Crystal and Circuitry, then Superconductor.
- No circular needs.
- No order lock between the mid regions (§5).
- Costs follow ruling 5: each version's sites hold about 2× what its nodes need. Nothing respawns except moon drift's
  small gifts.

### 4.3 The tree: 32 nodes in 6 branches
✔ = built today.

| Branch | 0.5 | 0.6 | 0.7 | 0.8 |
|---|---|---|---|---|
| **Mobility** | Hover-Jump ✔ | Magnetic Treads | Long Leap | — |
| **The beam** | Cargo Cradle ✔ | Heavy Tow | Thermal Cutter | Anchor Line |
| **Sensors** | Warm Headlamp ✔ | Wide Sonar | Floodlights | Star Tracker |
| **Power & range** | Capacitor drums ✔, Cell Pack II | Efficient Hubs, Cell Pack III | Twin Solar Wing, Range-Extender | Power Core |
| **Friends** (crafted for them) | — | Scout Module (Tilly) | Ferry Harness (Atlas) | Sister Drones (with their hangar) |
| **Station and structures** | Tower L1 ✔, L2 ✔, L3 ✔, relay network ✔ | Charge Pylon, Fabricator, Bay Wing | Solar Array, Materials Store, Lab Reader II | the Great Dish |

By version: 0.5 has 9 (8 built + Cell Pack II), 0.6 has 9, 0.7 has 9, 0.8 has 5. Pylon II is an upgrade in place,
not a node. The Deep Scan of the draft is
folded into Tilly's Scout Module, and Bay Arms II becomes the Bay Wing structure.

| Node | Needs | Does |
|---|---|---|
| Hover-Jump ✔ | blueprint (the chasm's lip, on the near side) + T1 | charged leap; opens the canyon |
| Magnetic Treads | blueprint (the basin's crawler wreck) + T1 + Gyro | climbs marked walls only (DESIGN's lane rule); opens the terraces |
| Long Leap | blueprint + Alloy + Gyro | the leap becomes a glide across the crater's rifts; needed on the Peak road |
| Cargo Cradle ✔ | blueprint (garage) + Metal | carries a find in a rack |
| Heavy Tow | blueprint (a heavy pod by the depot) + T1 | tows heavy salvage, Alloy's source |
| Thermal Cutter | blueprint + Alloy + Heat Sink | opens sealed wrecks, Circuitry's source |
| Anchor Line | blueprint (the Peak's foot) + Superconductor + Power Cell | the beam locks onto the crew's anchor posts and reels 07 up the Peak road |
| Warm Headlamp ✔ | blueprint (drill) + Optics | a wider, warmer pool |
| Wide Sonar | blueprint + T1 + Lens Array | ping range ×2 |
| Floodlights | blueprint (the canyon's terminus cache) + T1 + Lens Array | lights the deep dark; opens the crater |
| Star Tracker | blueprint + Superconductor | aims the great dish |
| Scout Module | blueprint (Bell's signal) + T1 + Lens Array | Tilly flies ahead to a pillar and scans what its site holds |
| Ferry Harness | blueprint + Alloy + Servo Pack | Atlas carries heavy salvage home from a Materials Store (§6.2) |
| Sister Drones | Circuitry + Crystal + Gyro (Tilly's own schematic) | two drones sweep loose T1 inside the network |
| Tower L1 ✔ / L2 ✔ / L3 ✔ | Coil / Transformer / Relay Spare + T1 | power to home and the lab / the bay / the lab's survey chart |
| Relay network ✔ | per mast: T1 (escalating) + its part | a charger, reach, radio-hop, the letters it caught |
| Structures | see §6.2 | |
| The Great Dish | Superconductor + Power Cell + Atlas's lift + the Link (§8) + the Star Tracker | the uplink |

### 4.4 07's silhouette: kit slots
Six slots, each showing **one** piece at a time. Later kits **replace or merge** into the slot's piece, so 07 stays
small and readable through 0.8.

| Slot | 0.5 | 0.6 | 0.7 | 0.8 |
|---|---|---|---|---|
| **Nose** | the caged lamp bar | — | **floodlights replace it**; the Thermal Cutter's heat-sink fins merge into their housing | the Anchor Line's winch drum merges beneath |
| **Roof** | — | Wide Sonar's dish; the bent antenna is straightened | — | the Star Tracker merges onto the dish mount |
| **Back** | the battery spine (Cell Pack II) | the spine grows (Cell Pack III) | the Range-Extender merges into the spine | the Power Core's window glows in the spine |
| **Flanks** | the capacitor drums | — | Long Leap's glide vanes fold along the drum housings | — |
| **Rear** | the Cargo Cradle rack (straps parallel, P3) | Heavy Tow's A-frame merges over the rack | — | — |
| **Wheels** | the odd wheel | Magnetic Treads: six matched treads; Efficient Hubs' copper hubs merge in | — | — |

- **Outside the slots:** the hover coils stay under the belly, below the silhouette. The wings are 07's body: one
  tired wing, repaired by Tilly, then a pair (0.7). **Wings and vanes fold flat while driving** and open when 07 is
  still or leaping.
- **Art rules:**
  - one design language: cream enamel, the worn orange stripe, brass fittings;
  - never spiky or militarised; the rear never reads as a face; the lens never changes;
  - by 0.8, 07's bounds grow ≤ 25% in height and ≤ 15% in width.

## 5. Exploration, regions and order
| Region | Gate (ruling 10: real, seen first, one-way exit) | Its masts and pylon pads | Its tier and friend |
|---|---|---|---|
| The basin (home) | range: masts and Cell Pack II (Kestrel is the first range gate) | mound, shoulder, canyon mouth; one pylon pad at Kestrel's rim (0.6) | T1; Tilly |
| Whispering Canyon | Hover-Jump | the terminus | T1; Bell |
| Rim Terraces | Magnetic Treads | a terrace mast; 2 pylon pads on the ledges | Alloy, Circuitry; Atlas |
| Shadowed Crater | Floodlights | a crater mast; 2 pylon pads on the floor | Crystal, Alloy, Circuitry; Moss |
| The Peak road | Anchor Line + Long Leap + Atlas (the rockfall) | the Peak's foot mast; 2 road-camp pylon pads | Superconductor; the dish |

**Order is the player's.**
- **Within a region,** sites, finds, logs, tapes and secrets can be done in any order. Friends are found in their
  regions whenever 07 gets there.
- **The two mid regions in either order.** The terraces' kit (Magnetic Treads) and the crater's kit (Floodlights) both
  need only **basin and canyon finds** (T1 plus one component each), so after the canyon the player may go
  terraces-first or crater-first. Alloy and Circuitry exist in both regions.
  - In the phased releases, 0.6 ships the terraces and 0.7 the crater, so the freedom is complete in 1.0.
  - The rule is written into every recipe now, so it holds.
- **The Link (§8) has two routes** to the Peak's foot: through the terraces' mast or through the crater's. Either
  completes it.
- **Masts in any order;** their letters always play back in time order.
- **The Peak needs both mid regions:** Atlas (terraces) and Long Leap (crater). It is the only convergence point.
- **Fixed by nature:** the tower's levels go L1 → L2 → L3 (their parts may be found in any order), and the Dish needs
  the Link.

**Why the player goes farther:**
- range grows, so the frontier keeps moving, and each version's first trip shows a light just past the old reach;
- farther hearts hold higher tiers and the next blueprints;
- every lit mast or built pylon makes the far near: a charger, and for masts a hop point, a clear radio, a stretch of
  inked chart and old letters;
- old ground pays again: each version's new salvage kind (heavy, sealed, dark, high) also hides in the basin, seen
  since 0.5;
- the Peak's red light has been in view since the first frame.

## 6. The base
### 6.1 The stations
| Station | Its job for progression |
|---|---|
| **The dock** | the full charge, the day kept (save), the dock's evening (the next light, the seen-sweep from M3-19) |
| **The radio tower** | power to home in stages (L1–L3); the radio |
| **The lander: the crew's lab** (up the belly lift, M3-19) | **where finds become know-how.** The **reader** decodes. The **tech board** (Kenji's plans for 07) carries the **recovery panel** (§8). The **survey chart** (Ines's map) shows reach, chargers and pads. A small **keepsake shelf** holds the crew's things. **Lab Reader II** (0.7) decodes sealed data cores. |
| **Kenji's Rover Bay** | builds and fits kits; the plotter draws decoded sheets |
| **Relay masts** | chargers, reach, radio-hop, letters |
| **Bell's corner** | the radio dial; her signals point at blueprints and components |
| **The biodome** (0.7, Moss) | grows Crystal |
| **The great dish** (0.8, the Peak) | the uplink |

**The lander's purpose, in one line:** *the crew's lab, where 07 turns what it finds into what it becomes.* M3-19's
geometry stays (the belly lift, the round platform as a turntable, the ring-rail camera), and its crew corners become
four lab stations.

### 6.2 Structures the player builds (on build pads)
The player builds structures for crafting and operations, but only on **predefined build pads**: snap points, never
free-form (the GDD's original rule).
- **A pad** is a rover-height plinth with a worn bolt circle and a sealed crate, a structure Kenji pre-packed (ruling
  12: built for wheels). It is visible from 50 m and named on the chart.
- **Building:** a blueprint unlocks a structure type. 07 parks at the pad, its beam feeds the crate's hopper, the
  frame unfolds by itself, and 07 stitches it (~6 s, a Moment, ruling 14). It lights, and a home-light or pillar
  joins the chart.
- **Cost:** materials (T1 first, then T2) plus, for some, a component. Repeated pylons cost a little more each time,
  which is the long-term scrap sink.

| Structure | Ver | Where (pads) | Job | Needs | Visible |
|---|---|---|---|---|---|
| **Field Charge Pylon** | 0.6 | ~2 per region: Kestrel's rim, the terraces' ledges, the crater floor, the Peak road's camps | **a charger where no mast stands.** Inside the network it draws from the masts. | blueprint + Metal + Wiring + Optics (escalating) | a slim post with a 4 m pad and a warm cap lamp |
| **Pylon II** | 0.7 | upgrade in place | charges twice as fast; its lamp is a small beacon on the chart | Alloy + Lens Array | a taller post with a second lamp |
| **Fabricator** | 0.6 | a home pad beside the bay | makes any component already found once, so progress never stalls on a missing part | blueprint + Alloy + T1 | a compact machine with a warm work light |
| **Bay Wing** | 0.6 | the bay's side pad | a second gantry: fits tier-2 kits (treads, wings, the tow frame) | blueprint + T1 + Servo Pack | a new gantry wing on the bay |
| **Solar Array** | 0.7 | a home pad, and the pylon pads beyond the network | powers a pylon **outside** the network's reach; at home, keeps the Fabricator and the lab lit when the tower is busy (cosmetic) | blueprint + Crystal + Optics | a fan of panels that tilts toward the low sun |
| **Materials Store** | 0.7 | one field pad per mid region, by its heavy wrecks; one at home | a field depot: 07 drops heavy salvage there instead of towing it home, and **Atlas** (Ferry Harness) brings it to the home store | blueprint + Alloy + Servo Pack | a corrugated shed with a crane arm; its stacks grow visibly |
| **Drone Hangar** | 0.8 | a home pad | home of the Sister Drones | (with the Sister Drones node) | a small hangar with two perches |
| **Paint Booth** | 1.0 | a home pad | 07's paint and stickers | blueprint + T1 | a booth with a striped awning |

- **Synergy with range.** Pylons are how the player extends reach where the station's masts don't go. Solar arrays
  power the pylons beyond the network. Stores remove long tows. Two structures serve the main targets: the
  **Fabricator** (repeat components, such as a second Gyro, come only from it) and **one road-camp pylon** on the Peak
  road, so the last climb is the battery's grand finale. Every other structure is optional.
- **Pads keep home readable.** Around the three masses (lander, bay, tower) there are 5 home pads, each with ≥ 4 m of
  lit ground (V3, V5). Home grows by machines on pads, friends' spots and masts' glow, never by free placement.

## 7. Friends in progression
| Friend | Found | Helps | Rescue | Gift on 07 | Crafted upgrade |
|---|---|---|---|---|---|
| **Tilly** (drone) | basin | spots glints and sites near 07 | **comes by herself when 07 crawls, and leads with her lamp** (35% → 55%) | the wing's cell: the trickle to one cell | Scout Module: flies ahead to a pillar and scans what its site holds |
| **Bell** (radio cabinet) | canyon | her signals lead to blueprints and components; the dial | — | fresh "07" paint and a pennant | — |
| **Atlas** (hauler) | terraces | lifts rockfalls, and later the dish | — | a tow hook | Ferry Harness: brings heavy salvage home from field stores |
| **Moss** (garden bot) | crater | the biodome grows Crystal | — | a flower painted on the patch | — |
| **Sister drones** | crafted, 0.8 | sweep loose T1 inside the network | — | — | — |

Friends are found in any order within their regions, and they help in ways that save time and battery: automation
as warmth, never a menu.

## 8. The story: 07's directive, and a free moon
**The goal.** When the crew evacuated in the storm, they left 07 two things. One is the words "Keep the radio on."
The other is the **Lumen Recovery Protocol** in its memory: if Earth's signal is ever heard, restore the station and
answer. Decades later, a faint signal from Earth (Ro's station jingle in the static) wakes 07.

**The opening's boot readout (owner: yes).** During the opening's electronics flicker (cutscenes O5), 07's lens shows
a faint readout for ~2 s, in the crew's old terminal type: **"EARTH SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN
RECOVERY"** (localized). Then the jingle pierces the static and the look-around begins.

**The main recovery targets: the only directed part of the game.** They are shown on the **recovery panel**, the
header of the lab's tech board. Each has a row of lamps, and the chart draws the Link's line toward the Peak.

| # | Target | Done when | Progress marker |
|---|---|---|---|
| 1 | **Power the station** | tower L1, L2, L3 | 3 lamps |
| 2 | **Link the network to the Peak** | a chain of lit masts from home to the Peak's foot, by either route | one lamp per mast on the chain; the chart's line grows |
| 3 | **Repair the great dish** | Atlas lifts it, Superconductor, the Power Cell, the Star Tracker aims it | 4 lamps |
| 4 | **Answer Earth** | the broadcast | the panel's last lamp |

**Everything else is free.** Sites, tech finds, kits, structures, friends, tapes, logs, memories and secrets are found
by roaming, in any order (§5). There is no list of them anywhere. The world points by itself: glints, pillars, Bell's
signals, Tilly's spotting, the chart's pencil notes.

**The crew's fate, discovered along the way (seasoning):**
- **The masts' letters:** each lit mast plays back what it caught. The crew landed safely; a return was studied,
  then cancelled; the program closed. Then Ro's own letters: she kept writing for twenty years.
- **Logs and memories:** they lie in hearts beside the tech finds, and go to the lab's keepsake shelf.
- **Kenji's logs** frame the growth: he fixed 07 seven times, hid a spare wheel for "next time", and his last line is
  "Don't let anyone retire you." 07's upgrades are Kenji's "next time", kept.

**The ending (owner: Ro answers).**
- 07 climbs the Peak with everything it has become, Atlas lifts the dish, and the Star Tracker aims it.
- The uplink opens, and Bell sends Side B of Ro's mixtape up the network.
- After a silence, a light on Earth's night side blinks the station's call sign, three short, one long, and 07's lens
  opens fully for the first time.
- "I heard you, 07. I'm still here. — Ro." Credits.
- After the credits, the recovery panel reads complete, Ro's letters keep coming, and the moon stays open.

## 9. Sessions and pacing
**Pacing rules:**
- ≤ 3 min without something new in view;
- a capability jump every session;
- a new verb every version;
- no grind (2×);
- no wasted trip (early warnings, ≤ 300 m to a charger, a crawl that always gets home).

The tables show **one typical path.** After S2 the player chooses the order of sites, and after the canyon, the
order of the mid regions.

### 0.5 "Lumen wakes" (~100 min): Power, and the basin's network
**0.5 scope:** the battery core, chargers (the dock and masts), the crawl with Tilly's lamp, Cell Pack II and the lab.
There are no structures or T2 in 0.5.

| # | Min | What's new | Range | 07 | Home |
|---|---|---|---|---|---|
| S1 | 0–15 | the opening (the boot readout); the depot; the coil; tower L1 (Power 1/3) | 4 cells | the eye opens; four flank lamps light as it wakes | windows, the belly lamp, the radio clears; the lab wakes |
| S2 | 15–30 | Tilly; her parts and the first warning (the dock charges); her gift | trickle to 1 cell | the wing's cell | Tilly's perch |
| S3 | 30–45 | the chasm: the Hover-Jump blueprint at the lip, **relay.2 lit (masts charge)**, the Cell Pack II blueprint; the first lift ride, the lab decodes, the lamp pulse to the bay; the garage; tower L2 (Power 2/3) | + the canyon-mouth charger | coils; the battery spine (6 lamps) | the bay, the plotter, the board's first lamps, the porthole lamps |
| S4 | 45–62 | the leap; the canyon; Bell; the terminus mast; radio-hop | the canyon covered | Bell's paint | Bell's corner, the jingle clear |
| S5 | 62–75 | tower L3 (Power 3/3: the first main target done); the chart inks reach and chargers | — | the rack or the drums | the chart's glow in the porthole |
| S6 | 75–100 | Kestrel (the first range gate); the drill; the mound and shoulder masts (Link begins) | the whole basin | the lamp bar | four masts lit |

### 0.6 "Rim Terraces" (~80 min): Link, the first route
| # | Min | What's new | Range | 07 | Home and field |
|---|---|---|---|---|---|
| S7 | 0–35 | moon drift; the basin's new wrecks (Servo Pack, Gyro, the heavy pod); **build pads appear**: the Fabricator, the Bay Wing, the first pylon at Kestrel's rim; Magnetic Treads | 8 cells after Cell Pack III | six matched treads, copper hubs | the Fabricator, the Bay Wing, the first pylon |
| S8 | 35–80 | the terraces: Heavy Tow, Alloy, ledge pylons, the terrace mast (Ro's first letter), Atlas, Wide Sonar, the Scout Module | the terraces covered | the dish and straight antenna, the tow frame | Atlas by the bay; Link + terrace mast |

### 0.7 "Shadowed Crater" (~90 min): Link, the second route, and the tools for the Peak
| # | Min | What's new | Range | 07 | Home and field |
|---|---|---|---|---|---|
| S9 | 0–40 | Floodlights; the crater; Moss; Crystal; the Twin Solar Wing; Pylon II | 11 cells | **two wings**, floodlights | the biodome |
| S10 | 40–90 | Thermal Cutter → Circuitry (and Kestrel's sealed core at home); Long Leap; the Range-Extender; Solar Arrays; Materials Stores and the Ferry Harness; Lab Reader II; the crater mast | ~500 m | vanes, fins, the long lamp | stores and arrays; the lander's new roof antenna |

### 0.8 "The Peak" (~60 min): the Dish, and Answer
| # | Min | What's new | Range | 07 | Home and field |
|---|---|---|---|---|---|
| S11 | 0–35 | the Peak's foot mast (Link complete); the Anchor Line; the Power Core; the road-camp pylon; the Peak road | 14 cells, full power | the winch, the core window | the Drone Hangar; the Sister Drones |
| S12 | 35–60 | the Star Tracker; Atlas lifts the dish; the broadcast; Earth answers | — | the tracker on the dish | the Peak's beacon turns amber |

### 1.0
No new region. The mid regions become fully order-free. Also: polish, the title and collections (M3-09), Settings
("Gentle camera", the next line, the battery HUD), controller support, photo mode, the Paint Booth, the after-ending
letters, and the events (Eclipse, Solar Flare: a flare fills every cell, Earthrise).

**Main arc ≈ 5.5 h; completion ≈ 9–10 h.**

## 10. KEEP / CHANGE / CUT, against what is built
| Built | Verdict | How |
|---|---|---|
| The M3-16 power spine and steps 0–8 | **KEEP** | it is the first main target. The battery's first lessons slot into steps 3–4 (§3.8). |
| P1 story parts | **KEEP, renamed** | **components** (text and keys) |
| Blueprints and tubes (P7) | **CHANGE** | decoded at the lab's reader; the lamp pulse hands off to the plotter. Hover-Jump is found at the chasm's lip, and **Kenji's pre-pinned sheet is removed** (no duplicate). |
| Kenji's Rover Bay, the plotter | **KEEP** | the Bay Wing extends it in 0.6 |
| The belly lift (M3-19) | **KEEP** | it goes up into the lab; restored on the first find brought home |
| M3-19's crew corners | **CHANGE** | four lab stations; the porthole gains a lamp per node |
| The memory shelf outside, the deck lookout | **CUT** | as M3-19 |
| The crew's map (M3-07) | **KEEP, moved** | the survey chart in the lab; it shows chargers' circles, pads and the Link's line |
| The dock | **CHANGE** | "where 07 charges and keeps the day"; the evening fills the cells |
| Relay masts (M3-06) | **CHANGE** | 4 m charge pads (dormant and lit states); the letters stay |
| The opening (cutscenes §1) | **CHANGE** | O5 gains the ~2 s boot readout; the flank lamps light as 07 wakes |
| Stargazing, the wide shot, radio by distance | **KEEP** | standing still never drains; the trickle stops at 1 cell |
| Tilly and Bell, their gifts | **KEEP** | Tilly comes by herself and leads with her lamp when 07 crawls |
| Kits: Hover-Jump, Cradle, Headlamp, Boost Coils | **KEEP, re-slotted** | the M3-11 pieces map onto the six slots (§4.4); the drums buffer charge |
| Wide Sonar | **CHANGE** | moves from 0.7 to 0.6 |
| Deep Scan (draft) | **CUT** | folded into Tilly's Scout Module |
| The P8 next line | **KEEP** | plus the one battery hint at 30%, naming only known chargers |
| The economy (P9) | **KEEP** for T1 | T2/T3 from 0.6 |
| The "home arc" framing, the crew corners as the reason to play | **CUT** | memories, logs and letters stay as seasoning |
| A linear protocol | **CUT** | four main targets, everything else free (§8) |
| Module building, free placement | **CUT** | structures only on predefined pads (§6.2), from 0.6 |

**New for 0.5:**
- gameplay: the battery model (plain C#, EditMode-tested), chargers (the dock, mast pads), costs from
  `BatteryTuning`, Cell Pack II, the crawl rules, "a verb completes", the leap refusal, Tilly's lead, the recovery
  panel's state;
- rover: the crawl, the flank lamps, the eye and wing at low charge, the calm crawl camera;
- ui: the HUD arc (hidden while working), the boot readout, the panel's labels;
- audio: the cell notes, the warning motif, the power-down sigh;
- art: the lamp strip (8 sockets), the battery spine, 4 m mast pads, the lab's stations, porthole lamps;
- world: the pads' warm pools, the lamp pulse along the ground cable.

**Core contracts:** `IRoverBattery` (Cells, Charge01, IsLowPower), `BatteryChanged` (on cell boundaries only, zero
GC), `LowPowerChanged(bool)`, `ChargingChanged(bool, ChargerKind)`; `TechNodeId`, `ITechTree`, `TechNodeUnlocked`;
`RecoveryTarget` with `RecoveryProgressChanged`; `place.lab`. From 0.6: `BuildPadId`, `StructureBuilt`.

**VISION, DESIGN and spec text to change:**
- **VISION anti-goals:** "Health, oxygen, fuel, damage, enemies, failure screens, timers." becomes "Health, oxygen,
  damage, enemies, failure screens, timers. 07's battery is its range, never a threat (ruling 15)."
- **VISION pillar 5 (gentle curiosity):** keep "No timers, no quest log, no fail states, no punishment". Add: "A few
  main recovery targets show their progress at home; nothing else is listed. The battery measures driving and work,
  never minutes."
- **VISION ruling 7:** add "Never stranded: at empty, 07 crawls slowly and quietly to the nearest charger, Tilly's lamp
  leading it, losing nothing."
- **New VISION ruling 15, "The battery is range, never a threat":** the rules of §3.1, including "a verb in progress
  always completes" and "a leap refuses at the lip, never fails mid-air".
- **VISION pillars:** as §1.
- **DESIGN "Station reach":** "There is never a battery, fuel or range limit that forces 07 home…" becomes "Range is
  07's battery. Every lit mast and every built pylon is a charger, so lighting the network and building pylons is how
  range grows."
- **DESIGN "Why people come back":** 07 and home grow (1), the moon opens with range and tools (2), the freedom to
  roam (3), friends (4), the crew's story as seasoning (5).
- **DESIGN regions and "Base life":** the gates of §5, and structures on build pads (§6.2) besides decorations.
- **M3-16 places:** the dock's job line becomes "Where 07 charges. A rest here keeps the day."
- **M3-06:** "Outside reach… nothing bad happens" becomes "outside reach 07 runs on its battery; masts and pylons
  charge".
- **M3-11:** the pieces follow the slots (§4.4).
- **cutscenes O5:** the boot readout.
- **ROADMAP:** each version names its tier, range and structures; the battery lands in 0.5; Wide Sonar moves to 0.6;
  build pads arrive in 0.6.

## 11. Owner decisions (2026-10-11)
| # | Question | Decision |
|---|---|---|
| 1 | The empty battery | **Yes, crawl home.** Tilly comes by herself and leads with her lamp (35% → 55%), with no tow line (review's cheap form). |
| 2 | A boot readout in the opening | **Yes,** ~2 s in 07's lens (§8) |
| 3 | The solar trickle | **Yes,** slow, capped at 1 cell |
| 4 | Range tuning | **The review's numbers:** 1 cell ≈ 120 m, 4 cells at start, the beam ≈ 60 s per cell, tow = driving ×1.25 with no beam drain, ≤ 300 m from a charger, 4 m pads |
| 5 | The recovery protocol | **Changed:** not linear. Four main targets with a progress marker (Power, Link, Dish, Answer); everything else free, in any order (§5, §8) |
| 6 | The ending | **Yes,** Ro answers |
| 7 | Building | **Changed:** the player builds structures for crafting and operations on predefined pads (§6.2) |
| 8 | Length | **Yes** |

Also adopted from the review:
- six kit slots, with later kits replacing or merging;
- wings and vanes fold while driving;
- flank lamps capped at 8;
- "a verb always completes", and the leap refusal at the lip;
- hints that name only known chargers;
- the HUD arc hidden while the beam works;
- the calm crawl camera;
- the lamp-pulse hand-off, and the porthole lamps per node;
- the scoped 0.5.

## 12. Amendments from Subnautica (owner, 2026-10-11)
The owner pointed to Subnautica as a reference. Its spine is ours: a limited range (oxygen there, the battery here),
upgrades to go farther, no minimap, landmarks and radio as guidance, a base that is truly home. The owner adopted four
of its lessons. They change 0.6 onward; **0.5's scope is unchanged** (phased releases).

| # | Lesson | Ours | Ver |
|---|---|---|---|
| A1 | **Blueprint fragments.** A blueprint is learned by scanning 2–4 fragments spread through a site, so every site is explored fully and "almost there" pulls the player on. | From 0.6, a kit's blueprint is split into 2–4 fragments in one site (or one region for big kits). 07 scans each with its lens (the sonar-and-look verb, no new button). The lab's reader shows the partial tape (1/3, 2/3) and decodes when the set is complete; the board's pin shows the count. 0.5's single tubes stay as they are. | 0.6 |
| A2 | **A great wreck seen from the start and revisited with each new tool** (the Aurora). | **Kestrel-3 becomes our Aurora.** 0.5: its outer hull and the Warm Headlamp heart (built). 0.6: a sealed hatch is visible and named on the chart, a promise. 0.7: the Thermal Cutter opens the sealed vault (Circuitry, a sealed data core for Lab Reader II, a crew log). 0.8: its core holds the Power Cell's last part, so the Peak climb starts from the first wreck the player ever saw. | 0.6–0.8 |
| A3 | **Radio that guides softly**: a caught signal gives a place without a quest log. | When the radio, Bell or a newly lit mast catches a signal, Kenji's chart pen draws a pencil mark on the survey chart (a cross, a circle, a small note in Ines's hand), and the place's pillar rises once. Never an arrow or HUD marker; tier-2 finds stay unnamed (ruling 16) — the mark is a place, not a task. | 0.6 |
| A4 | **A mid-game emotional turn** (the Sunbeam). | Midway through the Link target, the half-repaired network catches Earth for a few seconds: static, a fragment of Ro's station jingle, a voice too faint to read, then gone. The lab's panel lights the Dish row's first lamp as a question mark. Sad but warm (pillar 6): it turns "repair the dish" from a task into a wish. No threat, no timer. | 0.7 |

**Proposed, not yet decided** (backlog): each region's own light and sound identity with darkness as awe (a great
wreck's silhouette slowly revealed in the floodlights, 0.7); Atlas towing a mobile charger as a moving base (the
Cyclops idea, 0.7).

**Not taken:** hunger and thirst, predators, losing items on failure, resource grind, free-form building UI.
