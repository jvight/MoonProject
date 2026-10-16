# M3-22: Build pads and structures

> **Superseded (2026-10-15, 0.5.7):** Replaced by free placement with soft snapping and staged welding (`Assets/_Project/Scripts/Gameplay/Building/`, owner base-building design https://claude.ai/artifact/UYJCh1YaK4oELycWr2TVAT).

> Owner: Director (design). Version: **0.6** (owner, 2026-10-11: structures are not in 0.5). Source:
> `docs/design/gdd-v3.md` §6.2 and §3.3, approved by the owner on 2026-10-11 (decision 7: "the player builds
> structures for crafting and operations on predefined pads"). This spec fixes the rules and contracts now so 0.5's
> work leaves room for them; it is dispatched when 0.6 opens. Rulings 11, 12, 14 and 15 apply.

## 1. The rule: pads, never free placement
- **A build pad** is a rover-height plinth with a worn bolt circle and a sealed crate: a structure Kenji pre-packed
  (ruling 12: built for wheels). Visible from 50 m and named on the survey chart.
- Pads are **predefined world anchors** (`pad.<region>.<n>`), placed by World; the player never chooses a spot.
- **Home has 5 pads** around its three masses (lander, bay, tower), each with ≥ 4 m of lit ground (V3, V5). Field pads:
  Kestrel's rim (basin), two on the terraces' ledges (0.6); two on the crater floor (0.7); two road camps (0.8).
- Home grows by machines on pads, friends' spots and masts' glow, never by free placement.

## 2. Building (one continuous action, ~6 s, ruling 14)
1. A blueprint, decoded at the lab's reader (M3-19), unlocks a structure type (a tech node, M3-21).
2. 07 parks at a pad. If the structure's materials are in hand, the pad's lamps light and the next line may invite it
   (tier 1 only if it serves a main target, M3-21).
3. 07's beam feeds the crate's hopper; the crate's sides fold down and the frame unfolds by itself.
4. 07 stitches it (the existing stitch verb). It lights; its home-light or pillar joins the chart.

**Cost:** materials (T1 first, then T2) plus, for some, a component. Repeated pylons cost a little more each time (the
long-term scrap sink).

## 3. Structures
| Structure | Ver | Pads | Job | Needs | Visible |
|---|---|---|---|---|---|
| **Field Charge Pylon** | 0.6 | ~2 per region (Kestrel's rim first) | a charger where no mast stands (`ChargerKind.Pylon`); inside the network it draws from the masts | blueprint + Metal + Wiring + Optics (escalating) | a slim post with a 4 m pad and a warm cap lamp |
| **Fabricator** | 0.6 | a home pad beside the bay | makes any component already found once, so progress never stalls on a missing part | blueprint + Alloy + T1 | a compact machine with a warm work light |
| **Bay Wing** | 0.6 | the bay's side pad | a second gantry that fits tier-2 kits (treads, wings, the tow frame) | blueprint + T1 + Servo Pack | a new gantry wing on the bay |
| Pylon II | 0.7 | upgrade in place | charges twice as fast; a small beacon on the chart | Alloy + Lens Array | a taller post, a second lamp |
| Solar Array | 0.7 | home; pylon pads beyond the network | powers a pylon outside the network's reach | blueprint + Crystal + Optics | a fan of panels tilting to the low sun |
| Materials Store | 0.7 | a field pad per mid region; one at home | a field depot; Atlas (Ferry Harness) brings stacks home | blueprint + Alloy + Servo Pack | a corrugated shed with a crane arm; stacks grow |
| Drone Hangar | 0.8 | a home pad | the Sister Drones' home | with the Sister Drones node | a small hangar, two perches |
| Paint Booth | 1.0 | a home pad | 07's paint and stickers | blueprint + T1 | a booth with a striped awning |

Two structures serve the main targets: the **Fabricator** (repeat components, such as a second Gyro, come only from
it) and **one road-camp pylon** on the Peak road (0.8). Every other structure is optional.

## 4. Contracts (Core, landed by the Director when 0.6 opens)
- `BuildPadId` (string ids `pad.<region>.<n>`, World anchors), `StructureKind` enum (0.6: Pylon, Fabricator, BayWing),
  `IBuildPads` (state per pad: Empty, Ready, Built, and which structure), `StructureBuilt(padId, kind, restored)`.
- `ChargerKind.Pylon`.
- Tech nodes for the 0.6 structures join `TechNodeId` (M3-21).

## 5. Split (0.6)
| Domain | Delivers |
|---|---|
| world | pad anchors (home 5, Kestrel's rim, the terraces' 2), lit ground around home pads |
| art | the pad plinth and crate (sealed, open), each 0.6 structure in dormant-frame and built states, the Bay Wing gantry |
| gameplay | pads' state, the build flow (feed, unfold, stitch), costs with escalation, the pylon as a charger, the Fabricator's recipes, save |
| rover | parking at a pad, the build camera moment |
| ui | pad and structure names and lines, the chart's pads |
| audio | the crate's unfold, the build stitch, each structure's idle sound |

## 6. Acceptance (0.6)
- From home's chase frame the 5 pads read as places for something, and a built structure changes home's silhouette
  or lights (ruling 11).
- The first pylon at Kestrel's rim charges 07 and shows on the chart; the 300 m rule (M3-20) holds on 0.6's trips.
- No structure can be placed anywhere but a pad.
