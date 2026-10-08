using System.IO;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class SalvageAudioLogicTests
    {
        private const float Frame = 1f / 60f;

        private SalvageAudioTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<SalvageAudioTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void SalvageMelody_ANewChainStartsAtTheBottomAgain()
        {
            Assert.AreEqual(3, SalvageMelody.NoteIndex(3, 8, _tuning.MelodyTopWindow));
            Assert.AreEqual(0, SalvageMelody.NoteIndex(0, 8, _tuning.MelodyTopWindow), "the chain window passed");
        }

        [TestCase(SalvageMaterial.Metal, "salvage_cut_metal", "metal")]
        [TestCase(SalvageMaterial.Wiring, "salvage_cut_wiring", "wiring")]
        [TestCase(SalvageMaterial.Optics, "salvage_cut_optics", "optics")]
        public void EachMaterial_HasItsOwnCutAndBreak(SalvageMaterial material, string cut, string breakLabel)
        {
            Assert.AreEqual(cut, SalvageSounds.CutCue(material));
            Assert.AreEqual(breakLabel, SalvageSounds.BreakLabel(material));
        }

        [Test]
        public void EveryMaterialsBreak_IsRendered()
        {
            string manifest = File.ReadAllText(Path.Combine(Application.dataPath, "..", "tools", "audio",
                "sfx_manifest.json"));
            foreach (SalvageMaterial material in new[]
                     { SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics })
            {
                StringAssert.Contains($"salvage_break_{SalvageSounds.BreakLabel(material)}.wav", manifest);
            }
        }

        [Test]
        public void TheBeam_ClimbsOnePentatonicNotePerStep_ToItsTop_InKey()
        {
            var beam = new CutBeamModel(_tuning);
            beam.Start();
            float t = 0f;
            int lastStep = 0;
            while (t < _tuning.StepSeconds * (_tuning.TopStep + 3))
            {
                beam.Update(Frame);
                t += Frame;
                Assert.GreaterOrEqual(beam.Step, lastStep, "only upwards");
                Assert.LessOrEqual(beam.Step - lastStep, 1, "one note at a time");
                lastStep = beam.Step;
            }

            Assert.AreEqual(_tuning.TopStep, beam.Step, "holds at the top");
            Assert.AreEqual(CutBeamModel.Ratio(_tuning.TopStep), beam.TonePitch, 1e-3f);
            Assert.AreEqual(_tuning.TextureRise, beam.TexturePitch, 1e-4f, "the grind tightens");
            float[] expected = { 1f, 1.1225f, 1.2599f, 1.4983f, 1.6818f, 2f };
            for (int step = 0; step < expected.Length; step++)
            {
                Assert.AreEqual(expected[step], CutBeamModel.Ratio(step), 1e-3f, $"D major pentatonic step {step}");
            }
        }

        [Test]
        public void TheTone_GlidesOntoEachNote_NeverJumps()
        {
            var beam = new CutBeamModel(_tuning);
            beam.Start();
            float last = beam.TonePitch;
            for (int i = 0; i < 300; i++)
            {
                beam.Update(Frame);
                Assert.Less(Mathf.Abs(beam.TonePitch - last), 0.08f, $"frame {i}: a glide, not a step");
                last = beam.TonePitch;
            }
        }

        [Test]
        public void LettingGo_FadesSoftly_ABreakFallsAwayQuickly_AndTheNextHoldClimbsFromD()
        {
            var beam = new CutBeamModel(_tuning);
            beam.Start();
            Run(beam, 2f);
            Assert.AreEqual(1f, beam.Gain, 1e-4f);
            beam.Stop(false);
            Run(beam, _tuning.ReleaseFade * 0.5f);
            Assert.Greater(beam.Gain, 0.2f, "a soft release, not a cut-off");
            Run(beam, _tuning.ReleaseFade);
            Assert.IsFalse(beam.Audible);

            beam.Start();
            Assert.AreEqual(0, beam.Step);
            Assert.AreEqual(1f, beam.TonePitch, "the climb starts again from D");
            Run(beam, 2f);
            beam.Stop(true);
            Run(beam, _tuning.BreakFade + Frame);
            Assert.IsFalse(beam.Audible, "the break-off takes over at once");
        }

        private static void Run(CutBeamModel beam, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                beam.Update(Frame);
            }
        }
    }
}
