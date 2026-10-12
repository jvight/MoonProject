using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's body response: kicks peak near their travel and settle; holds lean and ease back.</summary>
    public sealed class ImpactNudgeTests
    {
        private const float Frame = 1f / 120f;

        private static float Run(ImpactNudge nudge, float seconds, System.Func<ImpactNudge, float> read)
        {
            float peak = 0f;
            for (float t = 0f; t < seconds; t += Frame)
            {
                nudge.Step(Frame);
                peak = Mathf.Max(peak, Mathf.Abs(read(nudge)));
            }

            return peak;
        }

        [Test]
        public void ARecoil_PeaksAtItsDistance_AndSettlesWithinItsRecovery()
        {
            var recoil = new NudgeSettings(0.015f, 0f, 4f, 1f);
            var nudge = new ImpactNudge();
            nudge.KickSurge(recoil);
            float peak = Run(nudge, 0.25f, n => n.Surge);
            Assert.AreEqual(0.015f, peak, 0.002f, "Peaks at 1.5 cm back.");
            Assert.Less(Mathf.Abs(nudge.Surge), 0.004f, "Mostly back after 250 ms.");
            Run(nudge, 1f, n => n.Surge);
            Assert.IsTrue(nudge.IsAtRest(1e-3f), "Settled.");
        }

        [Test]
        public void AnUnderdampedDip_OvershootsOnce()
        {
            var dip = new NudgeSettings(0.02f, 0f, 3f, 0.4f);
            var nudge = new ImpactNudge();
            nudge.KickHeave(dip);
            bool wentDown = false;
            bool cameBackAbove = false;
            for (float t = 0f; t < 1.5f; t += Frame)
            {
                nudge.Step(Frame);
                wentDown |= nudge.Heave < -0.01f;
                cameBackAbove |= wentDown && nudge.Heave > 0.001f;
            }

            Assert.IsTrue(wentDown && cameBackAbove, "Dips, then rebounds past rest.");
        }

        [Test]
        public void AHold_Leans_AndReleasing_EasesBack()
        {
            var lean = new NudgeSettings(0f, 1f, 2f, 1f);
            var nudge = new ImpactNudge();
            nudge.Hold(lean);
            Run(nudge, 2f, n => n.Pitch);
            Assert.AreEqual(1f, nudge.Pitch, 0.02f, "Leans 1 degree forward.");
            nudge.Release();
            Run(nudge, 3f, n => n.Pitch);
            Assert.IsTrue(nudge.IsAtRest(1e-3f));
        }

        [Test]
        public void Reset_IsAtRestAtOnce()
        {
            var nudge = new ImpactNudge();
            nudge.Hold(new NudgeSettings(0f, 2f, 2f, 1f));
            nudge.KickHeave(new NudgeSettings(0.02f, 0f, 3f, 1f));
            nudge.Step(0.1f);
            nudge.Reset();
            Assert.IsTrue(nudge.IsAtRest(1e-6f));
        }
    }
}
