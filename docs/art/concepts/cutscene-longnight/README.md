# Opening Cutscene Key Frames — Variant D: "The Long Night"

**Source Spec:** [`docs/design/opening-cutscene-script.md`](../../../design/opening-cutscene-script.md) §7 (Variant D shot list, wow beats, canon cautions) & §7.3 (10 cinematic key-frame prompts)  
**Visual Bar:** R1 stylized realism (`docs/art/r1-pipeline.md`), Lunarion palette (`docs/VISION.md` §Palette), dramatic hard vacuum light (no atmosphere, no haze/fog, pitch-black sky, razor shadows, cool earthlight fill), real sense of scale (tiny warm points vs. vast basin, Arks vs. Earth), no faces of people (suit lamps & a gloved EVA hand only), no repeated round-hole patterns.  
**Aspect & Resolution:** `2.39:1` scope (`3440 × 1440` PNG) per key frame + `2 × 5` contact sheet ([`sheet.png`](./sheet.png)).

---

## 1. Pipeline & Model Authenticity (Matte Generation + Real R1 3D Game Models)

Why Variant D ("The Long Night") replaces the flat Site Cam boards:
1. **Small ↔ Vast Scale Contrast:** Variant D alternates between cosmic/aerial vistas (`35 mm` orbit, `50 mm` descent, `24 mm` banking aerial at `200 m`, `20 mm` low-angle Departure sky river, `35 mm` `600 m` nadir ring) and the intimate locked `Site Cam 2` frame (`26 mm` CCTV in Shots 04 & 06) before descending through decades (`35 mm` → `50 mm` → `85 mm` close-up) into `07`'s dark curved CRT face.
2. **Hard Vacuum Lighting & Restrained Palette:** Every shot obeys strict vacuum optics — zero atmospheric scattering, zero fog or haze, a pitch-black star sky (`#03050A`–`#080B14`), razor-sharp shadow terminators (`0.53°` solar disc), and cool slate-blue earthlight fill (`#182238` / `#3E4048`). Warm amber (`#FFB547`) is reserved exclusively for living beacons, `07`'s lamp and CRT phosphor, the departing Ark drive-flares, and the 2024 copper eclipse corona (`#D96B43`).
3. **Real In-Game Models Composited into Every Shot Where They Appear (`tools/art/blender/*` & `Assets/_Project/Art/R1/Models/*`):**
   - **Real Rover `07` (`tools/art/blender/rover/build_rover.py` & `rover_parts.py`):** Built directly via `rover_parts.build_rover_model_hierarchy(build_rover.DIMENSIONS, mats)` (`1.476 m` outer tyre span, `1.53 m` CRT head height, `1.592 m` whip antenna tip height) with `SolarWing` mounted on `WingPivot` and post-opening gameplay gifts (`CellFilled`, `Pennant`) hidden. Includes the pillowed `R1_CreamEnamel` (`#F4E6C8`) beetle carapace, continuous `R1_OrangeStripe` (`#FF8A5B`) beltline, riveted `R1_SagePatch` left-flank repair plate, `KESSLER–ARAI` flank stencils, boxy `R1_CreamEnamel` CRT `Head` with dark charcoal `Visor` shroud, convex 4:3 `Screen`, left-cheek turned-brass viewfinder `Eye` pod (Ø `0.24 m`), and the mismatched rear-left wheel (`Wheel_RL` with dark dish and raised orange X-brace).
   - **Real Station, Lander, Relay & Ruin Hardware (`Assets/_Project/Art/R1/Models/`):**
     - `Lander/CargoLander.fbx` (`5.58 m` cream-and-coral VIS cargo lander with arched rover bay, deployable `RampDoor`, and gold Kapton leg damper sleeves) + `Props/LanderBeacon.fbx` and `Props/LanderServicePort.fbx`.
     - `Props/DeadChargingPost.fbx` (`0.99 m` trickle post in the lander's lee) + `Props/GroundCableEnd.fbx`.
     - `Crates/Crate_M.fbx` and `Crates/Crate_L.fbx`.
     - `Ruins/HabitatModules.fbx`, `Ruins/KenjiWorkshop.fbx`, `Ruins/RadioTower.fbx`, `Ruins/FallenSolarArray.fbx`.
     - `Structures/SolarArray.fbx`, `Structures/AntennaMast.fbx`, `Structures/RelayHut.fbx`.
   - **No Human Faces or Mannequins:** People appear strictly as distant warm-white suit helmet lamp points/cones (`04_last_crate.png`) and a close-cropped ILC Dover pressurized EVA gloved hand + copper wrist ring flipping `07`'s flank switch while the suit lamp is already turning away (`06_continue.png`).

---

## 2. Per-Frame Breakdown (`01` – `10`)

### Frame 01 — [`01_earthrise_over_the_limb.png`](./01_earthrise_over_the_limb.png)
- **Time & Title:** `0:00–0:08` · **"Earthrise over the limb"**
- **Prompt (`opening-cutscene-script.md` §7.3 #1):**
  > **"Earthrise over the limb"** (orbit, 35 mm): the grey lunar limb across the lower third in hard white sunlight; Earth a thin blue crescent rising beyond it with sparse warm city lights on its night side; a deep black sky.
- **Method:** R1 stylized-realism orbital matte plate generated at wide `2.39:1` aspect and finished at `3440 × 1440` with sparse-light density grading on Earth's night side (1987 *Long Dim* canon rule) and faceted highland ridge sculpting across the lunar limb (zero repeated round-hole patterns).
- **Lens & Composition Notes:** `35 mm` prime in low lunar orbit. The stark, airless lunar south-pole limb spans the lower third with unscattered white sunlight (`#D8DCE2`) and pitch-black crater floor shadows (`#05070C`). Earth rises at the upper-right third: a crisp ocean-blue (`#3FA7D6`) and cloud-white crescent rim on the sunlit limb, and sparse warm amber (`#FFB547`) city light nodes across its dark hemisphere against pure black vacuum space.

---

### Frame 02 — [`02_descent.png`](./02_descent.png)
- **Time & Title:** `0:08–0:20` · **"Descent"**
- **Prompt (`opening-cutscene-script.md` §7.3 #2):**
  > **"Descent"** (50 mm, behind the craft): a cream-and-coral VIS cargo lander small in the upper frame, a soft engine glow, descending toward a ring crater half in shadow at the lunar south pole; one cluster of warm lights inside it, and a dotted chain of amber relay lamps along the rim.
- **Method:** High-oblique south-polar orbital matte plate (`plate_02_descent_bg`) composited with a headless Blender Cycles `50 mm` RGBA render (`fg_02_descent.png`) of the real `CargoLander.fbx` + `LanderBeacon.fbx` (with `RampDoor` retracted for orbital descent) in hard white vacuum sunlight, plus a soft vacuum reaction/descent nozzle glow beneath the lander belly (pure light gradient, zero atmospheric smoke plume).
- **Lens & Composition Notes:** `50 mm` pursuit framing from behind and slightly above the descending VIS cargo lander in the upper-left third (`-14°` bank, `-28°` pitch), leading the viewer's eye diagonally across the razor-sharp south-pole terminator into the pitch-black bowl of the ring crater below, where Lumen Station glows as a single warm amber (`#FFB547`) island and the dotted chain of relay lamps climbs the jagged rim ridge to the Peak.

---

### Frame 03 — [`03_lumen_at_its_peak.png`](./03_lumen_at_its_peak.png)
- **Time & Title:** `0:20–0:36` · **"Lumen at its peak"** (1987, night)
- **Prompt (`opening-cutscene-script.md` §7.3 #3):**
  > **"Lumen at its peak"** (24 mm aerial, 200 m, banking): a night basin; a lit habitat, landers on pads, lines of tiny rover lamps; amber relay masts blinking in a chain up to a lit dish on a sharp peak; warm points on a cold grey floor.
- **Method:** `24 mm` banking aerial night-basin matte plate (`plate_03_lumen_aerial_bg`) composited with a headless Blender Cycles `24 mm` banking aerial pass (`fg_03_lumen_aerial.png`) containing two real `CargoLander.fbx` units on the landing pad aprons, `HabitatModules.fbx`, `KenjiWorkshop.fbx`, `RadioTower.fbx`, `SolarArray.fbx`, `RelayHut.fbx`, `AntennaMast.fbx`, and real `RoverModel` (`07`) units crawling along the regolith wheel tracks with warm `#FFB547` headlamps.
- **Lens & Composition Notes:** `24 mm` wide-angle dolly at `200 m` altitude banking `-7.5°` across the `600 m` crater basin (obeying the canon rule that intact Lumen is shown only from `200 m` and above with no interiors or crew faces). Cold Moon-grey earthlit regolith (`#3E4048`–`#787A80`) contrasts with the warm amber (`#FFB547`) station windows, pad beacons, rover lamp lines, and the sweeping chain of relay masts climbing the right-hand ridge to the lit Great Dish crowning the sharp northern Peak.

---

### Frame 04 — [`04_last_crate.png`](./04_last_crate.png)
- **Time & Title:** `0:36–0:44` · **"Last crate"** (`Site Cam 2`, locked)
- **Prompt (`opening-cutscene-script.md` §7.3 #4):**
  > **"Last crate"** (CCTV, locked): a 1980s site-camera frame, scanlines, the stamp `SITE CAM 2 · LUMEN-04 · 28 OCT 1988`; a small cream rover with a CRT face rolling a crate down a lander ramp; two suit lamps.
- **Method:** Ground-level rugged lunar crater-rim backdrop (`plate_04_sitecam_backdrop`) composited with a headless Blender Cycles `26 mm` locked-camera pass (`fg_04_last_crate.png`) of the real `CargoLander.fbx`, glowing `LanderBeacon.fbx`, `LanderServicePort.fbx`, `DeadChargingPost.fbx`, `Crate_L.fbx`, `Crate_M.fbx`, background `HabitatModules.fbx` / `RadioTower.fbx` / `SolarArray.fbx`, and the real `RoverModel` (`07`) rolling `Crate_M.fbx` down the deployed lander ramp. Two distant EVA workers in the midground are represented strictly as warm-white suit helmet lamp points with conical dust-mote beams and razor ground shadows (**zero human faces, zero stick mannequins**). Graded through an authentic 1980s vidicon CCTV pass with scanlines and bottom-left character-ROM stamp.
- **Lens & Composition Notes:** Locked `26 mm` Site Cam 2 security mount (`(0.0, -9.20, 2.10) m` looking north toward the Peak and Earth). `07` is angled at `+138°` yaw at the foot of the ramp so its warm amber two-pill-eye CRT `Screen`, left-cheek brass `Eye` pod, and mismatched rear-left wheel (`Wheel_RL`) read clearly under the lander's amber beacon pool.

---

### Frame 05 — [`05_the_departure.png`](./05_the_departure.png)
- **Time & Title:** `0:44–1:06` · **"The Departure"** (the one big musical swell)
- **Prompt (`opening-cutscene-script.md` §7.3 #5):**
  > **"The Departure"** (20 mm, low angle from the basin floor): 07 and the lander tiny in the foreground; Earth low over the crater rim; from Earth's limb, dozens of soft golden drive-flares rising and curving into a long slow river of light across the black sky, gathering into a distant cluster; awe, no violence.
- **Method:** `20 mm` ultra-wide low-angle cosmic matte painting (`plate_05_departure_sky`) composited with a headless Blender Cycles `20 mm` low-angle foreground pass (`fg_05_departure.png`) of the real `CargoLander.fbx` (with lit `LanderBeacon.fbx`), `DeadChargingPost.fbx`, and real `RoverModel` (`07`) small in the lower-left foreground with its CRT head pitched up (`-24°`) watching the sky.
- **Lens & Composition Notes:** `20 mm` ultra-wide lens placed `0.72 m` above the dark basin floor (`+14°` upward pitch), framed on left and right by steep faceted crater walls. In the lower-left foreground, `07` and the cargo lander sit tiny against the dark regolith, while from Earth's limb above the rim, dozens of soft golden-amber (`#FFB547` / `#FFE4B5`) long-exposure drive-flares curve gracefully across the pitch-black starfield into the distant Ark halo cluster in the upper-left sky.

---

### Frame 06 — [`06_continue.png`](./06_continue.png)
- **Time & Title:** `1:06–1:14` · **"Continue"** (`Site Cam 2`, locked)
- **Prompt (`opening-cutscene-script.md` §7.3 #6):**
  > **"Continue"** (CCTV): the same site-cam frame with white storm sparkles across it; a gloved hand at the rover's flank switch; a small screen glowing `CONTINUE`; the suit lamp already turning away.
- **Method:** Identical locked `26 mm` camera and basin backdrop as Frame 04 (`04_last_crate.png`) for 100% spatial lock continuity, composited with headless Blender Cycles render `fg_06_continue.png`: real `RoverModel` (`07`) parked at `DeadChargingPost.fbx` in the lander's lee with its curved 4:3 CRT `Screen` glowing warm amber with `CONTINUE_`, and a close-cropped ILC Dover pressurized EVA gloved hand + copper wrist disconnect ring flipping `07`'s flank switch while the suit lamp beam at the right frame edge is already angled away toward the habitat (pure work gesture, zero human face). Overlaid with 380 white radiation-storm Compton-scatter sensor sparkles/streaks and bottom-left OSD stamp `SITE CAM 2 · LUMEN-04 · 28 OCT 1988 · 21:42`.
- **Lens & Composition Notes:** Locked `26 mm` Site Cam 2 frame. Above the rim, Earth is wrapped in a pale radiation haze as a thin chain of departing Ark points climbs into the dark sky, while on the apron below, the gloved hand flips `07` to `CONTINUE` without a pause.

---

### Frame 07 — [`07_the_network_goes_dark.png`](./07_the_network_goes_dark.png)
- **Time & Title:** `1:14–1:28` · **"The network goes dark"**
- **Prompt (`opening-cutscene-script.md` §7.3 #7):**
  > **"The network goes dark"** (35 mm, straight down, 600 m): the whole crater as a ring; half its lights still on, half out, the darkness sweeping round the rim toward the peak's last lit dish; one amber beacon at the centre.
- **Method:** High-altitude `35 mm` nadir (`90°` straight-down from `600 m`) matte plate (`plate_07_network_dark_bg`) composited at the exact basin centre with a headless Blender Cycles straight-down nadir pass (`fg_07_network_dark.png`) of the real station cluster (`CargoLander.fbx` with its lone active `LanderBeacon.fbx`, dormant `RoverModel` `07` at `DeadChargingPost.fbx`, and darkened `HabitatModules.fbx`, `KenjiWorkshop.fbx`, `RadioTower.fbx`, `SolarArray.fbx`).
- **Lens & Composition Notes:** `35 mm` nadir view from `600 m` framing the entire south-pole crater as a monumental faceted stone ring. The western and southern arcs of the rim have already gone completely dark; only the eastern arc of amber relay lamps (`#FFB547`) still trails upward toward the last lit dish on the northern Peak crest at top-centre, leaving the cargo lander's single amber beacon (`#FFB547`) alone at the dead centre of the basin floor.

---

### Frame 08 — [`08_decades.png`](./08_decades.png)
- **Time & Title:** `1:28–1:52` · **"Decades"**
- **Prompt (`opening-cutscene-script.md` §7.3 #8):**
  > **"Decades"** (35 mm, mid-descent): a long-exposure feel; the low Sun's arcs as faint rings around the horizon; shadows like sundial strokes around the peak; dust banks; a meteor streak; one tiny dark rover at the centre.
- **Method:** `35 mm` elevated mid-descent chronophotography matte plate (`plate_08_decades_bg`) composited at the basin centre with a headless Blender Cycles multi-azimuth sun render (`fg_08_decades.png`) of the real `RoverModel` (`07`, completely dark/dormant, half-buried in sculpted regolith `DustBanks` at the exact frame centre), `CargoLander.fbx` (beacon dark), `DeadChargingPost.fbx` (tally light dead), `HabitatModules.fbx`, `RadioTower.fbx`, and `FallenSolarArray.fbx`.
- **Lens & Composition Notes:** `35 mm` mid-descent oblique angle looking down into the basin. Faint golden-white solar horizon ring arcs skim the jagged polar rim crest; overlapping radial "sundial stroke" shadows fan out across the regolith floor from five low solar azimuths; a crisp white-gold meteor streak flashes across the upper-left black sky; and at the exact geometric centre sits tiny dark `07`, unmoving as thirty-six years of dust banks creep around its wheels.

---

### Frame 09 — [`09_the_call.png`](./09_the_call.png)
- **Time & Title:** `1:52–2:05` · **"The call"** (2024 eclipse)
- **Prompt (`opening-cutscene-script.md` §7.3 #9):**
  > **"The call"** (50 mm, still): a dark basin of ruins under an eclipsed Earth with a copper ring; one far point of light beside Earth; a thin cyan pulse running across the black floor along a dead cable to a small dusty rover.
- **Method:** `50 mm` still 2024 eclipse basin plate (`plate_09_the_call_bg`) composited with a headless Blender Cycles `50 mm` pass (`fg_09_the_call.png`) of the real 2024 ruins (`CargoLander.fbx` with dark beacon, `FallenSolarArray.fbx`, `HabitatModules.fbx`, `RadioTower.fbx`, distant `AntennaMast.fbx` `relay.2` at the canyon mouth, `DeadChargingPost.fbx`, heavy ground cable, and dormant dust-draped `RoverModel` `07` with its whip `AntennaTip` lit cyan `#5FF3FF`).
- **Lens & Composition Notes:** `50 mm` still low-angle composition compressing the depth between the foreground ruins and the northern Peak. Above the canyon mouth hangs the eclipsed Earth ringed by a vivid copper-orange corona (`#D96B43` / `#FFB547`), with one bright Ark carrier star point blinking beside it (**three short, one long**). Across the dark, copper-rim-lit floor, a razor-thin electric cyan (`#5FF3FF`) carrier pulse races from `relay.2` along the half-buried cable into `07`'s post, sparking a bright cyan flash on `07`'s whip antenna tip.

---

### Frame 10 — [`10_the_face.png`](./10_the_face.png)
- **Time & Title:** `2:05–2:15` · **"The face"** (handoff into `BootFaceStage.PowerOn`)
- **Prompt (`opening-cutscene-script.md` §7.3 #10):**
  > **"The face"** (85 mm close-up): the rover's dark curved CRT face filling the frame, one amber power-on dot at its centre, dust on the bezel.
- **Method:** `85 mm` shallow-DOF (`f/2.8`) optical bokeh eclipse-basin backdrop (`plate_10_face_bokeh_bg`, with background ruin windows darkened) composited with a headless Blender Cycles `85 mm` close-up render (`fg_10_the_face.png`) of the real `RoverModel` (`07`)'s `Head`, `Visor`, convex 4:3 `Screen`, left-cheek turned-brass `Eye` pod, ribbed rubber `Neck` bellows, upper `Body` collar, and sculpted regolith dust drifts (`BezelDust`) settled on the upper visor brow, lower chin bezel ledge, and cheek lens housing.
- **Lens & Composition Notes:** `85 mm` portrait close-up (`1.76 m` focus distance, `f/2.8`) matching the `BootFaceStage.PowerOn` inventory handoff framing. The dark curved CRT glass fills the centre of the `2.39:1` frame, rim-lit by subtle copper eclipse light on the upper-left bezel and cool slate earthshine on the brass cheek lens, while a single incandescent warm-amber phosphor dot (`#FFF4C8` core with `#FFB547` halation and a faint horizontal cathode wake-up streak) ignites at the exact centre of the screen.
