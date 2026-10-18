# World box: handoff (2026-10-19, morning)

Basin dressing (`docs/design/basin-dressing.md`) is placed and checked in-engine through box/cl-basin-dress5
(0bbaff0f, landing). Read §7 of the plan first: it records every number that changed in-engine and why.

## State
- **Scout** (1967 lander) at (68.8, 81.0) on its stamped crater's wall: the first night's earthlit lure (B10).
  The last-light shadow lure was dropped: at last light the basin floor lies in the rim's shadow.
- **Crew traces**:
  - the flag at the front door (81.5, 188), in The Peak's golden-hour shade;
  - the retroreflector on the sunlit plain beside trail A at (52.8, 160.4). One prism-facet family, by its shading
    normals, mirrors the golden-hour Sun to the apron and blooms; dark in the eclipse;
  - the seismic station (128, 183); the fallen tripod on the blast ridge; the sample rack on the Scout crater's rim.
- **Trail A**: 3 m imprint tiles from the apron's edge, round the lander's footprint, into the shade to the flag.
- **Sealed crates** (Gameplay, done with the Director's OK):
  - the open lander crate gives its parts on a held E, so "collect the parts" shows;
  - every sealed crate passes CrateApproachSessions (clear, drivable approach; seal ≥ 5 m from scan targets);
  - SealedContainer.SealSide is the way in to a seal (an R1 crate's own forward points down).
- **Checks**:
  - World EditMode: BasinDressingTests;
  - World PlayMode capture: AtmosphereSession.BasinProps_AreCapturedInTheirLight (`--category AtmosphereSession`),
    writing Logs/world-captures/basin-*.png and logging each prop's brightest steady pixel.

## Next task: the geophone line (basin dressing §2)
Art's kit is on main: Geophone_Plug, Geophone_Ribbon_10m (Start/End anchors 10 m apart), Geophone_Sensor.
Lay it ~90 m from the seismic station at 121° toward 06, with 3 sensors evenly spaced, and no colliders on the ribbon
or the plug. Draft, untested (not yet compiled, built or captured):
- LandmarkSettings (and WorldSettings.asset):
  - `_geophoneBearing` 121, `[Range(0, 360)]`;
  - `_geophoneLength` 90, `[Range(10, 200)]`;
  - `_geophoneSensors` 3, `[Range(1, 10)]`.
- WorldPaths: `GeophonePlugPrefab`, `GeophoneRibbonPrefab`, `GeophoneSensorPrefab` (R1PropsFolder + name).
- OpeningLayoutPlacer:
  - constants `StationName = "Prop_SeismicStation"`, `StationBox = "CentralBox_Anchor"`, `PlugAnchor = "Plug_Anchor"`;
  - in `StandCrewTraces`, when the placement is the station, call
    `LayGeophoneLine(context, landmarks, surface, Node(prop, StationBox).position, <new "GeophoneLine" child>)`;
  - the method:

```csharp
private static void LayGeophoneLine(SceneBuildContext context, LandmarkSettings landmarks, MoonSurface surface,
    Vector3 station, Transform parent)
{
    Vector2 run = MoonSurface.BearingToDirection(landmarks.GeophoneBearing);
    var along = new Vector3(run.x, 0f, run.y);
    // The plug's head sits in the station's box; its lead runs back out along the line to the first ribbon.
    var plug = context.LoadAsset<GameObject>(WorldPaths.GeophonePlugPrefab);
    float head = FlatDistance(Node(plug, PlugAnchor).position, plug.transform.position);
    float lead = FlatDistance(Node(plug, TileStart).position, plug.transform.position);
    Vector3 plugAt = Ground(surface, station + along * head);
    context.InstantiatePrefab(plug, parent, plugAt,
        Quaternion.LookRotation(-along, Vector3.up) * plug.transform.localRotation).name = "Geophone_Plug";
    Vector3 start = Ground(surface, plugAt + along * lead);

    var ribbon = context.LoadAsset<GameObject>(WorldPaths.GeophoneRibbonPrefab);
    float length = Vector3.Distance(Node(ribbon, TileStart).position, Node(ribbon, TileEnd).position);
    int lengths = Mathf.Max(1, Mathf.RoundToInt(landmarks.GeophoneLength / length));
    for (int i = 0; i < lengths; i++)
    {
        Vector3 from = Ground(surface, start + along * (length * i));
        Vector3 to = Ground(surface, start + along * (length * (i + 1)));
        GameObject piece = context.InstantiatePrefab(ribbon, parent, (from + to) * 0.5f,
            Quaternion.LookRotation(to - from, Vector3.up) * ribbon.transform.localRotation);
        piece.name = "Geophone_Ribbon_" + i.ToString(CultureInfo.InvariantCulture);
    }

    float spacing = length * lengths / landmarks.GeophoneSensors;
    for (int i = 1; i <= landmarks.GeophoneSensors; i++)
    {
        StandTrace(context, parent, "Geophone_Sensor_" + i.ToString(CultureInfo.InvariantCulture),
            WorldPaths.GeophoneSensorPrefab, Ground(surface, start + along * (spacing * i)),
            landmarks.GeophoneBearing);
    }
}
```

To check when picking it up:
- Do the prefabs carry colliders? Remove none silently; ask Art if the ribbon or plug has one.
- Does a 10 m ribbon pitched end to end float or bury over the dunes? Capture it close up at golden hour.
- Does the plug head actually sit in the station box? Its local +Z is assumed to be the plug's length axis, as with
  the other R1 props.
- Is the end of the line clear of the canyon mouth's slopes?

Add a capture pose (golden hour, from the station along the line) and an EditMode check that the line ends within
drivable ground. Gate: `box_check world`.

## Open, not World's
- RuinsBasinCaptureSession "Scanning FallenSolarArray teaches SolarArray" fails on main since before dress3.
- BeamAccuracySessions flakes (first candidate is sometimes a ruin target): with Rover.
- The cut first-use chip shows "E Scan" while the key chip says "E Cut": with ui-fit.
