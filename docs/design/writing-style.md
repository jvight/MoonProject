# Writing style: every player-facing string, en and vi

> Owner: the story & flow session, for every box that adds a localization key. It expands `docs/design/feel-pass.md`
> §8.1. The source is `Assets/_Project/Data/Localization/en.json`; `vi.json` is written alongside it, never machine
> translated.

## 1. Two kinds of text
| Kind | Where | How it reads |
|---|---|---|
| **Functional** | next-step lines, place cards, recipe and upgrade cards, prompts, "not yet" lines, battery hints, readouts, settings, menus | plain: **what it is, what to do, why**, in that order. Short. No metaphor. |
| **Voices** | Ro's letters and recordings, the radio, the crew's logs and keepsake memories, Ines's map notes, Bell's lines | warm, personal, allowed to be poetic. Only here. |

If a line tells the player what to do, it is functional, even when a character could say it.

## 2. Rules for functional text
1. **Use the player's verb:** "Repair the radio tower" / "Sửa tháp radio". Never "Wake the old mast" / "Đánh thức cột
   anten".
2. **Machines are not people.** Only 07 and the friends (Tilly, Bell, Atlas, Moss) have feelings.
   - Never write: asleep, tired, wakes, waits, listens, sings (ngủ, mỏi mệt, thức dậy, chờ, lắng nghe, ngân nga).
   - Instead write: offline, no power, online, has power, needs (đang tắt, chưa có điện, đang hoạt động, đã có điện,
     cần).
3. **Say what is missing and where to get it:** "Needs a transformer. Look in Kenji's Garage." / "Cần một bộ biến áp.
   Tìm ở Gara của Kenji."
4. **One name per thing,** exactly as in the glossary below, in both languages and in every key.
5. **Never name a key or button in text.** The prompt glyph shows it.
6. **Never name a place the player hasn't found.** Hint lines name home, or only chargers already lit.
7. **Recipe rows show the need first, then what you have:** "Kim loại 1 (có 5) ✓", never "5/1".
8. **Length:** a next-step line is at most two short sentences. A card's effect line is one sentence.

## 3. Vietnamese
- **Write it natively,** as a well-made Vietnamese game would. Rewrite the sentence; don't translate word for word.
- **Address the player as "bạn"** only when needed; prefer imperative lines without a pronoun ("Hãy sửa trạm.").
- **Short particles are fine** ("rồi", "nhé" in voices). Functional lines use "Hãy" or a plain verb.
- **Voices keep their pronouns:**
  - Ro, Theo and Kenji write "mình" and call 07 "cậu";
  - Ines writes "tôi";
  - friends are "cô ấy", and Tilly is "cô bé".
- **Numbers and units:** "120 m", "cấp 2", "6 ô pin". Dates in letters are dd/mm/yyyy.

## 4. Glossary (canonical names)
### Places
| en | vi |
|---|---|
| Home (wherever the player is: hop list, meter, hints) | Nhà |
| Lumen Station (only in letters' call sign) | Lumen Station |
| Charging Dock (short: the Dock) | Đế sạc |
| Radio Tower | Tháp radio |
| The Lander | Tàu đổ bộ |
| Crew's Lab (short: the Lab) | Phòng thí nghiệm |
| Crew's Lift (short: the Lift) | Thang nâng |
| Kenji's Rover Bay | Xưởng xe của Kenji (short: xưởng xe) |
| turntable / pad / hopper | bàn xoay / bệ / phễu |
| Tape Rack | Kệ băng |
| Kenji's Garage Port | Cổng bảo trì ở gara |
| Supply Depot | Kho tiếp tế |
| Kenji's Garage | Gara của Kenji |
| Drill Rig | Giàn khoan |
| Kestrel-3 | Kestrel-3 |
| Canyon Supply Lander | Tàu tiếp tế trong hẻm |
| Whispering Canyon | Hẻm Thì Thầm |
| the chasm | vực |
| Rim Terraces | Bậc thềm vành hố |
| the Peak | Đỉnh núi |
| the basin | lòng chảo |
| Relay, by name only, never a number: Mound Relay, Shoulder Relay, Canyon Mouth Relay, Terminus Relay | trạm tiếp sóng: Trạm trên gò, Trạm vai vành hố, Trạm miệng hẻm, Trạm cuối hẻm |
| the great dish | chảo ăng-ten lớn |

### Things
| en | vi |
|---|---|
| Metal / Wiring / Optics | Kim loại / Dây điện / Thấu kính |
| component; Tower Coil (always this name), transformer, relay spare part | linh kiện; cuộn dây tháp, bộ biến áp, linh kiện trạm tiếp sóng |
| blueprint; the reader | bản vẽ; máy đọc |
| keepsake | kỷ vật |
| crew log; repair log | nhật ký phi hành đoàn; nhật ký sửa chữa |
| tape | băng |
| battery, cell | pin, ô pin |
| tether | dây kéo |
| sonar, ping, pillar | sonar, dò sóng, cột sáng |

### Abilities and kit
| en | vi |
|---|---|
| Hover-Jump | Bật nhảy |
| Cargo Cradle | Giỏ chở đồ |
| Warm Headlamp | Đèn pha ấm |
| Boost Coils (one name; "capacitor drums" is only the part's look) | Bộ tăng tốc |
| Cell Pack II | Bộ pin II |

### States and verbs
| en | vi |
|---|---|
| offline / online / no power / has power | đang tắt / đang hoạt động / chưa có điện / đã có điện |
| repair; build level N (the tower card); Radio Tower level N powers X | sửa; xây cấp N; tháp radio cấp N cấp điện cho X |
| salvage, cut, dig, tow, place, stow, reel in | trục vớt, cắt, đào, kéo, đặt, cất vào giỏ, thu dây |
| park on, feed, build, fit | đỗ lên, nạp, chế tạo, lắp |
| charge | sạc |

## 5. Adding a key
1. Write the en line from §2, using the glossary's words.
2. Write the vi line natively.
3. Read both aloud. If a Vietnamese player would ask "là sao?", rewrite it.
