using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.wallet" at version 2 (read only by <see cref="MaterialsSaveMigrations"/>): one shared
    /// total per salvage material.
    /// </summary>
    [Serializable]
    public sealed class MaterialsSaveDataV2
    {
        public int metal;

        public int wiring;

        public int optics;
    }
}
