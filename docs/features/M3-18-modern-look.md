# M3-18: A modern look, and a basin that reads (0.5)

> Owner: Director. Source: `docs/design/review-2026-10-11-visuals.md` (V1–V11) and
> `docs/design/review-2026-10-11-level.md` (L1–L9). The owner asked on 2026-10-11 whether the look is modern enough.
> The answer, measured on captures, is "not yet": 90% of every frame sits in about a quarter of the value range, the
> ground is one flat value, nothing is grounded, and the basin reads as snow rather than the Moon. VISION's rules stay
> as they are (low-poly, flat-shaded, palette-driven, no texture noise, pillar 6). This pass changes how we light,
> grade and dress the world, not what it is.

## Adopted now
| # | Change | Box |
|---|---|---|
| V1 | Real grounding: SSAO about 1.2 intensity and 1.2 m radius, written by World's builder from tuning data; vertex AO on the lowest ~0.4 m of grounded props and concave joints; a low dust skirt under each grounded object | world (SSAO), art (vertex AO, skirts) |
| V2 | Split the light inside the cool family: paler cyan-white earthlight, a deeper and weaker indigo ambient with a dark ground band. A Lofi lit shader replaces Simple Lit on `M_LowPoly`, with a fresnel rim tinted toward the horizon and masked off up-facing faces | world (atmosphere), art (shader) |
| V3 | Warm light pools on the ground under every warm point, growing with each power stage (dock at Asleep, porch and windows at Home, bay apron at Bay, deck and lift at Lift) | world (stages), art (decal disc) |
| V4 | Ground at three scales: a crater field of small crisp craters (1–6 m) on open plains; 07's worn tracks between home's places and out toward the depot and the canyon; the lander's landing-blast scar; a few crew bootprints near the ladder | world (rover's track material for the paths) |
| V6 | Atmospheric layers: a thin basin haze and distance desaturation; the rim face drops ~25% in value and keeps a lit crest | world |
| V7 | Moon, not snow: motes only in 07's lamp cone and near warm lights; round stars with a spread of sizes, bloom only on the brightest few, and a stronger Milky Way; horizontal strata and boulder aprons on the rim; lit dust ~10% darker | world (sky, rim, dust), rover (motes) |
| V8 | Pale is a bug: the dormant bay shutter, the depot panels, the leaning solar sheet and the ramp get weathered mid values. Pale stays only for 07's cream and labels | art |
| V9 | A grade with a real black and a real highlight: contrast ~20, no lift, saturation about −5, grading in HDR mode, and a slightly warmer highlight balance per power stage | world (post settings; the HDR grading switch in the URP asset through its builder) |
| V11 | Menus blur and soften the live frame instead of a flat dim | ui |
| L1 | Every salvage site gets one tall landmark (≥ 8 m) with a dim warm pilot once its heart is found | art |
| L2 | The crew's road to the canyon: route posts with reflectors every ~45 m on bearing 72° | art (post), world (placement) |
| L4 | The chasm reads from the basin: a broken crew cable bridge, with two anchor posts on the near lip, a snapped cable in the trough and the far anchor lit warm | art (meshes), world (placement) |
| L5 | A real lookout: the camera rises ~14 m above the deck, pulled back, with a slow settle; unvisited places in view answer once with a faint blink | rover (with the lift slice), world (the blink) |
| L6 | Home never shrinks below a readable warm dot at 300 m (the Peak beacon's minimum-size halo on the tower's top light) | world |
| L7 | A mid-west remnant: the crew's antenna farm, 3–4 toppled dish frames at about 270°/120 m | art (meshes), world (anchor and placement) |

## Later in 0.5
- **One layout pass, after gameplay's Playthrough follows steps 0–8.** It moves stations once and re-runs the
  Playthrough once:
  - L3: move the garage to 228°/140 m;
  - L8: Tilly's crater at 60–95°, 80–110 m;
  - L9: move the bay ~4 m south;
  - V5: declutter home into three masses, with at most 6 small silhouettes in the chase frame.
- **V10, depth of field in the still beats only.** It comes with the cutscene catalogue (story & flow session).

## Process
- **A fixed "look" capture set.** It covers:
  - the owner frame (the woken base at night from behind 07);
  - the base from 150 m;
  - the canyon drive;
  - the depot cut;
  - the lift lookout;
  - the title.

  The tools box builds the set and a luminance report; the box that changes the look runs it before and after.
- **Owner screenshots are evidence.** When the owner sends a frame, the Director adds a matching pose to the set.

## Acceptance (on the fixed capture set)
- The luminance spread between the 5th and 95th percentiles is ≥ 0.40 (today about 0.22).
- Ground flatness is ≤ 0.30 (today 0.53–0.83). Flatness is the share of the lower 45% of the frame within ±0.04 of
  its most common value.
- The brightest 1% of pixels are warm lights, Earth or 07's cream, never a prop panel.
- Every grounded prop has a dark foot in the owner frame.
- The rim is darker than the near ground in `playthrough-16`.
- 60 fps on the target PC holds (SSAO and pools within the performance budget in `docs/ARCHITECTURE.md`).
