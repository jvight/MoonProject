using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's road light, one of the few things that works from the start and costs no battery: the warm spot at the
    /// RoverModel's HeadlampSocket and the glow of the headlamp glasses (<see cref="HeadlampGlass"/>, plus the SSR-3
    /// brow pod lenses once fitted), set up from the rig tuning, dimmed (eased) while 07 rests on its charging pad
    /// (<see cref="RoverController.IsDocked"/>) and while it crawls on the reserve; the wake's lamp power switches
    /// both, and so does the player's lights key, easing them on or off (<see cref="SetLightsOn"/>). Ticked by
    /// <see cref="RoverController"/> in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverHeadlamp : MonoBehaviour
    {
        /// <summary>Level changes smaller than this are not worth writing to the light.</summary>
        private const float LevelEpsilon = 1e-4f;

        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset): 'Headlamp', 'Flank lamps'.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("Warm spot light under RoverModel 'HeadlampSocket' (07's road light).")]
        [SerializeField] private Light _headlamp;

        [Tooltip("RoverModel 'Body': its R1_LampAmber slot is the headlamp glasses, which light with the road light.")]
        [SerializeField] private Renderer _lampBody;

        [Tooltip("Which material slot of the body renderer is the headlamp glasses.")]
        [Min(0)]
        [SerializeField] private int _lampGlassSlot;

        [Tooltip("Kit_SSR3Headlamp 'Lens_L' and 'Lens_R' under Head/BrowSocket: glow with the road light once fitted.")]
        [SerializeField] private Renderer[] _podLenses = Array.Empty<Renderer>();

        private RoverController _rover;
        private BatteryLink _battery;
        private HeadlampGlass _glow;
        private HeadlampGlass[] _podGlows = Array.Empty<HeadlampGlass>();
        private float _baseIntensity;
        private float _level = 1f;
        private float _switch = 1f;
        private float _lastPower = 1f;
        private float _podWarmth;
        private float _appliedLevel = -1f;
        private float _appliedPodLevel = -1f;
        private bool _docked;

        /// <summary>How bright the road light is right now as a share of normal (dimmed on the pad and in the crawl).</summary>
        public float LampLevel => _level;

        /// <summary>The player has 07's lamps switched on (they ease to this over the tuned switch time).</summary>
        public bool LightsOn { get; private set; } = true;

        /// <summary>How far the lamps have eased toward <see cref="LightsOn"/>: 0 dark, 1 lit.</summary>
        public float SwitchLevel => _switch;

        /// <summary>How warm the SSR-3 brow pod lenses are from fitting: 0 before fit, 0..1 during fit, 1 fitted.</summary>
        public float PodWarmth => _podWarmth;

        /// <summary>Checks wiring and sets the road light up; false when broken (logged).</summary>
        public bool Initialize(GameContext context, RoverController rover)
        {
            if (_tuning == null || _headlamp == null || _lampBody == null)
            {
                Debug.LogError($"{nameof(RoverHeadlamp)}: the rig tuning, the Headlamp light or the body with the " +
                    "headlamp glasses is not assigned; rebuild the Rover prefab.", this);
                enabled = false;
                return false;
            }

            if (!HeadlampGlass.TryCreate(_lampBody, _lampGlassSlot, out _glow, out string problem))
            {
                Debug.LogError($"{nameof(RoverHeadlamp)}: {problem}", this);
                enabled = false;
                return false;
            }

            if (_podLenses != null && _podLenses.Length > 0)
            {
                _podGlows = new HeadlampGlass[_podLenses.Length];
                for (int i = 0; i < _podLenses.Length; i++)
                {
                    if (!HeadlampGlass.TryCreate(_podLenses[i], 0, out _podGlows[i], out string podProblem))
                    {
                        Debug.LogError($"{nameof(RoverHeadlamp)}: pod lens {i}: {podProblem}", this);
                        enabled = false;
                        return false;
                    }
                }
            }
            else
            {
                _podGlows = Array.Empty<HeadlampGlass>();
            }

            _rover = rover;
            _battery = new BatteryLink(context);
            _headlamp.type = LightType.Spot;
            _headlamp.transform.localRotation = Quaternion.Euler(_tuning.HeadlampDownTilt, 0f, 0f);
            _baseIntensity = _tuning.HeadlampIntensity;
            _headlamp.range = _tuning.HeadlampRange;
            _headlamp.spotAngle = _tuning.HeadlampSpotAngle;
            _headlamp.innerSpotAngle = _tuning.HeadlampInnerSpotAngle;
            ApplyLevel();
            return true;
        }

        /// <summary>
        /// Switches 07's lamps on or off: they ease there over the tuned switch time, or at once when
        /// <paramref name="instant"/> (a restored save).
        /// </summary>
        public void SetLightsOn(bool on, bool instant)
        {
            LightsOn = on;
            if (instant)
            {
                _switch = on ? 1f : 0f;
                ApplyLevel(_lastPower);
            }
        }

        /// <summary>07 rests on its charging pad (the road light dims) or left it.</summary>
        public void SetDocked(bool docked)
        {
            _docked = docked;
        }

        /// <summary>Sets the brow headlamp pods' fit warmth (0 before fit, 0..1 during fit, 1 once fitted).</summary>
        public void SetPodWarmth(float warmth)
        {
            _podWarmth = Mathf.Clamp01(warmth);
            ApplyPodLevel(_lastPower);
        }

        /// <param name="power">The lamp power the wake allows (1 normally; the flicker and ramp at wake).</param>
        public void Tick(float deltaTime, float power)
        {
            DockSettings dock = _rover.Tuning.Dock;
            float target = _docked ? dock.LampDim : _battery.IsLowPower ? _tuning.FlankLamps.CrawlHeadlamp
                : _battery.OnOpeningReserve ? _tuning.FlankLamps.OpeningReserveHeadlamp : 1f;
            _level = Smoothing.Damp(_level, target, dock.LampHalfLife, deltaTime);
            _switch = Mathf.MoveTowards(_switch, LightsOn ? 1f : 0f, deltaTime / _tuning.LampSwitchSeconds);
            ApplyLevel(power);
        }

        private void ApplyLevel(float power = 1f)
        {
            _lastPower = power;
            float level = _level * power * Mathf.SmoothStep(0f, 1f, _switch);
            ApplyPodLevel(power);
            // A tiny step is not worth writing, except the last one to dark: switched off, the lamp goes fully out.
            if (Mathf.Abs(level - _appliedLevel) < LevelEpsilon && (level > 0f || _appliedLevel == 0f))
            {
                return;
            }

            _headlamp.intensity = level * _baseIntensity;
            _glow.Apply(level);
            _appliedLevel = level;
        }

        private void ApplyPodLevel(float power)
        {
            if (_podGlows.Length == 0)
            {
                return;
            }

            float podLevel = _level * power * Mathf.SmoothStep(0f, 1f, _switch) * _podWarmth;
            if (Mathf.Abs(podLevel - _appliedPodLevel) < LevelEpsilon && (podLevel > 0f || _appliedPodLevel == 0f))
            {
                return;
            }

            for (int i = 0; i < _podGlows.Length; i++)
            {
                _podGlows[i].Apply(podLevel);
            }

            _appliedPodLevel = podLevel;
        }
    }
}
