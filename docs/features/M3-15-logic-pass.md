# M3-15: logic pass after the 0.4.0 review (0.4.1)

> Owner: Director (design). The owner played 0.4.0 on 2026-10-09 and asked: "why can the robot already jump at the start?
> Why does the lift look like that, does it do anything? Check carefully." The Director's audit used the owner's
> screenshot and a fresh golden-path playthrough. VISION rulings 11, 12 and 14 apply.

## 1. Every playtest starts where the player expects
- **Finding.** 0.4.0 loaded the owner's save from 0.2/0.3. That save already had Hover-Jump and tower level 2, and
  still carried the removed scrap section. The game has no way to start over, so a returning tester sees abilities
  they never earned in this version.
- **Fix (gameplay + ui).**
  - The pause menu gets **New game**, with a calm confirm. The current save is kept as `<slot>.old.json` and never
    deleted.
  - A save written before the salvage era (it has a `gameplay.scrap` section, or no `gameplay.salvage`) is not
    loaded. The game starts fresh, keeps the old file the same way and shows one quiet line: "An older journey was
    put away."
  - Saves carry the content version (the bundle version that wrote them), so later breaking changes can do the same.

## 2. The crew's cable lift gets its job now
- **Finding.** The lift has hung "jammed halfway" since M3-14, waiting for M3-07. In play it reads as an unexplained
  grey platform: not visibly jammed, no hint, nothing to do. That breaks "everything is logical".
- **Fix: a restoration step with a payoff.**
  - **Art.** Make the jam readable. The platform hangs tilted on one cable. The other cable has snapped and lies
    coiled on the dust. Rust runs down the posts, and the winch has a hopper at rover height facing a pad, like the
    tower's port.
  - **Art: restored state.** Both cables are taut, the platform is level at the bottom stop, and a small lamp on the
    winch is lit.
  - **Gameplay.** It is a station:
    1. 07 feeds the winch hopper (a little Metal and Wiring).
    2. Its beam stitches the winch while the platform settles level.
    3. From then on, 07 parks on the platform, the platform rises to the lander's deck and 07 can look out.
  - **The lookout.** At the top, the camera eases into a wide shot over the basin. Discovered sites, lit relays and
    home's warm points glow in the dusk. It is quiet: the music thins and the wind stays. Driving off the platform's
    front, or any input, brings the platform down. This is the "alone, and at peace" view (pillar 6). M3-07 later adds
    the map inside the hatch from this same deck.
  - **Rover.** 07 rides the platform through `IRoverPlacement` / cargo-seat style holding: wheels still, a small
    settle at each stop.
  - **Audio.** Winch creak while jammed when 07 is near. A ratchet and motor hum on the ride, a clunk at each stop.
  - **UI.** Station name "The crew's lift" / "Thang nâng của phi hành đoàn", and lines for jammed, feeding and riding.
- Visible progression (ruling 11): the restored lift is one more cleaned corner of home.

## 3. Rust that reads as decades, not as a pattern
- **Finding.** Weathering v2's rust runs repeat at even spacing and equal length in rows around the lander's band, on
  07's body, the shelf and the tower. From the chase camera they read as a printed trim, not as time. The patchwork
  paint reads as a checkerboard.
- **Fix (art).**
  - Runs start only at a cause: a rivet, a seam, the underside of a fitting, a bolt head.
  - Few per panel, with random gaps. Lengths and widths vary a lot (some only a stain at the bolt).
  - Colour: dark brown at the source fading out. Stains, never stripes.
  - Patchwork: irregular patch sizes and a few per face, never alternating.
- **Everything abandoned gets the same treatment:** Bell, the tape rack, the depot's pale panels and the Kestrel
  capsule are still showroom clean.
- **Acceptance.** No two adjacent runs on a face share length and spacing (a test). Captures at the chase camera of
  the lander, 07, the tower and Bell, before and after.

## 4. Station pads that belong to the base
- **Finding.** Every station shows a bright neon ring on the ground (tower, bay, hop pads). It reads as game UI laid
  on the moon.
- **Fix (art + gameplay).**
  - A worn painted pad circle with small rover-height pad lamps. The crew marked where the rover parks.
  - The lamps glow warmly when 07 is near and fade when it leaves.
  - No free-floating neon ring.

## 5. 07 can look up at the stars
- **Finding (owner).** The camera never looks up: the orbit's lowest elevation is 3° above the rover, so the sky
  is only ever a strip at the top of the frame. The owner wants the vibe of a robot gazing at the stars. 07's head
  also tilts up at most 25°.
- **Fix: stargazing that happens by itself.**
  - **Rover: camera.** The orbit can drop below 07 to about −25°. The camera sinks toward the dust and tilts up, so
    the sky, Earth and the Peak fill the frame over 07's silhouette. It never clips the ground: near the dust it stops
    sinking and only tilts.
  - **Rover: 07 looks where you look.** When the camera looks up and 07 is still, 07 slowly lifts its head toward the
    sky (head pitch up to ~45°). It also opens its solar wing a little and dims its eye to a soft glow.
  - **The stargazing beat.** After ~3 s still and looking up, the HUD fades out and the music thins to a few notes
    over the wind. Now and then a slow shooting star crosses the sky, rare and never a reward or collectible. Any drive
    input ends it gently.
  - **Art.** Clearances re-checked with the head up to 45° (lamp bar, wing). A star field dense enough near the zenith
    to hold a full-frame look up. The shooting-star streak.
  - **Audio.** Music ducking for the beat and a soft, low airy swell when it begins.
  - **UI.** The HUD fade. A one-time gentle hint at the first clear night view: "Look up" / "Ngước nhìn lên".
- Acceptance: from any open spot, looking up shows a full-frame sky over 07 with no clipping. The beat starts and
  ends softly. The PlayMode zero-GC test holds during it.

## Split
| Box | Delivers |
|---|---|
| gameplay | New game + old-save handling, lift station and ride, lookout trigger, pad lamp states |
| ui | New game confirm, the older-journey line, lift name and lines, HUD fade while stargazing, the "Look up" hint |
| art | readable jammed and restored lift, winch hopper, rust v3, weathering of Bell, rack, depot and capsule, pads, zenith stars, shooting star, head-up clearances |
| rover | riding the lift (hold and settle), the lookout wide shot, camera below 07, head-up gaze, the stargazing beat |
| audio | winch creak, ride ratchet and hum, stop clunk, stargazing music thin-out and swell |

## Acceptance
- A fresh game cannot jump until Hover-Jump is fitted. An old save is put away visibly, never silently lost.
- In a fresh game the lift can be restored, ridden up and down, and the lookout feels like the quietest moment so far.
- The owner's screenshot angle, re-captured, shows rust as stains with causes and no neon rings.
