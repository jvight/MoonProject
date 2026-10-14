using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.scans" (JsonUtility DTO, docs/design/0.5.5-owner-feedback.md §1): the fragments fully
    /// scanned, the ones scanned part of the way (letting go keeps the progress), and the Metal known blueprints'
    /// fragments still owe because the game was saved during their dissolve, and the places already reached
    /// (version 2; version 1 had none).
    /// </summary>
    [Serializable]
    public sealed class ScansSaveData
    {
        public string[] scanned = Array.Empty<string>();

        /// <summary>Fragments scanned part of the way, matched by index with <see cref="partials"/>.</summary>
        public string[] partialIds = Array.Empty<string>();

        public float[] partials = Array.Empty<float>();

        public int owedMetal;

        /// <summary>The places 07 has come within while fragments lay there (their "N fragments to scan" told once).
        /// </summary>
        public string[] reached = Array.Empty<string>();
    }
}
