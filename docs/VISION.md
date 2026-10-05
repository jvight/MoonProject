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

## Anti-goals (never ship these)
- Health, oxygen, fuel, damage, enemies, failure screens, timers.
- Pop-in, hard cuts, camera snaps, instant teleports (fade or ease instead).
- Walls of UI. Prefer diegetic feedback (lights, sounds, the rover's own gauges) over HUD.
- Realistic textures, PBR grime, noisy normal maps.
- Loud or harsh sounds. Nothing above a gentle mezzo-piano; no high-frequency fatigue.

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

Rules: max 4 swatches per object, emissive only for things that are *alive* (lamps, scrap, tether, sonar, Earth).

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
