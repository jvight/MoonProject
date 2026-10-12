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
  it. Radio-hop never moves it.
- Never a refusal sound, never a red colour, never a timer.

### 2.4 How the player sees it (no HUD stock)
- **No permanent counts** (the owner: nothing beside the battery). The pickup notifications (UI, in flight) say what
  arrived ("+3 Kim loại").
- **On 07's body (ruling 11):** every filled slot shows.
  - The body hold's rear hatch has a small window strip that fills with stacked crates.
  - The rack's straps hold bundles.
  - The side bins' lids sit on visible stacks.
  - So "how full" reads from the chase camera.
- **The cargo screen (Tab / View button),** in the Moonlight Glass style:
  - **Left:** 07's grid, slots drawn as rounded cells with the material's icon and its units (e.g. "4"). Empty
    slots are faint outlines. The kits that add slots are labelled at the grid's edge ("Side Bins: +4").
  - **Right:** home storage, the same grid per locker (§3). This is where the player learns "home keeps the rest".
  - **Bottom line,** plain: "07: 6 of 8 slots · Home: 11 of 24 slots".
  - Tab again or B closes it. **The game does not pause** (the radio keeps playing, 07 stays still). It opens over a
    blur in 200 ms (polish-pass §1.2).

## 3. Home storage
### 3.1 What it is
- **The crew's supply lockers,** under the lander's belly ring, beside the lift: built for wheels (ruling 12). They
  are rover-height bays with roll-up mesh doors, through which the crates show.
- **Start:** 2 lockers × 12 slots = **24 slots (96 units)**. They come with the base, restored with power at tower
  L1 (they open when the lift wakes).
- **Building more:** each new locker goes on a **locker socket** around the belly ring. Sockets are predefined spots,
  never free placement (M3-22's rule).
  - 07 parks at a socket, the beam feeds Metal (and Wiring), and 07 holds E to repair.
  - The locker unfolds from its crate by itself and its door lamp lights.

| Locker | Cost | Slots | Visible at home |
|---|---|---|---|
| 1–2 (built in) | — | 12 + 12 | two mesh doors under the belly |
| 3 | Metal 3 | +12 | a third door on the ring |
| 4 | Metal 4, Wiring 1 | +12 | a fourth door; the ring now half full |
| **Locker racks** (upgrade, 0.6) | Alloy 2 per locker | ×1.5 per locker | each locker grows a second tier on top, taller at home |
| **Supply crane** (0.6, a home pad, M3-22) | blueprint + Alloy + T1 | — | a small crane arm over the lockers; unloading is quicker and the stacks it lifts are visible |

### 3.2 Unloading at the dock: automatic
- **When:** every time 07 locks into the dock (feel-pass §7.1), during the charge. **No press.**
- **Why automatic:** the dock is already the end of every trip. A press per trip would be a chore repeated 30+ times
  for no choice. The choice in our game is *where to go*, not *what to unpack*.
- **What moves:** every material slot of 07 goes into home, up to home's free space. If home is full, the rest stays
  in 07, and once the line **"Home storage is full. Build another locker."** / **"Kho ở nhà đã đầy. Hãy dựng thêm một
  tủ chứa."** shows, with the free socket's pad lighting.
- **Nothing is ever lost** (ruling 1). A full home only means 07 leaves with less room.

### 3.3 Crafting draws from both
- Every recipe (tower levels, kits, masts, lockers) reads **home + 07** and spends **home first, then 07**.
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
- **Home storage is a slow, visible build:** lockers appear on the belly ring as the base fills up. It's a second
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

**Needs in 0.5:** tower 6/9/8, kits 13/4/7, masts 13/5/0, lockers 3 and 4 at 7/1/0, so **≈ 72 units**. Ruling 5
asks for about 2× (≈ 145). **Proposal: raise yields ~25%** with the lockers (≈ 120 units, 1.65×). Today's 0.5 is
already below 2× (P9's target was never fully reached).

**Pressure points (8-slot start):**
| When | What happens | Felt as |
|---|---|---|
| min 3–7, depot | 4 of 8 slots | nothing (correct: first trip) |
| min 17–27, garage | 5–6 of 8 slots | the hatch window looks full; no overflow |
| **~min 60–75**, with Cell Pack II, drill + Kestrel in one trip (~36 units, ~10 slots) | **the first "Cargo full"**: a bundle waits at the Kestrel | **"I need more space"**: the Side Bins sheet is the next find (the crashed lander's heart in the canyon) |
| ~min 80–100, home | the stock at home passes ~80 units if the player hoards | "Home storage is full" → locker 3 (Metal 3) |
| 0.6 | longer trips and Alloy's double slots | the Cargo Pod, locker racks, the crane |

Rules for tuning:
- the first full hold never happens before minute 45 (the first-hour onboarding stays clear);
- one site always fits one trip at every stage;
- home's built-in lockers never fill in the first hour.

## 5. Feel (feel-pass beats and chirps)
| Moment | Anticipation | Action | Impact / closing | 07 | Owner |
|---|---|---|---|---|---|
| **Bundle into the hold** | the rear hatch flips open 120 ms before arrival (rack or bin: the strap loosens) | the bundle drops in (existing spiral) | the hatch thunks shut on E; the window strip gains a crate; the pickup notification slides in; rumble 0.05 | — | rover (hatch, strip), art, audio, ui |
| **Cargo full** | the crate mark on the hold ring, the dimmer halo | the bundle flies and bumps the closed hatch | a soft "tok", it settles as a glowing pickup with a slow cyan pulse | **Worried** (cooldown per §7.2) | gameplay, rover, audio, ui |
| **Pickup taken later** | the pickup brightens when 07 is within 3 m and has room | it lifts and spirals in | as "into the hold" | Affirm if it was waiting > 5 min | gameplay, rover |
| **Unload at the dock** | the dock locks (§7.1); 07's hatch opens | bundles ride a short belt out of 07 into the lockers, one every 150 ms, each with a rising tick; locker door lamps light per slot filled | the last crate lands on D with a soft chord; the hatch closes; total 1.5–3 s inside the charge | Happy at full charge (existing §7.1), never two chirps | gameplay (sequence), art (belt, doors), audio, rover |
| **Home full** | the belt slows on the last crate that fits | the rest stays in 07 | the free locker socket's pad lights; the line once | Worried | gameplay, ui, world (pad light) |
| **Locker built** | the socket's crate lamps light when materials are in hand | feed, unfold (FeelKind.Build, the tower's §8.2 pattern, shorter: ~4 s) | the door lamp lights; the slots appear on the cargo screen | **Proud** | gameplay, art, audio, rover |
| **Cargo kit fitted** (Side Bins, Pod) | the bay's install beats (feel-pass §2.10) | | the new bins open once to show they're empty | Proud | rover, art |
| **Cargo screen** | Tab: blur 200 ms | the grids slide in 180 ms | slots fill with a soft tick each when opened after a trip (counting what came home), at most 1 s | — | ui, audio |

## 6. Save, migration and contracts
**Core (landed first by the Director):**
- `ICargoHold`: `SlotCount`, `UsedSlots`, `GetSlot(index)` → (`SalvageMaterial`, units), `FreeUnits(material)`,
  `IsFull`. Allocation-free.
- `IHomeStorage`: the same shape, plus `LockerCount`, `GetLocker(index)` → (tier, slot range).
- `IMaterialStock` **stays** as the combined view (home + 07), so the UI's recipe code keeps working. `CanAfford` and
  `Spend` spend home first. `MaterialsChanged` keeps its meaning (combined totals).
- **Events:**
  - `CargoChanged(usedSlots, slotCount)`;
  - `CargoOverflowed(material, unitsLeft, position)`;
  - `CargoPickupTaken(material, units)`;
  - `CargoUnloaded(unitsMoved, unitsKept, homeFull)`;
  - `HomeStorageChanged(usedSlots, slotCount)`;
  - `LockerBuilt(socketIndex, tier)`.
- `FeelKind.Cargo`, with phases as in §5. Unload uses `FeelKind.Cargo` with `Act` per crate and `Reward` at the end.
  Locker building uses `FeelKind.Build`.
- `place.lockers` (name and job: "Supply lockers: home keeps what 07 can't carry.").

**Save:**
- Gameplay's stock section becomes two sections: `cargo` (slots) and `home_storage` (lockers built, slots).
  Overflow pickups are saved with their positions.
- **Migration from 0.5.x saves** (single totals):
  - **all units go into home storage;**
  - 07's hold starts empty;
  - the built-in lockers are open if tower L1 is reached;
  - if the old total exceeds home's capacity, the excess is kept as **"overflow crates"** stacked beside the
    lockers: counted for recipes, shown at home, and leaving no room until spent. Never deleted.
- No content floor is needed.

## 7. Task split (next week, after the quota resets on 18 Oct)
| Session | Tasks | Size |
|---|---|---|
| **Director** | Core contracts (§6), `FeelKind.Cargo`, the locker place key; review | S |
| **gameplay** | `CargoHold` and `HomeStorage` (plain C#, EditMode tests: packing, partial fit, overflow, spend order, migration); overflow pickups (spawn, wait, listening answers, take); dock unload sequence; locker sockets and building; the Side Bins kit data and its blueprint at the crashed lander's heart; yields +25% and exact economy tests; save sections | **L** |
| **ui** | the cargo screen (two grids, Moonlight Glass, Tab / View, no pause); the "Cargo full" and "Home storage is full" lines (en + vi, plain); recipe cards "nhà / 07"; the crate mark on the hold ring | M |
| **art** | 07's rear hatch with the window strip, the rack's bundle straps, the Side Bins (panniers, the silhouette budget's flank-low slot), the bundle pickup look; the belly-ring lockers (2 built in, 2 sockets, door lamps), the unload belt; locker racks and the crane later (0.6) | M |
| **rover** | hatch open/close timing, the visible stacks driven by `ICargoHold`, chirps (Worried on overflow, Proud on lockers and kits), rumble ticks | S |
| **audio** | hatch thunk, the "tok" bump, the belt's rising ticks and closing chord, the locker unfold | S |
| **world** | the free socket's pad light, locker door lights at night | S |

**Order:** the Core contracts → gameplay's model and save (tests first) → ui, art, rover and audio in parallel →
the Playthrough adds the full-cargo beat at the Kestrel and a locker build.

## 8. Acceptance
- No HUD stock beside the battery. The cargo screen opens and closes in ≤ 200 ms, without pausing.
- One site always fits in one trip at every stage. The first overflow never happens before minute 45.
- A bundle that doesn't fit waits beside 07, answers listening, and is taken later. Nothing is ever lost, including
  in migration.
- Unloading at the dock needs no input and never makes the charge longer than it was.
- Every cargo upgrade (Cradle straps, Side Bins, Pod) and every locker is visible: on 07 from the chase camera, and
  at home at 30 m (ruling 11).
- Recipes at home spend home first, then 07. Field building (0.6) uses 07 only.
- Zero per-frame GC. The cargo screen allocates nothing while open.
