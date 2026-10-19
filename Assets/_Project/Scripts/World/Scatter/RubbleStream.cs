using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Streams the near-field rubble (owner's Apollo 11 reference) in round the camera instead of building it all at
    /// boot. The rubble only draws within a few tens of metres, so it is planned in small square chunks, nearest first,
    /// within <see cref="ScatterSettings.RubbleStreamRadius"/>, and put away again past that radius plus a hysteresis.
    /// Each frame plans at most <see cref="ScatterSettings.RubbleCellsPerFrame"/> grid cells (a fixed amount of work,
    /// a chunk spread over frames when it needs more), and every stone comes from its own cell's hash, so a chunk is
    /// the same on every visit. Chunks, meshes and buffers are pooled and sized once: a warm stream allocates nothing.
    /// The material fades the rubble out by distance (R1Lit's distance fade), so its far edge thins away, never pops.
    /// </summary>
    public sealed class RubbleStream : IDisposable
    {
        private const int VerticesPerClod = 6;

        private const MeshUpdateFlags UploadFlags = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;

        private static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        private readonly ScatterPlanner _planner;
        private readonly IReadOnlyList<RubbleApron> _aprons;
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly HideFlags _hideFlags;
        private readonly float _chunkSize;
        private readonly float _cell;
        private readonly float _keepRadius;
        private readonly float _showRadius;
        private readonly float _extentRadius;
        private readonly int _budget;
        private readonly int _capacity;
        private readonly int _triangles;
        private readonly Vector3[][] _clodPositions;
        private readonly Vector3[][] _clodNormals;
        private readonly Color32[][] _clodColors;
        private readonly int[] _clodTriangles;
        private readonly Vector2Int[] _offsets;
        private readonly Dictionary<long, Chunk> _byCell;
        private readonly List<Chunk> _resident;
        private readonly Stack<Chunk> _free;
        private readonly List<ScatterInstance> _stones;
        private readonly ScatterVertex[] _vertices;
        private readonly ushort[] _indices;
        private Job _job;
        private bool _hasJob;
        private bool _overflowReported;

        /// <param name="planner">The scatter planner: it plans each rubble cell and apron stone.</param>
        /// <param name="aprons">The boulders that wear aprons of rubble (<see cref="ScatterPlanner.CollectRubbleAprons"/>).</param>
        /// <param name="clods">The clod shapes (<see cref="ClodMesh"/>), all with the same 6-vertex topology.</param>
        /// <param name="material">The rubble's material, with R1Lit's distance fade on.</param>
        /// <param name="extentRadius">Rubble lies only within this distance of home (the drivable floor).</param>
        public RubbleStream(ScatterPlanner planner, IReadOnlyList<RubbleApron> aprons, ScatterSettings settings,
            IReadOnlyList<Mesh> clods, Material material, float extentRadius, Transform parent, HideFlags hideFlags)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _aprons = aprons ?? throw new ArgumentNullException(nameof(aprons));
            if (settings == null || clods == null || clods.Count == 0)
            {
                throw new ArgumentException("The rubble stream needs its settings and at least one clod shape.");
            }

            for (int i = 0; i < aprons.Count; i++)
            {
                if (aprons[i].Count > settings.RubbleCellsPerFrame)
                {
                    throw new ArgumentException($"A boulder's apron ({aprons[i].Count} stones) is more work than a " +
                        "whole frame's rubble budget; raise ScatterSettings' rubble cells per frame.");
                }
            }

            _material = material != null ? material : throw new ArgumentNullException(nameof(material));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _hideFlags = hideFlags;
            _chunkSize = settings.RubbleChunkSize;
            _cell = settings.RubbleSpacing;
            _extentRadius = extentRadius;
            _budget = settings.RubbleCellsPerFrame;
            _capacity = settings.RubbleChunkCapacity;
            float halfDiagonal = _chunkSize * 0.70710678f;
            _keepRadius = settings.RubbleStreamRadius + settings.RubbleStreamHysteresis + halfDiagonal;
            _showRadius = settings.RubbleFadeRange.y + halfDiagonal;

            _clodPositions = new Vector3[clods.Count][];
            _clodNormals = new Vector3[clods.Count][];
            _clodColors = new Color32[clods.Count][];
            for (int i = 0; i < clods.Count; i++)
            {
                Mesh clod = clods[i];
                if (clod == null || !clod.isReadable || clod.vertexCount != VerticesPerClod)
                {
                    throw new ArgumentException($"Clod {i} must be a readable {VerticesPerClod}-vertex mesh.");
                }

                _clodPositions[i] = clod.vertices;
                _clodNormals[i] = clod.normals;
                _clodColors[i] = clod.colors32;
            }

            _clodTriangles = clods[0].triangles;
            _triangles = _clodTriangles.Length;
            _offsets = RingOffsets(settings.RubbleStreamRadius + halfDiagonal, _chunkSize);
            int pool = RingOffsets(_keepRadius + halfDiagonal, _chunkSize).Length;
            _byCell = new Dictionary<long, Chunk>(pool);
            _resident = new List<Chunk>(pool);
            _free = new Stack<Chunk>(pool);
            _stones = new List<ScatterInstance>(_capacity + 1);
            _vertices = new ScatterVertex[_capacity * VerticesPerClod];
            _indices = new ushort[_capacity * _triangles];
            if (_vertices.Length > ushort.MaxValue + 1)
            {
                throw new ArgumentException("A rubble chunk's capacity must fit 16-bit indices.");
            }
        }

        /// <summary>Grid cells and apron stones planned in the last <see cref="Tick"/> (never more than the budget).</summary>
        public int LastFrameWork { get; private set; }

        /// <summary>Chunks streamed in and kept right now.</summary>
        public int ResidentChunks => _resident.Count;

        /// <summary>Stones in the streamed-in chunks right now.</summary>
        public int ResidentStones { get; private set; }

        /// <summary>The chunk (x, z) that <paramref name="position"/> lies in.</summary>
        public Vector2Int ChunkOf(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / _chunkSize), Mathf.FloorToInt(position.z / _chunkSize));
        }

        /// <summary>The chunk's ground square (x, z), metres.</summary>
        public Rect AreaOf(Vector2Int chunk)
        {
            return new Rect(chunk.x * _chunkSize, chunk.y * _chunkSize, _chunkSize, _chunkSize);
        }

        /// <summary>True when the chunk is streamed in: its stones (<paramref name="stones"/>) and their bounds.</summary>
        public bool TryGetChunk(Vector2Int chunk, out int stones, out Bounds bounds)
        {
            if (_byCell.TryGetValue(Key(chunk), out Chunk found))
            {
                stones = found.Stones;
                bounds = found.Bounds;
                return true;
            }

            stones = 0;
            bounds = default;
            return false;
        }

        /// <summary>
        /// Streams round <paramref name="focus"/> (the view camera): puts away chunks gone past the keep radius, shows
        /// or hides the rest by distance, and plans the nearest missing chunks within the frame's budget.
        /// </summary>
        public void Tick(Vector3 focus)
        {
            var flat = new Vector2(focus.x, focus.z);
            Release(flat);
            int work = 0;
            while (work < _budget)
            {
                if (!_hasJob && !TryStartJob(ChunkOf(focus)))
                {
                    break;
                }

                work += Advance(_budget - work, out bool waits);
                if (waits)
                {
                    break;
                }
            }

            LastFrameWork = work;
            for (int i = 0; i < _resident.Count; i++)
            {
                Chunk chunk = _resident[i];
                bool show = chunk.Stones > 0 && Vector2.Distance(flat, chunk.Middle) < _showRadius;
                if (chunk.Renderer.enabled != show)
                {
                    chunk.Renderer.enabled = show;
                }
            }
        }

        public void Dispose()
        {
            foreach (Chunk chunk in _resident)
            {
                chunk.Destroy();
            }

            foreach (Chunk chunk in _free)
            {
                chunk.Destroy();
            }

            _resident.Clear();
            _free.Clear();
            _byCell.Clear();
            _hasJob = false;
        }

        private void Release(Vector2 flat)
        {
            for (int i = _resident.Count - 1; i >= 0; i--)
            {
                Chunk chunk = _resident[i];
                if (Vector2.Distance(flat, chunk.Middle) <= _keepRadius)
                {
                    continue;
                }

                chunk.Renderer.enabled = false;
                ResidentStones -= chunk.Stones;
                _byCell.Remove(Key(chunk.Cell));
                _resident.RemoveAt(i);
                _free.Push(chunk);
            }

            if (_hasJob && Vector2.Distance(flat, AreaOf(_job.Cell).center) > _keepRadius)
            {
                _hasJob = false;
            }
        }

        /// <summary>The nearest chunk within the stream radius that is not streamed in yet becomes the job.</summary>
        private bool TryStartJob(Vector2Int focus)
        {
            for (int i = 0; i < _offsets.Length; i++)
            {
                Vector2Int cell = focus + _offsets[i];
                if (_byCell.ContainsKey(Key(cell)))
                {
                    continue;
                }

                Rect area = AreaOf(cell);
                _job = new Job(cell, area, _cell);
                _hasJob = true;
                _stones.Clear();
                if (NearestToHome(area) > _extentRadius)
                {
                    // Beyond the floor no cell keeps a stone: the chunk streams in empty at no cost.
                    _job.Row = _job.LastRow + 1;
                    _job.Apron = _aprons.Count;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Plans up to <paramref name="allowance"/> units of the job and finishes it when it is all planned;
        /// <paramref name="waits"/> when the next piece of work would not fit this frame.
        /// </summary>
        private int Advance(int allowance, out bool waits)
        {
            waits = false;
            int work = 0;
            Rect area = _job.Area;
            while (work < allowance && _job.Row <= _job.LastRow)
            {
                int before = _stones.Count;
                _planner.PlanRubbleCell(_job.Column, _job.Row, _stones);
                KeepInside(before, area);
                work++;
                if (++_job.Column > _job.LastColumn)
                {
                    _job.Column = _job.FirstColumn;
                    _job.Row++;
                }
            }

            while (_job.Row > _job.LastRow && _job.Apron < _aprons.Count)
            {
                RubbleApron apron = _aprons[_job.Apron];
                if (!Touches(apron, area))
                {
                    _job.Apron++;
                    continue;
                }

                if (work + apron.Count > allowance)
                {
                    waits = true;
                    return work;
                }

                int before = _stones.Count;
                work += _planner.PlanRubbleApron(apron, area, _stones);
                KeepInside(before, area);
                _job.Apron++;
            }

            if (_job.Row > _job.LastRow && _job.Apron >= _aprons.Count)
            {
                Finish();
            }

            waits = work >= allowance && _hasJob;
            return work;
        }

        /// <summary>Drops stones a cell or apron put outside the chunk, and any past its capacity.</summary>
        private void KeepInside(int before, Rect area)
        {
            for (int i = _stones.Count - 1; i >= before; i--)
            {
                Vector3 position = _stones[i].Position;
                bool inside = position.x >= area.xMin && position.x < area.xMax
                    && position.z >= area.yMin && position.z < area.yMax;
                if (!inside || i >= _capacity)
                {
                    if (inside && !_overflowReported)
                    {
                        _overflowReported = true;
                        Debug.LogError($"{nameof(RubbleStream)}: a chunk holds more than {_capacity} stones; raise " +
                            "ScatterSettings' rubble chunk capacity.");
                    }

                    _stones.RemoveAt(i);
                }
            }
        }

        private void Finish()
        {
            Chunk chunk = _free.Count > 0 ? _free.Pop() : new Chunk(_parent, _material, _hideFlags, _capacity,
                _triangles);
            chunk.Place(_job.Cell, _job.Area);
            int vertexCount = _stones.Count * VerticesPerClod;
            int indexCount = _stones.Count * _triangles;
            Bounds bounds = Mesh(chunk.Origin);
            chunk.Upload(_vertices, vertexCount, _indices, indexCount, bounds, _stones.Count);
            _byCell.Add(Key(chunk.Cell), chunk);
            _resident.Add(chunk);
            ResidentStones += chunk.Stones;
            _hasJob = false;
        }

        /// <summary>Writes the job's stones into the shared buffers, relative to <paramref name="origin"/>.</summary>
        private Bounds Mesh(Vector3 origin)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            int written = 0;
            int writtenIndices = 0;
            for (int s = 0; s < _stones.Count; s++)
            {
                ScatterInstance stone = _stones[s];
                int variant = stone.Variant % _clodPositions.Length;
                Vector3[] positions = _clodPositions[variant];
                Vector3[] normals = _clodNormals[variant];
                Color32[] colors = _clodColors[variant];
                Quaternion rotation = ScatterBuilder.Orientation(stone);
                Vector3 scale = stone.ScaleAspect * stone.Size;
                var inverse = new Vector3(1f / Mathf.Max(1e-4f, scale.x), 1f / Mathf.Max(1e-4f, scale.y),
                    1f / Mathf.Max(1e-4f, scale.z));
                Vector3 offset = stone.Position - stone.Normal * stone.BurialDepth - origin;
                int baseVertex = written;
                for (int v = 0; v < VerticesPerClod; v++)
                {
                    Vector3 position = offset + rotation * Vector3.Scale(positions[v], scale);
                    Vector3 normal = (rotation * Vector3.Scale(normals[v], inverse)).normalized;
                    Vector3 across = Vector3.Cross(normal, Vector3.forward);
                    Vector3 tangent = across.sqrMagnitude > 1e-6f ? across.normalized : Vector3.right;
                    _vertices[written++] = new ScatterVertex(position, normal,
                        new Vector4(tangent.x, tangent.y, tangent.z, 1f), colors[v], new Vector2(position.x,
                            position.z));
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }

                for (int t = 0; t < _triangles; t++)
                {
                    _indices[writtenIndices++] = (ushort)(baseVertex + _clodTriangles[t]);
                }
            }

            var bounds = new Bounds();
            if (_stones.Count > 0)
            {
                bounds.SetMinMax(min, max);
            }

            return bounds;
        }

        private static bool Touches(RubbleApron apron, Rect area)
        {
            float x = Mathf.Clamp(apron.Center.x, area.xMin, area.xMax);
            float z = Mathf.Clamp(apron.Center.y, area.yMin, area.yMax);
            float dx = apron.Center.x - x;
            float dz = apron.Center.y - z;
            return dx * dx + dz * dz <= apron.Reach * apron.Reach;
        }

        private static float NearestToHome(Rect area)
        {
            float x = Mathf.Clamp(0f, area.xMin, area.xMax);
            float z = Mathf.Clamp(0f, area.yMin, area.yMax);
            return Mathf.Sqrt(x * x + z * z);
        }

        private static long Key(Vector2Int cell)
        {
            return ((long)cell.x << 32) ^ (uint)cell.y;
        }

        /// <summary>Every chunk offset whose middle lies within <paramref name="radius"/>, nearest first.</summary>
        private static Vector2Int[] RingOffsets(float radius, float chunkSize)
        {
            int reach = Mathf.CeilToInt(radius / chunkSize);
            var offsets = new List<Vector2Int>();
            for (int z = -reach; z <= reach; z++)
            {
                for (int x = -reach; x <= reach; x++)
                {
                    if (new Vector2(x, z).magnitude * chunkSize <= radius)
                    {
                        offsets.Add(new Vector2Int(x, z));
                    }
                }
            }

            offsets.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            return offsets.ToArray();
        }

        /// <summary>The chunk being planned: its square and how far through its grid cells and aprons it has come.</summary>
        private struct Job
        {
            public Job(Vector2Int cell, Rect area, float gridCell)
            {
                Cell = cell;
                Area = area;
                // A cell's jitter keeps its stone inside the cell, so only the cells over the square can put one in it.
                FirstColumn = Mathf.FloorToInt(area.xMin / gridCell);
                LastColumn = Mathf.FloorToInt(area.xMax / gridCell);
                LastRow = Mathf.FloorToInt(area.yMax / gridCell);
                Column = FirstColumn;
                Row = Mathf.FloorToInt(area.yMin / gridCell);
                Apron = 0;
            }

            public Vector2Int Cell { get; }

            public Rect Area { get; }

            public int FirstColumn { get; }

            public int LastColumn { get; }

            public int LastRow { get; }

            public int Column;

            public int Row;

            public int Apron;
        }

        /// <summary>One pooled chunk: its object, renderer and a mesh sized once for the stream's capacity.</summary>
        private sealed class Chunk
        {
            private readonly GameObject _host;
            private readonly Mesh _mesh;

            public Chunk(Transform parent, Material material, HideFlags hideFlags, int capacity, int triangles)
            {
                _host = new GameObject("Rubble") { hideFlags = hideFlags, layer = Layers.Prop };
                _host.transform.SetParent(parent, false);
                _mesh = new Mesh { name = "Rubble", hideFlags = hideFlags };
                _mesh.SetVertexBufferParams(capacity * VerticesPerClod, Layout);
                _mesh.SetIndexBufferParams(capacity * triangles, IndexFormat.UInt16);
                _mesh.subMeshCount = 1;
                _host.AddComponent<MeshFilter>().sharedMesh = _mesh;
                Renderer = _host.AddComponent<MeshRenderer>();
                Renderer.sharedMaterial = material;
                Renderer.shadowCastingMode = ShadowCastingMode.On;
                Renderer.lightProbeUsage = LightProbeUsage.Off;
                Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                Renderer.enabled = false;
            }

            public MeshRenderer Renderer { get; }

            public Vector2Int Cell { get; private set; }

            public Vector2 Middle { get; private set; }

            public Vector3 Origin { get; private set; }

            public Bounds Bounds { get; private set; }

            public int Stones { get; private set; }

            public void Place(Vector2Int cell, Rect area)
            {
                Cell = cell;
                Middle = area.center;
                Origin = new Vector3(area.xMin, 0f, area.yMin);
                _host.transform.localPosition = Origin;
            }

            public void Upload(ScatterVertex[] vertices, int vertexCount, ushort[] indices, int indexCount,
                Bounds bounds, int stones)
            {
                Stones = stones;
                Bounds = new Bounds(bounds.center + Origin, bounds.size);
                if (stones == 0)
                {
                    return;
                }

                _mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0, UploadFlags);
                _mesh.SetIndexBufferData(indices, 0, 0, indexCount, UploadFlags);
                _mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { bounds = bounds, vertexCount = vertexCount },
                    UploadFlags);
                _mesh.bounds = bounds;
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
