# The first hour, v2: what 07 starts with, how the player is led, charging, legibility

> Game design review, 2026-10-12. Source: the owner's play-through of 0.5.0 (2026-10-12): "Is the Space function (sonar
> ping) still needed? I can't tell what it's for. The lifting/towing is also given from the start. We really need to
> review what to give the player from the start and how to lead them. The battery charges too fast. The UI is hard to
> read." Earlier the same day: purpose, crafting, the radio tower's job and the lines were unclear, and music should
> be earned (all decided in M3-24).
> Evidence: the 0.5.0 golden-path Playthrough at spec tuning (`MoonProject-wt/director2/Logs/gameplay-captures/
> playthrough.md`, 1457 s), the UiCapture set (`MoonProject-wt/wordmark/Logs/ui-captures`, 1600x900), `GameUI.uss`,
> the input asset, `BatteryTuning`, `SonarTuning` and the step flow.
> Rulings 6, 11, 14, 15, 16, pillar 6 and GDD v3 §13.4 apply. **Finalized with the owner's answers (§1).**

## Summary
- **07 starts with almost nothing:** drive, look and its one tired beam (cut, dig, repair). Everything else is earned
  where it is first needed, and shows on 07.
- **The sonar button is cut.** 07 *listens* by itself when it rests, from the moment the tower gives it a signal to
  listen for. What still holds something answers with a pillar, as before. Space goes to the leap.
- **Lift and tow are earned at Kenji's garage:** its rover-height service port fits Kenji's spare tether ring, and
  the first things to lift (the tape deck, then a keepsake) lie right beside it.
- **Charging becomes a moment:** 4 s per cell at the dock, 5 s per cell at a mast, only while stopped, one note per
  cell. You can drive away at any time and keep what has filled.
- **The UI grows and sits on glass:** lines 26 px, cards 22 px, nothing under 18 px at 1080p, a plate under every
  line, a cell row top-left instead of the thin arc, and a text-size setting.

## Tóm tắt (Tiếng Việt)
- **07 bắt đầu gần như tay trắng:** lái, nhìn, và một tia beam yếu (cắt, đào, sửa). Mọi thứ khác đều phải kiếm được,
  đúng ở chỗ cần đến lần đầu, và hiện ra trên người 07.
- **Bỏ phím sonar.** 07 tự *lắng nghe* khi dừng nghỉ, bắt đầu từ lúc tháp có điện. Vật còn giữ thứ gì đó sẽ trả lời
  bằng cột sáng như trước. Phím Space chuyển sang nhảy (Hover-Jump).
- **Nâng và kéo được mở ở garage của Kenji:** cổng bảo trì cao vừa 07 lắp cuộn tether dự phòng của Kenji. Thứ đầu tiên
  cần nâng (tape deck, rồi một kỷ vật) nằm ngay bên cạnh.
- **Sạc thành một khoảnh khắc:** dock 4 giây/ô, cột 5 giây/ô, chỉ sạc khi đã dừng hẳn, mỗi ô sáng kèm một nốt nhạc.
  Lái đi lúc nào cũng được, phần đã sạc vẫn giữ.
- **UI to hơn và có nền kính mờ:** dòng gợi ý 26 px, chữ trên thẻ 22 px, không chữ nào dưới 18 px (ở 1080p), dòng nào
  cũng có nền. Hàng ô pin ở góc trên bên trái thay cho vòng cung mảnh, và có cài đặt cỡ chữ.
- §7 liệt kê việc cho từng slice của M3-24 (0.5.1) và những đoạn spec cần sửa.

## 1. Owner decisions (2026-10-12)
| # | Question | Decision |
|---|---|---|
| 1 | The sonar's job | **Cut the Space button.** Discovery comes from Tilly, the dock's evening, glints and pillars; 07 listens by itself when it rests. |
| 2 | Lift and tow | **Unlocked at Kenji's garage:** a rover-height service port fits Kenji's spare tether ring; the first things to lift lie beside it. |
| 3 | Charging speed | **Slower:** 4 s per cell at the dock, 5 s per cell at a mast (4 cells from empty: 16 s and 20 s). |
| 4 | UI legibility | **Bigger type on glass plates, a cell row top-left, a text-size setting** (§6). |

## 2. What a fresh 0.5.0 game does today (facts)
- **Every verb but the leap works from the first second.** Ping (Space), the beam (E: cut, dig, mend, restore) and
  the tether (right mouse: lift, tow, reel) have no gate. Only the leap waits for the Hover-Jump kit; boost is
  automatic with the drums.
- **Nothing ever needs a ping.** The first step ends by driving 15 m from the dock, not by pinging, and no cut or dig
  checks whether its site was pinged. Sites are also revealed by Tilly's spotting and by the dock's evening sweep.
  Bell's light is scripted. The first line, "Something out there might answer a ping.", points at the depot, which is
  already in view. The stations' "not yet" answers are only heard: their text never reaches the screen.
- **The battery fills in seconds, whatever its size:** an empty battery is full in 4 s at the dock and in 3 s at a
  mast, and a mast charges at up to 1.3 m/s, so 07 can charge without stopping. In the Playthrough, 25% → 100% took
  3.1 s.
- **Trips are short.** The Playthrough charged 11 times in its first 24 minutes, and the garage needed three trips
  (two for its cuts, one for its heart and the tow): its cuts alone drove 407 m inside the site.
- **The next line waits 120 s** on a step before it shows. M3-24 B already makes teaching lines show at once.
- **The type is small and floats on the sky** (1080p reference, `GameUI.uss`):
  - the next line, the ticker and the place card's job are 19 px;
  - card text and key labels are 15 px, captions 14 px and small key labels 12 px;
  - the next line and the place card have no plate, only a text shadow, so stars sit inside the letters (capture 62);
  - the ticker is cream at 74% on a 50% plate, about 4.8:1 over a lit dune;
  - the battery arc is 172 x 18 px with a 3 px stroke, bowed over the next line, and hidden whenever the battery is
    full or the beam works, so players rarely see it;
  - the bay list cuts its descriptions with "…";
  - the three material icons are about 16 px, with similar silhouettes and no words;
  - prompts use jargon ("Mend", "Hold");
  - the ticker gives directions as numbers ("bearing 140°"), and there is no compass to read them against.

## 3. Verbs: at the start, or earned
Rule: a verb is given at the start only if the first two minutes need it. Every other verb is earned where it is
first needed, by a find or a machine, and **shows on 07** (ruling 11, GDD v3 §13.4). One input per idea: E is "the
beam works on what glows", right mouse is "the tether holds".

| Verb | Decision | When, and by what | Visible on 07 | Why |
|---|---|---|---|---|
| **Drive** | at the start | — | — | the first thing anyone does |
| **Look** | at the start | the opening's "Look up" | the head follows the camera | the sky is the hook (ruling 8) |
| **Beam: cut** (E, hold) | at the start | taught at the depot (min 4) | the beam itself | the one thing 07's tired beam still does |
| **Beam: repair** (E, hold) | at the start | taught at the tower port (min 8) | — | **merges stitch, mend and restore** into one word: the player learns "hold E on what glows" |
| **Beam: dig** (E, hold) | at the start | taught at Tilly's half-buried part (min 12) | — | the same beam, on what lies under the dust |
| **Feed** | automatic | parking on a port | the hopper and the beam | the machine's job, never an input (ruling 14) |
| **Sonar ping** (Space) | **cut as an input; becomes listening** | wakes with tower L1 (the tower's carrier gives 07 something to listen for) | the antenna tip lights cyan at L1; it lifts and blinks when 07 listens | owner decision 1; see "Listening" below |
| **Lift, tow, reel** (right mouse, hold; scroll) | **earned** | Kenji's garage port fits Kenji's spare tether ring (min ~20) | a brass ring round the beam's emitter, faintly cyan at rest and bright while holding | owner decision 2; the first things to lift lie beside the port |
| **Place** (right mouse) | with the tether | the garage port's tray (min ~22), then the lab shelf | — | a tether verb, taught where a tray waits |
| **Stow** (Cargo Cradle) | earned | the bay, as built | the rack | unchanged |
| **Leap** (Hover-Jump) | earned | the tube at the chasm's lip, decoded in the lab, fitted in the bay at L2 | the coils | unchanged. **Rebound to Space and the gamepad's South button** (freed by the ping). |
| **Boost** | automatic | the capacitor drums (Boost Coils) | the drums | unchanged, no input |
| **The lab's reader** | automatic | riding up the lift with a tube | the reel spins, the tape runs | no input. The first tube carried gets one line: "The lab can read what you found." |
| **Radio-hop** | earned | two lit masts | — | unchanged |
| **Survey Scanner** (0.6) | earned, **by looking** | its 0.6 blueprint | the neck boom (§13.4) | GDD v3 §12 A1 said "the sonar-and-look verb". Now the boom scans a fragment that 07 looks at, from within 10 m, while still for 1.5 s. Still no button. |

### Listening (what replaces the ping)
- **When:** from tower L1 on, once 07 has rested for **2 s** (speed ≤ 0.3 m/s), and only after **30 m** of driving
  since its last listen. So stopping in a new place listens once, and sitting still never repeats it.
- **What the player sees:** the antenna lifts, its tip blinks, and the old ring rolls out over 5 s to 140 m, with the
  same kalimba note, 6 dB softer than the old ping. Whatever still holds something answers as today (sites, loose
  keepsakes, broken friends): a pillar for 20 s, a breathing ground ring, and 07's head turns toward the nearest
  answer. A wreck that stays silent is picked clean.
- **The first listen** shows one line, once: "07 listens when it rests. What still holds something answers."
- **The world's pointers stay as they are:** the tower's sweep at L1 and at each dock evening, Tilly's spotting, Bell's
  signals and the glints. Listening is the quiet, always-on version of the same answer.
- **Not-yet answers** become lines on first approach: "Asleep. Metal and a coil will wake it." The text shows on
  screen, and the tone still plays.
- **Wide Sonar** (0.6) becomes **listening range x2**, with the roof dish, as GDD v3 §4.4 already has it.

### Kenji's garage port (where the tether comes back)
- The garage's heart is a rover-height **service port**: a dusty bay under the garage roof with a folded arm, a
  painted pad and a sealed crate stencilled "07 · SPARE", made for wheels (rulings 12 and 14).
- The last cut opens it. Its place card: "Kenji's Garage Port: fits 07's spare parts."
- 07 drives onto its pad and the arm fits the **tether ring** in a ~4 s install moment: sparks, the two-note rise and
  the ring's first hum. Ticker: "07 can hold things again."
- The same port then fits **the tape deck** (M3-24 C) in a second moment. 07 lifts the deck onto the port's tray
  ("Place"), and the arm fits it on 07's left flank; the first tape plays softly.
- **Before the ring,** a keepsake that 07 looks at gives one line, once: "07's tether is gone. Kenji kept a spare in his
  garage." This is the keepsake's not-yet answer. It teaches the verb, so it may name the garage.
- **Saves from 0.5.0:** the ring counts as fitted if the save holds any delivered keepsake or the Cargo Cradle, so
  nobody loses a verb they already used. No save floor is needed.

## 4. The first 30 minutes
Rules: one new thing at a time, each verb learned the moment it is needed (ruling 6), no tutorial screens. The next
line shows at once during these teaching steps (M3-24 B), hides while driving and returns 1.2 s after 07 stops. A
prompt is a key glyph and one word, shown near the thing, and it retires after it has been used (as built).

| Min | What the player sees | The one thing to do | Line (en) | Learned by needing it |
|---|---|---|---|---|
| 0–1:30 | The opening: darkness and crackle, the lid, the boot readout, then the **protocol readout** (M3-24 B): "POWER THE STATION ▸ the radio tower". 07's four flank lamps light one by one. | nothing (any drive input reaches the chase frame within ~4 s) | — | the game's goal, in 07's own eye |
| 1:30–2:30 | 07 on the dock. The only warm lights are the dock, 07's eye and the tower's slow pilot blink beside the lander. | drive to the tower | "Power first. The radio tower is asleep." | **drive**, **look** ("Drive" or "Look up" prompt) |
| 2:30–3:30 | The tower: its place card, then its port's recipe card when 07 faces it: Metal 0/1, Wiring 0/1, Optics 0/1, Tower Coil 0/1. The depot's cut seams glint cyan 40–55 m away, the only cyan on screen. | turn toward the glints | "It needs metal, wiring, optics and a coil. The old depot has some." | what the tower is for, and that a card shows what it needs |
| 3:30–7:00 | The depot: 4 glowing seams. Each cut floats its piece to 07, the melody climbs, and the materials chip appears top-right with words. The last cut opens the heart: the coil glows. | hold E on a seam; drive over the coil | after the last cut: "The coil. The tower is waiting." | **cut** ("Cut"). The first emptied cell shows the cell row top-left. |
| 7:00–9:30 | The tower's port pad lights, because 07 now carries what it needs. Parking feeds the hopper and the coil rides in. Then a slow light runs from the tower's foot along each cable to the lander, the bay and the lab (M3-24 B). The windows light one by one, the radio clears to the Earth signal's carrier (no music), and **07's antenna tip lights cyan**. | park on the pad, hold E on the port | ticker: "Power 1 of 3. Home has light again." | **feed** (by parking), **repair** ("Repair") |
| 9:30–10:30 | The tower's first sweep: a ring rolls out from the tower, and past the dune ~90 m away a pillar rises with a broken chirp. 07 turns its head to it. | drive toward the pillar | "A faint signal, out past the dune. Someone is out there." | pillars mean "something answers here" |
| on the way | 07 stops, and after 2 s it listens: the antenna lifts, the ring rolls out, and Tilly's pillar brightens. | — | once: "07 listens when it rests. What still holds something answers." | **listening**, with no button |
| 10:30–14:00 | Tilly's crater: Tilly, broken, and her three parts glinting. Two lie in the open; the third is half under the dust. | drive over two parts; hold E on the third; hold E on Tilly | at the third part: "Her last part is under the dust." | **dig** ("Dig"); repair again. Tilly wakes; her gift is the wing's missing cell. |
| 14:00–16:30 | Home with Tilly. At about 30%: the falling two-note (A → F#), the eye dims, the dock's pillar rises. | drive onto the dock | "Charge is getting low. The dock can top you up." | the battery is range, and home charges |
| 16:30–17:30 | **The charging moment** (§5): each lamp and each pill fills with a note, the camera eases wide, and 07 perks up at full. The dock's evening runs inside it, and the tower's sweep raises one pillar: Kenji's garage, ~140 m away. The day is kept. | rest, or drive off when ready | after the evening: "Kenji's garage. Something of 07's might still be there." | charging is a calm stop; the evening names the next light |
| 17:30–20:00 | Tilly spots glints on the way. The garage is a long shuttered shed with 2–3 glowing seams. | cut | — | (cut, already known) |
| 20:00–21:00 | The last cut opens **Kenji's garage port**: its card, its pad lit. | park on the port's pad | "Kenji's old service port. It still has a spare." | the tether is earned: the ring's install moment, and the ticker "07 can hold things again." |
| 21:00–23:00 | The **tape deck** lies half-buried beside the port, glinting. | hold E (dig), hold right mouse (lift), set it on the port's tray | — | **lift** ("Lift"), **place** ("Place"). The port fits the deck on 07's flank and the first tape plays softly: the first music, earned. |
| 23:00–27:00 | A second glint in the garage: a keepsake (the duck). | dig, lift, drive home with it on the tether | once it is held: "Home has room for it." | **tow**: it is simply lift and drive. The warning may fire again near home, and the dock teaches the same lesson twice. |
| 27:00–30:00 | The belly lift is jammed. Repaired, it carries 07 and the duck up into the lab: the keepsake shelf, the reader and the recovery panel (Power 1/3, read for the first time). | hold E on the cable; ride up; place the duck | "The lift is stuck. Repair its cable." Then, in the lab: "The lab. What 07 brings home becomes what it can do." | the lab's purpose, and the four targets on their panel |

**After minute 30** (unchanged from the 0.5.0 path): Tilly spots the transformer at the garage, then tower L2 and the
bay; the chasm's lip and the Hover-Jump tube; the mound mast ("masts charge"); the leap; the canyon. Once the teaching
steps are over, the next line speaks only of tier 1 (M3-21) and waits ~2 min (P8).

**The garage fits one trip.** From a full 4-cell battery, the garage's cuts, its port and the tape deck must leave at
least 25% on arrival at the dock (a Playthrough assertion). The duck may be a second trip. To get there, the garage's
seams sit within a 25 m circle of its port and number 3, not 5 (P10); the material counts per seam rise so that P9's
2x economy holds.

## 5. Battery and charging (ruling 15 unchanged)
| Tuning | 0.5.0 | 0.5.1 | Why |
|---|---|---|---|
| Dock fill | 4 s for a full battery, any size | **4 s per cell** (4 cells from empty: 16 s; 6 cells: 24 s; a usual 2–3 cells: 8–12 s) | owner decision 3: one cell, one lamp, one note, readable |
| Mast ring fill | 3 s for a full battery | **5 s per cell** (4 cells: 20 s; 6 cells: 30 s; a usual 2–3 cells: 10–15 s) | home is the best charger |
| When charging starts | at ≤ 1.3 m/s on the ring | **only when stopped:** ≤ 0.3 m/s for 0.5 s on the dock or the ring | charging reads as a stop, never a drive-through |
| Leaving early | — | **free at any moment;** the filled part of a cell is kept | rulings 4 and 15: nothing resets |
| Solar trickle | 180 s per cell, up to 1 cell, after 3 s still | **unchanged.** Visible: the wing tilts toward the sun and the lowest lamp breathes slowly (2 s). | 36x slower than the dock, so waiting is never a strategy |
| Low warning | below 30% | **below 30%, or when the range left is under 1.5x the way to the nearest lit charger** (straight line x1.2), whichever comes first. Never within 40 m of a lit charger, never while charging. | a percentage doesn't know where home is |
| Very-low glance | below 15% | unchanged | |
| Crawl, Tilly's lead, the 300 m rule | as M3-20 | unchanged | |

**The charging moment, beat by beat:**
1. **0–0.6 s:** a soft clunk, and the charging hum swells.
2. **From 2 s:** the camera eases to the stillness wide shot.
3. **Each cell** eases its flank lamp and its HUD pill from dark to full over its 4 s (5 s at a mast), and lands on
   the next note of D E F# A B D'.
4. **At full:** a soft resolved chord, and 07 perks up (head lift, brighter eye, antenna wiggle).
5. **Driving off** at any point eases it all back.

At the dock, the evening's wide shot holds at least as long as the fill, up to 16 s. If the fill runs longer, the
camera returns to the chase frame and the fill goes on, visible on the lamps.

## 6. UI legibility (diegetic first)
The world still carries meaning first: the flank lamps for charge, pillars for answers, lit pads for "come here", and
07's head turning to look. The HUD mirrors the world, and it must be easy to read when it does.

### 6.1 Type scale (1080p reference; the panel already scales by height)
| Role | Size | Weight | Used by |
|---|---|---|---|
| Display | 34 px, letter-spacing 2 px | SemiBold | kit titles, site and friend names |
| Title | 28 px | SemiBold | a place card's name, a card's heading |
| Line | 26 px | Medium | the next line, ticker lines, a place card's job, a prompt's word |
| Body | 22 px, line height 1.35 | Medium | card text, recipe rows, the bay list |
| Caption | 18 px, letter-spacing 1 px | Medium | a card's kicker ("Radio Tower"), "Level 1 of 3", counts' labels |
| Key glyph | a 40 px cap, 18 px label (the gamepad's round button: 40 px) | Medium | every prompt |
| Material icon | 24 px, each count 22 px | — | the chip and recipe rows |

- **Nothing is smaller than 18 px.** The 12, 14 and 15 px styles go.
- **Text size setting:** 100 / 125 / 150% in Settings (saved), scaling every size above. Layouts wrap; they never
  truncate.

### 6.2 Contrast and plates
- **Every line sits on a glass plate:** `rgba(11,14,42,0.72)`, radius 16 px, padding 10 px by 22 px, with the soft
  painted edge. Cards keep `--moon-glass` (0.84).
- **Target ≥ 7:1** over the brightest dune (`#A7A9CC`) and over the starfield; UiCapture measures it (§7).
- **Information is full cream.** Dim (74%) is only for secondary words inside a card. Faint (46%) never carries
  information.
- **Amber text** (keys, lit things) is used only at 18 px or more, on glass.

### 6.3 Zones (inside M3-24 A's safe area, 48 px at 1080p)
| Zone | Holds |
|---|---|
| Top-left | **07's cells** (the cell row, §6.4) |
| Top-centre, 56 px down | the next line |
| Top-right | the materials chip |
| Upper third, centre (22%) | place cards and kit titles, one at a time |
| In the world, above the target | prompts, never over 07, clamped inside the safe area |
| Bottom-right | station cards (recipes, the bay list) |
| Bottom-centre, 48 px up | the ticker |

Each zone shows one block at a time; a new block waits for the last to fade. The screen never shows more than three
text blocks at once.

### 6.4 The cell row (replaces the arc)
- **Shape:** one pill per cell, at most 8, on a small glass plate. Each pill is 14 x 30 px with a 6 px gap. The later
  long lamps (Range-Extender, Power Core) are 30 px wide pills. The row mirrors the flank lamps exactly: same count,
  and the same breathing for the cell being drawn.
- **States:** a full cell is amber, the cell in use breathes, and an empty cell is a 20% cream outline. At Low, the
  last lit pill breathes slowly. Never red.
- **When it shows:** it first appears when 07 drops below 70% (as built). After that it shows while 07 is not full,
  while charging, and for 4 s after full. While the beam works it **dims to 50%**; it no longer hides.

### 6.5 Words and cards
- **Prompt words:** Cut, Dig, Repair (replaces Mend, Stitch and Restore), Lift, Place, Stow, Tune, Hop, Leap. "Hold" is
  shown as a ring filling round the glyph, never as a word.
- **Lines** are one sentence: at most 60 characters in English and 70 in Vietnamese.
  - They name places the player has seen and never use bearings or numbers ("Bell's picking something up, toward the
    canyon.").
  - A line that points somewhere also raises that place's pillar.
- **Cards are 520 px wide and never truncate.**
  - The bay list shows each kit's name and cost. The selected row shows its full text, written to fit 3 lines.
  - Recipe rows read "icon + word + have / need" ("Metal 2 / 1"). A need that is met is cream; a short one is dim
    amber with a hollow pip.
- **Material icons** get distinct silhouettes: Metal a hex plate, Wiring a coil, Optics a lens. Art's icon painter
  draws them.
- **Pause → "07 can":** a short list of the verbs learned so far, each with its glyph, and nothing not yet learned.
  It is a reference, never a tutorial.

## 7. What this changes
### 7.1 M3-24 (0.5.1), tasks per slice
**ui-fit** (with A):
1. The type scale and the 18 px floor (§6.1); Medium weight for lines; the text-size setting (Settings, saved,
   en + vi).
2. Plates under the next line, the ticker and the place card, and contrast ≥ 7:1 (§6.2).
3. The zones and the one-block-per-zone queue (§6.3).
4. The cell row top-left, replacing the arc, with its show rules (§6.4).
5. Cards 520 px, no truncation, recipe rows with words and have/need; 24 px icons with art's new silhouettes; 40 px
   glyphs with the hold ring.
6. Pause → "07 can".
7. UiCapture at 1920x1080: the next line over stars, the ticker over a lit dune, the tower card, the bay list, the cell
   row while charging and at Low, and a 150% text size. Each capture logs its measured contrast.

**clarity** (with B and the tape deck):
1. **Listening:** remove the Ping input. 07 listens after 2 s at rest once 30 m have been driven, from tower L1 on. Add
   the first-listen line, the antenna tip lamp at L1 and its lift-and-blink (Rover), and the not-yet answers as lines
   on approach.
2. **The leap moves to Space and the gamepad's South button.** Update the prompt glyphs and the pause list.
3. **Tower L1's sweep** raises Tilly's pillar.
4. **Tilly's third part** lies half-buried, which teaches dig.
5. **Kenji's garage port:**
   - the station (Gameplay), its prefab and pad (Art), and the install moment (Rover);
   - the tether input stays asleep until the ring is fitted;
   - the ring is a new kit piece (Core: `RoverKitPiece.TetherRing` and a `RoverAbility.Tether` gate, both listed in
     the report);
   - the keepsake's not-yet line; the save rule (§3);
   - the tape deck is fitted at the same port after its lift.
6. **The step flow** follows §4, with its lines in en + vi.
7. **Charging:** 4 s per cell at the dock and 5 s per cell at a mast, only when stopped, partial cells kept, the
   distance-aware Low rule, the charging wide shot after 2 s, and the dock evening holding for the fill (§5).
   EditMode tests cover each number.
8. **The garage fits one trip:** 3 seams within 25 m of the port, with counts kept at 2x. The Playthrough asserts at
   least 25% at the dock after the tape deck.
9. **Words:** "Repair" replaces Mend, Stitch and Restore; ticker lines lose their bearings.
10. **The golden path and the free-order test** follow §4 and fail if the tether works before the ring. Capture the
    first 30 minutes again.

**voice** (with C and the audio):
1. Cell notes at the new 4–5 s cadence, a seamless charging hum and the full chord.
2. The listening ring: the kalimba note at −6 dB, at most once per stop.
3. The garage port: the arm, the install, the ring's first hum.

### 7.2 Spec and doc text to change (the Director's)
- **VISION ruling 3:** "Pings leave a trail" becomes "Listening leaves a trail: an answer leaves a soft light pillar for
  ~20 s, 07 keeps glancing toward the nearest, and 07 listens by itself when it rests."
- **VISION Audio:** "Sonar: a kalimba ping in key" becomes "Listening: a soft kalimba ring in key when 07 rests". The
  rest of that line stays.
- **GDD v3 §2:** the seconds loop becomes "notice a light → stop, 07 listens → it answers → drive → the beam works".
- **GDD v3 §3.2 and §3.3:** the listening cost is 0. The dock fills 4 s per cell and a mast 5 s per cell, only while
  stopped.
- **GDD v3 §3.4:** the HUD is the cell row (§6.4).
- **GDD v3 §4.3:** Wide Sonar becomes listening range x2.
- **GDD v3 §4.4 and §13.4:** add two visible parts: the antenna tip lamp (tower L1) and the tether ring round the
  beam's emitter (the garage port).
- **GDD v3 §12 A1:** fragments are scanned by looking at them while still. There is still no button.
- **M3-16:**
  - steps 1–4 follow §4: cut at the depot; repair at the tower; Tilly found by the tower's sweep; dig at her last
    part; the tether at the garage;
  - the place table gains Kenji's garage port;
  - "sonar 'not yet' answers" become approach lines.
- **M3-20:**
  - §3 takes the fill, start and Low numbers;
  - §4: charging only when stopped;
  - §7: the Low rule;
  - §8: the cell row.
- **M3-21 §3:** the teaching steps are §4's list.
- **ARCHITECTURE input map:** Ping is removed and Jump moves to Space and the gamepad's South button.

## 8. Acceptance
- **A fresh player, after 10 minutes,** can say what the game wants (power the station), what the tower does (powers
  home, level by level), and how to find things (pillars, and 07 listening when it rests), without having pressed a
  key that did nothing.
- **Before Kenji's garage port,** the tether never works. After it, the ring is visible on 07 from the chase camera and
  at 30 m.
- **Charging** a usual 2–3 cells at the dock takes 8–12 s and reads as a calm moment: a note per cell, the wide shot,
  the perk. No charging happens while moving. Driving off keeps what has filled.
- **At 1080p** no text is under 18 px. Every line and card measures ≥ 7:1 in UiCapture, and the 150% setting lays out
  with no clipping and no "…".
- **The first 30 minutes of the Playthrough** follow §4's order, and every teaching line shows at once.
