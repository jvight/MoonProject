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

        [Test]
        public void KenjisGaragePort_UnfoldsItsArm_FitsTheRing_AndTheRingHumsForTheFirstTime()
        {
            _rig.Events.Publish(new RoverKitInstalling(RoverKitPiece.TetherRing, false));
            Assert.AreEqual("port_arm", LastClip, "the folded arm reaches for 07");

            _rig.Events.Publish(new RoverKitFitted(RoverKitPiece.TetherRing, false, string.Empty));
            Assert.AreEqual("tether_ring_wake", LastClip, "the ring's first hum");
            Assert.IsTrue(RecentlyPlayed("bay_fitted_chime", 2), "the two-note rise");
            Assert.IsTrue(RecentlyPlayed("bay_weld", 3), "sparks");

            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new RoverKitInstalling(RoverKitPiece.HoverCoils, false));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "the bay's arms have their own sounds");
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
        public IEnumerator TheDock_ConnectsHumsWhileItChargesAndReleases()
        {
            var dock = new Vector3(3f, 0f, -5f);
            _rig.Events.Publish(new RoverDockChanged(true, dock, Quaternion.identity));
            Assert.AreEqual("dock_connect", LastClip);
            _rig.Events.Publish(new ChargingChanged(true, ChargerKind.Dock, dock));
            yield return new WaitForSeconds(2f);
            AudioSource hum = Stations.ChargeSource;
            Assert.IsTrue(hum.isPlaying, "a very soft charging hum");
            Assert.Less(Vector3.Distance(hum.transform.position, dock), 1e-4f);

            _rig.Events.Publish(new BatteryChanged(4, 4, true, false));
            _rig.Events.Publish(new ChargingChanged(false, ChargerKind.Dock, dock));
            yield return null;
            Assert.AreEqual("dock_full", LastClip, "the last cell filled: a gentle 'full' tone");
            _rig.Events.Publish(new RoverDockChanged(false, dock, Quaternion.identity));
            Assert.AreEqual("dock_release", LastClip);
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(hum.isPlaying);
        }

        [Test]
        public void PowerReturning_WakesEachPlace_ButNotOnARestore_AndAsleepStationsAnswerSoftly()
        {
            _rig.Events.Publish(new BasePowerChanged(BasePowerStage.Bay, false));
            Assert.AreEqual("base_wake_bay", LastClip, "the bay breathes awake");
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BasePowerChanged(BasePowerStage.Chart, true));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "a save restoring power is silent");

            var lift = new Vector3(40f, 2f, -30f);
            _rig.Events.Publish(new StationNotYet("notyet.chart", lift, 52f));
            Assert.AreEqual("station_not_yet", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, lift), 1e-4f, "where it sleeps");
        }

        [UnityTest]
        public IEnumerator TheBellyLift_CreaksOpensRidesAndClunks_AndKenjisPlotterDraws()
        {
            var winch = new Vector3(30f, 1f, -20f);
            _rig.Events.Publish(new LiftCued(LiftCue.WinchCreaked, winch));
            StringAssert.StartsWith("lift_winch_creak", LastClip);
            _rig.Events.Publish(new LiftCued(LiftCue.HatchOpened, winch + Vector3.up * 2.6f));
            Assert.AreEqual("lift_hatch_open", LastClip);
            _rig.Events.Publish(new LiftCued(LiftCue.RideStarted, winch));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(Stations.LiftSource.isPlaying, "the ride ratchets and hums");
            _rig.Events.Publish(new LiftCued(LiftCue.RideStopped, winch + Vector3.up * 8f));
            Assert.AreEqual("lift_stop_clunk", LastClip);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(Stations.LiftSource.isPlaying, "and lets go at the stop");
            _rig.Events.Publish(new LiftCued(LiftCue.HatchClosed, winch + Vector3.up * 2.6f));
            Assert.AreEqual("lift_hatch_close", LastClip);

            _rig.Events.Publish(new BlueprintDrawing("kit.cargo", winch, 6f));
            Assert.AreEqual("plotter_draw", LastClip);
            _rig.Events.Publish(new BlueprintFound("kit.cargo", "site.garage", winch));
            Assert.AreEqual("blueprint_found", LastClip);
        }

        [UnityTest]
        public IEnumerator TheLab_HasItsRoomTone_DetentsInKey_TheReaderDecodes_AndTheMapInks()
        {
            _rig.Events.Publish(new LabEntered(LabStation.Reader));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(_rig.Lab.RoomSource.isPlaying, "the lab's room tone comes in");
            _rig.Events.Publish(new LabStationFaced(LabStation.Board));
            Assert.AreEqual("lab_detent_03", LastClip, "each station settles on its own pentatonic note");
            _rig.Events.Publish(new BlueprintDelivered("kit.cargo"));
            Assert.AreEqual("reader_take", LastClip);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(_rig.Lab.ReelSource.isPlaying, "the reel turns while it decodes");
            _rig.Events.Publish(new FindDecoded("kit.cargo"));
            Assert.AreEqual("reader_decode_chime", LastClip);
            _rig.Events.Publish(new RecoveryProgressChanged(RecoveryTarget.Power, 3, 3, true));
            _rig.Events.Publish(new RecoveryProgressChanged(RecoveryTarget.Link, 1, 3, false));
            Assert.AreEqual("reader_decode_chime", LastClip, "a load or a lamp alone plays no chord");
            _rig.Events.Publish(new RecoveryProgressChanged(RecoveryTarget.Power, 3, 3, false));
            Assert.AreEqual("recovery_row_chord", LastClip, "Power restored: the row's warm chord");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(_rig.Lab.ReelSource.isPlaying, "and stops once decoded");
            _rig.Events.Publish(new MapPlaceSeen("site.depot", Vector3.zero));
            Assert.AreEqual("map_pencil", LastClip);
            _rig.Events.Publish(new MapInkCued("site.depot", Vector3.zero, 1));
            Assert.AreEqual("map_pen_02", LastClip, "the pen climbs the scale place by place");
            _rig.Events.Publish(new LabLeft());
            yield return new WaitForSeconds(3f);
            Assert.IsFalse(_rig.Lab.RoomSource.isPlaying, "the room tone lets go as 07 rides down");
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
