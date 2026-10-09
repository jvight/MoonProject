# M3-11 — Visible progression: kit on 07, crafted at Kenji's bench

> Owner: Director (design). Owner priority (2026-10-08): every upgrade must show (VISION ruling 11, DESIGN "Visible
> progression"). M3-13's materials give the bench something to craft. This feature gives the player kit to craft and
> see. 07 goes from tired to cared-for and never menacing.

## Scope for M3
Three new comfort upgrades that need no new region, so the crafting loop has meaning now. Each one has one readable
kit piece. On top of that come the visible friend gifts and the install moment.

| Upgrade | Recipe (indicative) | What it does | What you see on 07 |
|---|---|---|---|
| **Warm Headlamp** | Optics-heavy | wider, warmer headlamp pool; the sonar pillar of a site 07 faces stays a little longer | a caged lamp bar across the front (`HeadlampSocket`) |
| **Boost Coils** | Wiring-heavy | hold Drive forward on flat ground for a gentle extra cruise speed with a soft hum; never a race | twin capacitor drums on the flanks that glow while boosting (new `DrumSocket_L/R`) |
| **Cargo Cradle** | Metal-heavy | one relic rides home in a rear rack instead of being towed; the tether stays for big salvage pieces | a strapped rear rack basket (`CargoSocket`); a carried relic sits in it |

Hover-Jump's coils already show. Later upgrades follow the same rule: Magnetic Treads brings bigger treaded wheels,
and Wide Sonar brings a sonar array and a straightened antenna.

## Friend gifts (visible, no new mechanics)
- **Tilly home:** 07's missing solar cell is replaced (the wing's gap fills in) and the wing opens fully when idle.
- **Bell home:** the faded "07" is repainted fresh by Ro's stencil, and a small radio pennant appears on the antenna.
- Moss and Atlas (M4) will add a painted flower on the patch and a tow hook.

## The install moment (about 3 s, eased, never a cut)
At Kenji's bench, confirming a craft:
1. The camera eases to a low three-quarter view of 07.
2. Sparks fly from the bench's SparkSocket.
3. The kit piece settles onto 07 with a small overshoot.
4. 07 does a proud pose: head lifts, eye brightens, antenna wiggles.
5. The camera returns.

Audio uses the workbench cue plus a new "fitted" clunk. A friend gift gets a softer version when 07 next comes home.

## Contracts
- **Art.** Kit prefabs with mount pivots for the rig sockets, the new `DrumSocket_L/R` on RoverModel, the wing's
  filled cell as a variant node, the fresh "07" decal node, and the pennant.
- **Core.** `RoverAbility` already lists WarmHeadlamp, BoostCoils and CargoCradle. Add `RoverKitChanged`, or reuse
  UpgradePurchased plus friend events.
- **Rover.** Shows and hides kit by `IRoverAbilities` and friend state, plays the install moment, drives the
  headlamp, boost and cradle behaviours, and handles drum glow.
- **Gameplay.** Upgrade definitions and recipes for the three upgrades at Kenji's bench, the cradle's relic carry
  (deposit from the rack), and ruling 5 kept.
- **UI.** Recipe cards at the bench (exists) and the ability names.
- **Audio.** Boost hum, the fitted clunk, and the headlamp's warm switch-on.

## Acceptance
- A screenshot after crafting all three looks clearly different from minute one, from the default camera and at 30 m.
- Each kit piece reads in silhouette. 07 still reads as the same gentle one-eyed rover.
- The playthrough crafts at least one of the new upgrades and sees it on 07.
