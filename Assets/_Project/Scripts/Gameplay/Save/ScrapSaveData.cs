using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.scrap" (JsonUtility DTO): which pieces of the seeded scrap field were collected.
    /// <see cref="layoutSignature"/> identifies the field the indices refer to; if the field was re-tuned since, the
    /// indices mean nothing and the field starts full.
    /// </summary>
    [Serializable]
    public sealed class ScrapSaveData
    {
        public int layoutSignature;
        public int[] collected = Array.Empty<int>();
    }
}
