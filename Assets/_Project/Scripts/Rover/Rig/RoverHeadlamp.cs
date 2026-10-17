using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's road light, one of the few things that works from the start and costs no battery: the warm spot at the
    /// RoverModel's HeadlampSocket, set up from the rig tuning, dimmed (eased) while 07 rests on its charging pad
    /// (<see cref="RoverController.IsDocked"/>) and while it crawls on the reserve. Ticked by
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

        private RoverController _rover;
        private BatteryLink _battery;
        private float _baseIntensity;
        private float _level = 1f;
        private float _appliedLevel = -1f;
        private bool _docked;

        /// <summary>How bright the road light is right now as a share of normal (dimmed on the pad and in the crawl).</summary>
        public float LampLevel => _level;

        /// <summary>Checks wiring and sets the road light up; false when broken (logged).</summary>
        public bool Initialize(GameContext context, RoverController rover)
        {
            if (_tuning == null || _headlamp == null)
            {
                Debug.LogError($"{nameof(RoverHeadlamp)}: the rig tuning or the Headlamp light is not assigned.", this);
                enabled = false;
                return false;
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

        /// <summary>07 rests on its charging pad (the road light dims) or left it.</summary>
        public void SetDocked(bool docked)
        {
            _docked = docked;
        }

        public void Tick(float deltaTime)
        {
            DockSettings dock = _rover.Tuning.Dock;
            float target = _docked ? dock.LampDim : _battery.IsLowPower ? _tuning.FlankLamps.CrawlHeadlamp : 1f;
            _level = Smoothing.Damp(_level, target, dock.LampHalfLife, deltaTime);
            ApplyLevel();
        }

        private void ApplyLevel()
        {
            if (Mathf.Abs(_level - _appliedLevel) < LevelEpsilon)
            {
                return;
            }

            _headlamp.intensity = _level * _baseIntensity;
            _appliedLevel = _level;
        }
    }
}
