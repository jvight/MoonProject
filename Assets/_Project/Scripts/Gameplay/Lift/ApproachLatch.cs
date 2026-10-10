namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// Fires once each time 07 comes within a radius of something, and re-arms only once 07 has gone a margin further
    /// out, so idling at the edge never repeats it.
    /// </summary>
    public sealed class ApproachLatch
    {
        private bool _inside;

        /// <summary>True on the step 07 comes within <paramref name="radius"/>.</summary>
        public bool Step(float distance, float radius, float rearmMargin)
        {
            if (_inside)
            {
                _inside = distance <= radius + rearmMargin;
                return false;
            }

            _inside = distance <= radius;
            return _inside;
        }
    }
}
