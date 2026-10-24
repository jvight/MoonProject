using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BuildFraming"/>: entering build mode (<c>BuildModeState.Placing</c>) eases the
    /// chase camera calmly (<c>&lt;= 20 deg/s</c>, modest pitch, never top-down) to frame 07 and the ghost/apron
    /// together, follows the ghost when it moves, yields immediately to player look input, and eases back on exit.
    /// </summary>
    public sealed class BuildFramingTests
    {
        private const float MaxAllowedDegreesPerSecond = 20f;
        private const float MaxModestElevationDegrees = 30f;
        private const float FrameDt = 1f / 60f;

        private RoverCameraTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = AssetDatabase.LoadAssetAtPath<RoverCameraTuning>(RoverAssetPaths.CameraTuning);
            Assert.IsNotNull(_tuning, "RoverCameraTuning asset exists");
        }

        [Test]
        public void TunedBuildFraming_RespectsCalmRateAndModestPitchBounds()
        {
            Assert.LessOrEqual(_tuning.BuildFrameMaxRate, MaxAllowedDegreesPerSecond,
                "Build-mode camera turn/tilt rate must be <= 20 deg/s.");
            Assert.That(_tuning.BuildFrameElevation, Is.InRange(_tuning.DefaultPitch, MaxModestElevationDegrees),
                "Build-mode chase pitch is modest and never top-down.");
        }

        [Test]
        public void EnteringPlacing_EasesYawAndPitchAtMostTwentyDegreesPerSecond_ToFrame07AndGhost()
        {
            var framing = new BuildFraming();
            Vector3 rover = new Vector3(0f, 0f, -94.5f);
            Vector3 apron = new Vector3(0f, 0f, -90f);
            float bearing = -65f;
            float baseElevation = _tuning.OpeningPitch;

            framing.Begin(rover, apron, snappedToApron: true, currentBearing: bearing);

            for (int i = 0; i < 240; i++)
            {
                float prevBearing = bearing;
                float prevElevOffset = framing.ElevationOffset;
                framing.Step(
                    rover,
                    Vector2.zero,
                    recenter: false,
                    externalYawChange: false,
                    ref bearing,
                    baseElevation,
                    _tuning,
                    FrameDt);

                float yawRate = Mathf.Abs(Mathf.DeltaAngle(prevBearing, bearing)) / FrameDt;
                float pitchRate = Mathf.Abs(framing.ElevationOffset - prevElevOffset) / FrameDt;
                Assert.LessOrEqual(yawRate, MaxAllowedDegreesPerSecond + 1e-3f,
                    "Yaw rate never exceeds 20 deg/s.");
                Assert.LessOrEqual(pitchRate, MaxAllowedDegreesPerSecond + 1e-3f,
                    "Pitch rate never exceeds 20 deg/s.");
                Assert.LessOrEqual(baseElevation + framing.ElevationOffset, MaxModestElevationDegrees,
                    "Chase elevation stays modest, never top-down.");
            }

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, bearing), 1f,
                "Camera bearing eases to face the ghost/apron ahead of 07.");
            Assert.AreEqual(_tuning.BuildFrameElevation, baseElevation + framing.ElevationOffset, 0.5f,
                "Camera elevation settles at the modest build-frame pitch.");
            Assert.Greater(framing.LookShift.z, 0.5f,
                "Look target shifts toward the ghost to frame 07 and the ghost together.");
        }

        [Test]
        public void MovingTheGhost_UpdatesLookShiftToFollow_AndLookInputTakesOverWithoutResistance()
        {
            var framing = new BuildFraming();
            Vector3 rover = Vector3.zero;
            Vector3 ghostNorth = new Vector3(0f, 0f, 4.5f);
            float bearing = -45f;

            framing.Begin(rover, ghostNorth, snappedToApron: false, currentBearing: bearing);
            for (int i = 0; i < 60; i++)
            {
                framing.Step(rover, Vector2.zero, false, false, ref bearing, _tuning.DefaultPitch, _tuning, FrameDt);
            }

            // Player provides horizontal look input: auto-yaw stops immediately.
            float bearingAtInput = bearing;
            framing.Step(rover, new Vector2(2f, 0f), false, false, ref bearing, _tuning.DefaultPitch, _tuning, FrameDt);
            Assert.IsTrue(framing.PlayerSteeredYaw, "Look input marks yaw as player-steered.");
            Assert.AreEqual(bearingAtInput, bearing, 1e-4f, "BuildFraming does not override player yaw input.");

            // Moving the ghost East shifts LookShift East to follow the ghost.
            Vector3 ghostEast = new Vector3(4.5f, 0f, 0f);
            framing.MoveGhost(rover, ghostEast, snappedToApron: false);
            for (int i = 0; i < 60; i++)
            {
                framing.Step(rover, Vector2.zero, false, false, ref bearing, _tuning.DefaultPitch, _tuning, FrameDt);
            }

            Assert.Greater(framing.LookShift.x, 0.5f, "LookShift follows the ghost to the East.");
            Assert.Less(Mathf.Abs(framing.LookShift.z), 0.1f, "Northward shift decays as the ghost moves East.");

            // Leaving build mode eases Weight, LookShift, PullBack, and ElevationOffset back to zero.
            framing.End();
            for (int i = 0; i < 120; i++)
            {
                float prevElevOffset = framing.ElevationOffset;
                framing.Step(rover, Vector2.zero, false, false, ref bearing, _tuning.DefaultPitch, _tuning, FrameDt);
                float pitchRate = Mathf.Abs(framing.ElevationOffset - prevElevOffset) / FrameDt;
                Assert.LessOrEqual(pitchRate, MaxAllowedDegreesPerSecond + 1e-3f);
            }

            Assert.AreEqual(0f, framing.Weight, 1e-3f, "Weight eases back to 0 on exit.");
            Assert.AreEqual(0f, framing.ElevationOffset, 1e-3f, "ElevationOffset eases back to 0 on exit.");
            Assert.AreEqual(0f, framing.PullBack, 1e-3f, "PullBack eases back to 0 on exit.");
            Assert.Less(framing.LookShift.magnitude, 1e-3f, "LookShift eases back to zero on exit.");
        }
    }
}
