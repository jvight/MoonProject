# Studio Identity Specification — "Outsight" (`OUTSIGHT` / `Outsight Games`)

> **Status:** `[PROPOSAL]` — Four Distinct Concept Directions (`a`, `b`, `c`, `d`) & 5-Second Boot Splash Storyboards for Owner Pick  
> **Side-by-Side Review Gallery:** `Logs/studio/index.html`  
> **Deterministic Generator:** `tools/art/studio/build_studio_logos.py`  
> **Related Specs:** `docs/VISION.md`, `ProjectSettings/ProjectSettings.asset` (`m_SplashScreenBackgroundColor: #0B0E2A`)

---

## 1. Studio Philosophy & Design Rules

- **Studio Name:** **Outsight** (shown as **`OUTSIGHT`** or **`Outsight Games`**) — the owner's studio for **all future titles**.
- **Meaning:** The opposite of *insight* — looking outward beyond what humans know; games about worlds and perspectives nobody has made, places people have not explored or understood yet.
- **Tone & Character:**
  - **Not moon-specific, not sci-fi-only.** No craters, no rovers, no orbital diagrams, and no sci-fi HUD reticles.
  - **Timeless, calm, curious, a little mysterious.** Built from enduring modernist geometry that works across any future genre or world.
  - **Scale-invariant:** Reads unmistakably at **16×16 / 32×32 px** (browser/Steam favicon) and at **1024×1024 px / 4K** (boot splash screen).
  - **Owner Rule (Strict):** **Zero clusters of identical dots or holes (trypophobia).** Every direction uses continuous geometric ribbons, clean arcs, and solid focal discs.

### Studio Surface & Ink Palette

| Surface / Role | Hex | RGBA | Notes |
| :--- | :--- | :--- | :--- |
| **Gallery Black (`on_black.png`)** | `#0B0C10` | `(11, 12, 16, 255)` | Deep obsidian background for dark lockups |
| **Archival Off-White (`on_offwhite.png`)** | `#F4EFE6` | `(244, 239, 230, 255)` | Warm cotton paper background for light lockups |
| **Calm Boot Navy (`splash_1..4.png`)** | `#0B0E2A` | `(11, 14, 42, 255)` | Exact match to `ProjectSettings.asset` `m_SplashScreenBackgroundColor` & `VISION.md` Sky Top |
| **Primary Ink on Black** | `#F4EFE6` | `(244, 239, 230, 255)` | Warm ivory primary mark & wordmark ink |
| **Primary Ink on Off-White** | `#12141C` | `(18, 20, 28, 255)` | Deep charcoal ink on off-white paper |
| **Focal Warmth (Dark / Light)** | `#E2A856` / `#C47A2C` | `(226, 168, 86)` / `(196, 122, 44)` | Single restrained ochre-amber focal element (degrades cleanly to 1-bit monochrome) |

---

## 2. Four Distinct Studio Logo Directions (`1024×1024` PNG)

| Direction | Files (`Logs/studio/<dir>/`) | Wordmark Lockup | One-Line Rationale |
| :--- | :--- | :--- | :--- |
| **`a` (★ Best Pick #1)** — *The Outer Aperture* | `on_black.png`, `on_offwhite.png`, `transparent.png`, `favicon_64.png`, `splash_1..4.png`, `splash_storyboard.png` | `OUTSIGHT` + `GAMES` | A circular boundary ("O") that parts on its upper-right arc as a calm vesica lens looks outward beyond the ring—turning "insight" inside out into a timeless mark that reads at 16 px and 4K. |
| **`b`** — *The Open Horizon* | `on_black.png`, `on_offwhite.png`, `transparent.png`, `favicon_64.png` | `OUTSIGHT` + `GAMES` | A quiet horizontal datum that parts at its center to let a vaulted portal and warm perspective light-beam pass across the threshold—evoking an unmapped doorway on the edge of a calm horizon. |
| **`c` (★ Best Pick #2)** — *The Leaving Line* | `on_black.png`, `on_offwhite.png`, `transparent.png`, `favicon_64.png`, `splash_1..4.png`, `splash_storyboard.png` | `OUTSIGHT` | A single unbroken geometric ribbon that traces a circle, smoothly inflects across its center, and departs straight through an opening in its own rim—an iconic monogram of leaving the known loop. |
| **`d`** — *Two Perspectives* | `on_black.png`, `on_offwhite.png`, `transparent.png`, `favicon_64.png` | `Outsight Games` | A reversible figure-ground medallion whose negative space reads simultaneously as an open eye with a warm pupil and as a canyon portal opening onto a distant sun—two perspectives in one shape. |

---

## 3. 5-Second Calm Navy (`#0B0E2A`) Boot "Splash" Storyboards (Best 2 Directions)

Both storyboards are authored for the **5.0-second pre-title boot window** on the calm `#0B0E2A` navy screen (`ProjectSettings.asset`), using smooth cubic ease-in/out opacity and position curves with **zero flashy effects, zero camera snaps, and zero hard cuts**.

### 3.1 Storyboard 1 — Direction `a` (*The Outer Aperture*)
- **Files:** `Logs/studio/a/splash_1.png`, `splash_2.png`, `splash_3.png`, `splash_4.png`, `splash_storyboard.png`
1. **Frame 1 (`0.0 s – 1.2 s` · *Quiet Emergence*):** Calm `#0B0E2A` navy holds for `0.4 s`; the outer circular ring eases in softly at `38%` opacity with a narrow upper-right break.
2. **Frame 2 (`1.2 s – 2.5 s` · *The Ring Parts Outward*):** The upper-right arc of the ring smoothly widens (`cubic ease-out`) as the tilted vesica lens glides outward across the boundary (`78%` reach).
3. **Frame 3 (`2.5 s – 4.0 s` · *Outward Focus & Lockup*):** The warm ochre-amber iris settles at the outer apex of the lens (`100%` opacity) while **`OUTSIGHT`** (`GAMES`) fades up gently below (`y: +10 px → 0 px`).
4. **Frame 4 (`4.0 s – 5.0 s` · *Still Hold & Dissolve*):** After a calm beat of stillness, the ring, lens, and wordmark dissolve softly into the `#0B0E2A` navy dark (`22%`), leaving the warm outward iris point last to fade (`82%` → `0%`) before the game's title screen wakes.

### 3.2 Storyboard 2 — Direction `c` (*The Leaving Line*)
- **Files:** `Logs/studio/c/splash_1.png`, `splash_2.png`, `splash_3.png`, `splash_4.png`, `splash_storyboard.png`
1. **Frame 1 (`0.0 s – 1.2 s` · *The Known Circle*):** On the calm `#0B0E2A` navy screen, the circular arc begins tracing itself softly in warm ivory (`42%` opacity).
2. **Frame 2 (`1.2 s – 2.5 s` · *Tangential Inflection*):** Rather than closing the loop, the ribbon peels smoothly inward with $C^1$ continuity and travels across the center toward the upper-right opening (`80%` stroke progress).
3. **Frame 3 (`2.5 s – 4.0 s` · *Departure Beyond the Ring*):** The single line passes straight through the open rim into uncharted space, illuminating its warm ochre-amber outward tip as **`OUTSIGHT`** (`GAMES`) locks in below.
4. **Frame 4 (`4.0 s – 5.0 s` · *Still Hold & Dissolve*):** The circle and wordmark ease out into the `#0B0E2A` navy dark (`22%`), while the warm departing tip lingers for a final quiet beat (`82%` → `0%`) before transitioning into the game's title screen.
