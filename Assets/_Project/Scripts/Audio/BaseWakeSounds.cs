using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Which wake phrase each stage of the base's power plays (M3-16): the variant of
    /// <see cref="AudioCueIds.BaseWake"/> for the place that just woke.
    /// </summary>
    public static class BaseWakeSounds
    {
        /// <summary>Variant label for <paramref name="stage"/>; null when the stage has no phrase (asleep).</summary>
        public static string LabelOf(BasePowerStage stage)
        {
            switch (stage)
            {
                case BasePowerStage.Home:
                    return "home";
                case BasePowerStage.Bay:
                    return "bay";
                case BasePowerStage.Chart:
                    return "lift";
                default:
                    return null;
            }
        }
    }
}
