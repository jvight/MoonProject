# Lofi Lunar: Game Design v3 (draft)

> Author: the story & flow session, for the owner's review. The game design session reviews it next, then the owner
> approves. It is a draft: VISION, DESIGN, STORY, ROADMAP and the feature specs are unchanged until it is approved
> (§10 lists the text that would change). The Vietnamese review edition is `docs/design/gdd-v3-vi.md`.
>
> **Two owner decisions (2026-10-11) underlie everything here:**
> 1. **Progression first.** "The focus should be development: upgrading the robot, unlocking and crafting new
>    features, more technology, going farther." The crew's memories are seasoning.
> 2. **The battery is fuel.** "07 must go back to charge to keep moving, and upgrades gradually let it go farther and
>    explore more." The Director's calm rules apply: no death or loss, a low-power crawl to the nearest charger, the
>    dock and lit relays as chargers, a diegetic gauge, gentle early warnings.
>
> It replaces `docs/design/progression.md` (folded in here) and the session tables of `docs/design/game-flow.md`. The
> opening storyboard in `docs/design/cutscenes.md` stays.

## 1. The promise and the pillars
**The promise.** *A small, tired rover wakes on a dark moon, rebuilds itself and its station piece by piece, and
pushes its battery's reach a little farther every night until it can call Earth, and you never once feel stressed.*

**Pillars (revised).** Growth comes first; the calm is kept.
1. **Grow, and go farther.** Every trip leaves 07 or home more capable, and every upgrade opens a place, a tool or
   more range. The moon opens step by step, and you can see how far you've come: on 07, at home and on the chart.
2. **Weightless calm.** Nothing snaps, nothing punishes. The battery is range, never a threat: an empty battery means
   a slow, quiet crawl home, never loss and never failure.
3. **Warmth in the cold.** Cool world, warm meaning. Chargers, home and friends are the warm lights.
4. **Every touch sings.** Every action has a sound in D major pentatonic, even a cell filling up.
5. **Chunky, readable, handmade.** Every kit piece reads at 30 m. 07's single gentle lens never changes.
6. **Alone, and at peace.** The owner's bar for look and sound: small 07, a vast moon, a beautiful quiet.

## 2. The core loop at each scale
| Scale | Loop | Battery's role |
|---|---|---|
| **Seconds** (10–60 s) | notice a light → ping → it answers → drive → the beam works (cut, dig, tow, stitch) → the melody climbs → a find | every beam second and every leap draws a little; the gauge on 07's flank answers with a lamp dimming |
| **A trip** (5–15 min) | look at the range (the gauge, the chargers on the chart) → drive out → salvage, plus the site's **heart** (a blueprint or a component) → watch the charge, top up at a lit mast → home: the **lab** decodes, the **bay** builds and fits → the dock's evening: a full charge and the next light on the horizon | the trip's shape: how far, which mast to top up at, when to turn back |
| **A session** (20–40 min) | 2–3 trips and at least one **capability jump**: a new kit, a bigger cell, a mast that extends range, a tower level | ends on the dock: cells full, the next light named |
| **A version** (1–2 h) | see a region → need an ability and range → find its blueprints, build them → enter → light its mast (a new charger) → its tier of materials → see the next region | range grows by cells, efficiency and new masts |
| **The arc** (5–6 h) | 07's recovery protocol: **Power → Network → Reach → Uplink → Contact** (§8) | the last trip, up the Peak road, is the longest the battery ever makes |
| **After** | free roam: Ro's letters, moon drift, the last finds, photo mode, paint | the whole network charges; range no longer limits anything |

**The rule of the find.** Every trip's heart is something the player **brings home**, visibly: a tube, a component on
07's back, a relic in the rack, a friend alongside. A find goes up the belly lift into the lab; materials go to the bay.

## 3. Battery and range
### 3.1 The rules (the calm)
1. **No death, no loss, no failure screen.** Whatever 07 carries stays with it. Nothing breaks, resets or falls off.
2. **Never stranded.** At empty, 07 drops into a **low-power crawl**: slow (~35% of cruise), quiet, the lamp at a
   glow, the beam, leap and boost asleep, the sonar still on. The crawl never runs out: Kenji's sealed reserve cell
   keeps 07 moving, the same cell that kept it alive for decades. The nearest charger's pillar stands on the horizon
   until 07 reaches it.
3. **Tilly comes for you** (from 0.5, once Tilly is home). While crawling, call her and she flies out, clips a tow
   line on and pulls, lifting the crawl to ~70% speed. A friend's rescue, never a penalty.
4. **Only time is spent,** and never much. Inside the network, chargers are ≤ 250 m apart, so a crawl takes under
   ~2 min (≈ 2.3 m/s). At the frontier, before a region's mast is lit, the 30% warning comes early enough to turn
   back, and Tilly's tow halves any crawl.
5. **Standing still never drains.** Stargazing, the wide shot and pauses cost nothing (pillar 6). Range is measured
   in driving and work, not in minutes, so it is not a timer.
6. **Warnings are early and gentle.** No red, no alarm, no flashing.

### 3.2 What draws charge (tuning targets for a `BatteryTuning` asset)
| Use | Cost | Note |
|---|---|---|
| Driving at cruise, flat | 1 cell ≈ 150 m | uphill ×1.5, downhill ×0.5 |
| The beam (cut, dig, tow, stitch, feed) | 1 cell ≈ 40 s of work | one site's salvage ≈ 0.5–1 cell |
| Hover-Jump | full leap ≈ ¼ cell, tap hop ≈ ¹⁄₂₀ | the drums pay first (§3.6) |
| Boost | drive cost ×2 while boosting | the drums pay first |
| Lights | +20% drive cost while the Warm Headlamp or Floodlights are on in the dark | 07 switches them by itself |
| Heavy tools (Thermal Cutter, Anchor Line reel) | 1 cell ≈ 20 s | late-game work is power-hungry, so the power branch matters |
| Sonar ping | ~free | never discourage pinging (ruling 3) |
| Standing still | 0 | in light, with the solar wing, it **gains** charge (§3.6) |

### 3.3 Chargers
- **The dock at home:** a full charge in ~4 s, inside the dock's evening. Each cell fills with a rising note
  (D E F# A B …). Docking also keeps the day (saves).
- **Every lit relay mast** has a charge pad at its foot. Park on it and the cells fill in ~3 s while the mast's lamp
  pours down. **Lighting the network is extending range.** That ties "going farther" to the station.
- **Radio-hop** between lit masts is free and lands 07 on a pad.
- The **survey chart** in the lab shows each charger's circle, so the player can plan a trip at a glance.

### 3.4 How 07 shows it (diegetic first)
- **The cell gauge** is a row of small amber lamps along 07's flank stripe, one lamp per cell. It is readable from
  the chase camera and never sits on the rear as a pair (P3). Bigger batteries add lamps: progress you can count.
- **Body language:** below 30% the eye dims a little and the solar wing droops (07 is tired). A charge perks it up:
  head lift, brighter eye, an antenna wiggle.
- **The HUD:** a thin segmented arc around the compass, one segment per cell. It reveals itself the first time 07
  drops below 70% (M3-16's HUD reveals itself verb by verb) and fades while the cells are full.

### 3.5 Warnings
| Charge | What happens |
|---|---|
| 50% | the gauge shows half; nothing else |
| 30% | one soft falling two-note (A → F#); the eye dims a little; the nearest charger's pillar rises for 20 s; the next line, once: "Charge is getting low. The mound mast can top you up." |
| 15% | 07 glances toward the nearest charger |
| 0% | a soft power-down sigh; the crawl begins; the pillar stays; the radio thins |

### 3.6 Power upgrades (all visible, ruling 11)
| Upgrade | Ver | Needs | Does | Visible on 07 |
|---|---|---|---|---|
| **Solar cell repaired** (Tilly's gift ✔) | 0.5 | Tilly home | standing still in light trickles charge: 1 cell per ~3 min, a cozy option that is never needed | the wing's missing cell is back |
| **Capacitor drums** (Boost Coils ✔) | 0.5 | blueprint + Wiring | boosts and leaps draw from the drums first; downhill rolls and chargers refill them | twin drums on the flanks, glowing while charged |
| **Cell Pack II** | 0.5 | blueprint (relay.2's cabinet) + Metal + Optics | 4 → 6 cells | a second cell module on the back; two more gauge lamps |
| **Efficient Hubs** | 0.6 | blueprint + Alloy + Wiring | −25% driving cost | copper-coiled hubs on the new tread wheels |
| **Cell Pack III** | 0.6 | blueprint (Bell's signal) + Alloy + Optics | 6 → 8 cells | wider cell modules; gauge lamps 7–8 |
| **Twin Solar Wing** | 0.7 | blueprint + Crystal + Optics | double trickle; recovers abilities faster | a second wing: 07's single tired wing becomes a pair |
| **Range-Extender Pack** | 0.7 | blueprint (a sealed crater wreck) + Circuitry + Alloy | +3 cells in a pack | a cream battery box with a small lamp, under the rear rack |
| **Power Core** | 0.8 | Superconductor + Power Cell (Kenji's last sheet, already on the board) | +3 cells; every ability at full strength | a round glass core window on the flank, glowing amber |

### 3.7 Range by version (targets: a comfortable trip radius from the last charger, with a site's work)
| Version | Cells | Radius | What it covers | Where the network stands |
|---|---|---|---|---|
| 0.5 start | 4 | ~150 m | the inner basin: the depot, Tilly's crater, the garage | the dock only |
| 0.5 + masts and Cell Pack II | 6 | ~230 m from any lit mast | the outer basin and the canyon | 4 basin masts |
| 0.6 | 8, −25% drive | ~380 m | the Rim Terraces | + terrace mast(s) |
| 0.7 | 11 + trickle | ~500 m | the Shadowed Crater | + crater mast(s) |
| 0.8 | 14, full power | ~650 m | the Peak road, the longest trip | + the Peak's foot mast |

### 3.8 "I need more range" moments in 0.5 (each points at the next upgrade)
1. **Tilly's parts** (~100–130 m out): the gauge first drops to half, the arc appears, and the next line points home.
   *Lesson: the dock charges.*
2. **The chasm** (~280 m): 07 arrives on its last cell and the dark mast at the canyon mouth (relay.2) is right
   there. Lighting it gives a charger. *Lesson: masts are chargers.* Its service cabinet holds the **Cell Pack II**
   blueprint, so the mast gives range twice.
3. **The canyon** (the 616 m exit route): the terminus mast (relay.3) charges inside, and radio-hop brings 07 home.
4. **The outer basin** (Kestrel, the drill, the shoulder mast): Cell Pack II and the remaining masts.

## 4. The tech tree, crafting and materials
### 4.1 How an unlock happens
1. **Find:** a blueprint or a component in a site's heart, carried home on 07.
2. **Decode:** in the **lab** (inside the lander, up the belly lift), the beam sets the blueprint in the **reader**. It
   decodes in ~4 s (a moment, never a timer), and a node lights on the **tech board**. A cable pulse runs to the bay.
3. **Build:** the bay's **plotter** draws the sheet. 07 parks on the turntable and feeds the hopper (materials and the
   component), and the arms fit the kit (the install moment).
4. **Show:** one visible change on 07 or at home, readable at 30 m.

Station nodes (tower levels, masts, home machines) are fed at their own ports and need a component, not a blueprint
(P1's rule).

### 4.2 Materials, components, blueprints
| Kind | What | Where | From |
|---|---|---|---|
| **T1 scrap** | Metal, Wiring, Optics ✔ | every basin wreck, with signatures per site (P9) | 0.5 |
| **T2 refined** | Alloy (heavy hulls), Circuitry (sealed vaults), Crystal (the crater; Moss grows it) | heavy, sealed and dark wrecks that need 0.6–0.7 tools | 0.6–0.7 |
| **T3 rare** | Superconductor | the wrecks of the Peak road and the dish | 0.8 |
| **Components** | Tower Coil, Transformer, Relay Spare ✔ (P1); Servo Pack, Gyro, Lens Array (0.6); Heat Sink (0.7); Power Cell (0.8) | the first of each is a heart find; from 0.6 the **Fabricator** makes any component already found once | 0.5–0.8 |
| **Blueprints** | one per kit | site hearts, sealed vaults, masts' service cabinets; Bell's signals point at them | 0.5–0.8 |

**Recipes grow:**
- 0.5 uses T1 only.
- 0.6 adds Alloy and a component.
- 0.7 adds Crystal and Circuitry.
- 0.8 adds Superconductor.

There are no circular needs. In 0.6, for example: Servo Pack → Bay Arms II → Heavy Tow → Alloy → the Fabricator.
Costs follow ruling 5: each version's sites hold about 2× what its nodes need. Nothing is collected twice, and nothing
respawns except moon drift's small gifts (0.6).

### 4.3 The tree: 30 nodes in 6 branches
✔ = built today.

| Branch | 0.5 | 0.6 | 0.7 | 0.8 |
|---|---|---|---|---|
| **Mobility** (where 07 can go) | Hover-Jump ✔ | Magnetic Treads | Long Leap (a steerable glide) | — |
| **The beam** (what 07 can work) | Cargo Cradle ✔ | Heavy Tow | Thermal Cutter | Anchor Line |
| **Sensors** (what 07 knows) | Warm Headlamp ✔ | Wide Sonar, Deep Scan | Floodlights | Star Tracker |
| **Power & range** | Capacitor drums ✔, Cell Pack II | Efficient Hubs, Cell Pack III | Twin Solar Wing, Range-Extender | Power Core |
| **Friends** (crafted for them) | — | Scout Module (Tilly) | Ferry Harness (Atlas) | Sister Drones |
| **Station** (home and reach) | Tower L1 ✔, L2 ✔, L3 ✔, relay network ✔ | Bay Arms II, Fabricator | Lab Reader II | the Great Dish |

By version: 0.5 has 9 (8 built + Cell Pack II), 0.6 has 9, 0.7 has 7, 0.8 has 5.

| Node | Needs | Does | Visible |
|---|---|---|---|
| Hover-Jump ✔ | blueprint (the chasm's lip) + T1 | charged leap; opens the canyon | belly coils |
| Magnetic Treads | blueprint + T1 + Gyro | climbs marked walls only (DESIGN's lane rule); opens the terraces | six big **matched** treaded wheels; the odd wheel is healed and 07 sits higher |
| Long Leap | blueprint + Alloy + Gyro | the leap becomes a glide; crosses the crater's rifts | glide vanes on the flanks |
| Cargo Cradle ✔ | blueprint (garage) + Metal | carries a find in a rack | the strapped rear rack (straps parallel, P3) |
| Heavy Tow | blueprint (a terrace wreck) + T1 | tows heavy salvage: Alloy's source | an A-frame tow mast with a spool |
| Thermal Cutter | blueprint + Alloy + Heat Sink | opens sealed wrecks: Circuitry's source | heat-sink fins glowing while cutting |
| Anchor Line | blueprint (the Peak's foot) + Superconductor + Power Cell | locks onto the crew's anchor posts and reels 07 up the Peak road | a winch drum under the nose |
| Warm Headlamp ✔ | blueprint (drill) + Optics | a wider, warmer pool | the caged lamp bar |
| Wide Sonar | blueprint + T1 + Lens Array | ping range ×2 | a bigger dish; the bent antenna straightened |
| Deep Scan | blueprint + Alloy | a ping shows a site's tier and whether its heart is a blueprint or a component | a scanner vane on the mast |
| Floodlights | blueprint + Alloy + Lens Array | lights the deep dark; opens the crater | twin floods on a roof bar |
| Star Tracker | blueprint + Superconductor | aims the great dish | a brass tracker scope |
| Power & range nodes | see §3.6 | | |
| Scout Module | blueprint (Bell's signal) + T1 + Lens Array | Tilly scouts and deep-scans a pillar ahead | an antenna and lens on Tilly |
| Ferry Harness | blueprint + Alloy + Servo Pack | Atlas carries heavy salvage home from a marked spot (never memories) | a harness on Atlas |
| Sister Drones | Circuitry + Crystal + Gyro (Tilly's own schematic) | two drones sweep loose T1 inside the network | two drones on perches at home |
| Tower L1 ✔ / L2 ✔ / L3 ✔ | Coil / Transformer / Relay Spare + T1 | power to home and the lab / the bay / the lab's survey chart | windows and the radio / the bay wakes / the chart's lamp |
| Relay network ✔ | per mast: T1 (escalating) + its part | **a charger**, reach, radio-hop, letters | a chain of warm masts |
| Bay Arms II | T1 + Servo Pack | fits tier-2 kits | a second gantry |
| Fabricator | Alloy + T1 | makes components already found once | a new machine beside the bay |
| Lab Reader II | Circuitry + Lens Array | decodes sealed data cores (0.7–0.8 blueprints) | a second screen; a new roof antenna |
| The Great Dish | Superconductor + Power Cell + Atlas's lift + the network linked + the Star Tracker | the uplink: the ending | the Peak's dish turned to Earth |

### 4.4 07's silhouette, from a tired probe to an expedition rover
| Version | 07 looks |
|---|---|
| start | half-lidded, one tired wing missing a cell, a bent antenna, one odd wheel, faded paint, four gauge lamps |
| 0.5 | belly coils, the lamp bar, flank drums, the rear rack, a second cell module (six lamps), Tilly's cell in the wing, Bell's fresh "07" |
| 0.6 | taller on six matched treads with copper hubs; a big dish and a straight antenna; a tow mast; eight lamps |
| 0.7 | **two wings**; floodlights; glide vanes; heat-sink fins; the range pack |
| 0.8 | a winch drum, a glowing core window, a tracker scope: a capable expedition rover |

Rules for the arc:
- one design language: cream enamel, the worn orange stripe, brass fittings;
- never spiky or militarised; the rear never reads as a face (P3); the lens never changes;
- silhouette budget: by 0.8, 07's bounds grow ≤ 25% in height and ≤ 15% in width, so it stays small against the moon.

## 5. Exploration and regions
| Region | Ver | Gate (ruling 10: real, seen first, one-way exit) | Its mast(s) | Its tier |
|---|---|---|---|---|
| The basin (home) | 0.5 | — (range: Cell Pack II and masts) | mound, shoulder, canyon mouth | T1 |
| Whispering Canyon | 0.5 | Hover-Jump | the terminus | T1, Bell |
| Rim Terraces | 0.6 | Magnetic Treads | the terraces (the first to face Earth) | Alloy; Atlas |
| Shadowed Crater | 0.7 | Floodlights | the crater floor | Crystal, Circuitry; Moss |
| The Peak road | 0.8 | Anchor Line + Long Leap + Atlas (the rockfall) | the Peak's foot | Superconductor; the dish |

**Why the player goes farther:**
- **Range grows,** so the frontier keeps moving. Every version's first trip shows a light just past the old reach.
- **Farther hearts are richer:** higher tiers, rarer components, the next blueprints.
- **The network makes the far near:** every lit mast is a charger, a hop point, a clear radio and a stretch of inked
  chart. Lighting the next mast is the way forward.
- **Old ground pays again:** each version's new salvage kind (heavy, sealed, dark, high) also hides in the basin,
  seen since 0.5 (a heavy pod by the depot, Kestrel's sealed core). Going back is rewarded, never required.
- **The protocol points at the Peak:** its red light has been in view since the first frame (§8).

## 6. The base: every station serves progression
| Station | Its job for progression | Upgrades |
|---|---|---|
| **The dock** | the full charge, the day kept (save), the dock's evening (the next light) | — |
| **The radio tower** | power to home, in stages; the radio | L1–L3 (0.5) |
| **The lander: the crew's lab** (up the belly lift, M3-19) | **where finds become know-how.** The **reader** decodes blueprints. The **tech board** (Kenji's plans for 07) shows the tree and 07's recovery protocol. The **survey chart** (Ines's map) shows reach and chargers. A small **keepsake shelf** holds the crew's things (seasoning). | Lab Reader II (0.7) |
| **Kenji's Rover Bay** | builds and fits kits; its plotter draws decoded sheets | Bay Arms II (0.6) |
| **The Fabricator** (0.6) | makes components already found once, so progress never stalls on a missing part | — |
| **Relay masts** | **chargers**, reach, radio-hop, the letters they caught | one per region |
| **The biodome** (0.7, Moss) | grows Crystal, the one renewable material | — |
| **Bell's corner** | the radio dial; Bell's signals point at blueprints and components | — |
| **The great dish** (0.8, the Peak) | the uplink | — |

**The lander's purpose, in one line:** *the crew's lab, where 07 turns what it finds into what it becomes.*
- M3-19's geometry stays: the belly lift, the round platform as a turntable, the ring-rail camera. The five crew
  corners become four lab stations.
- Every trip home with a find goes up the lift. Seen from outside, the lab is the warm big porthole: the board's glow
  and the chart.
- The base grows outward as machines (the Fabricator, the biodome, Bay Arms II), friends' spots, masts and the
  network's glow. It never grows by placing modules; decorations on snap points stay (1.0, DESIGN "Base life").

## 7. Friends in progression
| Friend | Found | Helps (progression) | Rescue | Gift on 07 | Crafted upgrade |
|---|---|---|---|---|---|
| **Tilly** (drone) | 0.5, basin | spots glints and sites near 07 | **tows 07 when it crawls** | the wing's cell: the solar trickle | Scout Module (0.6): scouts and deep-scans a pillar ahead |
| **Bell** (radio cabinet) | 0.5, canyon | her signals lead to blueprints and components; the dial | — | fresh "07" paint and a pennant | — |
| **Atlas** (hauler) | 0.6, terraces | lifts rockfalls; lifts the dish (0.8) | — | a tow hook | Ferry Harness (0.7): carries heavy salvage home, saving trips |
| **Moss** (garden bot) | 0.7, crater | the biodome grows Crystal | — | a flower painted on the patch | — |
| **Sister drones** | 0.8, crafted | sweep loose T1 inside the network: late-game chores go away | — | — | — |

Friends are found (story beats) and then help in ways that save time and battery. That is automation as warmth, never
a menu.

## 8. The story, reframed: 07's directive
**The goal.** When the crew evacuated in the storm, they left 07 two things. One is the words "Keep the radio on."
The other is a protocol in its memory: **the Lumen Recovery Protocol.** If Earth's signal is ever heard, restore the
station and answer.

Decades later, a faint signal from Earth (Ro's station jingle in the static) wakes 07, and the protocol starts:
1. **Power:** wake the tower; home, the bay and the lab come back. (0.5)
2. **Network:** relight the relay chain across the moon. Every mast is reach, a charger and a voice from the past.
   (0.5–0.8)
3. **Reach:** become able to climb to the Peak. The protocol assumed a crew, and 07 is alone, so 07 must become more
   than it was built to be, using Kenji's unfinished plans on the lab's tech board. (0.5–0.8)
4. **Uplink:** rebuild the great dish on the Peak. (0.8)
5. **Contact:** answer Earth. (the ending)

**How it shows:**
- The tech board's header in the lab is the protocol's status panel (five stages, lamps), a clear goal for the whole
  game, visible at home.
- The opening's electronics flicker (cutscenes O5) can carry a two-second boot readout through 07's lens: "EARTH
  SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN RECOVERY" (§11, question 2).

**The crew's fate, discovered along the way (seasoning):**
- **The masts' letters:** each lit mast plays back the messages it caught. The crew landed safely; a return was
  studied, then cancelled; the program closed. Then Ro's own letters: she kept writing for twenty years.
- **Logs and memories:** found in hearts beside the tech finds. They sit on the lab's keepsake shelf and on the
  bunk-side clips.
- **Kenji's logs** frame the growth: he fixed 07 seven times, hid a spare wheel for "next time", and his last line is
  "Don't let anyone retire you." 07's upgrades are Kenji's "next time", kept.

**The ending.**
- 07 climbs the Peak with everything it has become, Atlas lifts the dish, and the Star Tracker aims it.
- The uplink opens, and Bell sends Side B of Ro's mixtape up the network.
- After a silence, a light on Earth's night side blinks the station's call sign, three short, one long, and 07's lens
  opens fully for the first time.
- "I heard you, 07. I'm still here. — Ro." Credits.
- After the credits, the protocol's panel reads complete, Ro's letters keep coming, and the moon stays open.

## 9. Sessions and pacing
**Pacing rules:**
- ≤ 3 min without something new in view;
- a capability jump every session;
- a new verb every version;
- no grind (2× materials);
- no wasted trip: warnings come early, chargers ≤ 250 m apart, and the crawl always gets home.

### 0.5 "Lumen wakes" (~100 min): Power, and the first of the Network
| # | Min | What's new | Range | 07 changes | Home changes | The next light |
|---|---|---|---|---|---|---|
| S1 | 0–15 | the opening (07's eye, the protocol boots); the depot; the coil; tower L1 | 4 cells, ~150 m | its eye opens; four gauge lamps light as it wakes | windows, the belly lamp, the radio clears; the lab wakes | Tilly's chirp |
| S2 | 15–30 | Tilly; her parts, at the first "I need more range" (the dock charges); Tilly's gift | trickle in light | the wing's cell | Tilly's perch | a warm light across the chasm |
| S3 | 30–45 | the chasm: the Hover-Jump blueprint at the lip; **relay.2 lit: masts are chargers**; Cell Pack II's blueprint; first ride up the lift: the lab decodes; the garage (Transformer, Cradle tube, the mixtape); tower L2: the bay | + the canyon mouth charger | hover coils; Cell Pack II (six lamps) | the bay, the plotter, the tech board's first nodes | the chasm, now crossable |
| S4 | 45–62 | the leap; the canyon; Bell; the terminus mast; radio-hop home | the canyon covered | Bell's paint | Bell's corner, the jingle clear | Bell's signal |
| S5 | 62–75 | tower L3: the survey chart inks the reach and shows the chargers | — | the Cradle or the drums (the player's order) | the chart's lamp in the big porthole | three dark masts on the chart |
| S6 | 75–100 | Kestrel (Headlamp tube), the drill (Boost Coils tube), the mound and shoulder masts: the basin is home | the whole basin from any mast | the lamp bar, the drums | four masts lit; the protocol's Power stage complete | the terrace wall's foot: a pillar, and Ines's pencil note |

### 0.6 "Rim Terraces" (~75 min): Reach, step one
| # | Min | What's new | Range | 07 changes | Home changes | The next light |
|---|---|---|---|---|---|---|
| S7 | 0–35 | moon drift's first morning; new basin wrecks (Servo Pack, Gyro); Bay Arms II; Magnetic Treads; the first climb | 8 cells after Cell Pack III | six matched treads, copper hubs | Bay Arms II | Atlas on a high terrace |
| S8 | 35–75 | Heavy Tow → Alloy; the Fabricator; the terrace mast (Ro's first letter); Atlas; Wide Sonar, Deep Scan, the Scout Module | the terraces covered | the dish, the straight antenna, the tow mast | the Fabricator; Atlas by the bay | the crater's black bowl, a green glow inside |

### 0.7 "Shadowed Crater" (~90 min): Reach, step two
| # | Min | What's new | Range | 07 changes | Home changes | The next light |
|---|---|---|---|---|---|---|
| S9 | 0–40 | Floodlights; the crater; Moss; Crystal; Twin Solar Wing | 11 cells, a double trickle | **two wings**, floodlights | the biodome | sealed wrecks glinting |
| S10 | 40–90 | Thermal Cutter → Circuitry (and Kestrel's sealed core at home); Long Leap; Range-Extender; Lab Reader II; the Ferry Harness; the crater mast | ~500 m | vanes, fins, the range pack | Reader II's roof antenna | one dark mast short of the Peak |

### 0.8 "The Peak" (~60 min): Uplink, and Contact
| # | Min | What's new | Range | 07 changes | Home changes | The next light |
|---|---|---|---|---|---|---|
| S11 | 0–35 | the Peak's foot mast; the Anchor Line; the Power Core; the Peak road | 14 cells, full power | the winch, the core window | Sister Drones | the dish on the summit |
| S12 | 35–60 | the Star Tracker; Atlas lifts the dish; 07 stitches; the broadcast; Earth answers | — | the tracker scope | the Peak's beacon turns amber | Ro's letters |

### 1.0
No new region. Polish, the title and collections (M3-09), Settings ("Gentle camera", the next line, the battery HUD),
controller support, photo mode and paint, the after-ending letters, and the events (Eclipse, Solar Flare: a flare
fills every cell, Earthrise).

**Main arc ≈ 5.4 h; completion ≈ 9–10 h** (DESIGN's targets: 5–7 h and 10–12 h).

## 10. KEEP / CHANGE / CUT, against what is built
| Built | Verdict | How |
|---|---|---|
| The M3-16 power spine (L1 home, L2 bay, L3 chart) and steps 0–8 | **KEEP** | it is the protocol's Power stage. Steps keep their order; the battery's first lessons slot into steps 3–4 (§3.8). |
| P1 story parts | **KEEP, renamed** | they become **components** (Tower Coil, Transformer, Relay Spare): text and cards only |
| Blueprints and tubes (P7) | **CHANGE** | delivered at the lab's reader (up the lift) instead of the plotter; the plotter draws after the decode. Hover-Jump is found at the chasm's lip instead of pre-pinned. |
| Kenji's Rover Bay, the plotter on its back wall | **KEEP** | the place that builds |
| The belly lift (M3-19) | **KEEP** | its purpose becomes "up into the lab"; restored the first time 07 brings a find home (step 3–4) |
| M3-19's crew corners | **CHANGE** | four lab stations: reader, tech board, survey chart, keepsake shelf |
| The memory shelf outside | **CUT** | (as M3-19) replaced by the keepsake shelf inside |
| The crew's map (M3-07) | **KEEP, moved** | the survey chart in the lab, woken by L3; it also shows chargers' circles |
| The deck lookout | **CUT** | (as M3-19) its seen sweep moves to the dock's evening |
| The dock | **CHANGE** | its job becomes "where 07 charges and keeps the day". The dock's evening (P6) includes the full charge. |
| Relay masts (M3-06) | **CHANGE** | every lit mast gets a charge pad; the masts' letters (`text/radio-letters.md`) stay |
| Radio clarity by distance, stargazing, the wide shot | **KEEP** | standing still never drains; in light it trickles |
| Friends Tilly and Bell, their gifts | **KEEP** | Tilly also tows 07 home when it crawls; her gift becomes the solar trickle |
| Kits: Hover-Jump, Cradle, Headlamp, Boost Coils | **KEEP** | Boost Coils' drums now buffer charge |
| Wide Sonar | **CHANGE** | moves from 0.7 to 0.6 |
| The opening and cutscenes (`cutscenes.md`) | **KEEP** | O5 may add the protocol's boot readout (§11). The gauge lamps light as 07 wakes. The dock's evening shows the cells filling. |
| The P8 next line | **KEEP** | plus the one battery hint at 30% |
| The economy (P9) | **KEEP** for T1 | T2/T3 from 0.6 |
| The "home arc" framing, crew corners as the reason to play | **CUT** | memories, logs and letters stay as seasoning |
| `progression.md` | **CUT** | folded into this document |

**New for 0.5** (the battery is the core of the owner's decision, so it lands in 0.5):
- gameplay: the battery model (plain C#, EditMode-tested), chargers (dock and masts), costs from `BatteryTuning`, Cell
  Pack II, the "never stranded" crawl rules and Tilly's tow;
- rover: the crawl, the gauge lamps, the eye and wing at low charge;
- ui: the HUD arc;
- audio: the cell notes and the warning motif;
- art: the gauge lamps, the cell module, the masts' charge pads.

**Core contracts:** `IRoverBattery` (Cells, Charge01, IsLowPower), `BatteryChanged` (on cell boundaries only, zero
GC), `LowPowerChanged(bool)`, `ChargingChanged(bool, ChargerKind)`; `TechNodeId`, `ITechTree`, `TechNodeUnlocked`;
`place.lab`.

**VISION and DESIGN text that must change once approved:**
- **VISION anti-goals:** "Health, oxygen, fuel, damage, enemies, failure screens, timers." becomes "Health, oxygen,
  damage, enemies, failure screens, timers. 07's battery is its range, never a threat (ruling 15)."
- **VISION pillar 5:** "No timers, no quest log, no fail states, no punishment" stays. Add: "the battery measures
  driving and work, never minutes; an empty battery is a slow crawl home, never a failure."
- **VISION ruling 7, "Never stuck",** gains a second sentence: "Never stranded: at empty, 07 crawls slowly and quietly
  to the nearest charger (or Tilly tows it), losing nothing."
- **New VISION ruling 15, "The battery is range, never a threat":** the rules of §3.1, the chargers of §3.3 and the
  gentle warnings of §3.5.
- **VISION pillars:** "Grow, and go farther" becomes pillar 1 (§1).
- **DESIGN "Station reach":** "There is never a battery, fuel or range limit that forces 07 home. That would be a
  hidden timer and would break rulings 1 and 7." becomes "Range is 07's battery. Every lit mast is a charger, so
  lighting the network is how range grows. Outside reach the radio is static and the chart stays pencil, and 07
  turns back to charge before going on."
- **DESIGN "Why people come back":** 07 and home visibly grow (1), the moon opens with range and tools (2), friends
  (3), home (4), the crew's story as seasoning (5).
- **DESIGN regions:** the crater's gate becomes Floodlights; the Peak's becomes Anchor Line + Long Leap + Atlas.
- **M3-16 places:** the dock's job line becomes "Where 07 charges. A rest here keeps the day."
- **M3-06:** "Outside reach… nothing bad happens" becomes "outside reach 07 runs on its battery; masts charge".
- **M3-11:** Boost Coils' drums buffer charge.
- **ROADMAP:** each version names its tech tier and its range; the battery lands in 0.5; Wide Sonar moves to 0.6.

## 11. Open questions for the owner
1. **Empty battery:** a slow crawl to the nearest charger, with Tilly able to tow. Is that the right feeling, or
   should Tilly always come by herself?
2. **The opening:** may 07's lens show a two-second boot readout ("Earth signal detected · Uplink offline · Begin
   recovery") so the goal is clear from minute one, or should the opening stay wordless?
3. **The solar trickle:** resting in light slowly recharges. Keep it as a cozy alternative to driving home, or remove
   it so chargers matter more?
4. **First range:** about 150 m from home with four cells. Too tight, or right?
5. **The goal:** is "the Lumen Recovery Protocol" (Power → Network → Reach → Uplink → Contact) clear and motivating
   enough as the reason to grow?
6. **The ending:** keep Ro answering (the personal payoff), or a wider "Earth answers"?
7. **Building:** home grows through machines, friends and masts, never by placing modules, with decorations only in
   1.0. Confirm?
8. **Length:** four regions after the basin (canyon, terraces, crater, Peak) for a ~5.4 h main arc. Enough?
