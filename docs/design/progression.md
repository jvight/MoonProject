# Progression: the spine of the game (07 grows, technology grows, the moon opens)

> Author: the story & flow session, for the Director and the owner to approve. A proposal: nothing here changes
> VISION, DESIGN, ROADMAP or a feature spec until it is approved. It replaces the "home arc" framing, which the owner
> rejected on 2026-10-11:
> > "Not compelling enough, the purpose isn't clear. The focus should be development: upgrading the robot, unlocking
> > and crafting new features, more technology, going farther."
>
> The crew's memories, logs and letters stay, as seasoning.

## Tóm tắt cho chủ dự án
1. Trục chính của game là **phát triển**: nâng cấp 07, mở khoá và chế tạo công nghệ mới, đi ngày càng xa. Ký ức, nhật
   ký và thư của phi hành đoàn chỉ còn là gia vị.
2. **Vòng lặp:** ra ngoài → trục vớt vật liệu và tìm đồ công nghệ hiếm (bản vẽ, linh kiện) → về nhà, giải mã ở phòng lab
   → chế tạo và lắp ở trạm sửa xe → 07 thay đổi thấy rõ → đến được vùng mới, trục vớt được đồ khó hơn → lặp lại.
3. **Cây công nghệ** gồm 29 nút trong 6 nhánh: Di chuyển, Tia, Cảm biến, Năng lượng, Bạn bè & drone, Trạm.
4. Mỗi phiên bản mở một tầng: 0.5 có 9 nút (8 nút đã làm), 0.6 có 8, 0.7 có 7, 0.8 có 5 và kết thúc bằng chảo ăng-ten lớn.
5. Vật liệu có 3 bậc: phế liệu (kim loại, dây điện, quang học) → hợp kim, mạch điện, tinh thể → siêu dẫn. Thêm vào đó là
   linh kiện hiếm và bản vẽ nằm trong lõi các xác tàu.
6. 07 đi từ một tàu thăm dò cũ, mệt mỏi, thành một rover thám hiểm vững chãi: bánh xích to, cánh pin đôi, đèn pha, tời
   neo… Gương mặt một mắt hiền lành thì không bao giờ đổi.
7. Căn cứ cũng nâng cấp theo: tháp radio, cánh tay trạm sửa xe, máy chế tạo, đầu đọc ở phòng lab, chuỗi cột tiếp sóng,
   và cuối cùng là chảo ăng-ten lớn.
8. **Bên trong tàu đổ bộ là phòng lab** của phi hành đoàn. Thang nâng dưới bụng tàu đưa 07 lên, và bản vẽ được giải mã
   thành công nghệ ở đó. Kỷ vật chỉ giữ một góc nhỏ.
9. **Đi xa hơn:** mỗi vùng mới cần một công nghệ (hẻm núi: nhảy; bậc thềm: bánh từ tính; hố tối: đèn pha mạnh; đỉnh
   núi: tời neo). Cột tiếp sóng biến vùng mới thành "nhà". Không bao giờ có giới hạn pin hay nhiên liệu.
10. **Thay đổi cho 0.5 rất gọn:** giữ các bước của M3-16; "bộ phận câu chuyện" đổi tên thành linh kiện; bản vẽ Hover-Jump
    được tìm thấy ở mép vực và giải mã trong lab; thêm một bảng công nghệ trên tường lab.

## 1. The core loop is growth
```
go out ──► salvage materials ──► find the site's heart: a blueprint or a component
   ▲                                              │
   │                                              ▼
reach new places,                     home: up the belly lift into the LAB,
harder salvage,                       where the reader decodes the blueprint
richer hearts                         and a node lights on the tech board
   ▲                                              │
   │                                              ▼
07 visibly changes ◄── the BAY fits the kit ◄── the plotter draws the sheet
(ruling 11)            (materials + a component)
```
Every step should feel like technology moving forward:

| Step | What the player feels | How the game shows it |
|---|---|---|
| **Go out** | "my rover can do more than last time" | each trip uses a verb that didn't exist a version ago (leap, climb walls, tow heavy, glide, anchor) |
| **Salvage** | "my tools open things I couldn't before" | wrecks come in kinds: open, **heavy** (needs Heavy Tow), **sealed** (Thermal Cutter), **dark** (Floodlights), **high** (Anchor Line). A sealed crate seen in hour one opens in hour four. |
| **Tech finds** | "a real find, not just more scrap" | each site's **heart** holds one rare thing: a **blueprint** (know-how) or a **component** (a named part: a coil, a gyro, a heat sink). It rides home on 07's back, glowing (ruling 11). |
| **Research** | "I learned something" | in the lab, 07's beam sets the blueprint in the **reader**. It decodes in ~4 s, a moment and never a timer, and a node lights on the lab's **tech board**. A cable pulse runs from the lander to the bay. |
| **Craft and fit** | "I built it" | the bay's plotter draws the sheet. 07 parks on the turntable and feeds the hopper (materials and the component), and the arms fit the kit (the install moment, M3-11). |
| **07 changes** | "look at my rover now" | one visible kit piece per node, readable from the chase camera and at 30 m |
| **Go farther** | "now I can" | a gate the player has seen for a while opens (ruling 10), and new wrecks there hold the next tier's materials |

**Two places, two jobs at home:** the **lab** turns finds into know-how, and the **bay** turns know-how and materials
into kit. A trip home with a find goes up the lift; a trip with only materials goes straight to the bay.

## 2. The tech tree: 29 nodes in 6 branches
**Rules for every node:**
- No circular needs: a material or component is always obtainable with nodes from an earlier step. In 0.6:
  Servo Pack → Bay Arms II → Heavy Tow → Alloy → the Fabricator, which then makes any component already found
  once.
- One node gives **one** new ability or one clear improvement, and **one** visible change on 07 or at home.
- Kit nodes need a **blueprint** (found) + **materials** + at most **one component**. Station nodes need a component
  (P1's rule) + materials.
- Costs follow ruling 5: each version's sites hold about 2× what its nodes need.
- **Power never limits range.** No battery or fuel meter ever forces 07 home (VISION, DESIGN "Station reach"). Power
  nodes make abilities stronger or faster, never possible-for-a-while.

✔ = built today (on main).

### Mobility: where 07 can go
| Node | Ver | Needs | Does | Visible |
|---|---|---|---|---|
| Hover-Jump ✔ | 0.5 | blueprint (the chasm's lip, §6) + T1 | charged leap over chasms; opens Whispering Canyon | spring coils under the belly, glowing when charged |
| Magnetic Treads | 0.6 | blueprint (the crawler at the terraces' foot) + T1 + Gyro | climbs marked walls only (DESIGN's lane rule); opens the Rim Terraces | six big **matched** treaded wheels; the odd wheel is healed and 07 sits higher |
| Long Leap | 0.7 | blueprint (crater) + Alloy + Gyro | the leap becomes a steerable glide; crosses the crater's wide rifts | glide vanes folded on the flanks, opening mid-air |

### The beam: what 07 can work (ruling 14: its one tool)
| Node | Ver | Needs | Does | Visible |
|---|---|---|---|---|
| Steady Beam | 0.5 (new, small) | blueprint (Kestrel) + T1 | cuts 30% faster; the salvage melody runs quicker | a brass focusing collar on the emitter; the beam gains a second, finer strand |
| Cargo Cradle ✔ | 0.5 | blueprint (garage) + Metal | carries a find home in a rack instead of towing | the strapped rear rack (straps parallel, P3) |
| Heavy Tow | 0.6 | blueprint (a heavy wreck at the basin's edge) + T1 | tows **heavy** salvage (hull sections, cargo pods): Alloy's source | an A-frame tow mast with a cable spool at the rear |
| Thermal Cutter | 0.7 | blueprint (crater) + Alloy + Heat Sink | opens **sealed** wrecks (vaults, pressure hulls): Circuitry's source, and the basin's sealed crates from 0.5 | finned heat sinks on the emitter that glow orange while cutting |
| Anchor Line | 0.8 | blueprint (the Peak's foot) + Superconductor + Power Cell | the beam locks onto the crew's anchor posts and reels 07 up the Peak road's sheer pitches | a winch drum under the nose, its cable glinting |

### Sensors: what 07 can know
| Node | Ver | Needs | Does | Visible |
|---|---|---|---|---|
| Warm Headlamp ✔ | 0.5 | blueprint (drill) + Optics | a wider, warmer pool; sonar pillars stay longer | the caged lamp bar across the front |
| Wide Sonar | 0.6 (from 0.7) | blueprint (Bell's signal) + T1 + Lens Array | ping range ×2; answers carry across a region | a larger dish on the back, and the bent antenna is straightened |
| Deep Scan | 0.6 | blueprint (terraces) + Alloy | a ping shows what a site holds (its material tier, whether its heart is a blueprint or a component) before the drive | a slow rotating scanner vane on the antenna mast |
| Floodlights | 0.7 | blueprint (the terraces' high wreck) + Alloy + Lens Array | lights the Shadowed Crater's deep dark; opens it | twin floodlights on a roof bar above the lamp bar |
| Star Tracker | 0.8 | blueprint (the Peak) + Superconductor | aims the great dish at Earth (the finale needs it) | a small brass tracker scope on the mast |

### Power: how strongly 07 works (never how far)
| Node | Ver | Needs | Does | Visible |
|---|---|---|---|---|
| Boost Coils ✔ | 0.5 | blueprint (garage or drill) + Wiring | a gentle extra cruise on flat ground | twin capacitor drums on the flanks, glowing while boosting |
| Fast Charge | 0.6 | blueprint (terraces) + Alloy + Wiring | Hover-Jump charges twice as fast; the beam's first second cuts at double speed | copper bus bars linking the drums to the coils, pulsing while charging |
| Twin Solar Wing | 0.7 | blueprint (crater) + Crystal + Optics | abilities refill while 07 rests in light; the stargaze gains a soft sun-soak | **a second solar wing**: the tired single wing becomes a pair, both whole |
| Power Core | 0.8 | Superconductor + Power Cell (no blueprint: Kenji's last sheet is already on the board) | every ability at full strength; drives the Anchor Line's reel | a round glass core window on the flank, glowing amber |

### Friends and drones: help, and gentle automation
Friends are **found** (story beats); their upgrades are **crafted** at the bay for them.

| Node | Ver | Needs | Does | Visible |
|---|---|---|---|---|
| Tilly ✔ (found) | 0.5 | her 3 parts, stitched | spots glints and sites near 07 | Tilly at 07's side |
| Bell ✔ (found) | 0.5 | her parts and a tape | the dial; her signals point to things not yet found | Bell's corner at home |
| Scout Module | 0.6 | blueprint (Bell's signal) + T1 + Lens Array | Tilly flies ahead to a pillar and Deep-scans it | a tiny antenna and lens on Tilly |
| Atlas (found) | 0.6 | heavy parts, a long stitch | lifts heavy things: rockfalls, later the dish | Atlas resting by the bay |
| Ferry Harness | 0.7 | blueprint (crater) + Alloy + Servo Pack | Atlas carries **heavy salvage** home from where 07 marks it (never memories: they ride with 07) | a cargo harness on Atlas |
| Courier Clip | 0.7 | blueprint (terraces) + Circuitry | Tilly carries small finds (tapes, logs, components) home | a little clip basket under Tilly |
| Moss (found) | 0.7 | parts + a seed | the biodome at home grows **Crystal**, the one renewable material, at a gentle pace | the biodome beside the lander |
| Sister Drones | 0.8 | Circuitry + Crystal + Gyro (no blueprint: Tilly's own schematic) | two small drones sweep loose T1 bits inside reach, so late game skips the small chores | two drones on perches at home, flying out with 07 |

### Station: home's machines and its reach
| Node | Ver | Needs | Does | Visible at home |
|---|---|---|---|---|
| Tower L1 ✔ | 0.5 | Tower Coil (component) + T1 | power to home: the lander, the lab, the lift | windows lit, belly lamp, the radio clears |
| Tower L2 ✔ | 0.5 | Transformer + T1 | power to the bay | the bay's shutter, sign and arms |
| Tower L3 ✔ | 0.5 | Relay Spare + T1 | power to the lab's survey chart (M3-19 D8, M3-07) | the chart's lamp in the big porthole |
| Relay network ✔ | 0.5 → 0.8 | per mast: T1 (escalating) + its part | reach: a clear radio, the chart inks, radio-hop between lit masts, letters play back | a chain of lit masts across the moon |
| Bay Arms II | 0.6 | T1 + Servo Pack | the bay can fit **tier-2 kits** (Treads, Heavy Tow, …) | a second gantry; bigger, cleaner arms |
| Fabricator | 0.6 | Alloy + T1 | makes **components** from materials once each one has been found once, so a missing part never blocks progress | a new compact machine beside the bay, with a warm work light |
| Lab Reader II | 0.7 | Circuitry + Lens Array | decodes **sealed data cores** (blueprints from 0.7 and 0.8) | a second screen inside; outside, a new antenna on the lander's roof |
| The Great Dish | 0.8 | Superconductor ×many + Power Cell + Atlas's lift + the chain linked + the Star Tracker | the broadcast: the ending | the Peak's dish turned toward Earth; its beacon turns amber |

**Count:** 3 Mobility, 5 Beam, 5 Sensors, 4 Power, 4 crafted friend nodes (plus 4 friends found), 8 Station = **29
nodes.** By version: 0.5 has 9 (8 built, plus Steady Beam), 0.6 has 8, 0.7 has 7, 0.8 has 5.

## 3. What makes it feel like "more technology" over time
1. **Materials have tiers,** and new ones arrive with new tools and regions. The HUD's material row grows, revealing
   itself (M3-16 rule 2).

   | Tier | Materials | Where | From |
   |---|---|---|---|
   | T1 scrap | Metal, Wiring, Optics ✔ | every basin wreck, with site signatures (P9) | 0.5 |
   | T2 refined | **Alloy** (heavy hulls), **Circuitry** (sealed vaults), **Crystal** (the crater; Moss grows it) | heavier, sealed and darker wrecks, which need 0.6–0.7 tools | 0.6–0.7 |
   | T3 rare | **Superconductor** | the wrecks along the Peak road and the dish itself | 0.8 |

2. **Components are named tech finds:** Tower Coil, Transformer, Relay Spare (✔ P1); Servo Pack, Gyro and Lens Array (0.6); Heat
   Sink (0.7); Power Cell (0.8). The first of each is always a heart find, which is a moment. From
   0.6 the Fabricator can make more, so a missing one never blocks progress.
3. **Recipes grow.** 0.5 recipes use only T1. 0.6 adds a component and Alloy. 0.7 adds Crystal and Circuitry. 0.8 adds
   Superconductor. The bay's choice list shows the next tier as it is decoded, never before.
4. **Home's machines upgrade,** and each upgrade changes home's silhouette or its light (ruling 11):
   - the tower L1–L3;
   - the bay's arms;
   - the Fabricator;
   - the Lab Reader;
   - the relay chain;
   - the dish.

   Seen from the dock's evening, home goes from three dark masses to a small, lit research station humming with
   machines.
5. **The tech board on the lab wall** is the crew's R&D chart of "what 07 could be": six branches, each node lit when
   unlocked. It shows only the current version's nodes (phase honesty: no promises beyond what ships). Its glow
   reaches the lander's big porthole, so the outside view shows it too.
6. **07's silhouette: from a tired probe to an expedition rover.**

   | Version | 07 looks |
   |---|---|
   | start | half-lidded, one tired wing with a missing cell, a bent antenna, one odd wheel, faded paint |
   | 0.5 | belly coils, the lamp bar, flank drums, the rear rack, the beam collar; Tilly's cell in the wing; Bell's fresh "07" |
   | 0.6 | **taller on big matched treads**; a large dish and a straight antenna; a tow mast; a scanner vane |
   | 0.7 | **two solar wings**; floodlights; glide vanes; heat-sink fins |
   | 0.8 | a winch drum, a glowing core window, a tracker scope: a capable expedition rover |

   Art rules for the arc:
   - every kit piece shares 07's design language: cream enamel, the worn orange stripe, brass fittings;
   - 07 never looks spiky or militarised, its rear never reads as a face (P3), and its one-lens face never changes;
   - silhouette budget: by 0.8, 07's bounds grow ≤ 25% in height and ≤ 15% in width, so it stays small against the
     moon (pillar 6).
7. **New verbs, one or more per version:**
   - 0.5: cut, tow, leap;
   - 0.6: climb walls, heavy tow, scan ahead;
   - 0.7: glide, cut sealed, light the dark;
   - 0.8: anchor and reel, aim the dish.

## 4. Going farther
- **Every region is gated by a node,** and every gate is seen long before it opens (ruling 10: real gates, one-way
  exits, never a trap):

  | Region | Ver | Gate | Seen first from |
  |---|---|---|---|
  | Whispering Canyon | 0.5 | Hover-Jump | the chasm's lip, with Bell's light across (P2) |
  | Rim Terraces | 0.6 | Magnetic Treads | the dock's evening and the survey chart |
  | Shadowed Crater | 0.7 | Floodlights | looking down from the terraces |
  | The Peak road | 0.8 | Anchor Line + Long Leap + Atlas (the rockfall) | the opening, and every frame since |

- **Better tools pay off where you have already been.** Each version's new salvage kind (heavy, sealed, dark, high)
  also appears in a few spots in the basin, seen since 0.5: a heavy pod beside the depot (Heavy Tow, 0.6), Kestrel's
  sealed core (Thermal Cutter, 0.7: Circuitry and a Lens Array). Going back is rewarded, never required.
- **The relay network is reach.** Every region gets a mast. Lighting it makes the region "home": a clear radio, the
  chart inks it, radio-hop back in one step, and its letters play. Longer trips stay short to return from.
- **Longer trips pay more.** Farther sites hold higher tiers and richer hearts, so the far trip is worth its drive.
  Radio-hop and Tilly's scout make it cheaper. Nothing is ever lost on the way (ruling 1).
- **No fuel, no range limit, no timer.** Power nodes change how strongly 07 works, never how far it can go.

## 5. The lander's purpose: the crew's lab
Lumen Station was a radio-relay and **research** outpost (STORY). The lander's cabin becomes **the crew's lab**, and
its one job is: **where finds become know-how.** M3-19's geometry stays: the belly lift, the round platform as a
turntable at the cabin's centre, and the ring-rail camera. Its stations around the ring:

| Station | Its job | What 07 does |
|---|---|---|
| **The reader** | decodes blueprints; from 0.7 (Reader II) sealed data cores too | the beam lifts the tube or core into the reader's tray; it decodes in ~4 s; a node lights on the board; a cable pulse runs to the bay, where the plotter draws the sheet |
| **The tech board** | the crew's R&D wall: the tree of what 07 can become | looks: each lit node shows its kit and its one line; unlit nodes for this version show their outline |
| **The survey chart** | Ines's map on the light table under the big porthole (M3-07; tower L3) | looks: the chart inks the station's reach |
| **The keepsake shelf** | the crew's things, a small corner | the beam sets a memory in its slot; logs clip beside it. Seasoning: never required, always warm. |

- **Every trip home with a find has a reason to go inside:** the blueprint goes to the reader. A memory found on the
  same trip goes on the shelf during the same ride.
- **Outside, the lab shows** as the warm big porthole (the board's glow and the chart) and the lander's roof antenna
  (Reader II).
- **The bay keeps making.** The plotter stays on the bay's back wall (M3-19 D7) and draws what the lab decoded, so the
  wire between the two buildings is the visible link between knowing and making.
- **The ring holds four stations** (M3-19 had five crew corners plus the map), which is simpler to build and to read.

## 6. What changes in what's already built for 0.5 (keep the scope sane)
| Built | Change | Box | Size |
|---|---|---|---|
| M3-16 steps and power spine | **Kept.** Steps 0–8 stay in order. The words change: P1's story parts become **components** (Tower Coil, Transformer, Relay Spare) in text and cards. | ui (text), gameplay (keys) | S |
| Step 4 "first memory" | Becomes **"first know-how"**: at the chasm (P2), the crew's broken bridge anchor (L4) holds the **Hover-Jump blueprint**; Kenji meant to jump it. 07 brings it home, restores the belly lift (stitch), rides up, and the reader decodes it: the tech board's first node lights. The duck is still found on the way and goes on the shelf on the same ride. | gameplay (tube placement, delivery point), world (anchor at the lip), art (reader, board) | M |
| Blueprints (P7) | Hover-Jump is no longer pre-pinned: it is found (above). Tubes are delivered at the **lab's reader** instead of the plotter. The plotter draws after the decode (a cable pulse). | gameplay | S |
| M3-19 interior | The five crew corners become the **four lab stations** (§5). Geometry, lift, turntable and camera are unchanged. Tower L3 still wakes the chart (D8). | art, gameplay, rover (unchanged) | M (similar to M3-19 as drafted) |
| The tech board | New in 0.5: a wall chart with 0.5's nine nodes, lit as they unlock. It is static art with lamps, driven by unlock events. | art + ui (labels) | S–M |
| Steady Beam | Optional for 0.5: one new node (tuning and a collar mesh), the first beam upgrade, whose blueprint is Kestrel's heart | gameplay (tuning), art (collar) | S |
| Economy (P9) | Unchanged in 0.5: T1 only, signatures, 2× | — | — |
| Not in 0.5 | T2/T3 materials, the Fabricator, Bay Arms II, new regions, the friend nodes, Wide Sonar | — | — |

**Core contracts (the Director lands them, when approved):**
- `TechNodeId` ids, `ITechTree` (IsUnlocked, CanCraft), `TechNodeUnlocked(nodeId)` published by Gameplay at the
  reader's decode. Rover, Art, UI and World listen.
- `BlueprintDelivered` keeps its meaning, at the reader.
- `place.lab` (`place.lab.name` "The Crew's Lab"; `place.lab.job` "Where finds become know-how.") replaces M3-19's
  `place.cabin`.

**Docs to change once approved:**
- DESIGN "Why people come back": put technology growth first (07 and home visibly grow), then friends, a moon that
  opens up, home, and the mystery as seasoning. "Workshop" points to this tree. In the regions table, the crater's
  gate becomes Floodlights and the Peak's becomes Anchor Line + Long Leap + Atlas.
- ROADMAP version table: each version names its tech tier. Wide Sonar moves to 0.6.
- `game-flow.md` §4 sessions get re-cut on this spine: each session names its node, its find and its 07 change.
- M3-19: the lab stations replace the crew corners.

## 7. What stays from the story, as seasoning
- **Memories:** found in hearts beside the tech finds, placed on the lab's keepsake shelf. Each one is a small smile,
  never a requirement.
- **Logs:** clip beside the shelf. Letters play when masts light. Bell's jingle is still the opening's ghost.
- **The ending** is the biggest technology project in the game: rebuild the great dish with everything 07 has
  become, and broadcast. Earth's answer is the emotional payoff of a technological climb, not a reward for collecting
  memories.
