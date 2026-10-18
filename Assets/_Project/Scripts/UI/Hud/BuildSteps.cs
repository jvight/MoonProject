using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>The step of building the one guidance line explains.</summary>
    internal enum BuildStep
    {
        /// <summary>No build step to explain (driving, or the card menu, which carries its own key row).</summary>
        None = 0,

        /// <summary>A structure is picked: drive to the apron's marks and place it.</summary>
        Place = 1,

        /// <summary>A structure stands unfinished: hold the key to weld it.</summary>
        Weld = 2,
    }

    /// <summary>
    /// Which build step the guidance line explains, and with which words (pure, EditMode-tested). The words hold one
    /// <c>{0}</c> for the key that places and welds on the active device.
    /// </summary>
    internal static class BuildSteps
    {
        /// <summary>The step for build mode in <paramref name="state"/> with weld <paramref name="activeWeldId"/>.</summary>
        public static BuildStep Of(BuildModeState state, int activeWeldId)
        {
            switch (state)
            {
                case BuildModeState.Placing:
                    return BuildStep.Place;
                case BuildModeState.Welding:
                    return activeWeldId > 0 ? BuildStep.Weld : BuildStep.None;
                default:
                    return BuildStep.None;
            }
        }

        /// <summary>The localization key of <paramref name="step"/>'s line, or null for none.</summary>
        public static string KeyOf(BuildStep step)
        {
            switch (step)
            {
                case BuildStep.Place:
                    return UiKeys.BuildStepPlace;
                case BuildStep.Weld:
                    return UiKeys.BuildStepWeld;
                default:
                    return null;
            }
        }

        /// <summary>True when the line carries the weld's progress bar.</summary>
        public static bool ShowsProgress(BuildStep step)
        {
            return step == BuildStep.Weld;
        }
    }
}
