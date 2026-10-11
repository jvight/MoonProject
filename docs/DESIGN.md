# Lofi Lunar — Game Design v2 (depth & reasons to return)

> Owner: Director (game designer). This document defines what the full game is beyond the vertical slice.
> `docs/VISION.md` sets the feel and the rulings; this sets the structure. Music is seasoning, never the main hook:
> every reason to return below works with the sound off.
>
> **Since 2026-10-11 the detailed design is `docs/design/gdd-v3.md`** (approved by the owner): progression first, the
> battery as range, four main recovery targets with everything else free, structures on build pads, the lander as the
> crew's lab, and a 32-node tech tree. Where this file and GDD v3 differ, GDD v3 wins. The sections below were updated
> to it on 2026-10-11 (GDD v3 §10); older sections not named there stand as written.

## The fantasy
**A tired old rover rebuilds itself and a forgotten lunar outpost, pushes its battery's reach a little farther every
night, and finally gets an answer from Earth.** The player is not saving the world — they are making one small place
warm again, and one small rover capable again.

## Why people come back (in order of strength)
1. **07 and home grow.** Every trip leaves 07 or home more capable: a kit fitted in Kenji's Rover Bay, a bigger cell, a
   tower level, a mast lit, later a structure on a pad. Each shows (ruling 11), so a screenshot from hour five looks
   clearly different from minute one.
2. **The moon opens with range and tools.** The battery is 07's range (ruling 15). Masts, pylons and cell packs push the
   frontier out, and kits open gated regions (metroidvania-lite): every upgrade turns "can't reach that" into "now I
   can", and the map is seeded with visible-but-unreachable promises (glints on ledges, a light deep in a canyon, a
   dark crater, the Peak's red light).
3. **The freedom to roam.** Only four main recovery targets are directed (Power, Link, Dish, Answer). Sites, finds,
   friends, tapes and secrets are found in any order, and the two mid regions can be done in either order.
4. **Friends to bring back.** The outpost's other machines lie broken across the moon. Each has a personality, a small
   idle life at the base, greets 07 on arrival, and helps in ways that save time and battery (Tilly leads a crawl
   home with her lamp).
5. **The crew's story, as seasoning.** Masts' letters, logs and keepsakes slowly tell why 07 is alone and what
   happened to the crew. They lie in site hearts beside the tech finds; they are never the reason a trip is needed.
6. **Gentle surprises between sessions.** The moon drifts while you're away — a meteor shower leaves fresh glinting
   scrap, a new faint signal appears, a moon-weather event is "on tonight". Never punishing, never time-limited, no
   streaks, no fear of missing out.
7. **Self-expression and completion.** Paint, stickers and accessories for 07, a photo mode, and collections that show
   their progress at the base, never in a spreadsheet menu.

## The loops
| Scale | Loop | Status |
|---|---|---|
| Seconds | drive → spot a wreck → salvage with the beam (salvage melody) → sonar → the site's relic → home | M2 ✅, salvage replaces the scrap field in M3-13 |
| Session (15–30 min) | pick a wreck or signal on the horizon → expedition → bring materials and a memory home → craft in Kenji's Rover Bay → 07 and the base visibly change | M2 slice, deepened in M3 |
| Hours | restore the outpost: radio tower · workshop (rover upgrades) · museum sets · friends · biodome | M3–M4 |
| Arc | the four main recovery targets: Power → Link → Dish → Answer (GDD v3 §8, spec M3-21) | 0.5–0.8 |
| After | free roam, completion, cosmetics, photo mode, moon-weather events | M4–M5 |

## Story (told, never explained)
> **Superseded 2026-10-11 by GDD v3 §13 "the long silence"** (owner): humanity on Earth died out long ago; every
> signal is an old recording or a machine; the ending is dark and open (a younger rover, 08, arrives with Ro's last
> recording). The paragraph below is the original pitch, kept for history.

The outpost **Lumen Station** was a tiny radio-relay and research base. Its four crew members left for Earth "until
after the storm" and never came back. 07 kept the radio playing and waited. The relics are the crew's personal
things, scattered by the storm. Each crew member has a set of relics, a broken companion machine they cared for, and
a handful of short logs. The radio slowly picks up fragments — old broadcasts, a weather report, a voice-less text
"ticker" of messages addressed to the station. The final broadcast from The Peak reaches one of the crew, now old,
and a small light on Earth's night side blinks back. Bittersweet, warm, hopeful: *you were never forgotten.*
(No voice acting: text cards, radio tickers, and environmental storytelling only.)

## Systems for the full game

### Friends (the spine) — 4 companions
| Friend | Who they were | Found | Repair needs | At the base | Gift to 07 |
|---|---|---|---|---|---|
| **Tilly** — a tiny hover-drone | Ines's surveyor (the geologist) | a crater on the basin floor (M3 first) | 3 parts nearby | flits around the lander, follows 07 on short trips | spots glints → scrap and relic signals within range show on 07's sonar without pinging |
| **Moss** — a slow garden bot | the botanist's gardener | the shadowed crater (needs headlamp+) | parts + a seed | tends the biodome, hums | grows the biodome; gifts small decorations |
| **Bell** — an old radio cabinet on legs | the radio operator's DJ machine | the canyon terminus (needs Hover-Jump) | parts + a cassette | runs the station, "announces" tracks via the ticker | unlocks the radio station dial (choose playlists/stations) |
| **Atlas** — a big, gentle hauler | the engineer's crane | the rim terraces (needs Magnetic Treads) | heavy parts | rests by the workshop | carries only heavy things (the great dish, rockfalls, a crew window's part); memories always ride home with 07 |
Each friend: a small set of idle behaviours, a greeting when 07 returns, a reaction to new relics, and a sleep spot.

### How 07 does things (VISION ruling 14: 07 has no hands)
| Action | How it physically happens on screen |
|---|---|
| Collect salvage, bits, parts, tapes | the beam cuts or pulls; the piece flies to the cargo socket |
| Dig a relic | the beam lifts dust and the relic rises |
| Carry a relic | the beam tows it, or it rides in the Cargo Cradle |
| Place a relic on the shelf | the beam lifts it into its slot |
| Repair a friend or a relay mast | the beam stitches; parts slide in along the beam |
| Craft kit | 07 drives into **Kenji's Rover Bay**. Its beam feeds materials into the bay's hopper, and the bay's gantry arms fit the part onto 07 |
| Upgrade the radio tower | 07 parks at the tower's rover-height service port, feeds the hopper, and its beam stitches the new section while the tower raises it |
| Turn Bell's dial | 07's beam taps the dial's knob; Bell turns it with a wiggle |
| Go into the lab | the belly lift under the lander carries 07 up through a floor hatch onto a turntable (M3-19) |
| Decode a find | 07's beam feeds the tube into the lab's reader slot at lens height (M3-19) |
| Charge | park on the dock at home or on a lit mast's 4 m pad; the cells fill by themselves (M3-20) |
| Build a structure (0.6) | park at a build pad, the beam feeds the sealed crate's hopper, the frame unfolds and 07 stitches it (M3-22) |

### Workshop — rover upgrades (traversal + comfort), crafted from materials in Kenji's Rover Bay
Hover-Jump (charged leap over chasms) · Magnetic Treads (climb steep crater walls) · Cargo Cradle (carry 3 relics,
never fall out) · Wider Sonar · Warm Headlamp (lights the shadowed crater) · Boost Coils (faster on flats, joyful).
Every traversal upgrade must open at least one region and one visible secret near the base.
Gates hold only while each upgrade stays inside its lane. Today 07 climbs at most ~45°. The crater rim (and so the
canyon gate) becomes climbable at ~50°, with a ~27 m drop into the canyon terminus. So Magnetic Treads must climb
only surfaces marked as climbable walls (the rim terraces), never raise the general slope limit.

### Visible progression — you can see how far you've come (owner priority, 2026-10-08)
Every upgrade to 07 or to home changes what you see, so a screenshot from hour five looks clearly different from minute
one.
- **07's arc goes from tired to cared-for, not from cute to menacing.** 07 starts with its "signs of solitude" (VISION):
  - a solar wing with a missing cell;
  - a bent antenna;
  - a patched panel;
  - one mismatched replacement wheel;
  - a half-lidded eye.

  Progression heals those signs one by one and adds sturdy, well-loved expedition kit. By the end 07 looks rugged and
  capable, a rover someone takes care of again, while keeping its gentle face. It is never militarised and never
  spiky.
- **One visible kit piece per upgrade,** readable from the default chase camera (rear three-quarter view) and at 30 m:

| Upgrade | What changes on 07 |
|---|---|
| Hover-Jump | spring coils under the belly, glowing when charged (done) |
| Magnetic Treads | all six wheels become bigger, chunkier treaded wheels, so the mismatched wheel is finally matched and 07 sits higher |
| Cargo Cradle | a rear rack basket with straps; carried relics ride in it |
| Wide Sonar | a larger dish and antenna array on the back, and the bent antenna is straightened |
| Warm Headlamp | a caged lamp bar across the front; the light pool is visibly wider |
| Boost Coils | twin capacitor drums (the "big batteries") on the flanks that glow while boosting |
| Friends' gifts | the solar wing's missing cell is replaced (Tilly), the patch gets a painted flower (Moss), the "07" is repainted (Ro and Bell), and a tow hook appears (Atlas) |

- **Install moment.** Crafting in Kenji's Rover Bay plays a short eased camera moment: the bay's gantry arms lower the part,
  sparks, the part settling
  onto 07, then a small proud pose (head lift, eye brighter, an antenna wiggle). It is the payoff, never skippable
  clutter, and about 3 s long.
- **Home shows it too.** The tower's three stages, Bell's corner, the cassette rack, the lit relay masts, the warmth
  spreading across the basin (M3-06) and decorations (M3-09). Every station upgrade must change home's silhouette or
  its lights.
- **Rule.** An upgrade that doesn't change what the player sees isn't finished (VISION ruling 11).

### Station reach — the relay network and the battery (the second progression axis)
Lumen Station was a radio-relay outpost, and the storm left its chain of relay masts dark across the moon. Restoring
them is how the station's reach grows, so that "home" spreads outward step by step.
- **Reach.** The radio tower (L1→L3) sets the home circle. Each repaired relay mast (scrap + one relay part found
  nearby) lights up and adds its own circle. Masts stand at fixed high points (world anchors `relay.<n>`): a mound, a
  rim shoulder, the canyon mouth, the foot of The Peak. Lit masts glow warm and are visible from the base, so the
  player watches a chain of lights spread across the basin.
- **Inside reach:**
  - the radio is clear;
  - sonar answers carry farther;
  - the map (M3-07) draws itself;
  - friends work fully;
  - **radio-hop**: at any lit mast, Interact to hop to the base or to any other lit mast. A calm static-and-tune
    transition, no loading drama.
- **Range is 07's battery** (ruling 15, spec M3-20). Every lit mast has a 4 m charge pad and every built pylon (0.6+)
  is a charger, so lighting the network and building pylons is how range grows. Cell packs and efficient hubs grow it
  on 07's side. Outside reach 07 runs on its battery; it is quieter there, with static on the radio and no inked map.
  At empty 07 never strands: it crawls home slowly, with Tilly's lamp ahead.
- **Two axes.** Rover upgrades answer "can I get there?" (terrain and range). The station's reach answers "is it home
  yet?" (charging, comfort, knowledge, travel). Every region needs both: a rover upgrade to enter it, and a mast or
  pylon to make it home.
- **Long-term scrap sink.** Masts cost escalating scrap, and from 0.6 so do repeated pylons.
- **The arc.** The chain leads to The Peak. The Link target (M3-21) is a chain of lit masts from home to the Peak's
  foot, by either mid region's route; the final broadcast needs it, so every mast lit is a visible step toward the
  ending.

### Regions (one basin today, five areas in the end; GDD v3 §5)
Gates are real and seen first, with a one-way exit (ruling 10). Order is the player's: within a region anything goes,
and the two mid regions can be done in either order.

| Region | Mood | Gate | Masts and pylon pads | Tier and friend |
|---|---|---|---|---|
| Crater basin (home) | lavender dunes, the base | range: masts and Cell Pack II (the Kestrel is the first range gate) | mound, shoulder, canyon mouth; a pylon pad at Kestrel's rim (0.6) | T1; Tilly |
| Whispering Canyon | narrow, echoing, blue | Hover-Jump | the terminus | T1; Bell |
| Rim Terraces | high, windless, Earth-view | Magnetic Treads | a terrace mast; 2 pylon pads on the ledges | Alloy, Circuitry; Atlas |
| The Shadowed Crater | dark, bioluminescent | Floodlights | a crater mast; 2 pylon pads on the floor | Crystal, Alloy, Circuitry; Moss |
| The Peak road | the great dish | Anchor Line + Long Leap + Atlas (the rockfall) | the Peak's foot mast; 2 road-camp pylon pads | Superconductor; the dish |

### Collections
- **Museum**: 24 relics in 4 crew sets of 6; completing a set lights that crew member's window in the lander and
  unlocks their final log.
- **Cassettes**: 8 tapes = 8 new radio tracks + a liner-note card each (seasoning, not the hook).
- **Map**: points of interest light up on a hand-drawn map pinned in the lander once visited.

### Moon weather & between-session drift (no FOMO)
On each new session: a gentle chance of a *meteor shower* (fresh glints), a *new signal* (a relic or part appears
where none was), or an *event*: **Eclipse** (bioluminescent paths reveal secrets), **Solar Flare** (aurora, 07
overcharged and fast), **Earthrise** (a calm photo moment). Events last the session; nothing expires.

### A base built for 07, waking from decades of neglect (owner priority, 2026-10-08)
- **Rover-first architecture.** The crew built Lumen Station around a rover:
  - a belly lift (a round winch platform) under the lander, up through a floor hatch into the crew's lab, where the
    reader, the tech board and Ines's survey chart are (M3-19);
  - ramps onto every pad;
  - 07's charging and docking cradle instead of a doormat;
  - rover-height junction boxes and dials.

  The crew's ladder stays as a remnant: 07 has never climbed it.
- **Abandoned at first.** At the start everything is dusty, faded and rusted:
  - the lander's paint is sun-bleached and rust streaks run from its rivets;
  - dust drifts lean against the legs;
  - a cable hangs slack and the "HOME" sign tilts;
  - the lift is jammed halfway.
- **Restored over time.** Each restoration step (tower levels, friends home, relays lit, the workshop) visibly
  cleans one part of home: dust caps gone, a fresh paint patch, the sign straightened, the lift running, lights on.
  By the end the base looks lived-in and loved, still old, never new.
- **The same arc on 07.** It starts with rust spots, a faded stripe and dust on its top faces; repairs and gifts
  clean and repaint it piece by piece (see "Visible progression").

### Base life, structures and decoration
- **Structures on build pads (0.6+, spec M3-22).** The player builds operational structures, but only on predefined
  pads: a Field Charge Pylon, the Fabricator, the Bay Wing, later the Solar Array, the Materials Store and the Drone
  Hangar. A pad is a rover-height plinth with a sealed crate Kenji pre-packed; 07 feeds it and stitches the frame.
  Never free placement. Home has 5 pads around its three masses (lander, bay, tower), each with lit ground around it.
- **Decoration.** The base also gains lights, plants, friends' corners and decorations on snap points (lanterns,
  pennants, a little bench facing Earth). Base "warmth" is visible from far away — the glow grows across the basin.

### 07 customisation & photo mode
Paint schemes, stickers and accessories found or earned (sets, events, friends' gifts); photo mode with gentle
framing helpers and filters.

## Scope guard
- Keep the slice's feel bar for every new system; a feature that can't pass the feel checklist is cut, not shipped.
- Target length: main arc 5–7 hours, completion 10–12 hours.
- No combat, timers, fail states or punishing mechanics will ever be added to create "depth".
