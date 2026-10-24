# Base home and crater exits: research and recommendation

> **Owner's concern** (2026-10-24): the opening sits in a giant crater with apparently one exit, and we said the main
> base is built there. Isn't that too confining? And why do the four opening structures (the pad, the solar array,
> the battery bank, the mast) matter? Do we build them and leave, or is this home?
>
> **Author:** the game design session. It builds on `opening-v3-purpose.md`, `research/caravan-tow.md` ("move to the
> light", deferred), `research/genre-survival-craft.md` (C2 gates, keys and lures), `0.6-world-regions.md`, and
> `ice-and-vehicles.md`. No code.

## 0. Player experience first
| | The answer |
|---|---|
| **Hook** | The crater is a **cradle**: sheltered, warm-lit and small enough to learn. From the first night, three ways out are *visible and closed*: a dark tube mouth, a switchback road up the Peak, a slumped wall in the west. |
| **Pull** | Each closed way out is a question: "what's past it, and what do I need?" Each one is answered by a part on 07's body (C2: gates before keys). |
| **Hold** | What you built **keeps working for you**: the crater camp becomes a waystation that charges 07 on every pass. Nothing is wasted. |
| **Finish** | The 0.6 arc ends with **the caravan arriving on the Peak's sunlit shoulder**: the base unfolds beside the Great Dish, which wakes on that power. Leaving the crater is the emotional milestone, and it is chosen. |
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

## 3. Recommendation: A + B, timed for 0.6, not the end of the opening
- **The crater is "the first camp"** (the lifepod pattern). Its purpose is to get 07 strong enough to leave.
- **B, now (opening and hour two):** every charger 07 lights becomes a **waystation**: the crater pad, then the lit
  relays (they already charge 07). Home is wherever 07 can charge. The range ring is drawn from *all* of them, so the
  map grows as the network grows (Valheim portals, NMS bases, in a calm, vehicle-scale form).
- **A, as the 0.6 spine ("Move to the light", caravan research §2):**
  - **Why move:** the crater floor is a polar cold trap with long nights, while the Peak's shoulder gets near-constant
    sun ("peaks of eternal light", real lunar science). Only that power can wake the Great Dish.
  - **How:** 07 packs the **moveable** modules (the solar array, the bank and the mast) onto skids and tows them up the
    Peak road as a caravan.
  - **What stays:** the **pad stays rooted** as the crater waystation.
  - **The 0.6 end beat:** the caravan arrives, the base unfolds on the shoulder, and the dish wakes on its power, jams
    at 14%, and the carrier locks.
- **Why not leave at the end of the opening?** At minute 60 it would erase the base just built and its "reason to
  return" (P13). Leaving must be **chosen and earned**, after the player has a stake in the place (BotW's lesson).

**How the four structures' purpose reads in the world, with no text** (the owner's "why do these matter"). They are
**07's life support**, and the player must *see* the dependence:
| Structure | What it does for 07 | What the world shows |
|---|---|---|
| **Pad** | 07 lives on it | 07's range ring is centred on it; 07 docks, the walls lock, the cells fill (exists) |
| **Solar array** | it feeds the pad | **a warm pulse runs along the cable from the panels to the pad** while the sun is up; the pad's lamp brightens with the panels' sunlight |
| **Battery bank** | it keeps the lights through the night | at dusk the cable pulse **reverses: bank → pad**, and the base lights stay on while the crater goes dark (first nightfall, exists) |
| **Mast** | 07's voice | its lamp blinks back with relay.1; the decode ticker grows on each dock (exists) |

**Fix for the owner's confusion: one power-flow cue on the existing cables**, a slow pulse moving the way the power
flows. That makes "this is my life support" readable at a glance. When the caravan later packs these modules, the
player is taking **their life support** with them. That is why they pack, and why it feels like moving home rather
than leaving junk.

## 4. Exits: three gates, three parts, in order
| Order | Exit | The gate the player sees | Key: a part on 07 | Leads to | Real-world logic |
|---|---|---|---|---|---|
| **1** (hour two) | **East: the lava tube** at Whispering Canyon's terminus, past relay.2 and 06 | a black tube mouth with a pale skylight shaft far inside; 07's stock lamps die in its dark (the darkness gate) | **the SSR-3 headlamp** (R11, from 06's lamp, Kessler–Arai understanding) | out through the rim to the **eastern outer plain**, the edge of the Long Mare (0.6 region) | lunar lava tubes with skylights are real (the Marius Hills pit) |
| **2** (0.6 spine) | **North: the Peak road**, switchbacks from `site.peakroad` up the Peak's shoulder | a crew-staked road whose second switchback is too steep: 07's wheels spin and slide back (a soft gate, Dredge-style) | **grip rings** (Kessler–Arai level 2), plus **range** (06's cell pack, or the Kestrel hex adapter for a forward charge) | the **sunlit shoulder and the Great Dish**: the caravan's destination and 0.6's end | rim peaks of near-eternal light; Lumen's road to its dish |
| **3** (0.6 late or 0.7) | **West: the slump** beyond relay.1, where the rim wall slid into a rubble ramp | a rock and regolith slope, loose and too soft to climb | **the dig scoop kit** (07's native Dig verb, a kit socket) to cut a track. Later, the anchor winch for its steep top. | the far relay chain and the routes to the cold traps (the ice ladder, `ice-and-vehicles.md`) | slumped crater walls are common |

**Why this order:**
- **The headlamp is the first part 07 earns,** so the first exit is the first thing it's *for*. That strengthens R11's
  purpose.
- **The Peak is the long goal,** visible since minute 1, so it comes second, as the caravan journey.
- **The west slump** opens the ice story later.

Each exit is **visible from the base or from a site the player already visits**, so every gate is seen before its key
(C2).

## 5. Impact on opening v3 and the 0.6 plan
**Opening v3:**
- **R4/R6 (purpose):** add the **power-flow pulse** on the base cables: sun → array → pad, and at dusk bank → pad.
  This answers the owner's "why".
- **R9 (release):** the view from the rise names the gates, not just lures:
  - the tube mouth's skylight glint (east);
  - the Peak road's stakes (north);
  - the dust slump (west).

  The range ring is drawn from every charger.
- **R10:** relay.1's repair makes it a **waystation**: the ring grows, and the west slump is in view from it.
- **R11:** at relay.2, 06 rests at the canyon mouth. **The tube mouth is visible from there and stays black until the
  headlamp is worn.** That gives the headlamp an immediate purpose.
- **Session end:** unchanged (the decode ticker, the bank filling).

**0.6 plan:**
- **Spine:** gate 1 (the tube → the eastern Mare edge) → keys (the cell pack, grip rings, couplers) → **the caravan up
  the Peak road** → arrival → the dish wakes. This is "Move to the light".
- **First:** C0 "Train feel" in the Showcase build (caravan research §5), once opening v3 is done (owner ruling).

**Proposed roadmap tasks, by domain** (each one goes to the roadmap, no code here):
| Domain | Task |
|---|---|
| **World** | Turn the canyon terminus into a lava-tube exit with a skylight (darkness-gated). Make the Peak road drivable, with a slope gate at switchback 2. Add the west slump rubble ramp. Keep the three exits visible from the base, R10 and R11. |
| **Gameplay** | A charger network as waystations (the ring from all chargers); the three gate rules (dark / grade / soft ground); packed and unpacked states for array, bank and mast; the caravan C0 then couplers (caravan §3) |
| **Rover** | The grip rings mount; the dig scoop kit; the coupler rack; a headlamp beam long enough to read in the tube |
| **Art (Argon)** | The tube mouth and skylight; switchback road stakes (Lumen era); the slump; skids and packed shapes for the three modules (vacuum shapes) |
| **UI** | The ring drawn from every charger; the CRT map marking the three exits as gates (no text list) |
| **Audio** | The power-flow pulse (a soft pentatonic tick); in the tube, **structure-borne sound only** (no air): wheel rumble through 07's frame |
| **Story** | Why the dish needs the shoulder (peaks of eternal light); crew stakes and logs along the Peak road; Lumen's survey of the tube |

## 6. Playtest signal
- At R9, a first-time player points at one exit and says what might open it.
- After the power pulse lands, asked "what does the solar array do?", they answer "it feeds my charger" without
  reading anything.
- At the 0.6 caravan, nobody asks "do I lose my base?"
