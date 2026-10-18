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

        // Bootprint trail A starts on the apron's edge (within this many metres of its radius) and ends within this
        // many metres of the front door between the flag and the retroreflector.
        private const float ApronEdge = 2f;
        private const float FrontDoorReach = 6f;

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
        public void TheBootprintTrail_SkirtsTheLander_FromTheApronToTheFrontDoor()
        {
            var landmarks = new LandmarkSettings();
            GroundShaping shaping = _surface.Shaping;
            Vector2[] trail = landmarks.BootprintTrail;
            float fromApron = Vector2.Distance(trail[0], shaping.Apron.Center);
            Assert.AreEqual(shaping.Apron.Radius, fromApron, ApronEdge, "the trail starts at the apron's edge");

            for (int i = 1; i < trail.Length; i++)
            {
                Vector2 leg = trail[i] - trail[i - 1];
                float along = Mathf.Clamp01(Vector2.Dot(shaping.Lander.Center - trail[i - 1], leg) / leg.sqrMagnitude);
                float off = Vector2.Distance(shaping.Lander.Center, trail[i - 1] + leg * along);
                Assert.Greater(off, shaping.Lander.Radius, $"leg {i} keeps off the lander's footprint");
            }

            Vector2 ruins = shaping.Ruins.Center;
            Vector2 flag = ruins + Trace(landmarks, "Prop_MissionFlag");
            Vector2 reflector = ruins + Trace(landmarks, "Prop_Retroreflector");
            Vector2 door = (flag + reflector) * 0.5f;
            Assert.Less(Vector2.Distance(trail[trail.Length - 1], door), FrontDoorReach,
                "the trail ends between the flag and the retroreflector");
        }

        private static Vector2 Trace(LandmarkSettings landmarks, string id)
        {
            foreach (PropPlacement placement in landmarks.CrewTraces)
            {
                if (placement.Id == id)
                {
                    return placement.Offset;
                }
            }

            Assert.Fail($"no crew trace {id}");
            return Vector2.zero;
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
