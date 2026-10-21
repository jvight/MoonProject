using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Stands opening v2's places in the scene at build time (docs/design/opening-v2.md §4), all from Art's R1 kits:
    /// the landed cargo lander with its service port and amber emergency beacon (and the <see cref="LanderBeacon"/>
    /// that pulses it and its cable's clip halos), 07's dead charging post at the wake with its old, torn cable to
    /// the port and the port's cable to the build apron (<see cref="GroundCableLayer"/>), the apron's markings, the
    /// dead west relay mast with its (hidden) signal pillar, Kestrel-3's debris, the big boulders near the routes and the rare big boulders on the floor
    /// (<see cref="BigBoulderPlanner"/>); and the basin's story props (docs/design/basin-dressing.md §2): the Scout
    /// on its crater's wall, the crew's traces (flag, retroreflector, seismic station, fallen tripod, sample rack) and
    /// bootprint trail A. Everything solid is fitted with colliders on <see cref="Layers.Prop"/>; the bootprints are
    /// pressed into the dust and never collide.
    /// </summary>
    public static class OpeningLayoutPlacer
    {
        /// <summary>[World]/Lander holds the landed lander, named <see cref="CargoLanderName"/>, and its fittings.</summary>
        public const string LanderRootName = "Lander";
        public const string CargoLanderName = "CargoLander";
        public const string ServicePortName = "ServicePort";
        public const string LanderBeaconName = "Beacon";

        /// <summary>[World]/OpeningProps holds the opening's other props.</summary>
        public const string OpeningPropsRootName = "OpeningProps";
        public const string WakePostName = "WakePost";
        public const string WestMastName = "WestMast";
        public const string ApronMarkingsName = "ApronMarkings";

        /// <summary>The west signal pillar, hidden until Gameplay shows it (opening v2 B11).</summary>
        public const string SignalPillarName = "SignalPillar";

        /// <summary>
        /// [World]/BasinProps holds the basin's story props, each named after its R1 prefab: Gameplay hosts their
        /// story scans by that name.
        /// </summary>
        public const string BasinPropsRootName = "BasinProps";
        public const string ScoutName = "Prop_OldLander";
        public const string ScoutBlastName = "Decal_OldLanderBlast";
        public const string BootprintTrailName = "BootprintTrail";

        /// <summary>Kestrel-3's story scan host in [World]/BasinProps; Art's hull panel under it is the node.</summary>
        public const string KestrelPanelHostName = "Kestrel3_HullPanel";

        // Art's R1 prop nodes this layout joins (tools/art/qa/contracts).
        private const string PostCableOrigin = "CableOrigin_Anchor";
        private const string PortWakeSocket = "CableSocket";
        private const string PortApronSocket = "Socket_Cable";
        private const string PortPilotLamp = "ReservePilotLamp";
        private const string BeaconLightAnchor = "Light_Anchor";
        private const string BeaconLens = "BeaconLens";
        private const string ApronCableChannel = "CableChannel_Anchor";
        private const string PillarBeam = "PillarBeam";

        // 06's open battery bay holds Art's three cell-pack fragments on its topper's sockets.
        private const string CellPackSocketPrefix = "Topper_Socket/SSR3_Topper_06/FragmentSocket_CellPack_";
        private const int CellPackCount = 3;

        // The Scout's parts the 1971 crew took home (basin dressing §4); its bare brackets stay.
        private static readonly string[] ScoutRemovedParts = { "TvCamera", "ScoopArm" };

        // Art's retroreflector: the panel (housing and prism array) turns on its hinge on the stand.
        private const string ReflectorName = "Prop_Retroreflector";
        private const string ReflectorHousing = "PanelHousing";
        private const string ReflectorPrisms = "PrismArray";

        // Facets facing within about 1 / this of one another (unit-vector steps, ~3 degrees) mirror as one: a corner
        // cube array repeats a few exact facings, and a smooth facet mirrors only within a degree or two of its own.
        private const float FacetGroups = 20f;
        private static readonly string[] ReflectorPanel = { "PanelHousing", "PrismArray" };

        // Art's bootprint tile: its trail runs from Start_Anchor to End_Anchor.
        private const string TileStart = "Start_Anchor";
        private const string TileEnd = "End_Anchor";

        // A walked path's sway: the tiles wander off the line on this phase step, so no two neighbours match.
        private const float WanderPhase = 1.7f;

        private const string WestMastAnchor = WorldAnchorIds.RelayPrefix + "1";

        // Art's dead mast prop: its beacon node, where the relay's lamp will stand (opening v3 R8).
        private const string RelayLampNode = "BeaconLamp";
        private const string RelayLampName = "RelayLamp";

        public static void Place(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            MoonSurface surface = settings.CreateSurface();
            var anchors = new WorldAnchors(surface, settings.Surface.Canyon, settings.Relays);
            LandmarkSettings landmarks = settings.Landmarks;
            Transform props = context.CreateChild(OpeningPropsRootName, root).transform;

            WorldAnchor wake = Require(anchors, WorldAnchorIds.PlaceWake);
            WorldAnchor apron = Require(anchors, WorldAnchorIds.PlaceApron);
            GameObject post = Stand(context, props, WakePostName, WorldPaths.DeadChargingPostPrefab, wake.Position,
                wake.Forward, true);
            GameObject markings = Stand(context, props, ApronMarkingsName, WorldPaths.BuildApronMarkingsPrefab,
                apron.Position, apron.Forward, false);
            WorldAnchor mast = Require(anchors, WestMastAnchor);
            GameObject westMast = Stand(context, props, WestMastName, WorldPaths.DeadAntennaMastPrefab, mast.Position,
                mast.Forward, true);
            StandRelayLamp(context, settings, props, westMast);
            Relay1SitePlacer.Stand(context, settings, root, surface, mast, westMast);
            Vector3 pillarSpot = mast.Position - mast.Forward * landmarks.SignalPillarSetback;
            GameObject pillar = Stand(context, props, SignalPillarName, WorldPaths.SignalPillarPrefab,
                Ground(surface, pillarSpot), mast.Forward, false);
            Vector3 width = pillar.transform.localScale;
            width.x *= landmarks.SignalPillarWidth;
            width.y *= landmarks.SignalPillarWidth;
            pillar.transform.localScale = width;
            var glow = new SerializedObject(pillar.AddComponent<SignalPillar>());
            glow.FindProperty("_settings").objectReferenceValue = settings;
            glow.FindProperty("_beam").objectReferenceValue = Node(pillar, PillarBeam).GetComponent<Renderer>();
            glow.ApplyModifiedPropertiesWithoutUndo();
            pillar.SetActive(false);

            Transform landerRoot = context.CreateChild(LanderRootName, root).transform;
            GameObject lander = LandLander(context, settings, surface, landerRoot,
                Require(anchors, WorldAnchorIds.PlaceLander), out Matrix4x4 upright);
            GameObject port = StandPort(context, settings, surface, landerRoot, lander, upright);
            GroundCableSettings cables = landmarks.WakeCable;
            var haloMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            GroundCableLayer.Wiring wiring = GroundCableLayer.LayWakeCable(context, props, surface, cables,
                Node(post, PostCableOrigin).position, Node(port, PortWakeSocket).position, haloMesh,
                context.LoadAsset<Material>(WorldPaths.CableMarkerHaloMaterial),
                context.LoadAsset<Material>(WorldPaths.CableTearSparkMaterial));
            Renderer[] apronClips = GroundCableLayer.LayApronCable(context, props, surface, cables,
                Node(markings, ApronCableChannel).position, Node(port, PortApronSocket).position, haloMesh,
                context.LoadAsset<Material>(WorldPaths.CableMarkerHaloMaterial));
            StandBeacon(context, settings, landerRoot, lander, upright, wiring, apronClips,
                Node(port, PortPilotLamp).GetComponent<Renderer>());

            StandAll(context, context.CreateChild("Debris", props).transform, anchors, surface, landmarks.Debris);
            Transform boulders = context.CreateChild("Boulders", props).transform;
            foreach (PropPlacement placement in landmarks.Boulders)
            {
                Vector3 near = Require(anchors, placement.Anchor).Position;
                var at = new Vector3(near.x + placement.Offset.x, 0f, near.z + placement.Offset.y);
                StandRock(context, boulders, placement.Id, Ground(surface, at) - Vector3.up * placement.Sink,
                    placement.Yaw);
            }

            StandBigBoulders(context, settings, surface, anchors, boulders);
            StandTrackBoulders(context, settings.Surface.Relief, surface, boulders);
            Transform basin = context.CreateChild(BasinPropsRootName, root).transform;
            StandScout(context, landmarks, surface, basin);
            StandCrewTraces(context, settings, surface, anchors, basin);
            StandStoryHosts(context, landmarks, surface, anchors, basin, boulders);
            Relay2SitePlacer.Stand(context, settings, root, surface, Require(anchors, WorldAnchorIds.RelayPrefix + 2),
                Require(anchors, WorldAnchorIds.Micro06));
            PressBootprints(context, landmarks, surface, context.CreateChild(BootprintTrailName, props).transform);
        }

        /// <summary>
        /// The crew's traces (basin dressing §2), each named after its prefab: the flag, the retroreflector (its panel
        /// aimed so the golden-hour Sun glints into the apron), the seismic station, the fallen tripod, and the sample
        /// rack on the Scout crater's north rim.
        /// </summary>
        private static void StandCrewTraces(SceneBuildContext context, WorldSettings settings, MoonSurface surface,
            WorldAnchors anchors, Transform parent)
        {
            LandmarkSettings landmarks = settings.Landmarks;
            foreach (PropPlacement placement in landmarks.CrewTraces)
            {
                Vector3 near = Require(anchors, placement.Anchor).Position;
                var at = new Vector3(near.x + placement.Offset.x, 0f, near.z + placement.Offset.y);
                GameObject prop = StandTrace(context, parent, placement.Id, WorldPaths.R1PropPrefab(placement.Id),
                    Ground(surface, at) - Vector3.up * placement.Sink, placement.Yaw);
                if (placement.Id == ReflectorName)
                {
                    Vector3 eye = Require(anchors, WorldAnchorIds.PlaceApron).Position
                                  + Vector3.up * landmarks.ReflectorEyeHeight;
                    AimReflector(prop, GoldenHourSun(settings), eye);
                }
            }

            Vector2 rack = surface.Shaping.ScoutCraterCenter + landmarks.SampleRackOffset;
            StandTrace(context, parent, "Prop_SampleRack", WorldPaths.SampleRackPrefab,
                Ground(surface, new Vector3(rack.x, 0f, rack.y)), landmarks.SampleRackYaw);
        }

        /// <summary>
        /// The databank's story scan hosts in [World]/BasinProps, each solid with one box on
        /// <see cref="Layers.Prop"/>: Kestrel-3's hull panel at the impact crater (its own host, the panel its scan
        /// node), 06 at micro.06, and 01 at the foot of the Peak road with the rockfall that stopped it (with the
        /// boulders).
        /// </summary>
        private static void StandStoryHosts(SceneBuildContext context, LandmarkSettings landmarks, MoonSurface surface,
            WorldAnchors anchors, Transform parent, Transform boulders)
        {
            PropPlacement panel = landmarks.KestrelPanel;
            Transform host = context.CreateChild(KestrelPanelHostName, parent).transform;
            host.position = Ground(surface, Near(anchors, panel));
            BlockOut(Stand(context, host, panel.Id, WorldPaths.KestrelDebrisPrefab(panel.Id),
                host.position - Vector3.up * panel.Sink, Quaternion.Euler(0f, panel.Yaw, 0f) * Vector3.forward, false));
            PropPlacement keeper = landmarks.Elder06;
            Vector3 rest = Ground(surface, Near(anchors, keeper)) - Vector3.up * keeper.Sink;
            Vector3 towardMast = Flat(Require(anchors, WorldAnchorIds.CanyonMouth).Position - rest);
            GameObject six = Stand(context, parent, keeper.Id, WorldPaths.ElderPrefab(keeper.Id), rest,
                Quaternion.Euler(0f, keeper.Yaw, 0f) * towardMast, false);
            SeatCellPacks(context, six);
            BlockOut(six);
            six.transform.rotation = Quaternion.AngleAxis(landmarks.Elder06Slump, six.transform.up)
                                     * six.transform.rotation;

            PropPlacement surveyor = landmarks.Elder01;
            BlockOut(Stand(context, parent, surveyor.Id, WorldPaths.ElderPrefab(surveyor.Id),
                Ground(surface, Near(anchors, surveyor)) - Vector3.up * surveyor.Sink,
                Quaternion.Euler(0f, surveyor.Yaw, 0f) * Vector3.forward, false));

            foreach (PropPlacement rock in landmarks.Rockfall)
            {
                StandRock(context, boulders, rock.Id, Ground(surface, Near(anchors, rock)) - Vector3.up * rock.Sink,
                    rock.Yaw);
            }
        }

        private static void SeatCellPacks(SceneBuildContext context, GameObject six)
        {
            for (int i = 0; i < CellPackCount; i++)
            {
                string path = CellPackSocketPrefix + i.ToString(CultureInfo.InvariantCulture);
                Transform socket = six.transform.Find(path);
                if (socket == null)
                {
                    throw new InvalidOperationException($"{six.name} has no '{path}' node (Art's R1 contract).");
                }

                var prefab = context.LoadAsset<GameObject>(WorldPaths.CellPackFragmentPrefab(i));
                GameObject cell = context.InstantiatePrefab(prefab, socket, socket.position, socket.rotation);
                cell.name = prefab.name;
            }
        }

        /// <summary>
        /// One box round all of <paramref name="prop"/>'s meshes, on <see cref="Layers.Prop"/>: it blocks 07 like the
        /// prop itself. A story host's meshes (the elders' wheels) are too fine for convex colliders, which Unity cuts
        /// down to a partial hull with a warning at load.
        /// </summary>
        private static void BlockOut(GameObject prop)
        {
            Bounds bounds = LocalBounds(prop.transform.localToWorldMatrix, prop);
            prop.layer = Layers.Prop;
            var box = prop.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        /// <summary>Where <paramref name="placement"/> stands: its offset from its anchor, on the XZ plane.</summary>
        private static Vector3 Near(WorldAnchors anchors, PropPlacement placement)
        {
            Vector3 near = Require(anchors, placement.Anchor).Position;
            return new Vector3(near.x + placement.Offset.x, 0f, near.z + placement.Offset.y);
        }

        /// <summary>
        /// A crew trace upright on the ground at <paramref name="yaw"/>: Art's own colliders where it has them (on
        /// <see cref="Layers.Prop"/>), else a convex collider on each mesh.
        /// </summary>
        private static GameObject StandTrace(SceneBuildContext context, Transform parent, string name,
            string prefabPath, Vector3 position, float yaw)
        {
            var prefab = context.LoadAsset<GameObject>(prefabPath);
            GameObject prop = context.InstantiatePrefab(prefab, parent, position,
                Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation);
            prop.name = name;
            Collider[] own = prop.GetComponentsInChildren<Collider>();
            if (own.Length == 0)
            {
                Solidify(prop);
                return prop;
            }

            prop.layer = Layers.Prop;
            foreach (Collider collider in own)
            {
                collider.gameObject.layer = Layers.Prop;
            }

            return prop;
        }

        /// <summary>
        /// Turns the retroreflector's panel on its hinge so one of its prism facets lies halfway between
        /// <paramref name="toSun"/> and the way to <paramref name="eye"/>: the Sun's mirror in that facet lands on the
        /// eye. The facet set chosen is the one the panel turns least to aim, of those on its front.
        /// </summary>
        private static void AimReflector(GameObject reflector, Vector3 toSun, Vector3 eye)
        {
            Transform hinge = Node(reflector, ReflectorHousing);
            Vector3 front = UpwardFace(hinge.GetComponent<MeshFilter>());
            Transform prisms = Node(reflector, ReflectorPrisms);
            Vector3 aim = (toSun.normalized + (eye - prisms.position).normalized).normalized;
            Vector3 facet = NearestFacet(prisms.GetComponent<MeshFilter>(), front, aim);
            Quaternion.FromToRotation(facet, aim).ToAngleAxis(out float angle, out Vector3 axis);
            foreach (string part in ReflectorPanel)
            {
                Node(reflector, part).RotateAround(hinge.position, axis, angle);
            }
        }

        /// <summary>
        /// Of a prism array's facets on its <paramref name="front"/> (world space), grouped by the way they face, the
        /// group's mean facing nearest <paramref name="aim"/>.
        /// </summary>
        private static Vector3 NearestFacet(MeshFilter filter, Vector3 front, Vector3 aim)
        {
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                throw new InvalidOperationException("The retroreflector's prism array has no mesh (Art's R1 contract).");
            }

            // The facets shade by their vertex normals, so they mirror the way those face; each counts by its area.
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            Transform node = filter.transform;
            var groups = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = node.TransformPoint(vertices[triangles[i]]);
                float area = Vector3.Cross(node.TransformPoint(vertices[triangles[i + 1]]) - a,
                    node.TransformPoint(vertices[triangles[i + 2]]) - a).magnitude;
                Vector3 facing = node.TransformDirection(normals[triangles[i]] + normals[triangles[i + 1]]
                                                         + normals[triangles[i + 2]]).normalized;
                Vector3 normal = facing * area;
                if (Vector3.Dot(facing, front) <= 0f)
                {
                    continue;
                }

                var key = Vector3Int.RoundToInt(facing * FacetGroups);
                groups[key] = groups.TryGetValue(key, out Vector3 sum) ? sum + normal : normal;
            }

            if (groups.Count == 0)
            {
                throw new InvalidOperationException("The retroreflector's prism array has no facet on its front.");
            }

            Vector3 best = front;
            float nearest = float.MaxValue;
            foreach (Vector3 sum in groups.Values)
            {
                float angle = Vector3.Angle(sum, aim);
                if (angle < nearest)
                {
                    nearest = angle;
                    best = sum.normalized;
                }
            }

            return best;
        }

        /// <summary>The way a mesh's upward faces look, area-weighted (world space): a panel's face.</summary>
        private static Vector3 UpwardFace(MeshFilter filter)
        {
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                throw new InvalidOperationException("The retroreflector's panel has no mesh (Art's R1 contract).");
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Transform node = filter.transform;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = node.TransformPoint(vertices[triangles[i]]);
                Vector3 normal = Vector3.Cross(node.TransformPoint(vertices[triangles[i + 1]]) - a,
                    node.TransformPoint(vertices[triangles[i + 2]]) - a);
                sum += normal.y > 0f ? normal : Vector3.zero;
            }

            return sum.normalized;
        }

        /// <summary>Where the Sun stands as the opening holds at golden hour (B6-B8), toward it.</summary>
        internal static Vector3 GoldenHourSun(WorldSettings settings)
        {
            var clock = new SunClock(settings.SkyCycle, settings.Sky);
            clock.SetTime(new OpeningSkyPacing(settings.SkyCycle, clock).GoldenHourTime);
            return clock.SunDirection;
        }

        /// <summary>
        /// Bootprint trail A (basin dressing §2): Art's imprint tiles end to end along the trail's waypoints, each
        /// laid on the ground's slope and wandering a little off the line. They are pressed into the dust: no
        /// colliders.
        /// </summary>
        private static void PressBootprints(SceneBuildContext context, LandmarkSettings landmarks, MoonSurface surface,
            Transform parent)
        {
            Vector2[] trail = landmarks.BootprintTrail;
            var prefab = context.LoadAsset<GameObject>(WorldPaths.BootprintTrailPrefab);
            float tile = Vector3.Distance(Node(prefab, TileStart).position, Node(prefab, TileEnd).position);
            int count = 0;
            for (int leg = 1; leg < trail.Length; leg++)
            {
                Vector2 from = trail[leg - 1];
                Vector2 along = trail[leg] - from;
                int tiles = Mathf.Max(1, Mathf.RoundToInt(along.magnitude / tile));
                Vector2 step = along / tiles;
                var forward = new Vector3(step.x, 0f, step.y);
                var across = new Vector2(-step.y, step.x).normalized;
                for (int i = 0; i < tiles; i++, count++)
                {
                    Vector2 spot = from + step * (i + 0.5f) + across * (landmarks.BootprintWander *
                                                                         Mathf.Sin(count * WanderPhase));
                    Vector3 slope = surface.SampleNormal(spot.x, spot.y);
                    GameObject print = context.InstantiatePrefab(prefab, parent,
                        Ground(surface, new Vector3(spot.x, 0f, spot.y)),
                        Quaternion.FromToRotation(Vector3.up, slope) * Quaternion.LookRotation(forward, Vector3.up)
                        * prefab.transform.localRotation);
                    print.name = BootprintTrailName + "_" + count.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// The Scout, landed in 1967 (basin dressing §2, §4): square on its small crater's inner wall, with its old
        /// blast patch laid along the slope under it. Its camera and scoop arm are gone.
        /// </summary>
        private static void StandScout(SceneBuildContext context, LandmarkSettings landmarks, MoonSurface surface,
            Transform parent)
        {
            Vector2 spot = surface.Shaping.ScoutSpot;
            Vector3 ground = Ground(surface, new Vector3(spot.x, 0f, spot.y));
            Vector3 slope = surface.SampleNormal(spot.x, spot.y);
            Quaternion onSlope = Quaternion.FromToRotation(Vector3.up, slope)
                * Quaternion.Euler(0f, landmarks.ScoutYaw, 0f);

            var prefab = context.LoadAsset<GameObject>(WorldPaths.OldLanderPrefab);
            GameObject scout = context.InstantiatePrefab(prefab, parent, ground - slope * landmarks.ScoutSink,
                onSlope * prefab.transform.localRotation);
            scout.name = ScoutName;
            foreach (string part in ScoutRemovedParts)
            {
                Node(scout, part).gameObject.SetActive(false);
            }

            Solidify(scout);

            var blast = context.LoadAsset<GameObject>(WorldPaths.OldLanderBlastPrefab);
            GameObject patch = context.InstantiatePrefab(blast, parent, ground - slope * landmarks.ScoutBlastSink,
                onSlope * blast.transform.localRotation);
            patch.name = ScoutBlastName;
        }

        /// <summary>
        /// The rare big boulders on the basin floor (<see cref="BigBoulderPlanner"/>), kept off the opening's routes:
        /// the crew route, 07's cable to the lander and the lander's to the apron.
        /// </summary>
        private static void StandBigBoulders(SceneBuildContext context, WorldSettings settings, MoonSurface surface,
            WorldAnchors anchors, Transform parent)
        {
            var routes = new List<Vector2>();
            string[] route = settings.GroundMarks.Route;
            for (int i = 1; i < route.Length; i++)
            {
                routes.Add(Flat2(Require(anchors, route[i - 1]).Position));
                routes.Add(Flat2(Require(anchors, route[i]).Position));
            }

            routes.Add(Flat2(Require(anchors, WorldAnchorIds.PlaceApron).Position));
            routes.Add(Flat2(Require(anchors, WorldAnchorIds.PlaceLander).Position));
            BigBoulderSettings tuning = settings.Scatter.BigBoulders;
            foreach (BigBoulderPlanner.Site site in BigBoulderPlanner.Plan(surface, anchors, tuning, routes))
            {
                string rock = tuning.Rocks[site.Rock];
                float height = context.LoadAsset<GameObject>(WorldPaths.R1RockPrefab(rock))
                    .GetComponentInChildren<Renderer>().bounds.size.y;
                StandRock(context, parent, rock, site.Position - Vector3.up * height * tuning.Sink, site.Yaw);
            }
        }

        /// <summary>
        /// The boulders at the foot of their furrows down the big old crater's wall (<see cref="ReliefSettings.Tracks"/>),
        /// half-buried where they came to rest, turned along the way they rolled.
        /// </summary>
        private static void StandTrackBoulders(SceneBuildContext context, ReliefSettings relief, MoonSurface surface,
            Transform parent)
        {
            foreach (BoulderTrack track in relief.Tracks)
            {
                float height = context.LoadAsset<GameObject>(WorldPaths.R1RockPrefab(track.Rock))
                    .GetComponentInChildren<Renderer>().bounds.size.y;
                Vector2 rolled = track.End - track.Start;
                float yaw = Mathf.Atan2(rolled.x, rolled.y) * Mathf.Rad2Deg;
                Vector3 rest = Ground(surface, new Vector3(track.End.x, 0f, track.End.y));
                StandRock(context, parent, track.Rock, rest - Vector3.up * height * relief.TrackBoulderSink, yaw);
            }
        }

        /// <summary>An R1 rock settled in the dust, solid with convex colliders on its meshes.</summary>
        private static void StandRock(SceneBuildContext context, Transform parent, string id, Vector3 position,
            float yaw)
        {
            Stand(context, parent, id, WorldPaths.R1RockPrefab(id), position,
                Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, true);
        }

        /// <summary>
        /// Makes a prop without authored collision proxies (the lander, the rocks, the debris and the dead machines)
        /// solid: a convex collider on each of its meshes, on <see cref="Layers.Prop"/>.
        /// </summary>
        private static void Solidify(GameObject prop)
        {
            prop.layer = Layers.Prop;
            foreach (MeshFilter filter in prop.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.layer = Layers.Prop;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;
            }
        }

        /// <summary>
        /// The R1 cargo lander on its footprint at place.lander: its long side, where the service port is, faces the
        /// build apron, and it leans a little off level from a hard landing. <paramref name="upright"/> is its own frame
        /// (without Art's FBX root rotation): +Y up through the hull, +Z out of the port side.
        /// </summary>
        private static GameObject LandLander(SceneBuildContext context, WorldSettings settings, MoonSurface surface,
            Transform landerRoot, WorldAnchor anchor, out Matrix4x4 upright)
        {
            Quaternion facing = Quaternion.LookRotation(anchor.Forward, Vector3.up);
            Quaternion lean = Quaternion.AngleAxis(settings.Landmarks.LanderTilt, anchor.Forward);
            var prefab = context.LoadAsset<GameObject>(WorldPaths.CargoLanderPrefab);
            Vector3 ground = Ground(surface, anchor.Position);
            upright = Matrix4x4.TRS(ground, lean * facing, Vector3.one);
            GameObject lander = context.InstantiatePrefab(prefab, landerRoot, ground,
                lean * facing * prefab.transform.localRotation);
            lander.name = CargoLanderName;
            Solidify(lander);
            return lander;
        }

        /// <summary>
        /// Art's service port on the ground against the lander's port side (its back to the hull, its sockets and
        /// readout facing the apron).
        /// </summary>
        private static GameObject StandPort(SceneBuildContext context, WorldSettings settings, MoonSurface surface,
            Transform landerRoot, GameObject lander, Matrix4x4 upright)
        {
            Bounds local = LocalBounds(upright, lander);
            var edge = new Vector3(local.center.x, local.min.y,
                local.max.z - settings.Landmarks.WakeCable.PortInset);
            Vector3 outward = Flat(upright.GetColumn(2));
            return Stand(context, landerRoot, ServicePortName, WorldPaths.LanderServicePortPrefab,
                Ground(surface, upright.MultiplyPoint3x4(edge)), -outward, true);
        }

        /// <summary>
        /// Art's amber beacon on top of the hull, World's tight halo and warm light at its light anchor, and the
        /// <see cref="LanderBeacon"/> that pulses them and runs the waves along the wake cable's clip halos.
        /// </summary>
        private static void StandBeacon(SceneBuildContext context, WorldSettings settings, Transform landerRoot,
            GameObject lander, Matrix4x4 upright, GroundCableLayer.Wiring wiring, Renderer[] apronClips,
            Renderer pilotLamp)
        {
            Bounds local = LocalBounds(upright, lander);
            Vector3 top = upright.MultiplyPoint3x4(new Vector3(local.center.x, local.max.y, local.center.z));
            var prefab = context.LoadAsset<GameObject>(WorldPaths.LanderBeaconPrefab);
            GameObject beacon = context.InstantiatePrefab(prefab, landerRoot, top,
                upright.rotation * prefab.transform.localRotation);
            beacon.name = LanderBeaconName;

            LanderBeaconSettings tuning = settings.Landmarks.LanderBeacon;
            GameObject halo = context.CreateChild("Halo", Node(beacon, BeaconLightAnchor));
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.LanderBeaconMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var light = halo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = tuning.Color;
            light.range = tuning.LightRange;
            light.shadows = LightShadows.None;

            var pulse = landerRoot.gameObject.AddComponent<LanderBeacon>();
            var serialized = new SerializedObject(pulse);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_lens").objectReferenceValue = Node(beacon, BeaconLens).GetComponent<Renderer>();
            serialized.FindProperty("_halo").objectReferenceValue = haloRenderer;
            serialized.FindProperty("_light").objectReferenceValue = light;
            Assign(serialized.FindProperty("_postClips"), wiring.PostClips);
            Assign(serialized.FindProperty("_landerClips"), wiring.LanderClips);
            serialized.FindProperty("_spark").objectReferenceValue = wiring.Spark;
            Assign(serialized.FindProperty("_apronClips"), apronClips);
            serialized.FindProperty("_pilotLamp").objectReferenceValue = pilotLamp;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(pulse);
        }

        private static void Assign(SerializedProperty list, Renderer[] renderers)
        {
            list.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        private static void StandAll(SceneBuildContext context, Transform parent, WorldAnchors anchors,
            MoonSurface surface, PropPlacement[] placements)
        {
            foreach (PropPlacement placement in placements)
            {
                Vector3 near = Require(anchors, placement.Anchor).Position;
                var at = new Vector3(near.x + placement.Offset.x, 0f, near.z + placement.Offset.y);
                Vector3 ground = Ground(surface, at) - Vector3.up * placement.Sink;
                Quaternion yaw = Quaternion.Euler(0f, placement.Yaw, 0f);
                Stand(context, parent, placement.Id, WorldPaths.KestrelDebrisPrefab(placement.Id), ground,
                    yaw * Vector3.forward, true);
            }
        }

        /// <summary>
        /// Stands an R1 prop facing <paramref name="forward"/>; a <paramref name="solid"/> one gets convex colliders
        /// (markings on the ground and the signal's light stay passable).
        /// </summary>
        private static GameObject Stand(SceneBuildContext context, Transform parent, string name, string prefabPath,
            Vector3 position, Vector3 forward, bool solid)
        {
            var prefab = context.LoadAsset<GameObject>(prefabPath);
            GameObject prop = context.InstantiatePrefab(prefab, parent, position,
                Quaternion.LookRotation(Flat(forward), Vector3.up) * prefab.transform.localRotation);
            prop.name = name;
            if (solid)
            {
                Solidify(prop);
            }

            return prop;
        }

        /// <summary>The renderers' bounds in the space of <paramref name="frame"/> (local to world).</summary>
        private static Bounds LocalBounds(Matrix4x4 frame, GameObject model)
        {
            Matrix4x4 toFrame = frame.inverse;
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
            {
                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = toFrame.MultiplyPoint3x4(filter.transform.TransformPoint(local));
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return any ? bounds : throw new InvalidOperationException($"{model.name} has no meshes to measure.");
        }

        /// <summary>
        /// relay.1's lamp: a warm halo floated at the dead mast's beacon node, kept apart from the prop, and the
        /// <see cref="RelayAnswerLamp"/> that keys it once the call is answered.
        /// </summary>
        private static void StandRelayLamp(SceneBuildContext context, WorldSettings settings, Transform props,
            GameObject westMast)
        {
            Transform node = Node(westMast, RelayLampNode);
            GameObject holder = context.CreateChild(RelayLampName, props);
            holder.transform.position = node.position + Vector3.up * settings.Landmarks.RelayLamp.HaloLift;
            holder.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var halo = holder.AddComponent<MeshRenderer>();
            halo.sharedMaterial = context.LoadAsset<Material>(WorldPaths.ModuleLampHaloMaterial);
            halo.shadowCastingMode = ShadowCastingMode.Off;
            halo.receiveShadows = false;
            halo.lightProbeUsage = LightProbeUsage.Off;
            halo.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var lamp = holder.AddComponent<RelayAnswerLamp>();
            var serialized = new SerializedObject(lamp);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_halo").objectReferenceValue = halo;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(lamp);
        }

        private static Transform Node(GameObject root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"{root.name} has no '{name}' node (Art's R1 contract).");
        }

        private static WorldAnchor Require(WorldAnchors anchors, string id)
        {
            return anchors.TryGet(id, out WorldAnchor anchor) ? anchor
                : throw new InvalidOperationException($"The world has no '{id}' anchor for an opening prop.");
        }

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.z), at.z);
        }

        private static Vector2 Flat2(Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.normalized;
        }
    }
}
