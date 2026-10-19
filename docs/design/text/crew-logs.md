# Text: Crew Logs, Friend Logs & Supply Crate Notes (Story v4)

> **Status:** Locked for Story v4 (Lumen built `1984`, crewed from `1985` to `1988`; `2024` rover wake).
> **Target files for UI port:** `Assets/_Project/Data/Localization/en.json` and `Assets/_Project/Data/Localization/vi.json`.
> **Writing rules (`docs/design/writing-style.md`):**
> - Crew logs are entries a person wrote in their own notebook or recorded for the station archive.
> - They may mention 07 in the third person (`"07 rolled up to the yard speaker"`), as you would mention a cat or a lorry.
> - **Never** write a human addressing 07 as a confidant (`"I need you to know..."`, `"If anything grows, tell me"`, `"Don't let anyone retire you, 07"`).
> - Operational notes to a rover or robot are maintenance tags or work orders (`"queued the dish alignment and Meridian uplink on 07's recovery stack"`, `"handed the greenhouse loop over to Moss"`).
> - One or two sentences. A single concrete detail beats an explanation.
> - **Vietnamese pronouns:** Ro, Theo, and Kenji write `"mình"`; Ines (mission lead) writes `"tôi"`. They refer to 07 in the third person (`"07"`, `"nó"`, `"chiếc xe nhỏ"`), never `"cậu"` as a human confidant. Others refer to Ines as `"chị ấy"` and to Theo and Kenji as `"anh ấy"`.

---

## 1. Key Summary (`Assets/_Project/Data/Localization/en.json`)

| Key | `en.json` Line | Action | Reason |
|---|---|---|---|
| `log.ro_1` | `en.json:746` | **REPLACE** | Removes `"a rover who"` personification; sets the 1985 local loop archive tone. |
| `log.kenji_1` | `en.json:1142` | **REPLACE** | Removes pet-like `"blinks it at me whenever I walk past"`; makes the beacon a receiver-lock maintenance detail. |
| `log.ines_1` | `en.json:1146` | **KEEP** | Already fits Ines's notebook voice and sets up `relic.astronaut_boot` / Core 41. |
| `log.theo_1` | `en.json:1150` | **KEEP** | Already fits Theo's notebook voice and sets up `relic.teapot`. |
| `log.ro_2` | `en.json:1154` | **REPLACE** | Replaces v3 evacuation storm warning with the October 1988 Geneva flight suspension telex. |
| `log.ines_2` | `en.json:1158` | **REPLACE** | Replaces `"leave on the morning shuttle"` with the October 1988 vote to stay and finish the basin survey. |
| `log.kenji_2` | `en.json:1162` | **KEEP** | Third-person maintenance log (`"Hid a spare wheel for 07..."`); works in v4. |
| `log.theo_2` | `en.json:1166` | **REPLACE** | Replaces `"Ten minutes to pack"` for the shuttle with sealing the backup seed tins and handing the greenhouse loop to Moss. |
| `log.ro_3` | `en.json:1170` | **REPLACE** | Removes `"before the shuttle"` and direct address (`"for you, 07"`); references patching the deck into the mast loop for Lena's Sunday window. |
| `log.ines_3` | `en.json:1174` | **REPLACE** | Removes `"We were coming back, 07"`; replaces with queuing the dish alignment and Meridian uplink on 07's recovery stack. |
| `log.theo_3` | `en.json:1178` | **REPLACE** | Removes `"If anything grows, tell me"`; replaces with closing the hydroponic cycle so the arks find working soil. |
| `log.kenji_3` | `en.json:1182` | **REPLACE** | Removes `"Don't let anyone retire you, 07"`; explains wiring 07's cradle to the deep-charge bus and Meridian carrier wake trigger. |
| `friend.tilly.name` | `en.json:730` | **KEEP** | Unchanged. |
| `friend.tilly.repair_log` | `en.json:734` | **KEEP** | Unchanged (`1985–1987` routine rim walk). |
| `friend.bell.name` | `en.json:738` | **KEEP** | Unchanged. |
| `friend.bell.repair_log` | `en.json:742` | **KEEP** | Unchanged. |
| `ticker.bell.home` | `en.json:774` | **KEEP** | Unchanged. |
| `ticker.bell.signal` | `en.json:778` | **KEEP** | Unchanged. |
| `ticker.bell.found_0` | `en.json:782` | **KEEP** | Unchanged. |
| `ticker.bell.found_1` | `en.json:786` | **KEEP** | Unchanged. |
| `ticker.bell.found_2` | `en.json:790` | **REPLACE** | Replaces past-counterfactual `"Ro would have liked that"` (premature death tell) with `"Bell chimes: one of Ro's old favourites."` |
| `crate.depot.note` | `en.json:1774` | **REPLACE** | Replaces `"Spares for the next rotation"` with ridge-line spares. |
| `crate.kestrel.note` | `en.json:1778` | **REPLACE** | Clarifies `Kestrel-3` is Meridian's relay satellite (`Site_KestrelCrash`), down in October 1988. |
| `crate.drill.note` | `en.json:1782` | **REPLACE** | Replaces `"next season"` with the deep-core ice survey. |
| `crate.garage.note` | `en.json:1786` | **KEEP** | Third-person maintenance tag (`"For 07, next time it breaks. It will. — Kenji"`). |
| `crate.lander.note` | `en.json:1790` | **REPLACE** | Clarifies the `'87` uncrewed drop (`Site_SupplyLander`, the cargo lander's sister). |

---

## 2. The Four First Logs (1985–1987: Life at Lumen)

Found in the basin (`0.5`). They establish who the four specialists were during the golden years of the mission.

| Key & `en.json` Line | Status | Found at | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|---|
| `log.ro_1` (`en.json:746`) | **REPLACE** | `canyon.terminus` (beside Bell) | `Night one of Lumen After Dark. Three listeners and a rover who rolls closer when I play the slow ones. Best audience I've ever had. — Ro` | `Night one of Lumen After Dark on the local loop. Audience: three tired scientists in the galley, plus 07 parked by the repeater mast because the transformer hums warm. — Ro` | `Đêm đầu tiên của Lumen After Dark trên sóng nội bộ. Khán giả: ba nhà khoa học mệt nhoài dưới bếp, cộng thêm 07 đỗ cạnh cột tiếp sóng vì cái biến áp ngân ấm. — Ro` |
| `log.kenji_1` (`en.json:1142`) | **REPLACE** | `site.garage` (`Site_Garage` ruins) | `Fixed the old survey rover today, the seventh thing on this week's list, so it's 07 now. I taught its antenna our call sign, three short, one long, and it blinks it at me whenever I walk past. — Kenji` | `Rebuilt the drive train on the spare survey chassis—seventh job on the board this week, so it's 07 now. Wired the beacon to key our call sign, three short and one long, whenever the receiver locks. — Kenji` | `Hôm nay làm lại hệ truyền động cho khung xe khảo sát dự phòng—việc thứ bảy trên bảng tuần này, nên đặt luôn là 07. Đấu sẵn đèn hiệu nháy mã gọi của trạm, ba ngắn một dài, mỗi khi máy thu khóa được sóng. — Kenji` |
| `log.ines_1` (`en.json:1146`) | **KEEP** | `site.drill` (`Site_DrillRig`) | `Core sample 41: grey dust, grey dust, one blue pebble. Logged it as "anomalous". Kept it as "mine". — Ines` | *(Keep current)* `Core sample 41: grey dust, grey dust, one blue pebble. Logged it as "anomalous". Kept it as "mine". — Ines` | *(Keep current)* `Mẫu lõi số 41: bụi xám, bụi xám, một viên sỏi xanh. Ghi vào sổ là "bất thường". Giữ lại làm "của tôi". — Ines` |
| `log.theo_1` (`en.json:1150`) | **KEEP** | `site.lander` (`Site_SupplyLander`) | `Named every seedling in tray four; the mint is Gerald. Gerald is the best mint on the Moon, which is a low bar, but he's clearing it. — Theo` | *(Keep current)* `Named every seedling in tray four; the mint is Gerald. Gerald is the best mint on the Moon, which is a low bar, but he's clearing it. — Theo` | *(Keep current)* `Đặt tên cho hết cây con ở khay số bốn; cây bạc hà tên là Gerald. Gerald là cây bạc hà ngon nhất trên Mặt Trăng; tiêu chuẩn thì thấp thật, nhưng cậu chàng vượt qua ngon lành. — Theo` |

---

## 3. The Four Second Logs (October 1988: The Long Dim & The Vote to Stay)

Found in the outer regions (`0.6–0.7`). They record the week Geneva suspended the autumn relief flight and the four crew voted to spend their remaining consumables finishing the basin survey for the orbital arks.

| Key & `en.json` Line | Status | Found at | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|---|
| `log.ro_2` (`en.json:1154`) | **REPLACE** | Antenna farm (`place.antenna_farm`) | `Read the storm warning on air tonight, tucked between two slow songs so it wouldn't sound so scary. 07 rolled right up to the speaker and stayed there. — Ro` | `Read Geneva's flight suspension telex on air tonight, tucked between two slow songs so the galley wouldn't go quiet. 07 rolled up to the yard speaker and parked in the spill light. — Ro` | `Tối nay đọc bức điện đình chỉ bay từ Geneva trên sóng, kẹp giữa hai bài nhạc chậm để dưới bếp khỏi chùng xuống. 07 lăn tới sát loa sân trạm rồi đỗ yên trong vệt đèn hắt ra. — Ro` |
| `log.ines_2` (`en.json:1158`) | **REPLACE** | Rim Terraces (`region.terraces`) | `My call as mission lead: all four of us leave on the morning shuttle. It's the right call, and I've never hated being right so much. — Ines` | `No return lander is coming, so I put it to a vote after supper: ration the scrubbers, or run the full basin survey on what we have left. Four hands went up before I finished the sentence. — Ines` | `Không có tàu đón về nữa, nên sau bữa tối tôi đưa ra biểu quyết: dè sẻn bộ lọc khí, hoặc chạy nốt đợt khảo sát toàn lòng chảo với những gì còn lại. Cả bốn cánh tay đều giơ lên trước khi tôi nói hết câu. — Ines` |
| `log.kenji_2` (`en.json:1162`) | **KEEP** | Crawler wreck at Terraces' foot | `Hid a spare wheel for 07 under the tarp in the crawler. Next time I'll give it six matching ones, not just patch the odd one out. — Kenji` | *(Keep current)* `Hid a spare wheel for 07 under the tarp in the crawler. Next time I'll give it six matching ones, not just patch the odd one out. — Kenji` | *(Keep current)* `Giấu một cái bánh dự phòng cho 07 dưới tấm bạt trong xe bánh xích. Lần tới mình sẽ thay cho nó đủ sáu bánh giống nhau, chứ không chỉ vá cái bánh lạc loài. — Kenji` |
| `log.theo_2` (`en.json:1166`) | **REPLACE** | Shadowed Crater (`region.crater`, beside Moss) | `Ten minutes to pack. I left Moss the watering schedule, and Moss hummed at me as if I'd insulted the tomatoes. — Theo` | `Sealed the backup seed tins in the crater cache and handed the greenhouse loop over to Moss. Moss hummed at me for twenty seconds, as if I'd personally insulted the tomatoes. — Theo` | `Đã niêm phong mấy hộp hạt giống dự phòng dưới kho hố tối và bàn giao vòng tưới nhà kính lại cho Moss. Moss ngân nga với mình suốt hai mươi giây, cứ như thể mình vừa đích thân xúc phạm đám cà chua. — Theo` |

---

## 4. The Four Third Logs (November 1988: Completing the Work)

Played on the station radio when a crew member's memory shelf is completed (`CrewSetCompleted`). Each is the crew member's final archive/notebook entry from late November 1988 before the ridge storm cut the peak lines and Suits `01–04` went offline. None of them address 07 in the second person.

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|
| `log.ro_3` (`en.json:1170`) | **REPLACE** | `Last show before the shuttle. I left the radio on for you, 07. Keep it playing, and we'll find you by the music. — Ro` | `Last night of the live show; patched the deck into the mast loop so the music keeps running while we finish the ridge work. If Lena's listening on the Sunday window, she'll know the porch light is still on. — Ro` | `Đêm cuối phát sóng trực tiếp; mình đã đấu đầu băng vào vòng lặp cột phát để nhạc cứ thế chạy trong lúc cả nhóm làm nốt việc trên sống núi. Nếu Chủ nhật tới Lena có mở máy nghe, cô ấy sẽ biết đèn hiên trạm vẫn sáng. — Ro` |
| `log.ines_3` (`en.json:1174`) | **REPLACE** | `We were coming back, 07. I need you to know we tried. — Ines` | `Basin survey compiled and loaded into the main core; queued the dish alignment and Meridian uplink on 07's recovery stack in case the ridge team runs short of hours. Everything they need to come home is in the file. — Ines` | `Bản khảo sát lòng chảo đã tổng hợp và nạp vào lõi chính; tôi đã xếp hàng lệnh căn chỉnh chảo lớn và phát lên Meridian trong ngăn khôi phục của 07 phòng khi đội trên sống núi không kịp giờ. Mọi thứ họ cần để trở về đều nằm trong hồ sơ đó. — Ines` |
| `log.theo_3` (`en.json:1178`) | **REPLACE** | `Left the last tray by the window. If anything grows, tell me. — Theo` | `Set the hydroponic vents to closed-cycle and left the last basil tray by the south window where the earthlight hits. When the arks bring their seed stock down, they'll want to see the soil already works. — Theo` | `Đã chuyển cửa thông gió thủy canh sang chu trình kín và đặt khay húng quế cuối cùng cạnh cửa sổ hướng nam, nơi ánh Trái Đất rọi vào. Khi các tàu Ark mang kho hạt giống đáp xuống, hẳn họ sẽ mừng khi thấy đất này đã trồng trọt được. — Theo` |
| `log.kenji_3` (`en.json:1182`) | **REPLACE** | `They asked me what to do with the rover, and I said leave it switched on. Don't let anyone retire you, 07. — Kenji` | `Wired 07's cradle to the deep-charge bus and set the wake trigger to the Meridian carrier band. The storm took the ridge lines, so if the dish stays dark after today, the little guy can finish the walk when the batteries top up. — Kenji` | `Đã đấu bệ sạc của 07 vào đường điện ngầm và đặt bộ kích thức dậy theo băng tần sóng mang Meridian. Bão làm đứt đường dây trên sống núi rồi, nên nếu sau hôm nay chảo lớn vẫn tối, chiếc xe nhỏ có thể tự đi nốt quãng đường khi pin đầy lại. — Kenji` |

---

## 5. Friend Repair Cards & Bell Tickers (`en.json:730–790`)

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|
| `friend.tilly.name` (`en.json:730`) | **KEEP** | `Tilly` | *(Keep current)* `Tilly` | *(Keep current)* `Tilly` |
| `friend.tilly.repair_log` (`en.json:734`) | **KEEP** | `Walked the rim at sunrise with Tilly chirping at my shoulder. The Moon is still here, Earth is still up, the coffee is still terrible: all is well. — Ines` | *(Keep current)* `Walked the rim at sunrise with Tilly chirping at my shoulder. The Moon is still here, Earth is still up, the coffee is still terrible: all is well. — Ines` | *(Keep current)* `Sáng nay lại đi dọc vành núi lúc bình minh, Tilly ríu rít bên vai. Mặt Trăng vẫn còn đây, Trái Đất vẫn treo trên trời, cà phê vẫn dở tệ: mọi chuyện vẫn ổn. — Ines` |
| `friend.bell.name` (`en.json:738`) | **KEEP** | `Bell` | *(Keep current)* `Bell` | *(Keep current)* `Bell` |
| `friend.bell.repair_log` (`en.json:742`) | **KEEP** | `Built Bell out of a dead receiver and four camera legs so the station keeps playing while I sleep. She dances. I didn't build that part. — Ro` | *(Keep current)* `Built Bell out of a dead receiver and four camera legs so the station keeps playing while I sleep. She dances. I didn't build that part. — Ro` | *(Keep current)* `Mình lắp Bell từ một máy thu hỏng và bốn cái chân máy quay, để đài vẫn phát khi mình ngủ. Cô ấy biết nhảy. Phần đó mình không hề lắp. — Ro` |
| `ticker.bell.home` (`en.json:774`) | **KEEP** | `Bell got home before you. She says the porch light was on.` | *(Keep current)* `Bell got home before you. She says the porch light was on.` | *(Keep current)* `Bell về nhà trước bạn. Cô ấy bảo đèn hiên vẫn sáng.` |
| `ticker.bell.signal` (`en.json:778`) | **KEEP** | `Bell has picked up a signal. Follow her light.` | *(Keep current)* `Bell has picked up a signal. Follow her light.` | *(Keep current)* `Bell bắt được tín hiệu. Hãy đi theo ánh đèn của cô ấy.` |
| `ticker.bell.found_0` (`en.json:782`) | **KEEP** | `Bell says: told you so.` | *(Keep current)* `Bell says: told you so.` | *(Keep current)* `Bell bảo: thấy chưa, đã bảo mà.` |
| `ticker.bell.found_1` (`en.json:786`) | **KEEP** | `Bell says: that one's been waiting for you.` | *(Keep current)* `Bell says: that one's been waiting for you.` | *(Keep current)* `Bell bảo: cái đó đợi bạn lâu lắm rồi.` |
| `ticker.bell.found_2` (`en.json:790`) | **REPLACE** | `Bell says: Ro would have liked that.` | `Bell chimes: one of Ro's old favourites.` | `Bell ngân vang: một bản nhạc Ro từng rất mê.` |

---

## 6. Supply Crate Stencil Notes (`en.json:1774–1790`)

Stencil notes painted or taped onto the five field supply crates in the basin (`0.5`).

| Key & `en.json` Line | Status | Current `en.json` String | v4 English | v4 Tiếng Việt |
|---|---|---|---|---|
| `crate.depot.note` (`en.json:1774`) | **REPLACE** | `Spares for the next rotation. Don't let Ines take the good wire. — Kenji` | `Spares for the ridge line. Don't let Ines take the good copper wire for her core tags. — Kenji` | `Đồ dự phòng cho đường dây sống núi. Đừng để Ines lấy cuộn dây đồng xịn đi buộc nhãn mẫu đá. — Kenji` |
| `crate.kestrel.note` (`en.json:1778`) | **REPLACE** | `Lenses off the old relay. Handle them like records. — Ro` | `Lenses off Meridian's relay satellite. Handle them like records. — Ro` | `Thấu kính tháo từ vệ tinh tiếp sóng của Meridian. Cầm nhẹ tay như cầm đĩa than nhé. — Ro` |
| `crate.drill.note` (`en.json:1782`) | **REPLACE** | `Wire for the drill's next season. Labelled, counted, twice. — I. Kalani` | `Spare cable for the deep-core ice survey. Labelled, coiled, counted twice. — I. Kalani` | `Cáp dự phòng cho đợt khoan thăm dò băng tầng sâu. Đã dán nhãn, cuộn gọn, đếm hai lần. — I. Kalani` |
| `crate.garage.note` (`en.json:1786`) | **KEEP** | `For 07, next time it breaks. It will. — Kenji` | *(Keep current)* `For 07, next time it breaks. It will. — Kenji` | *(Keep current)* `Cho 07, lần tới nó hỏng. Thế nào cũng hỏng. — Kenji` |
| `crate.lander.note` (`en.json:1790`) | **REPLACE** | `Seed trays went up top. This is everything else. — Theo` | `Seed trays from the '87 supply drop went up to the dome. Left the spare fittings boxed here. — Theo` | `Khay hạt giống từ chuyến hàng thả xuống năm 87 đã mang lên mái vòm. Phụ tùng dư đóng thùng để lại đây. — Theo` |
