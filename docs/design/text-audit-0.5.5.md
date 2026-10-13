# Text audit for 0.5.5: every string the player sees while playing

> Author: the story & flow session, for the Director, the ui and gameplay sessions, and game design's 0.5.5 spec.
> Source: `Data/Localization/en.json` and `vi.json` on main (499 keys). Triggers were read from the code (read only).
> Rules applied:
> - `docs/design/writing-style.md` and the owner's "plain functional text";
> - **at most one guidance element on screen at a time**;
> - **a hint only when the player needs it**;
> - **never repeat** what another element or the world already shows.
>
> The owner's verdict on 0.5.4: "showing more isn't better, and more AND unfitting is infuriating and confusing."

## 1. What went wrong, in two examples
**At game start**, `place.dock.job` ("Đỗ vào đây để sạc pin. Sạc xong là game được lưu.") shows with a full battery.
`PlaceApproaches.Tick` tells every place 07 is already inside, so the dock's card fires right after the wake. The hint
is answered before the player has the problem.

**At the tower**, four elements say the same thing at once:

| Element | Code | Text (vi) |
|---|---|---|
| next line | `NextStepLine` ← `StepFlow` step `next.salvage` | "Tháp cần kim loại, dây điện, thấu kính và cuộn dây tháp. Hãy trục vớt ở Kho tiếp tế." |
| recipe card | `RecipeCard` (07's view rests on the tower's hopper) | the rows, plus `ui.recipe.need` "Cần trục vớt thêm 1 Kim loại", plus `component.tower_coil.needed` "Cần cuộn dây tháp. Nó ở Kho tiếp tế.", plus `ui.recipecard.tower` "Đỗ lên bệ của tháp rồi nạp vật liệu vào phễu." |
| ticker | `UISystem.OnStationNotYet` ← sonar answer (`notyet.tower`) | "Đang tắt. Cần cuộn dây tháp và vật liệu: xem thẻ của tháp." |
| prompt | the hint chip | the E glyph |

## 2. The rule the code should enforce (proposal for ui + gameplay)
One **guidance slot**, by priority. A lower element **waits** (never stacks) while a higher one shows:
1. a station's panel or recipe card (07 is at the tower, the bay, a socket);
2. a contextual prompt (glyph + verb) for an action available **now**;
3. the next line;
4. ticker notices (not-yet, relay, battery, cargo).

Plus:
- **The next line never names what the visible card already lists.** When the tower's card is up, the next line hides.
- **A not-yet notice never repeats the next line's need.** If the current step is the tower, the tower answers the
  sonar with its plaque light and a tone, not words.
- **Place cards show the name only**, and only once. The action is taught by the next line at the moment the
  player needs it. Exceptions below give a job line where nothing else explains the place.
- **No hint fires for a problem the player doesn't have yet:** the dock only when the battery is below full, the
  tether only when the player tries to tow.
- **No key names in text.** The glyph chip shows the key.

## 3. The audit
Verdicts:
- **CUT**: remove the string, and the code path that shows it.
- **MERGE → X**: its content moves into X.
- **KEEP**: as is.
- **SHORTEN**: new text given.
- **SHOW INSTEAD**: a lamp, an icon or a chirp replaces the words.

### 3.1 Guidance: next lines (one line, top centre; `NextStepLine` from `BaseProgression` steps)
| Key | en / vi now | When | Verdict | New text (en / vi) |
|---|---|---|---|---|
| `next.wake` | First job: repair the radio tower. / Việc đầu tiên: sửa tháp radio. | step 0, after the opening, when still | **SHORTEN** | Repair the radio tower. / Sửa tháp radio. |
| `next.salvage` | The tower needs metal, wiring, optics and the Tower Coil. Salvage them at the Supply Depot. / Tháp cần … Kho tiếp tế. | step 1, until the coil is out | **SHORTEN**, and hide while the tower card is up | Salvage at the Supply Depot. / Trục vớt ở Kho tiếp tế. |
| `next.tower.part` | … Drive close and press E to pick it up. / … nhấn E để nhặt. | the coil is still in the depot | **SHORTEN** (no key names; the prompt shows E) | Pick up the Tower Coil at the depot. / Nhặt cuộn dây tháp ở kho. |
| `next.tower.pad` | Bring the Tower Coil to the radio tower and park on its pad. / Mang cuộn dây tháp về tháp radio và đỗ lên bệ. | the coil rides on 07 | **SHORTEN** | Bring the coil to the tower's pad. / Mang cuộn dây về bệ tháp. |
| `next.tower.hold` | Hold E on the pad to repair the radio tower. / Giữ E trên bệ để sửa tháp radio. | the coil is in, on the pad | **CUT** | the panel's hold ring and the prompt already say it (priority 1 and 2) |
| `next.signal` | A faint signal past the dune. Ping, then follow it. / … | Tilly's step | **SHORTEN** | A faint signal. Ping and follow it. / Có tín hiệu yếu. Dò sóng rồi lần theo. |
| `next.dig` | The last part is under the dust. Dig it up. / … | Tilly's parts | **KEEP** | |
| `next.friend.repair` | All her parts are here. Repair her. / Đã đủ bộ phận. Hãy sửa cho cô ấy. | all parts gathered | **SHORTEN** | Repair Tilly. / Sửa cho Tilly. |
| `next.garage` | Go to Kenji's Garage. A spare part for 07 may still be there. / … | after Tilly | **SHORTEN** | Go to Kenji's Garage. / Đến Gara của Kenji. |
| `next.port` | The garage's service port works. Park on its pad to fit the spare. / … | the port is open | **SHORTEN** | Park on the port's pad. / Đỗ lên bệ cổng bảo trì. |
| `next.relic` | Dig up the keepsake and tow it home. / … | the first relic | **KEEP** | |
| `next.lift.stitch` | The lift is stuck. Repair its cable. / … | the lift is jammed | **KEEP** | |
| `next.lift.ride` | Park on the lift's platform to ride up to the lab. / … | the lift is ready | **KEEP** | |
| `next.chasm` | A light across the chasm. You can't cross it yet. / … | the chasm step | **KEEP** | |
| `next.bay.wake` | The bay has no power. Radio Tower level 2 powers it. / … | before L2 | **SHORTEN** | Build tower level 2 to power the bay. / Xây tháp cấp 2 để có điện cho xưởng xe. |
| `next.bay` | The bay has power. Park on its turntable to build a kit. / … | the bay has power | **SHORTEN** | Park on the bay's turntable to build. / Đỗ lên bàn xoay ở xưởng xe để chế tạo. |
| `next.power.part` / `.carried` / `.build` | the tower's next level: part, carried, feed | recovery needs (after a long time without progress) | **KEEP**, hidden while the tower card is up | |
| `next.link.relay.N`, `next.link` | Repair the offline relay(s) | recovery needs | **KEEP** | |

### 3.2 Station cards and panels (`RecipeCard`, `TowerPanel`, `RewardCard`)
| Key | Now (vi) | When | Verdict | New text / note |
|---|---|---|---|---|
| `upgrade.radio_tower.N.title` | Xây cấp N | card and panel header | **KEEP** | |
| `upgrade.*.effect` (tower, kits) | one line of what it does | card and panel | **KEEP** | |
| `ui.station.offline` | đang tắt | tower header before L1 | **KEEP** | |
| `ui.recipecard.needs` | CẦN | card | **KEEP** | |
| `ui.recipe.row` | {0} (có {1}) | each material row | **KEEP** | one row per need, the Tower Coil included |
| `ui.recipe.need` | Cần trục vớt thêm {0} {1} | under the rows | **CUT** | the rows already show need and have; the dimmed row is the hint |
| `component.tower_coil.needed`, `.transformer.needed`, `.relay_spare.needed` | Cần … Nó ở … | card, under the rows | **MERGE → the part's row** | the row reads "Cuộn dây tháp (chưa có) · Kho tiếp tế" in small type. Source names: Supply Depot / Kho tiếp tế; Kenji's Garage / Gara của Kenji; a repaired relay / một trạm đã sửa. |
| `ui.recipecard.tower` | Đỗ lên bệ của tháp rồi nạp vật liệu vào phễu. | card | **SHORTEN**, shown only when every need is met | Park on the pad to build. / Đỗ lên bệ để xây. |
| `ui.recipecard.blueprint` | Đã giải mã bản vẽ. Đỗ lên bàn xoay ở xưởng xe để chế tạo. | a decoded sheet | **MERGE → `next.bay`** | the next line says it once; the card shows only the recipe |
| `ui.tower.level` | Cấp {0}/{1} | panel header | **CUT** | "Xây cấp N" already says which level comes next |
| `ui.tower.purchased` | Đang sửa tháp… | as the stitch starts | **CUT** | the world shows the stitch, then the reward card |
| `ui.bay.fitted` | Đã lắp cho 07 | the kit is on | **KEEP** | |
| `ui.bay.pick` | nhấn để xem món tiếp theo | bay list | **SHOW INSTEAD** | the glyph alone beside the list |
| `ui.reward.*` (title, light_on, light_up, radio_on, reach) | Tháp radio · Cấp 1 / Nhà đã có điện / … | after a build | **KEEP** (the one payoff) | |
| `ticker.recovery.power` | Trạm đã có điện. | Power target done | **CUT** | duplicates `ui.reward.light_on` at the same moment |
| `ticker.recovery.link` / `.dish` / `.answer` | the big targets | target done | **KEEP** | |

### 3.3 Place cards (`PlaceCard`, once per place on first approach)
| Key | When now | Verdict | New text / note |
|---|---|---|---|
| `place.dock.*` | fires at game start (07 starts inside its radius) | **CHANGE THE TRIGGER**: the first time 07 is near the dock **and** the battery is below full (in practice, the first return from a trip) | name + job **SHORTEN**: Charging Dock / Đế sạc · "Park here to charge and save." / "Đỗ vào để sạc pin và lưu game." |
| `place.tower.*` | first approach | **name only**; job **CUT** | the next line and the card teach it |
| `place.lander.*` | first approach | **name only**; job **CUT** | its windows show it |
| `place.bay.*` | first approach | **name only**; job **CUT** | `next.bay` teaches it when it has power |
| `place.lift.*` | first approach | **name only**; job **CUT** | `next.lift.*` teaches it |
| `place.lab.*` | first ride up | **KEEP** name + job | nothing else explains the reader |
| `place.rack.*` | first approach | **KEEP** name + job | no next line covers it |
| `place.garage_port.*` | the port opens | **name only**; job **CUT** | `next.port` teaches it |
| `place.ladder.*` | first approach | **CUT** the card | flavour with no action. The ladder stays a world remnant, and the lift line can add 07's look. |
| `ui.blueprint.found` | a tube found | **KEEP** (a find card) | |
| `ui.blueprint.delivered` | the reader takes the tube | **SHOW INSTEAD** | the reader's reel spins and the board lamp lights; no text |
| `ui.can.decoded` | the "07 can" page | **KEEP** (on demand in Tab) | |

### 3.4 Ticker notices (`TickerQueue`, one line, low)
| Key | Trigger | Verdict | New text (en / vi) |
|---|---|---|---|
| `notyet.tower` | the sonar reaches the dark tower | **SHOW INSTEAD** | the tower's offline plaque lamp blinks and a low "not yet" tone plays. The next line already names the job. |
| `notyet.bay` | the sonar reaches the dark bay | **SHORTEN**; only when the current step isn't `next.bay.wake` | Needs tower level 2. / Cần tháp cấp 2. |
| `notyet.chart` | the sonar reaches the dark chart | **SHORTEN** | Needs tower level 3. / Cần tháp cấp 3. |
| `notyet.tether` | near a loose keepsake, before the ring | **CHANGE THE TRIGGER**: only when the player holds the tether button without the ring | 07 has no tether. Kenji kept a spare at the garage. / 07 chưa có dây kéo. Kenji để một cái dự phòng ở gara. |
| `ticker.tether.back` | the ring is fitted | **CUT** | the unlock card and the "07 can" page show it |
| `ticker.listen.first` | the first stop | **SHORTEN**; only at the first stop **away from home** with an unpinged wreck within range | Stop and 07 listens: a wreck with something left lights up. / Dừng lại để 07 lắng nghe: xác tàu còn đồ sẽ sáng lên. |
| `battery.low.dock`, `battery.low.mast` | 30% | **KEEP** (needed, once per drop) | |
| `ticker.relay.online` | a relay lights | **KEEP** | |
| `ticker.relay.waiting` | a repaired relay has no lit neighbour | **SHORTEN** | Needs a lit relay nearby. / Cần một trạm có điện ở gần. |
| `ticker.cargo.full`, `ticker.cargo.home_full` | the hold or home is full | **KEEP** | |
| `ticker.radio.now_playing` | every track change (`NowPlayingLog`) | **CHANGE THE TRIGGER**: only when the player tunes Bell's dial | |
| `ticker.radio.weather` | once, at tower L1 | **KEEP** (the radio's voice) | |
| `ticker.bell.home`, `ticker.bell.signal`, `ticker.bell.found_0..2` | Bell's beats | **KEEP** (a friend's voice, rare) | |
| `ticker.set.*` | a crew set's 2nd item or completion | **KEEP** (rare, seasoning) | |
| `ui.journey.put_away` | an old save was put away | **KEEP** (rare) | |

### 3.5 Prompts (glyph + one verb; contextual, only when the action works now)
| Keys | Verdict |
|---|---|
| `hint.excavate` Dig, `hint.tether` Tow, `hint.deposit` Place, `hint.reel` Reel in, `hint.salvage` Cut, `hint.repair` / `hint.restore` Repair, `hint.hop` Hop, `hint.tune` Tune, `hint.stow` Stow, `hint.build` Build, `hint.pickup` Pick up + `pickup.*` nouns (13) | **KEEP.** Each shows only when its action is available now. `hint.restore` duplicates `hint.repair`: merge to one key when convenient. |
| `ui.hint.look_up`, `ui.hint.drive` | **KEEP** (first time only, as now) |
| `ui.lab.turn` / `look` / `down` | **KEEP**, first ride only |

### 3.6 HUD readouts and world labels
| Keys | Verdict |
|---|---|
| `ui.meter.home` "nhà · {0} m", `ui.meter.unit` | **KEEP** |
| `ui.battery.power` (the dial's word) | **SHOW INSTEAD**: the dial's cell segments and icon are enough |
| `ui.save_tick.word` | **KEEP** |
| `ui.friend.parts` "Bộ phận của {0}: {1}/{2}" | **KEEP**, on pickup only |
| `ui.crate.caption`, `crate.*.note` | **KEEP** (a find card) |
| `ui.cargo.shelf_free` "Ổ trống. Đỗ vào đây để dựng kệ." | **KEEP**, only when home storage is full (the moment it's needed) |
| site, relay and map names (`site.*`, `hop.node.*`, `map.*.name`), `map.state.*` | **KEEP** (labels and the chart, on demand) |
| `material.*` names, `.job`, `.source` | **KEEP** (Tab and cards, on demand) |

### 3.7 Opening, protocol and board
| Keys | Verdict |
|---|---|
| `lens.boot.*` (3) | **KEEP** (the owner approved it; ~2 s, once) |
| `ui.recovery.title`, `protocol.power.need` | **KEEP** once (the protocol readout). It is the only guidance at that moment, so `next.wake` waits until it fades. |
| `recovery.*.name`, `tech.*.name`, `ui.board.known` | **KEEP** (the lab's board, on demand) |

### 3.8 On demand (Tab, pause, cards, settings, title, credits): not guidance
`ui.cargo.*`, `ui.can.*`, `ui.pause.*`, `ui.settings.*`, `ui.display.*`, `ui.title.*`, `ui.credits.*`, `glyph.*`,
`ui.compass.*`, `ui.card.*`, `kit.*.name`, `relic.*`, `log.*`, `letter.*`, `cassette.*`, `track.*`, `radio.channel.*`,
`friend.*`, `crew.*`, `map.*.note`, `ticker.ending.answer`: **KEEP**. The player opens these or they are the story's
voice. They follow the writing style already and never compete with guidance.

## 4. Net effect
- **At the tower:** one element at a time. Far away, the next line says "Sửa tháp radio." Looking at the tower, the
  card shows only its rows with sources. Once parked with everything, the prompt and the hold ring take over.
- **At game start:** after the protocol readout fades, only "Sửa tháp radio." No dock card until the battery has
  actually been used.
- **Removed:** about 15 strings as text (CUT, MERGE or SHOW INSTEAD). About 20 shortened. 5 trigger changes: the dock
  card, `notyet.tether`, `ticker.listen.first`, `ticker.radio.now_playing`, `ui.cargo.shelf_free`.
- **Work:** ui owns the guidance slot and the card trims. Gameplay owns the trigger changes in `PlaceApproaches`,
  `StationNotYet` for the tower, the tether hint and the listen hint. Story writes the new text into the tables once
  the Director accepts this.

## 5. Status (Director accepted d7393cb7)
**Applied** in en/vi, in the commit after this section: the new text of `next.wake`, `next.salvage`, `next.signal`,
`next.friend.repair`, `next.garage`, `next.port`, `next.bay.wake`, `next.bay`, `ui.recipecard.tower`, `notyet.bay`,
`notyet.chart`, `notyet.tether`, `ticker.listen.first` and `ticker.relay.waiting`.

**Pending spec** (game design's 0.5.5 spec may change these flows; text unchanged for now):
- loose pickups: `next.tower.part`, `next.tower.pad`, `next.dig`, `component.*.needed` (the merge into the part's
  row), `pickup.*`;
- blueprint tubes: `ui.blueprint.found`, `ui.blueprint.delivered`, `ui.recipecard.blueprint`;
- the dock and inventory flow: `place.dock.*` (the new trigger), `ui.cargo.*`, `ticker.cargo.*`.

**CUT keys stay in the tables** until the code that reads them goes. Who removes each, in the same change as the code
path:

| Key | Code path | Removed by |
|---|---|---|
| `next.tower.hold` | `BaseProgression.TowerLine` | gameplay |
| `notyet.tower` | `BaseProgression` dormant answers (→ plaque lamp + tone) | gameplay |
| `ticker.tether.back` | `ProgressionKeys.TetherBack` | gameplay |
| `place.ladder.*` | `PlaceApproaches` (the ladder's place id) | gameplay |
| `place.tower.job`, `place.lander.job`, `place.bay.job`, `place.lift.job`, `place.garage_port.job` | `PlaceCard` (name only for these places) | ui |
| `ui.recipe.need` | `RecipeView` | ui |
| `ui.tower.level` | `TowerPanel` header | ui |
| `ui.tower.purchased` | `TowerPanel` station line | ui |
| `ui.bay.pick` | `TowerPanel` / `BayList` | ui |
| `ticker.recovery.power` | the recovery ticker in UI | ui |
| `ui.battery.power` | `GameUI.uxml` dial word | ui |

Story deletes the keys from en/vi once the code no longer reads them; the localization tests show it.
