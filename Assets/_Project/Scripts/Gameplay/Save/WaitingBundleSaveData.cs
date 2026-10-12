using System;

namespace MoonProject.Gameplay
{
    /// <summary>One bundle waiting in the world (JsonUtility DTO, part of <see cref="MaterialsSaveData"/>).</summary>
    [Serializable]
    public sealed class WaitingBundleSaveData
    {
        /// <summary>The material's saved value.</summary>
        public int material;

        public int units;

        public float x;

        public float y;

        public float z;
    }
}
