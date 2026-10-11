using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The crew's blueprints (docs/features/M3-16 "Blueprints", M3-19 §5, M3-21 §4): the bay's kits are buildable only
    /// once their tube is decoded. A kit's tube lies in a salvage site's heart and is found with the heart's relic
    /// (<see cref="RelicSurfaced"/>), or in a relay mast's service cabinet, found as the restored mast straightens
    /// (<see cref="RelayCued"/>), or waits on the ground at a world anchor until 07 drives through it
    /// (<see cref="PickUp"/>); either way <see cref="BlueprintFound"/> is published. The tube rides home on 07's back
    /// and up the belly lift to the lab's reader, which takes it (<see cref="Take"/>, publishing
    /// <see cref="BlueprintDelivered"/>) and decodes it (<see cref="Decode"/>, publishing <see cref="FindDecoded"/>):
    /// the kit is pinned. Kenji's plotter in the bay then draws the sheet (<see cref="MarkDrawn"/>). Ids that are not
    /// kits of the board (the radio tower) are always buildable.
    /// </summary>
    public sealed class Blueprints : IBlueprintBoard, IDisposable
    {
        private readonly EventBus _events;
        private readonly HashSet<string> _kits = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _pinned = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _drawn = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _carried = new List<string>();
        private readonly List<string> _reading = new List<string>();
        private readonly Dictionary<string, string> _kitAtSite = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _tubeKits = new List<string>();
        private readonly Func<string, string> _siteOfRelic;
        private readonly IDisposable _surfaced;
        private readonly IDisposable _restored;

        /// <param name="kits">Every kit the bay builds.</param>
        /// <param name="tubes">
        /// Where the kits' tubes lie, one tube per kit: a salvage site (found as its heart's relic surfaces), a relay mast
        /// (found in its service cabinet as the restored mast straightens, <see cref="RelayCued"/>, linked home or not)
        /// or a world anchor.
        /// </param>
        /// <param name="siteOfRelic">The salvage site whose heart held a relic (by relic id), or null.</param>
        public Blueprints(EventBus events, IEnumerable<string> kits, IEnumerable<BlueprintTube> tubes,
            Func<string, string> siteOfRelic)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _siteOfRelic = siteOfRelic ?? throw new ArgumentNullException(nameof(siteOfRelic));
            _kits.UnionWith(kits ?? throw new ArgumentNullException(nameof(kits)));
            foreach (BlueprintTube tube in tubes ?? throw new ArgumentNullException(nameof(tubes)))
            {
                bool inHeart = !string.IsNullOrEmpty(tube.SiteId);
                if (!_kits.Contains(tube.KitId) || _tubeKits.Contains(tube.KitId) || inHeart == tube.IsAtAnchor ||
                    inHeart && _kitAtSite.ContainsKey(tube.SiteId))
                {
                    throw new ArgumentException($"Blueprint tube '{tube.KitId}' is not one tube of a kit of the bay, " +
                                                "in one site's heart or at one world anchor.", nameof(tubes));
                }

                _tubeKits.Add(tube.KitId);
                if (inHeart)
                {
                    _kitAtSite.Add(tube.SiteId, tube.KitId);
                }
            }

            _surfaced = events.Subscribe<RelicSurfaced>(OnSurfaced);
            _restored = events.Subscribe<RelayCued>(OnRelayCued);
        }

        public bool IsPinned(string upgradeId)
        {
            return !_kits.Contains(upgradeId) || _pinned.Contains(upgradeId);
        }

        /// <summary>Every kit the bay builds.</summary>
        public IEnumerable<string> Kits => _kits;

        /// <summary>A found tube rides on 07's back, waiting for the lab's reader.</summary>
        public bool IsCarrying => _carried.Count > 0;

        /// <summary>The kit of the tube the reader would take next (null when 07 carries none).</summary>
        public string NextCarried => _carried.Count > 0 ? _carried[0] : null;

        /// <summary>The reader has decoded at least one found tube.</summary>
        public bool AnyDecoded => _pinned.Count > 0;

        /// <summary>The reader holds a tube it has taken but not finished decoding (null when none).</summary>
        public string Reading => _reading.Count > 0 ? _reading[0] : null;

        /// <summary>True once the reader has decoded <paramref name="kitId"/>'s tube (false for a non-kit).</summary>
        public bool IsDecoded(string kitId)
        {
            return _pinned.Contains(kitId);
        }

        /// <summary>
        /// True while <paramref name="kitId"/>'s tube still waits to be found: not on 07, in the reader or decoded.
        /// </summary>
        public bool IsWaiting(string kitId)
        {
            return _tubeKits.Contains(kitId) && !_pinned.Contains(kitId) && !_carried.Contains(kitId) &&
                   !_reading.Contains(kitId);
        }

        /// <summary>True when <paramref name="kitId"/>'s sheet hangs in the bay (drawn by the plotter).</summary>
        public bool IsDrawn(string kitId)
        {
            return _drawn.Contains(kitId);
        }

        /// <summary>
        /// 07 drove through <paramref name="kitId"/>'s tube waiting at world anchor <paramref name="anchorId"/>: it
        /// rides on 07's back from now on (<see cref="BlueprintFound"/>). Does nothing once the tube was found.
        /// </summary>
        public void PickUp(string kitId, string anchorId, Vector3 position)
        {
            if (!IsWaiting(kitId))
            {
                return;
            }

            _carried.Add(kitId);
            _events.Publish(new BlueprintFound(kitId, anchorId, position));
        }

        /// <summary>
        /// The reader takes the first carried tube off 07's back (<see cref="BlueprintDelivered"/>). Returns its kit,
        /// or null when 07 carries none.
        /// </summary>
        public string Take()
        {
            if (_carried.Count == 0)
            {
                return null;
            }

            string kit = _carried[0];
            _carried.RemoveAt(0);
            _reading.Add(kit);
            _events.Publish(new BlueprintDelivered(kit));
            return kit;
        }

        /// <summary>
        /// The reader finished <paramref name="kitId"/>'s tube: its kit is pinned (<see cref="FindDecoded"/>).
        /// </summary>
        public void Decode(string kitId)
        {
            if (!_reading.Remove(kitId))
            {
                throw new InvalidOperationException($"The reader holds no tube of '{kitId}'.");
            }

            _pinned.Add(kitId);
            _events.Publish(new FindDecoded(kitId));
        }

        /// <summary>A decoded kit whose sheet the plotter has not drawn yet, or null.</summary>
        public string NextToDraw()
        {
            foreach (string kit in _pinned)
            {
                if (!_drawn.Contains(kit))
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
                throw new InvalidOperationException($"'{kitId}' is not decoded; the plotter cannot draw it.");
            }

            _drawn.Add(kitId);
        }

        /// <summary>The state to save.</summary>
        public BlueprintsSaveData Capture()
        {
            return new BlueprintsSaveData
            {
                found = Found(_pinned), carried = _carried.ToArray(), reading = _reading.ToArray(),
                drawn = Found(_drawn),
            };
        }

        /// <summary>Puts a loaded game's blueprints back silently; unknown ids are ignored.</summary>
        public void Restore(BlueprintsSaveData data)
        {
            _pinned.Clear();
            _drawn.Clear();
            _carried.Clear();
            _reading.Clear();
            foreach (string kit in data.found ?? Array.Empty<string>())
            {
                if (_tubeKits.Contains(kit))
                {
                    _pinned.Add(kit);
                }
            }

            foreach (string kit in data.drawn ?? Array.Empty<string>())
            {
                if (_pinned.Contains(kit))
                {
                    _drawn.Add(kit);
                }
            }

            Restore(data.reading, _reading);
            Restore(data.carried, _carried);
        }

        /// <summary>
        /// Tells Rover how many tubes ride on 07 as the game starts (<see cref="BlueprintsCarriedRestored"/>, also when
        /// none do), so they are on its back at once with no ride-in.
        /// </summary>
        public void Announce()
        {
            _events.Publish(new BlueprintsCarriedRestored(_carried.Count));
        }

        public void Dispose()
        {
            _surfaced.Dispose();
            _restored.Dispose();
        }

        /// <summary>The tubes' kits among <paramref name="kits"/>, in the tubes' order.</summary>
        private string[] Found(HashSet<string> kits)
        {
            var found = new List<string>();
            foreach (string kit in _tubeKits)
            {
                if (kits.Contains(kit))
                {
                    found.Add(kit);
                }
            }

            return found.ToArray();
        }

        private void Restore(string[] saved, List<string> into)
        {
            foreach (string kit in saved ?? Array.Empty<string>())
            {
                if (IsWaiting(kit))
                {
                    into.Add(kit);
                }
            }
        }

        private void OnSurfaced(RelicSurfaced surfaced)
        {
            Find(_siteOfRelic(surfaced.RelicId), surfaced.Position);
        }

        /// <summary>A restored mast straightens: its service cabinet swings open on the tube it kept.</summary>
        private void OnRelayCued(RelayCued cued)
        {
            if (cued.Cue == RelayCue.Straightened)
            {
                Find(cued.RelayId, cued.Position);
            }
        }

        /// <summary>The tube kept at <paramref name="site"/>, if any and not yet found, rides home on 07's back.</summary>
        private void Find(string site, Vector3 position)
        {
            if (site == null || !_kitAtSite.TryGetValue(site, out string kit) || !IsWaiting(kit))
            {
                return;
            }

            _carried.Add(kit);
            _events.Publish(new BlueprintFound(kit, site, position));
        }
    }
}
