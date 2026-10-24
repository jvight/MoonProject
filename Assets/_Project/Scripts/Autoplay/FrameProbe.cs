using System;
using Unity.Profiling;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Reads the profiler's per-frame counters (managed bytes allocated in the frame, main and render thread time)
    /// through <see cref="ProfilerRecorder"/>, which reads without allocating. A counter the player does not offer
    /// reads -1. Dispose it when the run ends.
    /// </summary>
    public sealed class FrameProbe : IDisposable
    {
        private const double NanosecondsPerMillisecond = 1000000.0;

        private ProfilerRecorder _gc;
        private ProfilerRecorder _main;
        private ProfilerRecorder _render;
        private long _heapBefore = -1L;

        public FrameProbe()
        {
            _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1);
        }

        /// <summary>
        /// Managed bytes allocated in the frame, or -1: the profiler's counter where the player has it, else the growth
        /// of the managed heap since the last read (a release player offers few profiler counters; a collection reads
        /// as no growth, so a frame that allocated and collected under-reports). Read once a frame.
        /// </summary>
        public long GcBytes()
        {
            if (_gc.Valid)
            {
                return _gc.LastValue;
            }

            long now = GC.GetTotalMemory(false);
            long growth = _heapBefore < 0L ? 0L : Math.Max(0L, now - _heapBefore);
            _heapBefore = now;
            return growth;
        }

        /// <summary>The main thread's last whole frame (ms), or -1.</summary>
        public float MainMilliseconds => _main.Valid ? (float)(_main.LastValue / NanosecondsPerMillisecond) : -1f;

        /// <summary>The render thread's last whole frame (ms), or -1.</summary>
        public float RenderMilliseconds => _render.Valid ? (float)(_render.LastValue / NanosecondsPerMillisecond) : -1f;

        public void Dispose()
        {
            _gc.Dispose();
            _main.Dispose();
            _render.Dispose();
        }
    }
}
