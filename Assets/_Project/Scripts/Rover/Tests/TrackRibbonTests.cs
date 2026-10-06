using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class TrackRibbonTests
    {
        private const float Segment = 0.3f;
        private const float HalfWidth = 0.1f;
        private const float Lift = 0.02f;
        private const float Lifetime = 10f;

        private TrackRibbon _ribbon;
        private Vector3[] _vertices;
        private Color32[] _colors;
        private Vector2[] _uvs;
        private int[] _indices;

        [SetUp]
        public void SetUp()
        {
            Make(64);
        }

        private void Make(int capacity)
        {
            _ribbon = new TrackRibbon(capacity);
            _vertices = new Vector3[_ribbon.VertexCapacity];
            _colors = new Color32[_ribbon.VertexCapacity];
            _uvs = new Vector2[_ribbon.VertexCapacity];
            _indices = new int[_ribbon.IndexCapacity];
        }

        private void Build(float time, out int vertices, out int indices)
        {
            _ribbon.Build(Matrix4x4.identity, time, HalfWidth, Lift, 1f, Lifetime * 0.5f, Lifetime, _vertices,
                _colors, _uvs, _indices, out vertices, out indices);
        }

        private void Drive(Vector3 from, Vector3 direction, float distance, Vector3 normal, float time = 0f)
        {
            for (float d = 0f; d <= distance + 1e-4f; d += 0.1f)
            {
                _ribbon.Sample(from + direction * d, normal, time, Segment);
            }
        }

        [Test]
        public void StraightRun_MakesConnectedQuads()
        {
            Drive(Vector3.zero, Vector3.forward, 3f, Vector3.up);
            Build(0f, out int vertices, out int indices);
            Assert.GreaterOrEqual(_ribbon.Count, 9);
            Assert.AreEqual(vertices / 2 - 1, indices / 6, "Every pair after the first connects to its predecessor.");
        }

        [Test]
        public void RibbonLiesFlatOnTheGround_EvenOnASlope()
        {
            Vector3 normal = Quaternion.Euler(0f, 0f, 25f) * Vector3.up;
            Vector3 direction = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
            Drive(Vector3.zero, direction, 3f, normal);
            Build(0f, out int vertices, out _);
            for (int i = 0; i < vertices; i++)
            {
                Assert.AreEqual(Lift, Vector3.Dot(_vertices[i], normal), 1e-4f, $"vertex {i} is not on the ground");
            }
        }

        [Test]
        public void CurvingRun_NeverTwists()
        {
            for (int step = 0; step <= 60; step++)
            {
                float angle = step * 4f;
                Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.left * 3f;
                _ribbon.Sample(position, Vector3.up, 0f, Segment);
            }

            Build(0f, out _, out int indices);
            for (int t = 0; t < indices; t += 6)
            {
                Vector3 previousAcross = _vertices[_indices[t + 5]] - _vertices[_indices[t]];
                Vector3 across = _vertices[_indices[t + 2]] - _vertices[_indices[t + 1]];
                Assert.Greater(Vector3.Dot(previousAcross, across), 0f, $"quad {t / 6} folds over");
            }
        }

        [Test]
        public void Reversing_StartsANewStripInsteadOfFolding()
        {
            Drive(Vector3.zero, Vector3.forward, 2f, Vector3.up);
            Drive(Vector3.forward * 2f, Vector3.back, 1.5f, Vector3.up);
            Build(0f, out _, out int indices);
            for (int t = 0; t < indices; t += 6)
            {
                Vector3 previousAcross = _vertices[_indices[t + 5]] - _vertices[_indices[t]];
                Vector3 across = _vertices[_indices[t + 2]] - _vertices[_indices[t + 1]];
                Assert.Greater(Vector3.Dot(previousAcross, across), 0f, $"quad {t / 6} folds over");
            }
        }

        [Test]
        public void Break_LeavesAGap()
        {
            Drive(Vector3.zero, Vector3.forward, 1.5f, Vector3.up);
            _ribbon.Break();
            Drive(Vector3.forward * 4f, Vector3.forward, 1.5f, Vector3.up);
            Build(0f, out int vertices, out int indices);
            Assert.AreEqual(vertices / 2 - 2, indices / 6, "Two strips, no quad bridging the jump.");
        }

        [Test]
        public void OldTrack_FadesThenExpires()
        {
            Drive(Vector3.zero, Vector3.forward, 1.5f, Vector3.up, 0f);
            _ribbon.Break();
            Build(1f, out _, out _);
            Assert.AreEqual(255, _colors[0].a);
            Build(Lifetime * 0.8f, out _, out _);
            Assert.That(_colors[0].a, Is.InRange(1, 254));

            _ribbon.Expire(Lifetime + 1f, Lifetime);
            Assert.AreEqual(0, _ribbon.Count);
            Build(Lifetime + 1f, out int vertices, out _);
            Assert.AreEqual(0, vertices);
        }

        [Test]
        public void LiveHead_ReachesTheWheel()
        {
            _ribbon.Sample(Vector3.zero, Vector3.up, 0f, Segment);
            _ribbon.Sample(Vector3.forward * 0.2f, Vector3.up, 0f, Segment);
            Build(0f, out int vertices, out int indices);
            Assert.AreEqual(4, vertices);
            Assert.AreEqual(6, indices);
            Assert.AreEqual(0.2f, _vertices[3].z, 1e-4f);
            Assert.AreNotEqual(_vertices[0], _vertices[1], "The first pair borrows the head's side.");
        }

        [Test]
        public void FullBuffer_RecyclesTheOldestPoints()
        {
            Make(8);
            Drive(Vector3.zero, Vector3.forward, 20f, Vector3.up);
            Assert.AreEqual(8, _ribbon.Count);
            Build(0f, out int vertices, out int indices);
            Assert.LessOrEqual(vertices, _ribbon.VertexCapacity);
            Assert.LessOrEqual(indices, _ribbon.IndexCapacity);
            Assert.Greater(_vertices[0].z, 15f);
        }
    }
}
