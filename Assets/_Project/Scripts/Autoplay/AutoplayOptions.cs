using System;
using System.Globalization;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// What the command line asks of the bot: <c>-autoplay opening -autoplay-out &lt;dir&gt;</c>, with optional
    /// <c>-autoplay-shot-seconds</c>, <c>-autoplay-timeout</c> and <c>-autoplay-until</c> (the last beat to play).
    /// </summary>
    public sealed class AutoplayOptions
    {
        public const string ScenarioFlag = "-autoplay";
        public const string OutFlag = "-autoplay-out";
        public const string ShotSecondsFlag = "-autoplay-shot-seconds";
        public const string TimeoutFlag = "-autoplay-timeout";
        public const string UntilFlag = "-autoplay-until";

        public const float DefaultShotSeconds = 20f;
        public const float DefaultTimeoutSeconds = 900f;

        private AutoplayOptions(string scenario, string outDirectory, float shotSeconds, float timeoutSeconds,
            string untilBeat)
        {
            Scenario = scenario;
            OutDirectory = outDirectory;
            ShotSeconds = shotSeconds;
            TimeoutSeconds = timeoutSeconds;
            UntilBeat = untilBeat;
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
                Value(args, UntilFlag) ?? string.Empty);
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
