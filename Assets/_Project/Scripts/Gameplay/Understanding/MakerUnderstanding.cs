using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What 07 understands of each maker (docs/design/maker-understanding.md §1): every completed databank entry gives
    /// its maker one point; seeded entries and entries with no maker give none. Points are derived from the databank
    /// each time it changes (and once at session start, <see cref="Announce"/>), so nothing is saved and nothing can
    /// desync. A level unlocks its adapted blueprint. Through the opening (rule 6) gains are held: nothing is announced
    /// and nothing unlocks until the west signal, which then announces each maker's held gain once
    /// (<see cref="UnderstandingGained"/>); a loaded game past the signal starts silent. Plain logic: no engine
    /// objects; allocation-free after construction.
    /// </summary>
    public sealed class MakerUnderstanding : IDisposable
    {
        private readonly EventBus _events;
        private readonly IDatabank _databank;
        private readonly Dictionary<string, DatabankMaker> _counted =
            new Dictionary<string, DatabankMaker>(StringComparer.Ordinal);
        private readonly MakerTrack[] _tracks;
        private readonly int[] _points;
        private readonly int[] _announced;
        private readonly IDisposable[] _tokens;
        private bool _released;

        /// <param name="events">The game's bus.</param>
        /// <param name="databank">07's scan archive: what is found and how far.</param>
        /// <param name="catalogue">Every entry: its maker, and whether it is seeded (which never counts).</param>
        /// <param name="settings">Each maker's thresholds and adapted blueprints.</param>
        public MakerUnderstanding(EventBus events, IDatabank databank, DatabankCatalogue catalogue,
            MakerUnderstandingSettings settings)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _databank = databank ?? throw new ArgumentNullException(nameof(databank));
            if (catalogue == null)
            {
                throw new ArgumentNullException(nameof(catalogue));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            for (int i = 0; i < catalogue.Count; i++)
            {
                DatabankEntry entry = catalogue.EntryAt(i);
                DatabankEntryInfo info = entry.ToInfo();
                if (info.Maker != DatabankMaker.None && entry.Unlock.Kind != DatabankUnlockKind.Seeded)
                {
                    _counted[entry.Id] = info.Maker;
                }
            }

            int makers = 0;
            foreach (DatabankMaker maker in Enum.GetValues(typeof(DatabankMaker)))
            {
                makers = Math.Max(makers, (int)maker + 1);
            }

            _tracks = new MakerTrack[makers];
            for (int i = 0; i < settings.Tracks.Count; i++)
            {
                _tracks[(int)settings.Tracks[i].Maker] = settings.Tracks[i];
            }

            _points = new int[makers];
            _announced = new int[makers];
            _tokens = new[]
            {
                events.Subscribe<DatabankEntryAdded>(_ => Changed()),
                events.Subscribe<DatabankEntryProgressed>(_ => Changed()),
                events.Subscribe<OpeningMilestoneReached>(OnMilestoneReached),
            };
        }

        /// <summary>The opening's west signal has passed: gains show and unlocks count.</summary>
        public bool Released => _released;

        /// <summary>Points 07 holds for <paramref name="maker"/>: its completed, non-seeded entries.</summary>
        public int PointsOf(DatabankMaker maker)
        {
            return _points[(int)maker];
        }

        /// <summary>
        /// The session starts (after the save is applied, before the opening replays its milestones): counts what the
        /// databank holds, silently.
        /// </summary>
        public void Announce()
        {
            Recount();
        }

        /// <summary>The level <paramref name="maker"/>'s points reach (0 with no track).</summary>
        public int LevelOf(DatabankMaker maker)
        {
            MakerTrack track = _tracks[(int)maker];
            return track != null ? track.LevelAt(PointsOf(maker)) : 0;
        }

        /// <summary>
        /// True once the level that unlocks <paramref name="adaptedId"/> is reached and the opening has released it.
        /// </summary>
        public bool IsUnlocked(string adaptedId)
        {
            if (!_released || string.IsNullOrEmpty(adaptedId))
            {
                return false;
            }

            for (int m = 0; m < _tracks.Length; m++)
            {
                MakerTrack track = _tracks[m];
                if (track == null)
                {
                    continue;
                }

                for (int level = 1; level <= track.Levels; level++)
                {
                    if (string.Equals(track.Adapted(level), adaptedId, StringComparison.Ordinal))
                    {
                        return LevelOf(track.Maker) >= level;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                _tokens[i].Dispose();
            }
        }

        private void OnMilestoneReached(OpeningMilestoneReached reached)
        {
            if (reached.Milestone != OpeningMilestone.SignalWest || _released)
            {
                return;
            }

            _released = true;
            Recount();
            if (reached.Restored)
            {
                Array.Copy(_points, _announced, _points.Length);
                return;
            }

            AnnounceGains();
        }

        private void Changed()
        {
            Recount();
            if (_released)
            {
                AnnounceGains();
            }
        }

        private void Recount()
        {
            Array.Clear(_points, 0, _points.Length);
            IReadOnlyList<string> unlocked = _databank.UnlockedIds;
            for (int i = 0; i < unlocked.Count; i++)
            {
                string id = unlocked[i];
                if (_counted.TryGetValue(id, out DatabankMaker maker) && _databank.Progress(id).IsComplete)
                {
                    _points[(int)maker]++;
                }
            }
        }

        private void AnnounceGains()
        {
            for (int m = 0; m < _tracks.Length; m++)
            {
                MakerTrack track = _tracks[m];
                if (track == null || _points[m] <= _announced[m])
                {
                    continue;
                }

                int level = track.LevelAt(_points[m]);
                string adapted = level > track.LevelAt(_announced[m]) ? track.Adapted(level) : string.Empty;
                int next = level < track.Levels ? track.Threshold(level + 1) : 0;
                bool first = _announced[m] == 0;
                _announced[m] = _points[m];
                _events.Publish(new UnderstandingGained(track.Maker, _points[m], level, next, adapted, first));
            }
        }
    }
}
