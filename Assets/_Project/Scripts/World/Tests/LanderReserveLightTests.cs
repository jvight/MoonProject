using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The lander's own lights follow its reserve (opening v3 R4, <see cref="LanderReserveLight"/>): lit while it holds
    /// charge, dying together over the tuned fade once it is spent, never before; a restored game takes its state at
    /// once.
    /// </summary>
    public sealed class LanderReserveLightTests
    {
        private const float Frame = 1f / 60f;
        private const float Fade = 2.4f;

        [Test]
        public void WhileTheReserveHoldsCharge_TheLightsStayLit()
        {
            var light = new LanderReserveLight();
            light.OnReserve(1f, false);
            Run(light, 10f);
            Assert.AreEqual(1f, light.Level);

            light.OnReserve(0.01f, false);
            Run(light, 10f);
            Assert.AreEqual(1f, light.Level, "a last drop of charge still keeps them lit");
        }

        [Test]
        public void TheReserveSpent_TheLightsDieOverTheFade_NotAtOnce()
        {
            var light = new LanderReserveLight();
            light.OnReserve(0f, false);
            Run(light, Fade * 0.5f);
            Assert.That(light.Level, Is.InRange(0.4f, 0.6f), "half way through the fade they are half dark");
            Run(light, Fade * 0.5f + Frame);
            Assert.AreEqual(0f, light.Level, "and dark once it has run");
        }

        [Test]
        public void ARestoredGame_TakesTheLandersStateAtOnce()
        {
            var spent = new LanderReserveLight();
            spent.OnReserve(0f, true);
            Assert.AreEqual(0f, spent.Level, "a save from after the reserve died loads with the lander dark");

            var charged = new LanderReserveLight();
            charged.OnReserve(0.6f, true);
            Assert.AreEqual(1f, charged.Level);
        }

        [Test]
        public void ChargedAgain_TheLightsComeBack()
        {
            var light = new LanderReserveLight();
            light.OnReserve(0f, true);
            light.OnReserve(0.5f, false);
            Run(light, Fade + Frame);
            Assert.AreEqual(1f, light.Level);
        }

        private static void Run(LanderReserveLight light, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                light.Step(Frame, Fade);
            }
        }
    }
}
