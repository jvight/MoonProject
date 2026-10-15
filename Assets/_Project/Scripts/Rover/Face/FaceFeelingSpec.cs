using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Serializable target parameters for a single <see cref="FaceFeeling"/>, matching the numbers in
    /// <c>face_prototype.html</c>.
    /// </summary>
    [Serializable]
    public struct FaceFeelingSpec
    {
        [Tooltip("The feeling this specification defines.")]
        [SerializeField] private FaceFeeling _feeling;

        [Tooltip("Normalized eye width relative to screen height (prototype w).")]
        [Range(0.05f, 0.40f)]
        [SerializeField] private float _width;

        [Tooltip("Normalized eye height relative to screen height (prototype h).")]
        [Range(0.05f, 0.50f)]
        [SerializeField] private float _height;

        [Tooltip("Vertical eye openness multiplier (prototype open).")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _open;

        [Tooltip("Top eyelid tilt angle in radians; negative droops outer corners (prototype tilt).")]
        [Range(-1.2f, 1.2f)]
        [SerializeField] private float _tilt;

        [Tooltip("Top eyelid cut fraction from the top of the eye (prototype cut).")]
        [Range(0f, 1f)]
        [SerializeField] private float _cut;

        [Tooltip("Bottom eyelid squint fraction from the bottom of the eye (prototype squint).")]
        [Range(0f, 1f)]
        [SerializeField] private float _squint;

        [Tooltip("Upward happy crescent arc weight (prototype arc).")]
        [Range(0f, 1f)]
        [SerializeField] private float _arc;

        [Tooltip("Startled hollow ellipse ring weight (prototype ring).")]
        [Range(0f, 1f)]
        [SerializeField] private float _ring;

        [Tooltip("Asleep downward closed crescent weight (prototype closed).")]
        [Range(0f, 1f)]
        [SerializeField] private float _closed;

        [Tooltip("Base horizontal gaze offset in normalized screen width (prototype lx).")]
        [Range(-0.25f, 0.25f)]
        [SerializeField] private float _lookX;

        [Tooltip("Base vertical gaze offset in top-down screen height; negative looks up (prototype ly).")]
        [Range(-0.25f, 0.25f)]
        [SerializeField] private float _lookY;

        [Tooltip("Status/corner symbol shown with this feeling (prototype symbol).")]
        [SerializeField] private FaceSymbol _symbol;

        [Tooltip("Whether the left and right eyes use asymmetric curious proportions (prototype asym).")]
        [SerializeField] private bool _asymmetric;

        public FaceFeelingSpec(
            FaceFeeling feeling,
            float width,
            float height,
            float open,
            float tilt,
            float cut,
            float squint,
            float arc,
            float ring,
            float closed,
            float lookX,
            float lookY,
            FaceSymbol symbol,
            bool asymmetric)
        {
            _feeling = feeling;
            _width = width;
            _height = height;
            _open = open;
            _tilt = tilt;
            _cut = cut;
            _squint = squint;
            _arc = arc;
            _ring = ring;
            _closed = closed;
            _lookX = lookX;
            _lookY = lookY;
            _symbol = symbol;
            _asymmetric = asymmetric;
        }

        public FaceFeeling Feeling => _feeling;
        public float Width => _width;
        public float Height => _height;
        public float Open => _open;
        public float Tilt => _tilt;
        public float Cut => _cut;
        public float Squint => _squint;
        public float Arc => _arc;
        public float Ring => _ring;
        public float Closed => _closed;
        public float LookX => _lookX;
        public float LookY => _lookY;
        public FaceSymbol Symbol => _symbol;
        public bool Asymmetric => _asymmetric;
    }
}
