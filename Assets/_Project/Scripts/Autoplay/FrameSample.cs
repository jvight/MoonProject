namespace MoonProject.Autoplay
{
    /// <summary>One frame as the bot measured it: how long, what the game was doing, what the profiler counted.</summary>
    public readonly struct FrameSample
    {
        public FrameSample(float atSeconds, float milliseconds, FrameTag tags, long gcBytes, float mainMilliseconds,
            float cpuMainMilliseconds, float cpuRenderMilliseconds, float gpuMilliseconds)
        {
            AtSeconds = atSeconds;
            Milliseconds = milliseconds;
            Tags = tags;
            GcBytes = gcBytes;
            MainMilliseconds = mainMilliseconds;
            CpuMainMilliseconds = cpuMainMilliseconds;
            CpuRenderMilliseconds = cpuRenderMilliseconds;
            GpuMilliseconds = gpuMilliseconds;
        }

        public float AtSeconds { get; }

        public float Milliseconds { get; }

        public FrameTag Tags { get; }

        /// <summary>Managed bytes allocated in the frame, or -1 when the counter is not available.</summary>
        public long GcBytes { get; }

        /// <summary>The main thread's work in the frame as the profiler counted it (ms), or -1 when not available.</summary>
        public float MainMilliseconds { get; }

        /// <summary>
        /// The frame timing manager's CPU main thread time (ms) for the latest frame it had finished, a few frames
        /// behind this one, or -1 when not available.
        /// </summary>
        public float CpuMainMilliseconds { get; }

        /// <summary>The frame timing manager's CPU render thread time (ms), as <see cref="CpuMainMilliseconds"/>.</summary>
        public float CpuRenderMilliseconds { get; }

        /// <summary>The frame timing manager's GPU frame time (ms), as <see cref="CpuMainMilliseconds"/>.</summary>
        public float GpuMilliseconds { get; }
    }
}
