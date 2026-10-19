using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The databank's story scan hosts (docs/design/text/databank-entries.md): Kestrel-3's hull panel, 06 at micro.06
    /// and 01 at the foot of the Peak road, each on level drivable ground and in plain sight from the drive up to it,
    /// with the rockfall that stopped 01 ahead of it on the road.
    /// </summary>
    public sealed class StoryHostTests
    {
        // The ground under a host leans no more than this, degrees.
        private const float MaxLean = 6f;

        // A player driving up sees the host from this far back toward home, at the chase camera's height, metres; the
        // sightline ends this high above the host's ground.
        private const float ApproachDistance = 30f;
        private const float EyeHeight = 2f;
        private const float TargetHeight = 1f;
        private const float SightStep = 0.5f;

        private SurfaceSettings _surfaceSettings;
        private LandmarkSettings _landmarks;
        private MoonSurface _surface;
        private WorldAnchors _anchors;

        [SetUp]
        public void SetUp()
        {
            _surfaceSettings = new SurfaceSettings();
            _landmarks = new LandmarkSettings();
            _surface = new MoonSurface(_surfaceSettings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, _surfaceSettings.Canyon, new RelaySettings());
        }

        [Test]
        public void ThePeakRoad_LeavesTheBasinOnThePeaksBearing()
        {
            Vector2 foot = _surface.PeakRoadFoot;
            var summit = new Vector2(_surface.PeakSummit.x, _surface.PeakSummit.z);
            Assert.Less(Vector2.Angle(foot, summit), 1f, "the road's foot lies on the line toward The Peak");
            Assert.IsTrue(_surface.IsDrivable(foot.x, foot.y), "on the drivable floor");
            Assert.AreEqual(0f, _surface.Sample(foot.x, foot.y).CraterBowl, "not in a crater");
        }

        [Test]
        public void EveryHost_StandsOnLevelGround_InSightFromTheDriveUp()
        {
            foreach (PropPlacement host in new[] { _landmarks.KestrelPanel, _landmarks.Elder06, _landmarks.Elder01 })
            {
                Vector2 spot = Spot(host);
                float lean = Vector3.Angle(Vector3.up, _surface.SampleNormal(spot.x, spot.y));
                Assert.Less(lean, MaxLean, $"{host.Id} at {spot} stands on level ground");

                Vector2 back = spot - spot.normalized * ApproachDistance;
                var eye = new Vector3(back.x, _surface.SampleHeight(back.x, back.y) + EyeHeight, back.y);
                var target = new Vector3(spot.x, _surface.SampleHeight(spot.x, spot.y) + TargetHeight, spot.y);
                float run = Vector3.Distance(eye, target);
                for (float d = SightStep; d < run; d += SightStep)
                {
                    Vector3 point = Vector3.Lerp(eye, target, d / run);
                    Assert.Greater(point.y, _surface.SampleHeight(point.x, point.z),
                        $"the ground hides {host.Id} from the drive up, {d:F1} m along the sightline");
                }
            }
        }

        [Test]
        public void TheRockfall_LiesAhead_Of01_UpThePeakRoad()
        {
            Vector2 road = _surface.PeakRoadFoot.normalized;
            Assert.Greater(_landmarks.Rockfall.Length, 0, "a rockfall stopped 01");
            foreach (PropPlacement rock in _landmarks.Rockfall)
            {
                Assert.AreEqual(WorldAnchors.PeakRoad, rock.Anchor, $"{rock.Id} lies on the Peak road");
                Assert.Greater(Vector2.Dot(rock.Offset, road), 0f, $"{rock.Id} lies ahead of 01, up the road");
            }
        }

        private Vector2 Spot(PropPlacement placement)
        {
            Assert.IsTrue(_anchors.TryGet(placement.Anchor, out WorldAnchor anchor), $"anchor {placement.Anchor}");
            return new Vector2(anchor.Position.x, anchor.Position.z) + placement.Offset;
        }
    }
}
