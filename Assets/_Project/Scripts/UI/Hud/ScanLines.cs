using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>The words of a scan's pickup note (docs/design/0.5.5-owner-feedback.md §1.2).</summary>
    internal static class ScanLines
    {
        /// <summary>
        /// "Fragment 2 of 3 · Cargo Cradle", or, when the blueprint was already complete, "Known blueprint · +3 Metal".
        /// </summary>
        public static string Fragment(ILocalization localization, FragmentScanned scanned)
        {
            if (scanned.Known)
            {
                return string.Format(localization.Get(UiKeys.ScanKnown), scanned.Metal);
            }

            return string.Format(localization.Get(UiKeys.ScanFragment), scanned.Index + 1, scanned.Count,
                localization.Get(UiKeys.UpgradeName(scanned.BlueprintId)));
        }
    }
}
