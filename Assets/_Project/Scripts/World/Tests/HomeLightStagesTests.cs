using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// M3-16's opening and wake: a fresh game is dark but for the dock, Home wakes the windows, then the porch lamps,
    /// then the halo, each eased; later stages light the bay and the lift; a restored stage applies at once.
    /// </summary>
    public sealed class HomeLightStagesTests
    {
        private const float Start = 10f;
        private const float Tolerance = 1e-4f;

        private HomeLightingSettings _settings;
        private HomeLightStages _stages;

        [SetUp]
        public void SetUp()
        {
            _settings = new HomeLightingSettings();
            _stages = new HomeLightStages(_settings);
        }

        [Test]
        public void FreshGame_IsDarkButForTheDock()
        {
            Assert.AreEqual(1f, _stages.Level(HomeLight.Dock, Start));
            foreach (HomeLight light in new[]
                     {
                         HomeLight.Windows, HomeLight.PorchLamps, HomeLight.Halo, HomeLight.BayLamps,
                         HomeLight.LiftLamps,
                     })
            {
                Assert.AreEqual(0f, _stages.Level(light, Start), light.ToString());
            }
        }

        [Test]
        public void Home_WakesWindowsThenPorchLampsThenHalo_Eased()
        {
            _stages.SetStage(BasePowerStage.Home, false, Start);
            float mid = Start + _settings.WakeStagger + _settings.WakeFade * 0.5f;
            float windows = _stages.Level(HomeLight.Windows, mid);
            float porch = _stages.Level(HomeLight.PorchLamps, mid);
            float halo = _stages.Level(HomeLight.Halo, mid);
            Assert.That(windows, Is.GreaterThan(porch), "windows first");
            Assert.That(porch, Is.GreaterThan(halo), "then the porch lamps");
            Assert.AreEqual(0.5f, porch, Tolerance, "each eases symmetrically through its fade");
            Assert.AreEqual(0f, _stages.Level(HomeLight.Windows, Start), "no frame-one jump");
            Assert.AreEqual(0f, _stages.Level(HomeLight.BayLamps, mid), "the bay waits for its own stage");

            float done = Start + 2f * _settings.WakeStagger + _settings.WakeFade;
            Assert.AreEqual(1f, _stages.Level(HomeLight.Halo, done), Tolerance);
        }

        [Test]
        public void LaterStages_LightTheBayThenTheLift()
        {
            _stages.SetStage(BasePowerStage.Home, true, Start);
            _stages.SetStage(BasePowerStage.Bay, false, Start);
            float done = Start + _settings.WakeFade;
            Assert.AreEqual(1f, _stages.Level(HomeLight.Windows, Start), "home stays lit");
            Assert.AreEqual(1f, _stages.Level(HomeLight.BayLamps, done), Tolerance);
            Assert.AreEqual(0f, _stages.Level(HomeLight.LiftLamps, done));

            _stages.SetStage(BasePowerStage.Lift, false, done);
            Assert.AreEqual(1f, _stages.Level(HomeLight.LiftLamps, done + _settings.WakeFade), Tolerance);
        }

        [Test]
        public void RestoredStage_AppliesAtOnce()
        {
            _stages.SetStage(BasePowerStage.Lift, true, Start);
            foreach (HomeLight light in System.Enum.GetValues(typeof(HomeLight)))
            {
                Assert.AreEqual(1f, _stages.Level(light, Start), light.ToString());
            }
        }

        [Test]
        public void EveryWarmPoint_WakesWithAStage()
        {
            Assert.AreEqual(BasePowerStage.Asleep, HomeLightStages.WakesWith(HomeLight.Dock));
            Assert.AreEqual(BasePowerStage.Home, HomeLightStages.WakesWith(HomeLight.Halo));
            Assert.AreEqual(BasePowerStage.Bay, HomeLightStages.WakesWith(HomeLight.BayLamps));
            Assert.AreEqual(BasePowerStage.Lift, HomeLightStages.WakesWith(HomeLight.LiftLamps));
        }
    }
}
