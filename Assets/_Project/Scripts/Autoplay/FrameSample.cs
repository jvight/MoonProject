namespace MoonProject.Autoplay
{
    /// <summary>One frame as the bot measured it: how long, what the game was doing, what the profiler counted.</summary>
    public readonly struct FrameSample
    {
        public FrameSample(float atSeconds, float milliseconds, FrameTag tags, long gcBytes, float mainMilliseconds,
            float renderMilliseconds)
        {
            AtSeconds = atSeconds;
            Milliseconds = milliseconds;
            Tags = tags;
            GcBytes = gcBytes;
            MainMilliseconds = mainMilliseconds;
            RenderMilliseconds = renderMilliseconds;
        }

        public float AtSeconds { get; }

        public float Milliseconds { get; }

        public FrameTag Tags { get; }

        /// <summary>Managed bytes allocated in the frame, or -1 when the counter is not available.</summary>
        public long GcBytes { get; }

        /// <summary>The main thread's time (ms), or -1 when not available.</summary>
        public float MainMilliseconds { get; }

        /// <summary>The render thread's time (ms), or -1 when not available.</summary>
        public float RenderMilliseconds { get; }
    }
}
