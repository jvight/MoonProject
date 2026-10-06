# M3-03 — Workshop + Hover-Jump

> Owner: Director (design). Implements the "Workshop" and "A moon that opens up" parts of `docs/DESIGN.md`.
> Design rulings in `docs/VISION.md` apply: no fail states, nothing lost, generous, eased.

## Player experience
1. **The workbench.** Next to the lander stands Kenji's old workbench (a second shop pad, like the tower's). Driving
   onto it shows what 07 could become — first offer: **Hover-Jump**. It costs a lot of scrap (this is where the slice's
   surplus finally matters) and reads, in plain words, "Leap over gaps".
2. **Buying it.** Hold-to-confirm like the tower; 07 gets a little upgrade moment (sparks from the bench, a bounce, a
   perk-up), and a small set of spring coils appears on 07's underside.
3. **The leap.** Hold Jump (Left Shift / RB): 07 crouches (suspension compresses, eye squints with effort, a rising
   hum), release: a big, floaty, controllable lunar leap — ~6–8 m high at full charge, carrying forward speed, with a
   soft air-steer and a gentle, cushioned landing (dust ring, happy perk). A short tap gives a small hop. No fall
   damage, ever; landing on a slope is always fine.
4. **The promise fulfilled.** Since the first session the player has seen, from the base, a glinting ledge across a
   chasm at the mouth of a canyon in the rim (M3-04). Now they can reach it.

## Rules
- Charge time to full ≈ 0.8 s; jump strength eases with charge; a fully charged jump clears a ~12 m gap at top speed.
- Usable only when grounded; a cooldown short enough to chain hops for fun (~0.4 s after landing).
- Air control: gentle (≈20 % of ground steering); the camera lifts slightly with height and frames the landing.
- 07 can never be stranded: stuck recovery (M1-21) already covers awkward landings; landing on boulders/props is fine.
- Holding Jump while not owning the ability does nothing (no prompt) — the workbench is where it's taught.

## Systems & split
| Box | Delivers |
|---|---|
| rover | implement `IRoverAbilities` (register next to IRoverRig); Hover-Jump mechanic (charge, leap, air-steer, landing), its body language (crouch, squint, joy), camera lift, coil visual hook; events `RoverJumpCharged(strength)` / `RoverJumped(strength)` in Core/Events/RoverEvents.cs; feel metrics (heights, distances, hang time) |
| gameplay | generalise upgrades: a Workshop station (second shop pad) selling rover abilities through `IUpgradeShop`; definition `rover.hover_jump` (cost ~150 scrap); grant via `IRoverAbilities` on purchase and on load; save |
| art | Kenji's workbench + pad (tools, a vice, a lamp, a wrench rack, a sticker "07 PIT CREW"), hover-jump coil parts for 07's underside (`CoilSocket` contract addition) |
| audio | charge hum (rising, in key), leap whoosh + spring "boing" (soft), airborne wind, landing cushion; workbench purchase sparks |
| ui | Jump glyph/prompt after purchase (first few times), workbench offer panel reusing the tower panel |
| world | M3-04: canyon in the rim + the chasm and the glinting ledge visible from base |
