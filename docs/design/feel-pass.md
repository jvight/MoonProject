# 0.5.2 "Satisfying": the feel pass (design audit and spec)

> Author: the game design session. Proposal for the owner's approval; nothing is built until they approve.
> The owner, after playing 0.5: the moments aren't satisfying enough. Cutting has no punch. There should be a visible
> charge-up, energy condensing at the emitter, *before* the laser fires. "Rất nhiều thứ phải cải thiện": every verb
> should feel as good as it can.
>
> "Now" was read from the code on main (212af220), not from the docs:
> - salvage: `SalvageField`, `CutSparks`, `SalvageTuning.asset`, `Audio/CutBeamModel`;
> - movement and tools: `Rover/Driving/HoverJump`, `Gameplay/Tether/TetherSystem`, `Gameplay/Sonar/SonarSystem`;
> - home and charging: `Gameplay/Base/ChargingDock`, `Gameplay/Battery/BatteryField`;
> - the lab and the bay: `Gameplay/Lift/LabReader`, `LabInterior`, `Rover/Rig/LiftRide`, `Rover/Rig/BayFitting`;
> - audio cues: `Audio/AudioCueIds`.
>
> Rules that bind everything below:
> - VISION's feel checklist and pillar 6;
> - first-hour-v2 §7.3: **no camera shake anywhere**, never roll, moments blend ≥ 1.2 s, and the Gentle camera is
>   respected;
> - ruling 14: 07 has no hands;
> - D major pentatonic;
> - nothing above mezzo-piano.

## 1. What makes cutting and building satisfying elsewhere, adapted to a calm game
| Game | What it does | Our rule |
|---|---|---|
| **Hardspace: Shipbreaker** | The cut is a visible *seam* on the material, hot where the tool is and cooling behind it. Separation is physical: the piece pops free with a little release energy and drifts. Every material answers differently. Each piece has a destination. | **R1 Contact leaves a mark** (the seam). **R2 Separation is physical** (jolt, tumble). **R3 Every piece goes somewhere you can see.** |
| **Astroneer** | Collecting is a visible stream into a container that bulges and pops. Things snap into slots with a bounce. Printing builds the object up with a sweep. Everything has a small, friendly "pop". | **R4 Rewards travel to 07 and land with a bounce.** **R5 Building is a sweep you watch.** |
| **Subnautica** | The scanner has a beam, a progress readout and a completion chime. The fabricator builds with a laser sweeping layers. Tools hum before and after use. | **R6 Anticipation is always visible and audible** (charge, hum-up). |
| **Satisfactory** | Building: a hologram, materials fly in, the structure assembles with a sweep and a puff. Counters count. | **R7 Counters count** (roll, never jump). |
| **Death Stranding** | Weight lives on the body: balance, lean, load. Attach and detach sounds are precise and mechanical. Delivery has a closure beat. | **R8 Weight shows on 07's body, not on the camera.** **R9 Every action has a closing beat.** |

Our calm adaptation:
- **R10 Satisfying, not loud.** At most three sound layers per beat, all in key, peaks at mezzo-piano. No screen
  shake. Impact is carried by 07's body, light and a short rumble.
- **R11 Vacuum honesty.** No smoke, no billowing dust, no air drag. Sparks, slag and dust fly in clean ballistic arcs
  under 1/6 g (1.62 m/s²), fall, and lie glowing or settle. Sound is what 07's chassis feels: low, close and muffled.
  The emitter's own hum is 07's.
- **R12 Ease everything.** Every beat has an ease-in and an ease-out. Nothing lasts exactly one frame, except the
  ignition flash's first 60 ms.

## 2. The verbs: now, missing, target
Each target is a beat sequence: **anticipation → action → impact → follow-through → reward**. Times in ms.

Layers per row:
- **Body:** 07's body language.
- **VFX** and **Light.**
- **Audio:** pentatonic steps D E F# A B.
- **Cam:** never shake.
- **UI.**
- **Rumble:** gamepad motors, 0–1. Today **no rumble exists anywhere in the code**; every rumble line is new.

### 2.1 Cut a salvage piece: P0 (fully specified in §3)
- **Now:**
  - 07 eases to a stop and the beam *starts at once*.
  - 2–4 s of cutting with warm sparks: a soft cone of 0.02–0.05 m streaks at 0.6–1.8 m/s, ~26/s, which reads as
    gentle drizzle.
  - A singing tone climbs the pentatonic, and the piece pops (0.45) and tumbles (35°) for 0.7 s.
  - It folds into a bundle and spirals into 07 with a flash and the site's melody step.
- **Missing:**
  - any anticipation (the owner's point);
  - an ignition moment;
  - a hot core to the beam;
  - a mark on the material, so progress kept on release is invisible;
  - weight in the sparks;
  - a "crack" at the break;
  - any body response;
  - a stock count that *counts*;
  - rumble.

### 2.2 Piece breaks free, bundles, collects into stock: P0 (part of §3)
- **Now:** pop, tumble, fold, spiral, flash, chime, and the stock grows.
- **Missing:** a release jolt; a slow enough tumble to read; a bundle "tie-off" beat; 07 receiving it; a counter that
  rolls.

### 2.3 Drive: start, stop, turn (P1)
- **Now:** the hum rises with speed; dust crunch, suspension creak and tracks. Start and stop are eased by the
  controller.
- **Missing:** body anticipation at start, a settle at stop, visible rocker articulation in turns, dust arcs, rumble.

| Beat | Target |
|---|---|
| Start (0–250) | **Body:** 1.5° squat back with a servo whine. **VFX:** the rear wheels kick two small ballistic dust arcs. **Audio:** the hum swells from its idle note (D) up a fifth (A) over 400. **Rumble:** 0.05 swell. |
| Turn | **Body:** 1–2° roll outward; the rocker arms visibly take up the terrain. **Cam:** unchanged (45°/s follow rule). |
| Stop (0–350) | **Body:** 2° nose dip with one soft overshoot back (spring, damping ~0.6), a servo tick and a metal tick 300 later. **VFX:** a forward dust spray that arcs and falls. **Audio:** the hum falls to D. **Rumble:** 0.06 for 120. |
| Terrain | **Rumble:** a 0.02–0.06 texture by speed and slope, off under 0.5 m/s. |

### 2.4 Leap (Hover-Jump) (P1)
- **Now:** the charge reports steps; jump charge, coil twang, air wind, landing thump and coil pop. A refused leap
  never charges (good).
- **Missing:** a crouch, coil glow per step, a launch blast, an apex hush, landing dust.

| Beat | Target |
|---|---|
| Charge (per step) | **Body:** crouch 2 cm per step (to 8 cm). **Light:** the coils glow one notch per step. **Audio:** one pentatonic step per step (D E F# A). **Rumble:** 0.04 + 0.03 per step. |
| Launch (0–150) | **VFX:** a ring of dust blasts outward in flat ballistic arcs and a coil flash (120). **Body:** 07 stretches up 3%. **Audio:** coil twang on B. **Rumble:** 0.25 for 90. |
| Apex | **Audio:** the radio ducks 2 dB; replace the "air wind" cue (there is no wind in vacuum) with 07's own servo whirr and a metal tick. **Cam:** the existing leap pull-back, halved under Gentle. |
| Landing | **Body:** squash scaled by impact (≤ 6%) with one rebound. **VFX:** a dust ring that arcs and settles in ~1.2 s. **Audio:** the thump and the coil pop. **Rumble:** 0.1–0.3 for 120, by impact. |

### 2.5 Tow and tether (P1)
- **Now:** highlight, latch, a PD spring float, hum, release and snap.
- **Missing:** the reach, the lift-off weight, tension on 07.

| Beat | Target |
|---|---|
| Reach (0–150) | A cyan pulse travels from the brass ring to the target. The target's highlight brightens. |
| Latch (150–250) | A small flash on the object. Tether-attach pluck on A. **Rumble:** 0.08 for 60. |
| Lift (250–550) | The object rises with an ease. **Dust falls off it in arcs (R11).** 07 leans back by mass (≤ 2°). |
| Hold | The hum's pitch tracks tension. **Rumble:** 0.02–0.08 by tension. |
| Release | The object settles with a dust puff that arcs. A breathy "fwip". 07's lean recovers with one overshoot. |

### 2.6 Listening (P2)
- **Now:** after 2 s still, a 5 s ring to 140 m, a kalimba note, pillars, and the head turns.
- **Missing:** the antenna's anticipation, and an answer you feel.
- **Target:**
  - **Anticipation (0–400):** the antenna lifts and the head tilts 6° ("listening").
  - The ring rolls.
  - **Each answer:** its pillar rises with its own note, nearest first, climbing. The head turns to the nearest.
    **Rumble:** 0.03 for 40 per answer.

### 2.7 Charging at the dock and at masts (P1)
- **Now:** a full charge in 3–4 s, cell notes, and the dock glow breathes. A mast charges at walking pace.
- **Missing:** a plug-in moment, the gauge lamps lighting one by one on 07, and relief.
- **Target:**
  - **Settle (0–300):** the dock clamps with a soft "clunk", 07 dips 2 cm, and a 100 ms connection spark.
  - **Fill:** each cell's flank lamp lights with a tiny pop on its note (D E F# A…). **Rumble:** 0.04 per cell.
  - **Full:** 07 "sighs": the wing opens 10°, the eye brightens, the hum warms.
  - **At a mast:** the pad ring lights clockwise as 07 rolls on, and the mast lamp pours down.

### 2.8 Belly lift into the lander (P1)
- **Now:** guides ease 07 to the centre, the platform rises with 07 under a kinematic pose, and the winch sounds.
- **Missing:** the "entering home" beat.
- **Target:**
  - **Settle (0–600):** clamps clunk, and the rim lamps chase upward.
  - **Hatch opens (600–1200):** warm light spills down onto 07 (the light beat), and a hatch chord on A.
  - **Rise:** eased ~4 s.
  - **Arrival:** room tone and the close radio fade in over 800, and 07's head lifts.

### 2.9 Crafting in the lab (reader decode, plotter) (P2)
- **Now:** the beam feeds the tube, the reel spins, the tape runs, the lamp pulse goes to the bay and the plotter
  draws.
- **Missing:** insertion weight and a decoded reward.
- **Target:**
  - **Insert:** the slot's iris opens (200), the tube slides in (400), then a "chunk" and the slot lamps.
  - **Decode (4 s):** the tape prints dots in rhythm with four rising notes.
  - **Done:** the board node lights with a bloom pop and a D-major chord. The pulse runs down to the bay.
    **Rumble:** 0.1 for 80.
  - **Plotter (R5):** the pen draws with in-key scratches. The sheet's last line lands on the tonic.

### 2.10 Kit install on 07 (P1)
- **Now:** a planned arm sequence with the turntable, sparks and the upgrade arpeggio.
- **Missing:** an alignment beat, the seat, power-on and a first use.
- **Target:**
  - **Align (300):** the arm hovers and a cyan alignment light lines up.
  - **Seat:** a "clunk", 07 dips 1.5 cm, and **rumble 0.2 for 100**.
  - **Power-on:** a light sweep along the new part (400), then the arpeggio.
  - **Proud pose:** head lift, brighter eye, antenna wiggle.
  - **First use:** the part demonstrates once (the drums glow, the lamp bar sweeps).

### 2.11 Radio and letters (P2)
- **Now:** tune, static, needle sweep, hop out and in.
- **Missing:** the letter's arrival beat.
- **Target:** static rises, the needle sweeps, the signal *catches* with a soft two-note "lock" (A→D), and the
  ticker types in at reading speed. 07 tilts its head toward the sound.

### 2.12 UI presses and menus (P2)
- **Now:** cues exist (focus, confirm, back, hold fill and complete, card, menu open and close).
- **Missing:** consistent motion.
- **Target:**
  - **Focus:** 120 ms, scale 1.0 → 1.04, a tick.
  - **Confirm:** press-down 60 / release 140 with a glow.
  - **Hold ring:** fills with a rising pitch D → D′, then a completion pop.
  - **Panels:** slide in 200 ms with ease-out.
  - **Counters:** roll (§4, CountTick).

### 2.13 Low-battery moments (P1)
- **Now:** the 30% two-note, the eye dims, the pillar rises, the crawl, and Tilly leads.
- **Missing:** the power-down transition and the relief.
- **Target:**
  - **At 0 (0–600):** the lamp dims, the head droops 8°, the wing droops, the hum drops a fifth, and a soft sigh.
    **Rumble:** 0.06 decaying over 600.
  - **Tilly's arrival:** her light grows from the distance before she arrives, then a chirp.
  - **Recharge:** the relief is the §2.7 "full" beat, plus the head lifting from its droop.

## 3. P0: the cut, fully specified
The hold rule stays: **hold to cut; release keeps progress** (ruling 4). Charge is drawn only from ignition on.

### 3.1 Timeline (fresh cut; times from the press)
| t (ms) | Beat | Body | VFX | Light | Audio | Cam | UI | Rumble |
|---|---|---|---|---|---|---|---|---|
| 0–250 | **Engage** (existing ease to a stop) | the head turns to the cut point; the lens iris narrows and the eye glow +30% | the target's halo goes to its cut level (existing) | — | the emitter's servo whirr | Gentle off: the look target leans 15% toward the cut point over 600; Gentle on: nothing | the hold ring appears | — |
| 0–650 | **Charge-up** (500–800, tunable) | the emitter tilts onto the line; a slight 1° forward lean | **motes condense**: 14–20 cyan-white motes spawn on a 0.6 m sphere around the emitter and are drawn in on curving paths, accelerating (ease-in); a glow bead grows at the emitter (0 → 4 cm) | emitter point light 0 → 1.5 (ease-in) | a rising four-note arpeggio, D E F# A over 600 (soft, glassy), over a low "inhale" hum through the chassis | — | the ring's first segment fills (the charge) | 0 → 0.08 ramp |
| (release during charge) | **Cancel** | the lean recovers | the motes disperse outward in 200 ms | fades in 250 | a soft falling A → F# | — | the ring empties | off in 150 |
| 650–710 | **Ignition** | **recoil**: 1.5 cm back and a 2° head nod, recovering in 250 | a white flash at the emitter and the cut point (r 0.3 m, 60 ms); the beam extends to the cut point in 80 (fast ease-out, never instant) | flash 2.5 → 0 over 180 at the cut point | an ignition "zing" on D′ plus a felt low thump (~60 Hz, 120 ms, through the chassis) | — | — | **0.25 for 90**, then 0.12 sustained |
| 710 → break (2–4 s) | **Sustain** | small steadying micro-motions (±0.3°) | **beam:** a hot white core (2 cm) inside a cyan sheath (6 cm) with a slight shimmer. **Molten seam:** the hot point crawls along the piece's cut line with progress. Behind it the seam cools: white-yellow → orange (1.0 s) → dull red (3 s) → dark (6 s). **Sparks:** ballistic, 1/6 g, no drag, 1.5–4 m/s, rate rising with progress; on ground contact one bounce (restitution 0.3), then a **slag bead** lies glowing 1.5–2.5 s and cools out | a warm point light at the cut (orange-white, ±10% flicker at ~12 Hz) lights the wreck, the ground and 07's front | the existing singing tone climbs one pentatonic step per cut step (keep `CutBeamModel`), plus a low-passed sizzle "through the body", plus the material's grind | — | the hold ring fills (existing) | 0.10 → 0.18 with progress |
| (release mid-cut) | **Pause** | the lean recovers | the beam retracts into the emitter in 120. **The seam stays and keeps cooling: progress is visible as a dark cut line on the piece** | the cut light fades in 200 | the existing soft release | — | the ring shows the kept progress | off in 150 |
| (re-press) | **Resume** | — | charge-up **300 ms only** (the seam is still warm); the seam's end re-glows | — | a two-note arpeggio | — | — | — |
| break +0–120 | **Crack** | 07's suspension dips 2 cm (the beam's load lets go) | the seam's end flashes white (80); **the wreck shudders 1 cm for 150**; the emitter vents 6 sparks and its glow fades over 400 | a cut-light spike, then off | a "crack": a soft high B pluck plus a felt thud | — | — | **0.35 for 120** (the strongest pulse in normal play) |
| +120–1300 | **Release and tumble** | the head follows the piece | the piece pops (existing pop 0.45) and **tumbles slowly in 1/6 g for ~1.2 s** (was 0.7), rising ~0.5 m; 2–4 slag beads drop off it in arcs | the seam glow on the piece cools as it turns | the beam's quick fall-away (existing) | — | — | — |
| +1300–1900 | **Fold** | — | the piece's facets fold into a **bundle of its material** (strapped stack, material colour) | — | a strap "tuk" on E | — | — | 0.05 for 40 |
| +1900–2100 | **Tie-off** | — | the bundle pops: scale 1.0 → 1.12 → 1.0 | — | — | — | — | — |
| +2100–2800 | **Flight** | 07 turns its head to receive | the existing spiral flight to the cargo socket (0.7 s) | — | — | — | — | — |
| +2800 | **Received** | the rear dips 1.5 cm with one rebound; an antenna wiggle | the existing arrival flash | — | the site's melody step chime (existing) | — | **the stock count rolls up one unit per 80 ms, each unit a soft rising tick** (CountTick) | 0.05 per unit tick |

- Break to stock is ~2.8 s. **It never locks the player:** a new charge-up may start while the bundle flies. Chained
  pieces keep the site melody climbing (existing `_chainWindow`).

### 3.2 Contracts and data the cut needs
- **Art, per piece:**
  - a **cut line**: an ordered polyline on the piece's face, two to five points, part of the site prefab contract;
  - the bundle look per material.
- **Gameplay** adds a **Charging** phase before Cutting in the cut state, plus seam progress along the cut line
  (0–1), and publishes the beats (§4 `FeelBeat`). Battery drain starts at ignition.
- **`SalvageTuning`** new fields (each `[Tooltip]` + `[Range]`):
  - `_chargeSeconds` 0.5–0.8 and `_resumeChargeSeconds` 0.3;
  - `_ignitionFlash`;
  - `_seamCoolSeconds` (1 / 3 / 6 s stops);
  - `_sparkSpeed` 1.5–4;
  - `_sparkGravity` = lunar;
  - `_sparkRestitution` 0.3;
  - `_beadLifetime` 1.5–2.5;
  - `_tumbleSeconds` 1.2;
  - `_foldSeconds` 0.6;
  - `_tiePop` 1.12.
- **Pool sizes** (zero GC): sparks ≤ 64, beads ≤ 24, motes ≤ 24, flashes 4 (existing).
- **Dig** (excavation) reuses the same kit: charge-up, ignition and a beam into the ground. Its impact is a dust
  column that **arcs and falls** (R11), and the relic rising is the reward.

## 4. The feel kit (shared, reusable for 0.6's build pads)
One Core contract carries every beat, so VFX, audio, 07's body and rumble each subscribe once.

**`FeelBeat`** (`Core/Events/GameplayEvents.cs`, readonly struct, zero GC):
- `Kind`: Cut, Dig, Repair, Tether, Leap, Land, Drive, Dock, MastCharge, Lift, Decode, Install, Collect, LowPower,
  Letter, Build;
- `Phase`: Anticipate, Act, Impact, Follow, Reward, Cancel;
- `Position` and `Normal`;
- `Strength` (0–1);
- `Material` (or none).

Published by whoever owns the verb's state (gameplay or rover). Plus **`IFeelSettings`**: rumble on/off, rumble
strength, and the existing Gentle camera.

| Piece | Owner | What it is | Tuning SO fields | Used by |
|---|---|---|---|---|
| **ChargeUp** | art (VFX), audio | motes drawn in, a growing glow bead, a rising arpeggio, cancelable | duration, mote count, radius, light curve, note sequence | cut, dig, repair, leap coils, build pads |
| **IgnitionFlash** | art | a 60 ms white flash, light spike, 180 ms decay | radius, intensity, decay | cut, dig, repair, mast lighting |
| **HeatSeam** | art (shader) | a strip whose emission follows a heat ramp by age along a polyline | ramp stops (white, orange, dull red, dark), cool times | cut, repair stitch, build welds |
| **VacuumDebris** | art | ballistic particles at 1/6 g, no drag, one bounce, optional glowing rest (beads) or settle (dust) | gravity, speeds, restitution, rest time, kind (spark, bead, dust) | sparks, slag, wheel dust, landing ring, dig |
| **ImpactNudge** | rover | 07's body response: recoil, dip, squash, lean, by strength, with one overshoot | per kind: distance, angle, spring, damping | everything with an Impact phase |
| **LightPulse** | art / world | a pooled point light with a curve | curve, colour, range | ignition, cut light, dock spark, node pop |
| **CountTick** | ui, audio | the counter rolls one unit per step, each with a rising pentatonic tick and a chip pulse | step ms, pulse scale | stock, cells, sets |
| **PentatonicPhrase** | audio | a short note sequence on D E F# A B with glide | steps, spacing, envelope | charge-ups, fills, locks |
| **RumbleProfile** | rover (07 feels it) | an SO: low/high motor curve and duration. A player plays at most two at once, mixes by max, and caps at 0.35 | curve, duration, cap | every beat |

Order dependency: **`FeelBeat` and `IFeelSettings` land in Core first** (Director). Each domain's pieces follow
independently.

## 5. Priorities and task split (standing sessions only)
**P0: the cut and collection** (owner's example). One playable vertical slice before anything else.

| # | Task | Session | Size | Needs |
|---|---|---|---|---|
| 0 | `FeelBeat`, `IFeelSettings` in Core | Director (or gameplay, reported) | S | — |
| 1 | Cut state with a Charging phase (cancel and resume), seam progress along the cut line, break sequence timings, battery from ignition, `FeelBeat` publishing, the new `SalvageTuning` fields, EditMode tests | gameplay | M | 0, the cut lines |
| 2 | Cut lines on every piece; ChargeUp motes; IgnitionFlash; beam core and sheath; HeatSeam shader; VacuumDebris (sparks, beads); bundle fold and tie-off; LightPulse pool | art | **L** | 0 (stub beats for captures) |
| 3 | ImpactNudge (recoil, dip, wreck shudder hook), head and lens beats, RumbleProfile player and the cut profiles, the rumble setting's backend | rover | M | 0 |
| 4 | Charge arpeggio, ignition zing and thump, sizzle layer, crack, strap tick, count ticks (CutBeamModel kept) | audio | M | 0 |
| 5 | Stock CountTick roll and chip pulse; the hold ring's charge segment; the rumble on/strength setting | ui | S | 0 |

**P1** (after P0 is approved in play): drive start, stop and turn; leap; tether; dock and mast charging; the belly
lift; kit install; low battery. Mostly **rover** (body, rumble) and **art** (debris, light) reusing the kit,
**audio** for new beats, and **gameplay** for beat publishing. Each is S–M.

**P2:** listening, lab decode and plotter, radio letters, UI motion pass. S each.

### Acceptance (P0)
- A first-time player can say what happens before the laser fires ("it charges up").
- Releasing mid-cut leaves a visible cooled seam, and re-pressing resumes faster.
- Captures of each beat at 1080p: charge, ignition, sustain (seam and sparks), crack, tumble, fold, received.
- Sound: at most three layers at once, all in key, peaks at mezzo-piano. No camera shake, and Gentle is respected.
- Rumble never exceeds 0.35 and is off when the setting is off.
- Zero per-frame GC with 64 sparks, 24 beads and 24 motes live. 60 fps on the target PC.

## 6. Decisions (Director, 2026-10-12)
- **The owner approved P0** (the cut and collection) to start now. P1 and P2 wait until the owner has played P0.
- **Rumble: on by default, gentle.** The cap of 0.35 holds. Settings get "Rumble" (on/off) and "Rumble strength".
- **The contract is on main** (the Director's Core commit):
  - `Core/Events/FeelEvents.cs`: `FeelBeat(kind, phase, position, normal, strength, seconds[, material])`.
  - `Core/FeelKind.cs` and `Core/FeelPhase.cs`.
  - `Core/IFeelSettings.cs`: `RumbleEnabled`, `RumbleStrength`. Rover registers it; UI persists it.
- **How a cut publishes its beats.** Rover and Audio see only Core, so the beats carry everything they need:
  - `Anticipate`: `Seconds` = the charge length (0.3 s on a resume).
  - `Act` once at ignition (strength 0), then once per cut step (strength = progress), which drives the sustained
    rumble and sizzle.
  - `Cancel` on release: it keeps progress mid-cut.
  - `Impact` at the crack, `Follow` at the tumble (strength 0) and at the fold (strength 1), and `Reward` with `Kind = Collect` when the bundle
    is received.
- **Where the kit's runtime lives.** The Art assembly is a mesh library with no runtime components. So:
  - The runtime VFX pieces (ChargeUp motes, IgnitionFlash, VacuumDebris, LightPulse pool, HeatSeam strip driver,
    bundle tie-off) live in `Gameplay/Feel/`, next to today's `CutSparks` (which VacuumDebris replaces). Gameplay
    builds them.
  - Art owns the look: the HeatSeam and beam core/sheath shaders and materials, the mote/spark/bead materials, the
    bundle meshes per material, and the cut-line polylines in the salvage site data.
  - Art reviews every capture.
- **Audio starts after the voice slice lands.** The voice session continues as the standing audio session.

## 7. Owner additions (2026-10-12): the dock locks, and 07 speaks machine
The owner: "everything must be extremely polished". Two examples, now P1's first two items.

### 7.1 The dock locks 07 in, and lets it go when full (replaces §2.7's dock beats)
| Beat | Target |
|---|---|
| **Roll-on** | The dock rim lamps chase toward the centre as 07 rolls onto the pad. |
| **Lock** (charging starts: stopped on the pad) | Four low walls rise from slots in the dock floor, one per side. Each is ~0.35 m tall with a cyan lamp strip along its top. They rise staggered 60 ms apart, 450 ms each, ease-out with a small overshoot. Then the clamp closes with a soft "clunk" (FeelBeat Dock Impact): 07 dips 2 cm, a 100 ms connection spark, and 07 chirps **Affirm** (§7.2). If 07 arrived at ≤15%, it chirps **Relieved** instead. **Rumble:** 0.15 for 80. |
| **Fill** | **Each cell lights one wall's lamp strip** (4 cells = 4 walls; more cells subdivide the strips), plus 07's flank lamp pops on its note. FeelBeat Dock Act per cell, strength = charge. |
| **Full** (Reward) | The four strips sweep from cyan to warm gold, with a soft D-major chord. The walls lower, staggered, 600 ms ease-in. 07 "sighs": the wing opens 10°, the eye brightens. 07 chirps **Happy**. |
| **Drive off early** (Cancel) | The walls drop at once (250 ms) and never block 07. They carry no colliders that touch the wheels. Partial cells are kept (existing rule). |
| **Loaded game, docked** | The walls and lamps show the state silently. |

### 7.2 07 speaks machine (a new character layer)
07 gets a small voice. These are not words in any language. They are soft machine chirps (the owner's "tè tè"), each
in a melody that says the mood. Think R2-D2 or BD-1, but quieter and lofi, in D major pentatonic.

| Mood | When | Contour (pentatonic steps) |
|---|---|---|
| **Affirm** | the dock locks; a kit piece seats; a recipe completes | two short notes up, A → D′ (~240 ms) |
| **Happy** | the battery is full at the dock; a friend is repaired | a bouncy 3–4 note rising trill that ends on a high D (~600 ms) |
| **Proud** | a kit piece powers on (install) | a 5-note rising flourish (~800 ms) |
| **Curious** | the first answer of a kind when listening; a new place found | a rising "question" D → F# → A with an upward bend at the end (~500 ms) |
| **Worried** | the battery reaches Low (30%); 07 is stuck or recovering | two falling notes with a wobble, B → F# (~450 ms) |
| **Tired** | the battery reaches 0 (the crawl starts) | a slow falling glide, A → low D, fading (~900 ms) |
| **Relieved** | charging after a very low or empty battery; Tilly arrives to lead | "phew": F# → D, then up to A (~700 ms) |
| **Greeting** | a friend meets 07 at home (the first time each day) | a friendly two-syllable D → A |

Rules:
- One chirp at a time.
- A global cooldown of 6 s, and 30 s per mood (Affirm 3 s).
- Never over a voice line or a letter: wait for it to end, or drop the chirp.
- It ducks under the radio and peaks at mezzo-piano.
- **The timbre is 07's own,** distinct from the friends' kalimba-like chirps: a soft sine and square blend through a
  gentle low-pass, with 30–60 ms glides between notes and a slight wobble.
- Four variants per mood, picked at random but never the same one twice in a row. Intensity may shift the pitch one
  step.
- **Body sync:**
  - each syllable pulses the eye (+25%, 80 ms) and flicks the antenna tip;
  - Curious tilts the head 8°, Worried dips it 4°, Happy gives a small bounce, and Tired lowers it slowly.

The Core contract (Director):
- `Core/ChirpMood.cs`.
- `RoverChirpRequested(mood, intensity)`: published by Rover. 07's character decides when it speaks, from the events it
  already hears.
- `RoverChirpSyllable(mood, index, count, pitch, seconds)`: published by Audio at each syllable's onset, so Rover syncs
  the eye and antenna.
- Audio renders the bank offline (`tools/audio`, like the other SFX), stores each variant's syllable onsets as data, and
  plays them with zero GC.

### 7.3 Who does it
| Task | Session | Notes |
|---|---|---|
| The dock lock sequence: four wall meshes with lamp strips (Art/Editor/ChargingDockMeshes, BaseModelBuilder), their motion, the strip lamps per cell, the full sweep, the drive-off drop, Dock beats | gameplay (feature slice: Gameplay/Base + those Art builders) | after the Dig follow-up |
| 07's chirp decisions (moods, cooldowns, priorities, never over voice), the eye/antenna/head sync on `RoverChirpSyllable`, the dock dip and the full "sigh" | rover | now; test with scripted syllables |
| The chirp bank (8 moods × 4 variants, offline synth, the syllable data), playback publishing `RoverChirpSyllable`, the ducking, the dock sounds (wall rise ticks, clunk, lock spark, the full chord) | audio (the voice session, after it lands) | after 0.5.1 |

## 8. Owner review of 0.5.2 (2026-10-12): the words, and the tower's payoff
The owner says the UI effects are better, but:
- the language makes no sense: "Đánh thức cột anten cũ", "the tower is asleep", "power the tired antenna and home
  wakes up";
- the tower repair task is unclear;
- **when the tower is repaired, nothing happens.**

### 8.1 Writing rules (all player-facing text, en and vi)
- **Functional text is plain:** objectives, cards, prompts, recipe cards, settings and tutorials.
  - Say what it is, what to do, and why, in that order.
  - Use the verb the player performs: "Sửa tháp radio", not "Đánh thức cột anten".
- **Machines are not people,** except 07 and the friends, who have characters. Don't write "asleep", "tired" or "wakes
  up" for the tower, the dock or the lander. Write "offline", "no power", "online", "has power".
- **One name per thing,** everywhere: "Radio Tower / Tháp radio", never also "antenna" or "cột anten".
- **Vietnamese is written natively, not translated word for word.** Short and natural, like a well-made Vietnamese
  game.
- **Poetry belongs only to the voices:** Ro's letters and recordings, and the radio. That is the story's tone.
- **Recipe rows show need first, then have:** e.g. "Kim loại 1 (có 5) ✓". A raw "5/1" reads as a fraction.

### 8.2 The tower comes online (FeelKind.Build at the tower, every level-up)
Gameplay publishes it when an upgrade of the radio tower completes. Each piece answers the beat.

| Phase | What happens | Owner |
|---|---|---|
| **Anticipate** (Seconds ≈ 1.2) | The parts visibly go in at the hopper (each one drops in with a clunk). A low hum rises. The camera eases to frame the tower with 07 in the foreground. | gameplay (parts), audio (hum), rover (camera moment) |
| **Act** ×4 (strength 0.25 → 1) | Power climbs the tower: its lamps light bottom to top, one ring per step. A pentatonic step per ring. | gameplay or world (the tower's lamps), audio |
| **Impact** | The beacon on top flares and starts its slow turn. A deep soft "thoom" plus a D-major chord. 07 chirps **Happy**. Rumble 0.3. | world (beacon), audio, rover |
| **Follow** strength 0 | Light races along the cable from the tower to home (clarity's cable preview, now for real), then the home windows step on, then the radio crackles and music fades in. | world, audio |
| **Follow** strength 1 | The reach ring rolls out (existing) and the camera eases back. | world, rover |
| **Reward** | A card says what the player got: e.g. "Tháp radio · Cấp 1: nhà đã có điện · radio bắt được tín hiệu · đi xa hơn 150 m". | ui |

- **Before the repair:**
  - the next-step line and the recipe card name the exact steps (bring the parts, park on the tower's pad, hold E);
  - the tower shows its state: dark lamps, plus a small "offline" plaque light.
- **Never lock the player for long:** the sequence takes about 6 s and any drive input skips the camera part.
