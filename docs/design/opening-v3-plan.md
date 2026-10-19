# Opening v3: implementation plan

> **For the Director, to assign.** Design: `opening-v3-purpose.md` (final). Author: the game design session,
> 2026-10-20.
>
> **Code facts this plan builds on** (main ddd1a039):
> - `Gameplay/Progression/OpeningFlow` drives the objective lines through `StepFlow` and publishes the milestones.
> - `UI/Hud/ObjectiveLine`, `ObjectiveView`, `ObjectiveChip` and `UI/Prompts/GuidanceSlot` keep one element on screen.
> - `Gameplay/Opening/SupplyCaches` and `Gameplay/Containers/SealedContainers` run the crates, including the cut-open
>   small ones.
> - `Gameplay/Opening/RoverStartKit` holds the faults (cutter, wing, antenna) and the start hold.
> - `LanderPort` holds the one-charge reserve.
> - `IRoverBattery.IsLowPower` and `DriveCap01` already cap speed on the reserve cell.
> - `RoverBodyLanguage` already wakes and daydreams toward Earth, and plays tether strain on `TetherAttached` and
>   `TetherReleased`.
> - The rig has the winch nodes.
> - The bot: `Autoplay/OpeningScenario` (B0–B11) waits on objective lines through `OpeningBase.ExpectLine`.

## 0. Rules for every slice
- **Keep the opening playable.** After every land, a New Game plays R1 to the end of what exists.
- **The bot passes, or is updated in the same slice.** `tools/autoplay.py` covers B0 to the last built beat.
  - `box_check` and `compile_check` print `RESULT: PASS`.
- **One slice, one owner, one branch.** A Core contract (an event or an interface) lands first as its own tiny
  Director commit, so the domains can then work in parallel.
- **The Director plays each wave** as a first-time player:
  - real input (mouse and gamepad);
  - 16:9 and 21:9;
  - objective text never needed;
  - **stall metric:** more than 30 s at a loss is a fail.
- **Sizes:** S (≤ ½ day), M (1–2 days), L (3+ days, split before assigning).

## 1. Wave 1: remove the anti-patterns (first, and fast)
| # | Slice | Owner | Acceptance tests | Playtest signal | Depends on | Size | Parallel |
|---|---|---|---|---|---|---|---|
| 1.1 | **The bot follows the game, not lines.** Add `ExpectStep(index)`, reading `OpeningFlow.CurrentIndex` through `IOpeningProgress`, and `ExpectWorld(cue)`. Add a **guidance counter** that fails if two guidance elements are ever on screen at once, and a "no fallback fired" assertion. | Tools/bot | the bot's B0–B11 pass on both APIs; the counter fails a seeded two-element frame | — | — | S | yes |
| 1.2 | **World cues.** The lander's amber lamp in the first forward view after the hand-over. `LanderPort` reserve spent → the lamp and the lander-to-pad cable glow go dark together. The fallen array's glint once the eclipse ends. The power module's status lamp blinks from dusk. The tower ruin reads against the stars. | World + Art (Argon) | EditMode: lamp and cable state follow `LanderPort`; captures at 4 fixed points (look report) | in R2–R7, a new player heads for the cue within 30 s | — | M | yes |
| 1.3 | **Item economy v3.** Empty start hold (`RoverStartKit`). The lander's open crate becomes `CHARGER KIT · ILC-R` (Metal 4, Wiring 2). Fixed yields (array, junction box, cable reel, tower). **Every small sealed crate is cut** (`SupplyCaches` data and World placements). Supply covers each link at 1.0–1.5×. | Gameplay + World + bot | EditMode supply table: each link's supply ≥ need and ≤ 1.5×; no source yields an item unused in its link; the hold is empty at New Game; the bot is updated | nobody asks "what is this for?" | — | M | yes |
| 1.4 | **Pickups name their purpose:** `Metal 4 · for the charging pad`; costs on the build ghost. | UI + Story (text, en/vi) | EditMode: each pickup notice resolves its purpose key; the ghost shows the cost rows | — | 1.3 (keys) | S | yes |
| 1.5 | **Objective lines → a stall reminder.** `OpeningFlow` keeps its steps, but publishes a line only after 120 s with no progress and 07 still, once per step and never repeated. Lines are reworded to point at the world ("The lander's pilot lamp is still lit."). | Gameplay + UI + Story + bot | EditMode: no line before 120 s; one after a stall; never again for that step; the bot passes with the fallback asserted unfired | a first-time player gets to the first charge with no line shown | 1.1, 1.2 | S | after 1.1 and 1.2 |
| 1.6 | **The igniter at the fallen array.** The `CUTTER SPARES` rack beside the array. The first cut attempt sputters: `CUTTER · NO IGNITER`, a sputter sound, a face beat. Fit where 07 stands (owner 10-19). The `OpeningFlow` step moves. | Gameplay + World + Rover (face) + Audio + bot | EditMode: the cutter is FAULT until fitted; the first cut attempt raises the sputter event once; the bot updated | the player fits the igniter without a line | 1.3 | S–M | after 1.3 |

**Wave 1 lands as an opening that plays B0–B11 with no objective lines and no small crates.** It is shippable to
the owner as a check on the anti-pattern fixes before the call exists.

## 2. Wave 2: the call and early growth
| # | Slice | Owner | Acceptance tests | Playtest signal | Depends on | Size | Parallel |
|---|---|---|---|---|---|---|---|
| 2.0 | **Core contract (Director):** `CallState` {Broken, Locked, Clear, Answered}, `CallStateChanged`, `CallDecoded(chars)`, and the milestone `CallAnswered` | Director | compiles; EditMode on the enum and event | — | — | S | first |
| 2.1 | **The call's state and the carrier.** State from milestones: reserve → Broken, `FirstCharge` → Locked, `NightReady` → Clear. The decode count follows the rules in purpose §2, and is saved. The carrier is three short, one long in D pentatonic, breaking up while Broken; **gain swells with 07's facing to the sky point**. | Gameplay + Audio | EditMode state machine (including a loaded game); audio gain against bearing is monotonic, as a pure function | by minute 5, "it's trying to answer something" (**the hook gate**, ruling 1) | 2.0 | M | yes |
| 2.2 | **Body language.** The head tracks the sky point while idle and while charging (extend the existing wake and daydream toward Earth). The antenna lamp stutters; a reach-up gesture. At Locked, the lamp keys the pattern back. | Rover | PlayMode: the head's yaw converges on the point; the stutter plays only while Broken | players turn 07 toward the light unasked | 2.0 | M | yes |
| 2.3 | **The point of light** beside dark Earth: a calm 3S1L blink in the sky shader or a sprite, with no bloom spikes | Art (Argon) + World | capture at R1; the blink is in phase with the carrier event | — | 2.0 | S | yes |
| 2.4 | **The CRT call display.** Waveform plus decoded letters, shown when 07 has been still for 2 s, and for 3 s on a state change. It goes through `GuidanceSlot` as **the** element, never alongside a chip. | UI | EditMode: show and hide timing; the slot rule (the bot's counter from 1.1) | — | 2.0, 2.1 | M | yes |
| 2.5 | **The reserve feel.** The opening starts on reserve: `DriveCap01` caps **top speed only** (start at 0.7), with acceleration and turning untouched. Lamps dim. It lifts at `FirstCharge`. Charge has a floor until R9, so 07 cannot die (ruling 2). | Gameplay (tuning) + Rover (lamps) | EditMode: the cap applies only to top speed; the floor holds until `CallAnswered` + release; the bot's B0 → `FirstCharge` ≤ 8 min | first charge at 6–8 min for a first-time player; nobody calls the drive "slow" | — | S | yes (it can join wave 1) |
| 2.6 | **Growth on 07.** The antenna extends one segment at `FirstCharge`; the igniter is a visible part on the cutter turret once fitted. | Art (Argon) + Rover | rig node tests (new nodes named in `RoverModelNodes`); PlayMode: shown on the event | players notice the antenna by R4 | 1.6 | S | yes |

## 3. Wave 3: the middle (choice, tow, voice)
| # | Slice | Owner | Acceptance tests | Playtest signal | Depends on | Size | Parallel |
|---|---|---|---|---|---|---|---|
| 3.1 | **R5 and dusk by progress.** After `PowerOn` the sky holds in the afternoon. Dusk starts when 07 next docks after visiting a site (the seismic station or the habitat), or after scanning the module. The seismic station gets a curiosity scan and an ILC note. | Gameplay (sky, flow) + World + Story | EditMode on the dusk trigger (each path, a loaded game); the bot visits one site | players pick a site without a prompt | wave 1 | M | yes |
| 3.2a | **The tow core.** Restore `ITowable` and `SalvageDrag` from `0dd4ab31^` as a **physical cable** from `WinchOrigin` (no beam). Speed cap while towing, release, battery cost. | Gameplay | EditMode drag tests; PlayMode: a 100 m tow on basin slopes without snagging | — | wave 1 | M | yes |
| 3.2b | **Tow presentation:** the cable mesh, the spool spin, strain body language (existing), winch sounds | Rover + Audio | PlayMode: the cable attaches to `TowHook` and `WinchOrigin`; the strain rises with load | **the feel gate**: towing feels good, or switch to carry (3.2-alt) | 3.2a | M | yes |
| 3.2c | **The module as the bank's frame:** a towable module; bank cost = the towed module + Wiring 3; ghost rows | Gameplay + World + UI + bot | EditMode: the recipe accepts only the module within the apron; the bot tows and builds | the player tows home without a line | 3.2a | S–M | — |
| 3.2-alt | **Carry the core** (used only if 3.2b fails the gate): the module's core goes into the hold | Gameplay + bot | as 3.2c | — | — | S | — |
| 3.3 | **The failed reply and the raised mast.** At `NightReady` plus the clear call: the F# chirp, `TX · NO RANGE`, the head lowers (once). The mast builds in 3 raised segments with clunks and a settle. | Rover + UI + Gameplay + Art + Audio | EditMode: the trigger fires once (including a loaded game); PlayMode: 3 segment events | the player goes to the tower within 30 s at night | 2.1 | M | yes |

## 4. Wave 4: the answer and the release
| # | Slice | Owner | Acceptance tests | Playtest signal | Depends on | Size | Parallel |
|---|---|---|---|---|---|---|---|
| 4.1 | **R8, the answer.** `TX · READY`; hold to transmit at the mast; the network pulse to relay.1; relay.1 blinks the pattern back; `ACK …` on the CRT; 2 characters decode; a glint on the Peak dish; `CallAnswered`. | Gameplay + World + UI + Audio + Art + bot | EditMode: fires once, then saved; the bot extended to R8 | players hold transmit with no chip | 3.3 | M | — |
| 4.2 | **The range ring.** Pure out-and-back math over terrain cost and charge points (pads, lit relays, hex contacts). Faint 3 s at R3; full once at R9. | Gameplay (pure C#) + UI | EditMode on the ring math (flat, slope, extra charge points) | players read "Peak out of reach" without text | 2.0 | M | **start early** (pure logic) |
| 4.3 | **The socket rack at the pad** (ruling 5). Outlines of parts seen but not earned (SSR-3 headlamp, cell pack; the hex adapter once seen) fill as they are fitted. The held unlocks release after `SignalWest`. | Art (Argon) + Gameplay + UI | EditMode: outline state per part; PlayMode: shown at the dock | players point at an outline and know it's a goal | 4.1 | M | yes |
| 4.4 | **The release view.** The rise by the mast; relay.1, the Peak dish and Kestrel-3 readable from it; the staggered order (ring → view → rack at the next dock). Wear starts here. | World + Gameplay | captures from the rise; EditMode on the stagger order | players name 2+ places they want to go | 4.1, 4.2 | S–M | yes |

## 5. Wave 5: the free hour (R10–R11) and the session end
| # | Slice | Owner | Acceptance tests | Playtest signal | Depends on | Size | Parallel |
|---|---|---|---|---|---|---|---|
| 5.1 | **relay.1's repair:** its labelled spare, relit; its pad charges 07; the ring grows; relay.2 and 06's status appear on the CRT map | Gameplay + World | EditMode: charge points feed 4.2 | — | 4.2 | M | yes |
| 5.2 | **relay.2 and 06:** 06's scan → Kessler–Arai level 1 (with the trickle post) → the SSR-3 headlamp; the cell pack fragments; both fitted at the rack | Gameplay + Rover + Art | EditMode: understanding math; rig nodes for the headlamp and the pack | the player drives to 06 after seeing the rack | 4.3 | L (split by part) | yes |
| 5.3 | **The darkness gate:** the headlamp cone; dark interiors and the shadowed crater floor read as "can't see yet" | Rover + Art + World | captures with the lamp off and on | the player says "I need a light" | 5.2 | M | yes |
| 5.4 | **The Kestrel-3 hunt:** the hex plate limit (`CONTACT · HEX · NO ADAPTER`); 2 + 1 fragments over two sites; the Hex Charge Adapter; the dark interior and the buffer | World + Gameplay + Art + Story | EditMode: the fragment set across sites; the adapter enables hex charging | the player returns to the wreck once lit | 4.2, 5.3 | L (split) | yes |
| 5.5 | **The session end:** decode one character per dock until `ARK IV`; the bank's level lamp; `METEORS · NEXT NIGHT` | Gameplay + UI + Audio | EditMode: the decode count saves and stops at full | players dock "to see the next letter" | 2.1 | S | yes |

## 6. Two risks
**Risk 1: the call doesn't read as "calling me".** If players hear ambience instead of a call, the spine fails at
minute 1.
- Land wave 2 early. Run the **minute-5 gate** with 3 first-time players before wave 3 starts.
- The fallback, "the broken radio", changes only R1 and R3's presentation. Waves 1, 3 and 4 hold under either hook,
  so a switch costs about 2–3 slices, not a redesign.
- 2.1 and 2.2 carry most of the signal: sound plus the head turn. Tune them together in one feel session.

**Risk 2: the lines go before the world can lead, and the bot hides it.** Removing objective lines (1.5) before the
cues read (1.2) makes players stall. The bot, steering by coordinates, would still pass. That is the "bot misses
mouse" lesson again.
- 1.5 lands only after 1.1 and 1.2.
- The 120-s reminder stays as a net.
- The bot steers by **world cues** (`ExpectWorld`: it targets a cue only once that cue is lit and on screen), so a
  missing or dark cue fails the bot. It also asserts that no reminder fired.
- The bot still can't judge whether a human *reads* a cue. So the gate that counts is human: the Director plays every
  wave as a first-time player (real input, 16:9 and 21:9) against the 30-s stall metric.
