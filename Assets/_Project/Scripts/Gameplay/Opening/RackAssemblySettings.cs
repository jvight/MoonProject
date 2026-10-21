using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A part 07 assembles at a bay of the socket rack (docs/design/maker-understanding.md §2, opening v3 5.2): the
    /// adapted blueprint that allows it, what it costs and the socket it takes on 07. The bay's own kit piece is what
    /// fits (<see cref="RackBaySettings.FilledBy"/>).
    /// </summary>
    [Serializable]
    public sealed class RackAssemblySettings
    {
        [Tooltip("The rack bay it is assembled at (RackBaySettings id).")]
        [SerializeField] private string _bayId = string.Empty;

        [Tooltip("The adapted blueprint maker understanding unlocks for it ('adapted.<id>').")]
        [SerializeField] private string _adaptedId = string.Empty;

        [Tooltip("What it costs from 07's hold: small, from things the opening's sites give.")]
        [SerializeField] private Recipe _cost;

        [Tooltip("The socket on 07 it fits.")]
        [SerializeField] private RoverSocket _socket;

        [Tooltip("Seconds of a held E to assemble it.")]
        [Range(0.3f, 6f)] [SerializeField] private float _holdSeconds = 1.6f;

        public RackAssemblySettings()
        {
        }

        public RackAssemblySettings(string bayId, string adaptedId, Recipe cost, RoverSocket socket, float holdSeconds)
        {
            _bayId = bayId;
            _adaptedId = adaptedId;
            _cost = cost;
            _socket = socket;
            _holdSeconds = holdSeconds;
        }

        public string BayId => _bayId ?? string.Empty;
        public string AdaptedId => _adaptedId ?? string.Empty;
        public Recipe Cost => _cost;
        public RoverSocket Socket => _socket;
        public float HoldSeconds => _holdSeconds;

        /// <summary>Null when the assembly can work, else the first problem.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(_bayId) || string.IsNullOrEmpty(_adaptedId))
            {
                return "a rack assembly needs its bay and its adapted blueprint";
            }

            if (_cost.IsFree)
            {
                return $"the assembly at '{_bayId}' costs nothing";
            }

            return _holdSeconds > 0f ? null : $"the assembly at '{_bayId}' takes no time";
        }
    }
}
