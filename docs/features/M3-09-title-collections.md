# M3-09: Title screen and collections at the base

> **Approved by the Director on 2026-10-11** (drafted by the game design and story & flow sessions).
> ROADMAP 0.5: "title screen (New Game / Continue / Settings), collections shown at the base".
> Today:
> - the game opens straight into play, with a "Lunarion" wordmark (the game was "Lofi Lunar" until 2026-10-11) drifting in over the wake (`UI/Hud/TitleCard`,
>   `ui-captures/01_title`);
> - New Game lives in the pause menu (`NewGameRequested`, `ISaveService.PutAway`);
> - collection counts sit in the pause card (`12_pause`).
>
> DESIGN #7 applies: "every collection shows progress at the base, never in a spreadsheet menu". So do rulings 6, 11
> and 14, pillar 6 and the Steam bar (VISION product decisions).

## Part A: the title screen is the world

### The rule
**No separate menu scene, no loading screen.** The title is the live game world, loaded from the save, framed by a
still camera, with three words over it. The world behind the menu *is* the player's progress. A returning player sees
their own base, windows lit as they left them. A new player sees the dark base and a sleeping rover.

### The frames
- **Fresh install (no save):** the camera holds the P5 stargaze composition, low and front three-quarter on 07's
  closed lens, with Earth and the Peak's red blink behind. The base is dark. The radio crackles quietly, out of tune.
- **With a save:** the camera holds a high, wide shot of home from the south-east (the dock's-evening framing, P6).
  The lit windows, pools and masts are the save's real state. 07 sleeps on the dock, lens half-lidded. The radio
  plays the save's station softly.
- The wordmark is the new title treatment (see below). The menu sits low-left, small and calm, with blur-behind on the
  lower third only (visual review V11).
- **Idle:** after ~20 s untouched, the menu fades out and the camera drifts slowly (stillness is a reward). Any input
  brings it back.

### The menu
| Item | Shown when | Does |
|---|---|---|
| **Continue** | a save exists (default focus) | the menu fades and the camera eases from the title frame into the chase frame behind 07 in ~2.5 s. 07 wakes with a short stretch. Control returns when the blend ends. |
| **New Game** | always (default focus when no save) | no save: the camera stays on the stargaze and the M3-16 step-0 wake plays from here, with "Look up" as the first prompt. With a save: a confirm card, "Put this moon away and start again? Your old moon is kept." Safe answer focused. Yes runs `PutAway()`. The windows go dark one by one (~3 s, reversing M3-16 step 2), then the wake plays. Never a hard cut. |
| **Settings** | always | the same pages as the pause menu (audio, look, next-line toggle, language, display, controls), one implementation |
| **Quit** | always | a soft fade; the radio's static tails out |

- **First boot only:** before the title, one quiet language choice (English / Tiếng Việt), shown as two words, no
  flags. Remembered in settings, changeable later.
- The pause menu keeps Resume, Settings and Quit. Its **New Game moves to the title** ("Quit to title" replaces it),
  so starting over is never one mis-press away mid-session.
- The pause card's counts are removed; collections live at the base (Part B).

### The title treatment
The current wordmark is evenly spaced type with a thin rule: tidy, but anonymous. Proposal:
- "Lunarion" in Be Vietnam Pro, with one small diegetic touch: a thin crescent drawn as the dot of the "i". It is
  the only ornament.
- Cream (#F4E6C8) with a very soft warm bloom. It sits in the sky area, never over 07.
- A one-line subtitle under it in small caps, localized: "a radio left on" / "một chiếc radio vẫn bật".

### Rules
- **No frame-one snaps:** every transition is a camera blend or a fade (anti-goal).
- **The title loads the save once.** Continue is instant because the world is already there.
- **The title never shows a number,** a percentage or a play time. The base *is* the summary.
- **Steam bar:** controller and mouse both work, with focus always visible. Esc / B always backs out to the safe
  answer.

## Part B: collections at the base

Every collection has one physical home that changes as it grows, visible from the default camera and at 30 m (ruling
11). 07 browses with its lens: park in front, look, and the nearest item focuses (ruling 2's soft cone), with a
one-line card. No hands (ruling 14).

| Collection | 0.5 size | Its home at the base | How it shows progress | Complete |
|---|---|---|---|---|
| **Relics** (24 in 4 crew sets of 6; 0.5 ships the slice's 6) | 6 | the memory shelf, reorganised into **four crew bays**, each with the crew member's name in their own handwriting on a plate | Each empty slot holds a **dust outline** of the object that stood there (ruling 12 remnant, not a "?"). A placed relic wipes its outline clean and lights its slot lamp. | That crew member's lander window lights for good, and their last log plays on the radio (DESIGN). |
| **Crew logs** (12, 3 per crew) | as many as 0.5 places | clipped as small paper cards to the **same crew bay**, under the relics | Found cards hang there. Focusing one replays it as a ticker line. | Counts toward the crew bay feeling full; no separate meter. |
| **Cassettes** (8; 0.5 ships 3) | 3 | Bell's tape rack | Eight spine slots. Empty ones are blank cream spines; found tapes show their title in Ro's hand. Focusing one shows Ro's liner note. Bell plays it. | All 8: Bell's corner gets a small hand-lettered "Lumen After Dark" sign that lights. |
| **Blueprints** | 3 in 0.5 | Kenji's plotter board (M3-16 P7) | Drawn sheets, plus empty pins with their kit's silhouette pencilled. | All pinned: the board's lamp goes from dim to full. |
| **Places** | all anchors | the crew's map (M3-07) | ink vs pencil; Ines's corner tally | all inked: Ines's "checked the Moon was still there ✓" note appears |
| **Friends** | Tilly, Bell | their corners at home | the friend is there, idling | — |

### Browse rules
- **One verb:** park near a collection, and the first time, a context prompt reads "Look". Look input moves focus
  between items. Leaving the radius ends browsing.
- **Card:** the name plus at most two sentences (STORY writing rules), placed beside the item, never full-screen.
- **Camera:** a gentle ease to a three-quarter view of the bay or rack and back. Depth of field only while browsing
  (visual review V10).
- **The first time a set gains its second item,** the ticker says once, e.g. "Ro's shelf is filling up."

## Contracts (Core, landed first)
- **`GameFlowState`:** Title, Playing. **`IGameFlow`** (State) is registered by App.
  - **`TitleShown(hasSave)`**, **`ContinueRequested`**, **`QuitToTitleRequested`**; the existing
    **`NewGameRequested`** is reused.
  - App owns the flow. Gameplay, Rover and UI read `IGameFlow` to hold input and the HUD while in Title.
- **`ICollections`** (registered by Gameplay):
  - `GetSet(collectionId)` returns (found, total) for `relics.<crew>`, `logs.<crew>`, `cassettes`, `blueprints`,
    `places`;
  - `IsFound(itemId)`.
  - It is read by UI (cards), Art (outlines and slot lamps through Gameplay) and Audio. Allocation-free.
- **`CollectionItemFocused(itemId)`** and **`CollectionBrowseEnded`:** published by Gameplay while 07 browses.
  UI shows the card. Rover eases the camera. Audio plays a soft tick in key.
- **`CrewSetCompleted(crewId)`:** published once per save. World lights that crew's window for good. Gameplay queues
  the last log on the radio.
- **Localization:** `title.*` (menu, confirm, subtitle), `crew.<id>.plate`, and the existing `relic.*`, `log.*`,
  `cassette.*` keys.

## Split
| Box | Delivers |
|---|---|
| app / foundation | the boot flow into Title (world loaded, input held), Continue / New Game / Quit to title, the first-boot language step, `IGameFlow` |
| ui | the title menu, wordmark and subtitle, confirm card, blur-behind, idle fade, shared settings pages, "Quit to title" in pause, the collection cards, pause counts removed |
| rover | the two title frames (stargaze, home-wide), the Continue blend into chase, the idle drift, the browse camera |
| gameplay | `ICollections`, browse focus (soft cone), `CrewSetCompleted`, the "set filling up" ticker line, the New Game windows-dark sequence trigger |
| art | four crew bays on the shelf with handwriting plates, dust outlines per relic, log card clips, tape rack spines, plotter pin silhouettes, the Bell sign |
| world | windows going dark on New Game (reverse of the home light stages), crew windows lit on set completion |
| audio | title ambience (radio out of tune, or the save's station), menu ticks in key, the browse tick, the windows-dark sequence |
| Director (story) | crew plates, subtitle, "set filling up" lines, EN and VI |

## Acceptance
- A fresh install boots to the stargaze title with no save, in under ~10 s on the target PC. New Game flows into the
  M3-16 wake with no cut.
- With a save, the title shows that save's real base state. Continue hands control to the player in ≤ 3 s with an
  eased blend and no loading screen.
- New Game from a save asks once, archives the save (never deletes it) and darkens the base before waking.
- Every collection's progress is readable at the base from the default camera, and no menu shows a collection count.
- A playtester can find "how many of Ro's things are home" by driving to the shelf, without opening any menu.
- 60 fps and zero per-frame GC on the title and while browsing.

## Amendments proposed by the story & flow session (2026-10-11)
Checked against `docs/design/cutscenes.md` §1 (the opening, now VISION ruling 8) and `docs/design/game-flow.md`
§5, §7 and §8. For the Director to accept or refuse, one by one.
1. **The title meets the opening through the lens** (cutscenes §1.7). The fresh title frame, low and front
   three-quarter on 07's closed lens, is the same shot the opening lands on in O10.
   - New Game: the menu fades (0.5 s) and the camera pushes into the closed lens (2 s). The black inside is the
     opening's O1, and the title's quiet static carries on into it with no gap.
   - So "the M3-16 step-0 wake plays from here" becomes "the first-person opening plays from here". The first prompt
     follows cutscenes O14: "Look up" only if the player didn't look during the first-person part, otherwise "Drive".
2. **The fresh title's idle drift stays near the lens shot:** within ~10° of it, easing back (≤ 1 s) when New Game is
   chosen. An orbit would have to fly back first.
3. **New Game with a save, and the scene reload.** Keep the windows going dark (~3 s). Then:
   - the camera moves to the fresh frame and pushes into the lens;
   - the reload that puts the save away (today `NewGameRequested` reloads Main) happens **inside O1's black**. If
     loading takes longer than O1's 3 s, the black and the static hold. There is no loading screen and no hard cut;
   - App keeps a one-shot "new game" boot intent beside the save, so the reloaded boot skips the title and starts the
     opening.
4. **Continue is the short wake** (cutscenes §1.6). The "short stretch" is 07's lid opening, one blink and the wing
   easing open. Then a 2 s glance, which never holds control, at the newest pillar: Tilly's overnight glint
   (game-flow #15), or Earth. Control returns in ≤ 3 s, as written.
5. **One wordmark.** The title owns it, with the new treatment. Retire `TitleCard`'s drift over the wake: it would
   sit over 07's own eye during the first-person part.
6. **Contracts.** Keep `IGameFlow`. Add `SessionStarted(bool freshGame)`, published by App on Title → Playing
   (cutscenes §1.8). Rover plays the opening or the short wake from it. It also fixes today's bug, where any load at
   stage Asleep replays the opening (`RoverCameraRig.OnBasePowerChanged`, game-flow #11).
7. **The letters need a home.** Part B has no place for the radio's fragments and Ro's letters, which game-flow §5
   and §7 make a collection: each mast plays back the letters it caught, and about ten more arrive after the ending.
   - Proposal: Bell prints each one on a thin paper strip onto a spike at her corner, a literal ticker tape. The
     stack grows, and focusing the spike replays them in order.
   - Add the row, plus `letters` to `ICollections`.
8. **Promise only what this version holds.** Dust outlines and pencilled pin silhouettes appear only for the relics
   and kits in the build's catalogues:
   - in 0.5: 6 outlines, and the plotter board with Hover-Jump plus 3 tubes' pins (Cradle, Headlamp, Boost Coils);
   - the rest of each crew bay is plain, dusty shelf;
   - this follows the 2026-10-10 review's rule against a found promise that can't be kept in the same phase.
9. **The crew windows are the bunk windows.** `CrewSetCompleted` lights a crew member's bunk window, which must stay
   separate from L1's cabin windows (game-flow #2, cutscenes §2). Its moment is cutscenes §8.
10. **One radio profile.** The fresh title's "out-of-tune crackle" and the opening's static with the ghost chord are
    the same Asleep profile in audio, so the seam in point 1 can't be heard.

## Director decisions on the amendments (2026-10-11)
- **Adopted in full:**
  - the fresh title frame is O10's lens shot with the lid shut, and New Game pushes into the closed lens, whose
    black is O1;
  - with a save, the windows go dark first;
  - the reload hides inside that black, behind a one-shot boot intent;
  - Continue is the short wake;
  - TitleCard's drift retires;
  - outlines and pins only for content in the current build;
  - one Asleep radio profile.
- **`SessionStarted(freshGame)`:**
  - App publishes it on Title -> Playing once the title exists.
  - Until then, Gameplay publishes it when the save is applied (the opening slice). The title work moves the
    publish to App.
- **The letters' home** is Bell's paper-strip spike.
