namespace MoonProject.Autoplay
{
    /// <summary>
    /// Turns a throttle fraction into W presses and releases frame by frame, since the drive keys are digital: the
    /// key is down for that share of the frames, which the rover's own easing smooths into a speed.
    /// </summary>
    public sealed class ThrottleGate
    {
        private float _carry;

        /// <summary>True when the key should be down this frame for the given throttle (0..1).</summary>
        public bool Next(float throttle)
        {
            if (throttle <= 0f)
            {
                _carry = 0f;
                return false;
            }

            _carry += throttle;
            if (_carry < 1f)
            {
                return false;
            }

            _carry -= 1f;
            return true;
        }
    }
}
