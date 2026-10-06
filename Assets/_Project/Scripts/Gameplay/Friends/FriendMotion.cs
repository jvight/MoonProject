using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A friend's kinematic flight (no physics to fight): its position eases toward where it wants to be with a
    /// critically damped spring and a speed limit, it turns lazily toward what it looks at, and it leans into its
    /// own acceleration like a small drone. Pure maths, allocation-free.
    /// </summary>
    public sealed class FriendMotion
    {
        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private float _roll;

        public Vector3 Position { get; private set; }

        public Vector3 Velocity => _velocity;

        public Quaternion Rotation => Quaternion.Euler(_pitch, _yaw, _roll);

        public float Yaw => _yaw;

        /// <summary>Places it at <paramref name="position"/> facing <paramref name="yaw"/>, at rest.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            Position = position;
            _velocity = Vector3.zero;
            _yaw = yaw;
            _pitch = 0f;
            _roll = 0f;
        }

        /// <param name="floor">Lowest height it may fly at here.</param>
        public void Step(Vector3 target, Vector3 look, float maxSpeed, float floor, FriendTuning tuning,
            float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 previous = _velocity;
            Vector3 position = Vector3.SmoothDamp(Position, target, ref _velocity, tuning.SmoothTime, maxSpeed,
                deltaTime);
            if (position.y < floor)
            {
                position.y = floor;
                _velocity.y = Mathf.Max(0f, _velocity.y);
            }

            Position = position;
            Vector3 toLook = look - Position;
            toLook.y = 0f;
            if (toLook.sqrMagnitude > 1e-4f)
            {
                float desired = Mathf.Atan2(toLook.x, toLook.z) * Mathf.Rad2Deg;
                _yaw = Mathf.LerpAngle(_yaw, desired, Damp.Factor(tuning.TurnEase, deltaTime));
            }

            Vector3 acceleration = (_velocity - previous) / deltaTime;
            Quaternion heading = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 local = Quaternion.Inverse(heading) * acceleration;
            float lean = tuning.LeanPerAcceleration;
            float pitch = Mathf.Clamp(local.z * lean, -tuning.MaxLean, tuning.MaxLean);
            float roll = Mathf.Clamp(-local.x * lean, -tuning.MaxLean, tuning.MaxLean);
            float ease = Damp.Factor(tuning.TurnEase, deltaTime);
            _pitch = Mathf.Lerp(_pitch, pitch, ease);
            _roll = Mathf.Lerp(_roll, roll, ease);
        }
    }
}
