using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The cover over a reloading New Game: closes from the edges inside 0.6 s, and the new game opens from it.</summary>
    public sealed class CoverVignetteTests
    {
        private const float Frame = 1f / 60f;

        private BootFaceSettings _settings;
        private CoverVignette _cover;

        [SetUp]
        public void SetUp()
        {
            _settings = new BootFaceSettings();
            _cover = new CoverVignette(_settings);
        }

        [Test]
        public void TheCover_IsOpenUntilAsked_ThenClosesWithinSixTenths_EasedAllTheWay()
        {
            _cover.Tick(1f);
            Assert.IsFalse(_cover.IsVisible, "nothing covers the picture unless App will reload");
            _cover.Close();
            float t = 0f;
            float last = 0f;
            while (_cover.Cover01 < 1f && t < 2f)
            {
                _cover.Tick(Frame);
                t += Frame;
                Assert.GreaterOrEqual(_cover.Cover01, last, "it only closes");
                last = _cover.Cover01;
            }

            Assert.LessOrEqual(t, 0.6f + Frame, "closed before Rover's cover lets App reload");
            Assert.AreEqual(_settings.CoverCloseSeconds, t, 2f * Frame);
        }

        [Test]
        public void TheNewGameAfterTheReload_BeginsCovered_AndOpensOnTheFace()
        {
            _cover.SnapClosed();
            Assert.AreEqual(1f, _cover.Cover01, 1e-5f, "the first frame after the reload is covered");
            _cover.Open();
            float t = 0f;
            while (_cover.IsVisible && t < 2f)
            {
                _cover.Tick(Frame);
                t += Frame;
            }

            Assert.AreEqual(_settings.CoverOpenSeconds, t, 2f * Frame, "then opens in its tuned time");
        }
    }
}
