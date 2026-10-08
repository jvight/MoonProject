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

        // The relay network (docs/features/M3-06): four masts, each with a 3 m pad facing home, reach circles of
        // ~110 m per mast and ~120 m for home's tower, linked home -> relay.0 -> relay.2 -> relay.3 and home ->
        // relay.1. A mast is at least this tall, so its top must stand in the base's line of sight.
        private const int RelayCount = 4;
        private const float RelayPad = 3f;
        private const float HomeReach = 120f;
        private const float MastReach = 110f;
        private const float MaxHomeAngle = 3f;
        private const float MastHeight = 8f;
        private const float ChainMargin = 10f;

        // relay.0 is the teaching mast in the spawn first frame (yaw 355, about +-45 degrees, ruling 8): well inside
        // the view, at least 15 degrees from The Peak so the silhouettes read apart. The play ramp at bearing 38
        // fills that slice of the view out to its landing ground, ~130 m; the mast stands just beyond it, on its
        // mound, rather than where a jump comes down.
        private const float SpawnYaw = 355f;
        private const float SpawnHalfView = 40f;
        private const float MinPeakSeparation = 15f;
        private const float MinTeachingDistance = 100f;
        private const float MaxTeachingDistance = 160f;

        // A jump off a play ramp flies straight on along it and comes down within this far past the ramp's foot
        // (a charged leap at top speed), at most this far outside its sides.
        private const float RampFlightReach = 20f;
        private const float RampFlightSpread = 2f;
        private const float BaseEyeHeight = 3f;
        private const float SightTargetClearance = 2f;

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
            _anchors = new WorldAnchors(_surface, _settings.Canyon, new RelaySettings());
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

            for (int i = 0; i < RelayCount; i++)
            {
                expected.Add(WorldAnchorIds.RelayPrefix + i);
            }

            foreach (string site in SalvageSiteTests.Sites)
            {
                expected.Add(WorldAnchorIds.SitePrefix + site);
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
                bool canyon = anchor.Id.StartsWith("canyon.", System.StringComparison.Ordinal);
                Assert.IsTrue(!canyon || !_surface.PlayableArea.Contains(new Vector2(p.x, p.z)),
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

            var sideSpaces = new List<string> { WorldAnchorIds.CanyonLedge, WorldAnchorIds.SitePrefix + "lander" };
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
        public void Relays_HaveAPad_FacingHome()
        {
            for (int i = 0; i < RelayCount; i++)
            {
                WorldAnchor relay = Anchor(WorldAnchorIds.RelayPrefix + i);
                Assert.AreEqual(RelayPad, relay.Radius, 1e-4f, $"{relay.Id} pad radius");
                var home = new Vector3(-relay.Position.x, 0f, -relay.Position.z);
                Assert.LessOrEqual(Vector3.Angle(relay.Forward, home), MaxHomeAngle, $"{relay.Id} does not face home");
            }
        }

        [Test]
        public void Relays_LinkInAChain_BackToHome()
        {
            Vector3 relay0 = Anchor(WorldAnchorIds.RelayPrefix + 0).Position;
            Vector3 relay1 = Anchor(WorldAnchorIds.RelayPrefix + 1).Position;
            Vector3 relay2 = Anchor(WorldAnchorIds.RelayPrefix + 2).Position;
            Vector3 relay3 = Anchor(WorldAnchorIds.RelayPrefix + 3).Position;
            Assert.Less(Flat(relay0), HomeReach + MastReach - ChainMargin, "home to relay.0");
            Assert.Less(Flat(relay1), HomeReach + MastReach - ChainMargin, "home to relay.1");
            Assert.Less(Flat(relay2 - relay0), MastReach * 2f - ChainMargin, "relay.0 to relay.2");
            Assert.Less(Flat(relay3 - relay2), MastReach * 2f - ChainMargin, "relay.2 to relay.3");
        }

        [Test]
        public void TeachingRelay_StandsInTheSpawnView_ClearOfThePeak()
        {
            Vector3 relay = Anchor(WorldAnchorIds.RelayPrefix + 0).Position;
            float bearing = Mathf.Atan2(relay.x, relay.z) * Mathf.Rad2Deg;
            float peak = Mathf.Atan2(_surface.PeakSummit.x, _surface.PeakSummit.z) * Mathf.Rad2Deg;
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(SpawnYaw, bearing)), SpawnHalfView, "outside the spawn view");
            Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(peak, bearing)), MinPeakSeparation,
                "too close to The Peak");
            Assert.That(Flat(relay), Is.InRange(MinTeachingDistance, MaxTeachingDistance), "distance from home");
        }

        [Test]
        public void TeachingRelay_StandsClearOfWhereRampJumpsComeDown()
        {
            WorldAnchor relay = Anchor(WorldAnchorIds.RelayPrefix + 0);
            foreach (Ramp ramp in _surface.Ramps)
            {
                Vector2 offset = new Vector2(relay.Position.x, relay.Position.z) - ramp.Crest;
                float along = Vector2.Dot(offset, ramp.Direction);
                float across = Mathf.Abs(offset.y * ramp.Direction.x - offset.x * ramp.Direction.y);
                float beyondEnds = Mathf.Max(0f, Mathf.Max(along - (ramp.FallLength + RampFlightReach),
                    -ramp.RiseLength - along));
                float beyondSides = Mathf.Max(0f, across - (ramp.HalfWidth + RampFlightSpread));
                Assert.Greater(new Vector2(beyondEnds, beyondSides).magnitude, relay.Radius,
                    $"relay.0's pad lies where a jump off the ramp at {ramp.Crest} comes down");
            }
        }

        [Test]
        public void RelaysOutsideTheCanyon_StandInPlainSightOfTheBase()
        {
            var eye = new Vector3(0f, _surface.SampleHeight(0f, 0f) + BaseEyeHeight, 0f);
            for (int i = 0; i < 3; i++)
            {
                WorldAnchor relay = Anchor(WorldAnchorIds.RelayPrefix + i);
                Vector3 top = relay.Position + Vector3.up * MastHeight;
                float length = Vector3.Distance(eye, top);
                for (float d = 0f; d < length - SightTargetClearance; d += 1f)
                {
                    Vector3 p = Vector3.Lerp(eye, top, d / length);
                    Assert.Greater(p.y, _surface.SampleHeight(p.x, p.z),
                        $"the base cannot see {relay.Id}'s mast: blocked at ({p.x:F0}, {p.z:F0})");
                }
            }
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

        private static float Flat(Vector3 v)
        {
            return new Vector2(v.x, v.z).magnitude;
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
