# Tóm tắt Story v4: "Cuộc Rời Đi" (The Departure) — Bản Tóm Tắt Cho Owner (0.5.7)

> **Tài liệu đầy đủ (tiếng Anh):** `docs/design/story-v4.md`
> **Tinh thần chủ đạo:** Giữ phong cách retro-future thập niên 1980 (màn hình CRT hổ phách, băng cassette, máy móc analog) và tiền đề *"con người đã rời bỏ Trái Đất"* như Fallout, nhưng **tuyệt đối không** có bạo lực, máu me, quái vật hay châm biếm đen tối (`docs/VISION.md`). Thế giới của Lunarion vẫn yên bình, ấm áp, cô đơn nhưng đầy hy vọng. Mọi chi tiết đều tuân thủ logic thực tế: con người chỉ giao tiếp với máy móc qua giao thức vận hành và ghi chú bảo trì, không tâm sự ủy mị với robot (`Logs/story-v3.md` §4).

---

## 1. Dòng thời gian lịch sử giả tưởng (Alternate Timeline) `[ĐỀ XUẤT]`

- **1969 — Đặt chân lên Mặt Trăng & Hiệp ước Mặt Trăng Quốc tế:** Sau chuyến đáp năm 1969, các quốc gia hợp lực thành lập **Ủy ban Mặt Trăng Quốc tế (ILC)** thay vì dừng chương trình Apollo.
- **1971–1978 — Mặt Trăng thành trạm trung chuyển:** Nhờ trọng lực thấp ($1/6\,g$), Mặt Trăng trở thành xưởng đóng tàu và trạm trung chuyển nhiên liệu lên quỹ đạo. Tuy nhiên, các mỏ vùng xích đạo hoàn toàn khô hạn nên nước vẫn phải chở từ Trái Đất lên.
- **1979–1983 — Khủng hoảng "Mùa Đông Khói Mù" (*The Great Stagnation*):** Khí thải công nghiệp tích tụ cùng sự kiện giải phóng băng methane Bắc Cực tạo ra một lớp màn sương aerosol khóa chặt tầng bình lưu Trái Đất. Ánh nắng mặt đất giảm 55%, mùa màng toàn cầu sụp đổ, và lượng oxy bề mặt giảm dần trong nhiều thập kỷ. Không có chiến tranh hay xác sống — chỉ có một hoàng hôn lạnh kéo dài buộc nhân loại phải sơ tán trong vòng 10 năm.
- **1980–1987 — Chương trình Tàu Cứu Sinh (The Arks) & Lý do công nghệ dừng ở thập niên 1980:** Toàn bộ nhà máy trên Trái Đất ngừng sản xuất đồ tiêu dùng để dồn tài nguyên đóng **12 Tàu Cứu Sinh (Ark I – Ark XII)** — các trạm không gian hình trụ khổng lồ quay tạo trọng lực tại điểm Lagrange **L4/L5** giữa Trái Đất và Mặt Trăng. Vì mọi nguồn lực dồn vào vỏ tàu và hệ thống sinh tồn, công nghệ máy tính đóng băng ở chuẩn thập niên 1980: màn hình CRT hổ phách, băng từ cassette, rơ-le cơ học bền bỉ trước bức xạ. Các căn cứ Mặt Trăng đóng vai trò **trạm trung chuyển (waystation)** đưa người và vật tư lên các tàu Ark.
- **1984–1988 — Trạm Lumen & Trạm Meridian:** Các tàu Ark có thể tự duy trì trên quỹ đạo nhiều thế hệ, nhưng muốn hạ cánh định cư lâu dài trên Mặt Trăng thì bắt buộc phải có **trữ lượng băng nước khổng lồ**. Năm 1984, ILC lập **Trạm Lumen** ở cực Nam với 4 thành viên (**Ines, Kenji, Theo, Ro**), còn tập đoàn khai khoáng **Tycho Consortium** lập trạm tự động **Meridian** ở bờ vực phía Bắc.
- **14/10/1988 — Phán quyết "Khô hạn":** Các mũi khoan nông chỉ thấy sương giá. Trước khi đợt tàu cuối cùng rời Trái Đất (*Cuộc Rời Đi*), Hội đồng Ark kết luận cực Nam Mặt Trăng **"KHÔ HẠN — KHÔNG ĐỦ NƯỚC ĐỂ ĐỊNH CƯ"** và lệnh cho nhóm Lumen thu dọn lên chuyến tàu vét `TRANSFER-19` về tàu **Ark IV (*Vanguard*)**.
- **26–28/10/1988 — Phát hiện của Ines & Cơn bão mặt trời định mệnh:** Chỉ 48 giờ trước giờ rời trạm, mũi khoan sâu 140 m của Ines xuyên qua lớp đá bazan và chạm vào **tầng ngậm nước băng cổ đại khổng lồ** (đủ nuôi sống cả 12 tàu Ark), khớp hoàn toàn với dữ liệu radar từ trạm Meridian. Khi cả nhóm chuẩn bị truyền gói dữ liệu chứng minh qua chảo phát trên Đỉnh (Peak) lúc 22:00 ngày 28/10, **cơn bão hạt mặt trời lịch sử (SPE-88)** ập đến.
- **Đêm 28/10/1988 — Sự hy sinh của 4 thành viên:** Bão đánh rơi vệ tinh **Kestrel-3**, làm kẹt chảo phát ở mức `14%`, và làm vỡ gioăng mái vòm chính của trạm Lumen. Thay vì bỏ mặc dữ liệu để chạy ngay, cả 4 người mặc đồ bảo hộ ở lại vị trí để cứu bằng chứng nước cho nhân loại:
  - **Kenji** chuyển toàn bộ rover (gồm cả 07 dưới chân tàu đổ bộ) và Giao thức Khôi phục sang chế độ **`CONTINUE`** (tiếp tục chạy tự động);
  - **Ro** khóa máy phát radio ở chế độ phát tín hiệu định vị Chủ nhật liên tục (`142.8 MHz`);
  - **Theo** đóng chặt cửa hầm trú ẩn ống dung nham từ bên ngoài để bảo vệ vườn rêu chạy pin đồng vị và máy chủ sao lưu;
  - **Ines** niêm phong dữ liệu lõi khoan.
  Bị bão bụi tĩnh điện và bức xạ cắt đứt đường ra tàu đón, cả 4 người lặng lẽ cạn dưỡng khí trong bộ đồ bảo hộ bên trong khu nhà chính đã sập kín. *(Lưu ý: Game tuyệt đối không có cảnh xác chết hay xương cốt; 07 chỉ đọc được tín hiệu đồ bảo hộ ngừng hoạt động lúc `04:12 UTC 29/10/1988` trên bảng điều khiển ngoài cửa khóa khí).*
- **1988–2024 — 36 năm im lặng & Lúc 07 thức giấc:** Tưởng cực Nam không có nước và nhóm Lumen đã tử nạn, 12 tàu Ark đóng kín ở quỹ đạo chờ L4/L5. Trên Mặt Trăng, giao thức tự động lần lượt đánh thức các robot đàn anh **01–06** đi gom dữ liệu khảo sát, nhưng mỗi chiếc đều hết pin hoặc kẹt lại dọc đường. Trong khi đó, trạm tự động **Meridian** trên sườn núi đầy nắng vẫn âm thầm chạy suốt 36 năm, lập bản đồ toàn bộ túi nước ngầm. Năm **2024**, một xung quét tự động ngày Chủ nhật từ **Ark IV** chạm vào ăng-ten của **06**, kích hoạt nguồn phụ và đánh thức **07**.

---

## 2. Mở đầu (First-Hour Hook), Thang Bí Ẩn (Mystery Ladder) & Kết Cục

### Mở đầu (Giờ chơi đầu tiên)
1. **Hình ảnh đầu tiên:** 07 mở mắt CRT màu hổ phách giữa bóng tối của **nhật thực Trái Đất**. Phía trên rặng núi là quả địa cầu tối đen viền ánh đồng — **hoàn toàn không còn một ánh đèn thành phố nào trên mặt tối của Trái Đất**.
2. **Hành động & Manh mối đầu tiên:** 07 thay pin dự phòng bên tàu đổ bộ chở hàng (cargo lander), sửa khoang sạc, chờ bình minh lên cấp điện cho cột pin mặt trời, rồi **quét (scan)** tấm bảng chuyến bay trên tàu đổ bộ: `CHUYẾN TÀU TRANSFER-19 · ĐIỂM ĐẾN: ARK IV (L5) · HÀNH KHÁCH: KALANI, MORROW, LINDQVIST, AMADI · ĐÃ LÊN TÀU: 0 / 4`.
3. **Vòng lặp xây căn cứ:** Thay vì dọn dẹp đống đổ nát của con người, 07 quét các phế tích và máy móc để học bản vẽ, tự xây dựng căn cứ robot của riêng mình và chế tạo phương tiện để tiến ra các vùng xa.

### Thang bí ẩn qua từng vùng (Mystery Ladder)
- **Vùng 1 — Basin & Canyon (0.5):** Quét mái vòm Lumen bị sập, giàn khoan của Ines và xác vệ tinh **Kestrel-3** (mang phù hiệu *Tycho Consortium · Meridian*). Người chơi hiểu về *Cuộc Rời Đi* năm 1988, cái chết của 4 thành viên trong cơn bão, và thu được **Mảnh khảo sát 1/6 (Nhật ký lõi khoan của Ines)**. Giải cứu **Tilly** và **Bell**.
- **Vùng 2 — The Long Mare (0.6):** Sửa chảo liên lạc vùng Mare để tải bộ nhớ đệm **10 bức điện tháng 10/1988** giữa Trái Đất, tàu Ark IV, Lumen và Meridian — thấy rõ gói dữ liệu nước đã bị ngắt ở `14%` đúng đêm bão. Thu được **Mảnh khảo sát 2/6 (Dữ liệu đàn drone dò tìm)** và **3/6 (Gói dữ liệu truyền dở năm 1988)**.
- **Vùng 3 — Rim Terraces / Trạm Meridian (0.7 — Bước ngoặt giữa game):** Lên tới thềm núi trắng ngập nắng, người chơi phát hiện **Trạm Meridian vẫn đang hoạt động suốt 36 năm** dù không có con người đọc kết quả. Trong kho dữ liệu (Vault) của Meridian là bản đồ radar 3D hoàn chỉnh của biển băng ngầm (**Mảnh khảo sát 4/6**), cùng chỉ thị cho thấy **Hạm đội Ark ở quỹ đạo L5 vẫn đang thức canh sóng `142.8 MHz` mỗi Chủ nhật** để chờ bằng chứng nước trước khi vỏ tàu của họ xuống cấp.
- **Vùng 4 — Shadowed Crater & Lava Tube (0.8):** Dưới miệng hố tối đen, 07 cấp lại điện cho nhóm robot đào băng và gặp **Moss** để lấy **Mẫu lõi băng nguyên chất (5/6)**. Trong hầm trú ẩn ống dung nham — nơi Theo đã khóa chặt cửa từ bên ngoài trước khi mất — viên pin đồng vị vẫn giữ sáng chiếc đèn trồng cây suốt 36 năm: **vườn rêu xanh của Theo vẫn sống**, nằm cạnh **Bản tổng hợp địa chất có chữ ký của Ines (6/6)**.
- **Kết cục — Đỉnh Peak (1.0):** 07 sửa xong chảo phát lớn trên Đỉnh và truyền trọn bộ **6/6 dữ liệu khảo sát** lên tọa độ L5. Sau 5 giây tĩnh lặng, **Trạm canh Ark IV (*Vanguard*)** và **Tiến sĩ Lena Moreau** (người bạn khoa học năm xưa của Ines, nay đã 68 tuổi trên tàu Ark IV) hồi đáp: xác nhận trữ lượng nước vượt ngưỡng gấp 40 lần, phát lệnh cho cả 12 tàu Ark chuẩn bị hạ cánh về định cư tại Mặt Trăng, và phát lại băng nhạc *"Lumen After Dark"* của Ro trên toàn hạm đội.

---

## 3. Những gì thay đổi trong bản game hiện tại (Checklist)

| Hạng mục / File | Quyết định | Chi tiết thay đổi cụ thể |
|---|---|---|
| **Bầu trời & Đèn thành phố Trái Đất** (`LofiEarth.shader`, `SkyCycleSettings.cs`, `SkySettings.cs`) | **THAY ĐỔI** | Đặt `_eclipseCityLightStrength = 0.0f` và tắt lớp đèn đô thị trong shader để **các lục địa trên mặt tối Trái Đất tối hoàn toàn**. Chỉ giữ lại **viền khúc xạ khí quyển màu đồng hổ phách** khi nhật thực và **vòng cực quang tự nhiên mờ nhạt ở hai cực**. Ở cảnh kết, đốm sáng trả lời không nháy trên mặt đất mà nháy **ngoài không gian tại cụm tàu Ark ở điểm L5 (lệch $60^\circ$ cạnh Trái Đất)**. |
| **Nhân vật, Trạm, Robot đàn anh (01–06) & Bạn bè (Tilly, Bell, Atlas, Moss)** | **GIỮ NGUYÊN** | Giữ nguyên tên và vai trò của 4 thành viên (Ines, Kenji, Theo, Ro), 2 trạm (Lumen, Meridian / Tycho), các robot 01–06 nằm nghỉ, và các bạn robot (`Logs/TASK.md`). |
| **Mốc năm & Toàn bộ 20 bức thư radio** (`docs/design/text/radio-letters.md`, `letter.mast_1..10`, `letter.after_1..10` trong `en.json` / `vi.json`) | **THAY ĐỔI** | Bỏ mốc năm `2047–2067` và cốt truyện 4 thành viên về sống ở Trái Đất. Thay `letter.mast_1..10` bằng **10 bức điện tháng 10/1988** (quá trình rời Trái Đất, phán quyết khô hạn, phát hiện băng của Ines – Moreau, và đêm bão SPE-88). Thay `letter.after_1..10` bằng **10 bản tin từ Hạm đội Ark năm 2024** chuẩn bị hạ cánh xuống Mặt Trăng. |
| **Nhật ký phi hành đoàn** (`docs/design/text/crew-logs.md`, `log.*` trong `en.json` / `vi.json`) | **GIỮ 4 / SỬA 8** | **Giữ:** `log.kenji_2`, `log.ines_1`, `log.theo_1`, `log.theo_3`. **Sửa:** `log.ro_1..3`, `log.kenji_1`, `log.kenji_3`, `log.ines_2..3`, `log.theo_2` để bỏ các câu tâm sự ủy mị với 07 và khớp với việc cả nhóm ở lại giữ dữ liệu trong đêm bão 28/10/1988. |
| **Màn hình khởi động & Bảng khôi phục** (`lens.boot.signal`, `recovery.answer.*`, `ticker.ending.answer`) | **THAY ĐỔI** | Đổi `EARTH SIGNAL · DETECTED` thành `ORBITAL SIGNAL · 142.8 MHz`; đổi `Answer Earth` thành `Transmit Survey`; đổi câu kết từ Ro nói với 07 sang phản hồi chính thức từ **Ark Four Watch**. |
| **Kỷ vật (Keepsakes), Băng nhạc (Cassettes), Thùng hàng (Crates), Bản đồ (`map.*`)** | **GIỮ NGUYÊN** | 5 kỷ vật (vịt cao su, đĩa vàng Voyager 1977, giày bảo hộ, ấm trà, tượng lùn) và 3 cuộn băng cassette cực kỳ hợp với thập niên 1980. Bỏ kỷ vật Walkman (`relic.cassette_player.*`) khi tính năng C9 hoàn tất (vì đầu băng `kit.tape_deck` đã nằm trong thùng phụ tùng `07 · SPARE`). |

---

## 4. Năm câu hỏi mở cần Owner chốt (Open Questions)

1. **Nguyên nhân khiến Trái Đất không thể ở được vào thập niên 1980:**
   - **Phương án A (Khuyên dùng) — "Mùa Đông Khói Mù" (*The Great Stagnation*):** Lớp sương mù aerosol và khí methane khóa chặt tầng bình lưu từ 1979–1983 làm giảm 55% ánh nắng, mất mùa toàn cầu và giảm dần oxy bề mặt — không máu me, giải thích hợp lý vì sao nhân loại có 10 năm để đóng tàu Ark và vì sao Trái Đất trên bầu trời ban ngày phủ một lớp mây mù màu kem hổ phách.
   - **Phương án B — Suy giảm từ quyển & Bão bức xạ mặt trời kéo dài:** Làm thủng tầng ozone và khiến bề mặt không thể trồng trọt.
   - **Phương án C — Để mở (Không nêu tên khoa học cụ thể):** Chỉ nhắc tới *"tầng khí quyển khép lại"* và *"các vành đai nông nghiệp ngừng hoạt động"*.
2. **Cách 07 phát hiện sự hy sinh của 4 thành viên đêm bão 28/10/1988 (Đảm bảo không có xác chết/kinh dị):**
   - **Phương án A (Khuyên dùng) — Khu nhà chính bị sập kín + Đọc tín hiệu đồ bảo hộ ở cửa khóa khí:** Không bao giờ thấy thi thể ngoài đời. Khi 07 quét cửa khóa khí bị vùi lấp, màn hình hiện dòng trạng thái vận hành cuối cùng (`SUIT 01–04 · OFFLINE · 29 OCT 1988 · 04:12 UTC`), còn dụng cụ của họ vẫn nằm tại nơi họ làm việc trước khi rút vào cửa hầm.
   - **Phương án B — Bốn bia đá nhỏ do robot 01 dựng theo giao thức tự động** cạnh tàu đổ bộ sau cơn bão.
   - **Phương án C — Khoang thoát hiểm nhỏ gặp sự cố trong bão từ** và rơi xuống vực sâu không thể tiếp cận.
3. **Mặt tối của Trái Đất khi nhật thực có còn ánh sáng nào không?**
   - **Phương án A (Khuyên dùng) — Lục địa tối 100%, chỉ có viền khí quyển màu đồng và cực quang tự nhiên mờ ở hai cực:** Nhìn một phát hiểu ngay các thành phố đã tắt đèn; dành trọn vẹn hiệu ứng "một đốm sáng nhân tạo bật lên" cho cụm tàu Ark ở điểm L5 lúc kết game.
   - **Phương án B — Duy nhất 1 chấm đèn hiệu thời tiết tự động nhấp nháy ở xích đạo** (trạm đo khí quyển không người lái chạy pin đồng vị gửi số liệu mây lên tàu Ark).
   - **Phương án C — Các tia chớp tĩnh điện màu hổ phách** thỉnh thoảng lóe nhẹ trong dải mây xích đạo.
4. **Vị trí hiện tại của Hạm đội Tàu Cứu Sinh (Arks) khi 07 thức dậy năm 2024:**
   - **Phương án A (Khuyên dùng) — Quỹ đạo Halo tại điểm Lagrange L4/L5 giữa Trái Đất – Mặt Trăng (cách Trái Đất $60^\circ$ trên bầu trời):** Hợp lý nhất về vật lý (luôn có nắng, không tốn nhiên liệu giữ quỹ đạo, độ trễ tín hiệu chỉ 1,3 giây, và chảo phát trên Đỉnh có thể quay rõ ràng sang cụm sao L5 cạnh Trái Đất).
   - **Phương án B — Quỹ đạo Trái Đất tầm cao (ngay sát rìa khí quyển Trái Đất).**
   - **Phương án C — Quỹ đạo xa quanh Mặt Trời / Vành đai tiểu hành tinh** (mất vài giờ/ngày tín hiệu mới vọng về).
5. **Ai là người trả lời tín hiệu của 07 ở cảnh kết (năm 2024, 36 năm sau cơn bão)?**
   - **Phương án A (Khuyên dùng) — Trạm canh Ark IV + Tiến sĩ Lena Moreau (nhà khoa học của trạm Meridian năm 1988, nay đã 68 tuổi trên tàu Ark IV):** Vì 4 thành viên Lumen đã mất năm 1988, sự xuất hiện của Tiến sĩ Moreau (người cộng sự từng làm việc với Ines qua radio năm 1988 và kiên trì giữ lịch nghe sóng Chủ nhật suốt 36 năm) mang lại cảm xúc ấm áp, trọn vẹn và hoàn toàn hợp logic.
   - **Phương án B — Thế hệ kế tiếp trên tàu Ark IV** (học trò cũ của Ro hoặc cháu của Ines trực đài canh cùng Hội đồng Hạm đội).
   - **Phương án C — Máy tính dẫn đường của Ark IV xác nhận tự động trước, sau đó nhân viên trực đài mới lên tiếng.**
