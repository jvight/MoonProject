using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TitleAnimationState"/>: determinism, wheel rim speed matching the Moon's ground
    /// speed, the 48.0 s lunar revolution period, the Morse <c>· · · —</c> beacon cycle, and the rim landmarks at
    /// <c>t = 1.1 s</c>, <c>5.1 s</c>, and <c>9.0 s</c>.
    /// </summary>
    public sealed class TitleAnimationTests
    {
        [Test]
        public void Evaluate_IsDeterministic_ForAnyTimestamp()
        {
            var settings = new TitleSettings();
            var a = new TitleAnimationState(settings);
            var b = new TitleAnimationState(settings);

            float[] times = { 0f, 1.1f, 5.1f, 9.0f, 23.75f, 47.99f };
            for (int i = 0; i < times.Length; i++)
            {
                float t = times[i];
                a.Evaluate(t, true);
                b.Evaluate(t, true);

                Assert.AreEqual(a.MoonRotationRadians, b.MoonRotationRadians, 1e-6f);
                Assert.AreEqual(a.WheelAngleRadians, b.WheelAngleRadians, 1e-6f);
                Assert.AreEqual(a.BodyBob, b.BodyBob, 1e-6f);
                Assert.AreEqual(a.BodyPitch, b.BodyPitch, 1e-6f);
                Assert.AreEqual(a.ScoopNod, b.ScoopNod, 1e-6f);
                Assert.AreEqual(a.SolarTilt, b.SolarTilt, 1e-6f);
                Assert.AreEqual(a.AntennaSway, b.AntennaSway, 1e-6f);
                Assert.AreEqual(a.HeadNod, b.HeadNod, 1e-6f);
                Assert.AreEqual(a.MorseLit, b.MorseLit);
                Assert.AreEqual(a.EyeBlinking, b.EyeBlinking);
                Assert.AreEqual(a.EarthCenter, b.EarthCenter);

                for (int lm = 0; lm < a.Landmarks.Length; lm++)
                {
                    Assert.AreEqual(a.Landmarks[lm].Position.x, b.Landmarks[lm].Position.x, 1e-5f);
                    Assert.AreEqual(a.Landmarks[lm].Position.y, b.Landmarks[lm].Position.y, 1e-5f);
                    Assert.AreEqual(a.Landmarks[lm].Alpha, b.Landmarks[lm].Alpha, 1e-5f);
                    Assert.AreEqual(a.Landmarks[lm].Visible, b.Landmarks[lm].Visible);
                }
            }
        }

        [Test]
        public void WheelLinearSpeed_MatchesMoonSurfaceGroundSpeed()
        {
            var settings = new TitleSettings();
            var state = new TitleAnimationState(settings);
            state.Evaluate(3.2f, true);

            float expectedGroundSpeed = (2f * Mathf.PI / settings.MoonTurnPeriodSeconds) *
                                        settings.StandaloneMoonRadius;
            Assert.AreEqual(expectedGroundSpeed, state.GroundSpeed, 1e-4f);
            Assert.AreEqual(state.GroundSpeed, state.WheelLinearSpeed, 1e-4f,
                "wheelOmega * wheelR must equal moonOmega * moonR so the tire tread never slips on the regolith");
            Assert.AreEqual(state.WheelOmega * 3.2f, state.WheelAngleRadians, 1e-4f);
        }

        [Test]
        public void MoonAndLandmarks_RepeatExactlyEvery48Seconds()
        {
            var settings = new TitleSettings();
            Assert.AreEqual(48f, settings.MoonTurnPeriodSeconds, 1e-5f);

            var start = new TitleAnimationState(settings);
            var afterOneTurn = new TitleAnimationState(settings);

            float[] samples = { 1.1f, 5.1f, 9.0f, 19.4f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = samples[i];
                start.Evaluate(t, false);
                afterOneTurn.Evaluate(t + settings.MoonTurnPeriodSeconds, false);

                Assert.AreEqual(start.MoonRotationRadians, afterOneTurn.MoonRotationRadians, 1e-5f);
                Assert.AreEqual(start.BumpRear, afterOneTurn.BumpRear, 1e-5f);
                Assert.AreEqual(start.BumpFront, afterOneTurn.BumpFront, 1e-5f);

                for (int lm = 0; lm < start.Landmarks.Length; lm++)
                {
                    Assert.AreEqual(start.Landmarks[lm].Position.x, afterOneTurn.Landmarks[lm].Position.x, 1e-3f);
                    Assert.AreEqual(start.Landmarks[lm].Position.y, afterOneTurn.Landmarks[lm].Position.y, 1e-3f);
                    Assert.AreEqual(start.Landmarks[lm].Visible, afterOneTurn.Landmarks[lm].Visible);
                    Assert.AreEqual(start.Landmarks[lm].Alpha, afterOneTurn.Landmarks[lm].Alpha, 1e-4f);
                }
            }
        }

        [Test]
        public void MorseBeacon_FlashesThreeDotsAndOneDash_Each4Point2SecondCycle()
        {
            var settings = new TitleSettings();
            var state = new TitleAnimationState(settings);

            state.Evaluate(0.10f);
            Assert.IsFalse(state.MorseLit, "quiet before first dot");
            state.Evaluate(0.28f);
            Assert.IsTrue(state.MorseLit, "dot 1");
            state.Evaluate(0.45f);
            Assert.IsFalse(state.MorseLit, "gap 1");
            state.Evaluate(0.64f);
            Assert.IsTrue(state.MorseLit, "dot 2");
            state.Evaluate(0.82f);
            Assert.IsFalse(state.MorseLit, "gap 2");
            state.Evaluate(1.00f);
            Assert.IsTrue(state.MorseLit, "dot 3");
            state.Evaluate(1.20f);
            Assert.IsFalse(state.MorseLit, "gap 3");
            state.Evaluate(1.60f);
            Assert.IsTrue(state.MorseLit, "dash");
            state.Evaluate(2.50f);
            Assert.IsFalse(state.MorseLit, "long pause before next cycle");
            state.Evaluate(0.28f + settings.MorsePeriodSeconds);
            Assert.IsTrue(state.MorseLit, "dot 1 in next cycle");
        }

        [Test]
        public void Landmarks_PassAcrossHorizonAtStillTimestamps()
        {
            var settings = new TitleSettings();
            var state = new TitleAnimationState(settings);

            // t = 1.1 s: lander (0.6 s) has just passed behind 07 (dTheta < 0), tower (4.6 s) is approaching (dTheta > 0).
            state.Evaluate(1.1f, true);
            Assert.IsTrue(state.Landmarks[0].Visible, "t=1.1s: lander visible behind 07");
            Assert.Less(state.Landmarks[0].DeltaFromCrownRadians, 0f);
            Assert.IsTrue(state.Landmarks[1].Visible, "t=1.1s: relay tower visible ahead of 07");
            Assert.Greater(state.Landmarks[1].DeltaFromCrownRadians, 0f);

            // t = 5.1 s: tower (4.6 s) is now behind 07 (dTheta < 0), solar array (8.4 s) is approaching (dTheta > 0).
            state.Evaluate(5.1f, true);
            Assert.IsTrue(state.Landmarks[1].Visible, "t=5.1s: relay tower visible behind 07");
            Assert.Less(state.Landmarks[1].DeltaFromCrownRadians, 0f);
            Assert.IsTrue(state.Landmarks[2].Visible, "t=5.1s: solar array visible ahead of 07");
            Assert.Greater(state.Landmarks[2].DeltaFromCrownRadians, 0f);

            // t = 9.0 s: solar array (8.4 s) is now behind 07 (dTheta < 0), marker (13.0 s) is approaching (dTheta > 0).
            state.Evaluate(9.0f, true);
            Assert.IsTrue(state.Landmarks[2].Visible, "t=9.0s: solar array visible behind 07");
            Assert.Less(state.Landmarks[2].DeltaFromCrownRadians, 0f);
            Assert.IsTrue(state.Landmarks[3].Visible, "t=9.0s: marker visible ahead of 07");
            Assert.Greater(state.Landmarks[3].DeltaFromCrownRadians, 0f);
        }
    }
}
