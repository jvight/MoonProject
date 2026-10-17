using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Thin MonoBehaviour driver for 07's expressive CRT screen (<c>RoverModel/Neck/NeckLift/Head/Screen</c>). Holds a
    /// <see cref="FaceState"/> and pushes all per-eye, phosphor-trail, symbol, boot, and low-power parameters to a
    /// <see cref="Renderer"/> via a single cached <see cref="MaterialPropertyBlock"/> and cached property IDs with zero
    /// per-frame allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FaceDisplay : MonoBehaviour
    {
        private const int DefaultSeed = 7;

        private static readonly int TrailAlphaId = Shader.PropertyToID("_TrailAlpha");
        private static readonly int EyeLeftRectId = Shader.PropertyToID("_EyeLeftRect");
        private static readonly int EyeLeftShapeId = Shader.PropertyToID("_EyeLeftShape");
        private static readonly int EyeLeftModeId = Shader.PropertyToID("_EyeLeftMode");
        private static readonly int EyeRightRectId = Shader.PropertyToID("_EyeRightRect");
        private static readonly int EyeRightShapeId = Shader.PropertyToID("_EyeRightShape");
        private static readonly int EyeRightModeId = Shader.PropertyToID("_EyeRightMode");
        private static readonly int GhostLeftRectId = Shader.PropertyToID("_GhostLeftRect");
        private static readonly int GhostLeftShapeId = Shader.PropertyToID("_GhostLeftShape");
        private static readonly int GhostLeftModeId = Shader.PropertyToID("_GhostLeftMode");
        private static readonly int GhostRightRectId = Shader.PropertyToID("_GhostRightRect");
        private static readonly int GhostRightShapeId = Shader.PropertyToID("_GhostRightShape");
        private static readonly int GhostRightModeId = Shader.PropertyToID("_GhostRightMode");
        private static readonly int FaceSymbolId = Shader.PropertyToID("_FaceSymbol");
        private static readonly int FaceCrtStateId = Shader.PropertyToID("_FaceCrtState");
        private static readonly int FacePhasesId = Shader.PropertyToID("_FacePhases");
        private static readonly int ContentTexId = Shader.PropertyToID("_ContentTex");
        private static readonly int FaceContentStateId = Shader.PropertyToID("_FaceContentState");
        private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");

        [Tooltip("Face tuning asset (Assets/_Project/Data/Tuning/FaceTuning.asset).")]
        [SerializeField] private FaceTuning _tuning;

        [Tooltip("Screen MeshRenderer displaying R1_CrtFace.")]
        [SerializeField] private Renderer _screenRenderer;

        [Tooltip("Deterministic random seed for blink and saccade intervals.")]
        [SerializeField] private int _seed = DefaultSeed;

        private FaceState _state;
        private MaterialPropertyBlock _propertyBlock;
        private Texture _contentTexture;
        private bool _externallyDriven;

        /// <summary>The underlying pure C# face state machine.</summary>
        public FaceState State => _state;

        /// <summary>The assigned <see cref="FaceTuning"/> asset.</summary>
        public FaceTuning Tuning => _tuning;

        /// <summary>The target <see cref="Renderer"/> receiving the CRT property block.</summary>
        public Renderer ScreenRenderer => _screenRenderer;

        /// <summary>Currently bound screen UI content texture (<c>_ContentTex</c>).</summary>
        public Texture ContentTexture => _contentTexture;

        /// <summary>CRT barrel curvature parameter read from the screen's shared material (defaults to 0.10).</summary>
        public float Curvature
        {
            get
            {
                if (_screenRenderer != null && _screenRenderer.sharedMaterial != null &&
                    _screenRenderer.sharedMaterial.HasProperty(CurvatureId))
                {
                    return _screenRenderer.sharedMaterial.GetFloat(CurvatureId);
                }

                return 0.10f;
            }
        }

        /// <summary>When true, <see cref="RoverFaceDirector"/> steps this display and <see cref="Update"/> is a no-op.</summary>
        public bool ExternallyDriven
        {
            get => _externallyDriven;
            set => _externallyDriven = value;
        }

        /// <summary>Initializes the display with explicit tuning, renderer, and deterministic seed.</summary>
        public void Initialize(FaceTuning tuning, Renderer screenRenderer, int seed = DefaultSeed)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _screenRenderer = screenRenderer != null ? screenRenderer : throw new ArgumentNullException(nameof(screenRenderer));
            _seed = seed;
            _state = new FaceState(_tuning, _seed);
            ApplyToRenderer();
        }

        /// <summary>Validates serialized wiring and ensures <see cref="State"/> is constructed.</summary>
        public bool EnsureInitialized()
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            if (_state == null)
            {
                _state = new FaceState(_tuning, _seed);
                ApplyToRenderer();
            }

            return true;
        }

        /// <summary>Checks that both <see cref="Tuning"/> and <see cref="ScreenRenderer"/> are wired, logging an error if not.</summary>
        public bool ValidateWiring()
        {
            if (_tuning != null && _screenRenderer != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(FaceDisplay)}: missing required wiring (_tuning={_tuning != null}, _screenRenderer={_screenRenderer != null}). Rebuild Rover.prefab.",
                this);
            return false;
        }

        private void Awake()
        {
            if (_state == null && _tuning != null && _screenRenderer != null)
            {
                _state = new FaceState(_tuning, _seed);
                ApplyToRenderer();
            }
        }

        private void Update()
        {
            if (_externallyDriven || _state == null)
            {
                return;
            }

            Step(Time.deltaTime);
        }

        /// <summary>Advances the face state by <paramref name="deltaTime"/> and pushes parameters to the renderer.</summary>
        public void Step(float deltaTime)
        {
            EnsureState();
            _state.Step(deltaTime);
            ApplyToRenderer();
        }

        /// <summary>Sets 07's target <see cref="FaceFeeling"/>.</summary>
        public void SetFeeling(FaceFeeling feeling)
        {
            EnsureState();
            _state.SetFeeling(feeling);
        }

        /// <summary>Triggers an immediate blink (ignored while <see cref="FaceFeeling.Asleep"/>).</summary>
        public void Blink()
        {
            EnsureState();
            _state.Blink();
        }

        /// <summary>Sets the gaze direction in the head's local coordinate frame.</summary>
        public void LookAt(Vector3 directionInHeadSpace)
        {
            EnsureState();
            _state.LookAt(directionInHeadSpace);
        }

        /// <summary>Clears any external gaze target.</summary>
        public void ClearLookTarget()
        {
            EnsureState();
            _state.ClearLookTarget();
        }

        /// <summary>Starts the CRT power-on boot sequence from 0.</summary>
        public void Boot()
        {
            EnsureState();
            _state.Boot();
            ApplyToRenderer();
        }

        /// <summary>Sets the low-power state amount, clamped to [0..1].</summary>
        public void SetLowPower(float amount)
        {
            EnsureState();
            _state.SetLowPower(amount);
            ApplyToRenderer();
        }

        /// <summary>
        /// Enters or exits CRT screen content mode with the channel-switch choreography and binds <paramref name="contentTexture"/>
        /// to <c>_ContentTex</c> when provided.
        /// </summary>
        public void SetScreenMode(bool open, Texture contentTexture = null)
        {
            EnsureState();
            if (contentTexture != null)
            {
                _contentTexture = contentTexture;
            }

            _state.SetScreenMode(open);
            ApplyToRenderer();
        }

        /// <summary>Immediately snaps the CRT screen content mode without playing the transition choreography.</summary>
        public void SnapToScreenMode(bool open, Texture contentTexture = null)
        {
            EnsureState();
            if (contentTexture != null)
            {
                _contentTexture = contentTexture;
            }

            _state.SnapToScreenMode(open);
            ApplyToRenderer();
        }

        /// <summary>Pushes the current <see cref="FaceState"/> parameters to <see cref="ScreenRenderer"/>.</summary>
        public void ApplyToRenderer()
        {
            if (_state == null || _screenRenderer == null || _tuning == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            _screenRenderer.GetPropertyBlock(_propertyBlock);

            FaceEyeParams left = _state.LeftEye;
            FaceEyeParams right = _state.RightEye;
            FaceEyeParams ghostLeft = _state.GhostLeftEye;
            FaceEyeParams ghostRight = _state.GhostRightEye;

            _propertyBlock.SetFloat(TrailAlphaId, _tuning.TrailAlpha);

            _propertyBlock.SetVector(EyeLeftRectId, left.ToRectVector());
            _propertyBlock.SetVector(EyeLeftShapeId, left.ToShapeVector());
            _propertyBlock.SetVector(EyeLeftModeId, left.ToModeVector());

            _propertyBlock.SetVector(EyeRightRectId, right.ToRectVector());
            _propertyBlock.SetVector(EyeRightShapeId, right.ToShapeVector());
            _propertyBlock.SetVector(EyeRightModeId, right.ToModeVector());

            _propertyBlock.SetVector(GhostLeftRectId, ghostLeft.ToRectVector());
            _propertyBlock.SetVector(GhostLeftShapeId, ghostLeft.ToShapeVector());
            _propertyBlock.SetVector(GhostLeftModeId, ghostLeft.ToModeVector());

            _propertyBlock.SetVector(GhostRightRectId, ghostRight.ToRectVector());
            _propertyBlock.SetVector(GhostRightShapeId, ghostRight.ToShapeVector());
            _propertyBlock.SetVector(GhostRightModeId, ghostRight.ToModeVector());

            _propertyBlock.SetVector(
                FaceSymbolId,
                new Vector4((float)_state.ActiveSymbol, _state.SymbolAlpha, _state.ElapsedTime, 0f));
            _propertyBlock.SetVector(
                FaceCrtStateId,
                new Vector4(_state.BootProgress, _state.Brightness, _state.LowPower, _state.VerticalJitter));
            _propertyBlock.SetVector(
                FacePhasesId,
                new Vector4(_state.ScanlinePhase, _state.RollingBandPhase, _tuning.BootLineEnd, _tuning.BootOpenSpan));
            _propertyBlock.SetVector(
                FaceContentStateId,
                new Vector4(_state.ScreenContentMix, _state.ScreenVerticalPinch, _state.ScreenLineGlow, 0f));

            if (_contentTexture != null)
            {
                _propertyBlock.SetTexture(ContentTexId, _contentTexture);
            }

            _screenRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void EnsureState()
        {
            if (_state != null)
            {
                return;
            }

            if (_tuning == null)
            {
                throw new InvalidOperationException("FaceDisplay: FaceTuning is not assigned.");
            }

            _state = new FaceState(_tuning, _seed);
        }
    }
}
