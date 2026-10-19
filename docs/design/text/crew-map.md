# Text: Ines's Names and Notes on the Crew's Map & Basin Crates (M3-07 · Story v4)

> **Status:** Locked for Story v4 (`1969` landing, `1971` crewed sortie, `1979` Atlas rig, Lumen built `1984` and crewed from `1985` to `1988`, `1987` uncrewed cargo drop, Kestrel-3 down in October `1988`).
> **The 1987 drop (Director ruling, 2026-10-19):** the crew's cargo lander landed in the 1987 drop; 07 unloaded its last cargo in October 1988. Its sister, the Canyon Supply Lander, came down hard in the canyon in the same drop (`site.lander`).
> **Kestrel-3 (Director ruling, 2026-10-19):** Meridian's relay satellite, down in October 1988 (story v4 §2.2), not a 1971 probe.
> **Target files for UI port:** `Assets/_Project/Data/Localization/en.json` and `Assets/_Project/Data/Localization/vi.json`.
> **Writing rules (`docs/design/writing-style.md`):**
> - Names (`map.<placeId>.name`) are written in Ines's hand when the chart pen inks a place.
> - Notes (`map.<placeId>.note`) are her pencil margin notes: one short line each, dry and precise, secretly fond.
> - Ines writes `"tôi"`. She calls Tilly `"con bé"`, and Ro `"cô ấy"`.
> - **One name per place.** Where the tables already name a place (`site.<id>.name`, `hop.node.relay.<n>`), the map uses that exact name in both languages.

---

## 1. Key Summary (`Assets/_Project/Data/Localization/en.json`)

Out of **43 `map.*` keys** (`en.json:1274–1468`) and **5 `crate.*.note` keys** (`en.json:1774–1792`):
- **39 `map.*` keys are `KEEP`** (no changes needed).
- **4 `map.*.note` keys are `REPLACE`** to lock the 1969–1988 historical timeline (`Kestrel-3` as Meridian's relay satellite, down in October 1988, `Site_SupplyLander` as the cargo lander's sister in the 1987 drop, `Rim Terraces` as the 1979 Atlas rig, and `The Peak` as the final stop before transmitting the basin survey).
- **1 `crate.*.note` key is `KEEP`** (`crate.garage.note`) and **4 `crate.*.note` keys are `REPLACE`**.

---

## 2. Map Notes Replaced in Story v4 (`4` Keys)

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt | Why Changed |
|---|---|---|---|---|---|
| `map.site.kestrel.note` (`en.json:1322`) | **REPLACE** | `Poor thing. It came down still pointing at Earth.` | `Meridian's relay satellite. Came down in October, still pointing at the Arks.` | `Vệ tinh tiếp sóng của Meridian. Rơi xuống hồi tháng Mười, vẫn chĩa về phía các tàu Ark.` | Grounds `Kestrel-3` (`Site_KestrelCrash`) as Meridian's relay satellite, down in October 1988 (story v4 §2.2). |
| `map.site.lander.note` (`en.json:1354`) | **REPLACE** | `Brought Theo's seed trays. Landed… firmly.` | `The '87 resupply drop with Theo's seed trays. Landed… firmly.` | `Chuyến tiếp tế năm 87 chở khay hạt giống của Theo. Hạ cánh hơi… mạnh tay.` | Grounds `Site_SupplyLander` as the 1987 uncrewed resupply drop before the Long Dim grounded further launches. |
| `map.region.peak.note` (`en.json:1450`) | **REPLACE** | `The dish. Kenji's job, after the storm.` | `The high-gain dish. Last stop before we send the survey up.` | `Chảo ăng-ten cao tần. Điểm cuối trước khi gửi bản khảo sát lên.` | Replaces v3 post-storm routine maintenance note with the November 1988 push to align the dish and send the basin survey to Meridian. |
| `map.region.terraces.note` (`en.json:1458`) | **REPLACE** | `Too steep for wheels. Atlas parked up here?` | `Too steep for four wheels. The '79 Atlas rig is still parked up here.` | `Quá dốc cho xe bốn bánh. Cỗ xe Atlas năm 79 vẫn đỗ trên này.` | Grounds the heavy Atlas rig in the 1979 basin construction era and hints at the six-wheel upgrade (`log.kenji_2`). |

---

## 3. Supply Crate Notes in the Basin (`5` Keys, `en.json:1774–1790`)

*(Cross-referenced with `docs/design/text/crew-logs.md` §6.)*

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|
| `crate.depot.note` (`en.json:1774`) | **REPLACE** | `Spares for the next rotation. Don't let Ines take the good wire. — Kenji` | `Spares for the ridge line. Don't let Ines take the good copper wire for her core tags. — Kenji` | `Đồ dự phòng cho đường dây sống núi. Đừng để Ines lấy cuộn dây đồng xịn đi buộc nhãn mẫu đá. — Kenji` |
| `crate.kestrel.note` (`en.json:1778`) | **REPLACE** | `Lenses off the old relay. Handle them like records. — Ro` | `Lenses off Meridian's relay satellite. Handle them like records. — Ro` | `Thấu kính tháo từ vệ tinh tiếp sóng của Meridian. Cầm nhẹ tay như cầm đĩa than nhé. — Ro` |
| `crate.drill.note` (`en.json:1782`) | **REPLACE** | `Wire for the drill's next season. Labelled, counted, twice. — I. Kalani` | `Spare cable for the deep-core ice survey. Labelled, coiled, counted twice. — I. Kalani` | `Cáp dự phòng cho đợt khoan thăm dò băng tầng sâu. Đã dán nhãn, cuộn gọn, đếm hai lần. — I. Kalani` |
| `crate.garage.note` (`en.json:1786`) | **KEEP** | `For 07, next time it breaks. It will. — Kenji` | *(Keep current)* `For 07, next time it breaks. It will. — Kenji` | *(Keep current)* `Cho 07, lần tới nó hỏng. Thế nào cũng hỏng. — Kenji` |
| `crate.lander.note` (`en.json:1790`) | **REPLACE** | `Seed trays went up top. This is everything else. — Theo` | `Seed trays from the '87 supply drop went up to the dome. Left the spare fittings boxed here. — Theo` | `Khay hạt giống từ chuyến hàng thả xuống năm 87 đã mang lên mái vòm. Phụ tùng dư đóng thùng để lại đây. — Theo` |

---

## 4. Complete Table of `map.*` Keys (`en.json:1274–1468`)

### The Sheet & Card State (`en.json:1274–1298`) — All `KEEP`

| Key | `en.json` Line | Status | English | Tiếng Việt |
|---|---|---|---|---|
| `map.sheet.title` | `en.json:1274` | **KEEP** | `Lumen basin, surveyed on foot. — I. Kalani` | `Lòng chảo Lumen, khảo sát bằng đôi chân. — I. Kalani` |
| `map.tally.label` | `en.json:1278` | **KEEP** | `Checked:` | `Đã kiểm tra:` |
| `map.complete.note` | `en.json:1282` | **KEEP** | `Checked: the Moon is still there. — I.` | `Đã kiểm tra: Mặt Trăng vẫn còn đó. — I.` |
| `map.state.heart_waiting` | `en.json:1286` | **KEEP** | `Something is still inside.` | `Bên trong vẫn còn đồ.` |
| `map.state.picked_clean` | `en.json:1290` | **KEEP** | `Picked clean.` | `Đã dọn sạch.` |
| `map.state.mast_dark` | `en.json:1294` | **KEEP** | `Offline. Needs repair.` | `Đang tắt. Cần sửa.` |
| `map.state.mast_lit` | `en.json:1298` | **KEEP** | `Online. Inside the station's reach.` | `Đang hoạt động. Trong tầm phủ sóng.` |

### Basin & Outer Region Places (`en.json:1302–1468`) — `32 KEEP`, `4 REPLACE`

| Place ID | Name Key (`en.json` Line) | Note Key (`en.json` Line) | Status | Name (EN / VI) | v4 Note (EN) | v4 Note (VI) |
|---|---|---|---|---|---|---|
| `place.lander` | `map.place.lander.name` (`:1302`) | `map.place.lander.note` (`:1306`) | **KEEP** | `The Lander` / `Tàu đổ bộ` | `Home. Population: four, one rover, one very proud basil.` | `Nhà. Dân số: bốn người, một rover, một chậu húng quế rất tự hào.` |
| `site.depot` | `map.site.depot.name` (`:1310`) | `map.site.depot.note` (`:1314`) | **KEEP** | `Supply Depot` / `Kho tiếp tế` | `Spare everything. Mostly bolts.` | `Đồ dự phòng đủ cả. Chủ yếu là bu-lông.` |
| `site.kestrel` | `map.site.kestrel.name` (`:1318`) | `map.site.kestrel.note` (`:1322`) | Name **KEEP** / Note **REPLACE** | `Kestrel-3` / `Kestrel-3` | `Meridian's relay satellite. Came down in October, still pointing at the Arks.` | `Vệ tinh tiếp sóng của Meridian. Rơi xuống hồi tháng Mười, vẫn chĩa về phía các tàu Ark.` |
| `trail.kestrel` | `map.trail.kestrel.name` (`:1326`) | `map.trail.kestrel.note` (`:1330`) | **KEEP** | `Kestrel's skid` / `Vệt trượt của Kestrel` | `I paced it. Twice.` | `Tôi đã đi bộ đo nó. Hai lần.` |
| `site.drill` | `map.site.drill.name` (`:1334`) | `map.site.drill.note` (`:1338`) | **KEEP** | `Drill Rig` / `Giàn khoan` | `My drill. Core 41 came from here, the blue one.` | `Máy khoan của tôi. Mẫu lõi 41 lấy ở đây, viên màu xanh.` |
| `site.garage` | `map.site.garage.name` (`:1342`) | `map.site.garage.note` (`:1346`) | **KEEP** | `Kenji's Garage` / `Gara của Kenji` | `No entry without Kenji. Kenji: no entry without a helmet.` | `Không vào nếu không có Kenji. Kenji: không vào nếu không đội mũ.` |
| `site.lander` | `map.site.lander.name` (`:1350`) | `map.site.lander.note` (`:1354`) | Name **KEEP** / Note **REPLACE** | `Canyon Supply Lander` / `Tàu tiếp tế trong hẻm` | `The '87 resupply drop with Theo's seed trays. Landed… firmly.` | `Chuyến tiếp tế năm 87 chở khay hạt giống của Theo. Hạ cánh hơi… mạnh tay.` |
| `friend.tilly` | `map.friend.tilly.name` (`:1358`) | `map.friend.tilly.note` (`:1362`) | **KEEP** | `Tilly's crater` / `Hố của Tilly` | `Where she sulks when I walk too fast.` | `Chỗ con bé dỗi mỗi khi tôi đi nhanh quá.` |
| `relay.0` | `map.relay.0.name` (`:1366`) | `map.relay.0.note` (`:1370`) | **KEEP** | `Mound Relay` / `Trạm trên gò` | `Ro calls it the little one.` | `Ro gọi nó là "bé út".` |
| `relay.1` | `map.relay.1.name` (`:1374`) | `map.relay.1.note` (`:1378`) | **KEEP** | `Shoulder Relay` / `Trạm vai vành hố` | `Longest shadows in the basin.` | `Bóng đổ dài nhất lòng chảo.` |
| `relay.2` | `map.relay.2.name` (`:1382`) | `map.relay.2.note` (`:1386`) | **KEEP** | `Canyon Mouth Relay` / `Trạm miệng hẻm` | `Everything echoes here. Even the static.` | `Ở đây cái gì cũng vang. Cả tiếng rè.` |
| `relay.3` | `map.relay.3.name` (`:1390`) | `map.relay.3.note` (`:1394`) | **KEEP** | `Terminus Relay` / `Trạm cuối hẻm` | `Right above Ro's chamber. Of course.` | `Ngay trên căn phòng của Ro. Đương nhiên rồi.` |
| `canyon.mouth` | `map.canyon.mouth.name` (`:1398`) | `map.canyon.mouth.note` (`:1402`) | **KEEP** | `Whispering Canyon` / `Hẻm Thì Thầm` | `Say something. It says it back, quieter.` | `Nói gì đó đi. Nó sẽ nói lại, nhỏ hơn.` |
| `canyon.lip` | `map.canyon.lip.name` (`:1406`) | `map.canyon.lip.note` (`:1410`) | **KEEP** | `The chasm` / `Vực sâu` | `Too wide to cross. Kenji says "for now".` | `Quá rộng để qua. Kenji bảo "tạm thời thôi".` |
| `canyon.landing` | `map.canyon.landing.name` (`:1414`) | `map.canyon.landing.note` (`:1418`) | **KEEP** | `The far side` / `Bờ bên kia` | `Someone always leaves a light on over here.` | `Bên này lúc nào cũng có người để đèn.` |
| `canyon.ledge` | `map.canyon.ledge.name` (`:1422`) | `map.canyon.ledge.note` (`:1426`) | **KEEP** | `Ro's ledge` / `Gờ đá của Ro` | `She comes here to hear herself sing back.` | `Cô ấy ra đây để nghe tiếng mình hát vọng lại.` |
| `canyon.terminus` | `map.canyon.terminus.name` (`:1430`) | `map.canyon.terminus.note` (`:1434`) | **KEEP** | `Ro's chamber` / `Căn phòng của Ro` | `Best acoustics on the Moon. Her words, not mine.` | `Âm thanh hay nhất Mặt Trăng. Lời cô ấy, không phải của tôi.` |
| `canyon.exit` | `map.canyon.exit.name` (`:1438`) | `map.canyon.exit.note` (`:1442`) | **KEEP** | `The way out` / `Lối ra` | `One way only. Don't try it upwards.` | `Chỉ đi một chiều. Đừng thử leo ngược lên.` |
| `region.peak` | `map.region.peak.name` (`:1446`) | `map.region.peak.note` (`:1450`) | Name **KEEP** / Note **REPLACE** | `The Peak` / `Đỉnh núi` | `The high-gain dish. Last stop before we send the survey up.` | `Chảo ăng-ten cao tần. Điểm cuối trước khi gửi bản khảo sát lên.` |
| `region.terraces` | `map.region.terraces.name` (`:1454`) | `map.region.terraces.note` (`:1458`) | Name **KEEP** / Note **REPLACE** | `Rim Terraces` / `Bậc thềm vành hố` | `Too steep for four wheels. The '79 Atlas rig is still parked up here.` | `Quá dốc cho xe bốn bánh. Cỗ xe Atlas năm 79 vẫn đỗ trên này.` |
| `place.antenna_farm` | `map.place.antenna_farm.name` (`:1462`) | `map.place.antenna_farm.note` (`:1466`) | **KEEP** | `Ro's antennas` / `Rừng ăng-ten của Ro` | `They hum in a solar storm. Ro calls it the choir.` | `Gặp bão mặt trời là chúng ngân lên. Ro gọi đó là dàn đồng ca.` |

---

## 5. Proposed Future Map Entries (`[PROPOSAL]` — Not Yet in `en.json`)

These keys are proposed for `0.6+` regions when their `WorldAnchorIds` ship:

| Proposed Key | Proposed Name (EN / VI) | Proposed v4 Note (EN) | Proposed v4 Note (VI) |
|---|---|---|---|
| `map.region.crater.name` / `.note` (`[PROPOSAL]`) | `Theo's crater` / `Hố của Theo` | `Cold trap at the crater floor. Theo stored the backup seed tins down here.` | `Bẫy lạnh dưới đáy hố. Theo cất mấy hộp hạt giống dự phòng ở dưới này.` |
| `map.region.hollow.name` / `.note` (`[PROPOSAL]`) | `Kestrel's hollow` / `Hốc tối cạnh Kestrel` | `Something glints at the back. Too dark to tell.` | `Có gì đó lấp lánh tít trong. Tối quá, không nhìn rõ.` |
| `map.route.crew_road.name` / `.note` (`[PROPOSAL]`) | `The road to the canyon` / `Đường ra hẻm` | `Follow the posts. Kenji planted them, crooked.` | `Cứ theo mấy cây cọc. Kenji cắm đấy, xiêu xiêu vẹo vẹo.` |
| `map.canyon.bridge.name` / `.note` (`[PROPOSAL]`) | `Kenji's bridge` / `Cầu của Kenji` | `It holds. Mostly.` | `Vẫn trụ được. Phần lớn là vậy.` |
