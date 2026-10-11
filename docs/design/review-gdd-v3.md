# Review of GDD v3 (2026-10-11): battery, feasibility, visuals and level, calm

> Author: the game design session, as critic. Proposals to the Director and the owner, not decisions.
>
> Sources:
> - `docs/design/gdd-v3.md`;
> - the basin as built: `review-2026-10-11-level.md` §1, anchor positions and 07's top speed of 8 m/s;
> - `SalvageTuning` (cuts 2–4 s plus 1 s per unit), `LiftTuning` (stitch 3.5 s), friends' repair 3.5–4 s;
> - M3-19 (lander geometry), M3-18 (look).
>
> Distances are from home (the dock) unless stated. Drive cost assumes flat ground: the basin floor varies by only a
> few metres, so the uphill ×1.5 rule barely applies before the canyon.

## 1. Battery tuning against the basin

### 1.1 The draft's numbers on the real trips
With **4 cells × 150 m = 600 m** and "one site's salvage ≈ 0.5–1 cell":

| Trip (0.5 step) | Round-trip driving | Work | Cells used | Charge left at home | Felt? |
|---|---|---|---|---|---|
| Depot (S1) | 110 m | 4 cuts ≈ 25 s | 0.7 + 0.6 = **1.3** | 67% | no lesson (right: first trip) |
| Tilly + her parts (S2) | 120–220 m + ~90 m of part detours | stitch 3.5 s + parts | ~2.2 | **45%** | §3.8's "first I need more range" **doesn't fire**: the gauge never reaches 30% |
| Chasm look + relay.2 (S3) | 298 m out | stitch/feed ~10 s | **2.0** on arrival | arrives at **50%**, not "on its last cell" | the lesson works only if the player lingers |
| Garage (S3) | 250 m | cuts + dig ≈ 35 s | 1.7 + 0.9 = 2.6 | 35% | mild |
| Drill (S6) | 296 m | ≈ 35 s | 2.0 + 0.9 = 2.9 | 28% | by S6 it has 6 cells: not felt |
| Kestrel (S6) | 370 m | ≈ 35 s | 2.5 + 0.9 = 3.4 | 15% (4 cells) | the best natural range gate, and the draft doesn't use it |

**Verdict.** At 150 m per cell, none of the three planned "I need more range" moments fires on the critical path
for a player who doesn't wander. 4 cells × 150 m is generous for a basin whose 0.5 sites all lie within 185 m.

### 1.2 Two hidden costs in the draft
- **Towing is beam work in the table** (1 cell ≈ 40 s). A relic towed home from the Kestrel is ~45 s of beam on top
  of the drive: **+1.1 cells**. Towing then becomes the most expensive thing in the game, punishing the game's best
  "every touch sings" verb. **Cost towing as driving (×1.25), never as beam.**
- **Beam at 40 s per cell** is fine for cutting (a 4-cut site ≈ 25 s ≈ 0.6 cell). It is harsh for the heavy tools
  (20 s per cell): a Thermal Cutter vault would cost a whole cell per panel. Revisit in 0.7. For 0.5 it doesn't bite.

### 1.3 Proposed numbers (for `BatteryTuning`)
| Value | Draft | Proposed | Why |
|---|---|---|---|
| Drive, flat | 1 cell ≈ 150 m | **1 cell ≈ 120 m** | the three lessons fire on the critical path (table below) |
| Tow | beam rate | **drive cost × 1.25, no beam drain** | protect the best verb |
| Beam work | 1 cell ≈ 40 s | **1 cell ≈ 60 s** | a site's work stays ≈ 0.4–0.6 cell, so driving is what range is about |
| Leap | ¼ cell | ¼ cell | fine |
| Crawl speed | 35% (~2.3 m/s) | 35% | fine; a ≤ 300 m crawl is ≤ ~130 s |
| Solar trickle | 1 cell / 3 min, to full | **to a 1-cell floor only** | see §4: a full refill makes waiting a strategy, and that turns stillness into a timer |
| Starting cells | 4 | 4 | keep the gauge countable |

The same trips at 1 cell ≈ 120 m, tow ×1.25, beam 60 s per cell:

| Trip | Cells | Left at home or on arrival | The moment |
|---|---|---|---|
| Depot | 0.9 + 0.4 = 1.3 | 67% | none (right) |
| Tilly + parts | 2.6–2.9 | **~28% on the way home, about 60 m out** | **the 30% warning fires within sight of the dock.** Lesson: the dock charges. A crawl from there takes ~25 s. |
| Chasm (S3) | 2.5 on arrival | **~38% at the lip** | the warning fires as the player looks across, and relay.2's pillar rises 20 m away. Lesson: masts charge. |
| Garage | 2.1 + 0.6 | 33% | close to the warning: good tension, no crawl |
| Kestrel before Cell Pack II | 3.1 + 0.6 + tow | **over budget** | visible from the first frame, but reachable with work only after lighting the mound mast (60 m away) or fitting Cell Pack II. A real range gate, seen first (ruling 10's spirit). |

### 1.4 Charger spacing and crawls
- The draft's "chargers ≤ 250 m apart" is **not true of the built basin.** Home to relay.2 is **278 m**; relay.1 is a
  western spur, 218 m from home and 346 m from relay.0. Measured gaps: home–relay.0 150 m; relay.0–relay.2 204 m;
  relay.2–relay.3 ~190 m through the canyon.
- **Proposal:** state the rule as "**no point on a 0.5 trip is more than 300 m from a charger; the longest crawl is
  ≤ 130 s**". It is honest, and it holds. Don't move relay.2: the canyon mouth is the mast's story.
- **Longest possible 0.5 crawls:** from the chasm lip with relay.2 unlit (298 m, ~130 s), and from the Kestrel with
  relay.0 unlit (185 m, ~80 s). Both are fine with §2's Tilly change.
- **Dead walks:** a crawl across an empty plain *is* a dead walk unless it is staged. Make the crawl a pillar-6
  moment:
  - the camera eases wider;
  - the radio thins;
  - motes hang in 07's dimmed lamp;
  - the charger's pillar sits ahead.

  The level review's L2 road posts and L1 site silhouettes give the eye something on the way.

## 2. Feasibility against what's built

| Item | Verdict | Cost | Notes |
|---|---|---|---|
| Battery model (`IRoverBattery`, `BatteryTuning`, EditMode tests) | NEW | **S–M** | pure C#; costs are hooks on existing drive, beam and leap events |
| Dock as charger, evening fill | CHANGE | S | the dock already has a park/rest flow (`ChargingDock`); add the fill and the cell notes |
| Mast charge pads | CHANGE | S (gameplay) + S (art) | the relay anchors already reserve a 3 m flat pad. 07 is 2.3 m long, so **widen to 4 m**. |
| Crawl (speed cap, abilities asleep) | NEW | M | Rover caps speed; `IRoverAbilities` can sleep the beam, leap and boost |
| Gauge lamps on the flank | NEW | M | the lamp count grows with packs; build it as a strip with N sockets, not one mesh per count |
| HUD arc | NEW | S | UI already reveals the HUD per verb |
| **Tilly's tow** | NEW | **L** | a new friend behaviour, a tow-line render, and 07 driven by an outside force on rough ground. The draft's most expensive item. See §5, change 3, for a cheap version. |
| Cell Pack II | NEW kit | S–M | the kit pipeline exists (bay, plotter, install moment) |
| Blueprints decoded in the lab, then drawn by the plotter | CHANGE | M | it reroutes the existing `BlueprintDelivered` flow through a new reader station. It needs the lab to exist (L, below). |
| Hover-Jump found at the chasm lip | CHANGE | S | data plus a tube placement; the tube already exists |
| The lab interior and the lander's growth (M3-19) | NEW | **L** | the biggest art job in 0.5: lander rebuild, interior, ring camera |
| Tech board with the protocol header | NEW | M | show only 0.5's 9 nodes. Don't build the 30-node board until 0.6 needs it. |
| Boot readout in the opening | NEW | S | UI text over the lens; wordless alternative in §4 |
| P1 parts renamed to components | CHANGE | S | text and keys |
| Wide Sonar to 0.6 | CHANGE | none in 0.5 | |
| `ITechTree`, `TechNodeUnlocked` | NEW | S | contracts only for 0.5 |

**0.5 scope warning.** The draft grows 0.5 from M3-16's 50–60 min to ~100 min, and adds the battery, the lab, the
lander rebuild and a new kit. Three L items (lander and lab, Tilly's tow, M3-18's look) in one version is the risk.
Recommendation: **0.5 ships the battery core, chargers, crawl, Cell Pack II and the lab. Tilly's tow ships in its
cheap form (§5, change 3).**

## 3. Visuals and level

**The lab interior.** Four stations around the M3-19 turntable (reader, tech board, survey chart, keepsake shelf)
read better than four crew bunks, because each has a verb. Two conditions:
- **The purpose must show from outside.** The big porthole shows the tech board's lamps and the chart's glow. The
  porthole gains one lamp per unlocked node, so the lander's face counts 07's growth at 30 m (ruling 11).
- **The reader is a machine 07 feeds with its beam.** For example, a tube slot at lens height, a spinning reel and
  a paper tape running to the board. It must never read as a desk.

**Mast charge pads.**
- A worn painted ring, 4 m across, with a low ramp lip (ruling 12) at the mast's foot.
- Dormant: a dust-capped ring, visible from 50 m. It is a promise: "this was a charger".
- Lit: the mast lamp pours a warm pool onto it (M3-18 V3), so a charger is a warm floor you can see from 200 m at
  night.
- At relay.2, place the pad on the home side of the mast, never on the chasm run-up line.

**07's silhouette budget.** The draft lists ~14 kit pieces by 0.8 (drums, rack, two cell modules, lamp bar, dish,
tow mast, two wings, floodlights, vanes, fins, range pack, winch, core window, tracker). On a 2.3 m rover that is the
"busy cluster" problem again (visual review, owner verdict). "≤ 25% taller, ≤ 15% wider" can't hold with flank drums
plus vanes plus two wings. Proposal: **kit slots.**
- Six slots: nose, roof, back, flanks, rear, wheels. Each shows one piece at a time.
- A later kit replaces or merges into its slot's piece:
  - Floodlights replace the lamp bar.
  - Cell modules grow one battery spine on the back.
  - The range pack merges into that spine.
  - The tracker scope sits on the dish mount.
- Wings and vanes fold flat while driving and open when 07 is still or leaping.
- Gauge lamps: one lamp per cell up to 8. Beyond that, each pack segment gets a brighter, longer lamp, so the strip
  stays countable.

**Level.** At 120 m per cell the network shape works as built. The level review's L3 (garage to 228°/140 m) becomes
more valuable: the garage sits 85 m from relay.1, so the west gets a two-stop range arm.

## 4. What breaks the calm or "purpose must read"
1. **The trickle to full** turns stillness into waiting for charge, which is a timer by another name and against
   pillar 6. A floor-only trickle keeps "you can always get home" without making the player wait to play.
2. **"Call Tilly"** while crawling is a decision under pressure. **She should come on her own** after ~5 s of crawl
   (question 1: yes, by herself).
3. **Verbs interrupted by empty** would break "nothing snaps". **Rule: a verb in progress always completes** on the
   reserve cell (a cut, stitch, dig, feed or tow). The crawl starts after. A leap that can't be paid for never charges
   (the coils fizzle softly at the lip, and 07 glances at the gauge). It never fails mid-air.
4. **The 30% line**, "The mound mast can top you up", names a charger the player may never have lit. **Name only lit
   chargers or home.** For an unlit one, raise its pillar with no text.
5. **The boot readout** ("EARTH SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN RECOVERY") is a goal in words at second
   five, before "aww". **Keep it wordless:** the lens flickers, the radio jingle pierces the static, and the Peak's red
   light blinks in time. The protocol is first read as text on the tech board's header, at the first lift ride
   (question 2).
6. **Purpose:** three places per unlock (the lab decodes, the bay draws, the bay builds) risks reading as paperwork.
   Show the hand-off: when decoding ends, a lamp pulse runs from the lab down the lander leg along the ground cable to
   the bay. The plotter starts drawing while 07 rides down. The player watches one continuous action.
7. **Charge as a constant reading** during salvage would split attention. The HUD arc hides while 07 works the beam,
   and the flank lamps carry it.

## 5. Top 5 changes, ranked
| # | Change | Why | Box | Cost |
|---|---|---|---|---|
| 1 | **1 cell ≈ 120 m; tow = drive × 1.25 with no beam drain; beam 1 cell ≈ 60 s.** Keep 4 cells to start. | The three range lessons (Tilly, the chasm, the Kestrel) fire on the critical path instead of never. Towing stays joyful. | gameplay (data) | S |
| 2 | **Never interrupt, never trap:** a verb in progress completes; a leap refuses at the lip, never mid-air; the trickle tops up only to a 1-cell floor; the 30% line names only lit chargers. | Calm (pillar 2), "nothing snaps". Stillness stays a reward. | gameplay + rover | S |
| 3 | **Tilly comes by herself and leads with her lamp** (the crawl goes from 35% to 55%, no tow line). The tow line can follow in 0.6 if the owner wants the image. | Turns the most expensive item (L) into an M that reuses her follow flight. The rescue still reads as a friend. | gameplay + rover | M (instead of L) |
| 4 | **Kit slots for 07's silhouette:** six slots, one visible piece each; later kits replace or merge; wings and vanes fold while driving; gauge lamps capped at 8 countable lamps. | Keeps 07 readable and small (pillar 6, owner's "busy" verdict) through 0.8. | art (+ Director's kit table) | S now, saves L later |
| 5 | **Honest charger rule and a scoped 0.5:** "≤ 300 m from a charger, crawl ≤ 130 s"; 4 m pads; the tech board shows 0.5's nodes only; the opening stays wordless; the decode-to-plotter hand-off is one visible pulse. | The rule matches the built basin. 0.5 stays shippable. Purpose reads without words. | Director (docs) + art + gameplay | S |

## 6. KEEP / CHANGE / CUT sanity check
- I agree with every KEEP and CUT in §10, notably cutting the outdoor shelf and the deck lookout, and keeping the
  power spine.
- One CHANGE to question: **"Hover-Jump is found at the chasm's lip instead of pre-pinned."** It works only if the
  tube at the lip is *seen before* the player can cross, and it is: the lip is on the near side. Fine. But don't also
  keep Kenji's pre-pinned sheet, or the bay's first sheet becomes a duplicate.
- The VISION edits are consistent. Ruling 15 should also carry change 2's "a verb in progress completes" sentence.
