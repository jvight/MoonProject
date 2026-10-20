using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Autoplay
{
    /// <summary>One beat's first-time-player check: how long until a readable next-action cue was on screen.</summary>
    public sealed class NaiveResult
    {
        public NaiveResult(string beat, bool passed, float seconds, string cue, float budget, string shotStart,
            string shotEnd)
        {
            Beat = beat;
            Passed = passed;
            Seconds = seconds;
            Cue = cue;
            Budget = budget;
            ShotStart = shotStart;
            ShotEnd = shotEnd;
        }

        public string Beat { get; }

        public bool Passed { get; }

        /// <summary>Game seconds from the beat's end to the cue (the whole budget when none showed).</summary>
        public float Seconds { get; }

        /// <summary>What the cue was ("chip 'B Build'", "line '...'", "world cue '...'"), or what was missing.</summary>
        public string Cue { get; }

        public float Budget { get; }

        public string ShotStart { get; }

        public string ShotEnd { get; }
    }

    /// <summary>
    /// The first-time player's gate (opening-v3 risk 2: the bot knows the script, a player does not): after each beat
    /// the bot puts the controls down and, for a budget of game seconds, watches what the real game camera shows. A
    /// readable next-action cue must be on screen in time: a prompt chip whose middle lies inside the view, a line
    /// naming the goal, or a lit world cue inside the view. The bot neither drives nor turns the camera meanwhile, so
    /// the measure is what a passive new player sees. A beat with no cue in time fails the run, and the report lists
    /// every beat's time to cue.
    /// </summary>
    public sealed class NaiveGuard
    {
        /// <summary>Game seconds a cue may take to show after a beat, unless a beat's own budget says otherwise.</summary>
        public const float DefaultBudgetSeconds = 5f;

        private readonly AutoplayBot _bot;
        private readonly Dictionary<string, float> _budgets = new Dictionary<string, float>();
        private readonly List<NaiveResult> _results = new List<NaiveResult>();

        public NaiveGuard(AutoplayBot bot)
        {
            _bot = bot;
        }

        public IReadOnlyList<NaiveResult> Results => _results;

        /// <summary>True while every beat checked showed its cue in time.</summary>
        public bool Passed
        {
            get
            {
                foreach (NaiveResult result in _results)
                {
                    if (!result.Passed)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>A beat's own budget (game seconds) in place of the default.</summary>
        public void SetBudget(string beat, float seconds)
        {
            _budgets[beat] = seconds;
        }

        /// <summary>Stands passive after <paramref name="beat"/> and measures the time to a cue.</summary>
        public IEnumerator Check(string beat)
        {
            if (!_bot.World.Refresh() || _bot.World.Ui == null)
            {
                yield break;
            }

            float budget = _budgets.TryGetValue(beat, out float own) ? own : DefaultBudgetSeconds;
            _bot.Idle();
            string shotStart = "naive-" + beat + "-0s";
            _bot.Shot(shotStart);
            float start = Time.time;
            string cue = null;
            while (Time.time - start < budget && cue == null)
            {
                yield return null;
                cue = FindCue();
            }

            float seconds = Time.time - start;
            string shotEnd = "naive-" + beat + "-" + budget.ToString("0", CultureInfo.InvariantCulture) + "s";
            _bot.Shot(shotEnd);
            bool passed = cue != null;
            Record(new NaiveResult(beat, passed, seconds,
                passed ? cue : "nothing readable on screen (" + _bot.World.Ui.DescribeChip() + ")", budget, shotStart,
                shotEnd));
        }

        /// <summary>Adds a beat's measured result to the table.</summary>
        public void Record(NaiveResult result)
        {
            _results.Add(result);
        }

        /// <summary>The first readable cue now, or null.</summary>
        private string FindCue()
        {
            UiProbe ui = _bot.World.Ui;
            if (ui.ChipInView(out string chip))
            {
                return "chip '" + chip + "'";
            }

            string line = ui.ObjectiveLine();
            if (line.Length > 0)
            {
                return "line '" + line + "'";
            }

            Camera camera = _bot.World.Context.Get<IViewCamera>().Camera;
            foreach (WorldCue cue in WorldCues.All(_bot.World))
            {
                if (cue.Lit() && WorldCue.OnScreen(camera, cue.Point(), 0f))
                {
                    return "world cue '" + cue.Name + "'";
                }
            }

            return null;
        }

        /// <summary>The report's table: each beat's time to cue, the cue, and the two screenshots.</summary>
        public string Render()
        {
            if (_results.Count == 0)
            {
                return string.Empty;
            }

            var text = new StringBuilder("\n## First-time player check (naive)\n\n");
            text.Append("A readable next-action cue must be on screen within the budget of game seconds after each ")
                .Append("beat, with the bot passive: ").Append(Passed ? "all beats showed one." : "SOME BEATS SHOWED NONE.")
                .Append("\n\n| Beat | Time to cue (s) | Budget (s) | Cue | Result | At +0 s | At end |\n")
                .Append("|---|---|---|---|---|---|---|\n");
            foreach (NaiveResult result in _results)
            {
                text.Append("| ").Append(result.Beat).Append(" | ")
                    .Append(result.Passed ? result.Seconds.ToString("0.0", CultureInfo.InvariantCulture) : "-")
                    .Append(" | ").Append(result.Budget.ToString("0", CultureInfo.InvariantCulture)).Append(" | ")
                    .Append(result.Cue.Replace("|", "/")).Append(" | ").Append(result.Passed ? "pass" : "FAIL")
                    .Append(" | ").Append(result.ShotStart).Append(" | ").Append(result.ShotEnd).Append(" |\n");
            }

            foreach (NaiveResult result in _results)
            {
                if (!result.Passed)
                {
                    text.Append("\nFAIL ").Append(result.Beat).Append(": no readable next-action cue within ")
                        .Append(result.Budget.ToString("0", CultureInfo.InvariantCulture))
                        .Append(" s of the beat's end (").Append(result.Cue).Append(").");
                }
            }

            return text.Append('\n').ToString();
        }
    }
}
