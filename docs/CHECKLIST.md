# Lofi Lunar — Checklists

## Before merging to main
Run on the box branch with current `main` merged in, from the box's worktree (never the main project). Every step must
end in `RESULT: PASS`; paste the result lines into the merge report.

1. `python tools/compile_check.py` — editor + player configs, zero warnings.
2. `python tools/unity_batch.py tests --platform editmode` — every EditMode test.
3. `python tools/unity_batch.py tests --platform playmode` — every PlayMode test (needed whenever runtime code,
   prefabs or the scene changed).
4. `python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.CheckIdempotency` —
   every builder runs twice; `Generated/` and `Scenes/` must be byte-identical after the second pass and no builder
   may leave objects in the open scene. "pass 1 changed N file(s)" means the committed generated output was stale:
   the Director regenerates and commits it in main after the merge.
5. `python tools/build_player.py --no-smoke` — the Windows player still builds (catches build-only failures: URP
   validation, editor API in runtime code, stripping).
6. Full `python tools/build_player.py` (build + zip + 20 s smoke run) — before a build goes to the owner, and for any
   change to boot, world generation, save or the scene. The smoke run uses `-saveSlot smoke` and deletes it, so the
   owner's progress is never read or written.

## Playtesting a build
- Builds land in `Builds/LofiLunar-<version>.zip`; `build_info.txt` next to the exe names the exact commit.
- `LofiLunar.exe -saveSlot <name>` plays on a separate save (letters, digits, `-`, `_`); without it the game uses
  the owner's `main` slot.
