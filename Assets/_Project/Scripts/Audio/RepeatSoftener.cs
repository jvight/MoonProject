namespace MoonProject.Audio
{
    /// <summary>
    /// Keeps a rapidly repeated cue (menu focus steps, slider ticks) from fatiguing: repeats closer than the minimum
    /// interval are skipped, and each repeat inside the streak window plays a little softer (down to a floor) until
    /// the player pauses long enough for the streak to reset. Times are seconds on an unscaled clock.
    /// </summary>
    public sealed class RepeatSoftener
    {
        private float _lastTime = float.NegativeInfinity;
        private int _streak;

        /// <summary>Returns false to skip this repeat; otherwise <paramref name="gain"/> is its volume scale.</summary>
        public bool TryTrigger(float now, float minInterval, float streakWindow, float decayPerRepeat, float floor,
            out float gain)
        {
            float gap = now - _lastTime;
            if (gap < minInterval)
            {
                gain = 0f;
                return false;
            }

            _streak = gap <= streakWindow ? _streak + 1 : 0;
            _lastTime = now;
            float softened = 1f;
            for (int i = 0; i < _streak && softened > floor; i++)
            {
                softened *= decayPerRepeat;
            }

            gain = softened > floor ? softened : floor;
            return true;
        }
    }
}
