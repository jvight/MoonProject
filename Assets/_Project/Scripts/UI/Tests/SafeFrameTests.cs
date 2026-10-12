using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    public sealed class SafeFrameTests
    {
        private const float HudAspect = 2.3333f;
        private static readonly Rect WholeScreen = new Rect(0f, 0f, 1f, 1f);

        [Test]
        public void ASixteenByNineScreen_NeedsNoInsets_ForAnyAspectCapAtOrAboveIt()
        {
            FrameInsets insets = SafeFrame.Compute(1920f, 1080f, HudAspect, WholeScreen);

            Assert.AreEqual(0f, insets.Left);
            Assert.AreEqual(0f, insets.Right);
            Assert.AreEqual(0f, insets.Top);
            Assert.AreEqual(0f, insets.Bottom);
        }

        [Test]
        public void ASuperWideScreen_IsPulledInToTheCapOnBothSides()
        {
            FrameInsets insets = SafeFrame.Compute(3840f, 1080f, HudAspect, WholeScreen);

            Assert.AreEqual((3840f - 1080f * HudAspect) * 0.5f, insets.Left, 0.01f);
            Assert.AreEqual(insets.Left, insets.Right, 0.01f);
        }

        [Test]
        public void ANarrowScreen_NeverGetsNegativeInsets()
        {
            FrameInsets insets = SafeFrame.Compute(1440f, 1080f, 1.7778f, WholeScreen);

            Assert.AreEqual(0f, insets.Left);
            Assert.AreEqual(0f, insets.Right);
        }

        [Test]
        public void ADevicesUnsafeEdges_WinWhereTheyAreDeeperThanTheCap()
        {
            var safe = new Rect(0.05f, 0.02f, 0.9f, 0.94f);

            FrameInsets insets = SafeFrame.Compute(1920f, 1080f, HudAspect, safe);

            Assert.AreEqual(96f, insets.Left, 0.01f);
            Assert.AreEqual(96f, insets.Right, 0.01f);
            Assert.AreEqual(21.6f, insets.Bottom, 0.01f);
            Assert.AreEqual(1080f * 0.04f, insets.Top, 0.01f);
        }

        [Test]
        public void APanelWithNoSizeYet_GetsNoInsets()
        {
            FrameInsets insets = SafeFrame.Compute(0f, 0f, HudAspect, WholeScreen);

            Assert.AreEqual(0f, insets.Left);
            Assert.AreEqual(0f, insets.Top);
        }
    }
}
