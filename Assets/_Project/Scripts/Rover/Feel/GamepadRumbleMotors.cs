using UnityEngine.InputSystem;

namespace MoonProject.Rover
{
    /// <summary>
    /// The current Input System gamepad's motors. When the pad changes or goes away the last one is silenced, so a
    /// motor never keeps running on a pad the game no longer drives.
    /// </summary>
    public sealed class GamepadRumbleMotors : IRumbleMotors
    {
        private Gamepad _pad;
        private bool _running;

        public bool IsConnected => Gamepad.current != null;

        public void Set(float low, float high)
        {
            Gamepad current = Gamepad.current;
            if (current != _pad)
            {
                Silence();
                _pad = current;
            }

            if (_pad == null)
            {
                return;
            }

            bool running = low > 0f || high > 0f;
            if (running || _running)
            {
                _pad.SetMotorSpeeds(low, high);
            }

            _running = running;
        }

        private void Silence()
        {
            if (_pad != null && _pad.added && _running)
            {
                _pad.SetMotorSpeeds(0f, 0f);
            }

            _running = false;
        }
    }
}
