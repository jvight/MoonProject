# Level design review, 2026-10-11: the basin as built

> Author: the game design session. Proposals to the Director, not decisions.
>
> Sources:
> - World's anchor dump (`MoonProject-wt/world/Logs/batch/20261008-104951-exec-Run.log`), checked against
>   `Data/Tuning/WorldSettings.asset` today (sites, ramps, bowls, canyon, peak and relays are unchanged);
> - `Friend_tilly.asset` placement; `BaseModelBuilder` and `LanderMeshes` (deck top 2.8 m);
> - fresh captures in this worktree, `Logs/captures/design-2026-10-11/` (pose file `design_views.json`):
>   - `Main_top_south`, `Main_top_north` (straight down from 350 m, north up);
>   - `Main_lookout_n/e/s/w` (eye at 4.6 m on the lander deck);
>   - `Main_route_*`, `Main_home_from_*`, `Main_chasm_from_mouth`;
>   - `Main_look_*`, close frames for the visual review.
>
> Bearings are clockwise from north (+Z). Home is the origin. Drive times assume a first-time human averaging
> ~5 m/s (07's top speed is 8 m/s).

## 1. The map as built

| Place | Bearing | Distance | Drive from home | Notes |
|---|---|---|---|---|
| Depot (step 1) | 315° | 55 m | 11 s | in the deck's north view, left |
| Tilly's crater (step 3) | 35–145° (seeded) | 60–110 m | 12–22 s | `visibleFromBase` on |
| Garage (P1: L2 transformer) | 175° | 125 m | 25 s | directly behind the spawn view |
| Drill | 113° | 148 m | 30 s | a speck in `lookout_e` |
| relay.0 (the mound) | 28.5° | 150 m | 30 s | in the first frame; a good silhouette |
| Kestrel-3 crater | 356° | 185 m | 37 s | on the spawn line under the Peak; its furrow is the clearest shape in `top_south` |
| relay.1 (rim shoulder) | 250° | 218 m | 44 s | the only thing in the west half |
| Canyon mouth / relay.2 | 72–74° | 278–280 m | 56 s | the notch is clear from home (`lookout_e`) |
| Chasm lip (P2) | 72° | 298 m | 60 s | 24 m wide |
| Canyon exit (one-way step) | 64° | 299 m | 60 s back home | home reads as one warm dot (`home_from_canyon_exit`) |
| Crashed lander | 74° | 350 m | via the leap | a pale wreck, visible across the chasm from the mouth (`chasm_from_mouth`) |
| Ledge glow / Bell's terminus | 72° / 96° | 444 / 474 m | ~200 m of canyon past the landing | the terminus is out of sight from the lip |
| relay.3 | 92° | 457 m | inside the canyon | |
| The Peak | 12° | 440 m, 150 m high | n/a | the endgame, always in view |

Basin floor radius 320 m, crest 420 m. Play features: ramps at 38°/95 m and 205°/150 m; bowls at 292°/125 m and
140°/215 m.

### Sectors
- **North (315–40°):** depot, Kestrel, relay.0, the first ramp, the Peak and Earth. Dense, and it's the first frame.
  Good.
- **East (60–150°):** the canyon, Tilly, the drill, the far bowl. The game's spine. Good.
- **South (150–200°):** only the garage, at 125 m. Thin.
- **West (200–310°):** relay.1 at 218 m, the second ramp and a bowl. **Dead.** In `top_south` the whole left
  half of the floor is dunes and craters with nothing to drive to. In `route_relay1` the west rim is a dark,
  backlit silhouette: the most beautiful skyline in the game, wasted on an empty half.

## 2. The steps 0–8 on the ground

| Step | Leg | Distance | Human time | Comment |
|---|---|---|---|---|
| 0–1 | dock → depot → home | 110 m | ~25 s | Right: short, in view, a first win. |
| 2–3 | home → Tilly → home | 120–220 m | ~45 s | Right. Her pillar rises inside the deck's east view. |
| 4 (P2) | home → chasm lip, look across → home | ~600 m | ~2 min | The longest leg so far, and its payoff is "you can't". The lip view (`chasm_from_mouth`) doesn't read as a gap: the far face looks like a step, and Bell's light is a 3-pixel dot. |
| 4–5 (P1) | home → garage (transformer) → home | 250 m | ~50 s | Fine, and the first time the player turns their back on the Peak. |
| 6 | home → lip → leap → landing → ledge → terminus → exit → home | ~1.0 km | ~4 min | Good inside the canyon. The 300 m home is a plain with one warm dot ahead. |
| 7 | relays: 0 (150 m N), 1 (218 m W), 2 (mouth), 3 (in the canyon) | ~1.1 km | ~4 min | relay.1 sends the player west into nothing, with nothing on the way back. |
| 8 | lift, deck lookout | 0 | — | The lookout eye is 4.6 m up. It sees what the dock sees: every site is a speck (`lookout_e/s`). |

Total driving for steps 0–8 is about 3 km, about 11–12 min of the 50–60 min target. That's healthy: the rest is
doing, not driving. The problem isn't length. It's **three dead stretches and a lookout with nothing to show**.

## 3. Findings

1. **The home → canyon road (280 m) is empty.**
   - Nothing sits within 30 m of the line. The drill is 41° off-axis, the ramp at 38° is 53 m to the side.
   - `route_canyon_mid` is a pale wall above a blank plain.
   - The player drives it three times (P2 look, the leap trip, relay.2).
2. **The west half is dead,** although relay.1 sends the player there in step 7.
3. **The way home (300 m from the exit) has only one cue:** a warm dot. Home doesn't grow as you approach until the
   last ~60 m.
4. **The chasm doesn't read as a chasm from the basin side.** From the mouth, the 24 m gap hides behind the lip's
   own edge. "I can't cross" is only learned by driving to the lip. Bell's light (P2's promise) is too small and is
   the wrong object: the terminus is out of sight, so the light the player sees has to be on the near side of the
   bend (the landing or the ledge).
5. **The lookout shows nothing new.**
   - At 4.6 m the eye clears the 1.4 m dunes, but the sites are 2–6 px specks with no light.
   - The step-8 payoff ("07 sees the whole basin") needs either more height or things that answer from up there.
6. **Promises seen from home are good in the north and east, weak in the south and west.** From the deck:
   - north: Peak, Earth, Kestrel, relay.0, depot;
   - east: the canyon notch;
   - south: the garage, one speck;
   - west: the base's own bay blocks most of the view.
7. **Landmarks:** the Peak, Earth, the canyon notch and Kestrel's furrow work. There's no mid-distance (100–200 m)
   vertical landmark except relay.0. Every site is low and flat, so none of them reads at 150 m.

## 4. Layout proposals, ranked

| # | Change (with metres) | Box | Size | Fixes |
|---|---|---|---|---|
| L1 | **Give every site a tall, lit silhouette.** Each salvage site gets one vertical landmark ≥ 8 m tall: the garage's crane jib, the drill's derrick, Kestrel-3's tail fin pointing at the sky, the depot's mast. It carries a dim warm pilot once the site answers the sonar and goes dark when picked clean, so "done" is visible. | art (+ gameplay for the on/off) | M | finding 7, 5, 6 |
| L2 | **The crew's road to the canyon.** A line of rover-height route posts (half-buried survey stakes with reflectors) every ~45 m from the base pad's edge (25 m) to the canyon mouth (280 m) on bearing 72°: six posts at 70, 115, 160, 205, 250 and 275 m. The reflectors glint in 07's lamp; when relay.2 lights, the posts get a faint warm tip. Pair it with V4's worn tracks: 07's own old ruts run along the road. | world (placement) + art (post) | S | finding 1, 3 |
| L3 | **Move the garage to the south-west:** from 175°/125 m to **228°/140 m**, just past the second ramp (205°/150 m). The ramp becomes the garage's joyful approach, and relay.1 (250°/218 m) sits ~95 m beyond the garage. The west gets one destination and a two-stop arm: transformer, then relay. The south loses only a speck. | world (site bearing) + gameplay (no code: the anchor follows) | S | finding 2, 6 |
| L4 | **Make the chasm read from the basin.** A broken crew cable bridge: two rover-width anchor posts on the near lip, a snapped cable hanging into the trough, and the far anchor lit warm. Ruling 12: it was built for wheels, the storm broke it. Move Bell's P2 light to the **far landing (331 m)**, as a lantern Bell left on the bridge's far anchor, visible from the mouth at 280 m and from the deck's east view. | art (bridge) + world (anchor at canyon.landing) + gameplay (the light's state) | M | finding 4 |
| L5 | **A real lookout.** When 07 settles on the deck, the camera rises to ~14 m above the deck, pulled back toward the lander, with a slow 360° settle. Every place inside the lookout's view that the player hasn't visited answers once with a faint pilot blink in turn, a "seen" sweep that M3-07's map turns into pencil marks. | rover (camera) + gameplay (the sweep) + world (sightline query) | M | finding 5 |
| L6 | **Home grows as you approach.** The tower's top light uses the Peak beacon's min-angular-size halo, so home never shrinks below a readable warm dot at 300 m. The dock and porch pools (visual review V3) give the last 60 m a warm floor. | world | S | finding 3 |
| L7 | **A mid-west remnant, no new system:** the crew's antenna farm, 3–4 toppled dish frames at **270°/120 m**, inside the bowl at 292°/125 m. It's a place the lookout's west view can point at, a pebble of story (Ro's antennas) and a natural spot for one of 0.6's drift glints. | art + world | S | finding 2 |
| L8 | **Tilly's crater on the canyon side.** Narrow her bearing from 35–145° to **60–95°** and her distance to 80–110 m. Steps 3–4 then point the player along the future canyon road, and the chasm visit (P2) continues a direction already learned. | gameplay (data only) | S | step 4's 600 m round trip feels like one trip, not two |
| L9 | **Keep the deck's west view open.** Move the bay ~4 m south of its current line, so the deck's west view shows the antenna farm (L7) and relay.1 rather than the bay's roof. Do this together with the visual review's V5 declutter. | art (base layout) | S | finding 6 |

Not proposed: moving the canyon, the Peak or the Kestrel. Those three plus Earth are the basin's best composition,
and every early frame already depends on them.

## 5. Acceptance for any layout change
- **From the deck (L5 camera)**, at least one lit or lightable promise in each quadrant: north Kestrel and relay.0,
  east the bridge light and the road, south-west the garage jib, west the antenna farm and relay.1.
- **No leg of steps 0–8 crosses more than 120 m** without a landmark within 40 m of the line: a post, a site or a
  crater rim.
- **Home is a readable warm point** from every site and from the canyon exit (`home_from_*` poses).
- **Re-run `design_views.json`** with the changes and add the poses to the Director's look set.
