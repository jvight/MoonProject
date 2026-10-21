using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The power module lamp's purpose (<see cref="ModuleLampPurpose"/>): a blinking lure only while the module waits
    /// unhooked at the ruin; on the cable, home or built into the bank it eases to a steady powered glow; a loaded
    /// game takes its state at once.
    /// </summary>
    public sealed class ModuleLampPurposeTests
    {
        private const float Frame = 1f / 60f;
        private const float Fade = 1.5f;
        private const float Radius = 12f;
        private const float Steady = 0.55f;

        private static readonly Vector3 Ruin = new Vector3(100f, 3f, 200f);

        [Test]
        public void AtTheRuin_Unhooked_ItIsALure_AndBlinks()
        {
            Assert.IsTrue(ModuleLampPurpose.AtRuin(false, Ruin + new Vector3(4f, 0f, -3f), Ruin, Radius));
            var purpose = new ModuleLampPurpose();
            purpose.Step(true, Frame, Fade);
            Assert.IsTrue(purpose.IsLure);
            Assert.AreEqual(0.8f, purpose.Level(0.8f, Steady), 1e-5f, "the blink itself");
            Assert.AreEqual(0f, purpose.Level(0f, Steady), 1e-5f, "dark between blinks");
        }

        [Test]
        public void OnTheCable_OrAwayFromTheRuin_ItIsNoLure()
        {
            Assert.IsFalse(ModuleLampPurpose.AtRuin(true, Ruin, Ruin, Radius), "hooked on 07's cable");
            Assert.IsFalse(ModuleLampPurpose.AtRuin(false, Ruin + new Vector3(0f, 0f, 40f), Ruin, Radius),
                "home at the apron");
        }

        [Test]
        public void TakenHome_ItEasesToASteadyGlow_WhateverTheBlink()
        {
            var purpose = new ModuleLampPurpose();
            purpose.Step(true, Frame, Fade);
            for (float t = 0f; t < Fade * 0.5f; t += Frame)
            {
                purpose.Step(false, Frame, Fade);
            }

            Assert.IsFalse(purpose.IsLure);
            Assert.AreEqual(0.5f, purpose.Powered, 0.03f, "halfway eased halfway through the fade");
            for (float t = 0f; t < Fade; t += Frame)
            {
                purpose.Step(false, Frame, Fade);
            }

            Assert.AreEqual(Steady, purpose.Level(0f, Steady), 1e-5f);
            Assert.AreEqual(Steady, purpose.Level(1f, Steady), 1e-5f);
        }

        [Test]
        public void ALoadedGame_TakesItsStateAtOnce()
        {
            var purpose = new ModuleLampPurpose();
            purpose.Step(false, Frame, Fade);
            Assert.AreEqual(1f, purpose.Powered, "already home: steady from the first frame");
        }
    }
}
