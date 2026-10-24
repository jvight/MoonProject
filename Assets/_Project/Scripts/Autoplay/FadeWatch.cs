namespace MoonProject.Autoplay
{
    /// <summary>
    /// Tells a fade-out from a steady element: an element whose opacity fell since the last frame is leaving the
    /// screen, so a first-time player is not being asked anything by it, however readable its words still are.
    /// </summary>
    public sealed class FadeWatch
    {
        /// <summary>A fall smaller than this is rounding, not a fade.</summary>
        private const float FallEpsilon = 0.001f;

        private int _frame = -1;
        private float _opacity;
        private bool _falling;
        private bool _seen;

        /// <summary>
        /// True when the element is steady or rising at <paramref name="frame"/>; the first sample has nothing to be
        /// compared with and counts as not yet steady, so a fade already under way is never taken for a cue. Several
        /// samples in one frame give one answer.
        /// </summary>
        public bool Steady(float opacity, int frame)
        {
            if (frame != _frame)
            {
                _falling = _seen && opacity < _opacity - FallEpsilon;
                _opacity = opacity;
                _frame = frame;
                bool first = !_seen;
                _seen = true;
                if (first)
                {
                    return false;
                }
            }

            return !_falling;
        }

        /// <summary>Forgets the history: the element was hidden, so its next showing starts afresh.</summary>
        public void Reset()
        {
            _seen = false;
            _frame = -1;
            _falling = false;
        }
    }
}
