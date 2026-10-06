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
| Seconds | drive → glints → scrap melody → sonar → dig → tow | M2 ✅ |
| Session (15–30 min) | pick a signal on the horizon → expedition → bring something home → spend → base visibly changes | M2 slice, deepened in M3 |
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
