using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Stands in for the Rover and World systems: registers <see cref="IRoverState"/> and <see cref="IWorldLayout"/>
    /// with telemetry the test sets directly.
    /// </summary>
    public sealed class FakeRoverSystem : MonoBehaviour, IGameSystem, IRoverState, IWorldLayout
    {
        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public Vector3 Velocity { get; set; }

        public float Speed { get; set; }

        public float NormalizedSpeed { get; set; }

        public Vector2 DriveInput { get; set; }

        public bool IsGrounded { get; set; } = true;

        public float AirTime { get; set; }

        public Vector3 GroundNormal { get; set; } = Vector3.up;

        public Vector3 BasePosition { get; set; }

        public Vector3 PeakPosition { get; set; } = new Vector3(0f, 120f, 280f);

        public Vector3 EarthDirection { get; set; } = Vector3.up;

        public void Initialize(GameContext context)
        {
            context.Register<IRoverState>(this);
            context.Register<IWorldLayout>(this);
        }
    }
}
