using UnityEngine;

namespace MoonProject.Editor.Import
{
    /// <summary>Import settings <see cref="ImportRules"/> forces onto an audio clip.</summary>
    public readonly struct AudioImportRule
    {
        public AudioImportRule(AudioClipLoadType loadType, float quality, bool preload, bool forceMono,
            bool loadInBackground)
        {
            LoadType = loadType;
            Quality = quality;
            Preload = preload;
            ForceMono = forceMono;
            LoadInBackground = loadInBackground;
        }

        public AudioClipLoadType LoadType { get; }

        /// <summary>Vorbis quality 0..1 (every rule compresses with Vorbis).</summary>
        public float Quality { get; }

        public bool Preload { get; }

        public bool ForceMono { get; }

        public bool LoadInBackground { get; }
    }
}
