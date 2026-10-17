using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Thin MonoBehaviour adapter for <see cref="FaceDirector"/> (0.5.7). Subscribes to <see cref="EventBus"/> events,
    /// forwards <see cref="RoverBodyLanguage"/>'s single-source-of-truth gaze target to <see cref="FaceDirector"/>,
    /// pushes the resulting <see cref="FaceState"/> to <see cref="FaceDisplay"/>, and drives <c>_EmissionStrength</c>
    /// on <c>RearVentStrip</c>'s renderer so the player behind 07 sees its mood.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverFaceDirector : MonoBehaviour
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("Face director tuning asset (Assets/_Project/Data/Tuning/FaceDirectorTuning.asset).")]
        [SerializeField] private FaceDirectorTuning _tuning;

        [Tooltip("CRT face display component on Neck/NeckLift/Head/Screen.")]
        [SerializeField] private FaceDisplay _display;

        [Tooltip("Head transform (Neck/NeckLift/Head) for Earth-in-view checks.")]
        [SerializeField] private Transform _head;

        [Tooltip("RearVentStrip transform on Neck/NeckLift/Head/RearVentStrip.")]
        [SerializeField] private Transform _rearVentStrip;

        [Tooltip("Renderer receiving _EmissionStrength for the RearVentStrip mood echo.")]
        [SerializeField] private Renderer _rearVentRenderer;

        private FaceDirector _director;
        private MaterialPropertyBlock _rearVentBlock;
        private GameContext _context;
        private IRoverState _rover;
        private IRoverBattery _battery;
        private ISunlight _sunlight;
        private IWorldLayout _world;
        private IDisposable[] _subscriptions;
        private bool _servicesResolved;
        private bool _initialized;

        /// <summary>The pure C# <see cref="FaceDirector"/> logic instance.</summary>
        public FaceDirector Director => _director;

        /// <summary>The wired <see cref="FaceDisplay"/> on <c>Screen</c>.</summary>
        public FaceDisplay Display => _display;

        /// <summary>The wired <see cref="FaceDirectorTuning"/> asset.</summary>
        public FaceDirectorTuning Tuning => _tuning;

        /// <summary>The wired <c>RearVentStrip</c> transform.</summary>
        public Transform RearVentStrip => _rearVentStrip;

        /// <summary>The wired renderer receiving <c>_EmissionStrength</c> for <c>RearVentStrip</c>.</summary>
        public Renderer RearVentRenderer => _rearVentRenderer;

        /// <summary>The barrel curvature parameter on the CRT face material.</summary>
        public float Curvature => _display != null ? _display.Curvature : 0.12f;

        /// <summary>Explicitly wires all serialized references on this component.</summary>
        public void Wire(
            FaceDirectorTuning tuning,
            FaceDisplay display,
            Transform head,
            Transform rearVentStrip,
            Renderer rearVentRenderer)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _display = display != null ? display : throw new ArgumentNullException(nameof(display));
            _head = head != null ? head : throw new ArgumentNullException(nameof(head));
            _rearVentStrip = rearVentStrip != null ? rearVentStrip : throw new ArgumentNullException(nameof(rearVentStrip));
            _rearVentRenderer = rearVentRenderer != null ? rearVentRenderer : throw new ArgumentNullException(nameof(rearVentRenderer));
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!ValidateWiring() || !_display.EnsureInitialized())
            {
                enabled = false;
                return;
            }

            DisposeSubscriptions();

            _context = context;
            _display.ExternallyDriven = true;
            _director = new FaceDirector(_display.State, _tuning, context.Events);
            _rearVentBlock = new MaterialPropertyBlock();
            _servicesResolved = false;

            ResolveServices();

            EventBus events = context.Events;
            _subscriptions = new[]
            {
                events.Subscribe<RoverWakeBeatReached>(_director.OnWakeBeatReached),
                events.Subscribe<SessionStarted>(_director.OnSessionStarted),
                events.Subscribe<ChargingChanged>(_director.OnChargingChanged),
                events.Subscribe<BatteryWarned>(_director.OnBatteryWarned),
                events.Subscribe<LowPowerChanged>(_director.OnLowPowerChanged),
                events.Subscribe<ScanStarted>(_director.OnScanStarted),
                events.Subscribe<ScanStopped>(_director.OnScanStopped),
                events.Subscribe<FragmentScanned>(_director.OnFragmentScanned),
                events.Subscribe<BlueprintCompleted>(_director.OnBlueprintCompleted),
                events.Subscribe<UpgradePurchased>(_director.OnUpgradePurchased),
                events.Subscribe<PartFitted>(_director.OnPartFitted),
                events.Subscribe<RoverLanded>(_director.OnLanded),
                events.Subscribe<RoverRecovering>(_director.OnRecovering),
                events.Subscribe<TetherReleased>(_director.OnTetherReleased),
                events.Subscribe<FeelBeat>(_director.OnFeelBeat),
            };

            _initialized = true;
            _display.ApplyToRenderer();
            ApplyRearVent();
        }

        /// <summary>Validates that all serialized references are assigned, failing loudly with <see cref="Debug.LogError"/> if any are missing.</summary>
        public bool ValidateWiring()
        {
            bool displayOk = _display != null && _display.ValidateWiring();
            bool ok = _tuning != null &&
                displayOk &&
                _head != null &&
                _rearVentStrip != null &&
                _rearVentRenderer != null;

            if (!ok)
            {
                Debug.LogError(
                    $"{nameof(RoverFaceDirector)}: missing required wiring (_tuning={_tuning != null}, _display={displayOk}, " +
                    $"_head={_head != null}, _rearVentStrip={_rearVentStrip != null}, _rearVentRenderer={_rearVentRenderer != null}). Rebuild Rover.prefab.",
                    this);
            }

            return ok;
        }

        /// <summary>
        /// Steps the face director and applies both the CRT screen property block and the rear vent emission echo.
        /// Called once per frame by <see cref="RoverBodyLanguage"/> right after it steps its gaze target.
        /// </summary>
        public void Step(
            float deltaTime,
            bool hasExplicitGaze,
            float targetYawDeg,
            float targetPitchDeg,
            float currentYawDeg,
            float currentPitchDeg)
        {
            if (!_initialized || _director == null)
            {
                return;
            }

            if (!_servicesResolved)
            {
                ResolveServices();
                _servicesResolved = true;
            }

            if (_battery != null)
            {
                _director.PollBattery(_battery.Charge01, _battery.IsLowPower, _battery.IsCharging);
            }

            if (_rover != null)
            {
                bool inSunlight = _sunlight == null || _sunlight.SunlightAt(_rover.Position) > 0.05f;
                _director.StepMotion(_rover.IsGrounded, _rover.Velocity.y, _rover.Speed, inSunlight, deltaTime);
            }

            if (_world != null && _head != null)
            {
                _director.UpdateEarthView(_head.forward, _world.EarthDirection);
            }

            _director.UpdateGaze(hasExplicitGaze, targetYawDeg, targetPitchDeg, currentYawDeg, currentPitchDeg);
            _director.Step(deltaTime);
            _display.ApplyToRenderer();
            ApplyRearVent();
        }

        /// <summary>
        /// Enters or exits CRT screen content mode on <see cref="Display"/> and <see cref="Director"/>, binding
        /// <paramref name="contentTexture"/> to <c>_ContentTex</c>.
        /// </summary>
        public void SetScreenMode(bool open, Texture contentTexture = null)
        {
            if (_display != null)
            {
                _display.SetScreenMode(open, contentTexture);
            }

            if (_director != null)
            {
                _director.SetScreenMode(open);
            }

            ApplyRearVent();
        }

        /// <summary>
        /// Holds the CRT screen completely dark while 07 is asleep on the title or while the camera is inside the scan lens,
        /// until the front reveal beat (<see cref="WakeBeat.PulledOut"/>) starts the CRT boot sequence.
        /// </summary>
        public void SetOpeningDarkHold(bool hold)
        {
            if (_director != null)
            {
                _director.SetOpeningDarkHold(hold);
            }
            else if (_display != null && _display.State != null)
            {
                _display.State.SetDarkHold(hold);
            }

            if (_display != null)
            {
                _display.ApplyToRenderer();
            }

            ApplyRearVent();
        }

        private void ResolveServices()
        {
            if (_context == null)
            {
                return;
            }

            if (_rover == null)
            {
                _context.TryGet(out _rover);
            }

            if (_battery == null)
            {
                _context.TryGet(out _battery);
            }

            if (_sunlight == null)
            {
                _context.TryGet(out _sunlight);
            }

            if (_world == null)
            {
                _context.TryGet(out _world);
            }
        }

        private void ApplyRearVent()
        {
            if (_rearVentRenderer == null || _rearVentBlock == null || _director == null)
            {
                return;
            }

            _rearVentRenderer.GetPropertyBlock(_rearVentBlock);
            _rearVentBlock.SetFloat(EmissionStrengthId, _director.RearVentEmissionStrength);
            _rearVentRenderer.SetPropertyBlock(_rearVentBlock);
        }

        private void DisposeSubscriptions()
        {
            if (_subscriptions == null)
            {
                return;
            }

            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
            }

            _subscriptions = null;
        }

        private void OnDestroy()
        {
            DisposeSubscriptions();
        }
    }
}
