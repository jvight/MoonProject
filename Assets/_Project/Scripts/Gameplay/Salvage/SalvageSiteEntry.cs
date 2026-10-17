using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One salvage site in the catalog: its World anchor and Art's site prefab (docs/features/M3-13: a Skeleton and
    /// the "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" pieces each with a CutPoint), with each piece's seam and size.
    /// </summary>
    [Serializable]
    public sealed class SalvageSiteEntry
    {
        [Tooltip("Stable id (saves, events): its anchor id. Never rename.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("World anchor the site stands on (Core WorldAnchorIds.SitePrefix + name, e.g. site.kestrel).")]
        [SerializeField] private string _anchorId = string.Empty;

        [Tooltip("Art's site prefab (Generated/Art/Sites/Site_<name>.prefab), root at the anchor, +Z its forward.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Each of its pieces yields this many times its size's units.")]
        [Range(1, 4)]
        [SerializeField] private int _yieldScale = 1;

        [Tooltip("Each piece's seam (docs/design/feel-pass.md §3.2): the content builder writes one across its face; " +
                 "Art may author better ones.")]
        [SerializeField] private PieceCutLine[] _cutLines = Array.Empty<PieceCutLine>();

        [Tooltip("Each piece's size (docs/features/M3-26 §4.2): the content builder deals the site's classes to its " +
                 "pieces, biggest to biggest.")]
        [SerializeField] private PieceSizeEntry[] _sizes = Array.Empty<PieceSizeEntry>();

        public SalvageSiteEntry(string anchorId, GameObject prefab, int yieldScale, PieceCutLine[] cutLines,
            PieceSizeEntry[] sizes)
        {
            _id = anchorId;
            _anchorId = anchorId;
            _prefab = prefab;
            _yieldScale = yieldScale;
            _cutLines = cutLines ?? throw new ArgumentNullException(nameof(cutLines));
            _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
        }

        /// <summary>Its id everywhere (saves, events).</summary>
        public string Id => _id;

        /// <summary>The World anchor it stands on.</summary>
        public string AnchorId => _anchorId;

        public GameObject Prefab => _prefab;

        public int YieldScale => _yieldScale;

        /// <summary>The seam of piece <paramref name="number"/>, if the entry has a valid one.</summary>
        public bool TryGetCutLine(int number, out Vector3[] points)
        {
            for (int i = 0; i < _cutLines.Length; i++)
            {
                if (_cutLines[i].Number == number && _cutLines[i].IsValid)
                {
                    points = _cutLines[i].Points;
                    return true;
                }
            }

            points = null;
            return false;
        }

        /// <summary>The size of piece <paramref name="number"/>, if the entry has one.</summary>
        public bool TryGetSize(int number, out PieceSize size)
        {
            for (int i = 0; i < _sizes.Length; i++)
            {
                if (_sizes[i].Number == number)
                {
                    size = _sizes[i].Size;
                    return true;
                }
            }

            size = PieceSize.Small;
            return false;
        }

        /// <summary>Null when the entry is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_anchorId) ||
                !_anchorId.StartsWith(WorldAnchorIds.SitePrefix, StringComparison.Ordinal))
            {
                return $"has no '{WorldAnchorIds.SitePrefix}' anchor id ('{_anchorId}')";
            }

            if (_id != _anchorId)
            {
                return $"'{_anchorId}' stands under its anchor's id";
            }

            return _prefab == null ? $"'{_anchorId}' has no prefab (Generated/Art)" : null;
        }
    }
}
