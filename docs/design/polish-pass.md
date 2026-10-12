# The polish pass: everything the player can notice (design audit and waves)

> Author: the game design session. Proposal for the owner's approval.
> The owner: the cut's charge-up, the dock locking 07 in and 07's chirps were *examples*. The real ask: "everything as
> polished as possible, the best experience for the player."
>
> Method:
> - `tools/feel_coverage.py` on main 4072ee55: 49 of 95 Core events are both heard and seen.
> - Every NO that matters was checked in the code (§1.1), because visuals the publisher draws itself aren't counted.
> - Then the things events don't cover: screens, transitions, save moments, the first hour beat by beat, friends,
>   radio, rest and idle, recovery and progression (§1.2–§1.5).
> - "NOW" is what the code does today. Where a line says *unverified*, it was not checked in play.
>
> This builds on `feel-pass.md`: §2 (verbs), §3 (the cut, P0, approved), §4 (the feel kit and `FeelBeat`), §7 (the
> dock lock and 07's chirps). Rows here point to those sections instead of repeating them.

## 0. The bar (one rule for every row)
Every state change the player can notice gets:
1. **anticipation:** something shows it's about to happen;
2. **a visible mechanical action:** a machine, 07's beam or 07's body does it (ruling 14);
3. **a closing beat:** light, a settle, a sound that resolves on D;
4. **sound** in D major pentatonic, at most three layers, at most mezzo-piano;
5. **a 07 reaction** where it fits: body or a chirp (feel-pass §7.2), within its cooldowns.

Plus: no camera shake, Gentle respected, rumble ≤ 0.35, zero per-frame GC.

Impact: **H** = in the first hour, or seen many times a session; **M** = once or twice a session; **L** = rare.
Size: **S** ≤ ½ day of a session, **M** ~1 day, **L** several days.

## 1. Inventory

### 1.1 Core events the tool flags, checked in code
"Drawn by publisher" means the tool's NO is a false alarm: the publisher draws or plays it itself.

| Event | Tool | Checked | Verdict | Target | Imp | Owner | Size |
|---|---|---|---|---|---|---|---|
| `DockEveningOpened` | heard NO | `AudioCueIds.DockEveningSwell` exists, **but nothing plays it** | **GAP** | the swell under the wide shot: a soft pad on D rising to A over 3 s, ducking the radio 3 dB, resolving as the next pillar rises | H | audio | S |
| `StationNotYet` | seen NO | only `StationAudio` (a tone) and `DormantAnswers` | **GAP**: the "not yet" line still never reaches the screen (first-hour-v2 §3 asked for it) | on first approach: the place card's second line, "Asleep. Metal and a coil will wake it.", with the low "not yet" tone. Shown once per place per save. | H | gameplay + ui | S |
| `TechNodeChanged` | heard NO | UI and the lab lights only | **GAP** | a node lighting: a bloom pop and a pentatonic step by branch, and the lab porthole lamp ticks on (outside: one warm click) | H | audio | S |
| `DecodeHandOff` | heard NO | art meshes, world lights, the plotter | **GAP** | the lamp pulse down the cable gets a travelling soft "tick-tick-tick" panned along its path, and lands on the plotter's first scratch (feel-pass §2.9) | M | audio | S |
| `ComponentFound` | heard NO | the dock evening, tower parts, `RoverKit` | **GAP** | a component lifting free: a two-note "found" (E → A), then 07 Curious (first time) or Affirm | H | audio + rover (chirp) | S |
| `RoverBoostChanged` | heard NO | battery and controller | **GAP** | the drums' boost: a rising capacitor whine (A), and a release "fff-tick" as the drums empty. **Rumble:** 0.06 while boosting. | M | audio + rover | S |
| `RelicStowed` | heard NO | `CargoCradle`, UI | **GAP** | the strap cinch "tuk" (shared with the bundle tie-off, feel-pass §3) and the rack's settle | M | audio | S |
| `SignalRadiusChanged` | seen NO | radio and upgrades | **GAP**: the tower's reach grows only in sound | a warm ring rolls out over the ground from the tower to its new radius (2 s, ease-out) when a level lands: the reach you just bought, seen once | M | world | S |
| `MaterialsChanged` | heard NO | UI chip | covered by feel-pass §3 CountTick (P0) | — | H | (P0) | — |
| `NextStepChanged` | heard NO | UI line | **small gap** | a breath-quiet page-turn tick when a *new* step's line first shows (not on every re-show) | H | audio | S |
| `PlaceFirstApproached` | heard NO | UI card | *unverified* (the UI may play `UiCue` card) | confirm the card cue plays. If not, use the `ui_card` cue. | H | ui | S |
| `FriendGreeted` | seen NO | `FriendField` animates the friend | drawn by publisher; **07's side is missing** | 07 turns its head to the friend and chirps Greeting (feel-pass §7.2), first time each day | M | rover | S |
| `RadioHopStarted` | seen NO | `UI/Radio/HopFade` exists | drawn (UI fade) | — | — | — | — |
| `RoverJumpCharged` | seen NO | rover controller | drawn by rover (coils); feel-pass §2.4 adds the crouch | — | — | (P1) | — |
| `LiftCued`, `FindDecoded`, `LabStationFaced`, `BlueprintDrawing`, `BellSignalFound`, `FriendSpotted`, `FriendAnswered`, `SalvageCutStarted/Stopped`, `BatteryChanged`, `FlankLampLit` | seen NO | each is drawn by its gameplay or rover publisher | false alarms | covered by feel-pass §2 and §3 where they need more | — | — | — |
| `RoverChirpRequested`, `RoverChirpSyllable`, `FeelBeat` | no consumers | contracts landed, consumers in flight | in progress (feel-pass §6, §7) | — | H | — | — |
| `MapPlaceVisited`, `ContinueRequested`, `QuitToTitleRequested`, `LanguageChanged`, `RadioHopListChanged`, `BatteryArcRevealed`, `BlueprintsCarriedRestored` | — | state or flow events | not player moments | — | — | — | — |

**Result:** 9 real gaps from events, almost all audio and S. They are cheap and high-value: wave 1.

### 1.2 Screens and transitions
| Item | NOW | GAP | TARGET | Imp | Owner | Size |
|---|---|---|---|---|---|---|
| **Boot → title** | the world loads with the rover held; `TitleRadio` plays; the wordmark is drawn | *unverified*: whether the first frame fades in or cuts from black after the load | a black hold, the radio's crackle first (300 ms), then a 1.2 s fade-in of the frame, then the wordmark drifts in (existing). Nothing pops in while loading: shaders are warmed before the fade (§3.5). | H | ui + app | S |
| **Continue** | eases into play | no 07 reaction | 07 wakes with a short stretch (head lift, eye on) and a Greeting-quiet chirp as control returns | H | rover | S |
| **New Game over a save** | the save is put away and the **scene reloads** | a reload is a hard cut and a possible hitch | windows go dark one by one (M3-09 draft), then a 1.5 s fade to black with the radio tailing to static. The reload happens under black, and the wake fades in. | M | ui + app + world | S |
| **Quit to title / quit** | saves and reloads into the title / quits | the fade is *unverified* | always a 1 s fade with the radio tail. Quitting the game: fade, then exit. Never a frozen frame. | M | ui + app | S |
| **Pause** | a dark card, settings, quit | the world goes flat dim (M3-18 V11 adopted: blur-behind) | blur and desaturate behind in 250 ms. The radio ducks 6 dB with a low-pass. Unpause eases back in 200 ms. | H | ui + audio | S |
| **Settings** | sound, camera, Gentle, guidance, invert, look speed, language, text size | **no display settings** (resolution, window mode, vsync, frame cap, quality), **no rumble** (decided), **no controls page** | add Display (resolution, window mode, vsync, 30/60/uncapped), Rumble (on, strength), and Controls (a read-only bindings page; rebinding later). Each change previews live, and every slider ticks in key. | M (Steam bar) | ui | M |
| **Map** (survey chart) | `MapLookStarted/Ended`, ink cues heard and seen | — | fine. Add the M3-18 V11 blur behind the paper edge. | M | ui | S |
| **Lab board, cards, ticker** | built; M3-24 fixed the type sizes | cards appear without an anticipation beat (*unverified*) | cards slide 8 px and fade in 180 ms with the `ui_card` cue. The ticker types at reading speed with a quiet tape tick every few characters (never per character). | H | ui + audio | S |
| **Credits** | **none in code** | needed for the 0.8 ending and the Steam bar | a calm roll over the radio's last track, skippable after 3 s by a held button; ends at the title | L (now) | ui | M |
| **Save moments** | the dock keeps the day; checkpoints save; no visible indicator (*unverified*) | the player can't see that a save happened | a diegetic tick: the dock lamp blinks twice and 07's flank shows a tiny cream "07 ✓" glyph for 1 s on every real save. The ticker says nothing. Never a spinning icon. | H | ui + gameplay | S |

### 1.3 The first hour, beat by beat (first-hour-v2 §4)
| Min | Beat | GAP against the bar | TARGET (pointer) | Imp | Owner | Size |
|---|---|---|---|---|---|---|
| 0–1:30 | the opening, the protocol readout, the four flank lamps | the flank lamps light without per-lamp notes (*unverified*) | each lamp pops on its note (D E F# A); 07 Curious when the readout ends | H | audio + rover | S |
| 1:30–2:30 | drive to the tower | start/stop body beats | feel-pass §2.3 | H | rover | M |
| 2:30–3:30 | the tower's card, the recipe | "not yet" text missing | §1.1 StationNotYet | H | gameplay + ui | S |
| 3:30–7:00 | the depot cuts | — | feel-pass §3 (P0, in flight) | H | — | — |
| 7:00–9:30 | the tower wakes, the cables light, the windows light | each window lighting is silent (*unverified*); 07 doesn't react | each window: a soft warm click on a rising step; the radio clears; 07 lifts its head and chirps Happy | H | audio + rover | S |
| 9:30–10:30 | the sweep, Tilly's pillar | — | fine (heard and seen) | — | — | — |
| on the way | first listen | the antenna's anticipation | feel-pass §2.6 | H | rover | S |
| 10:30–14:00 | dig, repair Tilly | the dig shares the cut's kit | feel-pass §3.2 (Dig) | H | gameplay + art | M |
| 14:00–16:30 | 30% warning, home | 07's Worried chirp | feel-pass §7.2 | H | rover + audio | (in §7) |
| 16:30–17:30 | the charging moment and the evening | the dock lock; the evening swell unplayed | feel-pass §7.1; §1.1 DockEveningOpened | H | gameplay + audio | M |
| 20:00–21:00 | the garage port fits the ring | the install beats | feel-pass §2.10 | H | rover + audio | S |
| 21:00–23:00 | lift and place the deck; the first tape | the "first music" moment has no closing beat | the tape's first notes fade in under a 1 s hold where the radio's static falls away; 07 chirps Happy | H | audio | S |
| 27:00–30:00 | the belly lift into the lab | the entering-home beat | feel-pass §2.8 | H | gameplay + audio | S |
| after 30 | L2, the bay, the chasm, the leap, the canyon | the leap and the install | feel-pass §2.4 and §2.10 | H | rover + art | M |

### 1.4 Friends, radio, rest, recovery, progression
| Area | NOW | GAP | TARGET | Imp | Owner | Size |
|---|---|---|---|---|---|---|
| **Friends at home** (Tilly, Bell) | idles, greet and spot events | 07 never answers a friend | 07 looks and chirps Greeting (first time each day). Tilly's spot: 07's head follows her flight. | M | rover | S |
| **Radio and letters** | tune, static, letters on the ticker | the arrival beat | feel-pass §2.11 | M | audio + ui | S |
| **Evening and day** | the dock's evening is the only "time" | there's no weather in 0.5 (events are 1.0) | nothing new for 0.5; keep the evening's light change and add its swell (§1.1) | — | — | — |
| **Rest: the wide shot, stargazing** | built (`RoverWideShotChanged`, `StargazingChanged`) | 07's idle character | after ~20 s of stargazing, one Curious-soft hum (not a full chirp), and the eye breathes slower. Never during a letter. | M | rover | S |
| **Stuck and recovery** | the recovery lift exists (heard and seen) | 07's reaction | Worried on lift-off, Relieved on set-down (feel-pass §7.2) | M | rover | S |
| **Empty battery, crawl, Tilly** | built (warnings, crawl, Tilly leads) | the power-down and relief beats | feel-pass §2.13 and §7.2 (Tired, Relieved) | H | rover + audio | S |
| **Unlock** (blueprint decoded) | reel, tape and pulse heard and seen | the node pop sound | §1.1 TechNodeChanged | H | audio | S |
| **Craft and install** | the bay arms and arpeggio | feel-pass §2.10 | (P1) | H | rover + audio | S |
| **New range** (Cell Pack II, a new mast lit) | the mast lights; new lamps appear on 07 | **no "range grew" moment**: the player isn't told visually how far they can now go | when a pack is fitted or a mast is lit, a warm ground ring rolls out to the new comfortable radius once (the same piece as SignalRadius), and 07 chirps Proud | H | world + rover | S |

### 1.5 Readability and onboarding (does the player always know what to do and why?)
- The first 30 minutes are well scripted (first-hour-v2), and M3-24 fixed the type and the cell row.
- **Gaps:**
  1. The not-yet text (§1.1).
  2. After minute 30 the next line waits ~2 min (P8, by design). Add a still-player safety: if the player is still
     for 2 min *and* hasn't progressed for 6 min, the dock evening's last pillar re-rises once (a world pointer, not
     text).
  3. **Recipe cards must say where** each missing thing can be found, in pencil-icon form (a site glyph), never a
     marker. Owner: ui + gameplay, S. Impact H.

## 2. UX beyond juice

| Topic | Finding | Proposal | Imp | Owner | Size |
|---|---|---|---|---|---|
| **Input feel and response** | holds have ease-ins (the cut's charge is 500–800 ms by design) | **every press shows a response within 100 ms**, even when the action waits: the hold ring appears on press, and the charge motes start at frame one. The ring's charge segment carries feel-pass §3's cancel. Measure input-to-first-pixel in a capture. | H | gameplay + ui | S |
| **Audio mix and silence** (pillar 6) | many new layers arrive (chirps, sizzle, ticks, swells) | a **mix budget**: at most three foreground layers; chirps duck under the radio and letters; a 2 s "silence floor" after big moments (no new one-shots), so quiet returns. One audio mix pass at the end of wave 1 with a written loudness table (LUFS per bus). | H | audio | M |
| **Camera comfort** | first-hour-v2 §7.3 rules (no shake, no roll, slow recenter, Gentle) | keep them. Every new moment camera in this pass must blend ≥ 1.2 s and respect Gentle (no lean toward the cut under Gentle). | H | rover | — (rule) |
| **Performance hitches** | unmeasured | wave 2: a hitch capture on the five likely spikes: the first cut (shader variants), lab entry (interior lights), scene reload (New Game / Quit to title), a radio hop, the dock evening. Fix: shader warm-up before the title fade, and pooled lights. Target: no frame over 33 ms in those beats. | M | director (profile) + owners | M |
| **Menus** | pause and title built, settings missing pages | §1.2 Settings. Every menu is reachable and leavable with the gamepad alone, and focus is always visible. | M | ui | M |
| **Save safety** | atomic save, `PutAway` never deletes, backups | add a visible save tick (§1.2). On quit, the game saves *before* the fade starts. A corrupt save shows a calm card: "This moon's save couldn't be read. Your older one is kept." It then loads the backup, never a blank. | H | ui + app | S |
| **Accessibility** | text size and Gentle camera exist | **rumble on/off and strength** (decided). **Colour safety:** cyan (#5FF3FF) and amber (#FFB547) separate well for protan and deutan vision by brightness and hue. Tritan vision is the risk: amber against the Peak's alert red (#FF5A6E) can merge. Rule: **no meaning is carried by colour alone**: seams pulse, pillars have shapes, chargers have rings. A captions option: "Show sound cues" puts a short word for key sounds ("listening", "low charge") in the ticker's style. | M | ui + art (check) | S |

## 3. Waves (each about one week of tokens)

### 3.1 Cost model (estimate, per standing session including its own verification)
| Size | Share of the weekly quota |
|---|---|
| S | ~0.15% |
| M | ~0.4% |
| L | ~1.0% |

Plus about 1% per wave for the Director's reviews and merges.

### 3.2 Wave 1: the first hour feels finished (target ≤ 8%)
It includes feel-pass P1 and §7, plus every H-impact S gap above. P0 (the cut) is assumed to land before, or in
parallel with, its existing budget.

| Session | Tasks | Sizes | ≈ |
|---|---|---|---|
| **gameplay** | dock lock sequence (§7.1, M); tether reach, latch and lift beats (§2.5, S); lift entering-home beats (§2.8, S); "not yet" text (S, with ui); FeelBeat publishing for P1 verbs (S); save tick hook (S) | M + 5 S | 1.15% |
| **rover** | chirp decisions and body sync (§7.2, M); drive start, stop and turn body (§2.3, M); leap crouch, stretch and squash (§2.4, S); install seat, power-on and pose (§2.10, S); low battery power-down and relief (§2.13, S); 07's reactions to friends, recovery, windows, components and new range (S); rumble profiles for all P1 beats (S) | 2 M + 5 S | 1.55% |
| **audio** | chirp bank 8 × 4 and syllable data (§7.2, M); dock sounds (§7.1, S); the 9 event gaps of §1.1 (M); window clicks, flank-lamp notes, the first-tape moment (S); mix budget pass and loudness table (M) | 3 M + 2 S | 1.5% |
| **art** | leap dust ring and coil flash; landing ring; tether dust-fall (VacuumDebris reuse, S); dock walls and lamp strips (with gameplay, S); install alignment light and power-on sweep (S) | 3 S | 0.45% |
| **ui** | pause blur-behind and audio duck (S); card and ticker motion (S); save tick (S); "not yet" line (S); the rumble settings (S) | 5 S | 0.75% |
| **world** | the reach ring for tower levels, masts and packs (S); window light steps paired with the clicks (S) | 2 S | 0.3% |
| **Director** | reviews, merges, the end-of-wave owner build | — | ~1% |
| **Total** | | | **≈ 6.7%** |

That leaves about 1.3% of the 8% for rework after the owner plays it.

**If it doesn't fit, cut in this order:**
1. the mix pass's loudness table (keep the rules, do the measurement in wave 2);
2. the chirp bank to two variants per mood (not four);
3. drive start/stop/turn to the stop beat only;
4. install power-on and first use;
5. the lift's light beat.

The dock lock, the chirps' moods, the not-yet text, the evening swell and the low-battery beats are **never cut**.
They are the owner's examples and the first hour's spine.

### 3.3 Wave 2: every screen and moment (≈ 6–7%)
- **ui:** display, controls and rumble settings pages (M); the boot, title, New Game and quit transitions (S); a
  colour-safety check and the "show sound cues" option (S); recipe-card source glyphs (S, with gameplay).
- **app / Director:** hitch profiling on the five beats, shader warm-up before the title fade (M), save-on-quit
  ordering and the corrupt-save card (S).
- **audio:** P2 sounds: listening answers, the decode tape rhythm, radio "lock", UI motion ticks (M).
- **rover:** listening anticipation (S), stargazing idle (S), browse and lab camera polish (S).
- **gameplay:** lab decode beats (S), the stillness safety re-rise (S), dig on the cut's kit (M if not in P0).
- **art:** decode and plotter sweep visuals (S); a capture review of every wave-1 beat (S).

### 3.4 Wave 3: the long tail (≈ 4%)
- Credits (ui, M).
- The radio-letter arrival polish (S).
- The map paper blur (S).
- Rest and idle details (S).
- A full first-two-hours capture review with the owner's angles. Every row in this document then gets a ✓ or a
  follow-up.
- Ready for 0.6's build pads to reuse the kit.

## 4. Acceptance for the whole pass
- Each row in §1 ends verified: a capture or a short clip of its beat, its sound heard in a build, and rumble checked
  on a gamepad.
- Re-run `tools/feel_coverage.py`: every event a player can notice is heard and seen, or listed here as drawn by its
  publisher.
- The owner plays the first hour after wave 1. Every moment they name gets a row and a wave, not a one-off fix.
