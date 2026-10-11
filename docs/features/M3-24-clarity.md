# M3-24: Clarity, the first hour reads (0.5.1)

> Owner: Director. Version: **0.5.1**, a patch before 0.6 opens. Source: the owner's play-through of 0.5.0
> (2026-10-12): "the UI breaks on my screen ratio; do the loose pieces on the ground make sense?; it's very hard to tell
> the game's purpose, how to craft, what the radio tower is for; the lines can't be understood, they need clear sound;
> the lofi music shouldn't be on from the start, it should be earned." Rulings 6, 11, 15, 16 and pillar 6 apply.

## Owner decisions (2026-10-12)
1. **Lines:** hints are clear text; **Ro's recordings and the automated stations get a real voice**, generated offline by
   a TTS engine (en + vi) and filtered like an old tape or radio. The story is "everything is a recording" (GDD v3 §13).
2. **Loose pieces on the ground are removed.** Materials come only from wreck sites; the sites' counts rise to keep P9's
   2x economy.
3. **Music is earned in two steps:** the game starts with static and the room tone only. (1) Tilly's glint is an old
   **tape deck**: it is fitted on 07 (a visible part, rulings 11 and §13.4) and plays the first tape softly. (2) Bell's
   dial (the canyon) brings the lofi **station** with its tracks. Tower L1 only clears the static into the Earth
   signal's carrier (no music).

## A. The UI fits every screen
Title, pause, settings, HUD and the lab HUD lay out correctly at 16:9, 16:10, 21:9, 32:9 and 4:3 (and 1280x720 to
3840x2160): anchored to a safe area, scaled by height, never clipped. A UiCapture per ratio proves it.

## B. Purpose reads in the first minutes
- **The protocol readout:** at the end of the opening, after the boot readout, 07's lens shows the recovery protocol
  for ~4 s: the four targets with Power first and its first need ("POWER THE STATION ▸ the radio tower"), then it fades.
  The same panel is the lab's recovery panel later.
- **Teaching lines show at once:** during teaching steps 0-5 the next line appears as soon as 07 stops (no ~2 min wait;
  P8's wait applies after step 5).
- **Every place says its job when first near,** in one clear line (M3-16 rule 3), and the radio tower shows what it
  powers: on first approach, a slow light runs from the tower's foot along each dark cable to the lander, the bay and
  the chart, then the line "The station's heart: each level powers more of home."
- **Recipes are visible:** looking at a hopper or a board sheet shows its recipe card (materials with have/need, the
  component, the blueprint) in the HUD.
## C. Sound
- The voice pipeline (offline TTS → a tape/radio filter → clips per line, en and vi), for Ro's letters and recordings and
  the automated station lines; subtitles stay.
- The music gating above; the tape deck's part on 07 and its fit moment.

## Split (three slices)
| Slice | Delivers |
|---|---|
| ui-fit | A |
| clarity | B, the loose pieces removed with the economy rebalanced, the tape deck find and fit (gameplay, art, rover) |
| voice | C: the TTS pipeline (research a voice with a licence that allows a commercial release, ask the owner before any download), the lines' clips, the music gating in Audio |

## Acceptance
- The owner can say, after 5 minutes of a fresh game, what the game wants first and what the tower is for.
- No clipping at any listed ratio. No loose pickup on open ground. No music before the tape deck.
- Ro's first letter is understood when heard, in English and in Vietnamese.
