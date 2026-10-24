using System;
using Unity.Profiling;
using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Reads the per-frame counters: managed growth and main thread time through <see cref="ProfilerRecorder"/>, and
    /// the CPU main, CPU render and GPU times through <see cref="FrameTimingManager"/> (the autoplay player is built
    /// with Frame Timing Stats on; elsewhere they read -1). Neither read allocates. Call <see cref="Capture"/> once a
    /// frame before reading. Dispose it when the run ends.
    /// </summary>
    public sealed class FrameProbe : IDisposable
    {
        private const double NanosecondsPerMillisecond = 1000000.0;

        private ProfilerRecorder _gc;
        private ProfilerRecorder _main;
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private bool _timingRead;
        private long _heapBefore = -1L;

        public FrameProbe()
        {
            _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
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

        /// <summary>Takes this frame's reading of the frame timing manager (it lags the frame by a few).</summary>
        public void Capture()
        {
            _timingRead = false;
            if (!FrameTimingManager.IsFeatureEnabled())
            {
                return;
            }

            FrameTimingManager.CaptureFrameTimings();
            _timingRead = FrameTimingManager.GetLatestTimings(1, _timing) > 0;
        }

        /// <summary>The CPU main thread time (ms) of the latest frame the manager finished, or -1.</summary>
        public float CpuMainMilliseconds => _timingRead ? (float)_timing[0].cpuMainThreadFrameTime : -1f;

        /// <summary>The CPU render thread time (ms) of that frame, or -1.</summary>
        public float CpuRenderMilliseconds => _timingRead ? (float)_timing[0].cpuRenderThreadFrameTime : -1f;

        /// <summary>The GPU frame time (ms) of that frame, or -1.</summary>
        public float GpuMilliseconds => _timingRead ? (float)_timing[0].gpuFrameTime : -1f;

        public void Dispose()
        {
            _gc.Dispose();
            _main.Dispose();
        }
    }
}
