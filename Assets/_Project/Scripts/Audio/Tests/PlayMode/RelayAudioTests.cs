using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The relay network in the running mix: a mast's repair beat, home's answer, the radio through the
    /// network and the radio-hop.</summary>
    public sealed class RelayAudioTests
    {
        // A mast 300 m east of the base (well past home's signal) with its pad there.
        private const string Mast = "relay.1";
        private static readonly Vector3 MastPad = new Vector3(300f, 0f, 0f);

        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            _rig.Reach.AddMast(Mast, MastPad);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        private IEnumerator WakeAndWaitForMusic()
        {
            _rig.Wake(true);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music after 07 wakes");
        }

        [UnityTest]
        public IEnumerator TheRepairBeat_PartStitchSlotCreakLamp_EachAtTheMast()
        {
            var socket = new Vector3(301f, 1f, 0f);
            _rig.Events.Publish(new RelayCued(RelayCue.PartCollected, Mast, socket));
            Assert.AreEqual("friend_part_complete", LastClip, "the amber part tone, resolved: one part per mast");

            var beam = new Vector3(300f, 2f, 0.5f);
            _rig.Events.Publish(new RelayCued(RelayCue.Stitched, Mast, beam));
            yield return new WaitForSeconds(0.6f);
            AudioSource stitch = _rig.Relays.StitchSource;
            Assert.IsTrue(stitch.isPlaying, "07's beam stitches the mast");
            Assert.Less(Vector3.Distance(stitch.transform.position, beam), 1e-4f);

            _rig.Events.Publish(new RelayCued(RelayCue.PartSlotted, Mast, socket));
            Assert.AreEqual("bell_tape_slot", LastClip, "the part clacks home");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(stitch.isPlaying, "the stitching has faded");

            _rig.Events.Publish(new RelayCued(RelayCue.Straightened, Mast, MastPad));
            Assert.AreEqual("relay_mast_creak", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, MastPad), 1e-4f);

            var lamp = new Vector3(300f, 6f, 0f);
            _rig.Events.Publish(new RelayCued(RelayCue.LampWarmed, Mast, lamp));
            Assert.AreEqual("relay_lamp_warm", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, lamp), 1e-4f);
        }

        [UnityTest]
        public IEnumerator HomeAnswers_FromItsDirection_WhenThePulseArrives()
        {
            _rig.Rover.Position = MastPad + new Vector3(0f, 0f, 5f);
            _rig.Reach.Light(Mast);
            _rig.Events.Publish(new RelayRestored(Mast, MastPad + Vector3.up * 6f, 1, 4));

            // 300 m to home at 45 m/s is past the 3 s cap: the answer comes after 3 s.
            yield return new WaitForSeconds(2.5f);
            Assert.AreNotEqual("relay_link", LastClip, "the pulse is still on its way");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual("relay_link", LastClip, "home answers");

            AudioSource answer = _rig.Relays.AnswerSource;
            Vector3 toward = (Vector3.zero - _rig.Rover.Position).normalized;
            Vector3 expected = _rig.Rover.Position + toward * 14f;
            Assert.Less(Vector3.Distance(answer.transform.position, expected), 0.5f, "from home's direction");
            Assert.Greater(answer.spatialBlend, 0.3f, "enough to hear the direction");
        }

        [UnityTest]
        public IEnumerator ALitMastHumsFaintly_OnlyUpClose()
        {
            _rig.Reach.Light(Mast);
            _rig.Rover.Position = MastPad + new Vector3(3f, 0f, 0f);
            yield return null;
            yield return null;
            AudioSource hum = _rig.Relays.MastHumSource;
            Assert.IsTrue(hum.isPlaying);
            Assert.Greater(hum.volume, 0f);
            Assert.AreEqual(0.75f, hum.pitch, 1e-4f, "a fourth below 07's own lamp");

            _rig.Rover.Position = MastPad + new Vector3(80f, 0f, 0f);
            yield return null;
            yield return null;
            Assert.IsFalse(hum.isPlaying, "silent from afar");
        }

        [UnityTest]
        public IEnumerator AtALitMastFarFromHome_TheRadioIsWarm_AndLightingItWarmsItInGently()
        {
            yield return WakeAndWaitForMusic();
            _rig.Rover.Position = MastPad;
            yield return new WaitForSecondsRealtime(3f);
            Assert.Less(_rig.Radio.Clarity, 0.05f, "300 m out: static");
            float far = _rig.Soundscape.Farness;
            Assert.Greater(far, 0.6f, "and fading towards near-silence");

            _rig.Reach.Light(Mast);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Greater(_rig.Soundscape.Farness, far * 0.6f, "warming, never a snap");
            yield return new WaitForSecondsRealtime(6f);
            Assert.Less(_rig.Soundscape.Farness, 0.05f);
            Assert.Greater(_rig.Radio.Clarity, 0.95f, "the radio is clear in the mast's reach");
        }

        [UnityTest]
        public IEnumerator TheRadioHop_StaticRisesWithTheFade_TheNewPlaceTakesHoldInTheDark_ThenResolves()
        {
            yield return WakeAndWaitForMusic();
            _rig.Reach.Light(Mast);
            _rig.Events.Publish(new RadioHopStarted("home", Mast));
            Assert.AreEqual("radio_hop_out", LastClip);
            Assert.AreEqual(0f, _rig.Director.LastVoice.spatialBlend, "a radio sound, flat");

            yield return new WaitForSecondsRealtime(0.85f);
            Assert.Greater(_rig.Radio.HopAmount, 0.95f, "all static at full dark");
            _rig.Rover.Position = FakeWorldAnchors.DeepInside;
            _rig.Events.Publish(new RoverPlaced(FakeWorldAnchors.DeepInside, Quaternion.identity));
            Assert.Greater(_rig.Canyon.Inside, 0.9f, "the new place's air at once, unseen");

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("radio_hop_in", LastClip, "resolving as the view eases in");
            yield return new WaitForSecondsRealtime(0.7f);
            _rig.Events.Publish(new RadioHopFinished(Mast));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(0f, _rig.Radio.HopAmount, 1e-3f, "resolved");
            Assert.IsTrue(_rig.Radio.MusicStarted);
        }
    }
}
