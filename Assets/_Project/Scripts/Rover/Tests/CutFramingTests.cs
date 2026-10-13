using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The charge-up's push-in: when it is earned, its side, eye and depth, easing in and handing back.</summary>
    public sealed class CutFramingTests
    {
        [Test]
        public void TheCamera_SitsOnTheSideTheBeamRunsTo()
        {
            Assert.AreEqual(1f, CutFraming.ChooseSide(Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 2f)));
            Assert.AreEqual(-1f, CutFraming.ChooseSide(Vector3.zero, Vector3.forward, new Vector3(-1f, 0f, 2f)));
            Assert.AreEqual(1f, CutFraming.ChooseSide(Vector3.zero, Vector3.right, new Vector3(2f, 0f, -1f)),
                "Relative to 07's heading, not the world.");
        }

        [Test]
        public void TheEye_IsBehind_AndOutToTheSide()
        {
            var framing = new CutFraming();
            framing.Begin(new Vector3(0f, 0f, 3f), -1f, 1f);
            Vector3 eye = framing.Eye(Vector3.up, 2f, 1.2f);
            Assert.AreEqual(-1.2f, eye.x, 1e-4f, "Out to the left by the offset.");
            Assert.AreEqual(-2f, eye.z, 1e-4f, "Behind the emitter along the beam.");
            Assert.AreEqual(1f, eye.y, 1e-4f, "Level with the emitter.");
        }

        [Test]
        public void OnlyAFreshChargeOnABigPiece_PushesIn_AndNeverWithTheGentleCamera()
        {
            Assert.IsTrue(CutFraming.Wanted(false, 3, 3, false), "A fresh charge on a large or hull plate.");
            Assert.IsFalse(CutFraming.Wanted(true, 3, 3, false), "A warm emitter repeats too often.");
            Assert.IsFalse(CutFraming.Wanted(false, 2, 3, false), "A medium piece.");
            Assert.IsFalse(CutFraming.Wanted(false, 1, 3, false), "A small piece.");
            Assert.IsFalse(CutFraming.Wanted(false, 3, 3, true), "The Gentle camera never moves.");
        }

        [Test]
        public void ThePushIn_IsSixPercentCloser_AndThirtyCentimetresLower()
        {
            float elevation = 20f;
            float radius = 6f;
            float height = radius * Mathf.Sin(elevation * Mathf.Deg2Rad);
            CutFraming.PushIn(1f, 0.06f, 0.3f, ref elevation, ref radius);
            Assert.AreEqual(6f * 0.94f, radius, 1e-4f);
            Assert.AreEqual(height - 0.3f, radius * Mathf.Sin(elevation * Mathf.Deg2Rad), 1e-4f);

            float rest = 20f;
            float restRadius = 6f;
            CutFraming.PushIn(0f, 0.06f, 0.3f, ref rest, ref restRadius);
            Assert.AreEqual(20f, rest, 1e-3f, "At weight 0 it is exactly the chase camera.");
            Assert.AreEqual(6f, restRadius, 1e-5f);
        }

        [Test]
        public void ItEasesIn_ThenHandsBackQuickly_AndStaysBack()
        {
            var framing = new CutFraming();
            framing.Begin(Vector3.forward, 1f, 1.2f);
            framing.Step(0.6f);
            Assert.That(framing.Weight, Is.InRange(0.3f, 0.7f), "Halfway, eased.");
            framing.Step(0.7f);
            Assert.AreEqual(1f, framing.Weight);

            framing.End(0.3f);
            Assert.IsFalse(framing.IsActive, "The player's input takes the camera back.");
            framing.Step(0.15f);
            Assert.Less(framing.Weight, 1f, "Easing, not a cut.");
            framing.Step(0.2f);
            Assert.AreEqual(0f, framing.Weight);
            framing.Step(5f);
            Assert.AreEqual(0f, framing.Weight, "It stays with the player until the next cut.");
        }
    }
}
