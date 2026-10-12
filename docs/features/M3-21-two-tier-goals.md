# M3-21: Two-tier goals: four recovery targets, everything else free

> Owner: Director (design). Version: **0.5** (the rows and rules; later versions light more lamps). Source:
> `docs/design/gdd-v3.md` §8, §4.1, §4.3 and §9, approved by the owner on 2026-10-11 (decision 5: "not linear; four
> main targets with a progress marker; everything else free, in any order"). VISION ruling 16 is the law here.

## 1. The two tiers
**Tier 1, directed: the four main recovery targets.** When the crew evacuated, they left 07 the Lumen Recovery
Protocol: if Earth's signal is ever heard, restore the station and answer. The opening's boot readout starts it
("EARTH SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN RECOVERY", O5). Its four targets are the only things the game
ever names as goals:

| # | Target | Done when | Lamps | In 0.5 |
|---|---|---|---|---|
| 1 | **Power the station** | tower L1, L2, L3 | 3 | all three can be lit: the first main target done (S5) |
| 2 | **Link the network to the Peak** | a chain of lit masts from home to the Peak's foot, by either mid region's route | one per chain mast, plus a larger last lamp for the Peak's foot | the basin's chain masts can be lit (S6: "Link begins"); the rest stay dark |
| 3 | **Repair the great dish** | Atlas lifts it, Superconductor, the Power Cell, the Star Tracker aims it | 4 | dark |
| 4 | **Answer Earth** | the broadcast | 1 | dark |

**Tier 2, free: everything else.** Sites, finds, blueprints, kits, friends, tapes, logs, keepsakes and secrets are
found by roaming, in any order. **There is no list of them anywhere** and no text ever hints at them. The world
points by itself: glints, signal pillars, Bell's signals, Tilly's spotting, the chart's pencil notes, a dormant pad.

## 2. Where tier 1 shows
- **The recovery panel** is the header of the lab's tech board (M3-19): the protocol's name in the crew's terminal
  type, then four rows, each a target's name and its lamps. Dark lamps are dark glass; lit lamps are warm amber; a
  completed row's lamps hold a steady glow. It is first read at the first lift ride; the player never needs it to
  play.
- **The survey chart** (M3-19's front station) draws the Link's line in ink from home through each lit chain mast,
  pointing toward the Peak, and shows each charger's circle (the dock and lit masts, M3-20) and, from 0.6, the pads.
- **From outside:** the lander's porthole counts known tech nodes (M3-19 §3); the Peak's beacon stays the arc's
  promise in every frame.
- **Completion moment:** when a row completes, its lamps pulse once from left to right with a warm rising chord (in
  key), a ticker line names the target ("Power restored."), and the next dock evening holds a beat longer on the
  tower. Never a fanfare, never a pop-up.

## 3. The "next" line becomes two-tier (M3-16 "Gentle guidance", P8)
- **Teaching steps (fixed order, ~first 30 min):** the list in `docs/design/first-hour-v2.md` §4 (the wake, the
  tower, the depot's cuts, the tower's repair, Tilly's pillar and her buried part, the dock's first charge, Kenji's
  garage and its port, the tape deck, the duck, the lab's lift). Each teaches one verb, and the next line shows at once
  and invites it (M3-24 B).
- **After the teaching steps, the next line speaks only of tier 1:** the current target's next need, naming only
  what the player has already seen. Power first ("The tower needs its last part."), then Link ("The mound mast is
  still dark."). Once nothing in tier 1 is reachable in this version, the line stays empty.
- **It never names tier 2.** No line ever says "find the Kestrel", "a blueprint is in…", or "visit Bell".
- P8 stands once the teaching steps are over: the line waits ~2 min without progress, never shows while stargazing, and
  can be switched off in Settings. The one battery line at 30% (M3-20) is separate.

## 4. The tech tree (0.5's nine nodes)
| Node | Kind | Unknown → Decoded | → Built |
|---|---|---|---|
| Tower L1, L2, L3 | station | (skips Decoded) | the level lands |
| Relay network | station | (skips Decoded) | the first mast is lit |
| Hover-Jump | kit | its tube, found at the **chasm's near lip** (Kenji's pre-pinned sheet is removed), decoded at the reader | fitted in the bay |
| Cargo Cradle | kit | the garage's heart | fitted |
| Warm Headlamp | kit | the Kestrel's heart | fitted |
| Capacitor drums (Boost Coils) | kit | the drill's heart | fitted |
| Cell Pack II | kit | relay.2's service cabinet (M3-20) | fitted |

- `ITechTree.StateOf` and `KnownCount` (decoded or built) are derived from what the save already holds (tower level,
  lit masts, kits owned, tubes decoded), so no new save data is needed beyond "decoded, not yet built".
- The board shows only these nine pins in 0.5; later versions add their nodes (GDD v3 §4.3).
- The bay can build a kit only once it is Decoded (as the plotter's pinned sheets do today).

## 5. Free order, and what that means for the golden path
- **Within the basin and canyon** sites, finds, logs, tapes and masts can be done in any order. Nothing in tier 2
  gates tier 1 except through real needs (a tower level's component, the leap for the canyon, range for the Kestrel).
- **Recipes never loop:** no kit needs a kit that needs it; every 0.5 recipe needs only T1 materials and finds from the
  basin and canyon.
- **The golden-path Playthrough** keeps one typical path, GDD v3 §9's S1–S6, and checks the tier rules on the way: the
  next line never names a tier-2 thing, the panel's Power row lights at each level, and the Link row lights at the
  mound mast.
- **A second, short PlayMode test** takes a different order after the teaching steps (for example the drill before
  the chasm) and checks that nothing blocks and the next line stays tier-1 only.

## 6. Renames carried by this spec
- **P1 story parts become components** (GDD v3 §10): the tower's coil, transformer and relay spare are "components"
  in text and keys, and in code (`StoryPartFound`/`StoryPartDelivered` → `ComponentFound`/`ComponentDelivered`, the
  Gameplay types and every listener in all domains; a mechanical rename, authorised). Localization keys
  `component.<id>.*` (en + vi).

## 7. Contracts (Core, landed by the Director on 2026-10-11)
- `TechNodeId` (the nine 0.5 nodes), `TechNodeState` { Unknown, Decoded, Built }, `ITechTree` (`StateOf`,
  `KnownCount`), `TechNodeChanged(node, state, restored)`.
- `RecoveryTarget` { Power, Link, Dish, Answer }, `IRecoveryProgress` (`LitLamps`, `Lamps`, `IsComplete`),
  `RecoveryProgressChanged(target, litLamps, lamps, restored)`.
- Which masts belong to the Link chain is data on the relay content (a flag per mast), set by the Director: in 0.5,
  relay.0 (the mound, toward the Peak) and relay.1 (the rim shoulder). `Lamps(Link)` is the chain masts in this
  version plus one for the Peak's foot.

## 8. Split (one feature-slice session, "goals", after lab-systems lands M3-19)
| Domain | Delivers |
|---|---|
| gameplay | the tech tree service (plain C# state, EditMode-tested) and `ITechTree`; the recovery progress service and `IRecoveryProgress`; the two-tier next line (StepFlow); Hover-Jump's tube at the chasm's near lip and the pre-pinned sheet removed; the Link flag on relay content; the components rename; the golden path and the free-order test |
| ui | the panel's and the board's labels when looked at (target names, node names, en + vi), the chart's charger circles and the Link's line (the map view), the completion ticker lines |
| audio | the decode chime if lab-systems hasn't made it, the row completion chord |
| art (only if lab-build hasn't) | the board's sheets/lamps and the panel's lamps driven from the events |

## 9. Acceptance
- A tester who plays past the teaching steps can say, without help, "the tower is done; now the masts lead toward the
  Peak", and can name no other goal the game told them.
- The next line is empty or tier-1 at every moment after the teaching steps (Playthrough assertion).
- The recovery panel and the porthole lamps are right after a load (restored events, no moments replayed).
- Two different orders after the teaching steps both reach Power 3/3 with nothing blocked.
