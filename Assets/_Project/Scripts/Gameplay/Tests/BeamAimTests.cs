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
    /// L-shaped hull whose middle is air, and on a prop with no colliders (its mesh's own box). Once a beam lets go
    /// (a weld or scan done, cancelled, or its target gone) it fades where it last met its target, its end never
    /// thrown toward a target point that went away (owner: a finished weld's beam shot off into the distance).
    /// </summary>
    public sealed class BeamAimTests
    {
        /// <summary>Metres a beam's end may stand off its target's surface.</summary>
        private const float OnSurface = 0.01f;

        private const int SweepSteps = 24;

        /// <summary>Metres a fading beam's end may stray from where it last met its target, and travel per frame.
        /// </summary>
        private const float FadeHold = 0.03f;

        private const float FadeStep = 0.05f;
        private const float Frame = 1f / 60f;
        private const int FadeFrames = 120;
        private const string FeelTuningPath = "Assets/_Project/Data/Tuning/Gameplay/FeelVfxTuning.asset";

        private static readonly Vector3 Emitter = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 Contact = new Vector3(0.3f, 0.4f, 2.5f);

        /// <summary>Where a caller's target point went once it lost its target: the world's origin, far off.</summary>
        private static readonly Vector3 Gone = new Vector3(-180f, -20f, 240f);

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

        [Test]
        public void AWeldBeamLettingGo_FadesWhereItLastMetItsTarget_WhateverTargetFollows()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<FeelVfxTuning>(FeelTuningPath);
            Assert.IsNotNull(tuning, FeelTuningPath);
            var beam = new CutBeam(Made("Beam").transform, tuning, _surface, _surface);
            beam.Extend();
            for (int i = 0; i < FadeFrames && beam.Reach < 1f; i++)
            {
                beam.Step(Frame, Emitter, Contact);
            }

            Assert.AreEqual(1f, beam.Reach, "the beam is fully out");
            Assert.Less(Vector3.Distance(beam.End, Contact), 1e-4f, "fully out it ends on its target");
            beam.Retract();
            AssertFadesInPlace(() =>
            {
                beam.Step(Frame, Emitter, Gone);
                return beam.Reach > 0f;
            }, () => beam.End, "the weld beam");
        }

        [Test]
        public void AScanBeamCompletedOrLetGo_FadesWhereItLastMetItsTarget_WhateverContactFollows(
            [Values(true, false)] bool completed)
        {
            var tuning = AssetDatabase.LoadAssetAtPath<FeelVfxTuning>(FeelTuningPath);
            var scanTuning = AssetDatabase.LoadAssetAtPath<ScanTuning>(Editor.GameplayAssetPaths.ScanTuning);
            Assert.IsNotNull(tuning, FeelTuningPath);
            Assert.IsNotNull(scanTuning, Editor.GameplayAssetPaths.ScanTuning);
            var beam = new ScanBeam(Made("ScanBeam").transform, tuning, _surface, _surface, scanTuning);
            beam.Begin(Contact, Vector3.up, 0.5f, 2f);
            for (int i = 0; i < FadeFrames && beam.Level < 1f; i++)
            {
                beam.Meet(Contact);
                beam.Step(Frame, Emitter);
            }

            Assert.AreEqual(1f, beam.Level, 1e-3f, "the beam is fully out");
            Assert.Less(Vector3.Distance(beam.WedgeTargetCenter, Contact), 1e-4f, "fully out it ends on its target");
            if (completed)
            {
                beam.Complete();
            }
            else
            {
                beam.End();
            }

            AssertFadesInPlace(() =>
            {
                beam.Meet(Gone);
                beam.Step(Frame, Emitter);
                return beam.Shown;
            }, () => beam.WedgeTargetCenter, completed ? "the completed scan's beam" : "the let-go scan beam");
        }

        /// <summary>
        /// Steps a fading beam (<paramref name="step"/> says whether it still shows) until it is gone, its end
        /// (<paramref name="end"/>) held within <see cref="FadeHold"/> of <see cref="Contact"/> and moving less than
        /// <see cref="FadeStep"/> a frame.
        /// </summary>
        private static void AssertFadesInPlace(Func<bool> step, Func<Vector3> end, string what)
        {
            Vector3 last = end();
            int frame = 0;
            while (frame < FadeFrames && step())
            {
                frame++;
                Vector3 now = end();
                Assert.LessOrEqual(Vector3.Distance(now, Contact), FadeHold,
                    $"fade frame {frame}: {what} ends at {now}, off where it last met its target {Contact}");
                Assert.LessOrEqual(Vector3.Distance(now, last), FadeStep,
                    $"fade frame {frame}: {what}'s end moved {Vector3.Distance(now, last) * 100f:F1} cm");
                last = now;
            }

            Assert.Less(frame, FadeFrames, $"{what} fades out");
            Assert.Greater(frame, 0, $"{what} shows a fade");
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
