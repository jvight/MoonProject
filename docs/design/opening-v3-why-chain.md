# Opening v3, R8–R11: the "why" chain

> **The owner** (playtest 2c1ea201d): "Why, after repairing the antenna, do we know where 06 is broken? And why go
> find it? The game is extremely hard to understand."
>
> **Author:** the game design session, 2026-10-25.
>
> **Builds on:**
> - `opening-v3-purpose.md` R8–R11;
> - `base-home-and-crater-exits.md` §4 (the canyon's dark slot, a headlamp gate);
> - `07-identity.md` (the Ark IV carrier reached relay.2, and **06 relayed it into the lander's bus: that woke 07**);
> - `writing-style-ingame.md`.

## 0. Player experience
The rule is **feel the need, see the solution, then go**: a limit felt, a hint seen, the means earned, a change felt.
Today R8–R11 skips the first two steps. The player is *told* where to go and never *feels* why. The fix makes each
step **a broken thing the player can see on 07's CRT map or in the world**, with one short line at most.

## 1. Audit as a first-time player (build 2c1ea201d)
| Step | What the player knows | Why would they go? | What tells them | Verdict |
|---|---|---|---|---|
| R8: transmit, `ACK · RELAY.1 · ROUTE · PEAK DISH · OFFLINE` | "Something answered." | — | an all-caps code line | the ACK is a puzzle: what's a route, what's the dish to me? |
| → go to relay.1 | "relay.1 answered" (from the line) | **nothing felt.** It answered, so it seems to work. Why fix it? | the next line only | **BROKEN:** the goal has no need |
| R10: relay.1 repaired (spare fitted, pad charges) | "a charger out west" | — | — | a reward without a reason |
| → go to relay.2 and scan 06 | the line names 06 and relay.2 | **none.** The player has never seen 06 or relay.2, and **can't know 06 is "slumped"** | the line only (it reads as the game knowing things 07 can't) | **BROKEN:** it tells a fact the world never showed |
| R11: scan 06, cut its crate → the headlamp | "a headlamp blueprint" | **none.** No dark place has been felt | the unlock toast | **BROKEN:** a tool before its need |
| → dock, assemble at the rack | the line | to wear the reward | the line | fine mechanically, but it rewards a goal nobody had |

## 2. The redesigned chain: every "why" shown in the world
The spine is **the call's route**. 07 answered, but the answer only gets out if the relays carry it. The player
**sees the route on 07's CRT** as a chain of three lamps: `07 ─ RELAY 1 ─ RELAY 2 ─ PEAK DISH`. That is the same
map idea as the ring and the call display, so there is nothing new to learn.

**R8, the answer:**
- The CRT draws the route once: 07 ● (lit), **Relay 1 ◐ (lit, but flickering)**, Relay 2 ○ (dark), Peak Dish ○
  (dark).
- Relay 1's real lamp on the west rim blinks the pattern back, as built.
- **The limit is felt:** about 10 s later Relay 1's lamp sags and stutters, and **the call on 07's face breaks up
  again**. Its CRT dot goes dim.
- The line: `Relay 1 is fading. Go fix it`.

**R10, Relay 1:**
- **The hint:** its open cabinet with a labelled spare (as built). Fitting the spare relights it, and its pad charges
  07 (as built).
- **The change:** Relay 1's dot goes solid, the call is steady again, and the route **extends one hop**.

**R10 → R11, why relay.2 and why 06** (the owner's question):
- With Relay 1 holding, 07 now hears **Relay 2's last status ping**, carried in over Relay 1. The CRT shows Relay 2
  in the east at the canyon mouth, with a stored packet: `RELAY 2 · KEEPER 06 · CELL 0% · LAST: RELAYED CARRIER → LUMEN`.
- The player learns, with no story text, that **the unit keeping Relay 2 spent its last charge passing on the very
  signal that woke 07**, and that Relay 2 is the dead link between 07 and the dish.
- Relay 2's mast is visible from the base (east, 278 m) as a dark silhouette. Once the ping arrives, its foot lamp
  gives one weak pulse every few seconds: the lure.
- The line: `Relay 2 went quiet. Find 06 at the canyon mouth`.

**R11, at relay.2:**
- **06 is there**, slumped by its hut and still facing the mast.
- **The new limit, felt:** relay.2's power line runs **into the canyon's dark slot** (permanent rim shadow, toward
  the Canyon Supply Lander that fed it). 07 follows the cable into the slot, and its stock lamps fall short. The CRT
  shows `CAN'T SEE AHEAD`, and 07 backs out on its own. (This is `base-home-and-crater-exits.md` §4's slot, moved to
  this exact moment.)
- **The solution, seen:** 06's big boxy sealed-beam headlamp, a long-throw lamp built for this job, catches 07's light
  and glints.
- Scan 06 → the Kessler–Arai understanding → **the SSR-3 headlamp blueprint**. Cut 06's field crate for the parts
  (as built).
- The line: none (the need was just felt). The rack line comes after the parts are taken.

**R11 → R12:** assemble the headlamp at the rack (as built). **Then the payoff the player has been waiting for:**
the headlamp beam reaches into the slot. That is 0.6's first step (the Canyon Supply Lander's bay, the Field Charge
Pylon) and Relay 2's cut line.

**What changed, in one line each:**
- Relay 1 **visibly fades**, so there is a reason to fix it.
- Relay 1 fixed **carries Relay 2's ping**, so 07 learns about 06 the way a radio would.
- **The dark slot is felt before the headlamp is earned.**
- 06's lamp is the visible answer.

## 3. Text budget (the `writing-style-ingame.md` rules)
| Moment | Line (en / vi) |
|---|---|
| R8 + 10 s | `Relay 1 is fading. Go fix it` / `Trạm tiếp sóng 1 đang yếu dần. Tới sửa nó` |
| R10 done | `Relay 2 went quiet. Find 06 at the canyon mouth` / `Trạm 2 im lặng. Tìm 06 ở cửa hẻm` |
| The slot | CRT only: `CAN'T SEE AHEAD` / `KHÔNG THẤY ĐƯỜNG` |
| The relay.2 ping (CRT, capitals) | `RELAY 2 · KEEPER 06 · CELL 0% · LAST: RELAYED CARRIER → LUMEN` / `TRẠM 2 · XE 06 GIỮ · PIN 0% · CUỐI: CHUYỂN TÍN HIỆU → LUMEN` |

There are no other lines. The ACK line stays, shortened per the style guide.

## 4. Tasks per domain
| Domain | Task | Size |
|---|---|---|
| **Gameplay** | A route model: 07 → Relay 1 → Relay 2 → Peak Dish, each link off / weak / on. Relay 1 goes weak 10 s after `CallAnswered` and the call display breaks up while it is weak. Relay 1 relit sets it on and delivers relay.2's ping. The next lines are retimed to these states. The slot's `CAN'T SEE AHEAD` trigger, with 07 backing out. | M |
| **UI** | The CRT route strip (4 dots and their states, drawn on 07's face beside the call; one guidance element); the ping card in capitals | S–M |
| **World** | Relay 1's lamp sag and stutter state; relay.2's foot-lamp weak pulse after the ping; relay.2's cable laid into the canyon's dark slot; the slot in permanent rim shadow (base-home §4) | M |
| **Rover** | 07 reverses out of the slot when it can't see (a small, calm back-off); 06's headlamp glints when 07's light hits it | S |
| **Audio** | The call breaking up while Relay 1 is weak (reuse the broken carrier); a soft ping tone for relay.2's packet | S |
| **Story** | Final wording of the ping line, en and vi | S |
| **Tools/bot** | Update the naive-gate declarations for R8 → R12 (new words: fading, fix, Relay 2, 06, canyon) | S |

## 5. Playtest signal
Asked at R10 "why are you going to relay.2?", a first-time player answers **"the signal goes through it, and the
robot there is dead"**, without having read a story card. At the slot, they say **"I need a better light"** before
the headlamp unlocks.
