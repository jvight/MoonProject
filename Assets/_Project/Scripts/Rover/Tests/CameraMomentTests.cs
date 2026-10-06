using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class CameraMomentTests
    {
        private const float Frame = 1f / 60f;

        private static readonly CameraMomentSettings Settings =
            new CameraMomentSettings(1f, 1f, 1f, 0.5f, 3f, 0.8f, 60f, 10f, 0.5f, 40f);

        private static float Run(CameraMoment moment, float seconds)
        {
            float largestStep = 0f;
            for (float t = 0f; t < seconds; t += Frame)
            {
                float before = moment.Weight;
                moment.Step(Frame);
                largestStep = Mathf.Max(largestStep, Mathf.Abs(moment.Weight - before));
            }

            return largestStep;
        }

        [Test]
        public void Idle_HasNoWeight()
        {
            var moment = new CameraMoment();
            Run(moment, 1f);
            Assert.AreEqual(0f, moment.Weight);
            Assert.IsFalse(moment.IsActive);
        }

        [Test]
        public void EasesIn_Holds_EasesOut_ThenEnds()
        {
            var moment = new CameraMoment();
            moment.Start(Settings, Vector3.one);
            Assert.AreEqual(0f, moment.Weight, "No frame-one jump.");

            float steepest = Run(moment, Settings.EaseIn + 0.05f);
            Assert.AreEqual(1f, moment.Weight, 1e-4f);
            Assert.Less(steepest, 0.05f, "Eased, never a cut.");

            Run(moment, Settings.Hold - 0.1f);
            Assert.AreEqual(1f, moment.Weight, 1e-4f);

            Run(moment, 0.2f + Settings.EaseOut);
            Assert.AreEqual(0f, moment.Weight);
            Assert.IsFalse(moment.IsActive);
        }

        [Test]
        public void Cancel_EasesOutQuicklyFromWhereItIs()
        {
            var moment = new CameraMoment();
            moment.Start(Settings, Vector3.one);
            Run(moment, 0.5f);
            float at = moment.Weight;
            Assert.That(at, Is.InRange(0.3f, 0.7f));

            moment.Cancel(0.4f);
            moment.Step(Frame);
            Assert.Less(Mathf.Abs(moment.Weight - at), 0.05f, "Cancelling does not snap.");
            Run(moment, 0.45f);
            Assert.AreEqual(0f, moment.Weight);
            Assert.IsFalse(moment.IsActive);
        }

        [Test]
        public void Cancel_WhenIdle_DoesNothing()
        {
            var moment = new CameraMoment();
            moment.Cancel(0.5f);
            Assert.IsFalse(moment.IsActive);
        }

        [Test]
        public void Cancel_DoesNotSlowAQuickerEaseOut()
        {
            var moment = new CameraMoment();
            moment.Start(Settings, Vector3.one);
            Run(moment, 0.5f);
            moment.Cancel(0.2f);
            moment.Cancel(5f);
            Run(moment, 0.25f);
            Assert.IsFalse(moment.IsActive);
        }

        [Test]
        public void Restart_DuringEaseOut_ContinuesFromTheCurrentWeight()
        {
            var moment = new CameraMoment();
            moment.Start(Settings, Vector3.one);
            Run(moment, Settings.EaseIn + Settings.Hold + 0.4f);
            float at = moment.Weight;
            moment.Start(Settings, Vector3.zero);
            moment.Step(Frame);
            Assert.Less(Mathf.Abs(moment.Weight - at), 0.05f);
            Assert.AreEqual(Vector3.zero, moment.Subject);
            Run(moment, Settings.EaseIn);
            Assert.AreEqual(1f, moment.Weight, 1e-4f);
        }

        [Test]
        public void FocusReach_FullNearby_GoneBeyondTheLimit()
        {
            Assert.AreEqual(1f, CameraMoment.FocusReach(Settings, 5f));
            Assert.AreEqual(0f, CameraMoment.FocusReach(Settings, Settings.MaxFocusDistance + 1f));
            float between = CameraMoment.FocusReach(Settings, 0.875f * Settings.MaxFocusDistance);
            Assert.That(between, Is.InRange(0.1f, 0.9f), "Fades out smoothly.");
        }

        [Test]
        public void BlendYaw_SwingsAShareOfTheWay_CappedForSubjectsBehind()
        {
            Assert.AreEqual(10f + 0.8f * 40f, CameraMoment.BlendYaw(Settings, 10f, 50f, 1f), 1e-3f);
            Assert.AreEqual(10f + 0.8f * 20f, CameraMoment.BlendYaw(Settings, 10f, 50f, 0.5f), 1e-3f);
            Assert.AreEqual(Settings.MaxYawSwing, CameraMoment.BlendYaw(Settings, 0f, 170f, 1f), 1e-3f);
            Assert.AreEqual(-Settings.MaxYawSwing, CameraMoment.BlendYaw(Settings, 0f, -170f, 1f), 1e-3f);
            Assert.AreEqual(25f, CameraMoment.BlendYaw(Settings, 25f, 90f, 0f), 1e-4f);
        }

        [Test]
        public void LookPoint_MovesAShareTowardTheSubject_CappedSo07StaysInFrame()
        {
            Vector3 near = CameraMoment.LookPoint(Settings, Vector3.zero, new Vector3(4f, 0f, 0f), 1f);
            Assert.AreEqual(2f, near.x, 1e-4f);
            Vector3 far = CameraMoment.LookPoint(Settings, Vector3.zero, new Vector3(100f, 0f, 0f), 1f);
            Assert.AreEqual(Settings.MaxLookShift, far.x, 1e-4f);
            Assert.AreEqual(Vector3.zero, CameraMoment.LookPoint(Settings, Vector3.zero, Vector3.one, 0f));
        }

        [Test]
        public void BlendYaw_TakesTheShortWayAcrossNorth()
        {
            float yaw = CameraMoment.BlendYaw(Settings, 350f, 20f, 1f);
            Assert.AreEqual(350f + 0.8f * 30f, yaw, 1e-3f);
        }

        [Test]
        public void ShippedMoments_AreSlowAndSkippable()
        {
            var tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            try
            {
                Assert.That(tuning.RelicMoment.Duration, Is.InRange(2f, 3.5f), "Relic moment lasts ~2-3 s.");
                Assert.Greater(tuning.UpgradeMoment.Lift, 0f);
                Assert.Greater(tuning.UpgradeMoment.PullBack, 0f);
                Assert.Less(tuning.MomentCancelEaseOut, tuning.RelicMoment.EaseOut);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
