using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// The parts 07 grows in the opening, on RoverModel (docs/design/opening-v3-plan.md 2.6): the antenna's second
    /// segment (<see cref="RoverModelNodes.AntennaSegment2"/>) rests retracted in its mast and slides out at the first
    /// charge; the igniter cartridge (<see cref="RoverModelNodes.CutterIgniter"/>) is hidden until the cutter turns OK
    /// and then seats into its socket with a small click. A loaded game shows both as saved, silently, from the
    /// events the session replays with Restored set. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverGrowth : MonoBehaviour
    {
        [Tooltip("Rig tuning (its Growth settings).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("RoverModel 'Antenna/Antenna_Segment2': rests retracted inside the mast.")]
        [SerializeField] private Transform _antennaSegment;

        [Tooltip("RoverModel 'CutterTurret/Cutter_Igniter': the cartridge in the turret socket.")]
        [SerializeField] private Transform _igniter;

        [Tooltip("RoverModel 'CutterTurret': the space the cartridge's pull-out direction is in.")]
        [SerializeField] private Transform _turret;

        private IDisposable _milestones;
        private IDisposable _systems;
        private Vector3 _antennaRest;
        private Vector3 _igniterRest;
        private bool _antennaOut;
        private float _antennaSeconds;
        private bool _antennaMoving;
        private bool _igniterFitted;
        private float _igniterSeconds;
        private bool _igniterMoving;
        private Vector3 _igniterPullLocal;

        /// <summary>The second segment has been extended (live or from a save).</summary>
        public bool AntennaExtended => _antennaOut;

        /// <summary>The igniter cartridge is fitted and shown (live or from a save).</summary>
        public bool IgniterFitted => _igniterFitted;

        /// <summary>The motion tuning in use.</summary>
        public GrowthSettings Settings => _tuning.Growth;

        public bool Initialize(GameContext context)
        {
            if (_tuning == null || _antennaSegment == null || _igniter == null || _turret == null)
            {
                Debug.LogError($"{nameof(RoverGrowth)}: needs RoverRigTuning, 'Antenna_Segment2', 'Cutter_Igniter' and " +
                               "'CutterTurret' (RoverModel; run Art/Rover then Rover/Rover).", this);
                return false;
            }

            _antennaRest = _antennaSegment.localPosition;
            _igniterRest = _igniter.localPosition;
            Vector3 pull = _turret.TransformDirection(_tuning.Growth.IgniterPullOut);
            _igniterPullLocal = _igniter.parent.InverseTransformDirection(pull).normalized;
            _igniter.gameObject.SetActive(false);
            _milestones = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestone);
            _systems = context.Events.Subscribe<RoverSystemChanged>(OnSystem);
            return true;
        }

        private void OnDestroy()
        {
            _milestones?.Dispose();
            _systems?.Dispose();
        }

        public void Tick(float deltaTime)
        {
            GrowthSettings settings = _tuning.Growth;
            if (_antennaMoving)
            {
                _antennaSeconds += deltaTime;
                PlaceAntenna(GrowthMotion.AntennaOut(settings, _antennaSeconds));
                _antennaMoving = !GrowthMotion.AntennaDone(settings, _antennaSeconds);
            }

            if (_igniterMoving)
            {
                _igniterSeconds += deltaTime;
                PlaceIgniter(GrowthMotion.IgniterOut(settings, _igniterSeconds));
                _igniterMoving = !GrowthMotion.IgniterDone(settings, _igniterSeconds);
            }
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            if (reached.Milestone != OpeningMilestone.FirstCharge || _antennaOut)
            {
                return;
            }

            _antennaOut = true;
            if (reached.Restored)
            {
                PlaceAntenna(_tuning.Growth.AntennaExtendMetres);
                return;
            }

            _antennaSeconds = 0f;
            _antennaMoving = true;
        }

        private void OnSystem(RoverSystemChanged changed)
        {
            if (changed.System != RoverSystem.Cutter || changed.State != RoverSystemState.Ok || _igniterFitted)
            {
                return;
            }

            _igniterFitted = true;
            _igniter.gameObject.SetActive(true);
            if (changed.Restored)
            {
                PlaceIgniter(0f);
                return;
            }

            _igniterSeconds = 0f;
            _igniterMoving = true;
            PlaceIgniter(GrowthMotion.IgniterOut(_tuning.Growth, 0f));
        }

        private void PlaceAntenna(float metres)
        {
            _antennaSegment.localPosition = _antennaRest + Vector3.up * metres;
        }

        private void PlaceIgniter(float metres)
        {
            _igniter.localPosition = _igniterRest + _igniterPullLocal * metres;
        }
    }
}
