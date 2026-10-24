# Opening v3: feel audit, feature by feature

> **Author:** the game design session, 2026-10-24.
>
> **Owner:** "Những chức năng nào bạn đánh giá chưa ổn thì hãy sửa tiếp, khi nào feeling từng chức năng nhỏ ổn nhất
> thì thôi." (Keep fixing every feature that isn't right yet, until each small feature feels its best.)
>
> **Bar:** the VISION feel checklist, plus the owner's standing rules:
> - solitude and calm;
> - polish every state;
> - purpose must read;
> - real devices, never beams;
> - vacuum shapes;
> - no hole patterns;
> - calm camera;
> - plain text;
> - never lost.
>
> **Evidence:**
> - the IL2CPP bot run `director/Logs/autoplay/n3fe1350f7` (build 3fe1350f7; 93 frames, naive checks at +0 and +5 s);
> - the R12 runs `agy-brands/Logs/autoplay/hl-strict2`;
> - the owner feedback in memory.
>
> **Limit:** stills cannot show motion, timing or sound, so the scan beam, the cut, driving weight and sound, Tab fit,
> transmit and the audio mix are marked **not rated** below. They need a short screen-and-sound capture each, or the
> Director's listening pass. I did not guess.

## 0. Player experience
The opening's hook (the call) and its far goal now read. What drags the feel down is a set of small **breaks of
trust**:
- a tow with no visible rope;
- a debug-looking line across the plain;
- a countdown on a calm screen;
- a line that says "return to your pad" while 07 is charging on a pad.

Each one tells the player "this is a game being assembled", not "this is a place". Calm needs confidence; these are
the leaks.

## 1. Ratings, worst first
| # | Feature | Score | Evidence (frame) | The single most important problem | Concrete fix | Domain |
|---|---|---|---|---|---|---|
| 1 | **Tow: hook, rope, strain, release** | **2** | t0520: the module trails about 15 m behind 07 with **no cable visible**; the line still says "RMB … Tow it home" while towing | The rope doesn't read at chase distance, so the load seems to follow by magic: the "no beam" rule broken by invisibility | Draw the cable thick enough for a minimum screen width (about 3 px at 1440p) with a metal sheen. Show a taut/slack sag, the drum spin and skid dust at the load. Clear the hook line the moment the hook takes. | Rover (cable look) + Art (cable material) + Gameplay (clear the line) |
| 2 | **Range ring** | **2** | B12+5 s: a hard, thin **white line** across the whole plain and lander, staying in the world | It reads as a debug line or a road, not 07's sense of range. It also cuts through the night composition. | Show it **once**, as a soft amber dotted glow on the ground that eases out in about 4 s. After that, show it only on 07's CRT map page. Never leave it standing in the world. | UI + World |
| 3 | **Sun HUD ("SUN SETS IN 2 MIN")** | **2** | B9a+5 s and t0520: a minute countdown top-right, its label clipped by its own arc | A countdown on a calm screen adds pressure. It is also **false**: dusk is gated on progress (docking after a site visit), not on time. | Remove the minutes. Keep only the small arc glyph with the Sun's position, or nothing: the sky already shows it. Fix the clipping. | UI |
| 4 | **Next line and chip guidance** | **3** | B4+5 s is good (the B glyph, purpose). But: B10, B12 and B4b lines are faint on bright sky; the line goes **stale** (still "Tow it home" while towing; still "Assemble the headlamp" after it's worn); R10-done shows "**Battery low. Return to your pad.**" while 07 charges on relay.1's pad; stall reminders fired in 8 of 13 beats | The guidance is right but **untrustworthy**: stale and contradictory lines teach players to ignore it | One contrast style (a panel of fixed opacity, never the faded state while active). A step's line clears on the verb that completes it. The low-battery line stays silent while any charger is in reach or charging. | UI (style) + Gameplay (clearing, battery logic) |
| 5 | **Build mode: menu, ghost, place, weld** | **3** | B4b+5 s: the ghost floats **above 07's head**, the apron is bottom-left under the battery widget, and the cost panel and the line show together | The player has to search for where the thing goes, and two guidance elements compete | The ghost starts **snapped on the nearest valid apron**, and the camera eases to frame 07 and the apron. Hide the line while the cost panel shows (one element). Keep the apron clear of the HUD's safe area. | Gameplay (BuildField) + Rover (framing) + UI |
| 6 | **Day and night look** | **3** | Night at B10 is **beautiful** (Earth, the Milky Way, the warm base). But at B12 ("Earthlit") the polar Sun is a **huge white disc sitting on the Peak** and the plain is lit like day. | The low Sun reads as a ball balanced on the mountain. It is oversized and blown out, and it fights the night mood at the answer beat. | Sun disc at true angular size (about 0.5°) with capped bloom. At the answer beat, keep the sky night-dark with the Sun below the rim, or at least off the Peak's silhouette. | World (sky) + Art |
| 7 | **Driving dust** | **3** | t0240 and t0060: pale blue-white puffs that **hang and drift** behind the wheels like smoke | Vacuum logic: regolith thrown up falls on a clean arc with no cloud and no drift (owner's vacuum rule) | Ballistic particles: gravity 1.62 m/s², no drag, short life, ground-coloured (warm grey, not blue). A fine sheet on hard turns. | Rover (Fx) + Art |
| 8 | **Camera** | **3** | Calm in drives and at night. But B7c+5 s has a cut solar panel filling the foreground between the lens and 07; build mode doesn't frame the target (row 5). | Props that come between the camera and 07 block the view | Fade or soften props within about 3 m of the lens. Bias the frame toward the current target after a success (the audit's G5, already planned). | Rover |
| 9 | **The relay.1 repair** | **3** | R10-done: the relay lamp is a large blown-out bloom ball; the contradicting battery line (row 4) | The payoff light is a fuzzy blob, not a lamp | Bloom capped, so the lamp reads as a lamp with a sharp core and a small halo. A short slow pulse when relit (the network pulse). | World + Art |
| 10 | **The rack and the headlamp assembly** | **3** (provisional) | R12-done: the rack is a plain cabinet beside the pad; the headlamp on 07 doesn't read at chase distance; the line stays after the fit | The upgrade isn't *visible* on 07 (ruling 11), and nothing marks the moment | When fitted, a weld-flash beat and 07 flicking the new lamp on. The long, narrow white beam (per maker-understanding) must be clearly different from the stock lamps. Clear the line. | Rover + Art + Gameplay |
| 11 | **Pick-up** | **4** | B4+5 s: `Metal +4 · for the charging pad` is clear and has a purpose | At B7c the "+ Spare igniter" notice sits low-contrast over the habitat and is clipped | Pickup notices take the same panel style as the line (row 4) | UI |
| 12 | **Dock and charge** | **4** | B6: the dock close-up with its glowing ring is strong; the pad's walls frame 07 (t0140) | — (polish: let the lock beat sound physical; check with sound) | — | — |
| 13 | **The wake (boot face)** | **4** | The POST frames read cleanly; the hand-over faces the lander with its beacon | The bot's B0 stall reminder fired while it was looking around: watch a human's first 10 s | none from stills; confirm in the Director's play | — |
| — | **Scanning** (beam, face, sound) | not rated | the chips `E Scan 1/2` are fine | stills don't show the beam or the face; capture needed | — | — |
| — | **Cut** | not rated | the sputter line and the rack line are fine | needs a capture: the owner's earlier "charge too fast, weightless" (memory) must be rechecked | — | — |
| — | **Tab fit** | not rated | no frame of the Tab screen | capture with a real mouse at 16:9 and 21:9 (memory: bot misses mouse) | — | — |
| — | **Transmit and answer** | not rated | B12 shows the follow-up line, not the pulse | needs a capture of the pulse and relay.1's blink | — | — |
| — | **Driving weight and sound; the audio mix** | not rated | — | no audio evidence. The last owner verdict (0.5.5) was "too electronic, beepy, not physical". A listening pass is needed against the Sonniss rework. | — | — |

## 2. What to do next
- **Fix rows 1–4 first.** They are cheap and they are trust breaks.
- **Then rows 5–10.**
- **In parallel, capture the not-rated features** (about 10 s each with sound) so the next pass can score them:
  scan, cut, Tab fit, transmit, the drive sound, the mix.

## 3. Playtest signal
In the Director's first-time play:
- nobody asks "how is it pulling that?";
- nobody reads the white line as a road;
- nobody hurries because of the sun;
- nobody ignores a line after it contradicted itself.

Every feature here reaches 4 or more before the opening is called done.
