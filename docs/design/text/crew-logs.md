# Text: the crew's logs (M3-01)

> Author: the story & flow session, for ui to port into `Data/Localization/en.json` and `vi.json`. English is the
> source. Vietnamese is written to sound natural, not word for word. Voice rules: STORY "Writing rules" (one or two
> sentences, a concrete detail, humour next to sadness, never guilt).
>
> Pronouns in Vietnamese, kept the same as today's tables: Ro, Theo and Kenji write "mình" (casual); Ines, the
> mission lead, writes "tôi". Every crew member calls 07 "cậu". Others refer to Ines as "chị ấy" and to Theo and
> Kenji as "anh ấy".

## The set: 12 logs, three per crew member
- **First log:** who they were, found in 0.5.
- **Second log:** the storm and the leaving, found with their region (0.6–0.7).
- **Third log:** the reward for completing that crew member's set of memories. Their bunk window lights, and the log
  plays on the radio as a ticker line (M3-09).

Already in the tables (unchanged):
- `log.ro_1`, Ro's first log;
- the friends' repair cards `friend.tilly.repair_log` (Ines) and `friend.bell.repair_log` (Ro). These cards are not
  among the 12.

**One deviation from STORY.** STORY's beat for Ines's first log is "the morning rim walk", but Tilly's repair card
already tells it ("Walked the rim at sunrise with Tilly…"). Ines's first log tells her other side instead: the
precise geologist who is secretly sentimental. It also sets up her relic "a sample jar with a single blue pebble".

## The four first logs (0.5)
| Key | Found at | How | STORY beat |
|---|---|---|---|
| `log.ro_1` | `canyon.terminus` | log cache tin beside Bell (built) | the first night show |
| `log.kenji_1` | `site.garage` | a log cache tin in Kenji's garage ruins, beside the heart | fixing 07 the first time; the call sign (VISION, on main) |
| `log.ines_1` | `site.drill` | a log cache tin at the foot of her tipped survey drill, near her boot | the geologist (see the deviation above) |
| `log.theo_1` | `site.lander` | a log cache tin in the canyon supply lander, beside his teapot | naming the seedlings |

**Placement change from `game-flow.md` §5.** That section put Theo's first log in the dark hollow, which the
Director has moved to 0.6. It now lies in the canyon supply lander, which carried Theo's seed trays and holds his
teapot. game-flow §5 is updated in the same commit.

**What other boxes need for 0.5:** three `LogCache` assets (gameplay data, like `LogCache_ro_1`), anchored at
`site.garage`, `site.drill` and `site.lander`; art's existing log tin reused.

| Key | English | Tiếng Việt |
|---|---|---|
| `log.kenji_1` | Fixed the old survey rover today, the seventh thing on this week's list, so it's 07 now. I taught its antenna our call sign, three short, one long, and it blinks it at me whenever I walk past. — Kenji | Hôm nay sửa xong chiếc rover khảo sát cũ, món thứ bảy trong danh sách tuần này, nên giờ nó tên là 07. Mình dạy cái ăng-ten của nó tín hiệu gọi của trạm, ba ngắn một dài, và cứ thấy mình đi ngang là nó nháy liền. — Kenji |
| `log.ines_1` | Core sample 41: grey dust, grey dust, one blue pebble. Logged it as "anomalous". Kept it as "mine". — Ines | Mẫu lõi số 41: bụi xám, bụi xám, một viên sỏi xanh. Ghi vào sổ là "bất thường". Giữ lại làm "của tôi". — Ines |
| `log.theo_1` | Named every seedling in tray four; the mint is Gerald. Gerald is the best mint on the Moon, which is a low bar, but he's clearing it. — Theo | Đặt tên cho hết cây con ở khay số bốn; cây bạc hà tên là Gerald. Gerald là cây bạc hà ngon nhất trên Mặt Trăng; tiêu chuẩn thì thấp thật, nhưng cậu chàng vượt qua ngon lành. — Theo |

## The second logs (0.6–0.7)
| Key | Version | Found at (proposed) | STORY beat |
|---|---|---|---|
| `log.ro_2` | 0.6 | the crew's antenna farm (level review L7), Ro's outside mic | the storm warning read on air |
| `log.ines_2` | 0.6 | the Rim Terraces, by Atlas's resting place | choosing to evacuate |
| `log.kenji_2` | 0.6 | the crawler wreck at the terraces' foot, with the Magnetic Treads tube (game-flow S7) | the spare wheel he hid for "next time". The Treads give 07 six matching wheels: Kenji's "next time", kept. |
| `log.theo_2` | 0.7 | the Shadowed Crater, beside Moss | packing in a hurry, leaving Moss to water them |

| Key | English | Tiếng Việt |
|---|---|---|
| `log.ro_2` | Read the storm warning on air tonight, tucked between two slow songs so it wouldn't sound so scary. 07 rolled right up to the speaker and stayed there. — Ro | Tối nay đọc cảnh báo bão trên sóng, kẹp giữa hai bài chậm cho nghe đỡ đáng sợ. 07 lăn sát tới cái loa rồi đứng yên ở đó luôn. — Ro |
| `log.ines_2` | My call as mission lead: all four of us leave on the morning shuttle. It's the right call, and I've never hated being right so much. — Ines | Quyết định của tôi, với tư cách trưởng nhóm: cả bốn người sẽ đi chuyến tàu sáng mai. Quyết định đúng, và chưa bao giờ tôi ghét việc mình đúng đến thế. — Ines |
| `log.kenji_2` | Hid a spare wheel for 07 under the tarp in the crawler. Next time I'll give it six matching ones, not just patch the odd one out. — Kenji | Giấu một cái bánh dự phòng cho 07 dưới tấm bạt trong xe bánh xích. Lần tới mình sẽ thay cho nó đủ sáu bánh giống nhau, chứ không chỉ vá cái bánh lạc loài. — Kenji |
| `log.theo_2` | Ten minutes to pack. I left Moss the watering schedule, and Moss hummed at me as if I'd insulted the tomatoes. — Theo | Có mười phút để thu dọn. Mình để lại lịch tưới cho Moss, và Moss ngân nga với mình như thể mình vừa xúc phạm mấy cây cà chua. — Theo |

## The third logs (a completed set)
They play on the radio when the crew member's set is complete (M3-09 `CrewSetCompleted`). Ro's is her last show
before the shuttle. The others read as messages the station kept. Expected order: Ines (0.6), Theo and Kenji (0.7),
Ro (0.8, just before the broadcast).

| Key | English | Tiếng Việt |
|---|---|---|
| `log.ro_3` | Last show before the shuttle. I left the radio on for you, 07. Keep it playing, and we'll find you by the music. — Ro | Buổi phát sóng cuối trước chuyến tàu. Mình để radio bật cho cậu đó, 07. Cứ để nhạc chạy nhé, rồi tụi mình sẽ lần theo tiếng nhạc mà tìm cậu. — Ro |
| `log.ines_3` | We were coming back, 07. I need you to know we tried. — Ines | Chúng tôi đã định quay lại, 07. Tôi cần cậu biết rằng chúng tôi đã cố. — Ines |
| `log.theo_3` | Left the last tray by the window. If anything grows, tell me. — Theo | Để khay cuối cùng cạnh cửa sổ. Nếu có gì mọc lên, nhớ kể mình nghe. — Theo |
| `log.kenji_3` | They asked me what to do with the rover, and I said leave it switched on. Don't let anyone retire you, 07. — Kenji | Người ta hỏi nên làm gì với chiếc rover, mình bảo cứ để nó bật. Đừng để ai cho cậu nghỉ hưu nhé, 07. — Kenji |

## Notes for ui
- The em dash and signature are part of the text, as in `log.ro_1`.
- Keys follow `log.<crew>_<n>`; crew ids are `ro`, `theo`, `ines`, `kenji`.
- Lines run to 25–35 words in English. Vietnamese runs about 20–30% longer, so the card must wrap three lines
  without shrinking the type.
