# Lofi Lunar — Game Design v2 (depth & reasons to return)

> Owner: Director (game designer). This document defines what the full game is beyond the vertical slice.
> `docs/VISION.md` sets the feel and the rulings; this sets the structure. Music is seasoning, never the main hook:
> every reason to return below works with the sound off.

## The fantasy
**A tired old rover restores a forgotten lunar outpost, brings its lost family of machines back to life, and
finally gets an answer from Earth.** The player is not saving the world — they are making one small place warm again.

## Why people come back (in order of strength)
1. **A home that visibly changes every session.** Each outing ends with something new at the base: a relic on the
   shelf, a repaired friend waiting by the porch, a lamp that wasn't lit before, a sprouting plant. Coming home is the
   payoff, and the base is never the same twice.
2. **Friends to bring back.** The outpost's other machines lie broken across the moon. Repairing them is the spine of
   the game: each friend has a personality, a small idle life at the base, greets 07 on arrival, and grants a gentle
   ability. Players return for *them*.
3. **A mystery told in fragments.** Why is 07 alone? Relic memories, crew logs and radio fragments slowly reveal the
   crew who left "until after the storm" — and what happened to them. Each answer reveals a better question.
4. **A moon that opens up.** Rover upgrades unlock new terrain (metroidvania-lite): every upgrade turns a "can't
   reach that" into "now I can", and the map is seeded with visible-but-unreachable promises (glints on ledges, a
   light deep in a canyon, a dark crater).
5. **Gentle surprises between sessions.** The moon drifts while you're away — a meteor shower leaves fresh glinting
   scrap, a new faint signal appears, a moon-weather event is "on tonight". Never punishing, never time-limited, no
   streaks, no fear of missing out: it just makes "let me check on the moon" a pleasant habit.
6. **Self-expression.** Paint, stickers and little accessories for 07 (a knitted scarf, a flag on the antenna), base
   decorations placed on snap points, and a photo mode. Cozy players love to make things theirs.
7. **Completion with clear progress.** Museum sets, friend roster, cassette shelf, map discovery — every collection
   shows progress at the base, never in a spreadsheet menu.

## The loops
| Scale | Loop | Status |
|---|---|---|
| Seconds | drive → spot a wreck → salvage with the beam (salvage melody) → sonar → the site's relic → home | M2 ✅, salvage replaces the scrap field in M3-13 |
| Session (15–30 min) | pick a wreck or signal on the horizon → expedition → bring materials and a memory home → craft at Kenji's bench → 07 and the base visibly change | M2 slice, deepened in M3 |
| Hours | restore the outpost: radio tower · workshop (rover upgrades) · museum sets · friends · biodome | M3–M4 |
| Arc | climb The Peak → repair the great dish → broadcast → Earth answers | M4 |
| After | free roam, completion, cosmetics, photo mode, moon-weather events | M4–M5 |

## Story (told, never explained)
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
| **Tilly** — a tiny hover-drone | the scientist's surveyor | a crater on the basin floor (M3 first) | 3 parts nearby | flits around the lander, follows 07 on short trips | spots glints → scrap and relic signals within range show on 07's sonar without pinging |
| **Moss** — a slow garden bot | the botanist's gardener | the shadowed crater (needs headlamp+) | parts + a seed | tends the biodome, hums | grows the biodome; gifts small decorations |
| **Bell** — an old radio cabinet on legs | the radio operator's DJ machine | a canyon shelf (needs Hover-Jump) | parts + a cassette | runs the station, "announces" tracks via the ticker | unlocks the radio station dial (choose playlists/stations) |
| **Atlas** — a big, gentle hauler | the engineer's crane | the rim terraces (needs Magnetic Treads) | heavy parts | rests by the workshop | carries relics home automatically from marked spots; needed to lift the great dish |
Each friend: a small set of idle behaviours, a greeting when 07 returns, a reaction to new relics, and a sleep spot.

### Workshop — rover upgrades (traversal + comfort), bought with scrap + parts
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

- **Install moment.** Buying an upgrade at Kenji's bench plays a short eased camera moment: sparks, the part settling
  onto 07, then a small proud pose (head lift, eye brighter, an antenna wiggle). It is the payoff, never skippable
  clutter, and about 3 s long.
- **Home shows it too.** The tower's three stages, Bell's corner, the cassette rack, the lit relay masts, the warmth
  spreading across the basin (M3-06) and decorations (M3-09). Every station upgrade must change home's silhouette or
  its lights.
- **Rule.** An upgrade that doesn't change what the player sees isn't finished (VISION ruling 11).

### Station reach — the relay network (the second progression axis)
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
- **Outside reach.** Everything still works and nothing bad happens. It is only quieter, with static on the radio and
  no map. The frontier feels lonely; lighting it is the reward. **There is never a battery, fuel or range limit that
  forces 07 home.** That would be a hidden timer and would break rulings 1 and 7.
- **Two axes.** Rover upgrades answer "can I get there?" (terrain). The station's reach answers "is it home yet?"
  (comfort, knowledge, travel). Every region needs both: a rover upgrade to enter it, and a relay to make it home.
- **Long-term scrap sink.** Masts cost escalating scrap, which keeps collecting meaningful after the workshop is
  bought out.
- **The arc.** The chain leads to The Peak. The final broadcast needs the network linked from the base to the great
  dish, so every mast lit is a visible step toward the ending.

### Regions (one basin today → five areas)
| Region | Mood | Gate | Holds |
|---|---|---|---|
| Crater basin (home) | lavender dunes, the base | — | onboarding relics, Tilly |
| Whispering Canyon | narrow, echoing, blue | Hover-Jump | Bell, a crew log cache |
| Rim Terraces | high, windless, Earth-view | Magnetic Treads | Atlas, cassette shelf finds |
| The Shadowed Crater | dark, bioluminescent | Warm Headlamp | Moss, the seed |
| The Peak | the great dish | all + Atlas | the ending |

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
  - a cable lift (a winch platform) beside the lander's human ladder, up to the hatch deck, used by M3-07 to reach
    the hand-drawn map inside;
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

### Base life & decoration
The base gains lights, plants, friends' corners and player-placed decorations on snap points (lanterns, pennants,
a little bench facing Earth). Base "warmth" is visible from far away — the glow grows across the basin.

### 07 customisation & photo mode
Paint schemes, stickers and accessories found or earned (sets, events, friends' gifts); photo mode with gentle
framing helpers and filters.

## Scope guard
- Keep the slice's feel bar for every new system; a feature that can't pass the feel checklist is cut, not shipped.
- Target length: main arc 5–7 hours, completion 10–12 hours.
- No combat, timers, fail states or punishing mechanics will ever be added to create "depth".
