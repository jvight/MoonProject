# In-game text: style guide and opening rewrite

> **Owner (playtest 2c1ea201d):** "the game's lines still don't sound like a game at all, they sound off."
>
> **Author:** the game design session, 2026-10-25. It extends `writing-style.md` (story voice) with the rules for
> **UI and guidance text**. Story & flow applies the table after the owner approves the samples.

## 1. The voice
**Who reads it:** a Vietnamese gamer who plays Subnautica, Genshin or Stardew in Vietnamese. To that player, good game
text is **short, imperative, and uses the same word for the same thing every time**. Bad text reads like a machine
translation, or like a novel.

| Rule | Do | Don't |
|---|---|---|
| **1. Verb first, imperative** | `Quét thùng hàng` | `Hãy quét…`, `Bạn cần quét…` |
| **2. Short** | chips 1–2 words; next lines ≤ 45 characters in Vietnamese; at most two short clauses | three-clause sentences |
| **3. No narration of 07** | `Chưa phát sóng được. Quét tháp radio` | `07 đang…`, `07 chưa…` (07's state belongs on its CRT, in capitals) |
| **4. One word per thing** | the glossary (§2), always | `mô-đun điện` in one line, `mô-đun nguồn` in the next |
| **5. Plain, gamer words** | `Xây`, `Lắp`, `Nhặt`, `Kéo về` | literary (`đáp lời`, `nằm gục`), calques (`kéo về sân`), hyphen chains |
| **6. A reason only when it's new** | `Tàu đổ bộ cạn điện. Tìm dàn pin mặt trời` | a reason repeated on every line |
| **7. No closing full stop** on guidance lines and chips (objective style) | `Đỗ lên bệ sạc` | `Đỗ lên bệ sạc.` |
| **8. CRT status lines** stay in capitals with `·` separators, Vietnamese with full diacritics | `PIN 0% · DỪNG` | `PIN 0% - DUNG` |
| **9. Keys** | `Bấm {0}` (press), `Giữ {0}` (hold) | `Nhấn giữ phím {0} để` |
| **10. English follows the same rules** | `Tow it to the build site` | `Tow it home to the apron.` |

## 2. Glossary (one word per thing)
| Thing | EN | VI | Why |
|---|---|---|---|
| charging pad | Charging Pad | **Bệ sạc** | a pad you park on is a *bệ*; *trụ* is kept for posts (the trickle post, the pylon) |
| solar array | Solar Array | **Dàn pin mặt trời** | the common Vietnamese term; *giàn* reads like a scaffold |
| battery bank | Battery Bank | **Bộ tích điện** | short and clear; *cụm pin tích điện* is long, and *khối pin* is taken by the cell pack |
| cell pack (07's) | Cell Pack | **Khối pin** | — |
| power module | Power Module | **Mô-đun nguồn** | *mô-đun* is normal in Vietnamese games; never *mô-đun điện* |
| apron / build bay | build site | **Bãi xây** | replaces *sân* and *bãi kẻ vạch* |
| antenna mast | Antenna Mast | **Cột ăng-ten** | — |
| lander | lander | **Tàu đổ bộ** | — |
| habitat | Habitat | **Khu nhà ở** | — |
| ruins | the ruins | **Phế tích** | the usual game word (replaces *tàn tích*) |
| relay.1 / relay.2 | **Relay 1 / Relay 2** | **Trạm tiếp sóng 1 / 2** | `relay.1` reads as code |
| unit 06 | 06 | **06** (in prose: *xe 06*) | — |
| cutter / igniter | cutter / igniter | **Mỏ cắt / Bộ đánh lửa** | — |
| hold | Hold | **Khoang hàng** | — |
| blueprint | blueprint | **Bản vẽ** | — |
| rack | rack | **Giá lắp** | — |
| headlamp | headlamp | **Đèn pha** | — |
| verbs | Scan · Cut · Build · Assemble · Fit · Pick up · Hook cable · Transmit · Dismantle | **Quét · Cắt · Xây · Lắp ráp · Lắp · Nhặt · Móc cáp · Phát sóng · Tháo** | `Xây` replaces `Xây dựng` on chips; `Phát sóng` replaces `Phát tín hiệu` |

## 3. Fifteen before-and-after samples (for the owner)
| # | Key | Old vi | **New vi** | New en |
|---|---|---|---|---|
| 1 | `next.v3.hook_module` | Móc cáp vào mô-đun nguồn và kéo về sân. | **Móc cáp vào mô-đun nguồn** | Hook the power module |
| 2 | `next.v3.tow_module` | Mô-đun điện là khung của cụm pin. Kéo nó về sân. | **Kéo mô-đun về bãi xây: nó là khung của bộ tích điện** | Tow it to the build site: it's the battery bank's frame |
| 3 | `next.v2.scan_tower` | 07 chưa phát sóng được. Quét tháp radio để lấy bản vẽ ăng-ten. | **Chưa phát sóng được. Quét tháp radio** | Can't transmit yet. Scan the radio tower |
| 4 | `next.v3.go_relay` | relay.1 đã đáp lời từ vành phía tây. Hãy lái tới đó. | **Trạm tiếp sóng 1 đang yếu dần. Tới sửa nó** | Relay 1 is fading. Go fix it |
| 5 | `next.v3.scan_elder06` | 06 nằm gục ở relay.2 cạnh cửa hẻm núi. Lái tới đó và quét nó. | **Trạm 2 im lặng. Tìm 06 ở cửa hẻm** | Relay 2 went quiet. Find 06 at the canyon mouth |
| 6 | `next.v2.build_pad` | Xây bệ sạc trên bãi kẻ vạch. | **Xây bệ sạc ở bãi xây** | Build the charging pad at the build site |
| 7 | `next.v3.dock_for_dusk` | Đỗ vào bệ để kết thúc ngày. | **Về bệ sạc nghỉ qua đêm** | Dock on the pad for the night |
| 8 | `next.v3.visit_site` | Đã có điện. Hãy ghé trạm địa chấn hoặc khu nhà ở trước khi chiều xuống. | **Đã có điện. Ghé trạm địa chấn hoặc khu nhà ở** | Power's on. Visit the seismic station or the habitat |
| 9 | `next.v2.find_solar` | Tàu đổ bộ đã cạn điện. Tìm giàn pin mặt trời trong tàn tích. | **Tàu đổ bộ cạn điện. Tìm dàn pin mặt trời ở phế tích** | The lander's out of power. Find the solar array in the ruins |
| 10 | `next.v2.cut_optics` | Cắt rời các tấm pin và hộp nối dây của giàn pin đổ. | **Cắt tấm pin và hộp dây khỏi dàn pin đổ** | Cut the panels and junction box free |
| 11 | `build.reason.frame` | Kéo mô-đun nguồn về sân trước | **Cần kéo mô-đun nguồn về đây trước** | Tow the power module here first |
| 12 | `ui.unlock.blueprint` | Đã học bản vẽ: {0} | **Bản vẽ mới: {0}** | New blueprint: {0} |
| 13 | `next.v3.dock_for_rack` | Đỗ lên bệ sạc để mở giá linh kiện. | **Đỗ lên bệ sạc để mở giá lắp** | Dock on the pad to open the rack |
| 14 | `hint.openbuild` | Xây dựng | **Xây** | Build |
| 15 | `ui.cargo.drag.nowhere` | 07 chưa có chỗ lắp món này | **Chưa lắp được món này** | Nothing takes this yet |

## 4. The full rewrite table (the most visible lines, for story & flow after approval)
| Key | Old vi | New vi | Old en | New en |
|---|---|---|---|---|
| `next.v2.drive_lander` | Lái tới tàu đổ bộ. | Lái tới tàu đổ bộ | Drive to the lander. | Drive to the lander |
| `next.v2.scan_lander` | Quét hai bộ phận trong thùng hàng mở của tàu đổ bộ. | Quét hai mảnh trong thùng hàng mở | Scan the two parts in the lander's open crate. | Scan the two pieces in the open crate |
| `next.v2.collect_parts` | Lấy các bộ phận trong thùng hàng mở. | Nhặt phụ tùng trong thùng | Take the parts from the open crate. | Take the parts from the crate |
| `next.v2.build_pad` | Xây bệ sạc trên bãi kẻ vạch. | Xây bệ sạc ở bãi xây | Build the charging pad on the marked apron. | Build the charging pad at the build site |
| `next.v2.charge` | Đỗ lên bệ để sạc. | Đỗ lên bệ sạc | Park on the pad to charge. | Park on the pad to charge |
| `next.v2.park_on_pad` | Đỗ lên bệ để lắp bộ đánh lửa. | Đỗ lên bệ sạc để lắp bộ đánh lửa | Park on the pad to fit the igniter. | Park on the pad to fit the igniter |
| `next.v2.fit_igniter` | Lắp bộ đánh lửa dự phòng vào mỏ cắt. | Lắp bộ đánh lửa vào mỏ cắt | Fit the spare igniter to the cutter. | Fit the igniter to the cutter |
| `next.v2.take_igniter` | Lấy bộ đánh lửa dự phòng ở giá cạnh giàn pin. | Nhặt bộ đánh lửa ở giá cạnh dàn pin | Take the spare igniter from the rack by the array. | Take the igniter from the rack by the array |
| `next.v2.find_solar` | (sample 9) | Tàu đổ bộ cạn điện. Tìm dàn pin mặt trời ở phế tích | (sample 9) | The lander's out of power. Find the solar array in the ruins |
| `next.v2.cut_optics` | (sample 10) | Cắt tấm pin và hộp dây khỏi dàn pin đổ | (sample 10) | Cut the panels and junction box free |
| `next.v2.build_solar` | Xây giàn pin mặt trời cạnh bệ sạc. | Xây dàn pin mặt trời cạnh bệ sạc | Build the solar array beside the pad. | Build the solar array by the pad |
| `next.v2.build_battery_bank` | Dựng cụm pin tích điện lên mô-đun. | Xây bộ tích điện lên mô-đun nguồn | Build the battery bank on the module. | Build the battery bank on the module |
| `next.v2.move_battery_bank` | Dời cụm pin tích điện lại gần bệ sạc. | Dời bộ tích điện lại gần bệ sạc | Move the battery bank closer to the pad. | Move the battery bank closer to the pad |
| `next.v2.scan_tower` | (sample 3) | Chưa phát sóng được. Quét tháp radio | (sample 3) | Can't transmit yet. Scan the radio tower |
| `next.v2.build_mast` | Xây cột ăng-ten. | Xây cột ăng-ten | Build the antenna mast. | Build the antenna mast |
| `next.v3.scan_module` | Đêm sắp xuống. Quét mô-đun điện của khu nhà ở. | Đêm sắp xuống. Quét mô-đun nguồn ở khu nhà ở | Night is coming. Scan the habitat's power module. | Night's coming. Scan the habitat's power module |
| `next.v3.hook_module` | (sample 1) | Móc cáp vào mô-đun nguồn | (sample 1) | Hook the power module |
| `next.v3.tow_module` | (sample 2) | Kéo mô-đun về bãi xây: nó là khung của bộ tích điện | (sample 2) | Tow it to the build site: it's the battery bank's frame |
| `next.v3.dock_for_dusk` | (sample 7) | Về bệ sạc nghỉ qua đêm | (sample 7) | Dock on the pad for the night |
| `next.v3.transmit` | Phát sóng từ cột ăng-ten. | Phát sóng từ cột ăng-ten | Transmit from the antenna mast. | Transmit from the antenna mast |
| `next.v3.visit_site` | (sample 8) | Đã có điện. Ghé trạm địa chấn hoặc khu nhà ở | (sample 8) | Power's on. Visit the seismic station or the habitat |
| `next.v3.await_night` | Căn cứ đã sẵn sàng cho đêm. | Căn cứ đã sẵn sàng qua đêm | The base is ready for the night. | The base is ready for night |
| `next.v3.go_relay` | (sample 4) | Trạm tiếp sóng 1 đang yếu dần. Tới sửa nó | (sample 4) | Relay 1 is fading. Go fix it |
| `next.v3.scan_elder06` | (sample 5) | Trạm 2 im lặng. Tìm 06 ở cửa hẻm | (sample 5) | Relay 2 went quiet. Find 06 at the canyon mouth |
| `next.v3.cut_headlamp_crate` | Cắt thùng hàng cạnh 06 để lấy linh kiện đèn pha. | Cắt thùng cạnh 06 lấy linh kiện đèn pha | Cut the crate by 06 for the headlamp's parts. | Cut the crate by 06 for headlamp parts |
| `next.v3.assemble_headlamp` | Lắp đèn pha SSR-3 ở giá cạnh bệ sạc. | Lắp đèn pha ở giá lắp | Assemble the SSR-3 headlamp at the rack by the pad. | Assemble the headlamp at the rack |
| `next.v3.dock_for_rack` | (sample 13) | Đỗ lên bệ sạc để mở giá lắp | (sample 13) | Dock on the pad to open the rack |
| `battery.low.lander` | Pin yếu. Xây bệ sạc cạnh tàu đổ bộ để sạc. | Pin yếu. Xây bệ sạc cạnh tàu đổ bộ | Battery low. Build a pad at the lander to charge. | Battery low. Build a pad by the lander |
| `battery.low.pad` | Pin yếu. Hãy về bệ sạc. | Pin yếu. Về bệ sạc | Battery low. Return to your pad. | Battery low. Head back to a pad |
| `ui.unlock.blueprint` | (sample 12) | Bản vẽ mới: {0} | Blueprint learned: {0} | New blueprint: {0} |
| `hint.openbuild` / `ui.verb.build` / `ui.face.build` | Xây dựng | Xây | Build | Build |
| `hint.build` | Lắp ráp | Lắp ráp | Assemble | Assemble |
| `hint.transmit` | Phát tín hiệu | Phát sóng | Transmit | Transmit |
| `hint.hold` | Khoang hàng | Khoang hàng | Inventory | Hold |
| `hint.deconstruct` | Tháo dỡ | Tháo | Dismantle | Dismantle |
| `ui.hint.drive` | Lái xe | Lái | Drive | Drive |
| `structure.solar_array.name` | Giàn pin mặt trời | Dàn pin mặt trời | Solar Array | Solar Array |
| `structure.battery_bank.name` | Cụm pin tích điện | Bộ tích điện | Battery Bank | Battery Bank |
| `structure.charging_pad.desc` | Bệ đỗ để sạc pin cho 07 khi có điện. | Đỗ lên để sạc pin. Cần nguồn điện | A warm dock pad that charges 07's battery when powered. | Park here to charge. Needs power |
| `structure.solar_array.desc` | Giàn tấm pin hướng nắng thấp, cấp điện cho các công trình quanh đó. | Lấy nắng, cấp điện cho bệ sạc và căn cứ | Panels tilted to the low sun that feed power into nearby structures. | Turns sunlight into power for the pad and base |
| `structure.battery_bank.desc` | Tích trữ điện dự phòng để căn cứ luôn sáng đèn qua bóng tối. | Trữ điện ban ngày, giữ đèn sáng qua đêm | Stores spare watt-hours so the base stays lit through shadow. | Stores daylight power to keep the base lit at night |
| `structure.antenna_mast.desc` | Truyền tín hiệu của trạm đi xa hơn qua vành miệng hố. | Để 07 phát sóng ra ngoài vành hố | Carries the station signal farther out across the crater rim. | Lets 07 transmit beyond the crater rim |
| `build.cost.short` | Chưa đủ phế liệu | Thiếu vật liệu | Not enough salvage | Not enough materials |
| `build.reason.overlaps` | Vướng công trình khác | Vướng chỗ | Something is in the way | Something's in the way |
| `build.reason.outside` | Ngoài tầm căn cứ | Quá xa căn cứ | Outside base reach | Too far from the base |
| `build.reason.blocked` | Cần dọn chỗ trước | Dọn chỗ trước | Clear the ground first | Clear the ground first |
| `build.reason.frame` | (sample 11) | Cần kéo mô-đun nguồn về đây trước | Tow the power module onto the apron first | Tow the power module here first |
| `build.step.place` | Lái đến vạch sáng trên sân rồi bấm {0} để đặt | Lái tới vạch sáng, bấm {0} để đặt | Drive to the glowing marks on the apron, then press {0} to place | Drive to the glowing marks, press {0} to place |
| `build.step.weld` | Giữ {0} để lắp ráp | Giữ {0} để lắp ráp | Hold {0} to assemble | Hold {0} to assemble |
| `build.cost.affordable` | Sẵn sàng đặt | Đủ vật liệu | Ready to place | Ready to place |
| `pickup.for.solar_array` | cho giàn pin mặt trời | cho dàn pin mặt trời | for the solar array | for the solar array |
| `pickup.for.battery_bank` | cho cụm pin tích điện | cho bộ tích điện | for the battery bank | for the battery bank |
| `pickup.part.igniter.job` | Lắp vào mỏ cắt khi đỗ trên bệ. | Lắp vào mỏ cắt | Fits the cutter on the pad. | Fits the cutter |
| `pickup.part.feedunit` | Bộ cấp nguồn trạm chuyển tiếp | Bộ cấp nguồn trạm tiếp sóng | Relay feed unit | Relay feed unit |
| `pickup.part.feedunit.job` | Lắp vào hộp cột của relay.1. | Lắp vào hộp cột Trạm 1 | Fits relay.1's mast box. | Fits Relay 1's mast box |
| `ui.device.cutter.fault` | Cần bộ đánh lửa. Lắp khi đỗ trên bệ sạc. | Thiếu bộ đánh lửa | Needs an igniter. Fit it while parked on the charging pad. | No igniter |
| `ui.device.state.cutter_ready` | Sẵn sàng lắp bộ đánh lửa | Lắp được bộ đánh lửa | Igniter ready to fit | Igniter ready to fit |
| `ui.cargo.igniter.fit_hint` | Kéo vào mỏ cắt, hoặc bấm E | Kéo vào mỏ cắt hoặc bấm E | Drag to the cutter, or press E | Drag to the cutter or press E |
| `ui.cargo.drag.nowhere` | (sample 15) | Chưa lắp được món này | Nothing on 07 takes this yet | Nothing takes this yet |
| `ui.cargo.drag.not_docked` | Chỉ lắp được khi đỗ trên bệ sạc | Chỉ lắp khi đỗ trên bệ sạc | Fits only while parked on the charging pad | Fits only on the pad |
| `rack.headlamp` | Đèn pha SSR-3 · từ đơn vị 06 | Đèn pha SSR-3 · từ 06 | SSR-3 headlamp · from unit 06 | SSR-3 headlamp · from 06 |
| `rack.cell_pack` | Khối pin · từ đơn vị 06 | Khối pin · từ 06 | Cell pack · from unit 06 | Cell pack · from 06 |
| `ui.face.ack_route` | ACK · RELAY.1 · TUYẾN · ĐĨA ĐỈNH NÚI · NGẮT | ACK · TRẠM 1 · TUYẾN TỚI CHẢO ĐỈNH · MẤT | ACK · RELAY.1 · ROUTE · PEAK DISH · OFFLINE | ACK · RELAY 1 · ROUTE TO PEAK DISH · DOWN |
| `ui.face.no_line` | BỊ CHE KHUẤT | KHÔNG THẤY ĐƯỜNG | NO LINE OF SIGHT | CAN'T SEE AHEAD |
| `ui.readout.power_off` | NGUỒN · NGOẠI TUYẾN | NGUỒN · TẮT | POWER · OFFLINE | POWER · OFF |
| `elder.06.manifest` | SSR-3 · ĐƠN VỊ 06 · GIỮ TRẠM TIẾP SÓNG · NHIỆM VỤ: GIỮ LIÊN LẠC RELAY.2 · DỪNG: PIN 0% | SSR-3 · XE 06 · GIỮ TRẠM 2 · PIN 0% · DỪNG | SSR-3 · UNIT 06 · RELAY KEEPER · TASK: HOLD RELAY.2 LINK · STOPPED: CELL 0% | SSR-3 · UNIT 06 · KEEPS RELAY 2 · CELL 0% · STOPPED |

**Out of scope here:** the databank cards and story notes, which use the story voice (`writing-style.md`) and are
reviewed separately. Material descriptions are shown later in the game.

## 5. Process
1. The owner approves §3.
2. Story & flow applies §4 to `vi.json` and `en.json`, plus the glossary to any other line it touches.
3. The bot's naive-gate word lists (`OpeningScenario.DeclareNextActions`) are updated in the same land, because they
   match words.
