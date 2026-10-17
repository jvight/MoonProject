# Opening v2: the first 45 minutes (design, source of truth)

> Owner: the game design session, for the Director and the overnight sessions (2026-10-18). Frame:
> `D:/Project/MoonProject-wt/handoff/opening-v2/FRAME.md` (its locked decisions are not re-argued here).
>
> Built on:
> - VISION (rulings 1–17);
> - `docs/design/story-v4.md` ("The Departure"; the sealed habitat; dark Earth; the Arks at L4/L5);
> - `docs/design/scale.md` (07: 1.47 × 2.01 × 1.59 m; head top 1.525 m);
> - `docs/design/core-feel-audit.md`;
> - `docs/design/north-star.md` (the five questions, awe beats, variety);
> - the 0.5.5 spec (scanning, no floating loot, one guidance element, the inventory on 07's face).
>
> `first-hour-v2.md` is superseded where it disagrees (it assumed a prebuilt home, a radio tower and friends).
> Story text here is functional placeholder; story owns the final wording in en and vi.

## 0. Player experience (skill `player-experience`)
| Question | Opening v2's answer |
|---|---|
| **Hook** (first 5 min) | 07's lid opens **in the dark of an eclipse**. Earth hangs black with a thin copper ring and **no city lights at all**. A faint point near it (the Arks). The Peak's red blink. 07's own CRT face prints `ORBITAL SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN RECOVERY`. Then one warm light: a landed lander's emergency beacon, 165 m away, standing in front of the Peak, with an old cable running from 07's dead charging post toward it. *What happened here? Who is signalling? Why is Earth dark?* |
| **Pull** (to 45 min) | The world teaches by need. 07's battery is half empty, so it needs a charger. The charger needs a blueprint, the blueprint is in the lander's cargo, and the materials are in its crates. Then **the Sun comes back from behind Earth** (the eclipse ends), which makes solar possible. Then the ruins of a whole station sit 65 m beyond it, holding the next blueprints and the first story fact (`SUIT 01-04 · OFFLINE`). Then **night is coming**. Then a signal from beyond the west rim. Each answer opens the next question. |
| **Hold** | **07 builds its own home, piece by piece, visibly:** a pad, a solar array, a battery bank, an antenna mast, all standing where the player put them, glowing at night. The recovery protocol's first lamp (Power) lights on 07's face. |
| **Finish** (of the opening) | Power on before nightfall. The first nightfall turns the player's own base into a lit place in a dark basin. The antenna mast catches a signal, and a pillar rises over the west rim: *Link* is the next step. That is the opening's promise kept, and the next one made. |
| **Feeling** | **Lonely wonder turning into quiet pride.** Lonely wonder: the eclipse dark, a small rover, a dead station, silence except 07's servos and the hiss of a faint signal. Quiet pride: "I built that", lamps the player placed coming on at dusk, the radio clearing to music once power is on. |

**Learn, then improve (Subnautica's opening):**
- Subnautica: a crash, a lifepod, an ocean, then the fabricator and scanner teach by need.
- Ours keeps teaching by need but **replaces danger with scarcity and wonder:** no fire to put out, no thirst. Range
  (battery) and light (the eclipse, then night) create urgency, and building is the answer.
- 07's base is *its* lifepod, built by the player, not given.

**Playtest signals:** see §7.

## 1. Rules for the opening
1. **One new thing at a time.** Each beat teaches one verb or one structure.
2. **The world points first** (the cable, the beacon, scan brackets, a lit build site). The objective line exists
   only where the world can't say it.
3. **One guidance element on screen** (0.5.5 §4). The objective line counts as one. Prompt chips teach each verb
   once, then retire.
4. **No floating loot.** Every material comes out of a crate, a hull panel or a ruin; every blueprint comes from
   scanning fragments resting on something.
5. **Every structure's job reads at a glance** (purpose must read). The pad shows a rover outline; the solar array
   turns to the Sun; the battery bank shows its charge as a stack of lamps; the mast's dish turns.
6. **Calm urgency only:** battery and light. Wear and death (ruling 17) are designed but **not part of tonight's
   build**. At 0% the ruling-15 crawl applies.

## 2. The beats (0:00 → ~45:00)
Times are for a first-time player. "Exit" is the condition that ends the beat. Verbs and devices are 07's own (R1
rover):
- the **scan beam** from the lens;
- the **cutter turret** (front, 0.73 m);
- **build mode** (BUILD-CORE: menu, place, weld);
- **charging** on the pad;
- driving.

| # | Time | Beat | The player does | 07's verbs / devices | Sees / hears | Exit |
|---|---|---|---|---|---|---|
| **B0** | 0:00–1:30 | **Eclipse wake** (exists) | nothing, then look | lens shutter, face boot | Black sky, Earth eclipsed with a copper ring, no city lights, the faint Ark point, the Peak's red blink. The amber boot lines. Silence, then 07's servos and a faint signal hiss on the radio. | the pull-out to the chase frame completes |
| **B1** | 1:30–3:00 | **First look at the hold** (exists: the face boot shows the hold) | look; drive or press Tab | the CRT face (hold left, sockets right) | The hold boots: Metal 2, Wiring 1 slide in. The battery reads **50%**. The face shows a dim protocol line: `POWER · OFFLINE`. | the player drives > 5 m |
| **B2** | 3:00–4:30 | **The first drive** | drive 165 m toward the lander's amber beacon, along 07's old charging cable, past the Kestrel furrow's debris | driving | The dead charging post at the wake point, its cable on the ground leading away. The crew's faded ruts. The lander grows from a silhouette into a 13 m machine with one amber beacon. Earth eclipsed above. 07's motor, regolith crunch. | 07 within 25 m of the lander |
| **B3** | 4:30–8:00 | **Scan the lander** | scan 2 fragments of the charging-pad blueprint resting in the lander's open cargo bay | **scan beam** (hold E) | Corner brackets on the fragments (a pad coil plate, a guide rail). The beam draws white lines; each fragment dissolves into its lines, which stream into the lens (0.5.5 D2). The second fragment completes the blueprint: a Proud chirp, the CRT shows the pad's line drawing. | the charging-pad blueprint is learned |
| **B4** | 8:00–10:00 | **Cut the crates** | cut 1–2 sealed cargo crates and a loose hull panel for Metal and Wiring | **cutter turret** (hold E on a seam; the weighted charge-up, 0.5.5 §5) | The seal glows, the charge builds, the cut; bundles spiral into the rear hatch; the hold fills on the CRT. **The battery passes 30%:** the Low warning (one line, one chirp). | the hold has ≥ Metal 4, Wiring 2 |
| **B5** | 10:00–12:00 | **Build the pad** | open build mode, place the pad on the build site (the old unloading apron, 25 m from the lander), at the end of the port cable that runs there from the lander, then weld it | **build mode** (menu → place → weld) | The apron's old floor markings glow where a pad fits. The ghost pad snaps to the cable's end. The weld beam, sparks, the frame rising. The pad's rover outline lights. | the charging pad is built and connected to the lander's port cable |
| **B6** | 12:00–14:00 | **Charge, and the Sun returns** | park on the pad | **charging** (the dock-lock walls, cells filling with notes) | The pad locks 07 in and the cells fill. **The eclipse ends:** the Sun's edge breaks out behind Earth (a diamond-ring flash), and low golden light floods the basin. Then the CRT reads `LANDER RESERVE · 0%`: the lander's old cells are spent. | the battery is full and the lander's reserve is empty |
| **B7** | 14:00–22:00 | **The Lumen ruins** | drive ~85 m to the ruins; scan the fallen solar array's 3 fragments; scan the habitat's airlock panel (story); cut the workshop's lockers for Optics | scan beam, cutter | A station of human scale in the low Sun: habitat modules, Kenji's workshop, a fallen solar array, a radio tower ruin. **The airlock panel** reads `HABITAT SEAL · LOCKED · 28 OCT 1988 / SUIT 01-04 · OFFLINE · 29 OCT 1988 · 04:12`. No bodies, ever. | the solar-array blueprint is learned |
| **B8** | 22:00–28:00 | **Build solar, power on** | build the solar array within 14 m of the pad (its connection radius) | build mode | The array unfolds and turns to the Sun. A cable lights between it and the pad. **Power on:** the pad's lamps steady, the CRT's protocol line reads `POWER · ON`, the first lamp of Power → Link → Dish → Answer lights on 07's face, and the radio's hiss clears into the first music (low, warm). | the solar array is built and powering the pad |
| **B9** | 28:00–36:00 | **Night is coming** | the Sun nears the west rim; scan the habitat's power-module fragments (2); cut for Wiring; build a battery bank next to the array | scan beam, cutter, build mode | Shadows lengthen visibly. The CRT shows the Sun's remaining time as an arc (the only clock, and it is the world's). The battery bank's charge lamps fill as the array feeds it. | the battery bank is built and holds charge |
| **B10** | 36:00–40:00 | **The first nightfall** (awe beat, north-star §4a) | watch; or keep working | — | The Sun drops behind the rim. Shadows race; the array stows; **the player's own base lights come on in sequence**; the stars and the Milky Way bloom; Earth's night side stays dark (story v4), its rim glowing. The radio shifts to the night set. No text. | the Sun has set |
| **B11** | 40:00–45:00 | **Toward Link** | scan the radio tower ruin's 2 fragments (antenna mast); cut for Optics; build the antenna mast at the base | scan beam, cutter, build mode | Under earthshine (lavender night), the tower ruin's dish frame. The built mast's dish turns, listening. **A faint signal answers from beyond the west rim:** a pillar of light rises there, over a dead Lumen relay mast's silhouette on the rim shoulder. The CRT: `LINK · 0/1 · SIGNAL WEST`. | the antenna mast is built and the west signal is heard: **the opening ends** |

### After the opening: Power → Link → Dish → Answer, by building
| Step | What 07 builds | Where | What it opens |
|---|---|---|---|
| **Power** | pad, solar, battery bank | home (the lander apron) | the base runs day and night |
| **Link** | antenna masts and power pylons on high ground, a relay chain out of the basin (the rille pass west) | the basin rim, then the Long Mare (0.6) | the next region, a charger chain (range), the survey's first pieces |
| **Dish** | a great dish, built from blueprints scanned region by region (Meridian's vault, the Peak) | the Peak | the uplink |
| **Answer** | transmit the survey | — | the Arks answer (story v4 §5) |

Every step is reached by building something the player can see, never by visiting an old radio tower.

## 3. Objective lines (exact text, one at a time, no quest log)
Rules:
- **One line at a time,** high-left on screen. It appears **1.5 s after the previous step completes and 07 has been
  still for 1 s**, rests for 8 s, then dims to a small line.
- It **re-brightens only if** 07 has been still for 4 s and no progress has been made for 90 s.
- It **clears the moment its step completes.** Never shown while a prompt chip, the build menu or the CRT readout is
  on screen (one guidance element).
- Each line shows once per save. A loaded game shows the current step's line once, dimmed.

| Beat | en | vi | Shows when | Clears when |
|---|---|---|---|---|
| B1/B2 | Drive to the lander. | Lái tới tàu đổ bộ. | the face boot's readout has faded | 07 within 25 m of the lander |
| B3 | Scan the lander's cargo for a blueprint. | Quét hàng trong tàu đổ bộ để tìm bản vẽ. | at the lander | the pad blueprint is learned |
| B4 | Cut the crates for metal and wiring. | Cắt các thùng hàng để lấy kim loại và dây điện. | blueprint learned **and** the hold lacks its cost | the hold covers the cost |
| B5 | Build a charging pad beside the lander. | Dựng một bệ sạc cạnh tàu đổ bộ. | the hold covers the cost | the pad is built |
| B6 | Park on the pad to charge. | Đỗ lên bệ để sạc. | the pad is built and the battery < 100% | charging starts |
| B7 | Find a solar blueprint in the Lumen ruins. | Tìm bản vẽ pin mặt trời trong tàn tích Lumen. | `LANDER RESERVE · 0%` has shown | the solar blueprint is learned |
| B8 | Build a solar array near the pad. | Dựng một giàn pin mặt trời gần bệ sạc. | the solar blueprint is learned (and materials are covered; else "Cut the ruins for optics." / "Cắt tàn tích để lấy thấu kính." first) | the array powers the pad |
| B9 | Night is coming. Build a battery bank. | Đêm sắp xuống. Hãy dựng một khối pin dự trữ. | Power on, and the Sun < 8 min from setting | the battery bank is built |
| B11 | Scan the radio tower for an antenna. | Quét tháp radio để tìm bản vẽ ăng-ten. | after nightfall | the antenna blueprint is learned |
| B11 | Build an antenna mast. | Dựng một cột ăng-ten. | the antenna blueprint is learned | the mast is built |
| end | — (no line: the pillar in the west is the message) | — | — | — |

**Context lines** (they count as the one element, each shown once):
- **Low battery:** "Battery low. The lander's port can charge you once a pad stands there." / "Pin yếu. Cổng của tàu
  đổ bộ sạc được khi đã có bệ." (before the pad exists). After that: "Battery low. Return to your pad." / "Pin yếu.
  Hãy về bệ sạc."
- **Build mode without materials:** the build menu itself shows need and have (feel-pass §8.1). No extra line.

**Prompt chips** (glyph + one word, once each, retire on first use): Scan, Cut, Build (opening build mode), Place,
Weld, Tab (the hold, offered once in B1).

## 4. Basin layout v2
Coordinates follow World's inventory (`handoff/opening-v2/world-inventory.md` §5): **origin = today's flat pad**
(r 25 m), which becomes the wake point, so no terrain moves. Bearings are clockwise from north (+Z). Drive seconds
assume ~5 m/s average (07's top speed is 8 m/s; boost cruise ≈ 6.8 m/s).

| Place | Model (R1) | Position (x, z) | Distance / drive | Holds | Why it's there |
|---|---|---|---|---|---|
| **Wake point** | 07 on the flat pad; **a dead 07 charging post** with its cable running toward the lander (needs Argon) | (0, −4.5), yaw 355 | — | 1 sealed crate on a supply pallet (Metal 3, Wiring 1) | 07's last parking spot; its charger was fed from the lander and died |
| **The lander** | `CargoLander` (13.1 × 5.2 × 5.75 m), landed slightly tilted, **an amber emergency beacon** on top, a **service port** on the side facing 07 | ~(35, 160): 165 m at 12°, **in front of the Peak** from the wake | 165 m · 33 s | open cargo bay: **2 charging-pad fragments**; 2 sealed crates (Metal 3, Wiring 2 each); 3 loose hull panels (Metal 2 each) | the crew's cargo lander; its port fed 07's old charger |
| **Build site** (the old unloading apron) | flat plateau r ~14, faded floor markings (a ground decal), no scatter | ~(15, 140): 25 m short of the lander, toward the wake | 5 s from the lander | the pad (B5), solar (B8), battery bank (B9), mast (B11) | flat, by the lander's port, visible from the wake |
| **Lumen ruins** | `HabitatModules`, `KenjiWorkshop`, `FallenSolarArray`, `RadioTower`, `ScatterSet` as one cluster, **moved from (14, 53)** | ~(95, 185): ~65 m east of the lander, ~85 m from the build site | 17 s from the build site | `FallenSolarArray`: **3 solar fragments**. `HabitatModules`: the airlock panel (story scan), **2 battery-bank fragments**, a cable reel crate (Wiring 4). `KenjiWorkshop`: 2 lockers (Optics 2, Wiring 2 each). `RadioTower`: **3 antenna fragments**, 2 dish-frame panels (Metal 2, Optics 1 each). `ScatterSet`: 3 panels (Metal 2 each). | the station the crew left, seen from the build site |
| **Midway point of interest** | 2–3 R1 debris fragments and 1 crate, off the lane by 20–40 m (needs Argon: R1 debris) | the Kestrel furrow ~(−40, 90): 100 m at 335° | 12–15 s into the first drive | 1 crate (Metal 2, Wiring 1) | something to see on the first drive (one point every 150–200 m, north-star §4b) |
| **Kestrel-3** (optional) | the existing impact crater | (−13, 185) | 37 s from the wake | hull panels (Metal and Optics); a scan note: its foreign livery `KESTREL-3 · TYCHO CONSORTIUM · MERIDIAN RELAY` | story v4's question, off the critical path |
| **The west lure** | a dead relay mast on the west rim shoulder (an unlit `AntennaMast` variant) | relay.1 (−205, −75) | 218 m from the wake | nothing in the opening | B11's signal pillar rises behind it: Link's first target (0.6's Long Mare lies west, `0.6-world-regions.md`) |
| **The Peak** | the existing peak and its beacon (the beacon's model is legacy: **needs Argon**) | bearing 12°, 440 m, 150 m high | — | — | the endgame, always in view |

**Sightline from the wake** (yaw 355 → turning to ~12°):
- the lander's silhouette and amber beacon at 165 m;
- the Peak's red blink above and behind it;
- the eclipsed Earth;
- the Kestrel crater rim to the left.

**From the build site:** the ruins to the right (east) and the west rim's dead mast far to the left.

**Supply vs. needs** (ruling 5, about 2×):
- **Needs:** pad 4/2/0 + solar 3/2/1 + battery bank 3/3/0 + mast 4/3/2 = **Metal 14, Wiring 10, Optics 3**.
- **Supply:**
  - start: Metal 2, Wiring 1;
  - wake crate: Metal 3, Wiring 1;
  - midway crate: Metal 2, Wiring 1;
  - lander: Metal 12, Wiring 4;
  - habitat cable reel: Wiring 4;
  - workshop: Optics 4, Wiring 4;
  - radio tower: Metal 4, Optics 2;
  - scatter: Metal 6;
  - Kestrel (optional): Metal 6, Optics 2;
  - known-blueprint fragment scans: +3 Metal each.
- **Totals without Kestrel: Metal 29 (2.1×), Wiring 15 (1.5×), Optics 6 (2×).** Wiring is the scarce one on purpose:
  it makes the player look in every ruin.

**Removed from the basin** (CUT, §5): the legacy home lander (dock, lift, lab, shelves), the radio tower L1–L3, Kenji's
Rover Bay, the supply pod, the depot, garage, drill and canyon sites, the derelicts (06, Mule, the haul bot, the
drone) and the relay hut, the relay masts, cabinets and hop pads, the garage port and spare crate, the jumper wreck,
Tilly and her sled, Bell, cassettes, log tins, relics, the route posts, cable bridge and dish frames, the legacy hull
fragments, and the orange-vent beads. The Canyon's **terrain carve and R1 basalt slabs stay as scenery**, with no
content.

## 5. KEEP / CUT
- **KEEP** = the opening uses it.
- **CUT** = delete tonight, completely (code, tests, builders, generated assets, scene wiring, save fields,
  localization keys). Git history keeps it.
- Per the frame, **"later" is not a status:** if the opening doesn't use it, it is CUT.
- The inventories in `handoff/opening-v2/` have the file-level detail. This table rules on each subsystem.

### Gameplay
| Subsystem | Verdict | Reason |
|---|---|---|
| Scanning (ScanField, ScanSet, RuinScanSet, ScanFragment) | KEEP | the spine's learn step |
| Building (BuildMode, BuildField, PlacementRules, SnapSolver, PowerGrid, StructureCatalog) | KEEP | B5, B8, B9, B11 |
| Structures: charging_pad, solar_array, battery_bank, antenna_mast | KEEP | the four opening builds |
| Structures: power_pylon | KEEP | the Link step's next build (the B11 objective names it next); the build menu lists it greyed until its blueprint is known |
| Structures: smelter, fabricator, storage_rack, upgrade_bay, vehicle_pad | CUT | not in the opening |
| Salvage (cut pieces, bundles) and Containers (sealed crates) | KEEP | B4, B7, B9, B11 materials |
| Economy (MaterialStock, the hold) and the cargo hold | KEEP | the hold on the CRT face |
| Battery (model, chargers, warnings, crawl) | KEEP | range is the calm urgency |
| Base/DockLock (the walls locking on charge) | KEEP, rebound to the built charging pad | the owner's favourite charge moment |
| Base: ChargingDock, DockRest, HomeBase, HomeHalo, ShelfSlots | CUT | the prebuilt home is gone |
| Progression: Blueprints, RecoveryProgress (Power→Link→Dish→Answer) | KEEP | learning and the protocol lamps |
| Progression: StepFlow (the state-read step machinery) | REWORK | keep the machinery; replace its 14 old steps and `ProgressionKeys` with §3's steps (this *is* ObjectiveFlow v2) |
| Progression: PlaceApproaches, DormantAnswers, TowerParts and TowerPart*, BasePower, BaseProgression's old steps, BlueprintPlotter, GaragePort, DockEvening, RadioLetters, RecoveryLamps, TechTree and TechKit, IComponentGate | CUT | the tower, garage, lab and place cards are gone |
| RecoveryProgress | REWORK | the Power lamp comes from built structures (pad + solar + battery bank), not tower levels |
| Economy: HomeShelves, HomeStorage, DockUnload, TillyFetch | CUT | inventory is 07's hold only; no home storage in the opening |
| Salvage: Cutter, CutState, pieces, bundles | KEEP | the cutter; depot, garage, derelict and crew-crate catalogue entries are CUT |
| Containers (sealed crates and lockers) | KEEP | crates at the wake, the lander and the ruins (the CoilCrate and TapeCase content is CUT) |
| Equipment (sockets) | KEEP display only | the CRT shows 07's fixed devices (lens, cutter); fitting needs the bay, and fitting code is CUT |
| Upgrades (Workshop, UpgradeService, kits) | CUT | no bay, no kits in the opening |
| Friends (Tilly), Bell, Cassettes, Radio (program), Relays (restore, hop, StationReach, mast rings), Signals, Sonar/listening, Tether, Cradle, Relics, Excavation (dig), Logs, Map, Lift, Upgrades, the stale Scrap folder `.meta` | CUT | none is in the opening. The gameplay inventory suggests keeping several as future bases (Relays for Link, Map, Sonar, Logs, Upgrades as crafting), but the frame rules **unused = cut**. Link will be designed as *building* masts, not restoring old relays. |
| Hints (prompt kinds) | KEEP | add a Build kind; drop the kinds of cut verbs (Ping, Tune, Hop, Restore, Deposit, Stow, Tether, Reel, Excavate) |
| Feel (FeelBeat, the kit) | KEEP | every beat |
| Save | KEEP | **the content floor is raised:** pre-v2 saves are put away (archived, never deleted) |
| Common | KEEP | shared utilities (trim anything only cut systems used) |

### World
| Subsystem | Verdict | Reason |
|---|---|---|
| Terrain, Surface, Scatter, Sky (L1 Sun, eclipse ring, dark Earth), Lens (wake) | KEEP | the basin, the light, the wake |
| Beacon (the Peak) | KEEP | always-visible endgame |
| Layout: WorldAnchors (trimmed to: wake, lander, apron, ruins, kestrel, relay.1, peak), LandmarkPlacer, RuinPlacement | KEEP | §4 |
| Layout: RelaySettings and RelaySiteFinder | CUT | relays are built by the player from now on (relay.1 keeps only its anchor, for the dead mast prop) |
| Layout: LandmarkPlacer's route posts, cable bridge, dish frames and orange beads | CUT | legacy kit |
| Terrain/GroundMarks | REWORK | re-route the crew tracks to wake → lander → ruins |
| Lighting: NetworkPulse, ReachRing, LampBank | KEEP if B8–B11 use them (power-on, the mast's handshake), else CUT | ChargerPools CUT |
| Editor: WorldHomeWiringContributor, HomeGroundMarksBuilder, ChargerPoolSceneContributor, PeakBeaconBuilder's model | CUT | legacy |
| Sites: Kestrel impact | KEEP (optional site) | the story question, extra salvage |
| Sites: ScrapField (trail bits) | CUT | no loose loot |
| Canyon: the terrain carve and R1 gate slabs | KEEP as scenery | removing terrain costs time for no gain; it gets no content |
| Canyon: PodBeacon, LocatorBlink, and the legacy supply pod | CUT | no canyon content |
| Lighting: home lighting stages (lander windows, tower) | CUT | there's no prebuilt home. The player's structures light themselves. |
| Map | CUT | no survey chart in the opening |
| New: the build apron's floor markings, the wake point's post and cable | NEW | §4 |

### UI
| Subsystem | Verdict | Reason |
|---|---|---|
| CRT face screens: the hold, sockets display, protocol line, boot readout | KEEP | the inventory and readouts |
| Build menu (need/have rows) | KEEP | B5, B8, B9, B11 |
| Prompts (chips), the guidance slot | KEEP | teaching |
| Objective line v2 | NEW (ui-fit) | §3 |
| Pause, Settings, title, Motion, Localization | KEEP | always (prune keys of cut systems) |
| GuidanceSlot | REWORK | shrink to Objective, Prompt and Battery; every card goes behind it (ui inventory) |
| RecipeCard/RecipeView (need/have rows) | KEEP | build costs |
| UnlockCard | REWORK | "Blueprint learned: <name>" (the reward beat in B3, B7, B9, B11) |
| PickupFeed, ScanLines | KEEP | materials and "Fragment 2 of 3" |
| StargazeVeil | KEEP | it fades the HUD during the nightfall beat (B10) |
| CargoScreen, FaceInventoryView, HoldBoot | KEEP | the hold on 07's face (the hover-steals-focus bug is noted for later) |
| AbilityTable, PartInfo verbs ("07 can") | CUT | the owner banned "07 can" text |
| BatteryDial | KEEP (one charge read) | the battery |
| Friends, Map, Radio (hop list, dial, relay tag), Materials (old dials), Credits, PlaceCard, TowerPanel, Bay*, StationWork, RewardCard, KitTitle, LabHud, RecoveryBoard, MemoryCard, LetterCard, CrateNote, SiteNameTag, FriendNameTag, TetherReticle, HomeMeter, CompassPoint, NotYetLines, JourneyNotice, FitHint, LookUpHint, the ticker | CUT | none is in the opening (the airlock panel's text shows on the CRT, not as a card; there are no voices in the opening) |

### Rover
| Subsystem | Verdict | Reason |
|---|---|---|
| Driving, Camera, Character, Face, Feel, Fx, Tuning | KEEP | everything the player touches |
| Driving: Hover-Jump | CUT | no jump kit in the opening |
| Rig: kit mounts and bay fitting (ChassisMounts.Fit, BayFitting, kits' visuals), the lift ride, lab camera | CUT | no bay, no lab |
| Rig: the scan lens, cutter turret, rear hatch, solar wing | KEEP | the devices |
| Portrait | KEEP if the title or pause uses it, else CUT | the ui inventory decides |

### Audio
| Subsystem | Verdict | Reason |
|---|---|---|
| Rover audio (motor, servo, small sounds, chirps), charge, cut beam, salvage, feel, UI, wake, soundscape and ambience, stargaze mix, title music, radio station (one playlist and the signal hiss) | KEEP | the opening's sounds |
| AirWind | CUT | vacuum (and core-feel-audit VFX-03) |
| Bay arm voice, lab, lift, canyon, friends, relay and radio hop, relic answer tone, station sounds and base wake sounds (old tower and dock), radio letters and voices, recovery lift **(keep if ruling 7's "never stuck" lift uses it)** | CUT (except the recovery lift) | not in the opening |
| **New: build sounds** (place snap, weld, structure power-on), **the eclipse diamond-ring swell, the nightfall lights sequence** | NEW | B5–B10 |

### Models
| Asset | Verdict | Reason |
|---|---|---|
| R1 Rover (`RoverModel`, `LensGlass`, `ScanFan`, `SolarWing`) | KEEP | 07 |
| R1 kits (`FocusLens`, `HoverCoils`, `Kit_*`) | CUT | no kits in the opening |
| R1 `CargoLander`, `Rocks`, all R1 Ruins | KEEP | §4 |
| R1 Structures: `ChargingPad` (**fix the scale FAIL first**, scale.md §3.2: widen the ramps and rails, move the rear bollard), `SolarArray`, `BatteryBank`, `AntennaMast`, `PowerPylon` | KEEP | the builds |
| R1 Structures: `Smelter`, `Fabricator`, `StorageRack`, `UpgradeBay`, `VehiclePad` | CUT | not built in the opening |
| Every legacy low-poly generated prefab (old lander, bay, tower, shelf, dock, sites, derelicts, friends, relics) | CUT | R1 only |

## 6. Task split and acceptance
| Session | Tasks | Acceptance |
|---|---|---|
| **world** | §4's layout in World's coordinates: the wake on today's pad with the dead post and its cable; the R1 lander at ~(35, 160) with its port and beacon; the build site plateau at ~(15, 140) (flat, markings, no scatter); the ruins cluster moved to ~(95, 185); the midway debris point; Kestrel kept; the crew tracks re-routed (wake → lander → ruins); the relay.1 dead mast; remove everything in §4's "removed" list and §5's World CUT rows (code, builders, generated assets, anchors). The eclipse timing (it ends at ~12 min of a new game) and the first day's Sun (sets at ~36 min). | A capture from the wake point shows the lander in front of the Peak with the eclipsed Earth in one frame; a capture from the build site shows the ruins and the west dead mast. A new game's eclipse ends between 11 and 14 min, and the first sunset falls between 34 and 38 min. No legacy model in the scene (art QA lists only R1). compile_check and box_check PASS. |
| **gameplay** | ObjectiveFlow v2 (§3) read from game state (never stored); fragment sets (pad 2 at the lander, solar 3, battery bank 2, antenna 2 in the ruins); crate and panel yields (§4 supply); the lander port and its one-charge reserve; the pad's DockLock; the battery start at 50%; recovery Power on at B8; the west signal after B11; delete §5's Gameplay CUT rows fully, with a save content floor. **The GoldenPath Playthrough rewritten for B0–B11 (Playthrough required), runtime ≤ 10 min.** | The Playthrough passes B0–B11 in ≤ 10 min. Every objective line shows and clears per §3, and never two guidance elements at once. Zero per-frame GC. compile_check, box_check and the Playthrough PASS. |
| **ui-fit** | the objective line (show, dim, re-brighten rules, one-element arbitration with chips, the build menu and CRT readouts); the CRT protocol line (`POWER · OFFLINE/ON`, `LINK · 0/1 · SIGNAL WEST`) and the `LANDER RESERVE` and sun-arc readouts; remove §5's UI CUT rows. | Captures at B1, B5, B8 and B11 show exactly one guidance element. vi lines fit at 1080p. Box_check PASS. |
| **audio** | build sounds (snap, weld, power-on), the eclipse's diamond-ring swell, the nightfall light sequence, the radio hiss clearing to music at Power on; remove §5's Audio CUT rows (AirWind first). | Every build state change has a sound in D pentatonic at ≤ mezzo-piano. No wind in a vacuum. Box_check PASS. |
| **Argon (visuals)** | needs: the dead 07 charging post and its cable; the lander's service port and amber emergency beacon; the build apron's floor-marking decal; the `ChargingPad` scale fix (scale.md §3.2); a cable-lighting look between structures; the airlock panel's readout surface; the west relay mast's dead silhouette (unlit `AntennaMast` variant); R1 debris fragments (to replace the legacy hull fragments); R1 crates S/M (scale.md sizes); an R1 Peak beacon. | Each asset passes art QA (R1 contract, scale.md). The pad lets 07 roll on from the front with ≥ 0.1 m per side. |

## 7. Playtest signals (the opening works when…)
- At B0 the player looks around before driving (camera moves ≥ 3 s).
- At B2 they drive toward the lander **without the objective line** (it appears but they're already going).
- B3–B5 within 12 min.
- They stop to read the airlock panel (≥ 3 s).
- At B10 they stop driving to watch the nightfall (≥ 5 s still).
- At the end they drive toward the west pillar, or say they want to.
- **The owner says the opening feels coherent** and is the direction to expand.

## 8. Decisions I made (open for the Director and owner)
1. **The wake point is 07's old charging post,** 160 m from the lander, with a cable leading there: a reason to be
   there and a path. (The frame says "drive toward the landed lander"; story v4 §4.1 had 07 waking in the lander's
   lee.)
2. **The pad's first charge comes from the lander's old cells,** spent after one charge. This makes solar a real need
   (logic), not a checklist step.
3. **The layout uses World's coordinates** (the wake on today's flat pad, the lander 165 m at 12° in front of the Peak, the ruins moved ~65 m east of the lander), so no terrain moves. **World proposed the canyon as the onward lure; I chose the dead relay mast and signal in the west,** which is the approved 0.6 Long Mare. The canyon stays as scenery without content.
4. **The eclipse ends at ~12 min,** while the player is building the pad, so sunlight arrives exactly when solar
   becomes needed. The first sunset is at ~36 min, so the first nightfall lands after Power on.
5. **A battery bank before nightfall (B9)** is the logical answer to "solar stops at night". It adds one build, so
   the opening has four.
6. **The opening ends with the antenna mast and a signal from the west,** not with a trip there. The Link step's
   first relay on the west rim starts the next phase.
7. **Kestrel-3 stays** as an optional site (its foreign livery is story v4's question). Everything else outside the
   lander and ruins is CUT.
8. **Wiring is the scarce material** (1.5×); Metal is plentiful (~2×).
9. **The power pylon stays in the catalogue** as Link's next build. Everything else beyond the four builds is CUT.
10. **Unused means cut** (the frame): Relays, Map, Sonar, Logs and Upgrades are CUT even though gameplay suggested keeping them as future bases. StepFlow and RecoveryProgress are reworked, not cut.

## 9. Director rulings (2026-10-18 02:00)
1. **§8's decisions 1–10 are approved.** The canyon gets no content tonight.
2. **The sky follows progress, never a clock** (VISION: no timers). Gameplay publishes
   `Core.Events.OpeningMilestoneReached` (`FlowEvents.cs`, landed adb93815), replayed with `Restored = true` at session
   start; World's sky clock reacts:
   - `FirstCharge` (B6): the eclipse ends (the diamond-ring break-out). Expected near 12 min, but whenever it happens.
   - Until `PowerOn` (B8) the first day's Sun **holds at golden hour**.
   - After `PowerOn` the Sun sinks to "last light", low above the west rim, and **holds there** until `NightReady`.
   - `NightReady` (B9, a built battery bank holds charge): the first sunset runs (about 2–3 min) into B10.
   - After the first night the normal day cycle runs. (The real lunar Sun moves about 0.5° an hour, so a held Sun
     reads as natural.)
   - World's acceptance becomes: the eclipse ends within 2 s of `FirstCharge`; the Sun never sets before
     `NightReady`; a loaded save restores the right sky with no transition.
   - B9's line "Night is coming…" shows at `PowerOn`.
3. **Event and contract ownership tonight.**
   - Delete a Core type (event, interface) only when nothing on your branch uses it any more.
   - Core leftovers whose only users were other domains' cut code are removed in one final cleanup pass after
     every branch lands.
   - UI references Gameplay directly, so a gameplay deletion that breaks a UI consumer is fixed in the same branch
     (say so in the report).
4. **Landing order:**
   1. ui-fit's UI cuts (UI consumes Gameplay types);
   2. gameplay (flow v2, its cuts, the Playthrough);
   3. world's removals (after merging main, because Gameplay reads World anchors).
   - Audio lands any time.
   - Additive work first, deletions after the dependency has landed. Merge main whenever the Director says
     something landed.
5. **Argon** already started `box/agy-r1world` (crates S/M/L, R1 Kestrel debris kit, a big-boulder tier, the
   ChargingPad scale fix). The rest of §6's Argon list follows in a second job: the dead charging post and cable, the
   lander's port and beacon, the apron decal, the airlock readout surface, the unlit mast, the Peak beacon.
   - Until those land, place the closest existing R1 asset and list the swap in your report.

## Tóm tắt (Tiếng Việt)
- **Móc câu:** 07 mở mắt giữa nhật thực. Trái Đất đen, chỉ còn viền đồng mảnh, không một ánh đèn thành phố. Gần đó có
  một chấm sáng mờ (các con tàu Ark), và đèn đỏ trên Đỉnh. Mặt CRT in dòng `ORBITAL SIGNAL · DETECTED`. Cách 165 m có
  một tàu đổ bộ với một đèn hổ phách, và sợi cáp từ trụ sạc chết của 07 dẫn tới đó.
- **Mạch 45 phút:** lái tới tàu đổ bộ → quét 2 mảnh để học bệ sạc → cắt thùng lấy kim loại và dây → dựng bệ sạc cạnh
  cổng điện của tàu → sạc. Đúng lúc đó nhật thực kết thúc và nắng trở lại. Pin cũ của tàu cạn sau một lần sạc →
  quét tàn tích Lumen để học giàn pin mặt trời. Cửa khoá ngoài nhà ở hiện `SUIT 01-04 · OFFLINE · 1988`, không bao
  giờ có xác người → dựng giàn pin, **bật điện**, đèn Power sáng, radio rõ thành nhạc → đêm sắp xuống, dựng khối pin
  dự trữ → **hoàng hôn đầu tiên:** đèn của chính căn cứ người chơi dựng bật lên → quét tháp radio đổ, dựng cột
  ăng-ten → một tín hiệu từ bên kia vành tây, cột sáng mọc lên. Phần mở đầu kết thúc; bước Link bắt đầu.
- **Chữ hướng dẫn:** mỗi lúc một dòng, viết thẳng việc cần làm (ví dụ "Dựng một bệ sạc cạnh tàu đổ bộ."), hiện khi
  cần và tắt khi xong. Không có nhật ký nhiệm vụ.
- **Bản đồ:** tàu đổ bộ ở giữa, sân dựng căn cứ phía nam tàu, tàn tích Lumen cách tàu khoảng 65 m về phía đông, Kestrel-3 tuỳ chọn ở 185 m,
  cột relay chết trên vai núi phía tây làm mồi nhử. Mọi thứ khác bị bỏ: nhà dựng sẵn, tháp, xưởng, lab, hẻm núi, bạn
  bè, kỷ vật, băng nhạc, các mô hình low-poly cũ.
- **Giữ / bỏ:** giữ quét, cắt, xây, lưới điện, pin, khoang hàng trên mặt CRT, bảng giao thức, radio. Bỏ hẳn (git còn
  giữ lịch sử) mọi hệ thống mà phần mở đầu không dùng.
- **Chia việc:** world dựng bố cục và xoá đồ cũ; gameplay làm luồng mục tiêu mới và viết lại Playthrough (≤ 10 phút);
  ui-fit làm dòng mục tiêu và các dòng hiển thị trên CRT; audio làm âm thanh xây dựng, nhật thực và hoàng hôn; Argon
  làm trụ sạc chết, cổng điện của tàu, sửa tỉ lệ bệ sạc, vạch sân.
