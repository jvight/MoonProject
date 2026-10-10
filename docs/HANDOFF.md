# Director handoff (2026-10-10)

Read this, then `CLAUDE.md`, then `docs/VISION.md`, `docs/DESIGN.md`, `docs/ARCHITECTURE.md` and `docs/ROADMAP.md`.
It records how the project is run, where it stands, and what comes next, so a new Director session (on any
account) can continue without the old conversation.

## 1. Where it stands
- **Shipped:**
  - 0.4.0 "look right": pushed 50990e8.
  - 0.4.1: New game and stargazing. Pushed 98f6edd. Verified with EditMode 1173, PlayMode 232 (13 skipped,
    0 failed) and the Playthrough.
  - Playtest zips are in `Builds/` (git-ignored): `LofiLunar-0.4.1-20261009-98f6edd.zip` is the latest.
- **main:** after 98f6edd there are only docs commits: the M3-16 spec, the owner's approvals and this file.
- **Nothing in flight.** Every box branch is merged into main and every box worktree is clean. Both batch slots are
  free.
- **The owner's review of 0.4.x found:**
  - rust runs read like a printed pattern;
  - the lift does nothing;
  - everything is laid out and lit from minute one;
  - the old bench and "home" have no clear job.
  These are planned below.

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
