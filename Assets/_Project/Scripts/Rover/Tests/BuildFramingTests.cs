using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// <see cref="BuildFraming"/>: the one gentle turn toward an off-screen placing ghost (at most 20 deg/s), ended by
    /// any look, recenter or drive input, and started only for a ghost that is off screen.
    /// </summary>
    public sealed class BuildFramingTests
    {
        private const float MaxAllowedDegreesPerSecond = 20f;
        private const float FrameDt = 1f / 60f;

        private RoverCameraTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = AssetDatabase.LoadAssetAtPath<RoverCameraTuning>(RoverAssetPaths.CameraTuning);
            Assert.IsNotNull(_tuning, "RoverCameraTuning asset exists");
        }

        private void StepOnce(BuildFraming framing, Vector3 rover, ref float bearing, Vector2 look = default,
            bool recenter = false, bool driving = false, bool external = false)
        {
            framing.Step(rover, look, recenter, driving, external, ref bearing, _tuning, FrameDt);
        }

        [Test]
        public void TheTurn_IsCalm_AndComesRoundToTheGhost()
        {
            Assert.LessOrEqual(_tuning.BuildFrameMaxRate, MaxAllowedDegreesPerSecond, "No faster than 20 deg/s.");
            var framing = new BuildFraming();
            Vector3 rover = Vector3.zero;
            float bearing = -65f;
            framing.Begin(new Vector3(0f, 0f, 4.5f), bearing);
            Assert.IsTrue(framing.IsActive);
            for (int i = 0; i < 360; i++)
            {
                float before = bearing;
                StepOnce(framing, rover, ref bearing);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(before, bearing)) / FrameDt, MaxAllowedDegreesPerSecond + 1e-3f);
            }

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, bearing), 1f, "It faces the ghost.");
        }

        [Test]
        public void AnyInput_EndsTheTurnAtOnce_AndItDoesNotComeBack()
        {
            foreach (int which in new[] { 0, 1, 2, 3 })
            {
                var framing = new BuildFraming();
                float bearing = -65f;
                framing.Begin(new Vector3(0f, 0f, 4.5f), bearing);
                for (int i = 0; i < 30; i++)
                {
                    StepOnce(framing, Vector3.zero, ref bearing);
                }

                float held = bearing;
                StepOnce(framing, Vector3.zero, ref bearing, look: which == 0 ? new Vector2(2f, 0f) : default,
                    recenter: which == 1, driving: which == 2, external: which == 3);
                Assert.IsFalse(framing.IsActive, "Input " + which + " ends the turn.");
                Assert.IsTrue(framing.PlayerSteeredYaw);
                Assert.AreEqual(held, bearing, 1e-4f, "The view is the player's, input " + which + ".");
                for (int i = 0; i < 60; i++)
                {
                    StepOnce(framing, Vector3.zero, ref bearing);
                }

                Assert.AreEqual(held, bearing, 1e-4f, "It never pulls the view round again.");
            }
        }

        [Test]
        public void AGhostMovedAfterTheTurnEnded_StartsNothing()
        {
            var framing = new BuildFraming();
            framing.Begin(new Vector3(0f, 0f, 4.5f), 0f);
            framing.End();
            framing.MoveGhost(new Vector3(4.5f, 0f, 0f));
            Assert.IsFalse(framing.IsActive);
        }

        [Test]
        public void OnlyAGhostOffScreen_NeedsTheTurn()
        {
            float margin = _tuning.BuildFrameScreenMargin;
            Assert.IsFalse(BuildFraming.IsOffScreen(new Vector3(0.5f, 0.5f, 6f), margin), "In the middle.");
            Assert.IsTrue(BuildFraming.IsOffScreen(new Vector3(0.5f, 0.5f, -2f), margin), "Behind the camera.");
            Assert.IsTrue(BuildFraming.IsOffScreen(new Vector3(1.2f, 0.5f, 6f), margin), "Past the right edge.");
            Assert.IsTrue(BuildFraming.IsOffScreen(new Vector3(0.5f, margin * 0.5f, 6f), margin), "Hard against the bottom.");
        }
    }
}
