using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The words of the build ghost's reason chip (pure, EditMode-tested): the reason's own line, except a real overlap
    /// with a named structure in the way, which says what blocks it ("Blocked by: Charging Pad") so the player knows
    /// what to move.
    /// </summary>
    internal static class PlacementReasonText
    {
        public static string Of(PlacementReason reason, string blockerNameKey, ILocalization localization)
        {
            if (reason == PlacementReason.Overlaps && !string.IsNullOrEmpty(blockerNameKey))
            {
                return string.Format(localization.Get(UiKeys.BuildReasonOverlapsBy), localization.Get(blockerNameKey));
            }

            string key = UiKeys.PlacementReasonKey(reason);
            return key != null ? localization.Get(key) : string.Empty;
        }
    }
}
