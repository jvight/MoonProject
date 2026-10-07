using System;
using System.Collections.Generic;

namespace MoonProject.Audio
{
    /// <summary>
    /// "Now playing" for the radio ticker: each track is announced at most once per session. Title keys are built
    /// once when tracks are registered (<c>track.&lt;id&gt;.title</c> for base tracks, the cassette's existing
    /// <c>cassette.&lt;id&gt;.title</c> for tapes), so announcing never allocates.
    /// </summary>
    public sealed class NowPlayingLog
    {
        /// <summary>Ticker key whose {0} is the track title.</summary>
        public const string TickerKey = "ticker.radio.now_playing";

        private const string TrackKeyPrefix = "track.";
        private const string CassetteKeyPrefix = "cassette.";
        private const string TitleKeySuffix = ".title";

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        /// <summary>Title key of a base radio track.</summary>
        public static string TrackTitleKey(string trackId)
        {
            return TrackKeyPrefix + trackId + TitleKeySuffix;
        }

        /// <summary>Title key of a cassette (its tape track is announced under the cassette's own title).</summary>
        public static string CassetteTitleKey(string cassetteId)
        {
            return CassetteKeyPrefix + cassetteId + TitleKeySuffix;
        }

        /// <summary>Makes <paramref name="trackId"/> announceable under <paramref name="titleKey"/> (call at
        /// initialisation for every known track).</summary>
        public void Register(string trackId, string titleKey)
        {
            if (string.IsNullOrEmpty(trackId) || _entries.ContainsKey(trackId))
            {
                return;
            }

            _entries.Add(trackId, new Entry(titleKey));
        }

        /// <summary>The localization key of <paramref name="trackId"/>'s title (null for unknown tracks).</summary>
        public string TitleKey(string trackId)
        {
            return trackId != null && _entries.TryGetValue(trackId, out Entry entry) ? entry.TitleKey : null;
        }

        /// <summary>True (and remembers it) the first time <paramref name="trackId"/> starts this session.</summary>
        public bool TryAnnounce(string trackId, out string titleKey)
        {
            titleKey = null;
            if (trackId == null || !_entries.TryGetValue(trackId, out Entry entry) || entry.Announced)
            {
                return false;
            }

            entry.Announced = true;
            titleKey = entry.TitleKey;
            return true;
        }

        private sealed class Entry
        {
            public Entry(string titleKey)
            {
                TitleKey = titleKey;
            }

            public string TitleKey { get; }

            public bool Announced { get; set; }
        }
    }
}
