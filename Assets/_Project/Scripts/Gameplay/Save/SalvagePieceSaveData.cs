using System;

namespace MoonProject.Gameplay
{
    /// <summary>A part-cut or held piece in the "gameplay.salvage" section (JsonUtility DTO).</summary>
    [Serializable]
    public sealed class SalvagePieceSaveData
    {
        /// <summary>The piece's number (the &lt;n&gt; of its node name).</summary>
        public int number;

        /// <summary>How far the cut has come, 0..1.</summary>
        public float progress;

        /// <summary>Cut through with no room in the hold: it waits on its wreck to be taken (0.5.5 §2).</summary>
        public bool held;
    }
}
