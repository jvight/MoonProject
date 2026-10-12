using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The Moonlight Glass HUD's rules: the home meter's compass point, and the ability slots' rings and when the row
    /// shows.
    /// </summary>
    public sealed class GlassHudTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheCompassPoint_IsNorthAlongPlusZ_AndTurnsClockwise()
        {
            Assert.AreEqual(0, CompassPoint.Of(Vector3.zero, new Vector3(0f, 0f, 100f)), "north");
            Assert.AreEqual(1, CompassPoint.Of(Vector3.zero, new Vector3(70f, 5f, 70f)), "north-east; height ignored");
            Assert.AreEqual(2, CompassPoint.Of(Vector3.zero, new Vector3(100f, 0f, 0f)), "east");
            Assert.AreEqual(4, CompassPoint.Of(Vector3.zero, new Vector3(0f, 0f, -100f)), "south");
            Assert.AreEqual(6, CompassPoint.Of(Vector3.zero, new Vector3(-100f, 0f, 0f)), "west");
            Assert.AreEqual(0, CompassPoint.Of(Vector3.zero, new Vector3(-5f, 0f, 100f)), "just west of north is north");
            Assert.AreEqual("ui.compass.ne", CompassPoint.Key(1));
            Assert.AreEqual("ui.compass.n", CompassPoint.Key(0));
        }

        [Test]
        public void AnAbilityOwnedAtLoad_IsNotNew_ButOneEarnedLaterWearsTheRingForAWhile()
        {
            var settings = new AbilitySlotsSettings();
            var rules = new AbilitySlotRules(settings, 2);
            Step(rules, true, false, false, 1f);
            Assert.IsTrue(rules.IsOwned(0));
            Assert.IsFalse(rules.IsRinged(0), "owned at load is not just earned");
            Assert.IsFalse(rules.IsOwned(1));

            Step(rules, true, true, false, Frame);
            Assert.IsTrue(rules.IsRinged(1), "the new ability wears the amber ring");
            Assert.IsTrue(rules.Shown, "and the row comes up for it, even while driving");
            Step(rules, true, true, false, settings.EarnedSeconds + 0.1f);
            Assert.IsFalse(rules.IsRinged(1), "for a while only");
        }

        [Test]
        public void TheRow_RestsInViewWhile07IsStill_AndTheTetherWearsTheRingWhileTowing()
        {
            var settings = new AbilitySlotsSettings();
            var rules = new AbilitySlotRules(settings, 1);
            Run(rules, false, 2f, 0.5f);
            Assert.IsFalse(rules.Shown, "no permanent HUD while driving");
            Run(rules, false, settings.StillSeconds + 0.1f, 0f);
            Assert.IsTrue(rules.Shown, "it drifts into view once 07 rests");

            Run(rules, true, Frame, 3f);
            Assert.IsTrue(rules.IsRinged(0), "towing: the tether is in use");
            Assert.IsTrue(rules.Shown);
        }

        private static void Step(AbilitySlotRules rules, bool first, bool second, bool inUse, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                rules.Slot(0, first, inUse, Frame);
                rules.Slot(1, second, false, Frame);
                rules.Finish(5f, Frame);
            }
        }

        private static void Run(AbilitySlotRules rules, bool inUse, float seconds, float speed)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                rules.Slot(0, true, inUse, Frame);
                rules.Finish(speed, Frame);
            }
        }
    }
}
