# Databank: 07's scan archive on its CRT

> Owner decision (2026-10-19): a scan databank, as in Subnautica, living on 07's CRT. It opens like the inventory,
> as a page beside the hold, and the camera pushes in until the CRT nearly fills the screen. Author: the story & flow
> session. It supersedes the Notes tab in `0.6-living-moon.md` §4.
> Bound by: `tech-eras.md` (eras and makers), `screen-ui.md` (the CRT page, input, pixel budget), `writing-style.md`
> and `07-identity.md`. The entry text is in `text/databank-entries.md`. Everything here is `[PROPOSAL]` unless it
> cites a decision.

## 0. Player experience
- **Hook:** the first open shows 07's own maker's mark and **07's own wireframe** turning on the tube. The player
  learns what they are before anything else, and every scan after that becomes a turning amber drawing of the thing
  they just touched.
- **Pull:** every card stops where the crew's records stop, in October 1988, so the gaps ask the questions. Why do
  the eclipse's city lights read **0**? Who are Tycho? Why does a 1967 lander sit in a crater the crew walked past?
- **Hold:** the collection grows by category, each category counting up as it fills. On every card **the era strip**
  (5 marks, P S B L N) lights that entry's era alone, so the player reads where in the timeline each find sits.
  History becomes something the player owns. (Director ruling 2026-10-19: one lit era per entry, not a collection fill.)
- **Finish:** the cards build the case the transmission makes. The last Archive card arrives with the answer from
  Ark IV, as a print.
- **Feeling:** *quiet archaeology*, company in absence. It comes from the slow turntable, the amber lines, the
  crew's own plain sentences, a relay click between pages, and no voiceover.
- **Learn, then improve:** Subnautica's PDA rewards curiosity, and knowledge becomes a collection you read at your
  own pace. We improve on it four ways:
  - the picture is **your scan, live and turnable**, not stock art;
  - cards are 1 to 3 sentences, not essays;
  - Maker, Year and Era teach the player to read the basin's history (`tech-eras.md` §0);
  - nothing is read aloud: the calm stays.
- **Logic:** the databank is 07's **parts-profiler library** (07-identity §1, scan = profiler) matched against
  **Lumen's station catalogue**. The crew wrote the catalogue cards, which is why the history is in Kenji's, Ines's or
  Ro's voice (third person about 07, never addressing it) and ends in 1988. Anything newer has no card: it shows
  `NO LUMEN RECORD` and 07's measurements only, which is itself a clue.
- **Calm:** reading never times out and costs nothing beyond what the open hold page already costs (same rules
  for time and battery).

## 1. Categories
| en | vi | Holds | Default voice |
|---|---|---|---|
| MACHINES | MÁY MÓC | rovers, landers, satellites, Meridian machines, 0.6 machine life | Kenji |
| STRUCTURES | CÔNG TRÌNH | ruins, instruments, masts, dishes | Kenji, or Ines for science kit |
| PLACES | ĐỊA ĐIỂM | sites and regions, once reached | Ro |
| PHENOMENA | HIỆN TƯỢNG | the eclipse, earthshine, quakes, flashes, dust glow; rare life until it has 3 entries, then its own LIFE category | Ro |
| ARCHIVE | LƯU TRỮ | people and documents as scanned prints: maker cards, ILC posters, the crew roster, letters | the document itself |

Each category shows **found count only** (no totals, no percentages), plus a small dot while it holds an unread card.

## 2. Entry schema (Gameplay data, one ScriptableObject catalogue)
| Field | Type | Rule |
|---|---|---|
| `id` | string | `db.<name>`, saved, never renamed |
| `category` | enum | §1 |
| `nameKey` | loc key | ≤ 24 chars, in both languages |
| `maker` | enum: `None, ILC, KesslerArai, VIS, Tycho` | tech-eras §6. Places and phenomena: `None` |
| `year` | string | the year it was made or placed: `1967`, `1971`, or `1971 · 1984` for two eras |
| `era` | enum: `P, S, B, L, M, N, None` | tech-eras §1 |
| `readoutKey` | loc key | **07's measured line**: uppercase, `·`-separated, ≤ 2 lines of 48 chars (vi ≤ 56) |
| `historyKey` | loc key | the catalogue card: 1 to 3 sentences, signed `— K.`, `— Ines` or `— Ro`. en ≤ 200 chars, vi ≤ 240. Empty means `NO LUMEN RECORD` |
| `view` | `Model` or `Print` | a model is a prefab ref plus `hiddenNodes` (e.g. the Scout's removed camera and scoop, so the page matches the world) and a framing yaw. A print is a texture ref |
| `unlock` | `Seeded`, `StoryScan(id)`, `FragmentOf(setId)`, `SetComplete(setId)` or `Milestone(m)` | §3 |

## 3. Unlocking and set progress
- **Seeded** entries exist from the first open: 07, the Kessler–Arai card and the Earth eclipse.
- **A story scan** (`StoryScanned`) unlocks its entry whole. Each scan still prints its readout on the CRT as it
  does today.
- **A scan set** unlocks its entry on the **first** fragment (`FragmentScanned`). The model draws faint, with
  `SCAN 1/3`. Each further fragment brightens the wire. The last one (`BlueprintCompleted`) draws it full, shows the
  history, and adds one line: `BLUEPRINT · SOLAR ARRAY`. So the 1/3 count lives in **one place**, the entry, and the
  databank is where the player watches a blueprint come together.
- **On unlock:** one toast line on the CRT's event line, `Added to databank: Scout 3` / `Đã thêm vào dữ liệu:
  Scout 3`. No popup, no sound beyond the scan's own close. The page waits.

## 4. UI spec
- **Open:** Tab opens the hold page as today. **Q / E** (LB / RB) flip pages, HOLD ↔ DATABANK, with a CRT channel
  flip: the existing collapse to the line in 0.16 s, then open, plus a relay click. This **replaces** screen-ui §5.3's
  use of Q / E for jumping between hold and sockets; that move stays on the d-pad crossing. The page remembers the
  last entry it showed.
- **Camera:** on the databank page the face shot eases from 1.08 m to **0.90 m** in 0.35 s (SmoothStep), so the
  screen fills about 90% of the frame height. GentleCamera crops instead of moving.
- **Layout** (1152 × 768 panel):
  - **Left, 230 px:** the 5 categories. The selected one expands into its entry list, one level, newest first, with
    unread dots.
  - **Right, top, ~860 × 420:** the **live model** in the amber scan wireframe. It turns slowly (6°/s) when idle;
    drag or the right stick turns it (yaw free, pitch ±30°). A print shows as a flat scanned sheet with halftone
    grain and a slight skew.
  - **Right, below:** the field row `MAKER · VIS   YEAR · 1967   ERA · PROBES`, then the readout line, then the
    history card and its signature.
  - **Bottom:** the era strip (5 small marks; only the shown entry's era is lit).
- **F (Y on a gamepad): full colour.** The model renders with its real materials, and a print in its true colours.
  - The logic: 07's tube is a **colour shadow-mask CRT run in amber** for low power and visor-readable contrast. That
    is plausible for 1983, and it squares the owner's pick with v4's amber look.
  - The toggle stays per session.
- **The one-element rule:** the databank adds **nothing to the HUD**. Its only world-side trace is the toast line
  above, which reuses the CRT's existing event line.
- **Readability:** screen-ui §8.2's floors (title 17 px, body 13 px, mono 13 px), and the inventory's specular and
  scanline damping. History sits at 14 px with line height 1.4, because Vietnamese diacritics stack. Uppercase vi
  readouts keep their diacritics (`ĐƠN VỊ`, never `DON VI`).
- **Sound:** page flip, relay click; colour toggle, a soft tube thunk; selecting an entry, a tape-head tick. No 07
  tones here, because the annunciator is for status.

## 5. Contracts and tasks
**Core (Director):** `DatabankEntryAdded(entryId, category)`, `DatabankEntryProgressed(entryId, found, count)`, and
`IDatabank`, which offers unlocked ids, `Progress(id)`, `IsUnread(id)` and `MarkRead(id)` (the 0.6 doc's contract,
widened).

| Domain | Must build |
|---|---|
| **Gameplay** | the `DatabankCatalogue` asset (§2); the unlock rules wired to `StoryScanned`, `FragmentScanned`, `BlueprintCompleted` and `OpeningMilestoneReached`; save (unlocked, progress, read); **new story scans:** the seismic station, the retroreflector, the flag (unlocks the ILC card), a Kestrel-3 hull panel, 06's chassis, and the ruins' first fragment for the Lumen place entry |
| **UI** | the databank page in the CRT panel; the Q/E page flip; the model preview stage (an isolated layer, camera and light rendering one prefab into a sub-rect of `_ScreenUiTex`, reusing the scan wireframe material and honouring `hiddenNodes`); the print viewer; the colour toggle; the era strip; the toast |
| **Rover** | the per-page face-shot distance (0.90 m), driven from the page state |
| **Art (Argon)** | the prints: the 4 maker cards (the 3 logos plus the VIS wordmark), the ILC poster, the crew roster sheet |
| **Story** | `text/databank-entries.md` (done: 18 entries, en and vi) |

## 6. Playtest signal
- In the first session, the player opens the databank page **without being told**, by the third scan, and turns a
  model.
- At least half of testers stay on one history card for more than 6 s.
- Nobody asks "where do I see what I scanned?"
