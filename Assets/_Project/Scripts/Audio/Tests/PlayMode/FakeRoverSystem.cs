using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Stands in for the Rover, World and Gameplay systems: registers <see cref="IRoverState"/>,
    /// <see cref="IRoverRig"/>, <see cref="IWorldLayout"/>, <see cref="IRoverStillness"/> (counting up while
    /// <see cref="Resting"/>, as the Rover does), <see cref="ICutter"/> and <see cref="IRoverEquipment"/> with values
    /// the test sets directly.
    /// </summary>
    public sealed class FakeRoverSystem : MonoBehaviour, IGameSystem, IRoverState, IRoverRig, IWorldLayout,
        IRoverStillness
    {
        private Transform _tetherOrigin;

        /// <summary>The cutter tests change (tier, lamps).</summary>
        public FakeCutter Cutter { get; } = new FakeCutter();

        /// <summary>The call from the sky tests point 07 at (direction, state, name).</summary>
        public FakeCallSource Call { get; } = new FakeCallSource();

        /// <summary>The parts on 07 at boot (a loaded game); set before the rig boots.</summary>
        public AudioFakeEquipment Equipment { get; } = new AudioFakeEquipment();

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public Vector3 Velocity { get; set; }

        public float Speed { get; set; }

        /// <summary>While true, <see cref="StillSeconds"/> counts up each frame; false resets it (07 moved).</summary>
        public bool Resting { get; set; } = true;

        public float StillSeconds { get; set; }

        public float NormalizedSpeed { get; set; }

        public Vector2 DriveInput { get; set; }

        public bool IsGrounded { get; set; } = true;

        public float AirTime { get; set; }

        public Vector3 GroundNormal { get; set; } = Vector3.up;

        public Transform TetherOrigin => _tetherOrigin;

        public Transform CutterOrigin => _tetherOrigin;

        public Transform ScanOrigin => _tetherOrigin;

        public Transform WinchOrigin => _tetherOrigin;

        public Transform ScoopOrigin => _tetherOrigin;

        public Transform CargoSocket => transform;

        public Rigidbody PhysicsBody => null;

        public Vector3 BasePosition { get; set; }

        public Vector3 PeakPosition { get; set; } = new Vector3(0f, 120f, 280f);

        public Vector3 EarthDirection { get; set; } = Vector3.up;

        public void SetGazeTarget(object owner, Vector3 worldPosition, int priority)
        {
        }

        public void ClearGazeTarget(object owner)
        {
        }

        public void SetHoldStill(object owner, bool hold)
        {
        }

        private void Update()
        {
            StillSeconds = Resting ? StillSeconds + Time.deltaTime : 0f;
        }

        public void Initialize(GameContext context)
        {
            _tetherOrigin = new GameObject("TetherOrigin").transform;
            _tetherOrigin.SetParent(transform, false);
            context.Register<IRoverState>(this);
            context.Register<IRoverRig>(this);
            context.Register<IWorldLayout>(this);
            context.Register<IRoverStillness>(this);
            context.Register<ICutter>(Cutter);
            context.Register<IRoverEquipment>(Equipment);
            context.Register<ICallSource>(Call);
        }
    }
}
