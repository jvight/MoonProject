using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>The range ring's line is a few pixels on screen whatever its distance, thinner far away, never a band.</summary>
    public sealed class RangeRingWidthTests
    {
        private const float MaxPixels = 3f;
        private const float MinPixels = 1f;
        private const float FullDistance = 80f;
        private const float Focal = 900f;

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
    }
}
