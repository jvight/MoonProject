# Blueprint fit: why everything 07 scans and builds fits 07

> Author: the game design session, for the Director and the owner. Design only, nothing implemented.
>
> The owner: "Are the technologies and blueprints 07 scans and builds really suitable for 07? There are many kinds of
> robots; how do we make the structures and upgrades 07 builds fit 07?"
>
> Grounded in:
> - `07-identity.md`: the SSR-5 Station Service Rover. Its verbs are scan (a laser-line profiler for the shop's
>   plotter), cut, build and weld (it assembles flat-packed station modules from its program library), plus two kit
>   sockets.
> - VISION ruling 12 ("built for a rover") and ruling 14 ("no hands").
> - `scale.md`: 07 is 1.47 × 2.01 × 1.59 m; its work bands are scoop 0.12–0.35 m, cutter 0.73 m, lens 1.23 m, cargo
>   hatch 0.48–0.80 m and 0.58 × 0.355 × 0.23 m.
> - `opening-v2.md` §10 (the start kit, every function costs battery);
> - `inventions.md`;
> - story v4 (ILC and Lumen; the elders are SSR-3 units; Meridian and the Tycho Consortium; Kestrel-3's foreign livery);
> - `0.6-living-moon.md` §1a.6 (two machine families).

## 0. Player experience (skill `player-experience`)
| Question | Answer |
|---|---|
| **Hook** | The first scan *tells you what it is to 07*: `ILC-R · BUILDABLE` lights in amber, and seconds later 07 builds it. A scan is never a mystery box. |
| **Pull** | Different robots and stations hold different kinds of knowledge. The elders carry 07's own kit family, foreign machines carry things 07 can't use *yet*, and crew equipment carries the story. **"Foreign standard · needs adapter"** is a door the player wants to open. |
| **Hold** | Every blueprint lands on 07's body or base in a way that *makes sense for a rover*: rover-height ports, self-raising masts, kits in its sockets. The base looks like a robot's base, never a house. |
| **Finish** | Opening the foreign standard (Meridian's hex docks) is a mid-game turn: two machine families working together, and 07 the one machine that bridges them. |
| **Feeling** | **Competence and logic:** "of course 07 can build that; of course it can't build that." The player trusts the world's rules. |

**Learn, then improve:**
- Subnautica lets the player scan and build anything, with no reason why a diver can fabricate a nuclear reactor.
- Ours gives every blueprint a standard, a method and a reason, visible on the scan line. It also turns
  incompatibility (foreign tech) into a goal, not a wall.

## 1. The fit rules (every blueprint must pass; each is checkable)

### F1. It declares one hardware standard
| Standard | What it is | What 07 can do | Source |
|---|---|---|---|
| **A · ILC-R** ("rover-serviceable") | Lumen station hardware designed to the ILC standard so that service rovers can assemble and maintain it. It uses a few **formable members** (struts, plates, cable runs) plus salvaged standard components. Every interface sits at rover height. | **build directly** with the assembly program | Lumen's lander, ruins, masts and depots |
| **B · SSR-K** (SSR kits) | tool modules for the SSR family. The elders (SSR-3) carry the single-purpose versions; the SSR-5 library has a socket version of each. | **fit at a dock** (opening v2 §10: the socket screen on the CRT, docked only) | the elders' tools, Lumen kit crates, Kenji's workshop |
| **C · FOREIGN** | the Tycho Consortium's standard (Meridian, Kestrel-3): hex contact docks, its own cells, voltages and fasteners | **scan gives knowledge** (a Notes page and a "foreign" blueprint). **Building needs the Charge Adapter (or a Power Bridge for structures) first.** | Meridian's machines and station, Kestrel-3 |
| **D · CREW** | human-only equipment: the habitat, airlocks, suits, bunks, galley, ladders, seats | **never buildable.** A scan gives lore (a Notes page) or, for a separable part, raw Metal | the habitat ruins, the lander's crew deck, Meridian's (empty) crew provisions |

### F2. It names how 07 physically assembles it (ruling 14)
| Method | How it looks | Allowed for |
|---|---|---|
| **Form + weld** | Stock goes in through the hatch, and the hold's small former presses ILC members. The frame unfolds and self-raises (telescoping masts, folding arrays), and 07 welds the seams at its work band. | A |
| **Socket fit** | docked at a pad or bay, the kit is fitted into a mount or socket (the CRT socket screen, the bay's fitting arm) | B |
| **Adapter, then form or fit** | as above, but only once the Charge Adapter or Power Bridge exists | C |
| **Repair only** | an existing machine's damaged parts are cut out and replaced, then welded | A machines in the world (relays, the mass driver, glasshouse pumps) |
| **None** | not buildable | D |

**Banned:** anything needing hands, a person's height, climbing, or lifting above 07's reach without a machine the
player has built first (a crane or a bay arm).

### F3. Size and reach fit 07
- **Every weld, port, hopper and switch is at 0.35–1.46 m** (07's work band, lens at 1.23 m). Taller structures
  **self-raise** after welding, so the weld points stay at the base.
- **No single prefabricated part bigger than the hatch** (0.58 × 0.355 × 0.23 m). Big things are formed on site from
  stock, never carried whole.
- **Anything 07 drives onto clears 07** (scale.md: ≥ 1.57 m clear width, ≥ 0.1 m per side; no overhead part below
  1.65 m).
- **Towing or carrying** large salvage needs a kit (a winch or cargo rack); it never happens "by beam".

### F4. Power fits 07 and its base
- **Building costs 07's cells** (opening v2 §10: every function costs battery). A blueprint states its build energy,
  and it must be ≤ 1 cell at the tier where it first appears.
- **Running it needs the grid:** `consumeWatts` must be within what that version's base can generate (§4's lint
  checks this against the solar or generator supply available then).

### F5. It has a job on a robot base
- **Allowed purposes:** charge, generate, store power, signal, refine, fabricate (rover parts), store materials,
  dock vehicles, move, sense, a tool.
- **Never on a buildable:** doors, beds, seats, air, food, ladders, human-height controls (ruling 12).
- Crew things stay as ruins that *tell*.

### F6. Its purpose reads at a glance, and its shape obeys vacuum logic
- Silhouette and moving parts tell the job from 30 m.
- No balloons, wings, propellers or sails (`vacuum-shapes.md`).

## 2. How the player reads it (the scan line, one element)
The line is part of the **scan result** (the ScanLines / PickupFeed readout on the CRT or pickup note). It is not
an extra guidance element. It shows 3 s, in one line, and once per fragment set (later fragments of the same set show
"FRAGMENT n/N").

| Case | en | vi |
|---|---|---|
| A, set complete | ILC-R · BUILDABLE | ILC-R · DỰNG ĐƯỢC |
| A, partial | ILC-R · FRAGMENT 2/3 | ILC-R · MẢNH 2/3 |
| B, set complete | SSR KIT · FIT WHEN DOCKED | KIT SSR · LẮP KHI ĐỖ Ở BỆ |
| C, set complete | FOREIGN STANDARD · NEEDS ADAPTER | CHUẨN NGOẠI · CẦN BỘ CHUYỂN ĐỔI |
| C, with the adapter | FOREIGN STANDARD · ADAPTED · BUILDABLE | CHUẨN NGOẠI · ĐÃ CHUYỂN ĐỔI · DỰNG ĐƯỢC |
| D | CREW EQUIPMENT · NOT BUILDABLE → METAL 3 | THIẾT BỊ PHI HÀNH ĐOÀN · KHÔNG DỰNG ĐƯỢC → KIM LOẠI 3 |
| D, lore only | CREW EQUIPMENT · LOGGED | THIẾT BỊ PHI HÀNH ĐOÀN · ĐÃ GHI |
| Known blueprint | KNOWN · → METAL 3 | ĐÃ BIẾT · → KIM LOẠI 3 |
| Repair target | ILC-R · REPAIRABLE | ILC-R · SỬA ĐƯỢC |

**On the CRT build menu,** a C blueprint shows dimmed with the adapter's icon (no sentence). The build menu's
need/have rows stay as they are.

## 3. Audit of today's content
**Mounts vs. sockets first** (a conflict to resolve). 07-identity and opening v2 §10 say the SSR-5 has **2 kit
sockets**; the 0.5.5 equipment spec drew 12 sockets. Proposed reconciliation:
- **Device mounts** are fixed and upgraded *in place* (tier swaps):
  - lens (scan);
  - cutter;
  - lamp;
  - solar wing;
  - battery (back);
  - hold;
  - wheels;
  - antenna.
- **Kit sockets 1–2** hold *new capabilities*, and the player chooses which two:
  - jump coils or thrusters;
  - winch;
  - cargo rack;
  - capacitor drums;
  - scoop;
  - seismic bell.
- A **third socket** can be a late upgrade.
- **Owner decision:** this makes kits a real choice. Two sockets also limits what 07 can do at once.

### 3.1 Structures (`StructureCatalog`, 10)
| Structure | F1 std | F2 method | F3 size and reach | F4 power | F5 job | Verdict / fix |
|---|---|---|---|---|---|---|
| charging_pad | A | form + weld | **FAIL:** rails, ramp and bollard collide with 07 (scale.md §3.2) | 120 W ok | charge | **fix:** widen the ramp and rails, move the bollard (Argon) |
| solar_array | A | form + weld, unfolds | pass (1.94 m tall, self-raising; welds at the base) | generates | generate | pass |
| battery_bank | A | form + weld | pass (1.60 m; its terminals must sit ≤ 1.46 m) | stores | store | pass (check the terminal height) |
| power_pylon | A | form + weld, telescoping | pass (1.95 m, self-raising) | 15 W | signal and power reach | pass |
| antenna_mast | A | form + weld, telescoping | pass (2.80 m, self-raising; the dish aims itself) | 90 W | signal | pass |
| smelter | A | form + weld | its hopper must be ≤ 1.2 m (2.38 m tall) | 150 W | refine | pass if the hopper is at rover height; **check the model** |
| fabricator | A | form + weld | its output tray must be ≤ 1.2 m; **no human workbench look** | 140 W | fabricate rover parts | pass with a re-theme (a rover-height press, no bench) |
| storage_rack | A | form + weld | **FAIL:** top shelf at 1.62 m, out of reach | 10 W | store materials | **fix:** lower it to ≤ 1.46 m, or add a self-lift (a rack carousel) |
| upgrade_bay | A | form + weld | pass (2.75 m; its arms do the overhead work) | 160 W | fit kits | pass (it *is* the "machine built first" for overhead work) |
| vehicle_pad | A | form + weld | **FAIL:** the hoist hook at 1.23 m hits 07's head; the ramp lip is too narrow | 100 W | dock vehicles | **fix:** raise the hook +0.55 m, widen the lip ×1.25 (scale.md) |

### 3.2 Kits and upgrades (`Upgrades/*`)
| Kit | F1 | Mount or socket | Source (proposed) | Verdict / fix |
|---|---|---|---|---|
| hover_jump (coils) | B | kit socket | the elder **04**'s lift-cart spring legs | pass (vacuum-ok springs). Later superseded by the cold-gas thrusters (invention 05) in the same socket |
| cargo_cradle (rack) | B | kit socket | the haul sled (Lumen work bot) | pass. **Re-theme:** it carries bulky salvage, not relics (relics are cut in opening v2) |
| cell_pack (battery spine) | B | battery mount (tier) | elder **06**'s battery bay | pass |
| focus_lens | B | lens / cutter mount (tier) | **move** from Kestrel-3's cockpit (that's foreign, C) to Kenji's workshop ruin (Lumen) | **fix:** the source |
| side_bins | B | hold mount (tier) | Lumen kit crate at the lander | pass |
| warm_headlamp (lamp bar) | B | lamp mount (tier) | Mule's head lamp (Lumen work bot) | pass |
| boost_coils (capacitor drums) | B | kit socket | the haul sled's capacitor bank | pass |
| radio_tower (upgrade) | — | — | — | **CUT:** the tower is a ruin in opening v2 (Link is built with masts) |
| tether ring, tape deck (legacy kits) | — | — | — | **CUT** (opening v2) |
| igniter (opening v2 §10) | B | cutter mount (repair part) | the lander's open crate | pass (a repair part, not a blueprint) |

### 3.3 Inventions (`inventions.md`, 11)
| # | Invention | F1 | F2 | F5 job on a robot base | Verdict / fix |
|---|---|---|---|---|---|
| 01 | Ilmenite Breather | A | form + weld | refines oxidizer (O₂) for thrusters, the lance and glass-blowing; **07 doesn't breathe** | pass with the purpose stated as *industrial O₂*, never "air". **Note:** Oxygen isn't in M3-27's material list; add it as a refined material or drop it |
| 02 | Ember Shell & Frost Harp | B (launcher in a kit socket) + A (harp structure) | socket fit / form + weld | lights the dark crater | pass |
| 03 | Trial Glasshouses & Algae Lantern | **D** (crew greenhouse) | — | crew food | **fix:** not buildable. **Repair target only:** its ILC-R pumps and lamps can be *repaired* for an O₂ trickle and a charger (A, repair) |
| 04 | Pneumatic Post | A | form + weld (tube runs at rover height) | moves stock between structures | pass |
| 05 | Cold-gas thrusters | B | kit socket | mobility | pass. **Source:** an elder's (04's) thruster pack or a Lumen crate, not Meridian |
| 06 | Thermic Oxy-Lance | **C** (fragments in Meridian's vault) | adapter, then fit | cutter tier 3 | pass **only after the Charge Adapter**; scan line: "needs adapter" |
| 07 | Mass Driver Rail | A | **repair only** | launches ore to L5 | pass as a repair target, never built new |
| 08 | Heliostat and Molten-Salt Core (+ Cell Heater kit) | A + B | form + weld / socket | generate heat and power in the dark | pass |
| 09 | Glass-Blower and Basalt Spinner | A | form + weld | refine glass and fibre | pass (feeds rover-height hoppers) |
| 10 | Electrostatic Louver-Comb | A | form + weld | harvests charged dust at the terminator | pass |
| 11 | Seismic Bell | B | kit socket (a field device it drops and rings) | sense | pass |

**Totals:**
- 3 structure fails (pad, storage rack, vehicle pad: geometry fixes);
- 2 checks (the smelter's hopper, the fabricator's look);
- 1 source fix (the Focus Lens);
- 1 category fix (the glasshouses become a repair target);
- 3 cuts (radio tower, tether ring, tape deck);
- 1 material gap (Oxygen).

## 4. Data and lint proposal
**Fields every blueprint asset carries** (structures and kits; `StructureDefinition` / kit definitions):
| Field | Values |
|---|---|
| `standard` | IlcR, SsrKit, Foreign, Crew |
| `assembly` | FormWeld, SocketFit, AdapterThenFormWeld, AdapterThenSocketFit, RepairOnly, None |
| `mount` (kits) | a device mount (Lens, Cutter, Lamp, Wing, Battery, Hold, Wheels, Antenna) or KitSocket |
| `purpose` | Charge, Generate, StorePower, Signal, Refine, Fabricate, StoreMaterials, Vehicle, Mobility, Sense, Tool |
| `buildEnergyCells` | float (≤ 1 at its first tier) |
| `consumeWatts` | exists |
| `sourceKind` | LumenStation, Elder, LumenWorkBot, Foreign, Crew |
| `requires` | blueprint ids (Foreign must list `charge_adapter` or `power_bridge`) |
| `scanLineKey` | a localization key for §2's line |

**Content lint** (`tools/content_lint.py blueprints`, plus an EditMode test so `box_check` runs it):
1. Every blueprint has all fields; `scanLineKey` exists in en and vi.
2. `Crew` ⇒ `assembly = None`. `Foreign` ⇒ `requires` contains the adapter or bridge. `RepairOnly` is never in the build
   menu.
3. **Reach:** for buildable prefabs, every interaction node (`Port_*`, `Hopper_*`, `Weld_*`, `Socket_*`) has Z in
   [0.35, 1.46] m. This reads the art QA contract JSONs (`tools/art/qa`).
4. **Drive-on clearance:** prefabs with a `DriveOn` tag have clear width ≥ 1.57 m and no node below 1.65 m over the
   deck.
5. **Kit envelope:** a kit's bounds fit its mount's envelope (from `rover.json`).
6. **Power:** `consumeWatts` ≤ the generation available at the blueprint's version (a table per version in tuning).
   `buildEnergyCells` ≤ 1 at first tier.
7. **No human features:** buildable prefabs have no nodes named `Bed`, `Seat`, `Ladder`, `Door_Human`, `Galley`, `Bunk`.
8. **Vacuum shapes:** no nodes or tags `Propeller`, `Balloon`, `Wing_Lift`, `Sail`, `Parachute`.

The lint fails the build (box_check) on any violation, so new content can't drift.

## 5. Robot variety feeds 07's growth (which kit from which robot)
| Robot (family) | What it was | What 07 learns from it | Std |
|---|---|---|---|
| Elder **06** (SSR-3, relay keeper) | held relay.2's link | **Cell Pack** (battery mount), from its battery bay | B |
| Elder **05** (SSR-3, dish tender) | tended the mare dish | **Antenna transmit module** (the antenna mount: receive-only → transmit) | B |
| Elder **04** (SSR-3, lift cart) | carried parts up the terraces | **Jump coils**, then the **cold-gas thrusters** (kit socket) | B |
| Elder **03** (SSR-3, ice cart) | carried ice to the garden | **Cell Heater** (battery mount, cold) | B |
| Elder **02** (SSR-3, shelter cart) | stocked the lava-tube shelter | **Floodlights** (lamp mount tier) | B |
| Elder **01** (SSR-3, signal rover) | went to repair the dish | **Star Tracker** (antenna mount tier, aims the great dish) | B |
| **Mule** (Lumen lamp-and-lift bot) | planted lamp posts | **Warm Headlamp** (lamp mount) | B |
| **Haul sled** (Lumen work bot) | hauled the drill's loads | **Boost Coils** and the **Cargo Rack** (kit socket) | B |
| **The sweeper** (Lumen work bot) | cleaned the solar-field road | **Wing wiper** (solar wing tier: a dust-free wing) | B |
| **Panel mites** (Lumen) | cleaned panels | nothing to fit (too small); their scan page teaches *why* clean arrays matter | — |
| **Lumen lander, ruins, masts** | station hardware | pad, solar, battery bank, pylon, mast, smelter, fabricator, rack, bay, vehicle pad | A |
| **Kenji's workshop** (ruin) | built and fixed rovers | **Focus Lens** (lens/cutter mount), the **igniter** spare | B |
| **Meridian hex docks** (foreign) | Meridian's chargers | **Charge Adapter** (the key that opens C) | B (built from C knowledge) |
| **Meridian survey flock / prospector** (foreign) | sounded the mare | **Sounding kit** (kit socket) after the adapter | C |
| **Meridian ice tappers** (foreign) | prospected the crater | **Core drill** (kit socket) after the adapter | C |
| **Meridian vault** (foreign) | the station's store | **Thermic Oxy-Lance** (cutter tier 3) after the adapter | C |
| **Kestrel-3** (foreign relay satellite) | Meridian's relay bird | knowledge only (the foreign standard's first hint) and Metal | C |
| **Crew habitat, airlock, suits** | the crew's world | story pages, Metal | D |

**The rule this table keeps:**
- **07's own family (Lumen, SSR) teaches what it can use now;**
- **the foreign family teaches what it needs to earn;**
- **the crew's world teaches who was here.**

## Tóm tắt (Tiếng Việt)
- **Câu hỏi của chủ dự án:** đồ 07 quét và dựng có hợp với 07 không? Câu trả lời là mỗi bản vẽ phải qua 6 quy tắc
  kiểm được:
  1. Một chuẩn phần cứng:
     - A · ILC-R: đồ của trạm Lumen, làm theo chuẩn "xe tự hành bảo dưỡng được", dựng thẳng;
     - B · kit SSR: lắp vào ổ khi đang đỗ ở bệ, học từ dụng cụ của các đàn anh;
     - C · chuẩn ngoại (Meridian, Kestrel): quét để biết, muốn dựng phải có bộ chuyển đổi;
     - D · đồ của phi hành đoàn (nhà ở, cửa khoá, đồ bảo hộ): không bao giờ dựng được, quét ra truyện hoặc 3 Kim loại.
  2. Một cách lắp không cần tay:
     - ép và hàn, khung tự dựng lên;
     - lắp vào ổ;
     - qua bộ chuyển đổi;
     - chỉ sửa chữa.
  3. Kích thước: mọi điểm hàn, cổng và phễu ở độ cao 0,35–1,46 m; đồ cao thì tự dựng lên; không bộ phận nào to hơn
     cửa khoang.
  4. Điện: dựng tốn tối đa 1 ô pin; chạy phải nằm trong sức của lưới điện ở phiên bản đó.
  5. Có việc cho căn cứ robot: không cửa, giường, ghế hay không khí.
  6. Nhìn hình là hiểu, và đúng logic chân không.
- **Dòng quét trên CRT,** ví dụ "ILC-R · DỰNG ĐƯỢC", "CHUẨN NGOẠI · CẦN BỘ CHUYỂN ĐỔI", "THIẾT BỊ PHI HÀNH ĐOÀN ·
  KHÔNG DỰNG ĐƯỢC → KIM LOẠI 3". Nó là một phần kết quả quét, không thêm yếu tố hướng dẫn.
- **Kiểm tra nội dung hiện có:**
  - 3 công trình hỏng về kích thước: bệ sạc, giá chứa đồ, bệ xe;
  - 2 cần kiểm: phễu của lò luyện và hình dáng máy chế tạo;
  - Focus Lens phải đổi nguồn (Kestrel là đồ ngoại);
  - nhà kính thành mục tiêu sửa, không dựng được;
  - bỏ tháp radio, vòng dây kéo, máy băng;
  - thiếu vật liệu Oxy trong danh sách.
- **Ổ lắp:**
  - **Chủ dự án cần quyết:** thiết bị cố định (ống kính, mỏ cắt, đèn, cánh, pin, khoang, bánh, ăng-ten) nâng cấp tại
    chỗ; chỉ 2 ổ kit cho khả năng mới, nên phải chọn.
- **Dữ liệu và lint:** mỗi bản vẽ khai chuẩn, cách lắp, ổ, mục đích, điện, nguồn. Một bài lint tự động chặn bản vẽ
  sai quy tắc, đo độ cao từ hợp đồng model, kiểm khoảng hở cho 07, cấm đồ của người và hình dạng không hợp chân không.
- **Robot nào dạy gì:**
  - họ Lumen (đàn anh, robot làm việc) dạy thứ 07 dùng được ngay;
  - họ ngoại dạy thứ phải giành lấy;
  - đồ của phi hành đoàn kể chuyện ai đã ở đây.
