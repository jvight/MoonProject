using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Art.Tests
{
    public sealed class PlaceTests
    {
        [TestCase(0f, 0f, 0f)]
        [TestCase(90f, 0f, 0f)]
        [TestCase(0f, 0f, -90f)]
        [TestCase(30f, 45f, 60f)]
        [TestCase(-120f, 200f, 15f)]
        public void Rotation_MatchesUnityEuler(float x, float y, float z)
        {
            var euler = new Vector3(x, y, z);

            Quaternion expected = Quaternion.Euler(euler);
            Quaternion actual = Place.Rotation(euler);

            Assert.Greater(Mathf.Abs(Quaternion.Dot(expected, actual)), 0.99999f);
        }

        [Test]
        public void AxisPresets_TurnTheUpAxis()
        {
            AssertDirection(Vector3.right, Place.Rotation(Place.AlongX) * Vector3.up);
            AssertDirection(Vector3.forward, Place.Rotation(Place.AlongZ) * Vector3.up);
        }

        [Test]
        public void At_ComposesTranslationRotationAndScale()
        {
            Matrix4x4 m = Place.At(new Vector3(1f, 2f, 3f), new Vector3(0f, 90f, 0f), new Vector3(2f, 1f, 1f));

            AssertDirection(new Vector3(1f, 2f, 1f), m.MultiplyPoint3x4(Vector3.right));
        }

        private static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-5f);
            Assert.AreEqual(expected.y, actual.y, 1e-5f);
            Assert.AreEqual(expected.z, actual.z, 1e-5f);
        }
    }
}
