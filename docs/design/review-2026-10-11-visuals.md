# Visual direction review, 2026-10-11: is Lofi Lunar's look modern and polished enough?

> Author: the game design session ("MoonProject · game design"). Critic, not decider: everything below is a
> **proposal** to the Director. Nothing here changes VISION, DESIGN or a feature spec until the Director accepts it.
>
> Judged on real frames, not docs:
> - `MoonProject-wt/world/Logs/world-captures/`: `wake-0-asleep`, `wake-2-windows`, `wake-3-home`,
>   `atmosphere-base_from_150m`, `atmosphere-low_wide_07_small`;
> - `MoonProject-wt/art/Logs/captures/`: `wake-after/night_wake_lift_lift_from_chase` (the closest match to the
>   owner's screenshot), `wake-after/night_wake_lift_base_wide`, `wake-after/night_wake_dark_bay_front`,
>   `p3-rear/night_kitmain_full_kit_chase`;
> - `MoonProject-wt/director/Logs/gameplay-captures/`: `playthrough-16-toward-the-canyon`, `playthrough-02b-salvage-cut`;
> - `MoonProject-wt/ui/Logs/ui-captures/`: `01_title`, `12_pause`;
> - fresh frames from this worktree in `Logs/captures/design-2026-10-11/` (see the level review for the list).
>
> The render setup was read from `Assets/Settings/PC_Renderer.asset`, `PC_RPAsset.asset`,
> `Data/Tuning/WorldSettings.asset` (atmosphere, post) and the shader headers in `Assets/_Project/Shaders/`.

## 1. Verdict

**Not yet.** The *ideas* are modern and on-pillar: a huge faceted Earth, the Peak's red blink, long low-sun shadows,
and the base reading as a small amber cluster at 150 m (`atmosphere-base_from_150m`, `low_wide_07_small`). Pillar 6
already works at long range. The *rendering* reads like a 2016 low-poly Unity scene. One cool key, a cool ambient, a
cool fog and cool ground put nearly every surface in one narrow value band. Nothing is grounded by occlusion, nothing
is separated by a rim, the ground near the camera is one flat value, and the far rim is as crisp as the near ground.

Measured on the captures (luminance, 0–1; "ground flatness" is the share of the lower 45% of the frame within ±0.04
of its most common value):

| Frame | 5th–95th percentile | Ground flatness |
|---|---|---|
| `wake-3-home` (night base from behind 07) | 0.14–0.36 | 0.56 |
| `night_kitmain_full_kit_chase` | 0.13–0.35 | 0.57 |
| `night_wake_lift_lift_from_chase` (owner's frame) | 0.12–0.35 | 0.26 |
| `playthrough-16-toward-the-canyon` | 0.17–0.42 | **0.83** |
| `playthrough-02b-salvage-cut` | 0.14–0.55 | 0.53 |

Ninety per cent of every frame lives in about a quarter of the available range. That is the "flat" the Director
saw. Pillar 6 asks for the opposite: "Pastel flatness reads as a toy; contrast reads as a place."

## 2. The owner's screenshot, claim by claim

| Claim | Verdict | Evidence | Root cause |
|---|---|---|---|
| One ambient value on most faces | **Confirmed** | `night_wake_lift_lift_from_chase`, `wake-3-home`: the lander's shade side, the shelf, the bay walls, 07's back and the ground share one lavender | The key (0.86, 0.85, 1.0) and the trilight ambient are the same hue. The ambient's sky and equator bands are close (0.17/0.155/0.33 vs 0.13/0.115/0.25), so up faces and side faces barely differ. Props use Simple Lit with no specular and no rim, so every face turned from the key gets the same ambient. |
| No contact shadow or AO; props sit like cut-outs | **Confirmed** | The HOME sign, the two stands, the shelf legs and the bay's sill have a long cast shadow but no darkening at the foot | SSAO is on but at Intensity 0.4, Radius **0.3 m** and Direct Lighting Strength 0.25. At a 10–30 m camera distance a 0.3 m radius is two or three pixels, so it is effectively invisible. No prop has baked vertex AO or a darker dust skirt. |
| A blank ground plane around the base | **Confirmed** | 40–55% of each chase frame is one value (`wake-3-home`, `full_kit_chase`) | The base pad is flat by design (`_padRadius` 25 m). Flat-shaded flat triangles share one normal, so they get one value. Pebble scatter is sparse and sub-pixel at chase distance. |
| A busy cluster of small props around the lander | **Confirmed** | `night_wake_lift_lift_from_chase` has about 14 small silhouettes inside 15 m: sign, stands, crates, lamp posts, a lectern, boxes, the board on legs, the bead-lamp cable loop | They all have the same value and the same size class (0.5–1.5 m), so they read as clutter, not as places. The big masses (bay, lander, tower) are fine. |
| (implied) The base doesn't feel warm | **Partly refuted** | Windows, the dock and the pilot halo read, and from 150 m the base is a clear amber cluster | Warmth is dots, not a shape. No warm light reaches the ground, so up close the base sits on cold lavender. |

### Four more problems the owner didn't name
1. **It reads as snow, not the Moon.** Smooth lavender-white dunes, pale cliffs with vertical drapes
   (`playthrough-16`), and bright motes floating everywhere in mid-air (`wake-3-home`: dots in front of the rim) read
   as a winter night on a snowy mountain. The Moon's own signs are missing near the camera:
   - craters at every scale (25 craters across a 640 m basin, none in the near field);
   - crisp crater rims and regolith rubble;
   - near-black shadow cores.
2. **The rim's value hierarchy is inverted.** In `playthrough-16` and `02b` the rim cliffs are the brightest large
   surface, brighter than the near ground. Pillar 6: far things dissolve, and the moon is "mostly deep, cool shadow".
3. **Pale non-living surfaces still read as unlit (ruling 13).** In the dark base these are the brightest values in
   frame, and they mean nothing:
   - the dormant bay shutter (`night_wake_dark_bay_front`);
   - the depot panels (`02b`);
   - the leaning solar sheet and ramp at the lander (`wake-3-home`, left).
4. **The sky's stars look like particles.** Squarish blobs of similar size with bloom, mixed with the motes, read as
   snowfall. The Milky Way is too faint to register.

### UI type
Be Vietnam Pro, the dark rounded card and the amber focus read clean and current (`12_pause`). Two things date it:
- the flat dark overlay behind the pause (modern cozy games blur and soften the world behind a menu);
- the title wordmark (`01_title`) is evenly letter-spaced type with a thin rule. It is tidy but anonymous. See the
  M3-09 draft.

## 3. What makes comparable stylised games read as modern

These are named for technique only. None of them should be copied.
- **Lonely Mountains: Downhill.** The closest match: flat-shaded, untextured low-poly. It reads rich through:
  - strong ambient occlusion in every crease and under every rock;
  - a key and fill that differ in hue *and* value;
  - distance fog in bands that flatten far ridges into calm colour planes;
  - dense small rock scatter that gives the ground scale.
- **Townscaper and Islanders.** Plain flat colours look solid and touchable because soft AO sits under and between
  everything, with soft-edged shadows. Nothing floats.
- **Tunic.** Clean low-poly that gets its polish from:
  - heavy AO and a bright rim on silhouettes;
  - bloom-rich light pools on the ground;
  - tilt-shift depth of field in its framing;
  - ground that always carries pattern (paths, tiles, tufts), so it is never one value.
- **Firewatch.** Atmospheric perspective as graded, stylised fog colour per distance band. Each band is a flatter,
  lighter plane, and the grade shifts with the hour.
- **Dredge.** The reference for "warm points in a cold field". Its lamps throw visible pools on the surfaces around
  them, so safety is a *shape on the ground*, not a dot.
- **Journey.** Dunes come alive through a sparkle term, a bright rim along the dune crests and height haze. The sand
  is never flat.
- **Sable.** Restraint: huge silhouettes, flat colour and few props. Each place is one strong shape.
- **Outer Wilds.** A campfire as home in cold space, and a sun rim that makes small worlds read as round.

The common modern stylised stack is:
1. occlusion and contact grounding;
2. a split light model (key and fill differ in hue and value);
3. a rim for silhouette separation;
4. layered or height fog;
5. ground detail at three scales;
6. light pools from practical lights;
7. restrained, purposeful prop placement;
8. depth of field in cinematic beats;
9. a grade with a real black and a real highlight;
10. UI that blurs the world rather than covering it.

We have bloom, a vignette, grain, long shadows and a gradient sky. We are missing 1–7 and 9.

## 4. Ranked buildable changes

The ranking is by impact on the owner's night-base screenshot per unit of cost. "Owner frame" means the chase view
of the woken base from behind 07.

| # | Change | Box | Expected impact on the owner frame | Cost | Serves |
|---|---|---|---|---|---|
| V1 | **Real grounding.** SSAO Intensity ≈ 1.2, Radius ≈ 1.2 m, Direct Lighting Strength ≈ 0.55, written by World's builder from `PostProcessSettings` (tunable data, not a hand-edited renderer asset). Art bakes vertex AO into its builders: the lowest ~0.4 m of every grounded prop and every concave joint fades toward DustShadow. Each grounded object gets a low dust skirt (a drift ring a shade darker than the pad). | world (SSAO) + art (vertex AO, skirts) | The biggest single change. Every prop, leg and wheel gets a dark foot and stops floating. The lander's underside and the bay's mouth gain depth. | S + M | pillar 4, ruling 13, VISION weathering ("dust drifts piled against the bases") |
| V2 | **Split the light inside the cool family, and add a rim.** Earthlight becomes paler cyan-white (Earth is blue-green, so earthlight is too), about 20% stronger. The ambient turns deeper indigo and about 35% weaker, with a much darker ground band, so side faces fade from lit top to dark foot. Art's lit shader (move `M_LowPoly` off Simple Lit onto a Lofi lit shared with `LofiWeather`) gains a fresnel rim tinted toward the sky horizon, masked off up-facing faces. | world (atmosphere) + art (shader) | Lit faces read cyan-lavender and shade faces deep violet. 07, the lander and the tower get a thin cool edge against the dark ground. The frame's p5–p95 spread roughly doubles. | S + M | pillar 6 "value, not just hue", pillar 4 "silhouette first" |
| V3 | **Warm light pools on the ground.** Every warm point lights a soft pool on the terrain: a larger-range point light, or a cheap additive light-decal disc where light count matters. The pools grow with each power stage: dock pool at Asleep, porch and window spill at Home, bay apron at Bay, deck and lift at Lift. | world (home lighting stages) + art (decal mesh) | The base sits on a warm floor instead of cold lavender. "Home" becomes a shape you drive into, and each tower level visibly widens it. | S–M | pillar 2, pillar 6 "warm points", ruling 11 (home shows progress) |
| V4 | **Ground at three scales.** Macro: keep the patches. Meso: a crater field of a few hundred small craters (1–6 m, crisp one-facet rims, dark one-facet floors), densest on open plains. Micro: the base pad becomes a place: 07's own worn tracks between dock, tower, bay and shelf and out toward the depot and the canyon (static desire paths), the lander's radial landing-blast scar, a few crew bootprint remnants near the ladder, and denser pebbles. | world (+ rover's track material for the paths) | The blank 40–55% of the owner frame becomes readable ground with a story. The level review's dead stretches get crater rhythm. | M | ruling 9 (patchy, never confetti), ruling 12 (abandoned), pillar 6 |
| V5 | **Declutter home into three masses.** Bay and workshop / lander and shelf / tower and dock, with ≥ 4 m of open, lit ground between them. Fold the stands, lectern and crates into the shelf or the bay. Remove small props that have no job in 0.5. Dormant small props sit at low value. Target: ≤ 6 small silhouettes in the chase frame. | art (base layout) + gameplay (station anchors) | The owner frame reads as three places, one job each (M3-16 rule 3), instead of a prop yard. The pads and the cable lights get room to read. | M | pillar 4, M3-16 "every place has one job" |
| V6 | **Atmospheric layers.** A thin basin haze (0–6 m high, slightly lighter violet, densest far away) plus distance desaturation toward the horizon colour, in the shared fog term of `LofiTerrain` and `LofiWeather`. The rim face drops ~25% in value under the haze, but its crest keeps a lit edge. | world | `playthrough-16` stops being a bright wall: near (contrast), mid (haze), rim (flat plane, lit crest), sky. In the owner frame the rim steps back behind the base. | M | pillar 6 "scale and distance" |
| V7 | **Moon, not snow.** Motes only in 07's lamp cone and near warm lights, as VISION already says. Stars: round, a power-law spread of sizes, bloom only on the brightest few, a stronger Milky Way band. The rim's vertical drape striations become horizontal strata with boulder aprons at the foot. The lit dust value drops ~10%. | world (sky, rim, dust) + rover (motes) | No more snowfall read. The sky becomes the most beautiful thing on screen again (pillar 6). | S (motes, stars) + M (strata) | pillar 4, pillar 6 "a living sky" |
| V8 | **Pale-is-a-bug sweep.** Every large non-living pale surface gets its weathered mid value: the dormant bay shutter, the depot panels, the lander's leaning solar sheet and ramp. Pale stays for 07's cream and for labels. | art | The brightest things in the dark base become the warm lights, as pillar 2 intends. | S | ruling 13 |
| V9 | **A grade with a real black and a real highlight.** Contrast 10 → ~20, lift 0.01 → 0, saturation −12 → about −5 (split toning already carries the hue), and colour grading in HDR mode (`m_ColorGradingMode` 0 → 1) so the warm windows grade before tonemapping. Add a per-power-stage grade nudge: a slightly warmer highlight balance as home wakes. | world (post settings) + director (RP asset) | Deeper shadow cores and cleaner warm highlights. The frame finally has a black. | S | pillar 6 "filmic, never noisy" |
| V10 | **Depth of field in the still beats only.** Soft DOF in the wake stargaze, the dock's evening, the lift lookout and the title. A gentle tilt-shift on the lookout makes home read as a small, precious model below. | rover (camera) + world (volume) | No change to the chase frame. Big change to the screenshots people share. | S | pillar 6 "stillness is a reward" |
| V11 | **UI blur-behind.** Pause, title and map blur and slightly desaturate the live frame instead of a flat dim. The amber focus stays. | ui | Menus feel inside the world. A 2026 baseline. | S | anti-goal "walls of UI" |

### What the owner frame should look like after V1–V5 (the target)
- **Sky:** darkest at the top, with round stars of many sizes.
- **Rim:** a hazy, darker plane with one lit crest line.
- **Mid-ground:** crater rhythm and long shadows.
- **The base:** three clear masses on a warm pool of light, each prop with a dark foot.
- **07:** separated from the ground by a thin cool rim.
- **Near ground:** no longer one value. 07's old tracks lead in from the bottom of the frame to the dock.

Measurable acceptance on the fixed capture set (owner frame, `full_kit_chase`, `playthrough-16`, `02b`, base from
150 m):
- the luminance 5th–95th spread is ≥ 0.40 (today about 0.22);
- ground flatness is ≤ 0.30 (today 0.53–0.83);
- the brightest 1% of pixels are warm lights, Earth or 07's cream, never a prop panel.

## 5. Process proposals
- **A fixed "look" capture set**, run before and after every look change by whichever box made it. It covers the
  owner frame, base from 150 m, the canyon drive, the depot cut, the lift lookout and the title. A tiny
  luminance-spread and flatness report goes with it (the numbers above came from a 20-line Python script).
- **Owner screenshots are evidence.** When the owner sends one, the Director adds a matching pose to the set, so the
  fix is verified on the exact frame that was criticised.
