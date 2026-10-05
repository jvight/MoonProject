using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Draws one <see cref="TrackRibbon"/> into a dynamic mesh every frame (fading needs per-frame vertex colours).
    /// Vertices are written in this object's local space, so it can sit anywhere in the rover hierarchy.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [DisallowMultipleComponent]
    public sealed class RoverTrackRenderer : MonoBehaviour
    {
        private RoverFxTuning _tuning;
        private TrackRibbon _ribbon;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color32[] _colors;
        private Vector2[] _uvs;
        private int[] _indices;

        public TrackRibbon Ribbon => _ribbon;

        public void Initialize(RoverFxTuning tuning)
        {
            _tuning = tuning;
            _ribbon = new TrackRibbon(tuning.TrackCapacity);
            _vertices = new Vector3[_ribbon.VertexCapacity];
            _colors = new Color32[_ribbon.VertexCapacity];
            _uvs = new Vector2[_ribbon.VertexCapacity];
            _indices = new int[_ribbon.IndexCapacity];
            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        /// <summary>Records the wheel's ground contact this frame.</summary>
        public void Sample(Vector3 point, Vector3 normal, float time)
        {
            _ribbon.Sample(point, normal, time, _tuning.TrackSegmentLength);
        }

        /// <summary>Ends the current strip (wheel in the air).</summary>
        public void Break()
        {
            _ribbon.Break();
        }

        /// <summary>Ages the ribbon and uploads the mesh.</summary>
        public void Rebuild(float time)
        {
            float lifetime = _tuning.TrackLifetime;
            _ribbon.Expire(time, lifetime);
            _ribbon.Build(transform.worldToLocalMatrix, time, _tuning.TrackWidth * 0.5f, _tuning.TrackLift,
                _tuning.TrackOpacity, lifetime * _tuning.TrackFadeStart, lifetime, _vertices, _colors, _uvs, _indices,
                out int vertexCount, out int indexCount);

            _mesh.Clear(true);
            if (indexCount == 0)
            {
                return;
            }

            _mesh.SetVertices(_vertices, 0, vertexCount);
            _mesh.SetColors(_colors, 0, vertexCount);
            _mesh.SetUVs(0, _uvs, 0, vertexCount);
            _mesh.SetTriangles(_indices, 0, indexCount, 0, true);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
