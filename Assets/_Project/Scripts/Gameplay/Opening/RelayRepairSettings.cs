using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// relay.1's repair (docs/design/opening-v3-purpose.md R10): the relay, its labelled spare in the open service
    /// cabinet, how near 07 must be to take it and to fit it into the mast box's feed socket, and how near the relit
    /// relay's contact pad charges 07.
    /// </summary>
    [Serializable]
    public sealed class RelayRepairSettings
    {
        [Tooltip("The relay's id: its world anchor (relay.1) and its RelayRelit event.")]
        [SerializeField] private string _relayId = "relay.1";

        [Tooltip("The spare's story-part id in 07's hold (RELAY SPARE · FEED UNIT): taken from the cabinet, fitted " +
                 "at the feed socket.")]
        [SerializeField] private string _feedPart = "part.feedunit";

        [Tooltip("Metres (flat) from the spare in the open cabinet within which a held E takes it.")]
        [Range(1f, 8f)]
        [SerializeField] private float _takeReach = 3.5f;

        [Tooltip("Metres (flat) from the mast box's feed socket within which a held E fits the spare from the hold.")]
        [Range(1f, 8f)]
        [SerializeField] private float _fitReach = 3.5f;

        [Tooltip("Metres (flat) from the contact pad's centre within which a still 07 charges on the relit relay.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _padRadius = 2f;

        public string RelayId => _relayId ?? string.Empty;
        public string FeedPart => _feedPart ?? string.Empty;
        public float TakeReach => _takeReach;
        public float FitReach => _fitReach;
        public float PadRadius => _padRadius;
    }
}
