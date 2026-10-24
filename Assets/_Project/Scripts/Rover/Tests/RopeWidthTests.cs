using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>The tow rope is drawn at least a few pixels wide at 1440p at any distance, its real width up close.</summary>
    public sealed class RopeWidthTests
    {
        private const float Fov = 50f;
        private const float Pixels = 3f;
        private const float RealWidth = 0.04f;

        [Test]
        public void Close_TheRopeKeepsItsRealWidth()
        {
            Assert.AreEqual(RealWidth, RopeWidth.World(1.5f, Fov, Pixels, RealWidth));
        }

        [Test]
        public void FarOff_TheRopeCoversItsPixelsOfA1440pScreen()
        {
            foreach (float distance in new[] { 8f, 15f, 30f })
            {
                float width = RopeWidth.World(distance, Fov, Pixels, RealWidth);
                float visibleHeight = 2f * distance * UnityEngine.Mathf.Tan(Fov * UnityEngine.Mathf.Deg2Rad * 0.5f);
                float pixels = width / visibleHeight * RopeWidth.ReferenceHeight;
                Assert.GreaterOrEqual(pixels, Pixels - 1e-3f, $"{distance} m away it is a few pixels wide");
            }
        }

        [Test]
        public void ItGrowsWithDistance_OnceTheRealWidthIsTooThin()
        {
            Assert.Greater(RopeWidth.World(30f, Fov, Pixels, RealWidth), RopeWidth.World(15f, Fov, Pixels, RealWidth));
        }

        [Test]
        public void AWiderLens_NeedsAWiderRopeForTheSamePixels()
        {
            Assert.Greater(RopeWidth.World(15f, 80f, Pixels, RealWidth), RopeWidth.World(15f, 40f, Pixels, RealWidth));
        }
    }
}
