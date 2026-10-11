# Lofi Lunar: Thiết kế game v3 (bản đã duyệt)

> **Đã duyệt ngày 2026-10-11.** Bản tiếng Việt của `docs/design/gdd-v3.md`, cùng cấu trúc 11 phần, theo các câu trả lời
> của anh/chị và bản nhận xét của phiên thiết kế game. Hai quyết định nền móng của anh/chị:
> - **phát triển là trên hết:** nâng cấp 07, mở khoá và chế tạo công nghệ, đi ngày càng xa;
> - **pin là nhiên liệu:** 07 phải về sạc, và nâng cấp giúp nó đi xa dần.
>
> Không khí êm đềm vẫn giữ nguyên.

## 1. Lời hứa và các trụ cột
**Lời hứa:** một chiếc rover nhỏ, cũ và mệt, thức dậy trên một mặt trăng tối. Nó tự xây lại chính mình và trạm của
mình từng chút một, mỗi đêm đẩy tầm pin đi xa thêm một đoạn, cho đến khi gọi được về Trái Đất. Và người chơi không bao
giờ thấy căng thẳng.

**Trụ cột:**
1. **Lớn lên và đi xa hơn:** chuyến đi nào cũng làm 07 hoặc căn cứ mạnh hơn.
2. **Êm đềm:** hết pin không phải là thất bại.
3. **Tự do khám phá:** chỉ vài mục tiêu chính có đánh dấu tiến độ, còn lại đi đâu, làm gì trước cũng được.
4. **Ấm áp giữa lạnh lẽo:** trạm sạc, nhà và bạn bè là những ánh đèn ấm.
5. **Chạm đâu cũng thành nhạc, món nào cũng nhìn rõ:** đồ lắp lên 07 và công trình đều nhìn thấy từ 30 m.
6. **Cô đơn mà bình yên.**

## 2. Vòng lặp chính
- **Vài giây:** thấy ánh sáng → dò sóng → lái tới → dùng tia làm việc → nhặt được đồ. Đèn pin bên hông 07 tắt bớt dần.
- **Một chuyến (5–15 phút):** xem tầm pin và trạm sạc trên bản đồ → ra ngoài → trục vớt vật liệu cùng "món chính" của
  xác tàu (bản vẽ hoặc linh kiện) → ghé cột sạc → về nhà, giải mã ở phòng lab, chế tạo ở trạm sửa xe → buổi tối ở đế
  sạc: pin đầy, một ánh sáng mới ở chân trời.
- **Một buổi chơi:** 2–3 chuyến, luôn có một bước tiến lớn: món đồ mới, pin lớn hơn, một trạm sạc mới, một công trình
  mới hoặc một cấp tháp radio.
- **Cả game:** bốn mục tiêu chính, **Điện → Nối mạng → Sửa chảo → Trả lời Trái Đất**, đi theo đường nào là do người
  chơi chọn.

## 3. Pin và tầm đi
**Luật êm đềm:**
- **Không chết, không mất đồ.**
- **Việc đang làm luôn được làm xong:** đang cắt, đang khâu, đang kéo mà hết pin thì vẫn xong việc rồi mới chuyển sang
  bò chậm. Không đủ pin để nhảy thì 07 không tích lực, cuộn lò xo xèo nhẹ ở mép vực, không bao giờ rơi giữa chừng.
- **Không bao giờ mắc kẹt:** hết pin thì 07 bò chậm (35% tốc độ) về trạm sạc gần nhất bằng ô pin dự phòng của Kenji,
  ô pin không bao giờ cạn. Sau khoảng 5 giây, **Tilly tự bay ra, soi đèn dẫn đường**, và 07 đi nhanh hơn (55%). Không
  cần dây kéo.
- **Trạm sạc không bao giờ quá xa:** ở bản 0.5, mọi chỗ đều cách trạm sạc không quá 300 m; bò về lâu nhất khoảng 2
  phút, có Tilly thì chưa tới 1 phút rưỡi.
- **Đứng yên không tốn pin.** Đứng dưới nắng thì pin hồi chậm, nhưng **chỉ tới 1 ô**: đủ để về nhà, không bao giờ phải
  ngồi chờ.
- **Cảnh báo nhẹ và sớm:**
  - 30%: một tiếng nhạc nhỏ, và cột sạc gần nhất sáng lên ở chân trời. Lời nhắc chỉ nhắc tên những trạm đã thắp hoặc
    nhà.
  - 15%: 07 ngoái nhìn về phía trạm sạc.
  - Không có còi, không có màu đỏ.

**Con số:**
- một ô pin chạy được khoảng 120 m; khởi đầu có 4 ô;
- dùng tia khoảng 60 giây là hết một ô;
- kéo đồ chỉ tốn như lái xe thêm một phần tư;
- nhảy tốn một phần tư ô;
- dò sóng gần như miễn phí.

**Trạm sạc:**
- đế sạc ở nhà (đầy pin trong khoảng 4 giây);
- mọi cột tiếp sóng đã thắp, mỗi cột có một bệ sạc rộng 4 m;
- từ 0.6, các **cột sạc dã chiến** do người chơi tự xây.

**Nhìn thấy pin:**
- một dãy tối đa 8 đèn nhỏ màu hổ phách dọc thân 07;
- khi pin yếu, mắt 07 mờ đi và cánh pin rũ xuống;
- trên màn hình có một vòng nhỏ quanh la bàn, và vòng này tự ẩn khi 07 đang làm việc.

**Tầm đi:**
- 0.5: khoảng 140 m lúc đầu, rồi 220 m từ bất kỳ trạm sạc nào;
- 0.6: khoảng 370 m;
- 0.7: khoảng 500 m;
- 0.8: khoảng 650 m.

**Những lúc "cần thêm tầm" ở 0.5:**
- lúc đi tìm linh kiện cho Tilly: học được rằng đế sạc nạp pin;
- tới mép vực: học được rằng cột sạc nạp pin;
- xác vệ tinh Kestrel nằm ngoài tầm 4 ô pin: thắp cột gần đó, hoặc lắp bộ pin II.

## 4. Cây công nghệ, chế tạo và vật liệu
**Cách mở khoá một món:**
1. Tìm bản vẽ hoặc linh kiện trong xác tàu.
2. Đi thang nâng lên phòng lab. Máy đọc giải mã trong khoảng 4 giây, một ô sáng lên trên bảng công nghệ, và ô cửa sổ
   lớn của tàu có thêm một ngọn đèn.
3. Một luồng sáng chạy dọc dây cáp sang trạm sửa xe, máy vẽ bắt đầu vẽ bản thiết kế.
4. 07 nạp vật liệu, cánh tay máy lắp đồ lên.

**Vật liệu 3 bậc:**
- phế liệu: kim loại, dây điện, quang học;
- hợp kim, mạch điện, tinh thể;
- siêu dẫn.

Thêm vào đó là linh kiện có tên riêng, và máy chế tạo làm lại được những linh kiện đã từng tìm thấy.

**32 món nâng cấp trong 6 nhánh:**
- **Di chuyển:** nhảy xa, bánh từ tính leo vách, lướt;
- **Tia:** giỏ chở, kéo vật nặng, cắt vỏ kín, tời neo;
- **Cảm biến:** đèn pha, sonar rộng, đèn pha mạnh, ống ngắm sao;
- **Năng lượng & tầm đi:** trống tụ điện, bộ pin II và III, bánh tiết kiệm điện, cánh pin đôi, gói pin mở rộng, lõi
  năng lượng;
- **Bạn bè:** trinh sát cho Tilly, bộ chở hàng cho Atlas, hai drone em gái;
- **Trạm và công trình:** tháp radio 3 cấp, mạng cột tiếp sóng, cột sạc dã chiến, máy chế tạo, cánh mở rộng cho trạm
  sửa xe, giàn pin mặt trời, kho vật liệu, máy đọc II, chảo ăng-ten lớn.

**07 gọn gàng mà lột xác:** đồ lắp lên 07 chia vào 6 vị trí (mũi, nóc, lưng, hai bên hông, đuôi, bánh). Mỗi vị trí chỉ
hiện một món; món sau thay thế hoặc nhập vào món trước, ví dụ đèn pha mạnh thay giàn đèn cũ, các bộ pin gộp thành một
"xương sống pin" trên lưng. Cánh pin và cánh lướt gập lại khi chạy. 07 đi từ tàu thăm dò mệt mỏi thành rover thám hiểm
vững chãi, gương mặt một mắt không bao giờ đổi.

## 5. Khám phá, các vùng và thứ tự
**Mỗi vùng mở bằng một công nghệ:**
- hẻm Thì Thầm: nhảy xa;
- bậc thềm vành hố: bánh từ tính;
- hố tối: đèn pha mạnh;
- đường lên đỉnh núi: tời neo, cú lướt và Atlas dọn đá lở.

**Thứ tự do người chơi chọn:**
- Trong mỗi vùng, xác tàu, đồ quý, bạn bè và bí mật làm trước sau tuỳ ý.
- Sau hẻm núi, **đi bậc thềm hay hố tối trước đều được**: món mở hai vùng này chỉ cần đồ tìm ở lòng chảo và hẻm. (Các
  bản thử 0.6 và 0.7 ra từng vùng một; đến bản 1.0 thì tự do hoàn toàn.)
- Việc nối mạng tới đỉnh núi có hai đường, qua bậc thềm hoặc qua hố tối.
- Đỉnh núi là chỗ duy nhất cần cả hai vùng: Atlas ở bậc thềm, cú lướt ở hố tối.

**Vì sao muốn đi xa:**
- pin lớn dần;
- xác tàu càng xa càng quý;
- mỗi cột thắp và mỗi cột sạc tự xây biến chỗ xa thành gần;
- dụng cụ mới mở được cả những xác tàu cũ đã thấy từ đầu;
- ánh đèn đỏ trên đỉnh núi luôn trong tầm mắt.

## 6. Căn cứ và công trình
**Các trạm:**
- đế sạc: sạc đầy, lưu ngày chơi;
- tháp radio: cấp điện theo từng cấp;
- **tàu đổ bộ là phòng lab của phi hành đoàn,** lên bằng thang nâng dưới bụng tàu. Trong đó có:
  - máy đọc để giải mã;
  - bảng công nghệ kèm **bảng tiến độ các mục tiêu chính**;
  - bàn bản đồ, cho thấy tầm với, trạm sạc và các bệ xây;
  - một góc kỷ vật nhỏ;
- trạm sửa xe của Kenji: chế tạo và lắp đồ;
- cột tiếp sóng: trạm sạc, nhảy sóng, thư cũ;
- góc của Bell;
- mái vòm của Moss;
- chảo ăng-ten lớn.

**Người chơi tự xây công trình trên các bệ xây có sẵn** (không đặt tự do). Bệ là một nền đá có thùng đồ Kenji đóng gói
sẵn. 07 nạp vật liệu, khung tự bung ra, 07 khâu lại, rồi công trình sáng đèn.

| Công trình | Bản | Đặt ở đâu | Để làm gì |
|---|---|---|---|
| Cột sạc dã chiến | 0.6 | khoảng 2 bệ mỗi vùng | sạc pin ở chỗ không có cột tiếp sóng; nâng cấp lên II ở 0.7 thì sạc nhanh gấp đôi |
| Máy chế tạo | 0.6 | ở nhà, cạnh trạm sửa xe | làm lại linh kiện đã từng tìm thấy |
| Cánh mở rộng trạm sửa xe | 0.6 | cạnh trạm sửa xe | lắp được đồ bậc 2 (bánh xích, cánh pin…) |
| Giàn pin mặt trời | 0.7 | ở nhà và cạnh các cột sạc xa | cấp điện cho cột sạc nằm ngoài mạng lưới |
| Kho vật liệu | 0.7 | một bệ mỗi vùng giữa, một ở nhà | bỏ đồ nặng lại đó thay vì kéo về; Atlas chở về kho nhà |
| Nhà để drone | 0.8 | ở nhà | chỗ ở của hai drone em gái |
| Phòng sơn | 1.0 | ở nhà | sơn và dán hình cho 07 |

Cần cho mục tiêu chính chỉ có máy chế tạo và một cột sạc trên đường lên đỉnh núi; các công trình khác tuỳ ý.

## 7. Bạn bè
- **Tilly:** phát hiện đồ quanh 07, **tự bay ra soi đèn dẫn đường khi 07 hết pin**. Quà: ô pin mặt trời. Về sau bay
  trinh sát trước.
- **Bell:** tín hiệu của cô dẫn tới bản vẽ và linh kiện. Quà: sơn lại số 07.
- **Atlas:** nâng đá lở và chảo ăng-ten; chở đồ nặng từ các kho về nhà.
- **Moss:** trồng tinh thể.
- **Hai drone em gái:** tự gom phế liệu lặt vặt.

## 8. Câu chuyện
- **Lý do 07 thức dậy:** khi sơ tán vì bão, phi hành đoàn để lại cho 07 lời dặn "Cứ để radio bật" và một **quy trình
  khôi phục trạm**. Nhiều chục năm sau, tín hiệu yếu từ Trái Đất đánh thức nó. Trong đoạn mở đầu, mắt 07 hiện vài chữ
  khoảng 2 giây: "Phát hiện tín hiệu Trái Đất · Đường truyền tắt · Bắt đầu khôi phục".
- **Bốn mục tiêu chính** có đánh dấu tiến độ trên bảng trong phòng lab: cấp điện cho trạm, nối mạng tới đỉnh núi, sửa
  chảo ăng-ten, trả lời Trái Đất.
- **Mọi thứ khác đều tự do,** không có danh sách nào. Thế giới tự chỉ đường: ánh lấp lánh, cột sáng, tín hiệu của Bell,
  Tilly phát hiện.
- **Số phận phi hành đoàn là gia vị:**
  - các cột tiếp sóng giữ những tin nhắn cũ: phi hành đoàn về an toàn, chương trình bị huỷ;
  - rồi đến thư Ro viết suốt hai mươi năm;
  - kỷ vật và nhật ký nằm cạnh đồ công nghệ.

  Câu cuối của Kenji, "Đừng để ai cho cậu nghỉ hưu", chính là lý do 07 nâng cấp.
- **Kết thúc:** 07 lên đỉnh núi, Atlas nâng chảo, đường truyền mở ra. Một đốm sáng trên Trái Đất nháy ba ngắn một dài:
  "Mình nghe thấy rồi, 07. Mình vẫn còn đây. — Ro." Sau đó người chơi vẫn tự do khám phá, và thư của Ro vẫn đến.

## 9. Nhịp chơi theo phiên bản
- **Nguyên tắc:**
  - không quá 3 phút không có gì mới;
  - buổi nào cũng có bước tiến lớn;
  - bản nào cũng có động tác mới;
  - không cày cuốc;
  - không có chuyến vô ích.
- **0.5 (~100 phút), Điện:** pin, trạm sạc, chế độ bò chậm, bộ pin II và phòng lab; Tilly; mép vực và cột sạc đầu tiên;
  trạm sửa xe; nhảy qua vực; Bell; bản đồ; bốn cột sáng khắp lòng chảo. Chưa có công trình.
- **0.6 (~80 phút), Nối mạng, đường thứ nhất:** các bệ xây xuất hiện (máy chế tạo, cánh mở rộng trạm sửa xe, cột sạc
  đầu tiên); bánh từ tính; bậc thềm vành hố; Atlas; lá thư đầu tiên của Ro.
- **0.7 (~90 phút), đường thứ hai:** đèn pha mạnh; hố tối; Moss; hai cánh pin; cắt vỏ kín; giàn pin mặt trời; kho vật
  liệu.
- **0.8 (~60 phút), Sửa chảo và Trả lời:** cột cuối ở chân núi; tời neo; lõi năng lượng; đường lên đỉnh; phát sóng.
- **1.0:** tự do hoàn toàn thứ tự hai vùng giữa; đánh bóng, cài đặt, tay cầm, chụp ảnh, phòng sơn, sự kiện.
- **Tổng:** cốt truyện chính khoảng 5,5 giờ, chơi trọn khoảng 9–10 giờ.

## 10. Giữ / Đổi / Bỏ so với những gì đã làm
- **Giữ:**
  - các bước căn cứ thức dậy (điện về qua tháp, giờ là mục tiêu chính đầu tiên);
  - trạm sửa xe;
  - thang nâng dưới bụng tàu;
  - bản đồ (chuyển vào lab);
  - Tilly, Bell;
  - các món đã làm, giờ xếp vào 6 vị trí;
  - đoạn mở đầu, có thêm vài chữ khởi động;
  - ngắm sao.
- **Đổi:**
  - "bộ phận câu chuyện" thành linh kiện;
  - bản vẽ giải mã ở lab, và bản vẽ nhảy xa tìm ở mép vực (bỏ tờ vẽ sẵn của Kenji);
  - góc phi hành đoàn thành bốn khu lab;
  - đế sạc nạp pin;
  - cột tiếp sóng có bệ sạc 4 m;
  - sonar rộng sang 0.6.
- **Bỏ:**
  - kệ kỷ niệm ngoài trời, đài quan sát trên boong;
  - "ký ức là lý do để chơi";
  - quy trình theo một đường thẳng;
  - đặt nhà tự do.
- **Mới trong 0.5:** pin, trạm sạc, chế độ bò chậm có Tilly dẫn đường, đèn pin trên thân, bộ pin II, phòng lab, bảng
  mục tiêu chính.
- **Văn bản gốc cần sửa:**
  - bỏ "nhiên liệu" khỏi danh sách cấm;
  - thêm luật 15 "pin là tầm đi, không phải mối đe doạ";
  - luật 7 thêm "không bao giờ mắc kẹt vì hết pin";
  - phần tầm phủ sóng và phần xây dựng trong DESIGN.

## 11. Quyết định của anh/chị (2026-10-11)
1. Hết pin thì bò về, Tilly soi đèn dẫn đường: **đồng ý**.
2. Mắt 07 hiện vài chữ khởi động khoảng 2 giây: **đồng ý**.
3. Đứng dưới nắng hồi pin chậm, tối đa 1 ô: **đồng ý**.
4. Con số pin theo bản nhận xét (120 m mỗi ô, 4 ô lúc đầu, không quá 300 m tới trạm sạc): **đồng ý**.
5. Không đi theo một đường thẳng: **đã đổi**. Bốn mục tiêu chính có đánh dấu tiến độ, còn lại tự do.
6. Ro trả lời: **đồng ý**.
7. Được xây công trình trên bệ có sẵn: **đã đổi** (xem phần 6).
8. Độ dài: **đồng ý**.

## 12. Bổ sung từ Subnautica (chủ dự án, 2026-10-11)
Chủ dự án lấy Subnautica làm tham khảo. Xương sống của nó giống mình: tầm đi có giới hạn (ở đó là oxy, ở mình là
pin), nâng cấp để đi xa hơn, không minimap, định hướng bằng mốc lớn và radio, căn cứ là nhà thật sự. Chủ dự án chọn
bốn bài học. Chúng áp dụng từ 0.6; **phạm vi 0.5 giữ nguyên**.

| # | Bài học | Ở game mình | Bản |
|---|---|---|---|
| A1 | **Mảnh bản vẽ**: quét 2–4 mảnh rải trong một bãi mới học được bản vẽ, nên mỗi bãi được khám kỹ và cảm giác "sắp xong" kéo người chơi đi tiếp. | Từ 0.6, bản vẽ của một bộ đồ chia thành 2–4 mảnh trong một bãi (hoặc một vùng với bộ lớn). 07 quét từng mảnh bằng ống kính (động tác sonar và nhìn sẵn có, không thêm nút). Máy đọc trong lab hiện băng giấy dang dở (1/3, 2/3) và giải mã khi đủ bộ; ghim trên bảng hiện số mảnh. Ống bản vẽ đơn của 0.5 giữ nguyên. | 0.6 |
| A2 | **Một xác tàu lớn thấy từ đầu, quay lại nhiều lần theo công cụ mới** (như Aurora). | **Kestrel-3 thành "Aurora" của mình.** 0.5: vỏ ngoài và lõi Warm Headlamp (đã có). 0.6: một cửa kín hiện ra và có tên trên bản đồ, như lời hứa. 0.7: Thermal Cutter mở khoang kín (Circuitry, lõi dữ liệu cho Lab Reader II, một nhật ký phi hành đoàn). 0.8: lõi tàu giữ phần cuối của Power Cell, nên chuyến leo Đỉnh núi bắt đầu từ xác tàu đầu tiên người chơi từng thấy. | 0.6–0.8 |
| A3 | **Radio dẫn đường mềm**: bắt được tín hiệu là có một nơi để tới, không cần danh sách nhiệm vụ. | Khi radio, Bell hay một cột vừa thắp bắt được tín hiệu, bút vẽ của Kenji vẽ một vết chì lên bản đồ khảo sát (dấu chéo, vòng tròn, một dòng ghi chú nét chữ Ines), và cột sáng của nơi đó dâng lên một lần. Không mũi tên, không dấu trên HUD; đồ tầng 2 vẫn không được gọi tên (luật 16): vết chì là một nơi, không phải một việc. | 0.6 |
| A4 | **Cú ngoặt cảm xúc giữa game** (như tàu Sunbeam). | Giữa mục tiêu Link, mạng lưới mới sửa một nửa bắt được Trái Đất trong vài giây: tiếng rè, một đoạn nhạc hiệu đài của Ro, một giọng nói quá mờ để nghe rõ, rồi mất. Bảng phục hồi trong lab thắp đèn đầu của hàng Dish như một dấu hỏi. Buồn nhưng ấm (trụ cột 6): "sửa chảo" từ một việc thành một mong ước. Không đe doạ, không đếm giờ. | 0.7 |

**Đề xuất, chưa quyết** (backlog): mỗi vùng một bản sắc ánh sáng và âm thanh, bóng tối gợi kính sợ (bóng một xác
tàu khổng lồ hiện dần trong đèn pha, 0.7); Atlas kéo một trạm sạc di động như căn cứ di động (ý Cyclops, 0.7).

**Không học:** đói khát, quái vật, mất đồ khi thất bại, cày tài nguyên, xây tự do với giao diện rối.

## 13. Viết lại cốt truyện: "im lặng dài" (chủ dự án, 2026-10-11)
**Thay cho cái kết ở phần 8 và quyết định 6 ("Ro trả lời").** Ban đầu tưởng như trên Trái Đất con người vẫn còn,
nhưng dần dần người chơi nhận ra loài người đã diệt vong từ lâu. Mọi tín hiệu 07 bắt được là bản ghi âm cũ, trạm tự
động, hoặc máy móc nói chuyện với máy móc. Ở cuối game, sau khi người chơi chọn phát sóng, một tàu đổ bộ mới đến Mặt
Trăng, và lúc đó mới rõ: không có con người nào cả, chỉ có robot.

**Lựa chọn của chủ dự án:** kết tăm tối, bỏ ngỏ (đó là end game, các chi tiết đã được khám phá dần trên đường đi);
giọng Ro chỉ còn là bản ghi âm cuối; người bước xuống tàu mới là một rover trẻ hơn, **08**.

### 13.1 Luật cho sự tăm tối
- **Lối chơi vẫn êm** (luật 15, trụ cột 2). Tăm tối nằm ở những gì người chơi tự ghép lại, không bao giờ ở nguy hiểm,
  đếm giờ hay mất mát.
- **Không bao giờ cho thấy, không bao giờ giải thích.** Không nêu, không vẽ nguyên nhân: không đổ nát trên Trái Đất,
  không xác người, không cảnh thảm hoạ. Người chơi tự suy ra từ khoảng trống, ngày tháng, sự lặp lại.
- **Manh mối thuộc tầng 2** (luật 16): thư, nhật ký, kỷ vật, tín hiệu, tìm thấy khi đi khám phá, theo thứ tự nào cũng
  được. Bốn mục tiêu chính không đổi. Ai bỏ qua manh mối vẫn tới được cái kết; ai tìm đủ thì hiểu trước khi nó xảy ra.
- **Mỗi manh mối nhỏ và cụ thể:** một ngày tháng, một câu lặp lại, một giọng nói không bao giờ thở.

### 13.2 Hé lộ theo từng phiên bản
| Bản | Người chơi có thể nhận ra |
|---|---|
| 0.5 | Nhạc hiệu đánh thức 07 là một vòng lặp: đêm nào cũng y hệt. Dòng "EARTH SIGNAL · DETECTED" nói *tín hiệu*, chưa bao giờ nói *liên lạc*. Thư của Ro có ngày tháng, và khoảng cách giữa các lá thư dài dần. |
| 0.6 | Thư của Ro dừng sau năm thứ hai mươi. Sau đó các cột relay chỉ bắt được tín hiệu tự động: thời tiết, giờ chuẩn, mã trạm, không có giọng người. Một cột phát lại đúng vòng nhạc hiệu ấy, khớp từng giây. Các vết chì A3 chỉ tới những nơi các tín hiệu tự động này nhắc tới. |
| 0.7 | Khoảnh khắc A4 giữa game: Trái Đất vài giây, và giọng nói là một bản ghi có dấu thời gian từ mấy chục năm trước. Khoang kín của Kestrel-3 (A2) giữ thông báo cuối của chương trình: một thông báo tự động rằng trạm được "giữ lại để phục hồi sau này". Trong tiếng rè xuất hiện những cái bắt tay giữa máy với máy: thứ gì đó trên Trái Đất vẫn trả lời ping, không bằng lời. |
| 0.8 | Đường lên của chảo lớn chạm tới Trái Đất. Các câu trả lời đến với nhịp của máy. Người chơi chọn phát sóng (mục tiêu Answer). |

### 13.3 Cái kết (thay cho phần 8)
1. 07 phát sóng. Im lặng. Căn cứ trở về một đêm bình thường, người chơi vẫn điều khiển.
2. Vài đêm sau (buổi tối ở dock kế tiếp), một đốm sáng băng qua trời và một tàu đổ bộ nhỏ hạ xuống sân trước.
3. Cầu dốc hạ xuống. Một rover trẻ hơn lăn xuống: **08**, cùng dòng với 07, sạch hơn, ít dấu hiệu cô đơn hơn. Ăng-ten
   của nó nháy call sign của trạm: ba ngắn, một dài.
4. Từ loa của 08, bản ghi âm cuối của Ro vang lên: "I heard you, 07. I'm still here." Bút vẽ bản đồ trong lab đóng
   ngày tháng bên cạnh: mấy chục năm trước.
5. Không ai khác bước xuống. Ống kính của 07 mở hết cỡ, lần đầu tiên, nhìn 08.
6. Camera vượt qua Đỉnh núi lên tới mặt đêm của Trái Đất: tối đen, không một ánh đèn thành phố. Cắt sang đen. Không
   giải thích.
7. Sau credit, Mặt Trăng vẫn mở để chơi tiếp. 08 và tàu của nó ở lại căn cứ, lặng im. Không điều gì được giải thích.

### 13.4 Mỗi chức năng đều hiện trên 07 (chủ dự án, mở rộng luật 11 và mục 4.4)
Mở khoá một chức năng là thêm hoặc đổi một bộ phận nhìn thấy được cho nó, đúng khe của nó, và ống kính của 07 không bao
giờ đổi. Mới trong lần này:
- **Survey Scanner** (0.6, cần cho việc quét mảnh bản vẽ A1): một trục gập mảnh trên **khe cổ** mới, bên cạnh đầu,
  không bao giờ che ống kính. Khi 07 quét, trục mở ra và một quạt sáng mảnh lướt qua mảnh vỡ. Bản vẽ của nó tìm thấy
  trong lòng hố ở 0.6.
- Các bộ khác giữ khe cũ (mục 4.4). Mọi chức năng sau này (Star Tracker, Anchor Line…) phải nêu bộ phận của nó trước
  khi làm.
