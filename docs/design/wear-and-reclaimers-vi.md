# Hao Mòn (Wear), Sửa Chữa Cơ Lý & Máy Thu Hồi (Reclaimers) — Tài Liệu Thiết Kế

**Trạng thái:** `[DECIDED]` Khung cốt lõi đã được chốt bởi Phán quyết của Chủ dự án (2026-10-17) & `docs/VISION.md` Phán quyết 17 (`main` `e9a1b7af`); các công thức chi tiết, công thức chế tạo và dải số cân bằng dưới đây là `[PROPOSAL]` để Chủ dự án duyệt.  
**Bản tiếng Anh:** [`docs/design/wear-and-reclaimers.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/wear-and-reclaimers.md)  
**Trang duyệt Concept Art:** [`Logs/wear/index.html`](file:///D:/Project/MoonProject-wt/agy-wear/Logs/wear/index.html) (`Logs/concepts/*.png`)

---

## Tóm Tắt Điều Hành & Khung Thiết Kế

Vào ngày 2026-10-17, Chủ dự án đã chốt hai thay đổi nền tảng hiện đã được ghi vào [`docs/VISION.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/VISION.md#L180-L198) (**Phán quyết 17 — Hao mòn, Sửa chữa, Reclaimers và Cái chết**):

1. **07 có thanh Độ Bền Thân Vỏ ("Wear / Hull Integrity") bên cạnh thanh Pin (Battery).** Phóng quá nhanh đâm vào vật cản cứng, đi vào hiện tượng khắc nghiệt vùng sâu mà thiếu lớp bảo vệ, hoặc bị kẹp bởi các cỗ máy **Reclaimer (Máy Thu Hồi)** sẽ làm giảm độ bền. Khi độ bền về `0%`, **07 chết (tắt nguồn hoàn toàn)**.
2. **Mọi bước hư hại, sửa chữa và mối đe dọa đều tuân thủ tuyệt đối logic vật lý và chân không:**
   - **Pin vẫn là tầm hoạt động, không bao giờ gây chết** ([`docs/VISION.md` Phán quyết 15](file:///D:/Project/MoonProject-wt/agy-wear/docs/VISION.md#L163-L176)): cạn pin (`0%`) vẫn chỉ đưa 07 về chế độ bò chậm `25%` tốc độ bằng điện mặt trời rỉ (`M3-20`). Cái chết *chỉ* đến từ việc độ bền (Wear) chạm `0%`.
   - **Những cú nảy vẫn trọn vẹn niềm vui:** Nhảy thấp trọng lực, tiếp đất có đệm từ trên bụi Mặt Trăng (regolith) và các va quệt nhẹ **không bao giờ** tốn độ bền.
   - **07 không có bàn tay** ([`docs/VISION.md` Phán quyết 14](file:///D:/Project/MoonProject-wt/agy-wear/docs/VISION.md#L158-L162)): Tự vá ngoài thực địa (**Bộ Vá Vỏ — Patch Kit**) và đại tu tại căn cứ (**Khung Bảo Dưỡng — Service Gantry**) đều là thao tác cơ-điện vật lý 100% sử dụng nam châm điện gầm, gầu xúc trước và đầu hàn hồ quang trên ngực 07 hoặc cánh tay búa/hàn của giàn khung.
   - **Căng thẳng tĩnh lặng, không phải chiến đấu:** 07 không có vũ khí. **Reclaimers** — những đoàn máy thu hồi tự động thời kỳ Khởi Hành (`1984–1988`) vẫn miệt mài gom phế liệu nuôi Đường Phóng Mass Driver L5 để đóng tàu Ark — là mối nguy công nghiệp chậm rãi, dễ đọc, dễ né và tuân theo vòng cung tiến triển: **Ẩn nấp → Thấu hiểu → Thẻ Từ Miễn Nhiễm → Lập Trình Thành Đồng Minh**.

---

## 1. Mô Hình Hao Mòn (The Wear Model)

### 1.1 Quy Tắc Va Chạm: Vận Tốc Pháp Tuyến Tương Đối, Ngưỡng & Đường Cong

Trong [`RoverController.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/RoverController.cs#L38-L40), khối vật lý của 07 được điều khiển bởi `Rigidbody _body` và `SphereCollider _sphere`, với thông số di chuyển gốc nằm tại [`DriveSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/DriveSettings.cs#L18-L24), [`BoostSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/BoostSettings.cs#L15-L24) và [`HoverJumpSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/HoverJumpSettings.cs#L20-L50):
- **Tốc độ hành trình tiến tiêu chuẩn (`_topSpeed`):** `8.0 m/s` (`DriveSettings.cs:18`)
- **Tốc độ lùi tối đa (`_reverseTopSpeed`):** `3.5 m/s` (`DriveSettings.cs:22`)
- **Tốc độ kích hoạt cuộn tăng tốc trên đường bằng (`_topSpeed + _extraSpeed`):** `9.6 m/s` (`BoostSettings.cs:15`, kích hoạt khi vượt `85%` tốc độ hành trình = `6.8 m/s`)
- **Tốc độ đổ dốc / nhảy kết hợp tăng tốc:** `10.0–12.5 m/s`
- **Tốc độ tiếp đất có đệm cuộn từ (`_cushionLandingSpeed`):** `1.6 m/s` (`HoverJumpSettings.cs:48`)

#### Công Thức Vận Tốc Pháp Tuyến (`[PROPOSAL]`)
Những cú quệt sượt dọc vách miệng hố hoặc sườn tảng đá chỉ nên tóe tia lửa nhẹ mà không phạt người chơi. Do đó, hao mòn va chạm chỉ tính theo **thành phần vận tốc tương đối vuông góc (hướng thẳng vào bề mặt cứng)** tại điểm tiếp xúc:

$$v_\perp = \max\!\big(0,\; -(\mathbf{v}_{07} - \mathbf{v}_{\text{collider}}) \cdot \mathbf{n}_{\text{contact}}\big)$$

trong đó $\mathbf{n}_{\text{contact}}$ là vector pháp tuyến đơn vị hướng từ bề mặt vật cản ra phía 07.

- **Ngưỡng Va Chạm Cứng ($v_{\text{min}}$):** **`6.8 m/s`** (`[PROPOSAL]` dải tinh chỉnh: `6.5–7.5 m/s`, tương đương `85%` tốc độ hành trình tối đa).
  - Nếu $v_\perp < v_{\text{min}}$, **hao mòn bằng `0`**. Mọi cú chạm nhẹ khi đỗ xe, đi lùi (`3.5 m/s`), bò chậm khi cạn pin (`2.0 m/s`), hoặc quệt sượt góc hẹp $25^\circ$ ở tốc độ tối đa ($8.0 \times \sin 25^\circ = 3.38\text{ m/s} < 6.8\text{ m/s}$) đều **không mất một giọt độ bền nào**.
- **Đường Cong Sát Thương Trên Ngưỡng ($v_\perp \ge v_{\text{min}}$):**
  Từ $v_{\text{min}} = 6.8\text{ m/s}$ đến ngưỡng bão hòa $v_{\text{max}} = 12.0\text{ m/s}$, lượng độ bền mất đi tăng mượt theo hàm mũ lồi ($p = 1.5$) để một cú phanh muộn chỉ nhắc nhở nhẹ, còn cú tông trực diện khi đang Boost đổ dốc mới gây móp nặng:

$$\Delta W(v_\perp) = \text{round}\!\left(W_{\text{hit,min}} + (W_{\text{hit,max}} - W_{\text{hit,min}}) \cdot \operatorname{clamp01}\!\left(\frac{v_\perp - v_{\text{min}}}{v_{\text{max}} - v_{\text{min}}}\right)^{1.5}\right)$$

| Tình Huống Va Chạm | Vận Tốc Pháp Tuyến $v_\perp$ | Độ Bền Mất ($\Delta W$ trên thang `100`) | Cảm Giác & Hệ Quả Gameplay |
| :--- | :---: | :---: | :--- |
| Chạm nhẹ / lùi xe / quệt sượt góc $< 30^\circ$ | `< 6.8 m/s` | **`0%`** | Tiếng cộc kim loại nhẹ + bụi tung; `0` sát thương. |
| Chớm ngưỡng (phanh muộn chạm tảng đá) | `6.8 – 7.2 m/s` | **`4% – 6%`** | Tiếng chíp nhăn mặt ngắn, tia lửa nhỏ, thanh độ bền HUD hiện lên. |
| Đâm trực diện ở tốc độ hành trình (`100%` cruise) | `8.0 m/s` | **`12%`** | Tiếng uỳnh rõ, mặt CRT nhăn (`> <`), cần ~8 cú đâm trực diện mới hỏng hoàn toàn từ `100%`. |
| Đâm trực diện khi bật Boost Coil (`8.0 + 1.6 m/s`) | `9.6 m/s` | **`24%`** | Tiếng vỏ thép móp mạnh, mưa tia lửa, 2 cú là sang giai đoạn Móp/Cong ăng-ten. |
| Lao dốc đứng + Boost đâm trực diện | `11.5 – 12.0+ m/s` | **`38% – 40%` (trần)** | Chấn động cấu trúc lớn; nhưng không bao giờ "one-shot" khi đầy máu (tối thiểu 3 cú đâm tệ nhất mới về `0%`). |

- **Thời Gian Miễn Nhiễm Sau Va Chạm (Grace Cooldown):** **`1.0 s`** (`[PROPOSAL]` dải `0.8–1.2 s`). Khi nảy giữa hai tảng đá sát nhau trong vòng `1.0 s`, hệ thống chỉ tính duy nhất cú va chạm có $v_\perp$ cao nhất, triệt tiêu hoàn toàn hiện tượng kẹt vật lý chết oan.

---

### 1.2 Bề Mặt Nào Là Cứng (Hard) vs. An Toàn (Safe)

Để giữ trọn Trụ cột 2 (*"Weight and Bounce"* — nhảy thấp trọng lực và lăn qua đồi bụi luôn vui vẻ), các collider được chia làm hai nhóm vật liệu rõ rệt:

| Nhóm Bề Mặt | Ví Dụ Trong Thế Giới | Có Gây Hao Mòn? | Lý Giải Vật Lý & Gameplay |
| :--- | :--- | :---: | :--- |
| **AN TOÀN (Bụi Regolith / Địa hình mềm)** | Bụi biển bazan (mare), sườn đồi bụi cao nguyên, bờ miệng hố phủ bụi, đường đất nén (`Terrain` layer). | **KHÔNG BAO GIỜ (`0%`)** | Lớp bụi regolith dày hấp thụ xung lực bánh xe và gầm. Kết hợp với `HoverJumpSettings._cushionLandingSpeed = 1.6 m/s`, **mọi cú tiếp đất xuống địa hình bụi đều tốn `0` độ bền bất kể độ cao**. |
| **CỨNG (Đá Tảng, Kim Loại & Công Trình)** | Tảng đá bazan rời, mỏm đá sắc nhọn quanh miệng hố, khung vệ tinh rơi, xác xe tự hành cũ, công trình căn cứ (`StructureDefinition`), đường ray Mass Driver, thân vỏ Reclaimer. | **CÓ (nếu $v_\perp \ge 6.8\text{ m/s}$)** | Đá bazan đặc và thép cán nguội không đàn hồi sẽ làm biến dạng lớp vỏ nhôm-thép `1.5 mm` của 07 khi đâm trực diện ở tốc độ cao. |

---

### 1.3 Tiếng Kêu Cót Két Cảnh Báo (Đọc Được Trước Khi Đau)

Hư hại không bao giờ ập đến bất ngờ. Ngay trước khi 07 chạm ngưỡng tốc độ có thể gây móp vỏ khi đâm, khung gầm của 07 sẽ phát tín hiệu cảnh báo vật lý:
- **Ngưỡng Kích Hoạt:** Bắt đầu êm ở **`6.5 m/s`** (`~81%` tốc độ hành trình, sớm hơn ngưỡng sát thương `6.8 m/s`) và tăng dần độ dày/cao độ khi qua `8.0 m/s` (hành trình), `9.6 m/s` (Boost) tới `12.0 m/s` (lao dốc).
- **Âm Thanh:** Tiếng khung thép thập niên 1980 rùng nhẹ — lò xo giảm xóc (`Shock_FL..RR`) cót két, cao su đệm rên nhẹ và âm cộng hưởng khung gầm theo âm giai Ngũ cung Rê trưởng (`A3–E4` truyền qua càng bánh xe `Arm_FL..RR`).
- **Tín Hiệu Thị Giác Nhỏ:** Trên `7.2 m/s`, cần ăng-ten đuôi (`Antenna` / `AntennaTip` trong [`RoverModelNodes.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/RoverModelNodes.cs#L30-L31)) uốn cong về sau và dòng quét màn hình CRT rung nhẹ `1 px`.

---

### 1.4 Bốn Giai Đoạn Thị Giác & Hành Vi + Đồng Hồ HUD

Độ bền được lưu bằng biến chuẩn hóa `WearIntegrity` $\in [0.0, 1.0]$ (`100%` = nguyên vẹn, `0%` = tắt nguồn). Cả thân vỏ 07 v3 (`RoverModelNodes.cs`) lẫn giao diện HUD tối giản (`CellRowSettings.cs`) đều phản ánh qua 4 giai đoạn:

![07 v3 qua 3 giai đoạn hao mòn và 3 khung hình khoảnh khắc tắt nguồn](../../Logs/concepts/wear_stages_and_death.png)

| Giai Đoạn | Khoảng Độ Bền | Hành Vi Thanh HUD | Biểu Hiện Trên Thân 07 v3 (`RoverModelNodes.cs`) | Thay Đổi Vận Hành / Điều Khiển |
| :--- | :---: | :--- | :--- | :--- |
| **0. Nguyên Vẹn (Pristine)** | **`100%`** | **Ẩn hoàn toàn.** Chỉ hiện thanh pin (hoặc ẩn cả hai khi đầy). | Vỏ sơn kem `R1_HullCream` (`#E4DFD3`) phẳng phiu, sọc cam san hô `R1_AccentCoral` sắc nét, `Antenna` thẳng đứng, hai đèn pha `R1_LampAmber` sáng ấm, mắt CRT bình thản (`O O`). | `100%` chuẩn (`8.0 m/s` hành trình, nhảy cao `7.0 m`, quét `1.0×`). |
| **1. Mới / Trầy Nhẹ (Fresh)** | **`99% – 65%`** | **Hiện (Vàng Hổ Phách Ấm).** Thanh độ bền 5 đốt nhỏ gọn hiện cạnh thanh pin khi `< 100%`; nháy nhẹ khi va chạm. | Vết xước bụi/sơn nhẹ ở mép gầu xúc (`DigScoop`) và góc cản trước. Khi va chạm: mặt CRT nhăn `0.35 s` (`FaceFeeling.Startled` + tiếng chíp ngũ cung 2 nốt `F#4 → D4`). | `100%` chuẩn. Không giảm tốc độ hay tốc độ quét — trầy nhẹ chỉ mang tính phản hồi xúc giác. |
| **2. Móp Vỏ (Dented)** | **`64% – 25%`** | **Hiện (Vàng Đất Đậm).** Đốt 4–5 tắt; nhịp sáng nhẹ khi nhận thêm va chạm. | - Vết móp rõ trên các tấm hông `Body`.<br>- Cần `Antenna` sau cong nghiêng `14°`.<br>- Đèn pha trái (`HeadlampSocket`) chập chờn nhẹ khi xóc.<br>- Vết nứt chân chim ở góc kính `Visor`.<br>- Mặt CRT khi đứng yên thiên về `FaceFeeling.Tired` (`- -`). | Giữ nguyên `100%` tốc độ chạy và nhảy để hành trình khám phá không bị ì ạch; đèn pha trái thỉnh thoảng nháy nhẹ trong đêm. |
| **3. Nguy Kịch (Critical)** | **`24% – 1%`** | **Hiện (Đỏ San Hô Nhấp Nháy `#C86446`).** Đốt cuối chớp `1.5 Hz` kèm tiếng rơ-le tích tắc khẽ. | - Vỏ `Body` và `CargoHatch` móp lệch nặng.<br>- Bánh trước trái (`Wheel_FL` / `Arm_FL`) đảo nghiêng `4°`, giảm xóc `Shock_FL` xệ xuống.<br>- Tia lửa vàng cam bắn lác đác từ khe `StitchOrigin` mỗi `4–6 s`.<br>- Màn hình CRT (`R1CrtFace.shader`) bật `_FaceCrtState.z` (`lowPower/jitter = 0.65`) với biểu cảm `FaceFeeling.Worried` (`> <`). | **Làm mềm hiệu năng để tự bảo vệ 07 trước khi chết:**<br>- **Tốc độ tối đa giới hạn ở `75%` (`6.0 m/s`)** — *thấp hơn* ngưỡng va chạm cứng `6.8 m/s` để 07 không thể tự đâm chết mình trên đường bằng!<br>- **Khóa Boost Coils.**<br>- **Khóa Leap Coils (`HoverJump`)** (bấm Space chỉ nảy nhẹ `0.25 m` kèm tiếng lạch cạch rơ-le).<br>- **Thời gian khóa mục tiêu máy quét chậm hơn `+40%` (`1.4×`).** |

> [!TIP]
> **Vì Sao Giai Đoạn Nguy Kịch (`< 25%`) Giới Hạn Tốc Độ Ở `6.0 m/s`:** Vì `6.0 m/s` thấp hơn ngưỡng sát thương va chạm `6.8 m/s`, một chú 07 đang trọng thương tậpập tễnh bò về nhà trên địa hình bằng phẳng **không bao giờ chết oan vì lỡ quệt phải một hòn đá ven đường**. Để mất nốt `24%` cuối cùng, 07 chỉ có thể lao dốc đứng vượt `6.8 m/s`, đi sâu vào vùng nguy hiểm không đồ bảo hộ, hoặc bị Reclaimer kẹp giữ.

---

### 1.5 Khoảnh Khắc Tắt Nguồn (Death Moment) & Luồng Khởi Động Lại (Storyboard)

Khi `WearIntegrity` chạm `0%`, **không có vụ nổ kiểu arcade, không nháy đỏ màn hình và không có chữ "GAME OVER"**. Đúng tinh thần Trụ cột 1 (*"Lonely, Then Warm"*) và tận dụng trực tiếp [`R1CrtFace.shader`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Shaders/R1CrtFace.shader#L34-L37) (vốn đã hỗ trợ thu hẹp dọc CRT qua `_FaceContentState.y` và vệt sáng ngang phốt-pho qua `_FaceContentState.z`), cái chết của 07 là một thước phim tắt nguồn dài `2.8 s` đẹp, buồn và tĩnh lặng:

| Khung Hình | Thời Gian | Hành Động Thị Giác & Shader (`R1CrtFace.shader` / `RoverModelNodes`) | Âm Thanh & Máy Quay |
| :--- | :---: | :--- | :--- |
| **Khung 1: Rùng Mình Cuối & Nhả Hộp Hàng Báo Hiệu** | `0.0 s – 0.6 s` | Những tia lửa va chạm cuối cùng rơi xuống bụi xám. Cả 4 ống giảm xóc (`Shock_FL..RR`) lún xuống `8 cm`, khớp cổ (`Neck`) gục nhẹ `12°` về trước. Nếu khoang hàng (`CargoHold`) có đồ, nắp `CargoHatch` phía sau bật mở và đẩy một **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** màu cam-đồng kín khít rơi xuống cạnh 07. | Tay cầm/bàn phím nhả điều khiển mượt mà. Máy quay lùi nhẹ `1.8 m` và hạ thấp ngang tầm mắt. Hai nốt nhạc cello/synth Ngũ cung Rê trưởng trầm xuống (`A4 → D4`) vang lên cùng tiếng ngắt rơ-le cơ khí. |
| **Khung 2: Màn Hình CRT Thu Thành Một Đường Chỉ Ngang** | `0.6 s – 1.5 s` | `FaceDisplay.cs` kéo `ScreenVerticalPinch` (`0.0 → 1.0`) và `ScreenLineGlow` (`0.0 → 1.0`). Đôi mắt viên thuốc màu hổ phách của 07 khép dọc lại thành **một đường chỉ sáng ngang mảnh sắc nét** giữa màn kính CRT tối, rồi thu gọn tiếp thành một chấm sáng nhỏ ở tâm. Cùng lúc đó, đèn pha (`R1_LampAmber`) và hàng đèn pin hông (`BatteryLamp_0..7`) lụi dần từ vàng ấm sang đỏ than rồi tắt hẳn. | Tiếng xả tụ cao áp CRT cổ điển thập niên 1980 (*fweeee-tik*) nhỏ dần khi tiếng sóng mang radio mỏng đi rồi tan vào sự im lặng tuyệt đối của chân không. |
| **Khung 3: Tĩnh Lặng & Đóng Rơ-Le Hồi Sinh** | `1.5 s – 2.8 s` | Chấm sáng giữa màn hình CRT tắt hẳn, để lại mặt kính tối phản chiếu bầu trời sao. Đèn đỉnh ăng-ten (`AntennaTip`) tắt. 07 nằm bất động trong bóng tối sắc lạnh của tảng đá dưới Trái Đất tối không ánh đèn thành phố, trong khi **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** bên cạnh bắt đầu nhịp chớp vàng ấm áp `0.5 Hz`. Ở `2.8 s`, màn hình mờ dần sang đen (`1.2 s`) cùng tiếng lách cách băng từ, rồi mở ra tại **Khung Bảo Dưỡng (Service Gantry)** hoặc **Trạm Xe (Rover Bay)** gần nhất. | Khoảng lặng `1.0 s`, nối tiếp bằng tiếng tụ điện ấm áp tại `Service Gantry` / `Rover Bay` nạp đầy khi màn hình CRT của 07 khởi động mở dọc trở lại (`BootProgress: 0.0 → 1.0`). |

---

### 1.6 Hệ Quả Khi Chết, Lý Giải Vật Lý Trong Truyện & 2 Phương Án Thay Thế

#### A. Hệ Quả Mặc Định (`[DECIDED]` theo Phán quyết 17)
1. **Nơi 07 Khởi Động Lại:** 07 thức dậy tại **Khung Bảo Dưỡng (`Service Gantry`) hoặc Trạm Xe (`Rover Bay`)** cuối cùng từng ghé (mặc định là `Rover Bay` tại Thung Lũng Nhà trước khi xây trạm tiền tiêu).
2. **Những Gì 07 Giữ Nguyên:** Toàn bộ thiết bị/kit đã lắp trên xe, nâng cấp pin/khoang hàng, công thức chế tạo đã mở, mục từ điển đã quét, đồng minh đã lập trình và tiến trình cốt truyện được **giữ nguyên 100%**.
3. **Những Gì Ở Lại Hiện Trường:** Chỉ các **nguyên liệu thô và vật phẩm rời đang nằm trong `CargoHold`** ([`M3-25`](file:///D:/Project/MoonProject-wt/agy-wear/docs/features/M3-25-cargo-and-storage.md)) nằm lại bên trong **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** tại đúng tọa độ tắt nguồn (hoặc nằm trong **Thùng Phân Loại Số 03 — Sorting Bin 03** tại Bãi Thu Hồi nếu 07 tắt nguồn trong kẹp của Reclaimer — xem §3.2).
   - **Cargo Beacon Pod** phát cột sáng hổ phách dịu và hiển thị trên la bàn/máy quét từ mọi khoảng cách.
   - Lái xe tới chạm vào hoặc bấm Tương tác (`E`) sẽ nạp toàn bộ hàng trở lại khoang của 07.
   - **Quy Tắc Nhiều Lần Chết:** Tối đa `3` Cargo Beacon có thể tồn tại đồng thời trên thế giới (tránh việc lỡ đâm xe lần nữa trên đường đi nhặt lại làm mất mẻ hàng quý trước đó).

#### B. 07 Quay Về Bằng Cách Nào Theo Logic Vật Lý Trong Truyện? (Không Phép Thuật)
Phán quyết 17 yêu cầu một **lời giải thích vật lý 100% trong thế giới truyện** để việc trở lại căn cứ không bao giờ giống phép dịch chuyển tức thời của game. Chúng tôi kết hợp các gợi ý của Phán quyết 17 thành một cơ chế vật lý liền mạch bám sát [`story-v4.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/story-v4.md) (`[PROPOSAL]`):

1. **Vì Sao Hàng Ở Lại Còn Thân Xe Về Căn Cứ ("Cơ Chế Nhả Tải Khẩn Cấp & Kéo Cứu Hộ Ban Đêm"):**
   - Khi cảm biến khung gầm báo độ bền chạm `0%`, **Phản Xạ An Toàn Lumen** tự động tháo chốt và đẩy **Lõi Khoang Hàng Chống Sốc (Cargo Beacon Pod)** ra ngoài để giảm tải trọng chết, bảo vệ quặng/linh kiện và phát sóng định vị tần số thấp.
   - Trong `1.2 s` chuyển cảnh đen (đại diện cho vài giờ yên tĩnh trôi qua trên Mặt Trăng):
     - **Khi hỏng do va chạm / thời tiết ngoài thực địa:** **Tilly (`03`)** (sau khi đã kết bạn) hoặc **Tời Kéo Từ Tính / Xe Kéo Cứu Hộ Tự Động** của căn cứ lần theo sóng định vị, móc cáp kéo khung xe đã nhẹ tải của 07 về **Rover Bay / Service Gantry** gần nhất. Tại đó, hai cánh tay búa và mỏ hàn của Gantry gõ phẳng lại vỏ, hàn kín các đường nứt và kích điện lại ống CRT. Khi màn hình của 07 sáng lên (`BootProgress 0 → 1`), người chơi nhìn thấy rõ cánh tay hàn của Gantry vừa thu lên khỏi sườn xe còn ấm ánh lửa hàn — xác nhận trực quan rằng 07 vừa được kéo về và gò lại trong lúc ngất!
     - **Khi bị Reclaimer kẹp tắt nguồn (hoặc rơi xuống vực sâu không thể kéo):** Cỗ máy Reclaimer kéo khung vỏ móp cũ của 07 về **Bãi Thu Hồi (Reclamation Yard)** và đổ Hộp Hàng Báo Hiệu vào **Thùng Phân Loại Số 03 (`BIN 03: UNTAGGED UNITS`)**. Cùng lúc đó tại Trạm Xe căn cứ, **Giao Thức Sao Lưu Hộp Đen 1988 của Kenji** phát hiện mất sóng mang từ 07, tự động nạp **Bản Sao Bộ Nhớ Từ Tính (Telemetry Mirror)** (vốn được đồng bộ liên tục qua sóng S-band) vào một **Khung Gầm Dự Phòng `Lumen-07` chưa sơn** đang xếp trên giá của Rover Bay, dập số `07` lên hông và hạ xuống sàn! *(Tuyệt vời hơn nữa: khi 07 lái tới Bãi Thu Hồi để lấy lại Hộp Hàng trong Thùng 03, người chơi sẽ nhìn thấy chính chiếc vỏ `07` cũ móp méo của mình nằm trong thùng và có thể tháo dỡ lấy `2 Metal + 1 Wiring`!)*

#### C. Hai Phương Án Thay Thế Cho Hệ Quả Khi Chết (Ưu & Nhược Điểm)

Theo yêu cầu của `Logs/TASK.md`, dưới đây là 2 phương án thay thế hoàn chỉnh bên cạnh phương án Mặc định để Chủ dự án cân nhắc:

| Mô Hình Hệ Quả | Cách Hoạt Động | Ưu Điểm (Pros) | Nhược Điểm (Cons) |
| :--- | :--- | :--- | :--- |
| **Mặc Định (`[DECIDED]` Baseline):<br>Khởi Động Tại Gantry + 100% Hàng Ở Lại Trong Beacon** | 07 tỉnh dậy tại Dock/Gantry gần nhất cùng mọi kit; `100%` hàng rời trong khoang nằm lại tại hiện trường (hoặc Thùng Phân Loại ở Bãi Thu Hồi) trong một Cargo Beacon có thể thu hồi. | - Không mất mát vĩnh viễn; biến tai nạn thành một chuyến đi cứu hộ có mục tiêu.<br>- Tách bạch rõ *tiến trình vĩnh viễn* (kit/bản vẽ) với *rủi ro chuyến đi* (hàng trong khoang). | - Người chơi phải lái xe quay lại chỗ cũ để nhặt hàng; có thể hơi mệt nếu chết quá xa chỉ vì một cú trượt tay. |
| **Phương Án Thay Thế 1 (`[PROPOSAL]`):<br>"Tilly Kéo Cả Xe Lẫn Hàng" — Giữ 75% Hàng, Rơi Mất 25% Dọc Đường (Không Cần Chạy Đi Nhặt)** | Tilly hoặc tời cứu hộ kéo cả 07 *lẫn* khoang hàng về thẳng Gantry. Do nắp khoang bị móp khi đâm, `25%` nguyên liệu phổ thông (`Metal`, `Ilmenite`) bị rơi mất dọc đường kéo (vật phẩm hiếm/độc bản không bao giờ rơi); `75%` còn nguyên trong khoang ngay khi tỉnh dậy. | - **Không phải chạy lại đường cũ (Zero Corpse-Run):** Tiết kiệm thời gian, nhịp chơi liền mạch.<br>- Cực kỳ hợp lý về mặt câu chuyện (nếu Tilly đã kéo 07 về thì kéo luôn cả thùng hàng). | - Giảm độ hồi hộp khi chở đầy khoang hàng quý về nhà.<br>- Xóa vĩnh viễn `25%` quặng phổ thông thay vì cho người chơi cơ hội lấy lại đủ `100%`. |
| **Phương Án Thay Thế 2 (`[PROPOSAL]`):<br>"Khung Xe Mắc Kẹt & Xe Cứu Hộ Phụ" — Lái Một Khung Xe Thô Ra Hàn Cứu 07 Chính** | Thân xác thật của 07 (cùng mọi kit và hàng) nằm tắt nguồn tại hiện trường. Người chơi tỉnh dậy ở căn cứ trong một **Xe Kéo Phụ (`07-B`)** chỉ có bộ kích điện/mỏ hàn cơ bản, phải tự lái ra hiện trường để hàn và kích nguồn lại thân xe `07` chính. | - **Tính liên tục vật lý tuyệt đối 100%:** Chỉ có duy nhất một cơ thể `07` và chính tay bạn đi cứu nó. | - **Rủi ro gây ức chế cao:** Lái một chiếc xe phụ chưa nâng cấp vượt qua vùng `0.6+` xa xôi (đặc biệt nơi có chướng ngại thời tiết cần kit bảo vệ!) rất dễ dẫn tới kẹt cứng (soft-lock). |

---

### 1.7 Bảng Thông Số Cân Bằng Đề Xuất (`[PROPOSAL]`)

| Thông Số | Giá Trị Đề Xuất | Dải Tinh Chỉnh | Vị Trí Code / Asset | Ghi Chú |
| :--- | :---: | :---: | :--- | :--- |
| `MaxWearIntegrity` | **`100.0`** | `100 – 150` | `WearSettings.cs` (mới) | Tăng lên `150` khi lắp Vỏ Hợp Kim Cứng (`Hardened Hull`). |
| `CreakStartSpeed` | **`6.5 m/s`** | `6.0 – 7.2 m/s` | `WearSettings.cs` | `81%` tốc độ hành trình `8.0 m/s` (`DriveSettings._topSpeed`). |
| `HardImpactMinSpeed` ($v_{\text{min}}$) | **`6.8 m/s`** | `6.5 – 7.5 m/s` | `WearSettings.cs` | Vận tốc pháp tuyến $v_\perp$ dưới mức này gây `0` hao mòn. |
| `HardImpactMaxSpeed` ($v_{\text{max}}$) | **`12.0 m/s`** | `11.0 – 14.0 m/s` | `WearSettings.cs` | Tốc độ chạm trần sát thương `MaxHitWear`. |
| `MinHitWear` ($W_{\text{hit,min}}$) | **`4.0`** (`4%`) | `3.0 – 6.0` | `WearSettings.cs` | Độ bền mất ngay tại ngưỡng $v_\perp = 6.8\text{ m/s}$. |
| `MaxHitWear` ($W_{\text{hit,max}}$) | **`40.0`** (`40%`) | `30.0 – 50.0` | `WearSettings.cs` | Trần sát thương mỗi cú đâm (không bao giờ chết ngay từ `100%`). |
| `ImpactCurveExponent` ($p$) | **`1.5`** | `1.2 – 2.0` | `WearSettings.cs` | Độ cong hàm mũ mượt từ `6.8` đến `12.0 m/s`. |
| `PostHitGraceSeconds` | **`1.0 s`** | `0.8 – 1.5 s` | `WearSettings.cs` | Miễn nhiễm ngắn chống tính trùng sát thương khi nảy. |
| `DentedStageThreshold` | **`65%`** | `60% – 70%` | `WearSettings.cs` | Bắt đầu Giai đoạn 2 (móp vỏ, cong ăng-ten, đèn nháy). |
| `CriticalStageThreshold` | **`25%`** | `20% – 30%` | `WearSettings.cs` | Bắt đầu Giai đoạn 3 (bánh đảo, tia lửa, giảm tốc bảo vệ). |
| `CriticalTopSpeedMultiplier` | **`0.75×`** (`6.0 m/s`) | `0.70× – 0.80×` | `WearSettings.cs` | An toàn dưới ngưỡng $v_{\text{min}} = 6.8\text{ m/s}$ trên đường bằng. |
| `CriticalScanTimeMultiplier` | **`1.40×`** (`+40%`) | `1.25× – 1.50×` | `WearSettings.cs` | Thời gian quét lâu hơn đôi chút khi ở Giai đoạn 3. |

---

### 1.8 Tương Tác Với Pin (`M3-20`), Cuộn Nhảy (Leap Coils) & Kéo (Tow)

1. **Pin (`M3-20-battery-chargers-crawl.md`):**
   - **Hai Kênh Hoàn Toàn Độc Lập:** Pin đo *năng lượng/tầm đi xa*; Wear đo *độ nguyên vẹn cơ học*.
   - **Miễn Nhiễm Tự Nhiên Khi Bò Hết Pin:** Khi pin về `0%`, 07 bò ở tốc độ `25%` (`2.0 m/s`). Vì `2.0 m/s << 6.8 m/s`, **07 không thể bị móp vỏ do va chạm khi đang bò hết pin** trên địa hình thường.
   - **Khi Vừa Hết Pin Vừa Nguy Kịch:** Tốc độ lấy `min(batteryMult, wearMult) = 0.25×` (`2.0 m/s`) thay vì nhân chồng làm xe đứng im.
2. **Cuộn Nhảy Từ Tính (`HoverJumpSettings.cs`):**
   - **Ở Giai đoạn 0–2 (`100%–25%` độ bền):** Nhảy hoạt động `100%` công suất (`0.8 m` nhấp nhẹ, `7.0 m` nhảy cao, hạ cánh đệm từ `1.6 m/s`). Mọi cú tiếp đất có đệm xuống bụi hoặc sàn công trình đều tốn **0 độ bền**.
   - **Ở Giai đoạn 3 (`< 25%` Nguy kịch):** Khóa xả tụ nhảy chính để tránh gãy khung. Bấm `Space` chỉ nảy nhẹ `0.25 m` kèm tiếng lạch cạch rơ-le nhắc nhở.
   - **Khi Đang Bị Reclaimer Kẹp:** Bấm `Space` xả xung từ của cuộn nhảy tạo ra lực đẩy ngược từ trường, cộng ngay **`+60%` thanh thoát kẹp** (xem §3.2).
3. **Kéo Tải & Gỡ Kẹt (`RecoverySettings.cs` / `TowableMagnet`):**
   - Các va chạm từ chính vật thể đang được kéo phía sau 07 được **bỏ qua (`0` hao mòn)** để vật lý dây kéo không bao giờ làm hại người chơi.
   - Khi bấm nút gỡ kẹt (`RecoverySettings._verticalPop = 1.6 m/s`), 07 nhận `1.5 s` miễn nhiễm va chạm.

---

### 1.9 Các Chế Độ Chơi Mới (`Survival`, `Calm` và Đánh Giá `Hardcore`)

Khi tạo trò chơi mới (**New Game**), người chơi chọn giữa các chế độ rõ ràng theo tinh thần *Subnautica*:

| Chế Độ | Trạng Thái | Quy Tắc & Hành Vi | Đối Tượng Người Chơi |
| :--- | :---: | :--- | :--- |
| **1. Survival (Mặc Định)** | **`[DECIDED]`** | Đầy đủ Hao mòn (Wear), Sửa chữa vật lý (Patch Kit & Service Gantry), Reclaimers, Nguy hiểm vùng miền và vòng lặp Tắt nguồn / Thu hồi Cargo Beacon. Pin vẫn không gây chết (`M3-20`). | Trải nghiệm mặc định: khám phá tĩnh lặng kết hợp chuẩn bị kỹ thuật xúc giác và sự hồi hộp vừa đủ. |
| **2. Calm (Yên Bình)** | **`[DECIDED]`** | **Không hao mòn, không chết.** `WearIntegrity` khóa ở `100%` (thanh độ bền HUD luôn ẩn). Đâm vào đá vẫn có tiếng cộc kim loại và bụi tung vui mắt. Reclaimers vẫn tuần tra ở vùng `0.6+` như những cỗ máy cảnh quan sống động, **không bao giờ kẹp 07**, và vẫn có thể được quét/lập trình thành đồng minh chở phế liệu về nhà! | Người chơi muốn tận hưởng trọn vẹn *Mặt Trăng cô đơn ấm áp* hoàn toàn thư giãn, không áp lực hỏng xe. |
| **3. Hardcore ("Một Khung Gầm Duy Nhất")** | **`[PROPOSAL]` Đánh Giá** | Giống hệt Survival, nhưng **không có khung xe dự phòng**: nếu `WearIntegrity` chạm `0%`, file lưu kết thúc vĩnh viễn sau Khung hình 3 của cảnh tắt nguồn CRT. | Người chơi kỳ cựu muốn thử thách căng thẳng tuyệt đối ở lần chơi thứ hai. |

> [!WARNING]
> **Chế Độ Hardcore Một Mạng Có Hợp Với *Lunarion* Không? (Đánh Giá Của Đạo Diễn):**  
> **Không nên đưa vào `0.5.7`, và chỉ nên là tùy chọn ẩn/nâng cao ở `1.0`.** Một hành trình *Lunarion* hoàn chỉnh kéo dài `12–20 giờ` xây dựng căn cứ và đánh thức những người bạn robot (`Pillar 3` & `Pillar 4`). Mất trắng file lưu 15 giờ chỉ vì một cú lăn dốc quá đà sẽ phá vỡ lời hứa *"Mặt Trăng mặc định là chốn bình yên"* và khiến người chơi rón rén bò `4 m/s` khắp bản đồ. **Khuyến nghị:** Phát hành **Survival (Mặc định)** và **Calm** trong `0.5.7`.

---

## 2. Sửa Chữa Cơ Lý (Phán Quyết 14: 07 Không Có Bàn Tay)

Theo [`docs/VISION.md` Phán quyết 14](file:///D:/Project/MoonProject-wt/agy-wear/docs/VISION.md#L158-L162), **07 không có bàn tay, không có cánh tay người và không có đám mây sửa chữa phép thuật**. Mọi bước sửa chữa đều phải diễn ra qua các cơ cấu có thật trên thân 07 v3 ([`RoverModelNodes.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/RoverModelNodes.cs)) hoặc trên công trình căn cứ.

### 2.1 Tự Vá Ngoài Thực Địa: Bộ Vá Vỏ — Patch Kit (`item.patch_kit`)

![Storyboard 3 khung hình: 07 tự gắn và hàn tấm vá vỏ mà không cần bàn tay](../../Logs/concepts/patch_kit_storyboard.png)

#### Công Thức Chế Tạo & Lưu Trữ (`SalvageMaterial.cs` & `M3-27`)
Được chế tạo tại **Máy Chế Tạo (Fabricator)** (`StructureIds.Fabricator = "structure.fabricator"`) từ các nguyên liệu T1/T2 có sẵn trong [`SalvageMaterial.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Core/SalvageMaterial.cs#L12-L41):

| Mã Vật Phẩm | Tên Gọi | Cấp & Cột Mốc | Công Thức Chế Tạo (`SalvageMaterial`) | Thời Gian Chế | Xếp Chồng Trong Khoang | Độ Bền Hồi Phục |
| :--- | :--- | :---: | :--- | :---: | :---: | :---: |
| `item.patch_kit` | **Bộ Vá Vỏ (Hull Patch Kit)** | **T1 (`0.5.7`)** | **`2 × Metal` + `1 × Wiring`** | `2.5 s` | `3` bộ / ô khoang hàng | **`+40%` (`+40` Wear)** |
| `item.patch_kit_heavy` | **Tấm Vá Hợp Kim (Alloy Weld-Patch)** | **T2 (`0.6+`)** | **`1 × Alloy` + `1 × Wiring` + `1 × OrangeBeads`** | `3.5 s` | `3` bộ / ô khoang hàng | **`+80%` (`+80` Wear)** + xóa ngay trạng thái Nguy kịch |

#### Storyboard 3.5 Giây: 07 Tự Vá Vỏ Không Cần Bàn Tay Như Thế Nào?
Khi `WearIntegrity < 100%` và 07 đang dừng (hoặc đi `< 1.0 m/s`), bấm phím **Vá Nhanh (`R`)** (hoặc chọn Patch Kit trong khoang hàng) sẽ kích hoạt **chuỗi tự hàn vật lý dài `3.5 s`** tận dụng `CargoHatch`, nam châm điện gầm và **Đầu Cắt/Hàn Ngực (`CutterTurret` / `StitchOrigin`)** (`RoverModelNodes.cs:37-44`):

1. **Khung 1 — `0.0 s – 1.0 s` (Bật Nắp Khoang & Bắt Dính Từ Trường):**
   - 07 phanh đứng lại. Nắp khoang hàng sau (`CargoHatch`) bật mở `25°` và đẩy ra một tấm thép dập sẵn màu xanh xô thơm (`R1_SagePatch` `#627360` có đinh tán đồng ở 4 góc).
   - Cùng lúc đó, nam châm điện gầm và gầu xúc (`DigScoop`) kích hoạt với tiếng ngân trầm `D2`, bắt giữ tấm thép trong một dải từ trường xanh-hổ phách rồi kéo trượt nó vòng ra mảng sườn đang bị móp trên `Body`. Đầu CRT của 07 (`Head`) cúi xuống nhìn theo tấm vá (`FaceFeeling.Curious`).
2. **Khung 2 — `1.0 s – 2.8 s` (Kẹp Từ & Khâu Mối Hàn Hồ Quang):**
   - Với một tiếng *CẠCH* đanh gọn, nam châm điện ép chặt tấm vá xanh xô thơm áp khít lên vết lõm trên hông `Body`.
   - **Ụ Cắt/Hàn bằng đồng trên ngực (`CutterTurret`)** xoay nghiêng về phía sườn của chính 07 và bắn một tia hàn hồ quang vàng rực từ `StitchOrigin`, chạy dọc đường viền tấm vá trong `1.8 s`.
   - Mưa tia lửa vàng ấm bắn xuống lớp bụi xám; đôi mắt CRT của 07 nheo tít lại (`> <`) trước ánh chớp hàn.
3. **Khung 3 — `2.8 s – 3.5 s` (Bật Phẳng & Tiếng Chíp Vui Vẻ):**
   - Khi đường hàn nguội dần từ cam rực sang màu đồng xước (`R1_Brass`), lực giãn nở nhiệt làm vết móp bật phẳng ra (`+40%` độ bền; cần ăng-ten cong bật thẳng lại nếu vượt mốc `65%`).
   - Tấm vá màu xanh xô thơm (`R1_SagePatch`) nằm lại hiện hữu ngay trên sườn màu kem của 07 như một "miếng băng cá nhân cơ khí" đầy tự hào cho tới lần đại tu tiếp theo tại Khung Bảo Dưỡng!
   - Hệ thống treo của 07 nhún nhẹ một nhịp vui tươi, mặt CRT nở nụ cười mắt cong (`^ ^`) cùng hợp âm 3 nốt Ngũ cung Rê trưởng đi lên (`D4 → F#4 → A4`).

---

### 2.2 Sửa Chữa Tại Căn Cứ: Khung Bảo Dưỡng — Service Gantry (`structure.service_gantry`)

![Concept art Khung Bảo Dưỡng (Service Gantry) với 07 v3 bên trong](../../Logs/concepts/service_gantry.png)

Nếu Bộ Vá Vỏ dùng cho các tình huống khẩn cấp ngoài thực địa, thì **Khung Bảo Dưỡng (Service Gantry)** là công trình căn cứ đầu game, rẻ và tự động sửa chữa 07 bằng điện năng của mạng lưới căn cứ (`PowerGrid.cs`).

#### Thông Số Kỹ Thuật (`StructureDefinition.cs` Compatible)

| Trường (`StructureDefinition.cs`) | Giá Trị Đề Xuất (`[PROPOSAL]`) | Lý Giải Thiết Kế |
| :--- | :--- | :--- |
| **`_id` (`StructureIds`)** | `"structure.service_gantry"` | Thêm vào [`StructureIds.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs#L12-L20) cùng với `SolarPanel`, `BatteryRack`, `Fabricator`, `RoverBay`. |
| **`_nameKey`** | `"building.service_gantry.name"` | Tên hiển thị: **Service Gantry** (*Khung Bảo Dưỡng*). |
| **Kích thước (`_width × _depth × _height`)** | **`2.8 m × 2.4 m × 2.35 m`** | Cổng vòm gọn gàng cho phép lái xuyên qua (`07 v3` rộng `1.48 m` × dài `1.49 m` × cao `1.76 m`), nhỏ gọn hơn `UpgradeBay.fbx` (`3.20 m × 2.82 m`). |
| **`_maxGroundSlope` / `_clearance`** | **`15.0°` / `0.25 m`** | Đồng bộ chuẩn đặt công trình trên lưới xây dựng (`StructureDefinition.cs:29-33`). |
| **`_buildCost` (`Recipe`)** | **`3 × Metal` + `2 × Wiring` + `1 × Optics`** | Chi phí T1 rẻ và sớm — xây được ngay trong 15 phút đầu của `0.5.7` sau Tấm Pin Mặt Trời và Giá Pin. |
| **`_buildSeconds`** | **`4.0 s`** | Thời gian lắp dựng nhanh trên đế xây dựng. |
| **Điện năng (`_consumeWatts` trên `PowerGrid.cs`)** | **`120 W` (khi đang gò/hàn) / `10 W` (chờ)** | Lấy điện từ mạng lưới `SolarPanel` / `BatteryRack` (`PowerGrid.cs`). Nếu mạng điện thiếu hụt (`0 < satisfaction < 1`), tốc độ sửa tỉ lệ thuận với công suất cấp; nếu mất điện hoàn toàn (`0 W`), đèn làm việc tắt và quá trình sửa tạm dừng. |
| **`_ports` (`StructurePort[]`)** | **`2` cổng cáp:**<br>`(-1.30, 0.08, 0.0)` hướng `(-1, 0, 0)`<br>`(+1.30, 0.08, 0.0)` hướng `(+1, 0, 0)` | Hai ổ cắm cáp bằng đồng ở hai chân đế trái/phải để nối chuỗi vào lưới điện căn cứ. |
| **Tốc Độ Sửa Chữa** | **`+14% Wear / s`** (`~7.0 s` từ `1% → 100%`) | Đủ nhanh để không phải chờ lâu, đủ nhịp (`3–7 s`) để người chơi thưởng thức hoạt cảnh gò búa và hàn tia lửa. |

#### Nhận Diện Tức Thì & Phong Cách Retro-Future Thập Niên 1980
- **Nhìn Là Hiểu Ngay Chức Năng:** Từ khoảng cách `60 m`, Service Gantry nổi bật với **khung cổng chữ A lái xuyên qua** trên bệ sắt vát cạnh màu than (`R1_Charcoal` `#26292E`) có hai thanh dẫn hướng bánh xe kẻ sọc vàng-đen, hai trụ thép sơn kem-cam (`R1_HullCream` + `R1_AccentCoral`), bảng màn hình CRT chẩn đoán gắn bên trụ phải (`HULL INTEGRITY: 100%`), và **hai cánh tay công cụ cơ khí bằng đồng treo trên thanh ray ngang**:
  1. **Tay Trái:** **Đầu búa gò khí nén bằng đồng (`Planishing Hammer`)** hạ xuống gõ đều từng nhịp (`cốc-cốc-keng` theo nốt `D3/A3`) làm phẳng các vết móp.
  2. **Tay Phải:** **Mỏ hàn hồ quang bằng đồng đỏ** với dây cáp xoắn màu cam khâu kín các vết nứt bằng chùm tia lửa vàng ấm và tháo bỏ các tấm vá tạm `R1_SagePatch`, trả lại lớp vỏ sơn kem bóng đẹp như mới xuất xưởng.

---

### 2.3 Các Gói Nâng Cấp Khung Gầm Tùy Chọn Về Sau (`0.6+`)

Khi tiến vào các vùng `0.6+` hiểm trở, người chơi có thể chế tạo và lắp đặt các gói gia cố vĩnh viễn tại **Rover Bay / Upgrade Bay** (`upgrade_bay.py`), làm thay đổi trực quan ngoại hình của 07 v3 và nâng cao ngưỡng chịu đựng:

| Mã Kit | Tên Gọi & Cột Mốc | Công Thức (`SalvageMaterial`) | Hiệu Ứng Lên Mô Hình Hao Mòn | Thay Đổi Trực Quan Trên 07 v3 (`RoverModelNodes`) |
| :--- | :--- | :--- | :--- | :--- |
| `kit.bumper_frame` | **Cản Trước Thép Nhíp (Bullbar Bumper)** (`0.5.7` cuối / `0.6` đầu) | `4 × Metal` + `2 × Alloy` + `1 × Wiring` | Nâng ngưỡng tốc độ va chạm cứng $v_{\text{min}}$ từ **`6.8 m/s` → `8.4 m/s`**. *(Ở tốc độ hành trình chuẩn `8.0 m/s` không Boost, 07 đâm trực diện vào đá tốn **`0` độ bền**!)* | Gắn khung cản ống thép đôi màu than và nhíp đồng ngang mũi xe phía trên `DigScoop` kèm hai đệm cao su giảm chấn. |
| `kit.suspension_dampers` | **Giảm Xóc Bình Khí Nitơ Rời (Nitrogen Dampers)** (`0.6`) | `2 × Alloy` + `2 × Coolant` + `2 × Wiring` | Cộng thêm **`+1.2 m/s`** vào $v_{\text{min}}$ (lên tới **`9.6 m/s`** khi đi cùng Cản Trước — miễn nhiễm cả khi bật Boost trên đường bằng!) và giảm **`-30%`** sát thương va chạm tốc độ cao. | Gắn thêm các bình dầu-khí phụ bằng đồng có cánh tản nhiệt bức xạ lên cả 4 giảm xóc (`Shock_FL..RR`). |
| `kit.hardened_hull` | **Giáp Vỏ Hợp Kim Ilmenite-Titan (Hardened Hull)** (`0.6+`) | `4 × Alloy` + `3 × Ilmenite` + `2 × OrangeBeads` | Tăng `MaxWearIntegrity` từ **`100` → `150`** (`+50%` tổng độ bền) và giảm **`-40%`** tốc độ ép của kẹp Reclaimer cũng như mưa thiên thạch vi mô. | Ốp các tấm giáp titan xước và xanh xô thơm (`R1_SagePatch` + `R1_Steel`) tán đinh đồng lên vòm bánh xe, vai hông và viền bảo vệ đầu CRT. |

---

## 3. Reclaimers: Những Cỗ Máy Thu Hồi Robot Cũ

### 3.1 Câu Chuyện & Sự Nhất Quán Với `story-v4.md`

Để khớp tuyệt đối với [`docs/design/story-v4.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/story-v4.md) (Quyết định 1–5) và [`docs/design/inventions.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/inventions.md#L211-L234) (§2.7 *Đường Phóng Mass Driver*):

- **Ai Đã Chế Tạo Chúng & Để Làm Gì (`1984–1987`):**
  Trong bốn năm nước rút của cuộc Khởi Hành (`1984–1988`), việc đóng các con tàu **Ark tại điểm Lagrange L4 và L5** lâm vào cơn khát vật liệu tinh luyện: các lò luyện quặng regolith thô không thể cung cấp kịp thép hợp kim titan, cuộn đồng nguyên chất và thấu kính chống bức xạ trước khi **Cơn Bão Proton Mặt Trời Cực Đoan năm 1988** ập tới.
  - **Bộ Chỉ Huy Vật Liệu Ark thuộc ILC** đã đặt hàng **Tycho Heavy Industries (Meridian Automated Systems)** — tập đoàn công nghiệp nặng đứng sau các giàn khai thác tự động và Đường Phóng Mass Driver L5 (`story-v4.md` §5, `0.6-world-regions.md` Vùng 6) — triển khai **Các Phân Đội Thu Hồi Tự Động (`ARD-01` đến `ARD-06`)**.
  - Chỉ thị của chúng (`DIRECTIVE SCRAP-04`) rất rõ ràng: tuần tra khắp các biển bazan và đường vận tải, quét mọi thiết bị bằng đầu đọc thẻ từ cảm ứng/quang học, và **thu hồi bất kỳ phương tiện, vệ tinh hay trạm khảo sát nào không phát Mã Tín Hiệu Ưu Tiên Ark 1988 hợp lệ**. Những chiếc xe tự hành khảo sát thập niên 1970 đã ngừng hoạt động, tầng hạ cánh cũ và cáp đồng bỏ hoang đều bị kéo về **Bãi Thu Hồi (Reclamation Yard)**, phân loại vào các thùng hợp kim/quang học, ép thành thỏi tiêu chuẩn và bắn lên **Đường Phóng Mass Driver L5** để ghép vỏ tàu Ark.
- **Vì Sao Chúng Coi 07 Là Phế Liệu Vào Năm `2024` (Sau 36 Năm):**
  Khi bão mặt trời năm 1988 đánh sập liên lạc và những phi hành đoàn cuối cùng rời Mặt Trăng lên tàu Ark, không ai kịp gửi lệnh `STOP-HARVEST`. Nhờ bộ nhớ lõi từ bọc gang dày chống bức xạ, các cỗ máy Reclaimer vẫn bền bỉ chạy đúng vòng lặp suốt **36 năm**.
  - Vì `07` là robot bảo trì hạng nhẹ của **Lumen (ILC)** được đánh thức vào năm `2024` bởi đoạn mã tháp canh vá tạm của Kenji mà không có Thẻ Từ Ưu Tiên Ark `1988`, bất kỳ chiếc Reclaimer nào chiếu đèn vào `07` đều đọc được:  
    `UNIT: LUMEN-07 // TRANSPONDER: EXPIRED (1988) // CLASSIFICATION: GRADE-A RECLAIMABLE ALLOY & OPTICS`.
  - Chúng không độc ác, không giận dữ, không phải quái vật. Chúng chỉ là những chiếc xe thu gom tái chế cần mẫn của thập niên 1980 đang làm nốt công việc cho một cuộc di tản đã kết thúc từ 36 năm trước.
- **Bãi Thu Hồi & Đường Phóng Mass Driver Nằm Ở Đâu:**
  - **Thung Lũng Nhà (`0.5` — Crater Keep) hoàn toàn KHÔNG có Reclaimer** (`VISION.md` Phán quyết 17): vành đai núi cao quanh Crater Keep ngăn toàn bộ xe bánh xích hạng nặng đi vào vùng khởi đầu.
  - **Bãi Thu Hồi Tiền Tiêu (`Vùng 2: The Long Mare` rìa phía đông, `0.6`):** Một lòng chảo bazan bậc thang nơi phân đội `ARD-04` vận hành bãi phân loại và tuyến đường sắt nhánh dẫn về **Tổ Hợp Đường Phóng Mass Driver L5 Chính (`Vùng 6: The Industrial Dark`)**.

---

### 3.2 Hành Vi: Căng Thẳng Tĩnh Lặng, Gọng Kẹp Từ Tính & Bắt Giữ Về Bãi

Reclaimer không bao giờ bắn đạn, không rượt đuổi điên cuồng và không biến game thành hành động bắn súng. Sự đe dọa của chúng là **căng thẳng không gian cơ học chậm rãi** — giống như luồng đèn hải đăng và chiếc xe kéo hạng nặng trong đêm:

![Luồng đèn kiểm tra của Reclaimer quét qua biển tối khi 07 ẩn mình trong bóng miệng hố](../../Logs/concepts/reclaimer_searchlight_mare.png)

1. **Tuần Tra (`Patrol` — `2.2–3.2 m/s`):**
   - Di chuyển chậm theo các tuyến đường đất nén cố định hoặc quanh các bãi xác vệ tinh lớn.
   - Quét **luồng đèn kiểm tra hình nón màu vàng cam sắc cạnh** (`28 m` với Spotter, `38 m` với Hauler) qua lại theo chu kỳ con lắc `6.0 s`, đi kèm tiếng lẫy cơ khí tích tắc đều đặn (`72 BPM`).
2. **Phát Hiện & Kiểm Tra Thẻ Từ (`Investigate` — Cửa Sổ Xác Minh `2.5 s`):**
   - Nếu 07 đi vào nón sáng — hoặc bật đèn pha (`L`) sáng trưng trong tầm nhìn `35 m` — Reclaimer dừng quét, khóa luồng sáng vào 07 và tăng nhịp lẫy lên gấp đôi (`144 BPM`) trong `2.5 s` để đọc thẻ từ.
   - **Khắc Chế Tức Thì:** Nếu 07 tắt đèn pha (`L`), lách vào vùng bóng tối sắc lẹm sau tảng đá hoặc gờ miệng hố và đứng yên trước khi hết `2.5 s`, cảm biến quang/dòng xoáy của Reclaimer mất dấu, phát tiếng ngân trầm `B2 → D2` rồi quay lại tuần tra.
3. **Tiếp Cận & Kẹp Nam Châm Điện (`Approach & Clamp` — `4.5–6.0 m/s`):**
   - Khi xác nhận `UNTAGGED SCRAP`, Reclaimer cấp điện cho **gọng kẹp nam châm điện quấn đồng** phía trước (phát ánh cam ấm và hút bụi sắt bay lơ lửng) rồi tiến về phía 07.
   - Vì tốc độ hành trình của 07 là `8.0 m/s` (`9.6 m/s` khi Boost), **một chú 07 lành lặn luôn chạy nhanh hơn chiếc Heavy Hauler (`4.5 m/s`)** và có thể dễ dàng phóng vọt qua miệng hố để thoát — trừ khi bị dồn vào góc hẹp hoặc bị chiếc **Nimble Spotter (`6.5 m/s`)** phóng dây tời từ tính giữ chân trước.
4. **Bị Giữ Trong Kẹp — Kéo Lê, Rút Độ Bền & Giãy Thoát:**
   - Khi bị kẹp (`< 3.5 m`), Reclaimer kéo lùi 07 về phía **Bãi Thu Hồi** ở tốc độ `2.5 m/s` trong khi lực ép từ trường rút **`7.0% Wear / s`** (`[PROPOSAL]` dải `6%–9%/s`).
   - **Cách 07 Tự Giãy Thoát (Tương Tác Vật Lý):**
     Một thanh **Độ Trượt Từ Trường (Magnetic Slip)** hiện lên cạnh 07:
     - **Cách A — Xung Ngược Cuộn Nhảy (`Space`):** Xả tụ Leap Coils ngay trong gọng kẹp tạo xung từ ngược chiều, lấp ngay **`+60%` thanh thoát** và đẩy bật hàm kẹp!
     - **Cách B — Kích Cuộn Tăng Tốc (`Shift`):** Đốt Boost Coils cộng **`+45%/s`** tiến độ trượt.
     - **Cách C — Lắc Mô-Men Bánh Xe (`A / D` luân phiên):** Đánh lái trái/phải liên tục bằng 4 bánh xe mô-men xoắn cao làm lệch khe từ (`+25%` mỗi nhịp lắc; lắc 4 nhịp là bung kẹp ngay cả khi chưa nâng cấp Leap hay Boost!).
     - Khi đầy `100%` (thường chỉ mất `1.5–2.5 s` và tốn `10%–18%` độ bền), khóa từ bung ra với tiếng *KENG* đồng-sắt vang dội, đẩy 07 văng lùi `4 m` và cho **`4.0 s` miễn nhiễm bão hòa từ** để 07 phóng thoát vào bóng tối.

#### Điều Gì Xảy Ra Nếu Độ Bền Về `0%` Khi Đang Bị Kẹp? (`[DECIDED]` Khuyến Nghị Trường Hợp Đặc Biệt)
`Logs/TASK.md` yêu cầu quyết định giữa quy tắc chết tại chỗ mặc định hay **trường hợp đặc biệt: thân vỏ 07 bị đưa về Bãi Thu Hồi và hàng hóa rơi vào thùng phân loại của chúng**.

> [!IMPORTANT]
> **Quyết Định Thiết Kế (`[PROPOSAL]` — Chọn Trường Hợp Đặc Biệt: "Phân Loại Tại Bãi Thu Hồi"):**  
> Nếu độ bền của 07 chạm `0%` khi đang nằm trong gọng kẹp của Reclaimer, **hãy áp dụng Trường Hợp Đặc Biệt đưa về Bãi Thu Hồi!**
> 1. **Lý Do:** Toàn bộ mục đích tồn tại của Reclaimer là kéo robot không thẻ về Bãi Thu Hồi để phân loại. Nếu nó kẹp 07 đến tắt nguồn rồi lại vứt hàng ngay giữa đường tuần tra thì vừa phi lý về mặt câu chuyện, vừa khiến người chơi lúc quay lại nhặt hàng bị chính nó đứng đè lên Beacon!
> 2. **Diễn Biến Cụ Thể:**
>    - Trong Khung 1–3 của Khoảnh khắc tắt nguồn, màn hình CRT của 07 thu thành một đường chỉ ngang ngay trong hàm kẹp khi chiếc Hauler quay đầu chở 07 về phía ánh đèn vàng rực của **Bãi Thu Hồi** nơi chân trời.
>    - Sau `1.2 s` chuyển cảnh đen, **Giao Thức Sao Lưu của Kenji** khởi động một khung gầm `Lumen-07` dự phòng cho người chơi tại **Service Gantry / Rover Bay** gần nhất (xem §1.6.B).
>    - Trong khi đó, chiếc Hauler hoàn tất chuyến chở: **100% hàng trong khoang của 07 (nằm trong Cargo Beacon Pod) + chiếc vỏ `07` cũ bị móp** được đổ gọn gàng vào **`SORTING BIN 03: UNTAGGED UNITS` (Thùng Phân Loại Số 03)** ở rìa ngoài **Bãi Thu Hồi**!
> 3. **Giá Trị Gameplay Tuyệt Vời:**
>    - Bị bắt một lần sẽ **đánh dấu chính xác vị trí Bãi Thu Hồi lên la bàn** thông qua tín hiệu của Cargo Beacon.
>    - Thùng Phân Loại Số 03 nằm ở khu vực rìa có thể lẻn vào bằng bóng tối — và ngay cạnh Cargo Beacon của bạn trong thùng là **những đống phế liệu T2 quý giá (`Alloy`, `CircuitChips`, `Optics`)** mà Reclaimer gom từ các xác tàu khác, cộng thêm chính chiếc vỏ `07` cũ của bạn (tháo dỡ được `2 Metal + 1 Wiring`)!
>    - Thay vì gây ức chế, việc bị "tái chế" biến thành một phi vụ đột nhập bãi phế liệu đầy kịch tính và đáng nhớ như trong *WALL-E*.

![Bãi Thu Hồi (Reclamation Yard) lúc chập tối trên Mặt Trăng](../../Logs/concepts/reclamation_yard_dusk.png)

---

### 3.3 Vòng Cung Tiến Triển: Khắc Chế → Thấu Hiểu → Đồng Minh

Mọi mối nguy trong *Lunarion* đều đi theo vòng cung làm chủ không bạo lực gồm 4 bước:

| Bước | Hành Động Của Người Chơi | Phần Thưởng Cơ Chế & Câu Chuyện |
| :--- | :--- | :--- |
| **1. Khắc Chế (Ẩn Nấp & Kỷ Luật Ánh Sáng)** | - Tắt đèn pha (`L`) khi thấy ánh đèn cam quét nơi chân trời.<br>- Tận dụng gờ miệng hố, tảng đá bazan và bóng tối sắc nét để tránh nón sáng.<br>- Nếu lỡ bị rìa đèn quét trúng trong bóng râm, **đứng im hoàn toàn** (cảm biến dòng xoáy bỏ qua vật thể tĩnh).<br>- Nếu bị kẹp, dùng **xung nhảy `Space` + lắc `A/D`** để bung khóa từ. | Tạo chiều sâu lén lút và hồi hộp ngay từ đầu trên vùng Long Mare mà chưa cần trang bị mới. |
| **2. Thấu Hiểu (Quét Một Cỗ Máy Hỏng/Ngủ Đông)** | - Ở rìa tây Long Mare (hoặc gần lối vào Bãi), 07 tìm thấy một chiếc **`SHEAR-85` Spotter bị đá lở đè hỏng** với chiếc đèn kiểm tra vẫn tích tắc yếu ớt (`D4`).<br>- Quét (`E`) **Bộ Đọc Thẻ Từ Lõi Ferit** bị lộ ra sẽ mở khóa mục từ điển *"Chỉ Thị Ark 1985 SCRAP-04"* và bản vẽ **Thẻ Từ Thông Hành Ark**. | Giải thích trọn vẹn câu chuyện bi tráng về việc xây tàu Ark L5 và lý do vì sao Reclaimer lại bắt 07. |
| **3. Miễn Nhiễm (Chế Tạo Thẻ Từ Ark)** | - Chế tạo **Thẻ Từ Thông Hành Ark (`kit.ark_transponder`)** tại Fabricator:<br>  **Công thức:** `1 × CircuitChips` + `2 × Wiring` + `1 × Optics` (`T2`, `0.6`).<br>- Gắn một hộp ăng-ten lưỡng cực bằng đồng và xanh xô thơm lên cột `Antenna` của 07 với đèn LED xanh-vàng. | Khi luồng sáng Reclaimer quét qua 07, đầu đọc của nó ngân lên hai nốt hòa âm ấm áp (`D4 → A4`), nháy đèn xác nhận (`TAG VALID: ARK MAINT. 07`) và **bỏ qua 07 hoàn toàn** — cho phép 07 đường hoàng đi dạo giữa Bãi Thu Hồi! |
| **4. Đồng Minh & Tự Động Hóa (Lập Trình Lại Reclaimer)** | - Ở giữa/cuối `0.6`, chế tạo **Băng Từ Ghi Đè Tọa Độ Nhà (`item.reclaimer_override`)**:<br>  **Công thức:** `2 × CircuitChips` + `1 × Crystal` + `2 × Wiring`.<br>- Lái tới sau lưng một chiếc Heavy Hauler (khi đã đeo Thẻ Từ), mở nắp bảo trì băng từ thập niên 1980 bằng tia hàn/giao tiếp của 07 và cắm băng ghi đè vào. | Đổi tọa độ trả hàng của chiếc Hauler từ `YARD BIN 01` sang **Bãi Chứa Hàng Căn Cứ của 07 (`M3-25`)**. Chiếc Hauler đồng minh (nay được sơn lại một vệt **kem-cam Lumen** ấm áp) sẽ tự động tuần tra ngoài biển bazan và chở **`4–6` đơn vị `Metal`, `Wiring`, `Ilmenite` hoặc `Alloy`** về tận căn cứ cho 07 mỗi ngày Mặt Trăng! |

---

### 3.4 Ba Loại Máy Reclaimer & Kiểm Định Luật Hình Khối Chân Không (`vacuum-shapes.md`)

![Bảng thiết kế 3 loại máy Reclaimer và tỷ lệ so với 07 v3](../../Logs/concepts/reclaimer_types_sheet.png)

Cả ba loại máy Reclaimer đều mang ngôn ngữ công nghiệp nặng `1984–1987` của **Tycho / Bộ Chỉ Huy Vật Liệu Ark**: vỏ thép đúc sơn vàng mù tạt phai màu (`#C88A32`) phối sắt xám than (`#26292E`), sọc cảnh báo vàng-đen, cuộn nam châm đồng đỏ trần (`#B86B35`) và đèn pha cửa chớp cơ khí.

#### Loại A: `SHEAR-85` Nimble Spotter ("Chó Săn Thẻ Từ / Tag-Hound")
- **Vai trò:** Trinh sát cơ động tuần tra các sống đồi, soi đèn phát hiện phế liệu chưa gắn thẻ và phóng dây tời từ tính giữ chân mục tiêu cho Hauler.
- **Kích thước & Di chuyển:** Dài `2.2 m` × rộng `1.6 m` × cao `1.8 m` (nhỉnh hơn `07 v3` một chút). Tốc độ tuần tra `3.2 m/s` / đuổi bắt `6.5 m/s` trên **4 chân nhện thủy lực gắn bánh lăn thép có gờ** (vừa lăn nhanh trên đá phẳng vừa bước qua gờ miệng hố và xoay `360°` tại chỗ).
- **Công cụ & Hình khối:** Tháp xoay trên lưng mang **đèn pha kiểm tra cửa chớp màu cam-đỏ**, chảo ăng-ten đọc thẻ từ S-band và **tời cáp nam châm điện** gắn trên cần thép vươn phía trước (`8 m`).

#### Loại B: `BALER-IV` Heavy Clamp-Hauler ("Xe Ủi Kẹp Phế Liệu")
- **Vai trò:** Xe ủi thu hồi hạng nặng chậm nhưng không gì cản nổi, chuyên kẹp và kéo xác xe tự hành hoặc khung vệ tinh về Bãi Thu Hồi.
- **Kích thước & Di chuyển:** Dài `4.8 m` × rộng `3.2 m` × cao `3.0 m` (nặng gấp `~3×` 07). Bò chậm `2.2 m/s` khi tuần tra / `4.5 m/s` khi tiếp cận trên **4 cụm bánh xích thép độc lập** để lại vết xích đôi sâu trên bụi Mặt Trăng.
- **Công cụ & Hình khối:** Phía trước là **hai gọng kẹp nam châm điện hình móng ngựa quấn đồng đỏ** khổng lồ trên cần thủy lực cùng đèn pha rộng; phía sau là **thùng ben chở phế liệu** chất đầy khung giàn vệ tinh và vỏ robot cũ; đuôi xe có nắp cắm băng từ để 07 lập trình lại ở Bước 4.

#### Loại C: `GOLIATH-L5` Yard Gantry-Crane & Jaw-Crusher (Công Trình Điểm Nhấn Chân Trời)
- **Vai trò:** Cần cẩu giàn và hàm nghiền từ tính khổng lồ chạy trên đường ray tại trung tâm **Bãi Thu Hồi**, nhìn thấy từ khoảng cách hàng cây số trên biển Long Mare.
- **Kích thước & Di chuyển:** Cao `14.0 m` × nhịp ngang `18.0 m`, trượt dọc **đường ray sắt khổ rộng** (`1.0 m/s`) bắc ngang qua ba **Thùng Phân Loại (`BIN 01: HULL ALLOY`, `BIN 02: OPTICS & CHIPS`, `BIN 03: UNTAGGED UNITS`)**.
- **Công cụ & Hình khối:** Treo một **mâm nam châm điện nâng hàng đường kính `3.0 m`** bằng cáp thép bện và hàm kẹp thủy lực, phía trên gắn đèn hiệu cảnh báo màu hổ phách quét ngang bầu trời hoàng hôn như ngọn hải đăng dẫn đường.

#### Kiểm Định Bắt Buộc 6 Câu Hỏi Hình Khối Chân Không ([`docs/design/vacuum-shapes.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/vacuum-shapes.md))

| Cỗ Máy | 1. Đứng/nổi bằng gì? | 2. Đổi hướng bằng gì? | 3. Tản nhiệt bằng gì? | 4. Vì sao có hình dáng này? | 5. Có chi tiết khí quyển bị cấm không? | 6. Đặt cạnh *2001* / Apollo có hợp không? |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| **`SHEAR-85` Spotter** | 4 chân thủy lực gắn bánh thép trên nền regolith. | Vi sai mô-men bánh xe + vành răng tháp xoay. | Các lá đồng bức xạ nhiệt trên lưng (không quạt gió). | Khung 4 chân trọng tâm thấp để bước qua đá tảng miệng hố. | **KHÔNG** (0 cánh quạt, 0 cánh bay, 0 vây gió). | **CÓ** (Lunokhod + Apollo LRV + máy xúc 4 chân). |
| **`BALER-IV` Hauler** | 4 cụm bánh xích thép chịu tải dưới trọng lực `1/6 g`. | Lái trượt vi sai hai bên xích (skid-steer). | Tấm tản nhiệt bức xạ sơn đen mờ phía đuôi (`R1_Charcoal`). | Xe ủi + thùng ben + hai gọng nam châm điện móng ngựa. | **KHÔNG** (0 ống khói, 0 quạt làm mát, 0 vỏ khí động học). | **CÓ** (Máy ủi mỏ lộ thiên thập niên 1980). |
| **`GOLIATH-L5` Yard Crane** | Bánh sắt có gờ chạy trên tà vẹt đường ray đá bazan. | Chạy dọc ray + tời cáp ngang trên dầm. | Khung giàn thép tự bức xạ nhiệt vào bầu trời đen `3 K`. | Cẩu trục bắc qua 3 thùng phân loại tại đầu nạp Mass Driver. | **KHÔNG** (0 chong chóng gió, 0 tháp giải nhiệt). | **CÓ** (Cẩu trục cảng/mỏ công nghiệp nặng). |

---

### 3.5 Ngôn Ngữ Âm Thanh (Ngũ Cung Rê Trưởng: `D E F# A B`)

Tuân thủ tuyệt đối định hướng âm nhạc của *Lunarion*, Reclaimers không bao giờ phát tiếng còi báo động chói tai hay âm thanh kinh dị nghịch tai. Toàn bộ âm thanh của chúng đều trầm hùng, cơ khí và nằm gọn trong **âm giai Ngũ cung Rê trưởng (`D E F# A B`)**:
1. **Tiếng Động Cơ Trầm (`D1 + A1` Quãng Năm Đúng):** Tiếng ngân sub-bass ấm (`36.7 Hz` `D1` + `55.0 Hz` `A1`) của động cơ điện một chiều cổ góp và mắt xích thép truyền qua nền đá bazan khi bánh xe 07 chạm cùng mặt đất.
2. **Tiếng Lẫy Quét Đèn Kiểm Tra (`F#4` Metronome):** Khi đèn cửa chớp quét qua lại, lẫy cơ khí gõ nhịp ở nốt **`F#4` (`370 Hz`)** tốc độ `72 BPM`; khi phát hiện 07 (`Investigate`), nhịp tăng gấp đôi lên `144 BPM` luân phiên hai nốt **`F#4 – A4`**.
3. **Tiếng Sập Kẹp Từ & Ngân Dòng Xoáy (`D2` + `B2 → E3`):** Tiếng biến áp ngân từ `B2` lên `E3` rồi kết thúc bằng tiếng sập đồng-sắt trầm đục ở nốt **`D2`**.
4. **Tiếng Chuông Chào Thẻ Từ Hợp Lệ (`D4 → F#4 → A4`):** Khi 07 đã gắn **Thẻ Từ Ark**, mỗi lần đèn Reclaimer quét qua sẽ ngân lên ba nốt hợp âm Rê trưởng ấm áp (`D4 – F#4 – A4`) như một lời chào đồng nghiệp giữa biển đêm.

---

## 4. Nguy Hiểm Tự Nhiên Theo Vùng Miền (`0.6+`, 1–2 Dòng Mỗi Loại)

Mỗi vùng thế giới ở `0.6+` ([`docs/design/0.6-world-regions.md`](file:///D:/Project/MoonProject-wt/agy-wear/docs/design/0.6-world-regions.md)) có một **hiện tượng vật lý Mặt Trăng đặc trưng** gây hao mòn độ bền nếu đi vào mà không chuẩn bị — và mỗi hiện tượng đều có một **thiết bị bảo vệ chế tạo được** đóng vai trò là **chìa khóa mở vùng (region gate)** tự nhiên:

| Vùng (`0.6-world-regions.md`) | Hiện Tượng Mặt Trăng & Tốc Độ Hao Mòn (1–2 Dòng) | Thiết Bị Bảo Vệ Chế Tạo & Chìa Khóa Mở Vùng (`SalvageMaterial`) |
| :--- | :--- | :--- |
| **1. Biển Dài (`Vùng 2: The Long Mare`) — Mưa Thiên Thạch Vi Mô** | Các đợt mưa hạt bụi siêu vận tốc lao xuống từ bầu trời đen không khí quyển, bắn tia lửa nhỏ lấpánh trên mặt đồng bằng trống trải (`-4% Wear/hạt` trừ khi 07 trú dưới mái Gantry, gầm vách đá hoặc vòm xác tàu). | **Mái Chắn Kép Whipple (`kit.whipple_shield`):** Chế từ `3 × Alloy + 2 × Metal`. Gắn tấm chắn hai lớp đồng-xanh xô thơm phía trên đầu và vai 07, giảm sát thương thiên thạch vi mô về **`0%`**. |
| **2. Vành Đai Biển & Đường Ranh Giới Ngày-Đêm (Terminator) — Màn Bụi Tĩnh Điện Lơ Lửng** | Dọc ranh giới sáng-tối, tia UV mặt trời tích điện cho hạt bụi silicat sắc nhọn bay lơ lửng thành màn sương cao `1.5 m` bám vào khớp trục, mài mòn ổ trục bánh xe và làm mờ kính CRT (`-2.5% Wear/s` khi phóng nhanh qua màn bụi dày). | **Chổi Tiếp Địa Đồng & Gioăng Chắn Bụi (`kit.grounding_whiskers`):** Chế từ `3 × Wiring + 2 × OrangeBeads`. Chùm dây đồng bện thả chạm đất sau cản xe giúp xả sạch tĩnh điện và triệt tiêu mài mòn bụi. |
| **3. Cánh Đồng Thủy Tinh (`Vùng 3: The Glass Field`) — Mảnh Thủy Tinh Núi Lửa & Sốc Nhiệt Khe Nứt** | Những cánh đồng hạt thủy tinh cam (`OrangeBeads`) và dải đá nóng chảy sắc như dao cứa rách lốp cao su thường khi chạy trên `5.0 m/s` (`-3% Wear/s`), kèm các khe thoát khí gây sốc nhiệt. | **Lốp Lưới Thép-Ilmenite Thiêu Kết (`kit.sintered_treads`):** Chế từ `4 × Ilmenite + 2 × Glass + 2 × Metal`. Thay lốp cao su bằng lốp lưới hợp kim đan (như xe Apollo LRV), miễn nhiễm hoàn toàn mảnh thủy tinh sắc. |
| **4. Cực Nam (`Vùng 5: The South Pole` — Đáy Hố Shackleton) — Lạnh Cực Sâu Vĩnh Cửu (`40 K`)** | Trong các đáy hố tối vĩnh cửu không thấy ánh mặt trời suốt 2 tỷ năm, nhiệt độ `40 K` làm thép vỏ giòn gấp đôi (`2.0×` sát thương va chạm) và làm đông cứng phớt cơ khí (`-2.0% Wear/s` sau `20 s` ở trong bóng tối vĩnh cửu). | **Áo Tuần Hoàn Nhiệt Khép Kín (`kit.cryo_heater`):** Chế từ `3 × Coolant + 2 × OrangeBeads + 2 × Alloy`. Dẫn chất lỏng ấm từ cụm pin qua ống đồng quanh thân `Body`, giữ 07 ấm áp và miễn nhiễm lạnh sâu. |
| **5. Khu Công Nghiệp Tối (`Vùng 6: The Industrial Dark`) — Rò Rỉ Nhiên Liệu Đóng Băng & Hồ Quang Cảm Ứng** | Đường ống nhiên liệu vỡ và tụ điện Đường Phóng Mass Driver rò rỉ muối hydrazine ăn mòn cùng hồ quang điện áp cao trên sàn nhà máy (`-5.0% Wear/s` khi đi qua vùng rò rỉ). | **Lớp Phủ Gầm Cách Điện Fluoropolymer (`kit.chemical_seal`):** Chế từ `2 × Superconductor + 2 × Coolant + 2 × Glass`. Bọc kín gầm xe và trục bánh, chống ăn mòn hóa học và phóng điện đường ray. |

---

## 5. Phân Kỳ Triển Khai (Phasing), Miền Code & Kiểm Thử Tự Động

### 5.1 Phân Kỳ Theo Cột Mốc (`0.5.7` vs. `0.6+`)

- **Cột mốc `0.5.7` (Phát hành cùng Xây Dựng Căn Cứ — Base Building):**
  1. Mô hình Hao mòn (Wear): toán va chạm vận tốc pháp tuyến $v_\perp \ge 6.8\text{ m/s}$, phân loại bề mặt cứng vs. regolith, tiếng cót két cảnh báo (`6.5 m/s+`), 4 giai đoạn hiển thị/hành vi, và giới hạn tốc độ an toàn ở giai đoạn Nguy kịch (`6.0 m/s`).
  2. Luồng Tắt nguồn & Khởi động lại: hoạt cảnh CRT thu thành đường chỉ ngang (`R1CrtFace.shader`), nhả & thu hồi **Cargo Beacon Pod** (`M3-25`), tỉnh dậy tại Service Gantry / Rover Bay.
  3. Sửa chữa vật lý: **Bộ Vá Vỏ (`item.patch_kit`)** tự hàn trong `3.5 s` + công trình **Khung Bảo Dưỡng (`structure.service_gantry`)** dùng điện căn cứ (`PowerGrid.cs`).
  4. Chế độ chơi mới: **Survival** (mặc định) và **Calm** (không hao mòn, không chết).
- **Cột mốc `0.6+` (Phát hành cùng Các Vùng Thế Giới — World Regions):**
  1. Ba loại máy **Reclaimer** (`SHEAR-85` Spotter, `BALER-IV` Hauler, `GOLIATH-L5` Yard Crane) + **Bãi Thu Hồi (Reclamation Yard)** & Thùng Phân Loại Số 03.
  2. Vòng cung làm chủ: quét máy hỏng → chế **Thẻ Từ Ark (`kit.ark_transponder`)** → chế **Băng Ghi Đè (`item.reclaimer_override`)** biến Hauler thành đồng minh chở phế liệu về căn cứ.
  3. Năm **Nguy hiểm vùng miền** + các gói nâng cấp khung gầm & thiết bị bảo hộ mở vùng.

### 5.2 Các Miền Code Liên Quan & Bộ Kiểm Thử (`EditMode` / `PlayMode`)

| Miền / Assembly | File Hiện Có Tham Chiếu | File / Kiểu Mới Hoặc Cập Nhật |
| :--- | :--- | :--- |
| **`Core` (`Project.Core`)** | [`SalvageMaterial.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Core/SalvageMaterial.cs) | `GameMode.cs` (`Survival`, `Calm`), `WearState.cs` (struct lưu trữ & hàm toán va chạm thuần C#). |
| **`Rover` (`Project.Rover`)** | [`RoverController.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/RoverController.cs), [`DriveSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/DriveSettings.cs), [`BoostSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/BoostSettings.cs), [`HoverJumpSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/HoverJumpSettings.cs), [`FaceDisplay.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/FaceDisplay.cs), [`RoverModelNodes.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/RoverModelNodes.cs) | `WearSettings.cs`, `RoverWearController.cs`, `PatchKitApplicator.cs` (điều khiển hoạt cảnh tự vá `3.5 s`). |
| **`Gameplay` (`Project.Gameplay`)** | [`StructureDefinition.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Gameplay/Building/StructureDefinition.cs), [`StructureIds.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs), [`PowerGrid.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Gameplay/Building/PowerGrid.cs) | `StructureIds.ServiceGantry`, `ServiceGantryController.cs`, `CargoBeaconPod.cs`, `RespawnCoordinator.cs`. |
| **`World` (`Project.World` — `0.6+`)** | `WorldRegions`, `SalvageNode` | `ReclaimerController.cs`, `ReclaimerSearchlight.cs`, `ReclamationYardBin.cs`, `RegionalHazardZone.cs`. |
| **`UI` (`Project.UI`)** | [`CellRowSettings.cs`](file:///D:/Project/MoonProject-wt/agy-wear/Assets/_Project/Scripts/Rover/CellRowSettings.cs) | `WearGaugeView.cs` (thanh 5 đốt cạnh thanh pin, tự ẩn khi `100%` hoặc ở chế độ `Calm`), `MagneticSlipPromptView.cs`. |

**Danh sách Kiểm thử Tự động bắt buộc (`EditMode` & `PlayMode`):**
- `RoverWearMathTests.cs` (EditMode): Kiểm tra $v_\perp < 6.8\text{ m/s} \to 0$ hao mòn; quệt góc hẹp $20^\circ$ ở tốc độ `8.0 m/s` $\to 0$ hao mòn; va chạm lên `Regolith/Terrain` ở mọi tốc độ $\to 0$ hao mòn; đường cong hàm mũ chính xác từ `6.8` đến `12.0 m/s` (trần `40%`); cửa sổ miễn nhiễm `1.0 s` sau va chạm.
- `RoverWearStageTests.cs` (EditMode): Kiểm tra chuyển giai đoạn tại `65%` và `25%`; kiểm tra giai đoạn Nguy kịch (`< 25%`) khóa tốc độ tối đa ở `6.0 m/s`, khóa Leap Coils và nhân `1.4×` thời gian quét.
- `RoverDeathAndBeaconPlayModeTests.cs` (PlayMode): Kiểm tra khi độ bền về `0%` sẽ kích hoạt thu hẹp màn hình CRT (`ScreenVerticalPinch = 1.0`), sinh `CargoBeaconPod` chứa đúng số hàng trong khoang, đưa 07 về `ServiceGantry` gần nhất với `100%` độ bền và giữ nguyên kit, sau đó nhặt lại Beacon trả đủ hàng về khoang.
- `CalmModeTests.cs` (EditMode & PlayMode): Kiểm tra trong `GameMode.Calm`, đâm vào đá ở `12.0 m/s` hoặc đứng trong vùng nguy hiểm luôn giữ `WearIntegrity == 100%` và ẩn thanh HUD.

---

## 6. Rủi Ro & Lời Giải

1. **Rủi ro gây ức chế và mất thời gian (Frustration & Lost Time):**
   - *Lời giải:* (a) Không bao giờ mất kit, công trình hay tiến trình cốt truyện; (b) 100% các cú nhảy và tiếp đất trên bụi regolith đều tốn `0` độ bền; (c) Giai đoạn Nguy kịch (`< 25%`) tự giới hạn tốc độ ở `6.0 m/s` (dưới ngưỡng sát thương `6.8 m/s`) nên 07 không thể chết oan vì va đá nhỏ trên đường bằng khi đang lê bước về nhà; (d) **Service Gantry** cực rẻ (`3 Metal + 2 Wiring + 1 Optics`), khuyến khích người chơi xây tại mọi trạm tiền tiêu để rút ngắn quãng đường hồi sinh xuống dưới `60–90 giây`.
2. **Rủi ro phá vỡ Trụ cột Cô Đơn (`Pillar 6 — "Quiet by Default, Alive Underneath"`):**
   - *Lời giải:* Thung lũng khởi đầu (`0.5`) tuyệt đối không có Reclaimer. Ở các vùng `0.6+`, Reclaimers không phải kẻ thù hiếu chiến mà là những cỗ máy công trường thập niên 1980 chậm rãi (`2.2–3.2 m/s`) đi theo lộ trình cố định với ánh đèn hải đăng vàng ấm và tiếng động cơ ngũ cung trầm tĩnh. Ngay khi chế được **Thẻ Từ Ark**, chúng trở thành những người hàng xóm hiền lành và thậm chí có thể được lập trình lại để chở phế liệu về xây căn cứ cho 07.
3. **Rủi ro làm mất lòng người chơi thích sự thư giãn thuần túy → Chế độ Calm:**
   - *Lời giải:* **Calm Mode** được đặt trang trọng ngay tại màn hình tạo game mới. Người chơi chọn Calm Mode vẫn được trải nghiệm trọn vẹn cốt truyện, quét tìm hiểu Reclaimer, dạo chơi trong Bãi Thu Hồi và lập trình đồng minh mà không bao giờ phải lo lắng về móp vỏ hay tắt nguồn.
4. **Bài học từ *Subnautica* và những điểm *Lunarion* cải tiến vượt trội hơn:**
   - *Giữ lại tinh hoa:* Cảm giác ấm áp khi đưa chiếc tàu trầy xước về trạm sửa chữa (*Moonpool* $\to$ **Service Gantry**) và sự hồi hộp không cần súng đạn trước những thực thể lớn có quy luật rõ ràng.
   - *Cải tiến vượt bậc:* (1) Không nhảy ra khỏi xe cầm súng bắn tia sửa chữa vô lý — 07 tự dùng nam châm gầm và đầu hàn ngực gắn tấm vá lên chính mình; (2) Không mất vĩnh viễn phương tiện hay module nâng cấp khi chết; (3) Biến mối nguy thành **đồng minh tự động hóa** ở cuối vòng cung thay vì để chúng mãi là kẻ quấy rối; (4) Nếu bị Reclaimer bắt tới cạn độ bền, hàng của bạn được xếp gọn vào **Thùng Phân Loại Số 03** tại Bãi Thu Hồi cạnh một kho báu phế liệu T2 đang chờ bạn tới lấy lại!
