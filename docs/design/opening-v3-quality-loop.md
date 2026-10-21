# Opening v3 — quality loop (2026-10-20)

The owner's bar: "nếu chúng ta không thể làm tốt được ngay đoạn mở đầu này thì không thể đi xa hơn được."
Nothing past the opening (5.2b onward, caravan, regions) gets new work until this loop is green twice in a row.

## 1. What "good" means here

1. **Never lost.** Within 5 s (game time) of every success, a readable next action is on the real camera's screen:
   a key chip (on its object, or on 07 when the object is off-screen, with 07 looking toward it), a short CRT line,
   or a lit world cue in view. One guidance element at a time; plain text.
2. **Feels like a machine.** Every boot, load, fit, lock and unlock is a visible mechanical action with its sound, at a
   speed a person can read. Nothing looks sped up or skipped.
3. **Calm camera.** No tours, no fast swings; the player keeps control except for short, skippable moments.
4. **Purpose reads.** Every object's job is obvious without text (purpose-must-read).

## 2. Open work, in order

| # | Item | Owner | Status |
|---|------|-------|--------|
| Q1 | Off-screen first-use chips show on 07 and 07 looks toward the object; Build chip within 1 s of the pad becoming buildable; "what's missing" when not affordable; stall reminder 120 s → ~25 s | story | in progress |
| Q2 | Beat-by-beat guidance audit B0 → R11, from the camera's view, ranked by severity | GD | in progress |
| Q3 | `--naive` bot gate: per beat, time-to-visible-cue ≤ 5 s with the camera untouched; screenshots at +0 / +5 s | tools | in progress |
| Q4 | The wake at human speed: power-on ≈ 1.2 s, logo ≈ 4 s (plotter strokes, typed wordmark, ≥ 1.2 s readable hold, afterglow), load ≈ 2.4 s in 5 discrete steps; no input leak into the fast-forward | rover (timing), ui-fit (face), audio (cue points) | in progress |
| Q5 | Range ring: thin, dim, fades after the reveal; never the brightest thing at night | rover | queued after Q4 |
| Q6 | Rack outlines readable in daylight from the chase cam | ui-fit | queued |
| Q7 | Every gap from Q2, fixed by its domain | per audit | after Q2 |

## 3. Test flow for every owner build

1. Lands → `tools/autoplay.py --build --until R10` (strict) **and** `--naive` → both PASS.
2. `director.py verify --parallel --push` → VERIFIED.
3. The Director reads the naive report's per-beat screenshots (+0 s / +5 s) as a first-time player and writes down,
   per beat, what tells the player what to do next. Any beat without an answer blocks the build.
4. Build (`tools/build_player.py`, from the director worktree), hand over with the run command and an honest list of
   what changed and what is still known-bad.

## 4. Done when

- `--naive` is green B0 → R10 on two consecutive builds.
- The guidance audit has no "blocks" or "delays" rows left.
- The owner plays the opening start to R10 without asking what to do next.
