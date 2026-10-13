using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The bay's test cut after a cutter upgrade (docs/features/M3-26 §5, install moment) as plain logic: a pause
    /// while the part seats, the first charge at the new speed, a short burn, then the coupon cracks. Step says
    /// which beat comes due.
    /// </summary>
    public sealed class TestCut
    {
        public enum Beat
        {
            None,
            Charge,
            Ignite,
            Crack,
        }

        private float _elapsed;
        private float _delay;
        private float _charge;
        private float _burn;
        private int _next;

        /// <summary>Whether a test cut is under way.</summary>
        public bool IsRunning => _next > 0;

        /// <summary>The charge's length (s) of the cut under way.</summary>
        public float ChargeSeconds => _charge;

        public void Start(float delay, float charge, float burn)
        {
            _elapsed = 0f;
            _delay = Mathf.Max(0f, delay);
            _charge = Mathf.Max(0f, charge);
            _burn = Mathf.Max(0f, burn);
            _next = 1;
        }

        public Beat Step(float deltaTime)
        {
            if (_next == 0)
            {
                return Beat.None;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            float due = _next == 1 ? _delay : _next == 2 ? _delay + _charge : _delay + _charge + _burn;
            if (_elapsed < due)
            {
                return Beat.None;
            }

            Beat beat = (Beat)_next;
            _next = _next == 3 ? 0 : _next + 1;
            return beat;
        }
    }
}
