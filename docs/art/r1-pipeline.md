# R1 Stylized-Realism Art & Blender Pipeline Guide

This document defines the authoring, UV/trim-sheet, vertex-colour baking, and FBX export conventions for **R1 ("stylized realism")** hard-surface assets in *MoonProject* (`0.5.7`). Every model exported into `Assets/_Project/Art/R1/Models/` is validated on import by `R1ModelPostprocessor` and wrapped into a generated prefab by `R1ModelBuilder`.

---

## 1. Units & Coordinate Axes

- **Scene Units:** Metric (`Unit Scale = 1.0`, `1 Blender unit = 1 metre` in Unity).
- **Apply Transforms:** Always apply object **Scale** (`Ctrl+A -> Scale`, `1, 1, 1`) before adding bevel modifiers, baking vertex colours, or exporting.
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

## 4. Fixed Blender Material Slot Names & Generated Assets

Every mesh primitive exported under `Assets/_Project/Art/R1/Models/` must assign **only** material slots from the 12 fixed names below (`R1MaterialRecipes` in `Assets/_Project/Scripts/Art/Editor/R1MaterialRecipes.cs`):

- **Generated Materials (`[MoonBuilder("Art/R1Materials", 105)]`):** `R1MaterialLibrary.BuildMaterials` generates all 12 `.mat` assets into `Assets/_Project/Generated/Art/R1/Materials/` using `GeneratedAssets.CreateOrReplace`. Nothing under `Generated/` is committed to git.
- **Import Validation (`R1ModelPostprocessor`):** Sets `ImportViaMaterialDescription` + `InPrefab`, validates each source material name in `OnPreprocessMaterialDescription` (logging `Debug.LogError` with the model path and slot name on unknown names), and validates vertex colours in `OnPostprocessModel`.
- **Generated Prefabs (`[MoonBuilder("Art/R1Models", 106)]`):** `R1ModelBuilder.Build` wraps each FBX under `Assets/_Project/Art/R1/Models/` into `Assets/_Project/Generated/Art/R1/Models/<same relative path>.prefab` using `GeneratedAssets.SavePrefab`, replacing every renderer's material with the generated library material matching its source slot name.

| Blender Slot Name | Generated Material Asset | One-Line Intended Use |
| :--- | :--- | :--- |
| `R1_CreamEnamel` | `R1_CreamEnamel.mat` | Primary warm-cream baked enamel hull plating for rover shells, habitat modules, and companion bodies. |
| `R1_OrangeStripe` | `R1_OrangeStripe.mat` | Warm, slightly dusty orange accent stripes, grab handles, hatch rings, and safety bands. |
| `R1_GreyMetal` | `R1_GreyMetal.mat` | Secondary cool-grey painted structural frames, chassis ribs, mounting brackets, and recessed bays. |
| `R1_BareSteel` | `R1_BareSteel.mat` | Unpainted machined steel for actuator rods, latches, fasteners, wheel hubs, and exposed mechanisms. |
| `R1_Rubber` | `R1_Rubber.mat` | Matte dark elastomer for dust bellows, articulated joint boots, pressure seals, tyres, and cable sheathing. |
| `R1_GoldFoil` | `R1_GoldFoil.mat` | Fine-crinkled golden Kapton multi-layer thermal insulation blankets tucked into instrument cavities and lander undersides. |
| `R1_DarkGlass` | `R1_DarkGlass.mat` | Glossy dark-tinted optical glass for camera domes, sensor windows, and unlit instrument bezels. |
| `R1_Copper` | `R1_Copper.mat` | Brownish-pink metallic copper for antenna coils, heat-pipe fittings, grounding straps, and relay terminals. |
| `R1_SagePatch` | `R1_SagePatch.mat` | Muted sage-green field-repair patches, stowage covers, and secondary maintenance access panels. |
| `R1_LampAmber` | `R1_LampAmber.mat` | Smooth glowing warm amber indicator lamps, porch floodlight lenses, and beacon emitters with a small hot core. |
| `R1_SolarCell` | `R1_SolarCell.mat` | Deep blue-violet photovoltaic glass cells with fine silver busbars and collector grid for solar arrays and Tilly's solar wing. |
| `R1_Screen` | `R1_ScreenPlaceholder.mat` | Plain dark-glass placeholder assigned to the `R1_Screen` slot and reserved for the CRT face shader slice. |

---

## 5. Texel Density, Seamless Surface Detail & Trim-Sheet Rules

- **Target Texel Density:** `256 px/m` (`2.56 px/cm`) for hero rover/companion/habitat surfaces using `256x256` per-slot detail maps or the `512x512` shared trim sheet (`T_R1_TrimSheet_*`).
- **Seamless Per-Material Textures (`T_R1_<Surface>_*`):**
  - Library materials are **seamless, tileable surface detail only** (grain, fine scratches, slight mottling, fine foil crinkles, or tileable solar cell grid for `R1_SolarCell`).
  - Never bake rectangular panel frames, seam bands, corner bolts, ribs, or bullseye lens rings into per-material textures; panel seams, bolts, and vents belong on the trim sheet (`T_R1_TrimSheet_*`) or in geometry.
- **UV Channel Contract:**
  - `UV0` (`TEXCOORD0`): Primary material UVs. Each slot owns a seamless `[0, 1]` tileable texture set (`T_R1_<Surface>_Albedo/Normal/Mask`). Keep UV islands aligned horizontally (`U` along panel length, `V` along panel height) so brushed metal grain and solar cell busbars orient consistently.
  - `UV1` (`TEXCOORD1`): Optional static lightmap UV (`Pack Islands` with `Margin >= 0.02`).
- **Regenerating Library Textures:**
  - Run `python tools/art/r1/generate_r1_textures.py` to deterministically regenerate all 38 PNG maps in `Assets/_Project/Art/R1/Textures/`.

---

## 6. Per-Model Vertex Colours (`COLOR`) & `_MaskMap` Packing

### Per-Model Vertex Colour Attribute (`COLOR`)
Because 20+ models share the 12 library materials, **ambient occlusion, convex edge wear, and crease/cavity dust come from each mesh's baked vertex colours**, not from shared 2D textures:

| Vertex Colour Channel | Content | Neutral / Default | Shader Usage (`R1LitCore.hlsl`) |
| :--- | :--- | :--- | :--- |
| **R** | **Ambient Occlusion** | `1.0` = open, `0.0` = occluded | Attenuates indirect lighting via `_OcclusionStrength` and reinforces crevice dust (`1.0 - R`). |
| **G** | **Edge Wear (Convex Curvature)** | `0.0` = flat/concave, `1.0` = sharp convex bevel | Drives procedural `_EdgeWearColor`, `_EdgeWearMetallic`, and `_EdgeWearSmoothness` along bevelled edges, broken up by `_MaskMap.g` micro-scratches. |
| **B** | **Crease / Cavity Dust** | `0.0` = open, `1.0` = deep crease/cavity | Combines with upward normal (`normalWS.y`) and `1.0 - R` to settle lunar regolith (`_DustColor`) in creases and upward-facing pockets. |
| **A** | **Unused** | `1.0` | Reserved (`1.0`). |

### Baking the Vertex Colour Attribute in Blender
1. Create a Byte Color / Face Corner Color Attribute named `Col` on the mesh (`Data -> Color Attributes -> +`).
2. **Bake Ambient Occlusion (`R`):** In Cycles (`Render Engine = Cycles`, `Bake Type = Ambient Occlusion`, `Target = Active Color Attribute`), bake AO (`distance = 0.25 m`) into a temporary attribute `AO`.
3. **Bake Convex & Concave Curvature (`G` and `B`):** Using `Geometry -> Pointiness` (or `Dirty Vertex Colors` / Geometry Nodes edge-angle curvature):
   - Map convex ridges (`Pointiness > 0.52` or bevel edges) to `0.0 .. 1.0` for **G** (`0.0` on flat faces so flat panels stay clean, `1.0` on bevelled edges).
   - Map concave creases (`Pointiness < 0.48`) to `0.0 .. 1.0` for **B** (`0.0` on open faces, `1.0` in recessed seams and cavities).
4. **Combine into `Col`:** Combine `(R = AO, G = ConvexWear, B = ConcaveCavity, A = 1.0)` into the active `Col` attribute before exporting FBX. `R1ModelPostprocessor` verifies `mesh.colors32.Length > 0` on every imported mesh and logs `Debug.LogError` if vertex colours are missing.

### Shared Tileable Mask Map (`_MaskMap`, Linear RGBA)
| Channel | Content | Shader Usage |
| :--- | :--- | :--- |
| **R** | **Metallic** (`0..1`) | Scaled by `_MetallicScale`; overridden toward `_EdgeWearMetallic` on worn edges and `0.0` under dust. |
| **G** | **Micro Wear Breakup** (`0..1`, neutral `0.5`) | Tileable micro-scratch/grain variation that breaks up vertex-colour `G` edge wear along bevels. |
| **B** | **Micro Cavity Breakup** (`0..1`, neutral `0.5`) | Tileable micro-mottle variation that breaks up vertex-colour `B` dust in cavities. |
| **A** | **Smoothness** (`0..1`) | Remapped into `[_SmoothnessMin, _SmoothnessMax]`; modified by edge wear and lunar dust (`_DustSmoothness`). |

---

## 7. Bloom-Safe Emission Contract

- The project's URP Volume bloom threshold is `Palette.BloomThreshold = 1.25`.
- `MoonProject/R1/Lit` keeps the broad body of emissive surfaces (`R1_LampAmber`) clamped at `_BloomSafeMax` (`1.06`) so indicator housings read as smooth glowing lenses without washing out adjacent bevels, while pixels whose emission map luminance exceeds `_HotCoreThreshold` (`0.85`) ramp up to `_HotCorePeak` (`1.65`) to trigger a tight, soft HDR halo.
- Runtime scripts modulate glow intensity using the project's existing linear `MaterialPropertyBlock` contract (`block.SetVector("_EmissionColor", new Vector4(r, g, b, 1f))`).

---

## 8. Blender FBX Export Settings & Unity Import Rules

When exporting from Blender (`File -> Export -> FBX (.fbx)`) into `Assets/_Project/Art/R1/Models/`:

- **Include:** `Selected Objects`, `Object Types = Empty, Mesh` (no Cameras, Lamps, or Armatures unless rigged).
- **Transform:**
  - `Scale = 1.00`, `Apply Scalings = All Local`, `Forward = -Z Forward`, `Up = Y Up`, `Apply Unit = On`, `Use Space Transform = On`, `Apply Transform = On`
- **Geometry:**
  - `Smoothing = Normals Only`
  - `Apply Modifiers = On`
  - `Vertex Colors = sRGB` (ensures the active `Col` vertex colour attribute is written to the FBX)
  - `Tangent Space = Off` (Unity computes MikkTSpace tangents on import)

### Enforced by `R1ModelPostprocessor` & `R1ModelBuilder`
- `globalScale = 1f`, `useFileUnits = true`, `bakeAxisConversion = true`, `isReadable = false`
- `importNormals = ModelImporterNormals.Import`, `importTangents = ModelImporterTangents.CalculateMikk`
- `materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription`, `materialLocation = ModelImporterMaterialLocation.InPrefab`
- `importAnimation = false`, `importCameras = false`, `importLights = false`
- Validates all source material slot names against `R1MaterialRecipes` in `OnPreprocessMaterialDescription` and validates `mesh.colors32.Length > 0` in `OnPostprocessModel`.
- `R1ModelBuilder` (`Art/R1Models`, order 106) wraps each FBX into `Assets/_Project/Generated/Art/R1/Models/<same relative path>.prefab` with generated library materials assigned.
