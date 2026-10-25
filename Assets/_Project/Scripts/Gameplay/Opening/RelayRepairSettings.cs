using System;
using UnityEngine;
using MoonProject.Core.Events;

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

        [Tooltip("Seconds from the unit seating home (Latch) to the relay's lens flickering on (Wake).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _wakeAfter = 0.8f;

        [Tooltip("Seconds from Wake to the dish starting to turn to its bearing (Align).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _alignAfter = 0.8f;

        [Tooltip("Seconds from Align to the reply blink (Reply): long enough for the dish to settle.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _replyAfter = 1.8f;

        [Tooltip("Seconds from Reply to the contact pad going live (Live, the relay relit): the blink's length.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _liveAfter = 1.6f;

        [Tooltip("Share of a full battery 07 should still hold on arriving at the relay, by the range ring's own " +
                 "costing of the drive there. With less, the line first sends 07 to charge at the pad (full cells " +
                 "always do).")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _tripMargin01 = 0.2f;

        public string RelayId => _relayId ?? string.Empty;
        public string FeedPart => _feedPart ?? string.Empty;
        public float TakeReach => _takeReach;
        public float FitReach => _fitReach;
        public float PadRadius => _padRadius;
        public float TripMargin01 => _tripMargin01;

        /// <summary>Seconds after the latch at which <paramref name="stage"/> comes.</summary>
        public float StageAt(RelayStage stage)
        {
            float at = 0f;
            at += stage >= RelayStage.Wake ? _wakeAfter : 0f;
            at += stage >= RelayStage.Align ? _alignAfter : 0f;
            at += stage >= RelayStage.Reply ? _replyAfter : 0f;
            at += stage >= RelayStage.Live ? _liveAfter : 0f;
            return at;
        }
    }
}
