using System;
using System.Globalization;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// What the command line asks of the bot: <c>-autoplay opening -autoplay-out &lt;dir&gt;</c>, with optional
    /// <c>-autoplay-shot-seconds</c>, <c>-autoplay-timeout</c>, <c>-autoplay-until</c> (the last beat to play),
    /// <c>-autoplay-mute</c> (silence all audio at boot), <c>-autoplay-strict-guidance 0</c> (two guidance elements
    /// together only warn instead of failing the run) and <c>-autoplay-lines 1</c> (objective lines are part of the
    /// opening and are checked on screen; by default the game shows none, and one showing is a stall reminder that
    /// fails the run).
    /// </summary>
    public sealed class AutoplayOptions
    {
        public const string ScenarioFlag = "-autoplay";
        public const string OutFlag = "-autoplay-out";
        public const string ShotSecondsFlag = "-autoplay-shot-seconds";
        public const string TimeoutFlag = "-autoplay-timeout";
        public const string UntilFlag = "-autoplay-until";
        public const string LinesFlag = "-autoplay-lines";
        public const string NaiveFlag = "-autoplay-naive";
        public const string PointerFlag = "-autoplay-pointer";
        public const string StrictGuidanceFlag = "-autoplay-strict-guidance";
        public const string MuteFlag = "-autoplay-mute";

        public const float DefaultShotSeconds = 20f;
        public const float DefaultTimeoutSeconds = 900f;

        private AutoplayOptions(string scenario, string outDirectory, float shotSeconds, float timeoutSeconds,
            string untilBeat, bool linesExpected, bool strictGuidance, bool naive, bool mute, bool pointer)
        {
            Scenario = scenario;
            OutDirectory = outDirectory;
            ShotSeconds = shotSeconds;
            TimeoutSeconds = timeoutSeconds;
            UntilBeat = untilBeat;
            LinesExpected = linesExpected;
            StrictGuidance = strictGuidance;
            Naive = naive;
            Mute = mute;
            Pointer = pointer;
        }

        /// <summary>The scenario to play; today only "opening".</summary>
        public string Scenario { get; }

        public string OutDirectory { get; }

        /// <summary>Seconds between the screenshots taken on top of the one at each beat.</summary>
        public float ShotSeconds { get; }

        /// <summary>Seconds of wall time after which the run stops and fails.</summary>
        public float TimeoutSeconds { get; }

        /// <summary>Id of the last beat to play ("B3"), or empty to play the scenario to its end.</summary>
        public string UntilBeat { get; }

        /// <summary>
        /// Two guidance elements on screen together fail the run (the default); off, the run only warns.
        /// </summary>
        public bool StrictGuidance { get; }

        /// <summary>
        /// The first build (B5, the pad) is made with the mouse alone: B, a click on the card, the wheel to turn the
        /// ghost, a left click to place and weld (the pointer-only player, opening-v3 audit row 5).
        /// </summary>
        public bool Pointer { get; }

        /// <summary>
        /// After every beat the bot stands passive and measures how long a first-time player waits for a readable
        /// next-action cue to be on screen (<see cref="NaiveGuard"/>); a beat that shows none in time fails the run.
        /// </summary>
        public bool Naive { get; }

        /// <summary>The objective lines are part of the opening: the bot checks them on screen. Off, any line is a fallback.</summary>
        public bool LinesExpected { get; }

        /// <summary>Silences all audio at boot (<c>AudioListener.volume = 0</c>) for this run.</summary>
        public bool Mute { get; }

        /// <summary>
        /// True when the command line asks for the bot; <paramref name="problem"/> says what is wrong when it asks
        /// wrongly (and <paramref name="options"/> stays null).
        /// </summary>
        public static bool TryParse(string[] args, out AutoplayOptions options, out string problem)
        {
            options = null;
            problem = null;
            string scenario = Value(args, ScenarioFlag);
            if (scenario == null)
            {
                return false;
            }

            string outDirectory = Value(args, OutFlag);
            if (string.IsNullOrEmpty(outDirectory))
            {
                problem = $"{ScenarioFlag} needs {OutFlag} <directory>.";
                return true;
            }

            if (!TryFloat(Value(args, ShotSecondsFlag), DefaultShotSeconds, out float shotSeconds) || shotSeconds <= 0f)
            {
                problem = $"{ShotSecondsFlag} needs a positive number of seconds.";
                return true;
            }

            if (!TryFloat(Value(args, TimeoutFlag), DefaultTimeoutSeconds, out float timeout) || timeout <= 0f)
            {
                problem = $"{TimeoutFlag} needs a positive number of seconds.";
                return true;
            }

            options = new AutoplayOptions(scenario, outDirectory, shotSeconds, timeout,
                Value(args, UntilFlag) ?? string.Empty, Value(args, LinesFlag) == "1",
                Value(args, StrictGuidanceFlag) != "0", Value(args, NaiveFlag) == "1", HasFlag(args, MuteFlag),
                Value(args, PointerFlag) == "1");
            return true;
        }

        private static string Value(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1 >= args.Length || args[i + 1] != "0";
                }
            }

            return false;
        }

        private static bool TryFloat(string text, float fallback, out float value)
        {
            if (text == null)
            {
                value = fallback;
                return true;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
