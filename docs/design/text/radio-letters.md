# Text: the radio's fragments and Ro's letters

> Author: the story & flow session, for ui to port into the localization tables. English is the source; Vietnamese is
> natural, not word for word. Design: `docs/design/game-flow.md` §5 ("the masts hold the letters") and §7 (after the
> ending). They show as ticker lines (`TickerLine`), one at a time, never during a cinematic moment.

## How they are released
- **Tower L1:** the station's own automated weather loop, once, as the radio clears (cutscenes §2).
- **Each lit mast** plays back its share of the letters in **time order**, whichever mast it is, so the story always
  reads in order. The masts listened while the station was dark; only the great dish can talk back.
  - 0.5's four basin masts carry the official messages, which answer "did they try to come back?" (yes, and then the
    program closed).
  - From the fifth mast on (0.6+), Ro's own letters arrive, which answer "did anyone keep listening?".
  - Proposed for gameplay: letters released = 1 per mast for the first four, then 2 per mast; `letter.mast_<n>` in
    order.
- **The ending:** Earth's answer (cutscenes F8).
- **After the ending:** one new letter on the ticker per later session, ten in all, then none. Never time-limited.

Pronouns as in `crew-logs.md`: Ro writes "mình", calls 07 "cậu", the crew "tụi mình". The relay speaks formally.

## The weather loop (tower L1)
| Key | English | Tiếng Việt |
|---|---|---|
| `ticker.radio.weather` | …weather tonight: clear skies over the Sea of Tranquility, as always… | …thời tiết đêm nay: trời quang trên Biển Tĩnh Lặng, như mọi khi… |

## The masts' letters, in time order
| Key | Released by | English | Tiếng Việt |
|---|---|---|---|
| `letter.mast_1` | 1st mast (0.5) | Lumen Station, this is Houston relay. All four crew landed safely. Program review postponed. Stand by. | Lumen Station, đây là trạm tiếp sóng Houston. Cả bốn thành viên đã hạ cánh an toàn. Việc xem xét chương trình được hoãn lại. Xin chờ. |
| `letter.mast_2` | 2nd mast (0.5) | Lumen Station, this is Houston relay. A return window is under study. Keep station systems on standby. | Lumen Station, đây là trạm tiếp sóng Houston. Đang xem xét một đợt quay lại. Giữ các hệ thống của trạm ở chế độ chờ. |
| `letter.mast_3` | 3rd mast (0.5) | Lumen Station, this is Houston relay. Return window cancelled. Program review continues. | Lumen Station, đây là trạm tiếp sóng Houston. Đợt quay lại đã bị huỷ. Việc xem xét chương trình vẫn tiếp tục. |
| `letter.mast_4` | 4th mast (0.5) | Lumen Station, this is Houston relay. Program closed. This channel will no longer be monitored. Thank you, Lumen. | Lumen Station, đây là trạm tiếp sóng Houston. Chương trình đã khép lại. Kênh này sẽ không còn ai theo dõi. Cảm ơn, Lumen. |
| `letter.mast_5` | 5th mast (0.6, the terraces) | 07, it's Ro. They say nobody's listening up there. I'm sending this anyway. — Ro | 07, Ro đây. Người ta bảo trên đó chẳng còn ai nghe. Mình vẫn cứ gửi. — Ro |
| `letter.mast_6` | 5th mast | Ines took a desk job. She still walks round the block every morning to check Earth is still there. — Ro | Ines nhận một công việc bàn giấy. Sáng nào chị ấy vẫn đi một vòng quanh khu phố, để chắc rằng Trái Đất vẫn còn đó. — Ro |
| `letter.mast_7` | 6th mast (0.7, the crater) | Theo says the basil on my windowsill isn't as good as yours was. Don't tell him I agree. — Ro | Theo bảo chậu húng quế trên bậu cửa sổ nhà mình không ngon bằng chậu trên đó. Đừng nói với anh ấy là mình cũng thấy vậy nhé. — Ro |
| `letter.mast_8` | 6th mast | Kenji fixed my kettle with a paperclip and a long speech about tolerances. He says hello to the duck. — Ro | Kenji sửa ấm đun nước cho mình bằng một cái kẹp giấy và một bài giảng dài về dung sai. Anh ấy gửi lời chào chú vịt. — Ro |
| `letter.mast_9` | 7th mast (0.8, the Peak's foot) | It's been twenty years. I still play the long-drive tape on Sundays. — Ro | Hai mươi năm rồi. Chủ nhật nào mình cũng vẫn mở cuộn băng cho chuyến đi dài. — Ro |
| `letter.mast_10` | 7th mast | If that old dish ever wakes up, I'll be listening on the station's frequency. You know how to find me: three short, one long. — Ro | Nếu cái chảo cũ ấy có ngày thức dậy, mình sẽ nghe trên tần số của trạm. Cậu biết cách tìm mình mà: ba ngắn, một dài. — Ro |

`letter.mast_1`'s "all four crew landed safely" is deliberate. It settles the darkest question early, so the mystery
stays bittersweet, never grim (STORY tone; ruling: never guilt).

## The ending (cutscenes F8)
| Key | English | Tiếng Việt |
|---|---|---|
| `ticker.ending.answer` | I heard you, 07. I'm still here. — Ro | Mình nghe thấy rồi, 07. Mình vẫn còn đây. — Ro |

## After the ending: one per later session
| Key | English | Tiếng Việt |
|---|---|---|
| `letter.after_1` | Told the others. Theo cried, then blamed hay fever. — Ro | Mình kể cho mọi người rồi. Theo khóc, xong đổ tại dị ứng phấn hoa. — Ro |
| `letter.after_2` | Ines went out at midnight to look at the Moon. She says it's still there. — Ro | Nửa đêm Ines ra ngoài ngắm Mặt Trăng. Chị ấy bảo nó vẫn còn đó. — Ro |
| `letter.after_3` | Kenji asked if you still have the odd wheel. I told him all six match now, and he had to sit down. — Ro | Kenji hỏi cậu còn cái bánh lạc loài không. Mình bảo giờ cả sáu bánh đã giống nhau rồi, thế là anh ấy phải ngồi xuống. — Ro |
| `letter.after_4` | Played your song on my show tonight. Audience: two neighbours and a very old cat. Best audience since you. — Ro | Tối nay mình phát bài của cậu trong chương trình. Khán giả: hai người hàng xóm và một con mèo đã rất già. Khán giả tuyệt nhất kể từ cậu. — Ro |
| `letter.after_5` | I told Theo about the dome. He's been unbearable all week. — Ro | Mình kể cho Theo nghe về mái vòm. Cả tuần nay anh ấy vênh váo không ai chịu nổi. — Ro |
| `letter.after_6` | Weather tonight: rain over Lagos. It's always clear where you are, isn't it? — Ro | Thời tiết đêm nay: mưa ở Lagos. Chỗ cậu thì lúc nào trời cũng quang, phải không? — Ro |
| `letter.after_7` | Ines's daughter wants to be an astronaut. Ines is pretending to be worried. — Ro | Con gái Ines muốn làm phi hành gia. Ines đang giả vờ lo lắng. — Ro |
| `letter.after_8` | The four of us wrote to the agency together. Don't hold your breath; you don't breathe anyway. — Ro | Bốn đứa tụi mình cùng viết thư cho cơ quan vũ trụ. Đừng nín thở chờ nhé; mà cậu có thở đâu. — Ro |
| `letter.after_9` | Kenji's grandson won a rubber duck at the fair. Kenji says it's a sign. — Ro | Cháu trai Kenji thắng được một chú vịt cao su ở hội chợ. Kenji bảo đó là điềm báo. — Ro |
| `letter.after_10` | Goodnight, 07. Keep the radio on. We'll keep ours on too. — Ro | Ngủ ngon, 07. Cứ để radio bật nhé. Tụi mình cũng sẽ để radio bật. — Ro |

**Callbacks the after-letters lean on:**
- `after_3`: the Magnetic Treads' matched wheels (0.6);
- `after_5`: the biodome (0.7);
- `after_6`: Ro's Lagos postcard relic and the weather loop;
- `after_9`: the duck.

All of these exist before the ending, so no letter refers to something the player hasn't seen.
