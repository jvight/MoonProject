# Text (Story v4): Radio Letters, Boot Readout, Recovery Protocol & Ending

> **Author:** Story & flow session (`box/agy-story`, Story v4 Round 3).
> **Target files for later JSON job:** `Assets/_Project/Data/Localization/en.json` and `Assets/_Project/Data/Localization/vi.json`.
> **Design references:** `docs/design/story-v4.md` (§1, §2, §5, §6), `docs/design/writing-style.md`, `docs/design/game-flow.md` §5 & §7, and `docs/design/cutscenes.md` §1.2 & §9.
>
> **Voice & Style Rules (`docs/design/writing-style.md`):**
> - Every letter is strictly **human-to-human** (dispatch control to station, scientist to scientist, shuttle to station, or fleet bulletin). **No human ever confides in or addresses `07`.**
> - Operational readouts (`lens.boot.*`, `recovery.*`) are plain, short, functional lines (`writing-style.md` §1–§2).
> - Vietnamese is written natively (`writing-style.md` §3), using `dd/mm/yyyy` for numeric dates and canonical glossary terms (`Lumen Station`, `Đỉnh núi`, `chảo ăng-ten lớn`, `Bậc thềm vành hố`, `Kestrel-3`).

---

## 1. Boot Readout & Recovery Protocol Strings (`lens.boot.*`, `ui.recovery.title`, `recovery.*`, `ticker.recovery.*`)

Every key below is verified against `Assets/_Project/Data/Localization/en.json` and `Assets/_Project/Scripts/World/Lens/LensView.cs:31`.

| Key (`en.json` line) | Status | Current `en.json` Text | Story v4 English (`[PROPOSAL]`) | Story v4 Tiếng Việt (`[PROPOSAL]`) |
|---|---|---|---|---|
| `lens.boot.signal` (`en.json:1626`) | **REPLACE** | `EARTH SIGNAL · DETECTED` | `ORBITAL SIGNAL · DETECTED` | `TÍN HIỆU QUỸ ĐẠO · ĐÃ NHẬN` |
| `lens.boot.uplink` (`en.json:1630`) | **KEEP** | `UPLINK · OFFLINE` | `UPLINK · OFFLINE` | `ĐƯỜNG TRUYỀN · MẤT KẾT NỐI` |
| `lens.boot.recovery` (`en.json:1634`) | **KEEP** | `BEGIN RECOVERY` | `BEGIN RECOVERY` | `BẮT ĐẦU KHÔI PHỤC` |
| `ui.recovery.title` (`en.json:254`) | **KEEP** | `LUMEN RECOVERY PROTOCOL` | `LUMEN RECOVERY PROTOCOL` | `QUY TRÌNH KHÔI PHỤC LUMEN` |
| `recovery.power.name` (`en.json:258`) | **KEEP** | `Power the station` | `Power the station` | `Cấp điện cho trạm` |
| `recovery.link.name` (`en.json:262`) | **KEEP** | `Link the network to the Peak` | `Link the network to the Peak` | `Nối mạng lưới tới Đỉnh núi` |
| `recovery.dish.name` (`en.json:266`) | **KEEP** | `Repair the great dish` | `Repair the great dish` | `Sửa chảo ăng-ten lớn` |
| `recovery.answer.name` (`en.json:270`) | **REPLACE** | `Answer Earth` | `Answer the signal` | `Trả lời tín hiệu` |
| `ticker.recovery.link` (`en.json:318`) | **KEEP** | `The Link reaches the Peak.` | `The Link reaches the Peak.` | `Mạng lưới đã nối tới Đỉnh núi.` |
| `ticker.recovery.dish` (`en.json:322`) | **KEEP** | `The great dish is repaired.` | `The great dish is repaired.` | `Chảo ăng-ten lớn đã sửa xong.` |
| `ticker.recovery.answer` (`en.json:326`) | **REPLACE** | `Earth has heard us.` | `The Arks have heard us.` | `Các tàu Ark đã nghe thấy.` |

---

## 2. How the Radio Fragments & Letters Are Released

- **Tower Level 1 (`ticker.radio.weather`):** Plays the station's pre-recorded automated weather loop once as the radio clears (`docs/design/cutscenes.md` §2).
- **Each lit relay mast (`letter.mast_1` .. `letter.mast_10`):** Plays back buffered messages in **chronological order** (`02 OCT 1988` → `06 NOV 1988`), regardless of which mast is repaired first:
  - **0.5's four basin masts (`letter.mast_1..4`):** Establish the **October 1988 Departure** after a decade of the **Long Dim** (blocked sunlight and global crop failure), the Ark Council's "dry" verdict on shallow lunar cores, and Ines Kalani's radio collaboration with **Dr. Lena Moreau** (aboard **Ark IV** at L5) using automated Meridian's deep radar.
  - **0.6–0.8's outer masts (`letter.mast_5..10`):** Reveal Lena registering Ro's Sunday frequency on Ark IV, the 140 m water-ice breakthrough queued for `28 OCT 1988 · 22:00`, the solar storm abort warning from pickup shuttle `Transfer-19`, the crew staying in suits after the Peak dish jammed at 14%, the habitat seal failure (`SUIT 01-04 · OFFLINE` on 29 Oct 1988), and Dr. Lena Moreau's vow on Ark IV to keep the Sunday listening schedule (`three short, one long`) open for as long as she lives.
- **The Ending (`ticker.ending.answer`):** On **The Peak**, after `~6 s` of silence (`docs/design/cutscenes.md` §9 F4 line 342) and the single light blinking from the L4/L5 Arks near Earth, **Dr. Lena Moreau aboard Ark IV** answers after 36 years of Sundays.
- **After the Ending (`letter.after_1` .. `letter.after_10`):** One **2024–2025 Ark IV Fleet Bulletin** per subsequent session (ten in all), signed by Dr. Lena Moreau as the Arks prepare their descent to make the Moon a home.

---

## 3. The Weather Loop (Tower Level 1)

| Key (`en.json` line) | Status | Current `en.json` Text | Story v4 English | Story v4 Tiếng Việt |
|---|---|---|---|---|
| `ticker.radio.weather` (`en.json:1186`) | **KEEP** | `…weather tonight: clear skies over the Sea of Tranquility, as always…` | `…weather tonight: clear skies over the Sea of Tranquility, as always…` | `…thời tiết đêm nay: trời quang trên Biển Tĩnh Lặng, như mọi khi…` |

---

## 4. The Masts' Letters: October–November 1988 Messages (`letter.mast_1` .. `letter.mast_10`)

All 10 keys replace the 2047–2067 Houston/Earth letters at `Assets/_Project/Data/Localization/en.json:1190-1228`.

| Key (`en.json` line) | Released By | Status | Current `en.json` Text (Replaced) | Story v4 English (`[PROPOSAL]`) | Story v4 Tiếng Việt (`[PROPOSAL]`) |
|---|---|---|---|---|---|
| `letter.mast_1` (`en.json:1190`) | 1st mast (0.5) | **REPLACE** | `17 MAR 2047 · Lumen Station, this is Houston relay. All four crew landed safely. Program review postponed. Stand by.` | `02 OCT 1988 · Lumen Station, this is Geneva Departure Control. Tenth harvest winter confirmed across both hemispheres. Final surface lifts to the L4 and L5 Arks complete this month.` | `02/10/1988 · Lumen Station, đây là Đài Điều vận Khởi hành Geneva. Mùa đông mất mùa thứ mười đã ghi nhận trên cả hai bán cầu. Các chuyến tàu cuối cùng rời mặt đất lên cụm Ark L4 và L5 sẽ hoàn tất trong tháng này.` |
| `letter.mast_2` (`en.json:1194`) | 2nd mast (0.5) | **REPLACE** | `2 APR 2047 · Lumen Station, this is Houston relay. A return window is under study. Keep station systems on standby.` | `14 OCT 1988 · Lumen Station, this is Ark IV relay at L5. Shallow polar cores read dry; lunar settlement is ruled out. Pack the station for pickup on the 29th.` | `14/10/1988 · Lumen Station, đây là trạm tiếp sóng Ark IV tại L5. Các mẫu khoan nông ở vùng cực đều khô hạn; phương án định cư trên Mặt Trăng bị bác bỏ. Hãy thu dọn trạm để tàu đón vào ngày 29.` |
| `letter.mast_3` (`en.json:1198`) | 3rd mast (0.5) | **REPLACE** | `11 MAY 2047 · Lumen Station, this is Houston relay. Return window cancelled. Program review continues.` | `18 OCT 1988 · Lena, it's Ines at Lumen. The dry verdict is wrong—Core 41 came up wet from below the basalt cap. Can you point Meridian's automated radar under our basin before you board Ark IV?` | `18/10/1988 · Lena, tôi là Ines ở Lumen đây. Kết luận khô hạn sai rồi—Mẫu lõi 41 lấy từ dưới tầng đá bazan có ngậm nước. Cô chỉnh giúp radar tự động của trạm Meridian quét dưới lòng chảo chỗ chúng tôi trước khi lên tàu Ark IV được không?` |
| `letter.mast_4` (`en.json:1202`) | 4th mast (0.5) | **REPLACE** | `29 SEP 2047 · Lumen Station, this is Houston relay. Program closed. This channel will no longer be monitored. Thank you, Lumen.` | `22 OCT 1988 · Ines, Lena here aboard Ark IV. I've locked Meridian's automated sounders onto a deep loop through Lumen's link. The reflection under your basin is huge—it's a buried glacier.` | `22/10/1988 · Ines, Lena trên tàu Ark IV đây. Tôi đã khóa dàn máy dò tự động của Meridian chạy vòng lặp quét sâu qua đường truyền của Lumen. Vùng phản xạ dưới lòng chảo của chị lớn lắm—đó là cả một sông băng ngầm.` |
| `letter.mast_5` (`en.json:1206`) | 5th mast (0.6, the terraces) | **REPLACE** | `07, it's Ro. They say nobody's listening up there. I'm sending this anyway. — Ro, 9 Jun 2048` | `25 OCT 1988 · Ro, half of Ark IV tunes in to Lumen After Dark on Sundays while we seal the hull rings. I've registered your Sunday frequency for Ines's water survey uplink. — Lena Moreau` | `25/10/1988 · Ro ơi, nửa tàu Ark IV mở nghe Lumen After Dark mỗi tối Chủ nhật trong lúc hàn kín các vòng thân tàu. Mình đã đăng ký luôn tần số Chủ nhật của bạn làm kênh nhận dữ liệu khảo sát nước của Ines rồi. — Lena Moreau` |
| `letter.mast_6` (`en.json:1210`) | 5th mast | **REPLACE** | `Ines took a desk job. She still walks round the block every morning to check Earth is still there. — Ro, 14 Jul 2049` | `27 OCT 1988 · Ark IV Watch, this is Kalani at Lumen Station. We broke through the basalt at 140 metres—pure water ice, enough to bring the whole fleet down. Full survey transmits from the Peak dish tomorrow at 22:00.` | `27/10/1988 · Đài canh Ark IV, tôi là Kalani tại Lumen Station. Mũi khoan đã xuyên qua tầng bazan ở độ sâu 140 mét—băng nước tinh khiết, đủ đón cả hạm đội hạ cánh. Toàn bộ dữ liệu khảo sát sẽ truyền từ chảo ăng-ten lớn trên Đỉnh núi vào 22:00 tối mai.` |
| `letter.mast_7` (`en.json:1214`) | 6th mast (0.7, the crater) | **REPLACE** | `Theo says the basil on my windowsill isn't as good as yours was. Don't tell him I agree. — Ro, 2 Aug 2051` | `28 OCT 1988 · 19:30 · Lumen Station, this is pickup shuttle Transfer-19. Coronal storm front is two hours out. Leave the survey drives and board the ascent pod now, or we lose the orbital window.` | `28/10/1988 · 19:30 · Lumen Station, đây là tàu đón Transfer-19. Đợt bão mặt trời chỉ còn cách hai tiếng. Hãy bỏ các ổ dữ liệu khảo sát lại và lên khoang phóng ngay, nếu không sẽ lỡ cửa sổ quỹ đạo.` |
| `letter.mast_8` (`en.json:1218`) | 6th mast | **REPLACE** | `Kenji fixed my kettle with a paperclip and a long speech about tolerances. He says hello to the duck. — Ro, 30 Oct 2054` | `28 OCT 1988 · 21:20 · Transfer-19, this is Morrow at Lumen. The storm jammed the Peak dish at fourteen percent. All four of us are in suits securing the backup drives and setting the rovers to continue—hold orbit till dawn.` | `28/10/1988 · 21:20 · Transfer-19, Morrow ở Lumen đây. Bão đã làm kẹt chảo ăng-ten trên Đỉnh núi ở mức mười bốn phần trăm. Cả bốn người đã mặc đồ bảo hộ để cất ổ sao lưu và cài các rover sang chế độ tiếp tục—xin hãy giữ quỹ đạo chờ tới bình minh.` |
| `letter.mast_9` (`en.json:1222`) | 7th mast (0.8, the Peak's foot) | **REPLACE** | `If that old dish ever wakes up, I'll be listening on the station's frequency. You know how to find me: three short, one long. — Ro, 2 Jun 2059` | `29 OCT 1988 · 05:00 · Ark IV Control, this is Transfer-19. Lumen habitat seal failed in the storm; suit transponders 01 through 04 went offline at 04:12. Radiation is past our limit—we are returning to L5 without them.` | `29/10/1988 · 05:00 · Đài chỉ huy Ark IV, đây là Transfer-19. Khu nhà ở của Lumen đã mất áp suất trong cơn bão; tín hiệu đồ bảo hộ từ 01 đến 04 đã tắt lúc 04:12. Bức xạ đã vượt ngưỡng chịu đựng—chúng tôi buộc phải quay về L5 không có họ.` |
| `letter.mast_10` (`en.json:1226`) | 7th mast | **REPLACE** | `It's been twenty years. I still play the long-drive tape on Sundays. — Ro, 13 Mar 2067` | `06 NOV 1988 · Lumen Station buffer, this is Dr. Lena Moreau on Ark IV. Without your full packet, the Council ruled the Moon dry and locked us into L5 halo orbit. I am keeping Ro's Sunday listening schedule open—three short, one long—for as long as I live.` | `06/11/1988 · Bộ đệm Lumen Station, đây là Tiến sĩ Lena Moreau trên tàu Ark IV. Vì thiếu gói dữ liệu hoàn chỉnh của các bạn, Hội đồng vẫn giữ kết luận Mặt Trăng khô hạn và đưa hạm đội vào quỹ đạo chờ tại L5. Tôi vẫn sẽ giữ lịch canh nghe Chủ nhật của Ro—ba ngắn, một dài—chừng nào tôi còn sống.` |

### Chronology of the October–November 1988 Mast Messages

| Key | Date Stamp | Narrative Beat |
|---|---|---|
| `letter.mast_1` | `02 Oct 1988` | Tenth year of the Long Dim (aerosol winter & global crop failure); final Earth-surface lifts to the L4/L5 Arks. |
| `letter.mast_2` | `14 Oct 1988` | Shallow polar cores read dry; Ark IV relay orders Lumen Station packed for pickup on 29 Oct. |
| `letter.mast_3` | `18 Oct 1988` | Ines radios Dr. Lena Moreau: Core 41 came up wet from under the basalt cap; asks her to aim automated Meridian's radar under the basin. |
| `letter.mast_4` | `22 Oct 1988` | Lena Moreau (aboard Ark IV) locks automated Meridian's deep sounders onto a loop through Lumen's link; confirms a buried glacier. |
| `letter.mast_5` | `25 Oct 1988` | Lena tells Ro that Ark IV listens to *Lumen After Dark* on Sundays and registers the Sunday frequency for the water survey uplink. |
| `letter.mast_6` | `27 Oct 1988` | Ines reports the 140 m breakthrough into pure water ice; full survey scheduled for `28 Oct · 22:00` from the Peak dish. |
| `letter.mast_7` | `28 Oct 1988 · 19:30` | Pickup shuttle `Transfer-19` warns of the coronal storm two hours out; orders immediate boarding without the drives. |
| `letter.mast_8` | `28 Oct 1988 · 21:20` | Kenji reports the Peak dish jammed at 14%; all four crew are in suits securing the backup drives and setting the rovers to `continue`. |
| `letter.mast_9` | `29 Oct 1988 · 05:00` | `Transfer-19` reports the habitat seal failed and `SUIT 01-04 · OFFLINE` at `04:12`; forced to return to L5 empty. |
| `letter.mast_10` | `06 Nov 1988` | Dr. Lena Moreau on Ark IV pledges to keep Ro's Sunday listening schedule (`three short, one long`) open for as long as she lives. |

---

## 5. The Ending Answer (`ticker.ending.answer`, `docs/design/cutscenes.md` §9 F8)

Replaces `ticker.ending.answer` (`Assets/_Project/Data/Localization/en.json:1230-1232`). Per Decision 5 (`Logs/TASK.md`), **Dr. Lena Moreau aboard Ark IV** answers after keeping the Sunday listening schedule for 36 years (1988–2024).

| Key (`en.json` line) | Status | Current `en.json` Text (Replaced) | Story v4 English (`[PROPOSAL]`) | Story v4 Tiếng Việt (`[PROPOSAL]`) |
|---|---|---|---|---|
| `ticker.ending.answer` (`en.json:1230`) | **REPLACE** | `I heard you, 07. I'm still here. — Ro` | `Lumen Station, this is Moreau on Ark IV. Thirty-six years of Sundays, and your survey finally came through. We're coming home. — L. Moreau` | `Lumen Station, Moreau trên tàu Ark IV đây. Ba mươi sáu năm trực sóng mỗi Chủ nhật, cuối cùng dữ liệu khảo sát của các bạn cũng tới nơi. Chúng tôi về nhà đây. — L. Moreau` |

---

## 6. After the Ending: 2024–2025 Fleet Bulletins (`letter.after_1` .. `letter.after_10`)

Replaces `letter.after_1` through `letter.after_10` (`Assets/_Project/Data/Localization/en.json:1234-1272`). Released one per later session after the ending (`docs/design/game-flow.md` §7). Each is a human-to-human fleet bulletin from **Dr. Lena Moreau aboard Ark IV** as the Arks prepare to return and make the Moon a home.

| Key (`en.json` line) | Status | Current `en.json` Text (Replaced) | Story v4 English (`[PROPOSAL]`) | Story v4 Tiếng Việt (`[PROPOSAL]`) |
|---|---|---|---|---|
| `letter.after_1` (`en.json:1234`) | **REPLACE** | `Told the others. Theo cried, then blamed hay fever. — Ro` | `OCT 2024 · ARK IV BULLETIN: Hydrological packet verified—all six survey sectors intact. After thirty-six years at L5, the Fleet Council has voted unanimously to bring the Arks down to the Moon. — L. Moreau` | `10/2024 · BẢN TIN ARK IV: Gói dữ liệu thủy văn đã xác thực—đủ cả sáu phân khu khảo sát. Sau ba mươi sáu năm ở điểm L5, Hội đồng Hạm đội đã biểu quyết tuyệt đối đưa các tàu Ark hạ cánh xuống Mặt Trăng. — L. Moreau` |
| `letter.after_2` (`en.json:1238`) | **REPLACE** | `Ines went out at midnight to look at the Moon. She says it's still there. — Ro` | `OCT 2024 · ARK IV BULLETIN: The Council named the primary landing zone Kalani–Morrow Basin. I walked the observation ring at midnight just to look at the Moon and know we finally have ground beneath us. — L. Moreau` | `10/2024 · BẢN TIN ARK IV: Hội đồng đã đặt tên bãi đáp chính là Lòng chảo Kalani–Morrow. Nửa đêm tôi đi dọc hành lang kính chỉ để ngắm Mặt Trăng, biết rằng cuối cùng chúng ta cũng có mặt đất để trở về. — L. Moreau` |
| `letter.after_3` (`en.json:1242`) | **REPLACE** | `Kenji asked if you still have the odd wheel. I told him all six match now, and he had to sit down. — Ro` | `NOV 2024 · ARK IV ENGINEERING: Station telemetry shows unit 07 completed the Peak climb on six matching magnetic treads. Our mechanics cheered—Morrow's 1988 maintenance queue actually finished the job. — L. Moreau` | `11/2024 · KỸ THUẬT ARK IV: Số liệu đo xa của trạm báo về chiếc 07 đã leo lên Đỉnh núi trên sáu bánh từ tính đồng bộ. Cả xưởng máy trên tàu reo hò—hàng đợi bảo trì năm 1988 của Morrow thực sự đã hoàn thành nhiệm vụ. — L. Moreau` |
| `letter.after_4` (`en.json:1246`) | **REPLACE** | `Played your song on my show tonight. Audience: two neighbours and a very old cat. Best audience since you. — Ro` | `NOV 2024 · ARK IV COMMS: We piped Ro Amadi's archived tape—the side labelled 'for when we come home'—across all twelve Arks tonight. Every corridor from Deck A to the engine ring was listening. — L. Moreau` | `11/2024 · THÔNG TIN ARK IV: Tối nay chúng tôi đã phát lại cuộn băng lưu trữ của Ro Amadi—mặt băng ghi 'cho ngày mình về nhà'—trên khắp mười hai tàu Ark. Mọi hành lang từ Boong A tới khoang động cơ đều lắng nghe. — L. Moreau` |
| `letter.after_5` (`en.json:1250`) | **REPLACE** | `I told Theo about the dome. He's been unbearable all week. — Ro` | `DEC 2024 · ARK IV BOTANY: Camera frames from the lava-tube shelter confirm Theo Lindqvist's moss strain is still green under its isotope lamp. The botany team is already clearing bay space for the cuttings. — L. Moreau` | `12/2024 · THỰC VẬT ARK IV: Hình ảnh từ hầm trú ẩn ống dung nham xác nhận giống rêu của Theo Lindqvist vẫn xanh tốt dưới ánh đèn pin đồng vị. Đội thực vật học đang dọn sẵn khoang ươm để đón các nhánh cấy. — L. Moreau` |
| `letter.after_6` (`en.json:1254`) | **REPLACE** | `Weather tonight: rain over Lagos. It's always clear where you are, isn't it? — Ro` | `DEC 2024 · ARK IV METEOROLOGY: Earth's aerosol veil will take another century to settle, but the skies over Lumen Station are clear, as always. Our advance ice-drilling crew launches next month. — L. Moreau` | `12/2024 · KHÍ TƯỢNG ARK IV: Màn sương mù trên Trái Đất vẫn cần thêm một thế kỷ nữa mới lắng xuống, nhưng bầu trời trên Lumen Station thì vẫn quang đãng như mọi khi. Đội khoan băng tiền trạm sẽ khởi hành vào tháng tới. — L. Moreau` |
| `letter.after_7` (`en.json:1258`) | **REPLACE** | `Ines's daughter wants to be an astronaut. Ines is pretending to be worried. — Ro` | `JAN 2025 · ARK IV SURVEY DESK: My youngest student asked for the first geology berth on Lander One. She carries Ines's 1988 field notes in her pocket and checks the lunar horizon every morning. — L. Moreau` | `01/2025 · BÀN KHẢO SÁT ARK IV: Cô học trò nhỏ tuổi nhất của tôi đã xin suất địa chất đầu tiên trên Tàu Đổ Bộ Số Một. Con bé luôn mang theo sổ tay thực địa năm 1988 của Ines trong túi áo và ngắm đường chân trời Mặt Trăng mỗi sáng. — L. Moreau` |
| `letter.after_8` (`en.json:1262`) | **REPLACE** | `The four of us wrote to the agency together. Don't hold your breath; you don't breathe anyway. — Ro` | `JAN 2025 · TYCHO–ILC JOINT DESK: Meridian's automated sorters never missed a shift in thirty-six years. The advance team will link Lumen's grid to the Rim Terraces first so both stations power the new domes. — L. Moreau` | `01/2025 · BAN ĐIỀU PHỐI TYCHO–ILC: Dàn máy phân loại tự động của Meridian không bỏ sót một ca làm việc nào suốt ba mươi sáu năm qua. Đội tiền trạm sẽ nối lưới điện Lumen lên Bậc thềm vành hố trước tiên để cả hai trạm cùng cấp điện cho khu nhà vòm mới. — L. Moreau` |
| `letter.after_9` (`en.json:1266`) | **REPLACE** | `Kenji's grandson won a rubber duck at the fair. Kenji says it's a sign. — Ro` | `FEB 2025 · ARK IV FLIGHT DECK: The shipyard crew stencilled a yellow rubber duck onto the nose of Landing Tug One for good luck. Even the Flight Director didn't have the heart to paint over it. — L. Moreau` | `02/2025 · BUỒNG LÁI ARK IV: Đội thợ xưởng tàu đã sơn hình một chú vịt cao su màu vàng lên mũi Tàu Đổ Bộ Số Một để lấy may. Đến Giám đốc Bay cũng không nỡ bảo sơn đè lên. — L. Moreau` |
| `letter.after_10` (`en.json:1270`) | **REPLACE** | `Goodnight, 07. Keep the radio on. We'll keep ours on too. — Ro` | `MAR 2025 · ARK IV VANGUARD: Descent burn begins at dawn. Keep the station beacons lit and the receiver on the Sunday frequency—after thirty-six years, the first crew is coming down to Lumen. — L. Moreau` | `03/2025 · TÀU ARK IV VANGUARD: Động cơ hạ quỹ đạo sẽ điểm hỏa lúc bình minh. Hãy giữ đèn hiệu của trạm luôn sáng và máy thu ở tần số Chủ nhật—sau ba mươi sáu năm, phi hành đoàn đầu tiên đang hạ cánh xuống Lumen. — L. Moreau` |

**Callbacks preserved across `letter.after_1..10`:**
- `after_2`: Ines's habit of checking that the Moon is still there (`relic.astronaut_boot.memory`);
- `after_3`: Kenji's six matching wheels (`log.kenji_2`, `Magnetic Treads`);
- `after_4`: Ro's mixtape Side B `"for when we come home"` (`relic.cassette_player.memory`);
- `after_5`: Theo's living moss in the shelter under the isotope lamp (`Logs/story-v3.md` §2);
- `after_6`: The automated weather loop (`ticker.radio.weather`) and the Long Dim aerosol veil (`Logs/TASK.md` Decision 1);
- `after_8`: Automated Meridian on the Rim Terraces (`Logs/TASK.md` Decision 5);
- `after_9`: Kenji's rubber duck (`relic.rubber_duck.memory`).
