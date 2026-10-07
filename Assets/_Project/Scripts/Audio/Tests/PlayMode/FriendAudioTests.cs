using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Tilly's voice: follows her, rotors by effort, stitches, voices each friend event.</summary>
    public sealed class FriendAudioTests
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

        private FakeFriend Tilly => _rig.Rover.Tilly;

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        [Test]
        public void EveryRosterFriend_GetsAVoice()
        {
            Assert.AreEqual(2, _rig.Friends.VoiceCount, "Tilly and Bell");
            Assert.IsTrue(_rig.Friends.enabled);
        }

        [UnityTest]
        public IEnumerator FriendAnswered_IsTheBrokenChirp_FromWhereSheLies()
        {
            Tilly.Position = new Vector3(70f, -2f, 20f);
            yield return null;
            _rig.Events.Publish(new FriendAnswered("tilly", Tilly.Position));

            StringAssert.StartsWith("tilly_broken_", LastClip);
            AudioSource voice = _rig.Friends.ChirpSource(0);
            Assert.IsTrue(voice.isPlaying);
            Assert.AreEqual(1f, voice.spatialBlend);
            Assert.Less(Vector3.Distance(voice.transform.position, Tilly.Position), 1e-4f);
        }

        [Test]
        public void FriendPartCollected_ClimbsThenResolvesOnTheLast()
        {
            string[] expected = { "friend_part_step1", "friend_part_step2", "friend_part_complete" };
            for (int collected = 1; collected <= 3; collected++)
            {
                _rig.Events.Publish(new FriendPartCollected("tilly", collected - 1, collected, 3));
                Assert.AreEqual(expected[collected - 1], LastClip);
            }

            _rig.Events.Publish(new FriendPartCollected("tilly", 3, 4, 5));
            Assert.AreEqual("friend_part_step4", LastClip, "bigger friends keep climbing before resolving");
        }

        [UnityTest]
        public IEnumerator Repair_Stitches_Rises_BootsAsTheRotorsStart_ThenChirpsHappilyWhenRepaired()
        {
            _rig.Events.Publish(new FriendRepairStarted("tilly"));
            Tilly.Activity = FriendActivity.Repairing;
            Tilly.RotorSpeed = 0f;
            yield return new WaitForSeconds(0.6f);
            AudioSource stitch = _rig.Friends.StitchSource(0);
            Assert.IsTrue(stitch.isPlaying);
            float early = stitch.pitch;

            yield return new WaitForSeconds(3f);
            Assert.Greater(stitch.pitch, early * 1.2f, "the stitching lifts over the repair");

            Tilly.RotorSpeed = 0.1f;
            yield return null;
            yield return null;
            Assert.AreEqual("friend_boot", LastClip, "the boot jingle as her eye flickers on and the rotors start");

            Tilly.RotorSpeed = 0.4f;
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(stitch.isPlaying, "the stitching has faded away");
            Assert.IsTrue(_rig.Friends.RotorSource(0).isPlaying, "her rotors spun up");

            Tilly.Activity = FriendActivity.Following;
            _rig.Events.Publish(new FriendRepaired("tilly"));
            StringAssert.StartsWith("tilly_happy_", LastClip);
        }

        [UnityTest]
        public IEnumerator Rotor_FollowsHerEffortAndPosition()
        {
            Tilly.Activity = FriendActivity.Following;
            Tilly.RotorSpeed = 0.4f;
            yield return new WaitForSeconds(1.5f);
            AudioSource rotor = _rig.Friends.RotorSource(0);
            Assert.IsTrue(rotor.isPlaying);
            Assert.Greater(rotor.volume, 0f);
            float hover = rotor.pitch;

            Tilly.RotorSpeed = 1f;
            Tilly.Position = new Vector3(5f, 3f, -4f);
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(rotor.pitch, hover);
            Assert.Less(Vector3.Distance(rotor.transform.position, Tilly.Position), 1e-4f);

            Tilly.Activity = FriendActivity.Napping;
            Tilly.RotorSpeed = 0f;
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(rotor.isPlaying, "quiet on her perch");
        }

        [Test]
        public void FriendSpotted_ChirpsFoundIt_AndPingsAtTheSpot()
        {
            var spot = new Vector3(40f, 0f, 12f);
            _rig.Events.Publish(new FriendSpotted("tilly", spot));

            Assert.AreEqual("friend_spot_ping", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, spot), 1e-4f);
            Assert.IsTrue(_rig.Friends.ChirpSource(0).isPlaying, "her 'found it' chirp plays on her own voice");
        }

        [Test]
        public void FriendGreeted_AndARelicComingHome()
        {
            _rig.Events.Publish(new FriendGreeted("tilly"));
            StringAssert.StartsWith("tilly_greeting_", LastClip);

            Tilly.Activity = FriendActivity.Home;
            _rig.Events.Publish(new RelicDeposited("teapot", Vector3.zero, 1));
            StringAssert.StartsWith("tilly_excited_", LastClip);
        }

        [Test]
        public void RelicDeposited_WhileSheIsAway_DoesNotMakeHerChirp()
        {
            Tilly.Activity = FriendActivity.Following;
            _rig.Events.Publish(new RelicDeposited("teapot", Vector3.zero, 1));
            StringAssert.StartsWith("relic_placed", LastClip);
        }

        [Test]
        public void UnknownFriend_IsReported()
        {
            LogAssert.Expect(LogType.Error, new Regex("no friend 'moss' in the roster"));
            _rig.Events.Publish(new FriendGreeted("moss"));
        }
    }
}
