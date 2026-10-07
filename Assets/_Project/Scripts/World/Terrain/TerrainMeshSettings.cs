using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How the analytic surface is cut into flat-shaded chunk meshes: chunk grid, the three cell-size tiers, the
    /// handmade vertex jitter and the collider-free backdrop ring that carries the distant silhouettes.
    /// </summary>
    [Serializable]
    public sealed class TerrainMeshSettings
    {
        [Header("Chunks")]
        [Tooltip("Edge length of one square terrain chunk (one mesh + one collider), metres.")]
        [Range(16f, 256f)]
        [SerializeField] private float _chunkSize = 64f;

        [Tooltip("Chunks cover the square [-extent, extent] on X and Z. Must be a multiple of the chunk size.")]
        [Range(64f, 2048f)]
        [SerializeField] private float _gridHalfExtent = 512f;

        [Header("Cell tiers")]
        [Tooltip("Cell size on the drivable floor, metres: small facets for smooth, readable driving.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _fineCellSize = 2f;

        [Tooltip("Cell size on the foothills and inner rim wall, metres.")]
        [Range(1f, 16f)]
        [SerializeField] private float _midCellSize = 4f;

        [Tooltip("Cell size on the crest and just beyond, metres: big chunky mountain facets.")]
        [Range(2f, 32f)]
        [SerializeField] private float _coarseCellSize = 8f;

        [Tooltip("Cell size far behind the crest, metres: never seen up close from inside the crater.")]
        [Range(4f, 64f)]
        [SerializeField] private float _farCellSize = 16f;

        [Tooltip("Chunks whose nearest point lies within this radius use fine cells. Cover everything drivable.")]
        [Range(50f, 2000f)]
        [SerializeField] private float _fineRadius = 345f;

        [Tooltip("Chunks whose nearest point lies within this radius (and beyond the fine radius) use mid cells.")]
        [Range(50f, 2000f)]
        [SerializeField] private float _midRadius = 420f;

        [Tooltip("Chunks whose nearest point lies within this radius (and beyond the mid radius) use coarse cells; " +
            "farther ones use far cells.")]
        [Range(50f, 2000f)]
        [SerializeField] private float _coarseRadius = 470f;

        [Tooltip("Seeded sideways wobble of every vertex, as a fraction of its cell. Gives the handmade look.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _jitter = 0.25f;

        [Header("Backdrop ring")]
        [Tooltip("Inner radius of the collider-free backdrop ring. Keep it inside the chunk grid so there is no gap.")]
        [Range(100f, 4000f)]
        [SerializeField] private float _backdropInnerRadius = 500f;

        [Tooltip("Outer radius of the backdrop ring: the visible end of the world (hidden by fog).")]
        [Range(500f, 10000f)]
        [SerializeField] private float _backdropOuterRadius = 2800f;

        [Tooltip("Number of rings between the inner and outer radius (spaced wider with distance).")]
        [Range(4, 64)]
        [SerializeField] private int _backdropRings = 26;

        [Tooltip("Number of segments around the ring.")]
        [Range(16, 512)]
        [SerializeField] private int _backdropSegments = 192;

        [Tooltip("How far the backdrop is sunk below the surface where it overlaps the chunks, metres.")]
        [Range(0f, 20f)]
        [SerializeField] private float _backdropSink = 3f;

        public float ChunkSize => _chunkSize;
        public float GridHalfExtent => _gridHalfExtent;
        public float FineCellSize => _fineCellSize;
        public float MidCellSize => _midCellSize;
        public float CoarseCellSize => _coarseCellSize;
        public float FarCellSize => _farCellSize;
        public float FineRadius => _fineRadius;
        public float MidRadius => _midRadius;
        public float CoarseRadius => _coarseRadius;
        public float Jitter => _jitter;
        public float BackdropInnerRadius => _backdropInnerRadius;
        public float BackdropOuterRadius => _backdropOuterRadius;
        public int BackdropRings => _backdropRings;
        public int BackdropSegments => _backdropSegments;
        public float BackdropSink => _backdropSink;

        /// <summary>Returns null when the settings are consistent, otherwise a description of the problem.</summary>
        public string Validate()
        {
            if (!IsMultiple(_gridHalfExtent, _chunkSize))
            {
                return "Grid half extent must be a whole number of chunks.";
            }

            if (!IsMultiple(_chunkSize, _farCellSize) || !IsMultiple(_farCellSize, _coarseCellSize)
                || !IsMultiple(_coarseCellSize, _midCellSize) || !IsMultiple(_midCellSize, _fineCellSize))
            {
                return "Cell sizes must nest: each tier divides the next, and the far tier divides the chunk size.";
            }

            if (_chunkSize / _fineCellSize > 90f)
            {
                return "Too many fine cells per chunk: one chunk mesh must stay under 65k vertices.";
            }

            if (_fineRadius > _midRadius || _midRadius > _coarseRadius)
            {
                return "Tier radii must grow outward: fine, then mid, then coarse.";
            }

            if (_backdropInnerRadius > _gridHalfExtent || _backdropInnerRadius >= _backdropOuterRadius)
            {
                return "The backdrop must start inside the chunk grid and end beyond its start.";
            }

            return null;
        }

        private static bool IsMultiple(float value, float unit)
        {
            float ratio = value / unit;
            return unit > 0f && Mathf.Abs(ratio - Mathf.Round(ratio)) < 1e-4f && ratio >= 1f;
        }
    }
}
