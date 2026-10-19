# Opening v3: purpose (FINAL)

> **Status:** final design, 2026-10-20. Author: the game design session.
> - **Director rulings 1–6 (owner-delegated):** hook "The call, made concrete"; the reserve; empty start hold; the tow;
>   the socket rack; the 0.6 end beat.
> - **The build plan** is `opening-v3-plan.md`.
> - **This supersedes:** v0 (94e31db1), v1 (2f0c180c), and `opening-v2.md` §2–§4 where they differ.
>   Opening-v2 §9–§10 stand except where §8 says otherwise.
>
> **Citations:**
> - **P1–P14, H1–H5, M, E1–E7, HO, AP:** `research/player-guidance.md`.
> - **C1–C8, R1–R11:** `research/genre-survival-craft.md`.
>
> **Canon:** `07-identity.md`, `story-v4.md`, `maker-understanding.md`, `databank.md`, `tech-eras.md`.

## 0. The answer to the owner
The opening had no felt goal, said *what* but never *why*, ended in a dead end, and handed out unexplained items
(AP: task-list guidance, unmotivated building, loot spam, dead end). v3 fixes all four:
- **One felt goal from minute 1:** a call from the sky that 07 hears and can't answer. It is never written down.
- **Every step lifts a limit the player has just felt** (M). The "why" is a world state, said once.
- **The answer opens the basin:** three lures, a range ring, a goal board at home, and a planned hour two.
- **Items:** three materials and one igniter, each from the old version of what it builds.

**Is building three things the end of 0.6? No.** The opening (R1–R9, about 60 minutes) is the start of the game.
0.6 ends at the Peak (§6).

## 1. Player experience
| When | The player feels | Principles |
|---|---|---|
| 60 s | "Something up there is calling, and I'm too weak to answer." | P1, P2, S1–S2 |
| 5 min | "I need power, and that lander is the only warm light." | P3, S3 |
| 30 min | "07 is getting stronger, and the call clearer. And I chose where to go." | P7, C1 miss 3 |
| 60 min | "It answered, and now I can see where to go next." | P12, HO, S8 |
| Session end | "When I dock, a little more of the call decodes. Next time I'll reach 06." | P13, C6 |
| **Feeling** | A small machine, weak in the dark, earns its strength and its voice. Lonely wonder becomes quiet pride. | P1 |

## 2. The call, made concrete (C7 risk 1, ruling 1)
**The source.** A point of light beside dark Earth: the Ark IV sweep (canon). It is the far goal in view at minute 1
(P2) and the bookend for the ending's Ark light.

**The signal.** Three short, one long: Lumen's own call sign, which 07's antenna keys when its receiver locks
(07-identity). The fleet is calling the station by name. It plays as a soft tone in D pentatonic.

**A bearing (C5 rule 2):**
- 07's head tracks the light;
- the tone swells when 07 faces it, which works like tuning a radio;
- 07 makes a small reach-up gesture as its antenna lamp stutters the reply.

**A sender (C7):**
- after the first charge, **one character decodes** (`CALL · A?? ??`);
- two more decode at the answer;
- the rest decode one more each time 07 docks, until `ARK IV`.

A letter means someone is sending.

**The call's states are the only goal display on 07's face (H2):**

| State | When | What the player sees and hears |
|---|---|---|
| **Broken** | on reserve | the tone breaks up; the antenna lamp stutters |
| **Locked** | after the first charge | 07 keys the pattern back; the decoded letters appear |
| **Clear** | at night | the waveform is steady, with no static |
| **Answered** | at R8 | `ACK` from relay.1 |

**Fallback.** "The broken radio" (C8 alternative) is kept only for one case: by minute 5, playtesters can't say
something is calling 07. The limit chain (§3) works the same under either hook.

## 3. Canonical beat table
**The rules:**
- Each beat shows **one** guidance element.
- Each beat's "why" is said once, by the world.
- 07 grows on its body by 30 minutes (C7 risk 5).
- Guidance steps down: chip → glint → world → nothing (Portal's last test, player-guidance A4).
- Times are for a first-time player.

| # (old) | Time | Beat | Gate · key · lure | One guidance element | The world's "why" (once) | Verb | 07 growth | Principles |
|---|---|---|---|---|---|---|---|---|
| **R1** (B0) | 0–2 | **Wake.** POST one shot; the lens opens on dark Earth and the point of light; the call; 07's head tracks; the antenna stutters; 07 reaches up. | lure: the call | the sound and 07's head | the stuttered reply: 07 wants to answer and can't | look | — | P1, P2, C5.2 |
| **R2** (B1–B2) | 2–6 | **Drive to the lander** on reserve: top speed capped, lamps dim, the call breaking up. 07's own old tracks lie on the way (an optional curiosity dwell gives a databank entry). | gate: reserve | the lander's warm amber lamp, in the first forward view | slow and dim; the only warm light out here | drive | — | P3, H1, C7.2 |
| **R3** (B3–B6) | 6–10 | **The pad.** Scan 2 broken-pad fragments; the wireframe completes. Open `CHARGER KIT · ILC-R`, build, dock. **The diamond ring** as the cells fill. The reserve lifts, the call locks, and one character decodes. **The range ring** shows faint for 3 s. **First charge by minute 6–8** (ruling 2). | key: the pad; lure: the ring's edge | first-time prompt chips, one at a time (Scan → Open → Build) | the battery glyph flickers low; a broken charger beside a crate labelled for one | scan, open, build, dock | **the antenna extends one segment** | P3, P6, P7, E1, C2 |
| **R4** (B7–B8) | 10–20 | **The lamp dies.** The lander's lamp and the cable glow to the pad go dark together. The fallen array glints in the new sunlight. Scan 3; the cutter sputters (`CUTTER · NO IGNITER`). Kenji's `CUTTER SPARES` rack stands beside it: fit the igniter where you stand. Cut 2 panels free (yields shown); build solar. `PowerOn`, and the music returns. | gate: power source; key: solar | the glint | the visible cable goes dark: "my charger died with the lander" | scan, fit, cut, build | **the igniter, a visible part on the cutter** | M, P3, P6, C7.3 |
| **R5** (new) | 20–30 | **First free choice.** Two small sites in view from the pad, either order: (a) **the 1971 seismic station**, whose dead amber lamp sits on Lumen's junction box: a curiosity scan with an ILC note; (b) **the habitat ruin**, whose power module's status lamp blinks, with the airlock's `SUIT 01-04 · OFFLINE` beside it. | lures: 2 near | none (world only) | — (curiosity) | scan (dwell) | — | P12, C1 miss 3, C3.1 |
| **R6** (B9–B10) | 30–40 | **Dusk.** It begins when 07 next docks after visiting either site, so it follows progress, not a clock (opening-v2 §9). The charge slows on 07's face; the module lamp blinks. Scan 2. **Tow the module home with the winch** (ruling 4). Build the bank on it. `NightReady`: the first nightfall, and the lights stay on. | gate: night; key: the bank | the blinking module lamp | the pad charges slower as the light goes | **tow** (new) | — | M, C7.3, P8 |
| **R7** (B10–B11) | 40–50 | **The failed reply.** At night, with the call clear, 07 answers: an F# chirp, `TX · NO RANGE`, the antenna lamp drops, the head lowers. The tower ruin stands against the stars. Scan 3, cut, **raise the mast** segment by segment. `SignalWest`. | gate: voice; key: the mast | none: the skill test, no chips | 07 tried to speak and failed | **raise** (new) | — | P3, A4, C7.3 |
| **R8** (B12) | 50–55 | **The answer.** The mast's foot lamp pulses, and 07's face shows `TX · READY`. Hold to transmit. The network pulse runs out to the west rim; **relay.1 blinks three short, one long back**; `ACK · RELAY.1 · ROUTE · PEAK DISH · OFFLINE`. Two more characters decode, and the pulse glints on the Peak's dark dish. | payoff | the CRT line | the call is answered by the crew's network, and the dish is the way home | transmit | — | P7, P8, P10 |
| **R9** (B13) | 55–60 | **Release, staggered.** First the full **range ring**, shown once: relay.1 inside, the Peak just outside. Then the view from the rise: relay.1, the Peak's dish, and Kestrel-3's white-and-teal wreck. Then, at the next dock, **the socket rack** at the pad (ruling 5) shows its first outlines: the SSR-3 headlamp and the cell pack (§5). Wear starts from here (ruling 2). | gates shown; 3 lures; goal board | the range ring, once | the ring shows what 07 can and can't reach | look, choose | — | HO, P11, P12, C2, C4.2 |
| **R10** | 60–75 | **Free hour, part 1** (§5): relay.1's repair, or the Kestrel-3 hunt. | §5 | world only | §5 | §5 | §5 | C2, C3.3 |
| **R11** | 75–90 | **Free hour, part 2** (§5): relay.2 and elder 06: the headlamp and the cell pack. The session-end dock. | §5 | 07's face | §5 | §5 | **the headlamp** | C2, C6 |

- **Milestones** (Core `OpeningMilestoneReached`): `FirstCharge` (R3), `PowerOn` (R4), `NightReady` (R6) and
  `SignalWest` (R7) stand. Add `CallAnswered` (R8).
- **Held unlocks** release after `SignalWest` (maker-understanding §1 rule 6).

## 4. Items (first 60 minutes)
**Rules:**
- **Three materials:** Metal, Wiring and Optics, plus **the igniter**. That is four item types in all (E2: 3–5). No
  fourth material is needed.
- **Every source is fixed** and looks like what it gives (E4, E5).
- **Every pickup names its purpose:** `Metal 4 · for the charging pad` (E3).
- **Every item is spent within its link** (E6).
- **Costs show on the build ghost** (E7).
- **The start hold is empty** (ruling 3). The first Tab opens on the databank's seeded cards.
- **Cut from the opening:** every small crate, every shoot-a-crate stack, and every pickup outside its link.
- **Supply runs at about 1.3× the need.**

| Build | Cost | Fixed source |
|---|---|---|
| Charging pad | Metal 4 · Wiring 2 | the `CHARGER KIT · ILC-R` crate in the lander's open bay (a flat-pack) |
| — | igniter | Kenji's `CUTTER SPARES` rack by the fallen array, found the moment the cutter fails |
| Solar array | Metal 3 · Wiring 2 · Optics 1 | 2 panels cut from the fallen array (Metal 3, Optics 1); its junction box (Wiring 2) |
| Battery bank | **the towed module** · Wiring 3 | the habitat's power module, towed home as the bank's frame (it stands in for v2's Metal 3); its labelled cable-reel crate (Wiring 3) |
| Antenna mast | Metal 4 · Wiring 3 · Optics 2 | the tower ruin: mast sections, feed cable, dish panels |

## 5. The free hour (R10–R11) and the session end
**The gates (C2):**
- **Range** is the main gate. The ring shows energy out and back, and it counts lit relays as charge points.
- **Darkness** is the second gate: Kestrel-3's interior, and a shadowed crater floor where 07's lamps visibly fall
  short.

**R10, the player's choice:**
- **relay.1 (inside the ring).** Its cabinet holds the labelled spare (`RELAY SPARE · FEED UNIT`). Fitting it relights
  the relay, and its pad charges 07 there (lit relays charge, as in GDD v3). That puts a charge point on the map, so
  **the ring grows**, and the network map on 07's face reveals relay.2 with `SSR-3 · UNIT 06 · HOLD RELAY.2 LINK ·
  CELL 0%`.
- **Kestrel-3, the first two-site fragment hunt** (C3.3):
  - **The limit:** its solar boom still makes power, but its contact is a Tycho hex plate. 07 parks on it and reads
    `CONTACT · HEX · NO ADAPTER`.
  - **The fragments:** 2 in the wreck and 1 on the boom piece at the debris field (`-40, 90`).
  - **The key:** the **Hex Charge Adapter**, a socket kit (story v4 §7 Q7: Tycho's hex dock teaches it). Kestrel-3
    becomes a forward charge point toward the Peak.
  - **The darkness gate:** its interior is dark, and the best find, **Kestrel-3's data buffer**, waits there until 07
    has the headlamp.

**R11, relay.2 and elder 06:**
- Scanning 06 reaches Kessler–Arai understanding level 1 together with the trickle post scan at the wake (07-identity
  B2 readout). That unlocks the **SSR-3 headlamp**, adapted from 06's own lamp (maker-understanding §2).
- 06's battery bay holds the **cell pack** fragments (blueprint-fit).
- Both fit at the pad's socket rack, and their outlines fill as they are fitted. **The headlamp** opens the darkness
  gate. **The cell pack, or the Kestrel-3 adapter,** brings the Peak inside the ring. That gives two routes to the
  same gate, so the player chooses (C2 rule 4).

**Session end** (P13, C6):
- 07 docks;
- one more character of the call decodes;
- the bank's level lamp shows its charge;
- 07's face shows a gentle appointment for the next night: `METEORS · NEXT NIGHT` (an awe hook, never a fail
  timer).

The genre doc's `EARTHRISE` example can't happen here: from a fixed site on the Moon, Earth never rises.

## 6. 0.6's end beat (ruling 6)
1. The route reaches the Peak.
2. 07 wakes the Great Dish. It slews toward Earth and **jams at 14%** (story v4).
3. 07 sends **one fragment of the survey**: Kestrel-3's buffer.
4. The far carrier **stops sweeping and holds on Lumen**: `CARRIER · LOCKED`. Someone heard.
5. The Peak's view opens the 0.7 regions.

The game's ending is untouched: freeing the dish, the full survey, and the Arks' light.

## 7. Checklist (player-guidance B6), R1–R9
| Beat | Yes · No · n/a | Each "no" or condition |
|---|---|---|
| R1 | 14 · 2 · 4 | **No:** it changes nothing on 07 or the base. By design: it's the hook (P10). |
| R2 | 15 · 1 · 4 | **No:** nothing changes on 07 (travel). Watch the reserve feel. |
| R3 | 20 · 0 · 0 | — (the antenna segment closes v1's gap) |
| R4 | 19 · 1 · 0 | **No:** two ideas (fit, then cut). Accepted: the fit is 2 s; split it if playtests stall. |
| R5 | 15 · 0 · 5 | **n/a:** need-before-tool, and the economy questions. Purpose is curiosity. |
| R6 | 19 · 0 · 0 (+1 cond) | **Cond.:** the tow must pass the feel gate; otherwise carry the core (ruling 4). |
| R7 | 20 · 0 · 0 | — |
| R8 | 17 · 0 · 3 | — |
| R9 | 18 · 0 · 2 | v1's "four things at once" is fixed by staggering them. |

## 8. Changes to earlier decisions
- **Opening-v2 §10:**
  - the start hold is empty (was Metal 2, Wiring 1);
  - the igniter moves from the lander crate to the fallen array;
  - the bank's Metal 3 becomes the towed module;
  - all small sealed crates are cut.
- **Opening-v2's objective lines go.** `OpeningFlow` keeps its steps, but a line shows only as a 120-s stall
  reminder: once, never repeated (H5).
- **Reconciled with the genre doc's C8:**
  - **The headlamp** stays the canon SSR-3 headlamp (Kessler–Arai, from 06). An adapted blueprint can never also be a
    fragment blueprint (maker-understanding rule 2). Kestrel-3's two-site hunt teaches the Hex Charge Adapter instead.
  - **R5's first site** is the seismic station, because no elder rests near the pad (06 is at relay.2).
  - **The session-end appointment:** `EARTHRISE` → `METEORS · NEXT NIGHT`.
- **The tow (ruling 4): tow.**
  - **The evidence:**
    - 07's rig has `WinchSocket`, `WinchSpool`, `TowHook` and `WinchOrigin` (`RoverModelNodes`);
    - Rover still plays tether strain on `TetherAttached` and `TetherReleased` (`RoverBodyLanguage`);
    - `IRoverBattery` already prices towing.
  - **The catch:** the cut `ITowable` and `SalvageDrag` (0dd4ab31) drew a **beam**, which the owner rejected (07 v2:
    no eye beams). They come back as a **physical cable** from `WinchOrigin`.
  - **The fallback:** carry the module's core in the hold, if the tow fails the feel gate.
