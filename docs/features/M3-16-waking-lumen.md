# M3-16: Waking Lumen Station, the step-by-step first hours

> Owner: Director (design). Owner review of 0.4.x (2026-10-09): "from the very start everything is laid out. Does the
> crafting bench still do anything? I don't understand what home is for. There's a ladder, and on one side a lift
> that looks useless. It must be clear: design the progression step by step, so the player unlocks things and gets
> used to them." This spec replaces "everything available from minute one" with a guided, logical wake-up of the
> base. It absorbs M3-15 §2 (the lift) and §4 (pads). Rulings 11, 12 and 14 and pillar 6 apply.
>
> **Approved by the owner on 2026-10-09:** this is the core of 0.5; a fresh game opens fully dark (only the dock and 07's eye are lit); kits unlock through blueprints found in salvage sites.

## Three rules
1. **Asleep until woken.** The whole base is there from the first frame: it is home, and you should see it. But every
   machine is dormant: dark, silent, no prompt, no pad lamps.
   - A dormant thing answers the sonar with a soft low "not yet" tone and one line saying what wakes it, e.g. "Kenji's
     Rover Bay: no power. The tower feeds it."
   - Each wakes in its own moment: light, sound, one ticker line and the camera's brief attention.
2. **One new thing at a time.** Each step teaches one verb or opens one place. Prompts appear the first time a verb
   is needed and never before. The HUD only shows what has been learned: materials appear with the first material.
3. **Every place has one job, said in one line.** The first time 07 comes near a place, its name and its single job
   appear once.

## The spine: power comes back through the tower
The radio tower is the base's heart. Each tower level returns power one stage further: L1 home, L2 the bay, L3 the
lift. That gives every unlock a reason the player can see: cables from the tower's foot light up along the ground
to whatever just woke.

## The steps (fresh game, ~90 min)
| # | Step | What wakes or opens | Verb taught | What the player learns |
|---|---|---|---|---|
| 0 | **Waking** (0–3 min) | 07 wakes on its charging dock; the dock's glow and 07's eye are the only warm lights | drive, look | "Home. 07 rests here." Everything else is asleep. |
| 1 | **First salvage** (3–8) | the supply depot beside home answers the ping | sonar ping, beam cut | materials come from the wrecks of the old base |
| 2 | **The tower wakes** (8–15) | tower L1 (port hopper, stitch). Power returns home: lander windows light one by one, porch lamps, the HOME sign straightens, the radio crackles | feed a hopper, stitch | the tower is the heart; feeding it wakes things |
| 3 | **A faint signal** (15–22) | the tower hears Tilly; her signal pillar rises | follow a signal, stitch a friend | friends come home and help |
| 4 | **First memory** (22–32) | Tilly spots a glint; the memory shelf at home wakes when the first relic arrives | dig, tow, place | home keeps the crew's memories |
| 5 | **Kenji's Rover Bay** (32–45) | tower L2 powers the bay: shutter rolls up, the "07" sign flickers on, the arms wake | park on the turntable, choose a kit | 07 grows by fitting kits in the bay |
| 6 | **Over the chasm** (45–65) | Hover-Jump opens the canyon; Bell, the dial, cassettes | charged leap | upgrades open the moon |
| 7 | **Reach** (65–90) | relay masts carry the tower's signal; radio-hop between lit masts | restore a mast, hop | the network is the second axis |
| 8 | **The crew's lift** (~90) | tower L3 powers the winch. Restore it (feed and stitch), then ride up to the lander deck lookout | ride the lift | from up there 07 sees the whole basin. In 0.5 the crew's map inside opens new regions. |

## Each place and its one job
| Place | Its job | At the start | Wakes at |
|---|---|---|---|
| **Charging dock** | where 07 rests: resting saves the day, and later plays a tape | glowing faintly | always |
| **Lander (home)** | the crew's home and the heart of 07's world; its windows show how alive home is | dark windows | step 2 |
| **Memory shelf** | the crew's things, each a story | dark, empty | first relic (step 4) |
| **Tape rack and Bell's radio** | music and the crew's voices | absent | Bell home (step 6) |
| **Radio tower** | the heart: power and signals | dark broken stub with a lit port hopper | step 2 |
| **Kenji's Rover Bay** | fits kits on 07 | shutter down, sign dark | step 5 |
| **Kenji's blueprint board** (the old human bench) | the kits the bay can build: one pinned sheet per kit. 07 scans blueprints found in wrecks and they appear here. Answers "does the bench still do anything" | one sheet (Hover-Jump), others empty pins | step 5; sheets found from step 1 on |
| **Crew's ladder** | a remnant: the crew climbed it, 07 cannot. The first time near, 07 looks up it, then at the lift: "The crew's ladder. Not made for wheels." | always | never |
| **Crew's lift** | carries 07 to the lander deck lookout (and the map in 0.5) | visibly jammed: tilted platform, one cable snapped | step 8 |
| **Relay masts** | carry the signal; hop points | broken, dark | step 7 |

## Pads instead of neon rings (from M3-15 §4)
A station shows its pad (a worn painted circle with small pad lamps) only while it is awake and has something for 07
to do. Dormant and finished stations show nothing.

## Gentle guidance, never a quest log
- One soft "next" line under the compass shows only the current step's invitation, e.g. "The tower needs metal." /
  "Tháp cần kim loại." It hides while driving and returns when 07 stops. It can be switched off in Settings.
- No markers on the map, no arrows. The world itself points: a signal pillar, a glint, a lit pad, cables lighting up.

## Blueprints (the bench's purpose)
- Kits are no longer all buildable from the start. Hover-Jump's sheet is already pinned (Kenji left it).
- The others are found as blueprint tubes in salvage sites, one per site's heart: Cargo Cradle at the garage, Warm
  Headlamp in the Kestrel wreck, Boost Coils at the drill, Wider Sonar at Bell's terminus. Then they are pinned on
  the board.
- That turns every salvage site into a reason to go, and the bay's choice list grows visibly.

## Save compatibility
A save from before this progression is put away (SaveService content floor raised to this release), as in M3-15 §1.

## Amendments adopted from the design review (2026-10-10, `docs/design/review-2026-10-10.md`)
The Director adopted all ten proposals. The step table above stands, with these changes:
- **P1: a story part per tower level.** Each level needs one story part as well as materials:
  - L1: the coil in the depot's heart (the cassette player moves to Tilly's glint).
  - L2: a transformer Tilly spots at the garage.
  - L3: Bell's frequency, or a relay part.
  Power is earned by a find, not just paid for.
- **P2: the chasm before the bay.** Tilly spots Bell's light across the chasm. The player sees they can't cross, then
  L2 wakes the bay with the Hover-Jump sheet.
- **P3: 07's rear is not a face.** One small off-centre tail lamp, and Cradle straps that never form a V. The lens is
  07's only face.
- **P4: only the next thing speaks.** Before L1 only the tower answers "not yet". Place cards come one at a time and
  only near the current step's place.
- **P5: the wake is a stargaze.**
  - The first beat is a front three-quarter shot on the lens: 07 looks up at Earth, then the camera eases to the
    chase frame. The first prompt is "Look up".
  - There are three warm points: the dock, 07's eye and a slow tower pilot blink. This replaces the "lit port hopper".
- **P6: the dock's evening.** Parking at the dock after a trip gives a wide shot of the day's newly lit lamps. Then
  the tower's listening sweep raises one new signal pillar on the horizon. This is the homecoming and the
  "one more trip" hook.
- **P7: Kenji's pen plotter.**
  - The blueprint board is an old pen plotter.
  - A scanned tube rides home on 07's back, and the plotter draws the sheet.
  - This keeps ruling 14: no hands pin anything.
- **P8: the next line waits.** It shows only after ~2 min without progress, and never while stargazing.
- **P9: the economy at 2×.** Each site has a signature material. Total materials ≈ 2× what the steps need (ruling 5).
- **P10: fewer, better cuts.** The depot has 4 cuts, not 6, and the last cut reveals the site's heart.
- **Wider Sonar is out of 0.5.** It has no asset and belongs to 0.7. The blueprint tubes are Cargo Cradle (garage),
  Warm Headlamp (Kestrel) and Boost Coils (drill).
- **Length.** Steps 0–8 take about 50–60 min. Do not pad them to 90.

## Contracts (Core, landed first)
- **`BasePowerStage`:** Asleep, Home, Bay, Lift.
  - `IBasePower` (Stage, HasReached) is registered by Gameplay.
  - `BasePowerChanged(stage, restored)` is published by Gameplay when a tower level lands, and once on load with
    restored = true.
  - Tower L1 → Home, L2 → Bay, L3 → Lift. A fresh game is Asleep.
- **`NextStepChanged(lineKey)`:** Gameplay's step flow sets the soft "next" line. UI shows it while 07 is still. An
  empty key hides it.
- **`PlaceFirstApproached(placeId)`:** Gameplay publishes it once per save per place. UI shows the place's name and
  its job line. Place ids:
  - `place.dock`, `place.lander`, `place.shelf`, `place.tower`, `place.bay`, `place.board`, `place.ladder`,
    `place.lift`, `place.rack`;
  - localization keys `place.<id>.name` and `place.<id>.job`.
- **Dormant visuals.** Art builds both states into its prefabs: an awake child and a dormant child, or a component
  with a `SetAwake(bool, bool instant)`. Gameplay switches them from `IBasePower`. World lights home's warm points
  from the stage.

## Split
| Box | Delivers |
|---|---|
| gameplay | dormant/awake state per station and power stages from tower levels; step flow and the "next" line source; blueprints and the board; sonar "not yet" answers; lift station and ride |
| art | dormant looks (bay shutter, dark windows, dark tower stub, tilted HOME sign), ground power cables that light, blueprint board and tubes, jammed and restored lift, pads |
| world | home's lighting stages: only the dock and 07's eye warm at the start |
| rover | wake on the dock, the ladder look, riding the lift, the deck lookout shot |
| audio | wake stingers per station, the "not yet" tone, lift sounds |
| ui | first-approach place lines, the "next" line and its setting, the HUD revealing itself verb by verb |

## Acceptance
- A fresh game shows a dark, sleeping base around a lit dock, and nothing offers an action until its step.
- At each step a playtester can say in one sentence what just woke and why.
- Every place in the table above has its job shown once and does that job.
- The golden-path Playthrough follows steps 0–8 in order and fails if any station is usable before its step.
