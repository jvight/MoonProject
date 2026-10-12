# M3-25: Cargo and storage

> Owner: Director (design), drafted by the game design session. **Draft for the owner's approval.**
> The owner, after playing 0.5.2: "an inventory mechanic close to Subnautica's, to motivate the player to upgrade
> their storage. The storage inside the robot's body is limited, so they need to build or upgrade storage chests at
> home. Don't show the items I have next to the battery."
>
> Rulings 1 (no loss), 5 (no grind), 11 (every upgrade shows), 14 (07 has no hands) and 15 (battery) apply, as do
> pillar 6 and the writing rules (feel-pass §8.1). It builds on:
> - `MaterialStock` (today one shared total, spent by recipes);
> - the Cargo Cradle (relics);
> - M3-19 (the belly and the lab), M3-22 (build pads);
> - feel-pass §3, §4, §7 (the beats, `FeelBeat`, the chirps).

## 1. The idea in one line
**07 carries what fits. Home keeps the rest.** Range (the battery) decides how far 07 can go. Cargo decides how much
one trip brings back. The two upgrade in turn, so every trip that ends with "I had to leave a bundle" points at the
next upgrade.

## 1a. Learning from Subnautica, and doing better (revision, owner 2026-10-12)
The owner: "learn from Subnautica, but improve on it, so it's better and fits our game."

| Subnautica's pain | Lunarion's answer |
|---|---|
| **Inventory tetris:** items of many shapes, manual rearranging | Only materials take space, and they pack themselves into stacks. Story items have their own seats. There is nothing to rearrange. |
| **Labelling lockers:** opaque boxes need signs to be usable | **Open shelves** (§3.1): every stack is a crate in its material's colour, visible from the dock. There are no labels to make. |
| **Walking to the right locker to craft:** the fabricator reads only what you carry | Every recipe at home reads **home plus 07**, and the card says where it came from ("home 6, 07 3"). |
| **Not seeing what a locker holds** without opening it | Shelves show their crates. The Tab screen sums home by material. |
| **Base clutter:** lockers placed anywhere, the base fills with boxes | Shelves stand only on the belly ring's predefined sockets and grow in tidy tiers. Home stays three masses (V5). |
| **Losing track of things left outside** | A bundle that didn't fit glows, answers listening, shows on the survey chart as a small pencil crate, and **Tilly fetches it** when it lies within the station's reach (§1b, T3). |
| **A full inventory interrupts harvesting** | 07 still cuts. The bundle waits beside it. |
| **Unloading by hand after every trip** | Unloading is automatic at the dock, inside the charge. |

## 1b. Lunarion's twists
| # | Twist | Pillar | What it changes |
|---|---|---|---|
| **T1** | **What 07 carries is what you see.** The load lives on 07's body: crates stacked in the rear hatch window, bundles strapped to the rack, the side bins' lids sitting on their stacks. The suspension settles about 1 cm per quarter of the hold filled (cosmetic only: no speed or handling change, never a penalty). When the hold is full, 07 glances back at its load once. **The Tab screen is secondary:** it confirms what the body already says. | 11 (every upgrade shows), 07's character | §2.4: the body is the primary readout |
| **T2** | **Open supply shelves, not lockers.** Home keeps materials on open rover-height racks under the belly ring, one crate per stack in its material's colour (Metal grey, Wiring amber, Optics cyan-glass). The stash visibly grows trip by trip, and the shelves are lit warmly at night: a cozy pantry you can see from the dock. | 2 (warmth), 6 (calm), 11 | §3: "locker" becomes **shelf** everywhere; contracts renamed |
| **T3** | **Tilly fetches what was left.** Once Tilly is home, any waiting bundle **inside the station's reach** (the tower's circle or a lit relay's) gets fetched. Every ~40 s she flies out, hooks one bundle under her body, and brings it to the shelves while 07 does anything else. Outside reach, bundles wait for 07 or for the next relay to be lit. A friend gets a real, visible job, a chore disappears, and the relay network gains one more reason. | friends (DESIGN #2), the network | §2.3, §5; overflow stays a pull toward cargo upgrades (Tilly is slow and reach-bound) |
| **T4** | **The crew's supply crates.** Five sealed crew crates, one per salvage site, each beside its heart. One beam cut opens the lid. Inside are 6 units of the site's signature material and **a packing note in the crew's hand**, e.g. Kenji: "Spares for the next rotation. Don't let Ines take the good wire." The crate frame stays as an empty prop. | the story as seasoning, ruling 5 | §4: replaces the flat +25% yield raise with story-bearing supply |

Judged and not taken: Bell announcing every crate (too chatty for pillar 6); weight slowing 07 (a penalty, ruling 1's
spirit).

## 2. 07's cargo hold
### 2.1 What goes in it, and what never does
| Item | In the hold? | Where it rides | Why |
|---|---|---|---|
| **Material bundles** (Metal, Wiring, Optics; Alloy and Crystal from 0.6) | **yes** | the hold's slots | the only thing storage limits |
| Components (Tower Coil, Transformer, Relay Spare…) | no | 07's back socket, as built | progress is never blocked by space |
| Blueprint tubes | no | the tube socket, as built | same |
| Relics and keepsakes | no | the tether or the Cargo Cradle's rack, as built | same; they are story, not stock |
| Ro's tape deck, cassettes | no | the flank (the deck); the tape case (a collection) | same |

**Rule:** only materials take space. A find that moves the story always has its own seat. Storage is about *how much*
the player gathers per trip, never *whether* they can progress.

### 2.2 Slots and stacks (Subnautica's grid, without the juggling)
- The hold is a **grid of slots**. A slot holds **one material, up to 4 units** (a stack).
- A piece yields a bundle of 2–4 units (today's yields). On arrival, it **fills existing stacks of its material
  first**, then opens new slots. **It packs itself: the player never drags or sorts.**
- **T2 bundles** (0.6) are heavier. An Alloy stack takes two slots (1 × 2) and holds 4 units. That's the
  Subnautica touch, still auto-packed.
- **Capacity by upgrade:**

| Stage | Slots | Units (all T1) | How |
|---|---|---|---|
| start | **8** (4 × 2) | 32 | 07's own body hold |
| + Cargo Cradle (0.5, built) | **+2** while the rack carries no relic | 40 | bundles strap onto the rack |
| + **Side Bins** (0.5.x, new kit) | **+4** | 56 | two low panniers between the wheels |
| + **Cargo Pod** (0.6, new kit) | **+6** | 80 | a rounded pod on the back spine |

### 2.3 When it's full
- **Before:** when the hold is full and 07 aims at a piece of salvage, the hold ring shows a small crate mark and the
  piece's halo is a shade dimmer. Cutting is still allowed (ruling 4: nothing refuses the player).
- **After the cut:** the bundle tumbles and folds as usual (feel-pass §3), flies toward 07, bumps softly against the
  closed hatch ("tok") and **settles beside 07 as a pickup**. It glows softly and pulses cyan every 3 s.
  - 07 chirps **Worried** (feel-pass §7.2).
  - Once per trip, the line: **"Cargo full. The bundle will wait here."** / **"Khoang hàng đầy. Gói vật liệu nằm chờ
    ở đây."**
  - **A partial fit:** what fits goes in, and the remainder stays as a smaller bundle.
- **A pickup waits forever** (ruling 1). Listening answers it like any find. Driving over it with free space takes
- **Tilly fetches it (T3).** Once Tilly is home, a waiting bundle inside the station's reach (the tower's circle or a
  lit relay's) is fetched: every ~40 s she flies out, hooks one bundle under her body and brings it to the shelves.
  Outside reach it waits for 07, or for the next relay. A waiting bundle shows on the survey chart as a small pencil
  crate until it is home.
  it. Radio-hop never moves it.
- Never a refusal sound, never a red colour, never a timer.

### 2.4 How the player sees it (no HUD stock)
- **No permanent counts** (the owner: nothing beside the battery). The pickup notifications (UI, in flight) say what
  arrived ("+3 Kim loại").
- **On 07's body (ruling 11, T1), the primary readout:** every filled slot shows, and the suspension settles about
  1 cm per quarter filled (cosmetic only). When the hold fills, 07 glances back at its load once.
  - The body hold's rear hatch has a small window strip that fills with stacked crates.
  - The rack's straps hold bundles.
  - The side bins' lids sit on visible stacks.
  - So "how full" reads from the chase camera.
- **The cargo screen (Tab / View button),** in the Moonlight Glass style:
  - **Left:** 07's grid, slots drawn as rounded cells with the material's icon and its units (e.g. "4"). Empty
    slots are faint outlines. The kits that add slots are labelled at the grid's edge ("Side Bins: +4").
  - **Right:** home storage, the same grid per shelf (§3). This is where the player learns "home keeps the rest".
  - **Bottom line,** plain: "07: 6 of 8 slots · Home: 11 of 24 slots".
  - Tab again or B closes it. **The game does not pause** (the radio keeps playing, 07 stays still). It opens over a
    blur in 200 ms (polish-pass §1.2).

## 3. Home storage
### 3.1 What it is: open supply shelves (T2)
- **The crew's supply shelves:** open, rover-height racks under the lander's belly ring, beside the lift, built for
  wheels (ruling 12). Every stack sits on them as a crate in its material's colour, so what home holds is visible
  from the dock, lit warmly at night. Nothing to open, nothing to label.
- **Start:** 2 shelves × 12 slots = **24 slots (96 units)**. They come with the base and get power at tower L1.
- **Building more:** each new shelf goes on a **shelf socket** around the belly ring. Sockets are predefined spots,
  never free placement (M3-22's rule).
  - 07 parks at a socket, the beam feeds Metal (and Wiring), and 07 holds E to repair.
  - The rack unfolds from its crate by itself and its lamp lights.

| Shelf | Cost | Slots | Visible at home |
|---|---|---|---|
| 1–2 (built in) | — | 12 + 12 | two open racks under the belly |
| 3 | Metal 3 | +12 | a third rack on the ring |
| 4 | Metal 4, Wiring 1 | +12 | a fourth rack; the ring now half full |
| **Shelf tiers** (upgrade, 0.6) | Alloy 2 per shelf | ×1.5 per shelf | each rack grows a second tier, taller at home |
| **Supply crane** (0.6, a home pad, M3-22) | blueprint + Alloy + T1 | — | a small crane arm over the shelves; unloading is quicker and the stacks it lifts are visible |

### 3.2 Unloading at the dock: automatic
- **When:** every time 07 locks into the dock (feel-pass §7.1), during the charge. **No press.**
- **Why automatic:** the dock is already the end of every trip. A press per trip would be a chore repeated 30+ times
  for no choice. The choice in our game is *where to go*, not *what to unpack*.
- **What moves:** every material slot of 07 goes into home, up to home's free space. If home is full, the rest stays
  in 07, and once the line **"Home storage is full. Build another shelf."** / **"Kho ở nhà đã đầy. Hãy dựng thêm một kệ."** shows, with the free socket's pad lighting.
- **Nothing is ever lost** (ruling 1). A full home only means 07 leaves with less room.

### 3.3 Crafting draws from both
- Every recipe (tower levels, kits, masts, shelves) reads **home + 07** and spends **home first, then 07**.
- So the player never needs to fetch materials from home before building at home.
- Recipe cards show "need, then have" (feel-pass §8.1): "Kim loại 4 (có 9: nhà 6, 07 3) ✓".
- **Field building (0.6 pylons on field pads)** reads **07 only**, since home is far away. That is when packing
  matters. The cargo screen then gets one action: **"Take for the trip"**, which loads chosen materials from home
  into 07's free slots. It is never needed in 0.5.

### 3.4 Motivation without chores
- **The pressure is "one more trip", never juggling.** Full cargo means: go home (short in the basin, or hop), unload
  automatically, come back. The upgrade means fewer trips, a reward you feel.
- **Range and cargo leapfrog:**
  - early, one site fits one trip;
  - Cell Pack II lets 07 chain two sites in one trip, and that is when the hold fills first;
  - Side Bins answer it;
  - in 0.6, the terraces' longer trips call for the Cargo Pod.
- **No item management for its own sake:** no sorting, no dropping, no weight, no decay. Components and finds never
  compete for space.
- **Home storage is a slow, visible build:** shelves fill and new racks appear on the belly ring. It's a second
  thing at home to grow, never a wall.

## 4. The economy (first ~2 hours, 0.5 basin)
Yields are estimates from `SalvageCatalog` (26 pieces) at ~3 units per piece, the garage at its ×2 scale.
**Gameplay replaces them with exact sums in its slice's tests.**

| Site | Pieces | ≈ Units | Slots needed (stacks of 4, by material) |
|---|---|---|---|
| Depot (min 3–7) | 4 | ~12 | 4 |
| Garage (min 17–27) | 3 × 2 | ~18 | 5–6 |
| Drill | 5 | ~15 | 4–5 |
| Kestrel | 7 | ~21 | 6 |
| Crashed lander (canyon) | 7 | ~21 | 6 |
| Trail bits | — | ~8 | — |
| **Supply** | | **≈ 95** | |

**Needs in 0.5:** tower 6/9/8, kits 13/4/7, masts 13/5/0, shelves 3 and 4 at 7/1/0, so **≈ 72 units**. Ruling 5
asks for about 2× (≈ 145). Today's 0.5 is already below 2× (P9's target was never fully reached).
- **Revision:** the five crew supply crates (T4) add 5 × 6 = **30 units** with a story, instead of a flat yield raise.
- Supply becomes **≈ 125 units (1.7×)**.
- If the playtest still feels tight, raise piece yields ~10% on top.

**Pressure points (8-slot start):**
| When | What happens | Felt as |
|---|---|---|
| min 3–7, depot | 4 of 8 slots | nothing (correct: first trip) |
| min 17–27, garage | 5–6 of 8 slots | the hatch window looks full; no overflow |
| **~min 60–75**, with Cell Pack II, drill + Kestrel in one trip (~36 units, ~10 slots) | **the first "Cargo full"**: a bundle waits at the Kestrel | **"I need more space"**: the Side Bins sheet is the next find (the crashed lander's heart in the canyon) |
| ~min 80–100, home | the stock at home passes ~80 units if the player hoards | "Home storage is full" → shelf 3 (Metal 3) |
| 0.6 | longer trips and Alloy's double slots | the Cargo Pod, shelf tiers, the crane |

Rules for tuning:
- the first full hold never happens before minute 45 (the first-hour onboarding stays clear);
- one site always fits one trip at every stage;
- home's built-in shelves never fill in the first hour.

## 5. Feel (feel-pass beats and chirps)
| Moment | Anticipation | Action | Impact / closing | 07 | Owner |
|---|---|---|---|---|---|
| **Bundle into the hold** | the rear hatch flips open 120 ms before arrival (rack or bin: the strap loosens) | the bundle drops in (existing spiral) | the hatch thunks shut on E; the window strip gains a crate; the pickup notification slides in; rumble 0.05 | — | rover (hatch, strip), art, audio, ui |
| **Cargo full** | the crate mark on the hold ring, the dimmer halo | the bundle flies and bumps the closed hatch | a soft "tok", it settles as a glowing pickup with a slow cyan pulse | **Worried** (cooldown per §7.2) | gameplay, rover, audio, ui |
| **Pickup taken later** | the pickup brightens when 07 is within 3 m and has room | it lifts and spirals in | as "into the hold" | Affirm if it was waiting > 5 min | gameplay, rover |
| **Tilly fetches** | Tilly chirps and lifts off from her perch | she hooks the bundle and flies it home at a calm 4 m/s, the bundle swinging a little | she sets it on the shelves with a soft "tok" and a rising tick; the pencil crate leaves the chart | 07 looks up as she passes, the first time each day | gameplay (fetch), art (hook), audio |
| **Crew crate opened** (T4) | the crate's seal glows like a seam | one cut (the §3 cut, short) | the lid lifts; the stack goes to the hold like a bundle; the packing note shows as a small card in the crew member's hand | Curious the first time | gameplay, art, ui, story |
| **Unload at the dock** | the dock locks (§7.1); 07's hatch opens | bundles ride a short belt out of 07 onto the shelves, one every 150 ms, each with a rising tick; each crate lands visibly in its rack | the last crate lands on D with a soft chord; the hatch closes; total 1.5–3 s inside the charge | Happy at full charge (existing §7.1), never two chirps | gameplay (sequence), art (belt, shelves), audio, rover |
| **Home full** | the belt slows on the last crate that fits | the rest stays in 07 | the free shelf socket's pad lights; the line once | Worried | gameplay, ui, world (pad light) |
| **Shelf built** | the socket's crate lamps light when materials are in hand | feed, unfold (FeelKind.Build, the tower's §8.2 pattern, shorter: ~4 s) | the rack lamp lights; the slots appear on the cargo screen | **Proud** | gameplay, art, audio, rover |
| **Cargo kit fitted** (Side Bins, Pod) | the bay's install beats (feel-pass §2.10) | | the new bins open once to show they're empty | Proud | rover, art |
| **Cargo screen** | Tab: blur 200 ms | the grids slide in 180 ms | slots fill with a soft tick each when opened after a trip (counting what came home), at most 1 s | — | ui, audio |

## 6. Save, migration and contracts
**Core (landed first by the Director):**
- `ICargoHold`: `SlotCount`, `UsedSlots`, `GetSlot(index)` → (`SalvageMaterial`, units), `FreeUnits(material)`,
  `IsFull`. Allocation-free.
- `IHomeStorage`: the same shape, plus `ShelfCount`, `GetShelf(index)` → (tier, slot range).
- `IMaterialStock` **stays** as the combined view (home + 07), so the UI's recipe code keeps working. `CanAfford` and
  `Spend` spend home first. `MaterialsChanged` keeps its meaning (combined totals).
- **Events:**
  - `CargoChanged(usedSlots, slotCount)`;
  - `CargoOverflowed(material, unitsLeft, position)`;
  - `CargoPickupTaken(material, units)`;
  - `CargoUnloaded(unitsMoved, unitsKept, homeFull)`;
  - `HomeStorageChanged(usedSlots, slotCount)`;
  - `ShelfBuilt(socketIndex, tier)`;
  - `BundleFetched(material, units)` (Tilly, T3);
  - `CrewCrateOpened(siteId, crewId)` (T4).
- `FeelKind.Cargo`, with phases as in §5. Unload uses `FeelKind.Cargo` with `Act` per crate and `Reward` at the end.
  Shelf building uses `FeelKind.Build`.
- `place.shelves` (name and job: "Supply shelves: home keeps what 07 can't carry.").

**Save:**
- Gameplay's stock section becomes two sections: `cargo` (slots) and `home_storage` (shelves built, slots).
  Overflow pickups are saved with their positions.
- **Migration from 0.5.x saves** (single totals):
  - **all units go into home storage;**
  - 07's hold starts empty;
  - the built-in shelves have power if tower L1 is reached;
  - if the old total exceeds home's capacity, the excess is kept as **"overflow crates"** stacked beside the
    shelves: counted for recipes, shown at home, and leaving no room until spent. Never deleted.
- No content floor is needed.

## 7. Task split (next week, after the quota resets on 18 Oct)
| Session | Tasks | Size |
|---|---|---|
| **Director** | Core contracts (§6), `FeelKind.Cargo`, the shelves place key; review | S |
| **gameplay** | `CargoHold` and `HomeStorage` (plain C#, EditMode tests: packing, partial fit, overflow, spend order, migration); overflow pickups (spawn, wait, listening answers, take); dock unload sequence; shelf sockets and building; T4 crew crates (5, one per site, a note each); Tilly's fetch (reach-bound, one bundle at a time, ~40 s); the Side Bins kit data and its blueprint at the crashed lander's heart; exact economy tests; save sections | **L** |
| **ui** | the cargo screen (two grids, Moonlight Glass, Tab / View, no pause); the "Cargo full" and "Home storage is full" lines (en + vi, plain); recipe cards "nhà / 07"; the crate mark on the hold ring | M |
| **art** | 07's rear hatch with the window strip, the rack's bundle straps, the Side Bins (panniers, the silhouette budget's flank-low slot), the bundle pickup look; the belly-ring open shelves (2 built in, 2 sockets, crates in material colours, rack lamps), the crew crates, the unload belt; shelf tiers and the crane later (0.6) | M |
| **rover** | the suspension settle and the glance back (T1); hatch open/close timing, the visible stacks driven by `ICargoHold`, chirps (Worried on overflow, Proud on shelves and kits), rumble ticks | S |
| **audio** | hatch thunk, the "tok" bump, the belt's rising ticks and closing chord, the shelf unfold | S |
| **world** | the waiting bundles' pencil crates on the survey chart; the free socket's pad light, shelf lamps at night | S |

**Order:** the Core contracts → gameplay's model and save (tests first) → ui, art, rover and audio in parallel →
the Playthrough adds the full-cargo beat at the Kestrel, Tilly fetching it, a crew crate and a shelf build. The
story session writes the five packing notes.

## 8. Acceptance
- No HUD stock beside the battery. The cargo screen opens and closes in ≤ 200 ms, without pausing.
- One site always fits in one trip at every stage. The first overflow never happens before minute 45.
- A bundle that doesn't fit waits beside 07, answers listening, and is taken later. Nothing is ever lost, including
  in migration.
- Unloading at the dock needs no input and never makes the charge longer than it was.
- Every cargo upgrade (Cradle straps, Side Bins, Pod) and every shelf is visible: on 07 from the chase camera, and
  at home at 30 m (ruling 11).
- Recipes at home spend home first, then 07. Field building (0.6) uses 07 only.
- Zero per-frame GC. The cargo screen allocates nothing while open.
