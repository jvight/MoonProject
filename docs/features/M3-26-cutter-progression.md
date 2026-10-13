# M3-26: The cutter grows (charge run, cutter tiers, piece sizes)

> Owner: Director (design), drafted by the game design session. **Draft for the owner's approval.**
> The owner, 2026-10-12:
> 1. "Give 07 a light system like Godzilla's dorsal plates glowing in sequence before its atomic breath: when 07
>    charges the laser to cut scrap, lights run along its body."
> 2. "Laser upgrades: a shorter charge so it fires sooner, and faster dismantling."
> 3. "Early scrap pieces are small and cut fast; later they're bigger and take longer."
>
> Builds on:
> - feel-pass §3 (the cut's beats: ChargeUp motes, ignition, seam) and §4 (`FeelBeat`);
> - M3-20 (battery: `_beamSecondsPerCell` 60), M3-25 (cargo);
> - GDD v3's beam branch (Thermal Cutter, 0.7);
> - the cut lines in `SalvageSiteEntry` / `PieceCutLine`.
>
> Today's tuning (`SalvageTuning`): charge 0.65 s fresh and 0.3 s on a resume; a cut takes 2–4 s (1 s per unit);
> 5 cut steps.
>
> Rulings 4 (progress is kept), 5 (no grind), 11 (every upgrade shows) and 14 apply, as do P3 (the rear is never a
> face) and the writing rules.

## 1. Learn, then improve
| Reference | What it does | Our version |
|---|---|---|
| **Godzilla's dorsal plates** | light runs up the plates before the breath: the power is *seen travelling* through the body before it's released | The **charge run** (§2): energy flows from 07's battery along its back to the emitter, in the Anticipate's time. It is warm machinery, not menace: rounded radiator ribs, never spikes. |
| **Hardspace: Shipbreaker's tool tiers** | upgrades are numbers in a menu; bigger hulls are walls of time | Every tier **changes 07's body**: more ribs, a faster run, a hotter core. Big pieces cut in **segments**, so long work is a sequence of short satisfying beats, never one long hold. |
| **Both** | a charge you wait for | The charge is part of the pleasure, and the tiers make it *snappier*, never skippable. Waiting less is the reward you feel on every cut. |

## 2. The charge-up (revised 2026-10-13; full spec in `docs/design/0.5.5-owner-feedback.md` §5)
The owner, after 0.5.4: the first version (a row of lamps lighting one by one in 0.65 s) was too fast and weightless.
Its sound didn't fit, the lamps didn't read as technology, and 07 was lopsided. The charge-up is now one designed
moment:
- **Timing:** holding E builds **stages 1–3**, and the beam fires when the piece's needed stage is reached (small 1,
  medium 2, large and hull 3). A fixed **0.5 s brace** comes first on every fresh charge. A **warm emitter** (a
  re-press within 6 s at the same site) skips the brace and builds at ×0.6. Tiers shorten the stages (§3), never the
  brace.
- **Curve:** brace → per-stage ease-in builds, each ending in a lock click → full lock → a 120 ms held breath →
  ignition.
- **Body:** 07 squats 3 cm, locks its wheels and stiffens its rockers. It settles 0.5 cm per stage, with a body-only
  micro-tremor, and is completely still at lock.
- **Visual:** energy travels through **hardware** (a conduit, rings, a collar) from the battery to the emitter, with
  something moving at each stage. It is symmetric about 07's centre plane, readable in three stages at 30 m, and
  tech-cyan going to white. Art offers 2–3 variants for the owner; the requirements are in the 0.5.5 spec §5.4.
- **Sound:** a mechanical build heard through the chassis: brace servo, a capacitor whine stepping D → A → D′ on
  detent clicks, a "clunk-click" lock and the held note through the breath.
- **Camera:** a gentle push-in only for fresh large and hull charges, dead still at lock. None under Gentle, never any
  shake.
- **Drain on release** before firing: the fill runs back to the source in 200 ms. Nothing is lost (ruling 4).
- **The ChargeUp motes** (feel-pass §3) still condense at the emitter in the last stage, fed by the conduit.

## 3. Cutter tiers
Names follow the glossary style. **New glossary rows (story to add):** Cutter (the beam's cutting) / *máy cắt*; Focus
Lens / *Thấu kính hội tụ*; Cutter Ribs / *Gân tản nhiệt*; Thermal Cutter / *Máy cắt nhiệt*.

| Tier | Kit (fitted in the bay) | Ver / unlock | Recipe | Stage time (brace 0.5 s fixed) | Cut speed | Beam drain | Visible on 07 |
|---|---|---|---|---|---|---|---|
| 0 | the old cutter | start | — | 0.45 s | ×1.0 | ×1.0 | the base charge hardware (0.5.5 spec §5.4) |
| 1 | **Focus Lens** | 0.5.x. Bell's first signal points at its tube in Kestrel-3's cockpit (~min 50–60). | Optics 3, Wiring 1 | 0.36 s | ×1.3 | ×0.85 | a ringed lens on the emitter |
| 2 | **Cutter Ribs** | 0.6, a terrace wreck's heart | Alloy 2, Wiring 3 | 0.29 s | ×1.7 | ×0.70 | radiator ribs along the back conduit |
| 3 | **Thermal Cutter** (GDD v3's node) | 0.7 | blueprint + Alloy + Heat Sink | 0.22 s | ×2.2 | ×0.60 | the hot core, heat-sink fins at the emitter. **Also opens sealed hulls** (the one hard gate). |

- **Speed** divides each segment's cut time. **Drain** multiplies the beam's battery use (M3-20 counts beam seconds
  from ignition only).
- Faster *and* cheaper means longer trips: the second axis.
- Tiers never shorten the post-break beats (tumble, fold, flight). The reward keeps its pace.

## 4. Piece sizes
### 4.1 Classes
A piece lists 1–3 cut lines (`PieceCutLine`). **Each line is a segment**, cut in turn.

- Between segments, the beam stays on and slides to the next line's start in 250 ms (ease-in-out, no new charge),
  with a soft step note.
- The seam of the finished segment keeps cooling (feel-pass §3).
- Releasing between or during segments keeps the progress (ruling 4).

| Size | Segments | Seconds per segment (tier 0) | Total at tier 0 / 1 / 2 / 3 | Units | Slots (M3-25) |
|---|---|---|---|---|---|
| **Small** | 1 | 1.2 | 1.2 / 0.9 / 0.7 / 0.55 | 2 | 1 |
| **Medium** | 1 | 2.5 | 2.5 / 1.9 / 1.5 / 1.1 | 3 | 1 |
| **Large plate** | 2 | 2.5 | 5.0 / 3.8 / 2.9 / 2.3 | 5 | 2 |
| **Hull plate** | 3 | 3.5 | 10.5 / 8.1 / 6.2 / 4.8 | 8 | 2 |
| **Sealed hull** (0.7) | 3 | 4.0 | Thermal Cutter only | 10 (Circuitry) | 3 |

**Slow but always possible.** Every size up to the hull plate cuts at tier 0. The only hard gate is the sealed hull,
and it is GDD v3's existing Thermal Cutter gate for Circuitry. That is a *material* gate seen first (Kestrel's sealed
hatch, GDD v3 A2), not a slowdown.

### 4.2 Where each size lies (0.5 basin)
| Site | When | Pieces | Cutting at tier 0 (with charges) |
|---|---|---|---|
| **Depot** | min 3–7 | 2 small, 2 medium | each cut ≈ 1.9–3.2 s; **the whole depot ≈ 10 s** of beam |
| Garage | min 17–27 | 3 medium (×2 yield) | ≈ 3.2 s each |
| Drill | ~min 40 | 4 medium, 1 large | the first two-segment cut, ≈ 5.7 s |
| Kestrel-3 | ~min 50–70 | 4 medium, 3 large | ≈ 22 s in all; **the Focus Lens is found here** |
| Crashed lander (canyon) | ~min 60–80 | 4 medium, 2 large, 1 hull plate | the first hull plate (10.5 s, or 8.1 s with the lens) |
| 0.6 terraces | — | large and hull plates (Alloy) | Cutter Ribs arrive here |

### 4.3 The first hour
- The first cut the player ever makes (a depot small piece) is **0.95 s of charge (brace + stage 1) + 1.2 s of cutting ≈ 2.2 s** from
  press to crack, against ~2.7–4.7 s today.
- Nothing in the first 30 minutes takes over 3.2 s, so the owner's first impression is snappy.
- **Long cuts arrive only after the player knows the verb and has seen a reason:**
  - the drill's first two-segment piece at ~40 min;
  - the first hull plate at ~60 min.
- The Focus Lens arrives right when hull plates appear. The upgrade lands at the moment it is felt.

### 4.4 Battery and cargo
- **Battery** (beam: 1 cell ≈ 60 s):
  - the depot ≈ 10 s, about 0.17 cell;
  - Kestrel and the lander at tier 0 ≈ 50 s, about 0.85 cell;
  - with the lens: 50 / 1.3 × 0.85 ≈ 33 s-equivalent, about 0.55 cell.
  - Cutting stays a small share of a trip. Driving is the range story.
- **Cargo:** bigger pieces yield more units.
  - New yields (estimate): depot 10, garage 18, drill 17, Kestrel 27, lander 30, trail 8.
  - Add M3-25's crew crates (30), and the supply is **≈ 140 units, 1.9× the ~72 needed** (ruling 5 reached).
  - **One site still fits one trip:** Kestrel's 27 units ≈ 7 slots, the lander's 30 ≈ 8 slots of the 8-slot start.
  - Chaining two late sites overflows, which is M3-25's intended pressure for the Side Bins.

## 5. Feel per tier
| Beat | Tier 0 | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|---|
| Charge hardware | the base conduit (0.5.5 §5.4) | + the Focus Lens ring | + rib edges along the conduit | + a hot core |
| Arpeggio | D E F# A | + B | + D′ (a full octave) | + a sustained low D under it |
| Ignition flash | feel-pass §3 | +15% | +30% | +45% and amber-white |
| Beam hum | the existing tone | a step brighter | a fuller second layer | a deep warm undertone |
| Rumble at ignition | 0.25 | 0.25 | 0.28 | 0.30 (cap 0.35) |

- **Install moment** (feel-pass §2.10): the bay fits the part, then **the first charge run plays in the bay** at the
  new speed. 07 cuts a small test coupon the bay holds out, and it cracks in under a second. 07 chirps **Proud**. The
  kit card (first-hour-qa R1) says: "Focus Lens: 07 charges and cuts faster."
- **The first real cut after an upgrade:** 07 chirps **Happy** once at the crack (cooldown rules, feel-pass §7.2).
- **Segment step:** a soft "tick-slide" on the next pentatonic step as the beam moves to the next line, so a hull
  plate reads as three short victories.

## 6. Contracts, save, split
**Core (Director, landed first):**
- `ICutter` (registered by Gameplay): `Tier`, `LampCount`, `ChargeSeconds`, `ResumeChargeSeconds`, `SpeedFactor`,
  `DrainFactor`. Allocation-free.
- `CutterTierChanged(tier, restored)`.
- `FeelBeat` stays as is. **`Anticipate.Strength` carries the run's start fraction** (0 on a fresh charge, 0.5 on a
  resume). `Act` per segment step (strength = progress) as today. A new **`Follow` with strength 0.5** marks a
  segment change.
- **Glossary rows** (§3) in `writing-style.md`. Kit keys `kit.focus_lens.*`, `kit.cutter_ribs.*`.

**Tuning:**
- **`CutterTuning`** (gameplay): one row per tier (stage time, speed, drain), plus the brace time and the warm-emitter factor and window with `[Tooltip]` and
  `[Range]`.
- **`SalvageTuning`**: a size table (segment seconds, segments, units) replacing `_cutSeconds` and
  `_cutSecondsPerUnit`.
- **`SalvageSiteEntry`**: a size class per piece, and its cut lines become 1–3 segments.

**Save:** the tier is the fitted kits (the existing upgrade save). No new section and no content floor. Old saves
start at tier 0, and pieces already cut stay cut.

| Session | Tasks | Size |
|---|---|---|
| **gameplay** | `ICutter`, `CutterTuning`; segment cutting (slide, keep progress per segment); the size table and per-site classes; the Focus Lens kit data and its tube in the Kestrel's cockpit on Bell's first signal; drain by tier; economy tests (one site fits one trip; supply ≈ 1.9×) | M |
| **art** | the chosen symmetric charge hardware (0.5.5 §5.4, 2–3 variants first), the Focus Lens model; re-sized pieces at the depot (small), the lander's hull plate mesh; cut lines with 2–3 segments on large and hull pieces; ribs and tier 3 later (0.6–0.7) | M |
| **rover** | the charge-up driver (brace, stages, lock, breath, drain, warm glow), 07's brace body language and the push-in camera rule, the bay's test-coupon cut in the install moment, chirps | M |
| **audio** | the arpeggio extended per tier, the hum layers, the segment "tick-slide" | S |
| **ui** | the kit cards (first-hour-qa R1 lines), the cutter tier on the Tab screen's "07 can" page | S |

**Order:** Core → gameplay's tuning and segments (tests first) → rover's run (testable with scripted beats) and
art → audio and ui. It runs alongside M3-25: both touch salvage yields, so **gameplay owns one economy table for
both specs**.

## 7. Acceptance
- The run is readable from the chase camera: a first-time player can say "the light runs to its head, then it
  fires".
- Cancel drains it backwards, and a resume starts part-way.
- The first depot cut, press to crack, takes ≤ 2 s. Nothing in the first 30 minutes takes over 3.5 s.
- Each tier is visible on 07 at 30 m (conduit, lens, ribs, core) and felt on every cut (shorter stages, quicker crack).
- One site fits one trip at every tier and stage. Supply ≈ 1.9× needs.
- Zero per-frame GC. The charge hardware is pre-built and driven by a parameter.
