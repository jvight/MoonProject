using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The chase camera while 07 tows (<see cref="TowFraming"/>): the load behind 07 is an obstacle; the camera rises and
    /// draws back just enough to see 07 over it, never has its eye inside it, and leaves a clear view alone.
    /// </summary>
    public sealed class TowFramingTests
    {
        private const float Margin = 0.5f;
        private const float MaxElevation = 52f;
        private const float MaxScale = 1.8f;
        private const float Radius = 8f;
        private const float BaseElevation = 14f;

        // 07 faces +Z, so the chase camera (yaw 0) sits behind it at -Z, where the towed load trails.
        private static readonly Vector3 Target = new Vector3(0f, 1.2f, 0f);

        private static Bounds LoadBehind()
        {
            return new Bounds(new Vector3(0f, 0.7f, -3.2f), new Vector3(1.6f, 1.4f, 2.4f));
        }

        private static void Solve(float yaw, Bounds load, out float elevation, out float scale)
        {
            TowFraming.Solve(Target, yaw, BaseElevation, Radius, load, Margin, MaxElevation, MaxScale, out elevation,
                out scale);
        }

        [Test]
        public void ALoadBehind07_HidesIt_FromACameraStraightBehind()
        {
            Bounds obstacle = LoadBehind();
            obstacle.Expand(Margin * 2f);
            Assert.IsFalse(TowFraming.Clear(Target, 0f, BaseElevation, Radius, obstacle),
                "at the chase elevation the load stands between the camera and 07");
        }

        [Test]
        public void TheSolution_RisesAndDrawsBack_UntilTheLineToO7IsClear_AndTheEyeIsOutsideTheLoad()
        {
            Solve(0f, LoadBehind(), out float elevation, out float scale);
            Bounds obstacle = LoadBehind();
            obstacle.Expand(Margin * 2f);
            Assert.Greater(elevation, BaseElevation, "the camera rises");
            Assert.GreaterOrEqual(scale, 1f);
            Assert.LessOrEqual(elevation, MaxElevation);
            Assert.LessOrEqual(scale, MaxScale);
            Assert.IsTrue(TowFraming.Clear(Target, 0f, elevation, Radius * scale, obstacle), "and 07 is seen over the load");
            Assert.IsFalse(obstacle.Contains(TowFraming.EyeAt(Target, 0f, elevation, Radius * scale)),
                "with the eye outside the load");
        }

        [Test]
        public void ASideSwing_ClearsTheLoad_WithLessRiseThanStraightBehind_AndNeverBeyondItsLimit()
        {
            Bounds obstacle = LoadBehind();
            obstacle.Expand(Margin * 2f);
            Solve(0f, LoadBehind(), out float straightElevation, out float straightScale);
            TowFraming.SolveSide(Target, 0f, BaseElevation, Radius, LoadBehind(), Margin, MaxElevation, MaxScale, 45f, 1f,
                out float elevation, out float scale, out float swing);

            Assert.LessOrEqual(Mathf.Abs(swing), 45f);
            Assert.AreNotEqual(0f, swing, "the camera swings to a side");
            Assert.LessOrEqual(elevation, straightElevation, "and needs no more rise than straight behind");
            Assert.IsTrue(TowFraming.Clear(Target, swing, elevation, Radius * scale, obstacle));
        }

        [Test]
        public void ASideSwing_KeepsTheSideItHad_WhenBothClear()
        {
            TowFraming.SolveSide(Target, 0f, BaseElevation, Radius, LoadBehind(), Margin, MaxElevation, MaxScale, 45f, -1f,
                out _, out _, out float swing);

            Assert.LessOrEqual(swing, 0f, "it stays on the side it was on");
        }

        [Test]
        public void ASideSwing_IsNotUsed_ForAClearView()
        {
            TowFraming.SolveSide(Target, 90f, BaseElevation, Radius, LoadBehind(), Margin, MaxElevation, MaxScale, 45f, 1f,
                out float elevation, out float scale, out float swing);

            Assert.AreEqual(0f, swing);
            Assert.AreEqual(BaseElevation, elevation, 1e-3f);
            Assert.AreEqual(1f, scale, 1e-3f);
        }

        [Test]
        public void AClearView_IsLeftAlone_WhateverWayThePlayerSwingsIt()
        {
            foreach (float yaw in new[] { 90f, -90f, 180f, 135f })
            {
                Solve(yaw, LoadBehind(), out float elevation, out float scale);
                Assert.AreEqual(BaseElevation, elevation, 1e-4f, $"yaw {yaw}: no need to rise");
                Assert.AreEqual(1f, scale, 1e-4f, $"yaw {yaw}: no need to draw back");
            }
        }

        [Test]
        public void ALoadFarOff_ChangesNothing()
        {
            var far = new Bounds(new Vector3(0f, 0.7f, -40f), new Vector3(1.6f, 1.4f, 2.4f));
            Solve(0f, far, out float elevation, out float scale);
            Assert.AreEqual(BaseElevation, elevation, 1e-4f);
            Assert.AreEqual(1f, scale, 1e-4f);
        }

        [Test]
        public void AnEyeInsideTheLoad_IsAlwaysMovedOut()
        {
            var big = new Bounds(new Vector3(0f, 1f, -2f), new Vector3(6f, 3f, 12f));
            Solve(0f, big, out float elevation, out float scale);
            Bounds obstacle = big;
            obstacle.Expand(Margin * 2f);
            Assert.IsFalse(obstacle.Contains(TowFraming.EyeAt(Target, 0f, elevation, Radius * scale)),
                "even a load as big as a hut never holds the eye");
        }

        [Test]
        public void TheCheapestWayIsTaken_NotTheMostExtreme()
        {
            Solve(0f, LoadBehind(), out float elevation, out float scale);
            Assert.Less(elevation, MaxElevation - 5f, "no more height than it needs");
            Assert.Less(scale, MaxScale - 0.1f, "no more distance than it needs");
        }

        [Test]
        public void TheSolution_FollowsTheYaw_Smoothly()
        {
            float previous = -1f;
            float biggestStep = 0f;
            for (float yaw = -60f; yaw <= 60f; yaw += 2f)
            {
                Solve(yaw, LoadBehind(), out float elevation, out float scale);
                float cost = elevation / MaxElevation + scale / MaxScale;
                if (previous >= 0f)
                {
                    biggestStep = Mathf.Max(biggestStep, Mathf.Abs(cost - previous));
                }

                previous = cost;
            }

            Assert.Less(biggestStep, 0.6f, "swinging the view never flips the solution wildly (the rig smooths it too)");
        }
    }
}
