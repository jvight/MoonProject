using UnityEngine;

namespace MoonProject.Editor.Import
{
    /// <summary>Import settings <see cref="ImportRules"/> forces onto an audio clip.</summary>
    public readonly struct AudioImportRule
    {
        public AudioImportRule(AudioClipLoadType loadType, AudioCompressionFormat compressionFormat, float quality,
            bool preload, bool forceMono, bool loadInBackground)
        {
            LoadType = loadType;
            CompressionFormat = compressionFormat;
            Quality = quality;
            Preload = preload;
            ForceMono = forceMono;
            LoadInBackground = loadInBackground;
        }

        public AudioClipLoadType LoadType { get; }

        public AudioCompressionFormat CompressionFormat { get; }

        /// <summary>Vorbis quality 0..1 (ignored for PCM).</summary>
        public float Quality { get; }

        public bool Preload { get; }

        public bool ForceMono { get; }

        public bool LoadInBackground { get; }
    }
}
