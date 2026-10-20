# Caravan tow: research on 07 hauling a train of loads

Status: research for the owner's idea, 2026-10-20. Not a decision. Author: the game design session.

> The owner: *"tôi nghĩ cái việc 07 kéo sau 1 đống thùng hay phết, nó cũng giải thích luôn việc tại sao 07 cần gom
> hết đồ đạc di chuyển khỏi cái miệng hố đầu game"*. In English: 07 towing a whole train of crates is great, and it
> could explain *why* 07 gathers everything and moves out of the starting crater.

**What exists on main** (a7b0fc26, `Gameplay/Tow`):
- `TowLine`: a physical winch cable that only pulls, never a beam (ruling 4).
- `TowableBody`: a skid load. It is kinematic until hooked, slides while towed, and stands fast once at rest.
- `TowSnag`: the winch lifts a caught load free once; past the let-go distance (up to 12 m) it unhooks the load softly. Nothing is ever lost.
- `BatteryTuning`: towing costs 1.4× battery and caps speed.
- `TowSaveData`: one load on the cable.

It is used in opening v3 R6 to tow the power module home.

**Confidence:** game facts come from my knowledge of the games. The details I could not confirm are marked
*(unverified)*.

---

## 0. Player experience first
| | What it should be |
|---|---|
| **Hook** | The first time a second load clunks onto the first one's hitch and the cable goes taut behind, the player looks back. A little machine dragging its whole life uphill is a picture that sells itself. |
| **Keep** | Every trip home *brings something*. The train is the visible size of the player's ambition. Each coupler, drum or cleat on 07 lets it pull more, or pull it up steeper ground. |
| **Finish** | The caravan arrives somewhere that matters (§2), and its loads unfold into a new home. |
| **Feeling** | Patient and stubborn: lonely but purposeful. The Long Drive's calm, with Death Stranding's weight. Never stressful. |

## 1. Precedents
| Game | What makes hauling fun | What makes it tedious | How it avoids the chore | Lesson for us |
|---|---|---|---|---|
| **Death Stranding** | Felt weight: stacked cargo sways and stamina drains. Planning a route is a puzzle. Arriving is the reward, and each delivery visibly connects the map. | Re-walking known ground; cargo micro-management | **Infrastructure you build removes friction:** roads, ziplines, other players' structures. The haul gets easier because you invested. | Let 07 lay **route infrastructure** (posts, relays, later rails) so repeat trips get smoother. |
| **SnowRunner / MudRunner** | Terrain is the opponent. The winch to an anchor is a satisfying puzzle tool. Trailers change how the truck handles. | Repeatedly stuck; long, slow trips; camera fights | Scouting reveals routes before you commit; recovery options; tasks are chosen, not forced | The **winch as a puzzle verb** (anchor up a slope) is fun. Getting stuck again and again is not, so give a soft recovery. |
| **Subnautica: Below Zero, Seatruck** | A **modular train**: cab plus segments (storage, fabricator, sleeper, dock), hitched and dropped at will. Home comes with you. | Each segment slows you; long trains snag in caves *(details unverified)* | Segments detach anywhere; the train is optional; you choose its length per trip | **The closest precedent.** Segments that are also base modules; the length is the player's choice; each car costs speed. |
| **Subnautica: Cyclops** | A mobile base is the late reward for deep expeditions | Piloting a huge hull in tight caves | Fixed bases remain; the Cyclops is an expedition tool, not the only home | Moving home is a **mid/late reward**, never a replacement for the first home. |
| **Astroneer** | Rovers and tractors chain trailers that carry modules (even a working base on wheels) | Physics flips, trailers catching on terrain *(common complaint)* | Small chains; you can flatten terrain | Chained physics must be **assisted**, or it flips and snags. |
| **Pacific Drive** | The car is home and character. Upgrades show on the car; you bring it back to the garage. | Repairs and inventory pile up | Garage automation; care for the car is the point | **07 is the car.** Every hauling upgrade must show on 07. |
| **The Long Drive** | A lonely road, the car as everything, a meditative pace | Aimless; bugs | Little: it leans on mood | The **solitude-on-the-move** mood fits pillar 6, but needs a destination. |
| **Euro Truck Simulator** | Calm hauling as a whole genre: radio, views, a careful arrival | Long stretches | Chosen jobs, fast travel between depots | Cozy hauling works when it's **calm, readable and chosen**. |
| **Valheim carts** | Ore can't be teleported, so a cart haul is a real journey | Cart physics on rough ground; players complain | Ships and roads; players build paths | Forced hauling gives journeys meaning, but **the cart's handling decides whether it is loved or hated**. |
| **Zelda: Tears of the Kingdom** | Building your own hauler is creative and funny | Fiddly physics; Korok carrying is widely called tedious *(reception)* | Short, optional | **Improvised physics is delightful once and tiresome when repeated.** Keep our train predictable. |
| **Outer Wilds** | The ship is a base that moves: the log and the campfire | (no hauling) | — | A home that moves gives freedom, but the destination gives meaning. |

**The pattern the good ones share:**
- the load is **felt** (weight, sway, sound);
- the route is **read and chosen**;
- the trip **ends somewhere that changes**;
- **friction falls as you invest** (roads, segments, upgrades).

**The bad ones:**
- unassisted chained physics;
- getting stuck repeatedly;
- hauling you're forced to repeat.

## 2. The "why": a lunar-logic reason to leave the crater
**Candidate reasons:**
- **"The crater base can't survive the next long night."** It fights the calm (ruling: no death in the opening, and
  the sky follows progress). It is survival pressure, and it throws away the home the player just built. **Reject.**
- **"The call is out of range."** It is true, but relays already answer it (R8, R10). Moving the base isn't needed
  for that. **Weak.**
- **"Move to the light" (recommended).** It is real lunar science and it fits our polar site (VISION L1):
  - near the lunar poles, **crater floors are cold traps** that get little or no sunlight;
  - some **rim peaks get sunlight almost all the time** ("peaks of eternal light");
  - our **Peak** is such a rim peak, and it carries the **Great Dish**, dead since 1988 because nothing up there has
    power;
  - 07's crater base lives through long nights on its bank. **A base on the Peak's rim would run on near-constant
    Sun, and that is the only power that can drive the dish.**

  So 07 packs its base onto skids and hauls it up the route, relay by relay, to wake the dish.

**Does it strengthen or fight opening v3's spine?** It strengthens it, if it lives in 0.6, not in the opening:
- **The destination has been in view since minute 1.** The Peak was the R8 glint, the R9 "just outside the ring",
  and the 0.6 end beat (purpose §6). The caravan *is* the 0.6 journey to that end, and the 0.6 end becomes: **the
  caravan arrives, the base unfolds on the rim, the dish wakes on its power, jams at 14%, and the carrier locks.**
- **It answers two open questions with one physical act:** why the dish is dead, and why 07 must go there itself.
- **It keeps the home:** the crater base isn't abandoned. It stays as a **waystation**: the pad and the bank keep
  charging, and they feed the range ring. Only what the rim base needs gets packed. Players choose what to take, as
  with the Seatruck.
- **Later, the ice ladder:** the crater floor's cold trap is where the ice evidence lies (story v4). Leaving it for
  the light, then returning to it for the ice, is a good mid-game turn.

**It would fight the spine at the end of the opening.** R9 releases the player *to* the basin with a home to come
back to. Packing up at minute 60 would erase the base just built, and with it the "reason to return" (P13).

## 3. The mechanic
**The train.** 07, a cable, load 1, a coupler, load 2, and so on, up to N (start at N = 3).
- Each load is a `TowableBody` with a **rear hitch**.
- The first link is today's cable (only pulls, can go slack).
- Later links are **short rigid couplers**, so a train never folds up on itself.

**The physics model (recommended: assisted, not a free chain).**
- **Only load 1 is simulated as today.** Loads 2..N follow 07's path as a **breadcrumb trail**, with spring offsets
  for sway, skids for contact and a little lag. That is how good trailer games keep long trains readable.
- **It removes the classic failures:** jackknifing, flipping, and N rigidbodies fighting through a rope solver.
- **Snag and let-go are per hitch:**
  - a caught car is lifted once (as `TowSnag` does now);
  - past its reach it unhooks softly and **stays where it is**;
  - it shows as a marker on 07's range ring. Never lost (VISION calm).

**What each extra car costs and gives:**
| Cars | Battery cost | Top speed | Turning | Max slope | Gives |
|---|---|---|---|---|---|
| 1 (now) | 1.4× | capped | normal | normal | one big salvage home, or the bank's frame (R6) |
| 2 | about 1.6× | lower | wider (cars cut corners) | −3° | a module plus its parts in one trip |
| 3 | about 1.8× | lower | wider | −6° | a base section, or a full salvage strip |

All values go in tuning, as starting points. Extra cars are worth it only when the trip is **long** (to the rim) or
the load is **heavy** (a wreck to strip at home). Short errands stay one load, so the choice reads.

**The base as towable modules** (base-building pivot):
- Structures 07 builds from flat-packs get a **packed state**: legs fold, panels stow, skids drop. The owner's rule
  holds: every state change gets a visible mechanical action and a chirp.
- Unpacking at the destination is the build's own unfold animation, played in reverse order.
- Only **moveable** structures pack: the solar array, the bank, a storage rack. The charging pad stays rooted, and the
  crater keeps it as a waystation.

**Visible progression on 07** (ruling 11):
| Upgrade | Where it shows on 07 | What it unlocks |
|---|---|---|
| **Tow hitch** (now) | the winch and hook on the back | 1 load |
| **Couplers** | a coupler rack on the rear deck; each load gains a coupler | 2–3 cars. Source: Lumen's cargo sledges, as scan fragments |
| **Heavy drum** | a bigger winch drum and a thicker cable | longer reach, a heavier load 1 |
| **Grip rings** (Kessler–Arai level 2, maker-understanding) | welded cleat rings on the wheels | +slope when towing |
| **Anchor winch** (0.6+) | a ground spike and a second drum | winch the train up a cliff one car at a time (SnowRunner): the terrain-gate key |

## 4. Risks
| Risk | Why it bites | Mitigation |
|---|---|---|
| **Tedium** | Long drives at a capped speed, repeated | Few, meaningful long hauls: one big move in 0.6, chosen salvage trips. Route posts and relays make repeat trips quicker (Death Stranding). Short errands stay single-load. No forced re-hauls. |
| **Camera** | A train behind 07 leaves the frame, and turning hides it | The chase camera pulls back with the train's length. Add a soft "look back" when a hitch tugs or a car snags, using the existing camera moment system. |
| **Pathing on rough ground** | Cars clip rocks, trains fold or flip | The breadcrumb model; rigid couplers; snag-lift; soft let-go; a max slope per car count read on 07's face before the climb. |
| **Save** | `TowSaveData` holds one load | Extend it to an ordered list of {towId, pose, hitched}. Parked cars save as standing loads. EditMode round-trip tests. |
| **Performance** | N bodies plus cables plus skid contacts | Only load 1 is fully simulated; followers are kinematic with cheap ground probes; parked loads sleep. Cap N at 3–4. Frame-time check in the feel capture. |
| **Feel gate** | Train handling is love-or-hate (Valheim) | Prototype in the Showcase build first (owner: part by part); the owner feels it before any content depends on it. |

## 5. Recommendation
1. **Where it fits:**
   - **Not** as the end of the opening.
   - Make it **0.6's spine journey: "Move to the light."** The caravan hauls the base's moveable modules up the relay
     route to the Peak's rim of near-constant sunlight. The 0.6 end beat (the dish wakes on that power, jams at 14%,
     and the carrier locks) becomes the arrival.
   - The crater base stays as a waystation.
   - Before that, the tow's role grows **gently**: single big salvage hauls in hour two (strip at home), and 06's
     rescue (the Director's earlier proposal 2) as the first emotional tow.
2. **The minimal first slice: "C0 Train feel", in the Showcase build.**
   - **Build:**
     - give `TowableBody` a rear hitch and a coupler;
     - add a breadcrumb follower for loads 2..3;
     - stack the per-car cost and slope in tuning;
     - the chase camera pulls back with the train's length.
   - **Accept:**
     - EditMode: follower path math (turns, offsets), stacked cost, and the save round-trip for 3 loads;
     - PlayMode: tow 3 loads 150 m across basin terrain with 2 turns and a 10° slope, with no fold, flip or lost car,
       within the frame budget.
   - **Playtest signal:** the owner looks back at the train unprompted and wants to add a car.
   - **Owner:** Gameplay (tow and save) with Rover (camera). Size M. It is independent of the opening slices.
3. **If C0 feels good, one cheap teaser in the opening (optional):** at R6, the cable-reel crate hitches behind the
   power module, so "the bank's frame and its wiring come home together". It teaches the coupler idea without new
   items. Decide after C0.
