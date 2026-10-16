# Lunarion — Mặt Trăng Sống Động v2: Bầy Robot Bay, Khổng Lồ Ngủ Yên & Kỳ Quan Địa Chất (`0.6–1.0`)

> **Trạng thái:** Tài liệu Thiết kế (`box/agy-swarms`). Mở rộng và cập nhật `docs/design/0.6-living-moon.md` cùng `docs/design/0.6-world-regions.md`, đồng bộ hoàn toàn với `docs/design/north-star.md`, `docs/VISION.md`, cốt truyện đã khóa `docs/design/story-v4.md` ("Cuộc rời đi"), và hệ thống phát minh thập niên 1980 tại `docs/design/inventions.md`. Bản tiếng Anh: [`docs/design/living-moon-v2.md`](living-moon-v2.md). Concept art: `Logs/concepts/*.png`. Trang duyệt trực quan: `Logs/swarms/index.html`.
>
> **Quy ước nhãn (`RULES.md` §"Facts and creativity"):**
> - **Sự thật đã có trong mã nguồn & tài liệu** được trích dẫn chính xác đường dẫn tệp và số dòng (`Assets/_Project/...`, `docs/...`).
> - **Các quyết định cốt truyện Story v4 & quyết định của Owner đã khóa** được gắn nhãn **`[DECIDED]`** (`story-v4.md:40-57`; `0.6-living-moon.md:100-113`).
> - **Mọi loài bầy đàn mới, cơ chế khổng lồ (leviathan), kỳ quan địa chất và lịch bất ngờ** trong tài liệu này đều được gắn nhãn **`[PROPOSAL]`**.

---

## 0. Trải Nghiệm Người Chơi (`north-star.md` §2 & Danh mục 10 điểm §5)

| Câu hỏi | Câu trả lời của Mặt Trăng Sống Động v2 (`[PROPOSAL]`) |
|---|---|
| **Hook (Thu hút)** | Khi bình minh Mặt Trăng ló dạng, lớp bụi nơi đường chân trời bừng sáng—và **60 tàu lượn cánh lưới vàng nhỏ xíu (`Sơn ca bụi / Dust Larks`)** vút lên từ màn sương tĩnh điện, lượn thành vòng số 8 tĩnh lặng ngang qua Trái Đất tối đen không ánh đèn thành phố (`story-v4.md:50-54`, `[DECIDED]`). Mặt Trăng không chỉ có đá xám và những cỗ máy nằm im; bầu trời nơi đây có cả một hệ sinh thái cơ khí. |
| **Pull (Lôi cuốn)** | Mỗi bầy robot đều **đang thu thập một thứ gì đó vì một mục đích rõ ràng** và bay **về một công trình tổ**: *Đàn Sơn ca bụi trút bụi giàu Heli-3 ở đâu? Tốp Én phụt kia đang chở mẫu lõi khoan hay cuộn băng nào? Bóng hình sáu chân cao 40 mét đứng cạnh Đường Ray Phóng Quặng nơi chân trời kia là gì?* |
| **Hold (Giữ chân)** | **Những gì bạn khôi phục sẽ đón bầy đàn về trú ngụ.** Sửa một **Tháp Chuồng (Roost Tower)** ở căn cứ, cả đàn Sơn ca bụi sẽ nhận căn cứ làm nhà mỗi sáng, để lại nguồn `RareEarths / Ilmenite` (`SalvageMaterial.cs:26, 32`). Bật đèn pha của 07 hoặc khôi phục **Nhà Kính (Glasshouse)** (`inventions.md` §2.3), đàn **Bướm đèn (Lantern Moths)** sẽ tụ lại như đom đóm ấm áp. Bắn một **Đạn Cầu Lửa (Ember Shell)** (`inventions.md` §2.2) vào Hố Bóng Tối, đàn **Sứa khói băng (Plume Drifters)** sẽ bung nở giữa cột hơi nước bốc cao. |
| **Finish (Đích đến)** | Ở giai đoạn cuối (`0.9–1.0`), 07 bắc cầu nguồn điện và tín hiệu cho **Kẻ Bộ Hành (Regolith Strider) cao 40 m** đang ngủ yên—đánh thức người khổng lồ hiền lành từng nạp quặng cho **Đường Ray Điện Từ L5** (`inventions.md` §2.7) để nó chậm rãi bước qua Biển Dài, chở giá đỡ siêu dẫn và lõi băng tới chân Đỉnh (The Peak). |
| **Feeling (Cảm xúc)** | **Nỗi cô đơn kỳ vĩ điểm xuyết những khoảnh khắc choáng ngợp.** Sự tĩnh lặng vẫn là nền tảng (`docs/VISION.md:61-65` Pillar 6), nhờ vậy mỗi lần rẽ qua vách đá bắt gặp cánh đồng xoáy từ phát sáng, tổ ong thủy tinh cam hay vũ điệu bình minh đều là một bất ngờ thật sự. |

- **Học từ Subnautica, rồi làm tốt hơn (`north-star.md` §1, §3):** Cá đàn, cá đuối phát sáng và Reefback Leviathan của *Subnautica* khiến đại dương sống động ở cả 3 tầng tỷ lệ: đàn nhỏ quanh người chơi, loài chuyên biệt theo vùng, và sinh vật khổng lồ nơi chân trời. Chúng ta đưa **hệ sinh thái 3 tầng tỷ lệ (bầy nhỏ · đàn thu hoạch · khổng lồ chân trời)** lên Mặt Trăng không khí quyển bằng **vật lý chân không thập niên 1980 hoàn toàn hợp lý** và **tuyệt đối không thù địch**.
- **Tín hiệu kiểm thử (Playtest Signals, `north-star.md` §4):**
  1. Ở bình minh đầu tiên, người chơi dừng xe `≥ 5 s` và ngước camera nhìn đàn **Sơn ca bụi** lượn qua đĩa Trái Đất.
  2. Ban đêm, người chơi đỗ lại và bật/tắt đèn pha (`[L]`) để ngắm đàn **Bướm đèn** bay quanh và đậu lên mái màn hình CRT của 07.
  3. Khi thấy bóng **Kẻ Bộ Hành 40 m** nơi chân trời Biển Dài, người chơi chủ động lái về phía đó trong vòng `10 phút`.

---

## 1. Quy Tắc Mới Cho Bầy Đàn & Cơ Học Bay Chân Không (`[PROPOSAL]`)

### 1.1 Cập Nhật Quy Tắc Cũ "Thưa Thớt, Gặp Gỡ Chứ Không Bầy Đàn" (`0.6-living-moon.md:13, 39`)
Trong `docs/design/0.6-living-moon.md:13, 39`, quy tắc cũ đặt ra là *"Life is met, not swarming."* Ý kiến chỉ đạo của Owner (`2026-10-16`, `Logs/TASK.md:18-25`) đã cập nhật điểm này: **chỉ có vài robot lăn bánh trên mặt đất là quá đơn điệu; thế giới cần những bầy robot bay thu hoạch tài nguyên và những bất ngờ khổng lồ.** Để thêm bầy đàn mà không biến Mặt Trăng thành cảnh nền ồn ào, chúng ta áp dụng **Quy Tắc Bầy Đàn Như Một Sự Kiện (The Event-Swarm Rule)**:

1. **Bầy Đàn Là Sự Kiện Theo Pha Sáng Hoặc Địa Điểm, Không Phải Cảnh Nền:**
   - **Giới hạn đồng thời nghiêm ngặt:** Tối đa **1 bầy đàn đang hoạt động (`12–80` cá thể)** và **1 nhóm máy mặt đất** trong bán kính tầm nhìn `250 m` của 07 tại mọi thời điểm.
   - **Gắn chặt với pha thời gian & tương tác vật lý:**
     - **Sơn ca bụi (`Dust Larks`)** chỉ lượn theo **dải ranh giới sáng/tối (terminator)** lúc bình minh/hoàng hôn (`~10 phút` mỗi ngày Mặt Trăng 2 giờ, `0.6-living-moon.md:104` L1) và đáp xuống Tháp Chuồng đã sửa trong `60 s`.
     - **Bướm đèn (`Lantern Moths`)** chỉ xuất hiện vào **pha đêm ánh sáng Trái Đất (earthlit)** hoặc trong hang tối, tụ quanh nguồn sáng ấm (`#FFB547`).
     - **Sứa khói băng (`Plume Drifters`)** xếp cánh ngủ yên trên nền băng tối cho tới khi **Đạn Cầu Lửa** (`inventions.md` §2.2) hoặc **Tháp Gương Đẩy Nắng** (`inventions.md` §2.8) tạo cột hơi nước bốc lên.
     - **Ong thợ thủy tinh (`Glassweaver Bees`)** và **Cá đuối từ trường (`Swirl Rays`)** chỉ sống tại đúng kỳ quan địa chất của chúng (**Hố Thủy Tinh Cam** và **Cánh Đồng Xoáy Từ**).
2. **Vật Lý Bay Chân Không Đọc Được Ngay Bằng Mắt (Mặt Trăng Không Có Không Khí):**
   Vì Mặt Trăng là chân không (`10⁻¹² torr`), không một robot nào dùng cánh quạt trực thăng hay vỗ cánh quạt gió. Mọi loài bay đều vận hành bằng **4 cơ chế vật lý thập niên 1980 nhìn là hiểu ngay**:
   - **M1 — Lướt Tĩnh Điện Trên Lớp Bụi Đường Ranh Sáng (`Sơn ca bụi`):** Cánh lưới đồng–vàng tích điện (`±1.5 kV`) cưỡi lên **cột bụi tĩnh điện $1000\text{ V/m}$** dọc đường ranh giới ngày/đêm (hiện tượng *lunar horizon glow* được tàu *Surveyor 7* và *Apollo 17* ghi nhận thực tế, `0.6-living-moon.md:300`; `inventions.md` §2.10) nhờ lực đẩy Coulomb trong trọng lực $1/6\,g$.
   - **M2 — Phụt Khí Lạnh $\text{O}_2$ Qua Vòi Phun De Laval (`Bướm đèn`, `Én phụt`):** Xả khí **$\text{O}_2$ nén từ Lò Thở** (`inventions.md` §2.1, §2.5) thành từng cụm sương trắng ngưng tụ Joule-Thomson rõ nét.
   - **M3 — Nhảy Đạn Đạo $1/6\,g$ & Cưỡi Luồng Hơi Thăng Hoa (`Ong thợ thủy tinh`, `Sứa khói băng`):** Bật nhảy theo quỹ đạo parabol $1/6\,g$, hoặc bung dù lá vàng Kapton hứng động압 của cột hơi nước $\text{H}_2\text{O}$ bốc lên từ băng thăng hoa (`inventions.md` §2.2).
   - **M4 — Trượt Đệm Từ Trường Lorentz/Nghịch Từ (`Cá đuối từ trường`):** Đệm bụng siêu dẫn lướt trên dị thường từ trường vỏ Mặt Trăng cường độ $300\text{ nT}$ của **Xoáy Từ Reiner Gamma**.
3. **Giữ Nguyên Các Luật Cốt Lõi (`0.6-living-moon.md` §0 & §1a; `story-v4.md` §0–§1):**
   - **Tuyệt đối không thù địch, không cản đường (`VISION.md` Ruling 7):** Bầy đàn tự động rẽ sóng nhường đường (bán kính tách boids `3 m` quanh 07).
   - **Quét (`[E]`) không bao giờ làm biến mất sinh vật (`0.6-living-moon.md:46-48`):** Quét một cá thể bất kỳ sẽ vẽ nét trắng vào ống kính và mở trang Ghi chép (`Notes`) với 1–2 câu bình luận ngắn gọn của **Kenji** hoặc **Ro** (`0.6-living-moon.md:324-341`).
   - **Hai Họ Máy Rõ Ràng (`0.6-living-moon.md` §1a.6; `story-v4.md:24-25, 93-94`):** Mỗi loài thuộc về **Họ Lumen (`ILC`, sơn kem `#F4E6C8` + sọc cam `#FF8A5B`, đế sạc cảm ứng tròn, chế độ `"continue"`)** hoặc **Họ Meridian Tự Động (`Tycho Consortium`, vỏ trắng + xanh mòng két `#3CA6A2`, đế tiếp xúc lục giác, chế độ `"safe mode"` chờ khôi phục liên lạc)**.
4. **Ngân Sách Hiệu Năng Bầy Đàn (`CLAUDE.md:34` Zero GC):**
   - **Không tạo `GameObject` / `Transform` riêng cho từng cá thể:** Mỗi bầy là 1 `MonoBehaviour` duy nhất quản lý mảng `SwarmAgentState[maxCount]` cấp phát sẵn (`0 B` GC mỗi khung hình).
   - **1 Draw Call mỗi bầy:** Vẽ bằng `Graphics.DrawMeshInstancedProcedural` với vật liệu R1 (`docs/art/r1-pipeline.md`), truyền ma trận và `_EmissionStrength` qua `ComputeBuffer` / `MaterialPropertyBlock` (`RULES.md:43`).
   - **3 mức LOD:** **LOD0 (`0–40 m`):** `180–380` tam giác, uốn cánh trong vertex shader; **LOD1 (`40–120 m`):** `48–80` tam giác; **LOD2 (`120–300 m`):** `2–6` tam giác phát sáng; **`>300 m`:** ngủ đông (cập nhật 1 Hz).

---

## 2. Bảng Tổng Hợp 7 Loài Mới (`[PROPOSAL]`)

| # | Tên loài (VI / EN) | Họ máy & Nguồn gốc | Cơ chế di chuyển | Quy mô & Thời점 xuất hiện | Phản ứng với 07 | Thu thập → Nơi trút hàng | Vai trò lối chơi | Concept Art |
|---|---|---|---|---|---|---|---|---|
| **S1** | **Sơn ca bụi** / *Dust Larks* | **Lumen (`ILC`)** · Seed #1 | Cánh lưới vàng tích điện lướt trên màn bụi tĩnh điện | `30–80` cá thể · Quét theo đường bình minh/hoàng hôn + Tháp Chuồng | **Tò mò vui tươi:** lượn vòng số 8 trên đầu; sà xuống lướt vệt bụi bánh xe 07 | Bụi mịn giàu $\text{He-3}$ & Ti → **Tháp Chuồng (Roost Tower)** | `Khoảnh khắc choáng ngợp` + `Nguồn RareEarths / Ilmenite tự động` | `Logs/concepts/dust_larks_dawn.png` |
| **S2** | **Bướm đèn** / *Lantern Moths* | **Lumen (`ILC`)** · Seed #2 | Cánh tản nhiệt xếp nếp + phụt vi khí lạnh $\text{O}_2$ | `12–24` cá thể · Ban đêm & trong hang quanh đèn ấm | **Hướng quang dịu dàng:** bay thành vòng hào quang quanh đèn 07; đậu lên trán CRT | Nhiệt mặt trời & bụi thấu kính → **Cột Đèn & Nhà Kính** | `Đom đóm đêm` + `Tăng sáng vùng tối` + `Lau cánh năng lượng` | `Logs/concepts/lantern_moths_night.png` |
| **S3** | **Én phụt** / *Puff Swifts* | **Lumen (`ILC`)** · Seed #3 *(Đã tinh chỉnh, §4)* | Nhảy đạn đạo $1/6\,g$ bằng vòi phụt khí lạnh $\text{O}_2$ trắng | `3` chiếc đội hình chữ V · Mỗi `~15 phút` giữa các trụ lưới | **Đúng lịch trình:** bay vòng cung trên cao, phụt 3 nhịp còi khi được quét | Cầu mẫu lõi khoan & băng dự phòng → **Lưới Đón Trụ Tiếp Sóng** | `Hoa tiêu bầu trời` (dẫn đường tới trạm khoan & hộp lưu trữ) | `Logs/concepts/puff_swifts_flight.png` |
| **S4** | **Kẻ Bộ Hành 40 m** / *Regolith Strider* | **Lumen (`ILC`)** · Seed #4 (Leviathan) | Cỗ máy khai mỏ 6 chân cao `40 m` & nạp quặng Đường Ray L5 | `1` cá thể khổng lồ · Đứng nơi chân trời (`0.6+`), thức tỉnh ở `0.9–1.0` | **Khổng lồ hiền hòa:** bước cao qua đầu 07; hạ cầu bụng làm trạm sạc di động | Quặng bazan, `Alloy` & thùng phóng L5 → **Khóa Nạp Đường Ray L5** | `Kỳ quan Leviathan chân trời` + `Căn cứ di động` + `Cầu nối 1.0` | `Logs/concepts/dormant_strider_horizon.png` |
| **S5** | **Sứa khói băng** / *Plume Drifters* | **Meridian (`Tycho`)** · **Loài Mới #1** | Dù lá vàng & xanh ngọc cưỡi cột hơi nước $\text{H}_2\text{O}$ thăng hoa | `15–25` cá thể · Bung nở trong `90 s` khi bắn **Đạn Cầu Lửa** / Gương nắng | **Nhút nhát uyển chuyển:** xoắn ốc bay lên theo cột hơi; rắc tuyết băng quanh 07 | Hơi nước $\text{H}_2\text{O}$ thăng hoa → **Vòng Ngưng Tụ Lạnh** | `Cộng hưởng Phát minh` (`Ember Shell`) + `Nhân sản lượng Băng` | `Logs/concepts/plume_drifters_crater.png` |
| **S6** | **Ong thợ thủy tinh** / *Glassweaver Bees* | **Meridian (`Tycho`)** · **Loài Mới #2** | Nhảy parabol $1/6\,g$ + vòi phụt $\text{O}_2$ & bụng lò nung cảm ứng | `18–30` cá thể · Hoạt động khi **Hố Thủy Tinh Cam** có nắng | **Chăm chỉ lịch sự:** nhảy qua đầu 07; thả cầu thủy tinh ấm vào gầu xúc 07 | Hạt thủy tinh cam `OrangeBeads` → **Lò Tổ Ong Lục Giác** | `Lò luyện sinh học` (sản xuất vỏ cầu `Glass` cho `Ember Shell`) | `Logs/concepts/glassweaver_bees_swarm.png` |
| **S7** | **Cá đuối từ trường** / *Swirl Rays* | **Meridian (`Tycho`)** · **Loài Mới #3** | Đệm bụng siêu dẫn nghịch từ lướt cách mặt đất `0.4 m` | `8–14` cá thể · Lướt liên tục dọc dải trắng **Xoáy Từ Reiner Gamma** | **Tò mò từ tính:** kè hai bên hông 07; khử nhiễu màn hình CRT & tăng tốc | Proton gió mặt trời & điện tích → **Tháp Từ Trường Gauss** | `Linh hồn Cánh Đồng Xoáy Từ` + `Hộ tống làn tăng tốc` | `Logs/concepts/lunar_swirl_field.png` |

---

## 3. Đặc Tả Chi Tiết Từng Loài (`S1–S7`, `[PROPOSAL]`)

### 3.1 Loài S1: Sơn Ca Bụi (*Dust Larks*) — Phát Triển Từ Seed #1 `[PROPOSAL]`
![Đàn Sơn Ca Bụi Lúc Bình Minh](../../Logs/concepts/dust_larks_dawn.png)

- **Công việc & Người chế tạo:** Do **Kenji Morrow** và **Ủy ban Mặt Trăng Quốc tế (`ILC`, họ Lumen)** chế tạo năm **1983** (`story-v4.md:20, 70, 93`). Trong thập niên xây dựng các Tàu Phương舟 (Arks) tại điểm Lagrange L4/L5 (`1978–1988`), đội tàu kéo nhiệt hạch D–³He cần **Heli-3 ($\text{^3He}$)** và bụi mịn giàu titan tích tụ ở lớp bề mặt regolith nhờ gió Mặt Trời. Thay vì cày xới cả vùng biển bazan, Kenji thiết kế những đàn tàu lượn tĩnh điện siêu nhẹ đặt ở chế độ **`CONTINUE`** (`0.6-living-moon.md:168, 237`).
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Mỗi chiếc có sải cánh `0.65 m`, thân giọt nước sơn kem `R1_CreamEnamel` kẻ sọc cam `R1_OrangeStripe`, đuôi gắn hạt đèn hổ phách `R1_LampAmber`, và đôi cánh dệt bằng **lưới dây đồng `R1_Copper` căng lá vàng `R1_GoldFoil`**. Chúng không vỗ không khí: cánh lưới được tích điện `+1.8 kV` để **lướt trên lực đẩy tĩnh điện của màn bụi bay lơ lửng dọc đường ranh giới ngày/đêm (terminator)** (`0.6-living-moon.md:300`), nghiêng cánh bằng cách đổi điện áp giữa hai mạn.
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** `30–80` chiếc mỗi đàn.
  - **Trọng số Boids:** Tách đàn (Separation) `1.4` (bán kính `1.8 m`), Căn hướng (Alignment) `1.8`, Kết dính (Cohesion) `1.2`, cộng với **Đường dẫn Spline Hình Số 8 (`2.0`)** ở độ cao `4–18 m` bám theo đường bình minh đang dịch chuyển.
  - **Lịch trình:** Di cư liên tục theo đường ranh sáng của Mặt Trời vùng cực (`1 ngày Mặt Trăng = 2 giờ chơi`, `0.6-living-moon.md:104`). Mỗi bình minh, cả đàn lượn qua Thung Lũng Nhà và Biển Dài, in bóng đen lấp lánh trên nền Trái Đất tối đen và đốm sáng L4/L5 của các tàu Ark (`story-v4.md:50-54`, `[DECIDED]`).
- **Phản ứng với 07 (Không thù địch):** **Tò mò tinh nghịch.** Khi 07 chạy bên dưới, 8–10 chiếc ở tầng thấp sẽ sà xuống lướt trên đám bụi tích điện tung lên sau bánh xe 07 suốt `15 s`, khiến 07 ngoái đầu lại và hót tiếng **Happy (Vui vẻ)** (`0.6-living-moon.md:69-70`).
- **Thu thập gì & Trút hàng ở đâu:** Cánh lưới tích điện hút các hạt bụi nano giàu $\text{^3He}$, ilmenit và đất hiếm trong màn sương chân trời. Khi đầy, cả đàn xoắn ốc đáp xuống **Tháp Chuồng (*Roost Tower*)**—một cột cao `4.5 m` sơn kem và đồng thau có 3 vòng đậu sạc cảm ứng đồng tâm và phễu thu ở đỉnh (`dust_larks_dawn.png`). Khi đậu lên vòng, cánh ngắt điện áp về `0 V`, làm rơi một cơn mưa bụi vàng óng vào phễu trong lúc tụ điện sạc đầy.
- **Tương tác của 07 & Vai trò lối chơi:**
  - **Đi theo:** Đi theo đàn Sơn ca bụi lúc hạ cánh sẽ dẫn 07 tới các **Tháp Chuồng** chưa mở trên Biển Dài.
  - **Khôi phục Tháp Chuồng tại Căn cứ:** Nếu 07 sửa hoặc xây một **Tháp Chuồng** ở căn cứ (`2 Alloy + 1 Wiring + 1 Optics`), một đàn 40 chiếc sẽ **nhận căn cứ làm tổ**, mỗi bình minh lại bay về đậu sáng rực và trút **`1 Rare Earths` + `2 Ilmenite`** (`SalvageMaterial.cs:26, 32`) vào phễu cho 07.
  - **Quét (`[E]`):** Ghi chép của Kenji: *"Tàu lượn hớt bụi đường bình minh. Chúng cưỡi lên điện tích tĩnh nơi nắng chạm vào bụi lạnh, chải từng hạt heli-3 cho đội tàu kéo trên quỹ đạo."*
- **Âm thanh (Ngũ cung Rê trưởng `D E F# A B`):** Tiếng chuông gió lấp lánh (`D5 · F#5 · A5 · B5 · D6`) tạo bởi các hạt bụi tĩnh điện chạm vào lưới đồng, ngân vang êm dịu khi làn sóng lướt qua đầu.
- **Ngân sách hiệu năng:** `1` draw call instanced (`DrawMeshInstancedProcedural`, `MaxCount = 80`), góc nghiêng cánh tính trong vertex shader; `LOD0 = 210 tris`, `LOD1 = 56 tris`, `LOD2 = 4 tris`; `0 B/frame` GC.

---

### 3.2 Loài S2: Bướm Đèn (*Lantern Moths*) — Phát Triển Từ Seed #2 `[PROPOSAL]`
![Đàn Bướm Đèn Quanh 07 Trong Đêm](../../Logs/concepts/lantern_moths_night.png)

- **Công việc & Người chế tạo:** Do **Kenji Morrow** (`Họ Lumen`, `R1_CreamEnamel` + `R1_Brass`) chế tạo năm **1985** làm **đèn pha di động không bóng đổ** cho các thành viên phi hành đoàn khi sửa cáp hoặc chăm sóc **Nhà Kính Thử Nghiệm** (`inventions.md` §2.3) giữa đêm Mặt Trăng dài 55 phút (`north-star.md:164`). Cảm biến quang học của chúng chạy một lệnh đơn giản: *tìm bất kỳ ánh đèn làm việc màu vàng ấm (`2700–3200 K`), bay lơ lửng thành vòng tròn 4 m quanh đó và bật bầu đèn khuếch tán của mình để thợ máy không bị sấp bóng.* Khi con người rời đi từ tháng 10/1988 (`story-v4.md:48`, `[DECIDED]`), chúng tụ về quanh những ánh đèn ấm duy nhất còn sót lại: **đèn pha của 07, các trụ tiếp sóng đã thắp sáng, và căn cứ.**
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Mỗi chú bướm dài `0.35 m` có đầu sơn kem gắn hai "râu" ống quang điện bằng đồng, bốn **cánh tản nhiệt xếp nếp bằng lá vàng `R1_GoldFoil`** (xoay góc để bức xạ nhiệt và giữ thăng bằng quán tính, tuyệt đối không vỗ gió), phần bụng là **bầu đèn thủy tinh hổ phách ấm (`R1_LampAmber`)**, và dưới bụng gắn vòi phun đồng nhỏ xíu nhả **từng cụm khí lạnh $\text{O}_2$ trắng** (`inventions.md` §2.5) mỗi `1.2 s` để lơ lửng trong trọng lực $1/6\,g$ (thấy rõ từng cụm khói trắng nhỏ dưới bụng mỗi chú bướm trong `lantern_moths_night.png`).
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** `12–24` cá thể mỗi cụm.
  - **Trọng số Boids:** Quỹ đạo xoay tròn êm dịu quanh nguồn sáng ấm gần nhất trong bán kính `60 m` (Tangential Orbit `1.6`, Separation `1.5` ở `0.9 m`, nhịp nhấp nhô `0.4 Hz` khớp với nhịp phụt khí lạnh).
  - **Lịch trình:** Ngủ ban ngày dưới gầm tấm pin mặt trời hoặc khung Nhà Kính để sạc điện và nạp $\text{O}_2$ từ đường ống Lò Thở; thức giấc vào **Đêm Đầu Tiên** (`north-star.md` Beat 1) và trong **Ống Dung Nham** (`0.8`).
- **Phản ứng với 07 (Không thù địch):** **Hướng quang thân thiện (thuần túy do chương trình).** Khi 07 bật đèn pha ban đêm, đàn Bướm đèn bay tới kết thành vòng hào quang ấm áp quanh xe. Nếu 07 đứng yên `4 s`, **một hai chú sẽ nhẹ nhàng đậu lên mái che màn hình CRT của 07** (`lantern_moths_night.png`), làm đôi mắt CRT của 07 cong lên hạnh phúc (`^ ^`)!
- **Thu thập gì & Trút hàng ở đâu:** Ban ngày hấp thụ năng lượng mặt trời và chải sạch bụi tĩnh điện trên thấu kính đèn; khi bình minh lên, chúng quay về đậu trên **Vòng Đế Trụ Tiếp Sóng** hoặc **Nhà Kính Đã Khôi Phục** (`inventions.md` §2.3).
- **Tương tác của 07 & Vai trò lối chơi:**
  - **Vòng Sáng Sống Động (`Hỗ trợ khám phá đêm & Hố Bóng Tối`):** Dẫn một đàn Bướm đèn theo ánh đèn pha vào xác tàu tối hoặc vành hố sâu giúp **tăng gấp đôi bán kính chiếu sáng ấm của 07** trong `3 phút`.
  - **Hoa tiêu dẫn tới Nhà Kính:** Trong đêm tím earthlit, một đám mây vàng ấm của Bướm đèn lơ lửng trên bậc thềm phía xa sẽ dẫn 07 tới **Nhà Kính Số 3** (`inventions.md` §2.3).
  - **Quét (`[E]`):** Ghi chép của Ro: *"Kenji làm chúng để tụi mình không bị bóng lưng che khuất cờ-lê lúc làm ca đêm. Cứ bật một ngọn đèn ấm lên là chúng lại bay tới giúp."*
- **Âm thanh (Ngũ cung Rê trưởng):** Tiếng phụt khí *phù-leng* êm như chuông thủy tinh bọc nỉ (`F#4 · A4 · D5`).
- **Ngân sách hiệu năng:** `1` draw call instanced (`MaxCount = 24`) + **1 nguồn sáng Point Light dùng chung không đổ bóng** đặt tại tâm đám mây (không bao giờ bật 24 đèn riêng lẻ!); `LOD0 = 240 tris`, `LOD1 = 64 tris`, `LOD2 = 4 tris`.

---

### 3.3 Loài S3: Én Phụt (*Puff Swifts*) — Seed #3 Đã Kiểm Định & Tinh Chỉnh `[PROPOSAL]`
![Tốp Ba Én Phụt Đang Bay](../../Logs/concepts/puff_swifts_flight.png)

> **Ghi chú Thiết kế Quan trọng (Đồng bộ với `story-v4.md` & `inventions.md` §3.1):** Trong `inventions.md` §3.1, ý tưởng *máy bắn đá bắn quả cầu thủy tinh không phanh qua nền đá trơ trụi sang trạm Meridian* đã bị **LOẠI BỎ** vì (1) Meridian hoàn toàn tự động, không có người ở (`story-v4.md:55`, `[DECIDED]`), (2) cầu thủy tinh rơi ở vận tốc $40\text{ m/s}$ trong chân không sẽ vỡ vụn, và (3) vật phẩm nhặt được không bao giờ nằm trơ trọi trên đất trống (`0.5.5-owner-feedback.md` §2). **Én phụt giải quyết trọn vẹn bài toán vật lý này** vì chúng là **robot nhảy đạn đạo có vòi phụt hãm tốc chủ động**, bay theo lộ trình cố định giữa **Các Trại Khoan Thực Địa của Ines và Vòng Lưới Đón Trên Trụ Tiếp Sóng Lumen**, mang theo **quả cầu mẫu lõi khoan & cuộn băng dự phòng**! (Xem §4 về phân tích Loại bỏ / Tinh chỉnh.)

- **Công việc & Người chế tạo:** Do **Kenji Morrow** và **Ines Kalani** (`Họ Lumen`, `R1_CreamEnamel` + `R1_OrangeStripe`) chế tạo năm **1986** để chuyển phát nhanh **ống mẫu lõi khoan và cuộn băng từ dự phòng** từ các mũi khoan xa ở Hẻm Núi Thầm Thì và Biển Dài về phòng thí nghiệm Lumen mà không tốn pin xe tự hành.
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Thân hình thoi khí động học dài `0.85 m` với 4 vây đuôi chữ thập (để cân bằng mô-men quán tính, không phải nâng khí động), mắt mũi `R1_LampAmber`, càng bụng ôm **quả cầu thủy tinh hổ phách (`R1_DarkGlass` + `R1_LampAmber`)**, và đuôi gắn **vòi phun $\text{O}_2$ khí lạnh De Laval** (`inventions.md` §2.5). Phóng lên từ vòng trụ, chúng bay theo **quỹ đạo parabol $1/6\,g$ chuẩn xác**, nhả **3 cụm khói trắng rõ nét**: lúc phóng, lúc chỉnh hướng ở đỉnh vòm, và lúc phụt ngược giảm tốc để đáp êm vào vòng lưới đồng của trụ đích (`puff_swifts_flight.png`).
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** Luôn bay theo **tốp 3 chiếc (đội hình chữ V lệch)** cách nhau `2.5 m` trên cùng một đường cong parabol (`Độ cao đỉnh = 25–45 m`, `Tầm bay = 250–400 m`).
  - **Lịch trình:** Cứ mỗi `~12–15 phút` lại có một tốp 3 chiếc phóng qua bầu trời giữa trạm khoan thực địa và trụ tiếp sóng.
- **Phản ứng với 07 (Không thù địch):** **Theo lịch trình trên không;** khi đậu trên vòng lưới, chúng cúi mắt nhìn 07 và hót 3 nốt báo hiệu bưu phẩm (`D4 → A4 → D5`).
- **Thu thập gì & Trút hàng ở đâu:** Nhận **Quả Cầu Mẫu Lõi / Băng Ghi** từ giàn khoan tự động và thả vào **Lưới Đón Trụ Tiếp Sóng**. Nơi nào trụ nhận bị gãy trong cơn bão tháng 10/1988, quả cầu cuối cùng nằm gọn bên trong **khung lưới đón bị sập** (tuân thủ tuyệt đối quy tắc vật chứa của `0.5.5-owner-feedback.md` §2!).
- **Tương tác của 07 & Vai trò lối chơi:**
  - **Ngước Nhìn & Đuổi Theo Đường Cung (`Hoa Tiêu Không Lời`):** Thấy 3 vệt khói trắng vạch đường chấm bi qua bầu trời sao sẽ chỉ chính xác hướng tới **trạm khoan ẩn, trụ tiếp sóng bị sập hoặc thùng mảnh thiết kế chưa tìm thấy**.
  - **Quét (`[E]`):** Ghi chép của Kenji: *"Đội chuyển phát ba chiếc. Một nhịp phụt vượt gờ đá, một nhịp ngắm vòng lưới, một nhịp phanh. Rẻ hơn nhiều so với lái xe mười cây số chỉ để chở một cuộn băng."*
- **Âm thanh (Ngũ cung Rê trưởng):** Ba nhịp phụt khí *PHỤT–phụt–phụt* (`A3 · E4 · A4`) kèm tiếng còi ngân truyền qua trụ tiếp sóng.
- **Ngân sách hiệu năng:** `1` draw call instanced (`MaxCount = 3`) + 1 hệ hạt GPU dùng chung cho vệt khói chấm bi (`LOD0 = 310 tris`, `LOD1 = 90 tris`).

---

### 3.4 Loài S4: Kẻ Bộ Hành Ngủ Yên (*The Dormant Regolith Strider — Thiết Giáp Khai Mỏ 40 Mét*) — Seed #4 Leviathan `[PROPOSAL]`
![Kẻ Bộ Hành 40 Mét Nơi Chân Trời](../../Logs/concepts/dormant_strider_horizon.png)

- **Công việc & Người chế tạo:** Do **Ủy ban Mặt Trăng Quốc tế (`ILC`, công trình hạng nặng liên hợp Lumen–Tycho)** chế tạo năm **1981** (`story-v4.md:70-72`). Để xây dựng 12 Tàu Phương舟 (Arks) tại L4/L5 trong vòng 10 năm, Mặt Trăng cần những cỗ máy khai thác lộ thiên di động khổng lồ có thể bước qua bãi đá tảng, đào hàng ngàn tấn bazan giàu ilmenit mỗi ngày và nạp thẳng thùng quặng vào **Đường Ray Phóng Điện Từ L5** (`inventions.md` §2.7). Số hiệu: **`STRIDER-02 "GOLIATH-B"`**. Trong **cơn bão Mặt Trời ngày 28/10/1988** (`story-v4.md:76`, `[DECIDED]`), hệ thống tự động đã khóa chốt an toàn địa chấn ngay giữa một bước chân bên cạnh giàn Đường Ray để xung điện từ không làm cháy thanh cái chính. Nó đã đứng sừng sững in bóng lên Trái Đất suốt 36 năm qua.
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Một **cỗ máy đào sáu chân khổng lồ cao `40 m`, ngang `55 m`** (`dormant_strider_horizon.png`): thân vỏ sơn kem `R1_CreamEnamel` vát cạnh với hai dải màu cam `R1_OrangeStripe` và xanh xám `R1_SagePatch`, sáu chân trục vít thủy lực bọc **chăn cách nhiệt vàng `R1_GoldFoil`** trên bàn đạp tròn khổng lồ, dưới bụng là **bánh xe gầu đào răng cưa (`R1_BareSteel`)** cùng **cầu dẫn hậu có thể hạ xuống đất cho 07 chạy lên**, trên lưng là tháp cẩu chớp đèn cảnh báo hổ phách `R1_LampAmber` và **Sàn Đậu Cao** nơi đàn **Sơn ca bụi** làm tổ!
- **Hành vi & Lịch trình thức tỉnh:**
  - **Đầu & Giữa Game (`0.6–0.8`, Kỳ Quan Leviathan Nơi Chân Trời):** Nhìn thấy từ vành tây Thung Lũng Nhà và suốt Biển Dài (cách `1.5 km`), đứng bất động dưới đĩa Trái Đất tối đen và đốm sáng L4/L5. Mang lại đúng cảm giác rùng mình choáng ngợp khi lần đầu thấy Leviathan trong *Subnautica* (**tỷ lệ khổng lồ giữa đồng bằng hoang vắng**), nhưng nối tiếp ngay bằng **sự ấm áp yên bình**: đèn đỉnh tháp chớp nhịp chờ chậm rãi (`0.25 Hz`) và đàn Sơn ca bụi lượn quanh vai nó như chim hải âu quanh ngọn hải đăng.
  - **Cuối Game (`0.9–1.0`, Đánh Thức Người Khổng Lồ):** Khi 07 có **Mũi Cắt Nhiệt Khí Oxy** (`inventions.md` §2.6), **Cầu Nguồn (Power Bridge)** (`0.6-living-moon.md:245`) và `2 Superconductor`, 07 lái lên cầu dẫn bụng của Kẻ Bộ Hành, cắt chốt hãm bị kẹt và nối lại trạm liên lạc. Với một **hợp âm trầm `D1 + A1` rung chuyển lòng đất**, người khổng lồ 40 m nhấc một bàn chân, rũ xuống thác bụi tích tụ 36 năm, rồi chậm rãi bước đi ở vận tốc **`0.8 m/s`** dọc theo **Đường Ray Phóng L5** về phía chân Đỉnh!
- **Phản ứng với 07 (Không thù địch):** **Người khổng lồ hiền lành che chở.** Radar tầm gần của nó luôn theo dõi 07: không bao giờ đặt chân trong phạm vi `12 m` quanh 07, dừng chân giữa không trung nếu 07 chạy qua gầm, và hạ cầu bụng xuống mỗi khi 07 bấm còi/ping (`[Q]`) để 07 lên sạc pin và ngắm toàn cảnh Biển Dài từ trên lưng nó!
- **Thu thập gì & Trút hàng ở đâu:** Bánh xe gầu đào dưới bụng sàng lọc quặng thành `Alloy` tinh chế và chở **Thùng Phóng Mẫu Lõi L5 / Giá Đỡ Siêu Dẫn Của Chảo Thu Lớn** băng qua khe nứt Biển Dài tới chân Đường Lên Đỉnh (`1.0`).
- **Âm thanh (Ngũ cung Rê trưởng):** Tiếng trầm đại phong cầm (`D1 → A1 → D2`) truyền qua nền đá mỗi `8 s` khi bàn chân chạm đất, điểm tiếng còi xả áp đồng thau (`F#4`).
- **Ngân sách hiệu năng:** `1` bộ khung cơ khí phân cấp (`6` chân IK, không dùng skinned mesh, `~18,000 tris` chia 12 sub-mesh dùng chung vật liệu R1; gộp static batch khi ngủ yên ở `0.6–0.8`).

---

### 3.5 Loài S5: Sứa Khói Băng (*Plume Drifters*) — Loài Tự Thiết Kế #1 `[PROPOSAL]`
![Đàn Sứa Khói Băng Cưỡi Cột Hơi Nước Của Đạn Cầu Lửa](../../Logs/concepts/plume_drifters_crater.png)

- **Công việc & Người chế tạo:** Do **Trạm Meridian Tự Động (`Tycho Consortium`, vỏ trắng + xanh mòng két `#3CA6A2` + `R1_GoldFoil`)** chế tạo năm **1987** (`story-v4.md:25, 94`, `[DECIDED]` Decision 5) để thu giữ hơi nước và khí dễ bay hơi tinh khiết ($\text{H}_2\text{O}, \text{CO}, \text{NH}_3$) trong **Hố Bóng Tối (`0.8`)** mà không cần đưa xe bánh xích nặng nề đè lên thềm băng giòn ở $-220^\circ\text{C}$.
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Những drone thu băng hình sứa đường kính `0.9 m` (`plume_drifters_crater.png`) có mũ gốm trắng, lõi ngưng tụ phát sáng màu hổ phách ấm (`R1_LampAmber`), sáu **cánh dù màng xanh ngọc viền lá vàng `R1_GoldFoil`**, và ba **vòi phụt vi khí lạnh** dưới đáy. Trong chân không, chúng xếp cánh ngủ yên trên vành hố cho tới khi có nhiệt lượng làm thăng hoa lớp băng đáy hố; ngay khi 07 bắn một **Đạn Cầu Lửa (Ember Shell)** (`inventions.md` §2.2) hoặc xoay **Tháp Gương Đẩy Nắng** (`inventions.md` §2.8), luồng hơi nước $\text{H}_2\text{O}$ bốc mạnh lên cao ($v \approx 400\text{ m/s}$) sẽ thổi căng tán dù của chúng, **nâng 15–25 chú Sứa khói băng xoay vòng lấp lánh lên độ cao `30 m` giữa màn đêm vĩnh cửu!**
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** `15–25` cá thể mỗi túi băng.
  - **Trọng số Boids:** Xoắn ốc theo cột khí nóng (`2.2` trong hình trụ hơi nước), Tách đàn `1.6` (`2.0 m`), Hạ cánh dù chậm (`0.6 m/s` trong $1/6\,g$ khi đạt độ cao `25 m`, co giãn gân dù xanh ngọc nhịp nhàng như sứa biển sâu).
  - **Lịch trình:** Thức giấc và bay lượn suốt **`90 s`** mỗi khi **Đạn Cầu Lửa** bùng cháy hoặc **Tia nắng Heliostat** quét qua thềm băng (`0.6-living-moon.md:303` *"Frost sublimation"*).
- **Phản ứng với 07 (Không thù địch):** **Nhút nhát uyển chuyển.** Khi lơ lửng hạ xuống quanh 07, chúng nghiêng vòm chuông phát sáng về phía **Diều Hứng Hơi Băng (Vapor Kite)** của 07 (`inventions.md` §2.2), rắc một cơn mưa tinh thể băng lấp lánh trôi thẳng vào phễu thu của 07.
- **Thu thập gì & Trút hàng ở đâu:** Tấm ngưng tụ siêu lạnh `-180°C` bên trong vòm dù đóng băng hơi nước thành **Băng Nước tinh khiết (`SalvageMaterial.WaterIce = 6`)** và **Tinh Thể (`SalvageMaterial.Crystal = 11`)**, trút vào **Vòng Ngưng Tụ Lục Giác** của Meridian dưới đáy hố (07 dùng chung được sau khi xây **Cầu Nguồn**, `0.6-living-moon.md:245`).
- **Tương tác của 07 & Vai trò lối chơi:**
  - **Cộng hưởng trực tiếp với Phát minh (`Ember Shell` + `Heliostat`):** Bắn Đạn Cầu Lửa vào túi băng có Sứa khói băng đang ngủ sẽ đánh thức cả đàn, **tăng sản lượng thu hoạch từ `3 Water Ice` lên `5 Water Ice + 1 Crystal`**, đồng thời biến đáy hố tối đen thành một vực biển sâu phát quang tuyệt đẹp (`plume_drifters_crater.png`).
  - **Quét (`[E]`):** Ghi chép của Ro: *"Đội hứng sương của trạm Meridian. Khi lớp băng bừng thành hơi ấm, chúng bung tán dù như những đóa hoa dưới biển sâu."*
- **Âm thanh (Ngũ cung Rê trưởng):** Tiếng đàn glass-harmonica ngân trong vắt (`D4 · E4 · F#4 · A4`) hòa cùng tiếng tinh thể băng kết tinh lách tách.
- **Ngân sách hiệu năng:** `1` draw call instanced (`MaxCount = 25`), chuyển động co giãn tán dù chạy bằng hàm sin trong vertex shader (`LOD0 = 280 tris`, `LOD1 = 72 tris`, `LOD2 = 6 tris`).

---

### 3.6 Loài S6: Ong Thợ Thủy Tinh (*Glassweaver Bees*) — Loài Tự Thiết Kế #2 `[PROPOSAL]`
![Đàn Ong Thợ Thủy Tinh & Lò Tổ Ong](../../Logs/concepts/glassweaver_bees_swarm.png)

- **Công việc & Người chế tạo:** Do **Trạm Meridian Tự Động (`Tycho Consortium`, vỏ trắng + xanh mòng két `#3CA6A2` + đồng thau `R1_Brass`)** chế tạo năm **1986** ngay trong lòng **Hố Thủy Tinh Cam** (`orange_glass_crater.png` & `glassweaver_bees_swarm.png`). Cả Meridian lẫn Lumen đều cần hàng ngàn **Quả Cầu Thủy Tinh Hổ Phách** rỗng, đồng nhất và không bọt khí để làm vỏ **Đạn Cầu Lửa** (`inventions.md` §2.2), **Đèn Tảo Sống** (`inventions.md` §2.3) và bầu cách điện chân không. Thay vì chở đất núi lửa thô đi xa, Meridian đặt một **Lò Tổ Ong Cảm Ứng Mặt Trời (*Hive Kiln*)** giữa đáy hố cùng một đàn ong thợ tích hợp lò nung mini.
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Những chú ong cơ khí tròn trịa dài `0.45 m` (`glassweaver_bees_swarm.png`) với lưng trắng–xanh mòng két, vòi hút đồng thau để hút hạt thủy tinh núi lửa, 6 chân lò xo đồng, đôi **cánh tản nhiệt lá vàng `R1_GoldFoil`**, phần bụng là **lò nung quay trong suốt phát sáng màu mật ong hổ phách (`R1_LampAmber`)**, và hai **vòi phụt vi khí lạnh $\text{O}_2$** ở đuôi. Chúng di chuyển khắp lòng hố cam bằng những **cú bật nhảy parabol $1/6\,g$ xa `6–12 m`**, phụt một cụm khói trắng nhỏ ở đỉnh vòng cung (`orange_glass_crater.png`).
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** `18–30` chú ong quanh một Lò Tổ Ong.
  - **Trọng số Boids:** Vòng lặp kiếm ăn hướng tâm—ong nhảy tỏa ra (`15–35 m`) từ **Lò Tổ Ong** tới các triền cát giàu hạt thủy tinh cam, dừng `3 s` hút hạt vào bụng (bụng rực sáng cam khi cuộn cảm ứng bên trong nung chảy hạt ở `1100°C`), rồi bật nhảy thành những đường cung vàng rực rỡ (`orange_glass_crater.png`) về chui vào các ô lục giác của Lò Tổ Ong!
  - **Lịch trình:** Hoạt động mỗi khi **Hố Thủy Tinh Cam** có nắng; nằm chờ ở `safe mode` cho tới khi 07 khôi phục trạm liên lạc Meridian trên miệng hố hoặc nạp `1 Bình Khí Oxy` vào Lò Tổ Ong.
- **Phản ứng với 07 (Không thù địch):** **Chăm chỉ và hiếu khách.** Nếu 07 đỗ cạnh Lò Tổ Ong và hạ gầu xúc trước (`glassweaver_bees_swarm.png`), hai chú ong sẽ nhảy phốc lên nắp ca-pô của 07 để ngắm khuôn mặt CRT ấm áp (`^ ^`), trong khi khuôn tổ ong lục giác nhả một **Quả Cầu Thủy Tinh Hổ Phách (`SalvageMaterial.Glass = 8`)** tròn trịa lăn dọc máng trượt rơi thẳng vào gầu xúc của 07!
- **Thu thập gì & Trút hàng ở đâu:** Hút **`OrangeBeads` (`SalvageMaterial.OrangeBeads = 4`)** từ sườn hố núi lửa và rót thủy tinh nóng chảy vào **Lò Tổ Ong Lục Giác** để thổi ly tâm thành **Quả Cầu Thủy Tinh**.
- **Tương tác của 07 & Vai trò lối chơi:**
  - **Đánh Thức Tổ Ong (`0.6–0.7`):** Khôi phục tín hiệu và nạp bình $\text{O}_2$ cho Lò Tổ Ong sẽ đánh thức cả 24 chú Ong thợ. Từ đó, mỗi phiên ghé qua Hố Thủy Tinh Cam luôn có sẵn **`3 Glass` + `2 Vỏ Cầu Lửa (Ember Shell)`** chờ trên máng trượt!
  - **Quét (`[E]`):** Ghi chép của Kenji: *"Chúng hút hạt thủy tinh cam, nung chảy ngay trong bụng lúc đang nhảy giữa không trung, rồi nhả quả cầu thủy tinh hoàn chỉnh vào tổ. Giá mà chính tay tôi thiết kế ra tụi nó."*
- **Âm thanh (Ngũ cung Rê trưởng):** Giai điệu gõ marimba/kính lanh lảnh vui tai (`D4 · F#4 · A4 · B4`) mỗi khi ong bật nhảy và cầu thủy tinh lăn leng keng xuống máng đồng.
- **Ngân sách hiệu năng:** `1` draw call instanced (`MaxCount = 30`), quỹ đạo parabol tính bằng công thức giải tích trong C# (`p(t) = p0 + v0*t + 0.5*g*t²`, không tốn collider vật lý; `LOD0 = 260 tris`, `LOD1 = 68 tris`).

---

### 3.7 Loài S7: Cá Đuối Từ Trường (*Swirl Rays / Lorentz Skimmers*) — Loài Tự Thiết Kế #3 `[PROPOSAL]`
![Cánh Đồng Xoáy Từ & Đàn Cá Đuối Từ Trường](../../Logs/concepts/lunar_swirl_field.png)

- **Công việc & Người chế tạo:** Do **Trạm Meridian Tự Động (`Tycho Consortium`, dáng góc cạnh trắng + xanh mòng két `#3CA6A2`)** chế tạo năm **1987** (`story-v4.md:25, 94`) để thu hạt proton gió Mặt Trời và đo bản đồ từ trường vỏ Mặt Trăng tại **Cánh Đồng Xoáy Từ trên Biển Dài** (`0.6-world-regions.md:63-64, 145`).
- **Cách di chuyển (Vật lý chân không đọc được ngay):** Những tàu lượn hình cá đuối manta phẳng dẹt sải cánh `1.1 m` (`lunar_swirl_field.png`) với mũi kim từ kế bằng đồng thau, cánh composite trắng–xanh mòng két, và bốn **đệm bụng siêu dẫn nghịch từ phát sáng xanh lơ (`#5FF3FF`)**. Bên trong một xoáy từ Mặt Trăng thực tế (như *Reiner Gamma*), dị thường từ trường vỏ tạo ra vòm từ trường ngang mạnh hàng trăm nanotesla. Nhờ dòng điện siêu dẫn chạy trong đệm bụng, **Cá đuối từ trường nổi lơ lửng cách mặt đất `0.4 m` bằng lực đẩy Lorentz/nghịch từ**, lướt êm ru dọc theo các dải bụi trắng uốn lượn mà không cần bánh xe hay khí phụt!
- **Hành vi bầy đàn (Quy tắc Boids, Quy mô & Lịch trình):**
  - **Số lượng:** `8–14` chiếc chia thành 2 biên đội chữ V (`lunar_swirl_field.png`).
  - **Trọng số Boids:** Bám chặt theo **Đường Sức Từ Dải Trắng (`2.5`)**, lượn uốn lượn quanh **Tháp Từ Trường Gauss** của Meridian ở tâm xoáy và nghiêng cánh đồng loạt ở mỗi khúc cua.
  - **Lịch trình:** Hoạt động liên tục (`24/7`—vòng siêu dẫn hầu như không tiêu hao điện trong cái lạnh Mặt Trăng và được nạp cảm ứng khi lướt qua Tháp Gauss).
- **Phản ứng với 07 (Không thù địch):** **Tò mò từ tính.** Khi 07 lái lên dải bụi trắng của xoáy từ (`lunar_swirl_field.png`), cả biên đội sẽ khớp tốc độ và lướt kè hai bên hông trái/phải của 07 như đàn cá heo bơi cạnh mũi thuyền! Khi có đàn Cá đuối kè bên, bộ ổn định từ thông của chúng **làm dịu hiện tượng nhiễu sọc màn hình CRT** của 07 và tạo **luồng trượt tĩnh điện giúp 07 chạy nhanh hơn `+15%`** dọc theo dải xoáy trắng.
- **Thu thập gì & Trút hàng ở đâu:** Hứng các ion gió Mặt Trời bị lệch hướng và nạp điện tích vào **Tháp Từ Trường Gauss** ở tâm xoáy (`lunar_swirl_field.png`), nơi 07 thu được dữ liệu hiệu chuẩn `Optics` / `Superconductor` (`Mảnh Khảo Sát 2/6`, `story-v4.md:140`).
- **Âm thanh (Ngũ cung Rê trưởng):** Hợp âm synth analog ấm áp (`D3 + A3 + F#4`) ngân nhẹ qua cuộn cảm radio của 07 mỗi khi xe lăn bánh trên dải xoáy trắng.
- **Ngân sách hiệu năng:** `1` draw call instanced (`MaxCount = 14`), `LOD0 = 190 tris`, `LOD1 = 52 tris`, `LOD2 = 4 tris`.

---

## 4. Đánh Giá Phản Biện Các Ý Tưởng Hạt Giống (Seed): Cắt Bỏ Hay Tinh Chỉnh? (`[PROPOSAL]`)

Theo yêu cầu tại `Logs/TASK.md:37, 72` (*"cut weak seeds and say why; add at least two species of your own"*), chúng tôi đã đối chiếu từng seed với `story-v4.md` (`[DECIDED]`), `0.6-living-moon.md` (§1a) và `inventions.md` (§3.1):

1. **Seed #3 (`Én phụt / Puff Swifts` chở cầu thủy tinh chứa băng cassette giữa hai trạm) — CẮT BỎ dạng gốc & Xếp hạng Phương án:**
   - **Vì sao dạng gốc không đứng vững:** Theo `story-v4.md:25, 55-56` (`[DECIDED]` Decision 5), **Trạm Meridian hoàn toàn tự động và không có người ở**. Phi hành đoàn Lumen sẽ không bao giờ bắn băng cassette thu âm giọng nói sang một trạm robot không có con người nghe. Hơn nữa, `inventions.md` §3.1 đã cắt bỏ máy bắn đá bắn cầu thủy tinh không phanh vì cầu thủy tinh rơi ở $40\text{ m/s}$ trong chân không sẽ vỡ nát và việc rải quả cầu trên mặt đất trống vi phạm `0.5.5-owner-feedback.md` §2 (*"không vật nhặt được nào nằm trơ trọi trên regolith trống"*).
   - **Xếp hạng Phương án (`RULES.md` §"Facts and creativity"):**
     - **Hạng 1 (Khuyên dùng tại §3.3 — Tinh chỉnh thành "Đội Chuyển Mẫu Lõi & Băng Dự Phòng Nội Vùng"):** Giữ lại hình ảnh 3 chiếc Én phụt vạch vòng cung khói trắng tuyệt đẹp (`puff_swifts_flight.png`), nhưng đặt chúng làm **robot chuyển mẫu từ các trại khoan thực địa của Ines về Vòng Lưới Đón trên Trụ Tiếp Sóng Lumen**, có vòi phụt $\text{O}_2$ phanh ngược chủ động để đáp êm vào lưới. Quả cầu bị lỡ nhịp sẽ nằm gọn trong **khung lưới đón bị sập**, không bao giờ lăn lóc trên đất trống.
     - **Hạng 2 (Cắt hoàn toàn):** Loại bỏ hẳn Én phụt để **Ống Bưu Chính Khí Nén** (`inventions.md` §2.4) độc quyền vận chuyển ống капсул và **Đàn Drone Khảo Sát** (`0.6-living-moon.md:57`) độc quyền dẫn đường trên không. *(Chúng tôi chọn Hạng 1 vì 3 vệt khói trắng vạch ngang bầu trời sao là một tín hiệu dẫn đường tầm xa `300 m` cực kỳ cuốn hút.)*
2. **Seed #5e (`Đỉnh Nắng Vĩnh Cửu / Peak of Eternal Light`) & #5f (`Đài Phun Bụi / Dust Fountains`) — HỢP NHẤT vào các vùng đã có:**
   - **Lý do:** Theo Quyết định L1 của Owner (`0.6-living-moon.md:80-104`), Lumen vốn đã nằm gần cực: **Đỉnh The Peak (`1.0`)** và **Gờ Gương Đẩy Nắng Trên Miệng Hố Bóng Tối (`0.8`, `inventions.md` §2.8)** *chính là* các Đỉnh Nắng Vĩnh Cửu. Tạo thêm một vùng "Đỉnh Nắng Vĩnh Cửu" riêng sẽ làm trùng lặp hình bóng của `1.0`. Tương tự, **Đài Phun Bụi** được hợp nhất thành **Hành Lang Đường Ranh Sáng (Terminator Corridor)** quét qua Biển Dài (`§5`, `P3`), nơi **Sơn ca bụi** và **Cối Xay Bụi Tĩnh Điện** (`inventions.md` §2.10) hoạt động.

---

## 5. Sáu Kỳ Quan Mặt Trăng Có Thật Làm Địa Điểm (`[PROPOSAL]`)

Mỗi kỳ quan khác biệt với vùng lân cận trên **≥ 3 trong 5 trục đa dạng** (`Độ phản xạ Albedo · Địa hình · Pha ánh sáng · Dấu vết con người/Meridian · Sinh thái máy`, `north-star.md` §4b) và đặt ra một **Câu Hỏi Thu Hút (Hook Question)** tức thì:

| # | Tên địa điểm (VI / EN) | Vùng & Phiên bản | Câu hỏi thu hút (Hook Question) | Diện mạo: Bảng màu, Ánh sáng & Mặt đất (`VISION.md`) | Máy móc & Phát minh cư ngụ | Tương tác & Phần thưởng |
|---|---|---|---|---|---|---|
| **P1** | **Cánh Đồng Xoáy Từ Reiner Gamma** / *Lunar Swirl Field* (`lunar_swirl_field.png`) | Biển Dài (`0.6`) | *"Cái gì đã vẽ những dải lụa trắng khổng lồ lên nền đá bazan đen—và sao la bàn của mình lại quay tròn?"* | **Tương phản Albedo cực đại:** Những dải regolith trắng kem (`#F4E6C8`) uốn lượn trên nền bazan đen xanh (`#262A44`), phủ ánh hào quang từ trường xanh lơ (`#5FF3FF`) sát mặt đất. | **Cá đuối từ trường (`S7`)**, **Đàn Drone Khảo Sát**, và **Tháp Từ Trường Gauss** lục giác của Meridian (`lunar_swirl_field.png`). | **Hiệu ứng CRT & Làn tăng tốc:** Ở vùng đen giữa các dải xoáy, kim la bàn của 07 xoay nhẹ và màn hình CRT gợn sóng khử từ thập niên 80; khi lái *lên* dải trắng cùng đàn **Cá đuối từ trường**, màn hình ổn định và 07 được tăng `+15%` tốc độ dẫn thẳng tới **Tháp Gauss** (`Mảnh Khảo Sát 2/6`). |
| **P2** | **Hố Thủy Tinh Cam** / *Orange-Glass Crater* (`orange_glass_crater.png`) | Ranh giới Biển Dài / Bậc Thềm (`0.6–0.7`) | *"Sao lòng hố đen thẫm này lại lấp lánh như một chiếc hộp châu báu chứa đầy hổ phách nóng chảy?"* | **Địa chất thực tế từ Apollo 17 (Shorty Crater):** Vành tro núi lửa đen sẫm (`#1F2447`) bao quanh lòng hố bậc thang phủ hàng triệu **hạt thủy tinh núi lửa màu cam rực (`#FF8A5B` / `#FFB547`)** lấp lánh dưới nắng xiên. | **Ong thợ thủy tinh (`S6`)**, **Lò Tổ Ong Lục Giác** ở tâm hố, và mảnh ghép **Lò Thổi Thủy Tinh Chân Không** (`inventions.md` §2.9). | **Đánh Thức Tổ Ong:** 07 quét hố, nạp `1 Bình Khí Oxy` khôi phục Lò Tổ Ong, và ngắm 24 chú **Ong thợ** bật nhảy thành vòng cung vàng, nhả **Quả Cầu Thủy Tinh** (cho `Ember Shell` & `Đèn Tảo`) lăn thẳng vào gầu xúc 07. |
| **P3** | **Hành Lang Đài Phun Bụi & Dãy Tháp Chuồng** / *Terminator Dust-Fountain Corridor* (`dust_larks_dawn.png`) | Biển Dài (`0.6`) | *"Vì sao đường chân trời phát sáng vàng-tím, và đàn chim cơ khí kia đang lướt trên cái gì?"* | **Ranh giới ánh sáng động:** Nơi nắng cực vàng ấm `≤ 8°` (`#FFB547`) chạm vào ánh Trái Đất tím lạnh (`#6E738B`), tạo bức màn bụi tĩnh điện bốc cao `30 m` (`0.6-living-moon.md:300`). | **Sơn ca bụi (`S1`)**, **Tháp Chuồng (Roost Towers)**, và **Cối Xay Bụi Tĩnh Điện** (`inventions.md` §2.10). | **Sự kiện Bình minh & Khung giờ Trục vớt:** Khi màn bụi phát sáng, bụi tích điện bám vào phế liệu chôn nửa kín giúp dễ phát hiện (`0.6-living-moon.md:300`), **Cối Xay Bụi** quay `4 RPM`, và sửa **Tháp Chuồng** giúp đón đàn Sơn ca bụi về căn cứ (`+RareEarths / Ilmenite`). |
| **P4** | **Hẻm Rãnh Uốn Lượn & Công Trường Kẻ Bộ Hành** / *Sinuous Rille & Strider Trench* (`dormant_strider_horizon.png`) | Vành Tây → Bắc Biển Dài (`0.6–1.0`) | *"Dòng dung nham cổ đã khắc nên hẻm vực uốn lượn này sao—và cỗ máy 6 chân cao 40 mét ở cuối đường kia là gì?"* | **Địa hình kỳ vĩ:** Kênh dung nham sập rộng `30–60 m` với vách bazan phân tầng (`#3D4159`), đá tảng sáng màu (`#D8CFC2`), mở ra giàn **Đường Ray Phóng Quặng L5** (`inventions.md` §2.7) trên sống núi phía bắc. | **Kẻ Bộ Hành 40 m (`S4`)**, **Máy Đào Rãnh Lumen**, **Én phụt (`S3`)** trên không, và **Sơn ca bụi** đậu trên cần cẩu Kẻ Bộ Hành. | **Tiết lộ Leviathan:** Vừa thoát khỏi hẻm rãnh, bóng **Kẻ Bộ Hành** hiện ra sừng sững dưới đĩa Trái Đất (`north-star.md` Beat 6). Ở `0.9–1.0`, 07 đánh thức nó để chở giá đỡ siêu dẫn qua khe nứt Biển Dài. |
| **P5** | **Giếng Trời Ống Dung Nham & Hang Vọng Âm** / *Lava-Tube Skylight & Echo Cavern* | Dưới lòng Biển Dài (`0.8`) | *"Có gì dưới đáy giếng ánh sáng giữa đồng bằng kia—và sao vách đá lại ngân tiếng hát đáp lời?"* | **Thánh đường ngầm:** Giếng sập sâu `25 m` (`#05060F`) xuyên bởi **cột nắng vàng chéo di chuyển chậm** (`north-star.md` Beat 8) chiếu sáng vách tinh thể olivin (`#3CFFC2`) và **thảm rêu xanh tươi của Theo** (`#7BD389`) trong hầm trú bão. | **Đèn Hiệu Vọng** (`0.6-living-moon.md:61`), **Bướm đèn (`S2`)**, và **Chuông Địa Chấn Khí Oxy** của 07 (`inventions.md` §2.11). | **Không gian có tiếng vang duy nhất trên Mặt Trăng:** Hạ xuống bằng **Tời Cáp Neo** (`inventions.md` §2.9), 07 phát xung nghe (`[Q]`) hoặc gióng **Chuông Địa Chấn (`D2`)**; chuỗi **Đèn Hiệu Vọng** nhấp nháy đáp lại theo gam Rê trưởng dẫn tới hầm trú bão (`Mảnh Khảo Sát 6/6`, `story-v4.md:142`). |
| **P6** | **Gờ Nắng Vĩnh Cửu & Đáy Hố Khói Băng** / *Eternal-Light Heliostat Ridge & Sublimation Bowl* (`plume_drifters_crater.png`) | Hố Bóng Tối (`0.8`) | *"Làm sao mang lửa và ánh mặt trời xuống đáy hố chưa từng thấy nắng suốt hai tỷ năm?"* | **Tương phản cực hạn:** Sống núi hẹp tắm trong nắng xiên vàng ấm suốt `95%` thời gian (`#F4E6C8` / `#FFB547`) đổ thẳng xuống lòng hố sâu `60 m` tối đen ở `-220°C` (`#05060F`) phủ băng lấp lánh (`#5FF3FF` / `#3CFFC2`). | **Sứa khói băng (`S5`)**, **Rô-bốt Gõ Băng**, **Moss** (người bạn), **Tháp Gương Đẩy Nắng** (`inventions.md` §2.8), và **Đạn Cầu Lửa** (`inventions.md` §2.2). | **Lửa Trong Đêm Vĩnh Cửu:** Xoay **Tháp Gương** phóng tia nắng vàng xuống đáy hố để nung **Lõi Muối Nóng Chảy (`Cell Heater`)**; bắn **Đạn Cầu Lửa** làm bùng hoa lửa cam trên băng và nâng 20 chú **Sứa khói băng** bay lên theo cột hơi nước xanh ngọc (`Mảnh Khảo Sát 5/6`). |

---

## 6. "Lịch Bất Ngờ" 120 Phút Đầu Tiên (`0:00 – 2:00`, Mỗi 3–5 Phút Một Khám Phá) `[PROPOSAL]`

Để thế giới không bao giờ rơi vào cảm giác đơn điệu (`north-star.md` §4b), hai giờ chơi đầu tiên (`1 ngày Mặt Trăng trọn vẹn = 120 phút`, `0.6-living-moon.md:104` L1) được nhịp nhàng sắp đặt **một bất ngờ mới về sinh thái, hiện tượng, phát minh hoặc kỳ quan mỗi 3–5 phút**:

| Phút | Pha Ánh Sáng (`L1`) | Vùng / Tiểu khu | Điều Bất Ngờ Người Chơi Gặp | Tầng (`Sinh thái / Địa điểm / Phát minh / Cốt truyện`) |
|---|---|---|---|---|
| **00–04** | Nhật thực → Nắng chiều thấp | Bóng khuất Tàu Đổ Bộ (`Basin`) | **07 thức giấc:** mắt màn trập mở ra nhìn **Trái Đất tối đen 100% không ánh đèn đô thị** với vòng nhật thực đồng đỏ và **đốm sáng L4/L5 của các tàu Ark** (`story-v4.md:121-130`, `[DECIDED]`). | `Cốt truyện / Choáng ngợp` |
| **04–08** | Nắng chiều thấp (`Vàng ấm`) | Sân Nhà & Cửa Khóa Khí | **Mạt pin (`Panel Mites`)** lịch sự tản ra nhường lối khi đèn 07 tới gần (`0.6-living-moon.md:55`); bảng khóa khí hiện `SUIT 01-04 · OFFLINE · 29 OCT 1988` (`[DECIDED]`). | `Sinh thái + Cốt truyện` |
| **08–12** | Nắng chiều thấp | Trạm Phụ Relay.2 | **Tiền bối `06` yên nghỉ** giữ 3 mảnh `Cell Pack II` (`story-v4.md:139`); **Xung Mạng Lưới (Network Pulse)** đầu tiên chạy dọc các trụ (`north-star.md` Beat 7). | `Xác máy + Choáng ngợp` |
| **12–16** | Nắng chiều thấp | Cánh Đồng Pin Phía Tây | **Mạt Pin Lau Cánh Cho 07:** 07 đỗ yên `5 s` dưới nắng; đàn Mạt pin bò lên cánh năng lượng của 07 lau sạch bụi (`+15%` sạc rỉ, `0.6-living-moon.md:220`). | `Tương tác Sinh thái` |
| **16–20** | Nắng chiều thấp | Đáy Thung Lũng (Vệt Kestrel) | Sửa **Tilly** (drone bầu bạn); vệt tia sáng trắng dẫn tới vệ tinh **Kestrel-3** rơi vẫn chĩa ăng-ten về phía tàu Ark L5 (`story-v4.md:74`). | `Người bạn + Địa điểm` |
| **20–25** | Sắp Hoàng Hôn | Vòm Gara → Kho Tiếp Liệu | Quét vòm ống đồng bị gãy của **Ống Bưu Chính Khí Nén** (`inventions.md` §2.4); gặp **Mule** nghiêng mình bên cột đèn cuối cùng (`0.6-living-moon.md:275`). | `Phát minh + Xác máy` |
| **25–30** | Chạng Vạng | Giàn Khoan (`Mẫu Lõi 41`) | **Tốp Én Phụt (`S3`) Bay Ngang Bầu Trời!** Ba cụm khói trắng vẽ đường parabol qua bầu trời sẫm tối từ trại khoan của Ines vào lưới đón trụ tiếp sóng (`puff_swifts_flight.png`). | **`Sự Kiện Bầy Đàn #1`** |
| **30–35** | **Đêm Đầu Tiên Buông Xuống (Beat 1)** | Gờ Ngắm Thung Lũng Nhà | **Khoảnh khắc Choáng ngợp:** Bóng tối tràn qua thung lũng; pin mặt trời tắt dần; cửa sổ trạm và các trụ đèn bật sáng nối tiếp nhau như một ngôi làng lúc lên đèn (`north-star.md:146`). | `Khoảnh khắc Choáng ngợp` |
| **35–40** | Đêm Ánh Trái Đất (`Tím dịu`) | Sân Nhà & Hàng Cột Đèn | **Đàn Bướm Đèn (`S2`) Tụ Hội!** Được đèn pha 07 thu hút, 16 chú bướm bụng đèn hổ phách bay quanh 07 và 2 chú đậu lên mái màn hình CRT (`lantern_moths_night.png`). | **`Sự Kiện Bầy Đàn #2`** |
| **40–45** | Đêm Ánh Trái Đất | Mép Hẻm Núi Thầm Thì | **Đom Đóm Cơ Khí (`Machine Fireflies`)** nhấp nháy dọc vách vực; quét máy nhảy của Kenji mở khóa **Bộ Đẩy Khí Lạnh Bậc 1 (`Hover-Jump`)** (`inventions.md` §2.5) để bay qua khe vực. | `Trang bị + Choáng ngợp` |
| **45–50** | Đêm Ánh Trái Đất | Cuối Hẻm Núi Thầm Thì | Tìm thấy và đưa **Bell** (tủ radio 4 chân của Ro) về nhà; cuộn băng `"Lumen After Dark"` năm 1988 vang lên ấm áp giữa đêm. | `Người bạn + Âm nhạc` |
| **50–55** | Đêm Sâu | Vành Tây (`Lối vào Rãnh`) | **Chớp Thiên Thạch Vi Mô (`0.6-living-moon.md:301`):** Ánh chớp trắng tĩnh lặng trên sống núi + vòm mảnh bắn bắt nắng trên cao, để lại **thủy tinh va chạm** nóng đỏ. | `Hiện tượng Mặt Trăng` |
| **55–60** | Rạng Đông | Cửa Hẻm Rãnh (`0.6` Seam) | **Mở Ra Toàn Cảnh (Beat 6):** Lái khỏi hẻm rãnh uốn lượn mở ra **Biển Dài**, **Cánh Đồng Xoáy Từ** trắng ngà, và nơi chân trời xa là **Kẻ Bộ Hành 40 m (`S4`)**! | **`Kỳ Quan Leviathan`** |
| **60–65** | **Bình Minh Đầu Tiên (Beat 3)** | Hành Lang Đường Ranh Sáng | **Vũ Điệu Đàn Sơn Ca Bụi (`S1`)!** 60 tàu lượn cánh lưới vàng cưỡi lên màn bụi tĩnh điện phát sáng, lượn vòng số 8 ngang qua đĩa Trái Đất (`dust_larks_dawn.png`). | **`Sự Kiện Bầy Đàn #3`** |
| **65–70** | Pha Nắng Sớm | Bãi Pin Mặt Trời Biển Dài | Khôi phục **Rô-bốt Quét Bụi (`Sweeper`)** mở lại con đường nhẵn; **Máy Đào Rãnh (`Trencher`)** tiếp tục đào rãnh cáp thẳng tắp (`0.6-living-moon.md:56, 58`). | `Sinh thái Mặt đất` |
| **70–75** | Pha Nắng | Mỏ Ilmenit & Trạm Thời Tiết | Quét mở khóa **Lò Thở Ilmenit** (`inventions.md` §2.1) & **Cối Xay Bụi Tĩnh Điện** (`inventions.md` §2.10); căn cứ bắt đầu thở ra những **Bình Khí $\text{O}_2$ Đồng Thau**! | `Mở khóa Phát minh` |
| **75–80** | Pha Nắng | **Cánh Đồng Xoáy Từ (`P1`)** | **Đàn Cá Đuối Từ Trường (`S7`)!** La bàn 07 xoay tròn và CRT gợn sóng từ tính cho tới khi 12 chiếc cá đuối trắng–xanh kè hai bên hông 07 dọc dải trắng (`lunar_swirl_field.png`). | **`Sự Kiện Bầy Đàn #4 + Kỳ quan`** |
| **80–85** | Pha Nắng | Tháp Chuồng Biển Dài & Nhà | 07 khôi phục **Tháp Chuồng (Roost Tower)**; cả đàn **Sơn ca bụi** xoắn ốc đáp xuống vòng đồng, nhận căn cứ làm tổ và trút `RareEarths` + `Ilmenite`. | `Nhận nuôi Bầy đàn` |
| **85–90** | Nắng Trưa Nghiêng | **Hố Thủy Tinh Cam (`P2`)** | **Đàn Ong Thợ Thủy Tinh (`S6`)!** Giữa lòng hố cam lấp lánh, 24 chú ong lò nung bật nhảy thành vòng cung vàng và lăn **Quả Cầu Thủy Tinh** vào gầu xúc 07 (`glassweaver_bees_swarm.png`). | **`Sự Kiện Bầy Đàn #5 + Kỳ quan`** |
| **90–95** | Nắng Trưa Nghiêng | Chảo Thu Biển Dài & Đoàn Xe | Lắp pin dự phòng đánh thức **Đoàn 3 Xe Tải (`Convoy`)** (`0.6-living-moon.md:172`); thắp sáng Chảo thu phát bức điện **Dr. Lena Moreau hẹn trực sóng Chủ Nhật suốt 36 năm** (`story-v4.md:75`). | `Sinh thái + Cốt truyện` |
| **95–100** | Nắng Chiều | Bậc Thềm Nhà Kính (`0.6–0.7`) | **Đàn Bướm Đèn** dẫn 07 tới **Nhà Kính Thử Nghiệm Số 3** (`inventions.md` §2.3): luống khoai tây năm 1988 đã đóng băng khô, nhưng cột **Đèn Tảo Sống** vẫn thở bọt $\text{O}_2$ ấm áp! | `Địa điểm + Phát minh` |
| **100–105** | Nắng Chiều | Dưới Chân Kẻ Bộ Hành (`P4`) | **Đứng Dưới Gầm Kẻ Bộ Hành 40 m (`S4`):** 07 lái vào giữa 6 bàn chân khổng lồ (`dormant_strider_horizon.png`), quét khoang nạp Đường Ray Phóng L5. | **`Cận Cảnh Leviathan`** |
| **105–110** | Hoàng Hôn Thứ Hai | Tuyến Địa Chấn Biển Dài | 07 kích hoạt **Chuông Địa Chấn Khí Oxy (`D2`, `inventions.md` §2.11)**: sóng rung rũ sạch bụi phủ trên một hốc cổ vật và vẽ bản đồ vòm **Ống Dung Nham** bên dưới! | `Phát minh + Hiện tượng` |
| **110–115** | Đêm Thứ Hai | Miệng Hố Bóng Tối (`P6`) | Căn chỉnh **Tháp Gương Đẩy Nắng** (`inventions.md` §2.8) phóng tia nắng vàng cắt ngang bóng tối vĩnh cửu; **07 bắn một Đạn Cầu Lửa (`inventions.md` §2.2)**! | `Khoảnh khắc Phát minh` |
| **115–120** | Đêm Vĩnh Cửu / Hoa Lửa | Đáy Hố Bóng Tối (`P6`) | **Đàn Sứa Khói Băng (`S5`) Bung Nở!** 20 chiếc dù xanh ngọc–vàng mở tung giữa cột hơi nước xanh lơ của quả cầu lửa, rắc `Water Ice` tinh khiết vào Diều Hứng Hơi của 07 (`plume_drifters_crater.png`)! | **`Sự Kiện Bầy Đàn #6 Cao Trào`** |

---

## 7. Các Phương Án Thiết Kế & Hợp Đồng Kỹ Thuật (`[PROPOSAL]`)

### 7.1 Ba Phương Án Hệ Sinh Thái & Xếp Hạng (`RULES.md` §"Facts and creativity")
1. **Phương án A — Bầy Đàn Sự Kiện Dựa Trên Vật Lý Chân Không + Tổ/Tháp Khôi Phục Được (KHUYÊN DÙNG, Hạng #1):**
   - *Thiết kế:* 6 bầy đàn instanced (`S1–S3, S5–S7`) + 1 khổng lồ chân trời (`S4`), gắn chặt với pha ánh sáng L1, kỳ quan địa chất và phát minh của người chơi (`Đạn Cầu Lửa`, `Tháp Chuồng`, `Lò Tổ Ong`).
   - *Lý do xếp #1:* Giữ trọn sự tĩnh lặng làm nền (`VISION.md` Pillar 6), giải thích hợp lý 100% cách bay trong chân không, và kết nối bầy đàn trực tiếp vào vòng lặp chế tạo (`inventions.md`).
2. **Phương án B — Đàn Drone Bay Tự Do Ngẫu Nhiên Khắp Bản Đồ (Hạng #2):**
   - *Thiết kế:* Các đàn drone sinh ngẫu nhiên mỗi 3 phút ở mọi vùng.
   - *Lý do thấp hơn:* Biến bầy đàn thành cảnh nền nhàm chán, phá vỡ vẻ cô đơn mênh mông của Biển Dài (`0.6-world-regions.md:25`).
3. **Phương án C — Bầy Đàn Chỉ Xuất Hiện Trong Cutscene Cố Định (Hạng #3):**
   - *Thiết kế:* Chỉ chạy hoạt cảnh dựng sẵn một lần khi 07 đi qua điểm kích hoạt.
   - *Lý do thấp nhất:* Vi phạm Quy tắc 5 của `0.6-living-moon.md:45` (*"Chỉ dùng đầu vào hệ thống... Không dùng cutscene kịch bản cho sinh thái"*) và làm mất niềm vui khôi phục Tháp Chuồng / Lò Tổ Ong.

### 7.2 Hợp Đồng Kỹ Thuật C# (Cho Giai Đoạn Triển Khai Tiếp Theo)
- `MoonProject.Core`: mở rộng enum `SpeciesId` (`DustLark`, `LanternMoth`, `PuffSwift`, `RegolithStrider`, `PlumeDrifter`, `GlassweaverBee`, `SwirlRay`) và thêm sự kiện `SwarmRoostRestored(SpeciesId species, Vector3 position)`.
- `MoonProject.Gameplay.Ecology`: `SwarmBoidSimulator` (C# thuần, kiểm thử được trong EditMode trên `Span<SwarmAgentState>`, `0 B` GC) + `InstancedSwarmRenderer` (`Graphics.DrawMeshInstancedProcedural`, 1 draw call cho mỗi bầy đang hoạt động).
