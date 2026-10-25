using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The talus at the walls' feet (<see cref="RimTalusPlanner"/>): big blocks every so often at the foot, the rest
    /// fanning over the first metres of floor; none in the anchors' clearings, the rille's pass or by the Peak road;
    /// only the smallest stones in the canyon and the slot, never in the slot's drive line; the same plan every time.
    /// </summary>
    public sealed class RimTalusPlannerTests
    {
        // Art's kit (tools/art/qa/contracts/rim_talus_*.json): each piece's horizontal extent and height, metres.
        private static readonly Vector2[] Pieces =
        {
            new Vector2(0.56f, 0.34f), new Vector2(0.8f, 0.46f), new Vector2(1.1f, 0.62f), new Vector2(1.64f, 0.92f),
            new Vector2(2.1f, 1.05f), new Vector2(2.68f, 1.24f), new Vector2(3.2f, 1.5f), new Vector2(3.86f, 1.76f),
        };

        // A piece stands off its own bearing's foot by its band, give or take the sideways spread along the curve.
        private const float FootSlack = 2.5f;

        // The slot's drive line: no stone nearer its centre line than this share of its half width.
        private const float DriveLineShare = 1f / 3f;

        private MoonSurface _surface;
        private RimTalusSettings _talus;
        private WorldAnchors _anchors;
        private List<ScatterInstance> _plan;

        [SetUp]
        public void SetUp()
        {
            var settings = new SurfaceSettings();
            _surface = new MoonSurface(settings, WorldSettings.DefaultSeed);
            _talus = new ScatterSettings().RimTalus;
            _anchors = new WorldAnchors(_surface, settings.Canyon, new RelaySettings());
            _plan = new RimTalusPlanner(_surface, _talus, _anchors, Pieces).Plan();
        }

        [Test]
        public void TheBigBlocks_LieAtTheWallsFeet_EverySoOften()
        {
            var planner = new RimTalusPlanner(_surface, _talus, _anchors, Pieces);
            int big = 0;
            foreach (ScatterInstance piece in _plan)
            {
                if (piece.Variant < _talus.FirstBig)
                {
                    continue;
                }

                big++;
                var spot = new Vector2(piece.Position.x, piece.Position.z);
                float outFromFoot = planner.FootDistance(spot.normalized) - spot.magnitude;
                Assert.That(outFromFoot, Is.InRange(_talus.AnchorBand.x - FootSlack, _talus.AnchorBand.y + FootSlack),
                    $"a big block at {spot} lies at the wall's foot");
            }

            float rim = 2f * Mathf.PI * _surface.FloorRadius;
            Assert.That(big, Is.InRange(rim / _talus.AnchorSpacing.y / 2f, rim / _talus.AnchorSpacing.x),
                "a big block every 15-25 m round the rim, less the ways out and the canyon");
        }

        [Test]
        public void TheTalus_KeepsClearOfTheAnchorsTheRilleAndThePeakRoad()
        {
            Vector2 rille = _surface.Rille.Direction;
            foreach (ScatterInstance piece in _plan)
            {
                var spot = new Vector2(piece.Position.x, piece.Position.z);
                for (int i = 0; i < _anchors.Count; i++)
                {
                    WorldAnchor anchor = _anchors.Get(i);
                    float reach = anchor.Radius + _talus.AnchorClear;
                    Assert.Greater(Vector2.Distance(spot, new Vector2(anchor.Position.x, anchor.Position.z)), reach,
                        $"talus at {spot} lies in {anchor.Id}'s clearing");
                }

                if (_surface.Canyon.Bounds.Contains(spot))
                {
                    continue;
                }

                bool inRille = Vector2.Dot(spot, rille) > 0f
                    && Mathf.Abs(spot.x * rille.y - spot.y * rille.x) < _talus.ExitClear;
                Assert.IsFalse(inRille, $"talus at {spot} lies in the rille's pass");
                Assert.Greater(Vector2.Distance(spot, _surface.PeakRoadFoot), _talus.ExitClear,
                    $"talus at {spot} lies on the Peak road");
            }
        }

        [Test]
        public void InTheCanyon_OnlyTheSmallestStonesLie_AlongTheWallsNotTheDriveLine()
        {
            int corridor = 0;
            foreach (ScatterInstance piece in _plan)
            {
                var spot = new Vector2(piece.Position.x, piece.Position.z);
                if (!_surface.Canyon.TryFloor(spot.x, spot.y, out CanyonCorridor kind, out float _, out float share))
                {
                    continue;
                }

                corridor++;
                Assert.LessOrEqual(piece.Variant, _talus.CorridorLast, $"only small stones in the canyon at {spot}");
                if (kind == CanyonCorridor.Slot)
                {
                    Assert.Greater(share, DriveLineShare, $"a stone at {spot} lies in the slot's drive line");
                }
            }

            Assert.Greater(corridor, 0, "the canyon and the slot get their stones along the walls");
        }

        [Test]
        public void ThePlan_IsTheSameEveryTime()
        {
            List<ScatterInstance> again = new RimTalusPlanner(_surface, _talus, _anchors, Pieces).Plan();
            Assert.AreEqual(_plan.Count, again.Count);
            for (int i = 0; i < _plan.Count; i++)
            {
                Assert.AreEqual(_plan[i].Position, again[i].Position);
                Assert.AreEqual(_plan[i].Variant, again[i].Variant);
            }
        }
    }
}
