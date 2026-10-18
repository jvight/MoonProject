using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MoonProject.Autoplay
{
    /// <summary>The outcome of one beat of the scenario: what was seen on screen and why it passed or failed.</summary>
    public sealed class BeatResult
    {
        public BeatResult(string id, string title, bool passed, string reason, float seconds, string objective,
            string chip, string sky)
        {
            Sky = sky;
            Id = id;
            Title = title;
            Passed = passed;
            Reason = reason;
            Seconds = seconds;
            Objective = objective;
            Chip = chip;
        }

        public string Id { get; }

        /// <summary>The sky's phase when the beat ended (the day is the opening's clock: the base must be night-ready by sunset).</summary>
        public string Sky { get; }

        public string Title { get; }

        public bool Passed { get; }

        public string Reason { get; }

        public float Seconds { get; }

        /// <summary>The objective line on screen when the beat ended.</summary>
        public string Objective { get; }

        /// <summary>The prompt chip on screen when the beat ended (glyph and word), empty when none showed.</summary>
        public string Chip { get; }
    }

    /// <summary>The beats of a run in order, and the Markdown report written at the end.</summary>
    public sealed class BeatLog
    {
        private readonly List<BeatResult> _beats = new List<BeatResult>();

        public IReadOnlyList<BeatResult> Beats => _beats;

        /// <summary>True when at least one beat ran and every beat that ran passed.</summary>
        public bool Passed
        {
            get
            {
                foreach (BeatResult beat in _beats)
                {
                    if (!beat.Passed)
                    {
                        return false;
                    }
                }

                return _beats.Count > 0;
            }
        }

        public void Add(BeatResult beat)
        {
            _beats.Add(beat);
        }

        /// <summary>
        /// The report: verdict, beat table, frame-time summary with the slow frames, and the screenshots taken.
        /// <paramref name="stopReason"/> is empty when the scenario ran to its end.
        /// </summary>
        public string Render(string scenario, string buildInfo, float runSeconds, FrameTimeLog frames,
            IReadOnlyList<string> screenshots, string stopReason)
        {
            var text = new StringBuilder();
            text.Append("# Autoplay report: ").Append(scenario).Append("\n\n");
            text.Append("**Result: ").Append(Passed && string.IsNullOrEmpty(stopReason) ? "PASS" : "FAIL")
                .Append("**\n\n");
            if (!string.IsNullOrEmpty(stopReason))
            {
                text.Append("Stopped: ").Append(stopReason).Append("\n\n");
            }

            text.Append("- Build: ").Append(buildInfo).Append('\n');
            text.Append("- Run time: ").Append(Number(runSeconds, "0.0")).Append(" s\n\n");
            text.Append("## Beats\n\n| Beat | Result | Seconds | Sky | Reason | Objective line | Prompt chip |\n");
            text.Append("|---|---|---|---|---|---|---|\n");
            foreach (BeatResult beat in _beats)
            {
                text.Append("| ").Append(beat.Id).Append(' ').Append(Cell(beat.Title)).Append(" | ")
                    .Append(beat.Passed ? "pass" : "FAIL").Append(" | ").Append(Number(beat.Seconds, "0.0"))
                    .Append(" | ").Append(Cell(beat.Sky)).Append(" | ").Append(Cell(beat.Reason)).Append(" | ").Append(Cell(beat.Objective))
                    .Append(" | ").Append(Cell(beat.Chip)).Append(" |\n");
            }

            text.Append("\n## Frame times\n\n");
            text.Append("- Frames: ").Append(frames.Frames.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("- Mean: ").Append(Number(frames.MeanMilliseconds, "0.0")).Append(" ms, worst ")
                .Append(Number(frames.WorstMilliseconds, "0.0")).Append(" ms\n");
            text.Append("- Frames over ").Append(Number(FrameTimeLog.ThresholdMilliseconds, "0")).Append(" ms: ")
                .Append(frames.SlowCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
            if (frames.Slow.Count > 0)
            {
                text.Append("\n| At (s) | Frame (ms) | What was happening |\n|---|---|---|\n");
                foreach (SlowFrame slow in frames.Slow)
                {
                    text.Append("| ").Append(Number(slow.AtSeconds, "0.0")).Append(" | ")
                        .Append(Number(slow.Milliseconds, "0")).Append(" | ").Append(Cell(slow.Activity))
                        .Append(" |\n");
                }
            }

            text.Append("\n## Screenshots\n\n");
            foreach (string shot in screenshots)
            {
                text.Append("- ").Append(shot).Append('\n');
            }

            return text.ToString();
        }

        private static string Number(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Cell(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|", "/").Replace("\n", " ");
        }
    }
}
