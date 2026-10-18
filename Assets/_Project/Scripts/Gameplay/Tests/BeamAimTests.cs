using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay.PlayModeTests;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The scan and weld beams end on their target's real surface, never in the air beside or past it (owner,
    /// 2026-10-18): the pure aim math, and a scan target's whole sweep on colliders at an angle, tall, low, on an
    /// L-shaped hull whose middle is air, and on a prop with no colliders (its mesh's own box).
    /// </summary>
    public sealed class BeamAimTests
    {
        /// <summary>Metres a beam's end may stand off its target's surface.</summary>
        private const float OnSurface = 0.01f;

        private const int SweepSteps = 24;

        private readonly List<GameObject> _made = new List<GameObject>();
        private Material _surface;

        [SetUp]
        public void SetUp()
        {
            _surface = new Material(AssetDatabase.LoadAssetAtPath<Shader>(GameplayFixture.R1LitShaderPath));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
            Object.DestroyImmediate(_surface);
        }

        [Test]
        public void ARay_MeetsTheNearestFaceOfACollider_NotItsMiddle()
        {
            BoxCollider box = Box(null, new Vector3(0f, 0.5f, 4f), Vector3.one, Quaternion.identity);
            Physics.SyncTransforms();

            Assert.IsTrue(SurfaceAim.TryHit(new Collider[] { box }, new Vector3(0f, 1f, 0f), box.bounds.center,
                out Vector3 point, out Vector3 normal));
            Assert.AreEqual(3.5f, point.z, 1e-3f, "the near face");
            Assert.Greater(Vector3.Dot(normal, Vector3.back), 0.99f, "facing the emitter");
            Assert.Less(FaceDistance(box, point), OnSurface);
        }

        [Test]
        public void AnAimInTheAir_FindsTheNearestRealSurface()
        {
            BoxCollider a = Box(null, new Vector3(-1f, 0.5f, 4f), new Vector3(0.6f, 1f, 0.6f), Quaternion.identity);
            BoxCollider b = Box(null, new Vector3(1f, 0.5f, 4f), new Vector3(0.6f, 1f, 0.6f), Quaternion.identity);
            Physics.SyncTransforms();
            var hull = new Collider[] { a, b };

            Vector3 air = new Vector3(0f, 0.5f, 4f);
            Assert.IsTrue(SurfaceAim.TryClosest(hull, air, out Vector3 point, out _));
            Assert.AreEqual(0.7f, Mathf.Abs(point.x), 1e-3f, "the inner face of the nearer box, not the gap");
        }

        [Test]
        public void ABoxHit_LandsOnARotatedScaledBoxsFace()
        {
            Matrix4x4 m = Matrix4x4.TRS(new Vector3(2f, 1f, 5f), Quaternion.Euler(0f, 35f, 10f),
                new Vector3(1.5f, 0.4f, 2f));
            var local = new Bounds(Vector3.zero, Vector3.one);
            Assert.IsTrue(SurfaceAim.TryHitBox(m, local, Vector3.zero, m.MultiplyPoint3x4(Vector3.zero),
                out Vector3 point, out Vector3 normal));
            AssertOnBoxFace(m, local, point);
            Assert.Less(Vector3.Dot(normal, point), 0f, "the face toward the emitter");

            SurfaceAim.ClosestOnBox(m, local, m.MultiplyPoint3x4(new Vector3(0.1f, 0f, 0f)), out Vector3 face, out _);
            AssertOnBoxFace(m, local, face);
        }

        [Test]
        public void TheSweepLine_ClimbsTheBounds_AsTheShaderDrawsIt()
        {
            var bounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 1f));
            Vector2 toCamera = Vector2.up;
            float tilt = 0.5f * ScanLine.Tilt * ScanLine.TiltReach;
            Assert.AreEqual(0f - tilt, ScanLine.Height(bounds, toCamera, 0f), 1e-4f);
            Assert.AreEqual(2f + tilt, ScanLine.Height(bounds, toCamera, 1f), 1e-4f);

            Vector3 aim = ScanLine.AimPoint(bounds, 5f, toCamera);
            Assert.Less(aim.y, bounds.max.y, "the aim stays inside the target's own top");
            Assert.IsTrue(bounds.Contains(aim), "and inside its footprint");

            float onTop = bounds.max.y + 0.1f;
            Vector3 across = ScanLine.AimPoint(bounds, onTop, toCamera);
            Assert.AreEqual(onTop, ScanLine.SweepHeightOf(across, bounds.center, toCamera), 1e-4f,
                "past its top the aim travels across it, still on the tilted line");
        }

        [TestCase(0f, 1f, 1.2f, TestName = "AScanSweep_StaysOnASquareTarget")]
        [TestCase(65f, 1f, 1.2f, TestName = "AScanSweep_StaysOnATargetAtAnAngle")]
        [TestCase(20f, 0.6f, 4f, TestName = "AScanSweep_StaysOnATallTarget")]
        [TestCase(-40f, 2.4f, 0.25f, TestName = "AScanSweep_StaysOnALowTarget")]
        public void AScanSweep_StaysOnItsTargetsColliders(float yaw, float width, float height)
        {
            GameObject ruin = Made("Ruin");
            ruin.transform.SetPositionAndRotation(new Vector3(0.5f, 0f, 3.5f), Quaternion.Euler(0f, yaw, 0f));
            Transform part = Shown(ruin.transform, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, 0.8f));
            BoxCollider hull = Box(ruin.transform, part.position, new Vector3(width, height, 0.8f), part.rotation);
            ScanFragment fragment = Fragment(ruin.transform, part.position + Vector3.up * height * 0.3f);
            Assert.AreEqual(1, fragment.Surface.Count, "the ruin's hull box is the target's surface");

            Sweep(fragment, p => FaceDistance(hull, p));
        }

        [Test]
        public void AScanSweep_OnAnLShapedHullWithAirInItsMiddle_NeverEndsInTheAir()
        {
            GameObject ruin = Made("Ruin");
            ruin.transform.position = Vector3.zero;
            Transform part = Shown(ruin.transform, new Vector3(0f, 1f, 4f), new Vector3(2f, 2f, 2f));
            BoxCollider wall = Box(ruin.transform, new Vector3(0.8f, 1f, 4f), new Vector3(0.4f, 2f, 2f),
                Quaternion.identity);
            BoxCollider floor = Box(ruin.transform, new Vector3(0f, 0.15f, 4f), new Vector3(2f, 0.3f, 2f),
                Quaternion.identity);
            ScanFragment fragment = Fragment(ruin.transform, part.position);
            Assert.AreEqual(2, fragment.Surface.Count);

            Sweep(fragment, p => Mathf.Min(FaceDistance(wall, p), FaceDistance(floor, p)));
        }

        [Test]
        public void AScanSweep_OnAPropWithNoColliders_StaysOnItsMeshsBox()
        {
            GameObject crate = Made("Crate");
            crate.transform.SetPositionAndRotation(new Vector3(-0.4f, 0f, 3f), Quaternion.Euler(0f, 30f, 0f));
            Transform part = Shown(crate.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.08f, 0.3f));
            ScanFragment fragment = Fragment(part, part.position);
            Assert.AreEqual(0, fragment.Surface.Count);

            var local = new Bounds(Vector3.zero, Vector3.one);
            Sweep(fragment, p => DistanceToBoxFace(part.localToWorldMatrix, local, p));
        }

        private void Sweep(ScanFragment fragment, Func<Vector3, float> distanceToSurface)
        {
            Physics.SyncTransforms();
            var lens = new Vector3(0.2f, 1.1f, 0f);
            var camera = new Vector3(0f, 2.4f, -3f);
            for (int step = 0; step <= SweepSteps; step++)
            {
                fragment.Progress = (float)step / SweepSteps;
                Assert.IsTrue(fragment.TryContact(lens, camera, out Vector3 point, out _));
                float off = distanceToSurface(point);
                Assert.Less(off, OnSurface,
                    $"at {fragment.Progress:P0} the beam ends {off * 100f:F1} cm off the target, at {point}");
            }
        }

        private ScanFragment Fragment(Transform host, Vector3 at)
        {
            Transform target = new GameObject(ScanFragment.RuinScanPrefix + "1").transform;
            target.SetParent(host, false);
            target.position = at;
            return ScanFragment.ForRuinTarget("test.0", "test", Array.Empty<string>(), 0, 1, ScanSize.Small, "test",
                target, host);
        }

        private Transform Shown(Transform parent, Vector3 localCentre, Vector3 size)
        {
            GameObject shown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(shown.GetComponent<Collider>());
            shown.GetComponent<Renderer>().sharedMaterial = _surface;
            shown.transform.SetParent(parent, false);
            shown.transform.localPosition = localCentre;
            shown.transform.localScale = size;
            return shown.transform;
        }

        private BoxCollider Box(Transform parent, Vector3 centre, Vector3 size, Quaternion rotation)
        {
            GameObject hull = parent != null ? new GameObject("Colliders") : Made("Colliders");
            if (parent != null)
            {
                hull.transform.SetParent(parent, false);
            }

            hull.layer = Layers.Prop;
            hull.transform.SetPositionAndRotation(centre, rotation);
            var box = hull.AddComponent<BoxCollider>();
            box.size = size;
            return box;
        }

        private GameObject Made(string name)
        {
            var made = new GameObject(name);
            _made.Add(made);
            return made;
        }

        private static void AssertOnBoxFace(Matrix4x4 m, Bounds local, Vector3 point)
        {
            Assert.Less(DistanceToBoxFace(m, local, point), 1e-3f, $"{point} lies on the box's surface");
        }

        /// <summary>Metres from <paramref name="point"/> to <paramref name="box"/>'s nearest face (inside counts as
        /// off it too).</summary>
        private static float FaceDistance(BoxCollider box, Vector3 point)
        {
            return DistanceToBoxFace(box.transform.localToWorldMatrix, new Bounds(box.center, box.size), point);
        }

        /// <summary>Metres from <paramref name="point"/> to the nearest face of the carried box.</summary>
        private static float DistanceToBoxFace(Matrix4x4 m, Bounds local, Vector3 point)
        {
            SurfaceAim.ClosestOnBox(m, local, point, out Vector3 face, out _);
            return Vector3.Distance(face, point);
        }
    }
}
