using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where the relay masts of the station-reach network stand (M3-06, docs/features/M3-06-relay-network.md).
    /// relay.0, the teaching mast, stands on a low mound shaped for it in the spawn first frame right of The Peak
    /// (<see cref="SiteSettings"/>). relay.1 is found on the ground itself: the highest gently-topped rise in a
    /// sector, so it reads as a high point from the base. The third stands at the canyon mouth and the fourth on the
    /// canyon's relay ledge (<see cref="CanyonSettings"/>), past the Hover-Jump gate.
    /// </summary>
    [Serializable]
    public sealed class RelaySettings
    {
        [Tooltip("Radius of every mast's flat, drivable, rock-free pad, metres.")]
        [Range(2f, 6f)]
        [SerializeField] private float _padRadius = 3f;

        [Header("relay.1: a crater-rim shoulder opposite the canyon")]
        [Tooltip("Bearing from the base the search for relay.1 is centred on, degrees.")]
        [Range(0f, 360f)]
        [SerializeField] private float _shoulderBearing = 250f;

        [Tooltip("Half-width of relay.1's search sector, degrees.")]
        [Range(1f, 45f)]
        [SerializeField] private float _shoulderBearingSpread = 15f;

        [Tooltip("Distance from the base the search for relay.1 is centred on, metres.")]
        [Range(50f, 290f)]
        [SerializeField] private float _shoulderDistance = 220f;

        [Tooltip("Half-depth of relay.1's search sector, metres (it stays on the drivable floor).")]
        [Range(5f, 80f)]
        [SerializeField] private float _shoulderDistanceSpread = 30f;

        [Header("relay.2: the canyon mouth")]
        [Tooltip("Arc length along the canyon of relay.2, metres (0 = the mouth; the take-off lip is further in).")]
        [Range(-20f, 10f)]
        [SerializeField] private float _mouthArc = -2f;

        [Tooltip("Offset of relay.2 across the mouth, metres (positive = right of the way in), at the foot of the " +
            "mouth's rock fins and clear of the driving line.")]
        [Range(-18f, 18f)]
        [SerializeField] private float _mouthLateral = 12f;

        public float PadRadius => _padRadius;
        public float ShoulderBearing => _shoulderBearing;
        public float ShoulderBearingSpread => _shoulderBearingSpread;
        public float ShoulderDistance => _shoulderDistance;
        public float ShoulderDistanceSpread => _shoulderDistanceSpread;
        public float MouthArc => _mouthArc;
        public float MouthLateral => _mouthLateral;

        /// <summary>Returns null when the settings are consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_shoulderDistance - _shoulderDistanceSpread <= 0f)
            {
                return "Relays: relay.1's search sector reaches back to the base.";
            }

            return null;
        }
    }
}
