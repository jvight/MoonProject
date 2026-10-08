using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Stands in for the Rover, World and Gameplay systems: registers <see cref="IRoverState"/>,
    /// <see cref="IRoverRig"/>, <see cref="IWorldLayout"/>, a two-friend <see cref="IFriendRoster"/> (Tilly, then
    /// Bell), Bell's <see cref="IRadioProgram"/>, a test canyon's <see cref="IWorldAnchors"/>,
    /// <see cref="IRoverStillness"/> (counting up while <see cref="Resting"/>, as the Rover does) and a relay network
    /// (<see cref="IStationReach"/>: home at the base, masts the test lights) with values the test sets directly.
    /// </summary>
    public sealed class FakeRoverSystem : MonoBehaviour, IGameSystem, IRoverState, IRoverRig, IWorldLayout,
        IFriendRoster, IRoverStillness
    {
        private Transform _tetherOrigin;

        public FakeFriend Tilly { get; } = new FakeFriend("tilly");

        public FakeFriend Bell { get; } = new FakeFriend("bell");

        /// <summary>The radio program: the dial locked on Lumen After Dark, no tapes owned (3 exist).</summary>
        public FakeRadioProgram Program { get; } = new FakeRadioProgram(3);

        /// <summary>The relay network: home at the origin (the base) plus the test's masts.</summary>
        public FakeStationReach Reach { get; } = new FakeStationReach(Vector3.zero);

        public int Count => 2;

        public IFriendState Get(int index)
        {
            return index == 0 ? Tilly : Bell;
        }

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
            context.Register<IFriendRoster>(this);
            context.Register<IRadioProgram>(Program);
            context.Register<IWorldAnchors>(new FakeWorldAnchors());
            context.Register<IRoverStillness>(this);
            context.Register<IStationReach>(Reach);
        }
    }
}
