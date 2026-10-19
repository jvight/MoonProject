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
