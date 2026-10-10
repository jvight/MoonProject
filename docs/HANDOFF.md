# Director handoff (2026-10-10)

Read this, then `CLAUDE.md`, then `docs/VISION.md`, `docs/DESIGN.md`, `docs/ARCHITECTURE.md` and `docs/ROADMAP.md`.
It records how the project is run, where it stands, and what comes next, so a new Director session (on any
account) can continue without the old conversation.

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
- **0.5 in flight on box branches (not merged).** Land gameplay + ui + audio + rover + art together, only after
  gameplay's part F (the Playthrough follows steps 0–8) passes:

  | Box | State |
  |---|---|
  | gameplay | A–F done, tip 1b2ba6cd: power stages, dormancy, steps, places, blueprints (BlueprintFound), IHomeLights wired, P4. The Playthrough runs in step order but FAILS at step 7 (the relay is unaffordable). P9 (2× economy, a signature material per site, the path via depot, garage and Kestrel) + P10 (4 depot cuts) were in progress to fix it. Then P1, P2, P8. New ProgressionTuning asset; metas for Progression/*. |
  | ui | Done, 583ec3cc: next line, Guidance toggle, place cards, HUD verbs, Look-up hint waits for cards. UiTuning `_guidance` needs a re-serialise after landing. |
  | audio | Done, 93fd84f4: wake stingers per stage, the "not yet" tone. StationAudioTuning has 2 new fields. StationNotYet is also in box/gameplay with an identical diff. |
  | rover | P5 two-beat wake done, tip c53e3681. Beat 1 is the lens looking up at the stars. Beat 2 is an over-the-shoulder shot with Earth in the upper third, then it eases to the chase. Approved. RoverCameraTuning has the `_wake*` fields; reset or re-serialise them after landing. Metas: WakeOpening.cs and its tests. |
  | art | ALL M3-16 art done, tip c9c95b3:
  - bay shutter, lander glows, HOME sign;
  - lift jammed and restored states with the winch hopper;
  - StationPad prefabs with pad lamps;
  - PowerCable_Home/Bay/Lift glow segments;
  - P3 (rear is not a face);
  - P9 site yields (depot 5,2,2 in 4 cuts, garage 7,7,2, Kestrel 2,2,9, drill 7,7,2);
  - P7: Plotter with PenCarriage, PenHead, PlotterSheet and PlotterLamp; Blueprint_<Ability> sheets on a cord;
    TubeSocket on 07's lid.
  Still to wire, by gameplay and rover:
  - the plotter drawing a sheet when a tube arrives;
  - the tube riding on 07's back;
  - the lift station and ride;
  - pads replacing the neon rings;
  - cables lighting with BasePowerChanged.
  New RoverModel node TubeSocket: check Rover's node lists when landing. |
  | world | Its part is landed. |

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
1. **0.4.2:** M3-15 §3 "rust v3", art only (`docs/features/M3-15-logic-pass.md`).
   - Runs start only at a cause (rivet, seam, bolt), few per panel, with random length, spacing and width.
   - Brown fading out; irregular patchwork.
   - Weather Bell, the tape rack, the depot panels and the Kestrel capsule too.
   - Acceptance: a test that no two adjacent runs share length and spacing, plus chase-camera captures.
2. **0.5 "Căn cứ thức dậy":** the core is **M3-16** (`docs/features/M3-16-waking-lumen.md`), approved 2026-10-09:
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
- **Reports.** A box reports to the Director with `send_message`: tip SHA, compile_check, the targeted tests and
  captures it ran (paths), tuning fields added or changed, new scripts needing metas, limitations.
- **Tests.** Boxes run only the tests and captures that cover their change (`--filter`, `--assembly`,
  `--category`). The full PlayMode suite and the Playthrough are the Director's, on the merged main.
- **Director loop for each report:**
  1. Review the diff: `git diff main box/<box>`.
  2. `python tools/director.py land <box> [<box>...] -m "merge: ..." -m "Co-Authored-By: ..."`. It merges with a
     compile_check gate (and aborts on failure). Then it hot-reloads the main editor: refresh, the box's builders,
     Main.unity.
  3. Reset or write the tuning assets the merge touched (§5).
  4. `python tools/director.py outputs -m "build: ..." -m "Co-Authored-By: ..."` commits the regenerated assets and
     metas.
  5. When a version's work is all landed, run `python tools/director.py verify --push`. It runs compile_check,
     EditMode, PlayMode and the Playthrough in `D:/Project/MoonProject-wt/director` and pushes only a commit that
     passed all four. Run it in the background (~1 h); it costs almost no tokens.
  6. Build: `cd D:/Project/MoonProject-wt/director && python tools/build_player.py`. Then copy the zip to
     `D:/Project/MoonProject/Builds/` and give the owner the path.
- **Feature specs** go in `docs/features/M?-??-*.md`, written by the Director before assigning work. The ROADMAP
  version table and task rows are updated as work lands. The ROADMAP is in Vietnamese; the other docs are in English.

## 4. Tools and environment
- **Unity:** 6000.0.78f1 at `C:/Program Files/Unity/Hub/Editor/6000.0.78f1/Editor/Unity.exe`, URP.
  - The main project `D:/Project/MoonProject` must be open in the editor for `land` and `refresh`; the owner opens it
    from the Hub.
  - Its MCP server (ai-game-developer) listens on `localhost:25906`. If native MCP tools fail, use
    `python tools/unity_mcp.py call <tool> '<json>'` or `@file.json`.
  - Keep the Profiler's Record off in the main editor.
- **`python tools/director.py status`:** every box against main, unpushed commits, batch slots, editor idle or busy.
- **`python tools/compile_check.py`:** must print `RESULT: PASS` before every commit.
- **`python tools/unity_batch.py tests|exec|status`:** headless Unity on a worktree. At most two run machine-wide
  (`MOON_BATCH_SLOTS`); PlayMode defaults to a 2400 s timeout.
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
  - every land needs the Director's turn and the open main editor.

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
6. **Land without the main editor.**
   - Build `director.py land --batch`: merge in the verification worktree, run the builders there with
     `unity_batch exec`, which also generates the metas, and commit the outputs from there.
   - The main editor is then only for the owner's viewing; nobody has to keep Unity open.
7. **Faster, steadier tests.**
   - Fix the flaky tether test (it should wait on state, not time).
   - Shard PlayMode by assembly across the two slots.
   - Split the Playthrough into chapters that start from seeded saves, so a late failure re-runs one chapter, not
     15 minutes.
8. **Reports as files, not messages.** Sessions append a status block to `MoonProject-wt/.board/<box>.md` at each
   milestone, and `director.py status` prints the board. The Director reads one board per turn instead of being woken
   by every message.
