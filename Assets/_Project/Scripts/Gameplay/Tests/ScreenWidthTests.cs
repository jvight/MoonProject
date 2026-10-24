using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>The scan beam keeps a legible width on screen at any camera distance.</summary>
    public sealed class ScreenWidthTests
    {
        [Test]
        public void FourPixels_AtSevenAndAHalfMetres_On1080p60Degrees()
        {
            const float pixelsPerMetreAtOne = 1080f / (2f * 0.57735f);
            Assert.AreEqual(0.0321f, ScreenWidth.Metres(4f, 7.5f, pixelsPerMetreAtOne), 1e-3f);
        }

        [Test]
        public void TheWidth_GrowsWithDistance_AndNeverDividesByZero()
        {
            Assert.AreEqual(2f * ScreenWidth.Metres(3f, 5f, 900f), ScreenWidth.Metres(3f, 10f, 900f), 1e-6f);
            Assert.IsFalse(float.IsInfinity(ScreenWidth.Metres(3f, 5f, 0f)));
        }
    }
}
