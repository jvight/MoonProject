using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The camera's close shot while the Rover Bay fits a piece (docs/features/M3-14, ruling 14: the bay must be seen
    /// doing it): from the bay's view it eases in to a few metres off the socket, three-quarter on the working arm so
    /// the arm, the piece and its socket share the frame as it sets on, or low beside the turntable for a piece the
    /// floor arm lifts under 07's belly; as the arms fold away it eases back out to the bay's view. The shot is
    /// composed against the scenery (it keeps clear of the bay's walls, stays under the crane rail, is reached from
    /// the bay's view along a clear line and sees the socket past 07's bulk).
    /// </summary>
    [Serializable]
    public sealed class BayShotSettings
    {
        [Header("Easing")]
        [Tooltip("Seconds the camera takes to ease from the bay's view in to the fitting shot.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _easeIn = 1.3f;

        [Tooltip("Seconds the camera takes to ease back out to the bay's view as the arms fold away.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _easeOut = 1.4f;

        [Header("An arm setting a piece on")]
        [Tooltip("Distance (m) from the socket the camera stands, when the bay leaves room for it.")]
        [Range(2f, 6f)]
        [SerializeField] private float _armDistance = 3.6f;

        [Tooltip("Elevation (deg) the camera looks down at the socket from.")]
        [Range(0f, 40f)]
        [SerializeField] private float _armElevation = 12f;

        [Tooltip("Height (m) above the socket the camera aims at, so the arm coming down shares the frame.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _armFocusLift = 0.45f;

        [Tooltip("How far (deg) the view turns from side-on to the arm toward the arm's own side: 45 is a "
            + "three-quarter view, the links spread out and the socket clear of the arm.")]
        [Range(0f, 90f)]
        [SerializeField] private float _armQuarter = 45f;

        [Header("The floor arm lifting a piece under the belly")]
        [Tooltip("Distance (m) from the belly socket the low camera stands.")]
        [Range(1.5f, 6f)]
        [SerializeField] private float _bellyDistance = 3f;

        [Tooltip("Elevation (deg): low, to see under 07 between its wheels.")]
        [Range(0f, 20f)]
        [SerializeField] private float _bellyElevation = 4f;

        [Tooltip("Height (m) above the belly socket the camera aims at.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _bellyFocusLift = 0.05f;

        [Tooltip("Clearance (m) Cinemachine keeps from scenery while in the shot: small, so the low view under 07 "
            + "may skim the bay's floor (the chase camera's own clearance comes back as the shot eases out).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _cameraRadius = 0.12f;

        [Tooltip("Bearing (deg) of the low view off straight in through the bay's open front: a low three-quarter.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _bellyBearing = 20f;

        [Header("Composing against the bay")]
        [Tooltip("Closest (m) the camera may come to the socket when walls leave no room for the full distance; with "
            + "less room than this the shot is not used and the bay's view holds.")]
        [Range(1f, 5f)]
        [SerializeField] private float _minDistance = 1.8f;

        [Tooltip("Radius (m) of the sight lines the shot is checked along (to the socket, from the bay's view): "
            + "thick enough that no line merely grazes a wall edge or post.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _sightRadius = 0.25f;

        [Tooltip("Farthest (m) the camera may stand to either side of the turntable's centre, across the bay: inside "
            + "its side walls, even where a side is only a low wall the camera could look over (it may still stand out "
            + "through the open front).")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _sideReach = 1.9f;

        [Tooltip("Clearance (m) the camera keeps from walls and other solid scenery.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _wallClearance = 0.45f;

        [Tooltip("Half the width (deg) of the middle of the frame that must be clear of near scenery: no wall edge "
            + "or post right in front of the lens.")]
        [Range(0f, 30f)]
        [SerializeField] private float _frameWidth = 28f;

        [Tooltip("Half the height (deg) of the middle of the frame that must be clear of near scenery.")]
        [Range(0f, 20f)]
        [SerializeField] private float _frameHeight = 16f;

        [Tooltip("How much of the way to the socket's distance a line through the middle of the frame must run clear.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _frameClearShare = 0.5f;

        [Tooltip("Preference (deg of view angle per unit) for standing on the bay's open-front side.")]
        [Range(0f, 60f)]
        [SerializeField] private float _frontPreference = 15f;

        [Tooltip("Cost (deg of view angle per metre) of standing closer than the shot's distance.")]
        [Range(0f, 90f)]
        [SerializeField] private float _shortfallPenalty = 20f;

        [Header("07's bulk, which the camera must see past to the piece")]
        [Tooltip("Half the length (m) of 07's body.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _bodyHalfLength = 0.6f;

        [Tooltip("Half the width (m) of 07's body.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _bodyHalfWidth = 0.5f;

        [Tooltip("Height (m) of 07's body above its pivot (its head and antenna are thin enough to see past).")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _bodyHeight = 0.85f;

        [Tooltip("The last stretch (m) of the sight line to the piece on 07 that 07's bulk may cover.")]
        [Range(0f, 1f)]
        [SerializeField] private float _socketMargin = 0.35f;

        public float EaseIn => _easeIn;

        public float EaseOut => _easeOut;

        public float ArmDistance => _armDistance;

        public float ArmElevation => _armElevation;

        public float ArmFocusLift => _armFocusLift;

        public float ArmQuarter => _armQuarter;

        public float BellyDistance => _bellyDistance;

        public float BellyElevation => _bellyElevation;

        public float BellyFocusLift => _bellyFocusLift;

        public float BellyBearing => _bellyBearing;

        public float CameraRadius => _cameraRadius;

        public float MinDistance => _minDistance;

        public float WallClearance => _wallClearance;

        public float SideReach => _sideReach;

        public float SightRadius => _sightRadius;

        public float FrameWidth => _frameWidth;

        public float FrameHeight => _frameHeight;

        public float FrameClearShare => _frameClearShare;

        public float FrontPreference => _frontPreference;

        public float ShortfallPenalty => _shortfallPenalty;

        public float BodyHalfLength => _bodyHalfLength;

        public float BodyHalfWidth => _bodyHalfWidth;

        public float BodyHeight => _bodyHeight;

        public float SocketMargin => _socketMargin;
    }
}
