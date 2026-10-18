using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The basin's story props against the sky that makes them lures (docs/design/basin-dressing.md §2): the Scout on
    /// its crater's wall, clear of rocks and of the first drive's lane, and lit by Earth on the first night on the side
    /// the base sees. The Sun times are the opening's real holds (<see cref="OpeningSkyPacing"/>).
    /// </summary>
    public sealed class BasinDressingTests
    {
        // Art's Prop_OldLander contract (tools/art/qa/contracts/prop_old_lander.json): 3.07 m from footpads to dish.
        private const float ScoutHeight = 3.07f;

        // The plan's rule: nothing new within 45 m of the wake-to-lander lane in the eclipse (§1 rule 2).
        private const float LaneKeepOut = 45f;

        // On the first night Earth must clear the rim over the Scout and light the side the base sees (within this
        // many degrees of the line of sight), so its pale shape reads from the apron (§2, the night lure).
        private const float FrontLit = 45f;
        private const float HorizonStep = 25f;
        private const float HorizonReach = 3000f;

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
        public void OnTheFirstNight_EarthLightsTheSideTheBaseSees()
        {
            var sky = new SkySettings();
            GroundShaping shaping = _surface.Shaping;
            Vector2 spot = shaping.ScoutSpot;
            Vector2 toEarth = MoonSurface.BearingToDirection(sky.EarthBearing);
            Vector2 toApron = shaping.Apron.Center - spot;
            float offSight = Vector2.Angle(toEarth, toApron);

            float base0 = _surface.SampleHeight(spot.x, spot.y) + ScoutHeight;
            float horizon = -90f;
            for (float d = HorizonStep; d <= HorizonReach; d += HorizonStep)
            {
                Vector2 at = spot + toEarth * d;
                horizon = Mathf.Max(horizon, Mathf.Atan2(_surface.SampleHeight(at.x, at.y) - base0, d) * Mathf.Rad2Deg);
            }

            TestContext.WriteLine($"first night: Earth at {sky.EarthBearing:F0} deg, {sky.EarthElevation:F0} deg up, " +
                                  $"over a {horizon:F1} deg horizon from the Scout's dish; {offSight:F1} deg off the " +
                                  "line to the apron");
            Assert.Greater(sky.EarthElevation, horizon, "Earth stands clear of the rim over the Scout");
            Assert.Less(offSight, FrontLit, "Earth lights the side of the Scout the base sees");
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
