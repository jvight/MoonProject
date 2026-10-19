using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The real cables between the base's built pieces (owner's playtest: the build preview showed a cable, the built
    /// piece none). Every built piece that joins the grid gets the cable its build ghost showed
    /// (<see cref="GridLinkPreview.TryFindCable"/>): to the nearest hub built before it, or a pad on the apron to the
    /// lander's cable channel, lying on the ground with a plug at each port (<see cref="BaseCablePath"/>). The cables
    /// follow the base whenever a piece is built, taken down or restored, and keep their meshes when their ends stay
    /// put. Nothing runs per frame; one mesh per cable.
    /// </summary>
    public sealed class BaseCables : IDisposable
    {
        // Two ends this close are the same end: the cable between them needs no new mesh.
        private const float SameEnd = 1e-3f;

        private readonly PlacedBase _placed;
        private readonly BuildingTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly Material _material;
        private readonly Transform _parent;
        private readonly BaseCableMesh _mesher = new BaseCableMesh();
        private readonly Vector3[] _points = new Vector3[BaseCablePath.MaxPoints];
        private readonly List<Cable> _cables = new List<Cable>();
        private readonly Stack<Cable> _free = new Stack<Cable>();
        private readonly IDisposable _builtSubscription;
        private readonly IDisposable _removedSubscription;
        private ApronCable _apron;

        public BaseCables(EventBus events, PlacedBase placed, BuildingTuning tuning, ITerrainQuery terrain,
            Material material, Transform parent)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _placed = placed ?? throw new ArgumentNullException(nameof(placed));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _material = material != null ? material : throw new ArgumentNullException(nameof(material));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _builtSubscription = events.Subscribe<StructureBuilt>(_ => Follow());
            _removedSubscription = events.Subscribe<StructureRemoved>(_ => Follow());
        }

        /// <summary>How many cables lie between the built pieces.</summary>
        public int Count => _cables.Count;

        /// <summary>Where cable <paramref name="index"/> runs: from a piece's port to the far end.</summary>
        public void GetEnds(int index, out Vector3 from, out Vector3 to)
        {
            from = _cables[index].From;
            to = _cables[index].To;
        }

        /// <summary>The opening laid the lander's apron cable out: a pad on the apron cables to its channel.</summary>
        public void SetApronCable(ApronCable apron)
        {
            _apron = apron;
            Follow();
        }

        public void Dispose()
        {
            _builtSubscription.Dispose();
            _removedSubscription.Dispose();
            foreach (Cable cable in _cables)
            {
                cable.Destroy();
            }

            foreach (Cable cable in _free)
            {
                cable.Destroy();
            }

            _cables.Clear();
            _free.Clear();
        }

        /// <summary>Lays the cables the built base has now, reusing every cable whose ends have not moved.</summary>
        private void Follow()
        {
            for (int i = 0; i < _cables.Count; i++)
            {
                _cables[i].Kept = false;
            }

            for (int i = 0; i < _placed.Count; i++)
            {
                PlacedStructure piece = _placed.StructureAt(i);
                if (piece.IsBuilt && GridLinkPreview.TryFindCable(_placed, piece.Definition, piece.Pose, i,
                        piece.InstanceId, _apron, out Vector3 from, out Vector3 to, out _))
                {
                    Keep(from, to);
                }
            }

            for (int i = _cables.Count - 1; i >= 0; i--)
            {
                if (!_cables[i].Kept)
                {
                    _cables[i].Hide();
                    _free.Push(_cables[i]);
                    _cables.RemoveAt(i);
                }
            }
        }

        private void Keep(Vector3 from, Vector3 to)
        {
            for (int i = 0; i < _cables.Count; i++)
            {
                Cable cable = _cables[i];
                if (!cable.Kept && (cable.From - from).sqrMagnitude < SameEnd * SameEnd
                    && (cable.To - to).sqrMagnitude < SameEnd * SameEnd)
                {
                    cable.Kept = true;
                    return;
                }
            }

            Cable laid = _free.Count > 0 ? _free.Pop() : new Cable(_parent, _material);
            int count = BaseCablePath.Write(from, to, _terrain, _tuning, _points);
            laid.Lay(from, to, _mesher, _points, count, _tuning);
            laid.Kept = true;
            _cables.Add(laid);
        }

        /// <summary>One cable: its object, renderer and own mesh, reused when the base changes.</summary>
        private sealed class Cable
        {
            private readonly GameObject _host;
            private readonly MeshRenderer _renderer;
            private readonly Mesh _mesh;

            public Cable(Transform parent, Material material)
            {
                _host = new GameObject("BaseCable");
                _host.transform.SetParent(parent, false);

                // The cable's mesh is laid out in world space, so its object sits at the world's origin.
                _host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                _mesh = new Mesh { name = "BaseCable" };
                _host.AddComponent<MeshFilter>().sharedMesh = _mesh;
                _renderer = _host.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.shadowCastingMode = ShadowCastingMode.On;
                _renderer.lightProbeUsage = LightProbeUsage.Off;
                _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            public Vector3 From { get; private set; }

            public Vector3 To { get; private set; }

            public bool Kept { get; set; }

            public void Lay(Vector3 from, Vector3 to, BaseCableMesh mesher, Vector3[] points, int count,
                BuildingTuning tuning)
            {
                From = from;
                To = to;
                mesher.Write(_mesh, points, count, tuning);
                _host.SetActive(true);
            }

            public void Hide()
            {
                _host.SetActive(false);
            }

            public void Destroy()
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_host);
                    UnityEngine.Object.Destroy(_mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_host);
                    UnityEngine.Object.DestroyImmediate(_mesh);
                }
            }
        }
    }
}
