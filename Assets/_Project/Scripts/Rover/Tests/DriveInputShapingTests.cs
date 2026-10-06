using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class DriveInputShapingTests
    {
        [Test]
        public void NormalisedDiagonal_BecomesFullThrottleAndFullSteer()
        {
            Vector2 shaped = DriveInputShaping.CircleToSquare(new Vector2(1f, 1f).normalized);
            Assert.AreEqual(1f, shaped.x, 1e-5f);
            Assert.AreEqual(1f, shaped.y, 1e-5f);
        }

        [Test]
        public void SingleAxis_IsUnchanged()
        {
            Assert.AreEqual(new Vector2(0f, -0.6f), DriveInputShaping.CircleToSquare(new Vector2(0f, -0.6f)));
            Assert.AreEqual(new Vector2(1f, 0f), DriveInputShaping.CircleToSquare(new Vector2(1f, 0f)));
        }

        [Test]
        public void PartialDiagonal_KeepsItsDirectionAndMagnitudeOnTheLongerAxis()
        {
            Vector2 input = new Vector2(0.3f, 0.4f);
            Vector2 shaped = DriveInputShaping.CircleToSquare(input);
            Assert.AreEqual(0.5f, shaped.y, 1e-5f);
            Assert.AreEqual(0f, Vector2.SignedAngle(input, shaped), 1e-3f);
        }

        [Test]
        public void ZeroAndOversizedInput_AreSafe()
        {
            Assert.AreEqual(Vector2.zero, DriveInputShaping.CircleToSquare(Vector2.zero));
            Vector2 shaped = DriveInputShaping.CircleToSquare(new Vector2(3f, 0f));
            Assert.AreEqual(1f, shaped.x, 1e-5f);
        }
    }
}
