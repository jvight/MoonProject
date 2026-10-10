# M3-07: The crew's map

> **Approved by the Director on 2026-10-11** (drafted by the game design and story & flow sessions). Step 3, Ines's
> pencil notes beyond the rim, stays: it is 0.5's end-of-phase hook toward the 0.6+ regions.
> ROADMAP 0.5: "a hand-drawn map pinned inside the lander (reached by the lift), drawing itself within the station's
> reach (M3-06)". DESIGN "Collections: Map" and "Station reach: inside reach the map draws itself". It builds on
> M3-16 step 8 (the lift and the deck lookout) and the level review's L5 (the lookout's "seen" sweep).
> Rulings 6, 11, 12 and 14 and pillar 6 apply.

## The fantasy in one line
Ines drew the basin by hand, and the station still remembers where 07 has been. Up on the deck, 07 looks through
the hatch at her map, and finds that the station has been drawing in the places 07 brought home.

## How 07 does it (ruling 14)
- The map hangs **inside the lander, on the back of the deck hatch**: Ines's chart, pinned to a light table.
- When tower L3 wakes the lift (M3-16 step 8), the hatch's motor wakes too. When 07 settles on the deck, the hatch
  swings open by itself, and its window frames the map at 07's lens height.
- **07 looks.** That's all it ever does here. Its lens light falls on the paper. No hands, no pins.
- The station draws. Behind the paper, Kenji's **chart pen**, a small XY pen gantry wired to the station's radio,
  inks what the station knows. The chart pen reuses the plotter's arm parts and scratchy in-key pen sound (M3-16 P7).
  It is a sibling of the plotter, not a copy: smaller, brass and on a light table.
- The light table glows warm through the paper where the station reaches. Home's reach is literally a warm area on
  the map.

## What the map shows (three states per place, no markers)
| State | How it looks | When |
|---|---|---|
| **Ines's sketch** | faint pencil: the rim, the Peak, the canyon and her handwritten names for places the crew knew ("the depot", "Kestrel-3, poor thing", "Kenji's garage") | always: the crew drew it |
| **Seen** | a small pencil ring with no name, and a pencil "?" for a place whose light the player has seen but not reached | the lookout's sweep (L5), a sonar answer, or the dock's evening pillar (P6) |
| **Inked** | the chart pen inks the outline in warm brown ink. Its name is written in the crew's hand, plus a tiny icon of what 07 did there (a relic, a friend, a blueprint, a lit mast) | 07 visited it **and** it lies inside the station's reach (`IStationReach.IsInReach`) |

- A place 07 visited outside reach stays pencil until a mast brings it into reach. Then the pen inks it the next time
  07 looks. This is the "drawing itself" the owner asked for, and it makes the relay network (the second axis)
  visible on paper.
- No "you are here", no objectives, no arrows (M3-16 "No markers on the map"). The map is a memory, not a GPS.
- Collections show here too: a small tally in Ines's hand in the corner (places found / places on the crew's
  list). That's the DESIGN "map discovery" collection without a spreadsheet menu.

## The steps
| # | Step | What happens | What the player learns |
|---|---|---|---|
| 1 | **First look** (end of M3-16 step 8, ~55 min) | 07 reaches the deck. The lookout camera (L5) rises and sweeps; places in view blink once. The hatch opens and the view eases into the map. The chart pen inks, in order: home, then every visited place in reach, ~0.6 s each, with a pen sound climbing the pentatonic scale. | "Everything I did is written down. Home has been keeping track." |
| 2 | **The reach glows** | The light table warms under home's circle and each lit mast's circle. Unlit masts are pencil crosses. | "The masts make home bigger." This answers M3-16 step 7's weak "why relays". |
| 3 | **What's left** | The camera drifts to the edge of the paper: Ines's pencil notes beyond the rim, e.g. "Theo's garden crater: dark, bring a lamp" or "the terraces: too steep for wheels". | the next regions (0.6+), as the end-of-phase hook |
| 4 | **Any time after** | Riding the lift again opens the map. New ink since the last look draws first, then the view rests. | coming home includes "let's see what the station drew" |

Leaving: any input. The view eases back to the deck and 07 rides down (M3-16 lift rules).

## Interaction (map view)
- **Look** pans the paper slowly. **Zoom** has two levels: the whole basin, and a closer view.
- **Interact** on a place shows its one-line card: its name, its job or memory, and what is still there ("a heart
  not yet found", if the site's heart isn't opened).
- The view focuses the nearest place by a soft aim cone (ruling 2), the same rule as the tether.
- Controller-first. UI glyphs come only from the prompt system. No cursor.

## Design rules
- **One sound family:** pencil hiss for "seen", pen scratch in key for "inked", a warm hum while the light table
  warms. Nothing above mezzo-piano.
- **No reading walls:** a place card is at most one line plus its name.
- **The map never lies:** a place is inked only if the save says visited and the station says in reach.
- **0.5 promise rule:** pencil notes for regions that don't ship in 0.5 are allowed only at the paper's edge, in the
  "beyond the rim" area, and they never show a "?" ring. The Director may cut step 3 if it promises too much.

## Contracts (Core, landed first)
- **`MapPlaceState`:** Sketched, Seen, Visited. Inked is derived as Visited + in reach.
- **`IMapKnowledge`** (registered by Gameplay): `GetState(placeId)`, `IsInked(placeId)`, `PlaceCount`,
  `GetPlace(index)` (id, world position, kind), `InkedSinceLastLook` (count).
  - Place ids reuse the anchors: `site.*`, `relay.*`, `canyon.*`, friends' sites `friend.<id>`, home's `place.*`.
  - Map keys: `map.<placeId>.name` and `map.<placeId>.note` for Ines's notes.
- **`MapPlaceSeen(placeId)`** and **`MapPlaceVisited(placeId)`:** published by Gameplay once per save per place.
- **`MapLookStarted` / `MapLookEnded`:** published by Gameplay's lift flow when the hatch view opens and closes.
  UI shows the map. Rover holds 07 still.
- **`MapInkCued(placeId, position)`:** the chart pen drew one place. Audio plays the pen. UI animates the stroke.
- **Map geometry:** World writes a `MapSheet` asset in its builder. It holds polylines in basin coordinates
  (contours every 5 m, the rim crest, the canyon walls, the chasm, crater rims) plus the anchors' positions. UI draws
  it with its Drawing module, in ink on Cream (#F4E6C8) with a seeded hand wobble. No baked texture.
- **Save:** Gameplay's map section stores per-place Seen/Visited and which places were already inked when last
  looked at. The content floor is unchanged: older 0.5 saves derive Visited from existing site/relay/friend state.

## Split
| Box | Delivers |
|---|---|
| gameplay | `IMapKnowledge`, seen/visited tracking (site answered, entered radius, sweep, pillar), the lift's hatch step, `MapLookStarted/Ended`, `MapInkCued` order, save section |
| ui | the map view: paper, contours, pencil/ink drawing, light-table glow from `IStationReach`, place cards, pan/zoom/focus, Ines's notes, the corner tally, blur-behind (visual review V11) |
| world | the `MapSheet` builder (polylines from the generated surface), the sightline query for the lookout sweep |
| art | the hatch (dormant and awake), the light table, the chart pen gantry (reusing the plotter's parts) |
| rover | the deck pose (lens on the hatch window), the lookout camera rise (L5), the ease into and out of the map view |
| audio | pencil hiss, in-key pen scratch (shared with the plotter), light-table hum, the hatch motor |
| story (Director) | Ines's names and notes (`map.*` keys), EN and VI |

## Acceptance
- From a 0.5 golden-path save, the first look inks every visited in-reach place in order with a climbing pen sound.
  The light table glows exactly over home's and the lit masts' reach.
- Lighting a new mast and coming back inks the places that just entered reach, and only those.
- A playtester can say in one sentence what the masts are for after the first look.
- No place is inked that the save hasn't visited. Nothing on the map points anywhere (no arrows, no "you are here").
- Ruling 14: 07 never touches anything. The hatch, pen and light table are machines.
- The map view runs at 60 fps with zero per-frame GC (strokes pre-built at load, revealed by a parameter).
