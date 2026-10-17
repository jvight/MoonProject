# Lunarion — Creative Vision

> Owner: Director box. Every box reads this before building anything a player will see, hear or touch.
> **The game is called Lunarion** (owner, 2026-10-11; it was "Lofi Lunar"). Internal names (the MoonProject
> namespaces, the Lofi* shaders) stay as they are.
> The original pitch lives in `GDD.md`; this document is how we execute it.

## The one-sentence promise
**A small, tired rover wakes on a dark moon, rebuilds itself and its station piece by piece, and pushes its battery's
reach a little farther every night until it can call Earth. The moon is calm to roam, but it is not safe: 07 can wear
down and die (ruling 17).**

> Since 2026-10-11 the game's design is `docs/design/gdd-v3.md` (approved by the owner). Two founding decisions:
> **progression first** (07's growth: upgrade, unlock, craft new tech, go farther; the crew's memories are seasoning)
> and **the battery is fuel** (07 returns to charge; upgrades extend its range), kept gentle by ruling 15.

The player should exhale in the first 10 seconds and still be smiling after an hour.

## Feel pillars (use these to settle every argument)

1. **Grow, and go farther.** Every trip leaves 07 or home more capable, and every upgrade opens a place, a tool or
   more range. You can see how far you've come: on 07, at home and on the chart (rulings 11 and 15).
2. **Weightless calm.** Nothing snaps, pops or jerks. Everything eases in and out, floats a little too long,
   and settles with a soft overshoot. Low gravity is the core sensation — bounces are a reward, not a hazard.
   The battery is range, not the danger: an empty battery means a slow, quiet crawl home with a friend's light
   ahead (ruling 15). Danger comes from hard impacts, the world's hazards and the Reclaimers (ruling 17).
3. **Free to roam, gently curious.** A few main recovery targets give the game its direction and show their progress
   at home; nothing else is listed. Everything else (sites, finds, friends, secrets) is found by roaming, in any order.
   The world invites, never demands: glints on the horizon, the sonar's rising pitch, a blinking light on the distant
   peak. No timers and no quest log. Danger is real but always readable and avoidable (ruling 17). The battery
   measures driving and work, never minutes.
4. **Warmth in the cold.** The world is cool (indigo, lavender, slate). Everything that means *safety, home,
   friend* is warm (amber, peach, honey). Light is the language of comfort: the rover's lamp, the base glow,
   the radio tower's halo. Chargers, home and friends are the warm lights.
5. **Every touch sings, and every piece reads.** Every interaction makes a sound, and every tonal sound is in the same
   key (D major pentatonic: D E F# A B) so the world harmonises with the radio. Collecting things in a row climbs a
   melody. Chunky, readable, handmade: low-poly, flat-shaded, big shapes, few colours per object, no texture noise.
   Silhouette first: every kit piece and structure reads at 30 m, and 07's single gentle lens never changes.
6. **Alone, and at peace (the owner's first priority for look and sound).** The player must *feel* how small 07
   is and how far everything else is, and find that quiet beautiful rather than sad. Every frame and every mix is
   judged against it.
   - **Scale and distance.** The horizon is far and soft: atmospheric falloff fades distant rock toward the sky's
     colour, and a faint glow sits along the horizon line. Near things have contrast; far things dissolve.
   - **Value, not just hue.** The moon is mostly deep, cool shadow with soft-lit planes. A low sun throws long
     shadows that give the ground rhythm. Lumen Station sits near the Moon's pole: the Sun skims the horizon and
     circles the sky once per lunar day (2 hours of play), so shadows are always long and slowly turn. High ground is
     nearly always sunlit, the deepest craters never are, and Earth hangs fixed and low, its cool earthshine lighting
     the world but never charging anything. Pastel flatness reads as a toy; contrast reads as a place.
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
- Timers, oxygen or hunger meters, and combat (07 has no weapons). Deaths the player could not read coming or
  could not avoid. 07's battery is its range (ruling 15); its wear and death follow ruling 17.
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
  bent whip antenna whose faint tip blinks the station's old call sign (three short, one long; it pays off in the
  finale), a patched panel in a mismatched colour, one replacement wheel that
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
3. **Listening leaves a trail.** An answer leaves a soft light pillar on the horizon for ~20 s, 07 keeps glancing
   toward the nearest one, and 07 listens by itself when it rests, so players navigate by sight and never need a
   button for it.
4. **Progress is kept.** Releasing excavation early keeps progress; a snapped tether leaves the relic right where it
   was; nothing resets.
5. **No grind.** The basin holds at least 2× the scrap the slice's upgrades cost, and each deposited relic also
   gives a scrap gift, so exploring for memories funds the radio tower. Target: each version's steps need about half of its materials, and the tower's three levels land across the first hour (M3-16).
6. **Teach by doing.** No tutorial screens. Context prompts (key/button glyph + one word) fade in only near a usable
   thing, only the first few times (remembered in the save), then never again. A pause menu offers resume,
   volumes, look sensitivity, invert Y and quit.
7. **Never stuck.** If 07 is wedged or not progressing for a few seconds while the player is trying to move, it is
   automatically lifted on a soft arc to the nearest open, drivable spot and set down — no prompt, no penalty, no
   teleport flash. Never stranded: at empty, 07 crawls slowly and quietly to the nearest charger, Tilly's lamp leading
   it, losing nothing.
8. **The first look is the hook** (owner, 2026-10-11).
   - A new game opens in darkness on the radio's crackle, then through 07's own eye as it opens: the lid lifts in
     stages, the lens hunts for focus, and 07 looks around at the sleeping home, the tower's pilot blink, The Peak's
     red light and, last, Earth.
   - The camera then eases out of the lens to a front three-quarter shot of 07's eye looking up at Earth, and on to
     the chase frame: 07 on its dock facing bearing 355°, Earth and The Peak (with its slow red beacon) sharing the
     frame, the camera pitched down no more than ~6°.
   - Only the dock, 07's eye and the tower's pilot are warm.
   - Any drive input reaches the chase frame within ~4 s; control is never held. A loaded game opens with the short
     wake.
   - The first five seconds should make people lean in; 07's first blink should make them say "aww".
   Storyboard: `docs/design/cutscenes.md` §1.
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
13. **True scale, gently exaggerated.** The crew's world is human scale (a person is 1.75 m; doors, ladders, benches
    and shelves fit people), and 07 is a small rover inside it: 1.53 m to the top of its CRT head (1.59 m to the
    antenna tip), 2.0 m long with the front bumper (1.76 m wheel to wheel), 1.47 m wide, 1.22 m track (owner +
    Director, 2026-10-17; every number and every door/pad/bay rule is in `docs/design/scale.md`, enforced by the
    bounds contracts). Small personal things are at most about 2x their real size, so a duck stays a duck and a
    teapot stays a teapot, and never bigger than about 0.5 m. Readability comes from glow, outline, pillars and
    sound, never from giant props. Anything that looks like a rendering error (an unlit-looking pale part, a
    mismatched scale) is a bug, even when it was meant as character: intentional wear must read as intentional at
    first glance.
14. **07 has no hands.** Everything 07 does, it does with its wheels, its one beam (lift, tow, cut, dig, stitch,
    nudge), its head (look, scan, glow) or its friends. Everything else is done *for* it by machines the crew built
    for a rover: Kenji's Rover Bay with gantry arms that fit kit, hoppers 07 feeds with its beam, service ports at
    rover height, a cable lift, a charging dock. No human action is ever implied: no pressing buttons, carrying,
    climbing or using a workbench. Every interaction must answer "how does 07 physically do this?" on screen.
15. **The battery is range, not the danger** (owner, 2026-10-11; `docs/design/gdd-v3.md` §3, spec M3-20).
    - An empty battery never kills 07 and never costs cargo. Death comes only through wear (ruling 17).
    - A verb in progress always completes: a cut, stitch, dig, feed or tow that starts on the last charge finishes on
      the reserve, and the crawl starts after. A leap that can't be paid for never charges: the coils fizzle softly at
      the lip. A leap never fails mid-air.
    - Never stranded: at empty, 07 drops into a quiet low-power crawl that never runs out (Kenji's sealed reserve
      cell), and once Tilly is home she flies out by herself and leads with her lamp.
    - Never far from a charger: no point on a trip is more than 300 m from one (the dock, a lit mast's pad, later a
      built pylon), so the longest crawl takes about two minutes.
    - Standing still never drains. In light, the repaired solar cell trickles back up to one cell, never more: you can
      always get home, but waiting is never a strategy.
    - Warnings are gentle and early, with no red and no alarm, and a spoken hint names only home or a charger the
      player has lit.
    - A crawl is a pillar-6 moment: the camera eases wider, the radio thins, and the charger's pillar sits ahead.
16. **A few main targets, everything else free** (owner, 2026-10-11; spec M3-21). The game's only directed part is
    four recovery targets on the lab's recovery panel: Power, Link, Dish, Answer. Sites, finds, kits, friends, tapes,
    logs and secrets are found by roaming in any order, and there is no list of them anywhere.
17. **07 can wear down and die** (owner, 2026-10-17; it replaces the old "no death, no fail states" rule; design in
    `docs/design/wear-and-reclaimers.md`).
    - **Wear.** 07 has a wear meter beside its battery. Wear comes from:
      - hard impacts above a speed threshold against rock or metal;
      - the regions' hazards;
      - the Reclaimers, Departure-era machines that recycle old robots.
      Low-gravity hops, landings on regolith and gentle bumps never cost wear: bounces stay a reward.
    - **Readable before it hurts.** A rising creak above cruise speed, then dents, a bent antenna, a flickering lamp, a
      wobbly wheel and 07's wince with a short chirp. The wear gauge shows only when it isn't full. Low wear softens 07
      (a lower top speed, slower scans, no leap) before it kills.
    - **At zero, 07 dies.** The default consequence:
      - 07 restarts at its last dock or Service Gantry with its kits and progress;
      - the hold's cargo stays at the wreck site as a beacon to recover.
      The exact rules are in the design doc.
    - **Repair is physical** (ruling 14): a crafted Patch Kit 07 applies itself with its weld beam, and the Service
      Gantry, whose arms rebuild 07 while it stands on the pad. Repairs visibly restore 07 (ruling 11).
    - **Danger is regional.** The home basin has no Reclaimers. Every threat has a readable tell, a way to avoid it, and
      a learn-then-overcome arc (scan, craft, reprogram).
    - **New Game modes:** Survival (wear and death, the default) and Calm (no wear, no death).

## World concept
The playable space is the floor of a **vast ancient crater basin** (~600 m across for the vertical slice).
The crater rim is a natural, beautiful boundary: rolling lavender dunes rise into jagged rim mountains.
**The Peak** — the highest point of the rim — carries a broken satellite dish with a slowly blinking red light,
visible from the very first frame. That is the endgame, and the player will look at it for the whole game.

**Earth** hangs huge and low in the sky (blue-green, softly glowing): emotional anchor and compass.

**Light is physical.** Sunlight (warm-white, low) charges solar panels and 07's wing; earthshine (cool, from Earth's
fixed place) only lights. Places pass between a sunlit phase and an earthlit phase as the Sun circles; the earthlit
phase is the lavender night the game is known for. The terminator, where the two meet, is where dust glows.
The sky is a deep indigo-to-violet gradient with a dense starfield, a faint milky-way band, and the occasional
slow shooting star.

**Home base** is an abandoned habitat lander in the centre of the basin: the crew's lab inside (reached by the belly
lift), Kenji's Rover Bay and the radio tower beside it, 07's charging dock in front. Driving back towards its glow
should feel like coming home on a winter night.

## Palette (art bible — exact swatches live in code, `MoonProject.Art.PaletteSwatch`)
| Role | Hex | Notes |
|---|---|---|
| Sky top | `#0B0E2A` | near-black indigo |
| Sky horizon | `#3B2A6B` | violet glow band |
| Moon dust light | `#A6A7AB` | lit dune faces |
| Moon dust mid | `#787A80` | default ground |
| Moon dust shadow | `#3E4048` | crater walls, shade |
| Rock | `#525359` / `#6C6D72` | props, rim mountains |
| Warm lamp | `#FFB547` | rover lamp, base windows |
| Warm accent | `#FF8A5B` | rover paint, cosy details |
| Cream | `#F4E6C8` | rover body, labels |
| Tech glow | `#5FF3FF` | sonar, tether, scrap glow |
| Earth | `#3FA7D6` / `#7BD389` | ocean / land |
| Biolum (eclipse) | `#3CFFC2` / `#FF5FD2` | late-game magic only |
| Alert-soft | `#FF5A6E` | the peak's blinking light only |

Swatches are authored for the earthlit phase (look A "Moon grey", owner pick 2026-10-13: near-neutral regolith; the sunlit phase is look C "Low sun"). The sunlit phase warms highlights and rim light (it never changes the
swatches themselves), and permanently shadowed places never get it.

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
- **Listening**: a soft kalimba ring in key when 07 rests; relics answer with a higher, shimmering note; closer = faster.
- **Pickups**: glassy chime, each consecutive pickup within 2 s climbs one step of the pentatonic scale.
- **Tether**: a warm hum with gentle wobble, a soft *pluck* when it attaches, a breathy *fwip* when it lets go.
- Silence is a feature: ambience is sparse wind-less hush with distant, slow tones.

## Camera direction
- Floaty drone: high positional damping, slower rotational damping. The view holds its world direction; it recenters
  slowly, only while driving forward after 5 s without look input.
- Slight FOV widening with speed; gentle lift when going downhill; never clips into terrain.
- Cinematic moments (relic surfacing, tower upgrade) use slow, eased blends — never cuts.
- 07's eye is the only first-person view in the game: the opening, and the finale's answer. Everything else is the
  floaty third-person drone. The catalogue of moments is `docs/design/cutscenes.md`.

## The feel checklist (Director signs off a feature only when all are true)
- [ ] Starts and stops with easing; no frame-one snaps.
- [ ] Has a sound, in key, at a comfortable level.
- [ ] Has a visual response (light, particle, motion) that matches the sound's timing.
- [ ] Reads from the default camera distance.
- [ ] Can't frustrate: there is always a gentle recovery (auto-right, soft snap, retry for free).
- [ ] Runs at 60 fps on the target PC without per-frame GC allocations.
