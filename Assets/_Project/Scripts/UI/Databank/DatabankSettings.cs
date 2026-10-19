using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's databank page (docs/design/databank.md §4): the page flip, the turning model on its stage, the colour
    /// toggle's wireframe and the unlock line on 07's face.
    /// </summary>
    [Serializable]
    public sealed class DatabankSettings
    {
        private const int MinStagePixels = 64;

        [Tooltip("Seconds the page collapses to the CRT line before the other page opens (the channel flip).")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _flipCollapseSeconds = 0.16f;

        [Tooltip("Seconds the line takes to open into the other page.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _flipOpenSeconds = 0.22f;

        [Tooltip("Share of its height the page keeps at the flip's thinnest (the bright line).")]
        [Range(0.005f, 0.2f)]
        [SerializeField] private float _flipLineShare = 0.02f;

        [Tooltip("Degrees per second the model turns by itself while nobody turns it.")]
        [Range(0f, 30f)]
        [SerializeField] private float _idleTurnDegreesPerSecond = 6f;

        [Tooltip("Seconds after a hand-turn before the model turns by itself again.")]
        [Range(0f, 10f)]
        [SerializeField] private float _idleResumeSeconds = 2.5f;

        [Tooltip("Degrees the model turns per panel pixel of mouse drag.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _dragDegreesPerPixel = 0.35f;

        [Tooltip("Degrees per second the right stick turns the model at full tilt.")]
        [Range(10f, 360f)]
        [SerializeField] private float _stickDegreesPerSecond = 110f;

        [Tooltip("Degrees the model may tip up or down from level.")]
        [Range(0f, 80f)]
        [SerializeField] private float _pitchLimitDegrees = 30f;

        [Tooltip("Seconds the model eases toward where the hand turned it (a soft follow, not a snap).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _turnFollowSeconds = 0.12f;

        [Tooltip("Vertical field of view (degrees) of the stage camera that films the model.")]
        [Range(10f, 60f)]
        [SerializeField] private float _stageFieldOfView = 26f;

        [Tooltip("How much room the framing leaves around the model (1 = its bounds touch the frame).")]
        [Range(1f, 2f)]
        [SerializeField] private float _stageFrameMargin = 1.25f;

        [Tooltip("Pixels of the stage's render texture (twice the 860 x 420 panel rect, like the face texture).")]
        [SerializeField] private Vector2Int _stageResolution = new Vector2Int(1720, 840);

        [Tooltip("Where the stage stands in the world: far beyond every camera's reach, so its camera sees only the " +
                 "model.")]
        [SerializeField] private Vector3 _stagePosition = new Vector3(0f, -4000f, 0f);

        [Tooltip("The scan wireframe the model draws in, in amber (the build ghost's line-drawing material).")]
        [SerializeField] private Material _wireMaterial;

        [Tooltip("Stage fade of the wireframe at the first piece of a set: faint, brightening to full as pieces come.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _wireFirstPieceFade = 0.35f;

        [Tooltip("Warm key light colour on the model in full colour.")]
        [SerializeField] private Color _keyColour = new Color(1f, 0.93f, 0.82f);

        [Tooltip("Key light intensity in full colour.")]
        [Range(0f, 4f)]
        [SerializeField] private float _keyIntensity = 1.6f;

        [Tooltip("Where the key light comes from (Euler degrees, stage space; the camera looks along +Z).")]
        [SerializeField] private Vector3 _keyAngles = new Vector3(35f, -40f, 0f);

        [Tooltip("Cool fill light colour on the model in full colour.")]
        [SerializeField] private Color _fillColour = new Color(0.62f, 0.72f, 0.92f);

        [Tooltip("Fill light intensity in full colour.")]
        [Range(0f, 4f)]
        [SerializeField] private float _fillIntensity = 0.55f;

        [Tooltip("Where the fill light comes from (Euler degrees, stage space).")]
        [SerializeField] private Vector3 _fillAngles = new Vector3(-10f, 140f, 0f);

        [Tooltip("Degrees a print leans on the page, like a sheet laid on the scanner by hand.")]
        [Range(0f, 6f)]
        [SerializeField] private float _printSkewDegrees = 1.4f;

        [Tooltip("The tube's amber a print reads in until F shows its true colours.")]
        [SerializeField] private Color _printAmberTint = new Color(1f, 0.78f, 0.42f);

        [Tooltip("Seconds the 'Added to databank' line stays on 07's face.")]
        [Range(1f, 8f)]
        [SerializeField] private float _toastSeconds = 3f;

        public float FlipCollapseSeconds => _flipCollapseSeconds;

        public float FlipOpenSeconds => _flipOpenSeconds;

        public float FlipLineShare => _flipLineShare;

        public float IdleTurnDegreesPerSecond => _idleTurnDegreesPerSecond;

        public float IdleResumeSeconds => _idleResumeSeconds;

        public float DragDegreesPerPixel => _dragDegreesPerPixel;

        public float StickDegreesPerSecond => _stickDegreesPerSecond;

        public float PitchLimitDegrees => _pitchLimitDegrees;

        public float TurnFollowSeconds => _turnFollowSeconds;

        public float StageFieldOfView => _stageFieldOfView;

        public float StageFrameMargin => _stageFrameMargin;

        public Vector2Int StageResolution => _stageResolution;

        public Vector3 StagePosition => _stagePosition;

        public Material WireMaterial => _wireMaterial;

        public float WireFirstPieceFade => _wireFirstPieceFade;

        public Color KeyColour => _keyColour;

        public float KeyIntensity => _keyIntensity;

        public Quaternion KeyRotation => Quaternion.Euler(_keyAngles);

        public Color FillColour => _fillColour;

        public Quaternion FillRotation => Quaternion.Euler(_fillAngles);

        public float FillIntensity => _fillIntensity;

        public float PrintSkewDegrees => _printSkewDegrees;

        public Color PrintAmberTint => _printAmberTint;

        public float ToastSeconds => _toastSeconds;

        /// <summary>Null when usable, else the first problem.</summary>
        public string Validate()
        {
            if (_wireMaterial == null)
            {
                return "the databank's wireframe material is not assigned (run the UI/Tuning builder)";
            }

            return _stageResolution.x < MinStagePixels || _stageResolution.y < MinStagePixels
                ? "the databank stage's resolution is too small"
                : null;
        }
    }
}
