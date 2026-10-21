using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The socket rack seen at the pad (docs/design/opening-v3-plan.md 4.3, <see cref="SocketRackView"/>): Art's prefab,
    /// where it stands beside the dock, how it rises out of the ground and lights its bays when the release reveals it,
    /// how each bay glows in each state and how its jaws close when its part is fitted, and how near 07 reads a bay.
    /// </summary>
    [Serializable]
    public sealed class RackViewSettings
    {
        [Tooltip("Art's SocketRack prefab (Bay_0..n with BackPlate, Outline, Fill, PartSocket, Lamp, Clamp_L/R).")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Metres from the dock (07's place on the pad) to the rack's foot, in the dock's frame: x to 07's " +
                 "right, z ahead.")]
        [SerializeField] private Vector3 _offset = new Vector3(1.9f, 0f, 0f);

        [Tooltip("Degrees the rack turns from 07's heading on the pad: -90 faces its front back toward 07 from 07's " +
                 "right.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _yawFromDock = -90f;

        [Tooltip("Metres the rack starts below the ground when the release reveals it, rising to stand.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _riseDepth = 1.4f;

        [Tooltip("Seconds the rack takes to rise to stand (eased in and out).")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _riseSeconds = 1.4f;

        [Tooltip("Seconds between one bay lighting and the next, left to right, once the rack stands.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bayStaggerSeconds = 0.25f;

        [Tooltip("Seconds a bay's glow takes to ease to a new state.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _glowSeconds = 0.5f;

        [Tooltip("The outline's glow colour (linear): Art's warm amber line.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _outlineColor = new Color(1f, 0.70f, 0.40f);

        [Tooltip("The outline's strength while the part is seen but not yet earned.")]
        [Range(0f, 2f)]
        [SerializeField] private float _outlineDim = 0.35f;

        [Tooltip("The outline's strength once the part is fitted.")]
        [Range(0f, 4f)]
        [SerializeField] private float _outlineBright = 1.4f;

        [Tooltip("Seconds of one slow breath of a seen outline.")]
        [Range(1f, 10f)]
        [SerializeField] private float _breathSeconds = 3.5f;

        [Tooltip("Share of the dim outline's strength a breath takes away at its lowest.")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _breathDepth = 0.25f;

        [Tooltip("The fill's glow colour (linear) once the part is fitted.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _fillColor = new Color(0.95f, 0.62f, 0.34f);

        [Tooltip("The bay lamp's glow colour (linear) once the part is fitted.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _lampColor = new Color(1f, 0.76f, 0.32f);

        [Tooltip("Seconds a bay's jaws take to close when its part is fitted (eased; Audio clunks).")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _jawSeconds = 0.4f;

        [Tooltip("Metres from 07 within which it reads a bay's outline (the aim chip).")]
        [Range(1f, 10f)]
        [SerializeField] private float _readReach = 4f;

        [Tooltip("A bay counts as in view this far in from each edge of the screen (a share of it).")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _viewMargin = 0.1f;

        public GameObject Prefab => _prefab;

        public Vector3 Offset => _offset;

        public float YawFromDock => _yawFromDock;

        public float RiseDepth => _riseDepth;

        public float RiseSeconds => _riseSeconds;

        public float BayStaggerSeconds => _bayStaggerSeconds;

        public float GlowSeconds => _glowSeconds;

        public Color OutlineColor => _outlineColor;

        public float OutlineDim => _outlineDim;

        public float OutlineBright => _outlineBright;

        public float BreathSeconds => _breathSeconds;

        public float BreathDepth => _breathDepth;

        public Color FillColor => _fillColor;

        public Color LampColor => _lampColor;

        public float JawSeconds => _jawSeconds;

        public float ReadReach => _readReach;

        public float ViewMargin => _viewMargin;

        /// <summary>Art's rack for a tuning made in code (tests); the shipped asset names its own.</summary>
        internal void PopulatePrefab(GameObject prefab)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        /// <summary>Null when the rack can be seen, else the first problem.</summary>
        public string Validate()
        {
            return _prefab == null ? "the socket rack has no prefab (Art's SocketRack)" : null;
        }
    }
}
