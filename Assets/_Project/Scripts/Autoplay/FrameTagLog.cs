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

        private static readonly FrameTag[] Tags =
        {
            FrameTag.None, FrameTag.Face, FrameTag.Transition, FrameTag.Build, FrameTag.Scan, FrameTag.Dock,
        };

        private readonly FrameSample[][] _worst = new FrameSample[Tags.Length][];
        private readonly int[] _kept = new int[Tags.Length];
        private readonly int[] _frames = new int[Tags.Length];
        private readonly List<WindowStats> _windows = new List<WindowStats>();
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

        /// <summary>True when any frame carried a profiler reading.</summary>
        public bool HasCounters { get; private set; }

        /// <summary>Counts one frame; <paramref name="window"/> is the face-screen window it fell in, or empty.</summary>
        public void Record(in FrameSample sample, string window)
        {
            HasCounters |= sample.GcBytes >= 0 || sample.MainMilliseconds >= 0f || sample.RenderMilliseconds >= 0f;
            for (int i = 0; i < Tags.Length; i++)
            {
                bool inTag = Tags[i] == FrameTag.None ? sample.Tags == FrameTag.None : (sample.Tags & Tags[i]) != 0;
                if (inTag)
                {
                    _frames[i]++;
                    Keep(i, sample);
                }
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

        private void Keep(int tag, in FrameSample sample)
        {
            FrameSample[] list = _worst[tag];
            int count = _kept[tag];
            if (count == KeptPerTag && sample.Milliseconds <= list[KeptPerTag - 1].Milliseconds)
            {
                return;
            }

            if (count < KeptPerTag)
            {
                _kept[tag] = ++count;
            }

            int slot = count - 1;
            while (slot > 0 && list[slot - 1].Milliseconds < sample.Milliseconds)
            {
                list[slot] = list[slot - 1];
                slot--;
            }

            list[slot] = sample;
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

        /// <summary>The report section: the counters, the worst frames per tag and the Tab windows.</summary>
        public string Render()
        {
            var text = new StringBuilder("\n## Hitch attribution\n\n");
            text.Append("Profiler counters: ").Append(HasCounters
                    ? "recorded per frame (GC bytes in the frame, main thread ms, render thread ms; -1 = not available)."
                    : "not available in this player; frame times and tags only.")
                .Append("\n\n### Worst frames per tag\n\n")
                .Append("| Tag | Frames | Rank | At (s) | Frame (ms) | GC (bytes) | Main (ms) | Render (ms) |\n")
                .Append("|---|---|---|---|---|---|---|---|\n");
            for (int i = 0; i < Tags.Length; i++)
            {
                for (int rank = 0; rank < _kept[i]; rank++)
                {
                    FrameSample s = _worst[i][rank];
                    text.Append("| ").Append(i == 0 ? "none" : Tags[i].ToString()).Append(" | ")
                        .Append(_frames[i].ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append((rank + 1).ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(s.AtSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(s.Milliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(s.GcBytes.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(s.MainMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(s.RenderMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" |\n");
                }
            }

            if (_windows.Count > 0)
            {
                text.Append("\n### Tab (face screen) opens and closes\n\n")
                    .Append("| Window | Frames | Worst frame (ms) | Mean (ms) | GC (bytes, total) | Worst main (ms) | ")
                    .Append("Worst render (ms) |\n|---|---|---|---|---|---|---|\n");
                foreach (WindowStats w in _windows)
                {
                    text.Append("| ").Append(w.Name).Append(" | ")
                        .Append(w.Frames.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(w.WorstMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(w.MeanMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(w.GcBytes.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(w.WorstMain.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ")
                        .Append(w.WorstRender.ToString("0.0", CultureInfo.InvariantCulture)).Append(" |\n");
                }
            }

            return text.ToString();
        }

        private sealed class WindowStats
        {
            private double _totalMilliseconds;

            public WindowStats(string name)
            {
                Name = name;
                WorstMain = -1f;
                WorstRender = -1f;
            }

            public string Name { get; }

            public int Frames { get; private set; }

            public float WorstMilliseconds { get; private set; }

            public float MeanMilliseconds => Frames == 0 ? 0f : (float)(_totalMilliseconds / Frames);

            public long GcBytes { get; private set; }

            public float WorstMain { get; private set; }

            public float WorstRender { get; private set; }

            public void Add(in FrameSample sample)
            {
                Frames++;
                _totalMilliseconds += sample.Milliseconds;
                WorstMilliseconds = Math.Max(WorstMilliseconds, sample.Milliseconds);
                GcBytes += Math.Max(0L, sample.GcBytes);
                WorstMain = Math.Max(WorstMain, sample.MainMilliseconds);
                WorstRender = Math.Max(WorstRender, sample.RenderMilliseconds);
            }
        }
    }
}
