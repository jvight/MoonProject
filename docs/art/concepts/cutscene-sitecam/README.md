# Opening Cutscene Storyboard — Variant C: "Site Cam 2"

**Source Spec:** [`docs/design/opening-cutscene-script.md`](../../../design/opening-cutscene-script.md) §3 (Variant C shot list) & §6 (8 storyboard frame prompts)  
**Visual Bar:** R1 stylized realism (`docs/art/r1-pipeline.md`), Lunarion palette, near-polar lunar basin (`LUMEN-04`), vacuum logic, no human faces, no sentimentality.  
**Resolution:** `3440 × 1440` (`21:9` ultrawide PNG) per frame + `3584 × 3466` `2×4` contact sheet ([`sheet.png`](./sheet.png)).

---

## 1. How 07 and the R1 Scene Assets Were Referenced

To guarantee that **07 matches the real in-game rover** (and not a generic robot) and that the **camera is 100% locked across Frames 1–7**, all eight frames were built and rendered in headless Blender 5.1 (`CYCLES` with Freestyle R1 silhouette/crease inking and `AgX - Medium High Contrast`) directly from the canonical R1 procedural builders and exported project FBXs on `main`:

1. **Real 07 (`tools/art/blender/rover/build_rover.py` & `rover_parts.py`):**
   - Built directly via `rover_parts.build_rover_model_hierarchy(build_rover.DIMENSIONS, mats)` (`1.476 m` outer tyre span, `1.53 m` head height, `1.592 m` antenna tip height).
   - **Carapace (`Body`):** Pillowed "beetle" hull in `R1_CreamEnamel` (`#F2E3C6`) with a continuous `R1_OrangeStripe` (`#FF8A5B`) beltline (`y = 0.56..0.65 m`), recessed `R1_GreyMetal` seam channels, riveted `R1_SagePatch` (`#78A582`) left-flank repair plate (`10` brass dome rivets), `KESSLER–ARAI` flank stencils (`Decal_KA_L` / `Decal_KA_R` mapped to `T_R1_MakerDecals_Albedo.png`), and `"07"` serial plaques (`Decal07Fresh`).
   - **CRT Head (`Head`, `Screen`, `Visor`, `Eye`, `Neck`):** Boxy `0.75 m × 0.50 m × 0.48 m` `R1_CreamEnamel` CRT monitor housing on a 4-ribbed `R1_Rubber` bellows `Neck`, wrapped in a dark charcoal `Visor` shroud framing the convex `0.553 m × 0.3687 m` `Screen`, with the coaxial turned-brass viewfinder lens pod (`Eye`, Ø `0.24 m`) on its left cheek (`-X`).
   - **Suspension & Mismatched Wheel (`Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`):** Four Ø `0.70 m` balloon chevron tyres (`22` paired tread lugs per wheel) on twin-spar wishbones (`Arm_*`) and `R1_Copper` helical coil-over shocks (`Shock_*`). Oriented at a `3/4` angle (`yaw = +142°`) toward `SITE CAM 2` so **`Wheel_RL` (the mismatched rear-left wheel with the dark grey dish and bold raised `R1_OrangeStripe` X-brace)** and `Wheel_FL` (`R1_SagePatch` hub) face the camera clearly.
   - **Tools, Solar Wing & Whip Antenna:** Front 6-tooth `DigScoop` bucket (`0.68 m` wide), chest `CutterTurret` in a stepped brass collar, twin brass-bezel `Headlamp_L/R`, rear-deck `SolarWing` (`2×3` `R1_SolarCell` panel), and rear-shoulder whip `Antenna` with brass `SonarLoop` and `AntennaTip` dome. Post-opening gameplay gifts (`Pennant`, `CellFilled`) are hidden so 07 appears in its authentic opening-cutscene configuration.
2. **Lander, Trickle Post, Crates & Station Ruins (`Assets/_Project/Art/R1/Models/`):**
   - `Lander/CargoLander.fbx` (`5.58 m` cream-and-coral cargo lander with arched rover bay, lowered `RampDoor`, gold Kapton leg damper sleeves, and `VIS 1985` / `ILC` maker decals).
   - `Props/LanderBeacon.fbx` (stepped Fresnel amber beacon in a 4-strut birdcage mounted on the lander's upper shoulder) and `Props/LanderServicePort.fbx`.
   - `Props/DeadChargingPost.fbx` (`0.99 m` waist-high trickle post in the lander's lee) + `Props/GroundCableEnd.fbx` and umbilical cable.
   - `Crates/Crate_M.fbx` (`0.60 × 0.44 × 0.38 m` cargo crate with `SEC 04` stencil) and `Crates/Crate_L.fbx`.
   - `Ruins/HabitatModules.fbx`, `Ruins/RadioTower.fbx`, `Ruins/FallenSolarArray.fbx`, and `Rocks/Rock_01..05.fbx` along the path toward the distant crater rim and northern Peak silhouette.

---

## 2. Camera Continuity & Pipeline Rules

- **Locked Camera (Frames 01–07):** Fixed at Blender world position `(0.00, -9.40, 2.25) m` looking at `(0.00, 2.50, 1.45) m` with a `26.0 mm` lens (`36 mm` sensor, `21:9` `3440 × 1440`). The lander, trickle post, background station domes, and crater rim occupy identical pixel coordinates across all seven frames.
- **1980s CCTV Site Camera Pass (Frames 01–05):** Analog vidicon soft focus, horizontal chroma/luma bandwidth bleed, warm amber-sepia cast with lifted dark-charcoal blacks, horizontal raster scanlines, corner vignette, and a crisp white character-ROM bottom-left stamp.
- **Full R1 Colour Pass (Frames 06–08):** When the 1980s CCTV feed dies in Frame 05 and cuts to 2024 in Frame 06, the CCTV filter, scanlines, and stamp vanish completely, revealing crisp full-gamut R1 stylized-realism colour lit by the copper-ringed Earth eclipse over the rim.

---

## 3. Per-Frame Breakdown

### Frame 01 — [`01_night_shift_1988.png`](./01_night_shift_1988.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 1 — Night shift, 1988.** A cream-and-coral cargo lander with a lit amber beacon; a small cream rover (07, CRT face, mismatched wheel) rolling a crate down the ramp; two distant suit lamps; seen through a 1980s CCTV site camera, soft focus, warm cast, scanlines, white stamp bottom-left `SITE CAM 2 · LUMEN-04 · 28 OCT 1988 · 21:40`. R1 stylized-realism, Lunarion palette.
- **How 07 was referenced:** `RoverModel` positioned at the foot of `CargoLander.fbx`'s deployed `RampDoor` (`(-1.05, 0.55, 0.08) m`, `yaw = +136°`), guiding `Crate_M.fbx` onto the apron. 07's CRT `Screen` runs the procedural warm-amber two-pill-eye shader (`configure_procedural_crt_screen_shader`), and both its left-cheek brass `Eye` and mismatched `Wheel_RL` face the camera.
- **Notes:** `LanderBeacon.fbx` glows warm amber (`#FFB547`) on the lander shoulder. Two suited EVA figures in the midground are shown strictly as backlit silhouettes with warm-white helmet lamp cones (`no human faces`). Earth hangs calm above the crater rim.

### Frame 02 — [`02_continue.png`](./02_continue.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 2 — CONTINUE.** The same locked frame; 07 parked on a small trickle post in the lander's lee; a gloved hand at 07's flank switch; 07's CRT screen glowing `CONTINUE`; the suit lamp already turning away.
- **How 07 was referenced:** `RoverModel` parked at `(0.75, -1.45, 0.00) m` (`yaw = +142°`) beside `DeadChargingPost.fbx` in the lander's lee, connected via heavy rubber umbilical cable. 07's curved 4:3 CRT `Screen` is driven by a custom `1152 × 768` warm-amber phosphor texture displaying `CONTINUE` with a block cursor and trickle-charge bar.
- **Notes:** An ILC Dover cream thermal-garment forearm with copper/steel wrist disconnect ring and grey-palmed EVA glove reaches in from the right to flip `FlankSwitchPanel` on 07's side — framed as a quick, matter-of-fact work gesture with zero sentimentality. The worker's suited torso, PLSS backpack, and helmet lamp at the right frame edge are already angled away (`+52°`) toward the habitat (`no face visible`). Bottom-left CCTV stamp advances to `SITE CAM 2 · LUMEN-04 · 28 OCT 1988 · 21:42`.

### Frame 03 — [`03_the_storm.png`](./03_the_storm.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 3 — The storm.** The same locked frame, white particle sparkles across the image; far above the rim, a thin chain of bright points rising away from a hazed Earth; two suit lamps small, walking out of frame.
- **How 07 was referenced:** `RoverModel` remains locked in the exact parked pose at the trickle post (`(0.75, -1.45, 0.00) m`), its CRT face holding a dimmed standby `CONTINUE` raster as the station powers down to standby.
- **Notes:** `340` radiation-storm particle hits (bright white Compton-scatter sensor sparkles and short horizontal vidicon scan streaks) pepper the CCTV image. Far above the crater rim, Earth is wrapped in a pale radiation haze while a thin rising chain of 8 bright points climbs away from L4/L5 (the Arks departing). In the right midground, two small suit lamps recede toward `HabitatModules.fbx`. Stamp reads `SITE CAM 2 · LUMEN-04 · 28 OCT 1988 · 21:48`.

### Frame 04 — [`04_time_lapse.png`](./04_time_lapse.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 4 — Time-lapse.** The same locked frame; long low sun shadows sweeping; dust crept half over 07's wheels; frost glints at the post; the lander's beacon dark; stamp reading `2003`.
- **How 07 was referenced:** `RoverModel` in the exact same parked position, now dormant with its CRT `Screen`, headlamps, and antenna tip completely dark (`R1_RoverScreenMat` emission `0.0`). Sculpted regolith drift mounds (`DustBanks_2003`) cover the lower half (`z = 0.00..0.23 m`) of 07's Ø `0.70 m` chevron tyres.
- **Notes:** Rendered with two opposing low polar sun azimuths (`-62°` and `+48°`) blended together so long, low sun shadows fan and sweep across the regolith apron while the geometry remains razor-locked. `LanderBeacon.fbx` is dark (`R1_BeaconDarkOff`), `CargoLander.fbx` shows its severed bay cable (`Bay_CableGap`), specular ice facets (`FrostGlints_2003`) glint on `DeadChargingPost.fbx`, and long-exposure star trails arc across the upper sky. Stamp reads `SITE CAM 2 · LUMEN-04 · 2003`.

### Frame 05 — [`05_no_signal.png`](./05_no_signal.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 5 — No signal.** The same locked frame drained of colour, a horizontal roll bar across the frame, the image half-frozen, stamp `NO SIGNAL`.
- **How 07 was referenced:** Identical locked 3D scene and dormant 07 pose from the 2003 pass, drained of all colour into a degraded monochrome vidicon greyscale buffer.
- **Notes:** A heavy horizontal vertical-blanking roll bar with white sync-pulse edges and horizontal line-tear displacement cuts across the centre of the frame (`y ≈ 48%`), leaving the lower field half-frozen and peppered with analog snow before the camera's capacitor dies. Bottom-left OSD stamp reads `NO SIGNAL`.

### Frame 06 — [`06_2024_in_colour.png`](./06_2024_in_colour.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 6 — 2024, in colour.** The same frame, now rendered cleanly by the game's camera in full R1 colour; ruins and dust banks in the lander's lee; 07 dark; in the black sky over the rim, an Earth eclipse with a copper ring; a faint point of light beside it.
- **How 07 was referenced:** `RoverModel` in the exact same locked camera position and lens as Frames 01–05, now rendered cleanly in full R1 stylized-realism colour with zero CCTV filter, zero scanlines, and no OSD stamp. Deeper 2024 regolith drifts (`DustBanks_2024`) bank in the lander's lee around 07's wheels and the trickle post; 07's CRT `Screen` and lamps remain dark.
- **Notes:** `FallenSolarArray.fbx` and the weathered station ruins sit in cool slate-blue lunar shadow (`#182030`) rim-lit by warm copper light (`#D96B43`) from the northern sky, where the eclipsed Earth hangs with a glowing copper atmospheric ring and a faint point of light beside it (the returning Ark IV carrier).

### Frame 07 — [`07_the_call.png`](./07_the_call.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 7 — The call.** Same frame as 6; 07's antenna tip lamp lit cyan for a moment; the sky point visibly brighter; the mood hushed.
- **How 07 was referenced:** Identical full-colour 2024 R1 frame as Frame 06, except `RoverModel`'s rear-shoulder whip `AntennaTip` dome is driven by `R1_AntennaCyanOn` (`#5FF3FF`, emission strength `11.0`) plus a local cyan point light (`AntennaCyanPt`) that casts a delicate cyan reflection onto 07's `SonarLoop`, folded `SolarWing`, and cream rear carapace.
- **Notes:** In the black sky above the crater rim, the point of light beside the copper-ringed eclipsed Earth pulses visibly brighter with a crisp 4-point diffraction glint as the carrier broadcasts its wake-up call sign.

### Frame 08 — [`08_the_face.png`](./08_the_face.png)
- **Prompt used (`opening-cutscene-script.md` §6):**
  > **Frame 8 — The face.** Close-up of 07's dark CRT face (the inventory framing), a single amber power-on dot at its centre.
- **How 07 was referenced:** Camera placed in the `RoverCameraTuning` inventory framing directly in front of `RoverModel`'s `Screen` (`35 mm` lens, `1.66 m` distance, `4°` head pitch), framing the boxy `R1_CreamEnamel` `Head`, dark charcoal `Visor` shroud, convex 4:3 `Screen`, left-cheek turned-brass viewfinder `Eye` pod, ribbed rubber `Neck` bellows, and upper `Body` collar with `CutterTurret`.
- **Notes:** `Screen` is rendered with `_make_crt_dot_image` (`R1_RoverScreenMat`), placing a single incandescent warm-amber phosphor power-on dot (`#FFF4C8` core with `#FFB547` halation and a subtle horizontal tube wake-up whisper) at the exact centre of the dark curved CRT glass (`BootFaceStage.PowerOn`).
