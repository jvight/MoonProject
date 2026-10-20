# Opening v3: guidance audit, beat by beat

> **Why this audit:** the owner, after build fc1019205:
>
> > "sao giờ k còn hướng dẫn gì nữa, như đoạn này quét xong, thì user k biết làm gì nữa? k biết bấm 'b' để xây vì k
> > hề có thông tin gì … phải thật hoàn hảo từng trải nghiệm của user."
>
> In English: after scanning the lander, nothing tells the player what to do next or that B builds.
>
> **Author:** the game design session, 2026-10-20.
>
> **Evidence:**
> - the IL2CPP bot run `Logs/autoplay/mfc1019205` (report and screenshots per beat and every 20 s);
> - the code: `Gameplay/Hints/FirstUseSpots`, `InteractionHints`, `Progression/OpeningFlow` (the stall reminder) and
>   `Data/Input/Controls.inputactions`.
>
> **The bar (the owner's):** never leave a player without a readable next action within about 5 s of the last
> success.

## 0. Player experience
- **Hook and pull:** the call and the world (opening-v3) still lead. This audit only fills the holes where the world
  doesn't actually lead.
- **The feeling at risk:** "lost and stupid", which is the opposite of calm. A lonely game can be quiet, but it must
  never be *unclear*. Calm needs confidence: the player always knows the next small thing.

## 1. What the build does today
| Mechanism | Rule in code | Problem |
|---|---|---|
| Interaction chips (Scan, Cut, Pick up, Hook cable, Fit) | shown at the object, only when it is in reach and aimed | Nothing at all when the object is out of reach or off screen. |
| First-use Build chip (`FirstUseSpots.TryGetOpenBuild`) | at the apron's centre, **only while the apron is on screen** and the pad is affordable; **shown once, never again** | **At B4 the camera faces the lander and the apron is behind, so no chip ever shows.** Even when shown, a player who ignored it never sees it again. |
| First-use Fit chip | anchored on 07 (good) | — |
| Objective lines (`OpeningFlow`) | a stall reminder only: **120 s with no progress *and 07 still***, once per step, for 20 s | **A lost player usually drives around, so the reminder never fires.** 120 s is far past the 5-s bar. |
| Toasts | "Blueprint learned: Charging Pad" | It says what happened, never what's next, and never the key. |
| Controls card | WASD · Mouse · Tab · L, at the start | There is no B (Build), no right mouse button (Hook cable), and no E. |

**The keys a player must discover:**
- **B:** Build.
- **E:** scan, cut, pick up, assemble, fit, transmit.
- **Right mouse button:** Hook cable.
- **Tab:** 07 (the hold and sockets).
- **Y:** Winch.

## 2. Beat-by-beat audit
Each row starts at the moment the previous beat succeeds.
- **The camera** column means: is it in frame at that moment, per the bot screenshots?
- **Severity:** **BLOCK** = the player can't proceed without trial and error; **DELAY** = they can, after a search; **OK**.

| # | After… | Next action (key) | What tells them, and when | In frame? | Verdict | Smallest fix |
|---|---|---|---|---|---|---|
| 1 | B0 wake (hand-over) | drive to the lander (WASD) | the controls card; the lander's amber beacon; the lit cable dots to it (t0040) | yes | **OK** | — |
| 2 | B2 at the lander | scan the 2 fragments in the open crate (E) | the scan chip "E Scan 1/2" at the fragment, in reach | only in reach; parking on the lander's far side hides the crate | **OK** (minor DELAY) | the fragments' brackets readable from 30 m |
| 3 | B3 both scans | take the kit's parts (E) | the pick-up chip at the crate, in reach | yes | **OK** | — |
| 4 | **B4 parts taken** *(the owner's screenshot)* | **press B**, place the pad on the marked bay, hold E | the toast "Blueprint learned: Charging Pad"; the pickup notices fading. **The Build chip is suppressed: the apron is behind the camera.** | **no** | **BLOCK** | G2 next line `B  Build the charging pad`; G3 chip on 07 with a caret toward the bay; G5 head-look to the apron; the apron glows |
| 5 | build mode open | pick the pad, place it on the bay, hold E | the build strip key hints; "Hold E to assemble" | the bay is in frame only if 07 faces it | **DELAY** | the ghost starts on the nearest valid apron; check the place key with a real mouse at 16:9 and 21:9 (memory: bot misses mouse) |
| 6 | B5 pad welded | drive onto the pad (W) | the pad itself (just built, in view); the battery is low | yes | **DELAY** (nothing says "park to charge") | G2 next line `Park on the pad to charge` |
| 7 | B6 charged, lander reserve spent | find power: the fallen array (drive, E scan) | the lander lamp and cable go dark; the array's glint. **The bot warns the glint was not lit and on screen within 20 s** (B6 ends in the dock close-up; t0140 has the array at the frame's edge, dark) | **no** | **BLOCK → DELAY** | G5 a look toward the array as 07 undocks; G2 `No power left in the lander. Find the solar array in the ruins.`; fix the glint |
| 8 | B7 array scanned | cut its panels (E) | cut chips on the seals, in reach | in reach, yes | **OK** | — |
| 9 | B7 the cutter sputters | take the igniter from the rack (E) | the sputter beat; the rack "beside" | the rack is in frame only if 07 turns | **DELAY** | at the end of the sputter, 07's head turns to the rack and the rack lamp lights (G5) |
| 10 | B7 igniter in the hold | fit it (Tab, then the screen) | the Fit chip on 07 (FirstUseSpots) | yes | **OK** | verify the drag with a real mouse |
| 11 | B7 panels and box cut | build solar near the pad (B) | **nothing.** The Build chip was used once and never shows again. | — | **DELAY** | G2 `B  Build the solar array near the pad` (a key reminder when it's not been used for 5 min) |
| 12 | B8 `PowerOn` | R5: visit a site, then dock (dusk starts) | by design, world only: the two sites in view | partly | **DELAY** (the "dock to continue" logic is invisible) | after 30 s with no site visit, 07 glances at the nearer one (G5). On docking after a visit, the dusk line (G6). |
| 13 | dusk | scan the power module (E) | the module lamp blinks; the reminder at 120 s | the habitat is ~90 m off; the lamp is visible if faced | **DELAY** | G2 at dusk: `Night is coming. The habitat's power module can store it.` |
| 14 | module scanned | **hook the module (right mouse button) and tow it home** | the "Hook cable" chip at the module, in reach. **Nothing says the module is the bank's frame, or that it goes home.** | chip yes; the why, no | **BLOCK** | G6 the blueprint toast reads `Battery Bank · its frame is this module. Tow it home.`; the controls card adds the right mouse button the first time |
| 15 | module at the apron | build the bank on it (B), hold E | nothing | — | **DELAY** | G2 `B  Build the battery bank on the module` |
| 16 | B10 nightfall | (the failed reply) scan the tower ruin (E) | the night wide shot shows nothing (B10-done); `TX · NO RANGE` if 3.3 has landed; the reminder at 120 s | the tower stands left, dim | **DELAY** | after the failed TX, 07's head turns to the tower (G5); G2 `07 can't transmit. The radio tower has an antenna to learn.` |
| 17 | tower scanned | build the mast (B) | nothing | — | **DELAY** | G2 `B  Build the antenna mast` |
| 18 | mast built | transmit (E at the mast) | `TX · READY`, the mast foot lamp | **verify:** no chip in the bot report | **DELAY?** | the chip `Hold E  Transmit` on the mast, persisting until used |
| 19 | B12 answered | R9/R10: go to relay.1 (west) | the ACK line (gone by B12-done); relay.1's blink is **off frame** (the camera faces the Peak) | **no** | **DELAY** | G5 a look west at the blink; G2 `relay.1 answered. It is west, inside your range.`; the range ring once (R9) |
| 20 | at relay.1 | take the spare (E), fit it at the feed socket (E), charge | chips at the objects | in reach | **OK** | — |

**Two global holes cause half of the rows above:**
- **H1 · Help only while still.** The stall timer counts only while 07 stands still, so a driving, lost player never
  gets help. The 120 s is 24× the bar.
- **H2 · "Once, then never" with no visibility check.** The first-use chip can be "spent" on a frame the player never
  saw, or never shown at all when its object is off screen.

## 3. How the reference games keep the next action legible
| Game | The device | What we take |
|---|---|---|
| **Subnautica** | after each milestone the PDA says one calm line naming the next need; signals become beacons with a distance; the fabricator lists what you can make | **a short "next" line after each success**, in the world's voice |
| **Outer Wilds** | contextual button prompts in the corner that **stay until you have used them**; the ship log's arrows and rumours | **prompts persist until used**, not "shown once" |
| **A Short Hike** | the peak is always in view; characters point the way; prompts appear on everything you can interact with | **a nudge from a character's gaze**; our character is 07 itself |
| **Astroneer** | the backpack shows what you can craft now; a hologram tutorial points at the first things; prompts stay | the **"you can build this now"** cue, with its key |

**The common rule:** the long goal lives in the world, while the **next small action and its key stay legible**
until the player has done it once. None of these games leaves a key undiscoverable.

## 4. Rules (proposed)
- **G1 · The 5-second rule.** Within 5 s of any success there is exactly one readable next action on screen: a
  world cue **in frame**, a chip, or a next line. The bot checks it (G7).
- **G2 · The next line.**
  - **When:** right after a success, held for 6 s, on the existing objective view (one element; it hands over to a
    chip by cross-fade, as `GuidanceSlot` already does).
  - **Wording:** plain, short, naming the goal. The key glyph comes first **when the key is new or not used for 5
    min**, e.g. `B  Build the charging pad`.
  - **Not repeated** after that step's success, except by G4.
  - **This brings back the lines without the task list:** they appear after an achievement and leave. They are never
    a standing list.
- **G3 · Key chips persist until used.** A first-use chip shows until its key has been **pressed**, not merely shown.
  If its object is off screen, it anchors on **07** with a soft caret toward the object.
- **G4 · Stall reminder at 25 s.**
  - In the first hour, 25 s with no progress, **moving or still**, brings back the current step's line once. Later
    the delay is 60 s.
  - If the player is still stuck 90 s later, it may show once more.
  - The owner's "never repeat" was about spam. This is a safety net, not a repeat.
- **G5 · 07 looks.** After a success whose next target is off screen, 07's head (and a soft camera bias) turns
  toward it for about 1.5 s. Any input cancels it. This uses the existing gaze request and camera moment system. It
  is diegetic and wordless, like A Short Hike's pointing.
- **G6 · Invisible logic surfaces once.** Anything the game waits on that the player can't see gets a next line
  when it becomes relevant:
  - the module is the bank's frame, so tow it home;
  - dock to end the day;
  - transmit at the mast.
- **G7 · Guarding it.**
  - The bot asserts G1 at every beat: a chip, a next line, or a named world cue in frame within 5 s of success.
  - It saves a screenshot at success + 5 s.
  - The Director's first-time play at 16:9 and 21:9 is the final gate.

## 5. Ranked summary
| Rank | Where | Severity | Fix |
|---|---|---|---|
| 1 | **B4 → build the pad (B)**: the chip is suppressed off screen | **BLOCK** | G2 + G3 + G5 |
| 2 | **H1:** the stall reminder only while 07 is still, at 120 s | **BLOCK** (global) | G4 |
| 3 | **H2:** first-use chips "once", with no visibility check | **BLOCK** (global) | G3 |
| 4 | **The module → tow home → bank on it:** the why and the key are not said | **BLOCK** | G6 + the right mouse button on the controls card |
| 5 | B6 → the fallen array: the glint not in frame | DELAY (nearly BLOCK) | G5 + G2 + the glint fix |
| 6 | B12 → relay.1: the blink off frame | DELAY | G5 + G2 |
| 7 | solar, bank, mast builds: no build cue after the first | DELAY | G2 |
| 8 | B10 → the tower: night, nothing said | DELAY | G5 + G2 |
| 9 | the sputter → the rack | DELAY | G5 |
| 10 | R5 → dock to start dusk (invisible) | DELAY | G5 + G6 |
| 11 | the pad built → park to charge | DELAY | G2 |
| 12 | transmit: chip unverified | DELAY? | G3 chip |
| 13 | build-mode placement with a real mouse | DELAY? | verify at 16:9 and 21:9 |

## 6. Slices (for the Director)
1. **Next line** (Gameplay `OpeningFlow` + UI `ObjectiveView` + Story text en/vi), S–M:
   - publish the new step's line on each success for 6 s;
   - key glyph rule;
   - G6 lines;
   - EditMode: one line per success, never during a chip, never repeated outside G4.
2. **Persistent, anchored chips** (Gameplay `FirstUseSpots` + UI), S:
   - "used" means pressed;
   - an off-screen fallback anchors on 07 with a caret;
   - a Transmit chip;
   - the controls card adds B and the right mouse button when first relevant.
3. **Stall timer** (Gameplay), S: 25 s with no progress, moving or still, in the first hour; one more after 90 s.
   EditMode on both.
4. **07 looks** (Rover + Gameplay target), S–M: a gaze-plus-camera nudge to the next target after rows 4, 7, 9, 12,
   16 and 19; cancelled by input.
5. **Bot G1 guard** (Tools), S: assert within 5 s of each beat; a screenshot at success + 5 s; run on the IL2CPP build.

**Order:** 1 + 2 + 3 first, in parallel (they close every BLOCK), then 4, with 5 alongside.

## 7. Playtest signal
- A first-time player at 16:9 and 21:9, with real input, never stands still or circles for more than 10 s without a
  readable next action on screen.
- At B4 they press B within 10 s.
- At the module they hook and tow it home without asking.
