# Opening v3: purpose (proposal, v1)

> **Author:** the game design session. A proposal for the Director and the owner; no code changes.
> - v1 revises draft v0 (94e31db1) against `research/player-guidance.md`.
> - §9 lists what changed and why.
>
> **The owner, after build 95b1dd3a:** "You arrive, drive over and build a few buildings with no clear purpose or
> goal… follow text lines without knowing WHY… after building you can't interact with anything and don't know what
> to do next. Is building those 3 the end of 0.6? … little crates you shoot give 2–3 items you don't understand."
>
> **Canon used:**
> - `opening-v2.md` (§9 rulings, §10 kit, layout);
> - `07-identity.md` (CONTINUE, wake-on-carrier, cells at 50%, the antenna keys the call sign when its receiver
>   locks);
> - `story-v4.md` (the Ark IV sweep woke 07; the Great Dish on the Peak sends the survey home; the Arks' light blinks
>   three short, one long);
> - `maker-understanding.md` (unlocks are held until `SignalWest`);
> - `databank.md`;
> - `tech-eras.md`.
>
> **Citations** refer to `research/player-guidance.md`:
> - **P1–P14:** opening principles (B1);
> - **H1–H5:** the guidance hierarchy (B2);
> - **M:** limit → hint → means → change (B3);
> - **E1–E7:** economy rules (B4);
> - **HO:** handoff (B5);
> - **AP:** anti-patterns (B7).

## 0. Diagnosis (the owner is right)
Our opening shows four of the research's named anti-patterns (AP):
- **Task-list guidance:** objective lines carry every reason.
- **Unmotivated building / tool before need:** pad, solar and mast arrive as a list, before any limit is felt.
- **Loot spam:** small crates give random stacks.
- **Dead end after the tutorial:** a pillar on the horizon, then nothing to do.

Factorio named the same failures in its own tutorial (A7): "gather N because the list says so".

## 1. Player experience
| When | What the player feels | How | Principles |
|---|---|---|---|
| **60 s** | *"Something out there is calling, and I'm too weak to answer."* | 07 wakes in the eclipse. Its lens opens on dark Earth and **one point of light beside it**. A faint carrier ticks **three short, one long**, a soft tone in D pentatonic. 07's antenna lamp tries to key the same pattern and stutters out. 07 tilts its head up. It is dim and slow (§2, link 0). | P1, P2, P10 |
| **5 min** | *"I need power, and that lander is the only warm light here."* | The lander's amber pilot lamp at 165 m is the one warm light in view. A broken charging pad lies beside it. | P3, P6, H1 |
| **30 min** | *"Every build made 07 stronger and the call clearer."* | Each limit lifts in a way you can see and hear: speed, light, the call steadying, the lights holding through the night. Then 07 tries to answer, and can't. | P7, P8, M |
| **Finish** | *"It answered, and now I know where to go."* | The mast transmits; a relay on the rim blinks the pattern back; the Peak's dead dish is the next goal. | P12, HO |
| **Feeling** | A small machine, weak in the dark, earns its strength and its voice. Lonely wonder → quiet pride. | Dark/weak → dawn/capable → night/heard: the eclipse's three-act shape (research C4). | P1 |

## 2. The spine: the call, and a chain of felt limits
**The far goal is the call.** It is seen from the first minute (the point of light by Earth) and heard (three short,
one long). It is never written down. This is Journey's mountain or BotW's castle (P2). 07 doesn't know who is calling.
The player learns it at the game's end, when the Arks' light blinks the same pattern (story v4).

**Lunarion's tie-in.** Three short, one long is **Lumen's own call sign**: 07's antenna keys it when its receiver
locks (07-identity). The fleet's search sweep is calling Lumen by name.

**No lamps, no list.** Each step is driven by a **felt limit** (M), and progress shows **in the world and in the call
itself** (P8). The call's state is the only goal display on 07's face (H2):
1. broken;
2. steady and locked;
3. answered.

| Link | 1 · Feel the limit | 2 · See the hint | 3 · Earn the means | 4 · Feel the change | Why now? (the player's words) |
|---|---|---|---|---|---|
| **0 · Weak** | `STANDBY · RESERVE`: 07 drives at a soft cap, lamps dim, the receiver duty-cycled so the call breaks up | the lander's amber lamp, the only warm light | drive 165 m | arrival; the broken pad and its crate | "I'm weak, and that light is the only thing out here." |
| **1 · Power** | still on reserve | a broken 1980s pad by a crate labelled `CHARGER KIT · ILC-R` | scan the pad (2 fragments), open the crate, build the pad | the dock locks and the cells fill. **The eclipse ends** (the diamond ring). Reserve lifts: full speed, bright lamps. The receiver locks, so the call steadies and **07's antenna keys three short, one long back**. | "I'm almost empty, and I just learned to build a charger." |
| **2 · Steady power** | **the lander's pilot lamp goes out**: its reserve gave one charge, and the pad is dark now | the fallen solar array in the ruins glints in the new sunlight | scan the array (3); the cutter sputters, so fit the igniter from Kenji's `CUTTER SPARES` rack beside it; cut 2 panels free; build solar | `PowerOn`: the pad and base lamps light, the music returns | "My charger died, and that dead array could feed it." |
| **3 · Night** | in last light (the sky holds there; opening-v2 §9) the array's output falls and **the pad charges slower and slower** | the habitat's old power module, whose status lamp blinks as the charge slows | scan it (2); its labelled cable-reel crate and casing; build the bank | `NightReady`: **the first nightfall, and your lights stay on** | "The sun's going, and my charger is fading with it." |
| **4 · Voice** | at night, with the call clear, 07 tries to answer: an F# query chirp goes out, the CRT reads `TX · NO RANGE`, the antenna lamp drops, 07's head lowers | the radio tower ruin, a fallen mast with its dish face-down, a "tall thing that listens" | scan it (3); cut sections, feed cable and dish panels; build the mast. No prompt chips now: **the test without hints** (Portal, A4) | **B12: the answer** (§5) | "It's calling, and I can't answer." |

**Notes:**
- **Link 0 is new.** It is the one new mechanic. `STANDBY · RESERVE` is logical for a machine that survived 36 years
  in standby by design: it stays in low draw until it docks on a live source. Battery stays at canon 50%. Tuning
  bounds:
  - the cap must keep the 165 m drive pleasant (start near 70% top speed; feel gate);
  - **07 cannot die in the opening.** Reserve has a floor, and wear starts after B13 (calm, B6 Flow).
  - **Director call.**
- **Each "why" is said once, by the world:**
  - the lamp going out;
  - the charge slowing;
  - the TX attempt failing.

  These are world states (H1) and 07's own behaviour (H2), never a sentence. This replaces v0's readouts
  (`LANDER RESERVE · 0%`, `NIGHT · 0 W`).

## 3. Guidance on screen
- **No objective lines.** Guidance uses the highest level that works (H1 → H4):
  - the warm light;
  - the glint;
  - the blinking module lamp;
  - prompt chips, **the first time only**, at the object (Scan, Open, Build, Fit, Cut).
- **One element at a time** (owner rule):
  - a prompt chip never shows with the call display;
  - the call display shows only when 07 is still for 2 s, and for 3 s when its state changes.
- **One fallback (H5 as a reminder only):** after 120 s with no progress and 07 still, one dim line restates the
  *current limit's hint*, e.g. "The lander's pilot lamp is still lit." It is never repeated.
- **Playtest metric:** a stall of more than 30 s means the world isn't saying enough (C4).

## 4. Item economy (first 30 min): salvage the thing to build the thing
- **Four item types in all:** Metal, Wiring, Optics, and the igniter (E2: 3–5).
- **Every item:**
  - comes from a source whose look says what's inside (E5);
  - is named with its purpose at pickup, e.g. `Metal 4 · for the charging pad` (E3);
  - is spent within the same link (E6).
- **Costs show on the build ghost**, never in a list (E7).

| Build | Source (fixed contents, E4) | Spent in |
|---|---|---|
| Charging pad (4M 2W) | the `CHARGER KIT · ILC-R` crate, a flat-pack (Metal 4, Wiring 2) | link 1 |
| Igniter | Kenji's `CUTTER SPARES` rack, beside the fallen array: found **at the moment the cutter fails** (P3) | link 2, at once |
| Solar array (3M 2W 1O) | 2 panels cut from the fallen array (Metal 3, Optics 1); its junction box (Wiring 2) | link 2 |
| Battery bank (3M 3W) | the habitat power module: its cable-reel crate (Wiring 3), its casing cut free (Metal 3) | link 3 |
| Antenna mast (4M 3W 2O) | the radio tower ruin: mast sections (Metal 4), feed cable (Wiring 3), dish panels (Optics 2) | link 4 |

- **Cut from the opening:**
  - every small mixed crate, including the wake crate and the midway crate;
  - shooting crates for stacks;
  - every pickup that doesn't serve the current link.
- **Cutting stays a verb only to free a part the player can already see they need** (C4). Every cut piece shows its
  yield before cutting.
- **The start hold is empty** (E1). The first Tab opens on the databank's seeded cards: 07, Kessler–Arai, the eclipse
  (`databank.md` §3).
  - This overrides the earlier "first open = starter items" ruling. **Owner call.**
- **Supply:** about 1.3× the need, so a mistake never strands the player.

## 5. After the mast: the answer, the release, and 0.6
- **B12 · The answer** (a held beat, ~15 s, P7, P8):
  1. The player holds Transmit.
  2. The network pulse runs up the mast and out to the west rim.
  3. **relay.1's dead lamp lights and blinks three short, one long back.**
  4. 07's CRT reads: `ACK · RELAY.1 · ROUTE · PEAK DISH · OFFLINE`.
  5. The pulse glints once on the Peak's dark Great Dish.

  The call is answered by the network: the crew's relays still route it, and the fleet is still out of reach. That
  protects the ending: only the Great Dish can carry the survey home (story v4).
- **B13 · The release** (HO):
  - **Reach (P7):** 07's CRT shows a range ring for the first time. relay.1 is inside it; the Peak is just outside.
    That gate is visible before its key (P11): more cells, from elder 06's Cell Pack at relay.2.
  - **View and lures (P12):** from the rise by the mast, the player sees:
    - relay.1 blinking (**the next goal:** repair it and extend the route);
    - the Peak's dish (**the long goal**);
    - Kestrel-3's white-and-teal wreck (**curiosity**: a foreign maker, `NO LUMEN RECORD`).
  - **Growth on 07 (ruling 11):** the held maker unlocks arrive (`maker-understanding.md` §1). The first is the
    Kessler–Arai headlamp, fitted at the pad: 07 sees farther at night.
  - **Something growing at home (P13):** the mast keeps listening while 07 is away. Each time 07 docks, a few more
    characters of the carrier's ID resolve on the CRT (`A?? IV` → `ARK IV`). It is a reason to come home, and an
    open question (P10). **Story call.**
  - **Guidance steps down:** no fallback lines after B13; the world takes over.
- **Is this the end of 0.6? No.** The opening is the first ~45 minutes. **Proposed 0.6 end beat:**
  1. The route reaches the Peak.
  2. 07 wakes the Great Dish. It slews toward Earth and **jams at 14%** (story v4's figure).
  3. It sends one fragment of the survey. The far carrier **stops sweeping and holds on Lumen** (`CARRIER · LOCKED`).
     Someone heard, and the game's ending is still ahead: freeing the dish, gathering the survey, the Arks' light.
  4. The Peak's view opens the next regions.
  - **Director and story call.**

## 6. Beat table (v3)
| # | Purpose (why now) | On screen | Felt reward | Cut or merge | Principles |
|---|---|---|---|---|---|
| B0 | the call, too weak to answer | POST one shot (canon) → the lens opens on dark Earth and the point of light; the carrier; the antenna's stuttered echo | wonder and a question | **cut** the boot fault list (it moves to the databank) | P1, P2, P10 |
| B1 | weak; the only warm light | drive 165 m on reserve | arrival | **cut** the midway crate | P3, H1 |
| B2 | low power; a broken charger | scan 2 pad fragments → `ILC-R · BUILDABLE` drawn on the CRT | "scanning teaches me" | — | P6, P4 |
| B3 | the charger's parts | open the labelled crate: `Metal 4 · for the charging pad` | understood items | same spot as B2 | E1, E3, E5 |
| B4 | build what I learned | the pad, ghost costs, unfold and weld | "I made it" | — | E7, P5 |
| B5 | charge | dock lock; cells fill; diamond ring; reserve lifts; call locks; 07 keys it back | relief, awe, power felt in the hands | `FirstCharge` | P7, M |
| B6 | the pad died with the lander lamp | scan the fallen array (3); the cutter sputters | a new limit | **cut** the ruins' loose loot | M, P3 |
| B7 | fix my cutter, free the panels | fit the igniter (spares rack), first cut (yield shown) | "my tool works" | — | P6, E6 |
| B8 | feed the charger | build solar → `PowerOn`; music | pride | — | P8 |
| B9 | the charge fades at last light | scan the power module (2); collect, cut; build the bank | foresight | — | M, H1 |
| B10 | — | first nightfall, the lights stay on; then the failed TX attempt | awe, then a pang | `NightReady` | P8, P3 |
| B11 | "it's calling and I can't answer" | scan the tower ruin (3); cut; build the mast. **No chips** | mastery | `SignalWest` | P4, A4 |
| **B12** | answer | Transmit → relay.1 blinks back; `ACK · … PEAK DISH · OFFLINE` | the payoff | **new** | P7, P8 |
| **B13** | where next | range ring; the three lures; the headlamp; the decode ticker | release | **new** | HO, P11–P13 |

## 7. Checklist (research B6, 20 questions per beat)
| Beat | Yes · No · n/a | Each "no", condition or n/a, with the reason or fix |
|---|---|---|
| B0 | 13 · 2 · 4 (+1 cond) | **No:** it changes nothing on 07 or the base. That is by design: it's the hook, and its job is the question (P10). **Cond.:** "target visible" holds only if the lander's lamp is in the first forward view after the lens opens. World must check this. **n/a:** need-before-tool, and the 3 economy questions. |
| B1 | 15 · 1 · 4 | **No:** nothing changes on 07 (a travel beat). Watch the reserve cap's feel (§2). **n/a:** need-before-tool, economy. |
| B2 | 17 · 0 · 3 | **n/a:** economy (fragments aren't items). |
| B3 | 20 · 0 · 0 | — |
| B4 | 18 · 0 · 2 | **n/a:** purpose named at pickup, no random loot (nothing is picked up). |
| B5 | 16 · 0 · 3 (+1 cond) | **Cond.:** "knows what to do in 10 s" needs the fallen array's glint visible from the pad in the new sunlight. World and art must check. **n/a:** economy. |
| B6 | 17 · 0 · 3 | **n/a:** economy. |
| B7 | 19 · 1 · 0 | **No:** two ideas (fit, then cut). The fit is a 2-s socket action, and the cut is its payoff. Playtest it; if players stall, split the beat with a pause. |
| B8 | 18 · 0 · 2 | **n/a:** pickup questions. |
| B9 | 19 · 0 · 0 (+1 cond) | **Cond.:** "say why" depends on players linking the slowing charge to the blinking module lamp. Playtest signal (§8). |
| B10 | 16 · 0 · 3 (+1 cond) | **Cond.:** the tower ruin must read at night (a silhouette against the stars, or within the base lights' reach). Art must check. **n/a:** economy. |
| B11 | 20 · 0 · 0 | Note: no text and no chips; this is the skill test. |
| B12 | 17 · 0 · 3 | **n/a:** economy. |
| B13 | 16 · 1 · 3 | **No:** "one new idea" fails, because four things arrive (ring, lures, headlamp, ticker). Stagger them: the ring and the view first; the headlamp at the next dock; the ticker on the first return home. **n/a:** economy. |

## 8. Alternative core hooks (for the owner's pick page)
| | Hook | For | Against |
|---|---|---|---|
| **A · The call** *(pick)* | a sky light calls Lumen's name; 07 grows from weak to heard | a far goal from minute 1 (P2); every build lifts a felt limit (M); ends on an answer with lures (HO); fits canon and bookends the ending (same pattern) | the call is abstract at first: if the sound and the sky point don't read, the hook is lost. Needs the reserve mechanic (new work). B11's link is the weakest *felt* limit, because it's sad rather than blocking. |
| **B · Survive the first night** | the night is coming, and 07 must store power before it falls | the strongest primal urgency; simplest to read | it fights the sky-follows-progress ruling and the calm (F2); no "why next" after the night; the survival cliché; less wonder |
| **C · Finish K.'s job** | 07's CONTINUE queue shows the job it was on when Kenji parked it: `RESTORE POWER · STEP 1/4` | 07's own logic, ready-made; easy to build | it is still a task list, now on the CRT (AP: task-list guidance); it explains *what*, not a felt *why*; weak hook at 60 s (P1) |

**Pick A.** It is the only one that gives a far goal, felt needs and an open ending at once. It also borrows B's
urgency (the weak start) without the timer.

**Playtest signals:**
- by 5 min, an untold player says "it's trying to answer something";
- at B9, they head to the module lamp within 30 s;
- at B12, they hold Transmit without a chip.

## 9. Changes from v0, and the calls needed
- **Changes, and why:**
  - **The three-lamp panel is gone.** It was a task list on the CRT (AP; P4). The call's own state and the world carry
    progress (P8).
  - **The caller** is now the sky light (canon: the Ark IV sweep), not "a west station". The reply comes from relay.1
    and points at the Peak. Meridian is out of the opening.
  - **Battery:** 22% → canon 50%, plus a felt `STANDBY · RESERVE` limit (M, link 0).
  - **The igniter** moves from the lander crate to the fallen array: need before tool (P3).
  - **Readouts become world states:** the lamp going out, the slowing charge, the failed TX (H1 over H2).
  - **Pickups** now name their purpose, and costs move to the ghost (E3, E7).
  - **B13** now has reach, three lures, a reason to come home, and a stepped-down voice (HO).
  - **The 0.6 end** is now the Peak dish jamming at 14% and the carrier locking on, so it doesn't pre-empt the ending.
- **Owner calls:**
  - the empty start hold;
  - the hook pick (A / B / C).
- **Director calls:**
  - the reserve mechanic and its tuning;
  - no death until B13;
  - the 0.6 end beat.
- **Story calls:**
  - the point of light and its three-short-one-long from B0;
  - the decode ticker;
  - the relay.1 ACK line.
- **World and art checks:**
  - the lander's lamp in the first view;
  - the array's glint visible from the pad;
  - the tower readable at night;
  - a rise by the mast for the B13 view.
