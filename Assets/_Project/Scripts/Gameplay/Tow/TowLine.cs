using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's winch cable (docs/design/opening-v3-purpose.md R6, ruling 4: a physical cable, no beam). The tether key
    /// hooks the nearest towable load whose hitch is within reach of the winch and lets it go again; the cable also
    /// unhooks a load that has come home to the apron, and softly lets go of one that fell far behind. While it tows,
    /// each physics step the rope (<see cref="TowCable"/>) draws the load after 07, its skids bite so it never overruns
    /// 07, and the winch lifts a caught load free (<see cref="TowSnag"/>); the battery prices the tow (its speed cap and drive cost, read from
    /// <see cref="IsTowing"/>). Publishes <see cref="TowAttached"/> and <see cref="TowReleased"/>; Rover reads it as
    /// <see cref="ITowLine"/> to draw the cable. Allocation-free per step.
    /// </summary>
    public sealed class TowLine : ITowLine
    {
        private readonly List<ITowable> _loads = new List<ITowable>();
        private readonly TowSnag _snag = new TowSnag();
        private readonly EventBus _events;
        private readonly TowSettings _settings;
        private readonly Transform _winch;
        private readonly IRoverState _rover;
        private readonly Vector3 _home;
        private ITowable _load;

        /// <param name="winch">07's winch (IRoverRig.WinchOrigin): where the cable leaves 07.</param>
        /// <param name="home">The apron's centre: a load towed within the home radius of it has come home.</param>
        public TowLine(EventBus events, TowSettings settings, Transform winch, IRoverState rover, Vector3 home)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _winch = winch != null ? winch : throw new ArgumentNullException(nameof(winch));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _home = home;
            Length = settings.CableLength;
        }

        public bool IsTowing => _load != null;

        /// <summary>The id of the load on the cable, or empty.</summary>
        public string TowingId => _load != null ? _load.Id : string.Empty;

        public Vector3 LoadHitch => _load != null ? _load.Hitch : Vector3.zero;

        /// <summary>The cable's length now: its own, plus any slack the winch paid out for a caught load.</summary>
        public float Length { get; private set; }

        public float Strain01 { get; private set; }

        /// <summary>Times the winch has lifted the load on the cable free (it caught on the ground).</summary>
        public int Lifts => _snag.Lifts;

        /// <summary>A load the cable may hook; its tow id is its own.</summary>
        internal void Add(ITowable load)
        {
            if (load == null)
            {
                throw new ArgumentNullException(nameof(load));
            }

            if (Find(load.Id) != null)
            {
                throw new ArgumentException($"A towable load '{load.Id}' is on the winch's list already.",
                    nameof(load));
            }

            _loads.Add(load);
        }

        /// <summary>The hitch of the load <paramref name="towId"/>: where 07's winch hooks onto it.</summary>
        public bool TryFindHitch(string towId, out Vector3 hitch)
        {
            ITowable load = Find(towId);
            hitch = load != null ? load.Hitch : Vector3.zero;
            return load != null;
        }

        public bool TryFindLoad(string towId, out Transform root)
        {
            ITowable load = Find(towId);
            root = load?.Root;
            return load != null;
        }

        /// <summary>
        /// The load <paramref name="towId"/> stands home, on the apron off the cable, and is still in the world: a
        /// structure built on it may stand where it stands (<paramref name="root"/>).
        /// </summary>
        internal bool TryFindHome(string towId, out Transform root)
        {
            ITowable load = Find(towId);
            root = null;
            if (load == null || load == _load || !load.Root.gameObject.activeSelf ||
                SurfaceRules.HorizontalDistanceSquared(load.Root.position, _home) >
                _settings.HomeRadius * _settings.HomeRadius)
            {
                return false;
            }

            root = load.Root;
            return true;
        }

        /// <summary>
        /// A structure was placed on the load <paramref name="towId"/>: it is set into it at its pose and leaves the
        /// world (off the cable first, should it be on it).
        /// </summary>
        internal void TakeIn(string towId, Vector3 position, float yaw)
        {
            ITowable load = Find(towId);
            if (load == null)
            {
                return;
            }

            if (load == _load)
            {
                Release(TowRelease.Arrived);
            }

            load.Place(position, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Inverse(Quaternion.Euler(0f,
                load.Root.rotation.eulerAngles.y, 0f)) * load.Root.rotation);
            load.Root.gameObject.SetActive(false);
        }

        /// <summary>The structure on the load <paramref name="towId"/> is gone: the load stands where it stood.</summary>
        internal void GiveBack(string towId)
        {
            Find(towId)?.Root.gameObject.SetActive(true);
        }

        /// <summary>Where each load was left (a tow in progress is not kept).</summary>
        public TowSaveData Capture()
        {
            var data = new TowSaveData
            {
                ids = new string[_loads.Count],
                positions = new Vector3[_loads.Count],
                rotations = new Quaternion[_loads.Count],
            };
            for (int i = 0; i < _loads.Count; i++)
            {
                Transform root = _loads[i].Root;
                data.ids[i] = _loads[i].Id;
                data.positions[i] = root.position;
                data.rotations[i] = root.rotation;
            }

            return data;
        }

        /// <summary>A loaded game: each saved load is set down where it was left, off the cable.</summary>
        public void Restore(TowSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_load != null)
            {
                ITowable held = _load;
                _load = null;
                held.EndTow();
            }

            string[] ids = data.ids ?? Array.Empty<string>();
            Vector3[] positions = data.positions ?? Array.Empty<Vector3>();
            Quaternion[] rotations = data.rotations ?? Array.Empty<Quaternion>();
            for (int i = 0; i < ids.Length && i < positions.Length && i < rotations.Length; i++)
            {
                Find(ids[i])?.Place(positions[i], rotations[i]);
            }
        }

        /// <summary>One frame: the tether key (<paramref name="tetherPressed"/> this frame) hooks or lets go.</summary>
        public void Step(bool tetherPressed)
        {
            if (_load == null)
            {
                if (tetherPressed)
                {
                    Hook();
                }

                return;
            }

            if (tetherPressed)
            {
                Release(TowRelease.Player);
                return;
            }

            // The load itself, not its hitch, must be home: the same test a structure built on it reads.
            if (SurfaceRules.HorizontalDistanceSquared(_load.Root.position, _home) <=
                _settings.HomeRadius * _settings.HomeRadius)
            {
                Release(TowRelease.Arrived);
            }
        }

        /// <summary>One physics step: the rope pulls the load on the cable; loads off it settle.</summary>
        public void FixedStep(float deltaTime)
        {
            for (int i = 0; i < _loads.Count; i++)
            {
                if (_loads[i] != _load && _loads[i].Root.gameObject.activeSelf)
                {
                    _loads[i].Settle(deltaTime);
                }
            }

            if (_load == null)
            {
                return;
            }

            Vector3 winch = _winch.position;
            Vector3 hitch = _load.Hitch;
            Rigidbody body = _load.Body;
            Strain01 = TowCable.Strain01(winch, hitch, Length, _settings);
            Vector3 hitchVelocity = body.GetPointVelocity(hitch);
            float distance = Vector3.Distance(winch, hitch);
            float coming = distance > Mathf.Epsilon ? Vector3.Dot(hitchVelocity, (winch - hitch) / distance) : 0f;
            switch (_snag.Step(distance, Length, coming, deltaTime, _settings))
            {
                case TowSnagAction.LetGo:
                    Release(TowRelease.Snapped);
                    return;
                case TowSnagAction.LiftFree:
                    Vector3 toward = winch - hitch;
                    toward.y = 0f;
                    Vector3 lift = Vector3.up + (toward.sqrMagnitude > 1e-6f ? toward.normalized : Vector3.zero);
                    body.AddForce(lift.normalized * _settings.UnsnagLift, ForceMode.VelocityChange);
                    Length = Mathf.Min(Length + _settings.SlackStep, _settings.CableLength + _settings.MaxSlack);
                    break;
                default:
                    if (Length > _settings.CableLength && distance - Length <= _settings.SnagStretch &&
                        coming > -_settings.SnagSpeed)
                    {
                        Length = Mathf.Max(_settings.CableLength, Length - _settings.ReelInSpeed * deltaTime);
                    }

                    break;
            }

            Vector3 pull = TowCable.Pull(winch, hitch, hitchVelocity, _rover.Velocity, Length, -Physics.gravity.y,
                _settings);
            if (pull != Vector3.zero)
            {
                body.AddForceAtPosition(pull * body.mass, hitch, ForceMode.Force);
            }

            Vector3 bite = TowCable.SkidBite(body.linearVelocity, _rover.Speed,
                TowCable.CatchUp(distance, Length, _settings), _settings);
            if (bite != Vector3.zero)
            {
                body.AddForce(bite, ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// While nothing is on the cable, the hitch of the load the tether key would hook now (the nearest within hook
        /// reach of the winch): where the world teaches the key (<see cref="InteractionKind.Tether"/>).
        /// </summary>
        public bool TryGetHookable(out Vector3 hitch)
        {
            ITowable nearest = _load == null ? Nearest() : null;
            hitch = nearest != null ? nearest.Hitch : Vector3.zero;
            return nearest != null;
        }

        /// <summary>The nearest load whose hitch lies within the hook reach of the winch, or null.</summary>
        private ITowable Nearest()
        {
            Vector3 winch = _winch.position;
            float best = _settings.HookReach * _settings.HookReach;
            ITowable nearest = null;
            for (int i = 0; i < _loads.Count; i++)
            {
                float distance = SurfaceRules.HorizontalDistanceSquared(_loads[i].Hitch, winch);
                if (distance <= best && _loads[i].Root.gameObject.activeSelf)
                {
                    best = distance;
                    nearest = _loads[i];
                }
            }

            return nearest;
        }

        /// <summary>Hooks the nearest load not on the cable whose hitch lies within the hook reach of the winch.</summary>
        private void Hook()
        {
            ITowable nearest = Nearest();
            if (nearest == null)
            {
                return;
            }

            _load = nearest;
            _snag.Reset();
            Strain01 = 0f;
            Length = _settings.CableLength;
            _load.BeginTow();
            _events.Publish(new TowAttached(_load.Id, _load.Hitch, _load.Body.mass));
        }

        private ITowable Find(string towId)
        {
            for (int i = 0; i < _loads.Count; i++)
            {
                if (string.Equals(_loads[i].Id, towId, StringComparison.Ordinal))
                {
                    return _loads[i];
                }
            }

            return null;
        }

        private void Release(TowRelease reason)
        {
            ITowable load = _load;
            _load = null;
            Strain01 = 0f;
            load.EndTow();
            _events.Publish(new TowReleased(load.Id, load.Hitch, reason));
        }
    }
}
