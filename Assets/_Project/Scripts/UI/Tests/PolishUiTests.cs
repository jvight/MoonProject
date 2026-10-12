using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Polish wave 1 (docs/design/polish-pass.md): the save tick shows for a moment after a real save and a second
    /// save keeps it up; a sleeping station's "not yet" line is said once per reason.
    /// </summary>
    public sealed class PolishUiTests
    {
        private const float Step = 0.05f;

        [Test]
        public void TheSaveTick_ComesUp_RestsItsHold_AndEasesAway()
        {
            var settings = new SaveTickSettings();
            var tick = new SaveTick(Layout(), settings);
            Assert.IsFalse(tick.IsVisible, "nothing until the game saves");

            tick.Saved();
            Run(tick, settings.Reveal.FadeIn + 0.1f);
            Assert.IsTrue(tick.IsVisible);
            Run(tick, settings.HoldSeconds);
            Run(tick, settings.Reveal.FadeOut + 0.3f);
            Assert.IsFalse(tick.IsVisible, "gone a moment after its hold");
        }

        [Test]
        public void ABurstOfSaves_ReadsAsOneSign_AndALaterSaveShowsItAgain()
        {
            var settings = new SaveTickSettings();
            var tick = new SaveTick(Layout(), settings);
            tick.Saved();
            Run(tick, settings.HoldSeconds * 0.8f);
            tick.Saved();
            Run(tick, settings.HoldSeconds * 0.8f);
            tick.Saved();
            Run(tick, settings.HoldSeconds + settings.Reveal.FadeOut + 0.3f);
            Assert.IsFalse(tick.IsVisible, "the repeats inside the burst did not keep it up");

            Run(tick, settings.BurstSeconds);
            tick.Saved();
            Run(tick, settings.Reveal.FadeIn + 0.1f);
            Assert.IsTrue(tick.IsVisible, "a save after the burst shows the sign again");
        }

        [Test]
        public void ANotYetLine_IsSaidOncePerReason()
        {
            var lines = new NotYetLines();
            Assert.IsTrue(lines.ShouldSay("notyet.tower"));
            Assert.IsFalse(lines.ShouldSay("notyet.tower"), "a second ping of the same sleeper stays quiet");
            Assert.IsTrue(lines.ShouldSay("notyet.bay"), "another sleeper has its own line");
            Assert.IsFalse(lines.ShouldSay(string.Empty));
            Assert.IsFalse(lines.ShouldSay(null));
        }

        private static UiLayout Layout()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            return new UiLayout(uxml.Instantiate());
        }

        private static void Run(SaveTick tick, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                tick.Tick(Step);
            }
        }
    }
}
