# M3-19: The crew's lab, up the belly lift

> Owner: Director (design). Version: **0.5**. Final spec, 2026-10-11. It replaces the game design session's draft
> (`docs/design/drafts/M3-19-inside-the-lander.md`, removed; see git history) with GDD v3's purpose for the lander
> (`docs/design/gdd-v3.md` §6.1, §4.1, §10) and art's four refinements to the blockout the owner liked
> (`box/art-sketch-m319`).
>
> **The owner's asks (2026-10-11):** the lift must go under the lander's belly and carry 07 **inside**, so its purpose
> reads; the dock stays in front of the belly; the inside is the crew's **lab**, where finds become know-how; the old
> bench, the outdoor memory shelf and the deck lookout go. Rulings 11–14 and pillar 6 apply.

## 1. The lander's purpose, in one line
*The crew's lab, where 07 turns what it finds into what it becomes.* From outside, a first-time player can say what
the lift is for without text: the platform, the hatch and the lit cabin are in line under the belly.

## 2. Decisions
| # | Decision |
|---|---|
| D1 | **The lander becomes a two-level habitat lander** big enough for 07: a garage-height belly and a cabin 07 can turn round in on a turntable. |
| D2 | **The lift is a round platform under the belly** that rises through a floor hatch into the cabin. Its job: "up into the lab". |
| D3 | **The dock stays separate,** on the apron just in front of the belly, facing the lander (owner's choice). |
| D4 | **The lab wakes with home (tower L1).** The lift is restored the first time 07 comes home carrying something for the lab (a relic or a blueprint tube): on the golden path, the first relic at step 4. |
| D5 | **Inside are four lab stations** around the turntable: the **survey chart** under the big porthole, the **reader**, the **tech board** with the recovery panel at its head, and the **keepsake shelf**. Each has a verb. |
| D6 | **The deck lookout is cut.** Its view is the big porthole above the chart, facing the Peak. Its "seen" sweep moves to the dock's evening (P6). |
| D7 | **The plotter folds into the bay,** on the bay's back wall above the turntable. The outdoor bench goes; Kenji's mug stays on a bay shelf as the one remnant. |
| D8 | **Tower L3 wakes the survey chart.** `BasePowerStage.Lift` becomes `Chart` (same ordinal). Power stays the spine: L1 home and the lab, L2 the bay, L3 the chart. |
| D9 | **Home is three masses:** the lander (dock in front, lift beneath, Bell's corner and Tilly's perch beside it), the bay (kits, the plotter), the tower (power), with ≥ 4 m of lit ground between them. |

## 3. The habitat lander (art)
Sizes follow the blockout with art's four refinements. Art confirms 07's envelope from the prefab bounds first
(about 2.3 m long, 1.7 m wide, 2.0 m tall with the head up).

| Part | Target |
|---|---|
| Belly clearance (ground to the stage bottom) | **2.6 m**: 07 plus headroom; a towed relic floats beside it |
| Lift platform | **3.4 m** across, rover-height rim lamps; it is also the cabin's turntable |
| Descent stage | radius ~3.8 m, ~1.0 m tall: the cabin floor, the hatch ring and the winch |
| **Cabin interior** | **about 5.6 m across, 2.5 m tall** (refinement 1): the 3.4 m turntable plus a ~1.1 m ring of stations |
| Cabin shell | **octagonal, with weathered panels** (refinement 2): riveted, sun-bleached, rust from the rivets, dust caps on the ledges (ruling 12, M3-12) |
| Roof | **tapered** (refinement 3), with the dish and the crew's roof hatch |
| Legs | four splayed legs, foot radius ~6 m; 07 drives in between the two front legs |
| Height | **the tower stays taller than the lander** (refinement 4): tower L1 at least 1.25× the lander's height to the roof, so the tower is the tallest thing at home |

- **Keep the character:** round portholes, the dish on top, the crew's ladder up one leg (a remnant; it now leads to
  the roof hatch, and its line "Not made for wheels" gains a look down at the belly lift).
- **The big porthole** faces the Peak above the survey chart. Round it sits a ring of small lamp sockets, **one lamp
  per known tech node** (`ITechTree.KnownCount`), so the lander's face counts 07's growth at 30 m (ruling 11; up to
  32 sockets by 0.8, 9 used in 0.5). Through it at night: the board's lamps and the chart's glow.
- **Dormant / awake:** a fresh game shows the lander dark; L1 lights the windows, the belly work lamp and the hatch rim.

## 4. The belly lift
- **Dormant (fresh game):** the platform is down, a dust drift across it, one cable slack, the hatch above closed and
  dark. No prompt (P4).
- **Wakes with L1:** the belly lamp comes on with the windows and the rim lamps blink once. It reads jammed: one side
  of the platform hangs ~30 cm low.
- **Restored:** the first time 07 comes home with a relic or a tube, the platform's pad lights and the "next" line
  says "The lift is stuck: stitch the cable." 07 stitches (an existing verb, no feeding); the platform levels.
- **The ride:** 07 drives onto the platform and stops; after a ~1 s settle it rises by itself (no button, ruling 14).
  The hatch leaves slide open, the platform rises ~3.6 m in ~4 s, eased at both ends, belly light spilling down. The
  winch hums low in key (D); the hatch opens on an A.
- **Down:** the back input. The platform turns 180° while lowering, so 07 always drives out forward, toward the dock.
- **Towed relics** ride up beside 07 on the platform.

## 5. Inside: the turntable and the four stations
07 never drives inside. The platform is a turntable at the cabin's centre (the bay's idea, reused). Left and right
turn it softly to the next station; Look focuses a station; Back lowers the platform.

**When 07 arrives carrying something, the turntable turns by itself to the station that wants it:** a tube to the
reader, a relic to the keepsake shelf. Otherwise 07 arrives facing the survey chart and the Peak.

| Station (clockwise from the front) | What it is | What 07 does |
|---|---|---|
| **Survey chart** (front, under the big porthole) | Ines's light table and Kenji's chart pen (M3-07), facing the Peak and its red blink. Dark until tower L3. | Look: the map view opens (M3-07's look; the deck-hatch steps are gone). In 0.5 the chart shows reach, chargers' circles and the Link's line (M3-21). |
| **The reader** | a machine 07 feeds with its beam: a tube slot at lens height, a spinning reel and a paper tape running to the board. Never a desk. | The beam feeds the tube in; it decodes in ~4 s (a moment, not a timer). Then the **hand-off**: a lamp pulse runs from the lab down the lander's leg and along the ground cable to the bay, and the plotter starts drawing while 07 rides down. If the bay is still asleep (before L2), the pulse stops at the dark bay with a flicker of its sign, and the plotter draws the waiting sheets the moment L2 wakes it. |
| **The tech board** (with the recovery panel at its head) | Kenji's plans for 07: one pin per 0.5 node, a sheet on each decoded node, a lamp on each built one. Its header is the **recovery panel**: four rows of lamps (M3-21). Only 0.5's 9 nodes are shown. | Look: the board fills the frame; the newest sheet glows once. |
| **The keepsake shelf** | one shelf of deep open cubbies at lens height (0.9–1.6 m), four crew sections with a brass name plate each, a dust outline where each thing stood, one warm reading lamp per section that brightens per keepsake. No glass, no awning, no strip lights. | The beam lifts a towed relic into its outline (the existing deposit verb, moved inside). Logs clip on as paper cards (M3-09). |

- The cassette rack stays outside with Bell in 0.5.
- **The lab is the warmest frame in the game:** practical warm lamps only, plus cool earthlight through the portholes.

## 6. Camera
- **No free chase camera inside.** One camera rides a ring rail just under the ceiling, opposite the station 07 faces,
  looking over 07's shoulder; turning the turntable moves it round the ring with the same easing.
- The rail is inset 0.4 m from the wall and can never clip. Near plane 0.05 m. FOV ~55°.
- **Entering:** a crane path from the chase position, under the belly, up through the hatch to the ring (~4 s,
  matching the ride). Leaving reverses it.
- **Looking at a station** (the chart, the board): the M3-09 browse camera from the ring (three-quarter ease, DOF).
- Inside lights and the interior are culled while 07 is outside; the interior is hidden from outside except through
  the portholes' glow.

## 7. The steps, revised (M3-16 table)
| # | Step | Change |
|---|---|---|
| 2 | The tower wakes (L1) | + the lab's windows, the belly lamp and the hatch rim. The lift shows "jammed", silent. |
| 4 | First memory | **+ the lift:** the first relic home needs it. Stitch the cable, ride up, place the relic on the keepsake shelf. The verb taught is "ride the lift". |
| 5 | Bay (L2) | The plotter is on the bay's back wall; any sheets decoded before L2 are drawn as it wakes. |
| 6 | Over the chasm | The Hover-Jump tube is found at the chasm's near lip (M3-21) and decoded at the reader. |
| 8 | **The survey chart** (L3) | L3 lights the light table and the chart pen. On the next ride up, the chart inks everything visited in reach (M3-07). |

The dock's evening (P6) gains the lookout's sweep: on parking, the wide shot's places blink once in turn, then the
tower raises the next pillar. Those blinks are what the chart draws as "seen" (`MapKnowledge.MarkSeen`).

## 8. Home, decluttered
- **Removed from outside:** the cable-lift frame and its jam props, the deck guardrail lookout, the memory shelf
  cabinet, Kenji's bench, and the loose stands, lectern and crates near the lander.
- **Three masses** with ≥ 4 m of open, lit ground between them (V3 pools); ≤ 6 small silhouettes in the owner's chase
  frame (V5).
- **Halo rule (World):** a warm point's halo is never larger on screen than the object carrying it within 60 m;
  beyond 60 m the beacon minimum angular size applies (L6).

## 9. Impact on built things
| Built | Fate |
|---|---|
| Cable lift frame, jammed platform, snapped cable props (art) | removed from the base builder; the platform and jam parts move under the belly where shapes fit. Kept prefabs keep their GUIDs; deleted outputs go through the builder, never hand-deleted `.meta`. |
| `Gameplay/Lift` (`LiftCycle`, `LiftStation`, `LiftTuning`, `LiftSaveData`) | kept and re-aimed: dormant → restored by a stitch → ride. The Lookout phase becomes Inside (turntable facing, back to descend). The power gate moves to Home. |
| Rover's lift slice (ride pose, deck lookout shot) | the ride pose stays; the lookout shot is replaced by the crane path and the ring-rail camera. |
| M3-07's map look (generic, landed) | its trigger becomes "Look at the survey chart". The map slice's leftovers move here (§11). |
| Memory shelf (`place.shelf`) and Kenji's board (`place.board`) | replaced by `place.lab` ("The crew's lab: finds become know-how."). The shelf's deposit logic moves inside. |
| Blueprints and the plotter (M3-16 P7) | a tube now goes to the reader; `BlueprintDelivered` is published when the reader takes it; decoding publishes `TechNodeChanged(Decoded)` (M3-21); the plotter draws on the hand-off. |
| Golden-path Playthrough | step 4 adds the lift's restore and the ride with the first relic; tubes go through the reader; step 8 is the chart. |
| Saves | content floor raised to this release (the lander's places changed), as in M3-16. |

## 10. Contracts (Core)
Landed by the Director (2026-10-11):
- `ITechTree`, `TechNodeId`, `TechNodeState`, `TechNodeChanged`, `IRecoveryProgress`, `RecoveryProgressChanged`
  (M3-21);
- `ILabInterior` (registered by Gameplay): `IsInside`, `Facing` (`LabStation`: Chart, Reader, Board, Keepsakes);
- `Core/Events/LabEvents.cs`: `LabEntered(facing)` / `LabLeft` (Rover switches cameras, UI hides the HUD, Audio swaps
  to the room tone); `LabStationFaced(station)` on each turntable stop; `FindDecoded(kitId)` when the reader finishes;
  `DecodeHandOff(kitId, seconds)` for the lamp pulse (World and Art light it; the plotter starts on its arrival).

Made by the slices (authorised Core edits, listed in their reports):
- lab-systems: `BasePowerStage.Lift` → `Chart` (same ordinal), with every usage in all domains (a mechanical rename);
  `LiftRideChanged.AtLookout` → `Inside`; `LiftCue` gains `HatchOpened` and `HatchClosed`.
- lab-build: `HomeLight` entries: `LiftLamps` becomes the belly lamp and hatch rim; the lab's lamps and the light
  table's glow get entries if World needs them.
- Place ids (lab-systems): `place.lab` replaces `place.shelf` and `place.board`; `place.lift`'s line becomes "The
  crew's lift: up into the lab." Localization keys follow (en + vi).

**Art → Gameplay/Rover node names** (the lab-build slice publishes the exact list in `docs/ARCHITECTURE.md`, a new
"Contract: the lab (M3-19)" section, before the systems slice wires it): `BellyLift/Platform`, `BellyLift/HatchLeafL`,
`BellyLift/HatchLeafR`, `BellyLift/CableJam`, `Lab/RingRail`, `Lab/Station_Chart`, `Lab/Station_Reader/Slot`,
`Lab/Station_Reader/Reel`, `Lab/Station_Board`, `Lab/Station_Board/RecoveryPanel`, `Lab/Station_Keepsakes/Slot_<n>`,
`Lab/PortholeLamps/Lamp_<n>`, `LightTable`, `LightTableGlow`, `ChartPen`, and a `HandOffPath` polyline from the reader
down the leg to the bay.

## 11. Split (two feature-slice sessions)
| Slice | Delivers |
|---|---|
| **lab-build** (art + world) | the habitat lander (§3) with both states; the belly lift's platform, hatch and jam parts; the cabin interior with the four stations, the ring rail, the porthole lamp ring; the plotter on the bay's back wall; the declutter (§8); the ARCHITECTURE node contract; the light table, its glow and the chart pen driven from `MapLookStarted`/`MapLookEnded`/`MapInkCued` (the map slice's art leftovers); the board's sheets and lamps and the porthole's lamps driven from `TechNodeChanged`, the recovery panel's lamps from `RecoveryProgressChanged`; World: the belly lamp and hatch rim per stage, the lab's lamps and the table glow through `IHomeLights`, the hand-off pulse's light along the ground cable, the halo rule, warm pools around the three masses |
| **lab-systems** (gameplay + rover + ui + audio, and the Core renames above) | the lift re-aimed (Home gate, stitch restore, Inside phase, the 180° turn), the turntable and its auto-turn, `ILabInterior` and the events, the reader's decode and the hand-off, deposit inside, place ids, the revised steps 4/5/6/8 and the Playthrough, the save floor, the map trigger from the chart and P6's `MarkSeen` (the map slice's gameplay leftovers); Rover: the crane path, the ring-rail camera, the ride pose, the look toward the map (map leftover); UI: HUD hidden inside, first-time turntable prompts ("Turn", "Look", "Down"), the lab's place lines; Audio: winch hum, hatch, turntable detent ticks in key, room tone, the reader's reel and decode chime, the map's pencil hiss, pen scratch and table hum (map leftovers) |

## 12. Acceptance
- **From the owner's two angles** (behind 07 at the base; the lander from the ladder side), a first-time player can
  say what the lift is for without text.
- **Ladder-side frame:** 07 fits under the belly with visible headroom; the lander is clearly taller and wider than
  07; the tower is still the tallest thing at home.
- **Behind-07 frame:** three masses with lit ground between them, ≤ 6 small silhouettes, no glass cabinet or bench.
- **Golden path:** the first relic rides up and onto the keepsake shelf at step 4; the Hover-Jump tube is decoded at
  the reader and the pulse reaches the bay; at L3 the chart wakes.
- **Inside:** the camera never clips; every station is reachable with left/right only; Back always brings 07 down
  facing out.
- **Ruling 14:** 07 never presses, carries or climbs. The platform, hatch, turntable and reader are machines; the beam
  feeds and places.
- 60 fps and zero per-frame GC inside the lab.
