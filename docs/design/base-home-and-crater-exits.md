# Base home and crater exits: research and recommendation

> **Owner's concern** (2026-10-24): the opening sits in a giant crater with apparently one exit, and we said the main
> base is built there. Isn't that too confining? And why do the four opening structures (the pad, the solar array,
> the battery bank, the mast) matter? Do we build them and leave, or is this home?
>
> **Status: final** (Director-accepted, 2026-10-24; reconciled with 0.6 = the Long Mare).
>
> **Author:** the game design session. It builds on `opening-v3-purpose.md`, `research/caravan-tow.md` ("move to the
> light", deferred), `research/genre-survival-craft.md` (C2 gates, keys and lures), `0.6-world-regions.md`, and
> `ice-and-vehicles.md`. No code.

## 0. Player experience first
| | The answer |
|---|---|
| **Hook** | The crater is a **cradle**: sheltered, warm-lit and small enough to learn. From hour two, its ways out are *visible and closed*: the rille notch to the black Mare behind relay.1, a shadowed slot in the canyon, and the Peak road's rockfall. |
| **Pull** | Each closed way out is a question: "what's past it, and what do I need?" Each one is answered by a part on 07's body (C2: gates before keys). |
| **Hold** | What you built **keeps working for you**: the crater camp becomes a waystation that charges 07 on every pass. Nothing is wasted. |
| **Finish** | The 0.6 arc ends with **the caravan arriving on the Mare's sunlit swell**: the base unfolds beside the crew's comms dish, which wakes on that power. The Peak stays the 1.0 finale. Leaving the crater is the emotional milestone, and it is chosen. |
| **Feeling** | "Outgrowing the nest": calm pride, never loss. |

## 1. Research: first home and real home, enclosed starts
| Game | The first home | The enclosed start / the exit | Leaving feels good because… | Or feels bad when… |
|---|---|---|---|---|
| **Subnautica** | Lifepod 5: a fabricator, a radio, a respawn | Open sea, but the depth is the wall | **The pod keeps a job** (radio, respawn); you *choose* where the real base goes; seabases pay back (storage, power, a view) | — (no one is forced to leave) |
| **Subnautica: Below Zero** | the drop pod, then early habitat parts | Glaciers and caves gate the land | **The Seatruck** carries modules: home partly travels | land sections felt empty; the story pulled players around |
| **Astroneer** | the starting habitat: oxygen and power | the oxygen tether range is the wall | outposts with beacons; the vehicles carry your stuff; the start stays useful | relocating means re-tethering and hauling everything by hand |
| **No Man's Sky** | the first base computer, anywhere | a ship plus planets | **many homes, linked** by teleporters; nothing is abandoned | the early, unguided start (fixed by later updates) |
| **Satisfactory** | the HUB, chosen by the player, and movable | biome edges, cliffs | you pick a better spot; the old factory keeps producing | dismantling and rebuilding is a chore unless done by choice |
| **Valheim** | the first hut near spawn | biome danger is the wall | **portals** link homes; the meadow stays a safe heart | hauling ore by cart before portals (debated, but meaningful) |
| **Zelda: Breath of the Wild** | the Great Plateau | **cliff walls on every side; the exit is earned by the glider**, and the world is visible from the start | the exit is a **tool on Link's body**, the view promised it, and the jump is a release | — (the plateau is never "home", so nothing is lost) |

**What makes leaving good:**
1. **Your investment keeps working or comes with you:** the pod's radio, portals, outposts, the Seatruck.
2. **The exit is earned by a part on the body, and was visible long before:** BotW's glider and walls.
3. **The new place is better for a reason the player can see:** light, room, resources, a view.
4. **The old place stays meaningful:** a waystation, a link, a respawn.

**What makes it feel like loss:**
- forced relocation;
- rebuild costs;
- a dead old base with no link back;
- walls with no visible way out (claustrophobia).

## 2. Facts: the crater today
These are read from `World/Surface/SurfaceSettings`, `Data/Tuning/WorldSettings.asset` and `WorldAnchors`. They are
read-only; please verify in play.

**The basin:**
- **Floor:** radius 320 m, with a ±18 m warp. The base pad is at the origin, and the floor rises gently by 6 m to the
  edge.
- **The wall:** foothills to about 375 m, the steep wall from about 350 m, and the rim crest at about 420 m. The crest
  stands 46 m high, plus mountains up to 62 m with saddles.
- **Outside:** the outer plain lies beyond, about 26 m up, out to 660 m.

**Sites, by distance from the base:**
| Site | Where | Inside the floor? |
|---|---|---|
| Kestrel-3 | 185 m at 356° | yes |
| The lander | about (35, 160) | yes |
| The ruins | about (80, 180) | yes |
| relay.1 (the west mast) | (−205, −75): 218 m at about 250° | yes |
| relay.2 and elder 06 | (265, 84): 278 m at 72°, at **Whispering Canyon's mouth** (canyon bearing 72°, mouth at 280 m) | yes |
| **The Peak** (summit and the Great Dish) | 440 m at 12°, summit 150 m high; its road foot (`site.peakroad`) sits on the floor's edge | the summit is beyond the crest |

**Real exits today: none.**
- **Whispering Canyon** is carved into the rim, and its one-way exit leads **back to the basin**: it is a loop, not a
  way out. It is probably the "one exit" the owner sees.
- **The Peak road** has its foot anchor, but `MoonSurface.IsDrivable` excludes the Peak's flanks, so it is a lure
  today, not a path.

**Conclusion:** the owner is right. The crater is closed on every side, and nothing tells the player it will open.

## 3. Decision: A + B, the move at the end of 0.6, to the Mare's sunlit swell
> **Accepted by the Director** (2026-10-24, owner-delegated): A + B, plus the three opening additions.
>
> **Reconciled with the fixed constraints:**
> - 0.6 is the Long Mare (`0.6-world-regions.md` §3.1, concept r2);
> - regions keep their adjacency order (0.7 Terraces, 0.8 Shadowed Crater and lava tube, 1.0 the Peak road).

**B, from the opening on.** Every charger 07 lights is a **waystation**: the crater pad, the lit relays, and later the
Mare's Field Charge Pylons. Home is wherever 07 can charge. The range ring is drawn from *all* of them, so the map
grows as the network grows.

**A, the move: "Đi tìm ánh sáng" (Move to the light), at the end of 0.6, to the Mare's sunlit swell.**
- **Why there (lunar logic):**
  - At a polar site the Sun circles low. **The crater floor loses it behind its own rim** for much of each lunar day:
    long nights, and a cold trap.
  - Out on the open Mare, a broad swell near the crew's comms dish has **no rim to hide the Sun**, so it gets the long,
    low light. The region doc already gives it "a low sun, mile-long shadows".
  - The Peak gets the most light of all, but it is **1.0** content, and adjacency holds.
- **Why at the end of 0.6, not at minute 60 or in 0.7:**
  - By then the player has a stake in the crater (the opening, plus a whole region's errands), so leaving is a chosen
    milestone, not a loss.
  - **The Mare's keys are exactly the caravan's enablers.** The Field Charge Pylon chain (chargers ≤ 300 m apart) and
    Cell Pack III are what make a slow, heavy haul across about 1 km of open basalt possible. The infrastructure the
    player built is what lowers the friction (Death Stranding's lesson, caravan research §1).
  - It answers the owner's "too confining" in the **next** version, not at the game's end.
- **What moves:** the moveable modules (the solar array, the bank and the mast), packed onto skids. **The crater pad
  stays** as the crater waystation, so nothing is lost.
- **The 0.6 end beat** (it replaces `opening-v3-purpose.md` §6's Peak version, so that adjacency holds):
  1. The caravan arrives on the swell, and the base unfolds beside **the crew's comms dish** (the Mare's mast).
  2. Lighting the dish sends **one fragment of the survey** (Kestrel-3's buffer).
  3. The far carrier **stops sweeping and holds on Lumen**: `CARRIER · LOCKED`.
- **1.0 callback:** the Great Dish on the Peak stays the finale. Its last act is a small **dish caravan** up the Peak
  road, carrying only the kit the dish needs: "move to the light" one last time, to the place that never goes dark.

**How the four structures' purpose reads in the world, with no text.** They are **07's life support**; the player must
*see* the dependence:
| Structure | What it does for 07 | What the world shows |
|---|---|---|
| **Pad** | 07 lives on it | the range ring is centred on it; 07 docks, the walls lock, the cells fill (exists) |
| **Solar array** | it feeds the pad | **a warm pulse runs along the cable from the panels to the pad** while the Sun is up; the pad's lamp brightens with it |
| **Battery bank** | it keeps the lights through the night | at dusk **the pulse reverses: bank → pad**, and the base lights stay on while the crater goes dark (first nightfall, exists) |
| **Mast** | 07's voice | its lamp blinks back with relay.1; the decode ticker grows on each dock (exists) |

When the caravan packs these modules, the player is taking **their life support** with them. That is why they pack,
and why it feels like moving home.

## 4. The ways out: two exits and one dark pocket
| Version | Way out | The gate seen before (readable from the start) | Key: a part on 07 | Leads to |
|---|---|---|---|---|
| **0.5 (hour two)** | **East: Whispering Canyon's dark slot**, with relay.2 and 06 at its mouth (72°, 280 m). A pocket, not a region exit: it loops back to the basin, as built. | a narrow inner section **in permanent rim shadow** (the low polar Sun never reaches it). 07's stock lamps fall short and the way ahead reads black. | **the SSR-3 headlamp** (R11, from 06's lamp) | the canyon's terminus and the **Canyon Supply Lander's bay**, holding the **Field Charge Pylon** flat-pack and fragments: the first key to the Mare |
| **0.6** | **West: the rille pass** (255°, 420–520 m), **right behind relay.1** (250°, 218 m) | from relay.1 (R10): the rille's notch with the black Mare beyond, and at dusk the comms dish's slow amber blink far out. **The range ring stops short of it.** You *can* drive in, and the battery shows why you turn back: an honest, soft gate. | **Cell Pack III** (Back socket, 6 → 8 cells) **+ the Field Charge Pylon** (M3-22) | **the Long Mare** (0.6): the region, the comms dish and the relocation site |
| **1.0** | **North: the Peak road** (12°; the foot at `site.peakroad`, 308 m) | visible since minute 1: the staked road, with a **rockfall across the first switchback** (closed, and readable as closed) | as in the region doc: **Atlas** lifts the rockfall and **Long Leap** crosses the rifts. The road is properly reached from the terraces' top. | the Peak, the Great Dish, the finale |

**Kept from `0.6-world-regions.md`, with no region moved:** the Terraces (0.7) open from the Mare's north shore, and
the Shadowed Crater and the lava tube (0.8) open from the Mare. The real lava tube stays 0.8. The headlamp's early
purpose uses only the canyon's **existing** inner section as a short shadowed slot, which is 0.5 content, so nothing
is pulled forward from 0.8.

**Is two exits plus a pocket too confining?** No.
- The pocket gives the first part a purpose in hour two.
- The west exit opens a 1.2 × 0.9 km plain in the very next version.
- The Peak road is seen, closed and promised from minute 1. BotW's plateau shows the walls are fine when the way out
  is visible and earned.

## 5. Impact on opening v3 and 0.6, with roadmap tasks
**Opening v3:**
- **R4 and R6:** the power-flow pulse on the base cables, sun → array → pad, and at dusk bank → pad. This answers the
  owner's "why".
- **R9, the release:** the view from the rise names the gates:
  - the rille notch past relay.1 (west, the Mare);
  - the canyon mouth at relay.2 (east);
  - the Peak road's rockfall (north).

  The range ring is drawn from every charger and visibly stops short of the rille.
- **R10:** relay.1, relit, becomes a waystation. From it the rille notch and the black Mare are in plain view (0.6's
  lure).
- **R11:** at relay.2 the canyon's shadowed slot is right there, black to the stock lamps. **Once the headlamp is
  worn, that slot is the first thing to try** (its purpose). The Canyon Supply Lander's bay rewards it with the Field
  Charge Pylon, a direct link to 0.6.

**The 0.6 plan:** the rille pass leads into the Long Mare, with Cell Pack III and the Field Charge Pylon chain as its
keys. The spine ends with **the caravan to the sunlit swell** and the comms dish lighting (`CARRIER · LOCKED`). C0
"Train feel" (in the Showcase build) is the caravan's first slice, once opening v3 is done.

| Domain | Task |
|---|---|
| **World** | The canyon's inner section in permanent rim shadow as a readable dark slot (0.5). The Peak road's first-switchback rockfall, visible from the base. The rille notch readable from relay.1. The Mare swell as the relocation site near the comms dish (with the Mare kit). Base cable routes for the power pulse. |
| **Gameplay** | The charger network as waystations (the ring from all chargers); the power-flow pulse states (sun and dusk); the Field Charge Pylon reward at the Canyon Supply Lander's bay; packed and unpacked states for the array, bank and mast; caravan C0, then couplers (caravan §3); the 0.6 end beat at the comms dish. |
| **Rover** | A headlamp beam long enough to read in the slot; the coupler rack; Cell Pack III's spine module (0.6). |
| **Art (Argon)** | The slot's shadow edge; the rockfall; skids and packed shapes for the three modules (vacuum shapes); the swell site. |
| **UI** | The ring from every charger; the CRT map marking the rille, the slot and the rockfall as gates (no text list). |
| **Audio** | The power pulse (a soft pentatonic tick); the caravan's skids and strain; in the slot, structure-borne sound only (no air). |
| **Story** | Why the swell gets the light; why the crew left the Field Charge Pylons in the canyon lander; the comms dish's lock line. |

## 6. Playtest signal
- At R9, a first-time player points at one exit and says what might open it.
- After the power pulse lands, asked "what does the solar array do?", they answer "it feeds my charger" without
  reading anything.
- At the 0.6 caravan, nobody asks "do I lose my base?"
