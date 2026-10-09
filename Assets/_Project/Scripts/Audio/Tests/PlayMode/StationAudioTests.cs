using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The base's machines in the running mix: the bay's fitting, the tower's port and the dock.</summary>
    public sealed class StationAudioTests
    {
        private const string Upgrade = "rover.hover_jump";

        private AudioTestRig _rig;
        private float _firstDropPitch;
        private float _lastDropPitch;

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

        private StationAudio Stations => _rig.Stations;

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        [UnityTest]
        public IEnumerator ABayFitting_FeedArmsTurntableWeld()
        {
            var mouth = new Vector3(18f, 0.8f, 2f);
            _rig.Events.Publish(new StationCued(StationCue.FeedStarted, Upgrade, mouth));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(Stations.FeedSource.isPlaying, "the feeding beam hums");
            Assert.Less(Vector3.Distance(Stations.FeedSource.transform.position, mouth), 1e-4f);
            yield return DropBundles(mouth, 3);
            int clunks = _rig.Director.PlayCount;
            _rig.Events.Publish(new StationCued(StationCue.Fed, Upgrade, mouth));
            Assert.AreEqual(clunks, _rig.Director.PlayCount, "all in: no extra clunk, the hum settles away");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(Stations.FeedSource.isPlaying);

            float top = _lastDropPitch;
            _rig.Events.Publish(new StationCued(StationCue.FeedStarted, Upgrade, mouth));
            yield return DropBundles(mouth, 2);
            Assert.Less(_firstDropPitch, top, "the next feed starts in an empty bin again");
            _rig.Events.Publish(new StationCued(StationCue.Fed, Upgrade, mouth));

            _rig.Events.Publish(new RoverBayFitting(Upgrade));
            Transform upper = _rig.Bay.GetArmJoint(0, RoverBayJoint.Upper);
            Transform tip = _rig.Bay.GetArmJoint(0, RoverBayJoint.Tip);
            AudioSource servo = Stations.ArmSource(0);
            bool whirred = false;
            float start = Time.time;
            while (Time.time - start < 1.2f)
            {
                upper.localRotation = Quaternion.Euler(60f * (Time.time - start), 0f, 0f);
                yield return null;
                whirred |= servo.isPlaying && servo.volume > 0f;
            }

            Assert.IsTrue(whirred, "the old arm whirs as it carries the piece");
            Assert.Less(Vector3.Distance(servo.transform.position, tip.position), 1e-3f, "at the moving tip");
            Assert.IsFalse(Stations.ArmSource(1).isPlaying, "the arm at rest stays quiet");

            int plays = _rig.Director.PlayCount;
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(servo.isPlaying);
            Assert.IsTrue(RecentlyPlayed("bay_arm_sigh", _rig.Director.PlayCount - plays), "a hydraulic sigh at rest");

            _rig.Rover.Position = tip.position + Vector3.down;
            _rig.Events.Publish(new RoverKitFitted(RoverKitPiece.HoverCoils, false, Upgrade));
            Assert.AreEqual("bay_fitted_chime", LastClip, "done for 07");
            Assert.IsTrue(RecentlyPlayed("bay_weld", 2), "welded on");

            bool rumbled = false;
            start = Time.time;
            while (Time.time - start < 1f)
            {
                _rig.Bay.Turntable.localRotation = Quaternion.Euler(0f, 40f * (Time.time - start), 0f);
                yield return null;
                rumbled |= Stations.TurntableSource.isPlaying;
            }

            Assert.IsTrue(rumbled, "the turntable turns 07 to show the piece");
        }

        [Test]
        public void AGiftFromAFriend_IsNotWeldedByTheBay()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new RoverKitFitted(RoverKitPiece.LampBar, true, string.Empty));
            Assert.AreEqual(plays, _rig.Director.PlayCount);
        }

        [UnityTest]
        public IEnumerator TheTowerPort_OpensStitchesRisingAndShuts()
        {
            var hatch = new Vector3(-8f, 0.5f, 12f);
            _rig.Events.Publish(new StationCued(StationCue.HatchOpened, "radio_tower", hatch));
            Assert.AreEqual("port_hatch_open", LastClip);
            _rig.Events.Publish(new StationCued(StationCue.StitchStarted, "radio_tower", hatch));
            yield return new WaitForSeconds(0.5f);
            AudioSource stitch = Stations.StitchSource;
            Assert.IsTrue(stitch.isPlaying);
            float early = stitch.pitch;
            yield return new WaitForSeconds(2.5f);
            Assert.Greater(stitch.pitch, early * 1.15f, "rising with the new section");

            _rig.Events.Publish(new StationCued(StationCue.HatchClosed, "radio_tower", hatch));
            Assert.AreEqual("port_hatch_close", LastClip);
            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(stitch.isPlaying);
        }

        [UnityTest]
        public IEnumerator TheDock_ConnectsHumsAndReleases()
        {
            var dock = new Vector3(3f, 0f, -5f);
            _rig.Events.Publish(new RoverDockChanged(true, dock, Quaternion.identity));
            Assert.AreEqual("dock_connect", LastClip);
            yield return new WaitForSeconds(2f);
            AudioSource hum = Stations.ChargeSource;
            Assert.IsTrue(hum.isPlaying, "a very soft charging hum");
            Assert.Less(Vector3.Distance(hum.transform.position, dock), 1e-4f);

            _rig.Events.Publish(new RoverDockChanged(false, dock, Quaternion.identity));
            Assert.AreEqual("dock_release", LastClip);
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(hum.isPlaying);
        }

        [Test]
        public void BellsKnobTap_TicksAtHerDial()
        {
            var dial = new Vector3(-3f, 1f, 7f);
            _rig.Events.Publish(new BellCued(BellCue.DialTapped, dial));
            Assert.AreEqual("bell_knob_tap", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, dial), 1e-4f);
        }

        /// <summary>Drops <paramref name="count"/> bundles a beat apart: each clunks at the mouth, never the same
        /// variant twice running, each a little higher than the one before.</summary>
        private IEnumerator DropBundles(Vector3 mouth, int count)
        {
            string lastClip = string.Empty;
            float lastPitch = 0f;
            for (int i = 0; i < count; i++)
            {
                int plays = _rig.Director.PlayCount;
                _rig.Events.Publish(new StationCued(StationCue.BundleDropped, Upgrade, mouth));
                Assert.AreEqual(plays + 1, _rig.Director.PlayCount, $"bundle {i} clunks in");
                StringAssert.StartsWith("hopper_clunk", LastClip);
                Assert.AreNotEqual(lastClip, LastClip, $"bundle {i} is not the clunk before it again");
                AudioSource voice = _rig.Director.LastVoice;
                Assert.Less(Vector3.Distance(voice.transform.position, mouth), 1e-4f, "at the hopper's mouth");
                Assert.Greater(voice.pitch, lastPitch, $"bundle {i} lands on the ones already in");
                lastClip = LastClip;
                lastPitch = voice.pitch;
                if (i == 0)
                {
                    _firstDropPitch = lastPitch;
                }

                yield return new WaitForSeconds(0.15f);
            }

            Assert.Less(lastPitch, 1.1f, "a touch higher, never a squeak");
            _lastDropPitch = lastPitch;
        }

        private bool RecentlyPlayed(string name, int within)
        {
            for (int i = 0; i < Mathf.Min(within, 16); i++)
            {
                AudioClip clip = _rig.Director.RecentClip(i);
                if (clip != null && clip.name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
