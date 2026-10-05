using UnityEditor;
using UnityEngine;

namespace MoonProject.Editor.Import
{
    /// <summary>
    /// Enforces <see cref="ImportRuleSet"/> on every (re)import, so import settings live in code, never in hand-edited
    /// .meta files. Settings changed in the inspector for a covered path are reset on the next import. Bump
    /// <see cref="Version"/> whenever a rule changes so Unity reimports the affected assets.
    /// </summary>
    public sealed class ImportRules : AssetPostprocessor
    {
        public const uint Version = 1;

        public override uint GetVersion()
        {
            return Version;
        }

        private void OnPreprocessAudio()
        {
            if (!ImportRuleSet.TryGetAudioRule(assetPath, out AudioImportRule rule))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = rule.LoadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = rule.Quality;
            settings.preloadAudioData = rule.Preload;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = rule.ForceMono;
            importer.loadInBackground = rule.LoadInBackground;
            importer.ambisonic = false;
        }

        private void OnPreprocessTexture()
        {
            if (!ImportRuleSet.TryGetTextureRule(assetPath, out TextureImportRule rule))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = rule.Srgb;
        }
    }
}
