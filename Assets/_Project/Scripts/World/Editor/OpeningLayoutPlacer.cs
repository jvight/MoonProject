using System;
using System.Collections.Generic;
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
    /// that pulses it and its cable's markers), 07's dead charging post at the wake with its old cable to the port,
    /// the port's cable to the build apron and the apron's markings, the dead west relay mast with its (hidden) signal
    /// pillar, Kestrel-3's debris, the big boulders near the routes and the rare big boulders on the floor
    /// (<see cref="BigBoulderPlanner"/>). Everything solid is fitted with colliders on <see cref="Layers.Prop"/>.
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

        // Art's R1 prop nodes this layout joins (tools/art/qa/contracts).
        private const string PostCableOrigin = "CableOrigin_Anchor";
        private const string PortWakeSocket = "CableSocket";
        private const string PortApronSocket = "Socket_Cable";
        private const string BeaconLightAnchor = "Light_Anchor";
        private const string BeaconLens = "BeaconLens";
        private const string ApronCableChannel = "CableChannel_Anchor";
        private const string SegmentStart = "Start_Anchor";
        private const string SegmentEnd = "End_Anchor";
        private const string SegmentMarker = "MarkerLamp";
        private const string EndPlug = "Plug_Anchor";
        private const string PillarBeam = "PillarBeam";

        private const string WestMastAnchor = WorldAnchorIds.RelayPrefix + "1";

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
            Stand(context, props, WestMastName, WorldPaths.DeadAntennaMastPrefab, mast.Position, mast.Forward, true);
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
            List<Renderer> markers = LayCable(context, props, "WakeCable", surface, cables,
                Node(post, PostCableOrigin).position, Node(port, PortWakeSocket).position);
            LayCable(context, props, "ApronCable", surface, cables, Node(markings, ApronCableChannel).position,
                Node(port, PortApronSocket).position);
            StandBeacon(context, settings, landerRoot, lander, upright, markers);

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

        /// <summary>An R1 rock settled in the dust, solid with convex colliders on its meshes.</summary>
        private static void StandRock(SceneBuildContext context, Transform parent, string id, Vector3 position,
            float yaw)
        {
            GameObject rock = Stand(context, parent, id, WorldPaths.R1RockPrefab(id), position,
                Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, false);
            rock.layer = Layers.Prop;
            foreach (MeshFilter filter in rock.GetComponentsInChildren<MeshFilter>())
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
            RuinColliderBuilder.Fit(lander);
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
        /// <see cref="LanderBeacon"/> that pulses them and runs the wake cable's markers toward it.
        /// </summary>
        private static void StandBeacon(SceneBuildContext context, WorldSettings settings, Transform landerRoot,
            GameObject lander, Matrix4x4 upright, List<Renderer> markers)
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
            SerializedProperty list = serialized.FindProperty("_markers");
            list.arraySize = markers.Count;
            for (int i = 0; i < markers.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(pulse);
        }

        /// <summary>
        /// Lays Art's tileable cable segments joint to joint on the dust from <paramref name="from"/> to the ground
        /// under a socket, and Art's cable end rising into <paramref name="socket"/>. Returns the segments' marker
        /// lamps in order from <paramref name="from"/>.
        /// </summary>
        private static List<Renderer> LayCable(SceneBuildContext context, Transform parent, string name,
            MoonSurface surface, GroundCableSettings tuning, Vector3 from, Vector3 socket)
        {
            var segmentPrefab = context.LoadAsset<GameObject>(WorldPaths.GroundCableSegmentPrefab);
            var endPrefab = context.LoadAsset<GameObject>(WorldPaths.GroundCableEndPrefab);
            float segmentLength = Vector3.Distance(Node(segmentPrefab, SegmentStart).position,
                Node(segmentPrefab, SegmentEnd).position);
            Vector3 plug = Node(endPrefab, EndPlug).position - Node(endPrefab, SegmentStart).position;
            float endReach = new Vector2(plug.x, plug.z).magnitude;

            Vector3 toSocket = Flat(socket - from);
            Vector3 endStart = socket - toSocket * endReach;
            Transform cable = context.CreateChild(name, parent).transform;
            Vector3[] joints = CableRoute.Joints(new Vector2(from.x, from.z), new Vector2(endStart.x, endStart.z),
                segmentLength, tuning.Wander, tuning.Bends, surface.SampleHeight);
            var markers = new List<Renderer>(joints.Length);
            for (int i = 1; i < joints.Length; i++)
            {
                Vector3 span = joints[i] - joints[i - 1];
                GameObject segment = context.InstantiatePrefab(segmentPrefab, cable, (joints[i] + joints[i - 1]) * 0.5f,
                    Quaternion.LookRotation(span, Vector3.up) * segmentPrefab.transform.localRotation);
                Vector3 scale = segment.transform.localScale;
                scale.y *= span.magnitude / segmentLength;
                segment.transform.localScale = scale;
                markers.Add(Node(segment, SegmentMarker).GetComponent<Renderer>());
            }

            Vector3 last = joints[joints.Length - 1];
            context.InstantiatePrefab(endPrefab, cable, last,
                Quaternion.LookRotation(Flat(socket - last), Vector3.up) * endPrefab.transform.localRotation);
            return markers;
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
        /// Stands an R1 prop facing <paramref name="forward"/>; a <paramref name="solid"/> one gets fitted colliders
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
                RuinColliderBuilder.Fit(prop);
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
