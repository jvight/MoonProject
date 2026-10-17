using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// M3-18 V4's smallest scale of ground: 07's worn tracks are two ruts that follow the ground and fade in at their
    /// ends, the landing-blast scar is one streak per setting, and the bootprints are one quad each.
    /// </summary>
    public sealed class GroundMarksTests
    {
        private const float Ground = 3f;
        private const float Tolerance = 1e-4f;

        private GroundMarkSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new GroundMarkSettings();
        }

        [Test]
        public void Track_IsTwoRuts_OnTheGround_FadingInAtItsEnds()
        {
            var marks = new GroundMarkMesh();
            GroundMarks.AddTrack(marks, Vector2.zero, new Vector2(0f, 60f), _settings, 1u, (x, z) => Ground);
            Mesh mesh = marks.ToMesh("Track");
            int steps = Mathf.CeilToInt(60f / _settings.Step);
            Assert.AreEqual(2 * 2 * (steps + 1), mesh.vertexCount, "two ruts, two edges per point");
            foreach (Vector3 vertex in mesh.vertices)
            {
                Assert.AreEqual(Ground + _settings.Lift, vertex.y, Tolerance, "the ruts lie on the ground");
            }

            Color32[] colors = mesh.colors32;
            Assert.AreEqual(0, colors[0].a, "a track fades in from nothing");
            Assert.AreEqual(0, colors[2 * steps].a, "and out again");
        }
    }
}
