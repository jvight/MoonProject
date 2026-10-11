# M3-20: The battery, chargers and the crawl home

> Owner: Director (design). Source: `docs/design/gdd-v3.md` §3 (approved by the owner on 2026-10-11) and the review's
> numbers (`docs/design/review-gdd-v3.md` §1, owner decision 4). VISION ruling 15 is the law here; pillars 2 and 6
> apply to every frame of it. Version: **0.5**.
>
> The owner's words: "07 must go back to charge to keep moving, and upgrades gradually let it go farther and explore
> more." The calm rules keep it gentle: the battery is range, never a threat.

## 1. What the player feels
- 07 starts with **four cells**, shown as four small amber lamps along its stripe. Driving and beam work draw them
  down; the dock and lit masts fill them back up with a rising run of notes.
- The first time 07 runs low is within sight of home (Tilly's parts, §6), so the lesson is "the dock charges". At the
  chasm the second lesson is "masts charge": relay.2's dormant pad sits 20 m away.
- Running out is never a failure. 07 sighs, dims and crawls home slowly and quietly. Once Tilly is home she flies out
  by herself after a few seconds and leads with her lamp. Nothing is lost.
- Range grows visibly: Cell Pack II grows a battery spine on 07's back and two more lamps, and every lit mast is a
  warm charger on the horizon.

## 2. The rules (VISION ruling 15)
1. **No death, no loss, no failure screen.** Whatever 07 carries (a towed relic, a tube, a component) stays with it.
2. **A verb in progress always completes.** A cut, dig, stitch or feed that starts on the last charge finishes on the
   reserve; the crawl starts after it. A held tow never drops: at empty, 07 crawls on with the relic still on the
   tether.
3. **The leap refuses at the lip.** When the battery can't pay for a full leap (`IRoverBattery.CanLeap` false), the
   coils never charge: a soft fizzle under the belly and 07 glances at its lamps (`RoverLeapRefused`). A leap never
   fails mid-air.
4. **Never stranded.** At empty 07 drops into a **low-power crawl** at 35% of cruise (~2.3 m/s): quiet, the lamp at a
   glow, new beam work, the leap and the boost asleep, the sonar still on. The crawl never runs out (Kenji's sealed
   reserve cell). It ends on a charger, or when the solar trickle (§5) lifts the charge past a small exit margin.
5. **Tilly comes by herself** once she is home: after ~5 s of crawl she flies out from wherever she is, takes a spot
   ~10 m ahead of 07 toward the nearest lit charger, and leads with her lamp. The crawl rises to 55% (~3.6 m/s). No
   menu, no call, no tow line. She goes home when 07 starts charging.
6. **Standing still never drains.** Stargazing, the wide shot, pauses and menus cost nothing.
7. **Gentle, early warnings** (§7) that name only home or a charger the player has lit.
8. **A calm crawl** (§8): a pillar-6 moment, never a scramble.

## 3. What draws charge (`BatteryTuning`, Gameplay)
Every number is a `[Tooltip]` + `[Range]`/`[Min]` field. Defaults:

| Use | Default | Note |
|---|---|---|
| Starting cells | **4** | the gauge stays countable |
| Driving at cruise on flat ground | **1 cell per 120 m** | metres actually travelled on the ground; airborne metres are free |
| Uphill / downhill multiplier | **×1.5 / ×0.5** | from the ground slope along the travel direction, eased |
| Towing | **driving cost ×1.25, no beam drain** | protects the game's best verb (review §1.2) |
| Beam work: cut, dig, stitch, feed | **1 cell per 60 s** of active beam | a site's work stays ≈ 0.4–0.6 cell |
| Full leap / tap hop | **¼ cell / ¹⁄₂₀ cell**, scaled by `RoverJumped.Strength` | paid at the leap; the drums pay first |
| Boost | **driving cost ×2** while boosting | the drums pay first |
| Sonar ping | 0 | never discourage pinging (ruling 3) |
| Standing still, menus, pause | 0 | rule 6 |
| Crawl speed / with Tilly | **0.35 / 0.55** of cruise | `IRoverBattery.DriveCap01`, eased over ~1.5 s |
| Tilly's lead delay | **5 s** of crawl | |
| Crawl exit margin | **0.25 cell** | leaving the crawl needs this much charge, so it never flickers |
| Solar trickle | **1 cell per 180 s**, up to **1 cell** | only with Tilly's gift fitted and 07 still for ≥ 3 s (§5) |
| Dock fill / mast fill | **~4 s / ~3 s** for a full battery | |
| Warning thresholds | **50% / 30% / 15%** | §7 |

**Lights cost nothing in 0.5.** GDD v3 lists "+20% while the lamp bar or floodlights are on in the dark". In 0.5 the
lamp bar is a comfort kit, and charging for it would punish fitting it. The Director defers the lights cost to 0.7,
when Floodlights land with the dark crater.

## 4. Chargers
- **The dock at home.** Docked (`RoverDockChanged` true), the cells fill in ~4 s with one rising note per cell. When
  the dock's evening plays (`DockEveningOpened`), the fill happens inside its wide shot, so the evening is "home
  fills 07 up". Docking keeps the day as today. The dock's job line becomes "Where 07 charges. A rest here keeps the
  day."
- **Every lit relay mast has a 4 m charge pad**: a worn painted ring with a low ramp lip, at the mast's foot.
  - The relay anchor already reserves a flat pad of 3 m radius, which holds the 4 m ring. World keeps it.
  - Dormant: the ring is dust-capped, a promise that reads at 50 m ("this was a charger").
  - Lit: the mast's lamp pours a warm pool onto the ring, visible from 200 m at night (M3-18 V3).
  - 07 charges once it stands on the ring at walking pace or slower; the cells fill in ~3 s.
  - **relay.2:** the ring sits on the home side of the mast, never on the chasm's run-up line.
  - A radio-hop lands 07 on the destination mast's ring, so it arrives charging.
- **Charging feedback:** `ChargingChanged` true → 07 perks up (head lift, brighter eye, an antenna wiggle); each cell
  that fills plays the next note of D E F# A B (then up an octave); the charger's pool warms. `ChargingChanged` false
  when full or when 07 drives off.
- **The 300 m rule.** No point on 0.5's golden path is more than **300 m** from a charger that is lit by then, so the
  longest crawl is ≤ ~130 s (≤ ~85 s with Tilly). Off the path (for example the canyon before relay.2 is lit) the
  worst crawl must stay ≤ ~150 s with Tilly. World reports the reach table (§10) and the Director checks it.

## 5. Power upgrades in 0.5 (all visible, ruling 11)
| Upgrade | Needs | Does | Visible |
|---|---|---|---|
| **Solar cell repaired** (Tilly's gift, built) | Tilly home | the trickle: while 07 is still for ≥ 3 s, in any light, the charge creeps up to **1 cell, never more** | the wing's missing cell (built) |
| **Capacitor drums** (the Boost Coils kit, built) | as today | the drums hold **¼ cell**. Boosts and leaps draw from them first; downhill rolling (−5° or steeper at speed) and chargers refill them | the drums glow with `Drums01` |
| **Cell Pack II** (new kit) | its blueprint (relay.2's service cabinet, §6) + Metal + Optics (P9 economy: the Director tunes the counts within ±30% to keep 2×) | **4 → 6 cells** | the **battery spine** begins on 07's back; **6 flank lamps** |

- Cell Pack II is upgrade `rover_cell_pack`, level 1 (Cell Pack III is its level 2 in 0.6). Its blueprint tube is
  decoded at the lab's reader like every kit (M3-19), and the bay fits it with the install moment.
- New kit piece `RoverKitPiece.BatterySpine` (Core, added by this slice) on the back slot.
- Fitting it adds two lamps and fills them as part of the install moment (a two-note rise).

## 6. The range lessons on the critical path (GDD v3 §3.8)
> **Measured on the reshaped golden path at spec (milestone B, 2026-10-12):** home after Tilly's parts 25%; the garage
> cuts are split over two trips (lowest 17% and 33%); the duck's dig and tow from the garage at 232°/140 m runs to 10%
> (accepted: the 30% warning fires within sight of home, so it teaches "the dock charges" a second time); 07 charges at
> the mound mast (relay.0) on the way to the chasm, so **"masts charge" is taught at relay.0** and the chasm lip sits
> near 55%. Items 2 and 3 below are superseded by these numbers.
These come from the tuning above on the built basin; they are acceptance checks, not scripts.
1. **Tilly and her parts:** about **28%** left on the way home, ~60 m from the dock. The 30% warning fires within
   sight of home. *The dock charges.*
2. **The chasm:** about **38%** at the lip. The warning fires as the player looks across; relay.2's dormant pad and
   pillar stand 20 m away. *Masts charge.* relay.2's **service cabinet** holds the **Cell Pack II** blueprint tube
   (a rover-height cabinet at the mast's foot that opens when the mast is restored).
3. **The garage** lands close to the warning: tension, no crawl.
4. **Kestrel-3,** visible from the first frame, is over budget on 4 cells: it opens with work once the mound mast is
   lit (60 m away) or Cell Pack II is fitted. A real range gate, seen first.

## 7. Warnings (`BatteryWarned`)
| Charge | What happens |
|---|---|
| 50% | the lamps show half; nothing else |
| 30% (`Low`) | one soft falling two-note (A → F#); the eye dims a little; the nearest charger's pillar rises for 20 s (lit or dormant: a dormant mast's pillar is the "masts charge" promise); a ticker line naming **home or a lit charger only**: "Charge is getting low. The dock can top you up." / "… The mound mast can top you up." |
| 15% (`VeryLow`) | 07 glances toward the nearest lit charger |
| 0% | a soft power-down sigh; the crawl begins (§2 rule 4) |

- Each fires at most once per discharge (re-armed by charging).
- The 30% line is shown the **first three times** only (remembered in the save, as context prompts are, ruling 6);
  after that the sound, the eye and the pillar carry it.
- Keys: `battery.low.dock`, `battery.low.mast` with the mast's place name as `{0}` (en + vi).

## 8. How 07 shows it
- **Flank lamps:** a strip of small amber lamps along 07's orange stripe, one per cell, **8 sockets** built once and
  shown per capacity (4 at the start, 6 with Cell Pack II). A full cell is lit; the cell being drawn breathes from
  full to dark as it empties; empty cells are dark glass. Never on the rear as a pair (P3). Later the Range-Extender
  and Power Core each add one longer lamp at the strip's end (0.7–0.8).
- **The opening:** the four flank lamps light one by one as 07 wakes (after O5's flicker), with four quiet ticks.
- **Body language:** below 30% the eye dims a little and the wing droops; a charge perks 07 up.
- **The HUD arc (UI):** a thin segmented arc around the compass, one segment per cell. It reveals itself the first
  time 07 drops below 70%, hides while the cells are full, and hides while 07 works the beam (the flank lamps carry
  it then).
- **The calm crawl (Rover + Audio):** the camera eases wider and a touch lower over ~3 s; the radio thins (as if a
  little farther from home); motes hang in 07's dimmed lamp; the charger's pillar sits ahead in frame where the
  camera allows. Wheels hum lower. Coming out of the crawl reverses it all, eased.

## 9. Save
The charge, capacity, drums, the warnings' shown count and "the arc has revealed itself" are saved in Gameplay's
section with defaults (full, 4 cells), so older 0.5 saves load full. A game loaded during a crawl resumes the crawl
(`LowPowerChanged` restored). Raising the content floor is not needed for this spec alone.

## 10. Contracts (Core, landed by the Director on 2026-10-11)
- `IRoverBattery` (registered by Gameplay): `Cells`, `Charge`, `Charge01`, `IsLowPower`, `DriveCap01`, `CanLeap`,
  `Drums01`, `IsCharging`, `Charger`.
- `ChargerKind` { Dock, Mast } (Pylon comes with M3-22 in 0.6); `BatteryWarning` { Low, VeryLow }.
- Events (`Core/Events/BatteryEvents.cs`): `BatteryChanged(fullCells, cells, rising, restored)` on cell boundaries
  only, zero GC; `LowPowerChanged(lowPower, restored)`; `ChargingChanged(charging, charger, position)`;
  `BatteryWarned(warning, nearestLitCharger)`.
- `RoverLeapRefused(position)` (`RoverEvents.cs`), published by Rover.
- This slice adds `RoverKitPiece.BatterySpine` and may add small Core events it needs, listed in its report.

## 11. Split (one feature-slice session, "battery")
| Domain | Delivers |
|---|---|
| gameplay | `BatteryModel` (plain C#, EditMode-tested: drains, multipliers, drums, reserve, crawl entry and exit, trickle, verb completion, warnings), `BatteryTuning`, the service registering `IRoverBattery`, drain hooks (ground distance and slope from `IRoverState`, tow, beam verbs, `RoverJumped`, `RoverBoostChanged`), the dock and mast-pad chargers, Tilly's lead, the warnings and pillar, Cell Pack II (upgrade, recipe, relay.2's cabinet and tube), save, the Playthrough's charge log |
| rover | `DriveCap01` on the drive, the leap refusal, boost asleep while crawling, the flank lamp strip, the opening's lamp wake, the eye and wing at low charge, the perk on charging, the calm crawl camera and motes, the battery spine piece |
| art | the 8-socket lamp strip on 07's stripe, the battery spine piece (back slot, cream enamel and brass, ≤ 25% taller with every 0.5 kit on), the 4 m mast charge ring (dormant dust cap, lit state), relay.2's service cabinet |
| world | the lit pad's warm pool under each mast lamp, the reach table (each 0.5 site heart, friend spot and mast → metres to the dock and to the nearest mast pad) |
| ui | the HUD arc, the battery line keys and Cell Pack II's names and lines (en + vi) |
| audio | the cell notes, the warning two-note, the power-down sigh, the leap fizzle, the charging hum, the crawl's thinner radio |

## 12. Acceptance
- **Calm:** in a 10-minute capture that includes a crawl home, nothing flashes red, nothing alarms, nothing is lost,
  and the crawl reads as a quiet moment (owner judges by screenshots: the crawl frame, the charge frame).
- **Rules:** EditMode tests prove each rule in §2 and each number in §3 on the model (zero GC per step).
- **Lessons:** the golden-path Playthrough logs the lowest charge per trip: home after Tilly's parts (target 20–35%),
  every trip ends above 5% with no crawl, and a mast is the charger on the way to the chasm. It never crawls on the golden path, and it still finishes steps 0–8 within
  budget with the dock and relay.2 charging.
- **Crawl:** a PlayMode test empties the battery mid-cut: the cut completes, then the crawl starts at 35%, Tilly leads
  within ~6 s (55%), and 07 reaches the dock and charges.
- **Leap:** at too little charge the leap refuses at the lip and never leaves the ground.
- **Visible:** the flank lamps read from the chase camera at 30 m; Cell Pack II changes 07's back and the lamp count.
- 60 fps and zero per-frame GC with the battery running.
