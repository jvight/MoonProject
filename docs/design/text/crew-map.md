# Text: Ines's names and notes on the crew's map (M3-07)

> Author: the story & flow session, for ui to port into the localization tables. English is the source; Vietnamese is
> natural, not word for word. Spec: `docs/features/M3-07-crew-map.md`.
> - Names (`map.<placeId>.name`) are written in Ines's hand when the chart pen inks a place.
> - Notes (`map.<placeId>.note`) are her pencil margin notes: one short line each, dry and precise, secretly fond.
> - Ines writes "tôi". She calls Tilly "con bé", and Ro "cô ấy".

## The sheet itself
| Key | English | Tiếng Việt |
|---|---|---|
| `map.sheet.title` | Lumen basin, surveyed on foot. — I. Kalani | Lòng chảo Lumen, khảo sát bằng đôi chân. — I. Kalani |
| `map.tally.label` | Checked: | Đã kiểm tra: |
| `map.complete.note` | Checked: the Moon is still there. ✓ — I. | Đã kiểm tra: Mặt Trăng vẫn còn đó. ✓ — I. |

The tally counts found places in pencil strokes after the label, with no total (M3-07 amendment 6).
If Be Vietnam Pro has no ✓ (U+2713), the art pencils the tick instead of the font.

## Places in the basin (0.5)
| Place id | Name (EN) | Note (EN) | Name (VI) | Note (VI) |
|---|---|---|---|---|
| `place.lander` | Home | Population: four, one rover, one very proud basil. | Nhà | Dân số: bốn người, một rover, một chậu húng quế rất tự hào. |
| `site.depot` | The depot | Spare everything. Mostly bolts. | Kho tiếp tế | Đồ dự phòng đủ cả. Chủ yếu là bu-lông. |
| `site.kestrel` | Kestrel-3 | Poor thing. It came down still pointing at Earth. | Kestrel-3 | Tội nghiệp. Lúc rơi xuống nó vẫn chĩa về Trái Đất. |
| `trail.kestrel` | Kestrel's skid | I paced it. Twice. | Vệt trượt của Kestrel | Tôi đã đi bộ đo nó. Hai lần. |
| `site.drill` | My drill | Core 41 came from here. The blue one. | Máy khoan của tôi | Mẫu lõi 41 lấy ở đây. Viên màu xanh. |
| `site.garage` | Kenji's garage | No entry without Kenji. Kenji: no entry without a helmet. | Gara của Kenji | Không vào nếu không có Kenji. Kenji: không vào nếu không đội mũ. |
| `site.lander` | The supply lander | Brought Theo's seed trays. Landed… firmly. | Tàu tiếp tế | Chở khay hạt giống của Theo. Hạ cánh hơi… mạnh tay. |
| `friend.tilly` | Tilly's crater | Where she sulks when I walk too fast. | Hố của Tilly | Chỗ con bé dỗi mỗi khi tôi đi nhanh quá. |
| `relay.0` | Mound mast | Ro calls it the little one. | Cột trên gò | Ro gọi nó là "bé út". |
| `relay.1` | Shoulder mast | Longest shadows in the basin. | Cột vai núi | Bóng đổ dài nhất lòng chảo. |
| `relay.2` | Canyon-mouth mast | Everything echoes here. Even the static. | Cột cửa hẻm | Ở đây cái gì cũng vang. Cả tiếng rè. |
| `relay.3` | Ledge mast | Right above Ro's chamber. Of course. | Cột trên gờ đá | Ngay trên căn phòng của Ro. Đương nhiên rồi. |
| `canyon.mouth` | Whispering Canyon | Say something. It says it back, quieter. | Hẻm Thì Thầm | Nói gì đó đi. Nó sẽ nói lại, nhỏ hơn. |
| `canyon.lip` | The chasm | Too wide to cross. Kenji says "for now". | Vực sâu | Quá rộng để qua. Kenji bảo "tạm thời thôi". |
| `canyon.landing` | The far side | Someone always leaves a light on over here. | Bờ bên kia | Bên này lúc nào cũng có người để đèn. |
| `canyon.ledge` | Ro's ledge | She comes here to hear herself sing back. | Gờ đá của Ro | Cô ấy ra đây để nghe tiếng mình hát vọng lại. |
| `canyon.terminus` | Ro's chamber | Best acoustics on the Moon. Her words, not mine. | Căn phòng của Ro | Âm thanh hay nhất Mặt Trăng. Lời cô ấy, không phải của tôi. |
| `canyon.exit` | The way out | One way only. Don't try it upwards. | Lối ra | Chỉ đi một chiều. Đừng thử leo ngược lên. |

- Bell was found at `canyon.terminus`. If the sheet keeps `friend.bell` as its own place, give it the terminus lines.
- Home's other places (`place.dock`, `place.tower`, …) sit inside the "Home" mark at the basin's scale. If ui shows
  them, the existing `place.<id>.name` keys serve.

## Beyond the rim: pencil only (M3-07 step 3)
Region ids are proposed. They need anchors in `WorldAnchorIds` (Director) before ui can place them.

| Place id | Ships | Name (EN) | Note (EN) | Name (VI) | Note (VI) |
|---|---|---|---|---|---|
| `region.peak` | 0.5 | The Peak | The dish. Kenji's job, after the storm. | Đỉnh núi | Chảo ăng-ten. Việc của Kenji, sau cơn bão. |
| `region.terraces` | 0.5 | The terraces | Too steep for wheels. Atlas parked up here? | Bậc thềm vành núi | Quá dốc cho bánh xe. Atlas đỗ ở trên này à? |
| `region.crater` | 0.6+ | Theo's crater | Theo wants a garden down there. It's dark. Bring a lamp. | Hố của Theo | Theo muốn làm vườn dưới đó. Tối lắm. Mang đèn theo. |
| `region.hollow` | 0.6 (backlog) | Kestrel's hollow | Something glints at the back. Too dark to tell. | Hốc tối cạnh Kestrel | Có gì đó lấp lánh tít trong. Tối quá, không nhìn rõ. |

Only the 0.5 rows show in 0.5. The crater appears once it can be seen from the terraces, and the hollow when it ships
(M3-07 amendment 1).

## If the level review's L2, L4 and L7 land
| Place id (proposed) | Name (EN) | Note (EN) | Name (VI) | Note (VI) |
|---|---|---|---|---|
| `route.crew_road` (L2) | The road to the canyon | Follow the posts. Kenji planted them, crooked. | Đường ra hẻm | Cứ theo mấy cây cọc. Kenji cắm đấy, xiêu xiêu vẹo vẹo. |
| `canyon.bridge` (L4) | Kenji's bridge | It holds. Mostly. | Cầu của Kenji | Vẫn trụ được. Phần lớn là vậy. |
| `site.antennas` (L7) | Ro's antennas | They hum in a solar storm. Ro calls it the choir. | Rừng ăng-ten của Ro | Gặp bão mặt trời là chúng ngân lên. Ro gọi đó là dàn đồng ca. |

## The card's state line
M3-07's card adds "what is still there". These neutral lines are the station's, not Ines's.

| Key | English | Tiếng Việt |
|---|---|---|
| `map.state.heart_waiting` | Something still waits in its heart. | Trong lòng nó vẫn còn thứ gì đang chờ. |
| `map.state.picked_clean` | Picked clean. | Đã dọn sạch. |
| `map.state.mast_dark` | Dark. Waiting to be lit. | Còn tối. Đang chờ được thắp. |
| `map.state.mast_lit` | Lit. Home reaches this far. | Đã sáng. Nhà vươn tới tận đây. |
