using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Core;
using MoonProject.World.Editor;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The Lumen ruins' colliders come from Art's authored collision proxies (docs/art/collision-proxies.md): every
    /// UCX_* child becomes one hidden convex collider and nothing else collides but the few drawn parts made solid by
    /// their own hull (the radio tower's dish, so the chase camera keeps out of it); 07's body cannot pass the
    /// workshop's walls or the habitat's modules, while it fits through the workshop's doorway onto its floor and up
    /// to the habitat's airlock panel.
    /// </summary>
    public sealed class RuinColliderTests
    {
        // 07's body (the scale bible, docs/art/collision-proxies.md): 1.47 m wide, 2.01 m long, 1.59 m high, lifted
        // a little off the floor so the ground itself never counts as a hit.
        private static readonly Vector3 RoverHalfExtents = new Vector3(0.735f, 0.795f, 1.005f);
        private const float FloorLift = 0.05f;

        // 07 drives up to the airlock panel and stops with its nose this far from it.
        private const float AirlockStandoff = 0.5f;

        // The floor search for 07's spot, and how far from the shell's middle (a share of its half-diagonal) the spot
        // may lie and still be inside.
        private const float FloorSearchStep = 0.1f;
        private const float InsideShare = 0.5f;

        // Somewhere empty, away from anything else a test scene holds.
        private static readonly Vector3 TestSite = new Vector3(1000f, 0f, 1000f);

        private const string Workshop = "KenjiWorkshop";
        private const string Habitat = "HabitatModules";
        private const string AirlockPanel = "AirlockPanel";
        private const string RadioTower = "RadioTower";
        private const string TowerDish = "Dish";

        // A drawn vertex this close to a proxy counts as inside it; the share of a part's vertices that must be.
        private const float CoverSlack = 0.15f;
        private const float MinCoveredShare = 0.9f;

        // How far a proxy may stand out past the drawn part it stands for, metres, on any side.
        private const float MaxProxyOverhang = 0.5f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawned in _spawned)
            {
                UnityEngine.Object.DestroyImmediate(spawned);
            }

            _spawned.Clear();
        }

        [Test]
        public void EveryRuin_CollidesOnlyThroughItsHiddenConvexProxies()
        {
            foreach (RuinPlacement piece in new LandmarkSettings().Ruins)
            {
                GameObject ruin = Spawn(piece.Id);
                int proxies = 0;
                foreach (MeshFilter filter in ruin.GetComponentsInChildren<MeshFilter>(true))
                {
                    proxies += IsProxy(filter) ? 1 : 0;
                }

                int hulls = 0;
                foreach (string part in new LandmarkSettings().HullParts)
                {
                    hulls += part.StartsWith(piece.Id + "/", StringComparison.Ordinal) ? 1 : 0;
                }

                Collider[] colliders = ruin.GetComponentsInChildren<Collider>(true);
                Assert.That(proxies, Is.GreaterThan(0), $"{piece.Id} has no collision proxies");
                Assert.AreEqual(proxies + hulls, colliders.Length,
                    $"{piece.Id}: one collider per proxy and hull part, nothing else");
                foreach (Collider collider in colliders)
                {
                    var mesh = collider as MeshCollider;
                    Assert.IsNotNull(mesh, $"{collider.name} is a mesh collider");
                    Assert.IsTrue(mesh.convex, $"{collider.name} is convex");
                    Assert.AreEqual(Layers.Prop, collider.gameObject.layer, $"{collider.name} is on the Prop layer");
                    bool proxy = IsProxy(collider.GetComponent<MeshFilter>());
                    Assert.AreEqual(!proxy, collider.GetComponent<Renderer>().enabled,
                        $"{collider.name}: a proxy is hidden, a hull part stays drawn");
                }
            }
        }

        [Test]
        public void Workshop_LetsRoverInThroughItsDoorway_AndHoldsItInsideItsWalls()
        {
            GameObject workshop = Spawn(Workshop);
            Bounds shell = RenderBounds(workshop);
            Quaternion heading = Heading(workshop.transform);
            Vector3 centre = FreeSpotNear(shell, heading);

            float reach = shell.extents.magnitude + RoverHalfExtents.z;
            var open = new List<string>();
            foreach (Vector3 way in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Vector3 direction = heading * way;
                Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
                if (!Physics.BoxCast(centre, RoverHalfExtents, direction, facing, reach, Layers.PropMask))
                {
                    open.Add(way.ToString());
                }
            }

            Assert.AreEqual(1, open.Count, "exactly one way out of the workshop (its doorway), walls on the other " +
                $"three; open: {string.Join(", ", open)}");
        }

        [Test]
        public void Habitat_IsSolid_AndRoverCanDriveUpToItsAirlockPanel()
        {
            GameObject habitat = Spawn(Habitat);
            Bounds hull = RenderBounds(habitat);
            Vector3 middle = new Vector3(hull.center.x, TestSite.y + RoverHalfExtents.y + FloorLift, hull.center.z);
            Assert.IsTrue(Physics.CheckBox(middle, RoverHalfExtents, Quaternion.identity, Layers.PropMask),
                "07 cannot stand inside the habitat's modules");

            // Art's nodes all pivot at the FBX root, so the panel is where it is drawn.
            Vector3 panel = Find(habitat.transform, AirlockPanel).GetComponentInChildren<Renderer>().bounds.center;
            Vector3 outward = panel - NearestHullPoint(habitat, panel);
            outward.y = 0f;
            outward.Normalize();
            Vector3 stop = new Vector3(panel.x, TestSite.y, panel.z)
                + outward * (AirlockStandoff + RoverHalfExtents.z) + Vector3.up * (RoverHalfExtents.y + FloorLift);
            Assert.IsEmpty(InTheWay(stop, Quaternion.LookRotation(-outward, Vector3.up)),
                "07 can stop nose-on at the airlock panel");
        }

        [Test]
        public void TheRadioTowersDish_LiesInsideColliders_SoTheCameraKeepsOutOfIt()
        {
            GameObject tower = Spawn(RadioTower);
            Collider[] proxies = tower.GetComponentsInChildren<Collider>(true);
            var report = new System.Text.StringBuilder(
                "[tower-cover] share of each drawn part's vertices inside a collider:");
            float dish = 0f;
            foreach (MeshFilter filter in tower.GetComponentsInChildren<MeshFilter>(true))
            {
                if (IsProxy(filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                int inside = 0;
                Vector3[] vertices = filter.sharedMesh.vertices;
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 world = filter.transform.TransformPoint(vertex);
                    foreach (Collider proxy in proxies)
                    {
                        if (Vector3.Distance(proxy.ClosestPoint(world), world) <= CoverSlack)
                        {
                            inside++;
                            break;
                        }
                    }
                }

                float share = vertices.Length > 0 ? inside / (float)vertices.Length : 1f;
                report.Append($" {filter.name} {share:P0};");
                dish = filter.name == TowerDish ? share : dish;
            }

            TestContext.WriteLine(report.ToString());
            Assert.GreaterOrEqual(dish, MinCoveredShare, "the tower's dish frame reaches out of its colliders: the " +
                "chase camera's deoccluder cannot see it");
        }

        [Test]
        public void TheHabitatsProxies_HugTheModulesTheyStandFor()
        {
            GameObject habitat = Spawn(Habitat);
            var drawn = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            foreach (Renderer renderer in habitat.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled)
                {
                    drawn[renderer.name] = renderer.bounds;
                }
            }

            var report = new System.Text.StringBuilder("[habitat-proxies] proxy overhang past its part, metres:");
            float worst = 0f;
            string worstProxy = null;
            foreach (Collider proxy in habitat.GetComponentsInChildren<Collider>(true))
            {
                string name = proxy.name.Substring(R1ModelPostprocessor.CollisionProxyPrefix.Length);
                string part = name.Substring(0, name.LastIndexOf('_'));
                Assert.IsTrue(drawn.TryGetValue(part, out Bounds shape), $"{proxy.name} stands for a drawn {part}");
                Vector3 over = Vector3.Max(proxy.bounds.max - shape.max, shape.min - proxy.bounds.min);
                float most = Mathf.Max(over.x, over.y, over.z);
                report.Append($" {proxy.name} {most:F2};");
                if (most > worst)
                {
                    worst = most;
                    worstProxy = proxy.name;
                }
            }

            TestContext.WriteLine(report.ToString());
            Assert.LessOrEqual(worst, MaxProxyOverhang, $"{worstProxy} stands out past its module: the scan camera " +
                "pulls back from empty space");
        }

        private GameObject Spawn(string id)
        {
            string path = WorldPaths.RuinPrefab(id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"no ruin prefab at {path}; run Art's R1 builders");
            GameObject ruin = UnityEngine.Object.Instantiate(prefab, TestSite, prefab.transform.localRotation);
            ruin.name = id;
            _spawned.Add(ruin);
            RuinColliderBuilder.Fit(ruin, new LandmarkSettings().HullParts);
            Physics.SyncTransforms();
            return ruin;
        }

        /// <summary>
        /// The free spot for 07's body on the floor nearest the middle of <paramref name="shell"/>, facing along
        /// <paramref name="heading"/>.
        /// </summary>
        private static Vector3 FreeSpotNear(Bounds shell, Quaternion heading)
        {
            Vector3 middle = new Vector3(shell.center.x, TestSite.y + RoverHalfExtents.y + FloorLift, shell.center.z);
            float best = float.MaxValue;
            Vector3 spot = middle;
            for (float across = -shell.extents.x; across <= shell.extents.x; across += FloorSearchStep)
            {
                for (float along = -shell.extents.z; along <= shell.extents.z; along += FloorSearchStep)
                {
                    Vector3 at = middle + heading * new Vector3(across, 0f, along);
                    float distance = Vector3.Distance(at, middle);
                    if (distance < best && !Physics.CheckBox(at, RoverHalfExtents, heading, Layers.PropMask))
                    {
                        best = distance;
                        spot = at;
                    }
                }
            }

            Assert.Less(best, float.MaxValue, "07 fits nowhere on the workshop's floor");
            Assert.Less(best, shell.extents.magnitude * InsideShare, "07's only room is outside the workshop");
            return spot;
        }

        /// <summary>The point on <paramref name="ruin"/>'s colliders nearest <paramref name="point"/>.</summary>
        private static Vector3 NearestHullPoint(GameObject ruin, Vector3 point)
        {
            Vector3 nearest = point;
            float best = float.MaxValue;
            foreach (Collider collider in ruin.GetComponentsInChildren<Collider>())
            {
                Vector3 onHull = collider.ClosestPoint(point);
                float distance = Vector3.Distance(onHull, point);
                if (distance < best)
                {
                    best = distance;
                    nearest = onHull;
                }
            }

            Assert.Greater(best, 0f, "the airlock panel lies inside a collider");
            return nearest;
        }

        /// <summary>The colliders a rover-sized box at <paramref name="centre"/> would overlap, with their bounds.</summary>
        private static List<string> InTheWay(Vector3 centre, Quaternion rotation)
        {
            var names = new List<string>();
            foreach (Collider hit in Physics.OverlapBox(centre, RoverHalfExtents, rotation, Layers.PropMask))
            {
                names.Add($"{hit.name} {hit.bounds} (box at {centre})");
            }

            return names;
        }

        private static bool IsProxy(MeshFilter filter)
        {
            return filter != null
                && filter.name.StartsWith(R1ModelPostprocessor.CollisionProxyPrefix, StringComparison.Ordinal);
        }

        /// <summary>The drawn meshes' world bounds (the hidden proxies left out).</summary>
        private static Bounds RenderBounds(GameObject ruin)
        {
            var bounds = new Bounds();
            bool any = false;
            foreach (Renderer renderer in ruin.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (any)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }

            Assert.IsTrue(any, $"{ruin.name} draws nothing");
            return bounds;
        }

        /// <summary>
        /// A ruin's heading on the ground: Art's FBX root is turned so its up runs along the ground (its forward
        /// points down).
        /// </summary>
        private static Quaternion Heading(Transform ruin)
        {
            Vector3 forward = ruin.up;
            forward.y = 0f;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            Assert.Fail($"{root.name} has no '{name}'");
            return null;
        }
    }
}
