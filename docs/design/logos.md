# Brand Identities for the Makers and Agencies of Lunarion (`LOGOS`)

> **Status:** `[PROPOSAL]` — Concept Directions (`a`, `b`, `c`) for Owner Selection  
> **Review Gallery:** `Logs/logos/index.html`  
> **Deterministic Generator:** `tools/art/logos/build_logos.py`  
> **Related Specs:** `docs/design/story-v4.md` (§1, §3.1), `docs/design/07-identity.md` (§1, §3), `docs/VISION.md` (Palette), `Assets/_Project/Shaders/R1/R1CrtFace.shader`

---

## 1. Overview & Rules

Like Alterra in *Subnautica*, the machines and stations of *Lunarion* belong to distinct makers and agencies from the 1969–1988 Departure era so technology feels connected and the robot ecosystem reads as part of a real industrial history. When **07** boots, its curved amber CRT face displays its maker's emblem (`KESSLER–ARAI FIELD SYSTEMS · SSR-5`) before the self-test lines.

### Core Design Constraints
1. **True 1970s–1980s Print & Hardware Era:** Flat vector geometry, period space-agency insignia, Swiss waystation grids, and industrial stencil marks. Zero modern gradients, zero 3D chrome, and zero AI gloss.
2. **CRT-Friendly Monochrome Compatibility:** Every mark reduces cleanly to a 1-colour stencil or amber phosphor CRT stamp.
3. **Owner Rule (Strict):** Zero clusters of identical dots or holes (trypophobia) in any motif. All marks use solid geometric planes, continuous ribbons, chevrons, and clean line-art bars.
4. **Deliverables (`1024×1024` PNG):**
   - Transparent-background RGBA lockups: `Logs/logos/<brand>/{a,b,c}.png`
   - On-black presentation frames: `Logs/logos/<brand>/{a,b,c}_black.png` + `Logs/logos/<brand>/on_black.png`
   - 07 1-colour amber CRT boot screen mockups: `Logs/logos/kessler-arai/{crt,crt_a,crt_b,crt_c}.png`
   - Side-by-side HTML comparison gallery: `Logs/logos/index.html`

---

## 2. The Three Brands & Concept Directions

### 2.1 Brand 1 — **ILC** (*International Lunar Commission*, 1969 onward)
- **Role in Story (`story-v4.md` §2.2, §3.1):** The international lunar programme formed after the July 1969 Moon landing to operate the lunar waystations (`LUMEN-04`), hydrological surveys (`"WATER IS GRAVITY"`), and the Earth–Moon L4/L5 Ark transit corridor.
- **Visual Family & Palette:** Earnest, geometric, international 1970s/80s space-programme print style using Cream (`#F4E6C8`), Coral-Orange (`#FF8A5B`), Warm Amber (`#FFB547`), Earth Blue (`#3FA7D6`), and Deep Indigo (`#10142A`).

| Direction | Files (`Logs/logos/ilc/`) | One-Line Rationale |
|---|---|---|
| **`a` (★ Pick)** — *1975 Orbital Worm & Crescent* | `a.png`, `a_black.png` | 1975 Danne & Blackburn "Worm"-inspired tubular logotype paired with an Earth-to-Moon transfer ellipse and L4/L5 apex star—earnest, optimistic, and instantly readable on a station bulkhead. |
| **`b`** — *1969 Apollo-Era Mission Seal* | `b.png`, `b_black.png` | 1969–78 Apollo/UN-era circular agency insignia uniting Earth's meridian dome, the lunar disc, and a rising coral-orange delta vector—built for suit patches and lander hull decals. |
| **`c`** — *1982 Swiss Waystation Grid* | `c.png`, `c_black.png` | 1982 Lance Wyman / Otl Aicher architectural grid badge combining the L4/L5 transit diagram, stencil "ILC", and hydrological strata bars for silkscreened station bulkheads. |

---

### 2.2 Brand 2 — **Kessler–Arai Field Systems** (*Robot Maker of 07 & the SSR-3 Elders*, 1983)
- **Role in Story (`07-identity.md` §1, §3):** The German–Japanese field-robotics manufacturer contracted by the ILC that built **07** (`SSR-5 Station Service Rover, S/N 3-0417, 1983`) and the older single-purpose **SSR-3** elders (`01–06`).
- **Visual Family & Palette:** Industrial, friendly, reliable field equipment in the **Cream & Orange family** (`#F4E6C8` Cream, `#FF8A5B` Coral-Orange, `#FFB547` Warm Amber, `#1A1410` Warm Dark Enamel).

| Direction | Files (`Logs/logos/kessler-arai/`) | One-Line Rationale |
|---|---|---|
| **`a` (★ Pick)** — *KA Single-Lens & Rocker-Bogie Badge* | `a.png`, `a_black.png`, `crt.png`, `crt_a.png` | Chunky rounded-bezel industrial monogram uniting a stencil "K–A" with 07's hooded single-lens eye and rocker-bogie suspension link—warm, approachable, and unmistakable on 07's 1-colour amber CRT. |
| **`b`** — *Rising Solar Disc & Crater Lozenge* | `b.png`, `b_black.png`, `crt_b.png` | 1980s Komatsu/Braun-inspired heavy machinery stamp pairing a warm rising solar disc (*Arai*) with a stepped crater-ridge chevron (*Kessler*) inside chamfered chassis bumper clamps. |
| **`c`** — *1983 SSR-5 Stamped Chassis Nameplate* | `c.png`, `c_black.png`, `crt_c.png` | 1983 stamped-enamel equipment nameplate pairing a geometric 2D SSR-5 rover profile with a split-block "K/A" stencil and `CONTINUE` bus spec bars, ready to weld onto 07's rear flank. |

#### 07 CRT 1-Colour Amber Boot Screen (`Logs/logos/kessler-arai/crt.png`)
- **Shader & Palette Alignment (`R1CrtFace.shader`):** Warm phosphor base `#20140A` → `#120B05`, beacon amber `#FFB547` / `#FFAE38`, hot filament core `#FFF3CC`, dual-radius Gaussian phosphor halo bloom, and 270 horizontal CRT scanlines framed inside 07's cream/coral visor hood bezel.
- **Boot Readout Sequence (`07-identity.md` §3):**
  ```text
  KESSLER–ARAI ROM BIOS v1983.4                       BUS: CONTINUE [LOCKED]
                     [ KESSLER–ARAI EMBLEM ]
               KESSLER–ARAI FIELD SYSTEMS · SSR-5
  ══════════════════════════════════════════════════════════════════════════
  ILC · STATION SERVICE ROVER · S/N 3-0417 · BUILT 1983
  UNIT 07 · SSR-5 · CONTINUE · 13 148 DAYS
  OPTIC PROFILER Ø 0.245m ———————————— [OK]   CUTTER TURRET ———————————— [OK]
  ADAPTIVE SCHEDULER ————————————————— [WEIGHTS RETAINED · 36Y DRIFT]
  TRICKLE POST: WAKE ON CARRIER —————— [ORBITAL SIGNAL · DETECTED]
  > UPLINK · OFFLINE   //   BEGIN LUMEN RECOVERY PROTOCOL █
  ```

---

### 2.3 Brand 3 — **Tycho Consortium** (*Automated Meridian Station Operator*)
- **Role in Story (`story-v4.md` §0.2, §2.2, §3.1, §7 Q3):** The rival industrial prospecting cooperative operating **Meridian Station** (`0.6+`), a 100% automated, uncrewed radar-sounding and resource facility high on the Rim Terraces.
- **Visual Family & Palette:** Colder, corporate, uncrewed industrial geometry in **Clean White (`#F0F6F7`) + Muted Teal (`#489EA2` / `#2A686E`)** over dark slate composite (`#10181C`), built around hexagonal contact-dock motifs.

| Direction | Files (`Logs/logos/tycho/`) | One-Line Rationale |
|---|---|---|
| **`a` (★ Pick)** — *Hex-Dock & Isometric TC Prism* | `a.png`, `a_black.png` | Precision hexagonal contact-dock emblem forming an interlocking isometric "TC" prism in stark white and muted teal—cold, corporate, and built to double as the physical alignment target on Meridian's hex docks. |
| **`b`** — *Deep-Radar Hexagonal Sounding Array* | `b.png`, `b_black.png` | Downward-stepping half-hexagonal radar chevrons beneath a stark white lunar datum spire, visually encoding Meridian Station's automated 36-year deep-ice radar soundings. |
| **`c`** — *Tri-Facet Hexagonal Syndicate Turbine* | `c.png`, `c_black.png` | Late-1970s brutalist corporate mark built from three solid interlocking isometric chevron wings arranged in a 120° hexagonal turbine around a solid white core prism (zero holes). |
