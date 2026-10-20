using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The next-action nudge's rules (G5): once per target, only off screen.</summary>
    public sealed class GuidanceNudgeTests
    {
        private const float Margin = 0.08f;

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
        public void AnObjectInTheMiddleOfTheView_IsOnScreen()
        {
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.5f, 0.5f, 12f), Margin));
            Assert.IsFalse(GuidanceNudge.OffScreen(new Vector3(0.1f, 0.9f, 12f), Margin));
        }

        [Test]
        public void AnObjectBesideBehindOrBeyondTheEdge_IsOffScreen()
        {
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(-0.2f, 0.5f, 12f), Margin), "left of the view");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(0.5f, 1.3f, 12f), Margin), "above it");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(0.5f, 0.5f, -4f), Margin), "behind the camera");
            Assert.IsTrue(GuidanceNudge.OffScreen(new Vector3(0.97f, 0.5f, 12f), Margin), "hugging the edge");
        }
    }
}
