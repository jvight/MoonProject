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
    /// Scanning (docs/design/0.5.5-owner-feedback.md §1, opening-v2 §1): the <c>Scan_1..N</c> targets of the ruin
    /// pieces and of the lander's cargo bay, and the story scans on Art's mesh nodes. Looking at one in reach and
    /// holding E, with no other E verb before it, eases 07 to a stop and scans it (<see cref="ScanStarted"/>, the
    /// <see cref="FeelKind.Scan"/> beats: Anticipate, Act at each quarter, Cancel on letting go, which keeps the
    /// progress); every second of it costs battery (opening-v2 §10). Fully scanned, it dissolves
    /// (<see cref="FragmentScanned"/>, Reward at its scan point); a set's last fragment teaches its structures. A set
    /// whose structures are all known pays Metal instead, once the hold has room. A story scan publishes
    /// <see cref="StoryScanned"/> and teaches nothing. A save keeps what is scanned and how far. Allocation-free per
    /// frame.
    /// </summary>
    public sealed class ScanField
    {
        private const int Quarters = 4;
        private const string ScanCompleteKey = "scan.complete";

        private readonly List<ScanFragment> _fragments = new List<ScanFragment>();
        private readonly ScanTuning _tuning;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly MaterialStock _materials;
        private readonly KnownStructures _knownStructures;
        private readonly Func<bool> _othersClaim;
        private readonly BeamWork _beam;
        private ScanFragment _scanning;
        private ScanFragment _aiming;
        private int _quarter;
        private int _owedMetal;
        private bool _holding;
        private bool _gazing;
        private bool _waitRelease;
        private Transform _cachedScanOrigin;
        private Transform _headTransform;
        private Collider[] _roverColliders;
        private MeshFilter[] _roverMeshes;

        /// <param name="othersClaim">
        /// True while another E verb is on offer (a pickup, a cut, a build...): scanning comes last.
        /// </param>
        public ScanField(ScanTuning tuning, GameplayServices services, KnownStructures knownStructures,
            Func<bool> othersClaim)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _knownStructures = knownStructures ?? throw new ArgumentNullException(nameof(knownStructures));
            _othersClaim = othersClaim ?? throw new ArgumentNullException(nameof(othersClaim));
            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _materials = services.Materials;
            _beam = services.Beam;
            _tuning.ApplyShaderGlobals(_tuning.Seconds(ScanSize.Medium));
        }

        internal ScanField(ScanTuning tuning, EventBus events, ISaveService save, MaterialStock materials,
            KnownStructures knownStructures)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _knownStructures = knownStructures ?? throw new ArgumentNullException(nameof(knownStructures));
            _tuning.ApplyShaderGlobals(_tuning.Seconds(ScanSize.Medium));
        }

        public ScanTuning Tuning => _tuning;

        /// <summary>Every scan target in the world (the ruins', the lander's, the story scans).</summary>
        public IReadOnlyList<ScanFragment> Fragments => _fragments;

        /// <summary>The target a hold of E would scan now, or null.</summary>
        public ScanFragment Candidate { get; private set; }

        /// <summary>The target being scanned (07 stopped, its lens open), or null.</summary>
        public ScanFragment Scanning => _scanning;

        /// <summary>
        /// Registers every <c>Scan_1..N</c> target on an R1 ruin piece under <paramref name="ruinRoot"/> for
        /// <paramref name="set"/>. Null when all targets are present, else what breaks the contract.
        /// </summary>
        public string AddRuin(RuinScanSet set, Transform ruinRoot)
        {
            if (string.IsNullOrEmpty(set.RuinId) || ruinRoot == null)
            {
                throw new ArgumentException("A ruin scan host needs a ruin id and a root.", nameof(set));
            }

            for (int i = 0; i < set.Count; i++)
            {
                string targetName = ScanFragment.RuinScanPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
                Transform target = FindChild(ruinRoot, targetName);
                if (target == null)
                {
                    return $"'{set.RuinId}' has no '{targetName}' node";
                }

                string id = FragmentId(set.BlueprintId, i);
                if (Find(id) != null)
                {
                    return $"'{targetName}' on '{set.RuinId}' lies in the world twice";
                }

                _fragments.Add(ScanFragment.ForRuinTarget(id, set.BlueprintId, set.StructureIds, i, set.Count,
                    set.Size(i), set.RuinId, target));
            }

            return null;
        }

        /// <summary>
        /// Registers <paramref name="story"/> on Art's mesh node under <paramref name="ruinRoot"/>: its scan point
        /// stands at the node's renderer centre. Null when the node and its renderer are there, else what breaks the
        /// contract.
        /// </summary>
        public string AddStory(StoryScan story, Transform ruinRoot)
        {
            if (ruinRoot == null)
            {
                throw new ArgumentNullException(nameof(ruinRoot));
            }

            Transform node = FindChild(ruinRoot, story.Node);
            Renderer panel = node != null ? node.GetComponentInChildren<Renderer>() : null;
            if (panel == null)
            {
                return $"'{story.RuinId}' has no '{story.Node}' node with a renderer (story scan {story.FragmentId})";
            }

            if (Find(story.FragmentId) != null)
            {
                return $"the story scan '{story.FragmentId}' lies in the world twice";
            }

            Transform target = new GameObject(story.FragmentId).transform;
            target.SetParent(node, false);
            target.position = panel.bounds.center + story.Offset;
            _fragments.Add(ScanFragment.ForStoryTarget(story, story.RuinId, target));
            return null;
        }

        /// <summary>The target with <paramref name="fragmentId"/>, or null.</summary>
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

        public void Step(float now, float deltaTime)
        {
            for (int i = 0; i < _fragments.Count; i++)
            {
                StepDissolve(_fragments[i], now);
            }

            PayOwed();
            bool held = _input.ExcavateHeld;
            bool claimed = _othersClaim();
            _waitRelease = held && (_waitRelease || claimed && _scanning == null);
            Candidate = claimed ? null : Pick();
            bool wants = held && !_waitRelease && Candidate != null;
            if (_scanning != null && (!wants || Candidate != _scanning))
            {
                Stop();
            }

            if (wants)
            {
                _aiming = Candidate;
                _rig.SetGazeTarget(this, Candidate.Point, _tuning.GazePriority);
                _gazing = true;
            }
            else
            {
                _aiming = null;
                ClearGaze();
            }

            bool playerSteering = Mathf.Abs(_input.Drive.x) > 0.1f || Mathf.Abs(_rover.DriveInput.x) > 0.1f;
            Hold(wants && !playerSteering);
            bool hasLineOfSight = wants && HasLineOfSight(Candidate.Point);
            if (wants && hasLineOfSight && _scanning == null && _rover.Speed <= _tuning.EngageSpeed)
            {
                Begin(Candidate);
            }

            if (_scanning != null && hasLineOfSight)
            {
                Advance(_scanning, now, deltaTime);
            }
        }

        public ScansSaveData Capture()
        {
            int scanned = 0;
            int partial = 0;
            int owed = _owedMetal;
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
        /// A loaded game's scans, silently: scanned targets show as scanned, partial ones their sweep so far, and Metal
        /// still owed arrives as the hold has room (what fits at once). Unknown ids are ignored.
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
                    fragment.Scanned = true;
                    fragment.Progress = 1f;
                    fragment.Gone = true;
                    fragment.Hide();
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

            _owedMetal = Math.Max(0, data.owedMetal);
            Collect(_owedMetal);
        }

        /// <summary>"&lt;set id&gt;.&lt;n&gt;": a target's saved id.</summary>
        public static string FragmentId(string setId, int index)
        {
            return setId + "." + index.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>How many targets of <paramref name="blueprintId"/>'s set are scanned.</summary>
        public int ScannedOf(string blueprintId)
        {
            int scanned = 0;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                scanned += fragment.Scanned &&
                           string.Equals(fragment.BlueprintId, blueprintId, StringComparison.Ordinal) ? 1 : 0;
            }

            return scanned;
        }

        internal void CompleteTarget(ScanFragment fragment, float now)
        {
            Complete(fragment, now);
        }

        /// <summary>A scanned target runs its dissolve; then what its set owes waits for room in the hold.</summary>
        private void StepDissolve(ScanFragment fragment, float now)
        {
            if (!fragment.Scanned || fragment.Gone)
            {
                return;
            }

            float since = now - fragment.ScannedAt;
            if (since < _tuning.DissolveSeconds)
            {
                fragment.Dissolve(since / _tuning.DissolveSeconds);
                return;
            }

            _owedMetal += fragment.Owed;
            fragment.Owed = 0;
            fragment.Gone = true;
            fragment.Hide();
        }

        /// <summary>Owed Metal goes into 07's hold as it has room (never onto the ground); the game saves.</summary>
        private void PayOwed()
        {
            if (Collect(_owedMetal) > 0)
            {
                _save.SaveNow();
            }
        }

        /// <summary>What the hold has room for of <paramref name="owed"/> Metal goes in; returns how much
        /// did.</summary>
        private int Collect(int owed)
        {
            int paid = Math.Min(owed, _materials.Room(SalvageMaterial.Metal));
            if (paid > 0)
            {
                _owedMetal -= paid;
                _materials.Collect(SalvageMaterial.Metal, paid);
            }

            return paid;
        }

        /// <summary>
        /// The target in reach that 07's view rests on most squarely (or the nearest in reach if none lies inside the
        /// camera cone); the one being aimed at or scanned stays the candidate while it is in reach.
        /// </summary>
        private ScanFragment Pick()
        {
            float reachSq = _tuning.Reach * _tuning.Reach;
            ScanFragment nearest = null;
            float nearestSq = float.MaxValue;
            for (int i = 0; i < _fragments.Count; i++)
            {
                ScanFragment fragment = _fragments[i];
                float distSq = (fragment.Point - _rover.Position).sqrMagnitude;
                if (fragment.Scanned || distSq > reachSq)
                {
                    continue;
                }

                if (fragment == _scanning || (_input.ExcavateHeld && fragment == _aiming))
                {
                    return fragment;
                }

                if (distSq < nearestSq)
                {
                    nearestSq = distSq;
                    nearest = fragment;
                }
            }

            ScanFragment looked = LookedAt();
            return looked != null ? looked : nearest;
        }

        /// <summary>
        /// The unscanned target in reach that 07's view rests on most squarely, inside the camera cone, or null: what
        /// the player is looking at, whatever else claims E.
        /// </summary>
        public ScanFragment LookedAt()
        {
            Transform view = _view.Camera.transform;
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

                if (AimScore.TryScore(view.position, view.forward, fragment.Point, _tuning.Cone, float.MaxValue,
                        _tuning.DistanceWeight, out float score) && score < bestScore)
                {
                    bestScore = score;
                    best = fragment;
                }
            }

            return best;
        }

        /// <summary>True when <paramref name="point"/> lies inside the camera cone a scan target is picked in.</summary>
        public bool InView(Vector3 point)
        {
            Transform view = _view.Camera.transform;
            return AimScore.TryScore(view.position, view.forward, point, _tuning.Cone, float.MaxValue,
                _tuning.DistanceWeight, out _);
        }

        private void Hold(bool hold)
        {
            if (_holding != hold)
            {
                _holding = hold;
                _rig.SetHoldStill(this, hold);
            }
        }

        private void ClearGaze()
        {
            if (_gazing)
            {
                _gazing = false;
                _rig.ClearGazeTarget(this);
            }
        }

        private bool HasLineOfSight(Vector3 targetPoint)
        {
            Transform scanOrigin = _rig.ScanOrigin;
            Vector3 origin = scanOrigin.position;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance <= 1e-4f)
            {
                return true;
            }

            Vector3 dir = toTarget / distance;
            float tolerance = _scanning != null ? _tuning.AimToleranceDegrees * 1.5f : _tuning.AimToleranceDegrees;
            if (Vector3.Angle(scanOrigin.forward, dir) > tolerance)
            {
                return false;
            }

            CacheRoverGeometry(scanOrigin);
            return !IsSelfOccluded(origin, dir, distance);
        }

        private void CacheRoverGeometry(Transform scanOrigin)
        {
            if (_cachedScanOrigin == scanOrigin && _roverColliders != null)
            {
                return;
            }

            _cachedScanOrigin = scanOrigin;
            _headTransform = null;
            Transform root = scanOrigin;
            while (root.parent != null)
            {
                if (_headTransform == null && string.Equals(root.name, "Head", StringComparison.Ordinal))
                {
                    _headTransform = root;
                }

                root = root.parent;
            }

            _roverColliders = root.GetComponentsInChildren<Collider>(true);
            _roverMeshes = root.GetComponentsInChildren<MeshFilter>(true);
        }

        private bool IsSelfOccluded(Vector3 origin, Vector3 dir, float distance)
        {
            if (_headTransform != null)
            {
                Vector3 localOrigin = _headTransform.InverseTransformPoint(origin);
                Vector3 localEnd = _headTransform.InverseTransformPoint(origin + dir * distance);
                Vector3 localVec = localEnd - localOrigin;
                float localDist = localVec.magnitude;
                float halfWidth = Mathf.Max(0.08f, Mathf.Abs(localOrigin.x) - 0.03f);
                var headCore = new Bounds(new Vector3(0f, 0.04f, -0.02f), new Vector3(halfWidth * 2f, 0.44f, 0.36f));
                if (localDist > 1e-4f && !headCore.Contains(localOrigin))
                {
                    var localRay = new Ray(localOrigin, localVec / localDist);
                    if (headCore.IntersectRay(localRay, out float hit) && hit >= 0f && hit <= localDist)
                    {
                        return true;
                    }
                }
            }

            var ray = new Ray(origin, dir);
            for (int i = 0; _roverColliders != null && i < _roverColliders.Length; i++)
            {
                Collider col = _roverColliders[i];
                if (col == null || !col.enabled || col.isTrigger || (_headTransform != null &&
                        (col.transform == _headTransform || col.transform.IsChildOf(_headTransform))))
                {
                    continue;
                }

                if (!col.bounds.Contains(origin) && col.Raycast(ray, out RaycastHit hit, distance) &&
                    hit.distance > 0.02f && hit.distance < distance - 0.02f)
                {
                    return true;
                }
            }

            for (int i = 0; _roverMeshes != null && i < _roverMeshes.Length; i++)
            {
                MeshFilter mf = _roverMeshes[i];
                if (mf == null || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy ||
                    !string.Equals(mf.name, "Chassis", StringComparison.Ordinal))
                {
                    continue;
                }

                Transform tr = mf.transform;
                Bounds bounds = mf.sharedMesh.bounds;
                Vector3 localOrigin = tr.InverseTransformPoint(origin);
                if (bounds.Contains(localOrigin))
                {
                    continue;
                }

                Vector3 localEnd = tr.InverseTransformPoint(origin + dir * distance);
                Vector3 localVec = localEnd - localOrigin;
                float localDist = localVec.magnitude;
                if (localDist > 1e-4f)
                {
                    var localRay = new Ray(localOrigin, localVec / localDist);
                    if (bounds.IntersectRay(localRay, out float hitDist) && hitDist > 0.01f &&
                        hitDist < localDist - 0.01f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Begin(ScanFragment fragment)
        {
            float duration = _tuning.Seconds(fragment.Size);
            _tuning.ApplyShaderGlobals(duration);
            _scanning = fragment;
            _quarter = Mathf.FloorToInt(fragment.Progress * Quarters);
            _events.Publish(new ScanStarted(fragment.FragmentId, fragment.Point));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Anticipate, fragment.Point,
                fragment.SweepSpan, fragment.Progress, (1f - fragment.Progress) * duration));
        }

        private void Advance(ScanFragment fragment, float now, float deltaTime)
        {
            float duration = _tuning.Seconds(fragment.Size);
            _tuning.ApplyShaderGlobals(duration);
            fragment.Progress = Mathf.Min(1f, fragment.Progress + deltaTime / duration);
            _beam.Scan(deltaTime);
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

        /// <summary>E let go (or another verb took it): the progress stays, drawn on the target.</summary>
        private void Stop()
        {
            ScanFragment fragment = _scanning;
            _scanning = null;
            _events.Publish(new ScanStopped(fragment.FragmentId, fragment.Progress));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Cancel, fragment.Point, Vector3.up,
                fragment.Progress, 0f));
        }

        /// <summary>
        /// Fully scanned: the dissolve begins and its set learns (a story is read). E must be let go before the next
        /// scan.
        /// </summary>
        private void Complete(ScanFragment fragment, float now)
        {
            _scanning = null;
            _aiming = null;
            _waitRelease = true;
            if (_rig != null)
            {
                Hold(false);
                ClearGaze();
            }

            if (fragment.StoryKey != null)
            {
                CompleteStory(fragment, now);
                return;
            }

            CompleteRuin(fragment, now);
        }

        /// <summary>
        /// A structure set's target is fully scanned (opening-v2 §1 rule: N fragments by complexity): its dissolve
        /// begins and the note says "fragment k of N" of the structure it teaches; the set's last target teaches its
        /// structures. A target of a set whose structures are all known pays Metal instead.
        /// </summary>
        private void CompleteRuin(ScanFragment fragment, float now)
        {
            string firstUnknown = NextUnlearnedStructure(fragment.StructureIds);
            bool known = string.IsNullOrEmpty(firstUnknown);
            int before = ScannedOf(fragment.BlueprintId);
            fragment.Scanned = true;
            fragment.ScannedAt = now;
            fragment.Owed = known ? _tuning.KnownMetal : 0;
            string teaches = fragment.StructureIds.Count > 0 ? fragment.StructureIds[0] : fragment.BlueprintId;
            _events.Publish(new FragmentScanned(fragment.FragmentId, teaches, before, fragment.Count, known,
                fragment.Owed));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, fragment.Point, Vector3.up, 1f, 0f));
            if (!known && before + 1 >= fragment.Count)
            {
                for (string id = firstUnknown; id != null; id = NextUnlearnedStructure(fragment.StructureIds))
                {
                    _knownStructures.Learn(id);
                    _events.Publish(new TickerLine(ScanCompleteKey, StructureIds.NameKey(id), true));
                }

                _events.Publish(new BuildCue(BuildCueKind.StageComplete, fragment.Point));
                _events.Publish(new RoverChirpRequested(ChirpMood.Proud, 1f));
                _events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Reward, fragment.Point, Vector3.up, 1f, 0f));
            }

            _save.SaveNow();
        }

        /// <summary>A story scan is read: its readout shows on the CRT; it teaches and pays nothing.</summary>
        private void CompleteStory(ScanFragment fragment, float now)
        {
            fragment.Scanned = true;
            fragment.ScannedAt = now;
            _events.Publish(new StoryScanned(fragment.FragmentId, fragment.StoryKey, fragment.Point));
            _events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, fragment.Point, Vector3.up, 1f, 0f));
            _save.SaveNow();
        }

        private string NextUnlearnedStructure(IReadOnlyList<string> structureIds)
        {
            for (int i = 0; i < structureIds.Count; i++)
            {
                string id = structureIds[i];
                if (!string.IsNullOrEmpty(id) && !_knownStructures.IsKnown(id))
                {
                    return id;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform node, string name)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindChild(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
