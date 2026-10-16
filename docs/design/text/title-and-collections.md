# Text: Boot & Recovery HUD, Title Screen, Relics, Cassettes & Collections (M3-09 · Story v4)

> **Status:** Locked for Story v4 (`1985–1988` crew mission, `2024` rover wake).
> **Target files for UI port:** `Assets/_Project/Data/Localization/en.json` and `Assets/_Project/Data/Localization/vi.json`.
> **Writing rules (`docs/design/writing-style.md`):**
> - Operational HUD and recovery prompts are neutral machine telemetry.
> - Relics and cassette liner notes are artifacts from the 1985–1988 golden years at Lumen Station; all 12 relic keys and 6 cassette keys are **KEEP** in Story v4.

---

## 1. Boot, Recovery & Ending Answer Strings (`en.json:254–326, 1230, 1626–1634`)

*(Cross-referenced with `docs/design/text/radio-letters.md` §2 and §5.)*

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt | Why Kept / Changed |
|---|---|---|---|---|---|
| `lens.boot.signal` (`en.json:1626`) | **REPLACE** | `EARTH SIGNAL · DETECTED` | `ORBITAL SIGNAL · DETECTED` | `TÍN HIỆU QUỸ ĐẠO · ĐÃ NHẬN` | The wake ping comes from Meridian / the L4/L5 orbital arks (`ArkSignal.cs`), not from Earth's surface. |
| `lens.boot.uplink` (`en.json:1630`) | **KEEP** | `UPLINK · OFFLINE` | *(Keep current)* `UPLINK · OFFLINE` | *(Keep current)* `ĐƯỜNG TRUYỀN · MẤT KẾT NỐI` | Neutral machine status. |
| `lens.boot.recovery` (`en.json:1634`) | **KEEP** | `BEGIN RECOVERY` | *(Keep current)* `BEGIN RECOVERY` | *(Keep current)* `BẮT ĐẦU KHÔI PHỤC` | Neutral machine prompt. |
| `ui.recovery.title` (`en.json:254`) | **KEEP** | `LUMEN RECOVERY PROTOCOL` | *(Keep current)* `LUMEN RECOVERY PROTOCOL` | *(Keep current)* `QUY TRÌNH KHÔI PHỤC LUMEN` | Neutral machine header. |
| `recovery.power.name` (`en.json:258`) | **KEEP** | `Power the station` | *(Keep current)* `Power the station` | *(Keep current)* `Cấp điện cho trạm` | Step 1 of recovery protocol. |
| `recovery.link.name` (`en.json:262`) | **KEEP** | `Link the network to the Peak` | *(Keep current)* `Link the network to the Peak` | *(Keep current)* `Nối mạng lưới tới Đỉnh núi` | Step 2 of recovery protocol. |
| `recovery.dish.name` (`en.json:266`) | **KEEP** | `Repair the great dish` | *(Keep current)* `Repair the great dish` | *(Keep current)* `Sửa chảo ăng-ten lớn` | Step 3 of recovery protocol. |
| `recovery.answer.name` (`en.json:270`) | **REPLACE** | `Answer Earth` | `Answer the signal` | `Trả lời tín hiệu` | Preserves mystery before the ending and matches the orbital ark source. |
| `ticker.recovery.link` (`en.json:318`) | **KEEP** | `The Link reaches the Peak.` | *(Keep current)* `The Link reaches the Peak.` | *(Keep current)* `Mạng lưới đã nối tới Đỉnh núi.` | Neutral status ticker. |
| `ticker.recovery.dish` (`en.json:322`) | **KEEP** | `The great dish is repaired.` | *(Keep current)* `The great dish is repaired.` | *(Keep current)* `Chảo ăng-ten lớn đã sửa xong.` | Neutral status ticker. |
| `ticker.recovery.answer` (`en.json:326`) | **REPLACE** | `Earth has heard us.` | `The Arks have heard us.` | `Các tàu Ark đã nghe thấy.` | The L4/L5 orbital arks receive the completed basin survey via Meridian. |
| `ticker.ending.answer` (`en.json:1230`) | **REPLACE** | `I heard you, 07. I'm still here. — Ro` | `Lumen Station, this is Moreau on Ark IV. Thirty-six years of Sundays, and your survey finally came through. We're coming home. — L. Moreau` | `Lumen Station, đây là Moreau trên Ark IV. Ba mươi sáu năm canh mỗi Chủ nhật, cuối cùng bản khảo sát của trạm cũng đã tới nơi. Chúng tôi đang về nhà. — L. Moreau` | Replaces Ro's voice from Earth with Dr. Lena Moreau aboard Ark IV (Decision 5). |

---

## 2. Title Screen (`en.json:1470–1490, 1558–1570`) — All `KEEP`

| Key | `en.json` Line | Status | English (`en.json`) | Tiếng Việt (`vi.json`) |
|---|---|---|---|---|
| `ui.title.subtitle` | `en.json:1470` | **KEEP** | `a radio left on` | `một chiếc radio vẫn bật` |
| `ui.title.continue` | `en.json:1474` | **KEEP** | `Continue` | `Tiếp tục` |
| `ui.title.new_game` | `en.json:1478` | **KEEP** | `New Game` | `Chơi mới` |
| `ui.title.settings` | `en.json:1482` | **KEEP** | `Settings` | `Cài đặt` |
| `ui.title.quit` | `en.json:1486` | **KEEP** | `Quit` | `Thoát` |
| `ui.title.credits` | `en.json:1490` | **KEEP** | `Credits` | `Ghi công` |
| `ui.title.new_game_question` | `en.json:1558` | **KEEP** | `Start a new game?` | `Bắt đầu chơi mới?` |
| `ui.title.new_game_aside` | `en.json:1562` | **KEEP** | `Your current save is kept, not deleted.` | `Bản lưu hiện tại vẫn được giữ lại.` |
| `ui.title.new_game_keep` | `en.json:1566` | **KEEP** | `Keep playing` | `Chơi tiếp` |
| `ui.title.new_game_confirm` | `en.json:1570` | **KEEP** | `Start new` | `Chơi mới` |

---

## 3. Crew Plates & Memory Shelf Tickers (`en.json:1578–1622`) — All `KEEP`

| Key | `en.json` Line | Status | English (`en.json`) | Tiếng Việt (`vi.json`) |
|---|---|---|---|---|
| `crew.ro.plate` | `en.json:1578` | **KEEP** | `Ro, on air` | `Ro, đang lên sóng` |
| `crew.theo.plate` | `en.json:1582` | **KEEP** | `Theo & the seedlings` | `Theo & lũ cây con` |
| `crew.ines.plate` | `en.json:1586` | **KEEP** | `I. Kalani, Lead` | `I. Kalani, Trưởng nhóm` |
| `crew.kenji.plate` | `en.json:1590` | **KEEP** | `Kenji (ask the duck)` | `Kenji (hỏi chú vịt)` |
| `ticker.set.ro.filling` | `en.json:1594` | **KEEP** | `Ro's shelf is filling up. It already sounds like her.` | `Kệ của Ro đầy dần. Nghe đã thấy giống cô ấy rồi.` |
| `ticker.set.theo.filling` | `en.json:1598` | **KEEP** | `Theo's shelf is filling up. Something on it is almost growing.` | `Kệ của Theo đầy dần. Có món gì trên đó như sắp nảy mầm.` |
| `ticker.set.ines.filling` | `en.json:1602` | **KEEP** | `Ines's shelf is filling up, everything in neat rows.` | `Kệ của Ines đầy dần, món nào cũng xếp thẳng hàng.` |
| `ticker.set.kenji.filling` | `en.json:1606` | **KEEP** | `Kenji's shelf is filling up. The duck approves.` | `Kệ của Kenji đầy dần. Chú vịt gật gù tán thành.` |
| `ticker.set.ro.complete` | `en.json:1610` | **KEEP** | `Ro's window is lit again.` | `Cửa sổ của Ro lại sáng đèn.` |
| `ticker.set.theo.complete` | `en.json:1614` | **KEEP** | `Theo's window is lit again.` | `Cửa sổ của Theo lại sáng đèn.` |
| `ticker.set.ines.complete` | `en.json:1618` | **KEEP** | `Ines's window is lit again.` | `Cửa sổ của Ines lại sáng đèn.` |
| `ticker.set.kenji.complete` | `en.json:1622` | **KEEP** | `Kenji's window is lit again.` | `Cửa sổ của Kenji lại sáng đèn.` |

---

## 4. Relics (`en.json:558–602`) & Cassettes (`en.json:750–770`) — All `KEEP`

Every relic and cassette dates to the 1985–1987 operational years before the Long Dim grounded the relief lander. In Story v4, Ro's `"for when we come home"` on Side B of her mixtape becomes poignant irony rather than a literal prediction.

| Key | `en.json` Line | Status | English (`en.json`) | Tiếng Việt (`vi.json`) |
|---|---|---|---|---|
| `relic.cassette_player.name` | `en.json:558` | **KEEP** | `Ro's Mixtape` | `Cuộn băng của Ro` |
| `relic.cassette_player.memory` | `en.json:562` | **KEEP** | `A walkman with a tape still inside, labelled in Ro's handwriting: "for the long drive". Side B just says "for when we come home".` | `Chiếc walkman vẫn còn cuộn băng bên trong, nét chữ của Ro ghi: "cho chuyến đi dài". Mặt B chỉ ghi: "cho ngày mình về nhà".` |
| `relic.rubber_duck.name` | `en.json:566` | **KEEP** | `Senior Debugging Consultant` | `Cố vấn gỡ lỗi cấp cao` |
| `relic.rubber_duck.memory` | `en.json:570` | **KEEP** | `Kenji explained every broken part to this duck before fixing it. The duck never said a word, and it was usually right.` | `Kenji giải thích mọi bộ phận hỏng cho chú vịt này nghe trước khi sửa. Chú chẳng nói câu nào, mà thường thì chú đúng.` |
| `relic.golden_record.name` | `en.json:574` | **KEEP** | `The Lumen Record` | `Đĩa vàng Lumen` |
| `relic.golden_record.memory` | `en.json:578` | **KEEP** | `Ro's homemade golden record: greetings from four people, one rover and a very proud basil plant, ready for anyone listening.` | `Chiếc đĩa vàng Ro tự làm: lời chào từ bốn con người, một chiếc rover và một chậu húng quế rất đỗi tự hào, sẵn sàng gửi tới bất kỳ ai đang lắng nghe.` |
| `relic.astronaut_boot.name` | `en.json:582` | **KEEP** | `Ines's Field Boot` | `Đôi ủng của Ines` |
| `relic.astronaut_boot.memory` | `en.json:586` | **KEEP** | `Ines walked the rim every morning "to check the Moon was still there". The tread is still packed with dust from her favourite ridge.` | `Sáng nào Ines cũng đi dọc vành núi "để chắc rằng Mặt Trăng vẫn còn đó". Gai đế vẫn còn đầy bụi của ngọn đồi chị yêu nhất.` |
| `relic.teapot.name` | `en.json:590` | **KEEP** | `The First Real Tea` | `Ấm trà thật sự đầu tiên` |
| `relic.teapot.memory` | `en.json:594` | **KEEP** | `Theo brewed mint grown in lunar soil in this dented pot. "The first tea on the Moon that tasted like anything," Ro wrote on the lid.` | `Theo pha bạc hà trồng trong đất Mặt Trăng bằng chiếc ấm móp này. "Ấm trà đầu tiên trên Mặt Trăng có vị thật sự", Ro viết trên nắp.` |
| `relic.garden_gnome.name` | `en.json:598` | **KEEP** | `Chief Security Officer` | `Trưởng ban An ninh` |
| `relic.garden_gnome.memory` | `en.json:602` | **KEEP** | `Theo smuggled him aboard as a joke and put him in charge of the seedlings. Nothing was ever stolen on his watch.` | `Theo lén mang chú lên tàu cho vui rồi giao chú canh vườn ươm. Suốt nhiệm kỳ, chưa một cây non nào bị mất.` |
| `cassette.after_dark_1.title` | `en.json:750` | **KEEP** | `Lumen After Dark, Vol. 1` | `Lumen After Dark, Tập 1` |
| `cassette.after_dark_1.note` | `en.json:754` | **KEEP** | `First night show. Audience: three crew, one rover, one basil plant. The basil had requests.` | `Buổi phát sóng đêm đầu tiên. Khán giả: ba người, một chiếc rover, một chậu húng quế. Chậu húng quế còn xin bài.` |
| `cassette.dust_and_honey.title` | `en.json:758` | **KEEP** | `Dust & Honey` | `Bụi & Mật ong` |
| `cassette.dust_and_honey.note` | `en.json:762` | **KEEP** | `Recorded with the mic taped to the airlock. If you hear a thump, that's Kenji.` | `Thu với cái micro dán băng keo lên cửa khoang. Nghe tiếng thịch thì đó là Kenji.` |
| `cassette.slow_orbit.title` | `en.json:766` | **KEEP** | `Slow Orbit` | `Quỹ đạo chậm` |
| `cassette.slow_orbit.note` | `en.json:770` | **KEEP** | `For the nights Earth looks close enough to walk to. It isn't. I checked.` | `Cho những đêm Trái Đất trông gần như đi bộ tới được. Không đâu. Mình thử rồi.` |
| `micro.06.name` | `en.json:1938` | **KEEP** | `06` | `06` |
| `micro.mule.name` | `en.json:1942` | **KEEP** | `Mule` | `Mule` |
| `micro.haul_sled.name` | `en.json:1946` | **KEEP** | `Haul Sled` | `Xe chở hàng` |
| `micro.drone.name` | `en.json:1950` | **KEEP** | `Tilly's sister` | `Chị em của Tilly` |
