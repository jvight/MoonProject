namespace MoonProject.UI
{
    /// <summary>What a part dropped on 07's face screen does.</summary>
    internal enum DropOutcome
    {
        /// <summary>The cutter takes the spare igniter.</summary>
        Fit = 0,

        /// <summary>The igniter fits the cutter, but only while 07 stands on its charging pad.</summary>
        NotDocked = 1,

        /// <summary>07 is on the pad; the igniter belongs on the cutter, not where it was dropped.</summary>
        ToCutter = 2,

        /// <summary>No device of 07 takes this part.</summary>
        Nowhere = 3,
    }

    /// <summary>
    /// Where a dragged part may go (pure, EditMode-tested). Today one part fits one device: the spare igniter, into the
    /// cutter, while <see cref="MoonProject.Core.IRoverSystems.CanFitIgniter"/> says 07 stands on its pad. Everything
    /// else returns home with a short reason, so a drag always answers.
    /// </summary>
    internal static class DropRule
    {
        /// <summary>
        /// The spare igniter's story-part id in the hold, the same id Gameplay's opening tuning gives it (and the one
        /// <see cref="UiIconSet.IconIdOfPart"/> draws).
        /// </summary>
        public const string SpareIgniterPart = "part.igniter";

        /// <summary>The outcome of dropping story part <paramref name="partId"/> on <paramref name="device"/> (-1: none).</summary>
        public static DropOutcome Decide(string partId, int device, bool canFitIgniter)
        {
            if (partId != SpareIgniterPart)
            {
                return DropOutcome.Nowhere;
            }

            if (!canFitIgniter)
            {
                return DropOutcome.NotDocked;
            }

            return device == FaceDevices.Cutter ? DropOutcome.Fit : DropOutcome.ToCutter;
        }

        /// <summary>The localization key of the short reason a refused drop shows; null for a fit.</summary>
        public static string ReasonKey(DropOutcome outcome)
        {
            switch (outcome)
            {
                case DropOutcome.NotDocked:
                    return UiKeys.CargoDragNotDocked;
                case DropOutcome.ToCutter:
                    return UiKeys.CargoDragToCutter;
                case DropOutcome.Nowhere:
                    return UiKeys.CargoDragNowhere;
                default:
                    return null;
            }
        }
    }
}
