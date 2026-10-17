using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>Guidance is on by default (older saves too) and saved.</summary>
    public sealed class GuidanceTests
    {
        [Test]
        public void Guidance_IsOnByDefault_EvenForOlderSaves_AndRoundTrips()
        {
            var services = new FakeSettingsServices();
            ILocalization localization = TestTables.Localization(new EventBus(), "next.v2.build_pad", "x", "y");
            var settings = new PlayerSettings(services, services, services, new FakeDisplayControl(), localization,
                new PauseSettings());
            Assert.IsTrue(settings.Guidance);
            settings.Restore(new SettingsSaveData());
            Assert.IsTrue(settings.Guidance, "a save from before the setting keeps it on");

            settings.Guidance = false;
            Assert.IsTrue(settings.IsDirty);
            var restored = new PlayerSettings(services, services, services, new FakeDisplayControl(), localization,
                new PauseSettings());
            restored.Restore(settings.Capture());
            Assert.IsFalse(restored.Guidance);
        }
    }
}
