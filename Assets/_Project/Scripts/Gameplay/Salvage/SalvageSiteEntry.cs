using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One salvage site in the catalog: its World anchor and Art's site prefab (docs/features/M3-13: a Skeleton, the
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" pieces each with a CutPoint, and the Heart where a relic rests).
    /// </summary>
    [Serializable]
    public sealed class SalvageSiteEntry
    {
        [Tooltip("World anchor the site stands on (Core WorldAnchorIds.SitePrefix + name, e.g. site.depot).")]
        [SerializeField] private string _anchorId = string.Empty;

        [Tooltip("Art's site prefab (Generated/Art/Sites/Site_<name>.prefab), root at the anchor, +Z its forward.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("The rover ability reaching it needs (the canyon lander stands past the Hover-Jump gate).")]
        [SerializeField] private AbilityGate _gate = AbilityGate.Open;

        [Tooltip("Its heart lies under its pieces: the relic there can be dug only once the last piece is cut " +
                 "(docs/features/M3-16 P10, the supply depot).")]
        [SerializeField] private bool _heartUnderPieces;

        [Tooltip("Each of its pieces yields this many times its size's units: a site of few, rich seams (Kenji's " +
                 "garage, first-hour v2 §4: one trip, the 2x economy kept).")]
        [Range(1, 4)]
        [SerializeField] private int _yieldScale = 1;

        [Tooltip("Each piece's seam (docs/design/feel-pass.md §3.2): the content builder writes one across its face; " +
                 "Art may author better ones.")]
        [SerializeField] private PieceCutLine[] _cutLines = Array.Empty<PieceCutLine>();

        [Tooltip("Each piece's size (docs/features/M3-26 §4.2): the content builder deals the site's classes to its " +
                 "pieces, biggest to biggest.")]
        [SerializeField] private PieceSizeEntry[] _sizes = Array.Empty<PieceSizeEntry>();

        public SalvageSiteEntry(string anchorId, GameObject prefab, AbilityGate gate, bool heartUnderPieces,
            int yieldScale, PieceCutLine[] cutLines, PieceSizeEntry[] sizes)
        {
            _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
            _anchorId = anchorId;
            _prefab = prefab;
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _heartUnderPieces = heartUnderPieces;
            _yieldScale = yieldScale;
            _cutLines = cutLines ?? throw new ArgumentNullException(nameof(cutLines));
        }

        /// <summary>The site's id everywhere (saves, events, relic definitions): its anchor id.</summary>
        public string AnchorId => _anchorId;

        public GameObject Prefab => _prefab;

        public AbilityGate Gate => _gate;

        public bool HeartUnderPieces => _heartUnderPieces;

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

            if (_prefab == null)
            {
                return $"'{_anchorId}' has no prefab (Generated/Art/Sites)";
            }

            return _gate == null ? $"'{_anchorId}' has no ability gate" : null;
        }
    }
}
