# Lunarion — Roadmap & bảng việc

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
| **0.4 — "Nhìn cho đúng"** | Phần nhìn chỉn chu, mọi thứ hợp logic | M3-14 (xây cho 07: trạm sửa xe với cánh tay máy, cổng bảo trì tháp, dock sạc, thang nâng, tỉ lệ thật, bánh dự phòng, trống tụ điện) · M3-12 (phong hoá: gỉ, sơn phai, bụi cho căn cứ và 07) · đánh bóng hình còn tồn (quầng sáng nhà, Trái Đất bớt rực, đọc bộ phận ở 30 m, bóng cho nhãn tên) · âm thanh và UI đi kèm · preview trong Scene view · **0.4.1: M3-15 sửa logic sau khi chủ dự án chơi thử** · **0.4.2: gỉ sét có nguồn gốc (M3-15 §3)** | 🟩 0.4.0, 0.4.1, 0.4.2 đã giao (0.4.2 = 5a293e84) |
| 0.5 — "Căn cứ thức dậy" | Năng lượng và mạng lưới lòng hố (mục tiêu Power) | **M3-16** căn cứ thức dậy từng bước (lõi) · **M3-20** pin, trạm sạc, bò về (4 ô → 6 ô với Cell Pack II, bán kính ~140 → ~220 m; sạc ở dock và cột relay đã sáng) · **M3-19** phòng thí nghiệm của phi hành đoàn trong lander, lên bằng thang dưới bụng tàu · **M3-21** mục tiêu hai tầng (bảng phục hồi, cây công nghệ 9 nút) · M3-18 nhìn cho hiện đại · M3-17 cảnh mở đầu qua mắt 07 · M3-07 bản đồ khảo sát trong lab · vật liệu T1 · **không có công trình, không có T2** · M3-09 phần A (màn hình tiêu đề) đã có, phần B (bộ sưu tập) dời sang 1.0 | 🟦 **0.5.0 đã build (13faaec7, verify + push), chờ chủ dự án chơi thử và duyệt:** `Builds/Lunarion-0.5.0-20261011-13faaec7.zip` |
| 0.6 — "Rim Terraces" | Link, tuyến thứ nhất | **Cốt truyện "im lặng dài" (GDD v3 §13):** thư của Ro dừng ở năm thứ 20, tín hiệu tự động; **Survey Scanner** (cần quét mảnh, cần trục quét gập trên cổ 07) · **Bài học Subnautica (GDD v3 §12):** mảnh bản vẽ (quét 2–4 mảnh mỗi bản vẽ), tín hiệu radio vẽ vết chì lên bản đồ khảo sát, cửa kín của Kestrel-3 hiện ra như lời hứa · Magnetic Treads · vùng Rim Terraces · Atlas · Wide Sonar (dời từ 0.7) · Heavy Tow · Scout Module của Tilly · Efficient Hubs và Cell Pack III (8 ô, −25% hao khi lái, ~370 m) · **M3-22 bệ xây dựng**: trụ sạc, Fabricator, Bay Wing · vật liệu T2 (Alloy, Circuitry) · M3-08 mặt trăng trôi khi vắng mặt | ⬜ |
| 0.7 — "Shadowed Crater" | Link, tuyến thứ hai, và công cụ cho The Peak | khoang kín của Kestrel-3 (Thermal Cutter) · cú ngoặt giữa game: mạng lưới bắt được Trái Đất vài giây rồi mất (GDD v3 §12) · Floodlights · vùng hố tối · Moss + Biodome · Crystal · Twin Solar Wing, Range-Extender (11 ô, ~500 m) · Thermal Cutter · Long Leap · Pylon II, Solar Array, Materials Store, Ferry Harness, Lab Reader II | ⬜ |
| 0.8 — "The Peak" | Dish và Answer | **kết mới (GDD v3 §13):** tàu đổ bộ đến, rover 08 bước xuống, bản ghi âm cuối của Ro, mặt đêm Trái Đất tối đen, kết bỏ ngỏ · lõi Kestrel-3 giữ phần cuối của Power Cell · cột relay chân núi (Link xong) · Anchor Line · Power Core (14 ô, ~650 m) · trụ sạc trạm đường núi · Star Tracker · Sister Drones và Drone Hangar · sửa chảo lớn · phát sóng · Ro trả lời | ⬜ |
| 1.0 | Phát hành | đánh bóng, M3-09 phần B (bộ sưu tập), cài đặt, tay cầm, chế độ chụp ảnh, Paint Booth, thư sau kết thúc, sự kiện (Nhật thực, Bão mặt trời, Earthrise), hiệu năng, bản build phát hành | ⬜ |

> Từ 2026-10-11 thiết kế trọn game là `docs/design/gdd-v3.md` (chủ dự án đã duyệt): tiến trình là chính, pin là tầm
> đi, bốn mục tiêu phục hồi (Power → Link → Dish → Answer), còn lại khám phá tự do. Mỗi phiên bản ghi rõ tầng vật liệu,
> tầm đi và công trình của nó.

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
- **Blender cho model chính (chủ dự án 2026-10-12, dùng từ 0.8):** nhánh thử nghiệm `box/blender` (rover 08 dựng bằng script Blender 4.5.9 tại `D:/Tools/Blender`, báo cáo `docs/design/trial-blender-svg.md`) giữ lại, chưa land. Trước model Blender thứ hai: một module xuất file tất định dùng chung, một lớp quy tắc import cho `Generated/Art/Blender/`, test hai lần chạy ra cùng byte, đường dẫn Blender trong HANDOFF §4. 07, nhà đổ bộ và các model hiện có giữ dựng bằng code.
- **M3-18 còn hai chỉ tiêu (World):** độ tương phản khung đêm ≥ 0,40 (cần chỉnh grade/exposure) và mặt đất gần camera bớt phẳng (thêm chi tiết tiền cảnh: vệt bánh dock → garage, sỏi trên bãi); vành hẻm còn sáng hơn mặt đất gần.
- PlayMode test cho đèn mốc (L1) sáng lên thật; pool ánh sáng biến mất ở mức chất lượng Mobile (RP asset Mobile không có depth texture).

- Bụi bánh xe trông như "đá trong suốt" bay lơ lửng → nhỏ hơn, mềm hơn, trong hơn, tan nhanh hơn (rover).
- Audio đợt 3: radio rè bật lúc `RoverAwoke`; âm "nhấc bổng" khi `RoverRecovering`; hạ tiếng động cơ khi `PauseChanged`; mỗi relic đáp sonar bằng nốt riêng (cần thêm `RelicId` vào `RelicAnswered`).
- Camera blend chậm khi relic trồi lên / khi nâng cấp tháp (rover).
- World chuyển đá cuội sang `RockStyle.Grit` (16–20 tam giác) của art.
- `BayTests.Refresh_ReportsOnlyChanges_AndAllocatesNothing` thỉnh thoảng lỗi zero-GC khi chạy cả bộ, kể cả sau khi đã warm-up: cần chụp profiler để tìm chỗ cấp phát (ui).
- Từ kịch bản trọn game (`docs/design/game-flow.md` §8, 2026-10-11):
  - 0.5 nếu rẻ, sau Playthrough bước 0–8: khi Continue, Tilly đã tìm thấy một ánh lấp lánh mới gần nhà qua đêm, và cảnh thức ngắn nhìn về phía đó (gameplay + rover).
  - 0.6: "hốc tối" gần hố Kestrel (ống dung nham ngắn, tối cho tới khi có Warm Headlamp, chứa nhật ký đầu tiên của Theo và một băng) để Headlamp mở được một bí mật (world + art + gameplay).
  - Bốn ô cửa sổ giường tầng của phi hành đoàn là renderer riêng, tối cho tới khi bộ kỷ vật của người đó đầy (art; world thắp ở M3-09).
  - 0.8: tia của 07 đặt băng của Ro vào máy Bell trước lần leo cuối; chuỗi cột relay đưa bài hát lên chảo lớn.
- Sau 0.4 (từ báo cáo các phiên, 2026-10-09):
  - Chuyển `PrefabWriter` / `HierarchyComparison` của rover vào `Editor/Builders` dùng chung, để mọi builder prefab chỉ lưu khi đổi (rover).
  - Mỗi người bạn có độ nâng nhãn tên riêng: tên Bell đang đè lên mặt cô (ui). Ảnh `42_site_name` của UiCapture không hiện tên khu (ui).
  - Đặt đèn đường ấm trước kính `Lamp_1` vài cm. Ghi khối `_kit` vào `RoverRigTuning.asset` (rover).
  - Từ góc 3/4 sau, trống tụ điện che ~25% số "07" bên hông (art).
  - Thêm biến thể tiếng "cộp" thứ ba cho phễu để 3 bó không lặp mẫu (audio).
  - `ExcavationAndTetherSessions.Tether_CaughtBehindAWall_LetsGoSoftly…` chập chờn khi máy bận (không bắt được dây trong LatchTimeout lúc chạy cả bộ PlayMode, chạy riêng thì qua): làm test chờ theo trạng thái thay vì thời gian (gameplay).

## M3 — "Trạm thức giấc": chiều sâu phần 1 (xem `docs/DESIGN.md`)
| ID | Việc | Box | TT |
|---|---|---|---|
| M3-01 | Kinh thánh câu chuyện: 4 thành viên phi hành đoàn, bộ relic của từng người, nhật ký, mẩu tin radio (en + vi) | Director | 🟦 |
| M3-02 | Khung "bạn bè máy móc" + **Tilly** (drone nhỏ): tìm, sửa, sống ở căn cứ, chào 07, tự phát hiện tín hiệu | gameplay + art + audio | 🟩 |
| M3-03 | Xưởng nâng cấp rover (khung chung) + **Hover-Jump** | gameplay + rover | 🟩 |
| M3-04 | Vùng **Whispering Canyon** (mở bằng Hover-Jump) + bí mật nhìn thấy được từ căn cứ — world 🟩 (khe 18,5 m, lối ra một chiều); còn: nội dung gameplay, âm thanh hẻm, trang trí miệng hẻm (art) | world + art + gameplay + audio | 🟩 |
| M3-05 | **Bell** + núm dò đài (3 đài) + tín hiệu của Bell (dẫn đường tới thứ chưa tìm) + 3 băng cassette (bài mới + ghi chú của Ro) — đặc tả `docs/features/M3-05-bell-radio-cassettes.md` | art + gameplay + music + audio + ui | 🟩 |
| M3-06 | **Mạng tiếp sóng**: sửa các cột relay tắt của trạm → vùng phủ sóng lan dần (radio rõ, sonar xa hơn, "radio-hop" giữa các cột đã sáng, bản đồ tự vẽ); căn cứ ấm dần (đặc tả `docs/features/M3-06-relay-network.md`) | world + art + gameplay + audio + ui | 🟩 |
| M3-07 | Bản đồ vẽ tay của Ines, nay là bản đồ khảo sát trong phòng thí nghiệm (tự vẽ trong vùng phủ sóng của M3-06) — đặc tả `docs/features/M3-07-crew-map.md` (duyệt 2026-10-11) | gameplay + ui + world + art + rover + audio | 🟦 phần chung đã land (0acfc3f0); phần nối vào lab đi cùng M3-19 |
| M3-08 | "Mặt Trăng trôi khi bạn vắng mặt": mưa sao băng (scrap mới), tín hiệu mới — không FOMO | gameplay | ⬜ |
| M3-09 | Màn hình tiêu đề là chính thế giới (Continue / New Game / Settings / Quit), mỗi bộ sưu tập có chỗ ở căn cứ — đặc tả `docs/features/M3-09-title-collections.md` (duyệt 2026-10-11) | app + ui + rover + gameplay + art + world + audio | ⬜ |
| M3-10 | **Cô đơn & bình yên — đợt trau chuốt không khí** (trụ cột 6 trong VISION): hình (chân trời xa mờ, tương phản sáng tối, bóng dài, bầu trời sống, grading/bloom/vignette/grain) · camera (lùi ra toàn cảnh khi đứng yên, bụi lơ lửng trong đèn 07) · âm thanh (radio mỏng dần theo khoảng cách → gần như im lặng, tiếng máy nhỏ của 07) | world + rover + audio | 🟩 |
| M3-11 | **Tiến trình nhìn thấy được** (luật 11): mỗi nâng cấp có một bộ phận hiện trên 07 — bánh to có gai (Magnetic Treads), giỏ hàng sau (Cargo Cradle), chảo/ăng-ten lớn (Wide Sonar), giàn đèn có lồng (Warm Headlamp), hai trống tụ điện (Boost Coils); quà của bạn bè chữa dần "dấu hiệu cô đơn" (ô pin mặt trời, vá sơn, số 07 sơn lại); khoảnh khắc lắp đặt ở bàn Kenji (đặc tả `docs/features/M3-11-visible-progression.md`: Warm Headlamp, Boost Coils, Cargo Cradle + quà của bạn bè + khoảnh khắc lắp đặt) | art + rover + gameplay | 🟩 |
| M3-12 | **Bỏ hoang lâu năm → được chăm lại** (luật 12): bộ "phong hoá" low-poly (sơn phai, gỉ chảy từ đinh tán, bụi dồn chân, dây chùng, biển nghiêng) cho lander, tháp, xưởng, kệ, cột relay và cả 07; kiến trúc cho xe: thang nâng tời cạnh thang người ở lander, dốc lên mọi bệ, bệ sạc/đỗ của 07 thay tấm thảm; mỗi bước khôi phục làm sạch một phần căn cứ | art + gameplay + world | 🟩 |
| M3-13 | **Bãi trục vớt thay cho scrap rải đầy đất** (ý chủ dự án): mảnh vệ tinh rơi, kho tiếp tế đổ, giàn khoan, gara cũ, tàu hàng rơi trong hẻm → cắt/cạy bằng tia sáng lấy Kim loại / Dây điện / Quang học → chế tạo ở bàn Kenji; relic nằm trong "trái tim" mỗi bãi; bãi trống dần thành bộ khung — đặc tả `docs/features/M3-13-salvage-sites.md`. **Ưu tiên ngay sau M3-06, trước M3-11/M3-12** | world + art + gameplay + audio + ui + rover | 🟩 |
| M3-14 | **Xây cho 07 — chỉnh logic & tỉ lệ** (luật 12–14, ý chủ dự án): 07 không có tay → bàn chế tạo thành **Trạm sửa xe của Kenji** (cánh tay giàn treo lắp đồ, phễu nạp vật liệu bằng tia sáng); tháp radio có cổng bảo trì ngang tầm xe; dock sạc thay tấm thảm; thang nâng tời; thu nhỏ relic/cassette về đúng tỉ lệ; bánh dự phòng không còn trông như lỗi; nâng trống tụ điện — đặc tả `docs/features/M3-14-built-for-07.md`. **Ưu tiên ngay** | art + gameplay + rover + audio + ui | 🟩 |
| M3-15 | **Sửa logic sau review 0.4.0** (chủ dự án 2026-10-09): New game + cất save cũ; thang nâng có việc (sửa tời → chở 07 lên boong lander ngắm toàn lòng hố); gỉ sét thành vết có nguồn gốc, không thành hoa văn; vòng neon thành bệ sơn + đèn bệ; camera hạ thấp nhìn lên trời, 07 ngước nhìn sao — đặc tả `docs/features/M3-15-logic-pass.md` | gameplay + ui + art + rover + audio | 🟩 §1 New game + §5 ngắm sao (0.4.1); §3 gỉ có nguồn gốc (0.4.2); §2 thang nâng và §4 bệ chuyển sang M3-16 (0.5) |
| M3-16 | **Lumen thức dậy từng bước** (chủ dự án 2026-10-09): căn cứ ngủ đến khi được đánh thức; tháp là trái tim, mỗi cấp tháp cấp điện thêm một chặng (nhà → trạm sửa xe → thang nâng); mỗi bước dạy một động tác; mỗi nơi một việc nói trong một câu; bàn cũ của Kenji thành bảng bản vẽ (kit mở bằng bản vẽ tìm trong bãi trục vớt); thang nâng lên boong ngắm toàn lòng hố; gộp M3-15 §2, §4 — đặc tả `docs/features/M3-16-waking-lumen.md` | tất cả box | 🟦 đã duyệt 2026-10-09: lõi của 0.5, mở game tối hẳn, kit mở bằng bản vẽ. Đợt 1 và các tip đã land 2026-10-11; còn: bệ, P1, P2, P6, P9, trạm thang nâng và cảnh ngắm, Playthrough bước 0–8 |

| M3-17 | **Kịch bản trọn game và cutscene** (chủ dự án 2026-10-11): kịch bản luồng chơi từ New Game tới kết thúc, core loop, nhịp khám phá và nâng cấp; danh mục cảnh điện ảnh, mở đầu bằng góc nhìn qua ống kính của 07 — phiên story & flow soạn `docs/design/game-flow.md`, `docs/design/cutscenes.md`; GDD v3 `docs/design/gdd-v3.md` thay bảng phiên chơi của game-flow | story & flow → slice "opening" (rover + world + audio) | 🟦 GDD v3 đã duyệt; cảnh mở đầu O1–O14 và dòng khởi động O5 đã land; còn: vòng kính phản chiếu Trái Đất (art), luật nhắc O14 (ui), đài phát khẽ ở màn hình tiêu đề (audio) |
| M3-19 | **Phòng thí nghiệm của phi hành đoàn** (chủ dự án 2026-10-11): lander thành lander ở được, thang nâng dưới bụng đưa 07 vào trong; bàn xoay với bốn trạm: bản đồ khảo sát, máy đọc bản vẽ, bảng công nghệ có bảng phục hồi, kệ kỷ vật; máy vẽ chuyển vào trạm sửa xe; dọn gọn căn cứ thành ba khối — đặc tả `docs/features/M3-19-the-lab.md` | slice lab-build (art + world) · slice lab-systems (gameplay + rover + ui + audio) | ⬜ đặc tả xong 2026-10-11, chờ giao |
| M3-20 | **Pin, trạm sạc và bò về** (chủ dự án 2026-10-11, luật 15): pin là tầm đi, 1 ô ≈ 120 m; sạc ở dock và bệ 4 m dưới cột relay đã sáng; hết pin thì bò chậm về, Tilly tự bay ra dẫn đường; Cell Pack II (4 → 6 ô) — đặc tả `docs/features/M3-20-battery-chargers-crawl.md` | slice battery (gameplay + rover + art + world + ui + audio) | 🟦 mốc A (chơi được) xong 2026-10-11, chờ land. **Chặn phát hành 0.5:** `BatteryTuning.asset` tạm để 8 ô / 400 m cho Playthrough qua; mốc B phải đưa về đúng đặc tả (4 ô, 120 m) cùng lộ trình golden path mới (về dock giữa các chuyến, thắp relay.2 ở lần đầu tới vực, lắp Cell Pack II trước cú nhảy, radio-hop về từ relay.3) |
| M3-21 | **Mục tiêu hai tầng** (chủ dự án 2026-10-11, luật 16): bốn mục tiêu phục hồi có đèn tiến độ trên bảng phục hồi (Power, Link, Dish, Answer), mọi thứ khác tự do; dòng "tiếp theo" chỉ nói về tầng 1; cây công nghệ 9 nút của 0.5; Hover-Jump tìm ở mép vực; đổi "story part" thành "component" — đặc tả `docs/features/M3-21-two-tier-goals.md` | slice goals (gameplay + ui + audio) | ⬜ đặc tả xong, giao sau khi lab-systems land M3-19 |
| M3-22 | **Bệ xây dựng và công trình** (chủ dự án 2026-10-11): chỉ xây trên bệ định sẵn: trụ sạc, Fabricator, Bay Wing (0.6), rồi Solar Array, Materials Store (0.7), Drone Hangar (0.8) — đặc tả `docs/features/M3-22-build-pads-structures.md` | world + art + gameplay + rover + ui + audio | ⬜ **0.6** (chủ dự án chốt 2026-10-11) |
| M3-18 | **Nhìn cho hiện đại, lòng hố đọc được** (chủ dự án 2026-10-11): bóng tiếp đất, tách sáng tối + viền sáng, vũng sáng ấm theo từng chặng điện, mặt đất ba tầng chi tiết, sương nhiều lớp, trăng chứ không phải tuyết, grading có màu đen thật; mốc cao cho mỗi bãi, cọc chỉ đường, cầu cáp gãy ở vực, góc ngắm thật — đặc tả `docs/features/M3-18-modern-look.md` | world + art + rover + ui + tools | 🟦 |

| M3-23 | **Cốt truyện "im lặng dài"** (chủ dự án 2026-10-11, GDD v3 §13): loài người đã mất từ lâu, mọi tín hiệu là bản ghi cũ hoặc máy móc; hé lộ dần qua thư, nhật ký, tín hiệu (tầng 2); kết tăm tối, bỏ ngỏ với rover 08 và bản ghi âm cuối của Ro. 0.5 chỉ thêm ngày tháng vào thư của Ro | story & flow (văn bản) → gameplay + ui | ⬜ viết lại văn bản cho 0.6–0.8; 0.5: ngày tháng trên thư |

## M4 — Thế giới mở rộng, sự kiện & kết thúc
Theo GDD v3 §9 (0.6–0.8 ở bảng phiên bản trên): Magnetic Treads + **Rim Terraces** + **Atlas**; Floodlights +
**Shadowed Crater** + **Moss** + Biodome; công trình trên bệ; leo The Peak, sửa chảo vệ tinh lớn, phát sóng cuối, Ro
trả lời, credits, chơi tự do sau kết thúc. Sự kiện Nhật thực / Bão mặt trời / Earthrise, tuỳ biến 07 và chế độ chụp
ảnh ở 1.0.

## M5 — Đánh bóng & phát hành
Tối ưu hiệu năng, tay cầm hoàn chỉnh, accessibility, build pipeline, bug bash.

## Quyết định của chủ dự án (2026-10-05)
- Nhạc: tự sáng tác & render bằng code (box music) — không dùng `music_1.mp3` trong bản phát hành.
- Rover: KHÔNG dùng WALL-E. Nhân vật mới "07": rover cũ kỹ, máy móc u buồn (xem `docs/VISION.md`).
- Git: push `main` lên `origin` khi có mốc hoàn chỉnh.
