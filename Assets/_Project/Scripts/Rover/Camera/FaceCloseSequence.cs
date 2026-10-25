using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How closing 07's face screen ends (pure): the camera stays on the face while the CRT powers off, the view dips
    /// to dark, the camera is back on the chase pose (the swap, once, at full dark), and the view comes back up. Closing
    /// used to swing the camera half round 07 (about 175 degrees, up to a few hundred degrees a second): one fast whirl.
    /// </summary>
    public sealed class FaceCloseSequence
    {
        private float _hold;
        private float _dipOut;
        private float _dipIn;
        private float _clock;
        private bool _swapped;

        /// <summary>The sequence is running (from <see cref="Begin"/> to the last of the dip back up).</summary>
        public bool IsActive { get; private set; }

        /// <summary>How dark the view is (0 clear, 1 black).</summary>
        public float Dark01 { get; private set; }

        /// <summary>True for exactly the one step in which the camera is to be put back on the chase pose.</summary>
        public bool SwapNow { get; private set; }

        /// <summary>The camera has been swapped back (the dip is on its way up).</summary>
        public bool IsSwapped => IsActive && _swapped;

        /// <summary>Total seconds the sequence takes.</summary>
        public float TotalSeconds => _hold + _dipOut + _dipIn;

        public void Begin(float holdSeconds, float dipOutSeconds, float dipInSeconds)
        {
            _hold = Mathf.Max(0f, holdSeconds);
            _dipOut = Mathf.Max(0.01f, dipOutSeconds);
            _dipIn = Mathf.Max(0.01f, dipInSeconds);
            _clock = 0f;
            _swapped = false;
            IsActive = true;
            Dark01 = 0f;
            SwapNow = false;
        }

        /// <summary>Ends it where it is (the screen was opened again): the view is clear at once.</summary>
        public void Cancel()
        {
            IsActive = false;
            Dark01 = 0f;
            SwapNow = false;
        }

        public void Step(float deltaTime)
        {
            SwapNow = false;
            if (!IsActive)
            {
                return;
            }

            _clock += Mathf.Max(0f, deltaTime);
            float dipStart = _hold;
            float swapAt = _hold + _dipOut;
            if (_clock < dipStart)
            {
                Dark01 = 0f;
                return;
            }

            if (_clock < swapAt)
            {
                Dark01 = Smoothing.SmoothStep(0f, 1f, (_clock - dipStart) / _dipOut);
                return;
            }

            if (!_swapped)
            {
                _swapped = true;
                SwapNow = true;
                Dark01 = 1f;
                return;
            }

            float back = (_clock - swapAt) / _dipIn;
            if (back >= 1f)
            {
                IsActive = false;
                Dark01 = 0f;
                return;
            }

            Dark01 = 1f - Smoothing.SmoothStep(0f, 1f, back);
        }
    }
}
