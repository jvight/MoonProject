# Lofi Lunar — Technical Architecture

> Owner: Director box. Changes to this document go through the Director.

## Engine baseline
- **Unity 6000.0.78f1 (LTS)**, URP 17.0, Windows standalone (PC first; gamepad + keyboard/mouse).
- **Input System only** (`activeInputHandler` = Input System Package). No `UnityEngine.Input`, no `OnGUI`.
- **Cinemachine 3.x** (`Unity.Cinemachine` namespace) for every camera.
- **UI Toolkit** for UI (UXML/USS are text — reviewable and merge-friendly). No uGUI canvases.
- Physics: PhysX, fixed timestep 0.02 s, global gravity **(0, -1.62, 0)** (lunar). Systems needing a
  different feel (rover downforce, scrap magnet) add their own forces explicitly from tuning values.

## Folder layout
```
Assets/_Project/
  Scripts/
    Core/       MoonProject.Core       - context, event bus, input, layers, shared event contracts
    Art/        MoonProject.Art        - low-poly mesh kit, palette (runtime-safe, used by builders & procedural runtime)
    World/      MoonProject.World      - terrain generation & queries, sky, lighting/atmosphere, scatter
    Rover/      MoonProject.Rover      - rover physics controller, suspension visuals, camera rig, rover VFX
    Gameplay/   MoonProject.Gameplay   - scrap, sonar, excavation, tether, cargo, base, upgrades, progression, save
    Audio/      MoonProject.Audio      - audio director, radio/music, SFX players driven by events
    UI/         MoonProject.UI         - UI Toolkit screens & HUD
    App/        MoonProject.App        - composition root (GameBootstrap), scene flow
    Editor/     MoonProject.Editor     - shared editor framework: scene build, builder registry, batch automation, import rules
  Tests/
    EditMode/   MoonProject.Tests.EditMode  - Core/App/Art-contract tests (Director)
    PlayMode/   MoonProject.Tests.PlayMode  - shared PlayMode harness + bootstrap smoke tests (foundation)
  Shaders/<Domain>/   hand-written URP shaders, owned by that domain (kept small)
  Generated/<Domain>/ builder output (meshes, prefabs, materials, textures) - never hand-edited
  Audio/        SFX/ + Ambience/ (WAV from tools/audio), Music/ (OGG Vorbis from tools/music)
  Data/         ScriptableObject tuning & content assets, Input/Controls.inputactions
  Scenes/       Main.unity (built by the scene builder), test scenes
tools/          python tooling (compile_check, unity_batch, unity_mcp, audio + music synthesis);
                deps pinned in tools/requirements.txt (numpy, scipy, soundfile)
docs/           VISION, ARCHITECTURE, ROADMAP
```

Each domain owns one folder and everything in it, so boxes never collide:
```
Scripts/<Domain>/                  MoonProject.<Domain>.asmdef            runtime
Scripts/<Domain>/Editor/           MoonProject.<Domain>.Editor.asmdef     builders, scene contributor, inspectors (Editor only)
Scripts/<Domain>/Tests/            MoonProject.<Domain>.Tests.asmdef      EditMode tests (Editor only, UNITY_INCLUDE_TESTS)
Scripts/<Domain>/Tests/PlayMode/   MoonProject.<Domain>.PlayModeTests.asmdef  scripted play sessions & feel metrics
```
`Assets/Scripts`, `Assets/Moon`, `Assets/Models`, `Assets/TutorialInfo`, `Assets/Materials` are the **legacy
prototype**. They are reference material only and are deleted by the Director once their replacement is
integrated (tracked in ROADMAP). Do not add to them.

## Assembly dependency graph (enforced by asmdefs)
```
Core  <- Art <- World
Core  <- Rover
Core, Art, World, Rover <- Gameplay
Core  <- Audio
Core, Gameplay(read-only queries) <- UI
everything <- App
everything <- Editor (editor only)
```
Domains talk **through Core**: shared event structs in `Core/Events`, shared interfaces in `Core`.
Never add an asmdef reference that creates a sideways dependency (e.g. Audio -> Gameplay); publish an event instead.
Assembly references use **names**, not GUIDs (tools/compile_check.py relies on it).

## Composition & lifetime
- `GameBootstrap` (App) is the **only** composition root. It owns a `GameContext`:
  `Events` (EventBus), `Input` (InputReader), plus services registered by domains.
- Scene systems implement `IGameSystem.Initialize(GameContext)`. The bootstrap holds an explicit, ordered,
  serialized list of systems (filled by the scene builder) and initialises them in that order. No `FindObjectOfType`,
  no `GameObject.Find`, no singletons, no static mutable state.
- Objects spawned at runtime receive the context from their spawner (`Initialize(context)` or constructor args).
- Services are plain C# classes where possible (testable in EditMode); MonoBehaviours are thin adapters.

## Events
- `EventBus` is a typed publish/subscribe hub owned by the context (not static).
- Events are `readonly struct`s in `Core/Events/<Domain>Events.cs` — data only, past-tense names
  (`ScrapCollected`, `RoverLanded`, `RelicSurfaced`).
- `Subscribe` returns an `IDisposable`; subscribers dispose in `OnDestroy`/`OnDisable`.
- Publishing is allocation-free for struct events.

## Tuning & content data
- Every gameplay/feel number lives in a ScriptableObject tuning asset (`RoverTuning`, `TetherTuning`, ...)
  under `Assets/_Project/Data/Tuning`, with `[Tooltip]` and `[Range]`/`[Min]`. Runtime code never writes to them.
- Content (relic definitions, upgrade definitions) is ScriptableObject data under `Assets/_Project/Data/Content`.
- Persistent progress: a versioned plain C# `SaveData` serialised with `JsonUtility` to `Application.persistentDataPath`.

## Physics layers (`MoonProject.Core.Layers`)
| # | Name | Used by |
|---|---|---|
| 6 | Ground | terrain chunks, base floors |
| 7 | Rover | rover physics sphere + body colliders |
| 8 | Relic | excavated/tetherable relics |
| 9 | Pickup | scrap (trigger-only interactions) |
| 10 | Prop | rocks, structures |
| 11 | Trigger | volumes (snap points, signal radius, zones) |

## Content pipeline: everything is built by code
- **Meshes**: `MoonProject.Art` low-poly kit (primitives, flat shading, palette UVs, combine, seeded noise).
  Editor builders in `Editor/Builders` turn recipes into mesh assets + prefabs under `Generated/`.
  Re-running a builder is deterministic (seeded) and idempotent.
- **Materials**: one shared palette material for nearly everything (`M_LowPoly`), plus a few special ones
  (tether beam, sonar ring, sky). Palette texture is generated from `PaletteSwatch` definitions.
- **Scene**: `Assets/_Project/Scenes/Main.unity` is produced by the scene builder from prefabs + world data.
  Only the Director runs it in the main project and commits the result. Nobody hand-edits scenes.
- **Audio**: `tools/audio` (shared DSP core `tools/audio/synth` + SFX recipes) renders SFX/ambience WAVs;
  `tools/music` (instruments, composition, mixing on top of the same DSP core) renders the radio tracks as OGG.
  Both are deterministic (seeded) and re-runnable.
- **Import settings** are enforced by an `AssetPostprocessor` per folder convention, not by hand-edited `.meta`.

## Contract: rover model rig (Art -> Rover)
Art's builder produces `Assets/_Project/Generated/Art/Rover/RoverModel.prefab` (meshes only, no colliders, no
scripts). Rover's builder wraps it into the playable `Rover.prefab`. Units in metres, +Z forward, +Y up,
origin at the centre of the ground contact patch. Overall ~2.2 m long, ~1.6 m wide, ~1.4 m tall, wheel radius 0.35 m.
The character brief is "07" in docs/VISION.md.
```
RoverModel
  Body                 chassis + cabin (pivot at origin)
  Bogie_L, Bogie_R     rocker-bogie arms (pivot at the body-side hinge, pitch = local X); visual only
  Wheel_FL, Wheel_FR   front wheels   (pivot at wheel centre, roll axis = local X)
  Wheel_ML, Wheel_MR   middle wheels  (direct children of RoverModel, NOT of the bogies)
  Wheel_RL, Wheel_RR   rear wheels
  Neck                 pivot at neck base on the body (yaw = local Y)
    Head               pivot at head hinge (pitch = local X); hooded sensor head
      Eye              the round lens (emissive WarmLamp); +Z = gaze direction
        TetherOrigin   empty at the lens centre, +Z = gaze (sonar/tether beams leave from here)
      Eyelid           brow/shutter, pivot on the eye's horizontal axis; local X rotation 0 = open, + = closing
  SolarWing            folded panel on the back, pivot at its hinge (local X rotation 0 = folded, + = opening)
  Antenna              pivot at antenna base (spring wobble)
    AntennaTip         small emissive tip (blinks)
  HeadlampSocket       empty, low on the body front, +Z = road light direction
  CargoSocket          empty, where the cargo bed upgrade attaches
  DustSocket_L/_R      empties at the rear wheel contact points
```
Renaming or re-pivoting any node is a contract change: coordinate through the Director.

Glow modulation: `Eye` and `AntennaTip` are their own MeshRenderers on the shared palette material. Their glow
comes from the palette emission map; the material's `_EmissionColor` is authored as white (1,1,1). At runtime the
rover scales glow per renderer with a `MaterialPropertyBlock` setting `_EmissionColor` = white × intensity
(1 = authored, 0 = dark, > 1 brighter, HDR). Any custom palette shader must keep the URP `_EmissionColor` name.

## Contract: M2 content (Art -> Gameplay)
Meshes-only prefabs on `M_LowPoly` (no colliders, no scripts; gameplay adds physics), pivot at the centre of mass,
+Y up, +Z front. Gameplay references them by path; art may refine their looks freely without renaming.
```
Generated/Art/Scrap/Scrap_Bolt|Scrap_Gear|Scrap_Panel|Scrap_Coil.prefab     0.3–0.5 m, TechGlow accents
Generated/Art/Relics/Relic_<Id>.prefab                                       0.5–1.2 m, one per relic id:
    cassette_player, rubber_duck, golden_record, astronaut_boot, teapot, garden_gnome
Generated/Art/Base/Lander.prefab        the abandoned lander (base origin = its pivot on the ground)
    ShelfAnchor, TowerAnchor          empties where gameplay places the museum shelf and radio tower
    LampSocket_0..3                   empties for warm base lights
Generated/Art/Base/MuseumShelf.prefab
    Slot_0..Slot_5                    empties, +Y up, where deposited relics rest
Generated/Art/Base/RadioTower_L1|L2|L3.prefab   three upgrade stages, same footprint
    BeaconSocket                      empty at the top (signal light / beam)
```

## Verification ladder
1. `python tools/compile_check.py` — editor + player configs, zero warnings. Mandatory before every commit.
2. EditMode tests (`Assets/_Project/Tests/EditMode`) for pure logic: mesh kit, event bus, economy, save, curves.
3. `python tools/unity_batch.py` — headless Unity run on a worktree: run builders, capture screenshots,
   run scripted play sessions and print metrics (once Foundation lands it).
4. Director integration in the main editor: real play mode, feel review against `docs/VISION.md` checklist.

## Performance budget (PC target, 1080p)
- 60 fps on mid-range GPU; < 1.5 ms CPU per gameplay system per frame.
- Zero GC allocations per frame in steady state (no LINQ, no closures, no `GetComponent` in Update).
- Terrain: chunked meshes, <= 300k triangles visible; props GPU-instanced / SRP-batched via the shared material.
