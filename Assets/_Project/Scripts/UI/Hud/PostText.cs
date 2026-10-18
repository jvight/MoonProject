using System.Globalization;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The words of 07's self-test lines (pure, EditMode-tested). Information lines read <c>post.&lt;step&gt;</c> whole;
    /// a check prints <c>post.&lt;step&gt;</c> as its name, then "name · result", where the result is the step's own
    /// <c>post.&lt;step&gt;.&lt;result&gt;</c> when it has one (the cutter's missing igniter, the cells' charge) and the
    /// shared <c>post.result.&lt;result&gt;</c> otherwise.
    /// </summary>
    internal static class PostText
    {
        public const string Separator = " · ";

        private static readonly string[] StepKeys =
        {
            "post.carrier", "post.resume", "post.agenda", "post.lens", "post.lamps", "post.head", "post.wheels",
            "post.motor", "post.cutter", "post.solar_wing", "post.antenna", "post.battery", "post.assembly",
            "post.begin",
        };

        private static readonly string[] ResultSuffixes = { ".ok", ".fault", ".jammed", ".receive_only", ".info" };

        /// <summary>The localisation key of <paramref name="step"/>'s line or name.</summary>
        public static string KeyOf(BootCheckStep step)
        {
            return StepKeys[(int)step];
        }

        /// <summary>True for the steps that only inform: they have no check and no result.</summary>
        public static bool IsInformation(BootCheckStep step)
        {
            return step == BootCheckStep.Carrier || step == BootCheckStep.Resume || step == BootCheckStep.Agenda ||
                   step == BootCheckStep.Assembly || step == BootCheckStep.Begin;
        }

        /// <summary>What the line shows when the step starts.</summary>
        public static string Started(BootCheckStep step, ILocalization localization, int resumeDays)
        {
            string text = localization.Get(KeyOf(step));
            if (step == BootCheckStep.Resume)
            {
                return string.Format(text, resumeDays.ToString("N0", CultureInfo.InvariantCulture)
                    .Replace(',', ' '));
            }

            return IsInformation(step) ? text : text + Separator;
        }

        /// <summary>What the line settles to when the step completes; <paramref name="charge01"/> fills the cells' line.</summary>
        public static string Settled(BootCheckStep step, BootCheckResult result, ILocalization localization,
            int resumeDays, float charge01)
        {
            if (IsInformation(step) || result == BootCheckResult.Info)
            {
                return Started(step, localization, resumeDays);
            }

            string own = KeyOf(step) + ResultSuffixes[(int)result];
            string words = localization.TryGet(own, out string text)
                ? text
                : localization.Get("post.result" + ResultSuffixes[(int)result]);
            if (step == BootCheckStep.Battery)
            {
                words = string.Format(words, Mathf.RoundToInt(Mathf.Clamp01(charge01) * 100f));
            }

            return localization.Get(KeyOf(step)) + Separator + words;
        }
    }
}
