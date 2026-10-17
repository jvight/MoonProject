# Hao Mòn (Wear), Sửa Chữa Cơ Lý & Máy Thu Hồi (Reclaimers) — Tài Liệu Thiết Kế

> **Trạng thái:** **CHỈ THIẾT KẾ (DESIGN ONLY)** — Việc lập trình triển khai chờ qua **Cổng lõi cảm giác** ([`docs/ROADMAP.md`](../ROADMAP.md), *"Cổng lõi cảm giác"*). Khung cốt lõi và các quyết định Vòng 3 đã được chốt (`[DECIDED]`) bởi Phán quyết của Chủ dự án (2026-10-17) & [`docs/VISION.md`](../VISION.md#L180-L198) Phán quyết 17; các công thức chi tiết và dải số tinh chỉnh dưới đây là `[PROPOSAL]` phục vụ giai đoạn triển khai.  
> **Bản tiếng Anh:** [`wear-and-reclaimers.md`](wear-and-reclaimers.md)  
> **Trang duyệt Concept Art:** [`Logs/wear/index.html`](../../Logs/wear/index.html) (`Logs/concepts/*.png`)

---

## Tóm Tắt Điều Hành & Khung Thiết Kế

> [!IMPORTANT]
> **Trạng Thái Chỉ Thiết Kế (Design-Only Gate):** Tài liệu này và bộ Concept Art đi kèm chốt thiết kế hoàn chỉnh cho hệ thống Hao mòn, Sửa chữa cơ lý và Reclaimers. **Chưa triển khai bất kỳ dòng code hay asset gameplay nào ở bước này** — việc lập trình chờ đến khi dự án vượt qua **Cổng lõi cảm giác** trong [`docs/ROADMAP.md`](../ROADMAP.md) (*"Cổng lõi cảm giác"*).

Vào ngày 2026-10-17, Chủ dự án đã chốt hai thay đổi nền tảng hiện đã được ghi vào [`docs/VISION.md`](../VISION.md#L180-L198) (**Phán quyết 17 — Hao mòn, Sửa chữa, Reclaimers và Cái chết**) cùng các quyết định duyệt Vòng 3 dưới đây:

1. **07 có thanh Độ Bền Thân Vỏ ("Wear / Hull Integrity") bên cạnh thanh Pin (Battery).** Phóng quá nhanh đâm vào vật cản cứng, đi vào hiện tượng khắc nghiệt vùng sâu mà thiếu lớp bảo vệ, hoặc bị kẹp bởi các cỗ máy **Reclaimer (Máy Thu Hồi)** sẽ làm giảm độ bền qua **4 cột mốc hỏng hóc cụm bộ phận rõ rệt (`75%`, `50%`, `25%`, `10%`)**. Khi độ bền về `0%`, **07 chết (tắt nguồn hoàn toàn)**.
2. **Mọi bước hư hại, sửa chữa và mối đe dọa đều tuân thủ tuyệt đối logic vật lý và chân không ([`vacuum-shapes.md`](vacuum-shapes.md)):**
   - **Pin vẫn là tầm hoạt động, không bao giờ gây chết** ([`docs/VISION.md` Phán quyết 15](../VISION.md#L163-L176)): cạn pin (`0%`) vẫn chỉ đưa 07 về chế độ bò chậm `25%` tốc độ bằng pin dự trữ (`M3-20`). Cái chết *chỉ* đến từ việc độ bền (Wear) chạm `0%`.
   - **Những cú nảy vẫn trọn vẹn niềm vui:** Nhảy thấp trọng lực, tiếp đất có đệm từ trên bụi Mặt Trăng (regolith) và các va quệt nhẹ **không bao giờ** tốn độ bền.
   - **07 không có bàn tay** ([`docs/VISION.md` Phán quyết 14](../VISION.md#L158-L162)): Sửa chữa ngoài thực địa (**Bộ Sửa Chữa Thực Địa — Thay Thế Module ORU**, `item.repair_kit`) và đại tu tại căn cứ (**Khung Bảo Dưỡng — Service Gantry**) đều là thao tác cơ-điện vật lý 100% sử dụng tay bảo trì nhỏ gắn sườn xe cùng chính đầu hàn hồ quang ở giữa ngực 07 (`CutterOrigin`), hoặc hai cánh tay búa/hàn của Khung Bảo Dưỡng.
   - **Ánh sáng và vật lý chân không ở mọi nơi:** Vì Mặt Trăng không có không khí để tán xạ ánh sáng hay giữ bụi lơ lửng, đèn pha và đèn tìm kiếm **chỉ hiện dưới dạng thấu kính nóng rực trên máy và vũng sáng sắc cạnh nơi ánh sáng đập xuống mặt đất/bề mặt** (tuyệt đối không có luồng hay nón sáng nhìn thấy trong không gian trống), bụi bắn theo đường parabol đạn đạo sạch sẽ rồi rơi xuống ngay, và tia lửa hàn bay theo vệt thẳng ngắn.
   - **Căng thẳng tĩnh lặng, không phải chiến đấu:** 07 không có vũ khí. **Reclaimers** — những đoàn máy thu hồi tự động thời kỳ Khởi Hành (`1984–1988`) vẫn miệt mài gom phế liệu nuôi Đường Phóng Mass Driver L5 để đóng tàu Ark ([`story-v4.md`](story-v4.md)) — là mối nguy công nghiệp chậm rãi, dễ đọc, dễ né và tuân theo vòng cung tiến triển: **Ẩn nấp → Thấu hiểu → Thẻ Từ Miễn Nhiễm → Lập Trình Thành Đồng Minh**.

### Các Quyết Định Duyệt Của Chủ Dự Án (2026-10-17, `[DECIDED]`)

| Hạng Mục Quyết Định | Phán Quyết Chốt Của Chủ Dự Án (`[DECIDED]`) | Mục Tài Liệu |
| :--- | :--- | :---: |
| **Hệ quả khi chết (Death consequence)** | **Mặc định (`[DECIDED]`):** Tilly kéo 07 về Khung Bảo Dưỡng / Trạm Xe gần nhất; giữ nguyên 100% thiết bị/kit và tiến trình; hàng rời trong khoang nằm lại trong **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** tại nơi tắt nguồn (hoặc trong `Thùng Phân Loại Số 03` tại Bãi Thu Hồi nếu tắt nguồn trong kẹp Reclaimer). | §1.6 |
| **Tên chế độ không chết (No-death mode name)** | **`"Chỉ khám phá"` / `"Explore only"` (`[DECIDED]`):** Đổi toàn bộ tên chế độ không hao mòn / không chết thành `"Chỉ khám phá"` (Tiếng Việt) / `"Explore only"` (Tiếng Anh). | §1.9 |
| **Chế độ Hardcore một mạng** | **Hoãn sang bản `1.0` dưới dạng tùy chọn bật thêm (`[DECIDED]`):** Không đưa vào các bản đầu; để dành làm chế độ tùy chọn (opt-in) ở `1.0`. | §1.9 |
| **Đội hình máy Reclaimer** | **Giữ cả ba loại (`[DECIDED]`):** `SHEAR-85` Nimble Spotter, `BALER-IV` Heavy Clamp-Hauler và `GOLIATH-L5` Yard Gantry-Crane & Jaw-Crusher. | §3.4 |
| **Cột mốc hao mòn (Wear milestones)** | **4 cột mốc hỏng hóc cụm bộ phận leo thang (`75%`, `50%`, `25%`, `10%`, `[DECIDED]`):** Mỗi cột mốc làm hỏng một cụm bộ phận vật lý cụ thể trên 07 với biểu hiện trực quan rõ ràng trên model, thay đổi cảm giác lái thực tế, âm thanh Ngũ cung Rê trưởng, phản ứng mặt CRT và nhịp cảnh báo một lần khi chạm mốc. | §1.4 |
| **Bộ Sửa Chữa Thực Địa (Field Repair Kit)** | **Thay thế module ORU dùng 1 lần, không chỉ hàn tấm phẳng (`[DECIDED]`):** Dùng 1 lần (`1 bộ chế tạo = 1 lần sửa`, tiêu hao ngay); sửa cụm bộ phận hỏng nặng nhất hiện tại và hồi phục một cột mốc hao mòn; không có bộ sửa thì không tự sửa được ngoài thực địa (chỉ Khung Bảo Dưỡng mới đại tu toàn diện); chuỗi 4 bước vật lý tháo chốt module cháy $\to$ thả rơi xuống đất thành mảnh vỡ tồn tại trên nền bụi $\to$ lắp module ORU mới $\to$ hàn tấm ốp vỏ bảo vệ. | §2.1 |

---

## 1. Mô Hình Hao Mòn (The Wear Model)

### 1.1 Quy Tắc Va Chạm: Vận Tốc Pháp Tuyến Tương Đối, Ngưỡng & Đường Cong

Trong [`RoverController.cs`](../../Assets/_Project/Scripts/Rover/RoverController.cs#L38-L40), khối vật lý của 07 được điều khiển bởi `Rigidbody _body` và `SphereCollider _sphere`, với thông số di chuyển gốc nằm tại [`DriveSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/DriveSettings.cs#L18-L24), [`BoostSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/BoostSettings.cs#L15-L24) và [`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs#L20-L50):
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
| Chạm nhẹ / lùi xe / quệt sượt góc $< 30^\circ$ | `< 6.8 m/s` | **`0%`** | Tiếng cộc kim loại nhẹ + hạt bụi rơi đạn đạo; `0` sát thương. |
| Chớm ngưỡng (phanh muộn chạm tảng đá) | `6.8 – 7.2 m/s` | **`4% – 6%`** | Tiếng chíp nhăn mặt ngắn, vệt tia lửa thẳng ngắn, thanh độ bền HUD hiện lên. |
| Đâm trực diện ở tốc độ hành trình (`100%` cruise) | `8.0 m/s` | **`12%`** | Tiếng uỳnh rõ, mặt CRT nhăn (`> <`), cần ~8 cú đâm trực diện mới hỏng hoàn toàn từ `100%`. |
| Đâm trực diện khi bật Boost Coil (`8.0 + 1.6 m/s`) | `9.6 m/s` | **`24%`** | Tiếng vỏ thép móp mạnh, tia lửa thẳng ngắn, 1 cú là chạm ngay cột mốc `75%` (cong ăng-ten). |
| Lao dốc đứng + Boost đâm trực diện | `11.5 – 12.0+ m/s` | **`38% – 40%` (trần)** | Chấn động cấu trúc lớn; nhưng không bao giờ "one-shot" khi đầy máu (tối thiểu 3 cú đâm tệ nhất mới về `0%`). |

- **Thời Gian Miễn Nhiễm Sau Va Chạm (Grace Cooldown):** **`1.0 s`** (`[PROPOSAL]` dải `0.8–1.2 s`). Khi nảy giữa hai tảng đá sát nhau trong vòng `1.0 s`, hệ thống chỉ tính duy nhất cú va chạm có $v_\perp$ cao nhất, triệt tiêu hoàn toàn hiện tượng kẹt vật lý chết oan.

---

### 1.2 Bề Mặt Nào Là Cứng (Hard) vs. An Toàn (Safe)

Để giữ trọn Trụ cột 2 (*"Weightless calm"* — nhảy thấp trọng lực và lăn qua đồi bụi luôn là phần thưởng), các collider được chia làm hai nhóm vật liệu rõ rệt:

| Nhóm Bề Mặt | Ví Dụ Trong Thế Giới | Có Gây Hao Mòn? | Lý Giải Vật Lý & Gameplay |
| :--- | :--- | :---: | :--- |
| **AN TOÀN (Bụi Regolith / Địa hình mềm)** | Bụi biển bazan (mare), sườn đồi bụi cao nguyên, bờ miệng hố phủ bụi, đường đất nén (`Terrain` layer). | **KHÔNG BAO GIỜ (`0%`)** | Lớp bụi regolith dày hấp thụ xung lực bánh xe và gầm. Kết hợp với `HoverJumpSettings._cushionLandingSpeed = 1.6 m/s`, **mọi cú tiếp đất xuống địa hình bụi đều tốn `0` độ bền bất kể độ cao**. |
| **CỨNG (Đá Tảng, Kim Loại & Công Trình)** | Tảng đá bazan rời, mỏm đá sắc nhọn quanh miệng hố, khung vệ tinh rơi, xác xe tự hành cũ, công trình căn cứ (`StructureDefinition`), đường ray Mass Driver, thân vỏ Reclaimer. | **CÓ (nếu $v_\perp \ge 6.8\text{ m/s}$)** | Đá bazan đặc và thép cán nguội không đàn hồi sẽ làm biến dạng lớp vỏ kim loại của 07 khi đâm trực diện ở tốc độ cao. |

---

### 1.3 Tiếng Kêu Cót Két Cảnh Báo (Đọc Được Trước Khi Đau)

Hư hại không bao giờ ập đến bất ngờ. Ngay trước khi 07 chạm ngưỡng tốc độ có thể gây móp vỏ khi đâm, khung gầm của 07 sẽ phát tín hiệu cảnh báo vật lý:
- **Ngưỡng Kích Hoạt:** Bắt đầu êm ở **`6.5 m/s`** (`~81%` tốc độ hành trình, sớm hơn ngưỡng sát thương `6.8 m/s`) và tăng dần độ dày/cao độ khi qua `8.0 m/s` (hành trình), `9.6 m/s` (Boost) tới `12.0 m/s` (lao dốc).
- **Âm Thanh:** Tiếng khung thép thập niên 1980 rùng nhẹ — lò xo giảm xóc (`Shock_FL..RR`) cót két, cao su đệm rên nhẹ và âm cộng hưởng khung gầm theo âm giai Ngũ cung Rê trưởng (`A3–E4` truyền qua càng bánh xe `Arm_FL..RR`).
- **Tín Hiệu Thị Giác Nhỏ:** Trên `7.2 m/s`, cần ăng-ten đuôi (`Antenna` / `AntennaTip` trong [`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs#L54-L57)) uốn cong về sau và dòng quét màn hình CRT rung nhẹ `1 px`.

---

### 1.4 Các Cột Mốc Hao Mòn: 4 Bước Hỏng Hóc Cụm Bộ Phận Leo Thang (`[DECIDED]`)

Độ bền được lưu bằng biến chuẩn hóa `WearIntegrity` $\in [0.0, 1.0]$ (`100%` = nguyên vẹn, `0%` = tắt nguồn). Thay vì những vết xước chung chung trên vỏ, việc vượt qua từng **cột mốc hao mòn trong 4 mốc (`75%`, `50%`, `25%`, `10%`)** sẽ gây ra một **hỏng hóc cụm bộ phận cơ-điện cụ thể, nhìn thấy ngay lập tức** trên khung gầm của 07 v3 ([`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs) & [`build_rover.py`](../../tools/art/blender/rover/build_rover.py)). Mỗi cột mốc đều tích lũy (các hỏng hóc trước vẫn giữ nguyên cho tới khi được sửa bằng Bộ Sửa Chữa Thực Địa hoặc tại Khung Bảo Dưỡng) và người chơi có thể cảm nhận trực tiếp qua tay lái, âm thanh cùng khuôn mặt CRT của 07 ([`FaceDisplay.cs`](../../Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs) / [`R1CrtFace.shader`](../../Assets/_Project/Shaders/R1/R1CrtFace.shader)):

![07 v3 qua 4 cột mốc hỏng hóc cụm bộ phận (75%, 50%, 25%, 10%) và 3 khung hình khoảnh khắc tắt nguồn](../../Logs/concepts/wear_stages_and_death.png)

#### Bảng Tổng Quan: Từ Nguyên Vẹn (`100%`) Qua 4 Cột Mốc Hỏng Hóc Tới Tắt Nguồn (`0%`)

| Mốc Độ Bền | Cụm Bộ Phận Bị Hỏng | Biểu Hiện Trên Thân 07 v3 (`RoverModelNodes.cs`) | Ảnh Hưởng Điều Khiển & Cảm Giác Lái | Âm Thanh (Ngũ Cung Rê Trưởng) | Phản Ứng Mặt CRT (`FaceDisplay.cs`) | Nhịp Cảnh Báo Một Lần (Khi Vừa Chạm Mốc) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`100% – 76%`<br>Hoạt Động Chuẩn / Trầy Nhẹ** | **Không có** (chỉ xước sơn nhẹ trên lưỡi cản `DigScoop` khi `< 100%`). | Thân vỏ trắng bo tròn (`Body`), 3 đèn tròn phía trước + thấu kính đồng giữa ngực (`CutterOrigin`), cần `Antenna` thẳng đứng, 4 bánh xe chữ V lăn đều. Thanh HUD ẩn ở `100%`, hiện màu vàng ấm khi `< 100%`. | **`100%` chuẩn:** hành trình `8.0 m/s`, Boost `9.6 m/s`, nhảy `7.0 m`, quét `1.0×`. | Tiếng động cơ êm và nhịp nhún lò xo sạch sẽ. | Bình thản `O O` hoặc vui vẻ `^ ^`; nhăn mặt ngắn `0.35 s` (`FaceFeeling.Startled`) khi va chạm. | Thanh độ bền HUD trượt nhẹ ra lần đầu tiên độ bền giảm xuống dưới `100%`. |
| **Cột mốc 1:<br>`75%` (`75% – 51%`)** | **Cần Ăng-Ten Đuôi & Bộ Thu Phát Viễn Thông S-Band** (`Antenna` / `AntennaTip`) | Cần `Antenna` phía sau gập cong **`18°` sang trái-sau**; đèn hiệu trên đỉnh `AntennaTip` chớp tắt chập chờn (`3 Hz`); vết móp nhỏ trên vai phải của `Body`. | **Tốc độ lái giữ nguyên `100%` (`8.0 m/s`).** Các tín hiệu la bàn ở xa (`> 120 m`) bị rung góc `±4°` và chớp tắt `0.4 s` mỗi `3 s`; tầm phát hiện của máy quét giảm **`-15%` (`0.85×`)**. | Tiếng lẹt xẹt sóng mang S-band khe khẽ khi quét + tiếng dây thép `Antenna` ngân ở nốt **`B3`** khi xóc qua miệng hố. | Dòng quét ngang màn hình CRT rung nhẹ `1 px` khi thu sóng; mắt nheo tò mò `| |` (`FaceFeeling.Curious`) mỗi khi tín hiệu la bàn chập chờn. | Tiếng *tách-vụt* kim loại khi cần `Antenna` gập cong, tiếng rè radio `0.5 s` rơi từ **`F#4 → D4`**, và một vệt tia lửa thẳng ngắn bắn ra từ chân đế ăng-ten. |
| **Cột mốc 2:<br>`50%` (`50% – 26%`)** | **Đèn Pha Ngoài Bên Trái & Giảm Xóc Trước-Trái** (`HeadlampSocket` trái + `Shock_FL` / `Arm_FL`) | **Đèn pha tròn bên trái tắt ngóm hoàn toàn** (mặt kính tối đen, chỉ còn đèn phải và đèn giữa sáng); ống giảm xóc **`Shock_FL` sập lún xuống `-7 cm`**, làm càng `Arm_FL` chúi xuống và khiến toàn bộ thân 07 **nghiêng lệch `6.5°` về phía trước-trái**. | Tốc độ hành trình giảm nhẹ **`-10%` (`7.2 m/s`)**. Tay lái bất đối xứng: **tốc độ cua trái giảm `-18%` (`0.82×`)**, cua phải nhạy hơn `+10%`, và khi chạy qua ổ gà thân xe bị giật lệch nhẹ sang trái (`2.5°/s`); vũng sáng đèn pha bên trái mất `40%` diện tích. | Tiếng *cộc* kịch đáy giảm xóc ở nốt **`D2`** mỗi khi bánh trước-trái chạm đất + tiếng lò xo rên **`A2 → D3`** khi vào cua. | Đầu `Head` nghiêng ngược lại `3°` để giữ thăng bằng; mắt trái nheo một nửa trong khi mắt phải mở to (**`- O`**, biến thể lệch của `FaceFeeling.Tired`). | Tiếng *bụp-keng* đứt dây tóc bóng đèn chân không kèm tiếng *phập-xì* sập phuộc nhún, vũng sáng bên trái trên mặt đất vụt tắt, và 07 khụy lệch hẳn sang góc trước-trái. |
| **Cột mốc 3:<br>`25%` (`25% – 11%`)** | **Mô-Tơ Hộp Số Bánh Trước-Trái Bị Kẹt Cứng** (`Wheel_FL` / `Steer_FL`) | **`Wheel_FL` bị kẹt cứng ở góc choãi `5°` và ngừng quay** (đứng im `1.2 s`, giật cục `30°`, rồi lại khóa cứng), **cày một rãnh trượt sâu phẳng đáy liên tục trên lớp bụi Mặt Trăng** phía sau bánh trước-trái kèm các vệt tia lửa thẳng ngắn từ trục bánh. | **Tốc độ tối đa khóa ở `70%` (`5.6 m/s` — thấp hơn ngưỡng va chạm cứng `6.8 m/s`!)**, **khóa Boost Coils**, gia tốc giảm `-35%`, và bánh xe bị khóa kéo lệch mũi 07 **sang trái `8.5°/s`** (người chơi phải ghì nhẹ phím `D` / cần analog phải `~18%` để giữ xe đi thẳng). | Tiếng lốp sắt-cao su cày sột soạt qua lớp bụi regolith hòa cùng tiếng rít động cơ **`E3 → F#3`** và tiếng bánh răng trượt lạch cạch ở nốt **`D3`**. | Đôi mắt CRT gồng căng **`> <`** (`FaceFeeling.Worried`), liếc xuống góc bánh trước-trái đang bị kéo lê mỗi khi người chơi nhấn ga. | Tiếng *xoẹt-khực* vỡ cổ góp mô-tơ kèm chùm tia lửa thẳng ngắn từ trục `Wheel_FL`, tiếng kẹt bánh răng **`A3 → D2`**, và mũi 07 văng lệch `15°` sang trái khi bánh xe khóa cứng. |
| **Cột mốc 4:<br>`10%` (`10% – 1%`)** | **Bo Mạch Quét Dọc CRT & Giàn Tụ Cuộn Nhảy Từ Tính** (`FaceDisplay` / `R1CrtFace.shader` + `Coil_FL..RR`) | **Hình ảnh trên màn hình CRT bị xé ngang và trượt khung hình dọc** (`_FaceCrtState.z = 0.85` làm đôi mắt hổ phách tách lệch qua đường xé nhiễu); cửa sau `CargoHatch` hé lệch; cả 4 cuộn nhảy từ tính (`Coil_FL..RR`) đánh tia lửa thẳng ngắn xuống đất mỗi `2–3 s`. Thanh HUD chớp đỏ san hô `#C86446` ở `1.5 Hz`. | **Tốc độ bò khập khiễng `55%` (`4.4 m/s`)** kèm lực kéo lệch trái của bánh khóa; **Cuộn Nhảy Từ Tính (`HoverJump`) bị đánh lửa hụt** — bấm `Space` chỉ làm 07 nảy lệch một cú `0.35 m` (`12°` nghiêng thân) kèm tia lửa thay vì nhảy cao `7.0 m`; thời gian khóa mục tiêu máy quét lâu hơn **`+60%` (`1.6×`)**. | Tiếng ù biến áp `60 Hz` ở nốt **`B1`** + tiếng rơ-le bảo vệ nhảy tích tắc `1.5 Hz` ở nốt **`D4`**; tiếng cuộn nhảy đánh lửa hụt *bụp-xẹt* từ **`F#3 → D2`**. | Khuôn mặt CRT của 07 bị xé ngang qua đôi mắt hoảng hốt `> <`, cứ mỗi `1.8 s` lại chớp co rút dọc một nửa (`ScreenVerticalPinch = 0.45`) như đang cố gắng giữ tỉnh táo. | Cả 4 cuộn nhảy dưới gầm cùng xẹt tia lửa thẳng ngắn, hợp âm sụt áp 3 nốt đi xuống (**`A3 → F#3 → D3`**), và màn hình CRT co giật thành nửa đường chỉ ngang trong `0.3 s` trước khi gắng gượng mở lại. |
| **`0%`<br>Tắt Nguồn (Death)** | **Sập Nguồn Toàn Bộ Cụm Tụ Chính** | Xem Storyboard 3 khung hình ở §1.5 bên dưới (màn hình CRT thu thành một đường chỉ ngang mảnh rồi tắt hẳn; nhả Hộp Hàng Báo Hiệu). | Ngắt toàn bộ điều khiển lái; chuyển sang thước phim tắt nguồn `2.8 s` và hồi sinh tại Khung Bảo Dưỡng. | Hai nốt cello/synth trầm xuống (`A4 → D4`) + tiếng xả tụ CRT *fweeee-tik* rồi chìm vào im lặng chân không. | `ScreenVerticalPinch: 0.0 → 1.0` và `ScreenLineGlow: 0.0 → 1.0` rồi tắt đen hoàn toàn. | Khung hình 1 của cảnh tắt nguồn (§1.5). |

#### Chi Tiết 4 Cột Mốc Hỏng Hóc Cụm Bộ Phận (`75%`, `50%`, `25%`, `10%`)

1. **Cột Mốc `75%` (`75% – 51%` Độ Bền) — Gập Cần Ăng-Ten Đuôi & Nhiễu Sóng Thu Phát (`Antenna` / `AntennaTip`):**
   - **Cụm bộ phận bị hỏng:** Khớp lò xo chân cần ăng-ten đuôi (`Antenna`) và bộ lọc sóng mang S-band phía sau.
   - **Biểu hiện trên model (`RoverModelNodes.cs`):** Node `Antenna` xoay nghiêng `18°` sang trái và `8°` về phía sau; bóng đèn hiệu trên `AntennaTip` chuyển từ sáng đều sang chớp tắt bất quy tắc `3 Hz` (vẫn là nguồn sáng điểm trên đầu cần, tuyệt đối không có quầng sương trong chân không).
   - **Ảnh hưởng điều khiển (con số cụ thể):** Tốc độ chạy (`8.0 m/s`), Boost (`9.6 m/s`) và Nhảy (`7.0 m`) giữ nguyên `100%`. Tuy nhiên, các tín hiệu đánh dấu trên la bàn ở khoảng cách `> 120 m` bị dao động góc `±4°` và chớp ẩn `0.4 s` mỗi `3.0 s`; tầm phát hiện thụ động của máy quét giảm `-15%` (`0.85×`).
   - **Âm thanh:** Tiếng lẹt xẹt sóng mang S-band rất êm khi mở máy quét, kèm tiếng dây thép đàn hồi ở nốt **`B3`** mỗi khi 07 tiếp đất sau cú nhảy.
   - **Phản ứng mặt CRT:** Dòng quét ngang rung nhẹ `1 px` (`_FaceCrtState.z = 0.18`) khi dùng máy quét; mắt 07 thoáng nheo `| |` (`FaceFeeling.Curious`) khi vạch la bàn chập chờn.
   - **Nhịp cảnh báo một lần khi chạm `75%`:** Tiếng *tách-vụt* kim loại rõ nét từ đuôi xe, tiếng rè radio `0.5 s` rơi từ **`F#4 → D4`**, và một chùm 4–5 tia lửa thẳng ngắn bắn ra từ chân đế `Antenna`.

2. **Cột Mốc `50%` (`50% – 26%` Độ Bền) — Vỡ Đèn Pha Trái & Sập Giảm Xóc Trước-Trái (`HeadlampSocket` + `Shock_FL`):**
   - **Cụm bộ phận bị hỏng:** Bóng đèn pha halogen/vòm ngoài bên trái (`Lamp_0` dưới `HeadlampSocket`) và khoang dầu-khí của ống giảm xóc trước-trái (`Shock_FL` / `Arm_FL`).
   - **Biểu hiện trên model (`RoverModelNodes.cs`):** Đèn pha tròn bên trái tắt đen hoàn toàn (`_EmissionStrength = 0`, mặt kính nứt mờ), trong khi đèn tròn bên phải và đèn nhỏ ở giữa vẫn sáng rực. Hành trình nghỉ của `Shock_FL` sập lún xuống `-0.07 m` (`-7 cm`), ép càng `Arm_FL` gập lên `11°` khiến toàn bộ thân trắng của 07 (`Body`) **nghiêng hẳn `6.5°` về góc trước-trái**.
   - **Ảnh hưởng điều khiển (con số cụ thể):** Tốc độ hành trình tối đa giảm **`-10%` (`7.2 m/s`)**; **tốc độ xoay mũi sang trái giảm `-18%` (`0.82×`)** vì góc trước-trái bị đè nặng, trong khi xoay phải nhạy hơn `+10%` (`1.10×`); khi lăn qua địa hình gồ ghề, phuộc nhún bị sập tạo lực giật lệch mũi sang trái `2.5°/s` (`0.8°/s` trên đường bằng); vũng sáng chiếu trên mặt đất phía trước mất `40%` góc mở bên trái.
   - **Âm thanh:** Mỗi khi bánh trước-trái lăn qua đá hoặc tiếp đất, ống giảm xóc kịch đáy phát tiếng *cộc* trầm đục ở nốt **`D2`** kèm tiếng lò xo rên **`A2 → D3`**.
   - **Phản ứng mặt CRT:** Khớp `Head` nghiêng ngược `3°` sang phải để bù góc nghiêng của thân xe; mắt trái của 07 nheo lại một nửa thành nét gạch ngang trong khi mắt phải mở tròn (**`- O`**, biến thể bất đối xứng của `FaceFeeling.Tired`).
   - **Nhịp cảnh báo một lần khi chạm `50%`:** Tiếng *bụp-keng* vỡ bóng đèn chân không cùng tiếng *phập-xì* sập phuộc nhún, vũng sáng bên trái trên nền bụi vụt tắt ngay trước mũi xe, và khung gầm 07 sụp nghiêng sang trái.

3. **Cột Mốc `25%` (`25% – 11%` Độ Bền) — Kẹt Cứng Mô-Tơ Bánh Trước-Trái & Cày Rãnh Bụi (`Wheel_FL` / `Steer_FL`):**
   - **Cụm bộ phận bị hỏng:** Mô-tơ điện một chiều và hộp số hành tinh bên trong trục bánh trước-trái (`Wheel_FL` / `Steer_FL`).
   - **Biểu hiện trên model (`RoverModelNodes.cs`):** Khớp `Steer_FL` bị kẹt lệch vào trong `5°` và **`Wheel_FL` ngừng quay hoàn toàn** (khóa cứng `0 rad/s` trong `1.2 s`, giật cục một góc nhỏ `30°` kèm vài tia lửa thẳng ngắn ở trục bánh, rồi lại khóa cứng) trong khi 3 bánh còn lại (`Wheel_FR`, `Wheel_RL`, `Wheel_RR`) vẫn quay bình thường. Bánh trước-trái bị kéo lê **cày một rãnh trượt sâu, phẳng đáy và liên tục trên lớp bụi regolith** phía sau 07.
   - **Ảnh hưởng điều khiển (con số cụ thể):**
     - **Tốc độ tối đa giới hạn ở `70%` (`5.6 m/s`)** — nằm *dưới* ngưỡng sát thương va chạm cứng $v_{\text{min}} = 6.8\text{ m/s}$ một khoảng an toàn, giúp 07 đang tập tễnh bò về nhà trên đường bằng **không thể tự đâm chết mình vì va phải đá ven đường**!
     - **Khóa hoàn toàn Boost Coils (`Shift`)** và giảm `-35%` gia tốc (`0.65×`).
     - **Lực kéo lệch tay lái rõ rệt:** Bánh trước-trái bị khóa tạo mô-men cản kéo mũi 07 **lệch sang trái `8.5°/s`** khi chạy thẳng, buộc người chơi phải ghì nhẹ phím `D` / cần analog phải (`~18%` hành trình lái) để giữ xe đi thẳng.
   - **Âm thanh:** Tiếng lốp xe cày sột soạt liên tục trên lớp bụi Mặt Trăng hòa cùng tiếng rít gồng tải của 3 mô-tơ còn lại (**`E3 → F#3`**) và tiếng cá cóc bánh răng trượt lạch cạch ở nốt **`D3`** mỗi `1.2 s`.
   - **Phản ứng mặt CRT:** Đôi mắt CRT gồng chặt **`> <`** (`FaceFeeling.Worried`), thỉnh thoảng liếc xuống góc dưới bên trái về phía bánh xe đang bị kéo lê.
   - **Nhịp cảnh báo một lần khi chạm `25%`:** Tiếng *xoẹt-khực* cháy cổ góp kèm chùm tia lửa thẳng ngắn bắn ra từ trục `Wheel_FL`, tiếng nghiến bánh răng **`A3 → D2`**, và mũi 07 văng lệch `15°` sang trái ngay khoảnh khắc bánh xe khóa cứng và bắt đầu cày rãnh trên bụi.

4. **Cột Mốc `10%` (`10% – 1%` Độ Bền) — Xé Hình Màn Hình CRT & Đánh Lửa Hụt Cuộn Nhảy (`FaceDisplay` + `Coil_FL..RR`):**
   - **Cụm bộ phận bị hỏng:** Bo mạch lái tia quét dọc của màn hình CRT (`FaceDisplay` / `R1CrtFace.shader`) và giàn tụ phóng của 4 Cuộn Nhảy Từ Tính (`Coil_FL`, `Coil_FR`, `Coil_RL`, `Coil_RR`).
   - **Biểu hiện trên model (`RoverModelNodes.cs` & `R1CrtFace.shader`):**
     - Trên màn hình `FaceDisplay`, `_FaceCrtState.z` (`LowPower = 0.85`, `VerticalJitter = 0.75`) tạo ra **đường xé ngang màn hình (horizontal scanline tear) rõ rệt** làm nửa trên và nửa dưới đôi mắt hổ phách của 07 bị lệch nhau `±12%` UV, đồng thời cứ mỗi `1.8 s` khung hình lại trượt dọc và co rút một nửa (`ScreenVerticalPinch` nháy lên `0.45`).
     - Cả 4 cuộn nhảy dưới gầm (`Coil_FL..RR`) xả điện rò rỉ dưới dạng các vệt tia lửa thẳng ngắn bắn xuống bụi mỗi `2–3 s`; nắp khoang hàng `CargoHatch` phía sau bung chốt hé lệch `8°`.
   - **Ảnh hưởng điều khiển (con số cụ thể):**
     - **Tốc độ bò khập khiễng giới hạn ở `55%` (`4.4 m/s`)** cùng lực kéo lệch trái `8.5°/s` của bánh trước-trái đang khóa cứng.
     - **Cuộn Nhảy Từ Tính (`HoverJump`) đánh lửa hụt:** Bấm `Space` không còn kích hoạt được cú nhảy `7.0 m`; thay vào đó, tụ điện bị rò chỉ nổ *bụp* ở một bên gầm, hất 07 **nảy lệch một cú nhỏ `0.35 m` (`12°` nghiêng thân)** kèm tia lửa thẳng ngắn. *(Lưu ý: Xung từ thoát kẹp khi bị Reclaimer giữ ở §3.2 vẫn hoạt động vì chỉ cần xả ngắn mạch tầm gần).*
     - **Thời gian khóa mục tiêu máy quét tăng `+60%` (`1.6×`)**, và tia quét bị giật lệch nhẹ `0.25 s` ở mốc `70%` tiến độ do sụt áp.
   - **Âm thanh:** Tiếng ù biến áp sụt áp `60 Hz` ở nốt **`B1`** đi kèm tiếng rơ-le bảo vệ nhảy tích tắc `1.5 Hz` ở nốt **`D4`** (khớp nhịp chớp đỏ san hô của thanh HUD); bấm `Space` phát tiếng tụ điện đánh lửa hụt *bụp-xẹt* rơi từ **`F#3 → D2`**.
   - **Phản ứng mặt CRT:** Đôi mắt `> <` của 07 bị cắt ngang bởi đường xé nhiễu phốt-pho và chớp co rút dọc mỗi `1.8 s`, truyền tải trọn vẹn cảm giác một chú robot nhỏ đang dốc toàn bộ giọt điện áp cuối cùng để bò về tới ánh đèn ấm áp của Khung Bảo Dưỡng.
   - **Nhịp cảnh báo một lần khi chạm `10%`:** Cả 4 cuộn nhảy dưới gầm cùng xẹt tia lửa thẳng ngắn xuống nền bụi, hợp âm sụt áp 3 nốt đi xuống (**`A3 → F#3 → D3`**), và màn hình CRT co rút thành nửa đường chỉ ngang trong `0.3 s` trước khi gắng gượng mở lại.

---

### 1.5 Khoảnh Khắc Tắt Nguồn (Death Moment) & Luồng Khởi Động Lại (Storyboard)

Khi `WearIntegrity` chạm `0%`, **không có vụ nổ kiểu arcade, không nháy đỏ màn hình và không có chữ "GAME OVER"**. Đúng tinh thần Trụ cột 1 & 6 và tận dụng trực tiếp [`R1CrtFace.shader`](../../Assets/_Project/Shaders/R1/R1CrtFace.shader#L34-L37) (vốn đã hỗ trợ thu hẹp dọc CRT qua `_FaceContentState.y` và vệt sáng ngang phốt-pho qua `_FaceContentState.z`), cái chết của 07 là một thước phim tắt nguồn dài `2.8 s` đẹp, buồn và tĩnh lặng:

| Khung Hình | Thời Gian | Hành Động Thị Giác & Shader (`R1CrtFace.shader` / `RoverModelNodes`) | Âm Thanh & Máy Quay |
| :--- | :---: | :--- | :--- |
| **Khung 1: Rùng Mình Cuối & Nhả Hộp Hàng Báo Hiệu** | `0.0 s – 0.6 s` | Những vệt tia lửa thẳng ngắn rơi xuống bụi xám. Cả 4 ống giảm xóc (`Shock_FL..RR`) lún xuống `8 cm`, khớp cổ (`Neck`) gục nhẹ `12°` về trước. Nếu khoang hàng (`CargoHold`) có đồ, nắp `CargoHatch` phía sau bật mở và đẩy một **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** màu cam-đồng kín khít rơi xuống cạnh 07. | Tay cầm/bàn phím nhả điều khiển mượt mà. Máy quay lùi nhẹ `1.8 m` và hạ thấp ngang tầm mắt. Hai nốt nhạc cello/synth Ngũ cung Rê trưởng trầm xuống (`A4 → D4`) vang lên cùng tiếng ngắt rơ-le cơ khí. |
| **Khung 2: Màn Hình CRT Thu Thành Một Đường Chỉ Ngang** | `0.6 s – 1.5 s` | [`FaceDisplay.cs`](../../Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs) kéo `ScreenVerticalPinch` (`0.0 → 1.0`) và `ScreenLineGlow` (`0.0 → 1.0`). Đôi mắt viên thuốc màu hổ phách của 07 khép dọc lại thành **một đường chỉ sáng ngang mảnh sắc nét** giữa màn kính CRT tối, rồi thu gọn tiếp thành một chấm sáng nhỏ ở tâm. Cùng lúc đó, cụm đèn trước (`R1_LampAmber`) và hàng đèn pin (`BatteryLamp_0..7`) lụi dần từ vàng ấm sang đỏ than rồi tắt hẳn. | Tiếng xả tụ cao áp CRT cổ điển thập niên 1980 (*fweeee-tik*) nhỏ dần khi tiếng sóng mang radio mỏng đi rồi tan vào sự im lặng tuyệt đối của chân không. |
| **Khung 3: Tĩnh Lặng & Đóng Rơ-Le Hồi Sinh** | `1.5 s – 2.8 s` | Chấm sáng giữa màn hình CRT tắt hẳn, để lại mặt kính tối phản chiếu bầu trời sao. Đèn đỉnh ăng-ten (`AntennaTip`) tắt. 07 nằm bất động trong bóng tối sắc lạnh dưới Trái Đất tối không ánh đèn thành phố ([`story-v4.md`](story-v4.md) Quyết định 3), trong khi **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** bên cạnh chớp bóng đèn vòm màu vàng ấm `0.5 Hz` (hắt vũng sáng lên nền bụi xung quanh mà không tạo cột sáng trong chân không). Ở `2.8 s`, màn hình mờ dần sang đen (`1.2 s`) cùng tiếng lách cách băng từ, rồi mở ra tại **Khung Bảo Dưỡng (Service Gantry)** hoặc **Trạm Xe (Rover Bay)** gần nhất. | Khoảng lặng `1.0 s`, nối tiếp bằng tiếng tụ điện ấm áp tại `Service Gantry` / `Rover Bay` nạp đầy khi màn hình CRT của 07 khởi động mở dọc trở lại (`BootProgress: 0.0 → 1.0`). |

---

### 1.6 Hệ Quả Khi Chết, Lý Giải Vật Lý Trong Truyện & Các Phương Án Đã Xét (`[DECIDED]`)

#### A. Hệ Quả Khi Chết Đã Chốt (`[DECIDED]` — Phán Quyết 17 & Duyệt Của Chủ Dự Án 2026-10-17)
1. **Nơi 07 Khởi Động Lại:** 07 thức dậy tại **Khung Bảo Dưỡng (`Service Gantry`) hoặc Trạm Xe (`Rover Bay`)** cuối cùng từng ghé (mặc định là `Rover Bay` tại Thung Lũng Nhà trước khi xây trạm tiền tiêu).
2. **Những Gì 07 Giữ Nguyên:** Toàn bộ thiết bị/kit đã lắp trên xe, nâng cấp pin/khoang hàng, công thức chế tạo đã mở, mục từ điển đã quét, đồng minh đã lập trình và tiến trình cốt truyện được **giữ nguyên 100%**.
3. **Những Gì Ở Lại Hiện Trường:** Chỉ các **nguyên liệu thô và vật phẩm rời đang nằm trong `CargoHold`** ([`M3-25-cargo-and-storage.md`](../features/M3-25-cargo-and-storage.md)) nằm lại bên trong **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** tại đúng tọa độ tắt nguồn (hoặc nằm trong **Thùng Phân Loại Số 03 — Sorting Bin 03** tại Bãi Thu Hồi nếu 07 tắt nguồn trong kẹp của Reclaimer — xem §1.6.B & §3.2).
   - **Cargo Beacon Pod** phát sáng đèn vòm màu hổ phách ấm (tạo vũng sáng trên mặt đất/đống phế liệu xung quanh) và hiển thị trên la bàn/máy quét từ mọi khoảng cách.
   - Lái xe tới chạm vào hoặc bấm Tương tác (`E`) sẽ nạp toàn bộ hàng trở lại khoang của 07.
   - **Quy Tắc Nhiều Lần Chết:** Tối đa `3` Cargo Beacon có thể tồn tại đồng thời trên thế giới (tránh việc lỡ đâm xe lần nữa trên đường đi nhặt lại làm mất mẻ hàng quý trước đó).

#### B. 07 Quay Về Bằng Cách Nào Theo Logic Vật Lý Trong Truyện? (Một Quy Tắc Vật Lý Duy Nhất)
Phán quyết 17 yêu cầu một **lời giải thích vật lý 100% trong thế giới truyện** để việc trở lại căn cứ không bao giờ giống phép dịch chuyển tức thời, không sao chép ký ức và không sinh ra khung xe thứ hai. Mọi trường hợp đều gói gọn trong **một quy tắc vật lý rõ ràng trong một câu duy nhất**:

> **Quy Tắc Vật Lý Một Câu Duy Nhất:** Bất kể khi nào độ bền của 07 chạm `0%` — dù do va chạm ngoài thực địa hay trong gọng kẹp của Reclaimer — 07 đều tự tháo chốt để lại **Hộp Hàng Báo Hiệu (Cargo Beacon Pod)** nặng (tại chỗ va chạm, hoặc nằm trong **Thùng Phân Loại Số 03** sau khi Reclaimer chở 07 về Bãi Thu Hồi), còn **Tilly** (`friend.tilly`, chiếc drone bay nhỏ của phi hành đoàn, hoặc xe tời cứu hộ của căn cứ) sẽ kéo chính khung xe đã nhẹ tải của 07 về **Khung Bảo Dưỡng / Trạm Xe** gần nhất để gò phẳng và hàn lại.

- **Người Chơi Nhìn Thấy Điều Này Như Thế Nào Khi Tỉnh Dậy:** Khi `1.2 s` màn hình đen mở ra bên trong Service Gantry / Rover Bay (đại diện cho vài giờ yên tĩnh trôi qua trên Mặt Trăng), màn hình CRT của 07 sáng lên (`BootProgress 0 → 1`) đúng lúc cánh tay hàn trên cao của Gantry vừa thu lên khỏi đường hàn còn ấm trên lớp vỏ trắng vừa được gò phẳng của 07 (cùng Tilly bay lơ lửng bên cạnh chíp chào). Chỉ có **duy nhất một thân xe `07`** — được kéo về sau khi bỏ lại hộp hàng nặng và được gò hàn lại tại căn cứ.

#### C. Hồ Sơ Lưu Trữ: Hai Phương Án Thay Thế Đã Xét & Loại Bỏ (2026-10-17)

Để lưu vết thiết kế, hai phương án thay thế dưới đây đã được đặt lên bàn cân trong đợt duyệt ngày 2026-10-17 trước khi Chủ dự án chốt phương án **Mặc định (`[DECIDED]`)**:

| Mô Hình Hệ Quả | Cách Hoạt Động | Ưu Điểm (Pros) | Nhược Điểm (Cons) |
| :--- | :--- | :--- | :--- |
| **ĐÃ CHỐT (`[DECIDED]` Mặc Định):<br>Tilly Kéo Về Gantry + 100% Hàng Rời Ở Lại Trong Beacon / Thùng 03** | Tilly kéo khung xe nhẹ tải của 07 về Dock/Gantry gần nhất cùng mọi kit và tiến trình; `100%` hàng rời trong khoang nằm lại tại hiện trường (hoặc trong `Thùng Phân Loại Số 03` ở Bãi Thu Hồi) trong một Cargo Beacon có thể thu hồi. | - Không mất mát vĩnh viễn; biến tai nạn thành một chuyến đi cứu hộ có mục tiêu.<br>- Tách bạch rõ *tiến trình vĩnh viễn* (kit/bản vẽ) với *rủi ro chuyến đi* (hàng trong khoang). | - Người chơi cần lái xe quay lại chỗ cũ (hoặc Bãi Thu Hồi) để nhặt hàng. |
| **Phương Án Thay Thế 1 (Đã Loại):<br>"Tilly Kéo Cả Xe Lẫn Hàng" — Giữ 75% Hàng, Rơi Mất 25% Dọc Đường** | Tilly kéo cả 07 *lẫn* khoang hàng về thẳng Gantry; `25%` nguyên liệu phổ thông bị rơi mất dọc đường kéo; `75%` còn nguyên trong khoang ngay khi tỉnh dậy. | - Không phải chạy lại đường cũ (Zero Corpse-Run). | - Giảm độ hồi hộp khi chở đầy khoang hàng quý về nhà và xóa vĩnh viễn `25%` quặng phổ thông. |
| **Phương Án Thay Thế 2 (Đã Loại):<br>"Khung Xe Mắc Kẹt & Xe Cứu Hộ Phụ"** | Thân xác thật của 07 nằm tắt nguồn tại hiện trường; người chơi tỉnh dậy ở căn cứ trong một **Xe Kéo Phụ** thô sơ để tự lái ra hàn cứu `07`. | - Tính liên tục tại chỗ 100%. | - Rủi ro kẹt cứng (soft-lock) rất cao khi phải lái xe phụ chưa nâng cấp vượt qua các vùng `0.6+` có nguy hiểm môi trường. |

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
| `Milestone1_AntennaThreshold` | **`75%`** | `70% – 80%` | `WearSettings.cs` | Cột mốc 1 (`[DECIDED]`): cong `Antenna` `18°`, nhiễu la bàn xa, tầm quét `0.85×`. |
| `Milestone2_StrutLampThreshold` | **`50%`** | `45% – 55%` | `WearSettings.cs` | Cột mốc 2 (`[DECIDED]`): tắt đèn pha trái, sập `Shock_FL` (`6.5°` nghiêng), tốc độ `7.2 m/s` (`0.90×`), cua trái `0.82×`. |
| `Milestone3_LockedWheelThreshold` | **`25%`** | `20% – 30%` | `WearSettings.cs` | Cột mốc 3 (`[DECIDED]`): khóa cứng `Wheel_FL` cày rãnh bụi, tốc độ `5.6 m/s` (`0.70×`), kéo lệch trái `8.5°/s`, khóa Boost. |
| `Milestone4_CrtCoilThreshold` | **`10%`** | `8% – 15%` | `WearSettings.cs` | Cột mốc 4 (`[DECIDED]`): xé ngang CRT (`LowPower = 0.85`), tốc độ `4.4 m/s` (`0.55×`), cuộn nhảy đánh lửa hụt (`0.35 m`), thời gian quét `1.60×`. |

---

### 1.8 Tương Tác Với Pin (`M3-20`), Cuộn Nhảy (Leap Coils) & Kéo (Tow)

1. **Pin (`M3-20`):**
   - **Hai Kênh Hoàn Toàn Đập Lập:** Pin đo *năng lượng/tầm đi xa*; Wear đo *độ nguyên vẹn cơ học*.
   - **Miễn Nhiễm Tự Nhiên Khi Bò Hết Pin:** Khi pin về `0%`, 07 bò ở tốc độ `25%` (`2.0 m/s`) với Tilly (`friend.tilly`) bay ra dẫn đường. Vì `2.0 m/s << 6.8 m/s`, **07 không thể bị móp vỏ do va chạm khi đang bò hết pin** trên địa hình thường.
   - **Khi Vừa Hết Pin Vừa Hỏng Nặng:** Tốc độ lấy `min(batteryMult, wearMult) = 0.25×` (`2.0 m/s`) thay vì nhân chồng làm xe đứng im.
2. **Cuộn Nhảy Từ Tính ([`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs)):**
   - **Từ `100%` đến `11%` độ bền (Nguyên vẹn và Cột mốc 1–3):** Nhảy hoạt động `100%` công suất (`0.8 m` nhấp nhẹ, `7.0 m` nhảy cao, hạ cánh đệm từ `1.6 m/s`). Mọi cú tiếp đất có đệm xuống bụi hoặc sàn công trình đều tốn **0 độ bền**.
   - **Tại Cột mốc 4 (`10% – 1%` — Hỏng Mạch CRT & Giàn Tụ Cuộn Nhảy):** Các cuộn nhảy từ tính bị đánh lửa hụt — bấm `Space` chỉ làm 07 nảy lệch một cú nhỏ `0.35 m` (`12°` nghiêng thân) kèm tia lửa thẳng ngắn và tiếng rơ-le sụt áp.
   - **Khi Đang Bị Reclaimer Kẹp:** Bấm `Space` xả xung từ tầm gần của cuộn nhảy tạo ra lực đẩy ngược từ trường, cộng ngay **`+60%` thanh thoát kẹp** ngay cả ở Cột mốc 4 (xem §3.2).
3. **Kéo Tải & Gỡ Kẹt:**
   - Các va chạm từ chính vật thể đang được kéo phía sau 07 được **bỏ qua (`0` hao mòn)** để vật lý dây kéo không bao giờ làm hại người chơi.
   - Khi 07 được nhấc nhẹ theo cung tròn để gỡ kẹt ([`docs/VISION.md` Phán quyết 7](../VISION.md#L122-L125)), 07 nhận `1.5 s` miễn nhiễm va chạm.

---

### 1.9 Các Chế Độ Chơi Mới (`Survival`, `"Chỉ khám phá" / "Explore only"` và `Hardcore`, `[DECIDED]`)

Khi tạo trò chơi mới (**New Game**), người chơi chọn giữa các chế độ rõ ràng đã được Chủ dự án chốt tên và lộ trình (`2026-10-17`):

| Chế Độ | Trạng Thái | Quy Tắc & Hành Vi | Đối Tượng Người Chơi |
| :--- | :---: | :--- | :--- |
| **1. Survival (Mặc Định)** | **`[DECIDED]`** | Đầy đủ Hao mòn qua 4 cột mốc (`75% / 50% / 25% / 10%`), Sửa chữa vật lý (Bộ Sửa Chữa Thực Địa thay module ORU & Khung Bảo Dưỡng), Reclaimers, Nguy hiểm vùng miền và vòng lặp Tắt nguồn / Thu hồi Cargo Beacon. Pin vẫn không gây chết (`M3-20`). | Trải nghiệm mặc định: khám phá tĩnh lặng kết hợp chuẩn bị kỹ thuật xúc giác và sự hồi hộp vừa đủ. |
| **2. `"Chỉ khám phá"` / `"Explore only"`** | **`[DECIDED]`** | **Không hao mòn, không chết.** `WearIntegrity` khóa ở `100%` (thanh độ bền HUD luôn ẩn). Đâm vào đá vẫn có tiếng cộc kim loại và hạt bụi bắn đạn đạo. Reclaimers vẫn tuần tra ở vùng `0.6+` như những cỗ máy cảnh quan sống động, **không bao giờ kẹp 07**, và vẫn có thể được quét/lập trình thành đồng minh chở phế liệu về nhà! | Người chơi muốn tận hưởng trọn vẹn *Mặt Trăng cô đơn ấm áp* hoàn toàn thư giãn, chỉ tập trung khám phá và xây dựng mà không có áp lực hỏng xe. |
| **3. Hardcore ("Một Khung Gầm Duy Nhất")** | **`[DECIDED]` — Hoãn sang `1.0` (Tùy chọn Opt-In)** | Giống hệt Survival, nhưng nếu `WearIntegrity` chạm `0%`, file lưu kết thúc vĩnh viễn sau Khung hình 3 của cảnh tắt nguồn CRT. **Không đưa vào các bản đầu (`0.5.7 / 0.6`); để dành làm chế độ tùy chọn (opt-in) ở bản `1.0`.** | Người chơi kỳ cựu ở bản `1.0` muốn thử thách căng thẳng tuyệt đối trong lần chơi thứ hai. |

---

## 2. Sửa Chữa Cơ Lý (Phán Quyết 14: 07 Không Có Bàn Tay)

Theo [`docs/VISION.md` Phán quyết 14](../VISION.md#L158-L162), **07 không có bàn tay, không có cánh tay người và không có đám mây sửa chữa phép thuật**. Mọi bước sửa chữa đều phải diễn ra qua các cơ cấu có thật trên thân 07 v3 ([`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs)) hoặc trên công trình căn cứ.

### 2.1 Bộ Sửa Chữa Thực Địa: Thay Thế Module ORU, Không Chỉ Hàn Tấm Phẳng (`item.repair_kit`, `[DECIDED]`)

> [!NOTE]
> ### Có khả thi về mặt kỹ thuật không? (Đối Chiếu Thực Tế Vũ Trụ Với Sửa Chữa Ngoài Thực Địa)
> - **Năm 1972 — Sửa Chắn Bùn Xe Tự Hành Apollo 17:** Tại thung lũng Taurus-Littrow, Eugene Cernan và Harrison Schmitt đã sửa tấm chắn bùn bị gãy của chiếc Lunar Roving Vehicle bằng cách kẹp 4 tấm bản đồ Mặt Trăng cán nhựa bằng băng keo xám và hai chiếc kẹp đèn lấy từ cabin tàu đổ bộ. Đó là một giải pháp ứng biến khẩn cấp tuyệt vời cho **tấm chắn bụi bên ngoài**, nhưng nó chỉ ngăn bụi bay vào mặt chứ không thể sửa được một cụm cơ-điện bị hỏng.
> - **Năm 1969 — Thí Nghiệm Hàn Chân Không *Vulkan* Của Liên Xô (*Soyuz 6*):** Phi hành gia Valeri Kubasov và Georgi Shonin đã vận hành thiết bị *Vulkan* của Viện Hàn Điện Paton trong khoang chân không mở của tàu *Soyuz 6*, chứng minh rằng hàn hồ quang plasma áp suất thấp, hàn chùm tia điện tử và hàn điện cực nóng chảy hoạt động cực tốt trong chân không vũ trụ (tạo mối hàn kim loại rất sạch, không bọt khí oxy hóa và không có khói).
> - **Vì Sao Một Tấm Thép Phẳng Không Thể Cứu Mô-Tơ Bánh Xe Cháy Hay Phuộc Nhún Sập:** Hàn một tấm thép phẳng lên vỏ xe có thể bịt vết rách trên lớp vỏ nhôm-thép, nhưng **hoàn toàn không thể** hồi sinh cuộn dây mô-tơ bánh xe đã cháy đen ở mốc `25%`, ống giảm xóc đã vỡ phớt ở mốc `50%`, hay bo mạch quét CRT bị chập ở mốc `10%`. Trong kỹ thuật vũ trụ thực tế (trên Kính viễn vọng Hubble, Trạm Vũ trụ Quốc tế ISS và các xe tự hành hành tinh hiện đại), các cụm cơ-điện hỏng ngoài thực địa luôn được sửa bằng cách rút bỏ **Khối Thay Thế Tại Chỗ (Line-Replaceable Unit / Orbital Replacement Unit — ORU)**: một hộp module đóng gói sẵn gồm lõi mô-tơ/cuộn dây, khớp nối nhanh tự định tâm và tấm mặt bích bảo vệ bên ngoài, cho phép một cánh tay dịch vụ robot đơn giản tháo chốt rút hộp cũ bị cháy ra, cắm hộp ORU mới vào ổ, rồi hàn kín tấm nắp chắn bụi bên ngoài.

![Storyboard 4 khung hình: 07 thực hiện thay thế module ORU bằng tay bảo trì sườn và đầu hàn ngực CutterOrigin](../../Logs/concepts/repair_kit_module_swap.png)

#### Các Quy Tắc Cốt Lõi Của Bộ Sửa Chữa Thực Địa (`[DECIDED]`)
1. **Dùng một lần duy nhất (`1 bộ chế tạo = 1 lần sửa, tiêu hao ngay`):** Mỗi **Bộ Sửa Chữa Thực Địa (`item.repair_kit`)** là một hộp module thay thế ORU đóng gói sẵn. Bấm phím **Sửa Thực Địa (`R`)** khi đang đứng yên sẽ tiêu hao đúng `1` bộ trong khoang hàng.
2. **Sửa cụm bộ phận hỏng nặng nhất hiện tại và hồi phục một cột mốc hao mòn:**
   - Mỗi lần dùng sẽ ưu tiên **thay thế cụm bộ phận đang hỏng nặng nhất trên 07** và đưa `WearIntegrity` **lên ngay ngưỡng trên của cột mốc trước đó** (tối đa ngoài thực địa là `90%`, vì lớp sơn/vỏ hoàn hảo `100%` như mới xuất xưởng cần búa gò và máy cân chỉnh của Khung Bảo Dưỡng):
     - Đang ở **Cột mốc 4 (`10% – 1%`, Xé CRT & Hụt Cuộn Nhảy)** $\xrightarrow{\text{1 Bộ Sửa Chữa}}$ thay module bo mạch/tụ phóng, nâng độ bền lên **`25%`** (xóa lỗi xé CRT và phục hồi nhảy `HoverJump` đầy đủ; xe lùi về trạng thái Cột mốc 3).
     - Đang ở **Cột mốc 3 (`25% – 11%`, Khóa Bánh Trước-Trái)** $\xrightarrow{\text{1 Bộ Sửa Chữa}}$ thay lõi mô-tơ trục bánh `Wheel_FL`, nâng độ bền lên **`50%`** (bánh trước-trái quay trơn tru trở lại, hết cày rãnh bụi, mở khóa Boost; xe lùi về trạng thái Cột mốc 2).
     - Đang ở **Cột mốc 2 (`50% – 26%`, Tắt Đèn Trái & Sập Giảm Xóc)** $\xrightarrow{\text{1 Bộ Sửa Chữa}}$ thay cụm phuộc-đèn trước-trái, nâng độ bền lên **`75%`** (đèn pha trái sáng lại, khung gầm hết nghiêng, tốc độ trở về `100%`; xe lùi về trạng thái Cột mốc 1).
     - Đang ở **Cột mốc 1 (`75% – 51%`, Cong Ăng-Ten & Nhiễu Sóng)** $\xrightarrow{\text{1 Bộ Sửa Chữa}}$ thay khớp chân ăng-ten/bộ lọc S-band, nâng độ bền lên **`90%`** (tất cả cụm bộ phận đều lành lặn `100%`, chỉ còn vết tấm ốp xanh xô thơm ngoài vỏ chờ Khung Bảo Dưỡng đánh bóng lên `100%`).
3. **Không có bộ sửa thì không thể tự sửa ngoài thực địa — chỉ Khung Bảo Dưỡng mới đại tu toàn diện:** Nếu trong khoang hàng của 07 có `0` Bộ Sửa Chữa Thực Địa, 07 **không thể tự sửa ở ngoài đồng**; người chơi phải lái (hoặc tập tễnh bò) về **Khung Bảo Dưỡng (`structure.service_gantry`)** tại căn cứ hoặc trạm tiền tiêu để đại tu toàn diện miễn phí bằng điện năng.
4. **Chuỗi 4 bước thao tác vật lý với mảnh vỡ tồn tại trên mặt đất:** Quá trình sửa sử dụng **tay bảo trì có khớp gập nhỏ gắn trên thanh ray sườn 07** kết hợp với **chính đầu hàn hồ quang bằng đồng ở giữa ngực 07 (`CutterOrigin`)**, và để lại **module hỏng bị cháy đen nằm lại trên nền bụi Mặt Trăng như một mảnh vỡ vật lý cố định**.

#### Công Thức Chế Tạo Tại Máy Chế Tạo (`SalvageMaterial.cs` & `M3-27`)
Được chế tạo tại **Máy Chế Tạo (Fabricator)** (`StructureIds.Fabricator = "structure.fabricator"`) từ các nguyên liệu T1 có sẵn trong [`SalvageMaterial.cs`](../../Assets/_Project/Scripts/Core/SalvageMaterial.cs#L12-L41):

| Mã Vật Phẩm | Tên Gọi | Cấp & Cột Mốc | Công Thức Chế Tạo (`SalvageMaterial`) | Thời Gian Chế | Xếp Chồng Trong Khoang | Tác Dụng Khi Sử Dụng (`[DECIDED]`) |
| :--- | :--- | :---: | :--- | :---: | :---: | :--- |
| `item.repair_kit` | **Bộ Sửa Chữa Thực Địa (Field Repair Kit — ORU Cartridge)** | **T1 (`0.5.7`)** | **`2 × Metal` + `2 × Wiring` + `1 × Optics`** *(vỏ thép & bánh răng + cuộn dây đồng + cảm biến quang học/bóng đèn)* | `3.0 s` | `3` bộ / ô khoang hàng | **Tiêu hao 1 lần:** Thay thế cụm bộ phận hỏng nặng nhất hiện tại và hồi phục **1 cột mốc hao mòn** (`10–1% → 25%`, `25–11% → 50%`, `50–26% → 75%`, `75–51% → 90%`). |

#### Storyboard 4 Khung Hình (`4.5 s`): Thay Thế Module ORU Ngoài Thực Địa (`Logs/concepts/repair_kit_module_swap.png`)

Khi `WearIntegrity <= 75%`, 07 đang dừng hẳn và trong khoang có ít nhất `1 × item.repair_kit`, bấm phím **Sửa Thực Địa (`R`)** sẽ chạy **chuỗi 4 bước thay thế module vật lý dài `4.5 s`**:

1. **Khung 1 — `0.0 s – 1.1 s` (`1. UNLATCH BROKEN MODULE` / Tháo Chốt Module Hỏng):**
   - 07 khóa phanh đứng yên trên nền bụi xám; đầu CRT (`Head`) cúi xuống nhìn vào cụm bộ phận đang hỏng nặng nhất (ví dụ: trục bánh trước-trái `Wheel_FL` đang bị kẹt lệch) với đôi mắt tập trung `| |`.
   - **Tay bảo trì nhỏ bằng đồng và thép có khớp gập của 07** (xếp gọn dọc thanh ray bên sườn xe khi chạy) mở ra, vươn tới tâm trục bánh trước-trái, xoay ngàm khóa 90 độ (`cạch-keng` ở nốt `A3 → D3`) và mở chốt giữ khối module mô-tơ hình trụ đã cháy đen.
2. **Khung 2 — `1.1 s – 2.2 s` (`2. DROP BROKEN MODULE (DEBRIS)` / Thả Rơi Module Hỏng Thành Mảnh Vỡ Trên Đất):**
   - Tay bảo trì rút hẳn khối lõi mô-tơ cũ bị cháy sém và cong vênh ra khỏi hốc trục bánh xe, rồi **thả nó rơi xuống nền bụi Mặt Trăng ngay cạnh bánh xe của 07**.
   - Khối module hỏng nảy nhẹ một nhịp thấp trọng lực làm bắn vài hạt bụi theo đường đạn đạo ngắn rồi **nằm lại vĩnh viễn trên mặt đất như một mảnh vỡ cơ khí nhỏ nhìn thấy rõ (`Debris_BurntModule`)** — dấu vết vật lý chân thực ghi nhận rằng "07 đã từng tự thay phụ tùng tại đúng địa điểm này".
3. **Khung 3 — `2.2 s – 3.4 s` (`3. SEAT NEW ORU MODULE` / Lắp Module ORU Mới Vào Ổ):**
   - Tay bảo trì lấy **hộp lõi ORU mới tinh từ Bộ Sửa Chữa Thực Địa** (khối trụ đồng sáng bóng với cuộn dây mới và mặt bích xanh xô thơm `#627360`) từ khay trượt bên hông khoang hàng và đẩy thẳng vào hốc trục của `Wheel_FL`.
   - Khi khớp nối tự định tâm sập vào ổ (`khục-tích` ở nốt `D4`), bánh xe trước-trái lập tức đứng thẳng trở lại và quay thử một vòng êm ái; trong khi đó, khối module cháy đen vừa vứt ra vẫn nằm ngay trên lớp bụi bên cạnh bánh xe.
4. **Khung 4 — `3.4 s – 4.5 s` (`4. WELD SKIN PLATE` / Hàn Tấm Ốp Vỏ Bảo Vệ Bằng Đầu Hàn Ngực):**
   - Trong khi tay bảo trì giữ tấm ốp chắn bụi màu xanh xô thơm (`R1_SagePatch` `#627360` có đinh tán đồng) và mối nối cáp áp sát lên mép vỏ trục bánh/vè trước, **chính thấu kính hàn bằng đồng ở giữa ngực 07 (`CutterOrigin`, nằm giữa các đèn pha trước)** rực sáng màu vàng kim và bắn một tia hàn hồ quang chuẩn xác dọc theo mép tấm ốp và mối cáp.
   - Các **vệt tia lửa hàn bay thẳng và ngắn theo đường đạn đạo** xuống nền bụi xám (hoàn toàn không có khói trong chân không), khâu kín tấm ốp bảo vệ. Đôi mắt CRT của 07 nở nụ cười mắt cong `^ ^` cùng hợp âm 3 nốt Ngũ cung Rê trưởng đi lên (`D4 → F#4 → A4`), sẵn sàng lăn bánh tiếp và bỏ lại khối module hỏng cũ nằm lại trên cát bụi phía sau.

---

### 2.2 Sửa Chữa Tại Căn Cứ: Khung Bảo Dưỡng — Service Gantry (`structure.service_gantry`)

![Concept art Khung Bảo Dưỡng (Service Gantry) với 07 v3 bên trong](../../Logs/concepts/service_gantry.png)

Nếu Bộ Sửa Chữa Thực Địa (`item.repair_kit`) là hộp module ORU dùng một lần cho các tình huống khẩn cấp ngoài xa, thì **Khung Bảo Dưỡng (Service Gantry)** là công trình căn cứ đầu game, rẻ và tự động đại tu toàn diện 07 về `100%` bằng điện năng của mạng lưới căn cứ ([`PowerGrid.cs`](../../Assets/_Project/Scripts/Gameplay/Building/PowerGrid.cs)).

#### Thông Số Kỹ Thuật (`StructureDefinition.cs` Compatible)

| Trường (`StructureDefinition.cs`) | Giá Trị Đề Xuất (`[PROPOSAL]`) | Lý Giải Thiết Kế |
| :--- | :--- | :--- |
| **`_id` (`StructureIds`)** | `"structure.service_gantry"` | Thêm vào [`StructureIds.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs#L12-L20) cùng với `SolarPanel`, `BatteryRack`, `Fabricator`, `RoverBay`. |
| **`_nameKey`** | `"building.service_gantry.name"` | Tên hiển thị: **Service Gantry** (*Khung Bảo Dưỡng*). |
| **Kích thước (`_width × _depth × _height`)** | **`2.8 m × 2.4 m × 2.35 m`** | Cổng vòm gọn gàng cho phép lái xuyên qua (`07 v3` rộng `1.48 m` × dài `1.49 m` × cao `1.76 m`), nhỏ gọn hơn `UpgradeBay.fbx` (`3.20 m × 2.82 m` trong [`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py)). |
| **`_maxGroundSlope` / `_clearance`** | **`15.0°` / `0.25 m`** | Đồng bộ chuẩn đặt công trình trên lưới xây dựng ([`StructureDefinition.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureDefinition.cs)). |
| **`_buildCost` (`Recipe`)** | **`3 × Metal` + `2 × Wiring` + `1 × Optics`** | Chi phí T1 rẻ và sớm — xây được ngay trong 15 phút đầu sau Tấm Pin Mặt Trời và Giá Pin. |
| **`_buildSeconds`** | **`4.0 s`** | Thời gian lắp dựng nhanh trên đế xây dựng. |
| **Điện năng (`_consumeWatts` trên `PowerGrid.cs`)** | **`120 W` (khi đang gò/hàn) / `10 W` (chờ)** | Lấy điện từ mạng lưới `SolarPanel` / `BatteryRack` (`PowerGrid.cs`). Nếu mạng điện thiếu hụt (`0 < satisfaction < 1`), tốc độ sửa tỉ lệ thuận với công suất cấp; nếu mất điện hoàn toàn (`0 W`), đèn làm việc tắt và quá trình sửa tạm dừng. |
| **`_ports` (`StructurePort[]`)** | **`2` cổng cáp:**<br>`(-1.30, 0.08, 0.0)` hướng `(-1, 0, 0)`<br>`(+1.30, 0.08, 0.0)` hướng `(+1, 0, 0)` | Hai ổ cắm cáp bằng đồng ở hai chân đế trái/phải để nối chuỗi vào lưới điện căn cứ. |
| **Tốc Độ Sửa Chữa** | **`+14% Wear / s`** (`~7.0 s` từ `1% → 100%`) | Đủ nhanh để không phải chờ lâu, đủ nhịp (`3–7 s`) để người chơi thưởng thức hoạt cảnh gò búa, cân chỉnh trục và hàn tia lửa. |

#### Nhận Diện Tức Thì & Phong Cách Retro-Future Thập Niên 1980
- **Nhìn Là Hiểu Ngay Chức Năng:** Từ khoảng cách `60 m`, Service Gantry nổi bật với **khung cổng chữ A lái xuyên qua** trên bệ sắt vát cạnh màu than (`R1_Charcoal` `#26292E`) có hai thanh dẫn hướng bánh xe kẻ sọc vàng-đen, hai trụ thép sơn kem (`R1_HullCream`), bảng màn hình CRT chẩn đoán gắn bên trụ phải (`HULL: 100%`), và **hai cánh tay công cụ cơ khí treo trên thanh ray ngang**:
  1. **Tay Trái:** **Đầu búa gò khí nén (`Planishing Hammer`) & ngàm cân chỉnh trục** hạ xuống gõ đều từng nhịp (`cốc-cốc-keng` theo nốt `D3/A3`) làm phẳng các vết móp và cân lại giảm xóc/bánh xe.
  2. **Tay Phải:** **Mỏ hàn hồ quang bằng đồng đỏ** với dây cáp xoắn màu cam khâu kín các vết nứt bằng các vệt tia lửa thẳng ngắn và tháo bỏ các tấm ốp vá tạm `R1_SagePatch`, trả lại lớp vỏ trắng bo tròn bóng đẹp nguyên bản `100%`.

---

### 2.3 Các Gói Nâng Cấp Khung Gầm Tùy Chọn Về Sau (`0.6+`)

Khi tiến vào các vùng `0.6+` hiểm trở, người chơi có thể chế tạo và lắp đặt các gói gia cố vĩnh viễn tại **Rover Bay / Upgrade Bay** ([`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py)), làm thay đổi trực quan ngoại hình của 07 v3 và nâng cao ngưỡng chịu đựng:

| Mã Kit | Tên Gọi & Cột Mốc | Công Thức (`SalvageMaterial`) | Hiệu Ứng Lên Mô Hình Hao Mòn | Thay Đổi Trực Quan Trên 07 v3 (`RoverModelNodes`) |
| :--- | :--- | :--- | :--- | :--- |
| `kit.bumper_frame` | **Cản Trước Thép Nhíp (Bullbar Bumper)** (`0.5.7` cuối / `0.6` đầu) | `4 × Metal` + `2 × Alloy` + `1 × Wiring` | Nâng ngưỡng tốc độ va chạm cứng $v_{\text{min}}$ từ **`6.8 m/s` → `8.4 m/s`**. *(Ở tốc độ hành trình chuẩn `8.0 m/s` không Boost, 07 đâm trực diện vào đá tốn **`0` độ bền**!)* | Gắn khung cản ống thép đôi màu than và nhíp đồng ngang mũi xe phía trên lưỡi cản trước (`DigScoop`) kèm hai đệm cao su giảm chấn. |
| `kit.suspension_dampers` | **Giảm Xóc Bình Khí Nitơ Rời (Nitrogen Dampers)** (`0.6`) | `2 × Alloy` + `2 × Coolant` + `2 × Wiring` | Cộng thêm **`+1.2 m/s`** vào $v_{\text{min}}$ (lên tới **`9.6 m/s`** khi đi cùng Cản Trước — miễn nhiễm cả khi bật Boost trên đường bằng!) và giảm **`-30%`** sát thương va chạm tốc độ cao. | Gắn thêm các bình dầu-khí phụ bằng đồng có cánh tản nhiệt bức xạ lên cả 4 giảm xóc (`Shock_FL..RR`). |
| `kit.hardened_hull` | **Giáp Vỏ Hợp Kim Ilmenite-Titan (Hardened Hull)** (`0.6+`) | `4 × Alloy` + `3 × Ilmenite` + `2 × OrangeBeads` | Tăng `MaxWearIntegrity` từ **`100` → `150`** (`+50%` tổng độ bền) và giảm **`-40%`** tốc độ ép của kẹp Reclaimer cũng như mưa thiên thạch vi mô. | Ốp các tấm giáp titan xước và xanh xô thơm (`R1_SagePatch` + `R1_Steel`) tán đinh đồng lên vòm bánh xe, vai hông và viền bảo vệ đầu CRT. |


---

## 3. Reclaimers: Những Cỗ Máy Thu Hồi Robot Cũ

### 3.1 Câu Chuyện & Sự Nhất Quán Với `story-v4.md`

Để khớp tuyệt đối với [`story-v4.md`](story-v4.md) (Quyết định 1–5) và [`inventions.md`](inventions.md#L211-L234) (§2.7 *Đường Phóng Mass Driver*):

- **Ai Đã Chế Tạo Chúng & Để Làm Gì (`1984–1987`):**
  Trong bốn năm nước rút của cuộc Khởi Hành (`1984–1988`) do **Mùa Đông Khói Mù / Thập Kỷ Mờ Sương** ([`story-v4.md`](story-v4.md) Quyết định 1), việc đóng các con tàu **Ark trên quỹ đạo halo tại điểm Lagrange L4/L5** ([`story-v4.md`](story-v4.md) Quyết định 4) lâm vào cơn khát vật liệu tinh luyện: các lò luyện quặng regolith thô không thể cung cấp kịp thép hợp kim titan, cuộn đồng nguyên chất và thấu kính chống bức xạ trước khi **Cơn Bão Mặt Trời tháng 10 năm 1988** ập tới.
  - **Bộ Chỉ Huy Vật Liệu Ark thuộc ILC** (`[PROPOSAL]`) đã đặt hàng **Tycho Consortium** (`[PROPOSAL]`, tập đoàn công nghiệp vận hành **Trạm Meridian tự động không người lái** trong [`story-v4.md`](story-v4.md) §0.2 & Quyết định 5) triển khai **Các Phân Đội Thu Hồi Tự Động (`ARD-01` đến `ARD-06`)**.
  - Chỉ thị của chúng (`DIRECTIVE SCRAP-04`) rất rõ ràng: tuần tra khắp các biển bazan và đường vận tải, quét mọi thiết bị bằng đầu đọc thẻ từ cảm ứng/quang học, và **thu hồi bất kỳ phương tiện, vệ tinh hay thiết bị nào không phát Mã Tín Hiệu Ưu Tiên Ark 1988 hợp lệ**. Những chiếc xe tự hành khảo sát thập niên 1970 đã ngừng hoạt động, tầng hạ cánh cũ và cáp đồng bỏ hoang đều bị kéo về **Bãi Thu Hồi (Reclamation Yard)**, phân loại vào các thùng hợp kim/quang học, ép thành thỏi tiêu chuẩn và bắn lên **Đường Phóng Mass Driver L5** để ghép vỏ tàu Ark.
- **Vì Sao Chúng Coi 07 Là Phế Liệu Vào Năm `2024` (Sau 36 Năm):**
  Khi **cơn bão mặt trời ngày 28–29/10/1988** ập xuống ([`story-v4.md`](story-v4.md) §2.2), bốn thành viên phi hành đoàn Trạm Lumen (**Ines Kalani, Kenji Morrow, Theo Lindqvist, Ro Amadi**) đã ở lại vị trí trong bộ đồ áp suất để cứu dữ liệu băng nước và cài đặt các rover sang chế độ `CONTINUE` trước khi bộ đồ cạn kiệt trong khu nhà ở đóng kín (`SUIT 01-04 · OFFLINE · 29 OCT 1988`, [`story-v4.md`](story-v4.md) Quyết định 2). Không ai kịp gửi lệnh `STOP-HARVEST` cho đoàn máy Reclaimer. Nhờ bộ nhớ lõi từ bọc gang dày chống bức xạ, các cỗ máy Reclaimer vẫn bền bỉ chạy đúng vòng lặp suốt **36 năm** dưới bầu trời Trái Đất tối đen không ánh đèn thành phố ([`story-v4.md`](story-v4.md) Quyết định 3).
  - Vì `07` là robot khảo sát hạng nhẹ của **Lumen** thức dậy vào năm `2024` mà không có Thẻ Từ Ưu Tiên Ark `1988`, bất kỳ chiếc Reclaimer nào chiếu đèn vào `07` đều đọc được:  
    `UNIT: LUMEN-07 // TRANSPONDER: EXPIRED (1988) // CLASSIFICATION: GRADE-A RECLAIMABLE ALLOY & OPTICS`.
  - Chúng không độc ác, không giận dữ, không phải quái vật. Chúng chỉ là những chiếc xe thu gom tái chế cần mẫn của thập niên 1980 đang làm nốt công việc cho một cuộc di tản đã kết thúc từ 36 năm trước.
- **Bãi Thu Hồi & Đường Phóng Mass Driver Nằm Ở Đâu:**
  - **Thung Lũng Nhà (`0.5`) hoàn toàn KHÔNG có Reclaimer** ([`docs/VISION.md` Phán quyết 17](../VISION.md#L196)): vành đai núi cao quanh vùng khởi đầu ngăn toàn bộ xe bánh xích hạng nặng đi vào.
  - **Bãi Thu Hồi Tiền Tiêu (`Vùng 2: The Long Mare` rìa phía đông, `0.6`):** Một lòng chảo bazan bậc thang nơi phân đội `ARD-04` vận hành bãi phân loại và tuyến đường sắt nhánh dẫn về **Tổ Hợp Đường Phóng Mass Driver L5 Chính (`Vùng 6: The Industrial Dark`, [`0.6-world-regions.md`](0.6-world-regions.md))**.

---

### 3.2 Hành Vi: Căng Thẳng Tĩnh Lặng, Gọng Kẹp Từ Tính & Bắt Giữ Về Bãi

Reclaimer không bao giờ bắn đạn, không rượt đuổi điên cuồng và không biến game thành hành động bắn súng. Sự đe dọa của chúng là **căng thẳng không gian cơ học chậm rãi**:

![Đèn kiểm tra của Reclaimer chiếu vũng sáng trên biển tối khi 07 ẩn mình trong bóng miệng hố](../../Logs/concepts/reclaimer_searchlight_mare.png)

1. **Tuần Tra (`Patrol` — `2.2–3.2 m/s`):**
   - Di chuyển chậm theo các tuyến đường đất nén cố định hoặc quanh các bãi xác vệ tinh lớn (hoàn toàn không tạo đám mây khói hay bụi mù trong chân không).
   - Quét **vũng sáng kiểm tra hình elip màu vàng cam sắc cạnh trên mặt đất regolith** (`28 m` với Spotter, `38 m` với Hauler) qua lại theo chu kỳ con lắc `6.0 s`, đi kèm tiếng lẫy cơ khí tích tắc đều đặn (`72 BPM`). Vì Mặt Trăng là chân không không có không khí tán xạ, **không có nón sáng hay tia sáng lơ lửng trong không gian trống** — chỉ có thấu kính đèn nóng rực trên máy và vũng sáng di chuyển nơi ánh sáng chiếu trúng mặt đất.
2. **Phát Hiện & Kiểm Tra Thẻ Từ (`Investigate` — Cửa Sổ Xác Minh `2.5 s`):**
   - Nếu 07 đi vào vũng sáng trên mặt đất — hoặc bật đèn pha (`L`) sáng trưng trong tầm nhìn `35 m` — Reclaimer dừng quét, giữ vũng sáng trên 07 và tăng nhịp lẫy lên gấp đôi (`144 BPM`) trong `2.5 s` để đọc thẻ từ.
   - **Khắc Chế Tức Thì:** Nếu 07 tắt đèn pha (`L`), lách vào vùng bóng tối sắc lẹm sau tảng đá hoặc gờ miệng hố và đứng yên trước khi hết `2.5 s`, cảm biến quang/dòng xoáy của Reclaimer mất dấu, phát tiếng ngân trầm `B2 → D2` rồi quay lại tuần tra.
3. **Tiếp Cận & Kẹp Nam Châm Điện (`Approach & Clamp` — `4.5–6.0 m/s`):**
   - Khi xác nhận `UNTAGGED SCRAP`, Reclaimer cấp điện cho **gọng kẹp nam châm điện quấn đồng** phía trước rồi tiến về phía 07.
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

> [!IMPORTANT]
> **Quy Tắc Vật Lý Một Câu Khi Tắt Nguồn Trong Kẹp Reclaimer ("Phân Loại Tại Bãi Thu Hồi"):**  
> Nếu độ bền của 07 chạm `0%` trong gọng kẹp của Reclaimer, **cỗ máy Reclaimer sẽ chở 07 về Bãi Thu Hồi và đổ Hộp Hàng Báo Hiệu (Cargo Beacon Pod) của 07 vào `SORTING BIN 03: UNTAGGED UNITS` (Thùng Phân Loại Số 03), trong khi Tilly (`friend.tilly`) kéo chính khung xe đã nhẹ tải của 07 về Khung Bảo Dưỡng / Trạm Xe gần nhất để gò phẳng và hàn lại.**
> - **Vì Sao Quy Tắc Này Rõ Ràng & Tuyệt Vời:**
>   1. **Không trộn lẫn nhiều khái niệm:** Không có sao lưu bộ nhớ, không có khung xe dự phòng, không có xác xe thứ hai — tuân thủ đúng **cùng một quy tắc kéo xe vật lý duy nhất** như khi đâm hỏng ngoài thực địa (§1.6.B), chỉ khác là chiếc Reclaimer đã chở Hộp Hàng Báo Hiệu của bạn về tận `Thùng Phân Loại Số 03` tại Bãi Thu Hồi!
>   2. **Phần thưởng khám phá hấp dẫn:** Hộp Hàng Báo Hiệu nằm phát sáng ấm áp trong `Thùng 03` (đánh dấu vị trí Bãi Thu Hồi lên la bàn) ngay cạnh **những đống phế liệu T2 quý giá (`Alloy`, `CircuitChips`, `Optics`)** mà đoàn Reclaimer gom từ các xác tàu năm 1988, biến tai nạn bị bắt thành một chuyến đột nhập bãi phế liệu đầy phần thưởng.

![Bãi Thu Hồi (Reclamation Yard) lúc chập tối trên Mặt Trăng](../../Logs/concepts/reclamation_yard_dusk.png)

---

### 3.3 Vòng Cung Tiến Triển: Khắc Chế → Thấu Hiểu → Đồng Minh

Mọi mối nguy trong *Lunarion* đều đi theo vòng cung làm chủ không bạo lực gồm 4 bước:

| Bước | Hành Động Của Người Chơi | Phần Thưởng Cơ Chế & Câu Chuyện |
| :--- | :--- | :--- |
| **1. Khắc Chế (Ẩn Nấp & Kỷ Luật Ánh Sáng)** | - Tắt đèn pha (`L`) khi thấy vũng sáng cam quét nơi chân trời.<br>- Tận dụng gờ miệng hố, tảng đá bazan và bóng tối sắc nét để tránh vũng sáng trên mặt đất.<br>- Nếu lỡ bị rìa vũng sáng quét qua trong bóng râm, **đứng im hoàn toàn** (cảm biến dòng xoáy bỏ qua vật thể tĩnh).<br>- Nếu bị kẹp, dùng **xung nhảy `Space` + lắc `A/D`** để bung khóa từ. | Tạo chiều sâu lén lút và hồi hộp ngay từ đầu trên vùng Long Mare mà chưa cần trang bị mới. |
| **2. Thấu Hiểu (Quét Một Cỗ Máy Hỏng/Ngủ Đông)** | - Ở rìa tây Long Mare (hoặc gần lối vào Bãi), 07 tìm thấy một chiếc **`SHEAR-85` Spotter bị đá lở đè hỏng** với chiếc đèn kiểm tra vẫn tích tắc yếu ớt (`D4`).<br>- Quét (`E`) **Bộ Đọc Thẻ Từ Lõi Ferit** bị lộ ra sẽ mở khóa mục từ điển *"Chỉ Thị Ark 1985 SCRAP-04"* và bản vẽ **Thẻ Từ Thông Hành Ark**. | Giải thích trọn vẹn câu chuyện về việc xây tàu Ark L4/L5 và lý do vì sao Reclaimer lại bắt 07. |
| **3. Miễn Nhiễm (Chế Tạo Thẻ Từ Ark)** | - Chế tạo **Thẻ Từ Thông Hành Ark (`kit.ark_transponder`)** tại Fabricator:<br>  **Công thức:** `1 × CircuitChips` + `2 × Wiring` + `1 × Optics` (`T2`, `0.6`).<br>- Gắn một hộp ăng-ten lưỡng cực bằng đồng và xanh xô thơm lên cột `Antenna` của 07. | Khi ánh đèn Reclaimer quét qua 07, đầu đọc của nó ngân lên hai nốt hòa âm ấm áp (`D4 → A4`), nháy đèn xác nhận (`TAG VALID: ARK MAINT. 07`) và **bỏ qua 07 hoàn toàn** — cho phép 07 đường hoàng đi dạo giữa Bãi Thu Hồi! |
| **4. Đồng Minh & Tự Động Hóa (Lập Trình Lại Reclaimer)** | - Ở giữa/cuối `0.6`, chế tạo **Băng Từ Ghi Đè Tọa Độ Nhà (`item.reclaimer_override`)**:<br>  **Công thức:** `2 × CircuitChips` + `1 × Crystal` + `2 × Wiring`.<br>- Lái tới sau lưng một chiếc Heavy Hauler (khi đã đeo Thẻ Từ), mở nắp bảo trì băng từ thập niên 1980 bằng tia hàn/giao tiếp của 07 và cắm băng ghi đè vào. | Đổi tọa độ trả hàng của chiếc Hauler từ `YARD BIN 01` sang **Bãi Chứa Hàng Căn Cứ của 07 (`M3-25`)**. Chiếc Hauler đồng minh sẽ tự động tuần tra ngoài biển bazan và chở **`4–6` đơn vị `Metal`, `Wiring`, `Ilmenite` hoặc `Alloy`** về tận căn cứ cho 07 mỗi ngày Mặt Trăng! |

---

### 3.4 Ba Loại Máy Reclaimer & Kiểm Định Luật Hình Khối Chân Không (`vacuum-shapes.md`, `[DECIDED] — Giữ Cả Ba`)

![Bảng thiết kế 3 loại máy Reclaimer và tỷ lệ so với 07 v3](../../Logs/concepts/reclaimer_types_sheet.png)

Cả ba loại máy Reclaimer đều đã được **Chủ dự án chốt giữ lại (`[DECIDED]`)** và mang ngôn ngữ công nghiệp nặng `1984–1987` của **Tycho / Bộ Chỉ Huy Vật Liệu Ark**: vỏ thép đúc sơn vàng mù tạt phai màu (`#C88A32`) phối sắt xám than (`#26292E`), sọc cảnh báo vàng-đen, cuộn nam châm đồng đỏ trần (`#B86B35`) và thấu kính đèn pha cửa chớp chiếu vũng sáng sắc cạnh xuống mặt đất mà không tạo nón sáng trong chân không.

#### Loại A: `SHEAR-85` Nimble Spotter ("Chó Săn Thẻ Từ / Tag-Hound", `[DECIDED]`)
- **Vai trò:** Trinh sát cơ động tuần tra các sống đồi, soi đèn phát hiện phế liệu chưa gắn thẻ và phóng dây tời từ tính giữ chân mục tiêu cho Hauler.
- **Kích thước & Di chuyển:** Dài `2.2 m` × rộng `1.6 m` × cao `1.8 m` (nhỉnh hơn `07 v3` một chút). Tốc độ tuần tra `3.2 m/s` / đuổi bắt `6.5 m/s` trên **4 chân nhện thủy lực gắn bánh lăn thép có gờ** (vừa lăn nhanh trên đá phẳng vừa bước qua gờ miệng hố và xoay `360°` tại chỗ).
- **Công cụ & Hình khối:** Tháp xoay trên lưng mang **thấu kính đèn pha kiểm tra cửa chớp màu cam-đỏ**, chảo ăng-ten đọc thẻ từ S-band và **tời cáp nam châm điện** gắn trên cần thép vươn phía trước (`8 m`).

#### Loại B: `BALER-IV` Heavy Clamp-Hauler ("Xe Ủi Kẹp Phế Liệu", `[DECIDED]`)
- **Vai trò:** Xe ủi thu hồi hạng nặng chậm nhưng không gì cản nổi, chuyên kẹp và kéo xác xe tự hành hoặc khung vệ tinh về Bãi Thu Hồi.
- **Kích thước & Di chuyển:** Dài `4.8 m` × rộng `3.2 m` × cao `3.0 m` (nặng gấp `~3×` 07). Bò chậm `2.2 m/s` khi tuần tra / `4.5 m/s` khi tiếp cận trên **4 cụm bánh xích thép độc lập** để lại vết xích đôi sâu trên bụi Mặt Trăng (không có khói hay bụi mù).
- **Công cụ & Hình khối:** Phía trước là **hai gọng kẹp nam châm điện hình móng ngựa quấn đồng đỏ** khổng lồ trên cần thủy lực cùng thấu kính đèn pha trên nóc cabin; phía sau là **thùng ben chở phế liệu** chất đầy khung giàn vệ tinh; đuôi xe có nắp cắm băng từ để 07 lập trình lại ở Bước 4.

#### Loại C: `GOLIATH-L5` Yard Gantry-Crane & Jaw-Crusher (Công Trình Điểm Nhấn Chân Trời, `[DECIDED]`)
- **Vai trò:** Cần cẩu giàn và hàm nghiền từ tính khổng lồ chạy trên đường ray tại trung tâm **Bãi Thu Hồi**, nhìn thấy từ khoảng cách hàng cây số trên biển Long Mare.
- **Kích thước & Di chuyển:** Cao `14.0 m` × nhịp ngang `18.0 m`, trượt dọc **đường ray sắt khổ rộng** (`1.0 m/s`) bắc ngang qua ba **Thùng Phân Loại (`BIN 01`, `BIN 02`, `BIN 03`)**.
- **Công cụ & Hình khối:** Treo một **mâm nam châm điện nâng hàng đường kính `3.0 m`** bằng cáp thép bện và hàm kẹp thủy lực, phía trên gắn các bóng đèn hiệu màu hổ phách.

#### Kiểm Định Bắt Buộc 6 Câu Hỏi Hình Khối Chân Không ([`vacuum-shapes.md`](vacuum-shapes.md))

| Cỗ Máy | 1. Đứng/nổi bằng gì? | 2. Đổi hướng bằng gì? | 3. Tản nhiệt bằng gì? | 4. Vì sao có hình dáng này? | 5. Có chi tiết khí quyển hay nón sáng bị cấm không? | 6. Đặt cạnh *2001* / Apollo có hợp không? |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| **`SHEAR-85` Spotter** | 4 chân thủy lực gắn bánh thép trên nền regolith. | Vi sai mô-men bánh xe + vành răng tháp xoay. | Các lá đồng bức xạ nhiệt trên lưng (không quạt gió). | Khung 4 chân trọng tâm thấp để bước qua đá tảng miệng hố. | **KHÔNG** (0 cánh quạt, 0 cánh bay, 0 vây gió, 0 nón sáng trong không gian). | **CÓ** (Lunokhod + Apollo LRV + máy xúc 4 chân). |
| **`BALER-IV` Hauler** | 4 cụm bánh xích thép chịu tải dưới trọng lực `1/6 g`. | Lái trượt vi sai hai bên xích (skid-steer). | Tấm tản nhiệt bức xạ sơn đen mờ phía đuôi (`R1_Charcoal`). | Xe ủi + thùng ben + hai gọng nam châm điện móng ngựa. | **KHÔNG** (0 ống khói, 0 quạt làm mát, 0 nón sáng trong không gian). | **CÓ** (Máy ủi mỏ lộ thiên thập niên 1980). |
| **`GOLIATH-L5` Yard Crane** | Bánh sắt có gờ chạy trên tà vẹt đường ray đá bazan. | Chạy dọc ray + tời cáp ngang trên dầm. | Khung giàn thép tự bức xạ nhiệt vào bầu trời đen `3 K`. | Cẩu trục bắc qua 3 thùng phân loại tại đầu nạp Mass Driver. | **KHÔNG** (0 chong chóng gió, 0 tháp giải nhiệt, 0 cột sáng đứng). | **CÓ** (Cẩu trục cảng/mỏ công nghiệp nặng). |

---

### 3.5 Ngôn Ngữ Âm Thanh (Ngũ Cung Rê Trưởng: `D E F# A B`)

Tuân thủ tuyệt đối định hướng âm nhạc của *Lunarion* ([`docs/VISION.md`](../VISION.md) Trụ cột 5), Reclaimers không bao giờ phát tiếng còi báo động chói tai hay âm thanh kinh dị nghịch tai. Toàn bộ âm thanh của chúng đều trầm hùng, cơ khí và nằm gọn trong **âm giai Ngũ cung Rê trưởng (`D E F# A B`)**:
1. **Tiếng Động Cơ Trầm (`D1 + A1` Quãng Năm Đúng):** Tiếng ngân sub-bass ấm (`36.7 Hz` `D1` + `55.0 Hz` `A1`) của động cơ điện một chiều cổ góp và mắt xích thép truyền qua nền đá bazan khi bánh xe 07 chạm cùng mặt đất.
2. **Tiếng Lẫy Quét Đèn Kiểm Tra (`F#4` Metronome):** Khi đèn cửa chớp quét qua lại, lẫy cơ khí gõ nhịp ở nốt **`F#4` (`370 Hz`)** tốc độ `72 BPM`; khi phát hiện 07 (`Investigate`), nhịp tăng gấp đôi lên `144 BPM` luân phiên hai nốt **`F#4 – A4`**.
3. **Tiếng Sập Kẹp Từ & Ngân Dòng Xoáy (`D2` + `B2 → E3`):** Tiếng biến áp ngân từ `B2` lên `E3` rồi kết thúc bằng tiếng sập đồng-sắt trầm đục ở nốt **`D2`**.
4. **Tiếng Chuông Chào Thẻ Từ Hợp Lệ (`D4 → F#4 → A4`):** Khi 07 đã gắn **Thẻ Từ Ark**, mỗi lần đèn Reclaimer quét qua sẽ ngân lên ba nốt hợp âm Rê trưởng ấm áp (`D4 – F#4 – A4`) như một lời chào đồng nghiệp giữa biển đêm.

---

## 4. Nguy Hiểm Tự Nhiên Theo Vùng Miền (`0.6+`, 1–2 Dòng Mỗi Loại)

Mỗi vùng thế giới ở `0.6+` ([`0.6-world-regions.md`](0.6-world-regions.md)) có một **hiện tượng vật lý Mặt Trăng đặc trưng** gây hao mòn độ bền nếu đi vào mà không chuẩn bị — và mỗi hiện tượng đều có một **thiết bị bảo vệ chế tạo được** đóng vai trò là **chìa khóa mở vùng (region gate)** tự nhiên:

| Vùng (`0.6-world-regions.md`) | Hiện Tượng Mặt Trăng & Tốc Độ Hao Mòn (1–2 Dòng) | Thiết Bị Bảo Vệ Chế Tạo & Chìa Khóa Mở Vùng (`SalvageMaterial`) |
| :--- | :--- | :--- |
| **1. Biển Dài (`Vùng 2: The Long Mare`) — Mưa Thiên Thạch Vi Mô** | Các đợt mưa hạt bụi siêu vận tốc lao xuống từ bầu trời đen không khí quyển, bắn tia lửa nhỏ lấp lánh trên mặt đồng bằng trống trải (`-4% Wear/hạt` trừ khi 07 trú dưới mái Gantry, gầm vách đá hoặc vòm xác tàu). | **Mái Chắn Kép Whipple (`kit.whipple_shield`):** Chế từ `3 × Alloy + 2 × Metal`. Gắn tấm chắn hai lớp đồng-xanh xô thơm phía trên đầu và vai 07, giảm sát thương thiên thạch vi mô về **`0%`**. |
| **2. Vành Đai Biển & Đường Ranh Giới Ngày-Đêm (Terminator) — Hạt Bụi Tĩnh Điện Nhảy Đạn Đạo** | Dọc ranh giới sáng-tối, tia UV mặt trời tích điện làm các hạt silicat sắc nhọn nảy tĩnh điện cao tới `1.5 m`, bám vào khớp trục và mài mòn ổ bi bánh xe (`-2.5% Wear/s` khi phóng nhanh qua vùng ranh giới). | **Chổi Tiếp Địa Đồng & Gioăng Chắn Bụi (`kit.grounding_whiskers`):** Chế từ `3 × Wiring + 2 × OrangeBeads`. Chùm dây đồng bện thả chạm đất sau cản xe giúp xả sạch tĩnh điện và triệt tiêu mài mòn bụi. |
| **3. Cánh Đồng Thủy Tinh (`Vùng 3: The Glass Field`) — Mảnh Thủy Tinh Núi Lửa & Sốc Nhiệt Khe Nứt** | Những cánh đồng hạt thủy tinh cam (`OrangeBeads`) và dải đá nóng chảy sắc như dao cứa rách lốp cao su thường khi chạy trên `5.0 m/s` (`-3% Wear/s`), kèm các khe thoát khí gây sốc nhiệt. | **Lốp Lưới Thép-Ilmenite Thiêu Kết (`kit.sintered_treads`):** Chế từ `4 × Ilmenite + 2 × Glass + 2 × Metal`. Thay lốp cao su bằng lốp lưới hợp kim đan (như xe Apollo LRV), miễn nhiễm hoàn toàn mảnh thủy tinh sắc. |
| **4. Cực Nam (`Vùng 5: The South Pole` — Đáy Hố Shackleton) — Lạnh Cực Sâu Vĩnh Cửu (`40 K`)** | Trong các đáy hố tối vĩnh cửu không thấy ánh mặt trời suốt 2 tỷ năm, nhiệt độ `40 K` làm thép vỏ giòn gấp đôi (`2.0×` sát thương va chạm) và làm đông cứng phớt cơ khí (`-2.0% Wear/s` sau `20 s` ở trong bóng tối vĩnh cửu). | **Áo Tuần Hoàn Nhiệt Khép Kín (`kit.cryo_heater`):** Chế từ `3 × Coolant + 2 × OrangeBeads + 2 × Alloy`. Dẫn chất lỏng ấm từ cụm pin qua ống đồng quanh thân `Body`, giữ 07 ấm áp và miễn nhiễm lạnh sâu. |
| **5. Khu Công Nghiệp Tối (`Vùng 6: The Industrial Dark`) — Rò Rỉ Nhiên Liệu Đóng Băng & Hồ Quang Cảm Ứng** | Đường ống nhiên liệu vỡ và tụ điện Đường Phóng Mass Driver rò rỉ muối hydrazine ăn mòn cùng hồ quang điện áp cao trên sàn nhà máy (`-5.0% Wear/s` khi đi qua vùng rò rỉ). | **Lớp Phủ Gầm Cách Điện Fluoropolymer (`kit.chemical_seal`):** Chế từ `2 × Superconductor + 2 × Coolant + 2 × Glass`. Bọc kín gầm xe và trục bánh, chống ăn mòn hóa học và phóng điện đường ray. |

---

## 5. Phân Kỳ Triển Khai (Phasing), Miền Code & Kiểm Thử Tự Động (Chỉ Thiết Kế — Chờ Qua Cổng Lõi Cảm Giác)

> [!NOTE]
> **Cổng Lõi Cảm Giác (`docs/ROADMAP.md`, "Cổng lõi cảm giác"):** Toàn bộ tính năng này hiện ở trạng thái **chỉ thiết kế (design only)**. Chưa triển khai code hay asset cho tới khi dự án vượt qua **Cổng lõi cảm giác** trong [`docs/ROADMAP.md`](../ROADMAP.md) (*"Cổng lõi cảm giác"*).

### 5.1 Phân Kỳ Theo Cột Mốc (Sau Khi Qua Cổng Lõi Cảm Giác)

- **Giai đoạn A (`0.5.7` — Phát hành cùng Xây Dựng Căn Cứ):**
  1. Mô hình Hao mòn (Wear): toán va chạm vận tốc pháp tuyến $v_\perp \ge 6.8\text{ m/s}$, phân loại bề mặt cứng vs. regolith, tiếng cót két cảnh báo (`6.5 m/s+`), và **4 cột mốc hỏng hóc cụm bộ phận leo thang (`75% / 50% / 25% / 10%`)** bao gồm gập ăng-ten, vỡ đèn pha trái + sập phuộc `Shock_FL`, khóa cứng bánh trước-trái `Wheel_FL` cày rãnh bụi (giới hạn tốc độ `5.6 m/s`), và xé ngang màn hình CRT + đánh lửa hụt cuộn nhảy (`4.4 m/s`).
  2. Luồng Tắt nguồn & Khởi động lại: hoạt cảnh CRT thu thành đường chỉ ngang (`R1CrtFace.shader`), nhả & thu hồi **Cargo Beacon Pod** (`M3-25`), Tilly kéo khung xe về Service Gantry / Rover Bay (`[DECIDED]`).
  3. Sửa chữa vật lý: **Bộ Sửa Chữa Thực Địa (`item.repair_kit`)** thay thế module ORU dùng 1 lần trong `4.5 s` (tháo chốt module hỏng $\to$ thả rơi thành mảnh vỡ trên đất $\to$ lắp module mới $\to$ hàn tấm ốp vỏ bảo vệ bằng `CutterOrigin`) + công trình **Khung Bảo Dưỡng (`structure.service_gantry`)** đại tu toàn diện bằng điện căn cứ (`PowerGrid.cs`).
  4. Chế độ chơi mới: **Survival** (mặc định) và **`"Chỉ khám phá"` / `"Explore only"`** (không hao mòn, không chết); chế độ **Hardcore** một mạng để dành làm tùy chọn ở bản `1.0`.
- **Giai đoạn B (`0.6+` — Phát hành cùng Các Vùng Thế Giới):**
  1. Cả ba loại máy **Reclaimer** (`SHEAR-85` Spotter, `BALER-IV` Hauler, `GOLIATH-L5` Yard Crane) + **Bãi Thu Hồi (Reclamation Yard)** & Thùng Phân Loại Số 03.
  2. Vòng cung làm chủ: quét máy hỏng → chế **Thẻ Từ Ark (`kit.ark_transponder`)** → chế **Băng Ghi Đè (`item.reclaimer_override`)** biến Hauler thành đồng minh chở phế liệu về căn cứ.
  3. Năm **Nguy hiểm vùng miền** + các gói nâng cấp khung gầm & thiết bị bảo hộ mở vùng.

### 5.2 Các Miền Code Liên Quan & Bộ Kiểm Thử Dự Kiến (`EditMode` / `PlayMode`)

| Miền / Assembly | File Hiện Có Tham Chiếu | File / Kiểu Dự Kiến Khi Triển Khai |
| :--- | :--- | :--- |
| **`Core` (`Project.Core`)** | [`SalvageMaterial.cs`](../../Assets/_Project/Scripts/Core/SalvageMaterial.cs) | `GameMode.cs` (`Survival`, `ExploreOnly`), `WearState.cs` (struct lưu trữ 4 cột mốc hỏng hóc & hàm toán va chạm thuần C#). |
| **`Rover` (`Project.Rover`)** | [`RoverController.cs`](../../Assets/_Project/Scripts/Rover/RoverController.cs), [`DriveSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/DriveSettings.cs), [`BoostSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/BoostSettings.cs), [`HoverJumpSettings.cs`](../../Assets/_Project/Scripts/Rover/Tuning/HoverJumpSettings.cs), [`FaceDisplay.cs`](../../Assets/_Project/Scripts/Rover/Face/FaceDisplay.cs), [`RoverModelNodes.cs`](../../Assets/_Project/Scripts/Rover/Rig/RoverModelNodes.cs) | `WearSettings.cs`, `RoverWearController.cs`, `RepairKitModuleSwapApplicator.cs` (điều khiển hoạt cảnh 4 bước thay module ORU dài `4.5 s` bằng tay bảo trì sườn + `CutterOrigin` và sinh mảnh vỡ `Debris_BurntModule`). |
| **`Gameplay` (`Project.Gameplay`)** | [`StructureDefinition.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureDefinition.cs), [`StructureIds.cs`](../../Assets/_Project/Scripts/Gameplay/Building/StructureIds.cs), [`PowerGrid.cs`](../../Assets/_Project/Scripts/Gameplay/Building/PowerGrid.cs) | `StructureIds.ServiceGantry`, `ServiceGantryController.cs`, `CargoBeaconPod.cs`, `RespawnCoordinator.cs`. |
| **`World` (`Project.World` — `0.6+`)** | `WorldRegions`, `SalvageNode` | `ReclaimerController.cs`, `ReclaimerSearchlight.cs`, `ReclamationYardBin.cs`, `RegionalHazardZone.cs`. |
| **`UI` (`Project.UI`)** | [`CellRowSettings.cs`](../../Assets/_Project/Scripts/UI/Tuning/CellRowSettings.cs) | `WearGaugeView.cs` (thanh 5 đốt cạnh thanh pin, tự ẩn khi `100%` hoặc ở chế độ `"Chỉ khám phá"` / `"Explore only"`), `MagneticSlipPromptView.cs`. |
| **`Art & Shaders`** | [`R1CrtFace.shader`](../../Assets/_Project/Shaders/R1/R1CrtFace.shader), [`build_rover.py`](../../tools/art/blender/rover/build_rover.py), [`upgrade_bay.py`](../../tools/art/blender/upgrade_bay.py) | Biến dạng 4 cột mốc hỏng hóc, tay bảo trì nhỏ + lõi ORU + mảnh vỡ `Debris_BurntModule` + tấm ốp `R1_SagePatch`, `ServiceGantry.fbx`, `CargoBeacon.fbx`, các model Reclaimer (`0.6+`). |

**Danh sách Kiểm thử Tự động bắt buộc khi triển khai (`EditMode` & `PlayMode`):**
- `RoverWearMathTests.cs` (EditMode): Kiểm tra $v_\perp < 6.8\text{ m/s} \to 0$ hao mòn; quệt góc hẹp $20^\circ$ ở tốc độ `8.0 m/s` $\to 0$ hao mòn; va chạm lên `Regolith/Terrain` ở mọi tốc độ $\to 0$ hao mòn; đường cong hàm mũ chính xác từ `6.8` đến `12.0 m/s` (trần `40%`); cửa sổ miễn nhiễm `1.0 s` sau va chạm.
- `RoverWearMilestoneTests.cs` (EditMode): Kiểm tra kích hoạt đúng 4 cột mốc hỏng hóc tại `75%`, `50%`, `25%` và `10%`; kiểm tra mốc `50%` giảm tốc độ còn `7.2 m/s` và cua trái `0.82×`; kiểm tra mốc `25%` khóa `Wheel_FL`, giới hạn tốc độ ở `5.6 m/s`, khóa Boost và tạo lực kéo lệch trái `8.5°/s`; kiểm tra mốc `10%` giới hạn tốc độ ở `4.4 m/s`, làm cuộn nhảy đánh lửa hụt `0.35 m` và nhân `1.6×` thời gian quét; kiểm tra dùng 1 `item.repair_kit` tiêu hao đúng 1 bộ, sửa cụm bộ phận hỏng nặng nhất hiện tại và nâng độ bền lên ngưỡng cột mốc trước đó (`10–1% → 25%`, `25–11% → 50%`, `50–26% → 75%`, `75–51% → 90%`).
- `RoverDeathAndBeaconPlayModeTests.cs` (PlayMode): Kiểm tra khi độ bền về `0%` sẽ kích hoạt thu hẹp màn hình CRT (`ScreenVerticalPinch = 1.0`), sinh `CargoBeaconPod` chứa đúng số hàng trong khoang, đưa 07 về `ServiceGantry` gần nhất với `100%` độ bền và giữ nguyên kit, sau đó nhặt lại Beacon trả đủ hàng về khoang.
- `ExploreOnlyModeTests.cs` (EditMode & PlayMode): Kiểm tra trong `GameMode.ExploreOnly` (`"Chỉ khám phá"` / `"Explore only"`), đâm vào đá ở `12.0 m/s` hoặc đứng trong vùng nguy hiểm luôn giữ `WearIntegrity == 100%` và ẩn thanh HUD.

---

## 6. Rủi Ro & Lời Giải

1. **Rủi ro gây ức chế và mất thời gian (Frustration & Lost Time):**
   - *Lời giải:* (a) Không bao giờ mất kit, công trình hay tiến trình cốt truyện; (b) 100% các cú nhảy và tiếp đất trên bụi regolith đều tốn `0` độ bền; (c) Cột mốc `25%` (khóa bánh xe) tự giới hạn tốc độ ở `5.6 m/s` (dưới ngưỡng sát thương `6.8 m/s`) nên 07 không thể chết oan vì va đá nhỏ trên đường bằng khi đang lê bước về nhà; (d) **Service Gantry** cực rẻ (`3 Metal + 2 Wiring + 1 Optics`), khuyến khích người chơi xây tại mọi trạm tiền tiêu để rút ngắn quãng đường hồi sinh xuống dưới `60–90 giây`.
2. **Rủi ro phá vỡ Trụ cột Cô Đơn (`Pillar 6 — "Alone, and at peace"`):**
   - *Lời giải:* Thung lũng khởi đầu (`0.5`) tuyệt đối không có Reclaimer. Ở các vùng `0.6+`, Reclaimers không phải kẻ thù hiếu chiến mà là những cỗ máy công trường thập niên 1980 chậm rãi (`2.2–3.2 m/s`) đi theo lộ trình cố định với vũng sáng vàng ấm trên mặt đất và tiếng động cơ ngũ cung trầm tĩnh. Ngay khi chế được **Thẻ Từ Ark**, chúng trở thành những người hàng xóm hiền lành và thậm chí có thể được lập trình lại để chở phế liệu về xây căn cứ cho 07.
3. **Rủi ro làm mất lòng người chơi thích sự thư giãn thuần túy → Chế độ `"Chỉ khám phá"` / `"Explore only"`:**
   - *Lời giải:* Chế độ **`"Chỉ khám phá"` (`"Explore only"`)** được đặt trang trọng ngay tại màn hình tạo game mới. Người chơi chọn `"Chỉ khám phá"` vẫn được trải nghiệm trọn vẹn cốt truyện, quét tìm hiểu Reclaimer, dạo chơi trong Bãi Thu Hồi và lập trình đồng minh mà không bao giờ phải lo lắng về móp vỏ hay tắt nguồn.
4. **Bài học từ *Subnautica* và những điểm *Lunarion* cải tiến vượt trội hơn:**
   - *Giữ lại tinh hoa:* Cảm giác ấm áp khi đưa chiếc tàu trầy xước về trạm sửa chữa (*Moonpool* $\to$ **Service Gantry**) và sự hồi hộp không cần súng đạn trước những thực thể lớn có quy luật rõ ràng.
   - *Cải tiến vượt bậc:* (1) Không nhảy ra khỏi xe cầm súng bắn tia sửa chữa vô lý — 07 tự dùng tay bảo trì sườn rút bỏ module cháy ra đất, lắp lõi ORU mới và dùng chính đầu hàn ngực (`CutterOrigin`) hàn kín tấm ốp bảo vệ; (2) Không mất vĩnh viễn phương tiện hay module nâng cấp khi chết — Tilly luôn kéo khung xe 07 về Gantry nguyên vẹn; (3) Biến mối nguy thành **đồng minh tự động hóa** ở cuối vòng cung thay vì để chúng mãi là kẻ quấy rối; (4) Nếu bị Reclaimer bắt tới cạn độ bền, Hộp Hàng Báo Hiệu của bạn được đổ gọn vào **Thùng Phân Loại Số 03** tại Bãi Thu Hồi cạnh một kho báu phế liệu T2 đang chờ bạn tới lấy lại!

