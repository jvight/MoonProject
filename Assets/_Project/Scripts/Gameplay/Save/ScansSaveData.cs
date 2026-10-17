using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.scans" (JsonUtility DTO, docs/design/0.5.5-owner-feedback.md §1): the targets fully
    /// scanned, the ones scanned part of the way (letting go keeps the progress), and the Metal known structure sets
    /// still owe (it waits for room in the hold).
    /// </summary>
    [Serializable]
    public sealed class ScansSaveData
    {
        public string[] scanned = Array.Empty<string>();

        /// <summary>Targets scanned part of the way, matched by index with <see cref="partials"/>.</summary>
        public string[] partialIds = Array.Empty<string>();

        public float[] partials = Array.Empty<float>();

        public int owedMetal;
    }
}
