# Lunarion — Kế Hoạch Tổng Thể (Master Plan)

> **Trạng thái:** Bản trình duyệt Chủ dự án (Owner Review) — Nguồn sự thật duy nhất (Single Source of Truth)
> **Ngày cập nhật:** 2026-10-14
> **Nhánh làm việc:** `box/agy-plan`
> **Phạm vi:** Định nghĩa toàn bộ trò chơi từ `0.5.6` đến `1.0`, hợp nhất `docs/design/gdd-v3.md`, `docs/design/0.5.5-owner-feedback.md`, `Logs/sources/07V2.md`, `Logs/sources/story-v3.md`, `Logs/sources/PLAN.md` và toàn bộ 25 file trong `Logs/owner-memory/`.

---

## 0. Tóm tắt một trang (One-Page Summary)

**Lunarion là gì?**
Một trò chơi khám phá Mặt Trăng góc nhìn thứ ba, yên bình và cô đơn (*cozy, solitary lunar rover game*). Người chơi điều khiển **07** — chiếc xe tự hành khảo sát (*survey rover*) cuối cùng còn hoạt động tại **Trạm Lumen** (*Lumen Station*), một căn cứ nghiên cứu gần cực Nam Mặt Trăng bị sơ tán khẩn cấp sau cơn bão mặt trời tháng 10/2047. Không có thanh máu, không đếm ngược oxy, không kẻ thù, không chiến đấu, không màn hình thua cuộc (*No Fail State*).

**Người chơi làm gì?**
1. **Rời đế sạc đi khảo sát:** Lái 07 qua địa hình bụi xám, miệng hố, khe nứt dung nham và hẻm núi dưới bầu trời đen thẫm đầy sao và Trái Đất treo thấp trên đường chân trời.
2. **Tương tác bằng cơ khí thật:** Quét các mảnh bản vẽ công nghệ bị vỡ (*Fragment Blueprints*) bằng mắt quang học; cắt mối hàn phế liệu bằng mỏ cắt mũi xe; đào thiết bị vùi dưới đống đổ nát bằng gầu xúc trước; móc cáp tời kéo hoặc dùng cần trục lưng nâng kỷ vật về nhà; mở cửa khoang hàng sau để thu vật liệu và linh kiện.
3. **Chế tạo, lắp ráp và mở rộng mạng lưới:** Đổ vật liệu vào phễu Xưởng xe (*Vehicle Bay*) để chế tạo thiết bị mới, lắp trực tiếp lên các ổ cố định trên thân 07, dựng lại tháp tiếp sóng (*Relay Towers*), đặt trụ sạc (*Beacon Chargers*) và đánh thức các robot đồng đội bị kẹt (*Companions*).
4. **Đi xa dần qua 6 vùng địa chất:** Từ Lòng chảo căn cứ (*The Basin*) qua Hẻm Thì Thầm (*Whispering Canyon*), Biển Dài (*Long Mare*), Bậc thềm vành hố (*Rim Terraces* & *Meridian Station*), Hố Bóng Tối & Ống Dung Nham (*Shadowed Crater* & *Lava Tube*) để vươn tới Đỉnh núi (*The Peak*).

**Cảm giác chủ đạo là gì?**
**Sự kinh ngạc cô đơn chuyển dần thành niềm tự hào lặng lẽ (*Lonely wonder turning into quiet pride / quiet vindication*).**
Ban đầu, Mặt Trăng lạnh lẽo, tối tăm và tĩnh mịch; 07 nhỏ bé, pin yếu, đi xa vài trăm mét đã phải quay về. Qua từng giờ chơi, chính tay người chơi làm sáng lại từng tòa nhà, nối từng đường cáp vàng ấm áp trên mặt bụi xám, nghe tiếng máy móc trong lòng chảo rì rầm hoạt động trở lại, và nhìn thấy chiếc xe 07 của mình trưởng thành với đầy đủ thiết bị cơ khí gắn trên thân.

**Vì sao người chơi chơi đến hết (3–5 giờ cho mục tiêu chính)?**
Khi Trạm Lumen sơ tán năm 2047, Ủy ban Tài nguyên Trái Đất đã đóng cửa chương trình vì kết luận Mặt Trăng "khô cằn, không đáng quay lại" (*not worth returning to*). Nhưng trong quá trình mở rộng mạng lưới, 07 phát hiện ra hai sự thật bị bỏ quên:
1. Nhà địa chất **Ines Kalani** đã tìm thấy một mạch băng nước cổ đại khổng lồ kèm rêu ngủ đông dưới lòng đất ngay trong đêm bão mặt trời — nhưng kho dữ liệu bị cắt điện trước khi kịp gửi về.
2. Cạnh Lumen còn có một trạm tự động thứ hai — **Trạm Meridian** (*Meridian Station*) của liên doanh Tycho — cũng đã khoan trúng đúng tầng băng đó từ phía bên kia sườn núi.
3. Trái Đất chưa bao giờ ngừng gọi vào mỗi kỳ cửa sổ liên lạc, nhưng năm 2067 một con robot quét bụi bị kẹt làm mảng pin của chảo tiếp sóng chính phủ đầy bụi, khiến đường truyền từ Mặt Trăng im bặt.

07 thu thập đủ **6 mảnh dữ liệu Khảo sát (*Survey 6/6*)**, nối điện lên **Chảo ăng-ten Đỉnh núi (*Peak Dish*)**, và gửi bản báo cáo hoàn chỉnh về Trái Đất — chứng minh Mặt Trăng có nước và sự sống, buộc con người phải mở lại chương trình quay về.

---

## 1. Trải nghiệm người chơi & Luật bất biến (Player Experience & Non-Negotiable Rules)

### 1.1. Năm trụ cột trải nghiệm (Hook / Pull / Hold / Finish / Feeling)
*(Trích dẫn: `player-experience-first.md`, 2026-10-13; `awe-hooks.md`, 2026-10-13; `docs/design/north-star.md`)*

| Trụ cột | Câu hỏi của người chơi | Câu trả lời cụ thể trong game |
|---|---|---|
| **Hook** *(5 giây – 1 giờ đầu)* | *"Thế giới này là gì? Mình đang ở đâu?"* | 07 mở màn trập ống kính trong nắng xiên cực thấp. Trước mặt là Trạm Lumen tắt điện, trên Đỉnh núi cao 240 m có đốm đèn đỏ chớp tắt mỗi 3 giây, bên phải là Trái Đất khổng lồ treo sát chân trời. Radio bắt được tiếng nhạc hiệu rè trong sóng tĩnh. Cách 160 m là xác vệ tinh **Kestrel-3** vừa rơi, mang phù hiệu lạ (*Tycho Consortium · Meridian Relay*) không thuộc về Trạm Lumen. |
| **Pull** *(Động lực kéo đi)* | *"Chuyện gì đã xảy ra ở đây? Bên kia sườn đồi có gì?"* | Bốn câu hỏi nối tiếp nhau: (1) Ai vẫn đang phát sóng từ Trái Đất? (2) Trạm Meridian là ai và nằm ở đâu? (3) Mặt Trăng có thực sự chết và vô giá trị như lệnh đóng cửa nói không? (4) Vì sao liên lạc tắt hẳn từ năm 2067? Mỗi vùng trả lời một câu hỏi và để lộ một công trình nhìn thấy bằng mắt thường ở vùng kế tiếp. |
| **Hold** *(Giữ chân mỗi 10 phút)* | *"Mình đang mạnh lên và làm ấm thế giới này thế nào?"* | (1) Thân xe 07 gắn thêm thiết bị thật sau mỗi lần lắp; (2) Căn cứ và mặt đất sáng dần lên nhờ đèn ấm và đường cáp nối; (3) Hệ sinh thái máy móc xung quanh tự chạy lại sau khi 07 sửa xong; (4) Bản đồ địa hình tự vẽ thêm vùng mới + bộ đếm `Khảo sát 0/6 → 6/6` + Sổ ghi chép (*Databank*) mở thêm mục mới. |
| **Finish** *(Đích đến rõ ràng)* | *"Khi nào mình hoàn thành sứ mệnh?"* | **Bảng Phục Hồi (*Restoration Board*)** tại Trung tâm Liên lạc hiện rõ 4 bước từ phút thứ 20: `[1] Nguồn điện` → `[2] Nối mạng 3 tháp` → `[3] Chảo Đỉnh núi` → `[4] Gửi báo cáo + Khảo sát (6/6)`. Khi phát sóng thành công, **Ro** bắt được tín hiệu qua đài nghiệp dư trước, chuyển tiếp cho **Houston**, và Houston xác nhận mở lại sứ mệnh tàu có người lái. |
| **Feeling** *(Dư vị đọng lại)* | *"Mình cảm thấy gì khi chơi?"* | Cô đơn nhưng ấm áp, thư thái tuyệt đối khi lăn bánh trên cát bụi xám, xen lẫn những khoảnh khắc đứng lặng ngắm cảnh hùng vĩ (*Awe beats*: nhật thực Trái Đất, tinh thể phát sáng trong ống dung nham, đàn drone bay qua thung lũng) và niềm tự hào của một người thợ máy cần mẫn. |

### 1.2. Mười một luật bất biến chủ dự án đã khóa (Owner-Locked Laws)

Mọi thiết kế, mã nguồn, cảnh 3D và văn bản bắt buộc phải tuân thủ 11 luật sau. Nếu bất kỳ chi tiết nào vi phạm, chi tiết đó bị coi là lỗi (bug):

1. **Cô đơn mà bình yên là ưu tiên số 1 về hình và tiếng** (`feel-priority-solitude.md`, 2026-10-07; `look-too-purple.md`, 2026-10-14):
   Bầu trời đen, mặt đất xám bụi Mặt Trăng trung tính (không ám tím cảnh mộng mơ), bóng đổ sắc, gió cực êm, tiếng bánh xe lăn trên sỏi mịn và tiếng cơ khí nhỏ nhẹ. Căn cứ là ốc đảo vàng ấm giữa đồng hoang lạnh.
2. **Tiến trình là xương sống; ký ức chỉ là gia vị** (`progression-first.md`, 2026-10-11):
   Người chơi mở khóa vùng mới và cảm thấy tiến bộ nhờ **vật liệu → bản vẽ → chế tạo → lắp thiết bị lên 07 → mở cổng vật lý**, tuyệt đối không dùng "điểm ký ức" làm chìa khóa mở vùng. Ký ức chỉ làm giàu ý nghĩa cho chuyến đi.
3. **Mọi tiến trình đều phải nhìn thấy được bằng mắt ở khoảng cách 30 m** (`visible-progression-priority.md`, 2026-10-08; `equipment-07.md`, 2026-10-13):
   Không được chỉ cộng số ẩn (`+20% tốc độ`). Mọi nâng cấp của 07 phải gắn một khối hình học 3D mới lên đúng ổ trên thân xe; mọi công trình được sửa phải đổi tư thế cơ khí, bật đèn vàng ấm và nối dây năng lượng trên mặt đất.
4. **Công dụng của mọi vật thể phải đọc được ngay từ hình dáng, không cần nhãn chữ** (`purpose-must-read.md`, 2026-10-11):
   Đế sạc có vòng tiếp xúc đồng và cọc đèn; phễu nạp nguyên liệu có miệng phễu nghiêng và băng tải; tháp tiếp sóng có chảo parabol hướng về đỉnh núi; mảnh bản vẽ có khung viền lục giác màu lục lam và nằm trong xác máy hỏng.
5. **Logic vật lý, cơ khí và sinh thái phải chặt chẽ tuyệt đối** (`living-moon.md`, 2026-10-13; `story-logic.md`, 2026-10-13; `rover-07-v2.md`, 2026-10-14):
   Không có hành động "phép thuật". Muốn kéo phải có tời và dây cáp căng tới móc; muốn chở phải có giá đỡ hoặc khoang chứa mở nắp; muốn cắt phải có mỏ cắt ở mũi xe; muốn đào phải có gầu xúc cắm xuống đất; một máy móc còn chạy sau 20 năm phải có tấm thu năng lượng mặt trời hoặc cắm vào nguồn điện thật.
6. **Tuyệt đối không có đồ vật hay vòng sáng phát quang nằm trơ trên mặt đất trống** (`no-floating-loot.md`, 2026-10-13 & 2026-10-14; `derelict-variety.md`, 2026-10-13; `rover-07-v2.md`, 2026-10-14):
   Mọi vật phẩm thu thập được (mảnh bản vẽ, đá quặng, linh kiện, kỷ vật, băng ghi âm, bộ phận bạn bè) phải gắn trên giá đỡ, kẹt trong khung máy vỡ, nằm trong hộc bảo trì, lộ ra từ mạch đá gốc hoặc vùi một phần dưới đất đá. Kể cả hiệu ứng nghe sonar cũng không được vẽ cột sáng trên trời hay vòng sáng trôi trên mặt đất trống.
7. **Mọi thay đổi trạng thái đều phải có chuyển động cơ khí + âm thanh ngũ cung của 07** (`polish-every-state.md`, 2026-10-12):
   Không bao giờ dùng hiệu ứng "bốc hơi/hiện hình tức thì" (*pop-in / pop-out*). Khi nhặt đồ, nạp phễu, sửa tháp, lắp thiết bị hay đánh thức bạn bè, phải có hoạt cảnh cơ khí ngắn (0,4–1,6 giây) kèm tiếng máy và câu chirp 2–4 nốt của 07.
8. **Giao diện học từ Subnautica nhưng gọn hơn: Kho đồ + Trang bị trên MỘT màn hình cố định duy nhất** (`ui-taste-subnautica.md`, 2026-10-12; `learn-then-improve.md`, 2026-10-12; `inventory-07-only.md`, 2026-10-13; `inventory-one-screen.md`, 2026-10-14):
   Nhấn `Tab` mở đúng một màn hình cố định: bên trái là lưới Khoang hàng của 07, bên phải là hình chiếc 07 với các ổ thiết bị cố định. Không có tab chuyển trang, không có thanh cuộn, không có cửa sổ trôi theo vị trí xe 3D.
9. **Tối đa MỘT phần tử hướng dẫn trên màn hình; chỉ nhắc khi cần và không bao giờ lặp lại điều đã biết** (`plain-functional-text.md`, 2026-10-12 & 2026-10-13):
   Bộ khóa `HudMutex` đảm bảo tại một thời điểm chỉ hiện tối đa 1 dòng nhắc (`Prompt` hoặc `Hint` hoặc `Toast`). Khi người chơi đã thực hiện thành công một thao tác 1 lần, khóa vĩnh viễn gợi ý dạy thao tác đó trong hồ sơ lưu.
10. **Văn bản chức năng ngắn gọn, tự nhiên; không nhân cách hóa máy móc và không ủy mị** (`plain-functional-text.md`, 2026-10-12; `story-logic.md`, 2026-10-13):
    Nhãn và gợi ý chỉ nói: *đây là cái gì, bấm nút gì, để được việc gì*. Trong nhật ký và băng ghi âm, các kỹ sư nói chuyện với nhau như đồng nghiệp chuyên nghiệp; họ coi 07 và các drone là thiết bị tốt, không bao giờ tâm sự mùi mẫn kiểu *"Tilly ơi tao nhớ mày"* hay gọi máy móc là *"cô ấy/anh ấy"*.
11. **Làm theo từng phiên bản hoàn chỉnh; khóa phong cách và thiết kế trước khi dựng hàng loạt** (`phased-releases.md`, 2026-10-08; `rover-07-v2.md`, 2026-10-14):
    Không xây dựng dàn trải khi chưa chốt hình ảnh và cơ chế nền tảng. Sửa dứt điểm toàn bộ phản hồi `0.5.5` trong bản `0.5.6`, chốt phong cách 3D và âm thanh, dựng chuẩn `07 v2`, rồi mới mở rộng vùng `0.6 → 1.0`.

---

## 2. Mô hình 07 hoàn chỉnh — 07 v2 (Complete Mechanical Architecture of 07)

*(Trích dẫn: `cut-charge-weight.md`, 2026-10-13; `equipment-07.md`, 2026-10-13; `rover-07-v2.md`, 2026-10-14; `Logs/sources/07V2.md`)*

### 2.1. Triết lý thiết kế 07 v2: Mọi động từ đều có bộ phận cơ khí thật

Trong bản `0.5.5`, 07 bị biến thành một "phù thủy bắn tia từ mắt": mắt camera vừa quét, vừa bắn tia cắt, vừa bắn tia hàn, vừa bắn chùm sáng đào đất, vừa bắn hạt năng lượng vào phễu, lại vừa kéo kỷ vật lơ lửng không cần dây cáp. Chủ dự án đã bác bỏ hoàn toàn cách làm này (`0.5.5-owner-feedback.md`, mục #6, #9, #10, #11; `rover-07-v2.md`).

**Quy tắc 07 v2:**
1. **Mỗi nhóm hành động xuất phát từ đúng một cụm cơ khí chuyên biệt trên thân 07** (7 điểm gốc `IRoverRig`).
2. **Mắt camera (`EyeOrigin`) chỉ làm nhiệm vụ quang học** (nhìn, biểu cảm màn trập, chiếu chùm sáng quét `ScanFan`). Tuyệt đối không bắn tia công cụ hay phóng vật liệu từ mắt.
3. **Thân xe 07 giữ tính đối xứng trái–phải qua trục `x = 0`**: Khung gầm, 6 bánh rocker-bogie, boong trước, cửa khoang hàng sau, tời sau, gầu xúc trước và khung nâng lưng đều nằm chính giữa hoặc đối xứng gương hai bên. Riêng cột ăng-ten bên trái `(-0.35 m)` được cân bằng thị giác bằng trụ thông gió pin bên phải `(+0.35 m)`.

### 2.2. Sơ đồ 7 cụm thiết bị và điểm gốc trên thân 07 (`IRoverRig`)

| Điểm gốc (`IRoverRig`) | Tọa độ local `(x, y, z)` | Cụm bộ phận 3D trên thân 07 | Nhiệm vụ cơ khí duy nhất |
|---|---|---|---|
| **1. `EyeOrigin`** | `(0.00, 1.31, +0.53)` | Đầu camera đơn (`Head`), màn trập 2 mí (`EyelidUpper/Lower`), vành thấu kính lấy nét (`FocusLens`) | Nhìn mục tiêu, biểu cảm mí mắt, chiếu mặt quạt ánh sáng lục lam (`ScanFan`) khi quét mảnh bản vẽ và đầu đọc dữ liệu. |
| **2. `CutterOrigin` & `StitchOrigin`** | `(0.00, 1.05, +0.93)` & `(0.00, 1.00, +0.93)` | **Tháp cắt & hàn mũi xe (`CutterTurret`)** ở chính giữa mép trước boong + **2 đường ống tụ điện 3 tầng (`ChargeConduits/Stage_1..3_L/R`)** dọc hai bên sườn boong trước | Cắt mối hàn phế liệu (`LaserCut` ở cự ly gần `3,5 m`) và phóng hồ quang hàn vi điểm (`MicroArcStitch`) khi sửa cáp thang nâng, tháp tiếp sóng, bạn robot. |
| **3. `ScoopOrigin`** | `(0.00, 0.16, +1.37)` | **Gầu xúc răng cưa trước (`DigScoop`)** gắn vào cản trước dưới qua 2 tay đòn khuỷu (`ScoopArm_L/R`), vươn ra trước lốp trước `23 cm` | Hạ gầu cắm xuống đất, xúc 3 nhịp cơ khí (`1,6 s`) để đào kỷ vật hoặc linh kiện bị vùi dưới đống đổ nát. |
| **4. `HatchOrigin`** | `(0.00, 0.62, -0.64)` | **Cửa khoang hàng vỏ sò đôi (`CargoHatch`)** ở đuôi xe (nắp trên mở lên `+52°`, máng dưới hạ `-12°`), lòng khoang lõm (`CargoHold`), con lăn cuốn từ tính (`IntakeRoller`) | Mở cửa đuôi xe để: (1) cuốn các bó vật liệu, băng ghi âm, bộ phận bạn bè và linh kiện tháp vào trong bụng xe; (2) bắn trả bó vật liệu theo vòng cung vào miệng phễu trạm (`HopperFeed`). |
| **5. `WinchOrigin`** | `(0.00, 0.38, -0.86)` | **Trống tời cáp thép (`WinchDrum`)** có mô-tơ và **Móc cáp từ tính (`TowHook`)** gắn ở cản sau dưới | Phóng móc cáp thép vào vòng khuyên của kỷ vật hoặc vật nặng, kéo lê trên mặt đất (`GroundTow`) và thu/nhả độ dài dây (`Q/E`). |
| **6. `CradleOrigin`** | `(0.00, 0.99, -0.47)` | **Đệm chở hàng trên lưng (`CargoCradle`)** và **Khung cần trục chữ A (`HoistBail`)** khớp bản lề ở `(±0.41, 0.82, -0.58)` | Hạ khung chữ A ra phía sau móc kỷ vật từ tời, nâng bổng qua đuôi xe (`0,9 s`) đặt lên đệm lưng và kẹp chặt để chở đi không ma sát. |
| **7. `AntennaOrigin`** | `(-0.35, 1.54, -0.48)` | **Cột ăng-ten (`AntennaMast`)** có đầu đèn tín hiệu và **Vòng ăng-ten định hướng xoay được (`SonarLoop`)** ở cao độ `y = 1.11` | Xoay vòng ăng-ten về đúng phương vị có tín hiệu phản hồi khi 07 dừng xe lắng nghe sonar. |

### 2.3. Bảng tổng hợp: Khả năng → Bộ phận 07 → Cách mở khóa → Trước & Sau khi nâng cấp

| Khả năng / Động từ | Bộ phận 3D trên thân 07 | Cách mở khóa & Nơi lắp | Trước khi mở khóa thì sao? | Sau khi mở khóa / nâng cấp thì sao? |
|---|---|---|---|---|
| **1. Quét quang học** *(Optical Scan — Giữ chuột phải / E)* | Mắt ống kính `Eye` *(có sẵn)* → lắp thêm vành **Thấu kính lấy nét (`FocusLens`)** quanh mắt | Có sẵn từ đầu game. Nâng cấp **Quét Nhanh (*Fast Scan*)** sau khi quét 2 mảnh ở Lòng chảo + chế tạo tại Xưởng xe | Quét mất `2,0 s`, tầm `6 m`, mặt quạt ánh sáng lục lam mảnh xuất phát từ `EyeOrigin` quét dọc bề mặt vật thể. | Vành `FocusLens` xoay lấy nét; thời gian quét giảm còn `1,1 s`, tầm `9 m`, giữ lịch sử quét cho Sổ ghi chép. |
| **2. Thu & nạp vật liệu, linh kiện** *(Cargo Intake & Hopper Feed)* | Cửa vỏ sò sau `CargoHatch` + khoang `CargoHold` *(8 ô có sẵn)* → lắp thêm **Hộc hông (`SidePanniers_L/R`)** | Có sẵn 8 ô từ đầu. Nâng cấp **Khoang Hàng Rộng (*Cargo Rack*, 16 ô)**: quét 2 mảnh ở Hẻm Thì Thầm + chế tạo tại Xưởng xe (`4 Metal + 1 Circuit`) | Nhặt vật liệu/băng/linh kiện: 07 xoay nhẹ, nắp `CargoHatch` mở ra `0,45 s`, bó hàng bay vào lòng khoang `HatchOrigin` rồi đóng nắp. Khi nạp phễu: cửa sau mở, con lăn phóng từng bó hàng vào miệng phễu (không bắn từ mắt). Tối đa 8 ô. | Hai hộc `SidePanniers_L/R` gắn đối xứng hai bên sườn sau kèm đèn báo mức đầy (xanh → vàng → cam). Sức chứa tăng từ **8 lên 16 ô**. |
| **3. Móc cáp kéo lê** *(Ground Winch Tow — Phím F)* | **Trống tời cáp (`WinchDrum`)** + **Móc cáp (`TowHook`)** ở cản sau dưới (`WinchOrigin`) | Lắp miễn phí tại **Cổng bảo trì Gara Kenji (*Kenji's Rigging Gantry*)** cách đế sạc 95 m ngay trong 15 phút đầu | **Chưa có tời:** Khi đến gần kỷ vật nặng (như kiện hàng ở Gara Kenji), hiện thông báo rõ: *"Cần Tời cáp sau — lắp tại Gara của Kenji"*. 07 không thể kéo vật nặng bằng tay không hay bằng tia vô hình. | **Đã lắp tời (nhưng chưa có Giá chở):** Phím `F` bắn `TowHook` kèm dây cáp thép thật nối từ `WinchOrigin` tới vật. Kỷ vật **nằm sát mặt đất** (`HoverHeight = 0`), để lại 2 vệt cày sâu trên bụi, có ma sát lớn (07 chạy tối đa `55%` tốc độ, dây căng chùng vật lý, `Q/E` thu/nhả dây `2,2–6,5 m`). Không thể đưa lên thang nâng bụng tàu nếu chỉ kéo lê. |
| **4. Cần trục nâng & chở trên lưng** *(Hoist & Back Cradle — Phím F)* | **Đệm lưng (`CargoCradle`)** + **Khung cần trục chữ A (`HoistBail`)** + kẹp hông (`CradleClamp_L/R`) | Mở khóa Xưởng xe → Quét 2 mảnh bản vẽ Giá chở hàng → Chế tạo (`3 Metal`) → Đỗ vào Xưởng xe để cần cẩu lắp lên boong sau | Chỉ có thể kéo lê kỷ vật trên mặt đất bằng dây tời với tốc độ chậm `55%`, vướng đá và không vào được thang nâng bụng tàu (`BellyLift`). | Khi bấm `F` vào kỷ vật: tời móc vào vật, khung chữ A `HoistBail` ngả ra sau (`-115°`), nâng bổng kỷ vật lên qua đuôi xe (`0,9 s`), đặt gọn lên `CargoCradle` tại `CradleOrigin` và kẹp chặt. 07 chạy **100% tốc độ**, không ma sát đất, đi thẳng lên thang nâng bụng tàu. Khi giữ `F` để thả: khung chữ A hạ kỷ vật nhẹ nhàng xuống đất phía sau xe. |
| **5. Cắt phế liệu & Hàn sửa chữa** *(Plasma Cut & Micro-Arc Stitch)* | **Tháp cắt mũi xe (`CutterTurret`)** tại `CutterOrigin` + **2 đường ống tụ điện 3 tầng (`ChargeConduits`)** dọc boong trước | Mở khóa sau khi sửa Tháp tiếp sóng 1 → Quét 2 mảnh ở Bãi đáp vỡ (*Landing Pad*) → Chế tạo tại Xưởng xe (`3 Metal + 2 Quartz`) | Chỉ có kim hàn vi điểm nhỏ ở mũi xe để chấm mối cáp đứt của thang nâng và tháp tiếp sóng. Không thể cắt các tấm vách hoặc khung sắt có vạch mối hàn cam (*Cut Seam*). | Cụm `CutterTurret` và 2 ống tụ điện `Stage_1` gắn lên boong trước. Khi giữ chuột trái cắt: 3 cặp đốt tụ điện sáng dần từ sau ra trước, mỏ cắt xoay hướng và phóng tia plasma vàng cam ngắn (`3,5 m`) từ `CutterOrigin` kèm tia lửa hàn. Nâng cấp **Tụ Cắt Nhanh (*Charge Capacitor*)** ở `0.6` và **Mỏ Cắt Nhiệt (*Thermal Lance*)** ở `0.7` mở thêm đốt `Stage_2` và `Stage_3` để cắt vách hợp kim dày. |
| **6. Đào đất đá** *(Mechanical Dig Scoop — Phím E)* | **Gầu xúc răng cưa (`DigScoop`)** + 2 tay đòn khuỷu (`ScoopArm_L/R`) ở cản trước dưới (`ScoopOrigin`) | Quét 2 mảnh bản vẽ Gầu đào tại khu khai thác Hẻm Thì Thầm → Chế tạo tại Xưởng xe (`4 Metal + 2 Quartz`) | Không thể lấy các kỷ vật hoặc hộc linh kiện bị đất đá vùi lấp (như bộ phận của Bell dưới chân sườn đá lở). Hiện thông báo: *"Bị vùi dưới đá — cần Gầu đào trước"*. | Khi bấm `E` tại ụ đất/đá lở: 07 tiến sát (`1,5 m`), hạ gầu xúc cắm vào đất và thực hiện **3 nhịp xúc–hất cơ khí (`1,6 s`)** từ `ScoopOrigin` kèm bụi đá văng lên, làm lộ dần vật thể từ dưới đất lên mặt đất. |
| **7. Nghe Sonar định hướng** *(Passive Sonar Listen)* | **Vòng ăng-ten định hướng (`SonarLoop`)** trên cột ăng-ten trái + đầu đèn `AntennaTip` | Có sẵn từ đầu game. Nâng cấp **Bộ Khuếch Đại Tín Hiệu (*Signal Amp*)** sau khi quét 2 mảnh ở Trạm Thời Tiết (*Weather Station*) | Khi 07 đi hơn `30 m` rồi dừng lại `2,0 s`, vòng `SonarLoop` tự xoay một vòng tìm hướng (bán kính `180 m`) rồi khóa mũi tên vào vật gần nhất; đầu 07 quay nhìn theo; góc kim loại của vật lấp lánh dưới nắng (hoặc đèn cọc công trình chớp 2 lần) kèm tiếng chuông kim loại định hướng 3D. | Thêm hộp khuếch đại và vòng phụ trên cột ăng-ten; bán kính nghe tăng lên **`320 m`**, phân biệt rõ âm sắc giữa kỷ vật (chuông đồng trầm), mảnh bản vẽ (tiếng kính ngân cao) và công trình (nhịp xung đôi). |
| **8. Năng lượng & Sạc nắng** *(Battery & Solar Canopy)* | Khung pin sau (`BatteryPack`: 2 → 3 → 4 lõi) + **Cánh pin mặt trời gập mở (`SolarPanels_L/R`)** | Pin 2–4 lõi chế tạo tại Xưởng xe (`0.5.6–0.7`). **Cánh Pin Mặt Trời (*Solar Canopy*)** mở ở `0.6` (Biển Dài) | Pin gốc 100 s; dưới ánh nắng chỉ giảm tốc độ hao pin, muốn sạc đầy phải về Đế sạc hoặc Trụ sạc. | Mỗi cấp Pin gắn thêm 1 khối lõi thật vào hộc pin sau (`150 s → 210 s → 280 s`). Cánh pin mặt trời tự bung ngang hai bên khi đỗ dưới nắng, tự sạc lại tối đa 1 ô pin ngay giữa cánh đồng. |
| **9. Cơ động địa hình** *(Chassis, Boost, Mag-Wheels, Anchor, Heater)* | Động cơ bánh, bình khí nén sau (`BoostThrusters`), **Vành từ tính (`WheelRims`)**, **Tời neo mũi xe (`AnchorWinch`)**, **Lưới sưởi pin (`HeaterGrille`)** | Mở khóa tuần tự từ `0.5.6` đến `0.8` theo từng vùng địa chất | Tốc độ gốc `8 m/s`, leo dốc tối đa `28°`, trượt trên sườn dốc đứng của Bậc thềm vành hố, hao pin gấp đôi trong Hố Bóng Tối lạnh `-200°C`. | **Khung gầm (`0.5.6`):** `10,5 m/s → 13 m/s`. **Bộ đẩy nhảy (`0.5.6`):** nhảy qua khe nứt hẹp. **Bánh từ tính (`0.7`):** bám dốc kim loại/đá từ tính `50°`. **Bộ sưởi pin (`0.8`):** chống đóng băng pin ở Hố Bóng Tối. **Tời neo mũi (`0.8`):** thả dây neo xuống vách Ống Dung Nham. |

### 2.4. Giải quyết dứt điểm 10 điểm vô lý cơ khí chủ dự án đã chỉ ra ở 0.5.5

*(Trích dẫn: `docs/design/0.5.5-owner-feedback.md`; `rover-07-v2.md`, 2026-10-14; `resume-point.md`, 2026-10-14)*

| Điểm vô lý trong bản `0.5.5` | Giải pháp cơ khí dứt điểm trong `07 v2` (áp dụng ngay ở `0.5.6`) |
|---|---|
| **1. Kéo kỷ vật lơ lửng phía sau xe mà không có dây hay móc** | Bắt buộc phải lắp **Tời cáp sau (`WinchDrum` + `TowHook`)** tại Gara Kenji. Khi kéo, dây cáp thép (`LineRenderer` 12 đốt có độ võng catenary) nối trực tiếp từ `WinchOrigin` `(0, 0.38, -0.86)` tới khuy móc của kỷ vật; kỷ vật trượt sát mặt đất (`HoverHeight = 0`). |
| **2. Chở kỷ vật trên lưng mà không có giá đỡ hay tay nâng** | Bắt buộc phải chế tạo và lắp **Giá chở hàng (`CargoCradle` + cần trục chữ A `HoistBail`)** tại Xưởng xe. Khi nhặt, cần trục chữ A hạ xuống móc kỷ vật, nhấc bổng qua đuôi xe (`0,9 s`) và kẹp chặt lên đệm lưng tại `CradleOrigin`. |
| **3. Sửa xong bạn robot lại "rơi ra một thẻ ký ức" trên đất để nhặt** | **Xóa hoàn toàn vật phẩm `MemoryCard` rơi ra khi sửa bạn.** Khi 07 dùng mỏ hàn mũi xe sửa xong bạn robot, người bạn đó tự đứng dậy, lắc đầu, phát câu nhạc chirp cảm ơn và đi làm việc ngay. Ký ức chỉ nằm trong các kỷ vật thật (mũ bảo hộ, ống kính, nhật ký, hộp nhạc) đưa về tàu. |
| **4. Nhặt linh kiện tháp tiếp sóng thấy nó gắn lơ lửng trên nóc xe; đặt ở chân tháp mà tự biến lên đỉnh tháp** | **Xóa `StoryPartSocket` trên nóc 07.** Linh kiện tháp (`TowerCoil`, `Transformer`, `RelaySpare`) được cất gọn vào **Khoang hàng sau (`CargoHold`)** qua cửa vỏ sò `CargoHatch` và chiếm **1 ô khoang hàng**. Tại chân tháp tiếp sóng, bổ sung **Hộc tiếp nhận ở chân tháp (`Base Service Hatch`)** và đường ống dẫn dọc thân tháp: 07 mở cửa khoang sau đẩy linh kiện vào hộc chân tháp, dùng mỏ hàn mũi xe (`StitchOrigin`) chấm 2 mối cáp ở bảng điện chân tháp, sau đó cơ cấu tời nội bộ của tháp kéo lõi lên kích hoạt chảo đỉnh. |
| **5. Đào đất bằng cách bắn tia sáng từ mắt camera làm vật trồi lên** | **Xóa hoàn toàn tia đào từ mắt.** Đào bắt buộc dùng **Gầu xúc răng cưa trước (`DigScoop`)** tại `ScoopOrigin`: 07 hạ gầu cắm xuống đất và xúc 3 nhịp cơ khí làm văng đất đá, lộ vật thể ra. |
| **6. Nạp nguyên liệu vào phễu trạm và sửa công trình bằng cách bắn hạt sáng từ mắt** | **Xóa hoàn toàn hạt sáng bắn từ mắt.** Khi nạp phễu (`HopperFeed`), cửa khoang hàng sau `CargoHatch` mở ra và phóng từng bó kim loại/thạch anh từ `HatchOrigin` bay theo đường parabol rơi vào miệng phễu. Khi sửa cáp thang nâng hoặc tháp, tia hồ quang phóng ra từ **mỏ hàn mũi xe (`StitchOrigin`)**. |
| **7. Bộ phận của Bell nằm lộ thiên trên mặt đất trống mà trò chơi lại bắt phải có Gầu đào mới lấy được** | Đặt bộ phận của Bell (**Chuông cộng hưởng**, bộ phận thứ 3) **bị vùi 70% dưới chân một vạt đá lở (`Rockslide Mound`)** trong Hẻm Thì Thầm, chỉ lộ một góc vỏ đồng. Nhìn bằng mắt là hiểu ngay vì sao tay không không lấy được và phải dùng `DigScoop` xúc đá ra. |
| **8. Đầu của 07 và một đường tia laser luôn khóa cứng chỉ về hướng căn cứ** | **Tháo bỏ hoàn toàn ràng buộc hướng về căn cứ trên đầu 07 và tia công cụ.** Đầu 07 (`HeadYaw/HeadPitch`) mặc định nhìn theo hướng lái xe, khi đi ngang điểm thú vị trong tầm `12 m` thì liếc nhẹ sang nhìn rồi trả về phía trước; tia cắt/quét chỉ bật khi người chơi đang giữ nút thao tác và nhắm đúng vào mục tiêu. |
| **9. Bấm phím Space phát cột sáng khổng lồ xuyên lên bầu trời** | **Xóa phím Space và xóa hoàn toàn cột sáng trên trời (`SiteMarker.Pillar`).** Cũng **không dùng vòng sáng trên mặt đất trống** (bác bỏ đề xuất vòng bụi hổ phách của bản nháp `07V2.md` vì vi phạm `no-floating-loot.md`). Thay bằng phản hồi vật lý: khi 07 dừng xe 2 giây, vòng ăng-ten `SonarLoop` xoay về hướng có tín hiệu, đầu 07 liếc theo, bản thân góc kim loại của kỷ vật trong đống đổ nát lóe sáng phản quang (hoặc đèn cọc công trình chớp 2 lần) kèm âm thanh định hướng 3D. |
| **10. Gara Kenji ở phút thứ 5 cho kỷ vật ngay nhưng người chơi chưa có công cụ gì để mang về** | Thiết kế lại thành **Nhịp Gara Kenji 2 giai đoạn (*Two-Beat Rigging Gantry*)**: tại Gara Kenji có một cổng bảo trì cơ khí đang treo sẵn cụm **Tời cáp sau (`WinchDrum`)**. 07 lùi vào cổng để cánh tay máy lắp Tời cáp vào cản sau trước, rồi mới dùng chính tời đó móc kéo kiện kỷ vật đầu tiên về căn cứ. Sau khi mở Xưởng xe và lắp **Giá chở hàng (`CargoCradle`)**, các kỷ vật tiếp theo mới được nâng hẳn lên lưng. |

---

## 3. Vòng lặp và hệ thống (Core Loop & Systems)

### 3.1. Vòng lặp cốt lõi 3 tầng (Three-Loop Architecture)

1. **Vòng lặp từng phút (Micro Loop — 1 đến 3 phút):**
   Lái xe rời vùng sáng ấm của trạm → quan sát địa hình hoặc dừng xe 2 giây để ăng-ten `SonarLoop` bắt hướng tín hiệu → tiếp cận xác máy hỏng / mạch quặng / đống đổ nát → dùng đúng bộ phận cơ khí trên 07 (**Quét** bằng mắt, **Cắt** bằng tháp mũi, **Đào** bằng gầu trước, **Thu hàng** vào cửa khoang sau, hoặc **Kéo/Nâng** kỷ vật bằng tời và giá lưng) → kiểm tra pin và khoang hàng.
2. **Vòng lặp mỗi chuyến đi (Session Loop — 10 đến 20 phút):**
   Quay về Đế sạc hoặc Trụ sạc trước khi cạn pin → đổ vật liệu từ cửa khoang sau vào phễu Xưởng xe → chế tạo và lắp thiết bị mới lên thân 07 (hoặc đặt Bệ xây dựng ngoài thực địa) → đưa kỷ vật/băng ghi âm lên thang nâng bụng tàu để khôi phục nhật ký và đẩy tiến trình **Bảng Phục Hồi (`Restoration Board`)**.
3. **Vòng lặp toàn cục (Macro Loop — 6 vùng địa chất):**
   Mỗi thiết bị mới lắp lên 07 mở ra một cổng vật lý thật sang vùng kế tiếp → kích hoạt tháp tiếp sóng vùng đó để vẽ bản đồ địa hình, đánh thức hệ sinh thái máy móc địa phương và thu thập **1 trong 6 mảnh dữ liệu Khảo sát (`Survey 1/6 → 6/6`)** hướng tới việc sửa Chảo Đỉnh núi.

### 3.2. Năng lượng & Chu kỳ ánh sáng cực Nam (Energy & L1 South-Pole Lighting)
*(Trích dẫn: `0.6-living-moon.md`; `look-too-purple.md`, 2026-10-14)*

- **Mô hình vật lý thực tế tại Cực Nam Mặt Trăng (L1):** Mặt Trời luôn lướt sát đường chân trời (`1,5° – 6°`), không bao giờ đứng bóng. Một chu kỳ ánh sáng đầy đủ kéo dài **2 giờ chơi thực tế**:
  - **90 phút Ngày cực (*Look C — Low Sun*):** Bầu trời vẫn đen thẫm và có sao mờ, nhưng mặt đất xám bạc ấm rực dưới nắng xiên góc thấp và bóng đổ dài sắc nét.
  - **30 phút Đêm bóng râm (*Look A — Moon Grey*):** Mặt Trời khuất sau vành núi chính; lòng chảo chìm vào bóng tối mát lạnh dưới ánh sáng phản chiếu từ Trái Đất (*Earthshine*).
  - **Nhật thực (*Lunar Eclipse*):** Sự kiện đặc biệt khi Trái Đất che khuất Mặt Trời, vành khí quyển Trái Đất rực đỏ đồng và mặt đất chuyển sang sắc đỏ sẫm trong 4 phút.
- **Quy tắc sạc năng lượng hợp logic vật lý:**
  - **Tại Đế sạc căn cứ & các Trụ sạc (*Beacon Chargers*):** Sạc đầy **100%** dung lượng pin (tốc độ `+45%/giây`).
  - **Dưới ánh nắng trực tiếp (khi đã lắp Cánh Pin Mặt Trời ở `0.6`):** Khi 07 dừng xe dưới nắng, cánh pin mở ra và sạc chậm lại tối đa đến **mức dự phòng 1 ô pin (25%)** — đủ để không bao giờ bị kẹt cứng giữa đồng nắng, nhưng vẫn cần mạng lưới Trụ sạc để hoạt động hết công suất.
  - **Trong bóng tối / Ánh sáng Trái Đất (*Earthshine*):** Không bao giờ sạc được pin. Trong vùng Hố Bóng Tối vĩnh cửu (`0.8`), nhiệt độ `-200°C` làm pin tụt nhanh gấp **2 lần** cho đến khi lắp **Bộ Sưởi Pin (*Battery Heater*)**.
- **Khi cạn pin (0%) — Không bao giờ phạt xóa đồ:**
  07 không chết và không mất bất kỳ vật phẩm nào. Đèn pha tắt, 07 chuyển sang **Chế độ dự phòng (*Reserve Crawl*)** bò về ở tốc độ `35%`. Nếu đã sửa xong **Tilly**, Tilly sẽ chạy ra đón dọc đường, bật đèn dẫn đường và truyền điện cảm ứng giúp 07 lăn bánh về trạm ở tốc độ `55%`.

### 3.3. Khoang hàng & Kho nhà: Một màn hình cố định duy nhất (One-Screen Subnautica Inventory)
*(Trích dẫn: `ui-taste-subnautica.md`, 2026-10-12; `inventory-07-only.md`, 2026-10-13; `inventory-one-screen.md`, 2026-10-14)*

Chủ dự án đã khóa quy tắc: **Tuyệt đối không dùng giao diện nhiều tab, không có thanh cuộn và không vẽ ô đồ trôi lơ lửng theo xe 3D.**

- **Khi nhấn `Tab` ở bất kỳ đâu (Màn hình 07 — Khoang hàng & Thiết bị):**
  Mở một khung kính mờ tối giản (*Moonlight Glass*) cố định giữa màn hình, chia làm 2 nửa ngang nhau:
  - **Nửa trái — Lưới Khoang hàng của 07 (`8 ô` ban đầu, mở rộng lên `16 ô` trên cùng một lưới):** Hiển thị các ô vuông rõ ràng chứa vật liệu (`Kim loại`, `Thạch anh`, `Hợp kim Ilmenite`, `Nhôm Anorthite`, `Tinh thể Piezo`, `Băng tinh khiết`), linh kiện tháp (`TowerCoil`, `Transformer`, `RelaySpare`) và bộ phận bạn bè. Di chuột vào ô nào hiện tên và công dụng 1 dòng ở thanh chân trang.
  - **Nửa phải — Sơ đồ 07 & Các ổ lắp thiết bị cố định (*Equipment Paper-Doll*):** Hiển thị hình chiếc 07 từ góc 3/4 trên xuống với các ổ cắm cố định nối bằng đường chỉ mảnh tới đúng vị trí cơ khí trên thân xe:
    1. *Ổ Quang học & Ăng-ten (Đầu/Trái):* Thấu kính Quét Nhanh, Bộ Khuếch Đại Tín Hiệu, Đèn Pha Mạnh.
    2. *Ổ Mũi xe (Trước):* Mỏ Cắt & Hàn, Tụ Cắt Nhanh / Mỏ Cắt Nhiệt, Gầu Đào Trước, Tời Neo Mũi.
    3. *Ổ Khung gầm & Nguồn (Giữa):* Khung Gầm Cấp 1–2, Pin Cấp 1–3, Cánh Pin Mặt Trời, Bánh Từ Tính, Bộ Sưởi Pin.
    4. *Ổ Đuôi & Lưng xe (Sau):* Tời Cáp Sau, Giá Chở Hàng Lưng, Khoang Hàng Rộng (16 ô), Bộ Đẩy Nhảy.
- **Khi đỗ tại Đế sạc căn cứ và mở Kho nhà (`E` tại Locker hoặc `Tab` khi đang đỗ sạc):**
  Vẫn là một màn hình cố định hai cột: **Bên trái là Lưới Khoang hàng của 07 (8/16 ô)**, **Bên phải là Kệ Kho nhà của Trạm Lumen (`24 ô`)**. Click chuột trái một lần để chuyển tức thì món đồ giữa xe và kho nhà.
- **Hàng đợi chế tạo ghim góc màn hình (*Pinned Recipe Hint*):**
  Khi chọn ghim 1 bản vẽ tại Xưởng xe, góc trên phải chỉ hiện đúng 1 dòng nhỏ: `Giá chở hàng: Kim loại 2/3` — tự ẩn ngay khi chế tạo xong.

### 3.4. Quét mảnh bản vẽ, Chế tạo & Lắp đặt (Scan Fragments → Craft → Equip)
*(Trích dẫn: `scan-fragments.md`, 2026-10-12; `equipment-07.md`, 2026-10-13)*

1. **Quét mảnh bản vẽ (*Fragment Scanning*):**
   Các thiết bị bị vỡ nằm rải rác tại các xác máy, trạm khảo sát cũ và khu đổ nát. Quét bằng mắt quang học của 07 (`EyeOrigin`). Số mảnh cần quét phụ thuộc vào độ phức tạp của công nghệ (`2–4 mảnh`). Nếu quét một mảnh của bản vẽ đã hoàn thành: thưởng ngay **`+3 Kim loại`** (tự cuốn vào khoang hàng sau nếu còn chỗ).
2. **Chế tạo tại Xưởng xe (*Vehicle Bay*) & Đặt Bệ xây dựng (*Build Pads*):**
   - Người chơi lùi 07 vào phễu Xưởng xe, cửa khoang sau `CargoHatch` mở ra phóng đủ số vật liệu vào phễu.
   - Cần cẩu trần của Xưởng xe hạ xuống lắp trực tiếp thiết bị mới lên đúng ổ trên thân 07 (kèm hoạt cảnh `1,2 s`, tia lửa hàn nhẹ và câu nhạc chirp 4 nốt).
   - Với công trình ngoài thực địa (**Trụ sạc**, **Đèn chỉ đường**, **Cầu vượt khe nứt**, **Trạm thời tiết phụ**), 07 dừng trước các **Bệ xây dựng có sẵn khung móng (*Pre-sited Build Pads*)**, mở cửa khoang sau nạp vật liệu để khung công trình tự dựng lên và nối cáp vào mạng lưới.
3. **Bảng đầy đủ 15 Thiết bị & Nâng cấp của 07 từ đầu game đến `1.0`:**

| # | Tên thiết bị (Tiếng Việt / English) | Phiên bản | Số mảnh quét | Công thức chế tạo tại Xưởng xe | Ổ lắp trên thân 07 (`Equipment Paper-Doll`) | Tác dụng cơ khí cụ thể |
|---|---|---|---|---|---|---|
| 1 | **Tời Cáp Sau** (*Rear Winch*) | `0.5.6` | *Lắp sẵn tại cổng* | *Miễn phí tại Cổng Gara Kenji* | Ổ Đuôi xe (`WinchOrigin`) | Bắn móc cáp kéo lê kỷ vật trên mặt đất (`55%` tốc độ) & thu/nhả dây `Q/E`. |
| 2 | **Giá Chở Hàng Lưng** (*Cargo Cradle & Hoist*) | `0.5.6` | `2 mảnh` | `3 Kim loại` | Ổ Lưng xe (`CradleOrigin`) | Cần trục chữ A nâng kỷ vật đặt lên lưng, chạy `100%` tốc độ, vào được thang nâng tàu. |
| 3 | **Thấu Kính Quét Nhanh** (*Fast Scan Lens*) | `0.5.6` | `2 mảnh` | `2 Kim loại + 2 Thạch anh` | Ổ Quang học (`EyeOrigin`) | Giảm thời gian quét `2,0 s → 1,1 s`, tăng tầm quét `6 m → 9 m`. |
| 4 | **Mỏ Cắt & Hàn Mũi Xe** (*Plasma Cutter*) | `0.5.6` | `3 mảnh` | `3 Kim loại + 2 Thạch anh` | Ổ Mũi xe (`CutterOrigin` + `Stage_1`) | Cắt các mối hàn cam cấp 1 trên vách sắt, thùng kẹt, dầm cầu. |
| 5 | **Bộ Khuếch Đại Tín Hiệu** (*Signal Amp*) | `0.5.6` | `2 mảnh` | `2 Kim loại + 3 Thạch anh` | Ổ Ăng-ten (`AntennaOrigin`) | Tăng tầm nghe sonar khi dừng xe từ `180 m` lên `320 m`, tách âm sắc rõ. |
| 6 | **Gầu Đào Răng Cưa** (*Mechanical Dig Scoop*) | `0.5.6` | `2 mảnh` | `4 Kim loại + 2 Thạch anh` | Ổ Cản trước (`ScoopOrigin`) | Xúc 3 nhịp cơ khí đào kỷ vật và linh kiện bị vùi dưới đất đá. |
| 7 | **Khoang Hàng Rộng 16 Ô** (*Side Panniers*) | `0.5.6` | `3 mảnh` | `4 Kim loại + 2 Thạch anh` | Ổ Hông sau (`SidePanniers_L/R`) | Gắn 2 hộc hông đối xứng, tăng sức chứa khoang hàng từ `8` lên `16 ô`. |
| 8 | **Pin Cấp 1–3** (*Battery Cells Mk1–Mk3*) | `0.5.6–0.8` | `2 / 3 / 3 mảnh` | Mk1: `3 Kim loại + 2 Thạch anh`; Mk2: `+2 Hợp kim`; Mk3: `+2 Tinh thể` | Ổ Nguồn sau (`BatteryPack`) | Gắn thêm lõi pin vật lý, tăng thời lượng hoạt động `100 s → 150 s → 210 s → 280 s`. |
| 9 | **Khung Gầm Cấp 1–2** (*Chassis Mk1–Mk2*) | `0.5.6–0.6` | `2 / 3 mảnh` | Mk1: `4 Kim loại`; Mk2: `3 Hợp kim + 2 Thạch anh` | Ổ Khung gầm (`Suspension`) | Tăng tốc độ tối đa `8 m/s → 10,5 m/s → 13 m/s` và giảm xóc địa hình. |
| 10 | **Bộ Đẩy Nhảy** (*Jump Thrusters*) | `0.5.6` | `3 mảnh` | `4 Kim loại + 3 Thạch anh` | Ổ Đuôi sau (`BoostThrusters`) | Phun khí nén giúp 07 nhảy vọt qua khe nứt hẹp và gờ đá thấp. |
| 11 | **Cánh Pin Mặt Trời** (*Solar Canopy*) | `0.6` | `3 mảnh` | `3 Hợp kim + 3 Thạch anh` | Ổ Lưng trên (`SolarPanels_L/R`) | Tự mở 2 cánh pin khi đỗ dưới nắng cực L1, sạc hồi phục tới mức dự phòng `25%` (1 ô). |
| 12 | **Tụ Cắt Nhanh & Mỏ Cắt Nhiệt** (*Capacitor & Thermal Lance*) | `0.6–0.7` | `3 / 4 mảnh` | Tụ: `3 Hợp kim + 2 Thạch anh`; Nhiệt: `4 Nhôm + 3 Hợp kim` | Ổ Mũi xe (`ChargeConduits Stage_2/3`) | Sáng đủ 3 đốt tụ điện boong trước; cắt nhanh gấp đôi và mở được khoang hợp kim dày (Kestrel-3, Meridian). |
| 13 | **Bánh Từ Tính** (*Mag-Wheels*) | `0.7` | `3 mảnh` | `4 Nhôm + 3 Hợp kim` | Ổ Bánh xe (`WheelRims`) | Bám chặt sườn đá dốc `45°–50°` ở Bậc thềm vành hố và đường sắt từ tính. |
| 14 | **Bộ Chuyển Đổi Chuẩn Sạc** (*Cross-Standard Adapter*) | `0.7` | `2 mảnh` | `2 Nhôm + 2 Hợp kim + 2 Thạch anh` | Ổ Gầm xe (`InductiveCoil`) | Cho phép 07 sạc và cấp nguồn cho toàn bộ đế lục giác của Trạm Meridian. |
| 15 | **Đèn Pha Mạnh, Bộ Sưởi Pin & Tời Neo Mũi** (*High-Beam, Heater & Anchor*) | `0.8` | `2 / 3 / 3 mảnh` | Đèn: `2 Nhôm + 3 Thạch anh`; Sưởi: `3 Hợp kim + 2 Tinh thể`; Neo: `4 Nhôm + 2 Hợp kim` | Ổ Đầu & Mũi xe | Chiếu sáng `80 m` trong Hố Bóng Tối, chống tụt pin ở `-200°C`, và thả cáp neo xuống vách Ống Dung Nham. |

### 3.5. Bạn robot đồng hành (Robot Companions)
*(Trích dẫn: `polish-every-state.md`, 2026-10-12; `story-logic.md`, 2026-10-13; `0.5.5-owner-feedback.md`, mục #9)*

Mỗi người bạn là một robot chuyên dụng bị hỏng hoặc kẹt trong thế giới. Tìm đủ linh kiện trong các đống đổ nát gần đó và dùng mỏ hàn mũi xe sửa xong, bạn sẽ **tự đứng dậy vận động cơ khí, chirp chào 07 và bắt đầu công việc thật của mình** (không bao giờ rơi ra thẻ ký ức):

| Bạn robot | Vị trí & Cách cứu | Công việc thật giúp 07 sau khi thức dậy |
|---|---|---|
| **1. Tilly** *(Rover bảo trì nhỏ — 0.5)* | Cạnh Xưởng xe; tìm 3 bộ phận quanh Lòng chảo (bánh xe ở giàn giáo, ăng-ten ở thùng sườn đồi, lõi nguồn dưới vách tấm pin sau khi có Mỏ cắt). | Quét dọn sân trạm, dẫn đường tới linh kiện gần nhất trong bán kính `60 m`, và chạy ra tiếp điện dẫn 07 về nhà ở tốc độ `55%` khi 07 cạn pin. |
| **2. Bell** *(Drone âm thanh khảo sát — 0.5)* | Hẻm Thì Thầm; tìm 3 bộ phận (1 ở hộc trạm gác, 1 sau vách sắt cắt được, 1 **vùi dưới sườn đá lở phải dùng Gầu đào `DigScoop` xúc lên**). | Bay lượn quanh cột ăng-ten chính, mở rộng tầm phát hiện kỷ vật quanh trạm và ngân tiếng chuông báo hiệu mỗi khi cửa sổ liên lạc Trái Đất mở. |
| **3. Atlas** *(Xe tải hạng nặng 8 bánh — 0.7)* | Bậc thềm vành hố (gần Trạm Meridian); kẹt dưới dầm cầu sập. Cần **Mỏ cắt nhiệt** cắt dầm và **Pin cấp 2 + Hợp kim** để khởi động lại. | Tự động chạy tuyến vận chuyển dọc sườn núi, chở các kiện quặng lớn từ mỏ Bậc thềm về thẳng Kho nhà Lumen mỗi 8 phút và làm trạm sạc di động trên đèo. |
| **4. Moss** *(Robot chăm sóc sinh học — 0.8)* | Vườn ươm ngầm trong Ống Dung Nham; cần nối lại đường ống dẫn nước băng và thay thấu kính quang phổ. | Chăm sóc các lồng rêu Mặt Trăng ngủ đông (*Lunar Bryophyte*), bật đèn quang hợp xanh lục ấm làm sống lại toàn bộ khu vườn ngầm và mở mảnh Khảo sát thứ 5. |
| **5. Đàn Drone Chị Em (*Sister Drones* — 0.6–0.8)** | 5 drone khảo sát nhỏ bị kẹt rải rác ở các vùng; chỉ cần 1 nhịp cắt hoặc hàn nhanh để giải cứu. | Bay về đậu quanh mái vòm căn cứ Lumen, thắp sáng các cọc tiêu dọc đường cái và làm căn cứ đông vui, ấm cúng. |

### 3.6. Nghe Sonar tự nhiên (Passive Grounded Sonar)
*(Trích dẫn: `0.5.5-owner-feedback.md`, mục #10; `no-floating-loot.md`, 2026-10-14; `rover-07-v2.md`, 2026-10-14)*

- **Cơ chế kích hoạt:** Không có phím bấm riêng (đã xóa phím `Space`). Khi 07 di chuyển ít nhất `30 m` rồi dừng yên `2,0 s`, hoặc khi người chơi đứng ngắm cảnh từ một mỏm đá cao, 07 tự động lắng nghe.
- **Biểu hiện cơ khí & âm thanh (Không cột sáng trời, không vòng sáng mặt đất):**
  1. Vòng ăng-ten định hướng `SonarLoop` trên lưng trái 07 quay một vòng rồi khóa thẳng vào hướng có mục tiêu gần nhất trong tầm (`180 m`, hoặc `320 m` khi có `Signal Amp`).
  2. Đầu camera của 07 liếc nhẹ về hướng đó trong `1,2 s`.
  3. Tại vị trí mục tiêu thật: nếu là kỷ vật/mảnh vỡ, **chính cạnh kim loại của vật thể đó** lóe sáng phản quang 2 nhịp; nếu là công trình/tháp, **đèn cọc trên đỉnh công trình** chớp 2 nhịp vàng ấm.
  4. Âm thanh phản hồi phát ra đúng hướng 3D (tai trái/phải) với âm sắc phân biệt rõ (kỷ vật = tiếng đồng ngân trầm; bản vẽ = tiếng pha lê thanh; công trình = tiếng xung đôi).

### 3.7. Sổ ghi chép Khảo sát (Survey Databank)
*(Trích dẫn: `scan-fragments.md`, 2026-10-12; `plain-functional-text.md`, 2026-10-12; `0.6-living-moon.md`)*

Mỗi khi 07 quét xong một công nghệ mới, một hiện tượng địa chất, một máy móc thuộc hệ sinh thái hoặc một địa điểm, mục tương ứng được ghi vào **Sổ ghi chép (`Databank` — phím `J`)** chia làm 4 nhóm gọn gàng:
1. **Máy móc (*Machines*):** Các thiết bị của Lumen (đế sạc tròn cảm ứng) và của Meridian (đế sạc lục giác tiếp xúc), robot dọn bụi, xe khoan, vệ tinh.
2. **Mặt Trăng (*The Moon*):** Đá bazan, quặng Ilmenite, Anorthite, tinh thể áp điện, băng vĩnh cửu, chu kỳ nắng cực L1.
3. **Địa điểm (*Places*):** Các khu vực trong 6 vùng, ghi chú kỹ thuật của Kenji, Dr. Moreau và Ines (1–2 câu ngắn gọn, thực tế).
4. **Sự sống (*Life*):** Rêu Mặt Trăng ngủ đông (*Cryo-bryophyte*), vi khuẩn đá và thí nghiệm nhà kính.

### 3.8. Giao diện & Âm thanh (Moonlight Glass UI & Acoustic Pentatonic Audio)
*(Trích dẫn: `ui-taste-subnautica.md`, 2026-10-12; `plain-functional-text.md`, 2026-10-12; `audio-feedback-055.md`, 2026-10-14)*

- **Giao diện (*Moonlight Glass*):**
  - Màn hình khi lái xe cực kỳ sạch: góc dưới trái là thanh Pin ngang mảnh và số ô Khoang hàng (`3/8`); trên đầu màn hình là la bàn mảnh chỉ hướng Bắc/Căn cứ và các tháp đã bật; góc dưới giữa chỉ hiện tối đa **1 dòng nhắc duy nhất** (`HudMutex`).
  - **Đã xóa vĩnh viễn:** Vòng tròn pin 3D vẽ dưới bánh xe (`RoverEnergyRing`), bộ đếm mảnh bản vẽ ghim cố định góc dưới phải, các nhãn chữ trôi lơ lửng từ xa, và mọi hướng dẫn lặp lại.
- **Âm thanh (*Acoustic Pentatonic — D Major: D, E, F#, A, B*):**
  - **Đã xóa vĩnh viễn:** Mọi tiếng bíp điện tử chói tai kiểu "AI khoa học viễn tưởng", tiếng chuông báo thức gắt khi nhặt đồ, tiếng vo ve tần số cao.
  - Thay bằng tiếng cơ khí vật lý ấm áp (tiếng mô-tơ điện êm, tiếng bánh răng, tiếng chốt cửa khoang hàng *clack*, tiếng đá sỏi lạo xạo dưới bánh xe) kết hợp với giọng chirp 2–4 nốt ngũ cung ấm của 07 và tiếng đàn kalimba / gỗ / dây trầm khi hoàn thành công trình.

---

## 4. Thế giới và lộ trình khám phá (Seamless World & Regional Progression)

*(Trích dẫn: `world-regions.md`, 2026-10-13; `living-moon.md`, 2026-10-13; `derelict-variety.md`, 2026-10-13; `0.6-world-regions.md`; `0.6-living-moon.md`)*

### 4.1. Kiến trúc một thế giới liền mạch (Single Seamless World)

- **Không có màn hình nạp cảnh (*Zero Loading Screens*) và không có tường tàng hình:** Toàn bộ Mặt Trăng là một không gian liên tục duy nhất, được chia thành lưới các ô địa hình `64 × 64 m` do `WorldStreamer` tự động nạp trước trong bán kính `384 m` quanh 07.
- **Bảo toàn nguyên vẹn vùng Lòng chảo gốc:** Toàn bộ khu vực bán kính `r ≤ 345 m` (Lòng chảo Lumen & Hẻm Thì Thầm) được giữ nguyên từng tọa độ công trình và kỷ vật. Từ `r = 345 m` đến `400 m` là vành đai chuyển tiếp mượt sang các vùng mới.
- **Mở khóa theo tính liền kề (*Adjacency Unlocking*) & Lưu vị trí thật:**
  Thắp sáng Tháp tiếp sóng của vùng nào sẽ quét mở địa hình vùng liền kề tiếp theo. Khi người chơi thoát game tại bất kỳ Đế sạc hoặc Trụ sạc nào, lần vào lại tiếp theo 07 sẽ thức dậy ngay tại đúng Trụ sạc đó (`lastChargerNodeId` + `position`).
- **Mật độ điểm thú vị (*Point of Interest Density*):**
  Cứ mỗi **20–30 giây lái xe (`100–150 m`)** phải có ít nhất một điểm thu hút thị giác (xác máy khảo sát, mạch khoáng sản, bệ xây dựng, trạm đo đạc cũ, hoặc hiện tượng tự nhiên). Tất cả các xác máy đều phải đa dạng câu chuyện (vệ tinh rơi nghiêng, giàn khoan gãy mũi, xe trượt tuyết lật, lều bơm hơi xẹp, cột ăng-ten đổ) — không bao giờ đặt các thùng hàng giống hệt nhau.

### 4.2. Sáu vùng địa chất từ Lòng chảo đến Đỉnh núi

| Vùng & Phiên bản | Địa hình & Khí quyển | Mồi nhử nhìn thấy từ trước (*Visual Lure*) | Cổng điều kiện vật lý (*Real Gate*) | Tài nguyên & Công nghệ mới | Hệ sinh thái máy móc & Hiện tượng tự nhiên | Mảnh Khảo sát (*Survey Piece*) |
|---|---|---|---|---|---|---|
| **1. Lòng Chảo & Hẻm Thì Thầm** *(The Basin & Whispering Canyon — `0.5 / 0.5.6`)* | Miệng hố phẳng `700 × 700 m` bao quanh Trạm Lumen, nối với hẻm núi đá hẹp phía Đông. | Đèn đỏ chớp trên Đỉnh núi; xác vệ tinh **Kestrel-3** cắm nghiêng cách trạm `160 m`; đèn vàng mờ ở cổng Gara Kenji. | Cáp thang nâng đứt (sửa từ đầu) → Vách sắt Bãi đáp cần **Mỏ cắt** → Đá lở vùi bộ phận của Bell cần **Gầu đào**. | `Kim loại (Metal)`, `Thạch anh (Quartz)`. Mở khóa: Tời sau, Giá chở hàng, Quét nhanh, Mỏ cắt, Gầu đào, Khoang hàng 16 ô, Pin 1, Khung gầm 1, Bộ đẩy nhảy. | **Họ máy Lumen** (sơn trắng ngà – vàng đồng, đế sạc tròn cảm ứng): Tilly, Bell, bàn xoay pin mặt trời trên mái trạm tự hướng theo Mặt Trời sau khi có điện. | **Khảo sát 1/6:** Nhật ký sơ tán Lumen & Bản đồ thăm dò ban đầu (tại Tháp tiếp sóng Hẻm núi). |
| **2. Khe Rãnh & Biển Dài** *(Rille Pass & Long Mare — `0.6`)* | Khe nứt bazan dẫn ra đồng bằng dung nham cổ rộng `1,2 km` màu xám đen phẳng lặng, tầm nhìn cực thoáng. | Từ vành Đông Lòng chảo nhìn thấy **Dàn Ăng-ten Sâu (*Deep-Space Dish Array*)** khổng lồ nằm im lìm giữa cánh đồng bazan. | Khe nứt tại Rille Pass bị chắn bởi dầm cầu sập (cần **Mỏ cắt** + nạp vật liệu vào Bệ cầu hoặc dùng **Bộ đẩy nhảy**) và cự ly xa vượt quá 1 bình pin gốc. | `Quặng Ilmenite` → tinh luyện thành `Hợp kim (Alloy)`. Mở khóa: **Cánh Pin Mặt Trời**, **Tụ Cắt Nhanh**, **Pin Cấp 2**, **Khung Gầm Cấp 2**. | **Robot quét bụi (*Solar Sweepers*)** chạy dọc các dãy tấm pin ở Biển Dài. Một con bị kẹt bánh vào năm 2067 làm cả dàn pin phủ bụi tắt nguồn — khi 07 cắt gỡ thanh sắt kẹt, nó chạy lại, quét sạch bụi và bật sáng cả cánh đồng pin! Kèm hiện tượng **Bụi tĩnh điện lơ lửng (*Electrostatic Dust Levitation*)** khi nắng xiên qua ranh giới sáng–tối. | **Khảo sát 2/6:** Nhật ký đường truyền năm 2067 tại Dàn Ăng-ten Sâu (giải thích vì sao Mặt Trăng mất tiếng). |
| **3. Bậc Thềm Vành Hố & Trạm Meridian** *(Rim Terraces — `0.7`)* | Những bậc đá trắng Anorthite dốc đứng `35°–50°` uốn quanh sườn núi phía Bắc, nhìn xuống toàn cảnh Lòng chảo. | Từ Biển Dài nhìn lên sườn núi thấy các tháp khung lục giác sơn xanh lam đậm và cần cẩu của **Trạm Meridian** (*Tycho Consortium*). | Dốc đá trơn `45°` làm bánh thường trượt lùi (bắt buộc lắp **Bánh Từ Tính — *Mag-Wheels***) và cửa khoang kín cần **Mỏ Cắt Nhiệt (*Thermal Lance*)**. | `Đá Anorthite` → `Nhôm nhẹ (Aluminium)`. Mở khóa: **Bánh Từ Tính**, **Mỏ Cắt Nhiệt**, **Bộ Chuyển Đổi Chuẩn Sạc (*Cross-Standard Adapter*)**, cứu **Atlas**. | **Họ máy thứ hai — Meridian** (khung lục giác góc cạnh, sơn xanh xám công nghiệp, **đế sạc lục giác tiếp xúc** khác hẳn đế tròn của Lumen). Khi 07 lắp Bộ chuyển đổi và đóng điện Trạm Meridian, các giàn khoan tự động và xe tải **Atlas** thức dậy. | **Khảo sát 3/6:** Hồ sơ khoan thăm dò độc lập của Tycho tại Trạm Meridian (xác nhận mạch nước ngầm từ sườn Bắc). |
| **4. Hố Bóng Tối Vĩnh Cửu** *(Shadowed Crater — `0.8`)* | Lòng hố sâu phía Nam không bao giờ có ánh nắng Mặt Trời chiếu tới suốt hàng tỷ năm; sương giá trắng xóa và bóng tối đặc quánh. | Vành hố phủ băng lấp lánh dưới ánh sao và những cọc đèn dẫn đường màu xanh băng của đoàn thám hiểm Ines cắm dẫn xuống đáy hố. | Bóng tối tuyệt đối + cái lạnh `-200°C` rút cạn pin gấp đôi. Bắt buộc phải có **Đèn Pha Mạnh (*High-Beam Lamp*)** và **Bộ Sưởi Pin (*Battery Heater*)**. | `Lõi Băng Tinh Khiết (Pure Ice Cores)`. Mở khóa: **Đèn Pha Mạnh**, **Bộ Sưởi Pin**, **Pin Cấp 3**. | Các **Mũi khoan nhiệt (*Thermal Drills*)** của Ines đóng băng nửa chừng. Khi 07 cấp nguồn sưởi lại cho giàn khoan trung tâm, hơi nước ấm bốc lên làm tan lớp băng phủ trên kho dữ liệu hiện trường của Ines. | **Khảo sát 4/6:** Dữ liệu quang phổ lõi băng gốc của Ines Kalani (ghi ngay trong đêm bão mặt trời 2047). |
| **5. Ống Dung Nham Ngầm** *(Lava Tube & Cryo-Vent — `0.8`)* | Hang vòm bazan khổng lồ chạy ngầm dưới lòng đất nối từ đáy Hố Bóng Tối sang chân Đỉnh núi; vách hang đính tinh thể áp điện. | Luồng hơi băng lấp lánh thoát ra từ cửa hang vòm và hàng dây cáp đồng cổ dẫn sâu vào lòng núi. | Vách vực thẳng đứng ở cửa ống dung nham cần **Tời Neo Mũi Xe (*Anchor Winch*)** để thả cáp hạ 07 xuống an toàn. | `Tinh thể Áp Điện (Piezo Crystal)`. Cứu robot sinh học **Moss** và khôi phục **Vườn ươm rêu ngầm (*Subsurface Bryophyte Dome*)**. | **Hiện tượng Tinh thể Ngân (*Singing Piezo Crystals*):** Khi sóng địa chấn nhẹ đi qua vách hang, các cụm tinh thể phát sáng dịu và ngân hợp âm ngũ cung. Khi Moss bật lại hệ thống sưởi và nước băng, các lồng **Rêu Mặt Trăng (*Lunar Bryophyte*)** mở lá xanh lục dưới ánh đèn ấm. | **Khảo sát 5/6:** Mẫu sinh học Rêu ngủ đông & Bản sao lưu Hầm trú ẩn (*Bunker Backup Drive*). |
| **6. Đường Lên Đỉnh & Chảo Lớn** *(Peak Road & The Peak — `1.0`)* | Con đường xoắn ốc quanh sườn núi cao nhất (`+240 m`), đi qua các khe vực hẹp, dốc từ tính và cổng vòm đá để lên tới đài quan sát Đỉnh núi. | **Chảo Ăng-ten Đỉnh Núi (*Peak Dish*)** khổng lồ với đốm đèn đỏ chớp tắt đã nhìn thấy từ giây đầu tiên của trò chơi. | Bài kiểm tra tổng hợp mọi bộ phận trên 07: leo dốc bằng **Bánh từ tính**, vượt khe vực bằng **Bộ đẩy nhảy / Tời neo**, cắt khóa cơ khí bằng **Mỏ cắt nhiệt**, và nạp đủ **6/6 mảnh Khảo sát**. | Không thêm vật liệu cày cuốc mới — tập trung hoàn toàn vào cảm xúc chinh phục, sửa chữa Chảo lớn và phát sóng về Trái Đất. | Khi 07 đóng cầu dao cuối cùng, toàn bộ chuỗi Tháp tiếp sóng từ Lòng chảo → Biển Dài → Bậc thềm → Đỉnh núi sáng rực nối tiếp nhau; Chảo lớn xoay hướng thẳng về phía Trái Đất. | **Khảo sát 6/6:** Nhật ký cuối cùng tại Đài quan sát Đỉnh núi — hoàn tất gói dữ liệu gửi về Trái Đất. |

### 4.3. Chi tiết 6 nhịp trong Giờ Chơi Đầu Tiên tại Lòng Chảo (`First-Hour 6-Beat Flow` ở `0.5.6`)

| Nhịp (Thời gian) | Việc người chơi làm bằng cơ khí thật của 07 | Phần thưởng nhìn thấy ngay |
|---|---|---|
| **Nhịp 1 (`0:00–3:00`): Thức dậy & Hàn cáp Thang nâng** | 07 mở màn trập trên Đế sạc → lái `35 m` tới chân tàu chính → dùng kim hàn mũi xe (`StitchOrigin`) chấm 3 mối cáp đứt của thang nâng bụng tàu (`BellyLift`) → nghe đoạn băng đầu tiên của Kenji & Ro. | Thang nâng bụng tàu hạ xuống; đèn vàng ấm quanh tàu bật sáng; la bàn hiện trên HUD. |
| **Nhịp 2 (`3:00–8:00`): Vệ tinh Kestrel-3 & Gara Kenji** | Lái `95 m` tới **Gara Kenji**: lùi vào Cổng bảo trì để lắp **Tời cáp sau (`WinchDrum`)** → móc dây cáp kéo lê kiện hàng đầu tiên về trạm; ghé xác vệ tinh **Kestrel-3** (`160 m`) quét mảnh bản vẽ đầu tiên và thấy phù hiệu lạ *Meridian*. | Có Tời cáp sau trên xe; nhận 3 Kim loại đầu tiên từ kiện hàng; mở câu hỏi lớn về Trạm Meridian. |
| **Nhịp 3 (`8:00–18:00`): Mở Xưởng Xe & Lắp Giá Chở Hàng** | Mở cửa khoang sau (`HatchOrigin`) phóng `4 Kim loại` vào phễu **Xưởng xe (`Vehicle Bay`)** → quét đủ 2 mảnh Giá chở hàng → chế tạo và để cần cẩu lắp **Giá chở hàng (`CargoCradle` + `HoistBail`)** lên lưng 07 → nâng kỷ vật đầu tiên (Mũ bảo hộ của Kenji) lên lưng đưa vào thang nâng bụng tàu. | Mái vòm Xưởng xe sáng đèn ấm; 07 có cần trục chữ A và đệm chở hàng trên lưng; chở kỷ vật ở `100%` tốc độ. |
| **Nhịp 4 (`18:00–30:00`): Trung Tâm Liên Lạc & Tháp Tiếp Sóng 1** | Nạp vật liệu mở **Trung tâm Liên lạc (`Comms Hub`)** → **Bảng Phục Hồi 4 bước** bật sáng → nhặt Cuộn dây tháp (`TowerCoil`) vào **Khoang hàng sau** (1 ô) → chạy lên Đồi Bắc, nạp vào **Hộc chân Tháp tiếp sóng 1** và hàn cáp kích hoạt tháp. | Cáp vàng nối sáng từ trạm lên Đồi Bắc; bản đồ mở vùng phía Tây; mở khóa công thức **Mỏ cắt mũi xe (`Plasma Cutter`)**. |
| **Nhịp 5 (`30:00–45:00`): Chế tạo Mỏ Cắt, Cứu Tilly & Tháp 2** | Quét 3 mảnh Mỏ cắt ở Bãi đáp vỡ → lắp `CutterTurret` lên mũi xe → cắt tấm vách sắt lấy lõi nguồn thứ 3 để sửa **Tilly** → cắt rào chắn đường vào Rãnh Tây, sửa **Tháp tiếp sóng 2**. | **Tilly** thức dậy chạy quanh sân và đi theo hỗ trợ 07; mở khóa công thức **Gầu đào trước (`DigScoop`)** và **Khoang hàng 16 ô**. |
| **Nhịp 6 (`45:00–60:00`): Lắp Gầu Đào, Cứu Bell & Tháp Hẻm Núi** | Lắp `DigScoop` ở cản trước → đi vào **Hẻm Thì Thầm**, dùng gầu đào xúc đống đá lở lấy Chuông cộng hưởng để sửa **Bell** → sửa **Tháp tiếp sóng 3** trên đỉnh hẻm núi → nhận **Mảnh Khảo sát 1/6** và nhìn thấy cánh đồng **Biển Dài (`0.6`)** mở ra phía Đông. | **Bell** bay lên đậu ở cột ăng-ten trạm; cả 3 tháp Lòng chảo nối mạng hoàn chỉnh; cổng sang vùng `0.6` mở rộng. |

---

## 5. Cốt truyện và cách kể chuyện (Story v3 & Non-Melodramatic Delivery)

*(Trích dẫn: `story-logic.md`, 2026-10-13; `plain-functional-text.md`, 2026-10-12; `Logs/sources/story-v3.md`; `docs/design/text-audit-0.5.5.md`)*

### 5.1. Mạch truyện chính (The Core Mystery & Resolution)

1. **Bối cảnh năm 2047 (Cuộc sơ tán vội vã):**
   Trạm Lumen là cơ sở khảo sát cực Nam của Cơ quan Khảo sát Mặt Trăng (*Lunar Survey Authority*). Tháng 10/2047, một cơn bão mặt trời cấp X40 ập tới buộc phi hành đoàn 4 người (**Trưởng trạm Dr. Elise Moreau**, **Kỹ sư trưởng Kenji Sato**, **Nhà địa chất Ines Kalani**, và **Kỹ sư thông tin trẻ Ro**) phải lên tàu rời đi trong vòng 40 phút. Năm tiếp theo (2048), Ủy ban ngân sách trên Trái Đất tuyên bố khu vực cực Nam khô cằn, cắt toàn bộ kinh phí và hủy chuyến tàu quay lại.
2. **Phát hiện bị chôn vùi của Ines Kalani:**
   Ngay trước khi lệnh sơ tán vang lên, mũi khoan sâu của Ines ở Hố Bóng Tối và Ống Dung Nham đã xuyên qua lớp đá bazan và chạm vào một **tầng băng nước tinh khiết khổng lồ kèm các bào tử rêu cổ đại đang ngủ đông**. Vì bão mặt trời làm sập lưới điện tiếp sóng, toàn bộ dữ liệu gốc bị kẹt lại trong các ổ lưu trữ địa phương.
3. **Trạm thứ hai — Meridian Station (Tycho Consortium):**
   Cách Lumen không xa về phía Bắc là **Trạm Meridian**, một trạm khoan tự động hoàn toàn do tập đoàn Tycho vận hành với triết lý thiết kế máy móc khác hẳn (khung lục giác, sơn xanh công nghiệp). Cơn bão năm 2047 đã đánh rơi vệ tinh chuyển tiếp **Kestrel-3** của Meridian ngay sát Lòng chảo Lumen. Khi 07 mở được khoang Kestrel-3 và leo lên Bậc thềm vành hố, người chơi phát hiện các mũi khoan của Meridian cũng đã xác nhận chính xác túi nước ngầm đó.
4. **Vì sao năm 2067 đường truyền im bặt?**
   Sau khi về Trái Đất, cô kỹ sư trẻ **Ro** (nay đã lớn tuổi) vẫn giữ chiếc máy thu phát sóng ngắn tại câu lạc bộ vô tuyến của mình và đều đặn gửi tín hiệu hỏi thăm vào mỗi kỳ cửa sổ liên lạc. Suốt 20 năm (2047–2067), hệ thống tự động của Lumen vẫn trả lời bằng nhịp xung báo cáo trạng thái. Nhưng vào năm **2067**, một con robot quét bụi (*Solar Sweeper*) tại Dàn Ăng-ten Biển Dài bị kẹt bánh vào thanh giằng gãy; bụi Mặt Trăng phủ kín mảng pin mặt trời khiến trạm phát mất nguồn hoàn toàn. Trái Đất tưởng rằng mọi thiết bị trên Mặt Trăng đã chết hẳn.
5. **Vai trò của các tiền bối 01–06 và cái kết của 07:**
   Sáu chiếc rover đi trước (**01 đến 06**) không hề bị "bỏ rơi trong đau khổ"; chúng là những cỗ máy khảo sát đã hoàn thành phần việc của mình ở từng vùng (giữ cửa xả áp, neo dây cáp, giữ nguồn sưởi cho vườn rêu, dẫn đường lên đỉnh) trước khi cạn kiệt linh kiện. 07 đi theo dấu bánh xe của 01–06, thu thập đủ **6 mảnh dữ liệu Khảo sát**, sửa lại Chảo Đỉnh núi và phát đi gói bằng chứng hoàn chỉnh (dữ liệu của cả Lumen lẫn Meridian). **Ro** bắt được tín hiệu đầu tiên, chuyển tiếp thẳng cho trung tâm **Houston**, và màn hình kết thúc bằng thông báo xác nhận: **Ủy ban mở lại sứ mệnh tàu có người lái quay về Trạm Lumen**. Sau đoạn kết, người chơi tiếp tục tự do lái 07 khám phá toàn bộ Mặt Trăng.

### 5.2. Quy tắc viết lời thoại và văn bản: Không ủy mị, không nhân cách hóa máy móc

Chủ dự án đã khóa quy tắc trong `story-logic.md` (2026-10-13):
- **Con người nói chuyện với con người; đối xử với robot như những thiết bị cơ khí quý giá:**
  Trong mọi băng ghi âm và nhật ký, Kenji, Moreau, Ines và Ro trao đổi công việc với nhau. Họ có thể khen *"Con 07 giữ ổn định áp suất tốt đấy"* hoặc ghi sổ bảo trì *"Nhớ thay vòng bi cho cụm tời của chiếc số 3"*, nhưng **tuyệt đối không** nói chuyện tâm tình với robot như với người hay thú cưng (*"Tilly bé nhỏ tội nghiệp của chị, đừng sợ bóng tối nhé"* là cấm tuyệt đối).
- **16 chuỗi văn bản cũ trong `0.5.5` đã được rà soát và viết lại** trong `docs/design/text-audit-0.5.5.md` để áp dụng ngay ở `0.5.6`: xóa toàn bộ chữ *"Hồi ức"*, *"Tâm sự"*, *"Cô ấy/Anh ấy"* khi chỉ robot, thay bằng ngôn ngữ nhật ký kỹ thuật ngắn gọn, chân thực và giàu sức gợi.

### 5.3. Bảy câu hỏi mở của Story v3 (Chi tiết phương án & Đề xuất tại Mục 9)

Bản thảo `Logs/sources/story-v3.md` để mở 7 quyết định cốt truyện cho chủ dự án (tên chính thức của tập đoàn thứ hai, bản chất mẫu sinh học trong ống dung nham, số phận từng chiếc rover 01–06, hình thức tín hiệu trả lời của Ro và Houston ở cảnh kết, v.v.). Toàn bộ 7 câu hỏi này được trình bày kèm phương án và **Đề xuất** cụ thể tại **Mục 9.3**.

---

## 6. Định hướng nghệ thuật và âm thanh (Art, Visual & Audio Direction)

*(Trích dẫn: `feel-priority-solitude.md`, 2026-10-07; `look-too-purple.md`, 2026-10-14; `audio-feedback-055.md`, 2026-10-14; `rover-07-v2.md`, 2026-10-14)*

### 6.1. Những gì Chủ dự án ĐÃ KHÓA (Locked Visual & Audio Pillars)

1. **Bảng màu chuẩn (`PaletteSwatch`):**
   - Bầu trời: Đen thẫm vũ trụ (`#05070B`), không mây, không sương mù tím.
   - Bề mặt Mặt Trăng: Xám bụi regolith trung tính (`#7A7E85` ngoài nắng, `#2A2F37` trong bóng tối) — tuyệt đối không ám tím hay xanh neon.
   - Căn cứ & Công trình đã bật điện: Vàng hổ phách ấm (`#F4A640` / `#FFD27D`).
   - Công nghệ quét & Bản vẽ: Lục lam sắc nét (`#48D1CC`).
   - Mối hàn cắt được (*Cut Seam*) & Cảnh báo: Cam nhiệt (`#FF6B35`).
2. **Hai chế độ ánh sáng đã chốt (`Look A` & `Look C`):**
   - **Ban đêm / Trong bóng râm — `Look A ("Moon grey")`:** Đã áp dụng trong `0.5.5`. Mặt đất xám bạc mát lạnh dưới ánh Trái Đất, độ tương phản cao với ánh đèn vàng ấm của căn cứ.
   - **Ban ngày cực Nam — `Look C ("Low sun")`:** Đưa vào ở `0.6` cùng chu kỳ ánh sáng L1. Nắng trắng ấm chiếu xiên góc thấp (`3°–6°`), bóng đổ dài sắc cạnh, bụi bạc lấp lánh trên sườn gờ đá.

### 6.2. Những gì ĐANG CHỜ Chủ dự án chọn (Pending Owner Auditions)

Hiện tại có 3 bộ thử nghiệm trực quan đã được dựng sẵn để chủ dự án duyệt trước khi sản xuất hàng loạt cho `0.5.6`:

1. **Độ mượt bề mặt & Hậu kỳ (`Look Study V0–V3` trong `look-too-purple.md`):**
   - `V0`: Hiện trạng `0.5.5` (địa hình tam giác sắc cạnh `flat-shaded`, không AO, hơi gắt).
   - `V1`: Làm mượt pháp tuyến theo góc (`smooth-by-angle normals 35°–45°`) cho đá và gờ hố; giữ mặt phẳng cơ khí trên 07 và nhà cửa.
   - `V2`: `V1` + lưới địa hình mịn hơn quanh căn cứ + bóng tiếp xúc nhẹ (*Ambient Occlusion*).
   - `V3`: `V2` + khử răng cưa (*MSAA/TAA*) và viền bóng đổ mềm nhẹ ở xa, nhưng **vẫn giữ nguyên chất liệu màu phẳng (*flat-lit palette*), không dùng texture PBR bóng nhẫy**.
2. **Phong cách hình khối 3D (`Art Style S1 / S2 / S3` trong `Logs/sources/PLAN.md`):**
   - `S1 — Soft Toy`: Các cạnh vát bo tròn dày, tỷ lệ bầu bĩnh dễ thương.
   - `S2 — Clean Low-Poly`: Các mảng phẳng vát cạnh (`chamfered flat-shaded`) sắc sảo, hiện đại.
   - `S3 — Retro Lunar Hardware`: Tỷ lệ cơ khí Apollo / công nghiệp thập niên 1970–2040, có khớp nối, bulông, lá cách nhiệt vàng và khung giàn rõ ràng.
3. **Bảng âm thanh 14 hiệu ứng (`Audio Palette A / B / C` tại `Builds/AudioPreview/index.html`):**
   - `Palette A — Workshop`: Tiếng cơ khí kim loại chính xác, tiếng rơ-le, mô-tơ bước và chuông đồng nhỏ.
   - `Palette B — Wood & Felt`: Tiếng đàn kalimba, búa dạ gõ dây đàn piano trầm, mộc mạc và êm dịu.
   - `Palette C — Old Radio`: Tiếng băng từ ấm, tiếng công tắc đài cổ và sóng âm analog.
   - **14 hiệu ứng âm thanh (cues) trong bộ mẫu thử:** *(1) Nhặt vật liệu vào khoang sau (`CargoIntake`), (2) Khoang hàng đầy (`CargoFull`), (3) Đang quét quang học (`ScanLoop`), (4) Quét hoàn tất mảnh bản vẽ (`ScanComplete`), (5) Ghép đủ bản vẽ mới (`BlueprintUnlocked`), (6) Nạp bó hàng vào phễu (`HopperToss`), (7) Công trình bật điện (`BuildingOnline`), (8) Mỏ cắt plasma hoạt động (`CutterLoop`), (9) Cắt đứt mối hàn (`CutSnap`), (10) Gầu xúc đào đất (`ScoopDig`), (11) Tời móc & Cần trục nâng kỷ vật (`WinchAndHoist`), (12) Phản hồi nghe Sonar định hướng (`SonarEcho`), (13) 07 chirp chào bạn robot (`CompanionGreet`), (14) Tháp tiếp sóng nối mạng (`RelayLinkChime`).*

### 6.3. Cách áp dụng đồng bộ ngay sau khi Chủ dự án chốt

- Ngay khi chủ dự án chọn `S1/S2/S3` và `V1/V2/V3`, `RoverBuilder.cs` sẽ dựng mô hình **07 v2** cùng toàn bộ 15 khối thiết bị gắn ngoài theo đúng một quy chuẩn vát cạnh và góc làm mượt pháp tuyến đó; toàn bộ tảng đá, xác máy và công trình trong `0.5.6–1.0` dùng chung bộ shader và quy tắc vát cạnh này.
- Ngay khi chủ dự án chọn `Audio A/B/C` (hoặc bản phối lai), `ProceduralSfx.cs` sẽ thay thế toàn bộ 14 cue âm thanh trong game và xóa sạch các hàm phát tiếng bíp điện tử cũ.

---

## 7. Lộ trình phiên bản 0.5.6 → 1.0 (Phased Release Roadmap)

*(Trích dẫn: `phased-releases.md`, 2026-10-08; `docs/design/0.5.5-owner-feedback.md`; `Logs/sources/PLAN.md`)*

### 7.1. Phiên bản `0.5.6` — Giải quyết triệt để toàn bộ phản hồi `0.5.5` & Dựng `07 v2`
**Mục tiêu cho người chơi:** Giờ chơi đầu tiên tại Lòng chảo & Hẻm Thì Thầm đạt độ hoàn thiện chuẩn mực; mọi hành động của 07 đều có bộ phận cơ khí thật; giao diện, văn bản và âm thanh sạch lỗi.

**Danh sách 14 hạng mục bắt buộc hoàn thành trong `0.5.6` (đối chiếu 1–1 với phản hồi của Chủ dự án):**
1. **Khóa phong cách 3D (`S1/S2/S3`), độ mượt (`V0–V3`) và âm thanh (`A/B/C`)** theo quyết định của chủ dự án.
2. **Dựng mô hình `07 v2` hoàn chỉnh (`RoverBuilder` + `IRoverRig` 7 điểm gốc):** Cửa khoang hàng vỏ sò sau (`CargoHatch`), Tháp cắt & hàn mũi xe (`CutterTurret` + `ChargeConduits`), Gầu xúc răng cưa trước (`DigScoop`), Trống tời cáp sau (`WinchDrum` + `TowHook`), Đệm lưng & Cần trục chữ A (`CargoCradle` + `HoistBail`), Vòng ăng-ten định hướng (`SonarLoop`), Hộc hàng hông (`SidePanniers`).
3. **Làm lại Nhịp Gara Kenji 2 giai đoạn:** Lắp miễn phí **Tời cáp sau** tại cổng bảo trì Gara Kenji trước → kéo lê kỷ vật đầu tiên trên mặt đất bằng dây cáp thật về trạm → sau khi mở Xưởng xe mới chế tạo và lắp **Giá chở hàng lưng (`CargoCradle`)** để nâng kỷ vật lên lưng.
4. **Đưa linh kiện tháp tiếp sóng vào Khoang hàng sau & Hộc chân tháp:** Xóa `StoryPartSocket` trên nóc xe; linh kiện tháp đi qua cửa sau `CargoHatch` vào khoang hàng (chiếm 1 ô); khi sửa tháp, nạp vào hộc chân tháp và chấm hàn ở bảng điện chân tháp.
5. **Thay toàn bộ tia công cụ bắn từ mắt bằng cơ khí thật:** Nạp phễu bằng cách phóng bó hàng từ cửa khoang sau (`HatchOrigin`); đào đất bằng 3 nhịp xúc của gầu trước (`ScoopOrigin`); cắt và hàn từ mỏ mũi xe (`CutterOrigin`/`StitchOrigin`); mắt (`EyeOrigin`) chỉ chiếu quạt sáng quét quang học.
6. **Xóa thẻ ký ức rơi ra khi sửa bạn robot (`MemoryCard`):** Sửa xong Tilly hoặc Bell là bạn tự thức dậy, cử động, chirp chào và đi làm việc ngay.
7. **Đặt lại bộ phận thứ 3 của Bell bị vùi dưới sườn đá lở:** Nhìn bằng mắt thấy ngay đống đá đè lên, bắt buộc dùng Gầu đào `DigScoop` xúc đá mới lấy được.
8. **Giao diện Kho đồ + Trang bị trên MỘT màn hình cố định (`Tab`):** Trái là lưới Khoang hàng `8 → 16 ô`, phải là hình 07 với các ổ lắp cố định; đỗ ở trạm thì trái là khoang 07, phải là Kho nhà `24 ô`. Xóa các tab chuyển trang và xóa vòng tròn pin 3D quanh xe.
9. **Bỏ phím `Space` và xóa cột sáng trên trời (`SiteMarker.Pillar`):** Thay bằng cơ chế dừng xe 2 giây tự nghe sonar: vòng `SonarLoop` xoay chỉ hướng, đầu 07 liếc nhìn, cạnh vật thể lấp lánh (hoặc đèn cọc chớp 2 lần) kèm âm thanh 3D.
10. **Sửa lỗi đầu 07 và tia laser khóa cứng về hướng căn cứ:** Đầu 07 nhìn theo hướng lái và chỉ liếc nhìn điểm thú vị gần đó; tia quét/cắt chỉ bật khi người chơi đang thao tác.
11. **Làm lại hiệu ứng Quét quang học (`ScanFan`):** Chùm sáng dạng quạt mỏng xuất phát từ ống kính `EyeOrigin` quét dọc mặt cắt vật thể từ dưới lên trên.
12. **Áp dụng toàn bộ 16 bản sửa văn bản từ `text-audit-0.5.5.md` + khóa gợi ý không lặp lại (`HudMutex`):** Nhãn ngắn gọn, tự nhiên, không nhân cách hóa máy móc.
13. **Cập nhật bộ 14 hiệu ứng âm thanh mới:** Xóa sạch tiếng bíp điện tử gắt.
14. **Sửa lỗi nhấp nháy mặt phẳng (`z-fighting`) trên mái vòm Xưởng xe và các bệ công trình.**

### 7.2. Phiên bản `0.6` — Khe Rãnh, Biển Dài & Mặt Trăng Sống Động
**Mục tiêu cho người chơi:** Mở rộng ra ngoài Lòng chảo lần đầu tiên trên một thế giới liền mạch; trải nghiệm chu kỳ nắng cực L1 và hệ sinh thái máy móc tự hoạt động.
- Hệ thống nạp thế giới liền mạch (`WorldStreamer` `64 × 64 m`) + lưu/tải vị trí tại bất kỳ Trụ sạc nào.
- Vùng **Khe Rãnh (`Rille Pass`)** và **Biển Dài (`Long Mare`)** với Dàn Ăng-ten Sâu và mảnh **Khảo sát 2/6** (bí ẩn năm 2067).
- Chu kỳ ánh sáng cực Nam L1 (90 phút Ngày `Look C` / 30 phút Đêm `Look A`).
- Tài nguyên cấp 2 (`Quặng Ilmenite` → `Hợp kim`), mở khóa **Cánh Pin Mặt Trời**, **Tụ Cắt Nhanh**, **Pin Cấp 2**, **Khung Gầm Cấp 2**.
- Hệ sinh thái máy móc đợt 1 (**Robot quét bụi `Solar Sweepers`**, bàn xoay pin mặt trời, 2 Drone Chị Em đầu tiên) + hiện tượng **Bụi tĩnh điện lơ lửng**.
- **Sổ ghi chép (`Databank` — phím `J`)** và **4 Bệ xây dựng ngoài thực địa** đầu tiên (Trụ sạc, Cầu vượt, Đèn tiêu).

### 7.3. Phiên bản `0.7` — Bậc Thềm Vành Hố & Trạm Meridian
**Mục tiêu cho người chơi:** Khám phá trạm tự động thứ hai với ngôn ngữ thiết kế cơ khí đối lập và leo lên sườn núi dốc đứng.
- Vùng **Bậc Thềm Vành Hố (`Rim Terraces`)** và khu phức hợp **Trạm Meridian (`Meridian Station`)** với mảnh **Khảo sát 3/6**.
- Tài nguyên cấp 3 (`Đá Anorthite` → `Nhôm nhẹ`), mở khóa **Bánh Từ Tính (`Mag-Wheels`)**, **Mỏ Cắt Nhiệt (`Thermal Lance`)**, và **Bộ Chuyển Đổi Chuẩn Sạc (`Cross-Standard Adapter`)**.
- Mở khoang kín của vệ tinh **Kestrel-3** ở Lòng chảo bằng Mỏ cắt nhiệt để lấy khóa giao thức Meridian.
- Giải cứu bạn robot hạng nặng **Atlas** (chạy tuyến chở quặng tự động về Kho nhà Lumen) và đánh thức họ máy hình lục giác của Meridian.

### 7.4. Phiên bản `0.8` — Hố Bóng Tối, Ống Dung Nham & Phát Hiện Của Ines
**Mục tiêu cho người chơi:** Đi xuống lòng đất tối thẫm và lạnh giá nhất của Mặt Trăng để tìm ra bằng chứng nước và sự sống.
- Vùng **Hố Bóng Tối Vĩnh Cửu (`Shadowed Crater`)** và **Ống Dung Nham Ngầm (`Lava Tube`)** với mảnh **Khảo sát 4/6 & 5/6**.
- Cơ chế hao pin vì lạnh (`-200°C`) và bóng tối tuyệt đối; mở khóa **Đèn Pha Mạnh**, **Bộ Sưởi Pin**, **Tời Neo Mũi Xe (`Anchor Winch`)**, **Pin Cấp 3**.
- Giải cứu robot sinh học **Moss**, khôi phục **Vườn ươm rêu ngầm (`Subsurface Bryophyte Dome`)**, hiện tượng **Tinh thể Áp điện ngân vang** và sự kiện **Nhật thực Mặt Trăng**.

### 7.5. Phiên bản `1.0` — Đường Lên Đỉnh, Phát Sóng Về Trái Đất & Hoàn Thiện
**Mục tiêu cho người chơi:** Chinh phục đỉnh núi cao nhất, gửi bằng chứng về Trái Đất, nhận câu trả lời từ con người và tận hưởng thế giới đã được làm ấm hoàn toàn.
- Vùng **Đường Lên Đỉnh (`Peak Road`)** và **Đài Quan Sát Chảo Lớn (`The Peak`)** với mảnh **Khảo sát 6/6**.
- Trường đoạn sửa chữa Chảo Đỉnh núi, kích hoạt chuỗi tiếp sóng toàn vùng và phát sóng báo cáo khảo sát về Trái Đất.
- Cảnh kết (**Ending Cutscene**): Tín hiệu đến đài nghiệp dư của **Ro**, chuyển tiếp tới **Houston**, xác nhận mở lại sứ mệnh tàu có người lái quay về Trạm Lumen, chuyển sang chế độ chơi tự do (*Post-game Free Roam*).
- Tính năng hoàn thiện `1.0`: **Phòng Sơn (`Paint Bay`)** đổi màu thân xe 07, **Chế độ Chụp ảnh (`Photo Mode`)**, trình đơn Cài đặt đầy đủ và thành tựu (*Achievements*).

---

## 8. Cách làm để không sửa đi sửa lại (Workflow to Prevent Rework)

*(Trích dẫn: `phased-releases.md`, 2026-10-08; `polish-every-state.md`, 2026-10-12; `rover-07-v2.md`, 2026-10-14; `CLAUDE.md`)*

### 8.1. Quy trình 4 bước: Thiết kế trước → Duyệt → Dựng → Tự chơi kiểm chứng

1. **Bước 1 — Kiểm tra 10 điểm logic cơ khí trước khi viết bất kỳ dòng code nào:**
   Trước khi thêm một vật phẩm, công trình hay nâng cấp mới, bắt buộc trả lời 5 câu hỏi: *(1) Bộ phận 3D nào trên 07 thực hiện thao tác này? (2) Điểm gốc `IRoverRig` nào được dùng? (3) Có vật gì bị trôi lơ lửng trên mặt đất trống không? (4) Trạng thái trước và sau khi sửa khác nhau thế nào ở khoảng cách 30 m? (5) Có cần thêm bất kỳ chữ giải thích nào không hay nhìn hình là hiểu?*
2. **Bước 2 — Duyệt bằng bản xem trước nhẹ (*Preview / Blockout Checkpoint*):**
   Với các quyết định thẩm mỹ (phong cách 3D, ánh sáng, âm thanh, bố cục màn hình `Tab`), tạo trang xem trước tĩnh hoặc cảnh so sánh A/B/C để chủ dự án chọn trong 1 phút trước khi áp dụng vào toàn bộ dự án.
3. **Bước 3 — Một nguồn sự thật duy nhất (*Single Source of Truth*):**
   File `docs/design/master-plan.md` này là tài liệu gốc duy nhất. Khi chủ dự án chốt bất kỳ mục nào trong **Mục 9**, cập nhật quyết định đó vào ngay file này và đồng bộ xuống các file tài liệu con theo bảng ở **Mục 10** trước khi lập trình.
4. **Bước 4 — Tự chơi thực tế (*Self-Playthrough*) trước khi bàn giao:**
   Không bao giờ báo cáo "đã xong" chỉ vì code biên dịch không lỗi. Sau mỗi tính năng, phải chạy thử toàn bộ luồng từ góc nhìn người chơi mới (không dùng phím tắt gian lận) để kiểm tra cảm giác lái, góc máy, âm thanh và dòng nhắc.

### 8.2. Ba điểm chốt duyệt của Chủ dự án (Owner Checkpoints)

- **Chốt 1 (Ngay sau khi duyệt văn bản này):** Chủ dự án chốt các quyết định trong **Mục 9** (đặc biệt là Art Style `S1/S2/S3`, Look `V1–V3`, Audio `A/B/C` và các câu hỏi `07 v2` / `story v3`).
- **Chốt 2 (Giữa kỳ `0.5.6`):** Chủ dự án xem hình chụp mô hình `07 v2` với đầy đủ 7 cụm cơ khí và màn hình `Tab` một trang duy nhất trước khi nối toàn bộ hoạt cảnh.
- **Chốt 3 (Cuối mỗi phiên bản `0.5.6`, `0.6`, `0.7`, `0.8`, `1.0`):** Bản build chạy độc lập kèm báo cáo kiểm chứng và ảnh chụp các góc nhìn chính.

### 8.3. Thang kiểm chứng kỹ thuật bắt buộc (Verification Ladder)

Mọi thay đổi mã nguồn trước khi commit phải vượt qua đủ 4 tầng kiểm tra:
1. **Biên dịch sạch (`tools/compile_check.py`):** 0 lỗi (error), 0 cảnh báo mới (warning) trên cả `Lunarion.Runtime`, `Lunarion.Editor` và `Lunarion.Tests`.
2. **Kiểm thử tự động EditMode (`tests-run EditMode`):** Toàn bộ bài kiểm tra logic kho hàng, năng lượng, tiến trình bản vẽ, hệ thống lưu/tải và quy tắc văn bản phải xanh 100%.
3. **Kiểm thử tự động PlayMode & Quy tắc Thế giới (`NoLooseLootSession`):**
   - Kiểm tra tự động mọi điểm đặt vật phẩm: không có mảnh bản vẽ, đá quặng, kỷ vật hay bộ phận bạn bè nào nằm ngoài giá đỡ/xác máy hỏng (`no-floating-loot.md`).
   - Kiểm tra mọi điểm gốc `IRoverRig` trên `07 v2` tồn tại, đúng tọa độ đối xứng và không có tia công cụ nào xuất phát từ `EyeOrigin` trừ tia quét.
4. **Kiểm tra thị giác & Trải nghiệm thực tế:** Chụp ảnh kiểm tra ở khoảng cách `5 m` và `30 m`, kiểm tra `HudMutex` (không bao giờ có 2 hướng dẫn cùng lúc) và kiểm tra tốc độ khung hình ổn định.

---

## 9. Quyết định còn mở cho Chủ dự án (Open Decisions for Owner Review)

Dưới đây là toàn bộ **16 quyết định còn mở** được tổng hợp từ các bản thử nghiệm nghệ thuật, cơ khí `07 v2` và cốt truyện `story v3`. Mỗi mục đều có các phương án rõ ràng và một **Đề xuất** để chủ dự án có thể duyệt nhanh.

### 9.1. Nhóm Nghệ thuật, Hiển thị & Âm thanh (3 quyết định)

1. **Chọn Phong cách hình khối 3D (`Art Style S1 / S2 / S3`):**
   - *Phương án A (`S1 — Soft Toy`):* Cạnh bo tròn dày, tỷ lệ bầu bĩnh như đồ chơi để bàn.
   - *Phương án B (`S2 — Clean Low-Poly`):* Mảng phẳng vát cạnh sắc nét, hiện đại, tối giản.
   - *Phương án C (`S3 — Retro Lunar Hardware`):* Cơ khí công nghiệp lấy cảm hứng từ tàu Apollo & thiết bị khảo sát thực tế (khung giàn, lá cách nhiệt vàng, khớp nối cơ khí rõ ràng).
   - **Đề xuất:** Chọn **lai `S3 + S2`** (tỷ lệ cơ khí chân thực, đọc rõ công dụng từng bộ phận của `S3`, nhưng dựng bằng các mảng phẳng vát cạnh sạch sẽ của `S2`, không làm chi tiết rối mắt).
2. **Chọn Độ mượt bề mặt & Hậu kỳ (`Look Study V0 / V1 / V2 / V3`):**
   - *Phương án A (`V0`):* Giữ nguyên địa hình đa giác gắt như `0.5.5`.
   - *Phương án B (`V1`):* Chỉ làm mượt pháp tuyến theo góc (`35°–45°`) trên đá và gờ hố.
   - *Phương án C (`V2`):* `V1` + địa hình mịn hơn quanh căn cứ + bóng tiếp xúc AO nhẹ.
   - *Phương án D (`V3`):* `V2` + khử răng cưa và làm mềm viền bóng đổ, giữ nguyên màu phẳng không PBR.
   - **Đề xuất:** Chọn **`V3`** — loại bỏ hoàn toàn cảm giác "lưới tam giác thô" của địa hình mà vẫn giữ đúng chất màu phẳng cô đơn, sắc nét của Mặt Trăng.
3. **Chọn Bảng âm thanh chủ đạo (`Audio Palette A / B / C` tại `Builds/AudioPreview/index.html`):**
   - *Phương án A (`Palette A — Workshop`):* Cơ khí kim loại chính xác, tiếng công tắc, mô-tơ và chuông đồng.
   - *Phương án B (`Palette B — Wood & Felt`):* Tiếng đàn kalimba, gỗ và búa dạ ấm áp.
   - *Phương án C (`Palette C — Old Radio`):* Tiếng đài analog và băng từ cổ điển.
   - **Đề xuất:** Chọn **Phối hợp `A` cho thao tác cơ khí của 07 + `B` cho âm báo hoàn thành/kỷ vật** (mọi chuyển động mở cửa khoang, cắt, đào, kéo tời kêu tiếng cơ khí thật của `A`; còn giọng chirp của 07 và tiếng chuông hoàn thành công trình dùng âm mộc ngũ cung ấm của `B`).

### 9.2. Nhóm Cơ khí 07 v2 & Giao diện (5 quyết định)

4. **Cách nạp vật liệu vào phễu trạm (`HopperFeed`) từ Khoang hàng sau:**
   - *Phương án A:* 07 chỉ cần đỗ cạnh phễu (bất kể đầu hay đuôi), cửa khoang sau `CargoHatch` mở ra và phóng từng bó vật liệu bay vòng cung qua đầu xe rơi vào miệng phễu.
   - *Phương án B:* Bắt buộc người chơi phải xoay đuôi xe về phía phễu mới bấm nạp được.
   - **Đề xuất:** Chọn **Phương án A** (khi bấm `E` nạp phễu, 07 tự xoay nhẹ thân xe 15–20° hoặc phóng bó hàng theo đường cong tự nhiên từ `HatchOrigin` vào miệng phễu — vừa đúng logic vật lý từ khoang sau, vừa không gây khó dễ cho người chơi khi đỗ xe).
5. **Cách đưa linh kiện vào sửa Tháp tiếp sóng (`Tower Repair Delivery`):**
   - *Phương án A:* Mỗi chân tháp có một **Hộc tiếp nhận ở chân tháp (`Base Service Hatch`)**; 07 mở khoang sau đẩy linh kiện vào hộc, dùng mỏ hàn mũi xe chấm 2 mối điện, rồi cơ cấu tời nội bộ của tháp kéo linh kiện lên đỉnh.
   - *Phương án B:* Dùng cần trục lưng `HoistBail` của 07 cẩu trực tiếp linh kiện gắn vào sườn tháp.
   - **Đề xuất:** Chọn **Phương án A** (vì ở Tháp tiếp sóng 1 đầu game, người chơi chưa mở khóa Giá chở hàng có cần trục `HoistBail`; dùng Hộc tiếp nhận ở chân tháp đảm bảo tính nhất quán cho cả 3 tháp và giải quyết dứt điểm lỗi "đặt dưới đất tự nhảy lên đỉnh").
6. **Bố cục hiển thị khi mở Kho nhà (`Station Storage UI`):**
   - *Phương án A:* Dùng chung đúng khung màn hình `Tab` (cột trái là Lưới Khoang hàng 07 `8/16 ô`, cột phải đổi từ sơ đồ thiết bị 07 sang Lưới Kho nhà `24 ô`, có 1 nút nhỏ trên đỉnh cột phải để xem lại Thiết bị 07 nếu cần).
   - *Phương án B:* Hiển thị cả 3 cột cùng lúc (Khoang hàng 07 + Sơ đồ thiết bị 07 + Kho nhà) trên màn hình rộng.
   - **Đề xuất:** Chọn **Phương án B khi đỗ tại Đế sạc / Xưởng xe** (hoặc **Phương án A** nếu muốn giữ khung nhỏ gọn ở chính giữa). Khuyến nghị **Phương án A** để giữ đúng tỷ lệ khung 2 cột cân đối của Subnautica và chữ luôn to, dễ đọc.
7. **Sức chứa của Linh kiện tháp và Bộ phận bạn bè trong Khoang hàng 07:**
   - *Phương án A:* Mỗi linh kiện tháp (`TowerCoil`, `Transformer`, `RelaySpare`) hoặc bộ phận của Tilly/Bell chiếm **1 ô chuẩn** trong lưới 8/16 ô của Khoang hàng sau (cùng chung lưới với Kim loại và Thạch anh).
   - *Phương án B:* Mỗi linh kiện lớn chiếm `2 × 2` (4 ô) trong lưới Khoang hàng.
   - **Đề xuất:** Chọn **Phương án A (1 ô/linh kiện)** để giữ thao tác quản lý kho hàng đầu game đơn giản, trực quan, không bắt người chơi phải vứt bớt Kim loại khi đi nhặt linh kiện tháp.
8. **Chế độ Kéo lê bằng Tời cáp sau (`Winch Ground-Tow`) sau khi đã lắp Giá chở hàng lưng (`CargoCradle`):**
   - *Phương án A:* Sau khi đã lắp Giá chở hàng lưng, bấm `F` vào kỷ vật sẽ luôn tự động dùng cần trục nâng thẳng lên lưng xe (`CradleCarry`); Tời cáp sau chuyển sang dùng cho các việc nặng không thể nhấc lên lưng (kéo dầm sắt chắn đường, kéo xe Atlas bị kẹt, neo cáp).
   - *Phương án B:* Bấm nhanh `F` để nâng lên lưng, giữ `F` nếu vẫn muốn kéo lê dưới đất.
   - **Đề xuất:** Chọn **Phương án A** — một nút một hành động tối ưu nhất, thể hiện rõ sự tiến bộ vượt bậc trước và sau khi có Giá chở hàng.

### 9.3. Nhóm Cốt truyện Story v3 & Phạm vi Thế giới (8 quyết định)

9. **Tên chính thức và Quốc적/Tổ chức của Trạm thứ hai (`Second Base Identity`):**
   - *Phương án A:* **Trạm Meridian (`Meridian Station`)** thuộc **Liên doanh Tycho (`Tycho Consortium`)** — một liên doanh khai khoáng tư nhân dùng thiết bị tự động hóa công nghiệp (họ máy lục giác màu xanh xám).
   - *Phương án B:* Một trạm nghiên cứu của cơ quan vũ trụ quốc gia khác (ví dụ ESA hoặc JAXA).
   - **Đề xuất:** Chọn **Phương án A (`Meridian Station — Tycho Consortium`)** vì đối lập rõ nét giữa một bên là trạm khoa học công lập (Lumen) và một bên là trạm khảo sát thương mại tự động (Meridian), giải thích hợp lý vì sao hai bên không dùng chung chuẩn sạc và không chia sẻ dữ liệu trước cơn bão 2047.
10. **Bản chất của phát hiện trong Ống Dung Nham (`The Biological Discovery`):**
    - *Phương án A:* **Mạch băng nước khổng lồ kèm Rêu Mặt Trăng ngủ đông (`Dormant Lunar Bryophyte / Cryo-moss`)** — bào tử thực vật bậc thấp có nguồn gốc cổ đại hoặc từ thí nghiệm sinh học kín bị rò rỉ và thích nghi được nhờ mạch nước ấm địa nhiệt trong ống dung nham.
    - *Phương án B:* Chỉ thuần túy là băng nước và khoáng chất quý, hoàn toàn không có yếu tố sinh học.
    - *Phương án C:* Hóa thạch vi sinh vật ngoài hành tinh cổ đại trong lõi băng.
    - **Đề xuất:** Chọn **Phương án A (Nước băng + Rêu ngủ đông trong Vườn ươm ngầm của Ines & Moss)** vì tạo ra khoảnh khắc thị giác cực kỳ xúc động ở `0.8`: giữa toàn bộ Mặt Trăng xám đen, 07 và robot Moss thắp sáng một mái vòm nhỏ có sắc xanh lá cây sống động duy nhất, khớp hoàn hảo với mục thứ 4 của Sổ ghi chép (*Sự sống — Life*).
11. **Số phận của các rover tiền bối `01` đến `06` (`Predecessor Rovers 01–06`):**
    - *Phương án A:* Cả 6 chiếc đều nằm nghỉ yên tại vị trí công việc cuối cùng của mình ở 6 vùng (ví dụ: `01` giữ chốt cửa xả áp ở Hẻm núi, `02` kẹt cạnh robot quét bụi ở Biển Dài, `03` nằm ở chân dốc Bậc thềm, `04` giữ cáp khoan ở Hố Bóng Tối, `05` cấp nguồn cuối cho Vườn rêu, `06` nằm ngay trước cổng Chảo Đỉnh núi). Khi 07 quét từng chiếc sẽ lấy được nhật ký hành trình và 1 mảnh công nghệ quý.
    - *Phương án B:* Có thể sửa lại một vài chiếc trong số `01–06` để chúng chạy quanh trạm giống Tilly.
    - **Đề xuất:** Chọn **Phương án A** (để vai trò bạn đồng hành sống động thuộc về Tilly, Bell, Atlas, Moss và đàn Drone; còn `01–06` là những cột mốc lịch sử lặng lẽ truyền lại di sản cho người em út `07`).
12. **Hình thức tín hiệu trả lời từ Trái Đất ở cảnh kết `1.0` (`Earth's Response Format`):**
    - *Phương án A:* Kết hợp **cả chữ trên màn hình thiết bị đầu cuối, biểu đồ sóng âm thanh radio (tiếng Morse / tiếng sóng mang ấm), và giọng đọc ngắn qua bộ lọc radio xa xăm** của Ro rồi đến điều phối viên Houston (không hiện mặt người).
    - *Phương án B:* Chỉ hiện văn bản chữ trên màn hình máy tính của Trạm Lumen kèm tiếng tín hiệu radio, hoàn toàn không có giọng nói.
    - **Đề xuất:** Chọn **Phương án A** (với 2 câu thoại radio cực ngắn, điềm tĩnh, chuyên nghiệp — vì sau 4 giờ chơi hoàn toàn tĩnh lặng chỉ có tiếng máy và băng ghi âm cũ, khoảnh khắc nghe thấy một giọng nói trực tiếp *live* vang lên từ Trái Đất sẽ tạo cảm xúc vỡ òa mạnh nhất).
13. **Cách giải thích vì sao 07 thức dậy sau 20 năm (`Why 07 Wakes Up Now`):**
    - *Phương án A:* Vệ tinh **Kestrel-3** của Meridian rơi xuống cách trạm 160 m làm rung chấn nhẹ lớp bụi phủ trên tấm pin phụ của nhà kho, vừa đủ tích điện cho bộ hẹn giờ khởi động dự phòng kích hoạt chiếc rover cuối cùng trong hàng (`07`).
    - *Phương án B:* Bộ hẹn giờ chu kỳ 20 năm của Kenji tự bật theo lịch định sẵn.
    - **Đề xuất:** Chọn **Phương án A** vì gắn kết chặt chẽ sự kiện mở đầu (xác vệ tinh Kestrel-3 ngay trước mặt người chơi ở phút thứ 2) với lý do 07 tỉnh dậy đúng vào ngày hôm đó.
14. **Số lượng và vị trí 6 mảnh Khảo sát (`6 Survey Pieces`):**
    - *Phương án A:* Mỗi vùng trong 6 vùng giữ đúng **1 mảnh Khảo sát chính** (gắn liền với cột mốc lớn nhất của vùng đó: Tháp Hẻm núi → Dàn Ăng-ten Biển Dài → Trung tâm Meridian → Giàn khoan Hố Bóng Tối → Vườn ươm Ống Dung Nham → Đài quan sát Đỉnh núi).
    - *Phương án B:* Có 10 mảnh rải rác và người chơi chỉ cần tìm bất kỳ 6 mảnh nào.
    - **Đề xuất:** Chọn **Phương án A (6 mảnh cố định, mỗi vùng 1 mảnh)** để mỗi mảnh Khảo sát là một phần không thể thiếu của câu chuyện tổng thể và dẫn người chơi đi qua trọn vẹn 6 vùng.
15. **Mức độ tương tác với họ máy của Trạm Meridian (`Meridian Ecosystem Interaction`):**
    - *Phương án A:* Sau khi chế tạo **Bộ Chuyển Đổi Chuẩn Sạc (`Cross-Standard Adapter`)** ở `0.7`, 07 có thể sạc pin tại các đế sạc lục giác của Meridian và bật lại toàn bộ hệ thống máy tự động của Meridian.
    - *Phương án B:* Máy móc của Meridian chỉ dùng để quét lấy mảnh bản vẽ, không thể sạc hay kích hoạt lại.
    - **Đề xuất:** Chọn **Phương án A** — biến việc "hợp nhất hai mạng lưới Lumen và Meridian" thành thành tựu cơ khí lớn nhất của giai đoạn giữa game (`0.7`).
16. **Thứ tự ưu tiên sau khi hoàn tất `0.5.6`:**
    - *Phương án A:* Đi tuần tự đúng lộ trình `0.5.6 → 0.6 (Biển Dài & Chu kỳ nắng L1) → 0.7 (Meridian) → 0.8 (Hố tối & Ống dung nham) → 1.0 (Đỉnh núi)`.
    - *Phương án B:* Đưa tính năng Chu kỳ nắng cực L1 (`Look C`) vào sớm ngay trong `0.5.6` thay vì đợi `0.6`.
    - **Đề xuất:** Chọn **Phương án A** — giữ phạm vi `0.5.6` tập trung 100% vào việc sửa sạch phản hồi `0.5.5`, hoàn thiện mô hình `07 v2` và khóa phong cách nghệ thuật/âm thanh để giao bản `0.5.6` nhanh và chắc chắn nhất.

---

## 10. Mâu thuẫn đã phát hiện giữa các tài liệu cũ và cách giải quyết (Contradictions Resolved & Doc Sync Plan)

Qua quá trình đối chiếu toàn bộ kho tài liệu hiện tại, có **12 mâu thuẫn lớn** giữa các tài liệu cũ và các quyết định mới của chủ dự án. Bảng dưới đây khóa lại quy tắc chuẩn duy nhất và liệt kê chính xác những file cần cập nhật sau khi chủ dự án duyệt bản kế hoạch này (hiện tại **chưa chỉnh sửa** các file cũ theo đúng quy định của nhiệm vụ).

| # | Chủ đề mâu thuẫn | Tài liệu cũ đang viết sai / lỗi thời | Quy tắc chuẩn mới trong `master-plan.md` (Theo lệnh Chủ dự án) | File cần cập nhật sau khi duyệt |
|---|---|---|---|---|
| **1** | **Điều kiện mở khóa vùng mới: Ký ức vs Tiến trình cơ khí** | `GDD.md`, `docs/VISION.md`, `docs/DESIGN.md`, `docs/ROADMAP.md` ghi rằng *Ký ức (Memories)* là chìa khóa mở cổng vùng mới và mở công thức chế tạo. | **Tiến trình vật chất là xương sống (`progression-first.md`, 2026-10-11):** Mở vùng mới hoàn toàn bằng thiết bị cơ khí lắp trên 07 (Mỏ cắt, Gầu đào, Cánh pin mặt trời, Bánh từ tính, Đèn pha/Sưởi pin, Tời neo). Ký ức chỉ là phần thưởng cảm xúc. | `GDD.md`, `docs/VISION.md`, `docs/DESIGN.md`, `docs/ROADMAP.md` |
| **2** | **Cơ chế chế tạo: Mở sẵn theo tầng vs Quét mảnh bản vẽ** | `GDD.md` và `docs/DESIGN.md` mô tả bàn chế tạo mở sẵn công thức khi nâng cấp nhà trạm. | **Quét mảnh bản vẽ (`scan-fragments.md`, 2026-10-12):** Mọi thiết bị nâng cấp đều phải quét đủ `2–4` mảnh vỡ ngoài thực địa bằng mắt quang học của 07 rồi mới chế tạo tại Xưởng xe. Mảnh trùng cho `+3 Kim loại`. | `GDD.md`, `docs/DESIGN.md` |
| **3** | **Giao diện Kho đồ & Trang bị: Nhiều tab vs Một màn hình cố định** | `docs/design/gdd-v3.md` (§5.3, §12.4) vẫn mô tả giao diện `Tab` có 3 tab riêng biệt (*Rover / Inventory / Blueprints*) và vẽ vòng tròn pin 3D dưới bánh xe. | **Một màn hình cố định duy nhất (`inventory-one-screen.md`, 2026-10-14):** Nhấn `Tab` mở đúng 1 màn hình: trái là lưới Khoang hàng 07 (`8 → 16 ô`), phải là hình 07 với các ổ thiết bị cố định. Xóa sạch các tab chuyển trang và xóa vòng tròn pin 3D quanh xe. | `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md`, `docs/DESIGN.md` |
| **4** | **Vị trí phát tia công cụ & nạp nguyên liệu trên 07** | `docs/design/first-hour-v2.md` và code `0.5.5` cho mắt camera của 07 bắn cả tia cắt, tia hàn, tia đào đất và bắn hạt nguyên liệu vào phễu. | **Mỗi động từ một bộ phận cơ khí thật (`rover-07-v2.md`, 2026-10-14):** Mắt `EyeOrigin` chỉ nhìn và quét; cắt/hàn từ `CutterTurret` ở mũi xe; đào bằng gầu `DigScoop` ở cản trước; thu hàng và phóng nguyên liệu vào phễu từ cửa khoang hàng sau `CargoHatch`. | `docs/design/first-hour-v2.md`, `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md` |
| **5** | **Cách kéo và chở kỷ vật (Gara Kenji & Giá chở hàng)** | `docs/design/first-hour-v2.md` cho 07 kéo kỷ vật lơ lửng phía sau xe ngay từ phút thứ 5 mà không có tời cáp hay giá đỡ. | **Nhịp Gara Kenji 2 giai đoạn (`rover-07-v2.md`, 2026-10-14):** Lắp **Tời cáp sau (`WinchDrum`)** tại cổng Gara Kenji trước để móc dây cáp kéo lê kỷ vật trên mặt đất; sau khi mở Xưởng xe mới chế tạo và lắp **Giá chở hàng (`CargoCradle` + `HoistBail`)** để cẩu kỷ vật lên lưng. | `docs/design/first-hour-v2.md`, `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md` |
| **6** | **Cách chở và lắp linh kiện Tháp tiếp sóng** | Code `0.5.5` và `docs/design/first-hour-v2.md` gắn linh kiện tháp lên một điểm lơ lửng trên nóc xe (`StoryPartSocket`), và khi đặt ở chân tháp thì linh kiện tự biến mất rồi hiện trên đỉnh tháp. | **Cất trong Khoang hàng sau & Nạp vào Hộc chân tháp (`0.5.5-owner-feedback.md` #6, #11):** Xóa `StoryPartSocket`. Linh kiện tháp đi qua cửa sau `CargoHatch` vào khoang hàng (chiếm 1 ô); khi sửa tháp thì nạp vào hộc chân tháp và hàn cáp tại bảng điện chân tháp. | `docs/design/first-hour-v2.md`, `docs/design/gdd-v3.md` |
| **7** | **Thẻ ký ức (`MemoryCard`) rơi ra khi sửa bạn robot** | `docs/design/first-hour-v2.md` và `docs/design/gdd-v3.md` thiết kế khi sửa xong Tilly/Bell sẽ sinh ra một vật phẩm `MemoryCard` dưới đất để người chơi nhặt về tàu. | **Xóa hoàn toàn thẻ ký ức khi sửa bạn (`0.5.5-owner-feedback.md` #9):** Sửa xong bạn robot là bạn tự thức dậy bằng hoạt cảnh cơ khí + tiếng chirp và bắt đầu làm việc ngay. Ký ức chỉ đến từ các kỷ vật thật của con người. | `docs/design/first-hour-v2.md`, `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md`, `docs/STORY.md` |
| **8** | **Hiệu ứng Nghe Sonar: Cột sáng trời / Vòng sáng đất vs Phản hồi vật lý** | `docs/design/gdd-v3.md` dùng phím `Space` bắn cột sáng lên trời (`SiteMarker.Pillar`); còn bản nháp `Logs/sources/07V2.md` (§5.1) lại đề xuất vẽ "vòng bụi sáng màu hổ phách trên mặt đất". | **Không cột sáng trời, không vòng sáng trên mặt đất trống (`no-floating-loot.md`, 2026-10-14; `resume-point.md`):** Xóa phím `Space`. Khi dừng xe 2 giây, vòng ăng-ten `SonarLoop` xoay chỉ hướng, đầu 07 quay nhìn, chính góc kim loại của vật thể lấp lánh (hoặc đèn cọc chớp 2 lần) kèm âm thanh định hướng 3D. | `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md`, `Logs/sources/07V2.md` |
| **9** | **Giọng điệu cốt truyện & Lý do Trạm Lumen bị bỏ hoang** | `docs/STORY.md` và `GDD.md` cũ viết rằng dự án cạn tiền dần rồi bỏ rơi các robot, kèm nhiều câu thoại con người tâm sự uỷ mị với robot như thú cưng. | **Cốt truyện `story-v3` & Văn bản thực tế (`story-logic.md`, 2026-10-13):** Sơ tán khẩn cấp vì bão mặt trời 2047; phát hiện băng nước của Ines & Trạm thứ hai Meridian bị kẹt lại; năm 2067 mất liên lạc do robot quét bụi hỏng. Con người nói chuyện chuyên nghiệp với nhau, không nhân cách hóa máy móc. | `docs/STORY.md`, `GDD.md`, `docs/VISION.md`, `docs/design/writing-style.md` |
| **10** | **Cấu trúc các vùng thế giới & Thứ tự phiên bản** | `GDD.md` và `docs/ROADMAP.md` cũ chia 5 vùng rời rạc theo kiểu màn chơi và đặt lộ trình `0.1 → 0.5` cũ. | **Một thế giới liền mạch 6 vùng (`0.6-world-regions.md` & `PLAN.md`):** Lòng chảo & Hẻm núi (`0.5.6`) → Khe Rãnh & Biển Dài (`0.6`) → Bậc thềm & Trạm Meridian (`0.7`) → Hố Bóng Tối & Ống Dung Nham (`0.8`) → Đường lên Đỉnh (`1.0`). | `GDD.md`, `docs/ROADMAP.md`, `docs/design/game-flow.md` |
| **11** | **Chu kỳ ánh sáng và Sạc năng lượng ngoài trời** | `docs/DESIGN.md` cũ cho phép sạc pin dưới ánh sáng Trái Đất (*Earthshine*) hoặc có các vùng năng lượng trôi nổi giữa đồng. | **Chu kỳ nắng cực L1 thực tế (`0.6-living-moon.md`):** Ánh sáng Trái Đất không bao giờ sạc được pin. Chỉ sạc đầy 100% tại Đế sạc / Trụ sạc, và sạc dự phòng tối đa 1 ô (25%) dưới ánh nắng trực tiếp khi đã lắp Cánh Pin Mặt Trời. | `docs/DESIGN.md`, `docs/design/gdd-v3.md` |
| **12** | **Sổ ghi chép (`Databank`)** | `docs/design/gdd-v3.md` (§5.8) từng ghi *"Không có Codex hay danh sách văn bản"*. | **Có Sổ ghi chép Khảo sát 4 mục gọn nhẹ (`scan-fragments.md`, 2026-10-12; `0.6-living-moon.md`):** Gồm *Máy móc, Mặt Trăng, Địa điểm, Sự sống* với hình vẽ nét quét của 07 và ghi chú kỹ thuật 1–2 câu của phi hành đoàn. | `docs/design/gdd-v3.md`, `docs/design/gdd-v3-vi.md` |

---

### Bảng kiểm tra nhanh trước khi kết thúc (Master Plan Checklist)
- [x] Ngôn ngữ: Tiếng Việt rõ ràng, tự nhiên, thuật ngữ gốc tiếng Anh đặt trong ngoặc đơn.
- [x] Đầy đủ 11 mục (`0` đến `10`) theo đúng cấu trúc `Logs/TASK.md`.
- [x] Trích dẫn đầy đủ toàn bộ các quy tắc trong `Logs/owner-memory/` kèm tên file và ngày tháng.
- [x] Tích hợp toàn diện `Logs/sources/07V2.md`, `Logs/sources/story-v3.md`, `Logs/sources/PLAN.md` và `docs/design/0.5.5-owner-feedback.md`.
- [x] Không tự ý bịa quyết định của chủ dự án; mọi đề xuất đều đánh dấu rõ là **Đề xuất**.
- [x] Không chỉnh sửa bất kỳ tài liệu được theo dõi (tracked docs) nào khác ngoài `docs/design/master-plan.md`.
