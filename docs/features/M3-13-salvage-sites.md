# M3-13 — Salvage sites instead of a scrap field

> Owner: Director (design). Owner direction (2026-10-08): hundreds of glinting bits on the ground don't fit a moon
> abandoned for decades. Materials should come from **fallen satellite fragments, abandoned sites and ruins**, and be
> **crafted** into upgrades. This replaces the M2 scrap field and its currency. VISION rulings 1, 5, 11, 12 and
> pillar 6 apply.

## Why
- **Pillar 6.** A vast, quiet moon reads lonely when it is mostly empty, with a few meaningful places on the horizon.
  Confetti glints read arcade.
- **Ruling 12.** The world should feel abandoned. Wrecks and ruins *are* that story, and salvaging them is the most
  in-character activity for an old rover.
- **Ruling 11.** Upgrades become crafted kit made from what 07 salvaged, so the parts on 07 come from places the player
  remembers.

## The loop
spot a wreck on the horizon → drive there → salvage with the beam → carry the materials home → craft at Kenji's bench
→ the new kit shows on 07 → reach new regions and new wrecks.

## Materials (three, readable at a glance)
| Material | From | Used for |
|---|---|---|
| **Metal** | plates, struts, hull pieces | most kit, relay masts, the tower's frame |
| **Wiring** | cable bundles, circuit boards, junction boxes | coils, the radio tower, relays |
| **Optics** | solar cells, lenses, dish panels | lamps, sonar, the solar wing |

Friend parts and relay modules stay **unique found items**, as they are now. Materials never replace them.

## Sites (basin, M3: five; each region adds its own)
1. **The supply depot**, near the base. The crew's collapsed storage shed and toppled cargo pods. This is the first
   site and teaches salvage.
2. **Kestrel-3, a fallen relay satellite.** An impact crater with a long debris trail pointing back along its fall
   line: solar wings (Optics), the bus body (Metal) and antenna harness (Wiring). It is the basin's big landmark,
   visible from the base.
3. **The drill rig.** A tipped-over survey drill on a crater rim, rusted.
4. **Kenji's rover garage ruins.** Half-buried, spare rover parts. Its heart holds the "spare 07 wheel" memory.
5. **The canyon supply lander.** A crashed cargo lander past the Hover-Jump gate.

## Each site
- **Salvage points.** 4–8 per site, chunky and readable (a solar wing, a hull plate, a cable drum). Each yields one
  material bundle.
- **The heart.** Each site holds one crew relic, the personal thing found in the wreck. Relics move from random buried
  spots into sites. The sonar still finds them: a site answers a ping with its own warm tone.
- **Picked clean.** A site visibly empties as it is salvaged, down to a weathered skeleton, which is progress you can
  see. Nothing respawns at the same spot.
- **Loose bits.** A few loose bits lie only along a site's debris trail, leading the eye to it. There is no loose
  scrap anywhere else.

## The salvage beat
- **Aim.** Aim at a salvage point (the existing generous aim cone, ruling 2) and hold Excavate. 07's beam cuts or
  pries it loose over 2–4 s, eased, with sparks and a rising tone in D major pentatonic. Releasing early keeps
  progress (ruling 4).
- **Collect.** The piece breaks off, floats to 07 and folds into its cargo with a soft chime.
- **Melody.** Consecutive pieces at one site climb the pentatonic, replacing the old scrap melody.
- **Big pieces.** A whole solar wing must first be dragged clear with the tether (a few metres), then cut. This reuses
  the tether.

## Crafting at Kenji's bench
Upgrades and station work cost materials instead of scrap. Indicative values, tuned later against yields:

| Item | Cost |
|---|---|
| Hover-Jump | 10 Metal + 6 Wiring + 2 Optics |
| Radio tower level | Wiring + Optics |
| Relay mast | 6 Metal + 4 Wiring, plus its relay module |

Ruling 5 is restated: in each region, total site yield is at least 2× the region's recipe costs.

## Between sessions
M3-08 "moon drift" drops fresh small fragments: a new little smoking crater with one fragment site. That gives a gentle
reason to return, with no FOMO.

## What this replaces
| Old | New |
|---|---|
| ScrapField and its planner | SalvageSite planner (deterministic, anchors from World) |
| `ScrapCollected` / `CurrencyChanged` | `MaterialSalvaged` / `MaterialsChanged` (Core, Director) |
| The scrap chip HUD | a three-icon materials chip |
| The scrap melody | the salvage melody |
| Prices in scrap | recipes in materials |

The playthrough golden path is rewritten around salvage.

## Split (sequential, one box at a time)
| Box | Delivers |
|---|---|
| world | site anchors (`site.<id>`) with clear approach lanes, Kestrel-3's impact crater and debris-trail terrain, no loose scatter |
| art | weathered wrecks and ruins (ruling 12 weathering), salvage-point pieces, picked-clean skeleton states, three material bundle pickups |
| gameplay | sites, salvage points, the beat, materials, recipes at the bench, relics into site hearts, save migration, economy rule tests, playthrough |
| audio | cut/pry beam, break-off, fold-in chime, salvage melody, a site's sonar answer |
| ui | materials chip, recipe panel at the bench, salvage hold ring |
| rover | glance at a salvage point, a small effort lean while cutting |

## Acceptance
- From the base, the basin reads as wide and quiet with a few landmark wrecks. No loose glints except a site's trail.
- The golden path salvages the depot, crafts Hover-Jump from materials, and restores relay.0 with materials.
- Muted, the feature reads through sparks, chips, skeletons and the bench.
