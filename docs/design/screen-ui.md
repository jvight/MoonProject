# 07 · Diegetic CRT Face Inventory (`SCREEN-UI`)

> **Status:** `[PROPOSAL]` — Concept & Technical Design Note for showing 07's inventory (`Tab`) directly on 07's own CRT screen face (`RoverModel/Neck/NeckLift/Head/Screen`).
> **Visual Artifacts:** Flat 1.49:1 layouts (`Logs/mockups/screen_v{1,2,3}_{en,vi}.png`), 3D on-face renders (`Logs/mockups/onface_v{1,2,3}_vi.png`, `Logs/mockups/onface_wide_night.png`), 6-beat transition storyboard (`Logs/mockups/storyboard.png`), and comparison matrix (`Logs/mockups/index.html`).

---

## 1. Overview & Layout Recommendation (`v1 > v3 > v2`)

### 1.1 Existing Screen Mesh & Shader Baseline
- **Physical Screen Geometry:** On 07's procedural hierarchy (`RoverModel -> Neck -> NeckLift -> Head -> Screen`), the `Screen` mesh is built by `build_crt_screen_bmesh` (`tools/art/blender/rover/rover_parts.py:1032-1212`) with `screen_width = 0.58 m`, `screen_height = 0.39 m` (aspect ratio `0.58 / 0.39 = 1.487:1`), `screen_bulge_z = 0.018 m`, `screen_corner_round = 0.075`, and an `8 × 6` curved quad grid (`tools/art/blender/rover/build_rover.py:114-121`).
- **Head & Visor Framing:** The `Neck` pivot sits at `(0.0, 0.92, 0.13)` in Unity rover-local space, `Head` sits at `(0.0, 1.18, 0.13)`, and `Screen` sits at local `(0.0, 0.08, 0.295)` (world `(0.0, 1.26, +0.425)` at rest), framed by the cream/orange `Visor` hood (`0.77 m × 0.51 m`, `tools/art/blender/rover/build_rover.py:102-113`) and the right-hand machined brass emitter pod (`Eye` at `(+0.38, 0.08, 0.08)`, `tools/art/blender/rover/build_rover.py:107`).
- **UV Orientation:** On `Screen`, `u_norm = 0` (`-X` in head space) is on the viewer's right when looking at 07's face from `+Z`, so `R1CrtFaceCore.hlsl` mirrors X via `float2 rawUv = float2(1.0 - input.uv.x, input.uv.y)` (`Assets/_Project/Shaders/R1/R1CrtFaceCore.hlsl:267`).
- **Existing Cargo & Equipment UI:** Currently, pressing `Tab` calls `UISystem.OpenCargo()` (`Assets/_Project/Scripts/UI/UISystem.cs:532-535, 763-771`), which opens the 2D overlay `CargoScreen` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:31, 98, 267-356`) with 8 hold slots (`HoldSlotsAcross = 4`) and `EquipmentPage` (`Assets/_Project/Scripts/UI/Cargo/EquipmentPage.cs:31, 91, 232-303`) showing 12 `RoverSocket` positions (`Assets/_Project/Scripts/Core/RoverSocket.cs:7-44`) over a separate off-screen `RoverPortrait` render texture (`Assets/_Project/Scripts/Rover/Portrait/RoverPortrait.cs:50-54`).

### 1.2 Layout Comparison & Recommendation (`[PROPOSAL]`)
All three `1152×768` (`1.5:1`) layouts use Variant C's **"Terminal hai màu"** palette (`#20140A` / `#140C06` warm phosphor ground, `#FFB547` / `#FFD27A` amber primary, `#5FF3FF` cyan telemetry/routing accent, `#FF8A5B` coral shortage/toast warning) and font pair (`Chakra Petch` headers + `IBM Plex Mono` data/diacritics):

1. **`[PROPOSAL]` Rank #1 (Recommended) — Layout v1 (`screen_v1_{en,vi}.png`, `onface_v1_vi.png`): Split Hold Left / 07 Schematic Right.**
   - **Structure:** Left column (`x: 22..538`) holds the 4×2 cargo grid (`01..08`) on top and the **Hovered Item Inspector Card** directly beneath it (item icon, name, description, and the cyan *"Where it goes"* routing diagram). Right column (`x: 552..1130`) shows 07's orthographic side-3/4 schematic surrounded by 10 socket pills (`S01..S10`) with orthogonal leader lines.
   - **Why it wins:**
     1. **Zero eye-jump:** Inspecting a slot in the 4×2 grid updates the card immediately below it in the same column while highlighting compatible target sockets (`S01 Focus Lens`, `S02 Warm Headlamp`) in cyan on the right schematic.
     2. **Curved CRT legibility:** Keeps all multi-word labels and Vietnamese diacritics inside the flatter central 85% of the `Screen` mesh, avoiding barrel compression at the extreme corners.
     3. **Seamless Docked Transfer View:** When 07 docks at the shelter (`CargoScreen.IsTransferView`, `Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:98`), the left column stays unchanged (`07's Hold`) while the right column swaps from `07 · Equipment Sockets` to `Home Storage Shelves`.
2. **`[PROPOSAL]` Rank #2 — Layout v3 (`screen_v3_{en,vi}.png`, `onface_v3_vi.png`): 07 in Centre + 8-Slot Horizontal Belt Below.**
   - **Strengths & Trade-offs:** Celebrates 07's silhouette in the upper hub with symmetric left/right socket wings, but the 1×8 horizontal belt pushes slots `01` and `08` into the lower curved corners of the CRT mesh and increases gamepad D-pad traversal up to 7 horizontal steps (`01 -> 08`) versus at most 4 steps in v1's 4×2 grid.
3. **`[PROPOSAL]` Rank #3 — Layout v2 (`screen_v2_{en,vi}.png`, `onface_v2_vi.png`): Terminal Directory Grammar (`/dev/07/hold/`, `/dev/07/sockets/`).**
   - **Strengths & Trade-offs:** Strong retro microprocessor flavour (`├── [03] Thấu kính ×2`, `$ cat /spec/thau_kinh.txt`), but replacing spatial leader lines on 07's body with text directory paths (`/đầu/Đầu phóng`, `/lưng/Khối pin`) weakens immediate spatial recognition of where each part mounts on 07.

---

## 2. Camera Framing, Easing & 6-Beat Timing (`[PROPOSAL]`)

### 2.1 Existing Camera & Body-Language Hooks
- `RoverCameraRig` drives a `CinemachineCamera` with `CinemachineOrbitalFollow` and `CinemachineRotationComposer` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:69-74, 259-289`), and already blends into close-up world-space `BayShot` poses (`focus` + `eye`) using `BlendShot(BayShot from, BayShot to, float t)` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:634-647`).
- `RoverBodyLanguage` drives procedural `Neck` yaw (`_neckYaw`) and `Head` pitch (`_headPitch` / `_headLiftPitch`) (`Assets/_Project/Scripts/Rover/Character/RoverBodyLanguage.cs:54-83`).
- `R1CrtFaceCore.hlsl` already implements a horizontal CRT collapse/open line at `uv.y = 0.5` (`BOOT_LINE_HALF_HEIGHT = 0.0055`) controlled by `_FaceCrtState.x` (`bootProgress`) and `_FacePhases.zw` (`bootLineEnd = 0.45`, `bootOpenSpan = 0.30`) (`Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs:31-32`, `Assets/_Project/Shaders/R1/R1CrtFaceCore.hlsl:252-289`).

### 2.2 Target In-Game Framing (`[PROPOSAL]`)
When `Tab` is open and settled:
- **07's Head Pose:** `Neck` yaws to `0°` (or toward the camera's approach bearing when obstructed; see §3) and `Head` pitches up **`+10°`** (`Head` local X rotation `-10°` in Unity / `+10°` in Blender), lifting the `Screen` center to rover-local `(0.00, 1.310, +0.407)` with surface normal `(0.00, +0.174, +0.985)`.
- **Camera Eye & Look Target:** Camera eye sits **`1.08 m`** along the lifted screen normal at rover-local **`(0.00, 1.498, +1.471)`** (in front of the screen and slightly above), aiming at `(0.00, 1.305, +0.407)` with vertical FOV **`26.8°`** (`42 mm` full-frame equivalent).
- **Screen Coverage:** At `1920×1080`, the `0.58 m × 0.39 m` `Screen` fills **`~75%` of the frame height (`~800 px` tall)** and `~62%` of the frame width (`~1190 px` wide). 07's `Visor` bezel (`0.77 m × 0.51 m`), brass `Eye` emitter pod, and neck collar frame the display, while a shallow Depth of Field (`focusDistance = 1.08 m`, `f/1.4`) blurs the surrounding lunar horizon.

### 2.3 6-Beat Transition Choreography (`[PROPOSAL]`)
All motion uses calm, zero-overshoot slow-in / slow-out easing (`SmoothStep(t) = 3t² - 2t³` and `EaseOutCubic(t) = 1 - (1 - t)³`), illustrated in `Logs/mockups/storyboard.png`:

| Beat | Window (s) | Duration | Easing | Visual & Shader Action |
|---|---|---|---|---|
| **1. Chase camera** | `t = 0.00 s` | `0.00 s` | Steady orbital follow (`0.6 s` damping) | Player presses `Tab`; `UISystem.OpenCargo()` locks drive input (`Assets/_Project/Scripts/UI/UISystem.cs:767`) while 07's calm amber pill eyes remain visible. |
| **2. 07 stops & lifts head** | `0.00 – 0.22 s` | `0.22 s` | `SmoothStep(t)` (`3t² - 2t³`) | 07's wheel velocity eases to zero; `Neck` yaws toward the camera arc and `Head` lifts `+10°` to greet the player. |
| **3. Blink & collapse to CRT line** | `0.18 – 0.34 s` | `0.16 s` | `SmoothStep(t)` vertical pinch | Mid-arc channel switch: `FaceDisplay` drives `bootProgress` down to `0.42` (`R1CrtFaceCore.hlsl:280-285`), pinching 07's eyes into a white-hot horizontal CRT scanline (`v = 0.50 ± 0.0055`). |
| **4. Camera arrives** | `0.00 – 0.55 s` | `0.55 s` | `SmoothStep(t)` (`3t² - 2t³`) | Camera orbits smoothly around 07's clear flank via a cylindrical `BlendShot` arc (never passing through 07's chassis) and settles `1.08 m` in front of `Screen`; the bright CRT carrier line holds for `40 ms`. |
| **5. Line expands into inventory** | `0.50 – 0.72 s` | `0.22 s` | `EaseOutCubic(t)` (`1 - (1 - t)³`) | The shader switches its source texture to `_ScreenUiTex` and expands `bootProgress` from `0.45 -> 1.00`, opening the horizontal line vertically into `07 · SELF-CHECK` while `HoldSlotLit` illuminates the 8 hold slots. |
| **6. Closing returns with happy blink** | `Close: 0.00 – 0.50 s` | `0.50 s` | `SmoothStep(t)` + arc-eye blink | Pressing `Tab`/`Esc` collapses the UI back to the horizontal line (`0.00 – 0.14 s`), blends the camera back to chase view (`0.00 – 0.50 s`), and expands `R1CrtFace` (`0.14 – 0.30 s`) into a `0.20 s` happy curved-eye blink (`^ ^`) before re-enabling drive input. |

---

## 3. What Happens When Terrain Blocks the Camera (`[PROPOSAL]`)

### 3.1 Existing Obstacle & Sightline Infrastructure
- `RoverCameraRig` uses `PropSightLine` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:105`) and `CinemachineDeoccluder` with `PullCameraForward` against `Layers.GroundMask | Layers.PropMask` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:290-310`).
- For close-up equipment bay shots, `BayShotComposer.TrySolve` (`Assets/_Project/Scripts/Rover/Camera/BayShotComposer.cs:32-73`) already tests **24 bearings around 07 (every `15°`)**, checking `sight.IsFree(eye, WallClearance)`, 4-corner frustum clearance `FrameClear` (`BayShotComposer.cs:133-154`), and `sight.Clear(focus, ...)` so no rock wall orbay post blocks the lens.

### 3.2 Three-Tier Obstruction Solver for the Face Camera (`[PROPOSAL]`)
When 07 is parked nose-first against a boulder, cliff wall, wreck hull, or steep crater rim (`+Z` blocked within `1.25 m` of `Head`):

1. **Tier 1 — Neck Yaw Bearing Search (`±75°` in `15°` steps, `07` turns its head to clear air):**
   - Because 07's `Neck` node (`tools/art/blender/rover/build_rover.py:102`, `render_suite.py:398-403`) rotates freely up to `±120°` around vertical Y, the solver evaluates candidate neck yaw angles `ψ ∈ {0°, ±15°, ±30°, ±45°, ±60°, ±75°}` using `ISightLine.IsFree` and `BayShotComposer.FrameClear` (`Assets/_Project/Scripts/Rover/Camera/BayShotComposer.cs:52-53, 133-154`) against `Layers.GroundMask | Layers.PropMask`.
   - If `+Z` is blocked by a rock wall in front of 07, **07 turns its neck `ψ` toward the nearest open flank** (e.g. `-60°` left) and the camera positions itself `1.08 m` along that rotated screen normal. Visually, 07 simply turns its head away from the wall to face the player.
2. **Tier 2 — Wider-Angle Close Pull-In (`d: 1.08 m -> 0.62 m` with FOV compensation):**
   - In a tight trench where even `±75°` bearings have less than `1.08 m` of clearance but at least `0.62 m` (`MinDistance`), the camera pulls in to the available clear distance `d_clear ∈ [0.62 m, 1.08 m]` and widens vertical FOV proportionally (`FOV_v = 2 * atan(0.258 / d_clear)`, up to `39.6°`) so `Screen` still occupies `~75%` of the frame height without clipping into the rock behind the camera.
3. **Tier 3 — Instant Unobstructed CRT Modal Fallback (`d_clear < 0.62 m`):**
   - If all bearings within `±75°` have less than `0.62 m` of clearance (e.g. 07 is wedged under an overhang), `TrySolveFaceShot` returns `false` (matching `BayShotComposer.TrySolve` fallback semantics at `BayShotComposer.cs:29-30`). The camera holds its safe deoccluded pose and the `1152×768` CRT `Screen` render texture is presented directly as a centered ~75%-height CRT bezel modal over a blurred viewport, guaranteeing the player is never locked out of `Tab` or shown the inside of a rock mesh.

---

## 4. Input: Mouse Ray onto Screen UV + Gamepad (`[PROPOSAL]`)

### 4.1 Existing Input & Cursor Baseline
- `UISystem.OpenCargo()` currently disables driving input (`_services.Input.Disable()`), switches `_cursor.Menu()`, and calls `_cargo.FocusFirst()` (`Assets/_Project/Scripts/UI/UISystem.cs:767-769`).
- `CargoScreen` and `EquipmentPage` run on Unity UI Toolkit (`VisualElement` trees inside `UIDocument`, `Assets/_Project/Scripts/UI/UISystem.cs:37`).

### 4.2 Mouse Raycast to `Screen` Mesh UV (`[PROPOSAL]`)
When the UI Toolkit panel renders into a dedicated `RenderTexture` (`_ScreenUiTex`) mapped onto `RoverModel/Neck/NeckLift/Head/Screen`:
1. **Analytical Plane-Plus-Bulge Ray Intersection (Zero Physics Collider Overhead):**
   - Rather than adding a `MeshCollider` to `Screen`, a lightweight math helper transforms the mouse ray `Camera.ScreenPointToRay(mousePos)` into `Screen` local space (`headTransform.InverseTransformPoint` / `InverseTransformDirection`).
   - It intersects the ray with the `0.58 m × 0.39 m` screen quad (`z ≈ 0.018 * cos(πx/0.58) * cos(πy/0.39)`, 2 Newton steps from `z = 0`), yielding normalized mesh coordinates `(u_mesh, v_mesh) ∈ [0, 1]²`.
2. **UV-to-Panel Coordinate Mapping:**
   - Accounting for the horizontal mirror in `R1CrtFaceCore.hlsl:267` (`u_raw = 1.0 - u_mesh`) and UI Toolkit's top-left origin (`y_panel = (1.0 - v_mesh) * 768`, `x_panel = u_raw * 1152`), `PanelSettings.SetScreenToPanelSpaceFunction` maps screen-space pointer events directly into the `1152×768` UI Toolkit panel.
   - Outside the `[0, 1]²` screen bezel, `SetScreenToPanelSpaceFunction` returns ` Vector2.positiveInfinity` (ignoring off-screen hovers, or closing `Tab` if the player clicks outside the visor bezel).
3. **Drag-and-Drop Parity:**
   - Because `SetScreenToPanelSpaceFunction` feeds standard UI Toolkit `PointerMoveEvent`, `PointerDownEvent`, and `PointerUpEvent` coordinates, existing hold-slot hover, click-to-fit, and drag-and-drop between hold slots and sockets work with zero changes to `CargoScreen` event handlers.

### 4.3 Gamepad & Keyboard Navigation (`[PROPOSAL]`)
- **Initial Focus:** `OpenCargo()` calls `_cargo.FocusFirst()` (`Assets/_Project/Scripts/UI/UISystem.cs:769`), placing the cyan focus brackets on Hold Slot `01` (or the first non-empty slot) immediately at `t = 0.50 s` when the CRT line expands.
- **2D Spatial D-Pad / Stick Traversal (Layout v1):**
  - Left/Right (`D-Pad` / `Left Stick`) steps across the 4×2 Hold Grid (`01..04`, `05..08`), wrapping seamlessly from column 4 (`Slot 04` / `08`) into the left socket bank (`S04..S08`) and then the right socket bank (`S01..S03`, `S09..S10`).
  - `LB` / `RB` (or `Q` / `E`) jumps focus directly between the **Hold Grid** (left panel) and **07's Equipment Sockets** (right panel).
  - `South Button` (`A` / `Cross` / `Enter` / `LMB`) fits or moves the focused part; `West Button` (`X` / `Square` / `R`) drops/unfits; `East Button` (`B` / `Circle` / `Tab` / `Esc`) triggers Beat 6 (Close).

---

## 5. Sound: CRT Thump + Degauss from the Audio Audition (`[PROPOSAL]`)

### 5.1 Diagnosis of Existing `Tab` Audio
- Currently, `UISystem.OpenCargo()` and `CloseCargo()` publish `UiCueKind.Confirm` and `UiCueKind.Back` (`Assets/_Project/Scripts/UI/UISystem.cs:770, 778`), which `UiAudio` routes to `cues.ui_confirm` and `cues.ui_back` (`Assets/_Project/Scripts/Audio/UiAudio.cs:48-55`, `tools/audio/cues.py:589-605`)—two pure additive sine notes (`1.0, 0.15, 0.05` harmonics, `3 ms` linear attack) with no physical switch, cone, or CRT coil transient (`D:\Project\MoonProject-wt\agy-audio\Logs\round1\REPORT.md:18-20`).
- Meanwhile, the codebase already contains physical synthesis building blocks for 07's power-on thump (`wake_power_thump`: `95 -> 52 Hz` pitch-dropping body thump + `D2` `73.4 Hz` warm hum in `tools/audio/cues.py:2348-2358`), first-open hold boot ticks (`HoldBootStarted`, `HoldSlotLit`, `HoldItemArrived` in `Assets/_Project/Scripts/Audio/UiAudio.cs:88-96`), and the Round 2 **C2 ("Radio bench")** audition primitives (`bakelite_switch`, `relay_clack`, and `speaker_cone_thump` `180 -> 126 Hz` in `D:\Project\MoonProject-wt\agy-audio\tools\audio\audition\radio_bench.py:102-216`).

### 5.2 Proposed Diegetic Sound Layering for `Tab` Open / Close (`[PROPOSAL]`)
All cues stay in the warm **`D3`–`D5`** D-major pentatonic register and `-22.0 LUFS` momentary loudness target (`ui_2d`):

1. **Open (`tab_open_crt`, `0.72 s` total, synchronized with Beats 1–5):**
   - **`t = 0.00 s` (Beat 1 -> 2, Switch & Neck Lift):** Rotary bakelite switch click (`bakelite_switch`, `1250 Hz` & `2350 Hz` body modes) + quiet felt-damped neck servo whirr (`D3 -> A3`).
   - **`t = 0.18 s` (Beat 3, Face Collapse to CRT Line):** C2 `speaker_cone_thump` (`180 -> 126 Hz` paper-cone excursion + `410 Hz` chassis box resonance) paired with a soft magnetic reed-relay clack (`relay_clack`) as the vertical deflection collapses to the horizontal scanline.
   - **`t = 0.50 s` (Beat 5, CRT Line Expands + Degauss Bloom):** Low transformer/degauss thump (`wake_power_thump` `95 -> 52 Hz` sub-thud + `146.8 Hz` `D3` degauss coil ring decaying over `220 ms`) opening into the warm two-note valve settle (`D4 -> A4`) and rapid `HoldSlotLit` reed ticks (`18 ms` apart) as the 6 occupied slots populate.
2. **Close (`tab_close_crt`, `0.50 s` total, synchronized with Beat 6):**
   - **`t = 0.00 s` (UI Pinch to Line):** Reversed reed-relay contact release + capacitor voltage sag (`A4 -> D4` over `140 ms`).
   - **`t = 0.16 s` (Happy Blink `^ ^`):** Soft `140 -> 92 Hz` cone settle thud + a brief, gentle `D4 -> F#4` heterodyne valve/ocarina breath affirm as 07's eyes blink back on.
3. **In-Screen Interactions (`Hold` & `Sockets`):**
   - Slot hover: C2 `ticker_line` micro-reed tick (`A4`, `-28.0 LUFS`).
   - Part pick / fit / refuse: C2 `item_pick` (`E4 -> A4` patch-plug click), `item_drop` (`D4/D3` seated transformer thump + socket latch), and `item_refuse` (`B4 -> F#4` muted relay drop).

---

## 6. Which Other Screens Could Later Move Onto 07's Face (`[PROPOSAL]`)

Not every UI screen belongs on 07's CRT face. The guiding rule (`docs/VISION.md`) is **diegetic authorship**: *if 07's onboard computer measures or manages it, it belongs on 07's CRT face; if the human crew printed, wrote, or recorded it on physical media, or if it requires a wide bird's-eye spatial view, it stays off 07's face.*

| Screen / Element | Current Implementation | Move to 07's CRT Face? | Rationale (`[PROPOSAL]`) |
|---|---|---|---|
| **1. Inventory & 07 Sockets (`Tab`)** | `CargoScreen` + `EquipmentPage` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:31`, `EquipmentPage.cs:31`) | **YES (Primary)** | 07's own self-check (`07 · SELF-CHECK`) of its cargo hold and 12 body sockets; replaces the redundant off-screen `RoverPortrait` render texture (`RoverPortrait.cs:51`) with 07's real body in the world. |
| **2. Docked Shelter Transfer** | `CargoScreen.IsTransferView` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:98`) | **YES (Layout v1 Right Swap)** | When docked at the shelter, 07's umbilical links its onboard CRT to the shelter's storage bus: left column shows `07'S HOLD`, right column shows `HOME STORAGE SHELVES`. |
| **3. Detailed Scan Readout / Salvage Analysis** | `PlaceCard` / scan site counters (`Assets/_Project/Scripts/UI/Hud/PlaceCard.cs:16`) | **YES (Optional / Secondary)** | When 07 finishes scanning a wreck or mineral vein, showing the material breakdown (`Metal ×4, Optics ×2`) on 07's face (or as a quick glance-back) fits 07's role as the sensor instrument. Short driving toast chips (`PlaceCard` title while driving) stay on the HUD so the camera doesn't stop driving. |
| **4. Workshop / Upgrade Craft Preview** | `RecipeCard` / `PartStation` (`Assets/_Project/Scripts/Gameplay/Equipment/PartStation.cs`) | **HYBRID (Bay Camera + Face Status)** | During an actual Rover Bay fitting, `BayShotComposer` (`BayShotComposer.cs:32`) already frames the physical robotic arm bolting the part onto 07's body—which is more rewarding to watch externally than staring only at the screen. However, browsing recipes at the bench can mirror onto 07's face. |
| **5. Crew Logs, Liner Notes & Cassettes** | `MemoryCard` (`MemoryKind.CrewLog`, `Assets/_Project/Scripts/UI/Hud/MemoryCard.cs:112, 234-235, 273`) | **NO — Keep Paper / Tactile** | Per task brief and `docs/VISION.md`, crew logs, handwritten notes, and cassette liner cards are human keepsakes left by Tilly, Kenji, Bell, Moss, and Ro. Rendering them on cotton rag paper / physical cassette cards preserves the contrast between 07's amber CRT terminal and the crew's human artifacts. |
| **6. Basin Topographic Map (`M`)** | `UISystem` map check (`Assets/_Project/Scripts/UI/UISystem.cs:760`) | **NO — Keep Full-Screen / Survey Chart** | A basin-wide map requires high spatial resolution and wide aspect ratio to read crater contours, relay links, and routes; cramping it into `1152×768` at 75% frame height would hurt navigation legibility. |
| **7. Pause & Accessibility Settings (`Esc`)** | `PauseMenu` (`Assets/_Project/Scripts/UI/Pause/PauseMenu.cs:396, 450`) | **NO — Keep Instant 2D Overlay** | Players opening `Esc` (especially to toggle `GentleCamera` or adjust audio/display) need an instant, zero-camera-motion overlay that works even on the title screen before 07 wakes. |

---

## 7. Pixel Budget: Render-Texture Size & Font Sizes (`[PROPOSAL]`)

### 7.1 RenderTexture Budget & Memory Neutrality (`[PROPOSAL]`)
- **Resolution & Format:** `_ScreenUiTex` is a single **`1152 × 768`** (`1.5:1`, matching `Screen`'s `0.58 m / 0.39 m = 1.487:1` aspect within `0.8%`) `RenderTextureFormat.ARGB32` (or `R11G11B10_UFloat` / `DefaultHDR` if UI bloom is pre-composited), no depth buffer needed (`depthBits = 0`), bilinear filtering, `mipMap = false`.
- **VRAM Cost:** `1152 × 768 × 4 bytes = 3,538,944 bytes` (**`3.38 MB`**).
- **Net VRAM Savings via `RoverPortrait` Retirement:** Currently, `RoverPortrait.Initialize()` allocates a dedicated `RenderTexture` with a 16-bit depth buffer (`Assets/_Project/Scripts/Rover/Portrait/RoverPortrait.cs:18, 51-54`) plus two extra directional lights (`_key`, `_rim`) and a second URP `Camera` render pass every frame the `07` tab is open (`RoverPortrait.cs:65-75, 102-118`). Moving the UI onto 07's actual head in the world **eliminates the extra URP camera pass and replaces `RoverPortrait`'s render texture**, keeping net VRAM flat (`~3.4 MB`) and reducing GPU draw calls.
- **On-Demand Rendering:** Just as `RoverPortrait.SetActive(bool)` disables its camera when `Tab` is closed (`RoverPortrait.cs:72-75`), the `Screen` UI `PanelSettings` / `RenderTexture` only updates while `_cargo.IsOpen` is `true` (or during the `0.22 s` Beat 5 expansion); during normal driving, `R1CrtFace.shader` runs its zero-texture procedural eye SDF math (`R1CrtFaceCore.hlsl:290-370`).

### 7.2 Effective On-Screen Pixel Density & Minimum Font Sizes (`[PROPOSAL]`)
At `1920 × 1080` output resolution, when `Screen` fills **75% of the frame height (`810 px` tall × `1205 px` wide)**:
- **1:1 Pixel Mapping Ratio:** `810 / 768 = 1.055×` vertically and `1205 / 1152 = 1.046×` horizontally. Every `1 px` in the `1152×768` `RenderTexture` maps to **`~1.05 screen pixels` at 1080p** (`~1.40 px` at 1440p, `~2.10 px` at 4K)—avoiding downsampling blur!
- **Typography Scale Floor (verified in `Logs/mockups/onface_v{1,2,3}_vi.png` for Vietnamese stacked diacritics like `Ổ`, `Ắ`, `Ệ`, `Ữ`):**

| UI Role | Font Asset (`Assets/_Project/UI/Fonts/`) | Size in `1152×768` RT | Effective Height at 1080p (`1.05×`) | Minimum Allowed Floor |
|---|---|---|---|---|
| **Top Self-Check Header (`07 · TỰ KIỂM TRA`)** | `ChakraPetch-Bold.ttf` | `21 px` | `~22.1 px` | `18 px` |
| **Hovered Item Title (`Thấu kính ×2`)** | `ChakraPetch-Bold.ttf` | `18 – 19 px` | `~19.5 px` | `16 px` |
| **Section Headers (`KHOANG HÀNG CỦA 07`)** | `ChakraPetch-Bold.ttf` | `16 px` | `~16.8 px` | `15 px` |
| **Hold Slot / Socket Part Names** | `IBMPlexMono-SemiBold.ttf` | `12.5 – 14 px` | `~13.2 – 14.7 px` | `12 px` |
| **Inspector Body & Toast Line** | `IBMPlexMono-Regular/Bold.ttf` | `12.5 – 13.5 px` | `~13.2 – 14.2 px` | `12 px` |
| **Secondary Tags / Key Badges (`[LMB]`, `Ô 03`)** | `IBMPlexMono-Medium.ttf` | `11 – 11.5 px` | `~11.6 – 12.1 px` | `10.5 px` |

- **Shader Scanline & Curvature Tuning in UI Mode:** While `R1CrtFace.shader` uses `_Curvature = 0.08` and `_ScanlineDepth = 0.16` for 07's large expressive eyes (`Assets/_Project/Shaders/R1/R1CrtFace.shader:29-31`), the shader interpolates to **`_Curvature = 0.03`** and **`_ScanlineDepth = 0.07`** (and zeroes specular hotspot glare) during Beat 5 (`bootProgress` in UI mode) so 12 px Vietnamese diacritics never alias against scanline troughs.

---

## 8. Risks & Mitigations (`[PROPOSAL]`)

### 8.1 Risk 1 — Legibility (Small Displays, Steam Deck `1280×800`, Vietnamese Diacritics, Specular Glare)
- **Failure Mode:** At `1280×800` (720p/800p handhelds), a 75%-height screen is `600 px` tall (`0.78×` downsampling of `1152×768`), and world lights reflecting off `Screen` (`_Smoothness = 0.86`, `R1CrtFace.shader:28`) can wash out text (as observed in our initial Blender render before suppressing screen specular).
- **Mitigation (`[PROPOSAL]`):**
  1. **Suppress Direct Specular on `R1_Screen` in UI Mode:** Scale `_Smoothness` / direct light specular contribution by `0.1` when the inventory is open so world sun/point lights never create a blinding hotspot over text.
  2. **Respect `TextScale` / Handheld Zoom:** Tie the camera's Tab distance `d` to `PlayerSettings` text size (`_textScale` in `Assets/_Project/Scripts/UI/UISystem.cs:488`): on `"Large"` text scale or `< 900p` vertical resolution, pull `d` from `1.08 m` to `0.94 m` so `Screen` fills **86% of frame height** (`~690 px` at 800p, `~930 px` at 1080p).

### 8.2 Risk 2 — Motion Sickness from Repeated `Tab` Camera Swings
- **Failure Mode:** Players who toggle `Tab` frequently while salvaging wrecks may experience vection or discomfort if the camera swings `180°` from behind 07 to 07's face every time.
- **Mitigation (`[PROPOSAL]`):**
  1. **Honor `GentleCamera` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:179-187`):** The game already persists `PlayerSettings.GentleCamera` (`Assets/_Project/Scripts/UI/Settings/PlayerSettings.cs:197-203`, `PauseMenu.cs:396`) to suppress or soften camera swings (`RoverCameraRig.cs:476, 1243, 1288, 1391`).
  2. **Two-Level `GentleCamera` Behavior:**
     - **Standard (`GentleCamera == false`):** Full `0.55 s` `SmoothStep` cylindrical arc to 07's lifted face.
     - **Gentle (`GentleCamera == true`):** Skip the `180°` orbit swing entirely—perform a `0.25 s` soft cross-dissolve / fast cut straight to the stationary front-of-face framing (or present the `1152×768` CRT bezel modal in-place over the chase view with zero camera rotation).
  3. **Rapid Re-Toggle Debounce:** If `Tab` is pressed again while the `0.55 s` open blend is still in flight (`t < 0.55 s`), reverse smoothly from the current parameter `t` using `SmoothStep` rather than snapping to `t = 1.0`.

### 8.3 Risk 3 — Camera Inside Rocks, Wreck Walls, or Steep Slopes
- **Failure Mode:** 07 stops nose-up against a wreck wall, inside a tight salvage corridor, or on a `25°` crater slope, causing the fixed front-of-face camera offset `(0, 1.498, +1.471)` to land inside geometry or clip through the ground.
- **Mitigation (`[PROPOSAL]`):**
  1. **Cylindrical Arc + `BayShotComposer` Bearing Search (§3.2):** The camera never interpolates along a straight chord through 07's body, and tests 11 neck-yaw bearings (`0°, ±15° .. ±75°`) via `PropSightLine` (`Layers.GroundMask | Layers.PropMask`, `RoverCameraRig.cs:105, 294`) so 07 turns its head away from walls into open space.
  2. **Near-Clip & Pitch Clamp on Slopes:** Compute the camera's world target from `Head`'s actual world transform after suspension settle, clamp minimum ground clearance to `0.35 m` above `Layers.GroundMask`, set `nearClipPlane = 0.05 m`, and fall back to Tier 3 (centered CRT modal) whenever `d_clear < 0.62 m`.
