# Opening v3: payoff audit and a leaner opening

> **The owner** (build 2c1ea201d):
> - "I still don't understand the purpose of sending signals back and forth between two antennas inside the same
>   crater."
> - "What's the point of scanning old robots? I don't see any new recipe or blueprint."
>
> **Author:** the game design session, 2026-10-25.
>
> **The bar:**
> - progression first (the spine is 07's growth: upgrade, unlock, craft, go farther);
> - purpose must read;
> - visible progression.
>
> **Supersedes:** where this doc differs, it overrides the R8–R12 parts of `opening-v3-why-chain.md` and of
> `opening-v3-purpose.md`.

## 0. The honest diagnosis
The first half of the opening pays off well. **Every build changes play**: the pad charges 07, solar keeps it charging,
the bank keeps the lights on, and the cutter fix gives a new verb.

The second half pays off in **story and invisible numbers**:
- **decoded call characters;**
- **relay pings;**
- **"maker understanding"**, which moves an invisible counter and then holds its unlock until later.

The antenna chain feels like "talking to yourself" because **nothing the player can use changes**. The owner is
right.

## 1. Beat payoff table (B0–R12, as built)
**Flag** marks a beat whose only payoff is story, lore or an invisible number.

| Beat | What the player does | What they get that changes play | How it's shown | Flag |
|---|---|---|---|---|
| B0 wake | watch | — (the hook) | the boot face | (fine: the hook) |
| B2 drive | drive to the lander | arrives at the first resources | the lander's lamp | — |
| B3 scan the lander fragments | scan 2 | **the Charging Pad blueprint** | a blueprint card, a line drawing | — |
| B4 the kit | pick up | **materials for the pad** | pickup notices with their purpose | — |
| B5 build the pad | build, weld | **a charger** | the pad stands | — |
| B6 charge | dock | **full battery, the reserve cap lifts** (speed, light) | cells fill; the eclipse ends | — |
| B7 the ruins | scan the array; the cutter sputters; take and fit the igniter; cut | **the Solar Array blueprint; the cutter works (a new verb; the igniter on 07); materials** | a card; a part on 07; notices | the airlock read: story only ⚑ |
| B8 build solar | build | **power again** (the pad charges anew) | lamps on; music | — |
| R5 visit a site, dock | scan the seismic station; dock | **nothing usable**: an invisible ILC point, and dusk starts | — | ⚑ |
| B9 the habitat | scan the module; cut the reel; tow; build | **the Battery Bank blueprint; the tow verb; lights through the night** | a card; the rope; night lights | — |
| B10 nightfall (and the failed reply) | wait | awe; "can't transmit" | `TX · NO RANGE` | awe ok; the failed reply is story ⚑ |
| B11 the tower and the mast | scan, cut, build | **the mast** (but it does nothing for play yet) | the mast stands | ⚑ |
| B12 transmit | hold E | **decode characters, an ACK line** | CRT capitals | ⚑ (story only) |
| R10 relay.1 repair | take the spare, fit it, charge there | **a remote charge point** (real!) | its pad charges 07. **The ring growth is not shown.** | half ⚑ |
| R11 the trickle post and 06 | scan, scan, cut the crate | an invisible Kessler–Arai counter → the headlamp unlock | a toast | ⚑ ("no new recipe") |
| R12 the headlamp at the rack | dock, assemble | **a lamp on 07**, but **nothing to use it on yet** (the dark slot isn't built) | the lamp on 07 | ⚑ (a tool with no need) |

**Seven of 16 beats are flagged.** They all sit in the second half.

## 2. The signal chain: make it range, not talk
**The new purpose:**
- **The mast is a network hub. A relay it wakes becomes a charge point. A charge point grows the ring. A bigger ring
  means 07 goes farther.**
- The call, the ACK and the decode ride on top as **seasoning**, on the CRT.

| Step | The felt limit | The means | **The payoff, shown at once** |
|---|---|---|---|
| **Night** | the ring (shown at R3) stops at the crater floor's middle. **Relay 1's dark mast on the west rim is outside it.** | — | — |
| **Mast** | "my range ends here" | scan the tower, build the mast | the mast **lights the network**: a pulse runs from the mast toward the rim |
| **Transmit** (R8) | — | hold E at the mast | **Relay 1 answers: its lamp lights, and on the ring a new node appears**, with a dashed line to it marked "needs a part" (its cabinet) |
| **Relay 1** (R10) | its pad is dead: the feed unit is missing | take the labelled spare from its cabinet; fit it | **its pad charges 07, and the ring visibly grows outward from the new charge point** (an animated expansion, once). It now reaches **the rille notch: the Mare beyond is the next goal** (0.6). |

- **Why two antennas in one crater now reads:** the home mast is the hub, and Relay 1 is the first outpost on the way
  out. Each one lit pushes the ring outward toward the exit.
- **Relay 2 (east) is the next node** on the ring, shown dashed. That makes it an optional, visible "more range"
  goal, not a story errand.

## 3. Scans: every scan gives something visible, at once
**The rule:** a scan gives **a blueprint card**, **a part in the hold**, or **a visible step on a per-maker bar with
its next unlock named**. Otherwise it isn't a scan: it's a free "read" with no scan verb, or it's cut.

- **A per-maker bar, shown on the scan's own card at once:**
  - `KESSLER–ARAI ▮▯ · next: SSR-3 Headlamp`
  - `ILC ▮▮▯ · next: Reflector Beacon`
- At the last step, **the blueprint card shows immediately**. The "held until SignalWest" rule
  (`maker-understanding.md` §1 rule 6) **goes away**: the owner needs to see the reward when it is earned.

| Scan | Today | **New payoff** |
|---|---|---|
| Lander fragments, fallen array, power module, tower | blueprints | keep (cards) |
| **Trickle post** (at the wake) | an invisible K–A point | **the K–A bar 1/2 · next: SSR-3 Headlamp** |
| **06** | an invisible point, a delayed unlock | **K–A 2/2 → the SSR-3 Headlamp card at once**, plus 06's crate holds its parts |
| **Seismic station** | an invisible ILC point | **the ILC bar 1/3 · next: Reflector Beacon** (a buildable road marker that glints in the lamps; a real wayfinding use) |
| **Airlock panel** | a story scan | **a free read** (the text shows when 07 faces it, no scan verb). Story stays, with no fake reward. It also counts **ILC 2/3**. |
| Retroreflector (optional) | — | **ILC 3/3 → the Reflector Beacon card** |
| Scout 3, Kestrel-3 (optional) | lore | VIS and Tycho bar steps, with their next unlocks named |

## 4. A leaner opening: fewer, stronger beats
**Keep:** the wake, the lander, the pad, solar (with the igniter and the cut), the bank and the tow, the night, the
mast, Relay 1.

| Change | Why |
|---|---|
| **Merge R5 into B8:** after `PowerOn`, dusk starts on the **next dock**, wherever 07 has been. The seismic station stays an **optional** scan (the ILC step). | it removes plumbing ("visit a site, then dock") that pays nothing |
| **Cut the failed-reply beat** (`TX · NO RANGE`): the ring stopping short of Relay 1 is the felt limit | one fewer story beat; the limit is now range |
| **B11 + B12 become "the network":** build the mast, transmit, Relay 1 lights as a node (the call and ACK as seasoning, kept short) | gameplay first |
| **R10 is the opening's end beat:** Relay 1 fixed, **the ring grows, the Mare is in reach of the eye**. The release (R9's lures) moves here. | it ends on growth and a visible next place |
| **Move R11–R12 (06, the headlamp, the rack) out of the spine** into hour two, **once the canyon's dark slot exists** (base-home §4). Until then, 06 is an optional scan that shows the K–A bar and the **Headlamp named as next**: a pull, not a chore. | a tool without a need is the owner's "what's the point". Pairing it with the slot fixes that. |
| Drop the decode-character ticker from the end beat's focus (keep it as a CRT detail) | it's an invisible number |

**The lean opening:** 13 beats, every one with a payoff the player can use, ending at about 45 minutes on **"my ring
just grew; the Mare is out there"**.

## 4.1 Balance
**Balance (final, 2026-10-26; story's cl-v3-dayend, measured by the bot's per-beat cells table):**
- **Range:** 220 m per cell, so 880 m driving on 4 cells. The game starts at 60% charge.
- **Both day-end beats happen on the pad:** dusk on the next dock after `PowerOn`, and nightfall while docked.
  07 is released only when full (the dock walls lock; the sky turns in 10–16 s).
- **Every main leg stays at 53% or above:** B9 and B11 start at 100%, and the relay.1 trip starts full (the
  'charge up at the pad first' line). The optional seismic leg is the lowest, at about 23%.
- **The ring:** unlit, it reaches about 440 m from the pad, so the rille notch (489 m) sits about 49 m outside.
  Relay 1 lit grows it by about 145 m, across the notch. The relay.1 round trip is about 70% of a full charge.
- **The Low line:** it fires once, early, on the drive to the lander (the intended felt need). It is suppressed
  near a relay whose spare is reachable.

## 5. Tasks per domain
| Domain | Task | Size |
|---|---|---|
| **Gameplay** | Network nodes (the mast hub, relays as node → charge point); the ring recomputed from all charge points with a grow event; dusk on the next dock after PowerOn (R5 merge); remove the failed-reply beat; the per-maker bar data on scan results (step, total, next unlock) and the immediate unlock (drop the SignalWest hold); the airlock as a free read; the opening's end at Relay 1 relit; R11–R12 off the spine | M–L |
| **UI** | The ring **grow animation** from a new node, with dashed links for "needs a part"; the per-maker bar on the scan card (`MAKER ▮▯ · next: X`); the blueprint card immediately | M |
| **World** | The mast-to-rim network pulse; Relay 1's lamp as a node light | S |
| **Story** | Short CRT lines for the network (ACK kept brief); the airlock read text unchanged; maker bar names in en and vi | S |
| **Tools/bot** | Update the beats (B10 failed reply gone; R5 merged; R10 the end; R11–R12 out of the spine) and the naive declarations | S–M |
| **Design (me)** | Update `opening-v3-purpose.md` §5–§6 and `maker-understanding.md` rule 6 once this is accepted | S |

## 6. Playtest signal
- At the mast, a first-time player says **"it lights up the relays so I can charge out there"**.
- After any robot scan, they can name what it gave them, or what it's building toward.
- The opening ends with the player looking at the bigger ring and the rille.
