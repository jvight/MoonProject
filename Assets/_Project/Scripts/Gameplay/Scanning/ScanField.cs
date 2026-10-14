using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scanning (docs/design/0.5.5-owner-feedback.md §1): Art's fragments on their hosts' FragmentSocket nodes (the
    /// salvage sites, the derelicts and Kenji's crashed test jumper at the chasm's near lip). A fragment still to scan
    /// shows its corner brackets within reach of 07's eye (farther with the Focus Lens). Looking at one in reach and
    /// holding E, with no other E verb before it, eases 07 to a stop and scans it (<see cref="ScanStarted"/>, the
    /// <see cref="FeelKind.Scan"/> beats: Anticipate, Act at each quarter, Cancel on letting go, which keeps the
    /// progress). Fully scanned, it dissolves into its drawing (<see cref="FragmentScanned"/>, Reward at its scan
    /// point), leaves a dust outline that fades, and the set's last fragment completes the blueprint
    /// (<see cref="Blueprints.Learn"/>). A fragment of a blueprint already known pays Metal after its dissolve. A save
    /// keeps what is scanned and how far. Allocation-free per frame.
    /// </summary>
    public sealed class ScanField
    {
        private const int Quarters = 4;

        private readonly List<ScanFragment> _fragments = new List<ScanFragment>();
        private readonly ScanTuning _tuning;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly MaterialStock _materials;
        private readonly GameplayVisuals _visuals;
        private readonly IRoverAbilities _abilities;
        private readonly Blueprints _blueprints;
        private readonly Func<bool> _othersClaim;
        private ScanFragment _scanning;
        private int _quarter;
        private bool _holding;
        private bool _waitRelease;

        /// <param name="othersClaim">
        /// True while another E verb is on offer (a pickup, a cut, a dig, a restore...): scanning comes last.
        /// </param>
        public ScanField(ScanTuning tuning, GameplayServices services, IRoverAbilities abilities,
            Blueprints blueprints, Func<bool> othersClaim)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            _othersClaim = othersClaim ?? throw new ArgumentNullException(nameof(othersClaim));
            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _materials = services.Materials;
            _visuals = services.Visuals;
        }

        public ScanTuning Tuning => _tuning;

        public IReadOnlyList<ScanFragment> Fragments => _fragments;

        /// <summary>The fragment a hold of E would scan now, or null.</summary>
        public ScanFragment Candidate { get; private set; }

        /// <summary>The fragment being scanned (07 stopped, its lens open), or null.</summary>
        public ScanFragment Scanning => _scanning;

        /// <summary>
        /// Sets Art's fragments on every FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; node under <paramref name="root"/>
        /// (at initialisation). Null when they all stand, else what breaks the contract.
        /// </summary>
        public string AddHost(string hostId, Transform root)
        {
            if (string.IsNullOrEmpty(hostId) || root == null)
            {
                throw new ArgumentException("A scan host needs an id and a root.", nameof(hostId));
            }

            var sockets = new List<Transform>();
            Sockets(root, sockets);
            foreach (Transform socket in sockets)
            {
                string problem = AddFragment(hostId, socket);
                if (problem != null)
                {
                    return $"'{hostId}': {problem}";
                }
            }

            return null;
        }

        /// <summary>
        /// Null when every set lies in the world exactly once and teaches one of <paramref name="kits"/>, and every
        /// kit has a set; else the first broken set.
        /// </summary>
        public string Validate(IEnumerable<string> kits)
        {
            var board = new HashSet<string>(kits ?? throw new ArgumentNullException(nameof(kits)),
                StringComparer.Ordinal);
            var taught = new HashSet<string>(StringComparer.Ordinal);
            foreach (ScanSet set in _tuning.Sets)
            {
                if (!board.Contains(set.KitId) || !taught.Add(set.KitId) || set.Count == 0)
                {
                    return $"scan set '{set.Blueprint}' teaches '{set.KitId}', which is no kit of the bay, " +
                           "or another set teaches it too";
                }

                for (int i = 0; i < set.Count; i++)
                {
                    if (Find(FragmentId(set.KitId, i)) == null)
                    {
                        return $"no host holds FragmentSocket_{set.Blueprint}_{i}";
                    }
                }
            }

            foreach (string kit in board)
            {
                if (!taught.Contains(kit))
                {
                    return $"no scan set teaches the bay's kit '{kit}'";
                }
            }

            return null;
        }

        /// <summary>The fragment with <paramref name="fragmentId"/>, or null.</summary>
        public ScanFragment Find(string fragmentId)
        {
            for (int i = 0; i < _fragments.Count; i++)
            {
                if (string.Equals(_fragments[i].FragmentId, fragmentId, StringComparison.Ordinal))
                {
                    return _fragments[i];
                }
            }

            return null;
        }

        /// <summary>The kit whose fragments lie at <paramref name="hostId"/>, or null.</summary>
        public string KitAt(string hostId)
        {
            for (int i = 0; i < _fragments.Count; i++)
            {
                if (string.Equals(_fragments[i].HostId, hostId, StringComparison.Ordinal))
                {
                    return _fragments[i].BlueprintId;
                }
            }

            return null;
        }

        /// <summary>How many fragments at <paramref name="hostId"/> are still to scan.</summary>
        public int Left(string hostId)
        {
            int left = 0;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                left += !fragment.Scanned && string.Equals(fragment.HostId, hostId, StringComparison.Ordinal) ? 1 : 0;
            }

            return left;
        }

        public void Step(float now, float deltaTime)
        {
            bool lens = _abilities.Has(RoverAbility.FocusLens);
            float bracketRange = lens ? _tuning.LensBracketRange : _tuning.BracketRange;
            for (int i = 0; i < _fragments.Count; i++)
            {
                StepLook(_fragments[i], now, bracketRange);
            }

            bool held = _input.ExcavateHeld;
            bool claimed = _othersClaim();
            _waitRelease = held && (_waitRelease || claimed && _scanning == null);
            Candidate = claimed ? null : Pick();
            bool wants = held && !_waitRelease && Candidate != null;
            if (_scanning != null && (!wants || Candidate != _scanning))
            {
                Stop();
            }

            Hold(wants);
            if (wants && _scanning == null && _rover.Speed <= _tuning.EngageSpeed)
            {
                Begin(Candidate, lens);
            }

            if (_scanning != null)
            {
                Advance(_scanning, now, deltaTime, lens);
            }
        }

        /// <summary>
        /// The fragments of every blueprint already known lie scanned (a save from before scanning, whose tubes were
        /// found or decoded, D1 migration): gone at once, with no event.
        /// </summary>
        public void Sync()
        {
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                if (!fragment.Scanned && _blueprints.IsLearned(fragment.BlueprintId))
                {
                    LieScanned(fragment);
                }
            }
        }

        public ScansSaveData Capture()
        {
            int scanned = 0;
            int partial = 0;
            int owed = 0;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                scanned += fragment.Scanned ? 1 : 0;
                partial += !fragment.Scanned && fragment.Progress > 0f ? 1 : 0;
                owed += fragment.Owed;
            }

            var data = new ScansSaveData
            {
                scanned = new string[scanned], partialIds = new string[partial], partials = new float[partial],
                owedMetal = owed,
            };
            scanned = 0;
            partial = 0;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                if (fragment.Scanned)
                {
                    data.scanned[scanned++] = fragment.FragmentId;
                }
                else if (fragment.Progress > 0f)
                {
                    data.partialIds[partial] = fragment.FragmentId;
                    data.partials[partial++] = fragment.Progress;
                }
            }

            return data;
        }

        /// <summary>
        /// A loaded game's scans, silently: scanned fragments are gone, partial ones show their drawing so far, and
        /// Metal still owed arrives. Unknown ids are ignored.
        /// </summary>
        public void Restore(ScansSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (string id in data.scanned ?? Array.Empty<string>())
            {
                ScanFragment fragment = Find(id);
                if (fragment != null)
                {
                    LieScanned(fragment);
                }
            }

            string[] ids = data.partialIds ?? Array.Empty<string>();
            float[] partials = data.partials ?? Array.Empty<float>();
            for (int i = 0; i < ids.Length && i < partials.Length; i++)
            {
                ScanFragment fragment = Find(ids[i]);
                if (fragment != null && !fragment.Scanned)
                {
                    fragment.Progress = Mathf.Clamp(partials[i], 0f, 0.999f);
                    fragment.Draw(fragment.Progress);
                }
            }

            if (data.owedMetal > 0)
            {
                Pay(data.owedMetal);
            }
        }

        /// <summary>"&lt;kit id&gt;.&lt;n&gt;": a fragment's saved id.</summary>
        public static string FragmentId(string kitId, int index)
        {
            return kitId + "." + index.ToString(CultureInfo.InvariantCulture);
        }

        private string AddFragment(string hostId, Transform socket)
        {
            string tail = socket.name.Substring(ScanFragment.SocketPrefix.Length);
            int split = tail.LastIndexOf('_');
            if (split <= 0 || !int.TryParse(tail.Substring(split + 1), NumberStyles.None, CultureInfo.InvariantCulture,
                    out int index))
            {
                return $"'{socket.name}' is not FragmentSocket_<Blueprint>_<n>";
            }

            string blueprint = tail.Substring(0, split);
            if (!_tuning.TryGetSet(blueprint, out ScanSet set) || index >= set.Count)
            {
                return $"'{socket.name}' is no fragment of ScanTuning's sets";
            }

            string id = FragmentId(set.KitId, index);
            if (Find(id) != null)
            {
                return $"'{socket.name}' lies in the world twice";
            }

            string name = ScanFragment.PrefabPrefix + blueprint + "_" + index.ToString(CultureInfo.InvariantCulture);
            GameObject prefab = _visuals.Fragment(name);
            if (prefab == null)
            {
                return $"Art's fragment '{name}' is missing from GameplayVisuals (run Gameplay/Materials)";
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, socket, false);
            instance.name = name;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            ScanFragment fragment = ScanFragment.Read(id, set.KitId, index, set.Count, set.Size(index), hostId,
                instance.transform, out string problem);
            if (fragment == null)
            {
                return problem;
            }

            fragment.ShowBrackets(false);
            _fragments.Add(fragment);
            return null;
        }

        private static void Sockets(Transform node, List<Transform> into)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                if (child.name.StartsWith(ScanFragment.SocketPrefix, StringComparison.Ordinal))
                {
                    into.Add(child);
                }
                else
                {
                    Sockets(child, into);
                }
            }
        }

        /// <summary>
        /// A fragment still to scan shows its brackets while 07 is near; a scanned one runs its dissolve, pays what it
        /// owes, then leaves its dust outline to fade.
        /// </summary>
        private void StepLook(ScanFragment fragment, float now, float bracketRange)
        {
            if (!fragment.Scanned)
            {
                Vector3 toward = fragment.Point - _rover.Position;
                fragment.ShowBrackets(toward.sqrMagnitude <= bracketRange * bracketRange);
                return;
            }

            if (fragment.Gone)
            {
                return;
            }

            float since = now - fragment.ScannedAt;
            if (since < _tuning.DissolveSeconds)
            {
                fragment.Dissolve(since / _tuning.DissolveSeconds);
                return;
            }

            if (fragment.Owed > 0)
            {
                int owed = fragment.Owed;
                fragment.Owed = 0;
                Pay(owed);
                _save.SaveNow();
            }

            float fade = (since - _tuning.DissolveSeconds) / _tuning.OutlineSeconds;
            if (fade < 1f)
            {
                fragment.LeaveOutline(Ease.InOutSine(fade));
                return;
            }

            fragment.Gone = true;
            fragment.Hide();
        }

        /// <summary>
        /// The fragment in reach that 07's view rests on most squarely; the one being scanned stays the candidate
        /// while it is in reach, so a small turn of the view does not drop it.
        /// </summary>
        private ScanFragment Pick()
        {
            Transform view = _view.Camera.transform;
            Vector3 eye = view.position;
            Vector3 forward = view.forward;
            float reachSq = _tuning.Reach * _tuning.Reach;
            ScanFragment best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                if (fragment.Scanned || (fragment.Point - _rover.Position).sqrMagnitude > reachSq)
                {
                    continue;
                }

                if (fragment == _scanning)
                {
                    return fragment;
                }

                if (TetherAim.TryScore(eye, forward, fragment.Point, _tuning.Cone, float.MaxValue,
                        _tuning.DistanceWeight, out float score) && score < bestScore)
                {
                    bestScore = score;
                    best = fragment;
                }
            }

            return best;
        }

        private void Hold(bool hold)
        {
            if (_holding != hold)
            {
                _holding = hold;
                _rig.SetHoldStill(this, hold);
            }
        }

        private float Seconds(ScanFragment fragment, bool lens)
        {
            return _tuning.Seconds(fragment.Size) / (lens ? _tuning.LensFactor : 1f);
        }

        private void Begin(ScanFragment fragment, bool lens)
        {
            _scanning = fragment;
            _quarter = Mathf.FloorToInt(fragment.Progress * Quarters);
            _events.Publish(new ScanStarted(fragment.FragmentId, fragment.Point));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Anticipate, fragment.Point, Vector3.up,
                fragment.Progress, (1f - fragment.Progress) * Seconds(fragment, lens)));
        }

        private void Advance(ScanFragment fragment, float now, float deltaTime, bool lens)
        {
            fragment.Progress = Mathf.Min(1f, fragment.Progress + deltaTime / Seconds(fragment, lens));
            fragment.Draw(fragment.Progress);
            int quarter = Mathf.FloorToInt(fragment.Progress * Quarters);
            while (_quarter < quarter)
            {
                _quarter++;
                _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Act, fragment.Point, Vector3.up,
                    (float)_quarter / Quarters, 0f));
            }

            if (fragment.Progress >= 1f)
            {
                Complete(fragment, now);
            }
        }

        /// <summary>E let go (or another verb took it): the progress stays, drawn on the fragment.</summary>
        private void Stop()
        {
            ScanFragment fragment = _scanning;
            _scanning = null;
            _events.Publish(new ScanStopped(fragment.FragmentId, fragment.Progress));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Cancel, fragment.Point, Vector3.up,
                fragment.Progress, 0f));
        }

        /// <summary>
        /// Fully scanned: the dissolve begins, new knowledge joins its set (the last fragment completes the
        /// blueprint), and a known blueprint's fragment owes its Metal. E must be let go before the next scan.
        /// </summary>
        private void Complete(ScanFragment fragment, float now)
        {
            _scanning = null;
            _waitRelease = true;
            Hold(false);
            bool known = _blueprints.IsLearned(fragment.BlueprintId);
            int before = ScannedOf(fragment.BlueprintId);
            fragment.Scanned = true;
            fragment.ScannedAt = now;
            fragment.Owed = known ? _tuning.KnownMetal : 0;
            fragment.ShowBrackets(false);
            _events.Publish(new FragmentScanned(fragment.FragmentId, fragment.BlueprintId, before, fragment.Count,
                known, fragment.Owed));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, fragment.Point, Vector3.up, 1f, 0f));
            if (!known && before + 1 >= fragment.Count)
            {
                _blueprints.Learn(fragment.BlueprintId);
            }

            _save.SaveNow();
        }

        private int ScannedOf(string kitId)
        {
            int scanned = 0;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                scanned += fragment.Scanned && string.Equals(fragment.BlueprintId, kitId, StringComparison.Ordinal)
                    ? 1
                    : 0;
            }

            return scanned;
        }

        private static void LieScanned(ScanFragment fragment)
        {
            fragment.Scanned = true;
            fragment.Progress = 1f;
            fragment.Gone = true;
            fragment.Owed = 0;
            fragment.ShowBrackets(false);
            fragment.Hide();
        }

        /// <summary>A known blueprint's Metal goes into 07's hold, else home's overflow crates (§1.2).</summary>
        private void Pay(int metal)
        {
            int left = _materials.Collect(SalvageMaterial.Metal, metal);
            if (left > 0)
            {
                _materials.Home.AddOverflow(SalvageMaterial.Metal, left);
            }
        }
    }
}
