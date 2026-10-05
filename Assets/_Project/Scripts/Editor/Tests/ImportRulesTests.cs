using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Import;

namespace MoonProject.Editor.Tests
{
    /// <summary>
    /// Imports real files into the rule folders and checks the importer settings. Every folder the test creates is
    /// recorded and deleted again (deepest first), and the test asserts nothing is left behind.
    /// </summary>
    public sealed class ImportRulesTests
    {
        private const string TempName = "__ImportRulesTest__";
        private readonly List<string> _createdFolders = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdFolders.Count - 1; i >= 0; i--)
            {
                AssetDatabase.DeleteAsset(_createdFolders[i]);
            }

            foreach (string folder in _createdFolders)
            {
                Assert.IsFalse(Directory.Exists(folder), $"{folder} was not removed.");
                Assert.IsFalse(File.Exists(folder + ".meta"), $"{folder}.meta was not removed.");
            }

            _createdFolders.Clear();
        }

        [Test]
        public void SfxWav_GetsSfxSettings_AndStereo2DKeepsChannels()
        {
            string spatial = ImportFile(ImportRuleSet.SfxFolder + TempName + "/blip.wav", StereoWav());
            string flat = ImportFile(ImportRuleSet.SfxFolder + TempName + "/2D/blip.wav", StereoWav());

            var spatialImporter = (AudioImporter)AssetImporter.GetAtPath(spatial);
            var flatImporter = (AudioImporter)AssetImporter.GetAtPath(flat);

            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, spatialImporter.defaultSampleSettings.loadType);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, spatialImporter.defaultSampleSettings.compressionFormat);
            Assert.IsTrue(spatialImporter.defaultSampleSettings.preloadAudioData);
            Assert.IsTrue(spatialImporter.forceToMono);
            Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<AudioClip>(spatial).channels);
            Assert.IsFalse(flatImporter.forceToMono);
            Assert.AreEqual(2, AssetDatabase.LoadAssetAtPath<AudioClip>(flat).channels);
        }

        [Test]
        public void MusicWav_Streams()
        {
            string path = ImportFile(ImportRuleSet.MusicFolder + TempName + "/loop.wav", StereoWav());

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);

            Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType);
            Assert.IsFalse(importer.defaultSampleSettings.preloadAudioData);
            Assert.IsTrue(importer.loadInBackground);
        }

        [Test]
        public void GeneratedPng_IsPointUncompressedWithoutMips()
        {
            string color = ImportFile(ImportRuleSet.GeneratedFolder + TempName + "/Swatch.png", Png());
            string data = ImportFile(ImportRuleSet.GeneratedFolder + TempName + "/Swatch_Linear.png", Png());

            var colorImporter = (TextureImporter)AssetImporter.GetAtPath(color);
            var dataImporter = (TextureImporter)AssetImporter.GetAtPath(data);

            Assert.AreEqual(FilterMode.Point, colorImporter.filterMode);
            Assert.IsFalse(colorImporter.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, colorImporter.textureCompression);
            Assert.AreEqual(TextureImporterNPOTScale.None, colorImporter.npotScale);
            Assert.IsTrue(colorImporter.sRGBTexture);
            Assert.IsFalse(dataImporter.sRGBTexture);
            Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<Texture2D>(color).mipmapCount);
        }

        private string ImportFile(string path, byte[] bytes)
        {
            EnsureFolderTracked(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        private void EnsureFolderTracked(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolderTracked(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            _createdFolders.Add(folder);
        }

        private static byte[] StereoWav()
        {
            const int SampleRate = 22050;
            const int Frames = 2205;
            const short Channels = 2;
            const short BitsPerSample = 16;
            int dataSize = Frames * Channels * BitsPerSample / 8;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(Channels);
                writer.Write(SampleRate);
                writer.Write(SampleRate * Channels * BitsPerSample / 8);
                writer.Write((short)(Channels * BitsPerSample / 8));
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                for (int i = 0; i < Frames; i++)
                {
                    var sample = (short)(Mathf.Sin(i * 0.12f) * 8000f);
                    writer.Write(sample);
                    writer.Write((short)-sample);
                }

                return stream.ToArray();
            }
        }

        private static byte[] Png()
        {
            var texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            try
            {
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
