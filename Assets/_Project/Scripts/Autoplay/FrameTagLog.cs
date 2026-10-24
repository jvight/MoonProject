using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Where the slow frames of a run came from: the worst few frames under each <see cref="FrameTag"/>, and one row per
    /// face-screen open or close window, so the first Tab can be set beside the later ones. Allocates only when a
    /// window begins.
    /// </summary>
    public sealed class FrameTagLog
    {
        /// <summary>Worst frames kept per tag.</summary>
        public const int KeptPerTag = 3;

        /// <summary>Waiting frames kept (the worst by length).</summary>
        public const int KeptWaiting = 5;

        /// <summary>A frame this long (ms) can be a waiting frame.</summary>
        public const float WaitingMinMilliseconds = FrameTimeLog.ThresholdMilliseconds;

        /// <summary>A long frame whose main thread worked under this share of it is waiting.</summary>
        public const float WaitingMainShare = 0.5f;

        private static readonly FrameTag[] Tags =
        {
            FrameTag.None, FrameTag.Face, FrameTag.Transition, FrameTag.Build, FrameTag.Scan, FrameTag.Dock,
        };

        private readonly FrameSample[][] _worst = new FrameSample[Tags.Length][];
        private readonly int[] _kept = new int[Tags.Length];
        private readonly int[] _frames = new int[Tags.Length];
        private readonly List<WindowStats> _windows = new List<WindowStats>();
        private readonly FrameSample[] _waiting = new FrameSample[KeptWaiting];
        private int _waitingKept;
        private WindowStats _current;

        public FrameTagLog()
        {
            for (int i = 0; i < _worst.Length; i++)
            {
                _worst[i] = new FrameSample[KeptPerTag];
            }
        }

        /// <summary>How many face-screen windows were seen (opens plus closes).</summary>
        public int WindowCount => _windows.Count;

        /// <summary>How many waiting frames the run had.</summary>
        public int WaitingCount { get; private set; }

        /// <summary>True when any frame carried a profiler reading.</summary>
        public bool HasCounters { get; private set; }

        /// <summary>Counts one frame; <paramref name="window"/> is the face-screen window it fell in, or empty.</summary>
        public void Record(in FrameSample sample, string window)
        {
            HasCounters |= sample.GcBytes >= 0 || sample.MainMilliseconds >= 0f ||
                          sample.CpuMainMilliseconds >= 0f;
            for (int i = 0; i < Tags.Length; i++)
            {
                bool inTag = Tags[i] == FrameTag.None ? sample.Tags == FrameTag.None : (sample.Tags & Tags[i]) != 0;
                if (inTag)
                {
                    _frames[i]++;
                    Keep(_worst[i], ref _kept[i], sample);
                }
            }

            if (IsWaiting(sample))
            {
                WaitingCount++;
                Keep(_waiting, ref _waitingKept, sample);
            }

            if (string.IsNullOrEmpty(window))
            {
                _current = null;
                return;
            }

            if (_current == null || !string.Equals(_current.Name, window, StringComparison.Ordinal))
            {
                _current = new WindowStats(window);
                _windows.Add(_current);
            }

            _current.Add(sample);
        }

        private static void Keep(FrameSample[] list, ref int count, in FrameSample sample)
        {
            if (count == list.Length && sample.Milliseconds <= list[list.Length - 1].Milliseconds)
            {
                return;
            }

            if (count < list.Length)
            {
                count++;
            }

            int slot = count - 1;
            while (slot > 0 && list[slot - 1].Milliseconds < sample.Milliseconds)
            {
                list[slot] = list[slot - 1];
                slot--;
            }

            list[slot] = sample;
        }

        /// <summary>
        /// True for a frame that ran long while the main thread did little of it: the time went to waiting, not to the
        /// game code. A frame with no main-thread reading is never counted.
        /// </summary>
        public static bool IsWaiting(in FrameSample sample)
        {
            return sample.Milliseconds >= WaitingMinMilliseconds && sample.MainMilliseconds >= 0f &&
                   sample.MainMilliseconds < sample.Milliseconds * WaitingMainShare;
        }

        /// <summary>The worst frame (ms) seen under <paramref name="tag"/>, or 0 when none was.</summary>
        public float WorstMilliseconds(FrameTag tag)
        {
            int index = Array.IndexOf(Tags, tag);
            return index >= 0 && _kept[index] > 0 ? _worst[index][0].Milliseconds : 0f;
        }

        /// <summary>The worst frame (ms) inside the window called <paramref name="name"/>, or -1 when there was none.</summary>
        public float WindowWorstMilliseconds(string name)
        {
            foreach (WindowStats window in _windows)
            {
                if (window.Name == name)
                {
                    return window.WorstMilliseconds;
                }
            }

            return -1f;
        }

        /// <summary>The report section: the counters, the worst frames per tag, the waiting frames and the Tab windows.</summary>
        public string Render()
        {
            var text = new StringBuilder("\n## Hitch attribution\n\n");
            text.Append("Counters: ").Append(HasCounters
                    ? "managed heap growth and main thread ms by the profiler (for the frame the delta measures); CPU main, " +
                      "CPU render and GPU ms by the frame timing manager, which lags the frame by a few frames; n/a = not " +
                      "available."
                    : "not available in this player; frame times and tags only.")
                .Append("\n\n### Worst frames per tag\n\n").Append(FrameHeader("Tag | Frames | Rank | "));
            for (int i = 0; i < Tags.Length; i++)
            {
                for (int rank = 0; rank < _kept[i]; rank++)
                {
                    AppendFrame(text, (i == 0 ? "none" : Tags[i].ToString()) + " | " +
                                      _frames[i].ToString(CultureInfo.InvariantCulture) + " | " +
                                      (rank + 1).ToString(CultureInfo.InvariantCulture) + " | ", _worst[i][rank]);
                }
            }

            text.Append("\n### Waiting frames\n\n").Append("Frames over ")
                .Append(WaitingMinMilliseconds.ToString("0", CultureInfo.InvariantCulture))
                .Append(" ms whose main thread worked under ")
                .Append((WaitingMainShare * 100f).ToString("0", CultureInfo.InvariantCulture))
                .Append("% of the frame: the time went to waiting (vsync, the GPU, a lock, the OS), not to the game code. ")
                .Append(WaitingCount.ToString(CultureInfo.InvariantCulture)).Append(" in the run; the worst ")
                .Append(_waitingKept.ToString(CultureInfo.InvariantCulture)).Append(":\n\n")
                .Append(FrameHeader("Tags | "));
            for (int rank = 0; rank < _waitingKept; rank++)
            {
                AppendFrame(text, _waiting[rank].Tags + " | ", _waiting[rank]);
            }

            if (_windows.Count > 0)
            {
                text.Append("\n### Tab (face screen) opens and closes\n\n")
                    .Append("| Window | Frames | Worst frame (ms) | Mean (ms) | GC (bytes, total) | Worst main (ms) | ")
                    .Append("Worst CPU render (ms) | Worst GPU (ms) |\n|---|---|---|---|---|---|---|---|\n");
                foreach (WindowStats w in _windows)
                {
                    text.Append("| ").Append(w.Name).Append(" | ")
                        .Append(w.Frames.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(Ms(w.WorstMilliseconds)).Append(" | ").Append(Ms(w.MeanMilliseconds)).Append(" | ")
                        .Append(w.GcBytes.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(Ms(w.WorstMain)).Append(" | ").Append(Ms(w.WorstRender)).Append(" | ")
                        .Append(Ms(w.WorstGpu)).Append(" |\n");
                }
            }

            return text.ToString();
        }

        private static string FrameHeader(string lead)
        {
            string[] columns = lead.Split('|');
            var line = new StringBuilder("|");
            var rule = new StringBuilder("|");
            foreach (string column in columns)
            {
                if (column.Trim().Length > 0)
                {
                    line.Append(' ').Append(column.Trim()).Append(" |");
                    rule.Append("---|");
                }
            }

            line.Append(" At (s) | Frame (ms) | GC (bytes) | Main (ms) | CPU main (ms) | CPU render (ms) | GPU (ms) |\n");
            rule.Append("---|---|---|---|---|---|---|\n");
            return line.Append(rule).ToString();
        }

        private static void AppendFrame(StringBuilder text, string lead, in FrameSample s)
        {
            text.Append("| ").Append(lead)
                .Append(s.AtSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                .Append(s.Milliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                .Append(s.GcBytes.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                .Append(Ms(s.MainMilliseconds)).Append(" | ").Append(Ms(s.CpuMainMilliseconds)).Append(" | ")
                .Append(Ms(s.CpuRenderMilliseconds)).Append(" | ").Append(Ms(s.GpuMilliseconds)).Append(" |\n");
        }

        private static string Ms(float value)
        {
            return value < 0f ? "n/a" : value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private sealed class WindowStats
        {
            private double _totalMilliseconds;

            public WindowStats(string name)
            {
                Name = name;
                WorstMain = -1f;
                WorstRender = -1f;
                WorstGpu = -1f;
            }

            public string Name { get; }

            public int Frames { get; private set; }

            public float WorstMilliseconds { get; private set; }

            public float MeanMilliseconds => Frames == 0 ? 0f : (float)(_totalMilliseconds / Frames);

            public long GcBytes { get; private set; }

            public float WorstMain { get; private set; }

            public float WorstRender { get; private set; }

            public float WorstGpu { get; private set; }

            public void Add(in FrameSample sample)
            {
                Frames++;
                _totalMilliseconds += sample.Milliseconds;
                WorstMilliseconds = Math.Max(WorstMilliseconds, sample.Milliseconds);
                GcBytes += Math.Max(0L, sample.GcBytes);
                WorstMain = Math.Max(WorstMain, sample.MainMilliseconds);
                WorstRender = Math.Max(WorstRender, sample.CpuRenderMilliseconds);
                WorstGpu = Math.Max(WorstGpu, sample.GpuMilliseconds);
            }
        }
    }
}
