# Lunarion — Vacuum Shapes & Silhouette Law (`EN + VI`)

> **Status:** Permanent Art, Worldbuilding & 3D Modeling Rulebook (`RULES.md:71-75`, Owner Ruling `2026-10-17`).
> **Applies to:** Every concept illustration (`Logs/concepts/*.png`), design document (`docs/design/*.md`), creature/swarm spec (`living-moon-v2*.md`), invention/kit (`inventions*.md`), and headless-Blender R1 3D model (`tools/art/blender/*.py`).
> **Owner's Core Feedback (2026-10-17):** *"Hình dáng cái khinh khí cầu vẫn trái đất quá"* — *"The balloon shape is still too Earth-like. The Moon has NO air. Anything shaped like a balloon, a parachute, a kite, an air sail, a lifting wing, a propeller or a bird/insect wing reads as Earth physics and breaks the fiction."*

---

## PART I — ENGLISH SPECIFICATION (`EN`)

### 1. The Iron Law of Lunar Silhouettes
The Moon sits in a hard vacuum ($10^{-12}\text{ torr}$) under $1/6\,g$ gravity ($1.62\text{ m/s}^2$). There is **no ambient air** to inflate a canopy, resist a falling parachute, lift an airplane wing, push a windmill blade, or push against a flapping insect wing or aerodynamic tail fin.

Even if a text paragraph attempts to excuse an Earth-air silhouette with a clever caption (e.g., *"this parachute catches rising steam"*, *"these bee wings are actually folded thermal radiators"*, or *"this windmill is turned by electrostatic dust"*), **the player reads the silhouette first (`docs/VISION.md:28-30`, Ruling 11 & 13).** If a silhouette looks like it belongs in Earth's atmosphere, it breaks the lunar fiction at a glance and fails review.

---

### 2. Banned Earth-Air Shapes & Vocabulary (`DO NOT`)

Never use any of the following shapes in concepts or 3D meshes, and never use their aerodynamic terms in design docs or UI strings:

| Category | Banned Silhouette (`DO NOT MODEL OR DRAW`) | Banned Wording (`DO NOT WRITE`) | Why It Fails on the Moon |
|---|---|---|---|
| **1. Inflatable / Cloth Canopies** | Hot-air balloons, parafoils, parachutes, fabric kites, flexible umbrella bells, billowing cloth bags | *balloon, parachute, parafoil, kite, air canopy, parasol, umbrella descent* | Flexible canopies require ambient atmospheric pressure and drag to stay inflated and slow a descent. |
| **2. Lifting Wings & Airfoils** | Bird wings, insect wings (veined/translucent bee or moth wings), swept airplane gliders, delta-wing manta rays | *lifting wing, wingspan, flapping, gliding on air, airfoil, banking turn, slipstream, updraft* | Cambered or swept wings generate zero aerodynamic lift in vacuum. |
| **3. Rotors, Propellers & Windmills** | Helicopter rotors, quadcopter props, Dutch windmill blades on a horizontal shaft, pinwheels, air turbines | *propeller, rotor, windmill, wind sail, dust-sail, fan blade* | No wind exists to turn angled blades; exposed rotary shaft bearings seize in abrasive electrostatic regolith (`inventions.md` §2.4). |
| **4. Aerodynamic Fins & Fuselages** | Streamlined teardrop/torpedo airframes, cruciform rocket-fish tail fins, rudders, ailerons | *aerodynamic hull, tail fins, stabilizing fins, rudder* | Weathercocking tail fins require an airstream to produce restoring torque; in vacuum they are dead weight. |
| **5. Open Atmospheric Venting** | Open steam chimneys on "closed-loop" pipes, bellows sucking "outside air" | *breathing lunar air, open exhaust stack* | Venting working gas from a base loop wastes precious $\text{O}_2$; vacuum reactors must use sealed internal accumulators. |

---

### 3. Approved Vacuum-Logical Mechanisms & Silhouettes (`DO`)

Every machine, kit, structure, and autonomous creature on the Moon must work—and **look at a glance like it works**—by real 1970s–1980s vacuum physics:

| Need / Function | Vacuum-Logical Mechanism (`DO`) | Honest Silhouette That Reads at a Glance |
|---|---|---|
| **Catching / Condensing Gas (`H2O`, `O2`)** | **Cryogenic Cold Traps (`Cryopumps`):** In vacuum, expanding vapour molecules travel ballistically in straight lines and freeze solid the instant they strike a sub-cooled metal surface (`-180°C` to `-200°C`) cooled by a shadow radiator. | **The Frost Harp (`inventions.md` §2.2):** A rigid fold-out fan/lyre of bare copper condenser fins and brass cryo-rods on a boom. White frost **visibly blooms** across the warm amber copper; heating the rods melts the ice into a sealed flask. |
| **Riding a Sublimation Jet** | **Rigid Ballistic Impingement Plates + Cold-Gas Retro-Puffs:** Supersonic vapour molecules rising from sublimating ice transfer upward momentum to flat horizontal collector wafers (like a ping-pong ball on a water jet). Descent is a $1/6\,g$ ballistic fall cushioned by thruster puffs. | **Plume Drifters v2 (`living-moon-v2.md` §3.5):** Rigid hexagonal snow-crystal / Chladni-wafer frames of white ceramic, teal metal, and copper frost-louvers around an amber core, with 3 bottom brass cold-gas nozzles firing crisp white puffs. |
| **Short & Medium Flight (`0.5–400 m`)** | **Ballistic $1/6\,g$ Hops & De Laval Cold-Gas RCS Thrusters:** Pressurized **Breather $\text{O}_2$** (`22 bar`) vented through flared conical brass nozzles produces crisp reaction thrust and white Joule-Thomson condensation mist along a parabolic arc (`p(t) = p0 + v0*t + 0.5*g*t²`). | **Cold-Gas Puff Thrusters (`inventions.md` §2.5)**, **Wingless Glassweaver Kiln-Hoppers (`living-moon-v2.md` §3.6)**, **Spherical RCS Puff Swifts (`living-moon-v2.md` §3.3)**, and **Radial-Fin Lantern Buoys (`living-moon-v2.md` §3.2)**. |
| **Harvesting Terminator Dust** | **Static High-Voltage Electrostatic Louver Traps:** Alternating `+1.5 kV / -1.5 kV` parallel copper louver plates on ceramic stand-off insulators attract levitated terminator dust via Coulomb force; periodic relay de-energizing drops the mineral grains into a funnel. | **Terminator Electrostatic Louver-Comb (`inventions.md` §2.10):** A static architectural tower of parallel copper louver slats, white ceramic insulators, and magnetic collector bars—zero rotating windmill blades. |
| **Surfing the Terminator Field** | **Charged Corona-Ring Hoops (`±1.8 kV`):** A symmetric circular or hexagonal wire-mesh torus maximizes electrostatic repulsion against the $1000\text{ V/m}$ terminator dust layer without pretending to be an aerodynamic wing. | **Dust Larks v2 (`living-moon-v2.md` §3.1):** Concentric copper-and-gold wire corona rings suspended around a small cream capacitor seed body with micro attitude puffs—zero bird wings. |
| **Skimming a Magnetic Swirl** | **Superconducting Diamagnetic / Lorentz Coil Pads:** Current loops inside flat hexagonal superconducting platters repulse against the $300\text{ nT}$ horizontal crustal magnetic field of a Lunar Swirl. | **Swirl Rays v2 (`living-moon-v2.md` §3.7):** Flat hexagonal Meridian coil sleds with visible wound-copper induction rings and 4 glowing cyan belly pads—zero manta-ray wings. |
| **Rejecting Heat in Vacuum** | **Rigid Radiative Fin Crowns:** Without air convection, heat leaves only by infrared radiation ($P = \varepsilon \sigma A T^4$) or melting a thermal mass (`Molten-Salt Core`, `inventions.md` §2.8). | **Rigid dark-steel or copper radial radiator cages** (concentric rings or vertical cylinder crowns) that never flap or spread like insect wings. |

---

### 4. Complete Audit of Offenders & Ranked Vacuum Redesigns (`2026-10-17`)

Per `RULES.md` §"Facts and creativity", we evaluated 2–3 real vacuum-logical variants for each major offender before locking the winning design across `docs/design/inventions*.md` and `docs/design/living-moon-v2*.md`:

#### 4.1 Offender #1: The Ember Shell's "Vapor Kite" (`inventions.md` §2.2)
- **What Failed:** A golden-and-cream ribbed parafoil balloon/parachute canopy on a pole above 07 (`before_ember_shell.png`), plus a cloth bag on 07's rear (`before_plume_drifters_crater.png`). Reads immediately as an Earth hot-air balloon or parafoil kite.
- **Ranked Variants (`[PROPOSAL]`):**
  1. **Rank #1 (SELECTED) — The Frost Harp (*Đàn Hạc Sương Giá*):** A fold-out semicircular lyre/fan of rigid chilled copper condenser fins and brass cold-trap rods on an articulated boom, sub-cooled by a dark radiator panel in shadow. When 07 raises the harp into the expanding vapour plume, **sparkling white frost visibly blooms across the amber copper fins**. 07 then pulses warm current through the rods so meltwater drains down a heated copper tube into a sealed `Water Ice` cylinder flask (`Logs/concepts/ember_shell_frost_harp.png`).
     - *Why #1:* 100% real vacuum cold-trap physics (`cryopump`), zero cloth/canopy, high readability from `30 m` (amber copper turning snow-white), and natural D-major pentatonic thermal-tick audio.
  2. **Rank #2 — Brass Cone Hood on a Boom (*Chụp Phễu Đồng Thau Họng Lạnh*):** A rigid inverted brass funnel hood with an internal frost-lined throat held over the geyser.
     - *Why #2:* Physically valid, but an inverted cone on a mast can still squint-read as a rigid umbrella/lampshade, and the frost forms inside the hidden throat instead of blooming visibly on open copper fins.
  3. **Rank #3 — Ground Crawler Cryo-Rake:** 07 drives over the blast site afterward to scrape re-frosted ice.
     - *Why #3:* Loses the vertical spectacle of harvesting the towering plume fountain.

#### 4.2 Offender #2: The Plume Drifters (`living-moon-v2.md` §3.5)
- **What Failed:** Jellyfish-shaped drones with six leafy translucent teal parachute petals (`before_plume_drifters_crater.png`), described as making a *"Slow Parachute Descent"*.
- **Ranked Variants (`[PROPOSAL]`):**
  1. **Rank #1 (SELECTED) — Rigid Hexagonal Frost-Wafer Skimmers (*Tinh Thể Đón Hơi Lục Giác*, `Plume Drifters v2`):** Flat, rigid geometric snow-crystal / Chladni-plate frames of white ceramic, muted teal metal, and horizontal copper frost-louvers around a warm amber condenser core, equipped with three bottom cold-gas reaction nozzles (`Logs/concepts/plume_drifters_v2.png`). They ride the upward momentum of the ballistic vapour fountain like geometric wafers on a water jet while frost crystals grow across their copper slats, then descend on a $1/6\,g$ ballistic arc cushioned by crisp cold-gas retro-puffs.
     - *Why #1:* Zero canopy or jellyfish membrane; looks like an authentic 1980s Tycho/Meridian hexagonal cryo-instrument and a giant mechanical snowflake.
  2. **Rank #2 — Rigid Fan-Coral Cold-Trap Probes (*San Hô Quạt Bẫy Lạnh*):** Vertical branching copper-fin probes that hop through the plume on cold-gas thrusters.
     - *Why #2:* Visually overlaps with 07's own Frost Harp.
  3. **Rank #3 — Cut the Species:** Let 07's Frost Harp harvest the plume solo.
     - *Why #3:* Loses the magical 0.8 swarm synergy beat in the Shadowed Crater.

#### 4.3 Offender #3 (Worst Other Offender): Glassweaver Bees (`living-moon-v2.md` §3.6)
- **What Failed:** In `before_glassweaver_bees_swarm.png`, every robot had **two translucent, veined honeybee wings** raised in a `V` on its back while flying through vacuum.
- **Ranked Variants (`[PROPOSAL]`):**
  1. **Rank #1 (SELECTED) — Wingless Ballistic Kiln-Hoppers (`Glassweaver Bees v2`):** Strip 100% of wings and fins. Each `0.45 m` robot is a compact, wingless 6-legged spring-and-cold-gas lunar hopper with a glowing spherical amber rotary-kiln abdomen enclosed inside a **rigid cage of concentric brass radiator rings** and twin rear cold-gas $\text{O}_2$ nozzles (`Logs/concepts/glassweaver_bees_v2.png`). They keep the affectionate crew nickname *"Glassweaver Bees"* solely because they forage `OrangeBeads` in radial loops around a hexagonal **Honeycomb Hive Kiln**.
  2. **Rank #2 — Wheeled Micro-Rovers:** Tiny 4-wheeled carts rolling up ramps into the Hive Kiln.
     - *Why #2:* Loses the joyful $1/6\,g$ golden parabolic arcs across the Orange-Glass Crater.

#### 4.4 All Other Offenders Audited & Fixed in Docs & Cards

| # | Offender Concept | What Was Too Earth-Air | Vacuum-Logical Fix Applied in `inventions*.md` & `living-moon-v2*.md` |
|---|---|---|---|
| **4** | **Terminator Electrostatic Dust-Sail ("Lunar Windmill")** (`before_dust_sail.png`, `inventions.md` §2.10) | 4-bladed Dutch windmill rotor on a horizontal shaft; `"Dust-Sail"` & `"Windmill"` naming. | Renamed & reshaped to **The Terminator Electrostatic Louver-Comb (*Tháp Lược Tĩnh Điện Đường Ranh Sáng*)** (`Logs/concepts/dust_harvester_v2.png`): a **static, non-rotating vertical array** of parallel high-voltage copper louver plates (`±1.5 kV`) and magnetic bars on white ceramic insulators. |
| **5** | **Puff Swifts** (`before_puff_swifts_flight.png`, `living-moon-v2.md` §3.3) | Aerodynamic torpedo fuselage with **4 swept rocket-fish tail fins**; loose glass ball in bare crater. | Reshaped to **Finless Spherical Apollo-RCS Courier Pods** (`Logs/concepts/puff_swifts_v2.png`): compact cream-enamel spherical pressure bottles with **4 orthogonal brass De Laval cold-gas RCS thrusters**, active retro-braking into Mast Catcher-Nets, and zero tail fins. |
| **6** | **Dust Larks** (`before_dust_larks_dawn.png`, `living-moon-v2.md` §3.1) | Swept-back bird/airplane glider wings (`0.65 m wingspan`). | Reshaped to **Electrostatic Corona-Ring Skimmers**: concentric circular/hexagonal copper-and-gold wire corona hoops (`Ø 0.55 m`) around a cream capacitor pod with micro cold-gas attitude puffs—zero airfoil wings. |
| **7** | **Lantern Moths** (`before_lantern_moths_night.png`, `living-moon-v2.md` §3.2) | Spread horizontal pleated moth wings (`4 wings`) and curved insect antennae. | Reshaped to **Cylindrical Radial-Fin Lantern Buoys**: a vertical crown of rigid copper radiator fins (like a vintage vacuum-tube heat cage) over a glowing amber lantern globe, hovering on a bottom cold-gas $\text{O}_2$ nozzle—zero horizontal wings. |
| **8** | **Swirl Rays** (`before_lunar_swirl_field.png`, `living-moon-v2.md` §3.7) | Swept delta-wing manta-ray airfoils + `"slipstream"` wording. | Reshaped to **Hexagonal Diamagnetic Lorentz Sleds**: flat hexagonal Meridian superconducting coil platters levitating `0.4 m` on 4 cyan diamagnetic pads, giving 07 a **`+15%` magnetic induction guide-field assist** (never an air "slipstream"). |
| **9** | **Ilmenite Breather, Pneumatic Post & Strider Roost** (`inventions.md` §2.1, §2.4; `living-moon-v2.md` §3.4) | Bellows metaphor ("breathing air"), chimney steam puff on closed pipe, bird silhouettes on Strider. | Clarified Breather top bellows as a **sealed internal gas-volume compensator piston**; specified Pneumatic Post as a **100% hermetically sealed closed-loop pipe** with zero outside exhaust; replaced bird silhouettes on Strider roost with **ring-skimmers**. |

---

### 5. The 6-Point Vacuum Shape Gate (Mandatory Pre-Commit Checklist)

Before committing any new concept image, design doc, or Blender model script, verify all 6 checks:
1. **No Canopies:** Zero balloons, parachutes, parafoils, kites, umbrellas, or billowing cloth bags.
2. **No Wings:** Zero bird wings, veined/spread insect wings, swept airplane gliders, or manta-ray airfoils.
3. **No Rotors or Windmills:** Zero propellers, helicopter blades, or rotating windmill sails.
4. **No Aerodynamic Fins:** Zero airplane tail fins, rudders, or streamlined atmospheric fuselages on flying/hopping bots.
5. **Honest Vacuum Actuators Visible:** Every moving/flying thing shows its real vacuum hardware—flared brass De Laval cold-gas nozzles, coil-spring legs, electrostatic wire hoops/louvers, superconducting coil platters, or rigid copper cold-trap fins.
6. **Vocabulary Clean:** Grep the doc/code for `parachute`, `parafoil`, `kite`, `balloon`, `windmill`, `propeller`, `flapping`, `wingspan`, `airfoil`, `tail fin`, `slipstream`, `updraft`—all must be `0` (except inside negative rules like this file).

---
---

## PHẦN II — QUY TẮC HÌNH DÁNG CHÂN KHÔNG BẰNG TIẾNG VIỆT (`VI`)

> **Trạng thái:** Quy tắc Thiết kế, Mỹ thuật & Dựng hình 3D cố định (`RULES.md:71-75`, Chỉ đạo của Owner `2026-10-17`).
> **Ý kiến chỉ đạo của Owner (2026-10-17):** *"Hình dáng cái khinh khí cầu vẫn trái đất quá"* — Mặt Trăng **KHÔNG CÓ không khí**. Bất cứ thứ gì có hình dáng giống khinh khí cầu, dù lượn, cánh diều, buồm đón gió, cánh nâng khí động học, cánh quạt hay cánh chim / cánh côn trùng đều mang cảm giác vật lý Trái Đất và làm gãy hoàn toàn thế giới giả tưởng trên Mặt Trăng.

### 1. Định Luật Bất Biến Về Hình Bóng Chân Không (Silhouette Law)
Mặt Trăng là môi trường chân không cứng ($10^{-12}\text{ torr}$) với trọng lực bằng $1/6$ Trái Đất ($1.62\text{ m/s}^2$). Không hề có không khí xung quanh để thổi phồng vòm dù, tạo lực cản hãm rơi, nâng cánh máy bay, quay cánh cối xay gió hay tạo lực đẩy cho cánh ong, cánh bướm và vây đuôi tên lửa.

Ngay cả khi phần mô tả chữ cố giải thích bằng lý lẽ khoa học (ví dụ: *"chiếc dù này chỉ hứng hơi nước bốc lên"*, *"cánh ong này thực chất là tấm tản nhiệt"*, hay *"cối xay này quay nhờ bụi tĩnh điện"*), **người chơi luôn đọc hình bóng (silhouette) trước tiên (`docs/VISION.md:28-30`, Ruling 11 & 13).** Nếu hình bóng nhìn thoáng qua giống một vật thể bay trong khí quyển Trái Đất, thiết kế đó lập tức bị loại.

---

### 2. Các Hình Dáng & Từ Ngữ Khí Quyển Bị Cấm Tuyệt Đối (`KHÔNG ĐƯỢC LÀM`)

| Nhóm | Hình bóng bị cấm (`TUYỆT ĐỐI KHÔNG VẼ / DỰNG 3D`) | Từ ngữ bị cấm (`KHÔNG VIẾT`) | Lý do sai lệch trên Mặt Trăng |
|---|---|---|---|
| **1. Vòm vải / Khinh khí cầu / Dù / Diều** | Khinh khí cầu, dù lượn parafoil, dù nhảy, diều vải, tán sứa/ô dù mềm, túi vải phồng gió | *khinh khí cầu, dù, diều, cánh buồm gió, tán dù hạ cánh* | Vòm vải mềm cần áp suất khí quyển và sức cản không khí để căng phồng và hãm rơi. |
| **2. Cánh nâng khí động / Cánh chim & côn trùng** | Cánh chim vẫy, cánh ong trong suốt có gân, cánh bướm xòe ngang, tàu lượn cánh xuôi, cá đuối cánh tam giác | *cánh nâng, sải cánh, vỗ cánh, lướt gió, luồng khí trượt (slipstream), cột khí nóng (updraft)* | Cánh cong khí động học không tạo ra bất kỳ lực nâng nào trong chân không. |
| **3. Cánh quạt / Trực thăng / Cối xay gió** | Cánh quạt trực thăng, drone 4 cánh quạt, cối xay gió 4 cánh quay quanh trục ngang, chong chóng | *cánh quạt, trực thăng, cối xay gió, cối xay bụi, buồm gió* | Không có gió để quay cánh quạt; ổ trục quay lộ thiên sẽ bị bụi tĩnh điện sắc nhọn làm kẹt cứng (`inventions-vi.md` §2.4). |
| **4. Vây đuôi khí động & Thân ngư lôi** | Thân thuôn khí động học kiểu máy bay/ngư lôi, 4 vây đuôi chữ thập kiểu tên lửa/cá chuồn, bánh lái gió | *vây đuôi ổn định khí động, thân khí động học, bánh lái gió* | Vây đuôi cần luồng không khí thổi qua mới tạo được mô-men giữ hướng; trong chân không chúng hoàn toàn vô dụng. |
| **5. Ống xả hở trên mạch kín** | Ống khói xả hơi trắng lên trời trên đường ống tuần hoàn kín | *hít thở không khí Mặt Trăng, ống khói xả hở* | Xả khí $\text{O}_2$ ra chân không trên mạch vận chuyển kín là sự lãng phí phi lý. |

---

### 3. Các Cơ Chế & Hình Dáng Logic Chân Không Được Chuẩn Thuận (`NÊN LÀM`)

| Nhu cầu / Chức năng | Cơ chế vật lý chân không (`NÊN LÀM`) | Hình bóng cơ khí chân thực đọc được ngay lập tức |
|---|---|---|
| **Hứng & Ngưng tụ hơi nước (`H2O`, `O2`)** | **Bẫy Lạnh Chân Không (`Cryogenic Cold Trap / Cryopump`):** Trong chân không, phân tử hơi nước bay thẳng theo đường đạn đạo và lập tức kết tinh thành băng trắng xóa ngay khi chạm vào bề mặt kim loại siêu lạnh (`-190°C`, làm mát bằng tấm bức xạ đặt trong bóng tối). | **Đàn Hạc Sương Giá (`The Frost Harp`, `inventions-vi.md` §2.2):** Khung quạt/đàn hạc cứng bằng các thanh đồng đỏ và lá tản nhiệt đồng thau gắn trên cần vươn. **Sương băng trắng xóa nở rộ** rõ mồn một trên các thanh đồng cam ấm; sau đó dòng điện sưởi nhẹ làm băng chảy vào bình chứa. |
| **Bay lên trong cột hơi thăng hoa** | **Khung Tinh Thể Hứng Động Lượng Đạn Đạo + Phụt Khí Lạnh Hãm Rơi:** Các phân tử hơi nước phóng lên từ băng thăng hoa đập vào các tấm lá đồng nằm ngang (như quả bóng bàn nẩy trên vòi phun nước). Khi rơi xuống theo trọng lực $1/6\,g$, chúng phanh bằng vòi phụt khí lạnh. | **Tinh Thể Đón Hơi (`Plume Drifters v2`, `living-moon-v2-vi.md` §3.5):** Khung lục giác cứng hình bông tuyết cơ khí bằng gốm trắng, kim loại xanh mòng két và các nan chớp đồng siêu lạnh phủ đầy băng tinh thể, dưới đáy có 3 vòi phụt khí lạnh nhả khói trắng. |
| **Di chuyển trên không (`0.5–400 m`)** | **Nhảy Đạn Đạo $1/6\,g$ & Vòi Phun Khí Lạnh De Laval (`RCS`):** Xả khí $\text{O}_2$ nén từ Lò Thở qua vòi phun đồng thau hình nón loe tạo lực đẩy phản lực và chùm sương ngưng tụ Joule-Thomson màu trắng theo đường parabol. | **Bộ Đẩy Khí Lạnh O2 (`inventions-vi.md` §2.5)**, **Ong Thợ Lò Nung Không Cánh (`Glassweaver Bees v2`)**, **Trạm Chuyển Phát Cầu RCS (`Puff Swifts v2`)**, và **Phao Đèn Tản Nhiệt Trụ (`Lantern Moths v2`)**. |
| **Thu hoạch bụi tĩnh điện đường bình minh** | **Tháp Lược Chớp Tĩnh Điện Tĩnh (`Static Electrostatic Louver Trap`):** Các tấm nan chớp đồng cố định phân cực xen kẽ `+1.5 kV / -1.5 kV` đặt trên sứ cách điện cao áp hút hạt bụi tích điện; mỗi 30 giây ngắt áp để hạt quặng rơi xuống phễu. | **Tháp Lược Tĩnh Điện Đường Ranh Sáng (`inventions-vi.md` §2.10):** Khung tháp tĩnh gồm các tầng nan chớp lưới đồng song song và thanh nam châm trên đỉnh cột sứ trắng—tuyệt đối không có cánh quạt cối xay gió. |
| **Lướt trên màn bụi tĩnh điện** | **Vòng Hào Quang Tĩnh Điện (`Corona-Ring Hoops ±1.8 kV`):** Khung vòng tròn/lục giác đồng tâm bằng lưới dây đồng–vàng tạo lực đẩy tĩnh điện Coulomb cực đại trên lớp bụi $1000\text{ V/m}$ mà không mang hình dáng cánh chim. | **Sơn Ca Bụi v2 (`living-moon-v2-vi.md` §3.1):** Hai vòng tròn/lục giác dây đồng mạ vàng đồng tâm bao quanh hạt nhân tụ điện sơn kem và vòi vi khí lạnh—không có cánh chim hay cánh máy bay. |
| **Trượt trên Cánh Đồng Xoáy Từ** | **Đĩa Siêu Dẫn Nghịch Từ (`Diamagnetic Lorentz Sleds`):** Dòng điện siêu dẫn trong đĩa lục giác phẳng tạo lực đẩy từ trường nâng máy lơ lửng `0.4 m` trên dị thường từ trường $300\text{ nT}$. | **Cá Đuối Từ Trường v2 (`living-moon-v2-vi.md` §3.7):** Đĩa trượt lục giác phẳng mang kiến trúc Meridian với vòng cuộn cảm đồng lộ thiên và 4 đệm bụng phát sáng xanh lơ—không có cánh cá đuối manta. |
| **Tản nhiệt trong chân không** | **Vành Lá Tản Nhiệt Bức Xạ Cứng (`Radiative Fin Crowns`):** Không có đối lưu không khí, nhiệt chỉ thoát qua bức xạ hồng ngoại ($P = \varepsilon \sigma A T^4$) hoặc truyền vào khối muối nóng chảy. | **Lồng vòng đồng/thép đen đồng tâm hoặc vương miện lá tản nhiệt trụ đứng cứng cáp**, không bao giờ xòe ngang hay đập cánh như côn trùng. |

---

### 4. Bảng Kiểm Tra 6 Điểm Bắt Buộc Trước Khi Commit (`Vacuum Shape Gate`)
1. **Không Vòm Dù / Khinh Khí Cầu:** Không có bất kỳ chi tiết nào giống khinh khí cầu, dù nhảy, diều vải, ô dù hay túi vải phồng.
2. **Không Cánh Bay Khí Quyển:** Không có cánh chim, cánh ong có gân, cánh bướm xòe ngang, cánh máy bay hay cánh cá đuối.
3. **Không Cánh Quạt / Cối Xay Gió:** Không có cánh quạt trực thăng, chân vịt hay cánh cối xay gió quay quanh trục.
4. **Không Vây Đuôi Khí Động:** Không có vây đuôi chữ thập kiểu tên lửa/cá chuồn hay thân ngư lôi khí động học.
5. **Thấy Rõ Cơ Cấu Chân Không Thực Chất:** Mọi thiết bị bay/nhảy/thu hoạch đều phô diễn rõ phần cứng chân không thập niên 1980: vòi phun đồng thau De Laval, chân lò xo, vòng dây tĩnh điện, đĩa cuộn cảm siêu dẫn, hoặc dàn thanh đồng bẫy lạnh phủ sương giá.
6. **Thuật Ngữ Trong Sạch:** Tuyệt đối không dùng các từ *khinh khí cầu, dù lượn, cánh diều, cối xay gió, vỗ cánh, sải cánh, vây đuôi khí động, luồng khí trượt* trong tài liệu thiết kế và thẻ mô tả.
