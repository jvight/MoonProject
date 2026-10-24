using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// The parts 07 grows in the opening, on RoverModel (docs/design/opening-v3-plan.md 2.6, 5.2a): the antenna's
    /// second segment (<see cref="RoverModelNodes.AntennaSegment2"/>) rests retracted in its mast and slides out at the
    /// first charge; the igniter cartridge (<see cref="RoverModelNodes.CutterIgniter"/>) is hidden until the cutter
    /// turns OK and then seats into its socket with a small click; the SSR-3 headlamp pods on
    /// <see cref="RoverModelNodes.BrowSocket"/> and the flank cell pods on
    /// <see cref="RoverModelNodes.PodSocketLeft"/>/<see cref="RoverModelNodes.PodSocketRight"/> stay hidden until fitted
    /// at the rack (<see cref="PartFitted"/>), then unfold and clamp home in ~0.6 s while their lenses light warm. A
    /// loaded game with the rack bay <see cref="RackBayState.Filled"/> shows them at rest, with no motion.
    /// Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverGrowth : MonoBehaviour
    {
        /// <summary>The rack bay id for the SSR-3 headlamp bar (opening v3 5.2a).</summary>
        public const string HeadlampBayId = "rack.headlamp";

        /// <summary>The rack bay id for the flank cell pack.</summary>
        public const string CellPackBayId = "rack.cell_pack";

        [Tooltip("Rig tuning (its Growth settings).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("RoverModel 'Antenna/Antenna_Segment2': rests retracted inside the mast.")]
        [SerializeField] private Transform _antennaSegment;

        [Tooltip("RoverModel 'CutterTurret/Cutter_Igniter': the cartridge in the turret socket.")]
        [SerializeField] private Transform _igniter;

        [Tooltip("RoverModel 'CutterTurret': the space the cartridge's pull-out direction is in.")]
        [SerializeField] private Transform _turret;

        [Tooltip("07's road light, which drives the SSR-3 brow pod lenses once fitted.")]
        [SerializeField] private RoverHeadlamp _headlamp;

        [Tooltip("Kit_SSR3Headlamp mounted under RoverModel 'Head/BrowSocket', hidden until fitted.")]
        [SerializeField] private Transform _headlampPod;

        [Tooltip("Kit_SSR3Headlamp 'Housing_L': unfolds and clamps inward on fit.")]
        [SerializeField] private Transform _headlampHousingLeft;

        [Tooltip("Kit_SSR3Headlamp 'Housing_R': unfolds and clamps inward on fit.")]
        [SerializeField] private Transform _headlampHousingRight;

        [Tooltip("Kit_CellPod mounted under RoverModel 'PodSocket_L', hidden until the cell pack is fitted.")]
        [SerializeField] private Transform _cellPodLeft;

        [Tooltip("Kit_CellPod mounted under RoverModel 'PodSocket_R', hidden until the cell pack is fitted.")]
        [SerializeField] private Transform _cellPodRight;

        [Tooltip("The ChargeBar_0..1 renderers on the left and right flank cell pods.")]
        [SerializeField] private Renderer[] _cellPodBars = Array.Empty<Renderer>();

        private const float WarmthEpsilon = 1e-4f;

        private GameContext _context;
        private IDisposable _milestones;
        private IDisposable _systems;
        private IDisposable _parts;
        private IDisposable _rack;
        private Vector3 _antennaRest;
        private Vector3 _igniterRest;
        private Vector3 _housingLeftRest;
        private Quaternion _housingLeftRotRest;
        private Vector3 _housingRightRest;
        private Quaternion _housingRightRotRest;
        private Vector3 _cellPodLeftRest;
        private Quaternion _cellPodLeftRotRest;
        private Vector3 _cellPodLeftOutDir;
        private Vector3 _cellPodRightRest;
        private Quaternion _cellPodRightRotRest;
        private Vector3 _cellPodRightOutDir;
        private HeadlampGlass[] _cellBarGlows = Array.Empty<HeadlampGlass>();
        private float _appliedCellWarmth = -1f;
        private bool _rackChecked;
        private bool _antennaOut;
        private float _antennaSeconds;
        private bool _antennaMoving;
        private bool _igniterFitted;
        private float _igniterSeconds;
        private bool _igniterMoving;
        private Vector3 _igniterPullLocal;
        private bool _headlampFitted;
        private float _headlampSeconds;
        private bool _headlampMoving;
        private bool _cellPodsFitted;
        private float _cellPodsSeconds;
        private bool _cellPodsMoving;

        /// <summary>The second segment has been extended (live or from a save).</summary>
        public bool AntennaExtended => _antennaOut;

        /// <summary>The igniter cartridge is fitted and shown (live or from a save).</summary>
        public bool IgniterFitted => _igniterFitted;

        /// <summary>The SSR-3 headlamp pods on BrowSocket are fitted and shown (live or from a save).</summary>
        public bool HeadlampFitted => _headlampFitted;

        /// <summary>The SSR-3 headlamp pods are currently unfolding/clamping into place.</summary>
        public bool HeadlampMoving => _headlampMoving;

        /// <summary>The flank cell pods on PodSocket_L/R are fitted and shown (live or from a save).</summary>
        public bool CellPodsFitted => _cellPodsFitted;

        /// <summary>The flank cell pods are currently clamping into place.</summary>
        public bool CellPodsMoving => _cellPodsMoving;

        /// <summary>The motion tuning in use.</summary>
        public GrowthSettings Settings => _tuning.Growth;

        public bool Initialize(GameContext context)
        {
            if (_tuning == null || _antennaSegment == null || _igniter == null || _turret == null ||
                _headlamp == null || _headlampPod == null || _headlampHousingLeft == null ||
                _headlampHousingRight == null || _cellPodLeft == null || _cellPodRight == null ||
                _cellPodBars == null || _cellPodBars.Length != RoverModelNodes.CellPodBarCount * 2)
            {
                Debug.LogError($"{nameof(RoverGrowth)}: needs RoverRigTuning, 'Antenna_Segment2', 'Cutter_Igniter', " +
                               "'CutterTurret', RoverHeadlamp, 'Kit_SSR3Headlamp' on BrowSocket and 'Kit_CellPod' on " +
                               "PodSocket_L/R (run Art/Rover then Rover/Rover).", this);
                return false;
            }

            _cellBarGlows = new HeadlampGlass[_cellPodBars.Length];
            for (int i = 0; i < _cellPodBars.Length; i++)
            {
                if (!HeadlampGlass.TryCreate(_cellPodBars[i], 0, out _cellBarGlows[i], out string problem))
                {
                    Debug.LogError($"{nameof(RoverGrowth)}: cell pod bar {i}: {problem}", this);
                    return false;
                }

                _cellBarGlows[i].Apply(0f);
            }

            _appliedCellWarmth = 0f;
            _context = context;
            _antennaRest = _antennaSegment.localPosition;
            _igniterRest = _igniter.localPosition;
            _housingLeftRest = _headlampHousingLeft.localPosition;
            _housingLeftRotRest = _headlampHousingLeft.localRotation;
            _housingRightRest = _headlampHousingRight.localPosition;
            _housingRightRotRest = _headlampHousingRight.localRotation;
            _cellPodLeftRest = _cellPodLeft.localPosition;
            _cellPodLeftRotRest = _cellPodLeft.localRotation;
            _cellPodLeftOutDir = _cellPodLeftRotRest * Vector3.right;
            _cellPodRightRest = _cellPodRight.localPosition;
            _cellPodRightRotRest = _cellPodRight.localRotation;
            _cellPodRightOutDir = _cellPodRightRotRest * Vector3.right;
            Vector3 pull = _turret.TransformDirection(_tuning.Growth.IgniterPullOut);
            _igniterPullLocal = _igniter.parent.InverseTransformDirection(pull).normalized;
            _igniter.gameObject.SetActive(false);
            _headlampPod.gameObject.SetActive(false);
            _cellPodLeft.gameObject.SetActive(false);
            _cellPodRight.gameObject.SetActive(false);
            _headlamp.SetPodWarmth(0f);
            _milestones = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestone);
            _systems = context.Events.Subscribe<RoverSystemChanged>(OnSystem);
            _parts = context.Events.Subscribe<PartFitted>(OnPartFitted);
            _rack = context.Events.Subscribe<RackChanged>(OnRackChanged);
            SyncRack();
            return true;
        }

        private void OnDestroy()
        {
            _milestones?.Dispose();
            _systems?.Dispose();
            _parts?.Dispose();
            _rack?.Dispose();
        }

        public void Tick(float deltaTime)
        {
            if (!_rackChecked)
            {
                _rackChecked = true;
                SyncRack();
            }

            if (!_antennaMoving && !_igniterMoving && !_headlampMoving && !_cellPodsMoving)
            {
                return;
            }

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

            if (_headlampMoving)
            {
                _headlampSeconds += deltaTime;
                bool done = GrowthMotion.HeadlampDone(settings, _headlampSeconds);
                PlaceHeadlamp(
                    done ? 0f : GrowthMotion.HeadlampOut(settings, _headlampSeconds),
                    done ? 0f : GrowthMotion.HeadlampOpenDegrees(settings, _headlampSeconds),
                    done ? 1f : GrowthMotion.HeadlampWarmth01(settings, _headlampSeconds));
                _headlampMoving = !done;
            }

            if (_cellPodsMoving)
            {
                _cellPodsSeconds += deltaTime;
                bool done = GrowthMotion.HeadlampDone(settings, _cellPodsSeconds);
                PlaceCellPods(
                    done ? 0f : GrowthMotion.HeadlampOut(settings, _cellPodsSeconds),
                    done ? 0f : GrowthMotion.HeadlampOpenDegrees(settings, _cellPodsSeconds),
                    done ? 1f : GrowthMotion.HeadlampWarmth01(settings, _cellPodsSeconds));
                _cellPodsMoving = !done;
            }
        }

        private void SyncRack()
        {
            if (_context == null || !_context.TryGet(out IRackStatus rack))
            {
                return;
            }

            for (int i = 0; i < rack.Count; i++)
            {
                if (rack.StateOf(i) != RackBayState.Filled)
                {
                    continue;
                }

                string id = rack.IdOf(i);
                if (string.Equals(id, HeadlampBayId, StringComparison.Ordinal))
                {
                    FitHeadlamp(restored: true);
                }
                else if (string.Equals(id, CellPackBayId, StringComparison.Ordinal))
                {
                    FitCellPods(restored: true);
                }
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

        private void OnPartFitted(PartFitted fitted)
        {
            if (fitted.Part == RoverKitPiece.LampBar || fitted.Socket == RoverSocket.Lamp)
            {
                FitHeadlamp(restored: false);
            }
            else if (fitted.Part == RoverKitPiece.BatterySpine)
            {
                FitCellPods(restored: false);
            }
        }

        private void OnRackChanged(RackChanged changed)
        {
            if (changed.State != RackBayState.Filled)
            {
                return;
            }

            if (_context != null && _context.TryGet(out IRackStatus rack))
            {
                if (changed.Bay < 0 || changed.Bay >= rack.Count)
                {
                    return;
                }

                string id = rack.IdOf(changed.Bay);
                if (string.Equals(id, HeadlampBayId, StringComparison.Ordinal))
                {
                    FitHeadlamp(restored: true);
                }
                else if (string.Equals(id, CellPackBayId, StringComparison.Ordinal))
                {
                    FitCellPods(restored: true);
                }

                return;
            }

            if (changed.Bay == 0)
            {
                FitHeadlamp(restored: true);
            }
            else if (changed.Bay == 1)
            {
                FitCellPods(restored: true);
            }
        }

        private void FitHeadlamp(bool restored)
        {
            if (_headlampFitted)
            {
                return;
            }

            _headlampFitted = true;
            _headlampPod.gameObject.SetActive(true);
            if (restored)
            {
                _headlampMoving = false;
                PlaceHeadlamp(0f, 0f, 1f);
                return;
            }

            _headlampSeconds = 0f;
            _headlampMoving = true;
            GrowthSettings settings = _tuning.Growth;
            PlaceHeadlamp(
                GrowthMotion.HeadlampOut(settings, 0f),
                GrowthMotion.HeadlampOpenDegrees(settings, 0f),
                GrowthMotion.HeadlampWarmth01(settings, 0f));
        }

        private void FitCellPods(bool restored)
        {
            if (_cellPodsFitted)
            {
                return;
            }

            _cellPodsFitted = true;
            _cellPodLeft.gameObject.SetActive(true);
            _cellPodRight.gameObject.SetActive(true);
            if (restored)
            {
                _cellPodsMoving = false;
                PlaceCellPods(0f, 0f, 1f);
                return;
            }

            _cellPodsSeconds = 0f;
            _cellPodsMoving = true;
            GrowthSettings settings = _tuning.Growth;
            PlaceCellPods(
                GrowthMotion.HeadlampOut(settings, 0f),
                GrowthMotion.HeadlampOpenDegrees(settings, 0f),
                GrowthMotion.HeadlampWarmth01(settings, 0f));
        }

        private void PlaceAntenna(float metres)
        {
            _antennaSegment.localPosition = _antennaRest + Vector3.up * metres;
        }

        private void PlaceIgniter(float metres)
        {
            _igniter.localPosition = _igniterRest + _igniterPullLocal * metres;
        }

        private void PlaceHeadlamp(float outMetres, float openDegrees, float warmth)
        {
            var offsetLeft = new Vector3(-outMetres, 0f, outMetres * 0.5f);
            var offsetRight = new Vector3(outMetres, 0f, outMetres * 0.5f);
            _headlampHousingLeft.localPosition = _housingLeftRest + offsetLeft;
            _headlampHousingLeft.localRotation =
                _housingLeftRotRest * Quaternion.Euler(0f, openDegrees, openDegrees * 0.4f);
            _headlampHousingRight.localPosition = _housingRightRest + offsetRight;
            _headlampHousingRight.localRotation =
                _housingRightRotRest * Quaternion.Euler(0f, -openDegrees, -openDegrees * 0.4f);
            _headlamp.SetPodWarmth(warmth);
        }

        private void PlaceCellPods(float outMetres, float openDegrees, float warmth)
        {
            _cellPodLeft.localPosition = _cellPodLeftRest + _cellPodLeftOutDir * outMetres;
            _cellPodLeft.localRotation = _cellPodLeftRotRest * Quaternion.Euler(0f, 0f, -openDegrees * 0.5f);
            _cellPodRight.localPosition = _cellPodRightRest + _cellPodRightOutDir * outMetres;
            _cellPodRight.localRotation = _cellPodRightRotRest * Quaternion.Euler(0f, 0f, -openDegrees * 0.5f);
            if (Mathf.Abs(warmth - _appliedCellWarmth) < WarmthEpsilon &&
                (warmth > 0f || _appliedCellWarmth == 0f) &&
                (warmth < 1f || _appliedCellWarmth == 1f))
            {
                return;
            }

            _appliedCellWarmth = warmth;
            for (int i = 0; i < _cellBarGlows.Length; i++)
            {
                _cellBarGlows[i].Apply(warmth);
            }
        }
    }
}
