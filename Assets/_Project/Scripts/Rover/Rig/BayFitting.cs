using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// The install moment in Kenji's Rover Bay (docs/features/M3-14, VISION ruling 14: 07 has no hands), one piece at
    /// a time, stepped every frame:
    /// <list type="number">
    /// <item>The bay's guides ease 07 onto the turntable's centre and arrival facing while the turntable eases back
    /// from any turn the last fitting left (07 is held by the bay from here); each part already hangs from its arm's
    /// tip, taken from the rack under the roof.</item>
    /// <item>The turntable turns 07 if an arm needs a socket brought round, or to keep it in view of the open front
    /// (<see cref="BayPlanner"/>).</item>
    /// <item>Each part's arm swings from its folded rest to just above its socket, the part growing from the size it
    /// showed in the rack.</item>
    /// <item>The arm lowers the part straight onto its socket, tracking it live, so it is fitted exactly there
    /// (<see cref="BayCue.Landed"/>), and holds it there a moment, as for a weld.</item>
    /// <item>The arm rises off the piece and folds back to rest while the turntable turns 07 to show the piece (it
    /// stays turned, so 07 leaves showing it); then 07 is free (<see cref="BayCue.Finished"/>).</item>
    /// </list>
    /// Belly pieces ride the floor arm up through the turntable instead. A piece no arm can reach is a contract bug:
    /// <see cref="Begin"/> refuses it and logs, and it is never dropped the last stretch. Allocation-free per frame.
    /// </summary>
    public sealed class BayFitting : IBayFitView
    {
        public const int MaxParts = 2;

        private readonly BayFitSettings _settings;
        private readonly RoverController _rover;
        private readonly Vector3[] _sockets = new Vector3[MaxParts];
        private readonly int[] _armOf = new int[MaxParts];
        private readonly Transform[] _parts = new Transform[MaxParts];
        private readonly Vector3[] _rest = new Vector3[MaxParts];
        private readonly BayArmPose[] _ready = new BayArmPose[MaxParts];
        private readonly BayArmPose[] _held = new BayArmPose[MaxParts];

        private IRoverBay _bay;
        private BayArm[] _arms;
        private Transform _disc;
        private Quaternion _discRest;
        private Phase _phase;
        private float _elapsed;
        private int _count;
        private bool _floor;
        private Vector3 _floorRest;
        private Vector3 _tipRest;
        private Vector3 _centre;
        private Vector3 _up;
        private Vector3 _parkedBody;
        private float _parkedHeading;
        private Vector3 _startBody;
        private float _startHeading;
        private float _turn;
        private float _angle;
        private float _leftTurn;
        private float _showFrom;
        private float _showSwing;

        private enum Phase
        {
            Idle,
            Centre,
            Turn,
            Reach,
            Descend,
            Weld,
            Rise,
            Fold,
        }

        public BayFitting(BayFitSettings settings, RoverController rover)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rover = rover != null ? rover : throw new ArgumentNullException(nameof(rover));
        }

        public bool Active => _phase != Phase.Idle;

        public bool Working => Active && _phase != Phase.Fold;

        public bool Belly => _floor;

        public Vector3 Socket { get; private set; }

        public Vector3 Shoulder { get; private set; }

        public Vector3 Centre => _centre;

        public Vector3 Facing { get; private set; }

        public Vector3 Front { get; private set; }

        public RoverKitPiece Piece { get; private set; }

        /// <summary>The turntable's turn (deg from 07's arrival facing), kept from the last show turn.</summary>
        public float TurntableAngle => _angle;

        /// <summary>
        /// Starts fitting <paramref name="piece"/>: its <paramref name="count"/> <paramref name="parts"/> (each a
        /// child of its socket, standing at its rest pose there) carried by the bay's arms, or by its floor arm when
        /// <paramref name="belly"/>, the tip holding each part's pivot, and set back exactly on that rest pose.
        /// False (logged) when the bay cannot reach a socket: a contract bug.
        /// </summary>
        public bool Begin(IRoverBay bay, BayArm[] arms, RoverKitPiece piece, Transform[] parts, int count, bool belly,
            UnityEngine.Object context)
        {
            _bay = bay ?? throw new ArgumentNullException(nameof(bay));
            _arms = arms ?? throw new ArgumentNullException(nameof(arms));
            if (count < 1 || count > MaxParts || parts == null || parts.Length < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A piece has one or two parts.");
            }

            Piece = piece;
            _count = count;
            _floor = belly;
            for (int i = 0; i < count; i++)
            {
                _parts[i] = parts[i];
                _rest[i] = parts[i].localPosition;
            }

            if (_disc != bay.Turntable)
            {
                _disc = bay.Turntable;
                _discRest = _disc.rotation;
                _angle = 0f;
            }

            _up = _discRest * Vector3.up;
            _centre = bay.TurntablePosition;
            _parkedBody = _centre + _up * _rover.SphereRadius;
            _parkedHeading = RoverPlacementMath.Yaw(bay.TurntableRotation);
            _startBody = _rover.SpherePosition;
            _startHeading = _rover.Heading;
            if (!Plan(context))
            {
                _phase = Phase.Idle;
                return false;
            }

            _leftTurn = Mathf.DeltaAngle(0f, _angle);
            _elapsed = 0f;
            _phase = Phase.Centre;
            _rover.HoldPose(_startBody, _startHeading);
            return true;
        }

        /// <summary>
        /// Plans arms from where the sockets will be once 07 is parked: they ride with 07, so they are shifted from
        /// 07's current pose onto the parked one.
        /// </summary>
        private bool Plan(UnityEngine.Object context)
        {
            Quaternion toParked = Quaternion.AngleAxis(Mathf.DeltaAngle(_startHeading, _parkedHeading), _up);
            for (int i = 0; i < _count; i++)
            {
                _sockets[i] = _parkedBody + toParked * (_parts[i].position - _startBody);
            }

            bool planned;
            if (_floor)
            {
                planned = BayPlanner.TryPlanFloor(_bay.FloorLift, _bay.FloorTip, _centre, _up, _sockets[0],
                    _settings.FloorAlign, out _turn, out _);
                _floorRest = _bay.FloorLift.localPosition;
                _tipRest = _bay.FloorLift.parent.InverseTransformPoint(_bay.FloorTip.position);
            }
            else
            {
                Vector3 front = Quaternion.AngleAxis(_parkedHeading, _up) * Vector3.back;
                planned = BayPlanner.TryPlan(_arms, _centre, _up, front, _sockets, _count, _settings.Hover, _armOf,
                    out _turn);
            }

            if (!planned)
            {
                Debug.LogError($"{nameof(BayFitting)}: the Rover Bay cannot reach the {Piece} socket(s) on a parked "
                    + $"07 (contract bug: {BayArm.Contract}).", context);
                return false;
            }

            Socket = BayPlanner.Turned(_sockets[0], _centre, _up, _turn);
            Shoulder = _floor ? _bay.FloorTip.position : _arms[_armOf[0]].Shoulder;
            Facing = Quaternion.AngleAxis(_parkedHeading + _turn, _up) * Vector3.forward;
            Front = Quaternion.AngleAxis(_parkedHeading, _up) * Vector3.back;
            return true;
        }

        /// <summary>Advances the moment; reports the frame the piece is fitted and the frame it is over.</summary>
        public BayCue Step(float deltaTime)
        {
            if (_phase == Phase.Idle || deltaTime <= 0f)
            {
                return BayCue.None;
            }

            _elapsed += deltaTime;
            switch (_phase)
            {
                case Phase.Centre:
                    StepCentre();
                    return BayCue.None;
                case Phase.Turn:
                    StepTurn();
                    return BayCue.None;
                case Phase.Reach:
                    StepReach();
                    return BayCue.None;
                case Phase.Descend:
                    return StepDescend();
                case Phase.Weld:
                    StepWeld();
                    return BayCue.None;
                case Phase.Rise:
                    StepRise();
                    return BayCue.None;
                default:
                    return StepFold();
            }
        }

        private float Progress(float seconds)
        {
            return Smoothing.SmoothStep(0f, 1f, seconds > 0f ? _elapsed / seconds : 1f);
        }

        private void Next(Phase phase)
        {
            _phase = phase;
            _elapsed = 0f;
        }

        private void StepCentre()
        {
            float t = Progress(_settings.CentreSeconds);
            float heading = Mathf.LerpAngle(_startHeading, _parkedHeading, t);
            _rover.HoldPose(Vector3.Lerp(_startBody, _parkedBody, t), heading);
            _angle = _leftTurn * (1f - t);
            _disc.rotation = Quaternion.AngleAxis(_angle, _up) * _discRest;
            Carry(0f);
            if (t < 1f)
            {
                return;
            }

            if (Mathf.Approximately(_turn, 0f))
            {
                BeginReach();
            }
            else
            {
                Next(Phase.Turn);
            }
        }

        private void StepTurn()
        {
            float t = Progress(_settings.TurnSeconds(_turn));
            SetAngle(_turn * t);
            Carry(0f);
            if (t >= 1f)
            {
                BeginReach();
            }
        }

        /// <summary>Turns the turntable, with 07 on it, to <paramref name="angle"/> from the arrival facing.</summary>
        private void SetAngle(float angle)
        {
            _angle = angle;
            Quaternion turn = Quaternion.AngleAxis(angle, _up);
            _disc.rotation = turn * _discRest;
            _rover.HoldPose(_centre + turn * (_parkedBody - _centre), _parkedHeading + angle);
        }

        /// <summary>07 parked and turned: the arm poses that bring each part just above its socket.</summary>
        private void BeginReach()
        {
            Next(Phase.Reach);
            if (_floor)
            {
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                BayArm arm = _arms[_armOf[i]];
                if (!Solve(arm, LiveSocket(i) + _up * _settings.Hover, out _ready[i]))
                {
                    _ready[i] = arm.Rest;
                }
            }
        }

        private void StepReach()
        {
            float t = Progress(_settings.ReachSeconds);
            if (_floor)
            {
                SetLift(t * Mathf.Max(0f, Rise() - _settings.Hover));
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    BayArm arm = _arms[_armOf[i]];
                    _held[i] = BayArmIk.Blend(arm.Rest, _ready[i], t);
                    arm.Apply(_held[i]);
                }
            }

            Carry(t);
            if (t >= 1f)
            {
                Next(Phase.Descend);
            }
        }

        private BayCue StepDescend()
        {
            float t = Progress(_settings.DescendSeconds);
            Hold(1f - t);
            Carry(1f);
            if (t < 1f)
            {
                return BayCue.None;
            }

            for (int i = 0; i < _count; i++)
            {
                _parts[i].localPosition = _rest[i];
            }

            Next(Phase.Weld);
            return BayCue.Landed;
        }

        private void StepWeld()
        {
            Hold(0f);
            if (_elapsed >= _settings.WeldSeconds)
            {
                Next(Phase.Rise);
            }
        }

        private void StepRise()
        {
            float t = Progress(0.5f * _settings.LiftSeconds);
            Hold(t);
            if (t < 1f)
            {
                return;
            }

            Next(Phase.Fold);
            _showFrom = _angle;
            _showSwing = Mathf.DeltaAngle(_angle, _settings.Show(Piece));
        }

        /// <summary>The arms fold back (the floor arm sinks) as the turntable turns 07 to show the piece.</summary>
        private BayCue StepFold()
        {
            float fold = Progress(0.5f * _settings.LiftSeconds);
            if (_floor)
            {
                SetLift(Mathf.Lerp(Mathf.Max(0f, Rise() - _settings.Hover), 0f, fold));
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    BayArm arm = _arms[_armOf[i]];
                    arm.Apply(BayArmIk.Blend(_held[i], arm.Rest, fold));
                }
            }

            float show = Progress(_settings.TurnSeconds(_showSwing));
            SetAngle(_showFrom + _showSwing * show);
            if (fold < 1f || show < 1f)
            {
                return BayCue.None;
            }

            _phase = Phase.Idle;
            _rover.ReleasePose();
            return BayCue.Finished;
        }

        /// <summary>
        /// Each arm's tip (or the floor arm's) <paramref name="above"/> of the hover height over its socket, tracking
        /// the socket live so the part meets it exactly.
        /// </summary>
        private void Hold(float above)
        {
            if (_floor)
            {
                SetLift(Rise() - _settings.Hover * above);
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                BayArm arm = _arms[_armOf[i]];
                if (Solve(arm, LiveSocket(i) + _up * (_settings.Hover * above), out BayArmPose pose))
                {
                    _held[i] = pose;
                    arm.Apply(pose);
                }
            }
        }

        /// <summary>
        /// The pose putting <paramref name="arm"/>'s tip on <paramref name="target"/>. The plan proved the sockets
        /// reachable, so failing here is a contract bug: logged, and the arm keeps its pose.
        /// </summary>
        private bool Solve(BayArm arm, Vector3 target, out BayArmPose pose)
        {
            if (BayArmIk.TrySolve(arm.Geometry, arm.ToShoulder(target), out pose))
            {
                return true;
            }

            Debug.LogError($"{nameof(BayFitting)}: a Rover Bay arm cannot reach {target} while fitting {Piece} "
                + $"(contract bug: {BayArm.Contract}).");
            return false;
        }

        /// <summary>Where part <paramref name="index"/>'s pivot rests on 07 right now (world).</summary>
        private Vector3 LiveSocket(int index)
        {
            return _parts[index].parent.TransformPoint(_rest[index]);
        }

        /// <summary>How far (lift units) the floor arm must rise to set its part on the belly socket now.</summary>
        private float Rise()
        {
            return _bay.FloorLift.parent.InverseTransformPoint(LiveSocket(0)).y - _tipRest.y;
        }

        private void SetLift(float rise)
        {
            _bay.FloorLift.localPosition = _floorRest + Vector3.up * Mathf.Max(0f, rise);
        }

        /// <summary>
        /// Each part's pivot held at its arm's tip, or on the floor arm's platform, sliding across it (by
        /// <paramref name="under"/>, 0..1) to right under its socket once 07 is parked; it keeps its rest rotation on
        /// its socket, so it arrives level.
        /// </summary>
        private void Carry(float under)
        {
            if (_floor)
            {
                Vector3 tip = _bay.FloorTip.position;
                Vector3 aside = LiveSocket(0) - tip;
                aside -= Vector3.Project(aside, _bay.FloorLift.parent.up);
                _parts[0].position = tip + aside * under;
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                _parts[i].position = _arms[_armOf[i]].TipEnd;
            }
        }
    }
}
