# M3-02 — Friends framework + Tilly

> Owner: Director (design). Implements the "Friends" spine of `docs/DESIGN.md`. Story context: `docs/STORY.md`
> (Tilly was Ines Kalani's survey drone). All design rulings in `docs/VISION.md` apply — nothing here can be lost,
> failed or timed.

## Player experience (the five beats)
1. **A different answer.** A sonar ping near the small crater east of the base gets an answer that isn't a relic's
   bell: a broken, hopeful chirp from a dark shape on the crater floor. 07 glances at it.
2. **Meeting.** Up close: Tilly lies on her side, one rotor bent, eye dark. Three of her missing parts glint amber
   (not cyan like scrap) within 30–60 m; three small lamps on her body show 0/3.
3. **Gathering.** Driving through a part collects it like scrap (magnet, softer), a lamp on Tilly lights each time,
   and 07 perks up. No order, no timer.
4. **Repair.** With 3/3, holding Interact (E) near Tilly starts a calm repair: 07's beam "stitches" her for a few
   seconds; she shivers, her eye flickers on, rotors spin up, she wobbles into the air, looks at 07, and chirps
   happily. A short card: Ines's first log. She follows 07 home.
5. **Home.** Tilly takes her perch on the lander. From now on she lives there: flits around the base, inspects the
   shelf, naps on her perch, and flies out to meet 07 whenever it comes home (within ~30 m), circling once.

## Tilly's gift: the spotter (ability)
When 07 leaves the base, Tilly comes along (hovering ~3 m up, ~4 m behind, never blocking the camera). Within ~40 m
of an undiscovered relic, friend part or scrap cluster, she drifts toward it, hovers, and gives a soft ping with a
small light cone — passive discovery that rewards exploring without replacing the sonar. When 07 returns home, she
goes back to her perch. The player can't lose her: if she falls far behind she simply catches up (or reappears by
the base).

## Systems (reusable for Moss, Bell, Atlas)
- **FriendDefinition** (content, no prose): `id`, prefab paths (broken pose, repaired), `parts` (3–5 part ids with
  placement rules), `homeSocket` (a named empty on the lander/base), `abilityId`, `repairDuration`, chirp set id.
  Text lives in localization: `friend.<id>.name`, `friend.<id>.repair_log`.
- **Friend states**: Dormant → PartsGathering (n/N) → Repairing → Awake (following or home). Saved.
- **Placement**: deterministic site planner via `ITerrainQuery.IsDrivable`; Tilly's site is in a shallow crater
  60–110 m from base, visible from the base edge (a dark silhouette + the faint amber glints).
- **Events (Core)**: `FriendAnswered(id, position)`, `FriendPartCollected(id, partIndex, collected, total)`,
  `FriendRepairStarted(id)`, `FriendRepaired(id)`, `FriendGreeted(id)`. Audio, rover body language and UI hook these.
- **Interaction hint**: a new `Repair` kind in `IInteractionHints` (only when all parts are gathered).
- **Behaviour layer**: friends are kinematic, procedural movers (no physics fights), zero GC, ease everything,
  respect the camera (never between camera and 07).

## Content & domain split
| Box | Delivers |
|---|---|
| art | Tilly model (rig: Body, Eye (glow renderer), Rotor_FL/FR/RL/RR, Antenna, PartLamp_0..2 glow renderers, Perch socket), broken-pose variant (or rig pose data), 3 part pickups (amber accent), a lander perch |
| gameplay | friend framework, Tilly's site/parts/repair, follow + home life + greeting, spotter ability, save, events, contributor |
| audio | chirp vocabulary (broken, curious, happy, sleepy, greeting), rotor hum loop, repair "stitching" texture, part-collected tone |
| ui | repair prompt + 0/3 parts readout near Tilly, the log card, localization keys (en + vi) |
| rover | 07's reactions (glance at answers, perk on parts, joyful bounce on repair), dig/repair camera moment reuse |

## Acceptance
- A PlayMode playthrough extends the golden path: find Tilly via ping, gather 3 parts, repair, she follows home,
  greets on return, spots a relic on the next trip.
- Feel checklist in VISION.md passes; captures reviewed by the Director in the real scene.
