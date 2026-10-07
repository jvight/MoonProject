using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The <see cref="IWorldAnchors"/> contract (docs/features/M3-04): every id is published, each anchor stands on
    /// the surface with a horizontal unit forward, and its radius is flat, drivable, uncluttered ground.
    /// </summary>
    public sealed class WorldAnchorsTests
    {
        private const float FlatSlope = 12f;
        private const float FlatRise = 0.75f;
        private const int RingDirections = 24;
        private const float FacingDot = 0.9f;

        // Bell (M3-05) leans broken against the terminus's back wall: she needs this much flat ground, and the wall
        // must rise at least this high within this far past the radius's edge along Forward.
        private const float TerminusMinRadius = 3.5f;
        private const float BackWallRise = 3f;
        private const float BackWallReach = 2.5f;

        // Measured with the real rover (CanyonSession, RoverTuning defaults): a 3/4-charge leap at cruising speed
        // (5.8 m/s) touches down 22.3 m out and a full charge at top speed (7.7 m/s) 32.9 m out, both on the apron 2 m
        // above the lip; a tap peaks at 0.8 m.
        private const float ThreeQuarterLeap = 22.3f;
        private const float FullLeap = 32.9f;
        private const float HopApex = 1f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private WorldAnchors _anchors;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, _settings.Canyon);
        }

        [Test]
        public void EveryId_IsPublishedOnce()
        {
            var expected = new List<string>
            {
                WorldAnchorIds.CanyonMouth, WorldAnchorIds.CanyonLip, WorldAnchorIds.CanyonLanding,
                WorldAnchorIds.CanyonLedge, WorldAnchorIds.CanyonTerminus, WorldAnchorIds.CanyonExit,
            };
            for (int i = 0; i < _settings.Canyon.AlcoveCount; i++)
            {
                expected.Add(WorldAnchorIds.CanyonAlcovePrefix + i);
            }

            Assert.AreEqual(expected.Count, _anchors.Count);
            var seen = new HashSet<string>();
            for (int i = 0; i < _anchors.Count; i++)
            {
                Assert.IsTrue(seen.Add(_anchors.Get(i).Id), $"duplicate id {_anchors.Get(i).Id}");
            }

            foreach (string id in expected)
            {
                Assert.IsTrue(_anchors.TryGet(id, out WorldAnchor anchor), $"missing {id}");
                Assert.AreEqual(id, anchor.Id);
            }

            Assert.IsFalse(_anchors.TryGet("canyon.nowhere", out WorldAnchor _));
            Assert.IsFalse(_anchors.TryGet(null, out WorldAnchor _));
        }

        [Test]
        public void Anchors_StandOnTheSurface_FacingAHorizontalUnitDirection()
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor anchor = _anchors.Get(i);
                Vector3 p = anchor.Position;
                Assert.AreEqual(_surface.SampleHeight(p.x, p.z), p.y, 1e-4f, $"{anchor.Id} is off the surface");
                Assert.AreEqual(0f, anchor.Forward.y, $"{anchor.Id} forward is not horizontal");
                Assert.AreEqual(1f, anchor.Forward.magnitude, 1e-4f, $"{anchor.Id} forward is not a unit vector");
                Assert.Greater(anchor.Radius, 1f, $"{anchor.Id} has no room");
                Assert.IsFalse(_surface.PlayableArea.Contains(new Vector2(p.x, p.z)),
                    $"{anchor.Id} lies inside PlayableArea, which the canyon must leave alone");
            }
        }

        [Test]
        public void EveryRadius_IsFlatAndDrivable()
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor anchor = _anchors.Get(i);
                foreach (float fraction in new[] { 0f, 0.5f, 1f })
                {
                    for (int k = 0; k < RingDirections; k++)
                    {
                        float angle = k * Mathf.PI * 2f / RingDirections;
                        float x = anchor.Position.x + Mathf.Cos(angle) * anchor.Radius * fraction;
                        float z = anchor.Position.z + Mathf.Sin(angle) * anchor.Radius * fraction;
                        string where = $"{anchor.Id} at ({x:F1}, {z:F1})";
                        Assert.IsTrue(_surface.IsDrivable(x, z), $"{where} is not drivable");
                        float slope = Vector3.Angle(Vector3.up, _surface.SampleNormal(x, z));
                        Assert.LessOrEqual(slope, FlatSlope, $"{where} is not flat");
                        Assert.LessOrEqual(Mathf.Abs(_surface.SampleHeight(x, z) - anchor.Position.y), FlatRise,
                            $"{where} is not level with the anchor");
                    }
                }
            }
        }

        [Test]
        public void EveryRadius_IsClearOfScatter()
        {
            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), _anchors).Plan();
            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor anchor = _anchors.Get(i);
                var centre = new Vector2(anchor.Position.x, anchor.Position.z);
                foreach (ScatterInstance rock in plan)
                {
                    float distance = Vector2.Distance(centre, new Vector2(rock.Position.x, rock.Position.z));
                    Assert.Greater(distance, anchor.Radius, $"{rock.Kind} inside {anchor.Id} at {rock.Position}");
                }
            }
        }

        [Test]
        public void Forward_PointsTheWay07TravelsArrivingIntoEachSpace()
        {
            Canyon canyon = _surface.Canyon;
            CanyonPath main = canyon.MainPath;
            foreach (string id in new[] { WorldAnchorIds.CanyonMouth, WorldAnchorIds.CanyonLip,
                         WorldAnchorIds.CanyonLanding })
            {
                WorldAnchor anchor = Anchor(id);
                Assert.Greater(Vector3.Dot(anchor.Forward, Along(main, anchor.Position)), FacingDot,
                    $"{id} should face on into the canyon");
            }

            var sideSpaces = new List<string> { WorldAnchorIds.CanyonLedge };
            for (int i = 0; i < _settings.Canyon.AlcoveCount; i++)
            {
                sideSpaces.Add(WorldAnchorIds.CanyonAlcovePrefix + i);
            }

            foreach (string id in sideSpaces)
            {
                WorldAnchor anchor = Anchor(id);
                Assert.IsTrue(main.TryProject(anchor.Position.x, anchor.Position.z, out float arc, out float _));
                Vector2 centre = main.PointAt(arc);
                Vector3 fromCorridor = anchor.Position - new Vector3(centre.x, anchor.Position.y, centre.y);
                Assert.Greater(Vector3.Dot(anchor.Forward, fromCorridor.normalized), FacingDot,
                    $"{id} should face in from the corridor");
            }

            Vector2 end = main.TangentAt(main.EndArc);
            Assert.Greater(Vector3.Dot(Anchor(WorldAnchorIds.CanyonTerminus).Forward, new Vector3(end.x, 0f, end.y)),
                FacingDot, "the terminus should face on into the chamber");
        }

        [Test]
        public void Terminus_HasRoomForBell_InFrontOfASheerBackWall()
        {
            WorldAnchor terminus = Anchor(WorldAnchorIds.CanyonTerminus);
            Assert.GreaterOrEqual(terminus.Radius, TerminusMinRadius);
            float rise = float.MinValue;
            for (float d = terminus.Radius; d <= terminus.Radius + BackWallReach; d += 0.1f)
            {
                Vector3 p = terminus.Position + terminus.Forward * d;
                rise = Mathf.Max(rise, _surface.SampleHeight(p.x, p.z) - terminus.Position.y);
            }

            Assert.Greater(rise, BackWallRise, "a wall should stand just behind the terminus");
        }

        [Test]
        public void Landing_IsAcrossTheGap_InReachOfAChargedLeap_AndOutOfReachOfAHop()
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing));
            Vector3 across = landing.Position - lip.Position;
            float run = new Vector2(across.x, across.z).magnitude;
            Assert.Greater(Vector3.Dot(across.normalized, lip.Forward), 0.95f, "the lip should face the landing");
            Assert.Greater(Vector3.Dot(landing.Forward, lip.Forward), 0.9f,
                "the landing should face on into the canyon");

            Canyon canyon = _surface.Canyon;
            float apronStart = canyon.GapToFarFace;
            Assert.That(apronStart, Is.InRange(18f, 22f), "lip to far face (spec: ~18-22 m)");
            Assert.LessOrEqual(apronStart, ThreeQuarterLeap, "a 3/4-charge leap at cruise must clear the gap");
            Assert.That(FullLeap, Is.InRange(run - landing.Radius, run + landing.Radius),
                "a full-charge leap at top speed should come down on the landing");
            Assert.Greater(run - landing.Radius, apronStart + 2f * Canyon.SlabHalfDepth,
                "the landing radius starts beyond the far face's slabs");
            Assert.Greater(landing.Position.y - lip.Position.y, HopApex,
                "the landing must sit higher than a normal hop can reach");
        }

        [Test]
        public void Exit_IsTheTopOfTheStep_FacingOut_AboveItsOpenFoot()
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonExit, out WorldAnchor exit));
            Vector3 foot = _anchors.ExitFoot;
            Assert.AreEqual(_surface.SampleHeight(foot.x, foot.z), foot.y, 1e-4f);
            Assert.IsTrue(_surface.IsDrivable(foot.x, foot.z), "the exit's foot must be drivable basin ground");
            Assert.IsFalse(_surface.PlayableArea.Contains(new Vector2(foot.x, foot.z)));

            // One step (at most 2.5 m, see CanyonTests) plus the shelf's gentle rise over the anchor's setback.
            Vector3 down = foot - exit.Position;
            Assert.That(exit.Position.y - foot.y, Is.InRange(1f, 2.6f), "the exit's top is one step above its foot");
            Assert.Greater(Vector3.Dot(new Vector3(down.x, 0f, down.z).normalized, exit.Forward), 0.95f,
                "the exit should face down the step toward its foot");
        }

        private WorldAnchor Anchor(string id)
        {
            Assert.IsTrue(_anchors.TryGet(id, out WorldAnchor anchor), $"missing {id}");
            return anchor;
        }

        private static Vector3 Along(CanyonPath path, Vector3 position)
        {
            Assert.IsTrue(path.TryProject(position.x, position.z, out float arc, out float _));
            Vector2 tangent = path.TangentAt(arc);
            return new Vector3(tangent.x, 0f, tangent.y);
        }
    }
}
