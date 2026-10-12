using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The soft "next" line shows a teaching step's invitation as soon as 07 is still (M3-24 B) and waits a long while
    /// on a recovery target's need first (P8); it shows only while 07 is still, never while driving, and not with
    /// Guidance off; Guidance is on by default (older saves too) and saved.
    /// </summary>
    public sealed class GuidanceTests
    {
        private const float Step = 0.1f;

        [Test]
        public void ATargetNeed_WaitsFirst_ThenShowsOnlyWhile07IsStill_AndHidesForAnEmptyStep()
        {
            var settings = new GuidanceSettings();
            var line = new NextStepLine(settings);
            line.SetKey("next.power.part");
            Ticks(line, settings.StepPatienceSeconds - 1f, 0f, true);
            Assert.IsFalse(line.WantsShown, "patience first: a player finding their own way never sees it");
            line.SetKey("next.link");
            Ticks(line, settings.StepPatienceSeconds - 1f, 0f, true);
            Assert.IsFalse(line.WantsShown, "progress to a new step starts the wait again");
            line.SetKey("next.link");
            Ticks(line, 1f + settings.StillSeconds, 3f, true);
            Assert.IsFalse(line.WantsShown, "hidden while driving");
            line.SetKey("next.power.part");
            Ticks(line, settings.StepPatienceSeconds, 3f, true);

            Ticks(line, settings.StillSeconds * 0.5f, 0f, true);
            Assert.IsFalse(line.WantsShown, "a moment of stillness first");
            Ticks(line, settings.StillSeconds, 0f, true);
            Assert.IsTrue(line.WantsShown);
            Assert.AreEqual("next.power.part", line.Key);

            line.Tick(Step, 3f, true);
            Assert.IsFalse(line.WantsShown, "driving hides it at once (the view eases it out)");
            Ticks(line, settings.StillSeconds + Step, 0f, false);
            Assert.IsFalse(line.WantsShown, "Guidance off");
            line.SetKey(string.Empty);
            Ticks(line, Step, 0f, true);
            Assert.IsFalse(line.WantsShown, "no step to suggest");
            line.SetKey(null);
            Assert.AreEqual(string.Empty, line.Key);
        }

        [Test]
        public void ATeachingStep_ShowsAsSoonAs07Stops_WithoutTheWait()
        {
            var settings = new GuidanceSettings();
            var line = new NextStepLine(settings);
            line.SetKey("next.tower");
            Ticks(line, 2f, 3f, true);
            Assert.IsFalse(line.WantsShown, "hidden while driving");
            Ticks(line, settings.StillSeconds * 0.5f, 0f, true);
            Assert.IsFalse(line.WantsShown, "a moment of stillness first");
            Ticks(line, settings.StillSeconds, 0f, true);
            Assert.IsTrue(line.WantsShown, "a teaching step needs no patience (M3-24 B)");
            line.SetKey("next.signal");
            Ticks(line, settings.StillSeconds + Step, 0f, true);
            Assert.IsTrue(line.WantsShown, "the next teaching step shows at once too");
            Ticks(line, Step, 0f, false);
            Assert.IsFalse(line.WantsShown, "Guidance off");
        }

        [Test]
        public void Guidance_IsOnByDefault_EvenForOlderSaves_AndRoundTrips()
        {
            var services = new FakeSettingsServices();
            ILocalization localization = TestTables.Localization(new EventBus(), "next.tower", "x", "y");
            var settings = new PlayerSettings(services, services, services, localization, new PauseSettings());
            Assert.IsTrue(settings.Guidance);
            settings.Restore(new SettingsSaveData());
            Assert.IsTrue(settings.Guidance, "a save from before the setting keeps it on");

            settings.Guidance = false;
            Assert.IsTrue(settings.IsDirty);
            var restored = new PlayerSettings(services, services, services, localization, new PauseSettings());
            restored.Restore(settings.Capture());
            Assert.IsFalse(restored.Guidance);
        }

        private static void Ticks(NextStepLine line, float seconds, float speed, bool guidance)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                line.Tick(Step, speed, guidance);
            }
        }
    }
}
