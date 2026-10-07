using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Bell's voice: her moods and clockwork legs, her cues (tape, needle, foot taps, crackle, dial), jingles on
    /// their own source, leg taps from walking, doze and wake.
    /// </summary>
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

        [Test]
        public void HerWholeVoice_IsInTheLibrary()
        {
            string[] parts =
            {
                "broken", "curious", "happy", "sleepy", "greeting", "excited", "found", "rotor", "step", "doze",
                "wake", "tune", "jingle_short", "jingle_full", "tape_slot", "needle_sweep",
            };
            foreach (string part in parts)
            {
                Assert.IsTrue(_rig.Director.Library.TryResolve($"bell_{part}", out _), $"bell_{part}");
            }
        }

        [UnityTest]
        public IEnumerator Repair_TheTapeSlotsIn_EndingTheStitching_ThenTheNeedleSweeps()
        {
            Bell.Position = new Vector3(12f, 2f, -8f);
            Bell.Activity = FriendActivity.Repairing;
            Bell.RotorSpeed = 0f;
            yield return new WaitForSeconds(0.6f);
            AudioSource stitch = _rig.Friends.StitchSource(BellIndex);
            Assert.IsTrue(stitch.isPlaying, "07's beam stitches her");

            var slot = new Vector3(12f, 2.6f, -8f);
            _rig.Events.Publish(new BellCued(BellCue.TapeSlotted, slot));
            Assert.AreEqual("bell_tape_slot", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, slot), 1e-4f);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(stitch.isPlaying, "the stitching ends as the tape goes in");

            _rig.Events.Publish(new BellCued(BellCue.NeedleSwept, Bell.Position));
            Assert.AreEqual("bell_needle_sweep", LastClip);
            yield return null;
            Assert.AreEqual("bell_needle_sweep", LastClip, "her tape and needle are her boot: no shared one");
        }

        [UnityTest]
        public IEnumerator Jingles_PlayAtBell_OnTheirOwnSource()
        {
            Bell.Position = new Vector3(-30f, 1f, 42f);
            Bell.Activity = FriendActivity.Home;
            yield return null;

            _rig.Events.Publish(new FriendRepaired("bell"));
            Assert.AreEqual("bell_jingle_short", LastClip, "the first notes of the station jingle as she stands");
            AudioSource jingles = _rig.Friends.JingleSource(BellIndex);
            Assert.IsTrue(jingles.isPlaying);
            Assert.AreEqual(1f, jingles.spatialBlend);
            Assert.AreEqual(1f, jingles.pitch, "a melody is never pitch-varied");
            Assert.Less(Vector3.Distance(jingles.transform.position, Bell.Position), 1e-4f);
            Assert.AreNotSame(_rig.Friends.ChirpSource(BellIndex), jingles);

            _rig.Events.Publish(new FriendGreeted("bell"));
            Assert.AreEqual("bell_jingle_full", LastClip, "the whole jingle in greeting");
        }

        [UnityTest]
        public IEnumerator HerCues_FootTap_Crackle_AndTheDialsDetent()
        {
            Bell.Position = new Vector3(4f, 0f, 9f);
            Bell.Activity = FriendActivity.Home;
            yield return null;

            _rig.Events.Publish(new BellCued(BellCue.FootTapped, Bell.Position));
            StringAssert.StartsWith("bell_step_", LastClip, "a soft leg tap to the music");
            Assert.IsTrue(_rig.Friends.FeetSource(BellIndex).isPlaying);

            _rig.Events.Publish(new BellCued(BellCue.Crackled, Bell.Position));
            StringAssert.StartsWith("bell_excited_", LastClip, "her happy crackle at a new relic");
            Assert.IsTrue(_rig.Friends.ChirpSource(BellIndex).isPlaying);

            _rig.Events.Publish(new BellCued(BellCue.DialTurned, Bell.Position));
            Assert.AreEqual("radio_dial_click", LastClip);
            Assert.AreEqual(0f, _rig.Director.LastVoice.spatialBlend, "the detent is right under 07's nose");
        }

        [Test]
        public void ANewRelicOnTheShelf_IsHerCrackleCue_NotAChirpOfOurOwn()
        {
            Bell.Activity = FriendActivity.Home;
            _rig.Events.Publish(new RelicDeposited("lamp", Vector3.zero, 2));
            StringAssert.StartsWith("relic_placed", LastClip, "she crackles only when she saw it (BellCued.Crackled)");
        }

        [UnityTest]
        public IEnumerator HerLegs_TickLikeClockwork_WhileSheWaddles()
        {
            Bell.Activity = FriendActivity.Home;
            Bell.RotorSpeed = 0.5f;
            yield return new WaitForSeconds(1.5f);
            AudioSource legs = _rig.Friends.RotorSource(BellIndex);
            Assert.IsNotNull(legs);
            Assert.IsTrue(legs.isPlaying);
            StringAssert.StartsWith("bell_rotor", legs.clip.name);

            Bell.RotorSpeed = 0f;
            yield return new WaitForSeconds(3f);
            Assert.IsFalse(legs.isPlaying, "still when she stands still");
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
