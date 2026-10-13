using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.crates" (JsonUtility DTO; field names are the JSON keys): the crew supply crates already
    /// cut open (docs/features/M3-25 T4), by site id, and the units each still holds in its mouth for want of room in
    /// 07's hold (0.5.5 §2; the same order, empty in older saves).
    /// </summary>
    [Serializable]
    public sealed class CratesSaveData
    {
        public string[] opened = Array.Empty<string>();

        public int[] held = Array.Empty<int>();
    }
}
