using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.UI
{
    /// <summary>
    /// When this boot's load put an older journey away (a save from before the salvage era, kept beside the slot,
    /// never deleted), 07 says so once on waking, as one quiet ticker line. Nothing for any other load.
    /// </summary>
    internal sealed class JourneyNotice
    {
        private bool _told;

        /// <summary>The ticker line to queue on waking, true at most once per boot.</summary>
        public bool TryTake(SaveLoadResult loaded, out TickerLine line)
        {
            if (_told || loaded != SaveLoadResult.PutAwayOlder)
            {
                line = default;
                return false;
            }

            _told = true;
            line = new TickerLine(UiKeys.JourneyPutAway);
            return true;
        }
    }
}
