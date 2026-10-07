using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where a friend placed at the World's anchors lies, where its parts wait and how it walks home (see
    /// <see cref="FriendAnchorPlanner"/>).
    /// </summary>
    [Serializable]
    public sealed class FriendAnchorPlacement
    {
        [Tooltip("Where it lies broken: an anchor and an offset in its frame (x right, y forward); it faces back.")]
        [SerializeField] private AnchorSpot _site;

        [Tooltip("Prefix of the numbered anchors its parts wait at, one part each (e.g. canyon.alcove_).")]
        [SerializeField] private string _partAnchorPrefix = string.Empty;

        [Tooltip("The anchor where the driving line in starts (e.g. canyon.landing); leftover parts go along it.")]
        [SerializeField] private string _lineStart = string.Empty;

        [Tooltip("Leftover parts lie along the driving line at least this many metres apart (and from the site).")]
        [Range(5f, 200f)] [SerializeField] private float _lineSpacing = 40f;

        [Tooltip("The anchor at the top of the way back out that needs no ability (e.g. canyon.exit).")]
        [SerializeField] private string _exit = string.Empty;

        public FriendAnchorPlacement(AnchorSpot site, string partAnchorPrefix, string lineStart, float lineSpacing,
            string exit)
        {
            _site = site ?? throw new ArgumentNullException(nameof(site));
            _partAnchorPrefix = partAnchorPrefix;
            _lineStart = lineStart;
            _lineSpacing = lineSpacing;
            _exit = exit;
        }

        public AnchorSpot Site => _site;
        public string PartAnchorPrefix => _partAnchorPrefix;
        public string LineStart => _lineStart;
        public float LineSpacing => _lineSpacing;
        public string Exit => _exit;

        /// <summary>Null when the placement is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            return _site == null || string.IsNullOrWhiteSpace(_site.AnchorId) ? "names no site anchor"
                : string.IsNullOrWhiteSpace(_partAnchorPrefix) ? "names no part anchors"
                : string.IsNullOrWhiteSpace(_lineStart) ? "names no driving line start"
                : string.IsNullOrWhiteSpace(_exit) ? "names no way out"
                : null;
        }
    }
}
