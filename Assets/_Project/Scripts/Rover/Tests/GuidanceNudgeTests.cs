using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The next-action nudge's rules (G5): once per target, only off screen horizontally and far enough.</summary>
    public sealed class GuidanceNudgeTests
    {
        private const float Margin = 0.08f;
        private const float MinDistance = 12f;
        private const float Slack = 30f;
        private const float MaxBearing = 150f;

        [Test]
        public void ATarget_IsNudgedTowardOnce()
        {
            var nudge = new GuidanceNudge();

            Assert.IsTrue(nudge.TryTake("relay.1"));
            Assert.IsFalse(nudge.TryTake("relay.1"), "never twice for one target");
            Assert.IsTrue(nudge.TryTake("apron"), "another target has its own");
        }

        [Test]
        public void ATargetWithoutAnId_IsABug()
        {
            Assert.Throws<ArgumentException>(() => new GuidanceNudge().TryTake(""));
        }

        [Test]
        public void AnObjectInTheHorizontalField_IsOnScreen_RegardlessOfVerticalPosition()
        {
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.5f, 0.5f, 12f), Margin));
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.1f, 0.9f, 12f), Margin));
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.5f, 1.3f, 12f), Margin), "above the view but within horizontal field");
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.5f, -0.2f, 12f), Margin), "below the view but within horizontal field");
        }

        [Test]
        public void AnObjectBesideBehindOrBeyondTheHorizontalEdge_IsOffScreen()
        {
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(-0.2f, 0.5f, 12f), Margin), "left of the view");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(1.2f, 0.5f, 12f), Margin), "right of the view");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(0.5f, 0.5f, -4f), Margin), "behind the camera");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(0.97f, 0.5f, 12f), Margin), "hugging the horizontal edge");
        }

        [Test]
        public void ShouldNudge_SkipsCloseTargets_AndTargetsInViewOrWithinSlack()
        {
            Vector3 rover = Vector3.zero;
            Vector3 closeOffScreen = new Vector3(8f, 0f, 0f);
            Vector3 farOffScreen = new Vector3(40f, 0f, 0f);
            Vector3 farWithinSlack = new Vector3(10f, 0f, 40f);

            Assert.IsFalse(
                GuidanceNudge.ShouldNudge(rover, closeOffScreen, 0f, new Vector3(1.4f, 0.5f, 4f), Margin, MinDistance, Slack, MaxBearing),
                "within 12 m: 07's head look is enough");
            Assert.IsFalse(
                GuidanceNudge.ShouldNudge(rover, farOffScreen, 0f, new Vector3(0.97f, 0.5f, 20f), Margin, MinDistance, Slack, MaxBearing),
                "already in view near the edge: never move the camera so a visible target can leave the view");
            Assert.IsFalse(
                GuidanceNudge.ShouldNudge(rover, farOffScreen, 0f, new Vector3(0.5f, 0.5f, 20f), Margin, MinDistance, Slack, MaxBearing),
                "already in the horizontal field: no nudge");
            Assert.IsFalse(
                GuidanceNudge.ShouldNudge(rover, farWithinSlack, 0f, new Vector3(1.1f, 0.5f, 20f), Margin, MinDistance, Slack, MaxBearing),
                "within slack of looking bearing: no nudge");
            Assert.IsTrue(
                GuidanceNudge.ShouldNudge(rover, farOffScreen, 0f, new Vector3(1.4f, 0.5f, 20f), Margin, MinDistance, Slack, MaxBearing),
                "far and off screen horizontally: nudge");
        }
    }
}
