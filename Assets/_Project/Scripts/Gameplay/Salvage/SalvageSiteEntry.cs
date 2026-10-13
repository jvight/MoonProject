using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One salvage site in the catalog: its World anchor and Art's site prefab (docs/features/M3-13: a Skeleton, the
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" pieces each with a CutPoint, and the Heart where a relic rests), or
    /// one of the derelicts left on the routes (0.5.5 §1.7: a Skeleton and any pieces, no heart, no crew crate).
    /// </summary>
    [Serializable]
    public sealed class SalvageSiteEntry
    {
        [Tooltip("World anchor the site stands on (Core WorldAnchorIds.SitePrefix + name, e.g. site.depot; a " +
                 "derelict's WorldAnchorIds.MicroPrefix + name, e.g. micro.06).")]
        [SerializeField] private string _anchorId = string.Empty;

        [Tooltip("Art's site prefab (Generated/Art/Sites/Site_<name>.prefab, a derelict's " +
                 "Generated/Art/Props/Derelict_<name>.prefab), root at the anchor, +Z its forward.")]
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

        [Tooltip("The crew supply crate beside its heart (docs/features/M3-25 T4): whose hand and what stack. " +
                 "A derelict has none (left empty).")]
        [SerializeField] private SupplyCrate _crate;

        public SalvageSiteEntry(string anchorId, GameObject prefab, AbilityGate gate, bool heartUnderPieces,
            int yieldScale, PieceCutLine[] cutLines, PieceSizeEntry[] sizes, SupplyCrate crate)
        {
            _crate = crate;
            _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
            _anchorId = anchorId;
            _prefab = prefab;
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _heartUnderPieces = heartUnderPieces;
            _yieldScale = yieldScale;
            _cutLines = cutLines ?? throw new ArgumentNullException(nameof(cutLines));
        }

        /// <summary>
        /// A derelict on a micro-site anchor (0.5.5 §1.7): open to 07, the plain yield, no heart and no crew crate.
        /// </summary>
        public static SalvageSiteEntry Derelict(string anchorId, GameObject prefab, PieceCutLine[] cutLines,
            PieceSizeEntry[] sizes)
        {
            return new SalvageSiteEntry(anchorId, prefab, AbilityGate.Open, false, 1, cutLines, sizes, default);
        }

        /// <summary>The site's id everywhere (saves, events, relic definitions): its anchor id.</summary>
        public string AnchorId => _anchorId;

        public GameObject Prefab => _prefab;

        public AbilityGate Gate => _gate;

        public bool HeartUnderPieces => _heartUnderPieces;

        public int YieldScale => _yieldScale;

        public SupplyCrate Crate => _crate;

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
        /// <param name="anchorPrefix">The anchors its list stands on: <see cref="WorldAnchorIds.SitePrefix"/> for a
        /// wreck, <see cref="WorldAnchorIds.MicroPrefix"/> for a derelict.</param>
        public string Validate(string anchorPrefix)
        {
            if (string.IsNullOrWhiteSpace(_anchorId) ||
                !_anchorId.StartsWith(anchorPrefix, StringComparison.Ordinal))
            {
                return $"has no '{anchorPrefix}' anchor id ('{_anchorId}')";
            }

            if (_prefab == null)
            {
                return $"'{_anchorId}' has no prefab (Generated/Art)";
            }

            return _gate == null ? $"'{_anchorId}' has no ability gate" : null;
        }
    }
}
