using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// The radio before and at Power on (opening v2 B0 and B8): only a faint signal hiss while the base is dark, then
    /// the hiss clears and the first music fades in, low and warm; a save that already has power just plays.
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
        public IEnumerator PowerOn_ClearsTheHiss_AndTheFirstMusicFadesInSlowly()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);
            AudioSource hiss = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            float dark = hiss.volume;

            _rig.Milestone(OpeningMilestone.PowerOn);
            Assert.IsTrue(_rig.Radio.Powered);
            AudioSource swish = AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");
            Assert.IsTrue(swish.isPlaying, "the dial finds the station");
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(_rig.Radio.MusicStarted);
            float early = _rig.LoudestDeck().volume;

            yield return new WaitForSecondsRealtime(4f);
            Assert.Greater(_rig.LoudestDeck().volume, early, "the music is still swelling in seconds later");
            Assert.Less(_rig.Radio.HissFloor, 0.05f, "the hiss has cleared");
            Assert.Less(hiss.volume, dark * 0.5f);
        }

        [UnityTest]
        public IEnumerator ARestoredPowerOn_JustPlays_WithoutTheSwish()
        {
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            Assert.AreEqual(0f, _rig.Radio.HissFloor, 1e-5f, "a load with power has no hiss to clear");
            _rig.Wake(false);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the wake-up starts the music");
        }

        [Test]
        public void OtherMilestones_LeaveTheRadioDark()
        {
            _rig.Wake(true);
            _rig.Milestone(OpeningMilestone.FirstCharge);
            _rig.Milestone(OpeningMilestone.NightReady);
            Assert.IsFalse(_rig.Radio.Powered);
        }
    }
}
