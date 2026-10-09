# M3-14 — Built for 07: logic and scale pass

> Owner: Director (design). Owner direction (2026-10-08): "a robot has no hands; how can it use a crafting bench?
> Redesign everything so it is logical." VISION rulings 12 (built for a rover), 13 (true scale) and 14 (07 has no
> hands) apply. DESIGN "How 07 does things" is the reference table. This is a correction pass, so it takes priority
> over new content.

## 1. Kenji's Rover Bay replaces the workbench
- **Art: a drive-in service bay.**
  - A sheltered frame open at the front, with a ramp up to a turntable pad.
  - Two or three articulated gantry arms hang from the frame (sockets for the parts, welding tips).
  - A material hopper at rover height beside the entrance; 07's beam feeds it.
  - Work lamps and a cable reel.
  - Kenji's old human workbench stays beside the bay as a remnant: tools in outline on the pegboard, his mug.
  - Weathered per ruling 12. Sized for 07, which is 2.3 m long, with clearance.
- **Gameplay.**
  - The station is the bay. 07 parks on its turntable and the choosing panel opens there.
  - Crafting is: 07's beam feeds the hopper (a short beam to the hopper with the materials flying in), then the
    install moment.
  - Station name key: `ui.station.workshop` becomes "Kenji's Rover Bay" / "Trạm sửa xe của Kenji".
- **Rover and art: the install moment.** The bay's arms lower the kit piece onto its socket with welding sparks at
  the tips, the turntable turns 07 a little to show the piece, and 07 does its proud pose. The kit's existing
  settle animation is driven by the arm's placement.
- **UI.** Station header and copy. The tower's "The signal grows" line belongs to the tower only, and the bay gets
  its own line (e.g. "Fitted to 07").

## 2. Radio tower: rover-height service port
- **Art.** Each tower stage gets a service port at its foot: a hopper and a hatch at rover height facing the pad.
- **Gameplay.** Upgrading is: 07's beam feeds the hopper, then its beam stitches up the tower while the new section
  rises.

## 3. Other "how does 07 do it" fixes
- **Bell's dial.** On Tune, 07's beam taps the dial's knob, then Bell turns it with a wiggle.
- **Charging dock.** It replaces the "HOME" doormat: a low pad with a charging post and contacts at 07's height.
  07 rests here when parked at home: its lamp dims and a soft charging glow shows.
- **Cable lift.** A winch platform beside the crew's ladder, up to the lander deck. The art lands now; M3-07's map
  uses it.

## 4. Scale fixes (ruling 13; measured 2026-10-08)
| Item | Now | Target |
|---|---|---|
| Rubber duck | 0.73 m tall | ~0.20 m |
| Teapot | 1.18 m long | ~0.35 m |
| Garden gnome | 1.0 m | ~0.5 m |
| Cassette player | 0.80 m | ~0.25 m |
| Field boot | 0.75 m | ~0.40 m |
| Golden record | 0.95 m | ~0.40 m |
| Cassettes | 0.35 m | ~0.18 m |

- **Gameplay re-check:**
  - relic physics, colliders and mass (excavation time scales with mass);
  - tether and aim on smaller targets (keep the generous cone);
  - museum shelf slots;
  - the Cargo Cradle seat;
  - pillar and glint readability at distance.
- **Human-scale audit.** Check the lander's door and ladder, the old bench, the shelves and the cargo pods against a
  1.75 m person. Captures include a reference silhouette, in captures only.

## 5. Finish fixes
- **The spare wheel.** It now reads as a render error (a pale metal tyre under moonlight). Make it dark rubber like
  the others, with a mismatched sage hub and a wrap of faded tape, so the difference is clearly a patched-on spare.
- **Capacitor drums.** Raise them ~6 cm to clear the middle wheel's suspension travel (0.16 m).
- **Kit identity.** `RoverKitFitted` carries the ability, so UI names kit by identity rather than purchase order.

## Split (one box at a time, in this order)
| Box | Delivers |
|---|---|
| art | Rover Bay with arm rigs, tower service ports, charging dock, cable lift, relic and cassette rescale, spare wheel, drum raise |
| gameplay | bay station and hopper feeding, the tower port flow, relic physics and shelf re-check, Bell's dial tap, the dock rest |
| rover | the install moment driven by the bay's arms, rest-at-dock behaviour, kit identity on RoverKitFitted |
| audio | arm servos and welds, hopper feed, the dock's charging hum |
| ui | station names and lines |

## Acceptance
- Every interaction in DESIGN "How 07 does things" is visibly true in the game.
- Side-by-side captures against a 1.75 m reference show the crew's things at plausible size.
- No part of 07 looks like a rendering error.
