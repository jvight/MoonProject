using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// The radio through the opening (opening v3 R4 and R8): only a faint signal hiss while the base is dark; Power on
    /// clears the hiss but plays no music (a build is not a milestone); relay.1 answering 07's call is the milestone
    /// where one track fades in, low and warm; a save that is already answered stays silent.
    /// </summary>
    public sealed class RadioPowerOnTests
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

        [UnityTest]
        public IEnumerator UnpoweredBase_TheRadioWakesToAFaintHiss_AndNoMusic()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);

            Assert.IsTrue(_rig.Radio.IsOn);
            Assert.IsFalse(_rig.Radio.Powered);
            Assert.IsFalse(_rig.Radio.MusicStarted, "no music before the base has power");
            AudioSource hiss = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            Assert.IsTrue(hiss.isPlaying);
            Assert.Greater(hiss.volume, 0f, "the signal hiss is the only thing it hears");
            Assert.Greater(_rig.Radio.HissFloor, 0f);
        }

        [UnityTest]
        public IEnumerator PowerOn_ClearsTheHiss_ButPlaysNoMusic()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);
            AudioSource hiss = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            float dark = hiss.volume;

            _rig.Milestone(OpeningMilestone.PowerOn);
            Assert.IsTrue(_rig.Radio.Powered);
            yield return new WaitForSecondsRealtime(5f);

            Assert.IsFalse(_rig.Radio.MusicStarted, "building the solar array is not a music moment");
            Assert.IsFalse(_rig.Radio.Answered);
            Assert.Less(_rig.Radio.HissFloor, 0.05f, "the hiss has cleared");
            Assert.Less(hiss.volume, dark * 0.5f);
        }

        [UnityTest]
        public IEnumerator TheAnswer_TunesTheDial_AndTheFirstMusicFadesInSlowly()
        {
            _rig.Wake(true);
            _rig.Milestone(OpeningMilestone.PowerOn);
            yield return new WaitForSecondsRealtime(3f);

            _rig.Answer();
            Assert.IsTrue(_rig.Radio.Answered);
            AudioSource swish = AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");
            Assert.IsTrue(swish.isPlaying, "the dial finds the network");
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(_rig.Radio.MusicStarted);
            float early = _rig.LoudestDeck().volume;

            yield return new WaitForSecondsRealtime(4f);
            Assert.Greater(_rig.LoudestDeck().volume, early, "the music is still swelling in seconds later");
        }

        [UnityTest]
        public IEnumerator ARestoredAnswer_StaysSilent()
        {
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            _rig.Answer(true);
            Assert.AreEqual(0f, _rig.Radio.HissFloor, 1e-5f, "a load with power has no hiss to clear");
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(4f);

            Assert.IsTrue(_rig.Radio.Answered);
            Assert.IsFalse(_rig.Radio.MomentPending);
            Assert.IsFalse(_rig.Radio.MusicStarted, "a load past the answer does not replay its music");
        }

        [UnityTest]
        public IEnumerator BuildMilestonesAndEarlierCallStates_LeaveTheRadioSilent()
        {
            _rig.Wake(true);
            _rig.Milestone(OpeningMilestone.FirstCharge);
            _rig.Milestone(OpeningMilestone.PowerOn);
            _rig.Milestone(OpeningMilestone.NightReady);
            _rig.Milestone(OpeningMilestone.SignalWest);
            _rig.Events.Publish(new CallStateChanged(CallState.Locked, false));
            _rig.Events.Publish(new CallStateChanged(CallState.Clear, false));
            yield return new WaitForSecondsRealtime(3f);

            Assert.IsFalse(_rig.Radio.Answered);
            Assert.IsFalse(_rig.Radio.MusicStarted, "only the answer brings the music back");
        }
    }
}
