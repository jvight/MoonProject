# Kế Hoạch Tổng Thể Lunarion (0.5.5 → 1.0)

> **Phiên bản tài liệu:** Vòng 2, Director đã rà lại (2026-10-15). Đối chiếu với mã nguồn trên `main` (`36cbf853`) và tài liệu
> phản hồi đã chốt [`docs/design/0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md).
>
> **Quy ước gắn nhãn bắt buộc (áp dụng xuyên suốt, đặc biệt tại Mục 2, 3, 4 và 7 — không để bất kỳ câu khẳng định nào không có nhãn):**
> - **`[HIỆN TẠI]`**: Sự thật đang chạy trong mã nguồn C# hôm nay hoặc đã chốt trong
>   [`docs/design/0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md), kèm đường dẫn file xác thực.
> - **`[ĐỀ XUẤT]`**: Đề xuất thiết kế trình chủ dự án duyệt (cho các bản `0.5.6`, `0.5.7`, `0.6+` hoặc
>   bản nháp `story v3`), kèm một dòng lý do cụ thể.

---

## 1. Tóm tắt cho Chủ dự án (Executive Summary)

### 1.1 Hướng đi của game

Phần này gom lại những gì đã thống nhất trong [`north-star.md`](north-star.md) và các luật chủ dự án đã chốt,
để duyệt lại một lần.

**Game là gì.** 07, một rover nhỏ, thức dậy một mình trong trạm Lumen bị bỏ lại ở cực Nam Mặt Trăng. 07 sửa
từng thứ để nhà sống lại, tự lớn lên bằng những thiết bị mình chế tạo, đi mỗi lúc một xa hơn, và cuối cùng gửi
về Trái Đất bằng chứng rằng Mặt Trăng đáng để con người quay lại.

| Câu hỏi | Lunarion trả lời |
|---|---|
| Điều gì móc người chơi vào | 07 mở mắt, Trái Đất to và thấp, đèn đỏ nhấp nháy trên Đỉnh núi, một tín hiệu khẽ trong tiếng rè; ngôi nhà tối và đang ngủ. |
| Điều gì kéo họ đi tiếp | Những ánh đèn nhìn thấy mà chưa tới được, và một câu hỏi: ai vẫn đang phát sóng, và vì sao nơi này bị bỏ? |
| Điều gì giữ họ lại | Mọi thứ sửa được đều sống lại thấy rõ: thân 07, ngôi nhà, bản đồ, và máy móc trên Mặt Trăng tỉnh dậy theo. |
| Điều gì khiến họ chơi tới cuối | Mang bằng chứng về nhà: bước cuối của quy trình khôi phục hiện ra từ phút đầu, và cái kết giữ đúng lời hứa đó. |
| Cảm giác | Cô đơn mà kỳ thú, dần thành niềm tự hào lặng lẽ. |

**Vòng lặp chính.** Ra ngoài (pin giới hạn tầm đi) → quét, cắt, đào ở xác tàu, trạm, thùng hàng → mang về trong
khoang → về nhà sạc pin, chế tạo, lắp lên 07 ở xưởng → 07 hoặc ngôi nhà thay đổi thấy rõ → đi xa hơn.

**Luật không đổi (chủ dự án đã chốt).**
- Xương sống là sự lớn lên của 07 (nâng cấp, mở khóa, chế tạo, đi xa hơn); ký ức chỉ là gia vị.
- Mỗi nâng cấp làm 07 hoặc ngôi nhà thay đổi thấy rõ.
- Vật gì cũng phải đọc ra công dụng; mỗi việc 07 làm đều do một thiết bị thật làm (`07 v2`, Mục 3).
- Không có đồ phát sáng hay đồ nhặt được nằm trơ trên mặt đất; đồ nằm trong xác tàu, trạm, thùng.
- Mỗi lúc chỉ một yếu tố hướng dẫn; chữ ngắn, rõ, tiếng Việt tự nhiên; gợi ý chỉ khi cần và không lặp lại.
- Mỗi lần đổi trạng thái có một chuyển động cơ khí và một tiếng kêu nhỏ của 07 (thang ngũ cung Rê trưởng).
- Không khí cô đơn nhưng bình yên; 07 không bao giờ "chết" vì hết pin.
- Con người coi robot là công cụ; tính cách của 07 thể hiện qua hành vi. Không kể chuyện tình cảm người với máy.
- Phát hành theo pha, xong pha này mới sang pha sau: `0.5.6` → `0.5.7` → `0.6` → … → `1.0` (Mục 8).



---

## 2. Bản 0.5.5: chủ dự án thấy gì, game hôm nay ra sao, sửa thế nào

Nguồn: phản hồi chơi thử `0.5.5` của chủ dự án (2026-10-14) và code trên `main`. Đường dẫn trong ngoặc là nơi
kiểm chứng.

### P1. Phong cách chưa ra chất; vòm trong Xưởng xe bị sọc răng cưa
- `[HIỆN TẠI]` Thế giới, nhà và 07 là mesh low-poly sinh bằng code, tô màu theo đỉnh, đổ bóng phẳng
  (`Assets/_Project/Scripts/Art/Editor/`).
- `[HIỆN TẠI]` Trong ảnh chụp `0.5.5` của chủ dự án, vòm trong của Xưởng xe của Kenji (khối xưởng cạnh tàu đổ bộ ở
  nhà, nơi 07 đỗ để lắp đồ) hiện thành sọc răng cưa sáng tối.
- `[ĐỀ XUẤT]` Sửa vòm trong `0.5.6` (nhánh `agy-art`, đang làm), vì đây là lỗi hình học riêng, không phụ thuộc
  phong cách.
- `[ĐỀ XUẤT]` Chủ dự án chốt **một** phong cách trên trang bảng phong cách (3 hướng S1/S2/S3 và 4 mức mịn
  V0–V3, xem Mục 5) trước khi dựng `07 v2`, rồi áp dụng đồng loạt trong bản `0.5.7`.

### P2. Cột sáng xanh trên trời và vòng sóng dưới đất không rõ nghĩa; cuộn dây tháp nằm trên nóc 07
- `[HIỆN TẠI]` Lắng nghe không có nút (luật số 3 trong `docs/VISION.md`). Khi tháp đã có điện, 07 tự lắng nghe lúc
  đứng yên sau một quãng lái; lần đầu chỉ cần đứng yên. Ăng-ten sau bên phải nhô lên và nháy hai lần, một vòng
  sóng lan trên mặt đất tới `140 m`, và mỗi chỗ còn thứ để tìm trong tầm dựng một cột sáng cao `32 m` trong
  `20 s`; chiếu đèn pha ấm vào cột thì cột ở lâu hơn (`Gameplay/Sonar/`, `Data/Tuning/.../SonarTuning.asset`).
- `[HIỆN TẠI]` Linh kiện tháp và trạm (cuộn dây tháp, biến áp, phụ tùng relay) nằm trên một ổ phẳng trên nóc 07
  (`StoryPartSocket`, `Rover/Rig/StoryPartCarry.cs`).
- `[ĐỀ XUẤT]` `0.5.6`: bỏ cột sáng và vòng sóng; vật trả lời bằng một ánh nháy nhỏ sát đất tại chính nó, đầu 07
  ngoái nhìn về hướng đó, và tiếng trả lời phát ra từ đúng chỗ vật. `0.5.7`: thêm vòng ăng-ten xoay chỉ hướng
  (`07 v2`). Lý do: thế giới không còn "cột laser chỉ đường" giả.
- `[ĐỀ XUẤT]` `0.5.6`: linh kiện đi qua nắp khoang (`CargoHatch`) vào trong khoang 07 (nhánh `agy-hold`).
  `0.5.7`: nắp khoang thật, có con lăn thu và đẩy đồ (`07 v2`). Lý do: chủ dự án chọn "cho vào khoang hàng".

### P3. Gara của Kenji khó hiểu; kẹt ở thang nâng
- `[HIỆN TẠI]` Ở Gara của Kenji, cổng bảo trì mở khi 07 dọn hết phế liệu quanh gara. 07 đỗ lên bệ thì cổng lắp
  vòng dây kéo. Sau đó mối hàn thùng "07 · SPARE" sáng lên; 07 cắt thùng lấy máy phát băng, đưa lên khay rồi đỗ
  lại lên bệ để lắp (`Gameplay/Progression/GaragePort.cs`).
- `[HIỆN TẠI]` Hướng dẫn: `GuidanceSlot` cho 5 loại hướng dẫn lần lượt giữ một chỗ trên màn hình (gợi ý phím, thẻ
  trạm, thẻ địa điểm, nhắc pin, dòng bước tiếp). Dòng radio (`RadioTicker`) nằm ngoài; các thành phần khác chỉ chờ
  khi nó đang hiện (`UI/Prompts/GuidanceSlot.cs`, `UI/UISystem.cs`). Ở gara chủ dự án thấy quá nhiều thứ cùng lúc
  (cột sáng xanh trên trời, lời nhắc "đào kỷ vật rồi kéo về nhà"…) và không biết bước tiếp theo.
- `[HIỆN TẠI]` Thang nâng bụng: kể cả khi nhà đã có điện, thang chỉ gọi 07 tới hàn cáp khi 07 đang kéo hoặc chở
  một kỷ vật ở gần nhà. Người chơi không thể đoán ra điều kiện ẩn này (`Gameplay/Lift/LiftStation.cs`,
  `LiftCycle.cs`).
- `[ĐỀ XUẤT]` `0.5.6`: đưa dòng radio vào `GuidanceSlot` để mỗi lúc chỉ có một yếu tố hướng dẫn; gara đi từng
  bước, mỗi bước một chỉ dẫn (dọn phế liệu → đỗ lên bệ → cắt thùng); bỏ điều kiện ẩn của thang: nhà có điện là
  thang gọi 07 tới hàn cáp, rồi đưa 07 vào trong tàu đổ bộ.

### P4. Đào bằng chùm sáng từ mắt trông giả; đầu 07 bị ghim; 07 nâng được đồ khi chưa nâng cấp
- `[HIỆN TẠI]` Lỗi đầu bị ghim đã sửa và đã lên `main` (commit `72f984c9` kèm test `3ab086d1`): ánh nhìn khi cắt
  hoặc đào được nhả ra sau cú cắt, khi nhặt đồ hoặc sau `1.5 s` theo dõi (`Rover/Character/RoverBodyLanguage.cs`).
- `[HIỆN TẠI]` Đào có từ đầu game: giữ `E` trong tầm `5 m`; thời gian đào là `2.5 s` cộng `0.06 s` cho mỗi kg của
  vật, bộ phận của bạn robot mất `2.4 s`. Hình ảnh là một phễu sáng từ mắt 07 hút vật bay lên và xoay
  (`Gameplay/Excavation/`, `ExcavationTuning.asset`, `FriendTuning.asset`).
- `[HIỆN TẠI]` Kéo: sau khi có vòng dây kéo ở gara, chuột phải móc dây vào vật. Dây phát ra từ mắt, và kỷ vật bay
  lơ lửng cao `0.8 m` khi bị kéo, dù 07 chưa có thiết bị nâng (`Gameplay/Tether/`, `TetherTuning.asset`).
- `[ĐỀ XUẤT]` `07 v2` (`0.5.7`): gầu xúc ở mũi trước là thiết bị có sẵn từ đầu, nên đào vẫn có từ phút 0; tời cáp
  ở đuôi kéo vật trượt trên mặt đất, không bay; chỉ khi có Giỏ chở đồ mới cẩu được vật lên lưng. Lý do: việc gì
  cũng do một thiết bị thật làm.

### P5. Thẻ ký ức khi bạn thức dậy đè lên màn hình; `Tab` có hai thẻ khác kích thước
- `[HIỆN TẠI]` Khi một người bạn thức dậy sau khi sửa, thẻ `MemoryCard` hiện "Nhật ký sửa chữa: <tên>" với lời
  của phi hành đoàn và đè lên HUD. Phím `Tab` có hai thẻ, "Khoang hàng" (lưới 4×2 ô) và "07" (chân dung 07 với các
  hốc lắp thiết bị), mỗi thẻ một kích thước.
- `[ĐỀ XUẤT]` `0.5.6` (nhánh `agy-ui2`, đang làm): `Tab` thành **một khung cố định** kiểu Subnautica, không có
  thẻ: lưới khoang hàng của 07 bên trái, chân dung 07 với các hốc lắp bên phải (chỉ lắp được khi đỗ ở xưởng). Khi
  đỗ ở nhà, màn chuyển đồ dùng **đúng khung đó**: khoang 07 bên trái, kho nhà bên phải. Bỏ thẻ ký ức lúc bạn thức
  dậy; lời phi hành đoàn sẽ được viết lại và đưa vào kho ghi chép (Notes) của bản `0.6`.

### P6. Cắt đã có lực nhưng mọi tia đều ra từ mắt; tia quét thô
- `[HIỆN TẠI]` Cắt có từ đầu game. Giữ nút để nạp 1–3 tầng tụ trên hai ống dẫn đối xứng dọc boong trước: mảnh
  nhỏ 1 tầng, vừa 2 tầng, lớn 3 tầng (tổng `1.07 / 1.52 / 1.97 s` ở bậc 0). Thấu kính hội tụ đưa đầu cắt lên bậc
  1: mỗi tầng `0.45 s → 0.36 s`, hao pin khi cắt `×0.85` (`Gameplay/Salvage/CutterTuning.cs`, `ChargePlan.cs`).
- `[HIỆN TẠI]` `IRoverRig` chỉ có hai điểm gốc (mắt và điểm chở đồ), nên tia cắt, tia hàn, tia quét, phễu đào và
  dây kéo đều xuất phát từ mắt (`Core/IRoverRig.cs`).
- `[HIỆN TẠI]` Chủ dự án thấy tia quét thô, sai màu và độ sáng, và không nối 07 với vật.
- `[ĐỀ XUẤT]` `0.5.6` (brief `agy-scan` đã sẵn, chưa bắt đầu): làm lại tia quét thành một chùm mảnh, dịu, nối liền
  mắt 07 với mảnh đang quét, màu theo bảng màu `docs/VISION.md`.
- `[ĐỀ XUẤT]` `0.5.7` (`07 v2`): đầu cắt và kim hàn ở mũi xe, dưới tầm mắt, vẫn nối với hai ống tụ nạp; mắt chỉ để
  nhìn, biểu cảm và quét.

---

## 3. Thiết kế Chi tiết Rover `07 v2` (Mọi Hành động Đều Có Thiết bị Vật lý Đúng Chức năng)

### 3.1 Nguyên tắc cốt lõi: Giữ trọn bản sắc của 07, tách bạch 7 điểm gốc thiết bị
- `[HIỆN TẠI]` 07 được dựng bởi
  [`RoverModelBuilder.cs`](../../Assets/_Project/Scripts/Art/Editor/RoverModelBuilder.cs): thân hộp sơn kem, mặt
  trước màu than, một sọc cam, 6 bánh trên hệ treo rocker-bogie, một mắt lớn trên cổ, cánh pin mặt trời trên lưng,
  ăng-ten sau bên phải. Quy tắc đối xứng trái-phải của 07 do chủ dự án đặt
  ([`0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md) §5.3).
- `[ĐỀ XUẤT]` Thiết kế `07 v2` giữ bản sắc của 07 (thân kem sọc cam, 6 bánh rocker-bogie, một mắt lớn biểu cảm), đồng thời mở rộng hợp đồng
  [`Assets/_Project/Scripts/Core/IRoverRig.cs`](../../Assets/_Project/Scripts/Core/IRoverRig.cs)
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

### 3.2 Mỗi việc của 07, hôm nay và trong `07 v2`

| # | Việc | Hôm nay `[HIỆN TẠI]` | `07 v2` `[ĐỀ XUẤT]` |
|---|---|---|---|
| 1 | Nhìn, biểu cảm, quét | Mắt trên cổ xoay; quét bằng giữ `E` (`Gameplay/Scanning/ScanField.cs`); Thấu kính hội tụ lắp vào hốc thấu kính. | Mắt chỉ để nhìn, biểu cảm và quét; tia quét mảnh, nối từ mắt tới vật (làm từ `0.5.6`). |
| 2 | Cắt | Có từ phút 0; tia phát ra từ mắt (`Gameplay/Salvage/SalvageField.cs`). | Đầu cắt trên khớp xoay ở mũi xe, dưới tầm mắt, nối với hai ống tụ nạp đối xứng. |
| 3 | Hàn (cáp thang, bạn robot, trạm) | Tia hàn từ mắt. | Kim hàn dưới đầu cắt. |
| 4 | Đào | Giữ `E`; phễu sáng từ mắt hút vật lên (`Gameplay/Excavation/ExcavationSystem.cs`). | Gầu xúc có răng ở mũi trước, **có sẵn từ đầu**; xúc và bẩy vài nhịp, bụi văng. |
| 5 | Chứa đồ | Khoang `8` ô, `+2` với Giỏ chở đồ, `+4` với Thùng hông (`CargoTuning.cs`); linh kiện tháp nằm trên ổ phẳng trên nóc; khi nạp phễu trạm, tia đi từ mắt. | `0.5.6`: linh kiện vào khoang qua nắp. `0.5.7`: nắp khoang thật với con lăn thu đồ vào và đẩy đồ vào phễu trạm. |
| 6 | Kéo | Chuột phải, sau khi có vòng dây kéo ở gara; dây từ mắt, dài `2.5–12 m`; kỷ vật bay cao `0.8 m` (`TetherTuning.asset`). | Tời cáp ở cản sau; vật **trượt trên đất**, để lại vệt bụi; kéo tốn pin `×1.25` như hôm nay. |
| 7 | Chở trên lưng | Giỏ chở đồ: chuột phải để kỷ vật lướt lên chỗ chở ở đuôi xe (`Gameplay/Cradle/CargoCradle.cs`). | Khung chữ A cẩu vật từ cản sau lên đệm trên lưng; chở nhanh và đi thẳng lên thang nâng. |
| 8 | Lắng nghe | Ăng-ten sau bên phải nhô lên, nháy hai lần; vòng sóng `140 m`; cột sáng `32 m`. | Vòng ăng-ten xoay chỉ hướng, đầu 07 ngoái nhìn, ánh nháy tại vật, tiếng phát từ chỗ vật; không cột sáng. |
| 9 | Pin, tăng tốc | Cánh pin mặt trời giữa lưng; thanh 8 đèn pin trên lưng; Bộ pin II gắn lệch một bên (`Art/Editor/RoverKitBuilder.cs`). | Hai hộp pin đối xứng hai bên cánh pin, đúng quy tắc đối xứng. |

---

## 4. Các hệ thống chính, bạn robot và bảng nâng cấp (0.5 → 1.0)

### 4.1 Quét và bản vẽ

- `[HIỆN TẠI]` Quét: giữ `E` nhìn vào một mảnh trong tầm `8 m`, trong góc `25°`. Mảnh nhỏ `1.5 s`, vừa `2.5 s`, lớn
  `4 s`; nhả ra giữa chừng vẫn giữ tiến độ. Quét xong, mảnh tan đi (`0.9 s`) và để lại viền bụi (`2 s`). Mảnh của
  một bản vẽ đã biết cho `+3` Kim loại. Khung góc quanh mảnh chưa quét hiện từ `15 m`; có Thấu kính hội tụ thì quét
  nhanh `×1.5` và khung góc hiện từ `25 m` (`Gameplay/Scanning/ScanTuning.cs`).
- `[HIỆN TẠI]` Hai thiết bị nhận ở Gara của Kenji, không cần bản vẽ: **vòng dây kéo** (kéo vật, dây `2.5–12 m`)
  và **máy phát băng** (phát các cuộn băng của Ro) (`GaragePort.cs`, `Gameplay/Equipment/EquipmentTable.cs`).
- `[HIỆN TẠI]` 7 bản vẽ, 18 mảnh, mỗi bộ nằm ở một nơi, chế tạo ở Xưởng xe của Kenji khi điện đã về tới xưởng
  ([`0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md) §1.3; chi phí trong `Gameplay/Editor/SalvageEconomy.cs`;
  Kim loại / Dây điện / Thấu kính):

| Bản vẽ | Mảnh và nơi tìm | Chi phí | Tác dụng |
|---|---|---|---|
| **Giỏ chở đồ** (Cargo Cradle) | 2, Gara của Kenji (khung giá đang làm dở) | 4 / 0 / 0 | Chở kỷ vật trên lưng, chạy đủ tốc độ; khoang `+2` ô. |
| **Đèn pha ấm** (Warm Headlamp) | 2, xe Mule trên đường hẻm núi | 1 / 0 / 3 | Soi đường và chỗ tối; chiếu vào cột trả lời thì cột ở lâu hơn. |
| **Bật nhảy** (Hover-Jump) | 3, xe nhảy thử của Kenji ở mép gần khe vực | 4 / 2 / 1 | Tích lực rồi nhảy, qua khe vực tới chỗ Bell; mỗi cú `0.05–0.25` ô pin. |
| **Bộ tăng tốc** (Boost Coils) | 3, robot xe trượt chở hàng ở giàn khoan | 1 / 2 / 1 | Tự tăng tốc khi giữ lái trên đường bằng; trống tụ `0.25` ô, nạp lại khi lăn xuống dốc. |
| **Bộ pin II** (Cell Pack II) | 3, rover 06 cạnh nhà trạm tiếp sóng cửa hẻm núi | 3 / 0 / 2 | Pin `4 → 6` ô (đường bằng `480 → 720 m`). |
| **Thấu kính hội tụ** (Focus Lens) | 3, buồng lái vệ tinh rơi Kestrel-3 | 0 / 1 / 3 | Quét nhanh `×1.5`, khung góc từ `25 m`, đầu cắt lên bậc 1. |
| **Thùng hông** (Side Bins) | 2, tàu đổ bộ bị rơi | 3 / 1 / 0 | Khoang `+4` ô (tối đa `14` ô khi có cả Giỏ chở đồ). |

- `[ĐỀ XUẤT]` Sau `0.5`, thêm bản vẽ mới theo `gdd-v3.md`, `0.6-world-regions.md` và
  [`0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md) §1.6 và §5.1, không đổi 7 bản vẽ của `0.5`:
  - **`0.6` (Biển Dài):** Gân cắt (*Cutter Ribs*, bậc cắt 2, mỗi tầng `0.29 s`), Gạt bụi (*Dust Wiper*) cho tấm
    pin, Lớp phủ mặt trời (*Solar Coat*) sạc nhanh hơn khi đi xa.
  - **`0.7` (Thềm Vành Đai):** Đầu cắt nhiệt (*Thermal Cutter*, bậc cắt 3) và Bộ chuyển sạc (*Charge Adapter*)
    để sạc trên đế lục giác của trạm Meridian.
  - **`0.8` (Hố tối và ống dung nham):** Đèn lạnh (*Cold Lamp*) và Bộ sưởi (*Heater*) giữ pin trong vùng lạnh.

### 4.2 Những người bạn robot

- **Tilly, drone khảo sát của Ines (`0.5`):**
  - `[HIỆN TẠI]` Một drone nhỏ bốn cánh quạt (`Gameplay/Friends/DroneBody.cs`, `friend.tilly.repair_log` trong
    `vi.json`). Cả 3 bộ phận nằm trong chiếc xe trượt khảo sát bị lật của cô: một kẹt sau chốt kẹp (07 cắt), một
    dưới thân xe nghiêng (07 đào), một trong bầu cảm biến (07 cắt) (`FriendField.cs`, `LayTheSled`).
  - `[HIỆN TẠI]` Đủ 3 bộ phận thì 07 hàn sửa (`3.5 s`); Tilly thức dậy và tặng 07 tấm pin mặt trời.
  - `[HIỆN TẠI]` Sau đó Tilly: chỉ cho 07 những chỗ chưa tìm trong `40 m`; cứ mỗi `40 s` bay ra nhặt một bó vật liệu
    07 để lại ngoài thực địa khi khoang đầy, mang về kệ nhà (`Gameplay/Economy/TillyFetch.cs`); lúc rảnh thì đậu
    trên bệ đáp của tàu đổ bộ ở nhà; khi 07 bò dự phòng quá `5 s`, bay tới dẫn 07 về trạm sạc và nâng trần tốc độ
    bò từ `35%` lên `55%` (`Gameplay/Battery/BatteryField.cs`).
- **Bell, robot radio của Ro (`0.5`):**
  - `[HIỆN TẠI]` Bell không phải drone bay: Ro lắp Bell từ một máy thu hỏng và bốn chân máy quay
    (`friend.bell.repair_log`, `Gameplay/Bell/RadioCabinetBody.cs`). Bell nằm ở cuối hẻm núi, bên kia khe vực
    (cần Bật nhảy).
  - `[HIỆN TẠI]` 3 bộ phận (núm dò đài, màng loa, đèn điện tử) mỗi cái nằm dưới một đống đá sạt nhỏ trong hốc vách
    hẻm núi; 07 đào lên (`FriendField.cs`, `LayThePockets`, commit `0752bad7`). Sửa xong (`4 s`), Bell đứng dậy và
    tự đi bộ về góc nhà.
  - `[HIỆN TẠI]` Ở nhà: 07 đỗ trước mặt số thì núm xoay qua 3 kênh (Lumen After Dark, Máy phát băng, Giờ yên tĩnh;
    sang Giờ yên tĩnh thì Bell xếp chân nghỉ). Bell dò tín hiệu để dẫn 07 tới những chỗ chưa tới
    (`Gameplay/Signals/BellSignals.cs`) và reo lên khi kệ có thêm kỷ vật mới.
- **Bạn mới sau này:**
  - `[ĐỀ XUẤT]` **Atlas (`0.7`, Thềm Vành Đai):** xe kéo hạng nặng của Lumen đỗ ở công trường nối cáp; sửa xong thì
    giúp kéo kiện hàng rất lớn và mở cầu qua khe nứt lớn.
  - `[ĐỀ XUẤT]` **Moss (`0.8`, Hố tối):** robot làm vườn của Theo; sửa xong thì chăm mảng rêu sống dưới đèn hẹn giờ
    trong hầm trú bão ([`story-v3.md`](story-v3.md) §2).

### 4.3 Pin và năng lượng

- `[HIỆN TẠI]` Pin chia thành ô, hiện trên thanh 8 đèn trên lưng 07: `4` ô lúc đầu, `6` ô với Bộ pin II, trần `8`
  ô (`Gameplay/Battery/BatteryTuning.cs`, `BatteryModel.cs`).
- `[HIỆN TẠI]` Tiêu hao:
  - Lái: `1` ô mỗi `120 m` đường bằng; leo dốc tới `×1.5` (ở dốc `15°`), xuống dốc `×0.5`, đang kéo `×1.25`.
  - Tăng tốc: `×2`; phần thêm trừ trước vào trống tụ `0.25` ô của Bộ tăng tốc, trống nạp lại `0.01` ô cho mỗi mét
    lăn xuống dốc đủ nhanh.
  - Dùng tia (cắt, đào, hàn, nạp phễu): `1` ô mỗi `60 s`; cắt có Thấu kính hội tụ `×0.85`.
  - Bật nhảy: `0.05–0.25` ô mỗi cú.
- `[HIỆN TẠI]` Sạc:
  - Đế sạc ở nhà: `4 s` mỗi ô, kèm lưu game.
  - Vòng sạc ở trạm tiếp sóng đã thắp: `5 s` mỗi ô sau khi đỗ yên.
  - Tấm pin mặt trời của Tilly: khi 07 đứng yên ngoài nắng, sạc nhỏ giọt `180 s` mỗi ô, tối đa `1` ô.
- `[HIỆN TẠI]` Cảnh báo: "pin yếu" khi dưới `30%`, hoặc khi quãng đường còn lại không đủ về trạm sạc gần nhất (có
  hệ số an toàn); "rất yếu" khi dưới `15%`.
- `[HIỆN TẠI]` 07 không bao giờ "chết": ở `0` ô, 07 bò dự phòng với tốc độ tối đa `35%`, đèn pha `35%`, tắt tăng
  tốc và bật nhảy; việc dùng tia mới phải chờ, chỉ việc đang làm dở được làm xong. Tilly dẫn đường thì bò lên
  `55%`. Hết bò dự phòng khi tới trạm sạc hoặc hồi đủ `0.25` ô (`BatteryField.cs`, `BeamWork.cs`).
- `[ĐỀ XUẤT]` `0.6` (Biển Dài), quyết định D3: Mặt Trời đi chậm sát chân trời cực Nam (khoảng `2` giờ chơi một
  vòng). Khi bóng râm quét qua, các đế sạc mặt trời không có ắc-quy tạm nghỉ tới khi nắng quay lại, cho người
  chơi lý do nối cáp từ tháp hoặc mang thêm pin ([`story-v3.md`](story-v3.md) §3).

---

## 5. Phong cách nghệ thuật: chốt một lần trước `07 v2`

- `[HIỆN TẠI]` Thế giới, nhà và 07 là mesh đa giác thấp sinh bằng code, tô màu theo đỉnh, đổ bóng phẳng
  (`Assets/_Project/Scripts/Art/Editor/`). Ánh sáng đã chốt ngày
  2026-10-13: ban ngày "C · Nắng thấp", ban đêm "A · Xám trăng" (đêm A đã vào `0.5.5`).
- `[HIỆN TẠI]` Đã có hai bộ tài liệu để chọn: khối dựng Blender của 3 hướng (nhánh `agy-style`) và tranh
  concept cùng bố cục cho 3 hướng (nhánh `agy-concept`). Bộ thứ ba, "mức mịn" V0–V3 chụp trong chính game, đang
  được làm (nhánh `agy-look`).
- `[ĐỀ XUẤT]` Chủ dự án chọn trên **một trang bảng phong cách** gồm cả ba bộ, theo hai trục:

| Trục | Lựa chọn | Nghĩa là |
|---|---|---|
| Hình khối | **S1 · Đồ chơi mềm** | Khối tròn to, bo nhiều, rất ít chi tiết nhỏ, đổ bóng mịn, tỉ lệ dễ thương hơn. |
| | **S2 · Low-poly gọn** | Mặt vát phẳng, cạnh vát nhỏ sắc, chi tiết vừa phải, gọn như đồ chơi. |
| | **S3 · Phần cứng Mặt Trăng cổ** | Khung ống, hộp bọc giấy bạc, bánh lưới, bản lề lộ (như xe Apollo, Lunokhod), vẫn low-poly và màu ấm. |
| Độ mịn (chụp trong game) | **V0** | Như hiện tại. |
| | **V1** | Làm mịn pháp tuyến theo góc trên khối tròn (thân 07, bánh xe, đá tròn). |
| | **V2** | V1 cộng bóng tiếp xúc (AO) để vật có chiều sâu. |
| | **V3** | V2 cộng địa hình gần dày hơn và hạt bụi rất nhẹ. |

- `[ĐỀ XUẤT]` Hai worker đều khuyên **S2**, bên concept khuyên thêm vài chi tiết của S3 (bản lề, khung ống)
  cho thiết bị của 07. Director sẽ chỉ nêu khuyến nghị cuối cùng khi bảng có đủ ảnh V0–V3 trong game, vì độ
  mịn quyết định cảm giác "mềm" mà chủ dự án hỏi.
- `[ĐỀ XUẤT]` Sau khi chốt: ghi phong cách vào `docs/VISION.md` (quy tắc hình khối, độ vát, mật độ chi tiết,
  độ mịn), rồi mới dựng `07 v2` và làm lại tài sản theo đó trong `0.5.7`.

---

## 6. Âm thanh: hướng C "Radio cũ" và lớp tiếng cơ khí

- `[HIỆN TẠI]` Âm thanh được tổng hợp sẵn bằng Python (`tools/audio/cues.py`, `tools/audio/feel_cues.py`,
  dựng bằng `tools/audio/build_sfx.py`) thành file WAV trong game. Chủ dự án thấy tiếng thông báo và tiếng 07
  còn điện tử, chưa hợp không khí và thiếu cảm giác vật lý ([`0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md)).
- `[HIỆN TẠI]` Vòng nghe thử 1 (2026-10-14): chủ dự án chọn **C "Radio cũ"** nhưng chưa thật ưng. Vòng 2 có
  3 bản tinh chỉnh của C trên cùng trang nghe thử: **C1 Máy thu ấm** (bớt xì, dày và trầm hơn), **C2 Bàn
  radio** (núm vặn, công tắc bakelite, relay, màng loa thật; 07 như sinh vật nhỏ "ư, ồ"), **C3 Radio ru ngủ**
  (vài nốt hộp nhạc/celesta qua radio, êm nhất). Chờ chủ dự án nghe và chọn.
- `[ĐỀ XUẤT]` Áp dụng **bản C mà chủ dự án chọn ở vòng 2**, cộng một lớp tiếng cơ khí cho từng thiết bị, trong
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
> tại Mục 9** (nguồn: [`docs/design/story-v3.md`](story-v3.md)).
> Các khuyến nghị của nhóm thiết kế chỉ được đặt tại Mục 9.

### 7.1 Những gì đang có trong game hôm nay (`[HIỆN TẠI]`) so với Bản nháp `Story v3` (`[ĐỀ XUẤT]`)

- **Quy trình Khôi phục Lumen (`Lumen Recovery Protocol`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](../../Assets/_Project/Data/Localization/vi.json)
    (dòng 238–256), bảng tiến trình trong Phòng thí nghiệm hiện có 4 bước:
    1. `recovery.power.name`: *Cấp điện cho trạm*.
    2. `recovery.link.name`: *Nối mạng lưới tới Đỉnh núi*.
    3. `recovery.dish.name`: *Sửa chảo ăng-ten lớn*.
    4. `recovery.answer.name`: *Trả lời Trái Đất*.
  - `[ĐỀ XUẤT]` Bản nháp `Story v3` ([`docs/design/story-v3.md`](story-v3.md) §1)
    bổ sung động lực xuyên suốt từ `0.5` đến `1.0`: năm 2047, Trạm Lumen (gồm 4 thành viên: trưởng nhóm địa
    chất **Ines Kalani**, kỹ sư cơ khí **Kenji**, nhà thực vật **Theo**, phát thanh viên **Ro Amadi**) phải
    sơ tán khẩn cấp trong bão Mặt Trời; chương trình trên Trái Đất bị đóng cửa với kết luận *"không đáng để
    quay lại"*, nhưng trước khi rời đi Ines đã tìm thấy bằng chứng về một túi nước băng lớn dưới lòng đất
    (cùng dữ liệu dò sóng khớp từ trạm tự động **Meridian** bên kia vành núi). Cơn bão làm hỏng chảo ăng-ten
    lớn và làm rơi vệ tinh chuyển tiếp **Kestrel-3** trước khi dữ liệu kịp gửi về, nên 07 thực thi quy trình
    khôi phục để thu thập **6 mảnh bằng chứng khảo sát (`Survey pieces`)** và truyền về Trái Đất.
- **Kỷ vật (`Keepsakes`) và Máy phát băng của Ro (`TapeDeck`):**
  - `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](../../Assets/_Project/Data/Localization/vi.json)
    (dòng 542–588), bảng chuỗi chữ hiện có 6 mục `relic.*`: `relic.cassette_player` (*Cuộn băng của Ro*),
    `relic.rubber_duck` (*Cố vấn gỡ lỗi cấp cao*), `relic.golden_record` (*Đĩa vàng Lumen*),
    `relic.astronaut_boot` (*Đôi ủng của Ines*), `relic.teapot` (*Ấm trà thật sự đầu tiên*), và
    `relic.garden_gnome` (*Trưởng ban An ninh*); đồng thời tại Gara của Kenji đã có thiết bị `TapeDeck`
    trong thùng `"07 · SPARE"`
    ([`Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs`](../../Assets/_Project/Scripts/Gameplay/Progression/GaragePort.cs)).
  - `[ĐỀ XUẤT]` Theo quyết định trong `Story v3` ([`docs/design/story-v3.md`](story-v3.md) §2),
    Kệ kỷ vật giữ **5 kỷ vật cá nhân** (vịt cao su, thần lùn giữ vườn, ấm trà, đĩa vàng, đôi ủng của Ines),
    viên sỏi xanh thuộc về mạch bằng chứng khảo sát của Ines, còn máy phát băng của Ro là **linh kiện vận
    hành trong thùng `"07 · SPARE"`** (các khóa `relic.cassette_player.*` sẽ bỏ khi áp dụng).
- **Các rover đàn anh (`01–06`) và sinh thái máy móc hai trạm (`Lumen` & `Meridian`):**
  - `[HIỆN TẠI]` Trong bản `0.5` hôm nay, đàn anh **`06`** nằm nghỉ cạnh nhà trạm tiếp sóng cửa hẻm núi
    (`relay.2`) và giữ 3 mảnh bản vẽ Bộ pin II
    ([`docs/design/0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md) §1.3 & §1.7).
  - `[ĐỀ XUẤT]` Trong bản nháp `Story v3` ([`docs/design/story-v3.md`](story-v3.md) §2 & §3),
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
  - `[ĐỀ XUẤT]` Trong bản nháp `Story v3` ([`docs/design/story-v3.md`](story-v3.md) §1),
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
| **1. Lòng chảo & Hẻm núi (`0.5`)** | `[HIỆN TẠI]` Địa hình, nhà, gara, giàn khoan, Kestrel-3, khe vực, Tilly & Bell đã có trong game ([`0.5.5-owner-feedback.md`](0.5.5-owner-feedback.md) §1.7). `[ĐỀ XUẤT]` Nội dung chữ `Story v3`. | Đèn báo lỗi đỏ trên Đỉnh núi; ánh đèn của Bell bên kia khe vực. | Vệ tinh **Kestrel-3** mang phù hiệu lạ `"TYCHO CONSORTIUM · MERIDIAN RELAY"`; nhật ký lõi khoan `"bất thường"` của Ines tại giàn khoan; nhật ký chuyển tiếp của `06`. | `[ĐỀ XUẤT]` **Mảnh 1:** Dữ liệu lõi khoan của Ines (bộ ghi tại giàn khoan). | **`06`** (`[HIỆN TẠI]` ở nhà trạm `relay.2` cửa hẻm núi). |
| **2. Biển Dài — *The Long Mare* (`0.6`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.6` ([`docs/design/story-v3.md`](story-v3.md) §2). | Vệt xoáy từ trường sáng và đèn chớp hổ phách của chảo lưu trữ. | Kho lưu trữ 20 năm thư từ của nhóm (dừng năm 2067 do hỏng máy quét bụi); bầy drone khảo sát Meridian (`survey flock`); máy đào rãnh cáp liên trạm của Lumen (`trencher`). | `[ĐỀ XUẤT]` **Mảnh 2:** Dữ liệu dò sóng của bầy drone; **Mảnh 3:** Kho lưu trữ chảo liên lạc. | `[ĐỀ XUẤT]` **`05`** (dừng nghỉ cạnh chảo liên lạc). |
| **3. Thềm Vành Đai — *Rim Terraces* (`0.7`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.7` ([`docs/design/story-v3.md`](story-v3.md) §2). | Các bậc đá sáng và đèn chớp trắng của **Trạm Meridian** nhìn thấy từ bờ bắc Biển Dài. | **Trạm Meridian** vẫn hoạt động tự động dưới nắng: máy phân loại mẫu, máy gạt bụi, robot leo cáp; xe kéo **Atlas** của Lumen đỗ tại công trường nối cáp. | `[ĐỀ XUẤT]` **Mảnh 4:** Kho dữ liệu **Meridian's vault** (bản đồ toàn bộ túi nước băng). | `[ĐỀ XUẤT]` **`04`** (nghiêng trên vách thềm đá). |
| **4. Hố Đen Vĩnh Cửu — *Shadowed Crater* (`0.8`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.8` ([`docs/design/story-v3.md`](story-v3.md) §2). | Lòng chảo tối đen có ánh xanh lục ở hướng tây nam Biển Dài. | Các máy dò băng của Meridian (`ice tappers`) nằm chờ nối lại cáp và tín hiệu; robot làm vườn **Moss** của Theo đang lấy mẫu băng. | `[ĐỀ XUẤT]` **Mảnh 5:** Mẫu lõi băng thực chứng (`ice core cache`). | `[ĐỀ XUẤT]` **`03`** (dừng vì hết pin trong bóng tối). |
| **5. Ống Dung Nham — *The Lava Tube* (`0.8`)** | `[ĐỀ XUẤT]` Mở rộng thế giới `0.8` ([`docs/design/story-v3.md`](story-v3.md) §2). | Ánh sáng hắt lên từ giếng trời tự nhiên (`skylight`). | Hầm trú bão kiêm máy chủ sao lưu chạy bằng pin đồng vị kín; một bóng đèn hẹn giờ vẫn bật nuôi **mảng rêu xanh còn sống** của Theo. | `[ĐỀ XUẤT]` **Mảnh 6:** Bản sao lưu phân tích tổng hợp của Ines (`shelter backup`). | `[ĐỀ XUẤT]` **`02`** (không leo ra khỏi dốc ống được). |
| **6. Đường lên Đỉnh Núi — *The Peak Road* (`1.0`)** | `[ĐỀ XUẤT]` Kết thúc hành trình `1.0` ([`docs/design/story-v3.md`](story-v3.md) §2). | Đèn báo lỗi đỏ trên đỉnh chảo ăng-ten lớn. | Đống đá sạt từng chặn đường sửa chảo; trạm chảo ăng-ten lớn trên đỉnh vẫn có điện từ cột pin mặt trời cao. | `[ĐỀ XUẤT]` Truyền toàn bộ kho lưu trữ và 6 mảnh khảo sát về Trái Đất. | `[ĐỀ XUẤT]` **`01`** (dừng trước đống đá sạt). |

### 7.3 Kiểm toán Lời thoại: Không viết lời tâm sự mùi mẫn từ người gửi cho máy (`[ĐỀ XUẤT]`)

- `[HIỆN TẠI]` Trong [`Assets/_Project/Data/Localization/vi.json`](../../Assets/_Project/Data/Localization/vi.json),
  hầu hết thư trên cột tiếp sóng (`letter.mast_1..4, 6..8, 10`) và ghi chú sửa chữa (`friend.bell.repair_log`,
  `friend.tilly.repair_log`, `crate.garage.note`) đã viết theo đúng giọng con người nói với con người hoặc
  ghi chú kỹ thuật hóm hỉnh về công cụ, nhưng vẫn còn **14 khóa chữ** đang để con người tâm sự trực tiếp với
  07 như một người bạn tri kỷ.
- `[ĐỀ XUẤT]` Theo quy tắc của chủ dự án (*"con người coi robot là công cụ; cá tính của 07 thể hiện qua hành
  vi, không qua lời người khác tâm sự với nó"*), bản nháp `Story v3`
  ([`docs/design/story-v3.md`](story-v3.md) §4.1)
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
| **1** | **Bỏ khóa cứng đầu của 07:** Đầu 07 chỉ ngoái nhìn tự nhiên rồi trả về hướng lái, không bị ghim cứng vào vật thể ở xa. | `[HIỆN TẠI]` **Đã xong, đã lên `main`** (commit `72f984c9` + test `3ab086d1`, [`RoverBodyLanguage.cs`](../../Assets/_Project/Scripts/Rover/Character/RoverBodyLanguage.cs)). |
| **1b** | **Tia laser đôi khi chiếu về tận nhà:** nhiều khả năng cùng gốc với lỗi đầu bị ghim (ánh nhìn khi cắt không được nhả). | `[ĐỀ XUẤT]` Chủ dự án thử lại trong bản `0.5.6`; nếu còn, tìm và sửa riêng. |
| **2** | **3 bộ phận của Bell nằm dưới đống đá sạt (`RockfallPocket`):** Đặt 3 bộ phận của Bell dưới 3 đống đá sạt trong hốc hẻm núi để 07 đào lên. | `[HIỆN TẠI]` **Đã xong, đã lên `main`** (commit `0752bad7` trong [`FriendField.cs`](../../Assets/_Project/Scripts/Gameplay/Friends/FriendField.cs) `LayThePockets`). |
| **3** | **`Tab` một khung cố định & bỏ thẻ ký ức lúc bạn thức dậy:** khoang 07 bên trái, chân dung 07 với hốc lắp bên phải; màn chuyển đồ ở nhà dùng cùng khung (khoang 07 trái, kho nhà phải). | `[ĐỀ XUẤT]` **Đang làm** trên nhánh `agy-ui2`. |
| **4** | **Vòm trong Xưởng xe của Kenji bị sọc răng cưa:** tìm đúng nguyên nhân (mặt trùng nhau, pháp tuyến ngược hoặc bóng đổ) và sửa hình học. | `[ĐỀ XUẤT]` **Đang làm** trên nhánh `agy-art`. |
| **5** | **Đưa linh kiện cốt truyện (`TowerCoil`, `Transformer`, `RelaySpare`) vào trong khoang hàng qua `CargoHatch`:** Khi nhặt linh kiện tháp/trạm, nắp `CargoHatch` mở ra đón linh kiện vào trong bụng xe, xóa việc đặt cuộn dây lơ lửng trên nóc phẳng `StoryPartSocket`. | `[ĐỀ XUẤT]` Brief `agy-hold` đã sẵn; bắt đầu ngay sau khi `agy-ui2` lên `main` (cùng đụng tới khoang hàng). |
| **6** | **Tia quét bản vẽ (`ScanFan`) nối từ mắt 07 tới vật thể:** Hiển thị chùm sáng quét quang học nối từ thấu kính mắt của 07 tới mảnh bản vẽ đang quét (giữ nguyên thông số `1.5 s / 2.5 s / 4.0 s`, tầm `8 m` trong `ScanTuning.cs`). | `[ĐỀ XUẤT]` Brief `agy-scan` đã sẵn, chưa bắt đầu. |
| **7** | **Gỡ bỏ điều kiện ẩn của thang nâng bụng (`LiftStation.cs`):** Bỏ điều kiện ẩn "đang kéo hoặc chở kỷ vật gần nhà" trong [`LiftStation.cs`](../../Assets/_Project/Scripts/Gameplay/Lift/LiftStation.cs) để ngay khi nhà có điện, thang nâng bụng gọi 07 tới hàn cáp, rồi đưa 07 vào trong tàu đổ bộ. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để người chơi không bị kẹt trước thang nâng khi về nhà tay không. |
| **8** | **Làm rõ nhịp hướng dẫn tại Gara của Kenji & Chống chồng chéo chữ (`GuidanceSlot`, `PromptDirector`):** Đưa ticker vào chung bộ phân xử `GuidanceSlot` để chỉ hiện đúng 1 dòng hướng dẫn tại một thời điểm, và dẫn dắt tuần tự từng bước tại `GaragePort` (dọn phế liệu mở cổng → đỗ lắp `TetherRing` → cắt thùng `"07 · SPARE"` lấy `TapeDeck`). | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để giải quyết triệt để phản hồi P3 của chủ dự án. |
| **9** | **Phản hồi lắng nghe (`SonarSystem`, `SiteMarker`) không dùng cột sáng trên trời:** Xóa cột sáng `32 m` trong `SiteMarker.cs` và vòng sóng dưới đất, thay bằng ánh lấp lánh tại vật thể + đầu 07 ngoái nhìn + âm thanh 3D định hướng. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để giải quyết phản hồi P2 của chủ dự án. |
| **10** | **Rà soát và sửa câu chữ tiếng Việt (`text-audit-0.5.5.md`):** Sửa các chuỗi hướng dẫn, gợi ý và thông báo chưa tự nhiên trong `vi.json`. | `[ĐỀ XUẤT]` Thực hiện trong `0.5.6` để đồng bộ với nhịp hướng dẫn mới. |

### 8.2 Bản `0.5.7` — Áp dụng Khóa Phong cách Nghệ thuật + Thiết kế `07 v2` + Âm thanh C Tinh chỉnh + Tiến trình Kéo/Nâng

1. `[ĐỀ XUẤT]` **Áp dụng phong cách nghệ thuật đã khóa (Art Style Lock):**
   - Áp dụng phong cách và mức mịn chủ dự án chọn trên bảng phong cách (Mục 5, quyết định D1) cho địa hình,
     đá, nhà, trạm, xác tàu và 07.
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
4. `[ĐỀ XUẤT]` **Áp dụng bản âm thanh chủ dự án chọn ở vòng 2 (quyết định D4):**
   - Thay toàn bộ tiếng giao diện và tiếng 07 bằng bản đã chọn; thêm lớp tiếng cơ khí cho gầu xúc, đầu
     cắt/hàn, tời cáp, khung cẩu, nắp khoang và tiếng trả lời khi lắng nghe (phát từ đúng chỗ vật thể).

### 8.3 Các bản `0.6 → 1.0` — Mở rộng Thế giới Sống & Hoàn tất Hành trình

| Phiên bản | Vùng mở rộng & Cơ chế chính (`[ĐỀ XUẤT]`) | Trang bị, Bạn đồng hành & Mảnh khảo sát mới (`[ĐỀ XUẤT]`) |
|---|---|---|
| **`0.6`** | **Biển Dài (*The Long Mare*):** Đồng bằng tối ở phía tây lòng chảo (`0.6-world-regions.md`); thêm chu kỳ Mặt Trời `L1` (~2 giờ/chu kỳ) nếu chủ dự án duyệt; hệ thống bụi phủ tấm pin và máy đào rãnh cáp liên trạm (`trencher`). | Bản vẽ: `Cutter Ribs` (bậc cắt 2: `0.29 s`/tầng), `Dust Wiper`, `Solar Coat`. Sinh thái: bầy drone khảo sát Meridian (`survey flock`), đàn anh `05`. Khảo sát: **Mảnh 2** (dữ liệu bầy drone) & **Mảnh 3** (kho lưu trữ chảo 20 năm). |
| **`0.7`** | **Thềm Vành Đai (*Rim Terraces* — Trạm Meridian):** Các bậc thềm đá ngập nắng ở bờ bắc Biển Dài; trạm tự động Meridian vẫn chạy bộ điều phối nội bộ; cơ chế nối cầu nguồn `Power Bridge`. | Bản vẽ: `Thermal Cutter` (bậc cắt 3: `0.22 s`/tầng), `Charge Adapter` (sạc trên đế lục giác Meridian). Bạn mới: **Atlas** (xe kéo hạng nặng của Lumen). Đàn anh `04`. Khảo sát: **Mảnh 4** (`Meridian's vault`). |
| **`0.8`** | **Hố Đen Vĩnh Cửu & Ống Dung Nham (*Shadowed Crater & Lava Tube*):** Vùng tối vĩnh cửu không có nắng mặt trời; hầm trú bão ngầm chạy bằng pin đồng vị kín với đèn hẹn giờ nuôi rêu xanh của Theo. | Bản vẽ: `Cold Lamp`, `Heater`. Bạn mới: **Moss** (robot làm vườn của Theo). Đàn anh `03` và `02`. Khảo sát: **Mảnh 5** (`ice core cache`) & **Mảnh 6** (`shelter backup`). |
| **`1.0`** | **Đường lên Đỉnh Núi (*The Peak Road* & *Great Dish*):** Vượt qua đống đá sạt nơi đàn anh `01` dừng lại, sửa chảo ăng-ten lớn trên đỉnh và truyền toàn bộ kho lưu trữ kèm 6 mảnh bằng chứng khảo sát về Trái Đất. | Hoàn tất bước 3 & 4 của Quy trình Khôi phục Lumen (`Dish` → `Answer`); nhận tín hiệu trả lời từ Ro Amadi và Houston, mở khóa chế độ tự do khám phá sau cái kết. |

---

## 9. Các Quyết định Cần Chủ dự án Phê duyệt (Owner Decisions & Recommendations)

Để đội ngũ khóa bản `0.5.6`, `0.5.7` và chốt chính thức bản nháp `Story v3`, xin chủ dự án cho ý kiến về
**4 quyết định sản phẩm (`D1–D4`)** và **7 câu hỏi cốt truyện (`S1–S7`)** dưới đây (kèm khuyến nghị của nhóm
thiết kế):

### 9.1 Quyết định Sản phẩm, Nghệ thuật, Thiết kế `07 v2`, Chu kỳ Mặt Trời & Âm thanh (`D1–D4`)

1. **D1 — Chọn Phong cách Nghệ thuật cho bản `0.5.7` (Mục 5):**
   - *Câu hỏi:* Trên trang bảng phong cách, chọn một hướng hình khối (**S1** Đồ chơi mềm, **S2** Low-poly gọn,
     **S3** Phần cứng Mặt Trăng cổ) và một mức mịn (**V0–V3**).
   - *Khuyến nghị:* Sơ bộ **S2**, thêm vài chi tiết S3 cho thiết bị của 07; mức mịn chốt khi có ảnh V0–V3
     chụp trong game. Lý do: S2 đọc rõ ở xa, hợp với code sinh mesh hiện có, và chi tiết S3 làm thiết bị
     của 07 trông "có chức năng thật".
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

4. **D4 — Âm thanh (Mục 6):**
   - *Câu hỏi:* Trên trang nghe thử vòng 2, chọn C, C1, C2 hay C3 (có thể trộn: ví dụ giao diện một bản, tiếng
     07 một bản).
   - *Khuyến nghị:* Chủ dự án tự nghe; Director không chọn thay vì đây là gu nghe.

### 9.2 7 Câu hỏi Chốt Bản nháp `Story v3` (`S1–S7` từ `docs/design/story-v3.md` §6)

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
   - *Câu hỏi:* Chủ dự án có duyệt áp dụng 14 bản viết lại trong `docs/design/story-v3.md` §4.1 vào
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
