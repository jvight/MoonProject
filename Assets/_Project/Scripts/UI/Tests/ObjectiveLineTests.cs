using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The objective line's rule (opening v3 guidance audit G2, G4): it wants the screen the moment Gameplay gives a
    /// line and for as long as Gameplay keeps it; a next action after a success is the one element, a reminder is the
    /// quiet line; the key's glyph comes first only when Gameplay says the key is new; Guidance off, nothing shows.
    /// </summary>
    public sealed class ObjectiveLineTests
    {
        private const string BuildPad = "next.v2.build_pad";

        private ObjectiveLine _line;

        [SetUp]
        public void SetUp()
        {
            _line = new ObjectiveLine();
        }

        [Test]
        public void ANextLine_ShowsAtOnce_AsTheNextAction_WithItsNewKeysGlyph_UntilGameplayTakesItAway()
        {
            _line.Set(new NextStepChanged(BuildPad, NextLineReason.Next, RoverAction.Build, true, false, null,
                Vector3.zero));
            _line.Tick(true);
            Assert.IsTrue(_line.WantsShown, "right after the success, no waiting for 07 to settle");
            Assert.IsTrue(_line.IsNext, "the next action: the one element");
            Assert.IsTrue(_line.ShowsKey);
            Assert.AreEqual(RoverAction.Build, _line.Action);

            _line.Set(new NextStepChanged(string.Empty));
            _line.Tick(true);
            Assert.IsFalse(_line.WantsShown, "Gameplay's few seconds are over");
            Assert.IsFalse(_line.ShowsKey);
        }

        [Test]
        public void AReminder_IsTheQuietLine_AndAKeyAlreadyKnown_ShowsNoGlyph()
        {
            _line.Set(new NextStepChanged(BuildPad, NextLineReason.Reminder, RoverAction.Build, false, false, null,
                Vector3.zero));
            _line.Tick(true);
            Assert.IsTrue(_line.WantsShown);
            Assert.IsFalse(_line.IsNext, "a reminder waits behind any chip");
            Assert.IsFalse(_line.ShowsKey, "the key was used lately: words only");
        }

        [Test]
        public void GuidanceOff_ShowsNoLine()
        {
            _line.Set(new NextStepChanged(BuildPad));
            _line.Tick(false);
            Assert.IsFalse(_line.WantsShown);
            Assert.IsFalse(_line.IsNext);
        }
    }
}
