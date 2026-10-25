using System.Text;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The per-maker bar a maker scan puts on its own card (pure, EditMode-tested; docs/design/opening-v3-payoff-audit.md
    /// §3): <c>KESSLER–ARAI █░ · next: SSR-3 Headlamp</c>, one filled cell per entry understood toward the next adapted
    /// blueprint. A maker whose next unlock has no name yet shows no bar at all, never a bar that leads nowhere.
    /// </summary>
    internal static class MakerBar
    {
        /// <summary>Most cells the bar draws: a longer track would read as noise on the CRT.</summary>
        public const int MaxCells = 8;

        private const char Filled = '█';
        private const char Empty = '░';
        private const string NameSuffix = ".name";

        /// <summary>"adapted.ssr3_lamp.name": the name key of an adapted blueprint.</summary>
        public static string NameKeyOf(string adaptedId)
        {
            return adaptedId + NameSuffix;
        }

        /// <summary>
        /// The bar's line for <paramref name="maker"/> at <paramref name="points"/> of <paramref name="threshold"/>
        /// toward <paramref name="nextAdaptedId"/>; false when there is nothing true to show (no next unlock, no name for
        /// it, the threshold already met, or a track too long to draw).
        /// </summary>
        public static bool TryLine(DatabankMaker maker, int points, int threshold, string nextAdaptedId,
            ILocalization localization, out string line)
        {
            line = null;
            string makerKey = DatabankText.MakerKey(maker);
            if (makerKey == null || string.IsNullOrEmpty(nextAdaptedId) || threshold <= points || points < 0 ||
                threshold > MaxCells || !localization.TryGet(NameKeyOf(nextAdaptedId), out string next))
            {
                return false;
            }

            var cells = new StringBuilder(threshold);
            cells.Append(Filled, points);
            cells.Append(Empty, threshold - points);
            line = string.Format(localization.Get(UiKeys.MakerBar), localization.Get(makerKey), cells, next);
            return true;
        }

        /// <summary>The name of the adapted blueprint <paramref name="adaptedId"/>; false when it has none yet.</summary>
        public static bool TryName(string adaptedId, ILocalization localization, out string nameKey)
        {
            nameKey = string.IsNullOrEmpty(adaptedId) ? null : NameKeyOf(adaptedId);
            return nameKey != null && localization.TryGet(nameKey, out _);
        }
    }
}
