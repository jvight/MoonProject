# Basin dressing: props that lead the player (placement plan)

> Owner ask (2026-10-18): the basin looks empty; give it soul, and arrange the props so they lead the player.
> Builds on `opening-v2.md` (§2 beats B0–B11, §4 layout; coordinates in metres from the wake pad, +Z north,
> bearings clockwise from north). World implements; Argon models. Story canon: `story-v4.md`, `07-identity.md`.

## 0. Player experience
- **Hook:** after the eclipse, the low Sun shows things the dark hid: a glint at the ruins, bootprints, a flag.
- **Pull:** each find asks *who walked here, and when?* A three-legged shadow at last light asks *what is that?*
- **Hold:** the scan pages fill the Machines databank. In 0.6, power wakes the seismic station's dead lamp.
- **Finish:** the basin tells its own history without text: the 1967 probe, the 1971 sortie, the station.
  The player knows **people were here long before 07**.
- **Feeling:** *company in absence.* People were here, and the place still keeps their marks.
- **Learn, then improve:** Subnautica pulls the player with silhouettes, light and wrecks. Here a lure exists only
  when **the sky makes it physically true**: glints and shadows need the Sun, and prints show only in raking
  light. So the world takes turns pointing, and props never shout over the critical path.

## 1. Rules
1. **One lure and one guide line per beat** in the world. The objective line stays the one UI element (§3 of
   opening-v2). A lure retires when its job is done.
2. **B0–B5 is the beacon's.** In the eclipse dark, nothing new is lit or glints within 45 m of the wake→lander
   lane. Bootprint decals read **only in direct sunlight** (relief shading, no albedo), so they appear at B6.
3. Optional finds sit **20–60 m off a lane**, one point of interest every **150–200 m** of lane.
4. **Logic:** every prop has a reason to be where it is (below), and nothing floats or glows without a source.
   **No bodies, ever.** No real-world logos or flags: generic ILC white, sun-bleached.

## 2. Placement
Sun in the opening (World `SkyCycleSettings`): it breaks out of the eclipse at ~**346°** (NNW, low) and holds there
at golden hour (B6–B8). After power-on it sinks to **last light, low in the SSE (~160°)** (B8–B9).

| Prop | Job | Position (x, z) | First seen | Sightline / why it reads | Why it's there (logic) |
|---|---|---|---|---|---|
| **Wake cable + ruts** (exists) | guide line B2 | wake → lander | B0 | the cable wave runs ahead in the dark | fed 07's post |
| **Lander beacon** (exists) | lure B0–B5 | (35, 160) | B0 | the only warm light in the eclipse | emergency beacon |
| **Laser retroreflector panel** | lure **B6→B7** | (81, 177), ruins' west edge | B6, from the pad | a soft glint pulses as 07 or the camera moves. Face normal ≈ **293°**, tilted ~3° up: halfway between the Sun (346°) and the pad (240° from the panel). So the Sun's mirror lands on the apron. Glint off at < 35 m and after power-on | ranging target for landers on approach, so it faces the landing field |
| **Bootprint trail A** (decal) | guide line **B7** | apron edge (27, 147) → (80, 182), ~63 m at 56° | B6 (sunlight) | the golden Sun rakes in from the left, so the prints cast tiny shadows. The trail ends between the flag and the glint | the crew's daily walk from the lander to the station |
| **Mission flag** (horizontal rod) | quiet texture, awe | (81.5, 188), 8 m west of the habitat | B7 arrival | with the reflector, it frames the trail's end like a front door. Bleached white against the dark habitat | planted when Lumen opened |
| **Fallen survey tripod** | quiet texture | (−3, 160), 38 m west of the lander | B6, from the pad | lies at the blast zone's pebble ridge, legs pointing **away** from the lander | the last landing's blast knocked it over (it reinforces World's blast halo) |
| **Seismic station** (ALSEP-style, dead lamp) | lore scan + **B10 contrast** | (128, 183), 26 m past the workshop | B9, from the habitat | its little lamp is the one light that stays **dark** when the player's base lights up at nightfall | science kit stands clear of the station's dust |
| **Geophone line** (its cable, 3 small sensors) | guide line, after the opening | (128, 183) → (205, 137), 90 m at 121° | B9 | runs straight toward the canyon mouth and **06** | an active-seismic line, laid in a straight run |
| **Scout lander** (Surveyor-style), small crater r ~9 m, 1.5 m deep, tilted ~12° | **curiosity lure, lore scan** | **(56, 27)**: 120 m from the pad at the last-light bearing; 48 m off the B2 lane | **B8–B9, last light** | its **~60 m spidery shadow** reaches up the plain toward the base. Looking SSE from the pad, the three legs stand backlit in the glare. Kept unlit and outside 07's lamp cone in B2, so the shadow is the first look at it | landed 1967 (§3) |
| **Bootprint ring** + short trail (fades after 25 m NW) | quiet texture at the Scout | round (56, 27) | on arrival | 1971 prints circle it; the missing camera and scoop show bare brackets | the 1971 sortie |
| **Sample-tool rack, fallen core tube** | quiet texture | (60, 32), crater's north rim | on arrival | one tube lies downslope, open end out | the 1971 crew cored the crater |
| **Kestrel-3** + furrow debris (exist) | optional lore, west line | crater (−13, 185); furrow (−40, 90) | B2 (rim, left) | the furrow lies right on the apron→relay.1 lane, 74 m out | foreign crash (story v4) |
| **06** (SSR-3 elder, at rest) | lore scan, F# handshake ×2 | micro.06 (265, 84) | after the opening | at the geophone line's end, 80 m on, by the canyon-mouth mast | it stopped there (`07-identity.md`) |
| **relay.1** dead mast (exists) | west lure B11 | (−205, −75) | B5 (from the apron) | B11's signal pillar rises behind it | Link's first target |

**Per-beat schedule (who points):** B0–B5 beacon + cable. B6–B7 glint + trail A. B8 nothing new: the player
builds. B8–B9 the Scout's shadow (optional, the Sun holds until NightReady so the detour costs nothing).
B10 the dark station lamp. B11 the signal pillar west. After: the geophone line east to 06, the furrow west.

**Spacing:** lane B2 (165 m): the Scout (42 m along, 48 m off), the cable tear (84 m), the furrow (100 m, 57 m off).
Lane B7 (~63 m of trail): the glint ahead, then the ruins' front door; the tripod sits beside the base. East (ruins → 06, ~200 m): the station at 33 m, the
geophone end at 125 m, 06 at 200 m. West (apron → relay.1, ~310 m): the furrow at 74 m; the next point is a 0.6
gap, logged below.

## 3. Map (schematic: 1 col ≈ 5 m east, 1 row ≈ 10 m north; the table has exact numbers)
```
                              ^                 Peak (91, 430), red blink
                            F                   F flag   * reflector glint
         K                ..*  R      A =       R Lumen ruins   A seismic station
                        ..               = =    = geophone line -> 06 (265, 84)
           t       L ...                     =  L lander + beacon   t fallen tripod
                 ..:.                           .. trail A (bootprints, B7 line)
               P  :                             P apron / base   K Kestrel-3 crater
                  :
                 :                              : wake cable (B2 line)   x tear + splice box
                 :
                :
    f           :  \                            f Kestrel furrow debris
               x:   \                           \ the Scout's shadow at last light (SSE Sun)
               :    \
               :     \
              :       \
              :       \
             :         Sr                       S Scout lander in its crater   r sample rack
             :
             :
            W                                   W wake + dead post;  relay.1 (-205, -75) off map, WSW
```

## 4. The Scout lander (Argon brief + scan page)
- **Model:** three-legged 1960s soft lander, ~3.3 m tall, foil and white paint gone grey-tan. One footpad half-buried
  downslope, the high-gain dish still on its mast. **Removed:** the TV camera (bare bracket and cut cable ends) and
  the scoop arm (an empty hinge). Generic livery only: `S-3` stencilled on the side. No real logos.
- **Databank:** Machines · scan once (E). One page with the scan's line drawing.

| Key | en | vi |
|---|---|---|
| `scan.scout3.name` | Scout 3 | Tàu thăm dò Scout 3 |
| `scan.scout3.manifest` | SCOUT 3 · SOFT LANDER · LANDED 1967 · CAMERA, SCOOP: REMOVED 1971 | SCOUT 3 · TÀU ĐỔ BỘ MỀM · HẠ CÁNH 1967 · MÁY QUAY, GẦU XÚC: ĐÃ THÁO NĂM 1971 |
| `scan.scout3.note` (Kenji) | Landed here in 1967, two years before anyone walked on the Moon. A crew came over in '71, took its camera and scoop home to see what the Moon does to metal, and left the rest. It got here before any of us. | Nó hạ cánh ở đây năm 1967, hai năm trước khi có người đặt chân lên Mặt Trăng. Năm 71, một tổ bay đi bộ tới, tháo máy quay với gầu xúc mang về Trái Đất xem Mặt Trăng làm gì với kim loại, phần còn lại để nguyên. Nó đến trước tất cả chúng mình. |

Story check: Kenji's voice is a machine note, so it may be warm, but it stays plain. It never mentions or
addresses 07, and the probe gets no feelings.
- **Seismic station page** `[PROPOSAL]`: `SEISMIC STATION · LUMEN · LAST RECORD 28 OCT 1988 · CELL 0%`. The storm
  date fits story v4; this is not a launch, because the crew never left.

## 5. For World / Argon, and gaps
- **World:**
  - place everything in §2;
  - stamp the Scout's small crater (GroundShaping);
  - make the bootprint decal read only in direct sunlight;
  - aim the reflector glint as described;
  - re-add 06 at micro.06, which also gives Rover's elder glance its target;
  - **verify with captures:** the B2 chase in the eclipse (nothing new visible); the pad at B6 (one glint);
    the pad at last light looking SSE (the Scout's shadow and silhouette).
- **Argon:** the Scout lander (§4), its removed-part brackets, a 3-sensor geophone line, and a 1971-style bootprint
  variant (ribbed, older) beside the station-era trail.
- **Gaps (proposed tasks):**
  - a point of interest about 180 m along the west lane (0.6);
  - the station lamp blinks again when power reaches the ruins (0.6, "repairs wake the moon").

## 6. Playtest signal
- At B7, the player drives from the pad to the ruins along trail A without reading the objective line.
- At last light, **at least half** of first-time players turn toward the Scout's shadow within 2 min of noticing it
  and scan it.
- No player stops at a new prop during B0–B5.
