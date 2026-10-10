using System;

namespace MoonProject.Rover
{
    /// <summary>
    /// The first beat of a fresh game (M3-16 P5): the camera holds a front three-quarter shot of 07's lens as it wakes
    /// and looks up at Earth, until the player gives any input or the hold runs out; then it eases into the chase
    /// frame. <see cref="Weight"/> is the opening's share of the camera: 1 while holding, easing smoothly to 0.
    /// </summary>
    public sealed class WakeOpening
    {
        private readonly RoverCameraTuning _tuning;
        private float _held;
        private float _eased;

        public WakeOpening(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Whether the opening holds or is easing out.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Whether it has started easing out to the chase frame.</summary>
        public bool IsEasing { get; private set; }

        /// <summary>The opening's share of the camera, 1 while holding, 0 once handed to the chase frame.</summary>
        public float Weight { get; private set; }

        /// <summary>Starts the opening; ignored while one is already running.</summary>
        public void Begin()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            IsEasing = false;
            _held = 0f;
            _eased = 0f;
            Weight = 1f;
        }

        /// <param name="playerInput">The player gave look or drive input this frame.</param>
        public void Step(bool playerInput, float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return;
            }

            if (!IsEasing)
            {
                _held += deltaTime;
                IsEasing = playerInput || _held >= _tuning.WakeHoldSeconds;
                return;
            }

            _eased = Math.Min(1f, _eased + deltaTime / _tuning.WakeEaseSeconds);
            Weight = 1f - Smoothing.SmoothStep(0f, 1f, _eased);
            if (_eased >= 1f)
            {
                IsActive = false;
                IsEasing = false;
                Weight = 0f;
            }
        }
    }
}
