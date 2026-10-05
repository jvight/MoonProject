using System;
using System.IO;
using UnityEngine;

namespace MoonProject.Editor.Import
{
    /// <summary>
    /// Which import settings apply to which project path (pure, EditMode-tested). Conventions:
    /// <list type="bullet">
    /// <item>Audio/SFX: decompress on load, preloaded, Vorbis 0.8; forced mono (3D one-shots) unless the path
    /// contains <c>/2D/</c> (UI and other non-spatial sounds keep their channels).</item>
    /// <item>Audio/Music: streamed from disk in the background, Vorbis 0.7, not preloaded.</item>
    /// <item>Audio/Ambience: compressed in memory, preloaded, Vorbis 0.6 (long loops).</item>
    /// <item>Generated/** textures: point filter, no mipmaps, uncompressed, sRGB unless the file name ends in
    /// <c>_Linear</c> (data textures such as noise or masks).</item>
    /// </list>
    /// </summary>
    public static class ImportRuleSet
    {
        public const string SfxFolder = "Assets/_Project/Audio/SFX/";
        public const string MusicFolder = "Assets/_Project/Audio/Music/";
        public const string AmbienceFolder = "Assets/_Project/Audio/Ambience/";
        public const string GeneratedFolder = "Assets/_Project/Generated/";
        public const string NonSpatialMarker = "/2D/";
        public const string LinearSuffix = "_Linear";

        private const float SfxQuality = 0.8f;
        private const float MusicQuality = 0.7f;
        private const float AmbienceQuality = 0.6f;

        public static bool TryGetAudioRule(string assetPath, out AudioImportRule rule)
        {
            string path = Normalize(assetPath);
            if (path.StartsWith(SfxFolder, StringComparison.Ordinal))
            {
                bool spatial = path.IndexOf(NonSpatialMarker, SfxFolder.Length - 1, StringComparison.Ordinal) < 0;
                rule = new AudioImportRule(AudioClipLoadType.DecompressOnLoad, SfxQuality, true, spatial, false);
                return true;
            }

            if (path.StartsWith(MusicFolder, StringComparison.Ordinal))
            {
                rule = new AudioImportRule(AudioClipLoadType.Streaming, MusicQuality, false, false, true);
                return true;
            }

            if (path.StartsWith(AmbienceFolder, StringComparison.Ordinal))
            {
                rule = new AudioImportRule(AudioClipLoadType.CompressedInMemory, AmbienceQuality, true, false, false);
                return true;
            }

            rule = default;
            return false;
        }

        public static bool TryGetTextureRule(string assetPath, out TextureImportRule rule)
        {
            string path = Normalize(assetPath);
            if (!path.StartsWith(GeneratedFolder, StringComparison.Ordinal))
            {
                rule = default;
                return false;
            }

            string name = Path.GetFileNameWithoutExtension(path);
            rule = new TextureImportRule(!name.EndsWith(LinearSuffix, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        private static string Normalize(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? string.Empty : assetPath.Replace('\\', '/');
        }
    }
}
