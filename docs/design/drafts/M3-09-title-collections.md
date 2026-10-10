# M3-09 (draft): the title screen, and the collections kept at home

> **Draft** by the story & flow session for the Director, in the style of M3-16. Not adopted until the Director says
> so. Rulings 6, 11, 14 and pillar 6 apply, plus DESIGN reason 7 ("every collection shows progress at the base,
> never in a spreadsheet menu"). Related: `docs/design/cutscenes.md` §1 (the opening, the short wake),
> `docs/design/game-flow.md` §7 and gaps #9 and #10.
>
> Today on main: the game boots straight into play, and `UI/Hud/TitleCard` drifts the "Lofi Lunar" wordmark in while
> 07 wakes ("no menus before play"). The pause menu has Resume, Settings, New game (put the save away, never deleted)
> and Quit, plus count lines for tapes and relays (`ui.pause.cassettes`, `ui.pause.relays`).

## Part A: the title screen

### The idea
**The title screen is home tonight.** It is the live Main scene behind a small menu, seen from a slow, high drone
drift round the base, in the save's real state:
- a new player sees the dark, sleeping home with three warm points;
- a returning player sees the windows they lit, their friends at their spots and their masts on the horizon;
- after the ending, the Peak's beacon is amber.

The title shows how far you've come before you press anything (ruling 11).

### The menu
| Item | Shown | Does |
|---|---|---|
| **Continue** | only when a save exists; focused by default | eases the camera down from the drift to the chase frame on the dock, then the short wake (cutscenes §1.6, without its fade from black) |
| **New Game** | always; focused when there is no save | no save: starts the opening at once. A save: asks once ("Start a new journey? Your current one is kept, put away beside it."), then puts the save away as today (`NewGameRequested`) |
| **Settings** | always | the same Settings page as the pause menu (volumes, look, invert, language, the next line, Gentle camera) |
| **Quit** | always | quits; no question at the title |

- **Under Continue,** one quiet line in the crew's voice: the last dock evening's "next" line, e.g. "Something was
  calling from the west." It reminds the player where they were going, and it is never a stat.
- **The wordmark:** small, top left, "Lofi Lunar". `TitleCard`'s big drifting wordmark moves to the end of the
  opening, as the cold open's title card over the first chase frame (cutscenes O13), once per new game.
- **Sound:** the radio as the save left it. For a fresh game that is static with the ghost chord; for a lit home it is
  the station, clear, a little low. Menu moves click softly in key; choosing plays one rising note.
- **Input:** mouse, keyboard and gamepad. The focus ring is warm, never neon. No splash beyond Unity's required one.

### Transitions (always eased)
- **New Game:** the drift glides down toward 07 asleep on its dock, and the picture fades to black over 2 s while the
  static rises: that black **is** the opening's O1.
- **Continue:** the drift eases straight into the chase frame (no black), then 07's lid opens in one move and it blinks.
- **Pause → Quit** returns to the desktop as today. A "Back to title" item is not needed.

### Boot flow
1. App boots Main and applies the save (or none) as today, so the scene shows the real state.
2. The title opens. Gameplay holds the session: no step flow, no prompts, no wake. Rover flies the drift. 07 sleeps
   with its lid closed (fresh) or half-lidded on the dock (a save).
3. The player chooses. New Game with a save reloads the scene (today's flow). That reload must not show the title
   again, so App keeps a **one-shot boot intent** beside the save ("newGame"), read and deleted at the next boot (no
   static state). The game then starts with `SessionStarted(freshGame: true)` and the opening.

## Part B: collections, kept at home
Every collection has a physical home at the base, visible from the dock's evening shot, and its progress is something
you **see**: lit slots, empty slots, a full rack. Re-reading anything is 07 looking at it and pressing Interact, the
same verb as everything else.

| Collection (full game) | Its home | Progress you see | Re-read (ruling 14) |
|---|---|---|---|
| **Memories** (24: 4 crew × 6) | the memory shelf, one shelf per crew member, 6 slots each, a small lamp per slot | lit slot = found; an empty slot is dark, with a blank paper tag (nothing spoiled) | 07 aims at a memory (the generous aim cone, ruling 2), Interact: its memory card opens again |
| **A crew member's set** | their bunk window in the lander (4) | the window lights when the set is complete (cutscenes §8) | — |
| **Logs** (12: 4 × 3) | a log tin at the end of each crew member's shelf | the tin's lid sits higher as cards fill it: one, two, three | 07 aims at the tin, Interact: that crew member's found logs as a short card list (at most 3) |
| **Tapes** (8) | the tape rack in Bell's corner (built, 8 slots) | lit spines; dark empty slots | Bell's dial plays them (built); aiming at a spine + Interact shows Ro's liner note |
| **Letters** (the radio's fragments and Ro's letters) | a spike on Bell's side where Bell prints each fragment on a thin paper strip: a literal ticker tape | the stack of strips grows | aim at the spike, Interact: the letters in order, newest last |
| **Kits** | Kenji's plotter board (built, P7) | pinned sheets; empty pins | the bay's choice list (built) |
| **Friends** | their spots at home: Tilly's perch, Bell's corner, Atlas by the bay, Moss in the biodome | a friend is there, or a spot is dark | — |
| **Places** | the crew's map (M3-07) | ink and stamps | the map |

**The pause menu** keeps Resume, Settings, New game and Quit. The two count lines (`ui.pause.cassettes`,
`ui.pause.relays`) become **one** quiet line of found counts with small icons and **no totals** ("6 memories · 3 tapes
· 2 masts"). Totals are what the empty slots at home are for. That keeps DESIGN's "never a spreadsheet" and still helps
a completionist.

## Steps
| # | Step | Box |
|---|---|---|
| 1 | Core contracts below land | Director |
| 2 | Title shell: the menu, the Continue line, Settings reuse, focus and sounds; `TitleCard` moved to O13 | ui |
| 3 | Boot hold and the one-shot intent; `ISaveSummary` | App (Director) + gameplay |
| 4 | The title drift camera and both transitions | rover |
| 5 | Re-read interactions: shelf memories, log tins, tape spines, the letter spike | gameplay |
| 6 | Shelf restructure (4 × 6 slots with lamps and blank tags), log tins, Bell's letter spike and strips | art |
| 7 | Slot lamps and the letter strip as home lights where they are warm | world |
| 8 | Title radio mix; menu clicks; the letter-strip print sound (a tiny dot-matrix purr in rhythm) | audio |
| 9 | Pause count line; the log list card; the letters card | ui |
| 10 | Playthrough: boot → title → New Game → opening → ... → quit → boot → title → Continue → the short wake | gameplay + Director |

## Core contracts (landed first by the Director)
- **`TitleChanged(bool open)`:** UI publishes it. Rover flies the drift while open; Gameplay holds the session; Audio
  sets the title mix.
- **`TitleChoice` {Continue, NewGame} and `TitleChosen(TitleChoice choice)`:** UI publishes it. App handles NewGame
  with a save (the intent and `NewGameRequested`). Gameplay then publishes `SessionStarted(freshGame)` (cutscenes
  §1.7) for Continue, or NewGame without a save.
- **`ISaveSummary`** (read-only, registered by Gameplay before the title opens): HasSave, and the last "next" line
  key for the Continue line.
- **`KeepsakeKind` {Memory, Log, Tape, Letter} and `KeepsakeRecalled(KeepsakeKind kind, string id)`:** Gameplay
  publishes it on a re-read; UI opens the matching card. Memories reuse the existing memory card.
- Localization: `ui.title.continue`, `ui.title.new_game`, `ui.title.settings`, `ui.title.quit`,
  `ui.title.new_game_question`, `ui.title.new_game_aside`, `ui.pause.found_line` (en + vi).

## Acceptance
- First launch with no save: the title shows the dark home and three warm points; New Game reaches the opening's
  black in ≤ 2.5 s and never shows the title again in that boot.
- With a save: the title shows that save's lit home. Continue reaches the chase frame and 07's blink in ≤ 5 s.
- New Game with a save asks once, keeps the old save beside the slot, and the next boot starts the opening, not the
  title.
- Every collection in the table has a home that reads from the dock's evening shot, and every found item can be
  re-read by looking at it and pressing Interact. Nothing in the world shows a total of things not yet found.
- The pause menu shows found counts on one line and no totals.
- Zero GC while the title is open; the drift camera never clips into the base.

## Open questions for the Director
1. This draft moves the big wordmark to the end of the opening. The alternative keeps it on the title only; the
   opening would then have no title card.
2. Re-reading logs opens a short card list (at most 3 cards). Is that within "never a spreadsheet", or should the tin
   cycle one card per Interact instead?
