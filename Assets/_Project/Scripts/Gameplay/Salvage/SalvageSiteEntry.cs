using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One salvage site in the catalog: its World anchor and Art's site prefab (docs/features/M3-13: a Skeleton, the
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" pieces each with a CutPoint, and the Heart where a relic rests), or
    /// one of the derelicts left on the routes (0.5.5 §1.7: a Skeleton and any pieces, no heart, no crew crate), on a
    /// micro-site anchor or inside a wreck on one of its nodes (the haul bot in the drill site), with Art's props set
    /// on its sockets (relay.2's hut against 06).
    /// </summary>
    [Serializable]
    public sealed class SalvageSiteEntry
    {
        [Tooltip("Stable id (saves, events, relic definitions): a site's or an anchored derelict's is its anchor id, " +
                 "a derelict inside a wreck has its own (derelict.haul_bot). Never rename.")]
        [SerializeField] private string _id = string.Empty;

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

        [Tooltip("A derelict inside a wreck: the wreck's node it stands on, with identity (the haul bot on " +
                 "Site_drill's HaulBotSocket). Empty: it stands on the anchor itself.")]
        [SerializeField] private string _hostSocket = string.Empty;

        [Tooltip("Art's props set on its sockets with identity (relay.2's hut on 06's HutSocket).")]
        [SerializeField] private SocketProp[] _props = Array.Empty<SocketProp>();

        public SalvageSiteEntry(string anchorId, GameObject prefab, AbilityGate gate, bool heartUnderPieces,
            int yieldScale, PieceCutLine[] cutLines, PieceSizeEntry[] sizes, SupplyCrate crate)
        {
            _id = anchorId;
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
        /// A derelict (0.5.5 §1.7): open to 07, the plain yield, no heart and no crew crate.
        /// </summary>
        /// <param name="id">Its stable id: its micro-site anchor's, or its own inside a wreck.</param>
        /// <param name="anchorId">The micro-site anchor it stands on, or the wreck's it stands inside.</param>
        /// <param name="hostSocket">Inside a wreck: the wreck's node it stands on; else empty.</param>
        public static SalvageSiteEntry Derelict(string id, string anchorId, string hostSocket, GameObject prefab,
            PieceCutLine[] cutLines, PieceSizeEntry[] sizes, SocketProp[] props)
        {
            return new SalvageSiteEntry(anchorId, prefab, AbilityGate.Open, false, 1, cutLines, sizes, default)
            {
                _id = id,
                _hostSocket = hostSocket ?? string.Empty,
                _props = props ?? throw new ArgumentNullException(nameof(props)),
            };
        }

        /// <summary>Its id everywhere (saves, events, relic definitions).</summary>
        public string Id => _id;

        /// <summary>The World anchor it stands on (or the wreck it stands inside does).</summary>
        public string AnchorId => _anchorId;

        /// <summary>The wreck's node a derelict stands on inside it; empty when it stands on its anchor.</summary>
        public string HostSocket => _hostSocket ?? string.Empty;

        public IReadOnlyList<SocketProp> Props => _props;

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
        /// <param name="derelict">It is one of the derelicts: on a <see cref="WorldAnchorIds.MicroPrefix"/> anchor,
        /// or inside a wreck on a <see cref="WorldAnchorIds.SitePrefix"/> anchor. A wreck stands on a site anchor.
        /// </param>
        public string Validate(bool derelict)
        {
            string anchorPrefix = derelict && string.IsNullOrEmpty(HostSocket)
                ? WorldAnchorIds.MicroPrefix
                : WorldAnchorIds.SitePrefix;
            if (string.IsNullOrWhiteSpace(_anchorId) ||
                !_anchorId.StartsWith(anchorPrefix, StringComparison.Ordinal))
            {
                return $"has no '{anchorPrefix}' anchor id ('{_anchorId}')";
            }

            if (string.IsNullOrWhiteSpace(_id))
            {
                return $"'{_anchorId}' has no id";
            }

            if (!derelict && (!string.IsNullOrEmpty(HostSocket) || _id != _anchorId))
            {
                return $"'{_anchorId}' is a wreck: it stands on its own anchor, under its anchor's id";
            }

            for (int i = 0; i < _props.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_props[i].Socket) || _props[i].Prefab == null)
                {
                    return $"'{_id}' has prop {i} without its socket or prefab";
                }
            }

            if (_prefab == null)
            {
                return $"'{_anchorId}' has no prefab (Generated/Art)";
            }

            return _gate == null ? $"'{_anchorId}' has no ability gate" : null;
        }
    }
}
