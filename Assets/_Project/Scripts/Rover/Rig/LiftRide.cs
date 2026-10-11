using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's ride pose on the crew's belly lift (docs/features/M3-19 §4, ruling 14: the machine carries it): from the
    /// moment the platform starts up until it has set 07 down again, the platform holds 07 (a kinematic pose the
    /// controller takes, <see cref="RoverController.HoldPose"/>). Its guides ease 07 to the turntable's middle, and 07
    /// rises, turns and comes down with the platform, keeping the heading it drove on with. Allocation-free.
    /// </summary>
    public sealed class LiftRide
    {
        private readonly RoverController _rover;
        private readonly Transform _platform;
        private readonly LabCameraSettings _settings;
        private Vector3 _local;
        private float _yawOffset;
        private float _elapsed;

        public LiftRide(RoverController rover, Transform platform, LabCameraSettings settings)
        {
            _rover = rover != null ? rover : throw new ArgumentNullException(nameof(rover));
            _platform = platform != null ? platform : throw new ArgumentNullException(nameof(platform));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The platform holds 07.</summary>
        public bool Holding { get; private set; }

        /// <summary>The platform starts up with 07 on it: from here it holds 07 where it stands on it.</summary>
        public void Begin()
        {
            if (Holding)
            {
                return;
            }

            Holding = true;
            _elapsed = 0f;
            _local = _platform.InverseTransformPoint(_rover.PhysicsBody.position);
            _yawOffset = Mathf.DeltaAngle(_platform.eulerAngles.y, _rover.Rotation.eulerAngles.y);
            Hold();
        }

        public void Step(float deltaTime)
        {
            if (!Holding)
            {
                return;
            }

            _elapsed += deltaTime;
            Hold();
        }

        /// <summary>The platform has set 07 down: free again, at rest.</summary>
        public void End()
        {
            if (!Holding)
            {
                return;
            }

            Holding = false;
            _rover.ReleasePose();
        }

        private void Hold()
        {
            float spread = 1f - LabShot.Ease(_elapsed / _settings.CentreSeconds);
            var local = new Vector3(_local.x * spread, _local.y, _local.z * spread);
            _rover.HoldPose(_platform.TransformPoint(local), _platform.eulerAngles.y + _yawOffset);
        }
    }
}
