using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A small flying friend's body (Tilly, docs/features/M3-02-friends-tilly.md): its broken and repaired rigs, the
    /// boot-up after the stitching (the eye flickers awake, the rotors spin up, it wobbles into the air and turns to
    /// look at 07), and its awake flight driven by <see cref="FriendBehaviour"/>: home life on its perch, following
    /// 07, spotting with its little light cone, greeting 07 coming home. Allocation-free per frame.
    /// </summary>
    internal sealed class DroneBody : IFriendBody
    {
        /// <summary>Rotor effort while hovering in place (Core's IFriendState.RotorSpeed scale).</summary>
        private const float HoverEffort = 0.4f;

        // Shiver frequencies (radians per second) on two axes: unrelated, so it reads as a shudder, not a wobble.
        private const float ShiverPitchRate = 37f;
        private const float ShiverRollRate = 29f;
        private const float WobbleRate = 9f;

        /// <summary>The roll of the lift-off wobble is this share of its pitch.</summary>
        private const float WobbleRoll = 0.5f;

        /// <summary>Visibility below which a fading friend is hidden instead of scaled to a speck.</summary>
        private const float MinVisible = 0.01f;

        private readonly FriendTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly IRoverState _rover;
        private readonly IViewCamera _view;
        private readonly HomeBase _home;
        private readonly Transform _perch;
        private readonly FriendRig _broken;
        private readonly FriendRig _repaired;
        private readonly FriendBehaviour _behaviour;
        private readonly RepairSequence _sequence;
        private readonly GlowRenderer _cone;
        private readonly FriendMotion _motion = new FriendMotion();
        private readonly Vector3 _site;
        private readonly Quaternion _brokenRotation;
        private readonly int _phase;
        private bool _swapped;
        private float _rotorLevel;
        private float _eyeLevel;
        private float _coneLevel;
        private Vector3 _liftPosition;
        private bool _lead;
        private Vector3 _leadToward;
        private bool _fetch;
        private Vector3 _fetchToward;
        private float _fetchSpeed;
        private float _fetchHeight;
        private float _liftYaw;

        public DroneBody(FriendTuning tuning, ITerrainQuery terrain, IRoverState rover, IViewCamera view,
            HomeBase home, Transform perch, FriendRig broken, FriendRig repaired, FriendBehaviour behaviour,
            RepairSequence sequence, GlowRenderer cone, Vector3 site, int phase)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _perch = perch != null ? perch : throw new ArgumentNullException(nameof(perch));
            _broken = broken ?? throw new ArgumentNullException(nameof(broken));
            _repaired = repaired ?? throw new ArgumentNullException(nameof(repaired));
            _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _cone = cone ?? throw new ArgumentNullException(nameof(cone));
            _site = site;
            _phase = phase;
            _brokenRotation = broken.Root.rotation;
        }

        public Vector3 Position => _swapped ? _repaired.Root.position : _site;

        public Vector3 AnswerPosition => (_swapped ? _repaired : _broken).Eye.position;

        public Vector3 BeamTarget => _broken.Visible ? _broken.TetherPoint.position : _repaired.TetherPoint.position;

        public FriendActivity Activity { get; private set; } = FriendActivity.Dormant;

        public float Motor { get; private set; }

        public bool IsHome => _swapped && _behaviour.RoverHome;

        public float RepairDuration => _sequence.Duration;

        public bool Beaming(float t)
        {
            return _sequence.Stitching(t);
        }

        public void SetAnswerBlink(float intensity)
        {
            if (!_swapped)
            {
                _broken.SetEye(intensity);
            }
        }

        public void SetLamp(int lamp, float intensity)
        {
            (_swapped ? _repaired : _broken).SetLamp(lamp, intensity);
        }

        public void StepBroken(float now, float deltaTime)
        {
            Activity = FriendActivity.Dormant;
            Motor = 0f;
            StepCone(deltaTime, false);
        }

        public void StepRepair(float t, float now, float deltaTime)
        {
            Activity = FriendActivity.Repairing;
            Motor = HoverEffort * _sequence.Rotors(t);
            StepCone(deltaTime, false);
            if (_sequence.Stitching(t))
            {
                float shiver = _tuning.Shiver * Ease.InOutSine(_sequence.StitchProgress(t));
                _broken.Root.rotation = _brokenRotation * Quaternion.Euler(
                    Mathf.Sin(now * ShiverPitchRate) * shiver, 0f, Mathf.Sin(now * ShiverRollRate) * shiver);
                return;
            }

            if (!_swapped)
            {
                Swap();
            }

            float lift = _sequence.Lift(t);
            Vector3 toRover = _rover.Position - _site;
            float roverYaw = Mathf.Atan2(toRover.x, toRover.z) * Mathf.Rad2Deg;
            _liftYaw = Mathf.LerpAngle(_brokenRotation.eulerAngles.y, roverYaw, _sequence.Look(t));
            float wobble = _tuning.LiftWobble * Ease.Hump(lift) * Mathf.Sin(now * WobbleRate);
            _liftPosition = _site + Vector3.up * (_tuning.HoverHeight * lift);
            _repaired.Root.SetPositionAndRotation(_liftPosition,
                Quaternion.Euler(wobble, _liftYaw, wobble * WobbleRoll));
            _repaired.BlendPose(lift);
            _repaired.SetEye(_sequence.Eye(t));
            _repaired.SpinRotors(_tuning.RotorSpeed * _sequence.Rotors(t) * deltaTime);
        }

        public void Wake(float now)
        {
            _repaired.BlendPose(1f);
            _motion.Teleport(_liftPosition, _liftYaw);
            _rotorLevel = 1f;
            _eyeLevel = 1f;
            _behaviour.Wake(Senses(now));
        }

        public void SettleAtHome(float now)
        {
            _swapped = true;
            _broken.Visible = false;
            _repaired.Visible = true;
            _repaired.BlendPose(1f);
            Vector3 perch = _perch.position;
            _motion.Teleport(perch, _perch.eulerAngles.y);
            _repaired.Root.SetPositionAndRotation(perch, _motion.Rotation);
            _rotorLevel = 0f;
            _eyeLevel = 1f;
            _behaviour.Settle(Senses(now));
            Activity = FriendActivity.Home;
            Motor = 0f;
        }

        public FriendBeat StepAwake(float now, float deltaTime)
        {
            FriendIntent intent = _behaviour.Step(Senses(now), deltaTime);
            if (intent.Teleport)
            {
                _motion.Teleport(intent.Target, _motion.Yaw);
            }

            Vector3 target = intent.Target;
            float floor = intent.Rotors > 0f ? _terrain.SampleHeight(target.x, target.z) + _tuning.MinClearance
                : float.MinValue;
            _motion.Step(target, intent.Look, intent.Speed, floor, _tuning, deltaTime);
            _rotorLevel = Damp.Toward(_rotorLevel, intent.Rotors, _tuning.RotorEase, deltaTime);
            _eyeLevel = Damp.Toward(_eyeLevel, intent.Eye, _tuning.RotorEase, deltaTime);
            _coneLevel = Damp.Toward(_coneLevel, intent.Cone, _tuning.RotorEase, deltaTime);
            float bob = Mathf.Sin(now * 2f * Mathf.PI * _tuning.BobFrequency + _phase) * _tuning.Bob * _rotorLevel;
            Transform root = _repaired.Root;
            root.SetPositionAndRotation(_motion.Position + Vector3.up * bob, _motion.Rotation);
            _repaired.Visible = intent.Visibility > MinVisible;
            root.localScale = Vector3.one * Mathf.Max(MinVisible, intent.Visibility);
            _repaired.SpinRotors(_tuning.RotorSpeed * _rotorLevel * deltaTime);
            _repaired.SetEye(_eyeLevel);
            StepCone(deltaTime, true);

            Activity = ActivityOf(_behaviour.Current, _rotorLevel);
            float dash = Mathf.Clamp01(_motion.Velocity.magnitude / Mathf.Max(0.01f, _tuning.CatchUpSpeed));
            Motor = _rotorLevel * Mathf.Lerp(HoverEffort, 1f, dash);
            return new FriendBeat(intent.Greeted, intent.Spotted, intent.Spot);
        }

        private void Swap()
        {
            _swapped = true;
            _repaired.Root.SetPositionAndRotation(_site, _brokenRotation);
            _repaired.CapturePoseFrom(_broken);
            _repaired.BlendPose(0f);
            _repaired.Visible = true;
            _broken.Visible = false;
        }

        private void StepCone(float deltaTime, bool awake)
        {
            if (!awake)
            {
                _coneLevel = Damp.Toward(_coneLevel, 0f, _tuning.RotorEase, deltaTime);
            }

            _cone.Apply(_coneLevel * _tuning.SpotConeGlow);
            if (!_cone.Renderer.enabled)
            {
                return;
            }

            Vector3 from = _repaired.Root.position;
            float ground = _terrain.SampleHeight(from.x, from.z);
            float length = Mathf.Max(0.1f, from.y - ground);
            Transform host = _cone.Renderer.transform;
            host.SetPositionAndRotation(from, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            host.localScale = new Vector3(_tuning.SpotConeRadius, _tuning.SpotConeRadius, length);
        }

        /// <summary>
        /// Leads a crawl home toward <paramref name="toward"/> while <paramref name="lead"/> is true (the battery sends
        /// it out, docs/features/M3-20); false lets it go back to its own life.
        /// </summary>
        public void Lead(bool lead, Vector3 toward)
        {
            _lead = lead;
            _leadToward = toward;
        }

        /// <summary>
        /// Sends it on a fetch toward <paramref name="toward"/> while <paramref name="fetch"/> is true (a waiting
        /// bundle, then home's shelves; docs/features/M3-25 T3); false lets it go back to its own life. Leading a crawl
        /// home comes first.
        /// </summary>
        public void Fetch(bool fetch, Vector3 toward, float speed, float height)
        {
            _fetch = fetch;
            _fetchToward = toward;
            _fetchSpeed = speed;
            _fetchHeight = height;
        }

        /// <summary>True while it leads 07 home, which a fetch must give way to.</summary>
        public bool Leading => _behaviour.Current == FriendBehaviour.Mode.Leading;

        private FriendSenses Senses(float now)
        {
            return new FriendSenses
            {
                Now = now,
                Position = _motion.Position,
                Rover = _rover.Position,
                RoverForward = _rover.Rotation * Vector3.forward,
                Camera = _view.Camera.transform.position,
                Home = _home.LanderPosition,
                Perch = _perch.position,
                PerchForward = _perch.forward,
                Inspect = _home.InspectPosition,
                InspectForward = _home.InspectForward,
                Lead = _lead,
                LeadToward = _leadToward,
                Fetch = _fetch,
                FetchToward = _fetchToward,
                FetchSpeed = _fetchSpeed,
                FetchHeight = _fetchHeight,
            };
        }

        private static FriendActivity ActivityOf(FriendBehaviour.Mode mode, float rotorLevel)
        {
            switch (mode)
            {
                case FriendBehaviour.Mode.Following:
                case FriendBehaviour.Mode.Greeting:
                case FriendBehaviour.Mode.Leading:
                case FriendBehaviour.Mode.Fetching:
                    return FriendActivity.Following;
                case FriendBehaviour.Mode.Spotting:
                    return FriendActivity.Spotting;
                case FriendBehaviour.Mode.Perched:
                    return rotorLevel < GlowRenderer.VisibleThreshold ? FriendActivity.Napping : FriendActivity.Home;
                default:
                    return FriendActivity.Home;
            }
        }
    }
}
