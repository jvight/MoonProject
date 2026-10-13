using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's equipment in the save (section "gameplay.equipment", docs/design/0.5.5-owner-feedback.md §3.5): the pieces
    /// fitted (each in its own socket) and the parts row in order, as <see cref="Core.Events.RoverKitPiece"/> values.
    /// </summary>
    [Serializable]
    public sealed class EquipmentSaveData
    {
        public int[] fitted = Array.Empty<int>();

        public int[] row = Array.Empty<int>();
    }
}
