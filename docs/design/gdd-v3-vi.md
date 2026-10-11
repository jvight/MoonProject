# Lofi Lunar: Thiết kế game v3 (bản xem duyệt cho chủ dự án)

> Bản tóm gọn bằng tiếng Việt của `docs/design/gdd-v3.md`, cùng cấu trúc 11 phần. Đây là bản nháp, chờ anh/chị duyệt.
> Hai quyết định của anh/chị là nền móng cho mọi thứ dưới đây:
> - **phát triển là trên hết:** nâng cấp 07, mở khoá và chế tạo công nghệ, đi ngày càng xa;
> - **pin là nhiên liệu:** 07 phải về sạc, và nâng cấp giúp nó đi xa dần.
>
> Không khí êm đềm vẫn giữ nguyên: không chết, không mất đồ, không bị phạt.

## 1. Lời hứa và các trụ cột
**Lời hứa:** một chiếc rover nhỏ, cũ và mệt, thức dậy trên một mặt trăng tối. Nó tự xây lại chính mình và trạm của
mình từng chút một, mỗi đêm đẩy tầm pin đi xa thêm một đoạn, cho đến khi gọi được về Trái Đất. Và người chơi không bao
giờ thấy căng thẳng.

**Trụ cột:**
1. **Lớn lên và đi xa hơn:** chuyến đi nào cũng làm 07 hoặc căn cứ mạnh hơn.
2. **Êm đềm:** hết pin không phải là thất bại.
3. **Ấm áp giữa lạnh lẽo:** trạm sạc, nhà và bạn bè là những ánh đèn ấm.
4. **Chạm đâu cũng thành nhạc:** cả lúc pin sạc đầy cũng có tiếng nhạc.
5. **Khối hình rõ ràng:** món đồ nào lắp lên 07 cũng nhìn thấy từ 30 m.
6. **Cô đơn mà bình yên:** chuẩn hình ảnh và âm thanh anh/chị đặt ra.

## 2. Vòng lặp chính
- **Vài giây:** thấy một ánh sáng → dò sóng → lái tới → dùng tia cắt, đào, kéo → nhặt được đồ. Mỗi việc tốn một chút
  pin, và đèn pin bên hông 07 tắt bớt một ô.
- **Một chuyến (5–15 phút):** xem tầm pin và các trạm sạc trên bản đồ → ra ngoài → trục vớt vật liệu cùng "món chính"
  của xác tàu (một bản vẽ hoặc một linh kiện) → ghé cột tiếp sóng để sạc thêm → về nhà, giải mã ở phòng lab, chế tạo ở
  trạm sửa xe → buổi tối ở đế sạc: pin đầy, một ánh sáng mới hiện ở chân trời.
- **Một buổi chơi (20–40 phút):** 2–3 chuyến, luôn có một bước nhảy về khả năng: món đồ mới, pin lớn hơn, một cột sạc
  mới hoặc một cấp tháp radio.
- **Một phiên bản (1–2 giờ):** thấy vùng mới → cần công nghệ và tầm pin → chế tạo → vào vùng → thắp cột ở đó → vật
  liệu bậc mới → thấy vùng kế tiếp.
- **Cả game:** nhiệm vụ của 07 đi qua 5 chặng: **Điện → Mạng lưới → Tầm với → Đường truyền → Liên lạc**.

## 3. Pin và tầm đi
- **Không bao giờ mắc kẹt.** Hết pin thì 07 chuyển sang chế độ bò chậm, im lặng, và luôn về được trạm sạc gần nhất.
  Một ô pin dự phòng của Kenji giữ cho 07 không bao giờ dừng hẳn. Tilly có thể bay ra kéo 07 về nhanh hơn. Không mất
  đồ, không bị phạt, chỉ tốn chút thời gian.
- **Đứng yên không tốn pin:** ngắm sao và nghỉ đều miễn phí. Có cánh pin mặt trời thì đứng dưới nắng còn được sạc chậm.
- **Cái gì tốn pin:** lái xe (một ô pin chạy được khoảng 150 m), dùng tia, nhảy, tăng tốc, bật đèn trong bóng tối. Dò
  sóng gần như không tốn gì.
- **Trạm sạc:** đế sạc ở nhà (đầy pin trong khoảng 4 giây) và **mọi cột tiếp sóng đã thắp**. Thắp thêm cột chính là
  nới rộng tầm đi. Nhảy sóng giữa các cột đã sáng thì miễn phí.
- **Nhìn thấy pin:** một dãy đèn nhỏ màu hổ phách dọc thân 07, mỗi đèn là một ô pin; pin to hơn thì có thêm đèn. Khi
  pin yếu, mắt 07 mờ đi một chút và cánh pin rũ xuống. Trên màn hình chỉ có một vòng nhỏ quanh la bàn.
- **Cảnh báo nhẹ và sớm:**
  - 30%: một tiếng nhạc nhỏ, và cột sạc gần nhất sáng lên ở chân trời;
  - 15%: 07 ngoái nhìn về phía trạm sạc;
  - không có còi báo động, không có màu đỏ.
- **Tầm đi theo phiên bản:**
  - 0.5: 4 ô pin, quanh nhà khoảng 150 m; có thêm cột sạc và bộ pin II thì phủ cả lòng chảo và hẻm núi;
  - 0.6: 8 ô, đi được bậc thềm vành hố;
  - 0.7: 11 ô, xuống được hố tối;
  - 0.8: 14 ô, đủ cho con đường dài nhất, lên đỉnh núi.
- **Những lúc "cần thêm tầm" ở 0.5:**
  - lần đầu đi tìm linh kiện cho Tilly: học được rằng đế sạc nạp pin;
  - tới mép vực với ô pin cuối cùng: thắp cột ở cửa hẻm, học được rằng cột chính là trạm sạc, và trong tủ của cột có
    bản vẽ bộ pin II.

## 4. Cây công nghệ, chế tạo và vật liệu
- **Cách mở khoá một món:** tìm bản vẽ hoặc linh kiện trong lõi một xác tàu → mang về, đi thang nâng lên phòng lab →
  máy đọc giải mã trong khoảng 4 giây, và một ô sáng lên trên bảng công nghệ → trạm sửa xe vẽ bản thiết kế, 07 nạp vật
  liệu, cánh tay máy lắp đồ lên 07 → 07 thay đổi thấy rõ.
- **Vật liệu 3 bậc:**
  - phế liệu: kim loại, dây điện, quang học (0.5);
  - hợp kim, mạch điện, tinh thể (0.6–0.7);
  - siêu dẫn (0.8).

  Thêm vào đó là các linh kiện có tên riêng (cuộn dây, biến áp, động cơ servo, con quay, cụm thấu kính, tản nhiệt, ô
  năng lượng). Từ 0.6, máy chế tạo ở nhà làm lại được những linh kiện đã từng tìm thấy, nên không bao giờ bị kẹt vì
  thiếu một món.
- **30 món nâng cấp trong 6 nhánh:**
  - **Di chuyển:** nhảy xa, bánh từ tính leo vách, lướt;
  - **Tia:** giỏ chở đồ, kéo vật nặng, cắt vỏ kín, tời neo;
  - **Cảm biến:** đèn pha, sonar rộng, quét sâu, đèn pha mạnh, ống ngắm sao;
  - **Năng lượng & tầm đi:** trống tụ điện, bộ pin II và III, bánh xe tiết kiệm điện, cánh pin đôi, gói pin mở rộng,
    lõi năng lượng;
  - **Bạn bè:** mô-đun trinh sát cho Tilly, bộ chở hàng cho Atlas, hai drone em gái;
  - **Trạm:** tháp radio 3 cấp, mạng cột sạc, cánh tay trạm sửa xe II, máy chế tạo, máy đọc II, chảo ăng-ten lớn.

  Mỗi phiên bản mở một tầng: 0.5 có 9 món (8 món đã làm xong), 0.6 có 9, 0.7 có 7, 0.8 có 5.
- **07 lột xác dần:**
  - lúc đầu: tàu thăm dò mệt mỏi, một cánh pin thiếu ô, ăng-ten cong, một bánh lạc loài;
  - 0.6: cao hơn trên sáu bánh xích giống nhau, có chảo sonar;
  - 0.7: **hai cánh pin**, đèn pha, cánh lướt;
  - 0.8: tời neo và cửa sổ lõi năng lượng phát sáng. Một rover thám hiểm vững chãi.

  Gương mặt một mắt hiền lành không bao giờ đổi.

## 5. Khám phá và các vùng
- **Mỗi vùng mở bằng một công nghệ:**
  - hẻm Thì Thầm: nhảy xa (0.5);
  - bậc thềm vành hố: bánh từ tính (0.6);
  - hố tối: đèn pha mạnh (0.7);
  - đường lên đỉnh núi: tời neo, cú lướt và Atlas dọn đá lở (0.8).

  Vùng nào cũng nhìn thấy được rất lâu trước khi vào được, và luôn có lối ra một chiều.
- **Vì sao muốn đi xa hơn:**
  - pin lớn dần;
  - xác tàu càng xa càng giàu đồ quý;
  - mỗi cột thắp lên là thêm một trạm sạc, một điểm nhảy sóng, radio trong trẻo và bản đồ được vẽ thêm;
  - dụng cụ mới mở được cả những xác tàu cũ trong lòng chảo mà người chơi đã thấy từ đầu;
  - ánh đèn đỏ trên đỉnh núi, mục tiêu cuối, luôn nằm trong tầm mắt.

## 6. Căn cứ
- **Đế sạc:** nạp đầy pin, lưu ngày chơi, và là "buổi tối ở đế sạc" chỉ ra ánh sáng tiếp theo.
- **Tháp radio:** cấp điện cho nhà theo từng cấp.
- **Tàu đổ bộ là phòng lab của phi hành đoàn.** Thang nâng dưới bụng tàu đưa 07 lên. Trong đó có:
  - máy đọc để giải mã bản vẽ;
  - bảng công nghệ: kế hoạch Kenji để lại cho 07, kèm bảng tiến độ của nhiệm vụ;
  - bàn bản đồ của Ines, cho thấy tầm với và các trạm sạc;
  - một góc nhỏ giữ kỷ vật.

  Một câu cho dễ nhớ: *nơi 07 biến những gì nó tìm được thành chính nó.*
- **Trạm sửa xe của Kenji:** chế tạo và lắp đồ.
- **Máy chế tạo (0.6):** làm linh kiện.
- **Cột tiếp sóng:** trạm sạc, tầm với, nhảy sóng, và cả những lá thư chúng còn giữ.
- **Mái vòm sinh thái (0.7, nhờ Moss):** trồng tinh thể.
- **Chảo ăng-ten lớn (0.8):** đường truyền về Trái Đất.
- Căn cứ lớn lên bằng máy móc, bạn bè và ánh đèn lan rộng, không phải bằng việc tự đặt thêm nhà.

## 7. Bạn bè
- **Tilly (0.5):** phát hiện đồ quanh 07, **kéo 07 về khi hết pin**. Quà của cô bé: ô pin mặt trời. Về sau có mô-đun
  trinh sát.
- **Bell (0.5):** tín hiệu của cô dẫn tới bản vẽ và linh kiện; núm dò đài. Quà: sơn lại số 07.
- **Atlas (0.6):** nâng đá lở và cả chảo ăng-ten lớn. Về sau chở vật liệu nặng về nhà.
- **Moss (0.7):** trồng tinh thể ở mái vòm.
- **Hai drone em gái (0.8):** tự gom phế liệu lặt vặt, đỡ việc vặt cho người chơi.

## 8. Câu chuyện, kể lại
- **Lý do 07 thức dậy:** khi sơ tán vì bão, phi hành đoàn để lại cho 07 hai thứ. Một là lời dặn "Cứ để radio bật".
  Hai là một **quy trình khôi phục trạm** trong bộ nhớ: nếu còn nghe được tín hiệu từ Trái Đất, hãy dựng lại trạm và
  trả lời.
- Nhiều chục năm sau, một tín hiệu yếu từ Trái Đất (đoạn nhạc hiệu của Ro trong tiếng rè) đánh thức 07, và quy trình
  bắt đầu: **Điện → Mạng lưới → Tầm với → Đường truyền → Liên lạc.**
- Quy trình vốn tính cho cả một phi hành đoàn, nhưng 07 chỉ có một mình, nên 07 phải trở thành hơn những gì nó được
  tạo ra, theo những bản kế hoạch dang dở của Kenji.
- **Số phận phi hành đoàn chỉ là gia vị, hé lộ dọc đường:**
  - các cột tiếp sóng còn giữ những tin nhắn cũ: phi hành đoàn về đến nơi an toàn, chương trình bị huỷ;
  - rồi đến những lá thư Ro vẫn viết suốt hai mươi năm;
  - kỷ vật và nhật ký nằm cạnh các món công nghệ.
- **Kết thúc:** 07 leo lên đỉnh núi bằng tất cả những gì nó đã trở thành, Atlas nâng chảo ăng-ten, và đường truyền mở
  ra. Sau một khoảng lặng, một đốm sáng trên Trái Đất nháy ba ngắn một dài: "Mình nghe thấy rồi, 07. Mình vẫn còn đây.
  — Ro." Sau đó người chơi vẫn tiếp tục khám phá, và thư của Ro vẫn đến.

## 9. Nhịp chơi theo phiên bản
- **Nguyên tắc:**
  - không quá 3 phút mà không có gì mới trong tầm mắt;
  - buổi chơi nào cũng có một bước nhảy về khả năng;
  - phiên bản nào cũng có một động tác mới;
  - không cày cuốc;
  - không có chuyến đi nào vô ích.
- **0.5 (~100 phút), Điện:** thức dậy; tháp cấp 1; Tilly; mép vực và cột sạc đầu tiên; bộ pin II; thang nâng và phòng
  lab; trạm sửa xe; nhảy qua vực; Bell; bản đồ; bốn cột sáng khắp lòng chảo.
- **0.6 (~75 phút), Tầm với 1:** bánh từ tính; bậc thềm vành hố; hợp kim; máy chế tạo; Atlas; lá thư đầu tiên của Ro.
- **0.7 (~90 phút), Tầm với 2:** đèn pha mạnh; hố tối; Moss; hai cánh pin; cắt vỏ kín; gói pin mở rộng.
- **0.8 (~60 phút), Đường truyền và Liên lạc:** tời neo; lõi năng lượng; đường lên đỉnh; chảo ăng-ten; phát sóng;
  Trái Đất trả lời.
- **1.0:** đánh bóng, cài đặt, tay cầm, chụp ảnh, sự kiện (bão mặt trời làm đầy mọi ô pin).
- **Tổng:** cốt truyện chính khoảng 5,4 giờ; chơi trọn vẹn khoảng 9–10 giờ.

## 10. Giữ / Đổi / Bỏ so với những gì đã làm
- **Giữ:**
  - các bước của căn cứ thức dậy (điện về qua tháp);
  - trạm sửa xe và máy vẽ bản thiết kế;
  - thang nâng dưới bụng tàu;
  - bản đồ của Ines (chuyển vào phòng lab);
  - Tilly, Bell và quà của họ;
  - các món đã làm (nhảy xa, giỏ chở, đèn pha, tăng tốc);
  - đoạn mở đầu nhìn qua mắt 07, ngắm sao, radio trong dần theo khoảng cách.
- **Đổi:**
  - "bộ phận câu chuyện" thành linh kiện;
  - bản vẽ được giải mã ở phòng lab;
  - bản vẽ nhảy xa được tìm thấy ở mép vực;
  - bốn góc phi hành đoàn thành bốn khu của phòng lab;
  - đế sạc giờ là nơi nạp pin;
  - cột tiếp sóng có thêm bệ sạc;
  - sonar rộng chuyển sang 0.6.
- **Bỏ:**
  - kệ kỷ niệm ngoài trời;
  - đài quan sát trên boong;
  - ý tưởng "ký ức là lý do để chơi".
- **Mới trong 0.5:** hệ thống pin, trạm sạc, chế độ bò chậm, đèn pin trên thân 07, bộ pin II.
- **Cần sửa văn bản gốc:**
  - bỏ "nhiên liệu" khỏi danh sách cấm;
  - thêm luật 15 "pin là tầm đi, không phải mối đe doạ";
  - luật 7 thêm "không bao giờ mắc kẹt vì hết pin";
  - phần "tầm phủ sóng" trong DESIGN.

## 11. Câu hỏi cho anh/chị
1. Hết pin thì bò chậm về trạm gần nhất, và có thể gọi Tilly kéo về. Cảm giác vậy đã đúng chưa?
2. Ở đoạn mở đầu, cho mắt 07 hiện vài chữ khởi động ("Phát hiện tín hiệu Trái Đất · Đường truyền tắt · Bắt đầu khôi
   phục") để mục tiêu rõ ngay từ phút đầu, hay giữ hoàn toàn không chữ?
3. Đứng dưới nắng được sạc chậm: giữ lại như một lựa chọn thư thả, hay bỏ đi để trạm sạc quan trọng hơn?
4. Tầm pin ban đầu khoảng 150 m quanh nhà: hẹp quá hay vừa?
5. "Quy trình khôi phục trạm" (Điện → Mạng lưới → Tầm với → Đường truyền → Liên lạc) đã đủ rõ và đủ hấp dẫn để làm lý do
   phát triển chưa?
6. Cái kết: giữ Ro trả lời (cảm xúc riêng tư), hay để "Trái Đất trả lời" nói chung?
7. Căn cứ lớn lên bằng máy móc, bạn bè và cột sạc, không cho tự đặt thêm nhà (chỉ có đồ trang trí ở bản 1.0). Anh/chị
   đồng ý chứ?
8. Bốn vùng sau lòng chảo cho khoảng 5,4 giờ cốt truyện chính: đã đủ chưa?
