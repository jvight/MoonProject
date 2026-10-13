using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Tab "07" page's portrait camera (docs/design/0.5.5-owner-feedback.md §3.5): a fixed side-three-quarter view
    /// of 07, in 07's own frame, and the render texture's size.
    /// </summary>
    [Serializable]
    public sealed class PortraitSettings
    {
        [Tooltip("The camera's bearing round 07 (deg from its nose, + toward its right side): side-three-quarter.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _yaw = 55f;

        [Tooltip("How far (deg) above 07's deck the camera looks down from.")]
        [Range(-10f, 60f)]
        [SerializeField] private float _elevation = 14f;

        [Tooltip("The camera's distance (m) from the point it looks at.")]
        [Range(1f, 10f)]
        [SerializeField] private float _distance = 5f;

        [Tooltip("The point the camera looks at: its height (m) above 07's root, the middle of the body and head.")]
        [Range(0f, 2f)]
        [SerializeField] private float _lookHeight = 0.6f;

        [Tooltip("Vertical field of view (deg): long, so 07 reads flat and calm like a spec drawing.")]
        [Range(10f, 60f)]
        [SerializeField] private float _fieldOfView = 30f;

        [Tooltip("Near and far clip (m) around 07.")]
        [SerializeField] private Vector2 _clip = new Vector2(0.1f, 12f);

        [Tooltip("The render texture's width (px).")]
        [Range(128, 2048)]
        [SerializeField] private int _width = 960;

        [Tooltip("The render texture's height (px).")]
        [Range(128, 2048)]
        [SerializeField] private int _height = 640;

        public PortraitSettings()
        {
        }

        public PortraitSettings(float yaw, float elevation, float distance, float lookHeight, float fieldOfView,
            int width, int height)
        {
            _yaw = yaw;
            _elevation = elevation;
            _distance = distance;
            _lookHeight = lookHeight;
            _fieldOfView = fieldOfView;
            _width = width;
            _height = height;
        }

        public float Yaw => _yaw;

        public float Elevation => _elevation;

        public float Distance => _distance;

        public float LookHeight => _lookHeight;

        public float FieldOfView => _fieldOfView;

        public float Near => _clip.x;

        public float Far => _clip.y;

        public int Width => _width;

        public int Height => _height;

        public float Aspect => (float)_width / _height;
    }
}
