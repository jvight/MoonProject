# M3-06 — The relay network (station reach)

> Owner: Director (design). Implements `docs/DESIGN.md` "Station reach — the relay network", the second
> progression axis. Rulings in `docs/VISION.md` apply. This is the feature that turns "how far 07 can go" into "how
> far home reaches", without any battery, fuel or range limit (rulings 1 and 7, pillar 6).

## The promise
Lumen Station was a radio relay. From the base at night the player can see, here and there on high ground, the
dark silhouettes of old relay masts. Each has a dead lamp at the top. Restoring them lights them one by one, and the
basin slowly fills with small warm lights linked back home. The frontier between lit and unlit is where the moon
still feels lonely.

## Masts (M3-06 ships the basin's four, more come with each region)
| id | Where (World anchor `relay.<n>`) | Reachable with |
|---|---|---|
| relay.0 | a low mound ~120 m from base toward the canyon | nothing (first one, teaches the system) |
| relay.1 | a crater-rim shoulder ~220 m out, opposite side | nothing |
| relay.2 | the canyon mouth's rock fins | nothing (the canyon itself still needs Hover-Jump) |
| relay.3 | the canyon terminus ledge above Bell's chamber | Hover-Jump |

Later regions add theirs: rim terraces, shadowed crater, and the foot of The Peak, which is the arc's last link.

## Restoring a mast
- **Approaching an unlit mast.** It reads as a dark, leaning tower with a cold lamp. Its one missing "relay part"
  glints amber nearby (≤ 40 m), like friend parts.
- **Repair.** With the part held, hold Interact at the mast's foot. 07's beam stitches it like a friend repair, but
  shorter. Then:
  - the mast straightens with a creak;
  - its lamp warms up, amber, HDR, carried by bloom;
  - a soft radio "link" tone in D major pentatonic plays;
  - a thin line of light pulses once along the ground back toward the nearest lit node: home or another mast.
- **Scrap cost.** Each mast also costs scrap, escalating from 60 through 90 and 120 to 150. This is the long-term
  sink. VISION ruling 5 still holds: the basin's income must stay at least 2× all sinks, so retune the scrap field
  if needed.
- **The moment.** A short ticker line from the radio: "Relay 1 online. Lumen Station can hear a little farther."
  The camera may use the existing "camera moment" for a slow look at the lit mast against the sky.

## Reach and what it gives
- **Reach.** The union of circles: the tower's radius plus each lit mast's radius (tuning; ~110 m per mast). A mast
  only counts if it links, meaning its circle overlaps lit reach, so the network grows outward from home.
- **Inside reach:**
  - The radio is clear. The soundscape's distance model uses distance to the nearest lit node instead of distance to
    the base, so the radio stays warm wherever the network reaches, and the near-silence lives beyond it.
  - The sonar's answer range gets a gentle bonus (tuning).
  - Bell's signals may point at things inside reach first.
  - The hand-drawn map (M3-07) draws itself here.
  - **Radio-hop:** at any lit mast, or at home, Interact opens a tiny list of lit nodes. Choosing one plays a calm
    transition: static rises, the screen eases to a soft dark, 07 is placed at the target node's pad, the static
    resolves and the view eases in. It takes about 2 s and is never instant or jarring. Not available while towing a
    relic, so relics still come home by road. Hops are free.
- **Outside reach.** Everything works and nothing is lost or punished. It is only quieter: static, no map drawing,
  no hop.

## Systems (Core contracts via the Director)
- **`IStationReach`** (registered by Gameplay):
  - `bool IsInReach(Vector3 p)`
  - `float DistanceToNearestNode(Vector3 p)`
  - `int LitCount`
  - `int NodeCount`
  - `RelayNode GetNode(int i)` (id, position, lit)

  Audio, UI and rover read it. Allocation-free.
- **Events:** `RelayRestored(id, position, litCount, total)`, `RadioHopStarted(fromId, toId)`, `RadioHopFinished(toId)`.
- **World:** `relay.<n>` anchors (with Forward toward home) and a flat 3 m pad at each.
- **Save:** lit masts, collected relay parts and the scrap paid. Never lost.

## Content & domain split
| Box | Delivers |
|---|---|
| world | `relay.0..3` anchors on readable high points visible from the base; pads; the "link line" ground decal path (optional shader global) |
| art | RelayMast prefab (dark/broken pose + restored pose, `Lamp` HDR glow renderer, `PartSocket`, `Pad` root), relay part pickup |
| gameplay | masts, parts, repair beat, scrap cost, `IStationReach`, radio-hop (list + transition + placement), events, save, contributor, playthrough extension |
| audio | link tone, mast creak and lamp warm-up, hop static-and-resolve, soundscape distance from `IStationReach` |
| ui | hop list (diegetic, tiny), relay prompt, ticker lines, the pause line "Relays 2/4" |
| rover | glance at a newly lit mast; camera moment at restoration |

## Acceptance
- The playthrough extends: restore relay.0, see the radio stay clear out there, radio-hop home and back.
- From the base at night, lit masts read as warm points along the basin.
- Muted, the feature still reads through the lights, the line pulse, the ticker and the list.
- Feel checklist and pillar 6 pass. Captures are reviewed by the Director.
