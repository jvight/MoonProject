# Opening v3: purpose (proposal)

> Author: the game design session. A proposal for the Director and the owner; no code changes.
>
> The owner, after playing build 95b1dd3a: "The opening doesn't feel engaging. You arrive, drive over and build a few
> buildings with no clear purpose or goal. The player just follows text lines without knowing WHY. After building the
> first buildings you can't interact with anything and don't know what to do next. Is building those 3 the end of
> 0.6? We also hand out too many items early: little crates you shoot give 2–3 items you don't understand."
>
> Built on: `opening-v2.md` (its §9 rulings and §10 start kit), `07-identity.md`, story v4, `hook-review.md`,
> `maker-understanding.md` (unlocks held until `SignalWest`), `databank.md`, `blueprint-fit.md`.
>
> Respects: one guidance element, plain text, no floating loot, scan fragments, the base-building pivot, visible
> progression on 07, the sky follows progress (no clock).

## 0. Why it doesn't work today (the owner is right)
| What the owner felt | The cause |
|---|---|
| "No clear purpose or goal" | The goal (Power → Link → Dish → Answer) is a readout and a word list. Nothing the player can *see or hear* wants something from them. |
| "Following text lines without knowing WHY" | Each beat's reason lives in its objective line ("Build a charging pad…"). The line says *what*, never *why*, and it is the only voice. |
| "After the first buildings you can't do anything" | B8–B11 end in a pillar on the horizon. The base has no use after it's built, and every unlock is held until later. |
| "Too many items you don't understand" | Small crates are cut for mixed stacks. Materials arrive before the player knows what they're for. |

## 1. Player experience (skill `player-experience`)
| When | What the player should feel and think |
|---|---|
| **The first 60 s** | *"Something is calling, and 07 can hear it but can't answer."* The eclipse wake (it exists). Then 07's CRT shows **a live waveform**: a faint carrier repeating from the west, and one word under it: `ANSWER?` with three dark lamps. The antenna tip flickers each time the carrier arrives. 07 turns its head toward the west. |
| **At 5 min** | *"I know what I'm doing and why."* The lamps are the goal: **POWER · NIGHT · TRANSMIT**. The first need is concrete: 07's battery reads 22% and falling, and the lander 165 m away has a charger kit. |
| **What makes them finish** | Each lamp lights *because of something the player built*. The last lamp lights, the player presses one button, and **07 answers**. Then **something answers back**. |
| **Feeling** | **A small machine trying to answer a call in the dark, and managing it.** Lonely wonder → quiet pride (north-star). |

## 2. The one goal: answer the call (my pick, §6)
**The CALL panel** on 07's CRT:
- the incoming waveform;
- three lamps;
- one word.

It is **the goal itself, shown diegetically**: no task list and no text instructions.

| Lamp | Lights when | Why the world says it needs this (said once, by 07 or the world) |
|---|---|---|
| **POWER** | a charger is built **and** a steady source feeds it (pad + solar) | the battery is dying (22%), and the lander's old reserve runs out after one charge (`LANDER RESERVE · 0%` on the CRT) |
| **NIGHT** | a battery bank holds a night's charge | the Sun lowers after power-on, and the CRT's power line shows the night's need going unmet (`NIGHT · 0 W`) |
| **TRANSMIT** | an antenna mast stands, powered | the antenna mount reads `RECEIVE ONLY` (the boot readout): 07 can hear but not speak |

**Rules:**
- **When the panel shows:** on 07's face when 07 is still for 2 s, and for 3 s whenever a lamp changes. It is the
  one guidance element while it shows; it never shows with a prompt chip.
- **The objective lines go.** "How" is taught by **prompt chips** the first time only (Scan, Build, Fit…), and by the
  world (brackets on fragments, a lit apron).
  - **One fallback:** after 120 s with no progress and 07 still, a single dim line names the *next lamp's* need, e.g.
    "The lander's cargo had a charger kit."
- **Why, said once per step,** by 07's own readouts (the battery line, `LANDER RESERVE · 0%`, `NIGHT · 0 W`,
  `RECEIVE ONLY`) or by the world (the Sun lowering, the waveform). It is never an objective sentence.

## 3. The item economy for the first 30 minutes: "salvage the thing to build the thing"
**Each material comes from the old version of what you're about to build,** so it never needs explaining:
- the pad's kit comes from a charger crate;
- the solar array's cells come from the fallen array;
- the mast comes from the radio tower ruin.

| Need | Exactly where it comes from | What the player understands |
|---|---|---|
| Charging pad (4M 2W) | **one labelled crate** in the lander's open cargo bay: `CHARGER KIT · ILC-R` (Metal 4, Wiring 2 inside, as a flat-pack) plus the pad's 2 scan fragments beside it | "This crate holds a charger" |
| Igniter (a cutter repair part) | a toolbox in the same bay, labelled `CUTTER SPARES` | "My cutter is broken (boot: `NO IGNITER`); here is the part" |
| Solar array (3M 2W 1O) | **the fallen solar array** in the ruins: 3 fragments to scan, then cut 2 intact panels free (the first use of the fixed cutter): Optics 1, Metal 3 | "I'm making a solar array from a dead one" |
| Battery bank (3M 3W) | the habitat's power module: 2 fragments; its cable reel crate (Wiring 3) and its casing (Metal 3) | "Its old batteries teach mine" |
| Antenna mast (4M 3W 2O) | the radio tower ruin: 3 fragments; its mast sections (Metal 4), its feed cable (Wiring 3), its dish panels (Optics 2) | "I'm rebuilding its antenna for me" |

- **Cut from the opening:** every small sealed crate with mixed stacks, the wake-point crate and the midway debris
  crate, and every pickup that isn't for the current lamp.
- **The start hold is empty** (not Metal 2, Wiring 1): nothing arrives before it's needed. This changes the earlier
  "first open = starter items" ruling: the first Tab open shows 07's faults (`NO IGNITER`, `JAMMED`), not stock.
  **An owner call.**
- **Every cut piece shows its yield before cutting** (the brackets read `METAL 3`), so a cut is a choice, not a
  gamble.
- **Totals by B11:** 3 materials, 5 named sources, ~10 cuts. The supply is about 1.3× the need (enough slack for a
  mistake, no clutter).

## 4. After the mast (B11): the payoff, the next goal, things to do
- **The answer** (the payoff, a held beat, ~15 s):
  1. All three lamps are lit; the player holds `Transmit`.
  2. 07's antenna lights, the network pulse runs up the mast, and the waveform on the CRT changes from the faint
     repeating carrier **to a reply**: `ACK · UNIT 07 · LINK REQUEST · RELAY W-1 OFFLINE`.
  3. A pillar rises over the west rim at the dead relay mast.
  4. Operational, not sentimental: an automated station (story v4's Meridian, still running its scheduler) answers a
     Lumen unit. **The player learns someone else is out there, and it isn't human.**
- **The next goal (the panel's new line):** **LINK**, with one lamp: `RELAY W-1`. Reach the west relay, repair it and
  carry the call further. That opens the road to the Long Mare.
- **Things to do at the base right after** (each gives a visible change on 07, ruling 11):
  1. **The held unlocks arrive** (maker-understanding §1 rule 6): the first Kessler–Arai understanding lets 07 bolt
     an elder's old headlamp onto its visor at the pad. A visible upgrade.
  2. **Free the solar wing** (`JAMMED` on the boot readout): a part from the habitat, fitted at the pad. Now 07 trickles
     in sunlight.
  3. **The databank on 07's face** now holds the night's scans (the lander, the ruins, `SUIT 01-04 · OFFLINE`).
  4. **Lay lamp posts or a cable run** toward the ruins (craft catalogue's expressive builds). Optional.
- **Is this the end of 0.6? No.**
  - **The opening is the start of the game** (~45 min). It ends on the reply and the LINK goal.
  - **0.6's end beat should be:** the relay chain reaches the Long Mare's comms dish, 07 transmits **the first piece
    of the survey** (Ines's core logger), and the reply comes from the Arks: `ARK IV · LUMEN · RECEIVED 1/6 · KEEP
    SENDING`. **Proof that the call matters**, and the arc's goal made real.

## 5. Beat table (v3)
| # | Purpose (the panel or world line) | What changes on screen | Felt reward | Cut or merge |
|---|---|---|---|---|
| B0 | — (the eclipse wake) | the lens opens on dark Earth and the Ark point | wonder | keep |
| B1 | `ANSWER?` + 3 dark lamps; battery 22% | the waveform pulses; 07 turns toward the west | "it wants to answer" | **replaces** the long boot list (it moves to the databank; only the faults show) |
| B2 | (world) the lander's amber light; the cable | drive 165 m | approach | keep; **cut** the midway crate |
| B3 | (world) brackets on 2 fragments by the `CHARGER KIT` crate | scan → a line drawing on the CRT: `ILC-R · BUILDABLE` | "scanning teaches me" | keep |
| B4 | (world) the open crate | collect the kit (Metal 4, Wiring 2) and the igniter | understood items only | **merge** into B3's spot |
| B5 | battery Low (07's fault tone) | build the pad (the hatch press, the weld) | "I made my charger" | keep |
| B6 | the POWER lamp half lit; `LANDER RESERVE · 0%` | the dock lock, cells filling; **the eclipse ends** (diamond ring); fit the igniter while charging (a small field part, it fits anywhere) | relief, awe, my cutter works | keep |
| B7 | POWER needs a steady source | ruins: scan the fallen array, cut its panels; the airlock reads `SUIT 01-04 · OFFLINE` | the first story fact | keep; **cut** the locker and crate loot |
| B8 | **POWER lamp lit** | build solar; power on; music returns | pride | keep |
| B9 | `NIGHT · 0 W` as the Sun lowers | scan the habitat power module; build the bank | foresight | keep |
| B10 | **NIGHT lamp lit** | the first nightfall: your lights come on | awe | keep |
| B11 | `RECEIVE ONLY` → TRANSMIT | scan the tower ruin; build the mast | — | keep |
| **B12** | **all lamps lit: Transmit** | **07 answers; the reply; a pillar in the west** | **the payoff** | **new** |
| **B13** | LINK · RELAY W-1 | held unlocks (the headlamp), the solar wing freed, the databank | growth on 07 | **new** |

## 6. Alternative core hooks (for the pick page)
| | Hook | Strength | Weakness |
|---|---|---|---|
| **A · Answer the call** *(my pick)* | a carrier repeats from the west; 07 can hear but not speak; build power, night storage and a transmitter to answer; something answers back | the goal is felt from the first minute (sound, waveform, head turn); every build serves it; it ends on a reply that opens the next goal; it fits story v4 (the carrier woke 07) | the reply's sender must be set up well (Meridian's automation) to avoid confusion |
| **B · Survive the first night** | the battery dies against the coming night; build to stay alive | instant, primal urgency | the survival cliché; a calm game turned stressful; after the night there is no "why next"; it fights "the sky follows progress" |
| **C · Finish K.'s job** | 07's queue shows the task it was doing when Kenji set CONTINUE (`JOB 7 · RESTORE POWER · STEP 1/4`) | very logical, 07's own reason | still a task list, now on the CRT; weak wonder; risks reading as the same "follow lines" |

**Pick A.**
- It answers all four of the owner's points at once: a goal from minute 1; each step's reason shown by the world; a
  payoff and a next goal; items tied to what they build.
- It borrows B's felt pressure (22% battery) and C's logic (07's own receive-only antenna) without their weaknesses.
- **Playtest signal:** within 5 minutes, a new player says "it's trying to answer something", and at B12 they hold
  Transmit before reading anything.
