using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    public sealed class LensViewModelTests
    {
        private LensViewSettings _settings;
        private LensViewModel _model;

        [SetUp]
        public void SetUp()
        {
            _settings = new LensViewSettings();
            _model = new LensViewModel(_settings);
        }

        private static LensViewState State(float lid, float focus = 1000f)
        {
            return new LensViewState(lid, focus);
        }

        [Test]
        public void ShutLidLeavesTheWholeFrameDark()
        {
            _model.Step(State(0f));

            Assert.LessOrEqual(_model.LidEdge, 0f, "lid 0: the edge sits at the very bottom, so the frame is black");
        }

        [Test]
        public void OpenLidClearsTheEdgeItsArcAndItsSoftnessAboveTheFrame()
        {
            _model.Step(State(1f));

            Assert.GreaterOrEqual(_model.LidEdge - _settings.LidCurve - _settings.LidSoftness, 1f - 1e-5f,
                "lid 1: even the sides of the soft edge are above the top of the frame");
        }

        [Test]
        public void RestingLidLeavesMostOfTheFrameClear()
        {
            float rest = LensViewModel.EdgeFor(0.7f, _settings);
            float slit = LensViewModel.EdgeFor(0.12f, _settings);

            Assert.Greater(rest - _settings.LidSoftness, 0.6f, "half-lidded rest: the world fills most of the frame");
            Assert.Less(slit, 0.35f, "the first stage is a slit at the bottom");
        }

        [Test]
        public void EyeGain_IsFullThroughTheSlit_FadesOverTheHalfLid_AndIsGoneByTheRestingLid()
        {
            _model.Step(State(0.12f));
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "the slit gets the full gain");
            _model.Step(State(_settings.GainHoldLid));
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "the full gain holds up to its hold lid");
            _model.Step(State(0.55f));
            Assert.That(_model.Gain, Is.GreaterThan(0f).And.LessThan(_settings.WakeGain), "it fades on the way up");
            _model.Step(State(_settings.GainGoneLid));
            Assert.AreEqual(0f, _model.Gain, 1e-5f, "the resting lid sees the world's own exposure");
        }

        [Test]
        public void EyeGain_NeverComesBackOnABlink_OnlyAfterTheLidShutsOrTheLensIsLookedThroughAgain()
        {
            _model.Step(State(0.7f));
            _model.Step(State(0.25f));
            Assert.AreEqual(0f, _model.Gain, 1e-5f, "a blink after the rest does not brighten the picture");

            _model.Step(State(0f));
            _model.Step(State(0.12f));
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f,
                "the push into the shut lens, then its slit: the full gain");

            _model.Step(State(0.7f));
            _model.Reset();
            _model.Step(State(0.12f));
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "a new look starts with the full gain");
        }

        [Test]
        public void FocusNeverHuntsNearerThanTheLensCan()
        {
            _model.Step(State(0.7f, 0f));

            Assert.AreEqual(_settings.NearestFocus, _model.FocusDistance, 1e-5f);
        }
    }
}
