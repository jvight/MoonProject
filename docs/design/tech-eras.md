# Tech eras: what each decade's hardware looks like

> Owner asked (2026-10-19): does the technology match between the ALSEP period and our setting? Do shapes and
> structures progress from the 70s to the 80s? Reference: the Apollo 11 photo of Aldrin by the passive seismometer.
> Canon: `story-v4.md`, `07-identity.md`, `basin-dressing.md`. Author: the story & flow session.

## 0. The rule
The basin is a history book, and the player reads its pages by **skin, structure, displays, and wear**. Each object
wears its own decade, so no prop may borrow Lumen's look unless it was made for Lumen. A player who has seen three
objects should be able to guess the age of a fourth:

> **Foil and bare metal:** before anyone lived here. **White enamel:** the build-out. **Cream and coral:**
> Lumen, the crew's home. **Cream with fresh welds:** 07, now.

## 1. The eras
| | **P · Probes** 1966–68 | **S · Sorties** 1969–77 | **B · Build-out** 1978–83 | **L · Lumen** 1984–88 | **N · Now** 2024 |
|---|---|---|---|---|---|
| **Story** | robotic soft landers scout sites before crews | short crewed sorties leave instruments behind | the Long Dim begins, and the ILC industrialises the Moon for the Arks | the Departure's waystation, crewed by four, sealed by the 28 Oct 1988 storm | 07 wakes and builds from Lumen's library; nothing else is new |
| **Materials** | white thermal paint, bare aluminium tube, silver mirrors | gold and black Kapton foil, bare aluminium, silver panels, black anodising | **white enamel** sheet panels, grey trusses; foil only on instruments | **cream enamel** (`#F4E6C8`), cool-metal frames; foil only in recesses and on backs | Lumen flat-packs from the crates, plus salvage |
| **Shapes** | open tubular spaceframe, tripod legs, boxes hung on the frame | instruments as small boxes on legs, foil skirts, flat ribbon cables, no enclosure | cylinders and boxes with external ribs and stringers; brake-bent, boxy, square corners | chamfered shoulders, rounded corners, flush panels, bellows joints, ribbed hulls | Lumen shapes, assembled on site |
| **Colour** | no livery; small black stencils | none; function colours only (foil gold, black radiators) | **one narrow orange ILC band**, black stencils | **wide coral band** (`#FF8A5B`), stencils, pictograms, hand-written labels | Lumen colours, less sun-faded; bare weld beads |
| **Displays** | none | mechanical: spirit levels, painted scales, flip flags | analogue meters, toggles, segment counters; **no CRT** | **amber CRT**, LED bars, membrane keys, cassette slots | 07's CRT carries the readouts |
| **Fasteners** | exposed screws, pip pins | pip pins, tie-down straps, Velcro patches | hex bolt rows and rivets, all visible | captive flush fasteners, quarter-turn latches, coral handles | weld seams |
| **Power, data** | solar panel and battery; radios straight to Earth; nothing stored | RTG (a black finned cylinder), flat ribbon cables, telemetry straight to Earth | fuel cells or solar plus Ni-Cd, coax, reel tape | solar arrays, battery banks, induction pads, round mil connectors, 8/16-bit boards, cassette data | the same parts, rewired |
| **Wear in 2024** | paint chalked grey-tan, glass crazed | foil brittle with torn edges, aluminium dull | enamel chalked; the band faded to tan | cream bleached on the sun side, coral faded to salmon, storm damage, dust banks | lightly dusted |
| **References** | Surveyor 1–7, Luna 9 | Apollo LM descent stage, ALSEP (PSE, central station, active seismic line), LRRR, Apollo hand tools, Skylab | Viking, Skylab and Spacelab modules, NASA Ames 1970s colony art (Guidice, Davis) | Shuttle-era Spacelab racks, Mir core module, 80s amber terminals, cassette futurism | — |

**Vacuum logic across all eras:** no rust and no rot. Age shows as UV chalking, yellowing, thermal-cycle cracks,
brittle foil and dust. Never show pitting as a repeated pattern of holes (owner rule: no hole patterns).

**Why the colours change (one line each):** white was the 1970s thermal standard. In 1984 the ILC moved crewed gear
to cream because white glared in suit visors under the low polar Sun, and added the coral band as the Departure's
fleet mark. **Meridian** (Tycho Consortium, 1984–88, foreign) runs in parallel to L: white composite panels, teal
marks, hex docks (story v4 §3.1).

## 2. The progression the player can see
1. **Skin:** foil outside (S) → white panels with foil at the instruments (B) → cream, with foil hidden (L).
2. **Structure:** open frames (P, S) → bolted, boxy panels (B) → chamfered, flush, rounded (L).
3. **Displays:** none or mechanical → meters and counters → amber CRT.
4. **The fleet, side by side:** an **SSR-3** elder (1978: white, boxy, riveted, one headlamp, no screen) next to
   **07** (SSR-5, 1983, the first machine in the Lumen look: cream, coral, chamfered, CRT).
5. **One object, two eras:** the **seismic station** is 1971 hardware that Lumen wired into its bus in 1984. A
   bolted-on cream junction box carries the amber lamp that stays dark at B10.

## 3. Where each era sits in the basin, and why
- **P:** the Scout at (68.8, 81) in its small crater. It landed wherever its radar found flat ground, and nobody
  chose the spot.
- **S:** the 1971 crew landed **where Lumen's lander stands now** (35, 160). Its instruments ring that spot at
  Apollo-like distances: the retroreflector 18 m out (52.8, 160.4), the seismic station 95 m out of the ascent blast
  (128, 183) with its 90 m geophone line, and a walk to the Scout (bootprint ring, sample rack on the crater rim).
  **Why Lumen is here:** the site had 13 years of quiet seismic records and a surveyed position, so the ILC built on
  known ground. Lumen re-aimed the retroreflector at its landing approach in 1984 (basin-dressing §2's logic).
- **B:** the elders (06 at relay.2's hut) and Atlas (1979, 0.6+): older machines shipped in to serve Lumen.
- **L:** the ruins, the cargo lander, crates, relay masts, the Peak dish, the flag (planted at the opening),
  trail A, the fallen tripod, the ice core.
- **N:** 07's base, on the old Lumen unloading apron.

## 4. Audit of R1 assets on main (2026-10-19)
Coverage figures are each FBX's surface-area share per material slot, measured in Blender. The FBXs are on main;
the build scripts are in `tools/art/blender/`. The audit **keeps every node and anchor name**, because World places
by them (for example `CentralBox_Anchor` and the geophone `Plug_Anchor`).

**Prerequisite for Art (Art domain, `R1MaterialRecipes`):**
- add **`R1_WhitePaint`**: chalky cool white, low smoothness (P, S and B, and later Meridian);
- add **`R1_TealMark`**: muted teal (Meridian and Kestrel-3);
- recommended: add **`R1_SilverFoil`** (aluminised film, crinkled).

Bare aluminium uses `R1_BareSteel`, which reads the same at chase distance.

| Asset | Era | Verdict | Change |
|---|---|---|---|
| `Prop_SeismicStation` | S 1971 + L retrofit | **MISMATCH** | CentralBox is cream and orange (31%): make it `WhitePaint` on top, with a `SilverFoil` (or `BareSteel`) reflector skirt and `BareSteel` legs and handles. **No orange.** The SeismometerUnit's cream becomes `SilverFoil`/`BareSteel`. The foil-shroud tabs alternate orange and grey; make them `BareSteel`. Keep the gold `FoilShroud`, as in the owner's photo. Add a small cream **Lumen junction box** with one coral band, bolted to the box's side, and move the dead lamp onto it (same node names). Optional: a black finned RTG cylinder 3 m away, cabled. |
| `Prop_GeophoneLine`, `Geophone_Sensor`, `Geophone_Plug`, `Geophone_Ribbon_10m` | S 1971 (active seismic line) | **MISMATCH** | The sensors are cream and orange: make them `BareSteel` cylinders on spikes. The ribbon becomes flat `WhitePaint`, with no orange or gold. The plug becomes `BareSteel` with a black (`Rubber`) shell. |
| `Prop_Retroreflector` | S 1971 | **MISMATCH** | PanelHousing is cream (36%) with orange bands: make it `BareSteel`, drop all orange, and keep `GoldFoil` on the back face only. The stand's cream and orange bits become `BareSteel`. |
| `Prop_SampleRack` (with its core tubes) | S 1971 | **MISMATCH** | The cream and orange frame and tool bands, and the cream tube bodies, become a `BareSteel` tube frame and shafts, with black (`Rubber`) grips. Core tubes are `BareSteel`. No cream, no orange. |
| `Prop_OldLander` (Scout 3) | P 1967 | **MISMATCH** | The spaceframe and legs are painted `GreyMetal` (26%): make them `BareSteel`. The thermal boxes and planar array are cream: make them `WhitePaint`, chalked toward grey-tan. Take the leg `GoldFoil` back to `BareSteel`; at most a mirror patch (`SilverFoil`) tops each thermal box. Keep the `S-3` stencil black. |
| `SSR3_Chassis`, `SSR3_Topper_01–06`, `SSR3_Tracks` | B 1978 | **MISMATCH** | They wear 07's livery. Make the cream `WhitePaint`, and narrow the continuous orange beltline to **one thin band** (≤ 2%). Keep the boxy, riveted shape, the single headlamp and no screen: these already read as B. |
| `Kestrel_*` (6 pieces) | M 1988 (story v4) | **MISMATCH** | They wear Lumen livery and an orange "Kestrel-3 stripe". Make the cream `WhitePaint` and the stripe `TealMark`. **Replace `Kestrel_LandingFoot`** with a solar-boom or antenna fragment, because a relay satellite has no feet. See §5.1. |
| `CargoLander`, `LanderBeacon`, `LanderServicePort`, `Crate_*` | L | OK | — |
| `HabitatModules`, `AirlockReadoutPanel`, `KenjiWorkshop`, `RadioTower`, `ScatterSet`, `FallenSolarArray` | L | OK | Watch item: FallenSolarArray is 20% foil, and must show it on panel backs only. |
| Relay and Peak hardware: `DeadAntennaMast`, `PeakBeacon`, `SignalPillar`, `IceCoreTube`, `PadFragment_*`, `BuildApronMarkings` | L | OK | — |
| Cable kit (`Cable_*`, `GroundCable*`) and `DeadChargingPost` | L | OK | — |
| `Prop_MissionFlag` (planted at Lumen's opening), `Prop_SurveyTripod`, `Decal_BootprintTrail` (trail A) | L | OK | Still missing: the 1971 ribbed bootprint variant (basin-dressing §5). |
| `RoverModel` (07), `SolarWing`, all `Kit_*`, `ScanFan`, `FocusLens`, `LensGlass`, `HoverCoils` | L (SSR-5, 1983) | OK | SolarWing is 20% foil, on the wing's back only. |
| `ChargingPad`, `SolarArray`, `BatteryBank`, `AntennaMast`, `PowerPylon` | N (built in 2024 from Lumen flat-packs) | OK | Cream is right, because the flat-packs kept their factory paint in the crates. Watch item: SolarArray is 24% foil, on the backs only. Optional: bright weld beads at the joints, to read "built now". |
| `Decal_OldLanderBlast`, rocks | — | OK | era-neutral |

**Mismatch list for Argon:** seismic station, geophone kit (4), retroreflector, sample rack, Scout, SSR-3 family (8),
Kestrel-3 (6). All are material swaps, plus four geometry changes: the station's Lumen junction box, the optional
RTG, a Kestrel piece to replace the landing foot, and the Scout's mirror patches.

## 5. Story conflicts with v4 (for the Director)
1. **Kestrel-3 has two identities.** Story v4 §2.2 has Meridian's relay *satellite* coming down in October 1988, and
   that fact carries plot: it is why Meridian routes through Lumen's dish. But `text/crew-map.md` and
   `text/crew-logs.md` call it "the 1971 survey probe" (`map.site.kestrel.note`, `crate.kestrel.note`),
   and the model has a landing foot. **Recommendation:** keep v4, fix the two text docs, and give the wreck Tycho's
   livery as above. A foreign white-and-teal wreck in the basin also seeds Meridian early.
2. `crew-map.md` gives "1985–1988 Lumen crew"; v4 says commissioned 1984. These fit as "built 1984, crewed from
   1985". Write it that way in both docs.
3. `crew-map.md` makes the cargo lander "the 1987 resupply drop", but `07-identity.md` has 07 *unloading* it on
   28 Oct 1988. Either it landed in October 1988 for the pack-out, or 07 was *loading* it. Pick one.
4. The seismic station's scan line (basin-dressing §4) should carry both eras:
   `SEISMIC STATION · DEPLOYED 1971 · LUMEN BUS 1984 · LAST RECORD 28 OCT 1988 · CELL 0%` `[PROPOSAL]`.
5. **Gap:** a 1971 crew would leave a descent stage. Lumen cleared it off its landing field. Proposed for 0.6: its
   stripped frame by the workshop as Kenji's parts stand, which is era S in the middle of era L.
