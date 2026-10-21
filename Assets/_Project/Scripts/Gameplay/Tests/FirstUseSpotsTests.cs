using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// A first-use chip's place is off screen when it is behind the camera or outside the view's margin, so the chip
    /// stands on 07 instead (the Build key at the apron behind the camera, after the lander's scan).
    /// </summary>
    public sealed class FirstUseSpotsTests
    {
        private const float Margin = 0.08f;

        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _camera = new GameObject("View").AddComponent<Camera>();
            _camera.fieldOfView = 55f;
            _camera.aspect = 16f / 9f;
            _camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void APlaceAhead_IsOnScreen()
        {
            Assert.IsFalse(FirstUseSpots.OffScreen(_camera, new Vector3(0f, 0f, 10f), Margin));
        }

        [Test]
        public void APlaceBehindTheCamera_IsOffScreen()
        {
            Assert.IsTrue(FirstUseSpots.OffScreen(_camera, new Vector3(0f, 0f, -10f), Margin));
        }

        [Test]
        public void APlaceBesideTheView_OrInItsMargin_IsOffScreen()
        {
            Assert.IsTrue(FirstUseSpots.OffScreen(_camera, new Vector3(30f, 0f, 10f), Margin), "far to the side");
            float edge = 10f * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.97f;
            Assert.IsTrue(FirstUseSpots.OffScreen(_camera, new Vector3(0f, edge, 10f), Margin),
                "at the top edge, inside the margin");
        }
    }
}
