# R1 Collision Proxies (`UCX_<Part>_<n>`)

Authored, low-poly convex hull collision proxies embedded directly inside the R1 Lumen Station ruin FBXs under
`Assets/_Project/Art/R1/Models/Ruins/` so `World/Editor/RuinColliderBuilder` can build exact convex colliders without
mesh-slicing heuristics or per-model special cases.

## Naming & Hierarchy Contract

- **Child node name:** `UCX_<Part>_<n>` (Unreal-style convex collision convention), where `<Part>` matches the sibling
  render mesh node it covers (e.g. `UCX_ModuleA_Main_1`, `UCX_QuonsetShell_1..6`, `UCX_ToppledGearbox_1`) and `<n>` is a
  1-based index (`1, 2, ...`).
- **Parentage & pivot:** Each `UCX_<Part>_<n>` mesh is a direct child of the FBX root (`[0, 0, 0]` world pivot, unit
  scale `[1, 1, 1]`, no unapplied object rotation).
- **Material & vertex colours:** Assigned `R1_GreyMetal` and neutral vertex colour `(1, 0, 0, 1)` so `model_lint.py`
  and `R1ModelPostprocessor` pass without exempting collision meshes.
- **Invisible at runtime:** `R1ModelPostprocessor.DisableCollisionProxyRenderers` and
  `R1ModelBuilder.AssignLibraryMaterials` set `Renderer.enabled = false`, `shadowCastingMode = Off`, and
  `receiveShadows = false` on every `UCX_*` child while keeping its `MeshFilter` intact for collider generation.
- **Untouched AO baking:** `ruins_common.bake_ruins_vertex_colors` detaches and hides all `UCX_*` children before
  Cycles ambient-occlusion baking so the render meshes' vertex colour streams are identical to a build without proxies.

## Geometry Rules

1. **Strictly convex hull:** Built via `bmesh.ops.convex_hull` in `ruins_common.add_convex_proxy` and verified to have
   no concave edges (`calc_face_angle_signed >= -1e-3`), manifold closed topology, and outward normals.
2. **Face & triangle budget:** Each `UCX_<Part>_<n>` mesh has **<= 32 faces** (and **<= 32 triangles**), well inside
   Unity `MeshCollider.convex` and PhysX hull limits.
3. **Grounded / supported:** Nothing floats in mid-air. Every proxy either rests on the ground (`y >= 0.01 m`) or sits
   directly on top of supporting structural wall/mound proxies (such as `UCX_QuonsetShell_6`'s roof arch resting on
   `UCX_QuonsetShell_1..5`, or `UCX_TowerBase_2` and `UCX_Dish_1` resting on the `UCX_TowerBase_1` basalt mound).
4. **Within render bounds:** Every `UCX_*` hull sits strictly inside the ruin's outer render-mesh AABB so prefab scale
   and bounds contracts remain unchanged.

## Solid vs. Open Path Rules (Rover 07: `1.47 m W × 2.01 m L × 1.59 m H`)

| Ruin (`Assets/_Project/Art/R1/Models/Ruins/`) | `UCX_<Part>_<n>` Proxies | Solid Parts | Open / Unblocked Paths & Scan Faces |
|---|---|---|---|
| `FallenSolarArray.fbx` | `UCX_ToppledGearbox_1` (6f/12t)<br>`UCX_SolarWingLeft_1` (7f/16t)<br>`UCX_SolarWingRight_1` (7f/16t)<br>`UCX_CableStub_1` (6f/12t) | Central toppled drive gearbox, tilted left & right solar wing frames, and severed rear cable stub. | Open ground around the V-shaped wings; `Scan_1` (`ToppledGearbox`) and `Scan_2` (`SolarWingRight`) sit clear on the exterior faces. |
| `HabitatModules.fbx` | `UCX_ModuleA_Main_1` (8f/20t)<br>`UCX_BellowsJoints_1` (6f/12t)<br>`UCX_ModuleB_Lab_1` (8f/20t)<br>`UCX_BellowsJoints_2` (6f/12t)<br>`UCX_ModuleC_Crushed_1` (7f/16t) | All 3 cylindrical habitat modules (with lower saddle cradles down to ground) and the 2 corrugated bellows connectors. | `UCX_ModuleA_Main_1` stops at `z = -1.28 m` so `AirlockPanel`, `AirlockReadout`, the keypad, and `Scan_1` (`z = -1.36 .. -1.44 m`) are completely unblocked for 07's approach and scan beam. |
| `KenjiWorkshop.fbx` | `UCX_QuonsetShell_1..6` (6 × 6f/12t)<br>`UCX_WorkbenchAndRacks_1..2` (2 × 6f/12t) | Left/right lower curved walls, rear bulkhead, front left/right portal jambs, upper roof arch above `y = 1.98 m`, interior left workbench, and rear-right storage cabinet. | Front rectangular doorway (`3.24 m W × 1.98 m H`) and interior floor slab are completely open so 07 (`1.47 W × 1.59 H`) can drive inside; overhead `HoistRail` and `HangingLamp` (`y >= 1.75 m`) have no low-hanging collider blocking the portal. |
| `RadioTower.fbx` | `UCX_TowerBase_1` (10f/28t)<br>`UCX_TowerBase_2` (6f/12t)<br>`UCX_MastLeaning_1` (6f/12t)<br>`UCX_Dish_1` (10f/28t) | Rocky basalt mound, steel foundation plinth + snapped lower stub, leaning 8-bay lattice mast spine, and fallen parabolic dish + pedestal. | Open ground around the mound; `Scan_1` (dish feed horn tip) and `Scan_2` (front of snapped mast base at `z = -0.22 m`) sit outside the convex hulls. |
| `ScatterSet.fbx` | `UCX_SealedCrates_1` (6f/12t)<br>`UCX_CableReels_1` (6f/12t)<br>`UCX_ToppledAntenna_1` (6f/12t)<br>`UCX_InstrumentCase_1` (6f/12t)<br>`UCX_OxygenBottleRack_1` (6f/12t) | Each of the 5 individual field props (`SealedCrates`, `CableReels`, `ToppledAntenna`, `InstrumentCase`, `OxygenBottleRack`). | All ground gaps between the 5 separated field props remain open so 07 never hits an invisible bounding box bridging across the cluster. |
