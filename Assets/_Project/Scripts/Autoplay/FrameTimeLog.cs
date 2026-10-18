using System.Collections.Generic;

namespace MoonProject.Autoplay
{
    /// <summary>One frame that took longer than the threshold, with what the bot was doing then.</summary>
    public readonly struct SlowFrame
    {
        public SlowFrame(float atSeconds, float milliseconds, string activity)
        {
            AtSeconds = atSeconds;
            Milliseconds = milliseconds;
            Activity = activity;
        }

        public float AtSeconds { get; }

        public float Milliseconds { get; }

        public string Activity { get; }
    }

    /// <summary>
    /// Frame-time statistics of a run: count, mean, worst, and every frame over <see cref="ThresholdMilliseconds"/>
    /// with the activity reported at that moment. Allocates only for the slow frames it keeps.
    /// </summary>
    public sealed class FrameTimeLog
    {
        public const float ThresholdMilliseconds = 50f;
        public const int KeptSlowFrames = 200;

        private readonly List<SlowFrame> _slow = new List<SlowFrame>();
        private double _totalMilliseconds;

        public int Frames { get; private set; }

        public float WorstMilliseconds { get; private set; }

        public int SlowCount { get; private set; }

        public IReadOnlyList<SlowFrame> Slow => _slow;

        public float MeanMilliseconds => Frames == 0 ? 0f : (float)(_totalMilliseconds / Frames);

        public void Record(float atSeconds, float milliseconds, string activity)
        {
            Frames++;
            _totalMilliseconds += milliseconds;
            if (milliseconds > WorstMilliseconds)
            {
                WorstMilliseconds = milliseconds;
            }

            if (milliseconds <= ThresholdMilliseconds)
            {
                return;
            }

            SlowCount++;
            if (_slow.Count < KeptSlowFrames)
            {
                _slow.Add(new SlowFrame(atSeconds, milliseconds, activity));
            }
        }
    }
}
