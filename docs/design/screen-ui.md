# 07 · Diegetic CRT Face Inventory (`SCREEN-UI`)

> **Status:** `[PROPOSAL]` — Concept & Technical Design Note (updated for **Round 2: Icon-First CRT Inventory with Almost No Text**) for showing 07's inventory (`Tab`) directly on 07's own CRT screen face (`RoverModel/Neck/NeckLift/Head/Screen`).
> **Visual Artifacts:**
> - **Round 2 (Current — Icon-First):** Flat 1.49:1 mockups (`Logs/mockups/r2_A_{8,16,24,drag}_vi.png`, `Logs/mockups/r2_A_24_en.png`, `Logs/mockups/r2_B_{8,16,24,drag}_vi.png`, `Logs/mockups/r2_B_24_en.png`), 3D on-face renders (`Logs/mockups/r2_onface_A.png`, `Logs/mockups/r2_onface_B.png`), and comparison gallery (`Logs/mockups/r2_index.html`).
> - **Round 1 (Earlier Exploration & Transition Storyboard):** Flat layouts (`Logs/mockups/screen_v{1,2,3}_{en,vi}.png`), 3D renders (`Logs/mockups/onface_v{1,2,3}_vi.png`, `Logs/mockups/onface_wide_night.png`), 6-beat transition storyboard (`Logs/mockups/storyboard.png`), and Round 1 gallery (`Logs/mockups/index.html`).

---

## 1. Round 2 Design Rules & Variant Recommendation (`Variant A > Variant B`)

### 1.1 Existing Screen Mesh & Shader Baseline
- **Physical Screen Geometry:** On 07's procedural hierarchy (`RoverModel -> Neck -> NeckLift -> Head -> Screen`), the `Screen` mesh is built by `build_crt_screen_bmesh` (`tools/art/blender/rover/rover_parts.py:1032-1212`) with `screen_width = 0.58 m`, `screen_height = 0.39 m` (aspect ratio `0.58 / 0.39 = 1.487:1`), `screen_bulge_z = 0.018 m`, `screen_corner_round = 0.075`, and an `8 × 6` curved quad grid (`tools/art/blender/rover/build_rover.py:114-121`).
- **Head & Visor Framing:** The `Neck` pivot sits at `(0.0, 0.92, 0.13)` in Unity rover-local space, `Head` sits at `(0.0, 1.18, 0.13)`, and `Screen` sits at local `(0.0, 0.08, 0.295)` (world `(0.0, 1.26, +0.425)` at rest), framed by the cream/orange `Visor` hood (`0.77 m × 0.51 m`, `tools/art/blender/rover/build_rover.py:102-113`) and the right-hand machined brass emitter pod (`Eye` at `(+0.38, 0.08, 0.08)`, `tools/art/blender/rover/build_rover.py:107`).
- **UV Orientation:** On `Screen`, `u_norm = 0` (`-X` in head space) is on the viewer's right when looking at 07's face from `+Z`, so `R1CrtFaceCore.hlsl` mirrors X via `float2 rawUv = float2(1.0 - input.uv.x, input.uv.y)` (`Assets/_Project/Shaders/R1/R1CrtFaceCore.hlsl:267`).
- **Existing Cargo & Equipment UI:** Currently, pressing `Tab` calls `UISystem.OpenCargo()` (`Assets/_Project/Scripts/UI/UISystem.cs:532-535, 763-771`), which opens the 2D overlay `CargoScreen` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:31, 98, 267-356`) and `EquipmentPage` (`Assets/_Project/Scripts/UI/Cargo/EquipmentPage.cs:31, 91, 232-303`) showing `RoverSocket` positions (`Assets/_Project/Scripts/Core/RoverSocket.cs:7-44`) over a separate off-screen `RoverPortrait` render texture (`Assets/_Project/Scripts/Rover/Portrait/RoverPortrait.cs:50-54`).

### 1.2 Round 2 Core Rules (`[PROPOSAL]`)
Round 1's layouts (`v1`, `v2`, `v3`) worked on 07's 3D head but carried too much text and had no clean room for the hold to grow from `8` to `24` slots. Round 2 replaces text-heavy lists with a **Subnautica-inspired, icon-first CRT layout** governed by five strict rules:

1. **Strict Text Budget:**
   - At most **two small section headers**: `KHOANG HÀNG` and `TRANG BỊ` in Vietnamese (`INVENTORY` and `EQUIPPED` in English).
   - **One tooltip** when an item cell or socket bubble is hovered/focused: **Name + 1 short line** (`<= 52 chars`).
   - **Zero** file paths, slot codes (`S01..S10`, `01..24`), key hints (`[LMB]`, `[Tab]`), status bars, flow diagrams, or *"where it goes"* routing boxes.
   - **Zero English in the Vietnamese version** (all text comes from `Assets/_Project/Data/Localization/vi.json`).
   - **Single bottom toast line** only when an event occurs (e.g. `1 phần để lại tại Kestrel-3: hết chỗ chứa` / `1 left behind at Kestrel-3: no room aboard`).
2. **Icons Do the Talking:**
   - Hold items are large, unmistakable vector icons centered inside square cells with a compact count badge (`2..99`) at the bottom-right corner only when `stack > 1` (singletons and kits show no badge).
   - **Empty socket bubbles** display a **faint outline icon** (`#8E6026`, `1.5 px` stroke, no fill) of the specific kit that fits that socket so players immediately know what belongs where without text labels.
   - **Installed socket bubbles** display the **lit kit icon** (`#FFD680` / `#FFB547` stroke with warm phosphor fill and double outer ring).
3. **07 in the Centre of the Right Half (`drawRover07` Line-Art Style):**
   - 07 is drawn in the centre of the right panel using the exact multi-weight amber vector line-art language from the title screen (`drawRover07` and `drawWheel` in `D:\Project\MoonProject-wt\agy-title\Logs\title\anim.html`), surrounded by **10 round socket bubbles** (`r = 30–31 px`) with a thin radial leader line (`1.5 px`) from each bubble's perimeter to its physical mount point on 07's body.
   - When a kit is installed (e.g. `CargoRack` on `Hold` or `SideBins` on `Panniers`), both its socket bubble AND its corresponding hardware silhouette on 07's line-art body light up.
4. **Hold Growth (`8 → 16 → 24`) Without Layout Reflow:**
   - The hold grid on the left half is laid out at its **maximum capacity of 24 cells (`4 columns × 6 rows`) from the start of the game**:
     - **8 cells (`Base Chassis`):** Rows 1–2 unlocked (`8` cells); Rows 3–6 locked (`16` cells).
     - **16 cells (`+ Cargo Rack` installed in `Hold`):** Rows 1–4 unlocked (`16` cells); Rows 5–6 locked (`8` cells).
     - **24 cells (`+ Side Panniers` installed in `Panniers`):** All Rows 1–6 unlocked (`24` cells).
   - **Unlocked cells** have crisp solid borders (`#FFB547` when occupied, `#8E6026` when empty) and dark warm well fills (`#180F08`).
   - **Locked cells** are drawn as **faint dotted square outlines** (`#8E6026` at `45%` alpha) with a **tiny padlock icon** centered inside.
5. **Screen & Two-Colour CRT Palette:**
   - Rendered at `1152×768` (`1.5:1`) with phosphor bloom, subtle scanlines, and bezel edge vignette.
   - **Two colours only:**
     - **Warm Amber (Primary):** `#FFF3CC` (hot filament), `#FFD680` (bright stroke/header), `#FFB547` (main line), `#CC9644` (secondary text), `#8E6026` (muted/faint/locked) over `#20140A` / `#120B05` phosphor ground.
     - **Cyan (Strictly Focus / Selection / Drag Target):** `#5FF3FF` (with `#DCFFFF` core highlight and `#2EA8B8` leader/trail). No third accent colour.

### 1.3 Round 2 Variant Comparison & Recommendation (`[PROPOSAL]`)
We built and tested two complete Round 2 variants across all 5 states (`8`, `16`, `24` in `vi`, `24` in `en`, and `drag` in `vi`) plus `1920×1080` 3D on-face renders (`Logs/mockups/r2_index.html`):

| Dimension | **Variant A (★ Recommended Pick)** | **Variant B (Alternative)** |
|---|---|---|
| **Files** | `r2_A_{8,16,24,drag}_vi.png`, `r2_A_24_en.png`, `r2_onface_A.png` | `r2_B_{8,16,24,drag}_vi.png`, `r2_B_24_en.png`, `r2_onface_B.png` |
| **Left Hold Grid (`4 × 6`)** | Unified Subnautica-style matrix of **`86 × 86 px`** square cells (`gx0 = 48, gy0 = 58`, `10 px` col gap, `7 px` row gap). | Grouped into three `4 × 2` hardware expansion tray brackets of **`78 × 78 px`** square cells (`gx0 = 54, gy0 = 60`, `12 px` inter-tray gap). |
| **Right 07 Line Art** | **Side-3/4 profile** (direct port of `drawRover07` in `anim.html`): all 3 near-side wheels, rocker-bogie struts, scoop, neck bellows, CRT head, cheek emitter, solar wing, tape deck, and rear rack are cleanly separated horizontally. | **Front-3/4 perspective**: 07's head angled toward the viewer showing its two glowing pill eyes on the CRT face, right-cheek emitter pod, and foreshortened chassis/wheels. |
| **10 Socket Bubbles** | **360° elliptical orbit** (`r = 31 px`) around 07 with short, straight, non-crossing radial leader lines to each mount point. | **Two symmetrical curved crescent columns** (`r = 30 px`, 5 left for rear/cargo/power, 5 right for head/front/mobility) with radial leader lines. |
| **Tooltip Placement** | **Contextual zero-occlusion card:** sits cleanly below Row 6 (`48, 625, 374×58`) with an upward cyan pointer aligned to the focused column when inspecting hold cells; shifts beneath the target socket bubble (`632, 642, 434×60`) when dragging/hovering equipment on the right half. | **Fixed bottom-left dock** (`46, 616, 422×62`) permanently anchored below Tray 3 regardless of whether focus is on the left grid or right sockets. |

**Why Variant A is our Recommended Pick:**
1. **22% Larger Hold Cells (`86×86 px` vs `78×78 px`):** Removing the inter-tray bracket chrome gives every icon and stack badge 22% more screen area while keeping all 24 cells 100% unobstructed by the tooltip.
2. **Superior Anatomical Separation on `drawRover07`:** In Variant A's side-3/4 profile, 07's 10 mount points (`Wing`, `Cutter`, `Tether`, `Lamp`, `Flanks`, `Belly`, `Panniers`, `Hold`, `Deck`, `Back`) are spread across the full horizontal length of the chassis, making every radial leader line shorter and more intuitive than on a foreshortened front-3/4 body.
3. **Contextual Tooltip Proximity During Drag-and-Drop:** When dragging a kit (`Bật nhảy` / `Hover-Jump`) onto a socket bubble on the right half (`r2_A_drag_vi.png`), placing the tooltip directly beneath the target socket bubble keeps the player's eyes focused on the socket they are fitting rather than forcing them to look back to the bottom-left corner.

---

## 2. Complete Icon Manifest (`[PROPOSAL]`)

All icons are designed as crisp single-weight/dual-weight vector glyphs (`2.2 px` primary stroke + `1.5 px` detail stroke at `1152×768`, rendered at `2×` supersampling in `Logs/generate_r2_mockups.py`) that read clearly at `32–48 px` diameter inside `86×86 px` hold cells and `62 px` socket bubbles.

### 2.1 Hold Item Icons (`18` Icons — Salvage, Minerals, Samples & Keepsakes)
Drawn in `"lit"` amber mode (`#FFD680` primary stroke, `#FFB547` secondary stroke, `rgba(255, 181, 71, 0.14)` translucent fill) inside occupied hold cells, switching to `"cyan"` mode (`#DCFFFF` / `#5FF3FF`) when the cell is focused:

| # | Icon ID (`generate_r2_mockups.py`) | Game Item / Resource (`Localization/{vi,en}.json`) | Visual Silhouette | Stack Badge? |
|---|---|---|---|---|
| 1 | `metal` | **Tấm kim loại** / **Metal Plates** (`res.metal`) | Two overlapping isometric sheared hull plates with three corner rivet dots. | Yes (`2..99`) |
| 2 | `wiring` | **Cuộn dây đồng** / **Copper Wiring** (`res.wiring`) | Concentric wound cable spool with central hub and two loose stripped wire leads. | Yes (`2..99`) |
| 3 | `optics` | **Thấu kính** / **Optics** (`res.optics`) | Precision diamond lens housing enclosing a ground circular optic with specular arc and alignment ticks. | Yes (`2..99`) |
| 4 | `beads` | **Hạt thủy tinh cam** / **Orange Glass Beads** (`res.beads`) | Cluster of four overlapping volcanic lunar glass spherules with crescent specular highlights. | Yes (`2..99`) |
| 5 | `ilmenite` | **Quặng Ilmenite** / **Ilmenite Ore** (`res.ilmenite`) | Faceted 6-sided angular basaltic mineral polyhedron with internal crystal cleavage lines. | Yes (`2..99`) |
| 6 | `glass` | **Kính vỡ** / **Impact Glass** (`res.glass`) | Rounded rectangular pane of thick viewport glass with diagonal twin reflection streaks. | Yes (`2..99`) |
| 7 | `sealant` | **Keo bịt kín** / **Vacuum Sealant** (`res.sealant`) | Trapezoidal industrial patch strip / nozzle cartridge with three diagonal curing ribs. | Yes (`2..99`) |
| 8 | `ice` | **Đá nước vùng tối** / **Shadow Water Ice** (`res.ice`) | Hexagonal frost crystal prism with 6-fold internal snowflake axes. | Yes (`2..99`) |
| 9 | `he3` | **Bình Helium-3** / **Helium-3 Canister** (`res.he3`) | Pressurized cylindrical cryo-flask with top valve neck, shoulder collar, and central diamond hazard emblem. | Yes (`2..99`) |
| 10 | `quartz` | **Tinh thể thạch anh** / **Piezo Quartz** (`res.quartz`) | Three vertical hexagonal crystal spires (tall center, medium flanks) rising from a basal bar. | Yes (`2..99`) |
| 11 | `circuit` | **Mạch điều khiển** / **Logic Board** (`res.circuit`) | Square microprocessor package with inner die frame and 12 radial contact pins (3 per side). | Yes (`2..99`) |
| 12 | `gyro` | **Con quay hồi chuyển** / **Gyro Ring** (`res.gyro`) | Gimballed outer ring with 4 cardinal pivot ticks and an inscribed triangular rotor core. | Yes (`2..99`) |
| 13 | `coil_spool` | **Cuộn cảm từ** / **Inductor Spool** (`res.coil`) | Vertical flanged bobbin wound with 5 horizontal copper turns. | No (`×1` part) |
| 14 | `water_jar` | **Bình nước lọc** / **Filter Flask** (`item.water_jar`) | Ribbed cylindrical canteen with screw cap and three horizontal fluid-level gauge bars. | No (`×1` keepsake) |
| 15 | `cell_pack` | **Khối pin dự phòng** / **Spare Cell Pack** (`item.cell_pack`) | Twin vertical battery cells strapped side-by-side with top positive terminals. | No (`×1` part) |
| 16 | `beacon_part` | **Đài mốc tín hiệu** / **Survey Beacon** (`item.beacon`) | Rectangular transmitter base with vertical whip antenna, tip orb, and radiating signal arc. | No (`×1` deployable) |
| 17 | `radio_tube` | **Bóng đèn điện tử của Bell** / **Bell's Vacuum Tube** (`item.bells_tube`) | Classic glass vacuum envelope with internal filament chevron, base collar, and 3 bottom contact pins. | No (`×1` keepsake) |
| 18 | `tape_reel` | **Cuộn băng từ** / **Magnetic Tape Reel** (`item.cassette`) | Circular open-reel audio spool with central hub and 6 radial flange cutouts. | No (`×1` keepsake) |

### 2.2 Equipment Kit & Empty-Socket Outline Icons (`10` Active 0.5 Sockets + `2` Planned 0.6 Sockets = `12` Kit Icons)
Every `RoverSocket` (`Assets/_Project/Scripts/Core/RoverSocket.cs:7-44`, `EquipmentTable.cs:35-150`) uses **one shared vector icon definition** rendered in three states:
- **Empty Socket Bubble (`mode="faint"`):** Single thin outer circle (`1.5 px` `#8E6026`) + faint outline of the kit icon inside (`1.5 px` `#8E6026`, no fill) so an empty socket clearly communicates what fits.
- **Installed Socket Bubble (`mode="lit"`):** Bright outer circle (`2.4 px` `#FFD680`) + inner ring (`1.2 px` `#FFB547`) + lit kit icon (`2.2 px` `#FFD680` stroke + warm amber fill). Also used when an unequipped kit sits in a hold cell.
- **Focused / Drag-Target Socket Bubble (`mode="cyan"`):** Bright cyan outer halo (`#DCFFFF` / `#5FF3FF`) + cyan leader line to 07 + cyan kit icon.

| Socket Enum (`RoverSocket.cs`) | Kit ID (`EquipmentTable.cs`) | Localization (`vi` / `en`) | Icon ID | Visual Silhouette (Lit & Faint-Outline States) | Mount Anchor on 07 |
|---|---|---|---|---|---|
| `0: Cutter` | `FocusLens` | **Thấu kính hội tụ** / **Focus Lens** | `kit_focus_lens` | Parabolic emitter dish on left projecting a focused triangular beam to a hot focal point on right. | Right cheek emitter pod on `Head` |
| `1: Tether` | `TetherRing` | **Nâng (Vòng dây kéo)** / **Lift (Tether Ring)** | `kit_tether_ring` | Circular brass anchor ring on left paying out a horizontal tension cable to a curved grapple hook on right. | Cheek tether ring on `Head` |
| `2: Lamp` | `LampBar` | **Đèn pha ấm** / **Warm Headlamp** | `kit_headlamp` | D-shaped headlamp reflector housing on left with three diverging light rays on right. | Front nose headlamp bar |
| `3: Wing` | `SolarCell` | **Ô pin mặt trời của Tilly** / **Tilly's Solar Cell** | `kit_solar_wing` | Tilted rectangular photovoltaic wing panel subdivided into a `3 × 2` solar cell grid. | Top-rear deck solar wing mast |
| `4: Back` | `BatterySpine` | **Bộ pin II** / **Battery Pack II** | `kit_battery_spine` | Heavy rectangular spine battery module with two top terminal lugs and three vertical charge cells. | Upper chassis spine behind neck |
| `5: Deck` | `TapeDeck` | **Đầu băng cũ của Ro** / **Ro's Tape Deck** | `kit_tape_deck` | Compact cassette deck faceplate with twin sprocketed tape reels and lower tape-head window. | Rear deck bay under antenna |
| `6: Hold` | `CargoRack` | **Giỏ chở đồ** / **Cargo Rack** | `kit_cargo_rack` | Flared trapezoidal wireframe rear basket with horizontal retaining rail and three vertical ribs (unlocks Rows 3–4: `+8` cells). | Rear chassis cargo rack |
| `7: Panniers` | `SideBins` | **Thùng hông** / **Side Panniers** | `kit_side_panniers` | Pair of saddlebag storage pods linked by a central mounting crossbar (unlocks Rows 5–6: `+8` cells). | Lower flank between wheels |
| `8: Flanks` | `CapacitorDrums` | **Bộ tăng tốc** / **Boost** | `kit_boost` | Twin stacked horizontal cylindrical capacitor drums with diagonal energy slash marks. | Forward flank above front wheel |
| `9: Belly` | `HoverCoils` | **Bật nhảy** / **Hover-Jump** | `kit_hover_jump` | Upward-pointing thrust chevron above a compressed vertical zig-zag induction spring and base plate. | Underbody belly between bogies |
| `10: Wheels` *(0.6)* | `MagneticTreads` | **Bánh từ tính** / **Magnetic Treads** | `kit_mag_treads` *(0.6)* | Spoked rover wheel rim surrounded by external cleated magnetic grip arcs. | Lower wheel bogie hub |
| `11: Antenna` *(0.6)* | `WideSonar` | **Sonar dải rộng** / **Wide Sonar** | `kit_wide_sonar` *(0.6)* | Vertical mast orb emitting three concentric wide-angle sonar wavefront arcs. | Rear antenna mast tip |

### 2.3 System & UI Chrome Glyphs (`3` Glyphs)
1. **Locked Cell Padlock (`draw_tiny_lock`):** `17 × 19 px` miniature padlock (`#8E6026`, `1.5 px` stroke, rounded body + U-shackle + central keyhole dot) centered inside each locked hold cell alongside its `8 px`-dash dotted border.
2. **Cyan Focus Corner Brackets:** Four `11 px` L-shaped corner ticks (`#DCFFFF`, `2.6 px` stroke) framing the currently focused hold cell (`is_focused=True`).
3. **Diegetic CRT Cursor Arrow (`draw_cursor_arrow`):** Sharp geometric pointer triangle (`#FFF3CC` fill, `#5FF3FF` outline) shown during mouse hover / drag operations.

---

## 3. Camera Framing, Easing & 6-Beat Timing (`[PROPOSAL]`)

### 3.1 Existing Camera & Body-Language Hooks
- `RoverCameraRig` drives a `CinemachineCamera` with `CinemachineOrbitalFollow` and `CinemachineRotationComposer` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:69-74, 259-289`), and already blends into close-up world-space `BayShot` poses (`focus` + `eye`) using `BlendShot(BayShot from, BayShot to, float t)` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:634-647`).
- `RoverBodyLanguage` drives procedural `Neck` yaw (`_neckYaw`) and `Head` pitch (`_headPitch` / `_headLiftPitch`) (`Assets/_Project/Scripts/Rover/Character/RoverBodyLanguage.cs:54-83`).
- `R1CrtFaceCore.hlsl` already implements a horizontal CRT collapse/open line at `uv.y = 0.5` (`BOOT_LINE_HALF_HEIGHT = 0.0055`) controlled by `_FaceCrtState.x` (`bootProgress`) and `_FacePhases.zw` (`bootLineEnd = 0.45`, `bootOpenSpan = 0.30`) (`Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs:31-32`, `Assets/_Project/Shaders/R1/R1CrtFaceCore.hlsl:252-289`).

### 3.2 Target In-Game Framing (`[PROPOSAL]`)
When `Tab` is open and settled (`Logs/mockups/r2_onface_A.png`, `Logs/mockups/r2_onface_B.png`):
- **07's Head Pose:** `Neck` yaws to `0°` (or toward the camera's approach bearing when obstructed; see §4) and `Head` pitches up **`+10°`** (`Head` local X rotation `-10°` in Unity / `+10°` in Blender), lifting the `Screen` center to rover-local `(0.00, 1.310, +0.407)` with surface normal `(0.00, +0.174, +0.985)`.
- **Camera Eye & Look Target:** Camera eye sits **`1.08 m`** along the lifted screen normal at rover-local **`(0.00, 1.498, +1.471)`** (in front of the screen and slightly above), aiming at `(0.00, 1.305, +0.407)` with vertical FOV **`26.8°`** (`42 mm` full-frame equivalent).
- **Screen Coverage:** At `1920×1080`, the `0.58 m × 0.39 m` `Screen` fills **`~75%` of the frame height (`~800 px` tall)** and `~62%` of the frame width (`~1190 px` wide). 07's `Visor` bezel (`0.77 m × 0.51 m`), brass `Eye` emitter pod, and neck collar frame the display, while a shallow Depth of Field (`focusDistance = 1.08 m`, `f/1.4`) blurs the surrounding lunar horizon.

### 3.3 6-Beat Transition Choreography (`[PROPOSAL]`)
All motion uses calm, zero-overshoot slow-in / slow-out easing (`SmoothStep(t) = 3t² - 2t³` and `EaseOutCubic(t) = 1 - (1 - t)³`), illustrated in `Logs/mockups/storyboard.png`:

| Beat | Window (s) | Duration | Easing | Visual & Shader Action |
|---|---|---|---|---|
| **1. Chase camera** | `t = 0.00 s` | `0.00 s` | Steady orbital follow (`0.6 s` damping) | Player presses `Tab`; `UISystem.OpenCargo()` locks drive input (`Assets/_Project/Scripts/UI/UISystem.cs:767`) while 07's calm amber pill eyes remain visible. |
| **2. 07 stops & lifts head** | `0.00 – 0.22 s` | `0.22 s` | `SmoothStep(t)` (`3t² - 2t³`) | 07's wheel velocity eases to zero; `Neck` yaws toward the camera arc and `Head` lifts `+10°` to greet the player. |
| **3. Blink & collapse to CRT line** | `0.18 – 0.34 s` | `0.16 s` | `SmoothStep(t)` vertical pinch | Mid-arc channel switch: `FaceDisplay` drives `bootProgress` down to `0.42` (`R1CrtFaceCore.hlsl:280-285`), pinching 07's eyes into a white-hot horizontal CRT scanline (`v = 0.50 ± 0.0055`). |
| **4. Camera arrives** | `0.00 – 0.55 s` | `0.55 s` | `SmoothStep(t)` (`3t² - 2t³`) | Camera orbits smoothly around 07's clear flank via a cylindrical `BlendShot` arc (never passing through 07's chassis) and settles `1.08 m` in front of `Screen`; the bright CRT carrier line holds for `40 ms`. |
| **5. Line expands into inventory** | `0.50 – 0.72 s` | `0.22 s` | `EaseOutCubic(t)` (`1 - (1 - t)³`) | The shader switches its source texture to `_ScreenUiTex` and expands `bootProgress` from `0.45 -> 1.00`, opening the horizontal line vertically into the icon-first inventory (`KHOANG HÀNG` / `TRANG BỊ`) while `HoldSlotLit` illuminates the unlocked hold cells. |
| **6. Closing returns with happy blink** | `Close: 0.00 – 0.50 s` | `0.50 s` | `SmoothStep(t)` + arc-eye blink | Pressing `Tab`/`Esc` collapses the UI back to the horizontal line (`0.00 – 0.14 s`), blends the camera back to chase view (`0.00 – 0.50 s`), and expands `R1CrtFace` (`0.14 – 0.30 s`) into a `0.20 s` happy curved-eye blink (`^ ^`) before re-enabling drive input. |

---

## 4. What Happens When Terrain Blocks the Camera (`[PROPOSAL]`)

### 4.1 Existing Obstacle & Sightline Infrastructure
- `RoverCameraRig` uses `PropSightLine` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:105`) and `CinemachineDeoccluder` with `PullCameraForward` against `Layers.GroundMask | Layers.PropMask` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:290-310`).
- For close-up equipment bay shots, `BayShotComposer.TrySolve` (`Assets/_Project/Scripts/Rover/Camera/BayShotComposer.cs:32-73`) already tests **24 bearings around 07 (every `15°`)**, checking `sight.IsFree(eye, WallClearance)`, 4-corner frustum clearance `FrameClear` (`BayShotComposer.cs:133-154`), and `sight.Clear(focus, ...)` so no rock wall or bay post blocks the lens.

### 4.2 Three-Tier Obstruction Solver for the Face Camera (`[PROPOSAL]`)
When 07 is parked nose-first against a boulder, cliff wall, wreck hull, or steep crater rim (`+Z` blocked within `1.25 m` of `Head`):

1. **Tier 1 — Neck Yaw Bearing Search (`±75°` in `15°` steps, `07` turns its head to clear air):**
   - Because 07's `Neck` node (`tools/art/blender/rover/build_rover.py:102`, `render_suite.py:398-403`) rotates freely up to `±120°` around vertical Y, the solver evaluates candidate neck yaw angles `ψ ∈ {0°, ±15°, ±30°, ±45°, ±60°, ±75°}` using `ISightLine.IsFree` and `BayShotComposer.FrameClear` (`Assets/_Project/Scripts/Rover/Camera/BayShotComposer.cs:52-53, 133-154`) against `Layers.GroundMask | Layers.PropMask`.
   - If `+Z` is blocked by a rock wall in front of 07, **07 turns its neck `ψ` toward the nearest open flank** (e.g. `-60°` left) and the camera positions itself `1.08 m` along that rotated screen normal. Visually, 07 simply turns its head away from the wall to face the player.
2. **Tier 2 — Wider-Angle Close Pull-In (`d: 1.08 m -> 0.62 m` with FOV compensation):**
   - In a tight trench where even `±75°` bearings have less than `1.08 m` of clearance but at least `0.62 m` (`MinDistance`), the camera pulls in to the available clear distance `d_clear ∈ [0.62 m, 1.08 m]` and widens vertical FOV proportionally (`FOV_v = 2 * atan(0.258 / d_clear)`, up to `39.6°`) so `Screen` still occupies `~75%` of the frame height without clipping into the rock behind the camera.
3. **Tier 3 — Instant Unobstructed CRT Modal Fallback (`d_clear < 0.62 m`):**
   - If all bearings within `±75°` have less than `0.62 m` of clearance (e.g. 07 is wedged under an overhang), `TrySolveFaceShot` returns `false` (matching `BayShotComposer.TrySolve` fallback semantics at `BayShotComposer.cs:29-30`). The camera holds its safe deoccluded pose and the `1152×768` CRT `Screen` render texture is presented directly as a centered ~75%-height CRT bezel modal over a blurred viewport, guaranteeing the player is never locked out of `Tab` or shown the inside of a rock mesh.

---

## 5. Input: Mouse Ray onto Screen UV + Gamepad (`[PROPOSAL]`)

### 5.1 Existing Input & Cursor Baseline
- `UISystem.OpenCargo()` currently disables driving input (`_services.Input.Disable()`), switches `_cursor.Menu()`, and calls `_cargo.FocusFirst()` (`Assets/_Project/Scripts/UI/UISystem.cs:767-769`).
- `CargoScreen` and `EquipmentPage` run on Unity UI Toolkit (`VisualElement` trees inside `UIDocument`, `Assets/_Project/Scripts/UI/UISystem.cs:37`).

### 5.2 Mouse Raycast to `Screen` Mesh UV (`[PROPOSAL]`)
When the UI Toolkit panel renders into a dedicated `RenderTexture` (`_ScreenUiTex`) mapped onto `RoverModel/Neck/NeckLift/Head/Screen`:
1. **Analytical Plane-Plus-Bulge Ray Intersection (Zero Physics Collider Overhead):**
   - Rather than adding a `MeshCollider` to `Screen`, a lightweight math helper transforms the mouse ray `Camera.ScreenPointToRay(mousePos)` into `Screen` local space (`headTransform.InverseTransformPoint` / `InverseTransformDirection`).
   - It intersects the ray with the `0.58 m × 0.39 m` screen quad (`z ≈ 0.018 * cos(πx/0.58) * cos(πy/0.39)`, 2 Newton steps from `z = 0`), yielding normalized mesh coordinates `(u_mesh, v_mesh) ∈ [0, 1]²`.
2. **UV-to-Panel Coordinate Mapping:**
   - Accounting for the horizontal mirror in `R1CrtFaceCore.hlsl:267` (`u_raw = 1.0 - u_mesh`) and UI Toolkit's top-left origin (`y_panel = (1.0 - v_mesh) * 768`, `x_panel = u_raw * 1152`), `PanelSettings.SetScreenToPanelSpaceFunction` maps screen-space pointer events directly into the `1152×768` UI Toolkit panel.
   - Outside the `[0, 1]²` screen bezel, `SetScreenToPanelSpaceFunction` returns `Vector2.positiveInfinity` (ignoring off-screen hovers, or closing `Tab` if the player clicks outside the visor bezel).
3. **Drag-and-Drop Parity (`r2_A_drag_vi.png`):**
   - Because `SetScreenToPanelSpaceFunction` feeds standard UI Toolkit `PointerMoveEvent`, `PointerDownEvent`, and `PointerUpEvent` coordinates, hold-cell hover, click-to-fit, and drag-and-drop from a hold cell directly onto its matching round socket bubble work seamlessly. While dragging a kit, its compatible socket bubble and radial leader line pulse in cyan (`#5FF3FF`).

### 5.3 Gamepad & Keyboard Navigation (`[PROPOSAL]`)
- **Initial Focus:** `OpenCargo()` calls `_cargo.FocusFirst()` (`Assets/_Project/Scripts/UI/UISystem.cs:769`), placing the cyan focus brackets on the first occupied cell in `KHOANG HÀNG` immediately at `t = 0.50 s` when the CRT line expands.
- **2D Spatial D-Pad / Stick Traversal (Variant A):**
  - Left/Right/Up/Down (`D-Pad` / `Left Stick`) steps across the unlocked rows (`2`, `4`, or `6` rows of `4` columns) of the Hold Grid, skipping locked dotted cells (or allowing inspection of locked cells with a short tooltip explaining which rack/pannier unlocks them).
  - Stepping right from column 4 (`col = 3`) transitions smoothly into the nearest socket bubble on the left arc of 07's orbital ring (`Back`, `Deck`, `Hold`, `Panniers`), and `D-Pad` steps clockwise/counter-clockwise around the 10 orbital socket bubbles.
  - `LB` / `RB` (or `Q` / `E`) jumps focus directly between **Hold (`KHOANG HÀNG`)** and **Equipped (`TRANG BỊ`)**.

---

## 6. Sound: CRT Thump + Degauss from the Audio Audition (`[PROPOSAL]`)

### 6.1 Diagnosis of Existing `Tab` Audio
- Currently, `UISystem.OpenCargo()` and `CloseCargo()` publish `UiCueKind.Confirm` and `UiCueKind.Back` (`Assets/_Project/Scripts/UI/UISystem.cs:770, 778`), which `UiAudio` routes to `cues.ui_confirm` and `cues.ui_back` (`Assets/_Project/Scripts/Audio/UiAudio.cs:48-55`, `tools/audio/cues.py:589-605`)—two pure additive sine notes (`1.0, 0.15, 0.05` harmonics, `3 ms` linear attack) with no physical switch, cone, or CRT coil transient (`D:\Project\MoonProject-wt\agy-audio\Logs\round1\REPORT.md:18-20`).
- Meanwhile, the codebase already contains physical synthesis building blocks for 07's power-on thump (`wake_power_thump`: `95 -> 52 Hz` pitch-dropping body thump + `D2` `73.4 Hz` warm hum in `tools/audio/cues.py:2348-2358`), first-open hold boot ticks (`HoldBootStarted`, `HoldSlotLit`, `HoldItemArrived` in `Assets/_Project/Scripts/Audio/UiAudio.cs:88-96`), and the Round 2 **C2 ("Radio bench")** audition primitives (`bakelite_switch`, `relay_clack`, and `speaker_cone_thump` `180 -> 126 Hz` in `D:\Project\MoonProject-wt\agy-audio\tools\audio\audition\radio_bench.py:102-216`).

### 6.2 Proposed Diegetic Sound Layering for `Tab` Open / Close (`[PROPOSAL]`)
All cues stay in the warm **`D3`–`D5`** D-major pentatonic register and `-22.0 LUFS` momentary loudness target (`ui_2d`):

1. **Open (`tab_open_crt`, `0.72 s` total, synchronized with Beats 1–5):**
   - **`t = 0.00 s` (Beat 1 -> 2, Switch & Neck Lift):** Rotary bakelite switch click (`bakelite_switch`, `1250 Hz` & `2350 Hz` body modes) + quiet felt-damped neck servo whirr (`D3 -> A3`).
   - **`t = 0.18 s` (Beat 3, Face Collapse to CRT Line):** C2 `speaker_cone_thump` (`180 -> 126 Hz` paper-cone excursion + `410 Hz` chassis box resonance) paired with a soft magnetic reed-relay clack (`relay_clack`) as the vertical deflection collapses to the horizontal scanline.
   - **`t = 0.50 s` (Beat 5, CRT Line Expands + Degauss Bloom):** Low transformer/degauss thump (`wake_power_thump` `95 -> 52 Hz` sub-thud + `146.8 Hz` `D3` degauss coil ring decaying over `220 ms`) opening into the warm two-note valve settle (`D4 -> A4`) and rapid `HoldSlotLit` reed ticks (`18 ms` apart) as the occupied cells populate.
2. **Close (`tab_close_crt`, `0.50 s` total, synchronized with Beat 6):**
   - **`t = 0.00 s` (UI Pinch to Line):** Reversed reed-relay contact release + capacitor voltage sag (`A4 -> D4` over `140 ms`).
   - **`t = 0.16 s` (Happy Blink `^ ^`):** Soft `140 -> 92 Hz` cone settle thud + a brief, gentle `D4 -> F#4` heterodyne valve/ocarina breath affirm as 07's eyes blink back on.
3. **In-Screen Interactions (`Hold` & `Sockets`):**
   - Cell / socket hover: C2 `ticker_line` micro-reed tick (`A4`, `-28.0 LUFS`).
   - Part pick / fit / refuse: C2 `item_pick` (`E4 -> A4` patch-plug click), `item_drop` (`D4/D3` seated transformer thump + socket latch), and `item_refuse` (`B4 -> F#4` muted relay drop).

---

## 7. Which Other Screens Could Later Move Onto 07's Face (`[PROPOSAL]`)

Not every UI screen belongs on 07's CRT face. The guiding rule (`docs/VISION.md`) is **diegetic authorship**: *if 07's onboard computer measures or manages it, it belongs on 07's CRT face; if the human crew printed, wrote, or recorded it on physical media, or if it requires a wide bird's-eye spatial view, it stays off 07's face.*

| Screen / Element | Current Implementation | Move to 07's CRT Face? | Rationale (`[PROPOSAL]`) |
|---|---|---|---|
| **1. Inventory & 07 Sockets (`Tab`)** | `CargoScreen` + `EquipmentPage` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:31`, `EquipmentPage.cs:31`) | **YES (Primary)** | 07's own icon-first inventory (`KHOANG HÀNG` / `TRANG BỊ`) of its 24-cell cargo hold and 10 body sockets; replaces the redundant off-screen `RoverPortrait` render texture (`RoverPortrait.cs:51`) with 07's real body in the world and title-style `drawRover07` line art on screen. |
| **2. Docked Shelter Transfer** | `CargoScreen.IsTransferView` (`Assets/_Project/Scripts/UI/Cargo/CargoScreen.cs:98`) | **YES (Right-Half Swap)** | When docked at the shelter, 07's umbilical links its onboard CRT to the shelter's storage bus: left half shows `KHOANG HÀNG` (24-cell grid), right half swaps `TRANG BỊ` for the shelter storage grid. |
| **3. Detailed Scan Readout / Salvage Analysis** | `PlaceCard` / scan site counters (`Assets/_Project/Scripts/UI/Hud/PlaceCard.cs:16`) | **YES (Optional / Secondary)** | When 07 finishes scanning a wreck or mineral vein, showing the icon breakdown (`metal ×4, optics ×2`) on 07's face fits 07's role as the sensor instrument. Short driving toast chips (`PlaceCard` title while driving) stay on the HUD so the camera doesn't stop driving. |
| **4. Workshop / Upgrade Craft Preview** | `RecipeCard` / `PartStation` (`Assets/_Project/Scripts/Gameplay/Equipment/PartStation.cs`) | **HYBRID (Bay Camera + Face Status)** | During an actual Rover Bay fitting, `BayShotComposer` (`BayShotComposer.cs:32`) already frames the physical robotic arm bolting the part onto 07's body—which is more rewarding to watch externally than staring only at the screen. |
| **5. Crew Logs, Liner Notes & Cassettes** | `MemoryCard` (`MemoryKind.CrewLog`, `Assets/_Project/Scripts/UI/Hud/MemoryCard.cs:112, 234-235, 273`) | **NO — Keep Paper / Tactile** | Per task brief and `docs/VISION.md`, crew logs, handwritten notes, and cassette liner cards are human keepsakes left by Tilly, Kenji, Bell, Moss, and Ro. Rendering them on cotton rag paper / physical cassette cards preserves the contrast between 07's amber CRT terminal and the crew's human artifacts. |
| **6. Basin Topographic Map (`M`)** | `UISystem` map check (`Assets/_Project/Scripts/UI/UISystem.cs:760`) | **NO — Keep Full-Screen / Survey Chart** | A basin-wide map requires high spatial resolution and wide aspect ratio to read crater contours, relay links, and routes; cramping it into `1152×768` at 75% frame height would hurt navigation legibility. |
| **7. Pause & Accessibility Settings (`Esc`)** | `PauseMenu` (`Assets/_Project/Scripts/UI/Pause/PauseMenu.cs:396, 450`) | **NO — Keep Instant 2D Overlay** | Players opening `Esc` (especially to toggle `GentleCamera` or adjust audio/display) need an instant, zero-camera-motion overlay that works even on the title screen before 07 wakes. |

---

## 8. Pixel Budget: Render-Texture Size, Cell Sizes & Font Sizes (`[PROPOSAL]`)

### 8.1 RenderTexture Budget & Memory Neutrality (`[PROPOSAL]`)
- **Resolution & Format:** `_ScreenUiTex` is a single **`1152 × 768`** (`1.5:1`, matching `Screen`'s `0.58 m / 0.39 m = 1.487:1` aspect within `0.8%`) `RenderTextureFormat.ARGB32` (or `R11G11B10_UFloat` / `DefaultHDR` if UI bloom is pre-composited), no depth buffer needed (`depthBits = 0`), bilinear filtering, `mipMap = false`.
- **VRAM Cost:** `1152 × 768 × 4 bytes = 3,538,944 bytes` (**`3.38 MB`**).
- **Net VRAM Savings via `RoverPortrait` Retirement:** Retiring `RoverPortrait`'s off-screen `RenderTexture` + 16-bit depth buffer (`Assets/_Project/Scripts/Rover/Portrait/RoverPortrait.cs:18, 51-54`) and extra URP camera pass keeps net VRAM flat (`~3.4 MB`) and reduces GPU draw calls.
- **On-Demand Rendering:** `_ScreenUiTex` only updates while `_cargo.IsOpen` is `true` (or during Beat 5); during driving, `R1CrtFace.shader` runs its zero-texture procedural eye SDF math (`R1CrtFaceCore.hlsl:290-370`).

### 8.2 Effective On-Screen Pixel Density, Cell Sizes & Font Sizes (`[PROPOSAL]`)
At `1920 × 1080` output resolution, when `Screen` fills **75% of the frame height (`810 px` tall × `1205 px` wide)**:
- **1:1 Pixel Mapping Ratio:** Every `1 px` in the `1152×768` `RenderTexture` maps to **`~1.05 screen pixels` at 1080p** (`~1.40 px` at 1440p, `~2.10 px` at 4K).
- **Cell & Bubble Geometry (`1152×768` RT -> `1080p` screen):**
  - **Variant A Hold Cell:** `86 × 86 px` in RT (`~90 × 90 px` at 1080p), with a `50 × 50 px` icon glyph inside.
  - **Variant B Hold Cell:** `78 × 78 px` in RT (`~82 × 82 px` at 1080p), with a `46 × 46 px` icon glyph inside.
  - **Socket Bubble:** `62 px` diameter (`r = 31 px`) in Variant A (`60 px` / `r = 30 px` in Variant B), with a `34 × 34 px` kit icon inside.
- **Typography Scale Floor (verified in `Logs/mockups/r2_onface_A.png` and `r2_onface_B.png`):**

| UI Element (Round 2 Strict Text Budget) | Font Asset (`D:\Project\MoonProject-wt\agy-uicrt2\Assets\_Project\UI\Fonts\`) | Size in `1152×768` RT | Effective Height at 1080p (`1.05×`) | Minimum Allowed Floor |
|---|---|---|---|---|
| **Two Section Headers (`KHOANG HÀNG` / `TRANG BỊ`)** | `ChakraPetch-Bold.ttf` | `18 px` | `~18.9 px` | `16 px` |
| **Tooltip Title (`Thấu kính ×4` / `Bật nhảy`)** | `ChakraPetch-Bold.ttf` | `17 px` | `~17.9 px` | `16 px` |
| **Stack Count Badge (`2..99` inside cell)** | `IBMPlexMono-Bold.ttf` | `14 px` | `~14.7 px` | `13 px` |
| **Tooltip Description (`1 short line`)** | `IBMPlexMono-Medium.ttf` | `13 px` | `~13.7 px` | `12 px` |
| **Bottom Event Toast (`1 phần để lại...`)** | `IBMPlexMono-Medium.ttf` | `13 px` | `~13.7 px` | `12 px` |

---

## 9. Risks & Mitigations (`[PROPOSAL]`)

### 9.1 Risk 1 — Legibility (Small Displays, Steam Deck `1280×800`, Vietnamese Diacritics, Specular Glare)
- **Failure Mode:** At `1280×800` (720p/800p handhelds), a 75%-height screen is `600 px` tall (`0.78×` downsampling of `1152×768`), and world lights reflecting off `Screen` (`_Smoothness = 0.86`, `R1CrtFace.shader:28`) can wash out icons and tooltip text.
- **Mitigation (`[PROPOSAL]`):**
  1. **Icon-First 86×86 Cells (Round 2):** Replacing per-slot text labels with bold `50×50 px` vector icons inside `86×86 px` square cells makes items readable even at `720p` / Steam Deck scale.
  2. **Suppress Direct Specular on `R1_Screen` in UI Mode:** Scale `_Smoothness` / direct light specular contribution by `0.1` and reduce `_Curvature` from `0.08 -> 0.03` and `_ScanlineDepth` from `0.16 -> 0.07` when the inventory is open.
  3. **Respect `TextScale` / Handheld Zoom:** On `"Large"` text scale or `< 900p` vertical resolution, pull camera distance `d` from `1.08 m` to `0.94 m` so `Screen` fills **86% of frame height**.

### 9.2 Risk 2 — Motion Sickness from Repeated `Tab` Camera Swings
- **Failure Mode:** Players who toggle `Tab` frequently while salvaging wrecks may experience vection or discomfort if the camera swings `180°` from behind 07 to 07's face every time.
- **Mitigation (`[PROPOSAL]`):**
  1. **Honor `GentleCamera` (`Assets/_Project/Scripts/Rover/Camera/RoverCameraRig.cs:179-187`):** When `PlayerSettings.GentleCamera == true`, skip the `180°` orbit swing and perform a `0.25 s` soft cross-dissolve straight to the stationary front-of-face framing (or present the `1152×768` CRT bezel modal in-place).
  2. **Rapid Re-Toggle Debounce:** If `Tab` is pressed again while the `0.55 s` open blend is still in flight (`t < 0.55 s`), reverse smoothly from the current parameter `t` using `SmoothStep` rather than snapping to `t = 1.0`.

### 9.3 Risk 3 — Camera Inside Rocks, Wreck Walls, or Steep Slopes
- **Failure Mode:** 07 stops nose-up against a wreck wall, inside a tight salvage corridor, or on a `25°` crater slope, causing the fixed front-of-face camera offset `(0, 1.498, +1.471)` to land inside geometry or clip through the ground.
- **Mitigation (`[PROPOSAL]`):**
  1. **Cylindrical Arc + `BayShotComposer` Bearing Search (§4.2):** Test 11 neck-yaw bearings (`0°, ±15° .. ±75°`) via `PropSightLine` (`Layers.GroundMask | Layers.PropMask`, `RoverCameraRig.cs:105, 294`) so 07 turns its head away from walls into open space.
  2. **Near-Clip & Pitch Clamp on Slopes:** Compute the camera's world target from `Head`'s actual world transform after suspension settle, clamp minimum ground clearance to `0.35 m` above `Layers.GroundMask`, set `nearClipPlane = 0.05 m`, and fall back to Tier 3 (centered CRT modal) whenever `d_clear < 0.62 m`.
