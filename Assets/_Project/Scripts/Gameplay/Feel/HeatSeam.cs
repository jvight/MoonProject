using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The molten seam the beam leaves on a piece (docs/design/feel-pass.md R1, §3.1): a thin ribbon along the piece's
    /// cut line up to the hot point, each point cooling by its age from white-yellow to orange, dull red and dark (the
    /// HeatSeam shader maps the heat the strip writes into its UVs). It follows the piece as it tumbles, stays on the
    /// piece when the hold lets go (kept progress reads as a dark cut line) and its end re-glows on a resume.
    /// </summary>
    public sealed class HeatSeam
    {
        /// <summary>Points the ribbon samples along a cut line.</summary>
        public const int Samples = 24;

        private readonly FeelVfxTuning _tuning;
        private readonly Transform _host;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly Vector3[] _line = new Vector3[Samples];
        private readonly float[] _passed = new float[Samples];
        private readonly Vector3[] _vertices = new Vector3[Samples * 2];
        private readonly Vector2[] _heat = new Vector2[Samples * 2];
        private Vector3 _across;
        private float _clock;
        private float _hot;

        public HeatSeam(Transform parent, FeelVfxTuning tuning, Material material)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material), "HeatSeam: the heat seam material is missing.");
            }

            var host = new GameObject("HeatSeam");
            host.transform.SetParent(parent, false);
            _host = host.transform;
            _mesh = new Mesh { name = "HeatSeam" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.uv = _heat;
            var triangles = new int[(Samples - 1) * 6];
            for (int i = 0; i < Samples - 1; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            _mesh.triangles = triangles;
            host.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = host.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.enabled = false;
        }

        /// <summary>The piece this seam is on, or null when free.</summary>
        public Transform Piece { get; private set; }

        /// <summary>Seconds since this seam last moved (the pool reuses the coldest).</summary>
        public float Idle { get; private set; }

        /// <summary>The heat (0..1) at sample <paramref name="index"/> (tests and debugging views).</summary>
        public float HeatAt(int index)
        {
            return _heat[index * 2].x;
        }

        /// <summary>
        /// Lays the seam on <paramref name="piece"/> along <paramref name="cutLine"/> (piece space, 2+ points), on the
        /// face whose normal is <paramref name="faceNormal"/> (piece space). Nothing of it is cut yet.
        /// </summary>
        public void Begin(Transform piece, IReadOnlyList<Vector3> cutLine, Vector3 faceNormal)
        {
            if (cutLine == null || cutLine.Count < 2)
            {
                throw new ArgumentException("HeatSeam: a cut line needs two or more points.", nameof(cutLine));
            }

            Piece = piece;
            _hot = 0f;
            Idle = 0f;
            Resample(cutLine);
            Vector3 along = _line[Samples - 1] - _line[0];
            _across = Vector3.Cross(along, faceNormal).normalized * (_tuning.SeamWidth * 0.5f);
            Vector3 lift = faceNormal.normalized * _tuning.SeamLift;
            for (int i = 0; i < Samples; i++)
            {
                _line[i] += lift;
                _passed[i] = float.PositiveInfinity;
            }
        }

        /// <summary>The hot point moved to <paramref name="progress"/> (0..1) along the line.</summary>
        public void Cut(float progress)
        {
            _hot = Mathf.Max(_hot, Mathf.Clamp01(progress));
            Idle = 0f;
            int reached = Mathf.FloorToInt(_hot * (Samples - 1));
            for (int i = 0; i <= reached; i++)
            {
                if (float.IsPositiveInfinity(_passed[i]))
                {
                    _passed[i] = _clock;
                }
            }

            _passed[reached] = _clock;
        }

        /// <summary>The seam is let go (the piece is gone): it hides and is free for another piece.</summary>
        public void Clear()
        {
            Piece = null;
            _renderer.enabled = false;
        }

        public void Step(float deltaTime)
        {
            _clock += deltaTime;
            Idle += deltaTime;
            if (Piece == null)
            {
                _renderer.enabled = false;
                return;
            }

            _host.SetPositionAndRotation(Piece.position, Piece.rotation);
            _host.localScale = Piece.lossyScale;
            int reached = Mathf.FloorToInt(_hot * (Samples - 1));
            Vector3 tip = _line[reached];
            for (int i = 0; i < Samples; i++)
            {
                Vector3 at = i <= reached ? _line[i] : tip;
                float heat = i <= reached ? FeelCurves.Heat(_clock - _passed[i], _tuning.SeamOrangeSeconds,
                    _tuning.SeamRedSeconds, _tuning.SeamDarkSeconds) : 0f;
                _vertices[i * 2] = at - _across;
                _vertices[i * 2 + 1] = at + _across;
                _heat[i * 2] = new Vector2(heat, 0f);
                _heat[i * 2 + 1] = new Vector2(heat, 1f);
            }

            _mesh.vertices = _vertices;
            _mesh.uv = _heat;
            _mesh.RecalculateBounds();
            _renderer.enabled = _hot > 0f;
        }

        /// <summary>The world position of the hot point (the seam's end).</summary>
        public Vector3 HotPoint => _host.TransformPoint(_line[Mathf.FloorToInt(_hot * (Samples - 1))]);

        private void Resample(IReadOnlyList<Vector3> cutLine)
        {
            float length = 0f;
            for (int i = 1; i < cutLine.Count; i++)
            {
                length += Vector3.Distance(cutLine[i - 1], cutLine[i]);
            }

            int segment = 0;
            float walked = 0f;
            for (int s = 0; s < Samples; s++)
            {
                float target = length * s / (Samples - 1);
                while (segment < cutLine.Count - 2
                       && walked + Vector3.Distance(cutLine[segment], cutLine[segment + 1]) < target)
                {
                    walked += Vector3.Distance(cutLine[segment], cutLine[segment + 1]);
                    segment++;
                }

                float span = Mathf.Max(Vector3.Distance(cutLine[segment], cutLine[segment + 1]), 1e-5f);
                _line[s] = Vector3.Lerp(cutLine[segment], cutLine[segment + 1], (target - walked) / span);
            }
        }
    }
}
