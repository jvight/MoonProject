using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Face.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="FaceState"/>, <see cref="FaceTuning"/>, and <see cref="FaceDisplay"/>
    /// (0.5.7 slice 9).
    /// </summary>
    public sealed class FaceStateTests
    {
        private FaceTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<FaceTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tuning != null)
            {
                UnityEngine.Object.DestroyImmediate(_tuning);
            }
        }

        [Test]
        public void Easing_ConvergesTowardEachFeeling_AndMatchesPrototypeSpecs()
        {
            var state = new FaceState(_tuning, seed: 42);
            var feelings = (FaceFeeling[])Enum.GetValues(typeof(FaceFeeling));

            for (int i = 0; i < feelings.Length; i++)
            {
                FaceFeeling feeling = feelings[i];
                FaceFeelingSpec spec = _tuning.GetFeelingSpec(feeling);
                state.SetFeeling(feeling);

                // Step for 3 seconds in small increments so easing converges
                for (int step = 0; step < 180; step++)
                {
                    state.Step(1f / 60f);
                }

                // Ensure no blink is active at assertion moment
                while (state.IsBlinking)
                {
                    state.Step(0.02f);
                }

                Assert.AreEqual(feeling, state.Feeling);
                Assert.AreEqual(spec.Width, state.LeftEye.Width, 1e-3f, $"Width mismatch for {feeling}");
                Assert.AreEqual(spec.Height, state.LeftEye.Height, 1e-3f, $"Height mismatch for {feeling}");
                Assert.AreEqual(spec.Open, state.LeftEye.Open, 1e-3f, $"Open mismatch for {feeling}");
                Assert.AreEqual(spec.Tilt, state.LeftEye.Tilt, 1e-3f, $"Tilt mismatch for {feeling}");
                Assert.AreEqual(spec.Cut, state.LeftEye.Cut, 1e-3f, $"Cut mismatch for {feeling}");
                Assert.AreEqual(spec.Squint, state.LeftEye.Squint, 1e-3f, $"Squint mismatch for {feeling}");
                Assert.AreEqual(spec.Arc, state.LeftEye.Arc, 1e-3f, $"Arc mismatch for {feeling}");
                Assert.AreEqual(spec.Ring, state.LeftEye.Ring, 1e-3f, $"Ring mismatch for {feeling}");
                Assert.AreEqual(spec.Closed, state.LeftEye.Closed, 1e-3f, $"Closed mismatch for {feeling}");
                Assert.AreEqual(spec.Symbol, state.ActiveSymbol, $"Symbol mismatch for {feeling}");

                float expectedSymAlpha = spec.Symbol != FaceSymbol.None ? 1.0f : 0.0f;
                Assert.AreEqual(expectedSymAlpha, state.SymbolAlpha, 1e-2f, $"SymbolAlpha mismatch for {feeling}");
            }
        }

        [Test]
        public void Blinks_NeverHappenWhileAsleep()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Asleep);

            // Attempt manual blink while asleep
            state.Blink();
            Assert.IsFalse(state.IsBlinking, "Manual Blink() must be ignored while asleep.");
            Assert.AreEqual(1.0f, state.BlinkMultiplier, 1e-5f);

            // Step through 60 seconds of simulated time while asleep
            for (int i = 0; i < 3600; i++)
            {
                state.Step(1f / 60f);
                Assert.IsFalse(state.IsBlinking, $"Blink occurred while asleep at step {i}.");
                Assert.AreEqual(1.0f, state.BlinkMultiplier, 1e-5f);
                Assert.AreEqual(0.05f, state.LeftEye.LookY, 1e-3f, "Saccades must not shift gaze while asleep.");
            }
        }

        [Test]
        public void Blink_ClosesAndReopensWithinItsLength()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Calm);

            float duration = _tuning.BlinkDuration;
            state.Blink();
            Assert.IsTrue(state.IsBlinking, "Blink should be active immediately after Blink().");

            // Advance to mid-blink (50% of duration)
            state.Step(duration * 0.5f);
            Assert.IsTrue(state.IsBlinking);
            Assert.Less(state.BlinkMultiplier, 0.05f, "Eye should close near 0 at mid-blink.");
            Assert.Less(state.LeftEye.Open, 0.05f, "LeftEye.Open should reflect mid-blink closure.");

            // Advance past the end of the blink
            state.Step(duration * 0.55f);
            Assert.IsFalse(state.IsBlinking, "Blink should complete once BlinkDuration has elapsed.");
            Assert.AreEqual(1.0f, state.BlinkMultiplier, 1e-4f, "Eye should reopen to 1.0 after blink completes.");
            Assert.AreEqual(1.0f, state.LeftEye.Open, 1e-4f);
        }

        [Test]
        public void BootSequence_ShowsHorizontalLineFirst_ThenOpensEyesVertically()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Calm);
            state.Boot();

            Assert.AreEqual(0.0f, state.BootProgress, 1e-5f);
            Assert.IsTrue(state.IsBootLineStage, "Boot must begin in the horizontal power-on line stage.");
            Assert.AreEqual(0.0f, state.BootEyeOpen, 1e-5f, "Eyes must be closed during the power-on line stage.");

            // Step into the middle of the horizontal line stage (boot ~ 0.225 < 0.45)
            state.Step(0.25f);
            Assert.Less(state.BootProgress, _tuning.BootLineEnd);
            Assert.IsTrue(state.IsBootLineStage);
            Assert.Greater(state.BootLineProgress, 0.4f);
            Assert.AreEqual(0.0f, state.BootEyeOpen, 1e-5f);

            // Step into the vertical eye-opening stage (0.45 < boot < 0.75)
            state.Step(0.40f);
            Assert.Greater(state.BootProgress, _tuning.BootLineEnd);
            Assert.Less(state.BootProgress, _tuning.BootLineEnd + _tuning.BootOpenSpan);
            Assert.IsFalse(state.IsBootLineStage, "Boot should have exited the line stage.");
            Assert.Greater(state.BootEyeOpen, 0.1f, "Eyes should be opening vertically.");
            Assert.Less(state.BootEyeOpen, 0.95f);

            // Step past full boot completion
            state.Step(0.60f);
            Assert.AreEqual(1.0f, state.BootProgress, 1e-4f);
            Assert.AreEqual(1.0f, state.BootEyeOpen, 1e-4f);
        }

        [Test]
        public void CuriousFeeling_ProducesAsymmetricLeftAndRightEyes()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SetFeeling(FaceFeeling.Curious);

            for (int i = 0; i < 180; i++)
            {
                state.Step(1f / 60f);
            }

            Assert.AreEqual(_tuning.AsymmetricLeftScale, state.LeftEye.Scale, 1e-3f);
            Assert.AreEqual(_tuning.AsymmetricRightScale, state.RightEye.Scale, 1e-3f);
            Assert.Greater(state.LeftEye.Scale, state.RightEye.Scale, "Curious left eye must be larger than right eye.");
            Assert.Greater(state.LeftEye.RenderedWidth, state.RightEye.RenderedWidth);

            Assert.AreEqual(0.10f, state.LeftEye.Cut, 1e-3f);
            Assert.AreEqual(_tuning.AsymmetricRightCut, state.RightEye.Cut, 1e-3f);
            Assert.Greater(state.RightEye.Cut, state.LeftEye.Cut, "Curious right eye must squint with a deeper top cut.");

            Assert.AreEqual(0.20f, state.LeftEye.Tilt, 1e-3f);
            Assert.AreEqual(_tuning.AsymmetricRightTilt, state.RightEye.Tilt, 1e-3f);
            Assert.AreEqual(FaceSymbol.Question, state.ActiveSymbol);
            Assert.AreEqual(1.0f, state.SymbolAlpha, 1e-2f);
        }

        [Test]
        public void LowPowerAmount_ClampsToZeroOne_AndDimsBrightness()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Calm);
            float fullPowerBrightness = state.Brightness;
            float fullPowerHeight = state.LeftEye.RenderedHeight;

            state.SetLowPower(-2.5f);
            Assert.AreEqual(0.0f, state.LowPower, 1e-5f);
            Assert.AreEqual(fullPowerBrightness, state.Brightness, 1e-5f);
            Assert.AreEqual(fullPowerHeight, state.LeftEye.RenderedHeight, 1e-5f);

            state.SetLowPower(3.2f);
            Assert.AreEqual(1.0f, state.LowPower, 1e-5f);
            Assert.AreEqual(
                fullPowerBrightness * _tuning.LowPowerMinBrightness,
                state.Brightness,
                1e-4f,
                "Full low-power state should dim brightness by LowPowerMinBrightness (55%).");
            Assert.Less(
                state.LeftEye.RenderedHeight,
                fullPowerHeight * 0.65f,
                "Low-power mode must clamp eye height below full-power calm height.");
            Assert.GreaterOrEqual(state.LeftEye.Cut, 0.25f - 1e-4f);
            Assert.AreEqual(FaceSymbol.Battery, state.ActiveSymbol);

            state.Step(0.125f);
            Assert.LessOrEqual(Mathf.Abs(state.VerticalJitter), _tuning.LowPowerJitterAmplitude + 1e-5f);
        }

        [Test]
        public void PhosphorGhostTrail_LagsBehindEyeMovement_AndConverges()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Calm);

            state.SetFeeling(FaceFeeling.Happy);
            state.Step(1f / 60f);

            Assert.Less(
                state.GhostLeftEye.Arc,
                state.LeftEye.Arc,
                "Ghost eye should trail behind primary eye immediately after a transition step.");

            for (int i = 0; i < 180; i++)
            {
                state.Step(1f / 60f);
            }

            Assert.AreEqual(state.LeftEye.Arc, state.GhostLeftEye.Arc, 1e-3f);
        }

        [Test]
        public void FaceDisplay_PushesPropertyBlockVectorsToRenderer()
        {
            var go = new GameObject("TestFaceScreen");
            try
            {
                var renderer = go.AddComponent<MeshRenderer>();
                var display = go.AddComponent<FaceDisplay>();
                display.Initialize(_tuning, renderer, seed: 7);

                display.SetFeeling(FaceFeeling.Worried);
                display.SetLowPower(0.5f);
                display.LookAt(new Vector3(1f, 0.5f, 1f));
                display.Step(0.5f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                Vector4 crtState = block.GetVector("_FaceCrtState");
                Assert.AreEqual(1.0f, crtState.x, 1e-4f, "Boot progress should be 1.");
                Assert.AreEqual(0.5f, crtState.z, 1e-4f, "LowPower should be 0.5.");

                Vector4 symbolVec = block.GetVector("_FaceSymbol");
                Assert.AreEqual((float)FaceSymbol.Battery, symbolVec.x, 1e-4f);
                Assert.Greater(symbolVec.y, 0.8f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ScreenContentMode_CollapsesToLine_ExpandsToContent_AndClosesWithHappyBlink()
        {
            var state = new FaceState(_tuning, seed: 7);
            state.SnapToFeeling(FaceFeeling.Calm);

            state.SetScreenMode(true);
            Assert.IsTrue(state.IsScreenModeOpen);

            // Lead-in (0.00 - 0.18 s): calm eyes remain visible while 07 lifts head
            state.Step(0.10f);
            Assert.AreEqual(0f, state.ScreenContentMix, 1e-4f);
            Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-4f);

            // Collapse + Line Hold (0.18 - 0.50 s): pinched into bright horizontal line
            state.Step(0.30f);
            Assert.AreEqual(0f, state.ScreenContentMix, 1e-4f);
            Assert.Greater(state.ScreenVerticalPinch, 0.95f);
            Assert.Greater(state.ScreenLineGlow, 0.95f);

            // Expand into content (0.50 - 0.72 s): content mix = 1, pinch expanding toward 0
            state.Step(0.20f);
            Assert.AreEqual(1f, state.ScreenContentMix, 1e-4f);
            Assert.Less(state.ScreenVerticalPinch, 0.5f);

            // Settled content (> 0.72 s)
            state.Step(0.25f);
            Assert.AreEqual(1f, state.ScreenContentMix, 1e-4f);
            Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-4f);
            Assert.AreEqual(0f, state.ScreenLineGlow, 1e-4f);

            // Close: collapses to line (0.14 s), then expands back to happy arc eyes (^ ^)
            state.SetScreenMode(false);
            Assert.IsFalse(state.IsScreenModeOpen);
            state.Step(0.14f);
            Assert.Greater(state.ScreenVerticalPinch, 0.95f);

            state.Step(0.20f);
            Assert.AreEqual(0f, state.ScreenContentMix, 1e-4f);
            Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-4f);
            Assert.AreEqual(1, state.ClosingHappyBlinkCount);
            Assert.Greater(state.LeftEye.Arc, 0.5f, "Closing must end with a happy curved-eye blink (^ ^).");
        }
    }
}
