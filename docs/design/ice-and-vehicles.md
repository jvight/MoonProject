# The ice region and 07's vehicles (later phases)

> Author: the game design session, for the Director and the owner. Design only.
>
> The owner: (1) an ICE region 07 can only enter after upgrading its wheels; (2) vehicles 07 drives to travel far
> ("walking speed is too slow to reach other regions; like Subnautica's submarines, robots…").
>
> Consistent with:
> - `blueprint-fit.md`: standards A/B/C, no hands, reach 0.35–1.46 m, a robot-base job;
> - `craft-catalogue.md` (updated here: wheels tier, vehicles);
> - `0.6-world-regions.md`: regions open by adjacency; the Shadowed Crater sits south-west of the Long Mare;
> - story v4: **Ines's ice evidence is the point of the whole game**;
> - vacuum logic (no wings, no rotors);
> - `07-identity.md`;
> - ruling 15 (battery), ruling 17 (wear).

## 0. Player experience (skill `player-experience`)
| Question | Answer |
|---|---|
| **Hook** | The ice: a black bowl that the Sun has never touched in four billion years, glittering in 07's floods. *This is what the crew died to prove.* The carrier: 07 drives up a ramp into something bigger than itself, and the world gets wider. |
| **Pull** | The ice is visible (the crater's green glow and frost glitter from the mare's edge) but **07's wheels slip on its slopes**. The far regions are visible but hours away at 07's pace, and a carrier on the horizon promises the trip. |
| **Hold** | Vehicles grow visibly (bays, pods, tracks), the rail lines the player lays cross the map, and every hop pad is a place the player first reached on wheels. |
| **Finish** | The ice core cache (survey piece 5) is the hard proof that sends people back. The vehicles carry 07 to the Peak to transmit it. |
| **Feeling** | The ice: **hushed awe, careful.** The carrier: **the freedom of range, with a home that moves.** |

**Learn, then improve (Subnautica's vehicles):**
- The Seamoth, Cyclops and Prawn suit work because each opens depth and safety, and the Cyclops *is* a moving base.
- Their pains: micromanaging the Cyclops, vehicles getting lost or stuck, and the player leaving their own character
  behind.
- **Ours:** 07 stays the character. It **drives into** each vehicle and docks; the vehicle drives through 07's
  charge-port data link (no hands). The camera always shows 07 in its bay. 07 gets out for every real task.

## 1. The ice region: the Shadowed Crater's floor (0.8)
- **Where:** south-west of the Long Mare, 350 m across and 60 m deep (`0.6-world-regions.md` §3.3), opened by
  adjacency from the mare.
- **The real lunar conditions:**
  - a permanently shadowed region (no Sun ever, so no solar);
  - about −230 °C;
  - the floor is ice-cemented frost regolith, hard and slick;
  - the rim slopes reach 20–30° of frosted rock.

### The gate (a real need, opened by parts on 07's mounts)
| Need | Why (logic) | Part | Mount | Source |
|---|---|---|---|---|
| **Traction** (the gate) | normal tyres harden and their grease freezes: on frost-cemented slopes over 12°, 07 slides back (readable, never trapped: ruling 10's ≤ 20° way out) | **Cryo cleats:** cold-rated cleated wheels with heated hubs | wheels (a new tier, after the treads) | **elder 03**, the ice cart at rest on the crater rim: it was built for ice. Scanning its wheels teaches them (3 fragments) |
| Cold | cells lose capacity and wear rises below −150 °C | **Cell Heater** (already in the catalogue) | battery | elder 03 (same site) |
| Dark | no Sun, ever | **Floodlights** (already in the catalogue) | lamp | elder 02 |

- The cleats are the hard gate: without them the floor's slopes can't be climbed.
- The heater and floods are soft: without them it's possible but costly (cold drain ×1.25, wear on hard frost
  impacts, a smaller lit circle).

### Danger, readable and avoidable (ruling 17)
- Wear rises from hard impacts on frost-cemented ground above 6.8 m/s (wear's existing rule). Frost **creaks** first
  (wear §1.3).
- The cold drain is shown by the cell lamps frosting over.
- A forward **field charge pylon on the rim**, plus the heliostat (craft 6), makes the bowl workable.

### What's in it
- Meridian's dormant ice tappers (wake by contact);
- **the ice core cache** (survey piece 5);
- Theo's sled;
- Moss;
- frost-sublimation awe beats (north-star §4a).

## 2. Vehicles (07 drives in and docks; the vehicle drives through the charge port)
**Three vehicles,** each changing the pace and the map once:

| # | Vehicle | Std / source | Phase | What it is | Speed and range | Gate to build |
|---|---|---|---|---|---|---|
| V1 | **Service crawler** ("the crawler") | A. Lumen's mobile service crawler, found stalled at the mare's far shore (it hauled kit to the terraces); 4 fragments | **0.7** | a tracked platform about 8 × 4 m. A ramp drops; 07 drives into its **rover bay** and docks on a charge port. Inside: a charger for 07, **3 cargo pods**, a small fabricator slot, a 30-cell battery and its own solar deck. **A mobile base,** like the Cyclops, but simple. | 12 m/s on the flat (07: 8); ≤ 15° slopes; ~6 km per charge, and it refills from pylons and its deck | the vehicle pad (craft 30) + 6Al 4W 2O 1Ch, ⚡ built in 3 stages on the pad |
| V2 | **Rail runner** | A. The mass driver spine's maintenance car | **0.7** (with craft 22, the rail line) | a cart on the player's own rail lines. 07 drives onto it and docks; it runs between the player's stations. **Fast travel the player built**: it replaces the radio-hop idea. | 20 m/s on rails; anywhere the player has laid rail | the rail line + 3Al 2W, ⚡ 0.5 |
| V3 | **Ballistic hopper** | A + C. Lumen's survey lander frame + Meridian's guidance (needs the adapter) | **1.0** | a squat cold-gas/rocket frame with a rover cradle. 07 docks; it flies a **ballistic arc, no wings**, and lands only on **hop pads 07 has built** (a transponder target). Its propellant is **H₂/O₂ split from the crater's ice**, so the ice pays for the ending's reach. | up to ~2 km per hop; one hop per refuel at a pad with a propellant tank | hop pad structure (3M 2W 1O) + the hopper (4Al 2S 2Ch) + a propellant plant beside the ice still |

**How each changes the game:**
- **Before 0.7:** 07 drives everything (≤ 8 m/s, about 1 km per trip). The basin and the near mare.
- **V1, the crawler (0.7):** the map grows to several km; regions 2–3 km away take 3–4 minutes. The crawler parks at
  a region's edge as a **forward base** (charging, storage), and 07 drives out from it. Pace: longer expeditions
  with a moving home.
- **V2, rails (0.7+):** commuting between the player's own stations becomes quick, which removes the late-game
  backtracking chore (Subnautica's pain). Pace: return trips cost little, so exploring the frontier is the focus.
- **V3, the hopper (1.0):** point to point between pads the player built, and the last road to the Peak.
  **Exploration is kept,** because a pad only exists where 07 has driven first.

**Rules for all three:**
- **07 stays the character:**
  - the camera frames 07 in its bay;
  - its head tracks the route;
  - it gets out for every scan, cut and build.
- **The vehicle has no face and no voice**, only operational lamps and the annunciator's tones through 07.
- **Ruling 14:**
  - docking = driving onto a port;
  - controls = the charge port's data link;
  - cargo moves by the vehicle's own conveyor.
- **Ruling 7:**
  - a stuck vehicle self-recovers like 07;
  - a wrecked vehicle is towed home by the repair drone (craft helper 4).

### Visible progression
- **The crawler:** pods fill; its bay gets a second socket rack; later its deck grows a solar wing like 07's.
- **Rails:** they appear across the map as the player lays them, lit at night by the network pulse.
- **The hopper:** one pad per region; a flight arc visible from the base.

## 3. Counts and catalogue changes
Updated in `craft-catalogue.md`:
- **wheels tier +1:** Cryo cleats;
- **vehicles +3:** crawler, rail runner, hopper;
- **structures +2:** hop pad, propellant plant.
- **Totals: 75 → 78 makes** (34 structures · 12 kits · 23 tiers · 9 vehicles and helpers).

## 4. The biggest risks
1. **World size and streaming.** Vehicles at 12–20 m/s and ~2 km hops need terrain streaming well beyond 0.6's
   (`0.6-world-regions.md` §7 slice 1). Mitigation: cap speed by region, stream along rails and hop arcs early, and
   budget-test before 0.7.
2. **07 becomes a passenger.** Vehicles can push the character aside. Mitigation: the bay camera, 07 getting out for
   every task, no auto-pilot to objectives, and the vehicle only goes where the player drives it (or where rails go).
3. **Ice frustration.** Slipping can feel punishing. Mitigation: always a ≤ 20° way out, a readable creak and frost
   lamps, the never-stuck lift (ruling 7), and wear as a slow cost, never sudden.
4. **Scope.** Three vehicle controllers, cameras and docking flows is a lot. Mitigation: one shared "dock and drive"
   system (port, data link, bay camera), with the crawler first and the rails a cart on a spline. The hopper last, and
   cuttable to a scripted finale-only flight if needed.

## Tóm tắt (Tiếng Việt)
- **Vùng băng:** đáy Hố bóng tối (0.8), phía tây nam Biển Dài.
  - Mặt Trời không bao giờ chiếu tới, nên không có pin mặt trời.
  - Nhiệt độ khoảng −230 °C; đất trộn băng cứng và trơn.
- **Cổng vào, đúng điều kiện thật:**
  - **Bánh đinh chịu lạnh** (cổng cứng): lốp thường cứng lại, mỡ đông, trượt trên dốc băng quá 12°. Học từ bánh xe
    của đàn anh 03, chiếc xe chở băng đang nằm ở vành hố.
  - **Bộ sưởi pin** (cổng mềm): giảm hao pin vì lạnh.
  - **Đèn pha lớn** (cổng mềm): soi bóng tối.
  - Nguy hiểm qua hao mòn: đọc được (tiếng kẽo kẹt, đèn pin đóng băng) và tránh được. Luôn có lối thoát dốc ≤ 20°.
- **Ba phương tiện:** 07 lái vào, cắm cổng sạc và điều khiển qua cổng đó, không cần tay.
  - **Xe bánh xích dịch vụ** (0.7): căn cứ di động có sạc, 3 khoang hàng và pin lớn; 12 m/s; đỗ ở rìa vùng làm căn cứ
    tiền phương.
  - **Xe ray** (0.7): chạy trên đường ray người chơi tự rải giữa các trạm, 20 m/s; thay cho kiểu nhảy sóng radio.
  - **Tàu nhảy đạn đạo** (1.0): bay vòng cung, không cánh; chỉ hạ xuống bệ 07 đã dựng; nhiên liệu tách từ băng của
    hố, nên chính băng trả cho tầm bay cuối game.
- **Nhịp game thay đổi:** bản đồ từ khoảng 1 km mở ra nhiều km; về nhà không còn mệt; vẫn phải khám phá bằng bánh xe
  trước khi bay tới đâu.
- **Rủi ro lớn nhất:**
  - nạp thế giới ở tốc độ cao;
  - 07 thành hành khách;
  - trượt băng gây bực;
  - khối lượng việc.
  - Mỗi rủi ro có cách giảm trong tài liệu.
