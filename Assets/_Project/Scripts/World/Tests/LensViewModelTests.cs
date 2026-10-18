using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    public sealed class LensViewModelTests
    {
        private const float Frame = 1f / 60f;

        private LensViewSettings _settings;
        private LensViewModel _model;

        [SetUp]
        public void SetUp()
        {
            _settings = new LensViewSettings();
            _model = new LensViewModel(_settings);
        }

        private static LensViewState State(float lid, float focus = 1000f, float flicker = 0f)
        {
            return new LensViewState(lid, focus, flicker, 0f, Vector2.zero, 0f);
        }

        [Test]
        public void ShutLidLeavesTheWholeFrameDark()
        {
            _model.Step(State(0f), Frame);

            Assert.LessOrEqual(_model.LidEdge, 0f, "lid 0: the edge sits at the very bottom, so the frame is black");
        }

        [Test]
        public void OpenLidClearsTheEdgeItsArcAndItsSoftnessAboveTheFrame()
        {
            _model.Step(State(1f), Frame);

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
            _model.Step(State(0.12f), Frame);
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "the slit gets the full gain");
            _model.Step(State(_settings.GainHoldLid), Frame);
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "the full gain holds up to its hold lid");
            _model.Step(State(0.55f), Frame);
            Assert.That(_model.Gain, Is.GreaterThan(0f).And.LessThan(_settings.WakeGain), "it fades on the way up");
            _model.Step(State(_settings.GainGoneLid), Frame);
            Assert.AreEqual(0f, _model.Gain, 1e-5f, "the resting lid sees the world's own exposure");
        }

        [Test]
        public void EyeGain_NeverComesBackOnABlink_OnlyAfterTheLidShutsOrTheLensIsLookedThroughAgain()
        {
            _model.Step(State(0.7f), Frame);
            _model.Step(State(0.25f), Frame);
            Assert.AreEqual(0f, _model.Gain, 1e-5f, "a blink after the rest does not brighten the picture");

            _model.Step(State(0f), Frame);
            _model.Step(State(0.12f), Frame);
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f,
                "the push into the shut lens, then its slit: the full gain");

            _model.Step(State(0.7f), Frame);
            _model.Reset();
            _model.Step(State(0.12f), Frame);
            Assert.AreEqual(_settings.WakeGain, _model.Gain, 1e-5f, "a new look starts with the full gain");
        }

        [Test]
        public void DipNeverTakesMoreThanThirtyPercentOfTheLight()
        {
            for (float flicker = 0f; flicker <= 2f; flicker += 0.05f)
            {
                _model.Step(State(0.7f, 1000f, flicker), Frame);
                Assert.LessOrEqual(_model.Dip, 0.3f + 1e-6f, $"flicker {flicker}");
            }
        }

        [Test]
        public void RollBandTravelsOnlyWhileThePictureDips()
        {
            float start = _model.RollBand;
            _model.Step(State(0.7f), Frame);
            Assert.AreEqual(start, _model.RollBand, "no flicker, no band");

            _model.Step(State(0.7f, 1000f, 1f), 0.2f);
            Assert.Less(_model.RollBand, start, "the band rolls down the frame during a dip");
        }

        [Test]
        public void SpeckIsSharpOnlyWhileTheLensFocusesOnTheGlass()
        {
            _model.Step(State(0.7f, 0.1f), Frame);
            Assert.AreEqual(1f, _model.SpeckSharpness, 1e-5f, "focused very near: the speck is sharp");

            _model.Step(State(0.7f, 1000f), Frame);
            Assert.AreEqual(0f, _model.SpeckSharpness, 1e-5f, "focused far: the speck is a blur");
        }

        [Test]
        public void FocusNeverHuntsNearerThanTheLensCan()
        {
            _model.Step(State(0.7f, 0f), Frame);

            Assert.AreEqual(_settings.NearestFocus, _model.FocusDistance, 1e-5f);
        }
    }
}
