using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// Turns the drive stick into one <see cref="LabCommand"/> per push: a push past the press threshold gives the
    /// command of its main axis once, and the stick must come back under the release threshold before the next one.
    /// </summary>
    public sealed class LabCommandLatch
    {
        private bool _armed;

        /// <summary>
        /// Waits for the stick to come back first (e.g. it is still held from driving onto the platform).
        /// </summary>
        public void Disarm()
        {
            _armed = false;
        }

        public LabCommand Step(Vector2 drive, float press, float release)
        {
            float x = Mathf.Abs(drive.x);
            float y = Mathf.Abs(drive.y);
            float push = Mathf.Max(x, y);
            if (!_armed)
            {
                _armed = push < release;
                return LabCommand.None;
            }

            if (push < press)
            {
                return LabCommand.None;
            }

            _armed = false;
            if (x >= y)
            {
                return drive.x > 0f ? LabCommand.TurnRight : LabCommand.TurnLeft;
            }

            return drive.y > 0f ? LabCommand.Look : LabCommand.Down;
        }
    }
}
