using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The rille notch (base-home-and-crater-exits §4): a low pass through the rim on its bearing, the rim untouched
    /// well off it, and from relay.1 a clear line of sight between the notch's walls onto the dark mare.
    /// </summary>
    public sealed class RilleNotchTests
    {
        // relay.1, the west mast (RelaySettings' default), and an eye on its pad.
        private static readonly Vector2 Relay1 = new Vector2(-205f, -75f);
        private const float EyeHeight = 2.5f;
        private const float SightStep = 1f;

        // The sight line ends this far above the mare's ground: a glimpse of the plain, not a graze along it.
        private const float SightClearance = 1f;

        // Well off the pass, the rim stands at least this far above the pass's floor at the crest.
        private const float OffBearing = 40f;
        private const float RimAbovePass = 25f;

        // The lava lip, probed along the pass at the finest mesh cell: steeper than 07 can climb, by a margin.
        private const float LipProbeStep = 2f;
        private const float UnclimbableSlope = 55f;

        // Across the pass at the crest, no step between samples this far apart may exceed this rise (C1, no cliffs).
        private const float AcrossStep = 0.5f;
        private const float MaxRisePerStep = 1f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Rille.Validate(_settings.FloorRadius, _settings.CrestRadius));
        }

        [Test]
        public void AtTheCrest_ThePassIsLow_AndTheRimOffItStaysHigh()
        {
            RilleSettings rille = _settings.Rille;
            Vector2 pass = _surface.Rille.Direction * _settings.CrestRadius;
            Assert.AreEqual(rille.PassHeight, _surface.SampleHeight(pass.x, pass.y), 1.5f,
                "the pass runs low through the crest");

            Vector2 off = MoonSurface.BearingToDirection(rille.Bearing + OffBearing) * _settings.CrestRadius;
            Assert.Greater(_surface.SampleHeight(off.x, off.y), rille.PassHeight + RimAbovePass,
                "the rim well off the pass is untouched");
        }

        [Test]
        public void AcrossThePass_TheGroundRisesSmoothlyIntoItsWalls()
        {
            Vector2 along = _surface.Rille.Direction;
            var side = new Vector2(along.y, -along.x);
            Vector2 centre = along * _settings.CrestRadius;
            float previous = _surface.SampleHeight(centre.x, centre.y);
            for (float lateral = AcrossStep; lateral < _settings.Rille.HalfWidth * 4f; lateral += AcrossStep)
            {
                Vector2 p = centre + side * lateral;
                float height = _surface.SampleHeight(p.x, p.y);
                Assert.Less(Mathf.Abs(height - previous), MaxRisePerStep, $"no cliff in the pass's wall at {lateral} m");
                previous = height;
            }
        }

        [Test]
        public void TheLavaLip_IsFarTooSteepToClimb_SoThePassEndsThere()
        {
            RilleSettings rille = _settings.Rille;
            Vector2 along = _surface.Rille.Direction;
            float foot = _settings.CrestRadius + rille.LipDistance;
            float steepest = 0f;
            for (float d = foot - LipProbeStep; d <= foot + rille.LipRun + LipProbeStep; d += LipProbeStep)
            {
                Vector2 a = along * d;
                Vector2 b = along * (d + LipProbeStep);
                float rise = _surface.SampleHeight(b.x, b.y) - _surface.SampleHeight(a.x, a.y);
                steepest = Mathf.Max(steepest, Mathf.Atan2(rise, LipProbeStep) * Mathf.Rad2Deg);
            }

            Assert.Greater(steepest, UnclimbableSlope, "07 climbs 45 deg at most; the lip ends the drive out");
            Assert.IsTrue(_surface.NeedsFineCells(new Rect(along * (foot + rille.LipRun * 0.5f), Vector2.one)),
                "the lip is meshed in fine cells, so its sheer run stays sheer");
        }

        [Test]
        public void FromRelay1_TheDarkMareShowsBetweenTheNotchWalls()
        {
            RilleSettings rille = _settings.Rille;
            Vector2 mare = _surface.Rille.Direction * (_settings.CrestRadius + rille.RiseEnd);
            float eye = _surface.SampleHeight(Relay1.x, Relay1.y) + EyeHeight;
            float target = _surface.SampleHeight(mare.x, mare.y) + SightClearance;
            float distance = Vector2.Distance(Relay1, mare);
            for (float d = SightStep; d < distance - SightStep; d += SightStep)
            {
                float t = d / distance;
                Vector2 p = Vector2.Lerp(Relay1, mare, t);
                float sight = Mathf.Lerp(eye, target, t);
                Assert.LessOrEqual(_surface.SampleHeight(p.x, p.y), sight,
                    $"relay.1 sees the mare through the notch (blocked {d:F0} m out)");
            }

            Assert.Greater(_surface.Sample(mare.x, mare.y).Mare, 0.95f, "the ground it sees is the dark mare");
            Assert.AreEqual(0f, _surface.Sample(Relay1.x, Relay1.y).Mare, "the basin floor is no mare");
        }
    }
}
