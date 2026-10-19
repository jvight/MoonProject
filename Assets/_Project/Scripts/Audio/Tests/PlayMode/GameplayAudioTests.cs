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
            _tuning = AudioTestRig.LoadTuning<GameplayAudioTuning>("GameplayAudioTuning.asset");
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private GameplayAudioTuning _tuning;

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

        [UnityTest]
        public IEnumerator AStructureTakingPower_ThunksAndSwells_NeverOnARestoreOrADim()
        {
            var spot = new Vector3(2f, 0f, -5f);
            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 1f, false));
            AssertLastPlayed("structure_power_on", spot);
            int plays = _rig.Director.PlayCount;

            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 0.5f, false));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "a dim is not a power-on");
            _rig.Events.Publish(new StructurePowerChanged(8, "charge_pad", spot, 1f, true));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "a save restoring power stays quiet");
            _rig.Events.Publish(new StructurePowerChanged(9, "lamp_post", spot, 1f, false));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "several at once: one swell speaks for them");

            yield return new WaitForSeconds(_tuning.PowerOnMinInterval + 0.1f);
            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 0f, false));
            _rig.Events.Publish(new StructurePowerChanged(7, "charge_pad", spot, 1f, false));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount, "power lost and found again sounds again");
            AssertLastPlayed("structure_power_on", spot);
        }

        [Test]
        public void AStructureBuiltIntoAPoweredBase_IsLitByItsBuild_NotASecondSwell()
        {
            var spot = new Vector3(6f, 0f, 1f);
            _rig.Events.Publish(new StructureBuilt(11, "lamp_post", spot, 0f));
            AssertLastPlayed("build_structure_complete", spot);
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new StructurePowerChanged(11, "lamp_post", spot, 1f, false));
            Assert.AreEqual(plays, _rig.Director.PlayCount);
        }

        [Test]
        public void EachBaseLampLit_PlaysOneLampSound_AtItsStructure_AStepHigherAlongTheOrder()
        {
            Vector3[] spots = { new Vector3(3f, 0f, 0f), new Vector3(0f, 0f, 9f), new Vector3(-6f, 0f, 2f) };
            for (int i = 0; i < spots.Length; i++)
            {
                _rig.Events.Publish(new StructureBuilt(i + 1, "solar_array", spots[i], 0f, true));
            }

            int plays = _rig.Director.PlayCount;
            for (int i = 0; i < spots.Length; i++)
            {
                _rig.Events.Publish(new BaseLampLit(i + 1, i));
                AssertLastPlayed($"base_lamp_on_0{i + 1}", spots[i]);
            }

            Assert.AreEqual(plays + spots.Length, _rig.Director.PlayCount, "one lamp sound per BaseLampLit");
        }

        [UnityTest]
        public IEnumerator TheSkyPhaseAlone_PlaysNoLampSound()
        {
            _rig.Events.Publish(new StructureBuilt(1, "solar_array", Vector3.one, 0f, true));
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunset, 0f));
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Earthlit, 0f));
            float until = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.AreEqual(plays, _rig.Director.PlayCount, "nightfall itself lights nothing: BaseLamps does");
            Assert.IsNull(_rig.FindPlayingVoice("base_lamp_on"));
        }
    }
}
