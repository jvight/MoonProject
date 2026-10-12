# First-hour design QA (0.5.2): every HUD element, prompt, input, card, line and widget

> Author: the game design session. Proposals for the owner's approval.
>
> Base: the 0.5.2 code (a1263860). Sources:
> - `UI/Hud/*` (every element's own summary);
> - `TetherReticle`, `AbilitySlots` and `AbilitySlotRules`;
> - `Data/Localization/en.json` (every `next.*`, `notyet.*`, `hint.*`, `ticker.*`, `ui.*` line used in the first hour);
> - the latest UI captures, `MoonProject-wt/ui/Logs/ui-captures/legible/` (`74_tower_card`, `76_pause_07_can`);
> - the path in `first-hour-v2.md` §4.
>
> Fixes already running are marked **(in flight)** and are listed only where this QA adds something:
> - the tower comes online (feel-pass §8.2);
> - the copy pass (story);
> - the coil pickup prompt;
> - the material dials removed.

## 0. The four questions
Asked of every element:
1. **Purpose:** does the player understand what it is for?
2. **Availability:** does it appear only when it means something? No inputs, icons or keys before their ability
   exists.
3. **Agreement:** does it use the same name, key and state as everything else?
4. **Payoff:** does completing it visibly change something?

## 1. Two rules (the Director's proposals, confirmed and sharpened)

### R1. The ability slots leave the HUD
**Confirmed.** The row (tether, listen, then four padlocks) fails all four questions:
- it shows *ownership*, which the player can't act on;
- "Listen" is automatic, so a slot implies a key that doesn't exist;
- the padlocks promise things the player hasn't heard of;
- it repeats the pause menu's "07 can".

**Sharpened:**
- **Where abilities live:** a "07 can" page in the Tab screen (M3-25's cargo screen becomes the PDA). One row per
  ability **07 has**: its key glyph, one verb and one plain line, e.g. "RMB (hold) · Lift: holds what glows cyan;
  drive to tow it". The pause's "07 can" moves there.
- **No padlocks.** A kit whose blueprint has been *decoded* shows as a pencil sheet: "Hover-Jump: decoded, build it
  in the bay". A kit nobody has heard of shows nothing.
- **The unlocked moment** (once per ability):
  - after the kit title, a card stays up 4 s: the glyph, the verb and one line saying what it does;
  - 07 chirps **Proud**;
  - the next time something the ability works on is in view, its prompt shows (as built). The card teaches the key;
    the world gives the first use.
  - Example: "Kenji's spare tether ring · 07 can lift and tow now · hold RMB".

### R2. Locked inputs do nothing and show nothing
**Confirmed, with one condition** for the single soft line:
- A press of an input whose ability 07 doesn't have shows **no icon, no reticle, no sound, no refusal**.
- **The one line shows only when the press had a target the ability would act on.** Examples:
  - RMB held while a keepsake is in the aim cone shows the existing `notyet.tether` once per save;
  - Space at the chasm's lip before the Hover-Jump shows "07 can't leap yet. The Hover-Jump sheet is at the
    lip." once.
- A blind press says nothing, ever. A press is a question, and the world answers it only when there's something to
  answer about.

## 2. The QA table (worst confusion first)
| # | Element | Where | Problem | Fix (concrete) | Owner | Size |
|---|---|---|---|---|---|---|
| 1 | **Tether reticle before the ring** | `TetherReticle.Tick` reveals on "Idle and tetherHeld", with no ability check | Holding RMB from minute 0 shows the tether's ring and nothing happens. It teaches a broken verb (owner's report). | Gate the reveal, the hover highlight and the hold on `IRoverAbilities` (tether). R2's one contextual line. | ui (+ gameplay check on the hold) | S |
| 2 | **Ability slots row** | `AbilitySlots`, bottom centre | No purpose; padlocks; "Listen" has no key (owner: "completely unreasonable") | **Remove from the HUD.** R1: the "07 can" page in Tab, plus the unlocked moment. | ui M, rover S (chirp) | M |
| 3 | **Tower card** (`RecipeCard` / `TowerPanel`) | the first task, min 2–9 (`74_tower_card`) | "Level 1 of 3" reads as the *current* level (it's level 0). Rows read as fractions ("Wiring 6 / 1"). The card lists 2 materials while `next.salvage` names four things (metal, wiring, optics, a coil). The coil row is missing or unclear. The "E" circle has no verb and shows even when E does nothing yet. "Wake the old mast" / "tired antenna" is personified (in flight: copy). | Header: "Radio Tower · offline · build level 1". Rows: "need, then have" ("Metal 1 (have 6) ✓", feel-pass §8.1). **One row per need, the Tower Coil included**, with its source in small type ("at the depot"). The action line only when parked: "Hold E to repair". Otherwise "Park on the tower's pad". The card and `next.*` list the same needs. | ui S, story S | S |
| 4 | **The coil** | depot heart, min 6–7 | (in flight: the pickup prompt.) Its names disagree: "the coil", "Coil", "Tower Coil" in GDD v3. The "● ● ○" widget has no label. | One name, **Tower Coil**, everywhere: card, ticker, prompt, component socket. The widget is removed, or labelled "Tower parts 2 of 3" only on the tower card. | story S, ui S | S |
| 5 | **Names that disagree** | lines, hop list, ticker | "the mound **mast**" (`next.link.relay.0`), "Mound **relay**" (hop list), "**Relay 2** online" (ticker, a number the player never sees elsewhere). "Lumen Station" (hop) against "home" (meter). "Radio tower" against "mast" and "antenna" (card). | A glossary on the copy pass: **Radio Tower; Relay** with four names (Mound, Shoulder, Canyon Mouth, Terminus Relay), never a number; **Home** everywhere the player is (the hop list says "Home"); **the Lab, the Bay, the Dock, the Lift**. | story S | S |
| 6 | **Next lines that don't say what to do** | `next.lift.ride` "Rest on the lift", `next.bay` "The turntable is waiting", `ticker.relay.waiting` "It listens for a lit neighbour", `next.bay.wake` "The bay sleeps" | "Rest" is the dock's verb. "Waiting" and "listens" give no action. "Sleeps" is personified. | Say the action and why: "Park on the lift's platform to ride up to the lab." / "Park on the bay's turntable to build a kit." / "This relay needs a lit relay nearby. Repair the Mound Relay first." / "The bay has no power. Radio Tower level 2 powers it." | story S | S |
| 7 | **Not-yet lines** | `notyet.*` (shown now) | "Asleep. Metal and a coil will wake it." (personified, and the needs disagree with #3). "It wakes with the tower." doesn't say which level. | "Offline. Needs the Tower Coil and materials: see the tower's card." / "No power. Radio Tower level 2 powers the bay." / "No power. Radio Tower level 3 powers the chart." | story S | S |
| 8 | **Home meter** | `HomeMeter`, top centre | "home · NE 180 m" under an arc that fills with distance. Its purpose (can I get back?) is unstated. It isn't tied to the battery, the one thing distance matters for. | Keep it as the **range** readout. The battery dial gets a small notch at the charge needed to reach the nearest charger. The meter shows only when that need is ≥ 40% of the current charge, and names the nearest *charger* ("dock · NE 180 m" or "Mound Relay · E 90 m"), not just home. Off near a charger. | ui S, gameplay S (charge-to-charger estimate from `BatteryTuning`) | S |
| 9 | **Prompt words and the "07 can" list** | `hint.*`, pause (`76_pause_07_can`) | RMB is "Tow" in prompts but "Lift" in first-hour-v2. Pause lists Dig, Cut and Stow but not Lift or Repair, and lists Stow before the Cradle is understood. Two "Repair" keys (`hint.repair`, `hint.restore`) are fine but duplicated. | One word per input and context: **E** = Cut / Dig / Repair (what glows); **RMB** = Lift (towing is lift and drive); scroll = Reel; **Place** at a tray or shelf; **Stow** only with the Cradle. The Tab "07 can" page (R1) lists every owned verb in that order. | ui S, story S | S |
| 10 | **Relay payoff line** | `ticker.relay.online` "Lumen Station can hear a little farther." | The practical gains (a charger, radio-hop, reach) aren't said. Since M3-20 the charger is the reason to light one. | "Mound Relay online: 07 can charge here and hop home." Plus the reach ring (polish-pass §1.4). | story S, world (in polish wave) | S |
| 11 | **Pause header chip** | pause (`76_pause_07_can`): "⚙ 12 ● 7 ● 0" | Icons and numbers with no words. It repeats the dials being removed. | Remove. Materials live in the Tab screen (M3-25). | ui S | S |
| 12 | **Memory card caption** | `ui.card.caption` "Memory {0} of {1}" | "Of what?" The total spans the whole game (24), while 0.5 holds 6. | "Ro's things: 1 of 6" (by crew set, as on the shelf). | ui S, story S | S |
| 13 | **Lab glyphs on a gamepad** | `glyph.lab.*.pad` | Turn, Look and Down all read "LS". | Directional glyphs: LS ◀▶ (turn), LS ▲ (look), LS ▼ (down). | ui S | S |
| 14 | **The first listen line** | `ticker.listen.first` "What still holds something answers." | Poetic, not functional (writing rule §8.1). | "When 07 stops, it listens. A wreck with something left answers with a light." | story S | S |
| 15 | **Tower purchased line** | `ui.tower.purchased` "The signal grows" | A vague payoff for the game's first big task. | (in flight: §8.2's reward card.) The line becomes the card's first row: "Radio Tower · level 1: home has power". | story S | S |
| 16 | **Battery dial** | `BatteryDial`, bottom left | The cells and a "100% power" readout say the same thing twice. The % pulls the eye every frame it changes. | Keep the cells. Show the percentage only while charging or below 30%. The notch from #8 carries "enough to get back". | ui S | S |
| 17 | **Leap before the Hover-Jump** | Space / South button | *Unverified:* whether a press before the kit shows or plays anything (`RoverLeapRefused` exists for the battery case). | R2: nothing, except the contextual line at the chasm lip. | rover S (check) | S |
| 18 | **Kit title** | `KitTitle` | It names the item ("Kenji's spare tether ring") but not what 07 can now do. | Merged into R1's unlocked moment. | ui (with #2) | — |
| 19 | **Look up / Drive chip, protocol readout, save tick, place cards, hold ring** | various | Clear purpose, shown only when meaningful, consistent. | Keep. Place cards use the glossary (#5). | — | — |

## 3. Order of work (inside the polish waves)
1. **#1, #2 (R1/R2), #3, #4:** the first ten minutes. Without these the player is confused before the first payoff.
   ui-led, about 1 day.
2. **#5–#7, #9, #10, #14, #15:** one copy commit by the story session, using the glossary. Text only, both
   languages.
3. **#8, #11, #12, #13, #16:** ui, after the Tab screen exists (M3-25), since #8, #11 and #12 point into it.
4. **#17:** rover check.

**Acceptance:**
- A fresh player at minute 10 can say what the tower needs, where each need is, and which key repairs it.
- No icon or reticle appears for an ability 07 doesn't have.
- Every thing has one name across cards, lines, prompts and the hop list.
