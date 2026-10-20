using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The call from the light beside Earth: louder as 07 faces it, its mode per state, a note per decoded
    /// character.</summary>
    public sealed class CallCarrierTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private void Face(Vector3 direction)
        {
            _rig.Rover.ScanOrigin.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private IEnumerator Settle(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Facing_TheLight_SwellsTheCarrier()
        {
            Vector3 toLight = _rig.Rover.Call.Direction;
            _rig.Events.Publish(new CallStateChanged(CallState.Clear, false));
            Face(-toLight);
            yield return Settle(1.5f);
            float away = _rig.Call.CarrierSource.volume;
            Face(toLight);
            yield return Settle(1.5f);
            float facing = _rig.Call.CarrierSource.volume;
            Assert.IsTrue(_rig.Call.CarrierSource.isPlaying);
            Assert.Greater(facing, away * 2f, "tuning in: much louder facing the light");
            Assert.Greater(away, 0f, "the call is always there, faintly");
        }

        [UnityTest]
        public IEnumerator TheCallsState_SetsItsMode()
        {
            Face(_rig.Rover.Call.Direction);
            _rig.Events.Publish(new CallStateChanged(CallState.Broken, true));
            yield return Settle(1.5f);
            Assert.Greater(_rig.Call.StaticSource.volume, 0f, "on reserve it hisses");
            Assert.Greater(_rig.Call.CarrierSource.volume + 1e-6f, 0f);

            _rig.Events.Publish(new CallStateChanged(CallState.Clear, false));
            yield return Settle(2f);
            Assert.AreEqual(0f, _rig.Call.StaticSource.volume, 1e-4f, "the night clears it");
            Assert.AreEqual(1f, _rig.Call.CarrierSource.pitch, 1e-4f);

            _rig.Events.Publish(new CallStateChanged(CallState.Answered, false));
            yield return null;
            Assert.AreEqual(0f, _rig.Call.CarrierSource.volume, "the call alone gives way");
            Assert.Greater(_rig.Call.AnsweredSource.volume, 0f, "relay.1 answers");
        }

        [Test]
        public void EachDecodedCharacter_PlaysOneNote_TheWholeNameLandingOnD()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new CallDecoded(1, 5, false));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount);
            Assert.AreEqual("call_decoded_01", _rig.Director.LastClip.name);
            _rig.Events.Publish(new CallDecoded(5, 5, false));
            Assert.AreEqual(plays + 2, _rig.Director.PlayCount);
            Assert.AreEqual("call_decoded_05", _rig.Director.LastClip.name, "ARK IV whole: D");

            _rig.Events.Publish(new CallDecoded(3, 5, true));
            Assert.AreEqual(plays + 2, _rig.Director.PlayCount, "a loaded name is shown silently");
        }

        [UnityTest]
        public IEnumerator TheKeying_IsRegistered_AndFollowsTheCarriersPlayhead()
        {
            Assert.IsTrue(_rig.Bootstrap.Context.TryGet(out ICallSignal signal), "Audio registers the call's keying");
            _rig.Events.Publish(new CallStateChanged(CallState.Clear, false));
            bool keyed = false;
            float until = Time.realtimeSinceStartup + 4.5f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                float keying = signal.Keying;
                Assert.That(keying, Is.InRange(0f, 1f));
                float expected = CallKeying.Envelope(_rig.Call.KeyingPlayhead) * _rig.Call.Model.Cut;
                Assert.AreEqual(expected, keying, 1e-6f, "read off the loop's own playhead, sampled once this frame");
                Assert.AreEqual(keying, signal.Keying, "every reader in a frame gets the same keying");
                keyed |= keying > 0.9f;
            }

            Assert.IsTrue(keyed, "a full loop keys the sign");
        }
    }
}
