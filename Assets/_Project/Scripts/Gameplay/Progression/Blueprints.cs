using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The crew's blueprints (docs/features/M3-16 "Blueprints", docs/design/0.5.5-owner-feedback.md §1): the bay's kits
    /// are buildable only once learned, when 07 has scanned every fragment of the kit's set (<see cref="Learn"/>,
    /// publishing <see cref="BlueprintCompleted"/>): the kit is pinned. Kenji's plotter in the bay then draws the sheet
    /// (<see cref="MarkDrawn"/>). Ids that are not kits of the board (the radio tower) are always buildable.
    /// </summary>
    public sealed class Blueprints : IBlueprintBoard
    {
        private readonly EventBus _events;
        private readonly List<string> _kits = new List<string>();
        private readonly HashSet<string> _pinned = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _drawn = new HashSet<string>(StringComparer.Ordinal);

        /// <param name="kits">Every kit the bay builds.</param>
        public Blueprints(EventBus events, IEnumerable<string> kits)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            foreach (string kit in kits ?? throw new ArgumentNullException(nameof(kits)))
            {
                if (string.IsNullOrEmpty(kit) || _kits.Contains(kit))
                {
                    throw new ArgumentException($"Blueprint kit '{kit}' is empty or listed twice.", nameof(kits));
                }

                _kits.Add(kit);
            }
        }

        public bool IsPinned(string upgradeId)
        {
            return !_kits.Contains(upgradeId) || _pinned.Contains(upgradeId);
        }

        /// <summary>Every kit the bay builds.</summary>
        public IEnumerable<string> Kits => _kits;

        /// <summary>True once every fragment of <paramref name="kitId"/>'s set is scanned (false for a non-kit).</summary>
        public bool IsLearned(string kitId)
        {
            return _pinned.Contains(kitId);
        }

        /// <summary>True when <paramref name="kitId"/>'s sheet hangs in the bay (drawn by the plotter).</summary>
        public bool IsDrawn(string kitId)
        {
            return _drawn.Contains(kitId);
        }

        /// <summary>
        /// The last fragment of <paramref name="kitId"/>'s set is scanned: the kit is pinned
        /// (<see cref="BlueprintCompleted"/>). Does nothing when it is already learned.
        /// </summary>
        public void Learn(string kitId)
        {
            if (!_kits.Contains(kitId))
            {
                throw new ArgumentException($"'{kitId}' is no kit of the bay.", nameof(kitId));
            }

            if (_pinned.Add(kitId))
            {
                _events.Publish(new BlueprintCompleted(kitId));
            }
        }

        /// <summary>A learned kit whose sheet the plotter has not drawn yet (in the kits' order), or null.</summary>
        public string NextToDraw()
        {
            foreach (string kit in _kits)
            {
                if (_pinned.Contains(kit) && !_drawn.Contains(kit))
                {
                    return kit;
                }
            }

            return null;
        }

        /// <summary>The plotter drew <paramref name="kitId"/>'s sheet.</summary>
        public void MarkDrawn(string kitId)
        {
            if (!_pinned.Contains(kitId))
            {
                throw new InvalidOperationException($"'{kitId}' is not learned; the plotter cannot draw it.");
            }

            _drawn.Add(kitId);
        }

        /// <summary>The state to save.</summary>
        public BlueprintsSaveData Capture()
        {
            return new BlueprintsSaveData { found = InOrder(_pinned), drawn = InOrder(_drawn) };
        }

        /// <summary>
        /// Puts a loaded game's blueprints back silently; unknown ids are ignored. A save from before scanning keeps
        /// every blueprint whose tube was found: decoded, riding on 07 or in the lab's reader, the kit is learned (D1
        /// migration); a tube still waiting is gone with the tubes, and its set waits to be scanned.
        /// </summary>
        public void Restore(BlueprintsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _pinned.Clear();
            _drawn.Clear();
            Pin(data.found);
            Pin(data.carried);
            Pin(data.reading);
            foreach (string kit in data.drawn ?? Array.Empty<string>())
            {
                if (_pinned.Contains(kit))
                {
                    _drawn.Add(kit);
                }
            }
        }

        private void Pin(string[] kits)
        {
            foreach (string kit in kits ?? Array.Empty<string>())
            {
                if (_kits.Contains(kit))
                {
                    _pinned.Add(kit);
                }
            }
        }

        /// <summary>The bay's kits among <paramref name="kits"/>, in the kits' order.</summary>
        private string[] InOrder(HashSet<string> kits)
        {
            var listed = new List<string>();
            foreach (string kit in _kits)
            {
                if (kits.Contains(kit))
                {
                    listed.Add(kit);
                }
            }

            return listed.ToArray();
        }
    }
}
