using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A part-cut or loose piece in the "gameplay.salvage" section (JsonUtility DTO).</summary>
    [Serializable]
    public sealed class SalvagePieceSaveData
    {
        /// <summary>The piece's number (the &lt;n&gt; of its node name).</summary>
        public int number;

        /// <summary>How far the cut has come, 0..1.</summary>
        public float progress;

        /// <summary>A drag piece pulled clear of its wreck, lying at <see cref="position"/>.</summary>
        public bool loose;

        public Vector3 position;

        public Quaternion rotation = Quaternion.identity;
    }
}
