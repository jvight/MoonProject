# Cutscenes: the cinematic moments of Lofi Lunar

> Author: the story & flow session ("MoonProject · story & flow"). This is a **proposal** to the Director. Nothing here
> changes VISION, DESIGN, STORY or a feature spec until the Director accepts it. Exact ruling edits are in §1.4.
>
> Sources: VISION (pillars, rulings 1–14, camera direction), DESIGN, STORY, M3-16 with the adopted amendments, the
> design review of 2026-10-10, the code on main 6a73dc97 (`Rover/Camera/WakeOpening.cs`, `CameraMoment.cs`,
> `Rover/Character/WakeUpSequence.cs`, `Audio/RadioWakeUp.cs`, `Core/Events/*`), and the captures
> `world/Logs/world-captures/wake-0-asleep.png` and `art/Logs/captures/wake-after/night_wake_*_spawn_first_frame.png`.

## 0. The grammar every moment follows

**Wordless.** STORY's rule holds: no voice, no exposition. A moment shows a thing waking. At most one ticker line
comes after it, never during.

**Eased, never cut.** Every camera move blends with a smooth-step. Today that is `CameraMoment` (ease in, hold, ease
out; look input cancels) or the opening's own blends. The only darkness allowed is a fade through black, and only
where 07 is really moved (a radio-hop) or the game starts. There are no letterbox bars: the HUD fades instead.

**Three tiers of control.** Each moment below names its tier.

| Tier | Length | HUD | Player input |
|---|---|---|---|
| **Glance** | ≤ 3 s | stays | look input cancels at once; driving is never held |
| **Moment** | 4–10 s | fades out, back after | any input eases out within 1 s; the world event still finishes off camera |
| **Ceremony** | the opening, the finale | hidden | input hurries the beats but never removes the payload; Pause always works |

**First time full, then short.** A moment plays in full the first time per save. Repeats play as a glance or not at
all. A moment counts as seen once it starts, so quitting mid-moment never replays it, except the opening (§1).

**Sound.** Every moment has its cue in D major pentatonic (D E F# A B), at mezzo-piano or below. The radio ducks
about 6 dB under a moment and comes back over 2 s. Silence before the payoff is part of the cue.

**07 is the actor.** 07 never has hands (ruling 14). In every moment its head, lid, eye glow, solar wing and antenna
say what it feels. Its gentle one-lens face never changes, and its rear never reads as a face (P3).

**Who builds what.**

| Box | Owns in every moment |
|---|---|
| rover | all framing and camera moves; 07's body beats (head, lid, glow, wing, antenna) |
| world | light: home's warm points, cables, sky, post effects |
| audio | stingers, the radio's duck, the cues below |
| ui | HUD fade, the one ticker line, prompts after the moment |
| art | the models and their states (shutter, arms, lift, dish), props, shaders |
| gameplay | the triggers (events) and the "seen" flags in the save |

## 1. The opening (New Game): 07 wakes, through its own eye

### 1.1 Intent
The owner's brief: *the opening in the robot's first-person view: its eyes open slowly, it looks around, it doesn't
know what happened.* The player **is** 07 for half a minute. They see what 07 sees, in the order it would notice it:
the warm dock under it, the dark home, one small amber light, the Peak's red light far away, and Earth. Then the
camera steps out of the lens and the player sees who they were. That reveal, a small tired machine looking up, is
the "aww".

This is the **only first-person shot in the game** until the finale's answer (§9). 07's eye is the one lens the player
ever looks through, which makes it precious.

### 1.2 Storyboard (untouched, ~43 s)
POV = the camera sits at 07's lens socket and turns with its neck. Lid % = how open 07's brow-shutter is (0 shut,
70 its half-lidded rest, 100 fully open, which happens only in the finale).

| # | Time | Picture and camera | 07's body | Sound and music | UI |
|---|---|---|---|---|---|
| O1 | 0:00–0:03 | **Darkness.** Pure black, held. | asleep: lid 0, eye dark, head bowed | 0.8 s of silence. A felted power-on thump, then the radio's crackle swells. Inside the static, a ghost of a Rhodes chord (D–A–E) surfaces for one bar and sinks again. | none |
| O2 | 0:03–0:05.5 | **Lid, stage 1.** The black lifts from the bottom of the frame: a soft-edged slit, fully blurred, filled with the dock's warm glow (the first colour in the game is warm). It stalls there. | lid 0 → 12, stall | one felted servo tick, low (A2) | none |
| O3 | 0:05.5–0:08 | **Lid, stage 2.** The lid rises to 40% with a small eased shudder (three soft wobbles of about 2%). Everything is soft: lavender ground, a dark mass to the left (the lander). | lid 12 → 40 | a second tick (D3); the servo strains a breath longer | none |
| O4 | 0:08–0:11.5 | **The lens hunts.** Focus sweeps too near (the dock pad's painted edge sharp, the world mush), overshoots to far, swings back and settles at infinity. A **dust speck** on the glass passes through sharpness during the near sweep: a soft dark disc with a pale rim, upper left. It slides slowly down the glass and out of the frame over the rest of the opening. | the neck holds still; only the lens works | a small lens-motor whine gliding E4 → F#4, a soft tick (A4) as focus lands | none |
| O5 | 0:11.5–0:12.5 | **Old electronics.** The image dips about 30% twice, one faint horizontal roll band passes, and a brief warm tint lifts. Then the picture is steady. | lid settles at 70% (its rest); eye glow steadies | 07's lamp hum cuts out for a beat and returns steady: 07 is "on" | none |
| O6 | 0:12.5–0:16.5 | **Look: the sleeping home.** The head lifts from the ground to the horizon and turns left: the dark lander, black windows, the tilted HOME sign, the slack cable. The gaze stops for a second, then drifts back 3° (unsure). | slow neck turn, a hesitation | the neck servo whirs softly (D → E) | none |
| O7 | 0:16.5–0:20 | **Look: the pilot.** The gaze turns right to the tower's dark stub. Its pilot blinks once: slow amber, fade in 0.6 s, hold, fade 0.8 s. The gaze holds through the blink. | still; glow brightens slightly with the blink | as the pilot lights, the radio's ghost chord surfaces again, a hair clearer: *the music lives there* | none |
| O8 | 0:20–0:24 | **Look: the Peak.** On across the dark basin to the Peak's red beacon, far and small. The lens re-hunts once, briefly, to the far distance. The beacon blinks. | neck turns right; a tiny lift of the head | one soft tick as the far focus lands; otherwise only static | none |
| O9 | 0:24–0:29 | **Look: Earth.** The head tilts up and Earth enters from the top edge. The lid opens wider, to 85% (wonder). The gaze settles with Earth just above centre and holds. | lid 70 → 85; the eye glow breathes | the ghost fragment surfaces for two bars, the longest and clearest so far, then sinks into the static | none |
| O10 | 0:29–0:33 | **Out of the lens.** The camera dollies backward through the glass. The iris's amber rim slides out past the frame edges, a ring of lens glass catches Earth's reflection, the hood's edge passes, and the frame resolves into the **low front three-quarter shot of the eye** (today's WakeOpening beat 1: yaw 145°, elevation −15°, 3.2 m). 07 is still looking up, and Earth is small in its lens. | holds the gaze up at Earth | the static narrows from "inside 07" to "beside 07" (the radio's speaker position) | none |
| O11 | 0:33–0:36 | **The blink.** On the lens shot. | 07 blinks once (lid 85 → 25 → 75 over 0.7 s), tilts its head 8° to one side, the antenna tip blinks the station's call sign (three short, one long; see §1.5), and the solar wing opens a little (a sigh) | a soft lid click; a small two-note "hm?" chirp, A4 falling to F#4 (a question) | none |
| O12 | 0:36–0:39 | **Over the shoulder.** As 07 turns its head toward Earth, the camera eases round to over its shoulder with Earth in the upper third (today's beat 2). | head turns to Earth | the radio settles to its dock level | none |
| O13 | 0:39–0:43 | **The chase frame** of ruling 8: 07 on its dock facing bearing 355°, Earth and the Peak sharing the frame, the camera pitched down ≤ 6°. Only three warm points burn: the dock, 07's eye and the tower's pilot (P5). | the head returns to rest; lid 70 | — | none |
| O14 | +3 s still | — | idle body language resumes | — | the first prompt fades in: **"Look up"** if the player hasn't moved the look yet, otherwise **"Drive"** |

### 1.3 Input, skip and hold rules
- **Look input (from O3 on)** steers the gaze inside the POV. It is soft-limited (±70° yaw, −15° to +50° pitch) and
  heavily damped like an old neck. The scripted attention pauses, then resumes 2 s after the last look input, from
  wherever the gaze is, with the next target not yet seen. If the player looks at Earth on their own (within 10° for
  1 s), the opening skips ahead to O9. **The player's first verb is to look, and they discover it themselves.**
- **Drive input at any time** hurries:
  - the remaining lid and focus stages compress into ≤ 1.5 s;
  - the camera pulls out of the lens straight to the chase frame over 2.5 s (O11 still happens on the way: the blink
    is never skipped);
  - 07 takes throttle once the hurry ends, so control is never held for more than ~1.5 s (as `WakeUpSequence`'s
    hurried wake does today).
- **Pause** works throughout and freezes the opening where it is.
- **Once per save.** The full opening plays on a New Game only. Today the camera rig starts it whenever a save loads
  at the Asleep stage (`RoverCameraRig.OnBasePowerChanged`), so a player who quits before tower L1 would see it again.
  It needs a fresh-game flag (§1.6).
- **Photosensitivity.** The flicker in O5 is two soft dips of ≤ 30% exposure; nothing strobes between 3 and 30 Hz. A
  1.0 Settings toggle "Gentle camera" turns the blur, the flicker and the speck off.
- **Length guard.** Untouched, the opening must stay at or under 45 s. Tune the hold times by playtest, not the beats.

### 1.4 Reconciling with ruling 8 and M3-16 P5
What already agrees:
- ruling 8: Earth and the Peak share the first gameplay frame; 07 starts with its eye closed; it wakes as the radio
  crackles on;
- P5: the wake is a stargaze; front three-quarter on the lens, then the chase frame; three warm points.

What changes: the "first frame" is now black, and the first image is 07's own view. The chase frame of ruling 8 moves
to the end of the opening, and the "aww" moves from the first five seconds to the blink after the pull-out (O11).

**Proposed text for VISION ruling 8** (replaces the whole ruling):
> 8. **The first look is the hook.** A new game opens in darkness on the radio's crackle, then through 07's own eye as
>    it opens: the lid lifts in stages, the lens hunts for focus, and 07 looks around at the sleeping home, the
>    tower's pilot blink, The Peak's red light and, last, Earth. The camera then eases out of the lens to a front
>    three-quarter shot of 07's eye looking up at Earth, and on to the chase frame: 07 on its dock facing bearing 355°,
>    Earth and The Peak (with its slow red beacon) sharing the frame, the camera pitched down no more than ~6°. Only
>    the dock, 07's eye and the tower's pilot are warm. Any drive input reaches the chase frame within ~4 s; control is
>    never held. A loaded game opens with the short wake. The first five seconds should make people lean in; 07's
>    first blink should make them say "aww". Storyboard: `docs/design/cutscenes.md` §1.

**Proposed addition to VISION "Camera direction"** (a new bullet):
> - 07's eye is the only first-person view in the game: the opening, and the finale's answer. Everything else is the
>   floaty third-person drone. The catalogue of moments is `docs/design/cutscenes.md`.

**Proposed edit to M3-16 P5, first bullet** (replace):
> - The wake opens through 07's own eye (`docs/design/cutscenes.md` §1): darkness and the radio's crackle, the lid
>   opening in stages, the lens finding focus, a look round the sleeping home, the pilot, the Peak and Earth. Then the
>   camera eases out of the lens to the front three-quarter shot on the eye, 07 blinks, and the camera eases to the
>   chase frame. The first prompt is "Look up" if the player hasn't looked yet, otherwise "Drive".

M3-16 step 0's row ("the dock's glow and 07's eye are the only warm lights") should read "the dock, 07's eye and the
tower's pilot are the only warm lights", to match P5.

### 1.5 A setup for the ending: the call sign
STORY's ending has Earth blink "three short, one long — the station's old call sign", but nothing teaches that pattern
first, so the payoff would land on a stranger. Proposal:
- 07's antenna tip (VISION: "a faint blinking tip") blinks **three short, one long** every ~12 s at rest. The player
  sees it in O11 and all game.
- The pattern also appears once in a log: Kenji's first log (see `game-flow.md` §5).
- In the finale, Earth answers with the same blink, and the player recognises it before the ticker line says a word.

### 1.6 The short wake (Continue)
- **Trigger:** a save loads, and the opening has been seen.
- **Shots:** black fades up over 1.5 s on the crackle, straight into the chase frame on the dock. 07's lid opens in
  one eased move (0.8 s), with one blink. Then 07 glances (Glance tier, 2 s) at the newest pillar on the horizon or,
  if none, at Earth.
- **Length:** ~5 s. Any input hurries it.
- **UI:** the "next" line may show once, if P8's conditions hold.

### 1.7 Build split and contracts
| Box | Builds |
|---|---|
| rover | the POV camera at the lens socket; the gaze, with its attention list (lander, tower pilot, Peak beacon, Earth) and soft limits; the lid stages, the focus and flicker curves driven through `ILensView`; the pull-out path into the existing WakeOpening beat 1; the blink, tilt, wing and antenna call sign; the input rules in §1.3 |
| world | the lens view: a post pass with a soft curved lid mask, focus distance (URP depth of field), the exposure dips and roll band, the warm tint and the dust speck. Registers `ILensView`. The radio's speaker is not world's. |
| art | the inner faces of the hood and brow, if the POV camera can see them; the lens glass ring for the pull-out (it reflects Earth); the antenna tip lamp as its own renderer |
| audio | the Asleep radio profile (static with the ghost chord surfacing on cue); the lid ticks, the lens whine, the hum dropout, the neck servo, the "hm?" chirp, all keyed to the wake beats |
| ui | nothing during the opening; then the first-prompt rule in O14 |
| gameplay | the fresh-game flag and "opening seen" in the save |

Proposed Core contracts (the Director lands them first):
- `SessionStarted(bool freshGame)`: published by Gameplay once a save is applied (fresh or loaded). Rover plays the
  opening on a fresh game and the short wake otherwise.
- `WakeBeat` enum (Crackle, LidCracked, LidHalf, FocusSettled, Flicker, GazeHome, GazePilot, GazePeak, GazeEarth,
  PulledOut, Blinked, ChaseFrame) and `RoverWakeBeatReached(WakeBeat beat, bool hurried)`: published by Rover. Audio
  voices each beat; UI waits for ChaseFrame. `RoverAwoke` stays as the crackle (O1), as today.
- `ILensView` (Lid01, FocusDistance, Flicker01, Warmth01, SpeckOffset): registered by World, driven by Rover.
- The gaze targets: Rover reads the lander, the tower pilot and the Peak beacon from world anchors, and Earth from the
  sky. Any missing anchor is added to `WorldAnchorIds` by the Director.

## 2. The tower wakes: tower levels L1, L2 and L3

All three share one pattern, so the player learns it once: **feed, stitch, the tower grows, a cable of light runs
out, a place wakes.**

**Shared beats** (Moment tier, 8–10 s, after `StationCue.HatchClosed`):

| # | Time | Picture and camera | 07 | Sound |
|---|---|---|---|---|
| T1 | 0–2 s | The stitching beam lets go. The camera eases back and up (CameraMoment, subject the tower) so the whole tower is in frame as its new section settles; it must be the tallest thing at home from L1 on. | 07 backs off half a metre, looks up the tower | the hatch latch; a low swell on D |
| T2 | 2–3.5 s | The cable of light leaves the tower's foot and runs along the ground (world's cables, ~1.5 s). The camera pans with the light's head, never ahead of it. | head follows the light | a rising run of soft plucks along the cable, D E F# A |
| T3 | 3.5–8 s | The woken place: per level, below. | per level | the place's own stinger |
| T4 | 8–10 s | Ease back to the chase frame. HUD returns. One ticker line. | a perk: head lift, brighter eye, antenna wiggle | the radio comes back up |

**Per level:**
- **L1, home wakes (the big one).**
  - T3 picture: the cable reaches the lander. The cabin windows light one by one, left to right, then the porch
    lamps. The HOME sign straightens with a creak. The **four small bunk windows stay dark**: those are the crew's,
    lit only by completing their sets (STORY). Four dark windows in a lit home are four promises.
  - T3 sound: **the radio clears.** The static melts and the station's track plays clean for the first time in the
    game, the ghost chord from the opening resolving into a whole song. The player hears progress (VISION audio).
  - 07: turns to the lander and holds a beat longer than usual.
  - Ticker: "Lumen Station: power to home."
- **L2, the bay wakes.** T3 is the bay's shutter moment (§4).
- **L3, the lift gets power.**
  - T3 picture: the cable reaches the winch. The winch's lamp lights and the jammed platform shudders once, but does
    not move. It now *can* be fixed (feed and stitch at the winch, M3-16 step 8).
  - 07: looks up the lift to the deck.
  - Sound: the old creak (`LiftCue.WinchCreaked`) answered by a soft hum.

**Skip:** any input eases out within 1 s. The windows, cable and radio finish anyway.
**Builds:** gameplay (trigger: `BasePowerChanged(stage, restored: false)`), rover (the moment's framing per stage),
world (cables, windows in order, porch lamps, pilot off at L1), art (the tower's sections, the HOME sign's straighten,
bunk windows as separate renderers), audio (the stingers, the radio's clear), ui (the ticker line).

## 3. A friend comes home (Tilly, Bell; later Atlas and Moss)
- **Trigger:** the first `FriendGreeted` for that friend; Tilly arrives with 07, Bell arrives first ("Bell got home
  before you").
- **Tier:** Moment, ~7 s.
- **Shots:**
  1. (0–2.5 s) As 07 crosses the tower's circle, the camera eases to a low three-quarter from 07's side
     (CameraMoment, subject the friend's home spot). 07 is in the foreground and home's warm points are behind.
  2. (2.5–5 s) The friend goes ahead to its spot, and the spot wakes with its own warm point: Tilly's perch lamp
     under the porch; Bell's corner at the tower's foot with her dial lamp. Home gains a light you will see from the
     dock (P6).
  3. (5–7 s) The friend's greeting: Tilly loops once round 07's head and settles **beside** it, never on its back
     (review); Bell does her two-step. Ease back.
- **07:** stops by itself, head follows the friend, eye brightens, and the wing opens a little.
- **Sound:** the friend's motif (Tilly: three high kalimba notes, F# A B; Bell: a radio jingle in D) answered by 07's
  chirp an octave lower.
- **UI:** after the moment, Bell's ticker line where it exists (STORY). Tilly gets one: "Tilly found her perch."
- **Repeats:** later homecomings use the existing greeting only, with no camera.
- **Builds:** gameplay (trigger and the first-time flag), rover (framing), art (each friend's home spot with its warm
  point), world (registers the spot as a home light), audio (motifs), ui (line).

## 4. The bay's shutter (tower L2)
- **Trigger:** `BasePowerChanged(Bay, restored: false)`, as T3 of §2.
- **Tier:** Moment, ~10 s, the longest wake after L1, because the bay is where 07 grows.
- **Shots:**
  1. (0–1.5 s) The cable runs from the tower to the bay (T2).
  2. (1.5–5 s) The camera eases to a front view of the bay over 07's shoulder. The torn, weathered shutter shudders,
     then rises in three eased steps with stalls between them, an old motor finding its strength. This rhymes with
     07's own lid in the opening.
  3. (5–7.5 s) Inside, work lamps flicker on back to front. The "07" sign flickers twice and fails, then holds (the
     same flicker as O5: these two machines are kin).
  4. (7.5–10 s) The gantry arms wake: they unfold, sweep once and settle at ready. The turntable turns a quarter. The
     plotter's pen carriage homes with a little zip. Ease back.
- **07:** reads its own number: a small head tilt at the sign, then a lift. The only time 07 "recognises" something.
- **Sound:** the shutter's rattle, soft and in tempo with the radio; the sign's hum resolving onto A; the arms'
  whir climbing D E F# A; the turntable's clunk on D.
- **UI:** after it, the place card "Kenji's Rover Bay: fits kits on 07" (P4: shown when its job is available).
- **Builds:** gameplay (trigger), rover (framing), art (shutter steps, sign flicker states, arm wake pose, plotter
  homing), world (bay lamps as home lights), audio (stingers).

## 5. The first kit install (Hover-Jump)
- **Trigger:** the first `RoverBayFitting` of the save. Later fittings play the existing ~3 s install moment (DESIGN
  "Install moment"); gifts play the soft version as 07 comes home.
- **Tier:** Moment, ~7 s, the long version once.
- **Shots:**
  1. (0–1.5 s) The bay's lamps dim to one work light on the turntable. BayShot frames 07 side-on.
  2. (1.5–4 s) The turntable turns 07 to show its flank. The arm lowers the coils from the rack; sparks as the beam
     stitches.
  3. (4–5.5 s) The coils settle under the belly and glow once, from dim to the charged glow.
  4. (5.5–7 s) 07's proud pose: head lift, brighter eye, antenna wiggle. The turntable completes the turn, facing
     07 out of the bay toward the chasm it has already seen (P2). The work lights come back up. Ease to chase.
- **Sound:** the arm's whir, the stitch, a "fitted" clunk on D, then a rising D-F#-A-D' figure as the coils glow.
- **UI:** after it, the prompt for the new verb ("Jump", glyph + one word), shown near the first good place to try it.
- **Builds:** gameplay (`RoverBayFitting`), rover (BayShot timing, the pose), art (work-light state), audio.

## 6. The lift's lookout (M3-16 step 8, and M3-07)
- **Trigger:** the first ride up after the lift is restored (`LiftCued.RideStarted`, first time).
- **Tier:** Moment for the ride; the lookout holds while 07 is still (like the stargaze).
- **Shots:**
  1. **The ride** (~6 s): a crane shot that rises with the platform along the lander's flank. 07 is in profile, the
     weathered panels slide past, and the crew's ladder is beside it: the ladder 07 could never climb.
  2. **The rail** (~3 s): at the clunk of `RideStopped`, 07 rolls forward to the deck's rail. The camera eases behind
     and above it and widens.
  3. **The lookout** (hold): the whole basin. Lit masts, the cleaned sites' skeletons, Kestrel's long trail, the
     canyon's slit and the Peak. 07 scans slowly left to right, and the lens re-hunts once at the far rim (the
     opening's rhyme). Look input pans the view; drive input ends the hold.
  4. **The hatch** (0.5, M3-07): the first time, after 4 s of stillness, 07 turns its head toward the lander's deck
     hatch, open a crack with warm light. That is the only invitation to the map (see the M3-07 draft).
- **Sound:** the ride's ratchet and hum (`LiftCue`), the clunk. At the top the radio is a touch clearer and the room
  tone wider; the music thins to its pads.
- **UI:** nothing. The place card for the lift has already shown at L3.
- **Builds:** rover (the ride and lookout shots, already in its queue), gameplay (the ride and first-time flag), art
  (deck rail, hatch with its warm crack), audio (exists), world (the hatch's warm light).

## 7. The dock's evening (M3-16 P6)
- **Trigger:** `DockEveningOpened` (on main), at most once per return home when something new woke or was found.
- **Tier:** Moment, ~8 s.
- **Shots:**
  1. (0–3 s) 07 settles on the dock. The camera eases up and back to a wide, slightly high shot of home from the dock
     side, 07 small in the foreground.
  2. (3–5 s) Each of today's new lights pulses once, in the order they woke (window, shelf lamp, a friend's spot, a
     mast on the horizon). Friends drift to their spots.
  3. (5–7 s) The tower's listening sweep: a soft warm ring expands from the tower's top over the horizon, and **one**
     new faint pillar rises far away (the next site, a friend's chirp or a dark relay). The camera pans gently toward
     it and holds.
  4. (7–8 s) Ease back to the chase frame.
- **07:** eases onto the dock, its wing opens wide (a long sigh), the eye half-lids, and its head turns toward the new
  pillar when it rises.
- **Sound:** the evening swell (`dock_evening_swell`, generated). The sweep is a slow kalimba arpeggio D F# A B D'; the
  pillar answers with a high E, the relic-answer colour.
- **UI:** after it, the "next" line once (P8), phrased as a sense: "Something is calling from the west." The game
  saves as the shot begins.
- **Skip:** any input eases out within 1 s. The pillar rises regardless.
- **Builds:** gameplay (trigger, the sweep and choosing the pillar), rover (the wide shot), world (the "new today"
  pulse on `IHomeLights`), audio (swell and sweep, wiring in flight), ui (the line).

## 8. Smaller moments already built or systemic (keep, don't grow)
| Moment | Trigger | Tier | Note |
|---|---|---|---|
| Relic surfaces | `RelicSurfaced` | Glance → held while the beam is on | exists (CameraMoment) |
| First memory on the shelf | first `RelicDeposited` | Moment, 5 s | the shelf wakes (M3-16 step 4): its lamp lights, the memory card follows |
| A crew window lights | a crew set completed (24 relics, 4 sets, from 0.6) | Moment, 6 s | from the dock side: one bunk window lights; that crew member's final log card follows |
| A relay mast lights | `RelayRestored` | Moment, 5 s, the first only | the camera looks home along the link; the chain of lights is pillar 6's "warm points in a cold field" |
| Radio-hop | `RadioHopStarted/Finished` | fade through static | exists; the only dark transition besides the opening |
| Stargazing and the wide shot | stillness | systemic | exist (M3-10, M3-15 §5); no moment may start during them |
| First leap over the chasm | `RoverJumped` near the chasm | none | gameplay camera only. A first leap must stay in the player's hands. |
| First arrival in a region | entering the region | Glance | its name once, as a place card (no title screen card) |

## 9. The finale: the broadcast, and Earth's answer (0.8)
- **Trigger:** the great dish is restored (Atlas lifted it, the relay chain reaches the Peak's foot), and 07 parks
  at the dish's rover-height service port. The prompt is "Broadcast", the player's choice, never automatic. The
  player can drive away and come back; the dish waits.
- **Tier:** Ceremony, about 3 min with the song, credits after.

| # | Picture and camera | 07 | Sound and music |
|---|---|---|---|
| F1 | **The dish turns** (~8 s). From low beside 07, the dish swings on its mount toward Earth, groaning. The Peak's red beacon, which the player has looked at the whole game, fades from red to warm amber: the Peak becomes a home light. | 07 watches the dish, then the beacon | a long low creak; the beacon's colour change on a held D |
| F2 | **The chain** (~12 s). The camera rises and pulls back over the rim. From the Peak, every lit mast pulses in turn down across the basin to home, and home's tower answers. The whole network is a line of warm points in a cold field. | small, far below | each mast a note of a slow descending phrase, ending on the tower's low D |
| F3 | **The song** (the reserved final track, Ro's "for when we come home", Side B of the mixtape). The camera returns to 07 at the dish and slowly pushes past its head toward Earth over the song's first minute. For the first time the radio is clear everywhere, with no static at any distance. | looks up at Earth; the wing fully open | the final track, then its fade |
| F4 | **Silence** (~6 s). Earth, held. The song has ended; only the soft room tone. | still | near silence |
| F5 | **Into the lens.** The camera pushes into 07's lens, the reverse of O10, and the player sees through 07's eye again. The lens hunts for focus on Earth's night side. | lid 70 | the lens whine from O4, once |
| F6 | **The answer.** One small light on Earth's night side blinks: three short, one long. The lid opens to **100% for the first and only time.** | lid 70 → 100 | each blink a soft high E; the long one a held A |
| F7 | **Out of the lens** (the O10 move). 07's antenna answers with the same blink. | antenna: three short, one long | 07's chirp, a rising A to D', the question from O11 answered |
| F8 | **The line.** "I heard you, 07. I'm still here. — Ro." The only UI in the finale, as a ticker line. | eye at its brightest | the radio returns, warm |
| F9 | **Credits** roll over the radio's playlist, small and slow, over a very slow drift round 07 and the dish. | idle, looking at Earth | the station plays on |
| F10 | **After.** Ease to the chase frame on the Peak. The HUD returns. The game saves as "after the ending". Free roam (see `game-flow.md` §7). | — | — |

- **Skip and hold:** F1–F8 cannot be skipped the first time; Pause works. Credits can be left by holding Pause for 2 s
  (a small filling ring is the only UI). Later, the scene replays from the title screen's collections (M3-09 draft) and
  is skippable.
- **Builds (0.8):** gameplay (trigger, the after-ending save state), rover (every shot, the second POV), world
  (beacon colour, the chain pulse, Earth's night-side light), art (the dish and its mount, the service port), music
  (the final track), audio (stingers, the silence), ui (the line, credits, the hold ring).
- **Core (0.8):** `BroadcastStarted`, `EarthAnswered`, `CreditsFinished`.

## 10. Proposed changes, in one list
1. VISION ruling 8: replace with the text in §1.4.
2. VISION camera direction: add the first-person bullet in §1.4.
3. M3-16 P5, first bullet, and step 0's warm points: the edits in §1.4.
4. STORY "The ending": add the call sign's setup (§1.5) and the beacon turning amber (F1).
5. Core: `SessionStarted`, `WakeBeat` and `RoverWakeBeatReached`, `ILensView` (0.5); `BroadcastStarted`,
   `EarthAnswered`, `CreditsFinished` (0.8).
6. Optional, to keep moments cheap: one Rover event, `CinematicChanged(kind, active)`, so UI fades the HUD and audio
   ducks the radio for every moment without new wiring per moment. Today each moment has its own event (stargazing,
   wide shot, bay fitting).
