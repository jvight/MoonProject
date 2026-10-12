# Director handoff (2026-10-10)

Read this, then `CLAUDE.md`, then `docs/VISION.md`, `docs/DESIGN.md`, `docs/ARCHITECTURE.md` and `docs/ROADMAP.md`.
It records how the project is run, where it stands, and what comes next, so a new Director session (on any
account) can continue without the old conversation.

## 00000000000. LATEST (2026-10-13 ~00:50): 0.5.3 released; 0.5.4 landing (art on main)
- Director is session local_0b7c61a2 ("MoonProject Director handoff"); cron v4 = job 27877d8f (:17/:47).
- **0.5.3 released:** ui zero-GC fix e31c8ebf landed alone (7599f185), verify PASS + pushed,
  `Builds/Lunarion-0.5.3-20261012-7599f185.zip` (smoke PASS).
- **0.5.4 art landed:** ae455d84 via temp branch box/art-054 + its outputs 1defabe8; verify PASS + pushed.
- **Land queue:**
  1. rover box/rover 4cbf90af+ (box_check PASS; brightness 3df1c7e4), plus art 7bf91871 (deck lamps out from
     under the wing). Asked rover to merge 7bf91871, re-run box_check and send a mid-charge capture; they land
     together.
  2. gameplay Phase B box/gameplay 9cf6ef1f (box_check + Playthrough PASS);
  3. ui Tab screen box/ui (asked for hint.build/Build prompt, Focus Lens + Side Bins tech nodes);
  4. story text keys and audio cargo beats (dispatched, land after gameplay).
- **Lesson:** `land --batch <temp-branch>` picks builders by box name; a temp name like `art-054` matches none, so
  only Main Scene runs. Pass `--filter "^(Art|Gameplay)/"` (the real box's BUILDER_DOMAINS).
- Open gameplay deviations for later: waiting bundles silent to sonar; crate cuts don't hold 07 still; a general
  MaterialsChanged (0.6). From ui: a general Recipe, shelf cost display, per-slot fill tick.

## 0000000000. (2026-10-12 ~22:50): 0.5.3 one fix from release; 0.5.4 branches ready
- **main 9700010f** (version 0.5.3).
  - Verify: everything PASS (the Playthrough too, after the timeout fix ea... in GoldenPathPlaythrough: TestTimeoutMs
    1,800,000 above TotalBudget 1580 s) except UiAllocationTests.PromptFollowingAMovingPoint_DoesNotAllocate (2666 >
    2634 bytes).
  - ui was asked for a standalone zero-GC fix on main (cache the parts-chip and noun strings).
  - Next: land it alone, `verify --parallel --push`, then `tools/build_player.py` in MoonProject-wt/director and copy
    the zip to D:/Project/MoonProject/Builds.
- **Ready to land after the 0.5.3 build, in this order:**
  1. art ae455d84 (shelves Slot_00..11 + Stack_0..2, ShelfCrate_<Material> incl. OrangeBeads, CrewCrate
     Lid/CutPoint/Seal, the tower rings + plaque, the Focus Lens, ChargeRun_0/1 rows, Kit_SideBin, blueprint sheets);
  2. rover (box/rover: rows collected across parts, lens, side bins; needs art first);
  3. gameplay Phase B (box/gameplay: cargo, unload, Tilly, crates, cutter tiers, IChargerReach, crew sets, chart
     pencil marks; needs art's nodes and blueprint sheets);
  4. ui's Tab screen (in progress).
  Audio's 0.5.4 cues are already on main.
- **Owner ideas, designed** (M3-25/26/27 on main): the inventory and shelves, the cutter tiers with Godzilla-style
  charge lamps, the materials families. 0.6 is the big lunar-materials and build-pad wave.
- **Token cap lifted** (cron v4: notices at 85/95% only). Weekly ~46% on 2026-10-12 22:50; resets 2026-10-18 02:00.
- This Director's context is ~84%; a new Director should start here and recreate cron v4 (CronCreate is
  session-only).

## 000000000. (2026-10-12 ~21:25): 0.5.3 verify FAILED (my land order); fix pending
- **Main is broken at boot:** "No ICutter service registered".
  - I landed audio's 0.5.4 (FeelAudio Get<ICutter>() at init) before gameplay registered ICutter. The 0.5.3 verify
    of 318c3c68 failed: 8 PlayMode tests and the Playthrough.
  - The fix was asked of gameplay: a standalone commit registering ICutter at tier 0 (M3-26 values). Land it alone,
    re-run `verify --parallel --push`, then build 0.5.3 (Builds/).
- **Landed since 0.5.2:**
  - 0.5.3: copy pass, tower online, pickups, credits + Licenses, Settings tabs + Display, AA glass, pickup feed,
    slots removed;
  - 0.5.4 parts: Core contracts (cargo, cutter, materials, IChargerReach), material name keys, audio 0.5.4 cues,
    world vent/anchors.
- **Running 0.5.4** (on box branches, not landed):
  - gameplay Phase B: cargo, unload, Tilly fetch, crates, cutter tiers, chart pencil marks, IChargerReach, crew sets;
  - rover: charge-run rows, sag, tier install (778b868d+);
  - art: tower rings + plaque, shelves with Slot_00..11 + ShelfCrate_<Material>, crew crates with Seal, Focus Lens,
    ChargeRun_0..3 lamp rows, the orange glass bundle;
  - ui: the Tab screen (hold, shelves, "07 làm được", unlock cards), materials via IMaterialCatalog.
- **Lesson:** before landing a box whose code Get<T>()s a service, check that the registering box has already landed
  (grep main for Register<T>).
- This Director session's context is ~80%. A fresh Director starts here.

## 00000000. (2026-10-12 ~20:15): token cap lifted; 0.5.3 closing; 0.5.4 started
- **The owner lifted the 50% cap** ("just go"). Cron v4 (job 6c90db20, session-only) logs usage, tells the owner at
  85%/95%, and checks stalls. It never stops sessions.
- **0.5.3 (closing):**
  - landed: the copy pass (writing-style.md), the tower comes online (Build beats ~6 s), pickups on E, the tether gated,
    relay names;
  - ui is finishing: credits plus the Licenses/ folder (build_player ships them), Settings tabs plus Display,
    Moonlight Glass AA 4.5:1 legibility, the overlaps, the slots removed, pickup notifications, the reward card,
    recipe rows.
  - Then verify, build 0.5.3.
- **0.5.4 (started):** the M3-25 cargo and storage, M3-26 cutter tiers 0-1, the M3-27 materials foundation, and the
  first-hour QA leftovers.
  - Phase A: gameplay writes the Core contracts (authorised) and reports them for review.
  - In parallel: art (tower lamp rings plus plaque, shelves, crates, the Focusing Lens look, the orange glass field),
    rover (the charge-run lamps, the "Godzilla" idea), audio (the charge-run notes).
  - Next: ui (the Tab screen: hold, shelves, "07 can", unlock cards) and world (map crate marks, the orange glass
    field) after the contracts.
- **Design docs on main:**
  - M3-25 cargo (revised with Lunarion twists: the load on 07's body, open shelves, Tilly fetches, crew crates);
  - M3-26 cutter progression (the charge run, 4 tiers, segmented pieces);
  - M3-27 materials (salvage vs lunar families, 8 raw + 5 refined by 1.0, auto refining at home);
  - first-hour-qa.md;
  - feel-pass §8.
- Memories: `learn-then-improve`, `plain-functional-text`, `ui-taste-subnautica`, `standing-sessions`.

## 0000000. (2026-10-12 ~17:00): 0.5.2 released for the owner's play-through
- **origin/main = a1263860, verified** (compile, EditMode, both PlayMode shards, the Playthrough). Builds:
  - `Builds/Lunarion-0.5.1-20261012-c179a545.zip`;
  - `Builds/Lunarion-0.5.2-20261012-a1263860.zip` (smoke PASS).
- **What 0.5.2 has:**
  - the feel pass P0: the cut's charge-up, ignition, heat seam, vacuum debris, crack, tumble, fold, count roll, and the
    cut work framing;
  - polish wave 1: the dock lock, 07's machine chirps, drive/leap/install/low-power body beats, rumble, the reach
    ring, window steps, the save tick, pause blur, and the silent events voiced;
  - the Moonlight Glass UI (Subnautica grammar, owner pick #1).
- **Next:** the owner plays 0.5.2. Every moment they name becomes a polish-pass row (waves 2-3: settings pages,
  transitions, hitch profiling, credits screen, P2 verbs). Then 0.6 build pads (M3-22) reuse the feel kit.
- **Deferred (wave 2):**
  - the install sweep along the part's surface (needs the part's renderers via Core);
  - per-window lighting (HomeBase has one Windows renderer);
  - turn roll and terrain rumble;
  - Relieved on Tilly's arrival (no event);
  - a credits screen (CREDITS.txt files exist in UI/Fonts and Audio/Music/Voice);
  - pause tabs with the map/cassette pages;
  - Colab ideas (owner asked): an emotive TTS for Ro's recordings (Chatterbox, MIT) and Stable Audio Open for
    ambience. Check licences first.
- Lesson: a box that edits another domain's folder must run that domain's tests (art's swap broke 44 Gameplay
  PlayMode sessions); box_check now gives art the Gameplay builders and tests.

## 000000. (2026-10-12 ~14:40): 0.5.1 verified and pushed; 0.5.2 branches ready to land
- **0.5.1:** clarity + voice landed; version 0.5.1; `verify --parallel --push` PASS; origin/main = c179a545. The
  build is running in MoonProject-wt/director (copy the zip to D:/Project/MoonProject/Builds). Don't land while it
  builds: land --batch uses the same worktree.
- **0.5.2 ready on box branches** (all box_check PASS). Land order: rover -> ui -> gameplay -> world -> art (then art's
  swap from CutSparks) -> voice (audio), then wire count_tick and window_click, verify, build.
  - rover f4f0f874+;
  - ui b1083618 ("Moonlight Glass" UI, owner pick #1, Nunito Sans + Exo 2 OFL);
  - gameplay 37dc6640+ (a dock Anticipate tweak running);
  - world 6129cab3 (check that HomeLighting's dock tint survives in the asset);
  - art 6459cf09;
  - voice/audio 7d94543d.
- The owner's UI taste: memory `ui-taste-subnautica`. Owner rule: standing sessions only (memory `standing-sessions`).
- Lesson: never detach or check out other commits in the verify worktrees (director, director2, landtest). A test
  run there left an untracked meta that broke `verify` (director2 must stay on director/verify2).

## 00000. (2026-10-12 ~09:00): 0.5.2 "Satisfying" P0 dispatched; 0.5.1 still closing
- **Owner rule (memory `standing-sessions`):** no new session per task. Work goes to the standing sessions (game
  design, gameplay, art, rover, ui, world, map, tools, story & flow; voice becomes **audio** after it lands). The 12
  finished slice sessions were archived with the owner's consent.
- **0.5.2 feel pass:** `docs/design/feel-pass.md` (game design session). The owner approved P0 (the cut + collection:
  charge-up, ignition, hot seam, vacuum debris, crack, tumble, fold, count roll) and rumble on by default (cap 0.35).
  The Core contract is on main 745f5c0d: `FeelBeat`/`FeelKind`/`FeelPhase`/`IFeelSettings` (§6 says who publishes
  what).
- **Running on P0:**
  - gameplay (box/gameplay): the cut state and beats;
  - art (box/art): the VFX in `Gameplay/Feel/` and the swap from CutSparks after gameplay lands;
  - rover (box/rover): body nudges, camera lean, RumblePlayer, IFeelSettings;
  - ui (box/ui, Sonnet): CountTick roll + event, the ring's charge segment, rumble settings.
  - Audio starts after voice lands.
  - Land order: gameplay -> rover/ui -> art (swap) -> audio.
- **P0 tips (accepted, waiting for 0.5.1 to ship):**
  - rover 36d6e741 (registers IFeelSettings; UI fails fast without it, so it lands first);
  - ui 44fb5be5 (new `StockCountTicked` in UIEvents);
  - gameplay 9bd1a780 (+ a Dig-beats follow-up running);
  - art a4c300d0 (readability tweaks and a GC test running; the swap from CutSparks comes after gameplay lands).
  - Land order: rover -> ui -> gameplay -> art swap -> audio.
  - After landing: rover adds a 0.05 rumble per `StockCountTicked` unit; audio subscribes to FeelBeat and
    StockCountTicked.
- **Polish wave 1 approved** (`docs/design/polish-pass.md` §3.2, ~6.7%): feel-pass P1 + §7 (the dock lock, 07's
  machine chirps via ChirpMood/RoverChirpRequested/RoverChirpSyllable) + the high-impact gaps.
  - Dispatched on the same box branches as P0: gameplay, rover, art, ui, world.
  - Audio follows after voice lands.
  - `tools/feel_coverage.py` maps which Core events are seen and heard.
  - Waves 2-3 come next week.
- **Clarity calls (2026-10-12):**
  - keep OneTripArrival 0.25 and the battery tuning;
  - make the pilot line up once per seam; if it still arrives under 25%, move the garage up to 40 m toward home;
  - fix the recipe card's gating;
  - the tether ring: x0.65, lower behind the head, brass.
- **0.5.1 still to close:** clarity (running), then voice, then §8.2 doc edits, verify --parallel --push and a 0.5.1
  build.

## 0000. (2026-10-12 ~07:00): 0.5.1 "Clarity" in progress
- The owner played 0.5.0: UI clipped on 21:9, loose ground pieces odd, purpose/crafting/tower unclear, lines not
  understood, music should be earned, Space's sonar unclear, tether given from the start, charging too fast, UI hard
  to read, the camera's auto-recenter makes players dizzy.
- Specs: `docs/features/M3-24-clarity.md` and the owner-approved `docs/design/first-hour-v2.md` (§7.1 tasks per slice).
- Running slices (chips): `ui-fit` (Sonnet high), `clarity` (Opus medium), `voice` (Opus medium; must ask the owner
  before downloading any TTS engine), `first-hour design` (Opus high; adding a §camera section: auto-recenter rules,
  a Gentle camera option — after the owner approves it, dispatch a rover slice for the camera).
- **Landed:** ui-fit A (screen shapes, 5420b220). **Ready:** voice @ 102de0ba (box_check PASS; English voices only,
  sherpa-onnx + Piper at D:/Tools/sherpa-onnx; land it AFTER clarity, it carries clarity's Core commits; the credit line
  "Station voice synthesised with a Piper model trained on LibriTTS-R (CC BY 4.0)." must reach the credits screen;
  owner to listen to Assets/_Project/Audio/Music/Voice/letter_mast_5.wav and letter_mast_1.wav). **Running:** clarity
  (first-hour §8.1 tasks 1-10), ui-fit (§8.1 legibility tasks). **Camera landed** (first-hour §7); optional follow-up on box/camera: the rendered-heading drift (~17°).
- **Director to do:** §8.2 of first-hour-v2.md (VISION/GDD/M3-16/M3-20/M3-21/ARCHITECTURE text edits); land the three
  slices (full box_check + Playthrough), verify --parallel --push, then build 0.5.1 for the owner.
- Cron guard v3 (50% hard limit) runs in the current Director session only; this session's context is ~92% full:
  start a fresh Director session from this handoff.

## 000. (2026-10-12 ~05:20)
- **0.5.0 released for review:** origin/main = 13faaec7 (verified: compile, EditMode, both PlayMode shards, the
  Playthrough at spec battery tuning, 1457 s, no crawl). Build `D:/Project/MoonProject/Builds/Lunarion-0.5.0-20261011-13faaec7.zip`
  (smoke test passed). Save floor 0.5.0: older saves are put away to `.old-<stamp>.json`.
- Waiting for the owner's play-through and review of 0.5 before 0.6 opens (phased releases). No slice is running.
- The two verify failures of §00 were fixed (battery's tow snap, look's OpeningSessionTests teardown).

## 00. NOW (2026-10-12 ~01:30)
- **Director session** "MoonProject · Quản lý" `local_39353459-ab44-4d1f-b1db-4b1fa0a39a36`. Cron guard v3 is session-only
  (recreate it): usage log, wrap up at 46%, stop all at 48% — **the owner's HARD LIMIT is 50% of the weekly quota.**
  Model/effort per task (memory `model-effort-policy`): Sonnet for mechanical work, Opus medium for slices, Opus high
  for hard bugs; new chips start at Opus xhigh, so set them right after the owner clicks.
- **origin/main = f1ac7fb2** (lab + battery A, verified). **main = 3dfd6e62**, landed since and NOT yet verified:
  goals (M3-21), opening polish (light pools as decals, dock + spawn 4.5 m south, O1-O14, lens glass, first prompt,
  title radio, title glint veil, hop home lands on the dock), cleanup (box_check/verify exclude the Playthrough from
  PlayMode steps, trial-merge abort, orphan-asset warning in land, ReserializeAssets, Ro's letters dated, the
  **Lunarion** rename with a one-time save copy from `.../Lofi Lunar`), look (M3-18 finish: skirts, caked dust, garage
  232°/140 m, Tilly 60-95°/80-110 m), landmark pilots (L1), the Lunarion wordmark on the title (SVG → Painter2D).
- **Verify on 673d38a9 failed twice, both assigned:** the golden path's tether snaps once on a tow home (battery owns
  the route; fold into milestone B) and WorldBootTests counts 449 prop colliders instead of 195 (a Rover PlayMode test
  leaves a scene behind; the look session is fixing the teardown). Re-run `verify --parallel --push` once both land.
- **Running:** battery milestone B (light bar approved; Cell Pack II, mast ring, reshaped route at spec 4/120/60,
  the tether snap). look (the teardown fix). Done and resting: goals, cleanup, opening polish, lab-build,
  lab-systems, blender trial.
- **Owner decisions today:** M3-22 is 0.6; GDD v3 §12 Subnautica lessons (fragments, Kestrel-3 as our Aurora, radio
  pencil marks, the mid-game Earth moment); GDD v3 §13 "the long silence" (humanity long gone, signals are recordings
  and machines, a dark open ending with rover 08 and Ro's last recording; every function shows on 07); the game is
  **Lunarion**; Blender is parked until 0.8 (`box/blender` kept, `docs/design/trial-blender-svg.md`, Blender 4.5.9 at
  `D:/Tools/Blender`); the v2 plain wordmark.
- **0.5 still to do:** battery B (ship blocker: spec tuning + route), a full verify + push, then the release step
  (bundleVersion 0.5.0, save floor, build zip `Lunarion-0.5.0.zip` to `D:/Project/MoonProject/Builds/`, owner review).
  Backlog: M3-18's two open look targets (World grade + foreground), PlayMode test for landmark pilots, Mobile pools.

## 00b. Earlier on 2026-10-11 (~11:20)
- **Director session:** "MoonProject · Quản lý", id `local_39353459-ab44-4d1f-b1db-4b1fa0a39a36` (took over at ~10:45
  from `local_74e38880…`, which was told to delete its cron and stop). Its cron guard (every 10 min: usage log, wrap-up
  at 23%, stop at 24%, stall check) is session-only: recreate it in a new Director session (§6, §8).
- **main = 4fe6a83e**, landed with `land --batch`:
  - GDD v3 §10 edits: VISION (pillars as GDD §1, anti-goal, ruling 7, new **rulings 15 "the battery is range" and 16
    "a few main targets, everything else free"**), DESIGN (why people come back, station reach and the battery,
    regions, base life and pads), M3-06 charge pads, M3-11 kit slots, M3-16 dock line and amendment note, cutscenes
    O5's boot readout;
  - specs: `docs/features/M3-19-the-lab.md` (final; the draft is removed), `M3-20-battery-chargers-crawl.md`,
    `M3-21-two-tier-goals.md`, `M3-22-build-pads-structures.md`; ROADMAP's version table per GDD v3 and rows
    M3-19…M3-22;
  - Core contracts: battery (`IRoverBattery`, `ChargerKind`, `BatteryWarning`, `BatteryEvents.cs`,
    `RoverLeapRefused`), tech tree and goals (`TechNodeId`, `TechNodeState`, `ITechTree`, `RecoveryTarget`,
    `IRecoveryProgress`, `ProgressEvents.cs`), the lab (`ILabInterior`, `LabStation`, `LabEvents.cs`);
  - tools: box_check and land know the slices `battery`, `lab-build`, `lab-systems`, `goals`.
- **Verified and pushed 0364a6d8 (~15:30):** origin/main went 57fb8c1f → ff16a246 → 0364a6d8. It holds the specs and
  contracts, lab-systems' Core renames, the hatch sounds, **battery milestone A** (BatteryTuning.asset at interim 8
  cells / 400 m; spec 4 / 120 with the reshaped golden path is milestone B and a 0.5 ship blocker), the fixture and
  timing-test fixes.
- **M3-19 LANDED and pushed (f1ac7fb2, ~19:10):** lab-build + lab-systems together (the habitat lander, belly lift,
  lab stations, reader decode and hand-off, ring camera, dock on 07's spawn), WorldSettings written out. Both lab
  sessions are done. Leftovers (polish backlog): lab wall linings, reading lamps' restore, outdoor ambience ducking
  inside, DOF in the station focus view, the save floor with the 0.5.0 bump (release step), box_check's PlayMode should
  exclude the Playthrough category (it ran it via --assembly and hit 40 min).
- **Running sessions (chips the owner clicked):** `goals` (M3-21) and, if clicked, `opening polish` (box/opening: the
  headlamp pool cut along terrain chunk z=0 under Forward+, the dock inside the lander's long shadow, O1-O14 re-walk on
  the new home, the opening leftovers, a before/after contact sheet).
- **battery milestone B** (stopped for 2026-10-11): the lid-edge light bar for the flank lamps (they don't read from
  the chase camera), the reshaped route (decision sent: dock between trips, relay.2 lit on the first chasm visit and
  its cabinet's Cell Pack II tube, Cell Pack II before the leap, relay.3 and a hop home after Bell), Cell Pack II and
  the spine, the mast ring art; tests that aren't about the battery must not crawl at spec tuning.
- **Owner decision (2026-10-11): M3-22 (build pads, structures) is 0.6.** 0.5 = battery, chargers, crawl, Cell Pack
  II, the lab (M3-19), the recovery panel and tech tree (M3-21). No structures, no T2.
- **Dispatched as chips (fresh feature-slice sessions; the owner clicks them):**
  1. `battery` (M3-20, all six domains, milestones A playable / B art, Cell Pack II, audio, world);
  2. `lab-build` (M3-19 art + world: the habitat lander from `box/art-sketch-m319` with art's 4 refinements, the four
     stations, porthole lamps, declutter, the map's art leftovers, HomeLight entries; publishes "Contract: the lab
     (M3-19)" in ARCHITECTURE first);
  3. `lab-systems` (M3-19 gameplay + rover + ui + audio: lift re-aim, turntable, reader decode and hand-off, cameras,
     the map trigger and P6 MarkSeen; owns the Core renames `BasePowerStage.Lift→Chart`, `AtLookout→Inside`,
     `LiftCue` hatch cues).
  - When lab-build reports its ARCHITECTURE contract: land it at once (or tell lab-systems to merge box/lab-build's
    commit) so lab-systems wires real node names.
- **Next chips to offer:**
  - `goals` (M3-21) once lab-systems has landed M3-19's reader (it needs `FindDecoded`): tech tree and recovery
    services, two-tier next line, Hover-Jump tube at the chasm's near lip (pre-pinned sheet removed), Link flag on
    relay content (relay.0, relay.1), the components rename, the chart's charger circles and Link line, the free-order
    test.
  - A small Sonnet `cleanup` session after battery's milestone A (to avoid rename conflicts): the lint duplicate-type
    list (§0 below), World's flaky `MoonSurfaceTests.Sampling_IsFastEnoughForBootTimeMeshing` (wall clock), the
    opening's leftovers (art's lens glass ring reflecting Earth on the pull-out; UI's O14 prompt rule "Look up"/"Drive"
    from `WakeBeat.ChaseFrame`; the save's station playing softly on the title), and a `ReserializeAssets` automation
    entry to write out `RoverCameraTuning`'s OpeningSettings and `WorldSettings.LensView` (new fields only, defaults
    apply meanwhile).
- **Sessions:** map and opening are closed and idle with large contexts (map 559k); do not wake them, their leftovers
  are in the chips above. Resting with large contexts: gameplay, rover, art, ui, world, tools, game design, story &
  flow. box/rover's 4 commits net to zero (two commits and their reverts); nothing to land.
- **Tokens:** cap 25% of the weekly limit for 2026-10-11 (the week started 02:00 Sunday local). At takeover ~11:00 the
  weekly meter read 12%.
- **Release checklist for 0.5** (unchanged): raise the save floor, set the version to 0.5.0, verified build zip in
  `D:/Project/MoonProject/Builds/`, owner review.

## 00a. Before the takeover (2026-10-11 ~09:45)
- **origin/main = 57fb8c1f.** It passed the full verify (compile, EditMode, PlayMode in shards, the 0.5 golden path,
  steps 0–8).
  - Since then main has landed:
    - world's halo, ruts, pad and lift_lookout fixes, with WorldSettings reset in batch (ea404ee9);
    - the map and opening Core contracts;
    - GDD v3 and its review (docs only).
  - Land every change with `director.py land --batch`. For World tuning, then run in MoonProject-wt/director:
    `unity_batch exec ResetSettings`, then RunBuilders `^(World/|Main Scene$)`, commit, and fast-forward main.
- **Owner direction (overrides VISION and DESIGN until they are rewritten):**
  - Progression first (07's growth, tech, crafting, going farther).
  - The battery is fuel, under the calm rules.
  - Not linear: a few main recovery targets with a marker; the rest is free exploration.
  - Buildable operational structures on predefined pads (charge pylons, solar array, fabricator, store, bay
    extensions).
  - The lander's inside is the crew's lab, reached by M3-19's belly lift (the owner liked the sketch on
    box/art-sketch-m319; art's 4 refinements apply: cabin about 5.6 x 2.5 m, octagonal weathered panels, tapered roof,
    tower taller than the lander).
  - Ro answers at the end, and the opening gets a ~2 s boot readout.
- **GDD v3 APPROVED on main (565b49e1)** as `docs/design/gdd-v3.md` and `-vi.md`. §11 has the owner's answers, §10
  the exact edits to VISION, DESIGN, M3-16, M3-06, M3-11, cutscenes and ROADMAP.
- **0.5 scope:** the battery, chargers, the crawl with Tilly's lead, Cell Pack II, the lab (M3-19) and the recovery
  panel. Structures and T2 are not in 0.5.
- **The opening slice landed** (O1–O14, the lens view, the title camera and push-in, the short wake, the call sign,
  the wake sounds). The opening session is now doing: O10's swing, the owner-approved boot readout in O5, and
  pause ducking.
  - Still to dispatch: art's lens glass ring (it reflects Earth during the pull-out); UI's O14 prompt rule ("Look
    up" or "Drive") from WakeBeat.ChaseFrame; the save's station playing softly on the title.
  - Backlog: World's MoonSurfaceTests.Sampling_IsFastEnoughForBootTimeMeshing flakes under load (a 2.0 s
    wall-clock limit).
- **Next for the Director:**
  1. Land the approved GDD v3.
  2. Rewrite VISION and DESIGN where GDD v3 §10 says so.
  3. Write the specs: M3-20 battery, chargers and crawl; the final M3-19 (lab and belly lift); M3-21 the two-tier
     objectives; M3-22 build pads and structures. Agree with the owner what is 0.5 and what is 0.6.
  4. Dispatch: art (the final habitat lander), gameplay (battery, objectives), rover (gauge, crawl, the lift into the
     lab), ui (battery HUD, objectives), world (charge pads), the map slice (map in the lab; its deck-hatch step was
     paused), the opening slice (the boot readout).
  5. Verify and push.
- **Sessions** (group "moonproject"):
  - live slices: opening (cutscenes §1 and the title camera), map (M3-07 generic parts);
  - resting with large contexts: gameplay, rover, art, ui, world, tools. Start fresh sessions for new features rather
    than reviving them;
  - design: game design, story & flow.
- **The map slice's generic half landed (M3-07).** MapKnowledge and its save, the MapSheet, ISightline,
  MapSystem.OpenLook/CloseLook, the UI view, and 42 keys of Ines's notes.
  - Still to wire, with M3-19's lab:
    - the trigger (OpenLook/CloseLook in the cabin);
    - P6's evening pillar must call MapKnowledge.MarkSeen;
    - art: drive LightTable, LightTableGlow and the ChartPen from MapLookStarted/Ended and MapInkCued;
    - rover: the pose and lens toward the look;
    - audio: pencil hiss, pentatonic pen scratch, table hum;
    - world: the table glow through IHomeLights.
  - The deck-hatch flow was removed; ISightline waits for a lookout.
- **Tuning not yet written out** (the editor's MCP was down): RoverCameraTuning's OpeningSettings additions and
  WorldSettings.LensView. They are new fields only, so their defaults apply; re-serialise them when the editor is
  reachable.
- **Cron guard is session-only:** recreate the token and stall guard in a new Director session (see §6 and §8).

## 0. Current state on the new account (2026-10-11 ~00:50)
- **main 85bb390f.** It holds:
  - every box tip from the old account (merge 029b4807);
  - world's scene-build fix (1db8604b). HomeLightingSystem made its MaterialPropertyBlock in a field initializer,
    which Unity refuses inside a MonoBehaviour constructor, so Main.unity could not be built;
  - the outputs and the tuning written out (505ac8b4, 0fdefef4);
  - the Core contracts BlueprintsCarriedRestored and DockEveningOpened (85bb390f).

  `verify --parallel --push` was started on 85bb390f.
- **The cut-off sessions' uncommitted edits are still in their worktrees.** New sessions start by finishing or
  discarding them. They were offered as chips: gameplay, rover (the lift slice), art and ui.
  - gameplay owns Rover/Rig/TubeCarry.cs for P1 and for the tube after a load.
  - Audio and world have no session:
    - audio waits for gameplay's DockEveningOpened (it wires dock_evening_swell);
    - the Director re-runs world's wake capture.
- **verify on 85bb390f.**
  - EditMode passed. PlayMode failed 50 of 251:
    - 49 of them are in Rover and share one root cause: TestRover.Spawn does not wire RoverKit's BlueprintTube (from
      0579f46b). Rover was asked to fix it first.
    - One is in gameplay: BaseAndSaveSessions.Deposit_TowedRelic…; gameplay was asked.
  - Nothing was pushed.
- **DIRECTION CHANGE, owner (2026-10-11 ~09:15), overrides VISION/DESIGN where they conflict:**
  - Progression first: the game is about 07's growth (upgrade, unlock, craft new tech, go farther); the crew's
    memories are seasoning.
  - The battery is fuel: 07 must return to charge, and upgrades extend range. The Director's calm rules: no death
    or loss, a low-power crawl to the nearest charger, the dock and lit relays as chargers, a diegetic gauge.
  - The whole game is being re-sketched as `docs/design/gdd-v3.md` (story & flow), with a Vietnamese review
    edition `gdd-v3-vi.md`; game design reviews it next, then the owner approves.
  - M3-19's lift under the belly stays, and the cabin becomes the crew's lab.
  - Until approval, only work that survives any redesign continues: the opening slice and the map slice's generic
    parts.
- **VERIFIED AND PUSHED 57fb8c1f (2026-10-11 ~08:50):**
  - The steps passed: compile_check, EditMode, both PlayMode shards and the 0.5 golden path (steps 0–8).
  - origin/main went from 370bfc9c (0.4.2) to 57fb8c1f.
- **Owner review, morning:** the lift must go under the lander's belly and carry 07 inside. The draft is M3-19
  (`docs/design/drafts/M3-19-inside-the-lander.md`, on box/design).
  - The owner asked to see a blockout before approving, and art is building it on a sketch branch.
  - Dock in front of the belly: chosen by the owner.
  - The map slice's deck-hatch step is paused until M3-19 is decided.
- **MILESTONE, main 8b4c0e51 (~03:20):** the golden-path Playthrough follows M3-16 steps 0–8 end to end, the lift
  included, in 1241 s (budget 1340 s). 0.5's core acceptance is met.
  - Blockers before a verified push:
    - LiftSession: the deck ignores drive input after ~7 s at rest (rover);
    - NewGameSession must boot with the title on (gameplay);
    - the shared input harness against focus resets (tools).
  - Then run `verify --parallel --push`.
  - Still to build in 0.5:
    - art's M3-18 half (shader rim, vertex AO, skirts, V8, the L1/L2/L4/L7 meshes, PartSocket);
    - the opening slice (chip offered);
    - M3-07 the map slice (chip offered);
    - M3-09 Part B, the collections;
    - audio's queue;
    - the layout pass (L3, L8, L9, V5).
- **verify on 85bb390f, final result:**
  - Playthrough PASS: the golden path still runs end to end with the garage fix and every tip.
  - PlayMode FAIL, only from the rover rig.
- **M3-18 look baseline** (main 094c35e3, `tools/look_report.py capture`):
  - the p5–p95 spread is 0.23–0.28 on all six frames (target ≥ 0.40);
  - ground flatness is 0.21–0.62 (target ≤ 0.30).
- **Sessions running (2026-10-11 ~01:10).**
  - Build boxes: gameplay (P2, P6, P9, Playthrough 0–8), rover (rig fix, carried part, the lift slice, motes), art
    (lift props, then M3-18), ui (voice, V11, NeedsPart), world (M3-18), tools (lint and box_check landed; land
    --batch and the look set next), opening (slice, cutscenes §1; chip offered).
  - Design: game design (done; waits for the end-to-end first-hour review) and story & flow (game-flow.md, then
    amendments to the M3-07 and M3-09 drafts).
  - Every box runs `tools/box_check.py <box>` before it reports.
- **Text from story & flow, ready to port** (`docs/design/text/`: crew-logs, radio-letters, crew-map,
  title-and-collections):
  - ui ports it into the tables and checks that Be Vietnam Pro has ♪ and ✓;
  - gameplay (after its Playthrough item) places the 3 new log caches (garage, drill, supply lander) and
    releases the letters, one per mast for the first 4 masts, then 2;
  - world registers region.peak and region.terraces with M3-07's MapSheet;
  - the owner's native read of the Vietnamese is welcome.
- **Small follow-ups:**
  - The lift's restore must hide `LiftPlatform/JamProps` (art's OUT OF ORDER board and cable end) together with
    `LiftCableR_Snapped` (Gameplay/Lift, the next lift session).
  - Rover moves the story parts to art's PartSocket once it lands.
  - The input-event discard fix in the shared harness (tools d2427c96) still needs its proof runs reported and
    landing.
- **Audio has no session; its queue:** dock_evening_swell on DockEveningOpened; cues for StoryPartFound and
  StoryPartDelivered; the lint renames SignalField and FakeRoverSystem (gameplay renames its copies instead).
- **Lint duplicate-type fixes still to assign** (report-only in tools/lint.py until the list is empty; give each box
  its line at its next report):
  - rover: LiftCue (in queue), RoverBayTests and RoverKitTests vs art, StargazeTests vs ui;
  - art: SalvageMaterial (Core vs Art.Editor);
  - audio/gameplay: SignalField, FakeRoverSystem;
  - ui/world: RelaySettings;
  - gameplay/ui: MotionTests;
  - ui: FriendUiTests (EditMode vs PlayMode).
- **Tokens.** The cap for 2026-10-11 is 25% of the weekly limit (the week resets at 02:00 local). A cron guard runs
  every 10 min.

## 0a. FINAL STATE when the old account hit its limit (2026-10-10 ~23:58)
- **main e8066680** holds the 0.5 first batch and docs. It is NOT verified and NOT pushed (origin is at 370bfc9c, 53
  commits behind). Push only after `director.py verify --push` passes; try `--parallel`.
- **Box tips and uncommitted work.** The sessions were stopped mid-turn, so worktrees can hold uncommitted, unfinished
  edits. Review each `git status` before continuing: finish and commit, or discard.

  | Box | Tip | Uncommitted files | Latest |
  |---|---|---|---|
  | art | 2cf00c36 | 3 | the tower's three story parts (P1); before that the garage fix 413f532 and the QA pass eb423a5. Next: the lift's OUT OF ORDER tag |
  | gameplay | c0ed4f94 | 12 | P7 plotter delivery. In progress: BlueprintsCarriedRestored, the Playthrough with art 413f532, pads, P1 |
  | rover | 7bf4f722 | 10 | the lift cycle (dormant → restored) in Gameplay/Lift; the tube on 07's back (0579f46b). Next: the ladder look with lift framing, the ride and the lookout |
  | ui | 90f48074 | 3 | P8 (the next line waits 2 min); blueprint lines. Its PlayMode batch was still running |
  | audio | 7da01984 | 0 | lift, plotter, chime and evening sounds; tests confirmed. Next: wire DockEveningOpened |
  | world | 1f93d242 | 0 | cables and the tower pilot; re-run the wake capture (commands below) |

- **First moves for the new Director:**
  1. Run `python tools/director.py status`.
  2. Clean or commit each worktree.
  3. Land boxes as they reach clean, tested tips.
  4. Re-run the golden-path Playthrough with gameplay and art.
  5. Run `verify --parallel --push`.
  6. Ask the game-design session to review the merged 0.5 once it plays end to end.

## 1. Where it stands (updated 2026-10-10 evening, account at ~95% of its weekly limit)
- **Shipped and pushed:**
  - 0.4.0: 50990e8.
  - 0.4.1 (New game, stargazing): 98f6edd.
  - Playtest zips are in `Builds/`.
- **0.4.2 SHIPPED** (rust with causes, M3-15 §3): pushed 5a293e8, verified with EditMode 1179, PlayMode 232 (13 skipped) and the Playthrough. Build: `Builds/LofiLunar-0.4.2-20261010-5a293e84.zip`.
- **0.5 groundwork already on main** (inert until gameplay lands):
  - Core contracts: BasePowerStage, IBasePower, BasePowerChanged, NextStepChanged, PlaceFirstApproached, HomeLight,
    IHomeLights;
  - World's HomeLightingSystem (registers IHomeLights);
  - the design review (`docs/design/review-2026-10-10.md`) and the adopted amendments in M3-16.
- **0.5 FIRST BATCH LANDED on main** (2026-10-10 evening; merges up to 951d6db6, outputs 4d9d7646, BlueprintDelivered
  09d8cc0e, 3 batch slots 3e3a1a63). NOT verified and NOT pushed: the Playthrough still needs gameplay's P9 run. It
  contains:
  - gameplay A–F, P4, P9/P10 groundwork;
  - all M3-16 art: dormant base, lift states, pads, cables, plotter, P3, P9 sites, TubeSocket;
  - ui guidance and place cards;
  - audio wake and not-yet sounds;
  - rover's two-beat wake;
  - world's home lighting.
  Tuning assets were re-serialised.
- **Speed-mode assignments in flight** (each session merged main 09d8cc0e+ and works on its box branch):

  | Session | Queue |
  |---|---|
  | gameplay | Playthrough with the real sites (P9/P10) → P1 story parts per tower level → P2 the chasm before the bay → P7 logic (BlueprintDelivered drives the plotter; pin on delivery) → pads replace the neon rings → P6 the dock's evening (event for rover, audio and ui) |
  | rover | FEATURE SLICE "the crew's lift", authorised in Gameplay/Lift/**: station restore, ride, lookout, ladder look, tube on 07's back |
  | world | power cables light from the tower out; the tower pilot blink (P5); dark-start quality captures |
  | ui | P8 (next line after ~120 s, never while stargazing or waking); blueprint found and delivered lines; first-10-min one-voice QA |
  | audio | lift creak, ratchet, hum and clunk; plotter scratch; tube chime; dock-evening swell |
  | art | quality pass in the real Main scene (dark start, wakes, the owner's angle), jammed lift readable |
  | game design | idle; ask it to review the merged 0.5 once it plays end to end |

  Decisions taken while in flight (2026-10-10, late):
  - **Garage yields.** All cuttable, no drag piece: Wiring L, Wiring M, Metal L, Metal M, Optics S = (7,7,2). Art was
    asked to do this; the Playthrough failed at the Cradle with the drag version.
  - **The golden path passes end to end** with gameplay 9f7f9216 and the old art (1052.8 s / budget 1100 s).
    Re-run it with art's fixed garage.
  - **Gameplay order:** P7 first, built as a generic "carried find" (found → rides on 07's back → delivered at a home
    station). Then P1 uses it:
    - L1 coil: in the depot's heart after the last cut, delivered at the tower.
    - L2 transformer: in the garage's heart.
    - L3 relay spare: from the first restored mast.
    - The shop returns PurchaseResult.NeedsPart.
    - The cassette player moves to the garage heart. The duck is the step-4 first memory, towed home.
  - **Art was asked for** Part_TowerCoil, Part_Transformer and Part_RelaySpare under Generated/Art/Pickups, carried on
    07's back.

  Latest box tips reported before this account ran out (check `director.py status` for anything newer):
  - **world 1f93d242.**
    - Done: cables light outward from the tower (~1.5 s), with home's lights starting when the cable arrives; the
      tower pilot blinks while Asleep; WorldHomeWiringContributor; builder World/Tower Pilot.
    - Its tests and captures (Logs/world-captures/wake-0..3) were still running when it committed: re-run and
      critique them. Update: EditMode 7/7 passed. The capture never ran (a shell-quoting error), so in the world
      worktree run, in order:
      1. `python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.RunBuilders --arg "filter=^(World/|Main Scene$)"`
      2. `python tools/unity_batch.py tests --platform playmode --filter HomeWake_IsCapturedFromTheSpawnView`
    - After merging: ResetSettings (new `_homeLighting` cable and pilot fields). Revert the rebuilt Main.unity and
      Generated files and the untracked metas in that worktree before merging.
  - **audio 7da01984.**
    - Done: Core LiftCue/LiftCued; the lift creak, ride and clunk, the plotter draw, the blueprint chime and the dock
      evening swell (generated); runtime for the lift, the plotter and the chime.
    - Tests confirmed: Audio EditMode 159/159; PlayMode AudioRuntimeTests|StationAudioTests 15/15, including zero GC.
    - Next: wire dock_evening_swell to Core.Events.DockEveningOpened(Vector3 DockPosition) once gameplay adds it.
    - After landing: the Audio builders and a StationAudioTuning re-serialise.
  - **rover 0579f46b.**
    - Done: the tube rides on 07's TubeSocket (TubeCarry, RoverRigTuning._tubeHalfLife).
    - After landing: run the Rover/Rover builder.
    - Gap: a tube carried across a save/load is not restored. Gameplay should re-publish BlueprintFound for
      carried-but-undelivered tubes on load.
    - Next: the ladder look, then the lift station and ride (Gameplay/Lift/**, authorised).
  - **gameplay c0ed4f94+.**
    - P7 is done: BlueprintFound means the tube rides on 07. Delivery happens within PlotterReach of the plotter while the bay is awake: it publishes BlueprintDelivered, pins the kit and the plotter draws 8 rows over 6 s.
    - Asked to add Core `BlueprintsCarriedRestored(int count)` on load, so rover's TubeCarry restores the tube after a load.
    - Next: re-run the Playthrough with art's garage fix, then pads, then P1 (plan approved).
  - **art eb423a5.**
    - The QA pass is done:
      - the shutter is weathered and torn;
      - the cables are sunk sheaths with glass beads;
      - the jam is rolled to 22° (the maximum before it hits 07 at the dock);
      - capture scenes wake_dark/home/bay/lift/pads; compare shots in Logs/captures/wake-compare/;
      - Art.Tests 258/258.
    - Queued, in order:
      1. the garage fix (all cuttable, 7,7,2; it blocks gameplay's Playthrough);
      2. Part_TowerCoil, Part_Transformer, Part_RelaySpare;
      3. lift readability props (a crew tag "OUT OF ORDER — K." on the rail, the snapped cable's end over the
         deck). Rover frames the lift from the dock side during the ladder look.

- **After landing 0.5's first batch:**
  - Run `^(Art|Rover|Gameplay|UI|Audio)/` builders and the scene.
  - Re-serialise the tuning assets.
  - Raise `SaveService.OldestCompatibleContent` and the version to 0.5.0 at release (M3-16 breaks old progress).
- **Remaining 0.5 work** (M3-16 "Amendments adopted"):
  - gameplay: P1 story parts per tower level, P2 the chasm before the bay, P4, P6 the dock's evening, P8, P9 the 2×
    economy with signature materials, P10;
  - the lift station and ride, once Art's lift lands;
  - rover: the ladder look and riding the lift;
  - also in 0.5: M3-07 map and M3-09 title.

## 2. What comes next (owner-approved order)
1. **0.4.2, DONE:** M3-15 §3 "rust v3" shipped as 5a293e84 (see §1). Its captures are in
   `MoonProject-wt/art/Logs/captures/rust-0.4.2/`.
2. **0.5 "Căn cứ thức dậy" (in progress):** the core is **M3-16** (`docs/features/M3-16-waking-lumen.md`), approved 2026-10-09:
   - a fresh game opens fully dark (only the dock and 07's eye are lit);
   - tower levels return power: L1 home, L2 Kenji's Rover Bay, L3 the crew's lift;
   - one verb per step, and every place has one job shown in one line;
   - Kenji's old bench becomes the blueprint board: kits unlock through blueprints found in salvage sites;
   - painted pads with lamps replace the neon rings;
   - the lift takes 07 up to the lander deck lookout.
   - Also in 0.5: M3-07, the crew's map inside the lander, reached by the lift; M3-09, the title screen (New Game) and
     collections. M3-08 (moon drift) moved to 0.6.
   - Size M3-16 into per-box tasks from its "Split" table. Ship 0.5 only after the Playthrough follows steps 0–8 in
     order.
3. Backlog items are in `docs/ROADMAP.md` "Backlog".

## 3. How the work is organised
- **Sessions.** One Director session ("MoonProject · Quản lý") plus one chat session per box (rover, art, world,
  gameplay, audio, ui) in the sidebar group "moonproject". If the old sessions are not reachable from the new
  account, create new ones (see §7).
- **Worktrees and branches.** Each box works only in `D:/Project/MoonProject-wt/<box>` on branch `box/<box>`, and
  merges main before starting. Boxes:
  - edit only their own domain folder;
  - never merge into main, never push, never commit `.meta` files.
  Authorised cross-domain edits (a Core event, a test seed) are named in the task and listed in the report.
- **Before reporting, a box runs `python tools/box_check.py <box>`** in its worktree, on a trial merge of main
  (aborted at the end; the tip must be committed). It runs:
  - compile_check (with lint);
  - the box's builders and the Main Scene build in one headless Unity;
  - the box's EditMode assembly, then its PlayMode assembly.

  Options: `--fast` skips PlayMode for quick iterations; `--filter` / `--assembly` / `--platform playmode` run
  other tests instead; `--no-merge` tests the working tree as it is.

  It then restores Generated/ and Main.unity and moves the batch-made metas to `Logs/box_check/`. Last, it appends
  a PASS/FAIL block to `MoonProject-wt/.board/<box>.md`, which `director.py status` prints.
- **Reports.** A box reports to the Director with `send_message`: tip SHA, its box_check block, the targeted tests
  and captures it ran (paths), tuning fields added or changed, new scripts needing metas, limitations.
- **Tests.** Boxes run only the tests and captures that cover their change (`--filter`, `--assembly`,
  `--category`). The full PlayMode suite and the Playthrough are the Director's, on the merged main. A box that
  changes the look also runs `python tools/look_report.py capture` before and after (M3-18).
- **Director loop for each report:**
  1. Review the diff: `git diff main box/<box>`.
  2. `python tools/director.py land --batch <box> [<box>...] -m "merge: ..." -m "Co-Authored-By: ..."`, the
     default. The merge, compile_check, the boxes' builders and the Main Scene build all run in
     `MoonProject-wt/director` (unity_batch exec, which also writes the new scripts' metas). Only if everything
     passes does it commit the merge and a "build: outputs and metas" commit there and fast-forward main. On any
     failure main is untouched. No main editor is needed; `--dry-run` stops before main.
     - The old path still works: `land <box>` merges in the main checkout with a compile_check gate, then
       hot-reloads the open main editor (refresh, the box's builders, Main.unity). Then run `outputs -m ...` to
       commit the regenerated assets and metas.
  3. Reset or write the tuning assets the merge touched (§5); land prints a NOTE for each touched schema.
  4. When a version's work is all landed, run `python tools/director.py verify --push`. It runs compile_check,
     EditMode, PlayMode and the Playthrough in `D:/Project/MoonProject-wt/director` and pushes only a commit that
     passed all of them.
     - PlayMode is sharded by test assembly across `director`, `director2` and the spare `landtest` worktree,
       balanced on the last run's assembly times (`--shards N`; 1 = one Unity).
     - `--parallel` moves the Playthrough to `director2` alongside them.
     - verify and land --batch lock the worktrees they use (`Logs/director.lock`).
     - Run it in the background; it costs almost no tokens.
  5. Build: `cd D:/Project/MoonProject-wt/director && python tools/build_player.py`. Then copy the zip to
     `D:/Project/MoonProject/Builds/` and give the owner the path.
- **Feature specs** go in `docs/features/M?-??-*.md`, written by the Director before assigning work. The ROADMAP
  version table and task rows are updated as work lands. The ROADMAP is in Vietnamese; the other docs are in English.

## 4. Tools and environment
- **Unity:** 6000.0.78f1 at `C:/Program Files/Unity/Hub/Editor/6000.0.78f1/Editor/Unity.exe`, URP.
  - The main project `D:/Project/MoonProject` needs to be open in the editor only for the old `land` path and
    `refresh`; `land --batch` does not need it. The owner opens it from the Hub.
  - Its MCP server (ai-game-developer) listens on `localhost:25906`. If native MCP tools fail, use
    `python tools/unity_mcp.py call <tool> '<json>'` or `@file.json`.
  - Keep the Profiler's Record off in the main editor.
- **`python tools/director.py status`:** every box against main, unpushed commits, batch slots, editor idle or busy,
  and the last block of each board in `MoonProject-wt/.board/`.
- **`python tools/compile_check.py`:** must print `RESULT: PASS` before every commit.
  - It runs `tools/lint.py` first: the CLAUDE.md debt rules plus Unity pitfalls. One example is a Unity object
    made in a MonoBehaviour field initializer.
  - `python tools/lint.py` lists every finding with its owning box. Rules that main still breaks are report-only
    until the owning boxes have fixed them.
- **`python tools/box_check.py <box>`:** a box's gate before it reports (§3).
- **`python tools/look_report.py capture|report|diff`:** the fixed look capture set (M3-18). It writes PNGs under
  `Logs/look-captures/<stamp>/` and measures each frame: luminance spread, ground flatness and the brightest 1%.
- **Tool tests:** `python -m unittest discover -s tools/tests -t tools`.
- **`python tools/unity_batch.py tests|exec|status`:** headless Unity on a worktree. At most three run machine-wide
  (`MOON_BATCH_SLOTS`, default 3 since 2026-10-10; drop to 2 if RAM runs short); PlayMode defaults to a 2400 s timeout.
  - Playthrough: `--category Playthrough --timeout 2700`.
  - Builders on a worktree: `exec --method MoonProject.Editor.Automation.BuildAutomation.RunBuilders --arg filter=^Art/`.
  - Idempotency: `exec --method MoonProject.Editor.Automation.BuildAutomation.CheckIdempotency`.
  - Captures: `CaptureAutomation.CaptureScene` / `CaptureTurntable`. GPU is on by default; never pass `--nographics`
    for captures.
- **Git:**
  - Always run `git -c safe.directory=* -c core.safecrlf=false ...`; the repo has a foreign owner SID.
  - Push: `GIT_TERMINAL_PROMPT=0 git -c safe.directory=* -c credential.interactive=never push origin <sha>:main`.
    It uses the owner's GitHub sign-in in Git Credential Manager, not the Claude account.
  - Commits: Conventional Commits, scoped by domain, ending with the `Co-Authored-By` trailer the harness gives.
- **Saves:** `%USERPROFILE%/AppData/LocalLow/MoonProject/Lunarion/Saves/<slot>.json` once the rename lands (before:
  `.../Lofi Lunar/...`; the rename moves old saves across once).
  - `SaveService.OldestCompatibleContent` is `"0.4.1"`. Raise it when a release breaks old progress (M3-16 will):
    older saves are moved to `<slot>.old-<stamp>.json`, never deleted.
  - Tests seed saves with `"contentVersion":"0.4.1"`.
  - Each test process uses its own slot (`BootstrapHarness.ProcessSlot`).
- **Title in tests:** the game boots into the title (the scene builder sets `_showTitle`). Every editor test run
  boots straight into play instead (`Editor/Automation/TestRunTitleSkip`), so fixtures need nothing. A test of the
  title clears `GameBootstrap.TestRunSkipsTitleKey` for its own boot (see `TitleFlowSessionTests`).

## 5. Recipes the Director needs
- **Write new tuning fields into an asset.** When the merge only added fields, use script-execute with a C# body that
  loads the asset, calls `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets()`.
- **Change a default that the asset still holds.** Set it with `SerializedObject.FindProperty(...)` and
  `ApplyModifiedPropertiesWithoutUndo`, then save. Never `CopySerialized` a fresh instance over an asset whose other
  values were tuned away from the C# defaults.
- **WorldSettings:** call `MoonProject.World.Editor.WorldAssetBuilders.ResetSettings` via script-execute (reflection
  lookup of the type).
- **Version bump.** Set `PlayerSettings.bundleVersion` through script-execute in the open editor, then commit
  `ProjectSettings.asset`. Editing the file alone is undone by the open editor. Raise the save floor in the same
  release if needed.
- **After a land:**
  - `director.py` prints a "NOTE" for every `*Tuning.cs` / `*Settings.cs` the merge touched; check each.
  - Look at the diff of every regenerated asset before `outputs`.

## 6. Owner preferences and rules (also in the memory files)
- **Language and authority.**
  - The owner writes in Vietnamese; reply in short Vietnamese.
  - The game is English first, Vietnamese second.
  - The owner gave the Director full authority on routine decisions. Ask only about real product choices; use the
    AskUserQuestion tool with a recommended option.
- **The bar.**
  - Zero technical debt (see `CLAUDE.md`).
  - Feel is the product.
  - Pillar 6, "Alone, and at peace": polished, lonely but calm.
  - Ruling 11: every upgrade visibly changes 07 or home.
  - Ruling 12: things are old, rusty, abandoned for decades.
  - Ruling 13: true scale.
  - Ruling 14: 07 has no hands; it uses its beam, and the base's machines do the rest.
  - 07 is not WALL-E. All music and SFX are generated in code.
- **Phases.** Finish one version completely (verified build plus the owner's review) before opening the next. Park
  out-of-phase ideas in the backlog.
- **Tokens.** The owner sets a daily cap as a percentage of the weekly limit (2026-10-09: 80%).
  - Check `get_usage` while sessions run, with a 10-minute cron guard.
  - At cap −2%, tell the sessions to wrap up.
  - At cap −1%, stop them and tell them to leave nothing half-committed.
  - Don't wake idle sessions without need.
  - Keep sessions at medium effort and prompts tightly scoped.
  - Delete the cron when nothing runs.
- **Pushes.** Push to GitHub at milestones, and only verified SHAs.

## 7. Starting a box session (template)
Spawn a chip with `spawn_task`; the owner clicks it to start the session. File the session into the "moonproject"
sidebar group and title it "MoonProject · <box>". Its prompt must stand alone:

> You are the <box> box of MoonProject (read CLAUDE.md and the docs it lists). Work only in
> D:/Project/MoonProject-wt/<box> on box/<box>; merge main first. Task: <spec section and exact deliverables>.
> Authorised outside your domain: <files or none>. Verify with compile_check and targeted unity_batch runs only (no
> full PlayMode or Playthrough). Report to the Director session "<director title>" via send_message: tip SHA,
> compile_check, tests and captures run (paths), tuning fields added or changed, scripts needing metas, limitations.
> Token budget: <today's cap>; keep the report short.

## 8. Lessons learned (don't repeat)
- **Scene cleanup in PlayMode tests.** A PlayMode test that loads the Main scene must destroy that scene's roots in
  `[TearDown]`. A leftover world made World's boot test count 392 prop colliders instead of 172.
- **Shared save slots.** Two Playthroughs racing on one slot gave false failures. Slots are per process now.
- **The open editor wins.** The main editor overwrites `ProjectSettings` and in-memory assets, so change them through
  MCP and not only on disk.
- **Assets keep old values.** New ScriptableObject fields load at their C# defaults until the asset is re-serialised.
  Changed defaults never reach an existing asset by themselves.
- **Metas.** Workers' batch-made metas are deleted. The main editor generates the real ones, which `outputs`
  commits.
- **Queued messages can be lost** (2026-10-11: ui's (a)–(c) and rover's rig fix never arrived).
  - Check that a critical message arrived with list_events.
  - Restate open items in the next message after each land, and ask for a one-line acknowledgement.
- **Reset tuning before you rebuild the scene.** Run ResetSettings or the re-serialise first, then `refresh
  --scene`, so the scene wires what the new settings declare (e.g. home lighting's pools).
- **land --batch runs no tests.** It gates on compile_check, the builders and the scene build only. Before landing a
  change early (a Core rename, a contract), ask for the box's `box_check` block with its tests: 2026-10-11 the lift
  rename added two `LiftCue` values without sounds and main failed Audio's EditMode until the hatch sounds landed.
- **Verify needs shards under load.** One PlayMode shard took over 40 min while three slices ran Unity and timed out
  (2026-10-11); use the default `--parallel` (2 shards) or `--shards 3`.
- **A new tuning type and its asset in one land lose the asset's values.** The box never commits the script's
  .meta, so the land worktree gives the script a new GUID, the asset loads as missing and `EnsureExists` recreates it
  at C# defaults (2026-10-11: BatteryTuning's interim 8 cells / 400 m came back as 4 / 120 and 43 tests crawled).
  After such a land, diff the box's asset against main's and write the values back.
- **Only a full box_check block lands.** `--fast` skips PlayMode; battery's EditMode-only block hid 41 Gameplay
  PlayMode failures (the test fixture didn't wire the new BatteryField).
- **Fixtures' copies of Main are never versioned** (`.gitignore`): a land once committed five of them as outputs, and
  the fixtures' cleanup then left every verification worktree dirty.
- **Read captures yourself.** Judge feel and visuals on the captures, not on the numbers alone. The owner judges the
  game by screenshots.

## 9. Memory files
The old account's auto-memory lives in `C:/Users/jvigh/.claude/projects/D--Project-MoonProject/memory/`:
- `director-mode`
- `team-setup`
- `resume-point`
- `unity-mcp-access`
- `feel-priority-solitude`
- `visible-progression-priority`
- `phased-releases`
- `token-budget`

Everything essential from them is in this file. If the new account uses the same Windows user and project folder, the
memory may simply carry over. Check `resume-point` against §1 here and trust this file and git if they differ.

## 10. Speed-ups (owner asked 2026-10-10: "too slow, optimise the process")
Diagnosis:
- **The weekly token quota is the real throughput limit.** Speed comes from spending fewer tokens per unit of work.
- **Fixed waits:**
  - verification ~45–60 min, serial;
  - a flaky test forces a full re-run;
  - sessions wait on each other's answers;
  - every land needs the Director's turn (it no longer needs the open main editor).

Do first on the new account:
1. **`director.py verify --parallel` (built, NOT yet exercised).** It runs the Playthrough in a second worktree
   (`MoonProject-wt/director2`, created on first use with a first-import cost) alongside EditMode and PlayMode.
   Expected ~25 min instead of ~45. Try it once and fix it if needed.
2. **Keep contexts small.**
   - Start a fresh Director session per version; HANDOFF.md makes that safe.
   - Give box sessions a fresh session per task instead of reviving a long one. A 400k-token context costs more
     per turn than the work itself.
3. **Cheaper models for mechanical work.** Sonnet for asset resets, test fixes, data tables and captures; Opus for
   design-heavy and cross-system work.
4. **Contracts and defaults up front.**
   - The Director writes every Core contract and a dependency map before dispatching.
   - Each prompt pre-decides the defaults: "if blocked by another domain, stub it, continue, and list it in the
     report"; "economy: tune within ±X yourself".
   - Sessions should never sit waiting for an answer.
5. **Feature-slice sessions.**
   - A vertical feature (e.g. the lift: art + gameplay + audio + ui) goes to ONE session allowed in those folders.
   - Per-domain boxes stay for domain-wide work.
   - This removes most hand-offs and waiting.
6. **Land without the main editor (built 2026-10-11).** `director.py land --batch` (§3). The main editor is now
   only for the owner's viewing; nobody has to keep Unity open.
7. **Faster, steadier tests.**
   - Fix the flaky tether test (it should wait on state, not time).
   - Shard PlayMode by assembly (built 2026-10-11): `verify` runs one Unity per shard worktree, so the suite takes
     the longest shard's time instead of the sum. First trial: two shards of 779 s and 713 s ran side by side
     (1492 s summed). Two Unitys at once slow each other, so balance on the shard times of real runs.
   - Split the Playthrough into chapters that start from seeded saves, so a late failure re-runs one chapter, not
     15 minutes.
8. **Reports as files, not messages (built 2026-10-11).** `box_check.py` appends a status block to
   `MoonProject-wt/.board/<box>.md`, and `director.py status` prints the last block of each board. The Director reads one board per turn instead of being woken
   by every message.
