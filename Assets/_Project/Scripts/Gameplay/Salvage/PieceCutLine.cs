using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the beam's seam runs on one salvage piece (docs/design/feel-pass.md §3.2): an ordered polyline of two to
    /// five points on the piece's face, in its CutPoint's local space. The hot point travels along it with the cut's
    /// progress.
    /// </summary>
    [Serializable]
    public struct PieceCutLine
    {
        public const int MinPoints = 2;
        public const int MaxPoints = 5;

        [Tooltip("The piece's number (Salvage_<n>_<Material>).")]
        [SerializeField] private int _number;

        [Tooltip("The seam's points in order, in the piece's CutPoint space (x along the face, z out of it).")]
        [SerializeField] private Vector3[] _points;

        public PieceCutLine(int number, Vector3[] points)
        {
            _number = number;
            _points = points;
        }

        public int Number => _number;

        public Vector3[] Points => _points;

        /// <summary>Two to five points.</summary>
        public bool IsValid => _points != null && _points.Length >= MinPoints && _points.Length <= MaxPoints;
    }
}
