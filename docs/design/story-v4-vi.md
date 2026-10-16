# Lunarion — Cốt Truyện v4 (Bản Tóm Tắt Tiếng Việt)

> **Bản thiết kế chi tiết đầy đủ (tiếng Anh):** `docs/design/story-v4.md`
>
> **Quy ước ghi chú:** Các chi tiết đã có sẵn trong mã nguồn hoặc tài liệu gốc được dẫn nguồn chính xác tới tệp, khóa ngôn ngữ hoặc số dòng. Mọi ý tưởng mới của Story v4 (hoặc kế thừa từ bản nháp `Logs/story-v3.md` / `0.6`) đều được gắn nhãn **`[ĐỀ XUẤT]`** (`[PROPOSAL]`).
>
> **Tinh thần chủ đạo:** Giữ phong cách retro-future thập niên 1980 (màn hình CRT hổ phách `Assets/_Project/Shaders/R1/R1CrtFace.shader:1,7`, băng cassette `docs/STORY.md:20-25`, máy móc analog `docs/DESIGN.md:71-86`) và tiền đề *"con người đã phải rời bỏ Trái Đất"* như *Fallout*, nhưng **tuyệt đối không** có bạo lực, máu me, quái vật hay châm biếm đen tối (`docs/VISION.md` "The Hook", "Tone & Influences"). Thế giới của *Lunarion* vẫn yên bình, ấm áp, cô đơn nhưng đầy hy vọng. Mọi chi tiết đều tuân thủ logic thực tế: con người chỉ giao tiếp với máy móc qua giao thức vận hành và ghi chú bảo trì giữa các thành viên phi hành đoàn, không tâm sự ủy mị với robot (`Logs/story-v3.md` §4).

---

## 1. Bối cảnh Thế giới & Dòng thời gian (Thập niên 1970 – 2024, `[ĐỀ XUẤT]`)

- **1978–1982 — Bầu Trời Tĩnh Lặng (`The Quiet Sky`, `[ĐỀ XUẤT]`):** Một biến cố khí quyển toàn cầu *(nguyên nhân cụ thể chờ quyết định ở Câu hỏi 1: chuỗi siêu phun trào núi lửa địa nhiệt năm 1979 hoặc sự cố aerosol công nghiệp)* phủ một lớp sương mù tầng bình lưu dày đặc quanh Trái Đất, làm giảm 18% ánh sáng mặt trời, phá vỡ chu kỳ gió mùa và khiến mùa màng toàn cầu mất trắng nhiều thập kỷ.
- **1980–1987 — Chương trình Tàu Cứu Sinh (`The Arks`) & Lý do công nghệ dừng ở thập niên 1980 (`[ĐỀ XUẤT]`):** Toàn bộ nhà máy trên Trái Đất ngừng sản xuất đồ tiêu dùng để dồn tài nguyên đóng **12 Tàu Cứu Sinh (`Ark I – Ark XII`)** — các trạm không gian hình trụ khổng lồ quay tạo trọng lực tại điểm Lagrange **L4/L5** giữa Trái Đất và Mặt Trăng, nơi nhân loại tạm cư trong 1–2 thế kỷ chờ khí quyển Trái Đất tự phục hồi. Vì mọi nguồn lực dồn vào vỏ tàu và hệ thống sinh tồn, công nghệ máy tính đóng băng ở chuẩn thập niên 1980: màn hình CRT hổ phách (`#FFB547`, `docs/VISION.md:205`), băng từ cassette, rơ-le cơ học bền bỉ trước bức xạ.
- **1983–1988 — Trạm Lumen & Cuộc Sơ Tán:**
  - **Trạm Lumen** nằm trong một miệng hố yên tĩnh ở mặt gần Mặt Trăng (`docs/STORY.md:8`), tại vị trí gần cực (`docs/VISION.md:43`; `docs/design/0.6-living-moon.md` §1a.1 dòng 80, 100, quyết định L1). Trong cốt truyện v4 (`[ĐỀ XUẤT]`), Lumen đóng vai trò trạm khảo sát băng ngầm, thử nghiệm hạt giống và tiếp vận thông tin cho hành lang phóng lên các tàu Ark. Ở vùng kế bên là **trạm khai thác băng tự động không người lái Meridian** của Liên đoàn Tycho (sơn màu trắng – xanh ngọc `[ĐỀ XUẤT]`, đóng cửa năm 1986 sau khi cạn mạch băng chính; `docs/design/0.6-world-regions.md` §2 Region 3; `Logs/story-v3.md` §1).
  - Bốn thành viên phi hành đoàn tại Lumen (`docs/STORY.md:20-45`): **Ines Kalani** (Trưởng nhóm / Địa chất), **Kenji Morrow** (Kỹ sư), **Ro Amadi** (Vô tuyến & DJ chương trình *Lumen After Dark*), và **Theo Lindqvist** (Thực vật học). Sau khi sửa chiếc xe khảo sát thứ sáu là **06** (`micro.06.name`, `Assets/_Project/Data/Localization/en.json:1938-1940`; `docs/design/0.5.5-owner-feedback.md` §1.7 dòng 145–147), Kenji sửa chiếc xe khảo sát cũ — món thứ 7 trong danh sách tuần đó nên đặt tên là **07** (`docs/STORY.md:9`; `log.kenji_1`, `en.json:1142-1144`).
  - **Tháng 10/1988 (`[ĐỀ XUẤT]`):** Trong đợt sơ tán cuối cùng rời bề mặt Trái Đất, một **cơn bão mặt trời cực mạnh** (`docs/STORY.md:10`) ập tới. Đài chỉ huy trên **Tàu Ark IV (*Vanguard*)** (`[ĐỀ XUẤT]`) lệnh cho cả 4 người phải lên tàu con thoi buổi sáng (`log.ines_2`, `en.json:1158-1160`), buộc họ phải để lại dữ liệu **Mẫu lõi 41 (`Core 41` — viên sỏi xanh / bằng chứng mạch nước ngầm Mặt Trăng của Ines, `log.ines_1`, `en.json:1146-1148`; `map.site.drill.note`, `en.json:1338-1340`; `[ĐỀ XUẤT]`)**, **Khay ươm số 4** của Theo (`log.theo_1`, `log.theo_3`, `en.json:1150-1152, 1178-1180`) và **Đĩa Vàng Lumen** của Ro (`relic.golden_record.name` / `relic.golden_record.memory`: *"The Lumen Record"* — đĩa vàng tự thu âm của Ro, không phải đĩa Voyager; `en.json:574-580`, `docs/STORY.md:23, 53`). Kenji để **07** vẫn bật trong khoang tàu đổ bộ (`log.kenji_3`, `en.json:1182-1184`; `docs/design/north-star.md` §4a), còn **06** nằm lại cạnh chòi bảo trì trạm tiếp sóng miệng hẻm `relay.2` (`docs/design/0.5.5-owner-feedback.md` §1.7 dòng 138, 153).
- **2024 (36 năm sau, `[ĐỀ XUẤT]`):** Tín hiệu quét định kỳ từ **Tàu Ark IV** chạm tới mạng lưới tiếp sóng trong lòng chảo, đánh thức **07** trong khoang tàu đổ bộ giữa lúc nhật thực Trái Đất (`docs/design/north-star.md` §4a; `Logs/r1/TASK.md` "Keep").

---

## 2. 07 là ai, các robot 01–06 là gì, và Mục tiêu của người chơi

- **Bản chất của 07:** Một xe tự hành đa dụng 6 bánh (`docs/VISION.md` "07", dòng 55–83; `docs/ARCHITECTURE.md:129`) chạy vi xử lý thập niên 1980. 07 không phải con người trong vỏ máy, nhưng có cảm biến dò sóng âm D-major (nên tự động tiến lại gần loa nhạc của Ro, `docs/VISION.md:87`; `log.ro_1`, `en.json:746-748`) và ăng-ten nháy mã hiệu của trạm (`ba ngắn, một dài`, `log.kenji_1`, `en.json:1142-1144`).
- **Các robot tiền nhiệm 01–06 (`docs/design/0.5.5-owner-feedback.md` §1.7; `docs/design/0.6-living-moon.md` §1a.7):**
  - **Phân biệt bản hiện tại và bản 0.6+:** Trong bản game hiện tại, **chỉ có `06` đã tồn tại** (`micro.06.name`: `"06"` tại `Assets/_Project/Data/Localization/en.json:1938-1940` và `vi.json:1938-1940`, cùng các robot công trường `micro.mule.name`, `micro.haul_sled.name`, `micro.drone.name` tại `en.json:1942-1952`; `docs/design/0.5.5-owner-feedback.md` §1.7 dòng 135–156). Năm robot **`01–05` chưa có trong bản hiện tại** mà được lên kế hoạch cho **bản `0.6+`** khi mở các vùng ngoài (`0.5.5-owner-feedback.md` §1.7 dòng 168; `0.6-living-moon.md` §1a.7 dòng 279–282); vai trò chi tiết của `01–05` dưới đây là **`[ĐỀ XUẤT]`** (từ `Logs/story-v3.md` §2):
    - **01 (Vành Miệng Hố / Rim — `0.6+ [ĐỀ XUẤT]`):** Xe đào rãnh đời đầu (1973) sơn cam, pin hóa thủy tinh sau đêm Mặt Trăng, được giữ làm cột mốc trắc địa.
    - **02 (Đồng Bằng Mare — `0.6+ [ĐỀ XUẤT]`):** Xe tời cáp (1976) sụp trục bánh ở miệng ống dung nham, được Kenji tháo cụm thấu kính dự phòng.
    - **03 (Bậc Thềm Vành Hố / Rim Terraces — `0.6+ [ĐỀ XUẤT]`):** Xe chở mẫu lõi địa chất (1981) bị kẹt trong sạt lở đá, trên lưng vẫn giữ ống mẫu của Ines.
    - **04 (Hố Tối Vĩnh Cửu / Shadowed Crater — `0.6+ [ĐỀ XUẤT]`):** Xe thăm dò băng lạnh (1984) cháy cuộn sưởi khi cách ranh giới ánh sáng 40 m.
    - **05 (Hẻm Núi Meridian / Canyon — `0.6+ [ĐỀ XUẤT]`):** Xe nối cáp (1985) bị giàn ống của trạm tự động Meridian sập đè khi đóng trạm năm 1986.
    - **06 (Chòi bảo trì Trạm miệng hẻm `relay.2` — **Đã có trong game**, `en.json:1938-1940`):** Món đồ thứ sáu phi hành đoàn sửa (`0.5.5-owner-feedback.md` §1.7 dòng 145–147), nằm gục cạnh chòi bảo trì `relay.2` với ăng-ten vẫn hướng về nhà, giữ 3 mảnh bản vẽ **Bộ pin II (`Cell Pack II`, `upgrade.rover.cell_pack`, `en.json:1646-1656`)**.
  - *(Lưu ý: Bốn "Người bạn" trong game — **Tilly**, drone khảo sát nhỏ của Ines ở đáy lòng chảo (`friend.tilly.name` / `friend.tilly.repair_log`, `en.json:730-736`; `docs/STORY.md:36`; `docs/DESIGN.md:65`); **Moss**, robot làm vườn của Theo ở Hố Tối (`docs/STORY.md:29`; `docs/DESIGN.md:66`); **Bell**, tủ đài radio bốn chân của Ro ở cuối hẻm núi (`friend.bell.name` / `friend.bell.repair_log`, `en.json:738-744`; `docs/STORY.md:22`; `docs/DESIGN.md:67`); và **Atlas**, cần cẩu bốc dỡ hiền lành của Kenji trên Bậc thềm vành hố (`docs/STORY.md:43`; `docs/DESIGN.md:68`; `map.region.terraces.note`, `en.json:1458-1460`) — là các máy móc đồng hành chuyên biệt, tách biệt với dòng xe R1 `01–07`.)*
- **Mục tiêu của người chơi (`QUY TRÌNH KHÔI PHỤC LUMEN` / `LUMEN RECOVERY PROTOCOL`, `ui.recovery.title` tại `en.json:254-256`, `vi.json:254-256`; `docs/design/gdd-v3.md` §8):**
  1. `Cấp điện cho trạm` (`recovery.power.name`: `"Power the station"`, `en.json:258-260` — nâng 3 cấp Tháp Radio bằng `pickup.part_towercoil`, `pickup.part_transformer`, `pickup.part_relayspare` tại `en.json:130-140, 506-544`).
  2. `Nối mạng lưới tới Đỉnh núi` (`recovery.link.name`: `"Link the network to the Peak"`, `en.json:262-264`, qua chuỗi `hop.node.relay.0..3` tại `en.json:882-896` và các trạm vùng ngoài tới chân Đỉnh núi).
  3. `Sửa chảo ăng-ten lớn` (`recovery.dish.name`: `"Repair the great dish"`, `en.json:266-268` — dùng Siêu dẫn `material.superconductor.job` tại `en.json:1758-1760` và cần cẩu Atlas, cộng thêm `[ĐỀ XUẤT]` gỡ kẹt trục nâng 14%).
  4. `Trả lời Trái Đất` (`recovery.answer.name`: `"Answer Earth"`, `en.json:270-272` — hoặc `[ĐỀ XUẤT]` `"Trả lời tín hiệu"` nếu chọn phương án Ark IV ở Câu hỏi 3) từ **Đỉnh Núi (`The Peak`, `map.region.peak`, `en.json:1446-1452`)**, gửi toàn bộ dữ liệu **Mẫu lõi 41**, hình ảnh mầm cây trong **Khay số 4**, bài nhạc mặt B cuộn băng của Ro (`relic.cassette_player.memory`, `en.json:562-564`) và **Đĩa Vàng Lumen** (`relic.golden_record.memory`, `en.json:578-580`) (`[ĐỀ XUẤT]`).

---

## 3. Phân cảnh Mở đầu (`0:00 – 2:30`)

1. **Hình ảnh đầu tiên (`docs/design/cutscenes.md` §1.2 O1–O4; `docs/design/north-star.md` §4a):** 07 mở mắt trong khoang tàu đổ bộ (`place.lander.name`, `en.json:354-356`) giữa bóng tối của **nhật thực Trái Đất**. Phía trên rặng núi là quả địa cầu tối đen viền hào quang đồng đỏ (`SkyCycleSettings._eclipseRingColor` và `_eclipseRingStrength = 1.15f` tại `Assets/_Project/Scripts/World/Sky/SkyCycleSettings.cs:172, 176`). Trong mã nguồn hiện tại, mặt tối Trái Đất vẫn có ánh đèn thành phố mờ (`SkyCycleSettings._eclipseCityLightStrength = 0.45f` tại `SkyCycleSettings.cs:189`); Story v4 **`[ĐỀ XUẤT]`** đưa `_eclipseCityLightStrength` về `0.0f` (chờ quyết định ở Câu hỏi 2) để **hoàn toàn không còn một ánh đèn thành phố nào trên mặt tối của Trái Đất** — truyền tải trực quan ngay từ giây đầu tiên rằng nhân loại đã rời bề mặt Trái Đất.
2. **Màn hình khởi động (`cutscenes.md` §1.2 O5; `gdd-v3.md` §8; `LensView.cs:31`):** Góc ống kính hiện đúng 3 dòng chữ hệ thống hiện có trong `Assets/_Project/Data/Localization/en.json:1626-1636` và `vi.json:1626-1636`:
   - `lens.boot.signal`: `EARTH SIGNAL · DETECTED` (`TÍN HIỆU TRÁI ĐẤT · ĐÃ NHẬN`)
   - `lens.boot.uplink`: `UPLINK · OFFLINE` (`ĐƯỜNG TRUYỀN · MẤT KẾT NỐI`)
   - `lens.boot.recovery`: `BEGIN RECOVERY` (`BẮT ĐẦU KHÔI PHỤC`)
3. **Góc máy kéo ra (`cutscenes.md` §1.2 O6–O7):** Camera lùi khỏi ống kính để lộ khuôn mặt **màn hình CRT hổ phách** của 07 (`Shader "MoonProject/R1/CrtFace"`, `Assets/_Project/Shaders/R1/R1CrtFace.shader:1,7`), ăng-ten nháy 3 ngắn 1 dài (`log.kenji_1`, `en.json:1142-1144`) hướng về ngọn đèn báo màu hổ phách (`#FFB547`, `docs/VISION.md:205`) trên Tháp Radio, trong lúc ánh bình minh bắt đầu ló rạng sau vành nhật thực.
4. **Lăn bánh đầu tiên:** Người chơi điều khiển 07 ngay lập tức (trong vòng ~4 giây, `docs/VISION.md:131`), mở khóa danh sách 4 bước `QUY TRÌNH KHÔI PHỤC LUMEN` (`ui.recovery.title`, `en.json:254-272`, `vi.json:254-272`).

---

## 4. Cách người chơi khám phá sự thật & Diễn biến 4 Chặng

Không có lời thoại thuyết minh dài dòng; người chơi tự ghép nối câu chuyện qua **4 lớp bằng chứng**:
1. **Bầu trời (`Assets/_Project/Scripts/World/Sky/` & `LofiEarth.shader`):** Mặt đêm Trái Đất tắt sạch ánh đèn đô thị (`[ĐỀ XUẤT]` `_eclipseCityLightStrength = 0.0f` tại `SkyCycleSettings.cs:189`, giữ `SkySettings._earthNightBrightness = 0.02f` tại `SkySettings.cs:132`); mặt ngày phủ lớp mây mờ trắng kem dày hơn (`[ĐỀ XUẤT]` tăng `SkySettings._earthPaleness` từ `0.3f` tại `SkySettings.cs:127` và `_CloudCover` từ `0.38` tại `LofiEarth.shader:15`).
2. **Dấu vết vật lý (`crate.*`, `map.*`, `micro.*`):** Ghi chú trên thùng hàng (`crate.kestrel.note`: `"Lenses off the old relay. Handle them like records. — Ro"` tại `en.json:1778-1780`; `crate.lander.note`: `"Seed trays went up top. This is everything else. — Theo"` tại `en.json:1790-1792`), ghi chú bút chì của Ines trên bản đồ khảo sát (`map.*.note` tại `en.json:1274-1468`; `docs/design/text/crew-map.md:5-6`), và các xác robot lặng lẽ (`micro.06.name`, `micro.mule.name`, `micro.haul_sled.name`, `micro.drone.name` tại `en.json:1938-1952`).
3. **Nhật ký vận hành & Băng thu âm của Ro (`log.*`, `en.json:746-748, 1142-1184`):** Các bản ghi cuối (`log.ro_3`, `log.ines_3`, `log.theo_3`, `log.kenji_3`) được **`[ĐỀ XUẤT]`** chỉnh lại thành **Nhật ký Trạm (`STN-LOG`)**, **Thẻ Bảo trì (`MAINT-TAG`)**, và **Bản thu phòng phát thanh (`TAPE-LOG`)** chuẩn thập niên 1980 — loại bỏ hoàn toàn kiểu viết thư tâm sự trực tiếp cho robot (chi tiết đối chiếu từng chuỗi trong `docs/design/story-v4.md` Mục 10.7).
4. **Tín hiệu các cột tiếp sóng & Lời đáp cuối game (`letter.mast_1..10`, `ticker.ending.answer`, `letter.after_1..10` tại `en.json:1190-1272`; `docs/design/text/radio-letters.md`):** **`[ĐỀ XUẤT]`** viết lại các bức thư tiếp sóng (hiện đang ghi năm `2047–2067` từ `Houston relay` và tả cảnh sống trên mặt đất) để phản ánh đúng mốc sơ tán năm **1988** lên **Tàu Ark IV (*Vanguard*)** tại điểm L5 mà không gọi trực tiếp tên robot (chi tiết tại `docs/design/story-v4.md` Mục 10.2).

### Hành trình 4 Chặng:
- **Chặng I (Lòng chảo Lumen — `0.5.5`):** Nâng cấp Tháp Radio Cấp 1–3 (`upgrade.radio_tower.1..3`, `en.json:510-544`), sửa **Tilly** ở đáy lòng chảo (`friend.tilly.name`, `en.json:730-736`), gặp **06** tại chòi bảo trì `relay.2` (`micro.06.name`, `en.json:1938-1940`; `0.5.5-owner-feedback.md` §1.7 dòng 138, 153) để quét bản vẽ Bộ pin II, nhảy qua vực vào **Hẻm Thì Thầm (`Whispering Canyon`)** sửa **Bell** (`friend.bell.name`, `en.json:738-744`).
- **Chặng II (Vành Miệng Hố, Bậc Thềm Vành Hố & Hẻm Núi Meridian — `0.6+`):** Chế tạo bánh từ tính (`Magnetic Treads`) leo lên **Bậc thềm vành hố (`Rim Terraces`, `map.region.terraces`, `en.json:1454-1460`)** sửa cần cẩu **Atlas** của Kenji (`docs/STORY.md:43`; `docs/DESIGN.md:68`), tìm thấy các robot tiền nhiệm **01, 02, 03, 05** (`0.6+ [ĐỀ XUẤT]`), khám phá trạm tự động không người lái **Meridian** (`[ĐỀ XUẤT]`) và thu hồi dữ liệu **Mẫu lõi 41** (`[ĐỀ XUẤT]`).
- **Chặng III (Hố Tối Vĩnh Cửu & Khôi phục Chảo Lớn — `0.6+`):** Tiến vào **Hố Tối (`Shadowed Crater`, `en.json:1714-1716`)**, sửa robot làm vườn **Moss** của Theo (`docs/STORY.md:29`; `docs/DESIGN.md:66`), tìm thấy **04** (`0.6+ [ĐỀ XUẤT]`), cùng Atlas lắp đặt **Chảo Ăng-ten Lớn** (`recovery.dish.name`, `en.json:266-268`) và gỡ kẹt trục nâng 14% (`[ĐỀ XUẤT]`).
- **Chặng IV (Đỉnh Núi `The Peak` & Kết thúc — `docs/design/cutscenes.md` §9):** 07 leo lên **Đỉnh Núi (`The Peak`, `map.region.peak`, `en.json:1446-1452`)**. Chảo lớn xoay khớp vị trí, phóng chùm tia ấm áp lên không gian. Trên đỉnh núi hoàn toàn tĩnh lặng suốt **~6 giây** (`docs/design/cutscenes.md` §9 F4 dòng 342: `"Silence (~6 s). Earth, held."`) trước khi bài nhạc cuối trên mặt B cuộn băng của Ro (`relic.cassette_player.memory`, `en.json:562-564`) và **Đĩa Vàng Lumen** (`relic.golden_record.memory`, `en.json:578-580`) vang lên.

---

## 5. Kết thúc: Ai trả lời tín hiệu của 07? (`[ĐỀ XUẤT]`, Gắn với Câu hỏi 3)

- **Phương án A (Khuyên dùng — Thế hệ Tàu Ark IV & Phi hành đoàn năm xưa, `[ĐỀ XUẤT]`):** Sau khi dữ liệu Mẫu lõi 41, bức ảnh mầm xanh trong Khay số 4 và bản nhạc của Ro truyền đi, một đốm sáng màu hổ phách tại tọa độ **L5** cạnh Trái Đất tối đen nháy **ba ngắn, một dài** (`···—`), nối tiếp cảnh `cutscenes.md` §9 F7. **Đài Canh Nghe Ark IV (*Vanguard*)** trả lời trên tần số `142.8 MHz` (`[ĐỀ XUẤT]` thay cho `ticker.ending.answer`, `en.json:1230-1232`): 4 thành viên phi hành đoàn năm xưa (nay đã già trên tàu Ark IV) xác nhận đã nhận đủ dữ liệu và báo tin **tàu khảo sát trở lại Mặt Trăng đầu tiên sẽ khởi hành vào mùa xuân tới.**
- **Phương án B (Đồng nghiệp khoa học — Tiến sĩ Lena Moreau trên Tàu Ark IV, `[ĐỀ XUẤT]`):** Tiến sĩ Lena Moreau — đồng nghiệp thủy văn của Ines tại Geneva từng phân tích dữ liệu viễn thám của trạm tự động Meridian từ xa trước khi sơ tán lên Tàu Ark IV (chưa từng đóng quân tại Meridian vì Meridian hoàn toàn không người lái) — xác nhận đã lưu trữ kho dữ liệu nước ngầm và báo tin mừng cho 4 thành viên Lumen đang nghỉ hưu trên trạm.
- **Phương án C (Tối giản thuần chất thơ, `[ĐỀ XUẤT]`):** Chỉ có dòng xác nhận giao thức tự động từ Tàu Ark IV (`SIGNAL ACKNOWLEDGED · ARCHIVE STORED`) cùng đốm sáng màu hổ phách nháy `···—` tại tọa độ L5 bên cạnh Trái Đất tối đen.

---

## 6. Năm Câu Hỏi Mở Cần Chủ Dự Án Quyết Định (Open Questions)

1. **Câu hỏi 1 — Nguyên nhân khiến nhân loại phải rời Trái Đất năm 1988 (`[ĐỀ XUẤT]`):**
   - **1A (Khuyên dùng):** Chuỗi siêu phun trào núi lửa địa nhiệt năm 1979 tạo màn sương che khuất ánh nắng, làm sụp đổ chu kỳ gió mùa và nông nghiệp toàn cầu (không chiến tranh, không vũ khí hủy diệt).
   - **1B:** Biến đổi khí quyển / aerosol công nghiệp vượt ngưỡng khiến tầng bình lưu mờ đục, buộc con người tạm rút lên các Tàu Ark L4/L5 trong 1–2 thế kỷ chờ mặt đất tự làm sạch.
   - **1C:** Giữ bí ẩn — các nhật ký chỉ nhắc tới *"Mưa Tro Bụi / Bầu Trời Tĩnh Lặng"* và *"Cuộc Khởi Hành lên các Tàu Ark"* mà không chốt nguyên nhân địa chất hay công nghiệp.
2. **Câu hỏi 2 — Hiển thị Trái Đất trên bầu trời (`SkyCycleSettings.cs` & `LofiEarth.shader`, `[ĐỀ XUẤT]`):**
   - **2A (Khuyên dùng):** Đặt `SkyCycleSettings._eclipseCityLightStrength = 0.0f` (`Assets/_Project/Scripts/World/Sky/SkyCycleSettings.cs:189`, hiện tại là `0.45f`) để tắt sạch 100% ánh đèn thành phố ở mặt đêm/nhật thực; mặt ngày phủ mây mờ dày hơn (`[ĐỀ XUẤT]` tăng `SkySettings._earthPaleness` tại `SkySettings.cs:127` và `_CloudCover` tại `LofiEarth.shader:15`).
   - **2B:** Tắt đèn thành phố nhưng giữ lại **1 đốm sáng nhỏ duy nhất** ở rìa quỹ đạo/L5 tượng trưng cho trạm neo Tàu Ark.
   - **2C:** Lớp mây mờ trên Trái Đất quang đãng dần sau khi hoàn thành Chặng IV.
3. **Câu hỏi 3 — Ai gửi tín hiệu đánh thức và trả lời ở cuối game (`[ĐỀ XUẤT]`):**
   - Chọn **Phương án A** (Tàu Ark IV + lời nhắn từ phi hành đoàn già), **Phương án B** (Tiến sĩ Lena Moreau — đồng nghiệp từ xa của Ines trên Tàu Ark IV), hay **Phương án C** (Tín hiệu đèn hiệu tự động tối giản)?
4. **Câu hỏi 4 — Số phận của robot đàn anh 06 sau khi sửa xong Chảo Lớn (`[ĐỀ XUẤT]`):**
   - **4A:** Giữ `06` (`micro.06.name`, `en.json:1938-1940`) nằm yên tại chòi bảo trì `relay.2` như một đài tưởng niệm thầm lặng (như thiết kế `0.5.5-owner-feedback.md` §1.7 hiện tại).
   - **4B:** Mở thêm nhiệm vụ phụ sau Chặng III cho phép 07 mang pin dự phòng từ Xưởng Xe của Kenji ra sạc và đón `06` lăn bánh về nghỉ ngơi cạnh nhà.
5. **Câu hỏi 5 — Thời điểm cập nhật tệp ngôn ngữ `en.json` & `vi.json` (`[ĐỀ XUẤT]`):**
   - **5A:** Cập nhật ngay các thư tiếp sóng `letter.mast_1..10`, `ticker.ending.answer`, `letter.after_1..10` (`en.json:1190-1272`) và 4 nhật ký cuối `log.*_3` (`en.json:1170-1184`) theo bảng đề xuất ở Mục 10 của `story-v4.md`.
   - **5B:** Chỉ cập nhật `letter.mast_1..10` (`en.json:1190-1228`), giữ nguyên toàn bộ `log.*` hiện tại.
   - **5C:** Giữ tài liệu này làm bản thiết kế chuẩn và đợi tới đợt làm nội dung `0.6` mới sửa `en.json` / `vi.json`.
