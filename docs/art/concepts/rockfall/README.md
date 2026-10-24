# Peak Road Fresh Rockfall Kit (`box/agy-rockfall`)

![Peak Road Fresh Rockfall Concept & Verification Sheet](sheet.png)

## 1. Problem & Visual Solution

From the base apron (`~310 m` south-southwest of `site.peakroad`), the existing weathered boulders (`Rock_10`–`Rock_13`, `2.35–4.85 m` tall, shaded with dark weathered `R1_Basalt` `#5C5E66`) subtend less than `~1.5°` vertically and blend into the dark mountain shadow rather than reading at minute 1 as *"the Peak road is buried by a landslide."*

This kit adds five R1 rockfall assets under `Assets/_Project/Art/R1/Models/Rocks/` (generated prefabs at `Assets/_Project/Generated/Art/R1/Models/Rocks/<Id>.prefab`, directly loadable by `WorldPaths.R1RockPrefab(id)`):

1. **Four 10–16 m fresh-broken lunar rockfall boulders (`Rockfall_Boulder_00`..`Rockfall_Boulder_03`)**:
   - Multi-lobe, soft-edged R1 breccia/anorthosite-basalt masses with broad stepped conchoidal fracture planes, soft-bevelled shear rims, flared basal talus skirts, buried roots (`min.y < 0`), and **zero** round hole/pit clusters.
   - Shaded with **`R1_FreshBasalt.mat`** (`Assets/_Project/Generated/Art/R1/Materials/R1_FreshBasalt.mat`): higher fresh-break lunar albedo (`_BaseColor = (1.56, 1.58, 1.62)` over `T_R1_Basalt_Albedo`, pale cleaved facet wear `_EdgeWearColor = (0.76, 0.77, 0.80)`, and `DustLight`-matched basal skirt `_DustColor = (0.66, 0.67, 0.69)`), while remaining strictly non-emissive (`_EmissionStrength = 0`) and below `Palette.LitCeiling`.
2. **One 46 m wide scree/talus landslide tongue (`Rockfall_TalusSlab`)**:
   - A `46.0 m × 30.0 m × 4.5 m` low fan of broken rubble with a thicker uphill throat (`+Z`), fanned multi-lobe avalanche toe (`-Z`), 24 soft-shouldered hummocks plus 8 embedded fractured rubble slabs, and a feathered perimeter skirt (`min.y = -0.90 m`) whose vertex colors (`R -> 0.92`, `B -> 0.96`) eliminate any dark AO ring at the boundary so its edges blend seamlessly into World's `48 m` wide `RockfallScar` (`PaletteSwatch.DustLight` `#A6A7AB`).
   - Shaded with **`R1_RockfallTalus.mat`** (`Assets/_Project/Generated/Art/R1/Materials/R1_RockfallTalus.mat`).

---

## 2. Asset & ScaleContract Summary

| Prefab ID (`WorldPaths.R1RockPrefab`) | Source FBX | ScaleContract JSON | Size `[X, Z, Y]` (m) | Grounding `min.y` (m) | Triangles | Material |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `Rockfall_Boulder_00` | `Rocks/Rockfall_Boulder_00.fbx` | `rockfall_boulder_00.json` | `10.50 × 8.40 × 6.40` | `-1.024` | `776` | `R1_FreshBasalt.mat` |
| `Rockfall_Boulder_01` | `Rocks/Rockfall_Boulder_01.fbx` | `rockfall_boulder_01.json` | `12.20 × 9.80 × 7.60` | `-1.216` | `776` | `R1_FreshBasalt.mat` |
| `Rockfall_Boulder_02` | `Rocks/Rockfall_Boulder_02.fbx` | `rockfall_boulder_02.json` | `14.00 × 11.20 × 8.80` | `-1.408` | `776` | `R1_FreshBasalt.mat` |
| `Rockfall_Boulder_03` | `Rocks/Rockfall_Boulder_03.fbx` | `rockfall_boulder_03.json` | `15.80 × 12.60 × 10.20` | `-1.632` | `864` | `R1_FreshBasalt.mat` |
| `Rockfall_TalusSlab` | `Rocks/Rockfall_TalusSlab.fbx` | `rockfall_talus_slab.json` | `46.00 × 30.00 × 4.50` | `-0.900` | `2,368` | `R1_RockfallTalus.mat` |

---

## 3. Proposed Placement for World (`LandmarkSettings._rockfall`)

All five prefabs live in `Assets/_Project/Generated/Art/R1/Models/Rocks/<Id>.prefab`, so `OpeningLayoutPlacer.StandStoryHosts()` (`StandRock` -> `WorldPaths.R1RockPrefab(rock.Id)`) can instantiate them directly without any path-table changes.

Every offset below has `Offset.y > 0` along the Peak road direction (`Vector2.Dot(rock.Offset, road) > 0`), satisfying `StoryHostTests.TheRockfall_LiesAhead_Of01_UpThePeakRoad()`, while keeping `Elder01` at `(0, 0)` completely clear and visible from the drive up (`StoryHostTests.EveryHost_StandsOnLevelGround_InSightFromTheDriveUp()`):

| Order | Prefab `Id` | `Anchor` | `Offset (x, z)` (m) | `Yaw` (deg) | `Sink` (m) | Placement Role on the Peak Road |
| :---: | :--- | :--- | :---: | :---: | :---: | :--- |
| 1 | `Rockfall_TalusSlab` | `WorldAnchors.PeakRoad` | `(0.0, 16.5)` | `6` | `0.35` | `46 × 30 m` landslide rubble tongue spanning the road (`z = +1.5..+31.5 m`) behind `Elder01`, blending into the `48 m` wide `RockfallScar` fan |
| 2 | `Rockfall_Boulder_00` | `WorldAnchors.PeakRoad` | `(-2.4, 10.8)` | `-18` | `0.55` | `10.5 m` fresh-cleaved boulder tumbled near the toe of the slide, `10.8 m` ahead of `Elder01` |
| 3 | `Rockfall_Boulder_01` | `WorldAnchors.PeakRoad` | `(9.6, 14.5)` | `48` | `0.65` | `12.2 m` stepped shear-face boulder walling off the right road shoulder |
| 4 | `Rockfall_Boulder_02` | `WorldAnchors.PeakRoad` | `(-8.2, 15.8)` | `-32` | `0.75` | `14.0 m` multi-lobe avalanche block locking the left road lane |
| 5 | `Rockfall_Boulder_03` | `WorldAnchors.PeakRoad` | `(1.2, 18.5)` | `24` | `0.85` | `15.8 m` (`10.2 m` tall) central anchor megalith crowning the road barrier (`~3.1°` vertical silhouette from 310 m) |
| 6 | `Rock_13` | `WorldAnchors.PeakRoad` | `(-14.5, 19.5)` | `40` | `0.35` | Supporting outer-left flank boulder on the talus margin |
| 7 | `Rock_12` | `WorldAnchors.PeakRoad` | `(15.2, 20.2)` | `-35` | `0.35` | Supporting outer-right flank boulder on the talus margin |
| 8 | `Rock_11` | `WorldAnchors.PeakRoad` | `(-4.8, 25.0)` | `110` | `0.35` | Upper-throat rubble step leading into the scar fan |
| 9 | `Rock_10` | `WorldAnchors.PeakRoad` | `(6.5, 26.2)` | `-75` | `0.30` | Upper-throat rubble step on the right scar chute |

### Drop-in `LandmarkSettings._rockfall` Snippet for World

```csharp
[SerializeField] private PropPlacement[] _rockfall =
{
    new(WorldAnchors.PeakRoad, "Rockfall_TalusSlab", new Vector2(0.0f, 16.5f), 6f, 0.35f),
    new(WorldAnchors.PeakRoad, "Rockfall_Boulder_00", new Vector2(-2.4f, 10.8f), -18f, 0.55f),
    new(WorldAnchors.PeakRoad, "Rockfall_Boulder_01", new Vector2(9.6f, 14.5f), 48f, 0.65f),
    new(WorldAnchors.PeakRoad, "Rockfall_Boulder_02", new Vector2(-8.2f, 15.8f), -32f, 0.75f),
    new(WorldAnchors.PeakRoad, "Rockfall_Boulder_03", new Vector2(1.2f, 18.5f), 24f, 0.85f),
    new(WorldAnchors.PeakRoad, "Rock_13", new Vector2(-14.5f, 19.5f), 40f, 0.35f),
    new(WorldAnchors.PeakRoad, "Rock_12", new Vector2(15.2f, 20.2f), -35f, 0.35f),
    new(WorldAnchors.PeakRoad, "Rock_11", new Vector2(-4.8f, 25.0f), 110f, 0.35f),
    new(WorldAnchors.PeakRoad, "Rock_10", new Vector2(6.5f, 26.2f), -75f, 0.30f),
};
```

---

## 4. Sheet Panels (`sheet.png`)

- **Top-Left (`rockfall_310m_day_c.png`):** Captured from `~310 m` at the base apron's height (`z = -310 m, y = 2.4 m`) under **Day C low sun** (`13°` grazing elevation). The pale `46 m` wide `Rockfall_TalusSlab` tongue and `10.5–15.8 m` lighter-toned fresh boulders (`Rockfall_Boulder_00`..`03`) form a clear, sunlit barrier across the Peak road at the foot of the pale scar fan.
- **Top-Right (`rockfall_310m_earthlight_night.png`):** Same `~310 m` base-apron camera under **Earthlight night** illumination. The higher fresh-break albedo of `R1_FreshBasalt` and `R1_RockfallTalus` separates the landslide heap and blocked road from the dark weathered mountain flank.
- **Bottom-Left (`rockfall_approach_85m_day_c.png`):** `~85 m` approach view along the Peak road showing `Elder01` ("The Surveyor") stopped at `(0, 0)` right at the toe of `Rockfall_TalusSlab` with `Rockfall_Boulder_00`..`03` walling off the ascent.
- **Bottom-Right (`rockfall_slope_closeup_day_c.png`):** Close `3/4` slope inspection with `Elder01` and `Rover 07` at the toe for 1:1 scale reference, showing the soft R1 multi-lobe forms, stepped conchoidal cleavage planes, and feathered terrain-blending perimeter of `Rockfall_TalusSlab`.
