# Lofi Lunar — Roadmap & bảng việc

> Chủ sở hữu: **Director box** (session chính). Chỉ Director cập nhật file này.
> Trạng thái: ⬜ chưa làm · 🟦 đang làm · 🟩 xong (đã merge + kiểm chứng) · 🟥 bị chặn

## Đích đến
1. **Vertical slice (M1 + M2)**: 15–20 phút gameplay hoàn chỉnh, feeling đạt checklist trong `docs/VISION.md`:
   lái xe → nhặt scrap → sonar → đào relic → kéo tether về base → đặt lên kệ bảo tàng → nâng cấp tháp radio →
   nhạc rõ dần.
2. **Full game (M3 → M5)**: nâng cấp rover, mở rộng thế giới, sự kiện Nhật thực / Bão mặt trời, Biodome,
   sửa chảo vệ tinh trên đỉnh núi, phát bản nhạc cuối về Trái Đất.

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

## M3 — "Trạm thức giấc": chiều sâu phần 1 (xem `docs/DESIGN.md`)
| ID | Việc | Box | TT |
|---|---|---|---|
| M3-01 | Kinh thánh câu chuyện: 4 thành viên phi hành đoàn, bộ relic của từng người, nhật ký, mẩu tin radio (en + vi) | Director | 🟦 |
| M3-02 | Khung "bạn bè máy móc" + **Tilly** (drone nhỏ): tìm, sửa, sống ở căn cứ, chào 07, tự phát hiện tín hiệu | gameplay + art + audio | 🟩 |
| M3-03 | Xưởng nâng cấp rover (khung chung) + **Hover-Jump** | gameplay + rover | 🟩 |
| M3-04 | Vùng **Whispering Canyon** (mở bằng Hover-Jump) + bí mật nhìn thấy được từ căn cứ — world 🟩 (khe 18,5 m, lối ra một chiều); còn: nội dung gameplay, âm thanh hẻm, trang trí miệng hẻm (art) | world + art + gameplay + audio | 🟦 |
| M3-05 | **Bell** + núm dò đài (3 đài) + tín hiệu của Bell (dẫn đường tới thứ chưa tìm) + 3 băng cassette (bài mới + ghi chú của Ro) — đặc tả `docs/features/M3-05-bell-radio-cassettes.md` | art + gameplay + music + audio + ui | ⬜ |
| M3-06 | **Mạng tiếp sóng**: sửa các cột relay tắt của trạm → vùng phủ sóng lan dần (radio rõ, sonar xa hơn, "radio-hop" giữa các cột đã sáng, bản đồ tự vẽ); căn cứ ấm dần (xem `docs/DESIGN.md` "Station reach") | world + art + gameplay + audio + ui | ⬜ |
| M3-07 | Bản đồ vẽ tay ghim trong lander (tự vẽ trong vùng phủ sóng của M3-06) | ui | ⬜ |
| M3-08 | "Mặt Trăng trôi khi bạn vắng mặt": mưa sao băng (scrap mới), tín hiệu mới — không FOMO | gameplay | ⬜ |
| M3-09 | Màn hình tiêu đề (Continue/Settings), bộ sưu tập hiển thị ở căn cứ | ui | ⬜ |
| M3-10 | **Cô đơn & bình yên — đợt trau chuốt không khí** (trụ cột 6 trong VISION): hình (chân trời xa mờ, tương phản sáng tối, bóng dài, bầu trời sống, grading/bloom/vignette/grain) · camera (lùi ra toàn cảnh khi đứng yên, bụi lơ lửng trong đèn 07) · âm thanh (radio mỏng dần theo khoảng cách → gần như im lặng, tiếng máy nhỏ của 07) | world + rover + audio | ⬜ |

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
