using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The camera while 07 stands in Kenji's Rover Bay (docs/features/M3-14): it eases to a fixed view in through
    /// the bay's open front, showing 07 on the turntable, the gantry arms over it and the hopper by the entrance, and
    /// holds there however the turntable turns 07, so it never swings round into the bay's walls. It stays low enough
    /// to look in under the crane rail, never over the roof. Looking around dismisses it until 07 next drives in (or
    /// the bay starts fitting a piece); driving out hands back to the chase camera.
    /// </summary>
    [Serializable]
    public sealed class BayFramingSettings
    {
        [Tooltip("How near (m, on the ground plane) the turntable's centre 07 must come for the bay view.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _radius = 2f;

        [Tooltip("How much farther (m) 07 must go before the bay view hands back, so its edge never flickers.")]
        [Range(0f, 2f)]
        [SerializeField] private float _hysteresis = 0.5f;

        [Tooltip("Distance (m) into the bay, along 07's parked facing, of the point the view looks toward; only its "
            + "bearing matters, within the moment's focus range.")]
        [Range(2f, 30f)]
        [SerializeField] private float _subjectDistance = 10f;

        [Tooltip("Elevation (deg above 07's follow point) the view looks in from, whatever the player's pitch was: low "
            + "enough to see under the roof's edge to the arms, high enough to see 07's back and the turntable.")]
        [Range(0f, 30f)]
        [SerializeField] private float _elevation = 9f;

        [Tooltip("Clearance (m) kept below the crane rail (the arms' shoulders): the camera never rises above it, so "
            + "it looks in under the roof rather than over it.")]
        [Range(0f, 2f)]
        [SerializeField] private float _railClearance = 0.5f;

        public float Radius => _radius;

        public float Hysteresis => _hysteresis;

        public float SubjectDistance => _subjectDistance;

        public float Elevation => _elevation;

        public float RailClearance => _railClearance;
    }
}
