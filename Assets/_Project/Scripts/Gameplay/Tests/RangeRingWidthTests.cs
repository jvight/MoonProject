using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The range ring's line is a few pixels on screen whatever its distance, thinner far away, never a band, and its
    /// brightness falls off as a soft gradient from its warm centre to zero at both edges.
    /// </summary>
    public sealed class RangeRingWidthTests
    {
        private const float MaxPixels = 6f;
        private const float MinPixels = 2f;
        private const float FullDistance = 80f;
        private const float Focal = 900f;
        private const float EdgeFalloff = 0.5f;
        private const float CorePower = 2.2f;
        private const float CoreMix = 0.85f;

        [Test]
        public void NearBy_TheLineHoldsItsWidestPixelWidth()
        {
            Assert.AreEqual(MaxPixels, RangeRingWidth.Pixels(10f, MaxPixels, MinPixels, FullDistance));
            Assert.AreEqual(MaxPixels, RangeRingWidth.Pixels(FullDistance, MaxPixels, MinPixels, FullDistance), 1e-4f);
        }

        [Test]
        public void FarAway_TheLineThinsWithDistance_ButNeverUnderItsFloor()
        {
            float at160 = RangeRingWidth.Pixels(160f, MaxPixels, MinPixels, FullDistance);
            float at240 = RangeRingWidth.Pixels(240f, MaxPixels, MinPixels, FullDistance);
            Assert.Less(at160, MaxPixels);
            Assert.Less(at240, at160);
            Assert.AreEqual(MinPixels, RangeRingWidth.Pixels(5000f, MaxPixels, MinPixels, FullDistance));
        }

        [Test]
        public void TheWorldWidth_ShowsAsThePixelWidth_AtEveryDistance()
        {
            foreach (float distance in new[] { 5f, 40f, 80f, 200f, 700f })
            {
                float world = RangeRingWidth.World(distance, Focal, MaxPixels, MinPixels, FullDistance);
                float shown = world * Focal / distance;
                Assert.AreEqual(RangeRingWidth.Pixels(distance, MaxPixels, MinPixels, FullDistance), shown, 1e-3f);
                Assert.LessOrEqual(shown, MaxPixels + 1e-3f, "never a thick stripe at the horizon");
            }
        }

        [Test]
        public void TheFocalLength_GrowsAsTheFieldOfViewNarrows()
        {
            Assert.Greater(RangeRingWidth.FocalPixels(1080f, 40f), RangeRingWidth.FocalPixels(1080f, 70f));
        }

        [Test]
        public void AcrossTheRibbon_TheGlowFallsOffSmoothlyFromWarmCentreToZeroAtBothEdges()
        {
            float leftEdge = RangeRingWidth.AcrossFalloff(0f, EdgeFalloff, CorePower, CoreMix);
            float eighth = RangeRingWidth.AcrossFalloff(0.125f, EdgeFalloff, CorePower, CoreMix);
            float quarter = RangeRingWidth.AcrossFalloff(0.25f, EdgeFalloff, CorePower, CoreMix);
            float centre = RangeRingWidth.AcrossFalloff(0.5f, EdgeFalloff, CorePower, CoreMix);
            float threeQuarter = RangeRingWidth.AcrossFalloff(0.75f, EdgeFalloff, CorePower, CoreMix);
            float rightEdge = RangeRingWidth.AcrossFalloff(1f, EdgeFalloff, CorePower, CoreMix);

            Assert.AreEqual(0f, leftEdge, 1e-5f, "zero at the outer edge");
            Assert.AreEqual(0f, rightEdge, 1e-5f, "zero at the inner edge");
            Assert.AreEqual(1f, centre, 1e-5f, "full soft core at the centre");
            Assert.AreEqual(quarter, threeQuarter, 1e-5f, "symmetric across the ribbon");
            Assert.Greater(eighth, leftEdge);
            Assert.Greater(quarter, eighth);
            Assert.Greater(centre, quarter);
            Assert.Less(quarter, 0.3f, "soft gradient falloff across the width, not a flat hard-edged stripe");
        }
    }
}
