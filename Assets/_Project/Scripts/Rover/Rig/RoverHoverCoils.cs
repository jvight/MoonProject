using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Shows art's HoverCoils (mounted on RoverModel 'CoilSocket') only while 07 owns the Hover-Jump, popping them in
    /// when it is bought (<see cref="HoverCoilMotion"/>). While the jump charges, Coil_FL/FR/RL/RR squash along local
    /// Y and their Glow_ rings light up (MaterialPropertyBlock _EmissionColor = linear glow, set as a vector), and a soft cyan point
    /// light at CoilSocket pools on the ground so the charge reads from the chase camera; on the leap the springs kick
    /// out. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverHoverCoils : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset), 'Hover-Jump coils'.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("The HoverCoils instance under RoverModel 'CoilSocket' (inactive until the Hover-Jump is owned).")]
        [SerializeField] private Transform _mount;

        [Tooltip("HoverCoils springs in order Coil_FL, Coil_FR, Coil_RL, Coil_RR (pivot at the top, squash local Y).")]
        [SerializeField] private Transform[] _coils = new Transform[RoverModelNodes.CoilCount];

        [Tooltip("Glow ring renderers in order Glow_FL, Glow_FR, Glow_RL, Glow_RR (M_LowPolyGlowOff).")]
        [SerializeField] private Renderer[] _glows = new Renderer[RoverModelNodes.CoilCount];

        [Tooltip("Soft cyan point light at CoilSocket (no shadows), lit while the jump charges.")]
        [SerializeField] private Light _light;

        private RoverController _rover;
        private RoverVisualRig _rig;
        private HoverCoilMotion _motion;
        private MaterialPropertyBlock _glowBlock;
        private IDisposable _jumpedSubscription;
        private Vector3[] _coilRestScales;
        private Vector3 _mountRestScale;
        private float _appliedGlow = -1f;

        /// <summary>The coils' motion (visibility, pop-in, squash, glow), for tests and tooling.</summary>
        public HoverCoilMotion Motion => _motion;

        /// <summary>Validates the wiring and starts hidden. Returns false (and logs) when broken.</summary>
        public bool Initialize(GameContext context, RoverController rover, RoverVisualRig rig)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _rover = rover;
            _rig = rig;
            HoverCoilSettings settings = _tuning.HoverCoils;
            _motion = new HoverCoilMotion(settings);
            _glowBlock = new MaterialPropertyBlock();
            _mountRestScale = _mount.localScale;
            _coilRestScales = new Vector3[_coils.Length];
            for (int i = 0; i < _coils.Length; i++)
            {
                _coilRestScales[i] = _coils[i].localScale;
            }

            _light.type = LightType.Point;
            _light.range = settings.LightRange;
            _light.shadows = LightShadows.None;
            _mount.gameObject.SetActive(false);
            ApplyGlow(0f);
            _jumpedSubscription = context.Events.Subscribe<RoverJumped>(OnJumped);
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverRigTuning is not assigned.")
                & Require(_mount != null, "HoverCoils mount is not assigned.")
                & Require(_light != null, "Coil light is not assigned.")
                & Require(_coils != null && _coils.Length == RoverModelNodes.CoilCount,
                    $"Exactly {RoverModelNodes.CoilCount} coils are required.")
                & Require(_glows != null && _glows.Length == RoverModelNodes.CoilCount,
                    $"Exactly {RoverModelNodes.CoilCount} glow rings are required.");
            for (int i = 0; _coils != null && i < _coils.Length; i++)
            {
                ok &= Require(_coils[i] != null, $"Coil {i} is not assigned.");
            }

            for (int i = 0; _glows != null && i < _glows.Length; i++)
            {
                ok &= Require(_glows[i] != null, $"Glow ring {i} is not assigned.");
            }

            return ok;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverHoverCoils)}: {message}", this);
            }

            return condition;
        }

        private void OnJumped(RoverJumped jumped)
        {
            _motion.Leap(jumped.Strength);
        }

        public void Tick(float deltaTime)
        {
            bool wasVisible = _motion.Visible;
            if (_motion.Step(_rover.Has(RoverAbility.HoverJump), _rover.JumpCharge, deltaTime))
            {
                HoverCoilSettings settings = _tuning.HoverCoils;
                _rig.KickHeave(settings.PopHeaveKick);
                _rig.KickAntenna(settings.PopAntennaKick);
            }

            if (!_motion.Visible)
            {
                return;
            }

            if (!wasVisible)
            {
                _mount.gameObject.SetActive(true);
            }

            _mount.localScale = _mountRestScale * _motion.MountScale;
            float length = _motion.CoilLength;
            for (int i = 0; i < _coils.Length; i++)
            {
                Vector3 scale = _coilRestScales[i];
                scale.y *= length;
                _coils[i].localScale = scale;
            }

            ApplyGlow(_motion.Glow);
        }

        private void ApplyGlow(float glow)
        {
            if (glow == _appliedGlow)
            {
                return;
            }

            HoverCoilSettings settings = _tuning.HoverCoils;
            float emission = settings.GlowPeak * Mathf.Pow(glow, settings.GlowCurve);
            _glowBlock.SetVector(EmissionColorId, new Vector4(emission, emission, emission, 1f));
            for (int i = 0; i < _glows.Length; i++)
            {
                _glows[i].SetPropertyBlock(_glowBlock);
            }

            _light.intensity = settings.LightIntensity * glow;
            _light.enabled = glow > 0f;
            _appliedGlow = glow;
        }

        private void OnDestroy()
        {
            _jumpedSubscription?.Dispose();
        }
    }
}
