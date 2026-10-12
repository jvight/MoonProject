using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>The size class of one piece of a salvage site (by its number), written by the content builder.</summary>
    [Serializable]
    public struct PieceSizeEntry
    {
        [Tooltip("The piece's number (Salvage_<n>_<Material>).")]
        [SerializeField] private int _number;

        [Tooltip("How big it is: its segments, cut time and yield.")]
        [SerializeField] private PieceSize _size;

        public PieceSizeEntry(int number, PieceSize size)
        {
            _number = number;
            _size = size;
        }

        public int Number => _number;

        public PieceSize Size => _size;
    }
}
