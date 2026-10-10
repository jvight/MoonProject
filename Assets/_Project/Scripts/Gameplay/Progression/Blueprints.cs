using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's blueprint board (docs/features/M3-16 "Blueprints", P7): the bay's kits are buildable only once their
    /// sheet is pinned. One kit's sheet is pinned from the start (Kenji left it); each other kit's tube lies in a
    /// salvage site's heart and is found with the heart's relic (<see cref="RelicSurfaced"/>, publishing
    /// <see cref="BlueprintFound"/>). The tube rides home on 07's back; delivered to the pen plotter
    /// (<see cref="Deliver"/>, publishing <see cref="BlueprintDelivered"/>) its kit is pinned. Ids that are not kits of
    /// the board (the radio tower) are always buildable.
    /// </summary>
    public sealed class Blueprints : IBlueprintBoard, IDisposable
    {
        private readonly EventBus _events;
        private readonly HashSet<string> _kits = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _pinned = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _carried = new List<string>();
        private readonly Dictionary<string, string> _kitAtSite = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Func<string, string> _siteOfRelic;
        private readonly string _fromStart;
        private readonly IDisposable _surfaced;

        /// <param name="kits">Every kit the bay builds.</param>
        /// <param name="fromStart">The kit pinned from the start.</param>
        /// <param name="tubes">Where the other kits' tubes lie.</param>
        /// <param name="siteOfRelic">The salvage site whose heart held a relic (by relic id), or null.</param>
        public Blueprints(EventBus events, IEnumerable<string> kits, string fromStart, IEnumerable<BlueprintTube> tubes,
            Func<string, string> siteOfRelic)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _siteOfRelic = siteOfRelic ?? throw new ArgumentNullException(nameof(siteOfRelic));
            _kits.UnionWith(kits ?? throw new ArgumentNullException(nameof(kits)));
            if (!_kits.Contains(fromStart))
            {
                throw new ArgumentException($"'{fromStart}' is not a kit of the bay.", nameof(fromStart));
            }

            foreach (BlueprintTube tube in tubes ?? throw new ArgumentNullException(nameof(tubes)))
            {
                if (!_kits.Contains(tube.KitId) || tube.KitId == fromStart || string.IsNullOrEmpty(tube.SiteId) ||
                    _kitAtSite.ContainsKey(tube.SiteId))
                {
                    throw new ArgumentException($"Blueprint tube '{tube.KitId}' at '{tube.SiteId}' is not a kit of " +
                                                "the bay found elsewhere, or its site already holds a tube.",
                        nameof(tubes));
                }

                _kitAtSite.Add(tube.SiteId, tube.KitId);
            }

            _fromStart = fromStart;
            _pinned.Add(fromStart);
            _surfaced = events.Subscribe<RelicSurfaced>(OnSurfaced);
        }

        public bool IsPinned(string upgradeId)
        {
            return !_kits.Contains(upgradeId) || _pinned.Contains(upgradeId);
        }

        /// <summary>Every kit the bay builds.</summary>
        public IEnumerable<string> Kits => _kits;

        /// <summary>A found tube rides on 07's back, waiting to be delivered to the plotter.</summary>
        public bool IsCarrying => _carried.Count > 0;

        /// <summary>
        /// Hands the first carried tube to the plotter: its kit is pinned and <see cref="BlueprintDelivered"/>
        /// published. Returns the kit, or null when 07 carries none.
        /// </summary>
        public string Deliver()
        {
            if (_carried.Count == 0)
            {
                return null;
            }

            string kit = _carried[0];
            _carried.RemoveAt(0);
            _pinned.Add(kit);
            _events.Publish(new BlueprintDelivered(kit));
            return kit;
        }

        /// <summary>The tubes still riding on 07 (saved).</summary>
        public string[] CaptureCarried()
        {
            return _carried.ToArray();
        }

        /// <summary>The pinned kits besides the one pinned from the start (saved).</summary>
        public string[] Capture()
        {
            var found = new List<string>();
            foreach (string kit in _kitAtSite.Values)
            {
                if (_pinned.Contains(kit))
                {
                    found.Add(kit);
                }
            }

            return found.ToArray();
        }

        /// <summary>
        /// Pins <paramref name="found"/> and puts <paramref name="carried"/> back on 07 silently (a loaded game);
        /// unknown ids are ignored.
        /// </summary>
        public void Restore(string[] found, string[] carried)
        {
            _pinned.Clear();
            _pinned.Add(_fromStart);
            _carried.Clear();
            foreach (string kit in found ?? Array.Empty<string>())
            {
                if (_kitAtSite.ContainsValue(kit))
                {
                    _pinned.Add(kit);
                }
            }

            foreach (string kit in carried ?? Array.Empty<string>())
            {
                if (_kitAtSite.ContainsValue(kit) && !_pinned.Contains(kit) && !_carried.Contains(kit))
                {
                    _carried.Add(kit);
                }
            }
        }

        public void Dispose()
        {
            _surfaced.Dispose();
        }

        private void OnSurfaced(RelicSurfaced surfaced)
        {
            string site = _siteOfRelic(surfaced.RelicId);
            if (site == null || !_kitAtSite.TryGetValue(site, out string kit) || _pinned.Contains(kit) ||
                _carried.Contains(kit))
            {
                return;
            }

            _carried.Add(kit);
            _events.Publish(new BlueprintFound(kit, site, surfaced.Position));
        }
    }
}
