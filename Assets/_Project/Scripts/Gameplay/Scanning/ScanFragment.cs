using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Art's scan fragments set on its host's FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; (docs/design/0.5.5
    /// -owner-feedback.md §1.2): its <c>Solid</c> and white <c>Lines</c> drawing driven by <c>_ScanProgress</c> (the
    /// cyan sweep climbing it) and <c>_Dissolve</c> (the 900 ms close), its four corner <c>Brackets</c>, the
    /// <c>ScanTarget</c> 07 looks at, and the faint <c>DustOutline</c> it leaves, faded with <c>_Fade</c>. Plain state;
    /// <see cref="ScanField"/> drives it. Allocation-free after construction.
    /// </summary>
    public sealed class ScanFragment : IScanTarget
    {
        public const string SocketPrefix = "FragmentSocket_";
        public const string PrefabPrefix = "Fragment_";

        private const string SolidName = "Solid";
        private const string LinesName = "Lines";
        private const string ScanTargetName = "ScanTarget";
        private const string BracketsName = "Brackets";
        private const string DustOutlineName = "DustOutline";

        private static readonly int ProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");

        private readonly Renderer _solid;
        private readonly Renderer _lines;
        private readonly Renderer _outline;
        private readonly Transform _target;
        private readonly GameObject _brackets;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private ScanFragment(string fragmentId, string blueprintId, int index, int count, ScanSize size, string hostId,
            Transform root, Renderer solid, Renderer lines, Renderer outline, Transform target, GameObject brackets)
        {
            FragmentId = fragmentId;
            BlueprintId = blueprintId;
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
        }

        /// <summary>"&lt;kit id&gt;.&lt;n&gt;", e.g. "rover.cargo_cradle.0". Saved: never rename.</summary>
        public string FragmentId { get; }

        public string BlueprintId { get; }

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

            return new ScanFragment(fragmentId, blueprintId, index, count, size, hostId, root, solid, lines, outline,
                target, brackets.gameObject);
        }

        /// <summary>The corner brackets: shown while it waits to be scanned and 07 is near enough.</summary>
        internal void ShowBrackets(bool show)
        {
            if (_brackets.activeSelf != show)
            {
                _brackets.SetActive(show);
            }
        }

        /// <summary>The sweep has climbed <paramref name="progress"/> (0..1) of it; what it passed is drawn.</summary>
        internal void Draw(float progress)
        {
            Apply(_solid, ProgressId, progress);
            Apply(_lines, ProgressId, progress);
        }

        /// <summary>The close, 0..1: the solid fades, then the lines unthread into 07's lens.</summary>
        internal void Dissolve(float t)
        {
            Apply(_solid, DissolveId, t);
            Apply(_lines, DissolveId, t);
        }

        /// <summary>The dissolve is done: only the faint dust outline remains, faded by <paramref name="fade"/>.
        /// </summary>
        internal void LeaveOutline(float fade)
        {
            if (_solid.gameObject.activeSelf)
            {
                _solid.gameObject.SetActive(false);
                _lines.gameObject.SetActive(false);
                _outline.gameObject.SetActive(true);
            }

            Apply(_outline, FadeId, fade);
        }

        /// <summary>Nothing of it shows any more (scanned and faded, or a save loaded it scanned).</summary>
        internal void Hide()
        {
            Root.gameObject.SetActive(false);
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
    }
}
