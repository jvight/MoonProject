using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The basin's story props against the sky that makes them lures (docs/design/basin-dressing.md §2): the Scout on
    /// its crater's wall, clear of rocks and of the first drive's lane, and its long shadow at last light reaching up
    /// the plain toward the base. The Sun times are the opening's real holds (<see cref="OpeningSkyPacing"/>).
    /// </summary>
    public sealed class BasinDressingTests
    {
        // Art's Prop_OldLander contract (tools/art/qa/contracts/prop_old_lander.json): 3.07 m from footpads to dish.
        private const float ScoutHeight = 3.07f;

        // The plan's rules: nothing new within 45 m of the wake-to-lander lane in the eclipse (§1 rule 2), and the
        // shadow "reaches up the plain toward the base" (§2): within this many degrees of the line to the apron.
        private const float LaneKeepOut = 45f;
        private const float ShadowAim = 15f;

        // The shadow must leave the crater and run well out over the plain to be seen from the apron, 120 m off.
        private const float MinShadowReach = 40f;
        private const float ShadowStep = 0.25f;
        private const float MaxShadowSearch = 200f;

        // The crater's inner wall where the Scout came to rest: a soft lander's slope, not a cliff.
        private const float MinWallSlope = 6f;
        private const float MaxWallSlope = 18f;

        // The golden-hour Sun the retroreflector panel is aimed by (§2: ~346 degrees, low NNW).
        private const float GoldenHourBearing = 346f;
        private const float BearingTolerance = 10f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private SunClock _clock;
        private OpeningSkyPacing _pacing;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            var cycle = new SkyCycleSettings();
            _clock = new SunClock(cycle, new SkySettings());
            _pacing = new OpeningSkyPacing(cycle, _clock);
        }

        [Test]
        public void TheScout_RestsOnItsCratersInnerWall_ClearOfRocks()
        {
            GroundShaping shaping = _surface.Shaping;
            Vector2 spot = shaping.ScoutSpot;
            Vector2 centre = shaping.ScoutCraterCenter;
            Assert.Less(Vector2.Distance(spot, centre), shaping.ScoutCraterRadius, "the Scout lies in its crater");
            Assert.Less(_surface.SampleHeight(centre.x, centre.y), _surface.SampleHeight(spot.x, spot.y),
                "the crater's floor lies below the Scout");
            float slope = Vector3.Angle(Vector3.up, _surface.SampleNormal(spot.x, spot.y));
            TestContext.WriteLine($"the Scout stands at ({spot.x:F1}, {spot.y:F1}) on a {slope:F1} deg slope");
            Assert.That(slope, Is.InRange(MinWallSlope, MaxWallSlope), "the wall under the Scout");

            var anchors = new WorldAnchors(_surface, _settings.Canyon, new RelaySettings());
            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), anchors).Plan();
            foreach (ScatterInstance rock in plan)
            {
                float distance = Vector2.Distance(centre, new Vector2(rock.Position.x, rock.Position.z));
                Assert.Greater(distance, shaping.ScoutCraterRadius, $"{rock.Kind} in the Scout's crater");
            }
        }

        [Test]
        public void TheScout_StandsOffTheFirstDrivesLane()
        {
            GroundShaping shaping = _surface.Shaping;
            Vector2 wake = shaping.WakePost;
            Vector2 lane = shaping.Lander.Center - wake;
            Vector2 spot = shaping.ScoutSpot;
            float along = Mathf.Clamp01(Vector2.Dot(spot - wake, lane) / lane.sqrMagnitude);
            float off = Vector2.Distance(spot, wake + lane * along);
            TestContext.WriteLine($"the Scout stands {off:F1} m off the wake-to-lander lane");
            Assert.GreaterOrEqual(off, LaneKeepOut);
        }

        [Test]
        public void AtLastLight_TheScoutsShadow_ReachesUpThePlainTowardTheBase()
        {
            _clock.SetTime(_pacing.LastLightTime);
            Vector3 sun = _clock.SunDirection;
            var away = new Vector2(-sun.x, -sun.z);
            away.Normalize();

            GroundShaping shaping = _surface.Shaping;
            Vector2 spot = shaping.ScoutSpot;
            Vector2 toApron = shaping.Apron.Center - spot;
            float aim = Vector2.Angle(away, toApron);
            float top = _surface.SampleHeight(spot.x, spot.y) + ScoutHeight;
            float drop = Mathf.Tan(_clock.Elevation * Mathf.Deg2Rad);
            float reach = 0f;
            for (float d = ShadowStep; d < MaxShadowSearch; d += ShadowStep)
            {
                Vector2 at = spot + away * d;
                if (_surface.SampleHeight(at.x, at.y) > top - d * drop)
                {
                    break;
                }

                reach = d;
            }

            TestContext.WriteLine($"last light: Sun at {_clock.Bearing:F1} deg, {_clock.Elevation:F2} deg up; the " +
                                  $"shadow runs {reach:F1} m, {aim:F1} deg off the line to the apron " +
                                  $"({toApron.magnitude:F0} m away)");
            Assert.Less(aim, ShadowAim, "the shadow points at the base");
            Assert.Greater(reach, MinShadowReach, "the shadow leaves the crater and runs out over the plain");
        }

        [Test]
        public void AtGoldenHour_TheSunStandsWhereTheReflectorIsAimed()
        {
            _clock.SetTime(_pacing.GoldenHourTime);
            TestContext.WriteLine($"golden hour: Sun at {_clock.Bearing:F1} deg, {_clock.Elevation:F2} deg up");
            Assert.AreEqual(0f, Mathf.DeltaAngle(GoldenHourBearing, _clock.Bearing), BearingTolerance);
        }
    }
}
