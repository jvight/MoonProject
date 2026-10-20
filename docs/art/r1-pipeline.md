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

Every mesh primitive exported under `Assets/_Project/Art/R1/Models/` must assign **only** material slots from the 20 fixed names below (`R1MaterialRecipes` in `Assets/_Project/Scripts/Art/Editor/R1MaterialRecipes.cs`):

- **Generated Materials (`[MoonBuilder("Art/R1Materials", 105)]`):** `R1MaterialLibrary.BuildMaterials` generates all 15 `.mat` assets into `Assets/_Project/Generated/Art/R1/Materials/` using `GeneratedAssets.CreateOrReplace`. Nothing under `Generated/` is committed to git.
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
| `R1_Screen` | `R1_CrtFace.mat` | Expressive warm-amber CRT face and status screen material driven by `MoonProject/R1/CrtFace`. |
| `R1_Brass` | `R1_Brass.mat` | Warm golden machined brass for lamp bezels, chest lens ring, cheek lens, and fittings. |
| `R1_Basalt` | `R1_Basalt.mat` | Fractured lunar basalt stone for boulders, rock slabs, pebbles, and canyon gate monoliths. |
| `R1_Signage` | `R1_Signage.mat` | 1980s stencil signage atlas (`LUMEN`, `SEC-04`, `HABITAT & LAB`, `WORKSHOP`, `EVAC 1988`, sealed airlock status panel `SEALED · 1988 · STANDBY` / `SUIT 01-04 · OFFLINE` with amber emission mask, programme logo mark, hazard stripes, and 4 pictograms). |
| `R1_WhitePaint` | `R1_WhitePaint.mat` | Chalky cool-white low-gloss thermal control paint for 1960s-70s landers, experiment boxes, flat white Kapton ribbon, utility trusses, and Kestrel-3. |
| `R1_TealMark` | `R1_TealMark.mat` | Muted teal painted markings and trim for Meridian and Kestrel-3 hardware. |
| `R1_SilverFoil` | `R1_SilverFoil.mat` | Crinkled aluminised Mylar/Teflon thermal blanket and reflector film for 1960s-70s probes and sortie packages. |
| `R1_RackOutline` | `R1_RackOutline.mat` | The socket rack's lit silhouette line: an even warm glow (no hot core) Gameplay sets per bay through `_EmissionColor`. |
| `R1_RackFill` | `R1_RackFill.mat` | The socket rack's filled silhouette behind the line: a softer warm glow Gameplay sets per bay through `_EmissionColor`. |

---

## 5. Texel Density, Seamless Surface Detail & Trim-Sheet Rules

- **Target Texel Density:** `256 px/m` (`2.56 px/cm`) for hero rover/companion/habitat surfaces using `256x256` per-slot detail maps or the `512x512` shared trim sheet (`T_R1_TrimSheet_*`).
- **Seamless Per-Material Textures (`T_R1_<Surface>_*`):**
  - Library materials are **seamless, tileable surface detail only** (grain, fine scratches, slight mottling, fine foil crinkles, or tileable solar cell grid for `R1_SolarCell`), except `R1_Signage` which is a 4-row UV signage atlas (`512x512`).
  - Never bake rectangular panel frames, seam bands, corner bolts, ribs, or bullseye lens rings into per-material textures; panel seams, bolts, and vents belong on the trim sheet (`T_R1_TrimSheet_*`) or in geometry.
- **Signage Atlas Cells (`R1_Signage` / `T_R1_Signage_*`, `512x512`, `V = 0` at bottom):**
  - `Row 3 (V = 0.75..1.00)`: `LOGO` (`U = 0.00..0.25`, circular Lumen crescent mark), `LUMEN` (`U = 0.25..0.75`, stencil `LUMEN`), `EVAC_1988` (`U = 0.75..1.00`, stencil `EVAC 1988`).
  - `Row 2 (V = 0.50..0.75)`: `SEC04_HABITAT` (`U = 0.00..0.50`, stacked `SEC-04 / HABITAT & LAB`), `AIRLOCK_STATUS` (`U = 0.50..1.00`, dark CRT-style sealed airlock status panel reading `SEALED · 1988 · STANDBY` above a dimmer `SUIT 01-04 · OFFLINE` line in amber stencil/segment text, paired with `T_R1_Signage_Emission.png` so `HabitatModules/AirlockPanel` glows warm amber without ever using `R1_Screen` / `R1_CrtFace`). (Sub-regions `SEC_04` and `HABITAT_LAB` alias into `SEC04_HABITAT`.)
  - `Row 1 (V = 0.25..0.50)`: `WORKSHOP` (`U = 0.00..0.50`, stencil `WORKSHOP`), `HAZARD` (`U = 0.50..1.00`, 45° diagonal amber/charcoal hazard stripes).
  - `Row 0 (V = 0.00..0.25)`: 4 cassette-futurism pictogram tiles on warm orange band: `PICT_ROVER` (`U = 0.00..0.25`), `PICT_RADIO` (`U = 0.25..0.50`), `PICT_BOTANY` (`U = 0.50..0.75`), `PICT_AIRLOCK` (`U = 0.75..1.00`).
- **UV Channel Contract:**
  - `UV0` (`TEXCOORD0`): Primary material UVs. Each slot owns a seamless `[0, 1]` tileable texture set (`T_R1_<Surface>_Albedo/Normal/Mask`), or maps signage quads directly into `T_R1_Signage_*` cells. Keep UV islands aligned horizontally (`U` along panel length, `V` along panel height) so brushed metal grain and solar cell busbars orient consistently.
  - `UV1` (`TEXCOORD1`): Optional static lightmap UV (`Pack Islands` with `Margin >= 0.02`).
- **Regenerating Library Textures:**
  - Run `python tools/art/r1/generate_r1_textures.py` to deterministically regenerate all 51 PNG maps in `Assets/_Project/Art/R1/Textures/`.

---

## 6. Per-Model Vertex Colours (`COLOR`) & `_MaskMap` Packing

### Per-Model Vertex Colour Attribute (`COLOR`)
Because 20+ models share the 15 library materials, **ambient occlusion, convex edge wear, and crease/cavity dust come from each mesh's baked vertex colours**, not from shared 2D textures:

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

---

## 9. Shared Blender Modelling Kit (`tools/art/blender/r1kit.py`)

All procedural Blender builders across the project — the 10 base structures, `CargoLander`, `Rock_00..09`, the 5 Lumen Station ruins, Rover 07 (`RoverModel`, `SolarWing`, `Kit_TapeDeck`), and all 10 rover upgrade kits (`Kit_LampBar` .. `ScanFan`) — share a single unified modelling kit at `tools/art/blender/r1kit.py`. Do not create duplicate mesh/UV/vertex-colour/export helpers in sub-packages.

### Core Capabilities & Entry Points
1. **Scene Reset & Deterministic FBX Hooks (`reset_scene`)**
   - `r1kit.reset_scene()` clears all Blender objects, meshes, materials, lights, cameras, and collections, sets metric units (`scale_length = 1.0`) and `30 fps`, and installs deterministic FBX timestamp/UUID hooks (`_install_deterministic_fbx_hooks`) so repeated builds produce byte-identical `.fbx` files.
2. **Coordinate Mapping (`u_pos`, `u_vec`)**
   - Converts Unity coordinates `(ux, uy, uz)` (`+X` right, `+Y` up, `+Z` forward) to Blender world coordinates `(ux, uz, uy)` (`+X` right, `+Y` forward, `+Z` up). Accepts either three floats or a 3-sequence.
3. **Scene Hierarchy & Node Creation (`create_empty`, `create_mesh_node`, `merge_child_meshes_into_host`, `prune_unused_materials`)**
   - `create_empty(name, parent=None, pivot_u=(0, 0, 0), *, local_space=False, empty_size=0.12)`: creates an Empty node in either world-space Unity coordinates (`local_space=False`, compensating for parent `location`) or parent-local Unity coordinates (`local_space=True`, used by Rover 07 and its kits).
   - `create_mesh_node(name, bm, unity_loc=(0, 0, 0), parent=None, mat_list=None, sharp_deg=42.0, cfg=None)`: converts a `BMesh` into a scene `MESH` object, ensures outward normals per shell, applies area-weighted loop normals, projects triplanar UVs, and initialises the `Col` byte colour layer.
   - `merge_child_meshes_into_host(host_obj, child_names, cfg)`: merges static child meshes into their animated parent mesh while preserving empty socket/locator transforms.
4. **BMesh Primitives & High-Level `MeshBuilder`**
   - **Direct `BMesh` primitives** (operating in Unity `(ux, uy, uz)` coordinates with automatic conversion into Blender `BMesh` space):
     - `append_beveled_box(bm, size_xyz, center_xyz, bevel_width, bevel_segments, mat_idx, rot_euler, guard_inset=True, *, in_place=False)`
     - `append_cylinder(bm, radius1, radius2, length, axis, center_xyz, segments, bevel_width, bevel_segments, mat_idx, rot_euler, *, cap_tris=True, in_place=False)`
     - `append_torus(bm, major_radius, minor_radius, axis, center_xyz, major_seg, minor_seg, mat_idx, rot_euler, *, in_place=False)`
     - `append_lathe_profile(bm, profile_rz, axis, center_xyz, segments, mat_idx, rot_euler, ring_mat_indices=None, auto_chamfer=True)`
     - `append_prism_extrusion(bm, poly_2d, depth, axis, center_xyz, bevel_width, bevel_segments, mat_idx, rot_euler)`
     - `append_spherical_dome(bm, radius, dome_height, axis, center_xyz, segments, rings, mat_idx, rot_euler, *, collar_depth=0.008, closed_bottom=False)`
     - `append_helical_coil(...)`, `append_hex_bolt(..., include_washer=True, closed_bottom=False, segments=6)`, `append_dome_rivet(...)`
   - **`MeshBuilder` class** (fluent builder used by structures, lander, rocks, and ruins):
     - Wraps `append_beveled_box(..., in_place=True)`, `append_cylinder(..., cap_tris=False, in_place=True)`, and `append_torus(..., in_place=True)` alongside domain helpers (`add_sphere`, `add_cone`, `add_lathe`, `add_extruded_polygon`, `add_tube_along_points`, `add_bolt_circle`, `add_rivet_row`, `add_signage_plate`) and finalises via `build_object(...)`.
5. **Normals, UVs, Vertex Colours & FBX Export**
   - `ensure_outward_normals(bm, *, per_shell=False)`: ensures outward-facing winding (`per_shell=True` inspects each disconnected manifold island independently; `per_shell=False` uses `bmesh.ops.recalc_face_normals`).
   - `apply_weighted_normals(target, sharp_angle_deg=42.0, area_power=1.6, *, use_modifier=False, weight=50)`: computes face-area-weighted custom split normals directly (`use_modifier=False`, default for rover/kits) or applies Blender's `WEIGHTED_NORMAL` modifier (`use_modifier=True`, used by structures/ruins/rocks/lander).
   - `apply_triplanar_uvs(target, texel_scale=0.85, *, center_lamp_amber=False, mat_list=None)`: applies box/triplanar UV projection onto `UVMap` (`center_lamp_amber=True` maps `R1_LampAmber` faces into the hot-core centre of the emission texture).
   - `bake_vertex_colors(root, footprint_radius=2.0, bevel_wear_width=0.022, *, rover_cfg=None, include_ground_occluder=True)`: bakes `Col` (`R=AO`, `G=EdgeWear`, `B=Cavity`, `A=1.0`) across a root hierarchy or mesh list (`rover_cfg=None` uses the analytical curvature + vertical AO formulation for structures/rocks/ruins; passing `rover_cfg=DIMENSIONS` uses BVHTree 64-ray hemisphere AO + BMesh dihedral curvature for Rover 07 and its kits).
   - `export_fbx(root, filepath, *, bake_space_transform=False)`: exports a root hierarchy or explicit object list to `.fbx` with `-Z Forward`, `Y Up`, and `bake_space_transform=True` for Rover 07 / kits or `False` for structures/ruins/rocks/lander.

### Master Build Entry Point (`tools/art/blender/build_all.py`)
Run all 39 R1 FBX models in a single headless Blender invocation:
```powershell
D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/build_all.py -- --skip-renders
```
Omit `--skip-renders` to also regenerate the diagnostic and showcase PNG sheets under `Logs/renders/`.

