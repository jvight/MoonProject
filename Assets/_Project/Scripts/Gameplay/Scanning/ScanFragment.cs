using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Art's scan fragments set on its host's FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; (docs/design/0.5.5
    /// -owner-feedback.md §1.2), or a <c>Scan_1..N</c> target on an R1 ruin piece (RUINS-WORLD): its <c>Solid</c> and
    /// white <c>Lines</c> drawing driven by <c>_ScanProgress</c> (the cyan sweep climbing it) and <c>_Dissolve</c>
    /// (the 900 ms close), its four corner <c>Brackets</c>, the <c>ScanTarget</c> 07 looks at, and the faint
    /// <c>DustOutline</c> it leaves, faded with <c>_Fade</c>. Plain state; <see cref="ScanField"/> drives it.
    /// Allocation-free after construction.
    /// </summary>
    public sealed class ScanFragment : IScanTarget
    {
        public const string SocketPrefix = "FragmentSocket_";
        public const string PrefabPrefix = "Fragment_";
        public const string RuinScanPrefix = "Scan_";

        private const string SolidName = "Solid";
        private const string LinesName = "Lines";
        private const string ScanTargetName = "ScanTarget";
        private const string BracketsName = "Brackets";
        private const string DustOutlineName = "DustOutline";

        private static readonly int ProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");

        private const float EndBandShare = 0.15f;
        private const float MinBandHeight = 0.01f;
        private const float RuinSweepHeight = 0.7f;

        private readonly Renderer _solid;
        private readonly Renderer _lines;
        private readonly Renderer _outline;
        private readonly Transform _target;
        private readonly GameObject _brackets;
        private readonly bool _isRuin;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private ScanFragment(string fragmentId, string blueprintId, IReadOnlyList<string> structureIds, int index,
            int count, ScanSize size, string hostId, Transform root, Renderer solid, Renderer lines, Renderer outline,
            Transform target, GameObject brackets, bool isRuin = false, string storyKey = null)
        {
            StoryKey = storyKey;
            FragmentId = fragmentId;
            BlueprintId = blueprintId;
            StructureIds = structureIds;
            Index = index;
            Count = count;
            Size = size;
            HostId = hostId;
            Root = root;
            _solid = solid;
            _lines = lines;
            _outline = outline;
            _target = target;
            _brackets = brackets;
            _isRuin = isRuin;
            SweepSpan = ComputeSweepSpan(solid);
        }

        /// <summary>"&lt;kit id&gt;.&lt;n&gt;", e.g. "rover.cargo_cradle.0". Saved: never rename.</summary>
        public string FragmentId { get; }

        public string BlueprintId { get; }

        /// <summary>Structure blueprint ids taught by this ruin's scan set, or null for rover upgrade fragments.</summary>
        public IReadOnlyList<string> StructureIds { get; }

        /// <summary>A story scan's readout key (<see cref="StoryScan"/>), or null for a teaching fragment.</summary>
        public string StoryKey { get; }

        /// <summary>Its number in its set (the &lt;n&gt; of its socket).</summary>
        public int Index { get; }

        /// <summary>How many fragments its set has.</summary>
        public int Count { get; }

        public ScanSize Size { get; }

        /// <summary>The site, derelict or place it lies on (its host's id).</summary>
        public string HostId { get; }

        /// <summary>Its place in the scan field's place list (<see cref="IScanSites"/>).</summary>
        internal int Place { get; set; }

        public Transform Root { get; }

        public Vector3 Point => _target.position;

        /// <summary>World-space vector from the bottom of the solid to its top along the tilted fragment.</summary>
        public Vector3 SweepSpan { get; }

        public float Progress { get; internal set; }

        /// <summary>Fully scanned: dissolving, or gone for good.</summary>
        public bool Scanned { get; internal set; }

        /// <summary>When its dissolve began (negative infinity for one a save loaded scanned).</summary>
        internal float ScannedAt { get; set; } = float.NegativeInfinity;

        /// <summary>The scanned dissolve and its dust outline have run their course: nothing of it shows.</summary>
        internal bool Gone { get; set; }

        /// <summary>Metal a known blueprint's fragment still owes: it arrives once its dissolve is done.</summary>
        internal int Owed { get; set; }

        /// <summary>
        /// Reads Art's fragment prefab instance under <paramref name="root"/>; null and the broken node in
        /// <paramref name="problem"/> when one is missing.
        /// </summary>
        public static ScanFragment Read(string fragmentId, string blueprintId, int index, int count, ScanSize size,
            string hostId, Transform root, out string problem)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Renderer solid = Renderer(root, SolidName);
            Renderer lines = Renderer(root, LinesName);
            Renderer outline = Renderer(root, DustOutlineName);
            Transform target = root.Find(ScanTargetName);
            Transform brackets = root.Find(BracketsName);
            problem = solid == null ? SolidName
                : lines == null ? LinesName
                : outline == null ? DustOutlineName
                : target == null ? ScanTargetName
                : brackets == null ? BracketsName
                : null;
            if (problem != null)
            {
                problem = $"Art's fragment '{root.name}' has no '{problem}' node (scan fragment contract, 0.5.5 §1)";
                return null;
            }

            return new ScanFragment(fragmentId, blueprintId, null, index, count, size, hostId, root, solid, lines,
                outline, target, brackets.gameObject);
        }

        /// <summary>Wraps a <c>Scan_1..N</c> empty transform on an R1 ruin piece as a scan target.</summary>
        public static ScanFragment ForRuinTarget(string fragmentId, string blueprintId,
            IReadOnlyList<string> structureIds, int index, int count, ScanSize size, string hostId, Transform target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Renderer ruinSolid = FindClosestRuinRenderer(target);
            return new ScanFragment(fragmentId, blueprintId, structureIds, index, count, size, hostId, target, ruinSolid,
                null, null, target, null, isRuin: true);
        }

        /// <summary>Wraps a story scan's point on Art's mesh node <paramref name="target"/>'s parent.</summary>
        public static ScanFragment ForStoryTarget(StoryScan story, string hostId, Transform target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return new ScanFragment(story.FragmentId, story.FragmentId, Array.Empty<string>(), 0, 1, story.Size, hostId,
                target, FindClosestRuinRenderer(target), null, null, target, null, isRuin: true,
                storyKey: story.TextKey);
        }

        /// <summary>The corner brackets: shown while it waits to be scanned and 07 is near enough.</summary>
        internal void ShowBrackets(bool show)
        {
            if (_brackets != null && _brackets.activeSelf != show)
            {
                _brackets.SetActive(show);
            }
        }

        /// <summary>The sweep has climbed <paramref name="progress"/> (0..1) of it; what it passed is drawn.</summary>
        internal void Draw(float progress)
        {
            if (_solid != null)
            {
                Apply(_solid, ProgressId, progress);
                if (_lines != null)
                {
                    Apply(_lines, ProgressId, progress);
                }
            }
        }

        /// <summary>The close, 0..1: the solid fades, then the lines unthread into 07's lens.</summary>
        internal void Dissolve(float t)
        {
            if (_solid != null)
            {
                Apply(_solid, DissolveId, t);
                if (_lines != null)
                {
                    Apply(_lines, DissolveId, t);
                }
            }
        }

        /// <summary>The dissolve is done: only the faint dust outline remains, faded by <paramref name="fade"/>.
        /// </summary>
        internal void LeaveOutline(float fade)
        {
            if (_outline == null)
            {
                return;
            }

            if (_solid.gameObject.activeSelf)
            {
                _solid.gameObject.SetActive(false);
                if (_lines != null)
                {
                    _lines.gameObject.SetActive(false);
                }

                _outline.gameObject.SetActive(true);
            }

            Apply(_outline, FadeId, fade);
        }

        /// <summary>Nothing of it shows any more (scanned and faded, or a save loaded it scanned).</summary>
        internal void Hide()
        {
            if (_isRuin)
            {
                if (_solid != null)
                {
                    Apply(_solid, ProgressId, 0f);
                    Apply(_solid, DissolveId, 0f);
                }

                return;
            }

            if (_solid != null)
            {
                Root.gameObject.SetActive(false);
            }
        }

        private static Renderer FindClosestRuinRenderer(Transform target)
        {
            Transform root = target.parent != null ? target.parent : target;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Renderer best = null;
            float bestScore = float.MaxValue;
            Vector3 point = target.position;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                {
                    continue;
                }

                Bounds b = r.bounds;
                float boxDistSq = b.SqrDistance(point);
                float centerDistSq = (b.center - point).sqrMagnitude;
                float score = boxDistSq * 100f + centerDistSq;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = r;
                }
            }

            return best;
        }

        private void Apply(Renderer renderer, int property, float value)
        {
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(property, value);
            renderer.SetPropertyBlock(_block);
        }

        private static Renderer Renderer(Transform root, string name)
        {
            Transform node = root.Find(name);
            return node != null ? node.GetComponent<Renderer>() : null;
        }

        private static Vector3 ComputeSweepSpan(Renderer solid)
        {
            if (solid == null)
            {
                return new Vector3(0f, RuinSweepHeight, 0f);
            }

            Bounds bounds = solid.bounds;
            MeshFilter filter = solid.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
            {
                return new Vector3(0f, bounds.size.y, 0f);
            }

            Vector3[] vertices = mesh.vertices;
            Transform tr = solid.transform;
            float minY = bounds.min.y;
            float maxY = bounds.max.y;
            float band = Mathf.Max(bounds.size.y * EndBandShare, MinBandHeight);
            Vector3 bottomSum = Vector3.zero;
            int bottomCount = 0;
            Vector3 topSum = Vector3.zero;
            int topCount = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 w = tr.TransformPoint(vertices[i]);
                if (w.y <= minY + band)
                {
                    bottomSum += w;
                    bottomCount++;
                }

                if (w.y >= maxY - band)
                {
                    topSum += w;
                    topCount++;
                }
            }

            if (bottomCount == 0 || topCount == 0)
            {
                return new Vector3(0f, bounds.size.y, 0f);
            }

            Vector3 bottom = bottomSum / bottomCount;
            Vector3 top = topSum / topCount;
            return new Vector3(top.x - bottom.x, bounds.size.y, top.z - bottom.z);
        }
    }
}
