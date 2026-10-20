using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    /// <summary>A refused cut ticks, except in the frame the one-time sputter comes with it, whichever arrives first.
    /// </summary>
    public sealed class RefusalGateTests
    {
        [Test]
        public void ALoneRefusal_Ticks_Once()
        {
            var gate = new RefusalGate();
            gate.Refuse(10, Vector3.one);
            Assert.IsTrue(gate.TryTakeRefusal(out Vector3 point));
            Assert.AreEqual(Vector3.one, point);
            Assert.IsFalse(gate.TryTakeRefusal(out _), "taken once");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ASputterInTheSameFrame_SilencesTheRefusal_InEitherOrder(bool sputterFirst)
        {
            var gate = new RefusalGate();
            if (sputterFirst)
            {
                gate.Sputter(20);
                gate.Refuse(20, Vector3.zero);
            }
            else
            {
                gate.Refuse(20, Vector3.zero);
                gate.Sputter(20);
            }

            Assert.IsFalse(gate.TryTakeRefusal(out _), "the sputter speaks alone");
            gate.Refuse(21, Vector3.up);
            Assert.IsTrue(gate.TryTakeRefusal(out _), "the next refusal ticks again");
        }
    }
}
