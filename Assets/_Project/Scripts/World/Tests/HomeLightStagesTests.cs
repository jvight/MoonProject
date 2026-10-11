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

        // Forward+ takes many lights, but each pool costs per-pixel work across the base: a handful.
        private const int MaxPools = 8;

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
            float mid = Start + _settings.CableTravel + _settings.WakeStagger + _settings.WakeFade * 0.5f;
            float windows = _stages.Level(HomeLight.Windows, mid);
            float porch = _stages.Level(HomeLight.PorchLamps, mid);
            float halo = _stages.Level(HomeLight.Halo, mid);
            Assert.That(windows, Is.GreaterThan(porch), "windows first");
            Assert.That(porch, Is.GreaterThan(halo), "then the porch lamps");
            Assert.AreEqual(0.5f, porch, Tolerance, "each eases symmetrically through its fade");
            Assert.AreEqual(0f, _stages.Level(HomeLight.Windows, Start), "no frame-one jump");
            Assert.AreEqual(0f, _stages.Level(HomeLight.BayLamps, mid), "the bay waits for its own stage");

            float done = Start + _settings.CableTravel + 2f * _settings.WakeStagger + _settings.WakeFade;
            Assert.AreEqual(1f, _stages.Level(HomeLight.Halo, done), Tolerance);
        }

        [Test]
        public void EveryStage_AddsAWarmPool_WithinTheLightBudget()
        {
            var pooled = new bool[(int)BasePowerStage.Lift + 1];
            foreach (LightPoolTuning pool in _settings.Pools)
            {
                pooled[(int)HomeLightStages.WakesWith(pool.WakesWith)] = true;
                Assert.That(pool.Intensity, Is.GreaterThan(0f), pool.Anchor);
                Assert.That(pool.Disc || pool.Offset.y > 0f, Is.True, pool.Anchor + "'s light hangs above the ground");
            }

            for (int stage = 0; stage < pooled.Length; stage++)
            {
                Assert.IsTrue(pooled[stage], $"{(BasePowerStage)stage} widens home's warm floor");
            }

            Assert.LessOrEqual(_settings.Pools.Count, MaxPools, "each pool is a per-pixel light at the base");
        }

        [Test]
        public void Grade_WarmsAStepWithEachStage_Eased()
        {
            Assert.AreEqual(0f, _stages.GradeWarmth(Start), "a sleeping base keeps the cold grade");
            _stages.SetStage(BasePowerStage.Home, false, Start);
            Assert.AreEqual(0f, _stages.GradeWarmth(Start), "no frame-one jump");
            float woken = Start + _settings.CableTravel + _settings.WakeFade;
            float home = _stages.GradeWarmth(woken);
            Assert.AreEqual(1f / 3f, home, Tolerance, "home is a third of the way");

            _stages.SetStage(BasePowerStage.Bay, false, woken);
            float easing = _stages.GradeWarmth(woken + _settings.CableTravel + _settings.WakeFade * 0.5f);
            Assert.That(easing, Is.GreaterThan(home).And.LessThan(2f / 3f), "the bay's step eases in");

            _stages.SetStage(BasePowerStage.Lift, true, woken);
            Assert.AreEqual(1f, _stages.GradeWarmth(woken), Tolerance, "a restored Lift is fully warm at once");
        }

        [Test]
        public void LaterStages_LightTheBayThenTheLift()
        {
            _stages.SetStage(BasePowerStage.Home, true, Start);
            _stages.SetStage(BasePowerStage.Bay, false, Start);
            float done = Start + _settings.CableTravel + _settings.WakeFade;
            Assert.AreEqual(1f, _stages.Level(HomeLight.Windows, Start), "home stays lit");
            Assert.AreEqual(1f, _stages.Level(HomeLight.BayLamps, done), Tolerance);
            Assert.AreEqual(0f, _stages.Level(HomeLight.LiftLamps, done));

            _stages.SetStage(BasePowerStage.Lift, false, done);
            Assert.AreEqual(1f, _stages.Level(HomeLight.LiftLamps, done + _settings.CableTravel + _settings.WakeFade),
                Tolerance);
        }

        [Test]
        public void Cable_CarriesPowerFromTheTower_BeforeThePlaceWakes()
        {
            const int Segments = 6;
            _stages.SetStage(BasePowerStage.Home, false, Start);
            float halfway = Start + _settings.CableTravel * 0.5f;
            Assert.AreEqual(1f, _stages.CableLevel(BasePowerStage.Home, 0, Segments, halfway), Tolerance,
                "the tower end is lit by halfway");
            Assert.AreEqual(0f, _stages.CableLevel(BasePowerStage.Home, Segments - 1, Segments, halfway),
                "the far end waits");
            Assert.AreEqual(0f, _stages.Level(HomeLight.Windows, halfway), "windows wait for the cable");
            Assert.AreEqual(0f, _stages.CableLevel(BasePowerStage.Bay, 0, Segments, halfway), "the bay cable sleeps");
            float arrived = Start + _settings.CableTravel + _settings.CableSegmentFade;
            Assert.AreEqual(1f, _stages.CableLevel(BasePowerStage.Home, Segments - 1, Segments, arrived), Tolerance);
        }

        [Test]
        public void Pilot_BlinksWhileAsleep_AndRestsOnceHomeIsLit()
        {
            Assert.AreEqual(1f, _stages.PilotLevel(0f), Tolerance, "a blink's peak");
            Assert.Less(_stages.PilotLevel(_settings.PilotPeriod * 0.25f), 0.1f, "short blinks in a long dark");
            _stages.SetStage(BasePowerStage.Home, true, Start);
            Assert.AreEqual(_settings.TopLightLevel, _stages.PilotLevel(_settings.PilotPeriod * 10.3f), Tolerance,
                "a woken home keeps a steady warm top light");
        }

        [Test]
        public void RestoredStage_AppliesAtOnce()
        {
            _stages.SetStage(BasePowerStage.Lift, true, Start);
            foreach (HomeLight light in System.Enum.GetValues(typeof(HomeLight)))
            {
                Assert.AreEqual(1f, _stages.Level(light, Start), light.ToString());
            }

            Assert.AreEqual(1f, _stages.CableLevel(BasePowerStage.Lift, 3, 4, Start), "cables are lit at once");
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
