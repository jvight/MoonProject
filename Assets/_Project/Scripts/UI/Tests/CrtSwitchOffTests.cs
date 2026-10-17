using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The title CRT switching off: a squeeze to a line, then to a dot (opening-cutscene-research Q1).</summary>
    public sealed class CrtSwitchOffTests
    {
        private const float Seconds = 0.6f;
        private const float LineShare = 0.6f;
        private const float Thickness = 0.02f;

        [Test]
        public void BeforeItStarts_ThePictureIsWhole()
        {
            var off = new CrtSwitchOff(Seconds, LineShare, Thickness);

            Assert.IsFalse(off.IsRunning);
            Assert.AreEqual(1f, off.ScaleX);
            Assert.AreEqual(1f, off.ScaleY);
        }

        [Test]
        public void ItSqueezesToALineFirst_ThenShrinksTheLineToADot()
        {
            var off = new CrtSwitchOff(Seconds, LineShare, Thickness);
            off.Start();

            off.Tick(Seconds * LineShare * 0.5f);
            Assert.AreEqual(1f, off.ScaleX, "still full width while it squeezes");
            Assert.Less(off.ScaleY, 1f);
            Assert.Greater(off.ScaleY, Thickness);

            off.Tick(Seconds * LineShare * 0.5f + 0.001f);
            Assert.AreEqual(Thickness, off.ScaleY, 0.001f, "a thin line");
            Assert.AreEqual(1f, off.ScaleX, 0.05f, "line at full width");

            off.Tick(Seconds * (1f - LineShare) * 0.5f);
            Assert.Less(off.ScaleX, 1f, "the line shortens");
            Assert.AreEqual(Thickness, off.ScaleY, 0.001f);
        }

        [Test]
        public void ItEndsOnADot_AndSaysSoOnce()
        {
            var off = new CrtSwitchOff(Seconds, LineShare, Thickness);
            off.Start();

            Assert.IsFalse(off.Tick(Seconds * 0.5f));
            Assert.IsTrue(off.Tick(Seconds), "finishes on this tick");
            Assert.IsFalse(off.IsRunning);
            Assert.AreEqual(0f, off.ScaleX);
            Assert.IsFalse(off.Tick(Seconds), "and only once");
        }

        [Test]
        public void ADurationOfNothing_IsRefused()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CrtSwitchOff(0f, LineShare, Thickness));
        }
    }
}
