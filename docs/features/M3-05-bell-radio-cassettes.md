# M3-05 — Bell, the radio dial and cassettes

> Owner: Director (design). The second friend (`docs/DESIGN.md` "Friends"), the first one behind an ability gate. Story:
> `docs/STORY.md` (Bell is Ro Amadi's radio cabinet on legs). Rulings in `docs/VISION.md` apply: nothing can be lost,
> failed or timed, and music is seasoning — every reason to care below works with the sound off.

## Why this matters for the player (with or without sound)
- **A reason to go into the canyon.** Hover-Jump opens it, Bell gives the trip a destination, and her parts pull the
  player into each alcove. The canyon is a place to go, not just a shortcut.
- **A second resident at the base.** Bell takes a corner beside the radio tower, with its own lights and moves, so
  the base visibly changes again (DESIGN pillar 1).
- **Bell's signals.** The ongoing gift. Once home, Bell points the player at one thing they haven't found yet: a
  cassette, a crew log or a relic. "What did Bell pick up?" becomes the question that opens a session, and it guides
  without a quest log.
- **Cassettes.** A visible collection. Each tape on Bell's shelf comes with a short note from Ro, so the story
  advances even for someone who never turns the music up.

## Player experience (the beats)
1. **The light in the canyon.** The warm light deep in Whispering Canyon (M3-04) is Bell's dial lamp, flickering.
   07 already saw it from the base.
2. **Meeting.**
   - At `canyon.terminus` Bell is tipped against the wall, lid open and dial dark.
   - Next to her is the crew log cache: a battered tin box holding Ro's first log and a cassette labelled
     "Lumen After Dark, Vol. 1".
   - Three amber parts glint in the canyon: a tuning knob, a speaker cone and a valve. They sit at the alcove anchors
     (`canyon.alcove_0..2`), one per alcove. If the world has fewer alcoves, the leftovers go along the driving line
     at least 40 m apart.
   - Bell's three part lamps read 0/3, plus a fourth tape lamp.
3. **Gathering.**
   - Parts are collected exactly as Tilly's are.
   - The cassette is the fourth "part". Collecting it plays a short tape-click and fills the tape lamp.
   - The log card opens once, when the cache is first reached.
4. **Repair.**
   - It runs Tilly's flow: hold Interact near Bell, with 07's beam stitching.
   - Then the beat that is Bell's own: 07 slides the tape in, the dial glows, a needle sweeps, and she stands up on her
     four spindly legs.
   - She plays the first three notes of the station jingle and does a little two-step.
   - Card: Ro's repair log, "Bell".
5. **Home without escort.**
   - Bell waddles off toward `canyon.exit`. No escort mission, no waiting.
   - She is never seen teleporting. Once she is out of 07's view and ≥ 60 m away, she is placed at home.
   - The next time 07 arrives at the base, she is already set up in her corner beside the radio tower and plays the
     jingle in greeting. The ticker says: "Bell got home before you. She says the porch light was on."
6. **Life at the base.**
   - Bell sways gently in time with whatever plays and taps a foot.
   - She turns her dial-face to watch 07 park.
   - At night she dims and "sleeps" with the needle resting.
   - She reacts to new relics on the shelf with a happy station-switch crackle.

## Bell's gifts
### 1. The radio dial (at Bell's corner, Interact)
A small diegetic panel with a chunky dial and three detents:
- **Lumen After Dark**: Ro's show. Shuffles every track 07 owns (the 6 base tracks plus collected tapes). This is the
  default.
- **Tape Deck**: pick one collected cassette, which plays on repeat. Its liner note shows on the card.
- **Quiet Hours**: music off. Only the moon's ambience plays, plus the occasional ticker line. Some players want
  silence, and that is a choice to respect.

Turning the dial is pure feel: a detented click, a static swish between stations and an eased needle. The choice is
saved. Before Bell is repaired the dial does not exist, and the radio behaves exactly as it does today.

### 2. Bell's signals (the depth hook)
- **When a signal is picked.** At each session start, and each time the current signal's target is found, Bell picks
  one undiscovered thing to point at, in this priority:
  1. the nearest reachable cassette;
  2. a crew log;
  3. a relic signal not yet answered.
- **Reachable** means within the regions 07's current abilities can reach. It never points behind a gate the player
  can't pass yet.
- **What the player sees.**
  - A soft amber pillar on the horizon, the same visual language as sonar pillars (design ruling 3) but warmer and
    persistent.
  - A ticker line: "Bell's picking something up… bearing 140."
  - When the target is found, Bell's ticker says something kind and the pillar fades.
- **No pressure.** It never nags, never stacks markers and never expires. If nothing is left to find, Bell just plays
  music.

### 3. The ticker
Bell runs the radio's text ticker: a single soft line along the bottom of the HUD that eases in for a few seconds and
then out. It carries:
- "Now playing — <title>" on each track change (only while Lumen After Dark plays and only once per track per
  session);
- the signal lines above;
- the story's radio fragments (`docs/STORY.md`), released one at a time as the tower's signal grows.

It never covers prompts and is never shown during a dig or a card.

## Cassettes
- **Total 8 in the full game, 3 in M3-05:**
  - Vol. 1 with Bell (terminus cache).
  - One in the basin, its site found through Bell's signal: half-buried by a small crater rim, so a dig is needed.
  - One in the canyon on the glinting ledge (`canyon.ledge`), reached by a short Hover-Jump. The ledge relic moves to
    a later region.
- **Pickup.** A tape is a pickup (layer Pickup), not a relic: no tether and no museum shelf. It is collected by driving
  through it, with a tape-click and a little spin.
- **Cassette shelf.** It lives at Bell's corner. Slots fill in order of collection with chunky low-poly tapes in
  distinct label colours, so progress shows at the base (DESIGN pillar 7).
- **Each tape** is a new radio track from the music box (generated, D major pentatonic family, lofi) plus a liner-note
  card from Ro (1–2 sentences, en + vi). Text keys:
  - `cassette.<id>.title`
  - `cassette.<id>.note`
- **M3-05 tapes and notes (English source):**

| id | Title | Ro's note |
|---|---|---|
| after_dark_1 | Lumen After Dark, Vol. 1 | First night show. Audience: three crew, one rover, one basil plant. The basil had requests. |
| dust_and_honey | Dust & Honey | Recorded with the mic taped to the airlock. If you hear a thump, that's Kenji. |
| slow_orbit | Slow Orbit | For the nights Earth looks close enough to walk to. It isn't. I checked. |

## Logs in this feature
| key | Text (English source) |
|---|---|
| `log.ro_1` | "Night one of Lumen After Dark. Three listeners and a rover who rolls closer when I play the slow ones. Best audience I've ever had." |
| `log.ro_bell` | "Built Bell out of a dead receiver and four camera legs so the station keeps playing while I sleep. She dances. I didn't build that part." |

## Systems
- **Friends.** Bell is a `FriendDefinition` like Tilly. The new parts are these:
  - A cassette requirement: the friend framework gains a required-item list next to parts, so Moss's seed is the
    same thing later.
  - A home placement that is not the lander. A `homeSocket` on the radio tower: `BellCorner`.
  - A "walks home unseen" travel mode instead of following.
- **Activity.** Bell's activity values reuse `FriendActivity` (Home, Napping, ...).
- **Abilities.** `RoverAbility` is unchanged. Bell's gift is a station feature, not a rover ability.
- **Save.** The following are saved, deterministic and never lost:
  - collected cassettes;
  - the dial station;
  - the Tape Deck's chosen tape;
  - Bell's state;
  - the current signal target.
- **Events (Core, `GameplayEvents`/`FriendEvents`).**
  - `CassetteCollected(id, position, collectedCount, total)`
  - `RadioStationChanged(station, tapeId)`
  - `SignalPicked(targetKind, position)`
  - `SignalFound(targetKind, position)`
  - `TickerLine(key)`. The UI looks the text up. Track titles use `track.<id>.title` keys.

  Audio and music react to these; nothing reaches across domains.
- **Radio contract.** Audio owns playback. Gameplay owns what is owned and selected. Gameplay publishes the selection
  and the owned-tape set through a small Core contract, `IRadioProgram` (owned track ids, current station, chosen tape;
  read-only to Audio), so the playlist becomes "base tracks + owned tapes" without Audio knowing about pickups.
- **Placement.**
  - The cassette and part sites use `IWorldAnchors` (M3-04) plus `ITerrainQuery`.
  - The basin cassette is planned by a deterministic site planner outside the base's 60 m and away from relic sites.

## Content & domain split
| Box | Delivers |
|---|---|
| art | Bell (rig: Body, Lid, DialFace with Needle, DialLamp glow renderer, Leg_FL/FR/RL/RR, Speaker, PartLamp_0..3), broken pose variant, 3 part pickups + cassette pickup (amber accents), log cache tin, cassette shelf with 8 slots, BellCorner socket on the radio tower |
| gameplay | Bell's definition, required items, unseen walk home, base life, dial interaction, signals, cassettes (pickups, planner, save, shelf filling), `IRadioProgram`, events, contributor, playthrough extension |
| music | 3 new tracks (one per tape, distinct moods: playful show opener, warm airlock jam, slow orbit lullaby), station jingle (3 + 4 notes), rendered as OGG with playlist metadata |
| audio | playlist from `IRadioProgram`, station switching with static swish, Quiet Hours ambience, Bell's voice (needle sweeps, jingle, leg taps, crackle reactions), tape-click pickup, signal pillar shimmer |
| ui | dial panel, ticker line, liner-note card, log cards, cassette count in the pause menu's collection page, keys en + vi |
| rover | 07 glances at the signal pillar when it appears; a happy wiggle when a tape is collected |

## Acceptance
- A PlayMode playthrough extends the golden path: leap the chasm, gather Bell's 3 parts and the tape, repair, return
  home, find Bell there, turn the dial to each station, follow Bell's signal to the basin cassette and collect it.
- With the sound muted the whole feature still reads: signals, ticker, shelf, cards.
- Feel checklist in VISION.md passes; captures reviewed by the Director in the real scene.
