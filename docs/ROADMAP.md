# Lofi Lunar — Roadmap & bảng việc

> Chủ sở hữu: **Director box** (session chính). Chỉ Director cập nhật file này.
> Trạng thái: ⬜ chưa làm · 🟦 đang làm · 🟩 xong (đã merge + kiểm chứng) · 🟥 bị chặn

## Đích đến
1. **Vertical slice (M1 + M2)**: 15–20 phút gameplay hoàn chỉnh, feeling đạt checklist trong `docs/VISION.md`:
   lái xe → nhặt scrap → sonar → đào relic → kéo tether về base → đặt lên kệ bảo tàng → nâng cấp tháp radio →
   nhạc rõ dần.
2. **Full game (M3 → M5)**: nâng cấp rover, mở rộng thế giới, sự kiện Nhật thực / Bão mặt trời, Biodome,
   sửa chảo vệ tinh trên đỉnh núi, phát bản nhạc cuối về Trái Đất.

## Lộ trình theo phiên bản (chủ dự án, 2026-10-08): xong hẳn một phase mới sang phase sau
Mỗi phase kết thúc bằng một bản build chơi thử, đã kiểm chứng đầy đủ, bạn chơi và góp ý xong mới mở phase kế.
Không mở rộng nội dung khi phần nhìn của phase hiện tại chưa chỉn chu.

| Phiên bản | Chủ đề | Gồm | TT |
|---|---|---|---|
| **0.4 — "Nhìn cho đúng"** | Phần nhìn chỉn chu, mọi thứ hợp logic | M3-14 (xây cho 07: trạm sửa xe với cánh tay máy, cổng bảo trì tháp, dock sạc, thang nâng, tỉ lệ thật, bánh dự phòng, trống tụ điện) · M3-12 (phong hoá: gỉ, sơn phai, bụi cho căn cứ và 07) · đánh bóng hình còn tồn (quầng sáng nhà, Trái Đất bớt rực, đọc bộ phận ở 30 m, bóng cho nhãn tên) · âm thanh và UI đi kèm · preview trong Scene view · **0.4.1: M3-15 sửa logic sau khi chủ dự án chơi thử** | 🟦 0.4.0 đã giao, đang làm 0.4.1 |
| 0.5 — "Căn cứ sống" | Hoàn thiện lòng hố | M3-07 bản đồ vẽ tay trong lander (lên bằng thang nâng) · M3-08 mặt trăng trôi khi vắng mặt (mảnh vệ tinh mới rơi) · M3-09 màn hình tiêu đề, bộ sưu tập ở căn cứ · nhật ký phi hành đoàn còn lại trong vùng | ⬜ |
| 0.6 — "Rim Terraces" | Mở rộng bản đồ #1 | Magnetic Treads (bánh to có gai) · vùng Rim Terraces · Atlas · bãi trục vớt mới, cột relay mới | ⬜ |
| 0.7 — "Shadowed Crater" | Mở rộng bản đồ #2 | vùng hố tối · Moss + Biodome · Wide Sonar · sự kiện Nhật thực | ⬜ |
| 0.8 — "The Peak" | Kết thúc | leo The Peak · sửa chảo lớn · phát sóng bản nhạc cuối · Trái Đất đáp lại | ⬜ |
| 1.0 | Phát hành | đánh bóng, cài đặt, tay cầm, hiệu năng, bản build phát hành | ⬜ |

## Các box (agent) và vùng sở hữu
| Box | Sở hữu | Ghi chú |
|---|---|---|
| 🎬 Director | `docs/`, `Core` contracts, `App`, scene `Main.unity`, Unity editor chính, merge `main` | session này |
| 🧱 foundation | ProjectSettings, `tools/unity_batch.py`, `Editor/Automation`, `Editor/SceneBuild`, import rules, test infra | |
| 🎨 art | `Scripts/Art`, `Editor/Art`, `Generated/Art`, `Shaders/` | mesh kit low-poly, palette, mọi model |
| 🌙 world | `Scripts/World`, `Editor/World` | địa hình miệng núi lửa, bầu trời, ánh sáng, post, scatter |
| 🚙 rover | `Scripts/Rover`, `Editor/Rover` | điều khiển, treo, camera, FX bánh xe |
| 🎵 audio | `Scripts/Audio/**`, `tools/audio/**` (lõi DSP dùng chung), `Audio/SFX`, `Audio/Ambience` | synth SFX đúng tông, radio, ASMR |
| 🎼 music | `tools/music/**`, `Audio/Music/**` | tự sáng tác + render nhạc lofi cho radio (D trưởng / Si thứ) |
| ⚙️ gameplay | `Scripts/Gameplay/**`, `Data/Content` | scrap, sonar, đào, tether, base, nâng cấp, save |
| 🖥️ ui | `Scripts/UI/**`, `UI/` (UXML/USS) | HUD tối giản, gợi ý, menu tạm dừng, cài đặt, thẻ ký ức |

## M0 — Nền móng kỹ thuật
| ID | Việc | Box | TT |
|---|---|---|---|
| M0-01 | Chuẩn hoá Unity 6000.0.78f1 LTS, sửa manifest (bỏ module không tồn tại, URP 17.0.4) | Director | 🟩 |
| M0-02 | Xoá rác legacy hỏng: ImageEffects Built-in, TutorialInfo, demo scene Moon pack | Director | 🟩 |
| M0-03 | Khung `_Project` + asmdef Core/App/Art/Tests, EventBus, GameContext, InputReader + Controls, Layers, contracts `IRoverState`/`ITerrainQuery`, Palette | Director | 🟩 |
| M0-04 | `tools/compile_check.py` (compile ngoài Unity, editor + player, 0 warning) và `tools/unity_mcp.py` | Director | 🟩 |
| M0-05 | CLAUDE.md, VISION, ARCHITECTURE, ROADMAP | Director | 🟩 |
| M0-06 | ProjectSettings: layer 6–11 + ma trận va chạm, gravity −1.62, chỉ Input System, chất lượng PC | foundation | 🟩 |
| M0-07 | `tools/unity_batch.py` (Unity headless trên worktree, có lock) + entry points: chạy builder, chụp ảnh, chạy test | foundation | 🟩 |
| M0-08 | Khung SceneBuild: `ISceneContributor` theo domain → dựng `Main.unity` tất định | foundation | 🟩 |
| M0-09 | Test PlayMode asmdef + smoke test GameBootstrap; AssetPostprocessor import rules; .editorconfig | foundation | 🟩 |

## M1 — "First Drive": chỉ lái xe thôi cũng đã thấy dễ chịu
| ID | Việc | Box | TT |
|---|---|---|---|
| M1-01 | Mesh kit low-poly (primitive, flat shading, palette UV, noise có seed) + test | art | 🟩 |
| M1-02 | Palette texture + material dùng chung (quyết định URP Lit vs shader riêng, có bằng chứng ảnh chụp) | art | 🟩 |
| M1-03 | Model rover theo hợp đồng rig (ARCHITECTURE), đá/tảng (6+), scrap (4) | art | 🟩 |
| M1-04 | Hàm độ cao miệng núi lửa (bowl + rim + đụn cát + crater + The Peak + bãi base) → `ITerrainQuery` + test | world | 🟩 |
| M1-05 | Mesh địa hình chia chunk, flat-shaded, tô palette theo độ dốc/độ cao, collider layer Ground | world | 🟩 |
| M1-06 | Bầu trời (gradient + sao + dải ngân hà + Trái Đất + sao băng), sương, ánh sáng, Volume post | world | 🟩 |
| M1-07 | Rải đá/tảng theo Poisson, tránh bãi base | world | 🟩 |
| M1-08 | Rover controller (sphere physics), `IRoverState`, `RoverLanded`, tuning SO | rover | 🟩 |
| M1-09 | Visual rig: bám mặt đất, treo từng bánh, nghiêng thân, quay bánh, ăng-ten lò xo, đèn pha | rover | 🟩 |
| M1-10 | Camera Cinemachine 3: drone lơ lửng, auto-recenter, FOV theo tốc độ, chống xuyên địa hình | rover | 🟩 |
| M1-11 | Vệt bánh xe + bụi + bụi khi tiếp đất | rover | 🟩 |
| M1-12 | Feel metrics PlayMode (tăng tốc, quãng phanh, bán kính quay, airtime, settle time) | rover | 🟩 |
| M1-13 | Thư viện synth Python (stdlib) + bộ SFX M1 đúng tông D pentatonic | audio | 🟩 |
| M1-14 | AudioDirector (pool), tiếng rover (hum + lạo xạo bụi + kẽo kẹt treo + tiếp đất) | audio | 🟩 |
| M1-15 | Radio: độ rõ theo khoảng cách tới base (low-pass + static + wow/flutter) | audio | 🟩 |
| M1-17 | Engine sáng tác lofi (hoà âm, voicing jazz, groove swing, cấu trúc bài) + nhạc cụ (Rhodes FM, trống, bass, pad, vinyl) | music | 🟩 |
| M1-18 | Playlist radio 5–6 bài OGG (68–84 BPM), mix/master nhất quán, có báo cáo đo loudness | music | 🟩 |
| M1-19 | "Đánh thức 07": mở màn mắt nhắm → radio rè bật → 07 mở mắt; spawn hướng 355°, camera ≤ 6° | rover + audio | 🟩 |
| M1-20 | Đèn hiệu đỏ nhấp nháy chậm trên đỉnh The Peak (landmark từ khung hình đầu) | world | 🟩 |
| M1-21 | Gỡ kẹt nhẹ nhàng (nhấc + đặt lại với fade, không phạt) | rover | 🟩 |
| M1-16 | Tích hợp M1 vào `Main.unity`, playtest, đánh giá feeling, xoá prototype rover/WALL-E | Director | 🟩 |

## M2 — Vòng lặp lõi (vertical slice)
| ID | Việc | Box | TT |
|---|---|---|---|
| M2-01 | Scrap: rải theo cụm, hút từ tính, combo nốt nhạc leo thang, tiền tệ | gameplay | 🟩 |
| M2-02 | Sonar ping [Space]: vòng sóng trên mặt đất, relic đáp lại, nhịp nhanh dần khi lại gần | gameplay | 🟩 |
| M2-03 | Đào relic [giữ E]: tia kéo, relic trồi lên giữa bụi xoáy, camera blend nhẹ | gameplay | 🟩 |
| M2-04 | Tether [giữ RMB]: lò xo PD, cuộn dây bằng scroll, đứt mềm khi quá xa | gameplay | 🟩 |
| M2-05 | Base: lander, kệ bảo tàng snap-point, nộp relic, hiệu ứng "về nhà" | gameplay + art | 🟩 |
| M2-06 | Tháp radio 3 cấp: nâng cấp bằng scrap → mở rộng vùng tín hiệu (ánh sáng + nhạc rõ) | gameplay + audio + art | 🟩 |
| M2-07 | 6 relic có cá tính (model + tên + câu chuyện ngắn) | art + gameplay | 🟩 |
| M2-08 | Save/Load tự động | gameplay | 🟩 |
| M2-09 | HUD tối giản (diegetic ưu tiên), prompt ngữ cảnh chỉ vài lần đầu, menu tạm dừng + cài đặt âm lượng/độ nhạy/đảo trục, thẻ "ký ức" khi đặt relic | ui | 🟩 |
| M2-10 | Tích hợp + playtest slice 15 phút, sửa feeling | Director | 🟦 |
| M2-11 | Quy trình build Windows (zip) để chủ dự án chơi thử ngoài Unity | foundation | 🟩 |

## Backlog (đã ghi nhận, chưa giao)
- Bụi bánh xe trông như "đá trong suốt" bay lơ lửng → nhỏ hơn, mềm hơn, trong hơn, tan nhanh hơn (rover).
- Audio đợt 3: radio rè bật lúc `RoverAwoke`; âm "nhấc bổng" khi `RoverRecovering`; hạ tiếng động cơ khi `PauseChanged`; mỗi relic đáp sonar bằng nốt riêng (cần thêm `RelicId` vào `RelicAnswered`).
- Camera blend chậm khi relic trồi lên / khi nâng cấp tháp (rover).
- World chuyển đá cuội sang `RockStyle.Grit` (16–20 tam giác) của art.
- Sau 0.4 (từ báo cáo các phiên, 2026-10-09):
  - Chuyển `PrefabWriter` / `HierarchyComparison` của rover vào `Editor/Builders` dùng chung, để mọi builder prefab chỉ lưu khi đổi (rover).
  - Mỗi người bạn có độ nâng nhãn tên riêng: tên Bell đang đè lên mặt cô (ui). Ảnh `42_site_name` của UiCapture không hiện tên khu (ui).
  - Đặt đèn đường ấm trước kính `Lamp_1` vài cm. Ghi khối `_kit` vào `RoverRigTuning.asset` (rover).
  - Từ góc 3/4 sau, trống tụ điện che ~25% số "07" bên hông (art).
  - Thêm biến thể tiếng "cộp" thứ ba cho phễu để 3 bó không lặp mẫu (audio).

## M3 — "Trạm thức giấc": chiều sâu phần 1 (xem `docs/DESIGN.md`)
| ID | Việc | Box | TT |
|---|---|---|---|
| M3-01 | Kinh thánh câu chuyện: 4 thành viên phi hành đoàn, bộ relic của từng người, nhật ký, mẩu tin radio (en + vi) | Director | 🟦 |
| M3-02 | Khung "bạn bè máy móc" + **Tilly** (drone nhỏ): tìm, sửa, sống ở căn cứ, chào 07, tự phát hiện tín hiệu | gameplay + art + audio | 🟩 |
| M3-03 | Xưởng nâng cấp rover (khung chung) + **Hover-Jump** | gameplay + rover | 🟩 |
| M3-04 | Vùng **Whispering Canyon** (mở bằng Hover-Jump) + bí mật nhìn thấy được từ căn cứ — world 🟩 (khe 18,5 m, lối ra một chiều); còn: nội dung gameplay, âm thanh hẻm, trang trí miệng hẻm (art) | world + art + gameplay + audio | 🟩 |
| M3-05 | **Bell** + núm dò đài (3 đài) + tín hiệu của Bell (dẫn đường tới thứ chưa tìm) + 3 băng cassette (bài mới + ghi chú của Ro) — đặc tả `docs/features/M3-05-bell-radio-cassettes.md` | art + gameplay + music + audio + ui | 🟩 |
| M3-06 | **Mạng tiếp sóng**: sửa các cột relay tắt của trạm → vùng phủ sóng lan dần (radio rõ, sonar xa hơn, "radio-hop" giữa các cột đã sáng, bản đồ tự vẽ); căn cứ ấm dần (đặc tả `docs/features/M3-06-relay-network.md`) | world + art + gameplay + audio + ui | 🟩 |
| M3-07 | Bản đồ vẽ tay ghim trong lander (tự vẽ trong vùng phủ sóng của M3-06) | ui | ⬜ |
| M3-08 | "Mặt Trăng trôi khi bạn vắng mặt": mưa sao băng (scrap mới), tín hiệu mới — không FOMO | gameplay | ⬜ |
| M3-09 | Màn hình tiêu đề (Continue/Settings), bộ sưu tập hiển thị ở căn cứ | ui | ⬜ |
| M3-10 | **Cô đơn & bình yên — đợt trau chuốt không khí** (trụ cột 6 trong VISION): hình (chân trời xa mờ, tương phản sáng tối, bóng dài, bầu trời sống, grading/bloom/vignette/grain) · camera (lùi ra toàn cảnh khi đứng yên, bụi lơ lửng trong đèn 07) · âm thanh (radio mỏng dần theo khoảng cách → gần như im lặng, tiếng máy nhỏ của 07) | world + rover + audio | 🟩 |
| M3-11 | **Tiến trình nhìn thấy được** (luật 11): mỗi nâng cấp có một bộ phận hiện trên 07 — bánh to có gai (Magnetic Treads), giỏ hàng sau (Cargo Cradle), chảo/ăng-ten lớn (Wide Sonar), giàn đèn có lồng (Warm Headlamp), hai trống tụ điện (Boost Coils); quà của bạn bè chữa dần "dấu hiệu cô đơn" (ô pin mặt trời, vá sơn, số 07 sơn lại); khoảnh khắc lắp đặt ở bàn Kenji (đặc tả `docs/features/M3-11-visible-progression.md`: Warm Headlamp, Boost Coils, Cargo Cradle + quà của bạn bè + khoảnh khắc lắp đặt) | art + rover + gameplay | 🟩 |
| M3-12 | **Bỏ hoang lâu năm → được chăm lại** (luật 12): bộ "phong hoá" low-poly (sơn phai, gỉ chảy từ đinh tán, bụi dồn chân, dây chùng, biển nghiêng) cho lander, tháp, xưởng, kệ, cột relay và cả 07; kiến trúc cho xe: thang nâng tời cạnh thang người ở lander, dốc lên mọi bệ, bệ sạc/đỗ của 07 thay tấm thảm; mỗi bước khôi phục làm sạch một phần căn cứ | art + gameplay + world | 🟩 |
| M3-13 | **Bãi trục vớt thay cho scrap rải đầy đất** (ý chủ dự án): mảnh vệ tinh rơi, kho tiếp tế đổ, giàn khoan, gara cũ, tàu hàng rơi trong hẻm → cắt/cạy bằng tia sáng lấy Kim loại / Dây điện / Quang học → chế tạo ở bàn Kenji; relic nằm trong "trái tim" mỗi bãi; bãi trống dần thành bộ khung — đặc tả `docs/features/M3-13-salvage-sites.md`. **Ưu tiên ngay sau M3-06, trước M3-11/M3-12** | world + art + gameplay + audio + ui + rover | 🟩 |
| M3-14 | **Xây cho 07 — chỉnh logic & tỉ lệ** (luật 12–14, ý chủ dự án): 07 không có tay → bàn chế tạo thành **Trạm sửa xe của Kenji** (cánh tay giàn treo lắp đồ, phễu nạp vật liệu bằng tia sáng); tháp radio có cổng bảo trì ngang tầm xe; dock sạc thay tấm thảm; thang nâng tời; thu nhỏ relic/cassette về đúng tỉ lệ; bánh dự phòng không còn trông như lỗi; nâng trống tụ điện — đặc tả `docs/features/M3-14-built-for-07.md`. **Ưu tiên ngay** | art + gameplay + rover + audio + ui | 🟩 |
| M3-15 | **Sửa logic sau review 0.4.0** (chủ dự án 2026-10-09): New game + cất save cũ; thang nâng có việc (sửa tời → chở 07 lên boong lander ngắm toàn lòng hố); gỉ sét thành vết có nguồn gốc, không thành hoa văn; vòng neon thành bệ sơn + đèn bệ; camera hạ thấp nhìn lên trời, 07 ngước nhìn sao — đặc tả `docs/features/M3-15-logic-pass.md` | gameplay + ui + art + rover + audio | 🟦 §1 New game + §5 ngắm sao xong (0.4.1); §2 thang nâng, §3 gỉ, §4 bệ còn lại (0.4.2) |

## M4 — Thế giới mở rộng, sự kiện & kết thúc
Magnetic Treads + **Rim Terraces** + **Atlas**; Warm Headlamp + **Shadowed Crater** + **Moss** + Biodome; sự kiện
Nhật thực / Bão mặt trời / Earthrise; tuỳ biến 07 + chế độ chụp ảnh; 24 relic (4 bộ), 8 cassette; leo The Peak,
sửa chảo vệ tinh lớn, phát sóng cuối, Trái Đất đáp lại, credits, chơi tự do sau kết thúc.

## M5 — Đánh bóng & phát hành
Tối ưu hiệu năng, tay cầm hoàn chỉnh, accessibility, build pipeline, bug bash.

## Quyết định của chủ dự án (2026-10-05)
- Nhạc: tự sáng tác & render bằng code (box music) — không dùng `music_1.mp3` trong bản phát hành.
- Rover: KHÔNG dùng WALL-E. Nhân vật mới "07": rover cũ kỹ, máy móc u buồn (xem `docs/VISION.md`).
- Git: push `main` lên `origin` khi có mốc hoàn chỉnh.
