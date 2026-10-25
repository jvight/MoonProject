using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// The radio through the opening (opening v3 R4): only a faint signal hiss while the base is dark; Power on clears
    /// the hiss. No milestone, not even relay.1's answer, and no music request brings music: the lofi radio never
    /// plays in-game.
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
        public IEnumerator UnpoweredBase_TheRadioWakesToAFaintHiss()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);

            Assert.IsTrue(_rig.Radio.IsOn);
            Assert.IsFalse(_rig.Radio.Powered);
            AudioSource hiss = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            Assert.IsTrue(hiss.isPlaying);
            Assert.Greater(hiss.volume, 0f, "the signal hiss is the only thing it hears");
            Assert.Greater(_rig.Radio.HissFloor, 0f);
        }

        [UnityTest]
        public IEnumerator PowerOn_ClearsTheHiss()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);
            AudioSource hiss = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            float dark = hiss.volume;

            _rig.Milestone(OpeningMilestone.PowerOn);
            Assert.IsTrue(_rig.Radio.Powered);
            yield return new WaitForSecondsRealtime(5f);

            Assert.Less(_rig.Radio.HissFloor, 0.05f, "the hiss has cleared");
            Assert.Less(hiss.volume, dark * 0.5f);
        }

        [UnityTest]
        public IEnumerator ARestoredPowerOn_HasNoHissToClear()
        {
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            Assert.AreEqual(0f, _rig.Radio.HissFloor, 1e-5f, "a load with power has no hiss to clear");
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(AudioTestRig.FindChildSource(_rig.Radio.transform, "Static").isPlaying);
        }

        [UnityTest]
        public IEnumerator NoMilestoneAnswerOrMusicRequest_BringsMusic()
        {
            _rig.Wake(true);
            _rig.Milestone(OpeningMilestone.FirstCharge);
            _rig.Milestone(OpeningMilestone.PowerOn);
            _rig.Milestone(OpeningMilestone.NightReady);
            _rig.Milestone(OpeningMilestone.SignalWest);
            _rig.Milestone(OpeningMilestone.CallAnswered);
            _rig.Events.Publish(new CallStateChanged(CallState.Answered, false));
            _rig.Events.Publish(new MusicMomentRequested());
            yield return new WaitForSecondsRealtime(3f);

            AudioSource[] sources = _rig.Radio.GetComponentsInChildren<AudioSource>();
            Assert.AreEqual(2, sources.Length, "the set carries only its static and swish");
            foreach (AudioSource source in sources)
            {
                Assert.IsTrue(source.name == "Static" || source.name == "TuningSwish", source.name);
            }
        }
    }
}
