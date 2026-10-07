using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class CanyonFieldTests
    {
        private const float HalfWidth = 10f;
        private const float Edge = 8f;
        private const float Entry = 20f;

        // A straight canyon running north from a mouth at z = 200: the chasm between the lip (z 210) and the
        // landing (z 232), two alcoves, the terminus at z 400 and the exit corridor off the landing to the east.
        private static readonly Vector3 Mouth = new Vector3(0f, 0f, 200f);
        private static readonly Vector3 Lip = new Vector3(0f, 2f, 210f);
        private static readonly Vector3 Landing = new Vector3(0f, 2f, 232f);
        private static readonly Vector3 Alcove0 = new Vector3(0f, 4f, 290f);
        private static readonly Vector3 Alcove1 = new Vector3(0f, 6f, 340f);
        private static readonly Vector3 Terminus = new Vector3(0f, 8f, 400f);
        private static readonly Vector3 Exit = new Vector3(60f, 0f, 232f);

        private static CanyonField Build(bool withAlcoves = true)
        {
            var anchors = new FakeWorldAnchors();
            anchors.Add(WorldAnchorIds.CanyonMouth, Mouth);
            anchors.Add(WorldAnchorIds.CanyonLip, Lip);
            anchors.Add(WorldAnchorIds.CanyonLanding, Landing);
            if (withAlcoves)
            {
                anchors.Add(WorldAnchorIds.CanyonAlcovePrefix + 0, Alcove0);
                anchors.Add(WorldAnchorIds.CanyonAlcovePrefix + 1, Alcove1);
            }

            anchors.Add(WorldAnchorIds.CanyonTerminus, Terminus);
            anchors.Add(WorldAnchorIds.CanyonExit, Exit);
            anchors.Add(WorldAnchorIds.CanyonLedge, new Vector3(-30f, 20f, 260f));
            CanyonField field = CanyonField.FromAnchors(anchors, out string problem);
            Assert.IsNull(problem);
            Assert.IsNotNull(field);
            return field;
        }

        private static float Inside(CanyonField field, Vector3 position)
        {
            return field.Inside(position, HalfWidth, Edge, Entry);
        }

        [Test]
        public void TheBasin_IsOutside_AndDeepInIsFullyInside()
        {
            CanyonField field = Build();
            Assert.AreEqual(0f, Inside(field, Vector3.zero), "at the base");
            Assert.AreEqual(0f, Inside(field, new Vector3(0f, 0f, 150f)), "on the basin floor before the mouth");
            Assert.AreEqual(0f, Inside(field, Mouth), "the mouth is where it starts");
            Assert.AreEqual(1f, Inside(field, new Vector3(3f, 5f, 300f)), "deep in, off the centre line");
            Assert.AreEqual(1f, Inside(field, Terminus));
        }

        [Test]
        public void DrivingIn_RampsUpSmoothly_OverTheEntry()
        {
            CanyonField field = Build();
            float previous = 0f;
            for (float z = 200f; z <= 240f; z += 1f)
            {
                float inside = Inside(field, new Vector3(0f, 0f, z));
                Assert.GreaterOrEqual(inside, previous - 1e-6f, $"z {z}");
                Assert.LessOrEqual(inside - previous, 0.15f, $"no step at z {z}");
                previous = inside;
            }

            Assert.AreEqual(1f, previous);
            Assert.AreEqual(0.5f, Inside(field, new Vector3(0f, 0f, 210f)), 1e-4f, "half way up the entry");
        }

        [Test]
        public void TheWalls_FadeOverTheEdge()
        {
            CanyonField field = Build();
            Assert.AreEqual(1f, Inside(field, new Vector3(HalfWidth, 0f, 300f)));
            Assert.AreEqual(0.5f, Inside(field, new Vector3(HalfWidth + Edge * 0.5f, 0f, 300f)), 1e-4f);
            Assert.AreEqual(0f, Inside(field, new Vector3(HalfWidth + Edge, 0f, 300f)));
        }

        [Test]
        public void LeavingByTheExit_FadesOutLikeTheMouth()
        {
            CanyonField field = Build();
            Assert.AreEqual(1f, Inside(field, new Vector3(20f, 2f, 232f)), "on the apron's side corridor");
            Assert.AreEqual(0f, Inside(field, Exit), "the exit opens onto the basin");
            Assert.AreEqual(0.5f, Inside(field, new Vector3(50f, 0f, 232f)), 1e-4f);
        }

        [Test]
        public void AMidLeap_StaysInside_AndOutOfTheTrough()
        {
            CanyonField field = Build();
            var midLeap = new Vector3(0f, 9f, 221f);
            Assert.Greater(Inside(field, midLeap), 0.5f);
            Assert.AreEqual(0f, field.Trough(midLeap, HalfWidth, Edge, 2f, 6f));
            Assert.AreEqual(0f, field.Trough(Lip, HalfWidth, Edge, 2f, 6f), "the lip is the rim");
        }

        [Test]
        public void TheTrough_DeepensBelowTheLipToLandingLine()
        {
            CanyonField field = Build();
            Assert.AreEqual(0f, field.Trough(new Vector3(0f, 1f, 221f), HalfWidth, Edge, 2f, 6f), "a metre down");
            Assert.AreEqual(0.5f, field.Trough(new Vector3(0f, -3f, 221f), HalfWidth, Edge, 2f, 6f), 1e-4f);
            Assert.AreEqual(1f, field.Trough(new Vector3(0f, -12f, 221f), HalfWidth, Edge, 2f, 6f), "the floor");
            Assert.AreEqual(0f, field.Trough(new Vector3(0f, -12f, 300f), HalfWidth, Edge, 2f, 6f),
                "only the chasm is a trough");
        }

        [Test]
        public void WithoutAlcoves_TheCorridorRunsStraightToTheTerminus()
        {
            CanyonField field = Build(false);
            Assert.AreEqual(1f, Inside(field, new Vector3(0f, 0f, 330f)));
        }

        [Test]
        public void AMissingAnchor_IsNamed()
        {
            var anchors = new FakeWorldAnchors();
            anchors.Add(WorldAnchorIds.CanyonMouth, Mouth);
            anchors.Add(WorldAnchorIds.CanyonLip, Lip);
            anchors.Add(WorldAnchorIds.CanyonLanding, Landing);
            anchors.Add(WorldAnchorIds.CanyonExit, Exit);
            Assert.IsNull(CanyonField.FromAnchors(anchors, out string problem));
            StringAssert.Contains(WorldAnchorIds.CanyonTerminus, problem);
        }

        [Test]
        public void TheCentreAndExtent_CoverEveryAnchor()
        {
            CanyonField field = Build();
            foreach (Vector3 anchor in new[] { Mouth, Lip, Landing, Alcove0, Alcove1, Terminus, Exit })
            {
                Assert.LessOrEqual(Vector3.Distance(field.Centre, anchor), field.Extent + 1e-3f);
            }
        }

        private sealed class FakeWorldAnchors : IWorldAnchors
        {
            private readonly List<WorldAnchor> _anchors = new List<WorldAnchor>();

            public int Count => _anchors.Count;

            public WorldAnchor Get(int index)
            {
                return _anchors[index];
            }

            public bool TryGet(string id, out WorldAnchor anchor)
            {
                foreach (WorldAnchor candidate in _anchors)
                {
                    if (candidate.Id == id)
                    {
                        anchor = candidate;
                        return true;
                    }
                }

                anchor = default;
                return false;
            }

            public void Add(string id, Vector3 position)
            {
                _anchors.Add(new WorldAnchor(id, position, Vector3.forward, 6f));
            }
        }
    }
}
