using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One bay of the socket rack at the pad (docs/design/opening-v3-plan.md 4.3): the part it stands for, when its
    /// outline is seen (at the release, or once a fragment of the part's blueprint is scanned) and the kit piece whose
    /// fitting fills it.
    /// </summary>
    [Serializable]
    public sealed class RackBaySettings
    {
        [Tooltip("The bay's id, saved: never rename one that has shipped.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("The outline shows as soon as the rack is released (the parts 07 has already seen: the SSR-3 " +
                 "headlamp, the cell pack).")]
        [SerializeField] private bool _seenAtRelease;

        [Tooltip("Otherwise the outline shows once a fragment of this blueprint is scanned (the hex adapter).")]
        [SerializeField] private string _seenByBlueprint = string.Empty;

        [Tooltip("The kit piece whose fitting fills the bay.")]
        [SerializeField] private RoverKitPiece _filledBy;

        public RackBaySettings()
        {
        }

        public RackBaySettings(string id, bool seenAtRelease, string seenByBlueprint, RoverKitPiece filledBy)
        {
            _id = id;
            _seenAtRelease = seenAtRelease;
            _seenByBlueprint = seenByBlueprint ?? string.Empty;
            _filledBy = filledBy;
        }

        public string Id => _id;

        public bool SeenAtRelease => _seenAtRelease;

        public string SeenByBlueprint => _seenByBlueprint;

        public RoverKitPiece FilledBy => _filledBy;

        /// <summary>Null when the bay can work, else the first problem.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                return "a rack bay has no id";
            }

            return _seenAtRelease || !string.IsNullOrEmpty(_seenByBlueprint)
                ? null
                : $"rack bay '{_id}' is never seen (neither at the release nor by a blueprint)";
        }
    }
}
