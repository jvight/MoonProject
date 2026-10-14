# Kế Hoạch Tổng Thể Lunarion (0.5.5 → 1.0)

> **Phiên bản tài liệu:** Vòng 2 — Đã đối chiếu trực tiếp với mã nguồn C# trên nhánh `0.5` và tài liệu
> phản hồi đã chốt [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md).
>
> **Quy ước gắn nhãn bắt buộc (áp dụng xuyên suốt, đặc biệt tại Mục 2, 3, 4 và 7 — không để bất kỳ câu khẳng định nào không có nhãn):**
> - **`[HIỆN TẠI]`**: Sự thật đang chạy trong mã nguồn C# hôm nay hoặc đã chốt trong
>   [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md), kèm đường dẫn file xác thực.
> - **`[ĐỀ XUẤT]`**: Đề xuất thiết kế trình chủ dự án duyệt (cho các bản `0.5.6`, `0.5.7`, `0.6+` hoặc
>   bản nháp `story v3`), kèm một dòng lý do cụ thể.

---

## 1. Tóm tắt cho Chủ dự án (Executive Summary)

Bản kế hoạch vòng 2 này sửa toàn bộ 12 điểm chưa khớp giữa tài liệu quy hoạch vòng 1 với mã nguồn C#
thực tế của **Lunarion** và các quyết định của chủ dự án ngày `2026-10-14`:

1. **Giữ nguyên quyền sử dụng Cắt (`Cutter`) và Đào (`ExcavationSystem`) ngay từ những phút đầu tiên:**
   - **Cắt (`Cutter`)**: `[HIỆN TẠI]` 07 cắt được các mảnh phế liệu tại Kho tiếp tế, mối hàn thùng tại
     Gara của Kenji và bãi mảnh vỡ vệ tinh Kestrel-3 ngay từ đầu game với cụm tụ nạp 3 tầng `ChargeCells`
     ([`Assets/_Project/Scripts/Gameplay/Salvage/Cutter.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/Cutter.cs),
     [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §5).
     Không khóa cơ chế cắt sau bất kỳ nâng cấp nào.
   - **Đào (`ExcavationSystem`)**: `[HIỆN TẠI]` 07 đào được kỷ vật chôn dưới đất, ống kính của Tilly
     dưới xe trượt và 3 bộ phận của Bell dưới các đống đá sạt ngay từ đầu game
     ([`Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs),
     [`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs)).
     `[ĐỀ XUẤT]` Trong thiết kế `07 v2`, gầu xúc cơ khí phía trước (`DigScoop`) là **thiết bị khởi đầu có
     sẵn trên 07 từ phút 0** để thay cho phễu sáng hút đồ phi lý từ mắt camera, tuyệt đối không dời kỹ
     năng đào thành đồ mở khóa ở hẻm núi.
2. **Khớp 100% thông số Quét, 7 bộ Bản vẽ (`18 mảnh`), Bạn đồng hành, Pin và Lắng nghe với code:**
   - **Quét (`ScanTuning.cs`)**: `[HIỆN TẠI]` Giữ đúng thời gian quét `1.5 s` (nhỏ) / `2.5 s` (vừa) /
     `4.0 s` (lớn), tầm `8 m`, góc nón `25°`, tan mảnh `0.9 s`, viền bụi `2.0 s`, mảnh trùng thưởng
     `+3 Metal`, khung góc hiện từ `15 m`, và `Focus Lens` tăng tốc quét `1.5×` kèm mở rộng khung góc
     lên `25 m` ([`Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs)).
   - **7 bộ bản vẽ 0.5 (`18 mảnh`)**: `[HIỆN TẠI]` Giữ đúng tên và vị trí thực tế của `Cargo Cradle`,
     `Warm Headlamp`, `Hover-Jump`, `Boost Coils`, `Cell Pack II`, `Focus Lens`, `Side Bins`
     ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3 & §1.7,
     [`Assets/_Project/Scripts/Gameplay/Progression/TechTree.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/TechTree.cs)).
   - **Tilly và Bell**: `[HIỆN TẠI]` Tilly (`DroneBody`, drone bay khảo sát) có 3 bộ phận nằm trong xe
     trượt gặp nạn `TillySled` (1 cắt chốt kẹp, 1 đào dưới gầm nghiêng, 1 cắt bầu cảm biến); sau khi
     sửa xong sẽ bay chỉ điểm (`spotter`), tự nhặt các bó vật liệu dư về kệ nhà (`TillyFetch`), dẫn
     đường khi cạn pin (`BatteryField`) và tặng tấm pin mặt trời `SolarCell`.
     `[HIỆN TẠI]` Bell (`RadioCabinetBody`, robot tủ đài đi bằng 4 chân máy quay phim — không phải drone
     bay) có 3 bộ phận nằm dưới 3 đống đá sạt `RockfallPocket` trong hốc hẻm núi để 07 đào lên
     (`FriendField.LayThePockets`); sau khi sửa xong sẽ đi bộ về góc nhà (`BellCorner`), đổi kênh radio
     khi 07 chạm tia vào núm vặn (`radio_dial`), reo vui khi có kỷ vật mới và bắt sóng tín hiệu (`BellSignals`).
   - **Pin (`BatteryModel`, `BatteryTuning`, `BatteryField`)**: `[HIỆN TẠI]` Hệ thống tính theo ô pin
     (`4` ô đầu game, `6` ô khi lắp `Cell Pack II`, tối đa `8` ô): lái xe tốn `1 ô / 120 m` đường bằng
     (lên dốc `1.5×`, xuống dốc `0.5×` ở `15°`, kéo đồ `1.25×`, tăng tốc `2.0×` trừ vào tụ `Boost Coils`
     `0.25 ô` trước), dùng tia công cụ tốn `1 ô / 60 s`, `Hover-Jump` tốn `0.05..0.25 ô`, sạc ở Đế sạc
     nhà `4 s/ô`, sạc ở vòng sáng trạm tiếp sóng `5 s/ô`, pin mặt trời của Tilly sạc nhỏ giọt `180 s/ô`
     (tối đa `1 ô`), bò dự phòng ở `0` ô giới hạn `35%` tốc độ (tăng lên `55%` sau `5 s` khi Tilly dẫn đường).
   - **Lắng nghe (`SonarSystem`, `ListenGate`, `SiteMarker`)**: `[HIỆN TẠI]` Cơ chế bị động không nút bấm
     (từ Tháp cấp 1, sau khi lái `≥ 30 m` và đứng yên `≤ 0.3 m/s` trong `2 s`, sóng mở rộng `140 m` trong
     `5 s` và dựng cột sáng cao `32 m` trong `20 s`). `[ĐỀ XUẤT]` Bỏ hoàn toàn cột sáng `32 m` trên trời
     và vòng sóng mặt đất, thay bằng vòng ăng-ten định hướng xoay về mục tiêu + đầu 07 ngoái nhìn + ánh
     lấp lánh tại vật thể + âm thanh 3D định hướng.
3. **Tách lộ trình thực tế giữa `0.5.6` (không phụ thuộc khóa art style) và `0.5.7` (sau khi khóa art
   style + `07 v2`), và đánh dấu rõ `story v3` là bản nháp chờ 7 câu trả lời của chủ dự án ở Mục 9.**

---

## 2. Chẩn đoán Bản 0.5.5 & Phương án Xử lý từng Điểm (Thứ tự Ưu tiên của Chủ dự án)

Mọi mục dưới đây đối chiếu trực tiếp với phản hồi của chủ dự án trong
[`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2
và mã nguồn C# hiện hành:

### P1. Phong cách nghệ thuật chưa ra chất
- **Thực trạng vật liệu và hình khối trong game:** `[HIỆN TẠI]` Toàn bộ thế giới và thân xe 07 đang được dựng
  bằng mesh đa giác thấp sinh tự động bằng code C# (`Procedural low-poly meshes`) dùng chung vật liệu màu
  đỉnh (`Vertex-color`) trong [`Assets/_Project/Scripts/Art/Editor/RoverMeshes.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Art/Editor/RoverMeshes.cs)
  và [`Assets/_Project/Scripts/Art/Editor/BuildingMeshes.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Art/Editor/BuildingMeshes.cs),
  khiến mặt đất, đá và công trình trông phẳng, thiếu độ sâu vật liệu
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P1).
- **Thực trạng lỗi chồng mặt phẳng (z-fight) ở vòm Xưởng xe:** `[HIỆN TẠI]` Khối vòm của Xưởng xe của Kenji
  (`Repair Bay`) có hai mặt phẳng trùng tọa độ gây nhấp nháy khi xoay camera
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P1).
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Sửa ngay lỗi z-fight ở vòm Xưởng xe trong bản `0.5.6` (nhánh `agy-art`) vì đây là lỗi hình
    học độc lập, không phụ thuộc vào việc khóa phong cách nghệ thuật.
  - `[ĐỀ XUẤT]` Trình 2 phương án phong cách nghệ thuật (Phương án A: Low-Poly Stylised nâng cấp ánh sáng
    và độ nhám bụi regolith; Phương án B: Ghibli / Cel-Shaded ấm áp) tại Mục 5 & Mục 9 để chủ dự án chốt
    trước khi áp dụng đồng loạt vào bản `0.5.7`.

### P2. Hạt năng lượng bay về 07, vòng sóng dưới đất và hai cột sáng xanh trên trời không rõ nghĩa
- **Thực trạng hạt bay khi cắt phế liệu:** `[HIỆN TẠI]` Khi 07 cắt rời một mảnh phế liệu trong
  [`Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs)
  và [`Assets/_Project/Scripts/Gameplay/Salvage/CargoBundles.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/CargoBundles.cs),
  các gói vật liệu (`CargoBundles`) bay về điểm `CargoSocket` (`y = 0.48, z = -0.64`) nằm thấp hơn nắp khoang
  hàng `CargoHatch` (`y = 0.69`), trông như các hạt sáng chui xuyên qua cản sau đặc
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P2).
- **Thực trạng cơ chế lắng nghe bị động (`ListenGate` & `SonarSystem`):** `[HIỆN TẠI]` Khi Tháp radio đạt từ
  cấp 1 trở lên, [`Assets/_Project/Scripts/Gameplay/Sonar/ListenGate.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/ListenGate.cs)
  (dòng 35–68) tự động kích hoạt lắng nghe sau khi 07 lái ít nhất `30 m` (`_listenSpacing = 30f`, riêng lần
  đầu tiên trong game là `90 m` `_firstListenSpacing = 90f`) rồi dừng yên `≤ 0.3 m/s` (`_restSpeed = 0.3f`)
  trong `2.0 s` (`_restSeconds = 2f`).
- **Thực trạng vòng sóng và cột sáng `32 m` (`SonarTuning` & `SiteMarker`):** `[HIỆN TẠI]` Khi kích hoạt,
  [`Assets/_Project/Scripts/Gameplay/Sonar/SonarSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SonarSystem.cs)
  (dòng 162–215) và [`Assets/_Project/Scripts/Gameplay/Sonar/SonarTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SonarTuning.cs)
  (dòng 15–52) làm ăng-ten rung `1.2 s` (`14°`), phát một vòng sóng `SonarRing` lan trên mặt đất tới bán kính
  `140 m` (`_range = 140f`) trong `5.0 s` (`_waveDuration = 5f`), và mọi kỷ vật chưa đào, bạn hỏng, bãi phế
  liệu hoặc xác tàu còn đồ/mảnh bản vẽ trong tầm `140 m` sẽ dựng một cột sáng thẳng đứng cao `32 m`
  (`_pillarHeight = 32f`, bán kính `0.45 m`) trong
  [`Assets/_Project/Scripts/Gameplay/Sonar/SiteMarker.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SiteMarker.cs)
  suốt `20 s` (`_answerDuration = 20f`, kéo dài gấp `1.6×` nếu 07 chiếu `Warm Headlamp` trong góc `35°`).
- **Thực trạng nút bấm lắng nghe:** `[HIỆN TẠI]` Hiện không có phím bấm lắng nghe thủ công nào trong
  [`Assets/_Project/Scripts/Core/Input/InputReader.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Core/Input/InputReader.cs)
  (tuân theo quy tắc không thêm nút phụ `docs/VISION.md` Ruling 14).
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Trong bản `0.5.6`, xóa hoàn toàn cột sáng `32 m` trên trời trong `SiteMarker.cs` và vòng sóng
    lan dưới đất trong `SonarRing.cs` để thế giới không còn các cột laser chỉ đường giả tạo
    ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P2).
  - `[ĐỀ XUẤT]` Thay phản hồi lắng nghe bằng 4 tín hiệu vật lý tự nhiên: (1) vòng ăng-ten định hướng
    (`SonarLoop`) trên lưng 07 xoay về hướng có tín hiệu gần nhất, (2) đầu của 07 (`RoverHead`) ngoái nhìn
    nhẹ về hướng đó, (3) bản thân vật thể phát một ánh lấp lánh/chớp đèn cục bộ sát mặt đất khi nằm trong
    tầm nhìn, và (4) âm thanh phản hồi 3D định hướng phát ra từ vị trí vật thể.
  - `[ĐỀ XUẤT]` Khi thu hoạch vật liệu hoặc nhặt linh kiện cốt truyện (`TowerCoil`, `Transformer`, `RelaySpare`),
    mở nắp khoang sau `CargoHatch` (nhánh `agy-hold` ở `0.5.6` và khoang lõm có con lăn `IntakeRoller` ở
    `0.5.7`) để các kiện đồ bay đúng vào lòng khoang chứa thay vì xuyên qua vỏ xe.

### P3. Gara của Kenji và nhịp hướng dẫn đầu game chồng chéo, khó hiểu bước tiếp theo
- **Thực trạng cổng bảo trì ở Gara của Kenji (`GaragePort`):** `[HIỆN TẠI]` Trong
  [`Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs)
  (dòng 108–195), cổng bảo trì ở Gara của Kenji (`(-190, -140)`) chỉ mở khi 07 cắt sạch toàn bộ các mảnh phế
  liệu của bãi gara (`_garage.IsPickedClean`); khi 07 đỗ lên bệ `GaragePortPad`, cổng tự động lắp
  `RoverKitPiece.TetherRing` (`RoverAbility.Tether`) vào `RingSocket`, sau đó làm sáng mối hàn của thùng
  `"07 · SPARE"` (`container.garage.spare_crate`) để 07 cắt thùng lấy `TapeDeck` (`RoverKitPiece.TapeDeck`),
  nhấn `E` đưa `TapeDeck` lên khay rồi đỗ lại lên bệ để lắp.
- **Thực trạng điều phối chữ hướng dẫn trên màn hình (`GuidanceSlot` & `PromptDirector`):** `[HIỆN TẠI]` Mã
  nguồn đã có lớp [`Assets/_Project/Scripts/UI/Prompts/GuidanceSlot.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/UI/Prompts/GuidanceSlot.cs)
  (dòng 11–57) phân xử độ ưu tiên `GuidanceKind` (`Card > Prompt > Whisper > Next`) và lớp
  [`Assets/_Project/Scripts/UI/Prompts/PromptDirector.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/UI/Prompts/PromptDirector.cs)
  (dòng 17–75) cùng `PromptLedger` quản lý gợi ý ngữ cảnh, lời nhắc tiếp cận (`WhisperChanged`) và mục tiêu
  góc dưới trái (`NextStepChanged`).
- **Thực trạng chồng chéo thông báo:** `[HIỆN TẠI]` Dòng chữ băng truyền `RadioTickerMessage` và tên địa danh
  `LandmarkAnnounced` trong [`Assets/_Project/Scripts/Core/Events/UIEvents.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Core/Events/UIEvents.cs)
  vẫn hiển thị độc lập với `GuidanceSlot`, nên khi tới Gara của Kenji người chơi bị dội cùng lúc nhiều thông
  báo và không rõ phải cắt hết phế liệu hay đỗ lên bệ trước
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P3).
- **Thực trạng điều kiện ẩn của thang nâng bụng (`LiftStation`):** `[HIỆN TẠI]` Trong
  [`Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs)
  (dòng 353 và 584–593) cùng [`Assets/_Project/Scripts/Gameplay/Lift/LiftCycle.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Lift/LiftCycle.cs)
  (dòng 62–63), ngay cả khi nhà đã có điện (`BasePowerStage.Home` sau khi xây Tháp cấp 1), thang nâng bụng
  (`BellyLift`) vẫn nằm im ở trạng thái `LiftPhase.Jammed` và không cho hàn cáp cho đến khi thỏa điều kiện ẩn
  `Carrying() && NearHome()` (`_tether.Towed != null || _cradle.Carried != null` trong bán kính `_tuning.CallRadius`).
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Trong bản `0.5.6`, đưa toàn bộ thông báo ticker/địa danh vào chung cơ chế phân xử của
    `GuidanceSlot` và `PromptDirector` để trên màn hình chỉ xuất hiện đúng **một** thành phần hướng dẫn tại
    một thời điểm.
  - `[ĐỀ XUẤT]` Trong bản `0.5.6`, tách nhịp ở Gara của Kenji thành từng bước tuần tự rõ ràng trong
    `PromptDirector`: (1) khi cổng còn đóng, chỉ dẫn rõ cần dọn phế liệu quanh cổng bảo trì; (2) khi cổng mở
    và đèn bệ sáng, nhắc đỗ lên bệ `GaragePortPad` để lắp tời kéo; (3) sau khi lắp tời kéo, làm nổi bật thùng
    `"07 · SPARE"` để cắt lấy `TapeDeck`.
  - `[ĐỀ XUẤT]` Trong bản `0.5.6`, gỡ bỏ điều kiện ẩn `Carrying() && NearHome()` trong `LiftStation.cs`
    (dòng 353): ngay khi Tháp cấp 1 cấp điện cho nhà (`BasePowerStage.Home`), thang nâng bụng phát tiếng kẹt
    cáp và bật vòng sáng `LiftPhase.Called` để 07 có thể hàn sửa cáp (`next.lift.stitch`) và đi lên Phòng thí
    nghiệm bất cứ lúc nào.

### P4. Đào kỷ vật bằng chùm sáng từ mắt camera trông giả tạo; đầu 07 bị ghim cứng vào mục tiêu
- **Thực trạng đầu 07 bị ghim cứng:** `[HIỆN TẠI]` Lỗi đầu của 07 bị khóa cứng nhìn chằm chằm vào một vật thể
  từ xa đã được sửa xong ở commit `3ab086d1` trong
  [`Assets/_Project/Scripts/Rover/RoverHead.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Rover/RoverHead.cs)
  và [`Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs)
  (`Logs/owner-memory/resume-point.md`).
- **Thực trạng cơ chế đào (`ExcavationSystem`):** `[HIỆN TẠI]` Trong
  [`Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs)
  (dòng 120–165) và [`Assets/_Project/Scripts/Gameplay/Excavation/ExcavationTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Excavation/ExcavationTuning.cs),
  07 có thể đào ngay từ đầu game trong bán kính `5 m` (`DigRadius = 5f`) bằng cách giữ phím `E` trong `1.6 s`
  (`DigDuration = 1.6f`), nhưng hình ảnh hiện tại lại chiếu một hình nón ánh sáng (`TractorBeam`) từ mắt
  camera (`_rig.TetherOrigin`) làm kỷ vật bay lơ lửng và tự xoay tròn lên khỏi mặt đất
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P4).
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Trong thiết kế `07 v2` (bản `0.5.7`), giữ nguyên việc 07 **đào được ngay từ phút đầu tiên của
    game**, nhưng trang bị cho 07 một **gầu xúc cơ khí có răng ở gầm trước (`DigScoop`, điểm gốc `ScoopOrigin`)
    làm thiết bị khởi đầu mặc định**, xóa bỏ hoàn toàn hình nón `TractorBeam` ở mắt và thay quỹ đạo bay lơ
    lửng bằng 3 nhịp xúc-bẩy cơ khí hất tung bụi đất để bẩy kỷ vật và bộ phận của Tilly/Bell lên mặt đất.

### P5. Nhặt đồ hiện thẻ ký ức toàn màn hình gây đứt mạch; phím `Tab` cuộn qua nhiều trang rối rắm
- **Thực trạng thẻ ký ức và màn hình `Tab`:** `[HIỆN TẠI]` Khi 07 đào được kỷ vật hoặc nhặt băng cassette,
  game từng bật một thẻ ký ức đè lên màn hình (`CardShown` trong
  [`Assets/_Project/Scripts/Core/Events/UIEvents.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Core/Events/UIEvents.cs)),
  và phím `Tab` từng mở nhiều trang phải cuộn qua lại
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P5).
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Trong bản `0.5.6` (nhánh `agy-ui2` đang hoàn thiện), bỏ hoàn toàn thẻ ký ức bật lên khi nhặt
    kỷ vật/băng cassette ngoài thực địa (kỷ vật chỉ đọc tại Kệ kỷ vật trong Phòng thí nghiệm, băng cassette
    chỉ hiện dòng thông báo nhỏ khi phát), và gộp phím `Tab` thành **đúng một màn hình cố định duy nhất
    (`TabSheet`)** không cuộn trang, hiển thị gọn: (1) sơ đồ 07 và các thiết bị đã lắp, (2) trạng thái pin và
    các ô khoang hàng `CargoHold`, (3) tiến độ các bộ bản vẽ của Kenji.

### P6. Nhịp cắt 0.5.5 đã có lực nhưng mọi tia công cụ đều bắn ra từ mắt camera
- **Thực trạng cơ chế cắt và cụm nạp `ChargeCells`:** `[HIỆN TẠI]` 07 cắt được ngay từ đầu game nhờ
  [`Assets/_Project/Scripts/Gameplay/Salvage/Cutter.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/Cutter.cs),
  [`Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs)
  và [`Assets/_Project/Scripts/Rover/Feel/ChargeCells.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Rover/Feel/ChargeCells.cs)
  (cùng phần cứng `ChargeCellHardware.cs`): 07 có hai đường ống dẫn năng lượng đối xứng chạy dọc boong trước
  tới vòng khóa quanh mắt, với 3 tầng tụ trượt vỏ kính sáng dần khi nạp (`_braceSeconds = 0.5f`, mỗi tầng
  `_stageSeconds = 0.45f` ở Bậc 0, nhịp nghỉ `_breathSeconds = 0.12f`, cắt liên tiếp trong cửa sổ ấm
  `_warmWindowSeconds = 6f` nhanh hơn với hệ số `_warmFactor = 0.6f`)
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §5).
- **Thực trạng điểm phát tia (`IRoverRig`):** `[HIỆN TẠI]` Do
  [`Assets/_Project/Scripts/Core/IRoverRig.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Core/IRoverRig.cs)
  hiện chỉ khai báo đúng 2 điểm `TetherOrigin` (tâm mắt camera) và `CargoSocket`, cả tia cắt, tia hàn cáp/sửa
  bạn, tia quét, tia đào lẫn dây kéo đều phát ra từ chính giữa thấu kính mắt của 07.
- **Hướng xử lý:**
  - `[ĐỀ XUẤT]` Trong bản `0.5.6` (nhánh `agy-scan`), nối tia quét bản vẽ (`ScanFan`) trực tiếp từ thấu kính
    mắt của 07 tới mảnh bản vẽ đang quét để người chơi thấy rõ chùm quét quang học.
  - `[ĐỀ XUẤT]` Trong bản `0.5.7` (`07 v2`), giữ nguyên cụm tụ nạp 3 tầng đối xứng `ChargeCells` trên boong
    trước nhưng dẫn thanh cái đồng vào một **đầu cắt plasma & kim hàn vi điểm gắn trên mũi xe
    (`CutterTurret` / `CutterOrigin`)** nằm dưới tầm nhìn của mắt, để mắt camera (`EyeOrigin`) chỉ làm đúng
    nhiệm vụ nhìn, biểu cảm và quét quang học.

---

## 3. Thiết kế Chi tiết Rover `07 v2` (Mọi Hành động Đều Có Thiết bị Vật lý Đúng Chức năng)

### 3.1 Nguyên tắc cốt lõi: Giữ trọn bản sắc của 07, tách bạch 7 điểm gốc thiết bị
- `[HIỆN TẠI]` Thân xe 07 hiện được dựng bởi
  [`Assets/_Project/Scripts/Art/Editor/RoverModelBuilder.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Art/Editor/RoverModelBuilder.cs)
  theo hình dáng đầu dò thập niên 1970 kết hợp tủ đài Braun: thân hộp sơn kem (`#F4E6C8`), mặt lưới tản nhiệt
  trước màu than (`#3E4048`), sọc cam chạy ngang thân (`#FF8A5B`), 6 bánh xe trên hệ treo rocker-bogie
  (`Wheel_FL..RR`), vết vá màu xanh xô thơm (`#78A582`) và bánh sau bên trái (`Wheel_RL`) khác màu do Kenji
  từng thay đồ dự phòng, cùng một mắt camera lớn màu hổ phách dưới mí mắt biểu cảm (`Eyelid.cs`) và quy tắc
  đối xứng trái-phải nghiêm ngặt (`docs/VISION.md` Ruling 13).
- `[ĐỀ XUẤT]` Thiết kế `07 v2` giữ nguyên 100% hình dáng, bảng màu, 6 bánh rocker-bogie (kèm bánh thay thế
  `Wheel_RL`) và mắt camera biểu cảm của 07, đồng thời mở rộng hợp đồng
  [`Assets/_Project/Scripts/Core/IRoverRig.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Core/IRoverRig.cs)
  từ 2 điểm gốc (`TetherOrigin`, `CargoSocket`) thành **7 điểm gốc vật lý chuyên biệt** để không còn bất kỳ
  tia công cụ hay dây cáp nào bắn ra từ mắt camera:

```csharp
// [ĐỀ XUẤT] Mở rộng hợp đồng Assets/_Project/Scripts/Core/IRoverRig.cs trong 0.5.7 (07 v2)
namespace MoonProject.Core
{
    public interface IRoverRig
    {
        Transform EyeOrigin { get; }     // Neck/Head/Eye/EyeOrigin: chỉ dùng để nhìn và quét ScanFan
        Transform CutterOrigin { get; }  // CutterTurret/CutterHead/CutterOrigin: cắt phế liệu & hàn cáp/bạn
        Transform ScoopOrigin { get; }   // DigScoop/Bucket/ScoopOrigin: gầu xúc khởi đầu để đào kỷ vật/bộ phận
        Transform HatchOrigin { get; }   // CargoHold/HatchOrigin: miệng khoang hàng & con lăn thu/phóng đồ
        Transform WinchOrigin { get; }   // WinchMount/WinchOrigin: tời cáp thép cản sau kéo trượt vật nặng
        Transform CradleOrigin { get; }  // CargoCradle/CradleOrigin: đệm lưng & khung chữ A HoistBail cẩu kỷ vật
        Transform AntennaOrigin { get; } // Antenna/SonarLoop/AntennaOrigin: vòng ăng-ten xoay định hướng lắng nghe
    }
}
```

### 3.2 Bảng ánh xạ Hành động → Thiết bị vật lý (`[HIỆN TẠI]` so với `[ĐỀ XUẤT]` trong `07 v2`)

| # | Hành động (Verb) | Trạng thái trong game hôm nay (`[HIỆN TẠI]`) | Thiết kế vật lý trong `07 v2` (`[ĐỀ XUẤT]` và lý do) |
|---|---|---|---|
| **1** | **Nhìn, Biểu cảm & Quét Bản vẽ (`ScanField`)** | `[HIỆN TẠI]` Đầu camera trên cổ xoay (`Neck/Head/Eye`), chớp mí (`Eyelid.cs`) và quét mảnh bản vẽ (`FragmentSocket_*`) qua [`Assets/_Project/Scripts/Gameplay/Scanning/ScanField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanField.cs); nâng cấp `Focus Lens` gắn vào `LensSocket` ([`Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs)). | `[ĐỀ XUẤT]` Đổi tên nút gốc thành `EyeOrigin` (`Neck/Head/Eye/EyeOrigin`) và nối màn sáng quét `ScanFan` từ `EyeOrigin` tới vật thể (`0.5.6` `agy-scan`), vì nhìn và quét quang học là hai việc duy nhất thuộc về mắt camera. |
| **2** | **Cắt Phế liệu & Chốt Thùng (`Cutter`, `SalvageField`)** | `[HIỆN TẠI]` 07 cắt được ngay từ phút đầu (Bậc 0: `1` tầng cho mảnh nhỏ `0.95 s`, `2` tầng cho mảnh vừa `1.40 s`, `3` tầng cho mảnh lớn/tấm vỏ `1.85 s`; cửa sổ ấm `6 s` giảm còn `0.6×`; nâng cấp `Focus Lens` giảm mỗi tầng từ `0.45 s` xuống `0.36 s` và giảm hao pin còn `0.85×`) trong [`Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs), nhưng tia cắt đang phát ra từ tâm mắt `_rig.TetherOrigin` ([`Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs) dòng 194). | `[ĐỀ XUẤT]` Giữ nguyên quyền cắt từ phút đầu và giữ nguyên 2 thanh tụ nạp 3 tầng đối xứng `ChargeConduits/Stage_1..3_L/R` trên boong trước (`ChargeCellHardware.cs`), nhưng dẫn năng lượng vào **Đầu cắt Plasma gắn trên khớp xoay mũi xe (`CutterTurret/CutterHead/CutterOrigin`)** dưới tầm mắt để tia cắt phát ra từ nòng cắt cơ khí thay vì thấu kính mắt. |
| **3** | **Hàn Cáp & Sửa Bạn / Trạm (`RepairBeam`)** | `[HIỆN TẠI]` Hàn cáp thang nâng bụng ([`Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs) dòng 358), sửa Tilly/Bell ([`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs)) và sửa trạm tiếp sóng đều bắn tia `RepairBeam` từ mắt `_rig.TetherOrigin`. | `[ĐỀ XUẤT]` Phát tia hàn từ **Kim hàn đồng dưới nòng cụm mũi xe (`CutterOrigin` / `StitchOrigin`)** để việc hàn cơ khí tách khỏi mắt camera. |
| **4** | **Đào Kỷ vật & Bộ phận Bạn bị Chôn (`ExcavationSystem`)** | `[HIỆN TẠI]` 07 đào được ngay từ đầu game trong tầm `5 m` bằng cách giữ `E` `1.6 s` cho kỷ vật chôn (`RelicPocket_*`), ống kính của Tilly dưới xe trượt (`PartSocket_1`), và 3 bộ phận của Bell dưới các đống đá sạt (`RockfallPocket` trong [`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs) `LayThePockets`), nhưng đang vẽ phễu sáng `TractorBeam` từ mắt `_rig.TetherOrigin` ([`Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Excavation/ExcavationSystem.cs) dòng 148–154). | `[ĐỀ XUẤT]` Trang bị **Gầu xúc răng thép có tay cần thủy lực ở gầm trước (`DigScoop` / `ScoopOrigin`, vươn tới `z = +1.37 m` trước lốp trước)** làm **thiết bị khởi đầu có sẵn từ phút 0**; khi giữ `E` (`1.6 s`), gầu hạ xuống xúc-bẩy 3 nhịp làm văng bụi và bẩy vật bị chôn lên mặt đất thay cho tia hút từ mắt. |
| **5** | **Chứa Phế liệu, Băng, Bộ phận Bạn, Linh kiện Tháp & Nạp Phễu (`CargoHold`, `HopperFeed`)** | `[HIỆN TẠI]` Khoang hàng có `8` ô ban đầu (`_holdSlots = 8`), khởi đầu game mới với `2 Metal + 1 Wiring` (`_starterItems`), tăng thêm `+2` ô khi lắp `Cargo Cradle` (`_cradleSlots = 2`) và `+4` ô khi lắp `Side Bins` (`_sideBinSlots = 4`, tổng `8 → 10 → 14` ô) trong [`Assets/_Project/Scripts/Gameplay/Economy/CargoTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Economy/CargoTuning.cs) và [`Assets/_Project/Scripts/Gameplay/Economy/CargoHold.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Economy/CargoHold.cs). `[HIỆN TẠI]` Nhà có `2` kệ sẵn (`24` ô) và `2` vị trí đóng thêm kệ (`12` ô/kệ, giá `3 Metal` và `4 Metal + 1 Wiring`, tối đa `48` ô kệ nhà). `[HIỆN TẠI]` Linh kiện cốt truyện (`TowerCoil`, `Transformer`, `RelaySpare`) đang nhảy lên nóc phẳng `StoryPartSocket` ([`Assets/_Project/Scripts/Rover/StoryPartCarry.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Rover/StoryPartCarry.cs)), còn khi nạp phễu (`HopperFeed.cs`) 07 lại chiếu tia từ mắt trong khi các gói đồ bật ra từ cản sau đóng kín. | `[ĐỀ XUẤT]` Trong bản `0.5.6` (`agy-hold`), đưa linh kiện cốt truyện (`TowerCoil`, `Transformer`, `RelaySpare`) đi qua nắp khoang `CargoHatch` vào trong bụng xe (xóa `StoryPartSocket` trên nóc). `[ĐỀ XUẤT]` Trong bản `0.5.7` (`07 v2`), thiết kế nắp khoang sau dạng vỏ sò đôi (`CargoHatch` mở cánh trên `+52°`, máng dưới `-12°` tại `HatchOrigin` `y = 0.62 m`) kèm con lăn từ tính `IntakeRoller` để thu đồ vào hốc khoang thật và quay ngược con lăn bắn các bó vật liệu vào phễu trạm khi nạp (`HopperFeed`, bỏ tia mắt khi nạp phễu). |
| **6** | **Kéo Vật nặng bằng Dây cáp (`TetherSystem`, `WinchControl`)** | `[HIỆN TẠI]` Sau khi cắt sạch phế liệu ở Gara của Kenji và đỗ lên `GaragePortPad`, 07 được lắp `TetherRing` (`RoverAbility.Tether`) vào `RingSocket` (`z = -0.20` sau gáy đầu), nhưng dây kéo lại bắn ra từ mắt trước `_rig.TetherOrigin` và dùng lò xo `TetherSpring` nâng bổng kỷ vật lơ lửng trên không ở độ cao `HoverHeight` trước cả khi có nâng cấp nâng đồ ([`Assets/_Project/Scripts/Gameplay/Tether/TetherSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Tether/TetherSystem.cs), [`Assets/_Project/Scripts/Gameplay/Tether/TetherSpring.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Tether/TetherSpring.cs)). | `[ĐỀ XUẤT]` Trong bản `0.5.7` (`07 v2`), thay vòng sau gáy bằng **Trống tời cáp thép gắn ở cản sau thấp (`WinchDrum` / `WinchOrigin` tại `y = 0.38 m`)** và đặt `HoverHeight = 0f` khi chưa có `Cargo Cradle`: dây cáp thép móc vào kỷ vật và **kéo trượt vật nặng trên mặt đất** (tạo vệt bụi và lực cản tốc độ `1.25×` hao pin), không làm vật bay lơ lửng. |
| **7** | **Cẩu & Chở Kỷ vật trên Lưng (`CargoCradle`)** | `[HIỆN TẠI]` Sau khi quét 2 mảnh bản vẽ và chế tạo `Cargo Cradle` (*Giỏ chở đồ*, `4 Metal + 2 Wiring`) ở Xưởng xe, nhấn chuột phải làm kỷ vật tự bay theo đường cong spline `GlidePath` lên `CargoSocket` (`y = 0.48, z = -0.64`) đè ngay trước nắp khoang hàng ([`Assets/_Project/Scripts/Gameplay/Upgrades/CargoCradle.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Upgrades/CargoCradle.cs)). | `[ĐỀ XUẤT]` Trong bản `0.5.7` (`07 v2`), đặt đệm chở kỷ vật `CradleSeat` (`CradleOrigin`) lên boong sau cao (`y = 0.99, z = -0.47`) kèm **Khung cẩu chữ A (`HoistBail`)** bắt bản lề hai bên hông sau (`x = ±0.41`, không che `CargoHatch`): khi thu cáp về sát cản sau, khung chữ A hạ xuống ngoạm kỷ vật và **cẩu cơ khí (`0.9 s`) đặt lên lưng 07**, giúp chở kỷ vật đi nhanh, không vướng địa hình và đi thẳng lên thang nâng bụng. |
| **8** | **Lắng nghe & Định hướng Tín hiệu (`SonarSystem`, `Antenna`)** | `[HIỆN TẠI]` Ăng-ten sau bên trái (`Antenna` tại `(-0.34, 0.95, -0.46)`) chỉ là que thẳng rung nhẹ `1.2 s` khi lắng nghe, phát vòng sóng `140 m` và dựng cột sáng `32 m` ([`Assets/_Project/Scripts/Gameplay/Sonar/SonarSystem.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SonarSystem.cs), [`Assets/_Project/Scripts/Gameplay/Sonar/SiteMarker.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SiteMarker.cs)). | `[ĐỀ XUẤT]` Thêm **Vòng ăng-ten định hướng xoay được (`Antenna/SonarLoop` / `AntennaOrigin`)** xoay mũi về phía vật thể trả lời gần nhất kết hợp đầu 07 ngoái nhìn, ánh lấp lánh tại vật thể và âm thanh 3D (bỏ cột sáng `32 m`). |
| **9** | **Pin, Sạc Mặt trời, Đèn pha & Nhảy/Tăng tốc (`BatteryField`, `RoverKitBuilder`)** | `[HIỆN TẠI]` Cánh pin mặt trời `SolarWing` nằm giữa lưng; thanh 8 đèn pin `BatteryLamp_0..7` nằm sau lưng; nâng cấp `Cell Pack II` (`BatterySpine`) đang gắn lệch bên phải tại `SpineSocket` (`x = +0.385`) vi phạm tính đối xứng ([`Assets/_Project/Scripts/Art/Editor/RoverKitBuilder.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Art/Editor/RoverKitBuilder.cs) dòng 28). | `[ĐỀ XUẤT]` Trong `07 v2`, thiết kế lại `BatterySpine` thành **cặp hộp pin đôi đối xứng hai bên cánh mặt trời (`x = ±0.32`)** để giữ đúng quy tắc đối xứng Trái-Phải của 07 (`docs/VISION.md` Ruling 13). |

---

## 4. Các Hệ thống Gameplay Cốt lõi, Bạn đồng hành & Bảng Nâng cấp (0.5 → 1.0)

### 4.1 Hệ thống Quét & Bản vẽ (`Scanning` & `Blueprints`)

- **Thông số quét thực tế trong code (`ScanTuning.cs` & `ScanField.cs`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs)
    (dòng 12–66) và [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.2,
    thời gian giữ chuột phải để quét một mảnh bản vẽ theo kích thước là:
    - **Mảnh nhỏ (`_smallSeconds`):** **`1.5 s`**.
    - **Mảnh vừa (`_mediumSeconds`):** **`2.5 s`**.
    - **Mảnh lớn (`_largeSeconds`):** **`4.0 s`**.
  - `[HIỆN TẠI]` Tầm quét tối đa là **`8 m`** (`_reach = 8f`), góc nón ngắm từ mắt 07 là **`25°`**
    (`_coneDegrees = 25f`), và tốc độ tụt tiến độ khi nhả nút giữa chừng là `0.5 /s` (`_decayRate = 0.5f`)
    ([`Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs) dòng 12–34).
  - `[HIỆN TẠI]` Khi quét xong, mảnh bản vẽ tan thành hạt sáng bay về mắt 07 trong **`0.9 s`**
    (`_dissolveSeconds = 0.9f`) và để lại viền bụi sáng tại chỗ trong **`2.0 s`** (`_outlineSeconds = 2f`);
    nếu quét một mảnh thuộc bộ bản vẽ đã hoàn thành, mảnh đó thưởng **`+3 Metal`** (`_duplicateMetal = 3`)
    ([`Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs) dòng 36–51).
  - `[HIỆN TẠI]` Khung góc chữ L (`Corner brackets`) quanh mảnh bản vẽ chưa quét hiện lên ở cự ly **`15 m`**
    (`_bracketRadius = 15f`); sau khi chế tạo nâng cấp **`Focus Lens`** (`RoverAbility.FocusLens`), tốc độ
    quét tăng gấp **`1.5×`** (`_lensSpeedMultiplier = 1.5f`, rút thời gian quét xuống còn `1.0 s / 1.67 s / 2.67 s`)
    và khung góc hiện lên từ cự ly **`25 m`** (`_lensBracketRadius = 25f`)
    ([`Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs) dòng 53–65).
  - `[ĐỀ XUẤT]` Trong bản `0.5.6` (nhánh `agy-scan`), giữ nguyên toàn bộ các con số trong `ScanTuning.cs`
    nói trên và bổ sung màn sáng quang học (`ScanFan`) nối trực tiếp từ thấu kính mắt của 07 tới mảnh bản
    vẽ đang quét để người chơi thấy rõ tia quét
    ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P6).

- **Danh sách đầy đủ 7 bộ bản vẽ 0.5 (`18 mảnh` tổng cộng) và 2 thiết bị lắp tại Gara của Kenji:**
  - `[HIỆN TẠI]` Theo [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3 & §1.7,
    [`Assets/_Project/Scripts/Gameplay/Progression/TechTree.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/TechTree.cs),
    [`Assets/_Project/Scripts/Gameplay/Upgrades/Workshop.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Upgrades/Workshop.cs)
    và [`Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs),
    toàn bộ trang bị của 07 trong bản `0.5` gồm **2 thiết bị gắn tại Gara của Kenji** và **7 bộ bản vẽ chế
    tạo tại Xưởng xe của Kenji** (mở khóa khi Tháp radio đạt cấp 2):

| # | Tên thiết bị / Bản vẽ (Key & Tên tiếng Việt) | Trạng thái & Nguồn xác thực | Số mảnh & Vị trí từng mảnh trong thế giới 0.5 | Chi phí chế tạo & Tác dụng thực tế |
|---|---|---|---|---|
| **G1** | **`TetherRing`** (*Vòng dây kéo* / Tời kéo) | `[HIỆN TẠI]` ([`GaragePort.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs) dòng 126–155, [`EquipmentTable.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs)) | Không dùng mảnh bản vẽ; nằm sẵn trong cổng bảo trì tại **Gara của Kenji** (`(-190, -140)`), mở cổng khi cắt sạch phế liệu gara (`_garage.IsPickedClean`) và tự lắp khi 07 đỗ lên `GaragePortPad`. | Miễn phí (`RingSocket`, `RoverAbility.Tether`): cho phép móc dây kéo kỷ vật và thu/thả dây (`1.2 m..4.5 m`). `[ĐỀ XUẤT]` Ở `07 v2` (`0.5.7`), đổi thành trống tời cản sau `WinchDrum` chỉ kéo trượt vật nặng trên mặt đất (`HoverHeight = 0f`). |
| **G2** | **`TapeDeck`** (*Máy phát băng*) | `[HIỆN TẠI]` ([`GaragePort.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs) dòng 156–195, [`EquipmentTable.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Equipment/EquipmentTable.cs)) | Nằm trong thùng niêm phong `"07 · SPARE"` (`container.garage.spare_crate`) tại **Gara của Kenji** (`(-190, -140)`); mối hàn thùng sáng lên sau khi lắp `TetherRing`. | Miễn phí (`DeckSocket`, `RoverAbility.TapeDeck`): cắt chốt thùng, nhấn `E` đưa lên khay và đỗ lên `GaragePortPad` để lắp; cho phép phát các cuộn băng cassette của Ro qua loa của 07. |
| **1** | **`Cargo Cradle`** (`tech.cargo_cradle` — *Giỏ chở đồ*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`Workshop.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Upgrades/Workshop.cs), [`CargoTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Economy/CargoTuning.cs)) | **2 mảnh:** `1` tại **Gara của Kenji** (`(-190, -140)`), `1` trên **xe chở hàng Mule trên đường hẻm núi** (`(-140, -195)`). | Chi phí: **`4 Metal + 2 Wiring`** (`CargoSocket`, `RoverAbility.CargoCradle`). Đặt kỷ vật lên lưng 07 để chạy nguyên tốc độ không bị cản và cộng thêm **`+2` ô khoang hàng** (`8 → 10` ô). `[ĐỀ XUẤT]` Ở `07 v2` (`0.5.7`), lắp khung chữ A `HoistBail` + `CradleSeat` để cẩu cơ khí kỷ vật từ mặt đất lên lưng. |
| **2** | **`Warm Headlamp`** (`tech.warm_headlamp` — *Đèn pha ấm*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`SonarTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Sonar/SonarTuning.cs)) | **2 mảnh:** `1` tại **Gara của Kenji** (`(-190, -140)`), `1` tại **mép gần của khe vực** (`(-140, -245)`). | Chi phí: **`2 Metal + 1 Wiring + 1 Optics`** (`HeadlampSocket`, `RoverAbility.Headlamp`). Chiếu sáng đường đi ban đêm/hốc tối và giữ phản hồi lắng nghe sáng lâu hơn gấp **`1.6×`** khi nhìn thẳng trong góc `35°`. |
| **3** | **`Hover-Jump`** (`tech.hover_jump` — *Bật nhảy*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs)) | **3 mảnh:** `2` trên **xác xe nhảy thử nghiệm của Kenji ở mép gần khe vực** (`(-140, -245)`), `1` tại **Gara của Kenji** (`(-190, -140)`). | Chi phí: **`5 Metal + 2 Wiring + 1 Optics`** (`CoilSocket`, `RoverAbility.HoverJump`). Giữ `Space` để tích lực và thả ra để bật nhảy qua khe vực hẻm núi sang chỗ Bell (tốn `0.05..0.25` ô pin mỗi cú nhảy). |
| **4** | **`Boost Coils`** (`tech.capacitor_drums` — *Bộ tăng tốc*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs)) | **3 mảnh:** `2` trên **robot xe trượt chở hàng tại giàn khoan** (`(260, -80)`), `1` trên **xe Mule trên đường hẻm núi** (`(-140, -195)`). | Chi phí: **`4 Metal + 3 Wiring`** (`DrumSocket`, `RoverAbility.CapacitorDrums`). Lắp cặp trống tụ điện hai bên sườn (`0.25` ô pin dự trữ, tự nạp lại trong `6 s` khi lăn xuống dốc `≥ 5°` ở tốc độ `≥ 3.5 m/s`) để giữ `Shift` tăng tốc vọt qua dốc. |
| **5** | **`Cell Pack II`** (`tech.cell_pack_2` — *Bộ pin II*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs)) | **3 mảnh:** `2` trên **rover đàn anh `06` cạnh nhà trạm tiếp sóng cửa hẻm núi (`relay.2`)** (`(-70, -180)`), `1` tại **giàn khoan** (`(260, -80)`). | Chi phí: **`5 Metal + 3 Wiring + 1 Optics`** (`SpineSocket`, `RoverAbility.BatterySpine`). Nâng tổng số ô pin của 07 từ **`4` ô lên `6` ô** (`StartingCells = 4 → CellPackCells = 6`, tăng tầm lái đường bằng từ `480 m` lên `720 m`). |
| **6** | **`Focus Lens`** (`tech.focus_lens` — *Ống kính hội tụ*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3 & §5.1, [`ScanTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Scanning/ScanTuning.cs), [`CutterTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs)) | **2 mảnh:** cả `2` nằm trong **buồng lái vệ tinh rơi Kestrel-3** (`(180, 140)`). | Chi phí: **`2 Metal + 1 Wiring + 2 Optics`** (`LensSocket`, `RoverAbility.FocusLens`). Giúp quét bản vẽ **nhanh gấp `1.5×`**, hiện khung góc mảnh bản vẽ từ **`25 m`** (thay vì `15 m`), đồng thời nâng đầu cắt lên Bậc 1 (rút mỗi tầng nạp từ `0.45 s` xuống `0.36 s`, nhanh gấp `1.3×`, hao pin cắt giảm còn `0.85×`). |
| **7** | **`Side Bins`** (`tech.side_bins` — *Thùng hông*) | `[HIỆN TẠI]` ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3, [`CargoTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Economy/CargoTuning.cs)) | **3 mảnh:** `1` tại **tàu đổ bộ bị rơi** (`(-260, 180)`), `1` tại **Gara của Kenji** (`(-190, -140)`), `1` trên **xe Mule** (`(-140, -195)`). | Chi phí: **`6 Metal + 2 Wiring`** (`BinSocket`, `RoverAbility.SideBins`). Lắp hai thùng chứa hai bên hông, cộng thêm **`+4` ô khoang hàng** (`_sideBinSlots = 4`, nâng tổng khoang hàng của 07 lên tối đa `14` ô khi đi cùng `Cargo Cradle`). |

- **Các nâng cấp dự kiến cho các bản mở rộng `0.6 → 0.8`:**
  - `[ĐỀ XUẤT]` Theo lộ trình trong [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.6 & §5.1,
    các bản mở rộng sau `0.5` bổ sung các bộ bản vẽ mới mà không làm thay đổi 7 bộ bản vẽ gốc của `0.5`:
    - **Bản `0.6` (*The Long Mare*):** `[ĐỀ XUẤT]` **`Cutter Ribs`** (Bậc cắt 2: rút mỗi tầng nạp xuống
      `0.29 s`, cắt được dầm nặng), **`Dust Wiper`** (gạt bụi tấm pin mặt trời), và **`Solar Coat`**
      (tăng tốc sạc mặt trời trên đường trường).
    - **Bản `0.7` (*Rim Terraces / Meridian*):** `[ĐỀ XUẤT]` **`Thermal Cutter`** (Bậc cắt 3: rút mỗi tầng
      nạp xuống `0.22 s`, cắt vách hợp kim dày) và **`Charge Adapter`** (bộ chuyển đổi để 07 sạc được trên
      đế lục giác của trạm Meridian).
    - **Bản `0.8` (*Shadowed Crater & Lava Tube*):** `[ĐỀ XUẤT]` **`Cold Lamp`** (đèn soi sương băng trong
      hố tối vĩnh cửu) và **`Heater`** (bộ sưởi giữ pin không bị sụt áp trong vùng âm độ sâu).

### 4.2 Những người bạn robot (`Friends`: Tilly, Bell và các bạn dự kiến `0.7–0.8`)

- **1. Tilly — Drone bay khảo sát của Ines (`0.5`):**
  - **Hình dáng & Câu chuyện:** `[HIỆN TẠI]` Tilly là một drone bay hai cánh quạt đồng trục (`DroneBody`)
    từng đi khảo sát dọc vành núi cùng nhà địa chất Ines Kalani
    ([`Assets/_Project/Scripts/Gameplay/Friends/DroneBody.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/DroneBody.cs),
    khóa `friend.tilly.repair_log` trong
    [`Assets/_Project/Data/Localization/vi.json`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Data/Localization/vi.json) dòng 718–719).
  - **Vị trí & Cách lấy 3 bộ phận:** `[HIỆN TẠI]` Trong
    [`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs)
    (`LayTheSled`, dòng 618–680) và
    [`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.4,
    Tilly nằm hỏng tại `(-240, 165)` gần tàu đổ bộ rơi, và cả **3 bộ phận của cô bé đều nằm trong chiếc xe
    trượt khảo sát bị lật (`TillySled`) ngay cạnh đó**:
    1. `PartSocket_0` (`pickup.tilly.rotor` — *Cánh quạt của Tilly*): kẹt sau chốt kẹp của xe trượt
       (`PartBehindClamp = 0`), 07 dùng tia **cắt** chốt để lấy ra.
    2. `PartSocket_1` (`pickup.tilly.lens` — *ống kính của Tilly*): vùi trong khe đất dưới gầm xe trượt bị
       nghiêng (`PartUnderHull = 1`), 07 dùng kỹ năng **đào** (`E`) để lấy lên.
    3. `PartSocket_2` (`pickup.tilly.cell` — *pin của Tilly*): nằm trong bầu cảm biến của xe trượt
       (`PartInPod = 2`), 07 dùng tia **cắt** mở nắp bầu để lấy ra.
  - **Sửa chữa & Nhiệm vụ thực tế sau khi thức dậy (`SpotterAbility = "spotter"`):**
    - `[HIỆN TẠI]` Sau khi 07 gom đủ 3 bộ phận, nạp `2 Metal + 1 Wiring + 1 Optics` (`Recipe(2, 1, 1)`) và
      hàn thân vỏ trong `2.0 s` (`StitchSeconds = 2f` trong
      [`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs)),
      Tilly thức dậy và tặng 07 tấm pin mặt trời đã sửa (**`SolarCell`**, gắn lên `SolarWing` của 07).
    - `[HIỆN TẠI]` **Chỉ điểm (`Spotter`):** Trong
      [`Assets/_Project/Scripts/Gameplay/Friends/FriendBehaviour.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendBehaviour.cs)
      và [`Assets/_Project/Scripts/Gameplay/Friends/FriendDefinition.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendDefinition.cs)
      (`SpotterAbility = "spotter"`), Tilly bay lượn quanh 07 và lao ra chỉ điểm các bãi phế liệu, kỷ vật
      hoặc bộ phận chưa phát hiện trong bán kính `SpotRadius` (`35 m`).
    - `[HIỆN TẠI]` **Nhặt vật liệu dư về kệ nhà (`TillyFetch`):** Trong
      [`Assets/_Project/Scripts/Gameplay/Economy/TillyFetch.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Economy/TillyFetch.cs)
      (dòng 15–115), mỗi khi khoang hàng của 07 đã đầy mà 07 cắt thêm phế liệu để lại các bó vật liệu chờ
      ngoài thực địa (`_overflow.WaitingCount > 0`), Tilly tự bay từ nhà ra nhặt từng bó về xếp lên kệ nhà
      (`_tripSeconds = 8f` mỗi chuyến), rồi đậu nghỉ trên đỉnh Tháp radio (`GetPerchPosition`).
    - `[HIỆN TẠI]` **Dẫn đường khi 07 cạn pin (`BatteryField`):** Trong
      [`Assets/_Project/Scripts/Gameplay/Battery/BatteryField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryField.cs)
      (dòng 310–355), khi 07 về `0` ô pin và phải bò dự phòng (`ReserveCrawl`) quá `5 s` (`_leadDelay = 5f`),
      Tilly bay tới dẫn 07 về trạm sạc sáng đèn gần nhất và nâng trần tốc độ bò của 07 từ `35%` lên **`55%`**
      (`_leadCap = 0.55f`).

- **2. Bell — Robot tủ đài đi bộ 4 chân của Ro (`0.5`):**
  - **Hình dáng & Câu chuyện:** `[HIỆN TẠI]` Bell **không phải là drone bay**; trong
    [`Assets/_Project/Scripts/Gameplay/Bell/RadioCabinetBody.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Bell/RadioCabinetBody.cs)
    và nhật ký sửa chữa `friend.bell.repair_log`
    ([`Assets/_Project/Data/Localization/vi.json`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Data/Localization/vi.json)
    dòng 726–727: *"Mình lắp Bell từ một máy thu hỏng và bốn cái chân máy quay, để đài vẫn phát khi mình
    ngủ. Cô ấy biết nhảy. Phần đó mình không hề lắp. — Ro"*), Bell là một chiếc tủ radio cổ đi bộ lạch bạch
    trên **4 chân máy quay phim (tripod legs)**, có mặt số tròn, núm vặn đồng và cần ăng-ten trên nóc.
  - **Vị trí & Cách lấy 3 bộ phận (Quyết định chủ dự án `2026-10-14`):** `[HIỆN TẠI]` Trong
    [`Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs)
    (`LayThePockets`, dòng 685–730, đã chốt tại commit `4bffd365`), Bell nằm bên kia khe vực hẻm núi tại
    `(-140, -320)` (cần `Hover-Jump` để nhảy qua), kèm cuộn băng `Items[0]` tại `ItemSocket_0`, và **cả 3
    bộ phận của cô (`pickup.bell.knob` *núm dò đài của Bell*, `pickup.bell.cone` *màng loa của Bell*,
    `pickup.bell.valve` *đèn điện tử của Bell*) đều nằm dưới 3 đống đá sạt nhỏ (`RockfallPocket`) trong các
    hốc đá quanh khu vực đó (`PocketOffset_0..2`) để 07 đào lên bằng phím `E`**.
  - **Sửa chữa & Nhiệm vụ thực tế sau khi thức dậy (`RadioDialAbility = "radio_dial"`):**
    - `[HIỆN TẠI]` Sau khi 07 đào đủ 3 bộ phận dưới đá sạt, nạp `3 Metal + 2 Wiring + 1 Optics`
      (`Recipe(3, 2, 1)`) và hàn thân máy, Bell đứng dậy trên 4 chân và đi bộ về góc nhà (`BellCorner`, tốc
      độ `WalkSpeed = 1.6 m/s`) trong
      [`Assets/_Project/Scripts/Gameplay/Bell/BellLife.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Bell/BellLife.cs) (dòng 120–245).
    - `[HIỆN TẠI]` **Đổi kênh Radio & Nhảy theo nhạc (`BellLife.cs`):** Ở nhà, Bell nhún nhảy theo tiếng nhạc
      radio; khi 07 đứng trong tầm `4.5 m` (`TuneRange = 4.5f`) và chạm nhẹ tia vào núm vặn của cô trong
      `0.35 s`, Bell xoay núm `45°` và chuyển vòng tròn giữa 3 kênh radio (`Lumen After Dark`,
      `Máy phát băng`, `Giờ yên tĩnh` — khi sang `Giờ yên tĩnh`, Bell xếp chân nằm nghỉ).
    - `[HIỆN TẠI]` **Bắt sóng tín hiệu & Reo vui khi có kỷ vật mới (`BellSignals.cs`, `BellLife.cs`):** Trong
      [`Assets/_Project/Scripts/Gameplay/Signals/BellSignals.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Signals/BellSignals.cs)
      (dòng 35–110), Bell dò và chọn các mục tiêu tín hiệu chưa tới (`SignalTarget` — bạn đồng hành, kho
      hàng, xác tàu) để dẫn dắt vòng lặp dò sóng (`ticker.bell.signal`), đồng thời phát tiếng lách tách vui
      mừng kèm dòng chữ `ticker.bell.found_0..2` mỗi khi 07 đặt thêm một kỷ vật mới lên kệ trong Phòng thí nghiệm.

- **3. Các bạn đồng hành dự kiến ở các bản mở rộng sau (`0.7` & `0.8`):**
  - `[ĐỀ XUẤT]` **Atlas (`0.7` — *Rim Terraces*):** Xe kéo hạng nặng 6 bánh của trạm Lumen đang đỗ ở công
    trường nối cáp trên Thềm Vành Đai; sau khi được sửa sẽ giúp kéo các kiện hàng siêu trường và mở cầu qua
    khe nứt lớn ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.6).
  - `[ĐỀ XUẤT]` **Moss (`0.8` — *Shadowed Crater / Lava Tube*):** Robot chăm vườn của Theo đang lấy mẫu băng
    ở Hố Đen Vĩnh Cửu; sau khi được sửa sẽ chăm sóc mảng rêu sống dưới đèn hẹn giờ trong hầm trú bão ống
    dung nham ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2).

### 4.3 Hệ thống Pin, Năng lượng & Chu kỳ Mặt Trời (`BatteryModel`, `BatteryTuning`, `BatteryField`)

- **Dung lượng ô pin (`Cells`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs)
    (dòng 14–22) và [`Assets/_Project/Scripts/Gameplay/Battery/BatteryModel.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryModel.cs),
    pin của 07 được chia thành các ô rời rạc hiển thị trên thanh 8 đèn `BatteryLamp_0..7`: khởi đầu với
    **`4` ô pin** (`_startingCells = 4`), tăng lên **`6` ô pin** (`_cellPackCells = 6`) khi lắp nâng cấp
    **`Cell Pack II`** (`RoverAbility.BatterySpine`), và trần thiết kế tối đa là **`8` ô** (`_maxCells = 8`).
- **Mức tiêu hao năng lượng khi lái xe và dùng công cụ:**
  - `[HIỆN TẠI]` **Lái xe (`BatteryModel.TickDrive`, `BatteryTuning.cs` dòng 24–43):** Trên mặt đất bằng
    phẳng, 07 tiêu hao **`1 ô pin cho mỗi 120 m`** di chuyển (`_metresPerCell = 120f`, tức đi được `480 m`
    với 4 ô hoặc `720 m` với 6 ô). Khi leo dốc (`15°` chuẩn `_referenceSlopeDegrees = 15f`), mức hao tăng
    tối đa gấp **`1.5×`** (`_uphillMultiplier = 1.5f`); khi xuống dốc giảm còn **`0.5×`**
    (`_downhillMultiplier = 0.5f`); khi đang kéo kỷ vật (`isTowing`) nhân **`1.25×`** (`_towMultiplier = 1.25f`).
  - `[HIỆN TẠI]` **Tăng tốc & Tụ điện sườn (`Boost Coils`, `BatteryTuning.cs` dòng 41–55):** Giữ phím tăng
    tốc (`isBoosting`) nhân mức hao lái xe lên **`2.0×`** (`_boostMultiplier = 2f`), nhưng phần năng lượng
    tăng thêm này được trừ trước vào cặp trống tụ điện **`Boost Coils`** có dung lượng **`0.25` ô pin**
    (`_drumCapacityCells = 0.25f`); khi 07 lăn xuống dốc `≥ 5°` (`_regenSlopeDegrees = 5f`) ở tốc độ
    `≥ 3.5 m/s` (`_regenMinSpeed = 3.5f`), động năng thu hồi sẽ nạp đầy lại trống tụ trong **`6.0 s`**
    (`_drumRegenSeconds = 6f`).
  - `[HIỆN TẠI]` **Dùng tia công cụ (`BatteryModel.TickBeam`, `BatteryTuning.cs` dòng 57–60):** Cắt phế
    liệu, đào đất, hàn cáp hoặc nạp phễu tiêu hao **`1 ô pin cho mỗi 60 s`** hoạt động liên tục
    (`_beamSecondsPerCell = 60f`; riêng khi cắt có lắp `Focus Lens`, mức hao giảm còn `0.85×` theo
    [`Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/CutterTuning.cs) dòng 58).
  - `[HIỆN TẠI]` **Bật nhảy (`BatteryModel.SpendJump`, `BatteryTuning.cs` dòng 62–67):** Mỗi cú nhảy
    `Hover-Jump` tiêu hao từ **`0.05` ô pin** (nhấn nhẹ, `_jumpMinCells = 0.05f`) đến **`0.25` ô pin**
    (tích lực tối đa, `_jumpMaxCells = 0.25f`).
- **3 nguồn sạc pin trong game hôm nay:**
  - `[HIỆN TẠI]` **Đế sạc tại Nhà (`Home Dock`):** Sạc với tốc độ **`4.0 s / ô pin`**
    (`_dockSecondsPerCell = 4f`, đầy 4 ô trong `16 s`) kèm lưu game
    ([`Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs) dòng 70).
  - `[HIỆN TẠI]` **Vòng sạc tại Trạm tiếp sóng đã sửa (`Lit Relay Mast`):** Mỗi trạm tiếp sóng đã thắp sáng
    có một vòng sạc bán kính `2 m` (đường kính `4 m`); sau khi 07 đỗ yên `≤ 0.3 m/s` trong `0.5 s`
    (`_settleSpeed = 0.3f`, `_settleSeconds = 0.5f`), trạm sạc cho 07 với tốc độ **`5.0 s / ô pin`**
    (`_mastSecondsPerCell = 5f`)
    ([`Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs) dòng 73–82,
    [`Assets/_Project/Scripts/Gameplay/Battery/BatteryField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryField.cs) dòng 210–255).
  - `[HIỆN TẠI]` **Tấm pin mặt trời của Tilly (`SolarCell`):** Sau khi sửa xong Tilly, khi 07 đứng yên ngoài
    nắng (`≤ 0.2 m/s` trong `3.0 s`, `_solarStillSpeed = 0.2f`, `_solarStillSeconds = 3f`), cánh pin mặt
    trời sạc nhỏ giọt với tốc độ **`180 s / ô pin`** (`_solarSecondsPerCell = 180f`), giới hạn tối đa đến
    **`1.0` ô pin** (`_trickleCap = 1f`) để 07 luôn có thể tự hồi phục ngoài thực địa
    ([`Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Battery/BatteryTuning.cs) dòng 84–95).
- **Cảnh báo pin yếu & Chế độ Bò dự phòng (`Reserve Crawl` — không bao giờ chết):**
  - `[HIỆN TẠI]` **Cảnh báo `Low` và `VeryLow` (`BatteryTuning.cs` dòng 97–115, `BatteryField.cs`):** Trạng
    thái `Low` bật khi pin còn **`< 30%`** (`_lowFraction = 0.30f`) HOẶC khi quãng đường bằng còn lại thấp
    hơn **`1.5 × (khoảng cách tới trạm sạc sáng gần nhất × 1.2)`** (`_returnSafetyFactor = 1.5f`,
    `_pathDetourFactor = 1.2f`) lúc đang ở cách trạm sạc trên `40 m` (`_nearChargerDistance = 40f`); trạng
    thái `VeryLow` bật khi pin còn **`< 15%`** (`_veryLowFraction = 0.15f`).
  - `[HIỆN TẠI]` **Chế độ Bò dự phòng (`ReserveCrawl`, `BatteryTuning.cs` dòng 117–132, `BatteryField.cs`
    dòng 280–360):** Theo đúng `docs/VISION.md` Ruling 15, 07 không bao giờ chết vì hết pin. Khi về `0` ô
    pin, 07 chuyển sang `ReserveCrawl`: tốc độ tối đa giảm còn **`35%`** (`_crawlCap = 0.35f`), đèn pha hạ
    xuống `35%`, khóa `Boost` và `Hover-Jump` nhưng vẫn cho phép cắt/hàn cơ bản; sau **`5.0 s`**
    (`_leadDelay = 5f`), nếu Tilly đã thức dậy, cô bé bay ra dẫn đường về trạm sạc gần nhất và nâng tốc độ
    bò của 07 lên **`55%`** (`_leadCap = 0.55f`); chế độ bò kết thúc ngay khi chạm trạm sạc hoặc hồi được
    **`≥ 0.25` ô pin** (`_crawlExitCells = 0.25f`).
- **Chu kỳ Ngày / Đêm (`L1`) cho bản `0.6`:**
  - `[ĐỀ XUẤT]` Trong bản `0.6` (*The Long Mare*), bổ sung chu kỳ Mặt Trời di chuyển chậm sát đường chân
    trời cực Nam (mỗi chu kỳ dài khoảng `2 giờ` chơi thực tế): khi bóng râm quét qua vùng trũng, các đế sạc
    mặt trời không có ắc-quy dự trữ sẽ tạm nghỉ cho tới khi nắng quay lại, tạo lý do tự nhiên để người chơi
    nối cáp lưới điện từ tháp hoặc dùng `Cell Pack II`
    ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §3.0).

---

## 5. Phương án Phong cách Nghệ thuật (Art Style Options)

- `[HIỆN TẠI]` Hiện tại game đang dùng mesh đa giác thấp sinh bằng code với vật liệu màu đỉnh phẳng và ánh
  sáng chiều đơn giản
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P1).
- `[ĐỀ XUẤT]` Để giải quyết triệt để phản hồi P1 của chủ dự án trong bản `0.5.7`, chúng tôi trình 2 phương
  án nghệ thuật giữ nguyên bảng màu ấm áp của 07 (`#F4E6C8`, `#FF8A5B`, `#3E4048`, `#78A582`):

| Tiêu chí | Phương án A: Low-Poly Stylised Nâng cấp (`[ĐỀ XUẤT]` — Khuyên dùng) | Phương án B: Ghibli / Cel-Shaded Ấm áp (`[ĐỀ XUẤT]`) |
|---|---|---|
| **Đặc trưng thị giác** | Giữ hình khối đa giác thấp góc cạnh thập niên 1970, nhưng thêm độ nhám bề mặt regolith (micro-normal), đổ bóng khe hẹp (AO), viền sáng rim-light ấm từ ánh phản chiếu Trái Đất và sương bụi mỏng theo địa hình. | Đổ bóng bậc thang 2–3 sắc độ mềm (cel-shading), viền nét mảnh theo độ sâu, mặt đất và sườn đồi chuyển sắc màu phấn ấm như tranh minh họa khoa học viễn tưởng cổ điển. |
| **Tương thích với code hiện có** | Tận dụng trực tiếp toàn bộ hệ thống `RoverMeshes.cs`, `BuildingMeshes.cs` và mô hình Blender `07 v2` mà không phải làm lại toàn bộ quy trình sinh mesh. | Cần viết lại bộ shader Toon/Outline và điều chỉnh lại pháp tuyến (smooth normals) của toàn bộ đá, địa hình và công trình để mảng bóng không bị gãy. |
| **Rủi ro & Thời gian** | Thấp (`~3–4 ngày` cho shader địa hình, ánh sáng và hậu kỳ); giữ đúng chất cơ khí sắc nét của 07. | Trung bình–Cao (`~1.5–2 tuần`); dễ làm các chi tiết cơ khí nhỏ trên 07 bị nhòe nếu viền outline dày. |

---

## 6. Phương án Định hướng Âm thanh (Audio Direction — Tinh chỉnh Phương án C)

- `[HIỆN TẠI]` Toàn bộ âm thanh trong game hôm nay được tổng hợp bằng code C# (`Procedural audio synthesis`
  trong `Assets/_Project/Scripts/Audio/`), mang lại âm nền ấm nhưng tiếng cắt, tiếng hàn, tiếng quét và phản
  hồi sonar còn dùng chung âm sắc sóng sin/tam giác khá giống nhau
  ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §2 P2 & P6).
- `[ĐỀ XUẤT]` Áp dụng **Phương án C Tinh chỉnh (Hybrid Procedural Synth + Lớp tiếng cơ khí vật lý)** trong
  bản `0.5.7` để mỗi thiết bị trên `07 v2` có một chữ ký âm thanh riêng biệt mà vẫn giữ dung lượng gọn nhẹ:
  1. `[ĐỀ XUẤT]` **Gầu xúc trước (`DigScoop`):** 3 nhịp tiếng piston thủy lực rít nhẹ kèm tiếng răng thép
     cào sỏi regolith và tiếng bật nắp khi kỷ vật trồi lên (thay cho tiếng ngân hư ảo của `TractorBeam`).
  2. `[ĐỀ XUẤT]` **Đầu cắt mũi xe (`CutterTurret`) & Cụm nạp `ChargeCells`:** Giữ tiếng nạp 3 tầng tăng dần
     cao độ của `ChargeCells`, thêm tiếng đánh lửa hồ quang giòn khi bắt đầu cắt và tiếng bật chốt kim loại
     trầm khi mảnh phế liệu rời ra; tách hẳn với tiếng hàn (`RepairBeam`) dùng xung điện lách tách mềm.
  3. `[ĐỀ XUẤT]` **Trống tời cáp (`WinchDrum`) & Khung cẩu (`HoistBail`):** Tiếng bánh cóc dây cáp quay lạch
     cạch khi giữ phím thu dây (`Thu dây`), tiếng sỏi trượt dưới đáy kỷ vật khi kéo lê trên đất, và tiếng
     mô-tơ khung chữ A nâng lên kèm tiếng ngàm khóa kịch vào `CradleSeat`.
  4. `[ĐỀ XUẤT]` **Nắp khoang hàng (`CargoHatch`) & Con lăn (`IntakeRoller`):** Tiếng ty hơi mở nắp khẽ xì
     và tiếng con lăn cao su cuốn từng bó vật liệu vào khoang hoặc bắn vào phễu trạm.
  5. `[ĐỀ XUẤT]` **Lắng nghe (`SonarSystem`):** Thay tiếng ping sonar tàu ngầm bằng tiếng xoay cơ khí khẽ
     của vòng ăng-ten `SonarLoop` và âm chuông cộng hưởng 3D định hướng (stereo/HRTF) phát ra từ đúng vị
     trí vật thể trả lời.

---

## 7. Cốt truyện, Thế giới Logic & Kiểm toán Lời thoại (`[HIỆN TẠI]` vs Bản nháp `Story v3` `[ĐỀ XUẤT]`)

> **Lưu ý quan trọng:** Toàn bộ nội dung mở rộng của `Story v3` dưới đây (bằng chứng khảo sát nước băng của
> Ines, trạm thứ hai **Meridian** của **Tycho Consortium**, nguyên nhân mảng pin chảo lưu trữ bị phủ bụi năm
> 2067, và bảng viết lại 14 dòng thoại) đều là **BẢN NHÁP `[ĐỀ XUẤT]` đang chờ chủ dự án trả lời 7 câu hỏi
> tại Mục 9** (nguồn: [`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md)).
> Các khuyến nghị của nhóm thiết kế chỉ được đặt tại Mục 9.

### 7.1 Những gì đang có trong game hôm nay (`[HIỆN TẠI]`) so với Bản nháp `Story v3` (`[ĐỀ XUẤT]`)

- **Quy trình Khôi phục Lumen (`Lumen Recovery Protocol`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Data/Localization/vi.json)
    (dòng 238–256), bảng tiến trình trong Phòng thí nghiệm hiện có 4 bước:
    1. `recovery.power.name`: *Cấp điện cho trạm*.
    2. `recovery.link.name`: *Nối mạng lưới tới Đỉnh núi*.
    3. `recovery.dish.name`: *Sửa chảo ăng-ten lớn*.
    4. `recovery.answer.name`: *Trả lời Trái Đất*.
  - `[ĐỀ XUẤT]` Bản nháp `Story v3` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §1)
    bổ sung động lực xuyên suốt từ `0.5` đến `1.0`: năm 2047, Trạm Lumen (gồm 4 thành viên: trưởng nhóm địa
    chất **Ines Kalani**, kỹ sư cơ khí **Kenji**, nhà thực vật **Theo**, phát thanh viên **Ro Amadi**) phải
    sơ tán khẩn cấp trong bão Mặt Trời; chương trình trên Trái Đất bị đóng cửa với kết luận *"không đáng để
    quay lại"*, nhưng trước khi rời đi Ines đã tìm thấy bằng chứng về một túi nước băng lớn dưới lòng đất
    (cùng dữ liệu dò sóng khớp từ trạm tự động **Meridian** bên kia vành núi). Cơn bão làm hỏng chảo ăng-ten
    lớn và làm rơi vệ tinh chuyển tiếp **Kestrel-3** trước khi dữ liệu kịp gửi về, nên 07 thực thi quy trình
    khôi phục để thu thập **6 mảnh bằng chứng khảo sát (`Survey pieces`)** và truyền về Trái Đất.
- **Kỷ vật (`Keepsakes`) và Máy phát băng của Ro (`TapeDeck`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Data/Localization/vi.json)
    (dòng 542–588), bảng chuỗi chữ hiện có 6 mục `relic.*`: `relic.cassette_player` (*Cuộn băng của Ro*),
    `relic.rubber_duck` (*Cố vấn gỡ lỗi cấp cao*), `relic.golden_record` (*Đĩa vàng Lumen*),
    `relic.astronaut_boot` (*Đôi ủng của Ines*), `relic.teapot` (*Ấm trà thật sự đầu tiên*), và
    `relic.garden_gnome` (*Trưởng ban An ninh*); đồng thời tại Gara của Kenji đã có thiết bị `TapeDeck`
    trong thùng `"07 · SPARE"`
    ([`Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs)).
  - `[ĐỀ XUẤT]` Theo quyết định trong `Story v3` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2),
    Kệ kỷ vật giữ **5 kỷ vật cá nhân** (vịt cao su, thần lùn giữ vườn, ấm trà, đĩa vàng, đôi ủng của Ines),
    viên sỏi xanh thuộc về mạch bằng chứng khảo sát của Ines, còn máy phát băng của Ro là **linh kiện vận
    hành trong thùng `"07 · SPARE"`** (sẽ xóa các khóa `relic.cassette_player.*` khi hoàn tất dọn dẹp C9).
- **Các rover đàn anh (`01–06`) và sinh thái máy móc hai trạm (`Lumen` & `Meridian`):**
  - `[HIỆN TẠI]` Trong bản `0.5` hôm nay, đàn anh **`06`** nằm nghỉ cạnh nhà trạm tiếp sóng cửa hẻm núi
    (`relay.2` tại `(-70, -180)`) và giữ 2 mảnh bản vẽ `Cell Pack II`
    ([`docs/design/0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.3 & §1.7).
  - `[ĐỀ XUẤT]` Trong bản nháp `Story v3` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2 & §3),
    tất cả các rover đàn anh **`01–06` đều đã dừng nghỉ hẳn** tại đúng chướng ngại vật mà giờ đây 07 vượt qua
    được nhờ trang bị mới; mỗi đàn anh mang một bảng kê nhiệm vụ kỹ thuật ngắn gọn (ví dụ
    `03 · TASK: FETCH ICE CORE CACHE · STOPPED: NO CHARGE`), hoàn toàn không có cảnh ủy thác tình cảm hay
    đánh thức đàn anh.
  - `[ĐỀ XUẤT]` Bản nháp `Story v3` phân định hai họ máy móc có nguồn điện và mục đích hợp lý:
    1. **Họ Lumen:** sơn kem sọc cam, đèn hổ phách tròn, đế sạc cảm ứng tròn, cài chế độ `"tiếp tục"` của Kenji.
    2. **Họ Meridian (`Tycho Consortium`):** vỏ trắng vạch xanh cổ vịt, mặt lục giác, đế sạc tiếp điểm lục
       giác, tự về `safe mode` khi mất kết nối với bộ điều phối nội bộ, trừ các máy nằm trong tầm nút liên
       lạc trên Thềm Vành Đai vẫn chạy suốt nhiều thập kỷ dưới nắng cực Nam để chất đầy dữ liệu vào kho
       **Meridian's vault**.
- **Giải quyết mạch "Sự im lặng năm 2067" và cái kết một cách hợp lý:**
  - `[ĐỀ XUẤT]` Trong bản nháp `Story v3` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §1),
    chảo liên lạc ở vùng Biển Dài (*The Long Mare*, bản `0.6`) lưu trữ kênh trò chuyện của 4 thành viên suốt
    20 năm sau ngày sơ tán rồi im bặt vào năm `2067` không phải vì con người bỏ rơi trạm, mà vì **robot quét
    bụi (`sweeper`) của mảng pin mặt trời nuôi chảo bị hỏng, khiến bụi phủ kín mảng pin và chảo mất điện
    ngừng thu sóng**.
  - `[ĐỀ XUẤT]` Khi 07 sửa xong chảo lớn trên Đỉnh núi ở bản `1.0` và truyền kho lưu trữ kèm 6 mảnh bằng
    chứng khảo sát về Trái Đất, người bắt được tín hiệu đầu tiên là **Ro** (người vẫn phát chương trình
    *Lumen After Dark* mỗi Chủ nhật qua chảo của câu lạc bộ vô tuyến nghiệp dư trên tần số cũ của trạm —
    chính tín hiệu đã được `relay.2` và `06` chuyển tiếp để đánh thức 07 ở đầu game), tiếp nối bằng phản hồi
    chính thức từ Houston lên kế hoạch đưa phi hành đoàn quay lại Mặt Trăng.

### 7.2 Bảng tổng hợp 6 Vùng từ `0.5` đến `1.0` trong Bản nháp `Story v3` (`[ĐỀ XUẤT]`)

| Vùng & Phiên bản | Trạng thái xác thực | Điểm thu hút tầm nhìn (Lure) | Phát hiện chính trong vùng | Mảnh bằng chứng khảo sát (`Survey piece`) | Đàn anh dừng nghỉ tại vùng |
|---|---|---|---|---|---|
| **1. Lòng chảo & Hẻm núi (`0.5`)** | `[HIỆN TẠI]` Địa hình, nhà, gara, giàn khoan, Kestrel-3, khe vực, Tilly & Bell đã có trong game ([`0.5.5-owner-feedback.md`](file:///D:/Project/MoonProject-wt/agy-plan/docs/design/0.5.5-owner-feedback.md) §1.7). `[ĐỀ XUẤT]` Nội dung chữ `Story v3`. | Đèn báo lỗi đỏ trên Đỉnh núi; ánh đèn của Bell bên kia khe vực. | Vệ tinh **Kestrel-3** mang phù hiệu lạ `"TYCHO CONSORTIUM · MERIDIAN RELAY"`; nhật ký lõi khoan `"bất thường"` của Ines tại giàn khoan; nhật ký chuyển tiếp của `06`. | `[ĐỀ XUẤT]` **Mảnh 1:** Dữ liệu lõi khoan của Ines (bộ ghi tại giàn khoan). | **`06`** (`[HIỆN TẠI]` ở nhà trạm `relay.2` cửa hẻm núi). |
| **2. Biển Dài — *The Long Mare* (`0.6`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.6` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2). | Vệt xoáy từ trường sáng và đèn chớp hổ phách của chảo lưu trữ. | Kho lưu trữ 20 năm thư từ của nhóm (dừng năm 2067 do hỏng máy quét bụi); bầy drone khảo sát Meridian (`survey flock`); máy đào rãnh cáp liên trạm của Lumen (`trencher`). | `[ĐỀ XUẤT]` **Mảnh 2:** Dữ liệu dò sóng của bầy drone; **Mảnh 3:** Kho lưu trữ chảo liên lạc. | `[ĐỀ XUẤT]` **`05`** (dừng nghỉ cạnh chảo liên lạc). |
| **3. Thềm Vành Đai — *Rim Terraces* (`0.7`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.7` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2). | Các bậc đá sáng và đèn chớp trắng của **Trạm Meridian** nhìn thấy từ bờ bắc Biển Dài. | **Trạm Meridian** vẫn hoạt động tự động dưới nắng: máy phân loại mẫu, máy gạt bụi, robot leo cáp; xe kéo **Atlas** của Lumen đỗ tại công trường nối cáp. | `[ĐỀ XUẤT]` **Mảnh 4:** Kho dữ liệu **Meridian's vault** (bản đồ toàn bộ túi nước băng). | `[ĐỀ XUẤT]` **`04`** (nghiêng trên vách thềm đá). |
| **4. Hố Đen Vĩnh Cửu — *Shadowed Crater* (`0.8`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.8` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2). | Lòng chảo tối đen có ánh xanh lục ở hướng tây nam Biển Dài. | Các máy dò băng của Meridian (`ice tappers`) nằm chờ nối lại cáp và tín hiệu; robot làm vườn **Moss** của Theo đang lấy mẫu băng. | `[ĐỀ XUẤT]` **Mảnh 5:** Mẫu lõi băng thực chứng (`ice core cache`). | `[ĐỀ XUẤT]` **`03`** (dừng vì hết pin trong bóng tối). |
| **5. Ống Dung Nham — *The Lava Tube* (`0.8`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.8` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2). | Ánh sáng hắt lên từ giếng trời tự nhiên (`skylight`). | Hầm trú bão kiêm máy chủ sao lưu chạy bằng pin đồng vị kín; một bóng đèn hẹn giờ vẫn bật nuôi **mảng rêu xanh còn sống** của Theo. | `[ĐỀ XUẤT]` **Mảnh 6:** Bản sao lưu phân tích tổng hợp của Ines (`shelter backup`). | `[ĐỀ XUẤT]` **`02`** (không leo ra khỏi dốc ống được). |
| **6. Đường lên Đỉnh Núi — *The Peak Road* (`1.0`)** | `[ĐỀ XUẤT]` Kết thúc hành trình `1.0` ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §2). | Đèn báo lỗi đỏ trên đỉnh chảo ăng-ten lớn. | Đống đá sạt từng chặn đường sửa chảo; trạm chảo ăng-ten lớn trên đỉnh vẫn có điện từ cột pin mặt trời cao. | `[ĐỀ XUẤT]` Truyền toàn bộ kho lưu trữ và 6 mảnh khảo sát về Trái Đất. | `[ĐỀ XUẤT]` **`01`** (dừng trước đống đá sạt). |

### 7.3 Kiểm toán Lời thoại: Không viết lời tâm sự mùi mẫn từ người gửi cho máy (`[ĐỀ XUẤT]`)

- `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Data/Localization/vi.json),
  hầu hết thư trên cột tiếp sóng (`letter.mast_1..4, 6..8, 10`) và ghi chú sửa chữa (`friend.bell.repair_log`,
  `friend.tilly.repair_log`, `crate.garage.note`) đã viết theo đúng giọng con người nói với con người hoặc
  ghi chú kỹ thuật hóm hỉnh về công cụ, nhưng vẫn còn **14 khóa chữ** đang để con người tâm sự trực tiếp với
  07 như một người bạn tri kỷ.
- `[ĐỀ XUẤT]` Theo quy tắc của chủ dự án (*"con người coi robot là công cụ; cá tính của 07 thể hiện qua hành
  vi, không qua lời người khác tâm sự với nó"*), bản nháp `Story v3`
  ([`Logs/sources/story-v3.md`](file:///D:/Project/MoonProject-wt/agy-plan/Logs/sources/story-v3.md) §4.1)
  đề xuất viết lại 14 khóa chữ dưới đây (chỉ áp dụng vào `vi.json` / `en.json` sau khi chủ dự án duyệt câu
  hỏi `S5` ở Mục 9):

| Khóa (`vi.json`) | Trạng thái & Vấn đề hiện tại (`[HIỆN TẠI]`) | Bản viết lại tiếng Việt đề xuất (`[ĐỀ XUẤT]` — chờ duyệt câu S5) |
|---|---|---|
| `log.ro_1` | `[HIỆN TẠI]` Coi 07 như thính giả lăn lại gần nghe nhạc (`vi.json` dòng 730). | `[ĐỀ XUẤT]` *"Đêm đầu tiên của Lumen After Dark. Ba thính giả, một chậu húng quế, và một chiếc rover đỗ cạnh loa trên bệ sạc. Khán giả tuyệt nhất mình từng có. — Ro"* |
| `log.ro_2` | `[HIỆN TẠI]` Mô tả 07 sợ bão nên lăn sát vào loa. | `[ĐỀ XUẤT]` *"Tối nay đọc cảnh báo bão trên sóng, kẹp giữa hai bài chậm cho đỡ đáng sợ. Kenji đã cho mấy chiếc rover đỗ khuất sau tàu đổ bộ. — Ro"* |
| `log.ro_3` | `[HIỆN TẠI]` Dặn dò mùi mẫn trực tiếp với 07 trước lúc lên tàu sơ tán. | `[ĐỀ XUẤT]` *"Buổi phát sóng cuối trước chuyến tàu. Máy phát để chế độ 'liên tục'. Ai còn dò đúng sóng thì: Lumen After Dark, tạm chào nhé. — Ro"* |
| `log.ines_3` | `[HIỆN TẠI]` Tâm sự trực tiếp với 07 (*"chị muốn em biết tụi chị đã cố gắng"*). | `[ĐỀ XUẤT]` *"Gửi hội đồng xét duyệt: các mẫu lõi của tôi bất thường là có lý do. Bằng chứng nằm trong ổ dữ liệu của trạm. Hãy cho chúng tôi quay lại lấy. — I. Kalani"* |
| `log.kenji_1` | `[HIỆN TẠI]` Kể chuyện dạy 07 nháy mắt chào mình mỗi khi đi ngang. | `[ĐỀ XUẤT]` *"Hôm nay sửa xong chiếc rover khảo sát cũ, món thứ bảy trong danh sách tuần này, nên giờ nó là 07. Cài ăng-ten nháy tín hiệu gọi của trạm làm đèn trạng thái: ba ngắn, một dài. — Kenji"* |
| `log.kenji_3` | `[HIỆN TẠI]` Nhắn nhủ 07 *"đừng để ai cho nghỉ hưu nhé"*. | `[ĐỀ XUẤT]` *"Người ta hỏi xử lý mấy chiếc rover thế nào. Mình để chúng ở chế độ 'tiếp tục': tắt hẳn mất cả ngày, mà tụi mình không còn ngày nào. — Kenji"* |
| `letter.mast_5` | `[HIỆN TẠI]` Mở đầu thư bằng *"07 à, Ro đây"*. | `[ĐỀ XUẤT]` *"Kênh phi hành đoàn Lumen, Ro đây. Người ta bảo trên đó chẳng còn ai nghe. Mình vẫn cứ gửi."* |
| `letter.mast_9` | `[HIỆN TẠI]` Nhắn trực tiếp với 07 cách tìm Ro bằng mã nháy đèn. | `[ĐỀ XUẤT]` *"Ines bảo bằng chứng vẫn nằm trong ổ dữ liệu của trạm. Nếu cái chảo ấy hoạt động lại, Chủ nhật nào mình cũng nghe ở tần số cũ. Ba ngắn, một dài. — Ro"* |
| `letter.after_3` | `[HIỆN TẠI]` Hỏi thăm trực tiếp 07 xem còn giữ chiếc bánh xe lẻ không. | `[ĐỀ XUẤT]` *"Kenji đọc số liệu đo xa: cả sáu bánh của chiếc rover cũ giờ đã giống nhau. Anh ấy phải ngồi xuống. — Ro"* |
| `letter.after_4` | `[HIỆN TẠI]` Gọi 07 là khán giả tuyệt nhất. | `[ĐỀ XUẤT]` *"Tối nay mình phát bài 'cho ngày mình về nhà'. Khán giả: hai người hàng xóm và một con mèo đã rất già. Khán giả tuyệt nhất kể từ hồi ở trạm. — Ro"* |
| `letter.after_6` | `[HIỆN TẠI]` Trò chuyện trực tiếp với 07 về bầu trời trên trạm. | `[ĐỀ XUẤT]` *"Thời tiết đêm nay: mưa ở Lagos. Trên trạm thì trời vẫn quang như mọi khi, số liệu bảo vậy. — Ro"* |
| `letter.after_8` | `[HIỆN TẠI]` Đùa trực tiếp với 07 (*"đừng nín thở nhé, dù đằng ấy có thở đâu"*). | `[ĐỀ XUẤT]` *"Bốn đứa tụi mình cùng viết thư cho cơ quan vũ trụ. Ines bảo lần này họ buộc phải đọc. — Ro"* |
| `letter.after_10` | `[HIỆN TẠI]` Chúc 07 ngủ ngon trực tiếp. | `[ĐỀ XUẤT]` *"Ngủ ngon, Lumen. Chủ nhật nào cũng có chương trình, cho đến khi phi hành đoàn hạ cánh. — Ro"* |
| `ticker.ending.answer` & `ticker.bell.found_2` | `[HIỆN TẠI]` Ro trả lời *"Mình nghe thấy cậu rồi, 07"* và Bell đoán cảm xúc của Ro. | `[ĐỀ XUẤT]` Đổi `ticker.ending.answer` thành *"Lumen, Amadi đây. Tụi mình nghe rõ. Ines đang trên đường tới câu lạc bộ."* (rồi Houston xác nhận); đổi `ticker.bell.found_2` thành *"Bell bảo: thêm một món cho kệ."* |

---

## 8. Lộ trình Thực thi Khả thi: Tách bạch `0.5.6`, `0.5.7` và `0.6+`

Lộ trình dưới đây tách rõ những việc sửa ngay không phụ thuộc khóa phong cách nghệ thuật (`0.5.6`) và những
việc thực hiện sau khi chủ dự án chốt phong cách nghệ thuật cùng mô hình `07 v2` (`0.5.7`), tiếp nối tới `1.0`:

### 8.1 Bản `0.5.6` — Sửa lỗi phản hồi 0.5.5 KHÔNG phụ thuộc khóa phong cách nghệ thuật (Art Style Lock)

| # | Hạng mục công việc trong `0.5.6` | Trạng thái & Nhánh/Commit xác thực |
|---|---|---|
| **1** | **Bỏ khóa cứng đầu của 07 (`RoverHead`):** Đầu 07 chỉ ngoái nhìn tự nhiên rồi trả về hướng lái, không bị ghim cứng vào vật thể ở xa. | `[HIỆN TẠI]` **Đã hoàn thành** (commit `3ab086d1` trong [`RoverHead.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Rover/RoverHead.cs) và [`SalvageField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Salvage/SalvageField.cs)). |
| **2** | **3 bộ phận của Bell nằm dưới đống đá sạt (`RockfallPocket`):** Đặt 3 bộ phận của Bell dưới 3 đống đá sạt trong hốc hẻm núi để 07 đào lên. | `[HIỆN TẠI]` **Đã hoàn thành** (commit `4bffd365` trong [`FriendField.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs) `LayThePockets`). |
| **3** | **Phím `Tab` cố định một màn hình & Bỏ thẻ ký ức bật lên khi nhặt đồ:** Gộp `Tab` thành một bảng `TabSheet` duy nhất (sơ đồ 07, pin, khoang hàng, bản vẽ) và tắt thẻ toàn màn hình khi nhặt kỷ vật/băng ngoài thực địa. | `[ĐỀ XUẤT]` **Đang thực hiện** trên nhánh `agy-ui2` (để không làm đứt nhịp lái xe). |
| **4** | **Sửa lỗi chồng mặt phẳng (z-fight) ở vòm Xưởng xe của Kenji:** Tách các mặt phẳng trùng tọa độ trên mái vòm `Repair Bay`. | `[ĐỀ XUẤT]` **Đang thực hiện** trên nhánh `agy-art` (sửa lỗi hình học độc lập trước khi khóa art style). |
| **5** | **Đưa linh kiện cốt truyện (`TowerCoil`, `Transformer`, `RelaySpare`) vào trong khoang hàng qua `CargoHatch`:** Khi nhặt linh kiện tháp/trạm, nắp `CargoHatch` mở ra đón linh kiện vào trong bụng xe, xóa việc đặt cuộn dây lơ lửng trên nóc phẳng `StoryPartSocket`. | `[ĐỀ XUẤT]` **Đang thực hiện** trên nhánh `agy-hold` (giải quyết ngay lỗi logic đặt cuộn dây trên lưng phẳng mà chủ dự án đã chỉ ra). |
| **6** | **Tia quét bản vẽ (`ScanFan`) nối từ mắt 07 tới vật thể:** Hiển thị chùm sáng quét quang học nối từ thấu kính mắt của 07 tới mảnh bản vẽ đang quét (giữ nguyên thông số `1.5 s / 2.5 s / 4.0 s`, tầm `8 m` trong `ScanTuning.cs`). | `[ĐỀ XUẤT]` **Đang thực hiện** trên nhánh `agy-scan` (làm rõ phản hồi thị giác khi quét). |
| **7** | **Gỡ bỏ điều kiện ẩn của thang nâng bụng (`LiftStation.cs`):** Bỏ điều kiện `Carrying() && NearHome()` tại dòng 353 của [`LiftStation.cs`](file:///D:/Project/MoonProject-wt/agy-plan/Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs) để ngay khi Tháp cấp 1 cấp điện cho nhà (`BasePowerStage.Home`), thang nâng bụng bật vòng sáng gọi 07 tới hàn cáp và mở đường lên Phòng thí nghiệm. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để người chơi không bị kẹt trước thang nâng khi về nhà tay không. |
| **8** | **Làm rõ nhịp hướng dẫn tại Gara của Kenji & Chống chồng chéo chữ (`GuidanceSlot`, `PromptDirector`):** Đưa ticker vào chung bộ phân xử `GuidanceSlot` để chỉ hiện đúng 1 dòng hướng dẫn tại một thời điểm, và dẫn dắt tuần tự từng bước tại `GaragePort` (dọn phế liệu mở cổng → đỗ lắp `TetherRing` → cắt thùng `"07 · SPARE"` lấy `TapeDeck`). | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để giải quyết triệt để phản hồi P3 của chủ dự án. |
| **9** | **Phản hồi lắng nghe (`SonarSystem`, `SiteMarker`) không dùng cột sáng trên trời:** Xóa cột sáng `32 m` trong `SiteMarker.cs` và vòng sóng dưới đất, thay bằng ánh lấp lánh tại vật thể + đầu 07 ngoái nhìn + âm thanh 3D định hướng. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để giải quyết phản hồi P2 của chủ dự án. |
| **10** | **Rà soát và sửa câu chữ tiếng Việt (`text-audit-0.5.5.md`):** Sửa các chuỗi hướng dẫn, gợi ý và thông báo chưa tự nhiên trong `vi.json`. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để đồng bộ với nhịp hướng dẫn mới. |

### 8.2 Bản `0.5.7` — Áp dụng Khóa Phong cách Nghệ thuật + Thiết kế `07 v2` + Âm thanh C Tinh chỉnh + Tiến trình Kéo/Nâng

1. `[ĐỀ XUẤT]` **Áp dụng phong cách nghệ thuật đã khóa (Art Style Lock):**
   - Triển khai phương án nghệ thuật được chủ dự án chọn ở Mục 9 (Low-Poly Stylised nâng cấp hoặc
     Ghibli/Cel-Shaded) cho địa hình lòng chảo, hẻm núi, đá và các công trình.
2. `[ĐỀ XUẤT]` **Triển khai mô hình và hợp đồng `07 v2` (`IRoverRig` 7 điểm gốc vật lý):**
   - Mở rộng `IRoverRig` thành `EyeOrigin`, `CutterOrigin`, `ScoopOrigin`, `HatchOrigin`, `WinchOrigin`,
     `CradleOrigin`, `AntennaOrigin`.
   - Đưa **gầu xúc trước `DigScoop` (`ScoopOrigin`)** vào làm thiết bị khởi đầu từ phút 0 (xóa `TractorBeam`
     ở mắt).
   - Đưa **đầu cắt & kim hàn mũi xe (`CutterTurret` / `CutterOrigin`)** vào nối với thanh tụ nạp 3 tầng
     `ChargeConduits` trên boong trước.
   - Đưa **nắp khoang đôi `CargoHatch` + con lăn `IntakeRoller` (`HatchOrigin`)** vào vận hành khi thu phế
     liệu và khi bắn vật liệu vào phễu trạm (`HopperFeed`).
   - Đưa **vòng ăng-ten định hướng xoay (`SonarLoop` / `AntennaOrigin`)** và **hộp pin đôi đối xứng
     (`BatterySpine` tại `x = ±0.32`)** lên lưng 07.
3. `[ĐỀ XUẤT]` **Hoàn thiện tiến trình Kéo trượt mặt đất → Cẩu lên lưng (`WinchDrum` vs `CargoCradle`):**
   - Trước khi tới Gara của Kenji: chưa có tời sau, chỉ đào được kỷ vật tại chỗ bằng `DigScoop`.
   - Sau khi lắp `TetherRing` / `WinchDrum` tại Gara của Kenji: kéo trượt kỷ vật trên mặt đất bằng cáp thép
     (`HoverHeight = 0f`, tạo vệt bụi và lực cản).
   - Sau khi chế tạo `Cargo Cradle` (*Giỏ chở đồ*) tại Xưởng xe: khung chữ A `HoistBail` cẩu kỷ vật từ cản
     sau lên `CradleSeat` trên lưng 07 để chạy nguyên tốc độ và đi thẳng lên thang nâng bụng.
4. `[ĐỀ XUẤT]` **Áp dụng hướng âm thanh C tinh chỉnh:**
   - Bổ sung đầy đủ lớp âm cơ khí cho gầu xúc, đầu cắt/hàn, tời cáp, khung cẩu, nắp khoang và âm chuông
     lắng nghe 3D định hướng.

### 8.3 Các bản `0.6 → 1.0` — Mở rộng Thế giới Sống & Hoàn tất Hành trình

| Phiên bản | Vùng mở rộng & Cơ chế chính (`[ĐỀ XUẤT]`) | Trang bị, Bạn đồng hành & Mảnh khảo sát mới (`[ĐỀ XUẤT]`) |
|---|---|---|
| **`0.6`** | **Biển Dài (*The Long Mare*):** Mở rộng về phía đông; thêm chu kỳ Mặt Trời `L1` (~2 giờ/chu kỳ) nếu chủ dự án duyệt; hệ thống bụi phủ tấm pin và máy đào rãnh cáp liên trạm (`trencher`). | Bản vẽ: `Cutter Ribs` (bậc cắt 2: `0.29 s`/tầng), `Dust Wiper`, `Solar Coat`. Sinh thái: bầy drone khảo sát Meridian (`survey flock`), đàn anh `05`. Khảo sát: **Mảnh 2** (dữ liệu bầy drone) & **Mảnh 3** (kho lưu trữ chảo 20 năm). |
| **`0.7`** | **Thềm Vành Đai (*Rim Terraces* — Trạm Meridian):** Các bậc thềm đá ngập nắng phía bắc; trạm tự động Meridian vẫn chạy bộ điều phối nội bộ; cơ chế nối cầu nguồn `Power Bridge`. | Bản vẽ: `Thermal Cutter` (bậc cắt 3: `0.22 s`/tầng), `Charge Adapter` (sạc trên đế lục giác Meridian). Bạn mới: **Atlas** (xe kéo hạng nặng của Lumen). Đàn anh `04`. Khảo sát: **Mảnh 4** (`Meridian's vault`). |
| **`0.8`** | **Hố Đen Vĩnh Cửu & Ống Dung Nham (*Shadowed Crater & Lava Tube*):** Vùng tối vĩnh cửu không có nắng mặt trời; hầm trú bão ngầm chạy bằng pin đồng vị kín với đèn hẹn giờ nuôi rêu xanh của Theo. | Bản vẽ: `Cold Lamp`, `Heater`. Bạn mới: **Moss** (robot làm vườn của Theo). Đàn anh `03` và `02`. Khảo sát: **Mảnh 5** (`ice core cache`) & **Mảnh 6** (`shelter backup`). |
| **`1.0`** | **Đường lên Đỉnh Núi (*The Peak Road* & *Great Dish*):** Vượt qua đống đá sạt nơi đàn anh `01` dừng lại, sửa chảo ăng-ten lớn trên đỉnh và truyền toàn bộ kho lưu trữ kèm 6 mảnh bằng chứng khảo sát về Trái Đất. | Hoàn tất bước 3 & 4 của Quy trình Khôi phục Lumen (`Dish` → `Answer`); nhận tín hiệu trả lời từ Ro Amadi và Houston, mở khóa chế độ tự do khám phá sau cái kết. |

---

## 9. Các Quyết định Cần Chủ dự án Phê duyệt (Owner Decisions & Recommendations)

Để đội ngũ khóa bản `0.5.6`, `0.5.7` và chốt chính thức bản nháp `Story v3`, xin chủ dự án cho ý kiến về
**3 quyết định sản phẩm (`D1–D3`)** và **7 câu hỏi cốt truyện (`S1–S7`)** dưới đây (kèm khuyến nghị của nhóm
thiết kế):

### 9.1 Quyết định Sản phẩm, Nghệ thuật, Thiết kế `07 v2` & Chu kỳ Mặt Trời (`D1–D3`)

1. **D1 — Chọn Phong cách Nghệ thuật cho bản `0.5.7` (Mục 5):**
   - *Câu hỏi:* Chủ dự án chọn **Phương án A (Low-Poly Stylised nâng cấp ánh sáng, độ nhám regolith và sương
     bụi)** hay **Phương án B (Ghibli / Cel-Shaded ấm áp)**?
   - *Khuyến nghị:* **Chọn Phương án A**, vì giữ trọn nét cơ khí thập niên 1970 của 07, tương thích trực tiếp
     với mô hình `07 v2` và hoàn thành nhanh trong `3–4 ngày` mà không có rủi ro làm nhòe chi tiết nhỏ.
2. **D2 — Phê duyệt Thiết kế vật lý `07 v2` & Tiến trình Kéo trượt → Cẩu lên lưng (Mục 3):**
   - *Câu hỏi:* Chủ dự án có đồng ý áp dụng trọn bộ thiết bị vật lý của `07 v2` (gầu xúc trước `DigScoop` có
     sẵn từ đầu, đầu cắt/hàn mũi xe `CutterTurret`, nắp khoang đôi `CargoHatch` + con lăn, vòng ăng-ten xoay
     `SonarLoop`, pin đôi đối xứng `BatterySpine`) và tách `WinchDrum` (kéo trượt trên mặt đất ở Gara của
     Kenji) với `Cargo Cradle` (khung chữ A `HoistBail` cẩu kỷ vật lên lưng ở Xưởng xe) cho bản `0.5.7` không?
   - *Khuyến nghị:* **Đồng ý trọn bộ**, vì giải quyết tận gốc mọi điểm phi lý mà chủ dự án đã nêu trong đợt
     chơi thử `0.5.5` (không còn tia cắt/đào/kéo bắn ra từ mắt camera, không còn kỷ vật tự bay lơ lửng trước
     khi chế tạo giỏ nâng).
3. **D3 — Chu kỳ Mặt Trời di chuyển (`L1`) ở bản `0.6` (Mục 4.3):**
   - *Câu hỏi:* Trong bản `0.6`, chủ dự án muốn áp dụng **Mặt Trời di chuyển chậm sát đường chân trời
     (~2 giờ chơi/chu kỳ)** làm bóng râm dịch chuyển qua các đế sạc mặt trời, hay giữ **góc nắng cố định
     theo từng vùng địa lý**?
   - *Khuyến nghị:* **Áp dụng Mặt Trời di chuyển chậm (~2 giờ/chu kỳ)** cho vùng Biển Dài, nhưng giữ các vùng
     cao (Đỉnh núi, Thềm Vành Đai Meridian) luôn đón nắng và vùng Hố Đen Vĩnh Cửu luôn tối để đúng với địa
     lý cực Nam Mặt Trăng.

### 9.2 7 Câu hỏi Chốt Bản nháp `Story v3` (`S1–S7` từ `Logs/sources/story-v3.md` §6)

1. **S1 — Hiển thị tiến độ 6 mảnh khảo sát (`Survey` `0/6`) trên bảng Quy trình Khôi phục trong Phòng thí nghiệm:**
   - *Câu hỏi:* Thêm **`Survey (0/6)`** thành dòng thứ 5 trên bảng Quy trình Khôi phục (`Power → Link → Dish →
     Survey → Answer`), hay gộp nó vào bên trong bước thứ 4 **`Answer`** (phải đủ khảo sát mới truyền tin được)?
   - *Khuyến nghị:* **Thêm thành một dòng đếm rõ ràng (`Survey 0/6`) ngay trên bước `Answer`**, để người chơi
     nhìn thấy tiến độ thu thập bằng chứng tăng lên trực quan sau mỗi vùng.
2. **S2 — Ai là người nghe thấy tín hiệu truyền về đầu tiên ở cái kết `1.0`:**
   - *Câu hỏi:* Giữ kịch bản **Ro Amadi nghe thấy trước** qua chảo của câu lạc bộ vô tuyến nghiệp dư (khép
     kín vòng lặp hợp lý với tín hiệu Chủ nhật đã đánh thức 07 ở đầu game, sau đó Houston trả lời chính thức),
     hay để **Houston trả lời trực tiếp ngay từ đầu**?
   - *Khuyến nghị:* **Giữ kịch bản Ro nghe thấy trước rồi tới Houston**, vì nó giải thích trọn vẹn bằng logic
     kỹ thuật câu hỏi *"Ai vẫn đang phát sóng trên tần số cũ lúc mở đầu game?"* mà không cần lời thoại ủy mị.
3. **S3 — Tên và sắc thái của cơ quan vận hành Trạm Meridian:**
   - *Câu hỏi:* Giữ tên **`Tycho Consortium`** như một tổ chức khảo sát thương mại đã dừng dự án vì bài toán
     chi phí (không phải phản diện, chỉ là quyết định sổ sách), hay đổi sang tên khác?
   - *Khuyến nghị:* **Giữ tên `Tycho Consortium` và sắc thái trung tính**, đúng với tinh thần không có kẻ ác
     hay âm mưu của `docs/VISION.md`.
4. **S4 — Điều kiện số mảnh khảo sát để truyền tín hiệu ở Đỉnh núi (`1.0`):**
   - *Câu hỏi:* Bắt buộc thu thập **đủ cả `6/6` mảnh khảo sát** mới truyền tín hiệu kết thúc được, hay chỉ
     cần đạt ngưỡng (ví dụ `4/6` mảnh) còn `6/6` dành cho người chơi hoàn thành 100%?
   - *Khuyến nghị:* **Yêu cầu đủ `6/6` mảnh khảo sát**, vì mỗi vùng trên đường từ `0.5` đến `0.8` đều gắn tự
     nhiên với đúng 1–2 mảnh khảo sát chính.
5. **S5 — Phê duyệt bảng viết lại 14 dòng thoại trong Kiểm toán Lời thoại (Mục 7.3):**
   - *Câu hỏi:* Chủ dự án có duyệt áp dụng 14 bản viết lại trong `Logs/sources/story-v3.md` §4.1 vào
     `vi.json` / `en.json` để xóa toàn bộ các câu con người tâm sự mùi mẫn với 07, chuyển thành lời người
     nói với người và ghi chú bảo trì không?
   - *Khuyến nghị:* **Duyệt áp dụng ngay trong đợt rà soát chữ `0.5.6`**, vì bám sát quy tắc *"con người coi
     robot là công cụ"* của chủ dự án.
6. **S6 — Tiến sĩ Lena Moreau làm tiếng nói con người phía Trạm Meridian:**
   - *Câu hỏi:* Giữ nhân vật **TS. Lena Moreau** (trưởng nhóm khoa học của Meridian ở dưới Trái Đất, trao
     đổi dữ liệu với Ines và viết ghi chú thực địa về máy móc Meridian trong mục Notes) bên cạnh 4 thành
     viên Lumen không?
   - *Khuyến nghị:* **Giữ TS. Lena Moreau**, vì tạo đối trọng khoa học tự nhiên với Ines Kalani và giúp các
     ghi chú kỹ thuật về máy móc họ Meridian có tác giả hợp lý.
7. **S7 — Thiết bị chuyển đổi sạc `Charge Adapter` ở bản `0.7`:**
   - *Câu hỏi:* Giữ thiết bị **`Charge Adapter`** (tìm thấy tại Trạm Meridian ở bản `0.7`, giúp 07 sạc trực
     tiếp trên các đế sạc lục giác của Meridian bên cạnh việc xây cầu nguồn `Power Bridge`) không?
   - *Khuyến nghị:* **Giữ `Charge Adapter` ở bản `0.7`**, vì mang lại phần thưởng khám phá rõ rệt khi người
     chơi đặt chân tới Trạm Meridian và giữ đúng logic hai chuẩn điện áp khác nhau giữa hai trạm.
