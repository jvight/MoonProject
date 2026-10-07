using NUnit.Framework;
using UnityEngine;
using MoonProject.Editor.Import;

namespace MoonProject.Editor.Tests
{
    public sealed class ImportRuleSetTests
    {
        [Test]
        public void Sfx_DecompressesOnLoad_PreloadsAndIsMonoFor3D()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/Rover/land_soft.wav", out var rule));

            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, rule.LoadType);
            Assert.IsTrue(rule.Preload);
            Assert.IsTrue(rule.ForceMono);
            Assert.IsFalse(rule.LoadInBackground);
        }

        [Test]
        public void Sfx_Under2DFolder_KeepsChannels()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/2D/ui_chime.wav", out var direct));
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/UI/2D/tick.wav", out var nested));

            Assert.IsFalse(direct.ForceMono);
            Assert.IsFalse(nested.ForceMono);
        }

        [Test]
        public void SfxLoops_ArePcmSoTheirSeamStaysSampleExact()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/rover_hum_loop.wav", out var loop));
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/2D/radio_static_loop.wav", out var flat));
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/sonar_ping.wav", out var shot));
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFX/loop_start.wav", out var named));

            Assert.AreEqual(AudioCompressionFormat.PCM, loop.CompressionFormat);
            Assert.IsTrue(loop.ForceMono);
            Assert.AreEqual(AudioCompressionFormat.PCM, flat.CompressionFormat);
            Assert.IsFalse(flat.ForceMono);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, shot.CompressionFormat);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, named.CompressionFormat, "only a _loop suffix counts");
        }

        [Test]
        public void Music_Streams()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/Music/track_01.ogg", out var rule));

            Assert.AreEqual(AudioClipLoadType.Streaming, rule.LoadType);
            Assert.AreEqual(0.7f, rule.Quality, 1e-6f);
            Assert.IsFalse(rule.Preload);
            Assert.IsTrue(rule.LoadInBackground);
            Assert.IsFalse(rule.ForceMono);
        }

        [Test]
        public void Jingles_LoadLikeSpatialSfx()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/Music/Jingles/bell_jingle_short.wav",
                out var rule));

            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, rule.LoadType, "a stinger must start on cue");
            Assert.IsTrue(rule.Preload);
            Assert.IsTrue(rule.ForceMono, "played in 3D at the friend");
            Assert.IsFalse(rule.LoadInBackground);
        }

        [Test]
        public void Ambience_IsCompressedInMemory()
        {
            Assert.IsTrue(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/Ambience/hush.wav", out var rule));

            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, rule.LoadType);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, rule.CompressionFormat, "ambience loops stay Vorbis");
            Assert.IsTrue(rule.Preload);
            Assert.IsFalse(rule.ForceMono);
        }

        [Test]
        public void AudioOutsideRuleFolders_IsLeftAlone()
        {
            Assert.IsFalse(ImportRuleSet.TryGetAudioRule("Assets/Sounds/Music/music_1.mp3", out _));
            Assert.IsFalse(ImportRuleSet.TryGetAudioRule("Assets/_Project/Audio/SFXExtra/a.wav", out _));
            Assert.IsFalse(ImportRuleSet.TryGetAudioRule(null, out _));
        }

        [Test]
        public void GeneratedTextures_AreSrgbUnlessNamedLinear()
        {
            Assert.IsTrue(ImportRuleSet.TryGetTextureRule("Assets/_Project/Generated/Art/Palette.png", out var color));
            Assert.IsTrue(ImportRuleSet.TryGetTextureRule("Assets/_Project/Generated/World/Noise_Linear.png", out var data));
            Assert.IsTrue(ImportRuleSet.TryGetTextureRule("Assets\\_Project\\Generated\\World\\mask_linear.png", out var lower));

            Assert.IsTrue(color.Srgb);
            Assert.IsFalse(data.Srgb);
            Assert.IsFalse(lower.Srgb);
        }

        [Test]
        public void TexturesOutsideGenerated_AreLeftAlone()
        {
            Assert.IsFalse(ImportRuleSet.TryGetTextureRule("Assets/Moon/Textures/Mask.psd", out _));
            Assert.IsFalse(ImportRuleSet.TryGetTextureRule("Assets/_Project/GeneratedOld/x.png", out _));
        }
    }
}
