using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The rockfall's fresh scar (base-home-and-crater-exits §4): pale ground up the Peak's flank on the road's
    /// bearing, none off it, and tall enough from the apron that the eye finds the closed road at minute 1.
    /// </summary>
    public sealed class RockfallScarTests
    {
        // An eye on the apron, how tall the scar must stand there, and where off the road it must be gone.
        private const float EyeHeight = 3.2f;
        private const float MinTallDegrees = 4f;
        private const float OffTheRoad = 60f;
        private const float AlongStep = 1f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
        }

        [Test]
        public void TheScar_LiesUpTheRoad_AndNowhereElse()
        {
            RockfallScarSettings scar = _settings.RockfallScar;
            Vector2 road = _surface.PeakRoadFoot.normalized;
            Vector2 middle = _surface.PeakRoadFoot + road * (scar.Start + scar.Length * 0.5f);
            Assert.Greater(_surface.Sample(middle.x, middle.y).Scar, 0.99f, "fresh ground up the road");

            var side = new Vector2(road.y, -road.x);
            Vector2 off = middle + side * OffTheRoad;
            Assert.AreEqual(0f, _surface.Sample(off.x, off.y).Scar, "none off the road");
            Assert.AreEqual(0f, _surface.Sample(0f, 0f).Scar, "none at the base");
        }

        [Test]
        public void FromTheApron_TheScarStandsTallOnThePeaksFlank()
        {
            RockfallScarSettings scar = _settings.RockfallScar;
            Vector2 apron = _settings.Sites.ApronCenter;
            float eye = _surface.SampleHeight(apron.x, apron.y) + EyeHeight;
            Vector2 road = _surface.PeakRoadFoot.normalized;
            float lowest = float.MaxValue;
            float highest = float.MinValue;
            for (float d = scar.Start; d <= scar.Start + scar.Length; d += AlongStep)
            {
                Vector2 p = _surface.PeakRoadFoot + road * d;
                float angle = Mathf.Atan2(_surface.SampleHeight(p.x, p.y) - eye, Vector2.Distance(apron, p))
                    * Mathf.Rad2Deg;
                lowest = Mathf.Min(lowest, angle);
                highest = Mathf.Max(highest, angle);
            }

            Assert.Greater(highest - lowest, MinTallDegrees, "the scar stands tall enough to find from the base");
        }
    }
}
