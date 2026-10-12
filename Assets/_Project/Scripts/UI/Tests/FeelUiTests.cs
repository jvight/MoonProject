using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The feel pass's UI side (docs/design/feel-pass.md §4): the stock count rolls one unit per step and never
    /// jumps, skips or goes back; the hold ring's charge segment fills, empties and hands over; the rumble settings
    /// persist and default to on at full strength.
    /// </summary>
    public sealed class FeelUiTests
    {
        private const float Frame = 1f / 60f;

        private MaterialsChipSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new MaterialsChipSettings();
        }

        [Test]
        public void ARiseRolls_OneUnitPerStep_TheFirstAtOnce_NeverSkippingAValue()
        {
            var count = new CountUp(_settings);
            count.Snap(2);
            count.SetTarget(6);
            var seen = new List<int>();
            float elapsed = 0f;
            for (; elapsed < 2f && !count.IsSettled; elapsed += Frame)
            {
                if (count.Step(Frame))
                {
                    Assert.AreEqual(1, count.Moved, "a unit at a time");
                    seen.Add(count.Shown);
                }
            }

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, seen, "every number once, in order, ending on the target");
            Assert.AreEqual(0.08f * 3f, elapsed, 2f * Frame, "the first unit at once, then one per 80 ms");
        }

        [Test]
        public void ASecondCollectMidRoll_CarriesOnFromTheNumberOnScreen_AndLandsOnTheFinalValue()
        {
            var count = new CountUp(_settings);
            count.Snap(0);
            count.SetTarget(4);
            var seen = new List<int>();
            while (count.Shown < 2)
            {
                if (count.Step(Frame))
                {
                    seen.Add(count.Shown);
                }
            }

            int rolledBefore = count.Rolled;
            count.SetTarget(7);
            Assert.AreEqual(rolledBefore, count.Rolled, "the roll goes on; it does not start again");
            for (int i = 0; i < 300 && !count.IsSettled; i++)
            {
                if (count.Step(Frame))
                {
                    seen.Add(count.Shown);
                }
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, seen, "no repeat, no jump back, no skip");
            Assert.AreEqual(7, count.Rolled, "seven units rolled in one roll");
        }

        [Test]
        public void ARollThatHasRested_StartsItsIndexAgain()
        {
            var count = new CountUp(_settings);
            count.Snap(0);
            count.SetTarget(2);
            for (int i = 0; i < 60; i++)
            {
                count.Step(Frame);
            }

            Assert.IsTrue(count.IsSettled);
            Assert.AreEqual(2, count.Rolled);
            count.SetTarget(3);
            Assert.AreEqual(0, count.Rolled);
        }

        [Test]
        public void ABigHaul_QuickensTheStep_ToStayWithinTheLongestRoll()
        {
            var count = new CountUp(_settings);
            count.Snap(0);
            count.SetTarget(200);
            float elapsed = 0f;
            int units = 0;
            while (!count.IsSettled && elapsed < 10f)
            {
                count.Step(Frame);
                units += count.Moved;
                elapsed += Frame;
            }

            Assert.AreEqual(200, units, "even a big haul visits every unit");
            Assert.LessOrEqual(elapsed, _settings.MaxCountSeconds + 2f * Frame);
        }

        [Test]
        public void TheTally_PulsesEachRisingUnit_AndASpendRollsDownInSilence()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(5, 0, 0);
            tally.Apply(7, 0, 0);
            tally.Step(Frame);
            Assert.AreEqual(1, tally.Moved(SalvageMaterial.Metal));
            Assert.AreEqual(1, tally.Rolled(SalvageMaterial.Metal));
            Assert.AreEqual(1, tally.Remaining(SalvageMaterial.Metal), "one unit still to come");
            tally.Step(Frame);
            Assert.Greater(tally.TickPulse(SalvageMaterial.Metal), 0f, "the chip pulses with the unit");
            Assert.AreEqual(0f, tally.TickPulse(SalvageMaterial.Wiring), "only the material that ticked");

            tally.Snap(5, 0, 0);
            tally.Apply(2, 0, 0);
            for (float t = 0f; t < 1f; t += Frame)
            {
                tally.Step(Frame);
                Assert.LessOrEqual(tally.Moved(SalvageMaterial.Metal), 0, "a spend only counts down");
                Assert.AreEqual(0f, tally.TickPulse(SalvageMaterial.Metal), "and never pulses");
            }

            Assert.AreEqual(2, tally.Shown(SalvageMaterial.Metal));
        }

        [Test]
        public void TheChargeSegment_FillsOverTheBeat_HoldsFull_ThenHandsOverAtIgnition()
        {
            var charge = new ChargeSegment();
            charge.Begin(FeelKind.Cut, Vector3.zero, 0.65f);
            Assert.IsTrue(charge.IsCharging);
            Step(charge, 0.3f);
            Assert.That(charge.Value, Is.InRange(0.1f, 0.9f), "part way");
            Step(charge, 0.5f);
            Assert.AreEqual(1f, charge.Value, 1e-4f);
            Assert.IsTrue(charge.IsCharging, "it waits at full for ignition");

            charge.Ignite(0.35f);
            Assert.IsFalse(charge.IsCharging);
            Assert.IsTrue(charge.IsActive);
            Step(charge, 0.2f);
            Assert.That(charge.Value, Is.InRange(0.1f, 0.9f), "easing away while the progress fill takes over");
            Step(charge, 0.3f);
            Assert.AreEqual(0f, charge.Value);
            Assert.IsFalse(charge.IsActive);
        }

        [Test]
        public void ACancel_EmptiesTheSegmentFromWhereItWas_AndAResumeChargesQuicker()
        {
            var charge = new ChargeSegment();
            charge.Begin(FeelKind.Cut, Vector3.zero, 0.65f);
            Step(charge, 0.3f);
            float reached = charge.Value;
            charge.Cancel(0.2f);
            Step(charge, 0.1f);
            Assert.Less(charge.Value, reached);
            Assert.Greater(charge.Value, 0f);
            Step(charge, 0.2f);
            Assert.AreEqual(0f, charge.Value);
            Assert.IsFalse(charge.IsActive);

            charge.Begin(FeelKind.Cut, Vector3.zero, 0.3f);
            Step(charge, 0.35f);
            Assert.AreEqual(1f, charge.Value, 1e-4f, "a resume's short charge is full after its 0.3 s");
        }

        [Test]
        public void AnIgnitionAfterACancel_ChangesNothing()
        {
            var charge = new ChargeSegment();
            charge.Begin(FeelKind.Dig, Vector3.up, 0.65f);
            Step(charge, 0.2f);
            charge.Cancel(0.2f);
            Step(charge, 0.05f);
            float draining = charge.Value;
            charge.Ignite(0.35f);
            Assert.AreEqual(draining, charge.Value, 1e-6f, "an Act after a Cancel does not refill it");
            Assert.AreEqual(FeelKind.Dig, charge.Kind);
            Assert.AreEqual(Vector3.up, charge.Point);
        }

        [Test]
        public void Rumble_IsOnAtFullStrengthByDefault_EvenForOlderSaves_AndRoundTrips()
        {
            var services = new FakeSettingsServices();
            ILocalization localization = TestTables.Localization(new EventBus(), "next.tower", "x", "y");
            var settings = new PlayerSettings(services, services, services, new FakeDisplayControl(), localization, new PauseSettings());
            settings.Restore(new SettingsSaveData());
            Assert.IsTrue(services.RumbleEnabled, "a save from before the setting keeps it on");
            Assert.AreEqual(1f, services.RumbleStrength);
            Assert.AreEqual(settings.VolumeSteps, settings.GetRumbleStep());

            settings.Rumble = false;
            settings.SetRumbleStep(settings.VolumeSteps / 2);
            Assert.IsTrue(settings.IsDirty);
            Assert.AreEqual(0.5f, services.RumbleStrength, 0.06f);

            var other = new FakeSettingsServices();
            var restored = new PlayerSettings(other, other, other, new FakeDisplayControl(), localization, new PauseSettings());
            restored.Restore(settings.Capture());
            Assert.IsFalse(other.RumbleEnabled);
            Assert.AreEqual(services.RumbleStrength, other.RumbleStrength, 1e-5f);
            Assert.IsFalse(restored.IsDirty, "a restore is not a change");
        }

        private static void Step(ChargeSegment charge, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-6f; t += Frame)
            {
                charge.Step(Mathf.Min(Frame, seconds - t));
            }
        }
    }
}
