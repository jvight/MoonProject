using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The Tab "07" portrait's camera: a side-three-quarter pose looking at 07, and where 07's sockets land
    /// in the render, matching Unity's own camera projection.</summary>
    public sealed class PortraitFramingTests
    {
        private const float Tolerance = 1e-3f;

        private readonly PortraitSettings _settings = new PortraitSettings(55f, 14f, 3.4f, 0.75f, 30f, 960, 640);

        [Test]
        public void TheCamera_StandsOffTo07sFrontRight_AboveIt_LookingAtItsMiddle()
        {
            PortraitFraming.Pose(_settings, out Vector3 position, out Quaternion rotation);
            Assert.Greater(position.x, 0f, "Toward 07's right side.");
            Assert.Greater(position.z, 0f, "Ahead of its nose: three-quarter, not from behind.");
            Assert.Greater(position.y, _settings.LookHeight, "A little above.");
            Vector3 target = Vector3.up * _settings.LookHeight;
            Assert.AreEqual(_settings.Distance, Vector3.Distance(position, target), Tolerance);
            Vector3 toTarget = (target - position).normalized;
            Assert.Greater(Vector3.Dot(rotation * Vector3.forward, toTarget), 0.9999f, "Aimed at 07's middle.");
        }

        [Test]
        public void TheLookPoint_IsTheRendersCentre_AndSidesLandWhereTheyShould()
        {
            Assert.IsTrue(PortraitFraming.TryViewport(_settings, Vector3.up * _settings.LookHeight, out Vector2 centre));
            Assert.AreEqual(0.5f, centre.x, Tolerance);
            Assert.AreEqual(0.5f, centre.y, Tolerance);

            Assert.IsTrue(PortraitFraming.TryViewport(_settings, new Vector3(0f, 1.3f, 0f), out Vector2 higher));
            Assert.Greater(higher.y, 0.5f, "Higher on 07 is higher in the picture.");
            Assert.IsTrue(PortraitFraming.TryViewport(_settings, new Vector3(0f, 0.75f, 0.6f), out Vector2 nose));
            Assert.Greater(nose.x, 0.5f, "From its front-right, 07's nose points to the picture's right.");
        }

        [Test]
        public void PointsBehindTheCameraOrOutOfFrame_HaveNoPlace()
        {
            PortraitFraming.Pose(_settings, out Vector3 position, out Quaternion rotation);
            Vector3 behind = position - rotation * Vector3.forward;
            Assert.IsFalse(PortraitFraming.TryViewport(_settings, behind, out _));
            Assert.IsFalse(PortraitFraming.TryViewport(_settings, new Vector3(0f, 0.75f, -20f), out _));
        }

        [Test]
        public void TheProjection_MatchesUnitysCamera()
        {
            var go = new GameObject("PortraitProbe");
            var camera = go.AddComponent<Camera>();
            PortraitFraming.Pose(_settings, out Vector3 position, out Quaternion rotation);
            go.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = _settings.FieldOfView;
            camera.aspect = _settings.Aspect;
            camera.nearClipPlane = _settings.Near;
            camera.farClipPlane = _settings.Far;
            var points = new[]
            {
                new Vector3(0.3f, 0.4f, 0.5f), new Vector3(-0.4f, 1.1f, -0.2f), new Vector3(0.45f, 0.2f, -0.6f),
            };
            foreach (Vector3 point in points)
            {
                Assert.IsTrue(PortraitFraming.TryViewport(_settings, point, out Vector2 uv), point.ToString());
                Vector3 unity = camera.WorldToViewportPoint(point);
                Assert.AreEqual(unity.x, uv.x, Tolerance, point.ToString());
                Assert.AreEqual(unity.y, uv.y, Tolerance, point.ToString());
            }

            Object.DestroyImmediate(go);
        }
    }
}
