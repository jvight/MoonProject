using System.Globalization;
using UnityEngine;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Follows a camera and keeps the fastest travel (m/s) and turn (deg/s) seen, with the wide shot's weight at that
    /// moment, to catch snaps. Step it once per frame from a test coroutine (after Update): the pose read then is the
    /// one Cinemachine set in the previous frame's LateUpdate, so each change is timed with the previous frame's time.
    /// Changes are measured over windows of at least <see cref="Window"/> seconds: batch runs reach thousands of
    /// frames per second, where a single frame's change is lost in float precision, and a long suite has the odd
    /// render hitch, while a cut still shows as a jump within one window.
    /// </summary>
    public sealed class CameraTrace
    {
        public const float Window = 0.1f;

        private readonly Transform _camera;
        private Vector3 _position;
        private Vector3 _forward;
        private float _previousStep;
        private float _elapsed;

        public CameraTrace(Transform camera)
        {
            _camera = camera;
            _position = camera.position;
            _forward = camera.forward;
        }

        public float Fastest { get; private set; }

        public float FastestTurn { get; private set; }

        public float FastestAtWeight { get; private set; }

        public float FastestTurnAtWeight { get; private set; }

        public int Windows { get; private set; }

        public void Step(float weight)
        {
            _elapsed += _previousStep;
            _previousStep = Time.deltaTime;
            if (_elapsed < Window)
            {
                return;
            }

            Vector3 position = _camera.position;
            Vector3 forward = _camera.forward;
            float speed = Vector3.Distance(_position, position) / _elapsed;
            float turn = Vector3.Angle(_forward, forward) / _elapsed;
            if (speed > Fastest)
            {
                Fastest = speed;
                FastestAtWeight = weight;
            }

            if (turn > FastestTurn)
            {
                FastestTurn = turn;
                FastestTurnAtWeight = weight;
            }

            _position = position;
            _forward = forward;
            _elapsed = 0f;
            Windows++;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "fastest {0:0.00} m/s (wide weight {1:0.00}), turn {2:0.0} deg/s (weight {3:0.00}) over {4} windows",
                Fastest, FastestAtWeight, FastestTurn, FastestTurnAtWeight, Windows);
        }
    }
}
