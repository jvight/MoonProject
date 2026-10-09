# Lofi Lunar — Technical Architecture

> Owner: Director box. Changes to this document go through the Director.

## Engine baseline
- **Unity 6000.0.78f1 (LTS)**, URP 17.0, Windows standalone (PC first; gamepad + keyboard/mouse).
- **Input System only** (`activeInputHandler` = Input System Package). No `UnityEngine.Input`, no `OnGUI`.
- **Cinemachine 3.x** (`Unity.Cinemachine` namespace) for every camera.
- **UI Toolkit** for UI (UXML/USS are text — reviewable and merge-friendly). No uGUI canvases.
- Physics: PhysX, fixed timestep 0.02 s, global gravity **(0, -1.62, 0)** (lunar). Systems needing a
  different feel (rover downforce, the salvage bit magnet) add their own forces explicitly from tuning values.

## Folder layout
```
Assets/_Project/
  Scripts/
    Core/       MoonProject.Core       - context, event bus, input, layers, shared event contracts
    Art/        MoonProject.Art        - low-poly mesh kit, palette (runtime-safe, used by builders & procedural runtime)
    World/      MoonProject.World      - terrain generation & queries, sky, lighting/atmosphere, scatter
    Rover/      MoonProject.Rover      - rover physics controller, suspension visuals, camera rig, rover VFX
    Gameplay/   MoonProject.Gameplay   - salvage, materials, sonar, excavation, tether, base, upgrades, progression, save
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
  (`MaterialSalvaged`, `RoverLanded`, `RelicSurfaced`).
- `Subscribe` returns an `IDisposable`; subscribers dispose in `OnDestroy`/`OnDisable`.
- Publishing is allocation-free for struct events.

## Tuning & content data
- Every gameplay/feel number lives in a ScriptableObject tuning asset (`RoverTuning`, `TetherTuning`, ...)
  under `Assets/_Project/Data/Tuning`, with `[Tooltip]` and `[Range]`/`[Min]`. Runtime code never writes to them.
- Content (relic definitions, upgrade definitions) is ScriptableObject data under `Assets/_Project/Data/Content`.
- Persistent progress: a versioned plain C# `SaveData` serialised with `JsonUtility` to `Application.persistentDataPath`.

## Localization
- Contract `Core/ILocalization` (current language, `Get(key)`, a language-changed event), implemented and registered
  by the UI domain. String tables per language live in `Assets/_Project/Data/Localization/` (one source of truth;
  `en` is the source, `vi` must cover every key — a test enforces parity).
- Keys are stable, dotted and lower-case: `ui.pause.resume`, `hint.excavate`, `relic.<id>.name`,
  `relic.<id>.memory`, `upgrade.<id>.<level>.effect`. Content assets reference ids, never prose.
- English (`en`) is the primary and default language; Vietnamese (`vi`) is secondary and chosen by the player in the
  pause menu settings; the choice is saved.

## Physics layers (`MoonProject.Core.Layers`)
| # | Name | Used by |
|---|---|---|
| 6 | Ground | terrain chunks, base floors |
| 7 | Rover | rover physics sphere + body colliders |
| 8 | Relic | excavated/tetherable relics |
| 9 | Pickup | loose salvage bits, friend parts, cassettes (trigger-only interactions) |
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
  CoilSocket           empty under the chassis at the bottom of the belly plate, +Y up; HoverCoils mounts here
```

Hover-Jump coils:
```
Generated/Art/Rover/HoverCoils.prefab  (meshes only; parent it to RoverModel's CoilSocket with identity)
  HoverCoils           mounting plate (static)
    Coil_FL/FR/RL/RR   one spring each, pivot at the spring's top, hanging down ~0.13 m: squash local Y while charging
      Glow_<corner>    ring around the foot pad, own glow renderer on M_LowPolyGlowOff (dark glass, glows cyan):
                       light it with MaterialPropertyBlock _EmissionColor = white x charge
```
Visible kit (M3-11, VISION ruling 11). Meshes only, `M_LowPoly`; each prefab parents to its socket with identity.
Every piece must read in silhouette from the default chase camera and at 30 m, sturdy and well-loved, never spiky.
```
Generated/Art/Rover/Kit_LampBar.prefab       caged lamp bar across the front, root at HeadlampSocket
  Lamp_0..2                                  own glow renderers on M_LowPolyGlowOff (WarmLamp), lit via SetVector
Generated/Art/Rover/Kit_CapacitorDrum.prefab twin-ready capacitor drum, root at DrumSocket_L / DrumSocket_R
  Glow                                       own glow renderer on M_LowPolyGlowOff (cyan, like the coils), lit while boosting
Generated/Art/Rover/Kit_CargoRack.prefab     strapped rear rack basket, root at CargoSocket
  RelicSeat                                  empty where a carried relic rests (+Y up)
RoverModel additions:
  DrumSocket_L, DrumSocket_R                 empties on the flanks, +X outward
  SolarWing/CellFilled                       the replacement cell, hidden until Tilly's gift
  Body/Decal07Fresh                          a freshly stencilled "07", hidden until Bell's gift (the faded one hides then)
  Antenna/Pennant                            small radio pennant, hidden until Bell's gift
```

Renaming or re-pivoting any node is a contract change: coordinate through the Director.

Glow modulation: `Eye` and `AntennaTip` are their own MeshRenderers on the shared palette material. Their glow
comes from the palette emission map, which carries the warm swatches' HDR strength; the material's `_EmissionColor`
is authored as white (1,1,1). At runtime every domain scales glow per renderer with a `MaterialPropertyBlock`:
`SetVector(_EmissionColor, new Vector4(i, i, i, 1))`, a **linear** multiplier (1 = authored, 0 = dark, 2 = twice
the light). Never `SetColor`: Unity treats a colour as gamma-encoded and raises the intensity to ~2.2, so 2 would
mean ~4.6×. Any custom palette shader must keep the URP `_EmissionColor` name.

## Contract: M2 content (Art -> Gameplay)
Meshes-only prefabs on `M_LowPoly` (no colliders, no scripts; gameplay adds physics), +Y up, +Z front. Pivots: pickups and
relics at the centre of mass (they are physics bodies); Lander, MuseumShelf and RadioTower at their ground-contact centre
(they stand on anchors). Gameplay references them by path; art may refine their looks freely without renaming.
```
Generated/Art/Relics/Relic_<Id>.prefab                                       0.5–1.2 m, one per relic id:
    cassette_player, rubber_duck, golden_record, astronaut_boot, teapot, garden_gnome
Generated/Art/Base/Lander.prefab        the abandoned lander (base origin = its pivot on the ground)
    ShelfAnchor, TowerAnchor          empties where gameplay places the museum shelf and radio tower
    LampSocket_0..3                   empties for warm base lights
Generated/Art/Base/MuseumShelf.prefab
    Slot_0..Slot_5                    empties, +Y up, where deposited relics rest
Generated/Art/Base/RadioTower_L1|L2|L3.prefab   three upgrade stages, same footprint
    BeaconSocket                      empty at the top (signal light / beam)
Generated/Art/Base/Workbench.prefab  Kenji's bench (root on the ground at the bench centre, +Z = front, 2.3 m wide)
    Lights                            the work lamp's bulb, own glow renderer (WarmLamp)
    SparkSocket                       empty between the vice jaws (+Z out of the front): upgrade sparks
Lander.prefab  WorkshopAnchor        empty at lander-local (12.5, 0, -2), identity; the bench stands on it and its
                                      shop pad is centred 3.2 m in front (+Z), radius 2.4, nothing tall on it
```

## Contract: friend Tilly (Art -> Gameplay)
```
Generated/Art/Friends/Tilly.prefab  (meshes only; root on the ground between the feet, +Z = gaze, +Y up)
  Body                 body, legs, arms, rotor ducts (static)
  Eye                  the lens, its own glow renderer (EyeGlass glows cyan); pivot = lens centre, +Z = gaze
  Rotor_FL/FR/RL/RR    blades + hub, pivot at the hub, spin about local Y (identity rotation)
  Antenna              whip with ball tip, pivot at its base (spring wobble)
  PartLamp_0..2        own glow renderers on M_LowPolyGlowOff: dark until lit via MaterialPropertyBlock
                       _EmissionColor = white x intensity (the 0/3 parts readout, amber)
  TetherPoint          empty under the belly (tether / repair beam attach point)
Generated/Art/Friends/Tilly_Broken.prefab  same node names, posed lying on her side (pose baked into the nodes' local
  transforms), Rotor_FR bent, antenna kinked, Eye on M_LowPolyGlowOff (dark; a property block can flicker it on)
Generated/Art/Friends/Part_TillyRotor|Part_TillyLens|Part_TillyCell.prefab  0.27-0.36 m pickups, centre-of-mass
  pivot, warm amber accents (never cyan)
Lander.prefab  FriendSocket_<id>: empty on top of the friend's perch, +Y up, +Z = hatch side
```
`M_LowPolyGlowOff` is `M_LowPoly` with `_EmissionColor` authored black and `_EMISSION` on: its glow renderers start
dark and are lit per renderer with the same MaterialPropertyBlock contract as 07's eye.

## Contract: friend Bell, cassettes (Art -> Gameplay), M3-05
Bell is Ro's radio cabinet on four spindly camera-tripod legs: ~1.6 m tall overall (cabinet ~0.9 × 0.5 × 0.8 m on
~0.75 m legs), slender, a little taller than 07, warm wood-and-enamel body, amber dial. Meshes only, `M_LowPoly`.
```
Generated/Art/Friends/Bell.prefab  (root on the ground between the feet, +Z = dial face, +Y up; standing rest pose)
  Body                 the cabinet, pivot at the centre of its underside (sway / bob / dance)
    Lid                pivot on the top-back hinge; local X rotation 0 = closed, negative = opening
    DialFace           the dial plate on the front, +Z out
      Needle           pivot at the needle hub; rotates about local Z, 0 = left end of the band, + = sweeping right
      DialLamp         own glow renderer on M_LowPolyGlowOff behind the dial glass (amber)
    Speaker            grille + cone, pivot at the cone centre (pulse along local Z)
    TapeSlot           empty at the cassette door on the front, +Z out (07's beam slides the tape in here)
    Knob               chunky tuning knob right of the dial (~1.25 m high); turns about local Z; 07's beam taps it
    Antenna            telescopic whip, pivot at its base (spring wobble)
    PartLamp_0..3      own glow renderers on M_LowPolyGlowOff: 0..2 = parts, 3 = the tape (amber)
  Leg_FL/FR/RL/RR      pivot at the hip under Body; swing about local X, splay about local Z
    Shin_<corner>      pivot at the knee; bend about local X; the foot is the shin's bottom end
Generated/Art/Friends/Bell_Broken.prefab  same node names, posed tipped back against a wall (pose baked into local
  transforms), lid open, one leg folded under, DialLamp and PartLamps dark
Generated/Art/Friends/Part_BellKnob|Part_BellCone|Part_BellValve.prefab  0.27-0.36 m pickups, centre-of-mass pivot,
  warm amber accents (never cyan)
Generated/Art/Pickups/Cassette_<id>.prefab  chunky readable tape ~0.35 m wide, centre-of-mass pivot, label colour per
  id: after_dark_1, dust_and_honey, slow_orbit (more ids later, same recipe)
Generated/Art/Props/LogCache.prefab  Ro's battered tin box (~0.5 m), root on the ground, +Z = lid front, lid ajar
Generated/Art/Base/CassetteShelf.prefab  small standing rack, root on the ground, +Z = front
  Slot_0..Slot_7       empties, +Y up, +Z = label facing; a tape stands upright on each
RadioTower_L1|L2|L3.prefab  (same on all three stages)
  BellCorner           empty on the ground beside the tower foot, +Z = facing the lander; Bell stands here
  CassetteShelfAnchor  empty on the ground next to BellCorner, +Z = facing the lander; the shelf stands here
```
The sockets must leave a clear 2.5 m radius for Bell's dance and 07 parking in front of the dial (Interact range).

## Contract: relay masts (Art -> Gameplay), M3-06
The crew's old radio relay masts: a chunky tapering mast about 8 m to the lamp, with a small relay dish, a junction
box at its foot and guy wires. Meshes only, `M_LowPoly`. It must read as a silhouette at 100–280 m: a few big shapes
(taper, cross braces, dish, lamp housing), never a thin lattice that vanishes at distance.
```
Generated/Art/Relay/RelayMast.prefab  (restored pose; root at the pad centre on the ground, +Z = toward home, +Y up)
  Base                 footing plinth + junction box (static)
  Mast                 pivot at the mast foot; the broken pose leans it about local X/Z
    Dish               relay dish near the top, pivot at its mount; yaw about local Y to aim home
    Lamp               lamp housing at the top, own glow renderer on M_LowPolyGlowOff (WarmLamp, HDR): dark until
                       lit via MaterialPropertyBlock (SetVector, linear, see "Glow modulation")
  PartSocket           empty on the junction box front, +Z out: the relay part slots in here
  BeamPoint            empty on the junction box: 07's repair beam target
Generated/Art/Relay/RelayMast_Broken.prefab  same node names; Mast leaning 12–15° with the dish hanging, Lamp on
  M_LowPolyGlowOff (dark), slack guy wires
Generated/Art/Relay/Part_RelayModule.prefab  0.27–0.36 m pickup, centre-of-mass pivot, warm amber accents
```
Gameplay places the mast on the `relay.<n>` anchor (root at Position, +Z = Forward). The 3 m pad must stay clear for
07 to park and for the radio-hop arrival.

## Contract: salvage sites (Art -> Gameplay), M3-13
Weathered wrecks and ruins (VISION ruling 12: faded paint, rust streaks, dust drifts, dents, slack cables), meshes only,
`M_LowPoly`. Each site is one prefab whose salvage points are separate, detachable nodes.
```
Generated/Art/Sites/Site_<id>.prefab   id: depot | kestrel | drill | garage | lander
                                       root on the ground at the site anchor, +Z = the anchor's Forward, +Y up
  Skeleton                    what remains when everything is salvaged (weathered frame, stays forever)
  Salvage_<n>_<Material>      4–8 per site, Material = Metal | Wiring | Optics; a self-contained piece with its pivot
                              at its attach point, so gameplay can detach and fly it to 07 without seams
    CutPoint                  empty where 07's beam aims and sparks (+Z out of the cut face)
  Salvage_<n>_<Material>_Drag optional big piece (0–1 per site) that must be tethered clear before cutting
  Heart                       empty in a sheltered spot inside the wreck: the site's crew relic rests here
Generated/Art/Pickups/Material_Metal|Wiring|Optics.prefab   0.30–0.40 m bundles, centre-of-mass pivot (fold-in and
                                       loose trail bits)
Generated/Art/Sites/Debris_<Material>_<n>.prefab            4–6 small loose wreck bits for the Kestrel trail,
                                       centre-of-mass pivot, each worth one bundle
```
- **Footprints.** Site footprints (World): depot 10 m, kestrel 8 m crater floor, drill 9 m, garage 10 m, lander 9 m.
  Nothing tall in a 3 m lane on the approach side (−Z).
- **Kestrel-3 silhouette.** It must read from the base at 185 m: a tall element, such as a solar wing standing on edge
  or a bent antenna mast, ≥ 6 m.
- **Readability.** Salvage pieces read as distinct chunky shapes, so the player can tell what is left.

## Contract: Kenji's Rover Bay (Art -> Gameplay, Rover), M3-14
The bay fits kit onto 07 (VISION ruling 14), so its arms reach every kit socket on a 07 parked on the turntable; a
piece is never dropped the last stretch (BayReachContractTests proves it on the real prefabs).
```
Generated/Art/Base/RoverBay.prefab   on the lander's WorkshopAnchor (12.5, 0, -2); bay space, metres
  Turntable                 own disc, top y 0.15, radius 1.75, hazard rim on the disc, centre hole radius 0.5;
                            rotates about local Y with 07 on it
  Arm_0..2                  shoulders on the crane rail at y 3.08
    Yaw                     shoulder turn about local Y
      Upper / Lower / Tip   pitch about local X, each link hanging along its parent's -Y; links 1.30 / 1.20 / 0.26
        SparkSocket         on the welding nozzle at Tip (0, -0.26, 0.06)
                            rest pose: Upper -90, Lower 170, Tip -80, yaw -90/-90/+90 (folded along the rail)
  FloorArm                  on the ground under the turntable axis, over a dark pit
    FloorLift               rises along local Y through the centre hole
      FloorTip              reaches 07's CoilSocket under the belly (Hover-Jump coils)
  HopperMouth               back-right corner (1.55, 1.15, -1.85), facing the turntable, in 07's line of sight
  Lamp_0/1, BaySign         glow renderers (SetVector, linear)
  Weather_Paint/Rust/Dust   cleanable skins (arms share one set)
```
Gameplay registers `Core/IRoverBay` from serialized references (never name lookups); Rover drives the joints with IK.

## Contract: world anchors (World -> Gameplay, Audio)
World registers `Core/IWorldAnchors` (Count / Get(index) / TryGet(id)): named `WorldAnchor`s (id, surface position,
horizontal forward, radius of clear drivable ground), deterministic for the world seed and fixed after initialisation.
`IWorldLayout` stays the small set of global landmarks; anything region-specific is an anchor. Ids live in
`Core/WorldAnchorIds`:
```
canyon.mouth      where the canyon opens off the basin, facing in
canyon.lip        top of the take-off ramp, facing across the chasm
canyon.landing    centre of the far landing apron, facing into the canyon
canyon.ledge      the glinting ledge seen from the base (relic)
canyon.alcove_<n> side alcoves numbered from the mouth inward
canyon.terminus   the terminus chamber (Bell + crew log cache, M3-05)
canyon.exit       top of the no-jump way back to the basin, facing down it
```
A missing anchor is a wiring bug: consumers `Debug.LogError` with context, never invent a fallback position.

## Verification ladder
1. `python tools/compile_check.py` — editor + player configs, zero warnings. Mandatory before every commit.
2. EditMode tests (`Assets/_Project/Tests/EditMode`) for pure logic: mesh kit, event bus, economy, save, curves.
3. `python tools/unity_batch.py` — headless Unity run on a worktree: run builders, capture screenshots,
   run scripted play sessions and print metrics. At most two batch editors run machine-wide, so boxes run the
   tests and captures that cover their change (`--filter`, `--assembly`, `--category`); the full PlayMode suite and
   the Playthrough golden path are the Director's, on the merged main.
4. Director integration: `python tools/director.py land <box> -m ...` merges a box (compile_check gates the merge)
   and hot-reloads the main editor (refresh, the box's builders, Main.unity); `outputs` commits regenerated assets;
   `verify --push` runs compile_check, EditMode, PlayMode and the Playthrough on main in the verification worktree
   and pushes only a commit that passed all four. Then real play mode and a feel review against `docs/VISION.md`.

## Performance budget (PC target, 1080p)
- 60 fps on mid-range GPU; < 1.5 ms CPU per gameplay system per frame.
- Zero GC allocations per frame in steady state (no LINQ, no closures, no `GetComponent` in Update).
- Terrain: chunked meshes, <= 300k triangles visible; props GPU-instanced / SRP-batched via the shared material.
