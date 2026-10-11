# Trial: modelling through Blender, and the Lunarion wordmark (2026-10-12)

The owner asked two things: is it better, in quality and in token cost, for Claude to model through Blender rather than
through our C# mesh builders; and can we have a real Lunarion logo. This trial built one hero model, rover 08
(gdd-v3 §13.3), entirely through Blender, and set the wordmark from the project's own Be Vietnam Pro.

## What was built
- **`tools/blender/rover08.py`** (bpy, the source of truth; no .blend is versioned). It starts from an empty scene and
  builds 08 from primitives with Blender's own tools: weighted and angle **bevels** (cabinet corners, chamfers, tyre
  shoulders), an exact **boolean** (the lens recess), a **mirror** (head bolts), **inset + solidify** (solar cells),
  **text to mesh** (the "08" serial, three times), and a **Cycles AO bake** with a ground plane, quantized into UV1.x
  (the occlusion LofiLit reads). Each face's material index *is* its `PaletteSwatch`; the export writes UV0 at the
  swatch's cell centre, drops the materials and exports FBX. 16 parts, 5,108 triangles.
- **Unity side (Art/Editor):** `Rover08ModelBuilder` (`[MoonBuilder("Art/Rover08")]`) writes the palette as JSON from
  `Palette.cs` (single source), runs Blender through `BlenderProcess`, copies the FBX into
  `Generated/Art/Rover08/` only when its bytes change, and writes `Rover08Model.prefab` (M_LowPoly, rig-contract node
  names, fails fast if a part is missing or the lens does not face +Z). `Rover08ImportRules` owns the FBX import
  settings in code. `RoverFamilyCapture` renders 08 beside 07 in the Main scene.
- **Design of 08:** 07's family (six wheels on rocker-bogies, rounded radio-cabinet body, slender neck, one lens),
  a tenth smaller and clean: no rust, dust or patch, six matching wheels, a straight antenna with a pilot tip (its
  own node, for the call-sign blink), fresh WarmAccent stripe with cream "08" on both flanks and the back, an intact
  pair of folded wings where 07 has one tired wing, and a young cyan eye (`EyeGlass`) where 07's is amber.
  Not WALL-E: one lens; small metal bolts at the head's sides, because dark round pods there read as a second eye in
  the first preview.

## Evidence
- Blender preview: `MoonProject-wt/blender/Logs/blender-trial/rover08_preview.png` (Workbench, palette colours times
  the baked occlusion).
- Unity, Main scene, 08 (right) beside 07: `Logs/blender-trial/rover07_08_chase.png`, `rover07_08_30m.png`.
- Determinism: two Blender runs, and the shell run against the Unity builder's run, give byte-identical FBX files.
  The Unity builder takes 1.4 s for Art/Rover08 including Blender. `CheckIdempotency` (filter `^Art/Rover08`):
  pass 2 reproduced all 1,069 watched files byte for byte.

## Measured cost
| | |
|---|---|
| Context tokens, 08 work (get_usage before / after) | 130,042 -> 262,955: **~133k**, of which ~15k was the wordmark run in parallel |
| Weekly quota | 23% -> 24% |
| Wall-clock | 25 min (00:00 -> 00:25), incl. ~4 min first import of a fresh worktree |
| Blender iterations | 4 script runs: a typo, a bpy reference invalidated by adding a UV layer, then one design pass (second-eye read, hidden serial) |
| Unity iterations | 4 builder runs (axes twice, a stray UV map once), 2 captures |
| Source size | rover08.py 590 lines (geometry ~350) + 370 lines of reusable C# glue; 07: RoverMeshes 745 + RoverModelBuilder 278, on LowPolyMeshBuilder's 1,048 |

About half of the 133k went into one-time pipeline problems (axes, UV order, determinism), which the next Blender model
does not pay again. Geometry itself costs about the same tokens in bpy as in C#: both are code; Blender's modifiers
make rounded, cut and lettered shapes shorter to express, and its 2-second headless preview is far cheaper to look at
than a Unity batch run.

## Comparison
| | C# builders (07) | Blender bpy (08) |
|---|---|---|
| **Quality on the captures** | 07 has more character, from its weathering coats; flat, clean silhouette | Same family at chase and at 30 m; rounder corners (3-segment bevels) and real AO, but at game distance the flat palette shading dominates and the gain is small |
| **Bevels, rounds** | Hand-written RoundedSlab / Bevel helpers | One modifier line each (weight- or angle-limited) |
| **Booleans** | None (parts are stacked) | Exact boolean, materials kept by index |
| **Text, serials** | Glyph polygons by hand ("7"), a torus for "0" | Any string in a real font, three lines |
| **AO** | Height-based foot darkening (GroundOcclusion) | Ray-traced AO including self-occlusion and the ground |
| **Normals** | Flat by construction | Triangulate + flat faces; exact, no trouble |
| **Determinism** | Native | Not out of the box: the FBX exporter stamps now() and ids from Python's per-process string hash. Fixed by patching two private add-on functions, pinned to Blender 4.5.9 |
| **Import step** | None (meshes written as .asset) | The axis conversion bit twice: `bake_space_transform` corrupts grandchild nodes (Eye), a Y-up export leaves rotations on the roots; Z-up export + Unity's `bakeAxisConversion` is clean. Settings must live in an AssetPostprocessor because box metas are not versioned |
| **Palette mapping** | Typed `PaletteSwatch`, compile-time checked | Material index = swatch index, palette JSON written from C# per build; foreign UV maps (from text meshes) must be stripped or UV0 is wrong |
| **Iteration loop** | A Unity batch run to see anything | 2 s headless Blender preview, then one Unity run |
| **Toolchain** | Unity only | Blender 4.5.9 on every machine that runs builders (box_check, land, verify); `MOON_BLENDER` overrides the path; the builder fails fast without it |
| **Weathering** | Mature coat/skin system (rust, dust, LofiWeather) | Not ported; a weathered Blender model would need the coats re-done in bpy or a C# pass over the imported mesh |
| **Shared constants** | Rover code reads WheelRadius, sockets, etc. | Constants live in Python; C# can only validate node names after import |
| **Maintenance** | One language, one tool, tests next to it | Two languages, a pinned external tool, private-API patches to re-check on every Blender upgrade |

## Recommendation
1. **07 stays in C#.** It is finished, rigged, weathered by the coat system and its constants are shared with Rover
   code; a port costs tokens and risk for no visible gain at game distances.
2. **The lander and future hero models go through Blender** when the shape wants booleans, bevels, curves, lettering
   or real AO (the finale's lander, its ramp and hatches, the radio tower's dish, Bell). Keep **C#** for parametric
   families made in bulk or by seed (rocks, props, kit pieces) and for anything gameplay code shares constants with.
3. Before the second Blender model, spend one small task on the pipeline: a shared `tools/blender/moonfbx.py` (palette
   reading, deterministic export), one import-rules class for a `Generated/Art/Blender/` folder, a `tools/tests` check
   that two runs give identical bytes, and the Blender path in HANDOFF §4.

## The wordmark
- `tools/blender/wordmark.py` sets "Lunarion" in `Assets/_Project/UI/Fonts/BeVietnamPro-Regular.ttf` (OFL), reads
  the outlines back as cubic Beziers and writes them as SVG paths, kerns the L closer to the u, and replaces the i's
  dot with a thin crescent drawn as two arcs. The preview is rendered from the written SVG files, imported back.
- Files: `docs/design/lunarion-wordmark-on-dark.svg` (cream #F4E6C8 on the night sky, the title's version),
  `lunarion-wordmark-on-light.svg` (charcoal on cream, for light pages), `lunarion-wordmark-preview.png`.
- The crescent bulges left with its horns to the right and stands nearly upright. Tilted further, it read as an
  apostrophe. Opening upward, it would read as a breve, as in Vietnamese "ă".
- Open for the owner: Regular (calm, chosen) or Medium (sturdier over bloom); cream or amber crescent. Not
  integrated into the title until approved.
