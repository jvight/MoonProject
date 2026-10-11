# M3-19: Inside the lander (draft)

> Draft by the game design session for the Director. Not adopted until the Director moves it to `docs/features/`.
>
> **Owner's morning review (2026-10-11), from screenshots of the base:**
> 1. The crew's lift has no visible purpose: it raises 07 on an outside frame to a deck with nothing on it. Put it
>    **under the lander's belly**, carrying 07 **up into** the lander, so its job is obvious: entering home.
> 2. Kenji's old bench (now the pen plotter beside the bay) reads as a cluttered table.
> 3. The memory shelf (glass cabinet, scalloped awning, orange strip lights) doesn't look right for the Moon or for 07.
> 4. The tower pilot's big halo reads as a floating orb. World is fixing that; this spec only adds a rule (§8).
>
> Builds on M3-16 (the steps), M3-09 Part B (collections at the base), M3-07 (the map), M3-18 (modern look) and the
> 2026-10-11 reviews (V5 declutter, L5 lookout). Rulings 11–14 and pillar 6 apply.
>
> Angles judged: behind 07 at the base (`art/.../wake-after/night_wake_lift_lift_from_chase`) and the lander from the
> ladder side (`art/.../wake-after/night_wake_lift_dock_side_lander_lift_ladder`).

## 1. The problem in one frame
In the ladder-side frame, 07 stands beside the lander with its head level with the descent stage's top. The cabin
is about 2.9 m across and 2.2 m tall (`LanderMeshes`: cabin radius 1.45 m, stage bottom 1.45 m, deck 2.8 m). **07
cannot fit under the belly, and it cannot fit inside.** That is why the lift had to go outside and up to a bare deck.

So the owner's ask has a precondition. **The lander must become a habitat lander big enough for 07** (ruling 13: the
crew's world at true scale, 07 a 2.3 m rover inside it). Everything below follows from that one change.

## 2. Decisions (proposed)
| # | Decision | Why |
|---|---|---|
| D1 | **The lander grows into a two-level habitat lander.** The belly sits high enough for 07 to drive under, and the cabin is big enough for 07 to turn around in. | the owner's ask can't be built on the current lander |
| D2 | **The lift is a round platform under the belly** that rises through a floor hatch into the cabin. Its job is "go inside". | owner verdict 1 |
| D3 | **The dock stays separate,** on the apron just in front of the belly, facing Earth. It is not the lift's platform. | "one job per place" (M3-16 rule 3); the P5 stargaze needs open sky |
| D4 | **The lift wakes with home (tower L1),** and is restored the first time 07 brings a relic home (step 4). It no longer waits for L3. | the lift gets used every trip, so its purpose is learned early |
| D5 | **Inside are the crew's four corners:** a bunk, a porthole and a keepsake bay per crew member. They replace the outdoor memory shelf. **Ines's map** is on the light table under the big porthole. | owner verdict 3; M3-09 Part B; M3-07 moves inside |
| D6 | **The deck lookout is dropped.** Its view becomes the cabin's big porthole above the map table, facing the Peak. Its "seen" sweep (L5) moves to the dock's evening (P6). | the deck has no job; the porthole is the same height, and P6 is already a wide still shot |
| D7 | **The plotter folds into the bay,** mounted on the bay's back wall above the turntable. The outdoor bench goes. Kenji's mug stays on a bay shelf as the one remnant. | owner verdict 2; one place for kits, one job |
| D8 | **Tower L3 wakes the chart** (the light table and Kenji's chart pen, M3-07). It no longer wakes the lift. `BasePowerStage.Lift` becomes `Chart` (same ordinal). | keeps the power spine: L1 home, L2 bay, L3 the station's reach on paper |
| D9 | **Home is three masses:** lander (dock in front, lift beneath, everything personal inside), bay (kits and blueprints), tower (power). Bell's and Tilly's spots sit beside the lander. | V5, folded in here |

## 3. The habitat lander (art; sizes are targets for the builder)
07's envelope is assumed to be about 2.3 m long × 1.7 m wide × 2.0 m tall with the head up. **Art confirms it from
the prefab bounds first.** Every number below scales from it.

| Part | Today | Target | Reason |
|---|---|---|---|
| Belly clearance (ground to stage bottom) | 1.45 m | **2.6 m** | 07 plus 0.6 m headroom; a towed relic floats beside it |
| Lift platform | — | **3.4 m diameter**, rover-height rim lamps | 07 plus one towed relic side by side |
| Descent stage | radius 2.1 m, 1.3 m tall | radius **3.8 m**, 1.0 m tall | the floor of the cabin; holds the hatch ring and winch |
| Cabin (interior) | radius 1.45 m, 2.2 m tall | **6.4 m across, 2.7 m tall** | the 3.4 m platform in the middle plus a 1.5 m ring of crew corners; people (1.75 m) stand, bunks fit |
| Legs | foot radius 3.4 m | foot radius **6.0 m** | four splayed legs frame the belly bay; 07 drives in between two of them |
| Overall height | ~5 m + dish | ~**7.5 m** + dish | the tower must stay taller: **tower L1 ≥ 9 m** |

Keep the character: the riveted octagonal cabin, round portholes, the dish on top, the ladder up one leg (the crew's
remnant; it now leads to the crew's old hatch on the roof). The weathering rules are unchanged. The silhouette grows
from "tin can on stilts" to "a small home on four legs with a garage-height belly", which reads at 150 m. (The tower
height rule also comes from the 10-10 review.)

## 4. The belly lift
- **Dormant (fresh game):** the platform is down, with a dust drift across it, one cable slack and the hatch above
  closed and dark. It is visible from the chase camera when 07 faces the lander. No prompt (P4: only the next thing
  speaks).
- **Wakes (step 2, tower L1):** the belly's work lamp comes on with the windows, and the rim lamps blink once. The
  slack cable shows "jammed" clearly: one side of the platform hangs 30 cm low.
- **Restored (step 4, the first relic home):** 07 tows the first relic toward the lander. The platform's pad lights,
  the "next" line says "The lift is stuck: stitch the cable." 07 stitches the cable (an existing verb, one part,
  no feeding), the platform levels, and 07 drives on with the relic.
- **The ride:** drive onto the platform and stop, and it rises by itself after a short settle (~1 s; no button,
  ruling 14).
  - The hatch's two leaves slide open overhead, and the platform rises ~3.6 m in ~4 s, eased at both ends.
  - Belly-lamp light spills down. The winch has a low in-key hum (D), and the hatch opens on an A.
  - The camera rides a fixed crane path from behind 07, up through the hatch ring (§6).
- **Down:** the back input, or driving off the platform's edge inside. The platform lowers, and 07 drives out
  forward, the way it came in. **07 always drives in and out in the same direction** (the platform turns 180° while
  lowering), so the player never reverses blind.

## 5. The cabin: what's inside, what 07 does
07 never drives inside. **The platform is a turntable at the cabin's centre**, the bay's idea reused (ruling 12).
Left and right turn it softly to face the next station. Look focuses items (ruling 2's soft cone). Back lowers the
platform.

| Station (clockwise from the hatch's front) | What it is | What 07 does |
|---|---|---|
| **The big porthole and Ines's map table** | the light table under the largest porthole, which frames the Peak and its red blink. The map is pinned on it (M3-07). | looks: the map view opens, as in the M3-07 draft, with the hatch steps removed |
| **Ro's corner** | bunk, porthole, keepsake bay, and the station radio set the crew left playing ("Keep the radio on") | looks; a relic towed in is lifted by the beam into its slot |
| **Theo's corner** | bunk, porthole, keepsake bay, a dried basil pot under a dead grow-strip | same |
| **Ines's corner** | bunk, porthole, keepsake bay, her field journal shelf | same |
| **Kenji's corner** | bunk, porthole, keepsake bay, a spare-wheel bracket (empty: the wheel is a relic) | same |

- **Keepsake bays** replace the glass shelf (owner verdict 3). Each is a deep open cubby set into the wall under the
  bunk, at 07's lens height (0.9–1.6 m). There is no glass, no awning and no strip lights.
  - Six slots each, with a **dust outline** where each object stood (M3-09).
  - A small brass name plate in the crew member's handwriting.
  - One warm reading lamp per corner that brightens one step per relic placed.
- **Logs** clip to the bunk's edge as paper cards (M3-09).
- **The crew's portholes are the outside view of progress:** each placed relic warms that crew member's porthole one
  step, and a complete set lights it fully with its curtain open. This is DESIGN's "crew windows", readable from
  outside at 30 m (ruling 11).
- **The cassette rack stays outside with Bell** for 0.5 (it is built and is Bell's job). The Director may move it to
  the radio set in Ro's corner later, as a separate decision.
- **Depositing a relic:** the towed relic rides up beside 07. The turntable turns to its owner's corner, and the beam
  lifts it into its outline. The slot lamp lights, the corner lamp steps up, and outside the porthole warms. The
  existing deposit verb (beam lifts into a slot) is unchanged; only where it happens moves.

## 6. Camera in a small space
- **No free chase camera inside.** One camera rides a ring rail just under the cabin ceiling, always opposite the
  station 07 faces, looking over 07's shoulder. Turning the turntable moves the camera around the ring with the same
  easing.
- The rail is inset 0.4 m from the wall, so it can never clip. Near plane 0.05 m. FOV 55° (narrower than the chase,
  to keep the room's scale human).
- **Entering:** a crane path from the chase position, under the belly, up through the hatch, to the ring rail
  (~4 s, matching the ride).
- **Map and browsing:** the M3-09 browse camera (three-quarter ease plus DOF), launched from the ring.
- **Lighting:** practical warm lamps only (pillar 2), plus cool earthlight through the portholes. It is the warmest
  frame in the game, and it should be.

## 7. The steps, revised (M3-16 table)
| # | Step | Change |
|---|---|---|
| 2 | The tower wakes (L1) | + the belly lamp and hatch rim light. The lift shows "jammed", silent. |
| 4 | First memory | **+ the lift**: the first relic home needs it. Stitch the cable, ride up, place the relic in its owner's keepsake bay. The verb taught is "ride the lift" (it moves here from step 8). |
| 5 | Bay (L2) | The bay now holds the plotter on its back wall. One place for kits and sheets. |
| 8 | **The crew's chart** (L3, ~55 min) | L3 lights the light table and Kenji's chart pen. On the next ride up, the map inks everything visited in reach (M3-07). This is the 0.5 finale, inside home. |

The dock's evening (P6) gains the L5 sweep: on parking, the wide shot's places blink once in turn, then the tower
raises the next pillar. Those blinks are what the map draws as "seen".

## 8. Home, decluttered (V5 folded in)
- **Removed from outside:** the cable-lift frame and its jam props, the lander-deck guardrail lookout, the memory
  shelf cabinet, Kenji's bench, and the loose stands, lectern and crates near the lander.
- **The three masses,** each with ≥ 4 m of open, lit ground between them (V3 pools):
  1. **the lander** with the dock in front, the lift beneath, Bell's corner and Tilly's perch beside it;
  2. **the bay** with its apron, the plotter and its "07" sign;
  3. **the tower** with its port and pad.
- Target: ≤ 6 small silhouettes in the owner's chase frame (V5's acceptance).
- **Halo rule (owner verdict 4, for World):** a warm point's halo may never be larger on screen than the object
  that carries it, within 60 m. Beyond 60 m the beacon-style minimum angular size applies (L6).

## 9. Impact on built things
| Built | Fate |
|---|---|
| Cable lift frame, jammed platform, snapped cable props (art) | removed from the base builder. The platform and jam parts move under the belly (reuse the meshes where shapes fit). The existing prefab GUIDs stay for anything kept. Deleted outputs go through the builder, never hand-deleted `.meta`. |
| `Gameplay/Lift` (`LiftCycle`, `LiftStation`, `LiftTuning`, `LiftSaveData`) | kept and re-aimed. Dormant → restored by stitch → ride. The **Lookout** phase becomes **Inside** (turntable facing, back to descend). Power gate moves from Lift/Chart to Home. Restoration needs a stitch only, no feeding. |
| Rover's lift slice (ride pose, deck lookout shot) | the ride pose is kept. The lookout shot is replaced by the crane-up path and the ring-rail camera. |
| M3-07 map slice, deck-hatch plan (paused) | resumes inside: the hatch steps become "face the map table". States and the chart pen are unchanged. |
| Memory shelf (art + gameplay `place.shelf`) | replaced by four keepsake bays. `place.shelf` is renamed `place.cabin` (one place, one job line: "the crew's home; their things go back where they were"). |
| Kenji's blueprint board / plotter (`place.board`) | moves onto the bay's back wall. `place.board` is folded into `place.bay`'s job line. |
| Lander deck guardrail and ladder | the guardrail goes. The ladder stays and now leads to the roof hatch. Its line ("Not made for wheels") gains a look down at the belly lift. |
| Golden-path Playthrough | step 4 adds the lift restore and the ride with the first relic. Step 8 becomes the chart. Every deposit after step 4 goes through the lift. |
| Saves | content floor raised (the lander's places changed), as in M3-16 "Save compatibility". |

## 10. Contracts (Core, landed first)
- **`BasePowerStage`:** Asleep, Home, Bay, **Chart** (`Lift` renamed; same ordinals). Home now also gates the lift.
- **`IHomeInterior`** (registered by Gameplay): `IsInside`, `Facing` (a `CabinStation`: Map, Ro, Theo, Ines,
  Kenji).
- **Events:**
  - `CabinEntered` and `CabinLeft`: Rover switches cameras, UI hides the HUD, Audio swaps to the room tone and the
    radio set's near sound.
  - `CabinStationFaced(station)`: published by Gameplay on each turntable stop.
  - `KeepsakePlaced(relicId, crewId, countInSet)`: replaces the shelf's deposit event for its listeners. World warms
    that crew's porthole one step. `CrewSetCompleted(crewId)` comes from M3-09.
- **`LiftCued`** is kept, with new cues: `HatchOpen` and `HatchClose`.
- **Place ids:** `place.cabin` replaces `place.shelf`. `place.board` and `place.lift`'s deck job line are retired.
  `place.lift` gets the line "The crew's lift: up into the lander." Localization keys follow.
- **Anchors:** `IHomeLights` gains per-crew porthole lights (`HomeLight.Porthole<Crew>`), lit in steps by World.

## 11. Split
| Box | Delivers |
|---|---|
| art | the habitat lander (§3): belly bay, lift platform, floor hatch, the cabin interior with four crew corners, keepsake bays with outlines and plates, bunks, the map light table and big porthole, the radio set; the plotter on the bay's back wall; base builder declutter (§8); dormant/awake states |
| gameplay | the lift re-aimed (Home gate, stitch restore, Inside phase, turntable facing), deposit inside, `IHomeInterior`, the events, place ids, revised step flow (steps 4 and 8), save floor |
| rover | the crane-up path, the ring-rail interior camera, the 180° platform turn on descent, the ride pose |
| world | belly lamp and hatch rim light per stage, porthole lights per crew set, the halo rule, warm pools around the three masses (V3) |
| ui | HUD hidden inside, the turntable prompts (first time only: "Turn", "Look", "Down"), the map view from the light table, the keepsake cards (M3-09) |
| audio | winch hum, hatch open/close, the cabin room tone, the radio set close up, the turntable's soft detent ticks in key |
| Director (story) | the four corners' small details, plates and lines, EN and VI |

## 12. Acceptance
- **From the owner's two angles**, a first-time player can say what the lift is for without text ("it takes 07 up
  into the lander"), because the platform, hatch and lit cabin are in line under the belly.
- **The ladder-side frame:** 07 fits under the belly with visible headroom, and the lander is clearly taller and
  wider than 07. The tower is still the tallest thing at home.
- **The behind-07 frame:** three masses with lit ground between them, ≤ 6 small silhouettes, and no glass cabinet or
  bench.
- **Golden path:** the first relic goes up the lift and into its owner's bay at step 4. From outside, that crew's
  porthole is visibly warmer afterwards.
- **Inside,** the camera never clips, every station is reachable with left/right only, and back always brings 07
  down facing out.
- **Ruling 14:** 07 never presses, carries or climbs. The platform, hatch and turntable are machines, and the beam
  places relics.
- 60 fps and zero per-frame GC inside the cabin (its lights are culled when 07 is outside, and the interior is
  hidden outside).

## 13. Open questions for the Director
1. Is a **2.6 m belly and ~7.5 m lander** acceptable against the tower? This spec assumes the tower is raised to
   ≥ 9 m at L1. If not, the cabin can drop to 6.0 m across × 2.5 m tall, the minimum for the turntable plus a 1.3 m
   ring.
2. **Cassette rack** inside on Ro's radio set, or outside with Bell? This spec says outside for 0.5.
3. Should the **dock** sit just in front of the belly (proposed) or **inside the belly bay**? Inside, 07 would sleep
   sheltered, but the P5 stargaze would lose open sky.
