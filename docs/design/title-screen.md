# Title Screen Specification — 07's CRT Screen & Turning-Moon Schematic

> **Status:** Concept & Architecture Specification (`TITLE-SCREEN`)  
> **Visual Prototype & Review Page:** `Logs/title/index.html`, `Logs/title/anim.html`, `Logs/title/anim.gif`, `Logs/title/menu_v{1,2,3}_{en,vi}.png`  
> **Related Specs & Code:** `VISION.md`, `docs/design/story-v4.md`, `Assets/_Project/Scripts/UI/Pause/PauseMenu.cs`, `Assets/_Project/Scripts/UI/Drawing/WordmarkPainter.cs`, `Assets/_Project/Shaders/R1/R1CrtFace.shader`, `tools/art/blender/rover/build_rover.py`

---

## 1. Concept Overview & Variant Recommendation

### 1.1 The Core Hook
Per `VISION.md` (*"The first look is the hook"*), the title screen reimagines the main menu as **Unit 07's amber-and-cyan CRT screen**, pairing the terminal command list on the left with a deterministic 60 fps 2D line-art schematic on the right: a little 2D 07 driving forward atop a slowly turning Moon sphere while surface landmarks (the Cargo Lander, the Broken Radio Tower, and a Solar Array), sparse twinkling stars, and an occluded dark Earth with a thin sunlit crescent rim drift past.

### 1.2 Variant Comparison (`v1`, `v2`, `v3`)

| Variant | Mockup Files (`1920×1080`) | Screen Aspect & Framing | Strengths & Trade-offs |
| :--- | :--- | :--- | :--- |
| **`v1` — Full-Monitor CRT** | `Logs/title/menu_v1_en.png`<br>`Logs/title/menu_v1_vi.png` | `16:9` (`1920×1080`) full viewport with moulded outer CRT bezel, barrel curvature, and 216 scanlines. | Maximum text and line-art size on 1080p / handheld screens (Steam Deck); zero 3D render cost on title; requires a camera fade or zoom transition into the 3D world on session start. |
| **`v2` — 07's 3D Head Bezel at Night** *(Recommended Primary)* | `Logs/title/menu_v2_en.png`<br>`Logs/title/menu_v2_vi.png` | `1.487:1` (`0.58 m × 0.39 m`, rendered at `1152×768`) mapped onto 07's curved `Screen` mesh (`tools/art/blender/rover/build_rover.py:120-125`) inside 07's 3D visor hood in the lunar basin at night. | Grounds 07 physically in the lunar night with the brass cheek lens pod (`build_rover.py:129`), tape deck, and carapace catching warm CRT bounce light; **seamless start**: `New Game` / `Continue` pulls the 3D camera directly back from 07's face into the chase pose without a cut. |
| **`v3` — Split-Frame Diegetic OS / Horizon Dome** | `Logs/title/menu_v3_en.png`<br>`Logs/title/menu_v3_vi.png` | `16:9` multi-window CRT terminal (`UNIT 07 // STANDBY MONITOR`) with framed command pane, live `TitleRadio` carrier waveform strip, and wide Horizon Dome viewport. | Strongest cohesion with Variant C's framed inventory/pause panels; wide Moon arc (`R = 540 px`) displays all three landmarks simultaneously; slightly busier initial silhouette than `v1`/`v2`. |

### 1.3 Recommendation
- **`[PROPOSAL]` Primary Default — Variant 2 (`v2`, 3D Head Bezel at Night):** Use `v2` as the default title presentation. Rendering the `1152×768` (`1.50:1 ≈ 1.487:1`) title layout onto a `RenderTexture` driving 07's `Screen` mesh (`build_rover.py:120-125`, `rover_parts.py:1202-1210`) delivers an unforgettable first impression and enables a zero-cut camera pull-back into gameplay when `Continue` or `New Game` is confirmed.
- **`[PROPOSAL]` High-Scale / Handheld Toggle — Variant 1 (`v1`, Full-Monitor CRT):** Because `v1` and `v2` share the exact same UI Toolkit tree and `Painter2D` elements, automatically switches to (or lets the player toggle) `v1`'s full-viewport mode when `TextScale` (`Assets/_Project/Scripts/UI/UISystem.cs:488`) is set to `>= 1.35` (`135%–150%`).

---

## 2. Layout Geometry & Motion Timings

### 2.1 Screen Aspect & Coordinate Budgets

| Parameter | Variant 1 (`v1` Full Monitor) | Variant 2 (`v2` 3D `Screen` Mesh) | Variant 3 (`v3` Split-Frame OS) | Source / Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Target Resolution** | `1920 × 1080` (`16:9`) | `1152 × 768` (`1.50:1`, mesh `0.58 m × 0.39 m = 1.487:1`) | `1920 × 1080` (`16:9`) | `build_rover.py:120-121` (`screen_width: 0.58`, `screen_height: 0.39`) |
| **Left Menu Column (`x`)** | `150 .. 660 px` (`510 px` wide) | `74 .. 469 px` (`395 px` wide) | `162 .. 710 px` (`548 px` inside `125..750 px` pane) | Fits 150% VI text without wrapping |
| **`LUNARION` Wordmark** | `460 × 142 px` at `(120, 175)` | `355 × 110 px` at `(51, 92)` | `430 × 133 px` at `(134, 192)` | `Assets/_Project/UI/Wordmark/LunarionWordmark.svg:1` (`viewBox="0 0 353.32 109.11"`) |
| **Menu Row Pitch (`row_h`)** | `64 px` (`5` rows = `320 px`) | `52 px` (`5` rows = `260 px`) | `58 px` (`5` rows = `290 px`) | Only the 5 real title buttons (`PauseMenu.cs:935-941`) |
| **Moon Center & Radius** | `C = (1310, 980 px)`, `R = 450 px` | `C = (805, 700 px)`, `R = 295 px` | `C = (1285, 1095 px)`, `R = 540 px` | Right-hand schematic viewport |
| **2D Rover Scale (`S`)** | `82 px/m` (wheel `r = 28.7 px`, wheelbase `87.0 px`) | `64 px/m` (wheel `r = 22.4 px`, wheelbase `67.8 px`) | `82 px/m` (wheel `r = 28.7 px`, wheelbase `87.0 px`) | Derived from `build_rover.py:42-46` |

### 2.2 Deterministic Motion Timings (60 fps)
All title animations are driven by unscaled time (`Time.unscaledTime` / `Time.unscaledDeltaTime` as in `Assets/_Project/Scripts/UI/UISystem.cs:508`) so they remain deterministic at 60 fps and independent of physics tick rate:

1. **`[PROPOSAL]` Moon Rotation (`T_moon = 48.0 s/rev`):**
   - Angular velocity: $\omega_{\text{moon}} = \frac{2\pi}{48.0\text{ s}} \approx 0.1309\text{ rad/s}$ ($7.5^\circ/\text{s}$), counter-clockwise so the top crown moves right-to-left beneath 07.
   - Surface craters (`16` elliptical craters) foreshorten radially near the limb by $f_{\text{limb}}(\rho) = \max(0.32,\, 1 - 0.52\rho^2)$ where $\rho = r / R_{\text{moon}}$.
2. **`[PROPOSAL]` Ground-Matched Wheel Spin ($\omega_{\text{wheel}}$):**
   - 07's 4 chunky off-road wheels (`wheel_radius = 0.35 m`, `wheel_rim_radius = 0.185 m`, `wheel_base = ±0.53 m` at `build_rover.py:42-49`) rotate clockwise at exact no-slip ground speed:
     $$\omega_{\text{wheel}} = \omega_{\text{moon}} \cdot \frac{R_{\text{moon}}}{r_{\text{wheel}}} = 0.1309 \cdot \frac{320}{28.7} \approx 1.459\text{ rad/s}\;\;(83.6^\circ/\text{s})$$
3. **`[PROPOSAL]` Suspension Bob, Pitch Rock & Scoop Nod:**
   - Front and rear axles sample a deterministic multi-sine regolith micro-relief profile $h(\theta) = 1.1\sin(9\theta) + 0.65\cos(17\theta + 0.8) + 0.3\sin(29\theta)\text{ px}$.
   - Chassis heave: $y_{\text{bob}}(t) = \frac{1}{2}(h_{\text{rear}} + h_{\text{front}}) + 1.15\sin(2\omega_{\text{wheel}} t)\text{ px}$.
   - Chassis pitch: $\theta_{\text{pitch}}(t) = 0.65\,\text{atan2}(\Delta y_{\text{axle}},\, 2 L_{\text{wb}}) + 0.008\sin(1.5\omega_{\text{wheel}} t)\text{ rad}$.
   - Front dig scoop (`piv_scoop` at `build_rover.py:86-96`): subtle hydraulic nod $\pm 0.03\text{ rad}$ at $1.5\omega_{\text{wheel}}$.
4. **`[PROPOSAL]` Whip Antenna Sway & Morse Call-Sign (`· · · —`):**
   - Rear whip antenna with diamond sonar loop (`piv_antenna = (0.36, 0.91, -0.46)` at `build_rover.py:169-185`) sways with damped harmonic lag: $\theta_{\text{ant}}(t) = 0.09\sin(2\omega_{\text{wheel}} t - 0.7) + 0.035\cos(3\omega_{\text{wheel}} t)\text{ rad}$.
   - Antenna beacon tip pulses 07's Morse call sign `· · · —` on a `4.2 s` cycle (`dots` at `0.20..0.36 s`, `0.56..0.72 s`, `0.92..1.08 s`; `dash` at `1.32..1.88 s`) in `#5FF3FF` cyan.
5. **`[PROPOSAL]` CRT Head Nod & Eye Blink:**
   - Head (`piv_head_local` at `build_rover.py:103-105`) nods gently by $\pm 1.0^\circ$ at $\omega_{\text{wheel}}$; the side-profile phosphor eye blinks for `180 ms` every `6.5 s`.
6. **Earth & Twinkling Stars:**
   - **Earth** (`docs/design/story-v4.md` Decisions 3 & 4): drawn as a dark occluded circle (`r = 30..38 px`, zero night-side city lights) with a thin `#FFD680` / `#FFF3CC` sunlit crescent arc (`-0.78\pi .. +0.16\pi`), faint `#5FF3FF` atmospheric limb scatter tips, and the cyan L4 Ark point (`+54 px, -14 px` offset), drifting across the upper sky at `1.6 px/s`.
   - **Stars**: `12` fixed positions pulsing at `0.13–0.25 Hz` (sine phase offset per star).
7. **Existing Title Reveal, Idle Fade & Curtain Timings (`TitleSettings.cs`):**
   - Preserves `TitleSettings` (`Assets/_Project/Scripts/UI/Tuning/TitleSettings.cs:14-25`): `_reveal` (`2.2 s` in, `2.8 s` out), `_curtain` (`0.9 s` in, `1.4 s` out), and `_idleSeconds = 20.0 s` (`PauseMenu.cs:967-1014`).
   - `[PROPOSAL]` When untouched for `20.0 s`, the left-hand menu text and wordmark softly fade out over `2.8 s` (`_reveal.OutTime`), leaving 07 driving alone on the turning Moon on the CRT screen as a living attract loop until any input wakes the menu back up over `2.2 s` (`_reveal.InTime`).

---

## 3. Unity Implementation Architecture

### 3.1 `Painter2D` Vector Line Art from Model Data (Zero Per-Frame GC)
The project already establishes the pattern of Editor-baked `ScriptableObject` vector data rendered at runtime by allocation-free `StylePainter` subclasses using UI Toolkit's `Painter2D`:
- **Wordmark pipeline:** `WordmarkBuilder` (`Assets/_Project/Scripts/UI/Editor/WordmarkBuilder.cs:19-63`) parses `Assets/_Project/UI/Wordmark/LunarionWordmark.svg` into `WordmarkShape` (`Assets/_Project/Scripts/UI/Drawing/WordmarkShape.cs:12-119`), which `WordmarkPainter` (`Assets/_Project/Scripts/UI/Drawing/WordmarkPainter.cs:12-85`) draws with 3 glow passes (`GlowPasses = 3` at `:15, 53-63`) plus an even-odd fill and round-joined stroke.
- **Rover schematic pipeline (Variant C reference):** `RoverDiagramBuilder` and `RoverDiagramPainter` (`D:\Project\MoonProject-wt\agy-uicrt2\Assets\_Project\Scripts\UI\Drawing\RoverDiagramPainter.cs`) bake 07's 12 `RoverSocket` positions and 4 wheel centers (`Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`) from `RoverModel.prefab` into `RoverDiagramData` and stroke 07's silhouette in `Painter2D`.

`[PROPOSAL]` For the title screen, we add a single `TitleMoonScenePainter : StylePainter` backed by a baked `TitleRoverProfileData : ScriptableObject`:
1. **Editor Baker (`TitleRoverProfileBuilder`):**
   - Runs as a `[MoonBuilder]` step alongside `WordmarkBuilder` (`WordmarkBuilder.cs:19`).
   - Reads the generated `Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx` / `RoverModel.prefab` mesh sub-hierarchy (`Carapace`, `Head`, `Visor`, `CheekLens_L`, `NeckBellows`, `Scoop`, `SolarWing`, `Antenna`, and `Wheel_FL/FR/RL/RR` — matching `tools/art/blender/rover/build_rover.py:37-192`) and extracts normalized side-elevation (+Z forward, +Y up) contour polylines, pivot offsets (`Piv_Neck`, `Piv_Head`, `Piv_Scoop`, `Piv_SolarWing`, `Piv_Antenna`), wheel center offsets (`±0.53 m`), and wheel/rim radii (`0.35 m` / `0.185 m`).
   - Because the strokes are baked directly from `RoverModel.prefab`'s sub-mesh bounds and pivots, **any future proportion change to 07's 3D model automatically updates the 2D title rover on the next build**.
2. **Runtime `TitleMoonScenePainter` (`MeshGenerationContext.painter2D`):**
   - Pre-allocates fixed `Vector2[]` scratch arrays in its constructor (zero heap allocations in `Paint(MeshGenerationContext)`).
   - Evaluates the deterministic transforms for `t = Time.unscaledTime` (`drawEarthAndSky`, `drawMoon`, `drawLandmark`, `drawRover07`) and issues `painter.BeginPath()`, `painter.MoveTo()`, `painter.LineTo()`, `painter.Arc()`, and `painter.Stroke()` calls with `LineCap.Round` and `LineJoin.Round`.
   - Calls `Element.MarkDirtyRepaint()` once per `Tick(float deltaTime)` while the title is open (`PauseMenu.cs:316`).

### 3.2 CRT Look: Shared `R1CrtFace.shader` & UI Material
07's physical screen already uses `Assets/_Project/Shaders/R1/R1CrtFace.shader` (`:1-58`) and `Assets/_Project/Shaders/R1/R1CrtFaceCore.hlsl` (`:1-405`), which implement:
- **Barrel Curvature Warp:** `warpedUv = jitteredUv + cc * (dot(cc, cc) * _Curvature)` with `_Curvature = 0.08` (`R1CrtFace.shader:12`, `R1CrtFaceCore.hlsl:269-271`).
- **Phosphor Persistence Trail:** `_TrailAlpha = 0.28` (`R1CrtFace.shader:16`, `R1CrtFaceCore.hlsl:301-311`).
- **Dual-Radius Exponential Halo:** `_GlowRadius = 0.028` (`wideHalo` + `tightHalo` in `R1CrtFaceCore.hlsl:341-347`) blending `_PhosphorColor` `(1.000, 0.682, 0.220)` (`#FFAE38`), `_PhosphorGlowColor` `(0.784, 0.416, 0.078)` (`#C86A14`), and `_HotCoreColor` `(1.000, 0.953, 0.800)` (`#FFF3CC`).
- **Horizontal Scanlines & Rolling Refresh Band:** `_ScanlineCount = 180.0`, `_ScanlineDepth = 0.16`, `ROLLING_BAND_WIDTH = 0.12`, `ROLLING_BAND_STRENGTH = 0.08` (`R1CrtFace.shader:13-14`, `R1CrtFaceCore.hlsl:22-23, 377-385`).
- **Rounded Bezel SDF Vignette:** `SdfRoundedBox(warpedUv - 0.5, float2(0.485, 0.485), 0.055)` over `_ScreenBaseColor` `#20140A` to `_ScreenEdgeColor` `#120B05` (`R1CrtFace.shader:6-7`, `R1CrtFaceCore.hlsl:388-395`).

`[PROPOSAL]` **Shared Shader Integration (`MoonProject/R1/CrtUiComposite`):**
- Factor the post-processing stage of `R1CrtFaceCore.hlsl` (`:266-398`) into a shared HLSL function `ApplyR1CrtPass(float2 uv, Texture2D contentTex, Texture2D trailTex, CrtParams p)` included by both `R1CrtFace.shader` and a lightweight `R1CrtTitleScreen.shader`.
- **In Variant 2 (`v2`):** The title `PanelSettings` renders into a `1152×768` `RenderTexture` (`_TitleScreenRT`), which is ping-pong blended with a `576×384` persistence buffer (`_TitleTrailRT`, decay `0.76/frame` at 60 fps) and sampled directly by 07's `Screen` MeshRenderer using `R1CrtTitleScreen.shader`. When the player confirms `New Game` or `Continue`, `_TitleScreenFade` crossfades from `_TitleScreenRT` to `R1CrtFaceCore`'s procedural eye SDF (`EvalExpression` in `R1CrtFaceCore.hlsl:207-232`) as the camera pulls back!
- **In Variant 1 (`v1`):** The same `R1CrtTitleScreen.shader` material wraps the full-screen `1920×1080` title panel.

---

## 4. Audio Cues & Radio Integration

### 4.1 Exact Felt-Tick Cursor Cue & UI Audio Map
All title menu interactions route through `UiAudio` (`Assets/_Project/Scripts/Audio/UiAudio.cs:11-161`) and use the approved R1 audio audition cues (`C2 — "Radio bench"`, D major pentatonic `D E F# A B`, `max_ui_lufs = -28.5`):

| Menu Event | Runtime Hook (`PauseMenu.cs` / `UiAudio.cs`) | Audition Cue ID | Loudness & Duration | Timbre & Musical Pitch (R1 Audition) |
| :--- | :--- | :--- | :--- | :--- |
| **Cursor Focus Step** (`↑`/`↓`, D-pad, Stick, Hover) | `PauseMenu.MoveFocus` (`:651-670`) $\rightarrow$ `UiCueKind.FocusMove` (`UiAudio.cs:59, 123-125`) | **`ui_cursor_move`** (Pick A; `AudioCueIds.UiFocus`) | **`-30.5 LUFS`** (`vol 0.42`, eff. `-38.04 dB`), **`75 ms`** | **Felt-tick cursor cue:** tactile rotary wafer-switch detent / damped felt keycap tick with warm paper-cone speaker resonance, pitched at **`A4`** (Variant B at `D5`). |
| **Rapid Focus Streak** | `RepeatSoftener.Step` (`UiAudio.cs:27-35, 124`) | `ui_cursor_move` (softened) | Decays by `0.86×` per step down to `0.55×` floor within `0.35 s` window (`_focusMinInterval = 0.05 s`) | Prevents mechanical chatter when holding `↓` / `↑` across the 5 items. |
| **Confirm Selection** (`Continue`, `New Game`, `Settings`, `Credits`) | `UiCueKind.Confirm` (`UiAudio.cs:62, 127`) | **`ui_confirm`** (Pick A; `AudioCueIds.UiConfirm`) | **`-28.5 LUFS`** (`vol 0.48`, eff. `-34.88 dB`), **`320 ms`** | Bakelite push-button click followed by a warm rising **`D4 → A4`** fifth chime. |
| **Cancel / Back** (from `New Game` confirm, `Settings`, `Credits`) | `PauseMenu.Back` (`:296-314`) $\rightarrow$ `UiCueKind.Back` (`UiAudio.cs:65, 130`) | **`ui_cancel`** (Pick A; `AudioCueIds.UiCancel`) | **`-29.0 LUFS`** (`vol 0.46`, eff. `-35.74 dB`), **`300 ms`** | Spring button release with a gentle falling **`A4 → D4`** interval. |
| **Title CRT Wake / First Boot** | `PauseMenu.OpenTitle` (`:269-288`) | **`face_boot`** (`AudioCueIds.FaceBoot`) | **`-20.0 LUFS`** (`vol 0.68`, eff. `-23.35 dB`), **`1.28 s`** | Heavy bakelite toggle switch + **`D2–A2–D3`** CRT filament warm-up hum + **`D4–A4`** scanline sweep. |

### 4.2 `TitleRadio` ("a radio left on")
The subtitle `"a radio left on"` / `"một chiếc radio vẫn bật"` (`en.json:1470`, `vi.json:1470`) is backed directly by `TitleRadio` (`Assets/_Project/Scripts/Audio/TitleRadio.cs:8-51`) and `RadioStation` (`Assets/_Project/Scripts/Audio/RadioStation.cs:12-13, 56`):
- **Title over an existing save (`HasSave == true`):** `TitleRadio.ShowOverSave(soft)` (`TitleRadio.cs:20-25`) plays the player's saved station (`Lumen After Dark` or `Tape Deck`) softly beneath the menu.
- **Selecting `Continue`:** `TitleRadio.StartSession()` (`TitleRadio.cs:28-32`) eases the radio multiplier from `soft` up to `1.0` as the camera pulls back into gameplay.
- **Confirming `New Game` over a save:** `TitleRadio.PutAway()` (`TitleRadio.cs:35-44`) eases the station down to `0.0` (silence/static) as the camera transitions into a fresh session (`RadioGate`, `RadioStation.cs:25-27`).

---

## 5. Gamepad, Keyboard & Pointer Focus Rules

The title screen reuses `PauseMenu` (`Assets/_Project/Scripts/UI/Pause/PauseMenu.cs:269-288, 931-942`) and `PausePages` (`Assets/_Project/Scripts/UI/Pause/PausePages.cs:10-116`), preserving all existing focus guarantees:

1. **Strictly the 5 Real Title Buttons (`PauseMenu.cs:935-941`):**
   - `Continue` (`_resumeButton`, visible only when `_pages.HasSave` is `true` at `:938`),
   - `New Game` (`_newGameButton`, visible only on title at `:939`),
   - `Settings` (`_settingsButton`),
   - `Credits` (`_creditsButton`),
   - `Quit` (`_quitButton`).
2. **Initial & Default Focus (`PausePages.cs:24-40, 51-56`):**
   - **First boot (`chooseLanguage == true`):** Opens on `PausePage.Language` (`PausePages.cs:55`), offering `"English"` / `"Tiếng Việt"` (`PauseMenu.cs:1049-1078`). Once chosen (`PausePages.LanguageChosen()` at `:59-68`), advances to `PausePage.Main`.
   - **Main Title with a save (`HasSave == true`):** Focus rests on **`Continue`** (`PauseFocus.Resume` at `PausePages.cs:37`).
   - **Main Title without a save (`HasSave == false`):** `Continue` is hidden (`DisplayStyle.None`) and focus rests on **`New Game`** (`PauseFocus.NewGame` at `PausePages.cs:37`).
3. **Visual Focus State on CRT:**
   - The focused row renders a blinking `>` terminal prompt (`1.0 Hz` 50% duty cycle or steady during movement), `#5FF3FF` cyan label (`ThemeTokens.TechGlow`), `#0A1A20` dark cyan row highlight (`ThemeTokens.CrtCyanFill`), and a `6 px` `#5FF3FF` left bar.
   - Unfocused rows indent by two spaces (`"  "`) in warm amber `#FFC25C` (`Quit` in `#E8AE52`).
4. **Safe-Answer Confirmation for `New Game` (`PausePages.cs:32-33, 71-115`):**
   - When `HasSave == true`, activating `New Game` opens `PausePage.NewGame` (`"Start a new game?"` / `"Your current save is kept, not deleted."` at `en.json:1558-1564`) with focus **always defaulting to the safe answer** `PauseFocus.NewGameKeep` (`"Keep playing"` / `"Chơi tiếp"` at `PausePages.cs:33`).
   - Pressing `Escape` / `B` / `Cancel` calls `PausePages.Back(out focus)` (`PausePages.cs:86-103`, `UISystem.cs:520-523`), returning to `PausePage.Main` with focus restored to `New Game`.
   - On the title screen, `Quit` does not prompt a confirmation question (`PauseMenu.cs:784-789`); it saves any modified settings and draws the dark curtain.
5. **`[PROPOSAL]` Pointer Raycast in Variant 2 (`v2`):**
   - While keyboard (`W`/`S`, `↑`/`↓`, `Enter`/`Space`) and gamepad (`D-Pad`, `Left Stick`, `South/A` confirm, `East/B` cancel) work identically in `v1`, `v2`, and `v3`, mouse/pointer input in `v2` translates screen-space pointer coordinates through a `MeshCollider` on 07's `Screen` mesh (`RaycastHit.textureCoord` $\in [0,1]^2$) into the `1152×768` UI panel coordinates so hovering and clicking inside 07's 3D bezel is pixel-accurate.

---

## 6. Vietnamese Text Widths & Layout Clearance

All title and confirmation strings from `Assets/_Project/Data/Localization/en.json` (`:1470-1572`) and `Assets/_Project/Data/Localization/vi.json` (`:1470-1572`) were measured using the project's actual font files (`IBMPlexMono-SemiBold.ttf` `30 px`, `IBMPlexMono-Medium.ttf` `28 px`, and `BeVietnamPro-Regular.ttf` `20 px`):

| Localization Key (`en.json` / `vi.json`) | English (`en.json`) | EN Width | Vietnamese (`vi.json`) | VI Width | VI / EN Ratio | Width at 150% Scale | Column Budget (`v1` / `v2` / `v3`) |
| :--- | :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| `ui.title.continue` (`:1474`) | `Continue` | `144 px` | `Tiếp tục` | `144 px` | **100.0%** | `216 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.new_game` (`:1478`) | `New Game` | `136 px` | `Chơi mới` | `136 px` | **100.0%** | `204 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.settings` (`:1482`) | `Settings` | `136 px` | `Cài đặt` | `119 px` | **87.5%** | `179 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.credits` (`:1490`) | `Credits` | `119 px` | `Ghi công` | `136 px` | **114.3%** | `204 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.quit` (`:1486`) | `Quit` | `68 px` | `Thoát` | `85 px` | **125.0%** | `128 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.subtitle` (`:1470`) | `a radio left on` | `138 px` | `một chiếc radio vẫn bật` | `236 px` | **171.0%** | `354 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.new_game_question` (`:1558`) | `Start a new game?` | `306 px` | `Bắt đầu chơi mới?` | `306 px` | **100.0%** | `459 px` | Wraps cleanly at `>130%` in `v2` |
| `ui.title.new_game_aside` (`:1562`) | `Your current save is kept, not deleted.` | `374 px` | `Bản lưu hiện tại vẫn được giữ lại.` | `312 px` | **83.4%** | `468 px` | Multi-line wrap enabled |
| `ui.title.new_game_keep` (`:1566`) | `Keep playing` | `216 px` | `Chơi tiếp` | `162 px` | **75.0%** | `243 px` | `510 / 395 / 548 px` (Fits) |
| `ui.title.new_game_confirm` (`:1570`) | `Start new` | `153 px` | `Chơi mới` | `136 px` | **88.9%** | `204 px` | `510 / 395 / 548 px` (Fits) |

**Key Findings:**
1. Because `IBMPlexMono` is a fixed-advance monospace font (`18 px/char` at `30 px` SemiBold, `17 px/char` at `28 px` Medium), 4 of the 5 Vietnamese menu buttons (`Tiếp tục`, `Chơi mới`, `Cài đặt`, `Ghi công`) are **7 to 8 characters**, matching or beating English (`119–144 px`).
2. Even with the `"> "` cursor prefix (`+36 px`) and the right-aligned `"BẢN LƯU · 02:14"` badge (`+150 px`), the focused `Tiếp tục` row uses `330 px`, leaving `65 px` of horizontal breathing room in `v2` (`395 px` column) and `180–218 px` in `v1`/`v3`.
3. The largest Vietnamese expansion is `ui.title.subtitle` (`"một chiếc radio vẫn bật"`, `236 px` vs `138 px` EN = `171.0%`), which still fits on a single line at 150% text scale (`354 px < 395 px`).
4. Both `IBMPlexMono` and `BeVietnamPro` include complete precomposed Vietnamese glyphs (`ế`, `ụ`, `ơ`, `ớ`, `à`, `ặ`, `ô`, `á`, `ộ`, `ẫ`, `ậ`, `ư`, `ữ`) with vertical line-height set to `1.35×` so stacked diacritics (`ế`, `ẫ`, `ớ`) never clip against the top of the focus box.

---

## 7. `[PROPOSAL]` Visible Progression on the Turning Moon

To make the title screen reflect the player's journey whenever they boot the game over an existing save (`HasSave == true`), `TitleMoonScenePainter` reads a lightweight snapshot of the save's world progression (`IRelayStatus`, `IBuildPads`, and `RadioGate` state) and updates the landmarks around the Moon sphere:

| Moon Rim Landmark | Polar Angle $\theta_0$ | Fresh Game / Unrepaired State (`HasSave == false` or Early Save) | Repaired / Built State in Save (`[PROPOSAL]`) |
| :--- | :---: | :--- | :--- |
| **1. Habitat / Cargo Lander** | $\theta_0 = -90^\circ + 4.5^\circ$ | Dark cabin viewport; boarding ramp retracted; comm dish static. | Warm `#FFF3CC` lit cabin window; deployed boarding ramp; comm dish gently sweeping (`±12°`) with a soft beacon glint. |
| **2. Base Radio Tower** | $\theta_0 = -90^\circ + 34.5^\circ$ | **Broken Radio Tower:** upper lattice mast kinked `26°` to the right, slack snapped guy-wire, intermittent fault spark. | **Straightened & Lit Radio Mast:** upright lattice tower, taut guy-wires, and expanding concentric `#5FF3FF` cyan carrier rings pulsing in time with `TitleRadio`. |
| **3. Solar Array** | $\theta_0 = -90^\circ + 63.0^\circ$ | Single dusty tilted panel wing (or bare tripod mount if unbuilt). | Full twin-wing tracking solar array tilted toward the sunlit limb with a `#FFD680` charge-flow tick down the mast. |
| **4. Build-Pad Structures** *(unlocked as player builds)* | $\theta_0 = +40^\circ .. +160^\circ$ | Bare lunar regolith and small survey tripod markers (`kind: "marker"`). | Each completed build pad adds its corresponding miniature line-art silhouette to the Moon's rim: **Field Charge Pylons** (twin capacitor coils), **Fabricator** (gantry frame), **Rover Bay Wing** (arched roof shelter), and the **Peak Relay Dish** (parabolic dish aimed toward Earth's L4 Ark point). |
| **5. 07's Own Kit Upgrades** | On 2D 07 (`MOON_CX, MOON_CY - MOON_R`) | Base 07 silhouette (or missing `SolarWing` / `TapeDeck` if not yet fitted). | Reflects 07's saved `RoverSocket` loadout: roof `SolarWing` deployed, side `Kit_TapeDeck` reel spinning when a cassette is loaded, and 8-segment rear battery bar matching saved charge. |
