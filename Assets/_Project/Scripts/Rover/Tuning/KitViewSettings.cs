using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where the gift moment looks from for each friend's gift and each piece Kenji's garage port fits, so the gift faces the camera: the direction the
    /// camera looks along, in degrees from 07's heading (0 = the normal chase view from behind, 180 = looking back at
    /// 07's face, negative = turning left, so the camera stands off 07's right side). Crafted kit is fitted inside the
    /// Rover Bay's own view instead (<see cref="BayFramingSettings"/>).
    /// </summary>
    [Serializable]
    public sealed class KitViewSettings
    {
        [Tooltip("View of Tilly's cell on the solar wing: a rear three-quarter.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _solarCell = -35f;

        [Tooltip("View of Bell's fresh paint and pennant: from the side.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _freshPaint = -90f;

        [Tooltip("View of the old tape deck on the left flank: a rear three-quarter from 07's left side.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _tapeDeck = 60f;

        [Tooltip("View of Kenji's tether ring round the eye: from the front, a little to the side.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _tetherRing = 155f;

        [Tooltip("Distance (m) of the point the moment turns toward; only its bearing matters, within focus range.")]
        [Range(1f, 30f)]
        [SerializeField] private float _subjectDistance = 6f;

        public float SubjectDistance => _subjectDistance;

        /// <summary>The look direction (deg from 07's heading) of the moment for <paramref name="gift"/>.</summary>
        public float Bearing(RoverKitPiece gift)
        {
            switch (gift)
            {
                case RoverKitPiece.SolarCell:
                    return _solarCell;
                case RoverKitPiece.FreshPaint:
                    return _freshPaint;
                case RoverKitPiece.TapeDeck:
                    return _tapeDeck;
                case RoverKitPiece.TetherRing:
                    return _tetherRing;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gift), gift, "Only friends' gifts have a gift view.");
            }
        }
    }
}
