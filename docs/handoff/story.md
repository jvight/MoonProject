# Story & flow box: handoff (2026-10-19)

## Canon to read first
- `docs/design/story-v4.md`: The Departure, 1988, CONTINUE, 36 years.
- `docs/design/07-identity.md`: SSR-5, Kessler–Arai 1983; the elders are SSR-3.
- `docs/design/basin-dressing.md`: lures, guide lines, the Scout, the crew traces, and the in-engine numbers.
- `docs/design/tech-eras.md` (new, box/cl-tech-eras): the visual technology bible.
  - Five eras: P probes 1966–68, S sorties 1969–77, B build-out 1978–83, L Lumen 1984–88, N now 2024. Meridian runs
    in parallel to L.
  - The progression: foil and bare metal → white enamel → cream and coral with a CRT → cream with fresh welds.
  - An audit of every R1 FBX on main, by measured material coverage.

## State
- The tech-eras audit found 7 mismatch groups, all made in Lumen livery when they belong to an older era:
  - the seismic station, the geophone kit, the retroreflector and the sample rack (S 1971);
  - the Scout (P 1967);
  - the SSR-3 family (B 1978);
  - Kestrel-3 (Meridian 1988).
- Art needs new material slots before Argon can rework these:
  - `R1_WhitePaint` (required);
  - `R1_TealMark` (required for Kestrel-3);
  - `R1_SilverFoil` (recommended).
- Waiting on the Director (tech-eras §5):
  - Kestrel-3's identity: story v4 says Meridian's relay satellite, while the text docs say the 1971 probe. The
    recommendation is to keep v4;
  - the cargo lander: did it land in 1987, or was 07 unloading it in October 1988?
  - the seismic station's two-era scan line.

## Next for this box
- After the Director rules on §5: fix `docs/design/text/crew-map.md` and `crew-logs.md` (Kestrel-3, and Lumen
  "built 1984, crewed from 1985").
- Put the new scan line into `basin-dressing.md` §4.
- Brief the 0.6 descent-stage prop (era S at Kenji's workshop).

## Makers & agencies (tech-eras §6, a87084df)
- Three marks:
  - ILC, the programme: a round seal; the crescent stays Lumen's station patch;
  - Kessler–Arai, the SSR rovers: a vector mark on 07's CRT at boot;
  - Tycho, Meridian: a teal hexagon.
- VIS, Vanguard Interplanetary Structures (decided, owner 2026-10-19; it replaces the earlier VIS proposal): ILC's structures contractor (lander, habitat, arrays, flat-packs, Atlas). It
  also built the 1967 Scout, so the Scout wears VIS's 1960s wordmark and needs no fourth logo.
- Next: maker databank pages and maker lines on scan pages, once Argon's logos land.

## Rulings applied (e64547b3) and the databank
- The text docs now match the Director's rulings: Kestrel-3 is Meridian's relay satellite; Lumen is built in 1984
  and crewed from 1985; the cargo lander landed in the 1987 drop with its sister, the Canyon Supply Lander, and 07
  unloaded its last cargo in October 1988; the seismic scan line carries both eras.
- The 1971 descent stage is approved for the 0.6 roadmap. The Director adds it to `docs/ROADMAP.md`.
- `docs/design/databank.md` is written, with its entries in `text/databank-entries.md` (18 entries, en and vi).
  - The cards are Lumen's crew-written catalogue, which ends in 1988; anything newer shows `NO LUMEN RECORD`.
  - ui-fit implements it next.

## Maker understanding (box/cl-maker-understanding)
- `docs/design/maker-understanding.md`: each scanned databank entry gives +1 to its maker. Small thresholds unlock
  adapted blueprints: the donor part keeps its era and livery, joined by 07's weld beads and a `07` tag.
- Families: Kessler–Arai, 07 itself (fastest); VIS, base structures; ILC, instruments; Tycho, automation (slowest).
- Points derive from the databank, so there is no new save state.
- Open questions:
  - the survey flock's maker (living-moon says Lumen, v4 says Tycho);
  - the game-design session should fold the re-sourcing into `craft-catalogue.md`.
- **Ruled and folded** (box/cl-catalogue-fold, stacked on 5855d572):
  - the survey flock is Meridian's hex flock (0.6-living-moon is fixed; it charges by induction on any lit ring);
  - `craft-catalogue.md` and `blueprint-fit.md` now carry the 10 adapted blueprints and the 5 re-sourced ones. The
    catalogue counts 39 structures, 26 device tiers, 11 machines: 88 things to make.

## Where this box stands (2026-10-19, end of session)
- Done: tech-eras (eras, audit, makers), the text rulings, the databank and its 18 entries, VIS, maker
  understanding, the catalogue fold.
- Next:
  - after Argon's logos, the maker databank pages;
  - port `text/databank-entries.md` and the `und.*` and `adapted.*` keys into `en.json` / `vi.json` once UI asks;
  - the 0.6 databank entries for the mare, with each machine's maker.
