using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's scan archive (docs/design/databank.md): the entries unlocked so far, how far each scan set has come, and
    /// which are read. Story scans unlock an entry whole; the first fragment of a scan set unlocks its entry, each
    /// further fragment counts it up until the last draws it full; what 07 reads as it wakes joins silently at the end
    /// of the self-test. Publishes <see cref="DatabankEntryAdded"/> and <see cref="DatabankEntryProgressed"/> for what
    /// is found live, never for a load. A load that finds scans or milestones the saved databank does not know (an
    /// older save) unlocks their entries silently the first time the databank is read. Plain logic: no engine
    /// objects.
    /// </summary>
    public sealed class Databank : IDatabank, IDisposable
    {
        private const string StoryPrefix = StoryScan.IdPrefix;
        private const string SetPrefix = RuinScanSet.BlueprintPrefix;

        private readonly DatabankCatalogue _catalogue;
        private readonly EventBus _events;
        private readonly IDatabankSources _sources;
        private readonly List<string> _unlocked = new List<string>();
        private readonly HashSet<string> _unlockedSet = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _read = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _found = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, DatabankEntryInfo> _infos = new Dictionary<string, DatabankEntryInfo>(StringComparer.Ordinal);
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _needsReconcile;

        internal Databank(DatabankCatalogue catalogue, EventBus events, IDatabankSources sources)
        {
            _catalogue = catalogue != null ? catalogue : throw new ArgumentNullException(nameof(catalogue));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                _infos.Add(entry.Id, entry.ToInfo());
            }

            Seed();
            _subscriptions.Add(events.Subscribe<StoryScanned>(OnStoryScanned));
            _subscriptions.Add(events.Subscribe<FragmentScanned>(OnFragmentScanned));
            _subscriptions.Add(events.Subscribe<OpeningMilestoneReached>(OnMilestoneReached));
            _subscriptions.Add(events.Subscribe<BootCheckFinished>(_ => OnWoke()));
        }

        public IReadOnlyList<string> UnlockedIds
        {
            get
            {
                Reconcile();
                return _unlocked;
            }
        }

        public DatabankProgress Progress(string entryId)
        {
            Reconcile();
            if (!_unlockedSet.Contains(entryId))
            {
                return new DatabankProgress(0, 0);
            }

            return TryGetSet(entryId, out string setId)
                ? new DatabankProgress(_found.TryGetValue(entryId, out int found) ? found : 0, _sources.SetSize(setId))
                : new DatabankProgress(1, 1);
        }

        public bool IsUnread(string entryId)
        {
            Reconcile();
            return _unlockedSet.Contains(entryId) && !_read.Contains(entryId);
        }

        public void MarkRead(string entryId)
        {
            Reconcile();
            if (_unlockedSet.Contains(entryId))
            {
                _read.Add(entryId);
            }
        }

        public bool TryGetEntry(string entryId, out DatabankEntryInfo entry)
        {
            return _infos.TryGetValue(entryId ?? string.Empty, out entry);
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        public DatabankSaveData Capture()
        {
            Reconcile();
            var progressIds = new List<string>();
            var progressFound = new List<int>();
            foreach (KeyValuePair<string, int> pair in _found)
            {
                progressIds.Add(pair.Key);
                progressFound.Add(pair.Value);
            }

            var read = new string[_read.Count];
            _read.CopyTo(read);
            return new DatabankSaveData
            {
                unlocked = _unlocked.ToArray(), progressIds = progressIds.ToArray(),
                progressFound = progressFound.ToArray(), read = read,
            };
        }

        /// <summary>
        /// A loaded game's databank, silently. Ids the catalogue does not know are dropped; entries the catalogue
        /// gained since the save was written are unlocked by their rules the first time the databank is read.
        /// </summary>
        public void Restore(DatabankSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _unlocked.Clear();
            _unlockedSet.Clear();
            _read.Clear();
            _found.Clear();
            foreach (string id in data.unlocked ?? Array.Empty<string>())
            {
                if (_infos.ContainsKey(id) && _unlockedSet.Add(id))
                {
                    _unlocked.Add(id);
                }
            }

            string[] ids = data.progressIds ?? Array.Empty<string>();
            int[] found = data.progressFound ?? Array.Empty<int>();
            for (int i = 0; i < ids.Length && i < found.Length; i++)
            {
                if (_unlockedSet.Contains(ids[i]) && TryGetSet(ids[i], out _))
                {
                    _found[ids[i]] = Math.Max(0, found[i]);
                }
            }

            foreach (string id in data.read ?? Array.Empty<string>())
            {
                if (_unlockedSet.Contains(id))
                {
                    _read.Add(id);
                }
            }

            Seed();
            _needsReconcile = true;
        }

        private void Seed()
        {
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                if (entry.Unlock.Kind == DatabankUnlockKind.Seeded)
                {
                    Add(entry, false);
                }
            }
        }

        /// <summary>Unlocks, silently, what the scans and milestones already say is found (an older save).</summary>
        private void Reconcile()
        {
            if (!_needsReconcile)
            {
                return;
            }

            _needsReconcile = false;
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                DatabankUnlock rule = entry.Unlock;
                switch (rule.Kind)
                {
                    case DatabankUnlockKind.StoryScan:
                        if (_sources.IsStoryScanned(rule.Target))
                        {
                            Add(entry, false);
                        }

                        break;
                    case DatabankUnlockKind.FragmentOf:
                        ReconcileFragmentOf(entry, rule);
                        break;
                    case DatabankUnlockKind.SetComplete:
                        int size = _sources.SetSize(rule.Target);
                        if (size > 0 && _sources.ScannedInSet(rule.Target) >= size)
                        {
                            Add(entry, false);
                        }

                        break;
                    case DatabankUnlockKind.Milestone:
                        if (_sources.HasReached(rule.Milestone))
                        {
                            Add(entry, false);
                        }

                        break;
                    case DatabankUnlockKind.Wake:
                        Add(entry, false);
                        break;
                }
            }
        }

        private void ReconcileFragmentOf(DatabankEntry entry, DatabankUnlock rule)
        {
            if (!rule.NamesSeveralSets)
            {
                int scanned = _sources.ScannedInSet(rule.Target);
                if (scanned > 0)
                {
                    Count(entry, scanned, false);
                }

                return;
            }

            string[] sets = rule.Target.Split(',');
            for (int i = 0; i < sets.Length; i++)
            {
                if (_sources.ScannedInSet(sets[i]) > 0)
                {
                    Add(entry, false);
                    return;
                }
            }
        }

        private void OnStoryScanned(StoryScanned scanned)
        {
            Reconcile();
            string id = scanned.StoryId.StartsWith(StoryPrefix, StringComparison.Ordinal)
                ? scanned.StoryId.Substring(StoryPrefix.Length)
                : scanned.StoryId;
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                if (entry.Unlock.Kind == DatabankUnlockKind.StoryScan &&
                    string.Equals(entry.Unlock.Target, id, StringComparison.Ordinal))
                {
                    Add(entry, true);
                }
            }
        }

        private void OnFragmentScanned(FragmentScanned scanned)
        {
            Reconcile();
            if (!TrySetOf(scanned.FragmentId, out string setId))
            {
                return;
            }

            int found = scanned.Index + 1;
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                DatabankUnlock rule = entry.Unlock;
                if (!rule.NamesSet(setId))
                {
                    continue;
                }

                if (rule.Kind == DatabankUnlockKind.FragmentOf)
                {
                    if (rule.NamesSeveralSets)
                    {
                        Add(entry, true);
                    }
                    else
                    {
                        Count(entry, found, true);
                    }
                }
                else if (rule.Kind == DatabankUnlockKind.SetComplete && found >= scanned.Count)
                {
                    Add(entry, true);
                }
            }
        }

        private void OnMilestoneReached(OpeningMilestoneReached reached)
        {
            Reconcile();
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                if (entry.Unlock.Kind == DatabankUnlockKind.Milestone && entry.Unlock.Milestone == reached.Milestone)
                {
                    Add(entry, !reached.Restored);
                }
            }
        }

        /// <summary>The New Game self-test is over: what 07 read as it woke joins the databank, without a line.</summary>
        private void OnWoke()
        {
            Reconcile();
            for (int i = 0; i < _catalogue.Count; i++)
            {
                DatabankEntry entry = _catalogue.EntryAt(i);
                if (entry.Unlock.Kind == DatabankUnlockKind.Wake)
                {
                    Add(entry, false);
                }
            }
        }

        /// <summary>Unlocks <paramref name="entry"/> whole when it is not yet; true when it was added now.</summary>
        private bool Add(DatabankEntry entry, bool announce)
        {
            if (!_unlockedSet.Add(entry.Id))
            {
                return false;
            }

            _unlocked.Add(entry.Id);
            if (announce)
            {
                _events.Publish(new DatabankEntryAdded(entry.Id, entry.Category));
            }

            return true;
        }

        /// <summary>A single-set entry has <paramref name="found"/> fragments of its set: unlocks it, counts it up.</summary>
        private void Count(DatabankEntry entry, int found, bool announce)
        {
            Add(entry, announce);
            _found.TryGetValue(entry.Id, out int before);
            if (found <= before)
            {
                return;
            }

            _found[entry.Id] = found;
            _read.Remove(entry.Id);
            if (announce)
            {
                _events.Publish(new DatabankEntryProgressed(entry.Id, found, _sources.SetSize(entry.Unlock.Target)));
            }
        }

        private bool TryGetSet(string entryId, out string setId)
        {
            setId = null;
            if (_catalogue.TryGet(entryId, out DatabankEntry entry) &&
                entry.Unlock.Kind == DatabankUnlockKind.FragmentOf && !entry.Unlock.NamesSeveralSets)
            {
                setId = entry.Unlock.Target;
                return true;
            }

            return false;
        }

        /// <summary>"ruin.&lt;set&gt;.&lt;n&gt;": the set a fragment belongs to.</summary>
        private static bool TrySetOf(string fragmentId, out string setId)
        {
            setId = null;
            if (!fragmentId.StartsWith(SetPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int dot = fragmentId.LastIndexOf('.');
            if (dot <= SetPrefix.Length)
            {
                return false;
            }

            setId = fragmentId.Substring(SetPrefix.Length, dot - SetPrefix.Length);
            return true;
        }
    }
}
