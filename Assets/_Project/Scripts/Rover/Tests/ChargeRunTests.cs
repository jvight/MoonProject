using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The charge run (M3-26 §2): timing, resume, ignition, drain, fade, the tier rows; the cargo load.</summary>
    public sealed class ChargeRunTests
    {
        private readonly ChargeRunSettings _settings = new ChargeRunSettings();

        [Test]
        public void TheLastLamp_LightsAtIgnition_AndTheRunAccelerates()
        {
            Assert.AreEqual(0.65f, ChargeRun.Onset(4, 5, 0.65f, 0f), 1e-5f, "The last lamp lands at ignition.");
            float first = ChargeRun.Onset(0, 5, 0.65f, 0f);
            float second = ChargeRun.Onset(1, 5, 0.65f, 0f) - first;
            float last = ChargeRun.Onset(4, 5, 0.65f, 0f) - ChargeRun.Onset(3, 5, 0.65f, 0f);
            Assert.Less(last, second, "Ease-in: the gaps shrink toward the emitter.");
        }

        [Test]
        public void AResume_StartsPartWay_TheWarmLampsAtOnce()
        {
            Assert.AreEqual(0f, ChargeRun.Onset(0, 5, 0.3f, 0.5f), "Under the start share: lit at once.");
            Assert.AreEqual(0f, ChargeRun.Onset(1, 5, 0.3f, 0.5f));
            Assert.Greater(ChargeRun.Onset(2, 5, 0.3f, 0.5f), 0f);
            Assert.AreEqual(0.3f, ChargeRun.Onset(4, 5, 0.3f, 0.5f), 1e-5f);
        }

        [Test]
        public void TheLamps_FlareHoldSpikeDrainAndRest()
        {
            var run = new ChargeRun(_settings, 5);
            run.Begin(0.65f, 0f);
            run.Step(0.01f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(4), "The emitter's lamp waits for the run.");
            run.Step(0.65f);
            Assert.AreEqual(_settings.ChargedGlow, run.Glow(0), 1e-5f, "Charged once the run has passed.");
            run.Ignite();
            Assert.Greater(run.Glow(0), _settings.ChargedGlow, "Every lamp spikes at ignition.");
            run.Step(0.2f);
            run.Drain();
            run.Step(0.06f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(4), "The emitter end drains first ...");
            Assert.AreEqual(_settings.ChargedGlow, run.Glow(0), 1e-5f, "... the battery end last.");
            run.Step(0.2f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(0));
        }

        [Test]
        public void TheBreak_FadesTheLamps()
        {
            var run = new ChargeRun(_settings, 5);
            run.Begin(0.1f, 0f);
            run.Step(0.2f);
            run.Ignite();
            run.Fade();
            run.Step(_settings.FadeSeconds * 0.5f);
            Assert.That(run.Glow(2), Is.InRange(_settings.RestGlow, _settings.ChargedGlow));
            run.Step(_settings.FadeSeconds);
            Assert.AreEqual(_settings.RestGlow, run.Glow(2));
        }

        [Test]
        public void TheCutterTier_ShowsItsRow_InItsColour()
        {
            var events = new EventBus();
            var roots = new GameObject[3];
            var rows = new ChargeRunRow[3];
            int[] counts = { 5, 7, 9 };
            for (int tier = 0; tier < 3; tier++)
            {
                roots[tier] = new GameObject(ChargeRunLamps.RowName(tier));
                var lamps = new Renderer[counts[tier]];
                for (int i = 0; i < lamps.Length; i++)
                {
                    var lamp = new GameObject(ChargeRunLamps.SocketName(i));
                    lamp.transform.SetParent(roots[tier].transform);
                    lamps[i] = lamp.AddComponent<MeshRenderer>();
                }

                rows[tier] = Row(roots[tier], lamps);
            }

            var driver = new ChargeRunLamps(events, _settings, rows);
            Assert.AreEqual(5, driver.Run.Count);
            Assert.IsTrue(roots[0].activeSelf && !roots[1].activeSelf && !roots[2].activeSelf);
            events.Publish(new CutterTierChanged(2, false));
            Assert.AreEqual(9, driver.Run.Count, "Tier 2: nine lit edges.");
            Assert.IsTrue(roots[2].activeSelf && !roots[0].activeSelf);
            events.Publish(new CutterTierChanged(3, false));
            Assert.AreEqual(2, driver.Tier, "Beyond the rows art has built: the highest one.");
            driver.Dispose();
            foreach (GameObject root in roots)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TheHold_SagsPerQuarter_AndGlancesBackOnceWhenItFills()
        {
            Assert.AreEqual(0f, CargoLoad.Sag(1, 8, 0.01f), "Under a quarter: no sag.");
            Assert.AreEqual(0.02f, CargoLoad.Sag(4, 8, 0.01f), 1e-6f);
            Assert.AreEqual(0.04f, CargoLoad.Sag(8, 8, 0.01f), 1e-6f);
            var load = new CargoLoad();
            Assert.IsFalse(load.BecameFull(8, 8), "A loaded full hold: no glance.");
            Assert.IsFalse(load.BecameFull(5, 8));
            Assert.IsTrue(load.BecameFull(8, 8), "It just filled.");
            Assert.IsFalse(load.BecameFull(8, 8), "Once.");
        }

        private static ChargeRunRow Row(GameObject root, Renderer[] lamps)
        {
            var row = new ChargeRunRow();
            typeof(ChargeRunRow).GetField("_root", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance).SetValue(row, root);
            typeof(ChargeRunRow).GetField("_lamps", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance).SetValue(row, lamps);
            return row;
        }
    }
}
