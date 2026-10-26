# Lunarion → Unreal Engine 5.8: port plan

> **Status:** draft for the Director, 2026-10-26. Author: tools session. Owner decision (2026-10-26): port to Unreal 5.8;
> Unity is the **reference build** from now on (bug fixes and port support only, no new features).
>
> **Design companion:** `docs/design/ue-port-design-notes.md` (GD: eleven design areas mapped to UE tools, what to keep exactly,
> the risks, and a suggested order). This plan carries its intent; section 4.1 lists its must-keeps with owners.
>
> **Inputs read:** Rover's spike (`D:/Project/MoonProject-UE/PortSpike/Report/REPORT.md`, `SPIKE_LOG.md`),
> `docs/ARCHITECTURE.md`, `CLAUDE.md`, `tools/`. The spike measured the toolchain, the pure-logic port, input, camera, a
> physics sphere, FBX and asset scripting, and headless tests. It did **not** measure UI, audio, save, shader fidelity,
> packaging, or an IL2CPP-bot equivalent, so those parts of this plan are decisions to verify, marked **(verify)**.

## 0. Principles
1. **Text first.** Every number, recipe and rule must be reviewable as text. Binary `.uasset` files are build outputs of
   Python/C++, never hand-edited (the same rule as our Unity scenes).
2. **Port logic 1:1, redesign engine glue.** Pure C# classes port almost mechanically (spike: 1.0–1.2 C++ lines per C#
   line) together with their tests. MonoBehaviours, rigs, VFX, lighting, UI and audio are redesigned for UE.
3. **Same gates, same discipline.** zero warnings, zero technical debt, fail-fast wiring, tuning in data, one domain per
   session. The gates are rebuilt on UE tools (section 2).
4. **Feel is checked against the Unity build side by side** (the owner-facing risk). Unity stays runnable as the reference
   until M3 is accepted.

## 1. Target layout and repo strategy

### 1.1 Repo
- **New git repo `D:/Project/MoonProject-UE/Lunarion`** (own history, own branches `box/<name>`, same worktree-per-session
  flow). The Unity repo is frozen after the port starts: tag it `unity-reference` and keep it as read-only reference.
- **Git LFS** for binaries: `*.uasset *.umap *.fbx *.wav *.ogg *.png *.exr *.tga *.blend *.psd`. `Source_Assets/`
  (Blender sources, the FBX we export) is LFS too.
- **Text vs binary:**

  | Text (reviewed in diffs) | Binary (generated, LFS) |
  |---|---|
  | C++ headers and sources, Build.cs, Target.cs | `.uasset` (meshes, materials, data assets, widgets) |
  | `Config/*.ini`, `DefaultInput.ini` | `.umap` |
  | Tuning defaults in C++ `USTRUCT`s | imported FBX/WAV/OGG/PNG |
  | Content tables as CSV/JSON (`DataTables`, StringTables, localization `en`/`vi`) | |
  | Python asset scripts (`Scripts/…`), recipes as JSON | |
  | `docs/` | |

- **How agents keep diffs reviewable**
  - Tuning values live in C++ `USTRUCT` defaults (as in the spike): the `.uasset` stores only overrides, so it is nearly
    empty and the real diff is a header. Content (relic definitions, unlocks, lines) is CSV/JSON that a commandlet
    turns into DataTables.
  - Every `.uasset` is produced by `Scripts/<domain>/build_*.py`, idempotent, run through `UnrealEditor-Cmd` (spike:
    24 s per script run). A manifest `Content/_Manifest/<domain>.json` lists each generated asset path + source hash, so
    a PR shows *which* assets changed and why. A gate re-runs the builders on a clean checkout and fails if the manifest
    differs (reproducibility).
  - **Nobody hand-edits a map.** The Main map is built by a scene builder, like `Main.unity` is today. Enable One File Per
    Actor for any map that is saved, so a rare merge is per actor.
  - **Reviewers read headers, scripts and manifests, not binaries.** A PR that changes only `.uasset` with no script or
    header change is rejected.

### 1.2 Project layout (one game project, one module per domain)
One `Lunarion.uproject` with **modules, not plugins**, for domains. Plugins only where the code must be excludable from
shipping or reused (Autoplay). Modules mirror our asmdefs and enforce the same graph through `Build.cs`
dependencies.

```
Lunarion/
  Lunarion.uproject
  Source/
    LunarionCore/        <- Core: context, event bus, input actions, layers (collision channels), tuning base, save, localization
    LunarionWorld/       <- World:  terrain + queries, sky/sun/Earth, layout, scatter          (deps: Core)
    LunarionRover/       <- Rover:  drive model, camera, scan aim, rig, rover VFX hooks        (deps: Core)
    LunarionGameplay/    <- Gameplay: scan, salvage, build, tow, opening flow, battery, save   (deps: Core, World, Rover)
    LunarionUI/          <- UI:     HUD, menus, face/CRT (reads Gameplay through Core)         (deps: Core)
    LunarionAudio/       <- Audio:  director, radio/score, event-driven SFX                    (deps: Core)
    LunarionApp/         <- App:    game mode, boot, composition root                          (deps: all)
    LunarionTestSupport/ <- shared automation harness
    LunarionEditor/      <- editor-only: commandlet hooks, builders' C++ helpers
  Plugins/
    LunarionAutoplay/    <- the bot (Developer/Test, never in Shipping)                        (deps: Core, Gameplay, Rover)
  Content/               generated only (Art/, Audio/, Data/, UI/, Maps/, _Manifest/)
  Scripts/<domain>/      UE-Python builders + tools
  Config/ Tests (per module, Source/<Module>/Tests/)
```
- Dependency rule unchanged: domains talk **through Core** (typed events, interfaces); no sideways dependency.
  Shared event structs live in `LunarionCore/Events/<Domain>Events.h`.
- Spike lesson: a flat module layout needs `PublicIncludePaths.Add(ModuleDirectory)`; list `PhysicsCore`, `EnhancedInput`
  etc. explicitly, because a missing module is a **link** error, not a compile error.
- **Composition:** one `ULunarionContext` (owned by the GameInstance) holds the typed event bus and registered services. An
  **explicit ordered list** of systems is initialised by `ALunarionBootstrap` (no `GetAllActorsOfClass`, no global
  singletons, no `static` mutable state), exactly as `GameBootstrap` does now. Systems are `UActorComponent`/plain C++
  classes; actors stay thin.
- **Autoplay as a plugin** (`Type: Developer`, loaded only in Development/Test configs; a `WITH_LUNARION_AUTOPLAY` define
  mirrors `LUNARION_AUTOPLAY`). It never ships; the packaged Shipping build has no trace of it.

## 2. Toolchain: gate equivalents

| Unity gate (today) | UE equivalent | Notes |
|---|---|---|
| `tools/compile_check.py` (zero warnings, editor + player, lint) | `tools/ue/compile_check.py`: UBT build of `LunarionEditor` and `Lunarion` (Win64 Development) with **warnings as errors**, plus lint (no `new`/`delete` of UObjects, every `UPROPERTY` editable value has a tooltip and a `ClampMin`, no TODO, no commented-out code, no `GetAllActorsOfClass` or global statics) and `dead_code.py` | spike: incremental C++ edit 10–40 s, header touching the pawn 64 s. Verify the exact UBT flag in 5.8 (TargetRules). |
| `tools/box_check.py` (trial merge of main, builders, tests) | `tools/ue/box_check.py`: trial merge, UBT, builders for the box's domains, **per-module automation tests headless** (`UnrealEditor-Cmd Lunarion.uproject -ExecCmds="Automation RunTests Lunarion.<Module>; Quit" -nullrhi -unattended -NoSound -NoSplash`) | spike: 21 s for 7 tests, mostly editor boot, so batch all modules into one run |
| EditMode tests | `IMPLEMENT_SIMPLE_AUTOMATION_TEST` for pure logic (`EAutomationTestFlags` is an enum class in 5.8) | pure classes port with their tests almost mechanically |
| PlayMode scene tests, `Playthrough`, feel sessions | Functional Tests + latent automation commands with a rendering editor (not nullrhi); the Director's gold-path Playthrough | rewrite, about 2 weeks overlapping; the spike did not cover it |
| builders (`Editor/Builders`, Unity YAML out) | UE-Python commandlets `UnrealEditor-Cmd -run=pythonscript -script=Scripts/<domain>/build_*.py`, one idempotent script per builder, MaterialEditingLibrary for materials, `unreal.AssetTools` for imports | **biggest unknown** (REPORT). Spike gotchas: `save_directory` for FBX side-effect materials; re-creating a just-deleted path returns None in the same session; legacy FBX importer needs `Interchange.FeatureFlags.Import.FBX false`; FBX bakes node transforms (derive pivots from bounds or re-author the rig as a Skeletal Mesh or an Interchange scene) |
| `tools/build_player.py` (`--autoplay`, `--showcase`) | `tools/ue/build_player.py` wrapping `RunUAT BuildCookRun -cook -stage -pak -package -archive -platform=Win64`; configs Shipping (owner), Development + the Autoplay plugin (bot) | cooked build time to be measured **(verify)** |
| IL2CPP autoplay bot (`tools/autoplay.py`) | the same design: the bot **inside** the cooked game as the `LunarionAutoplay` plugin (virtual input through Enhanced Input injection, the naive gate, the hitch table, report.md), launched by `tools/ue/autoplay.py` against the packaged exe. Gauntlet is an optional CI wrapper, not a dependency | about 2–3 weeks (REPORT); the Python launcher, report format and beat declarations carry over unchanged |
| `unity_batch.py` slots (3 Unity at once) | `tools/ue/slots.py`: machine-wide slots for UBT and `UnrealEditor` processes, default **2** (see 2.1) | reuse `tools/moonbatch/slots.py` |
| `look_report.py`, `feel_capture.py` | UE automation screenshot + Unreal Insights `-trace=cpu,gpu,frame` for hitch attribution | the bot's `FrameTag`/`FrameProbe` ideas port to `stat unit` and Insights markers |

### 2.1 Parallel sessions and machine cost
Measured on this machine (spike): 16 threads, **3.5 GB free RAM** with Unity Hub and one editor open, so UBT limited itself
to 2 parallel actions; `Intermediate/` 2.7 GB for the editor target; module builds 1m41–2m31 cold.
- **Pause Unity sessions** as the port ramps up: no new Unity feature work (owner), so Unity editors and Hub should close.
  The reference Unity player is run from a packaged build, not the editor.
- **Shared DDC:** one machine-wide Derived Data Cache (`UE-SharedDataCachePath=D:/UE_DDC`, `-ddc=` in every tool), so each
  worktree does not rebuild shaders and textures.
- **Worktrees:** one per session as today, but each carries its own `Intermediate/` and `Binaries/` (3 to 6 GB). Budget disk
  accordingly and delete finished worktrees with the existing cleanup. `Saved/` and `Intermediate/` are git-ignored.
- **agy's own load:** an agy art/look job that opens `UnrealEditor-Cmd` counts as one heavy UE process. **One agy UE job at a
  time** within the 2-slot limit, so a Claude session always keeps the other slot for builds and tests; agy jobs queue on `slots.py`.
- **Concurrency limit:** at most **2** heavy UE processes (UBT, editor, cooked game) at once; more only after RAM
  is upgraded. `slots.py` queues the rest. Prefer **header-light** changes and keep logic in `.cpp`.
- **First-boot gap on a fresh machine:** the .NET Framework 4.8 SDK (VS component `Microsoft.Net.Component.4.8.SDK`,
  needs admin) must be installed before the first UBT run; document it in `docs/port/SETUP.md`.

## 3. Port order, dependencies and owners

Sessions: **rover**, **story & flow** (=Gameplay), **world**, **ui** / **ui-fit**, **audio**, **art**, **tools** (this
session), **GD** (game design), **Director** (merges, repo, plan).

Size (C#, excluding tests) and what it becomes:

| Domain | C# lines | Port nature | Owner |
|---|---|---|---|
| Core | 6.9k | pure logic + services: port first, 1:1 | tools + Director |
| Rover | 30.6k | drive/camera/scan aim ported (spike); rig, VFX, face redesigned | rover |
| Gameplay | 36.3k | ~60% pure logic (mechanical), rest actors/components | story & flow |
| World | 19.1k | queries/layout port (Claude); terrain look, sky, lighting redesigned (agy) | world (code); **agy** (look) |
| UI | 31.6k | **redesign** (UI Toolkit → Slate/CommonUI, see 5): widget code and logic by Claude, visual design by agy | ui, ui-fit (code); **agy** (visual design) |
| Audio | 8.4k | director logic ports; playback redesigned (MetaSounds) | audio |
| Art | 34.3k | **not ported**: the Unity mesh kit and builders are replaced by Blender FBX + UE Python | **agy** (models, materials); art session reviews |
| Autoplay + tools | 5.4k + Python | bot rewritten as a plugin; launcher and report ported | tools |
| Showcase, Editor, App | 10.2k | replaced by Sequencer / Editor Utility / bootstrap | tools, Director |

**Order (each step lands with its tests and gates green):**

0. **Week 0 – foundation (tools, Director).** Repo bootstrap, Build.cs graph, `compile_check`, `box_check`, slots, shared DDC,
   LFS, `SETUP.md`, a CI-less "gate chain" script. Exit: an empty module graph builds with warnings as errors and the
   headless test run is green.
1. **Core** (tools + Director): `ULunarionContext`, typed event bus (allocation-free for struct events), input actions
   (Drive, Look, Excavate, Tether, Winch, Jump, Recenter) in Enhanced Input, collision channels (Ground, Rover, Relic, Pickup,
   Prop, Trigger), tuning base, save (JSON, versioned), localization (`en`/`vi` JSON → StringTables, parity test). **Blocks
   everything.**
2. **Rover** (rover): spike code promoted into `LunarionRover`: drive model, orbit camera, scan aim, head, then the
   suspension/wheels rig, charge/battery visuals as placeholders. Decide the physics approach (section 5 risk 2).
3. **World** (world): terrain queries (`ITerrainQuery`), basin layout and landmarks as data, sun/Earth/`ISunlight`, placeholder
   terrain mesh and props, spawn of ruins as simple meshes.
4. **Gameplay** (story & flow), in this order: scan field and scan aim → materials/cargo hold → battery and pad/dock/ring →
   build mode and structures → tow (RMB cable) → cutting/salvage → opening flow (`OpeningFlow`, steps, keys) → relay/answer →
   save. Pure logic first with the tests, components after.
5. **UI minimal** (ui): HUD line, chips, build strip/cost chip, battery, pause. **Audio minimal** (audio): event bus
   subscribers with placeholder sounds. These run in parallel with step 4.
6. **Bot** (tools): `LunarionAutoplay` plugin as soon as the Rover and a scan exist (M1), growing beat by beat with the opening; the naive/strict guidance gates and real pointer input (16:9, 21:9) are built **before** the content they guard (GD, section 4.1).
7. **Art swap** (**agy**, reviewed by the art session): R1 FBX models (07, structures, ruins) imported and wired through the builders; the visual contracts in
   `tools/art/qa/contracts` are re-validated against the UE sockets.
8. **Full UI, audio, feel** (UI code, audio code, rover feel code: Claude sessions; UI visual design, VFX and look: **agy**; GD reviews): "Moonlight Glass" HUD, 07's face/CRT databank, bespoke score,
   chirps, camera and feel passes with the owner. Unity is the reference for every checklist.
9. **Look upgrade** (**agy**: Lumen/VSM exposure, Landscape/PCG look, Substrate materials; world and Director review): Lumen/Virtual Shadow Maps/Nanite/PCG once parity is accepted.

### 3.1 Who does what: agy or a Claude session (owner decision 2026-10-26)
**agy** (Gemini Argon, headless through `agy_run4.sh` with a `Logs/TASK.md` brief per worktree) is **prioritised** for art, feel,
VFX and look work. **Claude sessions** keep code, tools, tests, gameplay systems, integration and review.

| Task | Owner |
|---|---|
| Models, materials (Substrate), texture and decal work, Blender FBX export and the UE import script per asset | **agy** |
| Landscape and PCG look (basin, craters, scatter, rocks), terrain materials | **agy** |
| Niagara effects (scan/cut/weld beams, dust, thrusters, ring, glints, dock) driven by the existing `FeelBeat` events | **agy** (the event wiring: Claude, rover/gameplay) |
| Lighting and exposure (Lumen, Virtual Shadow Maps, sun/Earth, eclipse and night looks), post-process | **agy** |
| Sequencer shots (POST face shot, wake, hand-overs, cutscene) | **agy** (shot list from GD; playback hooks: Claude) |
| UI visual design (Moonlight Glass skin, icons, 07's face/CRT look, fonts, spacing, animation curves) | **agy** (widget classes, input focus, data binding: ui sessions) |
| MetaSounds patches and sound design | audio session (Claude); agy may draft candidate sounds only when asked |
| C++ modules, tuning structs, tests, builders' framework, gates, the Autoplay plugin, packaging | **Claude** (rover, story & flow, world, ui, audio, tools) |
| Integration, review, merging, parity checklists, feel review with the owner | Director, GD, Claude sessions |

**Designing an agy task so it finishes (agy often times out before its gate).** Every agy task is self-contained and small:
1. **One deliverable, one folder.** The brief names the output paths exactly (for example `Content/Art/Structures/ChargingPad/`),
   the files agy may touch, and the files it must not.
2. **Clear inputs.** Links to the contract (`tools/art/qa/contracts/*.json`, the VISION palette), the reference screenshots from
   the Unity build, and any C++ header it must match (socket names, event names).
3. **Scripted, idempotent output.** agy writes a UE-Python builder (`Scripts/<domain>/build_<thing>.py`) plus a manifest entry;
   it does not hand-click an editor. Re-running reproduces the asset.
4. **A capture as proof.** The brief requires one named screenshot or short capture
   (`Logs/proof/<thing>_<angle>.png`) from a fixed camera, taken by a provided script (`tools/ue/capture.py`), so the Director
   can judge without opening the editor.
5. **A simple gate agy can run in under 2 minutes:** the builder runs headless, the manifest hash matches, the asset loads, the
   capture file exists. Heavy gates (full builds, cooked bots, full test chains) are **not** in agy's brief: a Claude session or
   the Director runs them after agy lands the files.
6. **Finish early and say so.** The brief's last step writes `Logs/REPORT.md` and `Logs/DONE` before any optional polish, so a
   timeout loses only polish.
7. **Review by a Claude session.** The art, world or ui session checks the contract, runs the heavy gate and integrates; agy output is never
   merged unreviewed.
8. Quotas: at most 2 agy jobs at once, and Argon High only for visual judgement; mechanical captures use a smaller model.

### Milestones
| M | Name | Exit criteria | Estimate |
|---|---|---|---|
| **M1** | "07 drives and scans on the basin, placeholder art" | Rover drives and scans on the basin terrain with the ported camera; Core context/events/input/tuning in place; gates (`compile_check`, `box_check`, headless tests) green; first cooked build runs; the bot drives and scans through the packaged build and writes a report | ~2 weeks |
| **M2** | "Opening playable to relay.1 on placeholders" | B0–R10 playable end to end with real input: scan, build, tow, cut, dock, night, mast, relay; save/load; the naive first-time gate passes in the packaged build; every Unity opening test either ported or retired | ~4 weeks (cumulative) |
| **M3** | "Parity with the Unity opening, UI/audio/feel included" | HUD/menus/face in the Subnautica-grammar style, the spoken-less audio, camera and feel checklists accepted by the owner side by side with Unity; builds packaged by the pipeline; Unity freeze lifted | +6–8 weeks (10–14 total; spike: best ~8, worst ~20) |
| **M4** | "Look upgrade (Lumen, VSM, Nanite, PCG)" | the owner's look target on the same gameplay, performance budget met on the target PC | after M3 |

Spike basis: pure logic is ~45% of the work (mechanical, 10–14 working days with 8 sessions); engine-bound code ~55%
(3–5x slower per line, 5–8 weeks with 8 sessions); tests 95k lines (logic ports with its code, scene tests ~2 weeks
overlapping); the bot and builders are the two unknowns.

## 4. What we deliberately do NOT port 1:1 (rebuild better)

| Unity | UE | Why | Owner |
|---|---|---|---|
| Runtime-generated terrain mesh, scatter | **Landscape** from the heightmap data + **PCG** for rocks/craters; placeholder procedural mesh for M1–M2 only | native streaming, LOD, Nanite-friendly; the Unity terrain code was a runtime workaround | **agy** (look), world (queries) |
| Art domain mesh kit (34k lines C# builders) | **Blender FBX pipeline** (existing `tools/art/blender`) → UE Python import | the kit is the Unity mesh builder; the art contracts and the R1 models carry over | **agy** (models, imports); art session reviews |
| Particle systems, beam/weld VFX | **Niagara** | better tools; the feel beats (`FeelBeat` events) stay the contract | **agy** (Niagara); rover/gameplay wire the events |
| Audio: `AudioSource` players, our synth WAV/OGG | WAV/OGG kept as sources (`tools/audio` unchanged) + **MetaSounds** for the score, ambience, chirps | adaptive music and the no-lofi, bespoke-score decision are natural in MetaSounds | audio session |
| Cinemachine cameras, opening/face shots | player camera stays our **own orbit** (spike port; the camera is the player's); **Sequencer** only for authored shots (POST face shot, hand-overs) | owner rule: no auto camera on scan/tow/build | **agy** (Sequencer shots); rover keeps the orbit |
| URP bake + lighting | directional sun + Earth light, **Lumen/VSM** at M4; at M1–M3 a simple stable setup matched to the Unity look | look is milestone M4, not parity | **agy** (Lumen/VSM, exposure); world reviews |
| UI Toolkit (UXML/USS) | **Slate in C++** for the HUD (text, reviewable, testable), **CommonUI/UMG** only for menus; face/CRT as a Slate widget drawn to a render target on 07's screen mesh | see risk 4 | **agy** (visual design); ui sessions (widgets) |
| URP shaders and the shared palette material (`M_LowPoly`, glow, sky, beam) | **Substrate** materials built by UE-Python from the palette and the R1 contracts; hand-written HLSL only where a material cannot do it | the shaders were URP-specific; Substrate gives layered look at M3–M4 | **agy** (materials); world/rover review |
| `ScriptableObject` tuning | `USTRUCT` defaults + a `UDataAsset` per domain; content in DataTables from CSV/JSON | text-reviewable | Claude (core, domains) |
| Unity Input System asset | Enhanced Input, actions and mappings created in code/config | spike: needs `DefaultInput.ini` component classes and actions created before `BeginPlay` | Claude (core) |
| `JsonUtility` save | JSON via `FJsonObjectConverter`, versioned, in `Saved/` | keeps saves text and diffable | Claude (core) |
| Showcase build, `Editor/*` scene builder | Sequencer / Editor Utility Widgets; builder scripts | different engine tools | Claude (tools), **agy** (shots) |

### 4.1 Design must-keeps (from GD's `ue-port-design-notes.md`) and who guards them
The design docs stay valid; the port must not lose their rules. Each row names the owner of the guard.

| Area | Must keep exactly | Owner of the work | Guard |
|---|---|---|---|
| Regions and map | adjacency order; gates readable from the opened neighbour; one hand-placed purpose per site; the crater a closed cradle with three gated ways out; PCG only for scatter, never loot | **agy** (PCG look), world (layout data) | a layout-data test (sites, gates, adjacency) + GD review of every lure and site |
| Terrain look | craters few, varied, irregular; no repeated round dots; 07's 1.5 m scale against every prop; filter stock lunar assets | **agy** | GD/art review of captures against the no-hole-patterns rule |
| Light and sky | vacuum: no haze, black sky, hard shadows; Sky Atmosphere and fog **off from day one**; the sky follows progress, not a clock; earthshine never charges | **agy** (Lumen, exposure), world (the sky/sun logic) | a fixed-camera capture set per sky phase; an automation check that atmosphere and fog actors are absent |
| Cutscenes | ends into the boot face; one swell; skippable after 2 s; no camera tour around 07; Variant D only after the playable opening passes | **agy** (Sequencer shots), rover (hand-overs) | scheduled after M2 (GD order step 3) |
| 07's voice and the score | the 5-tone annunciator meanings; physical sources (Sonniss); bespoke score only at milestones, no lofi radio | audio session (MetaSounds); agy drafts only when asked | A/B against the Unity renders |
| Living moon | calm, never a threat; ballistic dust at 1.62 g, no smoke; every effect has a purpose | **agy** (Niagara/Mass look), gameplay (events) | frame-time budget per effect; GD review |
| Camera | the player's orbit is sacred: no auto camera on scan, tow or build; the "07 looks" nudge is 07's head; remove UE template auto-follow | rover | a test that the camera yaw/pitch are unchanged by scan, tow and build state changes |
| Guidance and text | one guidance element; the 5-s rule; persistent chips until pressed; the glossary; native vi/en String Tables; no default tooltips or quest lists | ui + ui-fit (code), **agy** (visual) | the naive/strict gates in the cooked build |
| Tow and caravan | breadcrumb follower for cars 2..N, snag-lift, soft let-go; assisted model, no free chain | story & flow (gameplay), rover | the bot's tow beat with real RMB |
| Range ring and network | 220 m per cell, 60% start, day-end on the pad, the ring from every charge point | story & flow | ported as **data tables**; the bot's per-beat cells table (`BeatTrip`) is the check |
| Bot gates | the 5-s next-action check; real pointer input at 16:9 and 21:9; strict one-guidance counter | tools | built **first**, not last (below) |

**Order, reconciled with GD's.** GD suggests: (1) the feel core (drive, camera, dock, scan, cut) so the core feel gate is passed again
in UE before content; (2) the opening basin and guidance with the bot gates; (3) Sequencer Variant D after the playable opening
passes; (4) World Partition regions (the Mare). This plan's steps 1 to 6 are (1) and (2); step 8 is the full feel; **the Sequencer
cutscene and World Partition follow M3** (they are part of M3's tail and M4). One change from the first draft: because GD asks that
the port never ship without the gates, **the bot's naive and strict gates (G1 5-second check, real pointer input at 16:9 and 21:9) move
into M1/M2**, ahead of content, rather than coming after it.

## 5. Risks and mitigations

1. **Feel parity (owner-facing).** Our drive and camera feel was tuned for 3 months. *Mitigation:* port the pure logic 1:1 with its
   tests; keep tuning identical; a side-by-side capture pair per beat (Unity reference vs UE) reviewed by GD and the owner;
   the bot's frame/hitch table in both builds.
2. **Physics substeps.** Unity used PhysX at a fixed 0.02 s; Chaos substeps at 20 ms plus variable frame time is a risk for
   drive feel (spike, unverified). *Mitigation:* enable **Chaos substepping at 50 Hz** (the project's physics settings: a fixed maximum substep of 0.02 s;
   **(verify)** the exact setting names in 5.8) and run the drive model in the physics tick (async physics) so it sees fixed steps; keep the rolling
   sphere design (spike: simpler than Chaos Vehicle); add a drive-feel regression test (speed/turn curves from the ported
   tests). *Decision by rover in M1.*
3. **Builders and assets (biggest unknown).** Unity builders emit Unity YAML; UE assets need UE Python. *Mitigation:* port the
   builders framework once (tools), then per domain; manifests and reproducibility gate; keep the Blender path for models;
   timebox a "build a map from script" proof in week 1.
4. **UI tech choice.** UMG is binary and hard to review; Slate in C++ is text but heavier to style; CommonUI helps input
   focus. *Mitigation:* a **2-day UI spike** (ui + ui-fit) in M1: the HUD line, a chip, the build strip and a CRT-face widget
   rendered to a texture on 07's screen mesh; pick by reviewability and the face requirement. Default if the spike
   is inconclusive: Slate HUD, CommonUI menus.
5. **Memory and machine load.** UE editor and UBT need 6 to 16 GB each; this machine had 3.5 GB free with Unity open.
   *Mitigation:* section 2.1 (close Unity, shared DDC, 2 slots, queue); recommend a RAM upgrade if the schedule slips; keep
   cooked-game runs for bot tests, not the editor.
6. **Automation gaps.** No PlayMode analogue; the bot has no direct UE equivalent. *Mitigation:* the bot as an in-game plugin (same design
   that already works), functional tests for scene logic, headless pure-logic tests per module.
7. **Hitches and shader compile.** "Preparing Shaders" delayed the first frames in the spike. *Mitigation:* pre-warm via PSO
   cache in the packaged build; the bot's first beat waits for shader readiness; report it as a finding rather than a failure.
8. **Dual maintenance.** Bugs fixed in Unity during the port. *Mitigation:* Unity changes only through the Director with a
   "port note" (a line in `docs/port/PORT_NOTES.md`); logic ported only from tagged commits.
9. **Scope creep.** *Mitigation:* the port milestones exit on parity checklists; new features wait for M3; the roadmap marks which
   Unity tasks are cancelled, kept or moved.
10. **Tool-chain break on engine updates.** *Mitigation:* pin UE 5.8.x in `docs/port/SETUP.md`; no engine source changes.

## 6. First two weeks (actionable)
| When | Who | Task |
|---|---|---|
| Day 1–2 | Director | create `D:/Project/MoonProject-UE/Lunarion` repo, LFS rules, `.gitignore`, branch/worktree flow, `CLAUDE.md` for UE |
| Day 1–3 | tools | `compile_check`, `box_check`, slots, shared DDC, headless test runner, `SETUP.md`, the `tools/ue/capture.py` fixed-camera capture and the agy brief template (`Logs/TASK.md`); one smoke module green |
| Day 1–5 | rover | promote the spike into `LunarionRover` + `LunarionCore` stubs; decide physics substeps; port the drive tests |
| Day 2–6 | tools + Director | Core: context, event bus, input actions, tuning base, save, localization + tests |
| Day 3–8 | world (code); **agy** (placeholder terrain look, sun and exposure) | terrain queries, sun/Earth, basin layout data, placeholder meshes |
| Day 3–10 | story & flow | scan field, materials, hold, battery logic with tests (pure logic first) |
| Day 4–8 | ui + ui-fit (code); **agy** (visual design) | UI spike (risk 4) and the HUD line/chip/cost-chip as the first widgets; agy drafts the look of the chip, line and CRT face in the spike |
| Day 5–10 | audio | event-driven placeholder audio and the MetaSounds proof (one chirp, one ambience) |
| Day 5–10 | **agy** (art session reviews) | FBX import of 07 and one structure through a UE Python builder with a manifest and a proof capture |
| Day 8–14 | tools | `LunarionAutoplay` plugin skeleton + `tools/ue/autoplay.py` + a cooked build that scans through the bot (M1 exit) |
| Day 14 | Director + GD | M1 review: 07 drives and scans on the basin; gates green |

## 7. Decisions (Director, 2026-10-26)
Decided: (1) modules per domain, Autoplay the only plugin; (3) Slate-first UI pending the 2-day spike in M1; (4) Unity takes no new
features, and each domain owner writes its own `docs/port/PORT_NOTES.md` entries; (5) rolling sphere with fixed 50 Hz substeps, Rover
verifies in M1. **Pending with the owner:** (2) the machine limit (2 heavy UE processes and closing Unity editors, or a RAM upgrade).

The original questions, kept for the record:
1. Confirm modules (not plugins) per domain and the plugin only for Autoplay.
2. Approve the 2-slot machine limit and closing Unity editors during the port, or a RAM upgrade.
3. Approve the Slate-first UI default pending the 2-day spike.
4. Decide whether the Unity repo accepts **no** new features (owner: yes) and who writes `PORT_NOTES.md` entries.
5. Confirm physics direction (rolling sphere with fixed 50 Hz substeps) for rover to implement and verify in M1.
