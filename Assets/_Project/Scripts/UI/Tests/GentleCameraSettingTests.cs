using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The Gentle camera row (docs/design/first-hour-v2.md §7.4): off by default (older saves too), saved, and while
    /// on the look slider offers one step lower, its floor.
    /// </summary>
    public sealed class GentleCameraSettingTests
    {
        [Test]
        public void GentleCamera_IsOffByDefault_OffersALowerLookStep_AndRoundTrips()
        {
            var services = new FakeSettingsServices();
            ILocalization localization = TestTables.Localization(new EventBus(), "next.tower", "x", "y");
            var pause = new PauseSettings();
            var settings = new PlayerSettings(services, services, services, new FakeDisplayControl(), localization, pause);
            settings.Restore(new SettingsSaveData());
            Assert.IsFalse(settings.GentleCamera, "a save from before the setting keeps it off");
            int usual = settings.MinLookStep;

            settings.GentleCamera = true;
            Assert.IsTrue(settings.IsDirty);
            Assert.AreEqual(usual - 1, settings.MinLookStep);
            settings.SetLookStep(settings.MinLookStep);
            Assert.AreEqual(pause.GentleLookMin, services.Sensitivity, 1e-5f);
            Assert.AreEqual(settings.MinLookStep, settings.GetLookStep());
            Assert.Less(pause.GentleLookMin, settings.LookValue(usual));

            var other = new FakeSettingsServices();
            var restored = new PlayerSettings(other, other, other, new FakeDisplayControl(), localization, pause);
            restored.Restore(settings.Capture());
            Assert.IsTrue(restored.GentleCamera);
            Assert.AreEqual(pause.GentleLookMin, other.Sensitivity, 1e-5f, "the low look survives the reload");
        }
    }
}
