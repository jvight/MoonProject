# R1 Stylized-Realism Art & Blender Pipeline Guide

This document defines the authoring, UV/trim-sheet, baking, and FBX export conventions for **R1 ("stylized realism")** hard-surface assets in *MoonProject* (`0.5.7`). Every model exported into `Assets/_Project/Art/R1/Models/` is validated and configured automatically by `R1ModelPostprocessor`.

---

## 1. Units & Coordinate Axes

- **Scene Units:** Metric (`Unit Scale = 1.0`, `1 Blender unit = 1 metre` in Unity).
- **Apply Transforms:** Always apply object **Scale** (`Ctrl+A -> Scale`, `1, 1, 1`) before adding bevel modifiers or exporting.
- **Coordinate Orientation:**
  - In Blender: `-Y` is forward, `+Z` is up, `+X` is right.
  - On FBX export, set **Forward = `-Z Forward`**, **Up = `Y Up`**, and enable **Apply Transform** so that in Unity `+Z` is forward, `+Y` is up, and `+X` is right with identity root rotation (`0, 0, 0`).

---

## 2. Pivot & Node Naming Contracts

- **Root Pivot:**
  - Grounded props, habitat modules, towers, and rovers place the root origin at `(0, 0, 0)` on the **ground contact plane** directly beneath the centre of mass.
  - Socket-mounted attachments (kit pieces, lamps, salvage fragments) place their root origin at the **mounting socket contact point** with `+Z` pointing outward along the socket normal.
- **Articulated & Gameplay Child Nodes:**
  - Preserve exact hierarchy names expected by gameplay and animation rigs (for example, Rover 07 requires `Chassis`, `Neck`, `Head`, `Eye`, `Eyelid`, `SolarWing`, `HeadlampSocket`, `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`).
  - Place each articulated child's local pivot at its physical hinge or rotation axis.
  - Suffix non-rendering locator nodes with `Socket` or `Anchor` (`LampSocket_0`, `LensSocket`, `CabinetSocket`).

---

## 3. Bevel Width & Weighted Normals

R1 hard-surface forms rely on **chamfered silhouettes** that catch both cold directional Earthlight and warm local point/spot lamps without high-poly normal-map bakes on every bracket:

| Feature Tier | Bevel Width | Segments | Profile | Notes |
| :--- | :--- | :--- | :--- | :--- |
| Primary hull shells & rover body | `15 mm – 25 mm` (`0.015 – 0.025 m`) | `2` | `0.5` | Gives a broad, readable specular highlight at 5–30 m chase distance. |
| Secondary frames, hatches, struts | `6 mm – 12 mm` (`0.006 – 0.012 m`) | `1 – 2` | `0.5` | Keeps mechanical edges crisp without aliasing. |
| Small fasteners, latches, bezels | `2 mm – 4 mm` (`0.002 – 0.004 m`) | `1` | `0.5` | Single-segment 45° chamfer + weighted normals. |

- **Weighted Normals Setup (Blender):**
  1. Enable **Shade Auto Smooth** (`30°–45°`).
  2. Add a **Bevel** modifier (`Limit Method = Angle`, `Angle = 30°`, `Harden Normals` off if followed by Weighted Normal).
  3. Add a **Weighted Normal** modifier at the bottom of the stack with `Weight = 50`, `Keep Sharp = On`, and `Face Influence = On`. Large flat hull plates keep flat-planed normals while bevel strips carry the smooth transition.

---

## 4. Fixed Blender Material Slot Names

Every mesh primitive exported under `Assets/_Project/Art/R1/Models/` must assign **only** material slots from the 11 fixed names below. `R1ModelPostprocessor` disables embedded FBX material creation (`materialImportMode = None`), remaps each slot to `Assets/_Project/Art/R1/Materials/<Asset>.mat`, and logs a `Debug.LogError` (with the model path) if an unknown slot name is encountered.

| Blender Slot Name | Unity Material Asset | One-Line Intended Use |
| :--- | :--- | :--- |
| `R1_CreamEnamel` | `R1_CreamEnamel.mat` | Primary warm-cream baked enamel hull plating for rover shells, habitat modules, and companion bodies. |
| `R1_OrangeStripe` | `R1_OrangeStripe.mat` | High-visibility worn international-orange accent stripes, grab handles, hatch rings, and safety bands. |
| `R1_GreyMetal` | `R1_GreyMetal.mat` | Secondary cool-grey painted structural frames, chassis ribs, mounting brackets, and recessed bays. |
| `R1_BareSteel` | `R1_BareSteel.mat` | Unpainted machined steel for actuator rods, latches, fasteners, wheel hubs, and exposed mechanisms. |
| `R1_Rubber` | `R1_Rubber.mat` | Matte dark elastomer for dust bellows, articulated joint boots, pressure seals, tyres, and cable sheathing. |
| `R1_GoldFoil` | `R1_GoldFoil.mat` | Crinkled Kapton multi-layer thermal insulation blankets tucked into instrument cavities and lander undersides. |
| `R1_DarkGlass` | `R1_DarkGlass.mat` | Glossy dark-tinted optical glass for camera domes, sensor windows, and unlit instrument bezels. |
| `R1_Copper` | `R1_Copper.mat` | Warm metallic copper for antenna coils, heat-pipe fittings, grounding straps, and relay terminals. |
| `R1_SagePatch` | `R1_SagePatch.mat` | Muted sage-green field-repair patches, stowage covers, and secondary maintenance access panels. |
| `R1_LampAmber` | `R1_LampAmber.mat` | Bloom-safe warm amber indicator lamps, porch floodlight lenses, and beacon emitters with a small HDR core. |
| `R1_Screen` | `R1_ScreenPlaceholder.mat` | Plain dark-glass placeholder assigned to the `R1_Screen` slot and reserved for the CRT face shader slice. |

---

## 5. Texel Density & Trim-Sheet / UV Rules

- **Target Texel Density:** `256 px/m` (`2.56 px/cm`) for hero rover/companion/habitat surfaces using `256x256` per-slot detail maps or the `512x512` shared trim sheet (`T_R1_TrimSheet_*`).
- **UV Channel Contract:**
  - `UV0` (`TEXCOORD0`): Primary material UVs.
    - When assigning the per-slot library materials (`R1_CreamEnamel` .. `R1_LampAmber`), each slot owns a full `[0, 1]` tileable texture set (`T_R1_<Surface>_Albedo/Normal/Mask`), so standard Smart UV Project or seam unwraps in `[0, 1]` never bleed across neighbouring trim bands.
    - Keep UV islands aligned horizontally (`U` along panel length, `V` along panel height) so brushed metal grain, rubber ribs, and panel bevel borders orient consistently.
  - `UV1` (`TEXCOORD1`): Optional static lightmap UV (`Pack Islands` with `Margin >= 0.02`).
- **Regenerating Library Textures:**
  - Run `python tools/art/r1/generate_r1_textures.py` to deterministically regenerate all 35 PNG maps in `Assets/_Project/Art/R1/Textures/`.

---

## 6. Mask Map Channel Packing & Baking (`_MaskMap`)

`MoonProject/R1/Lit` samples a single linear RGBA mask texture (`sRGBTexture = false`) with the following channel layout:

| Channel | Content | Range & Neutral Value | Shader Usage |
| :--- | :--- | :--- | :--- |
| **R** | **Metallic** | `0.0` (dielectric) .. `1.0` (metal) | Scaled by `_MetallicScale`; overridden toward `_EdgeWearMetallic` on worn edges and `0.0` under dust. |
| **G** | **Ambient Occlusion** | `0.0` (occluded cavity) .. `1.0` (open) | Attenuates indirect lighting via `_OcclusionStrength` and drives crevice dust accumulation (`1.0 - G`). |
| **B** | **Curvature / Edge Mask** | `0.5` = flat neutral, `> 0.5` = convex ridge, `< 0.5` = concave crease | Values above `_EdgeWearThreshold` (`~0.56`) modulated by 3D world noise drive procedural `_EdgeWearColor`; values `< 0.5` boost crevice dust. |
| **A** | **Smoothness** | `0.0` (rough) .. `1.0` (mirror smooth) | Remapped into `[_SmoothnessMin, _SmoothnessMax]`; modified by edge wear and lunar dust (`_DustSmoothness`). |

### Baking Curvature & AO in Blender (When Custom Bakes Are Needed)
1. **Ambient Occlusion (`G`):** Bake Cycles `Ambient Occlusion` (16–32 samples, distance `0.25 m`, normalized) into a linear greyscale image.
2. **Curvature (`B`):** In a Cycles shader node tree, wire `Geometry -> Pointiness` through a `Map Range` node (`From Min = 0.42, From Max = 0.58 -> To Min = 0.0, To Max = 1.0`) so flat surfaces sit at `0.5`, convex bevels approach `1.0`, and concave seams approach `0.0`. Bake as `Emit` with `Color Space = Non-Color`.
3. **Channel Pack:** Combine `R = Metallic`, `G = AO`, `B = Curvature`, `A = Smoothness` via Blender's Compositor (`Combine Color` RGBA) or a Python `numpy`/`Pillow` script and save as `*_Mask.png` under `Assets/_Project/Art/R1/Textures/`.

---

## 7. Bloom-Safe Emission Contract

- The project's URP Volume bloom threshold is `Palette.BloomThreshold = 1.25`.
- `MoonProject/R1/Lit` keeps the broad body of emissive surfaces (`R1_LampAmber`) clamped at `_BloomSafeMax` (`1.06`) so indicator housings read as crisp warm shapes without washing out adjacent bevels, while pixels whose emission map luminance exceeds `_HotCoreThreshold` (`0.85`) ramp up to `_HotCorePeak` (`1.65`) to trigger a tight, soft HDR halo.
- Runtime scripts modulate glow intensity using the project's existing linear `MaterialPropertyBlock` contract (`block.SetVector("_EmissionColor", new Vector4(r, g, b, 1f))`).

---

## 8. Blender FBX Export Settings & Unity Import Rules

When exporting from Blender (`File -> Export -> FBX (.fbx)`) into `Assets/_Project/Art/R1/Models/`:

- **Include:** `Selected Objects`, `Object Types = Empty, Mesh` (no Cameras, Lamps, or Armatures unless rigged).
- **Transform:**
  - `Scale = 1.00`
  - `Apply Scalings = All Local`
  - `Forward = -Z Forward`
  - `Up = Y Up`
  - `Apply Unit = On`
  - `Use Space Transform = On`
  - `Apply Transform = On`
- **Geometry:**
  - `Smoothing = Normals Only`
  - `Apply Modifiers = On`
  - `Tangent Space = Off` (Unity computes MikkTSpace tangents on import)
  - `Triangulate Faces = Off` (or `On` for curved domes)

### Enforced by `R1ModelPostprocessor` (`Assets/_Project/Scripts/Art/Editor/R1ModelPostprocessor.cs`)
- `globalScale = 1f`, `useFileUnits = true`, `bakeAxisConversion = true`
- `importNormals = ModelImporterNormals.Import`, `importTangents = ModelImporterTangents.CalculateMikk`
- `materialImportMode = ModelImporterMaterialImportMode.None` (never extracts duplicate `.mat` files)
- `isReadable = false`, `importAnimation = false`, `importCameras = false`, `importLights = false`
- Automatically maps the 11 `R1_*` slot names to `Assets/_Project/Art/R1/Materials/*.mat` and fails with `Debug.LogError` on any unrecognised slot name.
