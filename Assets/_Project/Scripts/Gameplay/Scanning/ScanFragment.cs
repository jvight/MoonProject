using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A scan target 07 looks at (opening-v2 §1): a <c>Scan_1..N</c> node on an R1 ruin piece (or on the lander's
    /// cargo bay), or a story scan on Art's mesh node. The nearest renderer of its host shows the scan: the cyan sweep
    /// climbing it (<c>_ScanProgress</c>) and the close (<c>_Dissolve</c>). The scan beam meets that renderer's real
    /// surface: its host's colliders around it, else the box of its mesh's own bounds (<see cref="TryContact"/>).
    /// Plain state; <see cref="ScanField"/> drives it. Allocation-free after construction.
    /// </summary>
    public sealed class ScanFragment : IScanTarget
    {
        public const string RuinScanPrefix = "Scan_";

        private static readonly int ProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private const float SweepHeight = 0.7f;

        /// <summary>Art's collision-only hull meshes (UCX_*): never the renderer that shows a scan.</summary>
        private const string CollisionHullPrefix = "UCX_";

        /// <summary>Metres around the shown renderer within which its host's colliders count as its surface.</summary>
        private const float SurfaceMargin = 0.05f;

        private readonly Transform _target;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly Collider[] _surface;
        private Renderer _solid;

        private ScanFragment(string fragmentId, string blueprintId, IReadOnlyList<string> structureIds, int index,
            int count, ScanSize size, string hostId, Transform target, Transform surfaceRoot, string storyKey)
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
            _solid = FindClosestRenderer();
            _surface = GatherSurface(surfaceRoot != null ? surfaceRoot : HostOf(target), _solid);
            string unshown = WhyItCannotShowTheScan(_solid);
            if (unshown != null)
            {
                Debug.LogError($"Scan target {fragmentId} ({PathOf(target)}): {unshown}, so its scan would draw " +
                    "nothing.", target);
            }
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

        /// <summary>The host's colliders the scan beam lands on (empty: it lands on its mesh's bounds).</summary>
        public IReadOnlyList<Collider> Surface => _surface;

        /// <summary>The renderer that shows the scan, or null.</summary>
        public Renderer Solid => ResolveSolid();

        /// <summary>The way the sweep climbs: up its host's height.</summary>
        public Vector3 SweepSpan
        {
            get
            {
                Renderer solid = ResolveSolid();
                return new Vector3(0f, solid != null ? solid.bounds.size.y : SweepHeight, 0f);
            }
        }

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
        /// <param name="surfaceRoot">The ruin (or crate) whose colliders the scan beam may land on.</param>
        public static ScanFragment ForRuinTarget(string fragmentId, string blueprintId,
            IReadOnlyList<string> structureIds, int index, int count, ScanSize size, string hostId, Transform target,
            Transform surfaceRoot)
        {
            return new ScanFragment(fragmentId, blueprintId, structureIds, index, count, size, hostId, target,
                surfaceRoot, null);
        }

        /// <summary>Wraps a story scan's point on Art's mesh node <paramref name="target"/>'s parent.</summary>
        public static ScanFragment ForStoryTarget(StoryScan story, string hostId, Transform target,
            Transform surfaceRoot)
        {
            return new ScanFragment(story.FragmentId, story.FragmentId, Array.Empty<string>(), 0, 1, story.Size,
                hostId, target, surfaceRoot, story.TextKey);
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

        /// <summary>
        /// Where the scan beam from <paramref name="lens"/> meets this target now: on its real surface, where the
        /// shader draws the sweep line at its progress as seen from <paramref name="camera"/>. False when nothing shows
        /// it.
        /// </summary>
        public bool TryContact(Vector3 lens, Vector3 camera, out Vector3 point, out Vector3 normal)
        {
            point = Point;
            normal = Vector3.up;
            Renderer solid = ResolveSolid();
            if (solid == null)
            {
                return false;
            }

            Bounds bounds = solid.bounds;
            Vector2 toCamera = ScanLine.TowardCamera(bounds.center, camera);
            float height = ScanLine.Height(bounds, toCamera, Progress);
            Vector3 aim = ScanLine.AimPoint(bounds, height, toCamera);
            if (!Hit(solid, lens, aim, out point, out normal))
            {
                Nearest(solid, aim, out point, out normal);
                return true;
            }

            // Once more where the tilted line crosses the face the beam met, so the beam ends on the drawn line.
            float drift = height - ScanLine.SweepHeightOf(point, bounds.center, toCamera);
            Vector3 onLine = ScanLine.AimPoint(bounds, height + drift, toCamera);
            if (Hit(solid, lens, onLine, out Vector3 lined, out Vector3 linedNormal))
            {
                point = lined;
                normal = linedNormal;
            }

            return true;
        }

        private bool Hit(Renderer solid, Vector3 lens, Vector3 aim, out Vector3 point, out Vector3 normal)
        {
            return _surface.Length > 0
                ? SurfaceAim.TryHit(_surface, lens, aim, out point, out normal)
                : SurfaceAim.TryHitBox(BoxOf(solid, out Bounds local), local, lens, aim, out point, out normal);
        }

        private void Nearest(Renderer solid, Vector3 aim, out Vector3 point, out Vector3 normal)
        {
            if (!SurfaceAim.TryClosest(_surface, aim, out point, out normal))
            {
                SurfaceAim.ClosestOnBox(BoxOf(solid, out Bounds local), local, aim, out point, out normal);
            }
        }

        /// <summary>The renderer's mesh bounds and the matrix carrying them (its world bounds where it has no mesh).
        /// </summary>
        private static Matrix4x4 BoxOf(Renderer solid, out Bounds local)
        {
            if (solid.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
            {
                local = filter.sharedMesh.bounds;
                return solid.localToWorldMatrix;
            }

            if (solid is SkinnedMeshRenderer skinned)
            {
                local = skinned.localBounds;
                return solid.localToWorldMatrix;
            }

            local = solid.bounds;
            return Matrix4x4.identity;
        }

        /// <summary>
        /// Why <paramref name="solid"/> cannot draw a scan (no renderer, or a material whose shader has no
        /// <c>_ScanProgress</c>/<c>_Dissolve</c>), or null when every one of its materials can.
        /// </summary>
        internal static string WhyItCannotShowTheScan(Renderer solid)
        {
            if (solid == null)
            {
                return "no renderer on its host shows it";
            }

            Material[] materials = solid.sharedMaterials;
            if (materials.Length == 0)
            {
                return $"its renderer {solid.name} has no material";
            }

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    return $"its renderer {solid.name} has an empty material slot";
                }

                if (!material.HasProperty(ProgressId) || !material.HasProperty(DissolveId))
                {
                    return $"material {material.name} on {solid.name} uses {material.shader.name}, which has no " +
                           "_ScanProgress/_Dissolve";
                }
            }

            return null;
        }

        private static string PathOf(Transform node)
        {
            string path = node.name;
            for (Transform up = node.parent; up != null; up = up.parent)
            {
                path = up.name + "/" + path;
            }

            return path;
        }

        private static Transform HostOf(Transform target)
        {
            return target.parent != null ? target.parent : target;
        }

        /// <summary>
        /// The solid prop colliders under <paramref name="root"/> around <paramref name="solid"/>: a ruin's fitted hull
        /// boxes on the part that shows the scan. One that holds the renderer with room to spare is a hull round it,
        /// not its surface (an open crate's box round what lies in it), and is left out.
        /// </summary>
        private static Collider[] GatherSurface(Transform root, Renderer solid)
        {
            if (solid == null)
            {
                return Array.Empty<Collider>();
            }

            Bounds around = solid.bounds;
            around.Expand(SurfaceMargin * 2f);
            var surface = new List<Collider>();
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Bounds hull = collider.bounds;
                if (!collider.isTrigger && collider.gameObject.layer == Layers.Prop && hull.Intersects(around) &&
                    !(hull.Contains(around.min) && hull.Contains(around.max)))
                {
                    surface.Add(collider);
                }
            }

            return surface.ToArray();
        }

        private Renderer ResolveSolid()
        {
            if (_solid == null || !_solid.enabled || !_solid.gameObject.activeInHierarchy)
            {
                _solid = FindClosestRenderer();
            }

            return _solid;
        }

        private Renderer FindClosestRenderer()
        {
            HostOf(_target).GetComponentsInChildren(true, _renderers);
            Renderer best = null;
            float bestScore = float.MaxValue;
            Vector3 point = _target.position;
            for (int i = 0; i < _renderers.Count; i++)
            {
                Renderer r = _renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy ||
                    r.name.StartsWith(CollisionHullPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Bounds b = r.bounds;
                float score = b.SqrDistance(point) * 100f + (b.center - point).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = r;
                }
            }

            return best;
        }

        private void Apply(int property, float value)
        {
            Renderer solid = ResolveSolid();
            if (solid == null)
            {
                return;
            }

            solid.GetPropertyBlock(_block);
            _block.SetFloat(property, value);
            solid.SetPropertyBlock(_block);
        }
    }
}
