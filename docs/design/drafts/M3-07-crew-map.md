# M3-07 (draft): the crew's map, inked by the station's reach

> **Draft** by the story & flow session for the Director, in the style of M3-16. Not adopted until the Director says
> so. Rulings 11, 12, 13 and 14 and pillar 6 apply. Related: M3-06 ("the map draws itself" within reach), M3-16 step 8
> (the lift up to the deck), `docs/design/cutscenes.md` §6 (the lookout and the hatch) and `docs/design/game-flow.md`
> (§3, S5 and gap #5).
>
> Owner's brief (2026-10-11): "a hand-drawn map pinned inside the lander, reached by the lift, drawing itself within the
> station's reach."

## The idea in three lines
1. **The crew already drew the whole moon, in pencil.** Ines walked the rim every morning and sketched it all: the
   basin, the canyon, the terraces, a black crater, the Peak. Her notes are in the margins. That is the crew's
   knowledge, faded and approximate.
2. **The station inks what it can hear.** A small survey plotter, a cousin of Kenji's pen plotter, sits on the chart
   table. Wherever the station's reach covers (the tower's circle and every lit mast's), it traces the true ground in
   blue ink and stamps what 07 has found there. The map "draws itself" literally: a pen moves.
3. **The map opens no region; it points at them.** In 0.5, the next regions are promises in pencil with Ines's notes.
   Its one job: **show where everything is, and let 07 choose its next light.**

## Why it fixes the conflicts (game-flow §8 #5)
- DESIGN says points of interest light up "once visited". M3-06 says the map draws itself within reach. Here, both
  are layers of one sheet: ink = reach; a stamp = visited **and** in reach.
- M3-16 step 8 says "the crew's map inside opens new regions", but 0.5 has no new region. Here, the map shows the
  regions to come as pencil sketches and notes, with no ink until a mast reaches them.

## How 07 physically uses it (ruling 14)
- The lander's hatch is human-sized (`LanderMeshes`: 0.8 × 1.85 m), so 07 can never go inside.
- The crew left the hatch **ajar** in their hurry. The first time, 07's beam nudges it open (a beam "nudge", one of
  ruling 14's verbs).
- 07 parks at the doorway and **peers in with its head**: its neck carries the lens to about 1.6 m, inside the
  door's height.
- The chart table stands against the cabin's back wall, facing the hatch, lit by one warm work lamp. The map is pinned
  flat on it, with the survey plotter's gantry over it.
- To choose a place, 07 shines its **headlamp spot** on the paper: the warm circle of light is the cursor. Look input
  moves the head, and so the spot. 07 never touches anything.

## The sheet: what is on it
| Layer | What it shows | When |
|---|---|---|
| **Pencil** (Ines) | the whole region's outline: rim, canyon, terraces, crater, the Peak; margin notes in her hand; small crew doodles (Kenji's duck in a corner, a coffee ring, Ro's sticker "Lumen After Dark: tune in") | always, from the first look |
| **Ink** (the station) | the true ground's shapes in blue: flat-shaded contour bands taken from the terrain, drawn as a pen would draw them, 1 line per 4 m of height | only inside the station's reach (`IStationReach.IsInReach`) |
| **Stamps** (the station) | one small icon per thing 07 has found and the station can hear: a site (whole, or a skeleton once picked clean), a friend's spot (dark, or lit once home), a mast (dark or lit), a memory's dot where it was found, the dock | visited and inside reach |
| **The spot** (07's headlamp) | the warm circle 07 shines on the paper | while 07 is looking at the map |

**What is never on it:** an unfound thing as a marker, a checklist, a percentage, an arrow or a quest. Pencil notes may
*hint* at what lies beyond, as a person would write it: "Atlas parked here?", "Moss's garden: crater floor. Dark.",
"The dish. Kenji's job, after the storm." These are localization keys (`map.note.<id>`), in the crew's voice (STORY
writing rules).

## The steps
| # | Step | What happens | Feel |
|---|---|---|---|
| 1 | **The first look** (game-flow S5) | After the lookout (cutscenes §6), 07 turns to the hatch: a crack of warm light. Driving near it shows the place card "The crew's map: where everything is." 07's beam nudges the hatch open, and it peers in. | the door's slow creak; inside, the room tone is close and warm, and the radio is muffled behind 07 |
| 2 | **The plotter wakes** | The camera eases over 07's head into the doorway until the sheet fills the frame (Moment tier). The plotter's lamp lights, its gantry homes with a zip, then it **inks everything currently in reach**: the basin's inner circle in blue, line by line, about 6 s, and stamps each place found so far. | a scratchy, in-key pen sound (reusing the P7 plotter's voice), one soft tick per stamp, climbing D E F# A B |
| 3 | **Choose a light** | 07's headlamp spot rests on the paper. Over a stamp or a pencil note, the spot warms and that place's name shows once, small, at the paper's edge (UI). Holding Interact on it **marks** it: the plotter draws a small ring round it, and outside, that place's pillar rises on the horizon and stays until 07 reaches it. One mark at a time; a new one replaces it. | a held hum while marking; the pillar's answer note (high E) heard faintly from outside |
| 4 | **Coming back** | When reach has grown since the last visit (a mast lit), the plotter catches up: it inks only the new ground and stamps the new finds (2–6 s). With nothing new, it rests, and the map is just there to look at. | the same pen voice, shorter |
| 5 | **Leaving** | Any drive input: 07 backs out of the doorway and the camera eases back to the chase frame on the deck. | the room tone gives way to the deck's wide hush |

**Marking and the dock's evening (P6).** The dock's evening raises the tower's chosen next pillar. If the player has
marked a place, that mark is respected: the evening shows the marked pillar, so the player's choice wins.

## Where it fits in the flow
- **0.5:** the map opens at S5 (after the lift and the lookout). Ink covers the tower's circle and grows with relay.0–3
  until the basin is inked. The terraces, the crater and the Peak stay in pencil with Ines's notes, and the 0.5 ending
  promise (the terraces' foot) shows as a stamp once 07 has seen the crawler wreck from the lookout.
- **0.6–0.8:** each region's mast inks it. The Peak is inked last, when its foot's mast lights: the whole sheet is blue.
- **After the ending:** the plotter draws a small "07" at the Peak's summit, the only time it writes a word.

## Split
| Box | Delivers |
|---|---|
| gameplay | the map station: the hatch (ajar → nudged open, saved), the peer-in spot at the doorway, the mark (one at a time, saved, raises its pillar via the existing pillar system), the "seen ink" record so the catch-up draws only what is new, the place card `place.map`; tests for the mark rules and the catch-up set |
| ui | the map's drawing: a render texture on the sheet with three layers (pencil art, the ink built from `ITerrainQuery` contours clipped to `IStationReach`, the stamps); the stroke-by-stroke reveal driven by the plotter's progress; the name at the paper's edge; all `map.note.*` and `place.map.*` keys (en + vi). ROADMAP already gives M3-07 to ui. |
| art | the chart table and its work lamp; the survey plotter (reuse P7's plotter rig at a smaller size); the hatch's ajar and open states; the pencil base sheet (Ines's sketch of the whole region, drawn from world's layout); doodles |
| rover | peering in (neck reach to the doorway), the headlamp spot as a cursor (07's lamp aimed by the look input, soft-limited to the sheet), the camera's ease into the doorway and back |
| world | the lamp inside the cabin as a home light (it shows through the porthole from outside once the map is awake); the pencil base needs the regions' rough outlines: export them from `IWorldLayout` for art |
| audio | the hatch creak, the pen's scratch (reuse), stamp ticks in key, the cabin's close room tone, the marking hum |

## Core contracts (landed first by the Director)
- **`MapViewChanged(bool viewing)`:** Gameplay publishes it when 07 starts or stops peering in. Rover frames the
  shot, UI shows the sheet's interaction and hides the HUD, Audio swaps to the cabin room tone.
- **`MapPlotted(int strokes, bool catchUp)`:** Gameplay publishes it as the plotter starts drawing (first look or
  catch-up). UI reveals the strokes over the plotter's time; Audio voices the pen.
- **`MapPlaceMarked(string placeId, Vector3 position)`:** Gameplay publishes it (empty id = unmarked). Gameplay's
  pillar system raises the pillar; UI rings the stamp.
- **`IMapSheet`** (read-only, registered by Gameplay): the stamps (place id, kind, position, state), the current mark,
  and the inked-so-far set. UI draws from it.
- **Place id:** `place.map` (`place.map.name`, `place.map.job`), joining M3-16's list.

## Acceptance
- From the deck, a playtester can say what the map is for in one sentence ("it shows where things are, and I can
  pick where to go").
- The first look inks only what is inside the station's reach; a place outside reach shows only Ines's pencil, even
  if 07 has been there.
- Lighting a mast and coming back to the map draws only the new ground, in ≤ 6 s.
- Marking a place raises its pillar outside within 2 s; the next dock's evening shows that pillar, not another.
- Nothing on the sheet names or marks a thing the player hasn't found, except Ines's handwritten notes.
- Ruling 14 holds on screen: the hatch opens by the beam's nudge, 07 peers in with its head, and the headlamp spot
  is the only cursor.
- The sheet reads at 1080p from the doorway shot: pencil, ink and stamps are distinct at a glance.
- Steady-state zero GC while viewing; the ink texture is rebuilt only when reach changes.

## Open questions for the Director
1. Should the map also be reachable from anywhere through a pause-menu view of the same sheet? This draft says **no**:
   the trip home to look at it is part of the ritual, and radio-hop makes home close.
2. The pencil sheet needs the 0.6–0.8 regions' rough shapes now. If world's layout for them isn't fixed yet, Ines's
   sketch may be deliberately loose: she drew it by eye.
