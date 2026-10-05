# MoonProject — "Lofi Lunar" agent handbook

Cozy low-poly lunar rover game (Unity 6000.0.78f1 LTS, URP). Read before doing anything:
- `docs/VISION.md` — creative direction, feel pillars, palette, feel checklist
- `docs/ARCHITECTURE.md` — assemblies, composition, events, content pipeline, verification ladder
- `docs/ROADMAP.md` — milestones, task board, who owns what
- `GDD.md` — original design pitch

This file overrides the older `.agent/` and `.agents/` rule sets (they are templates from other projects).

## How the team works
- **Director box** (the main session) owns vision, plan, task assignment, code review, merging to `main`,
  everything inside the running Unity editor, and `docs/`.
- **Worker boxes** each own one domain folder (`Assets/_Project/Scripts/<Domain>`, its tests, its builders,
  its generated output). A worker works in its own git worktree on branch `box/<name>`, commits there, and
  reports back. Workers never merge to `main`, never push, never edit another domain's folder.
  Exception: adding a new event struct to `Core/Events/<YourDomain>Events.cs` — mention it in your report.
- Need something from another domain? Say so in your report (or message the Director). Don't reach across.

## Git
- The repo folder has a foreign owner SID; always run git as `git -c safe.directory=* <command>`.
- Conventional Commits in English, imperative, scoped by domain: `feat(rover): add suspension bob`.
  Small, single-purpose commits. Never commit generated junk (Library/, Temp/, Logs/, *.csproj).
- Do not hand-write `.meta` files for scripts/folders; Unity generates them in the main project and the
  Director commits them. Never delete or regenerate an existing `.meta` (GUIDs are references).

## Zero technical debt — non-negotiable
- `python tools/compile_check.py` must print `RESULT: PASS` before every commit (zero warnings, editor + player).
- No `TODO`/`FIXME`/`HACK`, no commented-out code, no dead code, no "temporary" placeholders left behind.
  If something is deferred, it goes in your report as a proposed roadmap task — never as a code comment.
- No magic numbers in gameplay/feel code: tuning ScriptableObjects with `[Tooltip]` + `[Range]`/`[Min]`.
- No singletons, no static mutable state, no `Find*`/`GameObject.Find`/`SendMessage`, no `Resources.Load`,
  no legacy `Input`, no `OnGUI`, no `WheelCollider`, no coroutine-per-frame allocations.
- Steady-state zero GC per frame: no LINQ/closures/boxing/string building in Update/FixedUpdate.
- Fail fast on broken wiring: validate required references in `OnValidate`/`Initialize` and `Debug.LogError`
  with context; never silently "fall back" to a default that hides a missing reference.
- Pure logic lives in plain C# classes with EditMode tests; MonoBehaviours stay thin.

## Code style
- Namespace `MoonProject.<Domain>` (sub-namespaces allowed). One public type per file, file name = type name
  (small related event structs may share `<Domain>Events.cs`).
- Allman braces, 4 spaces, UTF-8, max ~120 cols. `using` order: System, UnityEngine, Unity.*, MoonProject.*.
- Private fields `_camelCase` (serialized ones too: `[SerializeField] private float _maxSpeed;`), properties /
  methods / types `PascalCase`, constants `PascalCase`. Always write access modifiers.
- XML `<summary>` on public types and non-obvious public members. Comments explain *why*, never *what*.
- Prefer composition, small components, explicit dependencies passed via `Initialize(GameContext)`.

## Feel is the product
Every player-facing change must satisfy the feel checklist in `docs/VISION.md`. Ease everything,
sound everything (in D major pentatonic), keep it warm and calm. When in doubt, slower and softer.

## Unity editor access
- Only the Director drives the running editor (Unity MCP `ai-game-developer`, port 25906) on the main project.
- Workers verify with compile_check, EditMode-testable logic, and (when available) `tools/unity_batch.py`,
  which runs a headless Unity on the worker's own worktree (serialised by a lock — one batch Unity at a time).

## Reporting (end of every worker task)
Report: what changed (files, commits), verification evidence (compile_check result, tests, captures/metrics),
known limitations, anything you need from other domains, and proposed next tasks. Never claim something works
that you did not verify; say exactly what was and wasn't checked.
