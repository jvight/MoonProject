# Hand-over: rover camera, face screen, rope, FX (Unity -> Unreal)

From the rover/ui worker, 2026-10-26. Unity is frozen; this is what the port should carry over and what it should not.
Paths are under `Assets/_Project/Scripts/`. Pure logic classes (marked *pure*) have EditMode tests and port 1:1;
MonoBehaviours are thin and should be re-expressed in the engine's own camera/actor model.

**Owner ruling to apply first:** 2026-10-26, the camera is the player's (`memory: camera-player-owned`): no automatic
camera on scan, tow or build; the player's orbit is sacred. Several moves below pre-date that ruling (marked **ruled out?**).
Port them only where the ruling still allows (screen-space or time-boxed beats the player can cancel by any input).

## Camera rig (`Rover/Camera/RoverCameraRig.cs`, tuning `Rover/Tuning/RoverCameraTuning.cs`)
- Chase orbit (yaw offset, elevation, radius, wall compression that lifts the camera when props are behind 07) + a
  stack of **moments** (`CameraMoment`, *pure*): ease in, hold, ease out; a moment swings yaw toward a subject point
  (`yawShare`, min/max swing), shifts the look point, lifts and pulls back. Any look input cancels a moment; drive input
  cancels the dock/tower/nudge/lamp-fit ones.
- **Face-inspect shot** (Tab, 07's CRT face fills the frame): `FaceInspectComposer` (*pure*) solves eye/focus from the
  screen's pose; `BlendFaceInspect` lerps yaw/elevation/radius/fov from the chase pose by `FaceInspectWeight`.
  The shot is *in front* of 07 and the chase is *behind* it, so a plain blend is a ~175 deg half-orbit.
- **Close = dip, not orbit** (`FaceCloseSequence` *pure*, `FaceCloseDip`): hold on the face 0.55 s while the CRT powers
  off, dim to black over 0.14 s (a global post volume: post-exposure -10 EV), put the camera back on the chase pose in ONE
  step under the dark (reset the camera's previous-state / damping so it does not glide), come back up over 0.22 s
  (~0.9 s total). Cancel on re-open. The old smoothstep orbit whirled up to ~330 deg/s: do not port an orbit.
  The open keeps its move (1.2 s) and has no pitch jump.
- **Near-prop dither** (`NearPropFade` + `_NearKeep` in the R1Lit shader): any prop renderer within 1.2-3 m of the
  lens dithers out (interleaved-gradient clip), damped 0.15 s half-life, restored on leaving; 07 and the tow load are
  spared. Cheap in UE as a per-material parameter or a dithered-opacity pass on a Prop collision channel.
- **Next-action nudge** (`GuidanceNudge`, `PendingNudge`, `TurnSettle`, all *pure*) **ruled out?**: pan the chase yaw once
  per target id toward an off-screen next-action (<=25 deg/s, settles with the target at the frame edge); a request made
  while another shot has the camera is held up to 15 s; waits while 07's own heading turns > 8 deg/s; measures from the
  camera's real yaw, not 07's heading; a drive key already held at start does not cancel it. Port the rules only if the
  design still wants a camera pan; a head-look/HUD cue is the lighter alternative.
- Lamp-fit moment (`LampFitMoment`, front 3/4 of 07's head 1.1 s in / 1.7 s hold / 1.4 s out) **ruled out?**; the beat's
  non-camera half (`LampFitBeat` *pure*, in `Rig/RoverGrowth.cs`: happy chirp at 0.7 s, head looks 40 deg left then right
  over 1.2 s so the head-mounted beam sweeps once) is fine to keep.

## Face screen / inventory (`UI/Cargo/*`, `UI/Databank/*`, `UI/UISystem.cs`, style `UI/GameUI.uss`)
- UI Toolkit panel rendered to a RenderTexture shown on 07's CRT (`R1CrtFace` shader); the Hold page (cells + sockets) and
  the Databank page (live wireframe model on a stage camera/RT) share it. In UMG/Slate terms: one widget tree into a render
  target on the head mesh; the databank stage is a scene-capture of a model on an isolated layer.
- **Build the whole screen early.** The two first-use hitches were lazy construction: the hold laid out/drawn invisibly
  before the first Tab (`CargoScreen.WarmUp`, 3 frames at opacity 0.004), and the databank page built lazily by the first
  Q flip cost 445-585 ms (`UISystem.BuildDatabankEarly`; stage films a stand-in once). Pre-create widgets, render targets,
  materials and pipeline states at load; never on the player's first key press.
- Contrast policy (`Rover/Tests/PlayMode/RoverFaceInventoryPlayModeTests.cs`): body/icon/accent strokes >= 7:1, quiet
  words >= 4.5:1, the line drawing of 07 is a graphic held to 3.5:1 (WCAG non-text asks 3:1); quiet text colour must stay
  under the body level (0.76 max channel). With SMAA/MSAA on, 1-1.5 px strokes lose contrast: use 2 px outlines.
- Fonts: IBM Plex Mono (Regular/Medium), Be Vietnam Pro for Vietnamese; strings in `Data/Localization/{en,vi}.json`.

## Tow rope (`Rover/Rig/RoverTowCable.cs`, `RopeWidth` *pure*, `TowCableShape` *pure*, `Tuning/TowCableSettings.cs`)
- Sag vs taut curve between the winch drum and the load's hitch (`TowCableShape`), winch drum spin follows slack.
- **Width in pixels, not metres**: `RopeWidth.World(distance, fov, pixels, minMetres)` keeps the rope >= 4 px at 1440p at
  any chase distance (reference height 1440); the hook 6 px. A rubber rope alone is invisible on dark regolith at night:
  a thin pale unlit **thread** rides 2.5 cm above it (separate line, own unlit material, otherwise the two z-fight).
  In UE: a ribbon/cable component with screen-size-driven width and a second thin unlit layer.
- Skid dust under a dragged load: speed-driven rate (0.6-4 m/s), ballistic grains (lunar gravity, no drag, no smoke),
  none when the load stands still.

## Other FX
- **Dish glint** (`Gameplay/Opening/DishGlintFlare.cs`): a small sharp warm-white four-point star, 0.3 deg half-angle (a few
  px at any distance), brightness capped ~1.1 (under bloom), 12 % flare then fast fade over 0.4 s, drawn over the ridge
  (no depth test: from the plain the dish sits behind its own summit). Never a sphere/halo.
- Wheel dust (`Rover/Fx/RoverWheelFx.cs`) is ballistic cone emitters; the tow's skid dust lives on the cable, not here.

## Tooling and measuring lessons
- Hitch table in the IL2CPP bot (`Autoplay/FrameTagger.cs`, `FrameTagLog.cs`): frames tagged Face/Transition/Build/Scan/Dock,
  Tab/Q-flip windows; **per-frame full-size screenshots distort timing** (each frame became 260-400 ms): read frames back
  asynchronously at low resolution or log camera state instead. The built-in profiler markers are not available in
  non-development players; use frame timing + your own stopwatch spans.
- Concurrent Unity builds on one machine make 100 ms-13 s stalls with almost no main-thread work: measure on a quiet machine.
- The editor test fixtures worth keeping as specs: `FaceCloseDipPlayModeTests`, `FaceCloseSequenceTests`,
  `TowCablePlayModeTests`, `NearPropFadeTests`/`NearPropFadeCaptureTests`, `NextActionNudgePlayModeTests`,
  `FaceOpenHitchTests`, `RoverFaceInventoryPlayModeTests`.

## Known open items (not solved in Unity)
- The B7 array-glint "lit and on screen within 20 s" warning (a content cue problem, not a camera one).
- A 30-100 ms Tab open/close main-thread spike seen only on a contended machine; not reproducible on a quiet one.
