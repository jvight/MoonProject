using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The base's events, published on the bus, play the right cue (and the weld loop starts and stops).</summary>
    public sealed class GameplayAudioTests
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

        private AudioSource Last => _rig.Director.LastVoice;

        private void AssertLastPlayed(string clipName, Vector3 position)
        {
            Assert.IsNotNull(Last);
            Assert.AreEqual(clipName, Last.clip.name);
            Assert.IsTrue(Last.isPlaying, clipName);
            Assert.Greater(Last.volume, 0f, clipName);
            Assert.AreEqual(1f, Last.spatialBlend, clipName);
            Assert.Less(Vector3.Distance(position, Last.transform.position), 1e-4f, clipName);
        }

        [UnityTest]
        public IEnumerator BuildModeCues_PlayAllEightCues_AndFadeWeldLoop()
        {
            var spot = new Vector3(4f, 0f, 6f);

            _rig.Events.Publish(new BuildCue(BuildCueKind.Ghost, spot));
            AssertLastPlayed("build_ghost", spot);

            _rig.Events.Publish(new BuildCue(BuildCueKind.Snap, spot));
            AssertLastPlayed("build_snap", spot);

            _rig.Events.Publish(new BuildCue(BuildCueKind.Invalid, spot));
            AssertLastPlayed("build_invalid", spot);

            _rig.Events.Publish(new StructurePlaced(1, "solar_array", spot, 0f));
            AssertLastPlayed("build_place", spot);

            var weldSpot = new Vector3(4f, 0.8f, 6f);
            _rig.Events.Publish(new BuildWeldStateChanged(true, weldSpot, 0.25f));
            yield return new WaitForSecondsRealtime(0.4f);

            AudioSource weldLoop = _rig.Gameplay.BuildWeldSource;
            Assert.IsNotNull(weldLoop);
            Assert.AreEqual("build_weld_loop", weldLoop.clip.name);
            Assert.IsTrue(weldLoop.isPlaying, "weld loop plays while welding");
            Assert.IsTrue(weldLoop.loop);
            Assert.Greater(weldLoop.volume, 0f);
            Assert.Less(Vector3.Distance(weldSpot, weldLoop.transform.position), 1e-4f);

            _rig.Events.Publish(new BuildCue(BuildCueKind.StageComplete, spot));
            AssertLastPlayed("build_stage_complete", spot);

            _rig.Events.Publish(new StructureBuilt(1, "solar_array", spot, 0f));
            _rig.Events.Publish(new BuildWeldStateChanged(false, Vector3.zero, 0f));
            AssertLastPlayed("build_structure_complete", spot);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(weldLoop.isPlaying, "weld loop fades out after structure completes");

            _rig.Events.Publish(new StructureRemoved(1, "solar_array", spot));
            AssertLastPlayed("build_deconstruct", spot);
        }

        [Test]
        public void AStructureTakingPower_ThunksAndSwellsOnce_NeverOnARestoreOrADim()
        {
            var spot = new Vector3(2f, 0f, -5f);
            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 1f, false));
            AssertLastPlayed("structure_power_on", spot);
            AudioSource first = Last;

            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 0.5f, false));
            Assert.AreSame(first, Last, "a dim is not a power-on");

            _rig.Events.Publish(new StructurePowerChanged(8, "charge_pad", spot, 1f, true));
            Assert.AreSame(first, Last, "a save restoring power stays quiet");

            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 0f, false));
            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 1f, false));
            Assert.AreNotSame(first, Last, "power lost and found again sounds again");
            AssertLastPlayed("structure_power_on", spot);
        }

        [UnityTest]
        public IEnumerator TheFirstNightfall_LightsTheBaseOneByOne_NearestFirst_ClimbingTheScale()
        {
            var near = new Vector3(3f, 0f, 0f);
            var far = new Vector3(0f, 0f, 20f);
            _rig.Events.Publish(new StructureBuilt(1, "solar_array", far, 0f, true));
            _rig.Events.Publish(new StructureBuilt(2, "charge_pad", near, 0f, true));
            Assert.AreEqual(2, _rig.Gameplay.NightLampCount);

            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunset, 0f));
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Earthlit, 0f));
            Assert.IsTrue(_rig.Gameplay.NightLightsRunning);

            AudioSource firstLamp = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (firstLamp == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                firstLamp = _rig.FindPlayingVoice("base_lamp_on");
            }

            Assert.IsNotNull(firstLamp, "the first lamp clicks on after the night settles");
            Assert.AreEqual("base_lamp_on_01", firstLamp.clip.name, "the first lamp is the lowest note");
            Assert.Less(Vector3.Distance(near, firstLamp.transform.position), 1e-4f, "nearest to the base first");

            while (_rig.Gameplay.NightLightsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual("base_lamp_on_02", Last.clip.name, "each lamp a step higher");
            Assert.Less(Vector3.Distance(far, Last.transform.position), 1e-4f);
        }

        [Test]
        public void ANightLoaded_OrADawn_DoesNotLightTheLamps()
        {
            _rig.Events.Publish(new StructureBuilt(1, "solar_array", Vector3.one, 0f, true));
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Earthlit, 0f));
            Assert.IsFalse(_rig.Gameplay.NightLightsRunning, "night without a sunset (a load) is already lit");

            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunset, 0f));
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Earthlit, 0f));
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunrise, 0f));
            Assert.IsFalse(_rig.Gameplay.NightLightsRunning, "a dawn stops the sequence");
        }
    }
}
