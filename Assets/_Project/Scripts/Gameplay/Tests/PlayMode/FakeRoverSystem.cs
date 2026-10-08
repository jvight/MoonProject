using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Stands in for the Rover domain: a kinematic body the test moves directly, with an eye (TetherOrigin), a cargo
    /// socket, a cargo seat on its back (fitted with the Cargo Cradle ability) and a following camera. Registers
    /// IRoverState, IRoverRig, IViewCamera, IRoverAbilities, IRoverPlacement and IRoverCargoSeat, and records every
    /// gaze and hold-still request, every ability granted and every placement so tests can assert 07's attention,
    /// upgrades and radio-hops.
    /// </summary>
    public sealed class FakeRoverSystem : MonoBehaviour, IGameSystem, IRoverState, IRoverRig, IViewCamera,
        IRoverAbilities, IRoverPlacement, IRoverCargoSeat
    {
        private readonly Dictionary<object, GazeRequest> _gaze = new Dictionary<object, GazeRequest>();
        private readonly HashSet<object> _holders = new HashSet<object>();
        private readonly HashSet<RoverAbility> _abilities = new HashSet<RoverAbility>();
        private Vector3 _lastPosition;
        private Transform _eye;
        private Transform _cargo;
        private Transform _seat;
        private bool _registersCargoSeat;
        private Rigidbody _body;
        private Camera _camera;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        public Vector3 Velocity { get; private set; }

        public float Speed => new Vector3(Velocity.x, 0f, Velocity.z).magnitude;

        public float NormalizedSpeed => Mathf.Clamp01(Speed / 8f);

        public Vector2 DriveInput => Vector2.zero;

        public bool IsGrounded => true;

        public float AirTime => 0f;

        public Vector3 GroundNormal => Vector3.up;

        public Transform TetherOrigin => _eye;

        public Transform CargoSocket => _cargo;

        public Rigidbody PhysicsBody => _body;

        public Camera Camera => _camera;

        /// <summary>The rack's RelicSeat stand-in, on top of the shell at the back.</summary>
        public Transform CargoSeat => _seat;

        /// <summary>Takes the Cargo Cradle off 07 even though the ability is owned (a test of "never lost").</summary>
        public bool SeatRemoved { get; set; }

        public bool IsFitted => Has(RoverAbility.CargoCradle) && !SeatRemoved;

        Vector3 IRoverCargoSeat.Position => _seat.position;

        Quaternion IRoverCargoSeat.Rotation => _seat.rotation;

        /// <summary>Owners currently asking 07 to hold still.</summary>
        public int HoldStillCount => _holders.Count;

        /// <summary>Gaze requests ever made, in order (owner type name, priority).</summary>
        public List<string> GazeLog { get; } = new List<string>();

        /// <summary>Grant calls received (idempotent grants still count).</summary>
        public int AbilityGrants { get; private set; }

        /// <summary>PlaceAt calls received (one per radio-hop).</summary>
        public int Placements { get; private set; }

        /// <param name="cargoSeat">False for a rover that registers no cargo seat (a test of the boot check).</param>
        public static FakeRoverSystem Create(Vector3 position, float yaw, bool cargoSeat)
        {
            var host = new GameObject("FakeRover");
            host.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var rover = host.AddComponent<FakeRoverSystem>();
            rover._registersCargoSeat = cargoSeat;
            rover.Build();
            return rover;
        }

        public void Initialize(GameContext context)
        {
            context.Register<IRoverState>(this);
            context.Register<IRoverRig>(this);
            context.Register<IViewCamera>(this);
            context.Register<IRoverAbilities>(this);
            context.Register<IRoverPlacement>(this);
            if (_registersCargoSeat)
            {
                context.Register<IRoverCargoSeat>(this);
            }
        }

        public bool Has(RoverAbility ability)
        {
            return _abilities.Contains(ability);
        }

        public void Grant(RoverAbility ability)
        {
            AbilityGrants++;
            _abilities.Add(ability);
        }

        /// <summary>Teleports 07 (velocity reads zero afterwards).</summary>
        public void Place(Vector3 position, float yaw)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _lastPosition = position;
            Velocity = Vector3.zero;
        }

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            Placements++;
            Place(position, rotation.eulerAngles.y);
        }

        /// <summary>Drives 07 to <paramref name="position"/> (velocity follows from the motion each frame).</summary>
        public void MoveTo(Vector3 position, float yaw)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Points the view camera from <paramref name="from"/> at <paramref name="target"/>.</summary>
        public void Aim(Vector3 from, Vector3 target)
        {
            _camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from));
        }

        public bool TryGetGaze(object owner, out Vector3 position, out int priority)
        {
            if (_gaze.TryGetValue(owner, out GazeRequest request))
            {
                position = request.Position;
                priority = request.Priority;
                return true;
            }

            position = default;
            priority = -1;
            return false;
        }

        /// <summary>The request 07 would follow: highest priority wins.</summary>
        public bool TryGetWinningGaze(out Vector3 position, out int priority)
        {
            priority = -1;
            position = default;
            foreach (GazeRequest request in _gaze.Values)
            {
                if (request.Priority > priority)
                {
                    priority = request.Priority;
                    position = request.Position;
                }
            }

            return priority >= 0;
        }

        public void SetGazeTarget(object owner, Vector3 worldPosition, int priority)
        {
            if (!_gaze.ContainsKey(owner))
            {
                GazeLog.Add(owner.GetType().Name + ":" + priority);
            }

            _gaze[owner] = new GazeRequest(worldPosition, priority);
        }

        public void ClearGazeTarget(object owner)
        {
            _gaze.Remove(owner);
        }

        public void SetHoldStill(object owner, bool hold)
        {
            if (hold)
            {
                _holders.Add(owner);
            }
            else
            {
                _holders.Remove(owner);
            }
        }

        private void Build()
        {
            _lastPosition = transform.position;
            _eye = Child("TetherOrigin", new Vector3(0f, 1.3f, 0.7f));
            _cargo = Child("CargoSocket", new Vector3(0f, 1.05f, -0.4f));
            _seat = Child("RelicSeat", new Vector3(0f, 1.15f, -0.85f));

            var body = new GameObject("PhysicsSphere") { layer = Layers.Rover };
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.AddComponent<SphereCollider>().radius = 0.6f;
            _body = body.AddComponent<Rigidbody>();
            _body.isKinematic = true;

            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Shell";
            Destroy(shell.GetComponent<Collider>());
            shell.transform.SetParent(transform, false);
            shell.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            shell.transform.localScale = new Vector3(1.6f, 0.9f, 2.2f);

            var cameraHost = new GameObject("ViewCamera");
            cameraHost.transform.SetParent(transform, false);
            cameraHost.transform.localPosition = new Vector3(0f, 3.4f, -7.5f);
            cameraHost.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            _camera = cameraHost.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.043f, 0.055f, 0.165f);
            _camera.fieldOfView = 55f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 1000f;
            _camera.enabled = false;
        }

        private Transform Child(string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(transform, false);
            child.localPosition = localPosition;
            return child;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f)
            {
                Velocity = (transform.position - _lastPosition) / deltaTime;
            }

            _lastPosition = transform.position;
        }

        private readonly struct GazeRequest
        {
            public GazeRequest(Vector3 position, int priority)
            {
                Position = position;
                Priority = priority;
            }

            public Vector3 Position { get; }

            public int Priority { get; }
        }
    }
}
