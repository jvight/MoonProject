using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// An awake friend's little life, as a pure state machine: senses in, an intent out. While 07 is home it lives at
    /// the base — naps on its perch, flits around the lander, inspects the museum shelf. When 07 leaves it comes
    /// along, beside and above 07, never between the camera and 07, catching up (or softly reappearing) if it falls
    /// behind; with the spotter gift it drifts to undiscovered things within reach and pings softly over them. When 07
    /// comes home from away it circles 07 once in greeting.
    /// </summary>
    public sealed class FriendBehaviour
    {
        /// <summary>Seconds between two looks around for something to spot.</summary>
        private const float ScanInterval = 0.5f;

        /// <summary>Close enough (m) to a hover point or waypoint.</summary>
        private const float ArriveDistance = 1.2f;

        /// <summary>Close enough (m) to the perch to settle on it.</summary>
        private const float PerchSettle = 0.3f;

        /// <summary>Eye glow while napping on the perch.</summary>
        private const float NapEye = 0.3f;

        /// <summary>Share of cruise speed it potters about the base at.</summary>
        private const float HomeSpeed = 0.5f;

        /// <summary>Height (m) above the shelf's base it hovers to inspect it.</summary>
        private const float InspectHeight = 1.4f;

        /// <summary>07's body height (m): the camera keeps clear of this point.</summary>
        private const float RoverCentre = 0.8f;

        private readonly FriendTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly ISpotTargets _spots;
        private DeterministicRandom _random;
        private Mode _mode;
        private float _modeStart;
        private float _modeEnd;
        private float _nextHop;
        private Vector3 _waypoint;
        private float _greetAngle;
        private SpotTarget _spot;
        private bool _hovering;
        private float _hoverStart;
        private float _cooldownUntil;
        private float _nextScan;
        private bool _roverHome;
        private bool _wasAway;
        private Fade _fade;
        private float _visibility = 1f;

        public FriendBehaviour(FriendTuning tuning, ITerrainQuery terrain, ISpotTargets spots, int seed)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            _random = new DeterministicRandom(seed);
        }

        public enum Mode
        {
            Perched = 0,
            Flitting = 1,
            Inspecting = 2,
            Following = 3,
            Spotting = 4,
            Greeting = 5,
        }

        private enum Fade
        {
            None = 0,
            Out = 1,
            In = 2,
        }

        public Mode Current => _mode;

        /// <summary>True while 07 is at home (friends live at the base).</summary>
        public bool RoverHome => _roverHome;

        /// <summary>Just repaired: follows 07 (or settles at home if 07 is there).</summary>
        public void Wake(FriendSenses senses)
        {
            float distance = SurfaceRules.HorizontalDistance(senses.Rover, senses.Home);
            _roverHome = distance < _tuning.HomeRadius;
            _wasAway = !_roverHome;
            Begin(_roverHome ? Mode.Perched : Mode.Following, senses.Now);
        }

        /// <summary>Loaded from a save: it is on its perch and 07 has just woken at home.</summary>
        public void Settle(FriendSenses senses)
        {
            _roverHome = true;
            _wasAway = false;
            Begin(Mode.Perched, senses.Now);
        }

        public FriendIntent Step(FriendSenses senses, float deltaTime)
        {
            var intent = new FriendIntent { Rotors = 1f, Eye = 1f, Speed = _tuning.CruiseSpeed };
            TrackHome(senses, ref intent);
            switch (_mode)
            {
                case Mode.Greeting:
                    Greet(senses, ref intent);
                    break;
                case Mode.Following:
                    Follow(senses, ref intent);
                    break;
                case Mode.Spotting:
                    SpotOver(senses, ref intent);
                    break;
                case Mode.Flitting:
                    Flit(senses, ref intent);
                    break;
                case Mode.Inspecting:
                    Inspect(senses, ref intent);
                    break;
                default:
                    Perch(senses, ref intent);
                    break;
            }

            if (_mode != Mode.Perched)
            {
                Vector3 target = intent.Target;
                float floor = _terrain.SampleHeight(target.x, target.z) + _tuning.MinClearance;
                intent.Target = new Vector3(target.x, Mathf.Max(target.y, floor), target.z);
            }

            StepFade(senses, ref intent, deltaTime);
            return intent;
        }

        private void TrackHome(FriendSenses senses, ref FriendIntent intent)
        {
            float distance = SurfaceRules.HorizontalDistance(senses.Rover, senses.Home);
            if (distance > _tuning.AwayRadius)
            {
                _wasAway = true;
            }

            bool home = _roverHome ? distance < _tuning.LeaveRadius : distance < _tuning.HomeRadius;
            if (home && !_roverHome)
            {
                if (_wasAway)
                {
                    _wasAway = false;
                    Vector3 from = senses.Position - senses.Rover;
                    _greetAngle = Mathf.Atan2(from.z, from.x);
                    Begin(Mode.Greeting, senses.Now);
                    intent.Greeted = true;
                }
                else
                {
                    Begin(Mode.Perched, senses.Now);
                }
            }
            else if (!home && _roverHome)
            {
                Begin(Mode.Following, senses.Now);
            }

            _roverHome = home;
        }

        private void Greet(FriendSenses senses, ref FriendIntent intent)
        {
            float t = (senses.Now - _modeStart) / _tuning.GreetDuration;
            float angle = _greetAngle + 2f * Mathf.PI * Ease.InOutSine(t);
            Vector3 around = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _tuning.GreetRadius;
            intent.Target = senses.Rover + around + Vector3.up * _tuning.GreetHeight;
            intent.Look = senses.Rover;
            if (t >= 1f)
            {
                Begin(_roverHome ? Mode.Perched : Mode.Following, senses.Now);
            }
        }

        private void Follow(FriendSenses senses, ref FriendIntent intent)
        {
            Vector3 forward = senses.RoverForward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 point = senses.Rover - forward * _tuning.FollowBehind + right * _tuning.FollowSide +
                            Vector3.up * _tuning.FollowHeight;
            intent.Target = CameraClearance.Apply(point, senses.Rover + Vector3.up * RoverCentre, senses.Camera,
                _tuning.CameraClearAngle);
            intent.Look = senses.Rover + forward * _tuning.FollowBehind;
            bool far = Vector3.Distance(senses.Position, intent.Target) > _tuning.CatchUpDistance;
            intent.Speed = far ? _tuning.CatchUpSpeed : _tuning.CruiseSpeed;

            if (senses.Now >= _nextScan && senses.Now >= _cooldownUntil)
            {
                _nextScan = senses.Now + ScanInterval;
                if (_spots.TryFind(senses.Rover, _tuning.SpotRadius, out SpotTarget spot))
                {
                    _spot = spot;
                    _hovering = false;
                    Begin(Mode.Spotting, senses.Now);
                }
            }
        }

        private void SpotOver(FriendSenses senses, ref FriendIntent intent)
        {
            Vector3 hover = _spot.Position + Vector3.up * _tuning.SpotHover;
            intent.Target = hover;
            intent.Look = _spot.Position;
            if (SurfaceRules.HorizontalDistance(senses.Rover, senses.Position) > _tuning.SpotLeash)
            {
                _cooldownUntil = senses.Now + _tuning.SpotCooldown;
                Begin(Mode.Following, senses.Now);
                return;
            }

            if (!_hovering && Vector3.Distance(senses.Position, hover) < ArriveDistance)
            {
                _hovering = true;
                _hoverStart = senses.Now;
                intent.Spotted = true;
                intent.Spot = _spot;
            }

            if (!_hovering)
            {
                return;
            }

            float t = (senses.Now - _hoverStart) / _tuning.SpotDuration;
            intent.Cone = Ease.Hump(Mathf.Clamp01(t));
            if (t >= 1f)
            {
                _cooldownUntil = senses.Now + _tuning.SpotCooldown;
                Begin(Mode.Following, senses.Now);
            }
        }

        private void Perch(FriendSenses senses, ref FriendIntent intent)
        {
            intent.Target = senses.Perch;
            intent.Look = senses.Perch + senses.PerchForward;
            intent.Speed = _tuning.CruiseSpeed * HomeSpeed;
            if (Vector3.Distance(senses.Position, senses.Perch) < PerchSettle)
            {
                intent.Rotors = 0f;
                intent.Eye = NapEye;
            }

            if (senses.Now >= _modeEnd)
            {
                Begin(Mode.Flitting, senses.Now);
            }
        }

        private void Flit(FriendSenses senses, ref FriendIntent intent)
        {
            if (senses.Now >= _nextHop)
            {
                _nextHop = senses.Now + _tuning.FlitHop;
                Vector2 radius = _tuning.FlitRadius;
                Vector2 height = _tuning.FlitHeight;
                Vector3 direction = SurfaceRules.BearingDirection(_random.Range(0f, 360f));
                Vector3 point = senses.Home + direction * _random.Range(radius.x, radius.y);
                point.y = _terrain.SampleHeight(point.x, point.z) + _random.Range(height.x, height.y);
                _waypoint = point;
            }

            intent.Target = _waypoint;
            intent.Look = _waypoint;
            intent.Speed = _tuning.CruiseSpeed * HomeSpeed;
            if (senses.Now >= _modeEnd)
            {
                Begin(Mode.Inspecting, senses.Now);
            }
        }

        private void Inspect(FriendSenses senses, ref FriendIntent intent)
        {
            Vector3 front = senses.ShelfForward;
            front.y = 0f;
            front = front.sqrMagnitude > 1e-6f ? front.normalized : Vector3.forward;
            intent.Target = senses.Shelf + front * _tuning.InspectDistance + Vector3.up * InspectHeight;
            intent.Look = senses.Shelf + Vector3.up * InspectHeight;
            intent.Speed = _tuning.CruiseSpeed * HomeSpeed;
            if (senses.Now >= _modeEnd)
            {
                Begin(Mode.Perched, senses.Now);
            }
        }

        private void StepFade(FriendSenses senses, ref FriendIntent intent, float deltaTime)
        {
            float rate = deltaTime / Mathf.Max(0.01f, _tuning.ReappearFade);
            switch (_fade)
            {
                case Fade.Out:
                    _visibility -= rate;
                    if (_visibility <= 0f)
                    {
                        _visibility = 0f;
                        _fade = Fade.In;
                        intent.Teleport = true;
                    }

                    break;
                case Fade.In:
                    _visibility += rate;
                    if (_visibility >= 1f)
                    {
                        _visibility = 1f;
                        _fade = Fade.None;
                    }

                    break;
                default:
                    if (Vector3.Distance(senses.Position, intent.Target) > _tuning.ReappearDistance)
                    {
                        _fade = Fade.Out;
                    }

                    break;
            }

            intent.Visibility = Ease.InOutSine(_visibility);
        }

        private void Begin(Mode mode, float now)
        {
            _mode = mode;
            _modeStart = now;
            switch (mode)
            {
                case Mode.Perched:
                    Vector2 nap = _tuning.NapDuration;
                    _modeEnd = now + _random.Range(nap.x, nap.y);
                    break;
                case Mode.Flitting:
                    Vector2 flit = _tuning.FlitDuration;
                    _modeEnd = now + _random.Range(flit.x, flit.y);
                    _nextHop = now;
                    break;
                case Mode.Inspecting:
                    _modeEnd = now + _tuning.InspectDuration;
                    break;
                default:
                    _modeEnd = float.PositiveInfinity;
                    break;
            }
        }
    }
}
