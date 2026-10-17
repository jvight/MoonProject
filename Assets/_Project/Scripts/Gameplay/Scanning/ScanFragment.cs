using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A scan target 07 looks at (opening-v2 §1): a <c>Scan_1..N</c> node on an R1 ruin piece (or on the lander's
    /// cargo bay), or a story scan on Art's mesh node. The nearest renderer of its host shows the scan: the cyan sweep
    /// climbing it (<c>_ScanProgress</c>) and the close (<c>_Dissolve</c>). Plain state; <see cref="ScanField"/>
    /// drives it. Allocation-free after construction.
    /// </summary>
    public sealed class ScanFragment : IScanTarget
    {
        public const string RuinScanPrefix = "Scan_";

        private static readonly int ProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private const float SweepHeight = 0.7f;

        private readonly Renderer _solid;
        private readonly Transform _target;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private ScanFragment(string fragmentId, string blueprintId, IReadOnlyList<string> structureIds, int index,
            int count, ScanSize size, string hostId, Transform target, string storyKey)
        {
            FragmentId = fragmentId;
            BlueprintId = blueprintId;
            StructureIds = structureIds ?? throw new ArgumentNullException(nameof(structureIds));
            Index = index;
            Count = count;
            Size = size;
            HostId = hostId;
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
            Root = target;
            StoryKey = storyKey;
            _solid = FindClosestRenderer(target);
        }

        /// <summary>"&lt;set id&gt;.&lt;n&gt;" (e.g. "ruin.FallenSolarArray.0") or "story.&lt;id&gt;". Saved: never
        /// rename.
        /// </summary>
        public string FragmentId { get; }

        public string BlueprintId { get; }

        /// <summary>Structure blueprint ids its set teaches when whole (empty for a story scan).</summary>
        public IReadOnlyList<string> StructureIds { get; }

        /// <summary>A story scan's readout key (<see cref="StoryScan"/>), or null for a teaching fragment.</summary>
        public string StoryKey { get; }

        /// <summary>Its number in its set.</summary>
        public int Index { get; }

        /// <summary>How many fragments its set has.</summary>
        public int Count { get; }

        public ScanSize Size { get; }

        /// <summary>The ruin piece or place it lies on.</summary>
        public string HostId { get; }

        public Transform Root { get; }

        public Vector3 Point => _target.position;

        /// <summary>The way the sweep climbs: up its host's height.</summary>
        public Vector3 SweepSpan => new Vector3(0f, _solid != null ? _solid.bounds.size.y : SweepHeight, 0f);

        public float Progress { get; internal set; }

        /// <summary>Fully scanned: dissolving, or gone for good.</summary>
        public bool Scanned { get; internal set; }

        /// <summary>When its dissolve began (negative infinity for one a save loaded scanned).</summary>
        internal float ScannedAt { get; set; } = float.NegativeInfinity;

        /// <summary>The scanned dissolve has run its course.</summary>
        internal bool Gone { get; set; }

        /// <summary>Metal a known blueprint's fragment still owes: it arrives once its dissolve is done.</summary>
        internal int Owed { get; set; }

        /// <summary>Wraps a <c>Scan_1..N</c> node on an R1 ruin piece as a target of a structure set.</summary>
        public static ScanFragment ForRuinTarget(string fragmentId, string blueprintId,
            IReadOnlyList<string> structureIds, int index, int count, ScanSize size, string hostId, Transform target)
        {
            return new ScanFragment(fragmentId, blueprintId, structureIds, index, count, size, hostId, target, null);
        }

        /// <summary>Wraps a story scan's point on Art's mesh node <paramref name="target"/>'s parent.</summary>
        public static ScanFragment ForStoryTarget(StoryScan story, string hostId, Transform target)
        {
            return new ScanFragment(story.FragmentId, story.FragmentId, Array.Empty<string>(), 0, 1, story.Size,
                hostId, target, story.TextKey);
        }

        /// <summary>The sweep has climbed <paramref name="progress"/> (0..1) of it.</summary>
        internal void Draw(float progress)
        {
            Apply(ProgressId, progress);
        }

        /// <summary>The close, 0..1.</summary>
        internal void Dissolve(float t)
        {
            Apply(DissolveId, t);
        }

        /// <summary>The close is done: its host shows as it was before the scan.</summary>
        internal void Hide()
        {
            Apply(ProgressId, 0f);
            Apply(DissolveId, 0f);
        }

        private static Renderer FindClosestRenderer(Transform target)
        {
            Transform root = target.parent != null ? target.parent : target;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Renderer best = null;
            float bestScore = float.MaxValue;
            Vector3 point = target.position;
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds b = renderers[i].bounds;
                float score = b.SqrDistance(point) * 100f + (b.center - point).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = renderers[i];
                }
            }

            return best;
        }

        private void Apply(int property, float value)
        {
            if (_solid == null)
            {
                return;
            }

            _solid.GetPropertyBlock(_block);
            _block.SetFloat(property, value);
            _solid.SetPropertyBlock(_block);
        }
    }
}
