using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The crew's lab through the camera (docs/features/M3-19 §6): the crane path that follows 07 up under the belly
    /// and through the hatch, the ring-rail camera over 07's shoulder, the eased look in at a station, and how 07 is
    /// held on the platform while it rides.
    /// </summary>
    [Serializable]
    public sealed class LabCameraSettings
    {
        [Header("The crane path")]
        [Tooltip("Seconds the crane takes from the chase view up through the hatch to the ring, and back: about the " +
                 "lift's ride and settle (Gameplay's LiftTuning), so camera and platform arrive together.")]
        [Range(1f, 12f)]
        [SerializeField] private float _craneSeconds = 4.6f;

        [Tooltip("How far (m) out from the shaft, toward where the chase view stood, the crane passes under the " +
                 "belly.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _underBellyOut = 2.4f;

        [Tooltip("Height (m) above the ground at which the crane passes under the belly.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _underBellyHeight = 1.3f;

        [Tooltip("How far (m) out from the shaft the crane rises through the hatch (inside the hatch's opening).")]
        [Range(0f, 2f)]
        [SerializeField] private float _hatchOut = 0.9f;

        [Tooltip("Height (m) above 07's lens the crane looks while it follows 07 up.")]
        [Range(-1f, 2f)]
        [SerializeField] private float _followAim = 0.2f;

        [Header("The ring rail")]
        [Tooltip("Degrees round the ring from straight behind 07: the camera looks over its shoulder.")]
        [Range(0f, 45f)]
        [SerializeField] private float _shoulder = 16f;

        [Tooltip("Metres below the ring rail the camera hangs.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _railDrop = 0.15f;

        [Tooltip("Share of the way from the shaft to the station's face the ring camera aims at (0: at 07, 1: at the " +
                 "station).")]
        [Range(0f, 1.2f)]
        [SerializeField] private float _aimAhead = 0.8f;

        [Tooltip("Vertical field of view (deg) inside the lab.")]
        [Range(35f, 75f)]
        [SerializeField] private float _fov = 55f;

        [Tooltip("Near clip plane (m) inside the lab: the ring runs close under the ceiling.")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float _nearClip = 0.05f;

        [Header("Looking at a station")]
        [Tooltip("Seconds the camera eases in to a station and back out to the ring.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _focusSeconds = 1.2f;

        [Tooltip("How far (m) out from the station's face the close look stands, three-quarter on.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _focusDistance = 1.5f;

        [Tooltip("How far (m) to the side of the station's face the close look stands.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _focusSide = 0.55f;

        [Tooltip("How far (m) above the station's face the close look stands.")]
        [Range(-0.5f, 1.5f)]
        [SerializeField] private float _focusHeight = 0.3f;

        [Tooltip("Vertical field of view (deg) of the close look.")]
        [Range(25f, 70f)]
        [SerializeField] private float _focusFov = 45f;

        [Header("07 on the platform")]
        [Tooltip("Seconds the platform's guides take to ease 07 to the turntable's middle as the ride starts.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _centreSeconds = 0.8f;

        public float CraneSeconds => _craneSeconds;

        public float UnderBellyOut => _underBellyOut;

        public float UnderBellyHeight => _underBellyHeight;

        public float HatchOut => _hatchOut;

        public float FollowAim => _followAim;

        public float Shoulder => _shoulder;

        public float RailDrop => _railDrop;

        public float AimAhead => _aimAhead;

        public float Fov => _fov;

        public float NearClip => _nearClip;

        public float FocusSeconds => _focusSeconds;

        public float FocusDistance => _focusDistance;

        public float FocusSide => _focusSide;

        public float FocusHeight => _focusHeight;

        public float FocusFov => _focusFov;

        public float CentreSeconds => _centreSeconds;
    }
}
