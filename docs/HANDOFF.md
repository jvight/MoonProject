# Director handoff (2026-10-10)

Read this, then `CLAUDE.md`, then `docs/VISION.md`, `docs/DESIGN.md`, `docs/ARCHITECTURE.md` and `docs/ROADMAP.md`.
It records how the project is run, where it stands, and what comes next, so a new Director session (on any
account) can continue without the old conversation.

## 00. NOW (2026-10-11 ~09:45): read this first
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
- **Saves:** `%USERPROFILE%/AppData/LocalLow/MoonProject/Lofi Lunar/Saves/<slot>.json`.
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
