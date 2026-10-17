# Title → play, and an opening cutscene: research

> Author: the game design session, for the Director and the owner. Research only: nothing here is approved for
> building.
>
> The owner: "the transition from the title screen into the game isn't smooth yet; I think we need an opening
> cutscene, but later, after the play time, the setting and the first guidance are good. Note it and research it."
>
> Read for this:
> - `docs/design/cutscenes.md` §1 (the lens-wake storyboard O1–O14, §1.7 title → opening);
> - `docs/design/title-screen.md` (the CRT title variants);
> - `docs/design/opening-v2.md` (B0–B2);
> - `docs/design/07-identity.md` (SSR-5, CONTINUE, the wake-on-carrier trickle post);
> - `docs/design/story-v4.md`;
> - `docs/design/core-feel-audit.md` (ATMO-01).
>
> Code read for this:
> - `App/GameBootstrap.cs` (title flow, `ContinueRequested`, `NewGameRequested`, the async reload);
> - `Rover/Camera/OpeningDirector.cs`, `OpeningScript.cs`, `OpeningPhase.cs`, `WakeOpening.cs`;
> - `Rover/Character/WakeUpSequence.cs`;
> - `Audio/TitleMusic.cs`, `TitleMusicFader.cs`, `WakeAudio.cs`;
> - `UI/Drawing/TitleScenePainter.cs` and `TitleCrtOverlayPainter.cs`, `UI/Tuning/TitleSettings.cs`.
>
> Items marked *verify* were read in code but not seen in a build.

## 1. Diagnosis: what happens today, and where it's rough
**Today's chain (New Game, no save):**
1. The title is up: a 2D CRT schematic (07 driving on a turning moon) is painted over the 3D world, with the title
   theme playing.
2. The 3D camera holds the low lens shot on 07's closed eye.
3. **New Game:**
   - the menu curtain closes (0.9 s in, 1.4 s out);
   - the title theme fades out over ~2.5 s;
   - `SessionStarted(true)` fires, and the camera pushes into the closed lens (~2 s) into O1's black (3 s);
   - the lens wake O2–O13 (~40 s) plays, ending on the chase frame.
4. Over a save, the save is put away and an async reload is pre-warmed. It activates inside O1's black
   (`WakeBeat.PushedIn`), with a timeout fallback. The core-feel audit's ATMO-01 freeze is addressed by the async
   reload; *verify in a build.*

| # | Rough point | Cause |
|---|---|---|
| R1 | **Two different 07s in one second.** The title shows a flat 2D line-drawn 07 on a toy moon; the menu fades and a 3D 07's closed eye appears in a different world (the eclipse). The player isn't sure what changed. | The title (`TitleScenePainter`, a 2D schematic) and the camera shot behind it are two unrelated pictures. `title-screen.md` recommends v2 (the title drawn on 07's own 3D CRT face) but the lens wake starts from the *eye*, not the face. |
| R2 | **Dead air before anything moves:** curtain out 1.4 s + push-in 2 s + O1 black 3 s ≈ **6–7 s** of fading and black after the click. Over a save the black holds as long as the load. | The beats are chained in sequence, not overlapped; O1's black was sized for a storyboard, not for a click. |
| R3 | **The music leaves the wrong way.** The title theme fades out over 2.5 s *while* the wake's static swells. Two layers in different moods overlap, then the game falls into near-silence. It reads as "the music stopped", not "07 woke". | `TitleMusic`/`TitleMusicFader` and `WakeAudio` each fade on their own clock from the same event. |
| R4 | **The opening looks at things that no longer exist.** O6 (the sleeping home lander and its belly lift), O7 (the tower's pilot blink) and O13 (07 "on its dock") target the prebuilt home, which opening v2 cuts. The new world (eclipse, the dead trickle post, the lander's amber beacon at 165 m) isn't in the script. | `OpeningScript`'s gaze targets and `cutscenes.md` §1.2 predate opening v2. |
| R5 | **The boot text disagrees with the story.** O5 shows "EARTH SIGNAL · DETECTED"; story v4 and opening v2 use "ORBITAL SIGNAL · DETECTED" (the Arks, not Earth). | cutscenes §1.2 predates story v4. |
| R6 | **Continue eases from a shot of a home that's gone.** The short wake starts "from the title's home-wide frame". | cutscenes §1.6 and `OpeningDirector` (the over-a-save title frame) predate the base-building pivot. |
| R7 | **The confirm press may leak into play.** Continue enables input in the same frame `SessionStarted` fires (`GameBootstrap.OnContinueRequested`). A held E or South button could start a scan or cut. *Verify.* | Input enabled before the confirm button has been seen released. |
| R8 | **Two titles in the same session.** If the lens wake still shows `TitleCard`'s drifting wordmark, a wordmark lands over 07's eye (cutscenes §1.7 retired it). *Verify.* | Legacy UI not removed. |
| R9 | **The opening has no reason in it.** It shows *that* 07 wakes, never *why* (the carrier, CONTINUE). The player learns the why only from the boot text's three lines. The owner's "not smooth" is partly this: the jump from a menu to "an eye opens" has no bridge of meaning. | The storyboard was written before story v4's wake-on-carrier logic existed. |

## 2. Quick fixes (no cutscene, can land soon)
| Fix | What | Owner | Size |
|---|---|---|---|
| Q1 | **Make the title *be* 07's CRT, and switch it off:** on New Game or Continue the CRT title collapses like an old tube (the picture squeezes to a bright line, then a dot, ~0.6 s), and that black *is* O1. One 07, one screen, no unrelated cut. Works with any title variant. | ui (the effect), audio (the tube "tock") | S |
| Q2 | **Overlap, don't chain:** start the camera's push-in at the same frame as the curtain, and trim O1's black to ≤ 1.5 s (the power-on thump inside it). Over a save the black still waits for the load. Target: first image ≤ 3 s after the click. | rover (timings), ui | S |
| Q3 | **One audio hand-off:** the title theme ducks to silence in 0.8 s under the CRT switch-off; the static and the thump own the black. Optionally the title theme ends on a held D that the wake's ghost chord (D–A–E) continues. | audio | S |
| Q4 | **Retarget the opening's gazes to opening v2:** O6 → the dead trickle post and its cable; O7 → the lander's amber beacon (the warm point that leads); O8 → the Peak; O9 → the eclipsed Earth (copper ring) and the faint Ark point; O13 → the chase frame on the wake pad facing the lander and the Peak. | rover (+ world anchors) | S |
| Q5 | **Text to story v4:** O5's boot lines become `ORBITAL SIGNAL · DETECTED / UPLINK · OFFLINE / BEGIN RECOVERY`, plus 07-identity's `UNIT 07 · SSR-5 · CONTINUE · 13 148 DAYS` on the first hold boot. | story (keys), ui | S |
| Q6 | **Continue lands on the player's base:** the over-a-save title frame looks at 07 parked at the base the player built (or at the wake pad if nothing is built), then eases to the chase frame. | rover | S |
| Q7 | **Swallow the confirm press:** enable gameplay input only after the confirm button has been seen released (as `HoldToConfirm` already does). | app, ui | S |
| Q8 | **Remove leftovers:** `TitleCard`'s wordmark over the wake (if still wired) and the "windows go dark" step of New Game over a save (there are no windows any more). | ui, world | S |

Q1–Q4 together answer most of "not smooth". Q4 is needed anyway the moment opening v2 lands.

## 3. How well-regarded games open (the first 60–120 s)
| Game | What its first 60–120 s do | Learn | Doesn't fit Lunarion |
|---|---|---|---|
| **Journey** | The title *is* the world: a robed figure in the desert. The first input walks it up a dune, and at the crest **the mountain** appears, the goal for the whole game. No words. | Menu = world; the first effort ends on a reveal of the far goal. Our Peak beacon and the lander's light do that job. | — (fits almost completely) |
| **Subnautica** | A short crash cinematic (the Aurora breaks up, the lifepod drops), then you wake in a burning pod. A calm PDA voice; you put out the fire and climb out of the hatch: **the ocean reveal**. | The first step outside is the awe beat; the device (PDA) is introduced diegetically. | Fire, damage, a talking AI, survival panic |
| **Outer Wilds** | You wake at a campfire under a starry sky, look up, and see the stars and the planets; a short walk to the launch site. | Wake *looking up*; the sky is the hook; control from second one. | Talking villagers, the time loop |
| **Death Stranding** | Long cinematics, establishing the world and the rules, then a quiet first walk. A music swell plays when you crest a hill after effort. | **Music as a reward for effort** (ours: the radio clears into music at Power on, opening-v2 B8). | Long exposition, voices, the length |
| **Firewatch** | A text prologue of personal choices, intercut with walking to the lookout; then the wilderness reveal. | Compress backstory into short abstract beats with player input; end on a vista. | A sentimental human-relationship story in text |
| **Astroneer** | A drop pod lands from orbit in ~20 s; you step out onto a planet and the first tool is already in your hands. | Short, physical arrival; the tool is in hand at once. | Arrival from space isn't our story (07 has been here 36 years) |
| **Inside / Limbo** | No menu flourish: the game starts the moment you press, and the title is a small in-world element. | Zero dead air. | Darkness and threat |
| **Tunic** | The title is a painterly in-world shot; you start as the fox wakes on a beach. | A wake on the shore of a place that is clearly old and abandoned. | — |
| **Stray** | A short, wordless, playable prologue shows the cat's life before the fall. Emotion is set up through behaviour, not text. | A playable cutscene; behaviour carries the feeling. | A family of cats (warmth from companions we don't have) |

**What the best openings share:**
- the menu and the world are one place;
- control comes back within seconds;
- the first image answers *where am I*;
- the first minute ends on a visible goal;
- every reason is shown, not told.

**What they all avoid:**
- black screens after the click;
- an unrelated cut between the menu and the game;
- text that explains.

## 4. Opening cutscene concepts (for later, as the owner says)
Each fits story v4 (the Departure, 1988; the Arks at L4/L5; Earth dark), 07-identity (SSR-5, CONTINUE, wake-on-carrier,
people treat 07 as a tool) and opening-v2's B0. Each is **short (≤ 35 s), skippable by any input after 2 s, with the
camera eased and no shake**. Each must set up for B1–B2:
- the lander's amber beacon and the cable as the way;
- the eclipse (so its end at ~12 min reads);
- the battery at 50%;
- the task (CONTINUE: recovery).

### Concept A: "The carrier arrives" *(recommended)*
| Time | Beat | Camera | Sound |
|---|---|---|---|
| 0–3 s | The title CRT switches off (Q1) into black. | — | the tube's "tock", then silence |
| 3–11 s | **The basin from high above**, in eclipse: dark Earth with its copper ring; the faint Ark point beside it; the Peak's red blink; far below, one amber light (the lander) and, small and dark, 07 by its post. | a slow crane down from the sky toward 07 (eased, ~8 s) | the deep, wide, near-silent room tone; one low hum |
| 11–17 s | **The carrier reaches 07:** no beam (vacuum logic, radio is invisible). 07's antenna tip lights cyan and blinks its call sign (three short, one long); its cell lamps step up one by one. | the crane settles beside 07's antenna | a faint carrier tone on D fades up, *heard only once the camera is beside 07* (the camera is "inside its radio") |
| 17–24 s | **07's CRT wakes:** the amber boot lines, operational only: `ORBITAL SIGNAL · DETECTED`, `CONTINUE · QUEUE 1 · RECOVERY`, `UPLINK · OFFLINE`, `CELLS 50%` | a slow push onto the face, then into the eye | the CRT's whine and ticks; the carrier holds |
| 24–35 s | **Into the lens:** O2–O4 compressed (the lid opens, the lens hunts) and the gaze to the lander's beacon (Q4), then the pull-out to the chase frame (O10–O13). | as today's opening | the ghost chord (D–A–E) surfaces once on the beacon |

**Why it's the recommendation:**
- It answers the question the player has after the menu (*why does 07 wake now?*) with logic, not words: a carrier
  arrives, an old protocol resumes.
- It sets the scene (eclipse, the Arks, dark Earth, the lander's light) before control, and stays short.
- It reuses today's lens wake.
- Cost: **M** (rover camera path, a few world shots, audio, ui boot lines).

### Concept B: "The task queue" (07's CRT only)
- **0–20 s:** 07's CRT replays its own log, operational lines only:
  - `28 OCT 1988 · UNLOAD LANDER · 4/6`;
  - `STORM WARNING · ROVERS SET CONTINUE · K. MORROW`;
  - `STANDBY`;
  - then a day counter racing to `13 148 DAYS`;
  - `CARRIER DETECTED`.
- Then the lens wake as today.
- **Strengths:** cheap (**S**, ui and audio); dense with story; consistent with "people treat 07 as a tool".
- **Weaknesses:** it is text before play, against the owner's "show, don't tell" and the one-guidance spirit. It reads
  as exposition, and it shows nothing of the world.

### Concept C: "Lumen, then and now"
- **0–25 s:** a wordless establishing shot of the station alive in 1988 (lights, rovers at work), the storm, the lights
  going out one by one, dust settling on 07 in a time-lapse, then the 2024 eclipse and the wake.
- **Strengths:** the strongest emotional contrast (alive → ruins); it shows the Departure's scale.
- **Weaknesses:** it is the most expensive (**L**: an intact station in R1, which doesn't exist; only ruins do). It
  risks sentimentality and is the longest. It also shows the crew's world, which story v4 keeps for the ruins to
  tell.

### Recommendation
**Concept A, after the owner is happy with opening v2's play time, setting and first guidance** (the owner's order).

- **Soon:** Q1–Q8 (small, and Q4 is needed anyway). They likely fix most of "not smooth" on their own.
- **Re-test after the quick fixes:** if the title → play transition then reads as one continuous moment, Concept A is
  a polish item, not a fix.

## 5. Playtest signals
- From the click to the first moving image: ≤ 3 s (today ~6–7 s).
- No frame shows two representations of 07.
- After the opening, a new player can say why 07 woke ("a signal") and where to go ("the light").
- Skipping (any input after 2 s) reaches control in ≤ 2 s.

## Tóm tắt (Tiếng Việt)
- **Chẩn đoán:** chuyển từ màn tiêu đề vào game chưa mượt vì:
  - hai hình 07 khác nhau trong một giây: 07 vẽ nét 2D trên màn tiêu đề, rồi mắt 07 3D ở một thế giới khác;
  - khoảng 6–7 giây chỉ có mờ dần và màn đen sau cú bấm;
  - nhạc tiêu đề tắt dần chồng lên tiếng nhiễu của lúc thức dậy, nghe như "nhạc tắt";
  - kịch bản thức dậy vẫn nhìn vào nhà dựng sẵn, đèn tháp và dock, những thứ opening v2 đã bỏ;
  - chữ khởi động sai truyện ("EARTH SIGNAL" thay vì "ORBITAL SIGNAL");
  - nút xác nhận có thể lọt vào lúc chơi (cần kiểm tra);
  - mở đầu không cho thấy *vì sao* 07 thức.
- **Sửa nhanh, chưa cần cutscene:**
  - màn tiêu đề chính là CRT của 07, tắt kiểu đèn hình cũ (co thành vạch, thành chấm) rồi vào màn đen;
  - cho các bước chạy chồng lên nhau, để có hình động trong ≤ 3 s;
  - một nhịp chuyển tiếng duy nhất;
  - nhắm lại mọi ánh nhìn theo opening v2 (trụ sạc chết, đèn hổ phách của tàu đổ bộ, Đỉnh, Trái Đất nhật thực);
  - sửa chữ theo truyện v4;
  - Continue mở ra từ căn cứ người chơi đã dựng;
  - chặn nút xác nhận lọt vào game.
- **Học từ:**
  - Journey: menu chính là thế giới;
  - Subnautica: bước ra ngoài là khoảnh khắc kinh ngạc;
  - Outer Wilds: thức dậy ngước nhìn trời;
  - Death Stranding: nhạc là phần thưởng sau công sức;
  - Astroneer và Inside: không có khoảng chết.
- **Ba ý tưởng cutscene,** tất cả ≤ 35 s và bỏ qua được:
  - A "Sóng mang tới": camera hạ từ trời nhật thực xuống 07, ăng-ten sáng, CRT in dòng nhiệm vụ, rồi vào mắt;
  - B "Hàng đợi nhiệm vụ": chỉ chữ trên CRT;
  - C "Lumen xưa và nay": cảnh trạm năm 1988, đắt và dễ sến.
  - **Đề xuất A,** làm sau khi phần chơi mở đầu đã tốt, đúng thứ tự chủ dự án muốn. Trước mắt làm các bản sửa nhanh.
