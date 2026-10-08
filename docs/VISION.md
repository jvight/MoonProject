# Lofi Lunar — Creative Vision

> Owner: Director box. Every box reads this before building anything a player will see, hear or touch.
> The original pitch lives in `GDD.md`; this document is how we execute it.

## The one-sentence promise
**A tiny warm rover, alone on a dreaming moon, collecting lost memories of Earth to the sound of a crackling lofi radio — and you never once feel stressed.**

The player should exhale in the first 10 seconds and still be smiling after an hour.

## Feel pillars (use these to settle every argument)

1. **Weightless calm.** Nothing snaps, pops or jerks. Everything eases in and out, floats a little too long,
   and settles with a soft overshoot. Low gravity is the core sensation — bounces are a reward, not a hazard.
2. **Warmth in the cold.** The world is cool (indigo, lavender, slate). Everything that means *safety, home,
   friend* is warm (amber, peach, honey). Light is the language of comfort: the rover's lamp, the base glow,
   the radio tower's halo.
3. **Every touch sings.** Every interaction makes a sound, and every tonal sound is in the same key
   (D major pentatonic: D E F# A B) so the world harmonises with the radio. Collecting things in a row climbs
   a melody.
4. **Chunky, readable, handmade.** Low-poly, flat-shaded, big shapes, few colours per object, no texture noise.
   Silhouette first. If it reads at 50 m, it is right.
5. **Gentle curiosity.** The world invites, never demands. Glints on the horizon, the sonar's rising pitch,
   a blinking light on the distant peak. No timers, no quest log, no fail states, no punishment.
6. **Alone, and at peace (the owner's first priority for look and sound).** The player must *feel* how small 07
   is and how far everything else is, and find that quiet beautiful rather than sad. Every frame and every mix is
   judged against it.
   - **Scale and distance.** The horizon is far and soft: atmospheric falloff fades distant rock toward the sky's
     colour, and a faint glow sits along the horizon line. Near things have contrast; far things dissolve.
   - **Value, not just hue.** The moon is mostly deep, cool shadow with soft-lit planes. A low sun throws long
     shadows that give the ground rhythm. Pastel flatness reads as a toy; contrast reads as a place.
   - **Warm points in a cold field.** 07's lamp, the base windows, a lit relay or Bell's dial are the only warm
     lights. From far away the base is a small amber cluster: home, visible and distant.
   - **A living sky.** Stars vary in size and brightness, a few twinkle slowly, the Milky Way is a soft band, and
     Earth glows with a faint rim. The sky is the most beautiful thing on screen.
   - **Stillness is a reward.** When the player stops, the world gets quieter and wider. The camera slowly drifts
     out to a wide shot with 07 small against the landscape and Earth. Dust motes hang in 07's lamp light.
   - **Sound of solitude.** Near the base the radio is warm and full. Farther out it thins to static, then to
     near-silence: a wide, very soft room tone and 07's own small sounds (servo whirs, ticking metal, the hum of
     its lamp). Distance takes things away gently; coming home gives them back.
   - **Filmic, never noisy.** Gentle colour grading (cool shadows, warm highlights), soft bloom on warm lights, a
     light vignette and very fine grain. Nothing sharp, nothing harsh, nothing busy.

## Anti-goals (never ship these)
- Health, oxygen, fuel, damage, enemies, failure screens, timers.
- Pop-in, hard cuts, camera snaps, instant teleports (fade or ease instead).
- Walls of UI. Prefer diegetic feedback (lights, sounds, the rover's own gauges) over HUD.
- Realistic textures, PBR grime, noisy normal maps.
- Loud or harsh sounds. Nothing above a gentle mezzo-piano; no high-frequency fatigue.

## The rover — "07"
A small, old survey rover that has been alone on the moon for a very long time. It is not cute-bubbly; it is
**gently melancholic machinery** — the kind of tired, faithful device you want to keep company.
- **Form**: a compact six-wheel rocker-bogie chassis (visible rocker arms, chunky faceted wheels) under a rounded,
  boxy body like a 1970s space probe or an old radio cabinet: cream enamel, a worn warm-orange stripe, rivets,
  a hand-painted serial **"07"** on its side.
- **Face**: a slender neck carries a hooded sensor head with **one large round lens-eye** (warm amber glow) under
  a brow/shutter that can lower. Resting state is *half-lidded* — that is where the melancholy lives.
- **Signs of solitude**: one folded solar panel on its back like a single tired wing (a cell missing), a slightly
  bent whip antenna with a faint blinking tip, a patched panel in a mismatched colour, one replacement wheel that
  doesn't match the others.
- **Body language** (procedural, subtle, slow — never cartoony): the head looks at whatever it interacts with
  (tether target, pinged relic, glinting scrap). Left idle, after a few seconds it slowly turns its head up toward
  Earth, the eyelid droops, the solar wing opens a little like a sigh, the eye-glow breathes. Finding something
  makes it perk up: a small head lift, a brighter eye, a quick antenna wiggle. Hard landings get a tiny "oof" squash.
- **Not WALL-E**: no binocular eyes, no treads, no cube body. One lens, six wheels on rockers, a tired wing.

## Music
All music is generated by our own tools (no licensed tracks). The radio is a **lofi station**: a playlist of
original tracks at 68–84 BPM, swung hats, dusty soft drums, warm Rhodes-like keys, round bass, vinyl crackle,
tape wow/flutter — melancholic but warm. Everything is in **D major / B minor** so sonar pings and chimes
(D major pentatonic) always harmonise with whatever is playing. A final, special track is reserved for the
endgame broadcast to Earth. Ro's cassettes are her own mixes, so they may stretch the range (60–90 BPM, e.g. a
64 BPM lullaby) and swap the vinyl crackle for tape or room hiss, while staying in the same key family.

## Product decisions (owner delegated these to the Director, 2026-10-06)
- **Target**: a Steam-quality PC (Windows) release. Playtest builds go out as zipped Windows builds first; the
  store/distribution choice is made in M5, but every system is built to that bar (settings, controller, saves).
- **Languages**: English is the primary language (source text, default, always complete); Vietnamese is a secondary
  language the player can choose in settings. Every player-facing string goes through localization keys from day
  one — no hard-coded UI text, no text inside content assets.
- **Typeface**: Be Vietnam Pro (SIL OFL 1.1) for all UI — full Vietnamese diacritics, calm and friendly.
- **Audio**: all music and SFX are generated by our tools; an owner listening pass refines taste but never blocks.

## Design rulings (Director as game designer — these override GDD.md where they differ)
Each ruling removes friction the original pitch would have caused. Boxes implement them as written.
1. **No loss, ever.** Relics never break, vanish or fall out of reach. Towing is the slice's transport; the M3
   Cargo Bed is a magnetic cradle carrying up to 3 relics that jiggle cosmetically but never fall out (the GDD's
   "bumps make relics bounce out" is cut — it punishes the joyful hops the rover is tuned for).
2. **Generous aim.** The tether picks the best target inside a soft aim cone (~8°) with sticky hover and a visible
   highlight before you press; pixel-precise aiming is never required. Excavation works anywhere within ~5 m of a
   site, and the rover eases to a stop by itself when you hold the beam.
3. **Pings leave a trail.** A sonar answer leaves a soft light pillar on the horizon for ~20 s and 07 keeps glancing
   toward the nearest one, so players navigate by sight and never need to spam the ping.
4. **Progress is kept.** Releasing excavation early keeps progress; a snapped tether leaves the relic right where it
   was; nothing resets.
5. **No grind.** The basin holds at least 2× the scrap the slice's upgrades cost, and each deposited relic also
   gives a scrap gift, so exploring for memories funds the radio tower. Target: all three tower levels in 15–20 min.
6. **Teach by doing.** No tutorial screens. Context prompts (key/button glyph + one word) fade in only near a usable
   thing, only the first few times (remembered in the save), then never again. A pause menu offers resume,
   volumes, look sensitivity, invert Y and quit.
7. **Never stuck.** If 07 is wedged or not progressing for a few seconds while the player is trying to move, it is
   automatically lifted on a soft arc to the nearest open, drivable spot and set down — no prompt, no penalty, no
   teleport flash.
8. **The first frame is the hook.** 07 spawns at the base facing bearing 355° so Earth and The Peak (with its slow
   red beacon) share the opening frame; the camera pitches down no more than ~6°. 07 starts with its eye closed and
   wakes as the radio crackles on — the first five seconds should make people say "aww".
9. **Ground variation is patchy, never confetti.** Colour changes on the floor follow low-frequency patches and
   facet tilt; isolated bright triangles on flat ground read as paper scraps and are not allowed.
10. **Gates are real, exits are one-way.** An ability gate must actually need the ability, not just shortcut a
    drivable path. Every gated region has a way back that needs no ability: a one-way step down of at most 2.5 m,
    landing more gently than the basin's play ramps. It is visible from the basin side as a promise, and it is never
    climbable as a way in. Trying the gate without the ability may fail, but it never traps: any trough or pit 07
    can fall into has a ≤ 20° side to drive out.
11. **Every upgrade shows.** Progress must be visible on 07 or at home, readable from the default camera and at 30 m.
    07's look moves from tired to cared-for: its signs of solitude heal and sturdy expedition kit appears. Rugged
    and capable, never menacing; the gentle one-eyed face never changes. See `docs/DESIGN.md` "Visible progression".
12. **Built for a rover, abandoned for decades.** Everything 07 uses is made for wheels: ramps, cable lifts,
    rover-height hatches, docking and charging pads, never stairs or ladders on 07's path. Human-scale things (the
    crew's ladder, a chair, a mug) stay only as remnants that tell of their absence. Everything starts looking left
    alone for a very long time, and restoring it visibly cleans and repairs it (ruling 11).

## World concept
The playable space is the floor of a **vast ancient crater basin** (~600 m across for the vertical slice).
The crater rim is a natural, beautiful boundary: rolling lavender dunes rise into jagged rim mountains.
**The Peak** — the highest point of the rim — carries a broken satellite dish with a slowly blinking red light,
visible from the very first frame. That is the endgame, and the player will look at it for the whole game.

**Earth** hangs huge and low in the sky (blue-green, softly glowing): emotional anchor and compass.
The sky is a deep indigo-to-violet gradient with a dense starfield, a faint milky-way band, and the occasional
slow shooting star.

**Home base** is an abandoned lander in the centre of the basin: warm windows, a radio tower, museum shelves.
Driving back towards its glow should feel like coming home on a winter night.

## Palette (art bible — exact swatches live in code, `MoonProject.Art.PaletteSwatch`)
| Role | Hex | Notes |
|---|---|---|
| Sky top | `#0B0E2A` | near-black indigo |
| Sky horizon | `#3B2A6B` | violet glow band |
| Moon dust light | `#A7A9CC` | lit dune faces |
| Moon dust mid | `#7C7FAE` | default ground |
| Moon dust shadow | `#45477A` | crater walls, shade |
| Rock | `#5A5480` / `#6E6894` | props, rim mountains |
| Warm lamp | `#FFB547` | rover lamp, base windows |
| Warm accent | `#FF8A5B` | rover paint, cosy details |
| Cream | `#F4E6C8` | rover body, labels |
| Tech glow | `#5FF3FF` | sonar, tether, scrap glow |
| Earth | `#3FA7D6` / `#7BD389` | ocean / land |
| Biolum (eclipse) | `#3CFFC2` / `#FF5FD2` | late-game magic only |
| Alert-soft | `#FF5A6E` | the peak's blinking light only |

Rules: max 4 swatches per object (the terrain is the exception: up to 6, the dust family plus Charcoal, which is kept for depth such as the chasm trough), emissive only for things that are *alive* (lamps, scrap, tether, sonar, Earth).
Weathering ("left alone for decades"), done the low-poly way and never as texture grime:
- faded paint swatches and rust patches as flat facets, with rust streaks running down from bolts and seams;
- dust drifts piled against the bases of things, and dust caps on top faces;
- dents (vertex offsets), slack or snapped cables, tilted signs, missing panels, half-buried debris;
- big, readable shapes: weathering must read at 30 m and never turn into noise.
Restored things lose their dust caps, get a patch of fresh paint and light up; the contrast is the reward.

## Audio direction
- **The radio** plays lofi. Near base the signal is clear; far away it drifts into warm static and low-pass haze.
  Upgrading the radio tower widens the clear zone — the player literally *hears* progress.
- **Rover**: soft electric hum that rises with speed, crunchy-soft dust under wheels (ASMR), springy suspension
  creaks on bounces, a gentle *whump* on landing scaled by impact.
- **Sonar**: a kalimba ping in key; relics answer with a higher, shimmering note; closer = faster.
- **Pickups**: glassy chime, each consecutive pickup within 2 s climbs one step of the pentatonic scale.
- **Tether**: a warm hum with gentle wobble, a soft *pluck* when it attaches, a breathy *fwip* when it lets go.
- Silence is a feature: ambience is sparse wind-less hush with distant, slow tones.

## Camera direction
- Floaty drone: high positional damping, slower rotational damping, auto-recenter behind the rover after idle.
- Slight FOV widening with speed; gentle lift when going downhill; never clips into terrain.
- Cinematic moments (relic surfacing, tower upgrade) use slow, eased blends — never cuts.

## The feel checklist (Director signs off a feature only when all are true)
- [ ] Starts and stops with easing; no frame-one snaps.
- [ ] Has a sound, in key, at a comfortable level.
- [ ] Has a visual response (light, particle, motion) that matches the sound's timing.
- [ ] Reads from the default camera distance.
- [ ] Can't frustrate: there is always a gentle recovery (auto-right, soft snap, retry for free).
- [ ] Runs at 60 fps on the target PC without per-frame GC allocations.
