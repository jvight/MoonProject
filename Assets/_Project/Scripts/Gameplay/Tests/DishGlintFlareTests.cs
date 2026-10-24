using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    public sealed class DishGlintFlareTests
    {
        private const float Seconds = 0.4f;
        private const float RiseShare = 0.12f;

        [Test]
        public void TheGlint_IsDarkBeforeAndAfterItsLife()
        {
            Assert.AreEqual(0f, DishGlintFlare.Curve(0f, Seconds, RiseShare), 1e-5f);
            Assert.AreEqual(0f, DishGlintFlare.Curve(Seconds, Seconds, RiseShare), 1e-5f);
            Assert.AreEqual(0f, DishGlintFlare.Curve(Seconds * 2f, Seconds, RiseShare), 1e-5f);
        }

        [Test]
        public void TheGlint_PeaksAtFullWhereTheRiseEnds_AndNeverExceedsIt()
        {
            Assert.AreEqual(1f, DishGlintFlare.Curve(Seconds * RiseShare, Seconds, RiseShare), 1e-3f);
            for (float age = 0f; age < Seconds; age += 0.005f)
            {
                Assert.LessOrEqual(DishGlintFlare.Curve(age, Seconds, RiseShare), 1.0001f);
            }
        }

        [Test]
        public void TheGlint_FadesFast_HalfGoneByTheMiddleOfItsLife()
        {
            Assert.Less(DishGlintFlare.Curve(Seconds * 0.5f, Seconds, RiseShare), 0.5f);
        }

        [Test]
        public void TheGlint_FlaresSharply_InAFewFrames()
        {
            float aFewFramesIn = 3f / 60f;
            Assert.Greater(DishGlintFlare.Curve(aFewFramesIn, Seconds, RiseShare), 0.8f);
        }
    }
}
