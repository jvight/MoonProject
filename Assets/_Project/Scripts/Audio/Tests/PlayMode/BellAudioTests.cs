using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Bell's voice: her own boot, jingles on their own source, leg taps from walking, doze and wake.</summary>
    public sealed class BellAudioTests
    {
        private const int BellIndex = 1;

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

        private FakeFriend Bell => _rig.Rover.Bell;

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        [UnityTest]
        public IEnumerator Jingles_PlayAtBell_OnTheirOwnSource()
        {
            Bell.Position = new Vector3(-30f, 1f, 42f);
            Bell.Activity = FriendActivity.Following;
            yield return null;

            _rig.Events.Publish(new FriendRepaired("bell"));
            Assert.AreEqual("bell_jingle_short", LastClip, "the first notes of the station jingle as she stands");
            AudioSource jingles = _rig.Friends.JingleSource(BellIndex);
            Assert.IsTrue(jingles.isPlaying);
            Assert.AreEqual(1f, jingles.spatialBlend);
            Assert.AreEqual(1f, jingles.pitch, "a melody is never pitch-varied");
            Assert.Less(Vector3.Distance(jingles.transform.position, Bell.Position), 1e-4f);
            Assert.AreNotSame(_rig.Friends.ChirpSource(BellIndex), jingles);

            Bell.Activity = FriendActivity.Home;
            _rig.Events.Publish(new FriendGreeted("bell"));
            Assert.AreEqual("bell_jingle_full", LastClip, "the whole jingle in greeting");
        }

        [UnityTest]
        public IEnumerator Boot_IsHerTapeAndNeedle()
        {
            Bell.Activity = FriendActivity.Repairing;
            Bell.RotorSpeed = 0f;
            yield return new WaitForSeconds(0.3f);
            Bell.RotorSpeed = 0.1f;
            yield return null;
            yield return null;
            Assert.AreEqual("bell_boot", LastClip);
        }

        [UnityTest]
        public IEnumerator LegTaps_FollowTheDistanceSheWalks_NeverWhileDormant()
        {
            Bell.Activity = FriendActivity.Dormant;
            yield return Walk(2f);
            Assert.AreEqual(0, CountRecent("bell_step"), "a broken Bell lies still");

            Bell.Activity = FriendActivity.Home;
            yield return Walk(2f);
            int taps = CountRecent("bell_step");
            Assert.GreaterOrEqual(taps, 3, "about one tap per stride");
            Assert.LessOrEqual(taps, 5);
            AudioSource feet = _rig.Friends.FeetSource(BellIndex);
            Assert.AreNotSame(_rig.Friends.ChirpSource(BellIndex), feet);
            Assert.Less(Vector3.Distance(feet.transform.position, Bell.Position), 1e-4f);
        }

        [UnityTest]
        public IEnumerator Napping_Dozes_ThenWakesWithASleepyTune()
        {
            Bell.Activity = FriendActivity.Napping;
            yield return new WaitForSeconds(1.5f);
            AudioSource doze = _rig.Friends.DozeSource(BellIndex);
            Assert.IsTrue(doze.isPlaying, "the ember hum while she sleeps");
            Assert.Greater(doze.volume, 0f);

            Bell.Activity = FriendActivity.Home;
            yield return null;
            yield return null;
            Assert.AreEqual("bell_wake", LastClip);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(doze.isPlaying, "the hum has faded out");
        }

        [Test]
        public void ANewRelicOnTheShelf_GetsHerHappyCrackle_OnlyWhenHome()
        {
            Bell.Activity = FriendActivity.Napping;
            _rig.Events.Publish(new RelicDeposited("teapot", Vector3.zero, 1));
            StringAssert.StartsWith("relic_placed", LastClip);

            Bell.Activity = FriendActivity.Home;
            _rig.Events.Publish(new RelicDeposited("lamp", Vector3.zero, 2));
            StringAssert.StartsWith("bell_excited_", LastClip);
        }

        private IEnumerator Walk(float metres)
        {
            const int frames = 20;
            Vector3 start = Bell.Position;
            for (int i = 1; i <= frames; i++)
            {
                Bell.Position = start + new Vector3(metres * i / frames, 0f, 0f);
                yield return null;
            }
        }

        private int CountRecent(string clipPrefix)
        {
            int count = 0;
            for (int i = 0; i < 16; i++)
            {
                AudioClip clip = _rig.Director.RecentClip(i);
                if (clip != null && clip.name.StartsWith(clipPrefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
