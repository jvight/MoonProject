using System;
using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class HoldRequestsTests
    {
        private readonly object _beam = new object();
        private readonly object _cinematic = new object();

        [Test]
        public void StartsReleased()
        {
            Assert.IsFalse(new HoldRequests().IsHeld);
        }

        [Test]
        public void HeldWhileAnyOwnerHolds()
        {
            var holds = new HoldRequests();
            holds.Set(_beam, true);
            holds.Set(_cinematic, true);
            holds.Set(_beam, false);
            Assert.IsTrue(holds.IsHeld, "The cinematic still holds.");
            holds.Set(_cinematic, false);
            Assert.IsFalse(holds.IsHeld);
        }

        [Test]
        public void HoldingOrReleasingTwice_IsIdempotent()
        {
            var holds = new HoldRequests();
            holds.Set(_beam, true);
            holds.Set(_beam, true);
            Assert.AreEqual(1, holds.Count);
            holds.Set(_beam, false);
            holds.Set(_beam, false);
            holds.Set(_cinematic, false);
            Assert.IsFalse(holds.IsHeld);
            Assert.AreEqual(0, holds.Count);
        }

        [Test]
        public void NullOwner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new HoldRequests().Set(null, true));
        }
    }
}
