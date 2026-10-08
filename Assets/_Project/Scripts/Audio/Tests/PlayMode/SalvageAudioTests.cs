using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Salvage in the running mix: the cutting beam, the break-off and the salvage melody.</summary>
    public sealed class SalvageAudioTests
    {
        private static readonly Vector3 CutPoint = new Vector3(12f, 1.5f, -6f);

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

        private SalvageAudio Salvage => _rig.Salvage;

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        [UnityTest]
        public IEnumerator Cutting_PlaysTheMaterialsTexture_AtTheCut_WithATone_ThatClimbs()
        {
            _rig.Events.Publish(new SalvageCutStarted(CutPoint, SalvageMaterial.Wiring));
            yield return new WaitForSeconds(0.4f);
            AudioSource texture = Salvage.TextureSource;
            AudioSource tone = Salvage.ToneSource;
            Assert.IsTrue(texture.isPlaying);
            Assert.IsTrue(tone.isPlaying);
            StringAssert.StartsWith("salvage_cut_wiring", texture.clip.name, "wiring crackles");
            Assert.Less(Vector3.Distance(texture.transform.position, CutPoint), 1e-4f);
            Assert.AreEqual(1f, texture.spatialBlend);
            float early = tone.pitch;

            yield return new WaitForSeconds(2f);
            Assert.Greater(Salvage.BeamStep, 1, "the edge climbs as the cut goes on");
            Assert.Greater(tone.pitch, early * 1.2f);
        }

        [UnityTest]
        public IEnumerator LettingGo_FadesSoftly()
        {
            _rig.Events.Publish(new SalvageCutStarted(CutPoint, SalvageMaterial.Metal));
            yield return new WaitForSeconds(0.6f);
            AudioSource texture = Salvage.TextureSource;
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new SalvageCutStopped(false));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no break-off: the piece is still on");
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(texture.isPlaying, "fading, not cut off");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(texture.isPlaying);
        }

        [UnityTest]
        public IEnumerator ThePieceComingLoose_BreaksOffWithItsMaterialsCrack()
        {
            _rig.Events.Publish(new SalvageCutStarted(CutPoint, SalvageMaterial.Optics));
            yield return new WaitForSeconds(0.5f);
            StringAssert.StartsWith("salvage_cut_optics", Salvage.TextureSource.clip.name);
            _rig.Events.Publish(new SalvageCutStopped(true));
            Assert.AreEqual("salvage_break_optics", LastClip);
            Assert.Less(Vector3.Distance(_rig.Director.LastVoice.transform.position, CutPoint), 1e-4f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(Salvage.TextureSource.isPlaying, "the beam falls away as the piece comes loose");

            _rig.Events.Publish(new SalvageCutStarted(CutPoint, SalvageMaterial.Metal));
            yield return null;
            StringAssert.StartsWith("salvage_cut_metal", Salvage.TextureSource.clip.name, "a deeper grind for metal");
            _rig.Events.Publish(new SalvageCutStopped(true));
            Assert.AreEqual("salvage_break_metal", LastClip);
        }

        [Test]
        public void TheSalvageMelody_ClimbsWithTheSitesChain_AndDropsBackOnANewOne()
        {
            string[] expected =
            {
                "salvage_chime_D5", "salvage_chime_E5", "salvage_chime_Fs5", "salvage_chime_A5", "salvage_chime_B5",
                "salvage_chime_D6", "salvage_chime_E6", "salvage_chime_Fs6", "salvage_chime_E6",
            };
            var cargo = new Vector3(0f, 1f, 0f);
            for (int step = 0; step < expected.Length; step++)
            {
                _rig.Events.Publish(new MaterialSalvaged(SalvageMaterial.Metal, 2, cargo, "site.depot", step));
                Assert.AreEqual(expected[step], LastClip, $"step {step}");
                Assert.AreEqual(1f, _rig.Director.LastVoice.pitch, 1e-6f, "chimes stay in key");
            }

            _rig.Events.Publish(new MaterialSalvaged(SalvageMaterial.Optics, 1, cargo, "trail.kestrel", 0));
            Assert.AreEqual("salvage_chime_D5", LastClip, "a new chain starts at the bottom");
        }
    }
}
