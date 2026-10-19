# 07: identity sheet

> **Status: DECIDED** (owner's picks, 2026-10-18; see §5). Author: the story & flow session. The Vietnamese
> edition is `07-identity-vi.md`.
>
> The owner asked: "We need to be much clearer about what kind of robot 07 is, why humans built it, what its job is,
> and what makes it gradually show feelings."
>
> Bound by:
> - `docs/design/story-v4.md` (The Departure, 1988, CONTINUE, elders 01–06 at rest);
> - `docs/design/opening-v2.md` (the story told through the environment, one guidance element);
> - `docs/design/scale.md`;
> - VISION rulings 11–17, especially 13 (scale), 14 (no hands) and 17 (wear);
> - `docs/design/text/crew-logs.md` (07 in the third person only);
> - vacuum logic.
>
> **Rules:** people treat 07 as a tool. Its character shows only through behaviour, never through text that says it
> feels. It is an old, melancholic machine, never WALL-E. `[PROPOSAL]` marks what is still open: the mass and the
> exact text lines in §3–4.

## 1. Identity sheet
| Question | Answer |
|---|---|
| **Designation and model** | **ILC Station Service Rover, Model SSR-5**, serial **3-0417**. Built in **1983** by **Kessler–Arai Field Systems**, a field-robotics firm contracted by the International Lunar Commission (ILC) for the Departure's lunar build-out. "07" is not the factory's name. It is **Lumen's asset number**: Kenji rebuilt a spare survey chassis as the SSR-5 and it was job 7 on his board that week (`log.kenji_1`, v4). |
| **Why Lumen needed it** | Lumen was the Departure's waystation, quarry and kitchen, with four people for a whole station. Outdoor work in vacuum (inspecting, salvaging, assembling and maintaining pads, arrays, masts and cables) went to service rovers. **The SSR-5 is the modular model:** it takes kit in sockets and carries an assembly program library, so it can build as well as fix. |
| **Its verbs, each a job tool** | **Scan** (the lens, Ø 0.245 m): a laser-line profiler and camera that records a part's geometry for the shop's plotter to fabricate a replacement. That is why scanning teaches blueprints, and why a known part yields only scrap metal. **Cut** (the front cutter turret, 0.73 m): the salvage and maintenance torch. **Build and weld:** it assembles flat-packed station modules on site from its program library (BUILD-CORE). **Dig** (the front scoop, 0.12–0.35 m): regolith clearing and sampling. **Tow** (the rear winch and hook, 0.38 m): recovering stuck units and hauling. **The hold and rear hatch** (0.48–0.80 m): spares out to a job, salvage back. **Charging:** induction pads, plus a trickle from the solar wing on long shifts. **The antenna:** telemetry, and a status light that keys the station call sign (three short, one long) when its receiver locks (`log.kenji_1`). |
| **Why it has a CRT "face"** | It is a **crew-facing diagnostics display**: a 0.58 × 0.39 m amber CRT at 1.26 m, the height a suited technician kneels to. It was big and amber so a crew member could read it through a gold visor in full sun: task queue, hold manifest, fault codes, and the line drawing of a scanned part to check before fabrication. It shows text and drawings, never a cartoon face. Players may read it as a face; the design never says so. |
| **Why it chirps** | It has a five-tone **status annunciator** keyed to the crew's suit-radio channel, so suited crew could know its state without looking (VISION's D major pentatonic: D acknowledge · E working · F# query/attention · A task done · low B fault). The player hears it through 07's own chassis. The tones are codes, not words. **Adopted:** 07's mechanical voice gestures in the build carry these five meanings. |
| **The day of the storm** (28 Oct 1988) | 07 had unloaded the last cargo from the cargo lander (landed in the 1987 drop) and was parked on its trickle post in the lander's lee, which shielded it from the radiation. Kenji set every rover to **CONTINUE** and wired 07's post to the deep-charge bus with a **wake-on-carrier** trigger. Ines queued the dish alignment and the Meridian uplink on 07's recovery stack (`log.ines_3`, `log.kenji_3`, v4). |
| **How it survived 36 years** | **Standby, by design.** CONTINUE kept its queue. Its queue's top task waited for a carrier: there was no point sending the survey into silence. Standby draw is tiny. The solar wing trickled through each lunar day. The trickle post's cable failed years ago, leaving the cells at 50% (opening B1). In 2024 the Ark IV sweep reached relay.2, unit 06 relayed it into the lander's bus, and the carrier woke 07 (v4). Around 13 150 days asleep. |
| **The elders 01–06** | **SSR-3 units**: older, smaller, single-purpose (relay keeper, survey cart, lift cart…), with no kit sockets. They share one SSR-3 chassis and each carries a topper for its one job (models 02–06 exist). They were Lumen's units 01–06, numbered in the order Kenji brought them online before 07. The station protocol dispatched them one at a time over the years, and each stopped where its task met something it couldn't overcome. **All six are at rest** where they stopped (v4). 07 is the only one built to grow, which is why it gets further. |
| **Physical facts** (`scale.md` §2.2) | **Height:** 1.525 m to the top of the head, 1.59 m to the antenna tip. **Length:** 2.01 m with the scoop, 1.76 m wheel to wheel. **Width:** 1.47 m. **Track:** 1.22 m. **Wheelbase:** 1.06 m. **Wheels:** four, Ø 0.70 m, one mismatched (`log.kenji_2`). **Ground clearance:** 0.335 m. **Solar wing:** at 0.948 m. **Mass:** about 420 kg `[PROPOSAL]`. Next to a person (1.75 m), it comes to about the shoulder. |

## 2. Why it seems to have feelings: Option A, decided
The owner picked Option A. Options B and C stay below as the rejected alternatives, so nobody reopens them by
accident. The player is never told 07 feels anything; the player infers it.

### Option A: an adaptive scheduler whose weights drifted *(decided)*
- **Mechanism.** Field rovers that run for months unattended use an **adaptive task scheduler**. It learns priority
  weights from outcomes: energy, wear, task success, and how much a scan teaches. Crews used to reset the weights
  after each rotation. For 36 years nobody did.
  - The weights drifted toward what kept the unit alive and useful: sunlit ground, its charger, known structures,
    new information.
  - A **fleet-handshake** routine still pings any Lumen transponder in range: the elders, the dead masts.
- **How it shows** (behaviour, animation, chirps; every one is a weight acting):

  | Behaviour | Which weight |
  |---|---|
  | It hesitates at unknown ground, then commits | risk weight |
  | Its head drifts to new things | information weight |
  | It lingers by its base at dusk | charge and safety weight |
  | At an elder, a **query tone (F#) repeats twice** with no answer, then 07 moves on | the handshake |
  | It turns toward a lit mast | network weight |

- **The gradual arc.** Early 07 is cautious: low cells, unknown terrain, failed handshakes. As the player restores
  power, the network and kit, the weights re-adapt: firmer driving, quicker commits, and new responders answering its
  handshake (masts, Meridian machines, friends). **Its "personality" grows with the player's progress**, which fits
  progression first (ruling 11).
- **Never:**
  - refuse or override the player's input;
  - display emotion words or emoji on the CRT;
  - mourn, cry or sulk;
  - show any behaviour that can't be traced to a weight.
- **Why it won:** it is the most logical, it gives a real gradual arc, it is built from systems we already have
  (stillness, battery, scanning, the network), and it stays original (an old scheduler, not a cute robot).

### Option B (rejected): a behaviour model learned from the crew's teleoperation
- **Mechanism.** For four years the crew drove 07 remotely for delicate jobs. Its motion planner learned **operator
  style** from those logs, a standard 1980s "teach and repeat" method.
- **How it shows.** It reproduces the crew's working habits:
  - a horizon-calibration sweep at dawn (Ines's survey routine);
  - a slow approach to cables (Kenji's);
  - parking facing the dish at night (Ro's broadcast checks).

  The player recognises them later in logs and the databank.
- **The arc:** more habits surface as 07 reaches the places where the crew worked.
- **Never:**
  - imply the crew "lives on" in 07, or that 07 misses them;
  - copy anything that isn't a work habit.
- **Risk:** it can read as eerie or sentimental (a ghost of the crew). It's harder to keep within the owner's rule.

### Option C (rejected): a radiation-damaged annunciator and motion governor
- **Mechanism.** The storm flipped bits in the tone generator and the motion governor. Tones come out with uneven
  pitch and timing, and damping is uneven, so it hesitates and its head returns slowly.
- **How it shows:** wavering chirps, small hesitations. Pure fault, which the player reads as mood.
- **The arc:** none of its own. Repairs would *remove* the character.
- **Never:** random comedy glitches.
- **Risk:** no growth, and fixing 07 erases what players love about it.

**Decided: Option A.** Option B's dawn horizon sweep may stay as one work routine inside A's scheduler (a
calibration task), without the crew-imprint framing.

## 3. How the player learns it, diegetically (one line each)
Each lives on a surface the player scans or a readout that already exists, so it never adds a guidance element.

| Where | What it says | Beat |
|---|---|---|
| **The chassis plate** (rear flank stencil) | "ILC · SSR-5 STATION SERVICE ROVER · S/N 3-0417 · KESSLER–ARAI 1983", and Kenji's hand-painted "07" above it | seen in the chase frame from B1; readable on the first close look |
| **The CRT face boot** (New Game's boot face POST, `cutscenes.md` §1.8) | "UNIT 07 · CARRIER DETECTED", then "CONTINUE · 13 148 DAYS · RESTORING TASK QUEUE" (`post.carrier`, `post.resume`) | the wake, before control |
| **The dead trickle post** (B2, scan) | "SSR-5 TRICKLE POST · WAKE ON CARRIER · LINE FAULT" | the wake point, before the first drive |
| **The airlock panel** (B7, beneath the existing lines) | "ROVERS · CONTINUE · SET 28 OCT 1988 · K. MORROW" | the Lumen ruins |
| **Kenji's job board** (the workshop ruin, scan) | "JOB 7 · SPARE SURVEY CHASSIS → SSR-5 REBUILD · DONE · K.M." | B7, optional |
| **Elder scan pages** (databank, Machines) | e.g. "SSR-3 · UNIT 06 · RELAY KEEPER · TASK: HOLD RELAY.2 LINK · STOPPED: CELL 0%" | where each elder rests (06 in the basin; 01–05 in 0.6+) |
| **07's own databank page** (Kenji's voice, third person) | "SSR-5 service rover. Scans parts for the shop, cuts what's broken, builds what's new. Rebuilt this one from a spare chassis. — K." | the first time Notes opens |

## 4. Text keys this adds (placeholders for review; en / vi)
| Key | en | vi |
|---|---|---|
| `rover.plate` | ILC · SSR-5 STATION SERVICE ROVER · S/N 3-0417 · KESSLER–ARAI 1983 | ILC · XE DỊCH VỤ TRẠM SSR-5 · S/N 3-0417 · KESSLER–ARAI 1983 |
| `ui.hold.unit` (not built: the boot face's `post.carrier` and `post.resume` carry it) | UNIT 07 · SSR-5 · CONTINUE · {0} DAYS | ĐƠN VỊ 07 · SSR-5 · TIẾP TỤC · {0} NGÀY |
| `post.trickle.readout` | SSR-5 TRICKLE POST · WAKE ON CARRIER · LINE FAULT | TRỤ SẠC NHỎ GIỌT SSR-5 · THỨC KHI CÓ SÓNG MANG · HỎNG ĐƯỜNG DÂY |
| `airlock.rovers` | ROVERS · CONTINUE · SET 28 OCT 1988 · K. MORROW | XE TỰ HÀNH · TIẾP TỤC · ĐẶT 28/10/1988 · K. MORROW |
| `board.job_7` | JOB 7 · SPARE SURVEY CHASSIS → SSR-5 REBUILD · DONE · K.M. | VIỆC 7 · KHUNG XE KHẢO SÁT DỰ PHÒNG → LẮP LẠI SSR-5 · XONG · K.M. |
| `elder.06.manifest` | SSR-3 · UNIT 06 · RELAY KEEPER · TASK: HOLD RELAY.2 LINK · STOPPED: CELL 0% | SSR-3 · ĐƠN VỊ 06 · GIỮ TRẠM TIẾP SÓNG · NHIỆM VỤ: GIỮ LIÊN LẠC RELAY.2 · DỪNG: PIN 0% |
| `notes.unit07` | SSR-5 service rover. Scans parts for the shop, cuts what's broken, builds what's new. Rebuilt this one from a spare chassis. — K. | Xe dịch vụ SSR-5. Quét linh kiện cho xưởng, cắt chỗ hỏng, dựng đồ mới. Chiếc này lắp lại từ một khung xe dự phòng. — K. |

## 5. Owner decisions (2026-10-18)
| Question | Decision |
|---|---|
| Name, maker, serial | **Keep:** SSR-5 Station Service Rover, Kessler–Arai Field Systems, S/N 3-0417. |
| Why 07 seems to feel | **Option A:** the adaptive scheduler whose weights drifted over 36 years. |
| The five-tone annunciator | **Adopted:** already in the game as 07's mechanical voice gestures. |
| The elders 01–06 | **SSR-3, yes:** single-purpose, no sockets; one shared chassis with a per-job topper. |
