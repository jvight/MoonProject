# UE 5.8 port: what changes for design

> **Owner decision (2026-10-26):** port to Unreal 5.8 and freeze Unity feature work. The design docs stay valid and
> are engine-agnostic. This note maps design areas onto UE tools, so the port plan (`docs/port/ue-port-plan.md`, by
> tools) can carry the intent and not just the code.
>
> **Author:** the game design session. No code.

## 1. Where UE changes the design (opportunity · what to keep · risk)
| Design area (doc) | UE tool | Opportunity | Keep exactly | Risk |
|---|---|---|---|---|
| **Regions and the map** (`0.6-world-regions.md`, `base-home-and-crater-exits.md`) | World Partition, PCG, Landscape | real-scale regions streamed seamlessly: the Mare (1.2 × 0.9 km), the terraces and the Peak road as one world; PCG for scatter, boulder fields and ejecta rays | **the adjacency order; gates readable from the opened neighbour; one hand-placed purpose per site** (no procedural loot sprinkle, memory no-floating-loot); the crater stays a closed cradle with three gated ways out | PCG density turning into "empty map" or clutter; hand-author every lure and site |
| **Terrain look** (no-hole-patterns, the scale bible) | Landscape materials, Nanite rocks | crisp vacuum shadows, huge draw distance for the Peak and Earth | **craters few, varied, irregular**; no repeated round dots; 07's 1.5 m scale against every prop | stock lunar or crater megascans bring hole patterns, so filter them |
| **Light and sky** (look day C / night A, the polar Sun, the eclipse) | Lumen GI, a custom Sky Atmosphere set to *no atmosphere*, Directional Light | a true low polar Sun; earthshine as real bounce | **vacuum: no haze, black sky, hard shadows**; earthshine never charges; the sky follows progress, not a clock | UE defaults add atmosphere and fog, so turn them off from day one |
| **Cutscenes** (`opening-cutscene-script.md` Variant D, the boot face) | Sequencer, Cine Camera | D's orbit-to-basin continuous move, the Departure river, the decades time-lapse: all in-engine, no mattes | **ends into the boot face; one swell only; skippable after 2 s; no camera tour around 07** | scope creep; the time-lapse strobing |
| **07's voice and the score** (audio memories: physical, not beepy; no lofi radio) | MetaSounds | procedural pentatonic chirps keyed to state; the call carrier as a live D-pentatonic patch; structure-borne sound in vacuum | **the 5-tone annunciator meanings; physical sources (Sonniss); bespoke score only at milestones** | synth-y defaults sounding "electronic" again; A/B against the Unity renders |
| **The living moon** (`0.6-living-moon.md`: flocks, machine fireflies, phenomena) | Niagara, Mass | swarms (the survey flock, the fireflies), dust that obeys 1.62 g ballistics | **calm, never threat; ballistic dust (no smoke); every effect has a purpose** | heavy VFX breaking calm and frame time |
| **Camera** (camera-player-owned, never-lost G5) | Enhanced Input, a Camera Manager | — | **the player's orbit is sacred: no auto-camera on scan, tow or build**; the G5 "07 looks" nudge is 07's head, not the camera | UE template cameras auto-follow; remove that |
| **Guidance and text** (`opening-v3-guidance-audit.md`, `writing-style-ingame.md`) | UMG/CommonUI, String Tables | the native vi/en String Tables | **one guidance element; the 5-s rule; persistent chips until pressed; the glossary** | the UMG port re-adding defaults (tooltips, quest lists) |
| **Tow and caravan** (`research/caravan-tow.md`) | Chaos physics, cable components | cable rendering for free; the train (C0) | **the breadcrumb follower for cars 2..N**, not a free chain; snag-lift; soft let-go | Chaos chains jackknifing; keep the assisted model |
| **The range ring and the network** (payoff audit, balance §4.1) | — (pure logic) | — | **220 m/cell, 60% start, day-end on the pad, ring from every charge point** | numbers drifting during the port; port them as data tables with the bot's per-beat table as the check |
| **The bot gates** (never-lost, bot misses mouse) | Automation / Gauntlet | the same naive/strict gates in UE | **the G1 5-s check, real pointer input at 16:9 and 21:9** | the port shipping without the gates, so build them first |

## 2. Order suggestion (design's view)
1. **Feel core first:** drive, camera, the dock, scan and cut, so the core feel gate is passed again in UE before
   content.
2. **The opening basin and the guidance**, with the bot gates.
3. **Sequencer Variant D** only after the playable opening passes (memory: opening-cutscene-later).
4. **World Partition regions** (0.6, the Mare) after that.
