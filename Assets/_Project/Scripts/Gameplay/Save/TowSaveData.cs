using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.tow" (JsonUtility DTO): where each towable load was left, by its tow id. A tow in
    /// progress is not kept: a loaded game finds the load where it stood, off the cable.
    /// </summary>
    [Serializable]
    public sealed class TowSaveData
    {
        public string[] ids = Array.Empty<string>();

        public Vector3[] positions = Array.Empty<Vector3>();

        public Quaternion[] rotations = Array.Empty<Quaternion>();
    }
}
