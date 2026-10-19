# Text: databank entries, opening basin (en / vi)

> For `docs/design/databank.md`. `[PROPOSAL]` until the Director ports these to `en.json` / `vi.json`. Rules:
> `writing-style.md`:
> - readouts are functional: 07's measurements, uppercase, `·`-separated;
> - histories are voices: Lumen's catalogue cards, written by the crew before the storm. Kenji uses "mình", Ines
>   "tôi" or "chúng tôi", Ro "mình";
> - 07 appears only in the third person, nobody addresses it, and no machine has feelings;
> - no bodies and no grief: the crew card is the roster.
>
> Keys: `db.<id>.name`, `db.<id>.readout`, `db.<id>.history`. Where a key already exists, it is reused and named.
> Length budgets (databank §2): name ≤ 24; readout ≤ 2 × 48 (vi ≤ 2 × 56); history en ≤ 200, vi ≤ 240.

## Field labels
| Key | en | vi |
|---|---|---|
| `db.title` | DATABANK | DỮ LIỆU |
| `db.cat.machines` / `structures` / `places` / `phenomena` / `archive` | MACHINES / STRUCTURES / PLACES / PHENOMENA / ARCHIVE | MÁY MÓC / CÔNG TRÌNH / ĐỊA ĐIỂM / HIỆN TƯỢNG / LƯU TRỮ |
| `db.field.maker` / `year` / `era` | MAKER / YEAR / ERA | HÃNG / NĂM / THỜI KỲ |
| `db.era.p` / `s` / `b` / `l` / `m` / `n` | PROBES / SORTIES / BUILD-OUT / LUMEN / MERIDIAN / NOW | TÀU THĂM DÒ / ĐỔ BỘ NGẮN NGÀY / XÂY DỰNG / LUMEN / MERIDIAN / HIỆN TẠI |
| `db.maker.ilc` / `kessler_arai` / `vis` / `tycho` | ILC / KESSLER–ARAI / VIS / TYCHO | ILC / KESSLER–ARAI / VIS / TYCHO |
| `db.no_record` | NO LUMEN RECORD | KHÔNG CÓ HỒ SƠ LUMEN |
| `db.scan_progress` | SCAN {0}/{1} | ĐÃ QUÉT {0}/{1} |
| `db.blueprint` | BLUEPRINT · {0} | BẢN VẼ · {0} |
| `db.toast.added` | Added to databank: {0} | Đã thêm vào dữ liệu: {0} |
| `db.colour` (tooltip on the F glyph) | Full colour | Màu thật |

## Entries
| # | id · category · maker · year · era · unlock | en | vi |
|---|---|---|---|
| 1 | `unit07` · Machines · Kessler–Arai · 1983 · L · Seeded | **07**<br>`SSR-5 · S/N 3-0417 · LUMEN ASSET 07`<br>history = `notes.unit07` (07-identity §4): "SSR-5 service rover. Scans parts for the shop, cuts what's broken, builds what's new. Rebuilt this one from a spare chassis. — K." | **07**<br>`SSR-5 · S/N 3-0417 · TÀI SẢN LUMEN SỐ 07`<br>`notes.unit07`: "Xe dịch vụ SSR-5. Quét linh kiện cho xưởng, cắt chỗ hỏng, dựng đồ mới. Chiếc này lắp lại từ một khung xe dự phòng. — K." |
| 2 | `elder06` · Machines · Kessler–Arai · 1978 · B · StoryScan(`elder06`) | **06**<br>readout = `elder.06.manifest`: `SSR-3 · UNIT 06 · RELAY KEEPER · TASK: HOLD RELAY.2 LINK · STOPPED: CELL 0%`<br>Our sixth unit, an SSR-3 relay keeper. One job and no sockets: it holds a relay link, nothing else. — K. | **06**<br>`elder.06.manifest`<br>Đơn vị thứ sáu của trạm, một xe SSR-3 giữ trạm tiếp sóng. Một việc, không có ổ lắp: giữ liên lạc cho trạm, ngoài ra không làm gì. — K. |
| 3 | `elder01` · Machines · Kessler–Arai · 1978 · B · StoryScan(`elder01`), on the Peak road (1.0) | **01**<br>`SSR-3 · UNIT 01 · SURVEY CART · TASK: PEAK ROAD SURVEY · STOPPED: ROCKFALL`<br>Unit 01 came up with the first habitat crates in '84. Six years old even then, and still the steadiest climber we have. — K. | **01**<br>`SSR-3 · ĐƠN VỊ 01 · XE KHẢO SÁT · NHIỆM VỤ: KHẢO SÁT ĐƯỜNG LÊN ĐỈNH · DỪNG: ĐÁ LỞ`<br>Đơn vị 01 lên đây cùng mấy thùng mô-đun nhà ở đầu tiên năm 84. Lúc ấy nó đã sáu tuổi, vậy mà vẫn leo dốc vững nhất trạm. — K. |
| 4 | `scout3` · Machines · VIS · 1967 · P · StoryScan(`scout3`) | **reuse** `scan.scout3.name`, `scan.scout3.manifest`, `scan.scout3.note` (basin-dressing §4) | **reuse** the same keys |
| 5 | `cargo_lander` · Machines · VIS · 1987 · L · SetComplete(lander cargo) | **The Lander**<br>`CARGO LANDER · VIS CL-4 · LANDED 1987`<br>Came down in the '87 drop. Its sister landed in the canyon, harder. 07 took the last crates off this one in October '88. — K. | **Tàu đổ bộ**<br>`TÀU ĐỔ BỘ CHỞ HÀNG · VIS CL-4 · HẠ CÁNH 1987`<br>Xuống trong đợt thả hàng năm 87. Chiếc chị em của nó đáp xuống hẻm, mạnh tay hơn. Tháng Mười năm 88, 07 dỡ mấy thùng cuối cùng khỏi chiếc này. — K. |
| 6 | `kestrel3` · Machines · Tycho · 1985 · M · StoryScan(`kestrel3`) | **Kestrel-3**<br>`KESTREL-3 · RELAY SATELLITE · DOWN OCT 1988`<br>Meridian's relay. Lost its attitude and came down in our basin, still pointing at the Arks. Meridian talks through our great dish now. — Ro | **Kestrel-3**<br>`KESTREL-3 · VỆ TINH TIẾP SÓNG · RƠI 10/1988`<br>Vệ tinh tiếp sóng của Meridian. Mất định hướng rồi rơi xuống lòng chảo của tụi mình, vẫn chĩa về phía các tàu Ark. Giờ Meridian phải nói chuyện qua chảo ăng-ten lớn của tụi mình. — Ro |
| 7 | `habitat` · Structures · VIS · 1984 · L · SetComplete(HabitatModules) | **Habitat & Lab**<br>`SEC-04 · HABITAT & LAB · SEAL LOCKED · NO POWER`<br>Three VIS modules, put up by the build crew in '84, a year before we moved in: living, lab and stores. — K. | **Nhà ở & phòng thí nghiệm**<br>`SEC-04 · NHÀ Ở & PHÒNG THÍ NGHIỆM · CỬA KÍN ĐÃ KHÓA · CHƯA CÓ ĐIỆN`<br>Ba mô-đun VIS, đội xây dựng dựng lên năm 84, một năm trước khi tụi mình dọn tới: chỗ ở, phòng thí nghiệm và kho. — K. |
| 8 | `solar_array` · Structures · VIS · 1984 · L · SetComplete(FallenSolarArray) | **Fallen solar array**<br>`MAIN SOLAR ARRAY · DOWN · NO OUTPUT`<br>Lumen's main array. At the pole the Sun never climbs high, so the array turns all day to follow it round the horizon. — K. | **Giàn pin mặt trời đổ**<br>`GIÀN PIN MẶT TRỜI CHÍNH · ĐỔ · KHÔNG CÓ ĐIỆN RA`<br>Giàn pin chính của Lumen. Ở vùng cực, Mặt Trời chẳng bao giờ lên cao, nên giàn xoay suốt ngày, đuổi theo nó quanh đường chân trời. — K. |
| 9 | `radio_tower` · Structures · VIS · 1984 · L · SetComplete(RadioTower) | **Radio Tower**<br>`RADIO TOWER · LINK TO PEAK · OFFLINE`<br>Everything we send goes up this first, then on to the great dish on the Peak. On Sundays it carries my show. — Ro | **Tháp radio**<br>`THÁP RADIO · LIÊN KẾT LÊN ĐỈNH · ĐANG TẮT`<br>Mọi thứ tụi mình gửi đi đều qua tháp này trước, rồi mới lên chảo ăng-ten lớn trên Đỉnh núi. Chủ nhật thì nó phát chương trình của mình. — Ro |
| 10 | `seismic_station` · Structures · ILC · 1971 · 1984 · S · StoryScan(`seismic`) | **Seismic station**<br>`SEISMIC STATION · DEPLOYED 1971 · LUMEN BUS 1984 · LAST RECORD 28 OCT 1988 · CELL 0%`<br>Left by the '71 crew. It had thirteen years of quiet ground on record, so the ILC put Lumen here. We wired it into the station bus. — Ines | **Trạm địa chấn**<br>`TRẠM ĐỊA CHẤN · ĐẶT 1971 · NỐI ĐIỆN LUMEN 1984 · BẢN GHI CUỐI 28/10/1988 · PIN 0%`<br>Tổ bay năm 71 để lại. Nó đã ghi được mười ba năm nền đất yên ổn, nên ILC đặt Lumen ở đây. Chúng tôi nối nó vào lưới điện của trạm. — Ines |
| 11 | `retroreflector` · Structures · ILC · 1971 · 1984 · S · StoryScan(`retroreflector`) | **Retroreflector**<br>`RETROREFLECTOR · 100 PRISMS · PLACED 1971 · RE-AIMED 1984`<br>The '71 crew set it up for lasers from Earth. We turned it toward our landing approach, so landers can range on it. — Ines | **Gương phản xạ laser**<br>`GƯƠNG PHẢN XẠ · 100 LĂNG KÍNH · ĐẶT 1971 · CHỈNH HƯỚNG 1984`<br>Tổ bay năm 71 đặt nó để đón tia laser từ Trái Đất. Chúng tôi xoay nó về phía đường hạ cánh, để tàu đổ bộ đo được khoảng cách. — Ines |
| 12 | `lumen` · Places · — · 1984 · L · FragmentOf(any Lumen ruin set) | **Lumen ruins**<br>`LUMEN · ILC · SEC-04 · NO POWER`<br>Lumen. Built in '84, and four of us here from '85: Ines, Kenji, Theo and me. Call sign: three short, one long. — Ro | **Tàn tích Lumen**<br>`LUMEN · ILC · SEC-04 · CHƯA CÓ ĐIỆN`<br>Lumen. Xây năm 84, bốn đứa tụi mình ở đây từ năm 85: Ines, Kenji, Theo và mình. Hô hiệu: ba ngắn, một dài. — Ro |
| 13 | `earth_eclipse` · Phenomena · — · — · — · Seeded (07 woke in one) | **Earth eclipse**<br>`EARTH ECLIPSE · NIGHT SIDE · CITY LIGHTS: 0`<br>When Earth covers the Sun, its whole night side shows, every city lit. Ines counts them. I put a record on. — Ro | **Trái Đất che Mặt Trời**<br>`TRÁI ĐẤT CHE MẶT TRỜI · MẶT ĐÊM · ĐÈN THÀNH PHỐ: 0`<br>Khi Trái Đất che Mặt Trời, cả mặt đêm của nó hiện ra, thành phố nào cũng sáng đèn. Ines ngồi đếm. Mình thì bật một đĩa nhạc. — Ro |
| 14 | `crew` · Archive · ILC · 1985 · L · StoryScan(`airlock`) · print: the station roster sheet, typed, ILC seal, no photos | **Lumen crew**<br>readout = `story.airlock_panel` (existing)<br>I. Kalani, lead, geology. K. Morrow, mechanic, systems. T. Lindqvist, botany, galley. R. Amadi, communications. *(the roster, unsigned)* | **Phi hành đoàn Lumen**<br>`story.airlock_panel`<br>I. Kalani, trưởng nhóm, địa chất. K. Morrow, cơ khí, hệ thống. T. Lindqvist, thực vật, bếp. R. Amadi, liên lạc. |
| 15 | `card_kessler_arai` · Archive · Kessler–Arai · 1983 · L · Seeded · print: the maker's mark and an SSR-5 brochure page | **Kessler–Arai Field Systems**<br>`KESSLER–ARAI FIELD SYSTEMS · FIELD ROBOTICS · SSR SERIES`<br>They build rovers that take kit: sockets, not welds. An SSR-3 does one job. An SSR-5 does whatever you bolt on. — K. | **Kessler–Arai Field Systems**<br>`KESSLER–ARAI FIELD SYSTEMS · RÔ-BỐT HIỆN TRƯỜNG · DÒNG SSR`<br>Hãng này làm xe tự hành lắp được đồ nghề: cắm vào ổ, chứ không hàn chết. Xe SSR-3 làm một việc. SSR-5 thì lắp gì làm nấy. — K. |
| 16 | `card_ilc` · Archive · ILC · 1984 · L · StoryScan(`flag`) · print: the ILC seal and the poster "WATER IS GRAVITY" (story v4 §3.2) | **International Lunar Commission**<br>`INTERNATIONAL LUNAR COMMISSION · ILC`<br>Set up after the '69 landing to run the Moon for everyone. Now it runs it for the Arks. Our pay slips say ILC. — Ro | **Ủy ban Mặt Trăng Quốc tế**<br>`ỦY BAN MẶT TRĂNG QUỐC TẾ · ILC`<br>Lập ra sau lần đổ bộ năm 69 để quản lý Mặt Trăng cho mọi người. Giờ thì quản lý cho các tàu Ark. Phiếu lương của tụi mình ghi ILC. — Ro |
| 17 | `card_tycho` · Archive · Tycho · 1985 · M · StoryScan(`kestrel3`) · print: the teal hexagon mark and a Meridian site plan | **Tycho Consortium**<br>`TYCHO CONSORTIUM · MERIDIAN STATION · UNCREWED`<br>Tycho runs Meridian, up on the Rim Terraces. No crew, only machines. Their deep radar is the best on the Moon. — Ines | **Tycho Consortium**<br>`TYCHO CONSORTIUM · TRẠM MERIDIAN · KHÔNG NGƯỜI`<br>Tycho vận hành Meridian, trên Bậc thềm vành hố. Không có người, chỉ có máy. Radar dò sâu của họ tốt nhất trên Mặt Trăng. — Ines |
| 18 | `card_vis` · Archive · VIS · 1967 · P · SetComplete(lander cargo) · print: a flat-pack assembly sheet with the VIS stencil and its 1980s wordmark | **VIS**<br>`VANGUARD INTERPLANETARY STRUCTURES · LANDERS · MODULES`<br>VIS built our modules, our landers and, back in '67, that Scout in the crater. Twenty years of the same bolts. — K. | **VIS**<br>`VANGUARD INTERPLANETARY STRUCTURES · TÀU ĐỔ BỘ · MÔ-ĐUN`<br>VIS làm mô-đun cho tụi mình, làm cả tàu đổ bộ, và hồi 67 làm luôn con tàu Scout dưới hố kia. Hai mươi năm vẫn một kiểu bu-lông. — K. |

## Notes
- `CL-4` (the cargo lander's model), Kestrel-3's 1985 launch year, and Unit 01's Peak-road task are new
  `[PROPOSAL]` facts.
- Ro's eclipse card (13) is the only line that sets 1988 against the player's present, and it does so with a
  measurement (`CITY LIGHTS: 0`), never a comment.
- The 1971 sample rack, geophone line and survey tripod get no entries. They stay quiet texture
  (basin-dressing §2).
